using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using ElgatoCapture.Models;
using Windows.Graphics.Imaging;
using WinRT;

namespace ElgatoCapture.Services;

/// <summary>
/// FFmpeg-based video encoder service for guaranteed CFR output.
/// Pipes raw video frames and audio samples to FFmpeg subprocess.
/// </summary>
public class FFmpegEncoderService : IDisposable, IAsyncDisposable
{
    private Process? _ffmpegProcess;
    private Stream? _videoStream;  // stdin stream for video
    private NamedPipeServerStream? _audioPipe;
    private BlockingCollection<byte[]>? _videoFrameQueue;
    private BlockingCollection<byte[]>? _audioSampleQueue;
    private CancellationTokenSource? _cts;
    private Task? _videoWriterTask;
    private Task? _audioWriterTask;
    private Task? _stderrReaderTask;
    private volatile bool _isEncoding;  // volatile for thread-safe reads across threads
    private volatile bool _audioQueueReady; // Allow audio samples to be queued before encoding starts
    private volatile bool _useAudioPipe; // Whether to use named pipe for audio (vs anullsrc)
    private volatile bool _stderrClosed; // Indicates stderr closed (FFmpeg crashed or exited)
    private readonly string _ffmpegPath;
    private readonly object _disposeLock = new();
    private bool _isDisposed;
    private long _droppedVideoFrames;
    private EncoderSupport _encoderSupport = EncoderSupport.Empty;
    private static Task<EncoderSupport>? _encoderProbeTask;
    private static readonly object _encoderProbeLock = new();
    private readonly Stopwatch _startupStopwatch = new();
    private int _loggedFirstVideoWrite;
    private int _loggedFirstAudioWrite;
    private int _loggedFirstVideoEnqueue;
    private int _loggedFirstAudioEnqueue;
    private long _lastVideoEnqueueTick;
    private long _lastAudioEnqueueTick;
    private long _lastVideoWriteTick;
    private long _lastAudioWriteTick;
    private long _lastVideoIdleLogTick;
    private long _lastAudioIdleLogTick;

    // Frame buffer pool to reduce GC pressure
    private readonly ConcurrentBag<byte[]> _frameBufferPool = new();
    private const int MaxQueueSize = 360; // ~6 seconds at 60fps (absorbs FFmpeg stalls)
    private const int MaxPoolSize = 10;
    private const string AudioPipeName = "ElgatoCaptureAudio";
    private const int VideoQueueWaitMs = 50; // Allow more time for queue to drain during stalls

    public event EventHandler<string>? StatusChanged;
    public event EventHandler<string>? ErrorOccurred;
    public event EventHandler<ulong>? FrameEncoded;

    public bool IsEncoding => _isEncoding;
    private ulong _encodedFrameCount;
    public ulong EncodedFrameCount => Interlocked.Read(ref _encodedFrameCount);
    public string FfmpegPath => _ffmpegPath;
    public int VideoQueueCount => _videoFrameQueue?.Count ?? 0;
    public int AudioQueueCount => _audioSampleQueue?.Count ?? 0;
    public long DroppedVideoFrames => Interlocked.Read(ref _droppedVideoFrames);

    public FFmpegEncoderService()
    {
        // Look for FFmpeg in multiple locations
        _ffmpegPath = FindFFmpegPath();
    }

    /// <summary>
    /// Prepares the audio queue so audio samples can be buffered before encoding starts.
    /// Call this BEFORE starting the audio capture graph, so audio samples can be queued
    /// while FFmpeg is starting up and probing inputs.
    /// </summary>
    public void PrepareAudioQueue()
    {
        if (_audioSampleQueue != null)
        {
            return; // Already prepared
        }

        Logger.Log("Preparing audio queue for early buffering");
        _audioSampleQueue = new BlockingCollection<byte[]>(MaxQueueSize * 10);
        _audioQueueReady = true;
    }

    private static string FindFFmpegPath()
    {
        // Check application directory first
        var appDir = AppContext.BaseDirectory;
        var paths = new[]
        {
            Path.Combine(appDir, "ffmpeg", "ffmpeg.exe"),
            Path.Combine(appDir, "ffmpeg.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "ffmpeg", "bin", "ffmpeg.exe"),
            "ffmpeg.exe" // PATH lookup
        };

        foreach (var path in paths)
        {
            if (path == "ffmpeg.exe")
            {
                // Check if ffmpeg is in PATH
                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = "where",
                        Arguments = "ffmpeg.exe",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    };
                    using var proc = Process.Start(psi);
                    if (proc != null)
                    {
                        var output = proc.StandardOutput.ReadToEnd();
                        proc.WaitForExit();
                        if (proc.ExitCode == 0 && !string.IsNullOrWhiteSpace(output))
                        {
                            return output.Split('\n')[0].Trim();
                        }
                    }
                }
                catch (Exception ex)
                {
                    // PATH lookup failed - this is expected on some systems
                    System.Diagnostics.Debug.WriteLine($"FFmpeg PATH lookup failed: {ex.Message}");
                }
            }
            else if (File.Exists(path))
            {
                return path;
            }
        }

        return "ffmpeg.exe"; // Fallback, will fail gracefully if not found
    }

    public static Task<EncoderSupport> GetEncoderSupportAsync()
    {
        lock (_encoderProbeLock)
        {
            _encoderProbeTask ??= ProbeEncoderSupportAsync();
            return _encoderProbeTask;
        }
    }

    private static async Task<EncoderSupport> ProbeEncoderSupportAsync()
    {
        var ffmpegPath = FindFFmpegPath();
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = ffmpegPath,
                Arguments = "-hide_banner -encoders",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process == null)
            {
                Logger.Log("FFmpeg encoder probe failed: process could not start");
                return EncoderSupport.Empty;
            }

            var stdoutTask = process.StandardOutput.ReadToEndAsync();
            var stderrTask = process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            var output = (await stdoutTask) + "\n" + (await stderrTask);

            var support = new EncoderSupport
            {
                HasH264Nvenc = output.Contains("h264_nvenc"),
                HasHevcNvenc = output.Contains("hevc_nvenc"),
                HasAv1Nvenc = output.Contains("av1_nvenc"),
                HasLibX264 = output.Contains("libx264"),
                HasLibX265 = output.Contains("libx265"),
                HasLibSvtAv1 = output.Contains("libsvtav1"),
                HasLibAomAv1 = output.Contains("libaom-av1")
            };

            Logger.Log(
                $"Encoder support: H.264={support.HasH264} (nvenc={support.HasH264Nvenc}, x264={support.HasLibX264}), " +
                $"HEVC={support.HasHevc} (nvenc={support.HasHevcNvenc}, x265={support.HasLibX265}), " +
                $"AV1={support.HasAv1} (nvenc={support.HasAv1Nvenc}, svt={support.HasLibSvtAv1}, aom={support.HasLibAomAv1})");

            return support;
        }
        catch (Exception ex)
        {
            Logger.Log($"FFmpeg encoder probe failed: {ex.Message}");
            return EncoderSupport.Empty;
        }
    }

    public async Task StartEncodingAsync(CaptureSettings settings, string outputPath, string? audioDeviceName = null, double? actualFrameRate = null, string? actualFrameRateArg = null, uint? actualWidth = null, uint? actualHeight = null, string inputPixelFormat = "bgra")
    {
        if (_isEncoding)
        {
            throw new InvalidOperationException("Encoding already in progress");
        }

        Logger.Log("=== FFmpeg Encoder Starting ===");
        Logger.Log($"FFmpeg path: {_ffmpegPath}");
        Logger.Log($"Output: {outputPath}");
        var effectiveFrameRate = actualFrameRate ?? settings.FrameRate;
        var frameRateArg = !string.IsNullOrWhiteSpace(actualFrameRateArg)
            ? actualFrameRateArg
            : FormatFrameRateArg(effectiveFrameRate);
        var effectiveWidth = actualWidth ?? settings.Width;
        var effectiveHeight = actualHeight ?? settings.Height;
        Logger.Log($"Settings: {settings.Width}x{settings.Height}@{effectiveFrameRate:0.###}fps, Format={settings.Format}, Quality={settings.Quality}");
        if (effectiveWidth != settings.Width || effectiveHeight != settings.Height)
        {
            Logger.Log($"FFmpeg using actual device size: {effectiveWidth}x{effectiveHeight}");
        }
        Logger.Log($"FFmpeg frame rate arg: {frameRateArg}");
        if (!settings.AudioEnabled)
        {
            Logger.Log("Audio: disabled (video-only output)");
        }
        else
        {
            Logger.Log($"Audio device: {audioDeviceName ?? "(none - using silent audio)"}");
        }

        // Probe encoder support once per process (can be slow)
        _startupStopwatch.Restart();
        Logger.LogVerbose("Encoder probe: awaiting cached result");
        _encoderSupport = await GetEncoderSupportAsync();
        Logger.LogVerbose($"Encoder probe complete in {_startupStopwatch.ElapsedMilliseconds} ms");

        _cts = new CancellationTokenSource();
        _loggedFirstVideoWrite = 0;
        _loggedFirstAudioWrite = 0;
        _loggedFirstVideoEnqueue = 0;
        _loggedFirstAudioEnqueue = 0;
        _lastVideoEnqueueTick = 0;
        _lastAudioEnqueueTick = 0;
        _lastVideoWriteTick = Environment.TickCount64;
        _lastAudioWriteTick = Environment.TickCount64;
        _lastVideoIdleLogTick = 0;
        _lastAudioIdleLogTick = 0;
        _stderrClosed = false; // Reset crash detection flag for new recording
        _videoFrameQueue = new BlockingCollection<byte[]>(MaxQueueSize);
        _audioQueueReady = false;
        Interlocked.Exchange(ref _encodedFrameCount, 0);

        // Determine if we should use named pipe for audio
        // This is set to true when audioDeviceName is provided, meaning CaptureService
        // will be capturing audio via AudioGraph and piping it to us
        _useAudioPipe = settings.AudioEnabled && !string.IsNullOrEmpty(audioDeviceName);

        // Only create audio queue if audio is enabled and we're using the pipe
        // This preserves any audio samples that were buffered while starting up
        if (_useAudioPipe)
        {
            if (_audioSampleQueue == null)
            {
                _audioSampleQueue = new BlockingCollection<byte[]>(MaxQueueSize * 10);
            }
            _audioQueueReady = true;
        }
        else if (_audioSampleQueue != null)
        {
            _audioSampleQueue.Dispose();
            _audioSampleQueue = null;
        }

        // Create named pipe for audio BEFORE starting FFmpeg
        if (_useAudioPipe)
        {
            Logger.Log($"Creating audio named pipe: {AudioPipeName}");
            _audioPipe = new NamedPipeServerStream(
                AudioPipeName,
                PipeDirection.Out,
                1, // maxNumberOfServerInstances
                PipeTransmissionMode.Byte,
                PipeOptions.Asynchronous);
            Logger.Log("Audio named pipe created");
        }

        // Build FFmpeg command (uses stdin for video, named pipe for audio)
        var ffmpegArgs = BuildFFmpegArguments(settings, outputPath, _useAudioPipe, frameRateArg, effectiveWidth, effectiveHeight, inputPixelFormat);
        Logger.Log($"FFmpeg arguments: {ffmpegArgs}");

        // Start FFmpeg process
        var startInfo = new ProcessStartInfo
        {
            FileName = _ffmpegPath,
            Arguments = ffmpegArgs,
            UseShellExecute = false,
            RedirectStandardInput = true,  // Video goes through stdin
            RedirectStandardOutput = false,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        try
        {
            _ffmpegProcess = Process.Start(startInfo);
            if (_ffmpegProcess == null)
            {
                throw new Exception("Failed to start FFmpeg process");
            }

            Logger.Log($"FFmpeg process started (PID: {_ffmpegProcess.Id})");
            Logger.LogVerbose($"FFmpeg process start took {_startupStopwatch.ElapsedMilliseconds} ms");

            // Start stderr reader task (for logging FFmpeg output)
            _stderrReaderTask = Task.Run(() => ReadStderrAsync(_cts.Token), _cts.Token);

            // Video uses stdin - immediately available, no connection needed
            _videoStream = _ffmpegProcess.StandardInput.BaseStream;
            Logger.Log("Video stream ready (using stdin)");

            // Set _isEncoding = true NOW so frames can be enqueued immediately
            // This must happen BEFORE returning so the recording frame reader can start feeding frames
            _isEncoding = true;

            // Start video writer immediately - video flows through stdin
            _videoWriterTask = Task.Run(() => WriteVideoFramesAsync(_cts.Token), _cts.Token);

            // Start audio writer if using named pipe for audio
            if (_useAudioPipe && _audioPipe != null)
            {
                Logger.Log("Starting audio writer task for named pipe");
                _audioWriterTask = Task.Run(() => WriteAudioSamplesAsync(_cts.Token), _cts.Token);
            }
            else
            {
                _audioWriterTask = Task.CompletedTask;
            }

            StatusChanged?.Invoke(this, "Encoding started");
            Logger.Log("=== FFmpeg Encoder Started ===");
        }
        catch (Exception ex)
        {
            Logger.LogException(ex);
            ErrorOccurred?.Invoke(this, $"Failed to start FFmpeg: {ex.Message}");
            await CleanupAsync();
            throw;
        }
    }

    private string BuildFFmpegArguments(CaptureSettings settings, string outputPath, bool useAudioPipe, string frameRateArg, uint effectiveWidth, uint effectiveHeight, string inputPixelFormat)
    {
        // Get encoder and quality settings
        var (videoCodec, qualityArgs) = GetEncoderSettings(settings);

        // Build audio input string
        // Using named pipe for audio gives us full timestamp control - both streams start at 0
        string audioInput = "";
        string audioArgs = "";
        if (settings.AudioEnabled)
        {
            if (useAudioPipe)
            {
            // Read raw float audio from named pipe
            // Format: f32le (32-bit float little-endian), 48kHz, stereo
            // This matches what AudioGraph actually outputs
            audioInput = $"-f f32le " +
                        $"-ar 48000 " +
                        $"-ac 2 " +
                        $"-thread_queue_size 1024 " +
                            $"-i \\\\.\\pipe\\{AudioPipeName} ";
                Logger.Log("Using named pipe for audio input");
            }
            else
            {
                // Generate silent audio track with lavfi (fallback)
                audioInput = $"-f lavfi -i anullsrc=channel_layout=stereo:sample_rate=48000 ";
            }

            audioArgs = "-c:a aac -b:a 320k ";
        }
        else
        {
            audioArgs = "-an ";
        }

        // Build arguments
        var outputPixelFormat = videoCodec.Contains("_nvenc", StringComparison.OrdinalIgnoreCase) && inputPixelFormat.Equals("nv12", StringComparison.OrdinalIgnoreCase)
            ? "nv12"
            : "yuv420p";

        var args = $"-y " +
                   // Video input from stdin (pipe:0) - immediately available
                   $"-probesize 32 " +        // Minimal probe size (bytes) - format is known
                   $"-analyzeduration 0 " +   // Don't analyze duration
                   $"-f rawvideo " +
                   $"-pixel_format {inputPixelFormat} " +
                   $"-video_size {effectiveWidth}x{effectiveHeight} " +
                   $"-framerate {frameRateArg} " +
                   $"-thread_queue_size 512 " + // Buffer video frames for sync with audio
                   $"-i pipe:0 " +            // stdin for video - no connection issues
                   // Audio input (named pipe or anullsrc)
                   audioInput +
                   // Video encoding with CFR
                   $"-c:v {videoCodec} " +
                   $"{qualityArgs} " +
                   $"-r {frameRateArg} " + // Force output frame rate (CFR)
                   $"-pix_fmt {outputPixelFormat} " +
                   // Audio encoding (or disable audio)
                   audioArgs +
                   // Output options
                   $"-shortest " + // End when shortest input ends
                   $"-movflags +faststart " + // Enable fast start for MP4
                   $"\"{outputPath}\"";

        return args;
    }

    private static string FormatFrameRateArg(double frameRate)
    {
        if (frameRate <= 0)
        {
            return "30";
        }
        return frameRate.ToString("0.###", CultureInfo.InvariantCulture);
    }

    private (string codec, string qualityArgs) GetEncoderSettings(CaptureSettings settings)
    {
        string codec;
        string qualityArgs;

        var support = _encoderSupport ?? EncoderSupport.Empty;
        bool useNvenc;

        // Determine base codec based on format and encoder availability
        switch (settings.Format)
        {
            case RecordingFormat.HevcMp4:
                codec = support.HasHevcNvenc ? "hevc_nvenc" : "libx265";
                useNvenc = codec.EndsWith("_nvenc", StringComparison.OrdinalIgnoreCase);
                break;
            case RecordingFormat.Av1Mp4:
                codec = support.PreferredAv1Encoder ?? "libsvtav1";
                useNvenc = codec.EndsWith("_nvenc", StringComparison.OrdinalIgnoreCase);
                break;
            case RecordingFormat.UncompressedAvi:
                // For "uncompressed", use high bitrate H.264
                codec = support.HasH264Nvenc ? "h264_nvenc" : "libx264";
                useNvenc = codec.EndsWith("_nvenc", StringComparison.OrdinalIgnoreCase);
                break;
            default: // H264Mp4
                codec = support.HasH264Nvenc ? "h264_nvenc" : "libx264";
                useNvenc = codec.EndsWith("_nvenc", StringComparison.OrdinalIgnoreCase);
                break;
        }

        // Determine quality settings
        var isAv1 = codec.Contains("av1", StringComparison.OrdinalIgnoreCase);
        var isSvtAv1 = codec.Equals("libsvtav1", StringComparison.OrdinalIgnoreCase);
        var isAomAv1 = codec.Equals("libaom-av1", StringComparison.OrdinalIgnoreCase);
        var nvencProfile = codec.Contains("hevc", StringComparison.OrdinalIgnoreCase) || isAv1 ? "main" : "high";
        if (settings.Quality == VideoQuality.Custom)
        {
            // Use CBR with user-specified bitrate
            var bitrate = (int)(settings.CustomBitrateMbps * 1000); // kbps

            if (useNvenc)
            {
                // NVENC CBR mode
                qualityArgs = $"-preset p4 -rc cbr -b:v {bitrate}k -maxrate {bitrate}k -bufsize {bitrate * 2}k -profile:v {nvencProfile} -bf 3";
            }
            else if (isSvtAv1 || isAomAv1)
            {
                qualityArgs = $"-b:v {bitrate}k";
            }
            else
            {
                // CPU CBR mode
                qualityArgs = $"-b:v {bitrate}k -maxrate {bitrate}k -bufsize {bitrate * 2}k";
            }
        }
        else
        {
            // Use CQ/CRF for quality-based encoding
            var qualityValue = settings.Quality switch
            {
                VideoQuality.Low => isAv1 ? 40 : 28,
                VideoQuality.Medium => isAv1 ? 32 : 23,
                VideoQuality.High => isAv1 ? 28 : 18,
                VideoQuality.VeryHigh => isAv1 ? 24 : 15,
                VideoQuality.Lossless => 0,
                _ => isAv1 ? 28 : 23 // Auto
            };

            // Map preset based on quality level
            string preset = settings.Quality switch
            {
                VideoQuality.Lossless => useNvenc ? "p7" : "ultrafast",
                VideoQuality.VeryHigh => useNvenc ? "p6" : "fast",
                VideoQuality.High => useNvenc ? "p5" : "fast",
                _ => useNvenc ? "p4" : "fast"
            };

            if (useNvenc)
            {
                // NVENC CQ (Constant Quality) mode
                if (settings.Quality == VideoQuality.Lossless)
                {
                    // Lossless mode
                    qualityArgs = $"-preset {preset} -rc lossless -profile:v {nvencProfile} -bf 3";
                }
                else
                {
                    // CQ mode (equivalent to CRF)
                    qualityArgs = $"-preset {preset} -rc vbr -cq {qualityValue} -b:v 0 -profile:v {nvencProfile} -bf 3";
                }
            }
            else if (isSvtAv1)
            {
                var svtPreset = settings.Quality switch
                {
                    VideoQuality.Low => 11,
                    VideoQuality.Medium => 10,
                    VideoQuality.High => 9,
                    VideoQuality.VeryHigh => 8,
                    VideoQuality.Lossless => 8,
                    _ => 9
                };
                qualityArgs = $"-preset {svtPreset} -crf {qualityValue}";
            }
            else if (isAomAv1)
            {
                var cpuUsed = settings.Quality switch
                {
                    VideoQuality.Low => 8,
                    VideoQuality.Medium => 6,
                    VideoQuality.High => 6,
                    VideoQuality.VeryHigh => 4,
                    VideoQuality.Lossless => 4,
                    _ => 6
                };
                qualityArgs = $"-cpu-used {cpuUsed} -crf {qualityValue} -b:v 0";
            }
            else
            {
                // CPU encoding (existing logic)
                if (settings.Quality == VideoQuality.Lossless)
                {
                    qualityArgs = $"-preset {preset} -crf 0";
                }
                else
                {
                    var cfr_opt = codec == "libx264" ? "-x264opts force-cfr=1" : "";
                    qualityArgs = $"-preset {preset} -crf {qualityValue} {cfr_opt}";
                }
            }
        }

        return (codec, qualityArgs);
    }

    /// <summary>
    /// Enqueue a video frame for encoding.
    /// </summary>
    public void EnqueueVideoFrame(SoftwareBitmap frame)
    {
        if (!_isEncoding || _videoFrameQueue == null || _cts?.IsCancellationRequested == true)
        {
            return;
        }

        _lastVideoEnqueueTick = Environment.TickCount64;
        if (Logger.VerboseEnabled && Interlocked.Exchange(ref _loggedFirstVideoEnqueue, 1) == 0)
        {
            Logger.LogVerbose($"First video frame enqueued after {_startupStopwatch.ElapsedMilliseconds} ms (queueCount={_videoFrameQueue.Count})");
        }

        try
        {
            var bufferSize = GetFrameSizeBytes(frame);
            if (bufferSize <= 0)
            {
                Logger.Log($"Unsupported pixel format for recording: {frame.BitmapPixelFormat}");
                return;
            }

            // Get or create buffer
            var buffer = GetFrameBuffer(bufferSize);

            if (frame.BitmapPixelFormat == BitmapPixelFormat.Nv12)
            {
                CopyNv12ToBuffer(frame, buffer);
            }
            else
            {
                // BGRA/YUY2 fallback
                frame.CopyToBuffer(buffer.AsBuffer());
            }

            // Try to add to queue with a short wait to reduce drops
            if (!_videoFrameQueue.TryAdd(buffer, VideoQueueWaitMs))
            {
                ReturnFrameBuffer(buffer);
                var dropped = Interlocked.Increment(ref _droppedVideoFrames);
                if (dropped == 1 || dropped % 30 == 0)
                {
                    Logger.Log($"Warning: Dropped video frame (queue full). Total dropped: {dropped}");
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogException(ex);
        }
    }

    /// <summary>
    /// Enqueue audio samples for encoding.
    /// Samples can be queued as soon as PrepareAudioQueue() has been called,
    /// even before StartEncodingAsync() completes.
    /// </summary>
    public void EnqueueAudioSamples(byte[] samples)
    {
        // Accept audio samples as soon as queue is ready (not just when encoding)
        // This allows audio to buffer while FFmpeg is starting up
        if (!_audioQueueReady || _audioSampleQueue == null || _cts?.IsCancellationRequested == true)
        {
            return;
        }

        _lastAudioEnqueueTick = Environment.TickCount64;
        if (Logger.VerboseEnabled && Interlocked.Exchange(ref _loggedFirstAudioEnqueue, 1) == 0)
        {
            Logger.LogVerbose($"First audio samples enqueued after {_startupStopwatch.ElapsedMilliseconds} ms (queueCount={_audioSampleQueue.Count})");
        }

        try
        {
            // Make a copy of the samples
            var buffer = new byte[samples.Length];
            Buffer.BlockCopy(samples, 0, buffer, 0, samples.Length);

            // Try to add to queue (non-blocking)
            if (!_audioSampleQueue.TryAdd(buffer, 0))
            {
                // Queue full - drop oldest samples
                if (_audioSampleQueue.TryTake(out _))
                {
                    Logger.Log("Warning: Dropped audio samples (queue full)");
                }
                _audioSampleQueue.TryAdd(buffer, 0);
            }
        }
        catch (Exception ex)
        {
            Logger.LogException(ex);
        }
    }

    private async Task WriteVideoFramesAsync(CancellationToken ct)
    {
        Logger.Log("Video writer task started");

        try
        {
            // Capture local references to avoid null check-then-use race conditions
            var videoStream = _videoStream;
            var videoQueue = _videoFrameQueue;
            var process = _ffmpegProcess;

            if (videoStream == null || videoQueue == null)
            {
                Logger.Log("Video writer: stream or queue is null, exiting");
                return;
            }

            while (!ct.IsCancellationRequested && _isEncoding)
            {
                // Check if FFmpeg has exited prematurely or crashed
                var hasExited = process != null && process.HasExited;
                var stderrClosed = _stderrClosed && !hasExited;  // stderr closed but process still "running" = zombie

                if (hasExited || stderrClosed)
                {
                    var exitCode = hasExited ? process!.ExitCode : -1;
                    var reason = hasExited ? "process exited" : "stderr closed (possible crash)";
                    Logger.Log($"Video writer: FFmpeg {reason} (code: {exitCode})");
                    ErrorOccurred?.Invoke(this, $"FFmpeg exited unexpectedly: {reason}, code {exitCode}");
                    _isEncoding = false;  // Stop accepting new frames
                    DrainQueueOnCrash();
                    break;
                }

                try
                {
                    // Block until frame available or cancellation
                    if (videoQueue.TryTake(out var frameData, 100, ct))
                    {
                        await videoStream.WriteAsync(frameData, 0, frameData.Length, ct);
                        var count = Interlocked.Increment(ref _encodedFrameCount);
                        FrameEncoded?.Invoke(this, count);
                        _lastVideoWriteTick = Environment.TickCount64;
                        if (Interlocked.Exchange(ref _loggedFirstVideoWrite, 1) == 0)
                        {
                            Logger.LogVerbose($"First video frame written after {_startupStopwatch.ElapsedMilliseconds} ms");
                        }

                        // Return buffer to pool
                        ReturnFrameBuffer(frameData);
                    }
                    else if (Logger.VerboseEnabled)
                    {
                        var now = Environment.TickCount64;
                        var idleMs = now - _lastVideoWriteTick;
                        if (idleMs >= 1000 && now - _lastVideoIdleLogTick >= 1000)
                        {
                            _lastVideoIdleLogTick = now;
                            var lastEnqueueAge = _lastVideoEnqueueTick == 0 ? -1 : now - _lastVideoEnqueueTick;
                            Logger.LogVerbose(
                                $"Video writer idle: {idleMs} ms (queue={videoQueue.Count}, lastEnqueueMs={lastEnqueueAge}, dropped={Interlocked.Read(ref _droppedVideoFrames)})");
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
        catch (IOException ex)
        {
            // Stream closed - FFmpeg likely exited
            Logger.Log($"Video stream closed: {ex.Message}");
        }
        catch (Exception ex)
        {
            Logger.LogException(ex);
            ErrorOccurred?.Invoke(this, $"Video write error: {ex.Message}");
        }

        Logger.Log($"Video writer task ended. Frames encoded: {EncodedFrameCount}");
    }

    private async Task WriteAudioSamplesAsync(CancellationToken ct)
    {
        Logger.Log("Audio writer task started");

        try
        {
            // Capture local references to avoid null check-then-use race conditions
            var audioPipe = _audioPipe;
            var audioQueue = _audioSampleQueue;
            var process = _ffmpegProcess;

            if (audioPipe == null)
            {
                Logger.Log("Audio pipe is null - audio will not be recorded");
                return;
            }

            // Wait for FFmpeg to connect to audio pipe (it connects after receiving some video frames)
            Logger.Log("Audio writer: waiting for FFmpeg to connect to audio pipe...");
            var connectStartTicks = Logger.VerboseEnabled ? Stopwatch.GetTimestamp() : 0;
            try
            {
                // Use a timeout - if FFmpeg doesn't connect within 15 seconds, skip audio
                var connectTask = audioPipe.WaitForConnectionAsync(ct);
                var timeoutTask = Task.Delay(15000, ct);
                var result = await Task.WhenAny(connectTask, timeoutTask);

                if (result == timeoutTask)
                {
                    Logger.Log("Audio pipe connection timed out - audio will not be recorded");
                    ErrorOccurred?.Invoke(this, "Audio recording failed: FFmpeg did not connect to audio pipe within 15 seconds");
                    return;
                }
            }
            catch (OperationCanceledException)
            {
                Logger.Log("Audio pipe connection cancelled");
                return;
            }

            Logger.Log("Audio writer: pipe connected, starting to write samples");
            if (Logger.VerboseEnabled && connectStartTicks != 0)
            {
                var connectMs = (Stopwatch.GetTimestamp() - connectStartTicks) * 1000.0 / Stopwatch.Frequency;
                Logger.LogVerbose($"Audio pipe connected after {connectMs:0.00} ms");
            }

            // Write initial silence buffer to ensure FFmpeg has data during startup
            // This prevents FFmpeg from timing out while probing the audio input
            // 48000 Hz * 2 channels * 4 bytes/sample = 384000 bytes/second (f32le)
            // Write ~100ms of silence (38400 bytes)
            var silence = new byte[38400];
            await audioPipe.WriteAsync(silence, 0, silence.Length, ct);
            Logger.Log("Audio writer: wrote initial silence buffer (100ms)");
            _lastAudioWriteTick = Environment.TickCount64;
            if (Interlocked.Exchange(ref _loggedFirstAudioWrite, 1) == 0)
            {
                Logger.LogVerbose($"First audio bytes written after {_startupStopwatch.ElapsedMilliseconds} ms");
            }

            // Now drain any buffered samples from the queue as fast as possible
            if (audioQueue != null)
            {
                while (audioQueue.TryTake(out var bufferedSample, 0))
                {
                    if (audioPipe.IsConnected)
                    {
                        await audioPipe.WriteAsync(bufferedSample, 0, bufferedSample.Length, ct);
                    }
                }
            }
            Logger.Log("Audio writer: drained initial buffer, entering normal write loop");

            while (!ct.IsCancellationRequested && _isEncoding && audioQueue != null)
            {
                // Check for FFmpeg crash via process exit or stderr closing
                var hasExited = process != null && process.HasExited;
                var stderrClosed = _stderrClosed && !hasExited;

                if (hasExited || stderrClosed)
                {
                    Logger.Log("Audio writer: FFmpeg crashed or exited, stopping");
                    break;
                }

                // Check if pipe is still connected
                if (!audioPipe.IsConnected)
                {
                    Logger.Log("Audio writer: pipe disconnected, stopping");
                    break;
                }

                try
                {
                    // Block until samples available or cancellation
                    if (audioQueue.TryTake(out var audioData, 100, ct))
                    {
                        await audioPipe.WriteAsync(audioData, 0, audioData.Length, ct);
                        _lastAudioWriteTick = Environment.TickCount64;
                        if (Interlocked.Exchange(ref _loggedFirstAudioWrite, 1) == 0)
                        {
                            Logger.LogVerbose($"First audio bytes written after {_startupStopwatch.ElapsedMilliseconds} ms");
                        }
                    }
                    else if (Logger.VerboseEnabled)
                    {
                        var now = Environment.TickCount64;
                        var idleMs = now - _lastAudioWriteTick;
                        if (idleMs >= 1000 && now - _lastAudioIdleLogTick >= 1000)
                        {
                            _lastAudioIdleLogTick = now;
                            var lastEnqueueAge = _lastAudioEnqueueTick == 0 ? -1 : now - _lastAudioEnqueueTick;
                            Logger.LogVerbose(
                                $"Audio writer idle: {idleMs} ms (queue={audioQueue.Count}, lastEnqueueMs={lastEnqueueAge})");
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
        catch (IOException ex)
        {
            // Pipe closed - FFmpeg likely exited
            Logger.Log($"Audio pipe closed: {ex.Message}");
        }
        catch (Exception ex)
        {
            Logger.LogException(ex);
            ErrorOccurred?.Invoke(this, $"Audio write error: {ex.Message}");
        }

        Logger.Log("Audio writer task ended");
    }

    private async Task ReadStderrAsync(CancellationToken ct)
    {
        if (_ffmpegProcess?.StandardError == null) return;

        try
        {
            while (!ct.IsCancellationRequested && !_ffmpegProcess.HasExited)
            {
                var line = await _ffmpegProcess.StandardError.ReadLineAsync(ct);
                if (line == null)
                {
                    // EOF - stderr closed, FFmpeg likely crashed or exited
                    Logger.Log("FFmpeg stderr closed (EOF) - process may have exited");
                    _stderrClosed = true;
                    break;
                }

                Logger.Log($"[FFmpeg] {line}");

                // Check for common errors
                if (line.Contains("Error") || line.Contains("error"))
                {
                    ErrorOccurred?.Invoke(this, line);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected during shutdown
        }
        catch (Exception ex)
        {
            Logger.Log($"Stderr reader error: {ex.Message}");
            _stderrClosed = true;  // Mark as closed on any error
        }
        finally
        {
            _stderrClosed = true;  // Always mark closed when task ends
            Logger.Log("Stderr reader task ended");
        }
    }

    public async Task StopEncodingAsync()
    {
        if (!_isEncoding)
        {
            return;
        }

        Logger.Log("=== FFmpeg Encoder Stopping ===");
        _isEncoding = false;
        _audioQueueReady = false;

        // Signal cancellation
        _cts?.Cancel();

        // Complete the queues (signals writers to finish)
        _videoFrameQueue?.CompleteAdding();
        _audioSampleQueue?.CompleteAdding();

        // Wait for writer tasks to finish (with timeout)
        var tasksToWait = new List<Task>();
        if (_videoWriterTask != null) tasksToWait.Add(_videoWriterTask);
        if (_audioWriterTask != null) tasksToWait.Add(_audioWriterTask);
        if (_stderrReaderTask != null) tasksToWait.Add(_stderrReaderTask);
        if (tasksToWait.Count > 0)
        {
            await Task.WhenAny(Task.WhenAll(tasksToWait), Task.Delay(5000));
        }

        // Close pipes (signals EOF to FFmpeg)
        await CleanupPipesAsync();

        // Wait for FFmpeg to finish encoding
        if (_ffmpegProcess != null && !_ffmpegProcess.HasExited)
        {
            Logger.Log("Waiting for FFmpeg to finish encoding...");
            var exitTask = _ffmpegProcess.WaitForExitAsync();
            if (await Task.WhenAny(exitTask, Task.Delay(30000)) != exitTask)
            {
                Logger.Log("FFmpeg did not exit in time, killing process");
                _ffmpegProcess.Kill();
            }
        }

        if (_ffmpegProcess != null)
        {
            Logger.Log($"FFmpeg exited with code: {_ffmpegProcess.ExitCode}");
        }

        await CleanupAsync();
        StatusChanged?.Invoke(this, "Encoding stopped");
        Logger.Log($"=== FFmpeg Encoder Stopped (Total frames: {EncodedFrameCount}) ===");
    }

    private async Task CleanupPipesAsync()
    {
        try
        {
            var videoStream = _videoStream;
            if (videoStream != null)
            {
                if (videoStream.CanWrite)
                {
                    await videoStream.FlushAsync();
                }
                // Don't close _videoStream directly - it's the process's stdin
                // Closing it signals EOF to FFmpeg
                _ffmpegProcess?.StandardInput.Close();
            }
        }
        catch (IOException)
        {
            // Stream already closed - expected during shutdown
        }
        catch (ObjectDisposedException)
        {
            // Stream already disposed - expected during shutdown
        }
        catch (Exception ex)
        {
            Logger.Log($"Error closing video stream: {ex.Message}");
        }

        try
        {
            var audioPipe = _audioPipe;
            if (audioPipe != null)
            {
                if (audioPipe.IsConnected)
                {
                    await audioPipe.FlushAsync();
                }
                audioPipe.Close();
            }
        }
        catch (IOException)
        {
            // Pipe already closed - expected during shutdown
        }
        catch (ObjectDisposedException)
        {
            // Pipe already disposed - expected during shutdown
        }
        catch (Exception ex)
        {
            Logger.Log($"Error closing audio pipe: {ex.Message}");
        }
    }

    private static int GetFrameSizeBytes(SoftwareBitmap frame)
    {
        var width = frame.PixelWidth;
        var height = frame.PixelHeight;

        return frame.BitmapPixelFormat switch
        {
            BitmapPixelFormat.Nv12 => (width * height * 3) / 2,
            BitmapPixelFormat.Yuy2 => width * height * 2,
            BitmapPixelFormat.Bgra8 => width * height * 4,
            _ => -1
        };
    }

    private static unsafe void CopyNv12ToBuffer(SoftwareBitmap frame, byte[] destination)
    {
        using var bitmapBuffer = frame.LockBuffer(BitmapBufferAccessMode.Read);
        using var reference = bitmapBuffer.CreateReference();

        byte* dataInBytes;
        uint capacityInBytes;
        var byteAccess = reference.As<IMemoryBufferByteAccess>();
        byteAccess.GetBuffer(out dataInBytes, out capacityInBytes);

        var planeY = bitmapBuffer.GetPlaneDescription(0);
        var planeUV = bitmapBuffer.GetPlaneDescription(1);

        var width = frame.PixelWidth;
        var height = frame.PixelHeight;
        var ySize = width * height;
        var uvHeight = height / 2;
        var totalSize = ySize + (uvHeight * width);

        if (destination.Length < totalSize)
        {
            throw new ArgumentException("Destination buffer too small for NV12 frame.");
        }

        fixed (byte* dest = destination)
        {
            // Copy Y plane
            for (var row = 0; row < height; row++)
            {
                Buffer.MemoryCopy(
                    dataInBytes + planeY.StartIndex + (row * planeY.Stride),
                    dest + (row * width),
                    width,
                    width);
            }

            // Copy UV plane (interleaved)
            var uvDest = dest + ySize;
            for (var row = 0; row < uvHeight; row++)
            {
                Buffer.MemoryCopy(
                    dataInBytes + planeUV.StartIndex + (row * planeUV.Stride),
                    uvDest + (row * width),
                    width,
                    width);
            }
        }
    }

    private async Task CleanupAsync()
    {
        _cts?.Cancel();
        _audioQueueReady = false;

        await CleanupPipesAsync();

        _videoStream = null; // Stream is owned by the process

        _audioPipe?.Dispose();
        _audioPipe = null;

        _videoFrameQueue?.Dispose();
        _videoFrameQueue = null;

        _audioSampleQueue?.Dispose();
        _audioSampleQueue = null;

        _ffmpegProcess?.Dispose();
        _ffmpegProcess = null;

        _cts?.Dispose();
        _cts = null;
    }

    private byte[] GetFrameBuffer(int size)
    {
        if (_frameBufferPool.TryTake(out var buffer) && buffer.Length >= size)
        {
            return buffer;
        }
        return new byte[size];
    }

    private void ReturnFrameBuffer(byte[] buffer)
    {
        if (_frameBufferPool.Count < MaxPoolSize)
        {
            _frameBufferPool.Add(buffer);
        }
    }

    /// <summary>
    /// Drains and discards queued frames/samples when FFmpeg crashes.
    /// Prevents queue from filling indefinitely.
    /// </summary>
    private void DrainQueueOnCrash()
    {
        var videoQueue = _videoFrameQueue;
        var audioQueue = _audioSampleQueue;

        int videoDrained = 0, audioDrained = 0;

        if (videoQueue != null)
        {
            while (videoQueue.TryTake(out var frame, 0))
            {
                ReturnFrameBuffer(frame);
                videoDrained++;
            }
        }

        if (audioQueue != null)
        {
            while (audioQueue.TryTake(out _, 0))
            {
                audioDrained++;
            }
        }

        if (videoDrained > 0 || audioDrained > 0)
        {
            Logger.Log($"Drained queues after crash: {videoDrained} video frames, {audioDrained} audio samples");
        }
    }

    public void Dispose()
    {
        lock (_disposeLock)
        {
            if (_isDisposed) return;
            _isDisposed = true;
        }

        // Don't block with GetAwaiter().GetResult() - it can deadlock
        // Instead, do synchronous cleanup for critical resources
        try
        {
            _isEncoding = false;
            _audioQueueReady = false;
            _cts?.Cancel();

            // Complete the queues to unblock any waiting threads
            try { _videoFrameQueue?.CompleteAdding(); }
            catch (Exception ex) { Logger.Log($"Error completing video queue: {ex.Message}"); }

            try { _audioSampleQueue?.CompleteAdding(); }
            catch (Exception ex) { Logger.Log($"Error completing audio queue: {ex.Message}"); }

            // Give writer tasks a brief chance to finish
            var videoTask = _videoWriterTask;
            var audioTask = _audioWriterTask;
            if (videoTask != null || audioTask != null)
            {
                Task.WhenAny(
                    Task.WhenAll(new[] { videoTask, audioTask }.Where(t => t != null)!),
                    Task.Delay(1000)
                ).Wait();
            }

            // Close pipes
            try { _ffmpegProcess?.StandardInput.Close(); }
            catch (Exception ex) { Logger.Log($"Error closing FFmpeg stdin: {ex.Message}"); }

            try { _audioPipe?.Close(); }
            catch (Exception ex) { Logger.Log($"Error closing audio pipe: {ex.Message}"); }

            // Give FFmpeg a brief moment to exit gracefully
            var process = _ffmpegProcess;
            if (process != null && !process.HasExited)
            {
                if (!process.WaitForExit(2000))
                {
                    try { process.Kill(); }
                    catch (Exception ex) { Logger.Log($"Error killing FFmpeg process: {ex.Message}"); }
                }
            }

            // Dispose resources
            _audioPipe?.Dispose();
            _videoFrameQueue?.Dispose();
            _audioSampleQueue?.Dispose();
            _ffmpegProcess?.Dispose();
            _cts?.Dispose();

            Logger.Log("FFmpegEncoderService disposed");
        }
        catch (Exception ex)
        {
            Logger.Log($"Error during FFmpegEncoderService disposal: {ex.Message}");
        }
    }

    /// <summary>
    /// Async dispose for proper cleanup without blocking.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        lock (_disposeLock)
        {
            if (_isDisposed) return;
            _isDisposed = true;
        }

        await StopEncodingAsync();
        Logger.Log("FFmpegEncoderService disposed (async)");
    }
}

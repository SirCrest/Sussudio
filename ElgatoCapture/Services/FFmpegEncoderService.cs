using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using ElgatoCapture.Models;
using Windows.Graphics.Imaging;

namespace ElgatoCapture.Services;

/// <summary>
/// FFmpeg-based video encoder service for guaranteed CFR output.
/// Pipes raw video frames and audio samples to FFmpeg subprocess.
/// </summary>
public class FFmpegEncoderService : IDisposable
{
    private Process? _ffmpegProcess;
    private NamedPipeServerStream? _videoPipe;
    private NamedPipeServerStream? _audioPipe;
    private BlockingCollection<byte[]>? _videoFrameQueue;
    private BlockingCollection<byte[]>? _audioSampleQueue;
    private CancellationTokenSource? _cts;
    private Task? _videoWriterTask;
    private Task? _audioWriterTask;
    private Task? _stderrReaderTask;
    private bool _isEncoding;
    private readonly string _ffmpegPath;

    // Frame buffer pool to reduce GC pressure
    private readonly ConcurrentBag<byte[]> _frameBufferPool = new();
    private const int MaxQueueSize = 120; // ~2 seconds at 60fps
    private const int MaxPoolSize = 10;

    public event EventHandler<string>? StatusChanged;
    public event EventHandler<string>? ErrorOccurred;
    public event EventHandler<ulong>? FrameEncoded;

    public bool IsEncoding => _isEncoding;
    public ulong EncodedFrameCount { get; private set; }

    public FFmpegEncoderService()
    {
        // Look for FFmpeg in multiple locations
        _ffmpegPath = FindFFmpegPath();
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
                catch { }
            }
            else if (File.Exists(path))
            {
                return path;
            }
        }

        return "ffmpeg.exe"; // Fallback, will fail gracefully if not found
    }

    public async Task StartEncodingAsync(CaptureSettings settings, string outputPath)
    {
        if (_isEncoding)
        {
            throw new InvalidOperationException("Encoding already in progress");
        }

        Logger.Log("=== FFmpeg Encoder Starting ===");
        Logger.Log($"FFmpeg path: {_ffmpegPath}");
        Logger.Log($"Output: {outputPath}");
        Logger.Log($"Settings: {settings.Width}x{settings.Height}@{settings.FrameRate}fps, Format={settings.Format}, Quality={settings.Quality}");

        _cts = new CancellationTokenSource();
        _videoFrameQueue = new BlockingCollection<byte[]>(MaxQueueSize);
        _audioSampleQueue = new BlockingCollection<byte[]>(MaxQueueSize * 10); // Audio has more frequent, smaller samples
        EncodedFrameCount = 0;

        // Create unique pipe names
        var pipeName = $"elgatocapture_{Guid.NewGuid():N}";
        var videoPipeName = $"{pipeName}_video";
        var audioPipeName = $"{pipeName}_audio";

        // Create named pipes
        _videoPipe = new NamedPipeServerStream(videoPipeName, PipeDirection.Out, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
        _audioPipe = new NamedPipeServerStream(audioPipeName, PipeDirection.Out, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);

        // Build FFmpeg command
        var ffmpegArgs = BuildFFmpegArguments(settings, outputPath, videoPipeName, audioPipeName);
        Logger.Log($"FFmpeg arguments: {ffmpegArgs}");

        // Start FFmpeg process
        var startInfo = new ProcessStartInfo
        {
            FileName = _ffmpegPath,
            Arguments = ffmpegArgs,
            UseShellExecute = false,
            RedirectStandardInput = false, // Using named pipes instead
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

            // Start stderr reader task (for logging FFmpeg output)
            _stderrReaderTask = Task.Run(() => ReadStderrAsync(_cts.Token), _cts.Token);

            // Wait for FFmpeg to connect to pipes
            Logger.Log("Waiting for FFmpeg to connect to video pipe...");
            var videoConnectTask = _videoPipe.WaitForConnectionAsync(_cts.Token);
            var audioConnectTask = _audioPipe.WaitForConnectionAsync(_cts.Token);

            // Use timeout to detect connection issues
            var connectTimeout = Task.Delay(10000, _cts.Token);
            var videoResult = await Task.WhenAny(videoConnectTask, connectTimeout);
            if (videoResult == connectTimeout)
            {
                throw new TimeoutException("FFmpeg failed to connect to video pipe within timeout");
            }

            Logger.Log("Video pipe connected, waiting for audio pipe...");

            var audioResult = await Task.WhenAny(audioConnectTask, connectTimeout);
            if (audioResult == connectTimeout)
            {
                throw new TimeoutException("FFmpeg failed to connect to audio pipe within timeout");
            }

            Logger.Log("Audio pipe connected");

            // Start writer tasks
            _videoWriterTask = Task.Run(() => WriteVideoFramesAsync(_cts.Token), _cts.Token);
            _audioWriterTask = Task.Run(() => WriteAudioSamplesAsync(_cts.Token), _cts.Token);

            _isEncoding = true;
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

    private string BuildFFmpegArguments(CaptureSettings settings, string outputPath, string videoPipeName, string audioPipeName)
    {
        // Get encoder and quality settings
        var (videoCodec, qualityArgs) = GetEncoderSettings(settings);

        // Frame size in bytes (BGRA = 4 bytes per pixel)
        var frameSize = settings.Width * settings.Height * 4;

        // Build arguments
        // Note: Using -thread_queue_size to prevent buffer issues
        var args = $"-y " +
                   // Video input from named pipe
                   $"-thread_queue_size 512 " +
                   $"-f rawvideo " +
                   $"-pixel_format bgra " +
                   $"-video_size {settings.Width}x{settings.Height} " +
                   $"-framerate {settings.FrameRate} " +
                   $"-i \"\\\\.\\pipe\\{videoPipeName}\" " +
                   // Audio input from named pipe
                   $"-thread_queue_size 1024 " +
                   $"-f s16le " +
                   $"-ar 48000 " +
                   $"-ac 2 " +
                   $"-i \"\\\\.\\pipe\\{audioPipeName}\" " +
                   // Video encoding with CFR
                   $"-c:v {videoCodec} " +
                   $"{qualityArgs} " +
                   $"-r {settings.FrameRate} " + // Force output frame rate (CFR)
                   $"-pix_fmt yuv420p " +
                   // Audio encoding
                   $"-c:a aac " +
                   $"-b:a 192k " +
                   // Output options
                   $"-shortest " + // End when shortest input ends
                   $"-movflags +faststart " + // Enable fast start for MP4
                   $"\"{outputPath}\"";

        return args;
    }

    private (string codec, string qualityArgs) GetEncoderSettings(CaptureSettings settings)
    {
        string codec;
        string qualityArgs;

        // Determine codec based on format
        switch (settings.Format)
        {
            case RecordingFormat.HevcMp4:
                codec = "libx265";
                break;
            case RecordingFormat.UncompressedAvi:
                // For "uncompressed", use very high bitrate H.264
                codec = "libx264";
                break;
            default:
                codec = "libx264";
                break;
        }

        // Determine quality settings
        if (settings.Quality == VideoQuality.Custom)
        {
            // Use CBR with user-specified bitrate
            var bitrate = (int)(settings.CustomBitrateMbps * 1000); // kbps
            qualityArgs = $"-b:v {bitrate}k -maxrate {bitrate}k -bufsize {bitrate * 2}k";
        }
        else
        {
            // Use CRF for quality-based encoding
            var crf = settings.Quality switch
            {
                VideoQuality.Low => 28,
                VideoQuality.Medium => 23,
                VideoQuality.High => 18,
                VideoQuality.VeryHigh => 15,
                VideoQuality.Lossless => 0,
                _ => 23 // Auto
            };

            // For lossless, use different preset
            if (settings.Quality == VideoQuality.Lossless)
            {
                qualityArgs = $"-preset ultrafast -crf 0";
            }
            else
            {
                // Add x264/x265 specific options for CFR
                var cfr_opt = codec == "libx264" ? "-x264opts force-cfr=1" : "";
                qualityArgs = $"-preset fast -crf {crf} {cfr_opt}";
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

        try
        {
            // Get or create buffer
            var buffer = GetFrameBuffer((int)(frame.PixelWidth * frame.PixelHeight * 4));

            // Copy frame data to buffer
            frame.CopyToBuffer(buffer.AsBuffer());

            // Try to add to queue (non-blocking)
            if (!_videoFrameQueue.TryAdd(buffer, 0))
            {
                // Queue full - drop oldest frame to prevent memory buildup
                if (_videoFrameQueue.TryTake(out var oldBuffer))
                {
                    ReturnFrameBuffer(oldBuffer);
                    Logger.Log("Warning: Dropped video frame (queue full)");
                }
                _videoFrameQueue.TryAdd(buffer, 0);
            }
        }
        catch (Exception ex)
        {
            Logger.LogException(ex);
        }
    }

    /// <summary>
    /// Enqueue audio samples for encoding.
    /// </summary>
    public void EnqueueAudioSamples(byte[] samples)
    {
        if (!_isEncoding || _audioSampleQueue == null || _cts?.IsCancellationRequested == true)
        {
            return;
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
            while (!ct.IsCancellationRequested && _videoPipe != null && _videoFrameQueue != null)
            {
                try
                {
                    // Block until frame available or cancellation
                    if (_videoFrameQueue.TryTake(out var frameData, 100, ct))
                    {
                        await _videoPipe.WriteAsync(frameData, 0, frameData.Length, ct);
                        EncodedFrameCount++;
                        FrameEncoded?.Invoke(this, EncodedFrameCount);

                        // Return buffer to pool
                        ReturnFrameBuffer(frameData);
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
            Logger.Log($"Video pipe closed: {ex.Message}");
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
            while (!ct.IsCancellationRequested && _audioPipe != null && _audioSampleQueue != null)
            {
                try
                {
                    // Block until samples available or cancellation
                    if (_audioSampleQueue.TryTake(out var audioData, 100, ct))
                    {
                        await _audioPipe.WriteAsync(audioData, 0, audioData.Length, ct);
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
                if (line != null)
                {
                    Logger.Log($"[FFmpeg] {line}");

                    // Check for common errors
                    if (line.Contains("Error") || line.Contains("error"))
                    {
                        ErrorOccurred?.Invoke(this, line);
                    }
                }
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            Logger.LogException(ex);
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

        // Signal cancellation
        _cts?.Cancel();

        // Complete the queues (signals writers to finish)
        _videoFrameQueue?.CompleteAdding();
        _audioSampleQueue?.CompleteAdding();

        // Wait for writer tasks to finish (with timeout)
        var tasksToWait = new List<Task>();
        if (_videoWriterTask != null) tasksToWait.Add(_videoWriterTask);
        if (_audioWriterTask != null) tasksToWait.Add(_audioWriterTask);
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
            if (_videoPipe != null)
            {
                await _videoPipe.FlushAsync();
                _videoPipe.Close();
            }
        }
        catch { }

        try
        {
            if (_audioPipe != null)
            {
                await _audioPipe.FlushAsync();
                _audioPipe.Close();
            }
        }
        catch { }
    }

    private async Task CleanupAsync()
    {
        _cts?.Cancel();

        await CleanupPipesAsync();

        _videoPipe?.Dispose();
        _videoPipe = null;

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

    public void Dispose()
    {
        StopEncodingAsync().GetAwaiter().GetResult();
    }
}

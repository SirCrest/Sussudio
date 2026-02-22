using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Channels;
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
    private Channel<VideoFramePacket>? _videoFrameQueue;
    private Channel<AudioSamplePacket>? _audioSampleQueue;
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
    private static Task<HdrArgumentSupport>? _hdrArgumentProbeTask;
    private static readonly object _hdrArgumentProbeLock = new();
    private HdrArgumentSupport _hdrArgumentSupport = HdrArgumentSupport.Unknown;
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
    private VideoFrameDropPolicy _videoDropPolicy = VideoFrameDropPolicy.DropOldest;
    private string _audioPipeName = string.Empty;
    private int _videoQueueDepth;
    private int _audioQueueDepth;
    private long _videoDropsQueueSaturated;
    private long _videoDropsBacklogEviction;
    private long _audioDropsQueueSaturated;
    private long _audioDropsBacklogEviction;
    private int? _lastExitCode;
    private bool _lastStopTimedOut;
    private bool _usesDirectShowInput;
    private string _activeInputPixelFormat = "nv12";
    private string _activeOutputPixelFormat = "yuv420p";
    private string _activeVideoCodec = string.Empty;
    private string _activeVideoProfile = string.Empty;
    private bool? _activeTenBitPipelineConfirmed;
    private bool _mfReadwriteDisableConverters;
    private readonly object _observedFormatSync = new();
    private string? _firstObservedFramePixelFormat;
    private string? _latestObservedFramePixelFormat;
    private string? _latestObservedSurfaceFormat;
    private long _observedP010FrameCount;
    private long _observedNv12FrameCount;
    private long _observedOtherFrameCount;
    private long _observedP010BitDepthSampleCount;
    private long _observedP010Low2BitNonZeroSampleCount;
    private long _observedP010PaddingNonZeroSampleCount;
    private const int ComInteropErrorLogIntervalMs = 2000;
    private long _lastComInteropErrorLogTick;
    private long _suppressedComInteropErrorCount;

    // Frame buffer pool to reduce GC pressure
    private const int MaxQueueSize = 360;
    private const int AudioQueueCapacity = MaxQueueSize * 10;
    private const string AudioPipePrefix = "ElgatoCaptureAudio";
    private const int WriterDrainTimeoutMs = 1000;
    private const int WriterCancelGraceMs = 1000;
    private const int AudioPipeConnectTimeoutMs = 15000;
    private const int AudioPipeConnectAttempts = 3;
    private const int DirectShowHdrProbeMaxAttempts = 3;
    private const int DirectShowHdrProbeRetryBaseDelayMs = 350;
    private static readonly Regex VideoStreamProbeRegex = new(@"Video:", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex DropCounterRegex = new(@"drop=\s*(\d+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex ProgressSizeRegex = new(@"size=\s*(\d+)\s*([kmg]?i?b)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex ProgressBitrateRegex = new(@"bitrate=\s*([\d\.]+)\s*([kmg]?bits/s)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly ConcurrentDictionary<string, DateTimeOffset> DirectShowHdrProbeCache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly TimeSpan DirectShowHdrProbeCacheTtl = TimeSpan.FromMinutes(2);
    private bool _directShowHdrRequested;
    private bool _directShowInputFormatVerified;
    private bool _directShowInputSectionActive;
    private bool _directShowIngressViolationRaised;
    private long _lastReportedOutputBytes;
    private double _lastReportedOutputBitrateBps;

    public event EventHandler<string>? StatusChanged;
    public event EventHandler<string>? ErrorOccurred;
    public event EventHandler<ulong>? FrameEncoded;
    public event EventHandler<string>? IngressViolationDetected;

    public bool IsEncoding => _isEncoding;
    private ulong _encodedFrameCount;
    public ulong EncodedFrameCount => Interlocked.Read(ref _encodedFrameCount);
    public string FfmpegPath => _ffmpegPath;
    public int VideoQueueCount => Volatile.Read(ref _videoQueueDepth);
    public int AudioQueueCount => Volatile.Read(ref _audioQueueDepth);
    public long DroppedVideoFrames => Interlocked.Read(ref _droppedVideoFrames);
    public long VideoDropsQueueSaturated => Interlocked.Read(ref _videoDropsQueueSaturated);
    public long VideoDropsBacklogEviction => Interlocked.Read(ref _videoDropsBacklogEviction);
    public long AudioDropsQueueSaturated => Interlocked.Read(ref _audioDropsQueueSaturated);
    public long AudioDropsBacklogEviction => Interlocked.Read(ref _audioDropsBacklogEviction);
    public int? LastExitCode => _lastExitCode;
    public bool LastStopTimedOut => _lastStopTimedOut;
    public string ActiveInputPixelFormat => _activeInputPixelFormat;
    public string ActiveOutputPixelFormat => _activeOutputPixelFormat;
    public string ActiveVideoCodec => _activeVideoCodec;
    public string ActiveVideoProfile => _activeVideoProfile;
    public bool? ActiveTenBitPipelineConfirmed => _activeTenBitPipelineConfirmed;
    public bool MfReadwriteDisableConverters => _mfReadwriteDisableConverters;
    public long LastReportedOutputBytes => Interlocked.Read(ref _lastReportedOutputBytes);
    public double LastReportedOutputBitrateBps => Volatile.Read(ref _lastReportedOutputBitrateBps);
    public string? FirstObservedFramePixelFormat
    {
        get
        {
            lock (_observedFormatSync)
            {
                return _firstObservedFramePixelFormat;
            }
        }
    }

    public string? LatestObservedFramePixelFormat
    {
        get
        {
            lock (_observedFormatSync)
            {
                return _latestObservedFramePixelFormat;
            }
        }
    }

    public string? LatestObservedSurfaceFormat
    {
        get
        {
            lock (_observedFormatSync)
            {
                return _latestObservedSurfaceFormat;
            }
        }
    }

    public long ObservedP010FrameCount => Interlocked.Read(ref _observedP010FrameCount);
    public long ObservedNv12FrameCount => Interlocked.Read(ref _observedNv12FrameCount);
    public long ObservedOtherFrameCount => Interlocked.Read(ref _observedOtherFrameCount);
    public long ObservedP010BitDepthSampleCount => Interlocked.Read(ref _observedP010BitDepthSampleCount);
    public double ObservedP010Low2BitNonZeroPercent
    {
        get
        {
            var sampleCount = Interlocked.Read(ref _observedP010BitDepthSampleCount);
            if (sampleCount <= 0)
            {
                return 0;
            }

            var low2NonZero = Interlocked.Read(ref _observedP010Low2BitNonZeroSampleCount);
            return (double)low2NonZero / sampleCount * 100.0;
        }
    }

    public bool? ObservedP010Likely8BitUpscaled
    {
        get
        {
            var sampleCount = Interlocked.Read(ref _observedP010BitDepthSampleCount);
            if (sampleCount < 256)
            {
                return null;
            }

            return ObservedP010Low2BitNonZeroPercent < 0.50;
        }
    }

    private readonly record struct VideoFramePacket(byte[] Buffer, int Length);
    private readonly record struct AudioSamplePacket(byte[] Buffer, int Length);
    private readonly record struct HdrArgumentSupport(
        bool SupportsP010Input,
        bool SupportsColorPrimaries,
        bool SupportsColorTransfer,
        bool SupportsColorSpace,
        bool SupportsColorRange,
        bool SupportsMasterDisplay,
        bool SupportsMaxCll)
    {
        public static HdrArgumentSupport Unknown => new(
            SupportsP010Input: true,
            SupportsColorPrimaries: true,
            SupportsColorTransfer: true,
            SupportsColorSpace: true,
            SupportsColorRange: true,
            SupportsMasterDisplay: false,
            SupportsMaxCll: false);
    }

    public FFmpegEncoderService()
    {
        // Look for FFmpeg in multiple locations
        _ffmpegPath = FindFFmpegPath();
    }

    public static string ResolveFfmpegPath()
    {
        return FindFFmpegPath();
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
        _audioSampleQueue = CreateAudioSampleQueue();
        Interlocked.Exchange(ref _audioQueueDepth, 0);
        _audioQueueReady = true;
    }

    private static Channel<AudioSamplePacket> CreateAudioSampleQueue()
    {
        return Channel.CreateBounded<AudioSamplePacket>(new BoundedChannelOptions(AudioQueueCapacity)
        {
            SingleReader = false,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.Wait
        });
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

    private static Task<HdrArgumentSupport> GetHdrArgumentSupportAsync()
    {
        lock (_hdrArgumentProbeLock)
        {
            _hdrArgumentProbeTask ??= ProbeHdrArgumentSupportAsync();
            return _hdrArgumentProbeTask;
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

    private static async Task<HdrArgumentSupport> ProbeHdrArgumentSupportAsync()
    {
        var ffmpegPath = FindFFmpegPath();
        try
        {
            var fullHelpTask = RunProbeCommandAsync(ffmpegPath, "-hide_banner -h full");
            var pixFmtTask = RunProbeCommandAsync(ffmpegPath, "-hide_banner -pix_fmts");
            var masterDisplayTask = SupportsFfmpegOptionAsync(
                ffmpegPath,
                "master_display",
                "-master_display \"G(13250,34500)B(7500,3000)R(34000,16000)L(10000000,1)\"");
            var maxCllTask = SupportsFfmpegOptionAsync(
                ffmpegPath,
                "max_cll",
                "-max_cll 1000,400");
            await Task.WhenAll(fullHelpTask, pixFmtTask, masterDisplayTask, maxCllTask).ConfigureAwait(false);

            var fullHelp = fullHelpTask.Result;
            var pixFmts = pixFmtTask.Result;
            var supportsP010 = pixFmts.Contains("p010", StringComparison.OrdinalIgnoreCase);
            var supportsColorPrimaries = fullHelp.Contains("color_primaries", StringComparison.OrdinalIgnoreCase);
            var supportsColorTransfer = fullHelp.Contains("color_trc", StringComparison.OrdinalIgnoreCase);
            var supportsColorSpace = fullHelp.Contains("colorspace", StringComparison.OrdinalIgnoreCase);
            var supportsColorRange = fullHelp.Contains("color_range", StringComparison.OrdinalIgnoreCase);
            var supportsMasterDisplay = masterDisplayTask.Result;
            var supportsMaxCll = maxCllTask.Result;

            var support = new HdrArgumentSupport(
                SupportsP010Input: supportsP010,
                SupportsColorPrimaries: supportsColorPrimaries,
                SupportsColorTransfer: supportsColorTransfer,
                SupportsColorSpace: supportsColorSpace,
                SupportsColorRange: supportsColorRange,
                SupportsMasterDisplay: supportsMasterDisplay,
                SupportsMaxCll: supportsMaxCll);

            Logger.Log(
                $"HDR argument support: p010={support.SupportsP010Input}, color_primaries={support.SupportsColorPrimaries}, " +
                $"color_trc={support.SupportsColorTransfer}, colorspace={support.SupportsColorSpace}, " +
                $"color_range={support.SupportsColorRange}, master_display={support.SupportsMasterDisplay}, max_cll={support.SupportsMaxCll}");

            return support;
        }
        catch (Exception ex)
        {
            Logger.Log($"HDR argument support probe failed: {ex.Message}");
            return HdrArgumentSupport.Unknown;
        }
    }

    private static async Task<bool> SupportsFfmpegOptionAsync(string ffmpegPath, string optionName, string optionArguments)
    {
        var probeArgs =
            "-hide_banner -loglevel error " +
            "-f lavfi -i color=size=16x16:rate=1:color=black " +
            "-frames:v 1 " +
            $"{optionArguments} " +
            "-f null -";

        var result = await RunProbeCommandWithExitCodeAsync(ffmpegPath, probeArgs).ConfigureAwait(false);
        var output = result.Output ?? string.Empty;
        if (output.Contains($"Unrecognized option '{optionName}'", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return result.ExitCode == 0;
    }

    private static async Task<string> RunProbeCommandAsync(string fileName, string arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo);
        if (process == null)
        {
            return string.Empty;
        }

        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync().ConfigureAwait(false);
        return (await stdoutTask.ConfigureAwait(false)) + Environment.NewLine + (await stderrTask.ConfigureAwait(false));
    }

    private static async Task<(string Output, int ExitCode)> RunProbeCommandWithExitCodeAsync(string fileName, string arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo);
        if (process == null)
        {
            return (string.Empty, -1);
        }

        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync().ConfigureAwait(false);
        var output = (await stdoutTask.ConfigureAwait(false)) + Environment.NewLine + (await stderrTask.ConfigureAwait(false));
        return (output, process.ExitCode);
    }

    public async Task StartEncodingAsync(CaptureSettings settings, string outputPath, string? audioDeviceName = null, double? actualFrameRate = null, string? actualFrameRateArg = null, uint? actualWidth = null, uint? actualHeight = null, string inputPixelFormat = "bgra", bool hdrPipelineActive = false, RecordingPipelineOptions? pipelineOptions = null)
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
        var options = pipelineOptions ?? new RecordingPipelineOptions();
        var videoQueueSize = Math.Clamp(options.ResolveVideoQueueCapacity(effectiveFrameRate), 1, MaxQueueSize);
        _videoDropPolicy = options.VideoDropPolicy;
        Logger.Log($"Settings: {settings.Width}x{settings.Height}@{effectiveFrameRate:0.###}fps, Format={settings.Format}, Quality={settings.Quality}");
        Logger.Log($"Video queue: size={videoQueueSize}, dropPolicy={_videoDropPolicy}, targetLatencyMs={options.TargetVideoLatencyMs}");
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
        _hdrArgumentSupport = await GetHdrArgumentSupportAsync();
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
        _lastExitCode = null;
        _lastStopTimedOut = false;
        _usesDirectShowInput = false;
        _directShowHdrRequested = false;
        _directShowInputFormatVerified = false;
        _directShowInputSectionActive = false;
        Interlocked.Exchange(ref _droppedVideoFrames, 0);
        Interlocked.Exchange(ref _videoDropsQueueSaturated, 0);
        Interlocked.Exchange(ref _videoDropsBacklogEviction, 0);
        Interlocked.Exchange(ref _audioDropsQueueSaturated, 0);
        Interlocked.Exchange(ref _audioDropsBacklogEviction, 0);
        ResetObservedFormatTelemetry();
        _videoFrameQueue = Channel.CreateBounded<VideoFramePacket>(new BoundedChannelOptions(videoQueueSize)
        {
            SingleReader = false,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.Wait
        });
        Interlocked.Exchange(ref _videoQueueDepth, 0);
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
            var createdAudioQueue = false;
            if (_audioSampleQueue == null)
            {
                _audioSampleQueue = CreateAudioSampleQueue();
                createdAudioQueue = true;
            }

            if (createdAudioQueue)
            {
                Interlocked.Exchange(ref _audioQueueDepth, 0);
            }
            _audioQueueReady = true;
            _audioPipeName = CreateSessionAudioPipeName();
        }
        else if (_audioSampleQueue != null)
        {
            _audioSampleQueue.Writer.TryComplete();
            while (_audioSampleQueue.Reader.TryRead(out var bufferedSample))
            {
                ReturnAudioBuffer(bufferedSample.Buffer);
            }
            _audioSampleQueue = null;
            Interlocked.Exchange(ref _audioQueueDepth, 0);
            _audioPipeName = string.Empty;
        }

        // Create named pipe for audio BEFORE starting FFmpeg
        if (_useAudioPipe)
        {
            Logger.Log($"Creating audio named pipe: {_audioPipeName}");
            _audioPipe = new NamedPipeServerStream(
                _audioPipeName,
                PipeDirection.Out,
                1, // maxNumberOfServerInstances
                PipeTransmissionMode.Byte,
                PipeOptions.Asynchronous);
            Logger.Log("Audio named pipe created");
        }

        // Build FFmpeg command (uses stdin for video, named pipe for audio)
        var ffmpegArgs = BuildFFmpegArguments(settings, outputPath, _useAudioPipe, frameRateArg, effectiveWidth, effectiveHeight, inputPixelFormat, hdrPipelineActive, _audioPipeName);
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

    public async Task StartDirectShowEncodingAsync(
        CaptureSettings settings,
        string outputPath,
        string videoDeviceName,
        string? audioDeviceName = null,
        double? actualFrameRate = null,
        string? actualFrameRateArg = null,
        uint? actualWidth = null,
        uint? actualHeight = null,
        bool hdrPipelineActive = false,
        bool requireHdrP010Ingress = true)
    {
        if (_isEncoding)
        {
            throw new InvalidOperationException("Encoding already in progress");
        }

        if (string.IsNullOrWhiteSpace(videoDeviceName))
        {
            throw new InvalidOperationException("DirectShow video device name is required.");
        }

        Logger.Log("=== FFmpeg Encoder Starting (DirectShow) ===");
        Logger.Log($"FFmpeg path: {_ffmpegPath}");
        Logger.Log($"DirectShow video device: {videoDeviceName}");
        Logger.Log($"Output: {outputPath}");

        var effectiveFrameRate = actualFrameRate ?? settings.FrameRate;
        var frameRateArg = !string.IsNullOrWhiteSpace(actualFrameRateArg)
            ? actualFrameRateArg
            : FormatFrameRateArg(effectiveFrameRate);
        var effectiveWidth = actualWidth ?? settings.Width;
        var effectiveHeight = actualHeight ?? settings.Height;
        Logger.Log($"Settings: {effectiveWidth}x{effectiveHeight}@{effectiveFrameRate:0.###}fps, Format={settings.Format}, Quality={settings.Quality}");
        Logger.Log($"FFmpeg frame rate arg: {frameRateArg}");

        var hdrRequested = hdrPipelineActive &&
                           settings.HdrEnabled &&
                           settings.HdrOutputMode == HdrOutputMode.Hdr10Pq;

        _startupStopwatch.Restart();
        Logger.LogVerbose("Encoder probe: awaiting cached result");
        _encoderSupport = await GetEncoderSupportAsync();
        _hdrArgumentSupport = await GetHdrArgumentSupportAsync();
        Logger.LogVerbose($"Encoder probe complete in {_startupStopwatch.ElapsedMilliseconds} ms");

        _cts = new CancellationTokenSource();
        _stderrClosed = false;
        _lastExitCode = null;
        _lastStopTimedOut = false;
        _usesDirectShowInput = true;
        _mfReadwriteDisableConverters = false;
        _directShowHdrRequested = hdrRequested;
        _directShowInputFormatVerified = false;
        _directShowInputSectionActive = false;
        _directShowIngressViolationRaised = false;
        _audioQueueReady = false;
        _useAudioPipe = false;
        _audioPipeName = string.Empty;
        _videoStream = null;
        _videoFrameQueue = null;
        _audioSampleQueue = null;
        Interlocked.Exchange(ref _droppedVideoFrames, 0);
        Interlocked.Exchange(ref _videoQueueDepth, 0);
        Interlocked.Exchange(ref _audioQueueDepth, 0);
        Interlocked.Exchange(ref _videoDropsQueueSaturated, 0);
        Interlocked.Exchange(ref _videoDropsBacklogEviction, 0);
        Interlocked.Exchange(ref _audioDropsQueueSaturated, 0);
        Interlocked.Exchange(ref _audioDropsBacklogEviction, 0);
        Interlocked.Exchange(ref _encodedFrameCount, 0);
        ResetObservedFormatTelemetry();

        if (hdrRequested)
        {
            _activeInputPixelFormat = "p010le";
            Logger.Log("HDR_INGEST_PIXEL_FORMAT av_pix_fmt=AV_PIX_FMT_P010LE source=DirectShow");
            Logger.Log("HDR_CAPTURE_NEGOTIATION mf_subtype=P010(MFVideoFormat_P010) mf_readwrite_disable_converters=false source=DirectShow");
            if (requireHdrP010Ingress)
            {
                var probeKey = $"{videoDeviceName}|{effectiveWidth}x{effectiveHeight}|{frameRateArg}";
                var skipProbe = DirectShowHdrProbeCache.TryGetValue(probeKey, out var cachedAt) &&
                                DateTimeOffset.UtcNow - cachedAt <= DirectShowHdrProbeCacheTtl;
                if (!skipProbe)
                {
                    var probeResult = await ProbeDirectShowHdrIngressWithRetryAsync(
                        videoDeviceName,
                        frameRateArg,
                        effectiveWidth,
                        effectiveHeight).ConfigureAwait(false);
                    if (!probeResult.Success)
                    {
                        throw new InvalidOperationException($"HDR ingress verification failed: {probeResult.Reason}");
                    }

                    DirectShowHdrProbeCache[probeKey] = DateTimeOffset.UtcNow;
                    Logger.Log($"DirectShow HDR ingress verified: {probeResult.Reason}");
                }
                else
                {
                    Logger.Log("DirectShow HDR ingress probe reused recent successful evidence.");
                }
            }
        }
        else
        {
            _activeInputPixelFormat = "nv12";
            Logger.Log("HDR_INGEST_PIXEL_FORMAT av_pix_fmt=AV_PIX_FMT_NV12 source=DirectShow");
        }

        var ffmpegArgs = BuildDirectShowFFmpegArguments(
            settings,
            outputPath,
            videoDeviceName,
            audioDeviceName,
            frameRateArg,
            effectiveWidth,
            effectiveHeight,
            hdrPipelineActive);
        Logger.Log($"FFmpeg arguments: {ffmpegArgs}");

        var startInfo = new ProcessStartInfo
        {
            FileName = _ffmpegPath,
            Arguments = ffmpegArgs,
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = false,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        try
        {
            _ffmpegProcess = Process.Start(startInfo);
            if (_ffmpegProcess == null)
            {
                throw new InvalidOperationException("Failed to start FFmpeg DirectShow process.");
            }

            Logger.Log($"FFmpeg process started (PID: {_ffmpegProcess.Id})");
            _stderrReaderTask = Task.Run(() => ReadStderrAsync(_cts.Token), _cts.Token);
            _videoWriterTask = Task.CompletedTask;
            _audioWriterTask = Task.CompletedTask;
            _isEncoding = true;
            StatusChanged?.Invoke(this, "Encoding started");
            Logger.Log("=== FFmpeg Encoder Started (DirectShow) ===");
        }
        catch (Exception ex)
        {
            Logger.LogException(ex);
            ErrorOccurred?.Invoke(this, $"Failed to start FFmpeg DirectShow encoding: {ex.Message}");
            await CleanupAsync();
            throw;
        }
    }

    private async Task<(bool Success, string Reason)> ProbeDirectShowHdrIngressWithRetryAsync(
        string videoDeviceName,
        string frameRateArg,
        uint width,
        uint height)
    {
        (bool Success, string Reason) lastResult = (false, "probe-not-run");
        for (var attempt = 1; attempt <= DirectShowHdrProbeMaxAttempts; attempt++)
        {
            lastResult = await ProbeDirectShowHdrIngressAsync(
                videoDeviceName,
                frameRateArg,
                width,
                height).ConfigureAwait(false);

            if (lastResult.Success)
            {
                return lastResult;
            }

            if (attempt >= DirectShowHdrProbeMaxAttempts)
            {
                break;
            }

            var retryDelayMs = DirectShowHdrProbeRetryBaseDelayMs * attempt;
            Logger.Log(
                $"DirectShow HDR ingress probe attempt {attempt}/{DirectShowHdrProbeMaxAttempts} failed: " +
                $"{lastResult.Reason}. Retrying in {retryDelayMs} ms.");
            var cancellationToken = _cts?.Token ?? CancellationToken.None;
            await Task.Delay(retryDelayMs, cancellationToken).ConfigureAwait(false);
        }

        return lastResult;
    }

    private void EnsureHdrColorMetadataSupport()
    {
        var missingOptions = new List<string>(capacity: 4);
        if (!_hdrArgumentSupport.SupportsColorPrimaries)
        {
            missingOptions.Add("color_primaries");
        }

        if (!_hdrArgumentSupport.SupportsColorTransfer)
        {
            missingOptions.Add("color_trc");
        }

        if (!_hdrArgumentSupport.SupportsColorSpace)
        {
            missingOptions.Add("colorspace");
        }

        if (!_hdrArgumentSupport.SupportsColorRange)
        {
            missingOptions.Add("color_range");
        }

        if (missingOptions.Count > 0)
        {
            throw new InvalidOperationException(
                "HDR output requires FFmpeg color metadata options that are unavailable: " +
                string.Join(", ", missingOptions) + ".");
        }
    }

    private void EnsureHdrMasteringMetadataSupport(bool hdrMasterRequested, bool hdrCllRequested)
    {
        var missingOptions = new List<string>(capacity: 2);
        if (hdrMasterRequested && !_hdrArgumentSupport.SupportsMasterDisplay)
        {
            missingOptions.Add("master_display");
        }

        if (hdrCllRequested && !_hdrArgumentSupport.SupportsMaxCll)
        {
            missingOptions.Add("max_cll");
        }

        if (missingOptions.Count > 0)
        {
            throw new InvalidOperationException(
                "HDR mastering metadata was requested but FFmpeg does not support: " +
                string.Join(", ", missingOptions) + ".");
        }
    }

    private string BuildDirectShowFFmpegArguments(
        CaptureSettings settings,
        string outputPath,
        string videoDeviceName,
        string? audioDeviceName,
        string frameRateArg,
        uint effectiveWidth,
        uint effectiveHeight,
        bool hdrPipelineActive)
    {
        var hdrRequested = hdrPipelineActive &&
                           settings.HdrEnabled &&
                           settings.HdrOutputMode == HdrOutputMode.Hdr10Pq;
        var hdrMasterRequested = !string.IsNullOrWhiteSpace(settings.HdrMasterDisplayMetadata);
        var hdrCllRequested = settings.HdrMaxCll > 0 && settings.HdrMaxFall > 0;

        var (videoCodec, qualityArgs) = GetEncoderSettings(settings, hdrRequested, preferSoftwareHevc: false);
        _activeVideoCodec = videoCodec;
        _activeVideoProfile = "sdr";
        _activeTenBitPipelineConfirmed = false;

        if (hdrRequested &&
            videoCodec.Equals("hevc_nvenc", StringComparison.OrdinalIgnoreCase) &&
            (hdrMasterRequested || hdrCllRequested))
        {
            var missingMasterSupport = hdrMasterRequested && !_hdrArgumentSupport.SupportsMasterDisplay;
            var missingCllSupport = hdrCllRequested && !_hdrArgumentSupport.SupportsMaxCll;
            if (missingMasterSupport || missingCllSupport)
            {
                throw new InvalidOperationException(
                    "HDR mastering metadata was requested, but the active FFmpeg/NVENC path does not support " +
                    "master-display/max-cll metadata emission.");
            }
        }

        var escapedVideoDevice = EscapeDshowDeviceName(videoDeviceName);
        var inputArgs = $"-f dshow -rtbufsize 512M -thread_queue_size 2048 " +
                        $"-video_size {effectiveWidth}x{effectiveHeight} " +
                        $"-framerate {frameRateArg} ";
        if (hdrRequested)
        {
            inputArgs += "-pixel_format p010le ";
        }
        inputArgs += $"-i video=\"{escapedVideoDevice}\" ";

        var includeAudio = settings.AudioEnabled && !string.IsNullOrWhiteSpace(audioDeviceName);
        string audioInputArgs;
        string audioCodecArgs;
        if (includeAudio)
        {
            var escapedAudioDevice = EscapeDshowDeviceName(audioDeviceName!);
            audioInputArgs = $"-f dshow -thread_queue_size 1024 -i audio=\"{escapedAudioDevice}\" ";
            audioCodecArgs = "-c:a aac -b:a 320k ";
        }
        else
        {
            audioInputArgs = string.Empty;
            audioCodecArgs = "-an ";
        }

        var outputPixelFormat = videoCodec.Contains("_nvenc", StringComparison.OrdinalIgnoreCase) && !hdrRequested
            ? "nv12"
            : "yuv420p";
        var hdrMetadataArgs = string.Empty;
        if (hdrRequested)
        {
            var isHevcNvenc = videoCodec.Equals("hevc_nvenc", StringComparison.OrdinalIgnoreCase);
            var isLibx265 = videoCodec.Equals("libx265", StringComparison.OrdinalIgnoreCase);
            var isAv1 = videoCodec.Contains("av1", StringComparison.OrdinalIgnoreCase);
            if (!isHevcNvenc && !isLibx265 && !isAv1)
            {
                throw new InvalidOperationException(
                    $"HDR10 output requires HEVC or AV1 10-bit encoding, but resolved codec is '{videoCodec}'.");
            }

            outputPixelFormat = isHevcNvenc ? "p010le" : "yuv420p10le";
            _activeVideoProfile = isAv1 ? "main-10bit" : "main10";
            _activeTenBitPipelineConfirmed = true;
            EnsureHdrColorMetadataSupport();
            if (_hdrArgumentSupport.SupportsColorPrimaries)
            {
                hdrMetadataArgs += "-color_primaries bt2020 ";
            }
            if (_hdrArgumentSupport.SupportsColorTransfer)
            {
                hdrMetadataArgs += "-color_trc smpte2084 ";
            }
            if (_hdrArgumentSupport.SupportsColorSpace)
            {
                hdrMetadataArgs += "-colorspace bt2020nc ";
            }
            if (_hdrArgumentSupport.SupportsColorRange)
            {
                hdrMetadataArgs += "-color_range tv ";
            }

            if (isLibx265)
            {
                var x265Params = new List<string>
                {
                    "hdr-opt=1",
                    "repeat-headers=1",
                    "colorprim=bt2020",
                    "transfer=smpte2084",
                    "colormatrix=bt2020nc"
                };
                if (!string.IsNullOrWhiteSpace(settings.HdrMasterDisplayMetadata))
                {
                    x265Params.Add($"master-display={settings.HdrMasterDisplayMetadata}");
                }
                if (settings.HdrMaxCll > 0 && settings.HdrMaxFall > 0)
                {
                    x265Params.Add($"max-cll={settings.HdrMaxCll},{settings.HdrMaxFall}");
                }

                qualityArgs = $"{qualityArgs} -x265-params \"{string.Join(":", x265Params)}\"";
            }
            else
            {
                if (hdrMasterRequested || hdrCllRequested)
                {
                    Logger.Log(
                        "HDR_STATIC_METADATA " +
                        $"requested_master_display={hdrMasterRequested} " +
                        $"requested_max_cll={hdrCllRequested} " +
                        $"master_display_len={(settings.HdrMasterDisplayMetadata ?? string.Empty).Length} " +
                        $"max_cll={settings.HdrMaxCll} " +
                        $"max_fall={settings.HdrMaxFall}");
                }

                if (hdrMasterRequested)
                {
                    EnsureHdrMasteringMetadataSupport(hdrMasterRequested: true, hdrCllRequested: false);
                    hdrMetadataArgs += $"-master_display \"{settings.HdrMasterDisplayMetadata}\" ";
                }
                if (hdrCllRequested)
                {
                    EnsureHdrMasteringMetadataSupport(hdrMasterRequested: false, hdrCllRequested: true);
                    hdrMetadataArgs += $"-max_cll {settings.HdrMaxCll},{settings.HdrMaxFall} ";
                }
            }
        }

        _activeOutputPixelFormat = outputPixelFormat;
        Logger.Log(
            $"HDR_ENCODER_CONFIG codec={_activeVideoCodec} profile={_activeVideoProfile} " +
            $"input_pix_fmt={_activeInputPixelFormat} output_pix_fmt={_activeOutputPixelFormat} " +
            $"ten_bit_pipeline_confirmed={_activeTenBitPipelineConfirmed}");

        return $"-y -stats " +
               $"{inputArgs}" +
               $"{audioInputArgs}" +
               $"-c:v {videoCodec} " +
               $"{qualityArgs} " +
               $"-r {frameRateArg} " +
               $"-pix_fmt {outputPixelFormat} " +
               $"{hdrMetadataArgs}" +
               $"{audioCodecArgs}" +
               $"-shortest " +
               $"-movflags +faststart " +
               $"\"{outputPath}\"";
    }

    private async Task<(bool Success, string Reason)> ProbeDirectShowHdrIngressAsync(
        string videoDeviceName,
        string frameRateArg,
        uint width,
        uint height)
    {
        var escapedVideoDevice = EscapeDshowDeviceName(videoDeviceName);
        var probeArgs = $"-hide_banner -loglevel info " +
                        $"-f dshow -rtbufsize 256M -video_size {width}x{height} -framerate {frameRateArg} " +
                        $"-pixel_format p010le -i video=\"{escapedVideoDevice}\" " +
                        "-t 2 -frames:v 12 -an -f null NUL";

        var (output, exitCode) = await RunProbeCommandWithExitCodeAsync(_ffmpegPath, probeArgs).ConfigureAwait(false);
        if (exitCode != 0)
        {
            var reason = output
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .TakeLast(3);
            var baseReason = $"probe-exit={exitCode}; {string.Join(" | ", reason)}";
            if (IsLikelyDirectShowOpenFailure(output))
            {
                var diagnostics = await CollectDirectShowOpenFailureDiagnosticsAsync(videoDeviceName).ConfigureAwait(false);
                return (false, $"{baseReason} | {diagnostics}");
            }

            return (false, baseReason);
        }

        if (TryExtractDirectShowInputVideoEvidence(output, out var evidenceLine, out var inputIsP010))
        {
            if (inputIsP010)
            {
                return (true, evidenceLine);
            }

            return (false, $"DirectShow input negotiated non-P010 format: {evidenceLine}");
        }

        return (false, "No DirectShow input video stream evidence was detected in FFmpeg probe output.");
    }

    private async Task<string> CollectDirectShowOpenFailureDiagnosticsAsync(string videoDeviceName)
    {
        try
        {
            var listArgs = "-hide_banner -loglevel info -f dshow -list_devices true -i dummy";
            var (output, exitCode) = await RunProbeCommandWithExitCodeAsync(_ffmpegPath, listArgs).ConfigureAwait(false);
            var entry = FindDirectShowDeviceListEntry(output, videoDeviceName);
            if (entry is not { } resolvedEntry)
            {
                return $"dshow-list-device='{videoDeviceName}' not-found (list-exit={exitCode})";
            }

            return $"dshow-list-device='{resolvedEntry.FriendlyName}' kind={resolvedEntry.Kind} alt='{resolvedEntry.AlternativeName ?? "n/a"}' (list-exit={exitCode})";
        }
        catch (Exception ex)
        {
            return $"dshow-list-diagnostics-failed: {ex.Message}";
        }
    }

    private static bool IsLikelyDirectShowOpenFailure(string ffmpegOutput)
    {
        if (string.IsNullOrWhiteSpace(ffmpegOutput))
        {
            return false;
        }

        return ffmpegOutput.Contains("Error opening input file video=", StringComparison.OrdinalIgnoreCase) ||
               ffmpegOutput.Contains("Error opening input: I/O error", StringComparison.OrdinalIgnoreCase) ||
               ffmpegOutput.Contains("Unable to BindToObject", StringComparison.OrdinalIgnoreCase) ||
               ffmpegOutput.Contains("Could not find video device with name", StringComparison.OrdinalIgnoreCase);
    }

    private static DirectShowDeviceListEntry? FindDirectShowDeviceListEntry(string ffmpegOutput, string videoDeviceName)
    {
        if (string.IsNullOrWhiteSpace(ffmpegOutput) || string.IsNullOrWhiteSpace(videoDeviceName))
        {
            return null;
        }

        var lines = ffmpegOutput.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (!line.Contains($"\"{videoDeviceName}\"", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var kind = "unknown";
            if (line.Contains("(video)", StringComparison.OrdinalIgnoreCase))
            {
                kind = "video";
            }
            else if (line.Contains("(none)", StringComparison.OrdinalIgnoreCase))
            {
                kind = "none";
            }

            string? altName = null;
            if (i + 1 < lines.Length && lines[i + 1].Contains("Alternative name", StringComparison.OrdinalIgnoreCase))
            {
                var altLine = lines[i + 1];
                var quoteStart = altLine.IndexOf('"');
                if (quoteStart >= 0)
                {
                    var quoteEnd = altLine.LastIndexOf('"');
                    if (quoteEnd > quoteStart)
                    {
                        altName = altLine.Substring(quoteStart + 1, quoteEnd - quoteStart - 1);
                    }
                }
            }

            return new DirectShowDeviceListEntry(videoDeviceName, kind, altName);
        }

        return null;
    }

    private readonly record struct DirectShowDeviceListEntry(string FriendlyName, string Kind, string? AlternativeName);

    private static bool TryExtractDirectShowInputVideoEvidence(
        string ffmpegOutput,
        out string evidenceLine,
        out bool inputIsP010)
    {
        evidenceLine = string.Empty;
        inputIsP010 = false;
        var inPrimaryDshowInput = false;

        foreach (var line in ffmpegOutput.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
        {
            var trimmed = line.TrimStart();
            if (trimmed.StartsWith("Input #0, dshow", StringComparison.OrdinalIgnoreCase))
            {
                inPrimaryDshowInput = true;
                continue;
            }

            if (trimmed.StartsWith("Input #", StringComparison.OrdinalIgnoreCase) &&
                !trimmed.StartsWith("Input #0, dshow", StringComparison.OrdinalIgnoreCase))
            {
                inPrimaryDshowInput = false;
                continue;
            }

            if (trimmed.StartsWith("Output #", StringComparison.OrdinalIgnoreCase) ||
                trimmed.StartsWith("Stream mapping:", StringComparison.OrdinalIgnoreCase))
            {
                inPrimaryDshowInput = false;
            }

            if (!inPrimaryDshowInput)
            {
                continue;
            }

            if (!TryParseDirectShowInputVideoLineCore(line, out var isP010))
            {
                continue;
            }

            evidenceLine = line.Trim();
            inputIsP010 = isP010;
            return true;
        }

        return false;
    }

    private void UpdateDirectShowInputSectionState(string line)
    {
        var trimmed = line.TrimStart();
        if (trimmed.StartsWith("Input #0, dshow", StringComparison.OrdinalIgnoreCase))
        {
            _directShowInputSectionActive = true;
            return;
        }

        if (trimmed.StartsWith("Input #", StringComparison.OrdinalIgnoreCase) &&
            !trimmed.StartsWith("Input #0, dshow", StringComparison.OrdinalIgnoreCase))
        {
            _directShowInputSectionActive = false;
            return;
        }

        if (trimmed.StartsWith("Output #", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("Stream mapping:", StringComparison.OrdinalIgnoreCase))
        {
            _directShowInputSectionActive = false;
        }
    }

    private bool TryParseDirectShowInputVideoLine(
        string line,
        out bool inputIsP010,
        out string evidenceLine)
    {
        inputIsP010 = false;
        evidenceLine = line.Trim();

        if (!_directShowInputSectionActive)
        {
            return false;
        }

        if (!TryParseDirectShowInputVideoLineCore(line, out var parsedIsP010))
        {
            return false;
        }

        inputIsP010 = parsedIsP010;
        return true;
    }

    private static bool TryParseDirectShowInputVideoLineCore(string line, out bool inputIsP010)
    {
        inputIsP010 = false;
        if (string.IsNullOrWhiteSpace(line))
        {
            return false;
        }

        if (!line.Contains("Stream #0:", StringComparison.OrdinalIgnoreCase) ||
            !VideoStreamProbeRegex.IsMatch(line) ||
            line.Contains("->", StringComparison.Ordinal))
        {
            return false;
        }

        inputIsP010 = line.Contains("p010", StringComparison.OrdinalIgnoreCase);
        return true;
    }

    private static string? TryExtractObservedPixelFormatToken(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return null;
        }

        if (line.Contains("p010", StringComparison.OrdinalIgnoreCase))
        {
            return "P010";
        }

        if (line.Contains("nv12", StringComparison.OrdinalIgnoreCase))
        {
            return "NV12";
        }

        var videoMarkerIndex = line.IndexOf("Video:", StringComparison.OrdinalIgnoreCase);
        if (videoMarkerIndex < 0)
        {
            return null;
        }

        var payload = line[(videoMarkerIndex + "Video:".Length)..].Trim();
        if (payload.Length == 0)
        {
            return null;
        }

        var commaIndex = payload.IndexOf(',');
        if (commaIndex >= 0)
        {
            payload = payload[..commaIndex];
        }

        var token = payload.Trim();
        return string.IsNullOrWhiteSpace(token) ? null : token;
    }

    private static string? NormalizeObservedPixelFormat(string? formatToken)
    {
        if (string.IsNullOrWhiteSpace(formatToken))
        {
            return null;
        }

        if (formatToken.Contains("P010", StringComparison.OrdinalIgnoreCase))
        {
            return "P010";
        }

        if (formatToken.Contains("NV12", StringComparison.OrdinalIgnoreCase))
        {
            return "NV12";
        }

        return formatToken.Trim().ToUpperInvariant();
    }

    private void RecordObservedPixelFormatSample(string? formatToken, bool incrementAsFrame)
    {
        var normalizedFormat = NormalizeObservedPixelFormat(formatToken);
        if (string.IsNullOrWhiteSpace(normalizedFormat))
        {
            return;
        }

        var shouldIncrement = incrementAsFrame;
        lock (_observedFormatSync)
        {
            if (string.IsNullOrWhiteSpace(_firstObservedFramePixelFormat))
            {
                _firstObservedFramePixelFormat = normalizedFormat;
            }

            if (!incrementAsFrame)
            {
                shouldIncrement = !string.Equals(
                    _latestObservedFramePixelFormat,
                    normalizedFormat,
                    StringComparison.OrdinalIgnoreCase);
            }

            _latestObservedFramePixelFormat = normalizedFormat;
            _latestObservedSurfaceFormat = normalizedFormat;
        }

        if (!shouldIncrement)
        {
            return;
        }

        if (string.Equals(normalizedFormat, "P010", StringComparison.OrdinalIgnoreCase))
        {
            Interlocked.Increment(ref _observedP010FrameCount);
        }
        else if (string.Equals(normalizedFormat, "NV12", StringComparison.OrdinalIgnoreCase))
        {
            Interlocked.Increment(ref _observedNv12FrameCount);
        }
        else
        {
            Interlocked.Increment(ref _observedOtherFrameCount);
        }
    }

    private void ResetObservedFormatTelemetry()
    {
        lock (_observedFormatSync)
        {
            _firstObservedFramePixelFormat = null;
            _latestObservedFramePixelFormat = null;
            _latestObservedSurfaceFormat = null;
        }

        Interlocked.Exchange(ref _observedP010FrameCount, 0);
        Interlocked.Exchange(ref _observedNv12FrameCount, 0);
        Interlocked.Exchange(ref _observedOtherFrameCount, 0);
        Interlocked.Exchange(ref _observedP010BitDepthSampleCount, 0);
        Interlocked.Exchange(ref _observedP010Low2BitNonZeroSampleCount, 0);
        Interlocked.Exchange(ref _observedP010PaddingNonZeroSampleCount, 0);
    }

    private void SampleP010BitDepthTelemetry(byte[] sourceBuffer, int sourceLength)
    {
        if (sourceBuffer == null || sourceLength < 2)
        {
            return;
        }

        const int maxWordsPerFrame = 2048;
        var wordsAvailable = sourceLength / 2;
        var wordsToSample = Math.Min(wordsAvailable, maxWordsPerFrame);
        if (wordsToSample <= 0)
        {
            return;
        }

        long low2NonZero = 0;
        long paddingNonZero = 0;
        for (var sampleIndex = 0; sampleIndex < wordsToSample; sampleIndex++)
        {
            var offset = sampleIndex * 2;
            ushort word = (ushort)(sourceBuffer[offset] | (sourceBuffer[offset + 1] << 8));
            var sample10 = (word >> 6) & 0x3FF;
            if ((sample10 & 0x3) != 0)
            {
                low2NonZero++;
            }

            if ((word & 0x3F) != 0)
            {
                paddingNonZero++;
            }
        }

        Interlocked.Add(ref _observedP010BitDepthSampleCount, wordsToSample);
        Interlocked.Add(ref _observedP010Low2BitNonZeroSampleCount, low2NonZero);
        Interlocked.Add(ref _observedP010PaddingNonZeroSampleCount, paddingNonZero);
    }

    private static string EscapeDshowDeviceName(string deviceName)
        => deviceName.Replace("\"", "\\\"");

    private string BuildFFmpegArguments(CaptureSettings settings, string outputPath, bool useAudioPipe, string frameRateArg, uint effectiveWidth, uint effectiveHeight, string inputPixelFormat, bool hdrPipelineActive, string? audioPipeName)
    {
        var hdrRequested = hdrPipelineActive &&
                           settings.HdrEnabled &&
                           settings.HdrOutputMode == HdrOutputMode.Hdr10Pq;

        var hdrMasterRequested = !string.IsNullOrWhiteSpace(settings.HdrMasterDisplayMetadata);
        var hdrCllRequested = settings.HdrMaxCll > 0 && settings.HdrMaxFall > 0;

        // Get encoder and quality settings
        var (videoCodec, qualityArgs) = GetEncoderSettings(settings, hdrRequested, preferSoftwareHevc: false);
        _activeVideoCodec = videoCodec;
        _activeVideoProfile = "sdr";
        _activeTenBitPipelineConfirmed = false;

        if (hdrRequested &&
            videoCodec.Equals("hevc_nvenc", StringComparison.OrdinalIgnoreCase) &&
            (hdrMasterRequested || hdrCllRequested))
        {
            var missingMasterSupport = hdrMasterRequested && !_hdrArgumentSupport.SupportsMasterDisplay;
            var missingCllSupport = hdrCllRequested && !_hdrArgumentSupport.SupportsMaxCll;
            if (missingMasterSupport || missingCllSupport)
            {
                throw new InvalidOperationException(
                    "HDR mastering metadata was requested, but the active FFmpeg/NVENC path does not support " +
                    "master-display/max-cll metadata emission.");
            }
        }

        // Build audio input string
        // Using named pipe for audio gives us full timestamp control - both streams start at 0
        string audioInput = "";
        string audioArgs = "";
        if (settings.AudioEnabled)
        {
            if (useAudioPipe)
            {
                if (string.IsNullOrWhiteSpace(audioPipeName))
                {
                    throw new InvalidOperationException("Audio pipe mode requested without an initialized pipe name");
                }

                // Read raw float audio from named pipe
                // Format: f32le (32-bit float little-endian), 48kHz, stereo
                // This matches what AudioGraph actually outputs
                audioInput = $"-f f32le " +
                            $"-ar 48000 " +
                            $"-ac 2 " +
                            $"-thread_queue_size 1024 " +
                            $"-i \\\\.\\pipe\\{audioPipeName} ";
                Logger.Log($"Using named pipe for audio input: {audioPipeName}");
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
        var hdrMetadataArgs = string.Empty;
        var av1HdrMetadataBsfArgs = string.Empty;

        if (hdrRequested)
        {
            var isHevcNvenc = videoCodec.Equals("hevc_nvenc", StringComparison.OrdinalIgnoreCase);
            var isLibx265 = videoCodec.Equals("libx265", StringComparison.OrdinalIgnoreCase);
            var isAv1 = videoCodec.Contains("av1", StringComparison.OrdinalIgnoreCase);
            if (!isHevcNvenc && !isLibx265 && !isAv1)
            {
                throw new InvalidOperationException(
                    $"HDR10 output requires HEVC or AV1 10-bit encoding, but resolved codec is '{videoCodec}'.");
            }

            outputPixelFormat = isHevcNvenc ? "p010le" : "yuv420p10le";
            _activeVideoProfile = isAv1 ? "main-10bit" : "main10";
            _activeTenBitPipelineConfirmed = true;
            if (!inputPixelFormat.Equals("p010le", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"HDR output requires P010 ingress, but capture input is '{inputPixelFormat}'.");
            }
            EnsureHdrColorMetadataSupport();
            if (_hdrArgumentSupport.SupportsColorPrimaries)
            {
                hdrMetadataArgs += "-color_primaries bt2020 ";
            }

            if (_hdrArgumentSupport.SupportsColorTransfer)
            {
                hdrMetadataArgs += "-color_trc smpte2084 ";
            }

            if (_hdrArgumentSupport.SupportsColorSpace)
            {
                hdrMetadataArgs += "-colorspace bt2020nc ";
            }

            if (_hdrArgumentSupport.SupportsColorRange)
            {
                hdrMetadataArgs += "-color_range tv ";
            }

            if (isLibx265)
            {
                var x265Params = new List<string>
                {
                    "hdr-opt=1",
                    "repeat-headers=1",
                    "colorprim=bt2020",
                    "transfer=smpte2084",
                    "colormatrix=bt2020nc"
                };
                if (!string.IsNullOrWhiteSpace(settings.HdrMasterDisplayMetadata))
                {
                    x265Params.Add($"master-display={settings.HdrMasterDisplayMetadata}");
                }
                if (settings.HdrMaxCll > 0 && settings.HdrMaxFall > 0)
                {
                    x265Params.Add($"max-cll={settings.HdrMaxCll},{settings.HdrMaxFall}");
                }

                var x265HdrParams = string.Join(":", x265Params);
                qualityArgs = $"{qualityArgs} -x265-params \"{x265HdrParams}\"";
            }
            else
            {
                if (hdrMasterRequested)
                {
                    EnsureHdrMasteringMetadataSupport(hdrMasterRequested: true, hdrCllRequested: false);
                    hdrMetadataArgs += $"-master_display \"{settings.HdrMasterDisplayMetadata}\" ";
                }

                if (hdrCllRequested)
                {
                    EnsureHdrMasteringMetadataSupport(hdrMasterRequested: false, hdrCllRequested: true);
                    hdrMetadataArgs += $"-max_cll {settings.HdrMaxCll},{settings.HdrMaxFall} ";
                }
            }

            Logger.Log($"HDR10 output enabled: codec={videoCodec}, pix_fmt={outputPixelFormat}, maxCLL={settings.HdrMaxCll}, maxFALL={settings.HdrMaxFall}");
            if (isAv1)
            {
                Logger.Log("HDR10 AV1 metadata: applying av1_metadata bitstream filter (color_primaries=9 transfer_characteristics=16 matrix_coefficients=9).");
                // bt2020 primaries=9, smpte2084 transfer=16, bt2020nc matrix=9
                av1HdrMetadataBsfArgs = "-bsf:v av1_metadata=color_primaries=9:transfer_characteristics=16:matrix_coefficients=9 ";
            }
        }

        _activeInputPixelFormat = inputPixelFormat;
        _activeOutputPixelFormat = outputPixelFormat;
        Logger.Log(
            $"HDR_ENCODER_CONFIG codec={_activeVideoCodec} profile={_activeVideoProfile} " +
            $"input_pix_fmt={_activeInputPixelFormat} output_pix_fmt={_activeOutputPixelFormat} " +
            $"ten_bit_pipeline_confirmed={_activeTenBitPipelineConfirmed}");

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
                   $"{hdrMetadataArgs}" +
                   $"{av1HdrMetadataBsfArgs}" +
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

    private static string CreateSessionAudioPipeName()
    {
        return $"{AudioPipePrefix}_{Environment.ProcessId}_{Guid.NewGuid():N}";
    }

    private (string codec, string qualityArgs) GetEncoderSettings(
        CaptureSettings settings,
        bool hdrRequested,
        bool preferSoftwareHevc)
    {
        string codec;
        string qualityArgs;

        var support = _encoderSupport ?? EncoderSupport.Empty;
        bool useNvenc;

        // Determine base codec based on format and encoder availability
        switch (settings.Format)
        {
            case RecordingFormat.HevcMp4:
                codec = preferSoftwareHevc && support.HasLibX265
                    ? "libx265"
                    : support.HasHevcNvenc ? "hevc_nvenc" : "libx265";
                useNvenc = codec.EndsWith("_nvenc", StringComparison.OrdinalIgnoreCase);
                break;
            case RecordingFormat.Av1Mp4:
                codec = support.PreferredAv1Encoder ?? "libsvtav1";
                useNvenc = codec.EndsWith("_nvenc", StringComparison.OrdinalIgnoreCase);
                break;
            default: // H264Mp4
                codec = support.HasH264Nvenc ? "h264_nvenc" : "libx264";
                useNvenc = codec.EndsWith("_nvenc", StringComparison.OrdinalIgnoreCase);
                break;
        }

        if (hdrRequested &&
            !codec.Contains("hevc", StringComparison.OrdinalIgnoreCase) &&
            !codec.Contains("av1", StringComparison.OrdinalIgnoreCase))
        {
            if (support.HasHevc)
            {
                codec = support.HasHevcNvenc ? "hevc_nvenc" : "libx265";
                useNvenc = codec.EndsWith("_nvenc", StringComparison.OrdinalIgnoreCase);
            }
            else if (support.HasAv1)
            {
                codec = support.PreferredAv1Encoder ?? "libsvtav1";
                useNvenc = codec.EndsWith("_nvenc", StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                throw new InvalidOperationException("HDR10 output requires HEVC or AV1 10-bit encoder support, but no compatible encoder was detected.");
            }
        }

        // Determine quality settings
        var isAv1 = codec.Contains("av1", StringComparison.OrdinalIgnoreCase);
        var isSvtAv1 = codec.Equals("libsvtav1", StringComparison.OrdinalIgnoreCase);
        var isAomAv1 = codec.Equals("libaom-av1", StringComparison.OrdinalIgnoreCase);
        var nvencProfile = codec.Contains("hevc", StringComparison.OrdinalIgnoreCase)
            ? (hdrRequested ? "main10" : "main")
            : "high";
        var nvencProfileArg = useNvenc && !isAv1 ? $"-profile:v {nvencProfile} " : string.Empty;
        if (settings.Quality == VideoQuality.Custom)
        {
            // Use CBR with user-specified bitrate
            var bitrate = (int)(settings.CustomBitrateMbps * 1000); // kbps

            if (useNvenc)
            {
                // NVENC CBR mode
                qualityArgs = $"-preset p4 -rc cbr -b:v {bitrate}k -maxrate {bitrate}k -bufsize {bitrate * 2}k {nvencProfileArg}-bf 3";
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
                    qualityArgs = $"-preset {preset} -rc lossless {nvencProfileArg}-bf 3";
                }
                else
                {
                    // CQ mode (equivalent to CRF)
                    qualityArgs = $"-preset {preset} -rc vbr -cq {qualityValue} -b:v 0 {nvencProfileArg}-bf 3";
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
        var queue = _videoFrameQueue;
        if (!_isEncoding || queue == null || _cts?.IsCancellationRequested == true)
        {
            return;
        }

        _lastVideoEnqueueTick = Environment.TickCount64;
        if (Logger.VerboseEnabled && Interlocked.Exchange(ref _loggedFirstVideoEnqueue, 1) == 0)
        {
            Logger.LogVerbose($"First video frame enqueued after {_startupStopwatch.ElapsedMilliseconds} ms (queueCount={Volatile.Read(ref _videoQueueDepth)})");
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
                RecordObservedPixelFormatSample("NV12", incrementAsFrame: true);
                CopyNv12ToBuffer(frame, buffer);
            }
            else if (frame.BitmapPixelFormat == BitmapPixelFormat.P010)
            {
                RecordObservedPixelFormatSample("P010", incrementAsFrame: true);
                CopyP010ToBuffer(frame, buffer);
                SampleP010BitDepthTelemetry(buffer, bufferSize);
            }
            else
            {
                // BGRA/YUY2 fallback
                RecordObservedPixelFormatSample(frame.BitmapPixelFormat.ToString(), incrementAsFrame: true);
                frame.CopyToBuffer(buffer.AsBuffer());
            }

            var packet = new VideoFramePacket(buffer, bufferSize);
            if (!TryEnqueueVideoPacket(queue, packet))
            {
                var dropped = Interlocked.Increment(ref _droppedVideoFrames);
                Interlocked.Increment(ref _videoDropsQueueSaturated);
                if (dropped == 1 || dropped % 30 == 0)
                {
                    Logger.Log($"Warning: Dropped video frame (queue saturated). Total dropped: {dropped}, saturated={Interlocked.Read(ref _videoDropsQueueSaturated)}, evicted={Interlocked.Read(ref _videoDropsBacklogEviction)}");
                }
            }
        }
        catch (Exception ex)
        {
            LogComInteropAwareException(ex);
        }
    }

    private static bool IsComInteropDisabledException(Exception ex)
        => ex is NotSupportedException &&
           ex.Message.Contains("Built-in COM has been disabled", StringComparison.OrdinalIgnoreCase);

    private void LogComInteropAwareException(Exception ex)
    {
        if (!IsComInteropDisabledException(ex))
        {
            Logger.LogException(ex);
            return;
        }

        var nowTick = Environment.TickCount64;
        var lastTick = Interlocked.Read(ref _lastComInteropErrorLogTick);
        if (nowTick - lastTick < ComInteropErrorLogIntervalMs ||
            Interlocked.CompareExchange(ref _lastComInteropErrorLogTick, nowTick, lastTick) != lastTick)
        {
            Interlocked.Increment(ref _suppressedComInteropErrorCount);
            return;
        }

        var suppressed = Interlocked.Exchange(ref _suppressedComInteropErrorCount, 0);
        var suffix = suppressed > 0 ? $" (suppressed repeats: {suppressed})" : string.Empty;
        Logger.Log($"Video frame enqueue error: {ex.Message}{suffix}");
    }

    private bool TryEnqueueVideoPacket(Channel<VideoFramePacket> queue, VideoFramePacket packet)
    {
        if (_cts?.IsCancellationRequested == true)
        {
            ReturnFrameBuffer(packet.Buffer);
            return false;
        }

        if (queue.Writer.TryWrite(packet))
        {
            Interlocked.Increment(ref _videoQueueDepth);
            return true;
        }

        if (_videoDropPolicy == VideoFrameDropPolicy.DropOldest && queue.Reader.TryRead(out var evictedPacket))
        {
            Interlocked.Decrement(ref _videoQueueDepth);
            Interlocked.Increment(ref _videoDropsBacklogEviction);
            ReturnFrameBuffer(evictedPacket.Buffer);
            if (queue.Writer.TryWrite(packet))
            {
                Interlocked.Increment(ref _videoQueueDepth);
                return true;
            }
        }

        ReturnFrameBuffer(packet.Buffer);
        return false;
    }

    /// <summary>
    /// Enqueue audio samples for encoding.
    /// Samples can be queued as soon as PrepareAudioQueue() has been called,
    /// even before StartEncodingAsync() completes.
    /// </summary>
    public void EnqueueAudioSamples(byte[] samples)
    {
        if (samples == null || samples.Length == 0)
        {
            return;
        }

        EnqueueAudioSamples((ReadOnlyMemory<byte>)samples);
    }

    public void EnqueueAudioSamples(ReadOnlyMemory<byte> samples)
    {
        // Accept audio samples as soon as queue is ready (not just when encoding)
        // This allows audio to buffer while FFmpeg is starting up
        if (!_audioQueueReady || _audioSampleQueue == null || _cts?.IsCancellationRequested == true || samples.IsEmpty)
        {
            return;
        }

        _lastAudioEnqueueTick = Environment.TickCount64;
        if (Logger.VerboseEnabled && Interlocked.Exchange(ref _loggedFirstAudioEnqueue, 1) == 0)
        {
            Logger.LogVerbose($"First audio samples enqueued after {_startupStopwatch.ElapsedMilliseconds} ms (queueCount={Volatile.Read(ref _audioQueueDepth)})");
        }

        try
        {
            var buffer = GetAudioBuffer(samples.Length);
            samples.Span.CopyTo(buffer);
            var packet = new AudioSamplePacket(buffer, samples.Length);

            if (TryEnqueueAudioPacket(_audioSampleQueue, packet))
            {
                return;
            }

            var saturatedDrops = Interlocked.Increment(ref _audioDropsQueueSaturated);
            if (saturatedDrops == 1 || saturatedDrops % 120 == 0)
            {
                Logger.Log($"Warning: Dropped audio samples (queue saturated). Total dropped: {saturatedDrops}, evicted={Interlocked.Read(ref _audioDropsBacklogEviction)}");
            }
        }
        catch (Exception ex)
        {
            Logger.LogException(ex);
        }
    }

    private bool TryEnqueueAudioPacket(Channel<AudioSamplePacket> queue, AudioSamplePacket packet)
    {
        if (_cts?.IsCancellationRequested == true)
        {
            ReturnAudioBuffer(packet.Buffer);
            return false;
        }

        if (queue.Writer.TryWrite(packet))
        {
            Interlocked.Increment(ref _audioQueueDepth);
            return true;
        }

        if (queue.Reader.TryRead(out var evictedPacket))
        {
            Interlocked.Decrement(ref _audioQueueDepth);
            Interlocked.Increment(ref _audioDropsBacklogEviction);
            ReturnAudioBuffer(evictedPacket.Buffer);
            if (queue.Writer.TryWrite(packet))
            {
                Interlocked.Increment(ref _audioQueueDepth);
                return true;
            }
        }

        ReturnAudioBuffer(packet.Buffer);
        return false;
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
                    var hasFrame = await videoQueue.Reader.WaitToReadAsync(ct).ConfigureAwait(false);
                    if (!hasFrame)
                    {
                        break;
                    }

                    if (Logger.VerboseEnabled)
                    {
                        var now = Environment.TickCount64;
                        var idleMs = now - _lastVideoWriteTick;
                        if (idleMs >= 1000 && now - _lastVideoIdleLogTick >= 1000)
                        {
                            _lastVideoIdleLogTick = now;
                            var lastEnqueueAge = _lastVideoEnqueueTick == 0 ? -1 : now - _lastVideoEnqueueTick;
                            Logger.LogVerbose(
                                $"Video writer idle: {idleMs} ms (queue={Volatile.Read(ref _videoQueueDepth)}, lastEnqueueMs={lastEnqueueAge}, dropped={Interlocked.Read(ref _droppedVideoFrames)})");
                        }
                    }

                    while (videoQueue.Reader.TryRead(out var framePacket))
                    {
                        Interlocked.Decrement(ref _videoQueueDepth);
                        try
                        {
                            await videoStream.WriteAsync(framePacket.Buffer, 0, framePacket.Length, ct).ConfigureAwait(false);
                            var count = Interlocked.Increment(ref _encodedFrameCount);
                            FrameEncoded?.Invoke(this, count);
                            _lastVideoWriteTick = Environment.TickCount64;
                            if (Interlocked.Exchange(ref _loggedFirstVideoWrite, 1) == 0)
                            {
                                Logger.LogVerbose($"First video frame written after {_startupStopwatch.ElapsedMilliseconds} ms");
                            }
                        }
                        finally
                        {
                            ReturnFrameBuffer(framePacket.Buffer);
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
                var connected = await WaitForAudioPipeConnectionWithRetriesAsync(audioPipe, ct).ConfigureAwait(false);
                if (!connected)
                {
                    var timeoutSeconds = AudioPipeConnectAttempts * AudioPipeConnectTimeoutMs / 1000;
                    Logger.Log("Audio pipe connection timed out after retries - audio will not be recorded");
                    ErrorOccurred?.Invoke(
                        this,
                        $"Audio recording failed: FFmpeg did not connect to audio pipe within {timeoutSeconds} seconds");
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
                while (audioQueue.Reader.TryRead(out var bufferedSample))
                {
                    Interlocked.Decrement(ref _audioQueueDepth);
                    try
                    {
                        if (audioPipe.IsConnected)
                        {
                            await audioPipe.WriteAsync(bufferedSample.Buffer, 0, bufferedSample.Length, ct);
                        }
                    }
                    finally
                    {
                        ReturnAudioBuffer(bufferedSample.Buffer);
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
                    var hasSamples = await audioQueue.Reader.WaitToReadAsync(ct).ConfigureAwait(false);
                    if (!hasSamples)
                    {
                        break;
                    }

                    if (Logger.VerboseEnabled)
                    {
                        var now = Environment.TickCount64;
                        var idleMs = now - _lastAudioWriteTick;
                        if (idleMs >= 1000 && now - _lastAudioIdleLogTick >= 1000)
                        {
                            _lastAudioIdleLogTick = now;
                            var lastEnqueueAge = _lastAudioEnqueueTick == 0 ? -1 : now - _lastAudioEnqueueTick;
                            Logger.LogVerbose(
                                $"Audio writer idle: {idleMs} ms (queue={Volatile.Read(ref _audioQueueDepth)}, lastEnqueueMs={lastEnqueueAge})");
                        }
                    }

                    while (audioQueue.Reader.TryRead(out var audioData))
                    {
                        Interlocked.Decrement(ref _audioQueueDepth);
                        try
                        {
                            await audioPipe.WriteAsync(audioData.Buffer, 0, audioData.Length, ct).ConfigureAwait(false);
                            _lastAudioWriteTick = Environment.TickCount64;
                            if (Interlocked.Exchange(ref _loggedFirstAudioWrite, 1) == 0)
                            {
                                Logger.LogVerbose($"First audio bytes written after {_startupStopwatch.ElapsedMilliseconds} ms");
                            }
                        }
                        finally
                        {
                            ReturnAudioBuffer(audioData.Buffer);
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

    private async Task<bool> WaitForAudioPipeConnectionWithRetriesAsync(
        NamedPipeServerStream audioPipe,
        CancellationToken ct)
    {
        for (var attempt = 1; attempt <= AudioPipeConnectAttempts; attempt++)
        {
            ct.ThrowIfCancellationRequested();

            using var attemptCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            attemptCts.CancelAfter(AudioPipeConnectTimeoutMs);
            try
            {
                await audioPipe.WaitForConnectionAsync(attemptCts.Token).ConfigureAwait(false);
                return true;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (OperationCanceledException) when (attemptCts.IsCancellationRequested)
            {
                if (attempt < AudioPipeConnectAttempts)
                {
                    Logger.Log(
                        $"Audio pipe connection attempt {attempt}/{AudioPipeConnectAttempts} timed out after {AudioPipeConnectTimeoutMs} ms; retrying.");
                }
            }
            catch (InvalidOperationException) when (audioPipe.IsConnected)
            {
                return true;
            }
        }

        return false;
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

                if (TryParseProgressLine(line, out var outputBytes, out var bitrateBps))
                {
                    if (outputBytes.HasValue)
                    {
                        Interlocked.Exchange(ref _lastReportedOutputBytes, outputBytes.Value);
                    }

                    if (bitrateBps.HasValue)
                    {
                        Volatile.Write(ref _lastReportedOutputBitrateBps, bitrateBps.Value);
                    }
                }

                if (_usesDirectShowInput)
                {
                    UpdateDirectShowInputSectionState(line);

                    if (TryParseDirectShowInputVideoLine(line, out var runtimeInputIsP010, out var runtimeEvidenceLine))
                    {
                        var observedToken = TryExtractObservedPixelFormatToken(runtimeEvidenceLine);
                        RecordObservedPixelFormatSample(observedToken, incrementAsFrame: false);

                        if (_directShowHdrRequested && runtimeInputIsP010)
                        {
                            _directShowInputFormatVerified = true;
                        }
                        else if (_directShowHdrRequested &&
                                 !_directShowInputFormatVerified &&
                                 !_directShowIngressViolationRaised)
                        {
                            _directShowIngressViolationRaised = true;
                            var reason = $"DirectShow runtime input is not P010 in HDR mode: {runtimeEvidenceLine}";
                            Logger.Log(reason);
                            IngressViolationDetected?.Invoke(this, reason);
                            ErrorOccurred?.Invoke(this, reason);
                        }
                    }

                    var match = DropCounterRegex.Match(line);
                    if (match.Success && long.TryParse(match.Groups[1].Value, out var dropped))
                    {
                        Interlocked.Exchange(ref _droppedVideoFrames, dropped);
                    }
                }

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

    private static bool TryParseProgressLine(string line, out long? outputBytes, out double? bitrateBps)
    {
        outputBytes = null;
        bitrateBps = null;

        if (string.IsNullOrWhiteSpace(line))
        {
            return false;
        }

        // FFmpeg progress lines look like:
        // frame=   12 fps=... size=   1024KiB time=... bitrate=1234.5kbits/s ...
        if (line.IndexOf("frame=", StringComparison.OrdinalIgnoreCase) < 0)
        {
            return false;
        }

        var sizeMatch = ProgressSizeRegex.Match(line);
        if (sizeMatch.Success &&
            long.TryParse(sizeMatch.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var sizeValue))
        {
            outputBytes = ConvertSizeToBytes(sizeValue, sizeMatch.Groups[2].Value);
        }

        var bitrateMatch = ProgressBitrateRegex.Match(line);
        if (bitrateMatch.Success &&
            double.TryParse(bitrateMatch.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var brValue))
        {
            bitrateBps = ConvertBitrateToBps(brValue, bitrateMatch.Groups[2].Value);
        }

        return outputBytes.HasValue || bitrateBps.HasValue;
    }

    private static long ConvertSizeToBytes(long value, string unit)
    {
        if (value <= 0)
        {
            return 0;
        }

        var normalized = (unit ?? string.Empty).Trim().ToLowerInvariant();
        return normalized switch
        {
            "b" => value,
            "kb" => value * 1000L,
            "kib" => value * 1024L,
            "mb" => value * 1000L * 1000L,
            "mib" => value * 1024L * 1024L,
            "gb" => value * 1000L * 1000L * 1000L,
            "gib" => value * 1024L * 1024L * 1024L,
            _ => value
        };
    }

    private static double ConvertBitrateToBps(double value, string unit)
    {
        if (value <= 0)
        {
            return 0;
        }

        var normalized = (unit ?? string.Empty).Trim().ToLowerInvariant();
        return normalized switch
        {
            "bits/s" => value,
            "kbits/s" => value * 1000.0,
            "mbits/s" => value * 1000.0 * 1000.0,
            "gbits/s" => value * 1000.0 * 1000.0 * 1000.0,
            _ => value
        };
    }

    public async Task StopEncodingAsync()
    {
        var hasActiveResources =
            _ffmpegProcess != null ||
            _videoWriterTask != null ||
            _audioWriterTask != null ||
            _stderrReaderTask != null ||
            _videoFrameQueue != null ||
            _audioSampleQueue != null;

        if (!_isEncoding && !hasActiveResources)
        {
            return;
        }

        Logger.Log("=== FFmpeg Encoder Stopping ===");
        _isEncoding = false;
        _audioQueueReady = false;
        _lastStopTimedOut = false;

        // Phase 1: complete queues and allow writers to drain naturally.
        _videoFrameQueue?.Writer.TryComplete();
        _audioSampleQueue?.Writer.TryComplete();

        var writerTasks = new List<Task>();
        if (_videoWriterTask != null) writerTasks.Add(_videoWriterTask);
        if (_audioWriterTask != null) writerTasks.Add(_audioWriterTask);

        var writersDrained = true;
        if (writerTasks.Count > 0)
        {
            var allWriters = Task.WhenAll(writerTasks);
            writersDrained = await Task.WhenAny(allWriters, Task.Delay(WriterDrainTimeoutMs)) == allWriters;
            if (!writersDrained)
            {
                Logger.Log($"Writer drain exceeded {WriterDrainTimeoutMs} ms. Canceling writer tasks.");
                _cts?.Cancel();
                await Task.WhenAny(allWriters, Task.Delay(WriterCancelGraceMs));
            }
        }

        // Phase 2: cancel remaining background operations (e.g., stderr reader).
        if (writersDrained)
        {
            _cts?.Cancel();
        }

        if (_stderrReaderTask != null)
        {
            await Task.WhenAny(_stderrReaderTask, Task.Delay(1000));
        }

        if (_usesDirectShowInput && _ffmpegProcess != null && !_ffmpegProcess.HasExited)
        {
            try
            {
                await _ffmpegProcess.StandardInput.WriteLineAsync("q").ConfigureAwait(false);
                await _ffmpegProcess.StandardInput.FlushAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.Log($"DirectShow stop signal warning: {ex.Message}");
            }
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
                _lastStopTimedOut = true;
            }
            else
            {
                await exitTask;
            }
        }

        if (_ffmpegProcess != null)
        {
            _lastExitCode = _ffmpegProcess.HasExited ? _ffmpegProcess.ExitCode : null;
            if (!_ffmpegProcess.HasExited)
            {
                _lastStopTimedOut = true;
            }
            Logger.Log($"FFmpeg exited with code: {(_lastExitCode.HasValue ? _lastExitCode.Value : -1)}");
        }

        var enqueueDeltaMs = (_lastVideoEnqueueTick > 0 && _lastAudioEnqueueTick > 0)
            ? _lastAudioEnqueueTick - _lastVideoEnqueueTick
            : 0;
        var writeDeltaMs = (_lastVideoWriteTick > 0 && _lastAudioWriteTick > 0)
            ? _lastAudioWriteTick - _lastVideoWriteTick
            : 0;
        Logger.Log(
            $"CFR_DRIFT_METRICS video_master=true enqueue_delta_ms={enqueueDeltaMs} write_delta_ms={writeDeltaMs} " +
            $"video_dropped={DroppedVideoFrames} audio_saturated={AudioDropsQueueSaturated} audio_evicted={AudioDropsBacklogEviction}");

        await CleanupAsync();
        StatusChanged?.Invoke(this, "Encoding stopped");
        Logger.Log(
            $"=== FFmpeg Encoder Stopped (Total frames: {EncodedFrameCount}, droppedVideo={DroppedVideoFrames}, " +
            $"videoSaturated={VideoDropsQueueSaturated}, videoEvicted={VideoDropsBacklogEviction}, " +
            $"audioSaturated={AudioDropsQueueSaturated}, audioEvicted={AudioDropsBacklogEviction}, " +
            $"p010Samples={ObservedP010BitDepthSampleCount}, p010Low2NonZeroPct={ObservedP010Low2BitNonZeroPercent:0.###}, " +
            $"p010Likely8BitUpscaled={ObservedP010Likely8BitUpscaled?.ToString() ?? "unknown"}) ===");
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
            BitmapPixelFormat.P010 => width * height * 3,
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
        _videoFrameQueue?.Writer.TryComplete();
        _audioSampleQueue?.Writer.TryComplete();

        await CleanupPipesAsync();

        _videoStream = null; // Stream is owned by the process

        _audioPipe?.Dispose();
        _audioPipe = null;
        _audioPipeName = string.Empty;

        DrainVideoQueueBuffers();
        DrainAudioQueueBuffers();
        _videoFrameQueue = null;
        Interlocked.Exchange(ref _videoQueueDepth, 0);

        _audioSampleQueue = null;
        Interlocked.Exchange(ref _audioQueueDepth, 0);

        _ffmpegProcess?.Dispose();
        _ffmpegProcess = null;
        _activeInputPixelFormat = "nv12";
        _activeOutputPixelFormat = "yuv420p";
        _usesDirectShowInput = false;
        _directShowHdrRequested = false;
        _directShowInputFormatVerified = false;
        _directShowInputSectionActive = false;
        _directShowIngressViolationRaised = false;
        _hdrArgumentSupport = HdrArgumentSupport.Unknown;

        _cts?.Dispose();
        _cts = null;
        _videoWriterTask = null;
        _audioWriterTask = null;
        _stderrReaderTask = null;
    }

    private void DrainVideoQueueBuffers()
    {
        var videoQueue = _videoFrameQueue;
        if (videoQueue == null)
        {
            return;
        }

        while (videoQueue.Reader.TryRead(out var frame))
        {
            Interlocked.Decrement(ref _videoQueueDepth);
            ReturnFrameBuffer(frame.Buffer);
        }

        if (Volatile.Read(ref _videoQueueDepth) < 0)
        {
            Interlocked.Exchange(ref _videoQueueDepth, 0);
        }
    }

    private static unsafe void CopyP010ToBuffer(SoftwareBitmap frame, byte[] destination)
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
        var yRowBytes = width * 2;
        var uvRowBytes = width * 2;
        var uvHeight = height / 2;
        var ySize = yRowBytes * height;
        var uvSize = uvRowBytes * uvHeight;
        var totalSize = ySize + uvSize;

        if (destination.Length < totalSize)
        {
            throw new ArgumentException("Destination buffer too small for P010 frame.");
        }

        fixed (byte* dest = destination)
        {
            for (var row = 0; row < height; row++)
            {
                Buffer.MemoryCopy(
                    dataInBytes + planeY.StartIndex + (row * planeY.Stride),
                    dest + (row * yRowBytes),
                    yRowBytes,
                    yRowBytes);
            }

            var uvDest = dest + ySize;
            for (var row = 0; row < uvHeight; row++)
            {
                Buffer.MemoryCopy(
                    dataInBytes + planeUV.StartIndex + (row * planeUV.Stride),
                    uvDest + (row * uvRowBytes),
                    uvRowBytes,
                    uvRowBytes);
            }
        }
    }

    private void DrainAudioQueueBuffers()
    {
        var audioQueue = _audioSampleQueue;
        if (audioQueue == null)
        {
            return;
        }

        while (audioQueue.Reader.TryRead(out var sample))
        {
            Interlocked.Decrement(ref _audioQueueDepth);
            ReturnAudioBuffer(sample.Buffer);
        }

        if (Volatile.Read(ref _audioQueueDepth) < 0)
        {
            Interlocked.Exchange(ref _audioQueueDepth, 0);
        }
    }

    private byte[] GetFrameBuffer(int size)
    {
        return ArrayPool<byte>.Shared.Rent(size);
    }

    private static byte[] GetAudioBuffer(int size)
    {
        return ArrayPool<byte>.Shared.Rent(size);
    }

    private void ReturnFrameBuffer(byte[] buffer)
    {
        ArrayPool<byte>.Shared.Return(buffer);
    }

    private static void ReturnAudioBuffer(byte[] buffer)
    {
        ArrayPool<byte>.Shared.Return(buffer);
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
            while (videoQueue.Reader.TryRead(out var frame))
            {
                Interlocked.Decrement(ref _videoQueueDepth);
                ReturnFrameBuffer(frame.Buffer);
                videoDrained++;
            }
        }

        if (audioQueue != null)
        {
            while (audioQueue.Reader.TryRead(out var sample))
            {
                Interlocked.Decrement(ref _audioQueueDepth);
                ReturnAudioBuffer(sample.Buffer);
                audioDrained++;
            }
        }

        if (videoDrained > 0 || audioDrained > 0)
        {
            Logger.Log($"Drained queues after crash: {videoDrained} video frames, {audioDrained} audio samples");
        }

        if (Volatile.Read(ref _videoQueueDepth) < 0)
        {
            Interlocked.Exchange(ref _videoQueueDepth, 0);
        }

        if (Volatile.Read(ref _audioQueueDepth) < 0)
        {
            Interlocked.Exchange(ref _audioQueueDepth, 0);
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
            try { _videoFrameQueue?.Writer.TryComplete(); }
            catch (Exception ex) { Logger.Log($"Error completing video queue: {ex.Message}"); }

            try { _audioSampleQueue?.Writer.TryComplete(); }
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
            DrainVideoQueueBuffers();
            DrainAudioQueueBuffers();
            _audioPipe?.Dispose();
            _ffmpegProcess?.Dispose();
            _cts?.Dispose();
            _videoFrameQueue = null;
            _audioSampleQueue = null;
            Interlocked.Exchange(ref _videoQueueDepth, 0);
            Interlocked.Exchange(ref _audioQueueDepth, 0);
            _activeInputPixelFormat = "nv12";
            _activeOutputPixelFormat = "yuv420p";
            _hdrArgumentSupport = HdrArgumentSupport.Unknown;

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

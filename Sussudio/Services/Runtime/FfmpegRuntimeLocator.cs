using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using FFmpeg.AutoGen;
using Sussudio.Models;

namespace Sussudio.Services.Runtime;

// Selects the native runtime, discovers optional verification tools, and caches
// native capability evidence. Hardware trials run in supervised app children.
internal static class FfmpegRuntimeLocator
{
    // Match the binding ABI for every library used by capture, recording and
    // playback before selecting a root. Executable discovery is independent.
    private static readonly string[] RequiredNativeLibraryFileNames =
    {
        $"avcodec-{ffmpeg.LIBAVCODEC_VERSION_MAJOR}.dll",
        $"avformat-{ffmpeg.LIBAVFORMAT_VERSION_MAJOR}.dll",
        $"avutil-{ffmpeg.LIBAVUTIL_VERSION_MAJOR}.dll",
        $"swresample-{ffmpeg.LIBSWRESAMPLE_VERSION_MAJOR}.dll"
    };
    private const string AppLocalRuntimeFolderName = "ffmpeg";
    private static Task<EncoderSupport>? _encoderProbeTask;
    private static readonly object EncoderProbeLock = new();
    private static Task<SplitEncodeSupport>? _splitEncodeSupportTask;
    private static readonly object SplitEncodeSupportLock = new();
    private const int ProbeTimeoutMs = 10_000;

    internal static string GetAssemblyBaseDirectory()
    {
        var assemblyLocation = typeof(FfmpegRuntimeLocator).Assembly.Location;
        if (!string.IsNullOrWhiteSpace(assemblyLocation))
        {
            var assemblyDir = Path.GetDirectoryName(assemblyLocation);
            if (!string.IsNullOrWhiteSpace(assemblyDir))
            {
                return assemblyDir;
            }
        }

        return AppContext.BaseDirectory;
    }

    // The ffmpeg folder next to the app. It is searched first, so DLLs placed here
    // win over any FFmpeg found under Program Files or on PATH.
    internal static string GetAppLocalRuntimeDirectory()
        => Path.Combine(GetAssemblyBaseDirectory(), AppLocalRuntimeFolderName);

    internal static bool TryResolveNativeRuntimeRoot(out string runtimeRoot)
        => TryResolveNativeRuntimeRoot(preferredBaseDirectory: null, out runtimeRoot);

    internal static bool TryResolveNativeRuntimeRoot(string? preferredBaseDirectory, out string runtimeRoot)
    {
        foreach (var candidate in EnumerateCandidateDirectories(preferredBaseDirectory))
        {
            if (ContainsRequiredNativeLibraries(candidate))
            {
                runtimeRoot = candidate;
                return true;
            }
        }

        runtimeRoot = string.Empty;
        return false;
    }

    internal static string FindToolPath(string toolFileName)
        => FindToolPath(toolFileName, preferredBaseDirectory: null);

    internal static string FindToolPath(string toolFileName, string? preferredBaseDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(toolFileName);

        foreach (var candidate in EnumerateCandidateDirectories(preferredBaseDirectory))
        {
            var path = Path.Combine(candidate, toolFileName);
            if (File.Exists(path))
            {
                return path;
            }
        }

        if (TryResolvePathTool(toolFileName, out var pathToolMatch))
        {
            return pathToolMatch;
        }

        return toolFileName;
    }

    public static Task<EncoderSupport> GetEncoderSupportAsync()
    {
        lock (EncoderProbeLock)
        {
            _encoderProbeTask ??= Task.Run(ReadNativeEncoderSupport);
            return _encoderProbeTask;
        }
    }

    public static Task<SplitEncodeSupport> GetSplitEncodeSupportAsync()
    {
        lock (SplitEncodeSupportLock)
        {
            _splitEncodeSupportTask ??= ProbeSplitEncodeSupportAsync();
            return _splitEncodeSupportTask;
        }
    }

    private static IEnumerable<string> EnumerateCandidateDirectories(string? preferredBaseDirectory)
    {
        var assemblyDir = !string.IsNullOrWhiteSpace(preferredBaseDirectory)
            ? preferredBaseDirectory
            : GetAssemblyBaseDirectory();
        if (!string.IsNullOrWhiteSpace(assemblyDir))
        {
            yield return Path.Combine(assemblyDir, AppLocalRuntimeFolderName);
            yield return assemblyDir;
        }

        var programFilesDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            "ffmpeg",
            "bin");
        yield return programFilesDir;

        if (TryResolvePathTool("ffmpeg.exe", out var pathToolMatch))
        {
            var pathDir = Path.GetDirectoryName(pathToolMatch);
            if (!string.IsNullOrWhiteSpace(pathDir))
            {
                yield return pathDir;
            }
        }
    }

    internal static bool ContainsRequiredNativeLibraries(string directory)
    {
        if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
        {
            return false;
        }

        foreach (var fileName in RequiredNativeLibraryFileNames)
        {
            if (!File.Exists(Path.Combine(directory, fileName)))
            {
                return false;
            }
        }

        return true;
    }

    internal static unsafe EncoderSupport ReadNativeEncoderSupport()
    {
        var runtimeRoot = FfmpegRuntimeInit.GetInitializedRuntimeRoot();
        var support = new EncoderSupport
        {
            HasH264Nvenc = ffmpeg.avcodec_find_encoder_by_name("h264_nvenc") != null,
            HasHevcNvenc = ffmpeg.avcodec_find_encoder_by_name("hevc_nvenc") != null,
            HasAv1Nvenc = ffmpeg.avcodec_find_encoder_by_name("av1_nvenc") != null,
            HasLibX264 = ffmpeg.avcodec_find_encoder_by_name("libx264") != null,
            HasLibX265 = ffmpeg.avcodec_find_encoder_by_name("libx265") != null,
            HasLibSvtAv1 = ffmpeg.avcodec_find_encoder_by_name("libsvtav1") != null,
            HasLibAomAv1 = ffmpeg.avcodec_find_encoder_by_name("libaom-av1") != null
        };
        Logger.Log(
            $"LIBAV_ENCODER_SUPPORT root='{runtimeRoot}' h264_nvenc={support.HasH264Nvenc} " +
            $"hevc_nvenc={support.HasHevcNvenc} av1_nvenc={support.HasAv1Nvenc}");
        return support;
    }

    private static async Task<SplitEncodeSupport> ProbeSplitEncodeSupportAsync()
    {
        var runtimeRoot = FfmpegRuntimeInit.GetInitializedRuntimeRoot();
        var runtimeVersions = FfmpegRuntimeInit.GetInitializedRuntimeVersions();
        var executable = Path.ChangeExtension(typeof(FfmpegRuntimeLocator).Assembly.Location, ".exe");
        if (string.IsNullOrWhiteSpace(executable) || !File.Exists(executable))
        {
            throw new InvalidOperationException($"The Sussudio app executable is unavailable for native capability probing: '{executable}'.");
        }

        return await ProbeSplitEncodeSupportAsync(
            runtimeRoot,
            runtimeVersions,
            executable,
            Path.Combine(Path.GetTempPath(), "Sussudio", "NativeCapabilities"),
            new ProcessSupervisor()).ConfigureAwait(false);
    }

    internal static async Task<SplitEncodeSupport> ProbeSplitEncodeSupportAsync(
        string runtimeRoot,
        string runtimeVersions,
        string executable,
        string probeLogRoot,
        IProcessSupervisor supervisor)
    {
        // Serialize the trials. A failed/unconfirmed child aborts the sequence so
        // a driver stall cannot accumulate additional GPU workers.
        var twoWay = await TestModeAsync(2).ConfigureAwait(false);
        var threeWay = await TestModeAsync(3).ConfigureAwait(false);
        Logger.Log($"LIBAV_SPLIT_PROBE_COMPLETE root='{runtimeRoot}' two_way={twoWay} three_way={threeWay} fixture=hevc_nvenc_nv12_3840x2160");
        return new SplitEncodeSupport(twoWay, threeWay);

        async Task<bool> TestModeAsync(int mode)
        {
            var logDirectory = Path.Combine(probeLogRoot, Guid.NewGuid().ToString("N"));
            var process = await supervisor.RunAsync(new ProcessSpec
            {
                FileName = executable,
                Arguments = NativeFfmpegCapabilityProbe.CreateArguments(runtimeRoot, mode, logDirectory),
                TimeoutMs = ProbeTimeoutMs
            }).ConfigureAwait(false);
            try
            {
                return NativeFfmpegCapabilityProbe.ReadAcceptedResult(process, runtimeRoot, runtimeVersions, mode);
            }
            catch (Exception ex)
            {
                Logger.Log($"LIBAV_SPLIT_PROBE_FAILED mode={mode} log_root='{logDirectory}' error='{ex.Message}'");
                throw;
            }
        }
    }

    private static bool TryResolvePathTool(string toolFileName, out string resolvedPath)
    {
        resolvedPath = string.Empty;

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "where",
                Arguments = toolFileName,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };
            using var proc = Process.Start(psi);
            if (proc == null)
            {
                return false;
            }

            if (!proc.WaitForExit(5000))
            {
                Logger.Log($"FFMPEG_PATH_PROBE_TIMEOUT tool='{toolFileName}' timeoutMs=5000 pid={proc.Id}");
                try
                {
                    proc.Kill(entireProcessTree: true);
                }
                catch (Exception killEx) when (!proc.HasExited)
                {
                    // An already-exited probe is the expected race and stays quiet;
                    // a live process that resisted termination is worth recording.
                    Logger.Log(
                        $"FFMPEG_PATH_PROBE_KILL_FAIL pid={proc.Id} type={killEx.GetType().Name} msg='{killEx.Message}'");
                }
                catch (Exception)
                {
                    // where.exe exited between the timeout and the kill.
                }

                return false;
            }

            var output = proc.StandardOutput.ReadToEnd();
            if (proc.ExitCode != 0 || string.IsNullOrWhiteSpace(output))
            {
                return false;
            }

            resolvedPath = output.Split('\n', StringSplitOptions.RemoveEmptyEntries)[0].Trim();
            return !string.IsNullOrWhiteSpace(resolvedPath);
        }
        catch (Exception ex)
        {
            Logger.Log($"Suppressed exception in FfmpegRuntimeLocator path probe: {ex.Message}");
            return false;
        }
    }
}

/// <summary>
/// One-time FFmpeg native runtime initialization: resolves the native library root,
/// sets the log level, and installs the log callback.
/// Thread-safe; subsequent calls after the first successful init are no-ops.
/// </summary>
internal static unsafe class FfmpegRuntimeInit
{
    private static readonly object InitSync = new();
    private static bool _initialized;
    private static string? _selectedRuntimeRoot;
    private static string? _initializedRuntimeVersions;
    private const int LogLineCapacity = 1024;
    // Error lines per wall-clock second forwarded to the app log; the rest of that second is
    // counted and reported once a later second admits a line again.
    private const int MaxLogLinesPerSecond = 40;
    private static readonly object LogRateSync = new();
    private static long _logWindowStartTicks;
    private static int _logLinesInWindow;
    private static int _logLinesSuppressedInWindow;

    /// <summary>
    /// Native log sink. libav invokes the callback for every message at every level, so the
    /// common case must cost one integer compare and no marshalling. Errors are formatted with
    /// the original va_list and forwarded, rate limited.
    /// </summary>
    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static unsafe void NativeLogCallback(void* avcl, int level, byte* fmt, byte* vl)
    {
        if (level > ffmpeg.AV_LOG_ERROR || fmt == null)
        {
            return;
        }

        try
        {
            FfmpegLogCallbackImpl(avcl, level, fmt, vl);
        }
        catch
        {
            // Best effort: never throw back into native code.
        }
    }

    internal static unsafe void FfmpegLogCallbackImpl(void* avcl, int level, byte* fmt, byte* vl)
    {
        var format = Marshal.PtrToStringUTF8((IntPtr)fmt);
        if (string.IsNullOrEmpty(format))
        {
            return;
        }

        string? message = null;
        var line = stackalloc byte[LogLineCapacity];
        var printPrefix = 1;
        var written = ffmpeg.av_log_format_line2(avcl, level, format, vl, line, LogLineCapacity, &printPrefix);
        if (written > 0)
        {
            message = Marshal.PtrToStringUTF8((IntPtr)line);
        }

        message = (message ?? format).TrimEnd('\n', '\r');
        if (message.Length == 0 || FfmpegLogSuppressionScope.TryConsumeRecoverableSeekFfmpegLog(message))
        {
            return;
        }

        if (!TryAdmitLogLine(out var suppressedInPreviousWindow))
        {
            return;
        }

        if (suppressedInPreviousWindow > 0)
        {
            Logger.Log($"FFMPEG_LOG_RATE_LIMITED suppressed={suppressedInPreviousWindow}");
        }

        Logger.Log($"FFMPEG_LOG [{level}] {message}");
    }

    private static bool TryAdmitLogLine(out int suppressedInPreviousWindow)
    {
        suppressedInPreviousWindow = 0;
        var now = Environment.TickCount64;
        lock (LogRateSync)
        {
            if (now - _logWindowStartTicks >= 1000)
            {
                suppressedInPreviousWindow = _logLinesSuppressedInWindow;
                _logWindowStartTicks = now;
                _logLinesInWindow = 0;
                _logLinesSuppressedInWindow = 0;
            }

            if (_logLinesInWindow >= MaxLogLinesPerSecond)
            {
                _logLinesSuppressedInWindow++;
                return false;
            }

            _logLinesInWindow++;
            return true;
        }
    }

    /// <summary>
    /// Initializes the FFmpeg native runtime if it has not already been initialized.
    /// </summary>
    /// <param name="requireNativeRuntime">
    /// When <see langword="true"/>, throws <see cref="InvalidOperationException"/> if the
    /// native runtime cannot be located or fails to load.
    /// </param>
    public static void EnsureInitialized(bool requireNativeRuntime)
    {
        lock (InitSync)
        {
            if (_initialized)
            {
                return;
            }

            if (!FfmpegRuntimeLocator.TryResolveNativeRuntimeRoot(out var runtimeRoot))
            {
                var message =
                    $"FFmpeg native runtime not found. assembly_dir='{FfmpegRuntimeLocator.GetAssemblyBaseDirectory()}'";
                Logger.Log($"LIBAV_RUNTIME_MISSING {message}");
                if (requireNativeRuntime)
                {
                    throw new InvalidOperationException(message);
                }

                return;
            }

            try
            {
                EnsureInitializedAtRoot(runtimeRoot);
            }
            catch (Exception ex)
            {
                Logger.Log($"LIBAV_INIT_ERROR root_path='{ffmpeg.RootPath}' type={ex.GetType().Name} msg={ex.Message}");
                if (requireNativeRuntime)
                {
                    throw new InvalidOperationException(
                        $"FFmpeg native runtime failed to initialize from '{ffmpeg.RootPath}': {ex.Message}",
                        ex);
                }
            }
        }
    }

    internal static string GetInitializedRuntimeRoot()
    {
        EnsureInitialized(requireNativeRuntime: true);
        lock (InitSync)
        {
            return _selectedRuntimeRoot!;
        }
    }

    internal static string GetInitializedRuntimeVersions()
    {
        EnsureInitialized(requireNativeRuntime: true);
        lock (InitSync)
        {
            return _initializedRuntimeVersions!;
        }
    }

    internal static void EnsureInitializedAtRoot(string runtimeRoot)
    {
        if (!Path.IsPathFullyQualified(runtimeRoot))
        {
            throw new ArgumentException("An absolute native runtime root is required.", nameof(runtimeRoot));
        }

        var root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(runtimeRoot));
        lock (InitSync)
        {
            // Even a partially loaded binding can retain delegates to the first
            // library. Never retarget it after a failed initialization attempt.
            if (_selectedRuntimeRoot != null &&
                !string.Equals(_selectedRuntimeRoot, root, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"FFmpeg is already bound to '{_selectedRuntimeRoot}', not '{root}'.");
            }

            if (_initialized)
            {
                return;
            }

            if (!FfmpegRuntimeLocator.ContainsRequiredNativeLibraries(root))
            {
                throw new InvalidOperationException($"FFmpeg native runtime is incomplete or has the wrong ABI: '{root}'.");
            }

            _selectedRuntimeRoot = root;
            ffmpeg.RootPath = root;
            var avcodecVersion = ffmpeg.avcodec_version();
            var avformatVersion = ffmpeg.avformat_version();
            var avutilVersion = ffmpeg.avutil_version();
            var swresampleVersion = ffmpeg.swresample_version();
            if (avcodecVersion >> 16 != ffmpeg.LIBAVCODEC_VERSION_MAJOR ||
                avformatVersion >> 16 != ffmpeg.LIBAVFORMAT_VERSION_MAJOR ||
                avutilVersion >> 16 != ffmpeg.LIBAVUTIL_VERSION_MAJOR ||
                swresampleVersion >> 16 != ffmpeg.LIBSWRESAMPLE_VERSION_MAJOR)
            {
                throw new InvalidOperationException($"FFmpeg native library versions do not match the managed binding ABI at '{root}'.");
            }

            _initializedRuntimeVersions = $"{avcodecVersion}/{avformatVersion}/{avutilVersion}/{swresampleVersion}";
            // Error level only. The native callback already filters by level without
            // marshalling; the level itself matters for codecs that consult
            // av_log_get_level() before building diagnostics.
            ffmpeg.av_log_set_level(ffmpeg.AV_LOG_ERROR);
            ffmpeg.av_log_set_callback(new av_log_set_callback_callback_func
            {
                Pointer = (IntPtr)(delegate* unmanaged[Cdecl]<void*, int, byte*, byte*, void>)&NativeLogCallback
            });
            _initialized = true;
            Logger.Log($"LIBAV_INIT root_path='{root}' versions='{_initializedRuntimeVersions}'");
        }
    }
}

/// <summary>
/// Suppresses known-recoverable FFmpeg log messages emitted during seek operations.
/// The suppression is depth-tracked and thread-local, so nested scopes compose correctly.
/// </summary>
/// <remarks>
/// Usage:
/// <code>
/// using var scope = FfmpegLogSuppressionScope.SuppressRecoverableSeekFfmpegLogs();
/// // libav seek call that is known to spam recoverable errors
/// </code>
/// </remarks>
internal static class FfmpegLogSuppressionScope
{
    [ThreadStatic]
    private static int _recoverableSeekLogSuppressionDepth;
    [ThreadStatic]
    private static int _recoverableSeekLogSuppressedCount;

    internal static IDisposable SuppressRecoverableSeekFfmpegLogs()
    {
        _recoverableSeekLogSuppressionDepth++;
        return new RecoverableSeekLogSuppressionScope(_recoverableSeekLogSuppressedCount);
    }

    internal static bool TryConsumeRecoverableSeekFfmpegLog(string message)
    {
        if (_recoverableSeekLogSuppressionDepth <= 0)
        {
            return false;
        }

        var recoverable =
            message.Contains("Could not find ref with POC", StringComparison.Ordinal) ||
            message.Contains("Error constructing the frame RPS", StringComparison.Ordinal) ||
            message.Contains("First slice in a frame missing", StringComparison.Ordinal) ||
            message.Contains("PPS id out of range", StringComparison.Ordinal);

        if (recoverable)
        {
            _recoverableSeekLogSuppressedCount++;
        }

        return recoverable;
    }

    private sealed class RecoverableSeekLogSuppressionScope : IDisposable
    {
        private readonly int _initialSuppressedCount;
        private bool _disposed;

        public RecoverableSeekLogSuppressionScope(int initialSuppressedCount)
        {
            _initialSuppressedCount = initialSuppressedCount;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            if (_recoverableSeekLogSuppressionDepth > 0)
            {
                _recoverableSeekLogSuppressionDepth--;
            }

            var suppressed = _recoverableSeekLogSuppressedCount - _initialSuppressedCount;
            if (suppressed > 0)
            {
                Logger.Log($"FFMPEG_LOG_RECOVERABLE_SEEK_SUPPRESSED count={suppressed}");
            }
        }
    }
}

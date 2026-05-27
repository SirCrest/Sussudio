using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FFmpeg.AutoGen;
using Sussudio.Models;

namespace Sussudio.Services.Runtime;

// Locates FFmpeg/ffprobe and caches capability probes. UI option lists and
// recording verification depend on these probes, so failures return explicit
// "unsupported" snapshots rather than throwing during normal startup.
internal static class FfmpegRuntimeLocator
{
    private static readonly string[] RequiredNativeLibraryPatterns =
    {
        "avcodec-*.dll",
        "avutil-*.dll"
    };
    private static Task<EncoderSupport>? _encoderProbeTask;
    private static readonly object EncoderProbeLock = new();
    private static Task<SplitEncodeSupport>? _splitEncodeSupportTask;
    private static readonly object SplitEncodeSupportLock = new();
    private static readonly Lazy<string> CachedFfmpegPath = new(() => FindToolPath("ffmpeg.exe"));
    private const int ProbeTimeoutMs = 10_000;

    private readonly record struct ProbeCommandResult(
        string Output,
        int ExitCode,
        bool Started,
        bool TimedOut);

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
            _encoderProbeTask ??= ProbeEncoderSupportAsync();
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
            yield return Path.Combine(assemblyDir, "ffmpeg");
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

    private static bool ContainsRequiredNativeLibraries(string directory)
    {
        if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
        {
            return false;
        }

        foreach (var pattern in RequiredNativeLibraryPatterns)
        {
            if (!Directory.EnumerateFiles(directory, pattern, SearchOption.TopDirectoryOnly).Any())
            {
                return false;
            }
        }

        return true;
    }

    private static async Task<EncoderSupport> ProbeEncoderSupportAsync()
    {
        var ffmpegPath = CachedFfmpegPath.Value;
        try
        {
            var result = await RunProbeCommandAsync(ffmpegPath, "-hide_banner -encoders");
            if (!result.Started || result.TimedOut || result.ExitCode != 0)
            {
                Logger.Log(
                    "FFmpeg encoder probe did not complete successfully: " +
                    $"started={result.Started} timedOut={result.TimedOut} exitCode={result.ExitCode}");
                return EncoderSupport.Empty;
            }

            var output = result.Output;
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

    private static async Task<SplitEncodeSupport> ProbeSplitEncodeSupportAsync()
    {
        var ffmpegPath = CachedFfmpegPath.Value;
        try
        {
            var twoWayTask = TestSplitEncodeModeAsync(ffmpegPath, 2);
            var threeWayTask = TestSplitEncodeModeAsync(ffmpegPath, 3);
            await Task.WhenAll(twoWayTask, threeWayTask).ConfigureAwait(false);
            var support = new SplitEncodeSupport(twoWayTask.Result, threeWayTask.Result);
            Logger.Log($"Split encode support: 2-way={support.Supports2Way}, 3-way={support.Supports3Way}");
            return support;
        }
        catch (Exception ex)
        {
            Logger.Log($"Split encode probe failed: {ex.Message}");
            return SplitEncodeSupport.NvencUnavailable;
        }
    }

    private static async Task<bool> TestSplitEncodeModeAsync(string ffmpegPath, int mode)
    {
        var probeArgs =
            "-hide_banner -loglevel error " +
            "-f lavfi -i color=size=16x16:rate=1:color=black:duration=1 " +
            "-c:v hevc_nvenc " +
            $"-split_encode_mode {mode} " +
            "-frames:v 1 -an -f null NUL";
        var result = await RunProbeCommandAsync(ffmpegPath, probeArgs).ConfigureAwait(false);
        return result.Started && !result.TimedOut && result.ExitCode == 0;
    }

    private static async Task<ProbeCommandResult> RunProbeCommandAsync(string fileName, string arguments)
    {
        var result = await new ProcessSupervisor().RunAsync(new ProcessSpec
        {
            FileName = fileName,
            Arguments = arguments,
            TimeoutMs = ProbeTimeoutMs
        }).ConfigureAwait(false);

        if (!result.Started)
        {
            return new ProbeCommandResult(string.Empty, -1, Started: false, TimedOut: false);
        }

        var output = result.StdOut + Environment.NewLine + result.StdErr;
        return new ProbeCommandResult(output, result.ExitCode ?? -1, Started: true, TimedOut: result.TimedOut);
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
                try
                {
                    proc.Kill(entireProcessTree: true);
                }
                catch
                {
                    // Best-effort: where.exe may have already exited.
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
            System.Diagnostics.Trace.TraceWarning($"Suppressed exception in FfmpegRuntimeLocator path probe: {ex.Message}");
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
    // Must be a static field to prevent GC collection while FFmpeg holds the delegate pointer.
    private static av_log_set_callback_callback? _logCallback;

    internal static unsafe void FfmpegLogCallbackImpl(void* avcl, int level, string fmt, byte* vl)
    {
        // Only capture errors and above to avoid flooding.
        if (level > ffmpeg.AV_LOG_ERROR)
        {
            return;
        }

        try
        {
            // Log the raw format string; va_list formatting is unreliable across platforms.
            var msg = fmt?.TrimEnd('\n', '\r');
            if (!string.IsNullOrEmpty(msg))
            {
                if (FfmpegLogSuppressionScope.ShouldSuppressRecoverableSeekFfmpegLog(msg))
                {
                    return;
                }

                Logger.Log($"FFMPEG_LOG [{level}] {msg}");
            }
        }
        catch
        {
            // Best effort: never crash in a log callback.
        }
    }

    /// <summary>
    /// Initializes the FFmpeg native runtime if it has not already been initialized.
    /// </summary>
    /// <param name="requireNativeRuntime">
    /// When <see langword="true"/>, throws <see cref="InvalidOperationException"/> if the
    /// native runtime cannot be located or fails to load.
    /// </param>
    public static void EnsureInitialized(bool requireNativeRuntime = false)
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

            ffmpeg.RootPath = runtimeRoot;

            try
            {
                Logger.Log($"LIBAV_INIT root_path='{ffmpeg.RootPath}' avcodec_version={ffmpeg.avcodec_version()}");

                // Route FFmpeg internal logs (especially D3D11VA errors) to our logger.
                // Keep a static reference to prevent GC collection of the delegate.
                _logCallback = FfmpegLogCallbackImpl;
                unsafe
                {
                    ffmpeg.av_log_set_level(ffmpeg.AV_LOG_VERBOSE);
                    ffmpeg.av_log_set_callback(_logCallback);
                }

                _initialized = true;
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
}

/// <summary>
/// Suppresses known-recoverable FFmpeg log messages emitted during seek operations.
/// The suppression is depth-tracked and thread-local, so nested scopes compose correctly.
/// </summary>
/// <remarks>
/// Usage:
/// <code>
/// using var scope = FfmpegLogSuppressionScope.SuppressRecoverableSeekLogs();
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

    internal static bool ShouldSuppressRecoverableSeekFfmpegLog(string message)
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

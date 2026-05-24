using System;
using FFmpeg.AutoGen;

namespace Sussudio.Services.Runtime;

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
        // Only capture errors and above to avoid flooding
        if (level > ffmpeg.AV_LOG_ERROR) return;

        try
        {
            // Log the raw format string — va_list formatting is unreliable across platforms
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
            // Best effort — never crash in a log callback
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

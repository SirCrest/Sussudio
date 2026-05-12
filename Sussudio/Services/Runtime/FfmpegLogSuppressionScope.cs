using System;

namespace Sussudio.Services.Runtime;

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

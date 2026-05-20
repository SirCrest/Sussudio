using System;
using System.Threading;

namespace Sussudio.Services.Flashback;

internal sealed partial class FlashbackPlaybackController
{
    // --- Adjacent segment seek fallback ---

    private static readonly TimeSpan AdjacentSegmentSeekFallbackWindow = TimeSpan.FromSeconds(3);

    private bool TrySeekAdjacentSegmentStart(
        FlashbackDecoder decoder,
        ref bool fileOpen,
        TimeSpan seekTarget,
        string reason,
        out TimeSpan effectiveSeekTarget,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        effectiveSeekTarget = seekTarget;
        var currentPath = _currentOpenFilePath;
        if (string.IsNullOrWhiteSpace(currentPath))
        {
            return false;
        }

        var nextPath = _bufferManager.GetNextSegmentFile(currentPath);
        if (string.IsNullOrWhiteSpace(nextPath) || IsSamePlaybackPath(nextPath, currentPath))
        {
            return false;
        }

        var nextStart = _bufferManager.GetSegmentStartPts(nextPath);
        if (!nextStart.HasValue)
        {
            return false;
        }

        var targetGap = (nextStart.Value - seekTarget).Duration();
        if (targetGap > AdjacentSegmentSeekFallbackWindow)
        {
            return false;
        }

        effectiveSeekTarget = seekTarget < nextStart.Value ? nextStart.Value : seekTarget;
        try
        {
            Logger.Log(
                $"FLASHBACK_PLAYBACK_ADJACENT_SEGMENT_SEEK reason={reason} " +
                $"from='{System.IO.Path.GetFileName(currentPath)}' next='{System.IO.Path.GetFileName(nextPath)}' " +
                $"target_ms={(long)seekTarget.TotalMilliseconds} effective_ms={(long)effectiveSeekTarget.TotalMilliseconds}");
            ReopenDecoderPlaybackFile(
                decoder,
                nextPath,
                ref fileOpen,
                updateCurrentOpenPath: true,
                closeOnlyWhenOpen: true);
            cancellationToken.ThrowIfCancellationRequested();
            if (SeekToWithCapTelemetry(decoder, effectiveSeekTarget, reason, cancellationToken))
            {
                Interlocked.Increment(ref _playbackSegmentSwitches);
                Interlocked.Exchange(ref _lastSegmentSwitchUtcUnixMs, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
                ResetPlaybackPtsCadenceBaseline();
                return true;
            }

            SetReopenFailure(reason, "adjacent_seek_failed", effectiveSeekTarget);
            Logger.Log($"FLASHBACK_PLAYBACK_ADJACENT_SEGMENT_SEEK_FAIL reason={reason} path='{nextPath}' offset_ms={(long)effectiveSeekTarget.TotalMilliseconds}");
            return false;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            SetReopenFailure(reason, ex.GetType().Name, effectiveSeekTarget);
            Logger.Log($"FLASHBACK_PLAYBACK_ADJACENT_SEGMENT_SEEK_ERROR reason={reason} path='{nextPath}' type={ex.GetType().Name} msg='{ex.Message}'");
            MarkDecoderPlaybackFileClosed(ref fileOpen);
            return false;
        }
    }
}

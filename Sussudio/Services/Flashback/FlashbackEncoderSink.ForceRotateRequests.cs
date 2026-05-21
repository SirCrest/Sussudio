using System;
using System.Threading;
using Sussudio.Models;

namespace Sussudio.Services.Flashback;

internal sealed partial class FlashbackEncoderSink
{
    private const int ForceRotateCommittedGraceMs = 1_000;

    public FlashbackForceRotateResult ForceRotateForExport(
        TimeSpan inPoint,
        TimeSpan outPoint,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (inPoint < TimeSpan.Zero || outPoint <= inPoint)
        {
            Logger.Log(
                $"FLASHBACK_SINK_FORCE_ROTATE_REJECTED_RANGE in_ms={(long)inPoint.TotalMilliseconds} " +
                $"out_ms={(long)outPoint.TotalMilliseconds}");
            return FlashbackForceRotateResult.Failed();
        }

        lock (_sync)
        {
            if (!_started || _disposed)
            {
                Logger.Log(
                    $"FLASHBACK_SINK_FORCE_ROTATE_REJECTED_INACTIVE started={_started} disposed={_disposed}");
                return FlashbackForceRotateResult.Failed();
            }

            if (_encodingFailure != null || _encodingTask?.IsCompleted == true)
            {
                Logger.Log(
                    $"FLASHBACK_SINK_FORCE_ROTATE_REJECTED failed={_encodingFailure != null} " +
                    $"completed={_encodingTask?.IsCompleted == true} type={_encodingFailure?.GetType().Name ?? "None"}");
                return FlashbackForceRotateResult.Failed();
            }
        }

        // Signal the encoding thread to perform the rotation (all encoder ops must be on that thread)
        var request = new ForceRotateRequest();
        ForceRotateRequest? supersededRequest;
        lock (_sync)
        {
            if (!_started || _disposed || _encodingFailure != null || _encodingTask?.IsCompleted == true)
            {
                Logger.Log(
                    $"FLASHBACK_SINK_FORCE_ROTATE_REJECTED_AFTER_LOCK started={_started} disposed={_disposed} " +
                    $"failed={_encodingFailure != null} completed={_encodingTask?.IsCompleted == true} " +
                    $"type={_encodingFailure?.GetType().Name ?? "None"}");
                return FlashbackForceRotateResult.Failed();
            }

            supersededRequest = _forceRotateRequest;
            _forceRotateInPoint = inPoint;
            _forceRotateOutPoint = outPoint;
            _forceRotateRequest = request;
            Volatile.Write(ref _forceRotateRequested, true);
        }

        if (supersededRequest != null)
        {
            Logger.Log("FLASHBACK_SINK_FORCE_ROTATE_SUPERSEDED");
            supersededRequest.TryCancel();
        }

        SignalWork("force_rotate_request");

        // AV1 encoding is significantly slower than H.264/HEVC - drain can take
        // much longer at 4K@120fps with a deep queue. Use a longer timeout for AV1.
        var codecName = _sessionContext?.CodecName ?? string.Empty;
        var isSlowCodec = codecName.Contains("av1", StringComparison.OrdinalIgnoreCase);
        var timeoutSeconds = isSlowCodec ? 10 : 3;
        try
        {
            if (!request.Task.Wait(TimeSpan.FromSeconds(timeoutSeconds), cancellationToken))
            {
                var cancelled = TryCancelForceRotate(request);
                Logger.Log($"FLASHBACK_SINK_FORCE_ROTATE_TIMEOUT codec={codecName} timeout_s={timeoutSeconds} cancelled={cancelled} vq={Volatile.Read(ref _videoQueueDepth)} aq={Volatile.Read(ref _audioQueueDepth)}");
                if (!cancelled)
                {
                    Logger.Log("FLASHBACK_SINK_FORCE_ROTATE_TIMEOUT_COMMITTED");
                    if (request.Task.Wait(TimeSpan.FromMilliseconds(ForceRotateCommittedGraceMs)))
                    {
                        return FlashbackForceRotateResult.Completed(request.Task.GetAwaiter().GetResult());
                    }

                    Logger.Log($"FLASHBACK_SINK_FORCE_ROTATE_TIMEOUT_COMMITTED_PENDING grace_ms={ForceRotateCommittedGraceMs}");
                    return FlashbackForceRotateResult.CommittedPending();
                }

                return FlashbackForceRotateResult.CanceledBeforeCommit();
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            var cancelled = TryCancelForceRotate(request);
            Logger.Log($"FLASHBACK_SINK_FORCE_ROTATE_CANCELLED codec={codecName} cancelled={cancelled} vq={Volatile.Read(ref _videoQueueDepth)} aq={Volatile.Read(ref _audioQueueDepth)}");
            if (!cancelled)
            {
                Logger.Log("FLASHBACK_SINK_FORCE_ROTATE_CANCELLED_COMMITTED");
            }

            throw;
        }

        return FlashbackForceRotateResult.Completed(request.Task.GetAwaiter().GetResult());
    }

}

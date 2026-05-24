using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Sussudio.Models;

namespace Sussudio.Services.Flashback;

internal sealed partial class FlashbackEncoderSink
{
    private bool _forceRotateRequested;
    private volatile ForceRotateRequest? _forceRotateRequest;
    private TimeSpan _forceRotateInPoint;
    private TimeSpan _forceRotateOutPoint;
    private bool _forceRotateDraining;

    private const int ForceRotateCommittedGraceMs = 1_000;

    public bool IsForceRotateActive =>
        Volatile.Read(ref _forceRotateRequested) ||
        Volatile.Read(ref _forceRotateDraining);
    public bool IsForceRotateRequested => Volatile.Read(ref _forceRotateRequested);
    public bool IsForceRotateDraining => Volatile.Read(ref _forceRotateDraining);

    public bool WaitForForceRotateIdle(TimeSpan timeout)
    {
        var timeoutMs = Math.Max(0, (long)timeout.TotalMilliseconds);
        var deadlineTick = Environment.TickCount64 + timeoutMs;
        while (IsForceRotateActive)
        {
            if (timeoutMs == 0 || Environment.TickCount64 >= deadlineTick)
            {
                return false;
            }

            SignalWork("force_rotate_idle");
            if (WaitForCancellation(TimeSpan.FromMilliseconds(10)))
            {
                return false;
            }
        }

        return true;
    }

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

    private bool ProcessPendingForceRotate(
        Channel<VideoFramePacket> videoQueue,
        Channel<AudioSamplePacket> audioQueue,
        Channel<AudioSamplePacket>? microphoneQueue,
        Channel<GpuFramePacket>? gpuQueue)
    {
        ForceRotateRequest? localRequest;
        TimeSpan localIn, localOut;

        // Pause acceptance of new packets to ensure atomicity between drain and rotation.
        // Producers calling Enqueue* will see this flag and drop packets rather than
        // inserting them into the new segment that would be excluded from the export.
        lock (_videoQueueSync)
        {
            Volatile.Write(ref _forceRotateDraining, true);
        }

        lock (_sync)
        {
            _forceRotateRequested = false;
            localRequest = _forceRotateRequest;
            _forceRotateRequest = null;
            localIn = _forceRotateInPoint;
            localOut = _forceRotateOutPoint;
        }
        try
        {
            if (localRequest == null)
            {
                Logger.Log("FLASHBACK_SINK_FORCE_ROTATE_SKIP reason=no_pending_request");
                return true;
            }

            if (localRequest.IsCompleted)
            {
                Logger.Log("FLASHBACK_SINK_FORCE_ROTATE_SKIP reason=request_completed");
                return true;
            }

            // Drain all remaining queued packets into the current segment before rotating.
            // This ensures no data is lost at the live edge.
            var inFlightCount = 0;
            var forceRotateDrainAborted = ShouldAbortForceRotateDrain(localRequest, "before_drain", inFlightCount);
            if (!forceRotateDrainAborted)
            {
                while (DrainAudioPackets(audioQueue.Reader, AudioDrainBatchLimit))
                {
                    inFlightCount++;
                    if (ShouldAbortForceRotateDrain(localRequest, "audio", inFlightCount))
                    {
                        forceRotateDrainAborted = true;
                        break;
                    }
                }

                forceRotateDrainAborted = forceRotateDrainAborted ||
                    ShouldAbortForceRotateDrain(localRequest, "audio", inFlightCount);
            }
            if (!forceRotateDrainAborted && _microphoneEnabled && microphoneQueue != null)
            {
                while (DrainMicrophonePackets(microphoneQueue.Reader, AudioDrainBatchLimit))
                {
                    inFlightCount++;
                    if (ShouldAbortForceRotateDrain(localRequest, "microphone", inFlightCount))
                    {
                        forceRotateDrainAborted = true;
                        break;
                    }
                }

                forceRotateDrainAborted = forceRotateDrainAborted ||
                    ShouldAbortForceRotateDrain(localRequest, "microphone", inFlightCount);
            }
            if (!forceRotateDrainAborted && gpuQueue != null)
            {
                while (DrainGpuPackets(gpuQueue.Reader, GpuDrainBatchLimit))
                {
                    inFlightCount++;
                    if (ShouldAbortForceRotateDrain(localRequest, "gpu", inFlightCount))
                    {
                        forceRotateDrainAborted = true;
                        break;
                    }
                }

                forceRotateDrainAborted = forceRotateDrainAborted ||
                    ShouldAbortForceRotateDrain(localRequest, "gpu", inFlightCount);
            }
            if (!forceRotateDrainAborted)
            {
                while (DrainVideoPackets(videoQueue.Reader, VideoDrainBatchLimit))
                {
                    inFlightCount++;
                    if (ShouldAbortForceRotateDrain(localRequest, "video", inFlightCount))
                    {
                        forceRotateDrainAborted = true;
                        break;
                    }
                }

                forceRotateDrainAborted = forceRotateDrainAborted ||
                    ShouldAbortForceRotateDrain(localRequest, "video", inFlightCount);
            }

            if (inFlightCount > 0)
            {
                Logger.Log($"FLASHBACK_SINK_FORCE_ROTATE_DRAIN in_flight_rounds={inFlightCount}");
            }

            if (forceRotateDrainAborted)
            {
                return true;
            }

            if (localRequest.IsCompleted)
            {
                Logger.Log("FLASHBACK_SINK_FORCE_ROTATE_SKIP reason=request_completed_after_drain");
                return true;
            }

            var currentPts = ResolveEncoderPts();

            if (currentPts > _segmentStartPts)
            {
                if (!localRequest.TryBeginCommit())
                {
                    Logger.Log("FLASHBACK_SINK_FORCE_ROTATE_SKIP reason=request_completed_before_rotate");
                    return true;
                }

                if (!RotateSegment(currentPts))
                {
                    localRequest.CompleteEmpty();
                    return true;
                }
            }

            localRequest.Complete(_bufferManager.GetValidSegmentPaths(localIn, localOut));
            return false;
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_SINK_FORCE_ROTATE_FAIL type={ex.GetType().Name} msg={ex.Message}");
            localRequest?.CompleteEmpty();
            throw;
        }
        finally
        {
            lock (_videoQueueSync)
            {
                Volatile.Write(ref _forceRotateDraining, false);
            }
        }
    }

    private bool TryCancelForceRotate(ForceRotateRequest request)
    {
        lock (_sync)
        {
            if (ReferenceEquals(_forceRotateRequest, request))
            {
                _forceRotateRequested = false;
                _forceRotateRequest = null;
            }
        }

        return request.TryCancel();
    }

    private void CompletePendingForceRotateWithEmptyResult()
    {
        ForceRotateRequest? pendingRequest;
        lock (_sync)
        {
            _forceRotateRequested = false;
            pendingRequest = _forceRotateRequest;
            _forceRotateRequest = null;
        }

        lock (_videoQueueSync)
        {
            Volatile.Write(ref _forceRotateDraining, false);
        }

        pendingRequest?.CompleteEmpty();
    }

    private static bool ShouldAbortForceRotateDrain(
        ForceRotateRequest request,
        string phase,
        int inFlightRounds)
    {
        if (!request.IsCompleted)
        {
            return false;
        }

        Logger.Log($"FLASHBACK_SINK_FORCE_ROTATE_ABORT_DRAIN phase={phase} in_flight_rounds={inFlightRounds}");
        return true;
    }

    private sealed class ForceRotateRequest
    {
        private const int StatePending = 0;
        private const int StateCommitting = 1;
        private const int StateCompleted = 2;
        private const int StateCanceled = 3;

        private int _state = StatePending;

        private readonly TaskCompletionSource<IReadOnlyList<string>> _completion =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<IReadOnlyList<string>> Task => _completion.Task;

        public bool IsCompleted
        {
            get
            {
                var state = Volatile.Read(ref _state);
                return state == StateCompleted ||
                       state == StateCanceled ||
                       _completion.Task.IsCompleted;
            }
        }

        public bool TryBeginCommit()
            => Interlocked.CompareExchange(ref _state, StateCommitting, StatePending) == StatePending;

        public bool TryCancel()
        {
            if (Interlocked.CompareExchange(ref _state, StateCanceled, StatePending) != StatePending)
            {
                return false;
            }

            _completion.TrySetResult(Array.Empty<string>());
            return true;
        }

        public void Complete(IReadOnlyList<string> paths)
        {
            while (true)
            {
                var state = Volatile.Read(ref _state);
                if (state == StateCompleted || state == StateCanceled)
                {
                    return;
                }

                if (Interlocked.CompareExchange(ref _state, StateCompleted, state) == state)
                {
                    _completion.TrySetResult(paths);
                    return;
                }
            }
        }

        public void CompleteEmpty()
            => Complete(Array.Empty<string>());
    }
}

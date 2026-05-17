using System;
using System.Threading;
using System.Threading.Channels;

namespace Sussudio.Services.Flashback;

internal sealed partial class FlashbackEncoderSink
{
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
}

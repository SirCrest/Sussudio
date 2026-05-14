using System;
using System.Threading;

namespace Sussudio.Services.Flashback;

internal sealed partial class FlashbackEncoderSink
{
    private void EncodingLoop(CancellationToken cancellationToken)
    {
        try
        {
            Logger.Log("FLASHBACK_SINK_ENCODING_LOOP_START");
            var videoQueue = _videoQueue ?? throw new InvalidOperationException("Video queue is not initialized.");
            var audioQueue = _audioQueue ?? throw new InvalidOperationException("Audio queue is not initialized.");
            var microphoneQueue = _microphoneQueue;
            var gpuQueue = _gpuQueue;

            while (true)
            {
                var madeProgress = false;

                // Audio FIRST — prevent starvation during slow video encoding
                madeProgress = DrainAudioPackets(audioQueue.Reader) || madeProgress;
                if (_microphoneEnabled && microphoneQueue != null)
                {
                    madeProgress = DrainMicrophonePackets(microphoneQueue.Reader) || madeProgress;
                }

                // Video (existing drain methods, unchanged behavior)
                if (gpuQueue != null)
                {
                    madeProgress = DrainGpuPackets(gpuQueue.Reader, GpuDrainBatchLimit) || madeProgress;
                }
                madeProgress = DrainVideoPackets(videoQueue.Reader, VideoDrainBatchLimit) || madeProgress;

                // Audio AGAIN — catch samples that arrived during video encoding
                madeProgress = DrainAudioPackets(audioQueue.Reader) || madeProgress;
                if (_microphoneEnabled && microphoneQueue != null)
                {
                    madeProgress = DrainMicrophonePackets(microphoneQueue.Reader) || madeProgress;
                }

                // Handle force-rotate requests from the export thread (must run on encoding thread)
                if (Volatile.Read(ref _forceRotateRequested))
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
                            madeProgress = true;
                            continue;
                        }

                        if (localRequest.IsCompleted)
                        {
                            Logger.Log("FLASHBACK_SINK_FORCE_ROTATE_SKIP reason=request_completed");
                            madeProgress = true;
                            continue;
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
                            madeProgress = true;
                            continue;
                        }

                        if (localRequest.IsCompleted)
                        {
                            Logger.Log("FLASHBACK_SINK_FORCE_ROTATE_SKIP reason=request_completed_after_drain");
                            madeProgress = true;
                            continue;
                        }

                        var currentPts = ResolveEncoderPts();

                        if (currentPts > _segmentStartPts)
                        {
                            if (!localRequest.TryBeginCommit())
                            {
                                Logger.Log("FLASHBACK_SINK_FORCE_ROTATE_SKIP reason=request_completed_before_rotate");
                                madeProgress = true;
                                continue;
                            }

                            if (!RotateSegment(currentPts))
                            {
                                localRequest.CompleteEmpty();
                                madeProgress = true;
                                continue;
                            }
                        }

                        localRequest.Complete(_bufferManager.GetValidSegmentPaths(localIn, localOut));
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
                    madeProgress = true;
                }

                if (videoQueue.Reader.Completion.IsCompleted &&
                    audioQueue.Reader.Completion.IsCompleted &&
                    (microphoneQueue == null || microphoneQueue.Reader.Completion.IsCompleted) &&
                    (gpuQueue == null || gpuQueue.Reader.Completion.IsCompleted) &&
                    Volatile.Read(ref _videoQueueDepth) == 0 &&
                    Volatile.Read(ref _audioQueueDepth) == 0 &&
                    Volatile.Read(ref _microphoneQueueDepth) == 0 &&
                    Volatile.Read(ref _gpuQueueDepth) == 0)
                {
                    break;
                }

                if (madeProgress)
                {
                    continue;
                }

                // Reset THEN re-check queues before blocking. This closes the race where
                // a producer calls Set() between our drain loop exit and the Reset() call —
                // the re-check sees the item and loops back without entering Wait().
                _workAvailable.Reset();

                // Re-check all queues after reset to close the TOCTOU window
                if ((videoQueue.Reader.TryPeek(out _)) ||
                    (audioQueue.Reader.TryPeek(out _)) ||
                    (_microphoneEnabled && microphoneQueue != null && microphoneQueue.Reader.TryPeek(out _)) ||
                    (gpuQueue != null && gpuQueue.Reader.TryPeek(out _)) ||
                    Volatile.Read(ref _forceRotateRequested))
                {
                    continue;
                }

                while (!_workAvailable.Wait(50))
                    cancellationToken.ThrowIfCancellationRequested();
            }

            Logger.Log("FLASHBACK_SINK_ENCODING_LOOP_DRAIN_COMPLETE");
            _encoder.FlushAndClose();

            // Register the final active segment
            var finalPts = ResolveEncoderPts();
            if (finalPts > TimeSpan.Zero)
            {
                if (_tsFilePath != null && finalPts > _segmentStartPts)
                {
                    var finalSegmentBytes = NonNegativeByteDelta(_encoder.TotalBytesWritten, Interlocked.Read(ref _segmentStartBytes));
                    _bufferManager.OnSegmentCompleted(_tsFilePath, _segmentStartPts, finalPts, finalSegmentBytes);
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            Logger.Log("FLASHBACK_SINK_ENCODING_LOOP_CANCELLED");
            CompletePendingForceRotateWithEmptyResult();

            // Register the in-progress segment so the buffer index sees the live edge.
            if (_tsFilePath != null)
            {
                try
                {
                    var cancelPts = ResolveEncoderPts();
                    if (cancelPts > _segmentStartPts)
                    {
                        var cancelSegmentBytes = NonNegativeByteDelta(_encoder.TotalBytesWritten, Interlocked.Read(ref _segmentStartBytes));
                        _bufferManager.OnSegmentCompleted(_tsFilePath, _segmentStartPts, cancelPts, cancelSegmentBytes);
                        Logger.Log(
                            $"FLASHBACK_SINK_ENCODING_LOOP_CANCELLED_SEGMENT_REGISTERED " +
                            $"path='{_tsFilePath}' frames={_encoder.VideoPacketsWritten} " +
                            $"start_ms={(long)_segmentStartPts.TotalMilliseconds} end_ms={(long)cancelPts.TotalMilliseconds}");
                    }
                    else
                    {
                        Logger.Log("FLASHBACK_SINK_ENCODING_LOOP_CANCELLED_NO_SEGMENT no frames encoded in current segment");
                    }
                }
                catch (Exception segmentEx)
                {
                    Logger.Log($"FLASHBACK_SINK_CANCELLED_SEGMENT_REGISTER_FAIL type={segmentEx.GetType().Name} msg={segmentEx.Message}");
                }
            }
            else
            {
                Logger.Log("FLASHBACK_SINK_ENCODING_LOOP_CANCELLED_NO_SEGMENT tsFilePath is null");
            }

            ReturnAllRemainingQueuedBuffers();
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_SINK_ENCODING_LOOP_FATAL type={ex.GetType().Name} msg={ex.Message}");
            _encodingFailure = ex;
            CompletePendingForceRotateWithEmptyResult();
            lock (_sync) { _started = false; }

            // Notify the owning service so it can surface the failure
            try { _onFatalError?.Invoke(ex); }
            catch (Exception callbackEx)
            {
                Logger.Log($"FLASHBACK_SINK_FATAL_CALLBACK_FAIL type={callbackEx.GetType().Name} msg={callbackEx.Message}");
            }

            // Register the active segment so PurgeAllSegments can clean it up
            if (_tsFilePath != null)
            {
                try
                {
                    var crashPts = ResolveEncoderPts();
                    if (crashPts > _segmentStartPts)
                    {
                        var crashSegmentBytes = NonNegativeByteDelta(_encoder.TotalBytesWritten, Interlocked.Read(ref _segmentStartBytes));
                        _bufferManager.OnSegmentCompleted(_tsFilePath, _segmentStartPts, crashPts, crashSegmentBytes);
                    }
                }
                catch (Exception segmentEx)
                {
                    Logger.Log($"FLASHBACK_SINK_FATAL_SEGMENT_REGISTER_FAIL type={segmentEx.GetType().Name} msg={segmentEx.Message}");
                    // Preserve the original fatal error.
                }
            }

            ReturnAllRemainingQueuedBuffers();
            DisposeEncoderBestEffort("encoding_loop_fatal");
        }
    }

}

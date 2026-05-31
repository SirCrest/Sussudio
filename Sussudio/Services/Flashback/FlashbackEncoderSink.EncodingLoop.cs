using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.Services.Capture;

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
                    if (ProcessPendingForceRotate(videoQueue, audioQueue, microphoneQueue, gpuQueue))
                    {
                        madeProgress = true;
                        continue;
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

    private bool DrainVideoPackets(ChannelReader<VideoFramePacket> reader, int maxPackets = int.MaxValue)
    {
        var drainedAny = false;
        var w = _width;
        var h = _height;
        if (w <= 0 || h <= 0)
        {
            return false;
        }

        var drainedCount = 0;
        while (drainedCount < maxPackets)
        {
            VideoFramePacket packet;
            lock (_videoQueueSync)
            {
                if (!reader.TryRead(out packet))
                {
                    break;
                }

                _videoLatencyTracker.TrackDequeueUnderLock(packet.EnqueueTick);
                DecrementQueueDepth(ref _videoQueueDepth, "video");
            }

            _videoLatencyTracker.RecordPacketDequeued(packet.EnqueueTick, packet.SequenceNumber);
            try
            {
                var expectedFrameSize = MfSourceReaderVideoCapture.GetFrameSizeBytes(w, h, packet.IsP010);
                // Defense-in-depth: if a stale frame from a previous resolution
                // leaks through during a reinit cycle, drop it rather than sending
                // mismatched dimensions to the encoder, which could crash in native code.
                if (expectedFrameSize > 0 && packet.Length != expectedFrameSize)
                {
                    Interlocked.Increment(ref _droppedVideoFrames);
                    Logger.Log($"FLASHBACK_SINK_FRAME_SIZE_MISMATCH expected={expectedFrameSize} actual={packet.Length} w={w} h={h} p010={packet.IsP010}");
                    continue;
                }

                var frameData = packet.Lease != null
                    ? packet.Lease.Memory.Span
                    : packet.Buffer!.AsSpan(0, packet.Length);
                _encoder.SendVideoFrame(frameData, w, h);
                Interlocked.Increment(ref _videoFramesSubmittedToEncoder);
                OnVideoFrameEncoded();
            }
            finally
            {
                ReturnVideoPacket(packet);
            }

            drainedAny = true;
            drainedCount++;
        }

        return drainedAny;
    }

    private bool DrainGpuPackets(ChannelReader<GpuFramePacket> reader, int maxPackets = int.MaxValue)
    {
        var drainedAny = false;
        var drainedCount = 0;
        while (drainedCount < maxPackets && reader.TryRead(out var packet))
        {
            DecrementQueueDepth(ref _gpuQueueDepth, "gpu");
            try
            {
                _encoder.SendGpuVideoFrame(packet.Texture, packet.Subresource);
                Interlocked.Increment(ref _videoFramesSubmittedToEncoder);
                OnVideoFrameEncoded();
            }
            finally
            {
                ReleaseGpuTextureBestEffort(packet.Texture);
            }

            drainedAny = true;
            drainedCount++;
        }

        return drainedAny;
    }

    private bool DrainAudioPackets(ChannelReader<AudioSamplePacket> reader, int maxPackets = int.MaxValue)
    {
        var drainedAny = false;
        var drainedCount = 0;
        while (drainedCount < maxPackets && reader.TryRead(out var packet))
        {
            DecrementQueueDepth(ref _audioQueueDepth, "audio");
            try
            {
                _encoder.SendAudioSamples(packet.Buffer.AsSpan(0, packet.Length));
            }
            finally
            {
                ReturnBuffer(packet.Buffer);
            }

            drainedAny = true;
            drainedCount++;
        }

        return drainedAny;
    }

    private bool DrainMicrophonePackets(ChannelReader<AudioSamplePacket> reader, int maxPackets = int.MaxValue)
    {
        var drainedAny = false;
        var drainedCount = 0;
        while (drainedCount < maxPackets && reader.TryRead(out var packet))
        {
            DecrementQueueDepth(ref _microphoneQueueDepth, "microphone");
            try
            {
                _encoder.SendMicrophoneSamples(packet.Buffer.AsSpan(0, packet.Length));
            }
            finally
            {
                ReturnBuffer(packet.Buffer);
            }

            drainedAny = true;
            drainedCount++;
        }

        return drainedAny;
    }

    private void OnVideoFrameEncoded()
    {
        if (_disposed)
        {
            return;
        }

        Interlocked.Exchange(ref _lastVideoWriteTick, Environment.TickCount64);
        var encoded = Interlocked.Increment(ref _encodedVideoFrames);

        var pts = ResolveEncoderPts();
        if (pts > TimeSpan.Zero)
        {
            _bufferManager.UpdateLatestPts(pts);

            // Segment rotation happens on the encoding thread, so no extra lock is needed here.
            if (_segmentDuration > TimeSpan.Zero && pts - _segmentStartPts >= _segmentDuration)
            {
                _ = RotateSegment(pts);
            }
        }

        // Refresh disk bytes ~4 Hz so the monotonic counter stays current for UI
        // bitrate sampling; the prior frame-count gate plateaued for ~5 s at 60 fps.
        var nowMs = Environment.TickCount64;
        if (nowMs - _lastDiskBytesUpdateMs >= 250)
        {
            _lastDiskBytesUpdateMs = nowMs;
            _bufferManager.UpdateDiskBytes(_encoder.TotalBytesWritten);
        }

        // NOTE: This event fires on the encoding background thread, NOT the UI thread.
        // Handlers must marshal to DispatcherQueue if they need to update UI state.
        if (!_disposed && Volatile.Read(ref _recordingActive) == 1)
        {
            try
            {
                FrameEncoded?.Invoke(this, encoded);
            }
            catch (Exception ex)
            {
                Logger.Log($"FLASHBACK_SINK_FRAME_EVENT_FAIL type={ex.GetType().Name} msg={ex.Message}");
            }
        }
    }

    private TimeSpan ResolveEncoderPts()
    {
        var frameRate = ResolveSessionFrameRate(_sessionContext?.FrameRate ?? 30.0);
        var seconds = _encoder.NextVideoPts / frameRate;
        if (!double.IsFinite(seconds) || seconds <= 0)
        {
            return _ptsBaseOffset;
        }

        if (seconds >= TimeSpan.MaxValue.TotalSeconds)
        {
            return TimeSpan.MaxValue;
        }

        var delta = TimeSpan.FromSeconds(seconds);
        return _ptsBaseOffset > TimeSpan.MaxValue - delta
            ? TimeSpan.MaxValue
            : _ptsBaseOffset + delta;
    }

    private bool RotateSegment(TimeSpan currentPts)
    {
        string? completedPath = null;
        string? newPath = null;
        var encoderRotated = false;
        try
        {
            completedPath = _tsFilePath;
            var completedStartPts = _segmentStartPts;
            newPath = _bufferManager.GenerateSegmentPath();

            // RotateOutput flushes encoder queues, writes trailer, then resets
            // TotalBytesWritten to 0 for the new segment. PreviousTotalBytes
            // in the result includes all drain/trailer bytes.
            var result = _encoder.RotateOutput(newPath);
            var segmentBytes = NonNegativeByteDelta(result.PreviousTotalBytes, Interlocked.Read(ref _segmentStartBytes));
            encoderRotated = true;

            _segmentStartPts = currentPts;
            _tsFilePath = newPath;
            _bufferManager.MarkActiveSegmentStart(newPath, _segmentStartPts);
            Interlocked.Exchange(ref _segmentStartBytes, _encoder.TotalBytesWritten);

            _bufferManager.OnSegmentCompleted(completedPath!, completedStartPts, currentPts, segmentBytes);

            // Update disk bytes tracking.
            _bufferManager.UpdateDiskBytes(_encoder.TotalBytesWritten);
            _lastDiskBytesUpdateMs = Environment.TickCount64;

            Logger.Log(
                $"FLASHBACK_SINK_ROTATE new_segment='{Path.GetFileName(newPath)}' " +
                $"prev_bytes={segmentBytes} " +
                $"segment_start_ms={(long)currentPts.TotalMilliseconds}");
            return true;
        }
        catch (Exception ex)
        {
            if (newPath != null && !encoderRotated)
            {
                _bufferManager.AbandonGeneratedSegmentPath(newPath, completedPath);
            }

            Interlocked.Increment(ref _segmentRotationFailures);

            // Register the segment that was open before the rotation attempt so its
            // data remains visible in the buffer index even though rotation failed.
            if (completedPath != null)
            {
                try
                {
                    var failPts = ResolveEncoderPts();
                    if (failPts > _segmentStartPts)
                    {
                        var failSegmentBytes = NonNegativeByteDelta(_encoder.TotalBytesWritten, Interlocked.Read(ref _segmentStartBytes));
                        _bufferManager.OnSegmentCompleted(completedPath, _segmentStartPts, failPts, failSegmentBytes);
                        Logger.Log(
                            $"FLASHBACK_SINK_ROTATE_FAIL_SEGMENT_REGISTERED " +
                            $"path='{completedPath}' frames={_encoder.VideoPacketsWritten} " +
                            $"start_ms={(long)_segmentStartPts.TotalMilliseconds} end_ms={(long)failPts.TotalMilliseconds}");
                    }
                }
                catch (Exception segmentEx)
                {
                    Logger.Log($"FLASHBACK_SINK_ROTATE_FAIL_SEGMENT_REGISTER_FAIL type={segmentEx.GetType().Name} msg={segmentEx.Message}");
                }
            }

            // Advance _segmentStartPts to prevent infinite retry on every frame.
            _segmentStartPts = currentPts;
            Logger.Log($"FLASHBACK_SINK_ROTATE_FAIL type={ex.GetType().Name} msg={ex.Message}");
            return false;
        }
    }

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

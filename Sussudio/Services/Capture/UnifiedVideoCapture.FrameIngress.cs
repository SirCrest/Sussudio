using System;
using System.Diagnostics;
using System.Threading;
using Sussudio.Models;
using Sussudio.Services.Contracts;
using Sussudio.Services.Flashback;
using Sussudio.Services.Gpu;
using Sussudio.Services.Preview;
using Sussudio.Services.Recording;
using Sussudio.Services.Runtime;

namespace Sussudio.Services.Capture;

internal sealed partial class UnifiedVideoCapture
{
    private void OnFrameArrived(ReadOnlySpan<byte> frameData, int width, int height, long arrivalTick)
    {
        var sourceSequence = Interlocked.Increment(ref _videoFramesArrived) - 1;
        Interlocked.Exchange(ref _lastVideoFrameArrivedTick, Environment.TickCount64);
        RecordCaptureArrived(sourceSequence, arrivalTick, width, height, frameData.Length);

        var pipeline = Volatile.Read(ref _mjpegPipeline);
        if (pipeline != null)
        {
            var accepted = pipeline.EnqueueFrame(frameData, width, height, arrivalTick);
            _frameLedger.RecordEvent(
                sourceSequence,
                FrameLedgerStage.CompressedQueued,
                subsystem: "mjpeg",
                byteDepth: frameData.Length,
                accepted: accepted,
                reason: accepted ? null : "mjpeg_queue_rejected");
            return;
        }

        var isP010 = Volatile.Read(ref _isP010);
        FirePixelFormatObserverOnce(isP010 ? "P010" : "NV12");

        EnqueueRecordingFrame(frameData, width, height, isP010, sourceSequence);
        EnqueueFlashbackFrame(frameData, width, height, isP010, sourceSequence);

        var previewSink = Volatile.Read(ref _previewSink);
        if (!_previewSuppressed && previewSink != null && !frameData.IsEmpty)
        {
            SubmitPreviewRawFrame(previewSink, frameData, width, height, isP010, arrivalTick, sourceSequence);
        }
    }

    private void OnMjpegPipelineFrameEmitted(PooledVideoFrame frame)
    {
        FirePixelFormatObserverOnce("NV12");
        _frameLedger.RecordEvent(
            frame.SequenceNumber,
            FrameLedgerStage.StrictOrderReleased,
            subsystem: "mjpeg");

        EnqueueRecordingFrame(frame);
        EnqueueFlashbackFrame(frame);
    }

    private void OnDualFrameArrived(
        IntPtr gpuTexture,
        int gpuSubresource,
        ReadOnlySpan<byte> frameData,
        int width,
        int height,
        long arrivalTick)
    {
        var sourceSequence = Interlocked.Increment(ref _videoFramesArrived) - 1;
        Interlocked.Exchange(ref _lastVideoFrameArrivedTick, Environment.TickCount64);
        RecordCaptureArrived(sourceSequence, arrivalTick, width, height, frameData.Length);

        var isP010 = Volatile.Read(ref _isP010);
        FirePixelFormatObserverOnce(isP010 ? "P010" : "NV12");

        var gpuEncoder = Volatile.Read(ref _gpuRecordingEncoder);
        if (gpuEncoder != null && gpuTexture != IntPtr.Zero)
        {
            EnqueueGpuRecordingFrame(gpuEncoder, gpuTexture, gpuSubresource, sourceSequence);
        }
        else
        {
            EnqueueRecordingFrame(frameData, width, height, isP010, sourceSequence);
        }

        if (gpuTexture != IntPtr.Zero)
        {
            EnqueueFlashbackGpuFrame(gpuTexture, gpuSubresource, sourceSequence);
        }
        else
        {
            EnqueueFlashbackFrame(frameData, width, height, isP010, sourceSequence);
        }

        var previewSink = Volatile.Read(ref _previewSink);
        if (!_previewSuppressed && previewSink != null)
        {
            var textureSubmitted = false;
            if (gpuTexture != IntPtr.Zero)
            {
                try
                {
                    var previewPresentId = Interlocked.Increment(ref _livePreviewPresentId);
                    var submitTick = Stopwatch.GetTimestamp();
                    previewSink.SubmitTexture(
                        gpuTexture,
                        gpuSubresource,
                        width,
                        height,
                        isP010,
                        PreviewFrameTracking.Default with
                        {
                            ArrivalTick = arrivalTick,
                            SourceSequenceNumber = sourceSequence,
                            PreviewPresentId = previewPresentId,
                            SchedulerSubmitTick = submitTick,
                        });
                    _frameLedger.RecordEvent(
                        sourceSequence,
                        FrameLedgerStage.PreviewEnqueued,
                        subsystem: "preview",
                        accepted: true);
                    textureSubmitted = true;
                    Interlocked.Exchange(ref _consecutiveTextureFailures, 0);
                    if (!frameData.IsEmpty)
                    {
                        TrackPreviewVisualFrame(
                            frameData,
                            width,
                            height,
                            isP010 ? PooledVideoPixelFormat.P010 : PooledVideoPixelFormat.Nv12,
                            arrivalTick,
                            sequenceNumber: sourceSequence);
                    }
                    else
                    {
                        MarkPreviewVisualCadenceUnavailable("d3d_texture_only");
                    }
                }
                catch (Exception ex)
                {
                    _frameLedger.RecordEvent(
                        sourceSequence,
                        FrameLedgerStage.PreviewEnqueued,
                        subsystem: "preview",
                        accepted: false,
                        reason: "texture_submit_exception");
                    Logger.Log($"UNIFIED_VIDEO_PREVIEW_TEXTURE_FAIL type={ex.GetType().Name} msg={ex.Message}");
                }
            }

            if (!textureSubmitted &&
                Volatile.Read(ref _strictPreviewTextureRequired))
            {
                var failures = Interlocked.Increment(ref _consecutiveTextureFailures);
                Interlocked.Increment(ref _videoFramesDropped);

                if (failures >= MaxConsecutiveTextureFailures)
                {
                    SignalFatalError(
                        new InvalidOperationException(
                            $"4K120 MJPG mode requires D3D preview textures, but texture delivery failed {failures} consecutive times for native_input='{_nativeInputFormat}' negotiated='{_negotiatedFormat}'."),
                        $"UNIFIED_VIDEO_PREVIEW_TEXTURE_REQUIRED consecutive={failures} " +
                        $"native_input='{_nativeInputFormat}' negotiated='{_negotiatedFormat}'");
                }
                else
                {
                    Logger.Log($"UNIFIED_VIDEO_PREVIEW_TEXTURE_GRACE consecutive={failures}/{MaxConsecutiveTextureFailures}");
                }
            }
            else if (!textureSubmitted && !frameData.IsEmpty)
            {
                SubmitPreviewRawFrame(previewSink, frameData, width, height, isP010, arrivalTick, sourceSequence);
            }
        }
    }

    private void RecordCaptureArrived(long sourceSequence, long arrivalTick, int width, int height, int compressedByteLength)
    {
        _frameLedger.RecordCaptureArrived(new FrameIdentity(
            SourceSequence: sourceSequence,
            CaptureArrivalQpc: arrivalTick,
            DeviceTimestamp100ns: null,
            InputFormat: Volatile.Read(ref _nativeInputFormat),
            Width: width,
            Height: height,
            FrameRateNominal: Volatile.Read(ref _fps),
            CompressedByteLength: compressedByteLength));
    }

    private void FirePixelFormatObserverOnce(string format)
    {
        if (Interlocked.CompareExchange(ref _pixelFormatObserverFired, 1, 0) != 0)
        {
            return;
        }

        Volatile.Read(ref _pixelFormatDetectedCallback)?.Invoke(format);
    }

    private void SignalFatalError(Exception ex, string logMessage)
    {
        Logger.Log(logMessage);

        if (Interlocked.Exchange(ref _fatalErrorSignaled, 1) != 0)
        {
            return;
        }

        try
        {
            FatalErrorOccurred?.Invoke(this, ex);
        }
        catch (Exception callbackEx)
        {
            Logger.Log($"UNIFIED_VIDEO_FATAL_CALLBACK_FAIL type={callbackEx.GetType().Name} msg={callbackEx.Message}");
        }
    }

    public void SetPreviewSink(IPreviewFrameSink? sink)
    {
        Volatile.Write(ref _previewSink, sink);
    }

    public void SuppressPreviewSubmission()
    {
        // Flashback playback temporarily owns the preview renderer. Drain
        // pending live frames so an old live texture cannot flash over a
        // scrub/playback frame when presentation mode changes.
        _previewSuppressed = true;
        Volatile.Read(ref _mjpegPreviewJitterBuffer)?.ResetForPreviewSuppression();
        DropPendingPreviewFrames("live-preview-suppressed");
    }

    public void ResumePreviewSubmission()
    {
        // Drop before clearing suppression so the first resumed frame is a new
        // live source frame, not stale queue residue from the playback period.
        DropPendingPreviewFrames("live-preview-resumed");
        _previewSuppressed = false;
        Volatile.Read(ref _mjpegPreviewJitterBuffer)?.ReprimeAfterPreviewResume();
    }

    private void DropPendingPreviewFrames(string reason)
    {
        if (Volatile.Read(ref _previewSink) is not IPreviewFrameQueueControl queueControl)
        {
            return;
        }

        try
        {
            var dropped = queueControl.DropPendingFrames(reason);
            if (dropped > 0)
            {
                Logger.Log($"UNIFIED_VIDEO_PREVIEW_PENDING_DRAIN reason={reason} dropped={dropped}");
            }
        }
        catch (Exception ex)
        {
            Logger.Log($"UNIFIED_VIDEO_PREVIEW_PENDING_DRAIN_WARN reason={reason} type={ex.GetType().Name} msg='{ex.Message}'");
        }
    }

    private void OnMjpegPipelinePreviewFrameDecoded(PooledVideoFrameLease frame)
    {
        ArgumentNullException.ThrowIfNull(frame);
        FirePixelFormatObserverOnce("NV12");
        _frameLedger.RecordEvent(
            frame.SequenceNumber,
            FrameLedgerStage.PreviewEnqueued,
            subsystem: "preview",
            accepted: true);
        TrackPreviewVisualFrame(
            frame.Memory.Span,
            frame.Width,
            frame.Height,
            frame.PixelFormat,
            frame.ArrivalTick,
            frame.SequenceNumber);

        var previewSink = Volatile.Read(ref _previewSink);
        var jitterBuffer = Volatile.Read(ref _mjpegPreviewJitterBuffer);
        if (_previewSuppressed || previewSink == null)
        {
            jitterBuffer?.Clear();
            frame.Dispose();
            return;
        }

        if (jitterBuffer != null)
        {
            jitterBuffer.Enqueue(frame);
            return;
        }

        PooledVideoFrameLease? ownedFrame = frame;
        try
        {
            var previewPresentId = Interlocked.Increment(ref _livePreviewPresentId);
            var submitTick = Stopwatch.GetTimestamp();
            previewSink.SubmitRawFrameLease(
                ownedFrame,
                isHdr: false,
                PreviewFrameTracking.Default with
                {
                    PreviewPresentId = previewPresentId,
                    SchedulerSubmitTick = submitTick,
                });
            ownedFrame = null;
        }
        finally
        {
            ownedFrame?.Dispose();
        }
    }

    private unsafe void SubmitPreviewRawFrame(
        IPreviewFrameSink previewSink,
        ReadOnlySpan<byte> frameData,
        int width,
        int height,
        bool isP010,
        long arrivalTick,
        long sourceSequence)
    {
        try
        {
            TrackPreviewVisualFrame(
                frameData,
                width,
                height,
                isP010 ? PooledVideoPixelFormat.P010 : PooledVideoPixelFormat.Nv12,
                arrivalTick,
                sequenceNumber: sourceSequence);
            fixed (byte* pointer = frameData)
            {
                var previewPresentId = Interlocked.Increment(ref _livePreviewPresentId);
                var submitTick = Stopwatch.GetTimestamp();
                previewSink.SubmitRawFrame(
                    (IntPtr)pointer,
                    frameData.Length,
                    width,
                    height,
                    isP010,
                    PreviewFrameTracking.Default with
                    {
                        ArrivalTick = arrivalTick,
                        SourceSequenceNumber = sourceSequence,
                        PreviewPresentId = previewPresentId,
                        SchedulerSubmitTick = submitTick,
                    });
            }
            _frameLedger.RecordEvent(
                sourceSequence,
                FrameLedgerStage.PreviewEnqueued,
                subsystem: "preview",
                byteDepth: frameData.Length,
                accepted: true);
        }
        catch (Exception ex)
        {
            Interlocked.Increment(ref _videoFramesDropped);
            _frameLedger.RecordEvent(
                sourceSequence,
                FrameLedgerStage.PreviewEnqueued,
                subsystem: "preview",
                byteDepth: frameData.Length,
                accepted: false,
                reason: "exception");
            Logger.Log($"UNIFIED_VIDEO_PREVIEW_FRAME_FAIL type={ex.GetType().Name} msg={ex.Message}");
        }
    }

    private void TrackPreviewVisualFrame(
        ReadOnlySpan<byte> frameData,
        int width,
        int height,
        PooledVideoPixelFormat pixelFormat,
        long arrivalTick,
        long sequenceNumber)
    {
        try
        {
            Interlocked.Exchange(ref _visualCadenceCpuDataUnavailable, 0);
            _visualCadenceTracker.RecordFrame(
                frameData,
                width,
                height,
                pixelFormat,
                timestampTick: arrivalTick);
            _visualCenterCadenceTracker.RecordFrame(
                frameData,
                width,
                height,
                pixelFormat,
                timestampTick: arrivalTick);
        }
        catch (Exception ex)
        {
            Logger.Log(
                $"UNIFIED_VIDEO_VISUAL_CADENCE_FAIL type={ex.GetType().Name} " +
                $"msg={ex.Message} width={width} height={height} fmt={pixelFormat} seq={sequenceNumber}");
        }
    }

    private void MarkPreviewVisualCadenceUnavailable(string reason)
    {
        if (Interlocked.CompareExchange(ref _visualCadenceCpuDataUnavailable, 1, 0) != 0)
        {
            return;
        }

        _visualCadenceTracker.Reset();
        _visualCenterCadenceTracker.Reset();
        Logger.Log($"UNIFIED_VIDEO_VISUAL_CADENCE_UNAVAILABLE reason={reason}");
    }

    private void RecordRecordingEnqueue(long sourceSequence, bool accepted, string? reason)
    {
        _frameLedger.RecordEvent(
            sourceSequence,
            FrameLedgerStage.RecordingEnqueued,
            subsystem: "recording",
            accepted: accepted,
            reason: reason);
    }

    private void EnqueueRecordingFrame(ReadOnlySpan<byte> frameData, int width, int height, bool isP010, long sourceSequence)
    {
        if (!Volatile.Read(ref _recordingActive))
        {
            return;
        }

        var encoder = Volatile.Read(ref _recordingEncoder);
        if (encoder == null)
        {
            return;
        }

        Interlocked.Increment(ref _recordingFramesDelivered);

        try
        {
            var expectedSize = MfSourceReaderVideoCapture.GetFrameSizeBytes(width, height, isP010);
            if (frameData.Length < expectedSize)
            {
                Interlocked.Increment(ref _videoFramesDropped);
                RecordRecordingEnqueue(sourceSequence, accepted: false, reason: "frame_size_mismatch");
                Logger.Log(
                    "UNIFIED_VIDEO_FRAME_SIZE_MISMATCH " +
                    $"expected={expectedSize} actual={frameData.Length} width={width} height={height} isP010={isP010}");
                return;
            }

            var accepted = encoder is IRawVideoFrameTryEncoder tryEncoder
                ? tryEncoder.TryEnqueueRawVideoFrame(frameData, expectedSize)
                : TryLegacyRawVideoEnqueue(encoder, frameData, expectedSize);
            if (accepted)
            {
                Interlocked.Increment(ref _videoFramesWrittenToSink);
            }
            RecordRecordingEnqueue(sourceSequence, accepted, accepted ? null : "queue_rejected");
        }
        catch (Exception ex)
        {
            Interlocked.Increment(ref _videoFramesDropped);
            RecordRecordingEnqueue(sourceSequence, accepted: false, reason: "exception");
            Logger.Log($"UNIFIED_VIDEO_RECORDING_FRAME_FAIL type={ex.GetType().Name} msg={ex.Message}");
        }
    }

    private void EnqueueRecordingFrame(PooledVideoFrame frame)
    {
        if (!Volatile.Read(ref _recordingActive))
        {
            return;
        }

        var encoder = Volatile.Read(ref _recordingEncoder);
        if (encoder == null)
        {
            return;
        }

        Interlocked.Increment(ref _recordingFramesDelivered);

        try
        {
            var expectedSize = MfSourceReaderVideoCapture.GetFrameSizeBytes(frame.Width, frame.Height, isP010: false);
            if (frame.Length < expectedSize)
            {
                Interlocked.Increment(ref _videoFramesDropped);
                RecordRecordingEnqueue(frame.SequenceNumber, accepted: false, reason: "frame_size_mismatch");
                Logger.Log(
                    "UNIFIED_VIDEO_FRAME_SIZE_MISMATCH " +
                    $"expected={expectedSize} actual={frame.Length} width={frame.Width} height={frame.Height} isP010=false");
                return;
            }

            if (encoder is IRawVideoFrameLeaseEncoder leaseEncoder &&
                frame.TryAddLease(out var lease))
            {
                try
                {
                    var accepted = leaseEncoder is IRawVideoFrameLeaseTryEncoder leaseTryEncoder
                        ? leaseTryEncoder.TryEnqueueRawVideoFrame(lease!)
                        : TryLegacyLeaseVideoEnqueue(leaseEncoder, lease!);
                    lease = null;
                    if (accepted)
                    {
                        Interlocked.Increment(ref _videoFramesWrittenToSink);
                    }
                    RecordRecordingEnqueue(frame.SequenceNumber, accepted, accepted ? null : "queue_rejected");
                }
                finally
                {
                    lease?.Dispose();
                }
            }
            else
            {
                var accepted = encoder is IRawVideoFrameTryEncoder tryEncoder
                    ? tryEncoder.TryEnqueueRawVideoFrame(frame.Memory.Span, expectedSize)
                    : TryLegacyRawVideoEnqueue(encoder, frame.Memory.Span, expectedSize);
                if (accepted)
                {
                    Interlocked.Increment(ref _videoFramesWrittenToSink);
                }
                RecordRecordingEnqueue(frame.SequenceNumber, accepted, accepted ? null : "queue_rejected");
            }
        }
        catch (Exception ex)
        {
            Interlocked.Increment(ref _videoFramesDropped);
            RecordRecordingEnqueue(frame.SequenceNumber, accepted: false, reason: "exception");
            Logger.Log($"UNIFIED_VIDEO_RECORDING_FRAME_FAIL type={ex.GetType().Name} msg={ex.Message}");
        }
    }

    private void EnqueueGpuRecordingFrame(IGpuVideoFrameEncoder encoder, IntPtr texture, int subresource, long sourceSequence)
    {
        Interlocked.Increment(ref _recordingFramesDelivered);
        try
        {
            var accepted = encoder is IGpuVideoFrameTryEncoder tryEncoder
                ? tryEncoder.TryEnqueueGpuVideoFrame(texture, subresource)
                : TryLegacyGpuVideoEnqueue(encoder, texture, subresource);
            if (accepted)
            {
                Interlocked.Increment(ref _videoFramesWrittenToSink);
            }
            RecordRecordingEnqueue(sourceSequence, accepted, accepted ? null : "queue_rejected");
        }
        catch (Exception ex)
        {
            Interlocked.Increment(ref _videoFramesDropped);
            RecordRecordingEnqueue(sourceSequence, accepted: false, reason: "exception");
            Logger.Log($"UNIFIED_VIDEO_GPU_RECORDING_FAIL type={ex.GetType().Name} msg={ex.Message}");
        }
    }

    private static bool TryLegacyRawVideoEnqueue(IRawVideoFrameEncoder encoder, ReadOnlySpan<byte> frameData, int expectedSize)
    {
        encoder.EnqueueRawVideoFrame(frameData, expectedSize);
        return true;
    }

    private static bool TryLegacyLeaseVideoEnqueue(IRawVideoFrameLeaseEncoder encoder, PooledVideoFrameLease frame)
    {
        encoder.EnqueueRawVideoFrame(frame);
        return true;
    }

    private static bool TryLegacyGpuVideoEnqueue(IGpuVideoFrameEncoder encoder, IntPtr texture, int subresource)
    {
        encoder.EnqueueGpuVideoFrame(texture, subresource);
        return true;
    }

    private void RecordFlashbackEnqueue(long sourceSequence, bool accepted, string? reason)
    {
        _frameLedger.RecordEvent(
            sourceSequence,
            FrameLedgerStage.FlashbackEnqueued,
            subsystem: "flashback",
            accepted: accepted,
            reason: reason);
    }

    private void EnqueueFlashbackFrame(ReadOnlySpan<byte> frameData, int width, int height, bool isP010, long sourceSequence)
    {
        var sink = Volatile.Read(ref _flashbackSink);
        if (sink == null)
        {
            return;
        }

        try
        {
            var expectedSize = MfSourceReaderVideoCapture.GetFrameSizeBytes(width, height, isP010);
            if (frameData.Length < expectedSize)
            {
                RecordFlashbackRecordingAccounting(sink, accepted: false, sourceSequence);
                RecordFlashbackEnqueue(sourceSequence, accepted: false, reason: "frame_size_mismatch");
                return;
            }

            var accepted = sink.TryEnqueueRawVideoFrame(frameData, expectedSize);
            RecordFlashbackRecordingAccounting(sink, accepted, sourceSequence);
            RecordFlashbackEnqueue(sourceSequence, accepted, accepted ? null : "queue_rejected");
        }
        catch (Exception ex)
        {
            RecordFlashbackEnqueue(sourceSequence, accepted: false, reason: "exception");
            Logger.Log($"UNIFIED_VIDEO_FLASHBACK_FRAME_FAIL type={ex.GetType().Name} msg={ex.Message}");
        }
    }

    private void EnqueueFlashbackFrame(PooledVideoFrame frame)
    {
        var sink = Volatile.Read(ref _flashbackSink);
        if (sink == null)
        {
            return;
        }

        try
        {
            if (sink is IRawVideoFrameLeaseEncoder leaseEncoder &&
                frame.TryAddLease(out var lease))
            {
                try
                {
                    var accepted = leaseEncoder is IRawVideoFrameLeaseTryEncoder leaseTryEncoder
                        ? leaseTryEncoder.TryEnqueueRawVideoFrame(lease!)
                        : TryLegacyLeaseVideoEnqueue(leaseEncoder, lease!);
                    lease = null;
                    RecordFlashbackRecordingAccounting(sink, accepted, frame.SequenceNumber);
                    RecordFlashbackEnqueue(frame.SequenceNumber, accepted, accepted ? null : "queue_rejected");
                }
                finally
                {
                    lease?.Dispose();
                }

                return;
            }

            var expectedSize = MfSourceReaderVideoCapture.GetFrameSizeBytes(frame.Width, frame.Height, isP010: false);
            if (frame.Length < expectedSize)
            {
                RecordFlashbackRecordingAccounting(sink, accepted: false, frame.SequenceNumber);
                RecordFlashbackEnqueue(frame.SequenceNumber, accepted: false, reason: "frame_size_mismatch");
                return;
            }

            var rawAccepted = sink.TryEnqueueRawVideoFrame(frame.Memory.Span, expectedSize);
            RecordFlashbackRecordingAccounting(sink, rawAccepted, frame.SequenceNumber);
            RecordFlashbackEnqueue(frame.SequenceNumber, rawAccepted, rawAccepted ? null : "queue_rejected");
        }
        catch (Exception ex)
        {
            RecordFlashbackEnqueue(frame.SequenceNumber, accepted: false, reason: "exception");
            Logger.Log($"UNIFIED_VIDEO_FLASHBACK_FRAME_FAIL type={ex.GetType().Name} msg={ex.Message}");
        }
    }

    private void EnqueueFlashbackGpuFrame(IntPtr texture, int subresource, long sourceSequence)
    {
        var sink = Volatile.Read(ref _flashbackSink);
        if (sink == null)
        {
            return;
        }

        try
        {
            var accepted = sink.TryEnqueueGpuVideoFrame(texture, subresource);
            RecordFlashbackRecordingAccounting(sink, accepted, sourceSequence);
            RecordFlashbackEnqueue(sourceSequence, accepted, accepted ? null : "queue_rejected");
        }
        catch (Exception ex)
        {
            RecordFlashbackEnqueue(sourceSequence, accepted: false, reason: "exception");
            Logger.Log($"UNIFIED_VIDEO_FLASHBACK_GPU_FAIL type={ex.GetType().Name} msg={ex.Message}");
        }
    }

    private void RecordFlashbackRecordingAccounting(FlashbackEncoderSink sink, bool accepted, long sourceSequence)
    {
        if (!Volatile.Read(ref _flashbackRecordingAccountingActive) ||
            !sink.IsRecordingActive)
        {
            return;
        }

        Interlocked.Increment(ref _recordingFramesDelivered);
        if (accepted)
        {
            TrackFlashbackRecordingAcceptedSequence(sourceSequence);
            Interlocked.Increment(ref _videoFramesWrittenToSink);
        }
    }

    private void TrackFlashbackRecordingAcceptedSequence(long sourceSequence)
    {
        while (true)
        {
            var last = Interlocked.Read(ref _flashbackRecordingLastAcceptedSequence);
            if (sourceSequence <= last)
            {
                return;
            }

            if (Interlocked.CompareExchange(ref _flashbackRecordingLastAcceptedSequence, sourceSequence, last) == last)
            {
                if (last >= 0 && sourceSequence > last + 1)
                {
                    Interlocked.Add(ref _flashbackRecordingSequenceGaps, sourceSequence - last - 1);
                }

                return;
            }
        }
    }
}

using System;
using System.Diagnostics;
using System.Threading;
using Sussudio.Models;
using Sussudio.Services.Preview;
using Sussudio.Services.Runtime;

namespace Sussudio.Services.Capture;

internal sealed partial class UnifiedVideoCapture
{
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
}

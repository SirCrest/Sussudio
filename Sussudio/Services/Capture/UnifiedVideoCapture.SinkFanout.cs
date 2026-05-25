using System;
using System.Threading;
using Sussudio.Models;
using Sussudio.Services.Contracts;
using Sussudio.Services.Flashback;
using Sussudio.Services.Gpu;
using Sussudio.Services.Recording;
using Sussudio.Services.Runtime;

namespace Sussudio.Services.Capture;

internal sealed partial class UnifiedVideoCapture
{
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

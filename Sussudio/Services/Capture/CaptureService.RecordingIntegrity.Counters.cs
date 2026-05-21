using System;
using Sussudio.Services.Flashback;
using Sussudio.Services.Recording;

namespace Sussudio.Services.Capture;

public partial class CaptureService
{
    private sealed record RecordingIntegrityCounterSnapshot(
        string Backend,
        long SubmittedFrames,
        long EncodedFrames,
        long PacketsWritten,
        long EncoderDroppedFrames,
        long QueueDroppedFrames,
        long SequenceGaps,
        int QueueMaxDepth,
        long QueueOldestFrameAgeMs,
        long BackpressureWaitMs,
        long BackpressureEvents,
        long BackpressureMaxWaitMs,
        bool EncodingFailed,
        string? EncodingFailureType,
        string? EncodingFailureMessage);

    private RecordingIntegrityCounterSnapshot GetRecordingIntegrityCountersSinceBaseline(RecordingIntegrityCounterSnapshot current)
    {
        var baseline = _recordingIntegrityCounterBaseline;
        if (baseline == null ||
            !string.Equals(baseline.Backend, current.Backend, StringComparison.Ordinal))
        {
            return current;
        }

        return current with
        {
            SubmittedFrames = DeltaCounter(current.SubmittedFrames, baseline.SubmittedFrames),
            EncodedFrames = DeltaCounter(current.EncodedFrames, baseline.EncodedFrames),
            PacketsWritten = DeltaCounter(current.PacketsWritten, baseline.PacketsWritten),
            EncoderDroppedFrames = DeltaCounter(current.EncoderDroppedFrames, baseline.EncoderDroppedFrames),
            QueueDroppedFrames = DeltaCounter(current.QueueDroppedFrames, baseline.QueueDroppedFrames),
            SequenceGaps = DeltaCounter(current.SequenceGaps, baseline.SequenceGaps),
            BackpressureWaitMs = DeltaCounter(current.BackpressureWaitMs, baseline.BackpressureWaitMs),
            BackpressureEvents = DeltaCounter(current.BackpressureEvents, baseline.BackpressureEvents)
        };
    }

    private static RecordingIntegrityCounterSnapshot CaptureRecordingIntegrityCounters(LibAvRecordingSink sink)
        => new(
            Backend: "LibAv",
            SubmittedFrames: sink.VideoFramesSubmittedToEncoder,
            EncodedFrames: sink.EncodedVideoFrames,
            PacketsWritten: sink.VideoEncoderPacketsWritten,
            EncoderDroppedFrames: sink.VideoEncoderDroppedFrames,
            QueueDroppedFrames: SumNonNegative(
                sink.VideoDropsQueueSaturated,
                sink.GpuFramesDropped,
                sink.CudaFramesDropped),
            SequenceGaps: sink.VideoSequenceGaps,
            QueueMaxDepth: Math.Max(sink.VideoQueueMaxDepth, Math.Max(sink.GpuQueueMaxDepth, sink.CudaQueueMaxDepth)),
            QueueOldestFrameAgeMs: sink.VideoQueueOldestFrameAgeMs,
            BackpressureWaitMs: sink.VideoBackpressureWaitMs,
            BackpressureEvents: sink.VideoBackpressureEvents,
            BackpressureMaxWaitMs: sink.MaxVideoBackpressureWaitMs,
            EncodingFailed: sink.EncodingFailed,
            EncodingFailureType: sink.EncodingFailureType,
            EncodingFailureMessage: sink.EncodingFailureMessage);

    private static RecordingIntegrityCounterSnapshot CaptureRecordingIntegrityCounters(FlashbackEncoderSink sink)
        => new(
            Backend: "Flashback",
            SubmittedFrames: sink.VideoFramesSubmittedToEncoder,
            EncodedFrames: sink.EncodedVideoFrames,
            PacketsWritten: sink.VideoEncoderPacketsWritten,
            EncoderDroppedFrames: sink.VideoEncoderDroppedFrames,
            QueueDroppedFrames: SumNonNegative(
                sink.VideoDropsQueueSaturated,
                sink.GpuFramesDropped),
            SequenceGaps: sink.VideoSequenceGaps,
            QueueMaxDepth: Math.Max(sink.VideoQueueMaxDepth, sink.GpuQueueMaxDepth),
            QueueOldestFrameAgeMs: sink.VideoQueueOldestFrameAgeMs,
            BackpressureWaitMs: sink.VideoBackpressureWaitMs,
            BackpressureEvents: sink.VideoBackpressureEvents,
            BackpressureMaxWaitMs: sink.MaxVideoBackpressureWaitMs,
            EncodingFailed: sink.EncodingFailed,
            EncodingFailureType: sink.EncodingFailureType,
            EncodingFailureMessage: sink.EncodingFailureMessage);

    private RecordingIntegrityCounterSnapshot CaptureFlashbackRecordingIntegrityCountersSinceBaseline(
        FlashbackEncoderSink sink,
        UnifiedVideoCapture? videoCapture)
    {
        var counters = GetRecordingIntegrityCountersSinceBaseline(CaptureRecordingIntegrityCounters(sink));
        return videoCapture == null
            ? counters
            : counters with { SequenceGaps = Math.Max(0, videoCapture.FlashbackRecordingSequenceGaps) };
    }

    private static long DeltaCounter(long current, long baseline)
        => current >= baseline ? current - baseline : current;

    private static long SumNonNegative(long a, long b)
        => (a > 0 ? a : 0) + (b > 0 ? b : 0);

    private static long SumNonNegative(long a, long b, long c)
        => (a > 0 ? a : 0) + (b > 0 ? b : 0) + (c > 0 ? c : 0);
}

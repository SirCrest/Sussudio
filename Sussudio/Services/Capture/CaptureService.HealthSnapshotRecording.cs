using System.Threading;
using Sussudio.Services.Flashback;
using Sussudio.Services.Recording;

namespace Sussudio.Services.Capture;

public partial class CaptureService
{
    private RecordingHealthSnapshotFields CaptureRecordingHealthSnapshotFields(
        LibAvRecordingSink? sink,
        FlashbackEncoderSink? fbSink)
    {
        var flashbackIsRecordingBackend = IsFlashbackRecordingBackendOwnedByRecording();
        var lastFailure = GetLastFailureTelemetry();
        var liveRecordingFailed = sink?.EncodingFailed == true ||
                                  (flashbackIsRecordingBackend && fbSink?.EncodingFailed == true);
        (int SampleCount, double AverageMs, double P95Ms, double P99Ms, double MaxMs) emptyVideoQueueLatencyMetrics = default;
        var flashbackVideoQueueLatencyMetrics = fbSink?.VideoQueueLatencyMetrics ?? emptyVideoQueueLatencyMetrics;

        return new RecordingHealthSnapshotFields(
            liveRecordingFailed || lastFailure.RecordingFailed,
            sink?.EncodingFailureType ??
                (flashbackIsRecordingBackend ? fbSink?.EncodingFailureType : null) ??
                lastFailure.RecordingFailureType,
            sink?.EncodingFailureMessage ??
                (flashbackIsRecordingBackend ? fbSink?.EncodingFailureMessage : null) ??
                lastFailure.RecordingFailureMessage,
            fbSink?.EncodingFailed == true || lastFailure.FlashbackFailed,
            fbSink?.EncodingFailureType ?? lastFailure.FlashbackFailureType,
            fbSink?.EncodingFailureMessage ?? lastFailure.FlashbackFailureMessage,
            sink?.VideoQueueCount ??
                (flashbackIsRecordingBackend ? fbSink?.VideoQueueCount ?? 0 : 0),
            sink?.VideoQueueCapacityFrames ??
                (flashbackIsRecordingBackend ? fbSink?.VideoQueueCapacityFrames ?? 0 : 0),
            sink?.VideoQueueMaxDepth ??
                (flashbackIsRecordingBackend ? fbSink?.VideoQueueMaxDepth ?? 0 : 0),
            sink?.VideoFramesEnqueuedCount ??
                (flashbackIsRecordingBackend ? fbSink?.VideoFramesEnqueuedCount ?? 0 : 0),
            sink?.VideoFramesSubmittedToEncoder ??
                (flashbackIsRecordingBackend ? fbSink?.VideoFramesSubmittedToEncoder ?? 0 : 0),
            sink?.VideoEncoderPts ??
                (flashbackIsRecordingBackend ? fbSink?.VideoEncoderPts ?? 0 : 0),
            sink?.VideoEncoderPacketsWritten ??
                (flashbackIsRecordingBackend ? fbSink?.VideoEncoderPacketsWritten ?? 0 : 0),
            sink?.VideoEncoderDroppedFrames ??
                (flashbackIsRecordingBackend ? fbSink?.VideoEncoderDroppedFrames ?? 0 : 0),
            sink?.VideoSequenceGaps ??
                (flashbackIsRecordingBackend ? fbSink?.VideoSequenceGaps ?? 0 : 0),
            sink?.VideoQueueOldestFrameAgeMs ??
                (flashbackIsRecordingBackend ? fbSink?.VideoQueueOldestFrameAgeMs ?? 0 : 0),
            sink?.LastVideoQueueLatencyMs ??
                (flashbackIsRecordingBackend ? fbSink?.LastVideoQueueLatencyMs ?? 0 : 0),
            sink?.VideoQueueLatencyMetrics ??
                (flashbackIsRecordingBackend
                    ? flashbackVideoQueueLatencyMetrics
                    : emptyVideoQueueLatencyMetrics),
            sink?.VideoBackpressureWaitMs ??
                (flashbackIsRecordingBackend ? fbSink?.VideoBackpressureWaitMs ?? 0 : 0),
            sink?.VideoBackpressureEvents ??
                (flashbackIsRecordingBackend ? fbSink?.VideoBackpressureEvents ?? 0 : 0),
            sink?.LastVideoBackpressureWaitMs ??
                (flashbackIsRecordingBackend ? fbSink?.LastVideoBackpressureWaitMs ?? 0 : 0),
            sink?.MaxVideoBackpressureWaitMs ??
                (flashbackIsRecordingBackend ? fbSink?.MaxVideoBackpressureWaitMs ?? 0 : 0),
            sink?.DroppedVideoFrames ??
                (flashbackIsRecordingBackend ? fbSink?.DroppedVideoFrames ?? 0 : Interlocked.Read(ref _videoFramesDropped)),
            sink?.VideoDropsQueueSaturated ??
                (flashbackIsRecordingBackend ? fbSink?.VideoDropsQueueSaturated ?? 0 : 0),
            sink?.VideoDropsBacklogEviction ??
                (flashbackIsRecordingBackend ? fbSink?.VideoDropsBacklogEviction ?? 0 : 0),
            sink?.AudioQueueCount ??
                (flashbackIsRecordingBackend ? fbSink?.AudioQueueCount ?? 0 : 0),
            sink?.AudioDropsQueueSaturated ??
                (flashbackIsRecordingBackend ? fbSink?.AudioDropsQueueSaturated ?? 0 : 0),
            sink?.AudioDropsBacklogEviction ??
                (flashbackIsRecordingBackend ? fbSink?.AudioDropsBacklogEviction ?? 0 : 0),
            sink?.LastVideoEnqueueTick ??
                (flashbackIsRecordingBackend ? fbSink?.LastVideoEnqueueTick ?? 0 : 0),
            sink?.LastVideoWriteTick ??
                (flashbackIsRecordingBackend ? fbSink?.LastVideoWriteTick ?? 0 : 0),
            sink?.EncodedVideoFrames ??
                (flashbackIsRecordingBackend ? fbSink?.EncodedVideoFrames ?? 0 : 0),
            sink?.GpuQueueCount ??
                (flashbackIsRecordingBackend ? fbSink?.GpuQueueCount ?? 0 : 0),
            sink?.GpuQueueCapacityFrames ??
                (flashbackIsRecordingBackend ? fbSink?.GpuQueueCapacityFrames ?? 0 : 0),
            sink?.GpuQueueMaxDepth ??
                (flashbackIsRecordingBackend ? fbSink?.GpuQueueMaxDepth ?? 0 : 0),
            sink?.GpuFramesEnqueued ??
                (flashbackIsRecordingBackend ? fbSink?.GpuFramesEnqueued ?? 0 : 0),
            sink?.GpuFramesDropped ??
                (flashbackIsRecordingBackend ? fbSink?.GpuFramesDropped ?? 0 : 0),
            flashbackVideoQueueLatencyMetrics);
    }

    private readonly record struct RecordingHealthSnapshotFields(
        bool EncodingFailed,
        string? FailureType,
        string? FailureMessage,
        bool FlashbackEncodingFailed,
        string? FlashbackFailureType,
        string? FlashbackFailureMessage,
        int VideoQueueDepth,
        int VideoQueueCapacity,
        int VideoQueueMaxDepth,
        long VideoFramesEnqueued,
        long VideoFramesSubmitted,
        long VideoEncoderPts,
        long VideoEncoderPacketsWritten,
        long VideoEncoderDroppedFrames,
        long VideoSequenceGaps,
        long VideoQueueOldestFrameAgeMs,
        long VideoQueueLastLatencyMs,
        (int SampleCount, double AverageMs, double P95Ms, double P99Ms, double MaxMs) VideoQueueLatencyMetrics,
        long VideoBackpressureWaitMs,
        long VideoBackpressureEvents,
        long VideoBackpressureLastWaitMs,
        long VideoBackpressureMaxWaitMs,
        long DroppedFrames,
        long VideoDropsQueueSaturated,
        long VideoDropsBacklogEviction,
        int AudioQueueDepth,
        long AudioDropsQueueSaturated,
        long AudioDropsBacklogEviction,
        long LastVideoEnqueueTick,
        long LastVideoWriteTick,
        long EncodedVideoFrames,
        int GpuQueueDepth,
        int GpuQueueCapacity,
        int GpuQueueMaxDepth,
        long GpuFramesEnqueued,
        long GpuFramesDropped,
        (int SampleCount, double AverageMs, double P95Ms, double P99Ms, double MaxMs) FlashbackVideoQueueLatencyMetrics);
}

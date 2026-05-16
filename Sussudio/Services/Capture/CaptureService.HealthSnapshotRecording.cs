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
        var activeRecording = CaptureActiveRecordingBackendHealthSnapshotFields(
            sink,
            fbSink,
            flashbackIsRecordingBackend);

        return new RecordingHealthSnapshotFields(
            activeRecording.EncodingFailed || lastFailure.RecordingFailed,
            activeRecording.FailureType ?? lastFailure.RecordingFailureType,
            activeRecording.FailureMessage ?? lastFailure.RecordingFailureMessage,
            fbSink?.EncodingFailed == true || lastFailure.FlashbackFailed,
            fbSink?.EncodingFailureType ?? lastFailure.FlashbackFailureType,
            fbSink?.EncodingFailureMessage ?? lastFailure.FlashbackFailureMessage,
            activeRecording.VideoQueueDepth,
            activeRecording.VideoQueueCapacity,
            activeRecording.VideoQueueMaxDepth,
            activeRecording.VideoFramesEnqueued,
            activeRecording.VideoFramesSubmitted,
            activeRecording.VideoEncoderPts,
            activeRecording.VideoEncoderPacketsWritten,
            activeRecording.VideoEncoderDroppedFrames,
            activeRecording.VideoSequenceGaps,
            activeRecording.VideoQueueOldestFrameAgeMs,
            activeRecording.VideoQueueLastLatencyMs,
            activeRecording.VideoQueueLatencyMetrics,
            activeRecording.VideoBackpressureWaitMs,
            activeRecording.VideoBackpressureEvents,
            activeRecording.VideoBackpressureLastWaitMs,
            activeRecording.VideoBackpressureMaxWaitMs,
            activeRecording.DroppedFrames,
            activeRecording.VideoDropsQueueSaturated,
            activeRecording.VideoDropsBacklogEviction,
            activeRecording.AudioQueueDepth,
            activeRecording.AudioDropsQueueSaturated,
            activeRecording.AudioDropsBacklogEviction,
            activeRecording.LastVideoEnqueueTick,
            activeRecording.LastVideoWriteTick,
            activeRecording.EncodedVideoFrames,
            activeRecording.GpuQueueDepth,
            activeRecording.GpuQueueCapacity,
            activeRecording.GpuQueueMaxDepth,
            activeRecording.GpuFramesEnqueued,
            activeRecording.GpuFramesDropped,
            sink?.CudaQueueCount ?? 0,
            sink?.CudaQueueCapacityFrames ?? 0,
            sink?.CudaQueueMaxDepth ?? 0,
            sink?.CudaFramesEnqueued ?? 0,
            sink?.CudaFramesDropped ?? 0,
            activeRecording.FlashbackVideoQueueLatencyMetrics);
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
        int CudaQueueDepth,
        int CudaQueueCapacity,
        int CudaQueueMaxDepth,
        long CudaFramesEnqueued,
        long CudaFramesDropped,
        (int SampleCount, double AverageMs, double P95Ms, double P99Ms, double MaxMs) FlashbackVideoQueueLatencyMetrics);
}

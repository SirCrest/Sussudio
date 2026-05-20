namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static RecordingPipelineHardwareQueuesFlattenedProjection BuildRecordingPipelineHardwareQueuesFlattenedProjection(
        RecordingPipelineProjection recordingPipeline)
        => new()
        {
            GpuQueueDepth = recordingPipeline.RecordingGpuQueueDepth,
            GpuQueueCapacity = recordingPipeline.RecordingGpuQueueCapacity,
            GpuQueueMaxDepth = recordingPipeline.RecordingGpuQueueMaxDepth,
            GpuFramesEnqueued = recordingPipeline.RecordingGpuFramesEnqueued,
            GpuFramesDropped = recordingPipeline.RecordingGpuFramesDropped,
            CudaQueueDepth = recordingPipeline.RecordingCudaQueueDepth,
            CudaQueueCapacity = recordingPipeline.RecordingCudaQueueCapacity,
            CudaQueueMaxDepth = recordingPipeline.RecordingCudaQueueMaxDepth,
            CudaFramesEnqueued = recordingPipeline.RecordingCudaFramesEnqueued,
            CudaFramesDropped = recordingPipeline.RecordingCudaFramesDropped
        };

    private readonly record struct RecordingPipelineHardwareQueuesFlattenedProjection
    {
        public int GpuQueueDepth { get; init; }
        public int GpuQueueCapacity { get; init; }
        public int GpuQueueMaxDepth { get; init; }
        public long GpuFramesEnqueued { get; init; }
        public long GpuFramesDropped { get; init; }
        public int CudaQueueDepth { get; init; }
        public int CudaQueueCapacity { get; init; }
        public int CudaQueueMaxDepth { get; init; }
        public long CudaFramesEnqueued { get; init; }
        public long CudaFramesDropped { get; init; }
    }
}

using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static RecordingPipelineHardwareQueuesProjection BuildRecordingPipelineHardwareQueuesProjection(CaptureHealthSnapshot health)
        => new()
        {
            GpuQueueDepth = health.RecordingGpuQueueDepth,
            GpuQueueCapacity = health.RecordingGpuQueueCapacity,
            GpuQueueMaxDepth = health.RecordingGpuQueueMaxDepth,
            GpuFramesEnqueued = health.RecordingGpuFramesEnqueued,
            GpuFramesDropped = health.RecordingGpuFramesDropped,
            CudaQueueDepth = health.RecordingCudaQueueDepth,
            CudaQueueCapacity = health.RecordingCudaQueueCapacity,
            CudaQueueMaxDepth = health.RecordingCudaQueueMaxDepth,
            CudaFramesEnqueued = health.RecordingCudaFramesEnqueued,
            CudaFramesDropped = health.RecordingCudaFramesDropped
        };

    private readonly record struct RecordingPipelineHardwareQueuesProjection
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

using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static PreviewD3DFlattenedProjection BuildPreviewD3DFlattenedProjection(
        PreviewD3DProjection previewD3D)
        => new()
        {
            PresentSyncInterval = previewD3D.PresentSyncInterval,
            MaxFrameLatency = previewD3D.MaxFrameLatency,
            SwapChainBufferCount = previewD3D.SwapChainBufferCount,
            SwapChainAddress = previewD3D.SwapChainAddress,
            FramesSubmitted = previewD3D.FramesSubmitted,
            FramesRendered = previewD3D.FramesRendered,
            FramesDropped = previewD3D.FramesDropped,
            RenderThreadFailureCount = previewD3D.RenderThreadFailureCount,
            LastRenderThreadFailureType = previewD3D.LastRenderThreadFailureType,
            LastRenderThreadFailureMessage = previewD3D.LastRenderThreadFailureMessage,
            LastRenderThreadFailureHResult = previewD3D.LastRenderThreadFailureHResult,
            PendingFrameCount = previewD3D.PendingFrameCount,
            InputColorSpace = previewD3D.InputColorSpace,
            OutputColorSpace = previewD3D.OutputColorSpace,
            CpuTiming = BuildPreviewD3DCpuTimingFlattenedProjection(previewD3D.CpuTiming),
            LatencyAndStats = BuildPreviewD3DLatencyAndStatsFlattenedProjection(
                previewD3D.PipelineLatency,
                previewD3D.FrameLatencyWait,
                previewD3D.FrameStats),
            FrameFlow = BuildPreviewD3DFrameFlowFlattenedProjection(previewD3D.FrameFlow)
        };

    private readonly record struct PreviewD3DFlattenedProjection
    {
        public int PresentSyncInterval { get; init; }
        public int MaxFrameLatency { get; init; }
        public int SwapChainBufferCount { get; init; }
        public string SwapChainAddress { get; init; }
        public long FramesSubmitted { get; init; }
        public long FramesRendered { get; init; }
        public long FramesDropped { get; init; }
        public long RenderThreadFailureCount { get; init; }
        public string LastRenderThreadFailureType { get; init; }
        public string LastRenderThreadFailureMessage { get; init; }
        public int LastRenderThreadFailureHResult { get; init; }
        public int PendingFrameCount { get; init; }
        public string InputColorSpace { get; init; }
        public string OutputColorSpace { get; init; }
        public PreviewD3DCpuTimingFlattenedProjection CpuTiming { get; init; }
        public PreviewD3DLatencyAndStatsFlattenedProjection LatencyAndStats { get; init; }
        public PreviewD3DFrameFlowFlattenedProjection FrameFlow { get; init; }
    }
}

using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static PreviewD3DFrameFlowProjection BuildPreviewD3DFrameFlowProjection(
        PreviewRuntimeSnapshot previewRuntime)
        => new()
        {
            LastSubmittedPreviewPresentId = previewRuntime.D3DLastSubmittedPreviewPresentId,
            LastSubmittedSourceSequenceNumber = previewRuntime.D3DLastSubmittedSourceSequenceNumber,
            LastSubmittedSourcePtsTicks = previewRuntime.D3DLastSubmittedSourcePtsTicks,
            LastSubmittedQpc = previewRuntime.D3DLastSubmittedQpc,
            LastSubmittedUtcUnixMs = previewRuntime.D3DLastSubmittedUtcUnixMs,
            LastRenderedPreviewPresentId = previewRuntime.D3DLastRenderedPreviewPresentId,
            LastRenderedSourceSequenceNumber = previewRuntime.D3DLastRenderedSourceSequenceNumber,
            LastRenderedSourcePtsTicks = previewRuntime.D3DLastRenderedSourcePtsTicks,
            LastRenderedQpc = previewRuntime.D3DLastRenderedQpc,
            LastRenderedUtcUnixMs = previewRuntime.D3DLastRenderedUtcUnixMs,
            LastRenderedSchedulerToPresentMs = previewRuntime.D3DLastRenderedSchedulerToPresentMs,
            LastRenderedPipelineLatencyMs = previewRuntime.D3DLastRenderedPipelineLatencyMs,
            LastDroppedPreviewPresentId = previewRuntime.D3DLastDroppedPreviewPresentId,
            LastDroppedSourceSequenceNumber = previewRuntime.D3DLastDroppedSourceSequenceNumber,
            LastDroppedSourcePtsTicks = previewRuntime.D3DLastDroppedSourcePtsTicks,
            LastDroppedQpc = previewRuntime.D3DLastDroppedQpc,
            LastDroppedUtcUnixMs = previewRuntime.D3DLastDroppedUtcUnixMs,
            LastDropReason = previewRuntime.D3DLastDropReason,
            RecentSlowFrames = previewRuntime.D3DRecentSlowFrames
        };

    private readonly record struct PreviewD3DFrameFlowProjection
    {
        public long LastSubmittedPreviewPresentId { get; init; }
        public long LastSubmittedSourceSequenceNumber { get; init; }
        public long LastSubmittedSourcePtsTicks { get; init; }
        public long LastSubmittedQpc { get; init; }
        public long LastSubmittedUtcUnixMs { get; init; }
        public long LastRenderedPreviewPresentId { get; init; }
        public long LastRenderedSourceSequenceNumber { get; init; }
        public long LastRenderedSourcePtsTicks { get; init; }
        public long LastRenderedQpc { get; init; }
        public long LastRenderedUtcUnixMs { get; init; }
        public double LastRenderedSchedulerToPresentMs { get; init; }
        public double LastRenderedPipelineLatencyMs { get; init; }
        public long LastDroppedPreviewPresentId { get; init; }
        public long LastDroppedSourceSequenceNumber { get; init; }
        public long LastDroppedSourcePtsTicks { get; init; }
        public long LastDroppedQpc { get; init; }
        public long LastDroppedUtcUnixMs { get; init; }
        public string LastDropReason { get; init; }
        public PreviewSlowFrameDiagnostic[] RecentSlowFrames { get; init; }
    }
}

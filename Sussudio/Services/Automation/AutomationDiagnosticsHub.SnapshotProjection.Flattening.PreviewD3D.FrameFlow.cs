using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static PreviewD3DFrameFlowFlattenedProjection BuildPreviewD3DFrameFlowFlattenedProjection(
        PreviewD3DFrameFlowProjection frameFlow)
        => new()
        {
            LastSubmittedPreviewPresentId = frameFlow.LastSubmittedPreviewPresentId,
            LastSubmittedSourceSequenceNumber = frameFlow.LastSubmittedSourceSequenceNumber,
            LastSubmittedSourcePtsTicks = frameFlow.LastSubmittedSourcePtsTicks,
            LastSubmittedQpc = frameFlow.LastSubmittedQpc,
            LastSubmittedUtcUnixMs = frameFlow.LastSubmittedUtcUnixMs,
            LastRenderedPreviewPresentId = frameFlow.LastRenderedPreviewPresentId,
            LastRenderedSourceSequenceNumber = frameFlow.LastRenderedSourceSequenceNumber,
            LastRenderedSourcePtsTicks = frameFlow.LastRenderedSourcePtsTicks,
            LastRenderedQpc = frameFlow.LastRenderedQpc,
            LastRenderedUtcUnixMs = frameFlow.LastRenderedUtcUnixMs,
            LastRenderedSchedulerToPresentMs = frameFlow.LastRenderedSchedulerToPresentMs,
            LastRenderedPipelineLatencyMs = frameFlow.LastRenderedPipelineLatencyMs,
            LastDroppedPreviewPresentId = frameFlow.LastDroppedPreviewPresentId,
            LastDroppedSourceSequenceNumber = frameFlow.LastDroppedSourceSequenceNumber,
            LastDroppedSourcePtsTicks = frameFlow.LastDroppedSourcePtsTicks,
            LastDroppedQpc = frameFlow.LastDroppedQpc,
            LastDroppedUtcUnixMs = frameFlow.LastDroppedUtcUnixMs,
            LastDropReason = frameFlow.LastDropReason,
            RecentSlowFrames = frameFlow.RecentSlowFrames
        };

    private readonly record struct PreviewD3DFrameFlowFlattenedProjection
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

using Sussudio.Services.Preview;

namespace Sussudio.Controllers;

internal readonly record struct PreviewRuntimeD3DFrameOwnership(
    long LastSubmittedPreviewPresentId,
    long LastSubmittedSourceSequenceNumber,
    long LastSubmittedSourcePtsTicks,
    long LastSubmittedQpc,
    long LastSubmittedUtcUnixMs,
    long LastRenderedPreviewPresentId,
    long LastRenderedSourceSequenceNumber,
    long LastRenderedSourcePtsTicks,
    long LastRenderedQpc,
    long LastRenderedUtcUnixMs,
    double LastRenderedSchedulerToPresentMs,
    double LastRenderedPipelineLatencyMs,
    long LastDroppedPreviewPresentId,
    long LastDroppedSourceSequenceNumber,
    long LastDroppedSourcePtsTicks,
    long LastDroppedQpc,
    long LastDroppedUtcUnixMs,
    string LastDropReason);

internal static class PreviewRuntimeD3DFrameOwnershipPolicy
{
    public static PreviewRuntimeD3DFrameOwnership Evaluate(D3D11PreviewRenderer? d3d)
    {
        var frameOwnership = d3d?.GetFrameOwnershipMetrics();

        return new PreviewRuntimeD3DFrameOwnership(
            LastSubmittedPreviewPresentId: frameOwnership?.LastSubmittedPreviewPresentId ?? 0,
            LastSubmittedSourceSequenceNumber: frameOwnership?.LastSubmittedSourceSequenceNumber ?? -1,
            LastSubmittedSourcePtsTicks: frameOwnership?.LastSubmittedSourcePtsTicks ?? 0,
            LastSubmittedQpc: frameOwnership?.LastSubmittedQpc ?? 0,
            LastSubmittedUtcUnixMs: frameOwnership?.LastSubmittedUtcUnixMs ?? 0,
            LastRenderedPreviewPresentId: frameOwnership?.LastRenderedPreviewPresentId ?? 0,
            LastRenderedSourceSequenceNumber: frameOwnership?.LastRenderedSourceSequenceNumber ?? -1,
            LastRenderedSourcePtsTicks: frameOwnership?.LastRenderedSourcePtsTicks ?? 0,
            LastRenderedQpc: frameOwnership?.LastRenderedQpc ?? 0,
            LastRenderedUtcUnixMs: frameOwnership?.LastRenderedUtcUnixMs ?? 0,
            LastRenderedSchedulerToPresentMs: frameOwnership?.LastRenderedSchedulerToPresentMs ?? 0,
            LastRenderedPipelineLatencyMs: frameOwnership?.LastRenderedPipelineLatencyMs ?? 0,
            LastDroppedPreviewPresentId: frameOwnership?.LastDroppedPreviewPresentId ?? 0,
            LastDroppedSourceSequenceNumber: frameOwnership?.LastDroppedSourceSequenceNumber ?? -1,
            LastDroppedSourcePtsTicks: frameOwnership?.LastDroppedSourcePtsTicks ?? 0,
            LastDroppedQpc: frameOwnership?.LastDroppedQpc ?? 0,
            LastDroppedUtcUnixMs: frameOwnership?.LastDroppedUtcUnixMs ?? 0,
            LastDropReason: frameOwnership?.LastDropReason ?? string.Empty);
    }
}

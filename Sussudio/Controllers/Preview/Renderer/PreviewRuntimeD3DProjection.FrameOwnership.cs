namespace Sussudio.Controllers;

internal sealed partial class PreviewRuntimeD3DProjection
{
    public long D3DLastSubmittedPreviewPresentId { get; private set; }
    public long D3DLastSubmittedSourceSequenceNumber { get; private set; }
    public long D3DLastSubmittedSourcePtsTicks { get; private set; }
    public long D3DLastSubmittedQpc { get; private set; }
    public long D3DLastSubmittedUtcUnixMs { get; private set; }
    public long D3DLastRenderedPreviewPresentId { get; private set; }
    public long D3DLastRenderedSourceSequenceNumber { get; private set; }
    public long D3DLastRenderedSourcePtsTicks { get; private set; }
    public long D3DLastRenderedQpc { get; private set; }
    public long D3DLastRenderedUtcUnixMs { get; private set; }
    public double D3DLastRenderedSchedulerToPresentMs { get; private set; }
    public double D3DLastRenderedPipelineLatencyMs { get; private set; }
    public long D3DLastDroppedPreviewPresentId { get; private set; }
    public long D3DLastDroppedSourceSequenceNumber { get; private set; }
    public long D3DLastDroppedSourcePtsTicks { get; private set; }
    public long D3DLastDroppedQpc { get; private set; }
    public long D3DLastDroppedUtcUnixMs { get; private set; }
    public string D3DLastDropReason { get; private set; } = string.Empty;

    private void ApplyFrameOwnership(PreviewRuntimeD3DFrameOwnership frameOwnership)
    {
        D3DLastSubmittedPreviewPresentId = frameOwnership.LastSubmittedPreviewPresentId;
        D3DLastSubmittedSourceSequenceNumber = frameOwnership.LastSubmittedSourceSequenceNumber;
        D3DLastSubmittedSourcePtsTicks = frameOwnership.LastSubmittedSourcePtsTicks;
        D3DLastSubmittedQpc = frameOwnership.LastSubmittedQpc;
        D3DLastSubmittedUtcUnixMs = frameOwnership.LastSubmittedUtcUnixMs;
        D3DLastRenderedPreviewPresentId = frameOwnership.LastRenderedPreviewPresentId;
        D3DLastRenderedSourceSequenceNumber = frameOwnership.LastRenderedSourceSequenceNumber;
        D3DLastRenderedSourcePtsTicks = frameOwnership.LastRenderedSourcePtsTicks;
        D3DLastRenderedQpc = frameOwnership.LastRenderedQpc;
        D3DLastRenderedUtcUnixMs = frameOwnership.LastRenderedUtcUnixMs;
        D3DLastRenderedSchedulerToPresentMs = frameOwnership.LastRenderedSchedulerToPresentMs;
        D3DLastRenderedPipelineLatencyMs = frameOwnership.LastRenderedPipelineLatencyMs;
        D3DLastDroppedPreviewPresentId = frameOwnership.LastDroppedPreviewPresentId;
        D3DLastDroppedSourceSequenceNumber = frameOwnership.LastDroppedSourceSequenceNumber;
        D3DLastDroppedSourcePtsTicks = frameOwnership.LastDroppedSourcePtsTicks;
        D3DLastDroppedQpc = frameOwnership.LastDroppedQpc;
        D3DLastDroppedUtcUnixMs = frameOwnership.LastDroppedUtcUnixMs;
        D3DLastDropReason = frameOwnership.LastDropReason;
    }
}

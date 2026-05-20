namespace Sussudio.Controllers;

internal sealed partial class PreviewRuntimeD3DProjection
{
    public long D3DLastSubmittedPreviewPresentId { get; init; }
    public long D3DLastSubmittedSourceSequenceNumber { get; init; }
    public long D3DLastSubmittedSourcePtsTicks { get; init; }
    public long D3DLastSubmittedQpc { get; init; }
    public long D3DLastSubmittedUtcUnixMs { get; init; }
    public long D3DLastRenderedPreviewPresentId { get; init; }
    public long D3DLastRenderedSourceSequenceNumber { get; init; }
    public long D3DLastRenderedSourcePtsTicks { get; init; }
    public long D3DLastRenderedQpc { get; init; }
    public long D3DLastRenderedUtcUnixMs { get; init; }
    public double D3DLastRenderedSchedulerToPresentMs { get; init; }
    public double D3DLastRenderedPipelineLatencyMs { get; init; }
    public long D3DLastDroppedPreviewPresentId { get; init; }
    public long D3DLastDroppedSourceSequenceNumber { get; init; }
    public long D3DLastDroppedSourcePtsTicks { get; init; }
    public long D3DLastDroppedQpc { get; init; }
    public long D3DLastDroppedUtcUnixMs { get; init; }
    public string D3DLastDropReason { get; init; } = string.Empty;
}

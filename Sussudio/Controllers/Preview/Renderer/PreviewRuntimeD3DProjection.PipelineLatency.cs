namespace Sussudio.Controllers;

internal sealed partial class PreviewRuntimeD3DProjection
{
    public int D3DPipelineLatencySampleCount { get; init; }
    public double D3DPipelineLatencyAvgMs { get; init; }
    public double D3DPipelineLatencyP95Ms { get; init; }
    public double D3DPipelineLatencyP99Ms { get; init; }
    public double D3DPipelineLatencyMaxMs { get; init; }
    public double EstimatedPipelineLatencyMs { get; init; }
}

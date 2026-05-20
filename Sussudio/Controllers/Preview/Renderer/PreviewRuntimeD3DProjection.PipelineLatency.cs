namespace Sussudio.Controllers;

internal sealed partial class PreviewRuntimeD3DProjection
{
    public int D3DPipelineLatencySampleCount { get; private set; }
    public double D3DPipelineLatencyAvgMs { get; private set; }
    public double D3DPipelineLatencyP95Ms { get; private set; }
    public double D3DPipelineLatencyP99Ms { get; private set; }
    public double D3DPipelineLatencyMaxMs { get; private set; }
    public double EstimatedPipelineLatencyMs { get; private set; }

    private void ApplyPipelineLatency(PreviewRuntimeD3DPipelineLatency pipelineLatency)
    {
        D3DPipelineLatencySampleCount = pipelineLatency.SampleCount;
        D3DPipelineLatencyAvgMs = pipelineLatency.AverageMs;
        D3DPipelineLatencyP95Ms = pipelineLatency.P95Ms;
        D3DPipelineLatencyP99Ms = pipelineLatency.P99Ms;
        D3DPipelineLatencyMaxMs = pipelineLatency.MaxMs;
        EstimatedPipelineLatencyMs = pipelineLatency.EstimatedPipelineLatencyMs;
    }
}

using Sussudio.Services.Preview;

namespace Sussudio.Controllers;

internal readonly record struct PreviewRuntimeD3DPipelineLatency(
    int SampleCount,
    double AverageMs,
    double P95Ms,
    double P99Ms,
    double MaxMs,
    double EstimatedPipelineLatencyMs);

internal static class PreviewRuntimeD3DPipelineLatencyPolicy
{
    public static PreviewRuntimeD3DPipelineLatency Evaluate(D3D11PreviewRenderer? d3d)
    {
        var pipelineLatency = d3d?.GetPipelineLatencyMetrics();

        return new PreviewRuntimeD3DPipelineLatency(
            SampleCount: pipelineLatency?.SampleCount ?? 0,
            AverageMs: pipelineLatency?.AverageMs ?? 0,
            P95Ms: pipelineLatency?.P95Ms ?? 0,
            P99Ms: pipelineLatency?.P99Ms ?? 0,
            MaxMs: pipelineLatency?.MaxMs ?? 0,
            EstimatedPipelineLatencyMs: pipelineLatency?.AverageMs ?? 0);
    }
}

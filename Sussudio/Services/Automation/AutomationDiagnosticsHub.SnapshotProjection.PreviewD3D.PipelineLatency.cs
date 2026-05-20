using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static PreviewD3DPipelineLatencyProjection BuildPreviewD3DPipelineLatencyProjection(
        PreviewRuntimeSnapshot previewRuntime)
        => new()
        {
            SampleCount = previewRuntime.D3DPipelineLatencySampleCount,
            AvgMs = previewRuntime.D3DPipelineLatencyAvgMs,
            P95Ms = previewRuntime.D3DPipelineLatencyP95Ms,
            P99Ms = previewRuntime.D3DPipelineLatencyP99Ms,
            MaxMs = previewRuntime.D3DPipelineLatencyMaxMs
        };

    private readonly record struct PreviewD3DPipelineLatencyProjection
    {
        public int SampleCount { get; init; }
        public double AvgMs { get; init; }
        public double P95Ms { get; init; }
        public double P99Ms { get; init; }
        public double MaxMs { get; init; }
    }
}

using Sussudio.Services.Preview;

namespace Sussudio.Controllers;

internal readonly record struct PreviewRuntimeD3DFrameLatencyWait(
    bool Enabled,
    bool HandleActive,
    long CallCount,
    long SignaledCount,
    long TimeoutCount,
    long UnexpectedResultCount,
    uint LastResult,
    double LastWaitMs,
    int SampleCount,
    double AverageMs,
    double P95Ms,
    double P99Ms,
    double MaxMs);

internal static class PreviewRuntimeD3DFrameLatencyWaitPolicy
{
    public static PreviewRuntimeD3DFrameLatencyWait Evaluate(D3D11PreviewRenderer? d3d)
    {
        var frameLatencyWait = d3d?.GetFrameLatencyWaitMetrics();

        return new PreviewRuntimeD3DFrameLatencyWait(
            Enabled: frameLatencyWait?.Enabled ?? false,
            HandleActive: frameLatencyWait?.HandleActive ?? false,
            CallCount: frameLatencyWait?.CallCount ?? 0,
            SignaledCount: frameLatencyWait?.SignaledCount ?? 0,
            TimeoutCount: frameLatencyWait?.TimeoutCount ?? 0,
            UnexpectedResultCount: frameLatencyWait?.UnexpectedResultCount ?? 0,
            LastResult: frameLatencyWait?.LastResult ?? 0,
            LastWaitMs: frameLatencyWait?.LastWaitMs ?? 0,
            SampleCount: frameLatencyWait?.Timing.SampleCount ?? 0,
            AverageMs: frameLatencyWait?.Timing.AverageMs ?? 0,
            P95Ms: frameLatencyWait?.Timing.P95Ms ?? 0,
            P99Ms: frameLatencyWait?.Timing.P99Ms ?? 0,
            MaxMs: frameLatencyWait?.Timing.MaxMs ?? 0);
    }
}

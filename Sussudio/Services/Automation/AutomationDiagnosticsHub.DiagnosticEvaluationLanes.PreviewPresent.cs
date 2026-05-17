using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static string BuildPresentLane(
        PreviewRuntimeSnapshot previewRuntime,
        long recentD3DMissedRefreshes,
        long recentD3DStatsFailures)
    {
        var presentTarget = previewRuntime.DisplayCadenceExpectedIntervalMs > 0
            ? $"{previewRuntime.DisplayCadenceExpectedIntervalMs:0.##}ms"
            : "n/a";
        var dxgiStats = previewRuntime.D3DFrameStatsSuccessCount > 0
            ? $" dxgiStats ok={previewRuntime.D3DFrameStatsSuccessCount}/{previewRuntime.D3DFrameStatsSampleCount} pc={previewRuntime.D3DFrameStatsPresentCount} prc={previewRuntime.D3DFrameStatsPresentRefreshCount} prDelta={previewRuntime.D3DFrameStatsLastPresentRefreshDelta} missed={previewRuntime.D3DFrameStatsMissedRefreshCount} recentMissed={recentD3DMissedRefreshes} recentFail={recentD3DStatsFailures}"
            : previewRuntime.D3DFrameStatsSampleCount > 0
                ? $" dxgiStats err={previewRuntime.D3DFrameStatsLastError} fail={previewRuntime.D3DFrameStatsFailureCount}/{previewRuntime.D3DFrameStatsSampleCount} recentFail={recentD3DStatsFailures}"
                : string.Empty;

        return $"present target={presentTarget} avg={previewRuntime.DisplayCadenceAverageIntervalMs:0.##}ms p95={previewRuntime.DisplayCadenceP95IntervalMs:0.##}ms p99={previewRuntime.DisplayCadenceP99IntervalMs:0.##}ms max={previewRuntime.DisplayCadenceMaxIntervalMs:0.##}ms slow={previewRuntime.DisplayCadenceSlowFramePercent:0.##}% rate={previewRuntime.DisplayCadenceObservedFps:0.##}fps 1pctLow={previewRuntime.DisplayCadenceOnePercentLowFps:0.##}fps sync={previewRuntime.D3DPresentSyncInterval} latency={previewRuntime.D3DMaxFrameLatency} buffers={previewRuntime.D3DSwapChainBufferCount} swap={previewRuntime.D3DSwapChainAddress}{dxgiStats}";
    }
}

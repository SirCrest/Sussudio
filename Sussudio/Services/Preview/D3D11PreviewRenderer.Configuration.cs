using System;
using Sussudio.Services.Runtime;

namespace Sussudio.Services.Preview;

internal sealed partial class D3D11PreviewRenderer
{
    // Best measured 4K120 MJPG cadence on the SwapChainPanel path uses DWM-paced
    // Present(1) with a shallow compositor queue. The env overrides remain for A/B
    // runs on other machines or display modes.
    private readonly int _presentSyncInterval = EnvironmentHelpers.GetIntFromEnv("SUSSUDIO_PREVIEW_PRESENT_SYNC_INTERVAL", 1, 0, 1);
    private readonly int _dxgiMaxFrameLatency = EnvironmentHelpers.GetIntFromEnv("SUSSUDIO_PREVIEW_DXGI_MAX_FRAME_LATENCY", 1, 1, 3);
    private readonly int _swapChainBufferCount = EnvironmentHelpers.GetIntFromEnv("SUSSUDIO_PREVIEW_SWAPCHAIN_BUFFER_COUNT", 2, 2, 4);
    private readonly int _maxPendingFrames = EnvironmentHelpers.GetIntFromEnv("SUSSUDIO_PREVIEW_RENDER_QUEUE_DEPTH", 4, 1, 8);
    private readonly bool _waitableSwapChainEnabled = EnvironmentHelpers.GetIntFromEnv("SUSSUDIO_PREVIEW_WAITABLE_SWAPCHAIN", 0, 0, 1) != 0;
    private readonly bool _dxgiFrameStatisticsEnabled = EnvironmentHelpers.GetIntFromEnv("SUSSUDIO_PREVIEW_DXGI_FRAME_STATS", 1, 0, 1) != 0;
    private readonly int _dxgiFrameStatisticsSampleIntervalFrames = EnvironmentHelpers.GetIntFromEnv("SUSSUDIO_PREVIEW_DXGI_FRAME_STATS_SAMPLE_INTERVAL", 2, 1, 120);
    private readonly bool _dxgiFrameStatisticsDwmFlushEnabled = EnvironmentHelpers.GetIntFromEnv("SUSSUDIO_PREVIEW_DXGI_FRAME_STATS_DWM_FLUSH", 0, 0, 1) != 0;
    private readonly double _slowFrameDiagnosticThresholdMs = EnvironmentHelpers.GetDoubleFromEnv("SUSSUDIO_PREVIEW_SLOW_FRAME_THRESHOLD_MS", 0, 0, 1000);
    private readonly bool _mediaPresentDurationEnabled = EnvironmentHelpers.GetIntFromEnv("SUSSUDIO_PREVIEW_MEDIA_PRESENT_DURATION", 0, 0, 1) != 0;
    private readonly string _renderMmcssTask = Environment.GetEnvironmentVariable("SUSSUDIO_PREVIEW_RENDER_MMCSS_TASK") ?? "Playback";
    private readonly int _renderMmcssPriority = EnvironmentHelpers.GetIntFromEnv("SUSSUDIO_PREVIEW_RENDER_MMCSS_PRIORITY", 1, -2, 2);
    private readonly int _nativeStopFenceTimeoutMs = EnvironmentHelpers.GetIntFromEnv("SUSSUDIO_PREVIEW_NATIVE_STOP_FENCE_TIMEOUT_MS", 1000, 100, 10000);
    private readonly int _renderThreadStopTimeoutMs = EnvironmentHelpers.GetIntFromEnv("SUSSUDIO_PREVIEW_RENDER_THREAD_STOP_TIMEOUT_MS", 3000, 500, 30000);
}

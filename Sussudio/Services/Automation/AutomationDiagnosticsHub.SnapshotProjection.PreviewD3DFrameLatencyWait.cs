using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static PreviewD3DFrameLatencyWaitProjection BuildPreviewD3DFrameLatencyWaitProjection(
        PreviewRuntimeSnapshot previewRuntime)
        => new()
        {
            Enabled = previewRuntime.D3DFrameLatencyWaitEnabled,
            HandleActive = previewRuntime.D3DFrameLatencyWaitHandleActive,
            CallCount = previewRuntime.D3DFrameLatencyWaitCallCount,
            SignaledCount = previewRuntime.D3DFrameLatencyWaitSignaledCount,
            TimeoutCount = previewRuntime.D3DFrameLatencyWaitTimeoutCount,
            UnexpectedResultCount = previewRuntime.D3DFrameLatencyWaitUnexpectedResultCount,
            LastResult = previewRuntime.D3DFrameLatencyWaitLastResult,
            LastMs = previewRuntime.D3DFrameLatencyWaitLastMs,
            SampleCount = previewRuntime.D3DFrameLatencyWaitSampleCount,
            AvgMs = previewRuntime.D3DFrameLatencyWaitAvgMs,
            P95Ms = previewRuntime.D3DFrameLatencyWaitP95Ms,
            P99Ms = previewRuntime.D3DFrameLatencyWaitP99Ms,
            MaxMs = previewRuntime.D3DFrameLatencyWaitMaxMs
        };

    private readonly record struct PreviewD3DFrameLatencyWaitProjection
    {
        public bool Enabled { get; init; }
        public bool HandleActive { get; init; }
        public long CallCount { get; init; }
        public long SignaledCount { get; init; }
        public long TimeoutCount { get; init; }
        public long UnexpectedResultCount { get; init; }
        public uint LastResult { get; init; }
        public double LastMs { get; init; }
        public int SampleCount { get; init; }
        public double AvgMs { get; init; }
        public double P95Ms { get; init; }
        public double P99Ms { get; init; }
        public double MaxMs { get; init; }
    }
}

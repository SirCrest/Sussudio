namespace Sussudio.Controllers;

internal sealed partial class PreviewRuntimeD3DProjection
{
    public bool D3DFrameLatencyWaitEnabled { get; init; }
    public bool D3DFrameLatencyWaitHandleActive { get; init; }
    public long D3DFrameLatencyWaitCallCount { get; init; }
    public long D3DFrameLatencyWaitSignaledCount { get; init; }
    public long D3DFrameLatencyWaitTimeoutCount { get; init; }
    public long D3DFrameLatencyWaitUnexpectedResultCount { get; init; }
    public uint D3DFrameLatencyWaitLastResult { get; init; }
    public double D3DFrameLatencyWaitLastMs { get; init; }
    public int D3DFrameLatencyWaitSampleCount { get; init; }
    public double D3DFrameLatencyWaitAvgMs { get; init; }
    public double D3DFrameLatencyWaitP95Ms { get; init; }
    public double D3DFrameLatencyWaitP99Ms { get; init; }
    public double D3DFrameLatencyWaitMaxMs { get; init; }
}

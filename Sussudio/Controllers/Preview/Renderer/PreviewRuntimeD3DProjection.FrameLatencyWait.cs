namespace Sussudio.Controllers;

internal sealed partial class PreviewRuntimeD3DProjection
{
    public bool D3DFrameLatencyWaitEnabled { get; private set; }
    public bool D3DFrameLatencyWaitHandleActive { get; private set; }
    public long D3DFrameLatencyWaitCallCount { get; private set; }
    public long D3DFrameLatencyWaitSignaledCount { get; private set; }
    public long D3DFrameLatencyWaitTimeoutCount { get; private set; }
    public long D3DFrameLatencyWaitUnexpectedResultCount { get; private set; }
    public uint D3DFrameLatencyWaitLastResult { get; private set; }
    public double D3DFrameLatencyWaitLastMs { get; private set; }
    public int D3DFrameLatencyWaitSampleCount { get; private set; }
    public double D3DFrameLatencyWaitAvgMs { get; private set; }
    public double D3DFrameLatencyWaitP95Ms { get; private set; }
    public double D3DFrameLatencyWaitP99Ms { get; private set; }
    public double D3DFrameLatencyWaitMaxMs { get; private set; }

    private void ApplyFrameLatencyWait(PreviewRuntimeD3DFrameLatencyWait frameLatencyWait)
    {
        D3DFrameLatencyWaitEnabled = frameLatencyWait.Enabled;
        D3DFrameLatencyWaitHandleActive = frameLatencyWait.HandleActive;
        D3DFrameLatencyWaitCallCount = frameLatencyWait.CallCount;
        D3DFrameLatencyWaitSignaledCount = frameLatencyWait.SignaledCount;
        D3DFrameLatencyWaitTimeoutCount = frameLatencyWait.TimeoutCount;
        D3DFrameLatencyWaitUnexpectedResultCount = frameLatencyWait.UnexpectedResultCount;
        D3DFrameLatencyWaitLastResult = frameLatencyWait.LastResult;
        D3DFrameLatencyWaitLastMs = frameLatencyWait.LastWaitMs;
        D3DFrameLatencyWaitSampleCount = frameLatencyWait.SampleCount;
        D3DFrameLatencyWaitAvgMs = frameLatencyWait.AverageMs;
        D3DFrameLatencyWaitP95Ms = frameLatencyWait.P95Ms;
        D3DFrameLatencyWaitP99Ms = frameLatencyWait.P99Ms;
        D3DFrameLatencyWaitMaxMs = frameLatencyWait.MaxMs;
    }
}

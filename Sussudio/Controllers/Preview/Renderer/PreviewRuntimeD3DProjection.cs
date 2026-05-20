namespace Sussudio.Controllers;

internal sealed partial class PreviewRuntimeD3DProjection
{
    public bool GpuActive { get; private set; }
    public bool RendererAttached { get; private set; }
    public long FramesArrived { get; private set; }
    public long FramesDisplayed { get; private set; }
    public long FramesDropped { get; private set; }
    public long D3DFramesSubmitted { get; private set; }
    public long D3DFramesRendered { get; private set; }
    public long D3DFramesDropped { get; private set; }

    private void ApplyFrameCounters(PreviewRuntimeD3DFrameCounters frameCounters)
    {
        GpuActive = frameCounters.GpuActive;
        RendererAttached = frameCounters.RendererAttached;
        FramesArrived = frameCounters.FramesArrived;
        FramesDisplayed = frameCounters.FramesDisplayed;
        FramesDropped = frameCounters.FramesDropped;
        D3DFramesSubmitted = frameCounters.D3DFramesSubmitted;
        D3DFramesRendered = frameCounters.D3DFramesRendered;
        D3DFramesDropped = frameCounters.D3DFramesDropped;
    }
}

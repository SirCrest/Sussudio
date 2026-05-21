namespace Sussudio.Models;

public sealed partial class PreviewRuntimeSnapshot
{
    public string GpuPlaybackState { get; init; } = "None";
    public int GpuNaturalVideoWidth { get; init; }
    public int GpuNaturalVideoHeight { get; init; }
    public double GpuPositionMs { get; init; }
    public long GpuPositionEventCount { get; init; }
}

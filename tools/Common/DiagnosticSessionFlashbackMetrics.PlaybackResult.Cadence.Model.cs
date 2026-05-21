namespace Sussudio.Tools;

internal sealed partial class FlashbackPlaybackResultMetrics
{
    public double ObservedFpsAtEnd { get; init; }
    public double AvgFrameMsAtEnd { get; init; }
    public double P99FrameMsAtEnd { get; init; }
    public double MaxFrameMsAtEnd { get; init; }
    public double OnePercentLowFpsAtEnd { get; init; }
    public long FrameCountAtEnd { get; init; }
    public long LateFramesAtEnd { get; init; }
    public long SlowFramesAtEnd { get; init; }
    public double SlowFramePercentAtEnd { get; init; }
    public long DroppedFramesAtEnd { get; init; }
}

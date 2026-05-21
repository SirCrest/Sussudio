namespace Sussudio.Tools;

internal sealed partial class FlashbackPlaybackSessionMetrics
{
    public double MinObservedFpsObserved { get; set; } = double.PositiveInfinity;
    public double MaxP99FrameMsObserved { get; set; }
    public double MaxFrameMsObserved { get; set; }
    public double MaxSlowFramePercentObserved { get; set; }
    public long DroppedFramesDelta { get; set; }
}

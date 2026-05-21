namespace Sussudio.Tools;

internal sealed partial class FlashbackPlaybackSessionMetrics
{
    public double MinOnePercentLowFpsObserved { get; set; } = double.PositiveInfinity;
    public bool OnePercentLowSampleWindowObserved { get; set; }
    public long MinimumOnePercentLowFrameCount { get; set; }
    public long MaxSessionFrameCountObserved { get; set; }
    public long MinOnePercentLowOffsetMs { get; set; }
    public long MinOnePercentLowFrameCount { get; set; }
    public double MinOnePercentLowP99FrameMs { get; set; }
    public double MinOnePercentLowMaxFrameMs { get; set; }
    public double MinOnePercentLowDecodeP99Ms { get; set; }
    public double MinOnePercentLowDecodeMaxMs { get; set; }
    public double MinOnePercentLowAvDriftMs { get; set; }
    public long MinOnePercentLowAudioMasterFallbacks { get; set; }
}

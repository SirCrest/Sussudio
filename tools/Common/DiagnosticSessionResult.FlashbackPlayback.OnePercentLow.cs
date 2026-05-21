namespace Sussudio.Tools;

public sealed partial class DiagnosticSessionResult
{
    // Flashback playback 1% low sample-window summary.
    public double FlashbackPlaybackOnePercentLowFpsAtEnd { get; init; }
    public double FlashbackPlaybackMinOnePercentLowFpsObserved { get; init; }
    public bool FlashbackPlaybackOnePercentLowSampleWindowObserved { get; init; }
    public long FlashbackPlaybackOnePercentLowMinimumFrames { get; init; }
    public long FlashbackPlaybackMaxSessionFrameCountObserved { get; init; }
    public long FlashbackPlaybackMinOnePercentLowOffsetMs { get; init; }
    public long FlashbackPlaybackMinOnePercentLowFrameCount { get; init; }
    public double FlashbackPlaybackMinOnePercentLowP99FrameMs { get; init; }
    public double FlashbackPlaybackMinOnePercentLowMaxFrameMs { get; init; }
    public double FlashbackPlaybackMinOnePercentLowDecodeP99Ms { get; init; }
    public double FlashbackPlaybackMinOnePercentLowDecodeMaxMs { get; init; }
    public double FlashbackPlaybackMinOnePercentLowAvDriftMs { get; init; }
    public long FlashbackPlaybackMinOnePercentLowAudioMasterFallbacks { get; init; }
}

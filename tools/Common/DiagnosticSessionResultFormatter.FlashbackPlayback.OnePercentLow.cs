namespace Sussudio.Tools;

public static partial class DiagnosticSessionResultFormatter
{
    private static string BuildFlashbackPlaybackOnePercentLowPerformanceText(DiagnosticSessionResult result)
        =>
            $"onePercentLowFpsEnd={result.FlashbackPlaybackOnePercentLowFpsAtEnd:0.##} " +
            $"onePercentLowFpsMin={result.FlashbackPlaybackMinOnePercentLowFpsObserved:0.##} " +
            $"onePercentLowWindow={result.FlashbackPlaybackOnePercentLowSampleWindowObserved} " +
            $"onePercentLowMinRequiredFrames={result.FlashbackPlaybackOnePercentLowMinimumFrames} " +
            $"onePercentLowMaxSessionFrames={result.FlashbackPlaybackMaxSessionFrameCountObserved} " +
            $"onePercentLowMinOffsetMs={result.FlashbackPlaybackMinOnePercentLowOffsetMs} " +
            $"onePercentLowMinFrames={result.FlashbackPlaybackMinOnePercentLowFrameCount} " +
            $"onePercentLowMinP99FrameMs={result.FlashbackPlaybackMinOnePercentLowP99FrameMs:0.##} " +
            $"onePercentLowMinMaxFrameMs={result.FlashbackPlaybackMinOnePercentLowMaxFrameMs:0.##} " +
            $"onePercentLowMinDecodeP99Ms={result.FlashbackPlaybackMinOnePercentLowDecodeP99Ms:0.##} " +
            $"onePercentLowMinDecodeMaxMs={result.FlashbackPlaybackMinOnePercentLowDecodeMaxMs:0.##} " +
            $"onePercentLowMinAvDriftMs={result.FlashbackPlaybackMinOnePercentLowAvDriftMs:0.##} " +
            $"onePercentLowMinAudioFallbacks={result.FlashbackPlaybackMinOnePercentLowAudioMasterFallbacks}";
}

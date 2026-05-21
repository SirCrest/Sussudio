namespace Sussudio.Tools;

public static partial class DiagnosticSessionResultFormatter
{
    private static string BuildFlashbackPlaybackCadencePerformanceText(DiagnosticSessionResult result)
        =>
            $"fpsEnd={result.FlashbackPlaybackObservedFpsAtEnd:0.##} " +
            $"fpsMin={result.FlashbackPlaybackMinObservedFpsObserved:0.##} " +
            $"avgFrameMsEnd={result.FlashbackPlaybackAvgFrameMsAtEnd:0.##} " +
            $"p99FrameMsEnd={result.FlashbackPlaybackP99FrameMsAtEnd:0.##} " +
            $"maxFrameMsEnd={result.FlashbackPlaybackMaxFrameMsAtEnd:0.##} " +
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
            $"onePercentLowMinAudioFallbacks={result.FlashbackPlaybackMinOnePercentLowAudioMasterFallbacks} " +
            $"p99FrameMsMax={result.FlashbackPlaybackMaxP99FrameMsObserved:0.##} " +
            $"maxFrameMsObserved={result.FlashbackPlaybackMaxFrameMsObserved:0.##} " +
            $"framesEnd={result.FlashbackPlaybackFrameCountAtEnd} " +
            $"lateEnd={result.FlashbackPlaybackLateFramesAtEnd} " +
            $"slowEnd={result.FlashbackPlaybackSlowFramesAtEnd} " +
            $"slowPctEnd={result.FlashbackPlaybackSlowFramePercentAtEnd:0.##} " +
            $"slowPctMax={result.FlashbackPlaybackMaxSlowFramePercentObserved:0.##} " +
            $"droppedFramesEnd={result.FlashbackPlaybackDroppedFramesAtEnd} " +
            $"droppedFramesDelta={result.FlashbackPlaybackDroppedFramesDelta}";
}

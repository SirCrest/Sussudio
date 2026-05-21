using static Sussudio.Tools.DiagnosticSessionOptionalTextFormatter;

namespace Sussudio.Tools;

public static partial class DiagnosticSessionResultFormatter
{
    private static string BuildFlashbackPlaybackAudioMasterPerformanceText(DiagnosticSessionResult result)
        =>
            $"audioMasterDoubleEnd={result.FlashbackPlaybackAudioMasterDelayDoublesAtEnd} " +
            $"audioMasterDoubleMax={result.FlashbackPlaybackMaxAudioMasterDelayDoublesObserved} " +
            $"audioMasterShrinkEnd={result.FlashbackPlaybackAudioMasterDelayShrinksAtEnd} " +
            $"audioMasterShrinkMax={result.FlashbackPlaybackMaxAudioMasterDelayShrinksObserved} " +
            $"audioMasterFallbackEnd={result.FlashbackPlaybackAudioMasterFallbacksAtEnd} " +
            $"audioMasterFallbackMax={result.FlashbackPlaybackMaxAudioMasterFallbacksObserved} " +
            $"audioMasterUnavailableEnd={result.FlashbackPlaybackAudioMasterUnavailableFallbacksAtEnd} " +
            $"audioMasterStaleEnd={result.FlashbackPlaybackAudioMasterStaleFallbacksAtEnd} " +
            $"audioMasterDriftOutlierEnd={result.FlashbackPlaybackAudioMasterDriftOutlierFallbacksAtEnd} " +
            $"audioMasterLastFallback={FormatOptional(result.FlashbackPlaybackAudioMasterLastFallbackReasonAtEnd)} " +
            $"audioMasterLastFallbackAgeMs={result.FlashbackPlaybackAudioMasterLastFallbackClockAgeMsAtEnd:0.##} " +
            $"audioBufferedMsMax={result.FlashbackPlaybackMaxAudioBufferedDurationMsObserved:0.##} " +
            $"audioQueueMsMax={result.FlashbackPlaybackMaxAudioQueueDurationMsObserved:0.##} " +
            $"absAvDriftMsMax={result.FlashbackPlaybackMaxAbsAvDriftMsObserved:0.##}";
}

using System.Text.Json;
using static Sussudio.Tools.AutomationSnapshotFormatter;
using static Sussudio.Tools.DiagnosticSessionMetrics;

namespace Sussudio.Tools;

internal static partial class DiagnosticSessionFlashbackMetrics
{
    private static void ObservePlaybackAudioMasterMetrics(
        FlashbackPlaybackSessionMetrics metrics,
        JsonElement snapshot)
    {
        metrics.MaxAudioMasterDelayDoublesObserved = Math.Max(
            metrics.MaxAudioMasterDelayDoublesObserved,
            GetResetAwareCounterDelta(snapshot, metrics.BaselineSnapshot, "FlashbackPlaybackAudioMasterDelayDoubles"));
        metrics.MaxAudioMasterDelayShrinksObserved = Math.Max(
            metrics.MaxAudioMasterDelayShrinksObserved,
            GetResetAwareCounterDelta(snapshot, metrics.BaselineSnapshot, "FlashbackPlaybackAudioMasterDelayShrinks"));
        metrics.MaxAudioMasterFallbacksObserved = Math.Max(
            metrics.MaxAudioMasterFallbacksObserved,
            GetResetAwareCounterDelta(snapshot, metrics.BaselineSnapshot, "FlashbackPlaybackAudioMasterFallbacks"));
        metrics.MaxAudioBufferedDurationMsObserved = Math.Max(
            metrics.MaxAudioBufferedDurationMsObserved,
            GetDouble(snapshot, "WasapiPlaybackBufferedDurationMs"));
        metrics.MaxAudioQueueDurationMsObserved = Math.Max(
            metrics.MaxAudioQueueDurationMsObserved,
            GetDouble(snapshot, "WasapiPlaybackQueueDurationMs"));
        metrics.MaxAbsAvDriftMsObserved = Math.Max(
            metrics.MaxAbsAvDriftMsObserved,
            Math.Abs(GetDouble(snapshot, "FlashbackAvDriftMs")));
    }
}

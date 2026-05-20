using System.Text.Json;
using static Sussudio.Tools.AutomationSnapshotFormatter;

namespace Sussudio.Tools;

internal static partial class DiagnosticSessionFlashbackMetrics
{
    private static void ObservePlaybackOnePercentLow(
        FlashbackPlaybackSessionMetrics metrics,
        JsonElement snapshot,
        long offsetMs,
        long frameCount,
        long sessionFrameCount,
        long minimumPlaybackFramesForLowPercentile)
    {
        var onePercentLow = GetDouble(snapshot, "FlashbackPlaybackOnePercentLowFps");
        if (onePercentLow <= 0 || sessionFrameCount < minimumPlaybackFramesForLowPercentile)
        {
            return;
        }

        metrics.OnePercentLowSampleWindowObserved = true;
        if (onePercentLow >= metrics.MinOnePercentLowFpsObserved)
        {
            return;
        }

        metrics.MinOnePercentLowFpsObserved = onePercentLow;
        metrics.MinOnePercentLowOffsetMs = offsetMs;
        metrics.MinOnePercentLowFrameCount = frameCount;
        metrics.MinOnePercentLowP99FrameMs = GetDouble(snapshot, "FlashbackPlaybackP99FrameMs");
        metrics.MinOnePercentLowMaxFrameMs = GetDouble(snapshot, "FlashbackPlaybackMaxFrameMs");
        metrics.MinOnePercentLowDecodeP99Ms = GetDouble(snapshot, "FlashbackPlaybackDecodeP99Ms");
        metrics.MinOnePercentLowDecodeMaxMs = GetDouble(snapshot, "FlashbackPlaybackDecodeMaxMs");
        metrics.MinOnePercentLowAvDriftMs = GetDouble(snapshot, "FlashbackAvDriftMs");
        metrics.MinOnePercentLowAudioMasterFallbacks =
            GetNullableLong(snapshot, "FlashbackPlaybackAudioMasterFallbacks") ?? 0;
    }
}

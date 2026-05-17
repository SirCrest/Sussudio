using System.Text.Json;
using static Sussudio.Tools.AutomationSnapshotFormatter;
using static Sussudio.Tools.DiagnosticSessionMetrics;

namespace Sussudio.Tools;

internal static partial class DiagnosticSessionFlashbackValidation
{
    internal static void ValidateFlashbackPlaybackSession(
        JsonElement lastSnapshot,
        FlashbackPlaybackSessionMetrics metrics,
        VisualCadenceSessionMetrics visualCadenceMetrics,
        int durationSeconds,
        List<string> warnings)
    {
        var targetFps = GetDouble(lastSnapshot, "FlashbackPlaybackTargetFps");
        if (targetFps <= 0)
        {
            targetFps = GetDouble(lastSnapshot, "SelectedExactFrameRate");
        }

        var frameCount = Math.Max(metrics.EndSessionFrameCount, metrics.MaxSessionFrameCountObserved);
        if (frameCount <= 0)
        {
            warnings.Add("flashback playback: no playback frames were observed");
            return;
        }

        if (targetFps > 0 && durationSeconds > 0)
        {
            var minimumExpectedFrames = Math.Max(1, (long)Math.Floor(targetFps * durationSeconds * 0.80));
            if (frameCount < minimumExpectedFrames)
            {
                warnings.Add($"flashback playback: frame count below expected floor frames={frameCount} min={minimumExpectedFrames} targetFps={targetFps:0.##}");
            }

            var minimumOnePercentLow = targetFps * 0.80;
            var visualCadenceHealthy = IsVisualCadenceSessionHealthy(visualCadenceMetrics, targetFps);
            if (!visualCadenceHealthy &&
                metrics.MinOnePercentLowFpsObserved > 0 &&
                metrics.MinOnePercentLowFpsObserved < minimumOnePercentLow)
            {
                warnings.Add($"flashback playback: 1% low dipped below floor min={metrics.MinOnePercentLowFpsObserved:0.##} floor={minimumOnePercentLow:0.##}");
            }
        }

        if (metrics.DroppedFramesDelta > 0)
        {
            var droppedFrames = GetNullableLong(lastSnapshot, "FlashbackPlaybackDroppedFrames") ?? 0;
            warnings.Add($"flashback playback: dropped frames increased delta={metrics.DroppedFramesDelta} end={droppedFrames}");
        }

        if (metrics.SubmitFailuresDelta > 0)
        {
            var submitFailures = GetNullableLong(lastSnapshot, "FlashbackPlaybackSubmitFailures") ?? 0;
            warnings.Add($"flashback playback: submit failures increased delta={metrics.SubmitFailuresDelta} end={submitFailures}");
        }

        const double maxHealthyAudioBufferedMs = 250.0;
        if (metrics.MaxAudioBufferedDurationMsObserved > maxHealthyAudioBufferedMs)
        {
            warnings.Add($"flashback playback: audio buffered duration exceeded budget max={metrics.MaxAudioBufferedDurationMsObserved:0.##}ms budget={maxHealthyAudioBufferedMs:0.##}ms");
        }

        const double maxHealthyAvDriftMs = 250.0;
        if (metrics.MaxAbsAvDriftMsObserved > maxHealthyAvDriftMs)
        {
            warnings.Add($"flashback playback: absolute A/V drift exceeded budget max={metrics.MaxAbsAvDriftMsObserved:0.##}ms budget={maxHealthyAvDriftMs:0.##}ms");
        }
    }
}

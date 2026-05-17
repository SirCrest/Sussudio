using System;
using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static DiagnosticEvaluation? TryBuildFlashbackPlaybackDiagnosticEvaluation(
        CaptureHealthSnapshot health,
        DiagnosticEvaluationLanes lanes,
        double playbackTargetFps,
        long playbackCommandQueueAgeMs,
        bool playbackCommandFailedRecently)
    {
        var playbackSlow =
            string.Equals(health.FlashbackPlaybackState, "Playing", StringComparison.OrdinalIgnoreCase) &&
            playbackTargetFps > 0 &&
            health.FlashbackPlaybackFrameCount >= FlashbackPlaybackMinFramesForPerfAlert &&
            health.FlashbackPlaybackObservedFps > 0 &&
            health.FlashbackPlaybackObservedFps < playbackTargetFps * FlashbackPlaybackSlowFpsRatio;
        var playbackFrametimeDegraded =
            IsFlashbackPlaybackFrametimeDegraded(
                health.FlashbackPlaybackState,
                playbackTargetFps,
                health.FlashbackPlaybackFrameCount,
                health.FlashbackPlaybackCadenceSampleCount,
                health.FlashbackPlaybackOnePercentLowFps);

        if (playbackCommandQueueAgeMs >= FlashbackPlaybackCommandStallThresholdMs)
        {
            return new DiagnosticEvaluation(
                "Warning",
                "flashback_playback",
                "Flashback playback command queue is stalled.",
                lanes.PlaybackCommand,
                lanes.Source,
                lanes.Decode,
                lanes.Preview,
                lanes.Render,
                lanes.Present,
                lanes.Recording,
                lanes.Audio);
        }

        if (playbackCommandFailedRecently)
        {
            return new DiagnosticEvaluation(
                "Warning",
                "flashback_playback",
                "Flashback playback command failed recently.",
                lanes.PlaybackCommand,
                lanes.Source,
                lanes.Decode,
                lanes.Preview,
                lanes.Render,
                lanes.Present,
                lanes.Recording,
                lanes.Audio);
        }

        if (playbackSlow)
        {
            return new DiagnosticEvaluation(
                "Warning",
                "flashback_playback",
                "Flashback playback is below target rate.",
                lanes.PlaybackPerf,
                lanes.Source,
                lanes.Decode,
                lanes.Preview,
                lanes.Render,
                lanes.Present,
                lanes.Recording,
                lanes.Audio);
        }

        if (playbackFrametimeDegraded)
        {
            return new DiagnosticEvaluation(
                "Warning",
                "flashback_playback",
                "Flashback playback frametime is below target.",
                lanes.PlaybackPerf,
                lanes.Source,
                lanes.Decode,
                lanes.Preview,
                lanes.Render,
                lanes.Present,
                lanes.Recording,
                lanes.Audio);
        }

        if (health.FlashbackPlaybackSubmitFailures <= 0)
        {
            return null;
        }

        return new DiagnosticEvaluation(
            "Warning",
            "flashback_playback",
            "Flashback playback frame submission failed.",
            lanes.PlaybackPerf,
            lanes.Source,
            lanes.Decode,
            lanes.Preview,
            lanes.Render,
            lanes.Present,
            lanes.Recording,
            lanes.Audio);
    }
}

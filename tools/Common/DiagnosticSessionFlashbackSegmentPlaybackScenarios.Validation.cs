using System.Text.Json;
using static Sussudio.Tools.AutomationSnapshotFormatter;
using static Sussudio.Tools.DiagnosticSessionMetrics;

namespace Sussudio.Tools;

internal static partial class DiagnosticSessionFlashbackSegmentPlaybackScenarios
{
    private static void ValidateFlashbackSegmentPlaybackSnapshot(
        List<string> actions,
        List<string> warnings,
        JsonElement playbackSnapshot,
        JsonElement baselineSnapshot,
        FlashbackSegmentPlaybackTarget target)
    {
        var completedSegment = target.Segment;
        var state = GetString(playbackSnapshot, "FlashbackPlaybackState") ?? "Unknown";
        var positionMs = GetNullableLong(playbackSnapshot, "FlashbackPlaybackPositionMs") ?? 0;
        var frameCount = GetNullableLong(playbackSnapshot, "FlashbackPlaybackFrameCount") ?? 0;
        var observedFps = GetDouble(playbackSnapshot, "FlashbackPlaybackObservedFps");
        var lateFrames = GetNullableLong(playbackSnapshot, "FlashbackPlaybackLateFrames") ?? 0;
        var commandHealth = BuildPlaybackCommandHealth(playbackSnapshot, baselineSnapshot);
        var pending = GetInt(playbackSnapshot, "FlashbackPlaybackPendingCommands");
        actions.Add(
            "flashback segment playback observed " +
            $"positionMs={positionMs} frames={frameCount} late={lateFrames} fps={observedFps:0.##}");

        if (!string.Equals(state, "Playing", StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add($"flashback segment playback: expected Playing after boundary playback, got {state}");
        }

        if (positionMs < target.BoundaryPositionMs + 250)
        {
            warnings.Add(
                "flashback segment playback: playback position did not cross completed segment boundary " +
                $"positionMs={positionMs} boundaryMs={target.BoundaryPositionMs} " +
                $"absoluteBoundaryMs={completedSegment.EndPtsMs} validStartMs={target.ValidStartPtsMs}");
        }

        var targetFps = GetDouble(playbackSnapshot, "DetectedSourceFrameRate");
        if (targetFps <= 0)
        {
            targetFps = GetDouble(playbackSnapshot, "SelectedFrameRate");
        }

        if (frameCount <= 0)
        {
            warnings.Add(
                "flashback segment playback: playback frames did not advance " +
                $"frames={frameCount} observedFps={observedFps:0.##}");
        }
        else if (frameCount >= 120 && observedFps <= 1)
        {
            warnings.Add(
                "flashback segment playback: playback FPS did not warm after enough frames " +
                $"frames={frameCount} observedFps={observedFps:0.##}");
        }
        else if (targetFps >= 100 && frameCount >= 180 && observedFps < targetFps * 0.85)
        {
            warnings.Add(
                "flashback segment playback: playback FPS below source-rate target after warm sample " +
                $"frames={frameCount} observedFps={observedFps:0.##} targetFps={targetFps:0.##}");
        }

        if (commandHealth.NonCoalescedDropped > 0 || commandHealth.Skipped > 0 || commandHealth.SubmitFailures > 0 || pending > 0)
        {
            warnings.Add(
                "flashback segment playback: command queue unhealthy " +
                $"dropped={commandHealth.Dropped} nonCoalescedDropped={commandHealth.NonCoalescedDropped} " +
                $"coalescedScrub={commandHealth.CoalescedScrub} coalescedSeek={commandHealth.CoalescedSeek} skipped={commandHealth.Skipped} " +
                $"submitFailures={commandHealth.SubmitFailures} pending={pending}");
        }
    }
}

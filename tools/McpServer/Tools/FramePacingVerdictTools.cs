using System.ComponentModel;
using System.Text.Json;
using Sussudio.Models;
using Sussudio.Tools;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace McpServer.Tools;

[McpServerToolType]
// MCP diagnostic helper that combines snapshot cadence, recent interval arrays,
// visual/fingerprint cadence, and timeline deltas into one trust/readiness
// verdict before humans rely on 1% or 5% low numbers.
public static partial class FramePacingVerdictTools
{
    [McpServerTool, Description("Get a compact frame pacing verdict that combines snapshot cadence, raw recent frame intervals, visual cadence, MJPEG fingerprint cadence, and performance timeline counters. Use for 120fps-vs-60fps suspicion, hidden stutter, and sample-quality checks before trusting 1%/5% lows.")]
    public static async Task<CallToolResult> get_frame_pacing_verdict(
        PipeClient pipeClient,
        [Description("Maximum performance timeline entries to inspect for counter deltas. Default 240 is roughly two minutes at 500ms samples.")] int maxTimelineEntries = 240,
        [Description("Minimum per-channel cadence sample duration in seconds before lows are considered trustworthy. Default 30 seconds.")] double minSampleSeconds = 30,
        [Description("Optional target high-frame-rate FPS. Use 0 to infer from snapshot source/capture/playback fields.")] double targetFpsOverride = 0)
    {
        var snapshotResponse = await pipeClient.SendCommandAsync(AutomationCommandKind.GetSnapshot).ConfigureAwait(false);
        if (!AutomationSnapshotFormatter.IsSuccess(snapshotResponse))
        {
            return McpToolResultFactory.FromResponse(snapshotResponse, GetMessage(snapshotResponse));
        }

        if (!snapshotResponse.TryGetProperty("Snapshot", out var snapshot) ||
            snapshot.ValueKind != JsonValueKind.Object)
        {
            return McpToolResultFactory.FromText("Snapshot data was not available.", isError: true);
        }

        var timelinePayload = new Dictionary<string, object?>
        {
            ["maxEntries"] = maxTimelineEntries
        };
        var timelineResponse = await pipeClient.SendCommandAsync(AutomationCommandKind.GetPerformanceTimeline, timelinePayload).ConfigureAwait(false);
        if (!AutomationSnapshotFormatter.IsSuccess(timelineResponse))
        {
            return McpToolResultFactory.FromResponse(timelineResponse, GetMessage(timelineResponse));
        }

        var timeline = ReadTimeline(timelineResponse);
        var targetFps = ResolveTargetFps(snapshot, targetFpsOverride);
        var minSampleMs = Math.Max(0, minSampleSeconds) * 1000.0;

        var capture = ReadChannel(
            snapshot,
            "CaptureCadenceObservedFps",
            "CaptureCadenceFivePercentLowFps",
            "CaptureCadenceOnePercentLowFps",
            "CaptureCadenceSampleCount",
            "CaptureCadenceSampleDurationMs",
            "CaptureCadenceRecentIntervalsMs");
        var preview = ReadChannel(
            snapshot,
            "PreviewCadenceObservedFps",
            "PreviewCadenceFivePercentLowFps",
            "PreviewCadenceOnePercentLowFps",
            "PreviewCadenceSampleCount",
            "PreviewCadenceSampleDurationMs",
            "PreviewCadenceRecentIntervalsMs");
        var playback = ReadChannel(
            snapshot,
            "FlashbackPlaybackObservedFps",
            "FlashbackPlaybackFivePercentLowFps",
            "FlashbackPlaybackOnePercentLowFps",
            "FlashbackPlaybackCadenceSampleCount",
            "FlashbackPlaybackSampleDurationMs",
            "FlashbackPlaybackRecentFrameIntervalsMs");
        var captureReady = IsSampleReady(capture, minSampleMs, targetFps);
        var previewReady = IsSampleReady(preview, minSampleMs, targetFps);
        var playbackReady = playback.SampleCount <= 0 && playback.SampleDurationMs <= 0
            ? true
            : IsSampleReady(playback, minSampleMs, targetFps);
        var sampleReady = captureReady && previewReady && playbackReady;

        var previewHalfRate = IsHalfRate(targetFps, preview.ObservedFps, preview.FivePercentLowFps, preview.IntervalsMs);
        var playbackHalfRate = IsHalfRate(targetFps, playback.ObservedFps, playback.FivePercentLowFps, playback.IntervalsMs);
        var hiddenStutter = sampleReady && IsHiddenStutter(targetFps, preview) ||
                            sampleReady && IsHiddenStutter(targetFps, playback);
        var verdict = ResolveVerdict(sampleReady, previewHalfRate, playbackHalfRate, hiddenStutter);
        var text = BuildFramePacingVerdictText(
            snapshot,
            timeline,
            targetFps,
            minSampleSeconds,
            capture,
            preview,
            playback,
            captureReady,
            previewReady,
            playbackReady,
            sampleReady,
            previewHalfRate,
            playbackHalfRate,
            hiddenStutter,
            verdict);

        return McpToolResultFactory.FromResponse(snapshotResponse, text);
    }

    private static string GetMessage(JsonElement response)
        => AutomationSnapshotFormatter.Get(response, "Message", "Command failed.");

    private sealed record TimelineRow(
        long DxgiRecentMissed,
        long MjpegJitterDropped,
        long PlaybackDroppedFrames);

    private static IReadOnlyList<TimelineRow> ReadTimeline(JsonElement timelineResponse)
    {
        if (!timelineResponse.TryGetProperty("Data", out var data) ||
            data.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<TimelineRow>();
        }

        var rows = new List<TimelineRow>();
        foreach (var item in data.EnumerateArray())
        {
            rows.Add(new TimelineRow(
                AutomationSnapshotFormatter.GetLong(item, "PreviewD3DFrameStatsRecentMissedRefreshCount"),
                AutomationSnapshotFormatter.GetLong(item, "MjpegPreviewJitterTotalDropped"),
                AutomationSnapshotFormatter.GetLong(item, "FlashbackPlaybackDroppedFrames")));
        }

        return rows;
    }
}

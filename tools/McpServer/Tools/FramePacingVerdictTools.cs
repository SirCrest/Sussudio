using System.ComponentModel;
using System.Globalization;
using System.Text;
using System.Text.Json;
using Sussudio.Tools;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace McpServer.Tools;

[McpServerToolType]
// MCP diagnostic helper that combines snapshot cadence, recent interval arrays,
// visual/fingerprint cadence, and timeline deltas into one trust/readiness
// verdict before humans rely on 1% or 5% low numbers.
public static class FramePacingVerdictTools
{
    [McpServerTool, Description("Get a compact frame pacing verdict that combines snapshot cadence, raw recent frame intervals, visual cadence, MJPEG fingerprint cadence, and performance timeline counters. Use for 120fps-vs-60fps suspicion, hidden stutter, and sample-quality checks before trusting 1%/5% lows.")]
    public static async Task<CallToolResult> get_frame_pacing_verdict(
        PipeClient pipeClient,
        [Description("Maximum performance timeline entries to inspect for counter deltas. Default 240 is roughly two minutes at 500ms samples.")] int maxTimelineEntries = 240,
        [Description("Minimum per-channel cadence sample duration in seconds before lows are considered trustworthy. Default 30 seconds.")] double minSampleSeconds = 30,
        [Description("Optional target high-frame-rate FPS. Use 0 to infer from snapshot source/capture/playback fields.")] double targetFpsOverride = 0)
    {
        var snapshotResponse = await pipeClient.SendCommandAsync("GetSnapshot").ConfigureAwait(false);
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
        var timelineResponse = await pipeClient.SendCommandAsync("GetPerformanceTimeline", timelinePayload).ConfigureAwait(false);
        if (!AutomationSnapshotFormatter.IsSuccess(timelineResponse))
        {
            return McpToolResultFactory.FromResponse(timelineResponse, GetMessage(timelineResponse));
        }

        var timeline = ReadTimeline(timelineResponse);
        var targetFps = ResolveTargetFps(snapshot, targetFpsOverride);
        var targetFrameMs = targetFps > 0 ? 1000.0 / targetFps : 0;
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

        var first = timeline.FirstOrDefault();
        var last = timeline.LastOrDefault();
        var dxgiMissedMax = timeline.Count == 0 ? 0 : timeline.Max(row => row.DxgiRecentMissed);
        var previewDropDelta = first is null || last is null ? 0 : NonNegativeDelta(last.MjpegJitterDropped, first.MjpegJitterDropped);
        var playbackDropDelta = first is null || last is null ? 0 : NonNegativeDelta(last.PlaybackDroppedFrames, first.PlaybackDroppedFrames);
        var visualChangeFps = AutomationSnapshotFormatter.GetDouble(snapshot, "VisualCadenceChangeObservedFps");
        var visualRepeatPercent = AutomationSnapshotFormatter.GetDouble(snapshot, "VisualCadenceRepeatFramePercent");
        var visualConfidence = AutomationSnapshotFormatter.Get(snapshot, "VisualCadenceMotionConfidence");
        var mjpegInputFps = AutomationSnapshotFormatter.GetDouble(snapshot, "MjpegPacketHashInputObservedFps");
        var mjpegUniqueFps = AutomationSnapshotFormatter.GetDouble(snapshot, "MjpegPacketHashUniqueObservedFps");
        var mjpegDuplicatePercent = AutomationSnapshotFormatter.GetDouble(snapshot, "MjpegPacketHashDuplicateFramePercent");

        var builder = new StringBuilder();
        builder.AppendLine($"Verdict: {verdict}");
        builder.AppendLine($"SampleQuality: {(sampleReady ? "Ready" : "Insufficient")}");
        builder.AppendLine(string.Create(CultureInfo.InvariantCulture, $"TargetFps: {targetFps:0.##}"));
        builder.AppendLine(string.Create(CultureInfo.InvariantCulture, $"TargetFrameMs: {targetFrameMs:0.###}"));
        builder.AppendLine(string.Create(CultureInfo.InvariantCulture, $"MinSampleSeconds: {minSampleSeconds:0.##}"));
        builder.AppendLine(string.Create(CultureInfo.InvariantCulture, $"Capture: observed={capture.ObservedFps:0.##} 5pct={capture.FivePercentLowFps:0.##} 1pct={capture.OnePercentLowFps:0.##} samples={capture.SampleCount} durationMs={capture.SampleDurationMs:0.#} ready={FormatBool(captureReady)}"));
        builder.AppendLine(string.Create(CultureInfo.InvariantCulture, $"Preview: observed={preview.ObservedFps:0.##} 5pct={preview.FivePercentLowFps:0.##} 1pct={preview.OnePercentLowFps:0.##} samples={preview.SampleCount} durationMs={preview.SampleDurationMs:0.#} ready={FormatBool(previewReady)}"));
        builder.AppendLine(string.Create(CultureInfo.InvariantCulture, $"Playback: observed={playback.ObservedFps:0.##} 5pct={playback.FivePercentLowFps:0.##} 1pct={playback.OnePercentLowFps:0.##} samples={playback.SampleCount} durationMs={playback.SampleDurationMs:0.#} ready={FormatBool(playbackReady)}"));
        builder.AppendLine(string.Create(CultureInfo.InvariantCulture, $"SourceToPreviewRatio: {Ratio(preview.ObservedFps, targetFps):0.###}"));
        builder.AppendLine(string.Create(CultureInfo.InvariantCulture, $"SourceToPlaybackRatio: {Ratio(playback.ObservedFps, targetFps):0.###}"));
        builder.AppendLine($"HalfRatePreviewSuspected: {FormatBool(previewHalfRate)}");
        builder.AppendLine($"HalfRatePlaybackSuspected: {FormatBool(playbackHalfRate)}");
        builder.AppendLine($"HiddenStutterSuspected: {FormatBool(hiddenStutter)}");
        builder.AppendLine(string.Create(CultureInfo.InvariantCulture, $"VisualChangeFps: {visualChangeFps:0.##}"));
        builder.AppendLine(string.Create(CultureInfo.InvariantCulture, $"VisualRepeatPercent: {visualRepeatPercent:0.##}"));
        builder.AppendLine($"VisualMotionConfidence: {visualConfidence}");
        builder.AppendLine(string.Create(CultureInfo.InvariantCulture, $"MjpegInputFps: {mjpegInputFps:0.##}"));
        builder.AppendLine(string.Create(CultureInfo.InvariantCulture, $"MjpegUniqueFps: {mjpegUniqueFps:0.##}"));
        builder.AppendLine(string.Create(CultureInfo.InvariantCulture, $"MjpegDuplicatePercent: {mjpegDuplicatePercent:0.##}"));
        builder.AppendLine($"TimelineSamples: {timeline.Count}");
        builder.AppendLine($"DxgiMissedRefreshRecentMax: {dxgiMissedMax}");
        builder.AppendLine($"PreviewDropDelta: {previewDropDelta}");
        builder.AppendLine($"PlaybackDropDelta: {playbackDropDelta}");
        builder.AppendLine($"Evidence: captureReady={FormatBool(captureReady)} previewReady={FormatBool(previewReady)} playbackReady={FormatBool(playbackReady)} previewHalfRate={FormatBool(previewHalfRate)} playbackHalfRate={FormatBool(playbackHalfRate)}");

        return McpToolResultFactory.FromResponse(snapshotResponse, builder.ToString().TrimEnd());
    }

    private static string GetMessage(JsonElement response)
        => AutomationSnapshotFormatter.Get(response, "Message", "Command failed.");

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

    private static FramePacingChannel ReadChannel(
        JsonElement snapshot,
        string observedFpsProperty,
        string fivePercentLowFpsProperty,
        string onePercentLowFpsProperty,
        string sampleCountProperty,
        string sampleDurationMsProperty,
        string intervalsProperty)
    {
        return new FramePacingChannel(
            AutomationSnapshotFormatter.GetDouble(snapshot, observedFpsProperty),
            AutomationSnapshotFormatter.GetDouble(snapshot, fivePercentLowFpsProperty),
            AutomationSnapshotFormatter.GetDouble(snapshot, onePercentLowFpsProperty),
            AutomationSnapshotFormatter.GetInt(snapshot, sampleCountProperty),
            AutomationSnapshotFormatter.GetDouble(snapshot, sampleDurationMsProperty),
            GetDoubleArray(snapshot, intervalsProperty));
    }

    private static double ResolveTargetFps(JsonElement snapshot, double targetFpsOverride)
    {
        if (targetFpsOverride > 0)
        {
            return targetFpsOverride;
        }

        return new[]
            {
                AutomationSnapshotFormatter.GetDouble(snapshot, "ExpectedCaptureFrameRate"),
                AutomationSnapshotFormatter.GetDouble(snapshot, "SourceFrameRateExact"),
                AutomationSnapshotFormatter.GetDouble(snapshot, "FlashbackPlaybackTargetFps"),
                AutomationSnapshotFormatter.GetDouble(snapshot, "EncoderFrameRate")
            }
            .Where(value => double.IsFinite(value) && value > 0)
            .DefaultIfEmpty(0)
            .Max();
    }

    private static bool IsSampleReady(FramePacingChannel channel, double minSampleMs, double targetFps)
    {
        if (minSampleMs > 0 && channel.SampleDurationMs < minSampleMs)
        {
            return false;
        }

        var minSamples = targetFps >= 100 ? targetFps * 10 : 60;
        return channel.SampleCount >= minSamples;
    }

    private static bool IsHalfRate(double targetFps, double observedFps, double fivePercentLowFps, IReadOnlyList<double> intervalsMs)
    {
        if (targetFps < 100)
        {
            return false;
        }

        var fps = fivePercentLowFps > 0 ? Math.Min(observedFps > 0 ? observedFps : fivePercentLowFps, fivePercentLowFps) : observedFps;
        var ratio = Ratio(fps, targetFps);
        if (ratio >= 0.45 && ratio <= 0.62)
        {
            return true;
        }

        var targetFrameMs = 1000.0 / targetFps;
        return HasHalfRateIntervals(intervalsMs, targetFrameMs);
    }

    private static bool HasHalfRateIntervals(IReadOnlyList<double> intervalsMs, double targetFrameMs)
    {
        if (intervalsMs.Count < 6 || targetFrameMs <= 0)
        {
            return false;
        }

        var halfRateFrameMs = targetFrameMs * 2;
        var halfRateCount = intervalsMs.Count(value => value >= halfRateFrameMs * 0.80 && value <= halfRateFrameMs * 1.20);
        return halfRateCount >= Math.Max(3, intervalsMs.Count / 3);
    }

    private static bool IsHiddenStutter(double targetFps, FramePacingChannel channel)
    {
        if (targetFps <= 0 || channel.ObservedFps < targetFps * 0.92)
        {
            return false;
        }

        return channel.FivePercentLowFps > 0 && channel.FivePercentLowFps < targetFps * 0.90 ||
               channel.OnePercentLowFps > 0 && channel.OnePercentLowFps < targetFps * 0.85;
    }

    private static string ResolveVerdict(bool sampleReady, bool previewHalfRate, bool playbackHalfRate, bool hiddenStutter)
    {
        if (!sampleReady)
        {
            return "InsufficientSample";
        }

        if (previewHalfRate && playbackHalfRate)
        {
            return "HalfRatePreviewAndPlaybackSuspected";
        }

        if (previewHalfRate)
        {
            return "HalfRatePreviewSuspected";
        }

        if (playbackHalfRate)
        {
            return "HalfRatePlaybackSuspected";
        }

        return hiddenStutter ? "HiddenStutterSuspected" : "FramePacingLooksGood";
    }

    private static double[] GetDoubleArray(JsonElement element, string propertyName)
    {
        if (element.ValueKind != JsonValueKind.Object ||
            !element.TryGetProperty(propertyName, out var array) ||
            array.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<double>();
        }

        var values = new List<double>();
        foreach (var item in array.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.Number && item.TryGetDouble(out var numeric))
            {
                values.Add(numeric);
            }
            else if (item.ValueKind == JsonValueKind.String &&
                     double.TryParse(item.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
            {
                values.Add(parsed);
            }
        }

        return values.ToArray();
    }

    private static long NonNegativeDelta(long latest, long first)
        => latest >= first ? latest - first : 0;

    private static double Ratio(double value, double target)
        => target > 0 && double.IsFinite(value) ? value / target : 0;

    private static string FormatBool(bool value)
        => value ? "true" : "false";

    private sealed record FramePacingChannel(
        double ObservedFps,
        double FivePercentLowFps,
        double OnePercentLowFps,
        int SampleCount,
        double SampleDurationMs,
        double[] IntervalsMs);

    private sealed record TimelineRow(
        long DxgiRecentMissed,
        long MjpegJitterDropped,
        long PlaybackDroppedFrames);
}

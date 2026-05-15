using System.Globalization;
using System.Text;
using System.Text.Json;
using Sussudio.Tools;

namespace McpServer.Tools;

public static partial class FramePacingVerdictTools
{
    private static string BuildFramePacingVerdictText(
        JsonElement snapshot,
        IReadOnlyList<TimelineRow> timeline,
        double targetFps,
        double minSampleSeconds,
        FramePacingChannel capture,
        FramePacingChannel preview,
        FramePacingChannel playback,
        bool captureReady,
        bool previewReady,
        bool playbackReady,
        bool sampleReady,
        bool previewHalfRate,
        bool playbackHalfRate,
        bool hiddenStutter,
        string verdict)
    {
        var targetFrameMs = targetFps > 0 ? 1000.0 / targetFps : 0;
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
        var previewPacingStage = AutomationSnapshotFormatter.Get(snapshot, "PreviewPacingLikelySlowStage", "Unknown");
        var previewPacingConfidence = AutomationSnapshotFormatter.Get(snapshot, "PreviewPacingSlowStageConfidence", "None");
        var previewPacingEvidence = AutomationSnapshotFormatter.Get(snapshot, "PreviewPacingSlowStageEvidence", string.Empty);

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
        builder.AppendLine($"PreviewPacingLikelySlowStage: {previewPacingStage}");
        builder.AppendLine($"PreviewPacingSlowStageConfidence: {previewPacingConfidence}");
        builder.AppendLine($"PreviewPacingSlowStageEvidence: {previewPacingEvidence}");
        builder.AppendLine($"TimelineSamples: {timeline.Count}");
        builder.AppendLine($"DxgiMissedRefreshRecentMax: {dxgiMissedMax}");
        builder.AppendLine($"PreviewDropDelta: {previewDropDelta}");
        builder.AppendLine($"PlaybackDropDelta: {playbackDropDelta}");
        builder.AppendLine($"Evidence: captureReady={FormatBool(captureReady)} previewReady={FormatBool(previewReady)} playbackReady={FormatBool(playbackReady)} previewHalfRate={FormatBool(previewHalfRate)} playbackHalfRate={FormatBool(playbackHalfRate)}");

        return builder.ToString().TrimEnd();
    }
}

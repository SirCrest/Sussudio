using System.Text;
using System.Text.Json;

namespace Sussudio.Tools;

internal static partial class AutomationSnapshotFormatter
{
    private static void AppendCaptureSettingsSection(StringBuilder builder, JsonElement snapshot)
    {
        var frameRateSummary = FormatFrameRateSummary(snapshot);

        builder.AppendLine("== Capture Settings ==");
        builder.AppendLine($"Resolution: {Get(snapshot, "SelectedResolution")} | Frame Rate: {frameRateSummary}");
        builder.AppendLine($"Format: {Get(snapshot, "SelectedRecordingFormat")} | Quality: {Get(snapshot, "SelectedQuality")} | Preset: {Get(snapshot, "SelectedPreset")}");
        builder.AppendLine($"Video Format: {Get(snapshot, "SelectedVideoFormat")} | Split Encode: {Get(snapshot, "SelectedSplitEncodeMode")} | MJPEG Decoders: {Get(snapshot, "MjpegDecoderCount")}");
        builder.AppendLine($"HDR: {Get(snapshot, "IsHdrEnabled")} (Available: {Get(snapshot, "IsHdrAvailable")}, Active: {Get(snapshot, "HdrOutputActive")}, State: {Get(snapshot, "HdrRuntimeState")})");
        builder.AppendLine($"Pipeline: Requested={Get(snapshot, "RequestedPipelineMode")} Active={Get(snapshot, "ActivePipelineMode")} Matched={Get(snapshot, "PipelineModeMatched")}");
        builder.AppendLine($"UI: Show All Options={Get(snapshot, "ShowAllCaptureOptions")} | Preview Volume={Get(snapshot, "PreviewVolumePercent")}% | Stats Visible={Get(snapshot, "IsStatsVisible")}");
        builder.AppendLine();
    }

    private static string FormatFrameRateSummary(JsonElement snapshot)
    {
        var selectedFriendlyFrameRate = Get(snapshot, "SelectedFriendlyFrameRate", string.Empty);
        var selectedExactFrameRate = Get(snapshot, "SelectedExactFrameRate", string.Empty);
        var selectedExactFrameRateArg = Get(snapshot, "SelectedExactFrameRateArg", string.Empty);
        var frameRateBucket = string.IsNullOrWhiteSpace(selectedFriendlyFrameRate)
            ? Get(snapshot, "SelectedFrameRate")
            : selectedFriendlyFrameRate;
        var frameRateExactDetail = !string.IsNullOrWhiteSpace(selectedExactFrameRateArg)
            ? $"{selectedExactFrameRate} fps, {selectedExactFrameRateArg}"
            : !string.IsNullOrWhiteSpace(selectedExactFrameRate)
                ? $"{selectedExactFrameRate} fps"
                : string.Empty;

        return string.IsNullOrWhiteSpace(frameRateExactDetail)
            ? $"{frameRateBucket} fps"
            : $"{frameRateBucket} fps ({frameRateExactDetail})";
    }
}

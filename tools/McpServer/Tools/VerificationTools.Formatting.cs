using System.Text;
using System.Text.Json;
using Sussudio.Tools;

namespace McpServer.Tools;

public static partial class VerificationTools
{
    private static string BuildRecordingVerificationText(JsonElement response, JsonElement verification, string message)
    {
        var builder = new StringBuilder();
        builder.AppendLine(AutomationSnapshotFormatter.IsSuccess(response) ? "== Recording Verification: PASS ==" : "== Recording Verification: FAIL ==");
        builder.AppendLine($"Message: {message}");
        builder.AppendLine($"Output: {AutomationSnapshotFormatter.Get(verification, "OutputPath")} | Exists: {AutomationSnapshotFormatter.Get(verification, "FileExists")} | Size: {AutomationSnapshotFormatter.Get(verification, "FileSizeBytes")} bytes");
        builder.AppendLine($"Mode: {AutomationSnapshotFormatter.Get(verification, "VerificationMode")} | Codec: {AutomationSnapshotFormatter.Get(verification, "DetectedVideoCodec")} | Pixel Format: {AutomationSnapshotFormatter.Get(verification, "DetectedPixelFormat")}");
        builder.AppendLine($"Resolution: {AutomationSnapshotFormatter.Get(verification, "DetectedWidth")} x {AutomationSnapshotFormatter.Get(verification, "DetectedHeight")} | FPS: {AutomationSnapshotFormatter.Get(verification, "DetectedFrameRate")}");
        builder.AppendLine($"HDR: Level={AutomationSnapshotFormatter.Get(verification, "HdrVerificationLevel")} Metadata={AutomationSnapshotFormatter.Get(verification, "HdrMetadataPresent")} Colorimetry={AutomationSnapshotFormatter.Get(verification, "HdrColorimetryValid")} Mastering={AutomationSnapshotFormatter.Get(verification, "HdrMasteringMetadataPresent")}");
        builder.AppendLine(FormatJsonArrayList(verification, "Mismatches", "Mismatches"));

        return builder.ToString().TrimEnd();
    }

    private static string BuildSnapshotAssertionText(JsonElement response)
    {
        var builder = new StringBuilder();
        builder.AppendLine(AutomationSnapshotFormatter.IsSuccess(response) ? "Snapshot assertions: PASS" : "Snapshot assertions: FAIL");
        builder.AppendLine($"Message: {AutomationSnapshotFormatter.Get(response, "Message", "No message.")}");

        if (response.TryGetProperty("Data", out var data) && data.ValueKind == JsonValueKind.Object)
        {
            builder.AppendLine($"Assertions: {AutomationSnapshotFormatter.Get(data, "assertions")}");
            builder.AppendLine($"Passed: {AutomationSnapshotFormatter.Get(data, "passed")}");

            if (data.TryGetProperty("failures", out var failures) && failures.ValueKind == JsonValueKind.Array)
            {
                builder.AppendLine(FormatJsonArrayList(failures, "Failures"));
            }
        }

        return builder.ToString().TrimEnd();
    }

    private static string BuildFileVerificationText(string filePath, JsonElement response, JsonElement verification, string message)
    {
        var builder = new StringBuilder();
        builder.AppendLine(AutomationSnapshotFormatter.IsSuccess(response) ? "== File Verification: PASS ==" : "== File Verification: FAIL ==");
        builder.AppendLine($"Message: {message}");
        builder.AppendLine($"File: {filePath} | Exists: {AutomationSnapshotFormatter.Get(verification, "FileExists")} | Size: {AutomationSnapshotFormatter.Get(verification, "FileSizeBytes")} bytes");
        builder.AppendLine($"Codec: {AutomationSnapshotFormatter.Get(verification, "DetectedVideoCodec")} | Pixel Format: {AutomationSnapshotFormatter.Get(verification, "DetectedPixelFormat")}");
        builder.AppendLine($"Resolution: {AutomationSnapshotFormatter.Get(verification, "DetectedWidth")} x {AutomationSnapshotFormatter.Get(verification, "DetectedHeight")} | FPS: {AutomationSnapshotFormatter.Get(verification, "DetectedFrameRate")}");

        return builder.ToString().TrimEnd();
    }

    private static string FormatJsonArrayList(JsonElement parent, string propertyName, string label)
    {
        if (parent.TryGetProperty(propertyName, out var values) && values.ValueKind == JsonValueKind.Array)
        {
            return FormatJsonArrayList(values, label);
        }

        return $"{label}: None";
    }

    private static string FormatJsonArrayList(JsonElement values, string label)
    {
        var valueList = values.EnumerateArray()
            .Select(value => value.ValueKind == JsonValueKind.String ? value.GetString() : value.ToString())
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToList();

        return valueList.Count == 0
            ? $"{label}: None"
            : $"{label}: {string.Join("; ", valueList)}";
    }
}

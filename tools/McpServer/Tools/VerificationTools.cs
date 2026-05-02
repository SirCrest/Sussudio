using System.ComponentModel;
using System.Text;
using System.Text.Json;
using ElgatoCapture.Tools;
using ModelContextProtocol.Server;

namespace McpServer.Tools;

[McpServerToolType]
public static class VerificationTools
{
    [McpServerTool, Description("Run ffprobe validation on the last recording. Checks codec, resolution, HDR metadata parity.")]
    public static async Task<string> verify_recording(PipeClient pipeClient)
    {
        var response = await pipeClient.SendCommandAsync("VerifyLastRecording", responseTimeoutMs: 60000).ConfigureAwait(false);
        var message = AutomationSnapshotFormatter.Get(response, "Message", "No message.");

        if (!TryGetVerification(response, out var verification))
        {
            return message;
        }

        var builder = new StringBuilder();
        builder.AppendLine(AutomationSnapshotFormatter.IsSuccess(response) ? "== Recording Verification: PASS ==" : "== Recording Verification: FAIL ==");
        builder.AppendLine($"Message: {message}");
        builder.AppendLine($"Output: {AutomationSnapshotFormatter.Get(verification, "OutputPath")} | Exists: {AutomationSnapshotFormatter.Get(verification, "FileExists")} | Size: {AutomationSnapshotFormatter.Get(verification, "FileSizeBytes")} bytes");
        builder.AppendLine($"Mode: {AutomationSnapshotFormatter.Get(verification, "VerificationMode")} | Codec: {AutomationSnapshotFormatter.Get(verification, "DetectedVideoCodec")} | Pixel Format: {AutomationSnapshotFormatter.Get(verification, "DetectedPixelFormat")}");
        builder.AppendLine($"Resolution: {AutomationSnapshotFormatter.Get(verification, "DetectedWidth")} x {AutomationSnapshotFormatter.Get(verification, "DetectedHeight")} | FPS: {AutomationSnapshotFormatter.Get(verification, "DetectedFrameRate")}");
        builder.AppendLine($"HDR: Level={AutomationSnapshotFormatter.Get(verification, "HdrVerificationLevel")} Metadata={AutomationSnapshotFormatter.Get(verification, "HdrMetadataPresent")} Colorimetry={AutomationSnapshotFormatter.Get(verification, "HdrColorimetryValid")} Mastering={AutomationSnapshotFormatter.Get(verification, "HdrMasteringMetadataPresent")}");

        if (verification.TryGetProperty("Mismatches", out var mismatches) && mismatches.ValueKind == JsonValueKind.Array)
        {
            var mismatchList = mismatches.EnumerateArray()
                .Select(m => m.ValueKind == JsonValueKind.String ? m.GetString() : m.ToString())
                .Where(m => !string.IsNullOrWhiteSpace(m))
                .ToList();

            builder.AppendLine(mismatchList.Count == 0
                ? "Mismatches: None"
                : $"Mismatches: {string.Join("; ", mismatchList)}");
        }
        else
        {
            builder.AppendLine("Mismatches: None");
        }

        return builder.ToString().TrimEnd();
    }

    [McpServerTool, Description("Run programmatic assertions against the current app state snapshot. Each assertion has a field name, operator (eq/neq/gt/gte/lt/lte/contains), and expected value.")]
    public static async Task<string> assert_snapshot(
        PipeClient pipeClient,
        [Description("JSON array of assertion objects with field, op, value")] string assertions)
    {
        if (string.IsNullOrWhiteSpace(assertions))
        {
            return "The assertions parameter must be a JSON array string.";
        }

        JsonElement parsedAssertions;
        try
        {
            using var assertionsDocument = JsonDocument.Parse(assertions);
            if (assertionsDocument.RootElement.ValueKind != JsonValueKind.Array)
            {
                return "The assertions parameter must be a JSON array string.";
            }

            parsedAssertions = assertionsDocument.RootElement.Clone();
        }
        catch (JsonException ex)
        {
            return $"Invalid assertions JSON: {ex.Message}";
        }

        var payload = new Dictionary<string, object?>
        {
            ["assertions"] = parsedAssertions
        };

        var response = await pipeClient.SendCommandAsync("AssertSnapshot", payload).ConfigureAwait(false);
        var builder = new StringBuilder();
        builder.AppendLine(AutomationSnapshotFormatter.IsSuccess(response) ? "Snapshot assertions: PASS" : "Snapshot assertions: FAIL");
        builder.AppendLine($"Message: {AutomationSnapshotFormatter.Get(response, "Message", "No message.")}");

        if (response.TryGetProperty("Data", out var data) && data.ValueKind == JsonValueKind.Object)
        {
            builder.AppendLine($"Assertions: {AutomationSnapshotFormatter.Get(data, "assertions")}");
            builder.AppendLine($"Passed: {AutomationSnapshotFormatter.Get(data, "passed")}");

            if (data.TryGetProperty("failures", out var failures) && failures.ValueKind == JsonValueKind.Array)
            {
                var failureList = failures.EnumerateArray()
                    .Select(f => f.ValueKind == JsonValueKind.String ? f.GetString() : f.ToString())
                    .Where(f => !string.IsNullOrWhiteSpace(f))
                    .ToList();

                builder.AppendLine(failureList.Count == 0
                    ? "Failures: None"
                    : $"Failures: {string.Join("; ", failureList)}");
            }
        }

        return builder.ToString().TrimEnd();
    }

    [McpServerTool, Description("Run ffprobe validation on an arbitrary file path. Checks codec, resolution, HDR metadata.")]
    public static async Task<string> verify_file(
        PipeClient pipeClient,
        [Description("Absolute path to the media file to verify")] string filePath,
        [Description("Optional verifier profile, e.g. flashback-export for Flashback exports whose codec may differ from the selected recording format.")] string? verificationProfile = null)
    {
        var payload = new Dictionary<string, object?> { ["filePath"] = filePath };
        if (!string.IsNullOrWhiteSpace(verificationProfile))
        {
            payload["verificationProfile"] = verificationProfile;
        }

        var response = await pipeClient.SendCommandAsync("VerifyFile", payload, responseTimeoutMs: 60000).ConfigureAwait(false);
        var message = AutomationSnapshotFormatter.Get(response, "Message", "No message.");

        if (!TryGetVerification(response, out var verification))
        {
            return message;
        }

        var builder = new StringBuilder();
        builder.AppendLine(AutomationSnapshotFormatter.IsSuccess(response) ? "== File Verification: PASS ==" : "== File Verification: FAIL ==");
        builder.AppendLine($"Message: {message}");
        builder.AppendLine($"File: {filePath} | Exists: {AutomationSnapshotFormatter.Get(verification, "FileExists")} | Size: {AutomationSnapshotFormatter.Get(verification, "FileSizeBytes")} bytes");
        builder.AppendLine($"Codec: {AutomationSnapshotFormatter.Get(verification, "DetectedVideoCodec")} | Pixel Format: {AutomationSnapshotFormatter.Get(verification, "DetectedPixelFormat")}");
        builder.AppendLine($"Resolution: {AutomationSnapshotFormatter.Get(verification, "DetectedWidth")} x {AutomationSnapshotFormatter.Get(verification, "DetectedHeight")} | FPS: {AutomationSnapshotFormatter.Get(verification, "DetectedFrameRate")}");

        return builder.ToString().TrimEnd();
    }

    private static bool TryGetVerification(JsonElement response, out JsonElement verification)
    {
        verification = default;

        if (response.ValueKind == JsonValueKind.Object &&
            response.TryGetProperty("Data", out var data) &&
            data.ValueKind == JsonValueKind.Object &&
            data.TryGetProperty("Verification", out verification) &&
            verification.ValueKind == JsonValueKind.Object)
        {
            return true;
        }

        if (response.ValueKind == JsonValueKind.Object &&
            response.TryGetProperty("Snapshot", out var snapshot) &&
            snapshot.ValueKind == JsonValueKind.Object &&
            snapshot.TryGetProperty("LastVerification", out verification) &&
            verification.ValueKind == JsonValueKind.Object)
        {
            return true;
        }

        return false;
    }

}

using System.ComponentModel;
using System.Text;
using System.Text.Json;
using ModelContextProtocol.Server;

namespace McpServer.Tools;

[McpServerToolType]
public static class VerificationTools
{
    [McpServerTool, Description("Run ffprobe validation on the last recording. Checks codec, resolution, HDR metadata parity.")]
    public static async Task<string> verify_recording(PipeClient pipeClient)
    {
        var response = await pipeClient.SendCommandAsync("VerifyLastRecording", responseTimeoutMs: 60000).ConfigureAwait(false);
        var message = ResponseFormatter.Get(response, "Message", "No message.");

        if (!TryGetVerification(response, out var verification))
        {
            return message;
        }

        var builder = new StringBuilder();
        builder.AppendLine(IsSuccess(response) ? "== Recording Verification: PASS ==" : "== Recording Verification: FAIL ==");
        builder.AppendLine($"Message: {message}");
        builder.AppendLine($"Output: {ResponseFormatter.Get(verification, "OutputPath")} | Exists: {ResponseFormatter.Get(verification, "FileExists")} | Size: {ResponseFormatter.Get(verification, "FileSizeBytes")} bytes");
        builder.AppendLine($"Mode: {ResponseFormatter.Get(verification, "VerificationMode")} | Codec: {ResponseFormatter.Get(verification, "DetectedVideoCodec")} | Pixel Format: {ResponseFormatter.Get(verification, "DetectedPixelFormat")}");
        builder.AppendLine($"Resolution: {ResponseFormatter.Get(verification, "DetectedWidth")} x {ResponseFormatter.Get(verification, "DetectedHeight")} | FPS: {ResponseFormatter.Get(verification, "DetectedFrameRate")}");
        builder.AppendLine($"HDR: Level={ResponseFormatter.Get(verification, "HdrVerificationLevel")} Metadata={ResponseFormatter.Get(verification, "HdrMetadataPresent")} Colorimetry={ResponseFormatter.Get(verification, "HdrColorimetryValid")} Mastering={ResponseFormatter.Get(verification, "HdrMasteringMetadataPresent")}");

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
        builder.AppendLine(IsSuccess(response) ? "Snapshot assertions: PASS" : "Snapshot assertions: FAIL");
        builder.AppendLine($"Message: {ResponseFormatter.Get(response, "Message", "No message.")}");

        if (response.TryGetProperty("Data", out var data) && data.ValueKind == JsonValueKind.Object)
        {
            builder.AppendLine($"Assertions: {ResponseFormatter.Get(data, "assertions")}");
            builder.AppendLine($"Passed: {ResponseFormatter.Get(data, "passed")}");

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

    private static bool IsSuccess(JsonElement response)
    {
        return response.ValueKind == JsonValueKind.Object &&
               response.TryGetProperty("Success", out var success) &&
               success.ValueKind == JsonValueKind.True;
    }
}

using System.ComponentModel;
using System.Text.Json;
using Sussudio.Models;
using Sussudio.Tools;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace McpServer.Tools;

[McpServerToolType]
// MCP tools for verifying recordings and exported Flashback files.
public static partial class VerificationTools
{
    [McpServerTool, Description("Run ffprobe validation on the last recording. Checks codec, resolution, HDR metadata parity.")]
    public static async Task<CallToolResult> verify_recording(PipeClient pipeClient)
    {
        var response = await pipeClient.SendCommandAsync(AutomationCommandKind.VerifyLastRecording).ConfigureAwait(false);
        var message = AutomationSnapshotFormatter.Get(response, "Message", "No message.");

        if (!TryGetVerification(response, out var verification))
        {
            return McpToolResultFactory.FromResponse(response, message);
        }

        return McpToolResultFactory.FromResponse(response, BuildRecordingVerificationText(response, verification, message));
    }

    [McpServerTool, Description("Run programmatic assertions against the current app state snapshot. Each assertion has a field name, operator (eq/neq/gt/gte/lt/lte/contains), and expected value.")]
    public static async Task<CallToolResult> assert_snapshot(
        PipeClient pipeClient,
        [Description("JSON array of assertion objects with field, op, value")] string assertions)
    {
        if (!TryParseAssertionArray(assertions, out var parsedAssertions, out var parseError))
        {
            return McpToolResultFactory.FromText(parseError!, isError: true);
        }

        var payload = new Dictionary<string, object?>
        {
            ["assertions"] = parsedAssertions
        };

        var response = await pipeClient.SendCommandAsync(AutomationCommandKind.AssertSnapshot, payload).ConfigureAwait(false);
        return McpToolResultFactory.FromResponse(response, BuildSnapshotAssertionText(response));
    }

    [McpServerTool, Description("Run ffprobe validation on an arbitrary file path. Checks codec, resolution, HDR metadata.")]
    public static async Task<CallToolResult> verify_file(
        PipeClient pipeClient,
        [Description("Absolute path to the media file to verify")] string filePath,
        [Description("Optional verifier profile, e.g. flashback-export for Flashback exports whose codec may differ from the selected recording format.")] string? verificationProfile = null)
    {
        var payload = new Dictionary<string, object?> { ["filePath"] = filePath };
        if (!string.IsNullOrWhiteSpace(verificationProfile))
        {
            payload["verificationProfile"] = verificationProfile;
        }

        var response = await pipeClient.SendCommandAsync(AutomationCommandKind.VerifyFile, payload).ConfigureAwait(false);
        var message = AutomationSnapshotFormatter.Get(response, "Message", "No message.");

        if (!TryGetVerification(response, out var verification))
        {
            return McpToolResultFactory.FromResponse(response, message);
        }

        return McpToolResultFactory.FromResponse(response, BuildFileVerificationText(filePath, response, verification, message));
    }

    private static bool TryParseAssertionArray(string assertions, out JsonElement parsedAssertions, out string? error)
    {
        parsedAssertions = default;
        error = null;

        if (string.IsNullOrWhiteSpace(assertions))
        {
            error = "The assertions parameter must be a JSON array string.";
            return false;
        }

        try
        {
            using var assertionsDocument = JsonDocument.Parse(assertions);
            if (assertionsDocument.RootElement.ValueKind != JsonValueKind.Array)
            {
                error = "The assertions parameter must be a JSON array string.";
                return false;
            }

            parsedAssertions = assertionsDocument.RootElement.Clone();
            return true;
        }
        catch (JsonException ex)
        {
            error = $"Invalid assertions JSON: {ex.Message}";
            return false;
        }
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

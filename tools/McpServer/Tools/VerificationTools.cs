using System.ComponentModel;
using System.Text.Json;
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
        var response = await pipeClient.SendCommandAsync("VerifyLastRecording", responseTimeoutMs: 60000).ConfigureAwait(false);
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

        var response = await pipeClient.SendCommandAsync("AssertSnapshot", payload).ConfigureAwait(false);
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

        var response = await pipeClient.SendCommandAsync("VerifyFile", payload, responseTimeoutMs: 60000).ConfigureAwait(false);
        var message = AutomationSnapshotFormatter.Get(response, "Message", "No message.");

        if (!TryGetVerification(response, out var verification))
        {
            return McpToolResultFactory.FromResponse(response, message);
        }

        return McpToolResultFactory.FromResponse(response, BuildFileVerificationText(filePath, response, verification, message));
    }
}

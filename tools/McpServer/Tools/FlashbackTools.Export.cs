using System.ComponentModel;
using System.Text;
using System.Text.Json;
using Sussudio.Tools;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace McpServer.Tools;

public static partial class FlashbackTools
{
    [McpServerTool, Description("Export flashback buffer to an MP4 file. Exports the most recent N seconds of the rolling buffer. Refuses to overwrite an existing destination file unless force=true.")]
    public static async Task<CallToolResult> flashback_export(
        PipeClient pipeClient,
        [Description("Number of seconds to export from the buffer (default: 300)")] double seconds = 300,
        [Description("Output file path (default: temp/flashback_export_<timestamp>.mp4)")] string? outputPath = null,
        [Description("True to export the current in/out selection instead of the most recent N seconds")] bool useSelectionRange = false,
        [Description("True to overwrite an existing file at outputPath. Default false: the export is refused if the destination already exists, preserving any prior take.")] bool force = false)
    {
        if (!double.IsFinite(seconds) || seconds <= 0 || seconds > TimeSpan.MaxValue.TotalSeconds)
        {
            throw new ArgumentOutOfRangeException(nameof(seconds), "Flashback export seconds must be finite, greater than zero, and within TimeSpan range.");
        }

        outputPath ??= $"temp/flashback_export_{DateTime.Now:yyyyMMdd_HHmmss}.mp4";

        var dir = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var payload = new Dictionary<string, object?>
        {
            ["seconds"] = seconds,
            ["outputPath"] = outputPath,
            ["useSelectionRange"] = useSelectionRange,
            ["force"] = force
        };

        var response = await pipeClient.SendCommandAsync("FlashbackExport", payload).ConfigureAwait(false);
        var status = AutomationSnapshotFormatter.IsSuccess(response) ? "OK" : "ERROR";
        var message = AutomationSnapshotFormatter.Get(response, "Message", "No message.");

        var builder = new StringBuilder();
        builder.AppendLine($"[{status}] FlashbackExport: {message}");
        builder.AppendLine(useSelectionRange
            ? $"Requested: selected range -> {outputPath}"
            : $"Requested: {seconds}s -> {outputPath}");

        if (response.TryGetProperty("Data", out var data) && data.ValueKind == JsonValueKind.Object)
        {
            var failureKind = AutomationSnapshotFormatter.Get(data, "FailureKind", string.Empty);
            if (!string.IsNullOrWhiteSpace(failureKind))
            {
                builder.AppendLine($"FailureKind: {failureKind}");
            }

            builder.AppendLine($"Data: {data}");
        }

        return McpToolResultFactory.FromResponse(response, builder.ToString().TrimEnd());
    }
}

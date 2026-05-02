using System.ComponentModel;
using System.Text;
using System.Text.Json;
using Sussudio.Tools;
using ModelContextProtocol.Server;

namespace McpServer.Tools;

[McpServerToolType]
public static class VideoSourceProbeTool
{
    [McpServerTool, Description("Query the live video source's supported formats during preview. Shows P010/NV12 availability, current format, memory preference, and full format table without starting recording.")]
    public static async Task<string> probe_video_source(PipeClient pipeClient)
    {
        var response = await pipeClient.SendCommandAsync("ProbeVideoSource").ConfigureAwait(false);
        if (!AutomationSnapshotFormatter.IsSuccess(response))
        {
            return GetMessage(response);
        }

        if (!response.TryGetProperty("Data", out var data) || data.ValueKind != JsonValueKind.Object)
        {
            return "No probe data returned.";
        }

        var builder = new StringBuilder();
        builder.AppendLine("== Video Source Probe ==");

        var sessionActive = Get(data, "SessionActive");
        builder.AppendLine($"Session Active: {sessionActive}");

        if (string.Equals(sessionActive, "false", StringComparison.OrdinalIgnoreCase))
        {
            builder.AppendLine("No active ingest session. Start preview first.");
            return builder.ToString().TrimEnd();
        }

        builder.AppendLine($"Memory Preference: {Get(data, "MemoryPreference")}");
        builder.AppendLine($"Current Format: {Get(data, "CurrentSubtype")} {Get(data, "CurrentWidth")}x{Get(data, "CurrentHeight")}@{Get(data, "CurrentFrameRate")}fps");
        builder.AppendLine($"P010 Available: {Get(data, "P010Available")} | NV12 Available: {Get(data, "Nv12Available")}");

        if (data.TryGetProperty("SupportedSubtypes", out var subtypes) && subtypes.ValueKind == JsonValueKind.Array)
        {
            var subtypeList = new List<string>();
            foreach (var s in subtypes.EnumerateArray())
            {
                var val = s.GetString();
                if (!string.IsNullOrWhiteSpace(val))
                {
                    subtypeList.Add(val);
                }
            }
            builder.AppendLine($"Supported Subtypes: {(subtypeList.Count > 0 ? string.Join(", ", subtypeList) : "none")}");
        }

        builder.AppendLine($"Total Format Count: {Get(data, "TotalFormatCount")}");

        if (data.TryGetProperty("Formats", out var formats) && formats.ValueKind == JsonValueKind.Array)
        {
            builder.AppendLine();
            builder.AppendLine("== Format Table ==");
            var index = 0;
            foreach (var fmt in formats.EnumerateArray())
            {
                if (index >= 50)
                {
                    break;
                }

                builder.AppendLine($"  [{index}] {Get(fmt, "Summary")}");
                index++;
            }
        }

        return builder.ToString().TrimEnd();
    }

    private static string GetMessage(JsonElement response)
    {
        return AutomationSnapshotFormatter.Get(response, "Message", "Command failed.");
    }

    private static string Get(JsonElement el, string prop, string fallback = "N/A")
    {
        return AutomationSnapshotFormatter.Get(el, prop, fallback);
    }
}

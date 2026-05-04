using System.ComponentModel;
using System.Text.Json;
using Sussudio.Tools;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace McpServer.Tools;

[McpServerToolType]
public static class RecordingTools
{
    [McpServerTool, Description("Start or stop recording")]
    public static async Task<CallToolResult> control_recording(
        PipeClient pipeClient,
        [Description("True to start recording, false to stop")] bool enabled)
    {
        var payload = new Dictionary<string, object?>
        {
            ["enabled"] = enabled
        };

        return await ToolCommandFormatter.ExecuteAndFormatResultAsync(
                pipeClient,
                "SetRecordingEnabled",
                "SetRecordingEnabled",
                payload)
            .ConfigureAwait(false);
    }

}

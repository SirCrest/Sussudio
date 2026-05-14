using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

static partial class Program
{
    private static async Task McpToolCommandFormatter_BatchesPendingCommands()
    {
        var pipeName = NewMcpToolPipeName("formatter");
        var pipeClient = CreateMcpPipeClient(pipeName);
        var formatterType = RequireMcpType("McpServer.Tools.ToolCommandFormatter");
        var optional = formatterType.GetMethod(
                "Optional",
                BindingFlags.Static | BindingFlags.NonPublic,
                binder: null,
                types:
                [
                    typeof(string),
                    typeof(string),
                    typeof(bool),
                    typeof(Dictionary<string, object?>)
                ],
                modifiers: null)
            ?? throw new InvalidOperationException("ToolCommandFormatter.Optional overload was not found.");
        var pendingType = optional.ReturnType;
        var executeBatch = formatterType.GetMethod(
                "ExecuteBatchAsync",
                BindingFlags.Static | BindingFlags.NonPublic,
                binder: null,
                types:
                [
                    pipeClient.GetType(),
                    typeof(string),
                    pendingType.MakeArrayType()
                ],
                modifiers: null)
            ?? throw new InvalidOperationException("ToolCommandFormatter.ExecuteBatchAsync was not found.");
        var emptyCommands = Array.CreateInstance(pendingType, 0);
        var emptyResult = await InvokeFormatterBatchAsync(executeBatch, pipeClient, "nothing to do", emptyCommands).ConfigureAwait(false);
        AssertEqual("nothing to do", emptyResult, "ToolCommandFormatter empty batch result");

        var skipped = optional.Invoke(
            null,
            new object?[]
            {
                "SetShowAllCaptureOptions",
                "SetShowAllCaptureOptions",
                false,
                new Dictionary<string, object?> { ["enabled"] = true }
            });
        var firstPending = optional.Invoke(
            null,
            new object?[]
            {
                "SetStatsVisible",
                "SetStatsVisible",
                true,
                new Dictionary<string, object?> { ["visible"] = true }
            });
        var secondPending = optional.Invoke(
            null,
            new object?[]
            {
                "SetSettingsVisible",
                "SetSettingsVisible",
                true,
                new Dictionary<string, object?> { ["visible"] = false }
            });
        var commands = Array.CreateInstance(pendingType, 3);
        commands.SetValue(skipped, 0);
        commands.SetValue(firstPending, 1);
        commands.SetValue(secondPending, 2);

        string result = string.Empty;
        var requests = await CapturePipeRequestsAsync(
                pipeName,
                expectedCount: 2,
                async () =>
                {
                    result = await InvokeFormatterBatchAsync(executeBatch, pipeClient, "nothing to do", commands).ConfigureAwait(false);
                },
                i => i == 0
                    ? "{\"Success\":true,\"Message\":\"stats updated\"}"
                    : "{\"Success\":false,\"Message\":\"settings blocked\"}")
            .ConfigureAwait(false);

        AssertCommandRequest(requests[0], "SetStatsVisible", ("visible", true));
        AssertCommandRequest(requests[1], "SetSettingsVisible", ("visible", false));
        AssertEqual(
            "[OK] SetStatsVisible: stats updated" + Environment.NewLine + "[ERROR] SetSettingsVisible: settings blocked",
            result,
            "ToolCommandFormatter ordered joined batch result");
    }
}

using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

static partial class Program
{
    private static async Task McpHostToolSchema_UsesPipeClientAsService()
    {
        var assemblyPath = Path.Combine("tools", "McpServer", "bin", "Debug", "net8.0", "McpServer.dll");
        LoadToolAssemblyIsolated(assemblyPath);

        using var process = StartMcpServerProcess(
            assemblyPath,
            NewMcpToolPipeName("host-pipe-failure"));
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        _ = Task.Run(async () =>
        {
            try
            {
                await process.StandardError.ReadToEndAsync().ConfigureAwait(false);
            }
            catch
            {
            }
        });

        try
        {
            await WriteJsonRpcLineAsync(
                    process,
                    """
                    {"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05","capabilities":{},"clientInfo":{"name":"Sussudio.Tests","version":"1.0"}}}
                    """,
                    cts.Token)
                .ConfigureAwait(false);
            await ReadJsonRpcResponseAsync(process, 1, cts.Token).ConfigureAwait(false);

            await WriteJsonRpcLineAsync(
                    process,
                    """
                    {"jsonrpc":"2.0","method":"notifications/initialized","params":{}}
                    """,
                    cts.Token)
                .ConfigureAwait(false);
            await WriteJsonRpcLineAsync(
                    process,
                    """
                    {"jsonrpc":"2.0","id":2,"method":"tools/list","params":{}}
                    """,
                    cts.Token)
                .ConfigureAwait(false);

            using var toolsListDocument = await ReadJsonRpcResponseAsync(process, 2, cts.Token).ConfigureAwait(false);
            var tools = toolsListDocument.RootElement.GetProperty("result").GetProperty("tools");
            AssertNoToolSchemaExposesPipeClient(tools);
        }
        finally
        {
            await StopMcpServerProcessAsync(process).ConfigureAwait(false);
        }
    }

    private static async Task McpPipeClient_HonorsSussudioAutomationPipeEnvironment()
    {
        var pipeName = NewMcpToolPipeName("env");
        var previousPipeName = Environment.GetEnvironmentVariable("SUSSUDIO_AUTOMATION_PIPE");
        Environment.SetEnvironmentVariable("SUSSUDIO_AUTOMATION_PIPE", pipeName);
        try
        {
            var pipeClient = CreateDefaultMcpPipeClient();
            var appStateTools = RequireMcpType("McpServer.Tools.AppStateTools");

            var requests = await CapturePipeRequestsAsync(
                    pipeName,
                    expectedCount: 1,
                    async () =>
                    {
                        _ = await InvokeMcpToolResultAsync(
                                appStateTools,
                                "get_app_state_raw",
                                pipeClient)
                            .ConfigureAwait(false);
                    },
                    _ => "{\"Success\":true,\"Snapshot\":{\"SessionState\":\"Ready\"}}")
                .ConfigureAwait(false);

            AssertCommandRequest(requests[0], "GetSnapshot");
        }
        finally
        {
            Environment.SetEnvironmentVariable("SUSSUDIO_AUTOMATION_PIPE", previousPipeName);
        }
    }

    private static async Task McpHostToolInvocation_ReturnsPipeFailureInsteadOfClosingTransport()
    {
        var assemblyPath = Path.Combine("tools", "McpServer", "bin", "Debug", "net8.0", "McpServer.dll");
        LoadToolAssemblyIsolated(assemblyPath);

        using var process = StartMcpServerProcess(
            assemblyPath,
            NewMcpToolPipeName("host-pipe-failure"));
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        _ = Task.Run(async () =>
        {
            try
            {
                await process.StandardError.ReadToEndAsync().ConfigureAwait(false);
            }
            catch
            {
            }
        });

        try
        {
            await WriteJsonRpcLineAsync(
                    process,
                    """
                    {"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05","capabilities":{},"clientInfo":{"name":"Sussudio.Tests","version":"1.0"}}}
                    """,
                    cts.Token)
                .ConfigureAwait(false);
            await ReadJsonRpcResponseAsync(process, 1, cts.Token).ConfigureAwait(false);

            await WriteJsonRpcLineAsync(
                    process,
                    """
                    {"jsonrpc":"2.0","method":"notifications/initialized","params":{}}
                    """,
                    cts.Token)
                .ConfigureAwait(false);
            await WriteJsonRpcLineAsync(
                    process,
                    """
                    {"jsonrpc":"2.0","id":2,"method":"tools/call","params":{"name":"get_app_state","arguments":{}}}
                    """,
                    cts.Token)
                .ConfigureAwait(false);

            using var response = await ReadJsonRpcResponseAsync(process, 2, cts.Token).ConfigureAwait(false);
            var resultElement = response.RootElement.GetProperty("result");
            AssertEqual(true, resultElement.GetProperty("isError").GetBoolean(), "get_app_state pipe failure MCP isError");
            var content = resultElement.GetProperty("content");
            var text = content[0].GetProperty("text").GetString() ?? string.Empty;
            AssertContains(text, "Timed out connecting to automation pipe");
            AssertContains(text, "pipe-connect-timeout");

            await WriteJsonRpcLineAsync(
                    process,
                    """
                    {"jsonrpc":"2.0","id":3,"method":"tools/list","params":{}}
                    """,
                    cts.Token)
                .ConfigureAwait(false);
            using var toolsListResponse = await ReadJsonRpcResponseAsync(process, 3, cts.Token).ConfigureAwait(false);
            AssertEqual(
                true,
                toolsListResponse.RootElement.GetProperty("result").GetProperty("tools").GetArrayLength() > 0,
                "MCP transport remains open after pipe failure");
        }
        finally
        {
            await StopMcpServerProcessAsync(process).ConfigureAwait(false);
        }
    }
}

using System.Text.Json;
using System.Text.Json.Nodes;
using Xunit;

namespace Sussudio.Tests
{
    public sealed class McpPresentMonResultTests
    {
        [Fact]
        public Task HostPreservesRawAndFormattedPresentMonErrors()
            => global::Program.McpPresentMon_HostPreservesRawAndFormattedErrors();
    }
}

static partial class Program
{
    internal static async Task McpPresentMon_HostPreservesRawAndFormattedErrors()
    {
        const string processName = "MissingPresentMonTestTarget";
        const string expectedMessage = "No running process matched pid=-1 name='MissingPresentMonTestTarget'.";
        var assemblyPath = McpServerAssemblyRelativePath;
        LoadToolAssemblyIsolated(assemblyPath);
        var pipeName = NewMcpToolPipeName("presentmon-error-host");
        using var process = StartMcpServerProcess(assemblyPath, pipeName);
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        var stderr = process.StandardError.ReadToEndAsync();
        try
        {
            await WriteJsonRpcLineAsync(process,
                """{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2025-06-18","capabilities":{},"clientInfo":{"name":"Sussudio.Tests","version":"1.0"}}}""",
                deadline.Token).ConfigureAwait(false);
            using var initialized = await ReadJsonRpcResponseAsync(process, 1, deadline.Token).ConfigureAwait(false);
            await WriteJsonRpcLineAsync(process,
                """{"jsonrpc":"2.0","method":"notifications/initialized","params":{}}""",
                deadline.Token).ConfigureAwait(false);
            await WriteJsonRpcLineAsync(process,
                """{"jsonrpc":"2.0","id":2,"method":"tools/list","params":{}}""",
                deadline.Token).ConfigureAwait(false);
            using var listed = await ReadJsonRpcResponseAsync(process, 2, deadline.Token).ConfigureAwait(false);
            var tools = listed.RootElement.GetProperty("result").GetProperty("tools");
            AssertNoToolSchemaExposesPipeClient(tools);
            var rawSchema = tools.EnumerateArray()
                .Single(tool => tool.GetProperty("name").GetString() == "capture_presentmon_raw");
            Assert.Equal("object", rawSchema.GetProperty("inputSchema").GetProperty("type").GetString());
            if (rawSchema.TryGetProperty("outputSchema", out var outputSchema))
            {
                Assert.Equal("object", outputSchema.GetProperty("type").GetString());
                if (outputSchema.TryGetProperty("properties", out var properties))
                {
                    Assert.False(properties.TryGetProperty("result", out _));
                }
            }

            var requests = await CapturePipeRequestsAsync(pipeName, 2, async () =>
            {
                foreach (var (method, id) in new[] { ("capture_presentmon_raw", 3), ("capture_presentmon", 4) })
                {
                    // A negative PID fails target resolution before PresentMon or CSV setup.
                    await WriteJsonRpcLineAsync(process, JsonSerializer.Serialize(new
                    {
                        jsonrpc = "2.0", id, method = "tools/call",
                        @params = new { name = method, arguments = new { seconds = 1, processId = -1, processName } }
                    }), deadline.Token).ConfigureAwait(false);
                    using var response = await ReadJsonRpcResponseAsync(process, id, deadline.Token).ConfigureAwait(false);
                    var result = response.RootElement.GetProperty("result");
                    Assert.True(result.GetProperty("isError").GetBoolean());
                    Assert.False(result.TryGetProperty("result", out _));
                    var content = result.GetProperty("content");
                    Assert.Equal(1, content.GetArrayLength());
                    Assert.Equal("text", content[0].GetProperty("type").GetString());
                    var text = content[0].GetProperty("text").GetString()!;

                    if (method == "capture_presentmon_raw")
                    {
                        var structured = result.GetProperty("structuredContent");
                        Assert.Equal(JsonValueKind.Object, structured.ValueKind);
                        Assert.False(structured.GetProperty("success").GetBoolean());
                        Assert.Equal(expectedMessage, structured.GetProperty("message").GetString());
                        Assert.False(structured.TryGetProperty("result", out _));
                        Assert.All(structured.EnumerateObject(), property => Assert.True(char.IsLower(property.Name[0])));
                        Assert.True(JsonNode.DeepEquals(JsonNode.Parse(structured.GetRawText()), JsonNode.Parse(text)),
                            "The raw tool's JSON text must contain the same payload as structuredContent.");
                    }
                    else
                    {
                        Assert.Equal(expectedMessage, text);
                        Assert.False(result.TryGetProperty("structuredContent", out var structured) &&
                            structured.ValueKind != JsonValueKind.Null);
                    }
                }
            }, _ => """{"Success":true,"Snapshot":{"PreviewD3DSwapChainAddress":"0xABCDEF","PreviewD3DLastRenderedPreviewPresentId":42,"PreviewD3DLastRenderedSourceSequenceNumber":0,"PreviewD3DLastRenderedUtcUnixMs":1700000000000}}""")
                .ConfigureAwait(false);
            Assert.All(requests, request => AssertCommandRequest(request, "GetSnapshot"));
        }
        finally
        {
            await StopMcpServerProcessAsync(process).ConfigureAwait(false);
            await stderr.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
        }
    }
}

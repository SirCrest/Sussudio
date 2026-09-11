using System.IO.Pipes;
using System.Reflection;
using System.Text.Json;
using Xunit;

namespace Sussudio.Tests
{
    public sealed class McpCancellationTests
    {
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public Task PreCanceledPipeCommandDoesNotConnect(bool typedCommand)
            => global::Program.McpCancellation_PreCanceledPipeCommandDoesNotConnect(typedCommand);

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public Task CancelingResponseWaitClosesThePipe(bool typedCommand)
            => global::Program.McpCancellation_CancelingResponseWaitClosesThePipe(typedCommand);

        [Theory]
        [InlineData("ExecuteBatchAsync")]
        [InlineData("ExecuteBatchResultAsync")]
        public Task CancelingBatchClosesFirstRequestAndDoesNotSendSecond(string methodName)
            => global::Program.McpCancellation_CancelingBatchStopsBeforeSecondCommand(methodName);

        [Fact]
        public Task HostHidesCancellationTokenAndCancelsActivePipeRequest()
            => global::Program.McpCancellation_HostHidesTokenAndCancelsActivePipeRequest();
    }
}

static partial class Program
{
    internal static async Task McpCancellation_PreCanceledPipeCommandDoesNotConnect(bool typedCommand)
    {
        var pipeName = NewMcpToolPipeName("pre-canceled");
        using var server = CreateMcpCancellationTestPipe(pipeName);
        using var acceptCancellation = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var accept = server.WaitForConnectionAsync(acceptCancellation.Token);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        var command = StartMcpCancellationPipeCommand(CreateMcpPipeClient(pipeName), typedCommand, cancellation.Token);
        var error = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => command.WaitAsync(TimeSpan.FromSeconds(5))).ConfigureAwait(false);

        Assert.Equal(cancellation.Token, error.CancellationToken);
        Assert.True(command.IsCanceled);
        await AssertMcpCancellationConnectionAbsentAsync(accept).ConfigureAwait(false);
        acceptCancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => accept).ConfigureAwait(false);
    }

    internal static async Task McpCancellation_CancelingResponseWaitClosesThePipe(bool typedCommand)
    {
        var pipeName = NewMcpToolPipeName("cancel-response");
        using var server = CreateMcpCancellationTestPipe(pipeName);
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var cancellation = new CancellationTokenSource();
        var accept = server.WaitForConnectionAsync(deadline.Token);
        var command = StartMcpCancellationPipeCommand(CreateMcpPipeClient(pipeName), typedCommand, cancellation.Token);

        await accept.ConfigureAwait(false);
        using var reader = new StreamReader(server, leaveOpen: true);
        using var request = JsonDocument.Parse(
            await reader.ReadLineAsync(deadline.Token).ConfigureAwait(false)
            ?? throw new InvalidOperationException("The MCP client closed before sending its request."));
        AssertCommandRequest(request.RootElement, "GetSnapshot");

        cancellation.Cancel();
        var error = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => command.WaitAsync(TimeSpan.FromSeconds(5))).ConfigureAwait(false);

        Assert.Equal(cancellation.Token, error.CancellationToken);
        Assert.True(command.IsCanceled);
        await AssertMcpCancellationPipeDisconnectedAsync(reader, deadline.Token).ConfigureAwait(false);
    }

    internal static async Task McpCancellation_CancelingBatchStopsBeforeSecondCommand(string methodName)
    {
        var pipeName = NewMcpToolPipeName("cancel-batch");
        var pipeClient = CreateMcpPipeClient(pipeName);
        var formatter = RequireMcpType("McpServer.Tools.ToolCommandFormatter");
        var optional = formatter.GetMethods(BindingFlags.Static | BindingFlags.NonPublic)
            .Single(method => method.Name == "Optional" && !method.IsGenericMethodDefinition &&
                method.GetParameters().Length == 4 && method.GetParameters()[1].ParameterType == typeof(bool));
        var commandKind = optional.GetParameters()[0].ParameterType;
        var commands = Array.CreateInstance(optional.ReturnType, 2);
        commands.SetValue(optional.Invoke(null, new object?[]
        {
            Enum.Parse(commandKind, "SetStatsVisible"), true,
            new Dictionary<string, object?> { ["visible"] = true }, null
        }), 0);
        commands.SetValue(optional.Invoke(null, new object?[]
        {
            Enum.Parse(commandKind, "SetSettingsVisible"), true,
            new Dictionary<string, object?> { ["visible"] = false }, null
        }), 1);
        var execute = formatter.GetMethods(BindingFlags.Static | BindingFlags.NonPublic)
            .Single(method => method.Name == methodName && method.GetParameters().Length == 4 &&
                method.GetParameters()[2].ParameterType == typeof(CancellationToken));

        using var firstServer = CreateMcpCancellationTestPipe(pipeName);
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var cancellation = new CancellationTokenSource();
        var firstAccept = firstServer.WaitForConnectionAsync(deadline.Token);
        var batch = (Task)execute.Invoke(null, new object?[] { pipeClient, "empty batch", cancellation.Token, commands })!;
        await firstAccept.ConfigureAwait(false);
        using var firstReader = new StreamReader(firstServer, leaveOpen: true);
        using var request = JsonDocument.Parse(
            await firstReader.ReadLineAsync(deadline.Token).ConfigureAwait(false)
            ?? throw new InvalidOperationException("The MCP batch closed before sending its first request."));
        AssertCommandRequest(request.RootElement, "SetStatsVisible", ("visible", true));

        // Keep another server instance accepting while the first response is pending.
        // A batch that continued after cancellation would connect here.
        using var secondServer = CreateMcpCancellationTestPipe(pipeName);
        using var secondAcceptCancellation = CancellationTokenSource.CreateLinkedTokenSource(deadline.Token);
        var secondAccept = secondServer.WaitForConnectionAsync(secondAcceptCancellation.Token);
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => batch.WaitAsync(TimeSpan.FromSeconds(5))).ConfigureAwait(false);
        Assert.True(batch.IsCanceled);
        await AssertMcpCancellationPipeDisconnectedAsync(firstReader, deadline.Token).ConfigureAwait(false);
        await AssertMcpCancellationConnectionAbsentAsync(secondAccept).ConfigureAwait(false);
        secondAcceptCancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => secondAccept).ConfigureAwait(false);
    }

    internal static async Task McpCancellation_HostHidesTokenAndCancelsActivePipeRequest()
    {
        var assemblyPath = McpServerAssemblyRelativePath;
        LoadToolAssemblyIsolated(assemblyPath);
        var pipeName = NewMcpToolPipeName("host-cancellation");
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
            foreach (var tool in tools.EnumerateArray())
            {
                var inputSchema = tool.GetProperty("inputSchema");
                if (inputSchema.TryGetProperty("properties", out var properties))
                {
                    Assert.DoesNotContain(properties.EnumerateObject(),
                        property => string.Equals(property.Name, "cancellationToken", StringComparison.OrdinalIgnoreCase));
                }
                if (inputSchema.TryGetProperty("required", out var required))
                {
                    Assert.DoesNotContain(required.EnumerateArray(),
                        property => string.Equals(property.GetString(), "cancellationToken", StringComparison.OrdinalIgnoreCase));
                }
            }

            using var server = CreateMcpCancellationTestPipe(pipeName);
            var accept = server.WaitForConnectionAsync(deadline.Token);
            await WriteJsonRpcLineAsync(process,
                """{"jsonrpc":"2.0","id":3,"method":"tools/call","params":{"name":"get_app_state_raw","arguments":{}}}""",
                deadline.Token).ConfigureAwait(false);
            await accept.ConfigureAwait(false);
            using var reader = new StreamReader(server, leaveOpen: true);
            using var request = JsonDocument.Parse(
                await reader.ReadLineAsync(deadline.Token).ConfigureAwait(false)
                ?? throw new InvalidOperationException("The MCP host closed before sending its request."));
            AssertCommandRequest(request.RootElement, "GetSnapshot");

            await WriteJsonRpcLineAsync(process,
                """{"jsonrpc":"2.0","method":"notifications/cancelled","params":{"requestId":3,"reason":"test cancellation"}}""",
                deadline.Token).ConfigureAwait(false);
            await AssertMcpCancellationPipeDisconnectedAsync(reader, deadline.Token).ConfigureAwait(false);

            await WriteJsonRpcLineAsync(process,
                """{"jsonrpc":"2.0","id":4,"method":"ping","params":{}}""",
                deadline.Token).ConfigureAwait(false);
            using var ping = await ReadJsonRpcResponseAsync(process, 4, deadline.Token).ConfigureAwait(false);
            Assert.Equal(JsonValueKind.Object, ping.RootElement.GetProperty("result").ValueKind);
            Assert.False(process.HasExited);
        }
        finally
        {
            await StopMcpServerProcessAsync(process).ConfigureAwait(false);
            await stderr.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
        }
    }

    private static NamedPipeServerStream CreateMcpCancellationTestPipe(string pipeName)
        => new(pipeName, PipeDirection.InOut, 2, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);

    private static Task StartMcpCancellationPipeCommand(object pipeClient, bool typedCommand, CancellationToken cancellationToken)
    {
        var method = pipeClient.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Single(candidate => candidate.Name == "SendCommandAsync" && candidate.GetParameters().Length == 4 &&
                (candidate.GetParameters()[0].ParameterType == typeof(string)) != typedCommand);
        var commandType = method.GetParameters()[0].ParameterType;
        var command = typedCommand ? Enum.Parse(commandType, "GetSnapshot") : "GetSnapshot";
        return (Task)method.Invoke(pipeClient, new object?[] { command, null, 30_000, cancellationToken })!;
    }

    // Proving a connection never arrives is a negative/absence assertion: there is
    // no deterministic completion signal to await (nothing will ever complete
    // `accept` in the passing case), so a finite wall-clock wait is unavoidable
    // here. Keep this at 250ms -- it is used at exactly two call sites and no
    // flakiness at this bound has been observed; widening it "just in case" would
    // add roughly 3.5s of dead wall-clock time to every suite run. If CI later
    // shows real timeouts at this bound, raise it (e.g. to 500ms) in a change that
    // cites the failing run, rather than pre-emptively.
    private const int NoConnectionWindowMs = 250;

    private static async Task AssertMcpCancellationConnectionAbsentAsync(Task accept)
    {
        await Assert.ThrowsAsync<TimeoutException>(
            () => accept.WaitAsync(TimeSpan.FromMilliseconds(NoConnectionWindowMs))).ConfigureAwait(false);
    }

    private static async Task AssertMcpCancellationPipeDisconnectedAsync(StreamReader reader, CancellationToken cancellationToken)
    {
        try
        {
            var nextLine = await reader.ReadLineAsync(cancellationToken).AsTask()
                .WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
            Assert.Null(nextLine);
        }
        catch (IOException)
        {
            // Windows can report a closed named pipe as a broken-pipe I/O error.
        }
    }
}

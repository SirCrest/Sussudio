using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

static partial class Program
{
    private static Process StartMcpServerProcess(string assemblyPath, string? pipeName = null)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            WorkingDirectory = GetRepoRoot(),
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        startInfo.ArgumentList.Add(Path.GetFullPath(assemblyPath));
        if (!string.IsNullOrWhiteSpace(pipeName))
        {
            startInfo.Environment["SUSSUDIO_AUTOMATION_PIPE"] = pipeName;
        }

        var process = new Process { StartInfo = startInfo };
        if (!process.Start())
        {
            throw new InvalidOperationException("Failed to start MCP server process.");
        }

        return process;
    }

    private static async Task WriteJsonRpcLineAsync(Process process, string json, CancellationToken cancellationToken)
    {
        await process.StandardInput.WriteLineAsync(CompactJsonLine(json))
            .WaitAsync(cancellationToken)
            .ConfigureAwait(false);
        await process.StandardInput.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task<JsonDocument> ReadJsonRpcResponseAsync(Process process, int id, CancellationToken cancellationToken)
    {
        while (true)
        {
            var line = await process.StandardOutput.ReadLineAsync()
                .WaitAsync(cancellationToken)
                .ConfigureAwait(false);
            if (line is null)
            {
                var exitText = process.HasExited ? $" Process exited with code {process.ExitCode}." : string.Empty;
                throw new InvalidOperationException($"MCP server closed stdout before response id {id}.{exitText}");
            }

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var document = JsonDocument.Parse(line);
            var root = document.RootElement;
            if (!root.TryGetProperty("id", out var responseId) ||
                responseId.ValueKind != JsonValueKind.Number ||
                responseId.GetInt32() != id)
            {
                document.Dispose();
                continue;
            }

            if (root.TryGetProperty("error", out var error))
            {
                var errorText = error.GetRawText();
                document.Dispose();
                throw new InvalidOperationException($"MCP server returned error for response id {id}: {errorText}");
            }

            return document;
        }
    }

    private static async Task StopMcpServerProcessAsync(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.StandardInput.Close();
            }
        }
        catch
        {
        }

        if (!process.HasExited)
        {
            process.Kill(entireProcessTree: true);
        }

        try
        {
            await process.WaitForExitAsync()
                .WaitAsync(TimeSpan.FromSeconds(3))
                .ConfigureAwait(false);
        }
        catch
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
    }

    private static void AssertNoToolSchemaExposesPipeClient(JsonElement tools)
    {
        var checkedCount = 0;
        foreach (var tool in tools.EnumerateArray())
        {
            checkedCount++;
            var toolName = tool.GetProperty("name").GetString() ?? "<unnamed>";
            var inputSchema = tool.GetProperty("inputSchema");
            if (inputSchema.TryGetProperty("properties", out var properties) &&
                properties.TryGetProperty("pipeClient", out _))
            {
                throw new InvalidOperationException($"{toolName} exposes pipeClient in the MCP input schema.");
            }

            if (inputSchema.TryGetProperty("required", out var required))
            {
                foreach (var item in required.EnumerateArray())
                {
                    if (string.Equals(item.GetString(), "pipeClient", StringComparison.Ordinal))
                    {
                        throw new InvalidOperationException($"{toolName} requires pipeClient in the MCP input schema.");
                    }
                }
            }
        }

        if (checkedCount == 0)
        {
            throw new InvalidOperationException("MCP host did not list any tools.");
        }
    }

    private static Type RequireMcpType(string typeName)
    {
        var assembly = LoadToolAssemblyIsolated(Path.Combine("tools", "McpServer", "bin", "Debug", "net8.0", "McpServer.dll"));
        return assembly.GetType(typeName)
               ?? throw new InvalidOperationException($"{typeName} was not found in McpServer.dll.");
    }

    private static object CreateMcpPipeClient(string pipeName)
    {
        var type = RequireMcpType("McpServer.PipeClient");
        return Activator.CreateInstance(
                   type,
                   BindingFlags.Instance | BindingFlags.NonPublic,
                   binder: null,
                   args: new object?[] { pipeName },
                   culture: null)
               ?? throw new InvalidOperationException("Failed to create MCP PipeClient.");
    }

    private static object CreateDefaultMcpPipeClient()
    {
        var type = RequireMcpType("McpServer.PipeClient");
        return Activator.CreateInstance(type)
               ?? throw new InvalidOperationException("Failed to create default MCP PipeClient.");
    }

    private static async Task<string> InvokeMcpToolStringAsync(Type type, string methodName, params object?[] args)
    {
        var method = ResolveMcpToolMethod(type, methodName, args.Length);
        var task = method.Invoke(null, args) as Task
            ?? throw new InvalidOperationException($"{type.FullName}.{methodName} did not return a Task.");
        await task.ConfigureAwait(false);
        var result = task.GetType().GetProperty("Result")?.GetValue(task)
            ?? throw new InvalidOperationException($"{type.FullName}.{methodName} returned null.");
        return result is string text
            ? text
            : GetMcpToolResultText(result);
    }

    private static async Task<object> InvokeMcpToolResultAsync(Type type, string methodName, params object?[] args)
    {
        var method = ResolveMcpToolMethod(type, methodName, args.Length);
        var task = method.Invoke(null, args) as Task
            ?? throw new InvalidOperationException($"{type.FullName}.{methodName} did not return a Task.");
        await task.ConfigureAwait(false);
        return task.GetType().GetProperty("Result")?.GetValue(task)
            ?? throw new InvalidOperationException($"{type.FullName}.{methodName} returned null.");
    }

    private static MethodInfo ResolveMcpToolMethod(Type type, string methodName, int argumentCount)
    {
        var methods = type.GetMethods(BindingFlags.Static | BindingFlags.Public)
            .Where(method => string.Equals(method.Name, methodName, StringComparison.Ordinal))
            .ToArray();
        if (methods.Length == 0)
        {
            throw new InvalidOperationException($"{type.FullName}.{methodName} was not found.");
        }

        var matchingMethod = methods.SingleOrDefault(method => method.GetParameters().Length == argumentCount);
        if (matchingMethod != null)
        {
            return matchingMethod;
        }

        var shapes = string.Join(
            ", ",
            methods.Select(method => $"{method.Name}({string.Join(", ", method.GetParameters().Select(parameter => parameter.ParameterType.Name))})"));
        throw new InvalidOperationException(
            $"{type.FullName}.{methodName} had no overload accepting {argumentCount} argument(s). Available: {shapes}");
    }

    private static string GetMcpToolResultText(object? result)
    {
        if (result is null)
        {
            throw new InvalidOperationException("MCP tool result was null.");
        }

        var content = GetPropertyValue(result, "Content") as System.Collections.IEnumerable
            ?? throw new InvalidOperationException("MCP tool result content was not enumerable.");
        foreach (var item in content)
        {
            var text = GetPropertyValue(item, "Text") as string;
            if (text is not null)
            {
                return text;
            }
        }

        throw new InvalidOperationException("MCP tool result did not contain text content.");
    }

    private static bool GetMcpToolResultIsError(object? result)
    {
        if (result is null)
        {
            throw new InvalidOperationException("MCP tool result was null.");
        }

        return Convert.ToBoolean(GetPropertyValue(result, "IsError"));
    }

    private static async Task<string> InvokeFormatterBatchAsync(
        MethodInfo executeBatch,
        object pipeClient,
        string emptyMessage,
        Array commands)
    {
        var task = executeBatch.Invoke(null, new object?[] { pipeClient, emptyMessage, commands }) as Task<string>
            ?? throw new InvalidOperationException("ToolCommandFormatter.ExecuteBatchAsync did not return Task<string>.");
        return await task.ConfigureAwait(false);
    }

    private static async Task<JsonElement> CapturePipeRequestAsync(string pipeName, Func<Task> clientAction)
    {
        var requests = await CapturePipeRequestsAsync(
                pipeName,
                expectedCount: 1,
                clientAction,
                _ => "{\"Success\":true}")
            .ConfigureAwait(false);
        return requests[0];
    }

    private static async Task<JsonElement[]> CapturePipeRequestsAsync(
        string pipeName,
        int expectedCount,
        Func<Task> clientAction,
        Func<int, string>? responseFactory = null)
    {
        var requests = new List<JsonElement>();
        var clientTask = Task.Run(clientAction);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        for (var i = 0; i < expectedCount; i++)
        {
            using var serverPipe = new System.IO.Pipes.NamedPipeServerStream(
                pipeName,
                System.IO.Pipes.PipeDirection.InOut,
                1,
                System.IO.Pipes.PipeTransmissionMode.Byte,
                System.IO.Pipes.PipeOptions.Asynchronous);

            var connectTask = serverPipe.WaitForConnectionAsync(cts.Token);
            if (await Task.WhenAny(connectTask, clientTask).ConfigureAwait(false) == clientTask)
            {
                if (clientTask.IsFaulted || clientTask.IsCanceled)
                {
                    await clientTask.ConfigureAwait(false);
                }

                throw new InvalidOperationException(
                    $"Expected pipe request {i + 1} of {expectedCount}, but the client action completed after {requests.Count} request(s).");
            }

            try
            {
                await connectTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException ex)
            {
                throw new TimeoutException(
                    $"Expected pipe request {i + 1} of {expectedCount}, but no connection arrived after {requests.Count} request(s).",
                    ex);
            }

            using var reader = new StreamReader(serverPipe, leaveOpen: true);
            var readTask = reader.ReadLineAsync().WaitAsync(cts.Token);
            if (await Task.WhenAny(readTask, clientTask).ConfigureAwait(false) == clientTask)
            {
                if (clientTask.IsFaulted || clientTask.IsCanceled)
                {
                    await clientTask.ConfigureAwait(false);
                }

                throw new InvalidOperationException(
                    $"Expected request payload {i + 1} of {expectedCount}, but the client action completed after {requests.Count} complete request(s).");
            }

            string? requestLine;
            try
            {
                requestLine = await readTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException ex)
            {
                throw new TimeoutException(
                    $"Expected request payload {i + 1} of {expectedCount}, but no payload arrived after {requests.Count} complete request(s).",
                    ex);
            }

            if (requestLine is null)
            {
                throw new InvalidOperationException(
                    $"Expected request payload {i + 1} of {expectedCount}, but the pipe closed after {requests.Count} complete request(s).");
            }

            using var document = JsonDocument.Parse(requestLine);
            requests.Add(document.RootElement.Clone());

            using var writer = new StreamWriter(serverPipe, leaveOpen: true) { AutoFlush = true };
            await writer.WriteLineAsync(CompactJsonLine(responseFactory?.Invoke(i) ?? "{\"Success\":true,\"Message\":\"ok\"}"))
                .WaitAsync(cts.Token)
                .ConfigureAwait(false);
        }

        await EnsureNoUnexpectedPipeRequestAsync(pipeName, expectedCount, requests.Count, clientTask, cts.Token).ConfigureAwait(false);
        return requests.ToArray();
    }

    private static async Task EnsureNoUnexpectedPipeRequestAsync(
        string pipeName,
        int expectedCount,
        int capturedCount,
        Task clientTask,
        CancellationToken cancellationToken)
    {
        using var extraServerPipe = new System.IO.Pipes.NamedPipeServerStream(
            pipeName,
            System.IO.Pipes.PipeDirection.InOut,
            1,
            System.IO.Pipes.PipeTransmissionMode.Byte,
            System.IO.Pipes.PipeOptions.Asynchronous);

        using var probeCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var extraConnectTask = extraServerPipe.WaitForConnectionAsync(probeCts.Token);
        var completed = await Task.WhenAny(clientTask, extraConnectTask).ConfigureAwait(false);
        if (completed == clientTask)
        {
            probeCts.Cancel();
            try
            {
                await extraConnectTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }

            await clientTask.ConfigureAwait(false);
            return;
        }

        try
        {
            await extraConnectTask.ConfigureAwait(false);
        }
        catch (OperationCanceledException ex)
        {
            throw new TimeoutException(
                $"Client action did not complete after {capturedCount} expected request(s).",
                ex);
        }

        using var reader = new StreamReader(extraServerPipe, leaveOpen: true);
        var extraRequestLine = await reader.ReadLineAsync()
            .WaitAsync(cancellationToken)
            .ConfigureAwait(false);

        using var writer = new StreamWriter(extraServerPipe, leaveOpen: true) { AutoFlush = true };
        await writer.WriteLineAsync("{\"Success\":true,\"Message\":\"unexpected request acknowledged\"}")
            .WaitAsync(cancellationToken)
            .ConfigureAwait(false);

        Exception? clientException = null;
        try
        {
            await clientTask.WaitAsync(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
        }
        catch (TimeoutException)
        {
        }
        catch (Exception ex)
        {
            clientException = ex;
        }

        var message =
            $"Unexpected pipe request {expectedCount + 1} received after the expected {expectedCount} request(s): {extraRequestLine ?? "<no payload>"}";
        if (clientException is not null)
        {
            throw new InvalidOperationException(message, clientException);
        }

        throw new InvalidOperationException(message);
    }

    private static string NewMcpToolPipeName(string suffix)
        => $"ec-mcp-{suffix}-{Guid.NewGuid():N}";

    private static string CompactJsonLine(string json)
    {
        using var document = JsonDocument.Parse(json);
        return JsonSerializer.Serialize(document.RootElement);
    }

    private static void AssertCommandRequest(JsonElement request, string commandName, params (string Key, object? Value)[] expectedPayload)
    {
        AssertEqual(GetExpectedAutomationCommandValue(commandName), request.GetProperty("command").GetInt32(), $"{commandName} command id");
        var payload = request.GetProperty("payload");
        if (expectedPayload.Length == 0)
        {
            if (payload.ValueKind == JsonValueKind.Object && payload.EnumerateObject().Any())
            {
                throw new InvalidOperationException($"{commandName} payload contained unexpected properties.");
            }

            if (payload.ValueKind is not JsonValueKind.Null and not JsonValueKind.Object)
            {
                throw new InvalidOperationException($"{commandName} payload had unexpected kind {payload.ValueKind}.");
            }

            return;
        }

        AssertJsonObjectPropertyNames(payload, expectedPayload.Select(item => item.Key).ToArray());
        foreach (var (key, value) in expectedPayload)
        {
            AssertJsonPropertyEquals(payload, key, value, $"{commandName}.{key}");
        }
    }

    private static void AssertContainsOrdinal(string value, string token)
    {
        if (!value.Contains(token, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Assertion failed: expected '{value}' to contain '{token}' with ordinal casing.");
        }
    }

    private static void AssertJsonObjectPropertyNames(JsonElement element, params string[] expectedPropertyNames)
    {
        AssertEqual(JsonValueKind.Object, element.ValueKind, "JSON object property-name assertion kind");
        var actual = element.EnumerateObject()
            .Select(property => property.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
        var expected = expectedPropertyNames
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
        AssertEqual(string.Join(",", expected), string.Join(",", actual), "JSON object property names");
    }

    private static int GetExpectedAutomationCommandValue(string commandName)
    {
        foreach (var (name, value) in ExpectedAutomationCommands())
        {
            if (string.Equals(name, commandName, StringComparison.Ordinal))
            {
                return value;
            }
        }

        throw new InvalidOperationException($"Expected automation command '{commandName}' was not found.");
    }

    private static void AssertJsonPropertyEquals(JsonElement element, string propertyName, object? expected, string fieldName)
    {
        if (!element.TryGetProperty(propertyName, out var actual))
        {
            throw new InvalidOperationException($"Assertion failed for {fieldName}: property was missing.");
        }

        switch (expected)
        {
            case null:
                AssertEqual(JsonValueKind.Null, actual.ValueKind, fieldName);
                break;
            case bool expectedBool:
                AssertEqual(expectedBool, actual.GetBoolean(), fieldName);
                break;
            case int expectedInt:
                AssertEqual(expectedInt, actual.GetInt32(), fieldName);
                break;
            case double expectedDouble:
                AssertEqual(expectedDouble, actual.GetDouble(), fieldName);
                break;
            case string expectedString:
                AssertEqual(expectedString, actual.GetString(), fieldName);
                break;
            default:
                throw new InvalidOperationException($"Unsupported expected JSON value type for {fieldName}: {expected.GetType().FullName}.");
        }
    }
}

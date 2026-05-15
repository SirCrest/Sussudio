using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

static partial class Program
{
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
            var request = document.RootElement.Clone();
            AssertCapturedPipeRequestEnvelope(request, i + 1);
            requests.Add(request);

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

    private static void AssertCapturedPipeRequestEnvelope(JsonElement request, int requestNumber)
    {
        AssertEqual(JsonValueKind.Object, request.ValueKind, $"pipe request {requestNumber} envelope kind");

        if (!request.TryGetProperty("command", out var command) ||
            command.ValueKind != JsonValueKind.Number ||
            !command.TryGetInt32(out _))
        {
            throw new InvalidOperationException($"Pipe request {requestNumber} envelope command was not a numeric command ID.");
        }

        if (!request.TryGetProperty("correlationId", out var correlationId) ||
            correlationId.ValueKind != JsonValueKind.String ||
            string.IsNullOrWhiteSpace(correlationId.GetString()))
        {
            throw new InvalidOperationException($"Pipe request {requestNumber} envelope correlationId was missing or empty.");
        }

        if (!request.TryGetProperty("manifestRevision", out var manifestRevision) ||
            manifestRevision.ValueKind != JsonValueKind.Number ||
            !manifestRevision.TryGetInt32(out var actualManifestRevision))
        {
            throw new InvalidOperationException($"Pipe request {requestNumber} envelope manifestRevision was not numeric.");
        }

        AssertEqual(
            Sussudio.Tools.AutomationPipeProtocol.CommandManifestRevision,
            actualManifestRevision,
            $"pipe request {requestNumber} envelope manifestRevision");

        if (!request.TryGetProperty("payload", out var payload) ||
            payload.ValueKind is not JsonValueKind.Object and not JsonValueKind.Null)
        {
            throw new InvalidOperationException($"Pipe request {requestNumber} envelope payload had unexpected kind {payload.ValueKind}.");
        }
    }
}

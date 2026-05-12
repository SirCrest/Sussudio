using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

static partial class Program
{
    private static async Task SsctlPipeTransport_ExposesAdvancedAutomationCommandIds()
    {
        var assemblyPath = Path.Combine("tools", "ssctl", "bin", "Debug", "net8.0", "ssctl.dll");
        var ssctlAssembly = LoadToolAssembly(assemblyPath);

        // Verify PipeTransport exposes expected command routing
        var transportType = ssctlAssembly.GetType("Sussudio.Tools.Ssctl.PipeTransport")
            ?? throw new InvalidOperationException("Sussudio.Tools.Ssctl.PipeTransport type not found.");
        var sendCommandAsync = transportType.GetMethod("SendCommandAsync", BindingFlags.Instance | BindingFlags.Public)
            ?? throw new InvalidOperationException("Sussudio.Tools.Ssctl.PipeTransport.SendCommandAsync not found.");

        var pipeName = $"ssctl-pipe-transport-{Guid.NewGuid():N}";
        var transport = Activator.CreateInstance(transportType, pipeName, (int?)null)
            ?? throw new InvalidOperationException("Failed to create PipeTransport for transport test.");
        var request = await CapturePipeRequestAsync(
            pipeName,
            async () =>
            {
                var task = sendCommandAsync.Invoke(
                    transport,
                    new object?[]
                    {
                        "SetPreviewVolume",
                        new Dictionary<string, object?> { ["previewVolumePercent"] = 55.5 },
                        null
                    }) as Task
                    ?? throw new InvalidOperationException("PipeTransport.SendCommandAsync did not return a Task.");
                await task.ConfigureAwait(false);
            }).ConfigureAwait(false);

        AssertEqual(34, request.GetProperty("command").GetInt32(), "PipeTransport SetPreviewVolume command id");
        AssertEqual(55.5, request.GetProperty("payload").GetProperty("previewVolumePercent").GetDouble(), "PipeTransport preview volume payload");

        JsonElement response = default;
        var responsePipeName = $"ssctl-pipe-response-{Guid.NewGuid():N}";
        var responseTransport = Activator.CreateInstance(transportType, responsePipeName, (int?)null)
            ?? throw new InvalidOperationException("Failed to create PipeTransport for response test.");
        var responseRequests = await CapturePipeRequestsAsync(
                responsePipeName,
                expectedCount: 1,
                async () =>
                {
                    response = await InvokePipeTransportSendCommandAsync(
                            sendCommandAsync,
                            responseTransport,
                            "GetSnapshot",
                            null,
                            null)
                        .ConfigureAwait(false);
                },
                _ => """
                     {
                       "Success": true,
                       "Message": "snapshot ready",
                       "Data": {
                         "value": 123
                       }
                     }
                     """)
            .ConfigureAwait(false);
        AssertEqual(1, responseRequests[0].GetProperty("command").GetInt32(), "PipeTransport GetSnapshot command id");
        AssertEqual("snapshot ready", response.GetProperty("Message").GetString(), "PipeTransport parsed response message");
        AssertEqual(123, response.GetProperty("Data").GetProperty("value").GetInt32(), "PipeTransport parsed response data");

        JsonElement retryResponse = default;
        var retryPipeName = $"ssctl-pipe-retry-{Guid.NewGuid():N}";
        var retryTransport = Activator.CreateInstance(transportType, retryPipeName, (int?)null)
            ?? throw new InvalidOperationException("Failed to create PipeTransport for retry test.");
        var retryRequests = await CapturePipeRequestsAsync(
                retryPipeName,
                expectedCount: 2,
                async () =>
                {
                    retryResponse = await InvokePipeTransportSendCommandAsync(
                            sendCommandAsync,
                            retryTransport,
                            "GetSnapshot",
                            null,
                            null)
                        .ConfigureAwait(false);
                },
                i => i == 0
                    ? """
                      {
                        "Success": false,
                        "Status": "not_ready",
                        "RetryAfterMs": 100,
                        "Message": "snapshot not ready"
                      }
                      """
                    : """
                      {
                        "Success": true,
                        "Message": "snapshot ready after retry",
                        "Data": {
                          "attempt": 2
                        }
                      }
                      """)
            .ConfigureAwait(false);
        AssertEqual(1, retryRequests[0].GetProperty("command").GetInt32(), "PipeTransport retry first command id");
        AssertEqual(1, retryRequests[1].GetProperty("command").GetInt32(), "PipeTransport retry second command id");
        AssertEqual("snapshot ready after retry", retryResponse.GetProperty("Message").GetString(), "PipeTransport retry final message");
        AssertEqual(2, retryResponse.GetProperty("Data").GetProperty("attempt").GetInt32(), "PipeTransport retry final data");

        // Round 1 behavior change: invalid JSON no longer throws; instead SendCommandAsync
        // catches JsonException and returns a synthetic {Success:false} error envelope.
        JsonElement invalidJsonResponse = default;
        var invalidPipeName = $"ssctl-pipe-invalid-{Guid.NewGuid():N}";
        var invalidTransport = Activator.CreateInstance(transportType, invalidPipeName, (int?)null)
            ?? throw new InvalidOperationException("Failed to create PipeTransport for invalid JSON test.");
        var invalidRequest = await CapturePipeRequestWithRawResponseAsync(
                invalidPipeName,
                async () =>
                {
                    invalidJsonResponse = await InvokePipeTransportSendCommandAsync(
                            sendCommandAsync,
                            invalidTransport,
                            "GetSnapshot",
                            null,
                            null)
                        .ConfigureAwait(false);
                },
                "not-json")
            .ConfigureAwait(false);
        AssertEqual(1, invalidRequest.GetProperty("command").GetInt32(), "PipeTransport invalid JSON request command id");
        AssertEqual(false, invalidJsonResponse.GetProperty("Success").GetBoolean(), "PipeTransport invalid JSON response Success=false");
        AssertEqual("pipe-invalid-json", invalidJsonResponse.GetProperty("ErrorCode").GetString(), "PipeTransport invalid JSON response ErrorCode");
        var invalidJsonMessage = invalidJsonResponse.GetProperty("Message").GetString() ?? "";
        AssertEqual(
            true,
            invalidJsonMessage.Contains("invalid JSON", StringComparison.OrdinalIgnoreCase) || invalidJsonMessage.Contains("pipe-invalid-json", StringComparison.OrdinalIgnoreCase),
            $"PipeTransport invalid JSON response Message should mention invalid JSON, got: {invalidJsonMessage}");

        var usageTransport = Activator.CreateInstance(transportType, $"ssctl-pipe-usage-{Guid.NewGuid():N}", (int?)null)
            ?? throw new InvalidOperationException("Failed to create PipeTransport for usage test.");
        Exception? usageException = null;
        try
        {
            await InvokePipeTransportSendCommandAsync(
                    sendCommandAsync,
                    usageTransport,
                    "DefinitelyNotACommand",
                    null,
                    null)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            usageException = ex;
        }

        AssertEqual("Sussudio.Tools.Ssctl.UsageException", usageException?.GetType().FullName, "PipeTransport unknown command exception type");
    }

    private static async Task<JsonElement> InvokePipeTransportSendCommandAsync(
        MethodInfo sendCommandAsync,
        object transport,
        string commandName,
        Dictionary<string, object?>? payload,
        int? responseTimeoutMs)
    {
        var task = sendCommandAsync.Invoke(
                transport,
                new object?[]
                {
                    commandName,
                    payload,
                    responseTimeoutMs
                }) as Task<JsonElement>
            ?? throw new InvalidOperationException("PipeTransport.SendCommandAsync did not return Task<JsonElement>.");
        return await task.ConfigureAwait(false);
    }

    private static async Task<JsonElement> CapturePipeRequestWithRawResponseAsync(
        string pipeName,
        Func<Task> clientAction,
        string rawResponseLine)
    {
        var clientTask = Task.Run(clientAction);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        string requestLine;
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
                await clientTask.ConfigureAwait(false);
                throw new InvalidOperationException("Expected raw-response pipe request, but the client completed before connecting.");
            }

            await connectTask.ConfigureAwait(false);
            using var reader = new StreamReader(serverPipe, leaveOpen: true);
            var readTask = reader.ReadLineAsync().WaitAsync(cts.Token);
            if (await Task.WhenAny(readTask, clientTask).ConfigureAwait(false) == clientTask)
            {
                await clientTask.ConfigureAwait(false);
                throw new InvalidOperationException("Expected raw-response pipe payload, but the client completed before sending one.");
            }

            try
            {
                requestLine = await readTask.ConfigureAwait(false)
                    ?? throw new InvalidOperationException("No request received on raw-response pipe.");
            }
            catch (OperationCanceledException ex)
            {
                throw new TimeoutException("Timed out waiting for raw-response pipe payload.", ex);
            }

            using var writer = new StreamWriter(serverPipe, leaveOpen: true) { AutoFlush = true };
            await writer.WriteLineAsync(rawResponseLine)
                .WaitAsync(cts.Token)
                .ConfigureAwait(false);
        }

        await EnsureNoUnexpectedPipeRequestAsync(pipeName, 1, 1, clientTask, cts.Token)
            .ConfigureAwait(false);

        using var document = JsonDocument.Parse(requestLine);
        return document.RootElement.Clone();
    }
}

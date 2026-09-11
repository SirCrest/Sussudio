using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;
using Microsoft.Win32.SafeHandles;
using Xunit;

namespace Sussudio.Tests
{
    public sealed class DiagnosticInfrastructureTests
    {
        [Theory]
        [InlineData("pre-summary")]
        [InlineData("summary")]
        [InlineData("live")]
        public Task ArtifactWriteFailuresPreserveRequiredAndBestEffortPolicies(string blockedOutput)
            => global::Program.DiagnosticInfrastructure_ArtifactWriteFailures(blockedOutput);

        [Fact]
        public Task CancelingAGateWaiterDoesNotReleaseTheActiveSend()
            => global::Program.DiagnosticInfrastructure_CanceledWaiterPreservesSerialization();

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public Task DisposalRejectsQueuedSendsAndPreservesTheAdmittedOutcome(bool failSend)
            => global::Program.DiagnosticInfrastructure_DisposeDuringSerializedSend(failSend);

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public Task RawSendsRemainConcurrentAndRetainOwnershipThroughDisposal(bool failSend)
            => global::Program.DiagnosticInfrastructure_DisposeDuringRawSend(failSend);

        [Fact]
        public Task AnAdmittedTransportCanDisposeItsOwnChannel()
            => global::Program.DiagnosticInfrastructure_TransportCanDisposeChannel();

        [Theory]
        [InlineData("transient-exception")]
        [InlineData("synthetic-timeout")]
        [InlineData("access-denied")]
        public Task RetryExhaustionAndPermanentDenialRetainFailureEvidence(string failure)
            => global::Program.DiagnosticInfrastructure_RetryFailureEvidence(failure);

        [Fact]
        public void OutputLockReportsRealContentionAndCanBeReacquired()
            => global::Program.DiagnosticInfrastructure_OutputLockContention();

        [Fact]
        public void OutputLockPreservesAnUnrelatedIOException()
            => global::Program.DiagnosticInfrastructure_OutputLockPreservesMissingDirectory();

        [Fact]
        public Task ASucceedingRecoveryStopAddsNoWarning()
            => global::Program.DiagnosticInfrastructure_RecoveryStopSucceeds();

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public Task AFailedOrCanceledRecoveryStopKeepsThePrimaryWarningAndRecordsItsCause(bool canceled)
            => global::Program.DiagnosticInfrastructure_RecoveryStopFailureRecorded(canceled);
    }
}

static partial class Program
{
    private static readonly TimeSpan DiagnosticInfrastructureWaitLimit = TimeSpan.FromSeconds(10);

    internal static async Task DiagnosticInfrastructure_ArtifactWriteFailures(string blockedOutput)
    {
        var outputDirectory = Path.Combine(GetRepoRoot(), "temp", $"diagnostic-artifacts-{Guid.NewGuid():N}");
        var blockedFiles = blockedOutput switch
        {
            "pre-summary" => new[] { "samples.json", "frame-ledger.json" },
            "summary" => new[] { "summary.json" },
            "live" => new[] { "session-live.json" },
            _ => throw new ArgumentOutOfRangeException(nameof(blockedOutput))
        };
        var expectedStage = blockedOutput == "pre-summary" ? "write-samples" : "summary-write";
        try
        {
            Directory.CreateDirectory(outputDirectory);
            foreach (var name in blockedFiles)
            {
                Directory.CreateDirectory(Path.Combine(outputDirectory, name));
            }

            Task<JsonElement> SendAsync(string command, Dictionary<string, object?>? _, int? __, CancellationToken token)
            {
                token.ThrowIfCancellationRequested();
                Assert.Contains(command, new[] { "GetSnapshot", "GetPerformanceTimeline" });
                return Task.FromResult(command == "GetSnapshot"
                    ? DiagnosticCancellationSnapshot(false, false, false, "Live")
                    : JsonSerializer.SerializeToElement(new { Success = true, Data = Array.Empty<object>() }));
            }

            var assembly = LoadDiagnosticSessionRunnerAssembly();
            var options = CreateDiagnosticSessionOptions(assembly, "observe", 0, 100, outputDirectory);
            var result = await RunTokenAwareDiagnosticSessionAsync(assembly, options, SendAsync, CancellationToken.None)
                .WaitAsync(DiagnosticInfrastructureWaitLimit).ConfigureAwait(false);
            var resultJson = JsonSerializer.SerializeToElement(result, result.GetType());
            var warnings = resultJson.GetProperty("Warnings").EnumerateArray().Select(value => value.GetString()!).ToArray();
            var terminalException = resultJson.GetProperty("UnhandledException").GetString();
            var requiredArtifacts = new[] { "samples.json", "frame-ledger.json", "timeline.json", "summary.json" };

            Assert.True(resultJson.GetProperty("CompletedUtc").GetDateTimeOffset() >= resultJson.GetProperty("StartedUtc").GetDateTimeOffset());
            foreach (var name in blockedFiles)
            {
                Assert.True(Directory.Exists(Path.Combine(outputDirectory, name)));
                Assert.False(File.Exists(Path.Combine(outputDirectory, name)));
            }
            foreach (var name in requiredArtifacts.Except(blockedFiles))
            {
                using var artifact = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(outputDirectory, name)).ConfigureAwait(false));
                Assert.NotEqual(JsonValueKind.Undefined, artifact.RootElement.ValueKind);
            }

            if (blockedOutput == "live")
            {
                Assert.True(resultJson.GetProperty("Success").GetBoolean());
                Assert.Equal("completed", resultJson.GetProperty("TerminalState").GetString());
                Assert.Null(terminalException);
                using var summary = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(outputDirectory, "summary.json")).ConfigureAwait(false));
                Assert.True(summary.RootElement.GetProperty("Success").GetBoolean());
                Assert.Null(summary.RootElement.GetProperty("UnhandledException").GetString());
                return;
            }

            Assert.False(resultJson.GetProperty("Success").GetBoolean());
            Assert.Equal("failed", resultJson.GetProperty("TerminalState").GetString());
            Assert.Equal(expectedStage, resultJson.GetProperty("LastStage").GetString());
            Assert.False(string.IsNullOrWhiteSpace(terminalException));
            Assert.Contains(warnings, warning => warning.StartsWith(expectedStage + ":", StringComparison.Ordinal));
            if (blockedOutput == "pre-summary")
            {
                Assert.Contains(warnings, warning => warning.StartsWith("write-frame-ledger:", StringComparison.Ordinal));
                using var summary = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(outputDirectory, "summary.json")).ConfigureAwait(false));
                Assert.Equal("failed", summary.RootElement.GetProperty("TerminalState").GetString());
                Assert.Equal(expectedStage, summary.RootElement.GetProperty("LastStage").GetString());
                Assert.Equal(terminalException, summary.RootElement.GetProperty("UnhandledException").GetString());
            }

            using var live = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(outputDirectory, "session-live.json")).ConfigureAwait(false));
            Assert.Equal("failed", live.RootElement.GetProperty("TerminalState").GetString());
            Assert.Equal(expectedStage, live.RootElement.GetProperty("LastStage").GetString());
            Assert.Equal(terminalException, live.RootElement.GetProperty("UnhandledException").GetString());
            Assert.Equal(0, live.RootElement.GetProperty("CommandFailureCount").GetInt32());
            Assert.NotEqual(JsonValueKind.Null, live.RootElement.GetProperty("CompletedUtc").ValueKind);
        }
        finally
        {
            if (Directory.Exists(outputDirectory)) Directory.Delete(outputDirectory, recursive: true);
        }
    }

    internal static async Task DiagnosticInfrastructure_CanceledWaiterPreservesSerialization()
    {
        var firstEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseFirst = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var entered = new ConcurrentQueue<string>();
        var activeSenders = 0;
        var sentinel = new InvalidOperationException("first admitted send failed");
        using var cancellation = new CancellationTokenSource();
        using var channel = new DiagnosticInfrastructureChannel(async (command, _, _, _) =>
        {
            var active = Interlocked.Increment(ref activeSenders);
            try
            {
                Assert.Equal(1, active);
                entered.Enqueue(command);
                if (command == "first")
                {
                    firstEntered.TrySetResult();
                    await releaseFirst.Task.ConfigureAwait(false);
                    throw sentinel;
                }
                Assert.Equal("third", command);
                return JsonSerializer.SerializeToElement(new { Success = true, Marker = command });
            }
            finally
            {
                Interlocked.Decrement(ref activeSenders);
            }
        });
        Task<JsonElement>? first = null;
        Task<JsonElement>? canceled = null;
        Task<JsonElement>? third = null;
        try
        {
            first = channel.SendAsync("first");
            await firstEntered.Task.WaitAsync(DiagnosticInfrastructureWaitLimit).ConfigureAwait(false);
            canceled = channel.SendAsync("canceled", cancellation.Token);
            Assert.False(canceled.IsCompleted);
            cancellation.Cancel();
            var canceledError = await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => canceled.WaitAsync(DiagnosticInfrastructureWaitLimit)).ConfigureAwait(false);
            Assert.Equal(cancellation.Token, canceledError.CancellationToken);
            Assert.True(canceled.IsCanceled);

            third = channel.SendAsync("third");
            Assert.False(third.IsCompleted);
            Assert.Equal(new[] { "first" }, entered.ToArray());
            releaseFirst.TrySetResult();
            var error = await Assert.ThrowsAsync<InvalidOperationException>(
                () => first.WaitAsync(DiagnosticInfrastructureWaitLimit)).ConfigureAwait(false);
            Assert.Same(sentinel, error);
            var response = await third.WaitAsync(DiagnosticInfrastructureWaitLimit).ConfigureAwait(false);
            Assert.Equal("third", response.GetProperty("Marker").GetString());
            Assert.Equal(new[] { "first", "third" }, entered.ToArray());
            Assert.Equal(0, activeSenders);

            channel.Dispose();
            await Assert.ThrowsAsync<ObjectDisposedException>(() => channel.SendAsync("after-dispose").WaitAsync(DiagnosticInfrastructureWaitLimit)).ConfigureAwait(false);
            Assert.Equal(new[] { "first", "third" }, entered.ToArray());
        }
        finally
        {
            releaseFirst.TrySetResult();
            cancellation.Cancel();
            channel.Dispose();
            await ObserveDiagnosticInfrastructureTasksAsync(first, canceled, third).ConfigureAwait(false);
        }
    }

    internal static async Task DiagnosticInfrastructure_DisposeDuringSerializedSend(bool failSend)
    {
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var completion = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);
        var sentinel = new InvalidOperationException("admitted response failed");
        var expected = JsonSerializer.SerializeToElement(new { Success = true, Marker = "admitted response" });
        var calls = 0;
        using var channel = new DiagnosticInfrastructureChannel((_, _, _, token) =>
        {
            Assert.False(token.IsCancellationRequested);
            Interlocked.Increment(ref calls);
            entered.TrySetResult();
            return completion.Task;
        });
        Task<JsonElement>? admitted = null;
        Task<JsonElement>? queued = null;
        try
        {
            admitted = channel.SendAsync("admitted");
            await entered.Task.WaitAsync(DiagnosticInfrastructureWaitLimit).ConfigureAwait(false);
            var gateHandle = channel.GetGateHandle();
            queued = channel.SendAsync("queued");
            Assert.False(queued.IsCompleted);

            channel.Dispose();
            channel.Dispose();
            await Assert.ThrowsAsync<ObjectDisposedException>(
                () => queued.WaitAsync(DiagnosticInfrastructureWaitLimit)).ConfigureAwait(false);
            Assert.False(admitted.IsCompleted);
            Assert.False(gateHandle.IsClosed);
            await Assert.ThrowsAsync<ObjectDisposedException>(() => channel.SendAsync("late").WaitAsync(DiagnosticInfrastructureWaitLimit)).ConfigureAwait(false);
            await Assert.ThrowsAsync<ObjectDisposedException>(() => channel.SendRawAsync("late-raw").WaitAsync(DiagnosticInfrastructureWaitLimit)).ConfigureAwait(false);
            Assert.Equal(1, calls);

            if (failSend)
            {
                completion.TrySetException(sentinel);
                var error = await Assert.ThrowsAsync<InvalidOperationException>(
                    () => admitted.WaitAsync(DiagnosticInfrastructureWaitLimit)).ConfigureAwait(false);
                Assert.Same(sentinel, error);
            }
            else
            {
                completion.TrySetResult(expected);
                var response = await admitted.WaitAsync(DiagnosticInfrastructureWaitLimit).ConfigureAwait(false);
                Assert.Equal(expected.GetRawText(), response.GetRawText());
            }
            Assert.True(gateHandle.IsClosed);
            Assert.Empty(channel.Warnings);
        }
        finally
        {
            completion.TrySetResult(expected);
            channel.Dispose();
            await ObserveDiagnosticInfrastructureTasksAsync(admitted, queued).ConfigureAwait(false);
        }
    }

    internal static async Task DiagnosticInfrastructure_DisposeDuringRawSend(bool failSend)
    {
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var completion = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);
        var sentinel = new InvalidOperationException("raw response failed");
        var expected = JsonSerializer.SerializeToElement(new { Success = true, Marker = "raw response" });
        var calls = new ConcurrentQueue<string>();
        using var channel = new DiagnosticInfrastructureChannel((command, _, _, token) =>
        {
            Assert.False(token.IsCancellationRequested);
            calls.Enqueue(command);
            if (command == "raw")
            {
                entered.TrySetResult();
                return completion.Task;
            }
            Assert.Equal("ordinary", command);
            return Task.FromResult(JsonSerializer.SerializeToElement(new { Success = true }));
        });
        Task<JsonElement>? raw = null;
        try
        {
            raw = channel.SendRawAsync("raw");
            await entered.Task.WaitAsync(DiagnosticInfrastructureWaitLimit).ConfigureAwait(false);
            var gateHandle = channel.GetGateHandle();
            var ordinary = await channel.SendAsync("ordinary").WaitAsync(DiagnosticInfrastructureWaitLimit).ConfigureAwait(false);
            Assert.True(ordinary.GetProperty("Success").GetBoolean());
            Assert.False(raw.IsCompleted);

            channel.Dispose();
            Assert.False(gateHandle.IsClosed);
            await Assert.ThrowsAsync<ObjectDisposedException>(() => channel.SendAsync("late").WaitAsync(DiagnosticInfrastructureWaitLimit)).ConfigureAwait(false);
            await Assert.ThrowsAsync<ObjectDisposedException>(() => channel.SendRawAsync("late-raw").WaitAsync(DiagnosticInfrastructureWaitLimit)).ConfigureAwait(false);
            if (failSend)
            {
                completion.TrySetException(sentinel);
                var error = await Assert.ThrowsAsync<InvalidOperationException>(
                    () => raw.WaitAsync(DiagnosticInfrastructureWaitLimit)).ConfigureAwait(false);
                Assert.Same(sentinel, error);
            }
            else
            {
                completion.TrySetResult(expected);
                var response = await raw.WaitAsync(DiagnosticInfrastructureWaitLimit).ConfigureAwait(false);
                Assert.Equal(expected.GetRawText(), response.GetRawText());
            }
            Assert.True(gateHandle.IsClosed);
            Assert.Equal(new[] { "raw", "ordinary" }, calls.ToArray());
            Assert.Empty(channel.Warnings);
        }
        finally
        {
            completion.TrySetResult(expected);
            channel.Dispose();
            await ObserveDiagnosticInfrastructureTasksAsync(raw).ConfigureAwait(false);
        }
    }

    internal static async Task DiagnosticInfrastructure_TransportCanDisposeChannel()
    {
        DiagnosticInfrastructureChannel? channel = null;
        Task<JsonElement>? send = null;
        try
        {
            channel = new DiagnosticInfrastructureChannel((_, _, _, _) =>
            {
                channel!.Dispose();
                return Task.FromResult(JsonSerializer.SerializeToElement(new { Success = true, Marker = "disposed inside transport" }));
            });
            // Bound the test even if a future Dispose waits synchronously for itself.
            send = Task.Run(() => channel.SendAsync("self-dispose"));
            var response = await send.WaitAsync(DiagnosticInfrastructureWaitLimit).ConfigureAwait(false);
            Assert.Equal("disposed inside transport", response.GetProperty("Marker").GetString());
            await Assert.ThrowsAsync<ObjectDisposedException>(() => channel.SendRawAsync("late-raw").WaitAsync(DiagnosticInfrastructureWaitLimit)).ConfigureAwait(false);
        }
        finally
        {
            channel?.Dispose();
            await ObserveDiagnosticInfrastructureTasksAsync(send).ConfigureAwait(false);
        }
    }

    internal static async Task DiagnosticInfrastructure_RetryFailureEvidence(string failure)
    {
        var assembly = LoadDiagnosticSessionRunnerAssembly();
        var retryType = assembly.GetType("Sussudio.Tools.DiagnosticSessionPipeRetryPolicy", throwOnError: true)!;
        var cause = "retained cause: " + failure;
        var attempts = 0;
        Exception? connectException = null;
        if (failure != "synthetic-timeout")
        {
            var contractsReference = assembly.GetReferencedAssemblies().Single(reference => reference.Name == "Sussudio.Automation.Contracts");
            var contracts = AssemblyLoadContext.GetLoadContext(assembly)!.LoadFromAssemblyName(contractsReference);
            var exceptionType = contracts.GetType("Sussudio.Tools.AutomationPipeConnectException", throwOnError: true)!;
            connectException = (Exception)Activator.CreateInstance(exceptionType, cause,
                failure == "access-denied" ? "pipe-access-denied" : "pipe-connect-failed", new IOException(cause))!;
        }
        Func<string, Dictionary<string, object?>?, int?, CancellationToken, Task<JsonElement>> sender = (_, _, _, _) =>
        {
            Interlocked.Increment(ref attempts);
            return connectException != null
                ? Task.FromException<JsonElement>(connectException)
                : Task.FromResult(JsonSerializer.SerializeToElement(new
                {
                    Success = false,
                    ErrorCode = "pipe-connect-timeout",
                    Message = cause
                }));
        };
        var method = retryType.GetMethods(BindingFlags.Static | BindingFlags.NonPublic).Single(candidate =>
            candidate.Name == "SendCommandWithConnectRetryAsync" && candidate.GetParameters()[0].ParameterType == sender.GetType());
        var task = (Task<JsonElement?>)method.Invoke(null, new object?[]
        {
            sender, "GetSnapshot", null, null, TimeSpan.FromMilliseconds(50), CancellationToken.None
        })!;
        var response = await task.WaitAsync(DiagnosticInfrastructureWaitLimit).ConfigureAwait(false);
        Assert.True(response.HasValue);
        var failureResponse = response.GetValueOrDefault();
        Assert.False(failureResponse.GetProperty("Success").GetBoolean());
        Assert.Equal("error", failureResponse.GetProperty("Status").GetString());
        Assert.Equal("failed", failureResponse.GetProperty("CommandLifecycle").GetString());
        Assert.Contains(cause, failureResponse.GetProperty("Message").GetString());
        Assert.False(failureResponse.TryGetProperty("ErrorCode", out _));
        if (failure == "access-denied") Assert.Equal(1, attempts);
        else Assert.True(attempts >= 1);
    }

    internal static void DiagnosticInfrastructure_OutputLockContention()
    {
        var directory = Path.Combine(GetRepoRoot(), "temp", $"diagnostic-lock-{Guid.NewGuid():N}");
        try
        {
            Directory.CreateDirectory(directory);
            var acquire = GetDiagnosticInfrastructureOutputLock();
            using (var first = acquire(directory))
            {
                var error = Assert.Throws<InvalidOperationException>(() => { using var competing = acquire(directory); });
                var ioError = Assert.IsAssignableFrom<IOException>(error.InnerException);
                Assert.Contains(ioError.HResult, new[] { unchecked((int)0x80070020), unchecked((int)0x80070021) });
                Assert.Contains("Another diagnostic session is already running", error.Message);
            }
            using var next = acquire(directory);
            Assert.True(next.CanWrite);
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }

    internal static void DiagnosticInfrastructure_OutputLockPreservesMissingDirectory()
    {
        var directory = Path.Combine(GetRepoRoot(), "temp", $"diagnostic-missing-lock-{Guid.NewGuid():N}", "missing");
        Assert.False(Directory.Exists(directory));
        var error = Assert.Throws<DirectoryNotFoundException>(() => GetDiagnosticInfrastructureOutputLock()(directory));
        Assert.Null(error.InnerException);
        Assert.DoesNotContain("Another diagnostic session is already running", error.Message);
        Assert.False(Directory.Exists(directory));
    }

    // Recovery cleanup is best-effort, but a stop that never reached the app leaves the
    // session recording; the transport failure has to reach the diagnostic report.
    internal static async Task DiagnosticInfrastructure_RecoveryStopSucceeds()
    {
        var warnings = new List<string> { "primary readiness warning" };
        var commands = new List<string>();

        await InvokeDiagnosticRecoveryStopAsync(
            (command, _, _) =>
            {
                commands.Add(command);
                return Task.FromResult(JsonDocument.Parse("{\"Success\":true}").RootElement.Clone());
            },
            warnings).ConfigureAwait(false);

        Assert.Equal(new[] { "SetRecordingEnabled" }, commands);
        Assert.Equal(new[] { "primary readiness warning" }, warnings);
    }

    internal static async Task DiagnosticInfrastructure_RecoveryStopFailureRecorded(bool canceled)
    {
        var warnings = new List<string> { "primary readiness warning" };
        Exception failure = canceled
            ? new OperationCanceledException("diagnostic transport canceled")
            : new IOException("diagnostic pipe closed");

        await InvokeDiagnosticRecoveryStopAsync(
            (_, _, _) => Task.FromException<JsonElement>(failure),
            warnings).ConfigureAwait(false);

        Assert.Equal(2, warnings.Count);
        Assert.Equal("primary readiness warning", warnings[0]);
        Assert.Contains("recording-assisted cleanup stop failed", warnings[1]);
        Assert.Contains(failure.GetType().Name, warnings[1]);
        Assert.Contains(failure.Message, warnings[1]);
    }

    private static Task InvokeDiagnosticRecoveryStopAsync(
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync,
        List<string> warnings)
    {
        var method = LoadDiagnosticSessionRunnerAssembly()
            .GetType("Sussudio.Tools.DiagnosticSessionFlashbackSegmentPlaybackScenarios", throwOnError: true)!
            .GetMethod("TryStopRecordingAsync", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Diagnostic recovery stop helper was not found.");
        return (Task)method.Invoke(null, new object[] { sendCommandAsync, warnings })!;
    }
    private static Func<string, FileStream> GetDiagnosticInfrastructureOutputLock()
        => LoadDiagnosticSessionRunnerAssembly().GetType("Sussudio.Tools.DiagnosticSessionRunner", throwOnError: true)!
            .GetMethod("AcquireOutputLock", BindingFlags.Static | BindingFlags.NonPublic)!
            .CreateDelegate<Func<string, FileStream>>();

    private static async Task ObserveDiagnosticInfrastructureTasksAsync(params Task?[] tasks)
    {
        var observations = tasks.Where(task => task != null).Select(async task =>
        {
            try { await task!.ConfigureAwait(false); }
            catch { /* Expected outcomes are asserted before fixture cleanup. */ }
        });
        await Task.WhenAll(observations).WaitAsync(DiagnosticInfrastructureWaitLimit).ConfigureAwait(false);
    }

    private sealed class DiagnosticInfrastructureChannel : IDisposable
    {
        private readonly IDisposable _channel;
        private readonly SemaphoreSlim _gate;
        private readonly Func<string, Dictionary<string, object?>?, int?, bool, CancellationToken, Task<JsonElement>> _send;
        private readonly Func<string, Dictionary<string, object?>?, int?, CancellationToken, Task<JsonElement>> _sendRaw;

        internal DiagnosticInfrastructureChannel(Func<string, Dictionary<string, object?>?, int?, CancellationToken, Task<JsonElement>> sender)
        {
            var channelType = LoadDiagnosticSessionRunnerAssembly().GetType("Sussudio.Tools.DiagnosticSessionCommandChannel", throwOnError: true)!;
            _channel = (IDisposable)Activator.CreateInstance(channelType, BindingFlags.Instance | BindingFlags.NonPublic, null,
                new object[] { sender, CancellationToken.None, Warnings }, null)!;
            _gate = (SemaphoreSlim)channelType.GetField("_sendGate", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(_channel)!;
            var methods = channelType.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic);
            _send = methods.Single(method => method.Name == "SendWithTokenAsync" && method.GetParameters()[0].ParameterType == typeof(string))
                .CreateDelegate<Func<string, Dictionary<string, object?>?, int?, bool, CancellationToken, Task<JsonElement>>>(_channel);
            _sendRaw = methods.Single(method => method.Name == "SendRawWithConnectRetryWithTokenAsync" && method.GetParameters()[0].ParameterType == typeof(string))
                .CreateDelegate<Func<string, Dictionary<string, object?>?, int?, CancellationToken, Task<JsonElement>>>(_channel);
        }

        internal List<string> Warnings { get; } = new();
        internal Task<JsonElement> SendAsync(string command, CancellationToken token = default) => _send(command, null, null, false, token);
        internal Task<JsonElement> SendRawAsync(string command) => _sendRaw(command, null, null, CancellationToken.None);
        internal SafeWaitHandle GetGateHandle() => _gate.AvailableWaitHandle.SafeWaitHandle;
        public void Dispose() => _channel.Dispose();
    }
}

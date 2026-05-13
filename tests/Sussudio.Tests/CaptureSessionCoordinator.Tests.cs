using System.Reflection;
using System.Threading.Tasks;

static partial class Program
{
    // ── CaptureSessionCoordinator: API surface contract ──

    private static Task CaptureSessionCoordinator_HasExpectedPublicMethods()
    {
        var coordinatorType = RequireType("Sussudio.Services.Capture.CaptureSessionCoordinator");

        // Core lifecycle methods
        var expectedMethods = new[]
        {
            "InitializeAsync",
            "StartVideoPreviewAsync",
            "StopVideoPreviewAsync",
            "StopVideoPreviewWithTeardownAsync",
            "StartRecordingAsync",
            "StopRecordingAsync",
            "CleanupAsync",
            "StartAudioPreviewAsync",
            "StopAudioPreviewAsync",
            "StopAudioPreviewWithTeardownAsync",
            "UpdateAudioMonitoringAsync",
            "UpdateAudioInputAsync",
            "UpdateMicrophoneMonitorAsync",
            "RestartFlashbackAsync",
            "UpdateRecordingFormatAsync",
            "CycleFlashbackEncoderSettingsAsync",
            "SetFlashbackEnabledAsync",
            "UpdateFlashbackSettingsAsync"
        };

        foreach (var methodName in expectedMethods)
        {
            var method = Array.Find(
                coordinatorType.GetMethods(BindingFlags.Public | BindingFlags.Instance),
                method => method.Name == methodName);
            AssertNotNull(method, $"CaptureSessionCoordinator.{methodName}");
        }

        // Snapshot property
        var snapshotProp = coordinatorType.GetProperty("Snapshot", BindingFlags.Public | BindingFlags.Instance);
        AssertNotNull(snapshotProp, "CaptureSessionCoordinator.Snapshot");

        // Implements IDisposable and IAsyncDisposable
        AssertEqual(true, typeof(IDisposable).IsAssignableFrom(coordinatorType),
            "Implements IDisposable");
        AssertEqual(true, typeof(IAsyncDisposable).IsAssignableFrom(coordinatorType),
            "Implements IAsyncDisposable");

        return Task.CompletedTask;
    }

    // ── CaptureSessionCoordinator: CaptureCommand shape ──

    private static Task CaptureSessionCoordinator_CaptureCommandKind_HasExpectedValues()
    {
        var commandKindType = RequireType("Sussudio.Services.Capture.CaptureCommandKind");

        // Core command kinds should exist
        var expectedValues = new[]
        {
            "Initialize", "StartVideoPreview", "StopVideoPreview",
            "StartRecording", "StopRecording", "Cleanup",
            "StartAudioPreview", "StopAudioPreview",
            "UpdateAudioMonitoring", "UpdateAudioInput",
            "UpdateMicrophoneMonitor",
            "SetFlashbackEnabled", "UpdateFlashbackSettings",
            "RestartFlashback", "UpdateFlashbackRecordingFormat",
            "CycleFlashbackEncoderSettings"
        };

        foreach (var value in expectedValues)
        {
            var parsed = Enum.Parse(commandKindType, value);
            AssertNotNull(parsed, $"CaptureCommandKind.{value}");
        }

        return Task.CompletedTask;
    }

    // ── CaptureSessionCoordinator: CaptureSessionSnapshot ──

    private static Task CaptureSessionCoordinator_CaptureSessionSnapshot_HasFullContract()
    {
        var snapshotType = RequireType("Sussudio.Services.Capture.CaptureSessionSnapshot");

        var expectedProps = new[]
        {
            "LastTransitionUtc", "LastCommand", "LastCorrelationId",
            "LastError", "CommandsEnqueued", "CommandsCompleted",
            "CommandsFailed", "CommandsCanceled", "CommandsCoalesced", "PendingCommands",
            "MaxPendingCommands", "OldestPendingCommandAgeMs",
            "LastCommandQueueLatencyMs", "MaxCommandQueueLatencyMs", "LastOutcome", "SessionState",
            "IsRecording", "IsInitialized", "IsVideoPreviewActive", "IsAudioPreviewActive"
        };

        foreach (var prop in expectedProps)
        {
            var propInfo = snapshotType.GetProperty(prop, BindingFlags.Public | BindingFlags.Instance);
            AssertNotNull(propInfo, $"CaptureSessionSnapshot.{prop}");
        }

        // Default state should be clean
        var snapshot = Activator.CreateInstance(snapshotType)!;
        AssertEqual(false, GetBoolProperty(snapshot, "IsRecording"), "Default IsRecording");
        AssertEqual(false, GetBoolProperty(snapshot, "IsInitialized"), "Default IsInitialized");
        AssertEqual(0, Convert.ToInt32(GetPropertyValue(snapshot, "PendingCommands")), "Default PendingCommands");
        AssertEqual(0, Convert.ToInt32(GetPropertyValue(snapshot, "MaxPendingCommands")), "Default MaxPendingCommands");
        AssertEqual(0L, Convert.ToInt64(GetPropertyValue(snapshot, "OldestPendingCommandAgeMs")), "Default OldestPendingCommandAgeMs");
        AssertEqual(0L, Convert.ToInt64(GetPropertyValue(snapshot, "MaxCommandQueueLatencyMs")), "Default MaxCommandQueueLatencyMs");
        AssertEqual(0L, Convert.ToInt64(GetPropertyValue(snapshot, "CommandsCoalesced")), "Default CommandsCoalesced");
        AssertEqual("None", GetStringProperty(snapshot, "LastOutcome"), "Default LastOutcome");

        return Task.CompletedTask;
    }

    private static Task CaptureSessionTransitionPolicy_DefinesCoreLifecycleRules()
    {
        var policyType = RequireType("Sussudio.Models.CaptureSessionTransitionPolicy");
        var stateType = RequireType("Sussudio.Models.CaptureSessionState");
        var canEnter = policyType.GetMethod(
            "CanEnterTransition",
            BindingFlags.Public | BindingFlags.Static,
            binder: null,
            types: new[] { stateType, stateType },
            modifiers: null)
            ?? throw new InvalidOperationException("CaptureSessionTransitionPolicy.CanEnterTransition not found.");

        AssertCanEnterTransition(canEnter, stateType, "Uninitialized", "Initializing", expected: true);
        AssertCanEnterTransition(canEnter, stateType, "Ready", "Previewing", expected: true);
        AssertCanEnterTransition(canEnter, stateType, "Previewing", "Recording", expected: true);
        AssertCanEnterTransition(canEnter, stateType, "Recording", "Ready", expected: true);
        AssertCanEnterTransition(canEnter, stateType, "Faulted", "CleaningUp", expected: true);
        AssertCanEnterTransition(canEnter, stateType, "Uninitialized", "Uninitialized", expected: true);
        AssertCanEnterTransition(canEnter, stateType, "Disposed", "Ready", expected: false);
        AssertCanEnterTransition(canEnter, stateType, "Ready", "Disposed", expected: false);
        AssertCanEnterTransition(canEnter, stateType, "Uninitialized", "Recording", expected: false);

        return Task.CompletedTask;
    }

    private static Task CaptureSessionTransitionPolicy_ResolvesSteadyStateFromRuntimeFlags()
    {
        var policyType = RequireType("Sussudio.Models.CaptureSessionTransitionPolicy");
        var method = policyType.GetMethod(
            "ResolveSteadyState",
            BindingFlags.Public | BindingFlags.Static,
            binder: null,
            types: new[] { typeof(bool), typeof(bool), typeof(bool), typeof(bool), typeof(bool) },
            modifiers: null)
            ?? throw new InvalidOperationException("CaptureSessionTransitionPolicy.ResolveSteadyState not found.");

        AssertEqual(
            ParseEnum("Sussudio.Models.CaptureSessionState", "Disposed"),
            ResolveState(method, isDisposed: true, isRecording: true, isVideoPreviewActive: true, isAudioPreviewActive: true, isInitialized: true),
            "Disposed steady state precedence");
        AssertEqual(
            ParseEnum("Sussudio.Models.CaptureSessionState", "Recording"),
            ResolveState(method, isDisposed: false, isRecording: true, isVideoPreviewActive: true, isAudioPreviewActive: true, isInitialized: true),
            "Recording steady state precedence");
        AssertEqual(
            ParseEnum("Sussudio.Models.CaptureSessionState", "Previewing"),
            ResolveState(method, isDisposed: false, isRecording: false, isVideoPreviewActive: false, isAudioPreviewActive: true, isInitialized: true),
            "Audio preview steady state");
        AssertEqual(
            ParseEnum("Sussudio.Models.CaptureSessionState", "Ready"),
            ResolveState(method, isDisposed: false, isRecording: false, isVideoPreviewActive: false, isAudioPreviewActive: false, isInitialized: true),
            "Initialized steady state");
        AssertEqual(
            ParseEnum("Sussudio.Models.CaptureSessionState", "Uninitialized"),
            ResolveState(method, isDisposed: false, isRecording: false, isVideoPreviewActive: false, isAudioPreviewActive: false, isInitialized: false),
            "Uninitialized steady state");

        return Task.CompletedTask;
    }

    private static Task CaptureService_RunTransition_UsesTransitionPolicy()
    {
        var serviceText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.Coordination.cs");

        AssertContains(
            serviceText,
            "CaptureSessionTransitionPolicy.ThrowIfDisallowed(_sessionState, transitionState);");
        AssertContains(
            serviceText,
            "CaptureSessionTransitionPolicy.ResolveSteadyState(");

        return Task.CompletedTask;
    }

    private static async Task CaptureSessionCoordinator_CanceledQueuedCommandUpdatesAccounting()
    {
        var harness = CreateCaptureSessionCoordinatorHarness();
        try
        {
            var firstStarted = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
            var releaseFirst = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
            var operationsExecuted = 0;

            var firstTask = EnqueueCoordinatorOperation(
                harness,
                "StartVideoPreview",
                async ct =>
                {
                    Interlocked.Increment(ref operationsExecuted);
                    firstStarted.TrySetResult(null);
                    await releaseFirst.Task.WaitAsync(ct).ConfigureAwait(false);
                });

            await firstStarted.Task.WaitAsync(TimeSpan.FromSeconds(2)).ConfigureAwait(false);
            using var cts = new CancellationTokenSource();
            var canceledTask = EnqueueCoordinatorOperation(
                harness,
                "StartRecording",
                _ =>
                {
                    Interlocked.Increment(ref operationsExecuted);
                    return Task.CompletedTask;
                },
                cts.Token);

            cts.Cancel();
            await AssertTaskCanceledAsync(canceledTask).ConfigureAwait(false);

            var queuedSnapshot = GetCoordinatorSnapshot(harness.Coordinator);
            AssertEqual(2L, GetLongProperty(queuedSnapshot, "CommandsEnqueued"), "Queued cancellation enqueued count");
            AssertEqual(true, GetIntProperty(queuedSnapshot, "PendingCommands") >= 1, "Queued cancellation pending count before drain");

            releaseFirst.TrySetResult(null);
            await firstTask.WaitAsync(TimeSpan.FromSeconds(2)).ConfigureAwait(false);
            await WaitForConditionAsync(
                () => GetIntProperty(GetCoordinatorSnapshot(harness.Coordinator), "PendingCommands") == 0,
                "coordinator canceled queued command accounting").ConfigureAwait(false);

            var snapshot = GetCoordinatorSnapshot(harness.Coordinator);
            AssertEqual(1, operationsExecuted, "Canceled queued command did not execute");
            AssertEqual(2L, GetLongProperty(snapshot, "CommandsEnqueued"), "CommandsEnqueued after queued cancellation");
            AssertEqual(1L, GetLongProperty(snapshot, "CommandsCompleted"), "CommandsCompleted after queued cancellation");
            AssertEqual(1L, GetLongProperty(snapshot, "CommandsCanceled"), "CommandsCanceled after queued cancellation");
            AssertEqual(0L, GetLongProperty(snapshot, "CommandsFailed"), "CommandsFailed after queued cancellation");
            AssertEqual(0, GetIntProperty(snapshot, "PendingCommands"), "PendingCommands after queued cancellation");
            AssertEqual(true, GetIntProperty(snapshot, "MaxPendingCommands") >= 2, "MaxPendingCommands captures queued cancellation");
            AssertEqual("StartRecording", GetStringProperty(snapshot, "LastCommand"), "LastCommand after queued cancellation");
            AssertEqual("Canceled", GetStringProperty(snapshot, "LastOutcome"), "LastOutcome after queued cancellation");
            AssertContains(GetStringProperty(snapshot, "LastCorrelationId"), "StartRecording-");
        }
        finally
        {
            await DisposeCaptureSessionCoordinatorHarnessAsync(harness).ConfigureAwait(false);
        }
    }

    private static async Task CaptureSessionCoordinator_CoalescesQueuedLatestOnlyAndAccountsSkip()
    {
        var harness = CreateCaptureSessionCoordinatorHarness();
        try
        {
            var blockerStarted = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
            var releaseBlocker = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
            var staleExecuted = 0;
            var latestExecuted = 0;

            var blockerTask = EnqueueCoordinatorOperation(
                harness,
                "Initialize",
                async ct =>
                {
                    blockerStarted.TrySetResult(null);
                    await releaseBlocker.Task.WaitAsync(ct).ConfigureAwait(false);
                });
            await blockerStarted.Task.WaitAsync(TimeSpan.FromSeconds(2)).ConfigureAwait(false);

            var staleTask = EnqueueCoordinatorOperation(
                harness,
                "CycleFlashbackEncoderSettings",
                _ =>
                {
                    Interlocked.Increment(ref staleExecuted);
                    return Task.CompletedTask;
                },
                coalesceLatest: true);
            var latestTask = EnqueueCoordinatorOperation(
                harness,
                "CycleFlashbackEncoderSettings",
                _ =>
                {
                    Interlocked.Increment(ref latestExecuted);
                    return Task.CompletedTask;
                },
                coalesceLatest: true);

            releaseBlocker.TrySetResult(null);
            await blockerTask.WaitAsync(TimeSpan.FromSeconds(2)).ConfigureAwait(false);
            await staleTask.WaitAsync(TimeSpan.FromSeconds(2)).ConfigureAwait(false);
            await latestTask.WaitAsync(TimeSpan.FromSeconds(2)).ConfigureAwait(false);
            await WaitForConditionAsync(
                () => GetIntProperty(GetCoordinatorSnapshot(harness.Coordinator), "PendingCommands") == 0,
                "coordinator coalesced queue drain").ConfigureAwait(false);

            var snapshot = GetCoordinatorSnapshot(harness.Coordinator);
            AssertEqual(0, staleExecuted, "Stale coalesced operation skipped");
            AssertEqual(1, latestExecuted, "Latest coalesced operation executed");
            AssertEqual(3L, GetLongProperty(snapshot, "CommandsEnqueued"), "CommandsEnqueued after coalescing");
            AssertEqual(3L, GetLongProperty(snapshot, "CommandsCompleted"), "CommandsCompleted after coalescing");
            AssertEqual(1L, GetLongProperty(snapshot, "CommandsCoalesced"), "CommandsCoalesced after coalescing");
            AssertEqual(0L, GetLongProperty(snapshot, "CommandsFailed"), "CommandsFailed after coalescing");
            AssertEqual(0L, GetLongProperty(snapshot, "CommandsCanceled"), "CommandsCanceled after coalescing");
            AssertEqual(0, GetIntProperty(snapshot, "PendingCommands"), "PendingCommands after coalescing");
            AssertEqual(true, GetIntProperty(snapshot, "MaxPendingCommands") >= 3, "MaxPendingCommands captures coalesced backlog");
            AssertEqual("CycleFlashbackEncoderSettings", GetStringProperty(snapshot, "LastCommand"), "LastCommand after coalescing");
            AssertEqual("Completed", GetStringProperty(snapshot, "LastOutcome"), "LastOutcome after coalescing");
        }
        finally
        {
            await DisposeCaptureSessionCoordinatorHarnessAsync(harness).ConfigureAwait(false);
        }
    }

    private static async Task CaptureSessionCoordinator_DisposeDrainsQueuedCommandBeforeCancellation()
    {
        var harness = CreateCaptureSessionCoordinatorHarness();
        try
        {
            var executed = 0;
            var commandTask = EnqueueCoordinatorOperation(
                harness,
                "Cleanup",
                async ct =>
                {
                    await Task.Delay(50, ct).ConfigureAwait(false);
                    AssertEqual(false, ct.IsCancellationRequested, "Dispose drain should not pre-cancel queued cleanup");
                    Interlocked.Increment(ref executed);
                });

            await InvokeDisposeAsync(harness.Coordinator).ConfigureAwait(false);
            await commandTask.WaitAsync(TimeSpan.FromSeconds(2)).ConfigureAwait(false);

            var snapshot = GetCoordinatorSnapshot(harness.Coordinator);
            AssertEqual(1, executed, "Dispose drain executed queued command");
            AssertEqual(1L, GetLongProperty(snapshot, "CommandsEnqueued"), "CommandsEnqueued after dispose drain");
            AssertEqual(1L, GetLongProperty(snapshot, "CommandsCompleted"), "CommandsCompleted after dispose drain");
            AssertEqual(0L, GetLongProperty(snapshot, "CommandsCanceled"), "CommandsCanceled after dispose drain");
            AssertEqual(0L, GetLongProperty(snapshot, "CommandsFailed"), "CommandsFailed after dispose drain");
            AssertEqual(0, GetIntProperty(snapshot, "PendingCommands"), "PendingCommands after dispose drain");
            AssertEqual("Cleanup", GetStringProperty(snapshot, "LastCommand"), "LastCommand after dispose drain");
            AssertEqual("Completed", GetStringProperty(snapshot, "LastOutcome"), "LastOutcome after dispose drain");
        }
        finally
        {
            await DisposeCaptureSessionCoordinatorHarnessAsync(harness).ConfigureAwait(false);
        }
    }

    private static Task CaptureSessionCoordinator_CoalescesFlashbackEncoderCycles()
    {
        var coordinatorText = ReadRepoFile("Sussudio/Services/Capture/CaptureSessionCoordinator.cs")
            .Replace("\r\n", "\n");
        var cycleMethod = ExtractTextBetween(
            coordinatorText,
            "public Task CycleFlashbackEncoderSettingsAsync",
            "public Task SetFlashbackEnabledAsync");
        var queueProcessor = ExtractTextBetween(
            coordinatorText,
            "private async Task ProcessQueueAsync",
            "private void UpdateSnapshot");

        AssertContains(coordinatorText, "_latestFlashbackEncoderCycleGeneration");
        AssertContains(coordinatorText, "_commandsCoalesced");
        AssertContains(cycleMethod, "coalesceLatest: true");
        AssertContains(queueProcessor, "Volatile.Read(ref _latestFlashbackEncoderCycleGeneration)");
        AssertContains(queueProcessor, "CaptureCommandOutcome.Coalesced");
        AssertContains(queueProcessor, "CAP-COORD-SKIP");
        AssertContains(coordinatorText, "CAP-COORD-ENQUEUE-FAIL");

        return Task.CompletedTask;
    }

    private static Task CaptureSessionCoordinator_DisposalAccounting_ClassifiesCanceledQueuedCommands()
    {
        var coordinatorText = ReadRepoFile("Sussudio/Services/Capture/CaptureSessionCoordinator.cs")
            .Replace("\r\n", "\n");
        var failPending = ExtractTextBetween(
            coordinatorText,
            "private void FailPendingCommands(Exception ex)",
            "    private void TrackPendingCommandEnqueued");

        AssertContains(failPending, "if (pending.Completion.Task.IsCanceled)\n            {\n                Interlocked.Increment(ref _commandsCanceled);");
        AssertContains(failPending, "else if (pending.Completion.TrySetException(ex))\n            {\n                Interlocked.Increment(ref _commandsFailed);");
        AssertContains(failPending, "DecrementPendingCommands(\"fail_pending\");");
        AssertContains(coordinatorText, "DecrementPendingCommands(\"enqueue_failed\");");
        AssertContains(coordinatorText, "DecrementPendingCommands(\"process_complete\");");
        AssertContains(coordinatorText, "private void DecrementPendingCommands(string operation)");
        AssertContains(coordinatorText, "CAPTURE_COORD_PENDING_UNDERFLOW");
        AssertContains(coordinatorText, "throw new ObjectDisposedException(nameof(CaptureSessionCoordinator));");
        AssertDoesNotContain(failPending, "pending.Completion.TrySetException(ex);\n            Interlocked.Increment(ref _commandsFailed);\n            Interlocked.Decrement(ref _pendingCommands);");

        return Task.CompletedTask;
    }

    private static Task CaptureSessionCoordinator_FlashbackMutationsPropagateRequestCancellation()
    {
        var coordinatorText = ReadRepoFile("Sussudio/Services/Capture/CaptureSessionCoordinator.cs")
            .Replace("\r\n", "\n");
        var restartNoSettings = ExtractTextBetween(
            coordinatorText,
            "public Task RestartFlashbackAsync(CancellationToken cancellationToken = default)",
            "public Task RestartFlashbackAsync(CaptureSettings settings");
        var restartWithSettings = ExtractTextBetween(
            coordinatorText,
            "public Task RestartFlashbackAsync(CaptureSettings settings",
            "public Task UpdateRecordingFormatAsync");
        var setFlashbackEnabled = ExtractTextBetween(
            coordinatorText,
            "public Task SetFlashbackEnabledAsync",
            "public Task UpdateFlashbackSettingsAsync");

        AssertContains(restartNoSettings, "propagateCancellationToOperation: true");
        AssertContains(restartWithSettings, "propagateCancellationToOperation: true");
        AssertContains(restartWithSettings, "ct => _captureService.RestartFlashbackAsync(settings, ct)");
        AssertDoesNotContain(restartWithSettings, "_captureService.UpdateEncodingSettings(settings)");
        AssertContains(setFlashbackEnabled, "propagateCancellationToOperation: true");

        return Task.CompletedTask;
    }

    private static Task CaptureSessionCoordinator_CommittedStopsDoNotPropagateRequestCancellation()
    {
        var coordinatorText = ReadRepoFile("Sussudio/Services/Capture/CaptureSessionCoordinator.cs")
            .Replace("\r\n", "\n");
        var stopVideo = ExtractTextBetween(
            coordinatorText,
            "public Task StopVideoPreviewAsync(CancellationToken cancellationToken = default)",
            "public Task StopVideoPreviewWithTeardownAsync");
        var stopVideoTeardown = ExtractTextBetween(
            coordinatorText,
            "public Task StopVideoPreviewWithTeardownAsync",
            "public Task StartRecordingAsync");
        var stopRecording = ExtractTextBetween(
            coordinatorText,
            "public Task StopRecordingAsync",
            "public Task StartAudioPreviewAsync");
        var cycleFlashbackEncoder = ExtractTextBetween(
            coordinatorText,
            "public Task CycleFlashbackEncoderSettingsAsync",
            "public Task SetFlashbackEnabledAsync");

        AssertDoesNotContain(stopVideo, "propagateCancellationToOperation: true");
        AssertDoesNotContain(stopVideoTeardown, "propagateCancellationToOperation: true");
        AssertDoesNotContain(stopRecording, "propagateCancellationToOperation: true");
        AssertDoesNotContain(cycleFlashbackEncoder, "propagateCancellationToOperation: true");
        AssertContains(cycleFlashbackEncoder, "coalesceLatest: true");

        return Task.CompletedTask;
    }

    private static Task CaptureSessionCoordinator_LogsInactiveFlashbackCommandRejections()
    {
        var coordinatorText = ReadRepoFile("Sussudio/Services/Capture/CaptureSessionCoordinator.cs")
            .Replace("\r\n", "\n");

        AssertContains(coordinatorText, "TryGetActiveFlashback(nameof(FlashbackBeginScrub), out var controller)");
        AssertContains(coordinatorText, "TryGetActiveFlashback(nameof(FlashbackUpdateScrub), out var controller)");
        AssertContains(coordinatorText, "TryGetActiveFlashback(nameof(FlashbackEndScrub), out var controller)");
        AssertContains(coordinatorText, "TryGetActiveFlashback(nameof(FlashbackGoLive), out var controller)");
        AssertContains(coordinatorText, "TryGetActiveFlashback(nameof(FlashbackClearInOutPoints), out var controller)");
        AssertContains(coordinatorText, "bool ThreadAlive,\n    int PendingCommands,\n    string LastCommandFailure,\n    long LastCommandFailureUtcUnixMs");
        AssertContains(coordinatorText, "controller.PlaybackThreadAlive,\n                controller.PendingCommands,\n                controller.LastCommandFailure,\n                controller.LastCommandFailureUtcUnixMs");
        AssertContains(coordinatorText, "public static FlashbackPlaybackSnapshot Inactive(");
        AssertContains(coordinatorText, "private long _lastFlashbackCommandRejectionUtcUnixMs;");
        AssertContains(coordinatorText, "private string _lastFlashbackCommandRejection = string.Empty;");
        AssertContains(coordinatorText, "FlashbackPlaybackSnapshot.Inactive(\n                _lastFlashbackCommandRejection,\n                Interlocked.Read(ref _lastFlashbackCommandRejectionUtcUnixMs))");
        AssertContains(coordinatorText, "private bool TryGetActiveFlashback(\n        string command,");
        AssertContains(coordinatorText, "var reason = controller == null\n            ? \"missing_controller\"\n            : controller.IsDisposed\n                ? \"disposed\"\n                : !controller.IsInitialized\n                ? \"not_initialized\"\n                : $\"state_{controller.State}\";");
        AssertContains(coordinatorText, "_lastFlashbackCommandRejection = $\"{reason}:{command}\";");
        AssertContains(coordinatorText, "Interlocked.Exchange(ref _lastFlashbackCommandRejectionUtcUnixMs, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());");
        AssertContains(coordinatorText, "Logger.Log($\"FLASHBACK_COORD_COMMAND_REJECTED command={command} reason={reason}\");");

        return Task.CompletedTask;
    }

    private static void AssertCanEnterTransition(
        MethodInfo canEnter,
        Type stateType,
        string currentState,
        string transitionState,
        bool expected)
    {
        var actual = canEnter.Invoke(
            null,
            new[] { Enum.Parse(stateType, currentState), Enum.Parse(stateType, transitionState) });
        AssertEqual(expected, (bool)actual!, $"{currentState} -> {transitionState}");
    }

    private static object ResolveState(
        MethodInfo resolve,
        bool isDisposed,
        bool isRecording,
        bool isVideoPreviewActive,
        bool isAudioPreviewActive,
        bool isInitialized)
        => resolve.Invoke(
            null,
            new object[]
            {
                isDisposed,
                isRecording,
                isVideoPreviewActive,
                isAudioPreviewActive,
                isInitialized
            })
           ?? throw new InvalidOperationException("ResolveSteadyState returned null.");

    private sealed record CaptureSessionCoordinatorHarness(
        object Coordinator,
        object CaptureService,
        Type CommandKindType,
        MethodInfo EnqueueMethod);

    private static CaptureSessionCoordinatorHarness CreateCaptureSessionCoordinatorHarness()
    {
        var coordinatorType = RequireType("Sussudio.Services.Capture.CaptureSessionCoordinator");
        var captureServiceType = RequireType("Sussudio.Services.Capture.CaptureService");
        var commandKindType = RequireType("Sussudio.Services.Capture.CaptureCommandKind");
        var captureService = Activator.CreateInstance(captureServiceType)
            ?? throw new InvalidOperationException("Failed to create CaptureService.");
        var coordinator = Activator.CreateInstance(coordinatorType, captureService)
            ?? throw new InvalidOperationException("Failed to create CaptureSessionCoordinator.");
        var enqueueMethod = coordinatorType.GetMethod("EnqueueAsync", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("CaptureSessionCoordinator.EnqueueAsync not found.");
        return new CaptureSessionCoordinatorHarness(coordinator, captureService, commandKindType, enqueueMethod);
    }

    private static Task EnqueueCoordinatorOperation(
        CaptureSessionCoordinatorHarness harness,
        string commandKind,
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken = default,
        bool coalesceLatest = false,
        bool propagateCancellationToOperation = false)
    {
        var kind = Enum.Parse(harness.CommandKindType, commandKind);
        return (Task)(harness.EnqueueMethod.Invoke(
                   harness.Coordinator,
                   new object?[]
                   {
                       kind,
                       operation,
                       cancellationToken,
                       coalesceLatest,
                       propagateCancellationToOperation
                   })
               ?? throw new InvalidOperationException("CaptureSessionCoordinator.EnqueueAsync returned null."));
    }

    private static object GetCoordinatorSnapshot(object coordinator)
        => GetPropertyValue(coordinator, "Snapshot")
           ?? throw new InvalidOperationException("CaptureSessionCoordinator.Snapshot returned null.");

    private static async Task DisposeCaptureSessionCoordinatorHarnessAsync(CaptureSessionCoordinatorHarness harness)
    {
        await InvokeDisposeAsync(harness.Coordinator).ConfigureAwait(false);
        await InvokeDisposeAsync(harness.CaptureService).ConfigureAwait(false);
    }

    private static async Task InvokeDisposeAsync(object target)
    {
        var disposeAsync = target.GetType().GetMethod("DisposeAsync", BindingFlags.Instance | BindingFlags.Public)
            ?? throw new InvalidOperationException($"{target.GetType().Name}.DisposeAsync not found.");
        var result = disposeAsync.Invoke(target, Array.Empty<object?>());
        switch (result)
        {
            case ValueTask valueTask:
                await valueTask.ConfigureAwait(false);
                return;
            case Task task:
                await task.ConfigureAwait(false);
                return;
            default:
                throw new InvalidOperationException($"{target.GetType().Name}.DisposeAsync returned unsupported result.");
        }
    }

    private static async Task AssertTaskCanceledAsync(Task task)
    {
        try
        {
            await task.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        throw new InvalidOperationException("Expected task to be canceled.");
    }
}

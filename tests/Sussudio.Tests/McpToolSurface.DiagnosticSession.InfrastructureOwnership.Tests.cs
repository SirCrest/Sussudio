using System.Threading.Tasks;

static partial class Program
{
    private static Task DiagnosticSessionRunner_OwnsCompatibilitySurface()
    {
        var runnerText = ReadRepoFile("tools/Common/DiagnosticSessionRunner.cs")
            .Replace("\r\n", "\n");
        var executionText = ReadDiagnosticSessionRunExecutionRootSource();
        var scenarioText = ReadDiagnosticSessionRunExecutionScenarioSource();

        AssertContains(runnerText, "public static class DiagnosticSessionRunner");
        AssertContains(runnerText, "public static Task<DiagnosticSessionResult> RunAsync(");
        AssertContains(runnerText, "return DiagnosticSessionRunExecution.RunAsync(options, sendCommandAsync, cancellationToken);");
        AssertContains(runnerText, "return DiagnosticSessionResultFormatter.Format(result);");
        AssertDoesNotContain(runnerText, "DiagnosticSessionScenarioSetup.RunAsync(");
        AssertDoesNotContain(runnerText, "SampleLoopAsync(");
        AssertDoesNotContain(runnerText, "DiagnosticSessionCleanupActions.RunAsync(");
        AssertContains(executionText, "internal static partial class DiagnosticSessionRunExecution");
        AssertContains(executionText, "RunScenarioPhaseAsync(");
        AssertContains(executionText, "DiagnosticSessionCleanupActions.RunAsync(");
        AssertContains(scenarioText, "DiagnosticSessionScenarioSetup.RunAsync(");
        AssertContains(scenarioText, "SampleLoopAsync(");

        return Task.CompletedTask;
    }

    private static Task DiagnosticSessionInitialSnapshot_OwnsBaselineCapture()
    {
        var runnerText = ReadDiagnosticSessionRunnerSource();
        var initialSnapshotText = ReadRepoFile("tools/Common/DiagnosticSessionInitialSnapshot.cs")
            .Replace("\r\n", "\n");

        AssertContains(initialSnapshotText, "internal static class DiagnosticSessionInitialSnapshot");
        AssertContains(initialSnapshotText, "internal static DiagnosticSessionInitialSnapshotResult CreateUnknown()");
        AssertContains(initialSnapshotText, "internal static async Task<DiagnosticSessionInitialSnapshotResult> CaptureAsync(");
        AssertContains(initialSnapshotText, "CreateEmptyJsonObject()");
        AssertContains(initialSnapshotText, "var unknownSnapshot = CreateUnknown();");
        AssertContains(initialSnapshotText, "setStage(\"initial-snapshot\")");
        AssertContains(initialSnapshotText, "commandChannel.SendAsync(\"GetSnapshot\", null, null)");
        AssertContains(initialSnapshotText, "TryGetSnapshot(initialResponse, out var initial)");
        AssertContains(initialSnapshotText, "commandChannel.RecordFailure(\"initial-snapshot: baseline snapshot unavailable; state-mutating scenarios will be skipped\")");
        AssertContains(initialSnapshotText, "recordTerminalException(ex, \"initial-snapshot\")");
        AssertContains(initialSnapshotText, "await writeLiveStateAsync().ConfigureAwait(false);");
        AssertContains(initialSnapshotText, "internal sealed class DiagnosticSessionInitialSnapshotResult");
        AssertContains(initialSnapshotText, "internal DiagnosticSessionInitialSnapshotResult(JsonElement snapshot, bool known)");
        AssertContains(initialSnapshotText, "internal JsonElement Snapshot { get; }");
        AssertContains(initialSnapshotText, "internal bool Known { get; }");
        AssertContains(runnerText, "var initialSnapshotResult = DiagnosticSessionInitialSnapshot.CreateUnknown();");
        AssertContains(runnerText, "DiagnosticSessionInitialSnapshot.CaptureAsync(");
        AssertContains(runnerText, "initialSnapshot = initialSnapshotResult.Snapshot;");
        AssertContains(runnerText, "initialSnapshotKnown = initialSnapshotResult.Known;");
        AssertDoesNotContain(runnerText, "CreateEmptyJsonObject()");
        AssertDoesNotContain(runnerText, "var initialResponse = await commandChannel.SendAsync(\"GetSnapshot\", null, null)");
        AssertDoesNotContain(runnerText, "TryGetSnapshot(initialResponse, out var initial)");
        AssertDoesNotContain(runnerText, "baseline snapshot unavailable; state-mutating scenarios will be skipped");

        return Task.CompletedTask;
    }

    private static Task DiagnosticSessionPipeRetryPolicy_OwnsConnectRetryClassification()
    {
        var runnerText = ReadDiagnosticSessionRunnerSource();
        var channelText = ReadRepoFile("tools/Common/DiagnosticSessionCommandChannel.cs")
            .Replace("\r\n", "\n");
        var retryText = ReadRepoFile("tools/Common/DiagnosticSessionPipeRetryPolicy.cs")
            .Replace("\r\n", "\n");

        AssertContains(retryText, "internal static class DiagnosticSessionPipeRetryPolicy");
        AssertContains(retryText, "BuildLocalFailureResponse(command, ex.Message)");
        AssertContains(retryText, "\"pipe-connect-failed\"");
        AssertContains(retryText, "\"pipe-connect-timeout\"");
        AssertContains(retryText, "\"pipe-access-denied\"");
        AssertContains(channelText, "using static Sussudio.Tools.DiagnosticSessionPipeRetryPolicy;");
        AssertContains(channelText, "SendCommandWithConnectRetryAsync(");
        AssertDoesNotContain(runnerText, "using static Sussudio.Tools.DiagnosticSessionPipeRetryPolicy;");
        AssertDoesNotContain(runnerText, "private static bool IsSyntheticPipeConnectFailure(");
        AssertDoesNotContain(runnerText, "private static bool IsPermanentPipeConnectFailure(");
        AssertDoesNotContain(runnerText, "private static JsonElement BuildLocalFailureResponse(");

        return Task.CompletedTask;
    }

    private static Task DiagnosticSessionCommandChannel_OwnsSerializedCommandSending()
    {
        var runnerText = ReadDiagnosticSessionRunnerSource();
        var channelText = ReadRepoFile("tools/Common/DiagnosticSessionCommandChannel.cs")
            .Replace("\r\n", "\n");

        AssertContains(channelText, "internal sealed class DiagnosticSessionCommandChannel : IDisposable");
        AssertContains(channelText, "private readonly SemaphoreSlim _sendGate = new(1, 1);");
        AssertContains(channelText, "internal int FailureCount => _failureCount;");
        AssertContains(channelText, "internal void RecordFailure(string warning)");
        AssertContains(channelText, "internal async Task<JsonElement> SendRawWithConnectRetryAsync(");
        AssertContains(channelText, "internal async Task<JsonElement> SendWithTokenAsync(");
        AssertContains(channelText, "BuildLocalFailureResponse(command, \"no response after connect retry\")");
        AssertContains(channelText, "RecordFailure($\"{command}:");
        AssertContains(channelText, "Get(response, \"Message\", \"command failed\")");
        AssertContains(channelText, "internal async Task TryWaitWithTokenAsync(");
        AssertContains(channelText, "\"WaitForCondition\"");
        AssertContains(channelText, "[\"pollMs\"] = 250");
        AssertContains(runnerText, "using var commandChannel = new DiagnosticSessionCommandChannel(");
        AssertContains(runnerText, "commandChannel.SendAsync");
        AssertContains(runnerText, "commandChannel.SendWithTokenAsync");
        AssertContains(runnerText, "commandChannel.FailureCount");
        AssertDoesNotContain(runnerText, "var commandFailureCount = 0;");
        AssertDoesNotContain(runnerText, "var commandSendGate = new SemaphoreSlim(1, 1);");
        AssertDoesNotContain(runnerText, "async Task<JsonElement> SendAsync(");
        AssertDoesNotContain(runnerText, "async Task TryWaitAsync(");

        return Task.CompletedTask;
    }

    private static Task DiagnosticSessionRunState_OwnsTerminalState()
    {
        var runnerText = ReadDiagnosticSessionRunnerSource();
        var stateText = ReadRepoFile("tools/Common/DiagnosticSessionRunState.cs")
            .Replace("\r\n", "\n");

        AssertContains(stateText, "internal sealed class DiagnosticSessionRunState");
        AssertContains(stateText, "internal void SetStage(string stage)");
        AssertContains(stateText, "internal void RecordTerminalException(Exception ex, string stage)");
        AssertContains(stateText, "internal string GetTerminalState()");
        AssertContains(stateText, "internal async Task WriteArtifactBestEffortAsync<T>(");
        AssertContains(runnerText, "var runState = new DiagnosticSessionRunState(");
        AssertDoesNotContain(stateText, "internal string LivePath { get; }");
        AssertDoesNotContain(stateText, "internal async Task WriteLiveStateBestEffortAsync(");
        AssertDoesNotContain(stateText, "internal async Task WriteSamplingLiveStateBestEffortAsync(");
        AssertDoesNotContain(runnerText, "var lastStage = \"initializing\";");
        AssertDoesNotContain(runnerText, "Exception? terminalException = null;");
        AssertDoesNotContain(runnerText, "DateTimeOffset.MinValue");

        return Task.CompletedTask;
    }

    private static Task DiagnosticSessionLiveStateWriter_OwnsBreadcrumbFile()
    {
        var runnerText = ReadDiagnosticSessionRunnerSource();
        var liveStateWriterText = ReadRepoFile("tools/Common/DiagnosticSessionLiveStateWriter.cs")
            .Replace("\r\n", "\n");

        AssertContains(liveStateWriterText, "internal sealed class DiagnosticSessionLiveStateWriter");
        AssertContains(liveStateWriterText, "LivePath = Path.Combine(runBootstrap.OutputDirectory, \"session-live.json\");");
        AssertContains(liveStateWriterText, "internal string LivePath { get; }");
        AssertContains(liveStateWriterText, "internal async Task WriteLiveStateBestEffortAsync(");
        AssertContains(liveStateWriterText, "internal async Task WriteSamplingLiveStateBestEffortAsync(");
        AssertContains(liveStateWriterText, "TerminalState = terminalStateOverride ?? (_runState.TerminalException is null ? \"running\" : _runState.GetTerminalState())");
        AssertContains(liveStateWriterText, "LastStage = terminalStateOverride is null ? _runState.LastStage : _runState.GetResultLastStage()");
        AssertContains(liveStateWriterText, "TimeSpan.FromSeconds(5)");
        AssertContains(liveStateWriterText, "The live-state file is diagnostic breadcrumbs only.");
        AssertContains(runnerText, "var liveStateWriter = new DiagnosticSessionLiveStateWriter(runBootstrap, runState, warnings);");
        AssertContains(runnerText, "var livePath = liveStateWriter.LivePath;");
        AssertContains(runnerText, "liveStateWriter.WriteLiveStateBestEffortAsync(");
        AssertContains(runnerText, "liveStateWriter.WriteSamplingLiveStateBestEffortAsync(");
        AssertDoesNotContain(runnerText, "var livePath = runState.LivePath;");

        return Task.CompletedTask;
    }

    private static Task DiagnosticSessionRunBootstrap_OwnsNormalizedSessionIdentity()
    {
        var runnerText = ReadDiagnosticSessionRunnerSource();
        var bootstrapText = ReadRepoFile("tools/Common/DiagnosticSessionRunBootstrap.cs")
            .Replace("\r\n", "\n");

        AssertContains(bootstrapText, "internal readonly record struct DiagnosticSessionRunBootstrap(");
        AssertContains(bootstrapText, "internal static DiagnosticSessionRunBootstrap Create(DiagnosticSessionOptions options)");
        AssertContains(bootstrapText, "var scenario = DiagnosticSessionScenarios.Normalize(options.Scenario);");
        AssertContains(bootstrapText, "var scenarioPlan = DiagnosticSessionScenarioPlan.From(scenario);");
        AssertContains(bootstrapText, "Math.Clamp(options.DurationSeconds, 0, 24 * 60 * 60)");
        AssertContains(bootstrapText, "Math.Clamp(options.SampleIntervalMs, 100, 60_000)");
        AssertContains(bootstrapText, "DateTimeOffset.UtcNow.ToString(\"yyyyMMdd_HHmmss\", CultureInfo.InvariantCulture)");
        AssertContains(bootstrapText, "Path.Combine(Environment.CurrentDirectory, \"temp\", \"diagnostic-sessions\", sessionId)");
        AssertContains(bootstrapText, "Path.GetFullPath(options.OutputDirectory)");
        AssertContains(bootstrapText, "Directory.CreateDirectory(outputDirectory);");
        AssertContains(bootstrapText, "Environment.ProcessId");
        AssertContains(runnerText, "var runBootstrap = DiagnosticSessionRunBootstrap.Create(options);");
        AssertContains(runnerText, "var scenarioPlan = runBootstrap.ScenarioPlan;");
        AssertContains(runnerText, "using var sessionLock = DiagnosticSessionOutputLock.Acquire(outputDirectory);");
        AssertDoesNotContain(runnerText, "DiagnosticSessionScenarios.Normalize(options.Scenario)");
        AssertDoesNotContain(runnerText, "Math.Clamp(options.DurationSeconds");
        AssertDoesNotContain(runnerText, "Math.Clamp(options.SampleIntervalMs");
        AssertDoesNotContain(runnerText, "DateTimeOffset.UtcNow.ToString(\"yyyyMMdd_HHmmss\"");
        AssertDoesNotContain(runnerText, "Directory.CreateDirectory(outputDirectory);");
        AssertDoesNotContain(runnerText, "var runFlashbackPlayback = scenarioPlan.RunFlashbackPlayback;");

        return Task.CompletedTask;
    }

    private static Task DiagnosticSessionOutputLock_OwnsExclusiveOutputDirectoryLock()
    {
        var runnerText = ReadDiagnosticSessionRunnerSource();
        var lockText = ReadRepoFile("tools/Common/DiagnosticSessionOutputLock.cs")
            .Replace("\r\n", "\n");

        AssertContains(lockText, "internal static class DiagnosticSessionOutputLock");
        AssertContains(lockText, "internal static FileStream Acquire(string outputDirectory)");
        AssertContains(lockText, "\".sussudio-diag.lock\"");
        AssertContains(lockText, "FileShare.None");
        AssertContains(lockText, "FileOptions.DeleteOnClose");
        AssertContains(lockText, "Another diagnostic session is already running");
        AssertContains(runnerText, "using var sessionLock = DiagnosticSessionOutputLock.Acquire(outputDirectory);");
        AssertDoesNotContain(runnerText, "sessionLock.Dispose();");
        AssertDoesNotContain(runnerText, "var lockPath = Path.Combine(outputDirectory, \".sussudio-diag.lock\")");
        AssertDoesNotContain(runnerText, "FileShare.None");
        AssertDoesNotContain(runnerText, "FileOptions.DeleteOnClose");

        return Task.CompletedTask;
    }

    private static Task DiagnosticSessionRunExecutionScenario_OwnsScenarioPhase()
    {
        var executionText = ReadDiagnosticSessionRunExecutionRootSource();
        var scenarioText = ReadDiagnosticSessionRunExecutionScenarioSource();

        AssertContains(scenarioText, "private static Task<DiagnosticSessionScenarioPhaseResult> RunScenarioPhaseAsync(DiagnosticSessionScenarioPhaseContext context)");
        AssertContains(scenarioText, "DiagnosticSessionScenarioPhaseRunner.RunAsync(context)");
        AssertContains(scenarioText, "internal static class DiagnosticSessionScenarioPhaseRunner");
        AssertContains(scenarioText, "internal sealed class DiagnosticSessionScenarioPhaseContext");
        AssertContains(scenarioText, "internal sealed record DiagnosticSessionScenarioPhaseResult(");
        AssertContains(scenarioText, "internal sealed class DiagnosticSessionScenarioPhaseState");
        AssertContains(scenarioText, "return scenarioPhase.ToResult();");
        AssertContains(scenarioText, "DiagnosticSessionScenarioSetup.RunAsync(");
        AssertContains(scenarioText, "DiagnosticSessionScenarioStartup.StartAsync(");
        AssertContains(scenarioText, "context.SetStage(\"sampling\")");
        AssertContains(scenarioText, "SampleLoopAsync(");
        AssertContains(scenarioText, "backgroundTasks.AwaitScenarioTasksAsync()");
        AssertContains(scenarioText, "AwaitRecordingSettingsDeferredAsync(scenarioPhase.FlashbackRecordingSettingsDeferredPresetState)");
        AssertContains(scenarioText, "DiagnosticSessionFlashbackRejectedExports.RunSelectedRejectedExportScenariosAsync(");
        AssertContains(scenarioText, "backgroundTasks.AwaitPresentMonAsync(scenarioPhase.PresentMon, context.Warnings)");
        AssertContains(scenarioText, "context.RecordTerminalException(ex, context.GetLastStage())");
        AssertContains(scenarioText, "context.ScenarioCancellationSource.Cancel();");
        AssertContains(scenarioText, "backgroundTasks.ObserveAfterFaultAsync(");
        AssertContains(executionText, "var scenarioPhaseContext = new DiagnosticSessionScenarioPhaseContext");
        AssertContains(executionText, "var scenarioPhase = DiagnosticSessionScenarioPhaseResult.Empty;");
        AssertContains(executionText, "scenarioPhase = await RunScenarioPhaseAsync(scenarioPhaseContext)");
        AssertContains(executionText, "scenarioPhase.StartedRecording");
        AssertContains(executionText, "scenarioPhase.StartedPreview");
        AssertContains(executionText, "scenarioPhase.EnabledFlashback");
        AssertContains(executionText, "scenarioPhase.DisabledFlashback");
        AssertContains(executionText, "scenarioPhase.StartedFlashbackPlayback");
        AssertContains(executionText, "scenarioPhase.PresentMon");
        AssertContains(executionText, "scenarioPhase.FlashbackRecordingSettingsDeferredPresetState");
        AssertDoesNotContain(executionText, "new DiagnosticSessionScenarioPhaseState()");
        AssertDoesNotContain(scenarioText, "internal required DiagnosticSessionScenarioPhaseState PhaseState");
        AssertDoesNotContain(executionText, "DiagnosticSessionScenarioSetup.RunAsync(");
        AssertDoesNotContain(executionText, "DiagnosticSessionScenarioStartup.StartAsync(");
        AssertDoesNotContain(executionText, "SampleLoopAsync(");
        AssertDoesNotContain(executionText, "backgroundTasks.AwaitScenarioTasksAsync()");
        AssertDoesNotContain(executionText, "DiagnosticSessionFlashbackRejectedExports.RunSelectedRejectedExportScenariosAsync(");
        AssertDoesNotContain(executionText, "backgroundTasks.ObserveAfterFaultAsync(");
        AssertOccursBefore(scenarioText, "DiagnosticSessionScenarioSetup.RunAsync(", "DiagnosticSessionScenarioStartup.StartAsync(");
        AssertOccursBefore(scenarioText, "DiagnosticSessionScenarioStartup.StartAsync(", "SampleLoopAsync(");
        AssertOccursBefore(scenarioText, "SampleLoopAsync(", "backgroundTasks.AwaitScenarioTasksAsync()");
        AssertOccursBefore(scenarioText, "backgroundTasks.AwaitScenarioTasksAsync()", "DiagnosticSessionFlashbackRejectedExports.RunSelectedRejectedExportScenariosAsync(");
        AssertOccursBefore(scenarioText, "DiagnosticSessionFlashbackRejectedExports.RunSelectedRejectedExportScenariosAsync(", "backgroundTasks.AwaitPresentMonAsync(");

        return Task.CompletedTask;
    }
}

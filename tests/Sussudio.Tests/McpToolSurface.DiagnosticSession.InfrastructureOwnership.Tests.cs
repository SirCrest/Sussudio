using System.Threading.Tasks;

static partial class Program
{
    internal static Task DiagnosticSessionRunner_OwnsCompatibilitySurface()
    {
        var runnerText = ReadRepoFile("tools/Common/DiagnosticSessionRunner.cs")
            .Replace("\r\n", "\n");
        var executionText = ReadDiagnosticSessionRunExecutionRootSource();
        var scenarioText = ReadDiagnosticSessionRunExecutionScenarioSource();

        AssertContains(runnerText, "public static class DiagnosticSessionRunner");
        AssertContains(runnerText, "public static async Task<DiagnosticSessionResult> RunAsync(");
        AssertContains(runnerText, "await runContext.CaptureInitialSnapshotAsync().ConfigureAwait(false);");
        AssertContains(runnerText, "DiagnosticSessionScenarioPhaseRunner.RunAsync(scenarioPhaseContext)");
        AssertContains(runnerText, "DiagnosticSessionCleanupActions.RunAsync(");
        AssertContains(runnerText, "return await RunCompletionPhaseAsync(");
        AssertContains(runnerText, "return DiagnosticSessionResultFormatter.Format(result);");
        AssertContains(runnerText, "private static FileStream AcquireOutputLock(string outputDirectory)");
        AssertDoesNotContain(runnerText, "internal static class DiagnosticSessionRunExecution");
        AssertDoesNotContain(runnerText, "DiagnosticSessionRunExecution.RunAsync(");
        AssertDoesNotContain(runnerText, "DiagnosticSessionScenarioSetup.RunAsync(");
        AssertDoesNotContain(runnerText, "SampleLoopAsync(");
        AssertContains(executionText, "DiagnosticSessionScenarioPhaseRunner.RunAsync(scenarioPhaseContext)");
        AssertContains(executionText, "DiagnosticSessionCleanupActions.RunAsync(");
        AssertContains(scenarioText, "DiagnosticSessionScenarioSetup.RunAsync(");
        AssertContains(scenarioText, "SampleLoopAsync(");

        return Task.CompletedTask;
    }

    internal static Task DiagnosticSessionInitialSnapshot_OwnsBaselineCapture()
    {
        var runnerText = ReadDiagnosticSessionRunnerSource();
        var executionText = ReadDiagnosticSessionRunExecutionRootSource();
        var contextText = ReadDiagnosticSessionRunContextSource();
        var initialSnapshotText = contextText;

        AssertContains(initialSnapshotText, "internal sealed class DiagnosticSessionRunContext : IDisposable");
        AssertContains(initialSnapshotText, "using Sussudio.Models;");
        AssertContains(initialSnapshotText, "private DiagnosticSessionInitialSnapshotResult CreateUnknownInitialSnapshot()");
        AssertContains(initialSnapshotText, "private async Task<DiagnosticSessionInitialSnapshotResult> CaptureInitialSnapshotCoreAsync()");
        AssertContains(initialSnapshotText, "CreateEmptyJsonObject()");
        AssertContains(initialSnapshotText, "var unknownSnapshot = CreateUnknownInitialSnapshot();");
        AssertContains(initialSnapshotText, "SetStage(\"initial-snapshot\")");
        AssertContains(initialSnapshotText, "CommandChannel.SendAsync(AutomationCommandKind.GetSnapshot, null, null)");
        AssertDoesNotContain(initialSnapshotText, "commandChannel.SendAsync(\"GetSnapshot\", null, null)");
        AssertContains(initialSnapshotText, "TryGetSnapshot(initialResponse, out var initial)");
        AssertContains(initialSnapshotText, "CommandChannel.RecordFailure(\"initial-snapshot: baseline snapshot unavailable; state-mutating scenarios will be skipped\")");
        AssertContains(initialSnapshotText, "RecordTerminalException(ex, \"initial-snapshot\")");
        AssertContains(initialSnapshotText, "await WriteLiveStateBestEffortAsync().ConfigureAwait(false);");
        AssertContains(initialSnapshotText, "internal sealed class DiagnosticSessionInitialSnapshotResult");
        AssertContains(initialSnapshotText, "internal DiagnosticSessionInitialSnapshotResult(JsonElement snapshot, bool known)");
        AssertContains(initialSnapshotText, "internal JsonElement Snapshot { get; }");
        AssertContains(initialSnapshotText, "internal bool Known { get; }");
        AssertContains(contextText, "var unknownSnapshot = CreateUnknownInitialSnapshot();");
        AssertContains(contextText, "internal async Task CaptureInitialSnapshotAsync()");
        AssertContains(contextText, "CaptureInitialSnapshotCoreAsync()");
        AssertContains(contextText, "InitialSnapshot = initialSnapshotResult.Snapshot;");
        AssertContains(contextText, "InitialSnapshotKnown = initialSnapshotResult.Known;");
        AssertContains(runnerText, "await runContext.CaptureInitialSnapshotAsync().ConfigureAwait(false);");
        AssertDoesNotContain(executionText, "CreateEmptyJsonObject()");
        AssertDoesNotContain(executionText, "var initialResponse = await commandChannel.SendAsync(\"GetSnapshot\", null, null)");
        AssertDoesNotContain(executionText, "var initialResponse = await commandChannel.SendAsync(AutomationCommandKind.GetSnapshot, null, null)");
        AssertDoesNotContain(executionText, "TryGetSnapshot(initialResponse, out var initial)");
        AssertDoesNotContain(executionText, "baseline snapshot unavailable; state-mutating scenarios will be skipped");

        return Task.CompletedTask;
    }

    internal static Task DiagnosticSessionPipeRetryPolicy_OwnsConnectRetryClassification()
    {
        var runnerText = ReadDiagnosticSessionRunnerSource();
        var channelText = ReadRepoFile("tools/Common/DiagnosticSessionCommandChannel.cs")
            .Replace("\r\n", "\n");
        var retryText = channelText;

        AssertContains(retryText, "internal static class DiagnosticSessionPipeRetryPolicy");
        AssertContains(retryText, "BuildLocalFailureResponse(command, ex.Message)");
        AssertContains(retryText, "\"pipe-connect-failed\"");
        AssertContains(retryText, "\"pipe-connect-timeout\"");
        AssertContains(retryText, "\"pipe-access-denied\"");
        AssertContains(channelText, "using static Sussudio.Tools.DiagnosticSessionPipeRetryPolicy;");
        AssertContains(channelText, "SendCommandWithConnectRetryAsync(");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "tools", "Common", "DiagnosticSessionPipeRetryPolicy.cs")),
            "diagnostic-session pipe retry policy lives with the command channel transport owner");
        AssertDoesNotContain(runnerText, "using static Sussudio.Tools.DiagnosticSessionPipeRetryPolicy;");
        AssertDoesNotContain(runnerText, "private static bool IsSyntheticPipeConnectFailure(");
        AssertDoesNotContain(runnerText, "private static bool IsPermanentPipeConnectFailure(");
        AssertDoesNotContain(runnerText, "private static JsonElement BuildLocalFailureResponse(");

        return Task.CompletedTask;
    }

    internal static Task DiagnosticSessionCommandChannel_OwnsSerializedCommandSending()
    {
        var runnerText = ReadDiagnosticSessionRunnerSource();
        var executionText = ReadDiagnosticSessionRunExecutionRootSource();
        var contextText = ReadDiagnosticSessionRunContextSource();
        var channelText = ReadRepoFile("tools/Common/DiagnosticSessionCommandChannel.cs")
            .Replace("\r\n", "\n");

        AssertContains(channelText, "internal sealed class DiagnosticSessionCommandChannel : IDisposable");
        AssertContains(channelText, "using Sussudio.Models;");
        AssertContains(channelText, "private readonly SemaphoreSlim _sendGate = new(1, 1);");
        AssertContains(channelText, "internal int FailureCount => _failureCount;");
        AssertContains(channelText, "internal void RecordFailure(string warning)");
        AssertContains(channelText, "private static string CommandName(AutomationCommandKind kind)");
        AssertContains(channelText, "=> AutomationCommandCatalog.Get(kind).Name;");
        AssertContains(channelText, "internal async Task<JsonElement> SendRawWithConnectRetryAsync(");
        AssertContains(channelText, "internal async Task<JsonElement> SendRawWithConnectRetryWithTokenAsync(");
        AssertContains(channelText, "internal async Task<JsonElement> SendWithTokenAsync(");
        AssertContains(channelText, "AutomationCommandKind kind,");
        AssertContains(channelText, "=> await SendRawWithConnectRetryAsync(CommandName(kind), payload, responseTimeoutMs).ConfigureAwait(false);");
        AssertContains(channelText, "=> await SendAsync(CommandName(kind), payload, responseTimeoutMs).ConfigureAwait(false);");
        AssertContains(channelText, "=> await SendWithTokenAsync(CommandName(kind), payload, responseTimeoutMs, allowFailure, commandCancellationToken).ConfigureAwait(false);");
        AssertContains(channelText, "BuildLocalFailureResponse(command, \"no response after connect retry\")");
        AssertContains(channelText, "RecordFailure($\"{command}:");
        AssertContains(channelText, "Get(response, \"Message\", \"command failed\")");
        AssertContains(channelText, "internal async Task TryWaitAsync(string condition, int timeoutMs)");
        AssertContains(channelText, "internal async Task TryWaitWithTokenAsync(");
        AssertContains(channelText, "SendWithTokenAsync(\n                AutomationCommandKind.WaitForCondition,");
        AssertContains(channelText, "AutomationCommandKind.WaitForCondition");
        AssertContains(channelText, "[\"condition\"] = condition");
        AssertContains(channelText, "[\"timeoutMs\"] = timeoutMs");
        AssertContains(channelText, "[\"pollMs\"] = 250");
        AssertContains(channelText, "timeoutMs + 2_000");
        AssertContains(channelText, "$\"wait {condition}: {Get(response, \"Message\", \"not met\")}\"");
        AssertDoesNotContain(channelText, "\"WaitForCondition\"");
        AssertDoesNotContain(channelText, "\"GetSnapshot\"");
        AssertContains(contextText, "CommandChannel = new DiagnosticSessionCommandChannel(");
        AssertContains(runnerText, "context.CommandChannel,");
        AssertContains(runnerText, "runContext.CommandChannel,");
        AssertContains(contextText, "CommandChannel.FailureCount");
        AssertDoesNotContain(executionText, "new DiagnosticSessionCommandChannel(");
        AssertDoesNotContain(runnerText, "var commandFailureCount = 0;");
        AssertDoesNotContain(runnerText, "var commandSendGate = new SemaphoreSlim(1, 1);");
        AssertDoesNotContain(runnerText, "async Task<JsonElement> SendAsync(");
        AssertDoesNotContain(runnerText, "async Task TryWaitAsync(");

        return Task.CompletedTask;
    }

    internal static Task DiagnosticSessionRunExecutionScenario_OwnsScenarioPhase()
    {
        var executionText = ReadDiagnosticSessionRunExecutionRootSource();
        var contextText = ReadDiagnosticSessionRunContextSource();
        var scenarioText = ReadDiagnosticSessionRunExecutionScenarioSource();
        var phaseRunnerText = ReadRepoFile("tools/Common/DiagnosticSessionScenarioPhaseRunner.cs")
            .Replace("\r\n", "\n");
        var phaseModelsText = ReadRepoFile("tools/Common/DiagnosticSessionModels.cs")
            .Replace("\r\n", "\n");
        var completionText = phaseRunnerText;
        var backgroundTasksText = ReadDiagnosticSessionBackgroundTasksSource();

        AssertContains(executionText, "DiagnosticSessionScenarioPhaseRunner.RunAsync(scenarioPhaseContext)");
        AssertContains(phaseRunnerText, "internal static class DiagnosticSessionScenarioPhaseRunner");
        AssertContains(phaseModelsText, "internal sealed class DiagnosticSessionScenarioPhaseContext");
        AssertContains(phaseModelsText, "internal sealed record DiagnosticSessionScenarioPhaseResult(");
        AssertContains(phaseModelsText, "internal sealed class DiagnosticSessionScenarioPhaseState");
        AssertContains(scenarioText, "internal sealed class DiagnosticSessionScenarioPhaseContext");
        AssertContains(scenarioText, "internal sealed record DiagnosticSessionScenarioPhaseResult(");
        AssertContains(scenarioText, "internal sealed class DiagnosticSessionScenarioPhaseState");
        AssertContains(scenarioText, "return scenarioPhase.ToResult();");
        AssertContains(scenarioText, "DiagnosticSessionScenarioSetup.RunAsync(");
        AssertContains(scenarioText, "DiagnosticSessionScenarioStartup.StartAsync(");
        AssertContains(phaseRunnerText, "RunSamplingAndCompleteAsync(context, backgroundTasks, scenarioPhase)");
        AssertContains(phaseRunnerText, "private static async Task RunSamplingAndCompleteAsync(");
        AssertContains(phaseRunnerText, "context.SetStage(\"sampling\")");
        AssertContains(phaseRunnerText, "SampleLoopAsync(");
        AssertContains(phaseRunnerText, "CompleteAfterSamplingAsync(");
        AssertContains(completionText, "private static async Task CompleteAfterSamplingAsync(");
        AssertContains(completionText, ".CompleteRegisteredScenarioWorkAsync(scenarioPhase.FlashbackRecordingSettingsDeferredPresetState)");
        AssertContains(completionText, "DiagnosticSessionFlashbackRejectedExports.RunSelectedRejectedExportScenariosAsync(");
        AssertContains(completionText, "backgroundTasks.CompletePresentMonAsync(scenarioPhase.PresentMon, context.Warnings)");
        AssertContains(backgroundTasksText, "internal async Task<FlashbackRecordingSettingsDeferredPresetState> CompleteRegisteredScenarioWorkAsync(");
        AssertContains(backgroundTasksText, "private async Task AwaitScenarioTasksAsync()");
        AssertContains(backgroundTasksText, "private async Task<FlashbackRecordingSettingsDeferredPresetState> AwaitRecordingSettingsDeferredAsync(");
        AssertContains(backgroundTasksText, "internal async Task<PresentMonProbeResult?> CompletePresentMonAsync(");
        AssertContains(scenarioText, "context.RecordTerminalException(ex, context.GetLastStage())");
        AssertContains(scenarioText, "context.ScenarioCancellationSource.Cancel();");
        AssertContains(phaseRunnerText, "DrainAfterFaultAsync(context, backgroundTasks, scenarioPhase)");
        AssertContains(completionText, "private static async Task DrainAfterFaultAsync(");
        AssertContains(completionText, "backgroundTasks.ObserveAfterFaultAsync(");
        AssertDoesNotContain(phaseRunnerText, "internal sealed class DiagnosticSessionScenarioPhaseContext");
        AssertDoesNotContain(phaseRunnerText, "internal sealed record DiagnosticSessionScenarioPhaseResult(");
        AssertDoesNotContain(phaseRunnerText, "internal sealed class DiagnosticSessionScenarioPhaseState");
        AssertContains(contextText, "new DiagnosticSessionScenarioPhaseContext");
        AssertContains(executionText, "var scenarioPhaseContext = runContext.CreateScenarioPhaseContext(options, cancellationToken);");
        AssertContains(executionText, "var scenarioPhase = DiagnosticSessionScenarioPhaseResult.Empty;");
        AssertContains(executionText, "scenarioPhase = await DiagnosticSessionScenarioPhaseRunner.RunAsync(scenarioPhaseContext)");
        AssertContains(executionText, "scenarioPhase.StartedRecording");
        AssertContains(executionText, "scenarioPhase.StartedPreview");
        AssertContains(executionText, "scenarioPhase.EnabledFlashback");
        AssertContains(executionText, "scenarioPhase.DisabledFlashback");
        AssertContains(executionText, "scenarioPhase.StartedFlashbackPlayback");
        AssertContains(contextText, "ScenarioPhase = scenarioPhase,");
        AssertDoesNotContain(executionText, "new DiagnosticSessionScenarioPhaseState()");
        AssertDoesNotContain(scenarioText, "internal required DiagnosticSessionScenarioPhaseState PhaseState");
        AssertDoesNotContain(executionText, "DiagnosticSessionScenarioSetup.RunAsync(");
        AssertDoesNotContain(executionText, "DiagnosticSessionScenarioStartup.StartAsync(");
        AssertDoesNotContain(executionText, "SampleLoopAsync(");
        AssertDoesNotContain(executionText, "backgroundTasks.AwaitScenarioTasksAsync()");
        AssertDoesNotContain(executionText, "DiagnosticSessionFlashbackRejectedExports.RunSelectedRejectedExportScenariosAsync(");
        AssertDoesNotContain(executionText, "backgroundTasks.ObserveAfterFaultAsync(");
        AssertDoesNotContain(phaseRunnerText, "backgroundTasks.AwaitScenarioTasksAsync()");
        AssertOccursBefore(phaseRunnerText, "DiagnosticSessionScenarioSetup.RunAsync(", "DiagnosticSessionScenarioStartup.StartAsync(");
        AssertOccursBefore(phaseRunnerText, "DiagnosticSessionScenarioStartup.StartAsync(", "RunSamplingAndCompleteAsync(context, backgroundTasks, scenarioPhase)");
        AssertOccursBefore(phaseRunnerText, "context.RecordTerminalException(ex, context.GetLastStage())", "context.ScenarioCancellationSource.Cancel();");
        AssertOccursBefore(phaseRunnerText, "context.ScenarioCancellationSource.Cancel();", "DrainAfterFaultAsync(context, backgroundTasks, scenarioPhase)");
        AssertOccursBefore(phaseRunnerText, "context.SetStage(\"sampling\")", "SampleLoopAsync(");
        AssertOccursBefore(phaseRunnerText, "SampleLoopAsync(", "CompleteAfterSamplingAsync(");
        AssertOccursBefore(backgroundTasksText, "await AwaitScenarioTasksAsync()", "return await AwaitRecordingSettingsDeferredAsync(");
        AssertOccursBefore(completionText, ".CompleteRegisteredScenarioWorkAsync(", "DiagnosticSessionFlashbackRejectedExports.RunSelectedRejectedExportScenariosAsync(");
        AssertOccursBefore(completionText, "DiagnosticSessionFlashbackRejectedExports.RunSelectedRejectedExportScenariosAsync(", "backgroundTasks.CompletePresentMonAsync(");

        return Task.CompletedTask;
    }

    internal static Task DiagnosticSessionRunExecutionCompletion_OwnsPostCleanupEvidenceAndResult()
    {
        var executionText = ReadDiagnosticSessionRunExecutionRootSource();
        var contextText = ReadDiagnosticSessionRunContextSource();
        var completionRootText = ReadDiagnosticSessionRunExecutionCompletionRootSource();
        var completionContextText = ReadDiagnosticSessionRunExecutionCompletionContextSource();
        var recordingChecksText = ReadRepoFile("tools/Common/DiagnosticSessionRecordingChecks.cs")
            .Replace("\r\n", "\n");
        var recordingVerificationText = recordingChecksText;
        var postRunText = completionRootText;
        var resultBuilderText = ReadRepoFile("tools/Common/DiagnosticSessionResultBuilder.cs")
            .Replace("\r\n", "\n");
        var agentMapText = ReadRepoFile("docs/architecture/AGENT_MAP.md")
            .Replace("\r\n", "\n");
        var cleanupPlanText = ReadRepoFile("docs/architecture/cleanup-plan.md")
            .Replace("\r\n", "\n");

        AssertContains(completionRootText, "private static async Task<DiagnosticSessionResult> RunCompletionPhaseAsync(DiagnosticSessionCompletionContext context)");
        AssertContains(completionContextText, "internal sealed class DiagnosticSessionCompletionContext");
        AssertContains(completionRootText, "private static DiagnosticSessionResultBuildRequest CreateResultBuildRequest(");
        AssertContains(completionRootText, "internal sealed class DiagnosticSessionCompletionContext");
        AssertContains(completionRootText, "DiagnosticSessionRecordingChecks.RunAsync(");
        AssertContains(completionRootText, "var verification = recordingCheckResult.Verification;");
        AssertContains(completionRootText, "context.ScenarioPhase.FlashbackRecordingSettingsDeferredPresetState");
        AssertContains(completionRootText, "CapturePostRunSnapshotsAsync(");
        AssertContains(completionRootText, "DiagnosticSessionResultBuilder.BuildAndWriteAsync(");
        AssertContains(completionRootText, "CreateResultBuildRequest(");
        AssertContains(completionRootText, "context.ScenarioPhase.PresentMon");
        AssertContains(completionRootText, "await context.WriteLiveStateBestEffortAsync(result.CompletedUtc, result.TerminalState).ConfigureAwait(false);");
        AssertContains(completionRootText, "postRunSnapshots.HealthSnapshot");
        AssertContains(completionRootText, "postRunSnapshots.Timeline");
        AssertContains(completionRootText, "runBootstrap.RunnerProcessId");
        AssertContains(contextText, "new DiagnosticSessionCompletionContext");
        AssertContains(executionText, "return await RunCompletionPhaseAsync(");
        AssertContains(executionText, "runContext.CreateCompletionContext(options, scenarioPhase, stoppedRecordingForVerification, cancellationToken)");
        AssertContains(executionText, "DiagnosticSessionRecordingChecks.RunAsync(");
        AssertContains(executionText, "CapturePostRunSnapshotsAsync(");
        AssertContains(executionText, "DiagnosticSessionResultBuilder.BuildAndWriteAsync(");
        AssertContains(recordingVerificationText, "setStage(\"recording-verification\")");
        AssertContains(postRunText, "setStage(\"timeline\")");
        AssertContains(postRunText, "setStage(\"final-snapshot\")");
        AssertContains(resultBuilderText, "runState.SetStage(\"summary\")");
        AssertContains(agentMapText, "completion context handoff consumed by the post-cleanup completion phase");
        AssertContains(agentMapText, "post-cleanup evidence/result sequence, result-build");
        AssertContains(cleanupPlanText, "`DiagnosticSessionRunner.cs` owns the completion context handoff");
        AssertContains(cleanupPlanText, "`DiagnosticSessionRunner.cs` owns the post-cleanup evidence/result sequence");
        AssertOccursBefore(completionRootText, "DiagnosticSessionRecordingChecks.RunAsync(", "CapturePostRunSnapshotsAsync(");
        AssertOccursBefore(completionRootText, "CapturePostRunSnapshotsAsync(", "DiagnosticSessionResultBuilder.BuildAndWriteAsync(");
        AssertOccursBefore(completionRootText, "DiagnosticSessionResultBuilder.BuildAndWriteAsync(", "await context.WriteLiveStateBestEffortAsync(result.CompletedUtc, result.TerminalState)");
        AssertOccursBefore(postRunText, "setStage(\"timeline\")", "setStage(\"final-snapshot\")");

        return Task.CompletedTask;
    }

    internal static Task DiagnosticSessionRunState_OwnsTerminalState()
    {
        var runnerText = ReadDiagnosticSessionRunnerSource();
        var contextText = ReadDiagnosticSessionRunContextSource();

        AssertContains(contextText, "internal sealed class DiagnosticSessionRunState");
        AssertContains(contextText, "internal void SetStage(string stage)");
        AssertContains(contextText, "internal void RecordTerminalException(Exception ex, string stage)");
        AssertContains(contextText, "internal string GetTerminalState()");
        AssertContains(contextText, "internal async Task WriteArtifactBestEffortAsync<T>(");
        AssertContains(contextText, "RunState = new DiagnosticSessionRunState(");
        AssertContains(contextText, "internal void SetStage(string stage)");
        AssertContains(contextText, "internal void RecordTerminalException(Exception ex, string stage)");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "tools", "Common", "DiagnosticSessionRunState.cs")),
            "run state stays folded into DiagnosticSessionRunContext.cs");
        AssertDoesNotContain(runnerText, "var lastStage = \"initializing\";");
        AssertDoesNotContain(runnerText, "Exception? terminalException = null;");
        AssertDoesNotContain(runnerText, "DateTimeOffset.MinValue");

        return Task.CompletedTask;
    }

    internal static Task DiagnosticSessionLiveStateWriter_OwnsBreadcrumbFile()
    {
        var runnerText = ReadDiagnosticSessionRunnerSource();
        var contextText = ReadDiagnosticSessionRunContextSource();
        var liveStateWriterText = ReadRepoFile("tools/Common/DiagnosticSessionLiveStateWriter.cs")
            .Replace("\r\n", "\n");

        AssertContains(liveStateWriterText, "internal sealed class DiagnosticSessionLiveStateWriter");
        AssertContains(liveStateWriterText, "LivePath = Path.Combine(runBootstrap.OutputDirectory, \"session-live.json\");");
        AssertContains(liveStateWriterText, "internal string LivePath { get; }");
        AssertContains(liveStateWriterText, "internal async Task WriteLiveStateBestEffortAsync(");
        AssertContains(liveStateWriterText, "internal async Task WriteSamplingLiveStateBestEffortAsync(");
        AssertContains(liveStateWriterText, "private DateTimeOffset _lastSamplingLiveStateUtc = DateTimeOffset.MinValue;");
        AssertContains(liveStateWriterText, "TerminalState = terminalStateOverride ?? (_runState.TerminalException is null ? \"running\" : _runState.GetTerminalState())");
        AssertContains(liveStateWriterText, "LastStage = terminalStateOverride is null ? _runState.LastStage : _runState.GetResultLastStage()");
        AssertContains(liveStateWriterText, "TimeSpan.FromSeconds(5)");
        AssertContains(liveStateWriterText, "The live-state file is diagnostic breadcrumbs only.");
        AssertContains(contextText, "_liveStateWriter = new DiagnosticSessionLiveStateWriter(RunBootstrap, RunState, Warnings);");
        AssertContains(contextText, "LivePath = _liveStateWriter.LivePath;");
        AssertContains(contextText, "_liveStateWriter.WriteLiveStateBestEffortAsync(");
        AssertContains(contextText, "_liveStateWriter.WriteSamplingLiveStateBestEffortAsync(");
        AssertDoesNotContain(runnerText, "var livePath = runState.LivePath;");

        return Task.CompletedTask;
    }

    internal static Task DiagnosticSessionRunContext_OwnsMutableRunInfrastructure()
    {
        var executionText = ReadDiagnosticSessionRunExecutionRootSource();
        var contextText = ReadDiagnosticSessionRunContextSource();
        var agentMapText = ReadRepoFile("docs/architecture/AGENT_MAP.md")
            .Replace("\r\n", "\n");
        var cleanupPlanText = ReadRepoFile("docs/architecture/cleanup-plan.md")
            .Replace("\r\n", "\n");

        AssertContains(contextText, "internal sealed class DiagnosticSessionRunContext : IDisposable");
        AssertContains(contextText, "RunBootstrap = DiagnosticSessionRunBootstrap.Create(options);");
        AssertContains(contextText, "Actions = [];");
        AssertContains(contextText, "Warnings = [];");
        AssertContains(contextText, "Samples = [];");
        AssertContains(contextText, "ScenarioCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(runCancellationToken);");
        AssertContains(contextText, "CommandChannel = new DiagnosticSessionCommandChannel(sendCommandAsync, ScenarioCancellationToken, Warnings);");
        AssertContains(contextText, "InitializeUnknownSnapshotState();");
        AssertContains(contextText, "internal JsonElement InitialSnapshot { get; private set; }");
        AssertContains(contextText, "private void InitializeUnknownSnapshotState()");
        AssertContains(contextText, "InitialSnapshot = unknownSnapshot.Snapshot;");
        AssertContains(contextText, "internal async Task CaptureInitialSnapshotAsync()");
        AssertContains(contextText, "private readonly DiagnosticSessionLiveStateWriter _liveStateWriter;");
        AssertContains(contextText, "internal string LivePath { get; }");
        AssertContains(contextText, "internal async Task WriteLiveStateBestEffortAsync(");
        AssertContains(contextText, "internal async Task WriteSamplingLiveStateBestEffortAsync()");
        AssertContains(contextText, "public void Dispose()");
        AssertContains(contextText, "internal DiagnosticSessionScenarioPhaseContext CreateScenarioPhaseContext(");
        AssertContains(contextText, "internal DiagnosticSessionCompletionContext CreateCompletionContext(");
        AssertContains(contextText, "GetLastStage = () => RunState.LastStage,");
        AssertContains(contextText, "CommandChannel.Dispose();");
        AssertContains(contextText, "ScenarioCancellationSource.Dispose();");

        AssertContains(executionText, "using var runContext = new DiagnosticSessionRunContext(options, sendCommandAsync, cancellationToken);");
        AssertContains(executionText, "using var sessionLock = AcquireOutputLock(runContext.OutputDirectory);");
        AssertContains(executionText, "await runContext.CaptureInitialSnapshotAsync().ConfigureAwait(false);");
        AssertContains(executionText, "var scenarioPhaseContext = runContext.CreateScenarioPhaseContext(options, cancellationToken);");
        AssertContains(executionText, "runContext.CreateCompletionContext(options, scenarioPhase, stoppedRecordingForVerification, cancellationToken)");
        AssertDoesNotContain(executionText, "new DiagnosticSessionRunState(");
        AssertDoesNotContain(executionText, "new DiagnosticSessionCommandChannel(");
        AssertDoesNotContain(executionText, "new DiagnosticSessionLiveStateWriter(");

        AssertContains(agentMapText, "`tools/Common/DiagnosticSessionRunContext.cs` owns diagnostic-session core mutable run infrastructure");
        AssertContains(agentMapText, "initial snapshot state, live-state handoff, run context disposal, scenario/completion context construction");
        AssertContains(cleanupPlanText, "`DiagnosticSessionRunContext.cs`");
        AssertContains(cleanupPlanText, "owns the cohesive mutable per-run context");
        AssertContains(cleanupPlanText, "initial\nsnapshot state and capture, live-state writer handoff, disposal");
        AssertContains(cleanupPlanText, "scenario/completion context construction");

        return Task.CompletedTask;
    }

    internal static Task DiagnosticSessionRunBootstrap_OwnsNormalizedSessionIdentity()
    {
        var executionText = ReadDiagnosticSessionRunExecutionRootSource();
        var contextText = ReadDiagnosticSessionRunContextSource();
        var bootstrapText = ReadDiagnosticSessionRunContextSource()
            .Replace("\r\n", "\n");

        AssertContains(bootstrapText, "internal readonly record struct DiagnosticSessionRunBootstrap(");
        AssertContains(bootstrapText, "internal static DiagnosticSessionRunBootstrap Create(DiagnosticSessionOptions options)");
        AssertContains(bootstrapText, "var scenario = DiagnosticSessionScenarioCatalog.Normalize(options.Scenario);");
        AssertContains(bootstrapText, "var scenarioPlan = DiagnosticSessionScenarioPlan.From(scenario);");
        AssertContains(bootstrapText, "Math.Clamp(options.DurationSeconds, 0, 24 * 60 * 60)");
        AssertContains(bootstrapText, "Math.Clamp(options.SampleIntervalMs, 100, 60_000)");
        AssertContains(bootstrapText, "DateTimeOffset.UtcNow.ToString(\"yyyyMMdd_HHmmss\", CultureInfo.InvariantCulture)");
        AssertContains(bootstrapText, "Path.Combine(Environment.CurrentDirectory, \"temp\", \"diagnostic-sessions\", sessionId)");
        AssertContains(bootstrapText, "Path.GetFullPath(options.OutputDirectory)");
        AssertContains(bootstrapText, "Directory.CreateDirectory(outputDirectory);");
        AssertContains(bootstrapText, "Environment.ProcessId");
        AssertContains(contextText, "RunBootstrap = DiagnosticSessionRunBootstrap.Create(options);");
        AssertContains(contextText, "ScenarioPlan = RunBootstrap.ScenarioPlan;");
        AssertContains(executionText, "using var sessionLock = AcquireOutputLock(runContext.OutputDirectory);");
        AssertDoesNotContain(executionText, "DiagnosticSessionScenarioCatalog.Normalize(options.Scenario)");
        AssertDoesNotContain(executionText, "Math.Clamp(options.DurationSeconds");
        AssertDoesNotContain(executionText, "Math.Clamp(options.SampleIntervalMs");
        AssertDoesNotContain(executionText, "DateTimeOffset.UtcNow.ToString(\"yyyyMMdd_HHmmss\"");
        AssertDoesNotContain(executionText, "Directory.CreateDirectory(outputDirectory);");
        AssertDoesNotContain(executionText, "var runFlashbackPlayback = scenarioPlan.RunFlashbackPlayback;");

        return Task.CompletedTask;
    }

    internal static Task DiagnosticSessionOutputLock_OwnsExclusiveOutputDirectoryLock()
    {
        var executionText = ReadDiagnosticSessionRunExecutionRootSource();

        AssertContains(executionText, "private static FileStream AcquireOutputLock(string outputDirectory)");
        AssertContains(executionText, "\".sussudio-diag.lock\"");
        AssertContains(executionText, "FileShare.None");
        AssertContains(executionText, "FileOptions.DeleteOnClose");
        AssertContains(executionText, "Another diagnostic session is already running");
        AssertContains(executionText, "using var sessionLock = AcquireOutputLock(runContext.OutputDirectory);");
        AssertDoesNotContain(executionText, "sessionLock.Dispose();");

        return Task.CompletedTask;
    }
}

using System.Threading.Tasks;

static partial class Program
{
    private static Task DiagnosticSessionRunState_OwnsTerminalState()
    {
        var runnerText = ReadDiagnosticSessionRunnerSource();
        var contextText = ReadDiagnosticSessionRunContextSource();
        var stateText = ReadRepoFile("tools/Common/DiagnosticSessionRunState.cs")
            .Replace("\r\n", "\n");

        AssertContains(stateText, "internal sealed class DiagnosticSessionRunState");
        AssertContains(stateText, "internal void SetStage(string stage)");
        AssertContains(stateText, "internal void RecordTerminalException(Exception ex, string stage)");
        AssertContains(stateText, "internal string GetTerminalState()");
        AssertContains(stateText, "internal async Task WriteArtifactBestEffortAsync<T>(");
        AssertContains(contextText, "RunState = new DiagnosticSessionRunState(");
        AssertContains(contextText, "internal void SetStage(string stage)");
        AssertContains(contextText, "internal void RecordTerminalException(Exception ex, string stage)");
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
        var contextText = ReadDiagnosticSessionRunContextSource();
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
        AssertContains(contextText, "_liveStateWriter = new DiagnosticSessionLiveStateWriter(RunBootstrap, RunState, Warnings);");
        AssertContains(contextText, "LivePath = _liveStateWriter.LivePath;");
        AssertContains(contextText, "_liveStateWriter.WriteLiveStateBestEffortAsync(");
        AssertContains(contextText, "_liveStateWriter.WriteSamplingLiveStateBestEffortAsync(");
        AssertDoesNotContain(runnerText, "var livePath = runState.LivePath;");

        return Task.CompletedTask;
    }

    private static Task DiagnosticSessionRunContext_OwnsMutableRunInfrastructure()
    {
        var executionText = ReadDiagnosticSessionRunExecutionRootSource();
        var contextText = ReadDiagnosticSessionRunContextSource();
        var contextRootText = ReadDiagnosticSessionRunContextRootSource();
        var contextPhaseText = ReadDiagnosticSessionRunContextPhaseContextsSource();
        var agentMapText = ReadRepoFile("docs/architecture/AGENT_MAP.md")
            .Replace("\r\n", "\n");
        var cleanupPlanText = ReadRepoFile("docs/architecture/cleanup-plan.md")
            .Replace("\r\n", "\n");

        AssertContains(contextRootText, "internal sealed partial class DiagnosticSessionRunContext : IDisposable");
        AssertContains(contextText, "RunBootstrap = DiagnosticSessionRunBootstrap.Create(options);");
        AssertContains(contextText, "Actions = [];");
        AssertContains(contextText, "Warnings = [];");
        AssertContains(contextText, "Samples = [];");
        AssertContains(contextText, "ScenarioCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(runCancellationToken);");
        AssertContains(contextText, "CommandChannel = new DiagnosticSessionCommandChannel(sendCommandAsync, ScenarioCancellationToken, Warnings);");
        AssertContains(contextText, "InitialSnapshot = unknownSnapshot.Snapshot;");
        AssertContains(contextPhaseText, "internal DiagnosticSessionScenarioPhaseContext CreateScenarioPhaseContext(");
        AssertContains(contextPhaseText, "internal DiagnosticSessionCompletionContext CreateCompletionContext(");
        AssertContains(contextPhaseText, "GetLastStage = () => RunState.LastStage,");
        AssertDoesNotContain(contextRootText, "internal DiagnosticSessionScenarioPhaseContext CreateScenarioPhaseContext(");
        AssertDoesNotContain(contextRootText, "internal DiagnosticSessionCompletionContext CreateCompletionContext(");
        AssertContains(contextText, "CommandChannel.Dispose();");
        AssertContains(contextText, "ScenarioCancellationSource.Dispose();");

        AssertContains(executionText, "using var runContext = new DiagnosticSessionRunContext(options, sendCommandAsync, cancellationToken);");
        AssertContains(executionText, "using var sessionLock = DiagnosticSessionOutputLock.Acquire(runContext.OutputDirectory);");
        AssertContains(executionText, "await runContext.CaptureInitialSnapshotAsync().ConfigureAwait(false);");
        AssertContains(executionText, "var scenarioPhaseContext = runContext.CreateScenarioPhaseContext(options, cancellationToken);");
        AssertContains(executionText, "runContext.CreateCompletionContext(options, scenarioPhase, stoppedRecordingForVerification, cancellationToken)");
        AssertDoesNotContain(executionText, "new DiagnosticSessionRunState(");
        AssertDoesNotContain(executionText, "new DiagnosticSessionCommandChannel(");
        AssertDoesNotContain(executionText, "new DiagnosticSessionLiveStateWriter(");

        AssertContains(agentMapText, "`tools/Common/DiagnosticSessionRunContext.cs` owns diagnostic-session mutable run infrastructure");
        AssertContains(agentMapText, "`tools/Common/DiagnosticSessionRunContext.PhaseContexts.cs` owns diagnostic-session scenario/completion context construction");
        AssertContains(cleanupPlanText, "`DiagnosticSessionRunContext.cs` owns mutable per-run infrastructure");
        AssertContains(cleanupPlanText, "`DiagnosticSessionRunContext.PhaseContexts.cs` owns scenario/completion context construction");

        return Task.CompletedTask;
    }

    private static Task DiagnosticSessionRunBootstrap_OwnsNormalizedSessionIdentity()
    {
        var runnerText = ReadDiagnosticSessionRunnerSource();
        var contextText = ReadDiagnosticSessionRunContextSource();
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
        AssertContains(contextText, "RunBootstrap = DiagnosticSessionRunBootstrap.Create(options);");
        AssertContains(contextText, "ScenarioPlan = RunBootstrap.ScenarioPlan;");
        AssertContains(runnerText, "using var sessionLock = DiagnosticSessionOutputLock.Acquire(runContext.OutputDirectory);");
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
        AssertContains(runnerText, "using var sessionLock = DiagnosticSessionOutputLock.Acquire(runContext.OutputDirectory);");
        AssertDoesNotContain(runnerText, "sessionLock.Dispose();");
        AssertDoesNotContain(runnerText, "var lockPath = Path.Combine(outputDirectory, \".sussudio-diag.lock\")");
        AssertDoesNotContain(runnerText, "FileShare.None");
        AssertDoesNotContain(runnerText, "FileOptions.DeleteOnClose");

        return Task.CompletedTask;
    }
}

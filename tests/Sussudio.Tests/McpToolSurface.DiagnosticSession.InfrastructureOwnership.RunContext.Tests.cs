using System.Threading.Tasks;

static partial class Program
{
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

namespace Sussudio.Tools;

internal sealed partial class DiagnosticSessionRunContext
{
    internal DiagnosticSessionScenarioPhaseContext CreateScenarioPhaseContext(
        DiagnosticSessionOptions options,
        CancellationToken cancellationToken)
        => new DiagnosticSessionScenarioPhaseContext()
        {
            Options = options,
            Scenario = Scenario,
            ScenarioPlan = ScenarioPlan,
            DurationSeconds = DurationSeconds,
            SampleIntervalMs = SampleIntervalMs,
            OutputDirectory = OutputDirectory,
            InitialSnapshot = InitialSnapshot,
            InitialSnapshotKnown = InitialSnapshotKnown,
            Actions = Actions,
            Warnings = Warnings,
            Samples = Samples,
            CommandChannel = CommandChannel,
            ScenarioCancellationSource = ScenarioCancellationSource,
            ScenarioCancellationToken = ScenarioCancellationToken,
            RunCancellationToken = cancellationToken,
            SetStage = SetStage,
            GetLastStage = () => RunState.LastStage,
            RecordTerminalException = RecordTerminalException,
            WriteLiveStateBestEffortAsync = () => WriteLiveStateBestEffortAsync(),
            WriteSamplingLiveStateBestEffortAsync = WriteSamplingLiveStateBestEffortAsync,
        };

    internal DiagnosticSessionCompletionContext CreateCompletionContext(
        DiagnosticSessionOptions options,
        DiagnosticSessionScenarioPhaseResult scenarioPhase,
        bool stoppedRecordingForVerification,
        CancellationToken cancellationToken)
        => new DiagnosticSessionCompletionContext()
        {
            Options = options,
            RunBootstrap = RunBootstrap,
            LivePath = LivePath,
            InitialSnapshot = InitialSnapshot,
            Samples = Samples,
            ScenarioPhase = scenarioPhase,
            StoppedRecordingForVerification = stoppedRecordingForVerification,
            Actions = Actions,
            Warnings = Warnings,
            CommandChannel = CommandChannel,
            RunState = RunState,
            SetStage = SetStage,
            RecordTerminalException = RecordTerminalException,
            RunCancellationToken = cancellationToken,
            WriteLiveStateBestEffortAsync = (completedUtc, terminalState) =>
                WriteLiveStateBestEffortAsync(completedUtc, terminalState),
        };
}

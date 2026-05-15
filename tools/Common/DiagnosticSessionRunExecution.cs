using System.Text.Json;

namespace Sussudio.Tools;

internal static partial class DiagnosticSessionRunExecution
{
    // Scenario names and broad requirements live in DiagnosticSessionScenarios.
    // RunAsync reads like a phase plan: scenario execution, cleanup,
    // verification, post-run snapshots, then summary.

    internal static async Task<DiagnosticSessionResult> RunAsync(
        DiagnosticSessionOptions options,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(sendCommandAsync);

        var runBootstrap = DiagnosticSessionRunBootstrap.Create(options);
        var scenario = runBootstrap.Scenario;
        var scenarioPlan = runBootstrap.ScenarioPlan;
        var durationSeconds = runBootstrap.DurationSeconds;
        var sampleIntervalMs = runBootstrap.SampleIntervalMs;
        var sessionId = runBootstrap.SessionId;
        var outputDirectory = runBootstrap.OutputDirectory;
        var startedUtc = runBootstrap.StartedUtc;
        var runnerProcessId = runBootstrap.RunnerProcessId;

        using var sessionLock = DiagnosticSessionOutputLock.Acquire(outputDirectory);

        var actions = new List<string>();
        var warnings = new List<string>();
        var samples = new List<DiagnosticSessionSample>();
        var runState = new DiagnosticSessionRunState(
            sessionId,
            scenario,
            outputDirectory,
            startedUtc,
            runnerProcessId,
            () => cancellationToken.IsCancellationRequested,
            warnings);
        var livePath = runState.LivePath;
        JsonElement? verification = null;
        var stoppedRecordingForVerification = false;
        using var scenarioCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var scenarioCancellationToken = scenarioCts.Token;
        using var commandChannel = new DiagnosticSessionCommandChannel(sendCommandAsync, scenarioCancellationToken, warnings);
        var scenarioPhase = DiagnosticSessionScenarioPhaseResult.Empty;

        var initialSnapshotResult = DiagnosticSessionInitialSnapshot.CreateUnknown();
        var initialSnapshot = initialSnapshotResult.Snapshot;
        var initialSnapshotKnown = initialSnapshotResult.Known;
        await WriteLiveStateBestEffortAsync().ConfigureAwait(false);
        initialSnapshotResult = await DiagnosticSessionInitialSnapshot.CaptureAsync(
                commandChannel,
                SetStage,
                RecordTerminalException,
                () => WriteLiveStateBestEffortAsync())
            .ConfigureAwait(false);
        initialSnapshot = initialSnapshotResult.Snapshot;
        initialSnapshotKnown = initialSnapshotResult.Known;

        var scenarioPhaseContext = new DiagnosticSessionScenarioPhaseContext
        {
            Options = options,
            Scenario = scenario,
            ScenarioPlan = scenarioPlan,
            DurationSeconds = durationSeconds,
            SampleIntervalMs = sampleIntervalMs,
            OutputDirectory = outputDirectory,
            InitialSnapshot = initialSnapshot,
            InitialSnapshotKnown = initialSnapshotKnown,
            Actions = actions,
            Warnings = warnings,
            Samples = samples,
            CommandChannel = commandChannel,
            ScenarioCancellationSource = scenarioCts,
            ScenarioCancellationToken = scenarioCancellationToken,
            RunCancellationToken = cancellationToken,
            SetStage = SetStage,
            GetLastStage = () => runState.LastStage,
            RecordTerminalException = RecordTerminalException,
            WriteLiveStateBestEffortAsync = () => WriteLiveStateBestEffortAsync(),
            WriteSamplingLiveStateBestEffortAsync = WriteSamplingLiveStateBestEffortAsync,
        };

        try
        {
            scenarioPhase = await RunScenarioPhaseAsync(scenarioPhaseContext).ConfigureAwait(false);
        }
        finally
        {
            var cleanupResult = await DiagnosticSessionCleanupActions.RunAsync(
                    options,
                    initialSnapshot,
                    scenarioPhase.StartedRecording,
                    scenarioPhase.StartedPreview,
                    scenarioPhase.EnabledFlashback,
                    scenarioPhase.DisabledFlashback,
                    scenarioPhase.StartedFlashbackPlayback,
                    actions,
                    commandChannel.SendWithTokenAsync,
                    commandChannel.TryWaitWithTokenAsync,
                    SetStage,
                    RecordTerminalException)
                .ConfigureAwait(false);
            stoppedRecordingForVerification = cleanupResult.StoppedRecordingForVerification;

            await WriteLiveStateBestEffortAsync().ConfigureAwait(false);
        }

        var recordingCheckResult = await DiagnosticSessionRecordingChecks.RunAsync(
                options,
                scenarioPlan,
                scenario,
                outputDirectory,
                initialSnapshot,
                samples,
                scenarioPhase.StartedRecording,
                scenarioPhase.FlashbackRecordingSettingsDeferredPresetState,
                actions,
                warnings,
                commandChannel.SendAsync,
                SetStage,
                RecordTerminalException,
                cancellationToken)
            .ConfigureAwait(false);
        verification = recordingCheckResult.Verification;

        var postRunSnapshots = await DiagnosticSessionPostRunSnapshots.CaptureAsync(
                samples,
                initialSnapshot,
                commandChannel.SendAsync,
                SetStage,
                RecordTerminalException)
            .ConfigureAwait(false);

        var result = await DiagnosticSessionResultBuilder.BuildAndWriteAsync(
                CreateResultBuildRequest(
                    options,
                    runBootstrap,
                    livePath,
                    commandChannel.FailureCount,
                    samples,
                    initialSnapshot,
                    postRunSnapshots,
                    verification,
                    scenarioPhase.PresentMon,
                    scenarioPhase.StartedPreview,
                    scenarioPhase.EnabledFlashback,
                    scenarioPhase.StartedFlashbackPlayback,
                    stoppedRecordingForVerification,
                    actions,
                    warnings),
                runState)
            .ConfigureAwait(false);

        await WriteLiveStateBestEffortAsync(result.CompletedUtc, result.TerminalState).ConfigureAwait(false);
        return result;

        void SetStage(string stage)
        {
            runState.SetStage(stage);
        }

        void RecordTerminalException(Exception ex, string stage)
        {
            runState.RecordTerminalException(ex, stage);
        }

        async Task WriteLiveStateBestEffortAsync(DateTimeOffset? completedUtcOverride = null, string? terminalStateOverride = null)
        {
            await runState.WriteLiveStateBestEffortAsync(
                    samples,
                    initialSnapshot,
                    commandChannel.FailureCount,
                    completedUtcOverride,
                    terminalStateOverride)
                .ConfigureAwait(false);
        }

        async Task WriteSamplingLiveStateBestEffortAsync()
        {
            await runState.WriteSamplingLiveStateBestEffortAsync(
                    samples,
                    initialSnapshot,
                    commandChannel.FailureCount)
                .ConfigureAwait(false);
        }
    }

}

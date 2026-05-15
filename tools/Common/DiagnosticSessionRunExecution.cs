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
        var outputDirectory = runBootstrap.OutputDirectory;

        using var sessionLock = DiagnosticSessionOutputLock.Acquire(outputDirectory);

        var actions = new List<string>();
        var warnings = new List<string>();
        var samples = new List<DiagnosticSessionSample>();
        var runState = new DiagnosticSessionRunState(
            () => cancellationToken.IsCancellationRequested,
            warnings);
        var liveStateWriter = new DiagnosticSessionLiveStateWriter(runBootstrap, runState, warnings);
        var livePath = liveStateWriter.LivePath;
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

        return await RunCompletionPhaseAsync(new DiagnosticSessionCompletionContext
        {
            Options = options,
            RunBootstrap = runBootstrap,
            LivePath = livePath,
            InitialSnapshot = initialSnapshot,
            Samples = samples,
            ScenarioPhase = scenarioPhase,
            StoppedRecordingForVerification = stoppedRecordingForVerification,
            Actions = actions,
            Warnings = warnings,
            CommandChannel = commandChannel,
            RunState = runState,
            SetStage = SetStage,
            RecordTerminalException = RecordTerminalException,
            RunCancellationToken = cancellationToken,
            WriteLiveStateBestEffortAsync = (completedUtc, terminalState) =>
                WriteLiveStateBestEffortAsync(completedUtc, terminalState),
        }).ConfigureAwait(false);

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
            await liveStateWriter.WriteLiveStateBestEffortAsync(
                    samples,
                    initialSnapshot,
                    commandChannel.FailureCount,
                    completedUtcOverride,
                    terminalStateOverride)
                .ConfigureAwait(false);
        }

        async Task WriteSamplingLiveStateBestEffortAsync()
        {
            await liveStateWriter.WriteSamplingLiveStateBestEffortAsync(
                    samples,
                    initialSnapshot,
                    commandChannel.FailureCount)
                .ConfigureAwait(false);
        }
    }

}

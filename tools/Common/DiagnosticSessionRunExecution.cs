using System.Text.Json;

namespace Sussudio.Tools;

internal static class DiagnosticSessionRunExecution
{
    // Scenario names and broad requirements live in DiagnosticSessionScenarioCatalog.
    // RunAsync reads like a phase plan: scenario execution, cleanup,
    // verification, post-run snapshots, then summary.

    internal static async Task<DiagnosticSessionResult> RunAsync(
        DiagnosticSessionOptions options,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(sendCommandAsync);

        using var runContext = new DiagnosticSessionRunContext(options, sendCommandAsync, cancellationToken);
        using var sessionLock = DiagnosticSessionOutputLock.Acquire(runContext.OutputDirectory);

        var stoppedRecordingForVerification = false;
        var scenarioPhase = DiagnosticSessionScenarioPhaseResult.Empty;

        await runContext.CaptureInitialSnapshotAsync().ConfigureAwait(false);
        var scenarioPhaseContext = runContext.CreateScenarioPhaseContext(options, cancellationToken);

        try
        {
            scenarioPhase = await DiagnosticSessionScenarioPhaseRunner.RunAsync(scenarioPhaseContext).ConfigureAwait(false);
        }
        finally
        {
            var cleanupResult = await DiagnosticSessionCleanupActions.RunAsync(
                    options,
                    runContext.InitialSnapshot,
                    scenarioPhase.StartedRecording,
                    scenarioPhase.StartedPreview,
                    scenarioPhase.EnabledFlashback,
                    scenarioPhase.DisabledFlashback,
                    scenarioPhase.StartedFlashbackPlayback,
                    runContext.Actions,
                    runContext.CommandChannel,
                    runContext.CommandChannel.TryWaitWithTokenAsync,
                    runContext.SetStage,
                    runContext.RecordTerminalException)
                .ConfigureAwait(false);
            stoppedRecordingForVerification = cleanupResult.StoppedRecordingForVerification;

            await runContext.WriteLiveStateBestEffortAsync().ConfigureAwait(false);
        }

        return await RunCompletionPhaseAsync(
                runContext.CreateCompletionContext(options, scenarioPhase, stoppedRecordingForVerification, cancellationToken))
            .ConfigureAwait(false);
    }

    private static async Task<DiagnosticSessionResult> RunCompletionPhaseAsync(DiagnosticSessionCompletionContext context)
    {
        var recordingCheckResult = await DiagnosticSessionRecordingChecks.RunAsync(
                context.Options,
                context.RunBootstrap.ScenarioPlan,
                context.RunBootstrap.Scenario,
                context.RunBootstrap.OutputDirectory,
                context.InitialSnapshot,
                context.Samples,
                context.ScenarioPhase.StartedRecording,
                context.ScenarioPhase.FlashbackRecordingSettingsDeferredPresetState,
                context.Actions,
                context.Warnings,
                context.CommandChannel.SendAsync,
                context.SetStage,
                context.RecordTerminalException,
                context.RunCancellationToken)
            .ConfigureAwait(false);
        var verification = recordingCheckResult.Verification;

        var postRunSnapshots = await DiagnosticSessionPostRunSnapshots.CaptureAsync(
                context.Samples,
                context.InitialSnapshot,
                context.CommandChannel.SendAsync,
                context.SetStage,
                context.RecordTerminalException)
            .ConfigureAwait(false);

        var result = await DiagnosticSessionResultBuilder.BuildAndWriteAsync(
                CreateResultBuildRequest(
                    context.Options,
                    context.RunBootstrap,
                    context.LivePath,
                    context.CommandChannel.FailureCount,
                    context.Samples,
                    context.InitialSnapshot,
                    postRunSnapshots,
                    verification,
                    context.ScenarioPhase.PresentMon,
                    context.ScenarioPhase.StartedPreview,
                    context.ScenarioPhase.EnabledFlashback,
                    context.ScenarioPhase.StartedFlashbackPlayback,
                    context.StoppedRecordingForVerification,
                    context.Actions,
                    context.Warnings),
                context.RunState)
            .ConfigureAwait(false);

        await context.WriteLiveStateBestEffortAsync(result.CompletedUtc, result.TerminalState).ConfigureAwait(false);
        return result;
    }

    private static DiagnosticSessionResultBuildRequest CreateResultBuildRequest(
        DiagnosticSessionOptions options,
        DiagnosticSessionRunBootstrap runBootstrap,
        string livePath,
        int commandFailureCount,
        IReadOnlyList<DiagnosticSessionSample> samples,
        JsonElement initialSnapshot,
        DiagnosticSessionPostRunSnapshotResult postRunSnapshots,
        JsonElement? verification,
        PresentMonProbeResult? presentMon,
        bool startedPreview,
        bool enabledFlashback,
        bool startedFlashbackPlayback,
        bool stoppedRecordingForVerification,
        IReadOnlyList<string> actions,
        List<string> warnings)
    {
        return new DiagnosticSessionResultBuildRequest(
            options,
            runBootstrap.ScenarioPlan,
            runBootstrap.SessionId,
            runBootstrap.Scenario,
            runBootstrap.DurationSeconds,
            runBootstrap.SampleIntervalMs,
            runBootstrap.OutputDirectory,
            livePath,
            runBootstrap.StartedUtc,
            runBootstrap.RunnerProcessId,
            commandFailureCount,
            samples,
            initialSnapshot,
            postRunSnapshots.HealthSnapshot,
            postRunSnapshots.Timeline,
            verification,
            presentMon,
            startedPreview,
            enabledFlashback,
            startedFlashbackPlayback,
            stoppedRecordingForVerification,
            actions,
            warnings);
    }
}

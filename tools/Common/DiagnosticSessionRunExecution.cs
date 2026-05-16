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

        using var runContext = new DiagnosticSessionRunContext(options, sendCommandAsync, cancellationToken);
        using var sessionLock = DiagnosticSessionOutputLock.Acquire(runContext.OutputDirectory);

        var stoppedRecordingForVerification = false;
        var scenarioPhase = DiagnosticSessionScenarioPhaseResult.Empty;

        await runContext.CaptureInitialSnapshotAsync().ConfigureAwait(false);
        var scenarioPhaseContext = runContext.CreateScenarioPhaseContext(options, cancellationToken);

        try
        {
            scenarioPhase = await RunScenarioPhaseAsync(scenarioPhaseContext).ConfigureAwait(false);
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
                    runContext.CommandChannel.SendWithTokenAsync,
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

}

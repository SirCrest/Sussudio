namespace Sussudio.Tools;

internal static partial class DiagnosticSessionRunExecution
{
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
}

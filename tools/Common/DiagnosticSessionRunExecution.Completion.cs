using System.Text.Json;

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

internal sealed class DiagnosticSessionCompletionContext
{
    internal required DiagnosticSessionOptions Options { get; init; }

    internal required DiagnosticSessionRunBootstrap RunBootstrap { get; init; }

    internal required string LivePath { get; init; }

    internal required JsonElement InitialSnapshot { get; init; }

    internal required IReadOnlyList<DiagnosticSessionSample> Samples { get; init; }

    internal required DiagnosticSessionScenarioPhaseResult ScenarioPhase { get; init; }

    internal required bool StoppedRecordingForVerification { get; init; }

    internal required List<string> Actions { get; init; }

    internal required List<string> Warnings { get; init; }

    internal required DiagnosticSessionCommandChannel CommandChannel { get; init; }

    internal required DiagnosticSessionRunState RunState { get; init; }

    internal required Action<string> SetStage { get; init; }

    internal required Action<Exception, string> RecordTerminalException { get; init; }

    internal required CancellationToken RunCancellationToken { get; init; }

    internal required Func<DateTimeOffset?, string?, Task> WriteLiveStateBestEffortAsync { get; init; }
}

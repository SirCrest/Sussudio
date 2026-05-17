namespace Sussudio.Tools;

internal static partial class DiagnosticSessionScenarioPhaseRunner
{
    internal static async Task<DiagnosticSessionScenarioPhaseResult> RunAsync(DiagnosticSessionScenarioPhaseContext context)
    {
        var backgroundTasks = new DiagnosticSessionBackgroundTasks();
        var scenarioPhase = new DiagnosticSessionScenarioPhaseState();

        try
        {
            context.SetStage("scenario-setup");
            if (!context.InitialSnapshotKnown && context.Scenario != DiagnosticSessionScenarios.Observe)
            {
                context.CommandChannel.RecordFailure($"initial-snapshot: skipped state-mutating scenario '{context.Scenario}' because the initial app state is unknown");
            }
            else
            {
                var setupResult = await DiagnosticSessionScenarioSetup.RunAsync(
                        context.Scenario,
                        context.ScenarioPlan,
                        context.InitialSnapshot,
                        context.Actions,
                        context.Warnings,
                        context.CommandChannel,
                        context.CommandChannel.TryWaitAsync,
                        context.ScenarioCancellationToken)
                    .ConfigureAwait(false);
                scenarioPhase.StartedPreview = setupResult.StartedPreview;
                scenarioPhase.StartedRecording = setupResult.StartedRecording;
                scenarioPhase.EnabledFlashback = setupResult.EnabledFlashback;
                scenarioPhase.DisabledFlashback = setupResult.DisabledFlashback;

                var scenarioStartup = await DiagnosticSessionScenarioStartup.StartAsync(
                        context.Options,
                        context.ScenarioPlan,
                        context.DurationSeconds,
                        context.OutputDirectory,
                        backgroundTasks,
                        context.Actions,
                        context.Warnings,
                        context.CommandChannel.SendAsync,
                        context.CommandChannel.SendRawWithConnectRetryAsync,
                        context.CommandChannel.SendAsync,
                        context.ScenarioCancellationToken)
                    .ConfigureAwait(false);
                scenarioPhase.StartedFlashbackPlayback = scenarioStartup.StartedFlashbackPlayback;

                await RunSamplingAndCompleteAsync(context, backgroundTasks, scenarioPhase).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            context.RecordTerminalException(ex, context.GetLastStage());
            context.ScenarioCancellationSource.Cancel();
            await DiagnosticSessionScenarioPhaseCompletion.DrainAfterFaultAsync(context, backgroundTasks, scenarioPhase).ConfigureAwait(false);
            await context.WriteLiveStateBestEffortAsync().ConfigureAwait(false);
        }

        return scenarioPhase.ToResult();
    }
}

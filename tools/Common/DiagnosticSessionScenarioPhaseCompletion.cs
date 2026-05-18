namespace Sussudio.Tools;

internal static class DiagnosticSessionScenarioPhaseCompletion
{
    internal static async Task CompleteAfterSamplingAsync(
        DiagnosticSessionScenarioPhaseContext context,
        DiagnosticSessionBackgroundTasks backgroundTasks,
        DiagnosticSessionScenarioPhaseState scenarioPhase)
    {
        scenarioPhase.FlashbackRecordingSettingsDeferredPresetState = await backgroundTasks
            .CompleteRegisteredScenarioWorkAsync(scenarioPhase.FlashbackRecordingSettingsDeferredPresetState)
            .ConfigureAwait(false);

        await DiagnosticSessionFlashbackRejectedExports.RunSelectedRejectedExportScenariosAsync(
                context.ScenarioPlan,
                context.OutputDirectory,
                context.Actions,
                context.Warnings,
                context.CommandChannel.SendAsync,
                context.RunCancellationToken)
            .ConfigureAwait(false);

        scenarioPhase.PresentMon = await backgroundTasks.CompletePresentMonAsync(scenarioPhase.PresentMon, context.Warnings).ConfigureAwait(false);
    }

    internal static async Task DrainAfterFaultAsync(
        DiagnosticSessionScenarioPhaseContext context,
        DiagnosticSessionBackgroundTasks backgroundTasks,
        DiagnosticSessionScenarioPhaseState scenarioPhase)
    {
        var backgroundTaskDrain = await backgroundTasks.ObserveAfterFaultAsync(
                context.Warnings,
                context.SetStage,
                context.RecordTerminalException,
                context.WriteLiveStateBestEffortAsync,
                scenarioPhase.PresentMon,
                scenarioPhase.FlashbackRecordingSettingsDeferredPresetState)
            .ConfigureAwait(false);
        scenarioPhase.PresentMon = backgroundTaskDrain.PresentMon;
        scenarioPhase.FlashbackRecordingSettingsDeferredPresetState = backgroundTaskDrain.RecordingSettingsDeferredPresetState;
    }
}

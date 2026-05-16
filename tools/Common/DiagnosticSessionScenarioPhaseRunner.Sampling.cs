using static Sussudio.Tools.DiagnosticSessionSampler;

namespace Sussudio.Tools;

internal static partial class DiagnosticSessionScenarioPhaseRunner
{
    private static async Task RunSamplingAndCompleteAsync(
        DiagnosticSessionScenarioPhaseContext context,
        DiagnosticSessionBackgroundTasks backgroundTasks,
        DiagnosticSessionScenarioPhaseState scenarioPhase)
    {
        context.SetStage("sampling");
        await context.WriteLiveStateBestEffortAsync().ConfigureAwait(false);
        await SampleLoopAsync(
                context.DurationSeconds,
                context.SampleIntervalMs,
                context.Samples,
                context.CommandChannel.SendAsync,
                context.ScenarioCancellationToken,
                context.WriteSamplingLiveStateBestEffortAsync)
            .ConfigureAwait(false);

        await backgroundTasks.AwaitScenarioTasksAsync().ConfigureAwait(false);
        scenarioPhase.FlashbackRecordingSettingsDeferredPresetState = await backgroundTasks
            .AwaitRecordingSettingsDeferredAsync(scenarioPhase.FlashbackRecordingSettingsDeferredPresetState)
            .ConfigureAwait(false);

        await DiagnosticSessionFlashbackRejectedExports.RunSelectedRejectedExportScenariosAsync(
                context.ScenarioPlan,
                context.OutputDirectory,
                context.Actions,
                context.Warnings,
                context.CommandChannel.SendAsync,
                context.RunCancellationToken)
            .ConfigureAwait(false);

        scenarioPhase.PresentMon = await backgroundTasks.AwaitPresentMonAsync(scenarioPhase.PresentMon, context.Warnings).ConfigureAwait(false);
    }

    private static async Task DrainBackgroundTasksAfterFaultAsync(
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

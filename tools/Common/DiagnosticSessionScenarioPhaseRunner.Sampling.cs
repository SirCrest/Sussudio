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

        await DiagnosticSessionScenarioPhaseCompletion.CompleteAfterSamplingAsync(
                context,
                backgroundTasks,
                scenarioPhase)
            .ConfigureAwait(false);
    }
}

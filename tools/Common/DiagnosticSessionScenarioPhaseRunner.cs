using System.Diagnostics;
using System.Text.Json;
using static Sussudio.Tools.DiagnosticSessionAutomationResponseJson;

namespace Sussudio.Tools;

internal static class DiagnosticSessionScenarioPhaseRunner
{
    internal static async Task<DiagnosticSessionScenarioPhaseResult> RunAsync(DiagnosticSessionScenarioPhaseContext context)
    {
        var backgroundTasks = new DiagnosticSessionBackgroundTasks();
        var scenarioPhase = new DiagnosticSessionScenarioPhaseState();

        try
        {
            context.SetStage("scenario-setup");
            if (!context.InitialSnapshotKnown && context.Scenario != DiagnosticSessionScenarioCatalog.Observe)
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

    private static async Task SampleLoopAsync(
        int durationSeconds,
        int sampleIntervalMs,
        List<DiagnosticSessionSample> samples,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync,
        CancellationToken cancellationToken,
        Func<Task>? sampleCheckpointAsync = null)
    {
        var started = Stopwatch.GetTimestamp();
        var duration = TimeSpan.FromSeconds(durationSeconds);
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var response = await sendCommandAsync("GetSnapshot", null, null).ConfigureAwait(false);
            if (TryGetSnapshot(response, out var snapshot))
            {
                samples.Add(new DiagnosticSessionSample
                {
                    OffsetMs = (long)Stopwatch.GetElapsedTime(started).TotalMilliseconds,
                    TimestampUtc = DateTimeOffset.UtcNow,
                    Snapshot = snapshot.Clone()
                });
                if (sampleCheckpointAsync is not null)
                {
                    await sampleCheckpointAsync().ConfigureAwait(false);
                }
            }

            var elapsed = Stopwatch.GetElapsedTime(started);
            if (elapsed >= duration)
            {
                break;
            }

            var remaining = duration - elapsed;
            var delay = TimeSpan.FromMilliseconds(Math.Min(sampleIntervalMs, Math.Max(1, remaining.TotalMilliseconds)));
            await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
        }
    }
}

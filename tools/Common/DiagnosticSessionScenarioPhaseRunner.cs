using System.Text.Json;
using static Sussudio.Tools.DiagnosticSessionSampler;

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
                        context.CommandChannel.SendAsync,
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
        }
        catch (Exception ex)
        {
            context.RecordTerminalException(ex, context.GetLastStage());
            context.ScenarioCancellationSource.Cancel();
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
            await context.WriteLiveStateBestEffortAsync().ConfigureAwait(false);
        }

        return scenarioPhase.ToResult();
    }
}

internal sealed class DiagnosticSessionScenarioPhaseContext
{
    internal required DiagnosticSessionOptions Options { get; init; }

    internal required string Scenario { get; init; }

    internal required DiagnosticSessionScenarioPlan ScenarioPlan { get; init; }

    internal required int DurationSeconds { get; init; }

    internal required int SampleIntervalMs { get; init; }

    internal required string OutputDirectory { get; init; }

    internal required JsonElement InitialSnapshot { get; init; }

    internal required bool InitialSnapshotKnown { get; init; }

    internal required List<string> Actions { get; init; }

    internal required List<string> Warnings { get; init; }

    internal required List<DiagnosticSessionSample> Samples { get; init; }

    internal required DiagnosticSessionCommandChannel CommandChannel { get; init; }

    internal required CancellationTokenSource ScenarioCancellationSource { get; init; }

    internal required CancellationToken ScenarioCancellationToken { get; init; }

    internal required CancellationToken RunCancellationToken { get; init; }

    internal required Action<string> SetStage { get; init; }

    internal required Func<string> GetLastStage { get; init; }

    internal required Action<Exception, string> RecordTerminalException { get; init; }

    internal required Func<Task> WriteLiveStateBestEffortAsync { get; init; }

    internal required Func<Task> WriteSamplingLiveStateBestEffortAsync { get; init; }
}

internal sealed record DiagnosticSessionScenarioPhaseResult(
    bool StartedPreview,
    bool StartedRecording,
    bool EnabledFlashback,
    bool DisabledFlashback,
    bool StartedFlashbackPlayback,
    PresentMonProbeResult? PresentMon,
    FlashbackRecordingSettingsDeferredPresetState FlashbackRecordingSettingsDeferredPresetState)
{
    internal static readonly DiagnosticSessionScenarioPhaseResult Empty = new(
        StartedPreview: false,
        StartedRecording: false,
        EnabledFlashback: false,
        DisabledFlashback: false,
        StartedFlashbackPlayback: false,
        PresentMon: null,
        FlashbackRecordingSettingsDeferredPresetState: default);
}

internal sealed class DiagnosticSessionScenarioPhaseState
{
    internal bool StartedPreview { get; set; }

    internal bool StartedRecording { get; set; }

    internal bool EnabledFlashback { get; set; }

    internal bool DisabledFlashback { get; set; }

    internal bool StartedFlashbackPlayback { get; set; }

    internal PresentMonProbeResult? PresentMon { get; set; }

    internal FlashbackRecordingSettingsDeferredPresetState FlashbackRecordingSettingsDeferredPresetState { get; set; }

    internal DiagnosticSessionScenarioPhaseResult ToResult()
        => new(
            StartedPreview,
            StartedRecording,
            EnabledFlashback,
            DisabledFlashback,
            StartedFlashbackPlayback,
            PresentMon,
            FlashbackRecordingSettingsDeferredPresetState);
}

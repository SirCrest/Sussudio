using System.Text.Json;
using static Sussudio.Tools.DiagnosticSessionSampler;

namespace Sussudio.Tools;

internal static partial class DiagnosticSessionRunExecution
{
    private static async Task RunScenarioPhaseAsync(
        DiagnosticSessionOptions options,
        string scenario,
        DiagnosticSessionScenarioPlan scenarioPlan,
        int durationSeconds,
        int sampleIntervalMs,
        string outputDirectory,
        JsonElement initialSnapshot,
        bool initialSnapshotKnown,
        List<string> actions,
        List<string> warnings,
        List<DiagnosticSessionSample> samples,
        DiagnosticSessionCommandChannel commandChannel,
        CancellationTokenSource scenarioCts,
        CancellationToken scenarioCancellationToken,
        CancellationToken cancellationToken,
        DiagnosticSessionScenarioPhaseState scenarioPhase,
        Action<string> setStage,
        Func<string> getLastStage,
        Action<Exception, string> recordTerminalException,
        Func<Task> writeLiveStateBestEffortAsync,
        Func<Task> writeSamplingLiveStateBestEffortAsync)
    {
        var backgroundTasks = new DiagnosticSessionBackgroundTasks();

        try
        {
            setStage("scenario-setup");
            if (!initialSnapshotKnown && scenario != DiagnosticSessionScenarios.Observe)
            {
                commandChannel.RecordFailure($"initial-snapshot: skipped state-mutating scenario '{scenario}' because the initial app state is unknown");
            }
            else
            {
                var setupResult = await DiagnosticSessionScenarioSetup.RunAsync(
                        scenario,
                        scenarioPlan,
                        initialSnapshot,
                        actions,
                        warnings,
                        commandChannel.SendAsync,
                        commandChannel.TryWaitAsync,
                        scenarioCancellationToken)
                    .ConfigureAwait(false);
                scenarioPhase.StartedPreview = setupResult.StartedPreview;
                scenarioPhase.StartedRecording = setupResult.StartedRecording;
                scenarioPhase.EnabledFlashback = setupResult.EnabledFlashback;
                scenarioPhase.DisabledFlashback = setupResult.DisabledFlashback;

                var scenarioStartup = await DiagnosticSessionScenarioStartup.StartAsync(
                        options,
                        scenarioPlan,
                        durationSeconds,
                        outputDirectory,
                        backgroundTasks,
                        actions,
                        warnings,
                        commandChannel.SendAsync,
                        commandChannel.SendRawWithConnectRetryAsync,
                        commandChannel.SendAsync,
                        scenarioCancellationToken)
                    .ConfigureAwait(false);
                scenarioPhase.StartedFlashbackPlayback = scenarioStartup.StartedFlashbackPlayback;

                setStage("sampling");
                await writeLiveStateBestEffortAsync().ConfigureAwait(false);
                await SampleLoopAsync(
                        durationSeconds,
                        sampleIntervalMs,
                        samples,
                        commandChannel.SendAsync,
                        scenarioCancellationToken,
                        writeSamplingLiveStateBestEffortAsync)
                    .ConfigureAwait(false);

                await backgroundTasks.AwaitScenarioTasksAsync().ConfigureAwait(false);
                scenarioPhase.FlashbackRecordingSettingsDeferredPresetState = await backgroundTasks
                    .AwaitRecordingSettingsDeferredAsync(scenarioPhase.FlashbackRecordingSettingsDeferredPresetState)
                    .ConfigureAwait(false);

                await DiagnosticSessionFlashbackRejectedExports.RunSelectedRejectedExportScenariosAsync(
                        scenarioPlan,
                        outputDirectory,
                        actions,
                        warnings,
                        commandChannel.SendAsync,
                        cancellationToken)
                    .ConfigureAwait(false);

                scenarioPhase.PresentMon = await backgroundTasks.AwaitPresentMonAsync(scenarioPhase.PresentMon, warnings).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            recordTerminalException(ex, getLastStage());
            scenarioCts.Cancel();
            var backgroundTaskDrain = await backgroundTasks.ObserveAfterFaultAsync(
                    warnings,
                    setStage,
                    recordTerminalException,
                    writeLiveStateBestEffortAsync,
                    scenarioPhase.PresentMon,
                    scenarioPhase.FlashbackRecordingSettingsDeferredPresetState)
                .ConfigureAwait(false);
            scenarioPhase.PresentMon = backgroundTaskDrain.PresentMon;
            scenarioPhase.FlashbackRecordingSettingsDeferredPresetState = backgroundTaskDrain.RecordingSettingsDeferredPresetState;
            await writeLiveStateBestEffortAsync().ConfigureAwait(false);
        }
    }

    private sealed class DiagnosticSessionScenarioPhaseState
    {
        internal bool StartedPreview { get; set; }

        internal bool StartedRecording { get; set; }

        internal bool EnabledFlashback { get; set; }

        internal bool DisabledFlashback { get; set; }

        internal bool StartedFlashbackPlayback { get; set; }

        internal PresentMonProbeResult? PresentMon { get; set; }

        internal FlashbackRecordingSettingsDeferredPresetState FlashbackRecordingSettingsDeferredPresetState { get; set; }
    }
}

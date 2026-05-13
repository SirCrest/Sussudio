using System.Text.Json;
using static Sussudio.Tools.AutomationSnapshotFormatter;
using static Sussudio.Tools.DiagnosticSessionFlashbackCycleScenarios;
using static Sussudio.Tools.DiagnosticSessionFlashbackExportScenarios;
using static Sussudio.Tools.DiagnosticSessionFlashbackLifecycleScenarios;
using static Sussudio.Tools.DiagnosticSessionFlashbackPreviewCycleScenarios;
using static Sussudio.Tools.DiagnosticSessionFlashbackRecordingSettingsScenarios;
using static Sussudio.Tools.DiagnosticSessionFlashbackSegmentPlaybackScenarios;
using static Sussudio.Tools.DiagnosticSessionFlashbackStressScenario;
using static Sussudio.Tools.DiagnosticSessionFlashbackWaits;

namespace Sussudio.Tools;

internal static class DiagnosticSessionScenarioStartup
{
    internal static async Task<DiagnosticSessionScenarioStartupResult> StartAsync(
        DiagnosticSessionOptions options,
        DiagnosticSessionScenarioPlan scenarioPlan,
        int durationSeconds,
        string outputDirectory,
        DiagnosticSessionBackgroundTasks backgroundTasks,
        List<string> actions,
        List<string> warnings,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendAsync,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendRawWithConnectRetryAsync,
        Func<string, Dictionary<string, object?>?, int?, bool, Task<JsonElement>> sendAsyncWithFailurePolicy,
        CancellationToken cancellationToken)
    {
        var startedFlashbackPlayback = false;

        await DiagnosticSessionPresentMonStartup.StartAsync(
                options,
                durationSeconds,
                outputDirectory,
                backgroundTasks,
                actions,
                sendAsync)
            .ConfigureAwait(false);

        if (scenarioPlan.RunFlashbackStress)
        {
            backgroundTasks.AddScenario(
                1,
                "flashback-stress-task",
                RunFlashbackStressAsync(
                    outputDirectory,
                    actions,
                    warnings,
                    sendAsync,
                    cancellationToken));
            actions.Add("flashback stress started");
        }

        if (scenarioPlan.RunFlashbackScrubStress)
        {
            backgroundTasks.AddScenario(
                3,
                "flashback-scrub-stress-task",
                RunFlashbackScrubStressAsync(
                    actions,
                    warnings,
                    sendRawWithConnectRetryAsync,
                    cancellationToken));
            actions.Add("flashback scrub stress started");
        }

        if (scenarioPlan.RunFlashbackRestartCycle)
        {
            backgroundTasks.AddScenario(
                4,
                "flashback-restart-cycle-task",
                RunFlashbackRestartCycleAsync(
                    outputDirectory,
                    actions,
                    warnings,
                    sendAsync,
                    cancellationToken));
            actions.Add("flashback restart cycle started");
        }

        if (scenarioPlan.RunFlashbackEncoderCycle)
        {
            backgroundTasks.AddScenario(
                5,
                "flashback-encoder-cycle-task",
                RunFlashbackEncoderCycleAsync(
                    outputDirectory,
                    actions,
                    warnings,
                    sendAsync,
                    cancellationToken));
            actions.Add("flashback encoder cycle started");
        }

        if (scenarioPlan.RunFlashbackExportPlayback)
        {
            backgroundTasks.AddScenario(
                6,
                "flashback-export-playback-task",
                RunFlashbackExportPlaybackAsync(
                    outputDirectory,
                    actions,
                    warnings,
                    sendAsync,
                    cancellationToken));
            actions.Add("flashback export playback started");
        }

        if (scenarioPlan.RunFlashbackSegmentPlayback)
        {
            backgroundTasks.AddScenario(
                7,
                "flashback-segment-playback-task",
                RunFlashbackSegmentPlaybackAsync(
                    actions,
                    warnings,
                    sendAsync,
                    cancellationToken));
            actions.Add("flashback segment playback started");
        }

        if (scenarioPlan.RunFlashbackRangeExport)
        {
            backgroundTasks.AddScenario(
                8,
                "flashback-range-export-task",
                RunFlashbackRangeExportAsync(
                    outputDirectory,
                    actions,
                    warnings,
                    sendAsync,
                    cancellationToken));
            actions.Add("flashback range export started");
        }

        if (scenarioPlan.RunFlashbackRangeExportAudioSwitch)
        {
            backgroundTasks.AddScenario(
                9,
                "flashback-range-export-audio-switch-task",
                RunFlashbackRangeExportAsync(
                    outputDirectory,
                    actions,
                    warnings,
                    sendRawWithConnectRetryAsync,
                    cancellationToken,
                    scenarioLabel: "flashback range export audio switch",
                    exportFileName: "flashback-range-export-audio-switch.mp4",
                    outPointMs: 15_000,
                    switchAudioDuringExport: true));
            actions.Add("flashback range export audio switch started");
        }

        if (scenarioPlan.RunFlashbackLifecycle)
        {
            backgroundTasks.AddScenario(
                2,
                "flashback-lifecycle-task",
                RunFlashbackLifecycleAsync(
                    actions,
                    warnings,
                    sendAsync,
                    cancellationToken));
            actions.Add("flashback lifecycle started");
        }

        if (scenarioPlan.RunFlashbackExportConcurrent)
        {
            backgroundTasks.AddScenario(
                10,
                "flashback-export-concurrent-task",
                RunFlashbackExportConcurrentAsync(
                    outputDirectory,
                    actions,
                    warnings,
                    sendRawWithConnectRetryAsync,
                    cancellationToken));
            actions.Add("flashback concurrent export started");
        }

        if (scenarioPlan.RunFlashbackDisableDuringExport)
        {
            backgroundTasks.AddScenario(
                11,
                "flashback-disable-during-export-task",
                RunFlashbackDisableDuringExportAsync(
                    outputDirectory,
                    actions,
                    warnings,
                    sendRawWithConnectRetryAsync,
                    cancellationToken));
            actions.Add("flashback disable during export started");
        }

        if (scenarioPlan.RunFlashbackRotatedExport)
        {
            backgroundTasks.AddScenario(
                12,
                "flashback-rotated-export-task",
                RunFlashbackRotatedExportAsync(
                    outputDirectory,
                    actions,
                    warnings,
                    sendAsync,
                    cancellationToken));
            actions.Add("flashback rotated export started");
        }

        if (scenarioPlan.RunFlashbackPreviewCycle)
        {
            backgroundTasks.AddScenario(
                13,
                "flashback-preview-cycle-task",
                RunFlashbackPreviewCycleAsync(
                    outputDirectory,
                    actions,
                    warnings,
                    sendAsync,
                    cancellationToken));
            actions.Add("flashback preview cycle started");
        }

        if (scenarioPlan.RunFlashbackPlaybackPreviewCycle)
        {
            backgroundTasks.AddScenario(
                14,
                "flashback-playback-preview-cycle-task",
                RunFlashbackPlaybackPreviewCycleAsync(
                    outputDirectory,
                    actions,
                    warnings,
                    sendAsync,
                    cancellationToken));
            actions.Add("flashback playback preview cycle started");
        }

        if (scenarioPlan.RunFlashbackRecordingPreviewCycle)
        {
            backgroundTasks.AddScenario(
                15,
                "flashback-recording-preview-cycle-task",
                RunFlashbackRecordingPreviewCycleAsync(
                    actions,
                    warnings,
                    sendAsync,
                    cancellationToken));
            actions.Add("flashback recording preview cycle started");
        }

        if (scenarioPlan.RunFlashbackRecordingSettingsDeferred)
        {
            backgroundTasks.SetRecordingSettingsDeferred(RunFlashbackRecordingSettingsDeferredAsync(
                actions,
                warnings,
                sendAsyncWithFailurePolicy,
                cancellationToken));
            actions.Add("flashback recording settings deferred started");
        }

        if (scenarioPlan.RunFlashbackPlayback)
        {
            if (!await WaitForFlashbackStressBufferReadyAsync(sendAsync, cancellationToken).ConfigureAwait(false))
            {
                warnings.Add("flashback playback: Flashback buffer did not become playback-ready within 30s");
            }

            var playResponse = await sendAsync(
                    "FlashbackAction",
                    new Dictionary<string, object?> { ["action"] = "play", ["positionMs"] = 1000 },
                    null)
                .ConfigureAwait(false);
            if (IsSuccess(playResponse))
            {
                startedFlashbackPlayback = true;
                actions.Add("flashback playback started at 1000ms");
                var playingSnapshot = await WaitForFlashbackPlaybackStateAsync(
                        sendAsync,
                        "Playing",
                        TimeSpan.FromSeconds(5),
                        cancellationToken)
                    .ConfigureAwait(false);
                if (playingSnapshot is null)
                {
                    warnings.Add("flashback playback: playback did not report Playing within 5s");
                }
            }
            else
            {
                warnings.Add($"flashback playback: play command failed - {Get(playResponse, "Message", "unknown error")}");
            }
        }

        return new DiagnosticSessionScenarioStartupResult(startedFlashbackPlayback);
    }
}

internal readonly record struct DiagnosticSessionScenarioStartupResult(bool StartedFlashbackPlayback);

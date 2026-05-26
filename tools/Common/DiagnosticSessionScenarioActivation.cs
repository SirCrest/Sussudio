using System.Text.Json;
using Sussudio.Models;
using static Sussudio.Tools.AutomationSnapshotFormatter;
using static Sussudio.Tools.DiagnosticSessionAutomationResponseJson;
using static Sussudio.Tools.DiagnosticSessionFlashbackRecordingSettingsScenarios;
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
        await StartPresentMonAsync(
                options,
                durationSeconds,
                outputDirectory,
                backgroundTasks,
                actions,
                sendAsync)
            .ConfigureAwait(false);

        RegisterFlashbackScenarioTasks(
            scenarioPlan,
            outputDirectory,
            backgroundTasks,
            actions,
            warnings,
            sendAsync,
            sendRawWithConnectRetryAsync,
            cancellationToken);

        RegisterDeferredFlashbackRecordingSettingsTask(
            scenarioPlan,
            backgroundTasks,
            actions,
            warnings,
            sendAsyncWithFailurePolicy,
            cancellationToken);

        var startedFlashbackPlayback = await TryStartFlashbackPlaybackAsync(
                scenarioPlan,
                actions,
                warnings,
                sendAsync,
                cancellationToken)
            .ConfigureAwait(false);

        return new DiagnosticSessionScenarioStartupResult(startedFlashbackPlayback);
    }

    private static async Task StartPresentMonAsync(
        DiagnosticSessionOptions options,
        int durationSeconds,
        string outputDirectory,
        DiagnosticSessionBackgroundTasks backgroundTasks,
        List<string> actions,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendAsync)
    {
        if (!options.IncludePresentMon)
        {
            return;
        }

        var correlationSnapshotResponse = await sendAsync("GetSnapshot", null, null).ConfigureAwait(false);
        TryGetSnapshot(correlationSnapshotResponse, out var correlationSnapshot);
        backgroundTasks.SetPresentMon(PresentMonProbe.RunAsync(PresentMonProbe.CreateOptions(
            durationSeconds: Math.Max(1, durationSeconds),
            processName: "Sussudio",
            presentMonPath: options.PresentMonPath,
            outputFile: Path.Combine(outputDirectory, "presentmon.csv"),
            keepCsv: true,
            correlation: PresentMonProbe.ReadPreviewCorrelation(correlationSnapshot))));
        actions.Add("presentmon capture started");
    }

    private static void RegisterFlashbackScenarioTasks(
        DiagnosticSessionScenarioPlan scenarioPlan,
        string outputDirectory,
        DiagnosticSessionBackgroundTasks backgroundTasks,
        List<string> actions,
        List<string> warnings,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendAsync,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendRawWithConnectRetryAsync,
        CancellationToken cancellationToken)
    {
        DiagnosticSessionFlashbackStressScenario.RegisterSelectedFlashbackStressScenarioTasks(
            scenarioPlan,
            outputDirectory,
            backgroundTasks,
            actions,
            warnings,
            sendAsync,
            sendRawWithConnectRetryAsync,
            cancellationToken);

        DiagnosticSessionFlashbackCycleScenarios.RegisterSelectedFlashbackCycleScenarioTasks(
            scenarioPlan,
            outputDirectory,
            backgroundTasks,
            actions,
            warnings,
            sendAsync,
            cancellationToken);

        DiagnosticSessionFlashbackSegmentPlaybackScenarios.RegisterSelectedFlashbackSegmentPlaybackScenarioTask(
            scenarioPlan,
            backgroundTasks,
            actions,
            warnings,
            sendAsync,
            cancellationToken);

        DiagnosticSessionFlashbackExportScenarios.RegisterSelectedFlashbackExportScenarioTasks(
            scenarioPlan,
            outputDirectory,
            backgroundTasks,
            actions,
            warnings,
            sendAsync,
            sendRawWithConnectRetryAsync,
            cancellationToken);

        DiagnosticSessionFlashbackLifecycleScenarios.RegisterSelectedFlashbackLifecycleScenarioTask(
            scenarioPlan,
            backgroundTasks,
            actions,
            warnings,
            sendAsync,
            cancellationToken);

        DiagnosticSessionFlashbackPreviewCycleScenarios.RegisterSelectedFlashbackPreviewCycleScenarioTasks(
            scenarioPlan,
            outputDirectory,
            backgroundTasks,
            actions,
            warnings,
            sendAsync,
            cancellationToken);
    }

    private static void RegisterDeferredFlashbackRecordingSettingsTask(
        DiagnosticSessionScenarioPlan scenarioPlan,
        DiagnosticSessionBackgroundTasks backgroundTasks,
        List<string> actions,
        List<string> warnings,
        Func<string, Dictionary<string, object?>?, int?, bool, Task<JsonElement>> sendAsyncWithFailurePolicy,
        CancellationToken cancellationToken)
    {
        if (!scenarioPlan.RunFlashbackRecordingSettingsDeferred)
        {
            return;
        }

        backgroundTasks.SetRecordingSettingsDeferred(RunFlashbackRecordingSettingsDeferredAsync(
            actions,
            warnings,
            sendAsyncWithFailurePolicy,
            cancellationToken));
        actions.Add("flashback recording settings deferred started");
    }

    private static async Task<bool> TryStartFlashbackPlaybackAsync(
        DiagnosticSessionScenarioPlan scenarioPlan,
        List<string> actions,
        List<string> warnings,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendAsync,
        CancellationToken cancellationToken)
    {
        if (!scenarioPlan.RunFlashbackPlayback)
        {
            return false;
        }

        if (!await WaitForFlashbackStressBufferReadyAsync(sendAsync, cancellationToken).ConfigureAwait(false))
        {
            warnings.Add("flashback playback: Flashback buffer did not become playback-ready within 30s");
        }

        var playResponse = await sendAsync(
                "FlashbackAction",
                new Dictionary<string, object?> { ["action"] = "play", ["positionMs"] = 1000 },
                null)
            .ConfigureAwait(false);
        if (!IsSuccess(playResponse))
        {
            warnings.Add($"flashback playback: play command failed - {Get(playResponse, "Message", "unknown error")}");
            return false;
        }

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

        return true;
    }
}

internal readonly record struct DiagnosticSessionScenarioStartupResult(bool StartedFlashbackPlayback);

internal static class DiagnosticSessionScenarioSetup
{
    internal static async Task<DiagnosticSessionScenarioSetupResult> RunAsync(
        string scenario,
        DiagnosticSessionScenarioPlan scenarioPlan,
        JsonElement initialSnapshot,
        List<string> actions,
        List<string> warnings,
        DiagnosticSessionCommandChannel commandChannel,
        Func<string, int, Task> tryWaitAsync,
        CancellationToken cancellationToken)
    {
        var flashbackSetup = await SetupFlashbackStateAsync(
                scenario,
                scenarioPlan,
                initialSnapshot,
                actions,
                commandChannel)
            .ConfigureAwait(false);
        var startedPreview = await StartPreviewIfNeededAsync(
                scenario,
                initialSnapshot,
                actions,
                commandChannel,
                tryWaitAsync)
            .ConfigureAwait(false);
        var startedRecording = await StartRecordingIfNeededAsync(
                scenario,
                scenarioPlan,
                initialSnapshot,
                actions,
                warnings,
                commandChannel,
                tryWaitAsync,
                cancellationToken)
            .ConfigureAwait(false);

        return new DiagnosticSessionScenarioSetupResult(
            startedPreview,
            startedRecording,
            flashbackSetup.EnabledFlashback,
            flashbackSetup.DisabledFlashback);
    }

    private static async Task<DiagnosticSessionFlashbackSetupResult> SetupFlashbackStateAsync(
        string scenario,
        DiagnosticSessionScenarioPlan scenarioPlan,
        JsonElement initialSnapshot,
        List<string> actions,
        DiagnosticSessionCommandChannel commandChannel)
    {
        var enabledFlashback = false;
        var disabledFlashback = false;

        if (DiagnosticSessionScenarioCatalog.NeedsFlashback(scenario) && !GetBool(initialSnapshot, "FlashbackActive"))
        {
            await commandChannel.SendAsync(
                    AutomationCommandKind.SetFlashbackEnabled,
                    new Dictionary<string, object?> { ["enabled"] = true },
                    null)
                .ConfigureAwait(false);
            enabledFlashback = true;
            actions.Add("flashback enabled");
        }

        if (scenarioPlan.RunFlashbackExportRejected && GetBool(initialSnapshot, "FlashbackActive"))
        {
            await commandChannel.SendAsync(
                    AutomationCommandKind.SetFlashbackEnabled,
                    new Dictionary<string, object?> { ["enabled"] = false },
                    null)
                .ConfigureAwait(false);
            disabledFlashback = true;
            actions.Add("flashback disabled for rejected export");
        }

        return new DiagnosticSessionFlashbackSetupResult(enabledFlashback, disabledFlashback);
    }

    private static async Task<bool> StartPreviewIfNeededAsync(
        string scenario,
        JsonElement initialSnapshot,
        List<string> actions,
        DiagnosticSessionCommandChannel commandChannel,
        Func<string, int, Task> tryWaitAsync)
    {
        if (!DiagnosticSessionScenarioCatalog.NeedsPreview(scenario) || GetBool(initialSnapshot, "IsPreviewing"))
        {
            return false;
        }

        await commandChannel.SendAsync(
                AutomationCommandKind.SetPreviewEnabled,
                new Dictionary<string, object?> { ["enabled"] = true },
                null)
            .ConfigureAwait(false);
        actions.Add("preview started");
        await tryWaitAsync("VideoFramesFlowing", 15_000).ConfigureAwait(false);

        return true;
    }

    private static async Task<bool> StartRecordingIfNeededAsync(
        string scenario,
        DiagnosticSessionScenarioPlan scenarioPlan,
        JsonElement initialSnapshot,
        List<string> actions,
        List<string> warnings,
        DiagnosticSessionCommandChannel commandChannel,
        Func<string, int, Task> tryWaitAsync,
        CancellationToken cancellationToken)
    {
        if (!DiagnosticSessionScenarioCatalog.NeedsRecording(scenario) || GetBool(initialSnapshot, "IsRecording"))
        {
            return false;
        }

        Task<JsonElement> SendByNameAsync(string command, Dictionary<string, object?>? payload, int? timeoutMs)
            => commandChannel.SendAsync(command, payload, timeoutMs);

        if (scenarioPlan.RequiresFlashbackRecordingReadiness &&
            !await WaitForFlashbackStressBufferReadyAsync(SendByNameAsync, cancellationToken).ConfigureAwait(false))
        {
            warnings.Add("flashback recording: Flashback buffer did not become recording-ready within 30s");
        }

        await commandChannel.SendAsync(
                AutomationCommandKind.SetRecordingEnabled,
                new Dictionary<string, object?> { ["enabled"] = true },
                null)
            .ConfigureAwait(false);
        actions.Add("recording started");
        await tryWaitAsync("RecordingFileGrowing", 20_000).ConfigureAwait(false);

        return true;
    }

    private readonly record struct DiagnosticSessionFlashbackSetupResult(
        bool EnabledFlashback,
        bool DisabledFlashback);
}

internal readonly record struct DiagnosticSessionScenarioSetupResult(
    bool StartedPreview,
    bool StartedRecording,
    bool EnabledFlashback,
    bool DisabledFlashback);

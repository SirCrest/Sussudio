using System.Text.Json;
using Sussudio.Models;
using static Sussudio.Tools.AutomationSnapshotFormatter;
using static Sussudio.Tools.DiagnosticSessionFlashbackWaits;

namespace Sussudio.Tools;

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

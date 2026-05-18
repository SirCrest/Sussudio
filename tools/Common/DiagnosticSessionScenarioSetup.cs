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
        Task<JsonElement> SendByNameAsync(string command, Dictionary<string, object?>? payload, int? timeoutMs)
            => commandChannel.SendAsync(command, payload, timeoutMs);

        var enabledFlashback = false;
        var disabledFlashback = false;
        var startedPreview = false;
        var startedRecording = false;

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

        if (DiagnosticSessionScenarioCatalog.NeedsPreview(scenario) && !GetBool(initialSnapshot, "IsPreviewing"))
        {
            await commandChannel.SendAsync(
                    AutomationCommandKind.SetPreviewEnabled,
                    new Dictionary<string, object?> { ["enabled"] = true },
                    null)
                .ConfigureAwait(false);
            startedPreview = true;
            actions.Add("preview started");
            await tryWaitAsync("VideoFramesFlowing", 15_000).ConfigureAwait(false);
        }

        if (DiagnosticSessionScenarioCatalog.NeedsRecording(scenario) && !GetBool(initialSnapshot, "IsRecording"))
        {
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
            startedRecording = true;
            actions.Add("recording started");
            await tryWaitAsync("RecordingFileGrowing", 20_000).ConfigureAwait(false);
        }

        return new DiagnosticSessionScenarioSetupResult(
            startedPreview,
            startedRecording,
            enabledFlashback,
            disabledFlashback);
    }
}

internal readonly record struct DiagnosticSessionScenarioSetupResult(
    bool StartedPreview,
    bool StartedRecording,
    bool EnabledFlashback,
    bool DisabledFlashback);

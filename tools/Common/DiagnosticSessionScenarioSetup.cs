using System.Text.Json;
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
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendAsync,
        Func<string, int, Task> tryWaitAsync,
        CancellationToken cancellationToken)
    {
        var enabledFlashback = false;
        var disabledFlashback = false;
        var startedPreview = false;
        var startedRecording = false;

        if (DiagnosticSessionScenarios.NeedsFlashback(scenario) && !GetBool(initialSnapshot, "FlashbackActive"))
        {
            await sendAsync("SetFlashbackEnabled", new Dictionary<string, object?> { ["enabled"] = true }, null).ConfigureAwait(false);
            enabledFlashback = true;
            actions.Add("flashback enabled");
        }

        if (scenarioPlan.RunFlashbackExportRejected && GetBool(initialSnapshot, "FlashbackActive"))
        {
            await sendAsync("SetFlashbackEnabled", new Dictionary<string, object?> { ["enabled"] = false }, null).ConfigureAwait(false);
            disabledFlashback = true;
            actions.Add("flashback disabled for rejected export");
        }

        if (DiagnosticSessionScenarios.NeedsPreview(scenario) && !GetBool(initialSnapshot, "IsPreviewing"))
        {
            await sendAsync("SetPreviewEnabled", new Dictionary<string, object?> { ["enabled"] = true }, null).ConfigureAwait(false);
            startedPreview = true;
            actions.Add("preview started");
            await tryWaitAsync("VideoFramesFlowing", 15_000).ConfigureAwait(false);
        }

        if (DiagnosticSessionScenarios.NeedsRecording(scenario) && !GetBool(initialSnapshot, "IsRecording"))
        {
            if (scenarioPlan.RequiresFlashbackRecordingReadiness &&
                !await WaitForFlashbackStressBufferReadyAsync(sendAsync, cancellationToken).ConfigureAwait(false))
            {
                warnings.Add("flashback recording: Flashback buffer did not become recording-ready within 30s");
            }

            await sendAsync("SetRecordingEnabled", new Dictionary<string, object?> { ["enabled"] = true }, null).ConfigureAwait(false);
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

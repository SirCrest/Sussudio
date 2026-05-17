using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private void UpdateAudioSignalAlerts(AutomationSnapshot snapshot)
    {
        SetAlertState(
            "audio-muted-suspect",
            snapshot.AudioMutedSuspected,
            DiagnosticsSeverity.Warning,
            DiagnosticsCategory.Audio,
            "Audio is enabled but sustained low signal suggests muted or disconnected input.",
            "Audio signal recovered.");
    }
}

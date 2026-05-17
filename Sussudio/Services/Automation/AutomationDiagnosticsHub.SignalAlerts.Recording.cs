using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private void UpdateRecordingGrowthAlerts(AutomationSnapshot snapshot)
    {
        SetAlertState(
            "recording-not-growing",
            snapshot.IsRecording && !snapshot.RecordingFileGrowing,
            DiagnosticsSeverity.Warning,
            DiagnosticsCategory.Recording,
            "Recording is active but output bytes are not increasing.",
            "Recording output growth resumed.");
    }
}

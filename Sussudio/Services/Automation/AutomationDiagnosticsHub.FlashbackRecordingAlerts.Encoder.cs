using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private void UpdateFlashbackEncoderAlerts(AutomationSnapshot snapshot)
    {
        SetAlertState(
            "flashback-encoding-failed",
            snapshot.FlashbackEncodingFailed,
            DiagnosticsSeverity.Error,
            DiagnosticsCategory.Flashback,
            string.IsNullOrWhiteSpace(snapshot.FlashbackEncodingFailureMessage)
                ? $"Flashback encoder failed: type={snapshot.FlashbackEncodingFailureType ?? "Unknown"}."
                : $"Flashback encoder failed: type={snapshot.FlashbackEncodingFailureType ?? "Unknown"} message={snapshot.FlashbackEncodingFailureMessage}.",
            "Flashback encoder failure cleared.",
            throttleMs: 5000);
    }
}

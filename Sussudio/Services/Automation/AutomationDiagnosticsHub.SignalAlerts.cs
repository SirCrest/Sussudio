using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private void UpdateSignalAlerts(
        AutomationSnapshot snapshot,
        bool captureOnePercentLowDegraded,
        bool previewOnePercentLowDegraded,
        bool visualCadenceHealthy,
        string previewSlowFrameDetail)
    {
        UpdatePreviewSignalAlerts(snapshot, previewOnePercentLowDegraded, visualCadenceHealthy, previewSlowFrameDetail);
        UpdateAudioSignalAlerts(snapshot);
        UpdateRecordingGrowthAlerts(snapshot);
        UpdateCaptureSignalAlerts(snapshot, captureOnePercentLowDegraded);
    }
}

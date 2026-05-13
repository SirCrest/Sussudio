using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private void UpdateFlashbackPlaybackAlerts(AutomationSnapshot snapshot, long nowUnixMs)
    {
        UpdateFlashbackPlaybackCommandAlerts(snapshot, nowUnixMs);
        UpdateFlashbackPlaybackPerformanceAlerts(snapshot);
    }
}

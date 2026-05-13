using System;
using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private void UpdateFlashbackAlerts(
        AutomationSnapshot snapshot,
        FlashbackRecordingRecentCounters flashbackRecordingRecent)
    {
        var nowUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        UpdateFlashbackRecordingAlerts(snapshot, flashbackRecordingRecent);
        UpdateFlashbackPlaybackAlerts(snapshot, nowUnixMs);
    }
}

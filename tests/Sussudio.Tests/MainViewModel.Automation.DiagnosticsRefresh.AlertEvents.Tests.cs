static partial class Program
{
    private static void AssertDiagnosticsAlertEventOwnership(AutomationDiagnosticsHubSourceFamily diagnostics)
    {
        AssertContains(diagnostics.AlertsText, "private void UpdateAlerts(AutomationSnapshot snapshot, FlashbackRecordingRecentCounters flashbackRecordingRecent)");
        AssertDoesNotContain(diagnostics.AlertsText, "private void AddEventThrottled(");
        AssertDoesNotContain(diagnostics.AlertsText, "public IReadOnlyList<DiagnosticsEvent> GetRecentEvents");
        AssertContains(diagnostics.EventsText, "private void ObserveFlashbackExportCompletion(AutomationSnapshot snapshot)");
        AssertContains(diagnostics.EventsText, "private void AddEventThrottled(");
        AssertContains(diagnostics.EventsText, "private void SetAlertState(");
        AssertContains(diagnostics.EventsText, "public IReadOnlyList<DiagnosticsEvent> GetRecentEvents");
        AssertContains(diagnostics.AlertsText, "UpdateSignalAlerts(");
        AssertContains(diagnostics.SignalAlertsText, "private void UpdateSignalAlerts(");
        AssertContains(diagnostics.SignalAlertsText, "\"preview-blank\"");
        AssertDoesNotContain(diagnostics.AlertsText, "\"preview-blank\"");
        AssertContains(diagnostics.AlertsText, "UpdateFlashbackAlerts(snapshot, flashbackRecordingRecent);");
        AssertContains(diagnostics.FlashbackAlertsText, "private void UpdateFlashbackAlerts(");
        AssertContains(diagnostics.FlashbackAlertsText, "UpdateFlashbackRecordingAlerts(snapshot, flashbackRecordingRecent);");
        AssertContains(diagnostics.FlashbackAlertsText, "UpdateFlashbackPlaybackAlerts(snapshot, nowUnixMs);");
        AssertContains(diagnostics.FlashbackRecordingAlertsText, "private void UpdateFlashbackRecordingAlerts(");
        AssertContains(diagnostics.FlashbackRecordingAlertsText, "\"flashback-export-stalled\"");
        AssertContains(diagnostics.FlashbackPlaybackAlertsText, "private void UpdateFlashbackPlaybackAlerts(");
        AssertContains(diagnostics.FlashbackPlaybackAlertsText, "UpdateFlashbackPlaybackCommandAlerts(snapshot, nowUnixMs);");
        AssertContains(diagnostics.FlashbackPlaybackAlertsText, "UpdateFlashbackPlaybackPerformanceAlerts(snapshot);");
        AssertContains(diagnostics.FlashbackPlaybackCommandAlertsText, "private void UpdateFlashbackPlaybackCommandAlerts(");
        AssertContains(diagnostics.FlashbackPlaybackCommandAlertsText, "\"flashback-playback-command-stalled\"");
        AssertContains(diagnostics.FlashbackPlaybackPerformanceAlertsText, "private void UpdateFlashbackPlaybackPerformanceAlerts(");
        AssertContains(diagnostics.FlashbackPlaybackPerformanceAlertsText, "\"flashback-playback-slow\"");
        AssertDoesNotContain(diagnostics.FlashbackAlertsText, "\"flashback-export-stalled\"");
        AssertDoesNotContain(diagnostics.FlashbackAlertsText, "\"flashback-playback-slow\"");
        AssertDoesNotContain(diagnostics.FlashbackPlaybackAlertsText, "\"flashback-playback-command-stalled\"");
        AssertDoesNotContain(diagnostics.FlashbackPlaybackAlertsText, "\"flashback-playback-slow\"");
        AssertDoesNotContain(diagnostics.AlertsText, "\"flashback-export-stalled\"");
        AssertDoesNotContain(diagnostics.HubText, "private void UpdateAlerts(AutomationSnapshot snapshot, FlashbackRecordingRecentCounters flashbackRecordingRecent)");
    }
}

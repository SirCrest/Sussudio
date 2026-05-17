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
        AssertContains(diagnostics.SignalAlertsText, "UpdatePreviewSignalAlerts(");
        AssertContains(diagnostics.SignalAlertsText, "UpdateAudioSignalAlerts(snapshot);");
        AssertContains(diagnostics.SignalAlertsText, "UpdateRecordingGrowthAlerts(snapshot);");
        AssertContains(diagnostics.SignalAlertsText, "UpdateCaptureSignalAlerts(");
        AssertContains(diagnostics.SignalAlertsText, "private void UpdatePreviewSignalAlerts(");
        AssertContains(diagnostics.SignalAlertsText, "\"preview-blank\"");
        AssertContains(diagnostics.SignalAlertsText, "\"preview-stall\"");
        AssertContains(diagnostics.SignalAlertsText, "\"preview-startup-timeout\"");
        AssertContains(diagnostics.SignalAlertsText, "\"preview-startup-failed\"");
        AssertContains(diagnostics.SignalAlertsText, "\"preview-cadence-slow\"");
        AssertContains(diagnostics.SignalAlertsText, "\"preview-display-low-1pct\"");
        AssertContains(diagnostics.SignalAlertsText, "private void UpdateAudioSignalAlerts(");
        AssertContains(diagnostics.SignalAlertsText, "\"audio-muted-suspect\"");
        AssertContains(diagnostics.SignalAlertsText, "private void UpdateRecordingGrowthAlerts(");
        AssertContains(diagnostics.SignalAlertsText, "\"recording-not-growing\"");
        AssertContains(diagnostics.SignalAlertsText, "private void UpdateCaptureSignalAlerts(");
        AssertContains(diagnostics.SignalAlertsText, "\"capture-cadence-drop\"");
        AssertContains(diagnostics.SignalAlertsText, "\"capture-cadence-low-1pct\"");
        AssertDoesNotContain(diagnostics.AlertsText, "\"preview-blank\"");
        AssertContains(diagnostics.AlertsText, "var nowUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();");
        AssertContains(diagnostics.AlertsText, "UpdateFlashbackRecordingAlerts(snapshot, flashbackRecordingRecent);");
        AssertContains(diagnostics.AlertsText, "UpdateFlashbackPlaybackAlerts(snapshot, nowUnixMs);");
        AssertContains(diagnostics.FlashbackRecordingAlertsText, "private void UpdateFlashbackRecordingAlerts(");
        AssertContains(diagnostics.FlashbackRecordingAlertsText, "UpdateFlashbackExportAlerts(");
        AssertContains(diagnostics.FlashbackRecordingAlertsText, "UpdateFlashbackStorageAlerts(snapshot);");
        AssertContains(diagnostics.FlashbackRecordingAlertsText, "UpdateFlashbackEncoderAlerts(snapshot);");
        AssertContains(diagnostics.FlashbackRecordingAlertsText, "UpdateFlashbackRecordingDegradationAlert(");
        AssertContains(diagnostics.FlashbackRecordingAlertsText, "private void UpdateFlashbackExportAlerts(");
        AssertContains(diagnostics.FlashbackRecordingAlertsText, "\"flashback-export-stalled\"");
        AssertContains(diagnostics.FlashbackRecordingAlertsText, "\"flashback-export-rotation-gap\"");
        AssertContains(diagnostics.FlashbackRecordingAlertsText, "private void UpdateFlashbackStorageAlerts(");
        AssertContains(diagnostics.FlashbackRecordingAlertsText, "\"flashback-temp-cache-pressure\"");
        AssertContains(diagnostics.FlashbackRecordingAlertsText, "private void UpdateFlashbackEncoderAlerts(");
        AssertContains(diagnostics.FlashbackRecordingAlertsText, "\"flashback-encoding-failed\"");
        AssertContains(diagnostics.FlashbackRecordingAlertsText, "private void UpdateFlashbackRecordingDegradationAlert(");
        AssertContains(diagnostics.FlashbackRecordingAlertsText, "\"flashback-recording-degraded\"");
        AssertContains(diagnostics.FlashbackPlaybackAlertsText, "private void UpdateFlashbackPlaybackAlerts(");
        AssertContains(diagnostics.FlashbackPlaybackAlertsText, "UpdateFlashbackPlaybackCommandAlerts(snapshot, nowUnixMs);");
        AssertContains(diagnostics.FlashbackPlaybackAlertsText, "UpdateFlashbackPlaybackPerformanceAlerts(snapshot);");
        AssertContains(diagnostics.FlashbackPlaybackAlertsText, "private void UpdateFlashbackPlaybackCommandAlerts(");
        AssertContains(diagnostics.FlashbackPlaybackAlertsText, "\"flashback-playback-command-stalled\"");
        AssertContains(diagnostics.FlashbackPlaybackAlertsText, "private void UpdateFlashbackPlaybackPerformanceAlerts(");
        AssertContains(diagnostics.FlashbackPlaybackAlertsText, "UpdateFlashbackPlaybackCadenceAlerts(");
        AssertContains(diagnostics.FlashbackPlaybackAlertsText, "UpdateFlashbackPlaybackAudioAlerts(snapshot, playbackActive);");
        AssertContains(diagnostics.FlashbackPlaybackAlertsText, "UpdateFlashbackPlaybackSubmitFailureAlert(snapshot);");
        AssertContains(diagnostics.FlashbackPlaybackPerformanceAlertsCadenceText, "private void UpdateFlashbackPlaybackCadenceAlerts(");
        AssertContains(diagnostics.FlashbackPlaybackPerformanceAlertsCadenceText, "\"flashback-playback-slow\"");
        AssertContains(diagnostics.FlashbackPlaybackAlertsText, "private void UpdateFlashbackPlaybackAudioAlerts(");
        AssertContains(diagnostics.FlashbackPlaybackAlertsText, "\"flashback-playback-audio-master-fallback\"");
        AssertContains(diagnostics.FlashbackPlaybackAlertsText, "private void UpdateFlashbackPlaybackSubmitFailureAlert(");
        AssertContains(diagnostics.FlashbackPlaybackAlertsText, "\"flashback-playback-submit-failures\"");
        AssertDoesNotContain(diagnostics.FlashbackPlaybackAlertsText, "\"flashback-playback-slow\"");
        AssertDoesNotContain(diagnostics.AlertsText, "\"flashback-export-stalled\"");
        AssertDoesNotContain(diagnostics.HubText, "private void UpdateAlerts(AutomationSnapshot snapshot, FlashbackRecordingRecentCounters flashbackRecordingRecent)");
    }
}

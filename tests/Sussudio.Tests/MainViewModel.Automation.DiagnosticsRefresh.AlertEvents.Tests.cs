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
        AssertContains(diagnostics.AlertsText, "private void UpdateSignalAlerts(");
        AssertContains(diagnostics.AlertsText, "UpdatePreviewSignalAlerts(");
        AssertContains(diagnostics.AlertsText, "UpdateAudioSignalAlerts(snapshot);");
        AssertContains(diagnostics.AlertsText, "UpdateRecordingGrowthAlerts(snapshot);");
        AssertContains(diagnostics.AlertsText, "UpdateCaptureSignalAlerts(");
        AssertContains(diagnostics.AlertsText, "private void UpdatePreviewSignalAlerts(");
        AssertContains(diagnostics.AlertsText, "\"preview-blank\"");
        AssertContains(diagnostics.AlertsText, "\"preview-stall\"");
        AssertContains(diagnostics.AlertsText, "\"preview-startup-timeout\"");
        AssertContains(diagnostics.AlertsText, "\"preview-startup-failed\"");
        AssertContains(diagnostics.AlertsText, "\"preview-cadence-slow\"");
        AssertContains(diagnostics.AlertsText, "\"preview-display-low-1pct\"");
        AssertContains(diagnostics.AlertsText, "private void UpdateCaptureSignalAlerts(");
        AssertContains(diagnostics.AlertsText, "\"capture-cadence-drop\"");
        AssertContains(diagnostics.AlertsText, "\"capture-cadence-low-1pct\"");
        AssertContains(diagnostics.AlertsText, "private void UpdateAudioSignalAlerts(");
        AssertContains(diagnostics.AlertsText, "\"audio-muted-suspect\"");
        AssertContains(diagnostics.AlertsText, "private void UpdateRecordingGrowthAlerts(");
        AssertContains(diagnostics.AlertsText, "\"recording-not-growing\"");
        AssertContains(diagnostics.AlertsText, "var nowUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();");
        AssertContains(diagnostics.AlertsText, "UpdateFlashbackRecordingAlerts(snapshot, flashbackRecordingRecent);");
        AssertContains(diagnostics.AlertsText, "UpdateFlashbackPlaybackAlerts(snapshot, nowUnixMs);");
        AssertContains(diagnostics.AlertsText, "private void UpdateFlashbackRecordingAlerts(");
        AssertContains(diagnostics.AlertsText, "UpdateFlashbackExportAlerts(");
        AssertContains(diagnostics.AlertsText, "UpdateFlashbackStorageAlerts(snapshot);");
        AssertContains(diagnostics.AlertsText, "UpdateFlashbackEncoderAlerts(snapshot);");
        AssertContains(diagnostics.AlertsText, "UpdateFlashbackRecordingDegradationAlert(");
        AssertContains(diagnostics.AlertsText, "\"flashback-recording-degraded\"");
        AssertContains(diagnostics.AlertsText, "private void UpdateFlashbackExportAlerts(");
        AssertContains(diagnostics.AlertsText, "\"flashback-export-stalled\"");
        AssertContains(diagnostics.AlertsText, "\"flashback-export-rotation-gap\"");
        AssertContains(diagnostics.AlertsText, "private void UpdateFlashbackStorageAlerts(");
        AssertContains(diagnostics.AlertsText, "\"flashback-temp-cache-pressure\"");
        AssertContains(diagnostics.AlertsText, "private void UpdateFlashbackEncoderAlerts(");
        AssertContains(diagnostics.AlertsText, "\"flashback-encoding-failed\"");
        AssertContains(diagnostics.AlertsText, "private void UpdateFlashbackRecordingDegradationAlert(");
        AssertContains(diagnostics.AlertsText, "private void UpdateFlashbackPlaybackAlerts(");
        AssertContains(diagnostics.AlertsText, "UpdateFlashbackPlaybackCommandAlerts(snapshot, nowUnixMs);");
        AssertContains(diagnostics.AlertsText, "UpdateFlashbackPlaybackPerformanceAlerts(snapshot);");
        AssertContains(diagnostics.AlertsText, "\"flashback-playback-audio-master-fallback\"");
        AssertContains(diagnostics.AlertsText, "private void UpdateFlashbackPlaybackPerformanceAlerts(");
        AssertContains(diagnostics.AlertsText, "UpdateFlashbackPlaybackCadenceAlerts(");
        AssertContains(diagnostics.AlertsText, "UpdateFlashbackPlaybackAudioAlerts(snapshot, playbackActive);");
        AssertContains(diagnostics.AlertsText, "UpdateFlashbackPlaybackSubmitFailureAlert(snapshot);");
        AssertContains(diagnostics.AlertsText, "private void UpdateFlashbackPlaybackSubmitFailureAlert(");
        AssertContains(diagnostics.AlertsText, "\"flashback-playback-submit-failures\"");
        AssertContains(diagnostics.AlertsText, "private void UpdateFlashbackPlaybackAudioAlerts(");
        AssertContains(diagnostics.AlertsText, "\"flashback-playback-audio-queue-backlog\"");
        AssertContains(diagnostics.AlertsText, "private void UpdateFlashbackPlaybackCommandAlerts(");
        AssertContains(diagnostics.AlertsText, "\"flashback-playback-command-stalled\"");
        AssertContains(diagnostics.AlertsText, "private void UpdateFlashbackPlaybackCadenceAlerts(");
        AssertContains(diagnostics.AlertsText, "\"flashback-playback-slow\"");
        AssertEqual(
            false,
            System.IO.File.Exists(System.IO.Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Automation", "AutomationDiagnosticsHub.FlashbackRecordingAlerts.cs")),
            "Flashback recording alert rules folded into the main alerts owner");
        AssertDoesNotContain(diagnostics.HubText, "private void UpdateAlerts(AutomationSnapshot snapshot, FlashbackRecordingRecentCounters flashbackRecordingRecent)");
    }
}

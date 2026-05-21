static partial class Program
{
    private static void AssertDiagnosticsRefreshPipelineOwnership(AutomationDiagnosticsHubSourceFamily diagnostics, string dispatcherText)
    {
        AssertContains(diagnostics.SnapshotsText, "var snapshot = BuildAutomationSnapshot(");
        AssertDoesNotContain(diagnostics.SnapshotsText, "new AutomationSnapshot");
        AssertContains(diagnostics.SnapshotsText, "AppendPerformanceTimelineEntry(snapshot);");
        AssertContains(diagnostics.SnapshotStateText, "private AudioSignalState UpdateAudioSignalState(");
        AssertContains(diagnostics.SnapshotStateText, "private bool UpdateRecordingFileGrowthState(");
        AssertContains(diagnostics.SnapshotStateText, "private readonly record struct AudioSignalState(");
        AssertContains(diagnostics.SnapshotsText, "UpdateAudioSignalState(viewModelSnapshot, nowTick);");
        AssertContains(diagnostics.SnapshotsText, "UpdateRecordingFileGrowthState(");
        AssertDoesNotContain(diagnostics.SnapshotsText, "var audioSignalPresent = viewModelSnapshot.AudioPeak >= AudioSignalThreshold;");
        AssertContains(diagnostics.OutputFilesText, "private LastOutputProbe ProbeLastOutput(");
        AssertContains(diagnostics.OutputFilesText, "private readonly record struct LastOutputProbe(");
        AssertContains(diagnostics.ProcessMetricsText, "private ProcessResourceSnapshot CaptureProcessResourceSnapshot()");
        AssertContains(diagnostics.ProcessMetricsText, "private double CalculateProcessCpuPercent(double processCpuTotalMs)");
        AssertContains(diagnostics.ProcessMetricsText, "private readonly record struct ProcessResourceSnapshot(");
        AssertContains(diagnostics.TimelineText, "public IReadOnlyList<PerformanceTimelineEntry> GetPerformanceTimeline");
        AssertContains(diagnostics.TimelineText, "private void AppendPerformanceTimelineEntry(AutomationSnapshot snapshot)");
        AssertContains(diagnostics.TimelineText, "BuildPerformanceTimelineEntry(snapshot)");
        AssertDoesNotContain(diagnostics.TimelineText, "new PerformanceTimelineEntry\n        {");
        AssertContains(diagnostics.TimelineProjectionText, "private static PerformanceTimelineEntry BuildPerformanceTimelineEntry(AutomationSnapshot snapshot)");
        AssertContains(diagnostics.TimelineProjectionText, "var flashbackPlayback = BuildPerformanceTimelineFlashbackPlaybackProjection(snapshot);");
        AssertContains(diagnostics.TimelineProjectionText, "FlashbackPlaybackCommandsEnqueued = flashbackPlayback.CommandsEnqueued");
        AssertContains(diagnostics.TimelineProjectionFlashbackPlaybackText, "private static PerformanceTimelineFlashbackPlaybackProjection BuildPerformanceTimelineFlashbackPlaybackProjection(");
        AssertContains(diagnostics.TimelineProjectionFlashbackPlaybackText, "CommandsEnqueued: snapshot.FlashbackPlaybackCommandsEnqueued");
        AssertDoesNotContain(diagnostics.HubText, "private async Task<AutomationSnapshot> RefreshSnapshotCoreAsync");
        AssertContains(diagnostics.SnapshotsText, "var shouldAutoVerify = ShouldAutoVerifySnapshot(snapshot);");
        AssertContains(diagnostics.SnapshotsText, "var lastVerification = CaptureLastVerificationForSnapshot(recordingStarted);");
        AssertDoesNotContain(diagnostics.SnapshotsText, "_lastVerification = null;");
        AssertContains(diagnostics.SnapshotsText, "ScheduleAutoVerificationIfNeeded(shouldAutoVerify);");
        AssertDoesNotContain(diagnostics.SnapshotsText, "Automatic recording verification started.");
        AssertDoesNotContain(diagnostics.SnapshotsText, "new FileInfo(lastOutputPath).Length");
        AssertDoesNotContain(diagnostics.SnapshotsText, "GC.GetGCMemoryInfo()");
        AssertDoesNotContain(diagnostics.HubText, "private double CalculateProcessCpuPercent(double processCpuTotalMs)");
        AssertContains(diagnostics.SourceFamilyText, "private readonly SemaphoreSlim _refreshGate = new(1, 1);");
        AssertContains(diagnostics.SourceFamilyText, "await _refreshGate.WaitAsync(cancellationToken).ConfigureAwait(false);");
        AssertContains(diagnostics.SourceFamilyText, "return await RefreshSnapshotCoreAsync(cancellationToken).ConfigureAwait(false);");
        AssertContains(dispatcherText, "case AutomationCommandKind.GetSnapshot:\n                return await ExecuteGetSnapshotCommandAsync(correlationId, cancellationToken).ConfigureAwait(false);");
        AssertContains(dispatcherText, "private async Task<AutomationCommandResponse> ExecuteGetSnapshotCommandAsync(");
        AssertContains(dispatcherText, "var snapshot = await _diagnosticsHub.RefreshSnapshotNowAsync(cancellationToken).ConfigureAwait(false);");
        AssertContains(dispatcherText, "private async Task<AutomationCommandResponse> ExecuteAssertSnapshotCommandAsync(");
        AssertContains(dispatcherText, "var snapshot = await _diagnosticsHub.RefreshSnapshotNowAsync(cancellationToken).ConfigureAwait(false);\n        var assertions = ParseAssertions(payload);");
        AssertContains(dispatcherText, "private async Task<(bool Met, AutomationSnapshot Snapshot)> WaitForConditionAsync");
        AssertContains(dispatcherText, "return (true, snapshot);");
        AssertContains(dispatcherText, "snapshot: snapshot");
        AssertContains(dispatcherText, "AutomationSnapshot? snapshot = null");
        AssertContains(dispatcherText, "Snapshot = includeSnapshot ? snapshot ?? _diagnosticsHub.GetLatestSnapshot() : null");
    }
}

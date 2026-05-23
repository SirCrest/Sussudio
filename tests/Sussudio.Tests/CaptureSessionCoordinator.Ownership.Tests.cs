using System.Threading.Tasks;

static partial class Program
{
    internal static Task CaptureSessionCoordinator_CommandFacadeLivesInFocusedPartial()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Capture/CaptureSessionCoordinator.cs")
            .Replace("\r\n", "\n");
        var commandsText = ReadRepoFile("Sussudio/Services/Capture/CaptureSessionCoordinator.Commands.cs")
            .Replace("\r\n", "\n");

        AssertContains(commandsText, "public Task InitializeAsync(CaptureDevice device, CaptureSettings settings, CancellationToken cancellationToken = default)");
        AssertContains(commandsText, "public Task StartVideoPreviewAsync(CaptureSettings settings, CancellationToken cancellationToken = default)");
        AssertContains(commandsText, "public Task StopVideoPreviewWithTeardownAsync(CancellationToken cancellationToken = default)");
        AssertContains(commandsText, "public Task StartRecordingAsync(CaptureSettings settings, CancellationToken cancellationToken = default)");
        AssertContains(commandsText, "public Task StopRecordingForEmergencyAsync(CancellationToken cancellationToken = default)");
        AssertContains(commandsText, "_captureService.StopRecordingAsync(emergency: true, ct)");
        AssertContains(commandsText, "public Task UpdateAudioMonitoringAsync(bool enabled, CancellationToken cancellationToken = default)");
        AssertOccursBefore(commandsText, "await _captureService.StartAudioPreviewAsync(ct).ConfigureAwait(false);", "_captureService.SetMonitoringMuted(false);");
        AssertOccursBefore(commandsText, "_captureService.SetMonitoringMuted(true);", "await _captureService.StopAudioPreviewAsync(ct).ConfigureAwait(false);");
        AssertContains(commandsText, "internal void SetPreviewVolume(double volume)");
        AssertContains(commandsText, "ThrowIfDisposed();");
        AssertContains(commandsText, "public Task UpdateAudioInputAsync(string? audioDeviceId, string? audioDeviceName, CancellationToken cancellationToken = default)");
        AssertContains(commandsText, "public Task UpdateMicrophoneMonitorAsync(bool enabled, string? micDeviceId, string? micDeviceName, CancellationToken cancellationToken = default)");
        AssertContains(commandsText, "public Task CleanupAsync(CancellationToken cancellationToken = default)");

        AssertDoesNotContain(rootText, "public Task InitializeAsync(");
        AssertDoesNotContain(rootText, "public Task UpdateAudioMonitoringAsync(");
        AssertDoesNotContain(rootText, "StopRecordingForEmergencyAsync");
        AssertDoesNotContain(rootText, "SetMonitoringMuted(");
        AssertDoesNotContain(rootText, "public Task CleanupAsync(");

        return Task.CompletedTask;
    }

    internal static Task CaptureSessionCoordinator_FlashbackFacadeLivesInFocusedPartials()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Capture/CaptureSessionCoordinator.cs")
            .Replace("\r\n", "\n");
        var flashbackText = ReadRepoFile("Sussudio/Services/Capture/CaptureSessionCoordinator.Flashback.cs")
            .Replace("\r\n", "\n");
        var flashbackStatusText = flashbackText;
        var flashbackPlaybackText = ReadRepoFile("Sussudio/Services/Capture/CaptureSessionCoordinator.Flashback.Playback.cs")
            .Replace("\r\n", "\n");
        var flashbackExportText = flashbackText;
        var flashbackGuardsText = flashbackText;
        var agentMapText = ReadRepoFile("docs/architecture/AGENT_MAP.md")
            .Replace("\r\n", "\n");
        var cleanupPlanText = ReadRepoFile("docs/architecture/cleanup-plan.md")
            .Replace("\r\n", "\n");

        AssertContains(flashbackText, "public Task RestartFlashbackAsync(CancellationToken cancellationToken = default)");
        AssertContains(flashbackText, "public Task CycleFlashbackEncoderSettingsAsync(");
        AssertContains(flashbackText, "public Task SetFlashbackEnabledAsync(bool enabled, CancellationToken cancellationToken = default)");
        AssertContains(flashbackText, "public Task UpdateFlashbackSettingsAsync(int bufferMinutes, bool gpuDecode, CancellationToken cancellationToken = default)");
        AssertDoesNotContain(flashbackText, "internal bool FlashbackBeginScrub(TimeSpan position)");

        AssertContains(flashbackStatusText, "internal bool IsFlashbackActive => _captureService.IsFlashbackActive;");
        AssertContains(flashbackStatusText, "internal FlashbackBufferStatus GetFlashbackBufferStatus()");
        AssertContains(flashbackStatusText, "internal FlashbackPlaybackSnapshot GetFlashbackPlaybackSnapshot()");
        AssertContains(flashbackStatusText, "Interlocked.Read(ref _lastFlashbackCommandRejectionUtcUnixMs)");
        AssertContains(flashbackPlaybackText, "internal bool FlashbackBeginScrub(TimeSpan position)");
        AssertContains(flashbackPlaybackText, "internal bool FlashbackClearInOutPoints()");
        AssertContains(flashbackPlaybackText, "TryGetActiveFlashback(nameof(FlashbackGoLive), out var controller)");
        AssertContains(flashbackExportText, "internal Task<FinalizeResult> ExportFlashbackRangeAsync(");
        AssertContains(flashbackExportText, "internal Task<FinalizeResult> ExportFlashbackLastNSecondsAsync(");
        AssertContains(flashbackExportText, "internal IReadOnlyList<FlashbackSegmentInfo> GetFlashbackSegments()");
        AssertContains(flashbackGuardsText, "private bool TryGetActiveFlashback(");
        AssertContains(flashbackGuardsText, "Logger.Log($\"FLASHBACK_COORD_COMMAND_REJECTED command={command} reason={reason}\");");

        AssertDoesNotContain(rootText, "RestartFlashbackAsync");
        AssertDoesNotContain(rootText, "FlashbackBeginScrub");
        AssertContains(agentMapText, "`CaptureSessionCoordinator.Flashback.cs`");
        AssertContains(agentMapText, "`CaptureSessionCoordinator.Flashback.Playback.cs`");
        AssertContains(agentMapText, "read-only Flashback status");
        AssertContains(agentMapText, "active playback-controller guard");
        AssertContains(cleanupPlanText, "`CaptureSessionCoordinator.Flashback.cs`");
        AssertContains(cleanupPlanText, "`CaptureSessionCoordinator.Flashback.Playback.cs`");
        AssertContains(cleanupPlanText, "read-only Flashback status");
        AssertContains(cleanupPlanText, "active playback-controller guard");

        return Task.CompletedTask;
    }

    internal static Task CaptureSessionCoordinator_QueueWorkerLivesInFocusedPartial()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Capture/CaptureSessionCoordinator.cs")
            .Replace("\r\n", "\n");
        var queueText = ReadRepoFile("Sussudio/Services/Capture/CaptureSessionCoordinator.Queue.cs")
            .Replace("\r\n", "\n");
        var queueExecutionText = ReadRepoFile("Sussudio/Services/Capture/CaptureSessionCoordinator.QueueExecution.cs")
            .Replace("\r\n", "\n");

        AssertContains(queueText, "private sealed class CoordinatorWorkItem");
        AssertContains(queueText, "private Task EnqueueAsync(");
        AssertContains(queueText, "private void ThrowIfDisposed()");
        AssertContains(queueExecutionText, "private async Task ProcessQueueAsync()");
        AssertContains(queueExecutionText, "private void FailPendingCommands(Exception ex)");
        AssertContains(queueExecutionText, "private void DecrementPendingCommands(string operation)");
        AssertContains(queueExecutionText, "Logger.LogEvent(\"CAP-COORD-START\"");
        AssertContains(queueExecutionText, "Logger.LogEvent(\"CAP-COORD-DONE\"");
        AssertDoesNotContain(queueText, "private async Task ProcessQueueAsync()");
        AssertDoesNotContain(queueText, "private void FailPendingCommands(Exception ex)");
        AssertDoesNotContain(rootText, "private sealed class CoordinatorWorkItem");
        AssertDoesNotContain(rootText, "private async Task ProcessQueueAsync()");
        AssertDoesNotContain(rootText, "private void FailPendingCommands(Exception ex)");

        return Task.CompletedTask;
    }

    internal static Task CaptureSessionCoordinator_SnapshotProjectionLivesInFocusedPartial()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Capture/CaptureSessionCoordinator.cs")
            .Replace("\r\n", "\n");
        var snapshotText = ReadRepoFile("Sussudio/Services/Capture/CaptureSessionCoordinator.Snapshot.cs")
            .Replace("\r\n", "\n");

        AssertContains(snapshotText, "public CaptureSessionSnapshot Snapshot");
        AssertContains(snapshotText, "private void UpdateSnapshot(CaptureCommand command, CaptureCommandOutcome outcome, string? error)");
        AssertContains(snapshotText, "private void TrackPendingCommandEnqueued(DateTimeOffset enqueuedAtUtc)");
        AssertContains(snapshotText, "private void RemoveOldestPendingCommand()");
        AssertContains(snapshotText, "private void RecordCommandQueueLatency(DateTimeOffset enqueuedAtUtc)");
        AssertContains(snapshotText, "OldestPendingCommandAgeMs = oldestPendingCommandAgeMs,");
        AssertDoesNotContain(rootText, "public CaptureSessionSnapshot Snapshot");
        AssertDoesNotContain(rootText, "private void UpdateSnapshot(CaptureCommand command, CaptureCommandOutcome outcome, string? error)");
        AssertDoesNotContain(rootText, "private void RecordCommandQueueLatency(DateTimeOffset enqueuedAtUtc)");

        return Task.CompletedTask;
    }

    internal static Task CaptureSessionCoordinator_DisposalLivesInFocusedPartial()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Capture/CaptureSessionCoordinator.cs")
            .Replace("\r\n", "\n");
        var disposalText = ReadRepoFile("Sussudio/Services/Capture/CaptureSessionCoordinator.Disposal.cs")
            .Replace("\r\n", "\n");

        AssertContains(disposalText, "private const int DefaultDisposeDrainTimeoutMs = 15_000;");
        AssertContains(disposalText, "public void Dispose()");
        AssertContains(disposalText, "public async ValueTask DisposeAsync()");
        AssertContains(disposalText, "private async ValueTask CoreDisposeAsync()");
        AssertContains(disposalText, "private async Task WaitForWorkerCancellationAsync()");
        AssertContains(disposalText, "private void DisposeWorkerCancellationWhenSafe()");
        AssertContains(disposalText, "private void CancelWorkerBestEffort()");
        AssertDoesNotContain(rootText, "private async ValueTask CoreDisposeAsync()");
        AssertDoesNotContain(rootText, "SUSSUDIO_COORDINATOR_DISPOSE_TIMEOUT_MS");
        AssertDoesNotContain(rootText, "private void DisposeWorkerCancellationWhenSafe()");

        return Task.CompletedTask;
    }
}

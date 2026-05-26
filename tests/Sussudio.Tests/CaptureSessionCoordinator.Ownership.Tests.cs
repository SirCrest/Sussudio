using System.IO;
using System.Threading.Tasks;

static partial class Program
{
    internal static Task CaptureSessionCoordinator_CommandFacadeLivesInFocusedPartial()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Capture/CaptureSessionCoordinator.cs")
            .Replace("\r\n", "\n");

        AssertContains(rootText, "public Task InitializeAsync(CaptureDevice device, CaptureSettings settings, CancellationToken cancellationToken = default)");
        AssertContains(rootText, "public Task StartVideoPreviewAsync(CaptureSettings settings, CancellationToken cancellationToken = default)");
        AssertContains(rootText, "public Task StopVideoPreviewWithTeardownAsync(CancellationToken cancellationToken = default)");
        AssertContains(rootText, "public Task StartRecordingAsync(CaptureSettings settings, CancellationToken cancellationToken = default)");
        AssertContains(rootText, "public Task StopRecordingForEmergencyAsync(CancellationToken cancellationToken = default)");
        AssertContains(rootText, "_captureService.StopRecordingAsync(emergency: true, ct)");
        AssertContains(rootText, "public Task UpdateAudioMonitoringAsync(bool enabled, CancellationToken cancellationToken = default)");
        AssertOccursBefore(rootText, "await _captureService.StartAudioPreviewAsync(ct).ConfigureAwait(false);", "_captureService.SetMonitoringMuted(false);");
        AssertOccursBefore(rootText, "_captureService.SetMonitoringMuted(true);", "await _captureService.StopAudioPreviewAsync(ct).ConfigureAwait(false);");
        AssertContains(rootText, "internal void SetPreviewVolume(double volume)");
        AssertContains(rootText, "ThrowIfDisposed();");
        AssertContains(rootText, "public Task UpdateAudioInputAsync(string? audioDeviceId, string? audioDeviceName, CancellationToken cancellationToken = default)");
        AssertContains(rootText, "public Task UpdateMicrophoneMonitorAsync(bool enabled, string? micDeviceId, string? micDeviceName, CancellationToken cancellationToken = default)");
        AssertContains(rootText, "public Task CleanupAsync(CancellationToken cancellationToken = default)");

        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureSessionCoordinator.Commands.cs")),
            "CaptureSessionCoordinator command facade folded into the coordinator root");

        return Task.CompletedTask;
    }

    internal static Task CaptureSessionCoordinator_FlashbackFacadeLivesInFocusedPartials()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Capture/CaptureSessionCoordinator.cs")
            .Replace("\r\n", "\n");
        var flashbackText = ReadRepoFile("Sussudio/Services/Capture/CaptureSessionCoordinator.Flashback.cs")
            .Replace("\r\n", "\n");
        var flashbackStatusText = flashbackText;
        var flashbackPlaybackText = flashbackText;
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

        AssertContains(flashbackStatusText, "internal bool IsFlashbackActive => _captureService.IsFlashbackActive;");
        AssertContains(flashbackStatusText, "internal FlashbackBufferStatus GetFlashbackBufferStatus()");
        AssertContains(flashbackStatusText, "internal FlashbackPlaybackSnapshot GetFlashbackPlaybackSnapshot()");
        AssertContains(flashbackStatusText, "Interlocked.Read(ref _lastFlashbackCommandRejectionUtcUnixMs)");
        AssertContains(flashbackPlaybackText, "internal bool FlashbackBeginScrub(TimeSpan position)");
        AssertContains(flashbackPlaybackText, "internal bool FlashbackClearInOutPoints()");
        AssertContains(flashbackPlaybackText, "TryGetActiveFlashback(nameof(FlashbackGoLive), out var controller)");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureSessionCoordinator.Flashback.Playback.cs")),
            "CaptureSessionCoordinator Flashback playback adapters folded into the Flashback coordinator facade");
        AssertContains(flashbackExportText, "internal Task<FinalizeResult> ExportFlashbackRangeAsync(");
        AssertContains(flashbackExportText, "internal Task<FinalizeResult> ExportFlashbackLastNSecondsAsync(");
        AssertContains(flashbackExportText, "internal IReadOnlyList<FlashbackSegmentInfo> GetFlashbackSegments()");
        AssertContains(flashbackGuardsText, "private bool TryGetActiveFlashback(");
        AssertContains(flashbackGuardsText, "Logger.Log($\"FLASHBACK_COORD_COMMAND_REJECTED command={command} reason={reason}\");");

        AssertDoesNotContain(rootText, "RestartFlashbackAsync");
        AssertDoesNotContain(rootText, "FlashbackBeginScrub");
        AssertContains(agentMapText, "`CaptureSessionCoordinator.Flashback.cs`");
        AssertContains(agentMapText, "read-only Flashback status");
        AssertContains(agentMapText, "active playback-controller guard");
        AssertContains(cleanupPlanText, "`CaptureSessionCoordinator.Flashback.cs`");
        AssertContains(cleanupPlanText, "read-only Flashback status");
        AssertContains(cleanupPlanText, "active playback-controller guard");

        return Task.CompletedTask;
    }

    internal static Task CaptureSessionCoordinator_CoalescesFlashbackEncoderCycles()
    {
        var coordinatorText = ReadCaptureSessionCoordinatorSource();
        var cycleMethod = ExtractTextBetween(
            coordinatorText,
            "public Task CycleFlashbackEncoderSettingsAsync",
            "public Task SetFlashbackEnabledAsync");
        var queueProcessor = ExtractTextBetween(
            coordinatorText,
            "private async Task ProcessQueueAsync",
            "private void FailPendingCommands(Exception ex)");

        AssertContains(coordinatorText, "_latestFlashbackEncoderCycleGeneration");
        AssertContains(coordinatorText, "_commandsCoalesced");
        AssertContains(cycleMethod, "coalesceLatest: true");
        AssertContains(queueProcessor, "Volatile.Read(ref _latestFlashbackEncoderCycleGeneration)");
        AssertContains(queueProcessor, "CaptureCommandOutcome.Coalesced");
        AssertContains(queueProcessor, "CAP-COORD-SKIP");
        AssertContains(coordinatorText, "CAP-COORD-ENQUEUE-FAIL");

        return Task.CompletedTask;
    }

    internal static Task CaptureSessionCoordinator_DisposalAccounting_ClassifiesCanceledQueuedCommands()
    {
        var coordinatorText = ReadCaptureSessionCoordinatorSource();
        var failPending = ExtractTextBetween(
            coordinatorText,
            "private void FailPendingCommands(Exception ex)",
            "    private void DecrementPendingCommands");

        AssertContains(failPending, "if (pending.Completion.Task.IsCanceled)\n            {\n                Interlocked.Increment(ref _commandsCanceled);");
        AssertContains(failPending, "else if (pending.Completion.TrySetException(ex))\n            {\n                Interlocked.Increment(ref _commandsFailed);");
        AssertContains(failPending, "DecrementPendingCommands(\"fail_pending\");");
        AssertContains(coordinatorText, "DecrementPendingCommands(\"enqueue_failed\");");
        AssertContains(coordinatorText, "DecrementPendingCommands(\"process_complete\");");
        AssertContains(coordinatorText, "private void DecrementPendingCommands(string operation)");
        AssertContains(coordinatorText, "CAPTURE_COORD_PENDING_UNDERFLOW");
        AssertContains(coordinatorText, "throw new ObjectDisposedException(nameof(CaptureSessionCoordinator));");
        AssertDoesNotContain(failPending, "pending.Completion.TrySetException(ex);\n            Interlocked.Increment(ref _commandsFailed);\n            Interlocked.Decrement(ref _pendingCommands);");

        return Task.CompletedTask;
    }

    internal static Task CaptureSessionCoordinator_FlashbackMutationsPropagateRequestCancellation()
    {
        var coordinatorText = ReadCaptureSessionCoordinatorSource();
        var restartNoSettings = ExtractTextBetween(
            coordinatorText,
            "public Task RestartFlashbackAsync(CancellationToken cancellationToken = default)",
            "public Task RestartFlashbackAsync(CaptureSettings settings");
        var restartWithSettings = ExtractTextBetween(
            coordinatorText,
            "public Task RestartFlashbackAsync(CaptureSettings settings",
            "public Task UpdateRecordingFormatAsync");
        var setFlashbackEnabled = ExtractTextBetween(
            coordinatorText,
            "public Task SetFlashbackEnabledAsync",
            "public Task UpdateFlashbackSettingsAsync");

        AssertContains(restartNoSettings, "propagateCancellationToOperation: true");
        AssertContains(restartWithSettings, "propagateCancellationToOperation: true");
        AssertContains(restartWithSettings, "ct => _captureService.RestartFlashbackAsync(settings, ct)");
        AssertDoesNotContain(restartWithSettings, "_captureService.UpdateEncodingSettings(settings)");
        AssertContains(setFlashbackEnabled, "propagateCancellationToOperation: true");

        return Task.CompletedTask;
    }

    internal static Task CaptureSessionCoordinator_CommittedStopsDoNotPropagateRequestCancellation()
    {
        var coordinatorText = ReadCaptureSessionCoordinatorSource();
        var stopVideo = ExtractTextBetween(
            coordinatorText,
            "public Task StopVideoPreviewAsync(CancellationToken cancellationToken = default)",
            "public Task StopVideoPreviewWithTeardownAsync");
        var stopVideoTeardown = ExtractTextBetween(
            coordinatorText,
            "public Task StopVideoPreviewWithTeardownAsync",
            "public Task StartRecordingAsync");
        var stopRecording = ExtractTextBetween(
            coordinatorText,
            "public Task StopRecordingAsync",
            "public Task StartAudioPreviewAsync");
        var cycleFlashbackEncoder = ExtractTextBetween(
            coordinatorText,
            "public Task CycleFlashbackEncoderSettingsAsync",
            "public Task SetFlashbackEnabledAsync");

        AssertDoesNotContain(stopVideo, "propagateCancellationToOperation: true");
        AssertDoesNotContain(stopVideoTeardown, "propagateCancellationToOperation: true");
        AssertDoesNotContain(stopRecording, "propagateCancellationToOperation: true");
        AssertDoesNotContain(cycleFlashbackEncoder, "propagateCancellationToOperation: true");
        AssertContains(cycleFlashbackEncoder, "coalesceLatest: true");

        return Task.CompletedTask;
    }

    internal static Task CaptureSessionCoordinator_LogsInactiveFlashbackCommandRejections()
    {
        var coordinatorText = ReadCaptureSessionCoordinatorSource();

        AssertContains(coordinatorText, "TryGetActiveFlashback(nameof(FlashbackBeginScrub), out var controller)");
        AssertContains(coordinatorText, "TryGetActiveFlashback(nameof(FlashbackUpdateScrub), out var controller)");
        AssertContains(coordinatorText, "TryGetActiveFlashback(nameof(FlashbackEndScrub), out var controller)");
        AssertContains(coordinatorText, "TryGetActiveFlashback(nameof(FlashbackGoLive), out var controller)");
        AssertContains(coordinatorText, "TryGetActiveFlashback(nameof(FlashbackClearInOutPoints), out var controller)");
        AssertContains(coordinatorText, "bool ThreadAlive,\n    int PendingCommands,\n    string LastCommandFailure,\n    long LastCommandFailureUtcUnixMs");
        AssertContains(coordinatorText, "controller.PlaybackThreadAlive,\n                controller.PendingCommands,\n                controller.LastCommandFailure,\n                controller.LastCommandFailureUtcUnixMs");
        AssertContains(coordinatorText, "public static FlashbackPlaybackSnapshot Inactive(");
        AssertContains(coordinatorText, "private long _lastFlashbackCommandRejectionUtcUnixMs;");
        AssertContains(coordinatorText, "private string _lastFlashbackCommandRejection = string.Empty;");
        AssertContains(coordinatorText, "FlashbackPlaybackSnapshot.Inactive(\n                _lastFlashbackCommandRejection,\n                Interlocked.Read(ref _lastFlashbackCommandRejectionUtcUnixMs))");
        AssertContains(coordinatorText, "private bool TryGetActiveFlashback(\n        string command,");
        AssertContains(coordinatorText, "var reason = controller == null\n            ? \"missing_controller\"\n            : controller.IsDisposed\n                ? \"disposed\"\n                : !controller.IsInitialized\n                ? \"not_initialized\"\n                : $\"state_{controller.State}\";");
        AssertContains(coordinatorText, "_lastFlashbackCommandRejection = $\"{reason}:{command}\";");
        AssertContains(coordinatorText, "Interlocked.Exchange(ref _lastFlashbackCommandRejectionUtcUnixMs, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());");
        AssertContains(coordinatorText, "Logger.Log($\"FLASHBACK_COORD_COMMAND_REJECTED command={command} reason={reason}\");");

        return Task.CompletedTask;
    }

    internal static Task CaptureSessionCoordinator_QueueWorkerLivesInFocusedPartial()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Capture/CaptureSessionCoordinator.cs")
            .Replace("\r\n", "\n");
        var queueText = rootText;
        var queueExecutionText = queueText;

        AssertContains(queueText, "private sealed class CoordinatorWorkItem");
        AssertContains(queueText, "private Task EnqueueAsync(");
        AssertContains(queueText, "private void ThrowIfDisposed()");
        AssertContains(queueExecutionText, "private async Task ProcessQueueAsync()");
        AssertContains(queueExecutionText, "private void FailPendingCommands(Exception ex)");
        AssertContains(queueExecutionText, "private void DecrementPendingCommands(string operation)");
        AssertContains(queueExecutionText, "Logger.LogEvent(\"CAP-COORD-START\"");
        AssertContains(queueExecutionText, "Logger.LogEvent(\"CAP-COORD-DONE\"");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureSessionCoordinator.Queue.cs")),
            "CaptureSessionCoordinator queue worker folded into the coordinator root");

        return Task.CompletedTask;
    }

    internal static Task CaptureSessionCoordinator_SnapshotProjectionLivesInFocusedPartial()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Capture/CaptureSessionCoordinator.cs")
            .Replace("\r\n", "\n");

        AssertContains(rootText, "public CaptureSessionSnapshot Snapshot");
        AssertContains(rootText, "private void UpdateSnapshot(CaptureCommand command, CaptureCommandOutcome outcome, string? error)");
        AssertContains(rootText, "private void TrackPendingCommandEnqueued(DateTimeOffset enqueuedAtUtc)");
        AssertContains(rootText, "private void RemoveOldestPendingCommand()");
        AssertContains(rootText, "private void RecordCommandQueueLatency(DateTimeOffset enqueuedAtUtc)");
        AssertContains(rootText, "OldestPendingCommandAgeMs = oldestPendingCommandAgeMs,");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureSessionCoordinator.Snapshot.cs")),
            "CaptureSessionCoordinator snapshot projection folded into the coordinator root");

        return Task.CompletedTask;
    }

    internal static Task CaptureSessionCoordinator_DisposalLivesInCoordinatorRoot()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Capture/CaptureSessionCoordinator.cs")
            .Replace("\r\n", "\n");

        AssertContains(rootText, "private const int DefaultDisposeDrainTimeoutMs = 15_000;");
        AssertContains(rootText, "public void Dispose()");
        AssertContains(rootText, "public async ValueTask DisposeAsync()");
        AssertContains(rootText, "private async ValueTask CoreDisposeAsync()");
        AssertContains(rootText, "private async Task WaitForWorkerCancellationAsync()");
        AssertContains(rootText, "private void DisposeWorkerCancellationWhenSafe()");
        AssertContains(rootText, "private void CancelWorkerBestEffort()");
        AssertContains(rootText, "SUSSUDIO_COORDINATOR_DISPOSE_TIMEOUT_MS");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureSessionCoordinator.Disposal.cs")),
            "CaptureSessionCoordinator disposal lifecycle folded into the coordinator root");

        return Task.CompletedTask;
    }
}

using System.Threading.Tasks;

static partial class Program
{
    private static Task CaptureService_RecordingLifecycleLivesInFocusedPartial()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.cs")
            .Replace("\r\n", "\n");
        var lifecycleText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingLifecycle.cs")
            .Replace("\r\n", "\n");
        var startStateText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingStartState.cs")
            .Replace("\r\n", "\n");
        var flashbackStartText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingStartFlashback.cs")
            .Replace("\r\n", "\n");
        var libAvStartText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingStartLibAv.cs")
            .Replace("\r\n", "\n");
        var stopLifecycleText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingStopLifecycle.cs")
            .Replace("\r\n", "\n");

        AssertDoesNotContain(rootText, "public Task StartRecordingAsync(");
        AssertDoesNotContain(rootText, "public Task StopRecordingAsync(");
        AssertContains(lifecycleText, "public Task StartRecordingAsync(");
        AssertContains(lifecycleText, "RunTransitionAsync(CaptureSessionState.Recording");
        AssertContains(lifecycleText, "var rollback = new RecordingStartRollbackState();");
        AssertContains(lifecycleText, "await DisposeUnusableFlashbackRecordingBackendAsync(transitionToken)");
        AssertContains(lifecycleText, "await StartFlashbackRecordingAsync(settings, transitionToken, rollback)");
        AssertContains(lifecycleText, "await StartLibAvRecordingAsync(settings, transitionToken, rollback)");
        AssertContains(lifecycleText, "await RollbackRecordingStartAsync(rollback, ex).ConfigureAwait(false);");
        AssertContains(lifecycleText, "await RollbackRecordingStartAsync(rollback, ex).ConfigureAwait(false);\n                throw;");
        AssertDoesNotContain(lifecycleText, "CAPTURE_RECORDING_START_FAIL");
        AssertDoesNotContain(lifecycleText, "FLASHBACK_RECORDING_START_ROLLBACK_WARN");
        AssertDoesNotContain(lifecycleText, "FLASHBACK_UNIFIED_RECORDING_START");
        AssertDoesNotContain(lifecycleText, "HDR_NEGOTIATION");
        AssertContains(startStateText, "private sealed class RecordingStartRollbackState");
        AssertContains(startStateText, "public RecordingContext? RecordingContext { get; set; }");
        AssertContains(startStateText, "public FlashbackEncoderSink? FlashbackRecordingStartedSink { get; set; }");
        AssertContains(flashbackStartText, "private async Task DisposeUnusableFlashbackRecordingBackendAsync(");
        AssertContains(flashbackStartText, "private async Task StartFlashbackRecordingAsync(");
        AssertContains(flashbackStartText, "FLASHBACK_UNIFIED_RECORDING_START");
        AssertContains(flashbackStartText, "FLASHBACK_RECORDING_TOPOLOGY_MISMATCH_REJECT");
        AssertContains(flashbackStartText, "WaitForForceRotateIdle(TimeSpan.FromSeconds(10))");
        AssertContains(flashbackStartText, "_unifiedVideoCapture?.BeginFlashbackRecordingAccounting();");
        AssertDoesNotContain(flashbackStartText, "HDR_NEGOTIATION");
        AssertContains(libAvStartText, "private async Task StartLibAvRecordingAsync(");
        AssertContains(libAvStartText, "await RefreshSourceTelemetryAsync(transitionToken)");
        AssertContains(libAvStartText, "HDR_NEGOTIATION");
        AssertContains(libAvStartText, "await rollback.RecordingSink.StartAsync(rollback.RecordingContext, transitionToken)");
        AssertDoesNotContain(libAvStartText, "FLASHBACK_UNIFIED_RECORDING_START");
        AssertDoesNotContain(lifecycleText, "public Task StopRecordingAsync(");
        AssertDoesNotContain(lifecycleText, "internal Task StopRecordingAsync(bool emergency");
        AssertContains(stopLifecycleText, "public Task StopRecordingAsync(");
        AssertContains(stopLifecycleText, "internal Task StopRecordingAsync(bool emergency");
        AssertContains(stopLifecycleText, "await StopAndDisposeRecordingBackendAsync(\"Stopped\", emergency, transitionToken)");

        return Task.CompletedTask;
    }

    private static Task CaptureService_RecordingRollbackLivesInFocusedPartial()
    {
        var finalizationText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingFinalizeRecord.cs")
            .Replace("\r\n", "\n");
        var lifecycleText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingLifecycle.cs")
            .Replace("\r\n", "\n");
        var rollbackText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingRollback.cs")
            .Replace("\r\n", "\n");

        AssertDoesNotContain(lifecycleText, "Recording start rollback cleanup failed");
        AssertContains(rollbackText, "private async Task RollbackRecordingStartAsync(");
        AssertContains(rollbackText, "CAPTURE_RECORDING_START_FAIL");
        AssertContains(rollbackText, "RecordLastRecordingFailure(ex);");
        AssertContains(rollbackText, "CancelRecordingStartRollback(\"start_recording_failed\")");
        AssertContains(rollbackText, "FLASHBACK_RECORDING_START_ROLLBACK_WARN");
        AssertContains(rollbackText, "ReleaseSemaphoreBestEffort(_flashbackBackendLeaseLock, \"flashback_recording_start_fail\")");
        AssertContains(rollbackText, "Recording start rollback cleanup failed");
        AssertContains(rollbackText, "Transient recording backend cleanup failed during start rollback");
        AssertContains(rollbackText, "_recordingStopwatch.Reset();");
        AssertDoesNotContain(finalizationText, "private async Task DisposeTransientRecordingBackendAsync(");
        AssertContains(rollbackText, "private async Task DisposeTransientRecordingBackendAsync(");
        AssertContains(rollbackText, "Transient recording sink stop failed during rollback");
        AssertContains(rollbackText, "Transient unified video dispose failed during rollback");
        AssertContains(rollbackText, "ScheduleDeferredUnifiedVideoCaptureCleanup(");
        AssertContains(rollbackText, "reason: \"recording_start_rollback\"");
        AssertOccursBefore(rollbackText, "CAPTURE_RECORDING_START_FAIL", "RecordLastRecordingFailure(ex);");
        AssertOccursBefore(rollbackText, "RecordLastRecordingFailure(ex);", "await _artifactManager.RollbackAsync(rollback.RecordingContext)");
        AssertOccursBefore(rollbackText, "rollback.FlashbackRecordingBackendLeaseHeld = false;", "ReleaseSemaphoreBestEffort(_flashbackBackendLeaseLock, \"flashback_recording_start_fail\")");
        AssertOccursBefore(rollbackText, "await DisposeMicrophoneCaptureAsync().ConfigureAwait(false);", "await DisposeTransientRecordingBackendAsync(");

        return Task.CompletedTask;
    }

    private static Task CaptureService_RecordingOutcomeStateLivesInFocusedPartial()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.cs")
            .Replace("\r\n", "\n");
        var lifecycleText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingLifecycle.cs")
            .Replace("\r\n", "\n");
        var finalizationText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingFinalizeRecord.cs")
            .Replace("\r\n", "\n");
        var outcomeStateText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingOutcomeState.cs")
            .Replace("\r\n", "\n");

        AssertDoesNotContain(rootText, "private string? _lastOutputPath;");
        AssertDoesNotContain(rootText, "private string _lastFinalizeStatus = \"None\";");
        AssertDoesNotContain(rootText, "private DateTimeOffset? _lastFinalizeUtc;");
        AssertDoesNotContain(rootText, "private IReadOnlyList<string> _lastPreservedArtifacts = Array.Empty<string>();");
        AssertContains(outcomeStateText, "private void PublishRecordingStartedOutcome(string finalOutputPath)");
        AssertContains(outcomeStateText, "private string? _lastOutputPath;");
        AssertContains(outcomeStateText, "private string _lastFinalizeStatus = \"None\";");
        AssertContains(outcomeStateText, "private DateTimeOffset? _lastFinalizeUtc;");
        AssertContains(outcomeStateText, "private IReadOnlyList<string> _lastPreservedArtifacts = Array.Empty<string>();");
        AssertContains(outcomeStateText, "_lastOutputPath = finalOutputPath;");
        AssertContains(outcomeStateText, "_lastFinalizeStatus = \"Recording\";");
        AssertContains(outcomeStateText, "_lastFinalizeUtc = null;");
        AssertContains(outcomeStateText, "_lastPreservedArtifacts = Array.Empty<string>();");
        AssertContains(outcomeStateText, "private void PublishRecordingFinalizedOutcome(FinalizeResult result, bool updateOutputPath)");
        AssertContains(outcomeStateText, "if (updateOutputPath)");
        AssertContains(outcomeStateText, "_lastOutputPath = result.OutputPath;");
        AssertContains(outcomeStateText, "_lastFinalizeStatus = result.StatusMessage;");
        AssertContains(outcomeStateText, "_lastFinalizeUtc = DateTimeOffset.UtcNow;");
        AssertContains(outcomeStateText, "_lastPreservedArtifacts = result.PreservedArtifacts;");

        var flashbackStartText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingStartFlashback.cs")
            .Replace("\r\n", "\n");
        var libAvStartText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingStartLibAv.cs")
            .Replace("\r\n", "\n");

        AssertContains(flashbackStartText, "PublishRecordingStartedOutcome(fbRecordingContext.FinalOutputPath);");
        AssertContains(libAvStartText, "PublishRecordingStartedOutcome(rollback.RecordingContext.FinalOutputPath);");
        AssertDoesNotContain(lifecycleText, "_lastOutputPath = fbRecordingContext.FinalOutputPath;");
        AssertDoesNotContain(lifecycleText, "_lastOutputPath = recordingContext.FinalOutputPath;");
        AssertDoesNotContain(lifecycleText, "_lastFinalizeStatus = \"Recording\";");
        AssertDoesNotContain(lifecycleText, "_lastFinalizeUtc = null;");
        AssertDoesNotContain(lifecycleText, "_lastPreservedArtifacts = Array.Empty<string>();");

        AssertContains(finalizationText, "PublishRecordingFinalizedOutcome(fbResult, updateOutputPath: false);");
        AssertContains(finalizationText, "PublishRecordingFinalizedOutcome(result, updateOutputPath: true);");
        AssertDoesNotContain(finalizationText, "_lastOutputPath = result.OutputPath;");
        AssertDoesNotContain(finalizationText, "_lastFinalizeStatus = fbResult.StatusMessage;");
        AssertDoesNotContain(finalizationText, "_lastFinalizeStatus = result.StatusMessage;");
        AssertDoesNotContain(finalizationText, "_lastFinalizeUtc = DateTimeOffset.UtcNow;");
        AssertDoesNotContain(finalizationText, "_lastPreservedArtifacts = fbResult.PreservedArtifacts;");
        AssertDoesNotContain(finalizationText, "_lastPreservedArtifacts = result.PreservedArtifacts;");

        return Task.CompletedTask;
    }

}

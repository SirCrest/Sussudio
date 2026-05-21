using System.Threading.Tasks;

static partial class Program
{
    internal static Task CaptureService_RecordingRollbackLivesInFocusedPartial()
    {
        var finalizationCallSiteText = string.Join(
            "\n",
            new[]
            {
                ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingStopLifecycle.cs"),
                ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingFinalizeFlashbackBackend.cs"),
                ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingFinalizeLibAvBackend.cs"),
                ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingFinalizeLibAvVideoBoundary.cs"),
                ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingFinalizeLibAvSink.cs"),
                ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingFinalizeLibAvIdlePreview.cs"),
                ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingFinalizeLibAvPreviewRestore.cs"),
                ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingFinalizeFlashback.cs")
            }).Replace("\r\n", "\n");
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
        AssertDoesNotContain(finalizationCallSiteText, "private async Task DisposeTransientRecordingBackendAsync(");
        AssertContains(rollbackText, "private async Task DisposeTransientRecordingBackendAsync(");
        AssertContains(rollbackText, "Transient recording sink stop failed during rollback");
        AssertContains(rollbackText, "Transient unified video dispose failed during rollback");
        AssertContains(rollbackText, "_videoPipeline.ScheduleDeferredUnifiedVideoCaptureCleanup(");
        AssertContains(rollbackText, "reason: \"recording_start_rollback\"");
        AssertOccursBefore(rollbackText, "CAPTURE_RECORDING_START_FAIL", "RecordLastRecordingFailure(ex);");
        AssertOccursBefore(rollbackText, "RecordLastRecordingFailure(ex);", "await _artifactManager.RollbackAsync(rollback.RecordingContext)");
        AssertOccursBefore(rollbackText, "rollback.FlashbackRecordingBackendLeaseHeld = false;", "ReleaseSemaphoreBestEffort(_flashbackBackendLeaseLock, \"flashback_recording_start_fail\")");
        AssertOccursBefore(rollbackText, "await DisposeMicrophoneCaptureAsync().ConfigureAwait(false);", "await DisposeTransientRecordingBackendAsync(");

        return Task.CompletedTask;
    }

    internal static Task CaptureService_RecordingOutcomeStateLivesInFocusedPartial()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.cs")
            .Replace("\r\n", "\n");
        var lifecycleText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingLifecycle.cs")
            .Replace("\r\n", "\n");
        var finalizationCallSiteText = string.Join(
            "\n",
            new[]
            {
                ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingStopLifecycle.cs"),
                ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingFinalizeFlashbackBackend.cs"),
                ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingFinalizeLibAvBackend.cs"),
                ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingFinalizeLibAvVideoBoundary.cs"),
                ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingFinalizeLibAvSink.cs"),
                ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingFinalizeLibAvIdlePreview.cs"),
                ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingFinalizeLibAvPreviewRestore.cs"),
                ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingFinalizeFlashback.cs")
            }).Replace("\r\n", "\n");
        var routerText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingStopLifecycle.cs")
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

        AssertContains(finalizationCallSiteText, "PublishRecordingFinalizedOutcome(fbResult, updateOutputPath: false);");
        AssertContains(finalizationCallSiteText, "PublishRecordingFinalizedOutcome(result, updateOutputPath: true);");
        AssertDoesNotContain(routerText, "PublishRecordingFinalizedOutcome(fbResult, updateOutputPath: false);");
        AssertDoesNotContain(routerText, "PublishRecordingFinalizedOutcome(result, updateOutputPath: true);");
        AssertDoesNotContain(finalizationCallSiteText, "_lastOutputPath = result.OutputPath;");
        AssertDoesNotContain(finalizationCallSiteText, "_lastFinalizeStatus = fbResult.StatusMessage;");
        AssertDoesNotContain(finalizationCallSiteText, "_lastFinalizeStatus = result.StatusMessage;");
        AssertDoesNotContain(finalizationCallSiteText, "_lastFinalizeUtc = DateTimeOffset.UtcNow;");
        AssertDoesNotContain(finalizationCallSiteText, "_lastPreservedArtifacts = fbResult.PreservedArtifacts;");
        AssertDoesNotContain(finalizationCallSiteText, "_lastPreservedArtifacts = result.PreservedArtifacts;");

        return Task.CompletedTask;
    }
}

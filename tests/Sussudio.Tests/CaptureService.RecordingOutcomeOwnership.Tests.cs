using System.Threading.Tasks;

static partial class Program
{
    internal static Task CaptureService_RecordingRollbackLivesInFocusedPartial()
    {
        var finalizationCallSiteText = string.Join(
            "\n",
            new[]
            {
                ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingFinalizeFlashbackBackend.cs"),
                ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingFinalizeLibAvBackend.cs"),
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

    internal static Task CaptureService_RecordingOutcomeStateLivesWithRecordingLifecycle()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.cs")
            .Replace("\r\n", "\n");
        var lifecycleText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingLifecycle.cs")
            .Replace("\r\n", "\n");
        var finalizationCallSiteText = string.Join(
            "\n",
            new[]
            {
                ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingFinalizeFlashbackBackend.cs"),
                ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingFinalizeLibAvBackend.cs"),
                ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingFinalizeFlashback.cs")
            }).Replace("\r\n", "\n");
        var routerText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingLifecycle.cs")
            .Replace("\r\n", "\n");

        AssertDoesNotContain(rootText, "private string? _lastOutputPath;");
        AssertDoesNotContain(rootText, "private string _lastFinalizeStatus = \"None\";");
        AssertDoesNotContain(rootText, "private DateTimeOffset? _lastFinalizeUtc;");
        AssertDoesNotContain(rootText, "private IReadOnlyList<string> _lastPreservedArtifacts = Array.Empty<string>();");
        AssertContains(lifecycleText, "private void PublishRecordingStartedOutcome(string finalOutputPath)");
        AssertContains(lifecycleText, "private string? _lastOutputPath;");
        AssertContains(lifecycleText, "private string _lastFinalizeStatus = \"None\";");
        AssertContains(lifecycleText, "private DateTimeOffset? _lastFinalizeUtc;");
        AssertContains(lifecycleText, "private IReadOnlyList<string> _lastPreservedArtifacts = Array.Empty<string>();");
        AssertContains(lifecycleText, "_lastOutputPath = finalOutputPath;");
        AssertContains(lifecycleText, "_lastFinalizeStatus = \"Recording\";");
        AssertContains(lifecycleText, "_lastFinalizeUtc = null;");
        AssertContains(lifecycleText, "_lastPreservedArtifacts = Array.Empty<string>();");
        AssertContains(lifecycleText, "private void PublishRecordingFinalizedOutcome(FinalizeResult result, bool updateOutputPath)");
        AssertContains(lifecycleText, "if (updateOutputPath)");
        AssertContains(lifecycleText, "_lastOutputPath = result.OutputPath;");
        AssertContains(lifecycleText, "_lastFinalizeStatus = result.StatusMessage;");
        AssertContains(lifecycleText, "_lastFinalizeUtc = DateTimeOffset.UtcNow;");
        AssertContains(lifecycleText, "_lastPreservedArtifacts = result.PreservedArtifacts;");

        var flashbackStartText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingStartFlashback.cs")
            .Replace("\r\n", "\n");
        var libAvStartText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingStartLibAv.cs")
            .Replace("\r\n", "\n");

        AssertContains(flashbackStartText, "PublishRecordingStartedOutcome(fbRecordingContext.FinalOutputPath);");
        AssertContains(libAvStartText, "PublishRecordingStartedOutcome(rollback.RecordingContext.FinalOutputPath);");
        AssertDoesNotContain(lifecycleText, "_lastOutputPath = fbRecordingContext.FinalOutputPath;");
        AssertDoesNotContain(lifecycleText, "_lastOutputPath = recordingContext.FinalOutputPath;");

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
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.RecordingOutcomeState.cs")),
            "old recording outcome-state partial removed");

        return Task.CompletedTask;
    }
}

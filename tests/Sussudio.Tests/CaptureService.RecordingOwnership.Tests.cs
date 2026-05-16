using System.Threading.Tasks;

static partial class Program
{
    private static Task CaptureService_RecordingLifecycleLivesInFocusedPartial()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.cs")
            .Replace("\r\n", "\n");
        var lifecycleText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingLifecycle.cs")
            .Replace("\r\n", "\n");
        var stopLifecycleText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingStopLifecycle.cs")
            .Replace("\r\n", "\n");

        AssertDoesNotContain(rootText, "public Task StartRecordingAsync(");
        AssertDoesNotContain(rootText, "public Task StopRecordingAsync(");
        AssertContains(lifecycleText, "public Task StartRecordingAsync(");
        AssertContains(lifecycleText, "FLASHBACK_UNIFIED_RECORDING_START");
        AssertContains(lifecycleText, "await recordingSink.StartAsync(recordingContext, transitionToken)");
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
        var rollbackText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingRollback.cs")
            .Replace("\r\n", "\n");

        AssertDoesNotContain(finalizationText, "private async Task DisposeTransientRecordingBackendAsync(");
        AssertContains(rollbackText, "private async Task DisposeTransientRecordingBackendAsync(");
        AssertContains(rollbackText, "Transient recording sink stop failed during rollback");
        AssertContains(rollbackText, "Transient unified video dispose failed during rollback");
        AssertContains(rollbackText, "ScheduleDeferredUnifiedVideoCaptureCleanup(");
        AssertContains(rollbackText, "reason: \"recording_start_rollback\"");

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

        AssertContains(lifecycleText, "PublishRecordingStartedOutcome(fbRecordingContext.FinalOutputPath);");
        AssertContains(lifecycleText, "PublishRecordingStartedOutcome(recordingContext.FinalOutputPath);");
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

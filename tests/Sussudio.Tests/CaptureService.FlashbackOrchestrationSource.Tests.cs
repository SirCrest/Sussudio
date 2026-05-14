using System.Linq;

static partial class Program
{
    private static readonly string[] CaptureServiceFlashbackOrchestrationFiles =
    {
        "Sussudio/Services/Capture/CaptureService.FlashbackOrchestration.cs",
        "Sussudio/Services/Capture/CaptureService.FlashbackAudioInputs.cs",
        "Sussudio/Services/Capture/CaptureService.FlashbackPreviewBackend.cs",
        "Sussudio/Services/Capture/CaptureService.FlashbackPreviewBackendDisposal.cs",
        "Sussudio/Services/Capture/CaptureService.FlashbackBufferCycle.cs"
    };

    private static readonly string[] CaptureServiceRecordingFinalizationFiles =
    {
        "Sussudio/Services/Capture/CaptureService.RecordingFinalizeRecord.cs",
        "Sussudio/Services/Capture/CaptureService.RecordingFinalizeFlashback.cs"
    };

    private static string ReadCaptureServiceFlashbackOrchestrationSource()
        => string.Join(
            "\n",
            CaptureServiceFlashbackOrchestrationFiles.Select(file => ReadRepoFile(file).Replace("\r\n", "\n")));

    private static string ReadCaptureServiceRecordingFinalizationSource()
        => string.Join(
            "\n",
            CaptureServiceRecordingFinalizationFiles.Select(file => ReadRepoFile(file).Replace("\r\n", "\n")));

    private static string ReadCaptureServiceFlashbackOrchestrationCodeWithoutCommentsOrStrings()
        => string.Join(
            "\n",
            CaptureServiceFlashbackOrchestrationFiles.Select(ReadRepoCodeWithoutCommentsOrStrings));

    private static Task CaptureService_FlashbackOrchestrationLivesInFocusedPartials()
    {
        var orchestrationText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackOrchestration.cs");
        var audioInputsText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackAudioInputs.cs");
        var previewBackendText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackPreviewBackend.cs");
        var disposalText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackPreviewBackendDisposal.cs");
        var bufferCycleText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackBufferCycle.cs");

        AssertContains(orchestrationText, "private async Task RestartFlashbackCoreAsync(");
        AssertContains(audioInputsText, "private async Task EnsureFlashbackAudioInputsAsync(");
        AssertContains(previewBackendText, "private async Task EnsureFlashbackPreviewBackendAsync(");
        AssertContains(disposalText, "private async Task DisposeFlashbackPreviewBackendAsync(");
        AssertContains(disposalText, "private async Task DisposeFlashbackPreviewBackendCoreAsync(");
        AssertContains(bufferCycleText, "private async Task CycleFlashbackBufferAsync(");
        AssertDoesNotContain(orchestrationText, "private async Task EnsureFlashbackAudioInputsAsync(");
        AssertDoesNotContain(orchestrationText, "private async Task EnsureFlashbackPreviewBackendAsync(");
        AssertDoesNotContain(orchestrationText, "private async Task DisposeFlashbackPreviewBackendAsync(");
        AssertDoesNotContain(orchestrationText, "private async Task CycleFlashbackBufferAsync(");

        return Task.CompletedTask;
    }

    private static Task CaptureService_RecordingFinalizationLivesInFocusedPartials()
    {
        var recordFinalizationText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingFinalizeRecord.cs");
        var flashbackFinalizationText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingFinalizeFlashback.cs");

        AssertContains(recordFinalizationText, "private async Task<FinalizeResult> StopAndDisposeRecordingBackendAsync(");
        AssertContains(flashbackFinalizationText, "private async Task<FinalizeResult> FinalizeFlashbackRecordingAsync(");
        AssertContains(flashbackFinalizationText, "private sealed class FlashbackRecordingBoundarySnapshot");
        AssertContains(flashbackFinalizationText, "private void CaptureFlashbackRecordingBoundarySnapshot(");
        AssertContains(flashbackFinalizationText, "private static bool IsFlashbackFinalizeCancellationResult(FinalizeResult result)");
        AssertDoesNotContain(recordFinalizationText, "private sealed class FlashbackRecordingBoundarySnapshot");
        AssertDoesNotContain(recordFinalizationText, "private void CaptureFlashbackRecordingBoundarySnapshot(");

        return Task.CompletedTask;
    }
}

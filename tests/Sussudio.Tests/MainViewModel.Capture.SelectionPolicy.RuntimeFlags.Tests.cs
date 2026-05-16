using System.Threading.Tasks;

static partial class Program
{
    private static Task CaptureErrors_RefreshViewModelRuntimeFlags()
    {
        var mainViewModelText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.CaptureRuntimeEvents.cs")
            .Replace("\r\n", "\n");

        AssertContains(mainViewModelText, "IsInitialized = _captureService.IsInitialized;");
        AssertContains(mainViewModelText, "IsPreviewing = _captureService.IsVideoPreviewActive;");
        AssertContains(mainViewModelText, "IsRecording = _captureService.IsRecording;");
        AssertContains(mainViewModelText, "UpdateLiveCaptureInfo(runtimeSnapshot);");
        AssertContains(mainViewModelText, "UpdateHdrRuntimeStatusFromCapture(runtimeSnapshot);");

        return Task.CompletedTask;
    }
}

using System.Threading.Tasks;

static partial class Program
{
    private static Task CaptureErrors_RefreshViewModelRuntimeFlags()
    {
        var mainViewModelText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelRuntimeLifecycleController.cs")
            .Replace("\r\n", "\n");

        AssertContains(mainViewModelText, "_viewModel.IsInitialized = _viewModel._captureService.IsInitialized;");
        AssertContains(mainViewModelText, "_viewModel.IsPreviewing = _viewModel._captureService.IsVideoPreviewActive;");
        AssertContains(mainViewModelText, "_viewModel.IsRecording = _viewModel._captureService.IsRecording;");
        AssertContains(mainViewModelText, "_viewModel.UpdateLiveCaptureInfo(runtimeSnapshot);");
        AssertContains(mainViewModelText, "_viewModel.UpdateHdrRuntimeStatusFromCapture(runtimeSnapshot);");

        return Task.CompletedTask;
    }
}

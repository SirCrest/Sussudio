using System.Threading.Tasks;

static partial class Program
{
    private static Task CaptureErrors_RefreshViewModelRuntimeFlags()
    {
        var mainViewModelText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelRuntimeEventIngressController.cs")
            .Replace("\r\n", "\n");

        AssertContains(mainViewModelText, "_context.SetIsInitialized(_context.IsCaptureInitialized());");
        AssertContains(mainViewModelText, "_context.SetIsPreviewing(_context.IsVideoPreviewActive());");
        AssertContains(mainViewModelText, "_context.SetIsRecording(_context.IsCaptureRecording());");
        AssertContains(mainViewModelText, "_context.UpdateLiveCaptureInfo(runtimeSnapshot);");
        AssertContains(mainViewModelText, "_context.UpdateHdrRuntimeStatusFromCapture(runtimeSnapshot);");

        return Task.CompletedTask;
    }
}

using System.Threading.Tasks;

static partial class Program
{
    internal static Task PreviewReinitialization_WaitsForPendingFlashbackCycle()
    {
        var viewModelFiles = ReadMainViewModelCodeFiles();
        var viewModelSharedStateText = viewModelFiles["MainViewModel.State.cs"];
        var viewModelPreviewStateText = viewModelFiles["MainViewModel.PreviewState.cs"];
        var viewModelCaptureStateText = viewModelFiles["MainViewModel.CaptureState.cs"];
        var viewModelFlashbackStateText = viewModelFiles["MainViewModel.FlashbackState.cs"];
        var rawPreviewLifecycleControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelPreviewLifecycleController.cs")
            .Replace("\r\n", "\n");
        var rawPreviewReinitializeControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelPreviewReinitializeController.cs")
            .Replace("\r\n", "\n");

        AssertContains(viewModelFlashbackStateText, "private const int FlashbackCycleBeforeReinitializeTimeoutMs = 30000;");
        AssertContains(viewModelCaptureStateText, "private const int PreviewReinitializeDebounceMs = 250;");
        AssertContains(viewModelPreviewStateText, "private int _previewReinitializeGeneration;");
        AssertDoesNotContain(viewModelSharedStateText, "private int _previewReinitializeGeneration;");
        AssertContains(viewModelFiles["MainViewModel.PreviewState.cs"], "=> _previewLifecycleController.ReinitializeDeviceAsync(reason);");
        AssertContains(rawPreviewLifecycleControllerText, "=> _previewReinitializeController.ReinitializeDeviceAsync(reason);");
        AssertContains(rawPreviewReinitializeControllerText, "var reinitializeGeneration = _context.IncrementReinitializeGeneration();");
        AssertContains(rawPreviewReinitializeControllerText, "await Task.Delay(PreviewReinitializeDebounceMs).ConfigureAwait(true);");
        AssertContains(rawPreviewReinitializeControllerText, "_context.ReadReinitializeGeneration() != reinitializeGeneration");
        AssertContains(rawPreviewReinitializeControllerText, "REINIT_COALESCED reason='{reason}' generation={reinitializeGeneration}");
        AssertContains(rawPreviewReinitializeControllerText, "await AwaitWithTimeoutAsync(");
        AssertContains(rawPreviewReinitializeControllerText, "\"Flashback encoder settings cycle before reinitialize\").ConfigureAwait(false);");
        AssertContains(rawPreviewReinitializeControllerText, "REINIT_WAIT_FLASHBACK_CYCLE_TIMEOUT reason={reason} timeoutMs={FlashbackCycleBeforeReinitializeTimeoutMs}");
        AssertContains(rawPreviewReinitializeControllerText, "REINIT_WAIT_FLASHBACK_CYCLE_FAULT");
        AssertContains(rawPreviewReinitializeControllerText, "_context.ClearPendingFlashbackCycleIfSameAndCompleted(pendingCycle);");

        return Task.CompletedTask;
    }
}

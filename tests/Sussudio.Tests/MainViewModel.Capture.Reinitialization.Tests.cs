using System.Threading.Tasks;

static partial class Program
{
    private static Task MainViewModelPreviewLifecycle_LivesInController()
    {
        var captureText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.Capture.cs")
            .Replace("\r\n", "\n");
        var reinitializationText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.PreviewReinitialization.cs")
            .Replace("\r\n", "\n");
        var previewLifecycleControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelPreviewLifecycleController.cs")
            .Replace("\r\n", "\n");
        var agentMapText = ReadRepoFile("docs/architecture/AGENT_MAP.md")
            .Replace("\r\n", "\n");
        var cleanupPlanText = ReadRepoFile("docs/architecture/cleanup-plan.md")
            .Replace("\r\n", "\n");

        AssertContains(reinitializationText, "public partial class MainViewModel");
        AssertContains(reinitializationText, "private Task ReinitializeDeviceAsync(string reason)");
        AssertContains(reinitializationText, "=> _previewLifecycleController.ReinitializeDeviceAsync(reason);");
        AssertContains(previewLifecycleControllerText, "public async Task ReinitializeDeviceAsync(string reason)");
        AssertContains(previewLifecycleControllerText, "Interlocked.Increment(ref _viewModel._previewReinitializeGeneration)");
        AssertContains(previewLifecycleControllerText, "await Task.Delay(PreviewReinitializeDebounceMs).ConfigureAwait(true);");
        AssertContains(previewLifecycleControllerText, "Volatile.Read(ref _viewModel._previewReinitializeGeneration) != reinitializeGeneration");
        AssertContains(previewLifecycleControllerText, "await AwaitWithTimeoutAsync(");
        AssertContains(previewLifecycleControllerText, "FlashbackCycleBeforeReinitializeTimeoutMs");
        AssertContains(previewLifecycleControllerText, "await _viewModel._previewReinitializeGate.WaitAsync();");
        AssertContains(previewLifecycleControllerText, "await _viewModel.NotifyPreviewReinitRequestedAsync(reason);");
        AssertContains(previewLifecycleControllerText, "await _viewModel.NotifyRendererStopAsync();");
        AssertContains(previewLifecycleControllerText, "await StopPreviewAsync(userInitiated: false, teardownPipeline: true, CancellationToken.None);");
        AssertContains(previewLifecycleControllerText, "await InitializeDeviceAsync();");
        AssertContains(previewLifecycleControllerText, "await StartPreviewAsync(userInitiated: false);");
        AssertContains(previewLifecycleControllerText, "_viewModel._previewReinitializeGate.Release();");
        AssertDoesNotContain(captureText, "private async Task ReinitializeDeviceAsync(string reason)");
        AssertContains(captureText, "public Task StartPreviewAsync(bool userInitiated = true, CancellationToken cancellationToken = default)");
        AssertContains(captureText, "public Task StopPreviewAsync(bool userInitiated, bool teardownPipeline, CancellationToken cancellationToken)");
        AssertContains(agentMapText, "`Sussudio/Controllers/ViewModel/MainViewModelPreviewLifecycleController.cs`");
        AssertContains(cleanupPlanText, "`MainViewModel.PreviewReinitialization.cs`");
        AssertContains(cleanupPlanText, "`Sussudio/Controllers/ViewModel/MainViewModelPreviewLifecycleController.cs`");

        return Task.CompletedTask;
    }
}

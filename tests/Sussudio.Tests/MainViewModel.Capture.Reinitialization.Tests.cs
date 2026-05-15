using System.Threading.Tasks;

static partial class Program
{
    private static Task MainViewModelPreviewReinitialization_LivesInFocusedPartial()
    {
        var captureText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.Capture.cs")
            .Replace("\r\n", "\n");
        var reinitializationText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.PreviewReinitialization.cs")
            .Replace("\r\n", "\n");
        var agentMapText = ReadRepoFile("docs/architecture/AGENT_MAP.md")
            .Replace("\r\n", "\n");
        var cleanupPlanText = ReadRepoFile("docs/architecture/cleanup-plan.md")
            .Replace("\r\n", "\n");

        AssertContains(reinitializationText, "public partial class MainViewModel");
        AssertContains(reinitializationText, "private async Task ReinitializeDeviceAsync(string reason)");
        AssertContains(reinitializationText, "Interlocked.Increment(ref _previewReinitializeGeneration)");
        AssertContains(reinitializationText, "await Task.Delay(PreviewReinitializeDebounceMs).ConfigureAwait(true);");
        AssertContains(reinitializationText, "Volatile.Read(ref _previewReinitializeGeneration) != reinitializeGeneration");
        AssertContains(reinitializationText, "await AwaitWithTimeoutAsync(");
        AssertContains(reinitializationText, "FlashbackCycleBeforeReinitializeTimeoutMs");
        AssertContains(reinitializationText, "await _previewReinitializeGate.WaitAsync();");
        AssertContains(reinitializationText, "await NotifyPreviewReinitRequestedAsync(reason);");
        AssertContains(reinitializationText, "await NotifyRendererStopAsync();");
        AssertContains(reinitializationText, "await StopPreviewAsync(userInitiated: false, teardownPipeline: true);");
        AssertContains(reinitializationText, "await InitializeDeviceAsync();");
        AssertContains(reinitializationText, "await StartPreviewAsync(userInitiated: false);");
        AssertContains(reinitializationText, "_previewReinitializeGate.Release();");
        AssertDoesNotContain(captureText, "private async Task ReinitializeDeviceAsync(string reason)");
        AssertContains(captureText, "public async Task StartPreviewAsync(bool userInitiated = true, CancellationToken cancellationToken = default)");
        AssertContains(captureText, "public async Task StopPreviewAsync(bool userInitiated, bool teardownPipeline, CancellationToken cancellationToken)");
        AssertContains(agentMapText, "`MainViewModel.PreviewReinitialization.cs` owns\n  debounced preview reinitialization");
        AssertContains(cleanupPlanText, "`MainViewModel.PreviewReinitialization.cs`");

        return Task.CompletedTask;
    }
}

using System.Threading.Tasks;

static partial class Program
{
    internal static Task MainViewModelPreviewLifecycle_LivesInController()
    {
        var rootText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs")
            .Replace("\r\n", "\n");
        var previewStateText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.PreviewState.cs")
            .Replace("\r\n", "\n");
        var previewLifecycleControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelPreviewLifecycleController.cs")
            .Replace("\r\n", "\n");
        var previewReinitializeControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelPreviewReinitializeController.cs")
            .Replace("\r\n", "\n");
        var agentMapText = ReadRepoFile("docs/architecture/AGENT_MAP.md")
            .Replace("\r\n", "\n");
        var cleanupPlanText = ReadRepoFile("docs/architecture/cleanup-plan.md")
            .Replace("\r\n", "\n");

        AssertContains(previewStateText, "private Task ReinitializeDeviceAsync(string reason)");
        AssertContains(previewStateText, "=> _previewLifecycleController.ReinitializeDeviceAsync(reason);");
        AssertEqual(
            false,
            System.IO.File.Exists(System.IO.Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.Capture.cs")),
            "MainViewModel capture lifecycle facade partial");
        if (System.IO.File.Exists(System.IO.Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.PreviewReinitialization.cs")))
        {
            throw new System.InvalidOperationException("Preview reinitialization should not live in a tiny pass-through partial.");
        }
        AssertEqual(
            true,
            System.IO.File.ReadAllLines(System.IO.Path.Combine(
                GetRepoRoot(),
                "Sussudio",
                "Controllers",
                "ViewModel",
                "MainViewModelPreviewReinitializeController.cs")).Length >= 100,
            "Preview reinitialize transaction controller is not a tiny pass-through file");
        AssertContains(previewLifecycleControllerText, "private readonly MainViewModelPreviewReinitializeController _previewReinitializeController;");
        AssertContains(previewLifecycleControllerText, "public Task ReinitializeDeviceAsync(string reason)");
        AssertContains(previewLifecycleControllerText, "=> _previewReinitializeController.ReinitializeDeviceAsync(reason);");
        AssertContains(previewLifecycleControllerText, "namespace Sussudio.Controllers;");
        AssertContains(previewLifecycleControllerText, "internal sealed class MainViewModelPreviewLifecycleController");
        AssertContains(previewReinitializeControllerText, "namespace Sussudio.Controllers;");
        AssertContains(previewReinitializeControllerText, "internal sealed class MainViewModelPreviewReinitializeController");
        AssertContains(previewReinitializeControllerText, "public void CancelPendingPreviewRestart()");
        AssertContains(previewReinitializeControllerText, "public void ResetPendingPreviewRestartCancellation()");
        AssertContains(previewReinitializeControllerText, "public async Task ReinitializeDeviceAsync(string reason)");
        AssertContains(previewReinitializeControllerText, "private readonly MainViewModelPreviewReinitializeControllerContext _context;");
        AssertDoesNotContain(previewReinitializeControllerText, "private readonly MainViewModel _viewModel;");
        AssertDoesNotContain(previewReinitializeControllerText, "_viewModel.");
        AssertContains(previewReinitializeControllerText, "var reinitializeGeneration = _context.IncrementReinitializeGeneration();");
        AssertContains(previewReinitializeControllerText, "await Task.Delay(_context.PreviewReinitializeDebounceMs).ConfigureAwait(true);");
        AssertContains(previewReinitializeControllerText, "_context.ReadReinitializeGeneration() != reinitializeGeneration");
        AssertContains(previewReinitializeControllerText, "await _context.AwaitWithTimeoutAsync(");
        AssertContains(previewReinitializeControllerText, "FlashbackCycleBeforeReinitializeTimeoutMs");
        AssertContains(previewReinitializeControllerText, "await _context.WaitReinitializeGateAsync();");
        AssertContains(previewReinitializeControllerText, "await _context.NotifyPreviewReinitRequestedAsync(reason);");
        AssertContains(previewReinitializeControllerText, "await _context.NotifyRendererStopAsync();");
        AssertContains(previewReinitializeControllerText, "await _previewLifecycleController.StopPreviewAsync(userInitiated: false, teardownPipeline: true, CancellationToken.None);");
        AssertContains(previewReinitializeControllerText, "await _previewLifecycleController.InitializeDeviceAsync();");
        AssertContains(previewReinitializeControllerText, "await _previewLifecycleController.StartPreviewAsync(userInitiated: false);");
        AssertContains(previewReinitializeControllerText, "_context.ReleaseReinitializeGate();");
        AssertDoesNotContain(previewStateText, "private async Task ReinitializeDeviceAsync(string reason)");
        AssertDoesNotContain(rootText, "private Task ReinitializeDeviceAsync(string reason)");
        AssertContains(previewStateText, "public Task StartPreviewAsync(bool userInitiated = true, CancellationToken cancellationToken = default)");
        AssertContains(previewStateText, "public Task StopPreviewAsync(bool userInitiated, bool teardownPipeline, CancellationToken cancellationToken)");
        AssertContains(agentMapText, "`Sussudio/Controllers/ViewModel/MainViewModelPreviewLifecycleController.cs`");
        AssertContains(agentMapText, "`Sussudio/Controllers/ViewModel/MainViewModelPreviewReinitializeController.cs`");
        AssertDoesNotContain(cleanupPlanText, "`MainViewModel.PreviewReinitialization.cs`");
        AssertContains(cleanupPlanText, "`Sussudio/Controllers/ViewModel/MainViewModelPreviewLifecycleController.cs`");
        AssertContains(cleanupPlanText, "`Sussudio/Controllers/ViewModel/MainViewModelPreviewReinitializeController.cs`");

        return Task.CompletedTask;
    }
}

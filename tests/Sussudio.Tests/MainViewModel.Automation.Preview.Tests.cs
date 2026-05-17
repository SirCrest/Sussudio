using System.IO;
using System.Threading.Tasks;

static partial class Program
{
    private static Task MainViewModelAutomation_PreviewEnablementLivesInPreviewLifecycleController()
    {
        var mainViewModelText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs")
            .Replace("\r\n", "\n");
        var previewLifecycleControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelPreviewLifecycleController.cs")
            .Replace("\r\n", "\n");

        AssertContains(mainViewModelText, "public Task SetPreviewEnabledAsync(bool enabled, CancellationToken cancellationToken = default)\n        => _previewLifecycleController.SetPreviewEnabledAsync(enabled, cancellationToken);");
        AssertContains(previewLifecycleControllerText, "public Task SetPreviewEnabledAsync(bool enabled, CancellationToken cancellationToken = default)");
        AssertContains(previewLifecycleControllerText, "CancelPendingPreviewRestart();");
        AssertContains(previewLifecycleControllerText, "if (enabled == _viewModel.IsPreviewing)");
        AssertContains(previewLifecycleControllerText, "await StartPreviewAsync(userInitiated: true, cancellationToken);");
        AssertContains(previewLifecycleControllerText, "await StopPreviewAsync(userInitiated: true, teardownPipeline: false, cancellationToken);");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.AutomationPreview.cs")),
            "MainViewModel preview automation partial");

        return Task.CompletedTask;
    }
}

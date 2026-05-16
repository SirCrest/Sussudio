using System.Threading.Tasks;

static partial class Program
{
    private static Task MainViewModelAutomation_PreviewEnablementLivesInFocusedPartial()
    {
        var automationPreviewText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.AutomationPreview.cs")
            .Replace("\r\n", "\n");

        AssertContains(automationPreviewText, "public Task SetPreviewEnabledAsync(bool enabled, CancellationToken cancellationToken = default)");
        AssertContains(automationPreviewText, "CancelPendingPreviewRestart();");
        AssertContains(automationPreviewText, "if (enabled == IsPreviewing)");
        AssertContains(automationPreviewText, "await StartPreviewAsync(userInitiated: true, cancellationToken);");
        AssertContains(automationPreviewText, "await StopPreviewAsync(userInitiated: true, teardownPipeline: false, cancellationToken);");

        return Task.CompletedTask;
    }
}

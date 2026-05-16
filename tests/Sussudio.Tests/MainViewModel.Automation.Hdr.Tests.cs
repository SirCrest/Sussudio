using System.Threading.Tasks;

static partial class Program
{
    private static Task MainViewModelAutomation_HdrEnablementLivesInFocusedPartial()
    {
        var automationHdrText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.AutomationHdr.cs")
            .Replace("\r\n", "\n");

        AssertContains(automationHdrText, "public Task SetHdrEnabledAsync(bool enabled, CancellationToken cancellationToken = default)");
        AssertContains(automationHdrText, "throw new InvalidOperationException(HdrToggleBlockedWhileRecordingMessage);");
        AssertContains(automationHdrText, "if (enabled && !IsHdrAvailable)");
        AssertContains(automationHdrText, "throw new InvalidOperationException(\"HDR is not available on the selected device.\");");
        AssertContains(automationHdrText, "IsHdrEnabled = enabled;");
        AssertContains(automationHdrText, "public Task SetTrueHdrPreviewEnabledAsync(bool enabled, CancellationToken cancellationToken = default)");
        AssertContains(automationHdrText, "throw new InvalidOperationException(\"True HDR preview cannot be changed while recording.\");");
        AssertContains(automationHdrText, "IsTrueHdrPreviewEnabled = enabled;");

        return Task.CompletedTask;
    }
}

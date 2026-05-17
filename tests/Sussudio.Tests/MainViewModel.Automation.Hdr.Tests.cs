using System.Threading.Tasks;

static partial class Program
{
    private static Task MainViewModelAutomation_HdrEnablementLivesInFocusedPartial()
    {
        var automationHdrText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.AutomationHdr.cs")
            .Replace("\r\n", "\n");
        var captureModeTransactionsText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.CaptureModeTransactions.cs")
            .Replace("\r\n", "\n");

        AssertContains(automationHdrText, "public Task SetHdrEnabledAsync(bool enabled, CancellationToken cancellationToken = default)");
        AssertContains(automationHdrText, "throw new InvalidOperationException(HdrToggleBlockedWhileRecordingMessage);");
        AssertContains(automationHdrText, "if (enabled && !IsHdrAvailable)");
        AssertContains(automationHdrText, "throw new InvalidOperationException(\"HDR is not available on the selected device.\");");
        AssertContains(automationHdrText, "IsHdrEnabled = enabled;");
        AssertContains(automationHdrText, "public Task SetTrueHdrPreviewEnabledAsync(bool enabled, CancellationToken cancellationToken = default)");
        AssertContains(automationHdrText, "throw new InvalidOperationException(\"True HDR preview cannot be changed while recording.\");");
        AssertContains(automationHdrText, "IsTrueHdrPreviewEnabled = enabled;");
        AssertContains(captureModeTransactionsText, "partial void OnIsHdrEnabledChanged(bool value)");
        AssertContains(captureModeTransactionsText, "if (_isRevertingHdrToggle)");
        AssertContains(captureModeTransactionsText, "_pendingSdrAutoSelectionForDeviceChange = false;");
        AssertContains(captureModeTransactionsText, "_pendingSdrAutoFriendlyFrameRateBucket = null;");
        AssertContains(captureModeTransactionsText, "IsHdrEnabled = !value;");
        AssertContains(captureModeTransactionsText, "StatusText = HdrToggleBlockedWhileRecordingMessage;");
        AssertContains(captureModeTransactionsText, "ResetModeSelectionState();");
        AssertContains(captureModeTransactionsText, "RebuildResolutionOptions();");
        AssertContains(captureModeTransactionsText, "RebuildRecordingFormatOptions();");
        AssertContains(captureModeTransactionsText, "EnqueueUiOperation(() => ReinitializeDeviceAsync(\"HDR toggle\"), \"hdr toggle reinitialize\");");
        AssertContains(captureModeTransactionsText, "SaveSettings();");
        AssertOccursBefore(captureModeTransactionsText, "if (_isRevertingHdrToggle)", "if (value)");
        AssertOccursBefore(captureModeTransactionsText, "if (value)", "if (IsRecording)");
        AssertOccursBefore(captureModeTransactionsText, "StatusText = HdrToggleBlockedWhileRecordingMessage;", "if (!_isChangingDevice)");
        AssertOccursBefore(captureModeTransactionsText, "ResetModeSelectionState();", "RebuildResolutionOptions();");
        AssertOccursBefore(captureModeTransactionsText, "RebuildResolutionOptions();", "RebuildRecordingFormatOptions();");
        AssertOccursBefore(captureModeTransactionsText, "RebuildRecordingFormatOptions();", "EnqueueUiOperation(() => ReinitializeDeviceAsync(\"HDR toggle\"), \"hdr toggle reinitialize\");");
        AssertOccursBefore(captureModeTransactionsText, "EnqueueUiOperation(() => ReinitializeDeviceAsync(\"HDR toggle\"), \"hdr toggle reinitialize\");", "SaveSettings();");

        return Task.CompletedTask;
    }
}

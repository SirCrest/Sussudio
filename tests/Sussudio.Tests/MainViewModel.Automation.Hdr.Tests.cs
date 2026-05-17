using System.IO;
using System.Threading.Tasks;

static partial class Program
{
    private static Task MainViewModelAutomation_HdrEnablementLivesInCaptureModeTransactions()
    {
        var captureModeTransactionsText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.CaptureModeTransactions.cs")
            .Replace("\r\n", "\n");
        var hdrChangeBlock = ExtractTextBetween(
            captureModeTransactionsText,
            "partial void OnIsHdrEnabledChanged(bool value)",
            "partial void OnShowAllCaptureOptionsChanged(bool value)");

        AssertContains(captureModeTransactionsText, "public Task SetHdrEnabledAsync(bool enabled, CancellationToken cancellationToken = default)");
        AssertContains(captureModeTransactionsText, "throw new InvalidOperationException(HdrToggleBlockedWhileRecordingMessage);");
        AssertContains(captureModeTransactionsText, "if (enabled && !IsHdrAvailable)");
        AssertContains(captureModeTransactionsText, "throw new InvalidOperationException(\"HDR is not available on the selected device.\");");
        AssertContains(captureModeTransactionsText, "IsHdrEnabled = enabled;");
        AssertContains(captureModeTransactionsText, "public Task SetTrueHdrPreviewEnabledAsync(bool enabled, CancellationToken cancellationToken = default)");
        AssertContains(captureModeTransactionsText, "throw new InvalidOperationException(\"True HDR preview cannot be changed while recording.\");");
        AssertContains(captureModeTransactionsText, "IsTrueHdrPreviewEnabled = enabled;");
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
        AssertOccursBefore(hdrChangeBlock, "if (_isRevertingHdrToggle)", "if (value)");
        AssertOccursBefore(hdrChangeBlock, "if (value)", "if (IsRecording)");
        AssertOccursBefore(hdrChangeBlock, "StatusText = HdrToggleBlockedWhileRecordingMessage;", "if (!_isChangingDevice)");
        AssertOccursBefore(hdrChangeBlock, "ResetModeSelectionState();", "RebuildResolutionOptions();");
        AssertOccursBefore(hdrChangeBlock, "RebuildResolutionOptions();", "RebuildRecordingFormatOptions();");
        AssertOccursBefore(hdrChangeBlock, "RebuildRecordingFormatOptions();", "EnqueueUiOperation(() => ReinitializeDeviceAsync(\"HDR toggle\"), \"hdr toggle reinitialize\");");
        AssertOccursBefore(hdrChangeBlock, "EnqueueUiOperation(() => ReinitializeDeviceAsync(\"HDR toggle\"), \"hdr toggle reinitialize\");", "SaveSettings();");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.AutomationHdr.cs")),
            "MainViewModel HDR automation partial");

        return Task.CompletedTask;
    }
}

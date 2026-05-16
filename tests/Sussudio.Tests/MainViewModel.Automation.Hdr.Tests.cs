using System.Threading.Tasks;

static partial class Program
{
    private static Task MainViewModelAutomation_HdrEnablementLivesInFocusedPartial()
    {
        var automationHdrText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.AutomationHdr.cs")
            .Replace("\r\n", "\n");
        var hdrModeChangesText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.HdrModeChanges.cs")
            .Replace("\r\n", "\n");

        AssertContains(automationHdrText, "public Task SetHdrEnabledAsync(bool enabled, CancellationToken cancellationToken = default)");
        AssertContains(automationHdrText, "throw new InvalidOperationException(HdrToggleBlockedWhileRecordingMessage);");
        AssertContains(automationHdrText, "if (enabled && !IsHdrAvailable)");
        AssertContains(automationHdrText, "throw new InvalidOperationException(\"HDR is not available on the selected device.\");");
        AssertContains(automationHdrText, "IsHdrEnabled = enabled;");
        AssertContains(automationHdrText, "public Task SetTrueHdrPreviewEnabledAsync(bool enabled, CancellationToken cancellationToken = default)");
        AssertContains(automationHdrText, "throw new InvalidOperationException(\"True HDR preview cannot be changed while recording.\");");
        AssertContains(automationHdrText, "IsTrueHdrPreviewEnabled = enabled;");
        AssertContains(hdrModeChangesText, "partial void OnIsHdrEnabledChanged(bool value)");
        AssertContains(hdrModeChangesText, "if (_isRevertingHdrToggle)");
        AssertContains(hdrModeChangesText, "_pendingSdrAutoSelectionForDeviceChange = false;");
        AssertContains(hdrModeChangesText, "_pendingSdrAutoFriendlyFrameRateBucket = null;");
        AssertContains(hdrModeChangesText, "IsHdrEnabled = !value;");
        AssertContains(hdrModeChangesText, "StatusText = HdrToggleBlockedWhileRecordingMessage;");
        AssertContains(hdrModeChangesText, "ResetModeSelectionState();");
        AssertContains(hdrModeChangesText, "RebuildResolutionOptions();");
        AssertContains(hdrModeChangesText, "RebuildRecordingFormatOptions();");
        AssertContains(hdrModeChangesText, "EnqueueUiOperation(() => ReinitializeDeviceAsync(\"HDR toggle\"), \"hdr toggle reinitialize\");");
        AssertContains(hdrModeChangesText, "SaveSettings();");
        AssertOccursBefore(hdrModeChangesText, "if (_isRevertingHdrToggle)", "if (value)");
        AssertOccursBefore(hdrModeChangesText, "if (value)", "if (IsRecording)");
        AssertOccursBefore(hdrModeChangesText, "StatusText = HdrToggleBlockedWhileRecordingMessage;", "if (!_isChangingDevice)");
        AssertOccursBefore(hdrModeChangesText, "ResetModeSelectionState();", "RebuildResolutionOptions();");
        AssertOccursBefore(hdrModeChangesText, "RebuildResolutionOptions();", "RebuildRecordingFormatOptions();");
        AssertOccursBefore(hdrModeChangesText, "RebuildRecordingFormatOptions();", "EnqueueUiOperation(() => ReinitializeDeviceAsync(\"HDR toggle\"), \"hdr toggle reinitialize\");");
        AssertOccursBefore(hdrModeChangesText, "EnqueueUiOperation(() => ReinitializeDeviceAsync(\"HDR toggle\"), \"hdr toggle reinitialize\");", "SaveSettings();");

        return Task.CompletedTask;
    }
}

using System.Threading.Tasks;

static partial class Program
{
    private static Task ModeSelectionState_LivesInFocusedPartial()
    {
        var resolutionOptionsText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.ResolutionOptions.cs").Replace("\r\n", "\n");
        var resolutionOptionRebuildText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.ResolutionOptionRebuild.cs").Replace("\r\n", "\n");
        var frameRateOptionsText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.FrameRateOptions.cs").Replace("\r\n", "\n");
        var captureModeOptionsControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelCaptureModeOptionRebuildController.cs").Replace("\r\n", "\n");
        var modeSelectionText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.ModeSelectionState.cs").Replace("\r\n", "\n");

        AssertContains(resolutionOptionRebuildText, "private void RebuildResolutionOptions()");
        AssertContains(resolutionOptionRebuildText, "=> _captureModeOptionRebuildController.RebuildResolutionOptions();");
        AssertContains(captureModeOptionsControllerText, "public void RebuildResolutionOptions()");
        AssertContains(resolutionOptionsText, "private bool TryResolveResolutionKey(");
        AssertContains(resolutionOptionsText, "private static bool IsAutoResolutionValue(");
        AssertDoesNotContain(resolutionOptionsText, "private void ResetFrameRateSelectionState()");
        AssertDoesNotContain(resolutionOptionsText, "private void ApplyResolvedFrameRateSelection(");
        AssertDoesNotContain(resolutionOptionsText, "private void ResetModeSelectionState()");
        AssertDoesNotContain(frameRateOptionsText, "private void ResetFrameRateSelectionState()");
        AssertDoesNotContain(frameRateOptionsText, "private void ApplyResolvedFrameRateSelection(");
        AssertDoesNotContain(frameRateOptionsText, "private void ResetModeSelectionState()");
        AssertContains(frameRateOptionsText, "ApplyResolvedFrameRateSelection(selection.Selected, SelectedFrameRate > 0 ? SelectedFrameRate : 60);");
        AssertContains(captureModeOptionsControllerText, "_viewModel.ApplyResolvedFrameRateSelection(selection.Selected, fallbackRate);");
        AssertContains(modeSelectionText, "private void ResetFrameRateSelectionState()");
        AssertContains(modeSelectionText, "_hasUserOverriddenFrameRateForCurrentMode = false;");
        AssertContains(modeSelectionText, "IsAutoFrameRateSelected = true;");
        AssertContains(modeSelectionText, "private void ApplyResolvedFrameRateSelection(FrameRateOption? selected, double fallbackRate)");
        AssertContains(modeSelectionText, "_isApplyingAutomaticFrameRateSelection = true;\n        try\n        {\n            SelectedFrameRate = selected?.Value ?? fallbackRate;\n        }\n        finally\n        {\n            _isApplyingAutomaticFrameRateSelection = false;\n        }");
        AssertContains(modeSelectionText, "SelectedFriendlyFrameRate = selected?.FriendlyValue ?? Math.Round(SelectedFrameRate);");
        AssertContains(modeSelectionText, "SelectedExactFrameRate = selected?.Value ?? SelectedFrameRate;");
        AssertContains(modeSelectionText, "SelectedExactFrameRateArg = selected?.Rational;");
        AssertContains(modeSelectionText, "if (IsAutoResolutionValue(SelectedResolution))\n        {\n            AutoResolvedFrameRate = selected?.Value ?? SelectedFrameRate;\n        }");
        AssertContains(modeSelectionText, "AutoResolvedFrameRate = selected?.Value ?? SelectedFrameRate;");
        AssertContains(modeSelectionText, "DisabledFrameRateReason = selected is { IsEnabled: false }\n            ? selected.DisableReason\n            : string.Empty;");
        AssertContains(modeSelectionText, "private void ResetModeSelectionState()");
        AssertContains(modeSelectionText, "ResetFrameRateSelectionState();");
        AssertContains(modeSelectionText, "_hasUserOverriddenResolutionForCurrentMode = false;");
        AssertContains(modeSelectionText, "_forceSourceAutoRetarget = false;");
        AssertContains(modeSelectionText, "_lastSourceModeKey = null;");
        AssertContains(modeSelectionText, "_pendingSdrAutoSelectionForDeviceChange = false;");
        AssertContains(modeSelectionText, "_pendingSdrAutoFriendlyFrameRateBucket = null;");

        return Task.CompletedTask;
    }
}

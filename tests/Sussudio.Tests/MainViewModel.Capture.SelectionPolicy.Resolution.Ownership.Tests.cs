using System.Threading.Tasks;

static partial class Program
{
    internal static Task ResolutionSelectionPolicy_LivesInFocusedPartial()
    {
        var resolutionOptionsText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.ResolutionOptions.cs").Replace("\r\n", "\n");
        var captureModeTransactionsText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.CaptureModeTransactions.cs").Replace("\r\n", "\n");
        var captureModeOptionsControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelCaptureModeOptionRebuildController.cs").Replace("\r\n", "\n");
        var resolutionOptionRebuildControllerText = captureModeOptionsControllerText;
        var capturePresentationText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.CapturePresentation.cs").Replace("\r\n", "\n");
        var autoCaptureSelectionPolicyText = ReadRepoFile("Sussudio/ViewModels/AutoCaptureSelectionPolicy.cs").Replace("\r\n", "\n");
        var helperText = ReadRepoFile("Sussudio/ViewModels/CaptureResolutionSelectionPolicy.cs").Replace("\r\n", "\n");

        AssertContains(captureModeTransactionsText, "private void RebuildResolutionOptions()");
        AssertContains(captureModeTransactionsText, "=> _captureModeOptionRebuildController.RebuildResolutionOptions();");
        AssertContains(resolutionOptionRebuildControllerText, "namespace Sussudio.Controllers;");
        AssertContains(resolutionOptionRebuildControllerText, "internal sealed class MainViewModelCaptureModeOptionRebuildController");
        AssertContains(resolutionOptionRebuildControllerText, "public void RebuildResolutionOptions()");
        AssertContains(resolutionOptionRebuildControllerText, "private AutoCaptureSelection? ResolveAutoCaptureSelection(");
        AssertContains(resolutionOptionRebuildControllerText, "AutoCaptureSelectionPolicy.Select(new AutoCaptureSelectionRequest(");
        AssertContains(resolutionOptionRebuildControllerText, "CaptureModeOptionsBuilder.BuildResolutionOptions(");
        AssertContains(resolutionOptionRebuildControllerText, "_context.AvailableResolutions.Clear();");
        AssertContains(resolutionOptionRebuildControllerText, "_context.AvailableResolutions.Add(option);");
        AssertDoesNotContain(captureModeOptionsControllerText, "private readonly MainViewModel _viewModel;");
        AssertDoesNotContain(resolutionOptionRebuildControllerText, "_viewModel.");
        AssertContains(resolutionOptionRebuildControllerText, "=> RebuildFrameRateOptions();");
        AssertContains(captureModeOptionsControllerText, "public void RebuildResolutionOptions()");
        AssertDoesNotContain(captureModeOptionsControllerText, "_viewModel.AvailableResolutions.Clear();");
        AssertDoesNotContain(resolutionOptionsText, "private string GetSelectedResolutionDisplayText()");
        AssertContains(resolutionOptionRebuildControllerText, "private ResolutionOption CreateAutoResolutionOption()");
        AssertContains(resolutionOptionRebuildControllerText, "Value = _context.AutoResolutionValue,");
        AssertContains(resolutionOptionRebuildControllerText, "private bool ShouldSelectAutoResolutionOption(");
        AssertDoesNotContain(resolutionOptionsText, "private ResolutionOption? SelectBestAutoResolutionCandidate(");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Controllers", "ViewModel", "MainViewModelResolutionOptionRebuildController.cs")),
            "old standalone resolution option rebuild controller removed");
        AssertEqual(
            false,
            File.Exists(Path.Combine(
                GetRepoRoot(),
                "Sussudio",
                "ViewModels",
                "MainViewModel.AutoResolutionSelection.cs")),
            "MainViewModel auto resolution selection adapter partial");
        AssertContains(autoCaptureSelectionPolicyText, "internal sealed record AutoCaptureSelection(");
        AssertContains(autoCaptureSelectionPolicyText, "internal sealed record AutoCaptureSelectionRequest(");
        AssertContains(autoCaptureSelectionPolicyText, "internal static class AutoCaptureSelectionPolicy");
        AssertContains(autoCaptureSelectionPolicyText, "internal static AutoCaptureSelection? Select(AutoCaptureSelectionRequest request)");
        AssertContains(autoCaptureSelectionPolicyText, "private static ResolutionOption? SelectBestResolutionCandidate(");
        AssertContains(autoCaptureSelectionPolicyText, "private static MediaFormat SelectPreferredFrameRateFormat(");
        AssertDoesNotContain(autoCaptureSelectionPolicyText, "AvailableResolutions.Clear();");
        AssertDoesNotContain(autoCaptureSelectionPolicyText, "SelectedResolution =");
        AssertContains(resolutionOptionsText, "/// Effective resolution state and selection-policy delegates.");
        AssertContains(captureModeTransactionsText, "/// Capture-mode transactions that coordinate option rebuilds");
        AssertContains(resolutionOptionRebuildControllerText, "private void UpdateAutoResolutionState(AutoCaptureSelection? selection)");
        AssertContains(resolutionOptionRebuildControllerText, "_context.SetAutoResolvedWidth(selection?.Resolution.Width);");
        AssertContains(resolutionOptionRebuildControllerText, "private void ClearAutoResolutionState()");
        AssertContains(capturePresentationText, "/// Capture presentation adapters that apply runtime/source state to ViewModel labels.");
        AssertContains(capturePresentationText, "private string GetSelectedResolutionDisplayText()");
        AssertContains(capturePresentationText, "return $\"{AutoResolutionValue} ({GetResolutionKey(AutoResolvedWidth.Value, AutoResolvedHeight.Value)} @ {friendlyRate.Value:0} fps)\";");
        AssertContains(resolutionOptionsText, "private static bool IsAutoResolutionValue(");
        AssertContains(resolutionOptionsText, "private bool TryResolveResolutionKey(");
        AssertContains(resolutionOptionsText, "private string? GetEffectiveResolutionKey(");
        AssertContains(resolutionOptionsText, "private bool TryGetEffectiveResolutionSelection(");
        AssertDoesNotContain(resolutionOptionsText, "private ResolutionOption? SelectHdrResolutionOption(");
        AssertContains(resolutionOptionRebuildControllerText, "CaptureResolutionSelectionPolicy.Select(new CaptureResolutionSelectionRequest(");
        AssertContains(resolutionOptionsText, "CaptureResolutionSelectionPolicy.TryParseResolutionKey(");
        AssertContains(resolutionOptionsText, "CaptureResolutionSelectionPolicy.ResolutionSupportsFriendlyFrameRate(");
        AssertContains(resolutionOptionsText, "CaptureResolutionSelectionPolicy.BuildHdrSupportHint(");
        AssertDoesNotContain(resolutionOptionsText, "SelectNearestResolution(");
        AssertDoesNotContain(resolutionOptionsText, "sdrFriendlyBucketsByResolution");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.AutoResolutionOptions.cs")),
            "old auto resolution options partial removed");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.AutoResolutionState.cs")),
            "old auto resolution state partial removed");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.ResolutionSelectionPolicy.cs")),
            "old resolution selection policy adapter partial removed");
        AssertContains(helperText, "internal static class CaptureResolutionSelectionPolicy");
        AssertDoesNotContain(helperText, "partial class CaptureResolutionSelectionPolicy");
        AssertContains(helperText, "internal static CaptureResolutionSelection Select(CaptureResolutionSelectionRequest request)");
        AssertContains(helperText, "internal static bool TryParseResolutionKey(");
        AssertContains(helperText, "internal static string BuildHdrSupportHint(");
        AssertContains(helperText, "private static ResolutionOption? SelectSourceResolutionOption(");
        AssertContains(helperText, "SelectNearestResolution(sourceKey, enabled)");
        AssertContains(helperText, "private static HdrResolutionSelection SelectHdrResolutionOption(");
        AssertContains(helperText, "SelectNearestResolution(previousSelection, sameFpsCandidates)");
        AssertContains(helperText, "private static SdrAutoResolutionSelection? SelectSdrAutoResolutionOption(");
        AssertContains(helperText, "sdrFriendlyBucketsByResolution");
        AssertContains(helperText, "internal static bool ResolutionSupportsFriendlyFrameRate(");
        AssertContains(helperText, "private static ResolutionOption? SelectNearestResolution(");
        AssertContains(helperText, "internal sealed record CaptureResolutionSelectionRequest(");
        AssertContains(helperText, "internal sealed record CaptureResolutionSelection(");
        AssertDoesNotContain(helperText, "AvailableResolutions.Clear();");
        AssertDoesNotContain(helperText, "OnPropertyChanged(");
        AssertDoesNotContain(helperText, "SelectedResolution =");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "CaptureResolutionSelectionPolicy.Source.cs")),
            "old source resolution selection policy partial removed");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "CaptureResolutionSelectionPolicy.Hdr.cs")),
            "old HDR resolution selection policy partial removed");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "CaptureResolutionSelectionPolicy.Sdr.cs")),
            "old SDR resolution selection policy partial removed");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "CaptureResolutionSelectionPolicy.Support.cs")),
            "old resolution support policy partial removed");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "CaptureResolutionSelectionPolicy.Ranking.cs")),
            "old resolution ranking policy partial removed");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "CaptureResolutionSelectionPolicy.Models.cs")),
            "old resolution policy models partial removed");

        return Task.CompletedTask;
    }
}

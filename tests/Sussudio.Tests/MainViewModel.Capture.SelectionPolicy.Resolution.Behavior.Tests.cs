using System.IO;
using System.Threading.Tasks;

static partial class Program
{
    internal static Task ResolutionSelectionPolicy_LivesInFocusedPartial()
    {
        var resolutionOptionsText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.CaptureSelection.cs").Replace("\r\n", "\n");
        var captureModeTransactionsText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.CaptureSelection.cs").Replace("\r\n", "\n");
        var captureModeOptionsControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelCaptureModeOptionRebuildController.cs").Replace("\r\n", "\n");
        var resolutionOptionRebuildControllerText = captureModeOptionsControllerText;
        var capturePresentationText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs").Replace("\r\n", "\n");
        var autoCaptureSelectionPolicyText = ReadRepoFile("Sussudio/ViewModels/ViewModelSelectionPolicies.cs").Replace("\r\n", "\n");
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
        AssertEqual(false, File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.ResolutionOptions.cs")), "old resolution options partial folded into capture selection owner");
        AssertDoesNotContain(autoCaptureSelectionPolicyText, "AvailableResolutions.Clear();");
        AssertDoesNotContain(autoCaptureSelectionPolicyText, "SelectedResolution =");
        AssertContains(resolutionOptionsText, "/// Capture-device, resolution, and frame-rate selection reactions.");
        AssertContains(captureModeTransactionsText, "/// Capture-mode transactions that coordinate option rebuilds");
        AssertContains(resolutionOptionRebuildControllerText, "private void UpdateAutoResolutionState(AutoCaptureSelection? selection)");
        AssertContains(resolutionOptionRebuildControllerText, "_context.SetAutoResolvedWidth(selection?.Resolution.Width);");
        AssertContains(resolutionOptionRebuildControllerText, "private void ClearAutoResolutionState()");
        AssertContains(capturePresentationText, "// Capture presentation adapters that apply runtime/source state to ViewModel labels.");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.CapturePresentation.cs")),
            "MainViewModel.CapturePresentation.cs folded into MainViewModel.cs");
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

    internal static Task CaptureResolutionSelectionPolicy_PreservesHdrSourceRetargetBehavior()
    {
        var mediaFormatType = RequireType("Sussudio.Models.MediaFormat");
        var resolutionType = RequireType("Sussudio.Models.ResolutionOption");
        var telemetryType = RequireType("Sussudio.Models.SourceSignalTelemetrySnapshot");

        var formatsByResolution = CreateResolutionFormatDictionary(mediaFormatType);
        AddResolutionFormats(
            formatsByResolution,
            mediaFormatType,
            "3840x2160",
            CreateTestMediaFormat(mediaFormatType, 3840, 2160, 60, "P010", isHdr: true));
        AddResolutionFormats(
            formatsByResolution,
            mediaFormatType,
            "1920x1080",
            CreateTestMediaFormat(mediaFormatType, 1920, 1080, 120, "P010", isHdr: true));
        AddResolutionFormats(
            formatsByResolution,
            mediaFormatType,
            "1280x720",
            CreateTestMediaFormat(mediaFormatType, 1280, 720, 120, "P010", isHdr: true));

        var options = CreateResolutionOptionList(
            resolutionType,
            CreateResolutionOption(resolutionType, "3840x2160", 3840, 2160, isEnabled: true),
            CreateResolutionOption(resolutionType, "1920x1080", 1920, 1080, isEnabled: true),
            CreateResolutionOption(resolutionType, "1280x720", 1280, 720, isEnabled: true));
        var telemetry = CreateConfigInstance(telemetryType);
        SetPropertyOrBackingField(telemetry, "Width", 3840);
        SetPropertyOrBackingField(telemetry, "Height", 2160);

        var selection = InvokeCaptureResolutionSelection(
            options,
            formatsByResolution,
            telemetry,
            preferredSelection: "3840x2160",
            previousFrameRate: 120,
            isHdrEnabled: true,
            allowSourceAutoSelect: true,
            pendingSdrAutoSelectionForDeviceChange: false);
        var selected = selection.GetType().GetProperty("Selected")!.GetValue(selection)
            ?? throw new InvalidOperationException("HDR source retarget returned no selection.");

        AssertEqual("1920x1080", GetStringProperty(selected, "Value"), "HDR source retarget preserves frame-rate bucket before resolution");
        AssertEqual(
            "HDR at 3840x2160 supported up to 60 fps; switched to 1920x1080 to keep 120 fps.",
            selection.GetType().GetProperty("HdrHint")!.GetValue(selection) as string,
            "HDR source retarget hint");

        var retained = InvokeCaptureResolutionSelection(
            options,
            formatsByResolution,
            telemetry,
            preferredSelection: "3840x2160",
            previousFrameRate: 60,
            isHdrEnabled: true,
            allowSourceAutoSelect: true,
            pendingSdrAutoSelectionForDeviceChange: false);
        var retainedSelected = retained.GetType().GetProperty("Selected")!.GetValue(retained)
            ?? throw new InvalidOperationException("HDR exact match retention returned no selection.");

        AssertEqual("3840x2160", GetStringProperty(retainedSelected, "Value"), "HDR exact source match remains selected when it supports the current rate");
        AssertEqual(null, retained.GetType().GetProperty("HdrHint")!.GetValue(retained) as string, "HDR retained exact match defers support hint fallback to ResolutionOptions");

        return Task.CompletedTask;
    }

    internal static Task CaptureResolutionSelectionPolicy_PreservesSdrAutoBucketPreference()
    {
        var mediaFormatType = RequireType("Sussudio.Models.MediaFormat");
        var resolutionType = RequireType("Sussudio.Models.ResolutionOption");
        var telemetryType = RequireType("Sussudio.Models.SourceSignalTelemetrySnapshot");

        var formatsByResolution = CreateResolutionFormatDictionary(mediaFormatType);
        AddResolutionFormats(
            formatsByResolution,
            mediaFormatType,
            "3840x2160",
            CreateTestMediaFormat(mediaFormatType, 3840, 2160, 120, "NV12", isHdr: false));
        AddResolutionFormats(
            formatsByResolution,
            mediaFormatType,
            "1920x1080",
            CreateTestMediaFormat(mediaFormatType, 1920, 1080, 60, "NV12", isHdr: false));
        AddResolutionFormats(
            formatsByResolution,
            mediaFormatType,
            "1280x720",
            CreateTestMediaFormat(mediaFormatType, 1280, 720, 30, "NV12", isHdr: false));

        var selection = InvokeCaptureResolutionSelection(
            CreateResolutionOptionList(
                resolutionType,
                CreateResolutionOption(resolutionType, "3840x2160", 3840, 2160, isEnabled: true),
                CreateResolutionOption(resolutionType, "1920x1080", 1920, 1080, isEnabled: true),
                CreateResolutionOption(resolutionType, "1280x720", 1280, 720, isEnabled: true)),
            formatsByResolution,
            CreateConfigInstance(telemetryType),
            preferredSelection: "3840x2160",
            previousFrameRate: 120,
            isHdrEnabled: false,
            allowSourceAutoSelect: false,
            pendingSdrAutoSelectionForDeviceChange: true);
        var selected = selection.GetType().GetProperty("Selected")!.GetValue(selection)
            ?? throw new InvalidOperationException("SDR auto selection returned no selection.");

        AssertEqual("1920x1080", GetStringProperty(selected, "Value"), "SDR auto prefers a 60 fps bucket before largest 120-only resolution");
        AssertEqual(60, selection.GetType().GetProperty("SdrAutoFriendlyFrameRateBucket")!.GetValue(selection), "SDR auto selected friendly bucket");

        return Task.CompletedTask;
    }

    internal static Task AutoCaptureSelectionPolicy_PreservesSourceBoundedSelection()
    {
        var mediaFormatType = RequireType("Sussudio.Models.MediaFormat");
        var resolutionType = RequireType("Sussudio.Models.ResolutionOption");
        var telemetryType = RequireType("Sussudio.Models.SourceSignalTelemetrySnapshot");

        var formatsByResolution = CreateResolutionFormatDictionary(mediaFormatType);
        AddResolutionFormats(
            formatsByResolution,
            mediaFormatType,
            "3840x2160",
            CreateTestMediaFormat(mediaFormatType, 3840, 2160, 120, "NV12", isHdr: false));
        AddResolutionFormats(
            formatsByResolution,
            mediaFormatType,
            "1920x1080",
            CreateTestMediaFormat(mediaFormatType, 1920, 1080, 60, "NV12", isHdr: false));
        AddResolutionFormats(
            formatsByResolution,
            mediaFormatType,
            "1280x720",
            CreateTestMediaFormat(mediaFormatType, 1280, 720, 30, "NV12", isHdr: false));

        var telemetry = CreateConfigInstance(telemetryType);
        SetPropertyOrBackingField(telemetry, "Width", 1920);
        SetPropertyOrBackingField(telemetry, "Height", 1080);
        SetPropertyOrBackingField(telemetry, "FrameRateExact", 60d);

        var selection = InvokeAutoCaptureSelection(
            CreateResolutionOptionList(
                resolutionType,
                CreateResolutionOption(resolutionType, "3840x2160", 3840, 2160, isEnabled: true),
                CreateResolutionOption(resolutionType, "1920x1080", 1920, 1080, isEnabled: true),
                CreateResolutionOption(resolutionType, "1280x720", 1280, 720, isEnabled: true)),
            formatsByResolution,
            telemetry,
            isHdrEnabled: false);
        var selectedResolution = selection.GetType().GetProperty("Resolution")!.GetValue(selection)
            ?? throw new InvalidOperationException("Auto capture selection returned no resolution.");

        AssertEqual("1920x1080", GetStringProperty(selectedResolution, "Value"), "Auto capture selection caps resolution to source dimensions");
        AssertEqual(60, selection.GetType().GetProperty("FriendlyFrameRate")!.GetValue(selection), "Auto capture selection keeps source-friendly frame-rate bucket");
        AssertEqual(60d, GetDoubleProperty(selection, "ExactFrameRate"), "Auto capture selection keeps exact frame rate");

        return Task.CompletedTask;
    }
}

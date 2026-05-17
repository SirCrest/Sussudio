using System.Threading.Tasks;

static partial class Program
{
    private static Task ResolutionSelectionPolicy_LivesInFocusedPartial()
    {
        var resolutionOptionsText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.ResolutionOptions.cs").Replace("\r\n", "\n");
        var capturePresentationText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.CapturePresentation.cs").Replace("\r\n", "\n");
        var autoCaptureSelectionPolicyText = ReadRepoFile("Sussudio/ViewModels/AutoCaptureSelectionPolicy.cs").Replace("\r\n", "\n");
        var helperText = ReadRepoFile("Sussudio/ViewModels/CaptureResolutionSelectionPolicy.cs").Replace("\r\n", "\n");
        var sourcePolicyText = ReadRepoFile("Sussudio/ViewModels/CaptureResolutionSelectionPolicy.Source.cs").Replace("\r\n", "\n");
        var hdrPolicyText = ReadRepoFile("Sussudio/ViewModels/CaptureResolutionSelectionPolicy.Hdr.cs").Replace("\r\n", "\n");
        var sdrPolicyText = ReadRepoFile("Sussudio/ViewModels/CaptureResolutionSelectionPolicy.Sdr.cs").Replace("\r\n", "\n");
        var supportPolicyText = ReadRepoFile("Sussudio/ViewModels/CaptureResolutionSelectionPolicy.Support.cs").Replace("\r\n", "\n");
        var rankingPolicyText = ReadRepoFile("Sussudio/ViewModels/CaptureResolutionSelectionPolicy.Ranking.cs").Replace("\r\n", "\n");
        var modelsPolicyText = ReadRepoFile("Sussudio/ViewModels/CaptureResolutionSelectionPolicy.Models.cs").Replace("\r\n", "\n");
        var policyFamilyText = helperText + sourcePolicyText + hdrPolicyText + sdrPolicyText + supportPolicyText + rankingPolicyText + modelsPolicyText;

        AssertContains(resolutionOptionsText, "private void RebuildResolutionOptions()");
        AssertContains(resolutionOptionsText, "private AutoCaptureSelection? ResolveAutoCaptureSelection(");
        AssertContains(resolutionOptionsText, "AutoCaptureSelectionPolicy.Select(new AutoCaptureSelectionRequest(");
        AssertContains(resolutionOptionsText, "CaptureModeOptionsBuilder.BuildResolutionOptions(");
        AssertContains(resolutionOptionsText, "AvailableResolutions.Clear();");
        AssertContains(resolutionOptionsText, "AvailableResolutions.Add(option);");
        AssertDoesNotContain(resolutionOptionsText, "private string GetSelectedResolutionDisplayText()");
        AssertContains(resolutionOptionsText, "private ResolutionOption CreateAutoResolutionOption()");
        AssertContains(resolutionOptionsText, "private bool ShouldSelectAutoResolutionOption(");
        AssertDoesNotContain(resolutionOptionsText, "private ResolutionOption? SelectBestAutoResolutionCandidate(");
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
        AssertContains(resolutionOptionsText, "/// Resolution option building and dropdown mutation.");
        AssertContains(resolutionOptionsText, "private void UpdateAutoResolutionState(AutoCaptureSelection? selection)");
        AssertContains(resolutionOptionsText, "AutoResolvedWidth = selection?.Resolution.Width;");
        AssertContains(resolutionOptionsText, "private void ClearAutoResolutionState()");
        AssertContains(capturePresentationText, "/// Capture presentation adapters that apply runtime/source state to ViewModel labels.");
        AssertContains(capturePresentationText, "private string GetSelectedResolutionDisplayText()");
        AssertContains(capturePresentationText, "return $\"{AutoResolutionValue} ({GetResolutionKey(AutoResolvedWidth.Value, AutoResolvedHeight.Value)} @ {friendlyRate.Value:0} fps)\";");
        AssertContains(resolutionOptionsText, "private static bool IsAutoResolutionValue(");
        AssertContains(resolutionOptionsText, "private bool TryResolveResolutionKey(");
        AssertContains(resolutionOptionsText, "private string? GetEffectiveResolutionKey(");
        AssertContains(resolutionOptionsText, "private bool TryGetEffectiveResolutionSelection(");
        AssertDoesNotContain(resolutionOptionsText, "private ResolutionOption? SelectHdrResolutionOption(");
        AssertContains(resolutionOptionsText, "CaptureResolutionSelectionPolicy.Select(new CaptureResolutionSelectionRequest(");
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
        AssertContains(helperText, "internal static partial class CaptureResolutionSelectionPolicy");
        AssertContains(helperText, "internal static CaptureResolutionSelection Select(CaptureResolutionSelectionRequest request)");
        AssertDoesNotContain(helperText, "internal static bool TryParseResolutionKey(");
        AssertDoesNotContain(helperText, "internal static string BuildHdrSupportHint(");
        AssertDoesNotContain(helperText, "private static ResolutionOption? SelectSourceResolutionOption(");
        AssertDoesNotContain(helperText, "private static HdrResolutionSelection SelectHdrResolutionOption(");
        AssertDoesNotContain(helperText, "private static SdrAutoResolutionSelection? SelectSdrAutoResolutionOption(");
        AssertContains(sourcePolicyText, "private static ResolutionOption? SelectSourceResolutionOption(");
        AssertContains(sourcePolicyText, "SelectNearestResolution(sourceKey, enabled)");
        AssertDoesNotContain(sourcePolicyText, "private static ResolutionOption? SelectNearestResolution(");
        AssertContains(rankingPolicyText, "private static ResolutionOption? SelectNearestResolution(");
        AssertContains(hdrPolicyText, "private static HdrResolutionSelection SelectHdrResolutionOption(");
        AssertContains(hdrPolicyText, "internal static string BuildHdrSupportHint(");
        AssertContains(hdrPolicyText, "SelectNearestResolution(previousSelection, sameFpsCandidates)");
        AssertContains(sdrPolicyText, "private static SdrAutoResolutionSelection? SelectSdrAutoResolutionOption(");
        AssertDoesNotContain(sdrPolicyText, "private static ResolutionOption? SelectNearestResolution(");
        AssertContains(sdrPolicyText, "sdrFriendlyBucketsByResolution");
        AssertContains(supportPolicyText, "internal static bool TryParseResolutionKey(");
        AssertContains(supportPolicyText, "internal static bool ResolutionSupportsFriendlyFrameRate(");
        AssertContains(modelsPolicyText, "internal sealed record CaptureResolutionSelectionRequest(");
        AssertContains(modelsPolicyText, "internal sealed record CaptureResolutionSelection(");
        AssertDoesNotContain(policyFamilyText, "AvailableResolutions.Clear();");
        AssertDoesNotContain(policyFamilyText, "OnPropertyChanged(");
        AssertDoesNotContain(policyFamilyText, "SelectedResolution =");

        return Task.CompletedTask;
    }

    private static Task CaptureResolutionSelectionPolicy_PreservesHdrSourceRetargetBehavior()
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

    private static Task CaptureResolutionSelectionPolicy_PreservesSdrAutoBucketPreference()
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

    private static Task AutoCaptureSelectionPolicy_PreservesSourceBoundedSelection()
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

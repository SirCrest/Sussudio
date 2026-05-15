using System;
using System.Threading.Tasks;

static partial class Program
{
    private static Task ShowAllCaptureOptions_UnlocksSourceFilteredFrameRates()
    {
        var mainViewModelText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.FrameRateOptions.cs").Replace("\r\n", "\n");
        var sourceFilterPolicyText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.FrameRateSourceFilterPolicy.cs").Replace("\r\n", "\n");

        AssertContains(mainViewModelText, "FrameRateSourceFilterPolicy.Apply(");
        AssertContains(mainViewModelText, "ShowAllCaptureOptions);");
        AssertContains(sourceFilterPolicyText, "showAllCaptureOptions");
        AssertContains(sourceFilterPolicyText, "!IsSourceFilteredFrameRateDisableReason(option.DisableReason)");
        AssertContains(sourceFilterPolicyText, "CloneOption(option, isEnabled: true, disableReason: string.Empty)");
        AssertDoesNotContain(mainViewModelText, "private static bool IsSourceFilteredFrameRateDisableReason(");
        AssertDoesNotContain(mainViewModelText, "higher capture fps duplicates frames");

        return Task.CompletedTask;
    }

    private static Task FrameRateSourceFilterPolicy_LivesInFocusedHelper()
    {
        var frameRateOptionsText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.FrameRateOptions.cs").Replace("\r\n", "\n");
        var sourceFilterPolicyText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.FrameRateSourceFilterPolicy.cs").Replace("\r\n", "\n");

        AssertContains(frameRateOptionsText, "var sourceRate = ResolveDetectedSourceFrameRate(selectedResolutionKey, options, previousRate);");
        AssertContains(frameRateOptionsText, "AvailableFrameRates.Clear();");
        AssertContains(frameRateOptionsText, "ApplyResolvedFrameRateSelection(selected, fallbackRate);");
        AssertContains(sourceFilterPolicyText, "private static class FrameRateSourceFilterPolicy");
        AssertContains(sourceFilterPolicyText, "internal static FrameRateSourceFilterResult Apply(");
        AssertContains(sourceFilterPolicyText, "IReadOnlyCollection<FrameRateTimingVariant> resolutionTimingVariants");
        AssertContains(sourceFilterPolicyText, "option.FriendlyValue > sourceFriendlyRate.Value + 0.01");
        AssertContains(sourceFilterPolicyText, "option.Value > sourceRate.Value + 0.03");
        AssertContains(sourceFilterPolicyText, "higher capture fps duplicates frames");
        AssertContains(sourceFilterPolicyText, "duplicate variant is hidden");
        AssertContains(sourceFilterPolicyText, "not a clean divisor");
        AssertContains(sourceFilterPolicyText, "private IReadOnlyList<FrameRateTimingVariant> BuildFrameRateTimingVariants(string? resolutionKey)");
        AssertDoesNotContain(sourceFilterPolicyText, "AvailableFrameRates.Clear();");
        AssertDoesNotContain(sourceFilterPolicyText, "ApplyResolvedFrameRateSelection(");
        AssertDoesNotContain(sourceFilterPolicyText, "DetectedSourceFrameRate =");

        return Task.CompletedTask;
    }

    private static Task ResolutionSelectionPolicy_LivesInFocusedPartial()
    {
        var resolutionOptionsText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.ResolutionOptions.cs").Replace("\r\n", "\n");
        var selectionPolicyText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.ResolutionSelectionPolicy.cs").Replace("\r\n", "\n");

        AssertContains(resolutionOptionsText, "private void RebuildResolutionOptions()");
        AssertContains(resolutionOptionsText, "private bool TryResolveResolutionKey(");
        AssertDoesNotContain(resolutionOptionsText, "private ResolutionOption? SelectHdrResolutionOption(");
        AssertContains(selectionPolicyText, "private ResolutionOption? TrySelectSourceResolutionOption(");
        AssertContains(selectionPolicyText, "private ResolutionOption? SelectHdrResolutionOption(");
        AssertContains(selectionPolicyText, "private bool TrySelectSdrAutoResolutionOption(");
        AssertContains(selectionPolicyText, "private static bool TryParseResolutionKey(");
        AssertContains(selectionPolicyText, "private string BuildHdrSupportHintForResolution(");

        return Task.CompletedTask;
    }

    private static Task FrameRateTimingPolicy_LivesInFocusedPartial()
    {
        var formatSelectionText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.FormatSelection.cs").Replace("\r\n", "\n");
        var timingText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.FrameRateTiming.cs").Replace("\r\n", "\n");

        AssertContains(formatSelectionText, "private void UpdateSelectedFormat()");
        AssertContains(formatSelectionText, "private void RebuildVideoFormatOptions()");
        AssertContains(formatSelectionText, "partial void OnIsHdrEnabledChanged(bool value)");
        AssertDoesNotContain(formatSelectionText, "private FrameRateTimingFamily ResolvePreferredTimingFamily(");
        AssertDoesNotContain(formatSelectionText, "private static bool TryInferFrameRateTimingFamily(");
        AssertContains(timingText, "private FrameRateTimingFamily ResolvePreferredTimingFamily(");
        AssertContains(timingText, "private static MediaFormat SelectPreferredFrameRateFormat(");
        AssertContains(timingText, "private (double? Rate, string? Arg, string Origin) ResolveDetectedSourceFrameRate(");
        AssertContains(timingText, "private static bool TryInferFrameRateTimingFamily(");
        AssertContains(timingText, "private static bool TryParseFrameRateRational(");

        return Task.CompletedTask;
    }

    private static Task RecordingFormatSelectionPolicy_LivesInFocusedHelper()
    {
        var formatSelectionText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.FormatSelection.cs").Replace("\r\n", "\n");
        var automationRecordingSettingsText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.AutomationRecordingSettings.cs").Replace("\r\n", "\n");
        var recordingFormatPolicyText = ReadRepoFile("Sussudio/ViewModels/RecordingFormatSelectionPolicy.cs").Replace("\r\n", "\n");

        AssertContains(formatSelectionText, "RecordingFormatSelectionPolicy.Select(");
        AssertContains(formatSelectionText, "RecordingFormatSelectionPolicy.IsHdrCompatible(SelectedRecordingFormat)");
        AssertContains(automationRecordingSettingsText, "RecordingFormatSelectionPolicy.IsHdrCompatible(matched)");
        AssertDoesNotContain(formatSelectionText, "private static bool IsHdrCompatibleRecordingFormat(");
        AssertContains(recordingFormatPolicyText, "internal static class RecordingFormatSelectionPolicy");
        AssertContains(recordingFormatPolicyText, "internal static bool IsHdrCompatible(");
        AssertContains(recordingFormatPolicyText, "internal static RecordingFormatSelection Select(");
        AssertContains(recordingFormatPolicyText, "internal sealed record RecordingFormatSelection(");
        AssertContains(recordingFormatPolicyText, "Keep the last known real formats visible if capability refresh temporarily produced none.");

        return Task.CompletedTask;
    }

    private static Task LiveSignalTextProjection_PreservesPixelFormatFallbackOrder()
    {
        var runtimeText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.Runtime.cs")
            .Replace("\r\n", "\n");
        var liveSignalText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.LiveSignalText.cs")
            .Replace("\r\n", "\n");

        AssertContains(runtimeText, "var liveSignalText = BuildLiveSignalText(runtime, _captureService.EncoderCodecName);");
        AssertContains(runtimeText, "LiveResolution = liveSignalText.Resolution;");
        AssertContains(runtimeText, "LiveFrameRate = liveSignalText.FrameRate;");
        AssertContains(runtimeText, "LivePixelFormat = liveSignalText.PixelFormat;");
        AssertDoesNotContain(runtimeText, "runtime.ReaderSourceSubtype ??");
        AssertDoesNotContain(runtimeText, "runtime.LatestObservedFramePixelFormat ??");
        AssertContains(liveSignalText, "private static LiveSignalText BuildLiveSignalText(CaptureRuntimeSnapshot runtime, string? encoderCodecName)");
        AssertContains(liveSignalText, "runtime.ActualWidth ?? runtime.NegotiatedWidth ?? runtime.RequestedWidth");
        AssertContains(liveSignalText, "runtime.ActualHeight ?? runtime.NegotiatedHeight ?? runtime.RequestedHeight");
        AssertContains(liveSignalText, "runtime.ActualFrameRate ?? runtime.NegotiatedFrameRate ?? runtime.RequestedFrameRate");
        AssertContains(liveSignalText, "frameRateValue.Value.ToString(\"0.00\")");
        AssertContains(liveSignalText, "runtime.ReaderSourceSubtype ??");
        AssertContains(liveSignalText, "runtime.LatestObservedFramePixelFormat ??");
        AssertContains(liveSignalText, "\"hevc_nvenc\" => \" / HEVC\"");
        AssertContains(liveSignalText, "\"h264_nvenc\" => \" / H264\"");
        AssertContains(liveSignalText, "\"av1_nvenc\" => \" / AV1\"");
        AssertContains(liveSignalText, "? LiveInfoUnavailable");

        if (liveSignalText.IndexOf("runtime.ReaderSourceSubtype ??", StringComparison.Ordinal) >
            liveSignalText.IndexOf("runtime.LatestObservedFramePixelFormat ??", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("MainViewModel.LivePixelFormat should prefer ReaderSourceSubtype before LatestObservedFramePixelFormat.");
        }

        return Task.CompletedTask;
    }

    private static Task CaptureErrors_RefreshViewModelRuntimeFlags()
    {
        var mainViewModelText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.Runtime.cs")
            .Replace("\r\n", "\n");

        AssertContains(mainViewModelText, "IsInitialized = _captureService.IsInitialized;");
        AssertContains(mainViewModelText, "IsPreviewing = _captureService.IsVideoPreviewActive;");
        AssertContains(mainViewModelText, "IsRecording = _captureService.IsRecording;");
        AssertContains(mainViewModelText, "UpdateLiveCaptureInfo(runtimeSnapshot);");
        AssertContains(mainViewModelText, "UpdateHdrRuntimeStatusFromCapture(runtimeSnapshot);");

        return Task.CompletedTask;
    }
}

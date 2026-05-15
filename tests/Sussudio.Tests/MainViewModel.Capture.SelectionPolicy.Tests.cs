using System;
using System.Collections;
using System.Reflection;
using System.Threading.Tasks;

static partial class Program
{
    private static Task ShowAllCaptureOptions_UnlocksSourceFilteredFrameRates()
    {
        var mainViewModelText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.FrameRateOptions.cs").Replace("\r\n", "\n");
        var sourceFilterPolicyText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.FrameRateSourceFilterPolicy.cs").Replace("\r\n", "\n");
        var captureOptionVisibilityText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.CaptureOptionVisibility.cs").Replace("\r\n", "\n");

        AssertContains(mainViewModelText, "FrameRateSourceFilterPolicy.Apply(");
        AssertContains(mainViewModelText, "ShowAllCaptureOptions);");
        AssertContains(captureOptionVisibilityText, "partial void OnShowAllCaptureOptionsChanged(bool value)");
        AssertContains(captureOptionVisibilityText, "if (IsRecording)");
        AssertContains(captureOptionVisibilityText, "_pendingModeOptionsRefresh = true;");
        AssertContains(captureOptionVisibilityText, "_pendingModeOptionsRefresh = false;");
        AssertContains(captureOptionVisibilityText, "RebuildResolutionOptions();");
        AssertContains(captureOptionVisibilityText, "SaveSettings();");
        AssertOccursBefore(captureOptionVisibilityText, "if (IsRecording)", "_pendingModeOptionsRefresh = true;");
        AssertOccursBefore(captureOptionVisibilityText, "_pendingModeOptionsRefresh = true;", "SaveSettings();");
        AssertOccursBefore(captureOptionVisibilityText, "SaveSettings();", "return;");
        AssertOccursBefore(captureOptionVisibilityText, "return;", "_pendingModeOptionsRefresh = false;");
        AssertOccursBefore(captureOptionVisibilityText, "_pendingModeOptionsRefresh = false;", "RebuildResolutionOptions();");
        AssertOccursBefore(captureOptionVisibilityText, "RebuildResolutionOptions();", "SaveSettings();\n    }");
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
        var modeSelectionText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.ModeSelectionState.cs").Replace("\r\n", "\n");

        AssertContains(frameRateOptionsText, "var sourceRate = ResolveDetectedSourceFrameRate(selectedResolutionKey, options, previousRate);");
        AssertContains(frameRateOptionsText, "AvailableFrameRates.Clear();");
        AssertContains(frameRateOptionsText, "ApplyResolvedFrameRateSelection(selected, fallbackRate);");
        AssertContains(modeSelectionText, "private void ApplyResolvedFrameRateSelection(FrameRateOption? selected, double fallbackRate)");
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

    private static Task ModeSelectionState_LivesInFocusedPartial()
    {
        var resolutionOptionsText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.ResolutionOptions.cs").Replace("\r\n", "\n");
        var frameRateOptionsText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.FrameRateOptions.cs").Replace("\r\n", "\n");
        var modeSelectionText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.ModeSelectionState.cs").Replace("\r\n", "\n");

        AssertContains(resolutionOptionsText, "private void RebuildResolutionOptions()");
        AssertContains(resolutionOptionsText, "private bool TryResolveResolutionKey(");
        AssertDoesNotContain(resolutionOptionsText, "private void ResetFrameRateSelectionState()");
        AssertDoesNotContain(resolutionOptionsText, "private void ApplyResolvedFrameRateSelection(");
        AssertDoesNotContain(resolutionOptionsText, "private void ResetModeSelectionState()");
        AssertDoesNotContain(frameRateOptionsText, "private void ResetFrameRateSelectionState()");
        AssertDoesNotContain(frameRateOptionsText, "private void ApplyResolvedFrameRateSelection(");
        AssertDoesNotContain(frameRateOptionsText, "private void ResetModeSelectionState()");
        AssertContains(frameRateOptionsText, "ApplyResolvedFrameRateSelection(selected, SelectedFrameRate > 0 ? SelectedFrameRate : 60);");
        AssertContains(frameRateOptionsText, "ApplyResolvedFrameRateSelection(selected, fallbackRate);");
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

    private static Task ResolutionSelectionPolicy_LivesInFocusedPartial()
    {
        var resolutionOptionsText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.ResolutionOptions.cs").Replace("\r\n", "\n");
        var selectionPolicyText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.ResolutionSelectionPolicy.cs").Replace("\r\n", "\n");
        var helperText = ReadRepoFile("Sussudio/ViewModels/CaptureResolutionSelectionPolicy.cs").Replace("\r\n", "\n");

        AssertContains(resolutionOptionsText, "private void RebuildResolutionOptions()");
        AssertContains(resolutionOptionsText, "private bool TryResolveResolutionKey(");
        AssertDoesNotContain(resolutionOptionsText, "private ResolutionOption? SelectHdrResolutionOption(");
        AssertContains(resolutionOptionsText, "CaptureResolutionSelectionPolicy.Select(new CaptureResolutionSelectionRequest(");
        AssertContains(selectionPolicyText, "CaptureResolutionSelectionPolicy.TryParseResolutionKey(");
        AssertContains(selectionPolicyText, "CaptureResolutionSelectionPolicy.ResolutionSupportsFriendlyFrameRate(");
        AssertContains(selectionPolicyText, "CaptureResolutionSelectionPolicy.BuildHdrSupportHint(");
        AssertDoesNotContain(selectionPolicyText, "SelectNearestResolution(");
        AssertDoesNotContain(selectionPolicyText, "sdrFriendlyBucketsByResolution");
        AssertContains(helperText, "internal static class CaptureResolutionSelectionPolicy");
        AssertContains(helperText, "internal static CaptureResolutionSelection Select(CaptureResolutionSelectionRequest request)");
        AssertContains(helperText, "internal sealed record CaptureResolutionSelectionRequest(");
        AssertContains(helperText, "internal sealed record CaptureResolutionSelection(");
        AssertContains(helperText, "private static ResolutionOption? SelectSourceResolutionOption(");
        AssertContains(helperText, "private static HdrResolutionSelection SelectHdrResolutionOption(");
        AssertContains(helperText, "private static SdrAutoResolutionSelection? SelectSdrAutoResolutionOption(");
        AssertContains(helperText, "SelectNearestResolution(sourceKey, enabled)");
        AssertContains(helperText, "SelectNearestResolution(previousSelection, sameFpsCandidates)");
        AssertContains(helperText, "sdrFriendlyBucketsByResolution");
        AssertDoesNotContain(helperText, "AvailableResolutions.Clear();");
        AssertDoesNotContain(helperText, "OnPropertyChanged(");
        AssertDoesNotContain(helperText, "SelectedResolution =");

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

    private static object InvokeCaptureResolutionSelection(
        object options,
        object formatsByResolution,
        object telemetry,
        string? preferredSelection,
        double previousFrameRate,
        bool isHdrEnabled,
        bool allowSourceAutoSelect,
        bool pendingSdrAutoSelectionForDeviceChange)
    {
        var requestType = RequireType("Sussudio.ViewModels.CaptureResolutionSelectionRequest");
        var policyType = RequireType("Sussudio.ViewModels.CaptureResolutionSelectionPolicy");
        var constructor = FindConstructor(requestType, parameterCount: 8);
        var request = constructor.Invoke(new object?[]
        {
            options,
            formatsByResolution,
            telemetry,
            preferredSelection,
            previousFrameRate,
            isHdrEnabled,
            allowSourceAutoSelect,
            pendingSdrAutoSelectionForDeviceChange
        });
        var select = policyType.GetMethod("Select", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("CaptureResolutionSelectionPolicy.Select missing.");
        return select.Invoke(null, new[] { request })
            ?? throw new InvalidOperationException("CaptureResolutionSelectionPolicy.Select returned null.");
    }

    private static ConstructorInfo FindConstructor(Type type, int parameterCount)
    {
        foreach (var constructor in type.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            if (constructor.GetParameters().Length == parameterCount)
            {
                return constructor;
            }
        }

        throw new InvalidOperationException($"{type.Name} constructor with {parameterCount} parameters was not found.");
    }

    private static object CreateResolutionOptionList(Type resolutionType, params object[] options)
    {
        var list = (IList)(Activator.CreateInstance(typeof(System.Collections.Generic.List<>).MakeGenericType(resolutionType))
                           ?? throw new InvalidOperationException("Failed to create resolution option list."));
        foreach (var option in options)
        {
            list.Add(option);
        }

        return list;
    }

    private static object CreateResolutionOption(
        Type resolutionType,
        string value,
        uint width,
        uint height,
        bool isEnabled)
    {
        var option = CreateConfigInstance(resolutionType);
        SetPropertyOrBackingField(option, "Value", value);
        SetPropertyOrBackingField(option, "Width", width);
        SetPropertyOrBackingField(option, "Height", height);
        SetPropertyOrBackingField(option, "IsEnabled", isEnabled);
        return option;
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

    private static Task SourceTelemetryPresentationBuilder_PreservesSummaryAndTargetText()
    {
        var builderType = RequireType("Sussudio.ViewModels.SourceTelemetryPresentationBuilder");
        var snapshotType = RequireType("Sussudio.Models.SourceSignalTelemetrySnapshot");
        var buildSourceSummary = builderType.GetMethod(
            "BuildSourceSummary",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("SourceTelemetryPresentationBuilder.BuildSourceSummary was not found.");
        var buildAgeText = builderType.GetMethod(
            "BuildAgeText",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("SourceTelemetryPresentationBuilder.BuildAgeText was not found.");
        var buildTargetSummary = builderType.GetMethod(
            "BuildTargetSummary",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("SourceTelemetryPresentationBuilder.BuildTargetSummary was not found.");

        var now = new DateTimeOffset(2026, 5, 14, 22, 10, 30, TimeSpan.Zero);
        var unavailable = snapshotType.GetMethod(
            "CreateUnavailable",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static,
            binder: null,
            types: new[] { typeof(string), typeof(string) },
            modifiers: null)!.Invoke(null, new object?[] { "telemetry-not-started", null })!;
        AssertEqual(
            "Source: waiting for signal telemetry",
            buildSourceSummary.Invoke(null, new[] { unavailable, now }),
            "Source telemetry unavailable summary");

        var full = Activator.CreateInstance(snapshotType)
            ?? throw new InvalidOperationException("Failed to create SourceSignalTelemetrySnapshot.");
        SetPropertyOrBackingField(full, "Availability", ParseEnum("Sussudio.Models.SourceTelemetryAvailability", "Available"));
        SetPropertyOrBackingField(full, "Confidence", ParseEnum("Sussudio.Models.SourceTelemetryConfidence", "High"));
        SetPropertyOrBackingField(full, "Width", 3840);
        SetPropertyOrBackingField(full, "Height", 2160);
        SetPropertyOrBackingField(full, "FrameRateExact", 120000d / 1001d);
        SetPropertyOrBackingField(full, "FrameRateArg", "120000/1001");
        SetPropertyOrBackingField(full, "IsHdr", true);
        SetPropertyOrBackingField(full, "TimestampUtc", now.AddSeconds(-17));
        AssertEqual(
            "Source: 3840x2160 @ 120000/1001 | HDR | Available/High | updated 17s ago",
            buildSourceSummary.Invoke(null, new[] { full, now }),
            "Source telemetry full summary");

        var partial = Activator.CreateInstance(snapshotType)
            ?? throw new InvalidOperationException("Failed to create partial SourceSignalTelemetrySnapshot.");
        SetPropertyOrBackingField(partial, "Availability", ParseEnum("Sussudio.Models.SourceTelemetryAvailability", "Stale"));
        SetPropertyOrBackingField(partial, "Confidence", ParseEnum("Sussudio.Models.SourceTelemetryConfidence", "Low"));
        SetPropertyOrBackingField(partial, "FrameRateExact", 59.94d);
        SetPropertyOrBackingField(partial, "TimestampUtc", now.AddSeconds(2));
        AssertEqual(
            "Source: ?x? @ 59.94 | HDR? | Stale/Low | updated now",
            buildSourceSummary.Invoke(null, new[] { partial, now }),
            "Source telemetry partial summary");

        AssertEqual(
            "updated ?",
            buildAgeText.Invoke(null, new object?[] { null, now }),
            "Source telemetry null age");
        AssertEqual(
            "Target: Auto (3840 x 2160) @ 60 (exact 60000/1001) | HDR=Ready",
            buildTargetSummary.Invoke(null, new object?[] { "Auto (3840 x 2160)", 59.94d, 60d, 60000d / 1001d, "60000/1001", "Ready" }),
            "Source telemetry target summary exact rational");
        AssertEqual(
            "Target: 1080p @ 0 (exact ?) | HDR=Unknown",
            buildTargetSummary.Invoke(null, new object?[] { "1080p", 0d, null, null, null, " " }),
            "Source telemetry target summary unknown HDR");

        return Task.CompletedTask;
    }

    private static Task SourceTelemetryPresentationBuilder_LivesInFocusedHelper()
    {
        var telemetryText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.Telemetry.cs").Replace("\r\n", "\n");
        var builderText = ReadRepoFile("Sussudio/ViewModels/SourceTelemetryPresentationBuilder.cs").Replace("\r\n", "\n");

        AssertContains(telemetryText, "SourceTelemetryPresentationBuilder.BuildSourceSummary(_latestSourceTelemetry, DateTimeOffset.UtcNow);");
        AssertContains(telemetryText, "SourceTelemetryPresentationBuilder.BuildSourceSummary(snapshot, DateTimeOffset.UtcNow);");
        AssertContains(telemetryText, "SourceTelemetryPresentationBuilder.BuildTargetSummary(");
        AssertContains(telemetryText, "GetSelectedResolutionDisplayText(),");
        AssertDoesNotContain(telemetryText, "private static string BuildSourceTelemetrySummaryText(");
        AssertDoesNotContain(telemetryText, "private static string BuildTelemetryAgeText(");
        AssertDoesNotContain(telemetryText, "Source: waiting for signal telemetry");
        AssertDoesNotContain(telemetryText, "Target: {GetSelectedResolutionDisplayText()}");
        AssertContains(builderText, "internal static class SourceTelemetryPresentationBuilder");
        AssertContains(builderText, "internal static string BuildSourceSummary(SourceSignalTelemetrySnapshot snapshot, DateTimeOffset nowUtc)");
        AssertContains(builderText, "internal static string BuildAgeText(DateTimeOffset? timestampUtc, DateTimeOffset nowUtc)");
        AssertContains(builderText, "TelemetryAgeHelper.ComputeAgeSeconds(timestampUtc, nowUtc)");
        AssertContains(builderText, "snapshot.FrameRateArg ??");
        AssertContains(builderText, "snapshot.FrameRateExact?.ToString(\"0.###\")");
        AssertContains(builderText, "snapshot.IsHdr.HasValue ? (snapshot.IsHdr.Value ? \"HDR\" : \"SDR\") : \"HDR?\"");
        AssertContains(builderText, "internal static string BuildTargetSummary(");
        AssertContains(builderText, "string.IsNullOrWhiteSpace(hdrRuntimeState) ? \"Unknown\" : hdrRuntimeState");
        AssertDoesNotContain(builderText, "GetSelectedResolutionDisplayText()");
        AssertDoesNotContain(builderText, "SourceTelemetrySummaryText =");

        return Task.CompletedTask;
    }

    private static Task LiveSignalTextProjection_PreservesPixelFormatFallbackOrder()
    {
        var runtimeText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.Runtime.cs")
            .Replace("\r\n", "\n");
        var liveSignalPresentationText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.LiveSignalPresentation.cs")
            .Replace("\r\n", "\n");
        var liveSignalText = ReadRepoFile("Sussudio/ViewModels/LiveSignalTextPresentationBuilder.cs")
            .Replace("\r\n", "\n");
        var builderType = RequireType("Sussudio.ViewModels.LiveSignalTextPresentationBuilder");
        var snapshotType = RequireType("Sussudio.Models.CaptureRuntimeSnapshot");
        var buildMethod = builderType.GetMethod(
            "Build",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("LiveSignalTextPresentationBuilder.Build was not found.");

        AssertContains(runtimeText, "UpdateLiveCaptureInfo(runtimeSnapshot);");
        AssertContains(runtimeText, "ResetLiveCaptureInfo();");
        AssertDoesNotContain(runtimeText, "IsAudioPreviewActive =");
        AssertDoesNotContain(runtimeText, "private void UpdateLiveCaptureInfo(");
        AssertDoesNotContain(runtimeText, "private void ResetLiveCaptureInfo()");
        AssertContains(liveSignalPresentationText, "private void UpdateLiveCaptureInfo(CaptureRuntimeSnapshot? runtimeSnapshot = null)");
        AssertContains(liveSignalPresentationText, "IsAudioPreviewActive = runtime.IsAudioPreviewActive;");
        AssertContains(liveSignalPresentationText, "var liveSignalText = LiveSignalTextPresentationBuilder.Build(");
        AssertContains(liveSignalPresentationText, "_captureService.EncoderCodecName,");
        AssertContains(liveSignalPresentationText, "LiveInfoUnavailable);");
        AssertContains(liveSignalPresentationText, "LiveResolution = liveSignalText.Resolution;");
        AssertContains(liveSignalPresentationText, "LiveFrameRate = liveSignalText.FrameRate;");
        AssertContains(liveSignalPresentationText, "LivePixelFormat = liveSignalText.PixelFormat;");
        AssertContains(liveSignalPresentationText, "private void ResetLiveCaptureInfo()");
        AssertContains(liveSignalPresentationText, "IsAudioPreviewActive = false;");
        AssertContains(liveSignalPresentationText, "LiveResolution = LiveInfoUnavailable;");
        AssertContains(liveSignalPresentationText, "LiveFrameRate = LiveInfoUnavailable;");
        AssertContains(liveSignalPresentationText, "LivePixelFormat = LiveInfoUnavailable;");
        AssertDoesNotContain(runtimeText, "runtime.ReaderSourceSubtype ??");
        AssertDoesNotContain(runtimeText, "runtime.LatestObservedFramePixelFormat ??");
        AssertContains(liveSignalText, "internal static class LiveSignalTextPresentationBuilder");
        AssertContains(liveSignalText, "internal static LiveSignalTextPresentation Build(");
        AssertContains(liveSignalText, "runtime.ActualWidth ?? runtime.NegotiatedWidth ?? runtime.RequestedWidth");
        AssertContains(liveSignalText, "runtime.ActualHeight ?? runtime.NegotiatedHeight ?? runtime.RequestedHeight");
        AssertContains(liveSignalText, "runtime.ActualFrameRate ?? runtime.NegotiatedFrameRate ?? runtime.RequestedFrameRate");
        AssertContains(liveSignalText, "frameRateValue.Value.ToString(\"0.00\")");
        AssertContains(liveSignalText, "runtime.ReaderSourceSubtype ??");
        AssertContains(liveSignalText, "runtime.LatestObservedFramePixelFormat ??");
        AssertContains(liveSignalText, "\"hevc_nvenc\" => \" / HEVC\"");
        AssertContains(liveSignalText, "\"h264_nvenc\" => \" / H264\"");
        AssertContains(liveSignalText, "\"av1_nvenc\" => \" / AV1\"");
        AssertContains(liveSignalText, "? unavailableText");

        if (liveSignalText.IndexOf("runtime.ReaderSourceSubtype ??", StringComparison.Ordinal) >
            liveSignalText.IndexOf("runtime.LatestObservedFramePixelFormat ??", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("MainViewModel.LivePixelFormat should prefer ReaderSourceSubtype before LatestObservedFramePixelFormat.");
        }

        var actualRuntime = Activator.CreateInstance(snapshotType)
            ?? throw new InvalidOperationException("Failed to create CaptureRuntimeSnapshot.");
        SetPropertyOrBackingField(actualRuntime, "RequestedWidth", (uint?)1280);
        SetPropertyOrBackingField(actualRuntime, "RequestedHeight", (uint?)720);
        SetPropertyOrBackingField(actualRuntime, "NegotiatedWidth", (uint?)1920);
        SetPropertyOrBackingField(actualRuntime, "NegotiatedHeight", (uint?)1080);
        SetPropertyOrBackingField(actualRuntime, "ActualWidth", (uint?)3840);
        SetPropertyOrBackingField(actualRuntime, "ActualHeight", (uint?)2160);
        SetPropertyOrBackingField(actualRuntime, "RequestedFrameRate", 30d);
        SetPropertyOrBackingField(actualRuntime, "NegotiatedFrameRate", 59.94d);
        SetPropertyOrBackingField(actualRuntime, "ActualFrameRate", 119.88d);
        SetPropertyOrBackingField(actualRuntime, "RequestedPixelFormat", "MJPG");
        SetPropertyOrBackingField(actualRuntime, "RequestedReaderSubtype", "YUY2");
        SetPropertyOrBackingField(actualRuntime, "LatestObservedFramePixelFormat", "P010");
        SetPropertyOrBackingField(actualRuntime, "NegotiatedPixelFormat", "NV12");
        SetPropertyOrBackingField(actualRuntime, "VideoNegotiatedSubtype", "I420");
        SetPropertyOrBackingField(actualRuntime, "ReaderSourceSubtype", "RGB32");

        var actualPresentation = buildMethod.Invoke(null, new object?[] { actualRuntime, "av1_nvenc", "\u2014" })
            ?? throw new InvalidOperationException("LiveSignalTextPresentationBuilder.Build returned null.");
        AssertEqual("3840x2160", GetStringProperty(actualPresentation, "Resolution"), "Live signal actual resolution");
        AssertEqual("119.88", GetStringProperty(actualPresentation, "FrameRate"), "Live signal actual frame rate");
        AssertEqual("RGB32 / AV1", GetStringProperty(actualPresentation, "PixelFormat"), "Live signal reader subtype");

        var fallbackRuntime = Activator.CreateInstance(snapshotType)
            ?? throw new InvalidOperationException("Failed to create fallback CaptureRuntimeSnapshot.");
        SetPropertyOrBackingField(fallbackRuntime, "RequestedWidth", (uint?)1280);
        SetPropertyOrBackingField(fallbackRuntime, "RequestedHeight", (uint?)720);
        SetPropertyOrBackingField(fallbackRuntime, "RequestedFrameRate", 30d);
        SetPropertyOrBackingField(fallbackRuntime, "VideoNegotiatedSubtype", null);
        SetPropertyOrBackingField(fallbackRuntime, "NegotiatedPixelFormat", null);
        SetPropertyOrBackingField(fallbackRuntime, "LatestObservedFramePixelFormat", null);
        SetPropertyOrBackingField(fallbackRuntime, "RequestedReaderSubtype", null);
        SetPropertyOrBackingField(fallbackRuntime, "RequestedPixelFormat", "MJPG");
        var fallbackPresentation = buildMethod.Invoke(null, new object?[] { fallbackRuntime, "hevc_nvenc", "\u2014" })
            ?? throw new InvalidOperationException("LiveSignalTextPresentationBuilder.Build returned null.");
        AssertEqual("1280x720", GetStringProperty(fallbackPresentation, "Resolution"), "Live signal requested resolution fallback");
        AssertEqual("30.00", GetStringProperty(fallbackPresentation, "FrameRate"), "Live signal requested frame-rate fallback");
        AssertEqual("MJPG / HEVC", GetStringProperty(fallbackPresentation, "PixelFormat"), "Live signal requested pixel-format fallback");

        var unavailablePresentation = buildMethod.Invoke(
                null,
                new object?[] { CreateLiveSignalUnavailableRuntime(snapshotType), "libx264", "\u2014" })
            ?? throw new InvalidOperationException("LiveSignalTextPresentationBuilder.Build returned null.");
        AssertEqual("\u2014", GetStringProperty(unavailablePresentation, "Resolution"), "Live signal unavailable resolution");
        AssertEqual("\u2014", GetStringProperty(unavailablePresentation, "FrameRate"), "Live signal unavailable frame-rate");
        AssertEqual("\u2014", GetStringProperty(unavailablePresentation, "PixelFormat"), "Live signal unavailable pixel format");

        return Task.CompletedTask;
    }

    private static object CreateLiveSignalUnavailableRuntime(Type snapshotType)
    {
        var runtime = Activator.CreateInstance(snapshotType)
            ?? throw new InvalidOperationException("Failed to create unavailable CaptureRuntimeSnapshot.");
        SetPropertyOrBackingField(runtime, "VideoNegotiatedSubtype", null);
        SetPropertyOrBackingField(runtime, "NegotiatedPixelFormat", null);
        SetPropertyOrBackingField(runtime, "LatestObservedFramePixelFormat", null);
        SetPropertyOrBackingField(runtime, "RequestedReaderSubtype", null);
        SetPropertyOrBackingField(runtime, "RequestedPixelFormat", null);
        return runtime;
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

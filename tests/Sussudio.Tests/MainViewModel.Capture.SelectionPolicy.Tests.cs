using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

static partial class Program
{
    internal static Task ModeSelectionState_LivesInFocusedPartial()
    {
        var resolutionOptionsText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.CaptureSelection.cs").Replace("\r\n", "\n");
        var captureModeTransactionsText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.CaptureSelection.cs").Replace("\r\n", "\n");
        var frameRateOptionsText = resolutionOptionsText;
        var captureModeOptionsControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelCaptureModeOptionRebuildController.cs").Replace("\r\n", "\n");
        var frameRateRebuildControllerText = captureModeOptionsControllerText;
        var resolutionOptionRebuildControllerText = captureModeOptionsControllerText;
        var modeSelectionText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs").Replace("\r\n", "\n");

        AssertContains(captureModeTransactionsText, "private void RebuildResolutionOptions()");
        AssertContains(captureModeTransactionsText, "=> _captureModeOptionRebuildController.RebuildResolutionOptions();");
        AssertContains(resolutionOptionRebuildControllerText, "public void RebuildResolutionOptions()");
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
        AssertContains(frameRateRebuildControllerText, "_context.ApplyResolvedFrameRateSelection(selection.Selected, fallbackRate);");
        AssertDoesNotContain(frameRateRebuildControllerText, "_viewModel.");
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
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.ModeSelectionState.cs")),
            "MainViewModel.ModeSelectionState.cs folded into MainViewModel.cs");

        return Task.CompletedTask;
    }

    internal static Task RecordingSettingsSelectionPolicy_LivesInFocusedHelper()
    {
        var captureModeTransactionsText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.CaptureSelection.cs").Replace("\r\n", "\n");
        var recordingRuntimeText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs").Replace("\r\n", "\n");
        var recordingCapabilityControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelCaptureReadinessControllers.cs").Replace("\r\n", "\n");
        var automationSettingsText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs").Replace("\r\n", "\n");
        var automationRecordingControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelSettingsAutomationControllers.cs").Replace("\r\n", "\n");
        var recordingSettingsPolicyText = ReadRepoFile("Sussudio/ViewModels/CaptureSettingsProjectionBuilder.cs").Replace("\r\n", "\n");

        AssertContains(recordingRuntimeText, "private void RebuildRecordingFormatOptions()");
        AssertContains(recordingRuntimeText, "=> _recordingCapabilityController.RebuildRecordingFormatOptions();");
        AssertDoesNotContain(recordingCapabilityControllerText, "private void RebuildRecordingFormatOptions()");
        AssertContains(recordingCapabilityControllerText, "public void RebuildRecordingFormatOptions()");
        AssertContains(recordingCapabilityControllerText, "namespace Sussudio.Controllers;");
        AssertContains(recordingCapabilityControllerText, "internal sealed class MainViewModelRecordingCapabilityController");
        AssertContains(recordingCapabilityControllerText, "internal sealed class MainViewModelRecordingCapabilityControllerContext");
        AssertContains(recordingCapabilityControllerText, "private readonly MainViewModelRecordingCapabilityControllerContext _context;");
        AssertDoesNotContain(recordingCapabilityControllerText, "private readonly MainViewModel _viewModel;");
        AssertDoesNotContain(recordingCapabilityControllerText, "_viewModel.");
        AssertContains(recordingCapabilityControllerText, "RecordingSettingsSelectionPolicy.Select(");
        AssertContains(recordingCapabilityControllerText, "RecordingSettingsSelectionPolicy.IsHdrCompatible(_context.GetSelectedRecordingFormat())");
        AssertContains(recordingCapabilityControllerText, "_context.NotifySelectedRecordingFormatChanged();");
        AssertContains(recordingCapabilityControllerText, "Logger.Log($\"Selected recording format: {_context.GetSelectedRecordingFormat()}\");");
        AssertContains(captureModeTransactionsText, "RebuildRecordingFormatOptions();");
        AssertDoesNotContain(captureModeTransactionsText, "RecordingSettingsSelectionPolicy.Select(");
        AssertContains(automationSettingsText, "=> _recordingSettingsAutomationController.SetRecordingFormatAsync(format, cancellationToken);");
        AssertContains(automationRecordingControllerText, "RecordingSettingsSelectionPolicy.IsHdrCompatible(matched)");
        AssertContains(automationRecordingControllerText, "RecordingSettingsSelectionPolicy.ParseRecordingFormat(matched)");
        AssertContains(automationRecordingControllerText, "RecordingSettingsSelectionPolicy.ParseVideoQuality(_context.GetSelectedQuality())");
        AssertContains(automationRecordingControllerText, "namespace Sussudio.Controllers;");
        AssertContains(automationRecordingControllerText, "internal sealed class MainViewModelRecordingSettingsAutomationController");
        AssertContains(automationRecordingControllerText, "internal sealed class MainViewModelRecordingSettingsAutomationControllerContext");
        AssertContains(automationRecordingControllerText, "private readonly MainViewModelRecordingSettingsAutomationControllerContext _context;");
        AssertDoesNotContain(automationRecordingControllerText, "private readonly MainViewModel _viewModel;");
        AssertDoesNotContain(automationRecordingControllerText, "_viewModel.");
        AssertContains(automationRecordingControllerText, "RecordingSettingsSelectionPolicy.ClampCustomBitrateMbps(bitrateMbps)");
        AssertContains(automationRecordingControllerText, "public async Task SetRecordingFormatAsync");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.AutomationRecordingFormat.cs")),
            "stale recording format automation partial");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.AutomationRecordingSettings.cs")),
            "stale recording settings automation facade partial");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.RecordingFormatOptions.cs")),
            "stale recording format options partial");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.RecordingCapabilityRefresh.cs")),
            "stale recording capability refresh partial");
        AssertDoesNotContain(captureModeTransactionsText, "private static bool IsHdrCompatibleRecordingFormat(");
        AssertContains(recordingSettingsPolicyText, "internal static class RecordingSettingsSelectionPolicy");
        AssertContains(recordingSettingsPolicyText, "internal static bool IsHdrCompatible(");
        AssertContains(recordingSettingsPolicyText, "internal static RecordingFormat ParseRecordingFormat(");
        AssertContains(recordingSettingsPolicyText, "internal static VideoQuality ParseVideoQuality(");
        AssertContains(recordingSettingsPolicyText, "internal static double ClampCustomBitrateMbps(");
        AssertContains(recordingSettingsPolicyText, "internal static RecordingFormatSelection Select(");
        AssertContains(recordingSettingsPolicyText, "internal sealed record RecordingFormatSelection(");
        AssertContains(recordingSettingsPolicyText, "Keep the last known real formats visible if capability refresh temporarily produced none.");

        return Task.CompletedTask;
    }

    internal static Task CaptureFormatSelectionPolicy_LivesInFocusedHelper()
    {
        var captureModeTransactionsText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.CaptureSelection.cs").Replace("\r\n", "\n");
        var captureModeOptionsControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelCaptureModeOptionRebuildController.cs").Replace("\r\n", "\n");
        var policyText = ReadRepoFile("Sussudio/ViewModels/ViewModelSelectionPolicies.cs").Replace("\r\n", "\n");

        AssertContains(captureModeTransactionsText, "/// Capture-mode transactions that coordinate option rebuilds");
        AssertContains(captureModeTransactionsText, "private void UpdateSelectedFormat()");
        AssertContains(captureModeTransactionsText, "private void RebuildVideoFormatOptions()");
        AssertContains(captureModeTransactionsText, "=> _captureModeOptionRebuildController.UpdateSelectedFormat();");
        AssertContains(captureModeTransactionsText, "=> _captureModeOptionRebuildController.RebuildVideoFormatOptions();");
        AssertContains(captureModeOptionsControllerText, "public void UpdateSelectedFormat()");
        AssertContains(captureModeOptionsControllerText, "public void RebuildVideoFormatOptions()");
        AssertContains(captureModeOptionsControllerText, "CaptureFormatSelectionPolicy.Select(");
        AssertContains(captureModeOptionsControllerText, ".SelectModeTupleFormats(BuildCaptureFormatSelectionRequest(");
        AssertContains(captureModeOptionsControllerText, "_context.AvailableVideoFormats.Clear();");
        AssertContains(captureModeOptionsControllerText, "internal sealed class MainViewModelCaptureModeOptionRebuildControllerContext");
        AssertDoesNotContain(captureModeOptionsControllerText, "_viewModel.");
        AssertDoesNotContain(captureModeTransactionsText, "FrameRateTimingPolicy.SelectPreferredFrameRateFormat(");
        AssertDoesNotContain(captureModeTransactionsText, "private static bool IsHdrModeCandidate(");
        AssertDoesNotContain(captureModeTransactionsText, "ShouldPreserveMjpegHighFrameRateMode(");
        AssertContains(policyText, "internal static class CaptureFormatSelectionPolicy");
        AssertContains(policyText, "internal static MediaFormat? Select(CaptureFormatSelectionRequest request)");
        AssertContains(policyText, "internal static IReadOnlyList<MediaFormat> SelectModeTupleFormats(CaptureFormatSelectionRequest request)");
        AssertContains(policyText, "FrameRateTimingPolicy.SelectPreferredFrameRateFormat(");
        AssertContains(policyText, "CaptureModeOptionsBuilder.IsHdrModeCandidate(format)");
        AssertContains(policyText, "internal sealed record CaptureFormatSelectionRequest(");
        AssertEqual(
            true,
            policyText.Split('\n').Length >= 100,
            "capture format selection policy is a substantial ownership file");

        return Task.CompletedTask;
    }

    internal static Task CaptureFormatSelectionPolicy_PreservesSelectionBehavior()
    {
        var mediaFormatType = RequireType("Sussudio.Models.MediaFormat");
        var frameRateType = RequireType("Sussudio.Models.FrameRateOption");

        var sdrNv12 = CreateFrameRateTimingFormat(mediaFormatType, 3840, 2160, 120, 120, 1, "NV12", isHdr: false);
        var sdrMjpg = CreateFrameRateTimingFormat(mediaFormatType, 3840, 2160, 120, 120, 1, "MJPG", isHdr: false);
        var hdrP010 = CreateFrameRateTimingFormat(mediaFormatType, 3840, 2160, 120, 120, 1, "P010", isHdr: true);
        var ntsc119 = CreateFrameRateTimingFormat(mediaFormatType, 3840, 2160, 120000d / 1001d, 120000, 1001, "NV12", isHdr: false);
        var otherResolution = CreateFrameRateTimingFormat(mediaFormatType, 1920, 1080, 120, 120, 1, "NV12", isHdr: false);
        var formats = CreateMediaFormatList(mediaFormatType, hdrP010, sdrNv12, sdrMjpg, ntsc119, otherResolution);
        var frameRates = CreateFrameRateOptionList(
            frameRateType,
            CreateFrameRateOption(frameRateType, 120, 120, "120/1", isEnabled: true),
            CreateFrameRateOption(frameRateType, 120, 120000d / 1001d, "120000/1001", isEnabled: true));

        var sdrAuto = InvokeCaptureFormatSelection(
            formats,
            frameRates,
            width: 3840,
            height: 2160,
            selectedFrameRate: 120,
            selectedVideoFormat: "Auto",
            isHdrEnabled: false,
            preferredTimingFamilyName: "Integer");
        AssertEqual(false, GetBoolProperty(sdrAuto!, "IsHdr"), "SDR selected format excludes HDR when SDR alternatives exist");
        AssertEqual("NV12", GetStringProperty(sdrAuto!, "PixelFormat"), "4K HFR SDR auto preserves existing source-order tie");

        var hdrAuto = InvokeCaptureFormatSelection(
            formats,
            frameRates,
            width: 3840,
            height: 2160,
            selectedFrameRate: 120,
            selectedVideoFormat: "Auto",
            isHdrEnabled: true,
            preferredTimingFamilyName: "Integer");
        AssertEqual(true, GetBoolProperty(hdrAuto!, "IsHdr"), "HDR selected format uses HDR candidates");
        AssertEqual("P010", GetStringProperty(hdrAuto!, "PixelFormat"), "HDR selected format keeps P010 candidate");

        var explicitNv12 = InvokeCaptureFormatSelection(
            formats,
            frameRates,
            width: 3840,
            height: 2160,
            selectedFrameRate: 120,
            selectedVideoFormat: "NV12",
            isHdrEnabled: false,
            preferredTimingFamilyName: "Integer");
        AssertEqual("NV12", GetStringProperty(explicitNv12!, "PixelFormat"), "explicit selected pixel format narrows candidates");
        AssertEqual(120u, (uint)GetPropertyValue(explicitNv12!, "FrameRateNumerator")!, "integer timing family wins for explicit NV12");

        var ntscPreferred = InvokeCaptureFormatSelection(
            formats,
            frameRates,
            width: 3840,
            height: 2160,
            selectedFrameRate: 120000d / 1001d,
            selectedVideoFormat: "NV12",
            isHdrEnabled: false,
            preferredTimingFamilyName: "Ntsc1001");
        AssertEqual(120000u, (uint)GetPropertyValue(ntscPreferred!, "FrameRateNumerator")!, "friendly bucket selection preserves NTSC timing");

        var unavailablePixelFormat = InvokeCaptureFormatSelection(
            formats,
            frameRates,
            width: 3840,
            height: 2160,
            selectedFrameRate: 120,
            selectedVideoFormat: "YUY2",
            isHdrEnabled: false,
            preferredTimingFamilyName: "Integer");
        AssertEqual(null, unavailablePixelFormat, "unavailable explicit pixel format returns no selected format");

        var tupleFormats = InvokeCaptureFormatModeTupleFormats(
                formats,
                frameRates,
                width: 3840,
                height: 2160,
                selectedFrameRate: 120000d / 1001d,
                selectedVideoFormat: "Auto",
                isHdrEnabled: false,
                preferredTimingFamilyName: "Ntsc1001")
            .Cast<object>()
            .ToArray();
        AssertEqual(3, tupleFormats.Length, "friendly 119.88/120 mode tuple includes SDR bucket variants");
        AssertEqual(
            false,
            tupleFormats.Any(format => GetBoolProperty(format, "IsHdr")),
            "mode tuple formats exclude HDR while SDR is selected");

        return Task.CompletedTask;
    }

    private static object? InvokeCaptureFormatSelection(
        object formats,
        object frameRates,
        uint width,
        uint height,
        double selectedFrameRate,
        string selectedVideoFormat,
        bool isHdrEnabled,
        string preferredTimingFamilyName)
    {
        var request = CreateCaptureFormatSelectionRequest(
            formats,
            frameRates,
            width,
            height,
            selectedFrameRate,
            selectedVideoFormat,
            isHdrEnabled,
            preferredTimingFamilyName);
        var policyType = RequireType("Sussudio.ViewModels.CaptureFormatSelectionPolicy");
        var select = policyType.GetMethod("Select", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("CaptureFormatSelectionPolicy.Select missing.");
        return select.Invoke(null, new[] { request });
    }

    private static IEnumerable InvokeCaptureFormatModeTupleFormats(
        object formats,
        object frameRates,
        uint width,
        uint height,
        double selectedFrameRate,
        string selectedVideoFormat,
        bool isHdrEnabled,
        string preferredTimingFamilyName)
    {
        var request = CreateCaptureFormatSelectionRequest(
            formats,
            frameRates,
            width,
            height,
            selectedFrameRate,
            selectedVideoFormat,
            isHdrEnabled,
            preferredTimingFamilyName);
        var policyType = RequireType("Sussudio.ViewModels.CaptureFormatSelectionPolicy");
        var selectModeTupleFormats = policyType.GetMethod("SelectModeTupleFormats", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("CaptureFormatSelectionPolicy.SelectModeTupleFormats missing.");
        return (IEnumerable)(selectModeTupleFormats.Invoke(null, new[] { request })
            ?? throw new InvalidOperationException("CaptureFormatSelectionPolicy.SelectModeTupleFormats returned null."));
    }

    private static object CreateCaptureFormatSelectionRequest(
        object formats,
        object frameRates,
        uint width,
        uint height,
        double selectedFrameRate,
        string selectedVideoFormat,
        bool isHdrEnabled,
        string preferredTimingFamilyName)
    {
        var requestType = RequireType("Sussudio.ViewModels.CaptureFormatSelectionRequest");
        var timingFamily = ParseEnum("Sussudio.ViewModels.FrameRateTimingFamily", preferredTimingFamilyName);
        var constructor = FindConstructor(requestType, parameterCount: 8);
        return constructor.Invoke(new[]
        {
            formats,
            frameRates,
            width,
            height,
            selectedFrameRate,
            selectedVideoFormat,
            isHdrEnabled,
            timingFamily
        });
    }

    private static object InvokeDeviceFormatProbeRetargetDecision(
        bool preserveActiveSelection,
        bool allowProbeDrivenRetarget,
        bool isHdrEnabled,
        bool modeChanged,
        string? previousResolution,
        double previousFrameRate,
        string? selectedResolution,
        double selectedFrameRate,
        object? selectedFormat,
        object supportedFormats,
        bool previousResolutionAvailable,
        bool includeSessionMismatchCheck,
        uint? sessionActualWidth,
        uint? sessionActualHeight)
    {
        var requestType = RequireType("Sussudio.ViewModels.DeviceFormatProbeRetargetRequest");
        var policyType = RequireType("Sussudio.ViewModels.DeviceFormatProbeRetargetPolicy");
        var constructor = FindConstructor(requestType, parameterCount: 14);
        var request = constructor.Invoke(new object?[]
        {
            preserveActiveSelection,
            allowProbeDrivenRetarget,
            isHdrEnabled,
            modeChanged,
            previousResolution,
            previousFrameRate,
            selectedResolution,
            selectedFrameRate,
            selectedFormat,
            supportedFormats,
            previousResolutionAvailable,
            includeSessionMismatchCheck,
            sessionActualWidth,
            sessionActualHeight
        });
        var decide = policyType.GetMethod("Decide", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("DeviceFormatProbeRetargetPolicy.Decide missing.");
        return decide.Invoke(null, new[] { request })
            ?? throw new InvalidOperationException("DeviceFormatProbeRetargetPolicy.Decide returned null.");
    }

    private static string GetEnumName(object instance, string propertyName)
        => instance.GetType().GetProperty(propertyName)!.GetValue(instance)?.ToString()
           ?? throw new InvalidOperationException($"{propertyName} returned null.");

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

    private static object InvokeAutoCaptureSelection(
        object options,
        object formatsByResolution,
        object telemetry,
        bool isHdrEnabled)
    {
        var requestType = RequireType("Sussudio.ViewModels.AutoCaptureSelectionRequest");
        var policyType = RequireType("Sussudio.ViewModels.AutoCaptureSelectionPolicy");
        var constructor = FindConstructor(requestType, parameterCount: 4);
        var request = constructor.Invoke(new object?[]
        {
            options,
            formatsByResolution,
            telemetry,
            isHdrEnabled
        });
        var select = policyType.GetMethod("Select", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("AutoCaptureSelectionPolicy.Select missing.");
        return select.Invoke(null, new[] { request })
            ?? throw new InvalidOperationException("AutoCaptureSelectionPolicy.Select returned null.");
    }

    private static object InvokeFrameRateAutoSelection(
        object options,
        bool autoFrameRateOptionAvailable,
        bool forceAutoSelection,
        bool isAutoFrameRateSelected,
        bool hasUserOverriddenFrameRateForCurrentMode,
        bool isHdrEnabled,
        bool pendingSdrAutoSelectionForDeviceChange,
        int? pendingSdrAutoFriendlyFrameRateBucket,
        double? sourceRate,
        bool sourceTimingFamilyKnown,
        string sourceTimingFamilyName,
        double previousRate)
    {
        var sourceType = RequireType("Sussudio.ViewModels.FrameRateAutoSelectionSource");
        var requestType = RequireType("Sussudio.ViewModels.FrameRateAutoSelectionRequest");
        var policyType = RequireType("Sussudio.ViewModels.FrameRateAutoSelectionPolicy");
        var timingFamily = ParseEnum("Sussudio.ViewModels.FrameRateTimingFamily", sourceTimingFamilyName);
        var sourceConstructor = FindConstructor(sourceType, parameterCount: 3);
        var source = sourceConstructor.Invoke(new object?[]
        {
            sourceRate,
            sourceTimingFamilyKnown,
            timingFamily
        });
        var requestConstructor = FindConstructor(requestType, parameterCount: 10);
        var request = requestConstructor.Invoke(new object?[]
        {
            options,
            autoFrameRateOptionAvailable,
            forceAutoSelection,
            isAutoFrameRateSelected,
            hasUserOverriddenFrameRateForCurrentMode,
            isHdrEnabled,
            pendingSdrAutoSelectionForDeviceChange,
            pendingSdrAutoFriendlyFrameRateBucket,
            source,
            previousRate
        });
        var select = policyType.GetMethod("Select", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("FrameRateAutoSelectionPolicy.Select missing.");
        return select.Invoke(null, new[] { request })
            ?? throw new InvalidOperationException("FrameRateAutoSelectionPolicy.Select returned null.");
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

    private static object CreateFrameRateOptionList(Type frameRateType, params object[] options)
    {
        var list = (IList)(Activator.CreateInstance(typeof(System.Collections.Generic.List<>).MakeGenericType(frameRateType))
                           ?? throw new InvalidOperationException("Failed to create frame-rate option list."));
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

    private static object CreateFrameRateOption(
        Type frameRateType,
        double friendlyValue,
        double value,
        string rational,
        bool isEnabled)
    {
        var option = CreateConfigInstance(frameRateType);
        SetPropertyOrBackingField(option, "FriendlyValue", friendlyValue);
        SetPropertyOrBackingField(option, "Value", value);
        SetPropertyOrBackingField(option, "Rational", rational);
        SetPropertyOrBackingField(option, "IsEnabled", isEnabled);
        return option;
    }

    internal static Task DeviceFormatProbeRetargetPolicy_LivesInFocusedHelper()
    {
        var probeControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelDeviceDiscoveryControllers.cs").Replace("\r\n", "\n");
        var retargetApplierText = probeControllerText;
        var retargetPolicyText = ReadRepoFile("Sussudio/ViewModels/ViewModelSelectionPolicies.cs").Replace("\r\n", "\n");

        AssertContains(probeControllerText, "namespace Sussudio.Controllers;");
        AssertContains(probeControllerText, "internal sealed class MainViewModelDeviceFormatProbeController");
        AssertContains(probeControllerText, "internal sealed class MainViewModelDeviceFormatProbeControllerContext");
        AssertContains(probeControllerText, "private readonly MainViewModelDeviceFormatProbeControllerContext _context;");
        AssertDoesNotContain(probeControllerText, "private readonly MainViewModel _viewModel;");
        AssertDoesNotContain(probeControllerText, "_viewModel.");
        AssertContains(probeControllerText, "public void OnDeviceFormatProbeCompleted");
        AssertContains(probeControllerText, "_retargetApplier.TryApplyDeviceFormatProbeRetarget(");
        AssertContains(probeControllerText, "_context.RebuildSelectedDeviceCapabilities(selectedDevice, false);");
        AssertContains(probeControllerText, "FORMAT_PROBE_UI_ENQUEUE_FAILED deviceId='{e.DeviceId}' requestId={e.RequestId}");
        AssertDoesNotContain(probeControllerText, "var nv12Candidates = target.SupportedFormats");
        AssertDoesNotContain(probeControllerText, "ShouldPreserveMjpegHighFrameRateMode(_viewModel.SelectedFormat)");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Controllers", "ViewModel", "MainViewModelDeviceFormatProbeRetargetApplier.cs")),
            "device format probe retarget applier lives with probe event owner");
        AssertContains(retargetApplierText, "namespace Sussudio.Controllers;");
        AssertContains(retargetApplierText, "internal sealed class MainViewModelDeviceFormatProbeRetargetApplier");
        AssertContains(retargetApplierText, "internal sealed class MainViewModelDeviceFormatProbeRetargetApplierContext");
        AssertContains(retargetApplierText, "private readonly MainViewModelDeviceFormatProbeRetargetApplierContext _context;");
        AssertDoesNotContain(retargetApplierText, "private readonly MainViewModel _viewModel;");
        AssertDoesNotContain(retargetApplierText, "_viewModel.");
        AssertContains(retargetApplierText, "public bool TryApplyDeviceFormatProbeRetarget(");
        AssertContains(retargetApplierText, "DeviceFormatProbeRetargetPolicy.Decide(new DeviceFormatProbeRetargetRequest(");
        AssertContains(retargetApplierText, "RebuildFrameRateOptions();");
        AssertContains(retargetApplierText, "EnqueueUiOperation(");
        AssertContains(retargetPolicyText, "internal static class DeviceFormatProbeRetargetPolicy");
        AssertContains(retargetPolicyText, "internal static DeviceFormatProbeRetargetDecision Decide(DeviceFormatProbeRetargetRequest request)");
        AssertContains(retargetPolicyText, "internal sealed record DeviceFormatProbeRetargetRequest(");
        AssertContains(retargetPolicyText, "internal sealed record DeviceFormatProbeRetargetDecision(");
        AssertContains(retargetPolicyText, "CaptureSettings.IsMjpegHighFrameRateMode(");
        AssertContains(retargetPolicyText, "\"format probe (HDR retarget)\"");
        AssertContains(retargetPolicyText, "\"format probe (SDR nv12 retarget)\"");
        AssertContains(retargetPolicyText, "\"format probe (session mismatch)\"");
        AssertDoesNotContain(retargetPolicyText, "Logger.Log(");
        AssertDoesNotContain(retargetPolicyText, "ReinitializeDeviceAsync(");
        AssertDoesNotContain(retargetPolicyText, "SelectedResolution =");
        AssertDoesNotContain(retargetPolicyText, "RebuildFrameRateOptions(");

        return Task.CompletedTask;
    }

    internal static Task DeviceFormatProbeRetargetApplication_LivesInFocusedPartial()
    {
        var probeControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelDeviceDiscoveryControllers.cs").Replace("\r\n", "\n");
        var retargetApplierText = probeControllerText;
        var retargetPolicyText = ReadRepoFile("Sussudio/ViewModels/ViewModelSelectionPolicies.cs").Replace("\r\n", "\n");

        AssertContains(probeControllerText, "namespace Sussudio.Controllers;");
        AssertContains(probeControllerText, "internal sealed class MainViewModelDeviceFormatProbeController");
        AssertContains(probeControllerText, "internal sealed class MainViewModelDeviceFormatProbeControllerContext");
        AssertContains(probeControllerText, "private readonly MainViewModelDeviceFormatProbeControllerContext _context;");
        AssertDoesNotContain(probeControllerText, "private readonly MainViewModel _viewModel;");
        AssertDoesNotContain(probeControllerText, "_viewModel.");
        AssertContains(probeControllerText, "public void OnDeviceFormatProbeCompleted");
        AssertContains(probeControllerText, "target.SupportedFormats.Clear();");
        AssertContains(probeControllerText, "_context.RebuildSelectedDeviceCapabilities(selectedDevice, false);");
        AssertContains(probeControllerText, "_retargetApplier.TryApplyDeviceFormatProbeRetarget(");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Controllers", "ViewModel", "MainViewModelDeviceFormatProbeRetargetApplier.cs")),
            "device format probe retarget applier lives with probe event owner");
        AssertContains(retargetApplierText, "namespace Sussudio.Controllers;");
        AssertContains(retargetApplierText, "internal sealed class MainViewModelDeviceFormatProbeRetargetApplier");
        AssertContains(retargetApplierText, "internal sealed class MainViewModelDeviceFormatProbeRetargetApplierContext");
        AssertContains(retargetApplierText, "private readonly MainViewModelDeviceFormatProbeRetargetApplierContext _context;");
        AssertDoesNotContain(retargetApplierText, "private readonly MainViewModel _viewModel;");
        AssertDoesNotContain(retargetApplierText, "_viewModel.");
        AssertContains(retargetApplierText, "public bool TryApplyDeviceFormatProbeRetarget(");
        AssertContains(retargetApplierText, "private DeviceFormatProbeRetargetDecision DecideDeviceFormatProbeRetarget(");
        AssertContains(retargetApplierText, "DeviceFormatProbeRetargetPolicy.Decide(new DeviceFormatProbeRetargetRequest(");
        AssertContains(retargetApplierText, "_context.SetSelectedResolution(retargetDecision.TargetResolution);");
        AssertContains(retargetApplierText, "_context.RebuildFrameRateOptions();");
        AssertContains(retargetApplierText, "_context.SetSelectedResolution(previousResolution);");
        AssertContains(retargetApplierText, "_context.GetCaptureRuntimeSnapshot();");
        AssertDoesNotContain(retargetPolicyText, "EnqueueUiOperation(");
        AssertDoesNotContain(retargetPolicyText, "GetCaptureRuntimeSnapshot(");

        return Task.CompletedTask;
    }

    internal static Task DeviceFormatProbeRetargetPolicy_PreservesRetargetDecisionBehavior()
    {
        var mediaFormatType = RequireType("Sussudio.Models.MediaFormat");

        var hdrDecision = InvokeDeviceFormatProbeRetargetDecision(
            preserveActiveSelection: true,
            allowProbeDrivenRetarget: true,
            isHdrEnabled: true,
            modeChanged: true,
            previousResolution: "3840x2160",
            previousFrameRate: 120,
            selectedResolution: "1920x1080",
            selectedFrameRate: 120,
            selectedFormat: CreateTestMediaFormat(mediaFormatType, 1920, 1080, 120, "P010", isHdr: true),
            supportedFormats: CreateMediaFormatList(mediaFormatType),
            previousResolutionAvailable: true,
            includeSessionMismatchCheck: false,
            sessionActualWidth: null,
            sessionActualHeight: null);
        AssertEqual("HdrRetarget", GetEnumName(hdrDecision, "Kind"), "HDR retarget decision");
        AssertEqual("format probe (HDR retarget)", GetStringProperty(hdrDecision, "ReinitializeReason"), "HDR retarget reason");
        AssertEqual("format probe hdr retarget", GetStringProperty(hdrDecision, "UiOperationName"), "HDR retarget UI operation");

        var mjpgHfrDecision = InvokeDeviceFormatProbeRetargetDecision(
            preserveActiveSelection: true,
            allowProbeDrivenRetarget: true,
            isHdrEnabled: false,
            modeChanged: false,
            previousResolution: "3840x2160",
            previousFrameRate: 120,
            selectedResolution: "3840x2160",
            selectedFrameRate: 120,
            selectedFormat: CreateTestMediaFormat(mediaFormatType, 3840, 2160, 120, "MJPG", isHdr: false),
            supportedFormats: CreateMediaFormatList(
                mediaFormatType,
                CreateTestMediaFormat(mediaFormatType, 1920, 1080, 120, "NV12", isHdr: false)),
            previousResolutionAvailable: true,
            includeSessionMismatchCheck: false,
            sessionActualWidth: null,
            sessionActualHeight: null);
        AssertEqual("PreserveMjpegHighFrameRate", GetEnumName(mjpgHfrDecision, "Kind"), "MJPG HFR preserve decision");

        var sdrNv12Decision = InvokeDeviceFormatProbeRetargetDecision(
            preserveActiveSelection: true,
            allowProbeDrivenRetarget: true,
            isHdrEnabled: false,
            modeChanged: false,
            previousResolution: "1280x720",
            previousFrameRate: 60,
            selectedResolution: "1280x720",
            selectedFrameRate: 60,
            selectedFormat: CreateTestMediaFormat(mediaFormatType, 1280, 720, 60, "MJPG", isHdr: false),
            supportedFormats: CreateMediaFormatList(
                mediaFormatType,
                CreateTestMediaFormat(mediaFormatType, 3840, 2160, 30, "NV12", isHdr: false),
                CreateTestMediaFormat(mediaFormatType, 1920, 1080, 60, "NV12", isHdr: false),
                CreateTestMediaFormat(mediaFormatType, 1280, 720, 60, "MJPG", isHdr: false)),
            previousResolutionAvailable: true,
            includeSessionMismatchCheck: false,
            sessionActualWidth: null,
            sessionActualHeight: null);
        AssertEqual("SdrNv12Retarget", GetEnumName(sdrNv12Decision, "Kind"), "SDR NV12 retarget decision");
        AssertEqual("1920x1080", GetStringProperty(sdrNv12Decision, "TargetResolution"), "SDR NV12 target resolution");
        AssertEqual(60d, sdrNv12Decision.GetType().GetProperty("TargetFrameRate")!.GetValue(sdrNv12Decision), "SDR NV12 target frame rate");
        AssertEqual("format probe (SDR nv12 retarget)", GetStringProperty(sdrNv12Decision, "ReinitializeReason"), "SDR NV12 reason");
        AssertEqual("format probe sdr retarget", GetStringProperty(sdrNv12Decision, "UiOperationName"), "SDR NV12 UI operation");

        var sessionMismatchDecision = InvokeDeviceFormatProbeRetargetDecision(
            preserveActiveSelection: true,
            allowProbeDrivenRetarget: true,
            isHdrEnabled: false,
            modeChanged: false,
            previousResolution: "1920x1080",
            previousFrameRate: 60,
            selectedResolution: "1920x1080",
            selectedFrameRate: 60,
            selectedFormat: CreateTestMediaFormat(mediaFormatType, 1920, 1080, 60, "NV12", isHdr: false),
            supportedFormats: CreateMediaFormatList(mediaFormatType),
            previousResolutionAvailable: true,
            includeSessionMismatchCheck: true,
            sessionActualWidth: 1280,
            sessionActualHeight: 720);
        AssertEqual("SessionMismatch", GetEnumName(sessionMismatchDecision, "Kind"), "session mismatch decision");
        AssertEqual("format probe (session mismatch)", GetStringProperty(sessionMismatchDecision, "ReinitializeReason"), "session mismatch reason");
        AssertEqual("format probe session mismatch", GetStringProperty(sessionMismatchDecision, "UiOperationName"), "session mismatch UI operation");

        var restoreDecision = InvokeDeviceFormatProbeRetargetDecision(
            preserveActiveSelection: true,
            allowProbeDrivenRetarget: false,
            isHdrEnabled: false,
            modeChanged: true,
            previousResolution: "3840x2160",
            previousFrameRate: 60,
            selectedResolution: "1920x1080",
            selectedFrameRate: 60,
            selectedFormat: CreateTestMediaFormat(mediaFormatType, 1920, 1080, 60, "NV12", isHdr: false),
            supportedFormats: CreateMediaFormatList(mediaFormatType),
            previousResolutionAvailable: true,
            includeSessionMismatchCheck: false,
            sessionActualWidth: null,
            sessionActualHeight: null);
        AssertEqual("RestoreActiveSelection", GetEnumName(restoreDecision, "Kind"), "recording-time restore decision");

        return Task.CompletedTask;
    }

    internal static Task MainViewModelCaptureSettings_OwnsSettingsProjection()
    {
        var captureText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs")
            .Replace("\r\n", "\n");
        var previewLifecycleControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelPreviewLifecycleController.cs")
            .Replace("\r\n", "\n");
        var recordingLifecycleText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs")
            .Replace("\r\n", "\n");
        var recordingTransitionControllerText =
            ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelRecordingTransitionController.cs")
                .Replace("\r\n", "\n");
        var captureStateText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs")
            .Replace("\r\n", "\n");
        var captureSettingsBuilderText = ReadRepoFile("Sussudio/ViewModels/CaptureSettingsProjectionBuilder.cs")
            .Replace("\r\n", "\n");

        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.CaptureSettings.cs")),
            "MainViewModel capture-settings adapter folded into MainViewModel.cs");
        AssertContains(captureStateText, "private CaptureSettings BuildCaptureSettings()");
        AssertContains(captureStateText, "var runtime = _captureService.GetRuntimeSnapshot();");
        AssertContains(captureStateText, "var sourceTelemetry = _captureService.GetLatestSourceTelemetrySnapshot();");
        AssertContains(captureStateText, "return CaptureSettingsProjectionBuilder.Build(new CaptureSettingsProjectionInput");
        AssertContains(captureStateText, "AvailableFrameRates = AvailableFrameRates.ToArray(),");
        AssertContains(captureStateText, "SelectedAudioInputDeviceId = SelectedAudioInputDevice?.Id,");
        AssertContains(captureStateText, "SelectedMicrophoneDeviceId = SelectedMicrophoneDevice?.Id,");
        AssertContains(captureSettingsBuilderText, "internal static class CaptureSettingsProjectionBuilder");
        AssertDoesNotContain(captureSettingsBuilderText, "partial class CaptureSettingsProjectionBuilder");
        AssertContains(captureSettingsBuilderText, "public static CaptureSettings Build(CaptureSettingsProjectionInput input)");
        AssertContains(captureSettingsBuilderText, "FrameRate = frameRateProjection.EffectiveFrameRate,");
        AssertContains(captureSettingsBuilderText, "RequestedFrameRateArg = frameRateProjection.RequestedFrameRateArg,");
        AssertContains(captureSettingsBuilderText, "RequestedFrameRateNumerator = frameRateProjection.RequestedFrameRateNumerator,");
        AssertContains(captureSettingsBuilderText, "RequestedFrameRateDenominator = frameRateProjection.RequestedFrameRateDenominator,");
        AssertContains(captureSettingsBuilderText, "RequestedPixelFormat = ResolveRequestedPixelFormat(input)");
        AssertContains(captureSettingsBuilderText, "ForceMjpegDecode = ShouldForceMjpegDecode(input)");
        AssertContains(captureSettingsBuilderText, "settings.UseCustomAudioInput = input.IsCustomAudioInputEnabled;");
        AssertContains(captureSettingsBuilderText, "settings.MicrophoneEnabled = input.IsMicrophoneEnabled;");
        AssertContains(captureSettingsBuilderText, "private static CaptureSettingsFrameRateProjection ProjectFrameRate(CaptureSettingsProjectionInput input)");
        AssertContains(captureSettingsBuilderText, "private static string? ResolveRequestedPixelFormat(CaptureSettingsProjectionInput input)");
        AssertContains(captureSettingsBuilderText, "private static bool ShouldForceMjpegDecode(CaptureSettingsProjectionInput input)");
        AssertContains(captureSettingsBuilderText, "internal sealed class CaptureSettingsProjectionInput");
        AssertContains(captureSettingsBuilderText, "var selectedFrameRateOption = input.AvailableFrameRates");
        AssertContains(captureSettingsBuilderText, "var effectiveFrameRate = input.IsAutoResolutionSelected && input.AutoResolvedFrameRate.HasValue && input.AutoResolvedFrameRate.Value > 0");
        AssertContains(captureSettingsBuilderText, "runtimeMatchesResolution");
        AssertContains(captureSettingsBuilderText, "input.Runtime.NegotiatedFrameRateNumerator");
        AssertContains(captureSettingsBuilderText, "input.SourceTelemetry.HasFrameRate");
        AssertContains(captureSettingsBuilderText, "TryParseFrameRateRational(requestedFrameRateArg");
        AssertContains(captureSettingsBuilderText, "input.SelectedFormat?.FrameRateNumerator > 0 && input.SelectedFormat.FrameRateDenominator > 0");
        AssertContains(captureSettingsBuilderText, "requestedFrameRateArg = effectiveFrameRate.ToString(\"0.###\");");
        AssertContains(captureSettingsBuilderText, "internal readonly record struct CaptureSettingsFrameRateProjection(");
        AssertDoesNotContain(captureStateText, "ProjectCaptureSettingsFrameRate");
        AssertDoesNotContain(captureStateText, "private string? ResolveRequestedPixelFormat()");
        AssertDoesNotContain(captureStateText, "private bool ShouldForceMjpegDecode()");
        AssertContains(captureText, "private CaptureSettings BuildCaptureSettings()");
        AssertContains(previewLifecycleControllerText, "await _context.SessionCoordinator.StartVideoPreviewAsync(settings, cancellationToken)");
        AssertContains(recordingTransitionControllerText, "await _context.StartRecordingAsync(settings, cancellationToken);");
        AssertDoesNotContain(recordingLifecycleText, "await _sessionCoordinator.StartRecordingAsync(settings, cancellationToken);");
        AssertDoesNotContain(captureText, "await _sessionCoordinator.StartRecordingAsync(settings, cancellationToken);");

        return Task.CompletedTask;
    }

    internal static Task MainViewModelCaptureSettingsFrameRate_PreservesProjectionPrecedence()
    {
        var settings = InvokeCaptureSettingsProjection(
            selectedResolution: "1920x1080",
            selectedFrameRate: 60,
            autoResolvedFrameRate: null,
            selectedFormat: CreateMediaFormat(width: 1920, height: 1080, frameRate: 60, numerator: 60, denominator: 1),
            runtime: CreateRuntimeSnapshot(
                actualWidth: 1920,
                actualHeight: 1080,
                actualFrameRate: 60000d / 1001d,
                actualFrameRateArg: "60000/1001",
                negotiatedNumerator: 60000,
                negotiatedDenominator: 1001),
            sourceTelemetry: CreateSourceTelemetry(frameRateExact: 60, frameRateArg: "60/1"),
            frameRateOptions: new[] { CreateFrameRateOption(
                RequireType("Sussudio.Models.FrameRateOption"),
                60,
                60000d / 1001d,
                "60000/1001",
                isEnabled: true) });

        AssertNearlyEqual(60, GetDoubleProperty(settings, "FrameRate"), 0.001, "source-over-runtime effective frame rate");
        AssertEqual("60/1", GetStringProperty(settings, "RequestedFrameRateArg"), "source telemetry frame-rate arg wins after runtime");
        AssertEqual(60, Convert.ToInt32(GetPropertyValue(settings, "RequestedFrameRateNumerator")), "source telemetry numerator wins after runtime");
        AssertEqual(1, Convert.ToInt32(GetPropertyValue(settings, "RequestedFrameRateDenominator")), "source telemetry denominator wins after runtime");

        settings = InvokeCaptureSettingsProjection(
            selectedResolution: "1920x1080",
            selectedFrameRate: 60,
            autoResolvedFrameRate: null,
            selectedFormat: CreateMediaFormat(width: 1920, height: 1080, frameRate: 59.94, numerator: 60000, denominator: 1001),
            runtime: CreateRuntimeSnapshot(),
            sourceTelemetry: CreateSourceTelemetry(),
            frameRateOptions: new[] { CreateFrameRateOption(
                RequireType("Sussudio.Models.FrameRateOption"),
                60,
                60,
                string.Empty,
                isEnabled: true) });

        AssertNearlyEqual(60, GetDoubleProperty(settings, "FrameRate"), 0.001, "selected frame-rate effective value");
        AssertEqual("60000/1001", GetStringProperty(settings, "RequestedFrameRateArg"), "selected format rational fallback");
        AssertEqual(60000, Convert.ToInt32(GetPropertyValue(settings, "RequestedFrameRateNumerator")), "selected format fallback numerator");
        AssertEqual(1001, Convert.ToInt32(GetPropertyValue(settings, "RequestedFrameRateDenominator")), "selected format fallback denominator");

        settings = InvokeCaptureSettingsProjection(
            selectedResolution: "Source",
            selectedFrameRate: 0,
            autoResolvedFrameRate: 119.88,
            selectedFormat: null,
            runtime: CreateRuntimeSnapshot(),
            sourceTelemetry: CreateSourceTelemetry());

        AssertNearlyEqual(119.88, GetDoubleProperty(settings, "FrameRate"), 0.001, "auto-resolved effective frame rate");
        AssertEqual("119.88", GetStringProperty(settings, "RequestedFrameRateArg"), "decimal frame-rate fallback");
        AssertEqual(null, GetPropertyValue(settings, "RequestedFrameRateNumerator"), "decimal fallback numerator remains unset");
        AssertEqual(null, GetPropertyValue(settings, "RequestedFrameRateDenominator"), "decimal fallback denominator remains unset");

        settings = InvokeCaptureSettingsProjection(
            selectedResolution: "3840x2160",
            selectedFrameRate: 120,
            autoResolvedFrameRate: null,
            selectedFormat: CreateMediaFormat(width: 3840, height: 2160, frameRate: 120, numerator: 120, denominator: 1, pixelFormat: "NV12"),
            runtime: CreateRuntimeSnapshot(),
            sourceTelemetry: CreateSourceTelemetry(),
            selectedVideoFormat: "Auto",
            isHdrEnabled: false,
            mjpegDecoderCount: 99);

        AssertEqual("MJPG", GetStringProperty(settings, "RequestedPixelFormat"), "auto SDR 4K HFR requests MJPG");
        AssertEqual(true, GetBoolProperty(settings, "ForceMjpegDecode"), "auto SDR 4K HFR forces MJPEG decode");
        AssertEqual(8, Convert.ToInt32(GetPropertyValue(settings, "MjpegDecoderCount")), "decoder count clamps high");

        settings = InvokeCaptureSettingsProjection(
            selectedResolution: "3840x2160",
            selectedFrameRate: 120,
            autoResolvedFrameRate: null,
            selectedFormat: CreateMediaFormat(width: 3840, height: 2160, frameRate: 120, numerator: 120, denominator: 1, pixelFormat: "P010"),
            runtime: CreateRuntimeSnapshot(),
            sourceTelemetry: CreateSourceTelemetry(),
            selectedVideoFormat: "Auto",
            isHdrEnabled: true,
            isTrueHdrPreviewEnabled: true,
            mjpegDecoderCount: 0);

        AssertEqual("P010", GetStringProperty(settings, "RequestedPixelFormat"), "HDR auto keeps selected format pixel format");
        AssertEqual(false, GetBoolProperty(settings, "ForceMjpegDecode"), "HDR auto does not force MJPEG decode");
        AssertEqual("Hdr10Pq", GetPropertyValue(settings, "HdrOutputMode")?.ToString(), "HDR output mode");
        AssertEqual("TrueHdr", GetPropertyValue(settings, "PreviewMode")?.ToString(), "true HDR preview mode");
        AssertEqual(1, Convert.ToInt32(GetPropertyValue(settings, "MjpegDecoderCount")), "decoder count clamps low");

        settings = InvokeCaptureSettingsProjection(
            selectedResolution: "1920x1080",
            selectedFrameRate: 60,
            autoResolvedFrameRate: null,
            selectedFormat: CreateMediaFormat(width: 1920, height: 1080, frameRate: 60, numerator: 60, denominator: 1, pixelFormat: "NV12"),
            runtime: CreateRuntimeSnapshot(),
            sourceTelemetry: CreateSourceTelemetry(),
            selectedVideoFormat: "MJPG",
            isHdrEnabled: false,
            isCustomAudioInputEnabled: true,
            selectedAudioInputDeviceId: "audio-1",
            selectedAudioInputDeviceName: "Capture Audio",
            isMicrophoneEnabled: true,
            selectedMicrophoneDeviceId: "mic-1",
            selectedMicrophoneDeviceName: "Mic");

        AssertEqual("MJPG", GetStringProperty(settings, "RequestedPixelFormat"), "explicit MJPG requests MJPG");
        AssertEqual(true, GetBoolProperty(settings, "ForceMjpegDecode"), "explicit MJPG forces MJPEG decode");
        AssertEqual(true, GetBoolProperty(settings, "UseCustomAudioInput"), "custom audio flag copied");
        AssertEqual("audio-1", GetStringProperty(settings, "AudioDeviceId"), "custom audio id copied");
        AssertEqual("Capture Audio", GetStringProperty(settings, "AudioDeviceName"), "custom audio name copied");
        AssertEqual(true, GetBoolProperty(settings, "MicrophoneEnabled"), "microphone flag copied");
        AssertEqual("mic-1", GetStringProperty(settings, "MicrophoneDeviceId"), "microphone id copied");
        AssertEqual("Mic", GetStringProperty(settings, "MicrophoneDeviceName"), "microphone name copied");

        return Task.CompletedTask;
    }

    private static object InvokeCaptureSettingsProjection(
        string selectedResolution,
        double selectedFrameRate,
        double? autoResolvedFrameRate,
        object? selectedFormat,
        object runtime,
        object sourceTelemetry,
        string? selectedVideoFormat = "Auto",
        bool isHdrEnabled = false,
        bool isTrueHdrPreviewEnabled = false,
        int mjpegDecoderCount = 6,
        bool isCustomAudioInputEnabled = false,
        string? selectedAudioInputDeviceId = null,
        string? selectedAudioInputDeviceName = null,
        bool isMicrophoneEnabled = false,
        string? selectedMicrophoneDeviceId = null,
        string? selectedMicrophoneDeviceName = null,
        params object[] frameRateOptions)
    {
        var inputType = RequireType("Sussudio.ViewModels.CaptureSettingsProjectionInput");
        var input = CreateConfigInstance(inputType);
        var frameRateType = RequireType("Sussudio.Models.FrameRateOption");
        var availableFrameRates = Array.CreateInstance(frameRateType, frameRateOptions.Length);
        for (var i = 0; i < frameRateOptions.Length; i++)
        {
            availableFrameRates.SetValue(frameRateOptions[i], i);
        }

        SetPropertyOrBackingField(input, "EffectiveResolutionKnown", true);
        SetPropertyOrBackingField(input, "EffectiveWidth", 1920u);
        SetPropertyOrBackingField(input, "EffectiveHeight", 1080u);
        SetPropertyOrBackingField(input, "SelectedResolution", selectedResolution);
        SetPropertyOrBackingField(input, "SelectedFrameRate", selectedFrameRate);
        SetPropertyOrBackingField(input, "AutoResolvedFrameRate", autoResolvedFrameRate);
        SetPropertyOrBackingField(input, "IsAutoResolutionSelected", string.Equals(selectedResolution, "Source", StringComparison.OrdinalIgnoreCase));
        SetPropertyOrBackingField(input, "SelectedFormat", selectedFormat);
        SetPropertyOrBackingField(input, "AvailableFrameRates", availableFrameRates);
        SetPropertyOrBackingField(input, "Runtime", runtime);
        SetPropertyOrBackingField(input, "SourceTelemetry", sourceTelemetry);
        SetPropertyOrBackingField(input, "SelectedVideoFormat", selectedVideoFormat);
        SetPropertyOrBackingField(input, "IsHdrEnabled", isHdrEnabled);
        SetPropertyOrBackingField(input, "IsTrueHdrPreviewEnabled", isTrueHdrPreviewEnabled);
        SetPropertyOrBackingField(input, "MjpegDecoderCount", mjpegDecoderCount);
        SetPropertyOrBackingField(input, "SelectedRecordingFormat", "HEVC");
        SetPropertyOrBackingField(input, "SelectedQuality", "High");
        SetPropertyOrBackingField(input, "SelectedPreset", "P5");
        SetPropertyOrBackingField(input, "SelectedSplitEncodeMode", "Auto");
        SetPropertyOrBackingField(input, "CustomBitrateMbps", 42d);
        SetPropertyOrBackingField(input, "OutputPath", "C:\\Capture");
        SetPropertyOrBackingField(input, "FlashbackGpuDecode", true);
        SetPropertyOrBackingField(input, "FlashbackBufferMinutes", 5);
        SetPropertyOrBackingField(input, "IsAudioEnabled", true);
        SetPropertyOrBackingField(input, "IsCustomAudioInputEnabled", isCustomAudioInputEnabled);
        SetPropertyOrBackingField(input, "SelectedAudioInputDeviceId", selectedAudioInputDeviceId);
        SetPropertyOrBackingField(input, "SelectedAudioInputDeviceName", selectedAudioInputDeviceName);
        SetPropertyOrBackingField(input, "IsMicrophoneEnabled", isMicrophoneEnabled);
        SetPropertyOrBackingField(input, "SelectedMicrophoneDeviceId", selectedMicrophoneDeviceId);
        SetPropertyOrBackingField(input, "SelectedMicrophoneDeviceName", selectedMicrophoneDeviceName);

        var builderType = RequireType("Sussudio.ViewModels.CaptureSettingsProjectionBuilder");
        var build = builderType.GetMethod("Build", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("CaptureSettingsProjectionBuilder.Build was not found.");
        return build.Invoke(null, new[] { input })
               ?? throw new InvalidOperationException("CaptureSettingsProjectionBuilder.Build returned null.");
    }

    private static object CreateRuntimeSnapshot(
        uint? actualWidth = null,
        uint? actualHeight = null,
        double? actualFrameRate = null,
        string? actualFrameRateArg = null,
        uint? negotiatedNumerator = null,
        uint? negotiatedDenominator = null)
    {
        var snapshot = CreateConfigInstance(RequireType("Sussudio.Models.CaptureRuntimeSnapshot"));
        SetPropertyOrBackingField(snapshot, "ActualWidth", actualWidth);
        SetPropertyOrBackingField(snapshot, "ActualHeight", actualHeight);
        SetPropertyOrBackingField(snapshot, "ActualFrameRate", actualFrameRate);
        SetPropertyOrBackingField(snapshot, "ActualFrameRateArg", actualFrameRateArg);
        SetPropertyOrBackingField(snapshot, "NegotiatedFrameRateNumerator", negotiatedNumerator);
        SetPropertyOrBackingField(snapshot, "NegotiatedFrameRateDenominator", negotiatedDenominator);
        return snapshot;
    }

    private static object CreateSourceTelemetry(double? frameRateExact = null, string? frameRateArg = null)
    {
        var snapshot = CreateConfigInstance(RequireType("Sussudio.Models.SourceSignalTelemetrySnapshot"));
        SetPropertyOrBackingField(snapshot, "FrameRateExact", frameRateExact);
        SetPropertyOrBackingField(snapshot, "FrameRateArg", frameRateArg);
        return snapshot;
    }

    private static object CreateMediaFormat(
        uint width,
        uint height,
        double frameRate,
        uint numerator,
        uint denominator,
        string pixelFormat = "NV12")
    {
        var format = CreateConfigInstance(RequireType("Sussudio.Models.MediaFormat"));
        SetPropertyOrBackingField(format, "Width", width);
        SetPropertyOrBackingField(format, "Height", height);
        SetPropertyOrBackingField(format, "FrameRate", frameRate);
        SetPropertyOrBackingField(format, "FrameRateNumerator", numerator);
        SetPropertyOrBackingField(format, "FrameRateDenominator", denominator);
        SetPropertyOrBackingField(format, "PixelFormat", pixelFormat);
        return format;
    }
}

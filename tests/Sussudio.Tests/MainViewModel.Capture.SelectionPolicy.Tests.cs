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
        var automationSettingsText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.AutomationCommands.cs").Replace("\r\n", "\n");
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
        var policyText = ReadRepoFile("Sussudio/ViewModels/CaptureFormatSelectionPolicy.cs").Replace("\r\n", "\n");

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
}

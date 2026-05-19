using System.Collections.Generic;
using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationCommandDispatcher
{
    // Trivial one-property capture and pipeline commands live here so the root
    // dispatcher stays focused on request lifecycle, readiness, and error shaping.
    private static readonly IReadOnlyDictionary<AutomationCommandKind, AutomationCommandHandler<IAutomationDeviceSelectionPort>> TrivialDeviceSelectionHandlers =
        new Dictionary<AutomationCommandKind, AutomationCommandHandler<IAutomationDeviceSelectionPort>>
        {
            [AutomationCommandKind.SetCustomAudioInput] = AutomationCommandHandler<IAutomationDeviceSelectionPort>.Bool(
                (vm, v, ct) => vm.SetCustomAudioInputEnabledAsync(v, ct), "enabled"),
        };

    private static readonly IReadOnlyDictionary<AutomationCommandKind, AutomationCommandHandler<IAutomationCaptureSettingsPort>> TrivialCaptureSettingsHandlers =
        new Dictionary<AutomationCommandKind, AutomationCommandHandler<IAutomationCaptureSettingsPort>>
        {
            [AutomationCommandKind.SetResolution] = AutomationCommandHandler<IAutomationCaptureSettingsPort>.String(
                (vm, v, ct) => vm.SetResolutionAsync(v, ct), "resolution"),
            [AutomationCommandKind.SetFrameRate] = AutomationCommandHandler<IAutomationCaptureSettingsPort>.Double(
                (vm, v, ct) => vm.SetFrameRateAsync(v, ct), "frameRate"),
            [AutomationCommandKind.SetVideoFormat] = AutomationCommandHandler<IAutomationCaptureSettingsPort>.String(
                (vm, v, ct) => vm.SetVideoFormatAsync(v, ct), "videoFormat"),
            [AutomationCommandKind.SetPreset] = AutomationCommandHandler<IAutomationCaptureSettingsPort>.String(
                (vm, v, ct) => vm.SetPresetAsync(v, ct), "preset"),
            [AutomationCommandKind.SetSplitEncodeMode] = AutomationCommandHandler<IAutomationCaptureSettingsPort>.String(
                (vm, v, ct) => vm.SetSplitEncodeModeAsync(v, ct), "splitEncodeMode"),
            [AutomationCommandKind.SetRecordingFormat] = AutomationCommandHandler<IAutomationCaptureSettingsPort>.String(
                (vm, v, ct) => vm.SetRecordingFormatAsync(v, ct), "format"),
            [AutomationCommandKind.SetQuality] = AutomationCommandHandler<IAutomationCaptureSettingsPort>.String(
                (vm, v, ct) => vm.SetQualityAsync(v, ct), "quality"),
            [AutomationCommandKind.SetCustomBitrate] = AutomationCommandHandler<IAutomationCaptureSettingsPort>.Double(
                (vm, v, ct) => vm.SetCustomBitrateAsync(v, ct), "bitrateMbps"),
            [AutomationCommandKind.SetHdrEnabled] = AutomationCommandHandler<IAutomationCaptureSettingsPort>.Bool(
                (vm, v, ct) => vm.SetHdrEnabledAsync(v, ct), "enabled"),
            [AutomationCommandKind.SetTrueHdrPreviewEnabled] = AutomationCommandHandler<IAutomationCaptureSettingsPort>.Bool(
                (vm, v, ct) => vm.SetTrueHdrPreviewEnabledAsync(v, ct), "enabled"),
        };

    private static readonly IReadOnlyDictionary<AutomationCommandKind, AutomationCommandHandler<IAutomationAudioPort>> TrivialAudioHandlers =
        new Dictionary<AutomationCommandKind, AutomationCommandHandler<IAutomationAudioPort>>
        {
            [AutomationCommandKind.SetAudioEnabled] = AutomationCommandHandler<IAutomationAudioPort>.Bool(
                (vm, v, ct) => vm.SetAudioEnabledAsync(v, ct), "enabled"),
            [AutomationCommandKind.SetAudioPreviewEnabled] = AutomationCommandHandler<IAutomationAudioPort>.Bool(
                (vm, v, ct) => vm.SetAudioPreviewEnabledAsync(v, ct), "enabled"),
        };

    private static readonly IReadOnlyDictionary<AutomationCommandKind, AutomationCommandHandler<IAutomationPreviewRecordingPort>> TrivialPreviewRecordingHandlers =
        new Dictionary<AutomationCommandKind, AutomationCommandHandler<IAutomationPreviewRecordingPort>>
        {
            [AutomationCommandKind.SetPreviewEnabled] = AutomationCommandHandler<IAutomationPreviewRecordingPort>.Bool(
                (vm, v, ct) => vm.SetPreviewEnabledAsync(v, ct), "enabled"),
        };
}

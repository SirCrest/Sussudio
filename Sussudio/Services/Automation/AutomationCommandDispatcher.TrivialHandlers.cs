using System.Collections.Generic;
using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationCommandDispatcher
{
    // Trivial one-property commands live here so the root dispatcher stays focused
    // on request lifecycle, readiness, and error shaping.
    private static readonly IReadOnlyDictionary<AutomationCommandKind, AutomationCommandHandler> TrivialHandlers =
        new Dictionary<AutomationCommandKind, AutomationCommandHandler>
        {
            [AutomationCommandKind.SetCustomAudioInput] = AutomationCommandHandler.Bool(
                (vm, v, ct) => vm.SetCustomAudioInputEnabledAsync(v, ct), "enabled"),
            [AutomationCommandKind.SetResolution] = AutomationCommandHandler.String(
                (vm, v, ct) => vm.SetResolutionAsync(v, ct), "resolution"),
            [AutomationCommandKind.SetFrameRate] = AutomationCommandHandler.Double(
                (vm, v, ct) => vm.SetFrameRateAsync(v, ct), "frameRate"),
            [AutomationCommandKind.SetVideoFormat] = AutomationCommandHandler.String(
                (vm, v, ct) => vm.SetVideoFormatAsync(v, ct), "videoFormat"),
            [AutomationCommandKind.SetPreset] = AutomationCommandHandler.String(
                (vm, v, ct) => vm.SetPresetAsync(v, ct), "preset"),
            [AutomationCommandKind.SetSplitEncodeMode] = AutomationCommandHandler.String(
                (vm, v, ct) => vm.SetSplitEncodeModeAsync(v, ct), "splitEncodeMode"),
            [AutomationCommandKind.SetShowAllCaptureOptions] = AutomationCommandHandler.Bool(
                (vm, v, ct) => vm.SetShowAllCaptureOptionsAsync(v, ct), "enabled"),
            [AutomationCommandKind.SetPreviewVolume] = AutomationCommandHandler.Double(
                (vm, v, ct) => vm.SetPreviewVolumeAsync(v, ct), "previewVolumePercent"),
            [AutomationCommandKind.SetStatsVisible] = AutomationCommandHandler.Bool(
                (vm, v, ct) => vm.SetStatsVisibleAsync(v, ct), "visible"),
            [AutomationCommandKind.SetSettingsVisible] = AutomationCommandHandler.Bool(
                (vm, v, ct) => vm.SetSettingsVisibleAsync(v, ct), "visible"),
            [AutomationCommandKind.SetFrameTimeOverlayVisible] = AutomationCommandHandler.Bool(
                (vm, v, ct) => vm.SetFrameTimeOverlayVisibleAsync(v, ct), "visible"),
            [AutomationCommandKind.SetFlashbackTimelineVisible] = AutomationCommandHandler.Bool(
                (vm, v, ct) => vm.SetFlashbackTimelineVisibleAsync(v, ct), "visible"),
            [AutomationCommandKind.SetRecordingFormat] = AutomationCommandHandler.String(
                (vm, v, ct) => vm.SetRecordingFormatAsync(v, ct), "format"),
            [AutomationCommandKind.SetQuality] = AutomationCommandHandler.String(
                (vm, v, ct) => vm.SetQualityAsync(v, ct), "quality"),
            [AutomationCommandKind.SetCustomBitrate] = AutomationCommandHandler.Double(
                (vm, v, ct) => vm.SetCustomBitrateAsync(v, ct), "bitrateMbps"),
            [AutomationCommandKind.SetHdrEnabled] = AutomationCommandHandler.Bool(
                (vm, v, ct) => vm.SetHdrEnabledAsync(v, ct), "enabled"),
            [AutomationCommandKind.SetTrueHdrPreviewEnabled] = AutomationCommandHandler.Bool(
                (vm, v, ct) => vm.SetTrueHdrPreviewEnabledAsync(v, ct), "enabled"),
            [AutomationCommandKind.SetAudioEnabled] = AutomationCommandHandler.Bool(
                (vm, v, ct) => vm.SetAudioEnabledAsync(v, ct), "enabled"),
            [AutomationCommandKind.SetAudioPreviewEnabled] = AutomationCommandHandler.Bool(
                (vm, v, ct) => vm.SetAudioPreviewEnabledAsync(v, ct), "enabled"),
            [AutomationCommandKind.SetPreviewEnabled] = AutomationCommandHandler.Bool(
                (vm, v, ct) => vm.SetPreviewEnabledAsync(v, ct), "enabled"),
        };
}

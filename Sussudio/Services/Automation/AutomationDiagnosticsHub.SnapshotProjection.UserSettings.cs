using System;
using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static UserSettingsProjection BuildUserSettingsProjection(ViewModelRuntimeSnapshot viewModelSnapshot)
        => new()
        {
            SelectedDeviceId = viewModelSnapshot.SelectedDeviceId,
            SelectedDeviceName = viewModelSnapshot.SelectedDeviceName,
            SelectedAudioInputDeviceId = viewModelSnapshot.SelectedAudioInputDeviceId,
            SelectedAudioInputDeviceName = viewModelSnapshot.SelectedAudioInputDeviceName,
            SelectedResolution = viewModelSnapshot.SelectedResolution,
            SelectedFrameRate = viewModelSnapshot.SelectedFrameRate,
            SelectedFriendlyFrameRate = viewModelSnapshot.SelectedFriendlyFrameRate ?? Math.Round(viewModelSnapshot.SelectedFrameRate),
            SelectedExactFrameRate = viewModelSnapshot.SelectedExactFrameRate ?? viewModelSnapshot.SelectedFrameRate,
            SelectedExactFrameRateArg = viewModelSnapshot.SelectedExactFrameRateArg,
            DisabledResolutionReason = viewModelSnapshot.DisabledResolutionReason,
            DisabledFrameRateReason = viewModelSnapshot.DisabledFrameRateReason,
            SelectedRecordingFormat = viewModelSnapshot.SelectedRecordingFormat,
            SelectedQuality = viewModelSnapshot.SelectedQuality,
            SelectedPreset = viewModelSnapshot.SelectedPreset,
            SelectedSplitEncodeMode = viewModelSnapshot.SelectedSplitEncodeMode,
            SelectedVideoFormat = viewModelSnapshot.SelectedVideoFormat,
            CustomBitrateMbps = viewModelSnapshot.CustomBitrateMbps,
            ShowAllCaptureOptions = viewModelSnapshot.ShowAllCaptureOptions,
            PreviewVolumePercent = viewModelSnapshot.PreviewVolumePercent,
            IsStatsVisible = viewModelSnapshot.IsStatsVisible
        };

    private readonly record struct UserSettingsProjection
    {
        public string? SelectedDeviceId { get; init; }
        public string? SelectedDeviceName { get; init; }
        public string? SelectedAudioInputDeviceId { get; init; }
        public string? SelectedAudioInputDeviceName { get; init; }
        public string? SelectedResolution { get; init; }
        public double SelectedFrameRate { get; init; }
        public double? SelectedFriendlyFrameRate { get; init; }
        public double? SelectedExactFrameRate { get; init; }
        public string? SelectedExactFrameRateArg { get; init; }
        public string? DisabledResolutionReason { get; init; }
        public string? DisabledFrameRateReason { get; init; }
        public string SelectedRecordingFormat { get; init; }
        public string SelectedQuality { get; init; }
        public string SelectedPreset { get; init; }
        public string SelectedSplitEncodeMode { get; init; }
        public string SelectedVideoFormat { get; init; }
        public double CustomBitrateMbps { get; init; }
        public bool ShowAllCaptureOptions { get; init; }
        public double PreviewVolumePercent { get; init; }
        public bool IsStatsVisible { get; init; }
    }

    private static RecordingSettingsProjection BuildRecordingSettingsProjection(UserSettingsProjection userSettings)
        => new()
        {
            SelectedRecordingFormat = userSettings.SelectedRecordingFormat,
            SelectedQuality = userSettings.SelectedQuality,
            SelectedPreset = userSettings.SelectedPreset,
            SelectedSplitEncodeMode = userSettings.SelectedSplitEncodeMode,
            SelectedVideoFormat = userSettings.SelectedVideoFormat,
            CustomBitrateMbps = userSettings.CustomBitrateMbps
        };

    private readonly record struct RecordingSettingsProjection
    {
        public string SelectedRecordingFormat { get; init; }
        public string SelectedQuality { get; init; }
        public string SelectedPreset { get; init; }
        public string SelectedSplitEncodeMode { get; init; }
        public string SelectedVideoFormat { get; init; }
        public double CustomBitrateMbps { get; init; }
    }
}

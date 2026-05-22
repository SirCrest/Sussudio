namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static SettingsFlattenedProjection BuildSettingsFlattenedProjection(
        UserSettingsProjection userSettings,
        RecordingSettingsProjection recordingSettings)
        => new()
        {
            SelectedDeviceId = userSettings.SelectedDeviceId,
            SelectedDeviceName = userSettings.SelectedDeviceName,
            SelectedAudioInputDeviceId = userSettings.SelectedAudioInputDeviceId,
            SelectedAudioInputDeviceName = userSettings.SelectedAudioInputDeviceName,
            SelectedResolution = userSettings.SelectedResolution,
            SelectedFrameRate = userSettings.SelectedFrameRate,
            SelectedFriendlyFrameRate = userSettings.SelectedFriendlyFrameRate,
            SelectedExactFrameRate = userSettings.SelectedExactFrameRate,
            SelectedExactFrameRateArg = userSettings.SelectedExactFrameRateArg,
            DisabledResolutionReason = userSettings.DisabledResolutionReason,
            DisabledFrameRateReason = userSettings.DisabledFrameRateReason,
            SelectedRecordingFormat = recordingSettings.SelectedRecordingFormat,
            SelectedQuality = recordingSettings.SelectedQuality,
            SelectedPreset = recordingSettings.SelectedPreset,
            SelectedSplitEncodeMode = recordingSettings.SelectedSplitEncodeMode,
            SelectedVideoFormat = recordingSettings.SelectedVideoFormat,
            CustomBitrateMbps = recordingSettings.CustomBitrateMbps,
            PreviewVolumePercent = userSettings.PreviewVolumePercent,
            IsStatsVisible = userSettings.IsStatsVisible
        };

    private readonly record struct SettingsFlattenedProjection
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
        public double PreviewVolumePercent { get; init; }
        public bool IsStatsVisible { get; init; }
    }
}

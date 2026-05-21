using System;
using System.Collections.Generic;
using System.Linq;
using Sussudio.Services.Runtime;

namespace Sussudio.ViewModels;

/// <summary>
/// Pure settings projection between persisted settings and MainViewModel load state.
/// </summary>
internal static partial class MainViewModelSettingsPersistenceProjection
{
    internal static MainViewModelSettingsLoadPlan BuildLoadPlan(
        UserSettings settings,
        MainViewModelSettingsLoadInput input)
    {
        var outputPath = !string.IsNullOrWhiteSpace(settings.OutputPath) &&
            input.OutputDirectoryExists(settings.OutputPath)
                ? settings.OutputPath
                : null;

        var selectedRecordingFormat = ResolveAvailableValue(
            settings.SelectedRecordingFormat,
            input.AvailableRecordingFormats,
            StringComparer.Ordinal);
        var unavailableRecordingFormat = selectedRecordingFormat is null &&
            !string.IsNullOrWhiteSpace(settings.SelectedRecordingFormat)
                ? settings.SelectedRecordingFormat
                : null;

        var microphoneVolume = settings.MicrophoneVolume.HasValue
            ? Math.Clamp(settings.MicrophoneVolume.Value, 0.0, 100.0)
            : (double?)null;

        return new MainViewModelSettingsLoadPlan(
            OutputPath: outputPath,
            SelectedRecordingFormat: selectedRecordingFormat,
            UnavailableRecordingFormat: unavailableRecordingFormat,
            SelectedQuality: ResolveAvailableValue(settings.SelectedQuality, input.AvailableQualities, StringComparer.Ordinal),
            SelectedPreset: ResolveAvailableValue(settings.SelectedPreset, input.AvailablePresets, StringComparer.Ordinal),
            SelectedSplitEncodeMode: ResolveAvailableValue(settings.SelectedSplitEncodeMode, input.AvailableSplitEncodeModes, StringComparer.Ordinal),
            CustomBitrateMbps: settings.CustomBitrateMbps,
            IsHdrEnabled: settings.IsHdrEnabled,
            IsAudioEnabled: settings.IsAudioEnabled,
            IsAudioPreviewEnabled: settings.IsAudioPreviewEnabled,
            IsCustomAudioInputEnabled: settings.IsCustomAudioInputEnabled,
            IsMicrophoneEnabled: settings.IsMicrophoneEnabled,
            MicrophoneVolume: microphoneVolume,
            PendingMicrophoneVolumeDeviceId: microphoneVolume.HasValue ? settings.SelectedMicrophoneDeviceId : null,
            PreviewVolume: settings.PreviewVolume.HasValue ? Math.Clamp(settings.PreviewVolume.Value, 0.0, 1.0) : null,
            ShowAllCaptureOptions: settings.ShowAllCaptureOptions,
            IsStatsVisible: settings.IsStatsVisible,
            SelectedDeviceAudioMode: ResolveAvailableValue(
                settings.SelectedDeviceAudioMode,
                input.AvailableDeviceAudioModes,
                StringComparer.OrdinalIgnoreCase),
            AnalogAudioGainPercent: settings.AnalogAudioGainPercent.HasValue
                ? Math.Clamp(settings.AnalogAudioGainPercent.Value, 0.0, 100.0)
                : null,
            FlashbackGpuDecode: settings.FlashbackGpuDecode,
            FlashbackBufferMinutes: settings.FlashbackBufferMinutes.HasValue
                ? Math.Clamp(settings.FlashbackBufferMinutes.Value, 1, 30)
                : null,
            PendingDeviceId: settings.SelectedDeviceId,
            PendingAudioDeviceId: settings.SelectedAudioInputDeviceId,
            PendingMicrophoneDeviceId: settings.SelectedMicrophoneDeviceId,
            PendingDeviceAudioMode: settings.SelectedDeviceAudioMode,
            PendingAnalogAudioGainPercent: settings.AnalogAudioGainPercent);
    }

    private static string? ResolveAvailableValue(
        string? savedValue,
        IEnumerable<string> availableValues,
        StringComparer comparer)
    {
        return !string.IsNullOrWhiteSpace(savedValue) &&
            availableValues.Contains(savedValue, comparer)
                ? savedValue
                : null;
    }
}

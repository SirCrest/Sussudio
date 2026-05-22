using System;
using System.Collections.Generic;

namespace Sussudio.ViewModels;

internal readonly record struct MainViewModelSettingsLoadInput(
    IReadOnlyCollection<string> AvailableRecordingFormats,
    IReadOnlyCollection<string> AvailableQualities,
    IReadOnlyCollection<string> AvailablePresets,
    IReadOnlyCollection<string> AvailableSplitEncodeModes,
    IReadOnlyCollection<string> AvailableDeviceAudioModes,
    Func<string, bool> OutputDirectoryExists);

internal readonly record struct MainViewModelSettingsLoadPlan(
    string? OutputPath,
    string? SelectedRecordingFormat,
    string? UnavailableRecordingFormat,
    string? SelectedQuality,
    string? SelectedPreset,
    string? SelectedSplitEncodeMode,
    double? CustomBitrateMbps,
    bool? IsHdrEnabled,
    bool? IsAudioEnabled,
    bool? IsAudioPreviewEnabled,
    bool? IsCustomAudioInputEnabled,
    bool? IsMicrophoneEnabled,
    double? MicrophoneVolume,
    string? PendingMicrophoneVolumeDeviceId,
    double? PreviewVolume,
    bool? IsStatsVisible,
    string? SelectedDeviceAudioMode,
    double? AnalogAudioGainPercent,
    bool? FlashbackGpuDecode,
    int? FlashbackBufferMinutes,
    string? PendingDeviceId,
    string? PendingAudioDeviceId,
    string? PendingMicrophoneDeviceId,
    string? PendingDeviceAudioMode,
    double? PendingAnalogAudioGainPercent);

internal readonly record struct MainViewModelSettingsSaveInput(
    string? SelectedDeviceId,
    string OutputPath,
    string SelectedRecordingFormat,
    string SelectedQuality,
    string SelectedPreset,
    string SelectedSplitEncodeMode,
    double CustomBitrateMbps,
    bool IsHdrEnabled,
    bool IsAudioEnabled,
    bool IsAudioPreviewEnabled,
    bool IsCustomAudioInputEnabled,
    string? SelectedAudioInputDeviceId,
    bool IsMicrophoneEnabled,
    string? SelectedMicrophoneDeviceId,
    double MicrophoneVolume,
    double PreviewVolume,
    bool IsStatsVisible,
    string SelectedDeviceAudioMode,
    double AnalogAudioGainPercent,
    bool FlashbackGpuDecode,
    int FlashbackBufferMinutes);

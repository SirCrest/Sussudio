using System;
using System.Collections.Generic;
using System.Linq;
using Sussudio.Models;

namespace Sussudio.ViewModels;

internal static class AudioDeviceSelectionPolicy
{
    internal static AudioDeviceSelection SelectStartup(
        IReadOnlyList<AudioInputDevice> audioDevices,
        IReadOnlyList<CaptureDevice> videoDevices,
        string? previousDeviceId,
        string? previousAudioId,
        string? savedAudioId,
        string? previousMicrophoneId,
        string? savedMicrophoneId)
    {
        var captureCardAudioId = ResolveStartupCaptureCardAudioId(videoDevices, previousDeviceId);
        var availableDevices = FilterOutCaptureCardAudio(audioDevices, captureCardAudioId);
        var selectedAudio = SelectByPreviousSavedOrFirst(availableDevices, previousAudioId, savedAudioId);
        var selectedMicrophone = SelectByPreviousSavedOrFirst(availableDevices, previousMicrophoneId, savedMicrophoneId);

        return new AudioDeviceSelection(
            availableDevices,
            selectedAudio,
            selectedMicrophone,
            ShouldLogSavedFallback(savedAudioId, selectedAudio),
            ShouldLogSavedFallback(savedMicrophoneId, selectedMicrophone));
    }

    internal static AudioDeviceSelection SelectRefresh(
        IReadOnlyList<AudioInputDevice> audioDevices,
        string? captureCardAudioId,
        string? previousAudioId,
        string? previousMicrophoneId,
        string? savedMicrophoneId)
    {
        var availableDevices = FilterOutCaptureCardAudio(audioDevices, captureCardAudioId);

        return new AudioDeviceSelection(
            availableDevices,
            SelectByPreviousOrFirst(availableDevices, previousAudioId),
            SelectByPreviousSavedOrFirst(availableDevices, previousMicrophoneId, savedMicrophoneId),
            ShouldLogSavedAudioFallback: false,
            ShouldLogSavedMicrophoneFallback: false);
    }

    internal static IReadOnlyList<AudioInputDevice> FilterOutCaptureCardAudio(
        IReadOnlyList<AudioInputDevice> devices,
        string? excludeId)
    {
        if (string.IsNullOrWhiteSpace(excludeId))
        {
            return devices;
        }

        return devices.Where(d => !string.Equals(d.Id, excludeId, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    private static string? ResolveStartupCaptureCardAudioId(
        IReadOnlyList<CaptureDevice> videoDevices,
        string? previousDeviceId)
        => (videoDevices.FirstOrDefault(d => d.Id == previousDeviceId) ?? videoDevices.FirstOrDefault())?.AudioDeviceId;

    private static AudioInputDevice? SelectByPreviousSavedOrFirst(
        IReadOnlyList<AudioInputDevice> devices,
        string? previousId,
        string? savedId)
        => SelectById(devices, previousId)
           ?? SelectById(devices, savedId)
           ?? devices.FirstOrDefault();

    private static AudioInputDevice? SelectByPreviousOrFirst(
        IReadOnlyList<AudioInputDevice> devices,
        string? previousId)
        => SelectById(devices, previousId) ?? devices.FirstOrDefault();

    private static AudioInputDevice? SelectById(
        IReadOnlyList<AudioInputDevice> devices,
        string? id)
        => !string.IsNullOrWhiteSpace(id)
            ? devices.FirstOrDefault(d => d.Id == id)
            : null;

    private static bool ShouldLogSavedFallback(string? savedId, AudioInputDevice? selected)
        => !string.IsNullOrWhiteSpace(savedId) && selected?.Id != savedId;
}

internal sealed record AudioDeviceSelection(
    IReadOnlyList<AudioInputDevice> AvailableDevices,
    AudioInputDevice? SelectedAudioInputDevice,
    AudioInputDevice? SelectedMicrophoneDevice,
    bool ShouldLogSavedAudioFallback,
    bool ShouldLogSavedMicrophoneFallback);

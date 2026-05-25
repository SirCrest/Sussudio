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

internal static class DeviceFormatProbeRetargetPolicy
{
    internal static DeviceFormatProbeRetargetDecision Decide(DeviceFormatProbeRetargetRequest request)
    {
        if (request.AllowProbeDrivenRetarget &&
            request.IsHdrEnabled &&
            request.ModeChanged)
        {
            return DeviceFormatProbeRetargetDecision.HdrRetarget;
        }

        if (request.AllowProbeDrivenRetarget &&
            !request.IsHdrEnabled &&
            request.SelectedFormat?.PixelFormat.Equals("MJPG", StringComparison.OrdinalIgnoreCase) == true)
        {
            if (ShouldPreserveMjpegHighFrameRateMode(request.SelectedFormat))
            {
                return DeviceFormatProbeRetargetDecision.PreserveMjpegHighFrameRate;
            }

            var selectedNv12 = SelectSdrNv12RetargetFormat(
                request.SupportedFormats,
                request.PreviousFrameRate > 0 ? request.PreviousFrameRate : request.SelectedFrameRate);
            if (selectedNv12 != null)
            {
                var targetResolution = GetResolutionKey(selectedNv12.Width, selectedNv12.Height);
                if (!string.Equals(targetResolution, request.SelectedResolution, StringComparison.OrdinalIgnoreCase))
                {
                    return DeviceFormatProbeRetargetDecision.SdrNv12Retarget(
                        targetResolution,
                        selectedNv12.FrameRateExact);
                }
            }
        }

        if (request.AllowProbeDrivenRetarget &&
            request.IncludeSessionMismatchCheck &&
            request.SelectedFormat != null &&
            request.SessionActualWidth.HasValue &&
            request.SessionActualHeight.HasValue &&
            (request.SessionActualWidth.Value != request.SelectedFormat.Width ||
             request.SessionActualHeight.Value != request.SelectedFormat.Height))
        {
            return DeviceFormatProbeRetargetDecision.SessionMismatch;
        }

        if (request.AllowProbeDrivenRetarget &&
            request.IncludeSessionMismatchCheck &&
            request.SelectedFormat != null &&
            (!request.SessionActualWidth.HasValue || !request.SessionActualHeight.HasValue))
        {
            return DeviceFormatProbeRetargetDecision.SessionRuntimeUnavailable;
        }

        if (request.PreserveActiveSelection &&
            !request.AllowProbeDrivenRetarget &&
            request.ModeChanged &&
            !string.IsNullOrWhiteSpace(request.PreviousResolution) &&
            request.PreviousResolutionAvailable)
        {
            return DeviceFormatProbeRetargetDecision.RestoreActiveSelection;
        }

        return DeviceFormatProbeRetargetDecision.None;
    }

    private static MediaFormat? SelectSdrNv12RetargetFormat(
        IReadOnlyCollection<MediaFormat> supportedFormats,
        double preferredRate)
    {
        var preferredBucket = GetFriendlyFrameRateBucket(preferredRate);
        var nv12Candidates = supportedFormats
            .Where(format => format.PixelFormat.Equals("NV12", StringComparison.OrdinalIgnoreCase))
            .ToList();

        return nv12Candidates
            .Where(format => GetFriendlyFrameRateBucket(format.FrameRateExact) == preferredBucket)
            .OrderByDescending(format => (long)format.Width * format.Height)
            .FirstOrDefault()
            ?? nv12Candidates
                .OrderBy(format => Math.Abs(format.FrameRateExact - preferredRate))
                .ThenByDescending(format => (long)format.Width * format.Height)
                .FirstOrDefault();
    }

    private static bool ShouldPreserveMjpegHighFrameRateMode(MediaFormat format)
        => CaptureSettings.IsMjpegHighFrameRateMode(
            format.PixelFormat,
            format.Width,
            format.Height,
            format.FrameRateExact,
            hdrEnabled: false);

    private static string GetResolutionKey(uint width, uint height)
        => $"{width}x{height}";

    private static int GetFriendlyFrameRateBucket(double frameRate)
        => (int)Math.Round(frameRate, MidpointRounding.AwayFromZero);
}

internal sealed record DeviceFormatProbeRetargetRequest(
    bool PreserveActiveSelection,
    bool AllowProbeDrivenRetarget,
    bool IsHdrEnabled,
    bool ModeChanged,
    string? PreviousResolution,
    double PreviousFrameRate,
    string? SelectedResolution,
    double SelectedFrameRate,
    MediaFormat? SelectedFormat,
    IReadOnlyCollection<MediaFormat> SupportedFormats,
    bool PreviousResolutionAvailable,
    bool IncludeSessionMismatchCheck,
    uint? SessionActualWidth,
    uint? SessionActualHeight);

internal sealed record DeviceFormatProbeRetargetDecision(
    DeviceFormatProbeRetargetDecisionKind Kind,
    string? TargetResolution = null,
    double TargetFrameRate = 0,
    string? ReinitializeReason = null,
    string? UiOperationName = null)
{
    internal static readonly DeviceFormatProbeRetargetDecision None =
        new(DeviceFormatProbeRetargetDecisionKind.None);

    internal static readonly DeviceFormatProbeRetargetDecision HdrRetarget =
        new(
            DeviceFormatProbeRetargetDecisionKind.HdrRetarget,
            ReinitializeReason: "format probe (HDR retarget)",
            UiOperationName: "format probe hdr retarget");

    internal static readonly DeviceFormatProbeRetargetDecision PreserveMjpegHighFrameRate =
        new(DeviceFormatProbeRetargetDecisionKind.PreserveMjpegHighFrameRate);

    internal static readonly DeviceFormatProbeRetargetDecision SessionMismatch =
        new(
            DeviceFormatProbeRetargetDecisionKind.SessionMismatch,
            ReinitializeReason: "format probe (session mismatch)",
            UiOperationName: "format probe session mismatch");

    internal static readonly DeviceFormatProbeRetargetDecision SessionRuntimeUnavailable =
        new(DeviceFormatProbeRetargetDecisionKind.SessionRuntimeUnavailable);

    internal static readonly DeviceFormatProbeRetargetDecision RestoreActiveSelection =
        new(DeviceFormatProbeRetargetDecisionKind.RestoreActiveSelection);

    internal static DeviceFormatProbeRetargetDecision SdrNv12Retarget(
        string targetResolution,
        double targetFrameRate)
        => new(
            DeviceFormatProbeRetargetDecisionKind.SdrNv12Retarget,
            TargetResolution: targetResolution,
            TargetFrameRate: targetFrameRate,
            ReinitializeReason: "format probe (SDR nv12 retarget)",
            UiOperationName: "format probe sdr retarget");
}

internal enum DeviceFormatProbeRetargetDecisionKind
{
    None,
    HdrRetarget,
    SdrNv12Retarget,
    PreserveMjpegHighFrameRate,
    SessionMismatch,
    SessionRuntimeUnavailable,
    RestoreActiveSelection
}

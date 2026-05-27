using System;
using System.Collections.Generic;
using System.Linq;
using Sussudio.Models;
using Sussudio.Services.Capture;

namespace Sussudio.ViewModels;

internal static class CaptureModeOptionsBuilder
{
    internal static IReadOnlyList<ResolutionOption> BuildResolutionOptions(
        IEnumerable<KeyValuePair<string, List<MediaFormat>>> resolutionToFormats,
        bool hdrEnabled,
        bool showAllCaptureOptions,
        SourceSignalTelemetrySnapshot sourceTelemetry)
    {
        var options = resolutionToFormats
            .Where(entry => entry.Value.Count > 0)
            .Select(entry =>
            {
                var formats = entry.Value;
                var first = formats[0];
                var hdrSupported = formats.Any(IsHdrModeCandidate);
                var enabled = !hdrEnabled || hdrSupported;
                return new ResolutionOption
                {
                    Value = entry.Key,
                    Width = first.Width,
                    Height = first.Height,
                    IsEnabled = enabled,
                    DisableReason = enabled
                        ? string.Empty
                        : "HDR mode is not supported at this resolution."
                };
            })
            .OrderByDescending(option => (long)option.Width * option.Height)
            .ToList();

        if (!showAllCaptureOptions && sourceTelemetry.HasDimensions)
        {
            options = options
                .Where(option => DoesResolutionMatchSourceAspectRatio(option, sourceTelemetry))
                .ToList();
        }

        return options;
    }

    internal static IReadOnlyList<string> BuildVideoFormatOptions(IEnumerable<MediaFormat> formats)
    {
        var pixelFormats = formats
            .Select(NormalizeVideoFormatName)
            .Where(format => !string.IsNullOrWhiteSpace(format))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(MediaFormat.GetPixelFormatPriority)
            .ThenBy(format => format, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var options = new List<string> { "Auto" };
        options.AddRange(pixelFormats);
        return options;
    }

    internal static bool IsHdrModeCandidate(MediaFormat format)
        => format.IsHdr || MediaFormat.IsTrue10BitPixelFormat(format.PixelFormat);

    private static string NormalizeVideoFormatName(MediaFormat format)
        => string.IsNullOrWhiteSpace(format.PixelFormat)
            ? string.Empty
            : format.PixelFormat.Trim().ToUpperInvariant();

    private static bool DoesResolutionMatchSourceAspectRatio(
        ResolutionOption option,
        SourceSignalTelemetrySnapshot sourceTelemetry)
    {
        if (!sourceTelemetry.HasDimensions)
        {
            return true;
        }

        var sourceWidth = (uint)Math.Max(0, sourceTelemetry.Width ?? 0);
        var sourceHeight = (uint)Math.Max(0, sourceTelemetry.Height ?? 0);
        if (sourceWidth == 0 || sourceHeight == 0 || option.Width == 0 || option.Height == 0)
        {
            return true;
        }

        var reducedSource = ReduceAspectRatio(sourceWidth, sourceHeight);
        var reducedOption = ReduceAspectRatio(option.Width, option.Height);
        return reducedSource.Width == reducedOption.Width &&
               reducedSource.Height == reducedOption.Height;
    }

    private static (uint Width, uint Height) ReduceAspectRatio(uint width, uint height)
    {
        if (width == 0 || height == 0)
        {
            return (width, height);
        }

        var divisor = GreatestCommonDivisor(width, height);
        return divisor == 0
            ? (width, height)
            : (width / divisor, height / divisor);
    }

    private static uint GreatestCommonDivisor(uint a, uint b)
    {
        while (b != 0)
        {
            var next = a % b;
            a = b;
            b = next;
        }

        return a;
    }
}

internal sealed record CaptureFormatSelectionRequest(
    IReadOnlyList<MediaFormat> AvailableFormats,
    IReadOnlyList<FrameRateOption> AvailableFrameRates,
    uint Width,
    uint Height,
    double SelectedFrameRate,
    string SelectedVideoFormat,
    bool IsHdrEnabled,
    FrameRateTimingFamily PreferredTimingFamily);

/// <summary>
/// Pure selected-format and video-format-option policy for the capture mode UI.
/// </summary>
internal static class CaptureFormatSelectionPolicy
{
    internal static MediaFormat? Select(CaptureFormatSelectionRequest request)
    {
        var candidates = FilterResolutionAndHdr(
            request.AvailableFormats,
            request.Width,
            request.Height,
            request.IsHdrEnabled);
        if (candidates.Count == 0)
        {
            return null;
        }

        var rateSelection = ResolveFrameRateSelection(
            request.AvailableFrameRates,
            request.SelectedFrameRate,
            request.PreferredTimingFamily);
        var rateCandidates = candidates
            .Where(format => FrameRateTimingPolicy.GetFriendlyFrameRateBucket(format.FrameRateExact) == rateSelection.FriendlyBucket)
            .ToList();
        if (rateCandidates.Count == 0)
        {
            return null;
        }

        if (!string.Equals(request.SelectedVideoFormat, "Auto", StringComparison.OrdinalIgnoreCase))
        {
            rateCandidates = rateCandidates
                .Where(format => string.Equals(format.PixelFormat, request.SelectedVideoFormat, StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (rateCandidates.Count == 0)
            {
                return null;
            }
        }

        return FrameRateTimingPolicy.SelectPreferredFrameRateFormat(
            rateCandidates,
            rateSelection.FriendlyBucket,
            rateSelection.TimingFamily);
    }

    internal static IReadOnlyList<MediaFormat> SelectModeTupleFormats(CaptureFormatSelectionRequest request)
    {
        var rateSelection = ResolveFrameRateSelection(
            request.AvailableFrameRates,
            request.SelectedFrameRate,
            request.PreferredTimingFamily);

        return request.AvailableFormats
            .Where(format =>
                format.Width == request.Width &&
                format.Height == request.Height &&
                FrameRateTimingPolicy.GetFriendlyFrameRateBucket(format.FrameRateExact) == rateSelection.FriendlyBucket &&
                (request.IsHdrEnabled ? IsHdrModeCandidate(format) : !IsHdrModeCandidate(format)))
            .ToList();
    }

    private static IReadOnlyList<MediaFormat> FilterResolutionAndHdr(
        IReadOnlyList<MediaFormat> formats,
        uint width,
        uint height,
        bool isHdrEnabled)
    {
        var candidates = formats
            .Where(format => format.Width == width && format.Height == height)
            .ToList();
        if (isHdrEnabled)
        {
            return candidates.Where(IsHdrModeCandidate).ToList();
        }

        // When HDR is off, prefer 8-bit candidates so source-reader setup does not
        // request P010/P016 and then fall back to NV12 during capture startup.
        var sdrCandidates = candidates.Where(format => !IsHdrModeCandidate(format)).ToList();
        return sdrCandidates.Count > 0
            ? sdrCandidates
            : candidates;
    }

    private static CaptureFormatRateSelection ResolveFrameRateSelection(
        IReadOnlyList<FrameRateOption> availableFrameRates,
        double selectedFrameRate,
        FrameRateTimingFamily preferredTimingFamily)
    {
        var selectedRateOption = availableFrameRates
            .FirstOrDefault(option => FrameRateTimingPolicy.IsFrameRateMatch(option.Value, selectedFrameRate))
            ?? availableFrameRates.FirstOrDefault(option => FrameRateTimingPolicy.IsFriendlyFrameRateMatch(option.FriendlyValue, selectedFrameRate));
        var friendlyBucket = selectedRateOption != null
            ? (int)Math.Round(selectedRateOption.FriendlyValue, MidpointRounding.AwayFromZero)
            : FrameRateTimingPolicy.GetFriendlyFrameRateBucket(selectedFrameRate);

        var timingFamily = preferredTimingFamily;
        if (selectedRateOption != null &&
            FrameRateTimingPolicy.TryInferFrameRateTimingFamily(selectedRateOption.Rational, selectedRateOption.Value, out var optionFamily))
        {
            timingFamily = optionFamily;
        }

        return new CaptureFormatRateSelection(friendlyBucket, timingFamily);
    }

    private static bool IsHdrModeCandidate(MediaFormat format)
        => CaptureModeOptionsBuilder.IsHdrModeCandidate(format);
}

internal readonly record struct CaptureFormatRateSelection(
    int FriendlyBucket,
    FrameRateTimingFamily TimingFamily);

internal sealed record AutoCaptureSelection(
    ResolutionOption Resolution,
    int FriendlyFrameRate,
    double ExactFrameRate);

internal sealed record AutoCaptureSelectionRequest(
    IReadOnlyList<ResolutionOption> Options,
    IReadOnlyDictionary<string, List<MediaFormat>> FormatsByResolution,
    SourceSignalTelemetrySnapshot SourceTelemetry,
    bool IsHdrEnabled);

internal static class AutoCaptureSelectionPolicy
{
    internal static AutoCaptureSelection? Select(AutoCaptureSelectionRequest request)
    {
        if (request.Options.Count == 0)
        {
            return null;
        }

        var rankedOptions = request.Options
            .OrderByDescending(option => (long)option.Width * option.Height)
            .ThenByDescending(option => option.Width)
            .ToList();
        var eligibleOptions = rankedOptions.Where(option => option.IsEnabled).ToList();
        if (eligibleOptions.Count == 0)
        {
            eligibleOptions = rankedOptions;
        }

        var sourceFriendlyCap = request.SourceTelemetry.HasFrameRate
            ? (int?)Math.Round(request.SourceTelemetry.FrameRateExact!.Value, MidpointRounding.AwayFromZero)
            : null;
        var friendlyBuckets = eligibleOptions
            .SelectMany(option => GetAutoEligibleFormats(request, option))
            .Select(format => FrameRateTimingPolicy.GetFriendlyFrameRateBucket(format.FrameRateExact))
            .Distinct()
            .OrderByDescending(bucket => bucket)
            .ToList();
        if (friendlyBuckets.Count == 0)
        {
            return BuildFallback(request, eligibleOptions);
        }

        var bestFriendlyBucket = friendlyBuckets
            .FirstOrDefault(bucket => !sourceFriendlyCap.HasValue || bucket <= sourceFriendlyCap.Value);
        if (bestFriendlyBucket == 0)
        {
            bestFriendlyBucket = friendlyBuckets[0];
        }

        var matchingResolutions = eligibleOptions
            .Where(option => CaptureResolutionSelectionPolicy.ResolutionSupportsFriendlyFrameRate(
                request.FormatsByResolution,
                option.Value,
                bestFriendlyBucket,
                hdrOnly: request.IsHdrEnabled,
                sdrOnly: !request.IsHdrEnabled))
            .ToList();
        if (matchingResolutions.Count == 0)
        {
            matchingResolutions = eligibleOptions;
        }

        var chosenResolution = SelectBestResolutionCandidate(request, matchingResolutions) ?? eligibleOptions[0];
        var preferredFormat = SelectPreferredFrameRateFormat(request, chosenResolution.Value, bestFriendlyBucket);
        return new AutoCaptureSelection(
            chosenResolution,
            FrameRateTimingPolicy.GetFriendlyFrameRateBucket(preferredFormat.FrameRateExact),
            preferredFormat.FrameRateExact);
    }

    private static AutoCaptureSelection? BuildFallback(
        AutoCaptureSelectionRequest request,
        IReadOnlyList<ResolutionOption> options)
    {
        var fallback = options.FirstOrDefault();
        if (fallback == null)
        {
            return null;
        }

        var preferredBucket = GetMaxFrameRateFriendlyBucket(request, fallback.Value);
        var preferredFormat = SelectPreferredFrameRateFormat(request, fallback.Value, preferredBucket);
        return new AutoCaptureSelection(
            fallback,
            FrameRateTimingPolicy.GetFriendlyFrameRateBucket(preferredFormat.FrameRateExact),
            preferredFormat.FrameRateExact);
    }

    private static IEnumerable<MediaFormat> GetAutoEligibleFormats(
        AutoCaptureSelectionRequest request,
        ResolutionOption option)
    {
        if (!request.FormatsByResolution.TryGetValue(option.Value, out var formats))
        {
            return Enumerable.Empty<MediaFormat>();
        }

        var filtered = formats
            .Where(format => request.IsHdrEnabled
                ? CaptureModeOptionsBuilder.IsHdrModeCandidate(format)
                : !CaptureModeOptionsBuilder.IsHdrModeCandidate(format))
            .ToList();
        return filtered.Count > 0 ? filtered : formats;
    }

    private static ResolutionOption? SelectBestResolutionCandidate(
        AutoCaptureSelectionRequest request,
        IReadOnlyList<ResolutionOption> candidates)
    {
        if (candidates.Count == 0)
        {
            return null;
        }

        var ranked = candidates
            .OrderByDescending(option => (long)option.Width * option.Height)
            .ThenByDescending(option => option.Width)
            .ToList();
        if (!request.SourceTelemetry.HasDimensions)
        {
            return ranked[0];
        }

        var sourceWidth = (uint)Math.Max(0, request.SourceTelemetry.Width ?? 0);
        var sourceHeight = (uint)Math.Max(0, request.SourceTelemetry.Height ?? 0);
        if (sourceWidth == 0 || sourceHeight == 0)
        {
            return ranked[0];
        }

        return ranked.FirstOrDefault(option => option.Width <= sourceWidth && option.Height <= sourceHeight)
            ?? ranked[0];
    }

    private static MediaFormat SelectPreferredFrameRateFormat(
        AutoCaptureSelectionRequest request,
        string resolutionKey,
        int preferredFriendlyBucket)
    {
        if (!request.FormatsByResolution.TryGetValue(resolutionKey, out var formats) || formats.Count == 0)
        {
            throw new InvalidOperationException($"No formats are available for resolution '{resolutionKey}'.");
        }

        var timingFamily = FrameRateTimingFamily.Unknown;
        if (request.SourceTelemetry.HasFrameRate &&
            FrameRateTimingPolicy.TryInferFrameRateTimingFamily(
                request.SourceTelemetry.FrameRateArg,
                request.SourceTelemetry.FrameRateExact,
                out var sourceFamily))
        {
            timingFamily = sourceFamily;
        }

        var selectionPool = formats
            .Where(format =>
                (request.IsHdrEnabled
                    ? CaptureModeOptionsBuilder.IsHdrModeCandidate(format)
                    : !CaptureModeOptionsBuilder.IsHdrModeCandidate(format)) &&
                FrameRateTimingPolicy.GetFriendlyFrameRateBucket(format.FrameRateExact) == preferredFriendlyBucket)
            .ToList();
        if (selectionPool.Count == 0)
        {
            selectionPool = formats
                .Where(format => FrameRateTimingPolicy.GetFriendlyFrameRateBucket(format.FrameRateExact) == preferredFriendlyBucket)
                .ToList();
        }

        if (selectionPool.Count == 0)
        {
            selectionPool = formats.ToList();
            preferredFriendlyBucket = FrameRateTimingPolicy.GetFriendlyFrameRateBucket(selectionPool.Max(format => format.FrameRateExact));
        }

        return FrameRateTimingPolicy.SelectPreferredFrameRateFormat(selectionPool, preferredFriendlyBucket, timingFamily);
    }

    private static int GetMaxFrameRateFriendlyBucket(AutoCaptureSelectionRequest request, string resolutionKey)
    {
        if (!request.FormatsByResolution.TryGetValue(resolutionKey, out var formats) || formats.Count == 0)
        {
            return 0;
        }

        var filtered = formats
            .Where(format => !request.IsHdrEnabled || CaptureModeOptionsBuilder.IsHdrModeCandidate(format))
            .ToList();
        if (filtered.Count == 0)
        {
            filtered = formats.ToList();
        }

        return filtered
            .Select(format => FrameRateTimingPolicy.GetFriendlyFrameRateBucket(format.FrameRateExact))
            .DefaultIfEmpty()
            .Max();
    }
}

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

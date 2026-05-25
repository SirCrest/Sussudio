using System;
using System.Collections.Generic;
using System.Linq;
using Sussudio.Models;

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

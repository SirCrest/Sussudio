using System;
using System.Collections.Generic;
using System.Linq;
using Sussudio.Models;

namespace Sussudio.ViewModels;

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

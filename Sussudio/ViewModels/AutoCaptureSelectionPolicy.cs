using System;
using System.Collections.Generic;
using System.Linq;
using Sussudio.Models;
using Sussudio.Services.Capture;

namespace Sussudio.ViewModels;

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

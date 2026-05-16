using System;
using System.Collections.Generic;
using System.Linq;
using Sussudio.Models;
using Sussudio.Services.Capture;

namespace Sussudio.ViewModels;

/// <summary>
/// Automatic resolution ranking and source-aware auto-selection helpers.
/// </summary>
public partial class MainViewModel
{
    private sealed record AutoCaptureSelection(
        ResolutionOption Resolution,
        int FriendlyFrameRate,
        double ExactFrameRate);

    private bool ShouldSelectAutoResolutionOption(string? previousSelection)
        => IsAutoResolutionValue(previousSelection) ||
           string.IsNullOrWhiteSpace(previousSelection) ||
           !_hasUserOverriddenResolutionForCurrentMode;

    private ResolutionOption CreateAutoResolutionOption()
        => new()
        {
            Value = AutoResolutionValue,
            Width = 0,
            Height = 0,
            IsEnabled = true,
            DisplayTextOverride = BuildAutoResolutionDisplayText()
        };

    private AutoCaptureSelection? ResolveAutoCaptureSelection(IReadOnlyList<ResolutionOption> options)
    {
        if (options.Count == 0)
        {
            return null;
        }

        var rankedOptions = options
            .OrderByDescending(option => (long)option.Width * option.Height)
            .ThenByDescending(option => option.Width)
            .ToList();
        var eligibleOptions = rankedOptions.Where(option => option.IsEnabled).ToList();
        if (eligibleOptions.Count == 0)
        {
            eligibleOptions = rankedOptions;
        }

        var sourceFriendlyCap = _latestSourceTelemetry.HasFrameRate
            ? (int?)Math.Round(_latestSourceTelemetry.FrameRateExact!.Value, MidpointRounding.AwayFromZero)
            : null;
        var friendlyBuckets = eligibleOptions
            .SelectMany(GetAutoEligibleFormats)
            .Select(format => FrameRateTimingPolicy.GetFriendlyFrameRateBucket(format.FrameRateExact))
            .Distinct()
            .OrderByDescending(bucket => bucket)
            .ToList();
        if (friendlyBuckets.Count == 0)
        {
            return BuildAutoCaptureSelectionFallback(eligibleOptions);
        }

        var bestFriendlyBucket = friendlyBuckets
            .FirstOrDefault(bucket => !sourceFriendlyCap.HasValue || bucket <= sourceFriendlyCap.Value);
        if (bestFriendlyBucket == 0)
        {
            bestFriendlyBucket = friendlyBuckets[0];
        }

        var matchingResolutions = eligibleOptions
            .Where(option => ResolutionSupportsFriendlyFrameRate(
                option.Value,
                bestFriendlyBucket,
                hdrOnly: IsHdrEnabled,
                sdrOnly: !IsHdrEnabled))
            .ToList();
        if (matchingResolutions.Count == 0)
        {
            matchingResolutions = eligibleOptions;
        }

        var chosenResolution = SelectBestAutoResolutionCandidate(matchingResolutions) ?? eligibleOptions[0];
        var preferredFormat = SelectPreferredAutoFrameRateFormat(chosenResolution.Value, bestFriendlyBucket);
        return new AutoCaptureSelection(
            chosenResolution,
            FrameRateTimingPolicy.GetFriendlyFrameRateBucket(preferredFormat.FrameRateExact),
            preferredFormat.FrameRateExact);
    }

    private AutoCaptureSelection? BuildAutoCaptureSelectionFallback(IReadOnlyList<ResolutionOption> options)
    {
        var fallback = options.FirstOrDefault();
        if (fallback == null)
        {
            return null;
        }

        var preferredBucket = GetMaxFrameRateFriendlyBucket(fallback.Value);
        var preferredFormat = SelectPreferredAutoFrameRateFormat(fallback.Value, preferredBucket);
        return new AutoCaptureSelection(
            fallback,
            FrameRateTimingPolicy.GetFriendlyFrameRateBucket(preferredFormat.FrameRateExact),
            preferredFormat.FrameRateExact);
    }

    private IEnumerable<MediaFormat> GetAutoEligibleFormats(ResolutionOption option)
    {
        if (!_resolutionToFormats.TryGetValue(option.Value, out var formats))
        {
            return Enumerable.Empty<MediaFormat>();
        }

        var filtered = formats
            .Where(format => IsHdrEnabled ? IsHdrModeCandidate(format) : !IsHdrModeCandidate(format))
            .ToList();
        return filtered.Count > 0 ? filtered : formats;
    }

    private ResolutionOption? SelectBestAutoResolutionCandidate(IReadOnlyList<ResolutionOption> candidates)
    {
        if (candidates.Count == 0)
        {
            return null;
        }

        var ranked = candidates
            .OrderByDescending(option => (long)option.Width * option.Height)
            .ThenByDescending(option => option.Width)
            .ToList();
        if (!_latestSourceTelemetry.HasDimensions)
        {
            return ranked[0];
        }

        var sourceWidth = (uint)Math.Max(0, _latestSourceTelemetry.Width ?? 0);
        var sourceHeight = (uint)Math.Max(0, _latestSourceTelemetry.Height ?? 0);
        if (sourceWidth == 0 || sourceHeight == 0)
        {
            return ranked[0];
        }

        return ranked.FirstOrDefault(option => option.Width <= sourceWidth && option.Height <= sourceHeight)
            ?? ranked[0];
    }

    private MediaFormat SelectPreferredAutoFrameRateFormat(string resolutionKey, int preferredFriendlyBucket)
    {
        if (!_resolutionToFormats.TryGetValue(resolutionKey, out var formats) || formats.Count == 0)
        {
            throw new InvalidOperationException($"No formats are available for resolution '{resolutionKey}'.");
        }

        var timingFamily = FrameRateTimingFamily.Unknown;
        if (_latestSourceTelemetry.HasFrameRate &&
            FrameRateTimingPolicy.TryInferFrameRateTimingFamily(_latestSourceTelemetry.FrameRateArg, _latestSourceTelemetry.FrameRateExact, out var sourceFamily))
        {
            timingFamily = sourceFamily;
        }

        var selectionPool = formats
            .Where(format =>
                (IsHdrEnabled ? IsHdrModeCandidate(format) : !IsHdrModeCandidate(format)) &&
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

    private int GetMaxFrameRateFriendlyBucket(string resolutionKey)
    {
        if (!_resolutionToFormats.TryGetValue(resolutionKey, out var formats) || formats.Count == 0)
        {
            return 0;
        }

        var filtered = formats
            .Where(format => !IsHdrEnabled || IsHdrModeCandidate(format))
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

    private string BuildAutoResolutionDisplayText()
        => AutoResolutionValue;

}

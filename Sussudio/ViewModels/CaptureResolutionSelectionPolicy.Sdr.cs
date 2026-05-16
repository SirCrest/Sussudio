using System;
using System.Collections.Generic;
using System.Linq;
using Sussudio.Models;

namespace Sussudio.ViewModels;

internal static partial class CaptureResolutionSelectionPolicy
{
    private static SdrAutoResolutionSelection? SelectSdrAutoResolutionOption(
        IReadOnlyList<ResolutionOption> options,
        IReadOnlyDictionary<string, List<MediaFormat>> resolutionToFormats)
    {
        if (options.Count == 0)
        {
            return null;
        }

        var enabledOptions = options
            .Where(option => option.IsEnabled)
            .OrderByDescending(option => (long)option.Width * option.Height)
            .ToList();
        if (enabledOptions.Count == 0)
        {
            return null;
        }

        var sdrFriendlyBucketsByResolution = new Dictionary<string, HashSet<int>>(StringComparer.OrdinalIgnoreCase);
        foreach (var option in enabledOptions)
        {
            if (!resolutionToFormats.TryGetValue(option.Value, out var formats))
            {
                continue;
            }

            var buckets = formats
                .Where(format => !CaptureModeOptionsBuilder.IsHdrModeCandidate(format))
                .Select(format => GetFriendlyFrameRateBucket(format.FrameRateExact))
                .ToHashSet();
            if (buckets.Count > 0)
            {
                sdrFriendlyBucketsByResolution[option.Value] = buckets;
            }
        }

        if (sdrFriendlyBucketsByResolution.Count == 0)
        {
            return null;
        }

        foreach (var friendlyBucket in new[] { 60, 30 })
        {
            var match = enabledOptions.FirstOrDefault(option =>
                sdrFriendlyBucketsByResolution.TryGetValue(option.Value, out var buckets) &&
                buckets.Contains(friendlyBucket));
            if (match != null)
            {
                return new SdrAutoResolutionSelection(match, friendlyBucket);
            }
        }

        var selected = enabledOptions.FirstOrDefault(option => sdrFriendlyBucketsByResolution.ContainsKey(option.Value));
        if (selected == null)
        {
            return null;
        }

        return new SdrAutoResolutionSelection(
            selected,
            ResolvePreferredFriendlyBucketForResolution(resolutionToFormats, selected.Value, sdrOnly: true) ?? 30);
    }

    private static ResolutionOption? SelectSdrResolutionOption(
        IReadOnlyList<ResolutionOption> options,
        string? preferredSelection)
        => options.FirstOrDefault(option =>
            option.IsEnabled &&
            string.Equals(option.Value, preferredSelection, StringComparison.OrdinalIgnoreCase))
            ?? options.FirstOrDefault(option => option.IsEnabled)
            ?? options.FirstOrDefault();

    private static int? ResolvePreferredFriendlyBucketForResolution(
        IReadOnlyDictionary<string, List<MediaFormat>> resolutionToFormats,
        string resolutionKey,
        bool sdrOnly)
    {
        if (!resolutionToFormats.TryGetValue(resolutionKey, out var formats))
        {
            return null;
        }

        var buckets = formats
            .Where(format => !sdrOnly || !CaptureModeOptionsBuilder.IsHdrModeCandidate(format))
            .Select(format => GetFriendlyFrameRateBucket(format.FrameRateExact))
            .Distinct()
            .OrderByDescending(bucket => bucket)
            .ToList();
        if (buckets.Count == 0)
        {
            return null;
        }

        if (buckets.Contains(60))
        {
            return 60;
        }

        if (buckets.Contains(30))
        {
            return 30;
        }

        return buckets[0];
    }
}

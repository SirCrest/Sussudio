using System;
using System.Collections.Generic;
using System.Linq;
using Sussudio.Models;

namespace Sussudio.ViewModels;

internal static partial class CaptureResolutionSelectionPolicy
{
    internal static bool TryParseResolutionKey(string? resolutionKey, out uint width, out uint height)
    {
        width = 0;
        height = 0;
        if (string.IsNullOrWhiteSpace(resolutionKey) || IsAutoResolutionValue(resolutionKey))
        {
            return false;
        }

        var parts = resolutionKey.Split('x');
        return parts.Length == 2 &&
               uint.TryParse(parts[0], out width) &&
               uint.TryParse(parts[1], out height);
    }

    internal static bool ResolutionSupportsFrameRate(
        IReadOnlyDictionary<string, List<MediaFormat>> resolutionToFormats,
        string resolutionKey,
        double frameRate,
        bool hdrOnly)
    {
        if (frameRate <= 0)
        {
            return false;
        }

        var requestedBucket = GetFriendlyFrameRateBucket(frameRate);
        return ResolutionSupportsFriendlyFrameRate(
            resolutionToFormats,
            resolutionKey,
            requestedBucket,
            hdrOnly: hdrOnly,
            sdrOnly: !hdrOnly);
    }

    internal static bool ResolutionSupportsFriendlyFrameRate(
        IReadOnlyDictionary<string, List<MediaFormat>> resolutionToFormats,
        string resolutionKey,
        int friendlyBucket,
        bool hdrOnly,
        bool sdrOnly)
    {
        if (!resolutionToFormats.TryGetValue(resolutionKey, out var formats))
        {
            return false;
        }

        return formats.Any(format =>
            (!hdrOnly || CaptureModeOptionsBuilder.IsHdrModeCandidate(format)) &&
            (!sdrOnly || !CaptureModeOptionsBuilder.IsHdrModeCandidate(format)) &&
            GetFriendlyFrameRateBucket(format.FrameRateExact) == friendlyBucket);
    }

    private static int GetFriendlyFrameRateBucket(double frameRate)
        => (int)Math.Round(frameRate, MidpointRounding.AwayFromZero);

    private static bool IsAutoResolutionValue(string? resolutionValue)
        => string.Equals(resolutionValue, "Source", StringComparison.OrdinalIgnoreCase);
}

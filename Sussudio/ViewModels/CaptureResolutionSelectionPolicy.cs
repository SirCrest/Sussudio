using System;
using System.Collections.Generic;
using System.Linq;
using Sussudio.Models;

namespace Sussudio.ViewModels;

internal static class CaptureResolutionSelectionPolicy
{
    internal static CaptureResolutionSelection Select(CaptureResolutionSelectionRequest request)
    {
        if (request.Options.Count == 0)
        {
            return new CaptureResolutionSelection(null, null, null);
        }

        var sourceSelected = request.AllowSourceAutoSelect
            ? SelectSourceResolutionOption(request.Options, request.PreferredSelection, request.SourceTelemetry)
            : null;
        var sourceSelectedValue = sourceSelected?.Value;
        string? hdrHint = null;
        if (request.IsHdrEnabled &&
            sourceSelected is { IsEnabled: true } &&
            request.PreviousFrameRate > 0 &&
            !ResolutionSupportsFrameRate(
                request.ResolutionToFormats,
                sourceSelected.Value,
                request.PreviousFrameRate,
                hdrOnly: true))
        {
            var sourceMax = GetMaxFrameRateForResolution(
                request.ResolutionToFormats,
                sourceSelected.Value,
                hdrOnly: true);
            if (sourceMax > 0)
            {
                hdrHint = $"HDR at {sourceSelected.Value} supported up to {FormatFriendlyFrameRate(sourceMax)} fps.";
            }

            sourceSelected = null;
        }

        var selected = sourceSelected;
        int? sdrAutoFriendlyBucket = null;
        if (!request.IsHdrEnabled &&
            request.PendingSdrAutoSelectionForDeviceChange)
        {
            var sdrAutoSelection = SelectSdrAutoResolutionOption(
                request.Options,
                request.ResolutionToFormats);
            if (sdrAutoSelection != null)
            {
                selected = sdrAutoSelection.Selected;
                sdrAutoFriendlyBucket = sdrAutoSelection.SelectedFriendlyBucket;
            }
        }

        if (selected == null)
        {
            if (request.IsHdrEnabled)
            {
                var hdrSelection = SelectHdrResolutionOption(
                    request.Options,
                    request.ResolutionToFormats,
                    request.PreferredSelection,
                    request.PreviousFrameRate);
                selected = hdrSelection.Selected;
                hdrHint = hdrSelection.Hint ?? hdrHint;
            }
            else
            {
                selected = SelectSdrResolutionOption(request.Options, request.PreferredSelection);
            }

            if (request.IsHdrEnabled &&
                !string.IsNullOrWhiteSpace(sourceSelectedValue) &&
                selected != null &&
                !string.Equals(sourceSelectedValue, selected.Value, StringComparison.OrdinalIgnoreCase) &&
                request.PreviousFrameRate > 0)
            {
                var sourceMax = GetMaxFrameRateForResolution(
                    request.ResolutionToFormats,
                    sourceSelectedValue,
                    hdrOnly: true);
                if (sourceMax > 0 && request.PreviousFrameRate > sourceMax + 0.01)
                {
                    hdrHint = $"HDR at {sourceSelectedValue} supported up to {FormatFriendlyFrameRate(sourceMax)} fps; switched to {selected.Value} to keep {FormatFriendlyFrameRate(request.PreviousFrameRate)} fps.";
                }
            }
        }

        return new CaptureResolutionSelection(
            selected,
            hdrHint,
            sdrAutoFriendlyBucket);
    }

    internal static string BuildHdrSupportHint(HdrSupportHintRequest request)
    {
        if (!request.IsHdrEnabled || string.IsNullOrWhiteSpace(request.ResolutionKey))
        {
            return string.Empty;
        }

        var maxHdrRate = GetMaxFrameRateForResolution(
            request.ResolutionToFormats,
            request.ResolutionKey,
            hdrOnly: true);
        if (maxHdrRate <= 0)
        {
            return $"HDR is not supported at {request.ResolutionKey}.";
        }

        if (request.SelectedFrameRate > 0 && maxHdrRate >= request.SelectedFrameRate - 0.01)
        {
            return string.Empty;
        }

        return $"HDR at {request.ResolutionKey} supported up to {FormatFriendlyFrameRate(maxHdrRate)} fps.";
    }

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

    private static ResolutionOption? SelectSourceResolutionOption(
        IReadOnlyList<ResolutionOption> options,
        string? previousSelection,
        SourceSignalTelemetrySnapshot sourceTelemetry)
    {
        if (options.Count == 0 || !sourceTelemetry.HasDimensions)
        {
            return null;
        }

        var sourceWidth = (uint)Math.Max(0, sourceTelemetry.Width ?? 0);
        var sourceHeight = (uint)Math.Max(0, sourceTelemetry.Height ?? 0);
        if (sourceWidth == 0 || sourceHeight == 0)
        {
            return null;
        }

        var exact = options.FirstOrDefault(option =>
            option.IsEnabled &&
            option.Width == sourceWidth &&
            option.Height == sourceHeight);
        if (exact != null)
        {
            return exact;
        }

        var sourceKey = GetResolutionKey(sourceWidth, sourceHeight);
        var enabled = options.Where(option => option.IsEnabled).ToList();
        if (enabled.Count == 0)
        {
            return options.FirstOrDefault();
        }

        return SelectNearestResolution(sourceKey, enabled)
            ?? SelectNearestResolution(previousSelection, enabled)
            ?? enabled.FirstOrDefault();
    }

    private static HdrResolutionSelection SelectHdrResolutionOption(
        IReadOnlyList<ResolutionOption> options,
        IReadOnlyDictionary<string, List<MediaFormat>> resolutionToFormats,
        string? previousSelection,
        double preferredFrameRate)
    {
        if (options.Count == 0)
        {
            return new HdrResolutionSelection(null, null);
        }

        var previous = options.FirstOrDefault(option =>
            string.Equals(option.Value, previousSelection, StringComparison.OrdinalIgnoreCase));
        if (previous is { IsEnabled: true } &&
            ResolutionSupportsFrameRate(resolutionToFormats, previous.Value, preferredFrameRate, hdrOnly: true))
        {
            return new HdrResolutionSelection(
                previous,
                BuildHdrSupportHint(new HdrSupportHintRequest(
                    resolutionToFormats,
                    previous.Value,
                    IsHdrEnabled: true,
                    SelectedFrameRate: preferredFrameRate)));
        }

        var sameFpsCandidates = options
            .Where(option =>
                option.IsEnabled &&
                ResolutionSupportsFrameRate(resolutionToFormats, option.Value, preferredFrameRate, hdrOnly: true))
            .ToList();

        // Prefer an HDR-capable mode in the same friendly FPS bucket before considering
        // any other HDR mode. SelectNearestResolution intentionally favors the nearest
        // lower resolution, so 4K120 SDR retargets to 1080p120 HDR before falling to 4K60.
        var selected = SelectNearestResolution(previousSelection, sameFpsCandidates)
            ?? SelectNearestResolution(previousSelection, options.Where(option => option.IsEnabled).ToList())
            ?? options.FirstOrDefault(option => option.IsEnabled)
            ?? options.FirstOrDefault();

        string? hint = null;
        if (!string.IsNullOrWhiteSpace(previousSelection) &&
            !string.Equals(previousSelection, selected?.Value, StringComparison.OrdinalIgnoreCase))
        {
            var previousMax = GetMaxFrameRateForResolution(resolutionToFormats, previousSelection, hdrOnly: true);
            if (previousMax > 0)
            {
                hint = $"HDR at {previousSelection} supported up to {FormatFriendlyFrameRate(previousMax)} fps.";
            }
        }

        hint ??= BuildHdrSupportHint(new HdrSupportHintRequest(
            resolutionToFormats,
            selected?.Value,
            IsHdrEnabled: true,
            SelectedFrameRate: preferredFrameRate));
        return new HdrResolutionSelection(selected, hint);
    }

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

    private static ResolutionOption? SelectNearestResolution(
        string? baselineResolution,
        IReadOnlyList<ResolutionOption> candidates)
    {
        if (candidates.Count == 0)
        {
            return null;
        }

        if (!TryParseResolutionKey(baselineResolution, out var baseWidth, out var baseHeight))
        {
            return candidates
                .OrderByDescending(option => (long)option.Width * option.Height)
                .FirstOrDefault();
        }

        var baseArea = (long)baseWidth * baseHeight;
        var lowerCandidate = candidates
            .Where(option => ((long)option.Width * option.Height) < baseArea)
            .OrderByDescending(option => (long)option.Width * option.Height)
            .FirstOrDefault();
        if (lowerCandidate != null)
        {
            return lowerCandidate;
        }

        return candidates
            .OrderBy(option => Math.Abs(((long)option.Width * option.Height) - baseArea))
            .ThenByDescending(option => (long)option.Width * option.Height)
            .FirstOrDefault();
    }

    private static double GetMaxFrameRateForResolution(
        IReadOnlyDictionary<string, List<MediaFormat>> resolutionToFormats,
        string? resolutionKey,
        bool hdrOnly)
    {
        if (string.IsNullOrWhiteSpace(resolutionKey) ||
            !resolutionToFormats.TryGetValue(resolutionKey, out var formats))
        {
            return 0;
        }

        var candidates = hdrOnly
            ? formats.Where(CaptureModeOptionsBuilder.IsHdrModeCandidate).ToList()
            : formats;
        if (candidates.Count == 0)
        {
            return 0;
        }

        return candidates.Max(format => format.FrameRateExact);
    }

    private static int GetFriendlyFrameRateBucket(double frameRate)
        => (int)Math.Round(frameRate, MidpointRounding.AwayFromZero);

    private static string FormatFriendlyFrameRate(double frameRate)
        => $"{Math.Round(frameRate):0}";

    private static string GetResolutionKey(uint width, uint height)
        => $"{width}x{height}";

    private static bool IsAutoResolutionValue(string? resolutionValue)
        => string.Equals(resolutionValue, "Source", StringComparison.OrdinalIgnoreCase);
}

internal sealed record CaptureResolutionSelectionRequest(
    IReadOnlyList<ResolutionOption> Options,
    IReadOnlyDictionary<string, List<MediaFormat>> ResolutionToFormats,
    SourceSignalTelemetrySnapshot SourceTelemetry,
    string? PreferredSelection,
    double PreviousFrameRate,
    bool IsHdrEnabled,
    bool AllowSourceAutoSelect,
    bool PendingSdrAutoSelectionForDeviceChange);

internal sealed record CaptureResolutionSelection(
    ResolutionOption? Selected,
    string? HdrHint,
    int? SdrAutoFriendlyFrameRateBucket);

internal sealed record HdrSupportHintRequest(
    IReadOnlyDictionary<string, List<MediaFormat>> ResolutionToFormats,
    string? ResolutionKey,
    bool IsHdrEnabled,
    double SelectedFrameRate);

internal sealed record HdrResolutionSelection(
    ResolutionOption? Selected,
    string? Hint);

internal sealed record SdrAutoResolutionSelection(
    ResolutionOption Selected,
    int SelectedFriendlyBucket);

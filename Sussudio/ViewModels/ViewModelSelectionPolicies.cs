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

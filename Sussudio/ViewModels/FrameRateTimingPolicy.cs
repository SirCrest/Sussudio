using System;
using System.Collections.Generic;
using System.Linq;
using Sussudio.Models;

namespace Sussudio.ViewModels;

internal enum FrameRateTimingFamily
{
    Unknown,
    Integer,
    Ntsc1001
}

internal readonly record struct FrameRateTimingVariant(int FriendlyBucket, FrameRateTimingFamily Family);

internal readonly record struct FrameRateAutoSelectionSource(
    double? Rate,
    bool TimingFamilyKnown,
    FrameRateTimingFamily TimingFamily);

internal sealed record FrameRateAutoSelectionRequest(
    IReadOnlyList<FrameRateOption> Options,
    bool AutoFrameRateOptionAvailable,
    bool ForceAutoSelection,
    bool IsAutoFrameRateSelected,
    bool HasUserOverriddenFrameRateForCurrentMode,
    bool IsHdrEnabled,
    bool PendingSdrAutoSelectionForDeviceChange,
    int? PendingSdrAutoFriendlyFrameRateBucket,
    FrameRateAutoSelectionSource Source,
    double PreviousRate);

internal sealed record FrameRateAutoSelection(
    FrameRateOption? Selected,
    bool SelectAutoOption);

internal static class FrameRateAutoSelectionPolicy
{
    internal static FrameRateAutoSelection Select(FrameRateAutoSelectionRequest request)
    {
        var selectAutoOption = request.ForceAutoSelection ||
                               (request.AutoFrameRateOptionAvailable &&
                                (request.IsAutoFrameRateSelected ||
                                 !request.HasUserOverriddenFrameRateForCurrentMode));

        var selected = selectAutoOption
            ? SelectPendingSdrBucket(request.Options, request)
            : null;
        selected ??= selectAutoOption
            ? SelectNearestSourceRate(request.Options, request.Source)
            : null;
        selected ??= selectAutoOption
            ? SelectAutoFallback(request.Options)
            : SelectPreviousFallback(request.Options, request.PreviousRate);

        return new FrameRateAutoSelection(selected, selectAutoOption);
    }

    private static FrameRateOption? SelectPendingSdrBucket(
        IReadOnlyList<FrameRateOption> options,
        FrameRateAutoSelectionRequest request)
    {
        if (request.IsHdrEnabled ||
            !request.PendingSdrAutoSelectionForDeviceChange ||
            !request.PendingSdrAutoFriendlyFrameRateBucket.HasValue)
        {
            return null;
        }

        return options.FirstOrDefault(option =>
            option.IsEnabled &&
            FrameRateTimingPolicy.IsFriendlyFrameRateMatch(
                option.FriendlyValue,
                request.PendingSdrAutoFriendlyFrameRateBucket.Value));
    }

    private static FrameRateOption? SelectNearestSourceRate(
        IReadOnlyList<FrameRateOption> options,
        FrameRateAutoSelectionSource source)
    {
        if (!source.Rate.HasValue)
        {
            return null;
        }

        return options
            .Where(option => option.IsEnabled)
            .OrderBy(option => Math.Abs(option.Value - source.Rate.Value))
            .ThenBy(option =>
                source.TimingFamilyKnown &&
                FrameRateTimingPolicy.TryInferFrameRateTimingFamily(option.Rational, option.Value, out var optionFamily) &&
                optionFamily == source.TimingFamily
                    ? 0
                    : 1)
            .FirstOrDefault();
    }

    private static FrameRateOption? SelectAutoFallback(IReadOnlyList<FrameRateOption> options)
        => options.FirstOrDefault(option => option.IsEnabled)
           ?? options.FirstOrDefault();

    private static FrameRateOption? SelectPreviousFallback(
        IReadOnlyList<FrameRateOption> options,
        double previousRate)
        => options.FirstOrDefault(option =>
                option.IsEnabled && FrameRateTimingPolicy.IsFrameRateMatch(option.Value, previousRate))
           ?? options.FirstOrDefault(option =>
                option.IsEnabled && FrameRateTimingPolicy.IsFriendlyFrameRateMatch(option.FriendlyValue, previousRate))
           ?? options.FirstOrDefault(option =>
                option.IsEnabled && FrameRateTimingPolicy.IsFriendlyFrameRateMatch(option.FriendlyValue, 60))
           ?? options.FirstOrDefault(option =>
                option.IsEnabled && FrameRateTimingPolicy.IsFriendlyFrameRateMatch(option.FriendlyValue, 30))
           ?? options.FirstOrDefault(option => option.IsEnabled)
           ?? options.FirstOrDefault();
}

internal sealed record FrameRateSourceFilterResult(
    IReadOnlyList<FrameRateOption> Options,
    bool SourceTimingFamilyKnown,
    FrameRateTimingFamily SourceTimingFamily);

internal static class FrameRateSourceFilterPolicy
{
    internal static FrameRateSourceFilterResult Apply(
        IReadOnlyList<FrameRateOption> options,
        double? sourceRate,
        string? sourceRateArg,
        IReadOnlyCollection<FrameRateTimingVariant> resolutionTimingVariants,
        bool showAllCaptureOptions)
    {
        var sourceTimingFamilyKnown = FrameRateTimingPolicy.TryInferFrameRateTimingFamily(sourceRateArg, sourceRate, out var sourceTimingFamily);
        var sourceFriendlyRate = sourceRate.HasValue
            ? Math.Round(sourceRate.Value, MidpointRounding.AwayFromZero)
            : (double?)null;
        var cappedOptions = options
            .Select(option => ApplySourceLimit(
                option,
                sourceRate,
                sourceRateArg,
                sourceFriendlyRate,
                sourceTimingFamilyKnown,
                sourceTimingFamily,
                resolutionTimingVariants))
            .ToList();

        var filteredOptions = showAllCaptureOptions
            ? cappedOptions
                .Select(option =>
                {
                    if (option.IsEnabled || !IsSourceFilteredFrameRateDisableReason(option.DisableReason))
                    {
                        return option;
                    }

                    return CloneOption(option, isEnabled: true, disableReason: string.Empty);
                })
                .ToList()
            : cappedOptions
                .Where(option => option.IsEnabled || !IsSourceFilteredFrameRateDisableReason(option.DisableReason))
                .ToList();

        return new FrameRateSourceFilterResult(filteredOptions, sourceTimingFamilyKnown, sourceTimingFamily);
    }

    private static FrameRateOption ApplySourceLimit(
        FrameRateOption option,
        double? sourceRate,
        string? sourceRateArg,
        double? sourceFriendlyRate,
        bool sourceTimingFamilyKnown,
        FrameRateTimingFamily sourceTimingFamily,
        IReadOnlyCollection<FrameRateTimingVariant> resolutionTimingVariants)
    {
        var enabled = option.IsEnabled;
        var disableReason = option.DisableReason;

        if (enabled && sourceFriendlyRate.HasValue)
        {
            if (option.FriendlyValue > sourceFriendlyRate.Value + 0.01)
            {
                enabled = false;
                disableReason = $"Source signal is {sourceFriendlyRate.Value:0} fps; higher capture fps duplicates frames.";
            }
            else if (sourceTimingFamilyKnown &&
                     sourceRate.HasValue &&
                     FrameRateTimingPolicy.TryInferFrameRateTimingFamily(option.Rational, option.Value, out var optionFamily) &&
                     optionFamily != FrameRateTimingFamily.Unknown &&
                     sourceTimingFamily != FrameRateTimingFamily.Unknown &&
                     optionFamily != sourceTimingFamily &&
                     HasTimingFamilyVariant(resolutionTimingVariants, option.FriendlyValue, sourceTimingFamily) &&
                     FrameRateTimingPolicy.IsFriendlyFrameRateMatch(option.FriendlyValue, sourceFriendlyRate.Value) &&
                     option.Value > sourceRate.Value + 0.03)
            {
                enabled = false;
                disableReason = $"Source timing is {sourceRateArg ?? sourceRate.Value.ToString("0.###")} so this duplicate variant is hidden.";
            }
            else
            {
                var roundedSourceFriendlyRate = (int)Math.Round(sourceFriendlyRate.Value, MidpointRounding.AwayFromZero);
                var roundedOptionFriendlyRate = (int)Math.Round(option.FriendlyValue, MidpointRounding.AwayFromZero);
                if (roundedOptionFriendlyRate > 0 &&
                    roundedOptionFriendlyRate <= roundedSourceFriendlyRate &&
                    roundedSourceFriendlyRate % roundedOptionFriendlyRate != 0)
                {
                    enabled = false;
                    disableReason = $"{roundedOptionFriendlyRate:0} fps is not a clean divisor of source {roundedSourceFriendlyRate:0} fps.";
                }
            }
        }

        return CloneOption(option, enabled, enabled ? string.Empty : disableReason);
    }

    private static bool HasTimingFamilyVariant(
        IReadOnlyCollection<FrameRateTimingVariant> resolutionTimingVariants,
        double friendlyFrameRate,
        FrameRateTimingFamily family)
    {
        if (family == FrameRateTimingFamily.Unknown || resolutionTimingVariants.Count == 0)
        {
            return false;
        }

        var bucket = (int)Math.Round(friendlyFrameRate, MidpointRounding.AwayFromZero);
        return resolutionTimingVariants.Any(variant =>
            variant.FriendlyBucket == bucket &&
            variant.Family == family);
    }

    private static FrameRateOption CloneOption(FrameRateOption option, bool isEnabled, string disableReason)
        => new()
        {
            FriendlyValue = option.FriendlyValue,
            Value = option.Value,
            Rational = option.Rational,
            Numerator = option.Numerator,
            Denominator = option.Denominator,
            IsEnabled = isEnabled,
            DisableReason = disableReason,
            DisplayTextOverride = option.DisplayTextOverride
        };

    private static bool IsSourceFilteredFrameRateDisableReason(string? disableReason)
        => !string.IsNullOrWhiteSpace(disableReason) &&
           (disableReason.IndexOf("higher capture fps", StringComparison.OrdinalIgnoreCase) >= 0 ||
            disableReason.IndexOf("duplicate variant", StringComparison.OrdinalIgnoreCase) >= 0 ||
            disableReason.IndexOf("not a clean divisor", StringComparison.OrdinalIgnoreCase) >= 0);
}

/// <summary>
/// Pure frame-rate timing, rational parsing, and preferred-format ranking policy.
/// </summary>
internal static class FrameRateTimingPolicy
{
    internal static IReadOnlyList<FrameRateTimingVariant> BuildTimingVariants(IEnumerable<MediaFormat> formats)
        => formats
            .Select(format => TryInferFrameRateTimingFamily(format.FrameRateRational, format.FrameRateExact, out var family)
                ? new FrameRateTimingVariant(GetFriendlyFrameRateBucket(format.FrameRateExact), family)
                : (FrameRateTimingVariant?)null)
            .Where(variant => variant.HasValue)
            .Select(variant => variant!.Value)
            .ToList();

    internal static MediaFormat SelectPreferredFrameRateFormat(
        IReadOnlyList<MediaFormat> candidates,
        int friendlyBucket,
        FrameRateTimingFamily timingFamily)
    {
        if (candidates.Count == 0)
        {
            throw new InvalidOperationException("No frame-rate candidates are available.");
        }

        return candidates
            .OrderBy(format => GetTimingFamilyRank(format, friendlyBucket, timingFamily))
            .ThenBy(format => Math.Abs(format.FrameRateExact - friendlyBucket))
            .ThenByDescending(CaptureModeOptionsBuilder.IsHdrModeCandidate)
            .ThenBy(format => GetEffectivePixelFormatPriority(format))
            .First();
    }

    /// <summary>
    /// At 4K HFR (>=3840x2160 @ >=100fps SDR), prefer MJPG over NV12. The UVC driver
    /// presents NV12 at these rates, but it is actually CPU-decoded MJPG causing frame
    /// drops. Selecting raw MJPG lets MF use GPU DXVA decode via hardware transforms.
    /// </summary>
    internal static int GetEffectivePixelFormatPriority(MediaFormat format)
    {
        if (format.Width >= 3840 &&
            format.Height >= 2160 &&
            format.FrameRateExact >= 100 &&
            format.PixelFormat.Equals("MJPG", StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }

        return MediaFormat.GetPixelFormatPriority(format.PixelFormat);
    }

    internal static int GetTimingFamilyRank(MediaFormat format, int friendlyBucket, FrameRateTimingFamily timingFamily)
    {
        if (format.FrameRateNumerator > 0 && format.FrameRateDenominator > 0)
        {
            return timingFamily switch
            {
                FrameRateTimingFamily.Ntsc1001 when format.FrameRateDenominator == 1001
                    => Math.Abs((int)format.FrameRateNumerator - friendlyBucket * 1000),
                FrameRateTimingFamily.Integer when format.FrameRateDenominator == 1
                    => Math.Abs((int)format.FrameRateNumerator - friendlyBucket),
                FrameRateTimingFamily.Ntsc1001 => 5000 + Math.Abs((int)format.FrameRateNumerator - friendlyBucket * 1000),
                FrameRateTimingFamily.Integer => 5000 + Math.Abs((int)format.FrameRateNumerator - friendlyBucket),
                _ => 100 + (int)Math.Round(Math.Abs(format.FrameRateExact - friendlyBucket) * 100)
            };
        }

        return 100 + (int)Math.Round(Math.Abs(format.FrameRateExact - friendlyBucket) * 100);
    }

    internal static bool TryInferFrameRateTimingFamily(
        string? frameRateArg,
        double? frameRate,
        out FrameRateTimingFamily family)
    {
        family = FrameRateTimingFamily.Unknown;

        if (TryParseFrameRateRational(frameRateArg, out _, out var denominator))
        {
            if (denominator == 1001)
            {
                family = FrameRateTimingFamily.Ntsc1001;
                return true;
            }

            if (denominator == 1)
            {
                family = FrameRateTimingFamily.Integer;
                return true;
            }
        }

        if (!frameRate.HasValue || frameRate.Value <= 0)
        {
            return false;
        }

        var value = frameRate.Value;
        var rounded = Math.Round(value);
        if (Math.Abs(value - rounded) <= 0.01)
        {
            family = FrameRateTimingFamily.Integer;
            return true;
        }

        var ntscCandidate = rounded * 1000.0 / 1001.0;
        if (Math.Abs(value - ntscCandidate) <= 0.03)
        {
            family = FrameRateTimingFamily.Ntsc1001;
            return true;
        }

        return false;
    }

    internal static bool IsFrameRateMatch(double a, double b, double tolerance = 0.01)
        => Math.Abs(a - b) < tolerance;

    internal static bool IsFriendlyFrameRateMatch(double optionFriendlyRate, double requestedRate)
        => Math.Round(optionFriendlyRate) == Math.Round(requestedRate);

    internal static bool IsAutoFrameRateValue(double value)
        => value == 0 || value < 0;

    internal static int GetFriendlyFrameRateBucket(double frameRate)
        => (int)Math.Round(frameRate, MidpointRounding.AwayFromZero);

    internal static bool TryParseFrameRateRational(string? rational, out uint numerator, out uint denominator)
    {
        numerator = 0;
        denominator = 0;
        if (string.IsNullOrWhiteSpace(rational))
        {
            return false;
        }

        var split = rational.Split('/');
        return split.Length == 2 &&
               uint.TryParse(split[0], out numerator) &&
               uint.TryParse(split[1], out denominator) &&
               denominator > 0;
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using Sussudio.Models;

namespace Sussudio.ViewModels;

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

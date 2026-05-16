using System;
using System.Collections.Generic;
using System.Linq;
using Sussudio.Models;

namespace Sussudio.ViewModels;

public partial class MainViewModel
{
    private readonly record struct FrameRateAutoSelectionSource(
        double? Rate,
        bool TimingFamilyKnown,
        FrameRateTimingFamily TimingFamily);

    private sealed record FrameRateAutoSelectionRequest(
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

    private sealed record FrameRateAutoSelection(
        FrameRateOption? Selected,
        bool SelectAutoOption);

    private static class FrameRateAutoSelectionPolicy
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
                IsFriendlyFrameRateMatch(
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
                    TryInferFrameRateTimingFamily(option.Rational, option.Value, out var optionFamily) &&
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
                    option.IsEnabled && IsFrameRateMatch(option.Value, previousRate))
               ?? options.FirstOrDefault(option =>
                    option.IsEnabled && IsFriendlyFrameRateMatch(option.FriendlyValue, previousRate))
               ?? options.FirstOrDefault(option =>
                    option.IsEnabled && IsFriendlyFrameRateMatch(option.FriendlyValue, 60))
               ?? options.FirstOrDefault(option =>
                    option.IsEnabled && IsFriendlyFrameRateMatch(option.FriendlyValue, 30))
               ?? options.FirstOrDefault(option => option.IsEnabled)
               ?? options.FirstOrDefault();
    }
}

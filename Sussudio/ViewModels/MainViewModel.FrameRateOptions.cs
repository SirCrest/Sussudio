using System;
using System.Collections.Generic;
using System.Linq;
using Sussudio.Models;

namespace Sussudio.ViewModels;

/// <summary>
/// Frame-rate option building, source-rate filtering, and automatic frame-rate selection.
/// </summary>
public partial class MainViewModel
{
    partial void OnSelectedFrameRateChanged(double value)
    {
        if (IsAutoFrameRateValue(value))
        {
            SelectAutoFrameRate(rebuildOptions: !IsRecording && !_isRebuildingModeOptions && !_isApplyingAutomaticFrameRateSelection);
            return;
        }

        if (!_isRebuildingModeOptions && !_isApplyingAutomaticFrameRateSelection)
        {
            IsAutoFrameRateSelected = false;
            _hasUserOverriddenFrameRateForCurrentMode = true;
            _pendingSdrAutoSelectionForDeviceChange = false;
            _pendingSdrAutoFriendlyFrameRateBucket = null;
        }

        var selected = AvailableFrameRates
            .FirstOrDefault(option => IsFrameRateMatch(option.Value, value))
            ?? AvailableFrameRates.FirstOrDefault(option => IsFriendlyFrameRateMatch(option.FriendlyValue, value));
        SelectedFriendlyFrameRate = selected?.FriendlyValue ?? Math.Round(value, MidpointRounding.AwayFromZero);
        SelectedExactFrameRate = selected?.Value ?? value;
        SelectedExactFrameRateArg = selected?.Rational;
        if (IsAutoResolutionValue(SelectedResolution))
        {
            AutoResolvedFrameRate = selected?.Value ?? value;
        }

        RebuildVideoFormatOptions();
        UpdateSelectedFormat();
        UpdateTargetSummary();
    }

    public void SelectAutoFrameRate()
        => SelectAutoFrameRate(rebuildOptions: !IsRecording && !_isRebuildingModeOptions && !_isApplyingAutomaticFrameRateSelection);

    private void SelectAutoFrameRate(bool rebuildOptions)
    {
        IsAutoFrameRateSelected = true;
        _hasUserOverriddenFrameRateForCurrentMode = false;
        _pendingSdrAutoSelectionForDeviceChange = false;
        _pendingSdrAutoFriendlyFrameRateBucket = null;

        if (rebuildOptions)
        {
            RebuildFrameRateOptions();
            return;
        }

        var currentOptions = AvailableFrameRates
            .Where(option => !IsAutoFrameRateValue(option.FriendlyValue))
            .ToList();
        var selectedResolutionKey = GetEffectiveResolutionKey(SelectedResolution);
        var sourceRate = ResolveDetectedSourceFrameRate(selectedResolutionKey, currentOptions, SelectedFrameRate);
        var sourceTimingFamilyKnown = TryInferFrameRateTimingFamily(sourceRate.Arg, sourceRate.Rate, out var sourceTimingFamily);
        FrameRateOption? selected = null;
        if (!IsHdrEnabled &&
            _pendingSdrAutoSelectionForDeviceChange &&
            _pendingSdrAutoFriendlyFrameRateBucket.HasValue)
        {
            selected = currentOptions.FirstOrDefault(option =>
                option.IsEnabled && IsFriendlyFrameRateMatch(option.FriendlyValue, _pendingSdrAutoFriendlyFrameRateBucket.Value));
        }

        if (selected == null &&
            sourceRate.Rate.HasValue)
        {
            selected = currentOptions
                .Where(option => option.IsEnabled)
                .OrderBy(option => Math.Abs(option.Value - sourceRate.Rate.Value))
                .ThenBy(option =>
                    sourceTimingFamilyKnown &&
                    TryInferFrameRateTimingFamily(option.Rational, option.Value, out var optionFamily) &&
                    optionFamily == sourceTimingFamily
                        ? 0
                        : 1)
                .FirstOrDefault();
        }

        selected ??= currentOptions.FirstOrDefault(option => option.IsEnabled)
            ?? currentOptions.FirstOrDefault();

        ApplyResolvedFrameRateSelection(selected, SelectedFrameRate > 0 ? SelectedFrameRate : 60);
        UpdateSelectedFormat();
        UpdateTargetSummary();
    }

    private void RebuildFrameRateOptions()
    {
        var previousRate = SelectedFrameRate;
        var options = new List<FrameRateOption>();
        var selectedResolutionKey = GetEffectiveResolutionKey(SelectedResolution);
        var timingFamily = ResolvePreferredTimingFamily(selectedResolutionKey, previousRate);
        if (_latestSourceTelemetry.HasFrameRate &&
            TryInferFrameRateTimingFamily(_latestSourceTelemetry.FrameRateArg, _latestSourceTelemetry.FrameRateExact, out var sourceFamilyHint))
        {
            timingFamily = sourceFamilyHint;
        }

        if (!string.IsNullOrWhiteSpace(selectedResolutionKey) &&
            _resolutionToFormats.TryGetValue(selectedResolutionKey, out var formats))
        {
            options = formats
                .GroupBy(format => GetFriendlyFrameRateBucket(format.FrameRateExact))
                .Select(group =>
                {
                    var allFormats = group.ToList();
                    var hdrFormats = allFormats.Where(IsHdrModeCandidate).ToList();
                    var sdrFormats = allFormats.Where(f => !IsHdrModeCandidate(f)).ToList();
                    // In HDR mode, only enable rates with HDR-capable formats.
                    // In SDR mode, enable if 8-bit formats exist. Also enable if only
                    // 10-bit formats exist for this rate (e.g., 4K HFR paths that only
                    // advertise P010) - UpdateSelectedFormat handles the fallback.
                    var enabled = IsHdrEnabled ? hdrFormats.Count > 0 : allFormats.Count > 0;
                    List<MediaFormat> selectionPool;
                    if (IsHdrEnabled && hdrFormats.Count > 0)
                        selectionPool = hdrFormats;
                    else if (!IsHdrEnabled && sdrFormats.Count > 0)
                        selectionPool = sdrFormats;
                    else
                        selectionPool = allFormats;
                    var preferred = SelectPreferredFrameRateFormat(selectionPool, group.Key, timingFamily);
                    var numerator = preferred.FrameRateNumerator > 0 ? preferred.FrameRateNumerator : (uint?)null;
                    var denominator = preferred.FrameRateDenominator > 0 ? preferred.FrameRateDenominator : (uint?)null;
                    return new FrameRateOption
                    {
                        FriendlyValue = group.Key,
                        Value = preferred.FrameRateExact,
                        Rational = preferred.FrameRateRational,
                        Numerator = numerator,
                        Denominator = denominator,
                        IsEnabled = enabled,
                        DisableReason = enabled
                            ? string.Empty
                            : "HDR mode is not supported at this frame rate."
                    };
                })
                .OrderByDescending(option => option.FriendlyValue)
                .ToList();
        }

        var sourceRate = ResolveDetectedSourceFrameRate(selectedResolutionKey, options, previousRate);
        var sourceFilter = FrameRateSourceFilterPolicy.Apply(
            options,
            sourceRate.Rate,
            sourceRate.Arg,
            BuildFrameRateTimingVariants(selectedResolutionKey),
            ShowAllCaptureOptions);
        var sourceTimingFamilyKnown = sourceFilter.SourceTimingFamilyKnown;
        var sourceTimingFamily = sourceFilter.SourceTimingFamily;
        options = sourceFilter.Options.ToList();
        var autoFrameRateOption = options.Count > 0
            ? new FrameRateOption
            {
                FriendlyValue = AutoFrameRateValue,
                Value = AutoFrameRateValue,
                IsEnabled = true,
                DisplayTextOverride = "Source"
            }
            : null;
        var availableOptions = autoFrameRateOption == null
            ? options
            : new[] { autoFrameRateOption }.Concat(options).ToList();
        DetectedSourceFrameRate = sourceRate.Rate;
        DetectedSourceFrameRateArg = sourceRate.Arg;
        SourceFrameRateOrigin = sourceRate.Origin;

        _isRebuildingModeOptions = true;
        try
        {
            AvailableFrameRates.Clear();
            foreach (var option in availableOptions)
            {
                AvailableFrameRates.Add(option);
            }

            FrameRateOption? selected = null;
            var selectAutoOption = autoFrameRateOption != null &&
                                   (IsAutoFrameRateSelected || !_hasUserOverriddenFrameRateForCurrentMode);
            if (selectAutoOption &&
                !IsHdrEnabled &&
                _pendingSdrAutoSelectionForDeviceChange &&
                _pendingSdrAutoFriendlyFrameRateBucket.HasValue)
            {
                selected = options.FirstOrDefault(option =>
                    option.IsEnabled && IsFriendlyFrameRateMatch(option.FriendlyValue, _pendingSdrAutoFriendlyFrameRateBucket.Value));
            }

            if (selected == null &&
                selectAutoOption &&
                sourceRate.Rate.HasValue)
            {
                selected = options
                    .Where(option => option.IsEnabled)
                    .OrderBy(option => Math.Abs(option.Value - sourceRate.Rate.Value))
                    .ThenBy(option =>
                        sourceTimingFamilyKnown &&
                        TryInferFrameRateTimingFamily(option.Rational, option.Value, out var optionFamily) &&
                        optionFamily == sourceTimingFamily
                            ? 0
                            : 1)
                    .FirstOrDefault();
            }

            if (selected == null)
            {
                selected = selectAutoOption
                    ? options.FirstOrDefault(option => option.IsEnabled)
                        ?? options.FirstOrDefault()
                    : options.FirstOrDefault(option =>
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

            if (autoFrameRateOption != null)
            {
                IsAutoFrameRateSelected = selectAutoOption;
            }
            var fallbackRate = previousRate > 0
                ? previousRate
                : 60;
            ApplyResolvedFrameRateSelection(selected, fallbackRate);
            if (IsHdrEnabled && selected is { IsEnabled: false })
            {
                StatusText = $"No HDR-capable frame rate is available for {GetSelectedResolutionDisplayText()}.";
            }

            if (!IsHdrEnabled && _pendingSdrAutoSelectionForDeviceChange && selected != null)
            {
                _pendingSdrAutoSelectionForDeviceChange = false;
                _pendingSdrAutoFriendlyFrameRateBucket = null;
            }
        }
        finally
        {
            _isApplyingAutomaticFrameRateSelection = false;
            _isRebuildingModeOptions = false;
        }

        RebuildVideoFormatOptions();
        UpdateSelectedFormat();
        UpdateTargetSummary();
        _forceSourceAutoRetarget = false;
    }

}

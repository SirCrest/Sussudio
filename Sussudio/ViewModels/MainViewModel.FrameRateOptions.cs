using System;
using System.Linq;

namespace Sussudio.ViewModels;

/// <summary>
/// Frame-rate selection reactions and auto-selection entry points.
/// </summary>
public partial class MainViewModel
{
    partial void OnSelectedFrameRateChanged(double value)
    {
        if (FrameRateTimingPolicy.IsAutoFrameRateValue(value))
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
            .FirstOrDefault(option => FrameRateTimingPolicy.IsFrameRateMatch(option.Value, value))
            ?? AvailableFrameRates.FirstOrDefault(option => FrameRateTimingPolicy.IsFriendlyFrameRateMatch(option.FriendlyValue, value));
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
            .Where(option => !FrameRateTimingPolicy.IsAutoFrameRateValue(option.FriendlyValue))
            .ToList();
        var selectedResolutionKey = GetEffectiveResolutionKey(SelectedResolution);
        var sourceRate = ResolveDetectedSourceFrameRate(selectedResolutionKey, currentOptions, SelectedFrameRate);
        var sourceTimingFamilyKnown = FrameRateTimingPolicy.TryInferFrameRateTimingFamily(sourceRate.Arg, sourceRate.Rate, out var sourceTimingFamily);
        var selection = FrameRateAutoSelectionPolicy.Select(new FrameRateAutoSelectionRequest(
            currentOptions,
            AutoFrameRateOptionAvailable: false,
            ForceAutoSelection: true,
            IsAutoFrameRateSelected: IsAutoFrameRateSelected,
            HasUserOverriddenFrameRateForCurrentMode: _hasUserOverriddenFrameRateForCurrentMode,
            IsHdrEnabled: IsHdrEnabled,
            PendingSdrAutoSelectionForDeviceChange: _pendingSdrAutoSelectionForDeviceChange,
            PendingSdrAutoFriendlyFrameRateBucket: _pendingSdrAutoFriendlyFrameRateBucket,
            Source: new FrameRateAutoSelectionSource(sourceRate.Rate, sourceTimingFamilyKnown, sourceTimingFamily),
            PreviousRate: SelectedFrameRate));

        ApplyResolvedFrameRateSelection(selection.Selected, SelectedFrameRate > 0 ? SelectedFrameRate : 60);
        UpdateSelectedFormat();
        UpdateTargetSummary();
    }

}

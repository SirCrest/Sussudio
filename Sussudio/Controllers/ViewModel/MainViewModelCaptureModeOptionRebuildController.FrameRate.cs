using System.Collections.Generic;
using System.Linq;
using Sussudio.Models;

namespace Sussudio.ViewModels;

public partial class MainViewModel
{
    private sealed partial class MainViewModelCaptureModeOptionRebuildController
    {
        public void RebuildFrameRateOptions()
        {
            var previousRate = _viewModel.SelectedFrameRate;
            var options = new List<FrameRateOption>();
            var selectedResolutionKey = _viewModel.GetEffectiveResolutionKey(_viewModel.SelectedResolution);
            var timingFamily = _viewModel.ResolvePreferredTimingFamily(selectedResolutionKey, previousRate);
            if (_viewModel._latestSourceTelemetry.HasFrameRate &&
                FrameRateTimingPolicy.TryInferFrameRateTimingFamily(_viewModel._latestSourceTelemetry.FrameRateArg, _viewModel._latestSourceTelemetry.FrameRateExact, out var sourceFamilyHint))
            {
                timingFamily = sourceFamilyHint;
            }

            if (!string.IsNullOrWhiteSpace(selectedResolutionKey) &&
                _viewModel._resolutionToFormats.TryGetValue(selectedResolutionKey, out var formats))
            {
                options = formats
                    .GroupBy(format => FrameRateTimingPolicy.GetFriendlyFrameRateBucket(format.FrameRateExact))
                    .Select(group =>
                    {
                        var allFormats = group.ToList();
                        var hdrFormats = allFormats.Where(CaptureModeOptionsBuilder.IsHdrModeCandidate).ToList();
                        var sdrFormats = allFormats.Where(f => !CaptureModeOptionsBuilder.IsHdrModeCandidate(f)).ToList();
                        // In HDR mode, only enable rates with HDR-capable formats.
                        // In SDR mode, enable if 8-bit formats exist. Also enable if only
                        // 10-bit formats exist for this rate (e.g., 4K HFR paths that only
                        // advertise P010) - UpdateSelectedFormat handles the fallback.
                        var enabled = _viewModel.IsHdrEnabled ? hdrFormats.Count > 0 : allFormats.Count > 0;
                        List<MediaFormat> selectionPool;
                        if (_viewModel.IsHdrEnabled && hdrFormats.Count > 0)
                            selectionPool = hdrFormats;
                        else if (!_viewModel.IsHdrEnabled && sdrFormats.Count > 0)
                            selectionPool = sdrFormats;
                        else
                            selectionPool = allFormats;
                        var preferred = FrameRateTimingPolicy.SelectPreferredFrameRateFormat(selectionPool, group.Key, timingFamily);
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

            var sourceRate = _viewModel.ResolveDetectedSourceFrameRate(selectedResolutionKey, options, previousRate);
            var sourceFilter = FrameRateSourceFilterPolicy.Apply(
                options,
                sourceRate.Rate,
                sourceRate.Arg,
                _viewModel.BuildFrameRateTimingVariants(selectedResolutionKey),
                _viewModel.ShowAllCaptureOptions);
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
            _viewModel.DetectedSourceFrameRate = sourceRate.Rate;
            _viewModel.DetectedSourceFrameRateArg = sourceRate.Arg;
            _viewModel.SourceFrameRateOrigin = sourceRate.Origin;

            _viewModel._isRebuildingModeOptions = true;
            try
            {
                _viewModel.AvailableFrameRates.Clear();
                foreach (var option in availableOptions)
                {
                    _viewModel.AvailableFrameRates.Add(option);
                }

                var selection = FrameRateAutoSelectionPolicy.Select(new FrameRateAutoSelectionRequest(
                    options,
                    AutoFrameRateOptionAvailable: autoFrameRateOption != null,
                    ForceAutoSelection: false,
                    IsAutoFrameRateSelected: _viewModel.IsAutoFrameRateSelected,
                    HasUserOverriddenFrameRateForCurrentMode: _viewModel._hasUserOverriddenFrameRateForCurrentMode,
                    IsHdrEnabled: _viewModel.IsHdrEnabled,
                    PendingSdrAutoSelectionForDeviceChange: _viewModel._pendingSdrAutoSelectionForDeviceChange,
                    PendingSdrAutoFriendlyFrameRateBucket: _viewModel._pendingSdrAutoFriendlyFrameRateBucket,
                    Source: new FrameRateAutoSelectionSource(sourceRate.Rate, sourceTimingFamilyKnown, sourceTimingFamily),
                    PreviousRate: previousRate));

                if (autoFrameRateOption != null)
                {
                    _viewModel.IsAutoFrameRateSelected = selection.SelectAutoOption;
                }
                var fallbackRate = previousRate > 0
                    ? previousRate
                    : 60;
                _viewModel.ApplyResolvedFrameRateSelection(selection.Selected, fallbackRate);
                if (_viewModel.IsHdrEnabled && selection.Selected is { IsEnabled: false })
                {
                    _viewModel.StatusText = $"No HDR-capable frame rate is available for {_viewModel.GetSelectedResolutionDisplayText()}.";
                }

                if (!_viewModel.IsHdrEnabled && _viewModel._pendingSdrAutoSelectionForDeviceChange && selection.Selected != null)
                {
                    _viewModel._pendingSdrAutoSelectionForDeviceChange = false;
                    _viewModel._pendingSdrAutoFriendlyFrameRateBucket = null;
                }
            }
            finally
            {
                _viewModel._isApplyingAutomaticFrameRateSelection = false;
                _viewModel._isRebuildingModeOptions = false;
            }

            RebuildVideoFormatOptions();
            UpdateSelectedFormat();
            _viewModel.UpdateTargetSummary();
            _viewModel._forceSourceAutoRetarget = false;
        }
    }
}

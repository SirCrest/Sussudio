using System;
using System.Collections.Generic;
using System.Linq;
using Sussudio.Models;

namespace Sussudio.ViewModels;

public partial class MainViewModel
{
    /// <summary>
    /// Owns capture-mode option rebuild transactions for the
    /// compatibility ViewModel facade.
    /// </summary>
    private sealed class MainViewModelCaptureModeOptionRebuildController
    {
        private readonly MainViewModel _viewModel;

        public MainViewModelCaptureModeOptionRebuildController(MainViewModel viewModel)
        {
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        }

        public void RebuildResolutionOptions()
        {
            var previousSelection = _viewModel.SelectedResolution;
            var previousRate = _viewModel.SelectedFrameRate;
            var desiredSelection = !string.IsNullOrWhiteSpace(previousSelection)
                ? previousSelection
                : _viewModel._lastKnownResolutionKey;
            var options = CaptureModeOptionsBuilder.BuildResolutionOptions(
                    _viewModel._resolutionToFormats,
                    _viewModel.IsHdrEnabled,
                    _viewModel.ShowAllCaptureOptions,
                    _viewModel._latestSourceTelemetry)
                .ToList();

            var autoSelection = ResolveAutoCaptureSelection(options);
            var autoOption = options.Count > 0
                ? CreateAutoResolutionOption()
                : null;

            if (options.Count == 0)
            {
                if (_viewModel.SelectedDevice != null && _viewModel.IsPreviewing && _viewModel.AvailableResolutions.Count > 0)
                {
                    var retainedSelection = _viewModel.AvailableResolutions.FirstOrDefault(option =>
                            string.Equals(option.Value, _viewModel.SelectedResolution, StringComparison.OrdinalIgnoreCase))
                        ?? _viewModel.AvailableResolutions.FirstOrDefault(option => option.IsEnabled)
                        ?? _viewModel.AvailableResolutions.FirstOrDefault();
                    if (retainedSelection != null)
                    {
                        _viewModel._isRebuildingModeOptions = true;
                        _viewModel._isApplyingAutomaticResolutionSelection = true;
                        try
                        {
                            var previousSelectedResolution = _viewModel.SelectedResolution;
                            _viewModel.SelectedResolution = retainedSelection.Value;
                            if (string.Equals(previousSelectedResolution, retainedSelection.Value, StringComparison.OrdinalIgnoreCase))
                            {
                                _viewModel.OnPropertyChanged(nameof(_viewModel.SelectedResolution));
                            }

                            if (_viewModel.TryResolveResolutionKey(retainedSelection.Value, out var retainedResolutionKey))
                            {
                                _viewModel._lastKnownResolutionKey = retainedResolutionKey;
                            }
                        }
                        finally
                        {
                            _viewModel._isApplyingAutomaticResolutionSelection = false;
                            _viewModel._isRebuildingModeOptions = false;
                        }
                    }

                    RebuildFrameRateOptions();
                    _viewModel.UpdateTargetSummary();
                    return;
                }

                _viewModel._isRebuildingModeOptions = true;
                try
                {
                    _viewModel.AvailableResolutions.Clear();
                    _viewModel._isApplyingAutomaticResolutionSelection = true;
                    _viewModel.SelectedResolution = null;
                    _viewModel._isApplyingAutomaticResolutionSelection = false;
                    ClearAutoResolutionState();
                    _viewModel.HdrResolutionSupportHint = string.Empty;
                    _viewModel.DisabledResolutionReason = string.Empty;
                }
                finally
                {
                    _viewModel._isApplyingAutomaticResolutionSelection = false;
                    _viewModel._isRebuildingModeOptions = false;
                }

                RebuildFrameRateOptions();
                _viewModel.UpdateTargetSummary();
                return;
            }

            var allowSourceAutoSelect = _viewModel.IsHdrEnabled && (_viewModel._forceSourceAutoRetarget || !_viewModel._hasUserOverriddenResolutionForCurrentMode);
            var selection = CaptureResolutionSelectionPolicy.Select(new CaptureResolutionSelectionRequest(
                options,
                _viewModel._resolutionToFormats,
                _viewModel._latestSourceTelemetry,
                desiredSelection,
                previousRate,
                _viewModel.IsHdrEnabled,
                allowSourceAutoSelect,
                _viewModel._pendingSdrAutoSelectionForDeviceChange));
            var selected = selection.Selected;
            var hdrHint = selection.HdrHint;
            if (!_viewModel.IsHdrEnabled && selection.SdrAutoFriendlyFrameRateBucket.HasValue)
            {
                _viewModel._pendingSdrAutoFriendlyFrameRateBucket = selection.SdrAutoFriendlyFrameRateBucket.Value;
            }

            var selectAutoOption = autoOption != null && ShouldSelectAutoResolutionOption(previousSelection);
            var selectedDropdownOption = selectAutoOption
                ? autoOption
                : selected;
            var availableOptions = autoOption == null
                ? options
                : new[] { autoOption }.Concat(options).ToList();

            _viewModel._isRebuildingModeOptions = true;
            try
            {
                UpdateAutoResolutionState(autoSelection);
                _viewModel.AvailableResolutions.Clear();
                foreach (var option in availableOptions)
                {
                    _viewModel.AvailableResolutions.Add(option);
                }

                _viewModel._isApplyingAutomaticResolutionSelection = true;
                if (selectedDropdownOption != null)
                {
                    var previousSelectedResolution = _viewModel.SelectedResolution;
                    _viewModel.SelectedResolution = selectedDropdownOption.Value;
                    if (string.Equals(previousSelectedResolution, selectedDropdownOption.Value, StringComparison.OrdinalIgnoreCase))
                    {
                        _viewModel.OnPropertyChanged(nameof(_viewModel.SelectedResolution));
                    }
                }

                _viewModel._isApplyingAutomaticResolutionSelection = false;
                if (selected != null)
                {
                    _viewModel._lastKnownResolutionKey = selected.Value;
                }

                if (_viewModel.IsHdrEnabled)
                {
                    _viewModel.HdrResolutionSupportHint = hdrHint ?? _viewModel.BuildHdrSupportHintForResolution(selected?.Value);
                }
                else
                {
                    _viewModel.HdrResolutionSupportHint = string.Empty;
                }

                if (_viewModel.IsHdrEnabled && selected is { IsEnabled: false })
                {
                    _viewModel.StatusText = "No HDR-capable resolution is available for this device.";
                }

                _viewModel.DisabledResolutionReason = selected is { IsEnabled: false }
                    ? selected.DisableReason
                    : string.Empty;
            }
            finally
            {
                _viewModel._isApplyingAutomaticResolutionSelection = false;
                _viewModel._isRebuildingModeOptions = false;
            }

            RebuildFrameRateOptions();
        }

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

        public void UpdateSelectedFormat()
        {
            if (!_viewModel.TryGetEffectiveResolutionSelection(out var resolutionKey, out var width, out var height))
            {
                _viewModel.SelectedFormat = null;
                return;
            }

            _viewModel.SelectedFormat = CaptureFormatSelectionPolicy.Select(
                BuildCaptureFormatSelectionRequest(resolutionKey, width, height));
        }

        public void RebuildVideoFormatOptions()
        {
            // Source-reader pixel formats are not global device capabilities. A card can expose
            // MJPG at 4K120 SDR while exposing only P010 at the HDR retarget mode, so keep this
            // list scoped to the currently selected resolution+fps tuple.
            var formats = GetFormatsForSelectedModeTuple();
            var nextFormats = CaptureModeOptionsBuilder.BuildVideoFormatOptions(formats);

            _viewModel.AvailableVideoFormats.Clear();
            foreach (var format in nextFormats)
            {
                _viewModel.AvailableVideoFormats.Add(format);
            }

            if (!_viewModel.AvailableVideoFormats.Any(format => string.Equals(format, _viewModel.SelectedVideoFormat, StringComparison.OrdinalIgnoreCase)))
            {
                var previousSuppress = _viewModel._suppressFormatChangeReinitialize;
                _viewModel._suppressFormatChangeReinitialize = true;
                try
                {
                    _viewModel.SelectedVideoFormat = "Auto";
                }
                finally
                {
                    _viewModel._suppressFormatChangeReinitialize = previousSuppress;
                }
            }
        }

        private List<MediaFormat> GetFormatsForSelectedModeTuple()
        {
            if (!_viewModel.TryGetEffectiveResolutionSelection(out var resolutionKey, out var width, out var height))
            {
                return new List<MediaFormat>();
            }

            return CaptureFormatSelectionPolicy
                .SelectModeTupleFormats(BuildCaptureFormatSelectionRequest(resolutionKey, width, height))
                .ToList();
        }

        private CaptureFormatSelectionRequest BuildCaptureFormatSelectionRequest(
            string resolutionKey,
            uint width,
            uint height)
            => new(
                _viewModel.AvailableFormats,
                _viewModel.AvailableFrameRates,
                width,
                height,
                _viewModel.SelectedFrameRate,
                _viewModel.SelectedVideoFormat,
                _viewModel.IsHdrEnabled,
                _viewModel.ResolvePreferredTimingFamily(resolutionKey, _viewModel.SelectedFrameRate));

        private AutoCaptureSelection? ResolveAutoCaptureSelection(IReadOnlyList<ResolutionOption> options)
            => AutoCaptureSelectionPolicy.Select(new AutoCaptureSelectionRequest(
                options,
                _viewModel._resolutionToFormats,
                _viewModel._latestSourceTelemetry,
                _viewModel.IsHdrEnabled));

        private bool ShouldSelectAutoResolutionOption(string? previousSelection)
            => IsAutoResolutionValue(previousSelection) ||
               string.IsNullOrWhiteSpace(previousSelection) ||
               !_viewModel._hasUserOverriddenResolutionForCurrentMode;

        private ResolutionOption CreateAutoResolutionOption()
            => new()
            {
                Value = AutoResolutionValue,
                Width = 0,
                Height = 0,
                IsEnabled = true,
                DisplayTextOverride = BuildAutoResolutionDisplayText()
            };

        private static string BuildAutoResolutionDisplayText()
            => AutoResolutionValue;

        private void UpdateAutoResolutionState(AutoCaptureSelection? selection)
        {
            _viewModel.AutoResolvedWidth = selection?.Resolution.Width;
            _viewModel.AutoResolvedHeight = selection?.Resolution.Height;
            _viewModel.AutoResolvedFrameRate = selection?.ExactFrameRate;
        }

        private void ClearAutoResolutionState()
        {
            _viewModel.AutoResolvedWidth = null;
            _viewModel.AutoResolvedHeight = null;
            _viewModel.AutoResolvedFrameRate = null;
        }
    }
}

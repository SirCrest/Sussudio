using System;
using System.Collections.Generic;
using System.Linq;
using Sussudio.Models;

namespace Sussudio.ViewModels;

public partial class MainViewModel
{
    /// <summary>
    /// Owns resolution option rebuild transactions and auto-resolution state for
    /// the compatibility ViewModel facade.
    /// </summary>
    private sealed class MainViewModelResolutionOptionRebuildController
    {
        private readonly MainViewModel _viewModel;

        public MainViewModelResolutionOptionRebuildController(MainViewModel viewModel)
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

                    RebuildDependentOptions();
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

                RebuildDependentOptions();
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

            RebuildDependentOptions();
        }

        private void RebuildDependentOptions()
            => _viewModel._captureModeOptionRebuildController.RebuildFrameRateOptions();

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

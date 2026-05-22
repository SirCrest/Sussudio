using System;
using System.Collections.Generic;
using System.Linq;
using Sussudio.Models;
using Sussudio.ViewModels;

namespace Sussudio.Controllers;

internal sealed partial class MainViewModelCaptureModeOptionRebuildController
{
    public void RebuildResolutionOptions()
    {
        var previousSelection = _context.GetSelectedResolution();
        var previousRate = _context.GetSelectedFrameRate();
        var desiredSelection = !string.IsNullOrWhiteSpace(previousSelection)
            ? previousSelection
            : _context.GetLastKnownResolutionKey();
        var options = CaptureModeOptionsBuilder.BuildResolutionOptions(
                _context.GetResolutionToFormats(),
                _context.IsHdrEnabled(),
                true,
                _context.GetLatestSourceTelemetry())
            .ToList();

        var autoSelection = ResolveAutoCaptureSelection(options);
        var autoOption = options.Count > 0
            ? CreateAutoResolutionOption()
            : null;

        if (options.Count == 0)
        {
            if (_context.GetSelectedDevice() != null && _context.IsPreviewing() && _context.AvailableResolutions.Count > 0)
            {
                var retainedSelection = _context.AvailableResolutions.FirstOrDefault(option =>
                        string.Equals(option.Value, _context.GetSelectedResolution(), StringComparison.OrdinalIgnoreCase))
                    ?? _context.AvailableResolutions.FirstOrDefault(option => option.IsEnabled)
                    ?? _context.AvailableResolutions.FirstOrDefault();
                if (retainedSelection != null)
                {
                    _context.SetIsRebuildingModeOptions(true);
                    _context.SetIsApplyingAutomaticResolutionSelection(true);
                    try
                    {
                        var previousSelectedResolution = _context.GetSelectedResolution();
                        _context.SetSelectedResolution(retainedSelection.Value);
                        if (string.Equals(previousSelectedResolution, retainedSelection.Value, StringComparison.OrdinalIgnoreCase))
                        {
                            _context.NotifySelectedResolutionChanged();
                        }

                        if (_context.TryResolveResolutionKey(retainedSelection.Value, out var retainedResolutionKey))
                        {
                            _context.SetLastKnownResolutionKey(retainedResolutionKey);
                        }
                    }
                    finally
                    {
                        _context.SetIsApplyingAutomaticResolutionSelection(false);
                        _context.SetIsRebuildingModeOptions(false);
                    }
                }

                RebuildDependentOptions();
                _context.UpdateTargetSummary();
                return;
            }

            _context.SetIsRebuildingModeOptions(true);
            try
            {
                _context.AvailableResolutions.Clear();
                _context.SetIsApplyingAutomaticResolutionSelection(true);
                _context.SetSelectedResolution(null);
                _context.SetIsApplyingAutomaticResolutionSelection(false);
                ClearAutoResolutionState();
                _context.SetHdrResolutionSupportHint(string.Empty);
                _context.SetDisabledResolutionReason(string.Empty);
            }
            finally
            {
                _context.SetIsApplyingAutomaticResolutionSelection(false);
                _context.SetIsRebuildingModeOptions(false);
            }

            RebuildDependentOptions();
            _context.UpdateTargetSummary();
            return;
        }

        var allowSourceAutoSelect = _context.IsHdrEnabled() && (_context.IsForceSourceAutoRetarget() || !_context.HasUserOverriddenResolutionForCurrentMode());
        var selection = CaptureResolutionSelectionPolicy.Select(new CaptureResolutionSelectionRequest(
            options,
            _context.GetResolutionToFormats(),
            _context.GetLatestSourceTelemetry(),
            desiredSelection,
            previousRate,
            _context.IsHdrEnabled(),
            allowSourceAutoSelect,
            _context.IsPendingSdrAutoSelectionForDeviceChange()));
        var selected = selection.Selected;
        var hdrHint = selection.HdrHint;
        if (!_context.IsHdrEnabled() && selection.SdrAutoFriendlyFrameRateBucket.HasValue)
        {
            _context.SetPendingSdrAutoFriendlyFrameRateBucket(selection.SdrAutoFriendlyFrameRateBucket.Value);
        }

        var selectAutoOption = autoOption != null && ShouldSelectAutoResolutionOption(previousSelection);
        var selectedDropdownOption = selectAutoOption
            ? autoOption
            : selected;
        var availableOptions = autoOption == null
            ? options
            : new[] { autoOption }.Concat(options).ToList();

        _context.SetIsRebuildingModeOptions(true);
        try
        {
            UpdateAutoResolutionState(autoSelection);
            _context.AvailableResolutions.Clear();
            foreach (var option in availableOptions)
            {
                _context.AvailableResolutions.Add(option);
            }

            _context.SetIsApplyingAutomaticResolutionSelection(true);
            if (selectedDropdownOption != null)
            {
                var previousSelectedResolution = _context.GetSelectedResolution();
                _context.SetSelectedResolution(selectedDropdownOption.Value);
                if (string.Equals(previousSelectedResolution, selectedDropdownOption.Value, StringComparison.OrdinalIgnoreCase))
                {
                    _context.NotifySelectedResolutionChanged();
                }
            }

            _context.SetIsApplyingAutomaticResolutionSelection(false);
            if (selected != null)
            {
                _context.SetLastKnownResolutionKey(selected.Value);
            }

            if (_context.IsHdrEnabled())
            {
                _context.SetHdrResolutionSupportHint(hdrHint ?? _context.BuildHdrSupportHintForResolution(selected?.Value));
            }
            else
            {
                _context.SetHdrResolutionSupportHint(string.Empty);
            }

            if (_context.IsHdrEnabled() && selected is { IsEnabled: false })
            {
                _context.SetStatusText("No HDR-capable resolution is available for this device.");
            }

            _context.SetDisabledResolutionReason(selected is { IsEnabled: false }
                ? selected.DisableReason
                : string.Empty);
        }
        finally
        {
            _context.SetIsApplyingAutomaticResolutionSelection(false);
            _context.SetIsRebuildingModeOptions(false);
        }

        RebuildDependentOptions();
    }

    private void RebuildDependentOptions()
        => RebuildFrameRateOptions();

    private AutoCaptureSelection? ResolveAutoCaptureSelection(IReadOnlyList<ResolutionOption> options)
        => AutoCaptureSelectionPolicy.Select(new AutoCaptureSelectionRequest(
            options,
            _context.GetResolutionToFormats(),
            _context.GetLatestSourceTelemetry(),
            _context.IsHdrEnabled()));

    private bool ShouldSelectAutoResolutionOption(string? previousSelection)
        => string.Equals(previousSelection, _context.AutoResolutionValue, StringComparison.OrdinalIgnoreCase) ||
           string.IsNullOrWhiteSpace(previousSelection) ||
           !_context.HasUserOverriddenResolutionForCurrentMode();

    private ResolutionOption CreateAutoResolutionOption()
        => new()
        {
            Value = _context.AutoResolutionValue,
            Width = 0,
            Height = 0,
            IsEnabled = true,
            DisplayTextOverride = BuildAutoResolutionDisplayText()
        };

    private string BuildAutoResolutionDisplayText()
        => _context.AutoResolutionValue;

    private void UpdateAutoResolutionState(AutoCaptureSelection? selection)
    {
        _context.SetAutoResolvedWidth(selection?.Resolution.Width);
        _context.SetAutoResolvedHeight(selection?.Resolution.Height);
        _context.SetAutoResolvedFrameRate(selection?.ExactFrameRate);
    }

    private void ClearAutoResolutionState()
    {
        _context.SetAutoResolvedWidth(null);
        _context.SetAutoResolvedHeight(null);
        _context.SetAutoResolvedFrameRate(null);
    }
}

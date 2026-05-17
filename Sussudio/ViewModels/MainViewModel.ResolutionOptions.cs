using System;
using System.Collections.Generic;
using System.Linq;
using Sussudio.Models;

namespace Sussudio.ViewModels;

/// <summary>
/// Resolution option building and dropdown mutation.
/// </summary>
public partial class MainViewModel
{
    private AutoCaptureSelection? ResolveAutoCaptureSelection(IReadOnlyList<ResolutionOption> options)
        => AutoCaptureSelectionPolicy.Select(new AutoCaptureSelectionRequest(
            options,
            _resolutionToFormats,
            _latestSourceTelemetry,
            IsHdrEnabled));

    private bool ShouldSelectAutoResolutionOption(string? previousSelection)
        => IsAutoResolutionValue(previousSelection) ||
           string.IsNullOrWhiteSpace(previousSelection) ||
           !_hasUserOverriddenResolutionForCurrentMode;

    private ResolutionOption CreateAutoResolutionOption()
        => new()
        {
            Value = AutoResolutionValue,
            Width = 0,
            Height = 0,
            IsEnabled = true,
            DisplayTextOverride = BuildAutoResolutionDisplayText()
        };

    private string BuildAutoResolutionDisplayText()
        => AutoResolutionValue;

    private void RebuildResolutionOptions()
    {
        var previousSelection = SelectedResolution;
        var previousRate = SelectedFrameRate;
        var desiredSelection = !string.IsNullOrWhiteSpace(previousSelection)
            ? previousSelection
            : _lastKnownResolutionKey;
        var options = CaptureModeOptionsBuilder.BuildResolutionOptions(
                _resolutionToFormats,
                IsHdrEnabled,
                ShowAllCaptureOptions,
                _latestSourceTelemetry)
            .ToList();

        var autoSelection = ResolveAutoCaptureSelection(options);
        var autoOption = options.Count > 0
            ? CreateAutoResolutionOption()
            : null;

        if (options.Count == 0)
        {
            if (SelectedDevice != null && IsPreviewing && AvailableResolutions.Count > 0)
            {
                var retainedSelection = AvailableResolutions.FirstOrDefault(option =>
                        string.Equals(option.Value, SelectedResolution, StringComparison.OrdinalIgnoreCase))
                    ?? AvailableResolutions.FirstOrDefault(option => option.IsEnabled)
                    ?? AvailableResolutions.FirstOrDefault();
                if (retainedSelection != null)
                {
                    _isRebuildingModeOptions = true;
                    _isApplyingAutomaticResolutionSelection = true;
                    try
                    {
                        var previousSelectedResolution = SelectedResolution;
                        SelectedResolution = retainedSelection.Value;
                        if (string.Equals(previousSelectedResolution, retainedSelection.Value, StringComparison.OrdinalIgnoreCase))
                        {
                            OnPropertyChanged(nameof(SelectedResolution));
                        }

                        if (TryResolveResolutionKey(retainedSelection.Value, out var retainedResolutionKey))
                        {
                            _lastKnownResolutionKey = retainedResolutionKey;
                        }
                    }
                    finally
                    {
                        _isApplyingAutomaticResolutionSelection = false;
                        _isRebuildingModeOptions = false;
                    }
                }

                RebuildFrameRateOptions();
                UpdateTargetSummary();
                return;
            }

            _isRebuildingModeOptions = true;
            try
            {
                AvailableResolutions.Clear();
                _isApplyingAutomaticResolutionSelection = true;
                SelectedResolution = null;
                _isApplyingAutomaticResolutionSelection = false;
                ClearAutoResolutionState();
                HdrResolutionSupportHint = string.Empty;
                DisabledResolutionReason = string.Empty;
            }
            finally
            {
                _isApplyingAutomaticResolutionSelection = false;
                _isRebuildingModeOptions = false;
            }

            RebuildFrameRateOptions();
            UpdateTargetSummary();
            return;
        }

        var allowSourceAutoSelect = IsHdrEnabled && (_forceSourceAutoRetarget || !_hasUserOverriddenResolutionForCurrentMode);
        var selection = CaptureResolutionSelectionPolicy.Select(new CaptureResolutionSelectionRequest(
            options,
            _resolutionToFormats,
            _latestSourceTelemetry,
            desiredSelection,
            previousRate,
            IsHdrEnabled,
            allowSourceAutoSelect,
            _pendingSdrAutoSelectionForDeviceChange));
        var selected = selection.Selected;
        var hdrHint = selection.HdrHint;
        if (!IsHdrEnabled && selection.SdrAutoFriendlyFrameRateBucket.HasValue)
        {
            _pendingSdrAutoFriendlyFrameRateBucket = selection.SdrAutoFriendlyFrameRateBucket.Value;
        }

        var selectAutoOption = autoOption != null && ShouldSelectAutoResolutionOption(previousSelection);
        var selectedDropdownOption = selectAutoOption
            ? autoOption
            : selected;
        var availableOptions = autoOption == null
            ? options
            : new[] { autoOption }.Concat(options).ToList();

        _isRebuildingModeOptions = true;
        try
        {
            UpdateAutoResolutionState(autoSelection);
            AvailableResolutions.Clear();
            foreach (var option in availableOptions)
            {
                AvailableResolutions.Add(option);
            }

            _isApplyingAutomaticResolutionSelection = true;
            if (selectedDropdownOption != null)
            {
                var previousSelectedResolution = SelectedResolution;
                SelectedResolution = selectedDropdownOption.Value;
                if (string.Equals(previousSelectedResolution, selectedDropdownOption.Value, StringComparison.OrdinalIgnoreCase))
                {
                    OnPropertyChanged(nameof(SelectedResolution));
                }
            }

            _isApplyingAutomaticResolutionSelection = false;
            if (selected != null)
            {
                _lastKnownResolutionKey = selected.Value;
            }

            if (IsHdrEnabled)
            {
                HdrResolutionSupportHint = hdrHint ?? BuildHdrSupportHintForResolution(selected?.Value);
            }
            else
            {
                HdrResolutionSupportHint = string.Empty;
            }

            if (IsHdrEnabled && selected is { IsEnabled: false })
            {
                StatusText = "No HDR-capable resolution is available for this device.";
            }

            DisabledResolutionReason = selected is { IsEnabled: false }
                ? selected.DisableReason
                : string.Empty;
        }
        finally
        {
            _isApplyingAutomaticResolutionSelection = false;
            _isRebuildingModeOptions = false;
        }

        RebuildFrameRateOptions();
    }

    private void UpdateAutoResolutionState(AutoCaptureSelection? selection)
    {
        AutoResolvedWidth = selection?.Resolution.Width;
        AutoResolvedHeight = selection?.Resolution.Height;
        AutoResolvedFrameRate = selection?.ExactFrameRate;
    }

    private void ClearAutoResolutionState()
    {
        AutoResolvedWidth = null;
        AutoResolvedHeight = null;
        AutoResolvedFrameRate = null;
    }

    private static bool IsAutoResolutionValue(string? resolutionValue)
        => string.Equals(resolutionValue, AutoResolutionValue, StringComparison.OrdinalIgnoreCase);

    private bool TryResolveResolutionKey(string? resolutionValue, out string resolutionKey)
    {
        resolutionKey = string.Empty;
        if (string.IsNullOrWhiteSpace(resolutionValue))
        {
            return false;
        }

        if (IsAutoResolutionValue(resolutionValue))
        {
            if (AutoResolvedWidth.HasValue &&
                AutoResolvedHeight.HasValue &&
                AutoResolvedWidth.Value > 0 &&
                AutoResolvedHeight.Value > 0)
            {
                resolutionKey = GetResolutionKey(AutoResolvedWidth.Value, AutoResolvedHeight.Value);
                return true;
            }

            return false;
        }

        if (!TryParseResolutionKey(resolutionValue, out var width, out var height))
        {
            return false;
        }

        resolutionKey = GetResolutionKey(width, height);
        return true;
    }

    private string? GetEffectiveResolutionKey(string? resolutionValue)
        => TryResolveResolutionKey(resolutionValue, out var resolutionKey)
            ? resolutionKey
            : null;

    private bool TryGetEffectiveResolutionSelection(out string resolutionKey, out uint width, out uint height)
    {
        resolutionKey = string.Empty;
        width = 0;
        height = 0;

        if (!TryResolveResolutionKey(SelectedResolution, out resolutionKey) ||
            !TryParseResolutionKey(resolutionKey, out width, out height))
        {
            resolutionKey = string.Empty;
            width = 0;
            height = 0;
            return false;
        }

        return true;
    }

    private static bool TryParseResolutionKey(string? resolutionKey, out uint width, out uint height)
        => CaptureResolutionSelectionPolicy.TryParseResolutionKey(resolutionKey, out width, out height);

    private bool ResolutionSupportsFrameRate(string resolutionKey, double frameRate, bool hdrOnly)
        => CaptureResolutionSelectionPolicy.ResolutionSupportsFrameRate(
            _resolutionToFormats,
            resolutionKey,
            frameRate,
            hdrOnly);

    private bool ResolutionSupportsFriendlyFrameRate(
        string resolutionKey,
        int friendlyBucket,
        bool hdrOnly,
        bool sdrOnly)
        => CaptureResolutionSelectionPolicy.ResolutionSupportsFriendlyFrameRate(
            _resolutionToFormats,
            resolutionKey,
            friendlyBucket,
            hdrOnly,
            sdrOnly);

    private string BuildHdrSupportHintForResolution(string? resolutionKey)
        => CaptureResolutionSelectionPolicy.BuildHdrSupportHint(new HdrSupportHintRequest(
            _resolutionToFormats,
            resolutionKey,
            IsHdrEnabled,
            SelectedFrameRate));
}

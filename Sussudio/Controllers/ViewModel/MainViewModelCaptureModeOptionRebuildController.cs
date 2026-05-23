using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;
using Sussudio.Models;
using Sussudio.ViewModels;

namespace Sussudio.Controllers;

/// <summary>
/// Graph-built ports consumed by the capture-mode option rebuild controller.
/// </summary>
internal sealed class MainViewModelCaptureModeOptionRebuildControllerContext
{
    public required ObservableCollection<MediaFormat> AvailableFormats { get; init; }
    public required ObservableCollection<FrameRateOption> AvailableFrameRates { get; init; }
    public required ObservableCollection<ResolutionOption> AvailableResolutions { get; init; }
    public required ObservableCollection<string> AvailableVideoFormats { get; init; }
    public required string AutoResolutionValue { get; init; }
    public required double AutoFrameRateValue { get; init; }
    public required Func<IReadOnlyDictionary<string, List<MediaFormat>>> GetResolutionToFormats { get; init; }
    public required Func<SourceSignalTelemetrySnapshot> GetLatestSourceTelemetry { get; init; }
    public required TryGetEffectiveResolutionSelectionDelegate TryGetEffectiveResolutionSelection { get; init; }
    public required TryResolveResolutionKeyDelegate TryResolveResolutionKey { get; init; }
    public required Func<string?, string?> GetEffectiveResolutionKey { get; init; }
    public required Action<FrameRateOption?, double> ApplyResolvedFrameRateSelection { get; init; }
    public required Func<string> GetSelectedResolutionDisplayText { get; init; }
    public required Func<string?, string> BuildHdrSupportHintForResolution { get; init; }
    public required Action UpdateTargetSummary { get; init; }
    public required Action NotifySelectedResolutionChanged { get; init; }
    public required Func<CaptureDevice?> GetSelectedDevice { get; init; }
    public required Func<string?> GetSelectedResolution { get; init; }
    public required Action<string?> SetSelectedResolution { get; init; }
    public required Func<double> GetSelectedFrameRate { get; init; }
    public required Func<string> GetSelectedVideoFormat { get; init; }
    public required Action<string> SetSelectedVideoFormat { get; init; }
    public required Action<MediaFormat?> SetSelectedFormat { get; init; }
    public required Func<bool> IsHdrEnabled { get; init; }
    public required Func<bool> IsPreviewing { get; init; }
    public required Func<bool> IsAutoFrameRateSelected { get; init; }
    public required Action<bool> SetIsAutoFrameRateSelected { get; init; }
    public required Func<bool> HasUserOverriddenResolutionForCurrentMode { get; init; }
    public required Func<bool> HasUserOverriddenFrameRateForCurrentMode { get; init; }
    public required Func<bool> IsPendingSdrAutoSelectionForDeviceChange { get; init; }
    public required Action<bool> SetPendingSdrAutoSelectionForDeviceChange { get; init; }
    public required Func<int?> GetPendingSdrAutoFriendlyFrameRateBucket { get; init; }
    public required Action<int?> SetPendingSdrAutoFriendlyFrameRateBucket { get; init; }
    public required Func<bool> IsForceSourceAutoRetarget { get; init; }
    public required Action<bool> SetForceSourceAutoRetarget { get; init; }
    public required Func<string?> GetLastKnownResolutionKey { get; init; }
    public required Action<string?> SetLastKnownResolutionKey { get; init; }
    public required Action<bool> SetIsRebuildingModeOptions { get; init; }
    public required Action<bool> SetIsApplyingAutomaticResolutionSelection { get; init; }
    public required Action<bool> SetIsApplyingAutomaticFrameRateSelection { get; init; }
    public required Func<bool> IsSuppressFormatChangeReinitialize { get; init; }
    public required Action<bool> SetSuppressFormatChangeReinitialize { get; init; }
    public required Action<double?> SetDetectedSourceFrameRate { get; init; }
    public required Action<string?> SetDetectedSourceFrameRateArg { get; init; }
    public required Action<string> SetSourceFrameRateOrigin { get; init; }
    public required Action<uint?> SetAutoResolvedWidth { get; init; }
    public required Action<uint?> SetAutoResolvedHeight { get; init; }
    public required Action<double?> SetAutoResolvedFrameRate { get; init; }
    public required Action<string> SetHdrResolutionSupportHint { get; init; }
    public required Action<string> SetDisabledResolutionReason { get; init; }
    public required Action<string> SetStatusText { get; init; }

    public delegate bool TryGetEffectiveResolutionSelectionDelegate(out string resolutionKey, out uint width, out uint height);
    public delegate bool TryResolveResolutionKeyDelegate(string? resolutionValue, out string resolutionKey);
}

/// <summary>
/// Owns capture-mode option rebuild transactions for the MainViewModel compatibility facade.
/// </summary>
internal sealed class MainViewModelCaptureModeOptionRebuildController
{
    private readonly MainViewModelCaptureModeOptionRebuildControllerContext _context;
    private readonly MainViewModelFrameRateTimingResolver _frameRateTimingResolver;

    public MainViewModelCaptureModeOptionRebuildController(
        MainViewModelCaptureModeOptionRebuildControllerContext context,
        MainViewModelFrameRateTimingResolver frameRateTimingResolver)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _frameRateTimingResolver = frameRateTimingResolver ?? throw new ArgumentNullException(nameof(frameRateTimingResolver));
    }

    public void UpdateSelectedFormat()
    {
        if (!_context.TryGetEffectiveResolutionSelection(out var resolutionKey, out var width, out var height))
        {
            _context.SetSelectedFormat(null);
            return;
        }

        _context.SetSelectedFormat(CaptureFormatSelectionPolicy.Select(
            BuildCaptureFormatSelectionRequest(resolutionKey, width, height)));
    }

    public void RebuildVideoFormatOptions()
    {
        // Source-reader pixel formats are not global device capabilities. A card can expose
        // MJPG at 4K120 SDR while exposing only P010 at the HDR retarget mode, so keep this
        // list scoped to the currently selected resolution+fps tuple.
        var formats = GetFormatsForSelectedModeTuple();
        var nextFormats = CaptureModeOptionsBuilder.BuildVideoFormatOptions(formats);

        _context.AvailableVideoFormats.Clear();
        foreach (var format in nextFormats)
        {
            _context.AvailableVideoFormats.Add(format);
        }

        if (!_context.AvailableVideoFormats.Any(format => string.Equals(format, _context.GetSelectedVideoFormat(), StringComparison.OrdinalIgnoreCase)))
        {
            var previousSuppress = _context.IsSuppressFormatChangeReinitialize();
            _context.SetSuppressFormatChangeReinitialize(true);
            try
            {
                _context.SetSelectedVideoFormat("Auto");
            }
            finally
            {
                _context.SetSuppressFormatChangeReinitialize(previousSuppress);
            }
        }
    }

    private List<MediaFormat> GetFormatsForSelectedModeTuple()
    {
        if (!_context.TryGetEffectiveResolutionSelection(out var resolutionKey, out var width, out var height))
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
            _context.AvailableFormats,
            _context.AvailableFrameRates,
            width,
            height,
            _context.GetSelectedFrameRate(),
            _context.GetSelectedVideoFormat(),
            _context.IsHdrEnabled(),
            _frameRateTimingResolver.ResolvePreferredTimingFamily(resolutionKey, _context.GetSelectedFrameRate()));

    public void RebuildFrameRateOptions()
    {
        var previousRate = _context.GetSelectedFrameRate();
        var options = new List<FrameRateOption>();
        var selectedResolutionKey = _context.GetEffectiveResolutionKey(_context.GetSelectedResolution());
        var timingFamily = _frameRateTimingResolver.ResolvePreferredTimingFamily(selectedResolutionKey, previousRate);
        var sourceTelemetry = _context.GetLatestSourceTelemetry();
        if (sourceTelemetry.HasFrameRate &&
            FrameRateTimingPolicy.TryInferFrameRateTimingFamily(sourceTelemetry.FrameRateArg, sourceTelemetry.FrameRateExact, out var sourceFamilyHint))
        {
            timingFamily = sourceFamilyHint;
        }

        if (!string.IsNullOrWhiteSpace(selectedResolutionKey) &&
            _context.GetResolutionToFormats().TryGetValue(selectedResolutionKey, out var formats))
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
                    var enabled = _context.IsHdrEnabled() ? hdrFormats.Count > 0 : allFormats.Count > 0;
                    List<MediaFormat> selectionPool;
                    if (_context.IsHdrEnabled() && hdrFormats.Count > 0)
                        selectionPool = hdrFormats;
                    else if (!_context.IsHdrEnabled() && sdrFormats.Count > 0)
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

        var sourceRate = _frameRateTimingResolver.ResolveDetectedSourceFrameRate(selectedResolutionKey, options, previousRate);
        var sourceFilter = FrameRateSourceFilterPolicy.Apply(
            options,
            sourceRate.Rate,
            sourceRate.Arg,
            _frameRateTimingResolver.BuildFrameRateTimingVariants(selectedResolutionKey),
            true);
        var sourceTimingFamilyKnown = sourceFilter.SourceTimingFamilyKnown;
        var sourceTimingFamily = sourceFilter.SourceTimingFamily;
        options = sourceFilter.Options.ToList();
        var autoFrameRateOption = options.Count > 0
            ? new FrameRateOption
            {
                FriendlyValue = _context.AutoFrameRateValue,
                Value = _context.AutoFrameRateValue,
                IsEnabled = true,
                DisplayTextOverride = "Source"
            }
            : null;
        var availableOptions = autoFrameRateOption == null
            ? options
            : new[] { autoFrameRateOption }.Concat(options).ToList();
        _context.SetDetectedSourceFrameRate(sourceRate.Rate);
        _context.SetDetectedSourceFrameRateArg(sourceRate.Arg);
        _context.SetSourceFrameRateOrigin(sourceRate.Origin);

        _context.SetIsRebuildingModeOptions(true);
        try
        {
            _context.AvailableFrameRates.Clear();
            foreach (var option in availableOptions)
            {
                _context.AvailableFrameRates.Add(option);
            }

            var selection = FrameRateAutoSelectionPolicy.Select(new FrameRateAutoSelectionRequest(
                options,
                AutoFrameRateOptionAvailable: autoFrameRateOption != null,
                ForceAutoSelection: false,
                IsAutoFrameRateSelected: _context.IsAutoFrameRateSelected(),
                HasUserOverriddenFrameRateForCurrentMode: _context.HasUserOverriddenFrameRateForCurrentMode(),
                IsHdrEnabled: _context.IsHdrEnabled(),
                PendingSdrAutoSelectionForDeviceChange: _context.IsPendingSdrAutoSelectionForDeviceChange(),
                PendingSdrAutoFriendlyFrameRateBucket: _context.GetPendingSdrAutoFriendlyFrameRateBucket(),
                Source: new FrameRateAutoSelectionSource(sourceRate.Rate, sourceTimingFamilyKnown, sourceTimingFamily),
                PreviousRate: previousRate));

            if (autoFrameRateOption != null)
            {
                _context.SetIsAutoFrameRateSelected(selection.SelectAutoOption);
            }

            var fallbackRate = previousRate > 0
                ? previousRate
                : 60;
            _context.ApplyResolvedFrameRateSelection(selection.Selected, fallbackRate);
            if (_context.IsHdrEnabled() && selection.Selected is { IsEnabled: false })
            {
                _context.SetStatusText($"No HDR-capable frame rate is available for {_context.GetSelectedResolutionDisplayText()}.");
            }

            if (!_context.IsHdrEnabled() && _context.IsPendingSdrAutoSelectionForDeviceChange() && selection.Selected != null)
            {
                _context.SetPendingSdrAutoSelectionForDeviceChange(false);
                _context.SetPendingSdrAutoFriendlyFrameRateBucket(null);
            }
        }
        finally
        {
            _context.SetIsApplyingAutomaticFrameRateSelection(false);
            _context.SetIsRebuildingModeOptions(false);
        }

        RebuildVideoFormatOptions();
        UpdateSelectedFormat();
        _context.UpdateTargetSummary();
        _context.SetForceSourceAutoRetarget(false);
    }

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

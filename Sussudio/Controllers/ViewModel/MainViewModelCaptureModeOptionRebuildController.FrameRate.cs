using System.Collections.Generic;
using System.Linq;
using Sussudio.Models;
using Sussudio.ViewModels;

namespace Sussudio.Controllers;

internal sealed partial class MainViewModelCaptureModeOptionRebuildController
{
    public void RebuildFrameRateOptions()
    {
        var previousRate = _context.GetSelectedFrameRate();
        var options = new List<FrameRateOption>();
        var selectedResolutionKey = _context.GetEffectiveResolutionKey(_context.GetSelectedResolution());
        var timingFamily = _context.ResolvePreferredTimingFamily(selectedResolutionKey, previousRate);
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

        var sourceRate = _context.ResolveDetectedSourceFrameRate(selectedResolutionKey, options, previousRate);
        var sourceFilter = FrameRateSourceFilterPolicy.Apply(
            options,
            sourceRate.Rate,
            sourceRate.Arg,
            _context.BuildFrameRateTimingVariants(selectedResolutionKey),
            _context.ShowAllCaptureOptions());
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
}

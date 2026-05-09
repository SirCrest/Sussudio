using System;
using System.Collections.Generic;
using System.Linq;
using Sussudio.Models;
using Sussudio.Services.Capture;

namespace Sussudio.ViewModels;

/// <summary>
/// Resolution option building, auto-selection logic, and resolution/frame-rate query helpers.
/// </summary>
public partial class MainViewModel
{
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

        string? hdrHint = null;
        var allowSourceAutoSelect = IsHdrEnabled && (_forceSourceAutoRetarget || !_hasUserOverriddenResolutionForCurrentMode);
        var sourceSelected = allowSourceAutoSelect
            ? TrySelectSourceResolutionOption(options, desiredSelection)
            : null;
        var sourceSelectedValue = sourceSelected?.Value;
        if (IsHdrEnabled &&
            sourceSelected is { IsEnabled: true } &&
            previousRate > 0 &&
            !ResolutionSupportsFrameRate(sourceSelected.Value, previousRate, hdrOnly: true))
        {
            var sourceMax = GetMaxFrameRateForResolution(sourceSelected.Value, hdrOnly: true);
            if (sourceMax > 0)
            {
                hdrHint = $"HDR at {sourceSelected.Value} supported up to {FormatFriendlyFrameRate(sourceMax)} fps.";
            }

            sourceSelected = null;
        }

        var selected = sourceSelected;
        if (!IsHdrEnabled &&
            _pendingSdrAutoSelectionForDeviceChange &&
            TrySelectSdrAutoResolutionOption(options, out var sdrAutoSelection, out var sdrAutoFriendlyBucket))
        {
            selected = sdrAutoSelection;
            _pendingSdrAutoFriendlyFrameRateBucket = sdrAutoFriendlyBucket;
        }

        if (selected == null)
        {
            // The capture card (e.g. 4K X) cannot deliver HDR at every resolution+FPS
            // combination due to USB bandwidth limits. When HDR is enabled, we pick the
            // highest resolution that still supports the user's chosen frame rate in HDR
            // mode, which may be lower than the source resolution. Fluidity wins over
            // resolution here: drop resolution first, and only drop FPS if no HDR mode can
            // keep the current frame-rate bucket.
            selected = IsHdrEnabled
                ? SelectHdrResolutionOption(options, desiredSelection, previousRate, out hdrHint)
                : options.FirstOrDefault(option =>
                    option.IsEnabled &&
                    string.Equals(option.Value, desiredSelection, StringComparison.OrdinalIgnoreCase))
                    ?? options.FirstOrDefault(option => option.IsEnabled)
                    ?? options.FirstOrDefault();

            if (IsHdrEnabled &&
                !string.IsNullOrWhiteSpace(sourceSelectedValue) &&
                selected != null &&
                !string.Equals(sourceSelectedValue, selected.Value, StringComparison.OrdinalIgnoreCase) &&
                previousRate > 0)
            {
                var sourceMax = GetMaxFrameRateForResolution(sourceSelectedValue, hdrOnly: true);
                if (sourceMax > 0 && previousRate > sourceMax + 0.01)
                {
                    hdrHint = $"HDR at {sourceSelectedValue} supported up to {FormatFriendlyFrameRate(sourceMax)} fps; switched to {selected.Value} to keep {FormatFriendlyFrameRate(previousRate)} fps.";
                }
            }
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

    private sealed record AutoCaptureSelection(
        ResolutionOption Resolution,
        int FriendlyFrameRate,
        double ExactFrameRate);

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

    private AutoCaptureSelection? ResolveAutoCaptureSelection(IReadOnlyList<ResolutionOption> options)
    {
        if (options.Count == 0)
        {
            return null;
        }

        var rankedOptions = options
            .OrderByDescending(option => (long)option.Width * option.Height)
            .ThenByDescending(option => option.Width)
            .ToList();
        var eligibleOptions = rankedOptions.Where(option => option.IsEnabled).ToList();
        if (eligibleOptions.Count == 0)
        {
            eligibleOptions = rankedOptions;
        }

        var sourceFriendlyCap = _latestSourceTelemetry.HasFrameRate
            ? (int?)Math.Round(_latestSourceTelemetry.FrameRateExact!.Value, MidpointRounding.AwayFromZero)
            : null;
        var friendlyBuckets = eligibleOptions
            .SelectMany(GetAutoEligibleFormats)
            .Select(format => GetFriendlyFrameRateBucket(format.FrameRateExact))
            .Distinct()
            .OrderByDescending(bucket => bucket)
            .ToList();
        if (friendlyBuckets.Count == 0)
        {
            return BuildAutoCaptureSelectionFallback(eligibleOptions);
        }

        var bestFriendlyBucket = friendlyBuckets
            .FirstOrDefault(bucket => !sourceFriendlyCap.HasValue || bucket <= sourceFriendlyCap.Value);
        if (bestFriendlyBucket == 0)
        {
            bestFriendlyBucket = friendlyBuckets[0];
        }

        var matchingResolutions = eligibleOptions
            .Where(option => ResolutionSupportsFriendlyFrameRate(
                option.Value,
                bestFriendlyBucket,
                hdrOnly: IsHdrEnabled,
                sdrOnly: !IsHdrEnabled))
            .ToList();
        if (matchingResolutions.Count == 0)
        {
            matchingResolutions = eligibleOptions;
        }

        var chosenResolution = SelectBestAutoResolutionCandidate(matchingResolutions) ?? eligibleOptions[0];
        var preferredFormat = SelectPreferredAutoFrameRateFormat(chosenResolution.Value, bestFriendlyBucket);
        return new AutoCaptureSelection(
            chosenResolution,
            GetFriendlyFrameRateBucket(preferredFormat.FrameRateExact),
            preferredFormat.FrameRateExact);
    }

    private AutoCaptureSelection? BuildAutoCaptureSelectionFallback(IReadOnlyList<ResolutionOption> options)
    {
        var fallback = options.FirstOrDefault();
        if (fallback == null)
        {
            return null;
        }

        var preferredBucket = GetMaxFrameRateFriendlyBucket(fallback.Value);
        var preferredFormat = SelectPreferredAutoFrameRateFormat(fallback.Value, preferredBucket);
        return new AutoCaptureSelection(
            fallback,
            GetFriendlyFrameRateBucket(preferredFormat.FrameRateExact),
            preferredFormat.FrameRateExact);
    }

    private IEnumerable<MediaFormat> GetAutoEligibleFormats(ResolutionOption option)
    {
        if (!_resolutionToFormats.TryGetValue(option.Value, out var formats))
        {
            return Enumerable.Empty<MediaFormat>();
        }

        var filtered = formats
            .Where(format => IsHdrEnabled ? IsHdrModeCandidate(format) : !IsHdrModeCandidate(format))
            .ToList();
        return filtered.Count > 0 ? filtered : formats;
    }

    private ResolutionOption? SelectBestAutoResolutionCandidate(IReadOnlyList<ResolutionOption> candidates)
    {
        if (candidates.Count == 0)
        {
            return null;
        }

        var ranked = candidates
            .OrderByDescending(option => (long)option.Width * option.Height)
            .ThenByDescending(option => option.Width)
            .ToList();
        if (!_latestSourceTelemetry.HasDimensions)
        {
            return ranked[0];
        }

        var sourceWidth = (uint)Math.Max(0, _latestSourceTelemetry.Width ?? 0);
        var sourceHeight = (uint)Math.Max(0, _latestSourceTelemetry.Height ?? 0);
        if (sourceWidth == 0 || sourceHeight == 0)
        {
            return ranked[0];
        }

        return ranked.FirstOrDefault(option => option.Width <= sourceWidth && option.Height <= sourceHeight)
            ?? ranked[0];
    }

    private MediaFormat SelectPreferredAutoFrameRateFormat(string resolutionKey, int preferredFriendlyBucket)
    {
        if (!_resolutionToFormats.TryGetValue(resolutionKey, out var formats) || formats.Count == 0)
        {
            throw new InvalidOperationException($"No formats are available for resolution '{resolutionKey}'.");
        }

        var timingFamily = FrameRateTimingFamily.Unknown;
        if (_latestSourceTelemetry.HasFrameRate &&
            TryInferFrameRateTimingFamily(_latestSourceTelemetry.FrameRateArg, _latestSourceTelemetry.FrameRateExact, out var sourceFamily))
        {
            timingFamily = sourceFamily;
        }

        var selectionPool = formats
            .Where(format =>
                (IsHdrEnabled ? IsHdrModeCandidate(format) : !IsHdrModeCandidate(format)) &&
                GetFriendlyFrameRateBucket(format.FrameRateExact) == preferredFriendlyBucket)
            .ToList();
        if (selectionPool.Count == 0)
        {
            selectionPool = formats
                .Where(format => GetFriendlyFrameRateBucket(format.FrameRateExact) == preferredFriendlyBucket)
                .ToList();
        }
        if (selectionPool.Count == 0)
        {
            selectionPool = formats.ToList();
            preferredFriendlyBucket = GetFriendlyFrameRateBucket(selectionPool.Max(format => format.FrameRateExact));
        }

        return SelectPreferredFrameRateFormat(selectionPool, preferredFriendlyBucket, timingFamily);
    }

    private int GetMaxFrameRateFriendlyBucket(string resolutionKey)
    {
        if (!_resolutionToFormats.TryGetValue(resolutionKey, out var formats) || formats.Count == 0)
        {
            return 0;
        }

        var filtered = formats
            .Where(format => !IsHdrEnabled || IsHdrModeCandidate(format))
            .ToList();
        if (filtered.Count == 0)
        {
            filtered = formats.ToList();
        }

        return filtered
            .Select(format => GetFriendlyFrameRateBucket(format.FrameRateExact))
            .DefaultIfEmpty()
            .Max();
    }

    private string BuildAutoResolutionDisplayText()
        => AutoResolutionValue;

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

    private string GetSelectedResolutionDisplayText()
    {
        if (string.IsNullOrWhiteSpace(SelectedResolution))
        {
            return "?";
        }

        if (!IsAutoResolutionValue(SelectedResolution))
        {
            return SelectedResolution;
        }

        var friendlyRate = SelectedFriendlyFrameRate
            ?? (AutoResolvedFrameRate.HasValue
                ? Math.Round(AutoResolvedFrameRate.Value, MidpointRounding.AwayFromZero)
                : (double?)null);
        if (AutoResolvedWidth.HasValue &&
            AutoResolvedHeight.HasValue &&
            friendlyRate.HasValue)
        {
            return $"{AutoResolutionValue} ({GetResolutionKey(AutoResolvedWidth.Value, AutoResolvedHeight.Value)} @ {friendlyRate.Value:0} fps)";
        }

        return AutoResolutionValue;
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

    private void ResetFrameRateSelectionState()
    {
        _hasUserOverriddenFrameRateForCurrentMode = false;
        IsAutoFrameRateSelected = true;
    }

    private void ApplyResolvedFrameRateSelection(FrameRateOption? selected, double fallbackRate)
    {
        _isApplyingAutomaticFrameRateSelection = true;
        try
        {
            SelectedFrameRate = selected?.Value ?? fallbackRate;
        }
        finally
        {
            _isApplyingAutomaticFrameRateSelection = false;
        }

        SelectedFriendlyFrameRate = selected?.FriendlyValue ?? Math.Round(SelectedFrameRate);
        SelectedExactFrameRate = selected?.Value ?? SelectedFrameRate;
        SelectedExactFrameRateArg = selected?.Rational;
        if (IsAutoResolutionValue(SelectedResolution))
        {
            AutoResolvedFrameRate = selected?.Value ?? SelectedFrameRate;
        }

        DisabledFrameRateReason = selected is { IsEnabled: false }
            ? selected.DisableReason
            : string.Empty;
    }

    private void ResetModeSelectionState()
    {
        ResetFrameRateSelectionState();
        _hasUserOverriddenResolutionForCurrentMode = false;
        _forceSourceAutoRetarget = false;
        _lastSourceModeKey = null;
        _pendingSdrAutoSelectionForDeviceChange = false;
        _pendingSdrAutoFriendlyFrameRateBucket = null;
    }

    private ResolutionOption? TrySelectSourceResolutionOption(
        IReadOnlyList<ResolutionOption> options,
        string? previousSelection)
    {
        if (options.Count == 0 || !_latestSourceTelemetry.HasDimensions)
        {
            return null;
        }

        var sourceWidth = (uint)Math.Max(0, _latestSourceTelemetry.Width ?? 0);
        var sourceHeight = (uint)Math.Max(0, _latestSourceTelemetry.Height ?? 0);
        if (sourceWidth == 0 || sourceHeight == 0)
        {
            return null;
        }

        var exact = options.FirstOrDefault(option =>
            option.IsEnabled &&
            option.Width == sourceWidth &&
            option.Height == sourceHeight);
        if (exact != null)
        {
            return exact;
        }

        var sourceKey = GetResolutionKey(sourceWidth, sourceHeight);
        var enabled = options.Where(option => option.IsEnabled).ToList();
        if (enabled.Count == 0)
        {
            return options.FirstOrDefault();
        }

        return SelectNearestResolution(sourceKey, enabled)
            ?? SelectNearestResolution(previousSelection, enabled)
            ?? enabled.FirstOrDefault();
    }

    private ResolutionOption? SelectHdrResolutionOption(
        IReadOnlyList<ResolutionOption> options,
        string? previousSelection,
        double preferredFrameRate,
        out string? hint)
    {
        hint = null;
        if (options.Count == 0)
        {
            return null;
        }

        var previous = options.FirstOrDefault(option =>
            string.Equals(option.Value, previousSelection, StringComparison.OrdinalIgnoreCase));
        if (previous is { IsEnabled: true } &&
            ResolutionSupportsFrameRate(previous.Value, preferredFrameRate, hdrOnly: true))
        {
            hint = BuildHdrSupportHintForResolution(previous.Value);
            return previous;
        }

        var sameFpsCandidates = options
            .Where(option =>
                option.IsEnabled &&
                ResolutionSupportsFrameRate(option.Value, preferredFrameRate, hdrOnly: true))
            .ToList();

        // Prefer an HDR-capable mode in the same friendly FPS bucket before considering
        // any other HDR mode. SelectNearestResolution intentionally favors the nearest
        // lower resolution, so 4K120 SDR retargets to 1080p120 HDR before falling to 4K60.
        var selected = SelectNearestResolution(previousSelection, sameFpsCandidates)
            ?? SelectNearestResolution(previousSelection, options.Where(option => option.IsEnabled).ToList())
            ?? options.FirstOrDefault(option => option.IsEnabled)
            ?? options.FirstOrDefault();

        if (!string.IsNullOrWhiteSpace(previousSelection) &&
            !string.Equals(previousSelection, selected?.Value, StringComparison.OrdinalIgnoreCase))
        {
            var previousMax = GetMaxFrameRateForResolution(previousSelection, hdrOnly: true);
            if (previousMax > 0)
            {
                hint = $"HDR at {previousSelection} supported up to {FormatFriendlyFrameRate(previousMax)} fps.";
            }
        }

        hint ??= BuildHdrSupportHintForResolution(selected?.Value);
        return selected;
    }

    private bool TrySelectSdrAutoResolutionOption(
        IReadOnlyList<ResolutionOption> options,
        out ResolutionOption? selected,
        out int selectedFriendlyBucket)
    {
        selected = null;
        selectedFriendlyBucket = 60;
        if (options.Count == 0)
        {
            return false;
        }

        var enabledOptions = options
            .Where(option => option.IsEnabled)
            .OrderByDescending(option => (long)option.Width * option.Height)
            .ToList();
        if (enabledOptions.Count == 0)
        {
            return false;
        }

        var sdrFriendlyBucketsByResolution = new Dictionary<string, HashSet<int>>(StringComparer.OrdinalIgnoreCase);
        foreach (var option in enabledOptions)
        {
            if (!_resolutionToFormats.TryGetValue(option.Value, out var formats))
            {
                continue;
            }

            var buckets = formats
                .Where(format => !IsHdrModeCandidate(format))
                .Select(format => GetFriendlyFrameRateBucket(format.FrameRateExact))
                .ToHashSet();
            if (buckets.Count > 0)
            {
                sdrFriendlyBucketsByResolution[option.Value] = buckets;
            }
        }

        if (sdrFriendlyBucketsByResolution.Count == 0)
        {
            return false;
        }

        foreach (var friendlyBucket in new[] { 60, 30 })
        {
            var match = enabledOptions.FirstOrDefault(option =>
                sdrFriendlyBucketsByResolution.TryGetValue(option.Value, out var buckets) &&
                buckets.Contains(friendlyBucket));
            if (match != null)
            {
                selected = match;
                selectedFriendlyBucket = friendlyBucket;
                return true;
            }
        }

        selected = enabledOptions.FirstOrDefault(option => sdrFriendlyBucketsByResolution.ContainsKey(option.Value));
        if (selected == null)
        {
            return false;
        }

        selectedFriendlyBucket = ResolvePreferredFriendlyBucketForResolution(selected.Value, sdrOnly: true) ?? 30;
        return true;
    }

    private static ResolutionOption? SelectNearestResolution(string? baselineResolution, IReadOnlyList<ResolutionOption> candidates)
    {
        if (candidates.Count == 0)
        {
            return null;
        }

        if (!TryParseResolutionKey(baselineResolution, out var baseWidth, out var baseHeight))
        {
            return candidates
                .OrderByDescending(option => (long)option.Width * option.Height)
                .FirstOrDefault();
        }

        var baseArea = (long)baseWidth * baseHeight;
        var lowerCandidate = candidates
            .Where(option => ((long)option.Width * option.Height) < baseArea)
            .OrderByDescending(option => (long)option.Width * option.Height)
            .FirstOrDefault();
        if (lowerCandidate != null)
        {
            return lowerCandidate;
        }

        return candidates
            .OrderBy(option => Math.Abs(((long)option.Width * option.Height) - baseArea))
            .ThenByDescending(option => (long)option.Width * option.Height)
            .FirstOrDefault();
    }

    private static bool TryParseResolutionKey(string? resolutionKey, out uint width, out uint height)
    {
        width = 0;
        height = 0;
        if (string.IsNullOrWhiteSpace(resolutionKey) || IsAutoResolutionValue(resolutionKey))
        {
            return false;
        }

        var parts = resolutionKey.Split('x');
        return parts.Length == 2 &&
               uint.TryParse(parts[0], out width) &&
               uint.TryParse(parts[1], out height);
    }

    private bool ResolutionSupportsFrameRate(string resolutionKey, double frameRate, bool hdrOnly)
    {
        if (frameRate <= 0)
        {
            return false;
        }

        var requestedBucket = GetFriendlyFrameRateBucket(frameRate);
        return ResolutionSupportsFriendlyFrameRate(
            resolutionKey,
            requestedBucket,
            hdrOnly: hdrOnly,
            sdrOnly: !hdrOnly);
    }

    private bool ResolutionSupportsFriendlyFrameRate(
        string resolutionKey,
        int friendlyBucket,
        bool hdrOnly,
        bool sdrOnly)
    {
        if (!_resolutionToFormats.TryGetValue(resolutionKey, out var formats))
        {
            return false;
        }

        return formats.Any(format =>
            (!hdrOnly || IsHdrModeCandidate(format)) &&
            (!sdrOnly || !IsHdrModeCandidate(format)) &&
            GetFriendlyFrameRateBucket(format.FrameRateExact) == friendlyBucket);
    }

    private int? ResolvePreferredFriendlyBucketForResolution(string resolutionKey, bool sdrOnly)
    {
        if (!_resolutionToFormats.TryGetValue(resolutionKey, out var formats))
        {
            return null;
        }

        var buckets = formats
            .Where(format => !sdrOnly || !IsHdrModeCandidate(format))
            .Select(format => GetFriendlyFrameRateBucket(format.FrameRateExact))
            .Distinct()
            .OrderByDescending(bucket => bucket)
            .ToList();
        if (buckets.Count == 0)
        {
            return null;
        }

        if (buckets.Contains(60))
        {
            return 60;
        }

        if (buckets.Contains(30))
        {
            return 30;
        }

        return buckets[0];
    }

    private bool ResolutionHasTimingFamilyVariant(
        string? resolutionKey,
        double friendlyFrameRate,
        FrameRateTimingFamily family)
    {
        if (family == FrameRateTimingFamily.Unknown ||
            string.IsNullOrWhiteSpace(resolutionKey) ||
            !_resolutionToFormats.TryGetValue(resolutionKey, out var formats))
        {
            return false;
        }

        var bucket = (int)Math.Round(friendlyFrameRate, MidpointRounding.AwayFromZero);
        foreach (var format in formats)
        {
            if (GetFriendlyFrameRateBucket(format.FrameRateExact) != bucket)
            {
                continue;
            }

            if (TryInferFrameRateTimingFamily(format.FrameRateRational, format.FrameRateExact, out var formatFamily) &&
                formatFamily == family)
            {
                return true;
            }
        }

        return false;
    }

    private double GetMaxFrameRateForResolution(string? resolutionKey, bool hdrOnly)
    {
        if (string.IsNullOrWhiteSpace(resolutionKey) ||
            !_resolutionToFormats.TryGetValue(resolutionKey, out var formats))
        {
            return 0;
        }

        var candidates = hdrOnly
            ? formats.Where(IsHdrModeCandidate).ToList()
            : formats;
        if (candidates.Count == 0)
        {
            return 0;
        }

        return candidates.Max(format => format.FrameRateExact);
    }

    private string BuildHdrSupportHintForResolution(string? resolutionKey)
    {
        if (!IsHdrEnabled || string.IsNullOrWhiteSpace(resolutionKey))
        {
            return string.Empty;
        }

        var maxHdrRate = GetMaxFrameRateForResolution(resolutionKey, hdrOnly: true);
        if (maxHdrRate <= 0)
        {
            return $"HDR is not supported at {resolutionKey}.";
        }

        if (SelectedFrameRate > 0 && maxHdrRate >= SelectedFrameRate - 0.01)
        {
            return string.Empty;
        }

        return $"HDR at {resolutionKey} supported up to {FormatFriendlyFrameRate(maxHdrRate)} fps.";
    }
}

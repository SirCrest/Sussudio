using System;

namespace Sussudio.ViewModels;

/// <summary>
/// Effective resolution state and selection-policy delegates.
/// </summary>
public partial class MainViewModel
{
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

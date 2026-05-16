using System;

namespace Sussudio.ViewModels;

internal static partial class CaptureResolutionSelectionPolicy
{
    internal static CaptureResolutionSelection Select(CaptureResolutionSelectionRequest request)
    {
        if (request.Options.Count == 0)
        {
            return new CaptureResolutionSelection(null, null, null);
        }

        var sourceSelected = request.AllowSourceAutoSelect
            ? SelectSourceResolutionOption(request.Options, request.PreferredSelection, request.SourceTelemetry)
            : null;
        var sourceSelectedValue = sourceSelected?.Value;
        string? hdrHint = null;
        if (request.IsHdrEnabled &&
            sourceSelected is { IsEnabled: true } &&
            request.PreviousFrameRate > 0 &&
            !ResolutionSupportsFrameRate(
                request.ResolutionToFormats,
                sourceSelected.Value,
                request.PreviousFrameRate,
                hdrOnly: true))
        {
            var sourceMax = GetMaxFrameRateForResolution(
                request.ResolutionToFormats,
                sourceSelected.Value,
                hdrOnly: true);
            if (sourceMax > 0)
            {
                hdrHint = $"HDR at {sourceSelected.Value} supported up to {FormatFriendlyFrameRate(sourceMax)} fps.";
            }

            sourceSelected = null;
        }

        var selected = sourceSelected;
        int? sdrAutoFriendlyBucket = null;
        if (!request.IsHdrEnabled &&
            request.PendingSdrAutoSelectionForDeviceChange)
        {
            var sdrAutoSelection = SelectSdrAutoResolutionOption(
                request.Options,
                request.ResolutionToFormats);
            if (sdrAutoSelection != null)
            {
                selected = sdrAutoSelection.Selected;
                sdrAutoFriendlyBucket = sdrAutoSelection.SelectedFriendlyBucket;
            }
        }

        if (selected == null)
        {
            if (request.IsHdrEnabled)
            {
                var hdrSelection = SelectHdrResolutionOption(
                    request.Options,
                    request.ResolutionToFormats,
                    request.PreferredSelection,
                    request.PreviousFrameRate);
                selected = hdrSelection.Selected;
                hdrHint = hdrSelection.Hint ?? hdrHint;
            }
            else
            {
                selected = SelectSdrResolutionOption(request.Options, request.PreferredSelection);
            }

            if (request.IsHdrEnabled &&
                !string.IsNullOrWhiteSpace(sourceSelectedValue) &&
                selected != null &&
                !string.Equals(sourceSelectedValue, selected.Value, StringComparison.OrdinalIgnoreCase) &&
                request.PreviousFrameRate > 0)
            {
                var sourceMax = GetMaxFrameRateForResolution(
                    request.ResolutionToFormats,
                    sourceSelectedValue,
                    hdrOnly: true);
                if (sourceMax > 0 && request.PreviousFrameRate > sourceMax + 0.01)
                {
                    hdrHint = $"HDR at {sourceSelectedValue} supported up to {FormatFriendlyFrameRate(sourceMax)} fps; switched to {selected.Value} to keep {FormatFriendlyFrameRate(request.PreviousFrameRate)} fps.";
                }
            }
        }

        return new CaptureResolutionSelection(
            selected,
            hdrHint,
            sdrAutoFriendlyBucket);
    }

}

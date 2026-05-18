using System;
using System.Collections.Generic;
using System.Linq;
using Sussudio.Models;

namespace Sussudio.ViewModels;

/// <summary>
/// Format and frame-rate selection adapter: selected-format assignment and
/// pixel-format option collection mutation for the capture mode pipeline.
/// </summary>
public partial class MainViewModel
{
    private void UpdateSelectedFormat()
    {
        if (!TryGetEffectiveResolutionSelection(out var resolutionKey, out var width, out var height))
        {
            SelectedFormat = null;
            return;
        }

        SelectedFormat = CaptureFormatSelectionPolicy.Select(
            BuildCaptureFormatSelectionRequest(resolutionKey, width, height));
    }

    private void RebuildVideoFormatOptions()
    {
        // Source-reader pixel formats are not global device capabilities. A card can expose
        // MJPG at 4K120 SDR while exposing only P010 at the HDR retarget mode, so keep this
        // list scoped to the currently selected resolution+fps tuple.
        var formats = GetFormatsForSelectedModeTuple();
        var nextFormats = CaptureModeOptionsBuilder.BuildVideoFormatOptions(formats);

        AvailableVideoFormats.Clear();
        foreach (var format in nextFormats)
        {
            AvailableVideoFormats.Add(format);
        }

        if (!AvailableVideoFormats.Any(format => string.Equals(format, SelectedVideoFormat, StringComparison.OrdinalIgnoreCase)))
        {
            var previousSuppress = _suppressFormatChangeReinitialize;
            _suppressFormatChangeReinitialize = true;
            try
            {
                SelectedVideoFormat = "Auto";
            }
            finally
            {
                _suppressFormatChangeReinitialize = previousSuppress;
            }
        }
    }

    private List<MediaFormat> GetFormatsForSelectedModeTuple()
    {
        if (!TryGetEffectiveResolutionSelection(out var resolutionKey, out var width, out var height))
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
            AvailableFormats,
            AvailableFrameRates,
            width,
            height,
            SelectedFrameRate,
            SelectedVideoFormat,
            IsHdrEnabled,
            ResolvePreferredTimingFamily(resolutionKey, SelectedFrameRate));
}

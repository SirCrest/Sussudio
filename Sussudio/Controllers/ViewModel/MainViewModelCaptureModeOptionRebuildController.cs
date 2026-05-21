using System;
using System.Collections.Generic;
using System.Linq;
using Sussudio.Models;
using Sussudio.ViewModels;

namespace Sussudio.Controllers;

/// <summary>
/// Owns capture-mode option rebuild transactions for the MainViewModel compatibility facade.
/// </summary>
internal sealed partial class MainViewModelCaptureModeOptionRebuildController
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
}

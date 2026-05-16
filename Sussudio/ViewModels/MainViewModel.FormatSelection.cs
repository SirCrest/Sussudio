using System;
using System.Collections.Generic;
using System.Linq;
using Sussudio.Models;
using Sussudio.Services.Capture;

namespace Sussudio.ViewModels;

/// <summary>
/// Format and frame-rate selection: pixel-format option building and selected
/// capture-format policy for the capture mode pipeline.
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

        var candidates = AvailableFormats
            .Where(f => f.Width == width && f.Height == height)
            .ToList();
        if (IsHdrEnabled)
        {
            candidates = candidates.Where(IsHdrModeCandidate).ToList();
        }
        else
        {
            // When HDR is off, exclude 10-bit formats (P010/P016) so the source reader
            // requests an 8-bit subtype (NV12) rather than triggering a P010→NV12 fallback.
            var sdrCandidates = candidates.Where(f => !IsHdrModeCandidate(f)).ToList();
            if (sdrCandidates.Count > 0)
                candidates = sdrCandidates;
        }

        if (candidates.Count == 0)
        {
            SelectedFormat = null;
            return;
        }

        var selectedRateOption = AvailableFrameRates
            .FirstOrDefault(option => FrameRateTimingPolicy.IsFrameRateMatch(option.Value, SelectedFrameRate))
            ?? AvailableFrameRates.FirstOrDefault(option => FrameRateTimingPolicy.IsFriendlyFrameRateMatch(option.FriendlyValue, SelectedFrameRate));
        var friendlyBucket = selectedRateOption != null
            ? (int)Math.Round(selectedRateOption.FriendlyValue, MidpointRounding.AwayFromZero)
            : FrameRateTimingPolicy.GetFriendlyFrameRateBucket(SelectedFrameRate);

        var timingFamily = ResolvePreferredTimingFamily(resolutionKey, SelectedFrameRate);
        if (selectedRateOption != null &&
            FrameRateTimingPolicy.TryInferFrameRateTimingFamily(selectedRateOption.Rational, selectedRateOption.Value, out var optionFamily))
        {
            timingFamily = optionFamily;
        }

        var rateCandidates = candidates
            .Where(format => FrameRateTimingPolicy.GetFriendlyFrameRateBucket(format.FrameRateExact) == friendlyBucket)
            .ToList();
        if (rateCandidates.Count == 0)
        {
            SelectedFormat = null;
            return;
        }

        if (!string.Equals(SelectedVideoFormat, "Auto", StringComparison.OrdinalIgnoreCase))
        {
            rateCandidates = rateCandidates
                .Where(format => string.Equals(format.PixelFormat, SelectedVideoFormat, StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (rateCandidates.Count == 0)
            {
                SelectedFormat = null;
                return;
            }
        }

        SelectedFormat = FrameRateTimingPolicy.SelectPreferredFrameRateFormat(rateCandidates, friendlyBucket, timingFamily);
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
        if (!TryGetEffectiveResolutionSelection(out _, out var width, out var height))
        {
            return new List<MediaFormat>();
        }

        // The UI groups 119.88/120.00-style variants under a friendly bucket. We still
        // select the exact rational later, but format availability should follow the same
        // bucket the user sees in the frame-rate picker.
        var selectedRateOption = AvailableFrameRates
            .FirstOrDefault(option => FrameRateTimingPolicy.IsFrameRateMatch(option.Value, SelectedFrameRate))
            ?? AvailableFrameRates.FirstOrDefault(option => FrameRateTimingPolicy.IsFriendlyFrameRateMatch(option.FriendlyValue, SelectedFrameRate));
        var friendlyBucket = selectedRateOption != null
            ? (int)Math.Round(selectedRateOption.FriendlyValue, MidpointRounding.AwayFromZero)
            : FrameRateTimingPolicy.GetFriendlyFrameRateBucket(SelectedFrameRate);

        return AvailableFormats
            .Where(format =>
                format.Width == width &&
                format.Height == height &&
                FrameRateTimingPolicy.GetFriendlyFrameRateBucket(format.FrameRateExact) == friendlyBucket &&
                (IsHdrEnabled ? IsHdrModeCandidate(format) : !IsHdrModeCandidate(format)))
            .ToList();
    }

    private static bool IsHdrModeCandidate(MediaFormat format)
        => CaptureModeOptionsBuilder.IsHdrModeCandidate(format);

    private static bool ShouldPreserveMjpegHighFrameRateMode(MediaFormat? format)
        => format != null &&
           CaptureSettings.IsMjpegHighFrameRateMode(
               format.PixelFormat,
               format.Width,
               format.Height,
               format.FrameRateExact,
               hdrEnabled: false);

}

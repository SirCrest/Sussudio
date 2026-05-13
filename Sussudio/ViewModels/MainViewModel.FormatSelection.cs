using System;
using System.Collections.Generic;
using System.Linq;
using Sussudio.Models;
using Sussudio.Services.Capture;

namespace Sussudio.ViewModels;

/// <summary>
/// Format and frame-rate selection: pixel-format option building, recording format filtering,
/// and HDR toggle side-effects for the capture mode pipeline.
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
            .FirstOrDefault(option => IsFrameRateMatch(option.Value, SelectedFrameRate))
            ?? AvailableFrameRates.FirstOrDefault(option => IsFriendlyFrameRateMatch(option.FriendlyValue, SelectedFrameRate));
        var friendlyBucket = selectedRateOption != null
            ? (int)Math.Round(selectedRateOption.FriendlyValue, MidpointRounding.AwayFromZero)
            : GetFriendlyFrameRateBucket(SelectedFrameRate);

        var timingFamily = ResolvePreferredTimingFamily(resolutionKey, SelectedFrameRate);
        if (selectedRateOption != null &&
            TryInferFrameRateTimingFamily(selectedRateOption.Rational, selectedRateOption.Value, out var optionFamily))
        {
            timingFamily = optionFamily;
        }

        var rateCandidates = candidates
            .Where(format => GetFriendlyFrameRateBucket(format.FrameRateExact) == friendlyBucket)
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

        SelectedFormat = SelectPreferredFrameRateFormat(rateCandidates, friendlyBucket, timingFamily);
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
            .FirstOrDefault(option => IsFrameRateMatch(option.Value, SelectedFrameRate))
            ?? AvailableFrameRates.FirstOrDefault(option => IsFriendlyFrameRateMatch(option.FriendlyValue, SelectedFrameRate));
        var friendlyBucket = selectedRateOption != null
            ? (int)Math.Round(selectedRateOption.FriendlyValue, MidpointRounding.AwayFromZero)
            : GetFriendlyFrameRateBucket(SelectedFrameRate);

        return AvailableFormats
            .Where(format =>
                format.Width == width &&
                format.Height == height &&
                GetFriendlyFrameRateBucket(format.FrameRateExact) == friendlyBucket &&
                (IsHdrEnabled ? IsHdrModeCandidate(format) : !IsHdrModeCandidate(format)))
            .ToList();
    }

    /// <summary>
    /// H.264 is intentionally excluded from HDR recording: the nvenc H.264
    /// encoder has no 10-bit profile, so it cannot carry bt2020/PQ metadata.
    /// Only HEVC (Main 10) and AV1 (main profile, 10-bit) support HDR output.
    /// When HDR is enabled, <see cref="RebuildRecordingFormatOptions"/> filters
    /// the codec list to these two formats and the UI hides H.264.
    /// </summary>
    private static bool IsHdrCompatibleRecordingFormat(string format)
        => format.Contains("HEVC", StringComparison.OrdinalIgnoreCase) ||
           format.Contains("AV1", StringComparison.OrdinalIgnoreCase);

    private void RebuildRecordingFormatOptions()
    {
        var sourceFormats = _detectedRecordingFormats.Count > 0
            ? _detectedRecordingFormats.ToList()
            : AvailableRecordingFormats.ToList();
        if (sourceFormats.Count == 0)
        {
            sourceFormats.Add(DefaultRecordingFormat);
        }
        var formats = IsHdrEnabled
            ? sourceFormats.Where(IsHdrCompatibleRecordingFormat).ToList()
            : sourceFormats.ToList();
        if (formats.Count == 0 && AvailableRecordingFormats.Count > 0)
        {
            // Keep the last known real formats visible if capability refresh temporarily produced none.
            formats = AvailableRecordingFormats.ToList();
        }

        AvailableRecordingFormats.Clear();
        foreach (var format in formats)
        {
            AvailableRecordingFormats.Add(format);
        }

        string? targetFormat;
        if (IsHdrEnabled)
        {
            // Preserve the user's codec when it already supports HDR (AV1 or HEVC).
            // Only override to HEVC/AV1 when the current selection is incompatible
            // (e.g. H.264, which has no 10-bit HDR profile on nvenc).
            if (!string.IsNullOrWhiteSpace(SelectedRecordingFormat) &&
                formats.Any(format => string.Equals(format, SelectedRecordingFormat, StringComparison.OrdinalIgnoreCase)) &&
                IsHdrCompatibleRecordingFormat(SelectedRecordingFormat))
            {
                targetFormat = SelectedRecordingFormat;
            }
            else
            {
                targetFormat = formats.FirstOrDefault(format =>
                    string.Equals(format, HevcRecordingFormat, StringComparison.OrdinalIgnoreCase))
                    ?? formats.FirstOrDefault(format =>
                        string.Equals(format, Av1RecordingFormat, StringComparison.OrdinalIgnoreCase))
                    ?? formats.FirstOrDefault();
            }
        }
        else
        {
            targetFormat = SelectedRecordingFormat;
            if (string.IsNullOrWhiteSpace(targetFormat) ||
                !formats.Any(format => string.Equals(format, targetFormat, StringComparison.OrdinalIgnoreCase)))
            {
                targetFormat = formats.FirstOrDefault(format =>
                    format.Contains("H.264", StringComparison.OrdinalIgnoreCase) ||
                    format.Contains("H264", StringComparison.OrdinalIgnoreCase))
                    ?? formats.FirstOrDefault();
            }
        }

        if (string.IsNullOrWhiteSpace(targetFormat))
        {
            targetFormat = DefaultRecordingFormat;
        }

        var previousSelection = SelectedRecordingFormat;
        SelectedRecordingFormat = targetFormat;
        if (string.Equals(previousSelection, targetFormat, StringComparison.Ordinal))
        {
            OnPropertyChanged(nameof(SelectedRecordingFormat));
        }

        if (IsHdrEnabled && !IsHdrCompatibleRecordingFormat(SelectedRecordingFormat))
        {
            StatusText = "HDR recording requires HEVC or AV1 (10-bit).";
        }

        Logger.Log($"Selected recording format: {SelectedRecordingFormat}");
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

    partial void OnIsHdrEnabledChanged(bool value)
    {
        if (_isRevertingHdrToggle)
        {
            return;
        }

        if (value)
        {
            _pendingSdrAutoSelectionForDeviceChange = false;
            _pendingSdrAutoFriendlyFrameRateBucket = null;
        }

        if (IsRecording)
        {
            _isRevertingHdrToggle = true;
            try
            {
                IsHdrEnabled = !value;
            }
            finally
            {
                _isRevertingHdrToggle = false;
            }

            StatusText = HdrToggleBlockedWhileRecordingMessage;
            return;
        }

        if (!_isChangingDevice)
        {
            _suppressFormatChangeReinitialize = true;
            try
            {
                ResetModeSelectionState();
                RebuildResolutionOptions();
                RebuildRecordingFormatOptions();
            }
            finally
            {
                _suppressFormatChangeReinitialize = false;
            }

            if (IsInitialized && !IsRecording && SelectedDevice != null && SelectedFormat != null)
            {
                Logger.Log($"HDR toggle changed to {(value ? "On" : "Off")} - forcing immediate device renegotiation");
                EnqueueUiOperation(() => ReinitializeDeviceAsync("HDR toggle"), "hdr toggle reinitialize");
            }
        }

        SaveSettings();
    }

}

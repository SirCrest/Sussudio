using System;
using System.Linq;
using Sussudio.Models;

namespace Sussudio.ViewModels;

/// <summary>
/// Builds capture settings from the current UI selection and observed runtime state.
/// </summary>
public partial class MainViewModel
{
    private CaptureSettings BuildCaptureSettings()
    {
        var format = SelectedRecordingFormat switch
        {
            "HEVC" => RecordingFormat.HevcMp4,
            "AV1" => RecordingFormat.Av1Mp4,
            _ => RecordingFormat.H264Mp4
        };

        var quality = SelectedQuality switch
        {
            "Auto" => VideoQuality.Auto,
            "Low" => VideoQuality.Low,
            "Medium" => VideoQuality.Medium,
            "High" => VideoQuality.High,
            "Super High" => VideoQuality.SuperHigh,
            "Custom" => VideoQuality.Custom,
            _ => VideoQuality.High
        };

        var selectedFrameRateOption = AvailableFrameRates
            .FirstOrDefault(option => IsFrameRateMatch(option.Value, SelectedFrameRate))
            ?? AvailableFrameRates.FirstOrDefault(option => IsFriendlyFrameRateMatch(option.FriendlyValue, SelectedFrameRate));

        var requestedFrameRateArg = selectedFrameRateOption?.Rational;
        var requestedFrameRateNumerator = selectedFrameRateOption?.Numerator;
        var requestedFrameRateDenominator = selectedFrameRateOption?.Denominator;
        var effectiveFrameRate = IsAutoResolutionValue(SelectedResolution) && AutoResolvedFrameRate.HasValue && AutoResolvedFrameRate.Value > 0
            ? AutoResolvedFrameRate.Value
            : SelectedFrameRate > 0
            ? SelectedFrameRate
            : selectedFrameRateOption?.Value
                ?? SelectedFormat?.FrameRateExact
                ?? 60;
        var effectiveResolutionKnown = TryGetEffectiveResolutionSelection(out _, out var effectiveWidth, out var effectiveHeight);
        var runtime = _captureService.GetRuntimeSnapshot();
        var sourceTelemetry = _captureService.GetLatestSourceTelemetrySnapshot();
        var selectedFriendlyRate = selectedFrameRateOption?.FriendlyValue ?? effectiveFrameRate;
        var runtimeRate = runtime.ActualFrameRate ?? runtime.NegotiatedFrameRate;
        var runtimeRateArg = runtime.ActualFrameRateArg ?? runtime.NegotiatedFrameRateArg;
        var runtimeMatchesResolution = false;
        if (effectiveResolutionKnown)
        {
            runtimeMatchesResolution =
                (runtime.ActualWidth == effectiveWidth && runtime.ActualHeight == effectiveHeight) ||
                (runtime.NegotiatedWidth == effectiveWidth && runtime.NegotiatedHeight == effectiveHeight);
        }

        if (runtimeMatchesResolution &&
            runtimeRate.HasValue &&
            runtimeRate.Value > 0 &&
            IsFriendlyFrameRateMatch(selectedFriendlyRate, runtimeRate.Value))
        {
            if (!string.IsNullOrWhiteSpace(runtimeRateArg))
            {
                requestedFrameRateArg = runtimeRateArg;
            }

            if (runtime.NegotiatedFrameRateNumerator.HasValue &&
                runtime.NegotiatedFrameRateDenominator.HasValue &&
                runtime.NegotiatedFrameRateDenominator.Value > 0)
            {
                requestedFrameRateNumerator = runtime.NegotiatedFrameRateNumerator;
                requestedFrameRateDenominator = runtime.NegotiatedFrameRateDenominator;
            }
            else if (TryParseFrameRateRational(runtimeRateArg, out var runtimeNumerator, out var runtimeDenominator))
            {
                requestedFrameRateNumerator = runtimeNumerator;
                requestedFrameRateDenominator = runtimeDenominator;
            }
        }

        if (sourceTelemetry.HasFrameRate &&
            IsFriendlyFrameRateMatch(selectedFriendlyRate, sourceTelemetry.FrameRateExact ?? 0))
        {
            if (!string.IsNullOrWhiteSpace(sourceTelemetry.FrameRateArg))
            {
                requestedFrameRateArg = sourceTelemetry.FrameRateArg;
            }

            if (TryParseFrameRateRational(sourceTelemetry.FrameRateArg, out var sourceNumerator, out var sourceDenominator))
            {
                requestedFrameRateNumerator = sourceNumerator;
                requestedFrameRateDenominator = sourceDenominator;
            }
        }

        if ((requestedFrameRateNumerator == null || requestedFrameRateDenominator == null) &&
            TryParseFrameRateRational(requestedFrameRateArg, out var parsedNumerator, out var parsedDenominator))
        {
            requestedFrameRateNumerator = parsedNumerator;
            requestedFrameRateDenominator = parsedDenominator;
        }

        if (requestedFrameRateNumerator == null || requestedFrameRateDenominator == null)
        {
            if (SelectedFormat?.FrameRateNumerator > 0 && SelectedFormat.FrameRateDenominator > 0)
            {
                requestedFrameRateNumerator = SelectedFormat.FrameRateNumerator;
                requestedFrameRateDenominator = SelectedFormat.FrameRateDenominator;
                requestedFrameRateArg = SelectedFormat.FrameRateRational;
            }
            else
            {
                requestedFrameRateArg = effectiveFrameRate.ToString("0.###");
            }
        }

        var settings = new CaptureSettings
        {
            Width = effectiveResolutionKnown ? effectiveWidth : (SelectedFormat?.Width ?? 1920),
            Height = effectiveResolutionKnown ? effectiveHeight : (SelectedFormat?.Height ?? 1080),
            FrameRate = effectiveFrameRate,
            RequestedFrameRateArg = requestedFrameRateArg,
            RequestedFrameRateNumerator = requestedFrameRateNumerator,
            RequestedFrameRateDenominator = requestedFrameRateDenominator,
            RequestedPixelFormat = ResolveRequestedPixelFormat(),
            ForceMjpegDecode = ShouldForceMjpegDecode(),
            FlashbackGpuDecode = FlashbackGpuDecode,
            FlashbackBufferMinutes = FlashbackBufferMinutes,
            Format = format,
            Quality = quality,
            NvencPreset = NvencPresetParser.Parse(SelectedPreset),
            SplitEncodeMode = SplitEncodeModeParser.Parse(SelectedSplitEncodeMode),
            CustomBitrateMbps = CustomBitrateMbps,
            HdrEnabled = IsHdrEnabled,
            HdrOutputMode = IsHdrEnabled ? HdrOutputMode.Hdr10Pq : HdrOutputMode.Off,
            PreviewMode = IsTrueHdrPreviewEnabled ? PreviewMode.TrueHdr : PreviewMode.GpuFast,
            OutputPath = OutputPath,
            AudioEnabled = IsAudioEnabled,
            MjpegDecoderCount = Math.Clamp(MjpegDecoderCount, 1, 8)
        };

        settings.UseCustomAudioInput = IsCustomAudioInputEnabled;
        if (IsCustomAudioInputEnabled && SelectedAudioInputDevice != null)
        {
            settings.AudioDeviceId = SelectedAudioInputDevice.Id;
            settings.AudioDeviceName = SelectedAudioInputDevice.Name;
        }

        settings.MicrophoneEnabled = IsMicrophoneEnabled;
        if (IsMicrophoneEnabled && SelectedMicrophoneDevice != null)
        {
            settings.MicrophoneDeviceId = SelectedMicrophoneDevice.Id;
            settings.MicrophoneDeviceName = SelectedMicrophoneDevice.Name;
        }

        return settings;
    }

    /// <summary>
    /// Resolves the pixel format to request from the source reader. On auto at
    /// 4K HFR, forces MJPG so the parallel decode pipeline is used instead of
    /// MF's single-pipeline internal MJPG->NV12 decode.
    /// </summary>
    private string? ResolveRequestedPixelFormat()
    {
        if (!string.Equals(SelectedVideoFormat, "Auto", StringComparison.OrdinalIgnoreCase))
        {
            return SelectedVideoFormat;
        }

        var format = SelectedFormat;
        if (format != null &&
            !IsHdrEnabled &&
            format.Width >= 3840 &&
            format.Height >= 2160 &&
            format.FrameRateExact >= 100)
        {
            return "MJPG";
        }

        return format?.PixelFormat;
    }

    private bool ShouldForceMjpegDecode()
    {
        if (string.Equals(SelectedVideoFormat, "MJPG", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // On auto at 4K HFR, force parallel MJPEG decode.
        if (string.Equals(SelectedVideoFormat, "Auto", StringComparison.OrdinalIgnoreCase))
        {
            var format = SelectedFormat;
            return format != null &&
                   !IsHdrEnabled &&
                   format.Width >= 3840 &&
                   format.Height >= 2160 &&
                   format.FrameRateExact >= 100;
        }

        return false;
    }
}

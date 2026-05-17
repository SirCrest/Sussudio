using System;
using Sussudio.Models;

namespace Sussudio.ViewModels;

/// <summary>
/// Builds capture settings from the current UI selection and observed runtime state.
/// </summary>
public partial class MainViewModel
{
    private CaptureSettings BuildCaptureSettings()
    {
        var format = RecordingSettingsSelectionPolicy.ParseRecordingFormat(SelectedRecordingFormat);
        var quality = RecordingSettingsSelectionPolicy.ParseVideoQuality(SelectedQuality);

        var effectiveResolutionKnown = TryGetEffectiveResolutionSelection(out _, out var effectiveWidth, out var effectiveHeight);
        var runtime = _captureService.GetRuntimeSnapshot();
        var sourceTelemetry = _captureService.GetLatestSourceTelemetrySnapshot();
        var frameRateProjection = ProjectCaptureSettingsFrameRate(new CaptureSettingsFrameRateRequest(
            effectiveResolutionKnown,
            effectiveWidth,
            effectiveHeight,
            runtime,
            sourceTelemetry));

        var settings = new CaptureSettings
        {
            Width = effectiveResolutionKnown ? effectiveWidth : (SelectedFormat?.Width ?? 1920),
            Height = effectiveResolutionKnown ? effectiveHeight : (SelectedFormat?.Height ?? 1080),
            FrameRate = frameRateProjection.EffectiveFrameRate,
            RequestedFrameRateArg = frameRateProjection.RequestedFrameRateArg,
            RequestedFrameRateNumerator = frameRateProjection.RequestedFrameRateNumerator,
            RequestedFrameRateDenominator = frameRateProjection.RequestedFrameRateDenominator,
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

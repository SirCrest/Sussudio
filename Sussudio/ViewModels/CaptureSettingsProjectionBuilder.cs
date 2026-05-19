using System;
using Sussudio.Models;

namespace Sussudio.ViewModels;

internal static partial class CaptureSettingsProjectionBuilder
{
    public static CaptureSettings Build(CaptureSettingsProjectionInput input)
    {
        var frameRateProjection = ProjectFrameRate(input);
        var settings = new CaptureSettings
        {
            Width = input.EffectiveResolutionKnown ? input.EffectiveWidth : (input.SelectedFormat?.Width ?? 1920),
            Height = input.EffectiveResolutionKnown ? input.EffectiveHeight : (input.SelectedFormat?.Height ?? 1080),
            FrameRate = frameRateProjection.EffectiveFrameRate,
            RequestedFrameRateArg = frameRateProjection.RequestedFrameRateArg,
            RequestedFrameRateNumerator = frameRateProjection.RequestedFrameRateNumerator,
            RequestedFrameRateDenominator = frameRateProjection.RequestedFrameRateDenominator,
            RequestedPixelFormat = ResolveRequestedPixelFormat(input),
            ForceMjpegDecode = ShouldForceMjpegDecode(input),
            FlashbackGpuDecode = input.FlashbackGpuDecode,
            FlashbackBufferMinutes = input.FlashbackBufferMinutes,
            Format = RecordingSettingsSelectionPolicy.ParseRecordingFormat(input.SelectedRecordingFormat),
            Quality = RecordingSettingsSelectionPolicy.ParseVideoQuality(input.SelectedQuality),
            NvencPreset = NvencPresetParser.Parse(input.SelectedPreset),
            SplitEncodeMode = SplitEncodeModeParser.Parse(input.SelectedSplitEncodeMode),
            CustomBitrateMbps = input.CustomBitrateMbps,
            HdrEnabled = input.IsHdrEnabled,
            HdrOutputMode = input.IsHdrEnabled ? HdrOutputMode.Hdr10Pq : HdrOutputMode.Off,
            PreviewMode = input.IsTrueHdrPreviewEnabled ? PreviewMode.TrueHdr : PreviewMode.GpuFast,
            OutputPath = input.OutputPath,
            AudioEnabled = input.IsAudioEnabled,
            MjpegDecoderCount = Math.Clamp(input.MjpegDecoderCount, 1, 8)
        };

        settings.UseCustomAudioInput = input.IsCustomAudioInputEnabled;
        if (input.IsCustomAudioInputEnabled && input.SelectedAudioInputDeviceId != null)
        {
            settings.AudioDeviceId = input.SelectedAudioInputDeviceId;
            settings.AudioDeviceName = input.SelectedAudioInputDeviceName;
        }

        settings.MicrophoneEnabled = input.IsMicrophoneEnabled;
        if (input.IsMicrophoneEnabled && input.SelectedMicrophoneDeviceId != null)
        {
            settings.MicrophoneDeviceId = input.SelectedMicrophoneDeviceId;
            settings.MicrophoneDeviceName = input.SelectedMicrophoneDeviceName;
        }

        return settings;
    }

}

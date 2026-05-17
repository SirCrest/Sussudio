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
        var effectiveResolutionKnown = TryGetEffectiveResolutionSelection(out _, out var effectiveWidth, out var effectiveHeight);
        var runtime = _captureService.GetRuntimeSnapshot();
        var sourceTelemetry = _captureService.GetLatestSourceTelemetrySnapshot();
        return CaptureSettingsProjectionBuilder.Build(new CaptureSettingsProjectionInput
        {
            EffectiveResolutionKnown = effectiveResolutionKnown,
            EffectiveWidth = effectiveWidth,
            EffectiveHeight = effectiveHeight,
            SelectedResolution = SelectedResolution,
            SelectedFrameRate = SelectedFrameRate,
            AutoResolvedFrameRate = AutoResolvedFrameRate,
            IsAutoResolutionSelected = IsAutoResolutionValue(SelectedResolution),
            SelectedFormat = SelectedFormat,
            AvailableFrameRates = AvailableFrameRates.ToArray(),
            Runtime = runtime,
            SourceTelemetry = sourceTelemetry,
            SelectedVideoFormat = SelectedVideoFormat,
            IsHdrEnabled = IsHdrEnabled,
            IsTrueHdrPreviewEnabled = IsTrueHdrPreviewEnabled,
            MjpegDecoderCount = MjpegDecoderCount,
            SelectedRecordingFormat = SelectedRecordingFormat,
            SelectedQuality = SelectedQuality,
            SelectedPreset = SelectedPreset,
            SelectedSplitEncodeMode = SelectedSplitEncodeMode,
            CustomBitrateMbps = CustomBitrateMbps,
            OutputPath = OutputPath,
            FlashbackGpuDecode = FlashbackGpuDecode,
            FlashbackBufferMinutes = FlashbackBufferMinutes,
            IsAudioEnabled = IsAudioEnabled,
            IsCustomAudioInputEnabled = IsCustomAudioInputEnabled,
            SelectedAudioInputDeviceId = SelectedAudioInputDevice?.Id,
            SelectedAudioInputDeviceName = SelectedAudioInputDevice?.Name,
            IsMicrophoneEnabled = IsMicrophoneEnabled,
            SelectedMicrophoneDeviceId = SelectedMicrophoneDevice?.Id,
            SelectedMicrophoneDeviceName = SelectedMicrophoneDevice?.Name
        });
    }
}

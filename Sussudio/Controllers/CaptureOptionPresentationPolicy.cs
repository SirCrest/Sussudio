using System;

namespace Sussudio.Controllers;

internal static class CaptureOptionPresentationPolicy
{
    internal static CaptureOptionPresentationAffordances Build(CaptureOptionPresentationInput input)
    {
        var selectedFrameRate = ResolveSelectedFrameRate(input);
        var showCustomBitrate = input.IsCustomBitrateVisible;

        return new CaptureOptionPresentationAffordances(
            InitialDecoderCount: Math.Clamp(input.MjpegDecoderCount, 1, 8),
            ShowDecoderCount: ShouldShowDecoderCount(input.SelectedVideoFormat, input.SelectedFormatPixelFormat, selectedFrameRate),
            EnableHdrToggle: input.IsHdrAvailable && !input.IsRecording && input.SourceIsHdr != false,
            EnableTrueHdrPreviewToggle: input.IsHdrEnabled && !input.IsRecording,
            ShowCustomBitrate: showCustomBitrate,
            ShowPreset: !showCustomBitrate,
            ShowAudioClip: input.AudioClipping);
    }

    private static double ResolveSelectedFrameRate(CaptureOptionPresentationInput input)
    {
        if (input.SelectedFrameRateOptionFriendlyValue is > 0)
        {
            return input.SelectedFrameRateOptionFriendlyValue.Value;
        }

        if (input.SelectedFrameRateOptionValue is > 0)
        {
            return input.SelectedFrameRateOptionValue.Value;
        }

        return input.SelectedFrameRateFallback;
    }

    private static bool ShouldShowDecoderCount(string? selectedVideoFormat, string? selectedFormatPixelFormat, double selectedFrameRate)
    {
        // Show decoder count when MJPG is explicitly selected, or when Auto resolves
        // to a device-native MJPG format at high frame rates.
        var isExplicitMjpg = string.Equals(selectedVideoFormat, "MJPG", StringComparison.OrdinalIgnoreCase);
        var isAutoWithMjpgDevice = string.Equals(selectedVideoFormat, "Auto", StringComparison.OrdinalIgnoreCase) &&
                                   string.Equals(selectedFormatPixelFormat, "MJPG", StringComparison.OrdinalIgnoreCase);

        return (isExplicitMjpg || isAutoWithMjpgDevice) && selectedFrameRate >= 90;
    }
}

internal readonly record struct CaptureOptionPresentationInput(
    string? SelectedVideoFormat,
    string? SelectedFormatPixelFormat,
    double? SelectedFrameRateOptionFriendlyValue,
    double? SelectedFrameRateOptionValue,
    double SelectedFrameRateFallback,
    int MjpegDecoderCount,
    bool IsHdrAvailable,
    bool IsRecording,
    bool? SourceIsHdr,
    bool IsHdrEnabled,
    bool IsCustomBitrateVisible,
    bool AudioClipping);

internal readonly record struct CaptureOptionPresentationAffordances(
    int InitialDecoderCount,
    bool ShowDecoderCount,
    bool EnableHdrToggle,
    bool EnableTrueHdrPreviewToggle,
    bool ShowCustomBitrate,
    bool ShowPreset,
    bool ShowAudioClip);

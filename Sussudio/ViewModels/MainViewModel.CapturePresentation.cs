using System;
using Sussudio.Models;

namespace Sussudio.ViewModels;

/// <summary>
/// Capture presentation adapters that apply runtime/source state to ViewModel labels.
/// </summary>
public partial class MainViewModel
{
    private void UpdateLiveCaptureInfo(CaptureRuntimeSnapshot? runtimeSnapshot = null)
    {
        var runtime = runtimeSnapshot ?? _captureService.GetRuntimeSnapshot();
        IsAudioPreviewActive = runtime.IsAudioPreviewActive;

        var liveSignalText = LiveSignalTextPresentationBuilder.Build(
            runtime,
            _captureService.EncoderCodecName,
            LiveInfoUnavailable);
        LiveResolution = liveSignalText.Resolution;
        LiveFrameRate = liveSignalText.FrameRate;
        LivePixelFormat = liveSignalText.PixelFormat;
    }

    private void ResetLiveCaptureInfo()
    {
        IsAudioPreviewActive = false;
        LiveResolution = LiveInfoUnavailable;
        LiveFrameRate = LiveInfoUnavailable;
        LivePixelFormat = LiveInfoUnavailable;
    }

    partial void OnIsPreviewingChanged(bool value)
    {
        if (!value && !IsRecording)
        {
            ResetLiveCaptureInfo();
        }
    }

    private void UpdateHdrRuntimeStatusFromCapture(CaptureRuntimeSnapshot? runtimeSnapshot = null)
    {
        var runtime = runtimeSnapshot ?? _captureService.GetRuntimeSnapshot();
        HdrRuntimeState = runtime.HdrRuntimeState;
        HdrReadinessReason = runtime.HdrReadinessReason;
        UpdateTargetSummary();
    }

    private void UpdateTargetSummary()
    {
        SourceTargetSummaryText = SourceTelemetryPresentationBuilder.BuildTargetSummary(
            GetSelectedResolutionDisplayText(),
            SelectedFrameRate,
            SelectedFriendlyFrameRate,
            SelectedExactFrameRate,
            SelectedExactFrameRateArg,
            HdrRuntimeState);
    }

    private string GetSelectedResolutionDisplayText()
    {
        if (string.IsNullOrWhiteSpace(SelectedResolution))
        {
            return "?";
        }

        if (!IsAutoResolutionValue(SelectedResolution))
        {
            return SelectedResolution;
        }

        var friendlyRate = SelectedFriendlyFrameRate
            ?? (AutoResolvedFrameRate.HasValue
                ? Math.Round(AutoResolvedFrameRate.Value, MidpointRounding.AwayFromZero)
                : (double?)null);
        if (AutoResolvedWidth.HasValue &&
            AutoResolvedHeight.HasValue &&
            friendlyRate.HasValue)
        {
            return $"{AutoResolutionValue} ({GetResolutionKey(AutoResolvedWidth.Value, AutoResolvedHeight.Value)} @ {friendlyRate.Value:0} fps)";
        }

        return AutoResolutionValue;
    }
}

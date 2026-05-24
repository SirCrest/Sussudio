using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.Services.Gpu;

namespace Sussudio.ViewModels;

/// <summary>
/// Automation-facing capture, health, recording, options, and probe snapshot entry points.
/// </summary>
public partial class MainViewModel
{
    public CaptureRuntimeSnapshot GetCaptureRuntimeSnapshot() => _captureService.GetRuntimeSnapshot();
    public CaptureHealthSnapshot GetCaptureHealthSnapshot() => _captureService.GetHealthSnapshot();
    public CaptureDiagnosticsSnapshot GetCaptureDiagnosticsSnapshot() => _captureService.GetDiagnosticsSnapshot();
    public RecordingStats GetRecordingStatsSnapshot() => _captureService.GetRecordingStats();
    internal ParallelMjpegDecodePipeline.PipelineTimingMetrics? GetMjpegPipelineTimingDetails()
        => _captureService.GetMjpegPipelineTimingDetails();
    public VideoSourceProbeResult ProbeVideoSource() => _captureService.ProbeVideoSource();
    public PreviewColorProbeResult ProbePreviewColor() => _captureService.ProbePreviewColor();
    public Task<CaptureRuntimeSnapshot> GetCaptureRuntimeSnapshotAsync(CancellationToken cancellationToken = default)
        => FromSynchronousSnapshot(_captureService.GetRuntimeSnapshot, cancellationToken);
    public Task<CaptureHealthSnapshot> GetCaptureHealthSnapshotAsync(CancellationToken cancellationToken = default)
        => FromSynchronousSnapshot(_captureService.GetHealthSnapshot, cancellationToken);
    public Task<RecordingStats> GetRecordingStatsSnapshotAsync(CancellationToken cancellationToken = default)
        => FromSynchronousSnapshot(_captureService.GetRecordingStats, cancellationToken);
    public Task<VideoSourceProbeResult> ProbeVideoSourceAsync(CancellationToken cancellationToken = default)
        => FromSynchronousSnapshot(ProbeVideoSource, cancellationToken);
    public Task<PreviewColorProbeResult> ProbePreviewColorAsync(CancellationToken cancellationToken = default)
        => FromSynchronousSnapshot(ProbePreviewColor, cancellationToken);
    public Task<PreviewFrameCaptureResult> CapturePreviewFrameAsync(string outputPath, CancellationToken cancellationToken = default)
        => _captureService.CapturePreviewFrameAsync(outputPath, cancellationToken);

    public Task<ViewModelRuntimeSnapshot> GetViewModelRuntimeSnapshotAsync(CancellationToken cancellationToken = default)
    {
        var sessionSnapshot = _sessionCoordinator.Snapshot;
        return InvokeOnUiThreadAsync(() =>
        {
            var input = new ViewModelRuntimeSnapshotInput
            {
                TimestampUtc = DateTimeOffset.UtcNow,
                SessionSnapshot = sessionSnapshot,
                IsInitialized = IsInitialized,
                IsPreviewing = IsPreviewing,
                IsRecording = IsRecording,
                IsAudioEnabled = IsAudioEnabled,
                IsAudioPreviewEnabled = IsAudioPreviewEnabled,
                IsCustomAudioInputEnabled = IsCustomAudioInputEnabled,
                StatusText = StatusText,
                SelectedDeviceId = SelectedDevice?.Id,
                SelectedDeviceName = SelectedDevice?.Name,
                SelectedAudioInputDeviceId = SelectedAudioInputDevice?.Id,
                SelectedAudioInputDeviceName = SelectedAudioInputDevice?.Name,
                SelectedResolution = SelectedResolution,
                SelectedFrameRate = SelectedFrameRate,
                SelectedFriendlyFrameRate = SelectedFriendlyFrameRate,
                SelectedExactFrameRate = SelectedExactFrameRate,
                SelectedExactFrameRateArg = SelectedExactFrameRateArg,
                DisabledResolutionReason = DisabledResolutionReason,
                DisabledFrameRateReason = DisabledFrameRateReason,
                HdrResolutionSupportHint = HdrResolutionSupportHint,
                DetectedSourceFrameRate = DetectedSourceFrameRate,
                DetectedSourceFrameRateArg = DetectedSourceFrameRateArg,
                SourceFrameRateOrigin = SourceFrameRateOrigin,
                SourceWidth = SourceWidth,
                SourceHeight = SourceHeight,
                SourceIsHdr = SourceIsHdr,
                SourceTelemetryAvailability = SourceTelemetryAvailability,
                SourceTelemetryOriginDetail = SourceTelemetryOriginDetail,
                SourceTelemetryConfidence = SourceTelemetryConfidence,
                SourceTelemetryDiagnosticSummary = SourceTelemetryDiagnosticSummary,
                SourceTelemetryTimestampUtc = SourceTelemetryTimestampUtc,
                SourceTelemetrySummaryText = SourceTelemetrySummaryText,
                SourceTargetSummaryText = SourceTargetSummaryText,
                SelectedRecordingFormat = SelectedRecordingFormat,
                SelectedQuality = SelectedQuality,
                SelectedPreset = SelectedPreset,
                SelectedSplitEncodeMode = SelectedSplitEncodeMode,
                SelectedVideoFormat = SelectedVideoFormat,
                CustomBitrateMbps = CustomBitrateMbps,
                PreviewVolume = PreviewVolume,
                IsStatsVisible = IsStatsVisible,
                IsHdrAvailable = IsHdrAvailable,
                IsHdrEnabled = IsHdrEnabled,
                HdrRuntimeState = HdrRuntimeState,
                HdrReadinessReason = HdrReadinessReason,
                LiveResolution = LiveResolution,
                LiveFrameRate = LiveFrameRate,
                LivePixelFormat = LivePixelFormat,
                OutputPath = OutputPath,
                RecordingTime = RecordingTime,
                RecordingSizeInfo = RecordingSizeInfo,
                RecordingBitrateInfo = RecordingBitrateInfo,
                AudioPeak = AudioPeak,
                AudioClipping = AudioClipping
            };

            return ViewModelRuntimeSnapshotBuilder.Build(input);
        }, cancellationToken);
    }

    public Task<AutomationOptionsSnapshot> GetAutomationOptionsSnapshotAsync(CancellationToken cancellationToken = default)
    {
        return InvokeOnUiThreadAsync(() =>
        {
            var selectedFrameRate = SelectedFrameRate;
            var input = new AutomationOptionsSnapshotInput
            {
                TimestampUtc = DateTimeOffset.UtcNow,
                Devices = Devices
                    .Select(device => new AutomationOptionsDeviceInput
                    {
                        Id = device.Id,
                        Name = device.Name
                    })
                    .ToArray(),
                AudioInputDevices = AudioInputDevices
                    .Select(device => new AutomationOptionsDeviceInput
                    {
                        Id = device.Id,
                        Name = device.Name
                    })
                    .ToArray(),
                Resolutions = AvailableResolutions
                    .Select(option => new AutomationOptionsResolutionInput
                    {
                        Value = option.Value,
                        Width = option.Width,
                        Height = option.Height,
                        IsEnabled = option.IsEnabled,
                        DisableReason = option.DisableReason
                    })
                    .ToArray(),
                FrameRates = AvailableFrameRates
                    .Select(option => new AutomationOptionsFrameRateInput
                    {
                        Value = option.Value,
                        FriendlyValue = option.FriendlyValue,
                        ExactValueArg = option.Rational,
                        IsEnabled = option.IsEnabled,
                        DisableReason = option.DisableReason,
                        IsSelected = FrameRateTimingPolicy.IsFrameRateMatch(option.Value, selectedFrameRate)
                    })
                    .ToArray(),
                RecordingFormats = AvailableRecordingFormats.ToArray(),
                Qualities = AvailableQualities.ToArray(),
                Presets = AvailablePresets.ToArray(),
                SplitEncodeModes = AvailableSplitEncodeModes.ToArray(),
                VideoFormats = AvailableVideoFormats.ToArray(),
                SelectedDeviceId = SelectedDevice?.Id,
                SelectedAudioInputDeviceId = SelectedAudioInputDevice?.Id,
                SelectedResolution = SelectedResolution,
                SelectedFrameRate = selectedFrameRate,
                SelectedRecordingFormat = SelectedRecordingFormat,
                SelectedQuality = SelectedQuality,
                SelectedPreset = SelectedPreset,
                SelectedSplitEncodeMode = SelectedSplitEncodeMode,
                SelectedVideoFormat = SelectedVideoFormat,
                MjpegDecoderCount = MjpegDecoderCount,
                PreviewVolume = PreviewVolume,
                IsStatsVisible = IsStatsVisible
            };

            return AutomationOptionsSnapshotBuilder.Build(input);
        }, cancellationToken);
    }

    private static Task<T> FromSynchronousSnapshot<T>(Func<T> snapshotFactory, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<T>(cancellationToken);
        }

        return Task.FromResult(snapshotFactory());
    }
    public CaptureSettings BuildCurrentSettings() => BuildCaptureSettings();

}

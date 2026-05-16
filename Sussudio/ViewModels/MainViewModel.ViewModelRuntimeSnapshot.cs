using System;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;

namespace Sussudio.ViewModels;

/// <summary>
/// Automation-facing view-model runtime snapshot UI-thread capture.
/// </summary>
public partial class MainViewModel
{
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
                ShowAllCaptureOptions = ShowAllCaptureOptions,
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
}

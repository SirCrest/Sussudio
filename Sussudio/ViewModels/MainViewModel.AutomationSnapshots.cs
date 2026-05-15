using System;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.Services.Gpu;
using Sussudio.Services.Runtime;

namespace Sussudio.ViewModels;

/// <summary>
/// Automation-facing runtime, health, recording, view-model, and probe snapshots.
/// </summary>
public partial class MainViewModel
{
    public CaptureRuntimeSnapshot GetCaptureRuntimeSnapshot() => _captureService.GetRuntimeSnapshot();
    public CaptureHealthSnapshot GetCaptureHealthSnapshot() => _captureService.GetHealthSnapshot();
    public CaptureDiagnosticsSnapshot GetCaptureDiagnosticsSnapshot() => _captureService.GetDiagnosticsSnapshot();
    public RecordingStats GetRecordingStatsSnapshot() => _captureService.GetRecordingStats();
    internal ParallelMjpegDecodePipeline.PipelineTimingMetrics? GetMjpegPipelineTimingDetails()
        => _captureService.GetMjpegPipelineTimingDetails();
    public Task<CaptureRuntimeSnapshot> GetCaptureRuntimeSnapshotAsync(CancellationToken cancellationToken = default)
        => FromSynchronousSnapshot(_captureService.GetRuntimeSnapshot, cancellationToken);
    public Task<CaptureHealthSnapshot> GetCaptureHealthSnapshotAsync(CancellationToken cancellationToken = default)
        => FromSynchronousSnapshot(_captureService.GetHealthSnapshot, cancellationToken);
    public Task<RecordingStats> GetRecordingStatsSnapshotAsync(CancellationToken cancellationToken = default)
        => FromSynchronousSnapshot(_captureService.GetRecordingStats, cancellationToken);
    public VideoSourceProbeResult ProbeVideoSource() => _captureService.ProbeVideoSource();
    public PreviewColorProbeResult ProbePreviewColor() => _captureService.ProbePreviewColor();

    private static Task<T> FromSynchronousSnapshot<T>(Func<T> snapshotFactory, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<T>(cancellationToken);
        }

        return Task.FromResult(snapshotFactory());
    }

    public Task<VideoSourceProbeResult> ProbeVideoSourceAsync(CancellationToken cancellationToken = default)
        => FromSynchronousSnapshot(ProbeVideoSource, cancellationToken);

    public Task<PreviewColorProbeResult> ProbePreviewColorAsync(CancellationToken cancellationToken = default)
        => FromSynchronousSnapshot(ProbePreviewColor, cancellationToken);

    public Task<PreviewFrameCaptureResult> CapturePreviewFrameAsync(string outputPath, CancellationToken cancellationToken = default) => _captureService.CapturePreviewFrameAsync(outputPath, cancellationToken);
    public CaptureSettings BuildCurrentSettings() => BuildCaptureSettings();

    public Task<ViewModelRuntimeSnapshot> GetViewModelRuntimeSnapshotAsync(CancellationToken cancellationToken = default)
    {
        var sessionSnapshot = _sessionCoordinator.Snapshot;
        return InvokeOnUiThreadAsync(() => new ViewModelRuntimeSnapshot
        {
            TimestampUtc = DateTimeOffset.UtcNow,
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
            SourceTelemetryAgeSeconds = TelemetryAgeHelper.ComputeAgeSeconds(SourceTelemetryTimestampUtc, DateTimeOffset.UtcNow),
            SourceTelemetrySummaryText = SourceTelemetrySummaryText,
            SourceTargetSummaryText = SourceTargetSummaryText,
            CaptureCommandCommandsEnqueued = sessionSnapshot.CommandsEnqueued,
            CaptureCommandCommandsCompleted = sessionSnapshot.CommandsCompleted,
            CaptureCommandCommandsFailed = sessionSnapshot.CommandsFailed,
            CaptureCommandCommandsCanceled = sessionSnapshot.CommandsCanceled,
            CaptureCommandCommandsCoalesced = sessionSnapshot.CommandsCoalesced,
            CaptureCommandPendingCommands = sessionSnapshot.PendingCommands,
            CaptureCommandMaxPendingCommands = sessionSnapshot.MaxPendingCommands,
            CaptureCommandOldestPendingCommandAgeMs = sessionSnapshot.OldestPendingCommandAgeMs,
            CaptureCommandLastQueueLatencyMs = sessionSnapshot.LastCommandQueueLatencyMs,
            CaptureCommandMaxQueueLatencyMs = sessionSnapshot.MaxCommandQueueLatencyMs,
            CaptureCommandLastCommand = sessionSnapshot.LastCommand?.ToString() ?? "None",
            CaptureCommandLastOutcome = sessionSnapshot.LastOutcome.ToString(),
            CaptureCommandLastCorrelationId = sessionSnapshot.LastCorrelationId ?? string.Empty,
            CaptureCommandLastError = sessionSnapshot.LastError ?? string.Empty,
            SelectedRecordingFormat = SelectedRecordingFormat,
            SelectedQuality = SelectedQuality,
            SelectedPreset = SelectedPreset,
            SelectedSplitEncodeMode = SelectedSplitEncodeMode,
            SelectedVideoFormat = SelectedVideoFormat,
            CustomBitrateMbps = CustomBitrateMbps,
            ShowAllCaptureOptions = ShowAllCaptureOptions,
            PreviewVolumePercent = PreviewVolume * 100.0,
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
        }, cancellationToken);
    }

}

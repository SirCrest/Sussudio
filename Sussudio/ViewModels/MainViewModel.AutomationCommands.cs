using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.Services.Gpu;

namespace Sussudio.ViewModels;

/// <summary>
/// Stable public automation facade for commands, snapshots, probes, and options.
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

    public Task RefreshDevicesForAutomationAsync(CancellationToken cancellationToken = default)
        => InvokeOnUiThreadAsync(() => RefreshDevicesAsync(cancellationToken), cancellationToken);

    public Task SelectDeviceAsync(string? deviceId, string? deviceName, CancellationToken cancellationToken = default)
    {
        return InvokeOnUiThreadAsync(async () =>
        {
            var target = ResolveDevice(deviceId, deviceName);
            if (target == null)
            {
                throw new InvalidOperationException($"Capture device not found. Id='{deviceId ?? "(null)"}', Name='{deviceName ?? "(null)"}'.");
            }

            await ApplySelectedDeviceAsync(target, cancellationToken).ConfigureAwait(true);
        }, cancellationToken);
    }

    public Task SelectAudioInputDeviceAsync(string? deviceId, string? deviceName, CancellationToken cancellationToken = default)
    {
        return InvokeOnUiThreadAsync(() =>
        {
            var target = ResolveAudioDevice(deviceId, deviceName);
            if (target == null)
            {
                throw new InvalidOperationException($"Audio input device not found. Id='{deviceId ?? "(null)"}', Name='{deviceName ?? "(null)"}'.");
            }

            SelectedAudioInputDevice = target;
            return Task.CompletedTask;
        }, cancellationToken);
    }

    public Task SetCustomAudioInputEnabledAsync(bool enabled, CancellationToken cancellationToken = default)
    {
        return InvokeOnUiThreadAsync(() =>
        {
            if (IsRecording)
            {
                throw new InvalidOperationException("Custom audio input cannot be changed while recording.");
            }

            IsCustomAudioInputEnabled = enabled;
            return Task.CompletedTask;
        }, cancellationToken);
    }

    public Task SetAudioEnabledAsync(bool enabled, CancellationToken cancellationToken = default)
    {
        return InvokeOnUiThreadAsync(() =>
        {
            IsAudioEnabled = enabled;
            return Task.CompletedTask;
        }, cancellationToken);
    }

    public Task SetAudioPreviewEnabledAsync(bool enabled, CancellationToken cancellationToken = default)
    {
        return InvokeOnUiThreadAsync(() =>
        {
            IsAudioPreviewEnabled = enabled;
            return Task.CompletedTask;
        }, cancellationToken);
    }

    public Task SetPreviewVolumeAsync(double previewVolumePercent, CancellationToken cancellationToken = default)
    {
        return InvokeOnUiThreadAsync(() =>
        {
            PreviewVolume = Math.Clamp(previewVolumePercent / 100.0, 0.0, 1.0);
            SavePreviewVolume();
            return Task.CompletedTask;
        }, cancellationToken);
    }

    public Task SetDeviceAudioModeAsync(string mode, CancellationToken cancellationToken = default)
    {
        return InvokeOnUiThreadAsync(async () =>
        {
            var normalizedMode = NormalizeDeviceAudioMode(mode);
            WithAudioControlRefreshSuppressed(() => SelectedDeviceAudioMode = normalizedMode);
            var applied = await ApplyDeviceAudioModeAsync(
                "automation device audio mode",
                normalizedMode,
                cancellationToken: cancellationToken).ConfigureAwait(false);
            if (!applied)
            {
                throw new InvalidOperationException($"Device audio mode change failed ({normalizedMode}).");
            }
        }, cancellationToken);
    }

    public Task SetAnalogAudioGainAsync(double gainPercent, CancellationToken cancellationToken = default)
    {
        return InvokeOnUiThreadAsync(async () =>
        {
            var clampedGain = Math.Clamp(gainPercent, 0.0, 100.0);
            WithAudioControlRefreshSuppressed(() => AnalogAudioGainPercent = clampedGain);
            var applied = await ApplyAnalogAudioGainAsync(
                "automation analog audio gain",
                clampedGain,
                cancellationToken: cancellationToken).ConfigureAwait(false);
            if (!applied)
            {
                throw new InvalidOperationException($"Analog audio gain change failed ({clampedGain:0}%).");
            }
        }, cancellationToken);
    }

    public Task SetMicrophoneEnabledAsync(bool enabled, CancellationToken cancellationToken = default)
    {
        return SetMicrophoneEnabledAutomationAsync(enabled, cancellationToken);
    }

    public Task SetResolutionAsync(string resolution, CancellationToken cancellationToken = default)
        => _captureSettingsAutomationController.SetResolutionAsync(resolution, cancellationToken);

    public Task SetFrameRateAsync(double frameRate, CancellationToken cancellationToken = default)
        => _captureSettingsAutomationController.SetFrameRateAsync(frameRate, cancellationToken);

    public Task SetVideoFormatAsync(string videoFormat, CancellationToken cancellationToken = default)
        => _captureSettingsAutomationController.SetVideoFormatAsync(videoFormat, cancellationToken);

    public Task SetMjpegDecoderCountAsync(int decoderCount, CancellationToken cancellationToken = default)
        => _captureSettingsAutomationController.SetMjpegDecoderCountAsync(decoderCount, cancellationToken);

    public Task SetRecordingFormatAsync(string format, CancellationToken cancellationToken = default)
        => _recordingSettingsAutomationController.SetRecordingFormatAsync(format, cancellationToken);

    public Task SetQualityAsync(string quality, CancellationToken cancellationToken = default)
        => _recordingSettingsAutomationController.SetQualityAsync(quality, cancellationToken);

    public Task SetSplitEncodeModeAsync(string splitEncodeMode, CancellationToken cancellationToken = default)
        => _recordingSettingsAutomationController.SetSplitEncodeModeAsync(splitEncodeMode, cancellationToken);

    public Task SetCustomBitrateAsync(double bitrateMbps, CancellationToken cancellationToken = default)
        => _recordingSettingsAutomationController.SetCustomBitrateAsync(bitrateMbps, cancellationToken);

    public Task SetPresetAsync(string preset, CancellationToken cancellationToken = default)
        => _recordingSettingsAutomationController.SetPresetAsync(preset, cancellationToken);

    public Task SetOutputPathAsync(string outputPath, CancellationToken cancellationToken = default)
        => _recordingSettingsAutomationController.SetOutputPathAsync(outputPath, cancellationToken);

    private CaptureDevice? ResolveDevice(string? deviceId, string? deviceName)
    {
        if (!string.IsNullOrWhiteSpace(deviceId))
        {
            var byId = Devices.FirstOrDefault(d => string.Equals(d.Id, deviceId, StringComparison.OrdinalIgnoreCase));
            if (byId != null)
            {
                return byId;
            }
        }

        if (!string.IsNullOrWhiteSpace(deviceName))
        {
            return Devices.FirstOrDefault(d => string.Equals(d.Name, deviceName, StringComparison.OrdinalIgnoreCase));
        }

        return null;
    }

    private AudioInputDevice? ResolveAudioDevice(string? deviceId, string? deviceName)
    {
        if (!string.IsNullOrWhiteSpace(deviceId))
        {
            var byId = AudioInputDevices.FirstOrDefault(d => string.Equals(d.Id, deviceId, StringComparison.OrdinalIgnoreCase));
            if (byId != null)
            {
                return byId;
            }
        }

        if (!string.IsNullOrWhiteSpace(deviceName))
        {
            return AudioInputDevices.FirstOrDefault(d => string.Equals(d.Name, deviceName, StringComparison.OrdinalIgnoreCase));
        }

        return null;
    }

    private async Task SetMicrophoneEnabledAutomationAsync(bool enabled, CancellationToken cancellationToken)
    {
        var request = await InvokeOnUiThreadAsync(
            () => (
                IsRecording,
                CurrentMicEnabled: IsMicrophoneEnabled,
                DeviceId: SelectedMicrophoneDevice?.Id,
                DeviceName: SelectedMicrophoneDevice?.Name),
            cancellationToken).ConfigureAwait(false);

        if (request.IsRecording)
        {
            if (enabled == request.CurrentMicEnabled)
            {
                // Idempotent reassertion during recording: automation clients often
                // re-issue desired state. The mic wiring is already where the caller
                // wants it, so succeed as a no-op rather than throwing.
                Logger.Log($"MIC_TOGGLE_NOOP reason=recording_active_idempotent requested={enabled}");
                return;
            }

            // Real state transition while recording: refuse. UpdateMicrophoneMonitorAsync
            // cannot rewire the device mid-recording, so setting IsMicrophoneEnabled
            // here would leave UI state lying about the actual device wiring.
            Logger.Log($"MIC_TOGGLE_REFUSED reason=recording_active requested={enabled} current={request.CurrentMicEnabled}");
            throw new InvalidOperationException(
                "Cannot change microphone enable state while recording. Stop the recording first.");
        }

        await _sessionCoordinator.UpdateMicrophoneMonitorAsync(
            enabled,
            request.DeviceId,
            request.DeviceName,
            cancellationToken).ConfigureAwait(false);

        await InvokeOnUiThreadAsync(
            () =>
            {
                _suppressMicrophoneMonitorUpdate = true;
                try
                {
                    IsMicrophoneEnabled = enabled;
                }
                finally
                {
                    _suppressMicrophoneMonitorUpdate = false;
                }

                return true;
            },
            cancellationToken).ConfigureAwait(false);
    }
}

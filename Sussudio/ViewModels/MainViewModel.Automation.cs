using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;

namespace Sussudio.ViewModels;

/// <summary>
/// Automation mutators.
/// Implements IAutomationViewModel members consumed by the automation layer.
/// </summary>
public partial class MainViewModel
{
    public Task SetFlashbackEnabledAsync(bool enabled, CancellationToken cancellationToken = default)
        => _sessionCoordinator.SetFlashbackEnabledAsync(enabled, cancellationToken);

    public async Task RestartFlashbackAsync(CancellationToken cancellationToken = default)
    {
        var settings = await InvokeOnUiThreadAsync(BuildCaptureSettings, cancellationToken).ConfigureAwait(false);
        await _sessionCoordinator.RestartFlashbackAsync(settings, cancellationToken).ConfigureAwait(false);
        await InvokeOnUiThreadAsync(
            () =>
            {
                _flashbackBitrateSamples.Clear();
                return true;
            },
            cancellationToken).ConfigureAwait(false);
    }

    // ── Automation set methods (IAutomationViewModel) ────────────────────

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

    public Task SetResolutionAsync(string resolution, CancellationToken cancellationToken = default)
    {
        return SetAutomationCaptureModeAsync("resolution", () =>
        {
            var matched = AvailableResolutions.FirstOrDefault(r =>
                string.Equals(r.Value, resolution, StringComparison.OrdinalIgnoreCase));
            if (matched == null)
            {
                throw new InvalidOperationException($"Resolution '{resolution}' is not available.");
            }
            if (!matched.IsEnabled)
            {
                throw new InvalidOperationException(
                    string.IsNullOrWhiteSpace(matched.DisableReason)
                        ? $"Resolution '{resolution}' is currently disabled."
                        : matched.DisableReason);
            }

            SelectedResolution = matched.Value;
        }, cancellationToken);
    }

    public Task SetFrameRateAsync(double frameRate, CancellationToken cancellationToken = default)
    {
        return SetAutomationCaptureModeAsync("frame rate", () =>
        {
            if (AvailableFrameRates.Count == 0)
            {
                throw new InvalidOperationException("No frame rates are available.");
            }

            var enabledRates = AvailableFrameRates
                .Where(rate => rate.IsEnabled)
                .ToList();
            if (enabledRates.Count == 0)
            {
                throw new InvalidOperationException("No enabled frame rates are available for the current selection.");
            }

            if (IsAutoFrameRateValue(frameRate))
            {
                var autoRate = enabledRates.FirstOrDefault(rate => IsAutoFrameRateValue(rate.Value));
                if (autoRate == null)
                {
                    throw new InvalidOperationException("Auto frame rate is not available for the current selection.");
                }

                SelectAutoFrameRate();
                return;
            }

            var requestedFriendly = Math.Round(frameRate);
            var friendlyMatches = enabledRates
                .Where(rate => Math.Round(rate.FriendlyValue) == requestedFriendly)
                .OrderBy(rate => Math.Abs(rate.FriendlyValue - frameRate))
                .ThenBy(rate => Math.Abs(rate.Value - frameRate))
                .ToList();

            var matched = (friendlyMatches.Count > 0 ? friendlyMatches : enabledRates)
                .OrderBy(rate => Math.Abs(rate.Value - frameRate))
                .First();

            if (friendlyMatches.Count == 0 && !IsFrameRateMatch(matched.Value, frameRate))
            {
                throw new InvalidOperationException(
                    $"Frame rate '{frameRate:0.###}' is not available for {SelectedResolution ?? "the current resolution"}.");
            }

            SelectedFrameRate = matched.Value;
        }, cancellationToken);
    }

    public Task SetVideoFormatAsync(string videoFormat, CancellationToken cancellationToken = default)
    {
        return SetAutomationCaptureModeAsync("video format", () =>
        {
            if (string.IsNullOrWhiteSpace(videoFormat))
            {
                throw new ArgumentException("Video format is required.", nameof(videoFormat));
            }

            var match = AvailableVideoFormats.FirstOrDefault(
                format => string.Equals(format, videoFormat, StringComparison.OrdinalIgnoreCase));
            if (match == null)
            {
                throw new InvalidOperationException($"Video format '{videoFormat}' is not available.");
            }

            SelectedVideoFormat = match;
        }, cancellationToken);
    }

    public async Task SetRecordingFormatAsync(string format, CancellationToken cancellationToken = default)
    {
        var recordingFormat = await InvokeOnUiThreadAsync(() =>
        {
            var matched = AvailableRecordingFormats.FirstOrDefault(value =>
                string.Equals(value, format, StringComparison.OrdinalIgnoreCase));
            if (matched == null)
            {
                throw new InvalidOperationException($"Recording format '{format}' is not available.");
            }
            if (IsHdrEnabled && !IsHdrCompatibleRecordingFormat(matched))
            {
                throw new InvalidOperationException("HDR recording requires HEVC or AV1 (10-bit).");
            }

            _suppressFlashbackFormatCycle = true;
            try
            {
                SelectedRecordingFormat = matched;
            }
            finally
            {
                _suppressFlashbackFormatCycle = false;
            }

            return matched switch
            {
                "HEVC" => RecordingFormat.HevcMp4,
                "AV1" => RecordingFormat.Av1Mp4,
                _ => RecordingFormat.H264Mp4
            };
        }, cancellationToken).ConfigureAwait(false);

        await _sessionCoordinator.UpdateRecordingFormatAsync(recordingFormat, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task SetPresetAsync(string preset, CancellationToken cancellationToken = default)
    {
        var settings = await InvokeOnUiThreadAsync(() =>
        {
            var matched = AvailablePresets.FirstOrDefault(value =>
                string.Equals(value, preset, StringComparison.OrdinalIgnoreCase));
            if (matched == null)
            {
                throw new InvalidOperationException($"Preset '{preset}' is not available.");
            }

            _suppressFlashbackEncoderSettingsCycle = true;
            try
            {
                SelectedPreset = matched;
            }
            finally
            {
                _suppressFlashbackEncoderSettingsCycle = false;
            }

            return (Quality: ParseVideoQuality(SelectedQuality), Bitrate: CustomBitrateMbps, Preset: SelectedPreset);
        }, cancellationToken).ConfigureAwait(false);

        await _sessionCoordinator.CycleFlashbackEncoderSettingsAsync(
                quality: settings.Quality,
                customBitrateMbps: settings.Bitrate,
                nvencPreset: settings.Preset,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task SetSplitEncodeModeAsync(string splitEncodeMode, CancellationToken cancellationToken = default)
    {
        var settings = await InvokeOnUiThreadAsync(() =>
        {
            var matched = AvailableSplitEncodeModes.FirstOrDefault(value =>
                string.Equals(value, splitEncodeMode, StringComparison.OrdinalIgnoreCase));
            if (matched == null)
            {
                throw new InvalidOperationException($"Split encode mode '{splitEncodeMode}' is not available.");
            }

            _suppressFlashbackEncoderSettingsCycle = true;
            try
            {
                SelectedSplitEncodeMode = matched;
            }
            finally
            {
                _suppressFlashbackEncoderSettingsCycle = false;
            }

            return (Quality: ParseVideoQuality(SelectedQuality), Bitrate: CustomBitrateMbps, Preset: SelectedPreset, SplitEncodeMode: SelectedSplitEncodeMode);
        }, cancellationToken).ConfigureAwait(false);

        await _sessionCoordinator.CycleFlashbackEncoderSettingsAsync(
                quality: settings.Quality,
                customBitrateMbps: settings.Bitrate,
                nvencPreset: settings.Preset,
                splitEncodeMode: settings.SplitEncodeMode,
                cancellationToken)
            .ConfigureAwait(false);
    }

    public Task SetMjpegDecoderCountAsync(int decoderCount, CancellationToken cancellationToken = default)
    {
        return SetAutomationCaptureModeAsync("mjpeg decoder count", () =>
        {
            MjpegDecoderCount = Math.Clamp(decoderCount, 1, 8);
        }, cancellationToken);
    }

    private async Task SetAutomationCaptureModeAsync(
        string reason,
        Action apply,
        CancellationToken cancellationToken)
    {
        await _automationCaptureModeGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var shouldReinitialize = await InvokeOnUiThreadAsync(() =>
            {
                var wasPreviewing = IsPreviewing && IsInitialized && SelectedDevice != null;
                _suppressFormatChangeReinitialize = true;
                try
                {
                    apply();
                }
                finally
                {
                    _suppressFormatChangeReinitialize = false;
                }

                return wasPreviewing && SelectedFormat != null;
            }, cancellationToken).ConfigureAwait(false);

            if (shouldReinitialize)
            {
                await InvokeOnUiThreadAsync(
                        () => ReinitializeDeviceAsync($"automation {reason}"),
                        cancellationToken)
                    .ConfigureAwait(false);
            }
        }
        finally
        {
            _automationCaptureModeGate.Release();
        }
    }

    public Task SetShowAllCaptureOptionsAsync(bool enabled, CancellationToken cancellationToken = default)
    {
        return InvokeOnUiThreadAsync(() =>
        {
            ShowAllCaptureOptions = enabled;
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

    public Action<string, bool>? StatsSectionVisibilityHandler { get; set; }
    public Action<bool>? FrameTimeOverlayVisibilityHandler { get; set; }

    public Task SetStatsSectionVisibleAsync(string section, bool visible, CancellationToken cancellationToken = default)
    {
        return InvokeOnUiThreadAsync(() =>
        {
            StatsSectionVisibilityHandler?.Invoke(section, visible);
            return Task.CompletedTask;
        }, cancellationToken);
    }

    public Task SetStatsVisibleAsync(bool visible, CancellationToken cancellationToken = default)
    {
        return InvokeOnUiThreadAsync(() =>
        {
            IsStatsVisible = visible;
            return Task.CompletedTask;
        }, cancellationToken);
    }

    public Task SetSettingsVisibleAsync(bool visible, CancellationToken cancellationToken = default)
    {
        return InvokeOnUiThreadAsync(() =>
        {
            IsSettingsVisible = visible;
            return Task.CompletedTask;
        }, cancellationToken);
    }

    public Task SetFrameTimeOverlayVisibleAsync(bool visible, CancellationToken cancellationToken = default)
    {
        return InvokeOnUiThreadAsync(() =>
        {
            FrameTimeOverlayVisibilityHandler?.Invoke(visible);
            return Task.CompletedTask;
        }, cancellationToken);
    }

    public Task SetFlashbackTimelineVisibleAsync(bool visible, CancellationToken cancellationToken = default)
    {
        return InvokeOnUiThreadAsync(() =>
        {
            IsFlashbackTimelineVisible = visible;
            return Task.CompletedTask;
        }, cancellationToken);
    }

    public async Task SetQualityAsync(string quality, CancellationToken cancellationToken = default)
    {
        var settings = await InvokeOnUiThreadAsync(() =>
        {
            var matched = AvailableQualities.FirstOrDefault(value =>
                string.Equals(value, quality, StringComparison.OrdinalIgnoreCase));
            if (matched == null)
            {
                throw new InvalidOperationException($"Quality '{quality}' is not available.");
            }

            _suppressFlashbackEncoderSettingsCycle = true;
            try
            {
                SelectedQuality = matched;
            }
            finally
            {
                _suppressFlashbackEncoderSettingsCycle = false;
            }

            return (Quality: ParseVideoQuality(SelectedQuality), Bitrate: CustomBitrateMbps, Preset: SelectedPreset);
        }, cancellationToken).ConfigureAwait(false);

        await _sessionCoordinator.CycleFlashbackEncoderSettingsAsync(
                quality: settings.Quality,
                customBitrateMbps: settings.Bitrate,
                nvencPreset: settings.Preset,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task SetCustomBitrateAsync(double bitrateMbps, CancellationToken cancellationToken = default)
    {
        var settings = await InvokeOnUiThreadAsync(() =>
        {
            _suppressFlashbackEncoderSettingsCycle = true;
            try
            {
                CustomBitrateMbps = Math.Clamp(bitrateMbps, 1, 300);
            }
            finally
            {
                _suppressFlashbackEncoderSettingsCycle = false;
            }

            return (Quality: ParseVideoQuality(SelectedQuality), Bitrate: CustomBitrateMbps, Preset: SelectedPreset);
        }, cancellationToken).ConfigureAwait(false);

        await _sessionCoordinator.CycleFlashbackEncoderSettingsAsync(
                quality: settings.Quality,
                customBitrateMbps: settings.Bitrate,
                nvencPreset: settings.Preset,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    public Task SetHdrEnabledAsync(bool enabled, CancellationToken cancellationToken = default)
    {
        return InvokeOnUiThreadAsync(() =>
        {
            if (IsRecording)
            {
                throw new InvalidOperationException(HdrToggleBlockedWhileRecordingMessage);
            }

            if (enabled && !IsHdrAvailable)
            {
                throw new InvalidOperationException("HDR is not available on the selected device.");
            }

            IsHdrEnabled = enabled;
            return Task.CompletedTask;
        }, cancellationToken);
    }

    public Task SetTrueHdrPreviewEnabledAsync(bool enabled, CancellationToken cancellationToken = default)
    {
        return InvokeOnUiThreadAsync(() =>
        {
            if (IsRecording)
            {
                throw new InvalidOperationException("True HDR preview cannot be changed while recording.");
            }

            IsTrueHdrPreviewEnabled = enabled;
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

    public Task SetOutputPathAsync(string outputPath, CancellationToken cancellationToken = default)
    {
        return InvokeOnUiThreadAsync(() =>
        {
            if (string.IsNullOrWhiteSpace(outputPath))
            {
                throw new InvalidOperationException("Output path cannot be empty.");
            }

            Directory.CreateDirectory(outputPath);
            OutputPath = outputPath;
            return Task.CompletedTask;
        }, cancellationToken);
    }

    public Task SetPreviewEnabledAsync(bool enabled, CancellationToken cancellationToken = default)
    {
        return InvokeOnUiThreadAsync(async () =>
        {
            if (!enabled && IsPreviewReinitializing)
            {
                CancelPendingPreviewRestart();
                if (!IsPreviewing)
                {
                    return;
                }
            }

            if (enabled == IsPreviewing)
            {
                return;
            }

            if (enabled)
            {
                await StartPreviewAsync(userInitiated: true, cancellationToken);
            }
            else
            {
                await StopPreviewAsync(userInitiated: true, teardownPipeline: false, cancellationToken);
            }
        }, cancellationToken);
    }

    public Task SetRecordingEnabledAsync(bool enabled, CancellationToken cancellationToken = default)
    {
        return SetRecordingDesiredStateAsync(enabled, cancellationToken);
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

    // ── Automation helpers ───────────────────────────────────────────────

    public Task SetMicrophoneEnabledAsync(bool enabled, CancellationToken cancellationToken = default)
    {
        return SetMicrophoneEnabledAutomationAsync(enabled, cancellationToken);
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
                // wants it, so succeed as a no-op rather than throwing — the failure
                // shape would break otherwise-safe scripts for no functional reason.
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

}

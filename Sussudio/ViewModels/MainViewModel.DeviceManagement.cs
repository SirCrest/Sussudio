using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.Services.Capture;

namespace Sussudio.ViewModels;

/// <summary>
/// Device and format discovery flow. It refreshes user-selectable options while
/// preserving the active selection whenever the underlying device list changes.
/// </summary>
public partial class MainViewModel
{
    public async Task RefreshDevicesAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        StatusText = "Scanning for devices...";

        try
        {
            var discoveryStopwatch = Stopwatch.StartNew();
            var scanGeneration = Interlocked.Increment(ref _deviceScanGeneration);
            var previousAudioId = SelectedAudioInputDevice?.Id;
            var previousMicrophoneId = SelectedMicrophoneDevice?.Id;
            var previousDeviceId = SelectedDevice?.Id;
            var audioDevicesTask = MfDeviceEnumerator.EnumerateAudioCaptureEndpointsAsync();
            var devicesTask = _deviceService.EnumerateVideoCaptureDevicesAsync(waitForFormatProbes: false);
            await Task.WhenAll(audioDevicesTask, devicesTask).ConfigureAwait(true);
            cancellationToken.ThrowIfCancellationRequested();
            var audioDevices = audioDevicesTask.Result.ToList();
            var devices = devicesTask.Result;
            cancellationToken.ThrowIfCancellationRequested();
            discoveryStopwatch.Stop();

            var captureCardAudioId = (devices.FirstOrDefault(d => d.Id == previousDeviceId) ?? devices.FirstOrDefault())?.AudioDeviceId;
            var filteredAudio = string.IsNullOrWhiteSpace(captureCardAudioId)
                ? audioDevices
                : audioDevices.Where(d => !string.Equals(d.Id, captureCardAudioId, StringComparison.OrdinalIgnoreCase)).ToList();
            ReplaceCollection(AudioInputDevices, filteredAudio);
            ReplaceCollection(MicrophoneDevices, filteredAudio);
            var savedAudioId = _pendingSavedAudioDeviceId;
            _pendingSavedAudioDeviceId = null;
            var savedMicrophoneId = _pendingSavedMicrophoneDeviceId;
            _pendingSavedMicrophoneDeviceId = null;
            SelectedAudioInputDevice =
                AudioInputDevices.FirstOrDefault(d => d.Id == previousAudioId)
                ?? (!string.IsNullOrWhiteSpace(savedAudioId) ? AudioInputDevices.FirstOrDefault(d => d.Id == savedAudioId) : null)
                ?? AudioInputDevices.FirstOrDefault();
            SelectedMicrophoneDevice =
                MicrophoneDevices.FirstOrDefault(d => d.Id == previousMicrophoneId)
                ?? (!string.IsNullOrWhiteSpace(savedMicrophoneId) ? MicrophoneDevices.FirstOrDefault(d => d.Id == savedMicrophoneId) : null)
                ?? MicrophoneDevices.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(savedAudioId) && SelectedAudioInputDevice?.Id != savedAudioId)
            {
                Logger.Log($"SETTINGS_RESTORE: saved audio device '{savedAudioId}' not found, using fallback.");
            }

            if (!string.IsNullOrWhiteSpace(savedMicrophoneId) && SelectedMicrophoneDevice?.Id != savedMicrophoneId)
            {
                Logger.Log($"SETTINGS_RESTORE: saved microphone device '{savedMicrophoneId}' not found, using fallback.");
            }

            ReplaceCollection(Devices, devices.ToList());
            foreach (var discoveredDevice in Devices)
            {
                _deviceService.BeginBackgroundFormatProbe(discoveredDevice, scanGeneration);
            }

            var discoverySummary = _deviceService.LastDiscoverySummary;
            Logger.Log($"Device discovery summary (ViewModel): {discoverySummary}");

            if (Devices.Count > 0)
            {
                StatusText = discoveryStopwatch.ElapsedMilliseconds <= 1500
                    ? $"Found {Devices.Count} device(s) in {discoveryStopwatch.ElapsedMilliseconds} ms"
                    : $"Found {Devices.Count} device(s) in {discoveryStopwatch.ElapsedMilliseconds} ms (slow scan: waiting on system device enumeration/probe startup)";

                var savedDeviceId = _pendingSavedDeviceId;
                _pendingSavedDeviceId = null;
                var nextSelectedDevice =
                    Devices.FirstOrDefault(d => d.Id == previousDeviceId)
                    ?? (!string.IsNullOrWhiteSpace(savedDeviceId) ? Devices.FirstOrDefault(d => d.Id == savedDeviceId) : null)
                    ?? Devices[0];
                if (!string.IsNullOrWhiteSpace(savedDeviceId) && nextSelectedDevice.Id != savedDeviceId)
                {
                    Logger.Log($"SETTINGS_RESTORE: saved device '{savedDeviceId}' not found, using fallback.");
                }
                SelectedDevice = nextSelectedDevice;
                Logger.Log($"Auto-selected device: {SelectedDevice?.Name}");

                // Auto-start preview (StartPreviewAsync will initialize device if needed)
                try
                {
                    await StartPreviewAsync(userInitiated: false, cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    Logger.Log($"Auto-start preview failed after device scan: {ex.Message}");
                    StatusText = $"Preview failed to start: {ex.Message}";
                }
            }
            else
            {
                SelectedDevice = null;
                StatusText = "No compatible video capture devices found (see log for discovery summary)";
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            StatusText = "Device scan canceled";
            throw;
        }
        catch (Exception ex)
        {
            StatusText = $"Error scanning devices: {ex.Message}";
        }
    }

    private static void ReplaceCollection<T>(ObservableCollection<T> target, IReadOnlyList<T> source)
    {
        target.Clear();
        foreach (var item in source)
        {
            target.Add(item);
        }
    }

    partial void OnSelectedDeviceChanged(CaptureDevice? value)
    {
        CancelPendingAudioControlWork();
        RebuildSelectedDeviceCapabilities(value, resetTelemetryState: true);
        var refreshCts = new CancellationTokenSource();
        var refreshToken = refreshCts.Token;
        _deviceAudioRefreshCts = refreshCts;
        var enqueued = EnqueueUiOperation(async () =>
        {
            try
            {
                if (Volatile.Read(ref _disposeState) == 0)
                {
                    await RefreshDeviceAudioControlsAsync(value, applySavedState: true, refreshToken).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                Logger.Log("Device audio controls refresh canceled because selected device changed");
            }
            finally
            {
                if (ReferenceEquals(_deviceAudioRefreshCts, refreshCts))
                {
                    _deviceAudioRefreshCts = null;
                }

                refreshCts.Dispose();
            }
        }, "device audio controls refresh", allowDuringDispose: true);
        if (!enqueued)
        {
            if (ReferenceEquals(_deviceAudioRefreshCts, refreshCts))
            {
                _deviceAudioRefreshCts = null;
            }

            refreshCts.Dispose();
        }
        SaveSettings();
    }

    private void RebuildSelectedDeviceCapabilities(CaptureDevice? device, bool resetTelemetryState)
    {
        _isChangingDevice = true;
        try
        {
            ResetFrameRateSelectionState();
            HdrResolutionSupportHint = string.Empty;

            AvailableFormats.Clear();
            AvailableFrameRates.Clear();
            _resolutionToFormats.Clear();
            if (resetTelemetryState)
            {
                _pendingSdrAutoSelectionForDeviceChange = device != null && !IsHdrEnabled;
                _pendingSdrAutoFriendlyFrameRateBucket = null;
                ApplySourceTelemetrySnapshot(
                    SourceSignalTelemetrySnapshot.CreateUnavailable("awaiting-source-telemetry"),
                    allowAutoRetarget: false);
            }

            if (device != null)
            {
                foreach (var format in device.SupportedFormats)
                {
                    AvailableFormats.Add(format);

                    var resolutionKey = GetResolutionKey(format.Width, format.Height);
                    if (!_resolutionToFormats.TryGetValue(resolutionKey, out var formats))
                    {
                        formats = new List<MediaFormat>();
                        _resolutionToFormats[resolutionKey] = formats;
                    }

                    formats.Add(format);
                }

                IsHdrAvailable = device.IsHdrCapable;
                if (!IsHdrAvailable)
                {
                    IsHdrEnabled = false;
                }
            }

            if (IsRecording)
            {
                _pendingModeOptionsRefresh = true;
            }
            else
            {
                RebuildResolutionOptions();
            }
        }
        finally
        {
            _isChangingDevice = false;
        }
    }

    partial void OnSelectedResolutionChanged(string? value)
    {
        if (TryResolveResolutionKey(value, out var resolvedResolutionKey))
        {
            _lastKnownResolutionKey = resolvedResolutionKey;
        }

        if (!_isRebuildingModeOptions && !_isApplyingAutomaticResolutionSelection)
        {
            _hasUserOverriddenResolutionForCurrentMode = !IsAutoResolutionValue(value);
            _pendingSdrAutoSelectionForDeviceChange = false;
            _pendingSdrAutoFriendlyFrameRateBucket = null;
        }

        if (_isRebuildingModeOptions)
        {
            return;
        }

        _forceSourceAutoRetarget = false;
        ResetFrameRateSelectionState();
        RebuildFrameRateOptions();
        UpdateTargetSummary();
    }

    partial void OnSelectedFormatChanged(MediaFormat? value)
    {
        // If preview is active and this isn't during initial device setup, reinitialize with new format
        if (value != null && !_isChangingDevice && !_suppressFormatChangeReinitialize && IsPreviewing && IsInitialized)
        {
            Logger.Log($"=== Format changed to {value.Width}x{value.Height}@{value.FrameRate}fps - reinitializing device ===");
            EnqueueUiOperation(() => ReinitializeDeviceAsync("format change"), "format change reinitialize");
        }
    }

    partial void OnSelectedVideoFormatChanged(string value)
    {
        if (!_isRebuildingModeOptions)
        {
            var previousSuppress = _suppressFormatChangeReinitialize;
            _suppressFormatChangeReinitialize = true;
            try
            {
                UpdateSelectedFormat();
            }
            finally
            {
                _suppressFormatChangeReinitialize = previousSuppress;
            }
        }

        if (!_isChangingDevice && !_suppressFormatChangeReinitialize && IsPreviewing && IsInitialized)
        {
            Logger.Log($"=== Video format override changed to {value} - reinitializing device ===");
            EnqueueUiOperation(() => ReinitializeDeviceAsync("video format override"), "video format override reinitialize");
        }
    }

    partial void OnMjpegDecoderCountChanged(int value)
    {
        var clamped = Math.Clamp(value, 1, 8);
        if (clamped != value)
        {
            MjpegDecoderCount = clamped;
            return;
        }

        if (!_isChangingDevice &&
            IsPreviewing &&
            IsInitialized &&
            BuildCaptureSettings().UseMjpegHighFrameRateMode)
        {
            Logger.Log($"=== MJPEG decoder count changed to {value} - reinitializing device ===");
            EnqueueUiOperation(() => ReinitializeDeviceAsync("mjpeg decoder count"), "mjpeg decoder count reinitialize");
        }
    }
}

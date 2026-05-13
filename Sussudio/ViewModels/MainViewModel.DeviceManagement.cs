using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.Services.Audio;
using Sussudio.Services.Capture;

namespace Sussudio.ViewModels;

/// <summary>
/// Device and format discovery flow. It refreshes user-selectable options while
/// preserving the active selection whenever the underlying device list changes.
/// </summary>
public partial class MainViewModel
{
    private void OnAudioDevicesChanged()
    {
        if (!_dispatcherQueue.TryEnqueue(() =>
        {
            _ = RefreshAudioDeviceListAsync();
        }))
        {
            Logger.Log("AUDIO_DEVICES_CHANGED_UI_ENQUEUE_FAILED");
        }
    }

    private List<AudioInputDevice> FilterOutCaptureCardAudio(List<AudioInputDevice> devices)
    {
        var excludeId = SelectedDevice?.AudioDeviceId;
        if (string.IsNullOrWhiteSpace(excludeId))
        {
            return devices;
        }

        return devices.Where(d => !string.Equals(d.Id, excludeId, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    private async Task RefreshAudioDeviceListAsync()
    {
        try
        {
            var previousAudioId = SelectedAudioInputDevice?.Id;
            var previousMicrophoneId = SelectedMicrophoneDevice?.Id;
            var audioDevices = FilterOutCaptureCardAudio(
                (await MfDeviceEnumerator.EnumerateAudioCaptureEndpointsAsync()).ToList());

            ReplaceCollection(AudioInputDevices, audioDevices);
            ReplaceCollection(MicrophoneDevices, audioDevices);
            var savedMicrophoneId = _pendingSavedMicrophoneDeviceId;
            SelectedAudioInputDevice =
                AudioInputDevices.FirstOrDefault(d => d.Id == previousAudioId)
                ?? AudioInputDevices.FirstOrDefault();
            SelectedMicrophoneDevice =
                MicrophoneDevices.FirstOrDefault(d => d.Id == previousMicrophoneId)
                ?? (!string.IsNullOrWhiteSpace(savedMicrophoneId) ? MicrophoneDevices.FirstOrDefault(d => d.Id == savedMicrophoneId) : null)
                ?? MicrophoneDevices.FirstOrDefault();

            Logger.Log($"Audio device list refreshed ({AudioInputDevices.Count} devices).");
        }
        catch (Exception ex)
        {
            Logger.Log($"Audio device list refresh failed: {ex.Message}");
        }
    }

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

    private void CancelPendingAudioControlWork()
    {
        var flashCts = _gainFlashDebounceCts;
        _gainFlashDebounceCts = null;
        flashCts?.Cancel();

        var xuCts = _gainXuDebounceCts;
        _gainXuDebounceCts = null;
        xuCts?.Cancel();

        var modeCts = _deviceAudioModeCts;
        _deviceAudioModeCts = null;
        modeCts?.Cancel();

        var refreshCts = _deviceAudioRefreshCts;
        _deviceAudioRefreshCts = null;
        refreshCts?.Cancel();
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

    private void OnDeviceFormatProbeCompleted(object? sender, DeviceService.DeviceFormatProbeCompletedEventArgs e)
    {
        if (!_dispatcherQueue.TryEnqueue(() =>
        {
            if (e.RequestId != Interlocked.Read(ref _deviceScanGeneration))
            {
                return;
            }

            var target = Devices.FirstOrDefault(d => string.Equals(d.Id, e.DeviceId, StringComparison.OrdinalIgnoreCase));
            if (target == null)
            {
                return;
            }

            if (!e.Succeeded)
            {
                _pendingSdrAutoSelectionForDeviceChange = false;
                _pendingSdrAutoFriendlyFrameRateBucket = null;
                Logger.Log($"Format probe failed for {e.DeviceName}: {e.Error}");
                return;
            }

            target.SupportedFormats.Clear();
            foreach (var format in e.Formats)
            {
                target.SupportedFormats.Add(new MediaFormat
                {
                    Width = format.Width,
                    Height = format.Height,
                    FrameRate = format.FrameRate,
                    FrameRateNumerator = format.FrameRateNumerator,
                    FrameRateDenominator = format.FrameRateDenominator,
                    PixelFormat = format.PixelFormat,
                    IsHdr = format.IsHdr
                });
            }

            target.IsHdrCapable = e.IsHdrCapable;

            if (SelectedDevice == null ||
                !string.Equals(SelectedDevice.Id, target.Id, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var preserveActiveSelection = IsPreviewing || IsRecording;
            var allowProbeDrivenRetarget = IsPreviewing && IsInitialized && !IsRecording;
            var previousResolution = SelectedResolution;
            var previousFrameRate = SelectedFrameRate;
            Logger.Log($"Format probe completed for {e.DeviceName}: formats={e.Formats.Count} preserveActive={preserveActiveSelection} allowRetarget={allowProbeDrivenRetarget} prevRes={previousResolution} prevFps={previousFrameRate:0.###}");

            if (preserveActiveSelection)
            {
                Logger.Log($"Refreshing selected-device capabilities during active capture for {e.DeviceName} (preserveSelection={!allowProbeDrivenRetarget}).");
            }

            _suppressFormatChangeReinitialize = preserveActiveSelection;
            try
            {
                RebuildSelectedDeviceCapabilities(SelectedDevice, resetTelemetryState: false);
            }
            finally
            {
                _suppressFormatChangeReinitialize = false;
            }
            Logger.Log($"Format probe rebuild done: SelectedRes={SelectedResolution} SelectedFormat={SelectedFormat?.Width}x{SelectedFormat?.Height}@{SelectedFormat?.FrameRate:0.###} modeChanged={!string.Equals(previousResolution, SelectedResolution, StringComparison.OrdinalIgnoreCase) || !IsFrameRateMatch(previousFrameRate, SelectedFrameRate)}");

            var modeChanged = !string.Equals(previousResolution, SelectedResolution, StringComparison.OrdinalIgnoreCase) ||
                              !IsFrameRateMatch(previousFrameRate, SelectedFrameRate);

            if (allowProbeDrivenRetarget &&
                IsHdrEnabled &&
                modeChanged)
            {
                Logger.Log($"Format probe updated HDR mode set; applying new mode {SelectedResolution}@{SelectedFrameRate:0.###} via device renegotiation.");
                EnqueueUiOperation(() => ReinitializeDeviceAsync("format probe (HDR retarget)"), "format probe hdr retarget");
                return;
            }

            if (allowProbeDrivenRetarget &&
                !IsHdrEnabled &&
                SelectedFormat?.PixelFormat.Equals("MJPG", StringComparison.OrdinalIgnoreCase) == true)
            {
                if (ShouldPreserveMjpegHighFrameRateMode(SelectedFormat))
                {
                    Logger.Log(
                        $"Format probe preserved special MJPG HFR mode at {SelectedResolution}@{SelectedFrameRate:0.###}; " +
                        "skipping SDR NV12 retarget.");
                    return;
                }

                var preferredRate = previousFrameRate > 0 ? previousFrameRate : SelectedFrameRate;
                var preferredBucket = GetFriendlyFrameRateBucket(preferredRate);
                var nv12Candidates = target.SupportedFormats
                    .Where(format => format.PixelFormat.Equals("NV12", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                MediaFormat? selectedNv12 = nv12Candidates
                    .Where(format => GetFriendlyFrameRateBucket(format.FrameRateExact) == preferredBucket)
                    .OrderByDescending(format => (long)format.Width * format.Height)
                    .FirstOrDefault();

                selectedNv12 ??= nv12Candidates
                    .OrderBy(format => Math.Abs(format.FrameRateExact - preferredRate))
                    .ThenByDescending(format => (long)format.Width * format.Height)
                    .FirstOrDefault();

                if (selectedNv12 != null)
                {
                    var targetResolution = GetResolutionKey(selectedNv12.Width, selectedNv12.Height);
                    if (!string.Equals(targetResolution, SelectedResolution, StringComparison.OrdinalIgnoreCase))
                    {
                        Logger.Log(
                            $"Format probe detected MJPG-only mode at {SelectedResolution}@{SelectedFrameRate:0.###}; " +
                            $"retargeting SDR to NV12-capable mode {targetResolution}@{selectedNv12.FrameRateExact:0.###}.");

                        _isRebuildingModeOptions = true;
                        _isApplyingAutomaticResolutionSelection = true;
                        try
                        {
                            SelectedResolution = targetResolution;
                        }
                        finally
                        {
                            _isApplyingAutomaticResolutionSelection = false;
                            _isRebuildingModeOptions = false;
                        }

                        _suppressFormatChangeReinitialize = true;
                        try
                        {
                            RebuildFrameRateOptions();
                        }
                        finally
                        {
                            _suppressFormatChangeReinitialize = false;
                        }
                        EnqueueUiOperation(() => ReinitializeDeviceAsync("format probe (SDR nv12 retarget)"), "format probe sdr retarget");
                        return;
                    }
                }
            }

            // After probes complete, compare the live session negotiated resolution against
            // the now-resolved SelectedFormat. This catches the startup case where preview began
            // with an incomplete format list (probes not yet done) and therefore initialized at
            // a lower resolution than the user saved selection.
            if (allowProbeDrivenRetarget && SelectedFormat != null)
            {
                var runtime = GetCaptureRuntimeSnapshot();
                Logger.Log($"Format probe session check: actual={runtime.ActualWidth}x{runtime.ActualHeight} selected={SelectedFormat.Width}x{SelectedFormat.Height}");
                if (runtime.ActualWidth == null || runtime.ActualHeight == null)
                {
                    Logger.Log("Format probe session mismatch check skipped: runtime width/height not yet available.");
                }
                else if (runtime.ActualWidth != SelectedFormat.Width || runtime.ActualHeight != SelectedFormat.Height)
                {
                    Logger.Log(
                        $"Format probe detected session/format mismatch: " +
                        $"session={runtime.ActualWidth}x{runtime.ActualHeight} " +
                        $"selected={SelectedFormat.Width}x{SelectedFormat.Height}; reinitializing.");
                    EnqueueUiOperation(
                        () => ReinitializeDeviceAsync("format probe (session mismatch)"),
                        "format probe session mismatch");
                    return;
                }
            }

            if (preserveActiveSelection &&
                !allowProbeDrivenRetarget &&
                modeChanged &&
                !string.IsNullOrWhiteSpace(previousResolution) &&
                AvailableResolutions.Any(option => string.Equals(option.Value, previousResolution, StringComparison.OrdinalIgnoreCase)))
            {
                _isRebuildingModeOptions = true;
                _isApplyingAutomaticResolutionSelection = true;
                try
                {
                    SelectedResolution = previousResolution;
                    SelectedFrameRate = previousFrameRate;
                    UpdateSelectedFormat();
                    UpdateTargetSummary();
                }
                finally
                {
                    _isApplyingAutomaticResolutionSelection = false;
                    _isRebuildingModeOptions = false;
                }
            }
        }))
        {
            Logger.Log($"FORMAT_PROBE_UI_ENQUEUE_FAILED deviceId='{e.DeviceId}' requestId={e.RequestId}");
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

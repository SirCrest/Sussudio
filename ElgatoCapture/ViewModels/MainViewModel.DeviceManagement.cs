using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ElgatoCapture.Models;
using ElgatoCapture.Services.Audio;
using ElgatoCapture.Services.Automation;
using ElgatoCapture.Services.Capture;
using ElgatoCapture.Services.Configuration;
using ElgatoCapture.Services.Flashback;
using ElgatoCapture.Services.Gpu;
using ElgatoCapture.Services.Preview;
using ElgatoCapture.Services.Recording;
using ElgatoCapture.Services.Runtime;
using ElgatoCapture.Services.Telemetry;

namespace ElgatoCapture.ViewModels;

public partial class MainViewModel
{
    private void OnAudioDevicesChanged()
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            _ = RefreshAudioDeviceListAsync();
        });
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
            var audioDevices = (await MfDeviceEnumerator.EnumerateAudioCaptureEndpointsAsync()).ToList();
            cancellationToken.ThrowIfCancellationRequested();
            var devices = await _deviceService.EnumerateVideoCaptureDevicesAsync(waitForFormatProbes: false);
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
        _dispatcherQueue.TryEnqueue(() =>
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
        });
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

    partial void OnSelectedFrameRateChanged(double value)
    {
        if (IsAutoFrameRateValue(value))
        {
            SelectAutoFrameRate(rebuildOptions: !IsRecording && !_isRebuildingModeOptions && !_isApplyingAutomaticFrameRateSelection);
            return;
        }

        if (!_isRebuildingModeOptions && !_isApplyingAutomaticFrameRateSelection)
        {
            IsAutoFrameRateSelected = false;
            _hasUserOverriddenFrameRateForCurrentMode = true;
            _pendingSdrAutoSelectionForDeviceChange = false;
            _pendingSdrAutoFriendlyFrameRateBucket = null;
        }

        var selected = AvailableFrameRates
            .FirstOrDefault(option => IsFrameRateMatch(option.Value, value))
            ?? AvailableFrameRates.FirstOrDefault(option => IsFriendlyFrameRateMatch(option.FriendlyValue, value));
        SelectedFriendlyFrameRate = selected?.FriendlyValue ?? Math.Round(value, MidpointRounding.AwayFromZero);
        SelectedExactFrameRate = selected?.Value ?? value;
        SelectedExactFrameRateArg = selected?.Rational;
        if (IsAutoResolutionValue(SelectedResolution))
        {
            AutoResolvedFrameRate = selected?.Value ?? value;
        }

        UpdateSelectedFormat();
        UpdateTargetSummary();
    }

    private void UpdateSelectedFormat()
    {
        if (!TryGetEffectiveResolutionSelection(out var resolutionKey, out var width, out var height))
        {
            SelectedFormat = null;
            return;
        }

        var candidates = AvailableFormats
            .Where(f => f.Width == width && f.Height == height)
            .ToList();
        if (IsHdrEnabled)
        {
            candidates = candidates.Where(IsHdrModeCandidate).ToList();
        }
        else
        {
            // When HDR is off, exclude 10-bit formats (P010/P016) so the source reader
            // requests an 8-bit subtype (NV12) rather than triggering a P010→NV12 fallback.
            var sdrCandidates = candidates.Where(f => !IsHdrModeCandidate(f)).ToList();
            if (sdrCandidates.Count > 0)
                candidates = sdrCandidates;
        }

        if (candidates.Count == 0)
        {
            SelectedFormat = null;
            return;
        }

        var selectedRateOption = AvailableFrameRates
            .FirstOrDefault(option => IsFrameRateMatch(option.Value, SelectedFrameRate))
            ?? AvailableFrameRates.FirstOrDefault(option => IsFriendlyFrameRateMatch(option.FriendlyValue, SelectedFrameRate));
        var friendlyBucket = selectedRateOption != null
            ? (int)Math.Round(selectedRateOption.FriendlyValue, MidpointRounding.AwayFromZero)
            : GetFriendlyFrameRateBucket(SelectedFrameRate);

        var timingFamily = ResolvePreferredTimingFamily(resolutionKey, SelectedFrameRate);
        if (selectedRateOption != null &&
            TryInferFrameRateTimingFamily(selectedRateOption.Rational, selectedRateOption.Value, out var optionFamily))
        {
            timingFamily = optionFamily;
        }

        var rateCandidates = candidates
            .Where(format => GetFriendlyFrameRateBucket(format.FrameRateExact) == friendlyBucket)
            .ToList();
        if (rateCandidates.Count == 0)
        {
            rateCandidates = candidates;
        }

        SelectedFormat = SelectPreferredFrameRateFormat(rateCandidates, friendlyBucket, timingFamily);
    }

    /// <summary>
    /// H.264 is intentionally excluded from HDR recording: the nvenc H.264
    /// encoder has no 10-bit profile, so it cannot carry bt2020/PQ metadata.
    /// Only HEVC (Main 10) and AV1 (main profile, 10-bit) support HDR output.
    /// When HDR is enabled, <see cref="RebuildRecordingFormatOptions"/> filters
    /// the codec list to these two formats and the UI hides H.264.
    /// </summary>
    private static bool IsHdrCompatibleRecordingFormat(string format)
        => format.Contains("HEVC", StringComparison.OrdinalIgnoreCase) ||
           format.Contains("AV1", StringComparison.OrdinalIgnoreCase);

    private void RebuildRecordingFormatOptions()
    {
        var sourceFormats = (_detectedRecordingFormats.Count > 0
            ? _detectedRecordingFormats
            : AvailableRecordingFormats.ToList())
            .ToList();
        if (sourceFormats.Count == 0)
        {
            sourceFormats.Add(DefaultRecordingFormat);
        }
        var formats = IsHdrEnabled
            ? sourceFormats.Where(IsHdrCompatibleRecordingFormat).ToList()
            : sourceFormats.ToList();
        if (formats.Count == 0 && AvailableRecordingFormats.Count > 0)
        {
            // Keep the last known real formats visible if capability refresh temporarily produced none.
            formats = AvailableRecordingFormats.ToList();
        }

        AvailableRecordingFormats.Clear();
        foreach (var format in formats)
        {
            AvailableRecordingFormats.Add(format);
        }

        string? targetFormat;
        if (IsHdrEnabled)
        {
            // Preserve the user's codec when it already supports HDR (AV1 or HEVC).
            // Only override to HEVC/AV1 when the current selection is incompatible
            // (e.g. H.264, which has no 10-bit HDR profile on nvenc).
            if (!string.IsNullOrWhiteSpace(SelectedRecordingFormat) &&
                formats.Any(format => string.Equals(format, SelectedRecordingFormat, StringComparison.OrdinalIgnoreCase)) &&
                IsHdrCompatibleRecordingFormat(SelectedRecordingFormat))
            {
                targetFormat = SelectedRecordingFormat;
            }
            else
            {
                targetFormat = formats.FirstOrDefault(format =>
                    string.Equals(format, HevcRecordingFormat, StringComparison.OrdinalIgnoreCase))
                    ?? formats.FirstOrDefault(format =>
                        string.Equals(format, Av1RecordingFormat, StringComparison.OrdinalIgnoreCase))
                    ?? formats.FirstOrDefault();
            }
        }
        else
        {
            targetFormat = SelectedRecordingFormat;
            if (string.IsNullOrWhiteSpace(targetFormat) ||
                !formats.Any(format => string.Equals(format, targetFormat, StringComparison.OrdinalIgnoreCase)))
            {
                targetFormat = formats.FirstOrDefault(format =>
                    format.Contains("H.264", StringComparison.OrdinalIgnoreCase) ||
                    format.Contains("H264", StringComparison.OrdinalIgnoreCase))
                    ?? formats.FirstOrDefault();
            }
        }

        if (string.IsNullOrWhiteSpace(targetFormat))
        {
            targetFormat = DefaultRecordingFormat;
        }

        var previousSelection = SelectedRecordingFormat;
        SelectedRecordingFormat = targetFormat;
        if (string.Equals(previousSelection, targetFormat, StringComparison.Ordinal))
        {
            OnPropertyChanged(nameof(SelectedRecordingFormat));
        }

        if (IsHdrEnabled && !IsHdrCompatibleRecordingFormat(SelectedRecordingFormat))
        {
            StatusText = "HDR recording requires HEVC or AV1 (10-bit).";
        }

        Logger.Log($"Selected recording format: {SelectedRecordingFormat}");
    }

    private static bool IsHdrModeCandidate(MediaFormat format)
        => format.IsHdr || MediaFormat.IsTrue10BitPixelFormat(format.PixelFormat);

    private static bool ShouldPreserveMjpegHighFrameRateMode(MediaFormat? format)
        => format != null &&
           CaptureSettings.IsMjpegHighFrameRateMode(
               format.PixelFormat,
               format.Width,
               format.Height,
               format.FrameRateExact,
               hdrEnabled: false);

    partial void OnIsHdrEnabledChanged(bool value)
    {
        if (_isRevertingHdrToggle)
        {
            return;
        }

        if (value)
        {
            _pendingSdrAutoSelectionForDeviceChange = false;
            _pendingSdrAutoFriendlyFrameRateBucket = null;
        }

        if (IsRecording)
        {
            _isRevertingHdrToggle = true;
            try
            {
                IsHdrEnabled = !value;
            }
            finally
            {
                _isRevertingHdrToggle = false;
            }

            StatusText = HdrToggleBlockedWhileRecordingMessage;
            return;
        }

        if (!_isChangingDevice)
        {
            _suppressFormatChangeReinitialize = true;
            try
            {
                ResetModeSelectionState();
                RebuildResolutionOptions();
                RebuildRecordingFormatOptions();
            }
            finally
            {
                _suppressFormatChangeReinitialize = false;
            }

            if (IsInitialized && !IsRecording && SelectedDevice != null && SelectedFormat != null)
            {
                Logger.Log($"HDR toggle changed to {(value ? "On" : "Off")} - forcing immediate device renegotiation");
                EnqueueUiOperation(() => ReinitializeDeviceAsync("HDR toggle"), "hdr toggle reinitialize");
            }
        }

        SaveSettings();
    }


    private void RebuildResolutionOptions()
    {
        var previousSelection = SelectedResolution;
        var previousRate = SelectedFrameRate;
        var desiredSelection = !string.IsNullOrWhiteSpace(previousSelection)
            ? previousSelection
            : _lastKnownResolutionKey;
        var options = _resolutionToFormats
            .Select(entry =>
            {
                var formats = entry.Value;
                var first = formats[0];
                var hdrSupported = formats.Any(IsHdrModeCandidate);
                var enabled = !IsHdrEnabled || hdrSupported;
                return new ResolutionOption
                {
                    Value = entry.Key,
                    Width = first.Width,
                    Height = first.Height,
                    IsEnabled = enabled,
                    DisableReason = enabled
                        ? string.Empty
                        : "HDR mode is not supported at this resolution."
                };
            })
            .OrderByDescending(option => (long)option.Width * option.Height)
            .ToList();

        if (!ShowAllCaptureOptions &&
            _latestSourceTelemetry.HasDimensions)
        {
            options = options
                .Where(DoesResolutionMatchSourceAspectRatio)
                .ToList();
        }

        var autoSelection = ResolveAutoCaptureSelection(options);
        var autoOption = options.Count > 0
            ? CreateAutoResolutionOption()
            : null;

        if (options.Count == 0)
        {
            if (SelectedDevice != null && IsPreviewing && AvailableResolutions.Count > 0)
            {
                var retainedSelection = AvailableResolutions.FirstOrDefault(option =>
                        string.Equals(option.Value, SelectedResolution, StringComparison.OrdinalIgnoreCase))
                    ?? AvailableResolutions.FirstOrDefault(option => option.IsEnabled)
                    ?? AvailableResolutions.FirstOrDefault();
                if (retainedSelection != null)
                {
                    _isRebuildingModeOptions = true;
                    _isApplyingAutomaticResolutionSelection = true;
                    try
                    {
                        var previousSelectedResolution = SelectedResolution;
                        SelectedResolution = retainedSelection.Value;
                        if (string.Equals(previousSelectedResolution, retainedSelection.Value, StringComparison.OrdinalIgnoreCase))
                        {
                            OnPropertyChanged(nameof(SelectedResolution));
                        }

                        if (TryResolveResolutionKey(retainedSelection.Value, out var retainedResolutionKey))
                        {
                            _lastKnownResolutionKey = retainedResolutionKey;
                        }
                    }
                    finally
                    {
                        _isApplyingAutomaticResolutionSelection = false;
                        _isRebuildingModeOptions = false;
                    }
                }

                RebuildFrameRateOptions();
                UpdateTargetSummary();
                return;
            }

            _isRebuildingModeOptions = true;
            try
            {
                AvailableResolutions.Clear();
                _isApplyingAutomaticResolutionSelection = true;
                SelectedResolution = null;
                _isApplyingAutomaticResolutionSelection = false;
                ClearAutoResolutionState();
                HdrResolutionSupportHint = string.Empty;
                DisabledResolutionReason = string.Empty;
            }
            finally
            {
                _isApplyingAutomaticResolutionSelection = false;
                _isRebuildingModeOptions = false;
            }

            RebuildFrameRateOptions();
            UpdateTargetSummary();
            return;
        }

        string? hdrHint = null;
        var allowSourceAutoSelect = IsHdrEnabled && (_forceSourceAutoRetarget || !_hasUserOverriddenResolutionForCurrentMode);
        var sourceSelected = allowSourceAutoSelect
            ? TrySelectSourceResolutionOption(options, desiredSelection)
            : null;
        var sourceSelectedValue = sourceSelected?.Value;
        if (IsHdrEnabled &&
            sourceSelected is { IsEnabled: true } &&
            previousRate > 0 &&
            !ResolutionSupportsFrameRate(sourceSelected.Value, previousRate, hdrOnly: true))
        {
            var sourceMax = GetMaxFrameRateForResolution(sourceSelected.Value, hdrOnly: true);
            if (sourceMax > 0)
            {
                hdrHint = $"HDR at {sourceSelected.Value} supported up to {FormatFriendlyFrameRate(sourceMax)} fps.";
            }

            sourceSelected = null;
        }

        var selected = sourceSelected;
        if (!IsHdrEnabled &&
            _pendingSdrAutoSelectionForDeviceChange &&
            TrySelectSdrAutoResolutionOption(options, out var sdrAutoSelection, out var sdrAutoFriendlyBucket))
        {
            selected = sdrAutoSelection;
            _pendingSdrAutoFriendlyFrameRateBucket = sdrAutoFriendlyBucket;
        }

        if (selected == null)
        {
            // The capture card (e.g. 4K X) cannot deliver HDR at every resolution+FPS
            // combination due to USB bandwidth limits. When HDR is enabled, we pick the
            // highest resolution that still supports the user's chosen frame rate in HDR
            // mode, which may be lower than the source resolution. This is an intentional
            // hardware-driven trade-off: preserve frame rate, drop resolution.
            selected = IsHdrEnabled
                ? SelectHdrResolutionOption(options, desiredSelection, previousRate, out hdrHint)
                : options.FirstOrDefault(option =>
                    option.IsEnabled &&
                    string.Equals(option.Value, desiredSelection, StringComparison.OrdinalIgnoreCase))
                    ?? options.FirstOrDefault(option => option.IsEnabled)
                    ?? options.FirstOrDefault();

            if (IsHdrEnabled &&
                !string.IsNullOrWhiteSpace(sourceSelectedValue) &&
                selected != null &&
                !string.Equals(sourceSelectedValue, selected.Value, StringComparison.OrdinalIgnoreCase) &&
                previousRate > 0)
            {
                var sourceMax = GetMaxFrameRateForResolution(sourceSelectedValue, hdrOnly: true);
                if (sourceMax > 0 && previousRate > sourceMax + 0.01)
                {
                    hdrHint = $"HDR at {sourceSelectedValue} supported up to {FormatFriendlyFrameRate(sourceMax)} fps; switched to {selected.Value} to keep {FormatFriendlyFrameRate(previousRate)} fps.";
                }
            }
        }

        var selectAutoOption = autoOption != null && ShouldSelectAutoResolutionOption(previousSelection);
        var selectedDropdownOption = selectAutoOption
            ? autoOption
            : selected;
        var availableOptions = autoOption == null
            ? options
            : new[] { autoOption }.Concat(options).ToList();

        _isRebuildingModeOptions = true;
        try
        {
            UpdateAutoResolutionState(autoSelection);
            AvailableResolutions.Clear();
            foreach (var option in availableOptions)
            {
                AvailableResolutions.Add(option);
            }

            _isApplyingAutomaticResolutionSelection = true;
            if (selectedDropdownOption != null)
            {
                var previousSelectedResolution = SelectedResolution;
                SelectedResolution = selectedDropdownOption.Value;
                if (string.Equals(previousSelectedResolution, selectedDropdownOption.Value, StringComparison.OrdinalIgnoreCase))
                {
                    OnPropertyChanged(nameof(SelectedResolution));
                }
            }

            _isApplyingAutomaticResolutionSelection = false;
            if (selected != null)
            {
                _lastKnownResolutionKey = selected.Value;
            }

            if (IsHdrEnabled)
            {
                HdrResolutionSupportHint = hdrHint ?? BuildHdrSupportHintForResolution(selected?.Value);
            }
            else
            {
                HdrResolutionSupportHint = string.Empty;
            }

            if (IsHdrEnabled && selected is { IsEnabled: false })
            {
                StatusText = "No HDR-capable resolution is available for this device.";
            }

            DisabledResolutionReason = selected is { IsEnabled: false }
                ? selected.DisableReason
                : string.Empty;
        }
        finally
        {
            _isApplyingAutomaticResolutionSelection = false;
            _isRebuildingModeOptions = false;
        }

        RebuildFrameRateOptions();
    }

    public void SelectAutoFrameRate()
        => SelectAutoFrameRate(rebuildOptions: !IsRecording && !_isRebuildingModeOptions && !_isApplyingAutomaticFrameRateSelection);

    private void SelectAutoFrameRate(bool rebuildOptions)
    {
        IsAutoFrameRateSelected = true;
        _hasUserOverriddenFrameRateForCurrentMode = false;
        _pendingSdrAutoSelectionForDeviceChange = false;
        _pendingSdrAutoFriendlyFrameRateBucket = null;

        if (rebuildOptions)
        {
            RebuildFrameRateOptions();
            return;
        }

        var currentOptions = AvailableFrameRates
            .Where(option => !IsAutoFrameRateValue(option.FriendlyValue))
            .ToList();
        var selectedResolutionKey = GetEffectiveResolutionKey(SelectedResolution);
        var sourceRate = ResolveDetectedSourceFrameRate(selectedResolutionKey, currentOptions, SelectedFrameRate);
        var sourceTimingFamilyKnown = TryInferFrameRateTimingFamily(sourceRate.Arg, sourceRate.Rate, out var sourceTimingFamily);
        FrameRateOption? selected = null;
        if (!IsHdrEnabled &&
            _pendingSdrAutoSelectionForDeviceChange &&
            _pendingSdrAutoFriendlyFrameRateBucket.HasValue)
        {
            selected = currentOptions.FirstOrDefault(option =>
                option.IsEnabled && IsFriendlyFrameRateMatch(option.FriendlyValue, _pendingSdrAutoFriendlyFrameRateBucket.Value));
        }

        if (selected == null &&
            sourceRate.Rate.HasValue)
        {
            selected = currentOptions
                .Where(option => option.IsEnabled)
                .OrderBy(option => Math.Abs(option.Value - sourceRate.Rate.Value))
                .ThenBy(option =>
                    sourceTimingFamilyKnown &&
                    TryInferFrameRateTimingFamily(option.Rational, option.Value, out var optionFamily) &&
                    optionFamily == sourceTimingFamily
                        ? 0
                        : 1)
                .FirstOrDefault();
        }

        selected ??= currentOptions.FirstOrDefault(option => option.IsEnabled)
            ?? currentOptions.FirstOrDefault();

        ApplyResolvedFrameRateSelection(selected, SelectedFrameRate > 0 ? SelectedFrameRate : 60);
        UpdateSelectedFormat();
        UpdateTargetSummary();
    }

    private void RebuildFrameRateOptions()
    {
        var previousRate = SelectedFrameRate;
        var options = new List<FrameRateOption>();
        var selectedResolutionKey = GetEffectiveResolutionKey(SelectedResolution);
        var timingFamily = ResolvePreferredTimingFamily(selectedResolutionKey, previousRate);
        if (_latestSourceTelemetry.HasFrameRate &&
            TryInferFrameRateTimingFamily(_latestSourceTelemetry.FrameRateArg, _latestSourceTelemetry.FrameRateExact, out var sourceFamilyHint))
        {
            timingFamily = sourceFamilyHint;
        }

        if (!string.IsNullOrWhiteSpace(selectedResolutionKey) &&
            _resolutionToFormats.TryGetValue(selectedResolutionKey, out var formats))
        {
            options = formats
                .GroupBy(format => GetFriendlyFrameRateBucket(format.FrameRateExact))
                .Select(group =>
                {
                    var allFormats = group.ToList();
                    var hdrFormats = allFormats.Where(IsHdrModeCandidate).ToList();
                    var sdrFormats = allFormats.Where(f => !IsHdrModeCandidate(f)).ToList();
                    // In HDR mode, only enable rates with HDR-capable formats.
                    // In SDR mode, enable if 8-bit formats exist. Also enable if only
                    // 10-bit formats exist for this rate (e.g., 4K HFR paths that only
                    // advertise P010) — UpdateSelectedFormat handles the fallback.
                    var enabled = IsHdrEnabled ? hdrFormats.Count > 0 : allFormats.Count > 0;
                    List<MediaFormat> selectionPool;
                    if (IsHdrEnabled && hdrFormats.Count > 0)
                        selectionPool = hdrFormats;
                    else if (!IsHdrEnabled && sdrFormats.Count > 0)
                        selectionPool = sdrFormats;
                    else
                        selectionPool = allFormats;
                    var preferred = SelectPreferredFrameRateFormat(selectionPool, group.Key, timingFamily);
                    var numerator = preferred.FrameRateNumerator > 0 ? preferred.FrameRateNumerator : (uint?)null;
                    var denominator = preferred.FrameRateDenominator > 0 ? preferred.FrameRateDenominator : (uint?)null;
                    return new FrameRateOption
                    {
                        FriendlyValue = group.Key,
                        Value = preferred.FrameRateExact,
                        Rational = preferred.FrameRateRational,
                        Numerator = numerator,
                        Denominator = denominator,
                        IsEnabled = enabled,
                        DisableReason = enabled
                            ? string.Empty
                            : "HDR mode is not supported at this frame rate."
                    };
                })
                .OrderByDescending(option => option.FriendlyValue)
                .ToList();
        }

        var sourceRate = ResolveDetectedSourceFrameRate(selectedResolutionKey, options, previousRate);
        var sourceTimingFamilyKnown = TryInferFrameRateTimingFamily(sourceRate.Arg, sourceRate.Rate, out var sourceTimingFamily);
        var sourceFriendlyRate = sourceRate.Rate.HasValue
            ? Math.Round(sourceRate.Rate.Value, MidpointRounding.AwayFromZero)
            : (double?)null;
        var cappedOptions = options
            .Select(option =>
            {
                var enabled = option.IsEnabled;
                var disableReason = option.DisableReason;

                if (enabled && sourceFriendlyRate.HasValue)
                {
                    if (option.FriendlyValue > sourceFriendlyRate.Value + 0.01)
                    {
                        enabled = false;
                        disableReason = $"Source signal is {sourceFriendlyRate.Value:0} fps; higher capture fps duplicates frames.";
                    }
                    else if (sourceTimingFamilyKnown &&
                             sourceRate.Rate.HasValue &&
                             TryInferFrameRateTimingFamily(option.Rational, option.Value, out var optionFamily) &&
                             optionFamily != FrameRateTimingFamily.Unknown &&
                             sourceTimingFamily != FrameRateTimingFamily.Unknown &&
                             optionFamily != sourceTimingFamily &&
                             ResolutionHasTimingFamilyVariant(selectedResolutionKey, option.FriendlyValue, sourceTimingFamily) &&
                             IsFriendlyFrameRateMatch(option.FriendlyValue, sourceFriendlyRate.Value) &&
                             option.Value > sourceRate.Rate.Value + 0.03)
                    {
                        enabled = false;
                        disableReason = $"Source timing is {sourceRate.Arg ?? sourceRate.Rate.Value.ToString("0.###")} so this duplicate variant is hidden.";
                    }
                    else
                    {
                        var roundedSourceFriendlyRate = (int)Math.Round(sourceFriendlyRate.Value, MidpointRounding.AwayFromZero);
                        var roundedOptionFriendlyRate = (int)Math.Round(option.FriendlyValue, MidpointRounding.AwayFromZero);
                        if (roundedOptionFriendlyRate > 0 &&
                            roundedOptionFriendlyRate <= roundedSourceFriendlyRate &&
                            roundedSourceFriendlyRate % roundedOptionFriendlyRate != 0)
                        {
                            enabled = false;
                            disableReason = $"{roundedOptionFriendlyRate:0} fps is not a clean divisor of source {roundedSourceFriendlyRate:0} fps.";
                        }
                    }
                }

                return new FrameRateOption
                {
                    FriendlyValue = option.FriendlyValue,
                    Value = option.Value,
                    Rational = option.Rational,
                    Numerator = option.Numerator,
                    Denominator = option.Denominator,
                    IsEnabled = enabled,
                    DisableReason = enabled ? string.Empty : disableReason,
                    DisplayTextOverride = option.DisplayTextOverride
                };
            })
            .ToList();

        options = ShowAllCaptureOptions
            ? cappedOptions
                .Select(option =>
                {
                    if (option.IsEnabled || !IsSourceFilteredFrameRateDisableReason(option.DisableReason))
                    {
                        return option;
                    }

                    return new FrameRateOption
                    {
                        FriendlyValue = option.FriendlyValue,
                        Value = option.Value,
                        Rational = option.Rational,
                        Numerator = option.Numerator,
                        Denominator = option.Denominator,
                        IsEnabled = true,
                        DisableReason = string.Empty,
                        DisplayTextOverride = option.DisplayTextOverride
                    };
                })
                .ToList()
            : cappedOptions
                .Where(option => option.IsEnabled || !IsSourceFilteredFrameRateDisableReason(option.DisableReason))
                .ToList();
        var autoFrameRateOption = options.Count > 0
            ? new FrameRateOption
            {
                FriendlyValue = AutoFrameRateValue,
                Value = AutoFrameRateValue,
                IsEnabled = true,
                DisplayTextOverride = "Source"
            }
            : null;
        var availableOptions = autoFrameRateOption == null
            ? options
            : new[] { autoFrameRateOption }.Concat(options).ToList();
        DetectedSourceFrameRate = sourceRate.Rate;
        DetectedSourceFrameRateArg = sourceRate.Arg;
        SourceFrameRateOrigin = sourceRate.Origin;

        _isRebuildingModeOptions = true;
        try
        {
            AvailableFrameRates.Clear();
            foreach (var option in availableOptions)
            {
                AvailableFrameRates.Add(option);
            }

            FrameRateOption? selected = null;
            var selectAutoOption = autoFrameRateOption != null &&
                                   (IsAutoFrameRateSelected || !_hasUserOverriddenFrameRateForCurrentMode);
            if (selectAutoOption &&
                !IsHdrEnabled &&
                _pendingSdrAutoSelectionForDeviceChange &&
                _pendingSdrAutoFriendlyFrameRateBucket.HasValue)
            {
                selected = options.FirstOrDefault(option =>
                    option.IsEnabled && IsFriendlyFrameRateMatch(option.FriendlyValue, _pendingSdrAutoFriendlyFrameRateBucket.Value));
            }

            if (selected == null &&
                selectAutoOption &&
                sourceRate.Rate.HasValue)
            {
                selected = options
                    .Where(option => option.IsEnabled)
                    .OrderBy(option => Math.Abs(option.Value - sourceRate.Rate.Value))
                    .ThenBy(option =>
                        sourceTimingFamilyKnown &&
                        TryInferFrameRateTimingFamily(option.Rational, option.Value, out var optionFamily) &&
                        optionFamily == sourceTimingFamily
                            ? 0
                            : 1)
                    .FirstOrDefault();
            }

            if (selected == null)
            {
                selected = selectAutoOption
                    ? options.FirstOrDefault(option => option.IsEnabled)
                        ?? options.FirstOrDefault()
                    : options.FirstOrDefault(option =>
                        option.IsEnabled && IsFrameRateMatch(option.Value, previousRate))
                        ?? options.FirstOrDefault(option =>
                            option.IsEnabled && IsFriendlyFrameRateMatch(option.FriendlyValue, previousRate))
                        ?? options.FirstOrDefault(option =>
                            option.IsEnabled && IsFriendlyFrameRateMatch(option.FriendlyValue, 60))
                        ?? options.FirstOrDefault(option =>
                            option.IsEnabled && IsFriendlyFrameRateMatch(option.FriendlyValue, 30))
                        ?? options.FirstOrDefault(option => option.IsEnabled)
                        ?? options.FirstOrDefault();
            }

            if (autoFrameRateOption != null)
            {
                IsAutoFrameRateSelected = selectAutoOption;
            }
            var fallbackRate = previousRate > 0
                ? previousRate
                : 60;
            ApplyResolvedFrameRateSelection(selected, fallbackRate);
            if (IsHdrEnabled && selected is { IsEnabled: false })
            {
                StatusText = $"No HDR-capable frame rate is available for {GetSelectedResolutionDisplayText()}.";
            }

            if (!IsHdrEnabled && _pendingSdrAutoSelectionForDeviceChange && selected != null)
            {
                _pendingSdrAutoSelectionForDeviceChange = false;
                _pendingSdrAutoFriendlyFrameRateBucket = null;
            }
        }
        finally
        {
            _isApplyingAutomaticFrameRateSelection = false;
            _isRebuildingModeOptions = false;
        }

        UpdateSelectedFormat();
        UpdateTargetSummary();
        _forceSourceAutoRetarget = false;
    }

    private sealed record AutoCaptureSelection(
        ResolutionOption Resolution,
        int FriendlyFrameRate,
        double ExactFrameRate);

    private bool ShouldSelectAutoResolutionOption(string? previousSelection)
        => IsAutoResolutionValue(previousSelection) ||
           string.IsNullOrWhiteSpace(previousSelection) ||
           !_hasUserOverriddenResolutionForCurrentMode;

    private ResolutionOption CreateAutoResolutionOption()
        => new()
        {
            Value = AutoResolutionValue,
            Width = 0,
            Height = 0,
            IsEnabled = true,
            DisplayTextOverride = BuildAutoResolutionDisplayText()
        };

    private AutoCaptureSelection? ResolveAutoCaptureSelection(IReadOnlyList<ResolutionOption> options)
    {
        if (options.Count == 0)
        {
            return null;
        }

        var rankedOptions = options
            .OrderByDescending(option => (long)option.Width * option.Height)
            .ThenByDescending(option => option.Width)
            .ToList();
        var eligibleOptions = rankedOptions.Where(option => option.IsEnabled).ToList();
        if (eligibleOptions.Count == 0)
        {
            eligibleOptions = rankedOptions;
        }

        var sourceFriendlyCap = _latestSourceTelemetry.HasFrameRate
            ? (int?)Math.Round(_latestSourceTelemetry.FrameRateExact!.Value, MidpointRounding.AwayFromZero)
            : null;
        var friendlyBuckets = eligibleOptions
            .SelectMany(GetAutoEligibleFormats)
            .Select(format => GetFriendlyFrameRateBucket(format.FrameRateExact))
            .Distinct()
            .OrderByDescending(bucket => bucket)
            .ToList();
        if (friendlyBuckets.Count == 0)
        {
            return BuildAutoCaptureSelectionFallback(eligibleOptions);
        }

        var bestFriendlyBucket = friendlyBuckets
            .FirstOrDefault(bucket => !sourceFriendlyCap.HasValue || bucket <= sourceFriendlyCap.Value);
        if (bestFriendlyBucket == 0)
        {
            bestFriendlyBucket = friendlyBuckets[0];
        }

        var matchingResolutions = eligibleOptions
            .Where(option => ResolutionSupportsFriendlyFrameRate(
                option.Value,
                bestFriendlyBucket,
                hdrOnly: IsHdrEnabled,
                sdrOnly: !IsHdrEnabled))
            .ToList();
        if (matchingResolutions.Count == 0)
        {
            matchingResolutions = eligibleOptions;
        }

        var chosenResolution = SelectBestAutoResolutionCandidate(matchingResolutions) ?? eligibleOptions[0];
        var preferredFormat = SelectPreferredAutoFrameRateFormat(chosenResolution.Value, bestFriendlyBucket);
        return new AutoCaptureSelection(
            chosenResolution,
            GetFriendlyFrameRateBucket(preferredFormat.FrameRateExact),
            preferredFormat.FrameRateExact);
    }

    private AutoCaptureSelection? BuildAutoCaptureSelectionFallback(IReadOnlyList<ResolutionOption> options)
    {
        var fallback = options.FirstOrDefault();
        if (fallback == null)
        {
            return null;
        }

        var preferredBucket = GetMaxFrameRateFriendlyBucket(fallback.Value);
        var preferredFormat = SelectPreferredAutoFrameRateFormat(fallback.Value, preferredBucket);
        return new AutoCaptureSelection(
            fallback,
            GetFriendlyFrameRateBucket(preferredFormat.FrameRateExact),
            preferredFormat.FrameRateExact);
    }

    private IEnumerable<MediaFormat> GetAutoEligibleFormats(ResolutionOption option)
    {
        if (!_resolutionToFormats.TryGetValue(option.Value, out var formats))
        {
            return Enumerable.Empty<MediaFormat>();
        }

        var filtered = formats
            .Where(format => IsHdrEnabled ? IsHdrModeCandidate(format) : !IsHdrModeCandidate(format))
            .ToList();
        return filtered.Count > 0 ? filtered : formats;
    }

    private ResolutionOption? SelectBestAutoResolutionCandidate(IReadOnlyList<ResolutionOption> candidates)
    {
        if (candidates.Count == 0)
        {
            return null;
        }

        var ranked = candidates
            .OrderByDescending(option => (long)option.Width * option.Height)
            .ThenByDescending(option => option.Width)
            .ToList();
        if (!_latestSourceTelemetry.HasDimensions)
        {
            return ranked[0];
        }

        var sourceWidth = (uint)Math.Max(0, _latestSourceTelemetry.Width ?? 0);
        var sourceHeight = (uint)Math.Max(0, _latestSourceTelemetry.Height ?? 0);
        if (sourceWidth == 0 || sourceHeight == 0)
        {
            return ranked[0];
        }

        return ranked.FirstOrDefault(option => option.Width <= sourceWidth && option.Height <= sourceHeight)
            ?? ranked[0];
    }

    private MediaFormat SelectPreferredAutoFrameRateFormat(string resolutionKey, int preferredFriendlyBucket)
    {
        if (!_resolutionToFormats.TryGetValue(resolutionKey, out var formats) || formats.Count == 0)
        {
            throw new InvalidOperationException($"No formats are available for resolution '{resolutionKey}'.");
        }

        var timingFamily = FrameRateTimingFamily.Unknown;
        if (_latestSourceTelemetry.HasFrameRate &&
            TryInferFrameRateTimingFamily(_latestSourceTelemetry.FrameRateArg, _latestSourceTelemetry.FrameRateExact, out var sourceFamily))
        {
            timingFamily = sourceFamily;
        }

        var selectionPool = formats
            .Where(format =>
                (IsHdrEnabled ? IsHdrModeCandidate(format) : !IsHdrModeCandidate(format)) &&
                GetFriendlyFrameRateBucket(format.FrameRateExact) == preferredFriendlyBucket)
            .ToList();
        if (selectionPool.Count == 0)
        {
            selectionPool = formats
                .Where(format => GetFriendlyFrameRateBucket(format.FrameRateExact) == preferredFriendlyBucket)
                .ToList();
        }
        if (selectionPool.Count == 0)
        {
            selectionPool = formats.ToList();
            preferredFriendlyBucket = GetFriendlyFrameRateBucket(selectionPool.Max(format => format.FrameRateExact));
        }

        return SelectPreferredFrameRateFormat(selectionPool, preferredFriendlyBucket, timingFamily);
    }

    private int GetMaxFrameRateFriendlyBucket(string resolutionKey)
    {
        if (!_resolutionToFormats.TryGetValue(resolutionKey, out var formats) || formats.Count == 0)
        {
            return 0;
        }

        var filtered = formats
            .Where(format => !IsHdrEnabled || IsHdrModeCandidate(format))
            .ToList();
        if (filtered.Count == 0)
        {
            filtered = formats.ToList();
        }

        return filtered
            .Select(format => GetFriendlyFrameRateBucket(format.FrameRateExact))
            .DefaultIfEmpty()
            .Max();
    }

    private bool DoesResolutionMatchSourceAspectRatio(ResolutionOption option)
    {
        if (!_latestSourceTelemetry.HasDimensions)
        {
            return true;
        }

        var sourceWidth = (uint)Math.Max(0, _latestSourceTelemetry.Width ?? 0);
        var sourceHeight = (uint)Math.Max(0, _latestSourceTelemetry.Height ?? 0);
        if (sourceWidth == 0 || sourceHeight == 0 || option.Width == 0 || option.Height == 0)
        {
            return true;
        }

        var reducedSource = ReduceAspectRatio(sourceWidth, sourceHeight);
        var reducedOption = ReduceAspectRatio(option.Width, option.Height);
        return reducedSource.Width == reducedOption.Width &&
               reducedSource.Height == reducedOption.Height;
    }

    private static (uint Width, uint Height) ReduceAspectRatio(uint width, uint height)
    {
        if (width == 0 || height == 0)
        {
            return (width, height);
        }

        var divisor = GreatestCommonDivisor(width, height);
        return divisor == 0
            ? (width, height)
            : (width / divisor, height / divisor);
    }

    private static uint GreatestCommonDivisor(uint a, uint b)
    {
        while (b != 0)
        {
            var next = a % b;
            a = b;
            b = next;
        }

        return a;
    }

    private static bool IsSourceFilteredFrameRateDisableReason(string? disableReason)
        => !string.IsNullOrWhiteSpace(disableReason) &&
           (disableReason.IndexOf("higher capture fps", StringComparison.OrdinalIgnoreCase) >= 0 ||
            disableReason.IndexOf("duplicate variant", StringComparison.OrdinalIgnoreCase) >= 0 ||
            disableReason.IndexOf("not a clean divisor", StringComparison.OrdinalIgnoreCase) >= 0);

    private string BuildAutoResolutionDisplayText()
        => AutoResolutionValue;

    private void UpdateAutoResolutionState(AutoCaptureSelection? selection)
    {
        AutoResolvedWidth = selection?.Resolution.Width;
        AutoResolvedHeight = selection?.Resolution.Height;
        AutoResolvedFrameRate = selection?.ExactFrameRate;
    }

    private void ClearAutoResolutionState()
    {
        AutoResolvedWidth = null;
        AutoResolvedHeight = null;
        AutoResolvedFrameRate = null;
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

    private static bool IsAutoResolutionValue(string? resolutionValue)
        => string.Equals(resolutionValue, AutoResolutionValue, StringComparison.OrdinalIgnoreCase);

    private bool TryResolveResolutionKey(string? resolutionValue, out string resolutionKey)
    {
        resolutionKey = string.Empty;
        if (string.IsNullOrWhiteSpace(resolutionValue))
        {
            return false;
        }

        if (IsAutoResolutionValue(resolutionValue))
        {
            if (AutoResolvedWidth.HasValue &&
                AutoResolvedHeight.HasValue &&
                AutoResolvedWidth.Value > 0 &&
                AutoResolvedHeight.Value > 0)
            {
                resolutionKey = GetResolutionKey(AutoResolvedWidth.Value, AutoResolvedHeight.Value);
                return true;
            }

            return false;
        }

        if (!TryParseResolutionKey(resolutionValue, out var width, out var height))
        {
            return false;
        }

        resolutionKey = GetResolutionKey(width, height);
        return true;
    }

    private string? GetEffectiveResolutionKey(string? resolutionValue)
        => TryResolveResolutionKey(resolutionValue, out var resolutionKey)
            ? resolutionKey
            : null;

    private bool TryGetEffectiveResolutionSelection(out string resolutionKey, out uint width, out uint height)
    {
        resolutionKey = string.Empty;
        width = 0;
        height = 0;

        if (!TryResolveResolutionKey(SelectedResolution, out resolutionKey) ||
            !TryParseResolutionKey(resolutionKey, out width, out height))
        {
            resolutionKey = string.Empty;
            width = 0;
            height = 0;
            return false;
        }

        return true;
    }

    private void ResetFrameRateSelectionState()
    {
        _hasUserOverriddenFrameRateForCurrentMode = false;
        IsAutoFrameRateSelected = true;
    }

    private void ApplyResolvedFrameRateSelection(FrameRateOption? selected, double fallbackRate)
    {
        _isApplyingAutomaticFrameRateSelection = true;
        try
        {
            SelectedFrameRate = selected?.Value ?? fallbackRate;
        }
        finally
        {
            _isApplyingAutomaticFrameRateSelection = false;
        }

        SelectedFriendlyFrameRate = selected?.FriendlyValue ?? Math.Round(SelectedFrameRate);
        SelectedExactFrameRate = selected?.Value ?? SelectedFrameRate;
        SelectedExactFrameRateArg = selected?.Rational;
        if (IsAutoResolutionValue(SelectedResolution))
        {
            AutoResolvedFrameRate = selected?.Value ?? SelectedFrameRate;
        }

        DisabledFrameRateReason = selected is { IsEnabled: false }
            ? selected.DisableReason
            : string.Empty;
    }

    private void ResetModeSelectionState()
    {
        ResetFrameRateSelectionState();
        _hasUserOverriddenResolutionForCurrentMode = false;
        _forceSourceAutoRetarget = false;
        _lastSourceModeKey = null;
        _pendingSdrAutoSelectionForDeviceChange = false;
        _pendingSdrAutoFriendlyFrameRateBucket = null;
    }

    private ResolutionOption? TrySelectSourceResolutionOption(
        IReadOnlyList<ResolutionOption> options,
        string? previousSelection)
    {
        if (options.Count == 0 || !_latestSourceTelemetry.HasDimensions)
        {
            return null;
        }

        var sourceWidth = (uint)Math.Max(0, _latestSourceTelemetry.Width ?? 0);
        var sourceHeight = (uint)Math.Max(0, _latestSourceTelemetry.Height ?? 0);
        if (sourceWidth == 0 || sourceHeight == 0)
        {
            return null;
        }

        var exact = options.FirstOrDefault(option =>
            option.IsEnabled &&
            option.Width == sourceWidth &&
            option.Height == sourceHeight);
        if (exact != null)
        {
            return exact;
        }

        var sourceKey = GetResolutionKey(sourceWidth, sourceHeight);
        var enabled = options.Where(option => option.IsEnabled).ToList();
        if (enabled.Count == 0)
        {
            return options.FirstOrDefault();
        }

        return SelectNearestResolution(sourceKey, enabled)
            ?? SelectNearestResolution(previousSelection, enabled)
            ?? enabled.FirstOrDefault();
    }

    private ResolutionOption? SelectHdrResolutionOption(
        IReadOnlyList<ResolutionOption> options,
        string? previousSelection,
        double preferredFrameRate,
        out string? hint)
    {
        hint = null;
        if (options.Count == 0)
        {
            return null;
        }

        var previous = options.FirstOrDefault(option =>
            string.Equals(option.Value, previousSelection, StringComparison.OrdinalIgnoreCase));
        if (previous is { IsEnabled: true } &&
            ResolutionSupportsFrameRate(previous.Value, preferredFrameRate, hdrOnly: true))
        {
            hint = BuildHdrSupportHintForResolution(previous.Value);
            return previous;
        }

        var sameFpsCandidates = options
            .Where(option =>
                option.IsEnabled &&
                ResolutionSupportsFrameRate(option.Value, preferredFrameRate, hdrOnly: true))
            .ToList();

        var selected = SelectNearestResolution(previousSelection, sameFpsCandidates)
            ?? SelectNearestResolution(previousSelection, options.Where(option => option.IsEnabled).ToList())
            ?? options.FirstOrDefault(option => option.IsEnabled)
            ?? options.FirstOrDefault();

        if (!string.IsNullOrWhiteSpace(previousSelection) &&
            !string.Equals(previousSelection, selected?.Value, StringComparison.OrdinalIgnoreCase))
        {
            var previousMax = GetMaxFrameRateForResolution(previousSelection, hdrOnly: true);
            if (previousMax > 0)
            {
                hint = $"HDR at {previousSelection} supported up to {FormatFriendlyFrameRate(previousMax)} fps.";
            }
        }

        hint ??= BuildHdrSupportHintForResolution(selected?.Value);
        return selected;
    }

    private bool TrySelectSdrAutoResolutionOption(
        IReadOnlyList<ResolutionOption> options,
        out ResolutionOption? selected,
        out int selectedFriendlyBucket)
    {
        selected = null;
        selectedFriendlyBucket = 60;
        if (options.Count == 0)
        {
            return false;
        }

        var enabledOptions = options
            .Where(option => option.IsEnabled)
            .OrderByDescending(option => (long)option.Width * option.Height)
            .ToList();
        if (enabledOptions.Count == 0)
        {
            return false;
        }

        var sdrFriendlyBucketsByResolution = new Dictionary<string, HashSet<int>>(StringComparer.OrdinalIgnoreCase);
        foreach (var option in enabledOptions)
        {
            if (!_resolutionToFormats.TryGetValue(option.Value, out var formats))
            {
                continue;
            }

            var buckets = formats
                .Where(format => !IsHdrModeCandidate(format))
                .Select(format => GetFriendlyFrameRateBucket(format.FrameRateExact))
                .ToHashSet();
            if (buckets.Count > 0)
            {
                sdrFriendlyBucketsByResolution[option.Value] = buckets;
            }
        }

        if (sdrFriendlyBucketsByResolution.Count == 0)
        {
            return false;
        }

        foreach (var friendlyBucket in new[] { 60, 30 })
        {
            var match = enabledOptions.FirstOrDefault(option =>
                sdrFriendlyBucketsByResolution.TryGetValue(option.Value, out var buckets) &&
                buckets.Contains(friendlyBucket));
            if (match != null)
            {
                selected = match;
                selectedFriendlyBucket = friendlyBucket;
                return true;
            }
        }

        selected = enabledOptions.FirstOrDefault(option => sdrFriendlyBucketsByResolution.ContainsKey(option.Value));
        if (selected == null)
        {
            return false;
        }

        selectedFriendlyBucket = ResolvePreferredFriendlyBucketForResolution(selected.Value, sdrOnly: true) ?? 30;
        return true;
    }

    private static ResolutionOption? SelectNearestResolution(string? baselineResolution, IReadOnlyList<ResolutionOption> candidates)
    {
        if (candidates.Count == 0)
        {
            return null;
        }

        if (!TryParseResolutionKey(baselineResolution, out var baseWidth, out var baseHeight))
        {
            return candidates
                .OrderByDescending(option => (long)option.Width * option.Height)
                .FirstOrDefault();
        }

        var baseArea = (long)baseWidth * baseHeight;
        var lowerCandidate = candidates
            .Where(option => ((long)option.Width * option.Height) < baseArea)
            .OrderByDescending(option => (long)option.Width * option.Height)
            .FirstOrDefault();
        if (lowerCandidate != null)
        {
            return lowerCandidate;
        }

        return candidates
            .OrderBy(option => Math.Abs(((long)option.Width * option.Height) - baseArea))
            .ThenByDescending(option => (long)option.Width * option.Height)
            .FirstOrDefault();
    }

    private static bool TryParseResolutionKey(string? resolutionKey, out uint width, out uint height)
    {
        width = 0;
        height = 0;
        if (string.IsNullOrWhiteSpace(resolutionKey) || IsAutoResolutionValue(resolutionKey))
        {
            return false;
        }

        var parts = resolutionKey.Split('x');
        return parts.Length == 2 &&
               uint.TryParse(parts[0], out width) &&
               uint.TryParse(parts[1], out height);
    }

    private bool ResolutionSupportsFrameRate(string resolutionKey, double frameRate, bool hdrOnly)
    {
        if (frameRate <= 0)
        {
            return false;
        }

        var requestedBucket = GetFriendlyFrameRateBucket(frameRate);
        return ResolutionSupportsFriendlyFrameRate(
            resolutionKey,
            requestedBucket,
            hdrOnly: hdrOnly,
            sdrOnly: !hdrOnly);
    }

    private bool ResolutionSupportsFriendlyFrameRate(
        string resolutionKey,
        int friendlyBucket,
        bool hdrOnly,
        bool sdrOnly)
    {
        if (!_resolutionToFormats.TryGetValue(resolutionKey, out var formats))
        {
            return false;
        }

        return formats.Any(format =>
            (!hdrOnly || IsHdrModeCandidate(format)) &&
            (!sdrOnly || !IsHdrModeCandidate(format)) &&
            GetFriendlyFrameRateBucket(format.FrameRateExact) == friendlyBucket);
    }

    private int? ResolvePreferredFriendlyBucketForResolution(string resolutionKey, bool sdrOnly)
    {
        if (!_resolutionToFormats.TryGetValue(resolutionKey, out var formats))
        {
            return null;
        }

        var buckets = formats
            .Where(format => !sdrOnly || !IsHdrModeCandidate(format))
            .Select(format => GetFriendlyFrameRateBucket(format.FrameRateExact))
            .Distinct()
            .OrderByDescending(bucket => bucket)
            .ToList();
        if (buckets.Count == 0)
        {
            return null;
        }

        if (buckets.Contains(60))
        {
            return 60;
        }

        if (buckets.Contains(30))
        {
            return 30;
        }

        return buckets[0];
    }

    private bool ResolutionHasTimingFamilyVariant(
        string? resolutionKey,
        double friendlyFrameRate,
        FrameRateTimingFamily family)
    {
        if (family == FrameRateTimingFamily.Unknown ||
            string.IsNullOrWhiteSpace(resolutionKey) ||
            !_resolutionToFormats.TryGetValue(resolutionKey, out var formats))
        {
            return false;
        }

        var bucket = (int)Math.Round(friendlyFrameRate, MidpointRounding.AwayFromZero);
        foreach (var format in formats)
        {
            if (GetFriendlyFrameRateBucket(format.FrameRateExact) != bucket)
            {
                continue;
            }

            if (TryInferFrameRateTimingFamily(format.FrameRateRational, format.FrameRateExact, out var formatFamily) &&
                formatFamily == family)
            {
                return true;
            }
        }

        return false;
    }

    private double GetMaxFrameRateForResolution(string? resolutionKey, bool hdrOnly)
    {
        if (string.IsNullOrWhiteSpace(resolutionKey) ||
            !_resolutionToFormats.TryGetValue(resolutionKey, out var formats))
        {
            return 0;
        }

        var candidates = hdrOnly
            ? formats.Where(IsHdrModeCandidate).ToList()
            : formats;
        if (candidates.Count == 0)
        {
            return 0;
        }

        return candidates.Max(format => format.FrameRateExact);
    }

    private string BuildHdrSupportHintForResolution(string? resolutionKey)
    {
        if (!IsHdrEnabled || string.IsNullOrWhiteSpace(resolutionKey))
        {
            return string.Empty;
        }

        var maxHdrRate = GetMaxFrameRateForResolution(resolutionKey, hdrOnly: true);
        if (maxHdrRate <= 0)
        {
            return $"HDR is not supported at {resolutionKey}.";
        }

        if (SelectedFrameRate > 0 && maxHdrRate >= SelectedFrameRate - 0.01)
        {
            return string.Empty;
        }

        return $"HDR at {resolutionKey} supported up to {FormatFriendlyFrameRate(maxHdrRate)} fps.";
    }

    private static string FormatFriendlyFrameRate(double frameRate)
        => $"{Math.Round(frameRate):0}";

    private FrameRateTimingFamily ResolvePreferredTimingFamily(string? resolutionKey, double previousRate)
    {
        var runtime = _captureService.GetRuntimeSnapshot();
        if (TryParseResolutionKey(resolutionKey, out var selectedWidth, out var selectedHeight))
        {
            if (runtime.ActualWidth == selectedWidth &&
                runtime.ActualHeight == selectedHeight &&
                TryInferFrameRateTimingFamily(
                    runtime.ActualFrameRateArg ?? runtime.NegotiatedFrameRateArg,
                    runtime.ActualFrameRate ?? runtime.NegotiatedFrameRate,
                    out var runtimeFamily))
            {
                return runtimeFamily;
            }

            if (runtime.NegotiatedWidth == selectedWidth &&
                runtime.NegotiatedHeight == selectedHeight &&
                TryInferFrameRateTimingFamily(
                    runtime.NegotiatedFrameRateArg,
                    runtime.NegotiatedFrameRate,
                    out var negotiatedFamily))
            {
                return negotiatedFamily;
            }
        }

        if (TryInferFrameRateTimingFamily(SelectedFormat?.FrameRateRational, SelectedFormat?.FrameRateExact, out var selectedFamily))
        {
            return selectedFamily;
        }

        var selectedOption = AvailableFrameRates.FirstOrDefault(option => IsFrameRateMatch(option.Value, previousRate));
        if (selectedOption != null &&
            TryInferFrameRateTimingFamily(selectedOption.Rational, selectedOption.Value, out var optionFamily))
        {
            return optionFamily;
        }

        if (TryInferFrameRateTimingFamily(null, previousRate, out var previousFamily))
        {
            return previousFamily;
        }

        return FrameRateTimingFamily.Unknown;
    }

    private static MediaFormat SelectPreferredFrameRateFormat(
        IReadOnlyList<MediaFormat> candidates,
        int friendlyBucket,
        FrameRateTimingFamily timingFamily)
    {
        if (candidates.Count == 0)
        {
            throw new InvalidOperationException("No frame-rate candidates are available.");
        }

        return candidates
            .OrderBy(format => GetTimingFamilyRank(format, friendlyBucket, timingFamily))
            .ThenBy(format => Math.Abs(format.FrameRateExact - friendlyBucket))
            .ThenByDescending(IsHdrModeCandidate)
            .ThenBy(format => GetEffectivePixelFormatPriority(format))
            .First();
    }

    /// <summary>
    /// At 4K HFR (≥3840x2160 @ ≥100fps SDR), prefer MJPG over NV12. The UVC driver
    /// presents NV12 at these rates, but it's actually CPU-decoded MJPG causing frame
    /// drops. Selecting raw MJPG lets MF use GPU DXVA decode via hardware transforms.
    /// </summary>
    private static int GetEffectivePixelFormatPriority(MediaFormat format)
    {
        if (format.Width >= 3840 &&
            format.Height >= 2160 &&
            format.FrameRateExact >= 100 &&
            format.PixelFormat.Equals("MJPG", StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }

        return MediaFormat.GetPixelFormatPriority(format.PixelFormat);
    }

    private static int GetTimingFamilyRank(MediaFormat format, int friendlyBucket, FrameRateTimingFamily timingFamily)
    {
        if (format.FrameRateNumerator > 0 && format.FrameRateDenominator > 0)
        {
            return timingFamily switch
            {
                FrameRateTimingFamily.Ntsc1001 when format.FrameRateDenominator == 1001
                    => Math.Abs((int)format.FrameRateNumerator - friendlyBucket * 1000),
                FrameRateTimingFamily.Integer when format.FrameRateDenominator == 1
                    => Math.Abs((int)format.FrameRateNumerator - friendlyBucket),
                FrameRateTimingFamily.Ntsc1001 => 5000 + Math.Abs((int)format.FrameRateNumerator - friendlyBucket * 1000),
                FrameRateTimingFamily.Integer => 5000 + Math.Abs((int)format.FrameRateNumerator - friendlyBucket),
                _ => 100 + (int)Math.Round(Math.Abs(format.FrameRateExact - friendlyBucket) * 100)
            };
        }

        return 100 + (int)Math.Round(Math.Abs(format.FrameRateExact - friendlyBucket) * 100);
    }

    private static bool TryInferFrameRateTimingFamily(
        string? frameRateArg,
        double? frameRate,
        out FrameRateTimingFamily family)
    {
        family = FrameRateTimingFamily.Unknown;

        if (TryParseFrameRateRational(frameRateArg, out var numerator, out var denominator))
        {
            if (denominator == 1001)
            {
                family = FrameRateTimingFamily.Ntsc1001;
                return true;
            }

            if (denominator == 1)
            {
                family = FrameRateTimingFamily.Integer;
                return true;
            }
        }

        if (!frameRate.HasValue || frameRate.Value <= 0)
        {
            return false;
        }

        var value = frameRate.Value;
        var rounded = Math.Round(value);
        if (Math.Abs(value - rounded) <= 0.01)
        {
            family = FrameRateTimingFamily.Integer;
            return true;
        }

        var ntscCandidate = rounded * 1000.0 / 1001.0;
        if (Math.Abs(value - ntscCandidate) <= 0.03)
        {
            family = FrameRateTimingFamily.Ntsc1001;
            return true;
        }

        return false;
    }

    private (double? Rate, string? Arg, string Origin) ResolveDetectedSourceFrameRate(
        string? resolutionKey,
        IReadOnlyList<FrameRateOption> options,
        double previousRate)
    {
        if (_latestSourceTelemetry.HasFrameRate)
        {
            return (
                _latestSourceTelemetry.FrameRateExact,
                _latestSourceTelemetry.FrameRateArg,
                _latestSourceTelemetry.Origin != SourceTelemetryOrigin.Unknown
                    ? _latestSourceTelemetry.Origin.ToString()
                    : "SourceTelemetry");
        }

        var runtime = _captureService.GetRuntimeSnapshot();
        if (TryParseResolutionKey(resolutionKey, out var selectedWidth, out var selectedHeight))
        {
            if (runtime.ActualFrameRate.HasValue &&
                runtime.ActualWidth == selectedWidth &&
                runtime.ActualHeight == selectedHeight)
            {
                return (
                    runtime.ActualFrameRate,
                    runtime.ActualFrameRateArg ??
                    runtime.NegotiatedFrameRateArg,
                    "NegotiatedDeviceFormat");
            }

            if (runtime.NegotiatedFrameRate.HasValue &&
                runtime.NegotiatedWidth == selectedWidth &&
                runtime.NegotiatedHeight == selectedHeight)
            {
                return (
                    runtime.NegotiatedFrameRate,
                    runtime.NegotiatedFrameRateArg,
                    "NegotiatedDeviceFormat");
            }
        }

        if (SelectedFormat != null &&
            options.Any(option => IsFriendlyFrameRateMatch(option.FriendlyValue, SelectedFormat.FrameRateExact)))
        {
            return (
                SelectedFormat.FrameRateExact,
                string.IsNullOrWhiteSpace(SelectedFormat.FrameRateRational)
                    ? null
                    : SelectedFormat.FrameRateRational,
                "SelectedMode");
        }

        if (previousRate > 0 &&
            options.Any(option => IsFriendlyFrameRateMatch(option.FriendlyValue, previousRate)))
        {
            return (previousRate, null, "SelectedMode");
        }

        return (null, null, "Unknown");
    }

    private static bool IsFrameRateMatch(double a, double b, double tolerance = 0.01)
        => Math.Abs(a - b) < tolerance;

    private static bool IsFriendlyFrameRateMatch(double optionFriendlyRate, double requestedRate)
        => Math.Round(optionFriendlyRate) == Math.Round(requestedRate);

    private static bool IsAutoFrameRateValue(double value)
        => value == AutoFrameRateValue || value < 0;

    private static string GetResolutionKey(uint width, uint height)
        => $"{width}x{height}";

    private static int GetFriendlyFrameRateBucket(double frameRate)
        => (int)Math.Round(frameRate, MidpointRounding.AwayFromZero);

    private static bool TryParseFrameRateRational(string? rational, out uint numerator, out uint denominator)
    {
        numerator = 0;
        denominator = 0;
        if (string.IsNullOrWhiteSpace(rational))
        {
            return false;
        }

        var split = rational.Split('/');
        return split.Length == 2 &&
               uint.TryParse(split[0], out numerator) &&
               uint.TryParse(split[1], out denominator) &&
               denominator > 0;
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
        if (!_isChangingDevice && IsPreviewing && IsInitialized)
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

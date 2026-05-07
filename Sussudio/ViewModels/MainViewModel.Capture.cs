using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;
using Windows.Storage.Pickers;
using Sussudio.Services.Audio;
using Sussudio.Services.Automation;
using Sussudio.Services.Capture;
using Sussudio.Services.Configuration;
using Sussudio.Services.Flashback;
using Sussudio.Services.Gpu;
using Sussudio.Services.Preview;
using Sussudio.Services.Recording;
using Sussudio.Services.Runtime;
using Sussudio.Services.Telemetry;

namespace Sussudio.ViewModels;

/// <summary>
/// Capture lifecycle: device initialization, preview start/stop,
/// recording start/stop, settings builder, and device reinitialization.
/// </summary>
public partial class MainViewModel
{
    private async Task InitializeDeviceAsync(CancellationToken cancellationToken = default)
    {
        if (SelectedDevice == null)
        {
            Logger.Log("ERROR: SelectedDevice is NULL");
            return;
        }

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            StatusText = "Initializing device...";
            var settings = BuildCaptureSettings();
            Logger.Log(
                $"CAPTURE_INIT device='{SelectedDevice.Name}' id='{SelectedDevice.Id}' format={settings.Format} {settings.Width}x{settings.Height}@{settings.FrameRate} hdr={settings.HdrEnabled} audio={settings.AudioEnabled}");

            await _sessionCoordinator.InitializeAsync(SelectedDevice, settings, cancellationToken);

            IsInitialized = true;
            StatusText = "Device ready";
            Logger.Log("CAPTURE_INIT_READY");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            StatusText = "Device initialization canceled";
            IsInitialized = false;
            throw;
        }
        catch (Exception ex)
        {
            Logger.LogException(ex);
            StatusText = $"Failed to initialize: {ex.Message}";
            IsInitialized = false;
        }
    }

    public async Task StartPreviewAsync(bool userInitiated = true, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (userInitiated)
        {
            _cancelPreviewRestartAfterReinitialize = false;
        }

        PreviewStartRequested?.Invoke(this, EventArgs.Empty);
        Logger.Log($"PREVIEW_START requested initialized={IsInitialized} audio={IsAudioPreviewEnabled && IsAudioEnabled}");

        if (!IsInitialized)
        {
            await InitializeDeviceAsync(cancellationToken);
        }

        if (IsInitialized)
        {
            var settings = BuildCaptureSettings();
            await _sessionCoordinator.StartVideoPreviewAsync(settings, cancellationToken).ConfigureAwait(true);

            IsPreviewing = true;
            StatusText = "Preview starting...";

            if (IsAudioPreviewEnabled && IsAudioEnabled)
            {
                await _sessionCoordinator.StartAudioPreviewAsync(cancellationToken);
            }

            ApplySourceTelemetrySnapshot(_captureService.GetLatestSourceTelemetrySnapshot(), allowAutoRetarget: true);
            Logger.Log($"PREVIEW_START_READY audio={IsAudioPreviewEnabled && IsAudioEnabled}");
        }
        else
        {
            Logger.Log("Cannot start preview - device not initialized");
            StatusText = "Cannot start preview - device not initialized";
        }
    }

    public Task StopPreviewAsync()
        => StopPreviewAsync(userInitiated: true, teardownPipeline: false, CancellationToken.None);

    public Task StopPreviewAsync(bool userInitiated)
        => StopPreviewAsync(userInitiated, teardownPipeline: false, CancellationToken.None);

    public Task StopPreviewAsync(bool userInitiated, bool teardownPipeline)
        => StopPreviewAsync(userInitiated, teardownPipeline, CancellationToken.None);

    public async Task ApplySelectedDeviceAsync(CaptureDevice device, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (IsRecording)
        {
            StatusText = "Stop recording before switching capture devices.";
            return;
        }

        if (SelectedDevice != null &&
            string.Equals(SelectedDevice.Id, device.Id, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        Logger.Log($"DEVICE_APPLY_REQUEST device='{device.Name}' id='{device.Id}' preview={IsPreviewing} initialized={IsInitialized}");
        SelectedDevice = device;

        if (IsPreviewing)
        {
            await ReinitializeDeviceAsync("device selection apply").ConfigureAwait(true);
            return;
        }

        IsInitialized = false;
        StatusText = $"Selected device: {device.Name}";
    }

    public async Task StopPreviewAsync(bool userInitiated, bool teardownPipeline, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (userInitiated && IsPreviewReinitializing)
        {
            _cancelPreviewRestartAfterReinitialize = true;
        }

        if (userInitiated && !IsPreviewReinitializing && _captureService.IsAudioPreviewActive)
        {
            await RampPreviewVolumeDownForStopAsync(cancellationToken);
        }

        PreviewStopRequested?.Invoke(this, EventArgs.Empty);
        var commitStoppedState = false;
        try
        {
            if (teardownPipeline)
            {
                await _sessionCoordinator.StopVideoPreviewWithTeardownAsync(cancellationToken);
            }
            else
            {
                await _sessionCoordinator.StopVideoPreviewAsync(cancellationToken);
            }

            commitStoppedState = true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            commitStoppedState = true;
            throw;
        }
        finally
        {
            if (commitStoppedState)
            {
                IsPreviewing = false;
            }
        }

        // Stop audio preview
        if (_captureService.IsAudioPreviewActive)
        {
            if (teardownPipeline)
            {
                await _sessionCoordinator.StopAudioPreviewWithTeardownAsync(cancellationToken);
            }
            else
            {
                await _sessionCoordinator.StopAudioPreviewAsync(cancellationToken);
            }
        }

        if (!IsPreviewReinitializing)
        {
            StatusText = "Preview stopped";
        }
    }

    public Task ToggleRecordingAsync()
        => SetRecordingDesiredStateAsync(!IsRecording);

    private Task BeginRecordingTransitionAsync(bool enabled, CancellationToken cancellationToken = default)
    {
        if (enabled == IsRecording)
        {
            return Task.CompletedTask;
        }

        if (Interlocked.CompareExchange(ref _recordingToggleInProgress, 1, 0) != 0)
        {
            Logger.Log("Recording transition rejected: operation already in progress.");
            throw new InvalidOperationException("Recording transition already in progress.");
        }

        var task = RecordingTransitionInnerAsync(enabled, cancellationToken);
        Volatile.Write(ref _activeRecordingTransitionTarget, enabled ? 1 : 0);
        _activeRecordingToggleTask = task;
        _ = task.ContinueWith(completed =>
        {
            if (ReferenceEquals(_activeRecordingToggleTask, completed))
            {
                _activeRecordingToggleTask = null;
                Volatile.Write(ref _activeRecordingTransitionTarget, -1);
            }
        }, TaskScheduler.Default);

        return task;
    }

    private async Task RecordingTransitionInnerAsync(bool enabled, CancellationToken cancellationToken)
    {
        try
        {
            IsRecordingTransitioning = true;
            StatusText = enabled ? "Starting recording..." : "Stopping recording...";

            if (enabled)
            {
                await StartRecordingAsync(cancellationToken);
            }
            else
            {
                await StopRecordingAsync(cancellationToken);
            }

            if (IsRecording != enabled)
            {
                throw new InvalidOperationException(
                    $"Recording transition did not reach requested state: requested={enabled}, actual={IsRecording}.");
            }
        }
        finally
        {
            IsRecordingTransitioning = false;
            Interlocked.Exchange(ref _recordingToggleInProgress, 0);
        }
    }

    internal Task SetRecordingDesiredStateAsync(bool enabled, CancellationToken cancellationToken = default)
        => InvokeOnUiThreadAsync(() => SetRecordingDesiredStateOnUiThreadAsync(enabled, cancellationToken), cancellationToken);

    /// <summary>
    /// Graceful-stop entry point for callers that must NOT short-circuit on the
    /// toggle CAS gate (e.g. the window-close handler). If a toggle is in flight,
    /// await it; afterwards, if still recording, initiate a fresh stop.
    /// </summary>
    public Task StopRecordingAndWaitAsync(CancellationToken cancellationToken = default)
        => InvokeOnUiThreadAsync(() => StopRecordingAndWaitOnUiThreadAsync(cancellationToken), cancellationToken);

    internal Task StopRecordingForEmergencyAsync(CancellationToken cancellationToken = default)
        => _sessionCoordinator.StopRecordingAsync(cancellationToken);

    private Task StopRecordingAndWaitOnUiThreadAsync(CancellationToken cancellationToken)
        => SetRecordingDesiredStateOnUiThreadAsync(enabled: false, cancellationToken);

    private async Task SetRecordingDesiredStateOnUiThreadAsync(bool enabled, CancellationToken cancellationToken)
    {
        var inFlight = _activeRecordingToggleTask;
        if (inFlight != null && !inFlight.IsCompleted)
        {
            var inFlightTarget = Volatile.Read(ref _activeRecordingTransitionTarget);
            Exception? transitionError = null;
            try
            {
                await inFlight;
            }
            catch (OperationCanceledException ex)
            {
                transitionError = ex;
                Logger.Log($"Recording transition wait canceled: {ex.Message}");
            }
            catch (Exception ex)
            {
                transitionError = ex;
                Logger.Log($"Recording transition wait faulted: {ex.Message}");
            }

            cancellationToken.ThrowIfCancellationRequested();

            if (transitionError is OperationCanceledException transitionCanceled && inFlightTarget == (enabled ? 1 : 0))
            {
                throw transitionCanceled;
            }

            if (transitionError != null && inFlightTarget == (enabled ? 1 : 0))
            {
                throw new InvalidOperationException("Recording transition failed.", transitionError);
            }

            if (IsRecording == enabled)
            {
                return;
            }
        }

        cancellationToken.ThrowIfCancellationRequested();

        if (IsRecording == enabled)
        {
            return;
        }

        await BeginRecordingTransitionAsync(enabled, cancellationToken);
        if (IsRecording != enabled)
        {
            throw new InvalidOperationException(
                $"Recording transition did not reach requested state: requested={enabled}, actual={IsRecording}.");
        }
    }

    private async Task StartRecordingAsync(CancellationToken cancellationToken = default)
    {
        if (SelectedDevice == null)
        {
            StatusText = "No device selected";
            throw new InvalidOperationException(StatusText);
        }

        if (!IsInitialized)
        {
            await InitializeDeviceAsync(cancellationToken);
            if (!IsInitialized)
            {
                throw new InvalidOperationException(
                    string.IsNullOrWhiteSpace(StatusText)
                        ? "Device failed to initialize."
                        : StatusText);
            }
        }

        try
        {
            var settings = BuildCaptureSettings();
            await _sessionCoordinator.StartRecordingAsync(settings, cancellationToken);

            IsRecording = true;
            _recordingStopwatch.Restart();
            _bitrateSamples.Clear();
            RecordingSizeInfo = "0 B";
            RecordingBitrateInfo = "--";
            StatusText = "Recording...";
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            IsRecording = _sessionCoordinator.Snapshot.IsRecording;
            StatusText = "Recording start canceled";
            throw;
        }
        catch (Exception ex)
        {
            Logger.LogException(ex);
            IsRecording = _sessionCoordinator.Snapshot.IsRecording;
            StatusText = $"Recording failed: {ex.Message}";
            throw;
        }
    }

    private async Task StopRecordingAsync(CancellationToken cancellationToken = default)
    {
        // UX: Freeze the timer immediately when the user requests stop (finalization can take seconds).
        // Keep IsRecording true until the stop transition completes so the button remains in "Stop" state.
        _recordingStopwatch.Stop();

        try
        {
            await _sessionCoordinator.StopRecordingAsync(cancellationToken);
            IsRecording = false;
            StatusText = $"Recording saved ({RecordingTime})";
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            IsRecording = _sessionCoordinator.Snapshot.IsRecording;
            StatusText = "Stop recording canceled";
            throw;
        }
        catch (Exception ex)
        {
            Logger.LogException(ex);
            IsRecording = _sessionCoordinator.Snapshot.IsRecording;
            StatusText = $"Stop recording failed: {ex.Message}";
            throw;
        }
    }

    public async Task BrowseOutputPathAsync()
    {
        try
        {
            var picker = new FolderPicker();
            picker.SuggestedStartLocation = PickerLocationId.VideosLibrary;
            picker.FileTypeFilter.Add("*");

            // Initialize the picker with the window handle for WinUI 3
            WinRT.Interop.InitializeWithWindow.Initialize(picker, _windowHandle);

            var folder = await picker.PickSingleFolderAsync();
            if (folder != null)
            {
                OutputPath = folder.Path;
            }
        }
        catch (Exception ex)
        {
            StatusText = $"Error selecting folder: {ex.Message}";
        }
    }

    private async Task ReinitializeDeviceAsync(string reason)
    {
        if (SelectedDevice == null || SelectedFormat == null)
            return;

        var reinitializeGeneration = Interlocked.Increment(ref _previewReinitializeGeneration);
        await Task.Delay(PreviewReinitializeDebounceMs).ConfigureAwait(true);
        if (Volatile.Read(ref _previewReinitializeGeneration) != reinitializeGeneration)
        {
            Logger.Log($"REINIT_COALESCED reason='{reason}' generation={reinitializeGeneration}");
            return;
        }

        // If a flashback encoder cycle (codec/quality/bitrate change) is still
        // in progress, wait for it to release the session transition lock before
        // we attempt the reinit. Without this, the reinit can read stale encoder
        // settings or partially fail because the transition lock is contended.
        var pendingCycle = _pendingFlashbackCycleTask;
        if (pendingCycle != null)
        {
            try
            {
                await AwaitWithTimeoutAsync(
                    pendingCycle,
                    FlashbackCycleBeforeReinitializeTimeoutMs,
                    "Flashback encoder settings cycle before reinitialize").ConfigureAwait(false);
            }
            catch (TimeoutException ex)
            {
                Logger.Log($"REINIT_WAIT_FLASHBACK_CYCLE_TIMEOUT reason={reason} timeoutMs={FlashbackCycleBeforeReinitializeTimeoutMs}");
                StatusText = $"Failed to apply format: {ex.Message}";
                return;
            }
            catch (Exception ex)
            {
                Logger.Log($"REINIT_WAIT_FLASHBACK_CYCLE_FAULT reason={reason} type={ex.GetType().Name} msg='{ex.Message}'");
                // Cycle errors don't block reinit; the reinitialize path should still
                // converge the live preview to the current UI settings.
            }
            if (ReferenceEquals(_pendingFlashbackCycleTask, pendingCycle) && pendingCycle.IsCompleted)
            {
                _pendingFlashbackCycleTask = null;
            }
        }

        await _previewReinitializeGate.WaitAsync();
        var shouldRestartPreview = IsPreviewing;
        try
        {
            StatusText = "Applying new settings...";
            Logger.Log($"=== Reinitializing device ({reason}) ===");

            if (shouldRestartPreview)
            {
                IsPreviewReinitializing = true;
                _cancelPreviewRestartAfterReinitialize = false;
                await NotifyPreviewReinitRequestedAsync(reason);

                // Stop the D3D11 renderer BEFORE tearing down the capture pipeline.
                // The renderer shares the D3D11 device with the MF source reader via
                // SharedD3DDeviceManager. If the renderer is still calling
                // VideoProcessorBlt/Present when UnifiedVideoCapture.DisposeAsync
                // releases the source reader and DXGI device manager, the concurrent
                // native calls race and trigger an uncatchable AccessViolationException.
                // The flashback encoder drain (DisposeFlashbackPreviewBackendAsync)
                // widens this window to hundreds of milliseconds.
                await NotifyRendererStopAsync();
            }

            if (IsPreviewing)
            {
                // Reinit applies new device/format/codec settings — the existing flashback
                // backend is keyed to the OLD settings, so force a full teardown.
                await StopPreviewAsync(userInitiated: false, teardownPipeline: true);
            }

            // Reinitialize the device with new settings
            IsInitialized = false;
            Logger.LogFatalBreadcrumb($"REINIT phase=init_device reason={reason}");
            await InitializeDeviceAsync();
            Logger.LogFatalBreadcrumb($"REINIT phase=init_device_done reason={reason}");

            // Restart preview
            if (IsInitialized && shouldRestartPreview && !_cancelPreviewRestartAfterReinitialize)
            {
                Logger.LogFatalBreadcrumb($"REINIT phase=start_preview reason={reason}");
                await StartPreviewAsync(userInitiated: false);
                Logger.LogFatalBreadcrumb($"REINIT phase=start_preview_done reason={reason}");

                StatusText = $"Preview: {SelectedFormat.Width}x{SelectedFormat.Height}@{SelectedFormat.FrameRate}fps";
            }
        }
        catch (Exception ex)
        {
            Logger.LogException(ex);
            StatusText = $"Failed to apply format: {ex.Message}";
        }
        finally
        {
            _cancelPreviewRestartAfterReinitialize = false;
            if (shouldRestartPreview)
            {
                IsPreviewReinitializing = false;
            }
            _previewReinitializeGate.Release();
        }
    }

    private CaptureSettings BuildCaptureSettings()
    {
        var format = SelectedRecordingFormat switch
        {
            "HEVC" => RecordingFormat.HevcMp4,
            "AV1" => RecordingFormat.Av1Mp4,
            _ => RecordingFormat.H264Mp4
        };

        var quality = SelectedQuality switch
        {
            "Auto" => VideoQuality.Auto,
            "Low" => VideoQuality.Low,
            "Medium" => VideoQuality.Medium,
            "High" => VideoQuality.High,
            "Super High" => VideoQuality.SuperHigh,
            "Custom" => VideoQuality.Custom,
            _ => VideoQuality.High
        };

        var selectedFrameRateOption = AvailableFrameRates
            .FirstOrDefault(option => IsFrameRateMatch(option.Value, SelectedFrameRate))
            ?? AvailableFrameRates.FirstOrDefault(option => IsFriendlyFrameRateMatch(option.FriendlyValue, SelectedFrameRate));

        var requestedFrameRateArg = selectedFrameRateOption?.Rational;
        var requestedFrameRateNumerator = selectedFrameRateOption?.Numerator;
        var requestedFrameRateDenominator = selectedFrameRateOption?.Denominator;
        var effectiveFrameRate = IsAutoResolutionValue(SelectedResolution) && AutoResolvedFrameRate.HasValue && AutoResolvedFrameRate.Value > 0
            ? AutoResolvedFrameRate.Value
            : SelectedFrameRate > 0
            ? SelectedFrameRate
            : selectedFrameRateOption?.Value
                ?? SelectedFormat?.FrameRateExact
                ?? 60;
        var effectiveResolutionKnown = TryGetEffectiveResolutionSelection(out _, out var effectiveWidth, out var effectiveHeight);
        var runtime = _captureService.GetRuntimeSnapshot();
        var sourceTelemetry = _captureService.GetLatestSourceTelemetrySnapshot();
        var selectedFriendlyRate = selectedFrameRateOption?.FriendlyValue ?? effectiveFrameRate;
        var runtimeRate = runtime.ActualFrameRate ?? runtime.NegotiatedFrameRate;
        var runtimeRateArg = runtime.ActualFrameRateArg ?? runtime.NegotiatedFrameRateArg;
        var runtimeMatchesResolution = false;
        if (effectiveResolutionKnown)
        {
            runtimeMatchesResolution =
                (runtime.ActualWidth == effectiveWidth && runtime.ActualHeight == effectiveHeight) ||
                (runtime.NegotiatedWidth == effectiveWidth && runtime.NegotiatedHeight == effectiveHeight);
        }

        if (runtimeMatchesResolution &&
            runtimeRate.HasValue &&
            runtimeRate.Value > 0 &&
            IsFriendlyFrameRateMatch(selectedFriendlyRate, runtimeRate.Value))
        {
            if (!string.IsNullOrWhiteSpace(runtimeRateArg))
            {
                requestedFrameRateArg = runtimeRateArg;
            }

            if (runtime.NegotiatedFrameRateNumerator.HasValue &&
                runtime.NegotiatedFrameRateDenominator.HasValue &&
                runtime.NegotiatedFrameRateDenominator.Value > 0)
            {
                requestedFrameRateNumerator = runtime.NegotiatedFrameRateNumerator;
                requestedFrameRateDenominator = runtime.NegotiatedFrameRateDenominator;
            }
            else if (TryParseFrameRateRational(runtimeRateArg, out var runtimeNumerator, out var runtimeDenominator))
            {
                requestedFrameRateNumerator = runtimeNumerator;
                requestedFrameRateDenominator = runtimeDenominator;
            }
        }

        if (sourceTelemetry.HasFrameRate &&
            IsFriendlyFrameRateMatch(selectedFriendlyRate, sourceTelemetry.FrameRateExact ?? 0))
        {
            if (!string.IsNullOrWhiteSpace(sourceTelemetry.FrameRateArg))
            {
                requestedFrameRateArg = sourceTelemetry.FrameRateArg;
            }

            if (TryParseFrameRateRational(sourceTelemetry.FrameRateArg, out var sourceNumerator, out var sourceDenominator))
            {
                requestedFrameRateNumerator = sourceNumerator;
                requestedFrameRateDenominator = sourceDenominator;
            }
        }

        if ((requestedFrameRateNumerator == null || requestedFrameRateDenominator == null) &&
            TryParseFrameRateRational(requestedFrameRateArg, out var parsedNumerator, out var parsedDenominator))
        {
            requestedFrameRateNumerator = parsedNumerator;
            requestedFrameRateDenominator = parsedDenominator;
        }

        if (requestedFrameRateNumerator == null || requestedFrameRateDenominator == null)
        {
            if (SelectedFormat?.FrameRateNumerator > 0 && SelectedFormat.FrameRateDenominator > 0)
            {
                requestedFrameRateNumerator = SelectedFormat.FrameRateNumerator;
                requestedFrameRateDenominator = SelectedFormat.FrameRateDenominator;
                requestedFrameRateArg = SelectedFormat.FrameRateRational;
            }
            else
            {
                requestedFrameRateArg = effectiveFrameRate.ToString("0.###");
            }
        }

        var settings = new CaptureSettings
        {
            Width = effectiveResolutionKnown ? effectiveWidth : (SelectedFormat?.Width ?? 1920),
            Height = effectiveResolutionKnown ? effectiveHeight : (SelectedFormat?.Height ?? 1080),
            FrameRate = effectiveFrameRate,
            RequestedFrameRateArg = requestedFrameRateArg,
            RequestedFrameRateNumerator = requestedFrameRateNumerator,
            RequestedFrameRateDenominator = requestedFrameRateDenominator,
            RequestedPixelFormat = ResolveRequestedPixelFormat(),
            ForceMjpegDecode = ShouldForceMjpegDecode(),
            FlashbackGpuDecode = FlashbackGpuDecode,
            FlashbackBufferMinutes = FlashbackBufferMinutes,
            Format = format,
            Quality = quality,
            NvencPreset = SelectedPreset,
            SplitEncodeMode = SelectedSplitEncodeMode,
            CustomBitrateMbps = CustomBitrateMbps,
            HdrEnabled = IsHdrEnabled,
            HdrOutputMode = IsHdrEnabled ? HdrOutputMode.Hdr10Pq : HdrOutputMode.Off,
            PreviewMode = IsTrueHdrPreviewEnabled ? PreviewMode.TrueHdr : PreviewMode.GpuFast,
            OutputPath = OutputPath,
            AudioEnabled = IsAudioEnabled,
            MjpegDecoderCount = Math.Clamp(MjpegDecoderCount, 1, 8)
        };

        settings.UseCustomAudioInput = IsCustomAudioInputEnabled;
        if (IsCustomAudioInputEnabled && SelectedAudioInputDevice != null)
        {
            settings.AudioDeviceId = SelectedAudioInputDevice.Id;
            settings.AudioDeviceName = SelectedAudioInputDevice.Name;
        }

        settings.MicrophoneEnabled = IsMicrophoneEnabled;
        if (IsMicrophoneEnabled && SelectedMicrophoneDevice != null)
        {
            settings.MicrophoneDeviceId = SelectedMicrophoneDevice.Id;
            settings.MicrophoneDeviceName = SelectedMicrophoneDevice.Name;
        }

        return settings;
    }

    /// <summary>
    /// Resolves the pixel format to request from the source reader. On auto at
    /// 4K HFR, forces MJPG so the parallel decode pipeline is used instead of
    /// MF's single-pipeline internal MJPG→NV12 decode.
    /// </summary>
    private string? ResolveRequestedPixelFormat()
    {
        if (!string.Equals(SelectedVideoFormat, "Auto", StringComparison.OrdinalIgnoreCase))
        {
            return SelectedVideoFormat;
        }

        var format = SelectedFormat;
        if (format != null &&
            !IsHdrEnabled &&
            format.Width >= 3840 &&
            format.Height >= 2160 &&
            format.FrameRateExact >= 100)
        {
            return "MJPG";
        }

        return format?.PixelFormat;
    }

    private bool ShouldForceMjpegDecode()
    {
        if (string.Equals(SelectedVideoFormat, "MJPG", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // On auto at 4K HFR, force parallel MJPEG decode.
        if (string.Equals(SelectedVideoFormat, "Auto", StringComparison.OrdinalIgnoreCase))
        {
            var format = SelectedFormat;
            return format != null &&
                   !IsHdrEnabled &&
                   format.Width >= 3840 &&
                   format.Height >= 2160 &&
                   format.FrameRateExact >= 100;
        }

        return false;
    }
}

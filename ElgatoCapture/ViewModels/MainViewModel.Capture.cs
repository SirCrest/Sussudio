using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ElgatoCapture.Models;
using ElgatoCapture.Services;
using Windows.Storage.Pickers;

namespace ElgatoCapture.ViewModels;

/// <summary>
/// Capture lifecycle: device initialization, preview start/stop,
/// recording start/stop, settings builder, and device reinitialization.
/// </summary>
public partial class MainViewModel
{
    private async Task InitializeDeviceAsync()
    {
        Logger.Log("=== InitializeDeviceAsync BEGIN ===");

        if (SelectedDevice == null)
        {
            Logger.Log("ERROR: SelectedDevice is NULL");
            return;
        }

        Logger.Log($"Device: {SelectedDevice.Name} (ID: {SelectedDevice.Id})");

        try
        {
            StatusText = "Initializing device...";
            var settings = BuildCaptureSettings();
            Logger.Log($"Settings: {settings.Width}x{settings.Height} @ {settings.FrameRate}fps");
            Logger.Log($"Format: {settings.Format}, HDR: {settings.HdrEnabled}, Audio: {settings.AudioEnabled}");

            await _sessionCoordinator.InitializeAsync(SelectedDevice, settings);
            Logger.Log("CaptureService initialized");

            IsInitialized = true;
            StatusText = "Device ready";
        }
        catch (Exception ex)
        {
            Logger.LogException(ex);
            StatusText = $"Failed to initialize: {ex.Message}";
            IsInitialized = false;
        }

        Logger.Log("=== InitializeDeviceAsync END ===");
    }

    public async Task StartPreviewAsync(bool userInitiated = true)
    {
        if (userInitiated)
        {
            _cancelPreviewRestartAfterReinitialize = false;
        }

        PreviewStartRequested?.Invoke(this, EventArgs.Empty);
        Logger.Log($"StartPreviewAsync BEGIN IsInitialized={IsInitialized}");

        if (!IsInitialized)
        {
            Logger.Log("Device not initialized, initializing now...");
            await InitializeDeviceAsync();
        }

        Logger.Log($"After initialization - IsInitialized: {IsInitialized}");

        if (IsInitialized)
        {
            var settings = BuildCaptureSettings();
            await _sessionCoordinator.StartVideoPreviewAsync(settings).ConfigureAwait(true);

            Logger.Log("Setting IsPreviewing = true");
            IsPreviewing = true;
            StatusText = "Preview starting...";

            if (IsAudioPreviewEnabled && IsAudioEnabled)
            {
                Logger.Log("Starting audio preview...");
                await _sessionCoordinator.StartAudioPreviewAsync();
            }

            ApplySourceTelemetrySnapshot(_captureService.GetLatestSourceTelemetrySnapshot(), allowAutoRetarget: true);
        }
        else
        {
            Logger.Log("Cannot start preview - device not initialized");
            StatusText = "Cannot start preview - device not initialized";
        }

        Logger.Log("StartPreviewAsync END");
    }

    public async Task StopPreviewAsync(bool userInitiated = true)
    {
        if (userInitiated && IsPreviewReinitializing)
        {
            _cancelPreviewRestartAfterReinitialize = true;
        }

        PreviewStopRequested?.Invoke(this, EventArgs.Empty);
        await _sessionCoordinator.StopVideoPreviewAsync();
        IsPreviewing = false;

        // Stop audio preview
        if (_captureService.IsAudioPreviewActive)
        {
            await _sessionCoordinator.StopAudioPreviewAsync();
        }

        if (!IsPreviewReinitializing)
        {
            StatusText = "Preview stopped";
        }
    }

    public async Task ToggleRecordingAsync()
    {
        if (Interlocked.CompareExchange(ref _recordingToggleInProgress, 1, 0) != 0)
        {
            Logger.Log("Recording toggle rejected: operation already in progress.");
            return;
        }

        try
        {
            IsRecordingTransitioning = true;

            if (IsRecording)
            {
                StatusText = "Stopping recording...";
                await StopRecordingAsync();
            }
            else
            {
                StatusText = "Starting recording...";
                await StartRecordingAsync();
            }
        }
        finally
        {
            IsRecordingTransitioning = false;
            Interlocked.Exchange(ref _recordingToggleInProgress, 0);
        }
    }

    private async Task StartRecordingAsync()
    {
        if (SelectedDevice == null)
        {
            StatusText = "No device selected";
            return;
        }

        if (!IsInitialized)
        {
            await InitializeDeviceAsync();
        }

        try
        {
            var settings = BuildCaptureSettings();
            await _sessionCoordinator.StartRecordingAsync(settings);

            IsRecording = true;
            _recordingStopwatch.Restart();
            _bitrateSamples.Clear();
            RecordingSizeInfo = "0 B";
            RecordingBitrateInfo = "--";
            StatusText = "Recording...";
        }
        catch (Exception ex)
        {
            StatusText = $"Recording failed: {ex.Message}";
        }
    }

    private async Task StopRecordingAsync()
    {
        // UX: Freeze the timer immediately when the user requests stop (finalization can take seconds).
        // Keep IsRecording true until the stop transition completes so the button remains in "Stop" state.
        _recordingStopwatch.Stop();

        try
        {
            await _sessionCoordinator.StopRecordingAsync();
            IsRecording = false;
            StatusText = $"Recording saved ({RecordingTime})";
        }
        catch (Exception ex)
        {
            // Even if finalization fails, unblock the UI and allow subsequent attempts.
            IsRecording = false;
            StatusText = $"Stop recording failed: {ex.Message}";
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
            }

            if (IsPreviewing)
            {
                await StopPreviewAsync(userInitiated: false);
            }

            // Reinitialize the device with new settings
            IsInitialized = false;
            await InitializeDeviceAsync();

            // Restart preview
            if (IsInitialized && shouldRestartPreview && !_cancelPreviewRestartAfterReinitialize)
            {
                await StartPreviewAsync(userInitiated: false);

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
            RequestedPixelFormat = string.Equals(SelectedVideoFormat, "Auto", StringComparison.OrdinalIgnoreCase)
                ? SelectedFormat?.PixelFormat
                : SelectedVideoFormat,
            ForceMjpegDecode = string.Equals(SelectedVideoFormat, "MJPG", StringComparison.OrdinalIgnoreCase),
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
}

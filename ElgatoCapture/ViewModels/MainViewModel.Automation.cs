using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ElgatoCapture.Models;
using ElgatoCapture.Services;
using Microsoft.UI.Dispatching;
using Windows.Storage.Pickers;

namespace ElgatoCapture.ViewModels;

/// <summary>
/// Automation, snapshot/diagnostic queries, and flashback playback commands.
/// Implements IAutomationViewModel members consumed by the automation layer.
/// </summary>
public partial class MainViewModel
{
    // ── Snapshot / diagnostic one-liners ─────────────────────────────────

    public CaptureRuntimeSnapshot GetCaptureRuntimeSnapshot() => _captureService.GetRuntimeSnapshot();
    public CaptureHealthSnapshot GetCaptureHealthSnapshot() => _captureService.GetHealthSnapshot();
    public CaptureDiagnosticsSnapshot GetCaptureDiagnosticsSnapshot() => _captureService.GetDiagnosticsSnapshot();
    public RecordingStats GetRecordingStatsSnapshot() => _captureService.GetRecordingStats();
    internal ParallelMjpegDecodePipeline.PipelineTimingMetrics? GetMjpegPipelineTimingDetails()
        => _captureService.GetMjpegPipelineTimingDetails();
    public Task<CaptureRuntimeSnapshot> GetCaptureRuntimeSnapshotAsync(CancellationToken cancellationToken = default)
        => InvokeOnUiThreadAsync(() => _captureService.GetRuntimeSnapshot(), cancellationToken);
    public Task<CaptureHealthSnapshot> GetCaptureHealthSnapshotAsync(CancellationToken cancellationToken = default)
        => InvokeOnUiThreadAsync(() => _captureService.GetHealthSnapshot(), cancellationToken);
    public Task<RecordingStats> GetRecordingStatsSnapshotAsync(CancellationToken cancellationToken = default)
        => InvokeOnUiThreadAsync(() => _captureService.GetRecordingStats(), cancellationToken);
    public VideoSourceProbeResult ProbeVideoSource() => _captureService.ProbeVideoSource();
    public PreviewColorProbeResult ProbePreviewColor() => _captureService.ProbePreviewColor();
    public Task<PreviewFrameCaptureResult> CapturePreviewFrameAsync(string outputPath, CancellationToken cancellationToken = default) => _captureService.CapturePreviewFrameAsync(outputPath, cancellationToken);
    public CaptureSettings BuildCurrentSettings() => BuildCaptureSettings();

    // ── Flashback playback commands ──────────────────────────────────────

    internal FlashbackPlaybackController? FlashbackPlaybackController
        => _captureService.FlashbackPlaybackController;

    /// <summary>
    /// Returns the active flashback playback controller if it exists and is not disabled.
    /// </summary>
    private bool TryGetActiveFlashback([System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out FlashbackPlaybackController? controller)
    {
        controller = _captureService.FlashbackPlaybackController;
        return controller is { State: not FlashbackPlaybackState.Disabled };
    }

    public bool FlashbackBeginScrub(TimeSpan position)
    {
        if (!TryGetActiveFlashback(out var c)) return false;
        c.BeginScrub(position);
        return true;
    }

    public void FlashbackUpdateScrub(TimeSpan position)
    {
        if (TryGetActiveFlashback(out var c)) c.UpdateScrub(position);
    }

    public bool FlashbackEndScrub()
    {
        if (!TryGetActiveFlashback(out var c)) return false;
        c.EndScrub();
        return true;
    }

    public bool FlashbackPlay()
    {
        if (!TryGetActiveFlashback(out var c)) return false;
        c.Play();
        return true;
    }

    public bool FlashbackPause()
    {
        if (!TryGetActiveFlashback(out var c)) return false;
        c.Pause();
        return true;
    }

    public bool FlashbackGoLive()
    {
        if (!TryGetActiveFlashback(out var c)) return false;
        c.GoLive();
        return true;
    }

    public bool FlashbackNudge(TimeSpan delta)
    {
        if (!TryGetActiveFlashback(out var c)) return false;
        c.NudgePosition(delta);
        return true;
    }

    public void FlashbackSetInPoint()
        => _captureService.FlashbackPlaybackController?.SetInPoint();

    public void FlashbackSetOutPoint()
        => _captureService.FlashbackPlaybackController?.SetOutPoint();

    public void FlashbackClearInOutPoints()
        => _captureService.FlashbackPlaybackController?.ClearInOutPoints();

    /// <summary>
    /// Updates flashback buffer status properties from the buffer manager.
    /// Called from a periodic timer on the UI thread.
    /// </summary>
    public void UpdateFlashbackBufferStatus()
    {
        var bufferManager = _captureService.FlashbackBufferManager;
        if (bufferManager == null || !_captureService.IsFlashbackActive)
        {
            if (FlashbackState != FlashbackPlaybackState.Disabled)
                FlashbackState = FlashbackPlaybackState.Disabled;
            FlashbackBufferFillPercent = 0;
            FlashbackBufferFilledDuration = TimeSpan.Zero;
            FlashbackBufferDiskBytes = 0;
            FlashbackBitrateInfo = "";
            IsDiskWarningActive = false;
            _flashbackBitrateSamples.Clear();
            return;
        }

        var bufferDuration = bufferManager.Options.BufferDuration;
        var filledDuration = bufferManager.BufferedDuration;
        FlashbackBufferFilledDuration = filledDuration;
        FlashbackBufferDiskBytes = _captureService.FlashbackDiskBytes;
        FlashbackBufferFillPercent = bufferDuration.TotalSeconds > 0
            ? Math.Clamp(filledDuration.TotalSeconds / bufferDuration.TotalSeconds * 100, 0, 100)
            : 0;

        IsDiskWarningActive = bufferManager.IsDiskWarningActive;

        // Sample flashback output bytes for bitrate computation
        UpdateFlashbackBitrate();

        // Sync state from controller
        var controller = _captureService.FlashbackPlaybackController;
        if (controller != null)
        {
            FlashbackState = controller.State;
            // Don't overwrite UI-driven position during scrub
            if (controller.State != FlashbackPlaybackState.Scrubbing)
                FlashbackPlaybackPosition = controller.PlaybackPosition;
            FlashbackGapFromLive = controller.GapFromLive;
        }
        else if (FlashbackState == FlashbackPlaybackState.Disabled)
        {
            FlashbackState = FlashbackPlaybackState.Live;
        }

    }

    private void UpdateFlashbackBitrate()
    {
        var diskBytes = _captureService.FlashbackTotalBytesWritten;
        var now = Environment.TickCount64;
        _flashbackBitrateSamples.Enqueue((now, diskBytes));
        while (_flashbackBitrateSamples.Count > 0 && now - _flashbackBitrateSamples.Peek().Tick > BitrateWindowMs)
        {
            _flashbackBitrateSamples.Dequeue();
        }

        if (_flashbackBitrateSamples.Count >= 2)
        {
            var first = _flashbackBitrateSamples.Peek();
            var last = _flashbackBitrateSamples.Last();
            var deltaBytes = Math.Max(0, last.Bytes - first.Bytes);
            var deltaSeconds = Math.Max(0.001, (last.Tick - first.Tick) / 1000.0);
            var bitsPerSecond = (deltaBytes * 8.0) / deltaSeconds;
            FlashbackBitrateInfo = FormatBitrate(bitsPerSecond);
        }
        else
        {
            FlashbackBitrateInfo = "";
        }
    }

    public async Task ExportFlashbackAsync()
    {
        var bufferManager = _captureService.FlashbackBufferManager;
        if (bufferManager == null) return;

        var file = await PickFlashbackExportFileAsync($"Flashback_{DateTime.Now:yyyyMMdd_HHmmss}");
        if (file == null) return;

        var controller = _captureService.FlashbackPlaybackController;
        var inPoint = controller?.InPoint;
        var outPoint = controller?.OutPoint;

        var (result, errorMessage) = await ExportFlashbackCoreAsync(async (progress, ct) =>
            await _captureService.ExportFlashbackRangeAsync(inPoint, outPoint, file.Path, progress, ct));

        if (errorMessage != null)
        {
            StatusText = $"Export error: {errorMessage}";
        }
        else
        {
            StatusText = result!.Succeeded
                ? $"Export complete: {file.Path}"
                : $"Export failed: {result.StatusMessage}";
        }
    }

    public async Task SaveFlashbackLast5mAsync()
    {
        var bufferManager = _captureService.FlashbackBufferManager;
        if (bufferManager == null) return;

        var file = await PickFlashbackExportFileAsync($"Flashback_Last5m_{DateTime.Now:yyyyMMdd_HHmmss}");
        if (file == null) return;

        var (result, errorMessage) = await ExportFlashbackCoreAsync(async (progress, ct) =>
            await _captureService.ExportFlashbackLastNSecondsAsync(300, file.Path, progress, ct));

        if (errorMessage != null)
        {
            StatusText = $"Save error: {errorMessage}";
        }
        else
        {
            StatusText = result!.Succeeded
                ? $"Saved last 5 minutes: {file.Path}"
                : $"Save failed: {result.StatusMessage}";
        }
    }

    private async Task<Windows.Storage.StorageFile?> PickFlashbackExportFileAsync(string suggestedFileName)
    {
        var picker = new Windows.Storage.Pickers.FileSavePicker();
        picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.VideosLibrary;
        picker.FileTypeChoices.Add("MP4 Video", new[] { ".mp4" });
        picker.SuggestedFileName = suggestedFileName;
        WinRT.Interop.InitializeWithWindow.Initialize(picker, _windowHandle);
        return await picker.PickSaveFileAsync();
    }

    private async Task<(FinalizeResult? Result, string? ErrorMessage)> ExportFlashbackCoreAsync(
        Func<IProgress<ExportProgress>, CancellationToken, Task<FinalizeResult>> exportAction)
    {
        IsFlashbackExporting = true;
        FlashbackExportProgress = 0;
        try
        {
            var progress = new Progress<ExportProgress>(p =>
            {
                _dispatcherQueue.TryEnqueue(() => FlashbackExportProgress = p.Percent);
            });

            _exportCts?.Cancel();
            _exportCts = new CancellationTokenSource();
            var ct = _exportCts.Token;

            var result = await exportAction(progress, ct);
            return (result, null);
        }
        catch (Exception ex)
        {
            Logger.LogException(ex);
            return (null, ex.Message);
        }
        finally
        {
            IsFlashbackExporting = false;
            FlashbackExportProgress = 0;
        }
    }

    public async Task<FinalizeResult> ExportFlashbackAutomationAsync(
        double seconds, string outputPath, CancellationToken ct)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            IsFlashbackExporting = true;
            FlashbackExportProgress = 0;
        });
        try
        {
            var progress = new Progress<ExportProgress>(p =>
            {
                _dispatcherQueue.TryEnqueue(() => FlashbackExportProgress = p.Percent);
            });
            return await _captureService.ExportFlashbackLastNSecondsAsync(
                seconds, outputPath, progress, ct);
        }
        finally
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                IsFlashbackExporting = false;
                FlashbackExportProgress = 0;
            });
        }
    }

    public IReadOnlyList<FlashbackSegmentInfo> GetFlashbackSegments()
        => _captureService.GetFlashbackSegments();

    public void SetFlashbackEnabled(bool enabled) => _captureService.SetFlashbackEnabled(enabled);

    public async Task RestartFlashbackAsync()
    {
        await _captureService.RestartFlashbackAsync().ConfigureAwait(false);
        _flashbackBitrateSamples.Clear();
    }

    // ── ViewModel runtime snapshot ───────────────────────────────────────

    public Task<ViewModelRuntimeSnapshot> GetViewModelRuntimeSnapshotAsync(CancellationToken cancellationToken = default)
    {
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
            SourceTelemetryAgeSeconds = ComputeTelemetryAgeSeconds(SourceTelemetryTimestampUtc, DateTimeOffset.UtcNow),
            SourceTelemetrySummaryText = SourceTelemetrySummaryText,
            SourceTargetSummaryText = SourceTargetSummaryText,
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

    public Task<AutomationOptionsSnapshot> GetAutomationOptionsSnapshotAsync(CancellationToken cancellationToken = default)
    {
        return InvokeOnUiThreadAsync(() => new AutomationOptionsSnapshot
        {
            TimestampUtc = DateTimeOffset.UtcNow,
            Devices = Devices
                .Select(device => new AutomationDeviceOption
                {
                    Id = device.Id,
                    Name = device.Name,
                    IsSelected = string.Equals(device.Id, SelectedDevice?.Id, StringComparison.OrdinalIgnoreCase)
                })
                .ToArray(),
            AudioInputDevices = AudioInputDevices
                .Select(device => new AutomationDeviceOption
                {
                    Id = device.Id,
                    Name = device.Name,
                    IsSelected = string.Equals(device.Id, SelectedAudioInputDevice?.Id, StringComparison.OrdinalIgnoreCase)
                })
                .ToArray(),
            Resolutions = AvailableResolutions
                .Select(option => new AutomationResolutionOption
                {
                    Value = option.Value,
                    Width = (int)option.Width,
                    Height = (int)option.Height,
                    IsEnabled = option.IsEnabled,
                    DisableReason = option.DisableReason ?? string.Empty,
                    IsSelected = string.Equals(option.Value, SelectedResolution, StringComparison.OrdinalIgnoreCase)
                })
                .ToArray(),
            FrameRates = AvailableFrameRates
                .Select(option => new AutomationFrameRateOption
                {
                    Value = option.Value,
                    FriendlyValue = option.FriendlyValue,
                    ExactValueArg = option.Rational ?? string.Empty,
                    IsEnabled = option.IsEnabled,
                    DisableReason = option.DisableReason ?? string.Empty,
                    IsSelected = IsFrameRateMatch(option.Value, SelectedFrameRate)
                })
                .ToArray(),
            RecordingFormats = BuildStringOptions(AvailableRecordingFormats, SelectedRecordingFormat),
            Qualities = BuildStringOptions(AvailableQualities, SelectedQuality),
            Presets = BuildStringOptions(AvailablePresets, SelectedPreset),
            SplitEncodeModes = BuildStringOptions(AvailableSplitEncodeModes, SelectedSplitEncodeMode),
            VideoFormats = BuildStringOptions(AvailableVideoFormats, SelectedVideoFormat),
            MjpegDecoderCounts = Enumerable.Range(1, 8)
                .Select(value => new AutomationIntOption
                {
                    Value = value,
                    IsSelected = value == Math.Clamp(MjpegDecoderCount, 1, 8)
                })
                .ToArray(),
            SelectedDeviceId = SelectedDevice?.Id,
            SelectedAudioInputDeviceId = SelectedAudioInputDevice?.Id,
            SelectedResolution = SelectedResolution,
            SelectedFrameRate = SelectedFrameRate,
            SelectedRecordingFormat = SelectedRecordingFormat,
            SelectedQuality = SelectedQuality,
            SelectedPreset = SelectedPreset,
            SelectedSplitEncodeMode = SelectedSplitEncodeMode,
            SelectedVideoFormat = SelectedVideoFormat,
            MjpegDecoderCount = Math.Clamp(MjpegDecoderCount, 1, 8),
            ShowAllCaptureOptions = ShowAllCaptureOptions,
            PreviewVolumePercent = PreviewVolume * 100.0,
            IsStatsVisible = IsStatsVisible
        }, cancellationToken);
    }

    // ── Automation set methods (IAutomationViewModel) ────────────────────

    public Task RefreshDevicesForAutomationAsync(CancellationToken cancellationToken = default)
        => InvokeOnUiThreadAsync(() => RefreshDevicesAsync(), cancellationToken);

    public Task SelectDeviceAsync(string? deviceId, string? deviceName, CancellationToken cancellationToken = default)
    {
        return InvokeOnUiThreadAsync(() =>
        {
            var target = ResolveDevice(deviceId, deviceName);
            if (target == null)
            {
                throw new InvalidOperationException($"Capture device not found. Id='{deviceId ?? "(null)"}', Name='{deviceName ?? "(null)"}'.");
            }

            SelectedDevice = target;
            return Task.CompletedTask;
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
        return InvokeOnUiThreadAsync(() =>
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
            return Task.CompletedTask;
        }, cancellationToken);
    }

    public Task SetFrameRateAsync(double frameRate, CancellationToken cancellationToken = default)
    {
        return InvokeOnUiThreadAsync(() =>
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
                return Task.CompletedTask;
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
            return Task.CompletedTask;
        }, cancellationToken);
    }

    public Task SetVideoFormatAsync(string videoFormat, CancellationToken cancellationToken = default)
    {
        return InvokeOnUiThreadAsync(() =>
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
            return Task.CompletedTask;
        }, cancellationToken);
    }

    public Task SetRecordingFormatAsync(string format, CancellationToken cancellationToken = default)
    {
        return InvokeOnUiThreadAsync(() =>
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

            SelectedRecordingFormat = matched;
            return Task.CompletedTask;
        }, cancellationToken);
    }

    public Task SetPresetAsync(string preset, CancellationToken cancellationToken = default)
    {
        return InvokeOnUiThreadAsync(() =>
        {
            var matched = AvailablePresets.FirstOrDefault(value =>
                string.Equals(value, preset, StringComparison.OrdinalIgnoreCase));
            if (matched == null)
            {
                throw new InvalidOperationException($"Preset '{preset}' is not available.");
            }

            SelectedPreset = matched;
            return Task.CompletedTask;
        }, cancellationToken);
    }

    public Task SetSplitEncodeModeAsync(string splitEncodeMode, CancellationToken cancellationToken = default)
    {
        return InvokeOnUiThreadAsync(() =>
        {
            var matched = AvailableSplitEncodeModes.FirstOrDefault(value =>
                string.Equals(value, splitEncodeMode, StringComparison.OrdinalIgnoreCase));
            if (matched == null)
            {
                throw new InvalidOperationException($"Split encode mode '{splitEncodeMode}' is not available.");
            }

            SelectedSplitEncodeMode = matched;
            return Task.CompletedTask;
        }, cancellationToken);
    }

    public Task SetMjpegDecoderCountAsync(int decoderCount, CancellationToken cancellationToken = default)
    {
        return InvokeOnUiThreadAsync(() =>
        {
            MjpegDecoderCount = Math.Clamp(decoderCount, 1, 8);
            return Task.CompletedTask;
        }, cancellationToken);
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

    public Task SetQualityAsync(string quality, CancellationToken cancellationToken = default)
    {
        return InvokeOnUiThreadAsync(() =>
        {
            var matched = AvailableQualities.FirstOrDefault(value =>
                string.Equals(value, quality, StringComparison.OrdinalIgnoreCase));
            if (matched == null)
            {
                throw new InvalidOperationException($"Quality '{quality}' is not available.");
            }

            SelectedQuality = matched;
            return Task.CompletedTask;
        }, cancellationToken);
    }

    public Task SetCustomBitrateAsync(double bitrateMbps, CancellationToken cancellationToken = default)
    {
        return InvokeOnUiThreadAsync(() =>
        {
            CustomBitrateMbps = Math.Clamp(bitrateMbps, 1, 300);
            return Task.CompletedTask;
        }, cancellationToken);
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
                await StartPreviewAsync(userInitiated: true);
            }
            else
            {
                await StopPreviewAsync(userInitiated: true);
            }
        }, cancellationToken);
    }

    public Task SetRecordingEnabledAsync(bool enabled, CancellationToken cancellationToken = default)
    {
        return InvokeOnUiThreadAsync(async () =>
        {
            if (enabled == IsRecording)
            {
                return;
            }

            if (enabled)
            {
                await StartRecordingAsync();
            }
            else
            {
                await StopRecordingAsync();
            }
        }, cancellationToken);
    }

    public Task SetDeviceAudioModeAsync(string mode, CancellationToken cancellationToken = default)
    {
        return InvokeOnUiThreadAsync(() =>
        {
            SelectedDeviceAudioMode = mode;
            return Task.CompletedTask;
        }, cancellationToken);
    }

    public Task SetAnalogAudioGainAsync(double gainPercent, CancellationToken cancellationToken = default)
    {
        return InvokeOnUiThreadAsync(() =>
        {
            AnalogAudioGainPercent = Math.Clamp(gainPercent, 0.0, 100.0);
            return Task.CompletedTask;
        }, cancellationToken);
    }

    // ── Automation helpers ───────────────────────────────────────────────

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

    private static AutomationStringOption[] BuildStringOptions(
        IEnumerable<string> values,
        string selectedValue)
    {
        return values
            .Select(value => new AutomationStringOption
            {
                Value = value,
                Label = value,
                IsEnabled = true,
                DisableReason = string.Empty,
                IsSelected = string.Equals(value, selectedValue, StringComparison.OrdinalIgnoreCase)
            })
            .ToArray();
    }
}

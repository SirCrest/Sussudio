using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ElgatoCapture.Models;
using Microsoft.UI.Dispatching;
using Windows.Storage.Pickers;
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

    // ── Flashback playback commands ──────────────────────────────────────

    internal FlashbackPlaybackSnapshot GetFlashbackPlaybackSnapshot()
        => _sessionCoordinator.GetFlashbackPlaybackSnapshot();

    /// <summary>
    /// Returns the active flashback playback controller if it exists and is not disabled.
    /// </summary>
    public bool FlashbackBeginScrub(TimeSpan position)
    {
        return _sessionCoordinator.FlashbackBeginScrub(position);
    }

    public bool FlashbackSeek(TimeSpan position)
    {
        return _sessionCoordinator.FlashbackSeek(position);
    }

    public void FlashbackUpdateScrub(TimeSpan position)
    {
        _sessionCoordinator.FlashbackUpdateScrub(position);
    }

    public bool FlashbackEndScrub()
    {
        return _sessionCoordinator.FlashbackEndScrub();
    }

    public bool FlashbackPlay()
    {
        return _sessionCoordinator.FlashbackPlay();
    }

    public bool FlashbackPause()
    {
        return _sessionCoordinator.FlashbackPause();
    }

    public bool FlashbackGoLive()
    {
        return _sessionCoordinator.FlashbackGoLive();
    }

    public bool FlashbackNudge(TimeSpan delta)
    {
        return _sessionCoordinator.FlashbackNudge(delta);
    }

    public Task<bool> ExecuteFlashbackActionAsync(
        AutomationFlashbackAction action,
        TimeSpan? position = null,
        CancellationToken cancellationToken = default)
        => InvokeOnUiThreadAsync(() => ExecuteFlashbackAction(action, position), cancellationToken);

    private bool ExecuteFlashbackAction(AutomationFlashbackAction action, TimeSpan? position)
    {
        switch (action)
        {
            case AutomationFlashbackAction.Play:
                if (!FlashbackPlay())
                {
                    return false;
                }

                if (position.HasValue)
                {
                    FlashbackBeginScrub(position.Value);
                    FlashbackEndScrub();
                }

                return true;
            case AutomationFlashbackAction.Pause:
                return FlashbackPause();
            case AutomationFlashbackAction.GoLive:
                return FlashbackGoLive();
            case AutomationFlashbackAction.Seek:
                return FlashbackSeek(position ?? TimeSpan.Zero);
            case AutomationFlashbackAction.SetInPoint:
                return _sessionCoordinator.FlashbackSetInPoint().HasValue;
            case AutomationFlashbackAction.SetOutPoint:
                return _sessionCoordinator.FlashbackSetOutPoint().HasValue;
            case AutomationFlashbackAction.ClearInOutPoints:
                _sessionCoordinator.FlashbackClearInOutPoints();
                return true;
            default:
                throw new InvalidOperationException($"Unsupported flashback action '{action}'.");
        }
    }

    public TimeSpan? FlashbackSetInPoint()
        => _sessionCoordinator.FlashbackSetInPoint();

    public TimeSpan? FlashbackSetOutPoint()
        => _sessionCoordinator.FlashbackSetOutPoint();

    public void FlashbackClearInOutPoints()
        => _sessionCoordinator.FlashbackClearInOutPoints();

    /// <summary>
    /// Updates flashback buffer status properties from the buffer manager.
    /// Called from a periodic timer on the UI thread.
    /// </summary>
    public void UpdateFlashbackBufferStatus()
    {
        var bufferStatus = _sessionCoordinator.GetFlashbackBufferStatus();
        if (!bufferStatus.IsActive)
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

        FlashbackBufferFilledDuration = bufferStatus.FilledDuration;
        FlashbackBufferDiskBytes = bufferStatus.DiskBytes;
        FlashbackBufferFillPercent = bufferStatus.BufferDuration.TotalSeconds > 0
            ? Math.Clamp(bufferStatus.FilledDuration.TotalSeconds / bufferStatus.BufferDuration.TotalSeconds * 100, 0, 100)
            : 0;

        IsDiskWarningActive = bufferStatus.IsDiskWarningActive;

        // Sample flashback output bytes for bitrate computation
        UpdateFlashbackBitrate();

        // Sync state from controller
        var playback = _sessionCoordinator.GetFlashbackPlaybackSnapshot();
        if (playback.IsActive)
        {
            FlashbackState = playback.State;
            // Don't overwrite UI-driven position during scrub
            if (playback.State != FlashbackPlaybackState.Scrubbing)
                FlashbackPlaybackPosition = playback.PlaybackPosition;
            FlashbackGapFromLive = playback.GapFromLive;
        }
        else if (FlashbackState == FlashbackPlaybackState.Disabled)
        {
            FlashbackState = FlashbackPlaybackState.Live;
        }

    }

    private void UpdateFlashbackBitrate()
    {
        var diskBytes = _sessionCoordinator.FlashbackTotalBytesWritten;
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
        if (!_sessionCoordinator.IsFlashbackActive) return;

        var file = await PickFlashbackExportFileAsync($"Flashback_{DateTime.Now:yyyyMMdd_HHmmss}");
        if (file == null) return;

        var playback = _sessionCoordinator.GetFlashbackPlaybackSnapshot();
        var inPoint = playback.InPoint;
        var outPoint = playback.OutPoint;

        var (result, errorMessage, isCurrent) = await ExportFlashbackCoreAsync(async (progress, ct) =>
            await _sessionCoordinator.ExportFlashbackRangeAsync(inPoint, outPoint, file.Path, progress, ct));
        if (!isCurrent) return;

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
        if (!_sessionCoordinator.IsFlashbackActive) return;

        var file = await PickFlashbackExportFileAsync($"Flashback_Last5m_{DateTime.Now:yyyyMMdd_HHmmss}");
        if (file == null) return;

        var (result, errorMessage, isCurrent) = await ExportFlashbackCoreAsync(async (progress, ct) =>
            await _sessionCoordinator.ExportFlashbackLastNSecondsAsync(300, file.Path, progress, ct));
        if (!isCurrent) return;

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

    private async Task<(FinalizeResult? Result, string? ErrorMessage, bool IsCurrent)> ExportFlashbackCoreAsync(
        Func<IProgress<ExportProgress>, CancellationToken, Task<FinalizeResult>> exportAction)
    {
        // Export snapshots the flashback backend under CaptureService locks, then runs
        // outside the transition lock so long FFmpeg work does not block lifecycle commands.
        var exportId = Interlocked.Increment(ref _flashbackExportOperationId);
        var oldExportCts = _exportCts;
        CancelFlashbackExportCts(oldExportCts);
        _exportCts = new CancellationTokenSource();
        var exportCts = _exportCts;
        var ct = exportCts.Token;

        IsFlashbackExporting = true;
        FlashbackExportProgress = 0;
        try
        {
            var progress = new Progress<ExportProgress>(p =>
            {
                _dispatcherQueue.TryEnqueue(() =>
                {
                    if (IsCurrentFlashbackExport(exportId, exportCts))
                    {
                        FlashbackExportProgress = p.Percent;
                    }
                });
            });

            var result = await exportAction(progress, ct);
            return (result, null, IsCurrentFlashbackExport(exportId, exportCts));
        }
        catch (Exception ex)
        {
            Logger.LogException(ex);
            return (null, ex.Message, IsCurrentFlashbackExport(exportId, exportCts));
        }
        finally
        {
            if (IsCurrentFlashbackExport(exportId, exportCts))
            {
                IsFlashbackExporting = false;
                FlashbackExportProgress = 0;
                _exportCts = null;
                exportCts.Dispose();
            }
            else
            {
                exportCts.Dispose();
            }
        }
    }

    private bool IsCurrentFlashbackExport(int exportId, CancellationTokenSource exportCts)
        => Volatile.Read(ref _flashbackExportOperationId) == exportId && ReferenceEquals(_exportCts, exportCts);

    private static void CancelFlashbackExportCts(CancellationTokenSource? cts)
    {
        if (cts == null)
        {
            return;
        }

        try
        {
            cts.Cancel();
        }
        catch (ObjectDisposedException)
        {
            // A previous automation export may have completed on a background
            // thread while its UI cleanup was still queued.
        }
    }

    public async Task<FinalizeResult> ExportFlashbackAutomationAsync(
        double seconds, string outputPath, bool useSelectionRange, CancellationToken ct)
    {
        var exportId = Interlocked.Increment(ref _flashbackExportOperationId);
        var oldExportCts = _exportCts;
        CancelFlashbackExportCts(oldExportCts);
        _exportCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var exportCts = _exportCts;

        _dispatcherQueue.TryEnqueue(() =>
        {
            if (IsCurrentFlashbackExport(exportId, exportCts))
            {
                IsFlashbackExporting = true;
                FlashbackExportProgress = 0;
            }
        });
        try
        {
            var progress = new Progress<ExportProgress>(p =>
            {
                _dispatcherQueue.TryEnqueue(() =>
                {
                    if (IsCurrentFlashbackExport(exportId, exportCts))
                    {
                        FlashbackExportProgress = p.Percent;
                    }
                });
            });

            if (useSelectionRange)
            {
                var playback = _sessionCoordinator.GetFlashbackPlaybackSnapshot();
                return await _sessionCoordinator.ExportFlashbackRangeAsync(
                    playback.InPoint, playback.OutPoint, outputPath, progress, exportCts.Token);
            }

            return await _sessionCoordinator.ExportFlashbackLastNSecondsAsync(
                seconds, outputPath, progress, exportCts.Token);
        }
        finally
        {
            if (!_dispatcherQueue.TryEnqueue(() =>
            {
                try
                {
                    if (IsCurrentFlashbackExport(exportId, exportCts))
                    {
                        IsFlashbackExporting = false;
                        FlashbackExportProgress = 0;
                        _exportCts = null;
                    }
                }
                finally
                {
                    exportCts.Dispose();
                }
            }))
            {
                if (IsCurrentFlashbackExport(exportId, exportCts))
                {
                    _exportCts = null;
                }
                exportCts.Dispose();
            }
        }
    }

    public IReadOnlyList<FlashbackSegmentInfo> GetFlashbackSegments()
        => _sessionCoordinator.GetFlashbackSegments();

    public Task<IReadOnlyList<FlashbackSegmentInfo>> GetFlashbackSegmentsAsync(CancellationToken cancellationToken = default)
        => FromSynchronousSnapshot(GetFlashbackSegments, cancellationToken);

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
            CancellationToken.None).ConfigureAwait(false);
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
        => InvokeOnUiThreadAsync(() => RefreshDevicesAsync(cancellationToken), cancellationToken);

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
                cancellationToken)
            .ConfigureAwait(false);
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
                cancellationToken)
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
                cancellationToken)
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
                DeviceId: SelectedMicrophoneDevice?.Id,
                DeviceName: SelectedMicrophoneDevice?.Name),
            cancellationToken).ConfigureAwait(false);

        if (!request.IsRecording)
        {
            await _sessionCoordinator.UpdateMicrophoneMonitorAsync(
                enabled,
                request.DeviceId,
                request.DeviceName,
                cancellationToken).ConfigureAwait(false);
        }

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
            CancellationToken.None).ConfigureAwait(false);
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

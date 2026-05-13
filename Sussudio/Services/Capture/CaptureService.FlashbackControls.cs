using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.Services.Flashback;

namespace Sussudio.Services.Capture;

// Flashback-facing control surface: public state, enable/settings mutations,
// restarts, and encoder-setting cycles. Backend resource construction remains
// in the Flashback orchestration partial.
public partial class CaptureService
{
    public bool IsFlashbackActive => _flashbackSink != null;
    public TimeSpan FlashbackBufferedDuration => _flashbackBufferManager?.BufferedDuration ?? TimeSpan.Zero;
    public long FlashbackDiskBytes => _flashbackBufferManager?.TotalDiskBytes ?? 0;
    public int FlashbackSegmentCount => _flashbackBufferManager?.SegmentCount ?? 0;
    internal FlashbackPlaybackController? FlashbackPlaybackController => _flashbackPlaybackController;
    internal FlashbackBufferManager? FlashbackBufferManager => _flashbackBufferManager;
    public long FlashbackOutputBytes => _flashbackSink?.OutputBytes ?? 0;
    public long FlashbackTotalBytesWritten => _flashbackBufferManager?.TotalBytesWritten ?? 0;
    public string? EncoderCodecName => _flashbackSink?.CodecName;
    public uint EncoderTargetBitRate => _flashbackSink?.TargetBitRate ?? 0;
    public int EncoderWidth => _flashbackSink?.EncoderWidth ?? 0;
    public int EncoderHeight => _flashbackSink?.EncoderHeight ?? 0;
    public double EncoderFrameRate => _flashbackSink?.EncoderFrameRate ?? 0;
    public FinalizeResult? LastExportResult => _lastExportResult;

    internal IReadOnlyList<FlashbackSegmentInfo> GetFlashbackSegments()
    {
        return _flashbackBufferManager?.GetSegmentInfoList()
            ?? Array.Empty<FlashbackSegmentInfo>();
    }

    public Task SetFlashbackEnabledAsync(bool enabled, CancellationToken cancellationToken = default)
        => RunTransitionAsync(_sessionState, async transitionToken =>
        {
            if (_isRecording && IsFlashbackRecordingBackendActive() && !enabled)
            {
                Logger.Log("FLASHBACK_DISABLE_BLOCKED reason=recording_active");
                throw new InvalidOperationException("Cannot disable Flashback while Flashback recording is active.");
            }

            if (_flashbackEnabled == enabled)
            {
                if (enabled && (_flashbackSink != null || _isRecording))
                {
                    return;
                }

                if (!enabled && !_flashbackBackend.HasAnyResource)
                {
                    return;
                }
            }

            _flashbackEnabled = enabled;
            if (!enabled)
            {
                _pendingFlashbackEnableAfterRecording = false;
                await DisposeFlashbackPreviewBackendAsync(transitionToken, purgeSegments: true).ConfigureAwait(false);
                if (!_isVideoPreviewActive && !_isAudioPreviewActive && !_isRecording)
                {
                    await DisposePreviewPipelineAsync(transitionToken, purgeFlashbackSegments: false).ConfigureAwait(false);
                }
                return;
            }

            if (_isRecording)
            {
                _pendingFlashbackEnableAfterRecording = true;
                Logger.Log("FLASHBACK_ENABLE_DEFERRED reason=recording_active");
                return;
            }

            _pendingFlashbackEnableAfterRecording = false;
            if (_unifiedVideoCapture != null && _currentSettings != null)
            {
                try
                {
                    await EnsureFlashbackPreviewBackendAsync(_unifiedVideoCapture, _currentSettings, transitionToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException ex) when (transitionToken.IsCancellationRequested)
                {
                    _flashbackEnabled = false;
                    _pendingFlashbackEnableAfterRecording = false;
                    if (_flashbackBackend.HasAnyResource)
                    {
                        await DisposeFlashbackPreviewBackendAsync(CancellationToken.None, purgeSegments: true).ConfigureAwait(false);
                    }
                    Logger.Log($"FLASHBACK_ENABLE_IMMEDIATE_CANCELLED type={ex.GetType().Name} error='{ex.Message}'");
                    throw;
                }
                catch (Exception ex)
                {
                    _flashbackEnabled = false;
                    _pendingFlashbackEnableAfterRecording = false;
                    if (_flashbackBackend.HasAnyResource)
                    {
                        await DisposeFlashbackPreviewBackendAsync(CancellationToken.None, purgeSegments: true).ConfigureAwait(false);
                    }
                    Logger.Log($"FLASHBACK_ENABLE_IMMEDIATE_FAIL type={ex.GetType().Name} error='{ex.Message}'");
                    throw;
                }
            }
        }, cancellationToken);

    /// <summary>
    /// Updates flashback-specific fields in the active capture settings without
    /// requiring a full session restart. Call before <see cref="RestartFlashbackAsync"/>
    /// so the rebuild uses the latest values.
    /// </summary>
    // REVIEWED 2026-04-07: called from UI thread only; values are independent scalars
    // so a stale read from a background thread produces a slightly-off config, not a crash.
    // RestartFlashbackAsync (which consumes these) acquires _sessionTransitionLock.
    public Task UpdateFlashbackSettingsAsync(
        int bufferMinutes,
        bool gpuDecode,
        CancellationToken cancellationToken = default)
        => RunTransitionAsync(_sessionState, transitionToken =>
        {
            if (_currentSettings != null)
            {
                _currentSettings.FlashbackBufferMinutes = bufferMinutes;
                _currentSettings.FlashbackGpuDecode = gpuDecode;
            }

            if (_flashbackPlaybackController != null)
            {
                _flashbackPlaybackController.GpuDecodeEnabled = gpuDecode;
            }

            if (_isRecording && IsFlashbackRecordingBackendActive())
            {
                _pendingFlashbackSettingsChange = true;
            }

            return Task.CompletedTask;
        }, cancellationToken);

    /// <summary>
    /// Updates encoding-related fields in the active capture settings so that
    /// <see cref="RestartFlashbackAsync"/> picks up the latest bitrate/quality/preset.
    /// Must only be called from within a <see cref="RunTransitionAsync"/> delegate
    /// (i.e. with <c>_sessionTransitionLock</c> held) to prevent concurrent UI toggles
    /// from tearing <c>_currentSettings</c> between the snapshot and the encoder rebuild.
    /// </summary>
    // REVIEWED 2026-05-11: method is private; the only call site is RestartFlashbackAsync(settings),
    // which already executes inside RunTransitionAsync and therefore holds _sessionTransitionLock.
    // Making this public (as it was before) allowed any caller to bypass the transition gate and
    // race with concurrent flashback restarts - the root cause of the rapid-settings segment-purge
    // data loss (Gate 4 #1, Gate 2 Section 551/553). SemaphoreSlim is not re-entrant, so we must NOT
    // acquire the lock here; callers are responsible for holding it (enforced by private access).
    private void UpdateEncodingSettings(CaptureSettings source)
    {
        if (_currentSettings == null) return;
        _currentSettings.Format = source.Format;
        _currentSettings.Quality = source.Quality;
        _currentSettings.NvencPreset = source.NvencPreset;
        _currentSettings.CustomBitrateMbps = source.CustomBitrateMbps;
        _currentSettings.AudioEnabled = source.AudioEnabled;
        _currentSettings.MicrophoneEnabled = source.MicrophoneEnabled;
        _currentSettings.MicrophoneDeviceId = source.MicrophoneDeviceId;
        _currentSettings.MicrophoneDeviceName = source.MicrophoneDeviceName;
        _currentSettings.FlashbackBufferMinutes = source.FlashbackBufferMinutes;
        _currentSettings.FlashbackGpuDecode = source.FlashbackGpuDecode;
        // If a flashback-backed recording is active, the restart will be deferred -
        // flag it so the stop-recording path knows to do a full rebuild.
        if (_isRecording && IsFlashbackRecordingBackendActive())
            _pendingFlashbackSettingsChange = true;
    }

    /// <summary>
    /// Tears down the running flashback encoder and buffer, then rebuilds
    /// with current settings. Purges all existing segments because encoding
    /// parameters (bitrate, codec, etc.) may have changed.
    /// </summary>
    public Task RestartFlashbackAsync(CancellationToken cancellationToken = default)
        => RunTransitionAsync(_sessionState, async transitionToken =>
        {
            if (_isRecording && IsFlashbackRecordingBackendActive())
            {
                Logger.Log("FLASHBACK_RESTART_BLOCKED reason=recording_active");
                throw new InvalidOperationException("Cannot restart Flashback while Flashback recording is active.");
            }

            await RestartFlashbackCoreAsync(transitionToken).ConfigureAwait(false);
        }, cancellationToken);

    public Task RestartFlashbackAsync(CaptureSettings settings, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return RunTransitionAsync(_sessionState, async transitionToken =>
        {
            if (_isRecording && IsFlashbackRecordingBackendActive())
            {
                Logger.Log("FLASHBACK_RESTART_BLOCKED reason=recording_active");
                throw new InvalidOperationException("Cannot restart Flashback while Flashback recording is active.");
            }

            UpdateEncodingSettings(settings);
            await RestartFlashbackCoreAsync(transitionToken).ConfigureAwait(false);
        }, cancellationToken);
    }

    /// <summary>
    /// Updates the recording format and cycles the flashback encoder so the buffer
    /// uses the new codec. No-op if not previewing or if a recording is active.
    /// </summary>
    public Task UpdateRecordingFormatAsync(RecordingFormat format, CancellationToken cancellationToken = default)
        => RunTransitionAsync(_sessionState, async transitionToken =>
        {
            if (_currentSettings == null || format == _currentSettings.Format)
                return;

            var previousSettings = CloneCaptureSettings(_currentSettings);
            if (_isRecording)
            {
                Logger.Log($"FLASHBACK_FORMAT_CHANGE_BLOCKED reason=recording_active format={format}");
                _currentSettings.Format = format;
                if (IsFlashbackRecordingBackendActive())
                    _pendingFlashbackSettingsChange = true;
                return;
            }

            _currentSettings.Format = format;

            var cycleFailed = false;
            if (_flashbackSink != null)
            {
                try
                {
                    await CycleFlashbackBufferAsync(transitionToken, purgeSegments: true).ConfigureAwait(false);
                }
                catch (OperationCanceledException ex) when (transitionToken.IsCancellationRequested)
                {
                    Logger.Log($"FLASHBACK_FORMAT_CHANGE_CYCLE_CANCELLED format={format} type={ex.GetType().Name} error='{ex.Message}'");
                    throw;
                }
                catch (Exception ex)
                {
                    cycleFailed = true;
                    Logger.Log($"FLASHBACK_FORMAT_CHANGE_CYCLE_FAIL format={format} type={ex.GetType().Name} error='{ex.Message}'");
                }
            }

            if (!cycleFailed)
            {
                Logger.Log($"FLASHBACK_FORMAT_CHANGE_OK format={format}");
            }
            else
            {
                _currentSettings = previousSettings;
                Logger.Log($"FLASHBACK_FORMAT_CHANGE_ROLLBACK format={format} restored={_currentSettings.Format}");
            }
        }, cancellationToken);

    /// <summary>
    /// Cycles the flashback encoder when encoder-affecting settings change
    /// (bitrate, quality, preset, split encode). Updates <see cref="_currentSettings"/> and
    /// restarts the flashback buffer so new recordings use the updated params.
    /// No-op if not previewing or recording is active.
    /// </summary>
    public Task CycleFlashbackEncoderSettingsAsync(
        VideoQuality? quality = null,
        double? customBitrateMbps = null,
        string? nvencPreset = null,
        string? splitEncodeMode = null,
        CancellationToken cancellationToken = default)
        => RunTransitionAsync(_sessionState, async transitionToken =>
        {
            if (_currentSettings == null) return;

            var previousSettings = CloneCaptureSettings(_currentSettings);
            var changed = false;
            if (quality.HasValue && quality.Value != _currentSettings.Quality)
            {
                _currentSettings.Quality = quality.Value;
                changed = true;
            }
            if (customBitrateMbps.HasValue && Math.Abs(customBitrateMbps.Value - _currentSettings.CustomBitrateMbps) > 0.01)
            {
                _currentSettings.CustomBitrateMbps = customBitrateMbps.Value;
                changed = true;
            }
            if (nvencPreset != null)
            {
                var parsedPreset = NvencPresetParser.Parse(nvencPreset);
                if (parsedPreset != _currentSettings.NvencPreset)
                {
                    _currentSettings.NvencPreset = parsedPreset;
                    changed = true;
                }
            }
            if (splitEncodeMode != null)
            {
                var parsedSplitMode = SplitEncodeModeParser.Parse(splitEncodeMode);
                if (parsedSplitMode != _currentSettings.SplitEncodeMode)
                {
                    _currentSettings.SplitEncodeMode = parsedSplitMode;
                    changed = true;
                }
            }

            if (!changed) return;

            if (_isRecording)
            {
                Logger.Log("FLASHBACK_ENCODER_SETTINGS_CHANGE_BLOCKED reason=recording_active");
                if (IsFlashbackRecordingBackendActive())
                    _pendingFlashbackSettingsChange = true;
                return;
            }

            var cycledBuffer = _flashbackSink != null;
            var cycleFailed = false;
            if (_flashbackSink != null)
            {
                try
                {
                    await CycleFlashbackBufferAsync(transitionToken, purgeSegments: true).ConfigureAwait(false);
                }
                catch (OperationCanceledException ex) when (transitionToken.IsCancellationRequested)
                {
                    Logger.Log($"FLASHBACK_ENCODER_SETTINGS_CHANGE_CYCLE_CANCELLED quality={_currentSettings.Quality} bitrate={_currentSettings.CustomBitrateMbps} preset={_currentSettings.NvencPreset} split={_currentSettings.SplitEncodeMode} type={ex.GetType().Name} error='{ex.Message}'");
                    throw;
                }
                catch (Exception ex)
                {
                    cycleFailed = true;
                    Logger.Log($"FLASHBACK_ENCODER_SETTINGS_CHANGE_CYCLE_FAIL quality={_currentSettings.Quality} bitrate={_currentSettings.CustomBitrateMbps} preset={_currentSettings.NvencPreset} split={_currentSettings.SplitEncodeMode} type={ex.GetType().Name} error='{ex.Message}'");
                }
            }

            if (!cycleFailed)
            {
                Logger.Log($"FLASHBACK_ENCODER_SETTINGS_CHANGE_OK quality={_currentSettings.Quality} bitrate={_currentSettings.CustomBitrateMbps} preset={_currentSettings.NvencPreset} split={_currentSettings.SplitEncodeMode} cycled={cycledBuffer}");
            }
            else
            {
                _currentSettings = previousSettings;
                Logger.Log($"FLASHBACK_ENCODER_SETTINGS_CHANGE_ROLLBACK quality={_currentSettings.Quality} bitrate={_currentSettings.CustomBitrateMbps} preset={_currentSettings.NvencPreset} split={_currentSettings.SplitEncodeMode}");
            }
        }, cancellationToken);
}

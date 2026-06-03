using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.Services.Audio;
using Sussudio.Services.Flashback;
using Sussudio.Services.Recording;
using Sussudio.Services.Runtime;

namespace Sussudio.Services.Capture;

// Flashback service-level state, enable/restart/cycle, and recording backend lifecycle.
public partial class CaptureService
{
    public bool IsFlashbackActive => _flashbackBackend.Sink != null;
    public TimeSpan FlashbackBufferedDuration => _flashbackBackend.BufferManager?.BufferedDuration ?? TimeSpan.Zero;
    public long FlashbackDiskBytes => _flashbackBackend.BufferManager?.TotalDiskBytes ?? 0;
    public int FlashbackSegmentCount => _flashbackBackend.BufferManager?.SegmentCount ?? 0;
    internal FlashbackPlaybackController? FlashbackPlaybackController => _flashbackBackend.PlaybackController;
    internal FlashbackBufferManager? FlashbackBufferManager => _flashbackBackend.BufferManager;
    public long FlashbackOutputBytes => _flashbackBackend.Sink?.OutputBytes ?? 0;
    public long FlashbackTotalBytesWritten => _flashbackBackend.BufferManager?.TotalBytesWritten ?? 0;
    public string? EncoderCodecName => _flashbackBackend.Sink?.CodecName;
    public uint EncoderTargetBitRate => _flashbackBackend.Sink?.TargetBitRate ?? 0;
    public int EncoderWidth => _flashbackBackend.Sink?.EncoderWidth ?? 0;
    public int EncoderHeight => _flashbackBackend.Sink?.EncoderHeight ?? 0;
    public double EncoderFrameRate => _flashbackBackend.Sink?.EncoderFrameRate ?? 0;
    public FinalizeResult? LastExportResult => _lastExportResult;

    internal IReadOnlyList<FlashbackSegmentInfo> GetFlashbackSegments()
    {
        return _flashbackBackend.BufferManager?.GetSegmentInfoList()
            ?? Array.Empty<FlashbackSegmentInfo>();
    }

    public Task SetFlashbackEnabledAsync(bool enabled, CancellationToken cancellationToken = default)
        => RunTransitionAsync(CurrentSessionState, async transitionToken =>
        {
            if (_isRecording && IsFlashbackRecordingBackendActive() && !enabled)
            {
                Logger.Log("FLASHBACK_DISABLE_BLOCKED reason=recording_active");
                throw new InvalidOperationException("Cannot disable Flashback while Flashback recording is active.");
            }

            if (_flashbackEnabled == enabled)
            {
                if (enabled && (_flashbackBackend.Sink != null || _isRecording))
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
                await DisposeFlashbackPreviewBackendAsync(transitionToken, purgeSegments: false).ConfigureAwait(false);
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
            var unifiedVideoCapture = _videoPipeline.Capture;
            if (unifiedVideoCapture != null && _currentSettings != null)
            {
                try
                {
                    await EnsureFlashbackPreviewBackendAsync(unifiedVideoCapture, _currentSettings, transitionToken).ConfigureAwait(false);
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
    /// Tears down the running flashback encoder and buffer, then rebuilds
    /// with current settings. Retires the old session for bounded startup
    /// cleanup instead of purging history as an implicit settings side effect.
    /// </summary>
    public Task RestartFlashbackAsync(CancellationToken cancellationToken = default)
        => RunTransitionAsync(CurrentSessionState, async transitionToken =>
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
        return RunTransitionAsync(CurrentSessionState, async transitionToken =>
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

    private async Task RestartFlashbackCoreAsync(CancellationToken cancellationToken)
    {
        await DisposeFlashbackPreviewBackendAsync(cancellationToken, purgeSegments: false).ConfigureAwait(false);

        var committedRestartToken = CancellationToken.None;
        var unifiedVideoCapture = _videoPipeline.Capture;
        var settings = _currentSettings;
        if (!_flashbackEnabled || unifiedVideoCapture == null || settings == null)
        {
            Logger.Log($"FLASHBACK_RESTART_TEARDOWN_ONLY enabled={_flashbackEnabled} capture={unifiedVideoCapture != null} settings={settings != null}");
            return;
        }

        await EnsureFlashbackPreviewBackendAsync(unifiedVideoCapture, settings, committedRestartToken).ConfigureAwait(false);
        Logger.Log("FLASHBACK_RESTART_OK");
        cancellationToken.ThrowIfCancellationRequested();
    }

    private async Task EnsureFlashbackPreviewBackendAsync(
        UnifiedVideoCapture unifiedVideoCapture,
        CaptureSettings settings,
        CancellationToken cancellationToken)
    {
        if (!_flashbackEnabled || _flashbackBackend.Sink != null)
            return;

        // Cache AV1 NVENC availability on first flashback init (async-safe here)
        if (!_hasAv1Nvenc)
        {
            try
            {
                var support = await FfmpegRuntimeLocator.GetEncoderSupportAsync().ConfigureAwait(false);
                _hasAv1Nvenc = support.HasAv1Nvenc;
            }
            catch (Exception ex)
            {
                Logger.Log($"FLASHBACK_ENCODER_SUPPORT_PROBE_WARN type={ex.GetType().Name} msg={ex.Message}");
                // Assume unavailable - will fall back to HEVC.
            }
        }

        try
        {
            // Wait until both video and audio are confirmed flowing before starting
            // the encoder. This eliminates the startup transient where audio PTS races
            // ahead of video PTS (~840ms) because WASAPI starts before the source reader.
            var deadline = Environment.TickCount64 + 5000;
            while (Environment.TickCount64 < deadline)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var videoReady = unifiedVideoCapture.VideoFramesArrived > 0;
                var audioReady = _previewAudioGraph.ProgramCapture == null || _previewAudioGraph.ProgramCapture.CaptureCallbackCount > 0;
                if (videoReady && audioReady)
                    break;
                await Task.Delay(100, cancellationToken).ConfigureAwait(false);
            }
            Logger.Log(
                $"FLASHBACK_PREVIEW_READINESS video_frames={unifiedVideoCapture.VideoFramesArrived} " +
                $"audio_callbacks={_previewAudioGraph.ProgramCapture?.CaptureCallbackCount ?? -1}");

            await _flashbackBackend.StartPreviewBackendAsync(
                    new FlashbackPreviewBackendStartRequest(
                        unifiedVideoCapture,
                        _previewAudioGraph.ProgramCapture,
                        _previewAudioGraph.MicrophoneCapture,
                        _previewAudioGraph.Playback,
                        _videoPipeline.PreviewFrameSink,
                        settings,
                        CloneCaptureSettings(settings),
                        () => CreateFlashbackSessionContext(unifiedVideoCapture, settings),
                        OnFlashbackBackendFatalError,
                        OnFlashbackFrameEncoded,
                        SchedulePreviewBackendStartDeferredCleanup,
                        cancellationToken))
                .ConfigureAwait(false);

            ClearLastFlashbackFailure();
        }
        catch (Exception ex)
        {
            var failureToken = ex is OperationCanceledException && cancellationToken.IsCancellationRequested
                ? "FLASHBACK_PREVIEW_INIT_CANCELLED"
                : "FLASHBACK_PREVIEW_INIT_FAIL";
            Logger.Log($"{failureToken} type={ex.GetType().Name} error='{ex.Message}'");

            throw;
        }
    }

    private void SchedulePreviewBackendStartDeferredCleanup(
        Task sinkCompletionTask,
        FlashbackBufferManager? bufferManager,
        FlashbackExporter? flashbackExporter,
        string reason,
        bool purgeSegments)
    {
        ScheduleDeferredFlashbackBackendCleanup(
            sinkCompletionTask,
            new FlashbackBackendArtifactCleanupRequest(
                bufferManager,
                flashbackExporter,
                reason,
                purgeSegments));
    }

    private void ScheduleDeferredFlashbackBackendCleanup(
        Task sinkCompletionTask,
        FlashbackBackendArtifactCleanupRequest request,
        int attempt = 0)
        => _flashbackBackend.ScheduleDeferredArtifactCleanup(
            sinkCompletionTask,
            request,
            WaitForFlashbackBackendCleanupExportLockAsync,
            ReleaseFlashbackBackendCleanupExportLock,
            attempt);

    private async Task<bool> CleanupFlashbackBackendArtifactsAfterExportAsync(
        FlashbackBackendArtifactCleanupRequest request,
        string mode,
        bool exportOperationLockAlreadyHeld = false)
        => await _flashbackBackend.CleanupArtifactsAfterExportAsync(
                request,
                mode,
                WaitForFlashbackBackendCleanupExportLockAsync,
                ReleaseFlashbackBackendCleanupExportLock,
                exportOperationLockAlreadyHeld)
            .ConfigureAwait(false);

    private Task<bool> WaitForFlashbackBackendCleanupExportLockAsync()
        => _flashbackExportOperationLock.WaitAsync(
            TimeSpan.FromSeconds(30),
            CancellationToken.None);

    private void ReleaseFlashbackBackendCleanupExportLock(string mode)
        => ReleaseSemaphoreBestEffort(_flashbackExportOperationLock, $"flashback_backend_cleanup_{mode}");

    private async Task DisposeFlashbackPreviewBackendAsync(
        CancellationToken cancellationToken,
        bool purgeSegments = true,
        bool detachMicrophoneWriter = true)
    {
        await _flashbackBackendLeaseLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        var exportOperationLockHeld = false;
        try
        {
            await _flashbackExportOperationLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            exportOperationLockHeld = true;

            var effectivePurgeSegments = _flashbackBackend.ResolveSegmentPurge(
                purgeSegments,
                "preview_backend_dispose");
            await DisposeFlashbackPreviewBackendCoreAsync(
                    cancellationToken,
                    CreateFlashbackPreviewBackendDisposalRequest(
                        effectivePurgeSegments,
                        detachMicrophoneWriter,
                        exportOperationLockAlreadyHeld: true,
                        cancellationToken))
                .ConfigureAwait(false);
        }
        finally
        {
            ReleaseFlashbackExportOperationLockIfHeld(ref exportOperationLockHeld);
            ReleaseSemaphoreBestEffort(_flashbackBackendLeaseLock, "flashback_preview_backend_dispose");
        }
    }

    private async Task DisposeFlashbackPreviewBackendCoreAsync(
        CancellationToken cancellationToken,
        FlashbackPreviewBackendDisposalRequest request)
    {
        await _flashbackBackend.DisposePreviewBackendAsync(request).ConfigureAwait(false);
    }

    private FlashbackPreviewBackendDisposalRequest CreateFlashbackPreviewBackendDisposalRequest(
        bool purgeSegments,
        bool detachMicrophoneWriter,
        bool exportOperationLockAlreadyHeld,
        CancellationToken cancellationToken)
        => new FlashbackPreviewBackendDisposalRequest(
            _videoPipeline.Capture,
            _previewAudioGraph.ProgramCapture,
            _previewAudioGraph.MicrophoneCapture,
            OnFlashbackFrameEncoded,
            WaitForFlashbackBackendCleanupExportLockAsync,
            ReleaseFlashbackBackendCleanupExportLock,
            purgeSegments,
            detachMicrophoneWriter,
            exportOperationLockAlreadyHeld,
            cancellationToken);


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
        => RunTransitionAsync(CurrentSessionState, transitionToken =>
        {
            if (_currentSettings != null)
            {
                _currentSettings.FlashbackBufferMinutes = bufferMinutes;
                _currentSettings.FlashbackGpuDecode = gpuDecode;
            }

            if (_flashbackBackend.PlaybackController != null)
            {
                _flashbackBackend.PlaybackController.GpuDecodeEnabled = gpuDecode;
            }

            if (_isRecording && IsFlashbackRecordingBackendActive())
            {
                _pendingFlashbackSettingsChange = true;
            }

            return Task.CompletedTask;
        }, cancellationToken);

    /// <summary>
    /// Updates the recording format and cycles the flashback encoder so the buffer
    /// uses the new codec. No-op if not previewing or if a recording is active.
    /// </summary>
    public Task UpdateRecordingFormatAsync(RecordingFormat format, CancellationToken cancellationToken = default)
        => RunTransitionAsync(CurrentSessionState, async transitionToken =>
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
            if (_flashbackBackend.Sink != null)
            {
                try
                {
                    await RebuildFlashbackPreviewBackendForSettingsChangeAsync(transitionToken).ConfigureAwait(false);
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
        => RunTransitionAsync(CurrentSessionState, async transitionToken =>
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

            var cycledBuffer = _flashbackBackend.Sink != null;
            var cycleFailed = false;
            if (_flashbackBackend.Sink != null)
            {
                try
                {
                    await RebuildFlashbackPreviewBackendForSettingsChangeAsync(transitionToken).ConfigureAwait(false);
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

    /// <summary>
    /// Retires the current Flashback history for bounded startup cleanup and
    /// starts a fresh buffer so incompatible setting changes do not mix segments.
    /// </summary>
    private async Task RebuildFlashbackPreviewBackendForSettingsChangeAsync(CancellationToken cancellationToken)
    {
        await DisposeFlashbackPreviewBackendAsync(cancellationToken, purgeSegments: false).ConfigureAwait(false);

        var committedRebuildToken = CancellationToken.None;
        var unifiedVideoCapture = _videoPipeline.Capture;
        var currentSettings = _currentSettings;
        if (!_flashbackEnabled || unifiedVideoCapture == null || currentSettings == null)
        {
            Logger.Log($"FLASHBACK_SETTINGS_REBUILD_TEARDOWN_ONLY enabled={_flashbackEnabled} capture={unifiedVideoCapture != null} settings={currentSettings != null}");
            return;
        }

        await EnsureFlashbackPreviewBackendAsync(unifiedVideoCapture, currentSettings, committedRebuildToken).ConfigureAwait(false);
        Logger.Log("FLASHBACK_SETTINGS_REBUILD_OK");
        cancellationToken.ThrowIfCancellationRequested();
    }

    /// <summary>
    /// Coordinates cycling the Flashback encoder sink after recording stops.
    /// CaptureService owns the transition locks and full rebuild fallbacks;
    /// FlashbackBackendResources owns the sink-only resource mechanics.
    /// </summary>
    private async Task CycleFlashbackBufferAsync(CancellationToken cancellationToken, bool purgeSegments = false)
    {
        await _flashbackBackendLeaseLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        var exportOperationLockHeld = false;
        try
        {
            await _flashbackExportOperationLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            exportOperationLockHeld = true;

            var unifiedVideoCapture = _videoPipeline.Capture;
            var currentSettings = _currentSettings;
            var effectivePurgeSegments = _flashbackBackend.ResolveSegmentPurge(
                purgeSegments,
                "buffer_cycle");

            if (purgeSegments && !effectivePurgeSegments)
            {
                await DisposeFlashbackPreviewBackendCoreAsync(
                        cancellationToken,
                        CreateFlashbackPreviewBackendDisposalRequest(
                            purgeSegments: false,
                            detachMicrophoneWriter: true,
                            exportOperationLockAlreadyHeld: true,
                            cancellationToken))
                    .ConfigureAwait(false);
                if (_flashbackEnabled && unifiedVideoCapture != null && currentSettings != null)
                {
                    await EnsureFlashbackPreviewBackendAsync(unifiedVideoCapture, currentSettings, cancellationToken).ConfigureAwait(false);
                    Logger.Log("FLASHBACK_BUFFER_CYCLE_OK mode=preserve_rebuild new_session=true");
                }
                else
                {
                    Logger.Log("FLASHBACK_BUFFER_CYCLE_OK mode=preserve_rebuild new_session=false reason='disabled_or_no_capture'");
                }
                return;
            }

            if (!_flashbackEnabled || unifiedVideoCapture == null || currentSettings == null || _flashbackBackend.BufferManager == null || _flashbackBackend.Sink == null)
            {
                await DisposeFlashbackPreviewBackendCoreAsync(
                        cancellationToken,
                        CreateFlashbackPreviewBackendDisposalRequest(
                            effectivePurgeSegments,
                            detachMicrophoneWriter: true,
                            exportOperationLockAlreadyHeld: true,
                            cancellationToken))
                    .ConfigureAwait(false);
                if (_flashbackEnabled && unifiedVideoCapture != null && currentSettings != null)
                {
                    await EnsureFlashbackPreviewBackendAsync(unifiedVideoCapture, currentSettings, cancellationToken).ConfigureAwait(false);
                    Logger.Log("FLASHBACK_BUFFER_CYCLE_OK mode=full_teardown new_session=true");
                }
                else
                {
                    Logger.Log("FLASHBACK_BUFFER_CYCLE_OK mode=full_teardown new_session=false reason='disabled_or_no_capture'");
                }
                return;
            }

            var committedCycleToken = CancellationToken.None;
            var cycleResult = await _flashbackBackend.CycleSinkOnlyAsync(
                    new FlashbackBufferCycleRequest(
                        unifiedVideoCapture,
                        _previewAudioGraph.ProgramCapture,
                        _previewAudioGraph.MicrophoneCapture,
                        _previewAudioGraph.Playback,
                        _videoPipeline.PreviewFrameSink,
                        currentSettings,
                        CloneCaptureSettings(currentSettings),
                        () => CreateFlashbackSessionContext(unifiedVideoCapture, currentSettings),
                        OnFlashbackBackendFatalError,
                        OnFlashbackFrameEncoded,
                        ClearLastFlashbackFailure,
                        (task, request) => ScheduleDeferredFlashbackBackendCleanup(task, request),
                        effectivePurgeSegments,
                        cancellationToken))
                .ConfigureAwait(false);

            if (cycleResult.Outcome == FlashbackBufferCycleOutcome.DeferredFullRebuild)
            {
                await EnsureFlashbackPreviewBackendAsync(unifiedVideoCapture, currentSettings, committedCycleToken).ConfigureAwait(false);
                Logger.Log("FLASHBACK_BUFFER_CYCLE_OK mode=deferred_full_rebuild");
                cancellationToken.ThrowIfCancellationRequested();
                return;
            }

            if (cycleResult.Outcome == FlashbackBufferCycleOutcome.PurgeFallbackRebuild)
            {
                await DisposeFlashbackPreviewBackendCoreAsync(
                        committedCycleToken,
                        CreateFlashbackPreviewBackendDisposalRequest(
                            effectivePurgeSegments,
                            detachMicrophoneWriter: true,
                            exportOperationLockAlreadyHeld: true,
                            committedCycleToken))
                    .ConfigureAwait(false);
                await EnsureFlashbackPreviewBackendAsync(unifiedVideoCapture, currentSettings, committedCycleToken).ConfigureAwait(false);
                Logger.Log("FLASHBACK_BUFFER_CYCLE_OK mode=purge_fallback_rebuild");
                cancellationToken.ThrowIfCancellationRequested();
                return;
            }

            if (cycleResult.Outcome == FlashbackBufferCycleOutcome.FallbackFullRebuild)
            {
                await DisposeFlashbackPreviewBackendCoreAsync(
                        committedCycleToken,
                        CreateFlashbackPreviewBackendDisposalRequest(
                            effectivePurgeSegments,
                            detachMicrophoneWriter: true,
                            exportOperationLockAlreadyHeld: true,
                            committedCycleToken))
                    .ConfigureAwait(false);
                await EnsureFlashbackPreviewBackendAsync(unifiedVideoCapture, currentSettings, committedCycleToken).ConfigureAwait(false);
                Logger.Log("FLASHBACK_BUFFER_CYCLE_OK mode=fallback_full_rebuild");
                cancellationToken.ThrowIfCancellationRequested();
                return;
            }
        }
        finally
        {
            ReleaseFlashbackExportOperationLockIfHeld(ref exportOperationLockHeld);
            ReleaseSemaphoreBestEffort(_flashbackBackendLeaseLock, "flashback_buffer_cycle");
        }
    }

    // Flashback recording support: backend ownership checks, start/finalize choreography, audio attachment, frame-encoded fan-out, and capability validation.
    private async Task DisposeUnusableFlashbackRecordingBackendAsync(CancellationToken transitionToken)
    {
        var flashbackSink = _flashbackBackend.Sink;
        if (_flashbackEnabled &&
            flashbackSink != null &&
            !flashbackSink.CanBeginRecording)
        {
            Logger.Log(
                "FLASHBACK_RECORDING_BACKEND_UNUSABLE_FALLBACK " +
                $"failed={flashbackSink.EncodingFailed} type={flashbackSink.EncodingFailureType ?? "None"}");
            await DisposeFlashbackPreviewBackendAsync(transitionToken, purgeSegments: true).ConfigureAwait(false);
        }
    }

    private async Task StartFlashbackRecordingAsync(
        CaptureSettings settings,
        CancellationToken transitionToken,
        RecordingStartRollbackState rollback)
    {
        var flashbackSink = _flashbackBackend.Sink
            ?? throw new InvalidOperationException("Flashback backend is not available for recording.");
        var videoCapture = _videoPipeline.Capture;

        // Guard: if the existing flashback sink's pixel format no longer matches the
        // negotiated UVC format, reject the reuse path so the slow path rebuilds correctly.
        if (flashbackSink.IsP010 is bool recSinkIsP010 &&
            videoCapture != null &&
            recSinkIsP010 != videoCapture.IsP010)
        {
            Logger.Log(
                $"FLASHBACK_FAST_PATH_FORMAT_MISMATCH " +
                $"existing_p010={recSinkIsP010} requested_p010={videoCapture.IsP010}");
            throw new InvalidOperationException(
                $"Flashback recording fast path: pixel-format mismatch — sink was built for " +
                $"{(recSinkIsP010 ? "P010" : "NV12")} but UVC session negotiated " +
                $"{(videoCapture.IsP010 ? "P010" : "NV12")}. " +
                "Rebuild the flashback backend with the correct format.");
        }

        var fbOutputFolder = await OpenRecordingOutputFolderAsync(settings).ConfigureAwait(false);

        transitionToken.ThrowIfCancellationRequested();

        var fbEffectiveFrameRate = videoCapture?.Fps > 0 ? videoCapture.Fps : settings.FrameRate;
        var fbRecordingContext = await CreateFlashbackRecordingContextAsync(
            settings,
            fbOutputFolder,
            fbEffectiveFrameRate).ConfigureAwait(false);
        rollback.RecordingContext = fbRecordingContext;

        // If flashback settings changed while preview was stopped, rebuild
        // before recording so the retained backend matches the requested file.
        var flashbackBackendSettingsChanged = _flashbackBackend.SettingsSnapshot == null ||
            !CanReuseFlashbackBackend(_flashbackBackend.SettingsSnapshot, settings);
        var flashbackAudioTopologyChanged =
            flashbackSink.AudioEnabled != settings.AudioEnabled ||
            flashbackSink.MicrophoneEnabled != settings.MicrophoneEnabled;
        if (flashbackAudioTopologyChanged)
        {
            Logger.Log($"FLASHBACK_RECORDING_TOPOLOGY_MISMATCH_REJECT " +
                $"audio={settings.AudioEnabled} (was {flashbackSink.AudioEnabled}) " +
                $"mic={settings.MicrophoneEnabled} (was {flashbackSink.MicrophoneEnabled})");
            EnsureFlashbackRecordingTopologyMatches(
                flashbackSink,
                settings.AudioEnabled,
                settings.MicrophoneEnabled);
        }

        if (flashbackBackendSettingsChanged)
        {
            Logger.Log($"FLASHBACK_SETTINGS_MISMATCH_AUTO_RESTART " +
                $"settings_changed={flashbackBackendSettingsChanged} " +
                $"audio={settings.AudioEnabled} " +
                $"mic={settings.MicrophoneEnabled}");

            await DisposeFlashbackPreviewBackendAsync(transitionToken, purgeSegments: false).ConfigureAwait(false);

            videoCapture = _videoPipeline.Capture;
            if (videoCapture != null)
            {
                await EnsureFlashbackPreviewBackendAsync(videoCapture, settings, transitionToken).ConfigureAwait(false);
            }

            flashbackSink = _flashbackBackend.Sink
                ?? throw new InvalidOperationException("Failed to restart flashback backend for updated recording settings.");
        }

        await EnsureFlashbackAudioInputsAsync(settings, transitionToken, "recording_flashback_start").ConfigureAwait(false);
        await _flashbackBackendLeaseLock.WaitAsync(transitionToken).ConfigureAwait(false);
        rollback.FlashbackRecordingBackendLeaseHeld = true;
        Volatile.Write(ref _flashbackRecordingStartInProgress, 1);
        try
        {
            var activeFlashbackSink = flashbackSink;
            if (!activeFlashbackSink.CanBeginRecording)
            {
                throw new InvalidOperationException("Flashback backend is not healthy enough to begin recording.");
            }

            if (!activeFlashbackSink.WaitForForceRotateIdle(TimeSpan.FromSeconds(10)))
            {
                throw new InvalidOperationException("Flashback backend export rotation did not quiesce before recording start.");
            }

            if (!activeFlashbackSink.CanBeginRecording)
            {
                throw new InvalidOperationException("Flashback backend became unavailable before recording start.");
            }

            rollback.FlashbackRecordingStartedSink = activeFlashbackSink;
            _recordingIntegrityCounterBaseline = CaptureRecordingIntegrityCounters(activeFlashbackSink);
            _recordingIntegrityAudioBaseline = CaptureRecordingAudioCounters(
                _previewAudioGraph.ProgramCapture,
                activeFlashbackSink,
                settings);
            activeFlashbackSink.BeginRecording(fbRecordingContext.FinalOutputPath);
            if (activeFlashbackSink.EncodingFailed)
            {
                throw new InvalidOperationException(
                    $"Flashback backend failed while starting recording: {activeFlashbackSink.EncodingFailureMessage ?? "unknown error"}");
            }

            videoCapture?.BeginFlashbackRecordingAccounting();
            _recordingBackend.InstallFlashback(activeFlashbackSink, fbRecordingContext, settings);
            ClearLastRecordingFailure();
            _isRecording = true;
            _flashbackRecordingStartBytes = _flashbackBackend.BufferManager?.TotalBytesWritten ?? 0;
            PublishRecordingStartedOutcome(fbRecordingContext.FinalOutputPath);
            _recordingStopwatch.Restart();
            StatusChanged?.Invoke(this, "Recording");
            Logger.Log($"FLASHBACK_UNIFIED_RECORDING_START output='{fbRecordingContext.FinalOutputPath}'");
        }
        finally
        {
            Volatile.Write(ref _flashbackRecordingStartInProgress, 0);
            if (rollback.FlashbackRecordingBackendLeaseHeld)
            {
                rollback.FlashbackRecordingBackendLeaseHeld = false;
                ReleaseSemaphoreBestEffort(_flashbackBackendLeaseLock, "flashback_recording_start");
            }
        }
    }

    private bool IsFlashbackRecordingBackendActive()
        => _flashbackBackend.Sink != null &&
           _recordingBackend.IsFlashbackBackend(_flashbackBackend.Sink);

    private bool IsFlashbackRecordingBackendOwnedByRecording()
        => Volatile.Read(ref _flashbackRecordingStartInProgress) != 0 ||
           Volatile.Read(ref _flashbackRecordingFinalizeInProgress) != 0 ||
           (_isRecording && IsFlashbackRecordingBackendActive());

    private void AttachFlashbackAudioIfSupported(WasapiAudioCapture? capture, string reason)
    {
        var flashbackSink = _flashbackBackend.Sink;
        if (capture == null || flashbackSink == null)
            return;

        if (!flashbackSink.AudioEnabled)
        {
            Logger.Log($"FLASHBACK_AUDIO_ATTACH_SKIPPED reason='{reason}' sink_audio_enabled=false");
            return;
        }

        capture.AttachFlashbackSink(flashbackSink);
        Logger.Log($"FLASHBACK_AUDIO_ATTACH_OK reason='{reason}'");
    }

    private async Task EnsureFlashbackAudioInputsAsync(
        CaptureSettings settings,
        CancellationToken cancellationToken,
        string reason)
    {
        var audioDeviceId = settings.AudioEnabled
            ? (settings.UseCustomAudioInput ? settings.AudioDeviceId : (_audioDeviceId ?? _currentDevice?.AudioDeviceId))
            : null;

        if (settings.AudioEnabled && _previewAudioGraph.ProgramCapture == null)
        {
            if (!string.IsNullOrWhiteSpace(audioDeviceId))
            {
                WasapiAudioCapture? wasapiCapture = new();
                try
                {
                    await wasapiCapture.InitializeAsync(audioDeviceId, cancellationToken).ConfigureAwait(false);
                    wasapiCapture.AudioLevelUpdated += OnWasapiAudioLevelUpdated;
                    wasapiCapture.CaptureFailed += OnWasapiCaptureFailed;
                    wasapiCapture.Start();
                    _previewAudioGraph.ProgramCapture = wasapiCapture;
                    wasapiCapture = null;
                    ResetAvSyncDriftBaseline();
                    _previewAudioGraph.ResetCaptureFault();
                    Logger.Log($"FLASHBACK_AUDIO_CAPTURE_RESTORED reason='{reason}' device='{audioDeviceId}'");
                }
                finally
                {
                    if (wasapiCapture != null)
                    {
                        wasapiCapture.AudioLevelUpdated -= OnWasapiAudioLevelUpdated;
                        wasapiCapture.CaptureFailed -= OnWasapiCaptureFailed;
                        try { await wasapiCapture.DisposeAsync().ConfigureAwait(false); }
                        catch (Exception disposeEx) { Logger.Log($"FLASHBACK_AUDIO_CAPTURE_RESTORE_DISPOSE_WARN type={disposeEx.GetType().Name} msg={disposeEx.Message}"); }
                    }
                }
            }
            else
            {
                Logger.Log($"FLASHBACK_AUDIO_CAPTURE_UNAVAILABLE reason='{reason}'");
            }
        }

        AttachFlashbackAudioIfSupported(_previewAudioGraph.ProgramCapture, reason);

        if (_micMonitorEnabled && _previewAudioGraph.MicrophoneCapture == null && !string.IsNullOrWhiteSpace(_micMonitorDeviceId))
        {
            WasapiAudioCapture? micCapture = new();
            try
            {
                await micCapture.InitializeAsync(_micMonitorDeviceId, cancellationToken).ConfigureAwait(false);
                micCapture.AudioLevelUpdated += OnMicrophoneAudioLevelUpdated;
                micCapture.CaptureFailed += OnWasapiCaptureFailed;
                micCapture.Start();
                _previewAudioGraph.MicrophoneCapture = micCapture;
                micCapture = null;
                Logger.Log("MIC_MONITOR_START device='" + (_micMonitorDeviceName ?? "?") + "'");
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception micEx)
            {
                Logger.Log("Mic monitor start failed (non-fatal): " + micEx.Message);
            }
            finally
            {
                if (micCapture != null)
                {
                    micCapture.AudioLevelUpdated -= OnMicrophoneAudioLevelUpdated;
                    micCapture.CaptureFailed -= OnWasapiCaptureFailed;
                    try { await micCapture.DisposeAsync().ConfigureAwait(false); }
                    catch (Exception disposeEx) { Logger.Log($"MIC_MONITOR_RESTORE_DISPOSE_WARN type={disposeEx.GetType().Name} msg={disposeEx.Message}"); }
                }
            }
        }

        if (_previewAudioGraph.MicrophoneCapture != null && _flashbackBackend.Sink is { MicrophoneEnabled: true } fbSink)
        {
            _previewAudioGraph.MicrophoneCapture.SetAudioWriter(samples => fbSink.WriteMicrophoneAudioAsync(samples));
            Logger.Log($"FLASHBACK_MIC_ATTACH_OK reason='{reason}'");
        }
    }

    private void OnFlashbackFrameEncoded(object? sender, long frameCount)
    {
        if (!IsFlashbackRecordingBackendActive())
            return;

        FrameCaptured?.Invoke(this, unchecked((ulong)Math.Max(0L, frameCount)));
    }

    private void ValidateFlashbackRecordingCapabilities(
        FlashbackEncoderSink flashbackSink,
        bool requiresHdmiAudio,
        bool requiresMicrophone)
    {
        if (requiresHdmiAudio && !flashbackSink.AudioEnabled)
            throw new InvalidOperationException(
                "Flashback recording cannot include HDMI audio because the active flashback session was started without audio.");

        if (requiresMicrophone && !flashbackSink.MicrophoneEnabled)
            throw new InvalidOperationException(
                "Flashback recording cannot include microphone audio because the active flashback session was started without microphone support.");
    }

    private static void EnsureFlashbackRecordingTopologyMatches(
        FlashbackEncoderSink flashbackSink,
        bool audioEnabled,
        bool microphoneEnabled)
    {
        if (flashbackSink.AudioEnabled == audioEnabled &&
            flashbackSink.MicrophoneEnabled == microphoneEnabled)
            return;

        throw new InvalidOperationException(
            "Flashback recording settings changed after preview start. " +
            $"Restart preview so flashback can reopen with audio={audioEnabled} microphone={microphoneEnabled} " +
            $"(current audio={flashbackSink.AudioEnabled} microphone={flashbackSink.MicrophoneEnabled}).");
    }

    private static string? ResolveFlashbackExportVerificationFormat(
        CaptureSettings? settings,
        UnifiedVideoCapture? unifiedVideoCapture)
        => settings?.Format.ToString();

    /// <summary>
    /// Flashback recording honors the requested codec and preset directly. This legacy
    /// snapshot field remains for compatibility and should stay null unless a future
    /// explicit, user-visible substitution is introduced.
    /// </summary>
    private static string? ResolveFlashbackCodecDowngradeReason(
        CaptureSettings? settings,
        UnifiedVideoCapture? unifiedVideoCapture)
        => null;

    private FlashbackSessionContext CreateFlashbackSessionContext(
        UnifiedVideoCapture unifiedVideoCapture,
        CaptureSettings settings)
    {
        var isP010 = unifiedVideoCapture.IsP010;
        var frameRate = unifiedVideoCapture.Fps > 0 ? unifiedVideoCapture.Fps : settings.FrameRate;
        if (isP010 && settings.Format == RecordingFormat.H264Mp4)
        {
            throw new InvalidOperationException("HDR/P010 recording requires HEVC or AV1; H.264 cannot encode this pipeline.");
        }

        if (settings.Format == RecordingFormat.Av1Mp4 && !_hasAv1Nvenc)
        {
            throw new InvalidOperationException("AV1 recording requires the av1_nvenc encoder, but it is not available.");
        }

        var codecName = settings.Format switch
        {
            RecordingFormat.HevcMp4 => "hevc_nvenc",
            RecordingFormat.Av1Mp4 => "av1_nvenc",
            _ => "h264_nvenc"
        };
        var audioDeviceId = settings.AudioEnabled
            ? (settings.UseCustomAudioInput ? settings.AudioDeviceId : (_audioDeviceId ?? _currentDevice?.AudioDeviceId))
            : null;
        var d3dManager = unifiedVideoCapture.D3DManager;
        // When the software MJPEG decode pipeline is active, frames arrive as CPU NV12
        // buffers (not D3D11 textures). Do not initialize hw_frames for software
        // packets; nvenc would expect D3D11 textures and can crash in the driver.
        var useGpuEncoding = !unifiedVideoCapture.IsSoftwareMjpegPipelineActive;

        var frameRateParts = ResolveFlashbackSessionFrameRateParts(settings, frameRate);
        frameRate = frameRateParts.EffectiveFrameRate;
        var fpsNum = frameRateParts.Numerator;
        var fpsDen = frameRateParts.Denominator;

        var flashbackNvencPreset = settings.NvencPreset;

        // Hard rail: HDR must never silently degrade. If the user requested HDR
        // but UVC negotiation did not land on P010, fail the operation rather than
        // allowing SDR data to be encoded as if it were HDR (or vice versa).
        var hdrRequested = HdrOutputPolicy.IsEnabled(settings);
        if (hdrRequested != isP010)
        {
            Logger.Log(
                $"FLASHBACK_HDR_NEGOTIATION_FAIL requested={hdrRequested} negotiated_p010={isP010} resolved_codec={codecName}");
            throw new InvalidOperationException(
                $"Flashback HDR negotiation mismatch: HDR requested={hdrRequested} but UVC negotiated P010={isP010}. " +
                "Operation aborted to prevent silent HDR degradation.");
        }

        return new FlashbackSessionContext
        {
            Width = Math.Max(1, unifiedVideoCapture.Width),
            Height = Math.Max(1, unifiedVideoCapture.Height),
            FrameRate = frameRate,
            FrameRateNumerator = fpsNum,
            FrameRateDenominator = fpsDen,
            CodecName = codecName,
            NvencPreset = flashbackNvencPreset.ToString(),
            SplitEncodeMode = SplitEncodeModeParser.ToWireString(settings.SplitEncodeMode),
            IsP010 = isP010,
            BitRate = settings.GetTargetBitrate(),
            HdrEnabled = hdrRequested,
            IsFullRangeInput = unifiedVideoCapture.IsHighFrameRateMjpegMode,
            HdrMasterDisplayMetadata = settings.HdrMasterDisplayMetadata,
            HdrMaxCll = settings.HdrMaxCll,
            HdrMaxFall = settings.HdrMaxFall,
            D3D11DevicePtr = useGpuEncoding ? (d3dManager?.Device?.NativePointer ?? IntPtr.Zero) : IntPtr.Zero,
            D3D11DeviceContextPtr = useGpuEncoding ? (d3dManager?.ImmediateContext?.NativePointer ?? IntPtr.Zero) : IntPtr.Zero,
            AudioEnabled = settings.AudioEnabled && !string.IsNullOrWhiteSpace(audioDeviceId),
            MicrophoneEnabled = settings.MicrophoneEnabled && !string.IsNullOrWhiteSpace(settings.MicrophoneDeviceId)
        };
    }

    private static (int? Numerator, int? Denominator, double EffectiveFrameRate) ResolveFlashbackSessionFrameRateParts(
        CaptureSettings settings,
        double deliveryFrameRate)
    {
        // Preserve exact rationals only when they describe the actual delivered USB cadence.
        // A source-reported 120000/1001 rate paired with ~120 delivered frames/sec causes A/V
        // drift if we stamp Flashback video against the slower source clock.
        if (!double.IsFinite(deliveryFrameRate) || deliveryFrameRate <= 0)
        {
            return (null, null, deliveryFrameRate);
        }

        if (settings.RequestedFrameRateNumerator is not uint numerator ||
            settings.RequestedFrameRateDenominator is not uint denominator ||
            numerator == 0 ||
            denominator == 0 ||
            numerator > int.MaxValue ||
            denominator > int.MaxValue)
        {
            return InferFlashbackSessionFrameRateParts(deliveryFrameRate);
        }

        var rationalFps = numerator / (double)denominator;
        if (!double.IsFinite(rationalFps) || rationalFps <= 0)
        {
            return (null, null, deliveryFrameRate);
        }

        var deltaFps = Math.Abs(rationalFps - deliveryFrameRate);
        var toleranceFps = Math.Max(0.01, deliveryFrameRate * 0.0001);
        if (deltaFps > toleranceFps)
        {
            Logger.Log(
                $"FLASHBACK_FRAME_RATE_RATIONAL_REJECT requested={numerator}/{denominator} " +
                $"rational={rationalFps:0.######} delivery={deliveryFrameRate:0.######} " +
                $"delta={deltaFps:0.######} tolerance={toleranceFps:0.######}");
            return InferFlashbackSessionFrameRateParts(deliveryFrameRate);
        }

        Logger.Log(
            $"FLASHBACK_FRAME_RATE_RATIONAL_ACCEPT requested={numerator}/{denominator} " +
            $"delivery={deliveryFrameRate:0.######} effective={rationalFps:0.######}");
        return ((int)numerator, (int)denominator, rationalFps);
    }

    private static (int? Numerator, int? Denominator, double EffectiveFrameRate) InferFlashbackSessionFrameRateParts(double deliveryFrameRate)
    {
        foreach (var (numerator, denominator) in CommonFlashbackFrameRateParts)
        {
            var rationalFps = numerator / (double)denominator;
            var deltaFps = Math.Abs(rationalFps - deliveryFrameRate);
            var toleranceFps = Math.Max(0.01, deliveryFrameRate * 0.0001);
            if (deltaFps <= toleranceFps)
            {
                Logger.Log(
                    $"FLASHBACK_FRAME_RATE_RATIONAL_INFER inferred={numerator}/{denominator} " +
                    $"delivery={deliveryFrameRate:0.######} effective={rationalFps:0.######}");
                return (numerator, denominator, rationalFps);
            }
        }

        return (null, null, deliveryFrameRate);
    }

    private static readonly (int Numerator, int Denominator)[] CommonFlashbackFrameRateParts =
    {
        (24, 1),
        (24000, 1001),
        (25, 1),
        (30, 1),
        (30000, 1001),
        (50, 1),
        (60, 1),
        (60000, 1001),
        (100, 1),
        (120, 1),
        (120000, 1001),
        (144, 1),
        (240, 1)
    };

    private async Task<FinalizeResult> FinalizeFlashbackRecordingAsync(
        RecordingContext? recordingContext,
        FlashbackRecordingBoundarySnapshot recordingBoundary,
        CancellationToken cancellationToken)
    {
        var outputPath = recordingContext?.FinalOutputPath ?? string.Empty;

        // H3: Pause eviction BEFORE EndRecordingAsync to close the window where
        // eviction could delete segments between EndRecording (which resumes eviction
        // internally) and ExportFlashbackCoreAsync (which pauses it again).
        // With ref-counted eviction, the nested Pause from ExportFlashbackCoreAsync is safe.
        var backendLeaseHeld = false;
        try
        {
            await _flashbackBackendLeaseLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            backendLeaseHeld = true;

            return await _flashbackBackend.FinalizeRecordingAsync(
                    outputPath,
                    captureBoundarySnapshot: sink => CaptureFlashbackRecordingBoundarySnapshot(sink, recordingBoundary),
                    exportRecordingAsync: (startPts, endPts, exportOutputPath, ct) =>
                        ExportFlashbackCoreAsync(
                            startPts,
                            endPts,
                            exportOutputPath,
                            progress: null,
                            ct: ct,
                            requireCompleteLiveEdge: true,
                            throttleHighResolutionBaseline: false),
                    resumeEvictionBestEffort: ResumeFlashbackEvictionBestEffort,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            ReleaseFlashbackBackendLeaseIfHeld(ref backendLeaseHeld);
        }
    }

    private static bool IsFlashbackFinalizeCancellationResult(FinalizeResult result)
        => !result.Succeeded &&
           (string.Equals(result.StatusMessage, "Flashback export cancelled.", StringComparison.Ordinal) ||
            string.Equals(result.StatusMessage, "Flashback recording finalize cancelled.", StringComparison.Ordinal));

    // Flashback recording boundary capture: snapshot the final live edge exactly
    // once so export/finalize and fallback paths share the same accounting data.
    private sealed class FlashbackRecordingBoundarySnapshot
    {
        public bool Captured { get; set; }
        public long RecordingFramesDelivered { get; set; }
        public long RecordingFramesEnqueued { get; set; }
        public long RecordingFramesRejected { get; set; }
        public long RecordingQueueRejectedFrames { get; set; }
        public RecordingIntegrityCounterSnapshot? Counters { get; set; }
        public RecordingAudioIntegrityCounterSnapshot? AudioCounters { get; set; }
    }

    private void CaptureFlashbackRecordingBoundarySnapshot(
        FlashbackEncoderSink flashbackSink,
        FlashbackRecordingBoundarySnapshot recordingBoundary)
    {
        if (recordingBoundary.Captured)
        {
            return;
        }

        var flashbackVideoCapture = _videoPipeline.Capture;
        if (flashbackVideoCapture != null)
        {
            flashbackVideoCapture.EndFlashbackRecordingAccounting();
            _lastMfSourceReaderFramesDelivered = flashbackVideoCapture.VideoFramesArrived;
            _lastMfSourceReaderFramesDropped = flashbackVideoCapture.VideoFramesDropped;
            _lastMfSourceReaderNegotiatedFormat = flashbackVideoCapture.NegotiatedFormat;
            recordingBoundary.RecordingFramesDelivered = flashbackVideoCapture.RecordingFramesDelivered;
            recordingBoundary.RecordingFramesEnqueued = flashbackVideoCapture.VideoFramesWrittenToSink;
            recordingBoundary.RecordingFramesRejected = flashbackVideoCapture.RecordingFramesRejected;
            recordingBoundary.RecordingQueueRejectedFrames = flashbackVideoCapture.RecordingQueueRejectedFrames;
            Logger.Log(
                "VIDEO_DIAG flashback_recording_pipeline " +
                $"source_frames_during_recording={recordingBoundary.RecordingFramesDelivered} " +
                $"frames_accepted_by_flashback={recordingBoundary.RecordingFramesEnqueued} " +
                $"frames_rejected_by_boundary={recordingBoundary.RecordingFramesRejected} " +
                $"queue_rejections={recordingBoundary.RecordingQueueRejectedFrames} " +
                $"pipeline_drops={recordingBoundary.RecordingFramesDelivered - recordingBoundary.RecordingFramesEnqueued}");
        }

        recordingBoundary.Counters = CaptureFlashbackRecordingIntegrityCountersSinceBaseline(flashbackSink, flashbackVideoCapture);
        recordingBoundary.AudioCounters = GetRecordingAudioCountersSinceBaseline(
            CaptureRecordingAudioCounters(_previewAudioGraph.ProgramCapture, flashbackSink, _recordingBackend.SettingsSnapshot));
        recordingBoundary.Captured = true;
    }

    private async Task<FinalizeResult> StopAndDisposeFlashbackRecordingBackendAsync(CancellationToken cancellationToken)
    {
        var flashbackSink = _flashbackBackend.Sink!;
        var fbRecordingContext = _recordingBackend.DetachFlashbackBackend();
        var fbOutputPath = fbRecordingContext?.FinalOutputPath ?? (_lastOutputPath ?? string.Empty);
        var recordingBoundary = new FlashbackRecordingBoundarySnapshot();

        Volatile.Write(ref _flashbackRecordingFinalizeInProgress, 1);
        // Do not clear the backend sink here; it continues serving the buffer.

        FinalizeResult fbResult;
        OperationCanceledException? flashbackCancellationException = null;
        try
        {
            try
            {
                fbResult = await FinalizeFlashbackRecordingAsync(
                        fbRecordingContext,
                        recordingBoundary,
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            finally
            {
                Volatile.Write(ref _flashbackRecordingFinalizeInProgress, 0);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            flashbackCancellationException = new OperationCanceledException(cancellationToken);
            fbResult = FinalizeResult.Failure(fbOutputPath, "Flashback recording finalize cancelled.");
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_UNIFIED_RECORDING_FINALIZE_FAIL type={ex.GetType().Name} error='{ex.Message}'");
            fbResult = FinalizeResult.Failure(fbOutputPath, $"Flashback recording finalize failed: {ex.Message}");
        }

        CaptureFlashbackRecordingBoundarySnapshot(flashbackSink, recordingBoundary);

        if (cancellationToken.IsCancellationRequested && IsFlashbackFinalizeCancellationResult(fbResult))
        {
            flashbackCancellationException ??= new OperationCanceledException(cancellationToken);
        }

        _lastRecordingIntegrity = BuildRecordingIntegritySummary(
            backend: "Flashback",
            recordingActive: false,
            finalizeSucceeded: fbResult.Succeeded,
            finalizeStatus: fbResult.StatusMessage,
            completedUtc: DateTimeOffset.UtcNow,
            sourceFrames: recordingBoundary.RecordingFramesDelivered,
            acceptedFrames: recordingBoundary.RecordingFramesEnqueued,
            counters: recordingBoundary.Counters ?? CaptureFlashbackRecordingIntegrityCountersSinceBaseline(flashbackSink, _videoPipeline.Capture),
            audioCounters: recordingBoundary.AudioCounters ?? GetRecordingAudioCountersSinceBaseline(
                CaptureRecordingAudioCounters(_previewAudioGraph.ProgramCapture, flashbackSink, _recordingBackend.SettingsSnapshot)),
            recordingBoundaryRejectedFrames: recordingBoundary.RecordingFramesRejected,
            recordingQueueRejectedFrames: recordingBoundary.RecordingQueueRejectedFrames);
        _recordingIntegrityCounterBaseline = null;
        _recordingIntegrityAudioBaseline = null;
        LogRecordingIntegritySummary(_lastRecordingIntegrity);

        flashbackCancellationException = await ReconcileFlashbackBackendAfterRecordingFinalizeAsync(
            fbResult,
            flashbackCancellationException,
            cancellationToken).ConfigureAwait(false);

        _recordingStopwatch.Stop();
        _isRecording = false;
        if (!_isVideoPreviewActive) await StopTelemetryPollAsync().ConfigureAwait(false);
        _recordingBackend.ClearContextAndSettings();
        PublishRecordingFinalizedOutcome(fbResult, updateOutputPath: false);

        // Restart mic monitoring if preview is still active
        try
        {
            await RestartMicrophoneMonitorAfterRecordingAsync(
                new MicrophoneMonitorRestartOptions(
                    OnlyWhenMissing: true,
                    FlashbackAttachReason: null,
                    RestartLogEvent: null,
                    DisposeWarningEvent: "FLASHBACK_MIC_RESTART_DISPOSE_WARN"),
                cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            flashbackCancellationException ??= new OperationCanceledException(cancellationToken);
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_MIC_RESTART_WARN type={ex.GetType().Name} error='{ex.Message}'");
        }

        if (fbResult.Succeeded)
        {
            Logger.Log($"FLASHBACK_UNIFIED_RECORDING_STOP_OK output='{fbResult.OutputPath}'");
        }
        else
        {
            Logger.Log($"FLASHBACK_UNIFIED_RECORDING_STOP_FAIL output='{fbResult.OutputPath}'");
        }
        if (flashbackCancellationException != null)
        {
            throw flashbackCancellationException;
        }

        return fbResult;
    }

    private async Task<OperationCanceledException?> ReconcileFlashbackBackendAfterRecordingFinalizeAsync(
        FinalizeResult fbResult,
        OperationCanceledException? cancellationException,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!fbResult.Succeeded)
            {
                var hadPendingFlashbackSettingsChange = _pendingFlashbackSettingsChange;
                _pendingFlashbackSettingsChange = false;
                _flashbackBackend.PreserveRecoverySegments("recording_finalize_failed");
                Logger.Log(
                    "FLASHBACK_SETTINGS_APPLY_AFTER_RECORDING_DEFERRED " +
                    $"reason=recording_finalize_failed pending_settings={hadPendingFlashbackSettingsChange}");
            }
            else if (_pendingFlashbackSettingsChange)
            {
                _pendingFlashbackSettingsChange = false;
                Logger.Log("FLASHBACK_SETTINGS_APPLY_AFTER_RECORDING");
                await DisposeFlashbackPreviewBackendAsync(cancellationToken, purgeSegments: false).ConfigureAwait(false);
                var unifiedVideoCapture = _videoPipeline.Capture;
                var settings = _currentSettings;
                if (_flashbackEnabled && unifiedVideoCapture != null && settings != null)
                {
                    await EnsureFlashbackPreviewBackendAsync(unifiedVideoCapture, settings, cancellationToken).ConfigureAwait(false);
                }
            }
            else
            {
                await CycleFlashbackBufferAsync(cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            cancellationException ??= new OperationCanceledException(cancellationToken);
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_BUFFER_CYCLE_FAIL type={ex.GetType().Name} error='{ex.Message}'");
            RecordLastFlashbackFailure(ex);
            _flashbackBackend.PreserveRecoverySegments("buffer_cycle_failed");
            BeginFlashbackBackendCleanup(ex);
        }

        return cancellationException;
    }

    // Shared Flashback export pipeline: eviction pause, force-rotate, exporter request,
    // diagnostics completion, and cleanup.
    private delegate (bool Succeeded, TimeSpan InPoint, TimeSpan OutPoint, string? FailureMessage)
        FlashbackExportRangeResolver(FlashbackBufferManager manager);

    private static FlashbackExportRangeResolver CreateFlashbackExportRangeResolver(
        TimeSpan? inPoint,
        TimeSpan? outPoint,
        TimeSpan? inPointFilePts,
        TimeSpan? outPointFilePts)
    {
        return manager => ResolveFlashbackExportRangeAfterEvictionPaused(
            manager,
            inPoint,
            outPoint,
            inPointFilePts,
            outPointFilePts);
    }

    private static FlashbackExportRangeResolver CreateFlashbackExportLastNRangeResolver(double seconds)
        => manager => ResolveFlashbackExportLastNRangeAfterEvictionPaused(manager, seconds);

    private FinalizeResult FailFlashbackExport(
        string outputPath,
        string statusMessage,
        TimeSpan? inPoint = null,
        TimeSpan? outPoint = null)
    {
        var result = FinalizeResult.Failure(outputPath, statusMessage);
        Logger.Log($"FLASHBACK_EXPORT_REJECTED status='{statusMessage}' output='{outputPath}'");
        RecordRejectedFlashbackExportDiagnostics(outputPath, result, inPoint, outPoint);
        return result;
    }

    // Called from two contexts:
    // (1) Export methods - pass snapshotSink/snapshotBufferManager captured under session lock.
    // (2) FinalizeFlashbackRecordingAsync - runs under session lock, omits snapshots (field reads safe).
    private async Task<FinalizeResult> ExportFlashbackCoreAsync(
        TimeSpan inPoint, TimeSpan outPoint, string outputPath,
        IProgress<ExportProgress>? progress, CancellationToken ct,
        FlashbackEncoderSink? snapshotSink = null,
        FlashbackBufferManager? snapshotBufferManager = null,
        FlashbackExporter? snapshotExporter = null,
        bool requireCompleteLiveEdge = false,
        bool exportOperationLockAlreadyHeld = false,
        bool throttleHighResolutionBaseline = true,
        bool force = false,
        FlashbackExportRangeResolver? resolveRangeAfterEvictionPaused = null)
    {
        var flashbackSink = snapshotSink ?? _flashbackBackend.Sink;
        var bufferManager = snapshotBufferManager ?? _flashbackBackend.BufferManager;

        var exportId = 0L;
        var evictionPaused = false;
        var exportOperationLockHeld = exportOperationLockAlreadyHeld;
        try
        {
            if (!exportOperationLockAlreadyHeld)
            {
                try
                {
                    await _flashbackExportOperationLock.WaitAsync(ct).ConfigureAwait(false);
                    exportOperationLockHeld = true;
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    return FailFlashbackExport(outputPath, "Flashback export cancelled.", inPoint, outPoint);
                }
            }

            if (bufferManager == null)
            {
                return FailFlashbackExport(outputPath, "Flashback buffer not active", inPoint, outPoint);
            }

            var exporter = snapshotExporter;
            if (exporter == null)
            {
                exporter = _flashbackBackend.Exporter ??= new FlashbackExporter();
            }

            // Pause eviction so segments aren't deleted while the exporter reads them.
            // Range-based UI exports resolve relative buffer positions after this pause
            // so queued exports cannot use a stale valid-start snapshot.
            bufferManager.PauseEviction();
            evictionPaused = true;

            if (resolveRangeAfterEvictionPaused != null)
            {
                var resolvedRange = resolveRangeAfterEvictionPaused(bufferManager);
                inPoint = resolvedRange.InPoint;
                outPoint = resolvedRange.OutPoint;
                if (!resolvedRange.Succeeded)
                {
                    return FailFlashbackExport(
                        outputPath,
                        resolvedRange.FailureMessage ?? "Flashback export range is empty or invalid.",
                        inPoint,
                        outPoint);
                }
            }

            exportId = BeginFlashbackExportDiagnostics(inPoint, outPoint, outputPath);
            var diagnosticProgress = CreateFlashbackExportProgressSink(exportId, progress);

            var preparedExport = PrepareFlashbackExportRequest(
                bufferManager,
                flashbackSink,
                exportId,
                inPoint,
                outPoint,
                outputPath,
                requireCompleteLiveEdge,
                force,
                throttleHighResolutionBaseline,
                ct);
            if (preparedExport.FailureResult is { } preparationFailure)
            {
                return preparationFailure;
            }

            var request = preparedExport.Request
                ?? throw new InvalidOperationException("Flashback export request preparation returned no request.");
            var result = await exporter.ExportAsync(request, diagnosticProgress, ct).ConfigureAwait(false);
            if (preparedExport.ForceRotateFallbackUsed && result.Succeeded)
            {
                result = FinalizeResult.Success(
                    result.OutputPath,
                    $"{result.StatusMessage} (live-edge partial fallback: active segment was not closed before timeout; export may omit the newest frames)");
            }

            RecordLastFlashbackExportResult(exportId, result);
            CompleteFlashbackExportDiagnostics(exportId, result);
            return result;
        }
        catch (Exception ex)
        {
            var statusMessage = ex is OperationCanceledException && ct.IsCancellationRequested
                ? "Flashback export cancelled."
                : ex.Message;
            Logger.Log(
                $"FLASHBACK_EXPORT_CORE_FAIL id={exportId} type={ex.GetType().Name} " +
                $"cancelled={ct.IsCancellationRequested} msg='{statusMessage}'");
            var failure = FinalizeResult.Failure(outputPath, statusMessage);
            if (exportId != 0)
            {
                RecordLastFlashbackExportResult(exportId, failure);
                CompleteFlashbackExportDiagnostics(exportId, failure);
            }
            else
            {
                RecordRejectedFlashbackExportDiagnostics(outputPath, failure, inPoint, outPoint);
            }
            return failure;
        }
        finally
        {
            if (evictionPaused)
            {
                ResumeFlashbackEvictionBestEffort(bufferManager, "flashback_export");
            }
            if (exportOperationLockHeld)
            {
                ReleaseFlashbackExportOperationLockIfHeld(ref exportOperationLockHeld);
            }
        }
    }

    private FlashbackExportPreparationResult PrepareFlashbackExportRequest(
        FlashbackBufferManager bufferManager,
        FlashbackEncoderSink? flashbackSink,
        long exportId,
        TimeSpan inPoint,
        TimeSpan outPoint,
        string outputPath,
        bool requireCompleteLiveEdge,
        bool force,
        bool throttleHighResolutionBaseline,
        CancellationToken ct)
    {
        var forceRotatePreparation = PrepareFlashbackExportForceRotateSegments(
            bufferManager,
            flashbackSink,
            exportId,
            inPoint,
            outPoint,
            outputPath,
            requireCompleteLiveEdge,
            ct);
        if (forceRotatePreparation.FailureResult is { } forceRotateFailure)
        {
            return FlashbackExportPreparationResult.Failure(forceRotateFailure);
        }

        var segmentPaths = forceRotatePreparation.SegmentPaths;
        var forceRotateFallbackUsed = forceRotatePreparation.ForceRotateFallbackUsed;
        string? tsPath = null;

        // Fallback: single-file export if no segments available.
        if (segmentPaths == null)
        {
            tsPath = bufferManager.ActiveFilePath;
            if (string.IsNullOrWhiteSpace(tsPath))
            {
                var result = FinalizeResult.Failure(outputPath, "Flashback buffer has no active file");
                RecordLastFlashbackExportResult(exportId, result);
                CompleteFlashbackExportDiagnostics(exportId, result);
                return FlashbackExportPreparationResult.Failure(result);
            }

            Logger.Log(
                "FLASHBACK_EXPORT_ACTIVE_FILE_FALLBACK " +
                $"path='{tsPath}' in_ms={(long)inPoint.TotalMilliseconds} " +
                $"out_ms={(long)(outPoint == TimeSpan.MaxValue ? -1 : outPoint.TotalMilliseconds)}");
        }

        var request = new FlashbackExportRequest
        {
            Segments = BuildFlashbackExportSegments(bufferManager, segmentPaths),
            SegmentPaths = segmentPaths,
            InputTsPath = tsPath,
            InPoint = inPoint,
            OutPoint = outPoint,
            OutputPath = outputPath,
            FastStart = false,
            Force = force,
            AdaptiveThrottleDelayMsProvider = CreateFlashbackExportThrottleDelayProvider(
                flashbackSink,
                throttleHighResolutionBaseline),
        };

        return FlashbackExportPreparationResult.Ready(request, forceRotateFallbackUsed);
    }

    private static IReadOnlyList<FlashbackExportSegment>? BuildFlashbackExportSegments(
        FlashbackBufferManager? bufferManager,
        IReadOnlyList<string>? segmentPaths)
    {
        if (segmentPaths is not { Count: > 0 })
        {
            return null;
        }

        var segmentInfo = bufferManager?.GetSegmentInfoList()
            .Where(segment => !segment.IsActive)
            .Select(segment => (Key: TryGetFullPath(segment.Path), Segment: segment))
            .Where(entry => !string.IsNullOrWhiteSpace(entry.Key))
            .GroupBy(entry => entry.Key!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First().Segment, StringComparer.OrdinalIgnoreCase);
        var segments = new List<FlashbackExportSegment>(segmentPaths.Count);
        foreach (var path in segmentPaths)
        {
            var pathKey = TryGetFullPath(path);
            if (segmentInfo != null &&
                pathKey != null &&
                segmentInfo.TryGetValue(pathKey, out var info))
            {
                var startPts = FromSegmentMilliseconds(info.StartPtsMs);
                var endPts = FromSegmentMilliseconds(info.EndPtsMs);
                if (endPts < startPts)
                {
                    endPts = startPts;
                }

                segments.Add(new FlashbackExportSegment
                {
                    Path = path,
                    StartPts = startPts,
                    EndPts = endPts
                });
            }
            else
            {
                segments.Add(new FlashbackExportSegment { Path = path });
            }
        }

        return segments;
    }

    private static Func<int>? CreateFlashbackExportThrottleDelayProvider(
        FlashbackEncoderSink? flashbackSink,
        bool throttleHighResolutionBaseline = true)
    {
        if (flashbackSink == null)
        {
            return null;
        }

        var lastLoggedTick = 0L;
        return () =>
        {
            var capacity = flashbackSink.VideoQueueCapacityFrames;
            if (capacity <= 0)
            {
                return 0;
            }

            var depth = flashbackSink.VideoQueueCount;
            var queueRatio = Math.Clamp(depth / (double)capacity, 0.0, 1.0);
            var oldestFrameAgeMs = flashbackSink.VideoQueueOldestFrameAgeMs;
            var delayMs = ResolveFlashbackExportThrottleDelayMs(
                queueRatio,
                oldestFrameAgeMs,
                throttleHighResolutionBaseline && IsHighResolutionFlashbackExport(flashbackSink));
            if (delayMs <= 0)
            {
                return 0;
            }

            var now = Environment.TickCount64;
            if (now - lastLoggedTick >= 1_000)
            {
                lastLoggedTick = now;
                Logger.Log(
                    "FLASHBACK_EXPORT_LIVE_THROTTLE " +
                    $"delay_ms={delayMs} queue={depth}/{capacity} " +
                    $"queue_ratio={queueRatio:0.00} oldest_ms={oldestFrameAgeMs}");
            }

            return delayMs;
        };
    }

    private static bool IsHighResolutionFlashbackExport(FlashbackEncoderSink flashbackSink)
        => flashbackSink.EncoderWidth >= 3840 || flashbackSink.EncoderHeight >= 2160;

    private static int ResolveFlashbackExportThrottleDelayMs(
        double queueRatio,
        long oldestFrameAgeMs,
        bool liveHighResolution = false)
    {
        if (queueRatio >= 0.85 || oldestFrameAgeMs >= 90)
        {
            return 25;
        }

        if (queueRatio >= 0.70 || oldestFrameAgeMs >= 50)
        {
            return 20;
        }

        if (liveHighResolution)
        {
            return 25;
        }

        if (queueRatio >= 0.50 || oldestFrameAgeMs >= 30)
        {
            return 16;
        }

        return 0;
    }

    private static string? TryGetFullPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        try
        {
            return Path.GetFullPath(path);
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_PATH_NORMALIZE_WARN path='{path}' type={ex.GetType().Name} msg='{ex.Message}'");
            return null;
        }
    }

    private static TimeSpan FromSegmentMilliseconds(long milliseconds)
    {
        if (milliseconds <= 0)
        {
            return TimeSpan.Zero;
        }

        return milliseconds >= TimeSpan.MaxValue.TotalMilliseconds
            ? TimeSpan.MaxValue
            : TimeSpan.FromMilliseconds(milliseconds);
    }

    private FlashbackExportForceRotatePreparation PrepareFlashbackExportForceRotateSegments(
        FlashbackBufferManager bufferManager,
        FlashbackEncoderSink? flashbackSink,
        long exportId,
        TimeSpan inPoint,
        TimeSpan outPoint,
        string outputPath,
        bool requireCompleteLiveEdge,
        CancellationToken ct)
    {
        if (flashbackSink == null)
        {
            return FlashbackExportForceRotatePreparation.Ready(null, forceRotateFallbackUsed: false);
        }

        var forceRotateResult = flashbackSink.ForceRotateForExport(inPoint, outPoint, ct);
        var segmentPaths = forceRotateResult.SegmentPaths;
        if (forceRotateResult.Status == FlashbackForceRotateStatus.Failed)
        {
            var preservedArtifacts = bufferManager.GetValidSegmentPaths(inPoint, outPoint);
            var result = FinalizeResult.Failure(
                outputPath,
                "Flashback export failed: live-edge segment rotation failed.",
                preservedArtifacts);
            RecordLastFlashbackExportResult(exportId, result);
            CompleteFlashbackExportDiagnostics(exportId, result);
            Logger.Log(
                "FLASHBACK_EXPORT_FORCE_ROTATE_FAILED " +
                $"preserved_segments={preservedArtifacts.Count} " +
                $"in_ms={(long)inPoint.TotalMilliseconds} " +
                $"out_ms={(long)(outPoint == TimeSpan.MaxValue ? -1 : outPoint.TotalMilliseconds)}");
            return FlashbackExportForceRotatePreparation.Failure(result);
        }

        if (forceRotateResult.Status == FlashbackForceRotateStatus.CommittedPending)
        {
            var preservedArtifacts = bufferManager.GetValidSegmentPaths(inPoint, outPoint);
            var result = FinalizeResult.Failure(
                outputPath,
                requireCompleteLiveEdge
                    ? "Flashback recording finalize failed: live-edge segment was not closed before timeout."
                    : "Flashback export failed: live-edge segment rotation committed but did not complete before timeout.",
                preservedArtifacts);
            RecordLastFlashbackExportResult(exportId, result);
            CompleteFlashbackExportDiagnostics(exportId, result);
            Logger.Log(
                "FLASHBACK_EXPORT_FORCE_ROTATE_COMMITTED_PENDING_FAIL " +
                $"preserved_segments={preservedArtifacts.Count} " +
                $"in_ms={(long)inPoint.TotalMilliseconds} " +
                $"out_ms={(long)outPoint.TotalMilliseconds}");
            return FlashbackExportForceRotatePreparation.Failure(result);
        }

        var forceRotateFallbackUsed = false;
        if (segmentPaths.Count == 0)
        {
            if (requireCompleteLiveEdge)
            {
                var preservedArtifacts = bufferManager.GetValidSegmentPaths(inPoint, outPoint);
                var result = FinalizeResult.Failure(
                    outputPath,
                    "Flashback recording finalize failed: live-edge segment was not closed before timeout.",
                    preservedArtifacts);
                RecordLastFlashbackExportResult(exportId, result);
                CompleteFlashbackExportDiagnostics(exportId, result);
                Logger.Log(
                    "FLASHBACK_RECORDING_EXPORT_INCOMPLETE_FAIL " +
                    $"preserved_segments={preservedArtifacts.Count} " +
                    $"in_ms={(long)inPoint.TotalMilliseconds} " +
                    $"out_ms={(long)outPoint.TotalMilliseconds}");
                return FlashbackExportForceRotatePreparation.Failure(result);
            }

            // ForceRotate timed out (AV1 encoder can be too slow to drain
            // within the 3-second window). Completed segments before the
            // active one are already finalized - query them directly.
            // NOTE: The encoding thread may still be completing the rotation.
            // This returns only already-completed segments - the live-edge
            // segment may be missed if it hasn't been finalized yet. This is
            // acceptable: the previous behavior returned a near-empty file.
            segmentPaths = bufferManager.GetValidSegmentPaths(inPoint, outPoint);
            if (segmentPaths is { Count: > 0 })
            {
                forceRotateFallbackUsed = true;
                RecordFlashbackExportForceRotateFallback(exportId, segmentPaths.Count, inPoint, outPoint);
                Logger.Log($"FLASHBACK_EXPORT_FORCE_ROTATE_FALLBACK reason=force_rotate_timeout segments={segmentPaths.Count} in_ms={(long)inPoint.TotalMilliseconds} out_ms={(long)outPoint.TotalMilliseconds}");
            }
            else
            {
                segmentPaths = null;
            }
        }

        return FlashbackExportForceRotatePreparation.Ready(segmentPaths, forceRotateFallbackUsed);
    }

    private static (bool Succeeded, TimeSpan InPoint, TimeSpan OutPoint, string? FailureMessage)
        ResolveFlashbackExportRangeAfterEvictionPaused(
            FlashbackBufferManager manager,
            TimeSpan? inPoint,
            TimeSpan? outPoint,
            TimeSpan? inPointFilePts,
            TimeSpan? outPointFilePts)
    {
        var validStart = manager.ValidStartPts;
        if (inPointFilePts.HasValue || outPointFilePts.HasValue)
        {
            var absoluteInPoint = inPointFilePts ?? validStart;
            var absoluteOutPoint = outPointFilePts ?? TimeSpan.MaxValue;
            if (absoluteInPoint < validStart)
            {
                return (false, absoluteInPoint, absoluteOutPoint, "Flashback export in point has been evicted from the buffer.");
            }

            if (absoluteOutPoint != TimeSpan.MaxValue && absoluteOutPoint <= validStart)
            {
                return (false, absoluteInPoint, absoluteOutPoint, "Flashback export out point has been evicted from the buffer.");
            }

            return absoluteOutPoint != TimeSpan.MaxValue && absoluteOutPoint <= absoluteInPoint
                ? (false, absoluteInPoint, absoluteOutPoint, "Flashback export range is empty or invalid.")
                : (true, absoluteInPoint, absoluteOutPoint, null);
        }

        var bufferedDuration = manager.BufferedDuration;
        var bufferInPoint = ClampFlashbackBufferPosition(inPoint ?? TimeSpan.Zero, bufferedDuration);
        var bufferOutPoint = outPoint.HasValue
            ? ClampFlashbackBufferPosition(outPoint.Value, bufferedDuration)
            : TimeSpan.MaxValue;
        var fileInPoint = AddFlashbackPtsOffsetOrMax(bufferInPoint, validStart);
        var fileOutPoint = AddFlashbackPtsOffsetOrMax(bufferOutPoint, validStart);
        return fileOutPoint != TimeSpan.MaxValue && fileOutPoint <= fileInPoint
            ? (false, fileInPoint, fileOutPoint, "Flashback export range is empty or invalid.")
            : (true, fileInPoint, fileOutPoint, null);
    }

    private static (bool Succeeded, TimeSpan InPoint, TimeSpan OutPoint, string? FailureMessage)
        ResolveFlashbackExportLastNRangeAfterEvictionPaused(FlashbackBufferManager manager, double seconds)
    {
        var bufferedDuration = manager.BufferedDuration;
        var validStart = manager.ValidStartPts;
        var rangeStart = bufferedDuration.TotalSeconds > seconds
            ? TimeSpan.FromSeconds(bufferedDuration.TotalSeconds - seconds)
            : TimeSpan.Zero;
        var fileInPoint = AddFlashbackPtsOffsetOrMax(rangeStart, validStart);
        return (true, fileInPoint, TimeSpan.MaxValue, null);
    }

    private static TimeSpan ClampFlashbackBufferPosition(TimeSpan position, TimeSpan bufferedDuration)
    {
        if (bufferedDuration <= TimeSpan.Zero)
        {
            return TimeSpan.Zero;
        }

        if (position < TimeSpan.Zero)
        {
            return TimeSpan.Zero;
        }

        return position > bufferedDuration ? bufferedDuration : position;
    }

    private static TimeSpan AddFlashbackPtsOffsetOrMax(TimeSpan position, TimeSpan offset)
    {
        if (position == TimeSpan.MaxValue || offset == TimeSpan.MaxValue)
        {
            return TimeSpan.MaxValue;
        }

        if (position < TimeSpan.Zero)
        {
            position = TimeSpan.Zero;
        }

        if (offset <= TimeSpan.Zero)
        {
            return position;
        }

        return position > TimeSpan.MaxValue - offset
            ? TimeSpan.MaxValue
            : position + offset;
    }

    private sealed record FlashbackExportPreparationResult(
        FlashbackExportRequest? Request,
        FinalizeResult? FailureResult,
        bool ForceRotateFallbackUsed)
    {
        public static FlashbackExportPreparationResult Ready(
            FlashbackExportRequest request,
            bool forceRotateFallbackUsed) =>
            new(request, null, forceRotateFallbackUsed);

        public static FlashbackExportPreparationResult Failure(FinalizeResult result) =>
            new(null, result, false);
    }

    private sealed record FlashbackExportForceRotatePreparation(
        IReadOnlyList<string>? SegmentPaths,
        FinalizeResult? FailureResult,
        bool ForceRotateFallbackUsed)
    {
        public static FlashbackExportForceRotatePreparation Ready(
            IReadOnlyList<string>? segmentPaths,
            bool forceRotateFallbackUsed) =>
            new(segmentPaths, null, forceRotateFallbackUsed);

        public static FlashbackExportForceRotatePreparation Failure(FinalizeResult result) =>
            new(null, result, false);
    }

    private void RecordLastFlashbackExportResult(long exportId, FinalizeResult result)
    {
        lock (_flashbackExportDiagnosticsLock)
        {
            _lastExportResult = result;
            Volatile.Write(ref _lastFlashbackExportResultId, exportId);
        }
    }

    private long BeginFlashbackExportDiagnostics(TimeSpan inPoint, TimeSpan outPoint, string outputPath)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        lock (_flashbackExportDiagnosticsLock)
        {
            var exportId = Interlocked.Increment(ref _flashbackExportId);
            _flashbackExportActive = true;
            _flashbackExportStatus = "Running";
            _flashbackExportOutputPath = outputPath;
            _flashbackExportStartedUtcUnixMs = now;
            _flashbackExportLastProgressUtcUnixMs = now;
            _flashbackExportCompletedUtcUnixMs = 0;
            _flashbackExportSegmentsProcessed = 0;
            _flashbackExportTotalSegments = 0;
            _flashbackExportPercent = 0;
            _flashbackExportInPointMs = (long)inPoint.TotalMilliseconds;
            _flashbackExportOutPointMs = outPoint == TimeSpan.MaxValue ? -1 : (long)outPoint.TotalMilliseconds;
            _flashbackExportMessage = string.Empty;
            _flashbackExportFailureKind = string.Empty;

            return exportId;
        }
    }

    private void RecordRejectedFlashbackExportDiagnostics(
        string outputPath,
        FinalizeResult result,
        TimeSpan? inPoint = null,
        TimeSpan? outPoint = null)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        lock (_flashbackExportDiagnosticsLock)
        {
            if (_flashbackExportActive)
            {
                _lastExportResult = result;
                Volatile.Write(ref _lastFlashbackExportResultId, 0);
                Logger.Log(
                    "FLASHBACK_EXPORT_REJECTED_DIAGNOSTICS_DEFERRED " +
                    $"active_id={_flashbackExportId} status='{_flashbackExportStatus}' " +
                    $"rejected_status='{result.StatusMessage}' output='{outputPath}'");
                return;
            }

            var exportId = Interlocked.Increment(ref _flashbackExportId);
            _flashbackExportId = exportId;
            _flashbackExportActive = false;
            _flashbackExportStatus = IsFlashbackExportCancelled(result.StatusMessage) ? "Cancelled" : "Failed";
            _flashbackExportOutputPath = outputPath;
            _flashbackExportStartedUtcUnixMs = now;
            _flashbackExportLastProgressUtcUnixMs = now;
            _flashbackExportCompletedUtcUnixMs = now;
            _flashbackExportSegmentsProcessed = 0;
            _flashbackExportTotalSegments = 0;
            _flashbackExportPercent = 0;
            _flashbackExportInPointMs = inPoint.HasValue ? (long)inPoint.Value.TotalMilliseconds : 0;
            _flashbackExportOutPointMs = outPoint.HasValue
                ? outPoint.Value == TimeSpan.MaxValue ? -1 : (long)outPoint.Value.TotalMilliseconds
                : 0;
            _flashbackExportMessage = result.StatusMessage;
            _flashbackExportFailureKind = ClassifyFlashbackExportFailureKind(result.StatusMessage);
            RecordLastFlashbackExportResult(exportId, result);
        }
    }

    private void CompleteFlashbackExportDiagnostics(long exportId, FinalizeResult result)
    {
        if (Volatile.Read(ref _flashbackExportId) != exportId)
        {
            return;
        }

        lock (_flashbackExportDiagnosticsLock)
        {
            if (_flashbackExportId != exportId)
            {
                return;
            }

            _flashbackExportActive = false;
            _flashbackExportStatus = result.Succeeded
                ? "Succeeded"
                : IsFlashbackExportCancelled(result.StatusMessage)
                    ? "Cancelled"
                    : "Failed";
            var completedUtcUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            _flashbackExportCompletedUtcUnixMs = completedUtcUnixMs;
            _flashbackExportLastProgressUtcUnixMs = completedUtcUnixMs;
            _flashbackExportMessage = result.StatusMessage;
            _flashbackExportFailureKind = result.Succeeded
                ? string.Empty
                : ClassifyFlashbackExportFailureKind(result.StatusMessage);
            if (result.Succeeded && _flashbackExportPercent < 100)
            {
                _flashbackExportPercent = 100;
            }
        }
    }

    private IProgress<ExportProgress> CreateFlashbackExportProgressSink(
        long exportId,
        IProgress<ExportProgress>? innerProgress)
    {
        return new FlashbackExportProgressForwarder(progress =>
        {
            UpdateFlashbackExportProgress(exportId, progress);
            try
            {
                innerProgress?.Report(progress);
            }
            catch (Exception ex)
            {
                Logger.Log($"FLASHBACK_EXPORT_PROGRESS_FORWARD_WARN id={exportId} type={ex.GetType().Name} msg='{ex.Message}'");
            }
        });
    }

    private void UpdateFlashbackExportProgress(long exportId, ExportProgress progress)
    {
        if (Volatile.Read(ref _flashbackExportId) != exportId)
        {
            return;
        }

        lock (_flashbackExportDiagnosticsLock)
        {
            if (_flashbackExportId != exportId || !_flashbackExportActive)
            {
                return;
            }

            var rawTotalSegments = progress.TotalSegments;
            var rawSegmentsProcessed = progress.SegmentsProcessed;
            var rawPercent = progress.Percent;
            var totalSegments = Math.Max(0, rawTotalSegments);
            var segmentsProcessed = Math.Max(0, rawSegmentsProcessed);
            if (totalSegments > 0 && segmentsProcessed > totalSegments)
            {
                segmentsProcessed = totalSegments;
            }

            var percent = double.IsFinite(rawPercent)
                ? Math.Clamp(rawPercent, 0.0, 100.0)
                : 0.0;
            if (rawTotalSegments != totalSegments ||
                rawSegmentsProcessed != segmentsProcessed ||
                !double.IsFinite(rawPercent) ||
                rawPercent != percent)
            {
                Logger.Log(
                    $"FLASHBACK_EXPORT_PROGRESS_NORMALIZED id={exportId} " +
                    $"raw_segments={rawSegmentsProcessed}/{rawTotalSegments} " +
                    $"segments={segmentsProcessed}/{totalSegments} " +
                    $"raw_percent={rawPercent:0.###} percent={percent:0.###}");
            }

            _flashbackExportSegmentsProcessed = segmentsProcessed;
            _flashbackExportTotalSegments = totalSegments;
            _flashbackExportPercent = percent;
            _flashbackExportLastProgressUtcUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }
    }

    private void RecordFlashbackExportForceRotateFallback(
        long exportId,
        int segmentCount,
        TimeSpan inPoint,
        TimeSpan outPoint)
    {
        if (Volatile.Read(ref _flashbackExportId) != exportId)
        {
            return;
        }

        lock (_flashbackExportDiagnosticsLock)
        {
            if (_flashbackExportId != exportId)
            {
                return;
            }

            _flashbackExportForceRotateFallbacks++;
            _flashbackExportLastForceRotateFallbackUtcUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            _flashbackExportLastForceRotateFallbackSegments = Math.Max(0, segmentCount);
            _flashbackExportLastForceRotateFallbackInPointMs = (long)inPoint.TotalMilliseconds;
            _flashbackExportLastForceRotateFallbackOutPointMs = outPoint == TimeSpan.MaxValue
                ? -1
                : (long)outPoint.TotalMilliseconds;
        }
    }

    private FlashbackExportHealthSnapshotFields CaptureFlashbackExportHealthSnapshotFields(
        long snapshotUtcUnixMs)
    {
        FlashbackExportHealthSnapshotFields export;
        lock (_flashbackExportDiagnosticsLock)
        {
            export = new FlashbackExportHealthSnapshotFields(
                _flashbackExportActive,
                _flashbackExportId,
                _flashbackExportStatus,
                _flashbackExportOutputPath,
                _flashbackExportStartedUtcUnixMs,
                _flashbackExportLastProgressUtcUnixMs,
                _flashbackExportCompletedUtcUnixMs,
                _flashbackExportSegmentsProcessed,
                _flashbackExportTotalSegments,
                _flashbackExportPercent,
                _flashbackExportInPointMs,
                _flashbackExportOutPointMs,
                _flashbackExportMessage,
                _flashbackExportFailureKind,
                _flashbackExportForceRotateFallbacks,
                _flashbackExportLastForceRotateFallbackUtcUnixMs,
                _flashbackExportLastForceRotateFallbackSegments,
                _flashbackExportLastForceRotateFallbackInPointMs,
                _flashbackExportLastForceRotateFallbackOutPointMs,
                _lastFlashbackExportResultId,
                _lastExportResult,
                0,
                0,
                0,
                0);
        }

        var elapsedMs = ComputeFlashbackExportElapsedMs(
            export.Active,
            export.StartedUtcUnixMs,
            export.CompletedUtcUnixMs,
            snapshotUtcUnixMs);
        var lastProgressAgeMs = ComputeFlashbackExportLastProgressAgeMs(
            export.Active,
            export.StartedUtcUnixMs,
            export.LastProgressUtcUnixMs,
            snapshotUtcUnixMs);
        var outputBytes = GetFileLengthOrZero(
            !string.IsNullOrWhiteSpace(export.OutputPath)
                ? export.OutputPath
                : export.LastResult?.OutputPath);
        var throughputBytesPerSec = elapsedMs > 0
            ? outputBytes / (elapsedMs / 1000.0)
            : 0;

        return export with
        {
            ElapsedMs = elapsedMs,
            LastProgressAgeMs = lastProgressAgeMs,
            OutputBytes = outputBytes,
            ThroughputBytesPerSec = throughputBytesPerSec
        };
    }

    private static long ComputeFlashbackExportElapsedMs(
        bool active,
        long startedUtcUnixMs,
        long completedUtcUnixMs,
        long nowUtcUnixMs)
    {
        if (startedUtcUnixMs <= 0)
        {
            return 0;
        }

        var endUtcUnixMs = active
            ? nowUtcUnixMs
            : completedUtcUnixMs > 0
                ? completedUtcUnixMs
                : nowUtcUnixMs;

        return Math.Max(0, endUtcUnixMs - startedUtcUnixMs);
    }

    private static long ComputeFlashbackExportLastProgressAgeMs(
        bool active,
        long startedUtcUnixMs,
        long lastProgressUtcUnixMs,
        long nowUtcUnixMs)
    {
        if (!active)
        {
            return 0;
        }

        var referenceUtcUnixMs = lastProgressUtcUnixMs > 0
            ? lastProgressUtcUnixMs
            : startedUtcUnixMs;

        return referenceUtcUnixMs > 0
            ? Math.Max(0, nowUtcUnixMs - referenceUtcUnixMs)
            : 0;
    }

    private static long GetFileLengthOrZero(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return 0;
        }

        try
        {
            return new FileInfo(path).Length;
        }
        catch
        {
            return 0;
        }
    }

    private static bool IsFlashbackExportCancelled(string? statusMessage)
        => statusMessage?.Contains("cancel", StringComparison.OrdinalIgnoreCase) == true;

    internal static string ClassifyFlashbackExportFailureKind(string? statusMessage)
    {
        if (string.IsNullOrWhiteSpace(statusMessage))
        {
            return string.Empty;
        }

        if (IsFlashbackExportCancelled(statusMessage))
        {
            return "Cancelled";
        }

        if (ContainsFlashbackExportFailureText(statusMessage, "request is required") ||
            ContainsFlashbackExportFailureText(statusMessage, "duration must be finite"))
        {
            return "InvalidRequest";
        }

        if (ContainsFlashbackExportFailureText(statusMessage, "active recording backend"))
        {
            return "UnavailableDuringRecording";
        }

        if (ContainsFlashbackExportFailureText(statusMessage, "buffer not active"))
        {
            return "BufferInactive";
        }

        if (ContainsFlashbackExportFailureText(statusMessage, "in point") ||
            ContainsFlashbackExportFailureText(statusMessage, "export range"))
        {
            return "InvalidRange";
        }

        if (ContainsFlashbackExportFailureText(statusMessage, "output path") ||
            ContainsFlashbackExportFailureText(statusMessage, "output directory") ||
            ContainsFlashbackExportFailureText(statusMessage, "destination file already exists") ||
            ContainsFlashbackExportFailureText(statusMessage, "does not overwrite existing files") ||
            ContainsFlashbackExportFailureText(statusMessage, "overwrite source"))
        {
            return "InvalidOutputPath";
        }

        if (ContainsFlashbackExportFailureText(statusMessage, "operation=avio_open2") ||
            ContainsFlashbackExportFailureText(statusMessage, "operation=avformat_alloc_output_context2") ||
            ContainsFlashbackExportFailureText(statusMessage, "operation=avformat_new_stream") ||
            ContainsFlashbackExportFailureText(statusMessage, "operation=avcodec_parameters_copy") ||
            ContainsFlashbackExportFailureText(statusMessage, "operation=av_dict_set") ||
            ContainsFlashbackExportFailureText(statusMessage, "operation=avformat_write_header") ||
            ContainsFlashbackExportFailureText(statusMessage, "operation=av_interleaved_write_frame") ||
            ContainsFlashbackExportFailureText(statusMessage, "operation=av_write_trailer") ||
            ContainsFlashbackExportFailureText(statusMessage, "output file length unavailable") ||
            ContainsFlashbackExportFailureText(statusMessage, "temporary export file was not created") ||
            ContainsFlashbackExportFailureText(statusMessage, "access is denied") ||
            ContainsFlashbackExportFailureText(statusMessage, "permission denied") ||
            ContainsFlashbackExportFailureText(statusMessage, "sharing violation"))
        {
            return "OutputWriteFailed";
        }

        if (ContainsFlashbackExportFailureText(statusMessage, "rotation failed"))
        {
            return "ForceRotateFailed";
        }

        if (ContainsFlashbackExportFailureText(statusMessage, "live-edge segment"))
        {
            return "IncompleteLiveEdge";
        }

        if (ContainsFlashbackExportFailureText(statusMessage, "no segment paths") ||
            ContainsFlashbackExportFailureText(statusMessage, "segment path") ||
            ContainsFlashbackExportFailureText(statusMessage, "segment files") ||
            ContainsFlashbackExportFailureText(statusMessage, "readable segment"))
        {
            return "SegmentUnavailable";
        }

        if (ContainsFlashbackExportFailureText(statusMessage, "input file not found") ||
            ContainsFlashbackExportFailureText(statusMessage, "buffer has no active file"))
        {
            return "InputUnavailable";
        }

        if (ContainsFlashbackExportFailureText(statusMessage, "operation=avformat_open_input") ||
            ContainsFlashbackExportFailureText(statusMessage, "operation=av_read_frame"))
        {
            return "InputReadFailed";
        }

        if (ContainsFlashbackExportFailureText(statusMessage, "input context") ||
            ContainsFlashbackExportFailureText(statusMessage, "input had no streams") ||
            ContainsFlashbackExportFailureText(statusMessage, "stream count"))
        {
            return "InvalidInputStream";
        }

        if (ContainsFlashbackExportFailureText(statusMessage, "no usable video stream") ||
            ContainsFlashbackExportFailureText(statusMessage, "no segment had complete video parameters") ||
            ContainsFlashbackExportFailureText(statusMessage, "output file is empty") ||
            ContainsFlashbackExportFailureText(statusMessage, "no video packets") ||
            ContainsFlashbackExportFailureText(statusMessage, "no packets"))
        {
            return "NoMediaWritten";
        }

        if (ContainsFlashbackExportFailureText(statusMessage, "disposed"))
        {
            return "Disposed";
        }

        if (ContainsFlashbackExportFailureText(statusMessage, "timeout") ||
            ContainsFlashbackExportFailureText(statusMessage, "timed out"))
        {
            return "Timeout";
        }

        return "Failed";
    }

    private static bool ContainsFlashbackExportFailureText(string statusMessage, string value)
        => statusMessage.Contains(value, StringComparison.OrdinalIgnoreCase);

    private sealed class FlashbackExportProgressForwarder : IProgress<ExportProgress>
    {
        private readonly Action<ExportProgress> _onProgress;

        public FlashbackExportProgressForwarder(Action<ExportProgress> onProgress)
        {
            _onProgress = onProgress;
        }

        public void Report(ExportProgress value)
            => _onProgress(value);
    }

    private readonly record struct FlashbackExportHealthSnapshotFields(
        bool Active,
        long Id,
        string Status,
        string OutputPath,
        long StartedUtcUnixMs,
        long LastProgressUtcUnixMs,
        long CompletedUtcUnixMs,
        int SegmentsProcessed,
        int TotalSegments,
        double Percent,
        long InPointMs,
        long OutPointMs,
        string Message,
        string FailureKind,
        long ForceRotateFallbacks,
        long LastForceRotateFallbackUtcUnixMs,
        int LastForceRotateFallbackSegments,
        long LastForceRotateFallbackInPointMs,
        long LastForceRotateFallbackOutPointMs,
        long LastResultId,
        FinalizeResult? LastResult,
        long ElapsedMs,
        long LastProgressAgeMs,
        long OutputBytes,
        double ThroughputBytesPerSec);

    // Flashback export entry points: range export, last-N-seconds export, and
    // operation-specific range resolution before the shared core pipeline runs.
    internal async Task<FinalizeResult> ExportFlashbackRangeAsync(
        TimeSpan? inPoint, TimeSpan? outPoint, string outputPath,
        IProgress<ExportProgress>? progress,
        CancellationToken ct,
        TimeSpan? inPointFilePts = null,
        TimeSpan? outPointFilePts = null,
        bool force = false)
    {
        var snapshotResult = await SnapshotFlashbackExportBackendAsync(
                outputPath,
                operationName: "range",
                sessionReleaseOperation: "flashback_export_snapshot_session",
                ct)
            .ConfigureAwait(false);
        if (snapshotResult.Failure != null)
        {
            return snapshotResult.Failure;
        }

        var snapshot = snapshotResult.Snapshot;
        return await ExportFlashbackCoreAsync(
                TimeSpan.Zero,
                TimeSpan.MaxValue,
                outputPath,
                progress,
                ct,
                snapshotSink: snapshot.Sink,
                snapshotBufferManager: snapshot.BufferManager,
                snapshotExporter: snapshot.Exporter,
                exportOperationLockAlreadyHeld: true,
                force: force,
                resolveRangeAfterEvictionPaused: CreateFlashbackExportRangeResolver(
                    inPoint,
                    outPoint,
                    inPointFilePts,
                    outPointFilePts))
            .ConfigureAwait(false);
    }

    internal async Task<FinalizeResult> ExportFlashbackLastNSecondsAsync(
        double seconds, string outputPath,
        IProgress<ExportProgress>? progress, CancellationToken ct,
        bool force = false)
    {
        if (ct.IsCancellationRequested)
        {
            return FailFlashbackExport(outputPath, "Flashback export cancelled.");
        }

        if (!double.IsFinite(seconds) || seconds <= 0 || seconds > TimeSpan.MaxValue.TotalSeconds)
        {
            return FailFlashbackExport(outputPath, "Flashback export duration must be finite, greater than zero, and within TimeSpan range.");
        }

        var snapshotResult = await SnapshotFlashbackExportBackendAsync(
                outputPath,
                operationName: "last_n",
                sessionReleaseOperation: "flashback_export_last_n_snapshot_session",
                ct)
            .ConfigureAwait(false);
        if (snapshotResult.Failure != null)
        {
            return snapshotResult.Failure;
        }

        var snapshot = snapshotResult.Snapshot;
        return await ExportFlashbackCoreAsync(
                TimeSpan.Zero,
                TimeSpan.MaxValue,
                outputPath,
                progress,
                ct,
                snapshotSink: snapshot.Sink,
                snapshotBufferManager: snapshot.BufferManager,
                snapshotExporter: snapshot.Exporter,
                exportOperationLockAlreadyHeld: true,
                force: force,
                resolveRangeAfterEvictionPaused: CreateFlashbackExportLastNRangeResolver(seconds))
            .ConfigureAwait(false);
    }

    private readonly record struct FlashbackExportBackendSnapshot(
        FlashbackBufferManager? BufferManager,
        FlashbackEncoderSink? Sink,
        FlashbackExporter? Exporter);

    private readonly record struct FlashbackExportBackendSnapshotResult(
        FlashbackExportBackendSnapshot Snapshot,
        FinalizeResult? Failure);

    private async Task<FlashbackExportBackendSnapshotResult> SnapshotFlashbackExportBackendAsync(
        string outputPath,
        string operationName,
        string sessionReleaseOperation,
        CancellationToken ct)
    {
        // Snapshot buffer state under the session lock, then release it.
        // PauseEviction (inside ExportFlashbackCoreAsync) protects segment files
        // from deletion - the session lock only needs to be held long enough to
        // read consistent references, not for the entire FFmpeg export.
        var sessionLockHeld = false;
        var backendLeaseHeld = false;
        var exportOperationLockHeld = false;
        try
        {
            await _sessionTransitionLock.WaitAsync(ct).ConfigureAwait(false);
            sessionLockHeld = true;

            if (_isRecording && IsFlashbackRecordingBackendActive())
            {
                Logger.Log("FLASHBACK_EXPORT_REJECTED reason=flashback_recording_active");
                return new FlashbackExportBackendSnapshotResult(
                    default,
                    FailFlashbackExport(outputPath, "Flashback export is unavailable while Flashback is the active recording backend."));
            }

            await _flashbackBackendLeaseLock.WaitAsync(ct).ConfigureAwait(false);
            backendLeaseHeld = true;
            var bufferManager = _flashbackBackend.BufferManager;
            var flashbackSink = _flashbackBackend.Sink;
            var flashbackExporter = bufferManager != null
                ? _flashbackBackend.Exporter ??= new FlashbackExporter()
                : _flashbackBackend.Exporter;

            await _flashbackExportOperationLock.WaitAsync(ct).ConfigureAwait(false);
            exportOperationLockHeld = true;

            return new FlashbackExportBackendSnapshotResult(
                new FlashbackExportBackendSnapshot(bufferManager, flashbackSink, flashbackExporter),
                Failure: null);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            ReleaseFlashbackBackendLeaseIfHeld(ref backendLeaseHeld);
            ReleaseFlashbackExportOperationLockIfHeld(ref exportOperationLockHeld);
            return new FlashbackExportBackendSnapshotResult(default, FailFlashbackExport(outputPath, "Flashback export cancelled."));
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_EXPORT_SNAPSHOT_FAIL op={operationName} type={ex.GetType().Name} msg='{ex.Message}'");
            ReleaseFlashbackBackendLeaseIfHeld(ref backendLeaseHeld);
            ReleaseFlashbackExportOperationLockIfHeld(ref exportOperationLockHeld);
            throw;
        }
        finally
        {
            ReleaseFlashbackBackendLeaseIfHeld(ref backendLeaseHeld);
            if (sessionLockHeld)
            {
                ReleaseSemaphoreBestEffort(_sessionTransitionLock, sessionReleaseOperation);
            }
        }
    }
}


using System;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.Services.Flashback;
using Sussudio.Services.Runtime;

namespace Sussudio.Services.Capture;

// Flashback settings mutations, encoder-affecting cycles, and rollback.
public partial class CaptureService
{
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
}

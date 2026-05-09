using System;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.Services.Audio;
using Sussudio.Services.Flashback;
using Sussudio.Services.Preview;
using Sussudio.Services.Recording;
using Sussudio.Services.Runtime;

namespace Sussudio.Services.Capture;

// Flashback backend lifecycle: start, restart, cycle, and dispose the encoder
// sink, buffer manager, and playback controller that implement the DVR preview.
public partial class CaptureService
{
    private async Task RestartFlashbackCoreAsync(CancellationToken cancellationToken)
    {
        await DisposeFlashbackPreviewBackendAsync(cancellationToken, purgeSegments: true).ConfigureAwait(false);

        var committedRestartToken = CancellationToken.None;
        var unifiedVideoCapture = _unifiedVideoCapture;
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

    private async Task EnsureFlashbackAudioInputsAsync(
        CaptureSettings settings,
        CancellationToken cancellationToken,
        string reason)
    {
        var audioDeviceId = settings.AudioEnabled
            ? (settings.UseCustomAudioInput ? settings.AudioDeviceId : (_audioDeviceId ?? _currentDevice?.AudioDeviceId))
            : null;

        if (settings.AudioEnabled && _wasapiAudioCapture == null)
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
                    _wasapiAudioCapture = wasapiCapture;
                    wasapiCapture = null;
                    _avSyncBaselineDriftMs = double.NaN;
                    Volatile.Write(ref _wasapiAudioCaptureFaulted, false);
                    Volatile.Write(ref _wasapiAudioCaptureFaultMessage, null);
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

        AttachFlashbackAudioIfSupported(_wasapiAudioCapture, reason);

        if (_micMonitorEnabled && _microphoneCapture == null && !string.IsNullOrWhiteSpace(_micMonitorDeviceId))
        {
            WasapiAudioCapture? micCapture = new();
            try
            {
                await micCapture.InitializeAsync(_micMonitorDeviceId, cancellationToken).ConfigureAwait(false);
                micCapture.AudioLevelUpdated += OnMicrophoneAudioLevelUpdated;
                micCapture.CaptureFailed += OnWasapiCaptureFailed;
                micCapture.Start();
                _microphoneCapture = micCapture;
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

        if (_microphoneCapture != null && _flashbackSink is { MicrophoneEnabled: true } fbSink)
        {
            _microphoneCapture.SetAudioWriter(samples => fbSink.WriteMicrophoneAudioAsync(samples));
            Logger.Log($"FLASHBACK_MIC_ATTACH_OK reason='{reason}'");
        }
    }

    private async Task EnsureFlashbackPreviewBackendAsync(
        UnifiedVideoCapture unifiedVideoCapture,
        CaptureSettings settings,
        CancellationToken cancellationToken)
    {
        if (!_flashbackEnabled || _flashbackSink != null)
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
                // Assume unavailable — will fall back to HEVC.
            }
        }

        var bufferMinutes = settings.FlashbackBufferMinutes > 0 ? settings.FlashbackBufferMinutes : 5;
        var bufferDuration = TimeSpan.FromMinutes(bufferMinutes);
        // Segment duration must be shorter than buffer duration so completed segments
        // can be evicted. Use half the buffer, clamped to [0.5, 5] minutes.
        // - Lower bound 0.5min: for 1-min buffer, ensures at least 1 completed segment
        //   exists before the buffer fills (2 segments × 0.5min = 1min).
        // - Upper bound 5min: for large buffers (15-30min), keeps eviction granular
        //   so users don't lose 15min of history in one eviction step.
        var segmentDuration = TimeSpan.FromMinutes(Math.Clamp(bufferMinutes / 2.0, 0.5, 5.0));
        var bufferManager = new FlashbackBufferManager(new FlashbackBufferOptions
        {
            BufferDuration = bufferDuration,
            SegmentDuration = segmentDuration
        });
        bufferManager.Initialize(Guid.NewGuid().ToString("N"));
        var flashbackSink = new FlashbackEncoderSink(bufferManager);
        flashbackSink.SetFatalErrorCallback(OnFlashbackBackendFatalError);
        var flashbackExporter = new FlashbackExporter();
        FlashbackPlaybackController? playbackController = null;

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
                var audioReady = _wasapiAudioCapture == null || _wasapiAudioCapture.CaptureCallbackCount > 0;
                if (videoReady && audioReady)
                    break;
                await Task.Delay(100, cancellationToken).ConfigureAwait(false);
            }
            Logger.Log(
                $"FLASHBACK_PREVIEW_READINESS video_frames={unifiedVideoCapture.VideoFramesArrived} " +
                $"audio_callbacks={_wasapiAudioCapture?.CaptureCallbackCount ?? -1}");

            await flashbackSink.StartAsync(
                CreateFlashbackSessionContext(unifiedVideoCapture, settings),
                cancellationToken).ConfigureAwait(false);
            flashbackSink.FrameEncoded += OnFlashbackFrameEncoded;
            unifiedVideoCapture.SetFlashbackSink(flashbackSink);
            // Install the backend before AttachFlashbackAudioIfSupported — it reads the active sink.
            _flashbackBackend.Install(
                bufferManager,
                flashbackSink,
                flashbackExporter,
                playbackController: null,
                settingsSnapshot: null);
            AttachFlashbackAudioIfSupported(_wasapiAudioCapture, "preview_backend_start");
            if (_microphoneCapture != null && flashbackSink.MicrophoneEnabled)
            {
                _microphoneCapture.SetAudioWriter(samples => flashbackSink.WriteMicrophoneAudioAsync(samples));
                Logger.Log("FLASHBACK_MIC_ATTACH_OK reason='preview_backend_start'");
            }

            // Create playback controller for timeline scrubbing/playback
            playbackController = new FlashbackPlaybackController(bufferManager);
            playbackController.GpuDecodeEnabled = settings.FlashbackGpuDecode;
            if (_previewFrameSink != null && unifiedVideoCapture != null)
            {
                playbackController.Initialize(_previewFrameSink, unifiedVideoCapture, _wasapiAudioPlayback, _wasapiAudioCapture);
            }
            _flashbackPlaybackController = playbackController;
            _flashbackBackendSettings = CloneCaptureSettings(settings);
            _flashbackBackend.ClearRecoveryPreserve();
            ClearLastFlashbackFailure();

            Logger.Log($"FLASHBACK_PREVIEW_INIT_OK session='{bufferManager.SessionId}' controller_initialized={playbackController.IsInitialized}");
        }
        catch (Exception ex)
        {
            var failureToken = ex is OperationCanceledException && cancellationToken.IsCancellationRequested
                ? "FLASHBACK_PREVIEW_INIT_CANCELLED"
                : "FLASHBACK_PREVIEW_INIT_FAIL";
            Logger.Log($"{failureToken} type={ex.GetType().Name} error='{ex.Message}'");
            flashbackSink.FrameEncoded -= OnFlashbackFrameEncoded;
            try { unifiedVideoCapture.SetFlashbackSink(null); }
            catch (Exception detachEx) { Logger.Log($"FLASHBACK_PREVIEW_ROLLBACK_DETACH_WARN target=video type={detachEx.GetType().Name} msg={detachEx.Message}"); }
            try { _wasapiAudioCapture?.DetachFlashbackSink(); }
            catch (Exception detachEx) { Logger.Log($"FLASHBACK_PREVIEW_ROLLBACK_DETACH_WARN target=audio type={detachEx.GetType().Name} msg={detachEx.Message}"); }
            try { _microphoneCapture?.SetAudioWriter(null); }
            catch (Exception detachEx) { Logger.Log($"FLASHBACK_PREVIEW_ROLLBACK_DETACH_WARN target=microphone type={detachEx.GetType().Name} msg={detachEx.Message}"); }
            try { (playbackController ?? _flashbackPlaybackController)?.Dispose(); }
            catch (Exception disposeEx) { Logger.Log($"FLASHBACK_PREVIEW_ROLLBACK_PLAYBACK_WARN type={disposeEx.GetType().Name} msg={disposeEx.Message}"); }
            try { await flashbackSink.DisposeAsync().ConfigureAwait(false); }
            catch (Exception disposeEx) { Logger.Log($"FLASHBACK_PREVIEW_ROLLBACK_SINK_WARN type={disposeEx.GetType().Name} msg={disposeEx.Message}"); }

            var sinkCompletionTask = flashbackSink.EncodingCompletionTask;
            if (!sinkCompletionTask.IsCompleted)
            {
                ScheduleDeferredFlashbackBackendCleanup(
                    sinkCompletionTask,
                    bufferManager,
                    flashbackExporter,
                    reason: "preview_init_rollback",
                    purgeSegments: true);
                bufferManager = null;
                flashbackExporter = null;
            }

            try { flashbackExporter?.Dispose(); }
            catch (Exception disposeEx) { Logger.Log($"FLASHBACK_PREVIEW_ROLLBACK_EXPORTER_WARN type={disposeEx.GetType().Name} msg={disposeEx.Message}"); }

            try { bufferManager?.PurgeAllSegments(); }
            catch (Exception disposeEx) { Logger.Log($"FLASHBACK_PREVIEW_ROLLBACK_PURGE_WARN type={disposeEx.GetType().Name} msg={disposeEx.Message}"); }

            try { bufferManager?.Dispose(); }
            catch (Exception disposeEx) { Logger.Log($"FLASHBACK_PREVIEW_ROLLBACK_BUFFER_WARN type={disposeEx.GetType().Name} msg={disposeEx.Message}"); }

            _flashbackBackend.Clear();

            throw;
        }
    }

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
                    effectivePurgeSegments,
                    detachMicrophoneWriter,
                    exportOperationLockAlreadyHeld: true)
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
        bool purgeSegments = true,
        bool detachMicrophoneWriter = true,
        bool exportOperationLockAlreadyHeld = false)
    {
        var flashbackSink = _flashbackSink;
        var flashbackBufferManager = _flashbackBufferManager;
        var flashbackExporter = _flashbackExporter;
        var flashbackPlaybackController = _flashbackBackend.TakePlaybackController();

        // Do NOT null the sink/buffer/exporter fields yet; the encoding loop may still be running
        // and code that checks _flashbackSink (e.g. IsFlashbackActive) must see
        // a consistent state until the sink is fully drained and stopped.

        if (flashbackPlaybackController != null)
        {
            try
            {
                flashbackPlaybackController.GoLive();
                flashbackPlaybackController.Dispose();
            }
            catch (Exception ex)
            {
                Logger.Log($"FLASHBACK_PLAYBACK_DISPOSE_WARN type={ex.GetType().Name} msg={ex.Message}");
            }
        }

        // Detach feeds first — stops new frames from entering the sink
        _flashbackBackend.DetachProducers(
            _unifiedVideoCapture,
            _wasapiAudioCapture,
            _microphoneCapture,
            "FLASHBACK_PREVIEW_DETACH_WARN",
            detachMicrophoneWriter);

        Task sinkCompletionTask = Task.CompletedTask;
        if (flashbackSink != null)
        {
            flashbackSink.FrameEncoded -= OnFlashbackFrameEncoded;
            try
            {
                // Once feeds are detached, finish the bounded sink drain even if the
                // caller cancels so service fields never point at a half-torn backend.
                await flashbackSink.StopAsync(CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.Log($"FLASHBACK_PREVIEW_STOP_WARN type={ex.GetType().Name} msg={ex.Message}");
            }

            try
            {
                await flashbackSink.DisposeAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.Log($"FLASHBACK_PREVIEW_DISPOSE_WARN type={ex.GetType().Name} msg={ex.Message}");
            }

            sinkCompletionTask = flashbackSink.EncodingCompletionTask;
        }

        // Now that the sink is fully stopped and disposed, clear the fields.
        // Any concurrent reader of _flashbackSink sees either the old (valid)
        // value or null — never a half-disposed object.
        _flashbackBackend.Clear();

        if (!sinkCompletionTask.IsCompleted)
        {
            ScheduleDeferredFlashbackBackendCleanup(
                sinkCompletionTask,
                flashbackBufferManager,
                flashbackExporter,
                reason: purgeSegments ? "preview_backend_dispose_purge" : "preview_backend_dispose",
                purgeSegments: purgeSegments);
            flashbackBufferManager = null;
            flashbackExporter = null;
            cancellationToken.ThrowIfCancellationRequested();
        }

        if (purgeSegments)
        {
            cancellationToken.ThrowIfCancellationRequested();
        }

        var cleanupCompleted = await CleanupFlashbackBackendArtifactsAfterExportAsync(
                flashbackBufferManager,
                flashbackExporter,
                purgeSegments ? "preview_backend_dispose_purge" : "preview_backend_dispose",
                purgeSegments,
                "preview_backend_dispose",
                exportOperationLockAlreadyHeld)
            .ConfigureAwait(false);

        if (!cleanupCompleted)
        {
            ScheduleDeferredFlashbackBackendCleanup(
                Task.Delay(TimeSpan.FromSeconds(1)),
                flashbackBufferManager,
                flashbackExporter,
                reason: purgeSegments ? "preview_backend_dispose_purge_retry" : "preview_backend_dispose_retry",
                purgeSegments: purgeSegments);
        }

        Logger.Log($"FLASHBACK_PREVIEW_DISPOSE_OK purge={purgeSegments}");
    }

    /// <summary>
    /// Cycles the flashback encoder sink after recording stops.
    /// Preserves the buffer manager and its segments so DVR rewind history
    /// survives across recordings. Only the encoder sink is torn down and
    /// replaced; the buffer manager continues accumulating segments.
    /// Falls back to full teardown+rebuild if sink-only cycle fails.
    /// </summary>
    private async Task CycleFlashbackBufferAsync(CancellationToken cancellationToken, bool purgeSegments = false)
    {
        await _flashbackBackendLeaseLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        var exportOperationLockHeld = false;
        try
        {
            await _flashbackExportOperationLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            exportOperationLockHeld = true;

        var unifiedVideoCapture = _unifiedVideoCapture;
        var bufferManager = _flashbackBufferManager;
        var oldSink = _flashbackSink;
        var effectivePurgeSegments = _flashbackBackend.ResolveSegmentPurge(
            purgeSegments,
            "buffer_cycle");

        if (purgeSegments && !effectivePurgeSegments)
        {
            await DisposeFlashbackPreviewBackendCoreAsync(cancellationToken, purgeSegments: false, exportOperationLockAlreadyHeld: true).ConfigureAwait(false);
            if (_flashbackEnabled && unifiedVideoCapture != null && _currentSettings != null)
            {
                await EnsureFlashbackPreviewBackendAsync(unifiedVideoCapture, _currentSettings, cancellationToken).ConfigureAwait(false);
                Logger.Log("FLASHBACK_BUFFER_CYCLE_OK mode=preserve_rebuild new_session=true");
            }
            else
            {
                Logger.Log("FLASHBACK_BUFFER_CYCLE_OK mode=preserve_rebuild new_session=false reason='disabled_or_no_capture'");
            }
            return;
        }

        // If prerequisites are missing, fall back to full teardown
        if (!_flashbackEnabled || unifiedVideoCapture == null || _currentSettings == null || bufferManager == null || oldSink == null)
        {
            await DisposeFlashbackPreviewBackendCoreAsync(cancellationToken, purgeSegments: effectivePurgeSegments, exportOperationLockAlreadyHeld: true).ConfigureAwait(false);
            if (_flashbackEnabled && unifiedVideoCapture != null && _currentSettings != null)
            {
                await EnsureFlashbackPreviewBackendAsync(unifiedVideoCapture, _currentSettings, cancellationToken).ConfigureAwait(false);
                Logger.Log("FLASHBACK_BUFFER_CYCLE_OK mode=full_teardown new_session=true");
            }
            else
            {
                Logger.Log("FLASHBACK_BUFFER_CYCLE_OK mode=full_teardown new_session=false reason='disabled_or_no_capture'");
            }
            return;
        }

        // Close playback before cycling the sink so active decoders release segment files.
        var oldPlaybackController = _flashbackBackend.TakePlaybackController();
        var preservedInPoint = !effectivePurgeSegments ? oldPlaybackController?.InPoint : null;
        var preservedOutPoint = !effectivePurgeSegments ? oldPlaybackController?.OutPoint : null;
        var preservedInPointFilePts = !effectivePurgeSegments ? oldPlaybackController?.InPointFilePts : null;
        var preservedOutPointFilePts = !effectivePurgeSegments ? oldPlaybackController?.OutPointFilePts : null;
        if (oldPlaybackController != null)
        {
            try
            {
                oldPlaybackController.GoLive();
                oldPlaybackController.Dispose();
            }
            catch (Exception ex)
            {
                Logger.Log($"FLASHBACK_PLAYBACK_DISPOSE_WARN type={ex.GetType().Name} msg={ex.Message}");
            }
        }

        _flashbackBackend.DetachProducers(
            unifiedVideoCapture,
            _wasapiAudioCapture,
            _microphoneCapture,
            "FLASHBACK_CYCLE_DETACH_WARN");
        oldSink.FrameEncoded -= OnFlashbackFrameEncoded;
        var committedCycleToken = CancellationToken.None;

        // Stop and dispose the old sink (leaves buffer manager and segments intact)
        try
        {
            await oldSink.StopAsync(committedCycleToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
        {
            Logger.Log($"FLASHBACK_CYCLE_STOP_CANCEL_DEFERRED type={ex.GetType().Name} msg={ex.Message}");
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_CYCLE_STOP_WARN type={ex.GetType().Name} msg={ex.Message}");
        }

        try
        {
            await oldSink.DisposeAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_CYCLE_DISPOSE_WARN type={ex.GetType().Name} msg={ex.Message}");
        }

        // From this point on the old sink is no longer a usable backend. Keep
        // cancellation deferred until a replacement is attached or teardown is complete.
        _flashbackBackend.ClearSinkAndSettings();

        var oldSinkCompletionTask = oldSink.EncodingCompletionTask;
        if (!oldSinkCompletionTask.IsCompleted)
        {
            Logger.Log("FLASHBACK_CYCLE_DISPOSE_DEFERRED - falling back to full teardown");
            var oldExporter = _flashbackExporter;

            _flashbackBackend.Clear();

            ScheduleDeferredFlashbackBackendCleanup(
                oldSinkCompletionTask,
                bufferManager,
                oldExporter,
                reason: "buffer_cycle_deferred_cleanup",
                purgeSegments: effectivePurgeSegments);

            await EnsureFlashbackPreviewBackendAsync(unifiedVideoCapture, _currentSettings, committedCycleToken).ConfigureAwait(false);
            Logger.Log("FLASHBACK_BUFFER_CYCLE_OK mode=deferred_full_rebuild");
            cancellationToken.ThrowIfCancellationRequested();
            return;
        }

        // When the codec/format changed, purge stale segments (incompatible with
        // new encoder) and reset PTS so the new encoder starts fresh from 0.
        // After stop-recording, keep everything — segments, PTS range, and
        // buffer state — so the user can immediately scrub/export DVR history.
        if (effectivePurgeSegments)
        {
            bufferManager.ResetLatestPts();
            bufferManager.PurgeCompletedSegments();

            // If some segments couldn't be deleted (e.g., playback has files locked),
            // fall back to full teardown to avoid mixed-codec segments in the buffer.
            if (bufferManager.SegmentCount > 0)
            {
                Logger.Log($"FLASHBACK_CYCLE_PURGE_INCOMPLETE remaining={bufferManager.SegmentCount} — falling back to full teardown");
                await DisposeFlashbackPreviewBackendCoreAsync(committedCycleToken, purgeSegments: effectivePurgeSegments, exportOperationLockAlreadyHeld: true).ConfigureAwait(false);
                await EnsureFlashbackPreviewBackendAsync(unifiedVideoCapture, _currentSettings, committedCycleToken).ConfigureAwait(false);
                Logger.Log("FLASHBACK_BUFFER_CYCLE_OK mode=purge_fallback_rebuild");
                cancellationToken.ThrowIfCancellationRequested();
                return;
            }
        }

        // Ensure the new sink gets a fresh segment file (not the old sink's active path).
        bufferManager.FinalizeActiveSegmentForCycle();

        // Create and start a new encoder sink on the same buffer manager
        var newSink = new FlashbackEncoderSink(bufferManager);
        newSink.SetFatalErrorCallback(OnFlashbackBackendFatalError);
        try
        {
            // When preserving DVR history (no purge), continue PTS from where
            // the old sink left off so new segments don't overlap existing ones.
            var ptsOffset = effectivePurgeSegments ? TimeSpan.Zero : bufferManager.LatestPts;
            await newSink.StartAsync(
                CreateFlashbackSessionContext(unifiedVideoCapture, _currentSettings),
                committedCycleToken,
                ptsBaseOffset: ptsOffset).ConfigureAwait(false);

            newSink.FrameEncoded += OnFlashbackFrameEncoded;
            _flashbackSink = newSink;
            _flashbackBackendSettings = CloneCaptureSettings(_currentSettings);
            ClearLastFlashbackFailure();

            // Reattach feeds
            unifiedVideoCapture.SetFlashbackSink(newSink);
            AttachFlashbackAudioIfSupported(_wasapiAudioCapture, "buffer_cycle");
            if (_microphoneCapture != null && newSink.MicrophoneEnabled)
            {
                _microphoneCapture.SetAudioWriter(samples => newSink.WriteMicrophoneAudioAsync(samples));
                Logger.Log("FLASHBACK_MIC_ATTACH_OK reason='buffer_cycle'");
            }

            var playbackController = new FlashbackPlaybackController(bufferManager);
            playbackController.GpuDecodeEnabled = _currentSettings.FlashbackGpuDecode;
            playbackController.RestoreInOutPoints(
                preservedInPoint,
                preservedOutPoint,
                preservedInPointFilePts,
                preservedOutPointFilePts);
            if (_previewFrameSink != null)
            {
                playbackController.Initialize(_previewFrameSink, unifiedVideoCapture, _wasapiAudioPlayback, _wasapiAudioCapture);
            }
            _flashbackPlaybackController = playbackController;

            Logger.Log($"FLASHBACK_BUFFER_CYCLE_OK mode=sink_only segments={bufferManager.SegmentCount} buffered={bufferManager.BufferedDuration.TotalSeconds:F1}s");
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_CYCLE_NEW_SINK_FAIL type={ex.GetType().Name} error='{ex.Message}' — falling back to full teardown");
            try { newSink.FrameEncoded -= OnFlashbackFrameEncoded; }
            catch (Exception detachEx) { Logger.Log($"FLASHBACK_CYCLE_NEW_SINK_EVENT_DETACH_WARN type={detachEx.GetType().Name} msg={detachEx.Message}"); }
            try { unifiedVideoCapture.SetFlashbackSink(null); }
            catch (Exception detachEx) { Logger.Log($"FLASHBACK_CYCLE_NEW_SINK_DETACH_WARN type={detachEx.GetType().Name} msg={detachEx.Message}"); }
            try { _wasapiAudioCapture?.DetachFlashbackSink(); }
            catch (Exception detachEx) { Logger.Log($"FLASHBACK_CYCLE_NEW_SINK_AUDIO_DETACH_WARN type={detachEx.GetType().Name} msg={detachEx.Message}"); }
            try { _microphoneCapture?.SetAudioWriter(null); }
            catch (Exception detachEx) { Logger.Log($"FLASHBACK_CYCLE_NEW_SINK_MIC_DETACH_WARN type={detachEx.GetType().Name} msg={detachEx.Message}"); }
            try { await newSink.DisposeAsync().ConfigureAwait(false); }
            catch (Exception disposeEx) { Logger.Log($"FLASHBACK_CYCLE_NEW_SINK_DISPOSE_WARN type={disposeEx.GetType().Name} msg={disposeEx.Message}"); }
            _flashbackBackend.ClearSinkAndSettings();

            // Full teardown and rebuild
            await DisposeFlashbackPreviewBackendCoreAsync(committedCycleToken, purgeSegments: effectivePurgeSegments, exportOperationLockAlreadyHeld: true).ConfigureAwait(false);
            await EnsureFlashbackPreviewBackendAsync(unifiedVideoCapture, _currentSettings, committedCycleToken).ConfigureAwait(false);
            Logger.Log("FLASHBACK_BUFFER_CYCLE_OK mode=fallback_full_rebuild");
        }

        if (cancellationToken.IsCancellationRequested)
        {
            Logger.Log("FLASHBACK_BUFFER_CYCLE_CANCEL_DEFERRED");
            cancellationToken.ThrowIfCancellationRequested();
        }
        }
        finally
        {
            ReleaseFlashbackExportOperationLockIfHeld(ref exportOperationLockHeld);
            ReleaseSemaphoreBestEffort(_flashbackBackendLeaseLock, "flashback_buffer_cycle");
        }
    }
}

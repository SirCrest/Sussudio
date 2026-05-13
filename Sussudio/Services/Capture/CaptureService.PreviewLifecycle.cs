using System;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.Services.Audio;

namespace Sussudio.Services.Capture;

public partial class CaptureService
{
    public Task StartVideoPreviewAsync(CaptureSettings settings, CancellationToken cancellationToken = default)
        => RunTransitionAsync(CaptureSessionState.Previewing, async transitionToken =>
        {
            EnsureInitialized();
            if (_currentDevice == null) throw new InvalidOperationException("No selected video device is available for preview.");
            if (_isVideoPreviewActive) return;
            transitionToken.ThrowIfCancellationRequested();
            var previousSettings = _flashbackBackendSettings ?? _currentSettings;
            var flashbackBackendSettingsChanged = _flashbackSink != null &&
                previousSettings != null &&
                !CanReuseFlashbackBackend(previousSettings, settings);
            _currentSettings = settings;

            // Capture mic monitor settings for preview-time metering
            _micMonitorEnabled = settings.MicrophoneEnabled;
            _micMonitorDeviceId = settings.MicrophoneDeviceId;
            _micMonitorDeviceName = settings.MicrophoneDeviceName;

            if (_unifiedVideoCapture != null &&
                !_isRecording &&
                !CanReuseVideoCaptureForPreview(_unifiedVideoCapture, settings))
            {
                Logger.Log("PREVIEW_START recycle_pipeline=1 reason=settings_changed");
                await DisposePreviewPipelineAsync(transitionToken, purgeFlashbackSegments: true).ConfigureAwait(false);
            }

            if (_unifiedVideoCapture != null &&
                !_isRecording &&
                !_flashbackEnabled)
            {
                Logger.Log("PREVIEW_START recycle_pipeline=1 reason=flashback_disabled");
                await DisposePreviewPipelineAsync(transitionToken, purgeFlashbackSegments: false).ConfigureAwait(false);
            }

            if (_unifiedVideoCapture != null &&
                !_isRecording &&
                _flashbackSink != null &&
                flashbackBackendSettingsChanged)
            {
                Logger.Log("PREVIEW_START recycle_flashback=1 reason=flashback_settings_changed");
                await DisposeFlashbackPreviewBackendAsync(transitionToken, purgeSegments: true).ConfigureAwait(false);
            }

            // Fast-path: the capture pipeline is already running (recording active, or
            // flashback backend kept alive across a prior preview toggle). Just reattach
            // the preview renderer — no device re-init, no flashback restart.
            if (_unifiedVideoCapture != null &&
                (_isRecording || _flashbackEnabled))
            {
                // Guard: if an active flashback sink exists but its pixel format no longer
                // matches the freshly negotiated UVC format, the fast path must not silently
                // reuse the wrong backend. Fail hard so the caller tears down and rebuilds.
                if (_flashbackSink?.IsP010 is bool sinkIsP010 &&
                    sinkIsP010 != _unifiedVideoCapture.IsP010)
                {
                    Logger.Log(
                        $"FLASHBACK_FAST_PATH_FORMAT_MISMATCH " +
                        $"existing_p010={sinkIsP010} requested_p010={_unifiedVideoCapture.IsP010}");
                    throw new InvalidOperationException(
                        $"Flashback fast path: pixel-format mismatch — sink was built for " +
                        $"{(sinkIsP010 ? "P010" : "NV12")} but UVC session negotiated " +
                        $"{(_unifiedVideoCapture.IsP010 ? "P010" : "NV12")}. " +
                        "Rebuild the flashback backend with the correct format.");
                }

                Logger.Log($"PREVIEW_START fast_path=1 recording={_isRecording} flashback_alive={_flashbackSink != null}");
                _unifiedVideoCapture.SetPreviewSink(_previewFrameSink);
                TryApplySharedPreviewDevice(_unifiedVideoCapture, _previewFrameSink);
                if (!_isRecording && _flashbackEnabled && _flashbackSink == null)
                {
                    await EnsureFlashbackPreviewBackendAsync(_unifiedVideoCapture, settings, transitionToken).ConfigureAwait(false);
                }
                await EnsureFlashbackAudioInputsAsync(settings, transitionToken, "preview_fast_path").ConfigureAwait(false);
                _isVideoPreviewActive = true;
                // Telemetry may have been stopped via a recording-stop path while preview
                // was off; StartTelemetryPoll is idempotent (stops any prior timer first).
                StartTelemetryPoll();
                StatusChanged?.Invoke(this, "Preview started");
                return;
            }

            ThrowIfPendingLibAvDrainTaskBlocksReentry();

            var hdrRequested = HdrOutputPolicy.IsEnabled(settings);
            var requireP010 = hdrRequested;
            var useMjpegHighFrameRateMode = settings.UseMjpegHighFrameRateMode;
            var audioDeviceId = settings.AudioEnabled
                ? (settings.UseCustomAudioInput ? settings.AudioDeviceId : (_audioDeviceId ?? _currentDevice.AudioDeviceId))
                : null;

            Logger.Log(
                "HDR_REQUEST_STATE scope=preview " +
                $"hdr_toggle={settings.HdrEnabled} " +
                $"require_p010={requireP010} " +
                $"mjpeg_hfr={useMjpegHighFrameRateMode} " +
                $"mode={settings.Width}x{settings.Height}@{settings.FrameRate:0.###}");

            UnifiedVideoCapture? unifiedVideoCapture = null;
            WasapiAudioCapture? wasapiCapture = null;
            try
            {
                Logger.LogFatalBreadcrumb($"PREVIEW_START phase=create_uvc");
                unifiedVideoCapture = new UnifiedVideoCapture();
                AttachUnifiedVideoCapture(unifiedVideoCapture);
                Logger.LogFatalBreadcrumb($"PREVIEW_START phase=init_uvc {(int)settings.Width}x{(int)settings.Height}@{settings.FrameRate:0.###} p010={requireP010} pxfmt={settings.RequestedPixelFormat} mjpeg_hfr={useMjpegHighFrameRateMode}");
                await unifiedVideoCapture.InitializeAsync(
                    _currentDevice.Id,
                    (int)settings.Width,
                    (int)settings.Height,
                    settings.FrameRate,
                    requireP010,
                    settings.RequestedPixelFormat,
                    useMjpegHighFrameRateMode,
                    settings.MjpegDecoderCount).ConfigureAwait(false);
                Logger.LogFatalBreadcrumb($"PREVIEW_START phase=init_done");
                unifiedVideoCapture.SetPreviewSink(_previewFrameSink);
                TryApplySharedPreviewDevice(unifiedVideoCapture, _previewFrameSink);
                Logger.LogFatalBreadcrumb($"PREVIEW_START phase=starting");
                unifiedVideoCapture.Start();
                Logger.LogFatalBreadcrumb($"PREVIEW_START phase=started");
                // Skip Lock2D by default — preview uses GPU textures via SubmitTexture,
                // never CPU bytes. Lock2D causes GPU pipeline stalls (~5% cadence drops
                // at 120fps, worse at 4K). The existing guards (hasTexture, !frameData.IsEmpty)
                // handle the rare fallback case where GPU texture extraction fails.
                if (unifiedVideoCapture.D3DManager != null)
                {
                    unifiedVideoCapture.SetSkipCpuReadback(true);
                }
                _unifiedVideoCapture = unifiedVideoCapture;
                _lastMfSourceReaderFramesDelivered = 0;
                _lastMfSourceReaderFramesDropped = 0;
                _lastMfSourceReaderNegotiatedFormat = unifiedVideoCapture.NegotiatedFormat;

                _actualWidth = (uint)Math.Max(1, unifiedVideoCapture.Width);
                _actualHeight = (uint)Math.Max(1, unifiedVideoCapture.Height);
                _actualFrameRateNumerator = settings.RequestedFrameRateNumerator;
                _actualFrameRateDenominator = settings.RequestedFrameRateDenominator;
                _actualFrameRate = _actualFrameRateNumerator.HasValue && _actualFrameRateDenominator is > 0
                    ? (double)_actualFrameRateNumerator.Value / _actualFrameRateDenominator.Value
                    : unifiedVideoCapture.Fps > 0 ? unifiedVideoCapture.Fps : settings.FrameRate;
                _actualFrameRateArg = ResolveFrameRateArg(settings, _actualFrameRate ?? settings.FrameRate);
                _actualPixelFormat = unifiedVideoCapture.NativeInputFormat ?? (unifiedVideoCapture.IsP010 ? "P010" : "NV12");
                _activeVideoInputPixelFormat = unifiedVideoCapture.IsP010 ? "p010le" : "nv12";
                TryCorrectFrameRateFromTelemetry();

                if (settings.AudioEnabled && !string.IsNullOrWhiteSpace(audioDeviceId))
                {
                    wasapiCapture = new WasapiAudioCapture();
                    await wasapiCapture.InitializeAsync(audioDeviceId, transitionToken).ConfigureAwait(false);
                    wasapiCapture.AudioLevelUpdated += OnWasapiAudioLevelUpdated;
                    wasapiCapture.CaptureFailed += OnWasapiCaptureFailed;
                    wasapiCapture.Start();
                    _wasapiAudioCapture = wasapiCapture;
                }
                else if (settings.AudioEnabled)
                {
                    Logger.Log("Audio preview requested but no audio capture device is available; continuing with video-only preview.");
                }

                if (_isAudioPreviewActive && _wasapiAudioCapture != null)
                {
                    await StartWasapiPlaybackAsync(transitionToken).ConfigureAwait(false);
                }

                Logger.Log(
                    _wasapiAudioCapture != null
                        ? "Preview backend active: IMFSourceReader video + WASAPI audio ingest."
                        : "Preview backend active: IMFSourceReader video only (no audio capture endpoint).");

                // Start mic monitoring if enabled (metering only, no recording sink)
                if (_micMonitorEnabled && !string.IsNullOrWhiteSpace(_micMonitorDeviceId))
                {
                    WasapiAudioCapture? micCapture = null;
                    try
                    {
                        micCapture = new WasapiAudioCapture();
                        await micCapture.InitializeAsync(_micMonitorDeviceId, transitionToken).ConfigureAwait(false);
                        micCapture.AudioLevelUpdated += OnMicrophoneAudioLevelUpdated;
                        micCapture.CaptureFailed += OnWasapiCaptureFailed;
                        micCapture.Start();
                        if (_flashbackSink is { MicrophoneEnabled: true } fbSink)
                        {
                            micCapture.SetAudioWriter(samples => fbSink.WriteMicrophoneAudioAsync(samples));
                            Logger.Log("FLASHBACK_MIC_ATTACH_OK reason='preview_mic_monitor_start'");
                        }
                        _microphoneCapture = micCapture;
                        micCapture = null;
                        Logger.Log("MIC_MONITOR_START device='" + (_micMonitorDeviceName ?? "?") + "'");
                    }
                    catch (OperationCanceledException) when (transitionToken.IsCancellationRequested)
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
                            catch (Exception disposeEx) { Logger.Log($"MIC_MONITOR_PREVIEW_START_DISPOSE_WARN type={disposeEx.GetType().Name} msg={disposeEx.Message}"); }
                        }
                    }
                }

                // Start flashback AFTER all preview components are running.
                // This eliminates the ~840ms A/V sync drift caused by WASAPI audio
                // flowing before the source reader delivers its first video frame.
                await EnsureFlashbackPreviewBackendAsync(unifiedVideoCapture, settings, transitionToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.Log($"Unified preview start failed: {ex.Message}");
                var previewStartRollbackToken = CancellationToken.None;
                await DisposeFlashbackPreviewBackendAsync(previewStartRollbackToken).ConfigureAwait(false);
                _unifiedVideoCapture = null;
                if (unifiedVideoCapture != null)
                {
                    DetachUnifiedVideoCapture(unifiedVideoCapture);
                    try
                    {
                        await unifiedVideoCapture.DisposeAsync().ConfigureAwait(false);
                    }
                    catch (Exception disposeEx)
                    {
                        Logger.Log($"Unified preview rollback dispose warning: {disposeEx.Message}");
                    }
                }

                if (wasapiCapture != null)
                {
                    wasapiCapture.AudioLevelUpdated -= OnWasapiAudioLevelUpdated;
                    wasapiCapture.CaptureFailed -= OnWasapiCaptureFailed;
                }

                var capture = _wasapiAudioCapture ?? wasapiCapture;
                _wasapiAudioCapture = null;
                if (capture != null)
                {
                    DetachWasapiAudioCapture(capture);
                    try
                    {
                        await capture.DisposeAsync().ConfigureAwait(false);
                    }
                    catch (Exception disposeEx)
                    {
                        Logger.Log($"WASAPI capture rollback dispose warning: {disposeEx.Message}");
                    }
                }

                throw;
            }

            _isVideoPreviewActive = true;
            StartTelemetryPoll();
            StatusChanged?.Invoke(this, "Preview started");
        }, cancellationToken);

    public Task StopVideoPreviewAsync(CancellationToken cancellationToken = default)
        => StopVideoPreviewCoreAsync(teardownPipeline: false, cancellationToken);

    public Task StopVideoPreviewWithTeardownAsync(CancellationToken cancellationToken = default)
        => StopVideoPreviewCoreAsync(teardownPipeline: true, cancellationToken);

    private Task StopVideoPreviewCoreAsync(bool teardownPipeline, CancellationToken cancellationToken = default)
        => RunTransitionAsync(CaptureSessionState.Ready, async transitionToken =>
        {
            if (!_isVideoPreviewActive) return;
            transitionToken.ThrowIfCancellationRequested();

            var commitStoppedState = false;
            Exception? stopFailure = null;
            try
            {
                // Invariant: preview lifecycle must not affect the recording/flashback pipeline.
                // Keep the capture + flashback backend alive across preview toggles unless the
                // caller explicitly requests a full teardown (reinit, shutdown, settings change).
                var keepPipelineAlive = !teardownPipeline &&
                    (_isRecording || (_flashbackEnabled && _flashbackSink != null));

                if (keepPipelineAlive)
                {
                    Logger.Log($"PREVIEW_STOP keep_pipeline_alive=1 recording={_isRecording} flashback_alive={_flashbackSink != null}");
                    _unifiedVideoCapture?.SetPreviewSink(null);
                }
                else
                {
                    await DisposePreviewPipelineAsync(transitionToken, purgeFlashbackSegments: false).ConfigureAwait(false);
                }

                commitStoppedState = true;
            }
            catch (OperationCanceledException) when (transitionToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                stopFailure = ex;
                commitStoppedState = true;
                throw;
            }
            finally
            {
                if (commitStoppedState)
                {
                    _isVideoPreviewActive = false;
                    if (!_isRecording)
                    {
                        try
                        {
                            await StopTelemetryPollAsync().ConfigureAwait(false);
                        }
                        catch (Exception ex) when (stopFailure != null)
                        {
                            Logger.Log($"PREVIEW_STOP_TELEMETRY_WARN type={ex.GetType().Name} msg='{ex.Message}'");
                        }
                    }
                }
            }

            StatusChanged?.Invoke(this, "Preview stopped");
        }, cancellationToken);

    private bool CanReuseVideoCaptureForPreview(UnifiedVideoCapture capture, CaptureSettings settings)
    {
        var hdrRequested = HdrOutputPolicy.IsEnabled(settings);
        return capture.Width == (int)settings.Width &&
               capture.Height == (int)settings.Height &&
               Math.Abs(capture.Fps - settings.FrameRate) < 0.01 &&
               capture.IsP010 == hdrRequested &&
               capture.IsHighFrameRateMjpegMode == settings.UseMjpegHighFrameRateMode;
    }

    private static bool CanReuseFlashbackBackend(CaptureSettings current, CaptureSettings next)
    {
        var currentHdr = HdrOutputPolicy.IsEnabled(current);
        var nextHdr = HdrOutputPolicy.IsEnabled(next);
        if (currentHdr != nextHdr)
        {
            Logger.Log(
                $"FLASHBACK_REUSE_REJECTED reason=hdr_mismatch existing={currentHdr} requested={nextHdr}");
            return false;
        }

        return current.Format == next.Format &&
               current.Quality == next.Quality &&
               Math.Abs(current.CustomBitrateMbps - next.CustomBitrateMbps) < 0.01 &&
               current.NvencPreset == next.NvencPreset &&
               current.SplitEncodeMode == next.SplitEncodeMode &&
               current.AudioEnabled == next.AudioEnabled &&
               current.MicrophoneEnabled == next.MicrophoneEnabled &&
               current.FlashbackBufferMinutes == next.FlashbackBufferMinutes &&
               current.FlashbackGpuDecode == next.FlashbackGpuDecode;
    }

    private static CaptureSettings CloneCaptureSettings(CaptureSettings source)
    {
        return new CaptureSettings
        {
            Width = source.Width,
            Height = source.Height,
            FrameRate = source.FrameRate,
            RequestedFrameRateArg = source.RequestedFrameRateArg,
            RequestedFrameRateNumerator = source.RequestedFrameRateNumerator,
            RequestedFrameRateDenominator = source.RequestedFrameRateDenominator,
            RequestedPixelFormat = source.RequestedPixelFormat,
            Format = source.Format,
            Quality = source.Quality,
            NvencPreset = source.NvencPreset,
            SplitEncodeMode = source.SplitEncodeMode,
            CustomBitrateMbps = source.CustomBitrateMbps,
            HdrEnabled = source.HdrEnabled,
            HdrOutputMode = source.HdrOutputMode,
            HdrNominalPeakNits = source.HdrNominalPeakNits,
            HdrMaxCll = source.HdrMaxCll,
            HdrMaxFall = source.HdrMaxFall,
            HdrMasterDisplayMetadata = source.HdrMasterDisplayMetadata,
            PreviewMode = source.PreviewMode,
            OutputPath = source.OutputPath,
            AudioEnabled = source.AudioEnabled,
            UseCustomAudioInput = source.UseCustomAudioInput,
            AudioDeviceId = source.AudioDeviceId,
            AudioDeviceName = source.AudioDeviceName,
            MicrophoneEnabled = source.MicrophoneEnabled,
            MicrophoneDeviceId = source.MicrophoneDeviceId,
            MicrophoneDeviceName = source.MicrophoneDeviceName,
            AudioPathMode = source.AudioPathMode,
            PipelineOptions = source.PipelineOptions,
            ForceMjpegDecode = source.ForceMjpegDecode,
            FlashbackGpuDecode = source.FlashbackGpuDecode,
            FlashbackBufferMinutes = source.FlashbackBufferMinutes,
            MjpegDecoderCount = source.MjpegDecoderCount
        };
    }

    private async Task DisposePreviewPipelineAsync(
        CancellationToken transitionToken,
        bool purgeFlashbackSegments)
    {
        ClearPendingLibAvDrainTaskIfCompletedSuccessfully();

        var unifiedVideoCapture = _unifiedVideoCapture;
        var videoCaptureCleanupDeferred = false;
        _unifiedVideoCapture = null;
        if (unifiedVideoCapture != null)
        {
            CacheMjpegTimingMetrics(unifiedVideoCapture);
            _lastMfSourceReaderFramesDelivered = unifiedVideoCapture.VideoFramesArrived;
            _lastMfSourceReaderFramesDropped = unifiedVideoCapture.VideoFramesDropped;
            _lastMfSourceReaderNegotiatedFormat = unifiedVideoCapture.NegotiatedFormat;
            DetachUnifiedVideoCapture(unifiedVideoCapture);
            try
            {
                unifiedVideoCapture.SetPreviewSink(null);
                unifiedVideoCapture.SetFlashbackSink(null);
            }
            catch (Exception ex)
            {
                Logger.Log($"PREVIEW_PIPELINE_VIDEO_DETACH_WARN type={ex.GetType().Name} msg='{ex.Message}'");
            }

            if (_pendingLibAvDrainTask is { IsCompleted: false } pendingLibAvDrainTask)
            {
                _pendingLibAvDrainTask = ScheduleDeferredUnifiedVideoCaptureCleanup(
                    pendingLibAvDrainTask,
                    unifiedVideoCapture,
                    reason: "dispose_preview_pipeline_after_deferred_recording");
                videoCaptureCleanupDeferred = true;
            }
            else
            {
                Logger.Log("PREVIEW_PIPELINE_VIDEO_STOP_BEFORE_FLASHBACK_DISPOSE");
                await unifiedVideoCapture.StopAsync().ConfigureAwait(false);
            }
        }

        await DisposeFlashbackPreviewBackendAsync(
                transitionToken,
                purgeSegments: _flashbackBackend.ResolveSegmentPurge(
                    purgeFlashbackSegments,
                    "preview_pipeline_dispose"))
            .ConfigureAwait(false);

        if (unifiedVideoCapture != null && !videoCaptureCleanupDeferred)
        {
            await unifiedVideoCapture.DisposeForPreviewReinitAsync().ConfigureAwait(false);
        }

        var capture = _wasapiAudioCapture;
        _wasapiAudioCapture = null;
        DetachWasapiAudioCapture(capture);
        if (capture != null)
        {
            await capture.DisposeAsync().ConfigureAwait(false);
        }

        await DisposeMicrophoneCaptureAsync().ConfigureAwait(false);
    }
}

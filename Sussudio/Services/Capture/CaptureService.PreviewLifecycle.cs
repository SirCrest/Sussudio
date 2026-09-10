using System;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.Services.Audio;
using Sussudio.Services.Capture.Mjpeg;
using Sussudio.Services.Gpu;
using Sussudio.Services.Preview;

namespace Sussudio.Services.Capture;

// Capture preview lifecycle: video preview start/stop, audio-preview start/stop,
// live input switching, microphone monitoring, and preview pipeline cleanup.
public partial class CaptureService
{
    public Task StartVideoPreviewAsync(CaptureSettings settings, CancellationToken cancellationToken = default)
        => RunTransitionAsync(CaptureSessionState.Previewing, async transitionToken =>
        {
            EnsureInitialized();
            if (_currentDevice == null) throw new InvalidOperationException("No selected video device is available for preview.");
            if (_isVideoPreviewActive) return;
            transitionToken.ThrowIfCancellationRequested();
            var previousSettings = _flashbackBackend.SettingsSnapshot ?? _currentSettings;
            var flashbackBackendSettingsChanged = _flashbackBackend.Sink != null &&
                previousSettings != null &&
                !CanReuseFlashbackBackend(previousSettings, settings);
            _currentSettings = settings;

            // Capture mic monitor settings for preview-time metering
            _micMonitorEnabled = settings.MicrophoneEnabled;
            _micMonitorDeviceId = settings.MicrophoneDeviceId;
            _micMonitorDeviceName = settings.MicrophoneDeviceName;

            await RecyclePreviewPipelineForStartAsync(
                settings,
                flashbackBackendSettingsChanged,
                transitionToken).ConfigureAwait(false);

            if (await TryStartPreviewFromRetainedPipelineAsync(settings, transitionToken).ConfigureAwait(false))
            {
                return;
            }

            _recordingBackend.ThrowIfPendingLibAvDrainBlocksReentry();

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

            await StartFreshPreviewPipelineAsync(
                settings,
                audioDeviceId,
                requireP010,
                useMjpegHighFrameRateMode,
                transitionToken).ConfigureAwait(false);

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
            if (!_isVideoPreviewActive)
            {
                if (teardownPipeline)
                {
                    await DisposePreviewPipelineAsync(transitionToken, purgeFlashbackSegments: false).ConfigureAwait(false);
                }

                return;
            }

            transitionToken.ThrowIfCancellationRequested();

            var commitStoppedState = false;
            Exception? stopFailure = null;
            try
            {
                // Invariant: preview lifecycle must not affect the recording/flashback pipeline.
                // Keep the capture + flashback backend alive across preview toggles unless the
                // caller explicitly requests a full teardown (reinit, shutdown, settings change).
                var keepPipelineAlive = !teardownPipeline &&
                    (_isRecording || (_flashbackEnabled && _flashbackBackend.Sink != null));

                if (keepPipelineAlive)
                {
                    Logger.Log($"PREVIEW_STOP keep_pipeline_alive=1 recording={_isRecording} flashback_alive={_flashbackBackend.Sink != null}");
                    _videoPipeline.Capture?.SetPreviewSink(null);
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
                commitStoppedState = Volatile.Read(ref _cleanupRequired) == 0;
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
                            await StopSourceTelemetryPollingAsync().ConfigureAwait(false);
                        }
                        catch (Exception ex) when (stopFailure != null)
                        {
                            Logger.Log($"PREVIEW_STOP_TELEMETRY_WARN type={ex.GetType().Name} msg='{ex.Message}'");
                        }
                    }
                }
            }

            StatusChanged?.Invoke(this, "Preview stopped");
        }, cancellationToken, cleanupRetainedResources: true);

    private async Task RecyclePreviewPipelineForStartAsync(
        CaptureSettings settings,
        bool flashbackBackendSettingsChanged,
        CancellationToken transitionToken)
    {
        var unifiedVideoCapture = _videoPipeline.Capture;
        if (unifiedVideoCapture != null &&
            !_isRecording &&
            !CanReuseVideoCaptureForPreview(unifiedVideoCapture, settings))
        {
            Logger.Log("PREVIEW_START recycle_pipeline=1 reason=settings_changed");
            await DisposePreviewPipelineAsync(transitionToken, purgeFlashbackSegments: false).ConfigureAwait(false);
        }

        unifiedVideoCapture = _videoPipeline.Capture;
        if (unifiedVideoCapture != null &&
            !_isRecording &&
            !_flashbackEnabled)
        {
            Logger.Log("PREVIEW_START recycle_pipeline=1 reason=flashback_disabled");
            await DisposePreviewPipelineAsync(transitionToken, purgeFlashbackSegments: false).ConfigureAwait(false);
        }

        unifiedVideoCapture = _videoPipeline.Capture;
        if (unifiedVideoCapture != null &&
            !_isRecording &&
            _flashbackBackend.Sink != null &&
            flashbackBackendSettingsChanged)
        {
            Logger.Log("PREVIEW_START recycle_flashback=1 reason=flashback_settings_changed");
            await DisposeFlashbackPreviewBackendAsync(transitionToken, purgeSegments: false).ConfigureAwait(false);
        }
    }

    private async Task<bool> TryStartPreviewFromRetainedPipelineAsync(
        CaptureSettings settings,
        CancellationToken transitionToken)
    {
        var unifiedVideoCapture = _videoPipeline.Capture;
        if (unifiedVideoCapture == null ||
            (!_isRecording && !_flashbackEnabled))
        {
            return false;
        }

        // Fast-path: the capture pipeline is already running (recording active, or
        // flashback backend kept alive across a prior preview toggle). Just reattach
        // the preview renderer: no device re-init, no flashback restart.
        if (_flashbackBackend.Sink?.IsP010 is bool sinkIsP010 &&
            sinkIsP010 != unifiedVideoCapture.IsP010)
        {
            Logger.Log(
                $"FLASHBACK_FAST_PATH_FORMAT_MISMATCH " +
                $"existing_p010={sinkIsP010} requested_p010={unifiedVideoCapture.IsP010}");
            throw new InvalidOperationException(
                $"Flashback fast path: pixel-format mismatch Ã¢â‚¬â€ sink was built for " +
                $"{(sinkIsP010 ? "P010" : "NV12")} but UVC session negotiated " +
                $"{(unifiedVideoCapture.IsP010 ? "P010" : "NV12")}. " +
                "Rebuild the flashback backend with the correct format.");
        }

        Logger.Log($"PREVIEW_START fast_path=1 recording={_isRecording} flashback_alive={_flashbackBackend.Sink != null}");
        unifiedVideoCapture.SetPreviewSink(_videoPipeline.PreviewFrameSink);
        TryApplySharedPreviewDevice(unifiedVideoCapture, _videoPipeline.PreviewFrameSink);
        if (!_isRecording && _flashbackEnabled && _flashbackBackend.Sink == null)
        {
            await EnsureFlashbackPreviewBackendAsync(unifiedVideoCapture, settings, transitionToken).ConfigureAwait(false);
        }
        await EnsureFlashbackAudioInputsAsync(settings, transitionToken, "preview_fast_path").ConfigureAwait(false);
        _isVideoPreviewActive = true;
        // Native source polling may have stopped while this capture kept running.
        // Enabling it again reuses the capture-owned telemetry worker.
        StartTelemetryPoll();
        StatusChanged?.Invoke(this, "Preview started");
        return true;
    }

    private async Task StartFreshPreviewPipelineAsync(
        CaptureSettings settings,
        string? audioDeviceId,
        bool requireP010,
        bool useMjpegHighFrameRateMode,
        CancellationToken transitionToken)
    {
        UnifiedVideoCapture? unifiedVideoCapture = null;
        WasapiAudioCapture? wasapiCapture = null;
        try
        {
            var pendingCaptureCleanup = _videoPipeline.PendingDeferredCaptureCleanupTask;
            if (pendingCaptureCleanup is { IsCompleted: false })
            {
                var sw = System.Diagnostics.Stopwatch.StartNew();
                await pendingCaptureCleanup.WaitAsync(transitionToken).ConfigureAwait(false);
                Logger.Log($"PREVIEW_REOPEN_WAITED_FOR_DEFERRED_CLEANUP ms={sw.ElapsedMilliseconds}");
            }
            else if (_recordingBackend.PendingLibAvDrainTask is { IsCompleted: false } pendingLibAvDrain)
            {
                var sw = System.Diagnostics.Stopwatch.StartNew();
                await pendingLibAvDrain.WaitAsync(transitionToken).ConfigureAwait(false);
                Logger.Log($"PREVIEW_REOPEN_WAITED_FOR_DEFERRED_CLEANUP ms={sw.ElapsedMilliseconds}");
            }

            Logger.LogFatalBreadcrumb($"PREVIEW_START phase=create_uvc");
            unifiedVideoCapture = new UnifiedVideoCapture();
            AttachUnifiedVideoCapture(unifiedVideoCapture);
            Logger.LogFatalBreadcrumb($"PREVIEW_START phase=init_uvc {(int)settings.Width}x{(int)settings.Height}@{settings.FrameRate:0.###} p010={requireP010} pxfmt={settings.RequestedPixelFormat} mjpeg_hfr={useMjpegHighFrameRateMode}");
            await unifiedVideoCapture.InitializeAsync(
                _currentDevice!.Id,
                width: (int)settings.Width,
                height: (int)settings.Height,
                fps: settings.FrameRate,
                requireP010: requireP010,
                requestedPixelFormat: settings.RequestedPixelFormat,
                useMjpegHighFrameRateMode: useMjpegHighFrameRateMode,
                mjpegDecoderCount: settings.MjpegDecoderCount).ConfigureAwait(false);
            Logger.LogFatalBreadcrumb($"PREVIEW_START phase=init_done");
            unifiedVideoCapture.SetPreviewSink(_videoPipeline.PreviewFrameSink);
            TryApplySharedPreviewDevice(unifiedVideoCapture, _videoPipeline.PreviewFrameSink);
            Logger.LogFatalBreadcrumb($"PREVIEW_START phase=starting");
            unifiedVideoCapture.Start();
            Logger.LogFatalBreadcrumb($"PREVIEW_START phase=started");
            // Skip Lock2D by default: preview uses GPU textures via SubmitTexture,
            // never CPU bytes. Lock2D causes GPU pipeline stalls (~5% cadence drops
            // at 120fps, worse at 4K). The existing guards (hasTexture, !frameData.IsEmpty)
            // handle the rare fallback case where GPU texture extraction fails.
            if (unifiedVideoCapture.D3DManager != null)
            {
                unifiedVideoCapture.SetSkipCpuReadback(true);
            }
            _videoPipeline.InstallCapture(unifiedVideoCapture);
            _lastMfSourceReaderFramesDelivered = 0;
            _lastMfSourceReaderFramesDropped = 0;
            _lastMfSourceReaderNegotiatedFormat = unifiedVideoCapture.NegotiatedFormat;

            _actualWidth = (uint)Math.Max(1, unifiedVideoCapture.Width);
            _actualHeight = (uint)Math.Max(1, unifiedVideoCapture.Height);
            SetActualCaptureFrameRate(settings, unifiedVideoCapture.Fps > 0 ? unifiedVideoCapture.Fps : settings.FrameRate);
            _actualPixelFormat = unifiedVideoCapture.NativeInputFormat ?? (unifiedVideoCapture.IsP010 ? "P010" : "NV12");
            _activeVideoInputPixelFormat = unifiedVideoCapture.IsP010 ? "p010le" : "nv12";

            wasapiCapture = await StartPreviewAudioGraphAsync(settings, audioDeviceId, transitionToken).ConfigureAwait(false);

            // Start flashback AFTER all preview components are running.
            // This eliminates the ~840ms A/V sync drift caused by WASAPI audio
            // flowing before the source reader delivers its first video frame.
            await EnsureFlashbackPreviewBackendAsync(unifiedVideoCapture, settings, transitionToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.Log($"Unified preview start failed: {ex.Message}");
            var previewStartRollbackToken = CancellationToken.None;
            await StopPreviewRendererBeforeCaptureCleanupAsync(previewStartRollbackToken).ConfigureAwait(false);
            await DisposeFlashbackPreviewBackendAsync(previewStartRollbackToken).ConfigureAwait(false);
            _videoPipeline.ClearCapture();
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

            await RollbackPreviewAudioCaptureStartupAsync(wasapiCapture).ConfigureAwait(false);

            throw;
        }
    }

    private async Task DisposePreviewPipelineAsync(
        CancellationToken transitionToken,
        bool purgeFlashbackSegments)
    {
        Volatile.Write(ref _cleanupRequired, 1);
        await StopPreviewRendererBeforeCaptureCleanupAsync(transitionToken).ConfigureAwait(false);
        _recordingBackend.ClearPendingLibAvDrainIfCompletedSuccessfully();

        var unifiedVideoCapture = _videoPipeline.Capture;
        var videoCaptureCleanupDeferred = false;
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

            if (_recordingBackend.PendingLibAvDrainTask is { IsCompleted: false } pendingLibAvDrainTask)
            {
                var captureCleanupTask = _videoPipeline.ScheduleDeferredUnifiedVideoCaptureCleanup(
                    pendingLibAvDrainTask,
                    unifiedVideoCapture,
                    reason: "dispose_preview_pipeline_after_deferred_recording");
                SetPendingLibAvCleanupTask(
                    Task.WhenAll(pendingLibAvDrainTask, captureCleanupTask),
                    "LibAv+PreviewPipeline");
                _videoPipeline.ClearCapture();
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
            _videoPipeline.ClearCapture();
        }

        var capture = _previewAudioGraph.ProgramCapture;
        _previewAudioGraph.ProgramCapture = null;
        _previewAudioGraph.DetachCapture(
            capture,
            OnWasapiAudioLevelUpdated,
            _flashbackBackend.PlaybackController);
        if (capture != null)
        {
            await capture.DisposeAsync().ConfigureAwait(false);
        }

        await DisposeMicrophoneCaptureAsync().ConfigureAwait(false);
        Volatile.Write(ref _cleanupRequired, 0);
    }

    public int GetNegotiatedVideoWidth() => _videoPipeline.NegotiatedVideoWidth;
    public int GetNegotiatedVideoHeight() => _videoPipeline.NegotiatedVideoHeight;
    public double GetNegotiatedVideoFps() => _videoPipeline.NegotiatedVideoFps;

    internal void SetPreviewFrameSink(IPreviewFrameSink? sink)
    {
        if (sink != null)
        {
            ThrowIfDisposed();
            if (Volatile.Read(ref _cleanupRequired) != 0)
            {
                throw new InvalidOperationException("Capture cleanup must complete before attaching a preview renderer.");
            }
        }

        var controller = _flashbackBackend.PlaybackController;
        if (sink == null && controller is { IsDisposed: false, IsInitialized: true })
        {
            controller.PrepareForPreviewDetach();
        }

        _videoPipeline.SetPreviewFrameSink(sink);
        var unifiedVideoCapture = _videoPipeline.Capture;
        TryApplySharedPreviewDevice(unifiedVideoCapture, sink);
        // Late-initialize playback controller if it was created before the renderer
        if (controller is { IsDisposed: false, IsInitialized: false } && sink != null && unifiedVideoCapture != null)
        {
            controller.Initialize(sink, unifiedVideoCapture, _previewAudioGraph.Playback, _previewAudioGraph.ProgramCapture);
            Logger.Log("FLASHBACK_PLAYBACK_LATE_INIT via SetPreviewFrameSink");
        }
        else if (controller is { IsDisposed: false, IsInitialized: true })
        {
            controller.UpdatePreviewComponents(sink, unifiedVideoCapture);
        }
    }

    private void CacheMjpegTimingMetrics(UnifiedVideoCapture? unifiedVideoCapture)
    {
        _videoPipeline.CacheMjpegTimingMetrics(unifiedVideoCapture);
    }

    private void ResetCachedMjpegTimingMetrics()
    {
        _videoPipeline.ResetCachedMjpegTimingMetrics();
    }

    internal ParallelMjpegDecodePipeline.PipelineTimingMetrics? GetMjpegPipelineTimingDetails()
    {
        return _videoPipeline.GetMjpegPipelineTimingDetails();
    }

    private void AttachUnifiedVideoCapture(UnifiedVideoCapture unifiedVideoCapture)
    {
        unifiedVideoCapture.FatalErrorOccurred += OnUnifiedVideoCaptureFatalError;
    }

    private void DetachUnifiedVideoCapture(UnifiedVideoCapture? unifiedVideoCapture)
    {
        if (unifiedVideoCapture == null)
        {
            return;
        }

        unifiedVideoCapture.FatalErrorOccurred -= OnUnifiedVideoCaptureFatalError;
    }

    private void TryApplySharedPreviewDevice(UnifiedVideoCapture? capture, IPreviewFrameSink? sink)
    {
        if (capture == null || sink is not D3D11PreviewRenderer renderer)
        {
            return;
        }

        renderer.FullRangeInput = capture.IsHighFrameRateMjpegMode;
        var d3dManager = capture.D3DManager;
        if (d3dManager == null)
        {
            return;
        }

        if (!d3dManager.TryCreateDeviceReference(out var sharedDevice, out var reason))
        {
            Logger.Log($"UNIFIED_VIDEO_SHARED_DEVICE_APPLY_SKIP reason={reason}");
            return;
        }

        try
        {
            renderer.SetSharedDevice(sharedDevice);
        }
        catch (Exception ex)
        {
            Logger.Log($"UNIFIED_VIDEO_SHARED_DEVICE_APPLY_WARN type={ex.GetType().Name} hr=0x{ex.HResult:X8} msg={ex.Message}");
        }
        finally
        {
            sharedDevice.Dispose();
        }
    }

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
            ForceMjpegDecode = source.ForceMjpegDecode,
            FlashbackGpuDecode = source.FlashbackGpuDecode,
            FlashbackBufferMinutes = source.FlashbackBufferMinutes,
            MjpegDecoderCount = source.MjpegDecoderCount
        };
    }

    private readonly record struct MicrophoneMonitorRestartOptions(
        bool OnlyWhenMissing,
        string? FlashbackAttachReason,
        string? RestartLogEvent,
        string DisposeWarningEvent);

    public void SetPreviewVolume(float volume)
    {
        _previewAudioGraph.SetPreviewVolume(volume);
    }

    public void SetMonitoringMuted(bool muted)
    {
        _previewAudioGraph.SetMonitoringMuted(muted);
    }

    public Task StartAudioPreviewAsync(CancellationToken cancellationToken = default)
        => RunTransitionAsync(CaptureSessionState.Previewing, async transitionToken =>
        {
            EnsureInitialized();
            transitionToken.ThrowIfCancellationRequested();

            var createdCaptureForAudioPreview = false;
            // Create WASAPI capture if it wasn't started with the preview (audio was disabled at start)
            if (_previewAudioGraph.ProgramCapture == null && _currentDevice != null)
            {
                var audioId = _audioDeviceId ?? _currentDevice.AudioDeviceId;
                if (!string.IsNullOrEmpty(audioId))
                {
                    Logger.Log($"Late-starting WASAPI audio capture for device {audioId}");
                    var wasapiCapture = new WasapiAudioCapture();
                    await wasapiCapture.InitializeAsync(audioId, transitionToken).ConfigureAwait(false);
                    wasapiCapture.AudioLevelUpdated += OnWasapiAudioLevelUpdated;
                    _previewAudioGraph.AttachCaptureFailure(wasapiCapture, "program", OnWasapiCaptureFailed);
                    wasapiCapture.Start();
                    _previewAudioGraph.ProgramCapture = wasapiCapture;
                    createdCaptureForAudioPreview = true;
                    ResetAvSyncDriftBaseline();
                    _previewAudioGraph.ResetCaptureFault();
                }
                else
                {
                    Logger.Log("Audio preview requested but no audio capture device is available.");
                }
            }

            if (_previewAudioGraph.ProgramCapture == null)
            {
                _isAudioPreviewActive = false;
                StatusChanged?.Invoke(this, "Audio preview unavailable");
                return;
            }

            _isAudioPreviewActive = true;
            try
            {
                AttachFlashbackAudioIfSupported(_previewAudioGraph.ProgramCapture, "audio_preview_start");
                await _previewAudioGraph.StartPlaybackAsync(
                    transitionToken,
                    _flashbackBackend.PlaybackController).ConfigureAwait(false);
            }
            catch
            {
                _isAudioPreviewActive = false;
                if (createdCaptureForAudioPreview)
                {
                    var capture = _previewAudioGraph.ProgramCapture;
                    _previewAudioGraph.ProgramCapture = null;
                    _previewAudioGraph.DetachCapture(
                        capture,
                        OnWasapiAudioLevelUpdated,
                        _flashbackBackend.PlaybackController);
                    if (capture != null)
                    {
                        try
                        {
                            await capture.DisposeAsync().ConfigureAwait(false);
                        }
                        catch (Exception disposeEx)
                        {
                            Logger.Log($"AUDIO_PREVIEW_START_ROLLBACK_DISPOSE_WARN type={disposeEx.GetType().Name} msg={disposeEx.Message}");
                        }
                    }
                }

                throw;
            }

            StatusChanged?.Invoke(this, "Audio preview started");
        }, cancellationToken);

    private async Task<WasapiAudioCapture?> StartPreviewAudioGraphAsync(
        CaptureSettings settings,
        string? audioDeviceId,
        CancellationToken transitionToken)
    {
        WasapiAudioCapture? wasapiCapture = null;
        try
        {
            if (settings.AudioEnabled && !string.IsNullOrWhiteSpace(audioDeviceId))
            {
                try
                {
                    wasapiCapture = new WasapiAudioCapture();
                    await wasapiCapture.InitializeAsync(audioDeviceId, transitionToken).ConfigureAwait(false);
                    wasapiCapture.AudioLevelUpdated += OnWasapiAudioLevelUpdated;
                    _previewAudioGraph.AttachCaptureFailure(wasapiCapture, "program", OnWasapiCaptureFailed);
                    wasapiCapture.Start();
                    _previewAudioGraph.ProgramCapture = wasapiCapture;
                }
                catch (OperationCanceledException) when (transitionToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception audioEx)
                {
                    Logger.Log(
                        $"WASAPI_CAPTURE_START_FAIL_CONTINUE_VIDEO type={audioEx.GetType().Name} msg='{audioEx.Message}'");
                    StatusChanged?.Invoke(this, "Audio unavailable; video preview is still running");
                    if (wasapiCapture != null)
                    {
                        wasapiCapture.AudioLevelUpdated -= OnWasapiAudioLevelUpdated;
                        _previewAudioGraph.DetachCaptureFailure(wasapiCapture);
                        try
                        {
                            await wasapiCapture.DisposeAsync().ConfigureAwait(false);
                        }
                        catch (Exception disposeEx)
                        {
                            Logger.Log(
                                $"WASAPI_CAPTURE_START_FAIL_DISPOSE_WARN type={disposeEx.GetType().Name} msg='{disposeEx.Message}'");
                        }
                        wasapiCapture = null;
                    }
                }
            }
            else if (settings.AudioEnabled)
            {
                Logger.Log("Audio preview requested but no audio capture device is available; continuing with video-only preview.");
            }

            if (_isAudioPreviewActive && _previewAudioGraph.ProgramCapture != null)
            {
                try
                {
                    await _previewAudioGraph.StartPlaybackAsync(
                        transitionToken,
                        _flashbackBackend.PlaybackController).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (transitionToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception playbackEx)
                {
                    _isAudioPreviewActive = false;
                    Logger.Log(
                        $"WASAPI_PLAYBACK_UNAVAILABLE_CONTINUE_VIDEO type={playbackEx.GetType().Name} msg='{playbackEx.Message}'");
                    StatusChanged?.Invoke(this, "Audio monitoring unavailable; video preview is still running");
                }
            }

            Logger.Log(
                _previewAudioGraph.ProgramCapture != null
                    ? "Preview backend active: IMFSourceReader video + WASAPI audio ingest."
                    : "Preview backend active: IMFSourceReader video only (no audio capture endpoint).");

            await StartPreviewMicrophoneMonitorAsync(transitionToken).ConfigureAwait(false);

            return wasapiCapture;
        }
        catch
        {
            await RollbackPreviewAudioCaptureStartupAsync(wasapiCapture).ConfigureAwait(false);
            throw;
        }
    }

    private async Task StartPreviewMicrophoneMonitorAsync(CancellationToken transitionToken)
    {
        // Start mic monitoring if enabled (metering only, no recording sink)
        if (!_micMonitorEnabled || string.IsNullOrWhiteSpace(_micMonitorDeviceId))
        {
            return;
        }

        WasapiAudioCapture? micCapture = null;
        try
        {
            micCapture = new WasapiAudioCapture();
            await micCapture.InitializeAsync(_micMonitorDeviceId, transitionToken).ConfigureAwait(false);
            micCapture.AudioLevelUpdated += OnMicrophoneAudioLevelUpdated;
            _previewAudioGraph.AttachCaptureFailure(micCapture, "microphone", OnWasapiCaptureFailed);
            micCapture.Start();
            if (_flashbackBackend.Sink is { MicrophoneEnabled: true } fbSink)
            {
                micCapture.SetAudioWriter(samples => fbSink.WriteMicrophoneAudioAsync(samples));
                Logger.Log("FLASHBACK_MIC_ATTACH_OK reason='preview_mic_monitor_start'");
            }

            _previewAudioGraph.MicrophoneCapture = micCapture;
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
                _previewAudioGraph.DetachCaptureFailure(micCapture);
                try
                {
                    await micCapture.DisposeAsync().ConfigureAwait(false);
                }
                catch (Exception disposeEx)
                {
                    Logger.Log($"MIC_MONITOR_PREVIEW_START_DISPOSE_WARN type={disposeEx.GetType().Name} msg={disposeEx.Message}");
                }
            }
        }
    }

    private void OnMicrophoneAudioLevelUpdated(object? sender, AudioLevelEventArgs e)
    {
        MicrophoneAudioLevelUpdated?.Invoke(this, e);
    }

    private async Task DisposeMicrophoneCaptureAsync()
    {
        var mic = _previewAudioGraph.MicrophoneCapture;
        _previewAudioGraph.MicrophoneCapture = null;
        if (mic != null)
        {
            try
            {
                try
                {
                    mic.SetAudioWriter(null);
                }
                catch (Exception detachEx)
                {
                    Logger.Log($"MIC_MONITOR_WRITER_DETACH_WARN type={detachEx.GetType().Name} msg={detachEx.Message}");
                }

                mic.AudioLevelUpdated -= OnMicrophoneAudioLevelUpdated;
                _previewAudioGraph.DetachCaptureFailure(mic);
                await mic.DisposeAsync().ConfigureAwait(false);
                Logger.Log("MIC_MONITOR_STOP");
            }
            catch (Exception ex)
            {
                Logger.Log("Microphone capture dispose failed: " + ex.Message);
            }
        }
    }

    public Task UpdateMicrophoneMonitorAsync(bool enabled, string? deviceId, string? deviceName, CancellationToken cancellationToken = default)
        => RunTransitionAsync(CurrentSessionState, async transitionToken =>
        {
            var previousEnabled = _micMonitorEnabled;
            var previousDeviceId = _micMonitorDeviceId;
            var previousDeviceName = _micMonitorDeviceName;
            WasapiAudioCapture? nextMicCapture = null;
            try
            {
                transitionToken.ThrowIfCancellationRequested();
                if (_isRecording)
                {
                    _micMonitorEnabled = enabled;
                    _micMonitorDeviceId = deviceId;
                    _micMonitorDeviceName = deviceName;
                    Logger.Log("MIC_MONITOR_UPDATE_DEFERRED recording=true");
                    return;
                }

                if (enabled && !_isRecording && _isVideoPreviewActive && !string.IsNullOrWhiteSpace(deviceId))
                {
                    nextMicCapture = new WasapiAudioCapture();
                    await nextMicCapture.InitializeAsync(deviceId, transitionToken).ConfigureAwait(false);
                    nextMicCapture.AudioLevelUpdated += OnMicrophoneAudioLevelUpdated;
                    _previewAudioGraph.AttachCaptureFailure(nextMicCapture, "microphone", OnWasapiCaptureFailed);
                }

                await DisposeMicrophoneCaptureAsync().ConfigureAwait(false);

                if (nextMicCapture != null)
                {
                    if (_flashbackBackend.Sink is { MicrophoneEnabled: true } fbSink)
                    {
                        nextMicCapture.SetAudioWriter(samples => fbSink.WriteMicrophoneAudioAsync(samples));
                        Logger.Log("FLASHBACK_MIC_ATTACH_OK reason='mic_monitor_update'");
                    }

                    // Start only after the previous worker has reached terminal cleanup. Start()
                    // rechecks quarantine in case that cleanup timed out.
                    nextMicCapture.Start();
                }

                _micMonitorEnabled = enabled;
                _micMonitorDeviceId = deviceId;
                _micMonitorDeviceName = deviceName;
                _previewAudioGraph.MicrophoneCapture = nextMicCapture;
                nextMicCapture = null;

                if (_previewAudioGraph.MicrophoneCapture != null)
                {
                    Logger.Log("MIC_MONITOR_START device='" + (deviceName ?? "?") + "'");
                }
                else
                {
                    MicrophoneAudioLevelUpdated?.Invoke(this, new AudioLevelEventArgs(0, 0, false));
                }
            }
            catch
            {
                _micMonitorEnabled = previousEnabled;
                _micMonitorDeviceId = previousDeviceId;
                _micMonitorDeviceName = previousDeviceName;
                if (nextMicCapture != null)
                {
                    try
                    {
                        nextMicCapture.SetAudioWriter(null);
                        nextMicCapture.AudioLevelUpdated -= OnMicrophoneAudioLevelUpdated;
                        _previewAudioGraph.DetachCaptureFailure(nextMicCapture);
                        await nextMicCapture.DisposeAsync().ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        Logger.Log("Microphone capture rollback dispose failed: " + ex.Message);
                    }
                }

                throw;
            }
        }, cancellationToken);

    private async Task RestartMicrophoneMonitorAfterRecordingAsync(
        MicrophoneMonitorRestartOptions options,
        CancellationToken cancellationToken)
    {
        if (!_isVideoPreviewActive || !_micMonitorEnabled || string.IsNullOrWhiteSpace(_micMonitorDeviceId))
        {
            return;
        }

        if (options.OnlyWhenMissing && _previewAudioGraph.MicrophoneCapture != null)
        {
            return;
        }

        WasapiAudioCapture? micCapture = null;
        try
        {
            micCapture = new WasapiAudioCapture();
            await micCapture.InitializeAsync(_micMonitorDeviceId, cancellationToken).ConfigureAwait(false);
            micCapture.AudioLevelUpdated += OnMicrophoneAudioLevelUpdated;
            _previewAudioGraph.AttachCaptureFailure(micCapture, "microphone", OnWasapiCaptureFailed);
            micCapture.Start();
            if (_flashbackBackend.Sink is { MicrophoneEnabled: true } fbSink)
            {
                micCapture.SetAudioWriter(samples => fbSink.WriteMicrophoneAudioAsync(samples));
                if (!string.IsNullOrWhiteSpace(options.FlashbackAttachReason))
                {
                    Logger.Log($"FLASHBACK_MIC_ATTACH_OK reason='{options.FlashbackAttachReason}'");
                }
            }

            _previewAudioGraph.MicrophoneCapture = micCapture;
            micCapture = null;
            if (!string.IsNullOrWhiteSpace(options.RestartLogEvent))
            {
                Logger.Log($"{options.RestartLogEvent} device='" + (_micMonitorDeviceName ?? "?") + "'");
            }
        }
        finally
        {
            if (micCapture != null)
            {
                micCapture.AudioLevelUpdated -= OnMicrophoneAudioLevelUpdated;
                _previewAudioGraph.DetachCaptureFailure(micCapture);
                try { await micCapture.DisposeAsync().ConfigureAwait(false); }
                catch (Exception disposeEx) { Logger.Log($"{options.DisposeWarningEvent} type={disposeEx.GetType().Name} msg={disposeEx.Message}"); }
            }
        }
    }

    private async Task RollbackPreviewAudioCaptureStartupAsync(WasapiAudioCapture? wasapiCapture)
    {
        if (wasapiCapture != null)
        {
            wasapiCapture.AudioLevelUpdated -= OnWasapiAudioLevelUpdated;
            _previewAudioGraph.DetachCaptureFailure(wasapiCapture);
        }

        var capture = _previewAudioGraph.ProgramCapture ?? wasapiCapture;
        _previewAudioGraph.ProgramCapture = null;
        if (capture != null)
        {
            _previewAudioGraph.DetachCapture(
                capture,
                OnWasapiAudioLevelUpdated,
                _flashbackBackend.PlaybackController);
            try
            {
                await capture.DisposeAsync().ConfigureAwait(false);
            }
            catch (Exception disposeEx)
            {
                Logger.Log($"WASAPI capture rollback dispose warning: {disposeEx.Message}");
            }
        }
    }

    public Task StopAudioPreviewAsync(CancellationToken cancellationToken = default)
        => StopAudioPreviewCoreAsync(teardownCapture: false, cancellationToken);

    public Task StopAudioPreviewWithTeardownAsync(CancellationToken cancellationToken = default)
        => StopAudioPreviewCoreAsync(teardownCapture: true, cancellationToken);

    private Task StopAudioPreviewCoreAsync(bool teardownCapture, CancellationToken cancellationToken = default)
        => RunTransitionAsync(CaptureSessionState.Ready, async transitionToken =>
        {
            transitionToken.ThrowIfCancellationRequested();
            _isAudioPreviewActive = false;
            _previewAudioGraph.StopPlayback(_flashbackBackend.PlaybackController);

            if (teardownCapture && !_isRecording)
            {
                var capture = _previewAudioGraph.ProgramCapture;
                _previewAudioGraph.ProgramCapture = null;
                _previewAudioGraph.DetachCapture(
                    capture,
                    OnWasapiAudioLevelUpdated,
                    _flashbackBackend.PlaybackController);
                if (capture != null)
                {
                    Logger.Log("Tearing down WASAPI audio capture (audio disabled)");
                    await capture.DisposeAsync().ConfigureAwait(false);
                }
            }

            AudioLevelUpdated?.Invoke(this, new AudioLevelEventArgs(0, 0, false));
            StatusChanged?.Invoke(this, "Audio preview stopped");
        }, cancellationToken);

    public Task UpdateAudioInputAsync(string? audioDeviceId, string? audioDeviceName, CancellationToken cancellationToken = default)
        => RunTransitionAsync(CurrentSessionState, async transitionToken =>
        {
            transitionToken.ThrowIfCancellationRequested();
            var switchGen = Interlocked.Increment(ref _audioSwitchGeneration);
            var previousDeviceId = _audioDeviceId;
            var previousDeviceName = _audioDeviceName;

            if (string.Equals(previousDeviceId, audioDeviceId, StringComparison.OrdinalIgnoreCase))
            {
                _audioDeviceName = audioDeviceName;
                return;
            }

            if (_previewAudioGraph.ProgramCapture == null)
            {
                _audioDeviceId = audioDeviceId;
                _audioDeviceName = audioDeviceName;
                return;
            }

            Logger.Log($"Live audio input switch: {audioDeviceName ?? "(card default)"} gen={switchGen}");

            var activeSink = _isRecording ? _recordingBackend.Sink : null;
            var oldCapture = _previewAudioGraph.ProgramCapture;

            var resolvedId = audioDeviceId ?? _currentDevice?.AudioDeviceId;
            if (!string.IsNullOrEmpty(resolvedId))
            {
                var newCapture = new WasapiAudioCapture();
                try
                {
                    await newCapture.InitializeAsync(resolvedId, transitionToken).ConfigureAwait(false);
                    newCapture.AudioLevelUpdated += OnWasapiAudioLevelUpdated;
                    _previewAudioGraph.AttachCaptureFailure(newCapture, "program", OnWasapiCaptureFailed);
                }
                catch
                {
                    _audioDeviceId = previousDeviceId;
                    _audioDeviceName = previousDeviceName;
                    try
                    {
                        newCapture.AudioLevelUpdated -= OnWasapiAudioLevelUpdated;
                        _previewAudioGraph.DetachCaptureFailure(newCapture);
                        await newCapture.DisposeAsync().ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        Logger.Log($"AUDIO_INPUT_SWITCH_NEW_DISPOSE_WARN type={ex.GetType().Name} msg={ex.Message}");
                    }

                    throw;
                }

                if (switchGen != Volatile.Read(ref _audioSwitchGeneration))
                {
                    Logger.Log($"AUDIO_INPUT_SWITCH_ABORT reason=generation_mismatch gen={switchGen}");
                    try
                    {
                        newCapture.AudioLevelUpdated -= OnWasapiAudioLevelUpdated;
                        _previewAudioGraph.DetachCaptureFailure(newCapture);
                        await newCapture.DisposeAsync().ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        Logger.Log($"AUDIO_INPUT_SWITCH_NEW_DISPOSE_WARN type={ex.GetType().Name} msg={ex.Message}");
                    }

                    return;
                }

                _previewAudioGraph.DetachCapture(
                    oldCapture,
                    OnWasapiAudioLevelUpdated,
                    _flashbackBackend.PlaybackController);

                _previewAudioGraph.ProgramCapture = null;
                try
                {
                    await oldCapture.DisposeAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Logger.Log($"AUDIO_INPUT_SWITCH_OLD_DISPOSE_FAIL type={ex.GetType().Name} msg={ex.Message}");
                    try
                    {
                        newCapture.AudioLevelUpdated -= OnWasapiAudioLevelUpdated;
                        _previewAudioGraph.DetachCaptureFailure(newCapture);
                        await newCapture.DisposeAsync().ConfigureAwait(false);
                    }
                    catch (Exception disposeEx)
                    {
                        Logger.Log($"AUDIO_INPUT_SWITCH_NEW_DISPOSE_WARN type={disposeEx.GetType().Name} msg={disposeEx.Message}");
                    }

                    throw;
                }

                try
                {
                    // Starting after old-worker cleanup prevents two producers from feeding one sink.
                    // Start() rejects a quarantined predecessor that missed its stop deadline.
                    newCapture.Start();
                }
                catch
                {
                    try
                    {
                        newCapture.AudioLevelUpdated -= OnWasapiAudioLevelUpdated;
                        _previewAudioGraph.DetachCaptureFailure(newCapture);
                        await newCapture.DisposeAsync().ConfigureAwait(false);
                    }
                    catch (Exception disposeEx)
                    {
                        Logger.Log($"AUDIO_INPUT_SWITCH_NEW_DISPOSE_WARN type={disposeEx.GetType().Name} msg={disposeEx.Message}");
                    }

                    throw;
                }

                _previewAudioGraph.ProgramCapture = newCapture;
                _audioDeviceId = audioDeviceId;
                _audioDeviceName = audioDeviceName;
                _previewAudioGraph.ResetCaptureFault();
                Logger.Log($"AUDIO_INPUT_SWITCH_COMMITTED gen={switchGen} device={audioDeviceName ?? audioDeviceId}");

                AttachFlashbackAudioIfSupported(newCapture, "audio_input_switch");

                if (activeSink != null && !ReferenceEquals(activeSink, _flashbackBackend.Sink))
                {
                    newCapture.AttachRecordingSink(activeSink);
                }

                if (_isAudioPreviewActive)
                {
                    try
                    {
                        await _previewAudioGraph.StartPlaybackAsync(
                            transitionToken,
                            _flashbackBackend.PlaybackController).ConfigureAwait(false);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        _isAudioPreviewActive = false;
                        Logger.Log($"AUDIO_INPUT_SWITCH_PLAYBACK_START_FAILED gen={switchGen} device={audioDeviceName ?? resolvedId} type={ex.GetType().Name} hr=0x{ex.HResult:X8}");
                        throw;
                    }
                }

                Logger.Log($"Audio input switched to: {audioDeviceName ?? resolvedId}");
            }
            else
            {
                _audioDeviceId = audioDeviceId;
                _audioDeviceName = audioDeviceName;
                _isAudioPreviewActive = false;
                _previewAudioGraph.ProgramCapture = null;
                _previewAudioGraph.DetachCapture(
                    oldCapture,
                    OnWasapiAudioLevelUpdated,
                    _flashbackBackend.PlaybackController);
                try
                {
                    await oldCapture.DisposeAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Logger.Log($"AUDIO_INPUT_SWITCH_OLD_DISPOSE_WARN type={ex.GetType().Name} msg={ex.Message}");
                }

                Logger.Log("Audio input cleared - no device available");
                AudioLevelUpdated?.Invoke(this, new AudioLevelEventArgs(0, 0, false));
            }

            if (transitionToken.IsCancellationRequested)
            {
                Logger.Log("AUDIO_INPUT_SWITCH_CANCEL_DEFERRED");
                transitionToken.ThrowIfCancellationRequested();
            }
        }, cancellationToken);

    private void OnWasapiAudioLevelUpdated(object? sender, AudioLevelEventArgs e)
    {
        AudioLevelUpdated?.Invoke(this, e);
    }

    private void OnWasapiCaptureFailed(Exception ex, CaptureErrorOrigin origin, string source)
    {
        if (!IsCaptureErrorCurrent(origin))
        {
            return;
        }

        var recordingFaultAttributionActive =
            Volatile.Read(ref _recordingFaultAttributionActive) != 0;
        var requestedSettings = _recordingBackend.SettingsSnapshot ?? _currentSettings;
        var microphoneRequested = requestedSettings?.MicrophoneEnabled == true;
        var programAudioRequested = requestedSettings?.AudioEnabled == true;
        var requestedRecordingMicrophoneFailed =
            recordingFaultAttributionActive &&
            string.Equals(source, "microphone", StringComparison.Ordinal) &&
            microphoneRequested;
        var requestedRecordingProgramAudioFailed =
            recordingFaultAttributionActive &&
            string.Equals(source, "program", StringComparison.Ordinal) &&
            programAudioRequested;
        var requestedSourceFailed = string.Equals(source, "microphone", StringComparison.Ordinal)
            ? microphoneRequested
            : string.Equals(source, "program", StringComparison.Ordinal)
                ? programAudioRequested
                : microphoneRequested || programAudioRequested;

        if (requestedSourceFailed &&
            (recordingFaultAttributionActive || Volatile.Read(ref _recordingAudioStartInProgress) != 0))
        {
            _previewAudioGraph.RecordCaptureFault(source, ex);
        }

        Logger.Log($"WASAPI_CAPTURE_FAILED source={source} type={ex.GetType().Name} hr=0x{ex.HResult:X8} message={ex.Message} recording={_isRecording}");
        PublishCaptureError(ex, origin);

        if (requestedRecordingMicrophoneFailed || requestedRecordingProgramAudioFailed)
        {
            var requestedTrack = requestedRecordingMicrophoneFailed ? "microphone" : "device audio";
            var fatalError = new InvalidOperationException(
                $"The requested recording {requestedTrack} became unavailable. Recording has failed and is being finalized.",
                ex);
            RecordLastRecordingFailure(fatalError);
            BeginFatalCaptureCleanup(fatalError);
        }
    }
}

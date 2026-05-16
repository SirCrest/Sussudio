using System;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Sussudio.Models;
using Sussudio.Services.Audio;
using Sussudio.Services.Flashback;
using Sussudio.Services.Gpu;
using Sussudio.Services.Recording;
using Sussudio.Services.Runtime;

namespace Sussudio.Services.Capture;

public partial class CaptureService
{
    public Task StartRecordingAsync(CaptureSettings settings, CancellationToken cancellationToken = default)
        => RunTransitionAsync(CaptureSessionState.Recording, async transitionToken =>
        {
            EnsureInitialized();
            if (_isRecording)
            {
                return;
            }

            if (_currentDevice == null)
            {
                throw new InvalidOperationException("No selected video device is available for recording.");
            }

            transitionToken.ThrowIfCancellationRequested();
            _currentSettings = settings;
            _micMonitorEnabled = settings.MicrophoneEnabled;
            _micMonitorDeviceId = settings.MicrophoneDeviceId;
            _micMonitorDeviceName = settings.MicrophoneDeviceName;

            LibAvRecordingSink? libAvSink = null;
            IRecordingSink? recordingSink = null;
            WasapiAudioCapture? ownedWasapiAudioCapture = null;
            UnifiedVideoCapture? ownedUnifiedVideoCapture = null;
            RecordingContext? recordingContext = null;
            UnifiedVideoCapture? recordingVideoCapture = null;
            FlashbackEncoderSink? flashbackRecordingStartedSink = null;
            var flashbackRecordingBackendLeaseHeld = false;
            var sinkAttachedForAudioOnly = false;
            Volatile.Write(ref _wasapiAudioCaptureFaulted, false);
            Volatile.Write(ref _wasapiAudioCaptureFaultMessage, null);
            ThrowIfPendingLibAvDrainTaskBlocksReentry();
            try
            {
                if (_flashbackEnabled &&
                    _flashbackSink != null &&
                    !_flashbackSink.CanBeginRecording)
                {
                    Logger.Log(
                        "FLASHBACK_RECORDING_BACKEND_UNUSABLE_FALLBACK " +
                        $"failed={_flashbackSink.EncodingFailed} type={_flashbackSink.EncodingFailureType ?? "None"}");
                    await DisposeFlashbackPreviewBackendAsync(transitionToken, purgeSegments: true).ConfigureAwait(false);
                }

                // --- Unified path: piggyback on existing flashback NVENC session ---
                if (_flashbackEnabled && _flashbackSink != null)
                {
                    // Guard: if the existing flashback sink's pixel format no longer matches the
                    // negotiated UVC format, reject the reuse path so the slow path rebuilds correctly.
                    if (_flashbackSink.IsP010 is bool recSinkIsP010 &&
                        _unifiedVideoCapture != null &&
                        recSinkIsP010 != _unifiedVideoCapture.IsP010)
                    {
                        Logger.Log(
                            $"FLASHBACK_FAST_PATH_FORMAT_MISMATCH " +
                            $"existing_p010={recSinkIsP010} requested_p010={_unifiedVideoCapture.IsP010}");
                        throw new InvalidOperationException(
                            $"Flashback recording fast path: pixel-format mismatch — sink was built for " +
                            $"{(recSinkIsP010 ? "P010" : "NV12")} but UVC session negotiated " +
                            $"{(_unifiedVideoCapture.IsP010 ? "P010" : "NV12")}. " +
                            "Rebuild the flashback backend with the correct format.");
                    }

                    StorageFolder fbOutputFolder;
                    try
                    {
                        fbOutputFolder = await StorageFolder.GetFolderFromPathAsync(settings.OutputPath);
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException($"Output folder is unavailable: {settings.OutputPath}", ex);
                    }

                    transitionToken.ThrowIfCancellationRequested();

                    var fbEffectiveFrameRate = _unifiedVideoCapture?.Fps > 0 ? _unifiedVideoCapture.Fps : settings.FrameRate;
                    var fbRecordingContext = await _artifactManager.CreateContextAsync(
                        fbOutputFolder,
                        new RecordingContextRequest
                        {
                            Settings = settings,
                            UsePostMuxAudio = false,
                            AudioDeviceName = settings.AudioEnabled
                                ? (settings.UseCustomAudioInput ? settings.AudioDeviceName : (_audioDeviceName ?? _currentDevice.AudioDeviceName))
                                : null,
                            MicrophoneDeviceName = settings.MicrophoneEnabled ? settings.MicrophoneDeviceName : null,
                            EffectiveFrameRate = fbEffectiveFrameRate,
                            FrameRateArg = ResolveFrameRateArg(settings, fbEffectiveFrameRate),
                            EffectiveWidth = _actualWidth ?? settings.Width,
                            EffectiveHeight = _actualHeight ?? settings.Height,
                            VideoInputPixelFormat = _unifiedVideoCapture?.IsP010 == true ? "p010le" : "nv12",
                            IsFullRangeInput = _unifiedVideoCapture?.IsSoftwareMjpegPipelineActive == true,
                            GpuHandles = GpuPipelineHandles.None
                        }).ConfigureAwait(false);
                    recordingContext = fbRecordingContext;

                    // If flashback settings changed while preview was stopped, rebuild
                    // before recording so the retained backend matches the requested file.
                    var flashbackBackendSettingsChanged = _flashbackBackendSettings == null ||
                        !CanReuseFlashbackBackend(_flashbackBackendSettings, settings);
                    var flashbackAudioTopologyChanged =
                        _flashbackSink.AudioEnabled != settings.AudioEnabled ||
                        _flashbackSink.MicrophoneEnabled != settings.MicrophoneEnabled;
                    if (flashbackAudioTopologyChanged)
                    {
                        Logger.Log($"FLASHBACK_RECORDING_TOPOLOGY_MISMATCH_REJECT " +
                            $"audio={settings.AudioEnabled} (was {_flashbackSink.AudioEnabled}) " +
                            $"mic={settings.MicrophoneEnabled} (was {_flashbackSink.MicrophoneEnabled})");
                        EnsureFlashbackRecordingTopologyMatches(
                            _flashbackSink,
                            settings.AudioEnabled,
                            settings.MicrophoneEnabled);
                    }

                    if (flashbackBackendSettingsChanged)
                    {
                        Logger.Log($"FLASHBACK_SETTINGS_MISMATCH_AUTO_RESTART " +
                            $"settings_changed={flashbackBackendSettingsChanged} " +
                            $"audio={settings.AudioEnabled} " +
                            $"mic={settings.MicrophoneEnabled}");

                        await DisposeFlashbackPreviewBackendAsync(transitionToken, purgeSegments: true).ConfigureAwait(false);

                        var uvc = _unifiedVideoCapture;
                        if (uvc != null)
                        {
                            await EnsureFlashbackPreviewBackendAsync(uvc, settings, transitionToken).ConfigureAwait(false);
                        }

                        if (_flashbackSink == null)
                        {
                            throw new InvalidOperationException("Failed to restart flashback backend for updated recording settings.");
                        }
                    }

                    await EnsureFlashbackAudioInputsAsync(settings, transitionToken, "recording_flashback_start").ConfigureAwait(false);
                    await _flashbackBackendLeaseLock.WaitAsync(transitionToken).ConfigureAwait(false);
                    flashbackRecordingBackendLeaseHeld = true;
                    Volatile.Write(ref _flashbackRecordingStartInProgress, 1);
                    try
                    {
                        var activeFlashbackSink = _flashbackSink
                            ?? throw new InvalidOperationException("Flashback backend is not available for recording.");
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

                        flashbackRecordingStartedSink = activeFlashbackSink;
                        _recordingIntegrityCounterBaseline = CaptureRecordingIntegrityCounters(activeFlashbackSink);
                        _recordingIntegrityAudioBaseline = CaptureRecordingAudioCounters(
                            _wasapiAudioCapture,
                            activeFlashbackSink,
                            settings);
                        activeFlashbackSink.BeginRecording(fbRecordingContext.FinalOutputPath);
                        if (activeFlashbackSink.EncodingFailed)
                        {
                            throw new InvalidOperationException(
                                $"Flashback backend failed while starting recording: {activeFlashbackSink.EncodingFailureMessage ?? "unknown error"}");
                        }

                        _unifiedVideoCapture?.BeginFlashbackRecordingAccounting();
                        _recordingSink = activeFlashbackSink;
                        _libavSink = null;
                        _recordingContext = fbRecordingContext;
                        _activeRecordingSettings = settings;
                        ClearLastRecordingFailure();
                        _isRecording = true;
                        _flashbackRecordingStartBytes = _flashbackBufferManager?.TotalBytesWritten ?? 0;
                        PublishRecordingStartedOutcome(fbRecordingContext.FinalOutputPath);
                        _recordingStopwatch.Restart();
                        StatusChanged?.Invoke(this, "Recording");
                        Logger.Log($"FLASHBACK_UNIFIED_RECORDING_START output='{fbRecordingContext.FinalOutputPath}'");
                        return;
                    }
                    finally
                    {
                        Volatile.Write(ref _flashbackRecordingStartInProgress, 0);
                        if (flashbackRecordingBackendLeaseHeld)
                        {
                            flashbackRecordingBackendLeaseHeld = false;
                            ReleaseSemaphoreBestEffort(_flashbackBackendLeaseLock, "flashback_recording_start");
                        }
                    }
                }

                // --- Standard path: create dedicated LibAvRecordingSink ---
                libAvSink = new LibAvRecordingSink();
                libAvSink.OnEncodingFailed = OnRecordingBackendFatalError;
                libAvSink.FrameEncoded += (s, count) => FrameCaptured?.Invoke(this, unchecked((ulong)Math.Max(0L, count)));
                recordingSink = libAvSink;

                StorageFolder outputFolder;
                try
                {
                    outputFolder = await StorageFolder.GetFolderFromPathAsync(settings.OutputPath);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Output folder is unavailable: {settings.OutputPath}", ex);
                }

                transitionToken.ThrowIfCancellationRequested();

                var effectiveWidth = _actualWidth ?? settings.Width;
                var effectiveHeight = _actualHeight ?? settings.Height;
                var effectiveFrameRate = _actualFrameRate ?? settings.FrameRate;
                await RefreshSourceTelemetryAsync(transitionToken).ConfigureAwait(false);
                TryCorrectFrameRateFromTelemetry();
                var hdrPipelineRequested = HdrOutputPolicy.IsEnabled(settings);
                if (hdrPipelineRequested && _latestSourceTelemetry.IsHdr == false)
                {
                    Logger.Log("HDR requested while source telemetry reports SDR; continuing to request P010 (no silent fallback).");
                }

                var videoInputPixelFormat = hdrPipelineRequested ? "p010le" : "nv12";
                var audioDeviceName = settings.AudioEnabled
                    ? (settings.UseCustomAudioInput ? settings.AudioDeviceName : (_audioDeviceName ?? _currentDevice.AudioDeviceName))
                    : null;
                var audioDeviceId = settings.AudioEnabled
                    ? (settings.UseCustomAudioInput ? settings.AudioDeviceId : (_audioDeviceId ?? _currentDevice.AudioDeviceId))
                    : null;

                var requireP010 = string.Equals(videoInputPixelFormat, "p010le", StringComparison.OrdinalIgnoreCase);
                var useMjpegHighFrameRateMode = settings.UseMjpegHighFrameRateMode;
                var unifiedVideoCapture = _unifiedVideoCapture;
                if (unifiedVideoCapture == null)
                {
                    ownedUnifiedVideoCapture = new UnifiedVideoCapture();
                    AttachUnifiedVideoCapture(ownedUnifiedVideoCapture);
                    await ownedUnifiedVideoCapture.InitializeAsync(
                        _currentDevice.Id,
                        (int)effectiveWidth,
                        (int)effectiveHeight,
                        effectiveFrameRate,
                        requireP010,
                        settings.RequestedPixelFormat,
                        useMjpegHighFrameRateMode,
                        settings.MjpegDecoderCount).ConfigureAwait(false);
                    ownedUnifiedVideoCapture.SetPreviewSink(_isVideoPreviewActive ? _previewFrameSink : null);
                    TryApplySharedPreviewDevice(ownedUnifiedVideoCapture, _isVideoPreviewActive ? _previewFrameSink : null);
                    unifiedVideoCapture = ownedUnifiedVideoCapture;
                    _unifiedVideoCapture = ownedUnifiedVideoCapture;
                }
                else if (unifiedVideoCapture.IsP010 != requireP010)
                {
                    throw new InvalidOperationException(
                        $"Recording requires {(requireP010 ? "P010" : "NV12")}, but the active source-reader session negotiated {(unifiedVideoCapture.IsP010 ? "P010" : "NV12")}.");
                }
                else if (unifiedVideoCapture.IsHighFrameRateMjpegMode != useMjpegHighFrameRateMode)
                {
                    throw new InvalidOperationException(
                        $"Recording requested mjpeg_hfr={useMjpegHighFrameRateMode}, but the active preview session is mjpeg_hfr={unifiedVideoCapture.IsHighFrameRateMjpegMode}.");
                }

                recordingVideoCapture = unifiedVideoCapture;
                TryApplySharedPreviewDevice(unifiedVideoCapture, _isVideoPreviewActive ? _previewFrameSink : null);

                var isMjpegMode = recordingVideoCapture.IsSoftwareMjpegPipelineActive;
                var d3dManager = unifiedVideoCapture.D3DManager;
                var recordingWidth = (uint)Math.Max(1, unifiedVideoCapture.Width);
                var recordingHeight = (uint)Math.Max(1, unifiedVideoCapture.Height);
                var recordingFrameRate = unifiedVideoCapture.Fps > 0 ? unifiedVideoCapture.Fps : effectiveFrameRate;
                var frameRateArg = ResolveFrameRateArg(settings, recordingFrameRate);
                IntPtr cudaHwDeviceCtxPtr = IntPtr.Zero;
                IntPtr cudaHwFramesCtxPtr = IntPtr.Zero;

                recordingContext = await _artifactManager.CreateContextAsync(
                    outputFolder,
                    new RecordingContextRequest
                    {
                        Settings = settings,
                        UsePostMuxAudio = false,
                        AudioDeviceName = audioDeviceName,
                        MicrophoneDeviceName = settings.MicrophoneEnabled ? settings.MicrophoneDeviceName : null,
                        EffectiveFrameRate = recordingFrameRate,
                        FrameRateArg = frameRateArg,
                        EffectiveWidth = recordingWidth,
                        EffectiveHeight = recordingHeight,
                        VideoInputPixelFormat = videoInputPixelFormat,
                        IsFullRangeInput = isMjpegMode,
                        GpuHandles = new GpuPipelineHandles(
                            isMjpegMode ? IntPtr.Zero : (d3dManager?.Device.NativePointer ?? IntPtr.Zero),
                            isMjpegMode ? IntPtr.Zero : (d3dManager?.ImmediateContext.NativePointer ?? IntPtr.Zero),
                            cudaHwDeviceCtxPtr,
                            cudaHwFramesCtxPtr)
                    }).ConfigureAwait(false);

                transitionToken.ThrowIfCancellationRequested();
                _mfConvertersDisabled = requireP010 || isMjpegMode;
                Logger.Log(
                    "HDR_NEGOTIATION " +
                    $"requested_hdr={hdrPipelineRequested} " +
                    $"requested_subtype={(hdrPipelineRequested ? "P010" : "NV12")} " +
                    $"requested_source_subtype={settings.RequestedPixelFormat ?? (hdrPipelineRequested ? "P010" : "NV12")} " +
                    $"mjpeg_hfr={useMjpegHighFrameRateMode} " +
                    $"negotiated_pixel_format={(unifiedVideoCapture.IsP010 ? "P010" : "NV12")} " +
                    $"negotiated_subtype_token={(string.Equals(videoInputPixelFormat, "p010le", StringComparison.OrdinalIgnoreCase) ? "P010|MFVideoFormat_P010" : "NV12")} " +
                    $"hdr_static_metadata_requested={(!string.IsNullOrWhiteSpace(settings.HdrMasterDisplayMetadata) || (settings.HdrMaxCll > 0 && settings.HdrMaxFall > 0))} " +
                    $"hdr_master_display_set={(!string.IsNullOrWhiteSpace(settings.HdrMasterDisplayMetadata))} " +
                    $"hdr_max_cll={settings.HdrMaxCll} " +
                    $"hdr_max_fall={settings.HdrMaxFall} " +
                    $"mf_readwrite_disable_converters={(_mfConvertersDisabled ? "true" : "false")} " +
                    $"libav_ingest_pix_fmt={(string.Equals(videoInputPixelFormat, "p010le", StringComparison.OrdinalIgnoreCase) ? "AV_PIX_FMT_P010LE" : "AV_PIX_FMT_NV12")}");

                await recordingSink.StartAsync(recordingContext, transitionToken).ConfigureAwait(false);
                transitionToken.ThrowIfCancellationRequested();

                _lastMfSourceReaderFramesDelivered = 0;
                _lastMfSourceReaderFramesDropped = 0;
                _lastMfSourceReaderNegotiatedFormat = unifiedVideoCapture.NegotiatedFormat;
                _actualWidth = (uint)Math.Max(1, unifiedVideoCapture.Width);
                _actualHeight = (uint)Math.Max(1, unifiedVideoCapture.Height);
                _actualFrameRateNumerator = settings.RequestedFrameRateNumerator;
                _actualFrameRateDenominator = settings.RequestedFrameRateDenominator;
                _actualFrameRate = _actualFrameRateNumerator.HasValue && _actualFrameRateDenominator is > 0
                    ? (double)_actualFrameRateNumerator.Value / _actualFrameRateDenominator.Value
                    : unifiedVideoCapture.Fps > 0 ? unifiedVideoCapture.Fps : effectiveFrameRate;
                _actualFrameRateArg = ResolveFrameRateArg(settings, _actualFrameRate ?? effectiveFrameRate);
                _actualPixelFormat = unifiedVideoCapture.NativeInputFormat ?? (unifiedVideoCapture.IsP010 ? "P010" : "NV12");
                TryCorrectFrameRateFromTelemetry();

                if (_wasapiAudioCapture == null && settings.AudioEnabled)
                {
                    var resolvedAudioDeviceId = audioDeviceId
                        ?? throw new InvalidOperationException("Recording requires an audio capture device.");
                    ownedWasapiAudioCapture = new WasapiAudioCapture();
                    await ownedWasapiAudioCapture.InitializeAsync(resolvedAudioDeviceId, transitionToken).ConfigureAwait(false);
                    ownedWasapiAudioCapture.AudioLevelUpdated += OnWasapiAudioLevelUpdated;
                    ownedWasapiAudioCapture.CaptureFailed += OnWasapiCaptureFailed;
                    ownedWasapiAudioCapture.Start();
                    _wasapiAudioCapture = ownedWasapiAudioCapture;
                }

                if (_wasapiAudioCapture != null && settings.AudioEnabled)
                {
                    _wasapiAudioCapture.AttachRecordingSink(recordingSink);
                    sinkAttachedForAudioOnly = true;
                    if (_isAudioPreviewActive)
                    {
                        await StartWasapiPlaybackAsync(transitionToken).ConfigureAwait(false);
                    }
                }

                var activeLibAvSink = libAvSink
                    ?? throw new InvalidOperationException("Recording requires an active LibAv sink.");

                // Dispose preview-time mic monitor — recording creates its own with sink
                await DisposeMicrophoneCaptureAsync().ConfigureAwait(false);

                if (settings.MicrophoneEnabled && !string.IsNullOrWhiteSpace(settings.MicrophoneDeviceId))
                {
                    var micSink = activeLibAvSink; // capture stable reference — libAvSink is nulled on success path
                    var micCapture = new WasapiAudioCapture();
                    await micCapture.InitializeAsync(settings.MicrophoneDeviceId, transitionToken).ConfigureAwait(false);
                    micCapture.AudioLevelUpdated += OnMicrophoneAudioLevelUpdated;
                    micCapture.CaptureFailed += OnWasapiCaptureFailed;
                    micCapture.SetAudioWriter(samples => micSink.WriteMicrophoneAudioAsync(samples));
                    micCapture.Start();
                    _microphoneCapture = micCapture;
                    Logger.Log("MICROPHONE_CAPTURE_START device='" + settings.MicrophoneDeviceName + "'");
                }

                IGpuVideoFrameEncoder? gpuEncoder =
                    (!isMjpegMode && activeLibAvSink.GpuEncodingEnabled)
                        ? activeLibAvSink
                        : null;

                _recordingIntegrityCounterBaseline = CaptureRecordingIntegrityCounters(activeLibAvSink);
                _recordingIntegrityAudioBaseline = CaptureRecordingAudioCounters(
                    _wasapiAudioCapture,
                    activeLibAvSink,
                    settings);
                await unifiedVideoCapture.StartRecordingAsync(recordingSink, activeLibAvSink, gpuEncoder).ConfigureAwait(false);
                if (gpuEncoder != null)
                {
                    Logger.Log("GPU_RECORDING_ACTIVE gpu_encoder=active");
                }

                if (ownedUnifiedVideoCapture != null)
                {
                    ownedUnifiedVideoCapture.Start();
                }

                _libavSink = libAvSink;
                _recordingSink = recordingSink;
                _recordingContext = recordingContext;
                _activeRecordingSettings = settings;
                ClearLastRecordingFailure();
                _isRecording = true;
                _activeVideoInputPixelFormat = videoInputPixelFormat;
                Interlocked.Exchange(ref _videoFramesDropped, 0);
                ResetObservedPixelTelemetry();
                RecordObservedPixelFormat(recordingContext.HdrPipelineActive ? "P010" : "NV12", incrementAsFrame: false);
                PublishRecordingStartedOutcome(recordingContext.FinalOutputPath);
                _lastUsePostMuxAudio = recordingContext.UsePostMuxAudio;
                _recordingStopwatch.Restart();
                StatusChanged?.Invoke(this, "Recording");
                libAvSink = null;
                recordingSink = null;
                ownedWasapiAudioCapture = null;
                ownedUnifiedVideoCapture = null;
            }
            catch (Exception ex)
            {
                Logger.Log($"CAPTURE_RECORDING_START_FAIL type={ex.GetType().Name} msg='{ex.Message}'");
                RecordLastRecordingFailure(ex);

                if (flashbackRecordingStartedSink != null)
                {
                    try
                    {
                        flashbackRecordingStartedSink.CancelRecordingStartRollback("start_recording_failed");
                    }
                    catch (Exception rollbackEx)
                    {
                        Logger.Log($"FLASHBACK_RECORDING_START_ROLLBACK_WARN type={rollbackEx.GetType().Name} error='{rollbackEx.Message}'");
                    }

                    _unifiedVideoCapture?.EndFlashbackRecordingAccounting();
                    if (ReferenceEquals(_recordingSink, flashbackRecordingStartedSink))
                    {
                        _recordingSink = null;
                    }
                }

                Volatile.Write(ref _flashbackRecordingStartInProgress, 0);
                if (flashbackRecordingBackendLeaseHeld)
                {
                    flashbackRecordingBackendLeaseHeld = false;
                    ReleaseSemaphoreBestEffort(_flashbackBackendLeaseLock, "flashback_recording_start_fail");
                }

                if (sinkAttachedForAudioOnly && _wasapiAudioCapture != null)
                {
                    _wasapiAudioCapture.DetachRecordingSink();
                }

                await DisposeMicrophoneCaptureAsync().ConfigureAwait(false);

                if (ownedUnifiedVideoCapture != null)
                {
                    DetachUnifiedVideoCapture(ownedUnifiedVideoCapture);
                }

                try
                {
                    await _artifactManager.RollbackAsync(recordingContext).ConfigureAwait(false);
                }
                catch (Exception rollbackEx)
                {
                    Logger.Log($"Recording start rollback cleanup failed: {rollbackEx.Message}");
                }

                try
                {
                    await DisposeTransientRecordingBackendAsync(
                        recordingSink,
                        ownedWasapiAudioCapture,
                        ownedUnifiedVideoCapture).ConfigureAwait(false);
                }
                catch (Exception disposeEx)
                {
                    Logger.Log($"Transient recording backend cleanup failed during start rollback: {disposeEx.Message}");
                }

                if (ownedWasapiAudioCapture != null && ReferenceEquals(_wasapiAudioCapture, ownedWasapiAudioCapture))
                {
                    DetachWasapiAudioCapture(ownedWasapiAudioCapture);
                    _wasapiAudioCapture = null;
                }

                if (ownedUnifiedVideoCapture != null && ReferenceEquals(_unifiedVideoCapture, ownedUnifiedVideoCapture))
                {
                    CacheMjpegTimingMetrics(ownedUnifiedVideoCapture);
                    _lastMfSourceReaderFramesDelivered = ownedUnifiedVideoCapture.VideoFramesArrived;
                    _lastMfSourceReaderFramesDropped = ownedUnifiedVideoCapture.VideoFramesDropped;
                    _lastMfSourceReaderNegotiatedFormat = ownedUnifiedVideoCapture.NegotiatedFormat;
                    _unifiedVideoCapture = null;
                }

                _recordingContext = null;
                _activeRecordingSettings = null;
                _recordingIntegrityCounterBaseline = null;
                _recordingIntegrityAudioBaseline = null;
                _isRecording = false;
                _recordingStopwatch.Reset();
                _mfConvertersDisabled = false;
                throw;
            }
        }, cancellationToken);

}

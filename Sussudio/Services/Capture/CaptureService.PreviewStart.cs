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

                await RollbackPreviewAudioCaptureStartupAsync(wasapiCapture).ConfigureAwait(false);

                throw;
            }

            _isVideoPreviewActive = true;
            StartTelemetryPoll();
            StatusChanged?.Invoke(this, "Preview started");
        }, cancellationToken);

}

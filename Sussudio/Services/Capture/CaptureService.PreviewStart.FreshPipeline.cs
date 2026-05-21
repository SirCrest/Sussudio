using System;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.Services.Audio;

namespace Sussudio.Services.Capture;

public partial class CaptureService
{
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
            Logger.LogFatalBreadcrumb($"PREVIEW_START phase=create_uvc");
            unifiedVideoCapture = new UnifiedVideoCapture();
            AttachUnifiedVideoCapture(unifiedVideoCapture);
            Logger.LogFatalBreadcrumb($"PREVIEW_START phase=init_uvc {(int)settings.Width}x{(int)settings.Height}@{settings.FrameRate:0.###} p010={requireP010} pxfmt={settings.RequestedPixelFormat} mjpeg_hfr={useMjpegHighFrameRateMode}");
            await unifiedVideoCapture.InitializeAsync(
                _currentDevice!.Id,
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
}

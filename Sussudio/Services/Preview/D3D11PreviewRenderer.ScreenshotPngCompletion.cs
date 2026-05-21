using System;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;
using Vortice.DXGI;

namespace Sussudio.Services.Preview;

internal sealed partial class D3D11PreviewRenderer
{
    private void BeginPngFrameCaptureCompletion(
        TaskCompletionSource<PreviewFrameCaptureResult> request,
        byte[] frameBuffer,
        int sourceRowBytes,
        int width,
        int height,
        string outputPath,
        string rendererMode,
        Format backBufferFormat)
    {
        _ = Task.Run(
            () =>
            {
                try
                {
                    var captureResult = PreviewScreenshotCapture.CaptureFrameBufferTo16BitPng(
                        frameBuffer,
                        sourceRowBytes,
                        width,
                        height,
                        outputPath,
                        rendererMode,
                        backBufferFormat);
                    request.TrySetResult(captureResult);
                    Logger.Log(
                        $"PREVIEW_FRAME_CAPTURE_RESULT ok={captureResult.Succeeded} renderer={captureResult.RendererMode} path={captureResult.FilePath ?? "n/a"} width={captureResult.CapturedWidth} height={captureResult.CapturedHeight} avgLum={captureResult.AverageLuminance:0.00} pureBlackPct={captureResult.PureBlackPercent:0.00}");
                }
                catch (Exception ex)
                {
                    request.TrySetResult(CreateFrameCaptureError($"Preview frame capture failed: {ex.Message}", rendererMode));
                    Logger.Log($"PREVIEW_FRAME_CAPTURE_RESULT ok=false renderer={rendererMode} type={ex.GetType().Name} hr=0x{ex.HResult:X8} msg={ex.Message}");
                }
                finally
                {
                    Interlocked.Exchange(ref _frameCaptureEncodeInProgress, 0);
                }
            });
    }
}

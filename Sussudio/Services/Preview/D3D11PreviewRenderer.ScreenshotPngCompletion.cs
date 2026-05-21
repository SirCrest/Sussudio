using System;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;
using Vortice.DXGI;

namespace Sussudio.Services.Preview;

internal sealed partial class D3D11PreviewRenderer
{
    private int _frameCaptureEncodeInProgress;

    private bool IsPngFrameCaptureCompletionInProgress()
        => Volatile.Read(ref _frameCaptureEncodeInProgress) != 0;

    private bool TryBeginPngFrameCaptureCompletion()
        => Interlocked.CompareExchange(ref _frameCaptureEncodeInProgress, 1, 0) == 0;

    private void EndPngFrameCaptureCompletion()
        => Interlocked.Exchange(ref _frameCaptureEncodeInProgress, 0);

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
                    LogFrameCaptureResult(captureResult);
                }
                catch (Exception ex)
                {
                    request.TrySetResult(CreateFrameCaptureError($"Preview frame capture failed: {ex.Message}", rendererMode));
                    LogFrameCaptureFailure(ex, rendererMode);
                }
                finally
                {
                    EndPngFrameCaptureCompletion();
                }
            });
    }
}

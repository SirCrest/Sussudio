using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;
using Vortice.Direct3D11;

namespace Sussudio.Services.Preview;

internal sealed partial class D3D11PreviewRenderer
{
    private const int FrameCaptureTimeoutMs = 5000;

    private TaskCompletionSource<PreviewFrameCaptureResult>? _frameCaptureRequest;
    private int _frameCaptureEncodeInProgress;
    private string? _frameCaptureOutputPath;

    // Persistent staging texture for frame capture (avoids GPU resource churn)
    private ID3D11Texture2D? _captureStagingTexture;
    private int _captureStagingWidth;
    private int _captureStagingHeight;

    public Task<PreviewFrameCaptureResult> CaptureNextFrameAsync(string outputPath)
        => CaptureNextFrameAsync(outputPath, CancellationToken.None);

    public Task<PreviewFrameCaptureResult> CaptureNextFrameAsync(string outputPath, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromResult(CreateFrameCaptureError("Preview frame capture canceled."));
        }

        if (!IsRendering || _device == null || _swapChain == null || Volatile.Read(ref _stopRequested) != 0)
        {
            return Task.FromResult(CreateFrameCaptureError("No active preview renderer."));
        }

        if (Volatile.Read(ref _frameCaptureEncodeInProgress) != 0)
        {
            return Task.FromResult(CreateFrameCaptureError("A preview frame capture is already pending."));
        }

        var resolvedOutputPath = string.IsNullOrWhiteSpace(outputPath)
            ? Path.Combine(Path.GetTempPath(), $"preview_capture_{DateTimeOffset.UtcNow:yyyyMMdd_HHmmss_fff}.bmp")
            : outputPath;

        var request = new TaskCompletionSource<PreviewFrameCaptureResult>(
            state: resolvedOutputPath,
            creationOptions: TaskCreationOptions.RunContinuationsAsynchronously);
        if (Interlocked.CompareExchange(ref _frameCaptureRequest, request, null) != null)
        {
            return Task.FromResult(CreateFrameCaptureError("A preview frame capture is already pending."));
        }

        CancellationTokenRegistration cancellationRegistration = default;
        if (cancellationToken.CanBeCanceled)
        {
            cancellationRegistration = cancellationToken.Register(
                static state =>
                {
                    var (renderer, request) = ((D3D11PreviewRenderer Renderer, TaskCompletionSource<PreviewFrameCaptureResult> Request))state!;
                    var pending = Interlocked.CompareExchange(ref renderer._frameCaptureRequest, null, request);
                    if (!ReferenceEquals(pending, request))
                    {
                        return;
                    }

                    Interlocked.Exchange(ref renderer._frameCaptureOutputPath, null);
                    request.TrySetResult(CreateFrameCaptureError("Preview frame capture canceled."));
                    Logger.Log("PREVIEW_FRAME_CAPTURE_CANCELED");
                },
                (this, request));
            _ = request.Task.ContinueWith(
                _ => cancellationRegistration.Dispose(),
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
        }

        Volatile.Write(ref _frameCaptureOutputPath, resolvedOutputPath);
        _ = Task.Delay(FrameCaptureTimeoutMs).ContinueWith(
            _ =>
            {
                var pending = Interlocked.CompareExchange(ref _frameCaptureRequest, null, request);
                if (!ReferenceEquals(pending, request))
                {
                    return;
                }

                Interlocked.Exchange(ref _frameCaptureOutputPath, null);
                request.TrySetResult(CreateFrameCaptureError("Timed out waiting for the next rendered preview frame."));
                Logger.Log("PREVIEW_FRAME_CAPTURE_TIMEOUT missing=RenderedFrame");
            },
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);

        return request.Task;
    }

    private void TryCaptureFrameBeforePresent(string rendererMode)
    {
        var request = Interlocked.Exchange(ref _frameCaptureRequest, null);
        if (request == null)
        {
            return;
        }

        var requestedOutputPath = request.Task.AsyncState as string;
        Interlocked.Exchange(ref _frameCaptureOutputPath, null);
        var outputPath = string.IsNullOrWhiteSpace(requestedOutputPath)
            ? Path.Combine(Path.GetTempPath(), $"preview_capture_{DateTimeOffset.UtcNow:yyyyMMdd_HHmmss_fff}.bmp")
            : requestedOutputPath;

        try
        {
            if (_device == null || _deviceContext == null || _swapChain == null)
            {
                request.TrySetResult(CreateFrameCaptureError("Renderer device state is unavailable.", rendererMode));
                return;
            }

            var fullOutputPath = Path.GetFullPath(outputPath);
            var isPng = fullOutputPath.EndsWith(".png", StringComparison.OrdinalIgnoreCase);
            if (isPng && Interlocked.CompareExchange(ref _frameCaptureEncodeInProgress, 1, 0) != 0)
            {
                request.TrySetResult(CreateFrameCaptureError("A preview frame capture is already pending.", rendererMode));
                return;
            }

            ID3D11Texture2D? backBuffer = _swapChainBackBuffer;
            var disposeBackBuffer = false;
            if (backBuffer == null)
            {
                backBuffer = _swapChain.GetBuffer<ID3D11Texture2D>(0);
                disposeBackBuffer = true;
            }

            try
            {
                if (backBuffer == null)
                {
                    throw new InvalidOperationException("Swap chain back buffer is unavailable.");
                }

                var backBufferDescription = backBuffer.Description;
                var width = checked((int)backBufferDescription.Width);
                var height = checked((int)backBufferDescription.Height);
                if (width <= 0 || height <= 0)
                {
                    throw new InvalidOperationException("Swap chain back buffer has invalid dimensions.");
                }

                if (_captureStagingTexture == null ||
                    _captureStagingWidth != width ||
                    _captureStagingHeight != height)
                {
                    _captureStagingTexture?.Dispose();
                    _captureStagingTexture = _device.CreateTexture2D(new Texture2DDescription(
                        backBufferDescription.Format,
                        (uint)width,
                        (uint)height,
                        1,
                        1,
                        BindFlags.None,
                        ResourceUsage.Staging,
                        CpuAccessFlags.Read,
                        1,
                        0,
                        ResourceOptionFlags.None));
                    _captureStagingWidth = width;
                    _captureStagingHeight = height;
                }

                var stagingTexture = _captureStagingTexture;
                _deviceContext.CopyResource(stagingTexture, backBuffer);

                _deviceContext.Map(stagingTexture, 0, MapMode.Read, Vortice.Direct3D11.MapFlags.None, out var mapped);
                PreviewFrameCaptureResult captureResult;
                byte[]? pngFrameBuffer = null;
                var pngSourceRowBytes = checked(width * 4);
                try
                {
                    if (isPng)
                    {
                        pngFrameBuffer = CopyMappedFrameToBuffer(mapped, height, pngSourceRowBytes);
                        captureResult = default!;
                    }
                    else
                    {
                        captureResult = CaptureMappedFrameToBmp(
                            mapped,
                            width,
                            height,
                            fullOutputPath,
                            rendererMode,
                            backBufferDescription.Format);
                    }
                }
                finally
                {
                    _deviceContext.Unmap(stagingTexture, 0);
                }

                if (isPng)
                {
                    var pngBuffer = pngFrameBuffer!;
                    _ = Task.Run(
                        () =>
                        {
                            try
                            {
                                var pngCaptureResult = PreviewScreenshotCapture.CaptureFrameBufferTo16BitPng(
                                    pngBuffer,
                                    pngSourceRowBytes,
                                    width,
                                    height,
                                    fullOutputPath,
                                    rendererMode,
                                    backBufferDescription.Format);
                                request.TrySetResult(pngCaptureResult);
                                Logger.Log(
                                    $"PREVIEW_FRAME_CAPTURE_RESULT ok={pngCaptureResult.Succeeded} renderer={pngCaptureResult.RendererMode} path={pngCaptureResult.FilePath ?? "n/a"} width={pngCaptureResult.CapturedWidth} height={pngCaptureResult.CapturedHeight} avgLum={pngCaptureResult.AverageLuminance:0.00} pureBlackPct={pngCaptureResult.PureBlackPercent:0.00}");
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
                    return;
                }

                request.TrySetResult(captureResult);
                Logger.Log(
                    $"PREVIEW_FRAME_CAPTURE_RESULT ok={captureResult.Succeeded} renderer={captureResult.RendererMode} path={captureResult.FilePath ?? "n/a"} width={captureResult.CapturedWidth} height={captureResult.CapturedHeight} avgLum={captureResult.AverageLuminance:0.00} pureBlackPct={captureResult.PureBlackPercent:0.00}");
            }
            finally
            {
                if (disposeBackBuffer)
                {
                    backBuffer?.Dispose();
                }
            }
        }
        catch (Exception ex)
        {
            Interlocked.Exchange(ref _frameCaptureEncodeInProgress, 0);
            request.TrySetResult(CreateFrameCaptureError($"Preview frame capture failed: {ex.Message}", rendererMode));
            Logger.Log($"PREVIEW_FRAME_CAPTURE_RESULT ok=false renderer={rendererMode} type={ex.GetType().Name} hr=0x{ex.HResult:X8} msg={ex.Message}");
        }
    }

    private void FailPendingFrameCapture(string message)
    {
        var request = Interlocked.Exchange(ref _frameCaptureRequest, null);
        Interlocked.Exchange(ref _frameCaptureOutputPath, null);
        if (request == null)
        {
            return;
        }

        request.TrySetResult(CreateFrameCaptureError(message));
        Logger.Log($"PREVIEW_FRAME_CAPTURE_ABORTED reason={message}");
    }

    private void DisposeFrameCaptureStagingResources()
    {
        _captureStagingTexture?.Dispose();
        _captureStagingTexture = null;
        _captureStagingWidth = 0;
        _captureStagingHeight = 0;
    }
}

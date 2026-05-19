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

    private static PreviewFrameCaptureResult CreateFrameCaptureError(string message, string rendererMode = "Unknown")
    {
        return new PreviewFrameCaptureResult
        {
            Succeeded = false,
            Message = message,
            RendererMode = rendererMode,
            LuminanceHistogram = new int[16]
        };
    }
}

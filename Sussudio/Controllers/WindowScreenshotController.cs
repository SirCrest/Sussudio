using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using Sussudio.Models;

namespace Sussudio.Controllers;

internal sealed class WindowScreenshotController
{
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly Func<IntPtr> _windowHandleProvider;

    public WindowScreenshotController(
        DispatcherQueue dispatcherQueue,
        Func<IntPtr> windowHandleProvider)
    {
        _dispatcherQueue = dispatcherQueue;
        _windowHandleProvider = windowHandleProvider;
    }

    public Task<WindowScreenshotResult> CaptureAsync(
        string outputPath,
        CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromResult(new WindowScreenshotResult
            {
                Succeeded = false,
                Message = "Screenshot canceled."
            });
        }

        var completion = new TaskCompletionSource<WindowScreenshotResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationTokenRegistration cancellationRegistration = default;
        if (cancellationToken.CanBeCanceled)
        {
            cancellationRegistration = cancellationToken.Register(() =>
            {
                completion.TrySetResult(new WindowScreenshotResult
                {
                    Succeeded = false,
                    Message = "Screenshot canceled."
                });
            });
            _ = completion.Task.ContinueWith(
                _ => cancellationRegistration.Dispose(),
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
        }

        if (!_dispatcherQueue.TryEnqueue(() =>
        {
            try
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    completion.TrySetResult(new WindowScreenshotResult
                    {
                        Succeeded = false,
                        Message = "Screenshot canceled."
                    });
                    return;
                }

                var result = CaptureCore(outputPath);
                completion.TrySetResult(result);
            }
            catch (Exception ex)
            {
                completion.TrySetResult(new WindowScreenshotResult
                {
                    Succeeded = false,
                    Message = $"Screenshot failed: {ex.Message}"
                });
            }
        }))
        {
            cancellationRegistration.Dispose();
            completion.TrySetResult(new WindowScreenshotResult
            {
                Succeeded = false,
                Message = "Failed to enqueue screenshot capture on the UI thread."
            });
        }

        return completion.Task;
    }

    private WindowScreenshotResult CaptureCore(string outputPath)
        => WindowScreenshotNativeCapture.Capture(_windowHandleProvider(), outputPath);
}

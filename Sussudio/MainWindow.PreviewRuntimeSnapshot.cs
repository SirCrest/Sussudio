using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Sussudio.Controllers;
using Sussudio.Models;

namespace Sussudio;

// UI-thread automation/runtime snapshot dispatch and read-only preview state
// projection for diagnostics and MCP/CLI callers.
public sealed partial class MainWindow
{
    private async Task<PreviewRuntimeSnapshot> GetPreviewRuntimeSnapshotAsync(CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            throw new OperationCanceledException(cancellationToken);
        }

        if (_dispatcherQueue.HasThreadAccess)
        {
            return GetPreviewRuntimeSnapshot();
        }

        const int maxAttempts = 3;
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var completion = new TaskCompletionSource<PreviewRuntimeSnapshot>(TaskCreationOptions.RunContinuationsAsynchronously);
            CancellationTokenRegistration registration = default;
            if (cancellationToken.CanBeCanceled)
            {
                registration = cancellationToken.Register(() =>
                {
                    completion.TrySetCanceled(cancellationToken);
                });
            }

            var enqueued = _dispatcherQueue.TryEnqueue(() =>
            {
                try
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        completion.TrySetCanceled(cancellationToken);
                        return;
                    }

                    completion.TrySetResult(GetPreviewRuntimeSnapshot());
                }
                catch (Exception ex)
                {
                    completion.TrySetException(ex);
                }
                finally
                {
                    registration.Dispose();
                }
            });

            if (enqueued)
            {
                return await completion.Task.ConfigureAwait(false);
            }

            registration.Dispose();
            if (attempt >= maxAttempts)
            {
                break;
            }

            await Task.Delay(50, cancellationToken).ConfigureAwait(false);
        }

        throw new InvalidOperationException("Failed to enqueue preview snapshot operation.");
    }

    private PreviewRuntimeSnapshot GetPreviewRuntimeSnapshot()
    {
        var startupMissingSignals = PreviewStartupMissingSignals;
        if (string.IsNullOrWhiteSpace(startupMissingSignals) &&
            CurrentPreviewStartupState is PreviewStartupState.WaitingForFirstVisual or PreviewStartupState.Failed)
        {
            startupMissingSignals = BuildPreviewStartupMissingSignals();
        }

        return PreviewRuntimeSnapshotController.Build(new PreviewRuntimeSnapshotInput
        {
            D3DRenderer = _d3dRenderer,
            IsPreviewing = ViewModel.IsPreviewing,
            PreviewSourceAttached = _previewSource != null,
            GpuElementVisible = PreviewSwapChainPanel.Visibility == Visibility.Visible,
            CpuElementVisible = PreviewImage.Visibility == Visibility.Visible,
            PlaceholderVisible = NoDevicePlaceholder.Visibility == Visibility.Visible,
            FramesArrived = Interlocked.Read(ref _previewFramesArrived),
            FramesDisplayed = Interlocked.Read(ref _previewFramesDisplayed),
            FramesDropped = Interlocked.Read(ref _previewFramesDropped),
            LastPresentedTick = Interlocked.Read(ref _previewLastPresentedTick),
            PreviewMinPresentationIntervalMs = _previewMinPresentationIntervalMs,
            StartupState = CurrentPreviewStartupState.ToString(),
            IsStartupWaitingForFirstVisual = CurrentPreviewStartupState == PreviewStartupState.WaitingForFirstVisual,
            StartupAttemptId = PreviewStartupAttemptId,
            StartupRequestedUtc = PreviewStartupRequestedUtc,
            StartupTimeoutMs = PreviewStartupVisualTimeoutMs,
            StartupGpuSignalMediaOpened = _previewGpuSignalMediaOpened,
            StartupGpuSignalFirstFrame = _previewGpuSignalFirstFrame,
            StartupGpuSignalPlaybackAdvancing = _previewGpuSignalPlaybackAdvancing,
            StartupRequiredSignals = _previewStartupRequiredSignals,
            StartupReceivedSignals = _previewStartupReceivedSignals,
            StartupStrategy = _previewStartupStrategy,
            StartupMissingSignals = startupMissingSignals,
            StartupRecoveryAttemptCount = PreviewStartupRecoveryAttemptCount,
            StartupLastFailureReason = PreviewStartupLastFailureReason,
            FirstVisualConfirmed = IsPreviewFirstVisualConfirmed,
            GpuPositionEventCount = Interlocked.Read(ref _previewStartupPositionEventCount)
        });
    }
}

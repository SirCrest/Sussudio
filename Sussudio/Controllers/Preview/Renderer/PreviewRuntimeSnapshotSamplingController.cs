using System;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.ViewModels;

namespace Sussudio.Controllers;

internal sealed class PreviewRuntimeSnapshotSamplingControllerContext
{
    public required WindowUiDispatchController UiDispatchController { get; init; }
    public required MainViewModel ViewModel { get; init; }
    public required PreviewRendererHostController RendererHostController { get; init; }
    public required PreviewStartupSessionController StartupSessionController { get; init; }
    public required PreviewStartupSignalCoordinator StartupSignalCoordinator { get; init; }
    public required Func<bool> IsGpuElementVisible { get; init; }
    public required Func<bool> IsCpuElementVisible { get; init; }
    public required Func<bool> IsPlaceholderVisible { get; init; }
    public required Func<int> GetStartupVisualTimeoutMs { get; init; }
}

internal sealed class PreviewRuntimeSnapshotSamplingController
{
    private readonly PreviewRuntimeSnapshotSamplingControllerContext _context;

    public PreviewRuntimeSnapshotSamplingController(PreviewRuntimeSnapshotSamplingControllerContext context)
    {
        _context = context;
    }

    public Task<PreviewRuntimeSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default)
        => _context.UiDispatchController.InvokeWithRetryAsync(
            BuildSnapshot,
            "Failed to enqueue preview snapshot operation.",
            cancellationToken);

    private PreviewRuntimeSnapshot BuildSnapshot()
    {
        var startupSession = _context.StartupSessionController;
        var startupSignals = _context.StartupSignalCoordinator;
        var startupSignalSnapshot = startupSignals.Snapshot;
        var startupMissingSignals = startupSession.MissingSignals;
        if (string.IsNullOrWhiteSpace(startupMissingSignals) &&
            startupSession.ShouldRefreshMissingSignalsForSnapshot)
        {
            startupMissingSignals = startupSignals.BuildMissingSignals();
        }

        var rendererHost = _context.RendererHostController;
        return PreviewRuntimeSnapshotController.Build(new PreviewRuntimeSnapshotInput
        {
            D3DRenderer = rendererHost.Renderer,
            IsPreviewing = _context.ViewModel.IsPreviewing,
            PreviewSourceAttached = rendererHost.IsCpuPreviewSourceAttached,
            GpuElementVisible = _context.IsGpuElementVisible(),
            CpuElementVisible = _context.IsCpuElementVisible(),
            PlaceholderVisible = _context.IsPlaceholderVisible(),
            FramesArrived = rendererHost.FramesArrived,
            FramesDisplayed = rendererHost.FramesDisplayed,
            FramesDropped = rendererHost.FramesDropped,
            LastPresentedTick = rendererHost.LastPresentedTick,
            PreviewMinPresentationIntervalMs = rendererHost.PreviewMinPresentationIntervalMs,
            StartupState = startupSession.State.ToString(),
            IsStartupWaitingForFirstVisual = startupSession.IsWaitingForFirstVisual,
            StartupAttemptId = startupSession.AttemptId,
            StartupRequestedUtc = startupSession.RequestedUtc,
            StartupTimeoutMs = _context.GetStartupVisualTimeoutMs(),
            StartupGpuSignalMediaOpened = startupSignalSnapshot.GpuSignalMediaOpened,
            StartupGpuSignalFirstFrame = startupSignalSnapshot.GpuSignalFirstFrame,
            StartupGpuSignalPlaybackAdvancing = startupSignalSnapshot.GpuSignalPlaybackAdvancing,
            StartupRequiredSignals = startupSignalSnapshot.RequiredSignals,
            StartupReceivedSignals = startupSignalSnapshot.ReceivedSignals,
            StartupStrategy = startupSignalSnapshot.Strategy,
            StartupMissingSignals = startupMissingSignals,
            StartupRecoveryAttemptCount = startupSession.RecoveryAttemptCount,
            StartupLastFailureReason = startupSession.LastFailureReason,
            FirstVisualConfirmed = startupSession.FirstVisualConfirmed,
            GpuPositionEventCount = startupSignals.PositionEventCount
        });
    }
}

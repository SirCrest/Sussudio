using System;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.Services.Preview;
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

internal sealed class PreviewRuntimeSnapshotInput
{
    public D3D11PreviewRenderer? D3DRenderer { get; init; }
    public bool IsPreviewing { get; init; }
    public bool PreviewSourceAttached { get; init; }
    public bool GpuElementVisible { get; init; }
    public bool CpuElementVisible { get; init; }
    public bool PlaceholderVisible { get; init; }
    public long FramesArrived { get; init; }
    public long FramesDisplayed { get; init; }
    public long FramesDropped { get; init; }
    public long LastPresentedTick { get; init; }
    public double PreviewMinPresentationIntervalMs { get; init; }
    public string StartupState { get; init; } = "Idle";
    public bool IsStartupWaitingForFirstVisual { get; init; }
    public string? StartupAttemptId { get; init; }
    public DateTimeOffset? StartupRequestedUtc { get; init; }
    public int StartupTimeoutMs { get; init; }
    public bool StartupGpuSignalMediaOpened { get; init; }
    public bool StartupGpuSignalFirstFrame { get; init; }
    public bool StartupGpuSignalPlaybackAdvancing { get; init; }
    public PreviewStartupSignalFlags StartupRequiredSignals { get; init; }
    public PreviewStartupSignalFlags StartupReceivedSignals { get; init; }
    public PreviewStartupStrategy StartupStrategy { get; init; }
    public string? StartupMissingSignals { get; init; }
    public int StartupRecoveryAttemptCount { get; init; }
    public string? StartupLastFailureReason { get; init; }
    public bool FirstVisualConfirmed { get; init; }
    public long GpuPositionEventCount { get; init; }
}

internal static class PreviewRuntimeSnapshotController
{
    public static PreviewRuntimeSnapshot Build(PreviewRuntimeSnapshotInput input)
    {
        var d3dProjection = PreviewRuntimeD3DProjection.Build(input);
        var healthInput = PreviewRuntimeSnapshotHealthInputFactory.Build(
            input,
            d3dProjection,
            Environment.TickCount64,
            DateTimeOffset.UtcNow);
        var health = PreviewRuntimeSnapshotHealthPolicy.Evaluate(healthInput);

        return PreviewRuntimeSnapshotMapper.Build(input, d3dProjection, health, DateTimeOffset.UtcNow);
    }
}

internal sealed class PreviewRuntimeSnapshotHealthInput
{
    public bool IsPreviewing { get; init; }
    public bool IsStartupWaitingForFirstVisual { get; init; }
    public DateTimeOffset? StartupRequestedUtc { get; init; }
    public int StartupTimeoutMs { get; init; }
    public bool RendererAttached { get; init; }
    public bool GpuActive { get; init; }
    public long FramesArrived { get; init; }
    public long FramesDisplayed { get; init; }
    public long LastPresentedTick { get; init; }
    public long CurrentTick { get; init; }
    public DateTimeOffset UtcNow { get; init; }
}

internal readonly record struct PreviewRuntimeSnapshotHealth(
    double? StartupElapsedMs,
    bool BlankSuspected,
    bool StallSuspected);

internal static class PreviewRuntimeSnapshotHealthInputFactory
{
    public static PreviewRuntimeSnapshotHealthInput Build(
        PreviewRuntimeSnapshotInput input,
        PreviewRuntimeD3DProjection d3dProjection,
        long currentTick,
        DateTimeOffset utcNow)
    {
        return new PreviewRuntimeSnapshotHealthInput
        {
            IsPreviewing = input.IsPreviewing,
            IsStartupWaitingForFirstVisual = input.IsStartupWaitingForFirstVisual,
            StartupRequestedUtc = input.StartupRequestedUtc,
            StartupTimeoutMs = input.StartupTimeoutMs,
            RendererAttached = d3dProjection.RendererAttached,
            GpuActive = d3dProjection.GpuActive,
            FramesArrived = d3dProjection.FramesArrived,
            FramesDisplayed = d3dProjection.FramesDisplayed,
            LastPresentedTick = input.LastPresentedTick,
            CurrentTick = currentTick,
            UtcNow = utcNow
        };
    }
}

internal static class PreviewRuntimeSnapshotHealthPolicy
{
    public static PreviewRuntimeSnapshotHealth Evaluate(PreviewRuntimeSnapshotHealthInput input)
    {
        var previewPipelineActive = input.IsPreviewing && input.RendererAttached;
        var startupElapsedMs = input.StartupRequestedUtc.HasValue
            ? Math.Max(0, (input.UtcNow - input.StartupRequestedUtc.Value).TotalMilliseconds)
            : (double?)null;
        var startupTimedOut = input.IsPreviewing &&
                              input.IsStartupWaitingForFirstVisual &&
                              startupElapsedMs.GetValueOrDefault() >= input.StartupTimeoutMs;
        var blankSuspected = !input.GpuActive && previewPipelineActive &&
                             input.FramesArrived > 30 &&
                             input.FramesDisplayed == 0;
        if (!blankSuspected && startupTimedOut)
        {
            blankSuspected = true;
        }

        var stallSuspected = !input.GpuActive && previewPipelineActive &&
                             input.LastPresentedTick > 0 &&
                             input.CurrentTick - input.LastPresentedTick > 3000;

        return new PreviewRuntimeSnapshotHealth(startupElapsedMs, blankSuspected, stallSuspected);
    }
}

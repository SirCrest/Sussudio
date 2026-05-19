using System;

namespace Sussudio.Controllers;

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

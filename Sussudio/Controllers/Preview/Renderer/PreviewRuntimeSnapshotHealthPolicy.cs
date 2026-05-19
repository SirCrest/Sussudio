using System;

namespace Sussudio.Controllers;

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

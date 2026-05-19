using System;
using Sussudio.Models;
using Sussudio.Services.Preview;

namespace Sussudio.Controllers;

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

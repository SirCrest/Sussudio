namespace Sussudio.Models;

public sealed partial class PreviewRuntimeSnapshot
{
    public string StartupState { get; init; } = "Idle";
    public string? StartupAttemptId { get; init; }
    public double? StartupElapsedMs { get; init; }
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
}

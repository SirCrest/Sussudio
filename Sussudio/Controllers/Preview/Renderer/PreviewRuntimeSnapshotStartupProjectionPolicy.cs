using Sussudio.Models;

namespace Sussudio.Controllers;

internal readonly record struct PreviewRuntimeSnapshotStartupProjection(
    string State,
    string? AttemptId,
    double? ElapsedMs,
    int TimeoutMs,
    bool GpuSignalMediaOpened,
    bool GpuSignalFirstFrame,
    bool GpuSignalPlaybackAdvancing,
    PreviewStartupSignalFlags RequiredSignals,
    PreviewStartupSignalFlags ReceivedSignals,
    PreviewStartupStrategy Strategy,
    string? MissingSignals,
    int RecoveryAttemptCount,
    string? LastFailureReason,
    bool FirstVisualConfirmed);

internal static class PreviewRuntimeSnapshotStartupProjectionPolicy
{
    public static PreviewRuntimeSnapshotStartupProjection Evaluate(
        PreviewRuntimeSnapshotInput input,
        PreviewRuntimeSnapshotHealth health)
        => new(
            State: input.StartupState,
            AttemptId: input.StartupAttemptId,
            ElapsedMs: health.StartupElapsedMs,
            TimeoutMs: input.StartupTimeoutMs,
            GpuSignalMediaOpened: input.StartupGpuSignalMediaOpened,
            GpuSignalFirstFrame: input.StartupGpuSignalFirstFrame,
            GpuSignalPlaybackAdvancing: input.StartupGpuSignalPlaybackAdvancing,
            RequiredSignals: input.StartupRequiredSignals,
            ReceivedSignals: input.StartupReceivedSignals,
            Strategy: input.StartupStrategy,
            MissingSignals: input.StartupMissingSignals,
            RecoveryAttemptCount: input.StartupRecoveryAttemptCount,
            LastFailureReason: input.StartupLastFailureReason,
            FirstVisualConfirmed: input.FirstVisualConfirmed);
}

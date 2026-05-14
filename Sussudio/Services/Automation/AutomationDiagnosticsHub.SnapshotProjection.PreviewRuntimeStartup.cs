using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static PreviewRuntimeStartupProjection BuildPreviewRuntimeStartupProjection(
        PreviewRuntimeSnapshot previewRuntime)
        => new()
        {
            State = previewRuntime.StartupState,
            AttemptId = previewRuntime.StartupAttemptId,
            ElapsedMs = previewRuntime.StartupElapsedMs,
            TimeoutMs = previewRuntime.StartupTimeoutMs,
            GpuSignalMediaOpened = previewRuntime.StartupGpuSignalMediaOpened,
            GpuSignalFirstFrame = previewRuntime.StartupGpuSignalFirstFrame,
            GpuSignalPlaybackAdvancing = previewRuntime.StartupGpuSignalPlaybackAdvancing,
            RequiredSignals = previewRuntime.StartupRequiredSignals,
            ReceivedSignals = previewRuntime.StartupReceivedSignals,
            Strategy = previewRuntime.StartupStrategy.ToString(),
            MissingSignals = previewRuntime.StartupMissingSignals,
            RecoveryAttemptCount = previewRuntime.StartupRecoveryAttemptCount,
            LastFailureReason = previewRuntime.StartupLastFailureReason,
            FirstVisualConfirmed = previewRuntime.FirstVisualConfirmed,
            BlankSuspected = previewRuntime.BlankSuspected,
            Stalled = previewRuntime.StallSuspected,
            RendererMode = previewRuntime.RendererMode
        };

    private readonly record struct PreviewRuntimeStartupProjection
    {
        public string State { get; init; }
        public string? AttemptId { get; init; }
        public double? ElapsedMs { get; init; }
        public int TimeoutMs { get; init; }
        public bool GpuSignalMediaOpened { get; init; }
        public bool GpuSignalFirstFrame { get; init; }
        public bool GpuSignalPlaybackAdvancing { get; init; }
        public PreviewStartupSignalFlags RequiredSignals { get; init; }
        public PreviewStartupSignalFlags ReceivedSignals { get; init; }
        public string Strategy { get; init; }
        public string? MissingSignals { get; init; }
        public int RecoveryAttemptCount { get; init; }
        public string? LastFailureReason { get; init; }
        public bool FirstVisualConfirmed { get; init; }
        public bool BlankSuspected { get; init; }
        public bool Stalled { get; init; }
        public string RendererMode { get; init; }
    }
}

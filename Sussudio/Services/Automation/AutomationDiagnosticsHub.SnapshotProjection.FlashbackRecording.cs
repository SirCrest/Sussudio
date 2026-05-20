using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static FlashbackRecordingProjection BuildFlashbackRecordingProjection(
        CaptureRuntimeSnapshot captureRuntime,
        CaptureHealthSnapshot health)
    {
        var startupCache = BuildFlashbackRecordingStartupCacheProjection(health);
        var queues = BuildFlashbackRecordingQueuesProjection(health);
        var runtime = BuildFlashbackRecordingRuntimeProjection(health);
        var backend = BuildFlashbackRecordingBackendProjection(captureRuntime, health);
        var encoder = BuildFlashbackRecordingEncoderProjection(health);

        return new()
        {
            EncodingFailed = health.FlashbackEncodingFailed,
            EncodingFailureType = health.FlashbackEncodingFailureType,
            EncodingFailureMessage = health.FlashbackEncodingFailureMessage,
            FatalCleanupInProgress = health.FatalCleanupInProgress,
            CleanupInProgress = health.FlashbackCleanupInProgress,
            ForceRotateActive = health.FlashbackForceRotateActive,
            ForceRotateRequested = health.FlashbackForceRotateRequested,
            ForceRotateDraining = health.FlashbackForceRotateDraining,
            StartupCache = startupCache,
            Queues = queues,
            Runtime = runtime,
            Backend = backend,
            Encoder = encoder
        };
    }

    private readonly record struct FlashbackRecordingProjection
    {
        public bool EncodingFailed { get; init; }
        public string? EncodingFailureType { get; init; }
        public string? EncodingFailureMessage { get; init; }
        public bool FatalCleanupInProgress { get; init; }
        public bool CleanupInProgress { get; init; }
        public bool ForceRotateActive { get; init; }
        public bool ForceRotateRequested { get; init; }
        public bool ForceRotateDraining { get; init; }
        public FlashbackRecordingStartupCacheProjection StartupCache { get; init; }
        public FlashbackRecordingQueuesProjection Queues { get; init; }
        public FlashbackRecordingRuntimeProjection Runtime { get; init; }
        public FlashbackRecordingBackendProjection Backend { get; init; }
        public FlashbackRecordingEncoderProjection Encoder { get; init; }
    }
}

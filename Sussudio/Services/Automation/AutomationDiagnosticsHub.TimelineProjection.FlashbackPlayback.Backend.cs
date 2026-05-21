using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static PerformanceTimelineFlashbackPlaybackBackendProjection BuildPerformanceTimelineFlashbackPlaybackBackendProjection(
        AutomationSnapshot snapshot)
        => new(
            SettingsStale: snapshot.FlashbackBackendSettingsStale,
            SettingsStaleReason: snapshot.FlashbackBackendSettingsStaleReason,
            ActiveFormat: snapshot.FlashbackBackendActiveFormat,
            RequestedFormat: snapshot.FlashbackBackendRequestedFormat,
            ActivePreset: snapshot.FlashbackBackendActivePreset,
            RequestedPreset: snapshot.FlashbackBackendRequestedPreset,
            VideoQueueRejectedFrames: snapshot.FlashbackVideoQueueRejectedFrames,
            VideoQueueLastRejectReason: snapshot.FlashbackVideoQueueLastRejectReason,
            GpuQueueRejectedFrames: snapshot.FlashbackGpuQueueRejectedFrames,
            GpuQueueLastRejectReason: snapshot.FlashbackGpuQueueLastRejectReason,
            FatalCleanupInProgress: snapshot.FatalCleanupInProgress,
            CleanupInProgress: snapshot.FlashbackCleanupInProgress,
            ForceRotateRequested: snapshot.FlashbackForceRotateRequested,
            ForceRotateDraining: snapshot.FlashbackForceRotateDraining);

    private readonly record struct PerformanceTimelineFlashbackPlaybackBackendProjection(
        bool SettingsStale,
        string SettingsStaleReason,
        string ActiveFormat,
        string RequestedFormat,
        string ActivePreset,
        string RequestedPreset,
        long VideoQueueRejectedFrames,
        string VideoQueueLastRejectReason,
        long GpuQueueRejectedFrames,
        string GpuQueueLastRejectReason,
        bool FatalCleanupInProgress,
        bool CleanupInProgress,
        bool ForceRotateRequested,
        bool ForceRotateDraining);
}

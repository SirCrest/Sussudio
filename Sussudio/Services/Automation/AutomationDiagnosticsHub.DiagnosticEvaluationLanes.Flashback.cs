using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static string BuildFlashbackRecordingLane(CaptureHealthSnapshot health)
    {
        return
            $"flashback recording active={health.FlashbackActive} failed={health.FlashbackEncodingFailed} type={health.FlashbackEncodingFailureType ?? "None"} " +
            $"dropped={health.FlashbackDroppedFrames} encoderDrops={health.FlashbackVideoEncoderDroppedFrames} seqGaps={health.FlashbackVideoSequenceGaps} " +
            $"queueRejects={health.FlashbackVideoQueueRejectedFrames} lastReject={health.FlashbackVideoQueueLastRejectReason ?? "None"} " +
            $"backendStale={health.FlashbackBackendSettingsStale} staleReason={health.FlashbackBackendSettingsStaleReason} activeFormat={health.FlashbackBackendActiveFormat} requestedFormat={health.FlashbackBackendRequestedFormat} activePreset={health.FlashbackBackendActivePreset} requestedPreset={health.FlashbackBackendRequestedPreset} " +
            $"gpuOverloads={health.FlashbackGpuFramesDropped} forceRotate={health.FlashbackForceRotateActive} requested={health.FlashbackForceRotateRequested} draining={health.FlashbackForceRotateDraining} queue={health.FlashbackVideoQueueDepth}/{health.FlashbackVideoQueueCapacity} maxQueue={health.FlashbackVideoQueueMaxDepth} " +
            $"audioQueue={health.FlashbackAudioQueueDepth}/{health.FlashbackAudioQueueCapacity} " +
            $"queueAgeMs={health.FlashbackVideoQueueOldestFrameAgeMs} backpressure={health.FlashbackVideoBackpressureWaitMs}ms/{health.FlashbackVideoBackpressureEvents} lastBackpressure={health.FlashbackVideoBackpressureLastWaitMs}ms maxBackpressure={health.FlashbackVideoBackpressureMaxWaitMs}ms " +
            $"fatalCleanup={health.FatalCleanupInProgress} flashbackCleanup={health.FlashbackCleanupInProgress}";
    }

}

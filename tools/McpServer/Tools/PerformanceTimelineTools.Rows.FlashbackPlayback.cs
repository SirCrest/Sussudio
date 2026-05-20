using System.Text.Json;
using Sussudio.Tools;

namespace McpServer.Tools;

public static partial class PerformanceTimelineTools
{
    private static void PopulateFlashbackPlaybackTimelineRow(JsonElement item, TimelineRow row)
    {
        row.FlashbackPlaybackState = AutomationSnapshotFormatter.Get(item, "FlashbackPlaybackState");
        row.FlashbackPlaybackTargetFps = AutomationSnapshotFormatter.GetDouble(item, "FlashbackPlaybackTargetFps");
        row.FlashbackPlaybackObservedFps = AutomationSnapshotFormatter.GetDouble(item, "FlashbackPlaybackObservedFps");
        row.FlashbackPlaybackP99FrameMs = AutomationSnapshotFormatter.GetDouble(item, "FlashbackPlaybackP99FrameMs");
        row.FlashbackPlaybackMaxFrameMs = AutomationSnapshotFormatter.GetDouble(item, "FlashbackPlaybackMaxFrameMs");
        row.FlashbackPlaybackOnePercentLowFps = AutomationSnapshotFormatter.GetDouble(item, "FlashbackPlaybackOnePercentLowFps");
        row.FlashbackPlaybackFivePercentLowFps = AutomationSnapshotFormatter.GetDouble(item, "FlashbackPlaybackFivePercentLowFps");
        row.FlashbackPlaybackSlowFramePercent = AutomationSnapshotFormatter.GetDouble(item, "FlashbackPlaybackSlowFramePercent");
        row.FlashbackPlaybackDecodeP99Ms = AutomationSnapshotFormatter.GetDouble(item, "FlashbackPlaybackDecodeP99Ms");
        row.FlashbackPlaybackDecodeMaxMs = AutomationSnapshotFormatter.GetDouble(item, "FlashbackPlaybackDecodeMaxMs");
        row.FlashbackPlaybackMaxDecodePhase = AutomationSnapshotFormatter.Get(item, "FlashbackPlaybackMaxDecodePhase");
        row.FlashbackPlaybackMaxDecodeReceiveMs = AutomationSnapshotFormatter.GetDouble(item, "FlashbackPlaybackMaxDecodeReceiveMs");
        row.FlashbackPlaybackMaxDecodeFeedMs = AutomationSnapshotFormatter.GetDouble(item, "FlashbackPlaybackMaxDecodeFeedMs");
        row.FlashbackPlaybackMaxDecodeReadMs = AutomationSnapshotFormatter.GetDouble(item, "FlashbackPlaybackMaxDecodeReadMs");
        row.FlashbackPlaybackMaxDecodeSendMs = AutomationSnapshotFormatter.GetDouble(item, "FlashbackPlaybackMaxDecodeSendMs");
        row.FlashbackPlaybackMaxDecodeAudioMs = AutomationSnapshotFormatter.GetDouble(item, "FlashbackPlaybackMaxDecodeAudioMs");
        row.FlashbackPlaybackMaxDecodeConvertMs = AutomationSnapshotFormatter.GetDouble(item, "FlashbackPlaybackMaxDecodeConvertMs");
        row.FlashbackPlaybackPendingCommands = AutomationSnapshotFormatter.GetInt(item, "FlashbackPlaybackPendingCommands");
        row.FlashbackPlaybackMaxPendingCommands = AutomationSnapshotFormatter.GetInt(item, "FlashbackPlaybackMaxPendingCommands");
        row.FlashbackPlaybackMaxCommandQueueLatencyMs = AutomationSnapshotFormatter.GetLong(item, "FlashbackPlaybackMaxCommandQueueLatencyMs");
        row.FlashbackPlaybackMaxCommandQueueLatencyCommand = AutomationSnapshotFormatter.Get(item, "FlashbackPlaybackMaxCommandQueueLatencyCommand");
        row.FlashbackPlaybackCommandsEnqueued = AutomationSnapshotFormatter.GetLong(item, "FlashbackPlaybackCommandsEnqueued");
        row.FlashbackPlaybackCommandsProcessed = AutomationSnapshotFormatter.GetLong(item, "FlashbackPlaybackCommandsProcessed");
        row.FlashbackPlaybackCommandsDropped = AutomationSnapshotFormatter.GetLong(item, "FlashbackPlaybackCommandsDropped");
        row.FlashbackPlaybackCommandsSkippedNotReady = AutomationSnapshotFormatter.GetLong(item, "FlashbackPlaybackCommandsSkippedNotReady");
        row.FlashbackPlaybackScrubUpdatesCoalesced = AutomationSnapshotFormatter.GetLong(item, "FlashbackPlaybackScrubUpdatesCoalesced");
        row.FlashbackPlaybackSeekCommandsCoalesced = AutomationSnapshotFormatter.GetLong(item, "FlashbackPlaybackSeekCommandsCoalesced");
        row.FlashbackPlaybackLastCommandQueued = AutomationSnapshotFormatter.Get(item, "FlashbackPlaybackLastCommandQueued");
        row.FlashbackPlaybackLastCommandProcessed = AutomationSnapshotFormatter.Get(item, "FlashbackPlaybackLastCommandProcessed");
        row.FlashbackPlaybackSubmitFailures = AutomationSnapshotFormatter.GetLong(item, "FlashbackPlaybackSubmitFailures");
        row.FlashbackPlaybackLastDropUtcUnixMs = AutomationSnapshotFormatter.GetLong(item, "FlashbackPlaybackLastDropUtcUnixMs");
        row.FlashbackPlaybackLastDropReason = AutomationSnapshotFormatter.Get(item, "FlashbackPlaybackLastDropReason");
        row.FlashbackPlaybackLastSubmitFailureUtcUnixMs = AutomationSnapshotFormatter.GetLong(item, "FlashbackPlaybackLastSubmitFailureUtcUnixMs");
        row.FlashbackPlaybackLastSubmitFailure = AutomationSnapshotFormatter.Get(item, "FlashbackPlaybackLastSubmitFailure");
        row.FlashbackPlaybackDroppedFrames = AutomationSnapshotFormatter.GetLong(item, "FlashbackPlaybackDroppedFrames");
        row.FlashbackPlaybackAudioMasterUnavailableFallbacks = AutomationSnapshotFormatter.GetLong(item, "FlashbackPlaybackAudioMasterUnavailableFallbacks");
        row.FlashbackPlaybackAudioMasterStaleFallbacks = AutomationSnapshotFormatter.GetLong(item, "FlashbackPlaybackAudioMasterStaleFallbacks");
        row.FlashbackPlaybackAudioMasterDriftOutlierFallbacks = AutomationSnapshotFormatter.GetLong(item, "FlashbackPlaybackAudioMasterDriftOutlierFallbacks");
        row.FlashbackPlaybackAudioMasterLastFallbackReason = AutomationSnapshotFormatter.Get(item, "FlashbackPlaybackAudioMasterLastFallbackReason");
        row.FlashbackPlaybackAudioMasterLastFallbackClockAgeMs = AutomationSnapshotFormatter.GetDouble(item, "FlashbackPlaybackAudioMasterLastFallbackClockAgeMs");
        row.FlashbackPlaybackSegmentSwitches = AutomationSnapshotFormatter.GetLong(item, "FlashbackPlaybackSegmentSwitches");
        row.FlashbackPlaybackFmp4Reopens = AutomationSnapshotFormatter.GetLong(item, "FlashbackPlaybackFmp4Reopens");
        row.FlashbackPlaybackWriteHeadWaits = AutomationSnapshotFormatter.GetLong(item, "FlashbackPlaybackWriteHeadWaits");
        row.FlashbackPlaybackNearLiveSnaps = AutomationSnapshotFormatter.GetLong(item, "FlashbackPlaybackNearLiveSnaps");
        row.FlashbackPlaybackDecodeErrorSnaps = AutomationSnapshotFormatter.GetLong(item, "FlashbackPlaybackDecodeErrorSnaps");
        row.FlashbackPlaybackLastWriteHeadWaitGapMs = AutomationSnapshotFormatter.GetLong(item, "FlashbackPlaybackLastWriteHeadWaitGapMs");
        row.FlashbackPlaybackLastCommandFailureUtcUnixMs = AutomationSnapshotFormatter.GetLong(item, "FlashbackPlaybackLastCommandFailureUtcUnixMs");
        row.FlashbackPlaybackLastCommandFailure = AutomationSnapshotFormatter.Get(item, "FlashbackPlaybackLastCommandFailure");
        row.FlashbackVideoQueueRejectedFrames = AutomationSnapshotFormatter.GetLong(item, "FlashbackVideoQueueRejectedFrames");
        row.FlashbackVideoQueueLastRejectReason = AutomationSnapshotFormatter.Get(item, "FlashbackVideoQueueLastRejectReason");
        row.FlashbackGpuQueueRejectedFrames = AutomationSnapshotFormatter.GetLong(item, "FlashbackGpuQueueRejectedFrames");
        row.FlashbackGpuQueueLastRejectReason = AutomationSnapshotFormatter.Get(item, "FlashbackGpuQueueLastRejectReason");
        row.FatalCleanupInProgress = AutomationSnapshotFormatter.GetBool(item, "FatalCleanupInProgress");
        row.FlashbackCleanupInProgress = AutomationSnapshotFormatter.GetBool(item, "FlashbackCleanupInProgress");
        row.FlashbackForceRotateRequested = AutomationSnapshotFormatter.GetBool(item, "FlashbackForceRotateRequested");
        row.FlashbackForceRotateDraining = AutomationSnapshotFormatter.GetBool(item, "FlashbackForceRotateDraining");
    }
}

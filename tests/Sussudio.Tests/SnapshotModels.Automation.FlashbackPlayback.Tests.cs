using Xunit;

namespace Sussudio.Tests;

public partial class SnapshotModelsTests
{
    [Fact]
    public void AutomationSnapshot_ExposesFlashbackPlaybackMetrics()
    {
        var snapshotType = RequireType("Sussudio.Models.AutomationSnapshot");
        var automationSnapshotText = ReadRepoFile("Sussudio/Models/Automation/AutomationSnapshot.cs");

        AssertAutomationSnapshotProperties(
            snapshotType,
            "FlashbackPlaybackThreadAlive",
            "FlashbackPlaybackDroppedFrames",
            "FlashbackPlaybackAudioMasterDelayDoubles",
            "FlashbackPlaybackAudioMasterDelayShrinks",
            "FlashbackPlaybackAudioMasterFallbacks",
            "FlashbackPlaybackSegmentSwitches",
            "FlashbackPlaybackFmp4Reopens",
            "FlashbackPlaybackWriteHeadWaits",
            "FlashbackPlaybackNearLiveSnaps",
            "FlashbackPlaybackDecodeErrorSnaps",
            "FlashbackPlaybackSubmitFailures",
            "FlashbackPlaybackLastDropUtcUnixMs",
            "FlashbackPlaybackLastDropReason",
            "FlashbackPlaybackLastSubmitFailureUtcUnixMs",
            "FlashbackPlaybackLastSubmitFailure",
            "FlashbackPlaybackLastSegmentSwitchUtcUnixMs",
            "FlashbackPlaybackLastFmp4ReopenUtcUnixMs",
            "FlashbackPlaybackLastWriteHeadWaitGapMs",
            "FlashbackPlaybackCadenceSampleCount",
            "FlashbackPlaybackP95FrameMs",
            "FlashbackPlaybackP99FrameMs",
            "FlashbackPlaybackMaxFrameMs",
            "FlashbackPlaybackSlowFrames",
            "FlashbackPlaybackSlowFramePercent",
            "FlashbackPlaybackTargetFps",
            "FlashbackPlaybackOnePercentLowFps",
            "FlashbackPlaybackPtsCadenceMismatchCount",
            "FlashbackPlaybackLastPtsCadenceDeltaMs",
            "FlashbackPlaybackLastPtsCadenceExpectedMs",
            "FlashbackPlaybackSeekForwardDecodeCapHits",
            "FlashbackPlaybackLastSeekHitForwardDecodeCap",
            "FlashbackPlaybackDecodeSampleCount",
            "FlashbackPlaybackDecodeAvgMs",
            "FlashbackPlaybackDecodeP95Ms",
            "FlashbackPlaybackDecodeP99Ms",
            "FlashbackPlaybackDecodeMaxMs",
            "FlashbackPlaybackMaxDecodePhase",
            "FlashbackPlaybackMaxDecodeReceiveMs",
            "FlashbackPlaybackMaxDecodeFeedMs",
            "FlashbackPlaybackMaxDecodeReadMs",
            "FlashbackPlaybackMaxDecodeSendMs",
            "FlashbackPlaybackMaxDecodeAudioMs",
            "FlashbackPlaybackMaxDecodeConvertMs",
            "FlashbackPlaybackMaxDecodeUtcUnixMs",
            "FlashbackPlaybackMaxDecodePositionMs",
            "CaptureCadenceP99IntervalMs",
            "CaptureCadenceOnePercentLowFps",
            "FlashbackPlaybackCommandsEnqueued",
            "FlashbackPlaybackCommandsProcessed",
            "FlashbackPlaybackCommandsDropped",
            "FlashbackPlaybackCommandsSkippedNotReady",
            "FlashbackPlaybackScrubUpdatesCoalesced",
            "FlashbackPlaybackSeekCommandsCoalesced",
            "FlashbackPlaybackCommandQueueCapacity",
            "FlashbackPlaybackPendingCommands",
            "FlashbackPlaybackMaxPendingCommands",
            "FlashbackPlaybackLastCommandQueueLatencyMs",
            "FlashbackPlaybackMaxCommandQueueLatencyMs",
            "FlashbackPlaybackMaxCommandQueueLatencyCommand",
            "FlashbackPlaybackLastCommandQueued",
            "FlashbackPlaybackLastCommandProcessed",
            "FlashbackPlaybackLastCommandQueuedUtcUnixMs",
            "FlashbackPlaybackLastCommandProcessedUtcUnixMs",
            "FlashbackPlaybackLastCommandFailureUtcUnixMs",
            "FlashbackPlaybackLastCommandFailure");
        AssertContains(automationSnapshotText, "public string FlashbackPlaybackState { get; init; } = \"N/A\";");
        AssertContains(automationSnapshotText, "public double[] FlashbackPlaybackRecentFrameIntervalsMs { get; init; } = Array.Empty<double>();");
        AssertContains(automationSnapshotText, "public string FlashbackPlaybackLastCommandFailure { get; init; } = string.Empty;");
        AssertContains(automationSnapshotText, "public bool FlashbackExportActive { get; init; }");
    }
}

using Xunit;

namespace Sussudio.Tests;

public partial class SnapshotModelsTests
{
    [Fact]
    public void AutomationSnapshot_ExposesFlashbackRecordingMetrics()
    {
        var snapshotType = RequireType("Sussudio.Models.AutomationSnapshot");
        var automationSnapshotText = ReadRepoFile("Sussudio/Models/Automation/AutomationSnapshot.cs");

        AssertAutomationSnapshotProperties(
            snapshotType,
            "FlashbackTotalBytesWritten",
            "FlashbackTempDriveFreeBytes",
            "FlashbackStartupCacheBudgetBytes",
            "FlashbackStartupCacheBytes",
            "FlashbackStartupCacheSessionCount",
            "FlashbackStartupCacheDeletedSessionCount",
            "FlashbackStartupCacheFreedBytes",
            "FlashbackStartupCacheOverBudget",
            "FatalCleanupInProgress",
            "FlashbackCleanupInProgress",
            "FlashbackForceRotateActive",
            "FlashbackVideoFramesSubmittedToEncoder",
            "FlashbackVideoEncoderPacketsWritten",
            "FlashbackVideoSequenceGaps",
            "FlashbackBackendSettingsStale",
            "FlashbackBackendSettingsStaleReason",
            "FlashbackBackendActiveFormat",
            "FlashbackBackendRequestedFormat",
            "FlashbackBackendActivePreset",
            "FlashbackBackendRequestedPreset",
            "FlashbackVideoQueueOldestFrameAgeMs",
            "FlashbackVideoQueueLatencyP95Ms",
            "FlashbackVideoQueueLatencyP99Ms",
            "FlashbackVideoBackpressureWaitMs",
            "FlashbackVideoBackpressureEvents",
            "FlashbackAudioQueueCapacity",
            "FlashbackVideoQueueRejectedFrames",
            "FlashbackVideoQueueLastRejectReason",
            "FlashbackGpuQueueRejectedFrames",
            "FlashbackGpuQueueLastRejectReason");
        AssertContains(automationSnapshotText, "public bool FlashbackActive { get; init; }");
        AssertContains(automationSnapshotText, "public int FlashbackAudioQueueCapacity { get; init; }");
        AssertContains(automationSnapshotText, "public string FlashbackPlaybackState { get; init; }");
        AssertContains(automationSnapshotText, "public bool FlashbackExportActive { get; init; }");
        AssertContains(automationSnapshotText, "public bool FlashbackForceRotateActive { get; init; }");
        AssertContains(automationSnapshotText, "public long FlashbackVideoFramesSubmittedToEncoder { get; init; }");
    }
}

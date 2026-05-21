using Xunit;

namespace Sussudio.Tests;

public partial class SnapshotModelsTests
{
    [Fact]
    public void AutomationSnapshot_ExposesFlashbackRecordingMetrics()
    {
        var snapshotType = RequireType("Sussudio.Models.AutomationSnapshot");
        var flashbackRecordingText = ReadRepoFile("Sussudio/Models/Automation/AutomationSnapshot.FlashbackRecording.cs");
        var recordingText = ReadRepoFile("Sussudio/Models/Automation/AutomationSnapshot.Recording.cs");

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
        AssertContains(flashbackRecordingText, "public bool FlashbackActive { get; init; }");
        AssertContains(flashbackRecordingText, "public int FlashbackAudioQueueCapacity { get; init; }");
        AssertDoesNotContain(flashbackRecordingText, "public string FlashbackPlaybackState { get; init; }");
        AssertDoesNotContain(flashbackRecordingText, "public bool FlashbackExportActive { get; init; }");
        AssertContains(recordingText, "public bool FlashbackForceRotateActive { get; init; }");
        AssertContains(recordingText, "public long FlashbackVideoFramesSubmittedToEncoder { get; init; }");
    }
}

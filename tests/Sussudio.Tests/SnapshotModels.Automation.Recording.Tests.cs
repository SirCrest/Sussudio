using Xunit;

namespace Sussudio.Tests;

public partial class SnapshotModelsTests
{
    [Fact]
    public void AutomationSnapshot_ExposesRecordingMetrics()
    {
        var snapshotType = RequireType("Sussudio.Models.AutomationSnapshot");

        AssertAutomationSnapshotProperties(
            snapshotType,
            "RecordingVideoFramesSubmittedToEncoder",
            "RecordingVideoEncoderPts",
            "RecordingVideoEncoderPacketsWritten",
            "RecordingVideoEncoderDroppedFrames",
            "RecordingVideoSequenceGaps",
            "RecordingVideoQueueOldestFrameAgeMs",
            "RecordingVideoQueueLatencyP95Ms",
            "RecordingVideoQueueLatencyP99Ms",
            "RecordingVideoBackpressureWaitMs",
            "RecordingVideoBackpressureEvents");
    }
}

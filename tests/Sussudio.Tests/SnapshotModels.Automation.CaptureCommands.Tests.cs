using Xunit;

namespace Sussudio.Tests;

public partial class SnapshotModelsTests
{
    [Fact]
    public void AutomationSnapshot_ExposesCaptureCommandMetrics()
    {
        var snapshotType = RequireType("Sussudio.Models.AutomationSnapshot");

        AssertAutomationSnapshotProperties(
            snapshotType,
            "CaptureCommandCommandsEnqueued",
            "CaptureCommandCommandsCompleted",
            "CaptureCommandCommandsFailed",
            "CaptureCommandCommandsCanceled",
            "CaptureCommandCommandsCoalesced",
            "CaptureCommandPendingCommands",
            "CaptureCommandMaxPendingCommands",
            "CaptureCommandOldestPendingCommandAgeMs",
            "CaptureCommandLastQueueLatencyMs",
            "CaptureCommandMaxQueueLatencyMs",
            "CaptureCommandLastCommand",
            "CaptureCommandLastOutcome",
            "CaptureCommandLastCorrelationId",
            "CaptureCommandLastError");
    }
}

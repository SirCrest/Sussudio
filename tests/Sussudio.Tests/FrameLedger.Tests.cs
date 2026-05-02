using System.Reflection;

static partial class Program
{
    private static Task FrameLedger_RetainsBoundedRecentEvents()
    {
        var ledgerType = RequireType("Sussudio.Services.Capture.FrameLedger");
        var identityType = RequireType("Sussudio.Models.FrameIdentity");
        var stageType = RequireType("Sussudio.Models.FrameLedgerStage");
        var ledger = Activator.CreateInstance(
                ledgerType,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                binder: null,
                args: new object[] { 3 },
                culture: null)
            ?? throw new InvalidOperationException("Failed to create FrameLedger.");

        var recordCapture = ledgerType.GetMethod(
                "RecordCaptureArrived",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("FrameLedger.RecordCaptureArrived missing.");
        var recordEvent = ledgerType.GetMethod(
                "RecordEvent",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("FrameLedger.RecordEvent missing.");
        var getSummary = ledgerType.GetMethod(
                "GetSummary",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("FrameLedger.GetSummary missing.");

        for (var i = 0; i < 4; i++)
        {
            var identity = Activator.CreateInstance(
                    identityType,
                    (long)i,
                    1000L + i,
                    null,
                    "MJPG",
                    3840,
                    2160,
                    120.0,
                    1024 + i)
                ?? throw new InvalidOperationException("Failed to create FrameIdentity.");
            recordCapture.Invoke(ledger, new object?[] { identity, "capture" });
        }

        var recordingStage = Enum.Parse(stageType, "RecordingEnqueued");
        recordEvent.Invoke(ledger, new object?[]
        {
            4L,
            recordingStage,
            2000L,
            "recording",
            null,
            null,
            true,
            null
        });

        var summary = getSummary.Invoke(ledger, new object[] { 3 })
                      ?? throw new InvalidOperationException("FrameLedger.GetSummary returned null.");
        AssertEqual(3, GetIntProperty(summary, "Capacity"), "FrameLedger capacity");
        AssertEqual(5L, GetLongProperty(summary, "TotalEventsRecorded"), "FrameLedger total events");
        AssertEqual(2L, GetLongProperty(summary, "EventsDroppedByRetention"), "FrameLedger retained drop count");
        AssertEqual(3, GetIntProperty(summary, "RecentEventCount"), "FrameLedger recent count");
        AssertEqual(2L, Convert.ToInt64(GetPropertyValue(summary, "OldestSourceSequence")), "FrameLedger oldest sequence");
        AssertEqual(4L, Convert.ToInt64(GetPropertyValue(summary, "NewestSourceSequence")), "FrameLedger newest sequence");

        var events = (Array)(GetPropertyValue(summary, "RecentEvents")
                             ?? throw new InvalidOperationException("FrameLedger recent events missing."));
        AssertEqual(3, events.Length, "FrameLedger recent event array length");
        AssertEqual(2L, GetLongProperty(events.GetValue(0)!, "SourceSequence"), "FrameLedger first retained sequence");
        AssertEqual(4L, GetLongProperty(events.GetValue(2)!, "SourceSequence"), "FrameLedger last retained sequence");
        AssertEqual("RecordingEnqueued", GetPropertyValue(events.GetValue(2)!, "Stage")!.ToString(), "FrameLedger last retained stage");
        AssertEqual(true, GetBoolProperty(events.GetValue(2)!, "Accepted"), "FrameLedger accepted state");

        return Task.CompletedTask;
    }

    private static Task FrameLedger_SnapshotContractExposesRecentEvents()
    {
        var captureSnapshotType = RequireType("Sussudio.Models.CaptureRuntimeSnapshot");
        var automationSnapshotType = RequireType("Sussudio.Models.AutomationSnapshot");
        var eventSnapshotType = RequireType("Sussudio.Models.FrameLedgerEventSnapshot");
        var identityType = RequireType("Sussudio.Models.FrameIdentity");

        foreach (var snapshotType in new[] { captureSnapshotType, automationSnapshotType })
        {
            AssertNotNull(snapshotType.GetProperty("FrameLedgerCapacity"), $"{snapshotType.Name}.FrameLedgerCapacity");
            AssertNotNull(snapshotType.GetProperty("FrameLedgerEventCount"), $"{snapshotType.Name}.FrameLedgerEventCount");
            AssertNotNull(snapshotType.GetProperty("FrameLedgerDroppedEventCount"), $"{snapshotType.Name}.FrameLedgerDroppedEventCount");

            var recentEvents = snapshotType.GetProperty("FrameLedgerRecentEvents")
                ?? throw new InvalidOperationException($"{snapshotType.Name}.FrameLedgerRecentEvents missing.");
            AssertEqual(eventSnapshotType.MakeArrayType(), recentEvents.PropertyType, $"{snapshotType.Name}.FrameLedgerRecentEvents type");
        }

        foreach (var prop in new[]
                 {
                     "SourceSequence",
                     "Stage",
                     "QpcTimestamp",
                     "Subsystem",
                     "QueueDepth",
                     "ByteDepth",
                     "Accepted",
                     "Reason",
                     "Identity"
                 })
        {
            AssertNotNull(eventSnapshotType.GetProperty(prop), $"FrameLedgerEventSnapshot.{prop}");
        }

        foreach (var prop in new[]
                 {
                     "SourceSequence",
                     "CaptureArrivalQpc",
                     "DeviceTimestamp100ns",
                     "InputFormat",
                     "Width",
                     "Height",
                     "FrameRateNominal",
                     "CompressedByteLength"
                 })
        {
            AssertNotNull(identityType.GetProperty(prop), $"FrameIdentity.{prop}");
        }

        return Task.CompletedTask;
    }
}

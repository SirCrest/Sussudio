using System;
using System.Reflection;
using System.Threading.Tasks;

static partial class Program
{
    private static Task RecordingIntegritySummary_DefaultsAreExplicit()
    {
        var summaryType = RequireType("Sussudio.Models.RecordingIntegritySummary");
        var notStarted = summaryType.GetProperty("NotStarted", BindingFlags.Public | BindingFlags.Static)?.GetValue(null)
            ?? throw new InvalidOperationException("RecordingIntegritySummary.NotStarted missing.");

        AssertEqual("NotStarted", GetStringProperty(notStarted, "Status"), "RecordingIntegritySummary default status");
        AssertEqual(false, GetBoolProperty(notStarted, "Complete"), "RecordingIntegritySummary default complete");
        AssertEqual("None", GetStringProperty(notStarted, "Backend"), "RecordingIntegritySummary default backend");
        AssertEqual(0L, GetLongProperty(notStarted, "SourceFrames"), "RecordingIntegritySummary default source frames");
        AssertEqual(0L, GetLongProperty(notStarted, "AcceptedFrames"), "RecordingIntegritySummary default accepted frames");
        AssertEqual(0L, GetLongProperty(notStarted, "EncodedFrames"), "RecordingIntegritySummary default encoded frames");
        AssertEqual(0, GetIntProperty(notStarted, "QueueMaxDepth"), "RecordingIntegritySummary default max queue depth");
        AssertEqual("Disabled", GetStringProperty(notStarted, "AudioStatus"), "RecordingIntegritySummary default audio status");
        AssertEqual(false, GetBoolProperty(notStarted, "AudioEnabled"), "RecordingIntegritySummary default audio enabled");
        AssertEqual("No recording has completed.", GetStringProperty(notStarted, "Reason"), "RecordingIntegritySummary default reason");

        return Task.CompletedTask;
    }

    private static Task RecordingIntegritySnapshotContract_ExposesAutomationFields()
    {
        foreach (var typeName in new[]
        {
            "Sussudio.Models.CaptureRuntimeSnapshot",
            "Sussudio.Models.AutomationSnapshot"
        })
        {
            var snapshotType = RequireType(typeName);
            AssertProperty(snapshotType, "RecordingIntegrityStatus", typeof(string));
            AssertProperty(snapshotType, "RecordingIntegrityComplete", typeof(bool));
            AssertProperty(snapshotType, "RecordingIntegrityBackend", typeof(string));
            AssertProperty(snapshotType, "RecordingIntegrityCompletedUtc", typeof(DateTimeOffset?));
            AssertProperty(snapshotType, "RecordingIntegritySourceFrames", typeof(long));
            AssertProperty(snapshotType, "RecordingIntegrityAcceptedFrames", typeof(long));
            AssertProperty(snapshotType, "RecordingIntegrityPipelineDroppedFrames", typeof(long));
            AssertProperty(snapshotType, "RecordingIntegrityQueueDroppedFrames", typeof(long));
            AssertProperty(snapshotType, "RecordingIntegritySubmittedFrames", typeof(long));
            AssertProperty(snapshotType, "RecordingIntegrityEncodedFrames", typeof(long));
            AssertProperty(snapshotType, "RecordingIntegrityPacketsWritten", typeof(long));
            AssertProperty(snapshotType, "RecordingIntegrityEncoderDroppedFrames", typeof(long));
            AssertProperty(snapshotType, "RecordingIntegritySequenceGaps", typeof(long));
            AssertProperty(snapshotType, "RecordingIntegrityQueueMaxDepth", typeof(int));
            AssertProperty(snapshotType, "RecordingIntegrityQueueOldestFrameAgeMs", typeof(long));
            AssertProperty(snapshotType, "RecordingIntegrityBackpressureWaitMs", typeof(long));
            AssertProperty(snapshotType, "RecordingIntegrityBackpressureEvents", typeof(long));
            AssertProperty(snapshotType, "RecordingIntegrityBackpressureMaxWaitMs", typeof(long));
            AssertProperty(snapshotType, "RecordingIntegrityAudioStatus", typeof(string));
            AssertProperty(snapshotType, "RecordingIntegrityAudioEnabled", typeof(bool));
            AssertProperty(snapshotType, "RecordingIntegrityAudioCaptureActive", typeof(bool));
            AssertProperty(snapshotType, "RecordingIntegrityAudioFramesArrived", typeof(long));
            AssertProperty(snapshotType, "RecordingIntegrityAudioFramesWrittenToSink", typeof(long));
            AssertProperty(snapshotType, "RecordingIntegrityAudioSamplesEncoded", typeof(long));
            AssertProperty(snapshotType, "RecordingIntegrityAudioDropEvents", typeof(long));
            AssertProperty(snapshotType, "RecordingIntegrityAudioDiscontinuities", typeof(long));
            AssertProperty(snapshotType, "RecordingIntegrityAudioTimestampErrors", typeof(long));
            AssertProperty(snapshotType, "RecordingIntegrityAudioCallbackGaps", typeof(long));
            AssertProperty(snapshotType, "RecordingIntegrityAvSyncDriftMs", typeof(double?));
            AssertProperty(snapshotType, "RecordingIntegrityAvSyncDriftRateMsPerSec", typeof(double?));
            AssertProperty(snapshotType, "RecordingIntegrityEncoderAvSyncDriftMs", typeof(double?));
            AssertProperty(snapshotType, "RecordingIntegrityEncoderAvSyncCorrectionSamples", typeof(long?));
            AssertProperty(snapshotType, "RecordingIntegrityReason", typeof(string));
        }

        return Task.CompletedTask;
    }

    private static Task RecordingIntegrityAutomationProjection_LivesInFocusedPartial()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Composition.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.cs")
            .Replace("\r\n", "\n");
        var snapshotRecordingIntegrityFlatteningText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.RecordingIntegrity.cs")
            .Replace("\r\n", "\n");
        var recordingProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.RecordingIntegrity.cs")
            .Replace("\r\n", "\n");

        AssertContains(snapshotProjectionText, "var recordingIntegrity = BuildRecordingIntegrityProjection(captureRuntime);");
        AssertContains(snapshotFlatteningText, "var recordingIntegrityFlattening = BuildRecordingIntegrityFlattenedProjection(recordingIntegrity);");
        AssertContains(snapshotFlatteningText, "RecordingIntegrityStatus = recordingIntegrityFlattening.Status,");
        AssertContains(snapshotFlatteningText, "RecordingIntegrityAudioFramesWrittenToSink = recordingIntegrityFlattening.AudioFramesWrittenToSink,");
        AssertContains(snapshotFlatteningText, "RecordingIntegrityEncoderAvSyncDriftMs = recordingIntegrityFlattening.EncoderAvSyncDriftMs,");
        AssertContains(snapshotFlatteningText, "RecordingIntegrityReason = recordingIntegrityFlattening.Reason,");
        AssertDoesNotContain(snapshotFlatteningText, "RecordingIntegrityStatus = captureRuntime.RecordingIntegrityStatus,");
        AssertDoesNotContain(snapshotFlatteningText, "RecordingIntegrityStatus = recordingIntegrity.Status,");
        AssertDoesNotContain(snapshotFlatteningText, "RecordingIntegrityAudioFramesWrittenToSink = captureRuntime.RecordingIntegrityAudioFramesWrittenToSink,");
        AssertDoesNotContain(snapshotFlatteningText, "RecordingIntegrityAudioFramesWrittenToSink = recordingIntegrity.AudioFramesWrittenToSink,");
        AssertDoesNotContain(snapshotFlatteningText, "RecordingIntegrityEncoderAvSyncDriftMs = captureRuntime.RecordingIntegrityEncoderAvSyncDriftMs,");
        AssertDoesNotContain(snapshotFlatteningText, "RecordingIntegrityEncoderAvSyncDriftMs = recordingIntegrity.EncoderAvSyncDriftMs,");
        AssertDoesNotContain(snapshotFlatteningText, "RecordingIntegrityReason = captureRuntime.RecordingIntegrityReason,");
        AssertDoesNotContain(snapshotFlatteningText, "RecordingIntegrityReason = recordingIntegrity.Reason,");

        AssertContains(snapshotRecordingIntegrityFlatteningText, "private static RecordingIntegrityFlattenedProjection BuildRecordingIntegrityFlattenedProjection(");
        AssertContains(snapshotRecordingIntegrityFlatteningText, "Status = recordingIntegrity.Status,");
        AssertContains(snapshotRecordingIntegrityFlatteningText, "AudioFramesWrittenToSink = recordingIntegrity.AudioFramesWrittenToSink,");
        AssertContains(snapshotRecordingIntegrityFlatteningText, "EncoderAvSyncDriftMs = recordingIntegrity.EncoderAvSyncDriftMs,");
        AssertContains(snapshotRecordingIntegrityFlatteningText, "Reason = recordingIntegrity.Reason");
        AssertContains(snapshotRecordingIntegrityFlatteningText, "private readonly record struct RecordingIntegrityFlattenedProjection");

        AssertContains(recordingProjectionText, "private static RecordingIntegrityProjection BuildRecordingIntegrityProjection(CaptureRuntimeSnapshot captureRuntime)");
        AssertContains(recordingProjectionText, "private readonly record struct RecordingIntegrityProjection");
        AssertContains(recordingProjectionText, "Status = captureRuntime.RecordingIntegrityStatus,");
        AssertContains(recordingProjectionText, "AudioFramesWrittenToSink = captureRuntime.RecordingIntegrityAudioFramesWrittenToSink,");
        AssertContains(recordingProjectionText, "EncoderAvSyncDriftMs = captureRuntime.RecordingIntegrityEncoderAvSyncDriftMs,");
        AssertContains(recordingProjectionText, "Reason = captureRuntime.RecordingIntegrityReason");

        return Task.CompletedTask;
    }

    private static void AssertProperty(Type type, string propertyName, Type propertyType)
    {
        var property = type.GetProperty(propertyName)
            ?? throw new InvalidOperationException($"{type.Name}.{propertyName} missing.");
        AssertEqual(propertyType, property.PropertyType, $"{type.Name}.{propertyName} type");
    }
}

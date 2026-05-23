using System;
using System.Reflection;
using System.Threading.Tasks;

static partial class Program
{
    internal static Task RecordingIntegritySummary_DefaultsAreExplicit()
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

    internal static Task RecordingIntegritySnapshotContract_ExposesAutomationFields()
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

    internal static Task RecordingIntegrityAutomationProjection_LivesInFocusedPartial()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningText = ReadAutomationSnapshotFlatteningFamilyText();
        var recordingProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.RecordingIntegrity.cs")
            .Replace("\r\n", "\n");
        AssertContains(snapshotProjectionText, "var recordingIntegrity = BuildRecordingIntegrityProjection(captureRuntime);");
        AssertContains(snapshotFlatteningText, "var recordingIntegrityFlattening = BuildRecordingIntegrityFlattenedProjection(recordingIntegrity);");
        AssertContains(snapshotFlatteningText, "RecordingIntegrityStatus = recordingIntegrityFlattening.Summary.Status,");
        AssertContains(snapshotFlatteningText, "RecordingIntegrityAudioFramesWrittenToSink = recordingIntegrityFlattening.Audio.AudioFramesWrittenToSink,");
        AssertContains(snapshotFlatteningText, "RecordingIntegrityEncoderAvSyncDriftMs = recordingIntegrityFlattening.AvSync.EncoderAvSyncDriftMs,");
        AssertContains(snapshotFlatteningText, "RecordingIntegrityReason = recordingIntegrityFlattening.Summary.Reason,");
        AssertDoesNotContain(snapshotFlatteningText, "RecordingIntegrityStatus = captureRuntime.RecordingIntegrityStatus,");
        AssertDoesNotContain(snapshotFlatteningText, "RecordingIntegrityStatus = recordingIntegrity.Status,");
        AssertDoesNotContain(snapshotFlatteningText, "RecordingIntegrityStatus = recordingIntegrityFlattening.Status,");
        AssertDoesNotContain(snapshotFlatteningText, "RecordingIntegrityAudioFramesWrittenToSink = captureRuntime.RecordingIntegrityAudioFramesWrittenToSink,");
        AssertDoesNotContain(snapshotFlatteningText, "RecordingIntegrityAudioFramesWrittenToSink = recordingIntegrity.AudioFramesWrittenToSink,");
        AssertDoesNotContain(snapshotFlatteningText, "RecordingIntegrityAudioFramesWrittenToSink = recordingIntegrityFlattening.AudioFramesWrittenToSink,");
        AssertDoesNotContain(snapshotFlatteningText, "RecordingIntegrityEncoderAvSyncDriftMs = captureRuntime.RecordingIntegrityEncoderAvSyncDriftMs,");
        AssertDoesNotContain(snapshotFlatteningText, "RecordingIntegrityEncoderAvSyncDriftMs = recordingIntegrity.EncoderAvSyncDriftMs,");
        AssertDoesNotContain(snapshotFlatteningText, "RecordingIntegrityEncoderAvSyncDriftMs = recordingIntegrityFlattening.EncoderAvSyncDriftMs,");
        AssertDoesNotContain(snapshotFlatteningText, "RecordingIntegrityReason = captureRuntime.RecordingIntegrityReason,");
        AssertDoesNotContain(snapshotFlatteningText, "RecordingIntegrityReason = recordingIntegrity.Reason,");
        AssertDoesNotContain(snapshotFlatteningText, "RecordingIntegrityReason = recordingIntegrityFlattening.Reason,");

        AssertContains(recordingProjectionText, "private static RecordingIntegrityFlattenedProjection BuildRecordingIntegrityFlattenedProjection(");
        AssertContains(recordingProjectionText, "Summary = BuildRecordingIntegritySummaryFlattenedProjection(recordingIntegrity.Summary),");
        AssertContains(recordingProjectionText, "Video = BuildRecordingIntegrityVideoFlattenedProjection(recordingIntegrity.Video),");
        AssertContains(recordingProjectionText, "Backpressure = BuildRecordingIntegrityBackpressureFlattenedProjection(recordingIntegrity.Backpressure),");
        AssertContains(recordingProjectionText, "Audio = BuildRecordingIntegrityAudioFlattenedProjection(recordingIntegrity.Audio),");
        AssertContains(recordingProjectionText, "AvSync = BuildRecordingIntegrityAvSyncFlattenedProjection(recordingIntegrity.AvSync)");
        AssertContains(recordingProjectionText, "private readonly record struct RecordingIntegrityFlattenedProjection");
        AssertContains(recordingProjectionText, "private static RecordingIntegritySummaryFlattenedProjection BuildRecordingIntegritySummaryFlattenedProjection(");
        AssertContains(recordingProjectionText, "Status = summary.Status,");
        AssertContains(recordingProjectionText, "Reason = summary.Reason");
        AssertContains(recordingProjectionText, "private static RecordingIntegrityVideoFlattenedProjection BuildRecordingIntegrityVideoFlattenedProjection(");
        AssertContains(recordingProjectionText, "EncodedFrames = video.EncodedFrames,");
        AssertContains(recordingProjectionText, "SequenceGaps = video.SequenceGaps");
        AssertContains(recordingProjectionText, "private static RecordingIntegrityBackpressureFlattenedProjection BuildRecordingIntegrityBackpressureFlattenedProjection(");
        AssertContains(recordingProjectionText, "QueueMaxDepth = backpressure.QueueMaxDepth,");
        AssertContains(recordingProjectionText, "BackpressureMaxWaitMs = backpressure.BackpressureMaxWaitMs");
        AssertContains(recordingProjectionText, "private static RecordingIntegrityAudioFlattenedProjection BuildRecordingIntegrityAudioFlattenedProjection(");
        AssertContains(recordingProjectionText, "AudioFramesWrittenToSink = audio.AudioFramesWrittenToSink,");
        AssertContains(recordingProjectionText, "AudioCallbackGaps = audio.AudioCallbackGaps");
        AssertContains(recordingProjectionText, "private static RecordingIntegrityAvSyncFlattenedProjection BuildRecordingIntegrityAvSyncFlattenedProjection(");
        AssertContains(recordingProjectionText, "EncoderAvSyncDriftMs = avSync.EncoderAvSyncDriftMs,");
        AssertContains(recordingProjectionText, "EncoderAvSyncCorrectionSamples = avSync.EncoderAvSyncCorrectionSamples");

        AssertContains(recordingProjectionText, "private static RecordingIntegrityProjection BuildRecordingIntegrityProjection(CaptureRuntimeSnapshot captureRuntime)");
        AssertContains(recordingProjectionText, "private readonly record struct RecordingIntegrityProjection");
        AssertContains(recordingProjectionText, "Summary = BuildRecordingIntegritySummaryProjection(captureRuntime),");
        AssertContains(recordingProjectionText, "Video = BuildRecordingIntegrityVideoProjection(captureRuntime),");
        AssertContains(recordingProjectionText, "Backpressure = BuildRecordingIntegrityBackpressureProjection(captureRuntime),");
        AssertContains(recordingProjectionText, "Audio = BuildRecordingIntegrityAudioProjection(captureRuntime),");
        AssertContains(recordingProjectionText, "AvSync = BuildRecordingIntegrityAvSyncProjection(captureRuntime)");
        AssertContains(recordingProjectionText, "private static RecordingIntegritySummaryProjection BuildRecordingIntegritySummaryProjection(");
        AssertContains(recordingProjectionText, "Status = captureRuntime.RecordingIntegrityStatus,");
        AssertContains(recordingProjectionText, "Reason = captureRuntime.RecordingIntegrityReason");
        AssertContains(recordingProjectionText, "private static RecordingIntegrityVideoProjection BuildRecordingIntegrityVideoProjection(");
        AssertContains(recordingProjectionText, "EncodedFrames = captureRuntime.RecordingIntegrityEncodedFrames,");
        AssertContains(recordingProjectionText, "SequenceGaps = captureRuntime.RecordingIntegritySequenceGaps");
        AssertContains(recordingProjectionText, "private static RecordingIntegrityBackpressureProjection BuildRecordingIntegrityBackpressureProjection(");
        AssertContains(recordingProjectionText, "QueueMaxDepth = captureRuntime.RecordingIntegrityQueueMaxDepth,");
        AssertContains(recordingProjectionText, "BackpressureMaxWaitMs = captureRuntime.RecordingIntegrityBackpressureMaxWaitMs");
        AssertContains(recordingProjectionText, "private static RecordingIntegrityAudioProjection BuildRecordingIntegrityAudioProjection(");
        AssertContains(recordingProjectionText, "AudioFramesWrittenToSink = captureRuntime.RecordingIntegrityAudioFramesWrittenToSink,");
        AssertContains(recordingProjectionText, "AudioCallbackGaps = captureRuntime.RecordingIntegrityAudioCallbackGaps");
        AssertContains(recordingProjectionText, "private static RecordingIntegrityAvSyncProjection BuildRecordingIntegrityAvSyncProjection(");
        AssertContains(recordingProjectionText, "EncoderAvSyncDriftMs = captureRuntime.RecordingIntegrityEncoderAvSyncDriftMs,");
        AssertContains(recordingProjectionText, "EncoderAvSyncCorrectionSamples = captureRuntime.RecordingIntegrityEncoderAvSyncCorrectionSamples");

        return Task.CompletedTask;
    }

    private static void AssertProperty(Type type, string propertyName, Type propertyType)
    {
        var property = type.GetProperty(propertyName)
            ?? throw new InvalidOperationException($"{type.Name}.{propertyName} missing.");
        AssertEqual(propertyType, property.PropertyType, $"{type.Name}.{propertyName} type");
    }
}

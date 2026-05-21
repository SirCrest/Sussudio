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
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Composition.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningText = ReadAutomationSnapshotFlatteningFamilyText();
        var snapshotRecordingIntegrityFlatteningText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.RecordingIntegrity.cs")
            .Replace("\r\n", "\n");
        var snapshotRecordingIntegritySummaryFlatteningText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.RecordingIntegrity.Summary.cs")
            .Replace("\r\n", "\n");
        var snapshotRecordingIntegrityVideoFlatteningText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.RecordingIntegrity.Video.cs")
            .Replace("\r\n", "\n");
        var snapshotRecordingIntegrityBackpressureFlatteningText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.RecordingIntegrity.Backpressure.cs")
            .Replace("\r\n", "\n");
        var snapshotRecordingIntegrityAudioFlatteningText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.RecordingIntegrity.Audio.cs")
            .Replace("\r\n", "\n");
        var snapshotRecordingIntegrityAvSyncFlatteningText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.RecordingIntegrity.AvSync.cs")
            .Replace("\r\n", "\n");
        var recordingProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.RecordingIntegrity.cs")
            .Replace("\r\n", "\n");
        var recordingSummaryProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.RecordingIntegrity.Summary.cs")
            .Replace("\r\n", "\n");
        var recordingVideoProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.RecordingIntegrity.Video.cs")
            .Replace("\r\n", "\n");
        var recordingBackpressureProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.RecordingIntegrity.Backpressure.cs")
            .Replace("\r\n", "\n");
        var recordingAudioProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.RecordingIntegrity.Audio.cs")
            .Replace("\r\n", "\n");
        var recordingAvSyncProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.RecordingIntegrity.AvSync.cs")
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

        AssertContains(snapshotRecordingIntegrityFlatteningText, "private static RecordingIntegrityFlattenedProjection BuildRecordingIntegrityFlattenedProjection(");
        AssertContains(snapshotRecordingIntegrityFlatteningText, "Summary = BuildRecordingIntegritySummaryFlattenedProjection(recordingIntegrity.Summary),");
        AssertContains(snapshotRecordingIntegrityFlatteningText, "Video = BuildRecordingIntegrityVideoFlattenedProjection(recordingIntegrity.Video),");
        AssertContains(snapshotRecordingIntegrityFlatteningText, "Backpressure = BuildRecordingIntegrityBackpressureFlattenedProjection(recordingIntegrity.Backpressure),");
        AssertContains(snapshotRecordingIntegrityFlatteningText, "Audio = BuildRecordingIntegrityAudioFlattenedProjection(recordingIntegrity.Audio),");
        AssertContains(snapshotRecordingIntegrityFlatteningText, "AvSync = BuildRecordingIntegrityAvSyncFlattenedProjection(recordingIntegrity.AvSync)");
        AssertContains(snapshotRecordingIntegrityFlatteningText, "private readonly record struct RecordingIntegrityFlattenedProjection");
        AssertContains(snapshotRecordingIntegritySummaryFlatteningText, "private static RecordingIntegritySummaryFlattenedProjection BuildRecordingIntegritySummaryFlattenedProjection(");
        AssertContains(snapshotRecordingIntegritySummaryFlatteningText, "Status = summary.Status,");
        AssertContains(snapshotRecordingIntegritySummaryFlatteningText, "Reason = summary.Reason");
        AssertContains(snapshotRecordingIntegrityVideoFlatteningText, "private static RecordingIntegrityVideoFlattenedProjection BuildRecordingIntegrityVideoFlattenedProjection(");
        AssertContains(snapshotRecordingIntegrityVideoFlatteningText, "EncodedFrames = video.EncodedFrames,");
        AssertContains(snapshotRecordingIntegrityVideoFlatteningText, "SequenceGaps = video.SequenceGaps");
        AssertContains(snapshotRecordingIntegrityBackpressureFlatteningText, "private static RecordingIntegrityBackpressureFlattenedProjection BuildRecordingIntegrityBackpressureFlattenedProjection(");
        AssertContains(snapshotRecordingIntegrityBackpressureFlatteningText, "QueueMaxDepth = backpressure.QueueMaxDepth,");
        AssertContains(snapshotRecordingIntegrityBackpressureFlatteningText, "BackpressureMaxWaitMs = backpressure.BackpressureMaxWaitMs");
        AssertContains(snapshotRecordingIntegrityAudioFlatteningText, "private static RecordingIntegrityAudioFlattenedProjection BuildRecordingIntegrityAudioFlattenedProjection(");
        AssertContains(snapshotRecordingIntegrityAudioFlatteningText, "AudioFramesWrittenToSink = audio.AudioFramesWrittenToSink,");
        AssertContains(snapshotRecordingIntegrityAudioFlatteningText, "AudioCallbackGaps = audio.AudioCallbackGaps");
        AssertContains(snapshotRecordingIntegrityAvSyncFlatteningText, "private static RecordingIntegrityAvSyncFlattenedProjection BuildRecordingIntegrityAvSyncFlattenedProjection(");
        AssertContains(snapshotRecordingIntegrityAvSyncFlatteningText, "EncoderAvSyncDriftMs = avSync.EncoderAvSyncDriftMs,");
        AssertContains(snapshotRecordingIntegrityAvSyncFlatteningText, "EncoderAvSyncCorrectionSamples = avSync.EncoderAvSyncCorrectionSamples");

        AssertContains(recordingProjectionText, "private static RecordingIntegrityProjection BuildRecordingIntegrityProjection(CaptureRuntimeSnapshot captureRuntime)");
        AssertContains(recordingProjectionText, "private readonly record struct RecordingIntegrityProjection");
        AssertContains(recordingProjectionText, "Summary = BuildRecordingIntegritySummaryProjection(captureRuntime),");
        AssertContains(recordingProjectionText, "Video = BuildRecordingIntegrityVideoProjection(captureRuntime),");
        AssertContains(recordingProjectionText, "Backpressure = BuildRecordingIntegrityBackpressureProjection(captureRuntime),");
        AssertContains(recordingProjectionText, "Audio = BuildRecordingIntegrityAudioProjection(captureRuntime),");
        AssertContains(recordingProjectionText, "AvSync = BuildRecordingIntegrityAvSyncProjection(captureRuntime)");
        AssertContains(recordingSummaryProjectionText, "private static RecordingIntegritySummaryProjection BuildRecordingIntegritySummaryProjection(");
        AssertContains(recordingSummaryProjectionText, "Status = captureRuntime.RecordingIntegrityStatus,");
        AssertContains(recordingSummaryProjectionText, "Reason = captureRuntime.RecordingIntegrityReason");
        AssertContains(recordingVideoProjectionText, "private static RecordingIntegrityVideoProjection BuildRecordingIntegrityVideoProjection(");
        AssertContains(recordingVideoProjectionText, "EncodedFrames = captureRuntime.RecordingIntegrityEncodedFrames,");
        AssertContains(recordingVideoProjectionText, "SequenceGaps = captureRuntime.RecordingIntegritySequenceGaps");
        AssertContains(recordingBackpressureProjectionText, "private static RecordingIntegrityBackpressureProjection BuildRecordingIntegrityBackpressureProjection(");
        AssertContains(recordingBackpressureProjectionText, "QueueMaxDepth = captureRuntime.RecordingIntegrityQueueMaxDepth,");
        AssertContains(recordingBackpressureProjectionText, "BackpressureMaxWaitMs = captureRuntime.RecordingIntegrityBackpressureMaxWaitMs");
        AssertContains(recordingAudioProjectionText, "private static RecordingIntegrityAudioProjection BuildRecordingIntegrityAudioProjection(");
        AssertContains(recordingAudioProjectionText, "AudioFramesWrittenToSink = captureRuntime.RecordingIntegrityAudioFramesWrittenToSink,");
        AssertContains(recordingAudioProjectionText, "AudioCallbackGaps = captureRuntime.RecordingIntegrityAudioCallbackGaps");
        AssertContains(recordingAvSyncProjectionText, "private static RecordingIntegrityAvSyncProjection BuildRecordingIntegrityAvSyncProjection(");
        AssertContains(recordingAvSyncProjectionText, "EncoderAvSyncDriftMs = captureRuntime.RecordingIntegrityEncoderAvSyncDriftMs,");
        AssertContains(recordingAvSyncProjectionText, "EncoderAvSyncCorrectionSamples = captureRuntime.RecordingIntegrityEncoderAvSyncCorrectionSamples");

        return Task.CompletedTask;
    }

    private static void AssertProperty(Type type, string propertyName, Type propertyType)
    {
        var property = type.GetProperty(propertyName)
            ?? throw new InvalidOperationException($"{type.Name}.{propertyName} missing.");
        AssertEqual(propertyType, property.PropertyType, $"{type.Name}.{propertyName} type");
    }
}

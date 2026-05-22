using System;
using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static RecordingIntegrityProjection BuildRecordingIntegrityProjection(CaptureRuntimeSnapshot captureRuntime)
        => new()
        {
            Summary = BuildRecordingIntegritySummaryProjection(captureRuntime),
            Video = BuildRecordingIntegrityVideoProjection(captureRuntime),
            Backpressure = BuildRecordingIntegrityBackpressureProjection(captureRuntime),
            Audio = BuildRecordingIntegrityAudioProjection(captureRuntime),
            AvSync = BuildRecordingIntegrityAvSyncProjection(captureRuntime)
        };

    private static RecordingIntegrityFlattenedProjection BuildRecordingIntegrityFlattenedProjection(
        RecordingIntegrityProjection recordingIntegrity)
        => new()
        {
            Summary = BuildRecordingIntegritySummaryFlattenedProjection(recordingIntegrity.Summary),
            Video = BuildRecordingIntegrityVideoFlattenedProjection(recordingIntegrity.Video),
            Backpressure = BuildRecordingIntegrityBackpressureFlattenedProjection(recordingIntegrity.Backpressure),
            Audio = BuildRecordingIntegrityAudioFlattenedProjection(recordingIntegrity.Audio),
            AvSync = BuildRecordingIntegrityAvSyncFlattenedProjection(recordingIntegrity.AvSync)
        };

    private readonly record struct RecordingIntegrityProjection
    {
        public RecordingIntegritySummaryProjection Summary { get; init; }
        public RecordingIntegrityVideoProjection Video { get; init; }
        public RecordingIntegrityBackpressureProjection Backpressure { get; init; }
        public RecordingIntegrityAudioProjection Audio { get; init; }
        public RecordingIntegrityAvSyncProjection AvSync { get; init; }
    }

    private static RecordingIntegritySummaryProjection BuildRecordingIntegritySummaryProjection(
        CaptureRuntimeSnapshot captureRuntime)
        => new()
        {
            Status = captureRuntime.RecordingIntegrityStatus,
            Complete = captureRuntime.RecordingIntegrityComplete,
            Backend = captureRuntime.RecordingIntegrityBackend,
            CompletedUtc = captureRuntime.RecordingIntegrityCompletedUtc,
            Reason = captureRuntime.RecordingIntegrityReason
        };

    private static RecordingIntegritySummaryFlattenedProjection BuildRecordingIntegritySummaryFlattenedProjection(
        RecordingIntegritySummaryProjection summary)
        => new()
        {
            Status = summary.Status,
            Complete = summary.Complete,
            Backend = summary.Backend,
            CompletedUtc = summary.CompletedUtc,
            Reason = summary.Reason
        };

    private readonly record struct RecordingIntegritySummaryProjection
    {
        public string Status { get; init; }
        public bool Complete { get; init; }
        public string Backend { get; init; }
        public DateTimeOffset? CompletedUtc { get; init; }
        public string Reason { get; init; }
    }

    private readonly record struct RecordingIntegritySummaryFlattenedProjection
    {
        public string Status { get; init; }
        public bool Complete { get; init; }
        public string Backend { get; init; }
        public DateTimeOffset? CompletedUtc { get; init; }
        public string Reason { get; init; }
    }

    private static RecordingIntegrityVideoProjection BuildRecordingIntegrityVideoProjection(
        CaptureRuntimeSnapshot captureRuntime)
        => new()
        {
            SourceFrames = captureRuntime.RecordingIntegritySourceFrames,
            AcceptedFrames = captureRuntime.RecordingIntegrityAcceptedFrames,
            PipelineDroppedFrames = captureRuntime.RecordingIntegrityPipelineDroppedFrames,
            QueueDroppedFrames = captureRuntime.RecordingIntegrityQueueDroppedFrames,
            SubmittedFrames = captureRuntime.RecordingIntegritySubmittedFrames,
            EncodedFrames = captureRuntime.RecordingIntegrityEncodedFrames,
            PacketsWritten = captureRuntime.RecordingIntegrityPacketsWritten,
            EncoderDroppedFrames = captureRuntime.RecordingIntegrityEncoderDroppedFrames,
            SequenceGaps = captureRuntime.RecordingIntegritySequenceGaps
        };

    private static RecordingIntegrityVideoFlattenedProjection BuildRecordingIntegrityVideoFlattenedProjection(
        RecordingIntegrityVideoProjection video)
        => new()
        {
            SourceFrames = video.SourceFrames,
            AcceptedFrames = video.AcceptedFrames,
            PipelineDroppedFrames = video.PipelineDroppedFrames,
            QueueDroppedFrames = video.QueueDroppedFrames,
            SubmittedFrames = video.SubmittedFrames,
            EncodedFrames = video.EncodedFrames,
            PacketsWritten = video.PacketsWritten,
            EncoderDroppedFrames = video.EncoderDroppedFrames,
            SequenceGaps = video.SequenceGaps
        };

    private readonly record struct RecordingIntegrityVideoProjection
    {
        public long SourceFrames { get; init; }
        public long AcceptedFrames { get; init; }
        public long PipelineDroppedFrames { get; init; }
        public long QueueDroppedFrames { get; init; }
        public long SubmittedFrames { get; init; }
        public long EncodedFrames { get; init; }
        public long PacketsWritten { get; init; }
        public long EncoderDroppedFrames { get; init; }
        public long SequenceGaps { get; init; }
    }

    private readonly record struct RecordingIntegrityVideoFlattenedProjection
    {
        public long SourceFrames { get; init; }
        public long AcceptedFrames { get; init; }
        public long PipelineDroppedFrames { get; init; }
        public long QueueDroppedFrames { get; init; }
        public long SubmittedFrames { get; init; }
        public long EncodedFrames { get; init; }
        public long PacketsWritten { get; init; }
        public long EncoderDroppedFrames { get; init; }
        public long SequenceGaps { get; init; }
    }

    private static RecordingIntegrityBackpressureProjection BuildRecordingIntegrityBackpressureProjection(
        CaptureRuntimeSnapshot captureRuntime)
        => new()
        {
            QueueMaxDepth = captureRuntime.RecordingIntegrityQueueMaxDepth,
            QueueOldestFrameAgeMs = captureRuntime.RecordingIntegrityQueueOldestFrameAgeMs,
            BackpressureWaitMs = captureRuntime.RecordingIntegrityBackpressureWaitMs,
            BackpressureEvents = captureRuntime.RecordingIntegrityBackpressureEvents,
            BackpressureMaxWaitMs = captureRuntime.RecordingIntegrityBackpressureMaxWaitMs
        };

    private static RecordingIntegrityBackpressureFlattenedProjection BuildRecordingIntegrityBackpressureFlattenedProjection(
        RecordingIntegrityBackpressureProjection backpressure)
        => new()
        {
            QueueMaxDepth = backpressure.QueueMaxDepth,
            QueueOldestFrameAgeMs = backpressure.QueueOldestFrameAgeMs,
            BackpressureWaitMs = backpressure.BackpressureWaitMs,
            BackpressureEvents = backpressure.BackpressureEvents,
            BackpressureMaxWaitMs = backpressure.BackpressureMaxWaitMs
        };

    private readonly record struct RecordingIntegrityBackpressureProjection
    {
        public int QueueMaxDepth { get; init; }
        public long QueueOldestFrameAgeMs { get; init; }
        public long BackpressureWaitMs { get; init; }
        public long BackpressureEvents { get; init; }
        public long BackpressureMaxWaitMs { get; init; }
    }

    private readonly record struct RecordingIntegrityBackpressureFlattenedProjection
    {
        public int QueueMaxDepth { get; init; }
        public long QueueOldestFrameAgeMs { get; init; }
        public long BackpressureWaitMs { get; init; }
        public long BackpressureEvents { get; init; }
        public long BackpressureMaxWaitMs { get; init; }
    }

    private static RecordingIntegrityAudioProjection BuildRecordingIntegrityAudioProjection(
        CaptureRuntimeSnapshot captureRuntime)
        => new()
        {
            AudioStatus = captureRuntime.RecordingIntegrityAudioStatus,
            AudioEnabled = captureRuntime.RecordingIntegrityAudioEnabled,
            AudioCaptureActive = captureRuntime.RecordingIntegrityAudioCaptureActive,
            AudioFramesArrived = captureRuntime.RecordingIntegrityAudioFramesArrived,
            AudioFramesWrittenToSink = captureRuntime.RecordingIntegrityAudioFramesWrittenToSink,
            AudioSamplesEncoded = captureRuntime.RecordingIntegrityAudioSamplesEncoded,
            AudioDropEvents = captureRuntime.RecordingIntegrityAudioDropEvents,
            AudioDiscontinuities = captureRuntime.RecordingIntegrityAudioDiscontinuities,
            AudioTimestampErrors = captureRuntime.RecordingIntegrityAudioTimestampErrors,
            AudioCallbackGaps = captureRuntime.RecordingIntegrityAudioCallbackGaps
        };

    private static RecordingIntegrityAudioFlattenedProjection BuildRecordingIntegrityAudioFlattenedProjection(
        RecordingIntegrityAudioProjection audio)
        => new()
        {
            AudioStatus = audio.AudioStatus,
            AudioEnabled = audio.AudioEnabled,
            AudioCaptureActive = audio.AudioCaptureActive,
            AudioFramesArrived = audio.AudioFramesArrived,
            AudioFramesWrittenToSink = audio.AudioFramesWrittenToSink,
            AudioSamplesEncoded = audio.AudioSamplesEncoded,
            AudioDropEvents = audio.AudioDropEvents,
            AudioDiscontinuities = audio.AudioDiscontinuities,
            AudioTimestampErrors = audio.AudioTimestampErrors,
            AudioCallbackGaps = audio.AudioCallbackGaps
        };

    private readonly record struct RecordingIntegrityAudioProjection
    {
        public string AudioStatus { get; init; }
        public bool AudioEnabled { get; init; }
        public bool AudioCaptureActive { get; init; }
        public long AudioFramesArrived { get; init; }
        public long AudioFramesWrittenToSink { get; init; }
        public long AudioSamplesEncoded { get; init; }
        public long AudioDropEvents { get; init; }
        public long AudioDiscontinuities { get; init; }
        public long AudioTimestampErrors { get; init; }
        public long AudioCallbackGaps { get; init; }
    }

    private readonly record struct RecordingIntegrityAudioFlattenedProjection
    {
        public string AudioStatus { get; init; }
        public bool AudioEnabled { get; init; }
        public bool AudioCaptureActive { get; init; }
        public long AudioFramesArrived { get; init; }
        public long AudioFramesWrittenToSink { get; init; }
        public long AudioSamplesEncoded { get; init; }
        public long AudioDropEvents { get; init; }
        public long AudioDiscontinuities { get; init; }
        public long AudioTimestampErrors { get; init; }
        public long AudioCallbackGaps { get; init; }
    }

    private static RecordingIntegrityAvSyncProjection BuildRecordingIntegrityAvSyncProjection(
        CaptureRuntimeSnapshot captureRuntime)
        => new()
        {
            AvSyncDriftMs = captureRuntime.RecordingIntegrityAvSyncDriftMs,
            AvSyncDriftRateMsPerSec = captureRuntime.RecordingIntegrityAvSyncDriftRateMsPerSec,
            EncoderAvSyncDriftMs = captureRuntime.RecordingIntegrityEncoderAvSyncDriftMs,
            EncoderAvSyncCorrectionSamples = captureRuntime.RecordingIntegrityEncoderAvSyncCorrectionSamples
        };

    private static RecordingIntegrityAvSyncFlattenedProjection BuildRecordingIntegrityAvSyncFlattenedProjection(
        RecordingIntegrityAvSyncProjection avSync)
        => new()
        {
            AvSyncDriftMs = avSync.AvSyncDriftMs,
            AvSyncDriftRateMsPerSec = avSync.AvSyncDriftRateMsPerSec,
            EncoderAvSyncDriftMs = avSync.EncoderAvSyncDriftMs,
            EncoderAvSyncCorrectionSamples = avSync.EncoderAvSyncCorrectionSamples
        };

    private readonly record struct RecordingIntegrityAvSyncProjection
    {
        public double? AvSyncDriftMs { get; init; }
        public double? AvSyncDriftRateMsPerSec { get; init; }
        public double? EncoderAvSyncDriftMs { get; init; }
        public long? EncoderAvSyncCorrectionSamples { get; init; }
    }

    private readonly record struct RecordingIntegrityAvSyncFlattenedProjection
    {
        public double? AvSyncDriftMs { get; init; }
        public double? AvSyncDriftRateMsPerSec { get; init; }
        public double? EncoderAvSyncDriftMs { get; init; }
        public long? EncoderAvSyncCorrectionSamples { get; init; }
    }

    private readonly record struct RecordingIntegrityFlattenedProjection
    {
        public RecordingIntegritySummaryFlattenedProjection Summary { get; init; }
        public RecordingIntegrityVideoFlattenedProjection Video { get; init; }
        public RecordingIntegrityBackpressureFlattenedProjection Backpressure { get; init; }
        public RecordingIntegrityAudioFlattenedProjection Audio { get; init; }
        public RecordingIntegrityAvSyncFlattenedProjection AvSync { get; init; }
    }
}

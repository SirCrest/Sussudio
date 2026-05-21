using System;

namespace Sussudio.Services.Capture;

public partial class CaptureService
{
    private readonly record struct RecordingIntegritySummaryVideoFields
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

        public int QueueMaxDepth { get; init; }

        public long QueueOldestFrameAgeMs { get; init; }

        public long BackpressureWaitMs { get; init; }

        public long BackpressureEvents { get; init; }

        public long BackpressureMaxWaitMs { get; init; }

        public bool EncodingFailed { get; init; }

        public string? EncodingFailureType { get; init; }

        public string? EncodingFailureMessage { get; init; }
    }

    private readonly record struct RecordingIntegritySummaryAudioFields
    {
        public bool AudioEnabled { get; init; }

        public bool AudioCaptureActive { get; init; }

        public long AudioFramesArrived { get; init; }

        public long AudioFramesWrittenToSink { get; init; }

        public long AudioSamplesEncoded { get; init; }

        public long AudioDropEvents { get; init; }

        public long AudioDiscontinuities { get; init; }

        public long AudioTimestampErrors { get; init; }

        public long AudioCallbackGaps { get; init; }

        public double? AvSyncDriftMs { get; init; }

        public double? AvSyncDriftRateMsPerSec { get; init; }

        public double? EncoderAvSyncDriftMs { get; init; }

        public long? EncoderAvSyncCorrectionSamples { get; init; }
    }

    private static RecordingIntegritySummaryVideoFields BuildRecordingIntegritySummaryVideoFields(
        bool recordingActive,
        long sourceFrames,
        long acceptedFrames,
        RecordingIntegrityCounterSnapshot counters)
    {
        var normalizedSourceFrames = Math.Max(0, sourceFrames);
        var normalizedAcceptedFrames = Math.Max(0, acceptedFrames);
        var rawPipelineDroppedFrames = Math.Max(0, normalizedSourceFrames - normalizedAcceptedFrames);

        return new RecordingIntegritySummaryVideoFields
        {
            SourceFrames = normalizedSourceFrames,
            AcceptedFrames = normalizedAcceptedFrames,
            PipelineDroppedFrames = recordingActive
                ? Math.Max(0, rawPipelineDroppedFrames - 1)
                : rawPipelineDroppedFrames,
            QueueDroppedFrames = Math.Max(0, counters.QueueDroppedFrames),
            SubmittedFrames = Math.Max(0, counters.SubmittedFrames),
            EncodedFrames = Math.Max(0, counters.EncodedFrames),
            PacketsWritten = Math.Max(0, counters.PacketsWritten),
            EncoderDroppedFrames = Math.Max(0, counters.EncoderDroppedFrames),
            SequenceGaps = Math.Max(0, counters.SequenceGaps),
            QueueMaxDepth = Math.Max(0, counters.QueueMaxDepth),
            QueueOldestFrameAgeMs = Math.Max(0, counters.QueueOldestFrameAgeMs),
            BackpressureWaitMs = Math.Max(0, counters.BackpressureWaitMs),
            BackpressureEvents = Math.Max(0, counters.BackpressureEvents),
            BackpressureMaxWaitMs = Math.Max(0, counters.BackpressureMaxWaitMs),
            EncodingFailed = counters.EncodingFailed,
            EncodingFailureType = counters.EncodingFailureType,
            EncodingFailureMessage = counters.EncodingFailureMessage
        };
    }

    private static RecordingIntegritySummaryAudioFields BuildRecordingIntegritySummaryAudioFields(
        RecordingAudioIntegrityCounterSnapshot audioCounters)
        => new()
        {
            AudioEnabled = audioCounters.AudioEnabled,
            AudioCaptureActive = audioCounters.AudioCaptureActive,
            AudioFramesArrived = Math.Max(0, audioCounters.AudioFramesArrived),
            AudioFramesWrittenToSink = Math.Max(0, audioCounters.AudioFramesWrittenToSink),
            AudioSamplesEncoded = Math.Max(0, audioCounters.AudioSamplesEncoded),
            AudioDropEvents = Math.Max(0, audioCounters.AudioDropEvents),
            AudioDiscontinuities = Math.Max(0, audioCounters.AudioDiscontinuities),
            AudioTimestampErrors = Math.Max(0, audioCounters.AudioTimestampErrors),
            AudioCallbackGaps = Math.Max(0, audioCounters.AudioCallbackGaps),
            AvSyncDriftMs = audioCounters.AvSyncDriftMs,
            AvSyncDriftRateMsPerSec = audioCounters.AvSyncDriftRateMsPerSec,
            EncoderAvSyncDriftMs = audioCounters.EncoderAvSyncDriftMs,
            EncoderAvSyncCorrectionSamples = audioCounters.EncoderAvSyncCorrectionSamples
        };
}

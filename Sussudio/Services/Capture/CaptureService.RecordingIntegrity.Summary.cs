using System;
using System.Globalization;
using Sussudio.Models;

namespace Sussudio.Services.Capture;

public partial class CaptureService
{
    private static RecordingIntegritySummary BuildRecordingIntegritySummary(
        string backend,
        bool recordingActive,
        bool finalizeSucceeded,
        string finalizeStatus,
        DateTimeOffset? completedUtc,
        long sourceFrames,
        long acceptedFrames,
        RecordingIntegrityCounterSnapshot counters,
        RecordingAudioIntegrityCounterSnapshot? audioCounters = null)
    {
        audioCounters ??= RecordingAudioIntegrityCounterSnapshot.Disabled;
        var videoFields = BuildRecordingIntegritySummaryVideoFields(
            recordingActive,
            sourceFrames,
            acceptedFrames,
            counters);
        var audioFields = BuildRecordingIntegritySummaryAudioFields(audioCounters);
        var evaluation = EvaluateRecordingIntegritySummary(
            recordingActive,
            finalizeSucceeded,
            finalizeStatus,
            videoFields,
            audioFields);

        return new RecordingIntegritySummary
        {
            Status = evaluation.Status,
            Complete = !recordingActive && string.Equals(evaluation.Status, "Complete", StringComparison.Ordinal),
            Backend = backend,
            CompletedUtc = completedUtc,
            SourceFrames = videoFields.SourceFrames,
            AcceptedFrames = videoFields.AcceptedFrames,
            PipelineDroppedFrames = videoFields.PipelineDroppedFrames,
            QueueDroppedFrames = videoFields.QueueDroppedFrames,
            SubmittedFrames = videoFields.SubmittedFrames,
            EncodedFrames = videoFields.EncodedFrames,
            PacketsWritten = videoFields.PacketsWritten,
            EncoderDroppedFrames = videoFields.EncoderDroppedFrames,
            SequenceGaps = videoFields.SequenceGaps,
            QueueMaxDepth = videoFields.QueueMaxDepth,
            QueueOldestFrameAgeMs = videoFields.QueueOldestFrameAgeMs,
            BackpressureWaitMs = videoFields.BackpressureWaitMs,
            BackpressureEvents = videoFields.BackpressureEvents,
            BackpressureMaxWaitMs = videoFields.BackpressureMaxWaitMs,
            AudioStatus = evaluation.AudioStatus,
            AudioEnabled = audioFields.AudioEnabled,
            AudioCaptureActive = audioFields.AudioCaptureActive,
            AudioFramesArrived = audioFields.AudioFramesArrived,
            AudioFramesWrittenToSink = audioFields.AudioFramesWrittenToSink,
            AudioSamplesEncoded = audioFields.AudioSamplesEncoded,
            AudioDropEvents = audioFields.AudioDropEvents,
            AudioDiscontinuities = audioFields.AudioDiscontinuities,
            AudioTimestampErrors = audioFields.AudioTimestampErrors,
            AudioCallbackGaps = audioFields.AudioCallbackGaps,
            AvSyncDriftMs = audioFields.AvSyncDriftMs,
            AvSyncDriftRateMsPerSec = audioFields.AvSyncDriftRateMsPerSec,
            EncoderAvSyncDriftMs = audioFields.EncoderAvSyncDriftMs,
            EncoderAvSyncCorrectionSamples = audioFields.EncoderAvSyncCorrectionSamples,
            Reason = evaluation.Reason
        };
    }

    private static void LogRecordingIntegritySummary(RecordingIntegritySummary summary)
    {
        Logger.Log(
            "RECORDING_INTEGRITY " +
            $"status={summary.Status} " +
            $"complete={summary.Complete} " +
            $"backend={summary.Backend} " +
            $"source_frames={summary.SourceFrames} " +
            $"accepted_frames={summary.AcceptedFrames} " +
            $"pipeline_drops={summary.PipelineDroppedFrames} " +
            $"queue_drops={summary.QueueDroppedFrames} " +
            $"submitted_frames={summary.SubmittedFrames} " +
            $"encoded_frames={summary.EncodedFrames} " +
            $"packets_written={summary.PacketsWritten} " +
            $"encoder_drops={summary.EncoderDroppedFrames} " +
            $"sequence_gaps={summary.SequenceGaps} " +
            $"queue_max_depth={summary.QueueMaxDepth} " +
            $"queue_oldest_age_ms={summary.QueueOldestFrameAgeMs} " +
            $"backpressure_wait_ms={summary.BackpressureWaitMs} " +
            $"backpressure_events={summary.BackpressureEvents} " +
            $"backpressure_max_wait_ms={summary.BackpressureMaxWaitMs} " +
            $"audio_status={summary.AudioStatus} " +
            $"audio_enabled={summary.AudioEnabled} " +
            $"audio_active={summary.AudioCaptureActive} " +
            $"audio_arrived={summary.AudioFramesArrived} " +
            $"audio_written={summary.AudioFramesWrittenToSink} " +
            $"audio_encoded={summary.AudioSamplesEncoded} " +
            $"audio_drops={summary.AudioDropEvents} " +
            $"audio_discontinuities={summary.AudioDiscontinuities} " +
            $"audio_timestamp_errors={summary.AudioTimestampErrors} " +
            $"audio_callback_gaps={summary.AudioCallbackGaps} " +
            $"av_drift_ms={summary.AvSyncDriftMs?.ToString("0.###", CultureInfo.InvariantCulture) ?? "N/A"} " +
            $"encoder_av_drift_ms={summary.EncoderAvSyncDriftMs?.ToString("0.###", CultureInfo.InvariantCulture) ?? "N/A"} " +
            $"reason='{summary.Reason.Replace("'", "\\'", StringComparison.Ordinal)}'");
    }
}

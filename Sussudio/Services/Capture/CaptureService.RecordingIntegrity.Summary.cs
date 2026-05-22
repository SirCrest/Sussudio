using System;
using System.Collections.Generic;
using System.Globalization;
using Sussudio.Models;

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

    private readonly record struct RecordingIntegritySummaryEvaluation(
        string Status,
        string AudioStatus,
        string Reason);

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

    private static RecordingIntegritySummaryEvaluation EvaluateRecordingIntegritySummary(
        bool recordingActive,
        bool finalizeSucceeded,
        string finalizeStatus,
        RecordingIntegritySummaryVideoFields videoFields,
        RecordingIntegritySummaryAudioFields audioFields)
    {
        var reasons = new List<string>();
        if (!recordingActive && !finalizeSucceeded)
        {
            reasons.Add($"finalize='{finalizeStatus}'");
        }

        if (videoFields.EncodingFailed)
        {
            var failure = string.IsNullOrWhiteSpace(videoFields.EncodingFailureMessage)
                ? videoFields.EncodingFailureType ?? "unknown"
                : $"{videoFields.EncodingFailureType ?? "unknown"}: {videoFields.EncodingFailureMessage}";
            reasons.Add($"encoding={failure}");
        }

        if (videoFields.PipelineDroppedFrames > 0)
        {
            reasons.Add($"pipeline_drops={videoFields.PipelineDroppedFrames}");
        }

        if (videoFields.QueueDroppedFrames > 0)
        {
            reasons.Add($"queue_drops={videoFields.QueueDroppedFrames}");
        }

        if (videoFields.EncoderDroppedFrames > 0)
        {
            reasons.Add($"encoder_drops={videoFields.EncoderDroppedFrames}");
        }

        if (videoFields.SequenceGaps > 0)
        {
            reasons.Add($"sequence_gaps={videoFields.SequenceGaps}");
        }

        var audioStatus = EvaluateRecordingIntegrityAudioStatus(audioFields, reasons);
        var status = reasons.Count > 0
            ? (videoFields.EncodingFailed ||
               (!recordingActive && !finalizeSucceeded) ||
               string.Equals(audioStatus, "Failed", StringComparison.Ordinal)
                ? "Failed"
                : "Incomplete")
            : recordingActive ? "Active" : "Complete";
        var reason = reasons.Count > 0
            ? string.Join("; ", reasons)
            : recordingActive
                ? "Recording active; all delivered source frames have reached the recording boundary so far."
                : "Every delivered source frame reached the recording boundary.";

        return new RecordingIntegritySummaryEvaluation(status, audioStatus, reason);
    }

    private static string EvaluateRecordingIntegrityAudioStatus(
        RecordingIntegritySummaryAudioFields audioFields,
        List<string> reasons)
    {
        if (!audioFields.AudioEnabled)
        {
            return "Disabled";
        }

        var audioFailed = false;
        var audioIncomplete = false;
        if (!audioFields.AudioCaptureActive)
        {
            audioFailed = true;
            reasons.Add("audio_inactive");
        }

        if (audioFields.AudioFramesArrived <= 0)
        {
            audioFailed = true;
            reasons.Add("audio_no_frames");
        }

        var audioBoundaryDropFrames = audioFields.AudioFramesArrived > audioFields.AudioFramesWrittenToSink
            ? audioFields.AudioFramesArrived - audioFields.AudioFramesWrittenToSink
            : 0;
        if (audioBoundaryDropFrames > RecordingIntegrityAudioBoundaryToleranceFrames)
        {
            audioIncomplete = true;
            reasons.Add($"audio_boundary_drops={audioBoundaryDropFrames}");
        }

        if (audioFields.AudioSamplesEncoded <= 0)
        {
            audioFailed = true;
            reasons.Add("audio_sink_no_samples");
        }

        if (audioFields.AudioDropEvents > 0)
        {
            audioIncomplete = true;
            reasons.Add($"audio_drops={audioFields.AudioDropEvents}");
        }

        if (audioFields.AudioDiscontinuities > 0)
        {
            audioIncomplete = true;
            reasons.Add($"audio_discontinuities={audioFields.AudioDiscontinuities}");
        }

        if (audioFields.AudioTimestampErrors > 0)
        {
            audioIncomplete = true;
            reasons.Add($"audio_timestamp_errors={audioFields.AudioTimestampErrors}");
        }

        if (audioFields.AudioCallbackGaps > 0)
        {
            audioIncomplete = true;
            reasons.Add($"audio_callback_gaps={audioFields.AudioCallbackGaps}");
        }

        if (audioFields.AvSyncDriftMs is { } captureDriftMs &&
            Math.Abs(captureDriftMs) > RecordingIntegrityAvSyncDriftWarningMs)
        {
            audioIncomplete = true;
            reasons.Add($"av_sync_drift_ms={FormatRecordingIntegrityDouble(captureDriftMs)}");
        }

        if (audioFields.EncoderAvSyncDriftMs is { } encoderDriftMs &&
            Math.Abs(encoderDriftMs) > RecordingIntegrityAvSyncDriftWarningMs)
        {
            audioIncomplete = true;
            reasons.Add($"encoder_av_sync_drift_ms={FormatRecordingIntegrityDouble(encoderDriftMs)}");
        }

        return audioFailed ? "Failed" : audioIncomplete ? "Incomplete" : "Clean";
    }

    private static string FormatRecordingIntegrityDouble(double value)
        => value.ToString("0.###", CultureInfo.InvariantCulture);

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

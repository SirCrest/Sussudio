using System;
using System.Collections.Generic;
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
        sourceFrames = Math.Max(0, sourceFrames);
        acceptedFrames = Math.Max(0, acceptedFrames);
        var submittedFrames = Math.Max(0, counters.SubmittedFrames);
        var encodedFrames = Math.Max(0, counters.EncodedFrames);
        var packetsWritten = Math.Max(0, counters.PacketsWritten);
        var encoderDroppedFrames = Math.Max(0, counters.EncoderDroppedFrames);
        var queueDroppedFrames = Math.Max(0, counters.QueueDroppedFrames);
        var sequenceGaps = Math.Max(0, counters.SequenceGaps);
        var queueMaxDepth = Math.Max(0, counters.QueueMaxDepth);
        var queueOldestFrameAgeMs = Math.Max(0, counters.QueueOldestFrameAgeMs);
        var backpressureWaitMs = Math.Max(0, counters.BackpressureWaitMs);
        var backpressureEvents = Math.Max(0, counters.BackpressureEvents);
        var backpressureMaxWaitMs = Math.Max(0, counters.BackpressureMaxWaitMs);
        var audioFramesArrived = Math.Max(0, audioCounters.AudioFramesArrived);
        var audioFramesWrittenToSink = Math.Max(0, audioCounters.AudioFramesWrittenToSink);
        var audioSamplesEncoded = Math.Max(0, audioCounters.AudioSamplesEncoded);
        var audioDropEvents = Math.Max(0, audioCounters.AudioDropEvents);
        var audioDiscontinuities = Math.Max(0, audioCounters.AudioDiscontinuities);
        var audioTimestampErrors = Math.Max(0, audioCounters.AudioTimestampErrors);
        var audioCallbackGaps = Math.Max(0, audioCounters.AudioCallbackGaps);
        var rawPipelineDroppedFrames = Math.Max(0, sourceFrames - acceptedFrames);
        var pipelineDroppedFrames = recordingActive
            ? Math.Max(0, rawPipelineDroppedFrames - 1)
            : rawPipelineDroppedFrames;

        var reasons = new List<string>();
        if (!recordingActive && !finalizeSucceeded)
        {
            reasons.Add($"finalize='{finalizeStatus}'");
        }

        if (counters.EncodingFailed)
        {
            var failure = string.IsNullOrWhiteSpace(counters.EncodingFailureMessage)
                ? counters.EncodingFailureType ?? "unknown"
                : $"{counters.EncodingFailureType ?? "unknown"}: {counters.EncodingFailureMessage}";
            reasons.Add($"encoding={failure}");
        }

        if (pipelineDroppedFrames > 0)
        {
            reasons.Add($"pipeline_drops={pipelineDroppedFrames}");
        }

        if (queueDroppedFrames > 0)
        {
            reasons.Add($"queue_drops={queueDroppedFrames}");
        }

        if (encoderDroppedFrames > 0)
        {
            reasons.Add($"encoder_drops={encoderDroppedFrames}");
        }

        if (sequenceGaps > 0)
        {
            reasons.Add($"sequence_gaps={sequenceGaps}");
        }

        var audioStatus = "Disabled";
        if (audioCounters.AudioEnabled)
        {
            var audioFailed = false;
            var audioIncomplete = false;
            if (!audioCounters.AudioCaptureActive)
            {
                audioFailed = true;
                reasons.Add("audio_inactive");
            }

            if (audioFramesArrived <= 0)
            {
                audioFailed = true;
                reasons.Add("audio_no_frames");
            }

            var audioBoundaryDropFrames = audioFramesArrived > audioFramesWrittenToSink
                ? audioFramesArrived - audioFramesWrittenToSink
                : 0;
            if (audioBoundaryDropFrames > RecordingIntegrityAudioBoundaryToleranceFrames)
            {
                audioIncomplete = true;
                reasons.Add($"audio_boundary_drops={audioBoundaryDropFrames}");
            }

            if (audioSamplesEncoded <= 0)
            {
                audioFailed = true;
                reasons.Add("audio_sink_no_samples");
            }

            if (audioDropEvents > 0)
            {
                audioIncomplete = true;
                reasons.Add($"audio_drops={audioDropEvents}");
            }

            if (audioDiscontinuities > 0)
            {
                audioIncomplete = true;
                reasons.Add($"audio_discontinuities={audioDiscontinuities}");
            }

            if (audioTimestampErrors > 0)
            {
                audioIncomplete = true;
                reasons.Add($"audio_timestamp_errors={audioTimestampErrors}");
            }

            if (audioCallbackGaps > 0)
            {
                audioIncomplete = true;
                reasons.Add($"audio_callback_gaps={audioCallbackGaps}");
            }

            if (audioCounters.AvSyncDriftMs is { } captureDriftMs &&
                Math.Abs(captureDriftMs) > RecordingIntegrityAvSyncDriftWarningMs)
            {
                audioIncomplete = true;
                reasons.Add($"av_sync_drift_ms={FormatRecordingIntegrityDouble(captureDriftMs)}");
            }

            if (audioCounters.EncoderAvSyncDriftMs is { } encoderDriftMs &&
                Math.Abs(encoderDriftMs) > RecordingIntegrityAvSyncDriftWarningMs)
            {
                audioIncomplete = true;
                reasons.Add($"encoder_av_sync_drift_ms={FormatRecordingIntegrityDouble(encoderDriftMs)}");
            }

            audioStatus = audioFailed ? "Failed" : audioIncomplete ? "Incomplete" : "Clean";
        }

        var status = reasons.Count > 0
            ? (counters.EncodingFailed ||
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

        return new RecordingIntegritySummary
        {
            Status = status,
            Complete = !recordingActive && string.Equals(status, "Complete", StringComparison.Ordinal),
            Backend = backend,
            CompletedUtc = completedUtc,
            SourceFrames = sourceFrames,
            AcceptedFrames = acceptedFrames,
            PipelineDroppedFrames = pipelineDroppedFrames,
            QueueDroppedFrames = queueDroppedFrames,
            SubmittedFrames = submittedFrames,
            EncodedFrames = encodedFrames,
            PacketsWritten = packetsWritten,
            EncoderDroppedFrames = encoderDroppedFrames,
            SequenceGaps = sequenceGaps,
            QueueMaxDepth = queueMaxDepth,
            QueueOldestFrameAgeMs = queueOldestFrameAgeMs,
            BackpressureWaitMs = backpressureWaitMs,
            BackpressureEvents = backpressureEvents,
            BackpressureMaxWaitMs = backpressureMaxWaitMs,
            AudioStatus = audioStatus,
            AudioEnabled = audioCounters.AudioEnabled,
            AudioCaptureActive = audioCounters.AudioCaptureActive,
            AudioFramesArrived = audioFramesArrived,
            AudioFramesWrittenToSink = audioFramesWrittenToSink,
            AudioSamplesEncoded = audioSamplesEncoded,
            AudioDropEvents = audioDropEvents,
            AudioDiscontinuities = audioDiscontinuities,
            AudioTimestampErrors = audioTimestampErrors,
            AudioCallbackGaps = audioCallbackGaps,
            AvSyncDriftMs = audioCounters.AvSyncDriftMs,
            AvSyncDriftRateMsPerSec = audioCounters.AvSyncDriftRateMsPerSec,
            EncoderAvSyncDriftMs = audioCounters.EncoderAvSyncDriftMs,
            EncoderAvSyncCorrectionSamples = audioCounters.EncoderAvSyncCorrectionSamples,
            Reason = reason
        };
    }

    private static string FormatRecordingIntegrityDouble(double value)
        => value.ToString("0.###", CultureInfo.InvariantCulture);
}

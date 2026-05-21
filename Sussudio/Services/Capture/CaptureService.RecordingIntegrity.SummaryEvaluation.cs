using System;
using System.Collections.Generic;
using System.Globalization;

namespace Sussudio.Services.Capture;

public partial class CaptureService
{
    private readonly record struct RecordingIntegritySummaryEvaluation(
        string Status,
        string AudioStatus,
        string Reason);

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
}

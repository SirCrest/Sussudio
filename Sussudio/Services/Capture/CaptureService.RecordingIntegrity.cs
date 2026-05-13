using System;
using System.Collections.Generic;
using System.Globalization;
using Sussudio.Models;
using Sussudio.Services.Audio;
using Sussudio.Services.Flashback;
using Sussudio.Services.Recording;

namespace Sussudio.Services.Capture;

// Recording integrity compares counters captured at start/stop, not just final
// file metadata. This catches capture/sink discontinuities that a syntactically
// valid MP4 would otherwise hide.
public partial class CaptureService
{
    private const double RecordingIntegrityAvSyncDriftWarningMs = 500.0;
    private const long RecordingIntegrityAudioBoundaryToleranceFrames = 960;

    private sealed record RecordingIntegrityCounterSnapshot(
        string Backend,
        long SubmittedFrames,
        long EncodedFrames,
        long PacketsWritten,
        long EncoderDroppedFrames,
        long QueueDroppedFrames,
        long SequenceGaps,
        int QueueMaxDepth,
        long QueueOldestFrameAgeMs,
        long BackpressureWaitMs,
        long BackpressureEvents,
        long BackpressureMaxWaitMs,
        bool EncodingFailed,
        string? EncodingFailureType,
        string? EncodingFailureMessage);

    private sealed record RecordingAudioIntegrityCounterSnapshot(
        bool AudioEnabled,
        bool AudioCaptureActive,
        long AudioFramesArrived,
        long AudioFramesWrittenToSink,
        long AudioSamplesEncoded,
        long AudioDropEvents,
        long AudioDiscontinuities,
        long AudioTimestampErrors,
        long AudioCallbackGaps,
        double? AvSyncDriftMs,
        double? AvSyncDriftRateMsPerSec,
        double? EncoderAvSyncDriftMs,
        long? EncoderAvSyncCorrectionSamples)
    {
        public static RecordingAudioIntegrityCounterSnapshot Disabled { get; } = new(
            AudioEnabled: false,
            AudioCaptureActive: false,
            AudioFramesArrived: 0,
            AudioFramesWrittenToSink: 0,
            AudioSamplesEncoded: 0,
            AudioDropEvents: 0,
            AudioDiscontinuities: 0,
            AudioTimestampErrors: 0,
            AudioCallbackGaps: 0,
            AvSyncDriftMs: null,
            AvSyncDriftRateMsPerSec: null,
            EncoderAvSyncDriftMs: null,
            EncoderAvSyncCorrectionSamples: null);
    }

    private RecordingIntegritySummary ResolveRecordingIntegritySummary(
        UnifiedVideoCapture? unifiedVideoCapture,
        LibAvRecordingSink? sink,
        FlashbackEncoderSink? fbSink)
    {
        if (!_isRecording)
        {
            return _lastRecordingIntegrity;
        }

        if (IsFlashbackRecordingBackendOwnedByRecording() && fbSink != null)
        {
            var counters = CaptureFlashbackRecordingIntegrityCountersSinceBaseline(fbSink, unifiedVideoCapture);
            var audioCounters = GetRecordingAudioCountersSinceBaseline(
                CaptureRecordingAudioCounters(_wasapiAudioCapture, fbSink, _activeRecordingSettings));
            return BuildRecordingIntegritySummary(
                backend: "Flashback",
                recordingActive: true,
                finalizeSucceeded: true,
                finalizeStatus: "Recording",
                completedUtc: null,
                sourceFrames: unifiedVideoCapture?.RecordingFramesDelivered ?? 0,
                acceptedFrames: unifiedVideoCapture?.VideoFramesWrittenToSink ?? 0,
                counters: counters,
                audioCounters: audioCounters);
        }

        if (sink != null)
        {
            var counters = GetRecordingIntegrityCountersSinceBaseline(CaptureRecordingIntegrityCounters(sink));
            var audioCounters = GetRecordingAudioCountersSinceBaseline(
                CaptureRecordingAudioCounters(_wasapiAudioCapture, sink, _activeRecordingSettings));
            return BuildRecordingIntegritySummary(
                backend: "LibAv",
                recordingActive: true,
                finalizeSucceeded: true,
                finalizeStatus: "Recording",
                completedUtc: null,
                sourceFrames: unifiedVideoCapture?.RecordingFramesDelivered ?? 0,
                acceptedFrames: unifiedVideoCapture?.VideoFramesWrittenToSink ?? 0,
                counters: counters,
                audioCounters: audioCounters);
        }

        return new RecordingIntegritySummary
        {
            Status = "Active",
            Backend = ResolveRecordingBackendName(),
            Reason = "Recording active; recording boundary is still attaching."
        };
    }

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

    private RecordingIntegrityCounterSnapshot GetRecordingIntegrityCountersSinceBaseline(RecordingIntegrityCounterSnapshot current)
    {
        var baseline = _recordingIntegrityCounterBaseline;
        if (baseline == null ||
            !string.Equals(baseline.Backend, current.Backend, StringComparison.Ordinal))
        {
            return current;
        }

        return current with
        {
            SubmittedFrames = DeltaCounter(current.SubmittedFrames, baseline.SubmittedFrames),
            EncodedFrames = DeltaCounter(current.EncodedFrames, baseline.EncodedFrames),
            PacketsWritten = DeltaCounter(current.PacketsWritten, baseline.PacketsWritten),
            EncoderDroppedFrames = DeltaCounter(current.EncoderDroppedFrames, baseline.EncoderDroppedFrames),
            QueueDroppedFrames = DeltaCounter(current.QueueDroppedFrames, baseline.QueueDroppedFrames),
            SequenceGaps = DeltaCounter(current.SequenceGaps, baseline.SequenceGaps),
            BackpressureWaitMs = DeltaCounter(current.BackpressureWaitMs, baseline.BackpressureWaitMs),
            BackpressureEvents = DeltaCounter(current.BackpressureEvents, baseline.BackpressureEvents)
        };
    }

    private static RecordingIntegrityCounterSnapshot CaptureRecordingIntegrityCounters(LibAvRecordingSink sink)
        => new(
            Backend: "LibAv",
            SubmittedFrames: sink.VideoFramesSubmittedToEncoder,
            EncodedFrames: sink.EncodedVideoFrames,
            PacketsWritten: sink.VideoEncoderPacketsWritten,
            EncoderDroppedFrames: sink.VideoEncoderDroppedFrames,
            QueueDroppedFrames: SumNonNegative(
                sink.VideoDropsQueueSaturated,
                sink.GpuFramesDropped,
                sink.CudaFramesDropped),
            SequenceGaps: sink.VideoSequenceGaps,
            QueueMaxDepth: Math.Max(sink.VideoQueueMaxDepth, Math.Max(sink.GpuQueueMaxDepth, sink.CudaQueueMaxDepth)),
            QueueOldestFrameAgeMs: sink.VideoQueueOldestFrameAgeMs,
            BackpressureWaitMs: sink.VideoBackpressureWaitMs,
            BackpressureEvents: sink.VideoBackpressureEvents,
            BackpressureMaxWaitMs: sink.MaxVideoBackpressureWaitMs,
            EncodingFailed: sink.EncodingFailed,
            EncodingFailureType: sink.EncodingFailureType,
            EncodingFailureMessage: sink.EncodingFailureMessage);

    private static RecordingIntegrityCounterSnapshot CaptureRecordingIntegrityCounters(FlashbackEncoderSink sink)
        => new(
            Backend: "Flashback",
            SubmittedFrames: sink.VideoFramesSubmittedToEncoder,
            EncodedFrames: sink.EncodedVideoFrames,
            PacketsWritten: sink.VideoEncoderPacketsWritten,
            EncoderDroppedFrames: sink.VideoEncoderDroppedFrames,
            QueueDroppedFrames: SumNonNegative(
                sink.VideoDropsQueueSaturated,
                sink.GpuFramesDropped),
            SequenceGaps: sink.VideoSequenceGaps,
            QueueMaxDepth: Math.Max(sink.VideoQueueMaxDepth, sink.GpuQueueMaxDepth),
            QueueOldestFrameAgeMs: sink.VideoQueueOldestFrameAgeMs,
            BackpressureWaitMs: sink.VideoBackpressureWaitMs,
            BackpressureEvents: sink.VideoBackpressureEvents,
            BackpressureMaxWaitMs: sink.MaxVideoBackpressureWaitMs,
            EncodingFailed: sink.EncodingFailed,
            EncodingFailureType: sink.EncodingFailureType,
            EncodingFailureMessage: sink.EncodingFailureMessage);

    private RecordingIntegrityCounterSnapshot CaptureFlashbackRecordingIntegrityCountersSinceBaseline(
        FlashbackEncoderSink sink,
        UnifiedVideoCapture? videoCapture)
    {
        var counters = GetRecordingIntegrityCountersSinceBaseline(CaptureRecordingIntegrityCounters(sink));
        return videoCapture == null
            ? counters
            : counters with { SequenceGaps = Math.Max(0, videoCapture.FlashbackRecordingSequenceGaps) };
    }

    private RecordingAudioIntegrityCounterSnapshot GetRecordingAudioCountersSinceBaseline(RecordingAudioIntegrityCounterSnapshot current)
    {
        var baseline = _recordingIntegrityAudioBaseline;
        if (baseline == null)
        {
            return current;
        }

        return current with
        {
            AudioFramesArrived = DeltaCounter(current.AudioFramesArrived, baseline.AudioFramesArrived),
            AudioFramesWrittenToSink = DeltaCounter(current.AudioFramesWrittenToSink, baseline.AudioFramesWrittenToSink),
            AudioSamplesEncoded = DeltaCounter(current.AudioSamplesEncoded, baseline.AudioSamplesEncoded),
            AudioDropEvents = DeltaCounter(current.AudioDropEvents, baseline.AudioDropEvents),
            AudioDiscontinuities = DeltaCounter(current.AudioDiscontinuities, baseline.AudioDiscontinuities),
            AudioTimestampErrors = DeltaCounter(current.AudioTimestampErrors, baseline.AudioTimestampErrors),
            AudioCallbackGaps = DeltaCounter(current.AudioCallbackGaps, baseline.AudioCallbackGaps)
        };
    }

    private RecordingAudioIntegrityCounterSnapshot CaptureRecordingAudioCounters(
        WasapiAudioCapture? capture,
        LibAvRecordingSink sink,
        CaptureSettings? settings)
    {
        double? encoderAvSyncDriftMs = null;
        long? encoderAvSyncCorrectionSamples = null;
        if (sink.TryGetEncoderAvSyncDrift(out var driftMs, out var correctionSamples))
        {
            encoderAvSyncDriftMs = driftMs;
            encoderAvSyncCorrectionSamples = correctionSamples;
        }

        return CreateRecordingAudioCounters(
            capture,
            settings,
            audioFramesArrived: sink.AudioSamplesReceived,
            audioFramesWrittenToSink: sink.AudioSamplesReceived,
            audioSamplesEncoded: sink.AudioSamplesReceived,
            audioDropEvents: SumNonNegative(sink.AudioDropsQueueSaturated, sink.AudioDropsBacklogEviction),
            avSyncDriftMs: null,
            avSyncDriftRateMsPerSec: null,
            encoderAvSyncDriftMs: encoderAvSyncDriftMs,
            encoderAvSyncCorrectionSamples: encoderAvSyncCorrectionSamples);
    }

    private RecordingAudioIntegrityCounterSnapshot CaptureRecordingAudioCounters(
        WasapiAudioCapture? capture,
        FlashbackEncoderSink sink,
        CaptureSettings? settings)
        => CreateRecordingAudioCounters(
            capture,
            settings,
            audioFramesArrived: sink.AudioSamplesReceived,
            audioFramesWrittenToSink: sink.AudioSamplesReceived,
            audioSamplesEncoded: sink.AudioSamplesReceived,
            audioDropEvents: SumNonNegative(sink.AudioDropsQueueSaturated, sink.AudioDropsBacklogEviction),
            avSyncDriftMs: null,
            avSyncDriftRateMsPerSec: null,
            encoderAvSyncDriftMs: null,
            encoderAvSyncCorrectionSamples: null);

    private RecordingAudioIntegrityCounterSnapshot CreateRecordingAudioCounters(
        WasapiAudioCapture? capture,
        CaptureSettings? settings,
        long audioFramesArrived,
        long audioFramesWrittenToSink,
        long audioSamplesEncoded,
        long audioDropEvents,
        double? avSyncDriftMs,
        double? avSyncDriftRateMsPerSec,
        double? encoderAvSyncDriftMs,
        long? encoderAvSyncCorrectionSamples)
    {
        var audioEnabled = settings?.AudioEnabled == true;
        if (!audioEnabled)
        {
            return RecordingAudioIntegrityCounterSnapshot.Disabled;
        }

        return new RecordingAudioIntegrityCounterSnapshot(
            AudioEnabled: true,
            AudioCaptureActive: capture?.IsCapturing == true,
            AudioFramesArrived: audioFramesArrived,
            AudioFramesWrittenToSink: audioFramesWrittenToSink,
            AudioSamplesEncoded: audioSamplesEncoded,
            AudioDropEvents: audioDropEvents,
            AudioDiscontinuities: capture?.AudioDataDiscontinuityCount ?? 0,
            AudioTimestampErrors: capture?.AudioTimestampErrorCount ?? 0,
            AudioCallbackGaps: capture?.CaptureCallbackSevereGapCount ?? 0,
            AvSyncDriftMs: avSyncDriftMs,
            AvSyncDriftRateMsPerSec: avSyncDriftRateMsPerSec,
            EncoderAvSyncDriftMs: encoderAvSyncDriftMs,
            EncoderAvSyncCorrectionSamples: encoderAvSyncCorrectionSamples);
    }

    private static long DeltaCounter(long current, long baseline)
        => current >= baseline ? current - baseline : current;

    private static long SumNonNegative(long a, long b)
        => (a > 0 ? a : 0) + (b > 0 ? b : 0);

    private static long SumNonNegative(long a, long b, long c)
        => (a > 0 ? a : 0) + (b > 0 ? b : 0) + (c > 0 ? c : 0);

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
            $"av_drift_ms={summary.AvSyncDriftMs?.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) ?? "N/A"} " +
            $"encoder_av_drift_ms={summary.EncoderAvSyncDriftMs?.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) ?? "N/A"} " +
            $"reason='{summary.Reason.Replace("'", "\\'", StringComparison.Ordinal)}'");
    }
}

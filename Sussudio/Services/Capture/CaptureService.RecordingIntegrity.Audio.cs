using Sussudio.Models;
using Sussudio.Services.Audio;
using Sussudio.Services.Flashback;
using Sussudio.Services.Recording;

namespace Sussudio.Services.Capture;

public partial class CaptureService
{
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
}

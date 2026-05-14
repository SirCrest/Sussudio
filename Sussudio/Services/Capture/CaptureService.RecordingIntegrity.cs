using System;
using Sussudio.Models;
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

}

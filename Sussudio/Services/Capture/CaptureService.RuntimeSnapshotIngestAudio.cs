using Sussudio.Services.Audio;
using Sussudio.Services.Recording;

namespace Sussudio.Services.Capture;

// Read-only ingest/audio projection for runtime snapshots. This path must not
// start, stop, or dispose capture resources while automation is polling.
public partial class CaptureService
{
    private RuntimeIngestAudioSnapshotFields CaptureRuntimeIngestAudioSnapshotFields(
        UnifiedVideoCapture? unifiedVideoCapture,
        LibAvRecordingSink? sink,
        WasapiAudioCapture? wasapiCapture,
        WasapiAudioPlayback? wasapiPlayback,
        bool videoPreviewActive,
        bool recordingActive)
    {
        var (wasapiCaptureCallbackAvgMs, wasapiCaptureCallbackMaxMs) =
            wasapiCapture?.GetCaptureCallbackIntervalSnapshot() ?? (0d, 0d);

        return new RuntimeIngestAudioSnapshotFields
        {
            AudioReaderActive = wasapiCapture?.IsCapturing ?? false,
            AudioFramesArrived = wasapiCapture?.AudioFramesArrived ?? 0,
            AudioFramesWrittenToSink = wasapiCapture?.AudioFramesWrittenToSink ?? 0,
            VideoReaderActive = unifiedVideoCapture != null && (videoPreviewActive || recordingActive),
            IngestVideoFramesArrived = unifiedVideoCapture?.VideoFramesArrived ?? 0,
            IngestVideoFramesWrittenToSink = unifiedVideoCapture?.VideoFramesWrittenToSink ?? 0,
            IngestLastVideoFrameAgeMs = ComputeTickAge(unifiedVideoCapture?.LastVideoFrameArrivedTick ?? 0),
            VideoIngestErrorCount = unifiedVideoCapture?.VideoFramesDropped ?? 0,
            MfSourceReaderFramesDelivered = unifiedVideoCapture?.VideoFramesArrived ?? _lastMfSourceReaderFramesDelivered,
            MfSourceReaderFramesDropped = unifiedVideoCapture?.VideoFramesDropped ?? _lastMfSourceReaderFramesDropped,
            SourceReaderReadOutstanding = unifiedVideoCapture?.SourceReaderReadOutstanding ?? false,
            SourceReaderReadOutstandingMs = unifiedVideoCapture?.SourceReaderReadOutstandingMs ?? 0,
            SourceReaderLastFrameTickMs = unifiedVideoCapture?.SourceReaderLastFrameTickMs ?? 0,
            SourceReaderFrameChannelDepth = sink?.VideoQueueCount ?? 0,
            WasapiCaptureCallbackCount = wasapiCapture?.CaptureCallbackCount ?? 0,
            WasapiCaptureCallbackAvgIntervalMs = wasapiCaptureCallbackAvgMs,
            WasapiCaptureCallbackMaxIntervalMs = wasapiCaptureCallbackMaxMs,
            WasapiCaptureCallbackSevereGapCount = wasapiCapture?.CaptureCallbackSevereGapCount ?? 0,
            WasapiCaptureAudioDiscontinuityCount = wasapiCapture?.AudioDataDiscontinuityCount ?? 0,
            WasapiCaptureAudioTimestampErrorCount = wasapiCapture?.AudioTimestampErrorCount ?? 0,
            WasapiCaptureAudioGlitchCount = wasapiCapture?.AudioGlitchCount ?? 0,
            WasapiCaptureCallbackSilenceCount = wasapiCapture?.CaptureCallbackSilenceCount ?? 0,
            WasapiCaptureLastCallbackTickMs = wasapiCapture?.LastCaptureCallbackTickMs ?? 0,
            WasapiCaptureAudioLevelEventsFired = wasapiCapture?.AudioLevelEventsFired ?? 0,
            WasapiCaptureAudioLevelLastFireTickMs = wasapiCapture?.AudioLevelEventsLastFireTickMs ?? 0,
            WasapiPlaybackRenderCallbackCount = wasapiPlayback?.RenderCallbackCount ?? 0,
            WasapiPlaybackRenderSilenceCount = wasapiPlayback?.RenderSilenceCount ?? 0,
            WasapiPlaybackQueueDepth = wasapiPlayback?.PlaybackQueueDepth ?? 0,
            WasapiPlaybackQueueDropCount = wasapiPlayback?.PlaybackQueueDropCount ?? 0,
            WasapiPlaybackQueueDurationMs = wasapiPlayback?.PlaybackQueueDurationMs ?? 0,
            WasapiPlaybackActiveChunkDurationMs = wasapiPlayback?.PlaybackActiveChunkDurationMs ?? 0,
            WasapiPlaybackEndpointQueuedDurationMs = wasapiPlayback?.PlaybackEndpointQueuedDurationMs ?? 0,
            WasapiPlaybackBufferedDurationMs = wasapiPlayback?.PlaybackBufferedDurationMs ?? 0,
            WasapiPlaybackStreamLatencyMs = wasapiPlayback?.PlaybackStreamLatencyMs ?? 0,
            WasapiPlaybackLastRenderTickMs = wasapiPlayback?.LastRenderCallbackTickMs ?? 0,
            WasapiPlaybackTargetVolumePercent = (wasapiPlayback?.TargetVolume ?? 0) * 100.0,
            WasapiPlaybackCurrentVolumePercent = (wasapiPlayback?.CurrentVolume ?? 0) * 100.0,
            WasapiPlaybackOutputPeak = wasapiPlayback?.LastOutputPeak ?? 0,
            WasapiPlaybackOutputRms = wasapiPlayback?.LastOutputRms ?? 0,
            WasapiPlaybackOutputLevelLastTickMs = wasapiPlayback?.LastOutputLevelTickMs ?? 0
        };
    }
}

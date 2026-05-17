using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static AudioAndIngestProjection BuildAudioAndIngestProjection(
        ViewModelRuntimeSnapshot viewModelSnapshot,
        CaptureRuntimeSnapshot captureRuntime,
        AudioSignalState audioSignal)
    {
        var audioSignalProjection = BuildAudioSignalProjection(viewModelSnapshot, audioSignal);
        var captureIngest = BuildCaptureIngestProjection(captureRuntime);
        var wasapiAudio = BuildWasapiAudioProjection(captureRuntime);

        return new()
        {
            AudioPeak = audioSignalProjection.Peak,
            AudioClipping = audioSignalProjection.Clipping,
            AudioSignalPresent = audioSignalProjection.SignalPresent,
            AudioMutedSuspected = audioSignalProjection.MutedSuspected,
            AudioReaderActive = captureIngest.AudioReaderActive,
            AudioFramesArrived = captureIngest.AudioFramesArrived,
            AudioFramesWrittenToSink = captureIngest.AudioFramesWrittenToSink,
            VideoReaderActive = captureIngest.VideoReaderActive,
            IngestVideoFramesArrived = captureIngest.VideoFramesArrived,
            IngestVideoFramesWrittenToSink = captureIngest.VideoFramesWrittenToSink,
            IngestLastVideoFrameAgeMs = captureIngest.LastVideoFrameAgeMs,
            VideoIngestErrorCount = captureIngest.VideoIngestErrorCount,
            MfSourceReaderFramesDelivered = captureIngest.MfSourceReaderFramesDelivered,
            MfSourceReaderFramesDropped = captureIngest.MfSourceReaderFramesDropped,
            MfSourceReaderNegotiatedFormat = captureIngest.MfSourceReaderNegotiatedFormat,
            SourceReaderReadOutstanding = captureIngest.SourceReaderReadOutstanding,
            SourceReaderReadOutstandingMs = captureIngest.SourceReaderReadOutstandingMs,
            SourceReaderLastFrameTickMs = captureIngest.SourceReaderLastFrameTickMs,
            SourceReaderFrameChannelDepth = captureIngest.SourceReaderFrameChannelDepth,
            WasapiCaptureCallbackCount = wasapiAudio.CaptureCallbackCount,
            WasapiCaptureCallbackAvgIntervalMs = wasapiAudio.CaptureCallbackAvgIntervalMs,
            WasapiCaptureCallbackMaxIntervalMs = wasapiAudio.CaptureCallbackMaxIntervalMs,
            WasapiCaptureCallbackSevereGapCount = wasapiAudio.CaptureCallbackSevereGapCount,
            WasapiCaptureAudioDiscontinuityCount = wasapiAudio.CaptureAudioDiscontinuityCount,
            WasapiCaptureAudioTimestampErrorCount = wasapiAudio.CaptureAudioTimestampErrorCount,
            WasapiCaptureAudioGlitchCount = wasapiAudio.CaptureAudioGlitchCount,
            WasapiCaptureCallbackSilenceCount = wasapiAudio.CaptureCallbackSilenceCount,
            WasapiCaptureLastCallbackTickMs = wasapiAudio.CaptureLastCallbackTickMs,
            WasapiCaptureAudioLevelEventsFired = wasapiAudio.CaptureAudioLevelEventsFired,
            WasapiCaptureAudioLevelLastFireTickMs = wasapiAudio.CaptureAudioLevelLastFireTickMs,
            WasapiPlaybackRenderCallbackCount = wasapiAudio.PlaybackRenderCallbackCount,
            WasapiPlaybackRenderSilenceCount = wasapiAudio.PlaybackRenderSilenceCount,
            WasapiPlaybackQueueDepth = wasapiAudio.PlaybackQueueDepth,
            WasapiPlaybackQueueDropCount = wasapiAudio.PlaybackQueueDropCount,
            WasapiPlaybackQueueDurationMs = wasapiAudio.PlaybackQueueDurationMs,
            WasapiPlaybackActiveChunkDurationMs = wasapiAudio.PlaybackActiveChunkDurationMs,
            WasapiPlaybackEndpointQueuedDurationMs = wasapiAudio.PlaybackEndpointQueuedDurationMs,
            WasapiPlaybackBufferedDurationMs = wasapiAudio.PlaybackBufferedDurationMs,
            WasapiPlaybackStreamLatencyMs = wasapiAudio.PlaybackStreamLatencyMs,
            WasapiPlaybackLastRenderTickMs = wasapiAudio.PlaybackLastRenderTickMs
        };
    }

    private readonly record struct AudioAndIngestProjection
    {
        public double AudioPeak { get; init; }
        public bool AudioClipping { get; init; }
        public bool AudioSignalPresent { get; init; }
        public bool AudioMutedSuspected { get; init; }
        public bool AudioReaderActive { get; init; }
        public long AudioFramesArrived { get; init; }
        public long AudioFramesWrittenToSink { get; init; }
        public bool VideoReaderActive { get; init; }
        public long IngestVideoFramesArrived { get; init; }
        public long IngestVideoFramesWrittenToSink { get; init; }
        public long IngestLastVideoFrameAgeMs { get; init; }
        public long VideoIngestErrorCount { get; init; }
        public long MfSourceReaderFramesDelivered { get; init; }
        public long MfSourceReaderFramesDropped { get; init; }
        public string? MfSourceReaderNegotiatedFormat { get; init; }
        public bool SourceReaderReadOutstanding { get; init; }
        public long SourceReaderReadOutstandingMs { get; init; }
        public long SourceReaderLastFrameTickMs { get; init; }
        public int SourceReaderFrameChannelDepth { get; init; }
        public long WasapiCaptureCallbackCount { get; init; }
        public double WasapiCaptureCallbackAvgIntervalMs { get; init; }
        public double WasapiCaptureCallbackMaxIntervalMs { get; init; }
        public long WasapiCaptureCallbackSevereGapCount { get; init; }
        public long WasapiCaptureAudioDiscontinuityCount { get; init; }
        public long WasapiCaptureAudioTimestampErrorCount { get; init; }
        public long WasapiCaptureAudioGlitchCount { get; init; }
        public int WasapiCaptureCallbackSilenceCount { get; init; }
        public long WasapiCaptureLastCallbackTickMs { get; init; }
        public long WasapiCaptureAudioLevelEventsFired { get; init; }
        public long WasapiCaptureAudioLevelLastFireTickMs { get; init; }
        public long WasapiPlaybackRenderCallbackCount { get; init; }
        public int WasapiPlaybackRenderSilenceCount { get; init; }
        public int WasapiPlaybackQueueDepth { get; init; }
        public int WasapiPlaybackQueueDropCount { get; init; }
        public double WasapiPlaybackQueueDurationMs { get; init; }
        public double WasapiPlaybackActiveChunkDurationMs { get; init; }
        public double WasapiPlaybackEndpointQueuedDurationMs { get; init; }
        public double WasapiPlaybackBufferedDurationMs { get; init; }
        public double WasapiPlaybackStreamLatencyMs { get; init; }
        public long WasapiPlaybackLastRenderTickMs { get; init; }
    }

    private static AudioSignalProjection BuildAudioSignalProjection(
        ViewModelRuntimeSnapshot viewModelSnapshot,
        AudioSignalState audioSignal)
        => new()
        {
            Peak = viewModelSnapshot.AudioPeak,
            Clipping = viewModelSnapshot.AudioClipping,
            SignalPresent = audioSignal.SignalPresent,
            MutedSuspected = audioSignal.MutedSuspected
        };

    private readonly record struct AudioSignalProjection
    {
        public double Peak { get; init; }
        public bool Clipping { get; init; }
        public bool SignalPresent { get; init; }
        public bool MutedSuspected { get; init; }
    }

    private static AudioDropsProjection BuildAudioDropsProjection(CaptureHealthSnapshot health)
    {
        return new()
        {
            QueueSaturated = health.AudioDropsQueueSaturated,
            BacklogEviction = health.AudioDropsBacklogEviction,
            ChunksDropped = health.AudioChunksDropped,
            QueueDropsRealtime = health.AudioDropsQueueSaturated + health.AudioDropsBacklogEviction,
            QueueDropsFileWriter = health.AudioChunksDropped
        };
    }

    private readonly record struct AudioDropsProjection
    {
        public long QueueSaturated { get; init; }
        public long BacklogEviction { get; init; }
        public long ChunksDropped { get; init; }
        public long QueueDropsRealtime { get; init; }
        public long QueueDropsFileWriter { get; init; }
    }
}

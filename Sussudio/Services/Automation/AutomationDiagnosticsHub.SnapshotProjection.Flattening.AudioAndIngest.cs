namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static AudioAndIngestFlattenedProjection BuildAudioAndIngestFlattenedProjection(
        AudioAndIngestProjection audioAndIngest)
        => new()
        {
            AudioPeak = audioAndIngest.AudioPeak,
            AudioClipping = audioAndIngest.AudioClipping,
            AudioSignalPresent = audioAndIngest.AudioSignalPresent,
            AudioMutedSuspected = audioAndIngest.AudioMutedSuspected,
            AudioReaderActive = audioAndIngest.AudioReaderActive,
            AudioFramesArrived = audioAndIngest.AudioFramesArrived,
            AudioFramesWrittenToSink = audioAndIngest.AudioFramesWrittenToSink,
            VideoReaderActive = audioAndIngest.VideoReaderActive,
            IngestVideoFramesArrived = audioAndIngest.IngestVideoFramesArrived,
            IngestVideoFramesWrittenToSink = audioAndIngest.IngestVideoFramesWrittenToSink,
            IngestLastVideoFrameAgeMs = audioAndIngest.IngestLastVideoFrameAgeMs,
            VideoIngestErrorCount = audioAndIngest.VideoIngestErrorCount,
            MfSourceReaderFramesDelivered = audioAndIngest.MfSourceReaderFramesDelivered,
            MfSourceReaderFramesDropped = audioAndIngest.MfSourceReaderFramesDropped,
            MfSourceReaderNegotiatedFormat = audioAndIngest.MfSourceReaderNegotiatedFormat,
            SourceReaderReadOutstanding = audioAndIngest.SourceReaderReadOutstanding,
            SourceReaderReadOutstandingMs = audioAndIngest.SourceReaderReadOutstandingMs,
            SourceReaderLastFrameTickMs = audioAndIngest.SourceReaderLastFrameTickMs,
            SourceReaderFrameChannelDepth = audioAndIngest.SourceReaderFrameChannelDepth,
            WasapiCaptureCallbackCount = audioAndIngest.WasapiCaptureCallbackCount,
            WasapiCaptureCallbackAvgIntervalMs = audioAndIngest.WasapiCaptureCallbackAvgIntervalMs,
            WasapiCaptureCallbackMaxIntervalMs = audioAndIngest.WasapiCaptureCallbackMaxIntervalMs,
            WasapiCaptureCallbackSevereGapCount = audioAndIngest.WasapiCaptureCallbackSevereGapCount,
            WasapiCaptureAudioDiscontinuityCount = audioAndIngest.WasapiCaptureAudioDiscontinuityCount,
            WasapiCaptureAudioTimestampErrorCount = audioAndIngest.WasapiCaptureAudioTimestampErrorCount,
            WasapiCaptureAudioGlitchCount = audioAndIngest.WasapiCaptureAudioGlitchCount,
            WasapiCaptureCallbackSilenceCount = audioAndIngest.WasapiCaptureCallbackSilenceCount,
            WasapiCaptureLastCallbackTickMs = audioAndIngest.WasapiCaptureLastCallbackTickMs,
            WasapiCaptureAudioLevelEventsFired = audioAndIngest.WasapiCaptureAudioLevelEventsFired,
            WasapiCaptureAudioLevelLastFireTickMs = audioAndIngest.WasapiCaptureAudioLevelLastFireTickMs,
            WasapiPlaybackRenderCallbackCount = audioAndIngest.WasapiPlaybackRenderCallbackCount,
            WasapiPlaybackRenderSilenceCount = audioAndIngest.WasapiPlaybackRenderSilenceCount,
            WasapiPlaybackQueueDepth = audioAndIngest.WasapiPlaybackQueueDepth,
            WasapiPlaybackQueueDropCount = audioAndIngest.WasapiPlaybackQueueDropCount,
            WasapiPlaybackQueueDurationMs = audioAndIngest.WasapiPlaybackQueueDurationMs,
            WasapiPlaybackActiveChunkDurationMs = audioAndIngest.WasapiPlaybackActiveChunkDurationMs,
            WasapiPlaybackEndpointQueuedDurationMs = audioAndIngest.WasapiPlaybackEndpointQueuedDurationMs,
            WasapiPlaybackBufferedDurationMs = audioAndIngest.WasapiPlaybackBufferedDurationMs,
            WasapiPlaybackStreamLatencyMs = audioAndIngest.WasapiPlaybackStreamLatencyMs,
            WasapiPlaybackLastRenderTickMs = audioAndIngest.WasapiPlaybackLastRenderTickMs
        };

    private readonly record struct AudioAndIngestFlattenedProjection
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
}

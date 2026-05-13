using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static AudioAndIngestProjection BuildAudioAndIngestProjection(
        ViewModelRuntimeSnapshot viewModelSnapshot,
        CaptureRuntimeSnapshot captureRuntime,
        AudioSignalState audioSignal)
        => new()
        {
            AudioPeak = viewModelSnapshot.AudioPeak,
            AudioClipping = viewModelSnapshot.AudioClipping,
            AudioSignalPresent = audioSignal.SignalPresent,
            AudioMutedSuspected = audioSignal.MutedSuspected,
            AudioReaderActive = captureRuntime.AudioReaderActive,
            AudioFramesArrived = captureRuntime.AudioFramesArrived,
            AudioFramesWrittenToSink = captureRuntime.AudioFramesWrittenToSink,
            VideoReaderActive = captureRuntime.VideoReaderActive,
            IngestVideoFramesArrived = captureRuntime.IngestVideoFramesArrived,
            IngestVideoFramesWrittenToSink = captureRuntime.IngestVideoFramesWrittenToSink,
            IngestLastVideoFrameAgeMs = captureRuntime.IngestLastVideoFrameAgeMs,
            VideoIngestErrorCount = captureRuntime.VideoIngestErrorCount,
            MfSourceReaderFramesDelivered = captureRuntime.MfSourceReaderFramesDelivered,
            MfSourceReaderFramesDropped = captureRuntime.MfSourceReaderFramesDropped,
            MfSourceReaderNegotiatedFormat = captureRuntime.MfSourceReaderNegotiatedFormat,
            SourceReaderReadOutstanding = captureRuntime.SourceReaderReadOutstanding,
            SourceReaderReadOutstandingMs = captureRuntime.SourceReaderReadOutstandingMs,
            SourceReaderLastFrameTickMs = captureRuntime.SourceReaderLastFrameTickMs,
            SourceReaderFrameChannelDepth = captureRuntime.SourceReaderFrameChannelDepth,
            WasapiCaptureCallbackCount = captureRuntime.WasapiCaptureCallbackCount,
            WasapiCaptureCallbackAvgIntervalMs = captureRuntime.WasapiCaptureCallbackAvgIntervalMs,
            WasapiCaptureCallbackMaxIntervalMs = captureRuntime.WasapiCaptureCallbackMaxIntervalMs,
            WasapiCaptureCallbackSevereGapCount = captureRuntime.WasapiCaptureCallbackSevereGapCount,
            WasapiCaptureAudioDiscontinuityCount = captureRuntime.WasapiCaptureAudioDiscontinuityCount,
            WasapiCaptureAudioTimestampErrorCount = captureRuntime.WasapiCaptureAudioTimestampErrorCount,
            WasapiCaptureAudioGlitchCount = captureRuntime.WasapiCaptureAudioGlitchCount,
            WasapiCaptureCallbackSilenceCount = captureRuntime.WasapiCaptureCallbackSilenceCount,
            WasapiCaptureLastCallbackTickMs = captureRuntime.WasapiCaptureLastCallbackTickMs,
            WasapiCaptureAudioLevelEventsFired = captureRuntime.WasapiCaptureAudioLevelEventsFired,
            WasapiCaptureAudioLevelLastFireTickMs = captureRuntime.WasapiCaptureAudioLevelLastFireTickMs,
            WasapiPlaybackRenderCallbackCount = captureRuntime.WasapiPlaybackRenderCallbackCount,
            WasapiPlaybackRenderSilenceCount = captureRuntime.WasapiPlaybackRenderSilenceCount,
            WasapiPlaybackQueueDepth = captureRuntime.WasapiPlaybackQueueDepth,
            WasapiPlaybackQueueDropCount = captureRuntime.WasapiPlaybackQueueDropCount,
            WasapiPlaybackQueueDurationMs = captureRuntime.WasapiPlaybackQueueDurationMs,
            WasapiPlaybackActiveChunkDurationMs = captureRuntime.WasapiPlaybackActiveChunkDurationMs,
            WasapiPlaybackEndpointQueuedDurationMs = captureRuntime.WasapiPlaybackEndpointQueuedDurationMs,
            WasapiPlaybackBufferedDurationMs = captureRuntime.WasapiPlaybackBufferedDurationMs,
            WasapiPlaybackStreamLatencyMs = captureRuntime.WasapiPlaybackStreamLatencyMs,
            WasapiPlaybackLastRenderTickMs = captureRuntime.WasapiPlaybackLastRenderTickMs
        };

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
}

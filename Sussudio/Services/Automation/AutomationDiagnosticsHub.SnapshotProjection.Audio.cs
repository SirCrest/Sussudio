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
            Signal = BuildAudioSignalProjection(viewModelSnapshot, audioSignal),
            Ingest = BuildCaptureIngestProjection(captureRuntime),
            Wasapi = BuildWasapiAudioProjection(captureRuntime)
        };

    private readonly record struct AudioAndIngestProjection
    {
        public AudioSignalProjection Signal { get; init; }
        public CaptureIngestProjection Ingest { get; init; }
        public WasapiAudioProjection Wasapi { get; init; }
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

    private static AudioSignalFlattenedProjection BuildAudioSignalFlattenedProjection(
        AudioSignalProjection signal)
        => new()
        {
            Peak = signal.Peak,
            Clipping = signal.Clipping,
            SignalPresent = signal.SignalPresent,
            MutedSuspected = signal.MutedSuspected
        };

    private readonly record struct AudioSignalFlattenedProjection
    {
        public double Peak { get; init; }
        public bool Clipping { get; init; }
        public bool SignalPresent { get; init; }
        public bool MutedSuspected { get; init; }
    }

    private static CaptureIngestProjection BuildCaptureIngestProjection(CaptureRuntimeSnapshot captureRuntime)
        => new()
        {
            AudioReaderActive = captureRuntime.AudioReaderActive,
            AudioFramesArrived = captureRuntime.AudioFramesArrived,
            AudioFramesWrittenToSink = captureRuntime.AudioFramesWrittenToSink,
            VideoReaderActive = captureRuntime.VideoReaderActive,
            VideoFramesArrived = captureRuntime.IngestVideoFramesArrived,
            VideoFramesWrittenToSink = captureRuntime.IngestVideoFramesWrittenToSink,
            LastVideoFrameAgeMs = captureRuntime.IngestLastVideoFrameAgeMs,
            VideoIngestErrorCount = captureRuntime.VideoIngestErrorCount,
            MfSourceReaderFramesDelivered = captureRuntime.MfSourceReaderFramesDelivered,
            MfSourceReaderFramesDropped = captureRuntime.MfSourceReaderFramesDropped,
            MfSourceReaderNegotiatedFormat = captureRuntime.MfSourceReaderNegotiatedFormat,
            SourceReaderReadOutstanding = captureRuntime.SourceReaderReadOutstanding,
            SourceReaderReadOutstandingMs = captureRuntime.SourceReaderReadOutstandingMs,
            SourceReaderLastFrameTickMs = captureRuntime.SourceReaderLastFrameTickMs,
            SourceReaderFrameChannelDepth = captureRuntime.SourceReaderFrameChannelDepth
        };

    private readonly record struct CaptureIngestProjection
    {
        public bool AudioReaderActive { get; init; }
        public long AudioFramesArrived { get; init; }
        public long AudioFramesWrittenToSink { get; init; }
        public bool VideoReaderActive { get; init; }
        public long VideoFramesArrived { get; init; }
        public long VideoFramesWrittenToSink { get; init; }
        public long LastVideoFrameAgeMs { get; init; }
        public long VideoIngestErrorCount { get; init; }
        public long MfSourceReaderFramesDelivered { get; init; }
        public long MfSourceReaderFramesDropped { get; init; }
        public string? MfSourceReaderNegotiatedFormat { get; init; }
        public bool SourceReaderReadOutstanding { get; init; }
        public long SourceReaderReadOutstandingMs { get; init; }
        public long SourceReaderLastFrameTickMs { get; init; }
        public int SourceReaderFrameChannelDepth { get; init; }
    }

    private static CaptureIngestFlattenedProjection BuildCaptureIngestFlattenedProjection(
        CaptureIngestProjection ingest)
        => new()
        {
            AudioReaderActive = ingest.AudioReaderActive,
            AudioFramesArrived = ingest.AudioFramesArrived,
            AudioFramesWrittenToSink = ingest.AudioFramesWrittenToSink,
            VideoReaderActive = ingest.VideoReaderActive,
            VideoFramesArrived = ingest.VideoFramesArrived,
            VideoFramesWrittenToSink = ingest.VideoFramesWrittenToSink,
            LastVideoFrameAgeMs = ingest.LastVideoFrameAgeMs,
            VideoIngestErrorCount = ingest.VideoIngestErrorCount
        };

    private readonly record struct CaptureIngestFlattenedProjection
    {
        public bool AudioReaderActive { get; init; }
        public long AudioFramesArrived { get; init; }
        public long AudioFramesWrittenToSink { get; init; }
        public bool VideoReaderActive { get; init; }
        public long VideoFramesArrived { get; init; }
        public long VideoFramesWrittenToSink { get; init; }
        public long LastVideoFrameAgeMs { get; init; }
        public long VideoIngestErrorCount { get; init; }
    }

    private static SourceReaderFlattenedProjection BuildSourceReaderFlattenedProjection(
        CaptureIngestProjection ingest)
        => new()
        {
            FramesDelivered = ingest.MfSourceReaderFramesDelivered,
            FramesDropped = ingest.MfSourceReaderFramesDropped,
            NegotiatedFormat = ingest.MfSourceReaderNegotiatedFormat,
            ReadOutstanding = ingest.SourceReaderReadOutstanding,
            ReadOutstandingMs = ingest.SourceReaderReadOutstandingMs,
            LastFrameTickMs = ingest.SourceReaderLastFrameTickMs,
            FrameChannelDepth = ingest.SourceReaderFrameChannelDepth
        };

    private readonly record struct SourceReaderFlattenedProjection
    {
        public long FramesDelivered { get; init; }
        public long FramesDropped { get; init; }
        public string? NegotiatedFormat { get; init; }
        public bool ReadOutstanding { get; init; }
        public long ReadOutstandingMs { get; init; }
        public long LastFrameTickMs { get; init; }
        public int FrameChannelDepth { get; init; }
    }

    private static WasapiAudioProjection BuildWasapiAudioProjection(CaptureRuntimeSnapshot captureRuntime)
        => new()
        {
            CaptureCallbackCount = captureRuntime.WasapiCaptureCallbackCount,
            CaptureCallbackAvgIntervalMs = captureRuntime.WasapiCaptureCallbackAvgIntervalMs,
            CaptureCallbackMaxIntervalMs = captureRuntime.WasapiCaptureCallbackMaxIntervalMs,
            CaptureCallbackSevereGapCount = captureRuntime.WasapiCaptureCallbackSevereGapCount,
            CaptureAudioDiscontinuityCount = captureRuntime.WasapiCaptureAudioDiscontinuityCount,
            CaptureAudioTimestampErrorCount = captureRuntime.WasapiCaptureAudioTimestampErrorCount,
            CaptureAudioGlitchCount = captureRuntime.WasapiCaptureAudioGlitchCount,
            CaptureCallbackSilenceCount = captureRuntime.WasapiCaptureCallbackSilenceCount,
            CaptureLastCallbackTickMs = captureRuntime.WasapiCaptureLastCallbackTickMs,
            CaptureAudioLevelEventsFired = captureRuntime.WasapiCaptureAudioLevelEventsFired,
            CaptureAudioLevelLastFireTickMs = captureRuntime.WasapiCaptureAudioLevelLastFireTickMs,
            PlaybackRenderCallbackCount = captureRuntime.WasapiPlaybackRenderCallbackCount,
            PlaybackRenderSilenceCount = captureRuntime.WasapiPlaybackRenderSilenceCount,
            PlaybackQueueDepth = captureRuntime.WasapiPlaybackQueueDepth,
            PlaybackQueueDropCount = captureRuntime.WasapiPlaybackQueueDropCount,
            PlaybackQueueDurationMs = captureRuntime.WasapiPlaybackQueueDurationMs,
            PlaybackActiveChunkDurationMs = captureRuntime.WasapiPlaybackActiveChunkDurationMs,
            PlaybackEndpointQueuedDurationMs = captureRuntime.WasapiPlaybackEndpointQueuedDurationMs,
            PlaybackBufferedDurationMs = captureRuntime.WasapiPlaybackBufferedDurationMs,
            PlaybackStreamLatencyMs = captureRuntime.WasapiPlaybackStreamLatencyMs,
            PlaybackLastRenderTickMs = captureRuntime.WasapiPlaybackLastRenderTickMs
        };

    private readonly record struct WasapiAudioProjection
    {
        public long CaptureCallbackCount { get; init; }
        public double CaptureCallbackAvgIntervalMs { get; init; }
        public double CaptureCallbackMaxIntervalMs { get; init; }
        public long CaptureCallbackSevereGapCount { get; init; }
        public long CaptureAudioDiscontinuityCount { get; init; }
        public long CaptureAudioTimestampErrorCount { get; init; }
        public long CaptureAudioGlitchCount { get; init; }
        public int CaptureCallbackSilenceCount { get; init; }
        public long CaptureLastCallbackTickMs { get; init; }
        public long CaptureAudioLevelEventsFired { get; init; }
        public long CaptureAudioLevelLastFireTickMs { get; init; }
        public long PlaybackRenderCallbackCount { get; init; }
        public int PlaybackRenderSilenceCount { get; init; }
        public int PlaybackQueueDepth { get; init; }
        public int PlaybackQueueDropCount { get; init; }
        public double PlaybackQueueDurationMs { get; init; }
        public double PlaybackActiveChunkDurationMs { get; init; }
        public double PlaybackEndpointQueuedDurationMs { get; init; }
        public double PlaybackBufferedDurationMs { get; init; }
        public double PlaybackStreamLatencyMs { get; init; }
        public long PlaybackLastRenderTickMs { get; init; }
    }

    private static WasapiCaptureFlattenedProjection BuildWasapiCaptureFlattenedProjection(
        WasapiAudioProjection wasapi)
        => new()
        {
            CallbackCount = wasapi.CaptureCallbackCount,
            CallbackAvgIntervalMs = wasapi.CaptureCallbackAvgIntervalMs,
            CallbackMaxIntervalMs = wasapi.CaptureCallbackMaxIntervalMs,
            CallbackSevereGapCount = wasapi.CaptureCallbackSevereGapCount,
            AudioDiscontinuityCount = wasapi.CaptureAudioDiscontinuityCount,
            AudioTimestampErrorCount = wasapi.CaptureAudioTimestampErrorCount,
            AudioGlitchCount = wasapi.CaptureAudioGlitchCount,
            CallbackSilenceCount = wasapi.CaptureCallbackSilenceCount,
            LastCallbackTickMs = wasapi.CaptureLastCallbackTickMs,
            AudioLevelEventsFired = wasapi.CaptureAudioLevelEventsFired,
            AudioLevelLastFireTickMs = wasapi.CaptureAudioLevelLastFireTickMs
        };

    private readonly record struct WasapiCaptureFlattenedProjection
    {
        public long CallbackCount { get; init; }
        public double CallbackAvgIntervalMs { get; init; }
        public double CallbackMaxIntervalMs { get; init; }
        public long CallbackSevereGapCount { get; init; }
        public long AudioDiscontinuityCount { get; init; }
        public long AudioTimestampErrorCount { get; init; }
        public long AudioGlitchCount { get; init; }
        public int CallbackSilenceCount { get; init; }
        public long LastCallbackTickMs { get; init; }
        public long AudioLevelEventsFired { get; init; }
        public long AudioLevelLastFireTickMs { get; init; }
    }

    private static WasapiPlaybackFlattenedProjection BuildWasapiPlaybackFlattenedProjection(
        WasapiAudioProjection wasapi)
        => new()
        {
            RenderCallbackCount = wasapi.PlaybackRenderCallbackCount,
            RenderSilenceCount = wasapi.PlaybackRenderSilenceCount,
            QueueDepth = wasapi.PlaybackQueueDepth,
            QueueDropCount = wasapi.PlaybackQueueDropCount,
            QueueDurationMs = wasapi.PlaybackQueueDurationMs,
            ActiveChunkDurationMs = wasapi.PlaybackActiveChunkDurationMs,
            EndpointQueuedDurationMs = wasapi.PlaybackEndpointQueuedDurationMs,
            BufferedDurationMs = wasapi.PlaybackBufferedDurationMs,
            StreamLatencyMs = wasapi.PlaybackStreamLatencyMs,
            LastRenderTickMs = wasapi.PlaybackLastRenderTickMs
        };

    private readonly record struct WasapiPlaybackFlattenedProjection
    {
        public long RenderCallbackCount { get; init; }
        public int RenderSilenceCount { get; init; }
        public int QueueDepth { get; init; }
        public int QueueDropCount { get; init; }
        public double QueueDurationMs { get; init; }
        public double ActiveChunkDurationMs { get; init; }
        public double EndpointQueuedDurationMs { get; init; }
        public double BufferedDurationMs { get; init; }
        public double StreamLatencyMs { get; init; }
        public long LastRenderTickMs { get; init; }
    }

    private static AudioDropsProjection BuildAudioDropsProjection(CaptureHealthSnapshot health)
        => new()
        {
            QueueSaturated = health.AudioDropsQueueSaturated,
            BacklogEviction = health.AudioDropsBacklogEviction,
            ChunksDropped = health.AudioChunksDropped,
            QueueDropsRealtime = health.AudioDropsQueueSaturated + health.AudioDropsBacklogEviction,
            QueueDropsFileWriter = health.AudioChunksDropped
        };

    private static AudioDropsFlattenedProjection BuildAudioDropsFlattenedProjection(AudioDropsProjection audioDrops)
        => new()
        {
            QueueSaturated = audioDrops.QueueSaturated,
            BacklogEviction = audioDrops.BacklogEviction,
            ChunksDropped = audioDrops.ChunksDropped,
            QueueDropsRealtime = audioDrops.QueueDropsRealtime,
            QueueDropsFileWriter = audioDrops.QueueDropsFileWriter
        };

    private readonly record struct AudioDropsProjection
    {
        public long QueueSaturated { get; init; }
        public long BacklogEviction { get; init; }
        public long ChunksDropped { get; init; }
        public long QueueDropsRealtime { get; init; }
        public long QueueDropsFileWriter { get; init; }
    }

    private readonly record struct AudioDropsFlattenedProjection
    {
        public long QueueSaturated { get; init; }
        public long BacklogEviction { get; init; }
        public long ChunksDropped { get; init; }
        public long QueueDropsRealtime { get; init; }
        public long QueueDropsFileWriter { get; init; }
    }

    private static AudioAndIngestFlattenedProjection BuildAudioAndIngestFlattenedProjection(
        AudioAndIngestProjection audioAndIngest)
        => new()
        {
            Signal = BuildAudioSignalFlattenedProjection(audioAndIngest.Signal),
            Ingest = BuildCaptureIngestFlattenedProjection(audioAndIngest.Ingest),
            SourceReader = BuildSourceReaderFlattenedProjection(audioAndIngest.Ingest),
            WasapiCapture = BuildWasapiCaptureFlattenedProjection(audioAndIngest.Wasapi),
            WasapiPlayback = BuildWasapiPlaybackFlattenedProjection(audioAndIngest.Wasapi)
        };

    private readonly record struct AudioAndIngestFlattenedProjection
    {
        public AudioSignalFlattenedProjection Signal { get; init; }
        public CaptureIngestFlattenedProjection Ingest { get; init; }
        public SourceReaderFlattenedProjection SourceReader { get; init; }
        public WasapiCaptureFlattenedProjection WasapiCapture { get; init; }
        public WasapiPlaybackFlattenedProjection WasapiPlayback { get; init; }
    }
}

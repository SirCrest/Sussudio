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

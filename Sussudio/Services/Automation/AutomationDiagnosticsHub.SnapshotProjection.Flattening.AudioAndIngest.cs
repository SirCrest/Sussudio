namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
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

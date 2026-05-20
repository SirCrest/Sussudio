namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static AudioSignalFlattenedProjection BuildAudioSignalFlattenedProjection(
        AudioAndIngestProjection audioAndIngest)
        => new()
        {
            Peak = audioAndIngest.AudioPeak,
            Clipping = audioAndIngest.AudioClipping,
            SignalPresent = audioAndIngest.AudioSignalPresent,
            MutedSuspected = audioAndIngest.AudioMutedSuspected
        };

    private readonly record struct AudioSignalFlattenedProjection
    {
        public double Peak { get; init; }
        public bool Clipping { get; init; }
        public bool SignalPresent { get; init; }
        public bool MutedSuspected { get; init; }
    }
}

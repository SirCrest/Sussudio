namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
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
}

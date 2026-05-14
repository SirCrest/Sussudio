using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
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
}

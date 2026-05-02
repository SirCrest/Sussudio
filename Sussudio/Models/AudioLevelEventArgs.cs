using System;

namespace Sussudio.Models;

public sealed class AudioLevelEventArgs : EventArgs
{
    public AudioLevelEventArgs(double peak, double rms, bool clipped)
    {
        Peak = peak;
        Rms = rms;
        Clipped = clipped;
    }

    public double Peak { get; }
    public double Rms { get; }
    public bool Clipped { get; }
}

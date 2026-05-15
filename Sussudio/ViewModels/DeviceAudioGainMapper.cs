using System;

namespace Sussudio.ViewModels;

internal static class DeviceAudioGainMapper
{
    private const double GainCurveK = 4.0;

    internal static byte PercentToGainByte(double percent)
    {
        var x = Math.Clamp(percent / 100.0, 0.0, 1.0);
        var curved = Math.Log(1.0 + x * (Math.Exp(GainCurveK) - 1.0)) / GainCurveK;
        return (byte)Math.Clamp(Math.Round(curved * 255.0), 0, 255);
    }

    internal static double GainByteToPercent(byte gainByte)
    {
        var y = gainByte / 255.0;
        var x = (Math.Exp(GainCurveK * y) - 1.0) / (Math.Exp(GainCurveK) - 1.0);
        return Math.Clamp(x * 100.0, 0.0, 100.0);
    }
}

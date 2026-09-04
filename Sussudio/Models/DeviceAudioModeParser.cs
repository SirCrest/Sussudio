using System;

namespace Sussudio.Models;

internal static class DeviceAudioModeParser
{
    public static string NormalizeOrThrow(string? mode)
    {
        if (string.Equals(mode, DeviceAudioMode.Hdmi, StringComparison.OrdinalIgnoreCase))
        {
            return DeviceAudioMode.Hdmi;
        }

        if (string.Equals(mode, DeviceAudioMode.Analog, StringComparison.OrdinalIgnoreCase))
        {
            return DeviceAudioMode.Analog;
        }

        throw new InvalidOperationException("Device audio mode must be either 'HDMI' or 'Analog'.");
    }
}

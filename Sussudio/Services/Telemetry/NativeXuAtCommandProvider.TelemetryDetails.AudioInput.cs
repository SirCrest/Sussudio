using System.Globalization;
using Sussudio.Models;

namespace Sussudio.Services.Telemetry;

public sealed partial class NativeXuAtCommandProvider
{
    /// <summary>
    /// Validates flash audio data: [source, 0x80, gainByte, 0xAA, 0x55, ...].
    /// Byte[2] is the gain value (0x00-0xFF), NOT part of the magic signature.
    /// </summary>
    private static bool IsValidFlashAudioData(AtCommandResult flashResult)
        => flashResult.Success && flashResult.Response.Length >= 5 &&
           flashResult.Response[1] == 0x80 &&
           flashResult.Response[3] == 0xAA && flashResult.Response[4] == 0x55;

    /// <summary>
    /// Resolves the audio input source from flash proprietary data (AT 0x52).
    /// The flash response contains 0x80, the gain byte, 0xAA, 0x55 at bytes 1-4
    /// and the source byte at offset 0: 0x00=HDMI, 0x01=Analog.
    /// Falls back to the AT 0x35 telemetry value if flash read fails.
    /// </summary>
    private static string? ResolveAudioInputSource(AtCommandResult flashResult, string? fallback)
    {
        if (IsValidFlashAudioData(flashResult))
        {
            return flashResult.Response[0] == 0 ? DeviceAudioMode.Hdmi : DeviceAudioMode.Analog;
        }

        return fallback;
    }

    private static SourceAudioInputMode? ResolveAudioInputMode(AtCommandResult flashResult, AtCommandResult inputSourceResult)
    {
        if (IsValidFlashAudioData(flashResult))
        {
            return flashResult.Response[0] == 0 ? SourceAudioInputMode.Hdmi : SourceAudioInputMode.Analog;
        }

        if (inputSourceResult.Success && inputSourceResult.Response.Length >= 1)
        {
            return inputSourceResult.Response[0] == 0 ? SourceAudioInputMode.Hdmi : SourceAudioInputMode.Analog;
        }

        return null;
    }

    private static (string Value, string? RawValue) FormatInputSourceDetail(byte[] data)
    {
        var raw = data[0].ToString(CultureInfo.InvariantCulture);
        var value = data[0] switch
        {
            0 => DeviceAudioMode.Hdmi,
            1 => DeviceAudioMode.Analog,
            _ => "Unknown"
        };
        return (value, raw);
    }
}

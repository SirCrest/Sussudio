using System;
using System.Globalization;

namespace Sussudio.Services.Telemetry;

public sealed partial class NativeXuAtCommandProvider
{
    private static (string Value, string? RawValue) FormatAudioFormatDetail(byte[] data)
    {
        var raw = data[0].ToString(CultureInfo.InvariantCulture);
        return ("Unknown", raw);
    }

    private static (string Value, string? RawValue) FormatAudioSampleRateDetail(byte[] data)
    {
        var raw = data[0].ToString(CultureInfo.InvariantCulture);
        return ("Unknown", raw);
    }

    private static (string Value, string? RawValue) FormatUsbHostProtocolDetail(byte[] data)
    {
        if (data.Length < 4)
        {
            return ("Unavailable", null);
        }

        var rawValue = BitConverter.ToInt32(data, 0);
        var raw = rawValue.ToString(CultureInfo.InvariantCulture);
        var value = rawValue switch
        {
            0 => "Undefined",
            1 => "Bulk",
            2 => "Isochronous",
            _ => "Unknown"
        };
        return (value, raw);
    }

    private static (string Value, string? RawValue) FormatHdcpModeDetail(byte[] data)
    {
        var raw = data[0].ToString(CultureInfo.InvariantCulture);
        return ("Unknown", raw);
    }

    private static (string Value, string? RawValue) FormatHdcpVersionDetail(byte[] data)
    {
        var raw = Convert.ToHexString(data);
        return (raw, raw);
    }

    private static (string Value, string? RawValue) FormatRxTxHdcpVersionDetail(byte[] data)
    {
        if (data.Length < 2)
        {
            return ("Unavailable", null);
        }

        var raw = BitConverter.ToInt16(data, 0).ToString(CultureInfo.InvariantCulture);
        return ("Unknown", raw);
    }

    private static (string Value, string? RawValue) FormatCodeByteDetail(byte[] data)
    {
        var raw = data[0].ToString(CultureInfo.InvariantCulture);
        return ($"Code {raw}", raw);
    }

    private static (string Value, string? RawValue) FormatOnOffByteDetail(byte[] data)
    {
        var raw = data[0].ToString(CultureInfo.InvariantCulture);
        var value = data[0] switch
        {
            0 => "Off",
            1 => "On",
            _ => "Unknown"
        };
        return (value, raw);
    }

    private static (string Value, string? RawValue) FormatMuteByteDetail(byte[] data)
    {
        var raw = data[0].ToString(CultureInfo.InvariantCulture);
        var value = data[0] switch
        {
            0 => "Unmuted",
            1 => "Muted",
            _ => "Unknown"
        };
        return (value, raw);
    }

    private static (string Value, string? RawValue) FormatValidByteDetail(byte[] data)
    {
        var raw = data[0].ToString(CultureInfo.InvariantCulture);
        var value = data[0] switch
        {
            0 => "Invalid",
            1 => "Valid",
            _ => "Unknown"
        };
        return (value, raw);
    }

    private static (string Value, string? RawValue) FormatDecimalInt16Detail(byte[] data)
    {
        if (data.Length < 2)
        {
            return ("Unavailable", null);
        }

        var raw = BitConverter.ToInt16(data, 0).ToString(CultureInfo.InvariantCulture);
        return (raw, raw);
    }

    private static (string Value, string? RawValue) FormatDecimalInt32Detail(byte[] data)
    {
        if (data.Length < 4)
        {
            return ("Unavailable", null);
        }

        var raw = BitConverter.ToInt32(data, 0).ToString(CultureInfo.InvariantCulture);
        return (raw, raw);
    }

    private static (string Value, string? RawValue) FormatModeInt16Detail(byte[] data)
    {
        if (data.Length < 2)
        {
            return ("Unavailable", null);
        }

        var raw = BitConverter.ToInt16(data, 0).ToString(CultureInfo.InvariantCulture);
        return ($"Mode {raw}", raw);
    }

    private static (string Value, string? RawValue) FormatModeInt32Detail(byte[] data)
    {
        if (data.Length < 4)
        {
            return ("Unavailable", null);
        }

        var raw = BitConverter.ToInt32(data, 0).ToString(CultureInfo.InvariantCulture);
        return ($"Mode {raw}", raw);
    }

    private static (string Value, string? RawValue) FormatHexDetail(byte[] data)
    {
        var raw = Convert.ToHexString(data);
        return (raw, raw);
    }

    private static (string Value, string? RawValue) FormatAsciiOrHexDetail(byte[] data)
    {
        var raw = Convert.ToHexString(data);
        var ascii = TryDecodePrintableAscii(data);
        return string.IsNullOrWhiteSpace(ascii)
            ? (raw, raw)
            : (ascii, raw);
    }
}

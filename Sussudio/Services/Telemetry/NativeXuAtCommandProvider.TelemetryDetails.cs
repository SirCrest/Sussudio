using System;
using System.Collections.Generic;
using System.Globalization;
using Sussudio.Models;

namespace Sussudio.Services.Telemetry;

public sealed partial class NativeXuAtCommandProvider
{
    private static IReadOnlyList<SourceTelemetryDetailEntry> BuildDetailEntries(
        AviInfoFrameInfo aviInfoFrame,
        HdrMetadataInfo hdrInfo,
        byte? hdr2SdrState,
        string? systemInfo,
        AtCommandResult audioFormat,
        AtCommandResult audioSamplingRate,
        AtCommandResult inputSource,
        AtCommandResult adcOnOff,
        AtCommandResult adcVolumeGain,
        AtCommandResult uacVolumeGain,
        AtCommandResult uacOut1Mute,
        AtCommandResult uacOut2Mute,
        AtCommandResult uacOut2MixerSource,
        AtCommandResult usbHostProtocol,
        AtCommandResult usbCdc,
        AtCommandResult usbLinkState,
        AtCommandResult usbForceSpeed,
        AtCommandResult txHpd,
        AtCommandResult txVrr,
        AtCommandResult txEdidValid,
        AtCommandResult uvcOutputTiming,
        AtCommandResult uvcVideoFormat,
        AtCommandResult uvcErrStatus,
        AtCommandResult hdcpMode,
        AtCommandResult hdcpVersion,
        AtCommandResult rxTxHdcpVersion,
        AtCommandResult hdr2SdrExtended,
        AtCommandResult customerVersion,
        AtCommandResult rescueVersion,
        AtCommandResult hdr2SdrColorParam,
        AtCommandResult colorRangeSetting,
        AtCommandResult rawTiming,
        int? vicCode,
        int? vfreqHz100)
    {
        var details = new List<SourceTelemetryDetailEntry>();

        AddDetail(details, "Signal Details", "Video Format", aviInfoFrame.ColorSpace);
        AddDetail(details, "Signal Details", "Colorimetry", aviInfoFrame.Colorimetry);
        AddDetail(details, "Signal Details", "Quantization", aviInfoFrame.Quantization);
        AddDetail(
            details,
            "Signal Details",
            "HDR Transfer",
            ResolveHdrTransferFunction(hdrInfo.Eotf),
            hdrInfo.Eotf?.ToString(CultureInfo.InvariantCulture));
        AddDetail(
            details,
            "Signal Details",
            "HDR to SDR",
            hdr2SdrState switch
            {
                0 => "Off",
                1 => "On",
                _ => null
            },
            hdr2SdrState?.ToString(CultureInfo.InvariantCulture));
        AddDetail(details, "Signal Details", "VIC", vicCode?.ToString(CultureInfo.InvariantCulture));
        AddDetail(details, "Signal Details", "Vert Freq", vfreqHz100.HasValue ? $"{vfreqHz100.Value / 100.0:0.##} Hz" : null, vfreqHz100?.ToString(CultureInfo.InvariantCulture));

        AddAtDetail(details, TelemetryLabels.GroupAudioInput, "Input Source", inputSource, FormatInputSourceDetail);
        AddAtDetail(details, TelemetryLabels.GroupAudioInput, "Audio Format", audioFormat, FormatAudioFormatDetail);
        AddAtDetail(details, TelemetryLabels.GroupAudioInput, "Audio Sample Rate", audioSamplingRate, FormatAudioSampleRateDetail);
        AddAtDetail(details, TelemetryLabels.GroupAudioInput, TelemetryLabels.AdcAnalog, adcOnOff, FormatOnOffByteDetail);
        AddAtDetail(details, TelemetryLabels.GroupAudioInput, "ADC Gain", adcVolumeGain, FormatDecimalInt16Detail);

        AddAtDetail(details, "Audio / USB", "UAC Volume", uacVolumeGain, FormatDecimalInt16Detail);
        AddAtDetail(details, "Audio / USB", "UAC Out1 Mute", uacOut1Mute, FormatMuteByteDetail);
        AddAtDetail(details, "Audio / USB", "UAC Out2 Mute", uacOut2Mute, FormatMuteByteDetail);
        AddAtDetail(details, "Audio / USB", "UAC Out2 Mixer", uacOut2MixerSource, FormatDecimalInt16Detail);

        AddAtDetail(details, "Link / Protection", "USB Protocol", usbHostProtocol, FormatUsbHostProtocolDetail);
        AddAtDetail(details, "Link / Protection", "USB CDC", usbCdc, FormatCodeByteDetail);
        AddAtDetail(details, "Link / Protection", "USB Link State", usbLinkState, FormatCodeByteDetail);
        AddAtDetail(details, "Link / Protection", "USB Speed", usbForceSpeed, FormatCodeByteDetail);
        AddAtDetail(details, "Link / Protection", "TX Hot Plug", txHpd, FormatModeInt32Detail);
        AddAtDetail(details, "Link / Protection", "TX VRR", txVrr, FormatModeInt32Detail);
        AddAtDetail(details, "Link / Protection", "TX EDID Valid", txEdidValid, FormatValidByteDetail);
        AddAtDetail(details, "Link / Protection", "HDCP Mode", hdcpMode, FormatHdcpModeDetail);
        AddAtDetail(details, "Link / Protection", "HDCP Version", hdcpVersion, FormatHdcpVersionDetail);
        AddAtDetail(details, "Link / Protection", "RX/TX HDCP", rxTxHdcpVersion, FormatRxTxHdcpVersionDetail);

        AddAtDetail(details, "Capture Card / UVC", "UVC Timing", uvcOutputTiming, FormatHexDetail);
        AddAtDetail(details, "Capture Card / UVC", "UVC Format", uvcVideoFormat, FormatHexDetail);
        AddAtDetail(details, "Capture Card / UVC", "UVC Error", uvcErrStatus, FormatCodeByteDetail);

        AddAtDetail(details, "Raw / Firmware", "HDR2SDR Status", hdr2SdrExtended, FormatModeInt32Detail);
        AddAtDetail(details, "Raw / Firmware", "Customer Version", customerVersion, FormatAsciiOrHexDetail);
        AddAtDetail(details, "Raw / Firmware", "Rescue Version", rescueVersion, FormatDecimalInt32Detail);
        AddAtDetail(details, "Raw / Firmware", "HDR2SDR Color", hdr2SdrColorParam, FormatHexDetail);
        AddAtDetail(details, "Raw / Firmware", "Color Range", colorRangeSetting, FormatCodeByteDetail);
        AddAtDetail(details, "Raw / Firmware", "Raw Timing", rawTiming, FormatHexDetail);

        return details;
    }

    private static void AddDetail(
        ICollection<SourceTelemetryDetailEntry> details,
        string group,
        string label,
        string? value,
        string? rawValue = null)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        var displayValue = string.IsNullOrWhiteSpace(rawValue) || string.Equals(value, rawValue, StringComparison.OrdinalIgnoreCase)
            ? value
            : $"{value} ({rawValue})";

        details.Add(new SourceTelemetryDetailEntry(group, label, displayValue, rawValue));
    }

    private static void AddAtDetail(
        ICollection<SourceTelemetryDetailEntry> details,
        string group,
        string label,
        AtCommandResult result,
        Func<byte[], (string Value, string? RawValue)> formatter)
    {
        if (!result.Success || result.Response.Length == 0)
        {
            details.Add(new SourceTelemetryDetailEntry(group, label, "Unavailable", result.FailureStage));
            return;
        }

        var formatted = formatter(result.Response);
        AddDetail(details, group, label, formatted.Value, formatted.RawValue);
    }

    private static string? TryFormatAtDetailValue(
        AtCommandResult result,
        Func<byte[], (string Value, string? RawValue)> formatter)
    {
        if (!result.Success || result.Response.Length == 0)
        {
            return null;
        }

        return BuildDisplayValue(formatter(result.Response));
    }

    private static string? ResolveHdrTransferFunction(byte? eotf)
        => eotf switch
        {
            0 => "SDR",
            1 => "Traditional HDR",
            2 => "HDR10 / PQ",
            3 => "HLG",
            _ => eotf.HasValue ? "Unknown" : null
        };

    private static string? BuildDisplayValue((string Value, string? RawValue) formatted)
    {
        if (string.IsNullOrWhiteSpace(formatted.Value))
        {
            return null;
        }

        return string.IsNullOrWhiteSpace(formatted.RawValue) ||
               string.Equals(formatted.Value, formatted.RawValue, StringComparison.OrdinalIgnoreCase)
            ? formatted.Value
            : $"{formatted.Value} ({formatted.RawValue})";
    }

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

    private static string ResolveSnapshotAudioInputOrigin(
        AtCommandResult flashAudioResult,
        AtCommandResult inputSourceResult,
        bool useDetailedAudioInputOrigin)
    {
        if (useDetailedAudioInputOrigin)
        {
            return flashAudioResult.Success && flashAudioResult.Response.Length >= 5
                ? $"NativeXu:Flash=0x{flashAudioResult.Response[0]:X2}"
                : (inputSourceResult.Success && inputSourceResult.Response.Length >= 1
                    ? $"NativeXu:InputSource={inputSourceResult.Response[0]}"
                    : "not-implemented");
        }

        return flashAudioResult.Success && flashAudioResult.Response.Length >= 5
            ? "nativexu-flash-audio"
            : (inputSourceResult.Success ? "nativexu-input-source" : "unknown");
    }

    private static int? ResolveAnalogGainByte(AtCommandResult flashResult)
        => IsValidFlashAudioData(flashResult)
            ? flashResult.Response[2]
            : null;

    private static IReadOnlyList<SourceTelemetryDetailEntry> AppendFlashAudioAnalogGainDetail(
        IReadOnlyList<SourceTelemetryDetailEntry> detailEntries,
        AtCommandResult flashResult)
    {
        var analogGainByte = ResolveAnalogGainByte(flashResult);
        if (!analogGainByte.HasValue)
        {
            return detailEntries;
        }

        var mutable = new List<SourceTelemetryDetailEntry>(detailEntries);
        var lastAudioIdx = mutable.FindLastIndex(d => d.Group == TelemetryLabels.GroupAudioInput);
        var insertIdx = lastAudioIdx >= 0 ? lastAudioIdx + 1 : mutable.Count;
        mutable.Insert(insertIdx,
            new SourceTelemetryDetailEntry(
                TelemetryLabels.GroupAudioInput,
                TelemetryLabels.AnalogGain,
                FormatAnalogGainDisplayValue((byte)analogGainByte.Value),
                analogGainByte.Value.ToString(CultureInfo.InvariantCulture)));
        return mutable;
    }

    private static string FormatAnalogGainDisplayValue(byte gainByte)
    {
        var y = gainByte / 255.0;
        var gainPct = (Math.Exp(4.0 * y) - 1.0) / (Math.Exp(4.0) - 1.0) * 100.0;
        return $"0x{gainByte:X2} ({gainPct:0}%)";
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

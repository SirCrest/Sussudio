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
}

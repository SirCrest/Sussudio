using System;
using System.Globalization;
using System.Text;

namespace Sussudio.Services.Telemetry;

public sealed partial class NativeXuAtCommandProvider
{
    private static string BuildDiagnosticSummary(
        int? vicCode,
        VicTiming? timing,
        double? frameRateExact,
        HdrMetadataInfo hdrInfo,
        AviInfoFrameInfo aviInfoFrame,
        int? vfreqHz100,
        byte? hdr2SdrState,
        string? systemInfo)
    {
        var resolutionToken = timing.HasValue
            ? $"{timing.Value.Width}x{timing.Value.Height}{(timing.Value.IsInterlaced ? "i" : "p")}"
            : "unknown";
        var hdr2SdrToken = hdr2SdrState.HasValue
            ? (hdr2SdrState.Value == 1 ? "on" : "off")
            : "unknown";

        return string.Join(
            ":",
            "nativexu",
            $"vic={(vicCode.HasValue ? vicCode.Value.ToString(CultureInfo.InvariantCulture) : "unknown")}",
            resolutionToken,
            FormatFrameRate(frameRateExact),
            hdrInfo.IsHdr switch
            {
                true => "hdr",
                false => "sdr",
                _ => "unknown"
            },
            $"vfreq={(vfreqHz100.HasValue ? vfreqHz100.Value.ToString(CultureInfo.InvariantCulture) : "unknown")}",
            aviInfoFrame.ColorSpace ?? "unknown-space",
            aviInfoFrame.Colorimetry ?? "unknown-color",
            $"quant={aviInfoFrame.Quantization ?? "unknown"}",
            $"hdr2sdr={hdr2SdrToken}",
            $"eotf={(hdrInfo.Eotf.HasValue ? hdrInfo.Eotf.Value.ToString(CultureInfo.InvariantCulture) : "unknown")}",
            $"fw={systemInfo ?? "unknown"}");
    }

    private static string AppendExtendedDiagnostics(
        string baseSummary,
        AtCommandResult audioFormat,
        AtCommandResult audioSamplingRate,
        AtCommandResult inputSource,
        AtCommandResult usbHostProtocol,
        AtCommandResult usbCdc,
        AtCommandResult usbLinkState,
        AtCommandResult usbForceSpeed,
        AtCommandResult txHpd,
        AtCommandResult txVrr,
        AtCommandResult uvcOutputTiming,
        AtCommandResult uvcVideoFormat,
        AtCommandResult uvcErrStatus,
        AtCommandResult hdcpMode,
        AtCommandResult hdcpVersion,
        AtCommandResult rxTxHdcpVersion,
        AtCommandResult hdr2SdrExtended,
        AtCommandResult hdr2SdrColorParam,
        AtCommandResult colorRangeSetting,
        AtCommandResult vtem,
        AtCommandResult bitError,
        AtCommandResult rawTiming)
    {
        var sb = new StringBuilder(baseSummary);

        AppendResultField(sb, "audiofmt", audioFormat, FormatByte);
        AppendResultField(sb, "audiosrate", audioSamplingRate, FormatByte);
        AppendResultField(sb, "inputsrc", inputSource, FormatByte);
        AppendResultField(sb, "usbproto", usbHostProtocol, FormatInt32);
        AppendResultField(sb, "usbcdc", usbCdc, FormatByte);
        AppendResultField(sb, "usblinkst", usbLinkState, FormatByte);
        AppendResultField(sb, "usbspeed", usbForceSpeed, FormatByte);
        AppendResultField(sb, "txhpd", txHpd, FormatInt32);
        AppendResultField(sb, "txvrr", txVrr, FormatInt32);
        AppendResultField(sb, "uvctiming", uvcOutputTiming, FormatHex);
        AppendResultField(sb, "uvcfmt", uvcVideoFormat, FormatByte);
        AppendResultField(sb, "uvcerr", uvcErrStatus, FormatByte);
        AppendResultField(sb, "hdcpmode", hdcpMode, FormatByte);
        AppendResultField(sb, "hdcpver", hdcpVersion, FormatHex);
        AppendResultField(sb, "rxtxhdcp", rxTxHdcpVersion, FormatInt16);
        AppendResultField(sb, "hdr2sdrext", hdr2SdrExtended, FormatInt32);
        AppendResultField(sb, "hdr2sdrcolor", hdr2SdrColorParam, FormatInt32);
        AppendResultField(sb, "colorrangesetting", colorRangeSetting, FormatByte);
        AppendResultField(sb, "vtem", vtem, FormatInt16);
        AppendResultField(sb, "biterr", bitError, FormatInt64);
        AppendResultField(sb, "rawtiming", rawTiming, FormatHex);

        return sb.ToString();
    }

    private static void AppendResultField(StringBuilder sb, string key, AtCommandResult result, Func<byte[], string> formatter)
    {
        sb.Append(':');
        sb.Append(key);
        sb.Append('=');
        if (result.Success && result.Response.Length > 0)
        {
            sb.Append(formatter(result.Response));
        }
        else
        {
            sb.Append("n/a");
        }
    }

    private static string FormatByte(byte[] data)
        => data.Length >= 1 ? data[0].ToString(CultureInfo.InvariantCulture) : "n/a";

    private static string FormatInt16(byte[] data)
        => data.Length >= 2 ? BitConverter.ToInt16(data, 0).ToString(CultureInfo.InvariantCulture) : "n/a";

    private static string FormatInt32(byte[] data)
        => data.Length >= 4 ? BitConverter.ToInt32(data, 0).ToString(CultureInfo.InvariantCulture) : "n/a";

    private static string FormatInt64(byte[] data)
        => data.Length >= 8 ? BitConverter.ToInt64(data, 0).ToString(CultureInfo.InvariantCulture) : "n/a";

    private static string FormatHex(byte[] data)
        => data.Length > 0 ? Convert.ToHexString(data) : "n/a";

    private static string FormatFrameRate(double? value)
        => value.HasValue && value.Value > 0
            ? value.Value.ToString("0.###", CultureInfo.InvariantCulture)
            : "unknown";
}

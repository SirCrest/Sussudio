using System;
using System.Globalization;
using Sussudio.Models;

namespace Sussudio.Services.Telemetry;

public sealed partial class NativeXuAtCommandProvider
{
    private static NodeReadAttempt BuildSnapshotFromCommandResults(
        NativeXuSnapshotCommandResults results,
        string interfacePath,
        int nodeId,
        bool logDecodeSummary,
        bool logNoDecodableSourceData,
        bool useDetailedAudioInputOrigin)
    {
        var aviInfo = results.AviInfo.Success ? DecodeAviInfoFrame(results.AviInfo.Response) : AviInfoFrameInfo.Empty;
        var hdrInfo = results.HdrMetadata.Success ? DecodeHdrMetadata(results.HdrMetadata.Response) : new HdrMetadataInfo(false, null, null);
        if (results.HdrMetadata.Success && !hdrInfo.HasMetadata)
        {
            hdrInfo = new HdrMetadataInfo(true, 0, false);
        }

        var systemInfo = results.SystemInfo.Success ? DecodeCString(results.SystemInfo.Response) : null;
        var vicCode = results.Vic.Success ? ExtractInt32AsVicCode(results.Vic.Response) : null;
        var vfreqHz100 = results.Vfreq.Success && TryReadInt32(results.Vfreq.Response, out var vfreqRaw) ? (int?)vfreqRaw : null;
        var timing = vicCode.HasValue && VicTimingMap.TryGetValue(vicCode.Value, out var mappedTiming)
            ? mappedTiming
            : (VicTiming?)null;
        double? frameRateExact;
        if (vfreqHz100.HasValue && vfreqHz100.Value > 0)
        {
            frameRateExact = SnapToCanonicalFrameRate(vfreqHz100.Value / 100.0);
        }
        else if (timing.HasValue)
        {
            frameRateExact = SnapToCanonicalFrameRate(timing.Value.NominalFrameRate);
        }
        else
        {
            frameRateExact = null;
        }

        var hdr2SdrState = results.Hdr2Sdr.Success && TryReadInt32(results.Hdr2Sdr.Response, out var hdr2SdrValue)
            ? (hdr2SdrValue == 1 ? (byte)1 : (byte)0)
            : (byte?)null;
        var adcOnOff = TryReadBoolean(results.AdcOnOff.Success ? results.AdcOnOff.Response : Array.Empty<byte>());
        var adcVolumeGain = TryReadInt16(results.AdcVolumeGain.Success ? results.AdcVolumeGain.Response : Array.Empty<byte>());
        var uacVolumeGain = TryReadInt16(results.UacVolumeGain.Success ? results.UacVolumeGain.Response : Array.Empty<byte>());
        var uacOut1Mute = TryReadBoolean(results.UacOut1Mute.Success ? results.UacOut1Mute.Response : Array.Empty<byte>());
        var uacOut2Mute = TryReadBoolean(results.UacOut2Mute.Success ? results.UacOut2Mute.Response : Array.Empty<byte>());
        var uacOut2MixerSource = TryReadInt16(results.UacOut2MixerSource.Success ? results.UacOut2MixerSource.Response : Array.Empty<byte>());
        var sourceAudioFormat = TryFormatAtDetailValue(results.AudioFormat, FormatAudioFormatDetail);
        var sourceAudioSampleRate = TryFormatAtDetailValue(results.AudioSamplingRate, FormatAudioSampleRateDetail);
        var sourceInputSource = TryFormatAtDetailValue(results.InputSource, FormatInputSourceDetail);
        var sourceUsbHostProtocol = TryFormatAtDetailValue(results.UsbHostProtocol, FormatUsbHostProtocolDetail);
        var txEdidValid = TryReadBoolean(results.TxEdidValid.Success ? results.TxEdidValid.Response : Array.Empty<byte>());
        var sourceHdcpMode = TryFormatAtDetailValue(results.HdcpMode, FormatHdcpModeDetail);
        var sourceHdcpVersion = TryFormatAtDetailValue(results.HdcpVersion, FormatHdcpVersionDetail);
        var sourceRxTxHdcpVersion = TryFormatAtDetailValue(results.RxTxHdcpVersion, FormatRxTxHdcpVersionDetail);
        var customerVersion = TryFormatAtDetailValue(results.CustomerVersion, FormatAsciiOrHexDetail);
        var rescueVersion = results.RescueVersion.Success && TryReadInt32(results.RescueVersion.Response, out var rescueVersionValue)
            ? (int?)rescueVersionValue
            : null;
        var sourceRawTimingHex = results.RawTiming.Success && results.RawTiming.Response.Length > 0
            ? Convert.ToHexString(results.RawTiming.Response)
            : null;

        if (!vicCode.HasValue && !timing.HasValue && !frameRateExact.HasValue && !hdrInfo.HasMetadata && !aviInfo.HasData)
        {
            if (logNoDecodableSourceData)
            {
                Logger.Log($"NATIVEXU_SIGNAL_UNAVAILABLE path='{interfacePath}' node={nodeId} reason=no-decodable-source-data");
            }

            return new NodeReadAttempt(null, false, "nativexu-no-signal-data", $"{interfacePath}: node={nodeId}");
        }

        if (logDecodeSummary)
        {
            Logger.Log(
                $"NATIVEXU_DECODE vic={(vicCode.HasValue ? vicCode.Value.ToString(CultureInfo.InvariantCulture) : "none")} " +
                $"size={timing?.Width.ToString(CultureInfo.InvariantCulture) ?? "?"}x{timing?.Height.ToString(CultureInfo.InvariantCulture) ?? "?"} " +
                $"fps={(frameRateExact.HasValue ? frameRateExact.Value.ToString("0.###", CultureInfo.InvariantCulture) : "?")} " +
                $"vfreq={(vfreqHz100.HasValue ? vfreqHz100.Value.ToString(CultureInfo.InvariantCulture) : "?")} " +
                $"hdr={BoolToToken(hdrInfo.IsHdr)} colorspace={aviInfo.ColorSpace ?? "unknown"} " +
                $"colorimetry={aviInfo.Colorimetry ?? "unknown"} firmware={systemInfo ?? "unknown"}");
        }

        var baseDiagnosticSummary = BuildDiagnosticSummary(vicCode, timing, frameRateExact, hdrInfo, aviInfo, vfreqHz100, hdr2SdrState, systemInfo);
        var fullDiagnosticSummary = AppendExtendedDiagnostics(
            baseDiagnosticSummary,
            results.AudioFormat, results.AudioSamplingRate, results.InputSource,
            results.UsbHostProtocol, results.UsbCdc, results.UsbLinkState, results.UsbForceSpeed,
            results.TxHpd, results.TxVrr,
            results.UvcOutputTiming, results.UvcVideoFormat, results.UvcErrStatus,
            results.HdcpMode, results.HdcpVersion, results.RxTxHdcpVersion,
            results.Hdr2SdrExtended, results.Hdr2SdrColorParam, results.ColorRangeSetting,
            results.Vtem, results.BitError, results.RawTiming);

        var effectiveInputSource = results.InputSource;
        if (IsValidFlashAudioData(results.FlashAudio))
        {
            effectiveInputSource = new AtCommandResult(
                "InputSource", CmdInputSource, true,
                new[] { results.FlashAudio.Response[0] }, null, null);
        }

        var detailEntries = BuildDetailEntries(
            aviInfo, hdrInfo, hdr2SdrState, systemInfo,
            results.AudioFormat, results.AudioSamplingRate, effectiveInputSource,
            results.AdcOnOff, results.AdcVolumeGain, results.UacVolumeGain,
            results.UacOut1Mute, results.UacOut2Mute, results.UacOut2MixerSource,
            results.UsbHostProtocol, results.UsbCdc, results.UsbLinkState, results.UsbForceSpeed,
            results.TxHpd, results.TxVrr, results.TxEdidValid,
            results.UvcOutputTiming, results.UvcVideoFormat, results.UvcErrStatus,
            results.HdcpMode, results.HdcpVersion, results.RxTxHdcpVersion,
            results.Hdr2SdrExtended, results.CustomerVersion, results.RescueVersion,
            results.Hdr2SdrColorParam, results.ColorRangeSetting,
            results.RawTiming, vicCode, vfreqHz100);

        detailEntries = AppendFlashAudioAnalogGainDetail(detailEntries, results.FlashAudio);
        var analogGainByte = ResolveAnalogGainByte(results.FlashAudio);

        return new NodeReadAttempt(
            new SourceSignalTelemetrySnapshot
            {
                TimestampUtc = DateTimeOffset.UtcNow,
                Availability = SourceTelemetryAvailability.Available,
                Origin = SourceTelemetryOrigin.NativeXu,
                OriginDetail = $"NativeXu:{interfacePath}",
                Confidence = ResolveConfidence(vicCode.HasValue, hdrInfo, aviInfo, frameRateExact),
                Width = timing?.Width,
                Height = timing?.Height,
                FrameRateExact = frameRateExact,
                FrameRateArg = InferFrameRateRational(frameRateExact),
                IsHdr = hdrInfo.IsHdr,
                VideoFormat = aviInfo.ColorSpace,
                Colorimetry = aviInfo.Colorimetry,
                Quantization = aviInfo.Quantization,
                HdrTransferFunction = ResolveHdrTransferFunction(hdrInfo.Eotf),
                HdrTransferCode = hdrInfo.Eotf,
                Firmware = null,
                AudioFormat = sourceAudioFormat,
                AudioSampleRate = sourceAudioSampleRate,
                InputSource = ResolveAudioInputSource(results.FlashAudio, sourceInputSource),
                AdcOnOff = adcOnOff,
                AdcVolumeGain = adcVolumeGain,
                AnalogGainByte = analogGainByte,
                UacVolumeGain = uacVolumeGain,
                UacOut1Mute = uacOut1Mute,
                UacOut2Mute = uacOut2Mute,
                UacOut2MixerSource = uacOut2MixerSource,
                UsbHostProtocol = sourceUsbHostProtocol,
                TxEdidValid = txEdidValid,
                HdcpMode = sourceHdcpMode,
                HdcpVersion = sourceHdcpVersion,
                RxTxHdcpVersion = sourceRxTxHdcpVersion,
                CustomerVersion = customerVersion,
                RescueVersion = rescueVersion,
                RawTimingHex = sourceRawTimingHex,
                DetailEntries = detailEntries,
                DiagnosticSummary = fullDiagnosticSummary,
                AudioInputAvailability = results.InputSource.Success
                    ? SourceAudioInputAvailability.Available
                    : SourceAudioInputAvailability.Unavailable,
                AudioInputMode = ResolveAudioInputMode(results.FlashAudio, results.InputSource),
                AudioInputOrigin = ResolveSnapshotAudioInputOrigin(results.FlashAudio, results.InputSource, useDetailedAudioInputOrigin)
            },
            false,
            null,
            null);
    }

}

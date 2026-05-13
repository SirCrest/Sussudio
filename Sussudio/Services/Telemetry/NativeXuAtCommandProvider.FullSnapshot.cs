using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.Win32.SafeHandles;
using Sussudio.Models;

namespace Sussudio.Services.Telemetry;

public sealed partial class NativeXuAtCommandProvider
{
    /// <summary>
    /// Original monolithic read - fires all 35 commands. Kept for reference
    /// and for callers that need a guaranteed-complete snapshot (e.g. on-demand
    /// diagnostics). The rolling poll path is used for periodic polling.
    /// </summary>
    private static NodeReadAttempt TryReadSnapshot(
        SafeFileHandle handle,
        int nodeId,
        string interfacePath)
    {
        var cable = SendAtCommand(handle, nodeId, "CableConnect", CmdCableConnect);
        if (!cable.Success)
        {
            return HandleFailedCommand("nativexu-read-failed", interfacePath, cable);
        }

        if (TryReadInt32(cable.Response, out var cableState) && cableState == 0)
        {
            Logger.Log($"NATIVEXU_SIGNAL_UNAVAILABLE path='{interfacePath}' node={nodeId} reason=no-cable");
            return CreateUnavailableNodeResult(interfacePath, "nativexu-no-cable");
        }

        var videoStable = SendAtCommand(handle, nodeId, "VideoStable", CmdVideoStable);
        if (!videoStable.Success)
        {
            return HandleFailedCommand("nativexu-read-failed", interfacePath, videoStable);
        }

        if (TryReadInt32(videoStable.Response, out var stableValue) && stableValue == 0)
        {
            Logger.Log($"NATIVEXU_SIGNAL_UNAVAILABLE path='{interfacePath}' node={nodeId} reason=signal-unstable");
            return CreateUnavailableNodeResult(interfacePath, "nativexu-signal-unstable");
        }

        var vicResult = SendAtCommand(handle, nodeId, "VIC", CmdVic);
        var vfreqResult = SendAtCommand(handle, nodeId, "Vfreq", CmdVfreq);
        var aviInfoResult = SendAtCommand(handle, nodeId, "AviInfoFrame", CmdAviInfoFrame);
        var hdrMetadataResult = SendAtCommand(handle, nodeId, "HdrMetadata", CmdHdrMetadata);
        var systemInfoResult = SendAtCommand(handle, nodeId, "SystemInfo", CmdSystemInfo);
        var hdr2SdrResult = SendAtCommand(handle, nodeId, "Hdr2Sdr", CmdHdr2Sdr);
        var audioFormatResult = SendAtCommand(handle, nodeId, "AudioFormat", CmdAudioFormat);
        var audioSamplingRateResult = SendAtCommand(handle, nodeId, "AudioSamplingRate", CmdAudioSamplingRate);
        var inputSourceResult = SendAtCommand(handle, nodeId, "InputSource", CmdInputSource);
        var flashAudioResult = SendAtCommand(handle, nodeId, "FlashAudioInput", CmdFlashGetCustomerProprietary);
        var adcOnOffResult = SendAtCommand(handle, nodeId, "AdcOnOff", CmdAdcOnOff);
        var adcVolumeGainResult = SendAtCommand(handle, nodeId, "AdcVolumeGain", CmdAdcVolumeGain);
        var uacVolumeGainResult = SendAtCommand(handle, nodeId, "UacVolumeGain", CmdUacVolumeGain);
        var uacOut1MuteResult = SendAtCommand(handle, nodeId, "UacOut1Mute", CmdUacOut1Mute);
        var uacOut2MuteResult = SendAtCommand(handle, nodeId, "UacOut2Mute", CmdUacOut2Mute);
        var uacOut2MixerSourceResult = SendAtCommand(handle, nodeId, "UacOut2MixerSource", CmdUacOut2MixerSource);
        var usbHostProtocolResult = SendAtCommand(handle, nodeId, "UsbHostProtocol", CmdUsbHostProtocol);
        var usbCdcResult = SendAtCommand(handle, nodeId, "UsbCdc", CmdUsbCdcOnOff);
        var usbLinkStateResult = SendAtCommand(handle, nodeId, "UsbLinkState", CmdUsbLinkState);
        var usbForceSpeedResult = SendAtCommand(handle, nodeId, "UsbForceSpeed", CmdUsbForceSpeed);
        var txHpdResult = SendAtCommand(handle, nodeId, "TxHpd", CmdTxHpdStatus);
        var txVrrResult = SendAtCommand(handle, nodeId, "TxVrr", CmdTxVrr);
        var txEdidValidResult = SendAtCommand(handle, nodeId, "TxEdidValid", CmdTxEdidValid);
        var uvcOutputTimingResult = SendAtCommand(handle, nodeId, "UvcOutputTiming", CmdUvcOutputTiming);
        var uvcVideoFormatResult = SendAtCommand(handle, nodeId, "UvcVideoFormat", CmdUvcVideoFormat);
        var uvcErrStatusResult = SendAtCommand(handle, nodeId, "UvcErrStatus", CmdUvcErrStatus);
        var hdcpModeResult = SendAtCommand(handle, nodeId, "HdcpMode", CmdHdcpMode);
        var hdcpVersionResult = SendAtCommand(handle, nodeId, "HdcpVersion", CmdHdcpVersion);
        var rxTxHdcpVersionResult = SendAtCommand(handle, nodeId, "RxTxHdcpVersion", CmdRxTxHdcpVersion);
        var hdr2SdrExtendedResult = SendAtCommand(handle, nodeId, "Hdr2SdrExtended", CmdHdr2SdrExtended);
        var customerVersionResult = SendAtCommand(handle, nodeId, "CustomerVersion", CmdCustomerVersion);
        var rescueVersionResult = SendAtCommand(handle, nodeId, "RescueVersion", CmdRescueVersion);
        var hdr2SdrColorParamResult = SendAtCommand(handle, nodeId, "Hdr2SdrColorParam", CmdHdr2SdrColorParam);
        var colorRangeSettingResult = SendAtCommand(handle, nodeId, "ColorRangeSetting", CmdColorRangeSetting);
        var vtemResult = SendAtCommand(handle, nodeId, "Vtem", CmdVtem);
        var bitErrorResult = SendAtCommand(handle, nodeId, "BitError", CmdBitError);
        var rawTimingResult = SendAtCommand(handle, nodeId, "RawTiming", CmdRawTiming);
        // TODO: Add AT_Get_LedLight (0x85) when the GET path supports input payloads for two-phase commands.

        var aviInfo = aviInfoResult.Success ? DecodeAviInfoFrame(aviInfoResult.Response) : AviInfoFrameInfo.Empty;
        var hdrInfo = hdrMetadataResult.Success ? DecodeHdrMetadata(hdrMetadataResult.Response) : new HdrMetadataInfo(false, null, null);
        if (hdrMetadataResult.Success && !hdrInfo.HasMetadata)
        {
            hdrInfo = new HdrMetadataInfo(true, 0, false);
        }
        var systemInfo = systemInfoResult.Success ? DecodeCString(systemInfoResult.Response) : null;
        var vicCode = vicResult.Success ? ExtractInt32AsVicCode(vicResult.Response) : null;
        var vfreqHz100 = vfreqResult.Success && TryReadInt32(vfreqResult.Response, out var vfreqRaw) ? (int?)vfreqRaw : null;
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

        var hdr2SdrState = hdr2SdrResult.Success && TryReadInt32(hdr2SdrResult.Response, out var hdr2SdrValue)
            ? (hdr2SdrValue == 1 ? (byte)1 : (byte)0)
            : (byte?)null;
        var adcOnOff = TryReadBoolean(adcOnOffResult.Success ? adcOnOffResult.Response : Array.Empty<byte>());
        var adcVolumeGain = TryReadInt16(adcVolumeGainResult.Success ? adcVolumeGainResult.Response : Array.Empty<byte>());
        var uacVolumeGain = TryReadInt16(uacVolumeGainResult.Success ? uacVolumeGainResult.Response : Array.Empty<byte>());
        var uacOut1Mute = TryReadBoolean(uacOut1MuteResult.Success ? uacOut1MuteResult.Response : Array.Empty<byte>());
        var uacOut2Mute = TryReadBoolean(uacOut2MuteResult.Success ? uacOut2MuteResult.Response : Array.Empty<byte>());
        var uacOut2MixerSource = TryReadInt16(uacOut2MixerSourceResult.Success ? uacOut2MixerSourceResult.Response : Array.Empty<byte>());
        var sourceAudioFormat = TryFormatAtDetailValue(audioFormatResult, FormatAudioFormatDetail);
        var sourceAudioSampleRate = TryFormatAtDetailValue(audioSamplingRateResult, FormatAudioSampleRateDetail);
        var sourceInputSource = TryFormatAtDetailValue(inputSourceResult, FormatInputSourceDetail);
        var sourceUsbHostProtocol = TryFormatAtDetailValue(usbHostProtocolResult, FormatUsbHostProtocolDetail);
        var txEdidValid = TryReadBoolean(txEdidValidResult.Success ? txEdidValidResult.Response : Array.Empty<byte>());
        var sourceHdcpMode = TryFormatAtDetailValue(hdcpModeResult, FormatHdcpModeDetail);
        var sourceHdcpVersion = TryFormatAtDetailValue(hdcpVersionResult, FormatHdcpVersionDetail);
        var sourceRxTxHdcpVersion = TryFormatAtDetailValue(rxTxHdcpVersionResult, FormatRxTxHdcpVersionDetail);
        var customerVersion = TryFormatAtDetailValue(customerVersionResult, FormatAsciiOrHexDetail);
        var rescueVersion = rescueVersionResult.Success && TryReadInt32(rescueVersionResult.Response, out var rescueVersionValue)
            ? (int?)rescueVersionValue
            : null;
        var sourceRawTimingHex = rawTimingResult.Success && rawTimingResult.Response.Length > 0
            ? Convert.ToHexString(rawTimingResult.Response)
            : null;

        if (!vicCode.HasValue && !timing.HasValue && !frameRateExact.HasValue && !hdrInfo.HasMetadata && !aviInfo.HasData)
        {
            Logger.Log($"NATIVEXU_SIGNAL_UNAVAILABLE path='{interfacePath}' node={nodeId} reason=no-decodable-source-data");
            return new NodeReadAttempt(
                null,
                false,
                "nativexu-no-signal-data",
                $"{interfacePath}: node={nodeId}");
        }

        Logger.Log(
            $"NATIVEXU_DECODE vic={(vicCode.HasValue ? vicCode.Value.ToString(CultureInfo.InvariantCulture) : "none")} " +
            $"size={timing?.Width.ToString(CultureInfo.InvariantCulture) ?? "?"}x{timing?.Height.ToString(CultureInfo.InvariantCulture) ?? "?"} " +
            $"fps={(frameRateExact.HasValue ? frameRateExact.Value.ToString("0.###", CultureInfo.InvariantCulture) : "?")} " +
            $"vfreq={(vfreqHz100.HasValue ? vfreqHz100.Value.ToString(CultureInfo.InvariantCulture) : "?")} " +
            $"hdr={BoolToToken(hdrInfo.IsHdr)} colorspace={aviInfo.ColorSpace ?? "unknown"} " +
            $"colorimetry={aviInfo.Colorimetry ?? "unknown"} firmware={systemInfo ?? "unknown"}");

        var baseDiagnosticSummary = BuildDiagnosticSummary(
            vicCode,
            timing,
            frameRateExact,
            hdrInfo,
            aviInfo,
            vfreqHz100,
            hdr2SdrState,
            systemInfo);
        var fullDiagnosticSummary = AppendExtendedDiagnostics(
            baseDiagnosticSummary,
            audioFormatResult,
            audioSamplingRateResult,
            inputSourceResult,
            usbHostProtocolResult,
            usbCdcResult,
            usbLinkStateResult,
            usbForceSpeedResult,
            txHpdResult,
            txVrrResult,
            uvcOutputTimingResult,
            uvcVideoFormatResult,
            uvcErrStatusResult,
            hdcpModeResult,
            hdcpVersionResult,
            rxTxHdcpVersionResult,
            hdr2SdrExtendedResult,
            hdr2SdrColorParamResult,
            colorRangeSettingResult,
            vtemResult,
            bitErrorResult,
            rawTimingResult);
        // Use flash proprietary state (AT 0x52) for input source display if available,
        // since AT 0x35 doesn't reflect I2C-based audio switches.
        // Flash format: [source(0/1), 0x80, gainByte(0-255), 0xAA, 0x55, ...zeros...]
        var effectiveInputSource = inputSourceResult;
        if (IsValidFlashAudioData(flashAudioResult))
        {
            effectiveInputSource = new AtCommandResult(
                "InputSource", CmdInputSource, true,
                new[] { flashAudioResult.Response[0] }, null, null);
        }

        var detailEntries = BuildDetailEntries(
            aviInfo,
            hdrInfo,
            hdr2SdrState,
            systemInfo,
            audioFormatResult,
            audioSamplingRateResult,
            effectiveInputSource,
            adcOnOffResult,
            adcVolumeGainResult,
            uacVolumeGainResult,
            uacOut1MuteResult,
            uacOut2MuteResult,
            uacOut2MixerSourceResult,
            usbHostProtocolResult,
            usbCdcResult,
            usbLinkStateResult,
            usbForceSpeedResult,
            txHpdResult,
            txVrrResult,
            txEdidValidResult,
            uvcOutputTimingResult,
            uvcVideoFormatResult,
            uvcErrStatusResult,
            hdcpModeResult,
            hdcpVersionResult,
            rxTxHdcpVersionResult,
            hdr2SdrExtendedResult,
            customerVersionResult,
            rescueVersionResult,
            hdr2SdrColorParamResult,
            colorRangeSettingResult,
            rawTimingResult,
            vicCode,
            vfreqHz100);

        if (IsValidFlashAudioData(flashAudioResult))
        {
            var gainByte = flashAudioResult.Response[2];
            // Inverse of the log-taper curve used by MapPercentToGainByte (k=4.0)
            var y = gainByte / 255.0;
            var gainPct = (Math.Exp(4.0 * y) - 1.0) / (Math.Exp(4.0) - 1.0) * 100.0;
            var mutable = new List<SourceTelemetryDetailEntry>(detailEntries);
            // Insert after the last "Audio / Input" entry to keep group contiguous
            var lastAudioIdx = mutable.FindLastIndex(d => d.Group == TelemetryLabels.GroupAudioInput);
            var insertIdx = lastAudioIdx >= 0 ? lastAudioIdx + 1 : mutable.Count;
            mutable.Insert(insertIdx,
                new SourceTelemetryDetailEntry(TelemetryLabels.GroupAudioInput, TelemetryLabels.AnalogGain,
                    $"0x{gainByte:X2} ({gainPct:0}%)", gainByte.ToString(CultureInfo.InvariantCulture)));
            detailEntries = mutable;
        }

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
                InputSource = ResolveAudioInputSource(flashAudioResult, sourceInputSource),
                AdcOnOff = adcOnOff,
                AdcVolumeGain = adcVolumeGain,
                AnalogGainByte = IsValidFlashAudioData(flashAudioResult)
                    ? (int?)flashAudioResult.Response[2]
                    : null,
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
                AudioInputAvailability = inputSourceResult.Success
                    ? SourceAudioInputAvailability.Available
                    : SourceAudioInputAvailability.Unavailable,
                AudioInputMode = ResolveAudioInputMode(flashAudioResult, inputSourceResult),
                AudioInputOrigin = flashAudioResult.Success && flashAudioResult.Response.Length >= 5
                    ? $"NativeXu:Flash=0x{flashAudioResult.Response[0]:X2}"
                    : (inputSourceResult.Success && inputSourceResult.Response.Length >= 1
                        ? $"NativeXu:InputSource={inputSourceResult.Response[0]}"
                        : "not-implemented")
            },
            false,
            null,
            null);
    }
}

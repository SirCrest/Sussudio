using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.Win32.SafeHandles;
using Sussudio.Models;

namespace Sussudio.Services.Telemetry;

public sealed partial class NativeXuAtCommandProvider
{
    private static readonly IReadOnlyDictionary<int, VicTiming> VicTimingMap = new Dictionary<int, VicTiming>
    {
        [118] = new(3840, 2160, 120.0, false),
        [119] = new(3840, 2160, 120.0, false),
        [97] = new(3840, 2160, 60.0, false),
        [96] = new(3840, 2160, 50.0, false),
        [95] = new(3840, 2160, 30.0, false),
        [94] = new(3840, 2160, 25.0, false),
        [93] = new(3840, 2160, 24.0, false),
        [63] = new(1920, 1080, 120.0, false),
        [16] = new(1920, 1080, 60.0, false),
        [31] = new(1920, 1080, 50.0, false),
        [34] = new(1920, 1080, 30.0, false),
        [32] = new(1920, 1080, 24.0, false),
        [5] = new(1920, 1080, 60.0, true),
        [4] = new(1280, 720, 60.0, false),
        [19] = new(1280, 720, 50.0, false)
    };

    private static readonly double[] CanonicalFrameRates =
    {
        24000.0 / 1001.0,
        24.0,
        25.0,
        30000.0 / 1001.0,
        30.0,
        50.0,
        60000.0 / 1001.0,
        60.0,
        120000.0 / 1001.0,
        120.0
    };

    private readonly record struct VicTiming(int Width, int Height, double NominalFrameRate, bool IsInterlaced);

    private readonly record struct NativeXuSnapshotCommandResults(
        AtCommandResult Vic,
        AtCommandResult Vfreq,
        AtCommandResult AviInfo,
        AtCommandResult HdrMetadata,
        AtCommandResult SystemInfo,
        AtCommandResult Hdr2Sdr,
        AtCommandResult AudioFormat,
        AtCommandResult AudioSamplingRate,
        AtCommandResult InputSource,
        AtCommandResult FlashAudio,
        AtCommandResult AdcOnOff,
        AtCommandResult AdcVolumeGain,
        AtCommandResult UacVolumeGain,
        AtCommandResult UacOut1Mute,
        AtCommandResult UacOut2Mute,
        AtCommandResult UacOut2MixerSource,
        AtCommandResult UsbHostProtocol,
        AtCommandResult UsbCdc,
        AtCommandResult UsbLinkState,
        AtCommandResult UsbForceSpeed,
        AtCommandResult TxHpd,
        AtCommandResult TxVrr,
        AtCommandResult TxEdidValid,
        AtCommandResult UvcOutputTiming,
        AtCommandResult UvcVideoFormat,
        AtCommandResult UvcErrStatus,
        AtCommandResult HdcpMode,
        AtCommandResult HdcpVersion,
        AtCommandResult RxTxHdcpVersion,
        AtCommandResult Hdr2SdrExtended,
        AtCommandResult CustomerVersion,
        AtCommandResult RescueVersion,
        AtCommandResult Hdr2SdrColorParam,
        AtCommandResult ColorRangeSetting,
        AtCommandResult Vtem,
        AtCommandResult BitError,
        AtCommandResult RawTiming);

    /// <summary>
    /// Original monolithic read - fires all commands. Kept for callers that
    /// need a guaranteed-complete snapshot, while rolling poll handles periodic reads.
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

        var results = new NativeXuSnapshotCommandResults(
            SendAtCommand(handle, nodeId, "VIC", CmdVic),
            SendAtCommand(handle, nodeId, "Vfreq", CmdVfreq),
            SendAtCommand(handle, nodeId, "AviInfoFrame", CmdAviInfoFrame),
            SendAtCommand(handle, nodeId, "HdrMetadata", CmdHdrMetadata),
            SendAtCommand(handle, nodeId, "SystemInfo", CmdSystemInfo),
            SendAtCommand(handle, nodeId, "Hdr2Sdr", CmdHdr2Sdr),
            SendAtCommand(handle, nodeId, "AudioFormat", CmdAudioFormat),
            SendAtCommand(handle, nodeId, "AudioSamplingRate", CmdAudioSamplingRate),
            SendAtCommand(handle, nodeId, "InputSource", CmdInputSource),
            SendAtCommand(handle, nodeId, "FlashAudioInput", CmdFlashGetCustomerProprietary),
            SendAtCommand(handle, nodeId, "AdcOnOff", CmdAdcOnOff),
            SendAtCommand(handle, nodeId, "AdcVolumeGain", CmdAdcVolumeGain),
            SendAtCommand(handle, nodeId, "UacVolumeGain", CmdUacVolumeGain),
            SendAtCommand(handle, nodeId, "UacOut1Mute", CmdUacOut1Mute),
            SendAtCommand(handle, nodeId, "UacOut2Mute", CmdUacOut2Mute),
            SendAtCommand(handle, nodeId, "UacOut2MixerSource", CmdUacOut2MixerSource),
            SendAtCommand(handle, nodeId, "UsbHostProtocol", CmdUsbHostProtocol),
            SendAtCommand(handle, nodeId, "UsbCdc", CmdUsbCdcOnOff),
            SendAtCommand(handle, nodeId, "UsbLinkState", CmdUsbLinkState),
            SendAtCommand(handle, nodeId, "UsbForceSpeed", CmdUsbForceSpeed),
            SendAtCommand(handle, nodeId, "TxHpd", CmdTxHpdStatus),
            SendAtCommand(handle, nodeId, "TxVrr", CmdTxVrr),
            SendAtCommand(handle, nodeId, "TxEdidValid", CmdTxEdidValid),
            SendAtCommand(handle, nodeId, "UvcOutputTiming", CmdUvcOutputTiming),
            SendAtCommand(handle, nodeId, "UvcVideoFormat", CmdUvcVideoFormat),
            SendAtCommand(handle, nodeId, "UvcErrStatus", CmdUvcErrStatus),
            SendAtCommand(handle, nodeId, "HdcpMode", CmdHdcpMode),
            SendAtCommand(handle, nodeId, "HdcpVersion", CmdHdcpVersion),
            SendAtCommand(handle, nodeId, "RxTxHdcpVersion", CmdRxTxHdcpVersion),
            SendAtCommand(handle, nodeId, "Hdr2SdrExtended", CmdHdr2SdrExtended),
            SendAtCommand(handle, nodeId, "CustomerVersion", CmdCustomerVersion),
            SendAtCommand(handle, nodeId, "RescueVersion", CmdRescueVersion),
            SendAtCommand(handle, nodeId, "Hdr2SdrColorParam", CmdHdr2SdrColorParam),
            SendAtCommand(handle, nodeId, "ColorRangeSetting", CmdColorRangeSetting),
            SendAtCommand(handle, nodeId, "Vtem", CmdVtem),
            SendAtCommand(handle, nodeId, "BitError", CmdBitError),
            SendAtCommand(handle, nodeId, "RawTiming", CmdRawTiming));

        return BuildSnapshotFromCommandResults(
            results,
            interfacePath,
            nodeId,
            logDecodeSummary: true,
            logNoDecodableSourceData: true,
            useDetailedAudioInputOrigin: true);
    }

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

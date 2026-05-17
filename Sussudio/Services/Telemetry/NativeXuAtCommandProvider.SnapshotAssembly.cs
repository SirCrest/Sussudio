using System;
using System.Collections.Generic;
using System.Globalization;
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

        if (IsValidFlashAudioData(results.FlashAudio))
        {
            var gainByte = results.FlashAudio.Response[2];
            var y = gainByte / 255.0;
            var gainPct = (Math.Exp(4.0 * y) - 1.0) / (Math.Exp(4.0) - 1.0) * 100.0;
            var mutable = new List<SourceTelemetryDetailEntry>(detailEntries);
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
                InputSource = ResolveAudioInputSource(results.FlashAudio, sourceInputSource),
                AdcOnOff = adcOnOff,
                AdcVolumeGain = adcVolumeGain,
                AnalogGainByte = IsValidFlashAudioData(results.FlashAudio)
                    ? (int?)results.FlashAudio.Response[2]
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
}

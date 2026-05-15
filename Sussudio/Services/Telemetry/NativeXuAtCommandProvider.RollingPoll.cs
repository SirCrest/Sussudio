using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using Microsoft.Win32.SafeHandles;
using Sussudio.Models;

namespace Sussudio.Services.Telemetry;

public sealed partial class NativeXuAtCommandProvider
{
    // ── Rolling poll ──
    // Commands are spread across ticks instead of all at once.
    // Each tick: gates (CableConnect + VideoStable) + one rotating group.
    // 6 groups → full cycle in 6 ticks (3 seconds at 500ms interval).
    //
    // Group 0: VIC, Vfreq, AviInfoFrame, HdrMetadata  (signal — most important)
    // Group 1: AudioFormat, AudioSamplingRate, InputSource, FlashAudioInput
    // Group 2: AdcOnOff, AdcVolumeGain, UacVolumeGain, UacOut1Mute, UacOut2Mute, UacOut2MixerSource
    // Group 3: SystemInfo, Hdr2Sdr, Hdr2SdrExtended, Hdr2SdrColorParam, ColorRangeSetting
    // Group 4: UsbHostProtocol, UsbCdc, UsbLinkState, UsbForceSpeed, TxHpd, TxVrr, TxEdidValid
    // Group 5: UvcOutputTiming, UvcVideoFormat, UvcErrStatus, HdcpMode, HdcpVersion, RxTxHdcpVersion,
    //          CustomerVersion, RescueVersion, Vtem, BitError, RawTiming
    private int _rollingGroup;
    private const int RollingGroupCount = 6;
    private bool _hasCompletedFullCycle;

    // Cached AT command results — updated as each group rotates through
    private AtCommandResult _cVic, _cVfreq, _cAviInfo, _cHdrMetadata;
    private AtCommandResult _cSystemInfo, _cHdr2Sdr, _cAudioFormat, _cAudioSamplingRate;
    private AtCommandResult _cInputSource, _cFlashAudio, _cAdcOnOff, _cAdcVolumeGain;
    private AtCommandResult _cUacVolumeGain, _cUacOut1Mute, _cUacOut2Mute, _cUacOut2MixerSource;
    private AtCommandResult _cUsbHostProtocol, _cUsbCdc, _cUsbLinkState, _cUsbForceSpeed;
    private AtCommandResult _cTxHpd, _cTxVrr, _cTxEdidValid;
    private AtCommandResult _cUvcOutputTiming, _cUvcVideoFormat, _cUvcErrStatus;
    private AtCommandResult _cHdcpMode, _cHdcpVersion, _cRxTxHdcpVersion;
    private AtCommandResult _cHdr2SdrExtended, _cCustomerVersion, _cRescueVersion;
    private AtCommandResult _cHdr2SdrColorParam, _cColorRangeSetting;
    private AtCommandResult _cVtem, _cBitError, _cRawTiming;
    private string? _rollingInterfacePath;
    private int? _rollingNodeId;

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

    /// <summary>
    /// Rolling poll: fires gates + one rotating command group per call,
    /// caching results. First call fires all commands. Subsequent calls
    /// fire only the current group and build the snapshot from cache.
    /// </summary>
    private NodeReadAttempt TryReadRolling(
        SafeFileHandle handle,
        int nodeId,
        string interfacePath,
        CancellationToken cancellationToken)
    {
        // ── Gates (always checked) ──
        if (!string.Equals(_rollingInterfacePath, interfacePath, StringComparison.OrdinalIgnoreCase) ||
            _rollingNodeId != nodeId)
        {
            _rollingInterfacePath = interfacePath;
            _rollingNodeId = nodeId;
            _hasCompletedFullCycle = false;
            _rollingGroup = 0;
        }

        var cable = SendRollingCommand(handle, nodeId, "CableConnect", CmdCableConnect, cancellationToken);
        if (!cable.Success)
            return HandleFailedCommand("nativexu-read-failed", interfacePath, cable);

        if (TryReadInt32(cable.Response, out var cableState) && cableState == 0)
        {
            Logger.Log($"NATIVEXU_SIGNAL_UNAVAILABLE path='{interfacePath}' node={nodeId} reason=no-cable");
            _hasCompletedFullCycle = false; // force full re-read when cable reconnects
            return CreateUnavailableNodeResult(interfacePath, "nativexu-no-cable");
        }

        var videoStable = SendRollingCommand(handle, nodeId, "VideoStable", CmdVideoStable, cancellationToken);
        if (!videoStable.Success)
            return HandleFailedCommand("nativexu-read-failed", interfacePath, videoStable);

        if (TryReadInt32(videoStable.Response, out var stableValue) && stableValue == 0)
        {
            Logger.Log($"NATIVEXU_SIGNAL_UNAVAILABLE path='{interfacePath}' node={nodeId} reason=signal-unstable");
            _hasCompletedFullCycle = false;
            return CreateUnavailableNodeResult(interfacePath, "nativexu-signal-unstable");
        }

        // ── First call: fire everything to populate cache ──
        if (!_hasCompletedFullCycle)
        {
            PopulateInitialRollingCache(handle, nodeId, cancellationToken);
            _hasCompletedFullCycle = true;
            _rollingGroup = 0;
        }
        else
        {
            // ── Subsequent calls: fire only current group ──
            RefreshRollingGroup(handle, nodeId, _rollingGroup, cancellationToken);
            _rollingGroup = (_rollingGroup + 1) % RollingGroupCount;
        }

        // ── Build snapshot from cached results ──
        // This is the same decode logic as TryReadSnapshot, using cached fields.
        return BuildSnapshotFromCachedResults(interfacePath, nodeId);
    }

    private NodeReadAttempt BuildSnapshotFromCachedResults(string interfacePath, int nodeId)
    {
        // Alias cache fields to the local names the decode logic expects
        var vicResult = _cVic;
        var vfreqResult = _cVfreq;
        var aviInfoResult = _cAviInfo;
        var hdrMetadataResult = _cHdrMetadata;
        var systemInfoResult = _cSystemInfo;
        var hdr2SdrResult = _cHdr2Sdr;
        var audioFormatResult = _cAudioFormat;
        var audioSamplingRateResult = _cAudioSamplingRate;
        var inputSourceResult = _cInputSource;
        var flashAudioResult = _cFlashAudio;
        var adcOnOffResult = _cAdcOnOff;
        var adcVolumeGainResult = _cAdcVolumeGain;
        var uacVolumeGainResult = _cUacVolumeGain;
        var uacOut1MuteResult = _cUacOut1Mute;
        var uacOut2MuteResult = _cUacOut2Mute;
        var uacOut2MixerSourceResult = _cUacOut2MixerSource;
        var usbHostProtocolResult = _cUsbHostProtocol;
        var usbCdcResult = _cUsbCdc;
        var usbLinkStateResult = _cUsbLinkState;
        var usbForceSpeedResult = _cUsbForceSpeed;
        var txHpdResult = _cTxHpd;
        var txVrrResult = _cTxVrr;
        var txEdidValidResult = _cTxEdidValid;
        var uvcOutputTimingResult = _cUvcOutputTiming;
        var uvcVideoFormatResult = _cUvcVideoFormat;
        var uvcErrStatusResult = _cUvcErrStatus;
        var hdcpModeResult = _cHdcpMode;
        var hdcpVersionResult = _cHdcpVersion;
        var rxTxHdcpVersionResult = _cRxTxHdcpVersion;
        var hdr2SdrExtendedResult = _cHdr2SdrExtended;
        var customerVersionResult = _cCustomerVersion;
        var rescueVersionResult = _cRescueVersion;
        var hdr2SdrColorParamResult = _cHdr2SdrColorParam;
        var colorRangeSettingResult = _cColorRangeSetting;
        var vtemResult = _cVtem;
        var bitErrorResult = _cBitError;
        var rawTimingResult = _cRawTiming;

        // If any critical result is still default (not yet populated), report unavailable
        if (vicResult.Name == null && vfreqResult.Name == null)
        {
            return new NodeReadAttempt(null, false, "nativexu-cache-incomplete", interfacePath);
        }

        // ── Decode (identical to TryReadSnapshot from here) ──
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
            return new NodeReadAttempt(null, false, "nativexu-no-signal-data", $"{interfacePath}: node={nodeId}");
        }

        var baseDiagnosticSummary = BuildDiagnosticSummary(vicCode, timing, frameRateExact, hdrInfo, aviInfo, vfreqHz100, hdr2SdrState, systemInfo);
        var fullDiagnosticSummary = AppendExtendedDiagnostics(
            baseDiagnosticSummary,
            audioFormatResult, audioSamplingRateResult, inputSourceResult,
            usbHostProtocolResult, usbCdcResult, usbLinkStateResult, usbForceSpeedResult,
            txHpdResult, txVrrResult,
            uvcOutputTimingResult, uvcVideoFormatResult, uvcErrStatusResult,
            hdcpModeResult, hdcpVersionResult, rxTxHdcpVersionResult,
            hdr2SdrExtendedResult, hdr2SdrColorParamResult, colorRangeSettingResult,
            vtemResult, bitErrorResult, rawTimingResult);

        var effectiveInputSource = inputSourceResult;
        if (IsValidFlashAudioData(flashAudioResult))
        {
            effectiveInputSource = new AtCommandResult(
                "InputSource", CmdInputSource, true,
                new[] { flashAudioResult.Response[0] }, null, null);
        }

        var detailEntries = BuildDetailEntries(
            aviInfo, hdrInfo, hdr2SdrState, systemInfo,
            audioFormatResult, audioSamplingRateResult, effectiveInputSource,
            adcOnOffResult, adcVolumeGainResult, uacVolumeGainResult,
            uacOut1MuteResult, uacOut2MuteResult, uacOut2MixerSourceResult,
            usbHostProtocolResult, usbCdcResult, usbLinkStateResult, usbForceSpeedResult,
            txHpdResult, txVrrResult, txEdidValidResult,
            uvcOutputTimingResult, uvcVideoFormatResult, uvcErrStatusResult,
            hdcpModeResult, hdcpVersionResult, rxTxHdcpVersionResult,
            hdr2SdrExtendedResult, customerVersionResult, rescueVersionResult,
            hdr2SdrColorParamResult, colorRangeSettingResult,
            rawTimingResult, vicCode, vfreqHz100);

        if (IsValidFlashAudioData(flashAudioResult))
        {
            var gainByte = flashAudioResult.Response[2];
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
                    ? "nativexu-flash-audio"
                    : (inputSourceResult.Success ? "nativexu-input-source" : "unknown")
            },
            false,
            null,
            null);
    }
}

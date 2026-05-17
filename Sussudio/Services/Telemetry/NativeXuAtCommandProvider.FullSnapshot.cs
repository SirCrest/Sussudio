using Microsoft.Win32.SafeHandles;

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

        var results = new NativeXuSnapshotCommandResults(
            vicResult,
            vfreqResult,
            aviInfoResult,
            hdrMetadataResult,
            systemInfoResult,
            hdr2SdrResult,
            audioFormatResult,
            audioSamplingRateResult,
            inputSourceResult,
            flashAudioResult,
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
            vtemResult,
            bitErrorResult,
            rawTimingResult);

        return BuildSnapshotFromCommandResults(
            results,
            interfacePath,
            nodeId,
            logDecodeSummary: true,
            logNoDecodableSourceData: true,
            useDetailedAudioInputOrigin: true);
    }
}

using System.Threading;
using Microsoft.Win32.SafeHandles;

namespace Sussudio.Services.Telemetry;

public sealed partial class NativeXuAtCommandProvider
{
    private AtCommandResult SendRollingCommand(
        SafeFileHandle handle,
        int nodeId,
        string name,
        int commandCode,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return SendAtCommand(handle, nodeId, name, commandCode);
    }

    private void PopulateInitialRollingCache(
        SafeFileHandle handle,
        int nodeId,
        CancellationToken cancellationToken)
    {
        _cVic = SendRollingCommand(handle, nodeId, "VIC", CmdVic, cancellationToken);
        _cVfreq = SendRollingCommand(handle, nodeId, "Vfreq", CmdVfreq, cancellationToken);
        _cAviInfo = SendRollingCommand(handle, nodeId, "AviInfoFrame", CmdAviInfoFrame, cancellationToken);
        _cHdrMetadata = SendRollingCommand(handle, nodeId, "HdrMetadata", CmdHdrMetadata, cancellationToken);
        _cSystemInfo = SendRollingCommand(handle, nodeId, "SystemInfo", CmdSystemInfo, cancellationToken);
        _cHdr2Sdr = SendRollingCommand(handle, nodeId, "Hdr2Sdr", CmdHdr2Sdr, cancellationToken);
        _cAudioFormat = SendRollingCommand(handle, nodeId, "AudioFormat", CmdAudioFormat, cancellationToken);
        _cAudioSamplingRate = SendRollingCommand(handle, nodeId, "AudioSamplingRate", CmdAudioSamplingRate, cancellationToken);
        _cInputSource = SendRollingCommand(handle, nodeId, "InputSource", CmdInputSource, cancellationToken);
        _cFlashAudio = SendRollingCommand(handle, nodeId, "FlashAudioInput", CmdFlashGetCustomerProprietary, cancellationToken);
        _cAdcOnOff = SendRollingCommand(handle, nodeId, "AdcOnOff", CmdAdcOnOff, cancellationToken);
        _cAdcVolumeGain = SendRollingCommand(handle, nodeId, "AdcVolumeGain", CmdAdcVolumeGain, cancellationToken);
        _cUacVolumeGain = SendRollingCommand(handle, nodeId, "UacVolumeGain", CmdUacVolumeGain, cancellationToken);
        _cUacOut1Mute = SendRollingCommand(handle, nodeId, "UacOut1Mute", CmdUacOut1Mute, cancellationToken);
        _cUacOut2Mute = SendRollingCommand(handle, nodeId, "UacOut2Mute", CmdUacOut2Mute, cancellationToken);
        _cUacOut2MixerSource = SendRollingCommand(handle, nodeId, "UacOut2MixerSource", CmdUacOut2MixerSource, cancellationToken);
        _cUsbHostProtocol = SendRollingCommand(handle, nodeId, "UsbHostProtocol", CmdUsbHostProtocol, cancellationToken);
        _cUsbCdc = SendRollingCommand(handle, nodeId, "UsbCdc", CmdUsbCdcOnOff, cancellationToken);
        _cUsbLinkState = SendRollingCommand(handle, nodeId, "UsbLinkState", CmdUsbLinkState, cancellationToken);
        _cUsbForceSpeed = SendRollingCommand(handle, nodeId, "UsbForceSpeed", CmdUsbForceSpeed, cancellationToken);
        _cTxHpd = SendRollingCommand(handle, nodeId, "TxHpd", CmdTxHpdStatus, cancellationToken);
        _cTxVrr = SendRollingCommand(handle, nodeId, "TxVrr", CmdTxVrr, cancellationToken);
        _cTxEdidValid = SendRollingCommand(handle, nodeId, "TxEdidValid", CmdTxEdidValid, cancellationToken);
        _cUvcOutputTiming = SendRollingCommand(handle, nodeId, "UvcOutputTiming", CmdUvcOutputTiming, cancellationToken);
        _cUvcVideoFormat = SendRollingCommand(handle, nodeId, "UvcVideoFormat", CmdUvcVideoFormat, cancellationToken);
        _cUvcErrStatus = SendRollingCommand(handle, nodeId, "UvcErrStatus", CmdUvcErrStatus, cancellationToken);
        _cHdcpMode = SendRollingCommand(handle, nodeId, "HdcpMode", CmdHdcpMode, cancellationToken);
        _cHdcpVersion = SendRollingCommand(handle, nodeId, "HdcpVersion", CmdHdcpVersion, cancellationToken);
        _cRxTxHdcpVersion = SendRollingCommand(handle, nodeId, "RxTxHdcpVersion", CmdRxTxHdcpVersion, cancellationToken);
        _cHdr2SdrExtended = SendRollingCommand(handle, nodeId, "Hdr2SdrExtended", CmdHdr2SdrExtended, cancellationToken);
        _cCustomerVersion = SendRollingCommand(handle, nodeId, "CustomerVersion", CmdCustomerVersion, cancellationToken);
        _cRescueVersion = SendRollingCommand(handle, nodeId, "RescueVersion", CmdRescueVersion, cancellationToken);
        _cHdr2SdrColorParam = SendRollingCommand(handle, nodeId, "Hdr2SdrColorParam", CmdHdr2SdrColorParam, cancellationToken);
        _cColorRangeSetting = SendRollingCommand(handle, nodeId, "ColorRangeSetting", CmdColorRangeSetting, cancellationToken);
        _cVtem = SendRollingCommand(handle, nodeId, "Vtem", CmdVtem, cancellationToken);
        _cBitError = SendRollingCommand(handle, nodeId, "BitError", CmdBitError, cancellationToken);
        _cRawTiming = SendRollingCommand(handle, nodeId, "RawTiming", CmdRawTiming, cancellationToken);
    }

    private void RefreshRollingGroup(
        SafeFileHandle handle,
        int nodeId,
        int rollingGroup,
        CancellationToken cancellationToken)
    {
        switch (rollingGroup)
        {
            case 0: // Signal (most important - cycles every pass)
                _cVic = SendRollingCommand(handle, nodeId, "VIC", CmdVic, cancellationToken);
                _cVfreq = SendRollingCommand(handle, nodeId, "Vfreq", CmdVfreq, cancellationToken);
                _cAviInfo = SendRollingCommand(handle, nodeId, "AviInfoFrame", CmdAviInfoFrame, cancellationToken);
                _cHdrMetadata = SendRollingCommand(handle, nodeId, "HdrMetadata", CmdHdrMetadata, cancellationToken);
                break;
            case 1: // Audio
                _cAudioFormat = SendRollingCommand(handle, nodeId, "AudioFormat", CmdAudioFormat, cancellationToken);
                _cAudioSamplingRate = SendRollingCommand(handle, nodeId, "AudioSamplingRate", CmdAudioSamplingRate, cancellationToken);
                _cInputSource = SendRollingCommand(handle, nodeId, "InputSource", CmdInputSource, cancellationToken);
                _cFlashAudio = SendRollingCommand(handle, nodeId, "FlashAudioInput", CmdFlashGetCustomerProprietary, cancellationToken);
                break;
            case 2: // Audio routing
                _cAdcOnOff = SendRollingCommand(handle, nodeId, "AdcOnOff", CmdAdcOnOff, cancellationToken);
                _cAdcVolumeGain = SendRollingCommand(handle, nodeId, "AdcVolumeGain", CmdAdcVolumeGain, cancellationToken);
                _cUacVolumeGain = SendRollingCommand(handle, nodeId, "UacVolumeGain", CmdUacVolumeGain, cancellationToken);
                _cUacOut1Mute = SendRollingCommand(handle, nodeId, "UacOut1Mute", CmdUacOut1Mute, cancellationToken);
                _cUacOut2Mute = SendRollingCommand(handle, nodeId, "UacOut2Mute", CmdUacOut2Mute, cancellationToken);
                _cUacOut2MixerSource = SendRollingCommand(handle, nodeId, "UacOut2MixerSource", CmdUacOut2MixerSource, cancellationToken);
                break;
            case 3: // HDR/color
                _cSystemInfo = SendRollingCommand(handle, nodeId, "SystemInfo", CmdSystemInfo, cancellationToken);
                _cHdr2Sdr = SendRollingCommand(handle, nodeId, "Hdr2Sdr", CmdHdr2Sdr, cancellationToken);
                _cHdr2SdrExtended = SendRollingCommand(handle, nodeId, "Hdr2SdrExtended", CmdHdr2SdrExtended, cancellationToken);
                _cHdr2SdrColorParam = SendRollingCommand(handle, nodeId, "Hdr2SdrColorParam", CmdHdr2SdrColorParam, cancellationToken);
                _cColorRangeSetting = SendRollingCommand(handle, nodeId, "ColorRangeSetting", CmdColorRangeSetting, cancellationToken);
                break;
            case 4: // USB/HDMI status
                _cUsbHostProtocol = SendRollingCommand(handle, nodeId, "UsbHostProtocol", CmdUsbHostProtocol, cancellationToken);
                _cUsbCdc = SendRollingCommand(handle, nodeId, "UsbCdc", CmdUsbCdcOnOff, cancellationToken);
                _cUsbLinkState = SendRollingCommand(handle, nodeId, "UsbLinkState", CmdUsbLinkState, cancellationToken);
                _cUsbForceSpeed = SendRollingCommand(handle, nodeId, "UsbForceSpeed", CmdUsbForceSpeed, cancellationToken);
                _cTxHpd = SendRollingCommand(handle, nodeId, "TxHpd", CmdTxHpdStatus, cancellationToken);
                _cTxVrr = SendRollingCommand(handle, nodeId, "TxVrr", CmdTxVrr, cancellationToken);
                _cTxEdidValid = SendRollingCommand(handle, nodeId, "TxEdidValid", CmdTxEdidValid, cancellationToken);
                break;
            case 5: // Diagnostics (least critical)
                _cUvcOutputTiming = SendRollingCommand(handle, nodeId, "UvcOutputTiming", CmdUvcOutputTiming, cancellationToken);
                _cUvcVideoFormat = SendRollingCommand(handle, nodeId, "UvcVideoFormat", CmdUvcVideoFormat, cancellationToken);
                _cUvcErrStatus = SendRollingCommand(handle, nodeId, "UvcErrStatus", CmdUvcErrStatus, cancellationToken);
                _cHdcpMode = SendRollingCommand(handle, nodeId, "HdcpMode", CmdHdcpMode, cancellationToken);
                _cHdcpVersion = SendRollingCommand(handle, nodeId, "HdcpVersion", CmdHdcpVersion, cancellationToken);
                _cRxTxHdcpVersion = SendRollingCommand(handle, nodeId, "RxTxHdcpVersion", CmdRxTxHdcpVersion, cancellationToken);
                _cCustomerVersion = SendRollingCommand(handle, nodeId, "CustomerVersion", CmdCustomerVersion, cancellationToken);
                _cRescueVersion = SendRollingCommand(handle, nodeId, "RescueVersion", CmdRescueVersion, cancellationToken);
                _cVtem = SendRollingCommand(handle, nodeId, "Vtem", CmdVtem, cancellationToken);
                _cBitError = SendRollingCommand(handle, nodeId, "BitError", CmdBitError, cancellationToken);
                _cRawTiming = SendRollingCommand(handle, nodeId, "RawTiming", CmdRawTiming, cancellationToken);
                break;
        }
    }
}

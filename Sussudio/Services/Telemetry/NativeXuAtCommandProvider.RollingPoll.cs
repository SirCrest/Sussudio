using System;
using System.Threading;
using Microsoft.Win32.SafeHandles;

namespace Sussudio.Services.Telemetry;

public sealed partial class NativeXuAtCommandProvider
{
    // Rolling poll
    // Commands are spread across ticks instead of all at once.
    // Each tick: gates (CableConnect + VideoStable) + one rotating group.
    // 6 groups -> full cycle in 6 ticks (3 seconds at 500ms interval).
    //
    // Group 0: VIC, Vfreq, AviInfoFrame, HdrMetadata  (signal - most important)
    // Group 1: AudioFormat, AudioSamplingRate, InputSource, FlashAudioInput
    // Group 2: AdcOnOff, AdcVolumeGain, UacVolumeGain, UacOut1Mute, UacOut2Mute, UacOut2MixerSource
    // Group 3: SystemInfo, Hdr2Sdr, Hdr2SdrExtended, Hdr2SdrColorParam, ColorRangeSetting
    // Group 4: UsbHostProtocol, UsbCdc, UsbLinkState, UsbForceSpeed, TxHpd, TxVrr, TxEdidValid
    // Group 5: UvcOutputTiming, UvcVideoFormat, UvcErrStatus, HdcpMode, HdcpVersion, RxTxHdcpVersion,
    //          CustomerVersion, RescueVersion, Vtem, BitError, RawTiming
    private int _rollingGroup;
    private const int RollingGroupCount = 6;
    private bool _hasCompletedFullCycle;

    // Cached AT command results updated as each group rotates through.
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
        {
            return HandleFailedCommand("nativexu-read-failed", interfacePath, cable);
        }

        if (TryReadInt32(cable.Response, out var cableState) && cableState == 0)
        {
            Logger.Log($"NATIVEXU_SIGNAL_UNAVAILABLE path='{interfacePath}' node={nodeId} reason=no-cable");
            _hasCompletedFullCycle = false;
            return CreateUnavailableNodeResult(interfacePath, "nativexu-no-cable");
        }

        var videoStable = SendRollingCommand(handle, nodeId, "VideoStable", CmdVideoStable, cancellationToken);
        if (!videoStable.Success)
        {
            return HandleFailedCommand("nativexu-read-failed", interfacePath, videoStable);
        }

        if (TryReadInt32(videoStable.Response, out var stableValue) && stableValue == 0)
        {
            Logger.Log($"NATIVEXU_SIGNAL_UNAVAILABLE path='{interfacePath}' node={nodeId} reason=signal-unstable");
            _hasCompletedFullCycle = false;
            return CreateUnavailableNodeResult(interfacePath, "nativexu-signal-unstable");
        }

        if (!_hasCompletedFullCycle)
        {
            PopulateInitialRollingCache(handle, nodeId, cancellationToken);
            _hasCompletedFullCycle = true;
            _rollingGroup = 0;
        }
        else
        {
            RefreshRollingGroup(handle, nodeId, _rollingGroup, cancellationToken);
            _rollingGroup = (_rollingGroup + 1) % RollingGroupCount;
        }

        return BuildSnapshotFromCachedResults(interfacePath, nodeId);
    }

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

    private NodeReadAttempt BuildSnapshotFromCachedResults(string interfacePath, int nodeId)
    {
        if (_cVic.Name == null && _cVfreq.Name == null)
        {
            return new NodeReadAttempt(null, false, "nativexu-cache-incomplete", interfacePath);
        }

        var results = new NativeXuSnapshotCommandResults(
            _cVic,
            _cVfreq,
            _cAviInfo,
            _cHdrMetadata,
            _cSystemInfo,
            _cHdr2Sdr,
            _cAudioFormat,
            _cAudioSamplingRate,
            _cInputSource,
            _cFlashAudio,
            _cAdcOnOff,
            _cAdcVolumeGain,
            _cUacVolumeGain,
            _cUacOut1Mute,
            _cUacOut2Mute,
            _cUacOut2MixerSource,
            _cUsbHostProtocol,
            _cUsbCdc,
            _cUsbLinkState,
            _cUsbForceSpeed,
            _cTxHpd,
            _cTxVrr,
            _cTxEdidValid,
            _cUvcOutputTiming,
            _cUvcVideoFormat,
            _cUvcErrStatus,
            _cHdcpMode,
            _cHdcpVersion,
            _cRxTxHdcpVersion,
            _cHdr2SdrExtended,
            _cCustomerVersion,
            _cRescueVersion,
            _cHdr2SdrColorParam,
            _cColorRangeSetting,
            _cVtem,
            _cBitError,
            _cRawTiming);

        return BuildSnapshotFromCommandResults(
            results,
            interfacePath,
            nodeId,
            logDecodeSummary: false,
            logNoDecodableSourceData: false,
            useDetailedAudioInputOrigin: false);
    }
}

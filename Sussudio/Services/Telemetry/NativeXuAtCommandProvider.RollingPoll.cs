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

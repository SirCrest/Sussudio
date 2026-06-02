using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;
using Sussudio.Models;
using Sussudio.Services.Capture;
using Sussudio.Services.Contracts;

namespace Sussudio.Services.Telemetry;

// Null-object telemetry provider used when source telemetry is unavailable or
// intentionally disabled.
public sealed class DisabledSourceSignalTelemetryProvider : ISourceSignalTelemetryProvider
{
    public Task<SourceSignalTelemetrySnapshot> ReadAsync(
        CaptureDevice? device,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(SourceSignalTelemetrySnapshot.CreateUnavailable("telemetry-provider-disabled"));
    }
}

public sealed class NativeXuAtCommandProvider : ISourceSignalTelemetryProvider
{
    private static readonly Guid XuGuid = NativeXuDeviceSupport.ExtensionUnitGuid;

    private const int AtPayloadSelector = 1;
    private const int AtTriggerSelector = 2;
    private const int AtFrameHeaderSize = 4;
    private const int AtFrameLrcSize = 1;
    private const int MaxAtResponseFrameSize = 0x200;

    private const int CmdSetAdcOnOff = 0x08;
    private const int CmdSetDacHpOnOff = 0x09;
    private const int CmdSetAdcVolumeGain = 0x0A;
    private const int CmdCableConnect = 0x36;
    private const int CmdVideoStable = 0x38;
    private const int CmdVic = 0x3C;
    private const int CmdSystemInfo = 0x23;
    private const int CmdHdrMetadata = 0x65;
    private const int CmdVfreq = 0x86;
    private const int CmdSetHdr2Sdr = 0x1F;
    private const int CmdSetInputSource = 0x34;
    private const int CmdAdcVolumeGain = 0x0B;
    private const int CmdUacVolumeGain = 0x11;
    private const int CmdUacOut2MixerSource = 0x27;
    private const int CmdDacHpMixerSource = 0x29;
    private const int CmdUacOut1Mute = 0x2D;
    private const int CmdUacOut2Mute = 0x2F;
    private const int CmdDacHpMute = 0x31;
    private const int CmdHdr2Sdr = 0x90;
    private const int CmdAviInfoFrame = 0x92;
    private const int CmdAudioFormat = 0x04;
    private const int CmdAudioSamplingRate = 0x06;
    private const int CmdInputSource = 0x35;
    private const int CmdTxEdidValid = 0x43;
    private const int CmdRawTiming = 0x37;
    private const int CmdUsbHostProtocol = 0x40;
    private const int CmdTxHpdStatus = 0x41;
    private const int CmdTxVrr = 0x42;
    private const int CmdUvcOutputTiming = 0x44;
    private const int CmdUvcVideoFormat = 0x45;
    private const int CmdUvcErrStatus = 0x46;
    private const int CmdHdcpMode = 0x72;
    private const int CmdAdcOnOff = 0x74;
    private const int CmdDacHpOnOff = 0x75;
    private const int CmdHdr2SdrExtended = 0x76;
    private const int CmdCustomerVersion = 0x77;
    private const int CmdRescueVersion = 0x78;
    private const int CmdVtem = 0x7D;
    private const int CmdFlashSetCustomerProprietary = 0x51;
    private const int CmdFlashGetCustomerProprietary = 0x52;
    private const int CmdGpioSetParam = 0x5B;
    private const int CmdI2cWrite = 0x1C;
    private const int CmdI2cRead = 0x1B;
    private const int I2cSelector = 4;
    private const int I2cPayloadSize = 525;
    private const int CmdHdcpVersion = 0x7E;
    private const int CmdSetLedLight = 0x84;
    private const int CmdSetHpOutGain = 0x66;
    private const int CmdHpOutGain = 0x67;
    private const int CmdRxTxHdcpVersion = 0x8A;
    private const int CmdUsbCdcOnOff = 0x8B;
    private const int CmdUsbLinkState = 0x8C;
    private const int CmdUsbForceSpeed = 0x8D;
    private const int CmdColorRangeSetting = 0x91;
    private const int CmdBitError = 0x93;
    private const int CmdHdr2SdrColorParam = 0x9B;

    public async Task<SourceSignalTelemetrySnapshot> ReadAsync(
        CaptureDevice? device,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (device == null || string.IsNullOrWhiteSpace(device.Id))
        {
            return SourceSignalTelemetrySnapshot.CreateUnavailable("device-unavailable");
        }

        if (!NativeXuDeviceSupport.TryGetSupported4kXIds(device, out var vendorId, out var productId))
        {
            return SourceSignalTelemetrySnapshot.CreateUnavailable("nativexu-device-unsupported");
        }

        if (string.IsNullOrWhiteSpace(device.NativeXuInterfacePath))
        {
            return SourceSignalTelemetrySnapshot.CreateUnavailable(
                "nativexu-interface-ambiguous",
                "Selected capture device has no resolved native XU interface path.");
        }

        var gateAcquired = false;
        string? unavailableReason = null;
        string? unavailableDetail = null;

        try
        {
            gateAcquired = await NativeXuDeviceSupport.TryAcquireTransportGateAsync(cancellationToken).ConfigureAwait(false);
            if (!gateAcquired)
            {
                return SourceSignalTelemetrySnapshot.CreateUnavailable(
                    "nativexu-native-busy",
                    $"{NativeXuDeviceSupport.DefaultTransportGateTimeoutMs}ms");
            }

            IReadOnlyList<KsExtensionUnitNative.KsInterfacePath> interfaces;
            try
            {
                interfaces = NativeXuDeviceSupport.EnumerateSelectedInterfaces(vendorId, productId, device);
            }
            catch (Exception ex)
            {
                Logger.Log($"NATIVEXU_ENUMERATE_FAILED type={ex.GetType().Name} message={ex.Message}");
                return SourceSignalTelemetrySnapshot.CreateUnavailable("nativexu-enumerate-failed", ex.Message);
            }

            if (interfaces.Count == 0)
            {
                return SourceSignalTelemetrySnapshot.CreateUnavailable("nativexu-interface-not-found");
            }

            foreach (var ksInterface in interfaces)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var attempt = TryReadInterface(ksInterface, cancellationToken);
                if (attempt.Snapshot != null)
                {
                    return attempt.Snapshot;
                }

                if (!string.IsNullOrWhiteSpace(attempt.UnavailableReason))
                {
                    unavailableReason = attempt.UnavailableReason;
                    unavailableDetail = attempt.UnavailableDetail;
                }
            }

            return SourceSignalTelemetrySnapshot.CreateUnavailable(
                unavailableReason ?? "nativexu-read-failed",
                unavailableDetail);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Logger.Log($"NATIVEXU_PROVIDER_EXCEPTION type={ex.GetType().Name} message={ex.Message}");
            return SourceSignalTelemetrySnapshot.CreateUnavailable("nativexu-exception", $"{ex.GetType().Name}: {ex.Message}");
        }
        finally
        {
            if (gateAcquired)
            {
                NativeXuDeviceSupport.ReleaseTransportGate();
            }
        }
    }

    private NodeReadAttempt TryReadInterface(
        KsExtensionUnitNative.KsInterfacePath ksInterface,
        CancellationToken cancellationToken)
    {
        try
        {
            using var handle = KsExtensionUnitNative.TryOpen(ksInterface.Path, out var openErrorCode);
            if (handle is null)
            {
                var detail = DescribeWin32Detail(ksInterface.Path, openErrorCode);
                Logger.Log($"NATIVEXU_OPEN_FAILED path='{ksInterface.Path}' detail='{detail}'");
                return new NodeReadAttempt(null, false, "nativexu-open-failed", detail);
            }

            if (!KsExtensionUnitNative.TryReadTopologyNodes(handle, out var nodes, out var topologyError))
            {
                var detail = $"{ksInterface.Path}: {topologyError ?? "unknown"}";
                Logger.Log($"NATIVEXU_TOPOLOGY_FAILED path='{ksInterface.Path}' error='{topologyError ?? "unknown"}'");
                return new NodeReadAttempt(null, false, "nativexu-topology-read-failed", detail);
            }

            var nodeList = nodes ?? Array.Empty<KsExtensionUnitNative.KsTopologyNode>();
            var devSpecificIds = new List<int>();
            foreach (var node in nodeList)
            {
                if (node.IsDevSpecific)
                {
                    devSpecificIds.Add(node.NodeId);
                }
            }

            Logger.Log(
                $"NATIVEXU_TOPOLOGY path='{ksInterface.Path}' nodeCount={nodeList.Count} " +
                $"devSpecificNodes=[{string.Join(",", devSpecificIds)}]");

            var candidateNodes = devSpecificIds.Count > 0
                ? nodeList.Where(node => node.IsDevSpecific)
                : nodeList.AsEnumerable();

            string? unavailableReason = null;
            string? unavailableDetail = null;

            foreach (var node in candidateNodes)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var attempt = TryReadRolling(handle, node.NodeId, ksInterface.Path, cancellationToken);
                if (attempt.Snapshot != null)
                {
                    return attempt;
                }

                if (attempt.UnsupportedNode)
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(attempt.UnavailableReason))
                {
                    unavailableReason = attempt.UnavailableReason;
                    unavailableDetail = attempt.UnavailableDetail;
                }
            }

            return new NodeReadAttempt(null, false, unavailableReason, unavailableDetail);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            var detail = $"{ksInterface.Path}: {ex.GetType().Name}: {ex.Message}";
            Logger.Log($"NATIVEXU_INTERFACE_EXCEPTION path='{ksInterface.Path}' type={ex.GetType().Name} message={ex.Message}");
            return new NodeReadAttempt(null, false, "nativexu-interface-exception", detail);
        }
    }

    private static NodeReadAttempt CreateUnavailableNodeResult(string interfacePath, string reason)
        => new(null, false, reason, interfacePath);

    private static NodeReadAttempt HandleFailedCommand(string reason, string interfacePath, AtCommandResult result)
    {
        if (IsUnsupportedNodeFailure(result.Win32Code))
        {
            return new NodeReadAttempt(null, true, null, null);
        }

        var detail = DescribeCommandFailure(interfacePath, result);
        return new NodeReadAttempt(null, false, reason, detail);
    }

    private static bool IsUnsupportedNodeFailure(int? win32Code)
        => win32Code is KsExtensionUnitNative.ErrorNotFound
            or KsExtensionUnitNative.ErrorSetNotFound
            or KsExtensionUnitNative.ErrorInvalidParameter
            or KsExtensionUnitNative.ErrorInvalidFunction;

    private static string DescribeCommandFailure(string interfacePath, AtCommandResult result)
        => $"{interfacePath}: {result.Name}:{result.FailureStage ?? "unknown"} win32={FormatWin32Code(result.Win32Code)}";

    private static string DescribeWin32Detail(string path, int? win32Code)
    {
        if (!win32Code.HasValue)
        {
            return $"{path}: unknown";
        }

        return $"{path}: win32={win32Code.Value} ({new Win32Exception(win32Code.Value).Message})";
    }

    private readonly record struct NodeReadAttempt(
        SourceSignalTelemetrySnapshot? Snapshot,
        bool UnsupportedNode,
        string? UnavailableReason,
        string? UnavailableDetail);

    private readonly record struct AtCommandResult(
        string Name,
        int CommandCode,
        bool Success,
        byte[] Response,
        int? Win32Code,
        string? FailureStage);

    private readonly record struct HdrMetadataInfo(bool HasMetadata, byte? Eotf, bool? IsHdr);

    private readonly record struct AviInfoFrameInfo(
        bool HasData,
        string? ColorSpace,
        string? Colorimetry,
        string? Quantization)
    {
        public static AviInfoFrameInfo Empty => new(false, null, null, null);
    }

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

    private static AtCommandResult SendAtCommand(
        SafeFileHandle handle,
        int nodeId,
        string name,
        int cmdCode)
    {
        var requestFrame = BuildAtReadFrame(cmdCode);
        var triggerData = new byte[]
        {
            (byte)(requestFrame.Length & 0xFF),
            (byte)((requestFrame.Length >> 8) & 0xFF)
        };

        if (!KsExtensionUnitNative.TryXuSetViaOutput(handle, nodeId, XuGuid, AtTriggerSelector, triggerData, out var triggerWin32))
        {
            Logger.Log($"NATIVEXU_AT_FAILED cmd={name} code=0x{cmdCode:X2} stage=trigger win32={FormatWin32Code(triggerWin32)}");
            return new AtCommandResult(name, cmdCode, false, Array.Empty<byte>(), triggerWin32, "trigger");
        }

        if (!KsExtensionUnitNative.TryXuSetViaOutput(handle, nodeId, XuGuid, AtPayloadSelector, requestFrame, out var sendWin32))
        {
            Logger.Log($"NATIVEXU_AT_FAILED cmd={name} code=0x{cmdCode:X2} stage=send win32={FormatWin32Code(sendWin32)}");
            return new AtCommandResult(name, cmdCode, false, Array.Empty<byte>(), sendWin32, "send");
        }

        if (!KsExtensionUnitNative.TryXuGetDirect(
                handle,
                nodeId,
                XuGuid,
                AtTriggerSelector,
                2,
                out var lengthData,
                out var lengthBytes,
                out var lengthWin32))
        {
            Logger.Log($"NATIVEXU_AT_FAILED cmd={name} code=0x{cmdCode:X2} stage=getlength win32={FormatWin32Code(lengthWin32)}");
            return new AtCommandResult(name, cmdCode, false, Array.Empty<byte>(), lengthWin32, "getlength");
        }

        var responseFrameLen = lengthBytes >= 2
            ? (int)BitConverter.ToUInt16(lengthData, 0)
            : 0;

        if (responseFrameLen <= 0 || responseFrameLen > MaxAtResponseFrameSize)
        {
            Logger.Log($"NATIVEXU_AT_FAILED cmd={name} code=0x{cmdCode:X2} stage=framelen len={responseFrameLen}");
            return new AtCommandResult(name, cmdCode, false, Array.Empty<byte>(), null, "framelen");
        }

        if (!KsExtensionUnitNative.TryXuGetDirect(
                handle,
                nodeId,
                XuGuid,
                AtPayloadSelector,
                responseFrameLen,
                out var responseFrame,
                out var responseBytes,
                out var responseWin32))
        {
            Logger.Log($"NATIVEXU_AT_FAILED cmd={name} code=0x{cmdCode:X2} stage=getresponse win32={FormatWin32Code(responseWin32)}");
            return new AtCommandResult(name, cmdCode, false, Array.Empty<byte>(), responseWin32, "getresponse");
        }

        var rawData = StripAtFrameEnvelope(responseFrame, responseBytes);
        Logger.Log(
            $"NATIVEXU_AT cmd={name} code=0x{cmdCode:X2} frameLen={responseFrameLen} " +
            $"rawBytes={rawData.Length} preview={GetHexPreview(rawData, rawData.Length, 32)}");
        return new AtCommandResult(name, cmdCode, true, rawData, null, null);
    }

    private static bool SendAtSetCommand(
        SafeFileHandle handle,
        int nodeId,
        int cmdCode,
        byte[] inputData,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var requestFrame = BuildAtWriteFrame(cmdCode, inputData);
        var triggerData = new byte[]
        {
            (byte)(requestFrame.Length & 0xFF),
            (byte)((requestFrame.Length >> 8) & 0xFF)
        };

        cancellationToken.ThrowIfCancellationRequested();
        if (!KsExtensionUnitNative.TryXuSetViaOutput(handle, nodeId, XuGuid, AtTriggerSelector, triggerData, out var triggerWin32))
        {
            Logger.Log($"NATIVEXU_SET_FAILED cmd=0x{cmdCode:X2} stage=trigger win32={FormatWin32Code(triggerWin32)}");
            return false;
        }

        cancellationToken.ThrowIfCancellationRequested();
        if (!KsExtensionUnitNative.TryXuSetViaOutput(handle, nodeId, XuGuid, AtPayloadSelector, requestFrame, out var sendWin32))
        {
            Logger.Log($"NATIVEXU_SET_FAILED cmd=0x{cmdCode:X2} stage=send win32={FormatWin32Code(sendWin32)}");
            return false;
        }

        cancellationToken.ThrowIfCancellationRequested();
        KsExtensionUnitNative.TryXuGetDirect(handle, nodeId, XuGuid, AtTriggerSelector, 2, out _, out _, out _);
        return true;
    }

    private static bool SendSelector4Command(
        SafeFileHandle handle,
        int nodeId,
        int cmdCode,
        byte[] inputData,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var atFrame = BuildAtWriteFrame(cmdCode, inputData);
        var payload = new byte[I2cPayloadSize];
        Array.Copy(atFrame, 0, payload, 0, atFrame.Length);

        cancellationToken.ThrowIfCancellationRequested();
        if (!KsExtensionUnitNative.TryXuSetViaOutput(handle, nodeId, XuGuid, I2cSelector, payload, out var win32))
        {
            Logger.Log($"NATIVEXU_SEL4_FAILED cmd=0x{cmdCode:X2} win32={FormatWin32Code(win32)}");
            return false;
        }

        return true;
    }

    private static byte ComputeLrc(ReadOnlySpan<byte> data)
    {
        byte sum = 0;
        foreach (var value in data)
        {
            sum = (byte)(sum + value);
        }

        return (byte)(~sum + 1);
    }

    private static byte[] BuildAtReadFrame(int cmdCode)
    {
        var frame = new byte[9];
        frame[0] = 0xA1;
        frame[1] = 0x06;
        frame[4] = (byte)(cmdCode & 0xFF);
        frame[5] = (byte)((cmdCode >> 8) & 0xFF);
        frame[6] = (byte)((cmdCode >> 16) & 0xFF);
        frame[7] = (byte)((cmdCode >> 24) & 0xFF);
        frame[8] = ComputeLrc(frame.AsSpan(0, 8));
        return frame;
    }

    private static byte[] BuildAtWriteFrame(int cmdCode, byte[] inputData)
    {
        var dataLen = 4 + inputData.Length;
        var frameLen = AtFrameHeaderSize + dataLen + AtFrameLrcSize;
        var frame = new byte[frameLen];
        frame[0] = 0xA1;
        frame[1] = (byte)((dataLen + 2) & 0x7F);
        frame[4] = (byte)(cmdCode & 0xFF);
        frame[5] = (byte)((cmdCode >> 8) & 0xFF);
        frame[6] = (byte)((cmdCode >> 16) & 0xFF);
        frame[7] = (byte)((cmdCode >> 24) & 0xFF);
        if (inputData.Length > 0)
        {
            Array.Copy(inputData, 0, frame, 8, inputData.Length);
        }

        frame[frameLen - 1] = ComputeLrc(frame.AsSpan(0, frameLen - 1));
        return frame;
    }

    private static byte[] StripAtFrameEnvelope(byte[] responseFrame, int frameLength)
    {
        var effectiveLength = Math.Min(Math.Max(frameLength, 0), responseFrame.Length);
        if (effectiveLength <= AtFrameHeaderSize + AtFrameLrcSize)
        {
            return Array.Empty<byte>();
        }

        var dataLength = effectiveLength - AtFrameHeaderSize - AtFrameLrcSize;
        var result = new byte[dataLength];
        Array.Copy(responseFrame, AtFrameHeaderSize, result, 0, dataLength);
        return result;
    }

    private static string FormatWin32Code(int? win32Code)
        => win32Code.HasValue ? win32Code.Value.ToString(CultureInfo.InvariantCulture) : "unknown";

    private static string GetHexPreview(byte[] buffer, int bytesReturned, int maxBytes)
    {
        if (buffer.Length == 0 || bytesReturned <= 0)
        {
            return "empty";
        }

        var previewLength = Math.Min(Math.Min(bytesReturned, buffer.Length), maxBytes);
        return previewLength > 0
            ? Convert.ToHexString(buffer.AsSpan(0, previewLength))
            : "empty";
    }

    private static int? ExtractInt32AsVicCode(byte[] buffer)
    {
        if (buffer.Length < 4 || !HasNonZeroData(buffer))
        {
            return null;
        }

        var value = BitConverter.ToInt32(buffer, 0);
        return value > 0 ? value : null;
    }

    private static AviInfoFrameInfo DecodeAviInfoFrame(byte[] buffer)
    {
        if (buffer.Length < 8 || !HasNonZeroData(buffer) || buffer[0] != 0x82)
        {
            return AviInfoFrameInfo.Empty;
        }

        var db1 = buffer[4];
        var db2 = buffer[5];
        var db3 = buffer[6];

        var colorSpace = ((db1 >> 5) & 0x03) switch
        {
            0 => "RGB",
            1 => "YCbCr422",
            2 => "YCbCr444",
            3 => "YCbCr420",
            _ => null
        };

        var colorimetry = ((db2 >> 6) & 0x03) switch
        {
            0 => null,
            1 => "BT.601",
            2 => "BT.709",
            3 => ((db3 >> 4) & 0x07) switch
            {
                0 => "xvYCC601",
                1 => "xvYCC709",
                2 => "sYCC601",
                3 => "AdobeYCC601",
                4 => "AdobeRGB",
                5 => "BT.2020cYCC",
                6 => "BT.2020",
                7 => "Reserved",
                _ => null
            },
            _ => null
        };

        var quantization = ((db3 >> 2) & 0x03) switch
        {
            0 => "Default",
            1 => "Limited",
            2 => "Full",
            _ => "Reserved"
        };

        return new AviInfoFrameInfo(true, colorSpace, colorimetry, quantization);
    }

    private static HdrMetadataInfo DecodeHdrMetadata(byte[] buffer)
    {
        const int InfoFrameTypeOffset = 0;
        const int InfoFrameLengthOffset = 2;
        const int HdrStaticMetadataInfoFrameType = 0x87;
        const int HdrStaticMetadataChecksumOffset = 3;
        const int HdrStaticMetadataDataStartOffset = HdrStaticMetadataChecksumOffset + 1;
        const int HdrStaticMetadataEotfOffset = HdrStaticMetadataDataStartOffset;

        if (buffer.Length <= HdrStaticMetadataEotfOffset ||
            !HasNonZeroData(buffer) ||
            buffer[InfoFrameTypeOffset] != HdrStaticMetadataInfoFrameType ||
            buffer[InfoFrameLengthOffset] < 1)
        {
            return new HdrMetadataInfo(false, null, null);
        }

        var eotf = buffer[HdrStaticMetadataEotfOffset];
        var isHdr = eotf switch
        {
            2 or 3 => true,
            0 or 1 => false,
            _ => (bool?)null
        };
        return new HdrMetadataInfo(true, eotf, isHdr);
    }

    private static double SnapToCanonicalFrameRate(double measured)
    {
        const double tolerance = 0.05;
        foreach (var canonical in CanonicalFrameRates)
        {
            if (Math.Abs(measured - canonical) <= tolerance)
            {
                return canonical;
            }
        }

        return measured;
    }

    private static string? InferFrameRateRational(double? frameRate)
    {
        if (!frameRate.HasValue || frameRate.Value <= 0)
        {
            return null;
        }

        var value = frameRate.Value;
        var rounded = Math.Round(value);
        if (Math.Abs(value - rounded) <= 0.01 && rounded > 0)
        {
            return $"{(int)rounded}/1";
        }

        if (rounded > 0)
        {
            var ntscCandidate = rounded * 1000.0 / 1001.0;
            if (Math.Abs(value - ntscCandidate) <= 0.03)
            {
                return $"{(int)rounded * 1000}/1001";
            }
        }

        return value.ToString("0.###", CultureInfo.InvariantCulture);
    }

    private static SourceTelemetryConfidence ResolveConfidence(
        bool hasVicCode,
        HdrMetadataInfo hdrInfo,
        AviInfoFrameInfo aviInfoFrame,
        double? frameRateExact)
    {
        if (hasVicCode && hdrInfo.HasMetadata)
        {
            return SourceTelemetryConfidence.High;
        }

        if (hasVicCode)
        {
            return SourceTelemetryConfidence.Medium;
        }

        if (aviInfoFrame.HasData || hdrInfo.HasMetadata || frameRateExact.HasValue)
        {
            return SourceTelemetryConfidence.Low;
        }

        return SourceTelemetryConfidence.Unknown;
    }

    private static bool TryReadInt32(byte[] buffer, out int value)
    {
        value = 0;
        if (buffer.Length < 4)
        {
            return false;
        }

        value = BitConverter.ToInt32(buffer, 0);
        return true;
    }

    private static int? TryReadInt16(byte[] buffer)
        => buffer.Length >= 2 ? BitConverter.ToInt16(buffer, 0) : null;

    private static bool? TryReadBoolean(byte[] buffer)
        => buffer.Length >= 1 ? buffer[0] != 0 : null;

    private static string? TryDecodePrintableAscii(byte[] buffer)
    {
        if (!HasNonZeroData(buffer))
        {
            return null;
        }

        var terminatorIndex = Array.IndexOf(buffer, (byte)0);
        if (terminatorIndex < 0)
        {
            terminatorIndex = buffer.Length;
        }

        if (terminatorIndex == 0)
        {
            return null;
        }

        for (var i = 0; i < terminatorIndex; i++)
        {
            var value = buffer[i];
            if (value < 0x20 || value > 0x7E)
            {
                return null;
            }
        }

        var decoded = Encoding.ASCII.GetString(buffer, 0, terminatorIndex).Trim();
        return string.IsNullOrWhiteSpace(decoded) ? null : decoded;
    }

    private static string? DecodeCString(byte[] buffer)
    {
        if (!HasNonZeroData(buffer))
        {
            return null;
        }

        var terminatorIndex = Array.IndexOf(buffer, (byte)0);
        if (terminatorIndex < 0)
        {
            terminatorIndex = buffer.Length;
        }

        var decoded = Encoding.ASCII.GetString(buffer, 0, terminatorIndex).Trim();
        return string.IsNullOrWhiteSpace(decoded) ? null : decoded;
    }

    private static string BoolToToken(bool? value)
        => value switch
        {
            true => "true",
            false => "false",
            _ => "unknown"
        };

    private static bool HasNonZeroData(byte[] buffer)
        => buffer.AsSpan().IndexOfAnyExcept((byte)0) >= 0;

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

    private static IReadOnlyList<SourceTelemetryDetailEntry> BuildDetailEntries(
        AviInfoFrameInfo aviInfoFrame,
        HdrMetadataInfo hdrInfo,
        byte? hdr2SdrState,
        string? systemInfo,
        AtCommandResult audioFormat,
        AtCommandResult audioSamplingRate,
        AtCommandResult inputSource,
        AtCommandResult adcOnOff,
        AtCommandResult adcVolumeGain,
        AtCommandResult uacVolumeGain,
        AtCommandResult uacOut1Mute,
        AtCommandResult uacOut2Mute,
        AtCommandResult uacOut2MixerSource,
        AtCommandResult usbHostProtocol,
        AtCommandResult usbCdc,
        AtCommandResult usbLinkState,
        AtCommandResult usbForceSpeed,
        AtCommandResult txHpd,
        AtCommandResult txVrr,
        AtCommandResult txEdidValid,
        AtCommandResult uvcOutputTiming,
        AtCommandResult uvcVideoFormat,
        AtCommandResult uvcErrStatus,
        AtCommandResult hdcpMode,
        AtCommandResult hdcpVersion,
        AtCommandResult rxTxHdcpVersion,
        AtCommandResult hdr2SdrExtended,
        AtCommandResult customerVersion,
        AtCommandResult rescueVersion,
        AtCommandResult hdr2SdrColorParam,
        AtCommandResult colorRangeSetting,
        AtCommandResult rawTiming,
        int? vicCode,
        int? vfreqHz100)
    {
        var details = new List<SourceTelemetryDetailEntry>();

        AddDetail(details, "Signal Details", "Video Format", aviInfoFrame.ColorSpace);
        AddDetail(details, "Signal Details", "Colorimetry", aviInfoFrame.Colorimetry);
        AddDetail(details, "Signal Details", "Quantization", aviInfoFrame.Quantization);
        AddDetail(
            details,
            "Signal Details",
            "HDR Transfer",
            ResolveHdrTransferFunction(hdrInfo.Eotf),
            hdrInfo.Eotf?.ToString(CultureInfo.InvariantCulture));
        AddDetail(
            details,
            "Signal Details",
            "HDR to SDR",
            hdr2SdrState switch
            {
                0 => "Off",
                1 => "On",
                _ => null
            },
            hdr2SdrState?.ToString(CultureInfo.InvariantCulture));
        AddDetail(details, "Signal Details", "VIC", vicCode?.ToString(CultureInfo.InvariantCulture));
        AddDetail(details, "Signal Details", "Vert Freq", vfreqHz100.HasValue ? $"{vfreqHz100.Value / 100.0:0.##} Hz" : null, vfreqHz100?.ToString(CultureInfo.InvariantCulture));

        AddAtDetail(details, TelemetryLabels.GroupAudioInput, "Input Source", inputSource, FormatInputSourceDetail);
        AddAtDetail(details, TelemetryLabels.GroupAudioInput, "Audio Format", audioFormat, FormatAudioFormatDetail);
        AddAtDetail(details, TelemetryLabels.GroupAudioInput, "Audio Sample Rate", audioSamplingRate, FormatAudioSampleRateDetail);
        AddAtDetail(details, TelemetryLabels.GroupAudioInput, TelemetryLabels.AdcAnalog, adcOnOff, FormatOnOffByteDetail);
        AddAtDetail(details, TelemetryLabels.GroupAudioInput, "ADC Gain", adcVolumeGain, FormatDecimalInt16Detail);

        AddAtDetail(details, "Audio / USB", "UAC Volume", uacVolumeGain, FormatDecimalInt16Detail);
        AddAtDetail(details, "Audio / USB", "UAC Out1 Mute", uacOut1Mute, FormatMuteByteDetail);
        AddAtDetail(details, "Audio / USB", "UAC Out2 Mute", uacOut2Mute, FormatMuteByteDetail);
        AddAtDetail(details, "Audio / USB", "UAC Out2 Mixer", uacOut2MixerSource, FormatDecimalInt16Detail);

        AddAtDetail(details, "Link / Protection", "USB Protocol", usbHostProtocol, FormatUsbHostProtocolDetail);
        AddAtDetail(details, "Link / Protection", "USB CDC", usbCdc, FormatCodeByteDetail);
        AddAtDetail(details, "Link / Protection", "USB Link State", usbLinkState, FormatCodeByteDetail);
        AddAtDetail(details, "Link / Protection", "USB Speed", usbForceSpeed, FormatCodeByteDetail);
        AddAtDetail(details, "Link / Protection", "TX Hot Plug", txHpd, FormatModeInt32Detail);
        AddAtDetail(details, "Link / Protection", "TX VRR", txVrr, FormatModeInt32Detail);
        AddAtDetail(details, "Link / Protection", "TX EDID Valid", txEdidValid, FormatValidByteDetail);
        AddAtDetail(details, "Link / Protection", "HDCP Mode", hdcpMode, FormatHdcpModeDetail);
        AddAtDetail(details, "Link / Protection", "HDCP Version", hdcpVersion, FormatHdcpVersionDetail);
        AddAtDetail(details, "Link / Protection", "RX/TX HDCP", rxTxHdcpVersion, FormatRxTxHdcpVersionDetail);

        AddAtDetail(details, "Capture Card / UVC", "UVC Timing", uvcOutputTiming, FormatHexDetail);
        AddAtDetail(details, "Capture Card / UVC", "UVC Format", uvcVideoFormat, FormatHexDetail);
        AddAtDetail(details, "Capture Card / UVC", "UVC Error", uvcErrStatus, FormatCodeByteDetail);

        AddAtDetail(details, "Raw / Firmware", "HDR2SDR Status", hdr2SdrExtended, FormatModeInt32Detail);
        AddAtDetail(details, "Raw / Firmware", "Customer Version", customerVersion, FormatAsciiOrHexDetail);
        AddAtDetail(details, "Raw / Firmware", "Rescue Version", rescueVersion, FormatDecimalInt32Detail);
        AddAtDetail(details, "Raw / Firmware", "HDR2SDR Color", hdr2SdrColorParam, FormatHexDetail);
        AddAtDetail(details, "Raw / Firmware", "Color Range", colorRangeSetting, FormatCodeByteDetail);
        AddAtDetail(details, "Raw / Firmware", "Raw Timing", rawTiming, FormatHexDetail);

        return details;
    }

    private static void AddDetail(
        ICollection<SourceTelemetryDetailEntry> details,
        string group,
        string label,
        string? value,
        string? rawValue = null)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        var displayValue = string.IsNullOrWhiteSpace(rawValue) || string.Equals(value, rawValue, StringComparison.OrdinalIgnoreCase)
            ? value
            : $"{value} ({rawValue})";

        details.Add(new SourceTelemetryDetailEntry(group, label, displayValue, rawValue));
    }

    private static void AddAtDetail(
        ICollection<SourceTelemetryDetailEntry> details,
        string group,
        string label,
        AtCommandResult result,
        Func<byte[], (string Value, string? RawValue)> formatter)
    {
        if (!result.Success || result.Response.Length == 0)
        {
            details.Add(new SourceTelemetryDetailEntry(group, label, "Unavailable", result.FailureStage));
            return;
        }

        var formatted = formatter(result.Response);
        AddDetail(details, group, label, formatted.Value, formatted.RawValue);
    }

    private static string? TryFormatAtDetailValue(
        AtCommandResult result,
        Func<byte[], (string Value, string? RawValue)> formatter)
    {
        if (!result.Success || result.Response.Length == 0)
        {
            return null;
        }

        return BuildDisplayValue(formatter(result.Response));
    }

    private static string? ResolveHdrTransferFunction(byte? eotf)
        => eotf switch
        {
            0 => "SDR",
            1 => "Traditional HDR",
            2 => "HDR10 / PQ",
            3 => "HLG",
            _ => eotf.HasValue ? "Unknown" : null
        };

    private static string? BuildDisplayValue((string Value, string? RawValue) formatted)
    {
        if (string.IsNullOrWhiteSpace(formatted.Value))
        {
            return null;
        }

        return string.IsNullOrWhiteSpace(formatted.RawValue) ||
               string.Equals(formatted.Value, formatted.RawValue, StringComparison.OrdinalIgnoreCase)
            ? formatted.Value
            : $"{formatted.Value} ({formatted.RawValue})";
    }

    /// <summary>
    /// Validates flash audio data: [source, 0x80, gainByte, 0xAA, 0x55, ...].
    /// Byte[2] is the gain value (0x00-0xFF), NOT part of the magic signature.
    /// </summary>
    private static bool IsValidFlashAudioData(AtCommandResult flashResult)
        => flashResult.Success && flashResult.Response.Length >= 5 &&
           flashResult.Response[1] == 0x80 &&
           flashResult.Response[3] == 0xAA && flashResult.Response[4] == 0x55;

    /// <summary>
    /// Resolves the audio input source from flash proprietary data (AT 0x52).
    /// The flash response contains 0x80, the gain byte, 0xAA, 0x55 at bytes 1-4
    /// and the source byte at offset 0: 0x00=HDMI, 0x01=Analog.
    /// Falls back to the AT 0x35 telemetry value if flash read fails.
    /// </summary>
    private static string? ResolveAudioInputSource(AtCommandResult flashResult, string? fallback)
    {
        if (IsValidFlashAudioData(flashResult))
        {
            return flashResult.Response[0] == 0 ? DeviceAudioMode.Hdmi : DeviceAudioMode.Analog;
        }

        return fallback;
    }

    private static SourceAudioInputMode? ResolveAudioInputMode(AtCommandResult flashResult, AtCommandResult inputSourceResult)
    {
        if (IsValidFlashAudioData(flashResult))
        {
            return flashResult.Response[0] == 0 ? SourceAudioInputMode.Hdmi : SourceAudioInputMode.Analog;
        }

        if (inputSourceResult.Success && inputSourceResult.Response.Length >= 1)
        {
            return inputSourceResult.Response[0] == 0 ? SourceAudioInputMode.Hdmi : SourceAudioInputMode.Analog;
        }

        return null;
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

    private static int? ResolveAnalogGainByte(AtCommandResult flashResult)
        => IsValidFlashAudioData(flashResult)
            ? flashResult.Response[2]
            : null;

    private static IReadOnlyList<SourceTelemetryDetailEntry> AppendFlashAudioAnalogGainDetail(
        IReadOnlyList<SourceTelemetryDetailEntry> detailEntries,
        AtCommandResult flashResult)
    {
        var analogGainByte = ResolveAnalogGainByte(flashResult);
        if (!analogGainByte.HasValue)
        {
            return detailEntries;
        }

        var mutable = new List<SourceTelemetryDetailEntry>(detailEntries);
        var lastAudioIdx = mutable.FindLastIndex(d => d.Group == TelemetryLabels.GroupAudioInput);
        var insertIdx = lastAudioIdx >= 0 ? lastAudioIdx + 1 : mutable.Count;
        mutable.Insert(insertIdx,
            new SourceTelemetryDetailEntry(
                TelemetryLabels.GroupAudioInput,
                TelemetryLabels.AnalogGain,
                FormatAnalogGainDisplayValue((byte)analogGainByte.Value),
                analogGainByte.Value.ToString(CultureInfo.InvariantCulture)));
        return mutable;
    }

    private static string FormatAnalogGainDisplayValue(byte gainByte)
    {
        var y = gainByte / 255.0;
        var gainPct = (Math.Exp(4.0 * y) - 1.0) / (Math.Exp(4.0) - 1.0) * 100.0;
        return $"0x{gainByte:X2} ({gainPct:0}%)";
    }

    private static (string Value, string? RawValue) FormatInputSourceDetail(byte[] data)
    {
        var raw = data[0].ToString(CultureInfo.InvariantCulture);
        var value = data[0] switch
        {
            0 => DeviceAudioMode.Hdmi,
            1 => DeviceAudioMode.Analog,
            _ => "Unknown"
        };
        return (value, raw);
    }

    private static (string Value, string? RawValue) FormatAudioFormatDetail(byte[] data)
    {
        var raw = data[0].ToString(CultureInfo.InvariantCulture);
        return ("Unknown", raw);
    }

    private static (string Value, string? RawValue) FormatAudioSampleRateDetail(byte[] data)
    {
        var raw = data[0].ToString(CultureInfo.InvariantCulture);
        return ("Unknown", raw);
    }

    private static (string Value, string? RawValue) FormatUsbHostProtocolDetail(byte[] data)
    {
        if (data.Length < 4)
        {
            return ("Unavailable", null);
        }

        var rawValue = BitConverter.ToInt32(data, 0);
        var raw = rawValue.ToString(CultureInfo.InvariantCulture);
        var value = rawValue switch
        {
            0 => "Undefined",
            1 => "Bulk",
            2 => "Isochronous",
            _ => "Unknown"
        };
        return (value, raw);
    }

    private static (string Value, string? RawValue) FormatHdcpModeDetail(byte[] data)
    {
        var raw = data[0].ToString(CultureInfo.InvariantCulture);
        return ("Unknown", raw);
    }

    private static (string Value, string? RawValue) FormatHdcpVersionDetail(byte[] data)
    {
        var raw = Convert.ToHexString(data);
        return (raw, raw);
    }

    private static (string Value, string? RawValue) FormatRxTxHdcpVersionDetail(byte[] data)
    {
        if (data.Length < 2)
        {
            return ("Unavailable", null);
        }

        var raw = BitConverter.ToInt16(data, 0).ToString(CultureInfo.InvariantCulture);
        return ("Unknown", raw);
    }

    private static (string Value, string? RawValue) FormatCodeByteDetail(byte[] data)
    {
        var raw = data[0].ToString(CultureInfo.InvariantCulture);
        return ($"Code {raw}", raw);
    }

    private static (string Value, string? RawValue) FormatOnOffByteDetail(byte[] data)
    {
        var raw = data[0].ToString(CultureInfo.InvariantCulture);
        var value = data[0] switch
        {
            0 => "Off",
            1 => "On",
            _ => "Unknown"
        };
        return (value, raw);
    }

    private static (string Value, string? RawValue) FormatMuteByteDetail(byte[] data)
    {
        var raw = data[0].ToString(CultureInfo.InvariantCulture);
        var value = data[0] switch
        {
            0 => "Unmuted",
            1 => "Muted",
            _ => "Unknown"
        };
        return (value, raw);
    }

    private static (string Value, string? RawValue) FormatValidByteDetail(byte[] data)
    {
        var raw = data[0].ToString(CultureInfo.InvariantCulture);
        var value = data[0] switch
        {
            0 => "Invalid",
            1 => "Valid",
            _ => "Unknown"
        };
        return (value, raw);
    }

    private static (string Value, string? RawValue) FormatDecimalInt16Detail(byte[] data)
    {
        if (data.Length < 2)
        {
            return ("Unavailable", null);
        }

        var raw = BitConverter.ToInt16(data, 0).ToString(CultureInfo.InvariantCulture);
        return (raw, raw);
    }

    private static (string Value, string? RawValue) FormatDecimalInt32Detail(byte[] data)
    {
        if (data.Length < 4)
        {
            return ("Unavailable", null);
        }

        var raw = BitConverter.ToInt32(data, 0).ToString(CultureInfo.InvariantCulture);
        return (raw, raw);
    }

    private static (string Value, string? RawValue) FormatModeInt16Detail(byte[] data)
    {
        if (data.Length < 2)
        {
            return ("Unavailable", null);
        }

        var raw = BitConverter.ToInt16(data, 0).ToString(CultureInfo.InvariantCulture);
        return ($"Mode {raw}", raw);
    }

    private static (string Value, string? RawValue) FormatModeInt32Detail(byte[] data)
    {
        if (data.Length < 4)
        {
            return ("Unavailable", null);
        }

        var raw = BitConverter.ToInt32(data, 0).ToString(CultureInfo.InvariantCulture);
        return ($"Mode {raw}", raw);
    }

    private static (string Value, string? RawValue) FormatHexDetail(byte[] data)
    {
        var raw = Convert.ToHexString(data);
        return (raw, raw);
    }

    private static (string Value, string? RawValue) FormatAsciiOrHexDetail(byte[] data)
    {
        var raw = Convert.ToHexString(data);
        var ascii = TryDecodePrintableAscii(data);
        return string.IsNullOrWhiteSpace(ascii)
            ? (raw, raw)
            : (ascii, raw);
    }

    /// <summary>
    /// Sends an AT SET command to the Realtek chip. Opens its own KS handle.
    /// </summary>
    public static async Task<bool> SendAtSetCommandAsync(
        CaptureDevice? device,
        int cmdCode,
        byte[] inputData,
        CancellationToken cancellationToken = default)
    {
        if (device == null || string.IsNullOrWhiteSpace(device.Id))
        {
            return false;
        }

        if (!NativeXuDeviceSupport.TryGetSupported4kXIds(device, out var vendorId, out var productId))
        {
            return false;
        }

        if (!NativeXuDeviceSupport.HasSelectedInterface(device, "SET"))
        {
            return false;
        }

        var gateAcquired = false;
        try
        {
            gateAcquired = await NativeXuDeviceSupport.TryAcquireTransportGateAsync(cancellationToken).ConfigureAwait(false);
            if (!gateAcquired)
            {
                return false;
            }

            var interfaces = NativeXuDeviceSupport.EnumerateSelectedInterfaces(vendorId, productId, device);
            foreach (var ksInterface in interfaces)
            {
                cancellationToken.ThrowIfCancellationRequested();
                using var handle = KsExtensionUnitNative.TryOpen(ksInterface.Path, out _);
                if (handle is null)
                {
                    continue;
                }

                if (!KsExtensionUnitNative.TryReadTopologyNodes(handle, out var nodes, out _))
                {
                    continue;
                }

                var nodeList = nodes ?? Array.Empty<KsExtensionUnitNative.KsTopologyNode>();
                foreach (var node in nodeList)
                {
                    if (!node.IsDevSpecific)
                    {
                        continue;
                    }

                    var result = SendAtSetCommand(handle, node.NodeId, cmdCode, inputData, cancellationToken);
                    if (result)
                    {
                        Logger.Log($"NATIVEXU_SET_OK cmd=0x{cmdCode:X2} inputLen={inputData.Length}");
                        return true;
                    }
                }
            }

            return false;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Logger.Log($"NATIVEXU_SET_EXCEPTION cmd=0x{cmdCode:X2} type={ex.GetType().Name} msg={ex.Message}");
            return false;
        }
        finally
        {
            if (gateAcquired)
            {
                NativeXuDeviceSupport.ReleaseTransportGate();
            }
        }
    }

    public static async Task<byte[]?> ReadAtCommandAsync(
        CaptureDevice? device,
        int cmdCode,
        string label,
        CancellationToken cancellationToken = default)
    {
        if (device == null || string.IsNullOrWhiteSpace(device.Id))
        {
            return null;
        }

        if (!NativeXuDeviceSupport.TryGetSupported4kXIds(device, out var vendorId, out var productId))
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(device.NativeXuInterfacePath))
        {
            return null;
        }

        var gateAcquired = false;
        try
        {
            gateAcquired = await NativeXuDeviceSupport.TryAcquireTransportGateAsync(cancellationToken).ConfigureAwait(false);
            if (!gateAcquired)
            {
                return null;
            }

            var interfaces = NativeXuDeviceSupport.EnumerateSelectedInterfaces(vendorId, productId, device);
            foreach (var ksInterface in interfaces)
            {
                cancellationToken.ThrowIfCancellationRequested();
                using var handle = KsExtensionUnitNative.TryOpen(ksInterface.Path, out _);
                if (handle is null)
                {
                    continue;
                }

                if (!KsExtensionUnitNative.TryReadTopologyNodes(handle, out var nodes, out _))
                {
                    continue;
                }

                var nodeList = nodes ?? Array.Empty<KsExtensionUnitNative.KsTopologyNode>();
                foreach (var node in nodeList)
                {
                    if (!node.IsDevSpecific)
                    {
                        continue;
                    }

                    var result = SendAtCommand(handle, node.NodeId, label, cmdCode);
                    if (result.Success)
                    {
                        return result.Response;
                    }
                }
            }

            return null;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Logger.Log($"NATIVEXU_GET_EXCEPTION cmd=0x{cmdCode:X2} type={ex.GetType().Name} msg={ex.Message}");
            return null;
        }
        finally
        {
            if (gateAcquired)
            {
                NativeXuDeviceSupport.ReleaseTransportGate();
            }
        }
    }

    public static Task<bool> SetInputSourceAsync(
        CaptureDevice? device,
        int source,
        CancellationToken ct = default)
        => SendNamedSetCommandAsync(
            device,
            CmdSetInputSource,
            new byte[] { (byte)source },
            $"InputSource source={source}",
            ct);

    public static Task<bool> SetAdcOnOffAsync(
        CaptureDevice? device,
        bool on,
        CancellationToken ct = default)
        => SendNamedSetCommandAsync(
            device,
            CmdSetAdcOnOff,
            BitConverter.GetBytes(on ? 1 : 0),
            $"AdcOnOff on={on}",
            ct);

    public static Task<bool> SetAdcVolumeGainAsync(
        CaptureDevice? device,
        int gain,
        CancellationToken ct = default)
        => SendNamedSetCommandAsync(
            device,
            CmdSetAdcVolumeGain,
            BitConverter.GetBytes(gain),
            $"AdcVolumeGain gain={gain}",
            ct);

    public static Task<bool> SetHdr2SdrOnOffAsync(
        CaptureDevice? device,
        bool on,
        CancellationToken ct = default)
        => SendNamedSetCommandAsync(
            device,
            CmdSetHdr2Sdr,
            new byte[] { on ? (byte)1 : (byte)0 },
            $"Hdr2Sdr on={on}",
            ct);

    public static Task<bool> SetLedLightAsync(
        CaptureDevice? device,
        int value,
        CancellationToken ct = default)
        => SendNamedSetCommandAsync(
            device,
            CmdSetLedLight,
            BitConverter.GetBytes(value),
            $"LedLight value={value}",
            ct);

    public static Task<bool> SetDacHpOnOffAsync(
        CaptureDevice? device,
        bool on,
        CancellationToken ct = default)
        => SendNamedSetCommandAsync(
            device,
            CmdSetDacHpOnOff,
            BitConverter.GetBytes(on ? 1 : 0),
            $"DacHpOnOff on={on}",
            ct);

    public static Task<bool> SetDacHpMuteAsync(
        CaptureDevice? device,
        bool mute,
        CancellationToken ct = default)
        => SendNamedSetCommandAsync(
            device,
            0x30,
            new byte[] { mute ? (byte)1 : (byte)0 },
            $"DacHpMute mute={mute}",
            ct);

    public static Task<bool> SetHpOutGainAsync(
        CaptureDevice? device,
        int gain,
        CancellationToken ct = default)
        => SendNamedSetCommandAsync(
            device,
            CmdSetHpOutGain,
            BitConverter.GetBytes((short)gain),
            $"HpOutGain gain={gain}",
            ct);

    // Public wrapper for probe tools
    public static Task<bool> SendNamedSetCommandPublicAsync(
        CaptureDevice? device,
        int cmdCode,
        byte[] inputData,
        string operation,
        CancellationToken cancellationToken = default)
        => SendNamedSetCommandAsync(device, cmdCode, inputData, operation, cancellationToken);

    private static async Task<bool> SendNamedSetCommandAsync(
        CaptureDevice? device,
        int cmdCode,
        byte[] inputData,
        string operation,
        CancellationToken cancellationToken)
    {
        Logger.Log(
            $"NATIVEXU_SET_REQUEST op='{operation}' cmd=0x{cmdCode:X2} " +
            $"inputLen={inputData.Length} input={GetHexPreview(inputData, inputData.Length, inputData.Length)}");
        var success = await SendAtSetCommandAsync(device, cmdCode, inputData, cancellationToken).ConfigureAwait(false);
        Logger.Log($"NATIVEXU_SET_RESULT op='{operation}' cmd=0x{cmdCode:X2} success={success}");
        return success;
    }

    /// <summary>
    /// Switches the audio input source (HDMI/Analog) using the same sequence
    /// that Elgato Studio uses: flash persistence plus I2C codec register writes.
    /// This is seamless: no USB re-enumeration, no preview interruption.
    /// DO NOT use SetInputSourceAsync (AT 0x34); it causes USB re-enumeration
    /// and can permanently corrupt firmware audio state.
    /// </summary>
    public static async Task<bool> SwitchAudioInputAsync(
        CaptureDevice? device,
        bool analog,
        byte gainByte = 0xFF,
        CancellationToken ct = default)
    {
        var sourceLabel = analog ? DeviceAudioMode.Analog : DeviceAudioMode.Hdmi;
        Logger.Log($"NATIVEXU_SWITCH_AUDIO begin source={sourceLabel} gain=0x{gainByte:X2}");

        if (device == null || string.IsNullOrWhiteSpace(device.Id))
        {
            return false;
        }

        if (!NativeXuDeviceSupport.TryGetSupported4kXIds(device, out var vendorId, out var productId))
        {
            return false;
        }

        if (!NativeXuDeviceSupport.HasSelectedInterface(device, "SWITCH_AUDIO"))
        {
            return false;
        }

        var gateAcquired = false;
        try
        {
            gateAcquired = await NativeXuDeviceSupport.TryAcquireTransportGateAsync(
                NativeXuDeviceSupport.DefaultTransportGateTimeoutMs * 4,
                ct).ConfigureAwait(false);
            if (!gateAcquired)
            {
                Logger.Log("NATIVEXU_SWITCH_AUDIO FAILED stage=gate_timeout");
                return false;
            }

            var interfaces = NativeXuDeviceSupport.EnumerateSelectedInterfaces(vendorId, productId, device);
            foreach (var ksInterface in interfaces)
            {
                ct.ThrowIfCancellationRequested();
                using var handle = KsExtensionUnitNative.TryOpen(ksInterface.Path, out _);
                if (handle is null)
                {
                    continue;
                }

                if (!KsExtensionUnitNative.TryReadTopologyNodes(handle, out var nodes, out _))
                {
                    continue;
                }

                var nodeList = nodes ?? Array.Empty<KsExtensionUnitNative.KsTopologyNode>();
                foreach (var node in nodeList)
                {
                    if (!node.IsDevSpecific)
                    {
                        continue;
                    }

                    var ok = ExecuteAudioSwitch(handle, node.NodeId, analog, gainByte, sourceLabel, ct);
                    if (ok)
                    {
                        Logger.Log($"NATIVEXU_SWITCH_AUDIO OK source={sourceLabel}");
                        return true;
                    }
                }
            }

            Logger.Log("NATIVEXU_SWITCH_AUDIO FAILED stage=no_device");
            return false;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Logger.Log($"NATIVEXU_SWITCH_AUDIO EXCEPTION type={ex.GetType().Name} msg={ex.Message}");
            return false;
        }
        finally
        {
            if (gateAcquired)
            {
                NativeXuDeviceSupport.ReleaseTransportGate();
            }
        }
    }

    /// <summary>
    /// Sets the analog input gain via I2C codec register writes plus flash persistence.
    /// The gain byte maps 0x00-0xFF (0-100%) to a 3-zone codec register configuration:
    /// Zone 1 (0x00-0x7F): digital attenuation only, regs 0x0C/0x0D = 0x80 + gain.
    /// Zone 2 (0x80-0x97): PGA gain, regs 0x0A/0x0B = gain - 0x80.
    /// Zone 3 (0x98-0xAF): PGA maxed plus output gain, regs 0x0E/0x0F = gain - 0x98.
    /// Zone 4 (0xB0-0xFF): all stages maxed (PGA=0x18, OutGain=0x18).
    /// </summary>
    public static async Task<bool> SetAnalogGainAsync(
        CaptureDevice? device,
        byte gainByte,
        bool persistFlash = true,
        CancellationToken ct = default)
    {
        var logPct = (Math.Exp(4.0 * (gainByte / 255.0)) - 1.0) / (Math.Exp(4.0) - 1.0) * 100.0;
        Logger.Log($"NATIVEXU_SET_GAIN begin gain=0x{gainByte:X2} ({logPct:0}%) flash={persistFlash}");

        if (device == null || string.IsNullOrWhiteSpace(device.Id))
        {
            return false;
        }

        if (!NativeXuDeviceSupport.TryGetSupported4kXIds(device, out var vendorId, out var productId))
        {
            return false;
        }

        if (!NativeXuDeviceSupport.HasSelectedInterface(device, "SET_GAIN"))
        {
            return false;
        }

        var gateAcquired = false;
        try
        {
            gateAcquired = await NativeXuDeviceSupport.TryAcquireTransportGateAsync(
                NativeXuDeviceSupport.DefaultTransportGateTimeoutMs * 4,
                ct).ConfigureAwait(false);
            if (!gateAcquired)
            {
                Logger.Log("NATIVEXU_SET_GAIN FAILED stage=gate_timeout");
                return false;
            }

            var interfaces = NativeXuDeviceSupport.EnumerateSelectedInterfaces(vendorId, productId, device);
            foreach (var ksInterface in interfaces)
            {
                ct.ThrowIfCancellationRequested();
                using var handle = KsExtensionUnitNative.TryOpen(ksInterface.Path, out _);
                if (handle is null)
                {
                    continue;
                }

                if (!KsExtensionUnitNative.TryReadTopologyNodes(handle, out var nodes, out _))
                {
                    continue;
                }

                var nodeList = nodes ?? Array.Empty<KsExtensionUnitNative.KsTopologyNode>();
                foreach (var node in nodeList)
                {
                    if (!node.IsDevSpecific)
                    {
                        continue;
                    }

                    var ok = ExecuteGainChange(handle, node.NodeId, gainByte, persistFlash, ct);
                    if (ok)
                    {
                        Logger.Log($"NATIVEXU_SET_GAIN OK gain=0x{gainByte:X2} flash={persistFlash}");
                        return true;
                    }
                }
            }

            Logger.Log("NATIVEXU_SET_GAIN FAILED stage=no_device");
            return false;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Logger.Log($"NATIVEXU_SET_GAIN EXCEPTION type={ex.GetType().Name} msg={ex.Message}");
            return false;
        }
        finally
        {
            if (gateAcquired)
            {
                NativeXuDeviceSupport.ReleaseTransportGate();
            }
        }
    }

    private static bool ExecuteAudioSwitch(
        SafeFileHandle handle,
        int nodeId,
        bool analog,
        byte gainByte,
        string sourceLabel,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!SendAtSetCommand(handle, nodeId, CmdGpioSetParam, new byte[] { 0x00, 0x05, 0x00 }, cancellationToken))
        {
            Logger.Log("NATIVEXU_SWITCH_AUDIO FAILED stage=gpio");
            return false;
        }

        cancellationToken.ThrowIfCancellationRequested();
        if (!SendAtSetCommand(handle, nodeId, CmdFlashGetCustomerProprietary, Array.Empty<byte>(), cancellationToken))
        {
            Logger.Log("NATIVEXU_SWITCH_AUDIO FAILED stage=flash_read");
            return false;
        }

        var flashData = new byte[32];
        flashData[0] = analog ? (byte)0x01 : (byte)0x00;
        flashData[1] = 0x80;
        flashData[2] = gainByte;
        flashData[3] = 0xAA;
        flashData[4] = 0x55;

        cancellationToken.ThrowIfCancellationRequested();
        if (!SendAtSetCommand(handle, nodeId, CmdFlashSetCustomerProprietary, flashData, cancellationToken))
        {
            Logger.Log("NATIVEXU_SWITCH_AUDIO FAILED stage=flash_write");
            return false;
        }

        Logger.Log($"NATIVEXU_SWITCH_AUDIO flash_ok source={sourceLabel}");

        byte reg0E = analog ? (byte)0x18 : (byte)0x98;
        byte reg0F = analog ? (byte)0x18 : (byte)0x98;
        byte reg10 = analog ? (byte)0x80 : (byte)0x00;
        byte reg11 = analog ? (byte)0x80 : (byte)0x00;

        var i2cCommands = new (int cmd, byte[] data)[]
        {
            (CmdI2cWrite, new byte[] { 0x00, 0x4A, 0x02, 0x00, 0x09, 0x42 }),
            (CmdI2cRead,  new byte[] { 0x00, 0x4A, 0x01, 0x00, 0x04, 0x01, 0x00 }),
            (CmdI2cWrite, new byte[] { 0x00, 0x4A, 0x02, 0x00, 0x03, 0xA0 }),
            (CmdI2cRead,  new byte[] { 0x00, 0x4A, 0x01, 0x00, 0x0E, 0x01, 0x00 }),
            (CmdI2cRead,  new byte[] { 0x00, 0x4A, 0x01, 0x00, 0x10, 0x01, 0x00 }),
            (CmdI2cRead,  new byte[] { 0x00, 0x4A, 0x01, 0x00, 0x04, 0x01, 0x00 }),
            (CmdI2cWrite, new byte[] { 0x00, 0x4A, 0x02, 0x00, 0x04, 0x0E }),
            (CmdI2cWrite, new byte[] { 0x00, 0x4A, 0x02, 0x00, 0x07, 0x00 }),
            (CmdI2cWrite, new byte[] { 0x00, 0x4A, 0x02, 0x00, 0x0E, reg0E }),
            (CmdI2cWrite, new byte[] { 0x00, 0x4A, 0x02, 0x00, 0x0F, reg0F }),
            (CmdI2cWrite, new byte[] { 0x00, 0x4A, 0x02, 0x00, 0x10, reg10 }),
            (CmdI2cWrite, new byte[] { 0x00, 0x4A, 0x02, 0x00, 0x11, reg11 }),
            (CmdI2cWrite, new byte[] { 0x00, 0x4A, 0x02, 0x00, 0x04, 0x0E }),
            (CmdI2cWrite, new byte[] { 0x00, 0x4A, 0x02, 0x00, 0x07, 0x00 }),
        };

        for (var i = 0; i < i2cCommands.Length; i++)
        {
            var (cmd, data) = i2cCommands[i];
            cancellationToken.ThrowIfCancellationRequested();
            if (!SendSelector4Command(handle, nodeId, cmd, data, cancellationToken))
            {
                Logger.Log($"NATIVEXU_SWITCH_AUDIO FAILED stage=i2c_{i} cmd=0x{cmd:X2}");
                return false;
            }
        }

        Logger.Log($"NATIVEXU_SWITCH_AUDIO i2c_ok source={sourceLabel} commands=14");
        return true;
    }

    private static bool ExecuteGainChange(
        SafeFileHandle handle,
        int nodeId,
        byte gainByte,
        bool persistFlash,
        CancellationToken cancellationToken)
    {
        ComputeGainRegisters(gainByte, out var pga, out var digAtt, out var outGain);

        Logger.Log($"NATIVEXU_SET_GAIN regs gain=0x{gainByte:X2} PGA=0x{pga:X2} DigAtt=0x{digAtt:X2} OutGain=0x{outGain:X2} flash={persistFlash}");

        var i2cWrites = new (byte reg, byte val)[]
        {
            (0x0A, pga),
            (0x0B, pga),
            (0x0E, outGain),
            (0x0F, outGain),
            (0x0C, digAtt),
            (0x0D, digAtt),
        };

        foreach (var (reg, val) in i2cWrites)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!SendSelector4Command(
                    handle,
                    nodeId,
                    CmdI2cWrite,
                    new byte[] { 0x00, 0x4A, 0x02, 0x00, reg, val },
                    cancellationToken))
            {
                Logger.Log($"NATIVEXU_SET_GAIN FAILED stage=i2c_write_r{reg:X2}");
                return false;
            }
        }

        if (!persistFlash)
        {
            return true;
        }

        cancellationToken.ThrowIfCancellationRequested();
        if (!SendAtSetCommand(handle, nodeId, CmdFlashGetCustomerProprietary, Array.Empty<byte>(), cancellationToken))
        {
            Logger.Log("NATIVEXU_SET_GAIN FAILED stage=flash_read");
            return false;
        }

        var flashData = new byte[32];
        flashData[0] = 0x01;
        flashData[1] = 0x80;
        flashData[2] = gainByte;
        flashData[3] = 0xAA;
        flashData[4] = 0x55;

        cancellationToken.ThrowIfCancellationRequested();
        if (!SendAtSetCommand(handle, nodeId, CmdFlashSetCustomerProprietary, flashData, cancellationToken))
        {
            Logger.Log("NATIVEXU_SET_GAIN FAILED stage=flash_write");
            return false;
        }

        return true;
    }

    internal static void ComputeGainRegisters(byte gainByte, out byte pga, out byte digAtt, out byte outGain)
    {
        if (gainByte < 0x80)
        {
            pga = 0x00;
            digAtt = (byte)(0x80 + gainByte);
            outGain = 0x00;
        }
        else if (gainByte < 0x98)
        {
            pga = (byte)(gainByte - 0x80);
            digAtt = 0x00;
            outGain = 0x00;
        }
        else if (gainByte < 0xB0)
        {
            pga = 0x18;
            digAtt = 0x00;
            outGain = (byte)(gainByte - 0x98);
        }
        else
        {
            pga = 0x18;
            digAtt = 0x00;
            outGain = 0x18;
        }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
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

public sealed partial class NativeXuAtCommandProvider : ISourceSignalTelemetryProvider
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
}

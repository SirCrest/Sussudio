using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;
using Microsoft.Win32.SafeHandles;
using Sussudio.Services.Capture;
using Sussudio.Services.Contracts;

namespace Sussudio.Services.Telemetry;

public sealed partial class NativeXuAtCommandProvider : ISourceSignalTelemetryProvider
{
    private static readonly Guid XuGuid = new("961073C7-49F7-44F2-AB42-E940405940C2");
    private static readonly SemaphoreSlim CallGate = new(1, 1);

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

    private const int GateTimeoutMs = 500;
    private const int AtPayloadSelector = 1;
    private const int AtTriggerSelector = 2;
    private const int AtFrameHeaderSize = 4;
    private const int AtFrameLrcSize = 1;
    private const int MaxAtResponseFrameSize = 0x200;
    private const ushort Elgato4kXVendorId = 0x0FD9;
    private const ushort Elgato4kXProductIdOriginal = 0x009B;
    private const ushort Elgato4kXProductIdRevision = 0x009C;
    private const ushort Elgato4kXProductIdAudioMode = 0x009D;

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

    internal static async Task<bool> TryAcquireTransportGateAsync(CancellationToken cancellationToken = default)
        => await CallGate.WaitAsync(GateTimeoutMs, cancellationToken).ConfigureAwait(false);

    internal static void ReleaseTransportGate() => CallGate.Release();

    private static IReadOnlyList<KsExtensionUnitNative.KsInterfacePath> EnumerateKsInterfaces(
        ushort vendorId,
        ushort productId,
        CaptureDevice? device)
    {
        var selectedInterfacePath = device?.NativeXuInterfacePath;
        if (!string.IsNullOrWhiteSpace(selectedInterfacePath))
        {
            return new[] { new KsExtensionUnitNative.KsInterfacePath(selectedInterfacePath, Guid.Empty) };
        }

        return Array.Empty<KsExtensionUnitNative.KsInterfacePath>();
    }

    private static bool HasSelectedNativeXuInterface(CaptureDevice? device, string operation)
    {
        if (!string.IsNullOrWhiteSpace(device?.NativeXuInterfacePath))
        {
            return true;
        }

        Logger.Log($"NATIVEXU_{operation}_FAILED stage=missing_selected_interface");
        return false;
    }

    internal static bool TryGetSupported4kXIds(
        CaptureDevice? device,
        out ushort vendorId,
        out ushort productId)
    {
        vendorId = 0;
        productId = 0;

        return device != null &&
               !string.IsNullOrWhiteSpace(device.Id) &&
               TryParseVendorProductIds(device.Id, out vendorId, out productId) &&
               IsSupported4kXDevice(vendorId, productId);
    }

    public async Task<SourceSignalTelemetrySnapshot> ReadAsync(
        CaptureDevice? device,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (device == null || string.IsNullOrWhiteSpace(device.Id))
        {
            return SourceSignalTelemetrySnapshot.CreateUnavailable("device-unavailable");
        }

        if (!TryParseVendorProductIds(device.Id, out var vendorId, out var productId) ||
            !IsSupported4kXDevice(vendorId, productId))
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
            gateAcquired = await CallGate.WaitAsync(GateTimeoutMs, cancellationToken).ConfigureAwait(false);
            if (!gateAcquired)
            {
                return SourceSignalTelemetrySnapshot.CreateUnavailable("nativexu-native-busy", $"{GateTimeoutMs}ms");
            }

            IReadOnlyList<KsExtensionUnitNative.KsInterfacePath> interfaces;
            try
            {
                interfaces = EnumerateKsInterfaces(vendorId, productId, device);
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

                try
                {
                    using var handle = KsExtensionUnitNative.TryOpen(ksInterface.Path, out var openErrorCode);
                    if (handle is null)
                    {
                        unavailableReason = "nativexu-open-failed";
                        unavailableDetail = DescribeWin32Detail(ksInterface.Path, openErrorCode);
                        Logger.Log($"NATIVEXU_OPEN_FAILED path='{ksInterface.Path}' detail='{unavailableDetail}'");
                        continue;
                    }

                    if (!KsExtensionUnitNative.TryReadTopologyNodes(handle, out var nodes, out var topologyError))
                    {
                        unavailableReason = "nativexu-topology-read-failed";
                        unavailableDetail = $"{ksInterface.Path}: {topologyError ?? "unknown"}";
                        Logger.Log($"NATIVEXU_TOPOLOGY_FAILED path='{ksInterface.Path}' error='{topologyError ?? "unknown"}'");
                        continue;
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

                    foreach (var node in candidateNodes)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        var attempt = TryReadRolling(handle, node.NodeId, ksInterface.Path, cancellationToken);
                        if (attempt.Snapshot != null)
                        {
                            return attempt.Snapshot;
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
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    unavailableReason = "nativexu-interface-exception";
                    unavailableDetail = $"{ksInterface.Path}: {ex.GetType().Name}: {ex.Message}";
                    Logger.Log($"NATIVEXU_INTERFACE_EXCEPTION path='{ksInterface.Path}' type={ex.GetType().Name} message={ex.Message}");
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
                CallGate.Release();
            }
        }
    }

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

        AtCommandResult Send(string name, int commandCode)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return SendAtCommand(handle, nodeId, name, commandCode);
        }

        var cable = Send("CableConnect", CmdCableConnect);
        if (!cable.Success)
            return HandleFailedCommand("nativexu-read-failed", interfacePath, cable);

        if (TryReadInt32(cable.Response, out var cableState) && cableState == 0)
        {
            Logger.Log($"NATIVEXU_SIGNAL_UNAVAILABLE path='{interfacePath}' node={nodeId} reason=no-cable");
            _hasCompletedFullCycle = false; // force full re-read when cable reconnects
            return CreateUnavailableNodeResult(interfacePath, "nativexu-no-cable");
        }

        var videoStable = Send("VideoStable", CmdVideoStable);
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
            _cVic = Send("VIC", CmdVic);
            _cVfreq = Send("Vfreq", CmdVfreq);
            _cAviInfo = Send("AviInfoFrame", CmdAviInfoFrame);
            _cHdrMetadata = Send("HdrMetadata", CmdHdrMetadata);
            _cSystemInfo = Send("SystemInfo", CmdSystemInfo);
            _cHdr2Sdr = Send("Hdr2Sdr", CmdHdr2Sdr);
            _cAudioFormat = Send("AudioFormat", CmdAudioFormat);
            _cAudioSamplingRate = Send("AudioSamplingRate", CmdAudioSamplingRate);
            _cInputSource = Send("InputSource", CmdInputSource);
            _cFlashAudio = Send("FlashAudioInput", CmdFlashGetCustomerProprietary);
            _cAdcOnOff = Send("AdcOnOff", CmdAdcOnOff);
            _cAdcVolumeGain = Send("AdcVolumeGain", CmdAdcVolumeGain);
            _cUacVolumeGain = Send("UacVolumeGain", CmdUacVolumeGain);
            _cUacOut1Mute = Send("UacOut1Mute", CmdUacOut1Mute);
            _cUacOut2Mute = Send("UacOut2Mute", CmdUacOut2Mute);
            _cUacOut2MixerSource = Send("UacOut2MixerSource", CmdUacOut2MixerSource);
            _cUsbHostProtocol = Send("UsbHostProtocol", CmdUsbHostProtocol);
            _cUsbCdc = Send("UsbCdc", CmdUsbCdcOnOff);
            _cUsbLinkState = Send("UsbLinkState", CmdUsbLinkState);
            _cUsbForceSpeed = Send("UsbForceSpeed", CmdUsbForceSpeed);
            _cTxHpd = Send("TxHpd", CmdTxHpdStatus);
            _cTxVrr = Send("TxVrr", CmdTxVrr);
            _cTxEdidValid = Send("TxEdidValid", CmdTxEdidValid);
            _cUvcOutputTiming = Send("UvcOutputTiming", CmdUvcOutputTiming);
            _cUvcVideoFormat = Send("UvcVideoFormat", CmdUvcVideoFormat);
            _cUvcErrStatus = Send("UvcErrStatus", CmdUvcErrStatus);
            _cHdcpMode = Send("HdcpMode", CmdHdcpMode);
            _cHdcpVersion = Send("HdcpVersion", CmdHdcpVersion);
            _cRxTxHdcpVersion = Send("RxTxHdcpVersion", CmdRxTxHdcpVersion);
            _cHdr2SdrExtended = Send("Hdr2SdrExtended", CmdHdr2SdrExtended);
            _cCustomerVersion = Send("CustomerVersion", CmdCustomerVersion);
            _cRescueVersion = Send("RescueVersion", CmdRescueVersion);
            _cHdr2SdrColorParam = Send("Hdr2SdrColorParam", CmdHdr2SdrColorParam);
            _cColorRangeSetting = Send("ColorRangeSetting", CmdColorRangeSetting);
            _cVtem = Send("Vtem", CmdVtem);
            _cBitError = Send("BitError", CmdBitError);
            _cRawTiming = Send("RawTiming", CmdRawTiming);
            _hasCompletedFullCycle = true;
            _rollingGroup = 0;
        }
        else
        {
            // ── Subsequent calls: fire only current group ──
            switch (_rollingGroup)
            {
                case 0: // Signal (most important — cycles every pass)
                    _cVic = Send("VIC", CmdVic);
                    _cVfreq = Send("Vfreq", CmdVfreq);
                    _cAviInfo = Send("AviInfoFrame", CmdAviInfoFrame);
                    _cHdrMetadata = Send("HdrMetadata", CmdHdrMetadata);
                    break;
                case 1: // Audio
                    _cAudioFormat = Send("AudioFormat", CmdAudioFormat);
                    _cAudioSamplingRate = Send("AudioSamplingRate", CmdAudioSamplingRate);
                    _cInputSource = Send("InputSource", CmdInputSource);
                    _cFlashAudio = Send("FlashAudioInput", CmdFlashGetCustomerProprietary);
                    break;
                case 2: // Audio routing
                    _cAdcOnOff = Send("AdcOnOff", CmdAdcOnOff);
                    _cAdcVolumeGain = Send("AdcVolumeGain", CmdAdcVolumeGain);
                    _cUacVolumeGain = Send("UacVolumeGain", CmdUacVolumeGain);
                    _cUacOut1Mute = Send("UacOut1Mute", CmdUacOut1Mute);
                    _cUacOut2Mute = Send("UacOut2Mute", CmdUacOut2Mute);
                    _cUacOut2MixerSource = Send("UacOut2MixerSource", CmdUacOut2MixerSource);
                    break;
                case 3: // HDR/color
                    _cSystemInfo = Send("SystemInfo", CmdSystemInfo);
                    _cHdr2Sdr = Send("Hdr2Sdr", CmdHdr2Sdr);
                    _cHdr2SdrExtended = Send("Hdr2SdrExtended", CmdHdr2SdrExtended);
                    _cHdr2SdrColorParam = Send("Hdr2SdrColorParam", CmdHdr2SdrColorParam);
                    _cColorRangeSetting = Send("ColorRangeSetting", CmdColorRangeSetting);
                    break;
                case 4: // USB/HDMI status
                    _cUsbHostProtocol = Send("UsbHostProtocol", CmdUsbHostProtocol);
                    _cUsbCdc = Send("UsbCdc", CmdUsbCdcOnOff);
                    _cUsbLinkState = Send("UsbLinkState", CmdUsbLinkState);
                    _cUsbForceSpeed = Send("UsbForceSpeed", CmdUsbForceSpeed);
                    _cTxHpd = Send("TxHpd", CmdTxHpdStatus);
                    _cTxVrr = Send("TxVrr", CmdTxVrr);
                    _cTxEdidValid = Send("TxEdidValid", CmdTxEdidValid);
                    break;
                case 5: // Diagnostics (least critical)
                    _cUvcOutputTiming = Send("UvcOutputTiming", CmdUvcOutputTiming);
                    _cUvcVideoFormat = Send("UvcVideoFormat", CmdUvcVideoFormat);
                    _cUvcErrStatus = Send("UvcErrStatus", CmdUvcErrStatus);
                    _cHdcpMode = Send("HdcpMode", CmdHdcpMode);
                    _cHdcpVersion = Send("HdcpVersion", CmdHdcpVersion);
                    _cRxTxHdcpVersion = Send("RxTxHdcpVersion", CmdRxTxHdcpVersion);
                    _cCustomerVersion = Send("CustomerVersion", CmdCustomerVersion);
                    _cRescueVersion = Send("RescueVersion", CmdRescueVersion);
                    _cVtem = Send("Vtem", CmdVtem);
                    _cBitError = Send("BitError", CmdBitError);
                    _cRawTiming = Send("RawTiming", CmdRawTiming);
                    break;
            }
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

    /// <summary>
    /// Original monolithic read — fires all 35 commands. Kept for reference
    /// and for callers that need a guaranteed-complete snapshot (e.g. on-demand
    /// diagnostics). The rolling poll path above is used for periodic polling.
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

    private readonly record struct VicTiming(int Width, int Height, double NominalFrameRate, bool IsInterlaced);

    private readonly record struct HdrMetadataInfo(bool HasMetadata, byte? Eotf, bool? IsHdr);

    private readonly record struct AviInfoFrameInfo(
        bool HasData,
        string? ColorSpace,
        string? Colorimetry,
        string? Quantization)
    {
        public static AviInfoFrameInfo Empty => new(false, null, null, null);
    }
}

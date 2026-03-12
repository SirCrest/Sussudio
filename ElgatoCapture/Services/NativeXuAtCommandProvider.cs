using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ElgatoCapture.Models;
using Microsoft.Win32.SafeHandles;

namespace ElgatoCapture.Services;

public sealed class NativeXuAtCommandProvider : ISourceSignalTelemetryProvider
{
    private static readonly Guid XuGuid = new("961073C7-49F7-44F2-AB42-E940405940C2");
    private static readonly SemaphoreSlim CallGate = new(1, 1);
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
    private const int MaxAtResponseFrameSize = 128;
    private const ushort Elgato4kXVendorId = 0x0FD9;
    private const ushort Elgato4kXProductIdOriginal = 0x009B;
    private const ushort Elgato4kXProductIdRevision = 0x009C;

    private const int CmdCableConnect = 0x36;
    private const int CmdVideoStable = 0x38;
    private const int CmdVic = 0x3C;
    private const int CmdSystemInfo = 0x23;
    private const int CmdHdrMetadata = 0x65;
    private const int CmdVfreq = 0x86;
    private const int CmdHdr2Sdr = 0x90;
    private const int CmdAviInfoFrame = 0x92;
    private const int CmdAudioFormat = 0x04;
    private const int CmdAudioSamplingRate = 0x06;
    private const int CmdInputSource = 0x35;
    private const int CmdRawTiming = 0x37;
    private const int CmdUsbHostProtocol = 0x40;
    private const int CmdTxHpdStatus = 0x41;
    private const int CmdTxVrr = 0x42;
    private const int CmdUvcOutputTiming = 0x44;
    private const int CmdUvcVideoFormat = 0x45;
    private const int CmdUvcErrStatus = 0x46;
    private const int CmdHdcpMode = 0x72;
    private const int CmdHdr2SdrExtended = 0x76;
    private const int CmdVtem = 0x7D;
    private const int CmdHdcpVersion = 0x7E;
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

        if (!TryParseVendorProductIds(device.Id, out var vendorId, out var productId) ||
            !IsSupported4kXDevice(vendorId, productId))
        {
            return SourceSignalTelemetrySnapshot.CreateUnavailable("nativexu-device-unsupported");
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
                interfaces = KsExtensionUnitNative.EnumerateKsInterfaces(vendorId, productId);
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

                        var attempt = TryReadSnapshot(handle, node.NodeId, ksInterface.Path);
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

        if (!TryParseVendorProductIds(device.Id, out var vendorId, out var productId) ||
            !IsSupported4kXDevice(vendorId, productId))
        {
            return false;
        }

        var gateAcquired = false;
        try
        {
            gateAcquired = await CallGate.WaitAsync(GateTimeoutMs, cancellationToken).ConfigureAwait(false);
            if (!gateAcquired)
            {
                return false;
            }

            var interfaces = KsExtensionUnitNative.EnumerateKsInterfaces(vendorId, productId);
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

                    var result = SendAtSetCommand(handle, node.NodeId, cmdCode, inputData);
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
                CallGate.Release();
            }
        }
    }

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
        var usbHostProtocolResult = SendAtCommand(handle, nodeId, "UsbHostProtocol", CmdUsbHostProtocol);
        var usbCdcResult = SendAtCommand(handle, nodeId, "UsbCdc", CmdUsbCdcOnOff);
        var usbLinkStateResult = SendAtCommand(handle, nodeId, "UsbLinkState", CmdUsbLinkState);
        var usbForceSpeedResult = SendAtCommand(handle, nodeId, "UsbForceSpeed", CmdUsbForceSpeed);
        var txHpdResult = SendAtCommand(handle, nodeId, "TxHpd", CmdTxHpdStatus);
        var txVrrResult = SendAtCommand(handle, nodeId, "TxVrr", CmdTxVrr);
        var uvcOutputTimingResult = SendAtCommand(handle, nodeId, "UvcOutputTiming", CmdUvcOutputTiming);
        var uvcVideoFormatResult = SendAtCommand(handle, nodeId, "UvcVideoFormat", CmdUvcVideoFormat);
        var uvcErrStatusResult = SendAtCommand(handle, nodeId, "UvcErrStatus", CmdUvcErrStatus);
        var hdcpModeResult = SendAtCommand(handle, nodeId, "HdcpMode", CmdHdcpMode);
        var hdcpVersionResult = SendAtCommand(handle, nodeId, "HdcpVersion", CmdHdcpVersion);
        var rxTxHdcpVersionResult = SendAtCommand(handle, nodeId, "RxTxHdcpVersion", CmdRxTxHdcpVersion);
        var hdr2SdrExtendedResult = SendAtCommand(handle, nodeId, "Hdr2SdrExtended", CmdHdr2SdrExtended);
        var hdr2SdrColorParamResult = SendAtCommand(handle, nodeId, "Hdr2SdrColorParam", CmdHdr2SdrColorParam);
        var colorRangeSettingResult = SendAtCommand(handle, nodeId, "ColorRangeSetting", CmdColorRangeSetting);
        var vtemResult = SendAtCommand(handle, nodeId, "Vtem", CmdVtem);
        var bitErrorResult = SendAtCommand(handle, nodeId, "BitError", CmdBitError);
        var rawTimingResult = SendAtCommand(handle, nodeId, "RawTiming", CmdRawTiming);

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
                DiagnosticSummary = fullDiagnosticSummary,
                AudioInputAvailability = inputSourceResult.Success
                    ? SourceAudioInputAvailability.Available
                    : SourceAudioInputAvailability.Unavailable,
                AudioInputMode = inputSourceResult.Success && inputSourceResult.Response.Length >= 1
                    ? (inputSourceResult.Response[0] == 0 ? SourceAudioInputMode.Hdmi : SourceAudioInputMode.Analog)
                    : null,
                AudioInputOrigin = inputSourceResult.Success
                    ? (inputSourceResult.Response.Length >= 1
                        ? $"NativeXu:InputSource={inputSourceResult.Response[0]}"
                        : "NativeXu:InputSource=missing")
                    : "not-implemented"
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

    private static bool SendAtSetCommand(SafeFileHandle handle, int nodeId, int cmdCode, byte[] inputData)
    {
        var requestFrame = BuildAtWriteFrame(cmdCode, inputData);
        var triggerData = new byte[]
        {
            (byte)(requestFrame.Length & 0xFF),
            (byte)((requestFrame.Length >> 8) & 0xFF)
        };

        if (!KsExtensionUnitNative.TryXuSetViaOutput(handle, nodeId, XuGuid, AtTriggerSelector, triggerData, out var triggerWin32))
        {
            Logger.Log($"NATIVEXU_SET_FAILED cmd=0x{cmdCode:X2} stage=trigger win32={FormatWin32Code(triggerWin32)}");
            return false;
        }

        if (!KsExtensionUnitNative.TryXuSetViaOutput(handle, nodeId, XuGuid, AtPayloadSelector, requestFrame, out var sendWin32))
        {
            Logger.Log($"NATIVEXU_SET_FAILED cmd=0x{cmdCode:X2} stage=send win32={FormatWin32Code(sendWin32)}");
            return false;
        }

        KsExtensionUnitNative.TryXuGetDirect(handle, nodeId, XuGuid, AtTriggerSelector, 2, out _, out _, out _);
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
        if (buffer.Length < 4 || !HasNonZeroData(buffer) || buffer[0] != 0x87)
        {
            return new HdrMetadataInfo(false, null, null);
        }

        var eotf = buffer[3];
        var isHdr = eotf switch
        {
            2 or 3 => true,
            0 or 1 => false,
            _ => (bool?)null
        };
        return new HdrMetadataInfo(true, eotf, isHdr);
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

    private static bool TryParseVendorProductIds(string deviceId, out ushort vendorId, out ushort productId)
    {
        vendorId = 0;
        productId = 0;
        return TryParseHexToken(deviceId, "vid_", out vendorId) &&
               TryParseHexToken(deviceId, "pid_", out productId);
    }

    private static bool TryParseHexToken(string value, string token, out ushort result)
    {
        result = 0;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var tokenIndex = value.IndexOf(token, StringComparison.OrdinalIgnoreCase);
        if (tokenIndex < 0 || tokenIndex + token.Length + 4 > value.Length)
        {
            return false;
        }

        return ushort.TryParse(
            value.AsSpan(tokenIndex + token.Length, 4),
            NumberStyles.HexNumber,
            CultureInfo.InvariantCulture,
            out result);
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

    private static bool IsSupported4kXDevice(ushort vendorId, ushort productId)
        => vendorId == Elgato4kXVendorId &&
           (productId == Elgato4kXProductIdOriginal || productId == Elgato4kXProductIdRevision);

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

    private static string FormatWin32Code(int? win32Code)
        => win32Code.HasValue ? win32Code.Value.ToString(CultureInfo.InvariantCulture) : "unknown";

    private static string BoolToToken(bool? value)
        => value switch
        {
            true => "true",
            false => "false",
            _ => "unknown"
        };

    private static string FormatFrameRate(double? value)
        => value.HasValue && value.Value > 0
            ? value.Value.ToString("0.###", CultureInfo.InvariantCulture)
            : "unknown";

    private static bool HasNonZeroData(byte[] buffer)
        => buffer.AsSpan().IndexOfAnyExcept((byte)0) >= 0;

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

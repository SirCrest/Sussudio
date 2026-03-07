using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using ElgatoCapture.Models;
using Microsoft.Win32.SafeHandles;

namespace ElgatoCapture.Services;

public sealed class KsXuSourceSignalTelemetryProvider : ISourceSignalTelemetryProvider
{
    private static readonly Guid Elgato4kXXuGuid = new("961073C7-49F7-44F2-AB42-E940405940C2");
    private static readonly SemaphoreSlim KsXuCallGate = new(1, 1);

    private const int GateTimeoutMs = 500;
    private const int HdrDetectionSelector = 3;
    private const int HdrFingerprintPrefixLength = 24;
    private const int MaxXuBufferSize = 1024;
    private const ushort Elgato4kXVendorId = 0x0FD9;
    private const ushort Elgato4kXProductId = 0x009B;

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
            vendorId != Elgato4kXVendorId ||
            productId != Elgato4kXProductId)
        {
            return SourceSignalTelemetrySnapshot.CreateUnavailable("ksxu-device-unsupported");
        }

        var gateAcquired = false;
        SourceSignalTelemetrySnapshot? inconclusiveSnapshot = null;
        string? unavailableReason = null;
        string? unavailableDetail = null;

        try
        {
            gateAcquired = await KsXuCallGate.WaitAsync(GateTimeoutMs, cancellationToken).ConfigureAwait(false);
            if (!gateAcquired)
            {
                return SourceSignalTelemetrySnapshot.CreateUnavailable("ksxu-native-busy", $"{GateTimeoutMs}ms");
            }

            cancellationToken.ThrowIfCancellationRequested();

            IReadOnlyList<KsInterfacePath> interfaces;
            try
            {
                interfaces = KsNative.EnumerateKsInterfaces(vendorId, productId);
            }
            catch (Exception ex)
            {
                Logger.Log($"KSXU_ENUMERATE_FAILED type={ex.GetType().Name} message={ex.Message}");
                return SourceSignalTelemetrySnapshot.CreateUnavailable("ksxu-enumerate-failed", ex.Message);
            }

            if (interfaces.Count == 0)
            {
                return SourceSignalTelemetrySnapshot.CreateUnavailable("ksxu-interface-not-found");
            }

            foreach (var ksInterface in interfaces)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    using var handle = KsNative.TryOpen(ksInterface.Path, out var openErrorCode);
                    if (handle is null)
                    {
                        unavailableReason = "ksxu-open-failed";
                        unavailableDetail = DescribeWin32Detail(ksInterface.Path, openErrorCode);
                        Logger.Log($"KSXU_OPEN_FAILED path='{ksInterface.Path}' detail='{unavailableDetail}'");
                        continue;
                    }

                    cancellationToken.ThrowIfCancellationRequested();

                    if (!KsNative.TryReadTopologyNodes(handle, out var nodes, out var topologyError))
                    {
                        unavailableReason = "ksxu-topology-read-failed";
                        unavailableDetail = $"{ksInterface.Path}: {topologyError ?? "unknown"}";
                        Logger.Log($"KSXU_TOPOLOGY_FAILED path='{ksInterface.Path}' error='{topologyError ?? "unknown"}'");
                        continue;
                    }

                    var nodeList = nodes ?? Array.Empty<KsTopologyNode>();
                    var devSpecificIds = new List<int>();
                    foreach (var n in nodeList)
                    {
                        if (n.IsDevSpecific) devSpecificIds.Add(n.NodeId);
                    }
                    Logger.Log(
                        $"KSXU_TOPOLOGY path='{ksInterface.Path}' nodeCount={nodeList.Count} " +
                        $"devSpecificNodes=[{string.Join(",", devSpecificIds)}]");

                    // Try dev-specific nodes first, then fall back to all nodes
                    var candidateNodes = devSpecificIds.Count > 0
                        ? nodeList.Where(n => n.IsDevSpecific)
                        : nodeList.AsEnumerable();

                    foreach (var node in candidateNodes)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        var readSucceeded = KsNative.TryReadPropertyValue(
                            handle,
                            node.NodeId,
                            Elgato4kXXuGuid,
                            HdrDetectionSelector,
                            MaxXuBufferSize,
                            out var bytesReturned,
                            out var valueHexPreview,
                            out var readWin32Code,
                            out var readError);

                        Logger.Log(
                            $"KSXU_READ_RESULT path='{ksInterface.Path}' node={node.NodeId} " +
                            $"selector={HdrDetectionSelector} succeeded={readSucceeded} " +
                            $"bytes={bytesReturned} win32={readWin32Code} " +
                            $"hex={valueHexPreview ?? "null"}");

                        if (readSucceeded)
                        {
                            // Read all selectors for diagnostic comparison
                            foreach (var sel in new[] { 1, 2, 4 })
                            {
                                KsNative.TryReadPropertyValue(
                                    handle, node.NodeId, Elgato4kXXuGuid, sel, MaxXuBufferSize,
                                    out var diagBytes, out var diagHex, out _, out _);
                                Logger.Log($"KSXU_DIAG sel={sel} bytes={diagBytes} hex={diagHex ?? "null"}");
                            }

                            return CreateSnapshot(ksInterface.Path, bytesReturned, valueHexPreview);
                        }

                        // Non-matching nodes return ErrorNotFound/ErrorSetNotFound — skip silently
                        if (readWin32Code is 1168 or 1170 or 87 or 1)
                        {
                            continue;
                        }

                        var readDetail = string.IsNullOrWhiteSpace(readError)
                            ? DescribeWin32Detail(ksInterface.Path, readWin32Code)
                            : $"{ksInterface.Path}: {readError}";

                        if (IsTransientAccessFailure(readWin32Code))
                        {
                            unavailableReason = "ksxu-read-failed";
                            unavailableDetail = readDetail;
                            continue;
                        }

                        inconclusiveSnapshot ??= CreateSnapshot(ksInterface.Path, bytesReturned, valueHexPreview);
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    unavailableReason = "ksxu-interface-exception";
                    unavailableDetail = $"{ksInterface.Path}: {ex.GetType().Name}: {ex.Message}";
                    Logger.Log(
                        $"KSXU_INTERFACE_EXCEPTION path='{ksInterface.Path}' type={ex.GetType().Name} message={ex.Message}");
                }
            }

            return inconclusiveSnapshot ??
                   SourceSignalTelemetrySnapshot.CreateUnavailable(
                       unavailableReason ?? "ksxu-read-failed",
                       unavailableDetail);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Logger.Log($"KSXU_PROVIDER_EXCEPTION type={ex.GetType().Name} message={ex.Message}");
            return SourceSignalTelemetrySnapshot.CreateUnavailable("ksxu-exception", $"{ex.GetType().Name}: {ex.Message}");
        }
        finally
        {
            if (gateAcquired)
            {
                KsXuCallGate.Release();
            }
        }
    }

    private static SourceSignalTelemetrySnapshot CreateSnapshot(string interfacePath, int bytesReturned, string? valueHexPreview)
    {
        var prefixHex = GetPrefixHex(valueHexPreview, bytesReturned);
        var isHdr = ResolveHdrState(valueHexPreview, bytesReturned);
        var hdrLabel = isHdr switch
        {
            true => "on",
            false => "off",
            _ => "unknown"
        };

        return new SourceSignalTelemetrySnapshot
        {
            TimestampUtc = DateTimeOffset.UtcNow,
            Availability = SourceTelemetryAvailability.Available,
            Origin = SourceTelemetryOrigin.KsXu,
            OriginDetail = $"KsXu:{interfacePath}",
            Confidence = isHdr.HasValue ? SourceTelemetryConfidence.Medium : SourceTelemetryConfidence.Low,
            IsHdr = isHdr,
            DiagnosticSummary = $"ksxu:hdr={hdrLabel}:s3prefix={prefixHex}"
        };
    }

    private static bool? ResolveHdrState(string? valueHexPreview, int bytesReturned)
    {
        if (bytesReturned < HdrFingerprintPrefixLength)
        {
            return null;
        }

        var expectedPrefixLength = HdrFingerprintPrefixLength * 2;
        if (string.IsNullOrWhiteSpace(valueHexPreview) || valueHexPreview.Length < expectedPrefixLength)
        {
            return null;
        }

        for (var index = 0; index < expectedPrefixLength; index++)
        {
            if (valueHexPreview[index] != '0')
            {
                return false;
            }
        }

        return true;
    }

    private static string GetPrefixHex(string? valueHexPreview, int bytesReturned)
    {
        if (bytesReturned <= 0 || string.IsNullOrWhiteSpace(valueHexPreview))
        {
            return "none";
        }

        var prefixLength = Math.Min(bytesReturned, HdrFingerprintPrefixLength) * 2;
        prefixLength = Math.Min(prefixLength, valueHexPreview.Length);
        return prefixLength > 0 ? valueHexPreview[..prefixLength] : "none";
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

    private static bool IsTransientAccessFailure(int? win32Code)
        => win32Code is 5 or 32 or 170;

    private static string DescribeWin32Detail(string path, int? win32Code)
    {
        if (!win32Code.HasValue)
        {
            return $"{path}: unknown";
        }

        return $"{path}: win32={win32Code.Value} ({new Win32Exception(win32Code.Value).Message})";
    }

    private readonly record struct KsInterfacePath(string Path, Guid CategoryGuid);

    private readonly record struct KsTopologyNode(int NodeId, bool IsDevSpecific, Guid NodeType);

    private static class KsNative
    {
        private static readonly Guid KsCategoryCapture = new("65E8773D-8F56-11D0-A3B9-00A0C9223196");
        private static readonly Guid KsCategoryVideo = new("6994AD05-93EF-11D0-A3CC-00A0C9223196");
        private static readonly Guid KsPropSetTopology = new("720D4AC0-7533-11D0-A5D6-28DB04C10000");
        private static readonly Guid KsNodeTypeDevSpecific = new("941C7AC0-C559-11D0-8A2B-00A0C9255AC1");

        private const int MaxTopologyBuffer = 64 * 1024;
        private const uint IoctlKsProperty = 0x002F0003;
        private const uint DigcfPresent = 0x00000002;
        private const uint DigcfDeviceInterface = 0x00000010;
        private const uint GenericRead = 0x80000000;
        private const uint GenericWrite = 0x40000000;
        private const uint FileShareRead = 0x00000001;
        private const uint FileShareWrite = 0x00000002;
        private const uint OpenExisting = 3;
        private const uint KsPropertyTypeGet = 0x00000001;
        private const uint KsPropertyTypeBasicSupport = 0x00000200;
        private const uint KsPropertyTypeTopology = 0x10000000;
        private const uint KsPropertyTopologyNodes = 1;
        private const int ErrorInsufficientBuffer = 122;
        private const int ErrorMoreData = 234;
        private const int ErrorNotFound = 1168;
        private const int ErrorSetNotFound = 1170;
        private const int ErrorInvalidParameter = 87;
        private const int ErrorInvalidFunction = 1;

        internal static IReadOnlyList<KsInterfacePath> EnumerateKsInterfaces(ushort vendorId, ushort productId)
        {
            var token = $"vid_{vendorId:x4}&pid_{productId:x4}";
            var result = new List<KsInterfacePath>();
            var dedupe = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var categories = new[] { KsCategoryCapture, KsCategoryVideo };

            foreach (var category in categories)
            {
                var categoryGuid = category;
                var deviceInfoSet = SetupDiGetClassDevs(ref categoryGuid, null, IntPtr.Zero, DigcfPresent | DigcfDeviceInterface);
                if (deviceInfoSet == IntPtr.Zero || deviceInfoSet == new IntPtr(-1))
                {
                    continue;
                }

                try
                {
                    var interfaceData = new SP_DEVICE_INTERFACE_DATA
                    {
                        cbSize = Marshal.SizeOf<SP_DEVICE_INTERFACE_DATA>()
                    };

                    for (uint index = 0; ; index++)
                    {
                        interfaceData.cbSize = Marshal.SizeOf<SP_DEVICE_INTERFACE_DATA>();
                        if (!SetupDiEnumDeviceInterfaces(deviceInfoSet, IntPtr.Zero, ref categoryGuid, index, ref interfaceData))
                        {
                            var error = Marshal.GetLastWin32Error();
                            if (error == 259)
                            {
                                break;
                            }

                            continue;
                        }

                        var detail = new SP_DEVICE_INTERFACE_DETAIL_DATA
                        {
                            cbSize = IntPtr.Size == 8 ? 8 : 6,
                            DevicePath = string.Empty
                        };

                        if (!SetupDiGetDeviceInterfaceDetail(
                                deviceInfoSet,
                                ref interfaceData,
                                ref detail,
                                (uint)Marshal.SizeOf<SP_DEVICE_INTERFACE_DETAIL_DATA>(),
                                out _,
                                IntPtr.Zero))
                        {
                            continue;
                        }

                        if (detail.DevicePath.IndexOf(token, StringComparison.OrdinalIgnoreCase) < 0)
                        {
                            continue;
                        }

                        if (!dedupe.Add(detail.DevicePath))
                        {
                            continue;
                        }

                        result.Add(new KsInterfacePath(detail.DevicePath, categoryGuid));
                    }
                }
                finally
                {
                    SetupDiDestroyDeviceInfoList(deviceInfoSet);
                }
            }

            return result;
        }

        internal static SafeFileHandle? TryOpen(string path)
            => TryOpen(path, out _);

        internal static SafeFileHandle? TryOpen(string path, out int? errorCode)
        {
            errorCode = null;
            var readWrite = CreateFile(
                path,
                GenericRead | GenericWrite,
                FileShareRead | FileShareWrite,
                IntPtr.Zero,
                OpenExisting,
                0,
                IntPtr.Zero);

            if (!readWrite.IsInvalid)
            {
                return readWrite;
            }

            var readWriteError = Marshal.GetLastWin32Error();
            readWrite.Dispose();

            var readOnly = CreateFile(
                path,
                GenericRead,
                FileShareRead | FileShareWrite,
                IntPtr.Zero,
                OpenExisting,
                0,
                IntPtr.Zero);

            if (readOnly.IsInvalid)
            {
                var readOnlyError = Marshal.GetLastWin32Error();
                readOnly.Dispose();
                errorCode = readOnlyError != 0 ? readOnlyError : readWriteError;
                return null;
            }

            return readOnly;
        }

        internal static bool TryReadTopologyNodes(
            SafeFileHandle handle,
            out IReadOnlyList<KsTopologyNode>? nodes,
            out string? error)
        {
            nodes = null;
            error = null;

            var property = new KSPROPERTY
            {
                Set = KsPropSetTopology,
                Id = KsPropertyTopologyNodes,
                Flags = KsPropertyTypeGet
            };

            var input = StructureToBytes(property);
            var bufferSize = 4096;
            byte[]? output = null;
            int bytesReturned = 0;

            while (bufferSize <= MaxTopologyBuffer)
            {
                output = new byte[bufferSize];
                if (DeviceIoControl(
                        handle,
                        IoctlKsProperty,
                        input,
                        input.Length,
                        output,
                        output.Length,
                        out bytesReturned,
                        IntPtr.Zero))
                {
                    break;
                }

                var errorCode = Marshal.GetLastWin32Error();
                if (errorCode is ErrorInsufficientBuffer or ErrorMoreData)
                {
                    bufferSize *= 2;
                    continue;
                }

                error = $"topology-query-failed win32={errorCode} ({new Win32Exception(errorCode).Message})";
                return false;
            }

            if (output is null)
            {
                error = "topology-query-failed output-null";
                return false;
            }

            // KSPROPERTY_TOPOLOGY_NODES returns KSMULTIPLE_ITEM header (8 bytes: uint Size, uint Count)
            // followed by Count GUIDs. We must skip the header before parsing node type GUIDs.
            const int headerSize = 8; // sizeof(KSMULTIPLE_ITEM)
            if (bytesReturned < headerSize)
            {
                nodes = Array.Empty<KsTopologyNode>();
                return true;
            }

            var count = (int)BitConverter.ToUInt32(output, 4);
            if (count <= 0 || headerSize + count * 16 > bytesReturned)
            {
                nodes = Array.Empty<KsTopologyNode>();
                return true;
            }

            var parsed = new List<KsTopologyNode>(count);
            for (var i = 0; i < count; i++)
            {
                var offset = headerSize + i * 16;
                var nodeType = new Guid(output.AsSpan(offset, 16));
                parsed.Add(new KsTopologyNode(i, nodeType == KsNodeTypeDevSpecific, nodeType));
            }

            nodes = parsed;
            return true;
        }

        internal static bool TryReadPropertyValue(
            SafeFileHandle handle,
            int nodeId,
            Guid propertySet,
            int propertyId,
            int maxBufferSize,
            out int bytesReturned,
            out string? valueHexPreview,
            out string? error)
            => TryReadPropertyValue(
                handle,
                nodeId,
                propertySet,
                propertyId,
                maxBufferSize,
                out bytesReturned,
                out valueHexPreview,
                out _,
                out error);

        internal static bool TryReadPropertyValue(
            SafeFileHandle handle,
            int nodeId,
            Guid propertySet,
            int propertyId,
            int maxBufferSize,
            out int bytesReturned,
            out string? valueHexPreview,
            out int? win32Code,
            out string? error)
        {
            bytesReturned = 0;
            valueHexPreview = null;
            win32Code = null;
            error = null;

            var request = new KSP_NODE
            {
                Property = new KSPROPERTY
                {
                    Set = propertySet,
                    Id = (uint)propertyId,
                    Flags = KsPropertyTypeGet | KsPropertyTypeTopology
                },
                NodeId = (uint)nodeId,
                Reserved = 0
            };

            var input = StructureToBytes(request);
            var bufferSize = 1;

            while (bufferSize <= maxBufferSize)
            {
                var output = new byte[bufferSize];
                if (DeviceIoControl(
                        handle,
                        IoctlKsProperty,
                        input,
                        input.Length,
                        output,
                        output.Length,
                        out bytesReturned,
                        IntPtr.Zero))
                {
                    var previewLength = Math.Min(Math.Max(bytesReturned, 0), output.Length);
                    valueHexPreview = previewLength > 0
                        ? Convert.ToHexString(output.AsSpan(0, previewLength))
                        : string.Empty;
                    return true;
                }

                var win32 = Marshal.GetLastWin32Error();
                if (win32 is ErrorInsufficientBuffer or ErrorMoreData)
                {
                    bufferSize *= 2;
                    continue;
                }

                win32Code = win32;
                if (win32 is ErrorNotFound or ErrorSetNotFound or ErrorInvalidParameter or ErrorInvalidFunction)
                {
                    return false;
                }

                error = $"get-failed win32={win32} ({new Win32Exception(win32).Message})";
                return false;
            }

            win32Code = ErrorMoreData;
            error = $"get-failed exceeded-max-buffer max={maxBufferSize}";
            return false;
        }

        private static byte[] StructureToBytes<T>(T value)
            where T : struct
        {
            var size = Marshal.SizeOf<T>();
            var bytes = new byte[size];
            var ptr = Marshal.AllocHGlobal(size);
            try
            {
                Marshal.StructureToPtr(value, ptr, false);
                Marshal.Copy(ptr, bytes, 0, size);
                return bytes;
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
        }

        [DllImport("setupapi.dll", SetLastError = true)]
        private static extern IntPtr SetupDiGetClassDevs(
            ref Guid classGuid,
            string? enumerator,
            IntPtr hwndParent,
            uint flags);

        [DllImport("setupapi.dll", SetLastError = true)]
        private static extern bool SetupDiEnumDeviceInterfaces(
            IntPtr deviceInfoSet,
            IntPtr deviceInfoData,
            ref Guid interfaceClassGuid,
            uint memberIndex,
            ref SP_DEVICE_INTERFACE_DATA deviceInterfaceData);

        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool SetupDiGetDeviceInterfaceDetail(
            IntPtr deviceInfoSet,
            ref SP_DEVICE_INTERFACE_DATA deviceInterfaceData,
            ref SP_DEVICE_INTERFACE_DETAIL_DATA deviceInterfaceDetailData,
            uint deviceInterfaceDetailDataSize,
            out uint requiredSize,
            IntPtr deviceInfoData);

        [DllImport("setupapi.dll", SetLastError = true)]
        private static extern bool SetupDiDestroyDeviceInfoList(IntPtr deviceInfoSet);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern SafeFileHandle CreateFile(
            string fileName,
            uint desiredAccess,
            uint shareMode,
            IntPtr securityAttributes,
            uint creationDisposition,
            uint flagsAndAttributes,
            IntPtr templateFile);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool DeviceIoControl(
            SafeFileHandle hDevice,
            uint dwIoControlCode,
            byte[] lpInBuffer,
            int nInBufferSize,
            byte[] lpOutBuffer,
            int nOutBufferSize,
            out int lpBytesReturned,
            IntPtr lpOverlapped);

        [StructLayout(LayoutKind.Sequential)]
        private struct KSPROPERTY
        {
            public Guid Set;
            public uint Id;
            public uint Flags;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct KSP_NODE
        {
            public KSPROPERTY Property;
            public uint NodeId;
            public uint Reserved;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct SP_DEVICE_INTERFACE_DATA
        {
            public int cbSize;
            public Guid InterfaceClassGuid;
            public int Flags;
            public IntPtr Reserved;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct SP_DEVICE_INTERFACE_DETAIL_DATA
        {
            public int cbSize;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string DevicePath;
        }
    }
}

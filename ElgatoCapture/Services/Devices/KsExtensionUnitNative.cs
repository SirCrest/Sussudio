using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace ElgatoCapture.Services.Devices;

internal static class KsExtensionUnitNative
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
    private const uint KsPropertyTypeSet = 0x00000002;
    private const uint KsPropertyTypeTopology = 0x10000000;
    private const uint KsPropertyTopologyNodes = 1;
    private const int ErrorInsufficientBuffer = 122;
    private const int ErrorMoreData = 234;
    internal const int ErrorNotFound = 1168;
    internal const int ErrorSetNotFound = 1170;
    internal const int ErrorInvalidParameter = 87;
    internal const int ErrorInvalidFunction = 1;
    private const int ErrorNoMoreItems = 259;

    internal readonly record struct KsInterfacePath(string Path, Guid CategoryGuid);

    internal readonly record struct KsTopologyNode(int NodeId, bool IsDevSpecific, Guid NodeType);

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
                        if (error == ErrorNoMoreItems)
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

        const int headerSize = 8;
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

    internal static bool TryXuGetDirect(
        SafeFileHandle handle,
        int nodeId,
        Guid propertySet,
        int selector,
        int bufferSize,
        out byte[] data,
        out int bytesReturned,
        out int? win32Code)
    {
        data = Array.Empty<byte>();
        bytesReturned = 0;
        win32Code = null;

        var request = new KSP_NODE
        {
            Property = new KSPROPERTY
            {
                Set = propertySet,
                Id = (uint)selector,
                Flags = KsPropertyTypeGet | KsPropertyTypeTopology
            },
            NodeId = (uint)nodeId,
            Reserved = 0
        };

        var input = StructureToBytes(request);
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
            var copiedLength = Math.Min(Math.Max(bytesReturned, 0), output.Length);
            data = copiedLength > 0
                ? output.AsSpan(0, copiedLength).ToArray()
                : Array.Empty<byte>();
            return true;
        }

        win32Code = Marshal.GetLastWin32Error();
        return false;
    }

    internal static bool TryXuSetViaOutput(
        SafeFileHandle handle,
        int nodeId,
        Guid propertySet,
        int selector,
        byte[] valueData,
        out int? win32Code)
    {
        win32Code = null;

        var request = new KSP_NODE
        {
            Property = new KSPROPERTY
            {
                Set = propertySet,
                Id = (uint)selector,
                Flags = KsPropertyTypeSet | KsPropertyTypeTopology
            },
            NodeId = (uint)nodeId,
            Reserved = 0
        };

        var input = StructureToBytes(request);
        if (DeviceIoControl(
                handle,
                IoctlKsProperty,
                input,
                input.Length,
                valueData,
                valueData.Length,
                out _,
                IntPtr.Zero))
        {
            return true;
        }

        win32Code = Marshal.GetLastWin32Error();
        return false;
    }

    private static bool TryReadNodePropertyBytes(
        SafeFileHandle handle,
        int nodeId,
        Guid propertySet,
        int propertyId,
        int maxBufferSize,
        out byte[] data,
        out int bytesReturned,
        out int? win32Code,
        out string? error)
    {
        data = Array.Empty<byte>();
        bytesReturned = 0;
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
        var bufferSize = Math.Min(256, maxBufferSize);

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
                var copiedLength = Math.Min(Math.Max(bytesReturned, 0), output.Length);
                data = copiedLength > 0
                    ? output.AsSpan(0, copiedLength).ToArray()
                    : Array.Empty<byte>();
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

    /// <summary>
    /// Alternative XU SET that appends data to KSP_NODE in the input buffer.
    /// Standard UVC SET_CUR typically uses this layout.
    /// </summary>
    internal static bool TryXuSetViaInput(
        SafeFileHandle handle,
        int nodeId,
        Guid propertySet,
        int selector,
        byte[] valueData,
        out int? win32Code)
    {
        win32Code = null;

        var request = new KSP_NODE
        {
            Property = new KSPROPERTY
            {
                Set = propertySet,
                Id = (uint)selector,
                Flags = KsPropertyTypeSet | KsPropertyTypeTopology
            },
            NodeId = (uint)nodeId,
            Reserved = 0
        };

        var headerBytes = StructureToBytes(request);
        var input = new byte[headerBytes.Length + valueData.Length];
        Array.Copy(headerBytes, input, headerBytes.Length);
        Array.Copy(valueData, 0, input, headerBytes.Length, valueData.Length);

        if (DeviceIoControl(
                handle,
                IoctlKsProperty,
                input,
                input.Length,
                Array.Empty<byte>(),
                0,
                out _,
                IntPtr.Zero))
        {
            return true;
        }

        win32Code = Marshal.GetLastWin32Error();
        return false;
    }

    private static byte[] StructureToBytes<T>(T value)
        where T : unmanaged
    {
        var bytes = new byte[Marshal.SizeOf<T>()];
        MemoryMarshal.Write(bytes, in value);
        return bytes;
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

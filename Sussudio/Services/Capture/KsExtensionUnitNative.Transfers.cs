using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Sussudio.Services.Capture;

internal static partial class KsExtensionUnitNative
{
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
}

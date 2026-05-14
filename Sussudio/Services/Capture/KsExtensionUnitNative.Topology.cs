using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Sussudio.Services.Capture;

internal static partial class KsExtensionUnitNative
{
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
}

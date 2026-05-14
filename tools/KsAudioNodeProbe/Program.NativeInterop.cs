using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using static KsAudioNodeProbeConstants;

static class KsAudioNodeProbeNative
{
    public static bool TryParseVidPid(string text, out ushort vendorId, out ushort productId)
    {
        vendorId = 0;
        productId = 0;
        var normalized = text.ToLowerInvariant();
        var vidIndex = normalized.IndexOf("vid_", StringComparison.Ordinal);
        var pidIndex = normalized.IndexOf("pid_", StringComparison.Ordinal);
        if (vidIndex < 0 || pidIndex < 0 || vidIndex + 8 > normalized.Length || pidIndex + 8 > normalized.Length)
        {
            return false;
        }

        return ushort.TryParse(normalized.Substring(vidIndex + 4, 4), System.Globalization.NumberStyles.HexNumber, null, out vendorId) &&
               ushort.TryParse(normalized.Substring(pidIndex + 4, 4), System.Globalization.NumberStyles.HexNumber, null, out productId);
    }

    public static List<string> EnumerateKsInterfaces(ushort vendorId, ushort productId)
    {
        var token = $"vid_{vendorId:x4}&pid_{productId:x4}";
        var result = new List<string>();
        var categories = new[]
        {
            new Guid("65E8773D-8F56-11D0-A3B9-00A0C9223196"),
            new Guid("6994AD05-93EF-11D0-A3CC-00A0C9223196")
        };

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
                var interfaceData = new SP_DEVICE_INTERFACE_DATA { cbSize = Marshal.SizeOf<SP_DEVICE_INTERFACE_DATA>() };
                for (uint index = 0; ; index++)
                {
                    interfaceData.cbSize = Marshal.SizeOf<SP_DEVICE_INTERFACE_DATA>();
                    if (!SetupDiEnumDeviceInterfaces(deviceInfoSet, IntPtr.Zero, ref categoryGuid, index, ref interfaceData))
                    {
                        if (Marshal.GetLastWin32Error() == ErrorNoMoreItems)
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

                    if (detail.DevicePath.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        result.Add(detail.DevicePath);
                    }
                }
            }
            finally
            {
                SetupDiDestroyDeviceInfoList(deviceInfoSet);
            }
        }

        return result.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    public static SafeFileHandle? TryOpen(string path, out int? errorCode)
    {
        errorCode = null;

        var readWrite = CreateFile(path, GenericRead | GenericWrite, FileShareRead | FileShareWrite, IntPtr.Zero, OpenExisting, 0, IntPtr.Zero);
        if (!readWrite.IsInvalid)
        {
            return readWrite;
        }

        errorCode = Marshal.GetLastWin32Error();
        readWrite.Dispose();
        return null;
    }

    public static bool TryAudioGetLong(SafeFileHandle handle, int nodeId, int propertyId, int channel, out int value, out int? win32)
    {
        value = 0;
        win32 = null;

        var request = new KsNodePropertyAudioChannel
        {
            NodeProperty = new KsNodeProperty
            {
                Property = new KsProperty
                {
                    Set = new Guid("45FFAAA0-6E1B-11D0-BCF2-444553540000"),
                    Id = (uint)propertyId,
                    Flags = KsPropertyTypeGet | KsPropertyTypeTopology
                },
                NodeId = (uint)nodeId,
                Reserved = 0
            },
            Channel = channel,
            Reserved = 0
        };

        var input = StructureToBytes(request);
        var output = new byte[4];
        if (DeviceIoControl(handle, IoctlKsProperty, input, input.Length, output, output.Length, out var bytesReturned, IntPtr.Zero))
        {
            if (bytesReturned >= 4)
            {
                value = BitConverter.ToInt32(output, 0);
                return true;
            }

            return false;
        }

        win32 = Marshal.GetLastWin32Error();
        return false;
    }

    public static bool TryAudioSetLong(SafeFileHandle handle, int nodeId, int propertyId, int channel, int value, out int? win32)
    {
        win32 = null;

        var request = new KsNodePropertyAudioChannel
        {
            NodeProperty = new KsNodeProperty
            {
                Property = new KsProperty
                {
                    Set = new Guid("45FFAAA0-6E1B-11D0-BCF2-444553540000"),
                    Id = (uint)propertyId,
                    Flags = KsPropertyTypeSet | KsPropertyTypeTopology
                },
                NodeId = (uint)nodeId,
                Reserved = 0
            },
            Channel = channel,
            Reserved = 0
        };

        var input = StructureToBytes(request);
        var output = BitConverter.GetBytes(value);
        if (DeviceIoControl(handle, IoctlKsProperty, input, input.Length, output, output.Length, out _, IntPtr.Zero))
        {
            return true;
        }

        var error = Marshal.GetLastWin32Error();
        if (error is ErrorInsufficientBuffer or ErrorMoreData)
        {
            var larger = new byte[16];
            BitConverter.GetBytes(value).CopyTo(larger, 0);
            if (DeviceIoControl(handle, IoctlKsProperty, input, input.Length, larger, larger.Length, out _, IntPtr.Zero))
            {
                return true;
            }

            error = Marshal.GetLastWin32Error();
        }

        win32 = error;
        return false;
    }

    public static string DescribeWin32(int? code)
        => code == null ? "n/a" : $"{code} ({new Win32Exception(code.Value).Message})";

    public static List<(int NodeId, Guid NodeType)> EnumerateTopologyNodeTypes(SafeFileHandle handle)
    {
        var result = new List<(int, Guid)>();
        var topologySet = new Guid("720D4AC0-7533-11D0-A5D6-28DB04C10000");
        var request = new KsProperty
        {
            Set = topologySet,
            Id = 1,
            Flags = KsPropertyTypeGet
        };
        var input = StructureToBytes(request);
        var output = new byte[4096];
        if (!DeviceIoControl(handle, IoctlKsProperty, input, input.Length, output, output.Length, out var bytesReturned, IntPtr.Zero))
        {
            Console.WriteLine($"  Topology enumeration failed: {DescribeWin32(Marshal.GetLastWin32Error())}");
            return result;
        }

        var guidSize = 16;
        var nodeCount = bytesReturned / guidSize;
        for (var i = 0; i < nodeCount; i++)
        {
            var guid = new Guid(output.AsSpan(i * guidSize, guidSize));
            result.Add((i, guid));
        }

        return result;
    }

    public static string DescribeNodeType(Guid nodeType)
    {
        var known = new Dictionary<string, string>
        {
            { "DFF220E1-F70F-11D0-B917-00A0C9223196", "KSNODETYPE_VOLUME" },
            { "DFF220E3-F70F-11D0-B917-00A0C9223196", "KSNODETYPE_MUTE" },
            { "DFF220E0-F70F-11D0-B917-00A0C9223196", "KSNODETYPE_MUX (Selector)" },
            { "DFF220E2-F70F-11D0-B917-00A0C9223196", "KSNODETYPE_SUM" },
            { "DFF220E5-F70F-11D0-B917-00A0C9223196", "KSNODETYPE_AGC" },
            { "DFF220E4-F70F-11D0-B917-00A0C9223196", "KSNODETYPE_LOUDNESS" },
            { "DFF21FE0-F70F-11D0-B917-00A0C9223196", "KSNODETYPE_DAC" },
            { "DFF21FE1-F70F-11D0-B917-00A0C9223196", "KSNODETYPE_ADC" },
            { "DFF21FE2-F70F-11D0-B917-00A0C9223196", "KSNODETYPE_SRC (Sample Rate)" },
            { "DFF21FE3-F70F-11D0-B917-00A0C9223196", "KSNODETYPE_SUPERMIX" },
            { "DFF21FE4-F70F-11D0-B917-00A0C9223196", "KSNODETYPE_MUX_2" },
            { "DFF21FE5-F70F-11D0-B917-00A0C9223196", "KSNODETYPE_DEMUX" },
            { "DFF21BE1-F70F-11D0-B917-00A0C9223196", "KSNODETYPE_ACOUSTIC_ECHO_CANCEL" },
            { "DFF21BE2-F70F-11D0-B917-00A0C9223196", "KSNODETYPE_NOISE_SUPPRESS" },
            { "DFF21CE1-F70F-11D0-B917-00A0C9223196", "KSNODETYPE_TONE" },
            { "DFF21CE2-F70F-11D0-B917-00A0C9223196", "KSNODETYPE_EQUALIZER" },
            { "DFF21DE1-F70F-11D0-B917-00A0C9223196", "KSNODETYPE_DELAY" },
            { "DFF21FE6-F70F-11D0-B917-00A0C9223196", "KSNODETYPE_DEV_SPECIFIC" },
        };
        var key = nodeType.ToString().ToUpperInvariant();
        return known.TryGetValue(key, out var name) ? name : $"Unknown ({nodeType})";
    }

    private static byte[] StructureToBytes<T>(T value) where T : unmanaged
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
}

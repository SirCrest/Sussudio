using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

const uint IoctlKsProperty = 0x002F0003;
const uint DigcfPresent = 0x00000002;
const uint DigcfDeviceInterface = 0x00000010;
const uint GenericRead = 0x80000000;
const uint GenericWrite = 0x40000000;
const uint FileShareRead = 0x00000001;
const uint FileShareWrite = 0x00000002;
const uint OpenExisting = 3;
const uint KsPropertyTypeGet = 0x00000001;
const uint KsPropertyTypeSet = 0x00000002;
const uint KsPropertyTypeTopology = 0x10000000;
const int ErrorNoMoreItems = 259;
const int ErrorInsufficientBuffer = 122;
const int ErrorMoreData = 234;

var holdMode = args.Any(a => a.StartsWith("--set-hold", StringComparison.OrdinalIgnoreCase));
var selector = args.FirstOrDefault(a => !a.StartsWith("--")) ?? "VID_0FD9&PID_009D";
if (!TryParseVidPid(selector, out var vendorId, out var productId))
{
    Console.Error.WriteLine($"Could not parse VID/PID from '{selector}'.");
    return 1;
}

var interfaces = EnumerateKsInterfaces(vendorId, productId);
var audioInterface = interfaces.FirstOrDefault(path =>
    path.Contains("&mi_02#", StringComparison.OrdinalIgnoreCase) &&
    path.Contains("{65e8773d-8f56-11d0-a3b9-00a0c9223196}", StringComparison.OrdinalIgnoreCase));

if (audioInterface == null)
{
    Console.Error.WriteLine("Could not find the MI_02 audio KS interface.");
    return 1;
}

Console.WriteLine($"Audio KS path: {audioInterface}");

using var handle = TryOpen(audioInterface, out var openError);
if (handle == null)
{
    Console.Error.WriteLine($"Failed to open audio KS interface: {DescribeWin32(openError)}");
    return 1;
}

if (holdMode)
{
    // Targeted test: set node 2, property 13 (the binary switch) and hold
    Console.WriteLine("== SET-AND-HOLD MODE ==");
    var holdNodeId = 2;
    var holdPropId = 13; // BASS (repurposed as selector)
    var holdChannel = 0;

    if (TryAudioGetLong(handle, holdNodeId, holdPropId, holdChannel, out var currentVal, out var getErr))
    {
        Console.WriteLine($"Current: node={holdNodeId} prop={holdPropId} ch={holdChannel} value={currentVal}");
        var targetVal = currentVal == 0 ? 1 : 0;
        Console.WriteLine($"Setting to {targetVal}...");
        if (TryAudioSetLong(handle, holdNodeId, holdPropId, holdChannel, targetVal, out var setErr))
        {
            TryAudioGetLong(handle, holdNodeId, holdPropId, holdChannel, out var afterSet, out _);
            Console.WriteLine($"Readback after SET: {afterSet}");
            Console.WriteLine($"Holding for 10 seconds — check AT telemetry (AdcOnOff, InputSource, selector 3 payload)...");
            Thread.Sleep(10000);
            TryAudioGetLong(handle, holdNodeId, holdPropId, holdChannel, out var stillSet, out _);
            Console.WriteLine($"Value after hold: {stillSet}");
            Console.WriteLine($"Restoring to {currentVal}...");
            TryAudioSetLong(handle, holdNodeId, holdPropId, holdChannel, currentVal, out _);
            TryAudioGetLong(handle, holdNodeId, holdPropId, holdChannel, out var restored, out _);
            Console.WriteLine($"Restored: {restored}");
        }
        else
        {
            Console.WriteLine($"SET failed: {DescribeWin32(setErr)}");
        }
    }
    else
    {
        Console.WriteLine($"GET failed: {DescribeWin32(getErr)}");
    }
    return 0;
}

// Enumerate topology nodes
Console.WriteLine("== Topology node enumeration ==");
var topologyNodeTypes = EnumerateTopologyNodeTypes(handle);
if (topologyNodeTypes.Count == 0)
{
    Console.WriteLine("No topology nodes found. Trying brute-force node scan...");
}
else
{
    foreach (var (nodeId, nodeType) in topologyNodeTypes)
    {
        Console.WriteLine($"  Node {nodeId}: {DescribeNodeType(nodeType)}");
    }
}

// Brute-force scan: try all node IDs (0-31) with common audio properties
Console.WriteLine();
Console.WriteLine("== Brute-force node/property scan ==");
var audioPropertySet = new Guid("45FFAAA0-6E1B-11D0-BCF2-444553540000");
var propertyNames = new Dictionary<int, string>
{
    { 1, "KSPROPERTY_AUDIO_LATENCY" },
    { 2, "KSPROPERTY_AUDIO_COPY_PROTECTION" },
    { 3, "KSPROPERTY_AUDIO_CHANNEL_CONFIG" },
    { 4, "KSPROPERTY_AUDIO_VOLUMELEVEL" },
    { 5, "KSPROPERTY_AUDIO_POSITION" },
    { 6, "KSPROPERTY_AUDIO_DYNAMIC_RANGE" },
    { 7, "KSPROPERTY_AUDIO_QUALITY" },
    { 8, "KSPROPERTY_AUDIO_SAMPLING_RATE" },
    { 9, "KSPROPERTY_AUDIO_DYNAMIC_SAMPLING_RATE" },
    { 10, "KSPROPERTY_AUDIO_MIX_LEVEL_TABLE" },
    { 11, "KSPROPERTY_AUDIO_MUX_SOURCE" },
    { 12, "KSPROPERTY_AUDIO_MUTE" },
    { 13, "KSPROPERTY_AUDIO_BASS" },
    { 14, "KSPROPERTY_AUDIO_MID" },
    { 15, "KSPROPERTY_AUDIO_TREBLE" },
    { 16, "KSPROPERTY_AUDIO_BASS_BOOST" },
    { 17, "KSPROPERTY_AUDIO_EQ_LEVEL" },
    { 18, "KSPROPERTY_AUDIO_NUM_EQ_BANDS" },
    { 19, "KSPROPERTY_AUDIO_EQ_BANDS" },
    { 20, "KSPROPERTY_AUDIO_AGC" },
};

for (int nodeId = 0; nodeId < 32; nodeId++)
{
    var hits = new List<string>();
    foreach (var (propId, propName) in propertyNames)
    {
        if (TryAudioGetLong(handle, nodeId, propId, channel: 0, out var val, out _))
        {
            hits.Add($"{propName}({propId})={val}");
        }
    }
    if (hits.Count > 0)
    {
        Console.WriteLine($"  Node {nodeId}: {string.Join(", ", hits)}");
    }
}

// Extended scan on responsive nodes with SET tests
Console.WriteLine();
Console.WriteLine("== Extended node tests ==");
for (int nodeId = 0; nodeId < 32; nodeId++)
{
    var anyHit = false;
    for (int propId = 1; propId <= 20; propId++)
    {
        for (int ch = -1; ch <= 1; ch++)
        {
            if (TryAudioGetLong(handle, nodeId, propId, ch, out var val, out _))
            {
                var pName = propertyNames.TryGetValue(propId, out var n) ? n : $"Property({propId})";
                Console.WriteLine($"  Node {nodeId}, {pName}, ch={ch}: GET={val}");
                anyHit = true;

                // Try SET on this property
                if (propId == 12) // MUTE
                {
                    var newVal = val == 0 ? 1 : 0;
                    if (TryAudioSetLong(handle, nodeId, propId, ch, newVal, out var setErr))
                    {
                        TryAudioGetLong(handle, nodeId, propId, ch, out var afterSet, out _);
                        Console.WriteLine($"    SET {newVal} -> readback={afterSet}");
                        // Restore
                        TryAudioSetLong(handle, nodeId, propId, ch, val, out _);
                        Console.WriteLine($"    RESTORED to {val}");
                    }
                    else
                    {
                        Console.WriteLine($"    SET {newVal} FAILED: {DescribeWin32(setErr)}");
                    }
                }
                else if (propId == 11) // MUX
                {
                    foreach (var target in new[] { 0, 1, 2, 3 })
                    {
                        if (TryAudioSetLong(handle, nodeId, propId, ch, target, out var setErr))
                        {
                            TryAudioGetLong(handle, nodeId, propId, ch, out var afterSet, out _);
                            Console.WriteLine($"    MUX SET {target} -> readback={afterSet}");
                        }
                        else
                        {
                            Console.WriteLine($"    MUX SET {target} FAILED: {DescribeWin32(setErr)}");
                        }
                    }
                    // Restore
                    TryAudioSetLong(handle, nodeId, propId, ch, val, out _);
                }
                else if (propId == 4) // VOLUME
                {
                    var testVal = val + 65536;
                    if (TryAudioSetLong(handle, nodeId, propId, ch, testVal, out var setErr))
                    {
                        TryAudioGetLong(handle, nodeId, propId, ch, out var afterSet, out _);
                        Console.WriteLine($"    VOLUME SET {testVal} -> readback={afterSet}");
                        TryAudioSetLong(handle, nodeId, propId, ch, val, out _);
                        Console.WriteLine($"    RESTORED to {val}");
                    }
                    else
                    {
                        Console.WriteLine($"    VOLUME SET {testVal} FAILED: {DescribeWin32(setErr)}");
                    }
                }
                else if (propId == 13) // BASS (might be repurposed)
                {
                    foreach (var target in new[] { 0, 1, 2, 3 })
                    {
                        if (target == val) continue;
                        if (TryAudioSetLong(handle, nodeId, propId, ch, target, out var setErr))
                        {
                            TryAudioGetLong(handle, nodeId, propId, ch, out var afterSet, out _);
                            Console.WriteLine($"    BASS SET {target} -> readback={afterSet}");
                        }
                        else
                        {
                            Console.WriteLine($"    BASS SET {target} FAILED: {DescribeWin32(setErr)}");
                        }
                    }
                    // Restore
                    TryAudioSetLong(handle, nodeId, propId, ch, val, out _);
                }
            }
        }
    }
}

Console.WriteLine("== ADC volume probe ==");
foreach (var channel in new[] { -1, 0, 1 })
{
    if (TryAudioGetLong(handle, nodeId: 0, propertyId: 4, channel, out var value, out var error))
    {
        Console.WriteLine($"Read volume channel={channel}: {value}");

        var target = value == 0 ? -6 * 65536 : value + 65536;
        if (TryAudioSetLong(handle, nodeId: 0, propertyId: 4, channel, target, out var setError))
        {
            if (TryAudioGetLong(handle, nodeId: 0, propertyId: 4, channel, out var after, out error))
            {
                Console.WriteLine($"  Set volume -> {target}; readback={after}");
            }
            else
            {
                Console.WriteLine($"  Set volume -> {target}; readback failed: {DescribeWin32(error)}");
            }

            TryAudioSetLong(handle, nodeId: 0, propertyId: 4, channel, value, out _);
        }
        else
        {
            Console.WriteLine($"  Set volume channel={channel} failed: {DescribeWin32(setError)}");
        }
    }
    else
    {
        Console.WriteLine($"Read volume channel={channel} failed: {DescribeWin32(error)}");
    }
}

Console.WriteLine();
Console.WriteLine("== Mux probe ==");
if (TryAudioGetLong(handle, nodeId: 3, propertyId: 11, channel: 0, out var muxValue, out var muxError))
{
    Console.WriteLine($"Read mux source: {muxValue}");
    foreach (var target in new[] { 0, 1, 2, 3 })
    {
        if (TryAudioSetLong(handle, nodeId: 3, propertyId: 11, channel: 0, target, out var setError))
        {
            if (TryAudioGetLong(handle, nodeId: 3, propertyId: 11, channel: 0, out var after, out muxError))
            {
                Console.WriteLine($"  Set mux -> {target}; readback={after}");
            }
            else
            {
                Console.WriteLine($"  Set mux -> {target}; readback failed: {DescribeWin32(muxError)}");
            }
        }
        else
        {
            Console.WriteLine($"  Set mux -> {target} failed: {DescribeWin32(setError)}");
        }
    }

    TryAudioSetLong(handle, nodeId: 3, propertyId: 11, channel: 0, muxValue, out _);
}
else
{
    Console.WriteLine($"Read mux source failed: {DescribeWin32(muxError)}");
}

Console.WriteLine();
Console.WriteLine("== Mute probe ==");
if (TryAudioGetLong(handle, nodeId: 2, propertyId: 12, channel: 0, out var muteValue, out var muteError))
{
    Console.WriteLine($"Read mute: {muteValue}");
    foreach (var target in new[] { 0, 1 })
    {
        if (TryAudioSetLong(handle, nodeId: 2, propertyId: 12, channel: 0, target, out var setError))
        {
            if (TryAudioGetLong(handle, nodeId: 2, propertyId: 12, channel: 0, out var after, out muteError))
            {
                Console.WriteLine($"  Set mute -> {target}; readback={after}");
            }
            else
            {
                Console.WriteLine($"  Set mute -> {target}; readback failed: {DescribeWin32(muteError)}");
            }
        }
        else
        {
            Console.WriteLine($"  Set mute -> {target} failed: {DescribeWin32(setError)}");
        }
    }

    TryAudioSetLong(handle, nodeId: 2, propertyId: 12, channel: 0, muteValue, out _);
}
else
{
    Console.WriteLine($"Read mute failed: {DescribeWin32(muteError)}");
}

return 0;

static bool TryParseVidPid(string text, out ushort vendorId, out ushort productId)
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

static List<string> EnumerateKsInterfaces(ushort vendorId, ushort productId)
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

static SafeFileHandle? TryOpen(string path, out int? errorCode)
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

static bool TryAudioGetLong(SafeFileHandle handle, int nodeId, int propertyId, int channel, out int value, out int? win32)
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

static bool TryAudioSetLong(SafeFileHandle handle, int nodeId, int propertyId, int channel, int value, out int? win32)
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

static byte[] StructureToBytes<T>(T value) where T : unmanaged
{
    var bytes = new byte[Marshal.SizeOf<T>()];
    MemoryMarshal.Write(bytes, in value);
    return bytes;
}

static string DescribeWin32(int? code)
    => code == null ? "n/a" : $"{code} ({new Win32Exception(code.Value).Message})";

static List<(int NodeId, Guid NodeType)> EnumerateTopologyNodeTypes(SafeFileHandle handle)
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
    for (int i = 0; i < nodeCount; i++)
    {
        var guid = new Guid(output.AsSpan(i * guidSize, guidSize));
        result.Add((i, guid));
    }
    return result;
}

static string DescribeNodeType(Guid nodeType)
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

[DllImport("setupapi.dll", SetLastError = true)]
static extern IntPtr SetupDiGetClassDevs(
    ref Guid classGuid,
    string? enumerator,
    IntPtr hwndParent,
    uint flags);

[DllImport("setupapi.dll", SetLastError = true)]
static extern bool SetupDiEnumDeviceInterfaces(
    IntPtr deviceInfoSet,
    IntPtr deviceInfoData,
    ref Guid interfaceClassGuid,
    uint memberIndex,
    ref SP_DEVICE_INTERFACE_DATA deviceInterfaceData);

[DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Unicode)]
static extern bool SetupDiGetDeviceInterfaceDetail(
    IntPtr deviceInfoSet,
    ref SP_DEVICE_INTERFACE_DATA deviceInterfaceData,
    ref SP_DEVICE_INTERFACE_DETAIL_DATA deviceInterfaceDetailData,
    uint deviceInterfaceDetailDataSize,
    out uint requiredSize,
    IntPtr deviceInfoData);

[DllImport("setupapi.dll", SetLastError = true)]
static extern bool SetupDiDestroyDeviceInfoList(IntPtr deviceInfoSet);

[DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
static extern SafeFileHandle CreateFile(
    string fileName,
    uint desiredAccess,
    uint shareMode,
    IntPtr securityAttributes,
    uint creationDisposition,
    uint flagsAndAttributes,
    IntPtr templateFile);

[DllImport("kernel32.dll", SetLastError = true)]
static extern bool DeviceIoControl(
    SafeFileHandle hDevice,
    uint dwIoControlCode,
    byte[] lpInBuffer,
    int nInBufferSize,
    byte[] lpOutBuffer,
    int nOutBufferSize,
    out int lpBytesReturned,
    IntPtr lpOverlapped);

[StructLayout(LayoutKind.Sequential)]
struct KsProperty
{
    public Guid Set;
    public uint Id;
    public uint Flags;
}

[StructLayout(LayoutKind.Sequential)]
struct KsNodeProperty
{
    public KsProperty Property;
    public uint NodeId;
    public uint Reserved;
}

[StructLayout(LayoutKind.Sequential)]
struct KsNodePropertyAudioChannel
{
    public KsNodeProperty NodeProperty;
    public int Channel;
    public uint Reserved;
}

[StructLayout(LayoutKind.Sequential)]
struct SP_DEVICE_INTERFACE_DATA
{
    public int cbSize;
    public Guid InterfaceClassGuid;
    public int Flags;
    public IntPtr Reserved;
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
struct SP_DEVICE_INTERFACE_DETAIL_DATA
{
    public int cbSize;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
    public string DevicePath;
}

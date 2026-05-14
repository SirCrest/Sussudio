using System.Runtime.InteropServices;

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

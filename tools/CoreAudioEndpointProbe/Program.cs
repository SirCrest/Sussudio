using System.Runtime.InteropServices;

var nameFilter = args.Length > 0 ? args[0] : "Elgato 4K X";

var enumerator = (IMMDeviceEnumerator)new MMDeviceEnumeratorComObject();
enumerator.EnumAudioEndpoints(EDataFlow.eCapture, DeviceState.Active, out var collection);
collection.GetCount(out var count);

Console.WriteLine($"Capture endpoints: {count}");

IMMDevice? target = null;
for (uint i = 0; i < count; i++)
{
    collection.Item(i, out var device);
    var name = GetFriendlyName(device);
    device.GetId(out var id);
    Console.WriteLine($"[{i}] {name}");
    Console.WriteLine($"    {id}");
    if (target == null && name.Contains(nameFilter, StringComparison.OrdinalIgnoreCase))
    {
        target = device;
    }
}

if (target == null)
{
    Console.Error.WriteLine($"No capture endpoint matched '{nameFilter}'.");
    return 1;
}

var endpointVolumeGuid = typeof(IAudioEndpointVolume).GUID;
ThrowIfFailed(target.Activate(ref endpointVolumeGuid, 23, IntPtr.Zero, out var endpointVolumeObj), "activate endpoint volume");
var endpointVolume = (IAudioEndpointVolume)endpointVolumeObj;

ThrowIfFailed(endpointVolume.GetMasterVolumeLevelScalar(out var originalScalar), "read original endpoint volume");
ThrowIfFailed(endpointVolume.GetVolumeRange(out var minDb, out var maxDb, out var incrementDb), "read endpoint volume range");
ThrowIfFailed(endpointVolume.GetMute(out var originalMute), "read endpoint mute state");

Console.WriteLine();
Console.WriteLine("== Endpoint volume ==");
Console.WriteLine($"Scalar: {originalScalar:0.000}");
Console.WriteLine($"Range dB: {minDb:0.##} .. {maxDb:0.##} step {incrementDb:0.##}");
Console.WriteLine($"Mute: {originalMute}");

var targetScalar = originalScalar > 0.55f ? 0.35f : 0.75f;
var restoreRequired = false;
var restoreFailed = false;
try
{
    restoreRequired = true;
    ThrowIfFailed(endpointVolume.SetMasterVolumeLevelScalar(targetScalar, Guid.Empty), "set endpoint volume");
    ThrowIfFailed(endpointVolume.GetMasterVolumeLevelScalar(out var afterScalar), "read endpoint volume after set");
    Console.WriteLine($"After set -> {afterScalar:0.000}");
}
finally
{
    if (restoreRequired)
    {
        try
        {
            ThrowIfFailed(endpointVolume.SetMasterVolumeLevelScalar(originalScalar, Guid.Empty), "restore endpoint volume");
            ThrowIfFailed(endpointVolume.GetMasterVolumeLevelScalar(out var restoredScalar), "read endpoint volume after restore");
            Console.WriteLine($"Restored -> {restoredScalar:0.000}");
        }
        catch (Exception ex)
        {
            restoreFailed = true;
            Console.Error.WriteLine($"Restore failed: {ex.GetType().Name}: {ex.Message}");
        }
    }
}

return restoreFailed ? 1 : 0;

static void ThrowIfFailed(int hresult, string operation)
{
    if (hresult < 0)
    {
        throw new COMException($"{operation} failed.", hresult);
    }
}

static string GetFriendlyName(IMMDevice device)
{
    var key = new PropertyKey(
        new Guid("A45C254E-DF1C-4EFD-8020-67D146A850E0"),
        14);

    device.OpenPropertyStore(0, out var store);
    store.GetValue(ref key, out var value);
    try
    {
        return value.Value ?? "(unnamed)";
    }
    finally
    {
        PropVariantClear(ref value);
    }
}

[DllImport("ole32.dll")]
static extern int PropVariantClear(ref PropVariant pvar);

[ComImport]
[Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
class MMDeviceEnumeratorComObject
{
}

enum EDataFlow
{
    eRender,
    eCapture,
    eAll,
    EDataFlow_enum_count
}

[Flags]
enum DeviceState : uint
{
    Active = 0x00000001,
    Disabled = 0x00000002,
    NotPresent = 0x00000004,
    Unplugged = 0x00000008,
    All = 0x0000000F
}

[ComImport]
[Guid("A95664D2-9614-4F35-A746-DE8DB63617E6")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
interface IMMDeviceEnumerator
{
    [PreserveSig] int EnumAudioEndpoints(EDataFlow dataFlow, DeviceState dwStateMask, out IMMDeviceCollection ppDevices);
    [PreserveSig] int GetDefaultAudioEndpoint(EDataFlow dataFlow, int role, out IMMDevice ppDevice);
    [PreserveSig] int GetDevice([MarshalAs(UnmanagedType.LPWStr)] string pwstrId, out IMMDevice ppDevice);
    [PreserveSig] int RegisterEndpointNotificationCallback(IntPtr pClient);
    [PreserveSig] int UnregisterEndpointNotificationCallback(IntPtr pClient);
}

[ComImport]
[Guid("0BD7A1BE-7A1A-44DB-8397-C0ECF42C4767")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
interface IMMDeviceCollection
{
    [PreserveSig] int GetCount(out uint pcDevices);
    [PreserveSig] int Item(uint nDevice, out IMMDevice ppDevice);
}

[ComImport]
[Guid("D666063F-1587-4E43-81F1-B948E807363F")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
interface IMMDevice
{
    [PreserveSig] int Activate(ref Guid iid, int dwClsCtx, IntPtr pActivationParams, [MarshalAs(UnmanagedType.Interface)] out object ppInterface);
    [PreserveSig] int OpenPropertyStore(int stgmAccess, out IPropertyStore ppProperties);
    [PreserveSig] int GetId([MarshalAs(UnmanagedType.LPWStr)] out string ppstrId);
    [PreserveSig] int GetState(out DeviceState pdwState);
}

[ComImport]
[Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
interface IPropertyStore
{
    [PreserveSig] int GetCount(out uint cProps);
    [PreserveSig] int GetAt(uint iProp, out PropertyKey pkey);
    [PreserveSig] int GetValue(ref PropertyKey key, out PropVariant pv);
}

[ComImport]
[Guid("5CDF2C82-841E-4546-9722-0CF74078229A")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
interface IAudioEndpointVolume
{
    int RegisterControlChangeNotify(IntPtr pNotify);
    int UnregisterControlChangeNotify(IntPtr pNotify);
    int GetChannelCount(out uint pnChannelCount);
    int SetMasterVolumeLevel(float fLevelDB, Guid pguidEventContext);
    int SetMasterVolumeLevelScalar(float fLevel, Guid pguidEventContext);
    int GetMasterVolumeLevel(out float pfLevelDB);
    int GetMasterVolumeLevelScalar(out float pfLevel);
    int SetChannelVolumeLevel(uint nChannel, float fLevelDB, Guid pguidEventContext);
    int SetChannelVolumeLevelScalar(uint nChannel, float fLevel, Guid pguidEventContext);
    int GetChannelVolumeLevel(uint nChannel, out float pfLevelDB);
    int GetChannelVolumeLevelScalar(uint nChannel, out float pfLevel);
    int SetMute([MarshalAs(UnmanagedType.Bool)] bool bMute, Guid pguidEventContext);
    int GetMute(out bool pbMute);
    int GetVolumeStepInfo(out uint pnStep, out uint pnStepCount);
    int VolumeStepUp(Guid pguidEventContext);
    int VolumeStepDown(Guid pguidEventContext);
    int QueryHardwareSupport(out uint pdwHardwareSupportMask);
    int GetVolumeRange(out float pflVolumeMindB, out float pflVolumeMaxdB, out float pflVolumeIncrementdB);
}

[StructLayout(LayoutKind.Sequential)]
struct PropertyKey
{
    public PropertyKey(Guid fmtid, uint pid)
    {
        Fmtid = fmtid;
        Pid = pid;
    }

    public Guid Fmtid;
    public uint Pid;
}

[StructLayout(LayoutKind.Explicit)]
struct PropVariant
{
    [FieldOffset(0)] public ushort vt;
    [FieldOffset(8)] private IntPtr pointerValue;

    public string? Value => vt == 31 ? Marshal.PtrToStringUni(pointerValue) : null;
}

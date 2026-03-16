using System;
using System.Runtime.InteropServices;

namespace ElgatoCapture.Services;

internal static class WasapiComInterop
{
    internal const uint CLSCTX_ALL = 0x17;
    internal const uint DEVICE_STATE_ACTIVE = 0x00000001;
    internal const uint STGM_READ = 0;
    internal const int AUDCLNT_SHAREMODE_SHARED = 0;
    internal const int AUDCLNT_SHAREMODE_EXCLUSIVE = 1;
    internal const uint AUDCLNT_STREAMFLAGS_EVENTCALLBACK = 0x00040000;
    internal const uint AUDCLNT_STREAMFLAGS_LOOPBACK = 0x00020000;
    internal const uint AUDCLNT_BUFFERFLAGS_SILENT = 0x00000002;

    internal const int WAVE_FORMAT_PCM = 0x0001;
    internal const int WAVE_FORMAT_IEEE_FLOAT = 0x0003;
    internal const int WAVE_FORMAT_EXTENSIBLE = 0xFFFE;

    internal const int S_OK = 0;
    internal const int S_FALSE = 1;

    internal static readonly Guid CLSID_MMDeviceEnumerator = new("BCDE0395-E52F-467C-8E3D-C4579291692E");
    internal static readonly Guid IID_IAudioCaptureClient = new("C8ADBD64-E71E-48A0-A4DE-185C395CD317");
    internal static readonly Guid IID_IAudioRenderClient = new("F294ACFC-3146-4483-A7BF-ADDCA7C260E2");
    internal static readonly Guid KSDATAFORMAT_SUBTYPE_PCM = new("00000001-0000-0010-8000-00AA00389B71");
    internal static readonly Guid KSDATAFORMAT_SUBTYPE_IEEE_FLOAT = new("00000003-0000-0010-8000-00AA00389B71");
    internal static readonly PROPERTYKEY PKEY_Device_FriendlyName = new()
    {
        fmtid = new Guid("a45c254e-df1c-4efd-8020-67d146a850e0"),
        pid = 14
    };

    [DllImport("ole32.dll")]
    internal static extern int CoCreateInstance(
        ref Guid rclsid,
        IntPtr pUnkOuter,
        uint dwClsContext,
        ref Guid riid,
        [MarshalAs(UnmanagedType.IUnknown)] out object ppv);

    [DllImport("ole32.dll")]
    internal static extern void CoTaskMemFree(IntPtr ptr);

    internal const uint WaitObject0 = 0;
    internal const uint WaitTimeout = 0x00000102;

    [DllImport("kernel32.dll")]
    internal static extern uint WaitForSingleObject(IntPtr handle, uint milliseconds);

    internal static IMMDeviceEnumerator CreateDeviceEnumerator()
    {
        var clsid = CLSID_MMDeviceEnumerator;
        var iid = typeof(IMMDeviceEnumerator).GUID;
        var hr = CoCreateInstance(
            ref clsid,
            IntPtr.Zero,
            CLSCTX_ALL,
            ref iid,
            out var obj);
        ThrowIfFailed(hr, "CoCreateInstance(MMDeviceEnumerator)");
        return (IMMDeviceEnumerator)obj;
    }

    internal static IntPtr AllocFloatStereo48kFormat()
    {
        var format = new WAVEFORMATEXTENSIBLE
        {
            Format = new WAVEFORMATEX
            {
                wFormatTag = WAVE_FORMAT_EXTENSIBLE,
                nChannels = 2,
                nSamplesPerSec = 48_000,
                nAvgBytesPerSec = 48_000 * 2 * 4,
                nBlockAlign = 8,
                wBitsPerSample = 32,
                cbSize = (ushort)(Marshal.SizeOf<WAVEFORMATEXTENSIBLE>() - Marshal.SizeOf<WAVEFORMATEX>())
            },
            wValidBitsPerSample = 32,
            dwChannelMask = 0x3,
            SubFormat = KSDATAFORMAT_SUBTYPE_IEEE_FLOAT
        };

        var ptr = Marshal.AllocCoTaskMem(Marshal.SizeOf<WAVEFORMATEXTENSIBLE>());
        Marshal.StructureToPtr(format, ptr, false);
        return ptr;
    }

    internal static WasapiAudioFormat ReadAudioFormat(IntPtr formatPtr)
    {
        if (formatPtr == IntPtr.Zero)
        {
            throw new InvalidOperationException("WASAPI format pointer is null.");
        }

        var wave = Marshal.PtrToStructure<WAVEFORMATEX>(formatPtr);
        var formatTag = wave.wFormatTag;
        var channels = wave.nChannels;
        var sampleRate = wave.nSamplesPerSec;
        var bitsPerSample = wave.wBitsPerSample;
        var blockAlign = wave.nBlockAlign;
        var subFormat = Guid.Empty;

        if (formatTag == WAVE_FORMAT_EXTENSIBLE && wave.cbSize >= 22)
        {
            var extensible = Marshal.PtrToStructure<WAVEFORMATEXTENSIBLE>(formatPtr);
            subFormat = extensible.SubFormat;
            bitsPerSample = extensible.wValidBitsPerSample == 0
                ? extensible.Format.wBitsPerSample
                : extensible.wValidBitsPerSample;
            blockAlign = extensible.Format.nBlockAlign;
        }

        var sampleType = ResolveSampleType(formatTag, subFormat, bitsPerSample);
        if (channels <= 0)
        {
            throw new InvalidOperationException("WASAPI format has invalid channel count.");
        }

        if (sampleRate <= 0)
        {
            throw new InvalidOperationException("WASAPI format has invalid sample rate.");
        }

        if (blockAlign <= 0)
        {
            throw new InvalidOperationException("WASAPI format has invalid block alignment.");
        }

        var bytesPerSample = blockAlign / channels;
        if (bytesPerSample <= 0)
        {
            throw new InvalidOperationException("WASAPI format has invalid bytes-per-sample.");
        }

        return new WasapiAudioFormat(
            (int)sampleRate,
            channels,
            bitsPerSample,
            blockAlign,
            sampleType);
    }

    internal static void ThrowIfFailed(int hr, string operation)
    {
        if (hr >= 0)
        {
            return;
        }

        throw new COMException($"{operation} failed with HRESULT 0x{hr:X8}.", hr);
    }

    internal static void ReleaseComObject<T>(ref T? comObject)
        where T : class
    {
        if (comObject == null)
        {
            return;
        }

        try
        {
            if (Marshal.IsComObject(comObject))
            {
                Marshal.ReleaseComObject(comObject);
            }
        }
        catch
        {
            // Best-effort.
        }
        finally
        {
            comObject = null;
        }
    }

    internal static IAudioClient ActivateAudioClient(IMMDevice device, out IAudioClient3? audioClient3)
    {
        var iidAudioClient3 = typeof(IAudioClient3).GUID;
        var hr = device.Activate(ref iidAudioClient3, CLSCTX_ALL, IntPtr.Zero, out var client3Object);
        if (hr >= 0 && client3Object is IAudioClient3 client3)
        {
            audioClient3 = client3;
            return client3;
        }

        var iidAudioClient = typeof(IAudioClient).GUID;
        ThrowIfFailed(
            device.Activate(ref iidAudioClient, CLSCTX_ALL, IntPtr.Zero, out var clientObject),
            "IMMDevice.Activate(IAudioClient)");
        audioClient3 = clientObject as IAudioClient3;
        return (IAudioClient)clientObject;
    }

    internal static bool TryInitializeSharedStreamWithAudioClient3(IAudioClient3? audioClient3, IntPtr format)
    {
        if (audioClient3 == null)
        {
            return false;
        }

        var hr = audioClient3.GetSharedModeEnginePeriod(
            format,
            out var defaultPeriodInFrames,
            out _,
            out _,
            out _);
        if (hr < 0)
        {
            return false;
        }

        hr = audioClient3.InitializeSharedAudioStream(
            AUDCLNT_STREAMFLAGS_EVENTCALLBACK,
            defaultPeriodInFrames,
            format,
            IntPtr.Zero);
        return hr >= 0;
    }

    private static WasapiSampleType ResolveSampleType(int formatTag, Guid subFormat, int bitsPerSample)
    {
        if (formatTag == WAVE_FORMAT_IEEE_FLOAT ||
            (formatTag == WAVE_FORMAT_EXTENSIBLE && subFormat == KSDATAFORMAT_SUBTYPE_IEEE_FLOAT))
        {
            return bitsPerSample switch
            {
                32 => WasapiSampleType.Float32,
                64 => WasapiSampleType.Float64,
                _ => throw new InvalidOperationException($"Unsupported floating-point bit depth: {bitsPerSample}.")
            };
        }

        if (formatTag == WAVE_FORMAT_PCM ||
            (formatTag == WAVE_FORMAT_EXTENSIBLE && subFormat == KSDATAFORMAT_SUBTYPE_PCM))
        {
            return bitsPerSample switch
            {
                16 => WasapiSampleType.Pcm16,
                24 => WasapiSampleType.Pcm24,
                32 => WasapiSampleType.Pcm32,
                _ => throw new InvalidOperationException($"Unsupported PCM bit depth: {bitsPerSample}.")
            };
        }

        throw new InvalidOperationException(
            $"Unsupported WASAPI sample format: formatTag=0x{formatTag:X4}, subFormat={subFormat}, bits={bitsPerSample}.");
    }
}

internal enum EDataFlow
{
    eRender = 0,
    eCapture = 1,
    eAll = 2
}

internal enum ERole
{
    eConsole = 0,
    eMultimedia = 1,
    eCommunications = 2
}

internal enum WasapiSampleType
{
    Float32,
    Float64,
    Pcm16,
    Pcm24,
    Pcm32
}

internal readonly record struct WasapiAudioFormat(
    int SampleRate,
    int Channels,
    int BitsPerSample,
    int BlockAlign,
    WasapiSampleType SampleType)
{
    public int BytesPerSample => BlockAlign / Channels;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct WAVEFORMATEX
{
    public ushort wFormatTag;
    public ushort nChannels;
    public uint nSamplesPerSec;
    public uint nAvgBytesPerSec;
    public ushort nBlockAlign;
    public ushort wBitsPerSample;
    public ushort cbSize;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct WAVEFORMATEXTENSIBLE
{
    public WAVEFORMATEX Format;
    public ushort wValidBitsPerSample;
    public uint dwChannelMask;
    public Guid SubFormat;
}

[StructLayout(LayoutKind.Sequential)]
internal struct PROPERTYKEY
{
    public Guid fmtid;
    public uint pid;
}

[StructLayout(LayoutKind.Sequential)]
internal struct PropVariant : IDisposable
{
    private const ushort VT_LPWSTR = 31;

    public ushort vt;
    private ushort wReserved1;
    private ushort wReserved2;
    private ushort wReserved3;
    public IntPtr pValue;
    private IntPtr pValueReserved;

    [DllImport("ole32.dll")]
    private static extern int PropVariantClear(ref PropVariant pvar);

    public string? GetString()
    {
        if (vt == VT_LPWSTR && pValue != IntPtr.Zero)
        {
            return Marshal.PtrToStringUni(pValue);
        }

        return null;
    }

    public void Dispose()
    {
        _ = PropVariantClear(ref this);
    }
}

[ComImport]
[Guid("A95664D2-9614-4F35-A746-DE8DB63617E6")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IMMDeviceEnumerator
{
    [PreserveSig]
    int EnumAudioEndpoints(EDataFlow dataFlow, uint stateMask, out IMMDeviceCollection devices);

    [PreserveSig]
    int GetDefaultAudioEndpoint(EDataFlow dataFlow, ERole role, out IMMDevice endpoint);

    [PreserveSig]
    int GetDevice([MarshalAs(UnmanagedType.LPWStr)] string id, out IMMDevice endpoint);

    [PreserveSig]
    int RegisterEndpointNotificationCallback(IMMNotificationClient client);

    [PreserveSig]
    int UnregisterEndpointNotificationCallback(IMMNotificationClient client);
}

[ComImport]
[Guid("D666063F-1587-4E43-81F1-B948E807363F")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IMMDevice
{
    [PreserveSig]
    int Activate(
        ref Guid iid,
        uint clsCtx,
        IntPtr activationParams,
        [MarshalAs(UnmanagedType.IUnknown)] out object interfacePointer);

    [PreserveSig]
    int OpenPropertyStore(uint access, out IPropertyStore properties);

    [PreserveSig]
    int GetId([MarshalAs(UnmanagedType.LPWStr)] out string id);

    [PreserveSig]
    int GetState(out int state);
}

[ComImport]
[Guid("0BD7A1BE-7A1A-44DB-8397-CC5392387B5E")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IMMDeviceCollection
{
    [PreserveSig]
    int GetCount(out uint count);

    [PreserveSig]
    int Item(uint index, out IMMDevice device);
}

[ComImport]
[Guid("1CB9AD4C-DBFA-4C32-B178-C2F568A703B2")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IAudioClient
{
    [PreserveSig]
    int Initialize(
        int shareMode,
        uint streamFlags,
        long bufferDuration,
        long periodicity,
        IntPtr format,
        IntPtr audioSessionGuid);

    [PreserveSig]
    int GetBufferSize(out uint bufferFrameCount);

    [PreserveSig]
    int GetStreamLatency(out long latency);

    [PreserveSig]
    int GetCurrentPadding(out uint paddingFrameCount);

    [PreserveSig]
    int IsFormatSupported(int shareMode, IntPtr format, out IntPtr closestMatch);

    [PreserveSig]
    int GetMixFormat(out IntPtr format);

    [PreserveSig]
    int GetDevicePeriod(out long defaultPeriod, out long minimumPeriod);

    [PreserveSig]
    int Start();

    [PreserveSig]
    int Stop();

    [PreserveSig]
    int Reset();

    [PreserveSig]
    int SetEventHandle(IntPtr eventHandle);

    [PreserveSig]
    int GetService(ref Guid riid, [MarshalAs(UnmanagedType.IUnknown)] out object service);
}

[ComImport]
[Guid("7ED4EE07-8E67-4CD4-8C1A-2B7A5987AD42")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IAudioClient3 : IAudioClient
{
    [PreserveSig]
    new int Initialize(
        int shareMode,
        uint streamFlags,
        long bufferDuration,
        long periodicity,
        IntPtr format,
        IntPtr audioSessionGuid);

    [PreserveSig]
    new int GetBufferSize(out uint bufferFrameCount);

    [PreserveSig]
    new int GetStreamLatency(out long latency);

    [PreserveSig]
    new int GetCurrentPadding(out uint paddingFrameCount);

    [PreserveSig]
    new int IsFormatSupported(int shareMode, IntPtr format, out IntPtr closestMatch);

    [PreserveSig]
    new int GetMixFormat(out IntPtr format);

    [PreserveSig]
    new int GetDevicePeriod(out long defaultPeriod, out long minimumPeriod);

    [PreserveSig]
    new int Start();

    [PreserveSig]
    new int Stop();

    [PreserveSig]
    new int Reset();

    [PreserveSig]
    new int SetEventHandle(IntPtr eventHandle);

    [PreserveSig]
    new int GetService(ref Guid riid, [MarshalAs(UnmanagedType.IUnknown)] out object service);

    [PreserveSig]
    int GetSharedModeEnginePeriod(
        IntPtr format,
        out uint defaultPeriodInFrames,
        out uint fundamentalPeriodInFrames,
        out uint minPeriodInFrames,
        out uint maxPeriodInFrames);

    [PreserveSig]
    int GetCurrentSharedModeEnginePeriod(out IntPtr format, out uint currentPeriodInFrames);

    [PreserveSig]
    int InitializeSharedAudioStream(
        uint streamFlags,
        uint periodInFrames,
        IntPtr format,
        IntPtr audioSessionGuid);
}

[ComImport]
[Guid("C8ADBD64-E71E-48A0-A4DE-185C395CD317")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IAudioCaptureClient
{
    [PreserveSig]
    int GetBuffer(
        out IntPtr data,
        out uint numFramesAvailable,
        out uint flags,
        out ulong devicePosition,
        out ulong qpcPosition);

    [PreserveSig]
    int ReleaseBuffer(uint numFramesRead);

    [PreserveSig]
    int GetNextPacketSize(out uint numFramesInNextPacket);
}

[ComImport]
[Guid("F294ACFC-3146-4483-A7BF-ADDCA7C260E2")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IAudioRenderClient
{
    [PreserveSig]
    int GetBuffer(uint numFramesRequested, out IntPtr data);

    [PreserveSig]
    int ReleaseBuffer(uint numFramesWritten, uint flags);
}

[ComImport]
[Guid("886d8eeb-8cf2-4446-8d02-cdba1dbdcf99")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IPropertyStore
{
    [PreserveSig]
    int GetCount(out uint cProps);

    [PreserveSig]
    int GetAt(uint iProp, out PROPERTYKEY pkey);

    [PreserveSig]
    int GetValue(ref PROPERTYKEY key, out PropVariant pv);

    [PreserveSig]
    int SetValue(ref PROPERTYKEY key, ref PropVariant propvar);

    [PreserveSig]
    int Commit();
}

[ComImport]
[Guid("7991EEC9-7E89-4D85-8390-6C703CEC60C0")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IMMNotificationClient
{
    [PreserveSig]
    int OnDeviceStateChanged(
        [MarshalAs(UnmanagedType.LPWStr)] string deviceId,
        uint newState);

    [PreserveSig]
    int OnDeviceAdded(
        [MarshalAs(UnmanagedType.LPWStr)] string deviceId);

    [PreserveSig]
    int OnDeviceRemoved(
        [MarshalAs(UnmanagedType.LPWStr)] string deviceId);

    [PreserveSig]
    int OnDefaultDeviceChanged(
        EDataFlow flow,
        ERole role,
        [MarshalAs(UnmanagedType.LPWStr)] string? defaultDeviceId);

    [PreserveSig]
    int OnPropertyValueChanged(
        [MarshalAs(UnmanagedType.LPWStr)] string deviceId,
        PROPERTYKEY key);
}


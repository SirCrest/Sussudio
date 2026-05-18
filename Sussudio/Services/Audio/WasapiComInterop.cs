using System;
using System.Runtime.InteropServices;

namespace Sussudio.Services.Audio;

// Minimal WASAPI/Core Audio interop surface used by capture, playback, and
// device watching. Keeping the COM declarations centralized avoids subtle ABI
// drift between the low-latency audio feature blocks.
internal static partial class WasapiComInterop
{
    internal const uint CLSCTX_ALL = 0x17;
    internal const uint DEVICE_STATE_ACTIVE = 0x00000001;
    internal const uint STGM_READ = 0;
    internal const int AUDCLNT_SHAREMODE_SHARED = 0;
    internal const int AUDCLNT_SHAREMODE_EXCLUSIVE = 1;
    internal const uint AUDCLNT_STREAMFLAGS_EVENTCALLBACK = 0x00040000;
    internal const uint AUDCLNT_STREAMFLAGS_LOOPBACK = 0x00020000;
    internal const uint AUDCLNT_BUFFERFLAGS_DATA_DISCONTINUITY = 0x00000001;
    internal const uint AUDCLNT_BUFFERFLAGS_SILENT = 0x00000002;
    internal const uint AUDCLNT_BUFFERFLAGS_TIMESTAMP_ERROR = 0x00000004;

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
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceWarning($"Suppressed exception in WasapiComInterop.ReleaseComObject<T>: {ex.Message}");
        }
        finally
        {
            comObject = null;
        }
    }

    internal static void ReleaseComObjectSafe(object? obj)
    {
        if (obj == null)
        {
            return;
        }

        try
        {
            if (Marshal.IsComObject(obj))
            {
                Marshal.ReleaseComObject(obj);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceWarning($"Suppressed exception in WasapiComInterop.SafeReleaseComObject: {ex.Message}");
        }
    }

}

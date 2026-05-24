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

    internal static float GetEndpointVolume(string deviceId)
    {
        IMMDeviceEnumerator? enumerator = null;
        IMMDevice? device = null;
        try
        {
            enumerator = CreateDeviceEnumerator();
            var hr = enumerator.GetDevice(deviceId, out device);
            if (hr < 0 || device == null)
            {
                return 1.0f;
            }

            var iid = typeof(IAudioEndpointVolume).GUID;
            hr = device.Activate(ref iid, CLSCTX_ALL, IntPtr.Zero, out var obj);
            if (hr < 0 || obj is not IAudioEndpointVolume volume)
            {
                if (obj != null) ReleaseComObjectSafe(obj);
                return 1.0f;
            }

            try
            {
                hr = volume.GetMasterVolumeLevelScalar(out var level);
                return hr >= 0 ? Math.Clamp(level, 0f, 1f) : 1.0f;
            }
            finally
            {
                ReleaseComObjectSafe(obj);
            }
        }
        finally
        {
            ReleaseComObject(ref device);
            ReleaseComObject(ref enumerator);
        }
    }

    internal static void SetEndpointVolume(string deviceId, float level)
    {
        IMMDeviceEnumerator? enumerator = null;
        IMMDevice? device = null;
        try
        {
            enumerator = CreateDeviceEnumerator();
            var hr = enumerator.GetDevice(deviceId, out device);
            if (hr < 0 || device == null)
            {
                return;
            }

            var iid = typeof(IAudioEndpointVolume).GUID;
            hr = device.Activate(ref iid, CLSCTX_ALL, IntPtr.Zero, out var obj);
            if (hr < 0 || obj is not IAudioEndpointVolume volume)
            {
                if (obj != null) ReleaseComObjectSafe(obj);
                return;
            }

            try
            {
                _ = volume.SetMasterVolumeLevelScalar(Math.Clamp(level, 0f, 1f), Guid.Empty);
            }
            finally
            {
                ReleaseComObjectSafe(obj);
            }
        }
        finally
        {
            ReleaseComObject(ref device);
            ReleaseComObject(ref enumerator);
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
}

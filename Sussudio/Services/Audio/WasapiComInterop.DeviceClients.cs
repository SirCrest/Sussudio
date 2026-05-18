using System;

namespace Sussudio.Services.Audio;

internal static partial class WasapiComInterop
{
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

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.Services.Audio;

namespace Sussudio.Services.Capture;

internal static partial class MfDeviceEnumerator
{
    public static Task<List<AudioInputDevice>> EnumerateAudioCaptureEndpointsAsync()
    {
        var devices = new List<AudioInputDevice>();
        IMMDeviceEnumerator? enumerator = null;
        IMMDeviceCollection? collection = null;
        try
        {
            enumerator = WasapiComInterop.CreateDeviceEnumerator();
            var hrEnum = enumerator.EnumAudioEndpoints(
                EDataFlow.eCapture,
                WasapiComInterop.DEVICE_STATE_ACTIVE,
                out collection);
            if (hrEnum < 0 || collection == null)
            {
                Logger.Log($"WASAPI capture endpoint enumeration failed (hr=0x{hrEnum:X8}).");
                return Task.FromResult(devices);
            }

            WasapiComInterop.ThrowIfFailed(
                collection.GetCount(out var count),
                "IMMDeviceCollection.GetCount(audio_capture)");

            for (uint i = 0; i < count; i++)
            {
                IMMDevice? endpoint = null;
                try
                {
                    var hrItem = collection.Item(i, out endpoint);
                    if (hrItem < 0 || endpoint == null)
                    {
                        continue;
                    }

                    var hrId = endpoint.GetId(out var endpointId);
                    if (hrId < 0 || string.IsNullOrWhiteSpace(endpointId))
                    {
                        continue;
                    }

                    var friendlyName = ReadAudioEndpointFriendlyName(endpoint, endpointId);
                    devices.Add(new AudioInputDevice
                    {
                        Id = endpointId,
                        Name = friendlyName
                    });
                }
                finally
                {
                    WasapiComInterop.ReleaseComObject(ref endpoint);
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Log($"WASAPI capture endpoint enumeration threw: {ex.Message}");
            devices.Clear();
        }
        finally
        {
            WasapiComInterop.ReleaseComObject(ref collection);
            WasapiComInterop.ReleaseComObject(ref enumerator);
        }

        return Task.FromResult(devices);
    }

    private static string ReadAudioEndpointFriendlyName(IMMDevice endpoint, string fallbackName)
    {
        IPropertyStore? properties = null;
        try
        {
            var hrOpen = endpoint.OpenPropertyStore(WasapiComInterop.STGM_READ, out properties);
            if (hrOpen < 0 || properties == null)
            {
                return fallbackName;
            }

            var key = WasapiComInterop.PKEY_Device_FriendlyName;
            var hrValue = properties.GetValue(ref key, out var value);
            if (hrValue < 0)
            {
                return fallbackName;
            }

            using (value)
            {
                var friendlyName = value.GetString();
                return string.IsNullOrWhiteSpace(friendlyName)
                    ? fallbackName
                    : friendlyName.Trim();
            }
        }
        catch
        {
            return fallbackName;
        }
        finally
        {
            WasapiComInterop.ReleaseComObject(ref properties);
        }
    }
}

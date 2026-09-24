using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.Services.Audio;

namespace Sussudio.Services.Capture;

// Thin Media Foundation device/format enumerator. It owns MFStartup/MFShutdown
// ref-counting and exposes only the capture-device data the managed app needs.
internal static class MfDeviceEnumerator
{
    internal sealed record MfVideoDeviceInfo(string Name, string SymbolicLink);

    public static Task<List<MfVideoDeviceInfo>> EnumerateVideoDevicesAsync()
    {
        var devices = new List<MfVideoDeviceInfo>();
        MfInteropHelpers.AddStartupReference();
        try
        {
            IMFAttributes? attributes = null;
            IntPtr activateArray = IntPtr.Zero;
            try
            {
                MfInteropHelpers.ThrowIfFailed(MFCreateAttributes(out attributes, 1), "MFCreateAttributes(video_enum)");
                MfInteropHelpers.ThrowIfFailed(
                    attributes.SetGUID(
                        ref MfGuids.MF_DEVSOURCE_ATTRIBUTE_SOURCE_TYPE,
                        ref MfGuids.MF_DEVSOURCE_ATTRIBUTE_SOURCE_TYPE_VIDCAP_GUID),
                    "IMFAttributes.SetGUID(MF_DEVSOURCE_ATTRIBUTE_SOURCE_TYPE)");
                MfInteropHelpers.ThrowIfFailed(
                    MFEnumDeviceSources(attributes, out activateArray, out var activateCount),
                    "MFEnumDeviceSources(video_enum)");

                for (var i = 0; i < activateCount; i++)
                {
                    var activatePtr = Marshal.ReadIntPtr(activateArray, i * IntPtr.Size);
                    if (activatePtr == IntPtr.Zero)
                    {
                        continue;
                    }

                    IMFActivate? activate = null;
                    var rawReleased = false;
                    try
                    {
                        activate = (IMFActivate)Marshal.GetObjectForIUnknown(activatePtr);
                        _ = Marshal.Release(activatePtr);
                        rawReleased = true;

                        MfInteropHelpers.TryReadAllocatedString(
                            activate,
                            ref MfGuids.MF_DEVSOURCE_ATTRIBUTE_FRIENDLY_NAME,
                            out var friendlyName);
                        MfInteropHelpers.TryReadAllocatedString(
                            activate,
                            ref MfGuids.MF_DEVSOURCE_ATTRIBUTE_SOURCE_TYPE_VIDCAP_SYMBOLIC_LINK,
                            out var symbolicLink);
                        if (string.IsNullOrWhiteSpace(symbolicLink))
                        {
                            continue;
                        }

                        var displayName = string.IsNullOrWhiteSpace(friendlyName)
                            ? symbolicLink
                            : friendlyName.Trim();
                        devices.Add(new MfVideoDeviceInfo(displayName, symbolicLink));
                    }
                    catch (Exception ex)
                    {
                        Logger.Log($"MF video device row failed at index {i}: {ex.Message}");
                    }
                    finally
                    {
                        if (!rawReleased)
                        {
                            try
                            {
                                _ = Marshal.Release(activatePtr);
                            }
                            catch (Exception releaseEx)
                            {
                                // The activation leaks either way; record why.
                                Logger.Log(
                                    $"MF_ACTIVATE_RELEASE_FAIL stage=enumeration_cleanup type={releaseEx.GetType().Name} " +
                                    $"hr=0x{releaseEx.HResult:X8} msg='{releaseEx.Message}'");
                            }
                        }

                        MfInteropHelpers.ReleaseComObject(ref activate);
                    }
                }
            }
            finally
            {
                if (activateArray != IntPtr.Zero)
                {
                    Marshal.FreeCoTaskMem(activateArray);
                }

                MfInteropHelpers.ReleaseComObject(ref attributes);
            }
        }
        catch (Exception ex)
        {
            Logger.Log($"MF video device enumeration failed: {ex.Message}");
            throw;
        }
        finally
        {
            MfInteropHelpers.ReleaseStartupReference();
        }

        return Task.FromResult(devices);
    }

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
            WasapiComInterop.ThrowIfFailed(hrEnum, "IMMDeviceEnumerator.EnumAudioEndpoints(audio_capture)");
            if (collection == null)
            {
                throw new InvalidOperationException("WASAPI capture endpoint enumeration returned no collection.");
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
                    MfInteropHelpers.ReleaseComObject(ref endpoint);
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Log($"WASAPI capture endpoint enumeration threw: {ex.Message}");
            throw;
        }
        finally
        {
            MfInteropHelpers.ReleaseComObject(ref collection);
            MfInteropHelpers.ReleaseComObject(ref enumerator);
        }

        return Task.FromResult(devices);
    }

    public static Task<List<MediaFormat>> ProbeVideoFormatsAsync(string symbolicLink)
    {
        var formats = new List<MediaFormat>();
        if (string.IsNullOrWhiteSpace(symbolicLink))
        {
            return Task.FromResult(formats);
        }

        MfInteropHelpers.AddStartupReference();
        IMFMediaSource? mediaSource = null;
        IMFAttributes? readerAttributes = null;
        IMFSourceReader? sourceReader = null;
        try
        {
            mediaSource = CreateMediaSource(symbolicLink);

            MfInteropHelpers.ThrowIfFailed(MFCreateAttributes(out readerAttributes, 1), "MFCreateAttributes(format_probe)");
            MfInteropHelpers.ThrowIfFailed(
                readerAttributes.SetUINT32(ref MfGuids.MF_READWRITE_DISABLE_CONVERTERS, 1),
                "IMFAttributes.SetUINT32(MF_READWRITE_DISABLE_CONVERTERS)");
            MfInteropHelpers.ThrowIfFailed(
                MFCreateSourceReaderFromMediaSource(mediaSource, readerAttributes, out sourceReader),
                "MFCreateSourceReaderFromMediaSource(format_probe)");

            for (var mediaTypeIndex = 0; ; mediaTypeIndex++)
            {
                IMFMediaType? mediaType = null;
                try
                {
                    var hr = sourceReader.GetNativeMediaType(
                        MfConstants.MF_SOURCE_READER_FIRST_VIDEO_STREAM,
                        mediaTypeIndex,
                        out mediaType);
                    if (hr == MfHResults.MF_E_NO_MORE_TYPES)
                    {
                        break;
                    }

                    MfInteropHelpers.ThrowIfFailed(hr, $"IMFSourceReader.GetNativeMediaType(index={mediaTypeIndex})");
                    if (mediaType == null)
                    {
                        continue;
                    }

                    if (!MfInteropHelpers.TryGetUInt64(mediaType, ref MfGuids.MF_MT_FRAME_SIZE, out var packedFrameSize))
                    {
                        continue;
                    }

                    var width = (uint)(packedFrameSize >> 32);
                    var height = (uint)(packedFrameSize & 0xFFFFFFFFu);
                    if (width == 0 || height == 0)
                    {
                        continue;
                    }

                    if (!MfInteropHelpers.TryGetGuid(mediaType, ref MfGuids.MF_MT_SUBTYPE, out var subtype))
                    {
                        continue;
                    }

                    uint frameRateNumerator = 0;
                    uint frameRateDenominator = 0;
                    if (MfInteropHelpers.TryGetUInt64(mediaType, ref MfGuids.MF_MT_FRAME_RATE, out var packedFrameRate))
                    {
                        frameRateNumerator = (uint)(packedFrameRate >> 32);
                        frameRateDenominator = (uint)(packedFrameRate & 0xFFFFFFFFu);
                    }

                    var frameRate = frameRateDenominator > 0
                        ? (double)frameRateNumerator / frameRateDenominator
                        : 0d;
                    var pixelFormat = SubtypeGuidToName(subtype);
                    var isHdr = MediaFormat.IsHdrPixelFormat(pixelFormat) || MediaFormat.IsTrue10BitPixelFormat(pixelFormat);
                    formats.Add(new MediaFormat
                    {
                        Width = width,
                        Height = height,
                        FrameRate = frameRate,
                        FrameRateNumerator = frameRateNumerator,
                        FrameRateDenominator = frameRateDenominator,
                        PixelFormat = pixelFormat,
                        IsHdr = isHdr
                    });
                }
                finally
                {
                    MfInteropHelpers.ReleaseComObject(ref mediaType);
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Log($"MF format probe failed for {symbolicLink}: {ex.Message}");
            throw;
        }
        finally
        {
            MfInteropHelpers.ReleaseComObject(ref sourceReader);
            MfInteropHelpers.ReleaseComObject(ref readerAttributes);
            MfInteropHelpers.ReleaseComObject(ref mediaSource);
            MfInteropHelpers.ReleaseStartupReference();
        }

        return Task.FromResult(formats);
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
            MfInteropHelpers.ReleaseComObject(ref properties);
        }
    }

    private static string SubtypeGuidToName(Guid subtype)
        => MfInteropHelpers.SubtypeGuidToName(subtype);

    private static IMFMediaSource CreateMediaSource(string symbolicLink)
    {
        IMFAttributes? attributes = null;
        try
        {
            MfInteropHelpers.ThrowIfFailed(MFCreateAttributes(out attributes, 2), "MFCreateAttributes(device_source)");
            MfInteropHelpers.ThrowIfFailed(
                attributes.SetGUID(
                    ref MfGuids.MF_DEVSOURCE_ATTRIBUTE_SOURCE_TYPE,
                    ref MfGuids.MF_DEVSOURCE_ATTRIBUTE_SOURCE_TYPE_VIDCAP_GUID),
                "IMFAttributes.SetGUID(MF_DEVSOURCE_ATTRIBUTE_SOURCE_TYPE)");
            MfInteropHelpers.ThrowIfFailed(
                attributes.SetString(
                    ref MfGuids.MF_DEVSOURCE_ATTRIBUTE_SOURCE_TYPE_VIDCAP_SYMBOLIC_LINK,
                    symbolicLink),
                "IMFAttributes.SetString(MF_DEVSOURCE_ATTRIBUTE_SOURCE_TYPE_VIDCAP_SYMBOLIC_LINK)");

            var directHr = MFCreateDeviceSource(attributes, out var mediaSource);
            if (directHr >= 0 && mediaSource != null)
            {
                return mediaSource;
            }

            return CreateMediaSourceByEnumeration(symbolicLink, directHr);
        }
        finally
        {
            MfInteropHelpers.ReleaseComObject(ref attributes);
        }
    }

    private static IMFMediaSource CreateMediaSourceByEnumeration(string targetSymbolicLink, int directHr)
    {
        IMFAttributes? attributes = null;
        IntPtr activateArray = IntPtr.Zero;
        try
        {
            MfInteropHelpers.ThrowIfFailed(MFCreateAttributes(out attributes, 1), "MFCreateAttributes(device_enum_fallback)");
            MfInteropHelpers.ThrowIfFailed(
                attributes.SetGUID(
                    ref MfGuids.MF_DEVSOURCE_ATTRIBUTE_SOURCE_TYPE,
                    ref MfGuids.MF_DEVSOURCE_ATTRIBUTE_SOURCE_TYPE_VIDCAP_GUID),
                "IMFAttributes.SetGUID(MF_DEVSOURCE_ATTRIBUTE_SOURCE_TYPE)");
            MfInteropHelpers.ThrowIfFailed(
                MFEnumDeviceSources(attributes, out activateArray, out var activateCount),
                "MFEnumDeviceSources(device_enum_fallback)");

            for (var i = 0; i < activateCount; i++)
            {
                var activatePtr = Marshal.ReadIntPtr(activateArray, i * IntPtr.Size);
                if (activatePtr == IntPtr.Zero)
                {
                    continue;
                }

                IMFActivate? activate = null;
                var rawReleased = false;
                try
                {
                    activate = (IMFActivate)Marshal.GetObjectForIUnknown(activatePtr);
                    _ = Marshal.Release(activatePtr);
                    rawReleased = true;

                    MfInteropHelpers.TryReadAllocatedString(
                        activate,
                        ref MfGuids.MF_DEVSOURCE_ATTRIBUTE_SOURCE_TYPE_VIDCAP_SYMBOLIC_LINK,
                        out var candidateLink);
                    if (!MfInteropHelpers.MatchesSymbolicLink(targetSymbolicLink, candidateLink))
                    {
                        continue;
                    }

                    var mediaSourceIid = typeof(IMFMediaSource).GUID;
                    MfInteropHelpers.ThrowIfFailed(
                        activate.ActivateObject(ref mediaSourceIid, out var activatedObject),
                        "IMFActivate.ActivateObject(IMFMediaSource)");
                    if (activatedObject is IMFMediaSource mediaSource)
                    {
                        // Release remaining activate objects that we won't visit.
                        for (var j = i + 1; j < activateCount; j++)
                        {
                            var remainingPtr = Marshal.ReadIntPtr(activateArray, j * IntPtr.Size);
                            if (remainingPtr != IntPtr.Zero)
                            {
                                try { Marshal.Release(remainingPtr); }
                                catch (Exception releaseEx)
                                {
                                    Logger.Log(
                                        $"MF_ACTIVATE_RELEASE_FAIL stage=skip_remaining index={j} " +
                                        $"type={releaseEx.GetType().Name} hr=0x{releaseEx.HResult:X8}");
                                }
                            }
                        }

                        return mediaSource;
                    }

                    if (activatedObject != null && Marshal.IsComObject(activatedObject))
                    {
                        _ = Marshal.ReleaseComObject(activatedObject);
                    }
                }
                finally
                {
                    if (!rawReleased)
                    {
                        try
                        {
                            _ = Marshal.Release(activatePtr);
                        }
                        catch (Exception releaseEx)
                        {
                            // The activation leaks either way; record why.
                            Logger.Log(
                                $"MF_ACTIVATE_RELEASE_FAIL stage=source_cleanup type={releaseEx.GetType().Name} " +
                                $"hr=0x{releaseEx.HResult:X8} msg='{releaseEx.Message}'");
                        }
                    }

                    MfInteropHelpers.ReleaseComObject(ref activate);
                }
            }
        }
        finally
        {
            if (activateArray != IntPtr.Zero)
            {
                Marshal.FreeCoTaskMem(activateArray);
            }

            MfInteropHelpers.ReleaseComObject(ref attributes);
        }

        throw new InvalidOperationException(
            $"Unable to open MF video source by symbolic link. requested='{targetSymbolicLink}' direct_hr=0x{directHr:X8}");
    }

    [DllImport("mfplat.dll", ExactSpelling = true)]
    private static extern int MFCreateAttributes(
        [MarshalAs(UnmanagedType.Interface)] out IMFAttributes ppMFAttributes,
        int cInitialSize);

    [DllImport("mf.dll", ExactSpelling = true)]
    private static extern int MFEnumDeviceSources(
        [MarshalAs(UnmanagedType.Interface)] IMFAttributes pAttributes,
        out IntPtr pppSourceActivate,
        out int pcSourceActivate);

    [DllImport("mf.dll", ExactSpelling = true)]
    private static extern int MFCreateDeviceSource(
        [MarshalAs(UnmanagedType.Interface)] IMFAttributes pAttributes,
        [MarshalAs(UnmanagedType.Interface)] out IMFMediaSource ppSource);

    [DllImport("mfreadwrite.dll", ExactSpelling = true)]
    private static extern int MFCreateSourceReaderFromMediaSource(
        [MarshalAs(UnmanagedType.Interface)] IMFMediaSource pMediaSource,
        [MarshalAs(UnmanagedType.Interface)] IMFAttributes? pAttributes,
        [MarshalAs(UnmanagedType.Interface)] out IMFSourceReader ppSourceReader);

}

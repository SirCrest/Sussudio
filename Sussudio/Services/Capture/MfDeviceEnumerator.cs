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
    private const int MfVersion = 0x00020070;
    private const int MfSourceReaderFirstVideoStream = unchecked((int)0xFFFFFFFC);
    private const int MfENoMoreTypes = unchecked((int)0xC00D36B9);
    private const int MfEAttributeNotFound = unchecked((int)0xC00D36E6);

    private static readonly object StartupSync = new();
    private static int _startupRefCount;

    private static Guid DevSourceAttributeSourceType = new(
        0xC60AC5FE, 0x252A, 0x478F, 0xA0, 0xEF, 0xBC, 0x8F, 0xA5, 0xF7, 0xCA, 0xD3);
    private static Guid DevSourceAttributeSourceTypeVidcapGuid = new(
        0x8AC3587A, 0x4AE7, 0x42D8, 0x99, 0xE0, 0x0A, 0x60, 0x13, 0xEE, 0xF9, 0x0F);
    private static Guid DevSourceAttributeFriendlyName = new(
        0x60D0E559, 0x52F8, 0x4FA2, 0xBB, 0xCE, 0xAC, 0xDB, 0x34, 0xA8, 0xEC, 0x01);
    private static Guid DevSourceAttributeSourceTypeVidcapSymbolicLink = new(
        0x58F0AAD8, 0x22BF, 0x4F8A, 0xBB, 0x3D, 0xD2, 0xC4, 0x97, 0x8C, 0x6E, 0x2F);
    private static Guid MfReadwriteDisableConverters = new(
        0x98D5B065, 0x1374, 0x4847, 0x8D, 0x5D, 0x31, 0x52, 0x0F, 0xEE, 0x71, 0x56);
    private static Guid MfMtSubtype = new(
        0xF7E34C9A, 0x42E8, 0x4714, 0xB7, 0x4B, 0xCB, 0x29, 0xD7, 0x2C, 0x35, 0xE5);
    private static Guid MfMtFrameSize = new(
        0x1652C33D, 0xD6B2, 0x4012, 0xB8, 0x34, 0x72, 0x03, 0x08, 0x49, 0xA3, 0x7D);
    private static Guid MfMtFrameRate = new(
        0xC459A2E8, 0x3D2C, 0x4E44, 0xB1, 0x32, 0xFE, 0xE5, 0x15, 0x6C, 0x7B, 0xB0);
    private static Guid MfVideoFormatP010 = new(
        0x30313050, 0x0000, 0x0010, 0x80, 0x00, 0x00, 0xAA, 0x00, 0x38, 0x9B, 0x71);
    private static Guid MfVideoFormatNv12 = new(
        0x3231564E, 0x0000, 0x0010, 0x80, 0x00, 0x00, 0xAA, 0x00, 0x38, 0x9B, 0x71);
    private static Guid MfVideoFormatYuy2 = new(
        0x32595559, 0x0000, 0x0010, 0x80, 0x00, 0x00, 0xAA, 0x00, 0x38, 0x9B, 0x71);
    private static Guid MfVideoFormatUyvy = new(
        0x59565955, 0x0000, 0x0010, 0x80, 0x00, 0x00, 0xAA, 0x00, 0x38, 0x9B, 0x71);
    private static Guid MfVideoFormatMjpg = new(
        0x47504A4D, 0x0000, 0x0010, 0x80, 0x00, 0x00, 0xAA, 0x00, 0x38, 0x9B, 0x71);

    internal sealed record MfVideoDeviceInfo(string Name, string SymbolicLink);

    public static Task<List<MfVideoDeviceInfo>> EnumerateVideoDevicesAsync()
    {
        var devices = new List<MfVideoDeviceInfo>();
        AddStartupReference();
        try
        {
            IMFAttributes? attributes = null;
            IntPtr activateArray = IntPtr.Zero;
            try
            {
                ThrowIfFailed(MFCreateAttributes(out attributes, 1), "MFCreateAttributes(video_enum)");
                ThrowIfFailed(
                    attributes.SetGUID(ref DevSourceAttributeSourceType, ref DevSourceAttributeSourceTypeVidcapGuid),
                    "IMFAttributes.SetGUID(MF_DEVSOURCE_ATTRIBUTE_SOURCE_TYPE)");
                ThrowIfFailed(
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

                        var friendlyName = ReadAllocatedString(activate, ref DevSourceAttributeFriendlyName);
                        var symbolicLink = ReadAllocatedString(activate, ref DevSourceAttributeSourceTypeVidcapSymbolicLink);
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
                            catch
                            {
                                // Best effort.
                            }
                        }

                        WasapiComInterop.ReleaseComObject(ref activate);
                    }
                }
            }
            finally
            {
                if (activateArray != IntPtr.Zero)
                {
                    Marshal.FreeCoTaskMem(activateArray);
                }

                WasapiComInterop.ReleaseComObject(ref attributes);
            }
        }
        catch (Exception ex)
        {
            Logger.Log($"MF video device enumeration failed: {ex.Message}");
            devices.Clear();
        }
        finally
        {
            ReleaseStartupReference();
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

    public static Task<List<MediaFormat>> ProbeVideoFormatsAsync(string symbolicLink)
    {
        var formats = new List<MediaFormat>();
        if (string.IsNullOrWhiteSpace(symbolicLink))
        {
            return Task.FromResult(formats);
        }

        AddStartupReference();
        IMFMediaSource? mediaSource = null;
        IMFAttributes? readerAttributes = null;
        IMFSourceReader? sourceReader = null;
        try
        {
            mediaSource = CreateMediaSource(symbolicLink);

            ThrowIfFailed(MFCreateAttributes(out readerAttributes, 1), "MFCreateAttributes(format_probe)");
            ThrowIfFailed(
                readerAttributes.SetUINT32(ref MfReadwriteDisableConverters, 1),
                "IMFAttributes.SetUINT32(MF_READWRITE_DISABLE_CONVERTERS)");
            ThrowIfFailed(
                MFCreateSourceReaderFromMediaSource(mediaSource, readerAttributes, out sourceReader),
                "MFCreateSourceReaderFromMediaSource(format_probe)");

            for (var mediaTypeIndex = 0; ; mediaTypeIndex++)
            {
                IMFMediaType? mediaType = null;
                try
                {
                    var hr = sourceReader.GetNativeMediaType(
                        MfSourceReaderFirstVideoStream,
                        mediaTypeIndex,
                        out mediaType);
                    if (hr == MfENoMoreTypes)
                    {
                        break;
                    }

                    ThrowIfFailed(hr, $"IMFSourceReader.GetNativeMediaType(index={mediaTypeIndex})");
                    if (mediaType == null)
                    {
                        continue;
                    }

                    if (!TryGetUInt64(mediaType, ref MfMtFrameSize, out var packedFrameSize))
                    {
                        continue;
                    }

                    var width = (uint)(packedFrameSize >> 32);
                    var height = (uint)(packedFrameSize & 0xFFFFFFFFu);
                    if (width == 0 || height == 0)
                    {
                        continue;
                    }

                    if (!TryGetGuid(mediaType, ref MfMtSubtype, out var subtype))
                    {
                        continue;
                    }

                    uint frameRateNumerator = 0;
                    uint frameRateDenominator = 0;
                    if (TryGetUInt64(mediaType, ref MfMtFrameRate, out var packedFrameRate))
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
                    WasapiComInterop.ReleaseComObject(ref mediaType);
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Log($"MF format probe failed for {symbolicLink}: {ex.Message}");
            formats.Clear();
        }
        finally
        {
            WasapiComInterop.ReleaseComObject(ref sourceReader);
            WasapiComInterop.ReleaseComObject(ref readerAttributes);
            WasapiComInterop.ReleaseComObject(ref mediaSource);
            ReleaseStartupReference();
        }

        return Task.FromResult(formats);
    }

    [DllImport("mfplat.dll", ExactSpelling = true)]
    private static extern int MFStartup(int version, int dwFlags);

    [DllImport("mfplat.dll", ExactSpelling = true)]
    private static extern int MFShutdown();

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

    private static IMFMediaSource CreateMediaSource(string symbolicLink)
    {
        IMFAttributes? attributes = null;
        try
        {
            ThrowIfFailed(MFCreateAttributes(out attributes, 2), "MFCreateAttributes(device_source)");
            ThrowIfFailed(
                attributes.SetGUID(ref DevSourceAttributeSourceType, ref DevSourceAttributeSourceTypeVidcapGuid),
                "IMFAttributes.SetGUID(MF_DEVSOURCE_ATTRIBUTE_SOURCE_TYPE)");
            ThrowIfFailed(
                attributes.SetString(ref DevSourceAttributeSourceTypeVidcapSymbolicLink, symbolicLink),
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
            WasapiComInterop.ReleaseComObject(ref attributes);
        }
    }

    private static IMFMediaSource CreateMediaSourceByEnumeration(string targetSymbolicLink, int directHr)
    {
        IMFAttributes? attributes = null;
        IntPtr activateArray = IntPtr.Zero;
        try
        {
            ThrowIfFailed(MFCreateAttributes(out attributes, 1), "MFCreateAttributes(device_enum_fallback)");
            ThrowIfFailed(
                attributes.SetGUID(ref DevSourceAttributeSourceType, ref DevSourceAttributeSourceTypeVidcapGuid),
                "IMFAttributes.SetGUID(MF_DEVSOURCE_ATTRIBUTE_SOURCE_TYPE)");
            ThrowIfFailed(
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

                    var candidateLink = ReadAllocatedString(activate, ref DevSourceAttributeSourceTypeVidcapSymbolicLink);
                    if (!SymbolicLinksMatch(targetSymbolicLink, candidateLink))
                    {
                        continue;
                    }

                    var mediaSourceIid = typeof(IMFMediaSource).GUID;
                    ThrowIfFailed(
                        activate.ActivateObject(ref mediaSourceIid, out var activatedObject),
                        "IMFActivate.ActivateObject(IMFMediaSource)");
                    if (activatedObject is IMFMediaSource mediaSource)
                    {
                        // Release remaining activate objects that we won't visit
                        for (var j = i + 1; j < activateCount; j++)
                        {
                            var remainingPtr = Marshal.ReadIntPtr(activateArray, j * IntPtr.Size);
                            if (remainingPtr != IntPtr.Zero)
                            {
                                try { Marshal.Release(remainingPtr); }
                                catch { /* Best effort. */ }
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
                        catch
                        {
                            // Best effort.
                        }
                    }

                    WasapiComInterop.ReleaseComObject(ref activate);
                }
            }
        }
        finally
        {
            if (activateArray != IntPtr.Zero)
            {
                Marshal.FreeCoTaskMem(activateArray);
            }

            WasapiComInterop.ReleaseComObject(ref attributes);
        }

        throw new InvalidOperationException(
            $"Unable to open MF video source by symbolic link. requested='{targetSymbolicLink}' direct_hr=0x{directHr:X8}");
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

    private static bool TryGetGuid(IMFAttributes attributes, ref Guid key, out Guid value)
    {
        var hr = attributes.GetGUID(ref key, out value);
        if (hr == MfEAttributeNotFound)
        {
            value = Guid.Empty;
            return false;
        }

        ThrowIfFailed(hr, $"IMFAttributes.GetGUID({key})");
        return true;
    }

    private static bool TryGetUInt64(IMFAttributes attributes, ref Guid key, out ulong value)
    {
        var hr = attributes.GetUINT64(ref key, out value);
        if (hr == MfEAttributeNotFound)
        {
            value = 0;
            return false;
        }

        ThrowIfFailed(hr, $"IMFAttributes.GetUINT64({key})");
        return true;
    }

    private static string ReadAllocatedString(IMFAttributes attributes, ref Guid key)
    {
        IntPtr textPtr = IntPtr.Zero;
        try
        {
            var hr = attributes.GetAllocatedString(ref key, out textPtr, out var length);
            if (hr == MfEAttributeNotFound || textPtr == IntPtr.Zero)
            {
                return string.Empty;
            }

            ThrowIfFailed(hr, $"IMFAttributes.GetAllocatedString({key})");
            return length > 0
                ? Marshal.PtrToStringUni(textPtr, length) ?? string.Empty
                : Marshal.PtrToStringUni(textPtr) ?? string.Empty;
        }
        finally
        {
            if (textPtr != IntPtr.Zero)
            {
                Marshal.FreeCoTaskMem(textPtr);
            }
        }
    }

    private static string SubtypeGuidToName(Guid subtype)
    {
        if (subtype == MfVideoFormatP010)
        {
            return "P010";
        }

        if (subtype == MfVideoFormatNv12)
        {
            return "NV12";
        }

        if (subtype == MfVideoFormatYuy2)
        {
            return "YUY2";
        }

        if (subtype == MfVideoFormatUyvy)
        {
            return "UYVY";
        }

        if (subtype == MfVideoFormatMjpg)
        {
            return "MJPG";
        }

        var bytes = subtype.ToByteArray();
        if (bytes[4] == 0 && bytes[5] == 0 && bytes[6] == 0x10 && bytes[7] == 0)
        {
            Span<char> fourCc = stackalloc char[4];
            for (var i = 0; i < 4; i++)
            {
                var b = bytes[i];
                fourCc[i] = b >= 0x20 && b <= 0x7E ? (char)b : '?';
            }

            return new string(fourCc);
        }

        return subtype.ToString("B");
    }

    private static bool SymbolicLinksMatch(string target, string candidate)
    {
        if (string.IsNullOrWhiteSpace(target) || string.IsNullOrWhiteSpace(candidate))
        {
            return false;
        }

        return string.Equals(target, candidate, StringComparison.OrdinalIgnoreCase) ||
               candidate.Contains(target, StringComparison.OrdinalIgnoreCase) ||
               target.Contains(candidate, StringComparison.OrdinalIgnoreCase);
    }

    private static void AddStartupReference()
    {
        lock (StartupSync)
        {
            if (_startupRefCount == 0)
            {
                ThrowIfFailed(MFStartup(MfVersion, 0), "MFStartup");
            }

            _startupRefCount++;
        }
    }

    private static void ReleaseStartupReference()
    {
        lock (StartupSync)
        {
            if (_startupRefCount <= 0)
            {
                return;
            }

            _startupRefCount--;
            if (_startupRefCount == 0)
            {
                _ = MFShutdown();
            }
        }
    }

    private static void ThrowIfFailed(int hr, string operation)
    {
        if (hr >= 0)
        {
            return;
        }

        throw new InvalidOperationException($"{operation} failed (hr=0x{hr:X8}).");
    }

}

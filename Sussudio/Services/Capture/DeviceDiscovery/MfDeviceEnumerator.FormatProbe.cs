using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.Services.Audio;

namespace Sussudio.Services.Capture;

internal static partial class MfDeviceEnumerator
{
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
                readerAttributes.SetUINT32(ref MfReadwriteDisableConverters, 1),
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
                        MfSourceReaderFirstVideoStream,
                        mediaTypeIndex,
                        out mediaType);
                    if (hr == MfENoMoreTypes)
                    {
                        break;
                    }

                    MfInteropHelpers.ThrowIfFailed(hr, $"IMFSourceReader.GetNativeMediaType(index={mediaTypeIndex})");
                    if (mediaType == null)
                    {
                        continue;
                    }

                    if (!MfInteropHelpers.TryGetUInt64(mediaType, ref MfMtFrameSize, out var packedFrameSize))
                    {
                        continue;
                    }

                    var width = (uint)(packedFrameSize >> 32);
                    var height = (uint)(packedFrameSize & 0xFFFFFFFFu);
                    if (width == 0 || height == 0)
                    {
                        continue;
                    }

                    if (!MfInteropHelpers.TryGetGuid(mediaType, ref MfMtSubtype, out var subtype))
                    {
                        continue;
                    }

                    uint frameRateNumerator = 0;
                    uint frameRateDenominator = 0;
                    if (MfInteropHelpers.TryGetUInt64(mediaType, ref MfMtFrameRate, out var packedFrameRate))
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
            MfInteropHelpers.ReleaseStartupReference();
        }

        return Task.FromResult(formats);
    }

    private static IMFMediaSource CreateMediaSource(string symbolicLink)
    {
        IMFAttributes? attributes = null;
        try
        {
            MfInteropHelpers.ThrowIfFailed(MFCreateAttributes(out attributes, 2), "MFCreateAttributes(device_source)");
            MfInteropHelpers.ThrowIfFailed(
                attributes.SetGUID(ref DevSourceAttributeSourceType, ref DevSourceAttributeSourceTypeVidcapGuid),
                "IMFAttributes.SetGUID(MF_DEVSOURCE_ATTRIBUTE_SOURCE_TYPE)");
            MfInteropHelpers.ThrowIfFailed(
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
            MfInteropHelpers.ThrowIfFailed(MFCreateAttributes(out attributes, 1), "MFCreateAttributes(device_enum_fallback)");
            MfInteropHelpers.ThrowIfFailed(
                attributes.SetGUID(ref DevSourceAttributeSourceType, ref DevSourceAttributeSourceTypeVidcapGuid),
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

                    var candidateLink = MfInteropHelpers.TryReadAllocatedString(activate, ref DevSourceAttributeSourceTypeVidcapSymbolicLink);
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
}

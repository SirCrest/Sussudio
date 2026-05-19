using System;
using System.Collections.Generic;
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

using System;
using Sussudio.Services.Audio;

namespace Sussudio.Services.Capture;

public sealed partial class MfSourceReaderVideoCapture
{
    private IMFMediaType SelectConvertedMediaType(
        IMFSourceReader reader,
        int requestedWidth,
        int requestedHeight,
        double requestedFps,
        Guid requestedSourceSubtype,
        Guid requestedOutputSubtype,
        out Guid selectedSubtype,
        out int selectedWidth,
        out int selectedHeight,
        out double selectedFps,
        out string negotiatedDescription)
    {
        var nativeType = SelectMediaType(
            reader,
            requestedWidth,
            requestedHeight,
            requestedFps,
            requestedSourceSubtype,
            out var nativeSubtype,
            out selectedWidth,
            out selectedHeight,
            out selectedFps,
            out _);

        IMFMediaType? convertedType = null;
        try
        {
            MfInteropHelpers.ThrowIfFailed(MfInterop.MFCreateMediaType(out convertedType), "MFCreateMediaType");

            MfInteropHelpers.ThrowIfFailed(
                convertedType.SetGUID(ref MfGuids.MF_MT_MAJOR_TYPE, ref MfGuids.MFMediaType_Video),
                "IMFMediaType.SetGUID(MF_MT_MAJOR_TYPE)");
            MfInteropHelpers.ThrowIfFailed(
                convertedType.SetGUID(ref MfGuids.MF_MT_SUBTYPE, ref requestedOutputSubtype),
                $"IMFMediaType.SetGUID(MF_MT_SUBTYPE,{SubtypeGuidToName(requestedOutputSubtype)})");

            if (MfInteropHelpers.TryGetUInt64(nativeType, ref MfGuids.MF_MT_FRAME_SIZE, out var frameSize))
            {
                MfInteropHelpers.ThrowIfFailed(
                    convertedType.SetUINT64(ref MfGuids.MF_MT_FRAME_SIZE, frameSize),
                    "IMFMediaType.SetUINT64(MF_MT_FRAME_SIZE)");
            }

            if (MfInteropHelpers.TryGetUInt64(nativeType, ref MfGuids.MF_MT_FRAME_RATE, out var frameRate))
            {
                MfInteropHelpers.ThrowIfFailed(
                    convertedType.SetUINT64(ref MfGuids.MF_MT_FRAME_RATE, frameRate),
                    "IMFMediaType.SetUINT64(MF_MT_FRAME_RATE)");
            }

            CopyOptionalUInt64(nativeType, convertedType, ref MfGuids.MF_MT_FRAME_RATE_RANGE_MIN);
            CopyOptionalUInt64(nativeType, convertedType, ref MfGuids.MF_MT_FRAME_RATE_RANGE_MAX);
            CopyOptionalUInt64(nativeType, convertedType, ref MfGuids.MF_MT_PIXEL_ASPECT_RATIO);
            CopyOptionalUInt32(nativeType, convertedType, ref MfGuids.MF_MT_INTERLACE_MODE);
            CopyOptionalUInt32(nativeType, convertedType, ref MfGuids.MF_MT_ALL_SAMPLES_INDEPENDENT);

            selectedSubtype = requestedOutputSubtype;
            negotiatedDescription =
                $"{SubtypeGuidToName(requestedOutputSubtype)} <= {SubtypeGuidToName(nativeSubtype)} {selectedWidth}x{selectedHeight}@{selectedFps:0.###}";

            var result = convertedType;
            convertedType = null;
            return result;
        }
        finally
        {
            WasapiComInterop.ReleaseComObject(ref nativeType);
            WasapiComInterop.ReleaseComObject(ref convertedType);
        }
    }

    private static void CopyOptionalUInt64(IMFAttributes source, IMFAttributes destination, ref Guid key)
    {
        if (!MfInteropHelpers.TryGetUInt64(source, ref key, out var value))
        {
            return;
        }

        MfInteropHelpers.ThrowIfFailed(destination.SetUINT64(ref key, value), $"IMFAttributes.SetUINT64({key})");
    }

    private static void CopyOptionalUInt32(IMFAttributes source, IMFAttributes destination, ref Guid key)
    {
        if (!MfInteropHelpers.TryGetUInt32(source, ref key, out var value))
        {
            return;
        }

        MfInteropHelpers.ThrowIfFailed(destination.SetUINT32(ref key, value), $"IMFAttributes.SetUINT32({key})");
    }
}

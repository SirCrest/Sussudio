using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Sussudio.Services.Audio;

namespace Sussudio.Services.Capture;

public sealed partial class MfSourceReaderVideoCapture
{
    private bool TrySetSourceReaderD3DManager(IMFAttributes attributes, IntPtr dxgiDeviceManager)
    {
        object? managerAsUnknown = null;
        try
        {
            managerAsUnknown = Marshal.GetObjectForIUnknown(dxgiDeviceManager);
            MfInteropHelpers.ThrowIfFailed(
                attributes.SetUnknown(ref MfGuids.MF_SOURCE_READER_D3D_MANAGER, managerAsUnknown),
                "IMFAttributes.SetUnknown(MF_SOURCE_READER_D3D_MANAGER)");
            return true;
        }
        catch (Exception ex)
        {
            Log(
                "MF_SOURCE_READER_D3D_INIT_WARN " +
                $"stage=SetUnknown type={ex.GetType().Name} hr=0x{ex.HResult:X8} msg={ex.Message} " +
                "fallback=cpu_only");
            return false;
        }
        finally
        {
            if (managerAsUnknown != null && Marshal.IsComObject(managerAsUnknown))
            {
                _ = Marshal.ReleaseComObject(managerAsUnknown);
            }
        }
    }

    private IMFMediaSource CreateMediaSource(string deviceSymbolicLink)
    {
        IMFAttributes? attrs = null;
        try
        {
            MfInteropHelpers.ThrowIfFailed(MfInterop.MFCreateAttributes(out attrs, 2), "MFCreateAttributes(device)");
            MfInteropHelpers.ThrowIfFailed(
                attrs.SetGUID(
                    ref MfGuids.MF_DEVSOURCE_ATTRIBUTE_SOURCE_TYPE,
                    ref MfGuids.MF_DEVSOURCE_ATTRIBUTE_SOURCE_TYPE_VIDCAP_GUID),
                "IMFAttributes.SetGUID(MF_DEVSOURCE_ATTRIBUTE_SOURCE_TYPE)");
            MfInteropHelpers.ThrowIfFailed(
                attrs.SetString(
                    ref MfGuids.MF_DEVSOURCE_ATTRIBUTE_SOURCE_TYPE_VIDCAP_SYMBOLIC_LINK,
                    deviceSymbolicLink),
                "IMFAttributes.SetString(MF_DEVSOURCE_ATTRIBUTE_SOURCE_TYPE_VIDCAP_SYMBOLIC_LINK)");

            var directHr = MfInterop.MFCreateDeviceSource(attrs, out var mediaSource);
            if (directHr >= 0 && mediaSource != null)
            {
                return mediaSource;
            }

            Log(
                "MF_SOURCE_READER_DEVICE_OPEN_DIRECT_FAIL " +
                $"device='{deviceSymbolicLink}' hr=0x{directHr:X8}");
            return CreateMediaSourceByEnumeration(deviceSymbolicLink, directHr);
        }
        finally
        {
            WasapiComInterop.ReleaseComObject(ref attrs);
        }
    }

    private IMFMediaSource CreateMediaSourceByEnumeration(string targetSymbolicLink, int directHr)
    {
        IMFAttributes? attrs = null;
        IntPtr activateArrayPtr = IntPtr.Zero;
        var candidates = new List<string>();

        try
        {
            MfInteropHelpers.ThrowIfFailed(MfInterop.MFCreateAttributes(out attrs, 1), "MFCreateAttributes(enum)");
            MfInteropHelpers.ThrowIfFailed(
                attrs.SetGUID(
                    ref MfGuids.MF_DEVSOURCE_ATTRIBUTE_SOURCE_TYPE,
                    ref MfGuids.MF_DEVSOURCE_ATTRIBUTE_SOURCE_TYPE_VIDCAP_GUID),
                "IMFAttributes.SetGUID(MF_DEVSOURCE_ATTRIBUTE_SOURCE_TYPE)");

            MfInteropHelpers.ThrowIfFailed(
                MfInterop.MFEnumDeviceSources(attrs, out activateArrayPtr, out var activateCount),
                "MFEnumDeviceSources");

            if (activateCount <= 0 || activateArrayPtr == IntPtr.Zero)
            {
                throw new InvalidOperationException(
                    $"No video capture devices were reported while opening '{targetSymbolicLink}'.");
            }

            for (var i = 0; i < activateCount; i++)
            {
                var activatePtr = Marshal.ReadIntPtr(activateArrayPtr, i * IntPtr.Size);
                if (activatePtr == IntPtr.Zero)
                {
                    continue;
                }

                IMFActivate? activate = null;
                try
                {
                    activate = (IMFActivate)Marshal.GetObjectForIUnknown(activatePtr);
                    _ = Marshal.Release(activatePtr);

                    var link = MfInteropHelpers.TryReadAllocatedString(
                        activate,
                        ref MfGuids.MF_DEVSOURCE_ATTRIBUTE_SOURCE_TYPE_VIDCAP_SYMBOLIC_LINK);
                    if (!string.IsNullOrWhiteSpace(link))
                    {
                        candidates.Add(link);
                    }

                    if (!DeviceSymbolicLinkMatcher.Matches(targetSymbolicLink, link))
                    {
                        continue;
                    }

                    var mediaSourceIid = typeof(IMFMediaSource).GUID;
                    MfInteropHelpers.ThrowIfFailed(
                        activate.ActivateObject(ref mediaSourceIid, out var activated),
                        "IMFActivate.ActivateObject(IMFMediaSource)");

                    if (activated is IMFMediaSource source)
                    {
                        return source;
                    }

                    throw new InvalidOperationException(
                        $"Activated object for '{link}' does not implement IMFMediaSource.");
                }
                finally
                {
                    WasapiComInterop.ReleaseComObject(ref activate);
                }
            }

            var candidateSummary = candidates.Count > 0
                ? string.Join(" | ", candidates)
                : "none";
            throw new InvalidOperationException(
                "Unable to open capture device by symbolic link. " +
                $"requested='{targetSymbolicLink}' direct_hr=0x{directHr:X8} candidates='{candidateSummary}'. " +
                "If this device cannot be shared, close other capture apps and retry with Windows Frame Server enabled.");
        }
        finally
        {
            if (activateArrayPtr != IntPtr.Zero)
            {
                Marshal.FreeCoTaskMem(activateArrayPtr);
            }

            WasapiComInterop.ReleaseComObject(ref attrs);
        }
    }

    private IMFMediaType SelectMediaType(
        IMFSourceReader reader,
        int requestedWidth,
        int requestedHeight,
        double requestedFps,
        Guid requestedSubtype,
        out Guid selectedSubtype,
        out int selectedWidth,
        out int selectedHeight,
        out double selectedFps,
        out string negotiatedDescription)
    {
        IMFMediaType? bestType = null;
        var bestFpsDelta = double.MaxValue;
        selectedSubtype = requestedSubtype;
        selectedWidth = requestedWidth;
        selectedHeight = requestedHeight;
        selectedFps = requestedFps;
        negotiatedDescription = "unknown";

        var totalNativeTypes = 0;
        var requestedSubtypeCount = 0;
        var subtypeSummary = new Dictionary<string, int>();
        var requestedSubtypeName = SubtypeGuidToName(requestedSubtype);

        for (var index = 0; ; index++)
        {
            IMFMediaType? nativeType = null;
            try
            {
                var hr = reader.GetNativeMediaType(
                    MfConstants.MF_SOURCE_READER_FIRST_VIDEO_STREAM,
                    index,
                    out nativeType);
                if (hr == MfHResults.MF_E_NO_MORE_TYPES)
                {
                    break;
                }

                MfInteropHelpers.ThrowIfFailed(hr, $"IMFSourceReader.GetNativeMediaType(index={index})");
                if (nativeType == null)
                {
                    continue;
                }

                totalNativeTypes++;
                var hasSubtype = MfInteropHelpers.TryGetGuid(nativeType, ref MfGuids.MF_MT_SUBTYPE, out var subtype);
                var subtypeName = hasSubtype ? SubtypeGuidToName(subtype) : "unknown";

                if (!subtypeSummary.ContainsKey(subtypeName))
                    subtypeSummary[subtypeName] = 0;
                subtypeSummary[subtypeName]++;

                TryGetFrameSize(nativeType, out var nWidth, out var nHeight);
                var nFps = TryGetFrameRate(nativeType, out var nNum, out var nDen) && nDen > 0
                    ? (double)nNum / nDen : 0;

                if (hasSubtype && subtype == requestedSubtype)
                {
                    requestedSubtypeCount++;
                    Log($"MF_SOURCE_READER_NATIVE_{requestedSubtypeName} index={index} {nWidth}x{nHeight}@{nFps:0.###}");
                }

                if (!hasSubtype || subtype != requestedSubtype)
                {
                    continue;
                }

                var width = nWidth;
                var height = nHeight;
                if (width != requestedWidth || height != requestedHeight)
                {
                    continue;
                }

                var delta = Math.Abs(nFps - requestedFps);

                if (delta < bestFpsDelta)
                {
                    WasapiComInterop.ReleaseComObject(ref bestType);
                    bestType = nativeType;
                    nativeType = null;
                    bestFpsDelta = delta;
                    selectedWidth = width;
                    selectedHeight = height;
                    selectedFps = nFps > 0 ? nFps : requestedFps;
                    selectedSubtype = subtype;
                    negotiatedDescription = nFps > 0
                        ? $"{requestedSubtypeName} {width}x{height}@{nFps:0.###}"
                        : $"{requestedSubtypeName} {width}x{height}";
                }
            }
            finally
            {
                WasapiComInterop.ReleaseComObject(ref nativeType);
            }
        }

        var subtypeList = string.Join(", ", subtypeSummary.Select(kv => $"{kv.Key}={kv.Value}"));
        Log(
            "MF_SOURCE_READER_NATIVE_TYPES " +
            $"total={totalNativeTypes} requested_subtype={requestedSubtypeName} " +
            $"requested_count={requestedSubtypeCount} subtypes=[{subtypeList}]");

        if (bestType == null)
        {
            throw new InvalidOperationException(
                $"No {requestedSubtypeName} media type was found for {requestedWidth}x{requestedHeight}@{requestedFps:0.###}. " +
                $"Source reader has {totalNativeTypes} native types ({requestedSubtypeCount} {requestedSubtypeName}). Subtypes: [{subtypeList}]");
        }

        if (bestFpsDelta > 0.5)
        {
            WasapiComInterop.ReleaseComObject(ref bestType);
            throw new InvalidOperationException(
                $"No {requestedSubtypeName} media type matched requested frame rate {requestedFps:0.###}fps " +
                $"for {requestedWidth}x{requestedHeight}.");
        }

        return bestType;
    }

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

    private static bool TryGetFrameSize(IMFAttributes attributes, out int width, out int height)
    {
        width = 0;
        height = 0;
        if (!MfInteropHelpers.TryGetUInt64(attributes, ref MfGuids.MF_MT_FRAME_SIZE, out var packed))
        {
            return false;
        }

        width = (int)(packed >> 32);
        height = (int)(packed & 0xFFFFFFFFu);
        return width > 0 && height > 0;
    }

    private static bool TryGetFrameRate(
        IMFAttributes attributes,
        out uint numerator,
        out uint denominator)
    {
        numerator = 0;
        denominator = 0;
        if (!MfInteropHelpers.TryGetUInt64(attributes, ref MfGuids.MF_MT_FRAME_RATE, out var packed))
        {
            return false;
        }

        numerator = (uint)(packed >> 32);
        denominator = (uint)(packed & 0xFFFFFFFFu);
        return numerator > 0 && denominator > 0;
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

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Sussudio.Services.Audio;

namespace Sussudio.Services.Capture;

public sealed partial class MfSourceReaderVideoCapture
{
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
                var rawReleased = false;
                try
                {
                    activate = (IMFActivate)Marshal.GetObjectForIUnknown(activatePtr);
                    _ = Marshal.Release(activatePtr);
                    rawReleased = true;

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
                        ReleaseRemainingActivateObjects(activateArrayPtr, activateCount, i + 1);
                        return source;
                    }

                    if (activated != null && Marshal.IsComObject(activated))
                    {
                        _ = Marshal.ReleaseComObject(activated);
                    }

                    throw new InvalidOperationException(
                        $"Activated object for '{link}' does not implement IMFMediaSource.");
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

    private static void ReleaseRemainingActivateObjects(IntPtr activateArrayPtr, int activateCount, int startIndex)
    {
        for (var i = startIndex; i < activateCount; i++)
        {
            var activatePtr = Marshal.ReadIntPtr(activateArrayPtr, i * IntPtr.Size);
            if (activatePtr == IntPtr.Zero)
            {
                continue;
            }

            try
            {
                _ = Marshal.Release(activatePtr);
            }
            catch
            {
                // Best effort.
            }
        }
    }
}

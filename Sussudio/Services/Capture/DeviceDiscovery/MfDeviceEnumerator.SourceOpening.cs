using System;
using System.Runtime.InteropServices;
using Sussudio.Services.Audio;

namespace Sussudio.Services.Capture;

internal static partial class MfDeviceEnumerator
{
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
                        // Release remaining activate objects that we won't visit.
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
}

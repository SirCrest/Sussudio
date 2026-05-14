using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Sussudio.Services.Audio;

namespace Sussudio.Services.Capture;

internal static partial class MfDeviceEnumerator
{
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
                    attributes.SetGUID(ref DevSourceAttributeSourceType, ref DevSourceAttributeSourceTypeVidcapGuid),
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

                        var friendlyName = MfInteropHelpers.TryReadAllocatedString(activate, ref DevSourceAttributeFriendlyName);
                        var symbolicLink = MfInteropHelpers.TryReadAllocatedString(activate, ref DevSourceAttributeSourceTypeVidcapSymbolicLink);
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
            MfInteropHelpers.ReleaseStartupReference();
        }

        return Task.FromResult(devices);
    }
}

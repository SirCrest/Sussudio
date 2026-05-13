using System;
using System.Runtime.InteropServices;
using FFmpeg.AutoGen;

namespace Sussudio.Services.Recording;

internal sealed unsafe partial class LibAvEncoder
{
    private void EnsureOpen()
    {
        if (!_isOpen || _formatCtx == null || _videoCodecCtx == null || _videoStream == null || _videoFrame == null || _packet == null)
        {
            throw new InvalidOperationException("LibAvEncoder is not initialized.");
        }
    }

    private static void ThrowIfError(int errorCode, string operation)
    {
        if (errorCode >= 0)
        {
            return;
        }

        var message = GetErrorString(errorCode);
        Logger.Log($"LIBAV_ENCODER_ERROR operation={operation} code={errorCode} msg='{message}'");
        throw new InvalidOperationException($"LIBAV_ENCODER_ERROR operation={operation} code={errorCode} msg='{message}'");
    }

    private static string GetErrorString(int errorCode)
    {
        var buffer = stackalloc byte[ffmpeg.AV_ERROR_MAX_STRING_SIZE];
        ffmpeg.av_strerror(errorCode, buffer, (ulong)ffmpeg.AV_ERROR_MAX_STRING_SIZE);
        return Marshal.PtrToStringAnsi((IntPtr)buffer) ?? $"unknown error {errorCode}";
    }

    private static InvalidOperationException CreateLibAvException(string message)
    {
        Logger.Log(message);
        return new InvalidOperationException(message);
    }

    /// <summary>
    /// Checks ID3D11Device::GetDeviceRemovedReason (vtable slot 39) to detect TDR.
    /// CopySubresourceRegion is void-return, so after a device-removed event all
    /// context calls silently no-op. This proactive check surfaces the error before
    /// NVENC encodes from stale/garbage textures, allowing the caller to finalize
    /// the recording and preserve already-encoded data.
    /// </summary>
    private static void CheckDeviceRemoved(IntPtr d3d11Device)
    {
        if (d3d11Device == IntPtr.Zero)
            return;

        var deviceVtable = *(IntPtr*)d3d11Device;
        // ID3D11Device vtable layout: IUnknown (0-2) + ID3D11Device methods (3+).
        // CreateTexture2D = slot 5 (validated elsewhere in this file).
        // GetDeviceRemovedReason = slot 39 (3 IUnknown + 36 ID3D11Device methods before it).
        var getDeviceRemovedReason =
            (delegate* unmanaged[Stdcall]<IntPtr, int>)*(IntPtr*)(deviceVtable + 39 * IntPtr.Size);
        var hr = getDeviceRemovedReason(d3d11Device);

        if (hr < 0)
        {
            throw CreateLibAvException(
                $"LIBAV_ENCODER_DEVICE_REMOVED hr=0x{unchecked((uint)hr):X8} " +
                "msg=GPU device was removed (TDR). Recording will be finalized with frames encoded so far.");
        }
    }
}

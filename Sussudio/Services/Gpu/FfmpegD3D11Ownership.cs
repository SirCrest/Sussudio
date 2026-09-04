using System;
using System.Runtime.InteropServices;
using FFmpeg.AutoGen;

namespace Sussudio.Services.Gpu;

/// <summary>
/// Transfers borrowed D3D11 COM pointers into an FFmpeg hardware-device context.
/// FFmpeg owns the retained references after a successful transfer and releases
/// them when the final owning <see cref="AVBufferRef"/> is unreferenced.
/// </summary>
internal static unsafe class FfmpegD3D11Ownership
{
    internal static void TransferBorrowedReferences(
        AVD3D11VADeviceContext* destination,
        IntPtr borrowedDevice,
        IntPtr borrowedDeviceContext)
    {
        TransferBorrowedReferencesCore(
            (IntPtr)destination,
            borrowedDevice,
            borrowedDeviceContext,
            static pointer => Marshal.AddRef(pointer),
            static pointer => Marshal.Release(pointer),
            static (destinationPointer, devicePointer, deviceContextPointer) =>
            {
                var target = (AVD3D11VADeviceContext*)destinationPointer.ToPointer();
                target->device = (FFmpeg.AutoGen.ID3D11Device*)devicePointer.ToPointer();
                target->device_context = (FFmpeg.AutoGen.ID3D11DeviceContext*)deviceContextPointer.ToPointer();
            });
    }

    /// <summary>
    /// Retains both borrowed references before publishing either pointer. The
    /// commit callback must publish both pointers without throwing after a
    /// partial write; the production callback consists only of two native stores.
    /// </summary>
    internal static void TransferBorrowedReferencesCore(
        IntPtr destination,
        IntPtr borrowedDevice,
        IntPtr borrowedDeviceContext,
        Action<IntPtr> addRef,
        Action<IntPtr> release,
        Action<IntPtr, IntPtr, IntPtr> commit)
    {
        if (destination == IntPtr.Zero)
        {
            throw new ArgumentException("FFmpeg D3D11 destination is null.", nameof(destination));
        }

        if (borrowedDevice == IntPtr.Zero)
        {
            throw new ArgumentException("D3D11 device pointer is null.", nameof(borrowedDevice));
        }

        if (borrowedDeviceContext == IntPtr.Zero)
        {
            throw new ArgumentException("D3D11 device-context pointer is null.", nameof(borrowedDeviceContext));
        }

        ArgumentNullException.ThrowIfNull(addRef);
        ArgumentNullException.ThrowIfNull(release);
        ArgumentNullException.ThrowIfNull(commit);

        var deviceRetained = false;
        var deviceContextRetained = false;
        var ownershipTransferred = false;

        try
        {
            addRef(borrowedDevice);
            deviceRetained = true;

            addRef(borrowedDeviceContext);
            deviceContextRetained = true;

            commit(destination, borrowedDevice, borrowedDeviceContext);
            ownershipTransferred = true;
        }
        finally
        {
            if (!ownershipTransferred)
            {
                try
                {
                    if (deviceContextRetained)
                    {
                        release(borrowedDeviceContext);
                    }
                }
                finally
                {
                    if (deviceRetained)
                    {
                        release(borrowedDevice);
                    }
                }
            }
        }
    }
}

using System;
using System.Buffers;
using System.Runtime.InteropServices;
using System.Threading;
using Sussudio.Services.Contracts;
using Sussudio.Services.Runtime;
using Vortice.Direct3D11;

namespace Sussudio.Services.Preview;

internal sealed partial class D3D11PreviewRenderer
{
    public void SubmitRawFrame(
        IntPtr data,
        int dataLength,
        int width,
        int height,
        bool isHdr,
        PreviewFrameTracking tracking)
    {
        if (Volatile.Read(ref _disposed) != 0 || Volatile.Read(ref _stopRequested) != 0)
        {
            return;
        }

        if (data == IntPtr.Zero || dataLength <= 0 || width <= 0 || height <= 0)
        {
            return;
        }

        var copied = ArrayPool<byte>.Shared.Rent(dataLength);
        try
        {
            Marshal.Copy(data, copied, 0, dataLength);
        }
        catch
        {
            ArrayPool<byte>.Shared.Return(copied);
            throw;
        }

        var frame = new PendingFrame(
            null,
            0,
            copied,
            dataLength,
            width,
            height,
            isHdr,
            tracking.ArrivalTick,
            tracking.SourceSequenceNumber,
            tracking.PreviewPresentId,
            tracking.SchedulerSubmitTick,
            tracking.SourcePtsTicks,
            countForPresentCadence: tracking.CountForPresentCadence);
        EnqueuePendingFrame(frame);
    }

    public void SubmitRawFrameLease(
        PooledVideoFrameLease frame,
        bool isHdr,
        PreviewFrameTracking tracking)
    {
        ArgumentNullException.ThrowIfNull(frame);

        if (Volatile.Read(ref _disposed) != 0 || Volatile.Read(ref _stopRequested) != 0)
        {
            frame.Dispose();
            return;
        }

        if (frame.Length <= 0 || frame.Width <= 0 || frame.Height <= 0)
        {
            frame.Dispose();
            return;
        }

        EnqueuePendingFrame(new PendingFrame(
            null,
            0,
            null,
            frame.Length,
            frame.Width,
            frame.Height,
            isHdr,
            frame.ArrivalTick,
            frame.SequenceNumber,
            tracking.PreviewPresentId,
            tracking.SchedulerSubmitTick,
            sourcePtsTicks: 0,
            frameLease: frame,
            countForPresentCadence: tracking.CountForPresentCadence));
    }

    public void SubmitTexture(
        IntPtr d3dTexture,
        int subresourceIndex,
        int width,
        int height,
        bool isHdr,
        PreviewFrameTracking tracking)
    {
        if (Volatile.Read(ref _disposed) != 0 || Volatile.Read(ref _stopRequested) != 0)
        {
            return;
        }

        if (d3dTexture == IntPtr.Zero || width <= 0 || height <= 0)
        {
            return;
        }

        if (Volatile.Read(ref _sharedDeviceActive) == 0)
        {
            throw new InvalidOperationException("Shared D3D11 device is not active for texture submission.");
        }

        IntPtr ownedTexturePtr = IntPtr.Zero;
        ID3D11Texture2D? texture = null;
        try
        {
            Marshal.AddRef(d3dTexture);
            ownedTexturePtr = d3dTexture;
            texture = new ID3D11Texture2D(ownedTexturePtr);
        }
        catch
        {
            texture?.Dispose();
            if (texture == null && ownedTexturePtr != IntPtr.Zero)
            {
                Marshal.Release(ownedTexturePtr);
            }

            throw;
        }

        var frame = new PendingFrame(
            texture,
            subresourceIndex,
            null,
            0,
            width,
            height,
            isHdr,
            tracking.ArrivalTick,
            tracking.SourceSequenceNumber,
            tracking.PreviewPresentId,
            tracking.SchedulerSubmitTick,
            tracking.SourcePtsTicks,
            countForPresentCadence: tracking.CountForPresentCadence);
        EnqueuePendingFrame(frame);
    }
}

using System;
using System.Runtime.InteropServices;
using System.Threading;
using Sussudio.Services.Contracts;
using Vortice.Direct3D11;

namespace Sussudio.Services.Preview;

internal sealed partial class D3D11PreviewRenderer
{
    private bool _loggedNv12ShaderMissing;
    private int _lastNv12IsHdr = -1; // tri-state: -1 = unset, 0 = SDR, 1 = HDR

    public void SubmitNv12PlaneTextures(
        IntPtr yTexturePtr,
        IntPtr uvTexturePtr,
        int width,
        int height,
        bool isHdr,
        PreviewFrameTracking tracking)
    {
        if (Volatile.Read(ref _disposed) != 0 || Volatile.Read(ref _stopRequested) != 0)
        {
            return;
        }

        if (yTexturePtr == IntPtr.Zero || uvTexturePtr == IntPtr.Zero || width <= 0 || height <= 0)
        {
            return;
        }

        if (_nv12PS == null)
        {
            if (!_loggedNv12ShaderMissing)
            {
                Logger.Log("D3D11_RENDERER_WARN NV12 pixel shader not available - frames will be dropped via this path");
                _loggedNv12ShaderMissing = true;
            }
            return;
        }

        if (Volatile.Read(ref _sharedDeviceActive) == 0)
        {
            throw new InvalidOperationException("Shared D3D11 device is not active for NV12 texture submission.");
        }

        // Log the first frame and any HDR/SDR transition through the NV12 plane path.
        var hdrInt = isHdr ? 1 : 0;
        var prev = Interlocked.Exchange(ref _lastNv12IsHdr, hdrInt);
        if (prev != hdrInt)
        {
            var prevLabel = prev == -1 ? "unset" : (prev == 1 ? "HDR" : "SDR");
            var curLabel = isHdr ? "HDR" : "SDR";
            Logger.Log($"D3D11_PREVIEW_NV12_HDR_TRANSITION from={prevLabel} to={curLabel} pathTag=PlaneTextures");
        }

        IntPtr ownedYTexturePtr = IntPtr.Zero;
        IntPtr ownedUvTexturePtr = IntPtr.Zero;
        ID3D11Texture2D? yTexture = null;
        ID3D11Texture2D? uvTexture = null;
        try
        {
            Marshal.AddRef(yTexturePtr);
            ownedYTexturePtr = yTexturePtr;
            yTexture = new ID3D11Texture2D(ownedYTexturePtr);

            Marshal.AddRef(uvTexturePtr);
            ownedUvTexturePtr = uvTexturePtr;
            uvTexture = new ID3D11Texture2D(ownedUvTexturePtr);

            EnqueueNv12Frame(
                ownedYTexturePtr,
                yTexture,
                ownedUvTexturePtr,
                uvTexture,
                width,
                height,
                isHdr,
                tracking);
        }
        catch
        {
            uvTexture?.Dispose();
            if (uvTexture == null && ownedUvTexturePtr != IntPtr.Zero)
            {
                Marshal.Release(ownedUvTexturePtr);
            }

            yTexture?.Dispose();
            if (yTexture == null && ownedYTexturePtr != IntPtr.Zero)
            {
                Marshal.Release(ownedYTexturePtr);
            }

            throw;
        }
    }

    private void EnqueueNv12Frame(
        IntPtr yTexturePtr,
        ID3D11Texture2D yTexture,
        IntPtr uvTexturePtr,
        ID3D11Texture2D uvTexture,
        int width,
        int height,
        bool isHdr,
        PreviewFrameTracking tracking)
    {
        var frame = new PendingFrame(
            d3dTexture: null,
            d3dSubresourceIndex: 0,
            rawData: null,
            rawDataLength: 0,
            width: width,
            height: height,
            isHdr: isHdr,
            arrivalTick: tracking.ArrivalTick,
            sourceSequenceNumber: tracking.SourceSequenceNumber,
            previewPresentId: tracking.PreviewPresentId,
            schedulerSubmitTick: tracking.SchedulerSubmitTick,
            sourcePtsTicks: tracking.SourcePtsTicks,
            d3dTextureY: yTexturePtr,
            d3dTextureUV: uvTexturePtr,
            d3dTextureYObject: yTexture,
            d3dTextureUVObject: uvTexture,
            countForPresentCadence: tracking.CountForPresentCadence);
        EnqueuePendingFrame(frame);
    }
}

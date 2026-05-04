using System;
using Sussudio.Services.Capture;

namespace Sussudio.Services.Preview;

internal readonly record struct PreviewDisplayClockSnapshot(
    long LastPresentTick,
    long FrameIntervalTicks,
    double ExpectedFrameIntervalMs,
    int SampleCount);

internal interface IPreviewDisplayClock
{
    bool TryGetDisplayClock(out PreviewDisplayClockSnapshot snapshot);
}

internal interface IPreviewFrameQueueControl
{
    int DropPendingFrames(string reason);
}

internal interface IPreviewFrameSink
{
    /// <summary>
    /// Submit a CPU-resident frame. Callee copies the data immediately;
    /// caller retains ownership and may free the buffer after return.
    /// </summary>
    void SubmitRawFrame(
        IntPtr data,
        int dataLength,
        int width,
        int height,
        bool isHdr,
        long arrivalTick = 0,
        long sourceSequenceNumber = -1,
        long previewPresentId = 0,
        long schedulerSubmitTick = 0);

    /// <summary>
    /// Submit a leased CPU-resident frame. Callee owns and disposes the lease.
    /// </summary>
    void SubmitRawFrameLease(
        PooledVideoFrameLease frame,
        bool isHdr,
        long previewPresentId = 0,
        long schedulerSubmitTick = 0);

    /// <summary>
    /// Submit a D3D11 texture. Callee calls AddRef on the COM pointer;
    /// caller may Release after return.
    /// </summary>
    void SubmitTexture(
        IntPtr d3dTexture,
        int subresourceIndex,
        int width,
        int height,
        bool isHdr,
        long arrivalTick = 0,
        long schedulerSubmitTick = 0);

    /// <summary>
    /// Submit split NV12 plane textures (Y + UV). Callee calls AddRef on
    /// both COM pointers; caller may Release after return.
    /// Pass <paramref name="isHdr"/> = true when the source content is HDR
    /// (e.g. NVDEC NV12 output from a P010 source) so the renderer can route
    /// the frame through the HDR shader path rather than the SDR VideoProcessor.
    /// </summary>
    void SubmitNv12PlaneTextures(IntPtr yTexturePtr, IntPtr uvTexturePtr, int width, int height, bool isHdr = false, long arrivalTick = 0);
}

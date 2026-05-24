using System;

namespace Sussudio.Services.Contracts;

// Bundles the per-frame tracking metadata that every IPreviewFrameSink.Submit*
// overload accepts. Collapses the prior 6-parameter trailing block (which had
// drifted into different orderings between SubmitRawFrame and SubmitTexture)
// into a single value with a stable field order. SourceSequenceNumber=-1 and
// CountForPresentCadence=true match the old default-argument behavior; use
// PreviewFrameTracking.Default as the starting point.
public readonly record struct PreviewFrameTracking(
    long ArrivalTick,
    long SourceSequenceNumber,
    long PreviewPresentId,
    long SchedulerSubmitTick,
    long SourcePtsTicks,
    bool CountForPresentCadence)
{
    public static PreviewFrameTracking Default { get; } = new(
        ArrivalTick: 0,
        SourceSequenceNumber: -1,
        PreviewPresentId: 0,
        SchedulerSubmitTick: 0,
        SourcePtsTicks: 0,
        CountForPresentCadence: true);

    public PreviewFrameTracking WithArrivalTick(long arrivalTick)
        => this with { ArrivalTick = arrivalTick };
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
        PreviewFrameTracking tracking);

    /// <summary>
    /// Submit a leased CPU-resident frame. Callee owns and disposes the lease.
    /// ArrivalTick and SourceSequenceNumber on <paramref name="tracking"/> are
    /// ignored — the lease's own ArrivalTick / SequenceNumber are authoritative.
    /// </summary>
    void SubmitRawFrameLease(
        PooledVideoFrameLease frame,
        bool isHdr,
        PreviewFrameTracking tracking);

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
        PreviewFrameTracking tracking);

    /// <summary>
    /// Submit split NV12 plane textures (Y + UV). Callee calls AddRef on
    /// both COM pointers; caller may Release after return.
    /// Pass <paramref name="isHdr"/> = true when the source content is HDR
    /// (e.g. NVDEC NV12 output from a P010 source) so the renderer can route
    /// the frame through the HDR shader path rather than the SDR VideoProcessor.
    /// </summary>
    void SubmitNv12PlaneTextures(
        IntPtr yTexturePtr,
        IntPtr uvTexturePtr,
        int width,
        int height,
        bool isHdr,
        PreviewFrameTracking tracking);
}

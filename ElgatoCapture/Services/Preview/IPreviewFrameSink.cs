using System;

namespace ElgatoCapture.Services;

internal interface IPreviewFrameSink
{
    /// <summary>
    /// Submit a CPU-resident frame. Callee copies the data immediately;
    /// caller retains ownership and may free the buffer after return.
    /// </summary>
    void SubmitRawFrame(IntPtr data, int dataLength, int width, int height, bool isHdr, long arrivalTick = 0);

    /// <summary>
    /// Submit a D3D11 texture. Callee calls AddRef on the COM pointer;
    /// caller may Release after return.
    /// </summary>
    void SubmitTexture(IntPtr d3dTexture, int subresourceIndex, int width, int height, bool isHdr, long arrivalTick = 0);

    /// <summary>
    /// Submit split NV12 plane textures (Y + UV). Callee calls AddRef on
    /// both COM pointers; caller may Release after return.
    /// </summary>
    void SubmitNv12PlaneTextures(IntPtr yTexturePtr, IntPtr uvTexturePtr, int width, int height, long arrivalTick = 0);
}

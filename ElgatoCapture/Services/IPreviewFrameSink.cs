using System;

namespace ElgatoCapture.Services;

internal interface IPreviewFrameSink
{
    void SubmitRawFrame(IntPtr data, int dataLength, int width, int height, bool isHdr, long arrivalTick = 0);
    void SubmitTexture(IntPtr d3dTexture, int subresourceIndex, int width, int height, bool isHdr, long arrivalTick = 0);
    void SubmitNv12PlaneTextures(IntPtr yTexturePtr, IntPtr uvTexturePtr, int width, int height, long arrivalTick = 0);
}

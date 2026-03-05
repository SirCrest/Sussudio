using System;

namespace ElgatoCapture.Services;

internal interface IPreviewFrameSink
{
    void SubmitRawFrame(IntPtr data, int dataLength, int width, int height, bool isHdr);
    void SubmitTexture(IntPtr d3dTexture, int subresourceIndex, int width, int height, bool isHdr);
}

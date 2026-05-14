using System;
using FFmpeg.AutoGen;

namespace Sussudio.Services.Gpu;

/// <summary>
/// FFmpeg mjpeg_cuvid decoder using NVIDIA's NVDEC hardware JPEG decode engine.
/// Supports either owning its CUDA contexts or retaining shared contexts provided by the caller.
/// </summary>
internal sealed unsafe partial class NvdecMjpegDecoder : IDisposable
{
    private AVCodecContext* _decoderCtx;
    private AVBufferRef* _hwDeviceCtx;
    private AVBufferRef* _hwFramesCtx;
    private AVFrame* _decodedFrame;
    private AVFrame* _drainFrame;
    private AVFrame* _cpuFrame;
    private AVPacket* _packet;
    private IntPtr _packedCpuBuffer;
    private int _packedCpuBufferSize;
    private int _width;
    private int _height;
    private bool _initialized;
    private bool _disposed;
    private int _downloadDiagDone;

    public AVBufferRef* HwDeviceCtx => _hwDeviceCtx;
    public AVBufferRef* HwFramesCtx => _hwFramesCtx;

}

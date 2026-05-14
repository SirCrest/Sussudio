using System;
using FFmpeg.AutoGen;

namespace Sussudio.Services.Gpu;

internal sealed unsafe partial class NvdecMjpegDecoder
{
    /// <summary>
    /// Decode a JPEG frame to a CUDA NV12 surface. The returned frame remains owned by the decoder.
    /// </summary>
    public AVFrame* DecodeFrame(ReadOnlySpan<byte> jpegData)
    {
        if (!_initialized || _decoderCtx == null || _packet == null || _decodedFrame == null)
        {
            throw new InvalidOperationException("Decoder is not initialized.");
        }

        fixed (byte* dataPtr = jpegData)
        {
            ffmpeg.av_packet_unref(_packet);
            _packet->data = dataPtr;
            _packet->size = jpegData.Length;

            var sendResult = ffmpeg.avcodec_send_packet(_decoderCtx, _packet);
            if (sendResult < 0)
            {
                Logger.Log($"NVDEC_MJPEG_SEND_PACKET_FAIL code={sendResult} msg='{GetErrorString(sendResult)}'");
                return null;
            }

            ffmpeg.av_frame_unref(_decodedFrame);
            var receiveResult = ffmpeg.avcodec_receive_frame(_decoderCtx, _decodedFrame);
            if (receiveResult < 0)
            {
                if (receiveResult != ffmpeg.AVERROR(ffmpeg.EAGAIN))
                {
                    Logger.Log($"NVDEC_MJPEG_RECV_FRAME_FAIL code={receiveResult} msg='{GetErrorString(receiveResult)}'");
                }

                return null;
            }

            // Drain any additional buffered frames — keep the latest.
            // MJPEG is intra-only so typically 1:1, but NVDEC may buffer briefly.
            if (_drainFrame == null)
                _drainFrame = ffmpeg.av_frame_alloc();

            while (true)
            {
                ffmpeg.av_frame_unref(_drainFrame);
                var drainResult = ffmpeg.avcodec_receive_frame(_decoderCtx, _drainFrame);
                if (drainResult < 0)
                    break;
                ffmpeg.av_frame_unref(_decodedFrame);
                ffmpeg.av_frame_move_ref(_decodedFrame, _drainFrame);
            }

            return _decodedFrame;
        }
    }

    /// <summary>
    /// Extract the CUcontext from FFmpeg's CUDA hw device context.
    /// AVCUDADeviceContext layout starts with CUcontext at offset 0.
    /// </summary>
    public IntPtr GetCudaContext()
    {
        if (!_initialized || _hwDeviceCtx == null)
        {
            throw new InvalidOperationException("Decoder not initialized.");
        }

        var hwDevCtx = (AVHWDeviceContext*)_hwDeviceCtx->data;
        if (hwDevCtx == null || hwDevCtx->hwctx == null)
        {
            throw new InvalidOperationException("CUDA device context is unavailable.");
        }

        return *(IntPtr*)hwDevCtx->hwctx;
    }

}

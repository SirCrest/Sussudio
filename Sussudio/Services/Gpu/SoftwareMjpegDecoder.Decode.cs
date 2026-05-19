using System;
using System.Threading;
using FFmpeg.AutoGen;

namespace Sussudio.Services.Gpu;

internal sealed unsafe partial class SoftwareMjpegDecoder
{
    /// <summary>
    /// Decodes MJPEG data and writes NV12 output directly into the caller's buffer,
    /// avoiding an intermediate copy through the decoder's own storage.
    /// </summary>
    public bool DecodeToNv12(ReadOnlySpan<byte> jpegData, Span<byte> nv12Destination)
    {
        if (!_initialized || _decoderCtx == null || _packet == null || _decodedFrame == null)
        {
            throw new InvalidOperationException("Decoder is not initialized.");
        }

        if (jpegData.IsEmpty)
        {
            return false;
        }

        if (nv12Destination.Length < _nv12Size)
        {
            throw new ArgumentException(
                $"NV12 destination too small: {nv12Destination.Length} < {_nv12Size}", nameof(nv12Destination));
        }

        fixed (byte* dataPtr = jpegData)
        {
            ffmpeg.av_packet_unref(_packet);
            _packet->data = dataPtr;
            _packet->size = jpegData.Length;

            var sendResult = ffmpeg.avcodec_send_packet(_decoderCtx, _packet);
            if (sendResult < 0)
            {
                Logger.Log($"SW_MJPEG_SEND_PACKET_FAIL code={sendResult} msg='{GetErrorString(sendResult)}'");
                return false;
            }

            ffmpeg.av_frame_unref(_decodedFrame);
            var receiveResult = ffmpeg.avcodec_receive_frame(_decoderCtx, _decodedFrame);
            if (receiveResult < 0)
            {
                if (receiveResult != ffmpeg.AVERROR(ffmpeg.EAGAIN))
                {
                    Logger.Log($"SW_MJPEG_RECV_FRAME_FAIL code={receiveResult} msg='{GetErrorString(receiveResult)}'");
                }

                return false;
            }

            while (_drainFrame != null)
            {
                ffmpeg.av_frame_unref(_drainFrame);
                var drainResult = ffmpeg.avcodec_receive_frame(_decoderCtx, _drainFrame);
                if (drainResult < 0)
                {
                    break;
                }

                ffmpeg.av_frame_unref(_decodedFrame);
                ffmpeg.av_frame_move_ref(_decodedFrame, _drainFrame);
            }

            var format = (AVPixelFormat)_decodedFrame->format;
            if (format != AVPixelFormat.AV_PIX_FMT_YUVJ420P &&
                format != AVPixelFormat.AV_PIX_FMT_YUV420P)
            {
                var message = $"SW_MJPEG_UNSUPPORTED_FMT fmt={format} w={_decodedFrame->width} h={_decodedFrame->height}";
                Logger.Log(message);
                throw new SoftwareMjpegDecoderPermanentException(message);
            }

            if (_decodedFrame->width != _width || _decodedFrame->height != _height)
            {
                var message =
                    $"SW_MJPEG_DIM_MISMATCH expected={_width}x{_height} actual={_decodedFrame->width}x{_decodedFrame->height}";
                Logger.Log(message);
                throw new SoftwareMjpegDecoderPermanentException(message);
            }

            if (Interlocked.Exchange(ref _diagDone, 1) == 0)
            {
                Logger.Log(
                    $"SW_MJPEG_DECODE_DIAG fmt={format} w={_decodedFrame->width} h={_decodedFrame->height} " +
                    $"y_stride={_decodedFrame->linesize[0]} u_stride={_decodedFrame->linesize[1]} v_stride={_decodedFrame->linesize[2]}");
            }

            fixed (byte* nv12Ptr = nv12Destination)
            {
                var yBytes = _width * _height;
                if (_decodedFrame->linesize[0] == _width)
                {
                    Buffer.MemoryCopy(_decodedFrame->data[0], nv12Ptr, yBytes, yBytes);
                }
                else
                {
                    for (var row = 0; row < _height; row++)
                    {
                        Buffer.MemoryCopy(
                            _decodedFrame->data[0] + (row * _decodedFrame->linesize[0]),
                            nv12Ptr + (row * _width),
                            _width,
                            _width);
                    }
                }

                var uvDestination = nv12Ptr + yBytes;
                for (var row = 0; row < _height / 2; row++)
                {
                    var uRow = _decodedFrame->data[1] + (row * _decodedFrame->linesize[1]);
                    var vRow = _decodedFrame->data[2] + (row * _decodedFrame->linesize[2]);
                    var destRow = uvDestination + (row * _width);

                    for (var column = 0; column < _width / 2; column++)
                    {
                        destRow[column * 2] = uRow[column];
                        destRow[(column * 2) + 1] = vRow[column];
                    }
                }
            }
        }

        return true;
    }
}

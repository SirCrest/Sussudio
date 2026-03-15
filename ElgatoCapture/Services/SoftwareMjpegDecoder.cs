using System;
using System.Runtime.InteropServices;
using System.Threading;
using FFmpeg.AutoGen;

namespace ElgatoCapture.Services;

internal sealed class SoftwareMjpegDecoderPermanentException : InvalidOperationException
{
    public SoftwareMjpegDecoderPermanentException(string message)
        : base(message)
    {
    }
}

/// <summary>
/// FFmpeg software MJPEG decoder with YUVJ420P->NV12 conversion.
/// Each instance owns its own AVCodecContext and is NOT thread-safe.
/// </summary>
internal sealed unsafe class SoftwareMjpegDecoder : IDisposable
{
    private AVCodecContext* _decoderCtx;
    private AVFrame* _decodedFrame;
    private AVFrame* _drainFrame;
    private AVPacket* _packet;
    private int _nv12Size;
    private int _width;
    private int _height;
    private bool _initialized;
    private bool _disposed;
    private int _diagDone;

    public int Width => _width;
    public int Height => _height;
    public int Nv12Size => _nv12Size;

    public void Initialize(int width, int height)
    {
        if (_initialized)
        {
            throw new InvalidOperationException("SoftwareMjpegDecoder is already initialized.");
        }

        if (width <= 0 || height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), "Width and height must be positive.");
        }

        LibAvEncoder.InitializeFFmpeg(requireNativeRuntime: true);

        var codec = ffmpeg.avcodec_find_decoder_by_name("mjpeg");
        if (codec == null)
        {
            throw new InvalidOperationException("mjpeg decoder not found.");
        }

        var decoderCtx = ffmpeg.avcodec_alloc_context3(codec);
        if (decoderCtx == null)
        {
            throw new InvalidOperationException("Failed to allocate mjpeg decoder context.");
        }

        try
        {
            decoderCtx->width = width;
            decoderCtx->height = height;
            decoderCtx->thread_count = 1;

            var openResult = ffmpeg.avcodec_open2(decoderCtx, codec, null);
            if (openResult < 0)
            {
                throw new InvalidOperationException(
                    $"avcodec_open2(mjpeg) failed: code={openResult} msg='{GetErrorString(openResult)}'");
            }

            _decodedFrame = ffmpeg.av_frame_alloc();
            if (_decodedFrame == null)
            {
                throw new InvalidOperationException("Failed to allocate decoded frame.");
            }

            _drainFrame = ffmpeg.av_frame_alloc();
            if (_drainFrame == null)
            {
                throw new InvalidOperationException("Failed to allocate drain frame.");
            }

            _packet = ffmpeg.av_packet_alloc();
            if (_packet == null)
            {
                throw new InvalidOperationException("Failed to allocate packet.");
            }

            _decoderCtx = decoderCtx;
            _width = width;
            _height = height;
            _nv12Size = checked(width * height * 3 / 2);
            _initialized = true;

            decoderCtx = null;

            Logger.Log($"SW_MJPEG_DECODER_INIT width={width} height={height} codec=mjpeg");
        }
        catch
        {
            if (decoderCtx != null)
            {
                ffmpeg.avcodec_free_context(&decoderCtx);
            }

            Dispose();
            throw;
        }
    }

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

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (_packet != null)
        {
            var packet = _packet;
            ffmpeg.av_packet_free(&packet);
            _packet = null;
        }

        if (_decodedFrame != null)
        {
            var decodedFrame = _decodedFrame;
            ffmpeg.av_frame_free(&decodedFrame);
            _decodedFrame = null;
        }

        if (_drainFrame != null)
        {
            var drainFrame = _drainFrame;
            ffmpeg.av_frame_free(&drainFrame);
            _drainFrame = null;
        }

        if (_decoderCtx != null)
        {
            var decoderCtx = _decoderCtx;
            ffmpeg.avcodec_free_context(&decoderCtx);
            _decoderCtx = null;
        }

        _initialized = false;
        Logger.Log("SW_MJPEG_DECODER_DISPOSED");
    }

    private static string GetErrorString(int errorCode)
    {
        var buffer = stackalloc byte[ffmpeg.AV_ERROR_MAX_STRING_SIZE];
        ffmpeg.av_strerror(errorCode, buffer, (ulong)ffmpeg.AV_ERROR_MAX_STRING_SIZE);
        return Marshal.PtrToStringAnsi((IntPtr)buffer) ?? $"unknown error {errorCode}";
    }
}

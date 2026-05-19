using System;
using System.Runtime.InteropServices;
using FFmpeg.AutoGen;
using Sussudio.Services.Runtime;

namespace Sussudio.Services.Gpu;

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
internal sealed unsafe partial class SoftwareMjpegDecoder : IDisposable
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

        FfmpegRuntimeInit.EnsureInitialized(requireNativeRuntime: true);

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

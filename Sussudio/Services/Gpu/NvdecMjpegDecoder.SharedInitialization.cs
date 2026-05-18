using System;
using FFmpeg.AutoGen;
using Sussudio.Services.Runtime;

namespace Sussudio.Services.Gpu;

internal sealed unsafe partial class NvdecMjpegDecoder
{
    public void Initialize(int width, int height, AVBufferRef* sharedHwDeviceCtx, AVBufferRef* sharedHwFramesCtx)
    {
        if (_initialized)
        {
            throw new InvalidOperationException("NvdecMjpegDecoder is already initialized.");
        }

        if (width <= 0 || height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), "Width and height must be positive.");
        }

        if (sharedHwDeviceCtx == null)
        {
            throw new ArgumentNullException(nameof(sharedHwDeviceCtx));
        }

        if (sharedHwFramesCtx == null)
        {
            throw new ArgumentNullException(nameof(sharedHwFramesCtx));
        }

        FfmpegRuntimeInit.EnsureInitialized(requireNativeRuntime: true);

        var codec = ffmpeg.avcodec_find_decoder_by_name("mjpeg_cuvid");
        if (codec == null)
        {
            throw new InvalidOperationException("mjpeg_cuvid decoder not found. NVDEC JPEG decode requires NVIDIA GPU + CUDA-enabled FFmpeg.");
        }

        var decoderCtx = ffmpeg.avcodec_alloc_context3(codec);
        if (decoderCtx == null)
        {
            throw new InvalidOperationException("Failed to allocate mjpeg_cuvid decoder context.");
        }

        AVBufferRef* hwDeviceCtx = null;
        AVBufferRef* hwFramesCtx = null;

        try
        {
            decoderCtx->width = width;
            decoderCtx->height = height;

            decoderCtx->hw_device_ctx = ffmpeg.av_buffer_ref(sharedHwDeviceCtx);
            if (decoderCtx->hw_device_ctx == null)
            {
                throw new InvalidOperationException("Failed to reference shared CUDA device context for decoder.");
            }

            decoderCtx->hw_frames_ctx = ffmpeg.av_buffer_ref(sharedHwFramesCtx);
            if (decoderCtx->hw_frames_ctx == null)
            {
                throw new InvalidOperationException("Failed to reference shared CUDA frames context for decoder.");
            }

            decoderCtx->extra_hw_frames = 16;

            var openResult = ffmpeg.avcodec_open2(decoderCtx, codec, null);
            if (openResult < 0)
            {
                throw new InvalidOperationException(
                    $"avcodec_open2(mjpeg_cuvid) failed: code={openResult} msg='{GetErrorString(openResult)}'");
            }

            hwDeviceCtx = ffmpeg.av_buffer_ref(sharedHwDeviceCtx);
            if (hwDeviceCtx == null)
            {
                throw new InvalidOperationException("Failed to retain shared CUDA device context for decoder.");
            }

            hwFramesCtx = ffmpeg.av_buffer_ref(sharedHwFramesCtx);
            if (hwFramesCtx == null)
            {
                throw new InvalidOperationException("Failed to retain shared CUDA frames context for decoder.");
            }

            _decodedFrame = ffmpeg.av_frame_alloc();
            if (_decodedFrame == null)
            {
                throw new InvalidOperationException("Failed to allocate decoded frame.");
            }

            _cpuFrame = ffmpeg.av_frame_alloc();
            if (_cpuFrame == null)
            {
                throw new InvalidOperationException("Failed to allocate CPU frame.");
            }

            _cpuFrame->format = (int)AVPixelFormat.AV_PIX_FMT_NV12;
            _cpuFrame->width = width;
            _cpuFrame->height = height;
            var cpuBufferResult = ffmpeg.av_frame_get_buffer(_cpuFrame, 32);
            if (cpuBufferResult < 0)
            {
                throw new InvalidOperationException(
                    $"av_frame_get_buffer(cpu) failed: code={cpuBufferResult} msg='{GetErrorString(cpuBufferResult)}'");
            }

            _packet = ffmpeg.av_packet_alloc();
            if (_packet == null)
            {
                throw new InvalidOperationException("Failed to allocate packet.");
            }

            _decoderCtx = decoderCtx;
            _hwDeviceCtx = hwDeviceCtx;
            _hwFramesCtx = hwFramesCtx;
            _width = width;
            _height = height;
            _initialized = true;

            hwDeviceCtx = null;
            hwFramesCtx = null;
            decoderCtx = null;

            Logger.Log($"NVDEC_MJPEG_DECODER_INIT_SHARED width={width} height={height} codec=mjpeg_cuvid");
        }
        catch
        {
            if (hwFramesCtx != null)
            {
                ffmpeg.av_buffer_unref(&hwFramesCtx);
            }

            if (hwDeviceCtx != null)
            {
                ffmpeg.av_buffer_unref(&hwDeviceCtx);
            }

            if (decoderCtx != null)
            {
                ffmpeg.avcodec_free_context(&decoderCtx);
            }

            Dispose();
            throw;
        }
    }
}

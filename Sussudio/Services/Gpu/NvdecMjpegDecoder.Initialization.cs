using System;
using System.Runtime.InteropServices;
using FFmpeg.AutoGen;
using Sussudio.Services.Runtime;

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

    public void Initialize(int width, int height)
    {
        if (_initialized)
        {
            throw new InvalidOperationException("NvdecMjpegDecoder is already initialized.");
        }

        if (width <= 0 || height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), "Width and height must be positive.");
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

        try
        {
            decoderCtx->width = width;
            decoderCtx->height = height;

            // Create CUDA hw device context — av_hwdevice_ctx_create does the full init:
            // cuInit(0), cuDeviceGet, cuCtxCreate + DLL loading.
            // (av_hwdevice_ctx_alloc + av_hwdevice_ctx_init only loads DLLs, no cuInit!)
            var createResult = ffmpeg.av_hwdevice_ctx_create(&hwDeviceCtx, AVHWDeviceType.AV_HWDEVICE_TYPE_CUDA, null, null, 0);
            if (createResult < 0)
            {
                throw new InvalidOperationException(
                    $"av_hwdevice_ctx_create(CUDA) failed: code={createResult} msg='{GetErrorString(createResult)}'");
            }

            Logger.Log($"NVDEC_MJPEG_CUDA_DEVICE_OK width={width} height={height}");

            // Pre-create hw_frames_ctx for the shared CUDA surface pool.
            // Now that CUDA is properly initialized via av_hwdevice_ctx_create,
            // av_hwframe_ctx_init can allocate CUDA surfaces successfully.
            var hwFramesRef = ffmpeg.av_hwframe_ctx_alloc(hwDeviceCtx);
            if (hwFramesRef == null)
            {
                throw new InvalidOperationException("Failed to allocate CUDA frames context.");
            }

            var framesCtx = (AVHWFramesContext*)hwFramesRef->data;
            framesCtx->format = AVPixelFormat.AV_PIX_FMT_CUDA;
            framesCtx->sw_format = AVPixelFormat.AV_PIX_FMT_NV12;
            framesCtx->width = width;
            framesCtx->height = height;
            framesCtx->initial_pool_size = 40;

            var framesInitResult = ffmpeg.av_hwframe_ctx_init(hwFramesRef);
            if (framesInitResult < 0)
            {
                ffmpeg.av_buffer_unref(&hwFramesRef);
                throw new InvalidOperationException(
                    $"av_hwframe_ctx_init(CUDA) failed: code={framesInitResult} msg='{GetErrorString(framesInitResult)}'");
            }

            Logger.Log(
                $"NVDEC_MJPEG_FRAMES_CTX_OK fmt={(int)framesCtx->format} sw_fmt={(int)framesCtx->sw_format} " +
                $"w={framesCtx->width} h={framesCtx->height} pool={framesCtx->initial_pool_size}");

            // Set both hw_device_ctx and hw_frames_ctx on the decoder before opening
            decoderCtx->hw_device_ctx = ffmpeg.av_buffer_ref(hwDeviceCtx);
            if (decoderCtx->hw_device_ctx == null)
            {
                ffmpeg.av_buffer_unref(&hwFramesRef);
                throw new InvalidOperationException("Failed to reference CUDA device context for decoder.");
            }

            decoderCtx->hw_frames_ctx = ffmpeg.av_buffer_ref(hwFramesRef);
            if (decoderCtx->hw_frames_ctx == null)
            {
                ffmpeg.av_buffer_unref(&hwFramesRef);
                throw new InvalidOperationException("Failed to reference CUDA frames context for decoder.");
            }

            decoderCtx->extra_hw_frames = 16;

            var openResult = ffmpeg.avcodec_open2(decoderCtx, codec, null);
            if (openResult < 0)
            {
                ffmpeg.av_buffer_unref(&hwFramesRef);
                throw new InvalidOperationException(
                    $"avcodec_open2(mjpeg_cuvid) failed: code={openResult} msg='{GetErrorString(openResult)}'");
            }

            // Keep our own reference to the frames context for the encoder
            var framesRef = hwFramesRef;
            hwFramesRef = null; // ownership transferred

            _decodedFrame = ffmpeg.av_frame_alloc();
            if (_decodedFrame == null)
            {
                ffmpeg.av_buffer_unref(&framesRef);
                throw new InvalidOperationException("Failed to allocate decoded frame.");
            }

            _cpuFrame = ffmpeg.av_frame_alloc();
            if (_cpuFrame == null)
            {
                ffmpeg.av_buffer_unref(&framesRef);
                throw new InvalidOperationException("Failed to allocate CPU frame.");
            }

            _cpuFrame->format = (int)AVPixelFormat.AV_PIX_FMT_NV12;
            _cpuFrame->width = width;
            _cpuFrame->height = height;
            var cpuBufferResult = ffmpeg.av_frame_get_buffer(_cpuFrame, 32);
            if (cpuBufferResult < 0)
            {
                ffmpeg.av_buffer_unref(&framesRef);
                throw new InvalidOperationException(
                    $"av_frame_get_buffer(cpu) failed: code={cpuBufferResult} msg='{GetErrorString(cpuBufferResult)}'");
            }

            _packet = ffmpeg.av_packet_alloc();
            if (_packet == null)
            {
                ffmpeg.av_buffer_unref(&framesRef);
                throw new InvalidOperationException("Failed to allocate packet.");
            }

            _decoderCtx = decoderCtx;
            _hwDeviceCtx = hwDeviceCtx;
            _hwFramesCtx = framesRef;
            _width = width;
            _height = height;
            _initialized = true;

            hwDeviceCtx = null;
            decoderCtx = null;

            Logger.Log($"NVDEC_MJPEG_DECODER_INIT width={width} height={height} codec=mjpeg_cuvid");
        }
        catch
        {
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

        if (_cpuFrame != null)
        {
            var cpuFrame = _cpuFrame;
            ffmpeg.av_frame_free(&cpuFrame);
            _cpuFrame = null;
        }

        if (_decoderCtx != null)
        {
            var decoderCtx = _decoderCtx;
            ffmpeg.avcodec_free_context(&decoderCtx);
            _decoderCtx = null;
        }

        if (_hwFramesCtx != null)
        {
            var hwFramesCtx = _hwFramesCtx;
            ffmpeg.av_buffer_unref(&hwFramesCtx);
            _hwFramesCtx = null;
        }

        if (_hwDeviceCtx != null)
        {
            var hwDeviceCtx = _hwDeviceCtx;
            ffmpeg.av_buffer_unref(&hwDeviceCtx);
            _hwDeviceCtx = null;
        }

        if (_packedCpuBuffer != IntPtr.Zero)
        {
            Marshal.FreeHGlobal(_packedCpuBuffer);
            _packedCpuBuffer = IntPtr.Zero;
            _packedCpuBufferSize = 0;
        }

        _initialized = false;
        Logger.Log("NVDEC_MJPEG_DECODER_DISPOSED");
    }

    private static string GetErrorString(int errorCode)
    {
        var buffer = stackalloc byte[ffmpeg.AV_ERROR_MAX_STRING_SIZE];
        ffmpeg.av_strerror(errorCode, buffer, (ulong)ffmpeg.AV_ERROR_MAX_STRING_SIZE);
        return Marshal.PtrToStringAnsi((IntPtr)buffer) ?? $"unknown error {errorCode}";
    }

}

using System;
using System.Runtime.InteropServices;
using System.Threading;
using FFmpeg.AutoGen;

namespace ElgatoCapture.Services;

/// <summary>
/// FFmpeg mjpeg_cuvid decoder using NVIDIA's NVDEC hardware JPEG decode engine.
/// Supports either owning its CUDA contexts or retaining shared contexts provided by the caller.
/// </summary>
internal sealed unsafe class NvdecMjpegDecoder : IDisposable
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

        LibAvEncoder.InitializeFFmpeg();

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

        LibAvEncoder.InitializeFFmpeg();

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

    /// <summary>
    /// Download a CUDA frame to packed CPU NV12 bytes for preview rendering.
    /// </summary>
    public bool TryDownloadToCpu(AVFrame* cudaFrame, out IntPtr nv12Data, out int nv12Size)
    {
        nv12Data = IntPtr.Zero;
        nv12Size = 0;

        if (cudaFrame == null || _cpuFrame == null)
        {
            return false;
        }

        var writableResult = ffmpeg.av_frame_make_writable(_cpuFrame);
        if (writableResult < 0)
        {
            Logger.Log($"NVDEC_CPU_MAKE_WRITABLE_FAIL code={writableResult} msg='{GetErrorString(writableResult)}'");
            return false;
        }

        var transferResult = ffmpeg.av_hwframe_transfer_data(_cpuFrame, cudaFrame, 0);
        if (transferResult < 0)
        {
            Logger.Log($"NVDEC_CPU_TRANSFER_FAIL code={transferResult} msg='{GetErrorString(transferResult)}'");
            return false;
        }

        // One-shot diagnostic: log frame formats and strides after first download
        if (Interlocked.Exchange(ref _downloadDiagDone, 1) == 0)
        {
            Logger.Log(
                $"NVDEC_CPU_DOWNLOAD_DIAG cuda_fmt={(AVPixelFormat)cudaFrame->format} cuda_w={cudaFrame->width} cuda_h={cudaFrame->height} " +
                $"cpu_fmt={(AVPixelFormat)_cpuFrame->format} cpu_w={_cpuFrame->width} cpu_h={_cpuFrame->height} " +
                $"y_stride={_cpuFrame->linesize[0]} uv_stride={_cpuFrame->linesize[1]} " +
                $"y_data=0x{(long)_cpuFrame->data[0]:X} uv_data=0x{(long)_cpuFrame->data[1]:X} " +
                $"y_uv_gap={(_cpuFrame->data[1] - _cpuFrame->data[0])} expected_y_size={_cpuFrame->linesize[0] * _cpuFrame->height}");
        }

        var packedSize = checked((_width * _height * 3) / 2);
        var yStride = _cpuFrame->linesize[0];
        var uvStride = _cpuFrame->linesize[1];
        var yPlaneSize = yStride * _height;
        var uvPlaneExpectedStart = _cpuFrame->data[0] + yPlaneSize;

        // Fast path: only when stride matches width AND planes are truly contiguous
        // (av_frame_get_buffer may add alignment padding between Y and UV planes)
        if (yStride == _width && uvStride == _width && _cpuFrame->data[1] == uvPlaneExpectedStart)
        {
            nv12Data = (IntPtr)_cpuFrame->data[0];
            nv12Size = packedSize;
            return true;
        }

        // Slow path: copy both planes to a packed contiguous buffer
        EnsurePackedBufferCapacity(packedSize);

        var destination = (byte*)_packedCpuBuffer;
        CopyPlane(_cpuFrame->data[0], yStride, destination, _width, _width, _height);
        CopyPlane(
            _cpuFrame->data[1],
            uvStride,
            destination + (_width * _height),
            _width,
            _width,
            _height / 2);

        nv12Data = _packedCpuBuffer;
        nv12Size = packedSize;
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

    private void EnsurePackedBufferCapacity(int requiredSize)
    {
        if (_packedCpuBufferSize >= requiredSize && _packedCpuBuffer != IntPtr.Zero)
        {
            return;
        }

        if (_packedCpuBuffer != IntPtr.Zero)
        {
            Marshal.FreeHGlobal(_packedCpuBuffer);
        }

        _packedCpuBuffer = Marshal.AllocHGlobal(requiredSize);
        _packedCpuBufferSize = requiredSize;
    }

    private static void CopyPlane(byte* source, int sourceStride, byte* destination, int destinationStride, int rowBytes, int rowCount)
    {
        for (var row = 0; row < rowCount; row++)
        {
            Buffer.MemoryCopy(
                source + (row * sourceStride),
                destination + (row * destinationStride),
                rowBytes,
                rowBytes);
        }
    }

    private static string GetErrorString(int errorCode)
    {
        var buffer = stackalloc byte[ffmpeg.AV_ERROR_MAX_STRING_SIZE];
        ffmpeg.av_strerror(errorCode, buffer, (ulong)ffmpeg.AV_ERROR_MAX_STRING_SIZE);
        return Marshal.PtrToStringAnsi((IntPtr)buffer) ?? $"unknown error {errorCode}";
    }
}

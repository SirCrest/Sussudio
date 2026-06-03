using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using FFmpeg.AutoGen;

namespace Sussudio.Services.Recording;

internal sealed unsafe partial class LibAvEncoder
{
    /// <summary>
    /// Creates a single D3D11 Texture2D (ArraySize=1) via raw vtable call.
    /// Returns the texture pointer (caller owns the reference) or IntPtr.Zero on failure.
    /// </summary>
    private static IntPtr CreateSingleTexture2D(IntPtr d3d11Device, int width, int height, bool isP010, uint bindFlags)
    {
        // D3D11_TEXTURE2D_DESC layout (44 bytes):
        // 0: Width(4) 4: Height(4) 8: MipLevels(4) 12: ArraySize(4)
        // 16: Format(4) 20: SampleDesc.Count(4) 24: SampleDesc.Quality(4)
        // 28: Usage(4) 32: BindFlags(4) 36: CPUAccessFlags(4) 40: MiscFlags(4)
        var texDesc = stackalloc byte[44];
        new Span<byte>(texDesc, 44).Clear();
        *(uint*)(texDesc + 0) = (uint)width;
        *(uint*)(texDesc + 4) = (uint)height;
        *(uint*)(texDesc + 8) = 1; // MipLevels
        *(uint*)(texDesc + 12) = 1; // ArraySize — individual textures, not array
        *(uint*)(texDesc + 16) = isP010 ? 104u : 103u; // DXGI_FORMAT_P010=104, DXGI_FORMAT_NV12=103
        *(uint*)(texDesc + 20) = 1; // SampleDesc.Count
        *(uint*)(texDesc + 28) = 0; // D3D11_USAGE_DEFAULT
        *(uint*)(texDesc + 32) = bindFlags;

        // ID3D11Device vtable slot 5 = CreateTexture2D
        var vtable = *(IntPtr*)d3d11Device;
        var createTexture2DPtr = *(IntPtr*)(vtable + 5 * IntPtr.Size);
        IntPtr ppTexture = IntPtr.Zero;
        var hr = ((delegate* unmanaged[Stdcall]<IntPtr, byte*, IntPtr, IntPtr*, int>)createTexture2DPtr)(
            d3d11Device, texDesc, IntPtr.Zero, &ppTexture);

        if (hr < 0)
        {
            Logger.Log($"LIBAV_ENCODER_HW_FRAMES_CREATE_TEX_FAIL hr=0x{unchecked((uint)hr):X8} w={width} h={height}");
            return IntPtr.Zero;
        }

        return ppTexture;
    }

    private void InitializeHardwareFramesIfNeeded(LibAvEncoderOptions options)
    {
        if (options.CudaHwDeviceCtxPtr != IntPtr.Zero && options.CudaHwFramesCtxPtr != IntPtr.Zero)
        {
            InitializeCudaHardwareFrames(options);
            return;
        }

        if (options.D3D11DevicePtr == IntPtr.Zero)
        {
            Logger.Log("LIBAV_ENCODER_HW_FRAMES skip=no_device");
            return;
        }

        if (options.D3D11DeviceContextPtr == IntPtr.Zero)
        {
            Logger.Log("LIBAV_ENCODER_HW_FRAMES skip=no_device_context");
            return;
        }

        AVBufferRef* hwDeviceCtx = null;
        AVBufferRef* hwFramesCtx = null;
        AVBufferRef* codecHwDeviceCtx = null;
        AVBufferRef* codecHwFramesCtx = null;
        var stage = "av_hwdevice_ctx_alloc";

        try
        {
            hwDeviceCtx = ffmpeg.av_hwdevice_ctx_alloc(AVHWDeviceType.AV_HWDEVICE_TYPE_D3D11VA);
            if (hwDeviceCtx == null)
            {
                throw new InvalidOperationException("Failed to allocate D3D11VA device context.");
            }

            stage = "av_hwdevice_ctx_init";
            var hwDeviceCtxData = (AVHWDeviceContext*)hwDeviceCtx->data;
            var d3d11vaDeviceCtx = (AVD3D11VADeviceContext*)hwDeviceCtxData->hwctx;
            d3d11vaDeviceCtx->device = (FFmpeg.AutoGen.ID3D11Device*)options.D3D11DevicePtr;
            d3d11vaDeviceCtx->device_context = (FFmpeg.AutoGen.ID3D11DeviceContext*)options.D3D11DeviceContextPtr;

            var initResult = ffmpeg.av_hwdevice_ctx_init(hwDeviceCtx);
            if (initResult < 0)
            {
                throw new InvalidOperationException($"code={initResult} (0x{unchecked((uint)initResult):X8}) msg='{GetErrorString(initResult)}'");
            }

            stage = "av_hwframe_ctx_alloc";
            hwFramesCtx = ffmpeg.av_hwframe_ctx_alloc(hwDeviceCtx);
            if (hwFramesCtx == null)
            {
                throw new InvalidOperationException("Failed to allocate hardware frames context.");
            }

            const int poolSize = 8;
            const uint bindFlags = 0x20; // D3D11_BIND_RENDER_TARGET — required by NVENC

            var framesCtx = (AVHWFramesContext*)hwFramesCtx->data;
            framesCtx->format = AVPixelFormat.AV_PIX_FMT_D3D11;
            framesCtx->sw_format = options.IsP010 ? AVPixelFormat.AV_PIX_FMT_P010LE : AVPixelFormat.AV_PIX_FMT_NV12;
            framesCtx->width = options.Width;
            framesCtx->height = options.Height;
            // initial_pool_size = 0: skip FFmpeg's internal pool. NV12/P010 texture arrays
            // (ArraySize>1) fail with E_INVALIDARG on some GPUs, and FFmpeg's pool mechanism
            // doesn't work with externally-provided individual textures. We manage our own
            // pool of ArraySize=1 textures and construct AVFrames manually in SendGpuVideoFrame.
            framesCtx->initial_pool_size = 0;

            var d3d11FramesCtx = (AVD3D11VAFramesContext*)framesCtx->hwctx;
            d3d11FramesCtx->BindFlags = bindFlags;

            stage = "av_hwframe_ctx_init";
            var framesInitResult = ffmpeg.av_hwframe_ctx_init(hwFramesCtx);
            if (framesInitResult < 0)
            {
                throw new InvalidOperationException(
                    $"code={framesInitResult} (0x{unchecked((uint)framesInitResult):X8}) " +
                    $"msg='{GetErrorString(framesInitResult)}'");
            }

            // Pre-create individual ArraySize=1 textures for our own pool
            stage = "pre_create_pool_textures";
            var poolTextures = new IntPtr[poolSize];
            for (var i = 0; i < poolSize; i++)
            {
                var tex = CreateSingleTexture2D(
                    options.D3D11DevicePtr, options.Width, options.Height, options.IsP010, bindFlags);
                if (tex == IntPtr.Zero)
                {
                    for (var j = 0; j < i; j++) Marshal.Release(poolTextures[j]);
                    throw new InvalidOperationException(
                        $"CreateTexture2D failed for pool texture {i} " +
                        $"(w={options.Width} h={options.Height} fmt={(options.IsP010 ? "P010" : "NV12")})");
                }
                poolTextures[i] = tex;
            }

            _hwPoolTextures = poolTextures;
            _hwPoolIndex = 0;

            Logger.Log(
                $"LIBAV_ENCODER_HW_FRAMES_POOL " +
                $"created {poolSize} individual textures, pool_bypass=true " +
                $"(w={options.Width} h={options.Height} fmt={(options.IsP010 ? "P010" : "NV12")} " +
                $"bindFlags=0x{bindFlags:X})");

            stage = "av_buffer_ref(hw_device_ctx)";
            codecHwDeviceCtx = ffmpeg.av_buffer_ref(hwDeviceCtx);
            if (codecHwDeviceCtx == null)
            {
                throw new InvalidOperationException("Failed to reference hardware device context.");
            }

            stage = "av_buffer_ref(hw_frames_ctx)";
            codecHwFramesCtx = ffmpeg.av_buffer_ref(hwFramesCtx);
            if (codecHwFramesCtx == null)
            {
                throw new InvalidOperationException("Failed to reference hardware frames context.");
            }

            _videoCodecCtx->hw_device_ctx = codecHwDeviceCtx;
            codecHwDeviceCtx = null;
            _videoCodecCtx->hw_frames_ctx = codecHwFramesCtx;
            codecHwFramesCtx = null;
            _videoCodecCtx->pix_fmt = AVPixelFormat.AV_PIX_FMT_D3D11;

            _hwDeviceCtx = hwDeviceCtx;
            hwDeviceCtx = null;
            _hwFramesCtx = hwFramesCtx;
            hwFramesCtx = null;
            _useHardwareFrames = true;
            _useCudaHardwareFrames = false;
            Logger.Log(
                $"LIBAV_ENCODER_HW_FRAMES mode=d3d11va sw_format={(options.IsP010 ? "p010le" : "nv12")} " +
                $"pool_size=8 width={options.Width} height={options.Height}");
        }
        catch (Exception ex)
        {
            if (codecHwFramesCtx != null)
            {
                ffmpeg.av_buffer_unref(&codecHwFramesCtx);
            }

            if (codecHwDeviceCtx != null)
            {
                ffmpeg.av_buffer_unref(&codecHwDeviceCtx);
            }

            if (hwFramesCtx != null)
            {
                ffmpeg.av_buffer_unref(&hwFramesCtx);
            }

            if (hwDeviceCtx != null)
            {
                ffmpeg.av_buffer_unref(&hwDeviceCtx);
            }

            // Release pool textures if we created them but failed at a later stage
            if (_hwPoolTextures != null)
            {
                for (var i = 0; i < _hwPoolTextures.Length; i++)
                {
                    if (_hwPoolTextures[i] != IntPtr.Zero)
                    {
                        Marshal.Release(_hwPoolTextures[i]);
                    }
                }
                _hwPoolTextures = null;
            }

            _videoCodecCtx->pix_fmt = options.IsP010 ? AVPixelFormat.AV_PIX_FMT_P010LE : AVPixelFormat.AV_PIX_FMT_NV12;
            _useHardwareFrames = false;
            _useCudaHardwareFrames = false;
            Logger.Log($"LIBAV_ENCODER_HW_FRAMES_WARN stage={stage} msg='{ex.Message}' fallback=software");
        }
    }

    private void InitializeCudaHardwareFrames(LibAvEncoderOptions options)
    {
        AVBufferRef* codecHwDeviceCtx = null;
        AVBufferRef* codecHwFramesCtx = null;
        var stage = "av_buffer_ref(cuda_device)";

        try
        {
            codecHwDeviceCtx = ffmpeg.av_buffer_ref((AVBufferRef*)options.CudaHwDeviceCtxPtr);
            if (codecHwDeviceCtx == null)
            {
                throw new InvalidOperationException("Failed to reference CUDA device context for encoder.");
            }

            stage = "av_buffer_ref(cuda_frames)";
            codecHwFramesCtx = ffmpeg.av_buffer_ref((AVBufferRef*)options.CudaHwFramesCtxPtr);
            if (codecHwFramesCtx == null)
            {
                throw new InvalidOperationException("Failed to reference CUDA frames context for encoder.");
            }

            _videoCodecCtx->hw_device_ctx = codecHwDeviceCtx;
            codecHwDeviceCtx = null;
            _videoCodecCtx->hw_frames_ctx = codecHwFramesCtx;
            codecHwFramesCtx = null;
            _videoCodecCtx->pix_fmt = AVPixelFormat.AV_PIX_FMT_CUDA;

            _hwDeviceCtx = ffmpeg.av_buffer_ref((AVBufferRef*)options.CudaHwDeviceCtxPtr);
            if (_hwDeviceCtx == null)
            {
                throw new InvalidOperationException("Failed to retain CUDA device context for encoder.");
            }

            _hwFramesCtx = ffmpeg.av_buffer_ref((AVBufferRef*)options.CudaHwFramesCtxPtr);
            if (_hwFramesCtx == null)
            {
                throw new InvalidOperationException("Failed to retain CUDA frames context for encoder.");
            }

            _useHardwareFrames = true;
            _useCudaHardwareFrames = true;

            Logger.Log(
                $"LIBAV_ENCODER_HW_FRAMES mode=cuda sw_format=nv12 width={options.Width} height={options.Height}");
        }
        catch (Exception ex)
        {
            if (_videoCodecCtx->hw_frames_ctx != null)
            {
                ffmpeg.av_buffer_unref(&_videoCodecCtx->hw_frames_ctx);
            }

            if (_videoCodecCtx->hw_device_ctx != null)
            {
                ffmpeg.av_buffer_unref(&_videoCodecCtx->hw_device_ctx);
            }

            if (codecHwFramesCtx != null)
            {
                ffmpeg.av_buffer_unref(&codecHwFramesCtx);
            }

            if (codecHwDeviceCtx != null)
            {
                ffmpeg.av_buffer_unref(&codecHwDeviceCtx);
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

            _videoCodecCtx->pix_fmt = options.IsP010 ? AVPixelFormat.AV_PIX_FMT_P010LE : AVPixelFormat.AV_PIX_FMT_NV12;
            _useHardwareFrames = false;
            _useCudaHardwareFrames = false;
            Logger.Log($"LIBAV_ENCODER_HW_FRAMES_WARN stage={stage} msg='{ex.Message}' fallback=software");
        }
    }

    public void SendGpuVideoFrame(IntPtr d3d11Texture, int subresourceIndex)
    {
        EnsureOpen();

        if (d3d11Texture == IntPtr.Zero)
        {
            throw new ArgumentException("D3D11 texture pointer is null.", nameof(d3d11Texture));
        }

        if (!_useHardwareFrames || _useCudaHardwareFrames || _hwFramesCtx == null || _hwFrame == null || _hwPoolTextures == null)
        {
            throw new InvalidOperationException("Hardware frames are not initialized. Use SendVideoFrame for CPU path.");
        }

        var options = _options ?? throw new InvalidOperationException("Encoder options are not initialized.");

        // Pick a pool texture via round-robin. With 8 textures and NVENC pipeline depth
        // of ~4 frames, we never wrap around while the encoder still references a texture.
        var poolTextures = _hwPoolTextures;
        var poolTexture = poolTextures[_hwPoolIndex & (poolTextures.Length - 1)];
        _hwPoolIndex++;

        // GPU-GPU copy: source reader texture to our pool texture (ArraySize=1, subresource=0).
        var deviceCtx = (void*)options.D3D11DeviceContextPtr;
        if (deviceCtx == null)
        {
            throw new InvalidOperationException("D3D11 device context is null.");
        }

        var vtable = *(void***)deviceCtx;
        var copySubresourceRegion = (delegate* unmanaged[Stdcall]<void*, void*, uint, uint, uint, uint, void*, uint, void*, void>)vtable[46];
        copySubresourceRegion(
            deviceCtx,
            (void*)poolTexture,
            0, // destination subresource = 0 (ArraySize=1)
            0, 0, 0,
            (void*)d3d11Texture,
            (uint)subresourceIndex,
            null);

        // CopySubresourceRegion is void; after a TDR (GPU device-removed), the call
        // silently no-ops and subsequent frames encode from stale/garbage texture data.
        // Proactively check device health to fail fast rather than corrupt the recording.
        CheckDeviceRemoved(options.D3D11DevicePtr);

        // Construct the AVFrame manually (bypass FFmpeg's pool which doesn't support
        // individual textures). The pool texture outlives the frame, so use no-op free.
        ffmpeg.av_frame_unref(_hwFrame);
        _hwFrame->format = (int)AVPixelFormat.AV_PIX_FMT_D3D11;
        _hwFrame->width = options.Width;
        _hwFrame->height = options.Height;
        _hwFrame->hw_frames_ctx = ffmpeg.av_buffer_ref(_hwFramesCtx);
        _hwFrame->data[0] = (byte*)poolTexture;
        _hwFrame->data[1] = (byte*)(nint)0; // subresource index = 0
        // Create a buffer ref with no-op free so av_frame_unref doesn't release our pool texture
        _hwFrame->buf[0] = ffmpeg.av_buffer_create(
            (byte*)poolTexture, 0, HwPoolTextureFree, null, 0);

        if (_forceNextKeyframe)
        {
            _hwFrame->pict_type = AVPictureType.AV_PICTURE_TYPE_I;
            _forceNextKeyframe = false;
        }

        _hwFrame->pts = Interlocked.Increment(ref _nextVideoPts) - 1;
        LogAvSyncIfDue();

        var attachedHdrSideData = false;
        if (options.HdrEnabled && _encodedFrameCount == 0)
        {
            attachedHdrSideData = AttachHdrFrameSideDataToHwFrame(options);
        }

        try
        {
            var sendResult = ffmpeg.avcodec_send_frame(_videoCodecCtx, _hwFrame);
            if (sendResult == ffmpeg.AVERROR(ffmpeg.EAGAIN))
            {
                DrainEncoderPackets();
                sendResult = ffmpeg.avcodec_send_frame(_videoCodecCtx, _hwFrame);
            }

            ThrowIfError(sendResult, "avcodec_send_frame(hw)");
            DrainEncoderPackets();
            _encodedFrameCount++;
        }
        catch
        {
            _droppedFrameCount++;
            throw;
        }
        finally
        {
            if (attachedHdrSideData)
            {
                ffmpeg.av_frame_remove_side_data(_hwFrame, AVFrameSideDataType.AV_FRAME_DATA_MASTERING_DISPLAY_METADATA);
                ffmpeg.av_frame_remove_side_data(_hwFrame, AVFrameSideDataType.AV_FRAME_DATA_CONTENT_LIGHT_LEVEL);
            }

            ffmpeg.av_frame_unref(_hwFrame);
        }
    }

    public void SendCudaVideoFrame(AVFrame* decodedFrame)
    {
        EnsureOpen();

        if (!_useCudaHardwareFrames || _hwFrame == null)
        {
            throw new InvalidOperationException("CUDA hardware frames are not initialized.");
        }

        if (decodedFrame == null)
        {
            throw new ArgumentNullException(nameof(decodedFrame));
        }

        var options = _options ?? throw new InvalidOperationException("Encoder options are not initialized.");

        ffmpeg.av_frame_unref(_hwFrame);
        var refResult = ffmpeg.av_frame_ref(_hwFrame, decodedFrame);
        if (refResult < 0)
        {
            throw new InvalidOperationException($"av_frame_ref(cuda) failed: code={refResult} msg='{GetErrorString(refResult)}'");
        }

        if (_forceNextKeyframe)
        {
            _hwFrame->pict_type = AVPictureType.AV_PICTURE_TYPE_I;
            _forceNextKeyframe = false;
        }

        _hwFrame->pts = Interlocked.Increment(ref _nextVideoPts) - 1;
        LogAvSyncIfDue();

        var attachedHdrSideData = false;
        if (options.HdrEnabled && _encodedFrameCount == 0)
        {
            attachedHdrSideData = AttachHdrFrameSideDataToHwFrame(options);
        }

        try
        {
            var sendResult = ffmpeg.avcodec_send_frame(_videoCodecCtx, _hwFrame);
            if (sendResult == ffmpeg.AVERROR(ffmpeg.EAGAIN))
            {
                DrainEncoderPackets();
                sendResult = ffmpeg.avcodec_send_frame(_videoCodecCtx, _hwFrame);
            }

            ThrowIfError(sendResult, "avcodec_send_frame(cuda)");
            DrainEncoderPackets();
            _encodedFrameCount++;
        }
        catch
        {
            _droppedFrameCount++;
            throw;
        }
        finally
        {
            if (attachedHdrSideData)
            {
                ffmpeg.av_frame_remove_side_data(_hwFrame, AVFrameSideDataType.AV_FRAME_DATA_MASTERING_DISPLAY_METADATA);
                ffmpeg.av_frame_remove_side_data(_hwFrame, AVFrameSideDataType.AV_FRAME_DATA_CONTENT_LIGHT_LEVEL);
            }

            ffmpeg.av_frame_unref(_hwFrame);
        }
    }

    public void SendVideoFrame(ReadOnlySpan<byte> frameData, int width, int height)
    {
        EnsureOpen();

        var options = _options ?? throw new InvalidOperationException("Encoder options are not initialized.");
        if (width != options.Width || height != options.Height)
        {
            _droppedFrameCount++;
            throw CreateLibAvException(
                $"LIBAV_ENCODER_ERROR operation=SendVideoFrame msg=Frame dimensions do not match encoder state width={width} height={height} expected_width={options.Width} expected_height={options.Height}");
        }

        var expectedSize = GetExpectedFrameSizeBytes(options.Width, options.Height, options.IsP010);
        if (frameData.Length < expectedSize)
        {
            _droppedFrameCount++;
            throw CreateLibAvException(
                $"LIBAV_ENCODER_ERROR operation=SendVideoFrame msg=Frame payload too small actual={frameData.Length} expected={expectedSize}");
        }

        ThrowIfError(ffmpeg.av_frame_make_writable(_videoFrame), "av_frame_make_writable");

        CopyPackedFrameToVideoFrame(frameData[..expectedSize], options);
        if (_forceNextKeyframe)
        {
            _videoFrame->pict_type = AVPictureType.AV_PICTURE_TYPE_I;
            _forceNextKeyframe = false;
        }

        _videoFrame->pts = Interlocked.Increment(ref _nextVideoPts) - 1;
        LogAvSyncIfDue();

        var attachedHdrSideData = false;
        if (options.HdrEnabled && _encodedFrameCount == 0)
        {
            attachedHdrSideData = AttachHdrFrameSideDataIfNeeded(options);
        }

        try
        {
            var sendResult = ffmpeg.avcodec_send_frame(_videoCodecCtx, _videoFrame);
            if (sendResult == ffmpeg.AVERROR(ffmpeg.EAGAIN))
            {
                DrainEncoderPackets();
                sendResult = ffmpeg.avcodec_send_frame(_videoCodecCtx, _videoFrame);
            }

            ThrowIfError(sendResult, "avcodec_send_frame");

            // Reset pict_type after send so the forced-keyframe flag doesn't stick.
            // _videoFrame is reused across calls and av_frame_make_writable does NOT
            // clear pict_type, so without this every subsequent frame would be I-frame.
            _videoFrame->pict_type = AVPictureType.AV_PICTURE_TYPE_NONE;

            DrainEncoderPackets();
            _encodedFrameCount++;
        }
        catch
        {
            _droppedFrameCount++;
            throw;
        }
        finally
        {
            if (attachedHdrSideData)
            {
                ffmpeg.av_frame_remove_side_data(_videoFrame, AVFrameSideDataType.AV_FRAME_DATA_MASTERING_DISPLAY_METADATA);
                ffmpeg.av_frame_remove_side_data(_videoFrame, AVFrameSideDataType.AV_FRAME_DATA_CONTENT_LIGHT_LEVEL);
            }
        }
    }

    private bool AttachHdrFrameSideDataIfNeeded(LibAvEncoderOptions options)
    {
        var attached = false;

        if (!string.IsNullOrWhiteSpace(options.HdrMasterDisplayMetadata))
        {
            var masteringMetadata = ffmpeg.av_mastering_display_metadata_create_side_data(_videoFrame);
            if (masteringMetadata == null)
            {
                throw CreateLibAvException("LIBAV_ENCODER_ERROR operation=av_mastering_display_metadata_create_side_data msg=Allocation returned null.");
            }

            HdrMasterDisplayMetadata.Apply(masteringMetadata, options.HdrMasterDisplayMetadata);
            attached = true;
        }

        if (options.HdrMaxCll > 0 && options.HdrMaxFall > 0)
        {
            var lightMetadata = ffmpeg.av_content_light_metadata_create_side_data(_videoFrame);
            if (lightMetadata == null)
            {
                throw CreateLibAvException("LIBAV_ENCODER_ERROR operation=av_content_light_metadata_create_side_data msg=Allocation returned null.");
            }

            lightMetadata->MaxCLL = (uint)options.HdrMaxCll;
            lightMetadata->MaxFALL = (uint)options.HdrMaxFall;
            attached = true;
        }

        return attached;
    }

    private bool AttachHdrFrameSideDataToHwFrame(LibAvEncoderOptions options)
    {
        var attached = false;

        if (!string.IsNullOrWhiteSpace(options.HdrMasterDisplayMetadata))
        {
            var masteringMetadata = ffmpeg.av_mastering_display_metadata_create_side_data(_hwFrame);
            if (masteringMetadata == null)
            {
                throw CreateLibAvException("LIBAV_ENCODER_ERROR operation=av_mastering_display_metadata_create_side_data(hw) msg=Allocation returned null.");
            }

            HdrMasterDisplayMetadata.Apply(masteringMetadata, options.HdrMasterDisplayMetadata);
            attached = true;
        }

        if (options.HdrMaxCll > 0 && options.HdrMaxFall > 0)
        {
            var lightMetadata = ffmpeg.av_content_light_metadata_create_side_data(_hwFrame);
            if (lightMetadata == null)
            {
                throw CreateLibAvException("LIBAV_ENCODER_ERROR operation=av_content_light_metadata_create_side_data(hw) msg=Allocation returned null.");
            }

            lightMetadata->MaxCLL = (uint)options.HdrMaxCll;
            lightMetadata->MaxFALL = (uint)options.HdrMaxFall;
            attached = true;
        }

        return attached;
    }

    private void DrainEncoderPackets()
    {
        while (true)
        {
            var receiveResult = ffmpeg.avcodec_receive_packet(_videoCodecCtx, _packet);
            if (receiveResult == ffmpeg.AVERROR(ffmpeg.EAGAIN) || receiveResult == ffmpeg.AVERROR_EOF)
            {
                return;
            }

            ThrowIfError(receiveResult, "avcodec_receive_packet");

            try
            {
                if (_bsfCtx != null)
                {
                    WriteFilteredPackets();
                }
                else
                {
                    WritePacket(_packet, useBsfTimeBase: false);
                }
            }
            finally
            {
                ffmpeg.av_packet_unref(_packet);
            }
        }
    }

    private void WriteFilteredPackets()
    {
        var sendResult = ffmpeg.av_bsf_send_packet(_bsfCtx, _packet);
        if (sendResult == ffmpeg.AVERROR(ffmpeg.EAGAIN))
        {
            DrainBsfPackets();
            sendResult = ffmpeg.av_bsf_send_packet(_bsfCtx, _packet);
        }

        ThrowIfError(sendResult, "av_bsf_send_packet");

        ffmpeg.av_packet_unref(_packet);
        DrainBsfPackets();
    }

    private void DrainBsfPackets()
    {
        while (true)
        {
            var receiveResult = ffmpeg.av_bsf_receive_packet(_bsfCtx, _packet);
            if (receiveResult == ffmpeg.AVERROR(ffmpeg.EAGAIN) || receiveResult == ffmpeg.AVERROR_EOF)
            {
                return;
            }

            ThrowIfError(receiveResult, "av_bsf_receive_packet");

            try
            {
                WritePacket(_packet, useBsfTimeBase: true);
            }
            finally
            {
                ffmpeg.av_packet_unref(_packet);
            }
        }
    }

    private void WritePacket(AVPacket* packet, bool useBsfTimeBase)
    {
        var sourceTimeBase = useBsfTimeBase && _bsfCtx != null ? _bsfCtx->time_base_out : _videoCodecCtx->time_base;
        ffmpeg.av_packet_rescale_ts(packet, sourceTimeBase, _videoStream->time_base);
        packet->stream_index = _videoStream->index;
        var packetSize = packet->size;
        ThrowIfError(ffmpeg.av_interleaved_write_frame(_formatCtx, packet), "av_interleaved_write_frame");
        Interlocked.Increment(ref _videoPacketsWritten);
        _totalBytesWritten += packetSize;
    }

    private void CopyPackedFrameToVideoFrame(ReadOnlySpan<byte> frameData, LibAvEncoderOptions options)
    {
        var rowBytes = options.IsP010 ? options.Width * 2 : options.Width;
        var uvHeight = options.Height / 2;
        var yBytes = rowBytes * options.Height;
        var uvBytes = rowBytes * uvHeight;
        if (frameData.Length < yBytes + uvBytes)
        {
            throw CreateLibAvException(
                $"LIBAV_ENCODER_ERROR operation=CopyPackedFrameToVideoFrame msg=Frame buffer shorter than computed planes actual={frameData.Length} expected={yBytes + uvBytes}");
        }

        fixed (byte* sourceStart = frameData)
        {
            CopyPlane(sourceStart, _videoFrame->data[0], _videoFrame->linesize[0], rowBytes, options.Height);
            CopyPlane(sourceStart + yBytes, _videoFrame->data[1], _videoFrame->linesize[1], rowBytes, uvHeight);
        }
    }

    private static void CopyPlane(byte* sourceStart, byte* destinationStart, int destinationStride, int rowBytes, int rowCount)
    {
        if (destinationStart == null)
        {
            throw CreateLibAvException("LIBAV_ENCODER_ERROR operation=CopyPlane msg=Destination plane pointer is null.");
        }

        var totalBytes = (long)rowBytes * rowCount;
        if (destinationStride == rowBytes)
        {
            Buffer.MemoryCopy(sourceStart, destinationStart, totalBytes, totalBytes);
            return;
        }

        for (var row = 0; row < rowCount; row++)
        {
            Buffer.MemoryCopy(
                sourceStart + (row * rowBytes),
                destinationStart + (row * destinationStride),
                rowBytes,
                rowBytes);
        }
    }
}

/// <summary>
/// Parses and applies HDR mastering display metadata strings to libav structs.
/// Format: G(gx,gy)B(bx,by)R(rx,ry)WP(wx,wy)L(maxL,minL)
/// Values are 50 000-denominator chromaticity and 10 000-denominator luminance.
/// </summary>
internal static unsafe class HdrMasterDisplayMetadata
{
    internal static readonly Regex Regex = new(
        @"^G\((\d+),(\d+)\)B\((\d+),(\d+)\)R\((\d+),(\d+)\)WP\((\d+),(\d+)\)L\((\d+),(\d+)\)$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    internal static void Apply(AVMasteringDisplayMetadata* metadata, string masterDisplayMetadata)
    {
        var match = Regex.Match(masterDisplayMetadata);
        if (!match.Success)
        {
            var msg =
                $"LIBAV_ENCODER_ERROR operation=ApplyMasterDisplayMetadata msg=Invalid mastering metadata format value='{masterDisplayMetadata}'.";
            Logger.Log(msg);
            throw new InvalidOperationException(msg);
        }

        var primaries = metadata->display_primaries;
        var red = primaries[0];
        red[0] = ToChromaticityRational(match.Groups[5].Value);
        red[1] = ToChromaticityRational(match.Groups[6].Value);
        primaries[0] = red;

        var green = primaries[1];
        green[0] = ToChromaticityRational(match.Groups[1].Value);
        green[1] = ToChromaticityRational(match.Groups[2].Value);
        primaries[1] = green;

        var blue = primaries[2];
        blue[0] = ToChromaticityRational(match.Groups[3].Value);
        blue[1] = ToChromaticityRational(match.Groups[4].Value);
        primaries[2] = blue;
        metadata->display_primaries = primaries;

        var whitePoint = metadata->white_point;
        whitePoint[0] = ToChromaticityRational(match.Groups[7].Value);
        whitePoint[1] = ToChromaticityRational(match.Groups[8].Value);
        metadata->white_point = whitePoint;

        metadata->max_luminance = ToLuminanceRational(match.Groups[9].Value);
        metadata->min_luminance = ToLuminanceRational(match.Groups[10].Value);
        metadata->has_primaries = 1;
        metadata->has_luminance = 1;
    }

    private static AVRational ToChromaticityRational(string value)
        => new()
        {
            num = int.Parse(value, CultureInfo.InvariantCulture),
            den = 50_000
        };

    private static AVRational ToLuminanceRational(string value)
        => new()
        {
            num = int.Parse(value, CultureInfo.InvariantCulture),
            den = 10_000
        };
}

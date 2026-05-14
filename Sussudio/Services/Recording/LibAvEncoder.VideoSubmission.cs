using System;
using System.Runtime.InteropServices;
using System.Threading;
using FFmpeg.AutoGen;

namespace Sussudio.Services.Recording;

internal sealed unsafe partial class LibAvEncoder
{
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
}

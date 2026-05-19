using System;
using System.Buffers;
using System.Runtime.InteropServices;
using FFmpeg.AutoGen;

namespace Sussudio.Services.Flashback;

internal sealed unsafe partial class FlashbackDecoder
{
    // ── Private: Frame Conversion ───────────────────────────────────────────

    private DecodedVideoFrame ConvertAndOutputVideoFrame()
    {
        // Calculate PTS first (used by both paths). MPEG-TS frames can have
        // AV_NOPTS_VALUE in pts even when FFmpeg recovered a usable timestamp.
        var pts = DecodePtsToTimeSpan(ResolveBestEffortFrameTimestamp(_videoFrame), _videoTimeBase);

        _currentPosition = pts;

        if (_ptsCalibrationCount < PtsCalibrationFrames && pts > TimeSpan.Zero)
        {
            if (_ptsCalibrationCount == 0)
                _firstCalibrationPtsTicks = pts.Ticks;
            _lastCalibrationPtsTicks = pts.Ticks;
            _ptsCalibrationCount++;

            if (_ptsCalibrationCount == PtsCalibrationFrames && _lastCalibrationPtsTicks > _firstCalibrationPtsTicks)
            {
                var elapsedSec = (_lastCalibrationPtsTicks - _firstCalibrationPtsTicks) / (double)TimeSpan.TicksPerSecond;
                if (elapsedSec > 0.001)
                {
                    var measuredFps = (PtsCalibrationFrames - 1) / elapsedSec;
                    if (_metadataFrameRate > measuredFps * 1.5 && measuredFps > 10)
                    {
                        Logger.Log($"FLASHBACK_DECODER_FPS_OVERRIDE metadata={_metadataFrameRate:F2} measured={measuredFps:F2}");
                        _frameRate = measuredFps;
                    }
                }
            }
        }

        // Check actual frame format — D3D11VA may silently fall back to software
        // (get_format callback runs on first decode, not during avcodec_open2).
        var actualFormat = (AVPixelFormat)_videoFrame->format;
        if (_isD3D11HwAccelerated && actualFormat != AVPixelFormat.AV_PIX_FMT_D3D11)
        {
            Logger.Log($"FLASHBACK_DECODER_D3D11VA_FALLBACK actual_fmt={actualFormat} — switching to software path");
            _isD3D11HwAccelerated = false;
            _decodedPixelFormat = actualFormat;
            var targetFmt = _isHdr ? AVPixelFormat.AV_PIX_FMT_P010LE : AVPixelFormat.AV_PIX_FMT_NV12;
            _needsConvert = actualFormat != targetFmt;
            AllocateVideoOutputBuffers();
        }

        if (_isD3D11HwAccelerated)
        {
            // D3D11VA path: frame->data[0] is ID3D11Texture2D*, data[1] is subresource index.
            // Clone the AVFrame to hold the D3D11VA surface reference — _videoFrame is reused
            // by the next avcodec_receive_frame call, which can release the surface back to the
            // pool before the renderer copies it. The cloned frame must be freed after the
            // texture is consumed (via HeldFrame).
            var clonedFrame = ffmpeg.av_frame_clone(_videoFrame);
            ffmpeg.av_frame_unref(_videoFrame); // release pool slot immediately
            if (clonedFrame == null)
            {
                Logger.Log("FLASHBACK_DECODE_CLONE_FAIL reason='av_frame_clone returned null'");
                return default;
            }

            if (!TryValidateD3D11VideoFrame(clonedFrame, _videoWidth, _videoHeight, out var d3dFrameFailure))
            {
                Logger.Log($"FLASHBACK_DECODER_VIDEO_WARN reason=invalid_d3d11_frame detail='{d3dFrameFailure}' w={_videoWidth} h={_videoHeight}");
                ffmpeg.av_frame_free(&clonedFrame);
                return default;
            }

            var texturePtr = (IntPtr)clonedFrame->data[0];
            var subresource = (int)(long)clonedFrame->data[1];

            return new DecodedVideoFrame
            {
                TexturePtr = texturePtr,
                SubresourceIndex = subresource,
                Width = _videoWidth,
                Height = _videoHeight,
                IsHdr = _isHdr,
                Pts = pts,
                IsD3D11Texture = true,
                HeldFrame = (IntPtr)clonedFrame
            };
        }

        // Software decode path
        if (actualFormat != AVPixelFormat.AV_PIX_FMT_NONE && actualFormat != _decodedPixelFormat)
        {
            _decodedPixelFormat = actualFormat;
            var targetFmt = _isHdr ? AVPixelFormat.AV_PIX_FMT_P010LE : AVPixelFormat.AV_PIX_FMT_NV12;
            _needsConvert = _decodedPixelFormat != targetFmt;
        }

        if (!TryValidateSoftwareVideoFrame(_videoFrame, _decodedPixelFormat, _videoWidth, _videoHeight, _isHdr, out var frameFailure))
        {
            Logger.Log($"FLASHBACK_DECODER_VIDEO_WARN reason=invalid_software_frame detail='{frameFailure}' fmt={_decodedPixelFormat} w={_videoWidth} h={_videoHeight}");
            ffmpeg.av_frame_unref(_videoFrame);
            return default;
        }

        try
        {
            var outputSize = CalculateFrameBufferSize(_videoWidth, _videoHeight, _isHdr);

            // Select the next buffer in the double-buffer ring
            var bufferIndex = _currentVideoBufferIndex;
            _currentVideoBufferIndex = (_currentVideoBufferIndex + 1) % VideoFrameBufferCount;

            var buffer = _videoFrameBuffers[bufferIndex]!;
            if (buffer.Length < outputSize)
            {
                Logger.Log($"FLASHBACK_DECODER_VIDEO_REALLOC old={buffer.Length} new={outputSize}");
                if (_videoFrameHandles[bufferIndex].IsAllocated)
                {
                    _videoFrameHandles[bufferIndex].Free();
                }

                ArrayPool<byte>.Shared.Return(buffer);
                buffer = ArrayPool<byte>.Shared.Rent(outputSize);
                _videoFrameBuffers[bufferIndex] = buffer;
                _videoFrameHandles[bufferIndex] = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            }

            var dataPtr = _videoFrameHandles[bufferIndex].AddrOfPinnedObject();

            if (_needsConvert)
            {
                if (!_isHdr)
                    ConvertYuv420pToNv12((byte*)dataPtr);
                else
                    ConvertYuv420p10leToP010((byte*)dataPtr);
            }
            else
            {
                CopyFramePlanesToBuffer((byte*)dataPtr, outputSize);
            }

            return new DecodedVideoFrame
            {
                Data = dataPtr,
                DataLength = outputSize,
                Width = _videoWidth,
                Height = _videoHeight,
                IsHdr = _isHdr,
                Pts = pts,
                IsD3D11Texture = false,
                HeldFrame = IntPtr.Zero
            };
        }
        finally
        {
            ffmpeg.av_frame_unref(_videoFrame);
        }
    }

}

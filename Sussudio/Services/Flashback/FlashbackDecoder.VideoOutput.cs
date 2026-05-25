using System;
using System.Buffers;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
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

    private static TimeSpan DecodePtsToTimeSpan(long pts, AVRational timeBase)
    {
        if (pts == ffmpeg.AV_NOPTS_VALUE || timeBase.num <= 0 || timeBase.den <= 0)
        {
            return TimeSpan.Zero;
        }

        var seconds = (double)pts * timeBase.num / timeBase.den;
        if (!double.IsFinite(seconds) || seconds <= 0 || seconds > TimeSpan.MaxValue.TotalSeconds)
        {
            return TimeSpan.Zero;
        }

        return TimeSpan.FromSeconds(seconds);
    }

    private static long ResolveBestEffortFrameTimestamp(AVFrame* frame)
    {
        if (frame == null)
        {
            return ffmpeg.AV_NOPTS_VALUE;
        }

        if (frame->best_effort_timestamp != ffmpeg.AV_NOPTS_VALUE)
        {
            return frame->best_effort_timestamp;
        }

        return frame->pts;
    }

    private void CopyFramePlanesToBuffer(byte* dest, int destSize)
    {
        if (_isHdr)
        {
            var yLinesize = _videoWidth * 2;
            var yPlaneSize = yLinesize * _videoHeight;
            var uvLinesize = _videoWidth * 2;

            CopyPlane(_videoFrame->data[0], _videoFrame->linesize[0],
                      dest, yLinesize, _videoHeight);
            CopyPlane(_videoFrame->data[1], _videoFrame->linesize[1],
                      dest + yPlaneSize, uvLinesize, _videoHeight / 2);
        }
        else
        {
            var yPlaneSize = _videoWidth * _videoHeight;

            CopyPlane(_videoFrame->data[0], _videoFrame->linesize[0],
                      dest, _videoWidth, _videoHeight);
            CopyPlane(_videoFrame->data[1], _videoFrame->linesize[1],
                      dest + yPlaneSize, _videoWidth, _videoHeight / 2);
        }
    }

    private static void CopyPlane(byte* src, int srcLinesize, byte* dst, int dstLinesize, int height)
    {
        if (srcLinesize == dstLinesize)
        {
            Buffer.MemoryCopy(src, dst, (long)dstLinesize * height, (long)srcLinesize * height);
            return;
        }

        var copyWidth = Math.Min(srcLinesize, dstLinesize);
        for (var y = 0; y < height; y++)
        {
            Buffer.MemoryCopy(
                src + y * srcLinesize,
                dst + y * dstLinesize,
                dstLinesize, copyWidth);
        }
    }

    private void ConvertYuv420pToNv12(byte* dest)
    {
        var w = _videoWidth;
        var h = _videoHeight;

        CopyPlane(_videoFrame->data[0], _videoFrame->linesize[0], dest, w, h);

        var uvDest = dest + w * h;
        var halfW = w / 2;
        var uStride = _videoFrame->linesize[1];
        var vStride = _videoFrame->linesize[2];

        for (var row = 0; row < h / 2; row++)
        {
            var uRow = _videoFrame->data[1] + row * uStride;
            var vRow = _videoFrame->data[2] + row * vStride;
            var destRow = uvDest + row * w;

            InterleaveUvRow(uRow, vRow, destRow, halfW);
        }
    }

    private void ConvertYuv420p10leToP010(byte* dest)
    {
        var w = _videoWidth;
        var h = _videoHeight;

        CopyPlane(_videoFrame->data[0], _videoFrame->linesize[0], dest, w * 2, h);

        var uvDest = (ushort*)(dest + w * h * 2);
        var halfW = w / 2;
        var uStride = _videoFrame->linesize[1];
        var vStride = _videoFrame->linesize[2];

        for (var row = 0; row < h / 2; row++)
        {
            var uRow = (ushort*)(_videoFrame->data[1] + row * uStride);
            var vRow = (ushort*)(_videoFrame->data[2] + row * vStride);
            var destRow = uvDest + row * w;

            for (var col = 0; col < halfW; col++)
            {
                destRow[col * 2] = uRow[col];
                destRow[col * 2 + 1] = vRow[col];
            }
        }
    }

    private static void InterleaveUvRow(byte* uRow, byte* vRow, byte* dest, int halfW)
    {
        var col = 0;

        if (Avx2.IsSupported)
        {
            for (; col + 32 <= halfW; col += 32)
            {
                var u = Avx.LoadVector256(uRow + col);
                var v = Avx.LoadVector256(vRow + col);

                var lo = Avx2.UnpackLow(u, v);
                var hi = Avx2.UnpackHigh(u, v);

                var out0 = Avx2.Permute2x128(lo, hi, 0x20);
                var out1 = Avx2.Permute2x128(lo, hi, 0x31);

                Avx.Store(dest + col * 2, out0);
                Avx.Store(dest + col * 2 + 32, out1);
            }
        }
        else if (Sse2.IsSupported)
        {
            for (; col + 16 <= halfW; col += 16)
            {
                var u = Sse2.LoadVector128(uRow + col);
                var v = Sse2.LoadVector128(vRow + col);

                var lo = Sse2.UnpackLow(u, v);
                var hi = Sse2.UnpackHigh(u, v);

                Sse2.Store(dest + col * 2, lo);
                Sse2.Store(dest + col * 2 + 16, hi);
            }
        }

        for (; col < halfW; col++)
        {
            dest[col * 2] = uRow[col];
            dest[col * 2 + 1] = vRow[col];
        }
    }
}

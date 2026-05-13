using System;
using System.Buffers;
using FFmpeg.AutoGen;

namespace Sussudio.Services.Flashback;

internal sealed unsafe partial class FlashbackDecoder
{
    private void CloseFileCore()
    {
        Logger.Log($"FLASHBACK_DECODER_CLOSE_CORE path='{_currentFilePath}' had_swr={_swrCtx != null} had_video={_videoCodecCtx != null} had_audio={_audioCodecCtx != null}");
        _isOpen = false;
        _suppressRecoverableSeekLogsForNextVideoFrame = false;

        // Clear any stashed pending frame (free held D3D11VA surface if present).
        if (_hasPendingVideoFrame)
        {
            ReleaseHeldFrameBestEffort(_pendingVideoFrame, "close_pending");
            _pendingVideoFrame = default;
            _hasPendingVideoFrame = false;
        }

        // Free pinned handles used by the software decode path.
        for (var i = 0; i < VideoFrameBufferCount; i++)
        {
            if (_videoFrameHandles[i].IsAllocated)
            {
                _videoFrameHandles[i].Free();
            }

            if (_videoFrameBuffers[i] != null)
            {
                ArrayPool<byte>.Shared.Return(_videoFrameBuffers[i]!);
                _videoFrameBuffers[i] = null;
            }
        }

        if (_packet != null)
        {
            var pkt = _packet;
            ffmpeg.av_packet_free(&pkt);
            _packet = null;
        }

        if (_videoFrame != null)
        {
            var frame = _videoFrame;
            ffmpeg.av_frame_free(&frame);
            _videoFrame = null;
        }

        if (_audioFrame != null)
        {
            var frame = _audioFrame;
            ffmpeg.av_frame_free(&frame);
            _audioFrame = null;
        }

        // _d3d11HwDeviceCtx is persistent across files. _isD3D11HwAccelerated
        // is reset per file in InitializeVideoDecoder.
        if (_swrCtx != null)
        {
            ffmpeg.swr_convert(_swrCtx, null, 0, null, 0);
            var swr = _swrCtx;
            ffmpeg.swr_free(&swr);
            _swrCtx = null;
        }

        if (_videoCodecCtx != null)
        {
            var ctx = _videoCodecCtx;
            ffmpeg.avcodec_free_context(&ctx);
            _videoCodecCtx = null;
        }

        if (_audioCodecCtx != null)
        {
            var ctx = _audioCodecCtx;
            ffmpeg.avcodec_free_context(&ctx);
            _audioCodecCtx = null;
        }

        if (_formatCtx != null)
        {
            var fmt = _formatCtx;
            ffmpeg.avformat_close_input(&fmt);
            _formatCtx = null;
        }

        _videoStreamIndex = -1;
        _audioStreamIndex = -1;
        _videoWidth = 0;
        _videoHeight = 0;
        _isHdr = false;
        _frameRate = 0;
        _metadataFrameRate = 0;
        _ptsCalibrationCount = 0;
        _firstCalibrationPtsTicks = 0;
        _lastCalibrationPtsTicks = 0;
        _currentPosition = TimeSpan.Zero;
        _currentFilePath = null;
        _needsConvert = false;
        _currentVideoBufferIndex = 0;
    }

    /// <summary>
    /// Frees the cloned AVFrame held by a D3D11VA <see cref="DecodedVideoFrame"/>,
    /// releasing the D3D11VA surface reference back to the decoder pool.
    /// Safe to call on software frames (no-op when <see cref="DecodedVideoFrame.HeldFrame"/> is zero).
    /// </summary>
    internal static void ReleaseHeldFrame(DecodedVideoFrame frame)
    {
        if (frame.HeldFrame != IntPtr.Zero)
        {
            var heldFrame = (AVFrame*)frame.HeldFrame;
            ffmpeg.av_frame_free(&heldFrame);
        }
    }

    private static void ReleaseHeldFrameBestEffort(DecodedVideoFrame frame, string operation)
    {
        try
        {
            ReleaseHeldFrame(frame);
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_DECODER_RELEASE_HELD_FRAME_WARN op={operation} type={ex.GetType().Name} msg='{ex.Message}'");
        }
    }
}

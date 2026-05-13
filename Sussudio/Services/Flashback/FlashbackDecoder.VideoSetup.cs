using System;
using System.Buffers;
using System.Runtime.InteropServices;
using FFmpeg.AutoGen;

namespace Sussudio.Services.Flashback;

internal sealed unsafe partial class FlashbackDecoder
{
    private void InitializeVideoDecoder()
    {
        // Reset hw accel flag; it persists across file opens but must reflect
        // the decoder chosen for this file.
        _isD3D11HwAccelerated = false;

        var videoStream = _formatCtx->streams[_videoStreamIndex];
        _videoTimeBase = videoStream->time_base;

        var codecPar = videoStream->codecpar;
        _videoWidth = codecPar->width;
        _videoHeight = codecPar->height;
        ValidateVideoDimensions(_videoWidth, _videoHeight);

        _decodedPixelFormat = (AVPixelFormat)codecPar->format;
        _isHdr = (codecPar->codec_id == AVCodecID.AV_CODEC_ID_HEVC ||
                  codecPar->codec_id == AVCodecID.AV_CODEC_ID_AV1) &&
                 (_decodedPixelFormat == AVPixelFormat.AV_PIX_FMT_YUV420P10LE ||
                  _decodedPixelFormat == AVPixelFormat.AV_PIX_FMT_P010LE);

        if (videoStream->avg_frame_rate.den > 0 && videoStream->avg_frame_rate.num > 0)
        {
            _frameRate = (double)videoStream->avg_frame_rate.num / videoStream->avg_frame_rate.den;
        }
        else if (videoStream->r_frame_rate.den > 0 && videoStream->r_frame_rate.num > 0)
        {
            _frameRate = (double)videoStream->r_frame_rate.num / videoStream->r_frame_rate.den;
        }
        else
        {
            _frameRate = 30.0;
            Logger.Log($"FLASHBACK_DECODER_VIDEO_WARN reason=framerate_fallback default=30.0 path='{_currentFilePath}'");
        }

        Logger.Log($"FLASHBACK_DECODER_STREAM_INFO " +
                   $"avg_frame_rate={{num={videoStream->avg_frame_rate.num}, den={videoStream->avg_frame_rate.den}}} " +
                   $"r_frame_rate={{num={videoStream->r_frame_rate.num}, den={videoStream->r_frame_rate.den}}} " +
                   $"time_base={{num={videoStream->time_base.num}, den={videoStream->time_base.den}}} " +
                   $"computed_fps={_frameRate:F4}");

        _metadataFrameRate = _frameRate;
        _ptsCalibrationCount = 0;
        _firstCalibrationPtsTicks = 0;
        _lastCalibrationPtsTicks = 0;

        if (TryInitializeD3D11VADecoder(codecPar))
        {
            _videoFrame = ffmpeg.av_frame_alloc();
            if (_videoFrame == null)
            {
                throw CreateException("Failed to allocate video frame.");
            }

            Logger.Log($"FLASHBACK_DECODER_VIDEO hw_accel=D3D11VA " +
                       $"sw_fmt={(_isHdr ? "P010" : "NV12")} {_videoWidth}x{_videoHeight}");
            return;
        }

        var codec = ffmpeg.avcodec_find_decoder(codecPar->codec_id);
        if (codec == null)
        {
            throw CreateException($"No decoder found for codec_id={codecPar->codec_id}.");
        }

        _videoCodecCtx = ffmpeg.avcodec_alloc_context3(codec);
        if (_videoCodecCtx == null)
        {
            throw CreateException("Failed to allocate video codec context.");
        }

        ThrowIfError(
            ffmpeg.avcodec_parameters_to_context(_videoCodecCtx, codecPar),
            "avcodec_parameters_to_context(video)");

        // MJPEG frames are independently decodable; FFmpeg auto-threading can add
        // avoidable per-frame latency spikes at 4K120 playback.
        if (codecPar->codec_id == AVCodecID.AV_CODEC_ID_MJPEG)
        {
            _videoCodecCtx->thread_count = 1;
        }

        ThrowIfError(
            ffmpeg.avcodec_open2(_videoCodecCtx, codec, null),
            "avcodec_open2(video)");

        _videoFrame = ffmpeg.av_frame_alloc();
        if (_videoFrame == null)
        {
            throw CreateException("Failed to allocate video frame.");
        }

        var targetFormat = _isHdr ? AVPixelFormat.AV_PIX_FMT_P010LE : AVPixelFormat.AV_PIX_FMT_NV12;

        var actualDecodedFormat = _videoCodecCtx->pix_fmt;
        if (actualDecodedFormat != AVPixelFormat.AV_PIX_FMT_NONE)
        {
            _decodedPixelFormat = actualDecodedFormat;
        }

        _needsConvert = _decodedPixelFormat != targetFormat;

        if (_needsConvert)
        {
            var canConvert =
                _decodedPixelFormat == AVPixelFormat.AV_PIX_FMT_YUV420P ||
                _decodedPixelFormat == AVPixelFormat.AV_PIX_FMT_YUV420P10LE;

            if (!canConvert)
            {
                throw CreateException($"Unsupported decoded format {_decodedPixelFormat} -> {targetFormat}");
            }
        }

        AllocateVideoOutputBuffers();

        var videoCodecName = codec->name != null ? Marshal.PtrToStringAnsi((IntPtr)codec->name) : "?";
        Logger.Log($"FLASHBACK_DECODER_VIDEO codec={videoCodecName} hw_accel=Software " +
                   $"pix_fmt={_decodedPixelFormat} target={targetFormat} " +
                   $"needs_convert={_needsConvert}");
    }

    private void AllocateVideoOutputBuffers()
    {
        var outputFrameSize = CalculateFrameBufferSize(_videoWidth, _videoHeight, _isHdr);
        for (var i = 0; i < VideoFrameBufferCount; i++)
        {
            _videoFrameBuffers[i] = ArrayPool<byte>.Shared.Rent(outputFrameSize);
            _videoFrameHandles[i] = GCHandle.Alloc(_videoFrameBuffers[i], GCHandleType.Pinned);
        }

        _currentVideoBufferIndex = 0;
    }
}

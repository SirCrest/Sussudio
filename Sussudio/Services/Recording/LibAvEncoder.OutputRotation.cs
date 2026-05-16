using System;
using System.IO;
using FFmpeg.AutoGen;

namespace Sussudio.Services.Recording;

internal sealed unsafe partial class LibAvEncoder
{
    public RotateOutputResult RotateOutput(string newPath)
    {
        EnsureOpen();

        if (string.IsNullOrWhiteSpace(newPath))
        {
            throw new ArgumentException("New output path is required.", nameof(newPath));
        }

        var options = _options ?? throw new InvalidOperationException("Encoder options are not initialized.");
        var previousPath = options.OutputPath;
        var previousEncodedFrames = _encodedFrameCount;

        var outputDirectory = Path.GetDirectoryName(Path.GetFullPath(newPath));
        if (!string.IsNullOrWhiteSpace(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        if (_audio.CodecCtx != null)
        {
            // Do not flush partial AAC frames while rotating; partial flushes act
            // like end-of-stream for the encoder and break the next segment.
            DrainBufferedFrames(ref _audio, flushPartialFrame: false);
            DrainStreamEncoderPackets(ref _audio);
        }

        if (_mic.CodecCtx != null)
        {
            DrainBufferedFrames(ref _mic, flushPartialFrame: false);
            DrainStreamEncoderPackets(ref _mic);
        }

        DrainEncoderPackets();

        if (_headerWritten && _formatCtx != null)
        {
            ThrowIfError(ffmpeg.av_write_trailer(_formatCtx), "av_write_trailer(rotate)");
        }

        // Capture totals after drains/trailer and before segment state reset.
        var previousTotalBytes = _totalBytesWritten;

        CloseCurrentOutputIo();
        FreeCurrentOutputContext();
        try
        {
            ReinitializeOutputContext(newPath);
        }
        catch (Exception ex)
        {
            _isOpen = false;
            Logger.Log($"LIBAV_ENCODER_ROTATE_FAILED path='{newPath}' error={ex.Message}");
            throw;
        }

        ResetSegmentRuntimeState();
        _options = options with { OutputPath = newPath };

        Logger.Log(
            $"LIBAV_ENCODER_ROTATE old_output='{previousPath}' new_output='{newPath}' frames={previousEncodedFrames} bytes={previousTotalBytes}");
        return new RotateOutputResult(previousPath, previousEncodedFrames, previousTotalBytes);
    }

    private void CloseCurrentOutputIo()
    {
        if (_formatCtx == null || _formatCtx->pb == null)
        {
            return;
        }

        ThrowIfError(ffmpeg.avio_closep(&_formatCtx->pb), "avio_closep(rotate)");
    }

    private void FreeCurrentOutputContext()
    {
        if (_formatCtx == null)
        {
            return;
        }

        ffmpeg.avformat_free_context(_formatCtx);
        _formatCtx = null;
        _videoStream = null;
        _audio.Stream = null;
        _mic.Stream = null;
        _headerWritten = false;
    }

    private void ReinitializeOutputContext(string outputPath)
    {
        var containerFormat = _options?.ContainerFormat ?? "mp4";
        AVFormatContext* formatCtx = null;
        ThrowIfError(
            ffmpeg.avformat_alloc_output_context2(&formatCtx, null, containerFormat, outputPath),
            "avformat_alloc_output_context2(rotate)");
        if (formatCtx == null)
        {
            throw CreateLibAvException(
                "LIBAV_ENCODER_ERROR operation=avformat_alloc_output_context2(rotate) msg=Output context allocation returned null.");
        }

        _formatCtx = formatCtx;
        ReinitializeVideoStream();
        ReinitializeAudioStream();
        ReinitializeMicrophoneStream();
        ReinitializeVideoBitstreamFilter();

        ThrowIfError(ffmpeg.avio_open2(&_formatCtx->pb, outputPath, ffmpeg.AVIO_FLAG_WRITE, null, null), "avio_open2(rotate)");

        AVDictionary* muxerOptions = null;
        try
        {
            ApplyMp4MuxerOptions(containerFormat, _options?.FragmentedMp4 ?? false, &muxerOptions, "rotate");
            ThrowIfError(ffmpeg.avformat_write_header(_formatCtx, &muxerOptions), "avformat_write_header(rotate)");
            _headerWritten = true;
        }
        finally
        {
            ffmpeg.av_dict_free(&muxerOptions);
        }
    }

    private void ReinitializeVideoStream()
    {
        if (_formatCtx == null || _videoCodecCtx == null)
        {
            throw new InvalidOperationException("Video rotation state is not initialized.");
        }

        _videoStream = ffmpeg.avformat_new_stream(_formatCtx, null);
        if (_videoStream == null)
        {
            throw CreateLibAvException("LIBAV_ENCODER_ERROR operation=avformat_new_stream(rotate_video) msg=Stream allocation returned null.");
        }

        ThrowIfError(
            ffmpeg.avcodec_parameters_from_context(_videoStream->codecpar, _videoCodecCtx),
            "avcodec_parameters_from_context(rotate_video)");
        _videoStream->time_base = _videoCodecCtx->time_base;
        _videoStream->avg_frame_rate = _videoCodecCtx->framerate;
        _videoStream->r_frame_rate = _videoCodecCtx->framerate;
    }

    private void ReinitializeAudioStream()
    {
        if (_formatCtx == null || _audio.CodecCtx == null)
        {
            return;
        }

        _audio.Stream = ffmpeg.avformat_new_stream(_formatCtx, null);
        if (_audio.Stream == null)
        {
            throw CreateLibAvException("LIBAV_ENCODER_ERROR operation=avformat_new_stream(rotate_audio) msg=Stream allocation returned null.");
        }

        ThrowIfError(
            ffmpeg.avcodec_parameters_from_context(_audio.Stream->codecpar, _audio.CodecCtx),
            "avcodec_parameters_from_context(rotate_audio)");
        _audio.Stream->time_base = _audio.CodecCtx->time_base;
    }

    private void ReinitializeMicrophoneStream()
    {
        if (_formatCtx == null || _mic.CodecCtx == null)
        {
            return;
        }

        _mic.Stream = ffmpeg.avformat_new_stream(_formatCtx, null);
        if (_mic.Stream == null)
        {
            throw CreateLibAvException("LIBAV_ENCODER_ERROR operation=avformat_new_stream(rotate_mic) msg=Stream allocation returned null.");
        }

        ThrowIfError(
            ffmpeg.avcodec_parameters_from_context(_mic.Stream->codecpar, _mic.CodecCtx),
            "avcodec_parameters_from_context(rotate_mic)");
        _mic.Stream->time_base = _mic.CodecCtx->time_base;
    }

    private void ReinitializeVideoBitstreamFilter()
    {
        if (_bsfCtx != null)
        {
            var existingBsf = _bsfCtx;
            ffmpeg.av_bsf_free(&existingBsf);
            _bsfCtx = null;
        }

        var options = _options;
        if (options != null)
        {
            InitializeVideoBitstreamFilterIfNeeded(options);
        }
    }

    private void ResetSegmentRuntimeState()
    {
        // Keep PTS and AAC accumulators continuous across segment boundaries:
        // encoders may still emit packets from before the rotation.
        _forceNextKeyframe = true;
        _encodedFrameCount = 0;
        _droppedFrameCount = 0;
        _audioSamplesReceived = 0;
        _micSamplesReceived = 0;
        _lastSyncLogVideoFrame = 0;
        _driftCorrectionAppliedSamples = 0;
        _lastDriftCorrectionVideoFrame = 0;
        _totalBytesWritten = 0;
        _flushSent = false;
    }
}

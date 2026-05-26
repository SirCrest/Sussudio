using System;
using System.IO;
using System.Runtime.InteropServices;
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

    private static unsafe void ApplyMp4MuxerOptions(
        string containerFormat,
        bool fragmentedMp4,
        AVDictionary** muxerOptions,
        string operation)
    {
        if (containerFormat != "mp4")
        {
            return;
        }

        var movflags = fragmentedMp4
            ? "frag_keyframe+empty_moov"
            : "+faststart";
        ThrowIfError(ffmpeg.av_dict_set(muxerOptions, "movflags", movflags, 0), $"av_dict_set(movflags,{operation})");

        if (fragmentedMp4)
        {
            // Keep active Flashback playback A/V interleaving tight. Keyframe-only
            // fragmentation can batch about a GOP of video before matching audio.
            ThrowIfError(ffmpeg.av_dict_set(muxerOptions, "frag_duration", "100000", 0), $"av_dict_set(frag_duration,{operation})");
            ThrowIfError(ffmpeg.av_dict_set(muxerOptions, "flush_packets", "1", 0), $"av_dict_set(flush_packets,{operation})");
        }
    }

    public void FlushAndClose()
    {
        if (!_isOpen && _formatCtx == null && _videoCodecCtx == null && _audio.CodecCtx == null && _mic.CodecCtx == null)
        {
            return;
        }

        try
        {
            if (_isOpen && !_flushSent)
            {
                try
                {
                    var flushResult = ffmpeg.avcodec_send_frame(_videoCodecCtx, null);
                    if (flushResult != ffmpeg.AVERROR_EOF)
                    {
                        ThrowIfError(flushResult, "avcodec_send_frame(flush)");
                        _flushSent = true;
                    }

                    DrainEncoderPackets();
                }
                catch (Exception ex)
                {
                    Logger.Log($"LIBAV_ENCODER_WARNING video_flush_error msg='{ex.Message}'");
                }
            }

            if (_audio.CodecCtx != null)
            {
                FlushPendingStreamSamples(ref _audio, "audio_flush",
                    trackDriftCorrection: true, DriftCorrectionThresholdMs);

                var flushResult = ffmpeg.avcodec_send_frame(_audio.CodecCtx, null);
                if (flushResult != ffmpeg.AVERROR_EOF)
                {
                    ThrowIfError(flushResult, "avcodec_send_frame(audio_flush)");
                }

                DrainStreamEncoderPackets(ref _audio);
            }

            if (_mic.CodecCtx != null)
            {
                FlushPendingStreamSamples(ref _mic, "mic_flush",
                    trackDriftCorrection: false, MicDriftCorrectionThresholdMs);

                var flushResult = ffmpeg.avcodec_send_frame(_mic.CodecCtx, null);
                if (flushResult != ffmpeg.AVERROR_EOF)
                {
                    ThrowIfError(flushResult, "avcodec_send_frame(mic_flush)");
                }

                DrainStreamEncoderPackets(ref _mic);
            }
        }
        finally
        {
            CleanupResources(writeTrailer: true);
        }
    }

    public void Dispose()
    {
        FlushAndClose();
    }

    private void CleanupResources(bool writeTrailer)
    {
        var outputPath = _options?.OutputPath;
        var normalClose = _isOpen;

        try
        {
            if (writeTrailer && _headerWritten && _formatCtx != null)
            {
                var trailerResult = ffmpeg.av_write_trailer(_formatCtx);
                if (trailerResult < 0)
                {
                    Logger.Log(
                        $"LIBAV_ENCODER_ERROR operation=av_write_trailer code={trailerResult} msg='{GetErrorString(trailerResult)}'");
                }
            }
        }
        finally
        {
            var useCudaHardwareFrames = _useCudaHardwareFrames;
            var finalMicSamplesReceived = ReleaseNativeResources(useCudaHardwareFrames);

            var outputBytes = 0L;
            if (!string.IsNullOrWhiteSpace(outputPath) && File.Exists(outputPath))
            {
                outputBytes = new FileInfo(outputPath).Length;
            }

            if (!string.IsNullOrWhiteSpace(outputPath))
            {
                if (normalClose)
                {
                    Logger.Log(
                        $"LIBAV_ENCODER_CLOSE output='{outputPath}' frames={_encodedFrameCount} dropped={_droppedFrameCount} audio_samples={_audioSamplesReceived} mic_samples={finalMicSamplesReceived} file_bytes={outputBytes}");
                }
                else if (_headerWritten || _encodedFrameCount > 0 || outputBytes > 0)
                {
                    Logger.Log(
                        $"LIBAV_ENCODER_CLEANUP init_failed=true output='{outputPath}' frames={_encodedFrameCount} dropped={_droppedFrameCount} audio_samples={_audioSamplesReceived} mic_samples={finalMicSamplesReceived} file_bytes={outputBytes}");
                }
            }
        }
    }

    private long ReleaseNativeResources(bool useCudaHardwareFrames)
    {
        if (_formatCtx != null && _formatCtx->pb != null)
        {
            var closeResult = ffmpeg.avio_closep(&_formatCtx->pb);
            if (closeResult < 0)
            {
                Logger.Log(
                    $"LIBAV_ENCODER_ERROR operation=avio_closep code={closeResult} msg='{GetErrorString(closeResult)}'");
            }
        }

        if (_bsfCtx != null)
        {
            var bsfCtx = _bsfCtx;
            ffmpeg.av_bsf_free(&bsfCtx);
            _bsfCtx = null;
        }

        if (_packet != null)
        {
            var packet = _packet;
            ffmpeg.av_packet_free(&packet);
            _packet = null;
        }

        if (_hwFrame != null)
        {
            var hwFrame = _hwFrame;
            ffmpeg.av_frame_free(&hwFrame);
            _hwFrame = null;
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

        _useHardwareFrames = false;
        _useCudaHardwareFrames = false;

        if (!useCudaHardwareFrames && _hwPoolTextures != null)
        {
            for (var i = 0; i < _hwPoolTextures.Length; i++)
            {
                if (_hwPoolTextures[i] != IntPtr.Zero)
                {
                    Marshal.Release(_hwPoolTextures[i]);
                    _hwPoolTextures[i] = IntPtr.Zero;
                }
            }

            _hwPoolTextures = null;
        }

        if (_audio.Frame != null)
        {
            var audioFrame = _audio.Frame;
            ffmpeg.av_frame_free(&audioFrame);
            _audio.Frame = null;
        }

        if (_mic.Frame != null)
        {
            var micFrame = _mic.Frame;
            ffmpeg.av_frame_free(&micFrame);
            _mic.Frame = null;
        }

        if (_videoFrame != null)
        {
            var videoFrame = _videoFrame;
            ffmpeg.av_frame_free(&videoFrame);
            _videoFrame = null;
        }

        if (_audio.SwrCtx != null)
        {
            var swrCtx = _audio.SwrCtx;
            ffmpeg.swr_free(&swrCtx);
            _audio.SwrCtx = null;
        }

        if (_mic.SwrCtx != null)
        {
            var micSwrCtx = _mic.SwrCtx;
            ffmpeg.swr_free(&micSwrCtx);
            _mic.SwrCtx = null;
        }

        if (_audio.CodecCtx != null)
        {
            var audioCodecCtx = _audio.CodecCtx;
            ffmpeg.avcodec_free_context(&audioCodecCtx);
            _audio.CodecCtx = null;
        }

        if (_mic.CodecCtx != null)
        {
            var micCodecCtx = _mic.CodecCtx;
            ffmpeg.avcodec_free_context(&micCodecCtx);
            _mic.CodecCtx = null;
        }

        if (_videoCodecCtx != null)
        {
            var videoCodecCtx = _videoCodecCtx;
            ffmpeg.avcodec_free_context(&videoCodecCtx);
            _videoCodecCtx = null;
        }

        if (_audio.ResampleBuffer != null)
        {
            ffmpeg.av_free(_audio.ResampleBuffer);
            _audio.ResampleBuffer = null;
        }

        if (_audio.SampleQueueBuffer != null)
        {
            ffmpeg.av_free(_audio.SampleQueueBuffer);
            _audio.SampleQueueBuffer = null;
        }

        if (_mic.ResampleBuffer != null)
        {
            ffmpeg.av_free(_mic.ResampleBuffer);
            _mic.ResampleBuffer = null;
        }

        if (_mic.SampleQueueBuffer != null)
        {
            ffmpeg.av_free(_mic.SampleQueueBuffer);
            _mic.SampleQueueBuffer = null;
        }

        if (_formatCtx != null)
        {
            ffmpeg.avformat_free_context(_formatCtx);
            _formatCtx = null;
        }

        _videoStream = null;
        _audio.Stream = null;
        _mic.Stream = null;
        _audio.FrameSize = 0;
        _mic.FrameSize = 0;
        _audio.AccumulatorCapacity = 0;
        _audio.SampleQueueCapacity = 0;
        _mic.AccumulatorCapacity = 0;
        _mic.SampleQueueCapacity = 0;
        _audio.AccumulatorBytes = 0;
        _audio.BufferedSamples = 0;
        _mic.AccumulatorBytes = 0;
        _mic.BufferedSamples = 0;
        _nextVideoPts = 0;
        _audio.NextPts = 0;
        _mic.NextPts = 0;
        var finalMicSamplesReceived = _micSamplesReceived;
        _micSamplesReceived = 0;
        _mic.CachedTimeBase = default;
        _isOpen = false;
        _headerWritten = false;
        _flushSent = false;

        return finalMicSamplesReceived;
    }
}

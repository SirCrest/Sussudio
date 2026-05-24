using FFmpeg.AutoGen;

namespace Sussudio.Services.Recording;

internal sealed unsafe partial class LibAvEncoder
{
    private void InitializeAudioIfNeeded(LibAvEncoderOptions options)
    {
        if (!options.AudioEnabled)
        {
            return;
        }

        var codec = ffmpeg.avcodec_find_encoder(AVCodecID.AV_CODEC_ID_AAC);
        if (codec == null)
        {
            throw CreateLibAvException("LIBAV_ENCODER_ERROR operation=avcodec_find_encoder(audio) codec='aac' msg=Encoder not available.");
        }

        _audio.Stream = ffmpeg.avformat_new_stream(_formatCtx, codec);
        if (_audio.Stream == null)
        {
            throw CreateLibAvException("LIBAV_ENCODER_ERROR operation=avformat_new_stream(audio) msg=Stream allocation returned null.");
        }

        _audio.CodecCtx = ffmpeg.avcodec_alloc_context3(codec);
        if (_audio.CodecCtx == null)
        {
            throw CreateLibAvException("LIBAV_ENCODER_ERROR operation=avcodec_alloc_context3(audio) msg=Codec context allocation returned null.");
        }

        ConfigureAudioCodecContext(_audio.CodecCtx, options, codec);

        // Skip GLOBAL_HEADER for MPEG-TS; AAC needs ADTS framing per segment.
        if (options.ContainerFormat != "mpegts" &&
            (_formatCtx->oformat->flags & ffmpeg.AVFMT_GLOBALHEADER) != 0)
        {
            _audio.CodecCtx->flags |= ffmpeg.AV_CODEC_FLAG_GLOBAL_HEADER;
        }

        ThrowIfError(ffmpeg.avcodec_open2(_audio.CodecCtx, codec, null), "avcodec_open2(audio)");

        _audio.FrameSize = _audio.CodecCtx->frame_size;
        if (_audio.FrameSize <= 0)
        {
            throw CreateLibAvException(
                $"LIBAV_ENCODER_ERROR operation=InitializeAudioIfNeeded msg=Unexpected AAC frame size value={_audio.FrameSize}");
        }

        _audio.Stream->time_base = _audio.CodecCtx->time_base;
        _audio.CachedTimeBase = _audio.CodecCtx->time_base;

        ThrowIfError(
            ffmpeg.avcodec_parameters_from_context(_audio.Stream->codecpar, _audio.CodecCtx),
            "avcodec_parameters_from_context(audio)");

        InitializeAudioResampler(options);
        AllocateAudioFrame();
        AllocateAudioAccumulator(options);
        AllocateAudioSampleQueue(options);
    }

    private void InitializeMicrophoneIfNeeded(LibAvEncoderOptions options)
    {
        if (!options.MicrophoneEnabled)
        {
            return;
        }

        var codec = ffmpeg.avcodec_find_encoder(AVCodecID.AV_CODEC_ID_AAC);
        if (codec == null)
        {
            throw CreateLibAvException("LIBAV_ENCODER_ERROR operation=avcodec_find_encoder(mic) codec='aac' msg=Encoder not available.");
        }

        _mic.Stream = ffmpeg.avformat_new_stream(_formatCtx, codec);
        if (_mic.Stream == null)
        {
            throw CreateLibAvException("LIBAV_ENCODER_ERROR operation=avformat_new_stream(mic) msg=Stream allocation returned null.");
        }

        _mic.CodecCtx = ffmpeg.avcodec_alloc_context3(codec);
        if (_mic.CodecCtx == null)
        {
            throw CreateLibAvException("LIBAV_ENCODER_ERROR operation=avcodec_alloc_context3(mic) msg=Codec context allocation returned null.");
        }

        _mic.CodecCtx->codec_type = AVMediaType.AVMEDIA_TYPE_AUDIO;
        _mic.CodecCtx->sample_rate = options.MicrophoneSampleRate;
        _mic.CodecCtx->sample_fmt = AVSampleFormat.AV_SAMPLE_FMT_FLTP;
        _mic.CodecCtx->bit_rate = options.MicrophoneBitRate;
        _mic.CodecCtx->time_base = new AVRational { num = 1, den = options.MicrophoneSampleRate };
        ffmpeg.av_channel_layout_default(&_mic.CodecCtx->ch_layout, options.MicrophoneChannels);

        if (!IsSampleFormatSupported(codec, _mic.CodecCtx->sample_fmt))
        {
            throw CreateLibAvException(
                $"LIBAV_ENCODER_ERROR operation=InitializeMicrophoneIfNeeded msg=Requested sample format '{_mic.CodecCtx->sample_fmt}' is not supported by AAC encoder.");
        }

        // Skip GLOBAL_HEADER for MPEG-TS; AAC needs ADTS framing per segment.
        if (options.ContainerFormat != "mpegts" &&
            (_formatCtx->oformat->flags & ffmpeg.AVFMT_GLOBALHEADER) != 0)
        {
            _mic.CodecCtx->flags |= ffmpeg.AV_CODEC_FLAG_GLOBAL_HEADER;
        }

        ThrowIfError(ffmpeg.avcodec_open2(_mic.CodecCtx, codec, null), "avcodec_open2(mic)");

        _mic.FrameSize = _mic.CodecCtx->frame_size;
        if (_mic.FrameSize <= 0)
        {
            throw CreateLibAvException(
                $"LIBAV_ENCODER_ERROR operation=InitializeMicrophoneIfNeeded msg=Unexpected AAC frame size value={_mic.FrameSize}");
        }

        _mic.Stream->time_base = _mic.CodecCtx->time_base;
        _mic.CachedTimeBase = _mic.CodecCtx->time_base;

        ThrowIfError(
            ffmpeg.avcodec_parameters_from_context(_mic.Stream->codecpar, _mic.CodecCtx),
            "avcodec_parameters_from_context(mic)");

        AVChannelLayout inputLayout = default;
        ffmpeg.av_channel_layout_default(&inputLayout, options.MicrophoneChannels);
        var swrCtx = _mic.SwrCtx;
        try
        {
            var result = ffmpeg.swr_alloc_set_opts2(
                &swrCtx,
                &_mic.CodecCtx->ch_layout,
                _mic.CodecCtx->sample_fmt,
                _mic.CodecCtx->sample_rate,
                &inputLayout,
                AVSampleFormat.AV_SAMPLE_FMT_FLT,
                options.MicrophoneSampleRate,
                0,
                null);
            _mic.SwrCtx = swrCtx;
            ThrowIfError(result, "swr_alloc_set_opts2(mic)");
            if (_mic.SwrCtx == null)
            {
                throw CreateLibAvException("LIBAV_ENCODER_ERROR operation=swr_alloc_set_opts2(mic) msg=Resampler allocation returned null.");
            }

            ThrowIfError(ffmpeg.swr_init(_mic.SwrCtx), "swr_init(mic)");
        }
        finally
        {
            ffmpeg.av_channel_layout_uninit(&inputLayout);
        }

        _mic.Frame = ffmpeg.av_frame_alloc();
        if (_mic.Frame == null)
        {
            throw CreateLibAvException("LIBAV_ENCODER_ERROR operation=av_frame_alloc(mic) msg=Frame allocation returned null.");
        }

        _mic.Frame->format = (int)_mic.CodecCtx->sample_fmt;
        _mic.Frame->nb_samples = _mic.FrameSize;
        _mic.Frame->sample_rate = _mic.CodecCtx->sample_rate;
        ThrowIfError(ffmpeg.av_channel_layout_copy(&_mic.Frame->ch_layout, &_mic.CodecCtx->ch_layout), "av_channel_layout_copy(mic_frame)");
        ThrowIfError(ffmpeg.av_frame_get_buffer(_mic.Frame, 0), "av_frame_get_buffer(mic)");
        if (_mic.Frame->extended_data == null)
        {
            throw CreateLibAvException("LIBAV_ENCODER_ERROR operation=av_frame_get_buffer(mic) msg=extended_data was null.");
        }

        _mic.AccumulatorCapacity = checked(_mic.FrameSize * options.MicrophoneChannels * sizeof(float));
        _mic.ResampleBuffer = (byte*)ffmpeg.av_malloc((ulong)_mic.AccumulatorCapacity);
        if (_mic.ResampleBuffer == null)
        {
            throw CreateLibAvException(
                $"LIBAV_ENCODER_ERROR operation=av_malloc(mic_accumulator) msg=Allocation returned null size={_mic.AccumulatorCapacity}.");
        }

        _mic.SampleQueueCapacity = checked((_mic.FrameSize * 2) + MaxDriftCorrectionSamplesPerPass);
        var queueBytes = checked(_mic.SampleQueueCapacity * options.MicrophoneChannels * sizeof(float));
        _mic.SampleQueueBuffer = (byte*)ffmpeg.av_malloc((ulong)queueBytes);
        if (_mic.SampleQueueBuffer == null)
        {
            throw CreateLibAvException(
                $"LIBAV_ENCODER_ERROR operation=av_malloc(mic_sample_queue) msg=Allocation returned null size={queueBytes}.");
        }
    }

    private void ConfigureAudioCodecContext(AVCodecContext* codecContext, LibAvEncoderOptions options, AVCodec* codec)
    {
        codecContext->codec_type = AVMediaType.AVMEDIA_TYPE_AUDIO;
        codecContext->sample_rate = options.AudioSampleRate;
        codecContext->sample_fmt = AVSampleFormat.AV_SAMPLE_FMT_FLTP;
        codecContext->bit_rate = options.AudioBitRate;
        codecContext->time_base = new AVRational { num = 1, den = options.AudioSampleRate };
        ffmpeg.av_channel_layout_default(&codecContext->ch_layout, options.AudioChannels);

        if (!IsSampleFormatSupported(codec, codecContext->sample_fmt))
        {
            throw CreateLibAvException(
                $"LIBAV_ENCODER_ERROR operation=ConfigureAudioCodecContext msg=Requested sample format '{codecContext->sample_fmt}' is not supported by AAC encoder.");
        }
    }

    private void InitializeAudioResampler(LibAvEncoderOptions options)
    {
        AVChannelLayout inputLayout = default;
        ffmpeg.av_channel_layout_default(&inputLayout, options.AudioChannels);
        var swrCtx = _audio.SwrCtx;

        try
        {
            var result = ffmpeg.swr_alloc_set_opts2(
                &swrCtx,
                &_audio.CodecCtx->ch_layout,
                _audio.CodecCtx->sample_fmt,
                _audio.CodecCtx->sample_rate,
                &inputLayout,
                AVSampleFormat.AV_SAMPLE_FMT_FLT,
                options.AudioSampleRate,
                0,
                null);
            _audio.SwrCtx = swrCtx;
            ThrowIfError(result, "swr_alloc_set_opts2");
            if (_audio.SwrCtx == null)
            {
                throw CreateLibAvException("LIBAV_ENCODER_ERROR operation=swr_alloc_set_opts2 msg=Resampler allocation returned null.");
            }

            ThrowIfError(ffmpeg.swr_init(_audio.SwrCtx), "swr_init");
        }
        finally
        {
            ffmpeg.av_channel_layout_uninit(&inputLayout);
        }
    }

    private void AllocateAudioFrame()
    {
        _audio.Frame = ffmpeg.av_frame_alloc();
        if (_audio.Frame == null)
        {
            throw CreateLibAvException("LIBAV_ENCODER_ERROR operation=av_frame_alloc(audio) msg=Frame allocation returned null.");
        }

        _audio.Frame->format = (int)_audio.CodecCtx->sample_fmt;
        _audio.Frame->nb_samples = _audio.FrameSize;
        _audio.Frame->sample_rate = _audio.CodecCtx->sample_rate;
        ThrowIfError(ffmpeg.av_channel_layout_copy(&_audio.Frame->ch_layout, &_audio.CodecCtx->ch_layout), "av_channel_layout_copy(audio_frame)");
        ThrowIfError(ffmpeg.av_frame_get_buffer(_audio.Frame, 0), "av_frame_get_buffer(audio)");

        if (_audio.Frame->extended_data == null)
        {
            throw CreateLibAvException("LIBAV_ENCODER_ERROR operation=av_frame_get_buffer(audio) msg=extended_data was null.");
        }
    }

    private void AllocateAudioAccumulator(LibAvEncoderOptions options)
    {
        _audio.AccumulatorCapacity = checked(_audio.FrameSize * options.AudioChannels * sizeof(float));
        _audio.ResampleBuffer = (byte*)ffmpeg.av_malloc((ulong)_audio.AccumulatorCapacity);
        if (_audio.ResampleBuffer == null)
        {
            throw CreateLibAvException(
                $"LIBAV_ENCODER_ERROR operation=av_malloc(audio_accumulator) msg=Allocation returned null size={_audio.AccumulatorCapacity}.");
        }
    }

    private void AllocateAudioSampleQueue(LibAvEncoderOptions options)
    {
        _audio.SampleQueueCapacity = checked((_audio.FrameSize * 2) + MaxDriftCorrectionSamplesPerPass);
        var queueBytes = checked(_audio.SampleQueueCapacity * options.AudioChannels * sizeof(float));
        _audio.SampleQueueBuffer = (byte*)ffmpeg.av_malloc((ulong)queueBytes);
        if (_audio.SampleQueueBuffer == null)
        {
            throw CreateLibAvException(
                $"LIBAV_ENCODER_ERROR operation=av_malloc(audio_sample_queue) msg=Allocation returned null size={queueBytes}.");
        }
    }
}

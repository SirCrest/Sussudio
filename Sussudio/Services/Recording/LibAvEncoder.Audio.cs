using System;
using FFmpeg.AutoGen;

namespace Sussudio.Services.Recording;

internal sealed unsafe partial class LibAvEncoder
{
    /// <summary>
    /// State for a single AAC audio stream (audio-in or microphone).
    /// All pointer-typed fields are valid only while the encoder is open.
    /// </summary>
    private unsafe struct AudioStreamState
    {
        public AVCodecContext* CodecCtx;
        public AVStream* Stream;
        public AVFrame* Frame;
        public SwrContext* SwrCtx;
        public int FrameSize;
        /// <summary>Capacity of <see cref="ResampleBuffer"/> in bytes.</summary>
        public int AccumulatorCapacity;
        /// <summary>Interleaved-float accumulator for partial input frames.</summary>
        public byte* ResampleBuffer;
        /// <summary>Capacity of <see cref="SampleQueueBuffer"/> in samples per channel.</summary>
        public int SampleQueueCapacity;
        /// <summary>Planar-float sample queue awaiting encoding.</summary>
        public byte* SampleQueueBuffer;
        /// <summary>Number of valid samples currently in <see cref="SampleQueueBuffer"/>.</summary>
        public int BufferedSamples;
        /// <summary>Number of bytes currently in <see cref="ResampleBuffer"/>.</summary>
        public int AccumulatorBytes;
        /// <summary>Running PTS counter (in samples) for this stream.</summary>
        public long NextPts;
        /// <summary>Cached copy of <see cref="AVCodecContext.time_base"/> set at open time.</summary>
        public AVRational CachedTimeBase;
    }

    private long _audioSamplesReceived;
    private long _micSamplesReceived;
    private AudioStreamState _audio;
    private AudioStreamState _mic;

    public long AudioSamplesReceived => _audioSamplesReceived;
    public long MicrophoneSamplesReceived => _micSamplesReceived;
    public bool AudioEnabled => _options?.AudioEnabled == true && _audio.CodecCtx != null && _audio.Stream != null;
    public bool MicrophoneEnabled => _options?.MicrophoneEnabled == true && _mic.CodecCtx != null && _mic.Stream != null;

    public void SendAudioSamples(ReadOnlySpan<byte> f32leSamples)
    {
        EnsureOpen();

        if (_audio.CodecCtx == null || _audio.Stream == null || _audio.Frame == null || _audio.SwrCtx == null || f32leSamples.IsEmpty)
        {
            return;
        }

        var options = _options ?? throw new InvalidOperationException("Encoder options are not initialized.");
        var inputBlockAlign = checked(options.AudioChannels * sizeof(float));
        if (f32leSamples.Length % inputBlockAlign != 0)
        {
            throw CreateLibAvException(
                $"LIBAV_ENCODER_ERROR operation=SendAudioSamples msg=Audio payload length is not aligned actual={f32leSamples.Length} block_align={inputBlockAlign}");
        }

        _audioSamplesReceived += f32leSamples.Length / inputBlockAlign;

        var remaining = f32leSamples;
        var frameBytes = checked(_audio.FrameSize * inputBlockAlign);

        if (_audio.AccumulatorBytes > 0)
        {
            var bytesNeeded = frameBytes - _audio.AccumulatorBytes;
            var copyBytes = Math.Min(bytesNeeded, remaining.Length);
            CopyToAccumulator(ref _audio, remaining[..copyBytes], _audio.AccumulatorBytes);
            _audio.AccumulatorBytes += copyBytes;
            remaining = remaining[copyBytes..];

            if (_audio.AccumulatorBytes == frameBytes)
            {
                EncodeStreamChunk(ref _audio, _audio.ResampleBuffer, _audio.FrameSize,
                    trackDriftCorrection: true, DriftCorrectionThresholdMs);
                _audio.AccumulatorBytes = 0;
            }
        }

        while (remaining.Length >= frameBytes)
        {
            var frameSlice = remaining[..frameBytes];
            fixed (byte* inputPtr = frameSlice)
            {
                EncodeStreamChunk(ref _audio, inputPtr, _audio.FrameSize,
                    trackDriftCorrection: true, DriftCorrectionThresholdMs);
            }

            remaining = remaining[frameBytes..];
        }

        if (!remaining.IsEmpty)
        {
            CopyToAccumulator(ref _audio, remaining, 0);
            _audio.AccumulatorBytes = remaining.Length;
        }
    }

    public void SendMicrophoneSamples(ReadOnlySpan<byte> f32leSamples)
    {
        EnsureOpen();

        if (_mic.CodecCtx == null || _mic.Stream == null || _mic.Frame == null || _mic.SwrCtx == null || f32leSamples.IsEmpty)
        {
            return;
        }

        var options = _options ?? throw new InvalidOperationException("Encoder options are not initialized.");
        var inputBlockAlign = checked(options.MicrophoneChannels * sizeof(float));
        if (f32leSamples.Length % inputBlockAlign != 0)
        {
            throw CreateLibAvException(
                $"LIBAV_ENCODER_ERROR operation=SendMicrophoneSamples msg=Audio payload length is not aligned actual={f32leSamples.Length} block_align={inputBlockAlign}");
        }

        _micSamplesReceived += f32leSamples.Length / inputBlockAlign;

        var remaining = f32leSamples;
        var frameBytes = checked(_mic.FrameSize * inputBlockAlign);

        if (_mic.AccumulatorBytes > 0)
        {
            var bytesNeeded = frameBytes - _mic.AccumulatorBytes;
            var copyBytes = Math.Min(bytesNeeded, remaining.Length);
            CopyToAccumulator(ref _mic, remaining[..copyBytes], _mic.AccumulatorBytes);
            _mic.AccumulatorBytes += copyBytes;
            remaining = remaining[copyBytes..];

            if (_mic.AccumulatorBytes == frameBytes)
            {
                EncodeStreamChunk(ref _mic, _mic.ResampleBuffer, _mic.FrameSize,
                    trackDriftCorrection: false, MicDriftCorrectionThresholdMs);
                _mic.AccumulatorBytes = 0;
            }
        }

        while (remaining.Length >= frameBytes)
        {
            var frameSlice = remaining[..frameBytes];
            fixed (byte* inputPtr = frameSlice)
            {
                EncodeStreamChunk(ref _mic, inputPtr, _mic.FrameSize,
                    trackDriftCorrection: false, MicDriftCorrectionThresholdMs);
            }

            remaining = remaining[frameBytes..];
        }

        if (!remaining.IsEmpty)
        {
            CopyToAccumulator(ref _mic, remaining, 0);
            _mic.AccumulatorBytes = remaining.Length;
        }
    }

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

        // Skip GLOBAL_HEADER for MPEG-TS — AAC needs ADTS framing per segment.
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

        // Skip GLOBAL_HEADER for MPEG-TS — AAC needs ADTS framing per segment.
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

    private void DrainStreamEncoderPackets(ref AudioStreamState s)
    {
        while (true)
        {
            var receiveResult = ffmpeg.avcodec_receive_packet(s.CodecCtx, _packet);
            if (receiveResult == ffmpeg.AVERROR(ffmpeg.EAGAIN) || receiveResult == ffmpeg.AVERROR_EOF)
            {
                return;
            }

            ThrowIfError(receiveResult, "avcodec_receive_packet(audio)");

            try
            {
                WriteStreamPacket(ref s, _packet);
            }
            finally
            {
                ffmpeg.av_packet_unref(_packet);
            }
        }
    }

    private void WriteStreamPacket(ref AudioStreamState s, AVPacket* packet)
    {
        ffmpeg.av_packet_rescale_ts(packet, s.CodecCtx->time_base, s.Stream->time_base);
        packet->stream_index = s.Stream->index;
        var packetSize = packet->size;
        ThrowIfError(ffmpeg.av_interleaved_write_frame(_formatCtx, packet), "av_interleaved_write_frame(audio)");
        _totalBytesWritten += packetSize;
    }

    private void EncodeStreamChunk(ref AudioStreamState s, byte* inputPtr, int inputSamples,
        bool trackDriftCorrection, double driftCorrectionThresholdMs)
    {
        if (s.CodecCtx == null || s.Stream == null || s.Frame == null || s.SwrCtx == null || inputSamples <= 0)
        {
            return;
        }

        var channelCount = GetStreamChannelCount(ref s);
        if (s.SampleQueueBuffer == null || s.SampleQueueCapacity <= 0)
        {
            throw CreateLibAvException("LIBAV_ENCODER_ERROR operation=EncodeStreamChunk msg=Audio sample queue is not allocated.");
        }

        if (s.BufferedSamples < 0 || s.BufferedSamples > s.SampleQueueCapacity)
        {
            throw CreateLibAvException(
                $"LIBAV_ENCODER_ERROR operation=EncodeStreamChunk msg=Audio queue sample count was out of range buffered={s.BufferedSamples} capacity={s.SampleQueueCapacity}.");
        }

        var availableSamples = s.SampleQueueCapacity - s.BufferedSamples;
        if (availableSamples < inputSamples + MaxDriftCorrectionSamplesPerPass)
        {
            throw CreateLibAvException(
                $"LIBAV_ENCODER_ERROR operation=EncodeStreamChunk msg=Audio queue capacity exhausted buffered={s.BufferedSamples} available={availableSamples} requested={inputSamples}.");
        }

        var inputData = stackalloc byte*[1];
        inputData[0] = inputPtr;

        var outputData = stackalloc byte*[channelCount];
        for (var channel = 0; channel < channelCount; channel++)
        {
            outputData[channel] = (byte*)(GetStreamQueuePlane(ref s, channel) + s.BufferedSamples);
        }

        var convertedSamples = ffmpeg.swr_convert(
            s.SwrCtx,
            outputData,
            availableSamples,
            inputData,
            inputSamples);
        if (convertedSamples < 0)
        {
            ThrowIfError(convertedSamples, "swr_convert");
        }

        var queuedSamples = s.BufferedSamples + convertedSamples;
        var queuedStreamSamples = s.NextPts + queuedSamples;
        var correctionSamples = GetDriftCorrectionSamples(
            queuedStreamSamples,
            s.CodecCtx->sample_rate,
            out var correctionVideoFrame,
            out var driftMs,
            driftCorrectionThresholdMs);
        var appliedCorrectionSamples = 0;

        if (correctionSamples < 0)
        {
            var trimmedSamples = Math.Min(-correctionSamples, queuedSamples);
            queuedSamples -= trimmedSamples;
            appliedCorrectionSamples -= trimmedSamples;
        }
        else if (correctionSamples > 0)
        {
            AppendSilentStreamSamples(ref s, queuedSamples, correctionSamples, channelCount);
            queuedSamples += correctionSamples;
            appliedCorrectionSamples += correctionSamples;
        }

        if (trackDriftCorrection && (correctionSamples == 0 || appliedCorrectionSamples == correctionSamples))
        {
            _lastDriftCorrectionVideoFrame = correctionVideoFrame;
        }

        s.BufferedSamples = queuedSamples;
        DrainBufferedFrames(ref s, flushPartialFrame: false);

        if (trackDriftCorrection && appliedCorrectionSamples != 0)
        {
            _driftCorrectionAppliedSamples += appliedCorrectionSamples;
            Logger.Log(
                $"LIBAV_AV_DRIFT_CORRECTION videoFrame={_nextVideoPts} driftMs={driftMs:F1} " +
                $"correctionSamples={appliedCorrectionSamples} totalCorrectionSamples={_driftCorrectionAppliedSamples}");
        }
    }

    private void DrainBufferedFrames(ref AudioStreamState s, bool flushPartialFrame)
    {
        while (s.BufferedSamples >= s.FrameSize || (flushPartialFrame && s.BufferedSamples > 0))
        {
            var sampleCount = s.BufferedSamples >= s.FrameSize
                ? s.FrameSize
                : s.BufferedSamples;
            SendPreparedStreamFrame(ref s, sampleCount);
            RemoveQueuedStreamSamples(ref s, sampleCount);
        }
    }

    private void SendPreparedStreamFrame(ref AudioStreamState s, int sampleCount)
    {
        if (s.CodecCtx == null || s.Frame == null || sampleCount <= 0)
        {
            return;
        }

        ThrowIfError(ffmpeg.av_frame_make_writable(s.Frame), "av_frame_make_writable(audio)");
        CopyQueuedSamplesToStreamFrame(ref s, sampleCount);

        s.Frame->nb_samples = sampleCount;
        var nextPts = s.NextPts;
        s.Frame->pts = nextPts;

        var sendResult = ffmpeg.avcodec_send_frame(s.CodecCtx, s.Frame);
        if (sendResult == ffmpeg.AVERROR(ffmpeg.EAGAIN))
        {
            DrainStreamEncoderPackets(ref s);
            sendResult = ffmpeg.avcodec_send_frame(s.CodecCtx, s.Frame);
        }

        ThrowIfError(sendResult, "avcodec_send_frame(audio)");
        s.NextPts = nextPts + sampleCount;
        DrainStreamEncoderPackets(ref s);
    }

    private void CopyQueuedSamplesToStreamFrame(ref AudioStreamState s, int sampleCount)
    {
        if (s.CodecCtx == null || s.Frame == null || s.Frame->extended_data == null)
        {
            throw CreateLibAvException("LIBAV_ENCODER_ERROR operation=CopyQueuedSamplesToStreamFrame msg=Audio frame storage was not initialized.");
        }

        var bytesPerSample = ffmpeg.av_get_bytes_per_sample(s.CodecCtx->sample_fmt);
        if (bytesPerSample <= 0)
        {
            throw CreateLibAvException(
                $"LIBAV_ENCODER_ERROR operation=CopyQueuedSamplesToStreamFrame msg=Unsupported sample format '{s.CodecCtx->sample_fmt}'.");
        }

        var channelCount = GetStreamChannelCount(ref s);
        if (ffmpeg.av_sample_fmt_is_planar(s.CodecCtx->sample_fmt) == 0)
        {
            throw CreateLibAvException("LIBAV_ENCODER_ERROR operation=CopyQueuedSamplesToStreamFrame msg=Expected planar audio frame layout.");
        }

        var planeBytes = sampleCount * bytesPerSample;
        for (var channel = 0; channel < channelCount; channel++)
        {
            var source = GetStreamQueuePlane(ref s, channel);
            var destination = (float*)s.Frame->extended_data[channel];
            if (destination == null)
            {
                throw CreateLibAvException(
                    $"LIBAV_ENCODER_ERROR operation=CopyQueuedSamplesToStreamFrame msg=Audio plane pointer was null channel={channel}.");
            }

            Buffer.MemoryCopy(source, destination, planeBytes, planeBytes);
        }
    }

    private void RemoveQueuedStreamSamples(ref AudioStreamState s, int sampleCount)
    {
        if (sampleCount <= 0)
        {
            return;
        }

        if (sampleCount > s.BufferedSamples)
        {
            throw CreateLibAvException(
                $"LIBAV_ENCODER_ERROR operation=RemoveQueuedStreamSamples msg=Cannot remove more samples than buffered remove={sampleCount} buffered={s.BufferedSamples}.");
        }

        var remainingSamples = s.BufferedSamples - sampleCount;
        if (remainingSamples > 0)
        {
            var channelCount = GetStreamChannelCount(ref s);
            for (var channel = 0; channel < channelCount; channel++)
            {
                var plane = GetStreamQueuePlane(ref s, channel);
                new ReadOnlySpan<float>(plane + sampleCount, remainingSamples)
                    .CopyTo(new Span<float>(plane, remainingSamples));
            }
        }

        s.BufferedSamples = remainingSamples;
    }

    private void AppendSilentStreamSamples(ref AudioStreamState s, int startSample, int sampleCount, int channelCount)
    {
        if (sampleCount <= 0)
        {
            return;
        }

        for (var channel = 0; channel < channelCount; channel++)
        {
            new Span<float>(GetStreamQueuePlane(ref s, channel) + startSample, sampleCount).Clear();
        }
    }

    private float* GetStreamQueuePlane(ref AudioStreamState s, int channel)
    {
        if (s.SampleQueueBuffer == null || s.SampleQueueCapacity <= 0)
        {
            throw CreateLibAvException("LIBAV_ENCODER_ERROR operation=GetStreamQueuePlane msg=Audio sample queue was not initialized.");
        }

        return (float*)(s.SampleQueueBuffer + (channel * s.SampleQueueCapacity * sizeof(float)));
    }

    private int GetStreamChannelCount(ref AudioStreamState s)
    {
        var channelCount = (int)(s.CodecCtx != null && s.CodecCtx->ch_layout.nb_channels > 0
            ? s.CodecCtx->ch_layout.nb_channels
            : 0);
        if (channelCount <= 0)
        {
            throw CreateLibAvException("LIBAV_ENCODER_ERROR operation=GetStreamChannelCount msg=Audio channel count was not available.");
        }

        return channelCount;
    }

    private void FlushPendingStreamSamples(ref AudioStreamState s, string streamLabel,
        bool trackDriftCorrection = false, double driftCorrectionThresholdMs = DriftCorrectionThresholdMs)
    {
        if (s.CodecCtx == null || s.Frame == null)
        {
            return;
        }

        if (s.AccumulatorBytes > 0)
        {
            var inputChannels = (int)(s.CodecCtx->ch_layout.nb_channels > 0
                ? s.CodecCtx->ch_layout.nb_channels
                : 0);
            var inputBlockAlign = checked(inputChannels * sizeof(float));
            if (inputBlockAlign <= 0)
            {
                throw CreateLibAvException(
                    $"LIBAV_ENCODER_ERROR operation=FlushPendingStreamSamples({streamLabel}) msg=Channel count was not available.");
            }

            if (s.AccumulatorBytes % inputBlockAlign != 0)
            {
                throw CreateLibAvException(
                    $"LIBAV_ENCODER_ERROR operation=FlushPendingStreamSamples({streamLabel}) msg=Accumulator is not sample-aligned bytes={s.AccumulatorBytes} block_align={inputBlockAlign}");
            }

            var pendingSamples = s.AccumulatorBytes / inputBlockAlign;
            if (pendingSamples > 0)
            {
                EncodeStreamChunk(ref s, s.ResampleBuffer, pendingSamples,
                    trackDriftCorrection, driftCorrectionThresholdMs);
            }

            s.AccumulatorBytes = 0;
        }

        DrainBufferedFrames(ref s, flushPartialFrame: true);
    }

    private void CopyToAccumulator(ref AudioStreamState s, ReadOnlySpan<byte> source, int destinationOffset)
    {
        if (source.IsEmpty)
        {
            return;
        }

        if (s.ResampleBuffer == null)
        {
            throw CreateLibAvException("LIBAV_ENCODER_ERROR operation=CopyToAccumulator msg=Audio accumulator buffer is null.");
        }

        fixed (byte* sourcePtr = source)
        {
            Buffer.MemoryCopy(
                sourcePtr,
                s.ResampleBuffer + destinationOffset,
                s.AccumulatorCapacity - destinationOffset,
                source.Length);
        }
    }
}

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
        /// <summary>Capacity of <see cref="InputAccumulatorBuffer"/> in bytes.</summary>
        public int AccumulatorCapacity;
        /// <summary>Interleaved-float accumulator for partial input frames.</summary>
        public byte* InputAccumulatorBuffer;
        /// <summary>Capacity of <see cref="SampleQueueBuffer"/> in samples per channel.</summary>
        public int SampleQueueCapacity;
        /// <summary>Planar-float sample queue awaiting encoding.</summary>
        public byte* SampleQueueBuffer;
        /// <summary>Number of valid samples currently in <see cref="SampleQueueBuffer"/>.</summary>
        public int BufferedSamples;
        /// <summary>Number of bytes currently in <see cref="InputAccumulatorBuffer"/>.</summary>
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
        => SendStreamSamples(ref _audio, f32leSamples, static o => o.AudioChannels, ref _audioSamplesReceived,
            trackDriftCorrection: true, DriftCorrectionThresholdMs, nameof(SendAudioSamples));

    public void SendMicrophoneSamples(ReadOnlySpan<byte> f32leSamples)
        => SendStreamSamples(ref _mic, f32leSamples, static o => o.MicrophoneChannels, ref _micSamplesReceived,
            trackDriftCorrection: false, MicDriftCorrectionThresholdMs, nameof(SendMicrophoneSamples));

    /// <summary>
    /// Shared body of <see cref="SendAudioSamples"/> and <see cref="SendMicrophoneSamples"/>.
    /// Accumulates interleaved-float samples into <paramref name="state"/> and encodes complete frames.
    /// </summary>
    private void SendStreamSamples(
        ref AudioStreamState state,
        ReadOnlySpan<byte> f32leSamples,
        Func<LibAvEncoderOptions, int> getChannels,
        ref long samplesReceived,
        bool trackDriftCorrection,
        double driftCorrectionThresholdMs,
        string operationName)
    {
        EnsureOpen();

        if (state.CodecCtx == null || state.Stream == null || state.Frame == null || state.SwrCtx == null || f32leSamples.IsEmpty)
        {
            return;
        }

        var options = _options ?? throw new InvalidOperationException("Encoder options are not initialized.");
        var inputBlockAlign = checked(getChannels(options) * sizeof(float));
        if (f32leSamples.Length % inputBlockAlign != 0)
        {
            throw CreateLibAvException(
                $"LIBAV_ENCODER_ERROR operation={operationName} msg=Audio payload length is not aligned actual={f32leSamples.Length} block_align={inputBlockAlign}");
        }

        samplesReceived += f32leSamples.Length / inputBlockAlign;

        var remaining = f32leSamples;
        var frameBytes = checked(state.FrameSize * inputBlockAlign);

        if (state.AccumulatorBytes > 0)
        {
            var bytesNeeded = frameBytes - state.AccumulatorBytes;
            var copyBytes = Math.Min(bytesNeeded, remaining.Length);
            CopyToAccumulator(ref state, remaining[..copyBytes], state.AccumulatorBytes);
            state.AccumulatorBytes += copyBytes;
            remaining = remaining[copyBytes..];

            if (state.AccumulatorBytes == frameBytes)
            {
                EncodeStreamChunk(ref state, state.InputAccumulatorBuffer, state.FrameSize,
                    trackDriftCorrection, driftCorrectionThresholdMs);
                state.AccumulatorBytes = 0;
            }
        }

        while (remaining.Length >= frameBytes)
        {
            var frameSlice = remaining[..frameBytes];
            fixed (byte* inputPtr = frameSlice)
            {
                EncodeStreamChunk(ref state, inputPtr, state.FrameSize,
                    trackDriftCorrection, driftCorrectionThresholdMs);
            }

            remaining = remaining[frameBytes..];
        }

        if (!remaining.IsEmpty)
        {
            CopyToAccumulator(ref state, remaining, 0);
            state.AccumulatorBytes = remaining.Length;
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
                EncodeStreamChunk(ref s, s.InputAccumulatorBuffer, pendingSamples,
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

        if (s.InputAccumulatorBuffer == null)
        {
            throw CreateLibAvException("LIBAV_ENCODER_ERROR operation=CopyToAccumulator msg=Audio accumulator buffer is null.");
        }

        fixed (byte* sourcePtr = source)
        {
            Buffer.MemoryCopy(
                sourcePtr,
                s.InputAccumulatorBuffer + destinationOffset,
                s.AccumulatorCapacity - destinationOffset,
                source.Length);
        }
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

    private const long AvSyncLogCadenceFrames = 300;
    private const long MinimumAvSyncVideoFrames = 30;
    // Drift correction disabled: capture card audio and video share the same USB bus
    // clock, so there is no ongoing drift. The apparent drift at startup is a one-time
    // offset from WASAPI pre-buffering. Correcting it causes audible pops because 480-
    // sample block insertions/removals create hard discontinuities in the waveform.
    // Players handle the small initial A/V offset in the container transparently.
    private const double DriftCorrectionThresholdMs = double.MaxValue;
    private const double MicDriftCorrectionThresholdMs = 200.0;
    private const int MaxDriftCorrectionSamplesPerPass = 480;

    private long _lastSyncLogVideoFrame;
    private long _driftCorrectionAppliedSamples;
    private long _lastDriftCorrectionVideoFrame;

    private int GetDriftCorrectionSamples(long audioSamples, int sampleRate, out long correctionVideoFrame, out double driftMs,
        double thresholdMs = DriftCorrectionThresholdMs)
    {
        correctionVideoFrame = 0;
        driftMs = 0.0;

        if (_options == null ||
            (!_options.AudioEnabled && !_options.MicrophoneEnabled) ||
            _nextVideoPts < MinimumAvSyncVideoFrames ||
            _nextVideoPts - _lastDriftCorrectionVideoFrame < AvSyncLogCadenceFrames)
        {
            return 0;
        }

        if (!TryGetAvSyncState(audioSamples, out var videoFrame, out _, out _, out _, out driftMs))
        {
            return 0;
        }

        correctionVideoFrame = videoFrame;
        if (Math.Abs(driftMs) <= thresholdMs || sampleRate <= 0)
        {
            return 0;
        }

        var correctionSamples = (int)(-(driftMs / 1000.0) * sampleRate);
        return Math.Clamp(correctionSamples, -MaxDriftCorrectionSamplesPerPass, MaxDriftCorrectionSamplesPerPass);
    }

    public bool TryGetCurrentAvSyncDrift(out double driftMs, out long correctionSamples)
    {
        driftMs = 0.0;
        correctionSamples = _driftCorrectionAppliedSamples;

        // Use cached time_base values instead of dereferencing codec context pointers.
        // The codec contexts can be freed by FlushAndClose on the encoding thread,
        // but the cached time_base structs are plain value types set once during open.
        var vtb = _cachedVideoTimeBase;
        var atb = _audio.CachedTimeBase;
        if (vtb.num <= 0 || vtb.den <= 0 || atb.num <= 0 || atb.den <= 0)
        {
            return false;
        }

        var videoFrame = _nextVideoPts;
        var audioSamples = _audio.NextPts + _audio.BufferedSamples;
        var videoTimeSec = videoFrame * vtb.num / (double)vtb.den;
        var audioTimeSec = audioSamples * atb.num / (double)atb.den;
        driftMs = (audioTimeSec - videoTimeSec) * 1000.0;
        return true;
    }

    private void LogAvSyncIfDue()
    {
        if (_options == null ||
            !_options.AudioEnabled ||
            _nextVideoPts < MinimumAvSyncVideoFrames ||
            _nextVideoPts - _lastSyncLogVideoFrame < AvSyncLogCadenceFrames)
        {
            return;
        }

        if (!TryGetAvSyncState(_audio.NextPts + _audio.BufferedSamples, out var videoFrame, out var videoTimeSec, out var audioSamples, out var audioTimeSec, out var driftMs))
        {
            return;
        }

        _lastSyncLogVideoFrame = videoFrame;

        Logger.Log(
            $"LIBAV_AV_SYNC videoFrame={videoFrame} videoSec={videoTimeSec:F3} " +
            $"audioSamples={audioSamples} audioSec={audioTimeSec:F3} driftMs={driftMs:F1} " +
            $"totalCorrectionSamples={_driftCorrectionAppliedSamples}");

        if (Math.Abs(driftMs) > 500.0)
        {
            Logger.Log(
                $"LIBAV_AV_SYNC_DRIFT_WARNING videoFrame={videoFrame} driftMs={driftMs:F1} " +
                $"audioSamples={audioSamples} — drift exceeds 500ms, investigate audio delivery");
        }
    }

    private bool TryGetAvSyncState(
        long audioSamples,
        out long videoFrame,
        out double videoTimeSec,
        out long reportedAudioSamples,
        out double audioTimeSec,
        out double driftMs)
    {
        videoFrame = _nextVideoPts;
        videoTimeSec = 0.0;
        reportedAudioSamples = audioSamples;
        audioTimeSec = 0.0;
        driftMs = 0.0;

        if (_videoCodecCtx == null ||
            _audio.CodecCtx == null ||
            _videoCodecCtx->time_base.num <= 0 ||
            _videoCodecCtx->time_base.den <= 0 ||
            _audio.CodecCtx->time_base.num <= 0 ||
            _audio.CodecCtx->time_base.den <= 0)
        {
            return false;
        }

        videoTimeSec = videoFrame * _videoCodecCtx->time_base.num / (double)_videoCodecCtx->time_base.den;
        audioTimeSec = reportedAudioSamples * _audio.CodecCtx->time_base.num / (double)_audio.CodecCtx->time_base.den;
        driftMs = (audioTimeSec - videoTimeSec) * 1000.0;
        return true;
    }

    private void InitializeAudioIfNeeded(LibAvEncoderOptions options)
    {
        if (!options.AudioEnabled)
        {
            return;
        }

        InitializeAudioStream(ref _audio, options.AudioSampleRate, options.AudioChannels,
            options.AudioBitRate, options.ContainerFormat, "audio");
    }

    private void InitializeMicrophoneIfNeeded(LibAvEncoderOptions options)
    {
        if (!options.MicrophoneEnabled)
        {
            return;
        }

        InitializeAudioStream(ref _mic, options.MicrophoneSampleRate, options.MicrophoneChannels,
            options.MicrophoneBitRate, options.ContainerFormat, "mic");
    }

    private void InitializeAudioStream(
        ref AudioStreamState state,
        int sampleRate,
        int channels,
        int bitRate,
        string containerFormat,
        string streamLabel)
    {
        var codec = ffmpeg.avcodec_find_encoder(AVCodecID.AV_CODEC_ID_AAC);
        if (codec == null)
        {
            throw CreateLibAvException($"LIBAV_ENCODER_ERROR operation=avcodec_find_encoder({streamLabel}) codec='aac' msg=Encoder not available.");
        }

        state.Stream = ffmpeg.avformat_new_stream(_formatCtx, codec);
        if (state.Stream == null)
        {
            throw CreateLibAvException($"LIBAV_ENCODER_ERROR operation=avformat_new_stream({streamLabel}) msg=Stream allocation returned null.");
        }

        state.CodecCtx = ffmpeg.avcodec_alloc_context3(codec);
        if (state.CodecCtx == null)
        {
            throw CreateLibAvException($"LIBAV_ENCODER_ERROR operation=avcodec_alloc_context3({streamLabel}) msg=Codec context allocation returned null.");
        }

        ConfigureAudioCodecContext(state.CodecCtx, sampleRate, channels, bitRate, codec, streamLabel);

        // Skip GLOBAL_HEADER for MPEG-TS; AAC needs ADTS framing per segment.
        if (containerFormat != "mpegts" &&
            (_formatCtx->oformat->flags & ffmpeg.AVFMT_GLOBALHEADER) != 0)
        {
            state.CodecCtx->flags |= ffmpeg.AV_CODEC_FLAG_GLOBAL_HEADER;
        }

        ThrowIfError(ffmpeg.avcodec_open2(state.CodecCtx, codec, null), $"avcodec_open2({streamLabel})");

        state.FrameSize = state.CodecCtx->frame_size;
        if (state.FrameSize <= 0)
        {
            var operation = streamLabel == "mic" ? nameof(InitializeMicrophoneIfNeeded) : nameof(InitializeAudioIfNeeded);
            throw CreateLibAvException(
                $"LIBAV_ENCODER_ERROR operation={operation} msg=Unexpected AAC frame size value={state.FrameSize}");
        }

        state.Stream->time_base = state.CodecCtx->time_base;
        state.CachedTimeBase = state.CodecCtx->time_base;

        ThrowIfError(
            ffmpeg.avcodec_parameters_from_context(state.Stream->codecpar, state.CodecCtx),
            $"avcodec_parameters_from_context({streamLabel})");

        InitializeAudioResampler(ref state, sampleRate, channels, streamLabel);
        AllocateAudioFrame(ref state, streamLabel);
        AllocateAudioAccumulator(ref state, channels, streamLabel);
        AllocateAudioSampleQueue(ref state, channels, streamLabel);
    }

    private void ConfigureAudioCodecContext(
        AVCodecContext* codecContext,
        int sampleRate,
        int channels,
        int bitRate,
        AVCodec* codec,
        string streamLabel)
    {
        codecContext->codec_type = AVMediaType.AVMEDIA_TYPE_AUDIO;
        codecContext->sample_rate = sampleRate;
        codecContext->sample_fmt = AVSampleFormat.AV_SAMPLE_FMT_FLTP;
        codecContext->bit_rate = bitRate;
        codecContext->time_base = new AVRational { num = 1, den = sampleRate };
        ffmpeg.av_channel_layout_default(&codecContext->ch_layout, channels);

        if (!IsSampleFormatSupported(codec, codecContext->sample_fmt))
        {
            var operation = streamLabel == "mic" ? nameof(InitializeMicrophoneIfNeeded) : nameof(ConfigureAudioCodecContext);
            throw CreateLibAvException(
                $"LIBAV_ENCODER_ERROR operation={operation} msg=Requested sample format '{codecContext->sample_fmt}' is not supported by AAC encoder.");
        }
    }

    private void InitializeAudioResampler(ref AudioStreamState state, int sampleRate, int channels, string streamLabel)
    {
        var diagnosticSuffix = streamLabel == "mic" ? "(mic)" : string.Empty;
        AVChannelLayout inputLayout = default;
        ffmpeg.av_channel_layout_default(&inputLayout, channels);
        var swrCtx = state.SwrCtx;

        try
        {
            var result = ffmpeg.swr_alloc_set_opts2(
                &swrCtx,
                &state.CodecCtx->ch_layout,
                state.CodecCtx->sample_fmt,
                state.CodecCtx->sample_rate,
                &inputLayout,
                AVSampleFormat.AV_SAMPLE_FMT_FLT,
                sampleRate,
                0,
                null);
            state.SwrCtx = swrCtx;
            ThrowIfError(result, $"swr_alloc_set_opts2{diagnosticSuffix}");
            if (state.SwrCtx == null)
            {
                throw CreateLibAvException($"LIBAV_ENCODER_ERROR operation=swr_alloc_set_opts2{diagnosticSuffix} msg=Resampler allocation returned null.");
            }

            ThrowIfError(ffmpeg.swr_init(state.SwrCtx), $"swr_init{diagnosticSuffix}");
        }
        finally
        {
            ffmpeg.av_channel_layout_uninit(&inputLayout);
        }
    }

    private void AllocateAudioFrame(ref AudioStreamState state, string streamLabel)
    {
        state.Frame = ffmpeg.av_frame_alloc();
        if (state.Frame == null)
        {
            throw CreateLibAvException($"LIBAV_ENCODER_ERROR operation=av_frame_alloc({streamLabel}) msg=Frame allocation returned null.");
        }

        state.Frame->format = (int)state.CodecCtx->sample_fmt;
        state.Frame->nb_samples = state.FrameSize;
        state.Frame->sample_rate = state.CodecCtx->sample_rate;
        ThrowIfError(ffmpeg.av_channel_layout_copy(&state.Frame->ch_layout, &state.CodecCtx->ch_layout), $"av_channel_layout_copy({streamLabel}_frame)");
        ThrowIfError(ffmpeg.av_frame_get_buffer(state.Frame, 0), $"av_frame_get_buffer({streamLabel})");

        if (state.Frame->extended_data == null)
        {
            throw CreateLibAvException($"LIBAV_ENCODER_ERROR operation=av_frame_get_buffer({streamLabel}) msg=extended_data was null.");
        }
    }

    private void AllocateAudioAccumulator(ref AudioStreamState state, int channels, string streamLabel)
    {
        state.AccumulatorCapacity = checked(state.FrameSize * channels * sizeof(float));
        state.InputAccumulatorBuffer = (byte*)ffmpeg.av_malloc((ulong)state.AccumulatorCapacity);
        if (state.InputAccumulatorBuffer == null)
        {
            throw CreateLibAvException(
                $"LIBAV_ENCODER_ERROR operation=av_malloc({streamLabel}_accumulator) msg=Allocation returned null size={state.AccumulatorCapacity}.");
        }
    }

    private void AllocateAudioSampleQueue(ref AudioStreamState state, int channels, string streamLabel)
    {
        state.SampleQueueCapacity = checked((state.FrameSize * 2) + MaxDriftCorrectionSamplesPerPass);
        var queueBytes = checked(state.SampleQueueCapacity * channels * sizeof(float));
        state.SampleQueueBuffer = (byte*)ffmpeg.av_malloc((ulong)queueBytes);
        if (state.SampleQueueBuffer == null)
        {
            throw CreateLibAvException(
                $"LIBAV_ENCODER_ERROR operation=av_malloc({streamLabel}_sample_queue) msg=Allocation returned null size={queueBytes}.");
        }
    }
}

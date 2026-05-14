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

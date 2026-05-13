using System;
using System.Buffers;
using System.Runtime.InteropServices;
using FFmpeg.AutoGen;

namespace Sussudio.Services.Flashback;

internal sealed unsafe partial class FlashbackDecoder
{
    // --- Audio packet delivery and output conversion ---

    private void InitializeAudioDecoder()
    {
        var audioStream = _formatCtx->streams[_audioStreamIndex];
        _audioTimeBase = audioStream->time_base;

        var codecPar = audioStream->codecpar;

        var codec = ffmpeg.avcodec_find_decoder(codecPar->codec_id);
        if (codec == null)
        {
            Logger.Log($"FLASHBACK_DECODER_AUDIO_WARN no decoder for codec_id={codecPar->codec_id}, audio disabled");
            _audioStreamIndex = -1;
            return;
        }

        _audioCodecCtx = ffmpeg.avcodec_alloc_context3(codec);
        if (_audioCodecCtx == null)
        {
            throw CreateException("Failed to allocate audio codec context.");
        }

        ThrowIfError(
            ffmpeg.avcodec_parameters_to_context(_audioCodecCtx, codecPar),
            "avcodec_parameters_to_context(audio)");

        ThrowIfError(
            ffmpeg.avcodec_open2(_audioCodecCtx, codec, null),
            "avcodec_open2(audio)");

        // Allocate audio decode frame
        _audioFrame = ffmpeg.av_frame_alloc();
        if (_audioFrame == null)
        {
            throw CreateException("Failed to allocate audio frame.");
        }

        // Set up SwrContext: decoded format (typically fltp) → f32le interleaved stereo 48kHz
        InitializeAudioResampler();

        var audioCodecName = codec->name != null ? Marshal.PtrToStringAnsi((IntPtr)codec->name) : "?";
        Logger.Log($"FLASHBACK_DECODER_AUDIO codec={audioCodecName} " +
                   $"sample_rate={_audioCodecCtx->sample_rate} sample_fmt={_audioCodecCtx->sample_fmt} " +
                   $"channels={_audioCodecCtx->ch_layout.nb_channels}");
    }

    private void InitializeAudioResampler()
    {
        AVChannelLayout outputLayout = default;
        ffmpeg.av_channel_layout_default(&outputLayout, OutputAudioChannels);

        var swrCtx = _swrCtx;

        try
        {
            var result = ffmpeg.swr_alloc_set_opts2(
                &swrCtx,
                &outputLayout,                              // output layout: stereo
                AVSampleFormat.AV_SAMPLE_FMT_FLT,           // output format: f32le interleaved
                OutputAudioSampleRate,                       // output sample rate: 48kHz
                &_audioCodecCtx->ch_layout,                 // input layout: from codec
                _audioCodecCtx->sample_fmt,                 // input format: from codec (typically fltp)
                _audioCodecCtx->sample_rate,                // input sample rate: from codec
                0,
                null);
            _swrCtx = swrCtx;
            ThrowIfError(result, "swr_alloc_set_opts2(decode)");

            if (_swrCtx == null)
            {
                throw CreateException("Failed to allocate audio resampler.");
            }

            ThrowIfError(ffmpeg.swr_init(_swrCtx), "swr_init(decode)");
        }
        finally
        {
            ffmpeg.av_channel_layout_uninit(&outputLayout);
        }
    }

    /// <summary>
    /// Sends an audio packet to the decoder and delivers any resulting chunks
    /// via <see cref="AudioChunkCallback"/>. If no callback is set, audio is
    /// silently decoded (keeps the decoder state advancing) but not delivered.
    /// </summary>
    private void DecodeAndDeliverAudioPacket(AVPacket* packet)
    {
        // Always feed packets to the codec so it tracks PTS position correctly.
        // During seek/scrub (callback null), we decode but discard the output.
        // This ensures audio and video codecs are at the same position when
        // playback starts - no suppression or drift compensation needed.
        var sendResult = ffmpeg.avcodec_send_packet(_audioCodecCtx, packet);
        if (sendResult < 0 && sendResult != ffmpeg.AVERROR(ffmpeg.EAGAIN))
            return;

        while (ffmpeg.avcodec_receive_frame(_audioCodecCtx, _audioFrame) == 0)
        {
            var callback = AudioChunkCallback;
            if (callback == null)
            {
                ffmpeg.av_frame_unref(_audioFrame);
                continue; // Codec advanced, but no delivery during seek/scrub
            }

            var chunk = ConvertAndOutputAudioFrame();
            if (chunk.ValidLength > 0)
            {
                try
                {
                    callback(chunk);
                }
                catch (Exception ex)
                {
                    Logger.Log($"FLASHBACK_DECODE_AUDIO_CALLBACK_FAIL type={ex.GetType().Name} msg={ex.Message}");
                    // Caller is responsible for returning buffer on success; on failure we must return it
                    if (chunk.Samples != null)
                        ArrayPool<byte>.Shared.Return(chunk.Samples);
                }
            }
            else if (chunk.Samples != null && chunk.Samples.Length > 0)
                ArrayPool<byte>.Shared.Return(chunk.Samples);
        }
    }

    private DecodedAudioChunk ConvertAndOutputAudioFrame()
    {
        var inputSamples = _audioFrame->nb_samples;
        var pts = DecodePtsToTimeSpan(ResolveBestEffortFrameTimestamp(_audioFrame), _audioTimeBase);
        byte[]? result = null;
        var returnResultToPool = true;

        try
        {
            if (inputSamples <= 0)
            {
                return new DecodedAudioChunk { Samples = Array.Empty<byte>(), ValidLength = 0, Pts = pts };
            }

            var maxOutputSamples = ffmpeg.swr_get_out_samples(_swrCtx, inputSamples);
            if (maxOutputSamples < 0)
            {
                maxOutputSamples = ToBoundedAudioSampleCount((long)inputSamples * 2);
            }

            if (!TryCalculateAudioBufferBytes(maxOutputSamples, out var outputBytesNeeded))
            {
                Logger.Log($"FLASHBACK_DECODER_AUDIO_WARN reason=invalid_output_size input_samples={inputSamples} max_output_samples={maxOutputSamples}");
                return new DecodedAudioChunk { Samples = Array.Empty<byte>(), ValidLength = 0, Pts = pts };
            }

            result = ArrayPool<byte>.Shared.Rent(outputBytesNeeded);

            int outputSamplesProduced;
            fixed (byte* outputPtr = result)
            {
                var outputPlanes = stackalloc byte*[1];
                outputPlanes[0] = outputPtr;

                outputSamplesProduced = ffmpeg.swr_convert(
                    _swrCtx,
                    outputPlanes, maxOutputSamples,
                    _audioFrame->extended_data, inputSamples);
            }

            if (outputSamplesProduced <= 0)
            {
                return new DecodedAudioChunk { Samples = Array.Empty<byte>(), ValidLength = 0, Pts = pts };
            }

            if (!TryCalculateAudioBufferBytes(outputSamplesProduced, out var validBytes) || validBytes > result.Length)
            {
                Logger.Log($"FLASHBACK_DECODER_AUDIO_WARN reason=invalid_converted_size output_samples={outputSamplesProduced} buffer_bytes={result.Length}");
                return new DecodedAudioChunk { Samples = Array.Empty<byte>(), ValidLength = 0, Pts = pts };
            }

            returnResultToPool = false;
            return new DecodedAudioChunk
            {
                Samples = result,
                ValidLength = validBytes,
                Pts = pts
            };
        }
        finally
        {
            ffmpeg.av_frame_unref(_audioFrame);
            if (returnResultToPool && result is { Length: > 0 })
            {
                ArrayPool<byte>.Shared.Return(result);
            }
        }
    }

    private static int ToBoundedAudioSampleCount(long sampleCount)
    {
        var maxSamples = MaxDecodedAudioFrameBytes / (OutputAudioChannels * sizeof(float));
        if (sampleCount <= 0)
        {
            return 0;
        }

        if (sampleCount > maxSamples)
        {
            return maxSamples;
        }

        return (int)sampleCount;
    }

    private static bool TryCalculateAudioBufferBytes(int sampleCount, out int bytes)
    {
        bytes = 0;
        if (sampleCount <= 0)
        {
            return false;
        }

        var calculated = (long)sampleCount * OutputAudioChannels * sizeof(float);
        if (calculated <= 0 || calculated > MaxDecodedAudioFrameBytes || calculated > int.MaxValue)
        {
            return false;
        }

        bytes = (int)calculated;
        return true;
    }
}

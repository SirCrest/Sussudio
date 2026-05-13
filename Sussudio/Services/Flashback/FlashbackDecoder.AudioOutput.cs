using System;
using System.Buffers;
using FFmpeg.AutoGen;
using Sussudio.Services.Audio;

namespace Sussudio.Services.Flashback;

internal sealed unsafe partial class FlashbackDecoder
{
    // --- Audio packet delivery and output conversion ---

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

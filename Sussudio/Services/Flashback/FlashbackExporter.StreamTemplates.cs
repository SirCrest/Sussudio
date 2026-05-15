using System;
using FFmpeg.AutoGen;

namespace Sussudio.Services.Flashback;

internal sealed unsafe partial class FlashbackExporter
{
    /// <summary>
    /// Copies stream templates from input to output, skipping streams with invalid codec parameters
    /// (e.g., audio with 0 channels). Returns a mapping array: streamMap[inputIndex] = outputIndex, or -1 if skipped.
    /// </summary>
    private static int[] CopyTemplateStreams(AVFormatContext* inputContext, AVFormatContext* outputContext, int inputStreamCount)
    {
        var streamMap = new int[inputStreamCount];

        for (var streamIndex = 0; streamIndex < inputStreamCount; streamIndex++)
        {
            var inStream = inputContext->streams[streamIndex];
            var codecType = inStream->codecpar->codec_type;

            // Skip audio streams with incomplete codec params (0 channels or 0 sample_rate)
            if (codecType == AVMediaType.AVMEDIA_TYPE_AUDIO &&
                (inStream->codecpar->ch_layout.nb_channels <= 0 || inStream->codecpar->sample_rate <= 0))
            {
                Logger.Log($"FLASHBACK_EXPORT_STREAM_SKIP input_index={streamIndex} reason='invalid_audio_params' channels={inStream->codecpar->ch_layout.nb_channels} sample_rate={inStream->codecpar->sample_rate}");
                streamMap[streamIndex] = -1;
                continue;
            }

            // Skip video streams with incomplete params
            if (codecType == AVMediaType.AVMEDIA_TYPE_VIDEO &&
                (inStream->codecpar->width <= 0 || inStream->codecpar->height <= 0))
            {
                Logger.Log($"FLASHBACK_EXPORT_STREAM_SKIP input_index={streamIndex} reason='invalid_video_params' width={inStream->codecpar->width} height={inStream->codecpar->height}");
                streamMap[streamIndex] = -1;
                continue;
            }

            var outStream = ffmpeg.avformat_new_stream(outputContext, null);
            if (outStream == null)
            {
                throw new InvalidOperationException("FLASHBACK_EXPORT_ERROR operation=avformat_new_stream msg='Stream allocation returned null.'");
            }

            ThrowIfError(ffmpeg.avcodec_parameters_copy(outStream->codecpar, inStream->codecpar), "avcodec_parameters_copy");
            outStream->codecpar->codec_tag = 0;
            outStream->time_base = inStream->time_base;
            outStream->avg_frame_rate = inStream->avg_frame_rate;
            outStream->sample_aspect_ratio = inStream->sample_aspect_ratio;

            streamMap[streamIndex] = outStream->index;
        }

        return streamMap;
    }

    private static string? FindSegmentStreamLayoutMismatch(
        AVFormatContext* inputContext,
        AVFormatContext* outputContext,
        int[] streamMap,
        int inputStreamCount)
    {
        if (inputContext == null || outputContext == null)
        {
            return "missing_context";
        }

        var comparableStreamCount = Math.Min(inputStreamCount, streamMap.Length);
        for (var streamIndex = 0; streamIndex < comparableStreamCount; streamIndex++)
        {
            var outputIndex = streamMap[streamIndex];
            if (outputIndex < 0)
            {
                continue;
            }

            if (outputIndex >= outputContext->nb_streams)
            {
                return $"stream={streamIndex} output_index_out_of_range output={outputIndex} output_count={outputContext->nb_streams}";
            }

            var inputStream = inputContext->streams[streamIndex];
            var outputStream = outputContext->streams[outputIndex];
            if (inputStream == null || outputStream == null || inputStream->codecpar == null || outputStream->codecpar == null)
            {
                return $"stream={streamIndex} missing_codec_params";
            }

            var inputCodec = inputStream->codecpar;
            var templateCodec = outputStream->codecpar;
            if (inputCodec->codec_type != templateCodec->codec_type)
            {
                return $"stream={streamIndex} codec_type expected={templateCodec->codec_type} actual={inputCodec->codec_type}";
            }

            if (inputCodec->codec_id != templateCodec->codec_id)
            {
                return $"stream={streamIndex} codec_id expected={templateCodec->codec_id} actual={inputCodec->codec_id}";
            }

            if (inputCodec->codec_type == AVMediaType.AVMEDIA_TYPE_VIDEO)
            {
                if (!VideoDimensionsMatchOrCanUseTemplate(inputCodec, templateCodec))
                {
                    return $"stream={streamIndex} video_size expected={templateCodec->width}x{templateCodec->height} actual={inputCodec->width}x{inputCodec->height}";
                }
            }
            else if (inputCodec->codec_type == AVMediaType.AVMEDIA_TYPE_AUDIO)
            {
                if (inputCodec->sample_rate != templateCodec->sample_rate)
                {
                    return $"stream={streamIndex} sample_rate expected={templateCodec->sample_rate} actual={inputCodec->sample_rate}";
                }

                if (inputCodec->ch_layout.nb_channels != templateCodec->ch_layout.nb_channels)
                {
                    return $"stream={streamIndex} channels expected={templateCodec->ch_layout.nb_channels} actual={inputCodec->ch_layout.nb_channels}";
                }

                if (inputCodec->format != templateCodec->format)
                {
                    return $"stream={streamIndex} sample_format expected={templateCodec->format} actual={inputCodec->format}";
                }
            }
        }

        return null;
    }

    private static bool VideoDimensionsMatchOrCanUseTemplate(AVCodecParameters* inputCodec, AVCodecParameters* templateCodec)
    {
        if (inputCodec->width == templateCodec->width && inputCodec->height == templateCodec->height)
        {
            return true;
        }

        var inputHasCompleteDimensions = inputCodec->width > 0 && inputCodec->height > 0;
        var templateHasCompleteDimensions = templateCodec->width > 0 && templateCodec->height > 0;
        return !inputHasCompleteDimensions && templateHasCompleteDimensions;
    }
}

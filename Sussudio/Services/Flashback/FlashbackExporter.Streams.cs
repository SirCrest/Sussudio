using System;
using FFmpeg.AutoGen;

namespace Sussudio.Services.Flashback;

internal sealed unsafe partial class FlashbackExporter
{
    private static int FindVideoStreamIndex(AVFormatContext* inputContext)
    {
        return ffmpeg.av_find_best_stream(inputContext, AVMediaType.AVMEDIA_TYPE_VIDEO, -1, -1, null, 0);
    }

    private static bool TryGetInputStreamCount(
        AVFormatContext* inputContext,
        string operation,
        out int streamCount,
        out string failureMessage)
    {
        streamCount = 0;
        if (inputContext == null)
        {
            failureMessage = $"Flashback export failed: input context was not available during {operation}.";
            return false;
        }

        var nativeStreamCount = inputContext->nb_streams;
        if (nativeStreamCount == 0)
        {
            failureMessage = $"Flashback export failed: input had no streams during {operation}.";
            return false;
        }

        if (nativeStreamCount > MaxSupportedInputStreams)
        {
            failureMessage = $"Flashback export failed: input stream count {nativeStreamCount} exceeds supported maximum {MaxSupportedInputStreams} during {operation}.";
            return false;
        }

        streamCount = (int)nativeStreamCount;
        failureMessage = string.Empty;
        return true;
    }

    private static void LogInputStreams(AVFormatContext* inputContext, int inputStreamCount)
    {
        for (var si = 0; si < inputStreamCount; si++)
        {
            var inStr = inputContext->streams[si];
            var codecId = inStr->codecpar->codec_id;
            var codecType = inStr->codecpar->codec_type;
            Logger.Log($"FLASHBACK_EXPORT_INPUT_STREAM idx={si} type={codecType} codec_id={codecId} " +
                $"w={inStr->codecpar->width} h={inStr->codecpar->height} " +
                $"extradata_size={inStr->codecpar->extradata_size} " +
                $"sample_rate={inStr->codecpar->sample_rate} channels={inStr->codecpar->ch_layout.nb_channels}");
        }
    }

    private static void LogTimestampBaseDrift(long[] timestampBasesUs, bool[] hasTimestampBase)
    {
        // All values are already in microseconds - find min/max to detect drift.
        long? minUs = null;
        long? maxUs = null;

        for (var i = 0; i < timestampBasesUs.Length; i++)
        {
            if (!hasTimestampBase[i])
            {
                continue;
            }

            var baseUs = timestampBasesUs[i];
            if (minUs == null || baseUs < minUs.Value) minUs = baseUs;
            if (maxUs == null || baseUs > maxUs.Value) maxUs = baseUs;
        }

        if (minUs == null || maxUs == null || minUs.Value == maxUs.Value)
        {
            return;
        }

        var driftUs = maxUs.Value - minUs.Value;
        if (driftUs > 100_000) // 100ms threshold
        {
            Logger.Log($"FLASHBACK_EXPORT_WARN reason='stream_base_drift' drift_us={driftUs}");
        }
    }

    private void OpenInput(string inputPath)
    {
        CloseActiveInput();

        AVFormatContext* inputContext = null;
        try
        {
            ThrowIfError(ffmpeg.avformat_open_input(&inputContext, inputPath, null, null), "avformat_open_input");

            // Increase probe size for TS segments that may start mid-stream.
            // H.264 TS segments from RotateOutput may not have SPS/PPS at the very start
            // (NVENC pipeline latency can push the first IDR several frames in).
            // Default probesize (5MB) may not be enough for 4K@120fps H.264 - increase
            // to 20MB so avformat_find_stream_info can find the first IDR and extract
            // video dimensions and extradata.
            inputContext->probesize = 20 * 1024 * 1024;
            inputContext->max_analyze_duration = 5 * ffmpeg.AV_TIME_BASE; // 5 seconds
        }
        catch
        {
            /* Cleanup must not throw - close partially-opened input before re-throwing */
            if (inputContext != null)
            {
                ffmpeg.avformat_close_input(&inputContext);
            }

            throw;
        }

        _activeInputContext = inputContext;
    }

    private void CreateOutputContext(string tmpPath, bool fastStart)
    {
        if (_activeOutputContext != null)
        {
            return;
        }

        AVFormatContext* outputContext = null;
        ThrowIfError(ffmpeg.avformat_alloc_output_context2(&outputContext, null, "mp4", tmpPath), "avformat_alloc_output_context2");
        if (outputContext == null)
        {
            throw new InvalidOperationException("FLASHBACK_EXPORT_ERROR operation=avformat_alloc_output_context2 msg='Output context allocation returned null.'");
        }

        _activeOutputContext = outputContext;
        _activeTempPath = tmpPath;

        if (fastStart)
        {
            Logger.Log($"FLASHBACK_EXPORT_MUX mode='faststart' path='{tmpPath}'");
        }
    }

    private static void OpenOutputIoAndWriteHeader(AVFormatContext* outputContext, string tmpPath, bool fastStart)
    {
        ThrowIfError(ffmpeg.avio_open2(&outputContext->pb, tmpPath, ffmpeg.AVIO_FLAG_WRITE, null, null), "avio_open2");

        AVDictionary* muxerOptions = null;
        try
        {
            if (fastStart)
            {
                ThrowIfError(ffmpeg.av_dict_set(&muxerOptions, "movflags", "+faststart", 0), "av_dict_set(movflags)");
            }

            ThrowIfError(ffmpeg.avformat_write_header(outputContext, &muxerOptions), "avformat_write_header");
        }
        finally
        {
            ffmpeg.av_dict_free(&muxerOptions);
        }
    }
}

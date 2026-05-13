using System;
using FFmpeg.AutoGen;
using Sussudio.Services.Recording;

namespace Sussudio.Services.Flashback;

internal sealed unsafe partial class FlashbackDecoder
{
    private static TimeSpan DecodePtsToTimeSpan(long pts, AVRational timeBase)
    {
        if (pts == ffmpeg.AV_NOPTS_VALUE || timeBase.num <= 0 || timeBase.den <= 0)
        {
            return TimeSpan.Zero;
        }

        var seconds = (double)pts * timeBase.num / timeBase.den;
        if (!double.IsFinite(seconds) || seconds <= 0 || seconds > TimeSpan.MaxValue.TotalSeconds)
        {
            return TimeSpan.Zero;
        }

        return TimeSpan.FromSeconds(seconds);
    }

    private static long ResolveBestEffortFrameTimestamp(AVFrame* frame)
    {
        if (frame == null)
        {
            return ffmpeg.AV_NOPTS_VALUE;
        }

        if (frame->best_effort_timestamp != ffmpeg.AV_NOPTS_VALUE)
        {
            return frame->best_effort_timestamp;
        }

        return frame->pts;
    }

    private static long ToAvTimeBaseTimestamp(TimeSpan value)
    {
        if (value <= TimeSpan.Zero)
        {
            return 0;
        }

        var microseconds = value.TotalMilliseconds * 1000.0;
        if (!double.IsFinite(microseconds) || microseconds >= long.MaxValue)
        {
            return long.MaxValue;
        }

        return (long)microseconds;
    }

    private static long ToStreamTimestamp(TimeSpan value, AVRational timeBase)
    {
        if (value <= TimeSpan.Zero || timeBase.num <= 0 || timeBase.den <= 0)
        {
            return 0;
        }

        var timestamp = value.TotalSeconds * timeBase.den / timeBase.num;
        if (!double.IsFinite(timestamp) || timestamp >= long.MaxValue)
        {
            return long.MaxValue;
        }

        return (long)timestamp;
    }

    private IDisposable? BeginRecoverableSeekLogSuppressionIfNeeded()
    {
        if (!_suppressRecoverableSeekLogsForNextVideoFrame)
        {
            return null;
        }

        _suppressRecoverableSeekLogsForNextVideoFrame = false;
        return LibAvEncoder.SuppressRecoverableSeekFfmpegLogs();
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Sussudio.Models;
using Sussudio.Services.Flashback;

namespace Sussudio.Services.Capture;

public partial class CaptureService
{
    private static IReadOnlyList<FlashbackExportSegment>? BuildFlashbackExportSegments(
        FlashbackBufferManager? bufferManager,
        IReadOnlyList<string>? segmentPaths)
    {
        if (segmentPaths is not { Count: > 0 })
        {
            return null;
        }

        var segmentInfo = bufferManager?.GetSegmentInfoList()
            .Where(segment => !segment.IsActive)
            .Select(segment => (Key: TryGetFullPath(segment.Path), Segment: segment))
            .Where(entry => !string.IsNullOrWhiteSpace(entry.Key))
            .GroupBy(entry => entry.Key!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First().Segment, StringComparer.OrdinalIgnoreCase);
        var segments = new List<FlashbackExportSegment>(segmentPaths.Count);
        foreach (var path in segmentPaths)
        {
            var pathKey = TryGetFullPath(path);
            if (segmentInfo != null &&
                pathKey != null &&
                segmentInfo.TryGetValue(pathKey, out var info))
            {
                var startPts = FromSegmentMilliseconds(info.StartPtsMs);
                var endPts = FromSegmentMilliseconds(info.EndPtsMs);
                if (endPts < startPts)
                {
                    endPts = startPts;
                }

                segments.Add(new FlashbackExportSegment
                {
                    Path = path,
                    StartPts = startPts,
                    EndPts = endPts
                });
            }
            else
            {
                segments.Add(new FlashbackExportSegment { Path = path });
            }
        }

        return segments;
    }

    private static Func<int>? CreateFlashbackExportThrottleDelayProvider(
        FlashbackEncoderSink? flashbackSink,
        bool throttleHighResolutionBaseline = true)
    {
        if (flashbackSink == null)
        {
            return null;
        }

        var lastLoggedTick = 0L;
        return () =>
        {
            var capacity = flashbackSink.VideoQueueCapacityFrames;
            if (capacity <= 0)
            {
                return 0;
            }

            var depth = flashbackSink.VideoQueueCount;
            var queueRatio = Math.Clamp(depth / (double)capacity, 0.0, 1.0);
            var oldestFrameAgeMs = flashbackSink.VideoQueueOldestFrameAgeMs;
            var delayMs = ResolveFlashbackExportThrottleDelayMs(
                queueRatio,
                oldestFrameAgeMs,
                throttleHighResolutionBaseline && IsHighResolutionFlashbackExport(flashbackSink));
            if (delayMs <= 0)
            {
                return 0;
            }

            var now = Environment.TickCount64;
            if (now - lastLoggedTick >= 1_000)
            {
                lastLoggedTick = now;
                Logger.Log(
                    "FLASHBACK_EXPORT_LIVE_THROTTLE " +
                    $"delay_ms={delayMs} queue={depth}/{capacity} " +
                    $"queue_ratio={queueRatio:0.00} oldest_ms={oldestFrameAgeMs}");
            }

            return delayMs;
        };
    }

    private static bool IsHighResolutionFlashbackExport(FlashbackEncoderSink flashbackSink)
        => flashbackSink.EncoderWidth >= 3840 || flashbackSink.EncoderHeight >= 2160;

    private static int ResolveFlashbackExportThrottleDelayMs(
        double queueRatio,
        long oldestFrameAgeMs,
        bool liveHighResolution = false)
    {
        if (queueRatio >= 0.85 || oldestFrameAgeMs >= 90)
        {
            return 25;
        }

        if (queueRatio >= 0.70 || oldestFrameAgeMs >= 50)
        {
            return 20;
        }

        if (liveHighResolution)
        {
            return 25;
        }

        if (queueRatio >= 0.50 || oldestFrameAgeMs >= 30)
        {
            return 16;
        }

        return 0;
    }

    private static string? TryGetFullPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        try
        {
            return Path.GetFullPath(path);
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_PATH_NORMALIZE_WARN path='{path}' type={ex.GetType().Name} msg='{ex.Message}'");
            return null;
        }
    }

    private static TimeSpan FromSegmentMilliseconds(long milliseconds)
    {
        if (milliseconds <= 0)
        {
            return TimeSpan.Zero;
        }

        return milliseconds >= TimeSpan.MaxValue.TotalMilliseconds
            ? TimeSpan.MaxValue
            : TimeSpan.FromMilliseconds(milliseconds);
    }

    private static TimeSpan ClampFlashbackBufferPosition(TimeSpan position, TimeSpan bufferedDuration)
    {
        if (bufferedDuration <= TimeSpan.Zero)
        {
            return TimeSpan.Zero;
        }

        if (position < TimeSpan.Zero)
        {
            return TimeSpan.Zero;
        }

        return position > bufferedDuration ? bufferedDuration : position;
    }

    private static TimeSpan AddFlashbackPtsOffsetOrMax(TimeSpan position, TimeSpan offset)
    {
        if (position == TimeSpan.MaxValue || offset == TimeSpan.MaxValue)
        {
            return TimeSpan.MaxValue;
        }

        if (position < TimeSpan.Zero)
        {
            position = TimeSpan.Zero;
        }

        if (offset <= TimeSpan.Zero)
        {
            return position;
        }

        return position > TimeSpan.MaxValue - offset
            ? TimeSpan.MaxValue
            : position + offset;
    }
}

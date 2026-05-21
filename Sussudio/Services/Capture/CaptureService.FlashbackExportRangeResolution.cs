using System;
using Sussudio.Services.Flashback;

namespace Sussudio.Services.Capture;

public partial class CaptureService
{
    private delegate (bool Succeeded, TimeSpan InPoint, TimeSpan OutPoint, string? FailureMessage)
        FlashbackExportRangeResolver(FlashbackBufferManager manager);

    private static FlashbackExportRangeResolver CreateFlashbackExportRangeResolver(
        TimeSpan? inPoint,
        TimeSpan? outPoint,
        TimeSpan? inPointFilePts,
        TimeSpan? outPointFilePts)
    {
        return manager => ResolveFlashbackExportRangeAfterEvictionPaused(
            manager,
            inPoint,
            outPoint,
            inPointFilePts,
            outPointFilePts);
    }

    private static FlashbackExportRangeResolver CreateFlashbackExportLastNRangeResolver(double seconds)
        => manager => ResolveFlashbackExportLastNRangeAfterEvictionPaused(manager, seconds);

    private static (bool Succeeded, TimeSpan InPoint, TimeSpan OutPoint, string? FailureMessage)
        ResolveFlashbackExportRangeAfterEvictionPaused(
            FlashbackBufferManager manager,
            TimeSpan? inPoint,
            TimeSpan? outPoint,
            TimeSpan? inPointFilePts,
            TimeSpan? outPointFilePts)
    {
        var validStart = manager.ValidStartPts;
        if (inPointFilePts.HasValue || outPointFilePts.HasValue)
        {
            var absoluteInPoint = inPointFilePts ?? validStart;
            var absoluteOutPoint = outPointFilePts ?? TimeSpan.MaxValue;
            if (absoluteInPoint < validStart)
            {
                return (false, absoluteInPoint, absoluteOutPoint, "Flashback export in point has been evicted from the buffer.");
            }

            if (absoluteOutPoint != TimeSpan.MaxValue && absoluteOutPoint <= validStart)
            {
                return (false, absoluteInPoint, absoluteOutPoint, "Flashback export out point has been evicted from the buffer.");
            }

            return absoluteOutPoint != TimeSpan.MaxValue && absoluteOutPoint <= absoluteInPoint
                ? (false, absoluteInPoint, absoluteOutPoint, "Flashback export range is empty or invalid.")
                : (true, absoluteInPoint, absoluteOutPoint, null);
        }

        var bufferedDuration = manager.BufferedDuration;
        var bufferInPoint = ClampFlashbackBufferPosition(inPoint ?? TimeSpan.Zero, bufferedDuration);
        var bufferOutPoint = outPoint.HasValue
            ? ClampFlashbackBufferPosition(outPoint.Value, bufferedDuration)
            : TimeSpan.MaxValue;
        var fileInPoint = AddFlashbackPtsOffsetOrMax(bufferInPoint, validStart);
        var fileOutPoint = AddFlashbackPtsOffsetOrMax(bufferOutPoint, validStart);
        return fileOutPoint != TimeSpan.MaxValue && fileOutPoint <= fileInPoint
            ? (false, fileInPoint, fileOutPoint, "Flashback export range is empty or invalid.")
            : (true, fileInPoint, fileOutPoint, null);
    }

    private static (bool Succeeded, TimeSpan InPoint, TimeSpan OutPoint, string? FailureMessage)
        ResolveFlashbackExportLastNRangeAfterEvictionPaused(FlashbackBufferManager manager, double seconds)
    {
        var bufferedDuration = manager.BufferedDuration;
        var validStart = manager.ValidStartPts;
        var rangeStart = bufferedDuration.TotalSeconds > seconds
            ? TimeSpan.FromSeconds(bufferedDuration.TotalSeconds - seconds)
            : TimeSpan.Zero;
        var fileInPoint = AddFlashbackPtsOffsetOrMax(rangeStart, validStart);
        return (true, fileInPoint, TimeSpan.MaxValue, null);
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

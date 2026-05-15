using System;
using Sussudio.Models;

namespace Sussudio.Services.Flashback;

internal sealed unsafe partial class FlashbackExporter
{
    private static bool SegmentOverlapsExportRange(
        FlashbackExportSegment segment,
        TimeSpan inPoint,
        TimeSpan outPoint)
    {
        if (!segment.StartPts.HasValue || !segment.EndPts.HasValue)
        {
            return true;
        }

        var segmentStart = segment.StartPts.Value;
        var segmentEnd = segment.EndPts.Value;
        if (segmentEnd < segmentStart)
        {
            segmentEnd = segmentStart;
        }

        return segmentEnd > inPoint && segmentStart < outPoint;
    }

    private static bool TryValidateExportRange(TimeSpan inPoint, TimeSpan outPoint, out string failureMessage)
    {
        if (inPoint < TimeSpan.Zero)
        {
            failureMessage = "Flashback export failed: in point must not be negative.";
            return false;
        }

        if (outPoint != TimeSpan.MaxValue && outPoint <= inPoint)
        {
            failureMessage = "Flashback export failed: export range is empty or invalid.";
            return false;
        }

        failureMessage = string.Empty;
        return true;
    }
}

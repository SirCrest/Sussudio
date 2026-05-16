using System;
using Sussudio.Models;

namespace Sussudio.Services.Flashback;

internal sealed unsafe partial class FlashbackExporter
{
    private readonly record struct SegmentExportWindow(
        bool UseSegmentTimeline,
        long SegmentInOffsetUs,
        long SegmentOutOffsetUs,
        bool SkipBecauseEmpty);

    private static SegmentExportWindow ProjectSegmentExportWindow(
        FlashbackExportSegment segment,
        TimeSpan inPoint,
        TimeSpan outPoint,
        long outPtsLimitUs)
    {
        var useSegmentTimeline = segment.StartPts.HasValue;
        if (!useSegmentTimeline)
        {
            return new SegmentExportWindow(
                UseSegmentTimeline: false,
                SegmentInOffsetUs: 0,
                SegmentOutOffsetUs: outPtsLimitUs,
                SkipBecauseEmpty: false);
        }

        var segmentInOffsetUs = ToMicrosecondsSaturated(SaturatingSubtract(inPoint, segment.StartPts!.Value));
        var segmentOutDelta = SaturatingSubtract(
            (segment.EndPts.HasValue && segment.EndPts.Value < outPoint) ? segment.EndPts.Value : outPoint,
            segment.StartPts!.Value);
        var segmentOutOffsetUs = ToMicrosecondsSaturated(segmentOutDelta);

        return new SegmentExportWindow(
            UseSegmentTimeline: true,
            SegmentInOffsetUs: segmentInOffsetUs,
            SegmentOutOffsetUs: segmentOutOffsetUs,
            SkipBecauseEmpty: segmentOutDelta <= TimeSpan.Zero);
    }
}

using System;
using Sussudio.Models;

namespace Sussudio.Services.Flashback;

internal sealed unsafe partial class FlashbackExporter
{
    private struct RequestedSegmentSkipTracker
    {
        private readonly TimeSpan _inPoint;
        private readonly TimeSpan _outPoint;
        private int _count;
        private string? _firstReason;

        public RequestedSegmentSkipTracker(TimeSpan inPoint, TimeSpan outPoint)
        {
            _inPoint = inPoint;
            _outPoint = outPoint;
            _count = 0;
            _firstReason = null;
        }

        public void Track(FlashbackExportSegment segment, string reason)
        {
            if (!SegmentOverlapsExportRange(segment, _inPoint, _outPoint))
            {
                return;
            }

            _count++;
            _firstReason ??= reason;
        }

        public bool TryCreateFailureMessage(out string message)
        {
            if (_count <= 0)
            {
                message = string.Empty;
                return false;
            }

            message = $"Flashback export failed: {_count} requested segment(s) were skipped; first reason: {_firstReason}.";
            return true;
        }
    }

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

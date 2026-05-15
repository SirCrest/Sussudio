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
}

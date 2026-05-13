using System;
using System.Collections.Generic;
using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    public IReadOnlyList<PerformanceTimelineEntry> GetPerformanceTimeline(int maxEntries = 240)
    {
        lock (_stateLock)
        {
            var count = Math.Min(_timelineCount, Math.Max(0, maxEntries));
            if (count == 0)
            {
                return Array.Empty<PerformanceTimelineEntry>();
            }

            var result = new PerformanceTimelineEntry[count];
            var oldest = (_timelineHead - _timelineCount + TimelineCapacity) % TimelineCapacity;
            var skip = _timelineCount - count;
            var readIndex = (oldest + skip) % TimelineCapacity;
            for (var i = 0; i < count; i++)
            {
                result[i] = _timelineBuffer[readIndex];
                readIndex = (readIndex + 1) % TimelineCapacity;
            }

            return result;
        }
    }

    // Caller must hold _stateLock so _latestSnapshot and timeline advance atomically.
    private void AppendPerformanceTimelineEntry(AutomationSnapshot snapshot)
    {
        _timelineBuffer[_timelineHead] = BuildPerformanceTimelineEntry(snapshot);
        _timelineHead = (_timelineHead + 1) % TimelineCapacity;
        if (_timelineCount < TimelineCapacity)
        {
            _timelineCount++;
        }
    }
}

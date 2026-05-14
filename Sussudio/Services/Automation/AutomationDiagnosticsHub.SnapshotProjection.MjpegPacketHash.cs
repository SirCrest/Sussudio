using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static MjpegPacketHashProjection BuildMjpegPacketHashProjection(CaptureHealthSnapshot health)
        => new()
        {
            SampleCount = health.MjpegPacketHashSampleCount,
            UniqueFrameCount = health.MjpegPacketHashUniqueFrameCount,
            DuplicateFrameCount = health.MjpegPacketHashDuplicateFrameCount,
            LongestDuplicateRun = health.MjpegPacketHashLongestDuplicateRun,
            InputObservedFps = health.MjpegPacketHashInputObservedFps,
            UniqueObservedFps = health.MjpegPacketHashUniqueObservedFps,
            DuplicateFramePercent = health.MjpegPacketHashDuplicateFramePercent,
            LastHash = health.MjpegPacketHashLastHash,
            LastFrameDuplicate = health.MjpegPacketHashLastFrameDuplicate,
            Pattern = health.MjpegPacketHashPattern,
            RecentInputIntervalsMs = health.MjpegPacketHashRecentInputIntervalsMs,
            RecentUniqueIntervalsMs = health.MjpegPacketHashRecentUniqueIntervalsMs,
            RecentDuplicateFlags = health.MjpegPacketHashRecentDuplicateFlags
        };

    private readonly record struct MjpegPacketHashProjection
    {
        public int SampleCount { get; init; }
        public long UniqueFrameCount { get; init; }
        public long DuplicateFrameCount { get; init; }
        public long LongestDuplicateRun { get; init; }
        public double InputObservedFps { get; init; }
        public double UniqueObservedFps { get; init; }
        public double DuplicateFramePercent { get; init; }
        public string LastHash { get; init; }
        public bool LastFrameDuplicate { get; init; }
        public string Pattern { get; init; }
        public double[] RecentInputIntervalsMs { get; init; }
        public double[] RecentUniqueIntervalsMs { get; init; }
        public int[] RecentDuplicateFlags { get; init; }
    }
}

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

    private static MjpegPacketHashFlattenedProjection BuildMjpegPacketHashFlattenedProjection(
        MjpegPacketHashProjection packetHash)
        => new()
        {
            SampleCount = packetHash.SampleCount,
            UniqueFrameCount = packetHash.UniqueFrameCount,
            DuplicateFrameCount = packetHash.DuplicateFrameCount,
            LongestDuplicateRun = packetHash.LongestDuplicateRun,
            InputObservedFps = packetHash.InputObservedFps,
            UniqueObservedFps = packetHash.UniqueObservedFps,
            DuplicateFramePercent = packetHash.DuplicateFramePercent,
            LastHash = packetHash.LastHash,
            LastFrameDuplicate = packetHash.LastFrameDuplicate,
            Pattern = packetHash.Pattern,
            RecentInputIntervalsMs = packetHash.RecentInputIntervalsMs,
            RecentUniqueIntervalsMs = packetHash.RecentUniqueIntervalsMs,
            RecentDuplicateFlags = packetHash.RecentDuplicateFlags
        };

    private readonly record struct MjpegPacketHashFlattenedProjection
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

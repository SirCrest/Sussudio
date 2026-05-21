using System;

namespace Sussudio.Models;

public sealed partial class AutomationSnapshot
{
    public int MjpegPacketHashSampleCount { get; init; }
    public long MjpegPacketHashUniqueFrameCount { get; init; }
    public long MjpegPacketHashDuplicateFrameCount { get; init; }
    public long MjpegPacketHashLongestDuplicateRun { get; init; }
    public double MjpegPacketHashInputObservedFps { get; init; }
    public double MjpegPacketHashUniqueObservedFps { get; init; }
    public double MjpegPacketHashDuplicateFramePercent { get; init; }
    public string MjpegPacketHashLastHash { get; init; } = string.Empty;
    public bool MjpegPacketHashLastFrameDuplicate { get; init; }
    public string MjpegPacketHashPattern { get; init; } = "NoSamples";
    public double[] MjpegPacketHashRecentInputIntervalsMs { get; init; } = Array.Empty<double>();
    public double[] MjpegPacketHashRecentUniqueIntervalsMs { get; init; } = Array.Empty<double>();
    public int[] MjpegPacketHashRecentDuplicateFlags { get; init; } = Array.Empty<int>();
}

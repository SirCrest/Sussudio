using System;

namespace Sussudio.Models;

public enum VideoFrameDropPolicy
{
    DropOldest,
    DropNewest
}

public sealed class RecordingPipelineOptions
{
    public int TargetVideoLatencyMs { get; set; } = 250;
    public int MinBufferedVideoFrames { get; set; } = 4;
    public int MaxBufferedVideoFrames { get; set; } = 30;
    public VideoFrameDropPolicy VideoDropPolicy { get; set; } = VideoFrameDropPolicy.DropOldest;

    public int ResolveVideoQueueCapacity(double frameRate)
    {
        var safeFrameRate = frameRate > 0 ? frameRate : 60;
        var byLatency = (int)Math.Ceiling((safeFrameRate * Math.Max(50, TargetVideoLatencyMs)) / 1000.0);
        var min = Math.Max(1, MinBufferedVideoFrames);
        var max = Math.Max(min, MaxBufferedVideoFrames);
        return Math.Clamp(byLatency, min, max);
    }
}

using System;

namespace Sussudio.Models;

// Video queue behavior knob for recording pipelines that support latency
// constrained buffering.
public enum VideoFrameDropPolicy
{
    DropOldest,
    DropNewest
}

// Desired queue/latency policy for recording sinks. Active sinks must opt into
// these values explicitly; this model alone does not change queue behavior.
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

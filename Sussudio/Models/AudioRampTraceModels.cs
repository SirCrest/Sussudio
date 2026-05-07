using System;

namespace Sussudio.Models;

// Bounded audio-transition trace returned through automation for stutter/ramp
// investigations.
public sealed class AudioRampTraceSnapshot
{
    public DateTimeOffset TimestampUtc { get; init; } = DateTimeOffset.UtcNow;
    public int SampleIntervalMs { get; init; }
    public int Capacity { get; init; }
    public int EntryCount { get; init; }
    public bool IsSamplingActive { get; init; }
    public long ActiveSessionId { get; init; }
    public string ActiveReason { get; init; } = string.Empty;
    public AudioRampTraceEntry[] Entries { get; init; } = Array.Empty<AudioRampTraceEntry>();
}

// One 10ms-ish sample of control-side volume state plus render/capture evidence.
public sealed class AudioRampTraceEntry
{
    public long Sequence { get; init; }
    public long SessionId { get; init; }
    public string Kind { get; init; } = string.Empty;
    public string Reason { get; init; } = string.Empty;
    public string Note { get; init; } = string.Empty;
    public DateTimeOffset TimestampUtc { get; init; } = DateTimeOffset.UtcNow;
    public double ElapsedMs { get; init; }
    public double PreviewVolumePercent { get; init; }
    public double TargetVolumePercent { get; init; }
    public double PlaybackTargetVolumePercent { get; init; }
    public double PlaybackCurrentVolumePercent { get; init; }
    public double PlaybackOutputPeak { get; init; }
    public double PlaybackOutputRms { get; init; }
    public long PlaybackOutputAgeMs { get; init; }
    public long PlaybackRenderCallbackCount { get; init; }
    public int PlaybackQueueDepth { get; init; }
    public bool IsAudioEnabled { get; init; }
    public bool IsAudioPreviewEnabled { get; init; }
    public bool IsAudioPreviewActive { get; init; }
    public bool AudioReaderActive { get; init; }
    public double CaptureAudioPeak { get; init; }
    public long AudioFramesArrived { get; init; }
}

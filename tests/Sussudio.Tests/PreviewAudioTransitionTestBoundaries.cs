// Passive model shapes for the complete linked PreviewAudioTransitionControllers.cs.
// These declare only the members that file reads or writes, with the same names and
// types as production. They contain no ramp policy, no trace retention policy, and
// no volume arithmetic — every decision under test stays in the linked source.
namespace Sussudio.Models
{
    public sealed class CaptureRuntimeSnapshot
    {
        public bool IsAudioPreviewActive { get; init; }
        public bool AudioReaderActive { get; init; }
        public long AudioFramesArrived { get; init; }
        public long WasapiPlaybackRenderCallbackCount { get; init; }
        public int WasapiPlaybackQueueDepth { get; init; }
        public double WasapiPlaybackTargetVolumePercent { get; init; }
        public double WasapiPlaybackCurrentVolumePercent { get; init; }
        public double WasapiPlaybackOutputPeak { get; init; }
        public double WasapiPlaybackOutputRms { get; init; }
        public long WasapiPlaybackOutputLevelLastTickMs { get; init; }
    }

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
}

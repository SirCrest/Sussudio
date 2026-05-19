using System;
using System.Threading;
using Sussudio.Models;

namespace Sussudio.ViewModels;

internal sealed class AudioRampTraceRecorderContext
{
    public required Func<CaptureRuntimeSnapshot> GetRuntimeSnapshot { get; init; }
    public required Func<double> GetPreviewVolume { get; init; }
    public required Func<bool> GetIsAudioEnabled { get; init; }
    public required Func<bool> GetIsAudioPreviewEnabled { get; init; }
    public required Func<double> GetAudioPeak { get; init; }
    public required Action<string> Log { get; init; }
}

/// <summary>
/// Bounded recorder for preview-audio ramp diagnostics.
/// </summary>
internal sealed partial class AudioRampTraceRecorder
{
    private const int AudioRampTraceCapacity = 2048;

    // Short-lived ring buffer for audible transition forensics. It captures the
    // control target and actual WASAPI render envelope so a reported pop, mute,
    // or stutter can be correlated with the playback thread rather than guessed.
    private readonly object _lock = new();
    private readonly AudioRampTraceEntry[] _buffer = new AudioRampTraceEntry[AudioRampTraceCapacity];
    private readonly AudioRampTraceRecorderContext _context;
    private CancellationTokenSource? _samplerCts;
    private int _head;
    private int _count;
    private long _sequence;
    private long _activeSessionId;
    private long _sessionStartTimestamp;
    private string _activeReason = string.Empty;
    private double _targetVolume = double.NaN;
    private bool _samplingActive;

    public AudioRampTraceRecorder(AudioRampTraceRecorderContext context)
    {
        _context = context;
    }

    public AudioRampTraceSnapshot GetSnapshot(int maxEntries = 512)
    {
        lock (_lock)
        {
            var count = Math.Min(_count, Math.Clamp(maxEntries, 0, AudioRampTraceCapacity));
            var entries = count == 0
                ? Array.Empty<AudioRampTraceEntry>()
                : new AudioRampTraceEntry[count];

            if (count > 0)
            {
                var oldest = (_head - _count + AudioRampTraceCapacity) % AudioRampTraceCapacity;
                var skip = _count - count;
                var readIndex = (oldest + skip) % AudioRampTraceCapacity;
                for (var i = 0; i < count; i++)
                {
                    entries[i] = _buffer[readIndex];
                    readIndex = (readIndex + 1) % AudioRampTraceCapacity;
                }
            }

            return new AudioRampTraceSnapshot
            {
                TimestampUtc = DateTimeOffset.UtcNow,
                SampleIntervalMs = AudioRampTraceSampleIntervalMs,
                Capacity = AudioRampTraceCapacity,
                EntryCount = _count,
                IsSamplingActive = _samplingActive,
                ActiveSessionId = _activeSessionId,
                ActiveReason = _activeReason,
                Entries = entries
            };
        }
    }

}

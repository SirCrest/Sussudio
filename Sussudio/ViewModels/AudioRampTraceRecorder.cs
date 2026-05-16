using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
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
internal sealed class AudioRampTraceRecorder
{
    private const int AudioRampTraceCapacity = 2048;
    private const int AudioRampTraceSampleIntervalMs = 10;
    private const int AudioRampTracePostCompleteSampleMs = 250;

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

    public long BeginSession(string reason, double targetVolume)
    {
        var cts = new CancellationTokenSource();
        long sessionId;
        CancellationTokenSource? previousCts;
        lock (_lock)
        {
            previousCts = _samplerCts;
            previousCts?.Cancel();
            _samplerCts = cts;
            sessionId = _activeSessionId + 1;
            _activeSessionId = sessionId;
            _sessionStartTimestamp = Stopwatch.GetTimestamp();
            _activeReason = reason;
            _targetVolume = Math.Clamp(targetVolume, 0.0, 1.0);
            _samplingActive = true;
        }

        RecordPoint("session-start", reason, targetVolume);
        _ = RunSamplerAsync(sessionId, cts);
        return sessionId;
    }

    public void CompleteSession(long sessionId, string reason)
    {
        if (sessionId <= 0)
        {
            return;
        }

        RecordPoint("session-complete", reason);
        _ = StopSamplerAfterDelayAsync(sessionId, AudioRampTracePostCompleteSampleMs);
    }

    public void RecordPoint(
        string kind,
        string? reason = null,
        double? targetVolume = null,
        string? note = null,
        long? sessionId = null)
    {
        TraceState state;
        lock (_lock)
        {
            state = new TraceState(
                sessionId ?? _activeSessionId,
                _sessionStartTimestamp,
                reason ?? _activeReason,
                targetVolume ?? _targetVolume,
                _samplingActive || !string.Equals(kind, "volume-set", StringComparison.Ordinal));
        }

        if (!state.ShouldRecord)
        {
            return;
        }

        var nowUtc = DateTimeOffset.UtcNow;
        var nowTimestamp = Stopwatch.GetTimestamp();
        var elapsedMs = state.SessionStartTimestamp > 0
            ? Stopwatch.GetElapsedTime(state.SessionStartTimestamp, nowTimestamp).TotalMilliseconds
            : 0;
        var runtime = _context.GetRuntimeSnapshot();
        var previewVolume = _context.GetPreviewVolume();
        var isAudioEnabled = _context.GetIsAudioEnabled();
        var isAudioPreviewEnabled = _context.GetIsAudioPreviewEnabled();
        var audioPeak = _context.GetAudioPeak();
        var outputAgeMs = runtime.WasapiPlaybackOutputLevelLastTickMs > 0
            ? Math.Max(0, Environment.TickCount64 - runtime.WasapiPlaybackOutputLevelLastTickMs)
            : 0;

        var entry = new AudioRampTraceEntry
        {
            Sequence = Interlocked.Increment(ref _sequence),
            SessionId = state.SessionId,
            Kind = kind,
            Reason = state.Reason,
            Note = note ?? string.Empty,
            TimestampUtc = nowUtc,
            ElapsedMs = elapsedMs,
            PreviewVolumePercent = Math.Clamp(previewVolume, 0.0, 1.0) * 100.0,
            TargetVolumePercent = double.IsNaN(state.TargetVolume) ? 0 : Math.Clamp(state.TargetVolume, 0.0, 1.0) * 100.0,
            PlaybackTargetVolumePercent = runtime.WasapiPlaybackTargetVolumePercent,
            PlaybackCurrentVolumePercent = runtime.WasapiPlaybackCurrentVolumePercent,
            PlaybackOutputPeak = runtime.WasapiPlaybackOutputPeak,
            PlaybackOutputRms = runtime.WasapiPlaybackOutputRms,
            PlaybackOutputAgeMs = outputAgeMs,
            PlaybackRenderCallbackCount = runtime.WasapiPlaybackRenderCallbackCount,
            PlaybackQueueDepth = runtime.WasapiPlaybackQueueDepth,
            IsAudioEnabled = isAudioEnabled,
            IsAudioPreviewEnabled = isAudioPreviewEnabled,
            IsAudioPreviewActive = runtime.IsAudioPreviewActive,
            AudioReaderActive = runtime.AudioReaderActive,
            CaptureAudioPeak = audioPeak,
            AudioFramesArrived = runtime.AudioFramesArrived
        };

        lock (_lock)
        {
            _buffer[_head] = entry;
            _head = (_head + 1) % AudioRampTraceCapacity;
            if (_count < AudioRampTraceCapacity)
            {
                _count++;
            }
        }
    }

    private async Task RunSamplerAsync(long sessionId, CancellationTokenSource cts)
    {
        try
        {
            while (!cts.IsCancellationRequested)
            {
                RecordPoint("sample", sessionId: sessionId);
                await Task.Delay(AudioRampTraceSampleIntervalMs, cts.Token);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when a trace session completes or is superseded.
        }
        catch (Exception ex)
        {
            _context.Log($"AUDIO_RAMP_TRACE_SAMPLER_FAIL type={ex.GetType().Name} msg='{ex.Message}'");
        }
        finally
        {
            cts.Dispose();
        }
    }

    private async Task StopSamplerAfterDelayAsync(long sessionId, int delayMs)
    {
        try
        {
            await Task.Delay(delayMs);
        }
        catch
        {
            return;
        }

        CancellationTokenSource? cts;
        lock (_lock)
        {
            if (_activeSessionId != sessionId)
            {
                return;
            }

            cts = _samplerCts;
            _samplerCts = null;
            _samplingActive = false;
        }

        cts?.Cancel();
    }

    private readonly record struct TraceState(
        long SessionId,
        long SessionStartTimestamp,
        string Reason,
        double TargetVolume,
        bool ShouldRecord);
}

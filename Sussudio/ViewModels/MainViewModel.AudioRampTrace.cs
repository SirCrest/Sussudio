using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;

namespace Sussudio.ViewModels;

/// <summary>
/// Audio ramp trace ring buffer and sampler used for preview-volume diagnostics.
/// </summary>
public partial class MainViewModel
{
    private const int AudioRampTraceCapacity = 2048;
    private const int AudioRampTraceSampleIntervalMs = 10;
    private const int AudioRampTracePostCompleteSampleMs = 250;

    // Short-lived ring buffer for audible transition forensics. It captures the
    // control target and actual WASAPI render envelope so a reported pop, mute,
    // or stutter can be correlated with the playback thread rather than guessed.
    private readonly object _audioRampTraceLock = new();
    private readonly AudioRampTraceEntry[] _audioRampTraceBuffer = new AudioRampTraceEntry[AudioRampTraceCapacity];
    private CancellationTokenSource? _audioRampTraceSamplerCts;
    private int _audioRampTraceHead;
    private int _audioRampTraceCount;
    private long _audioRampTraceSequence;
    private long _audioRampTraceActiveSessionId;
    private long _audioRampTraceSessionStartTimestamp;
    private string _audioRampTraceActiveReason = string.Empty;
    private double _audioRampTraceTargetVolume = double.NaN;
    private bool _audioRampTraceSamplingActive;

    public AudioRampTraceSnapshot GetAudioRampTraceSnapshot(int maxEntries = 512)
    {
        lock (_audioRampTraceLock)
        {
            var count = Math.Min(_audioRampTraceCount, Math.Clamp(maxEntries, 0, AudioRampTraceCapacity));
            var entries = count == 0
                ? Array.Empty<AudioRampTraceEntry>()
                : new AudioRampTraceEntry[count];

            if (count > 0)
            {
                var oldest = (_audioRampTraceHead - _audioRampTraceCount + AudioRampTraceCapacity) % AudioRampTraceCapacity;
                var skip = _audioRampTraceCount - count;
                var readIndex = (oldest + skip) % AudioRampTraceCapacity;
                for (var i = 0; i < count; i++)
                {
                    entries[i] = _audioRampTraceBuffer[readIndex];
                    readIndex = (readIndex + 1) % AudioRampTraceCapacity;
                }
            }

            return new AudioRampTraceSnapshot
            {
                TimestampUtc = DateTimeOffset.UtcNow,
                SampleIntervalMs = AudioRampTraceSampleIntervalMs,
                Capacity = AudioRampTraceCapacity,
                EntryCount = _audioRampTraceCount,
                IsSamplingActive = _audioRampTraceSamplingActive,
                ActiveSessionId = _audioRampTraceActiveSessionId,
                ActiveReason = _audioRampTraceActiveReason,
                Entries = entries
            };
        }
    }

    public Task<AudioRampTraceSnapshot> GetAudioRampTraceSnapshotAsync(
        int maxEntries = 512,
        CancellationToken cancellationToken = default)
        => FromSynchronousSnapshot(() => GetAudioRampTraceSnapshot(maxEntries), cancellationToken);

    private long BeginAudioRampTraceSession(string reason, double targetVolume)
    {
        CancellationTokenSource? previousCts = null;
        var cts = new CancellationTokenSource();
        long sessionId;
        lock (_audioRampTraceLock)
        {
            previousCts = _audioRampTraceSamplerCts;
            previousCts?.Cancel();
            _audioRampTraceSamplerCts = cts;
            sessionId = _audioRampTraceActiveSessionId + 1;
            _audioRampTraceActiveSessionId = sessionId;
            _audioRampTraceSessionStartTimestamp = Stopwatch.GetTimestamp();
            _audioRampTraceActiveReason = reason;
            _audioRampTraceTargetVolume = Math.Clamp(targetVolume, 0.0, 1.0);
            _audioRampTraceSamplingActive = true;
        }

        RecordAudioRampTracePoint("session-start", reason, targetVolume);
        _ = RunAudioRampTraceSamplerAsync(sessionId, cts);
        return sessionId;
    }

    private void CompleteAudioRampTraceSession(long sessionId, string reason)
    {
        if (sessionId <= 0)
        {
            return;
        }

        RecordAudioRampTracePoint("session-complete", reason);
        _ = StopAudioRampTraceSamplerAfterDelayAsync(sessionId, AudioRampTracePostCompleteSampleMs);
    }

    private async Task RunAudioRampTraceSamplerAsync(long sessionId, CancellationTokenSource cts)
    {
        try
        {
            while (!cts.IsCancellationRequested)
            {
                RecordAudioRampTracePoint("sample", sessionId: sessionId);
                await Task.Delay(AudioRampTraceSampleIntervalMs, cts.Token);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when a trace session completes or is superseded.
        }
        catch (Exception ex)
        {
            Logger.Log($"AUDIO_RAMP_TRACE_SAMPLER_FAIL type={ex.GetType().Name} msg='{ex.Message}'");
        }
        finally
        {
            cts.Dispose();
        }
    }

    private async Task StopAudioRampTraceSamplerAfterDelayAsync(long sessionId, int delayMs)
    {
        try
        {
            await Task.Delay(delayMs);
        }
        catch
        {
            return;
        }

        CancellationTokenSource? cts = null;
        lock (_audioRampTraceLock)
        {
            if (_audioRampTraceActiveSessionId != sessionId)
            {
                return;
            }

            cts = _audioRampTraceSamplerCts;
            _audioRampTraceSamplerCts = null;
            _audioRampTraceSamplingActive = false;
        }

        cts?.Cancel();
    }

    private void RecordAudioRampTracePoint(
        string kind,
        string? reason = null,
        double? targetVolume = null,
        string? note = null,
        long? sessionId = null)
    {
        long activeSessionId;
        long sessionStartTimestamp;
        string activeReason;
        double activeTargetVolume;
        bool shouldRecord;
        lock (_audioRampTraceLock)
        {
            activeSessionId = sessionId ?? _audioRampTraceActiveSessionId;
            sessionStartTimestamp = _audioRampTraceSessionStartTimestamp;
            activeReason = reason ?? _audioRampTraceActiveReason;
            activeTargetVolume = targetVolume ?? _audioRampTraceTargetVolume;
            shouldRecord = _audioRampTraceSamplingActive || !string.Equals(kind, "volume-set", StringComparison.Ordinal);
        }

        if (!shouldRecord)
        {
            return;
        }

        var nowUtc = DateTimeOffset.UtcNow;
        var nowTimestamp = Stopwatch.GetTimestamp();
        var elapsedMs = sessionStartTimestamp > 0
            ? Stopwatch.GetElapsedTime(sessionStartTimestamp, nowTimestamp).TotalMilliseconds
            : 0;
        var runtime = _captureService.GetRuntimeSnapshot();
        var outputAgeMs = runtime.WasapiPlaybackOutputLevelLastTickMs > 0
            ? Math.Max(0, Environment.TickCount64 - runtime.WasapiPlaybackOutputLevelLastTickMs)
            : 0;

        var entry = new AudioRampTraceEntry
        {
            Sequence = Interlocked.Increment(ref _audioRampTraceSequence),
            SessionId = activeSessionId,
            Kind = kind,
            Reason = activeReason,
            Note = note ?? string.Empty,
            TimestampUtc = nowUtc,
            ElapsedMs = elapsedMs,
            PreviewVolumePercent = Math.Clamp(PreviewVolume, 0.0, 1.0) * 100.0,
            TargetVolumePercent = double.IsNaN(activeTargetVolume) ? 0 : Math.Clamp(activeTargetVolume, 0.0, 1.0) * 100.0,
            PlaybackTargetVolumePercent = runtime.WasapiPlaybackTargetVolumePercent,
            PlaybackCurrentVolumePercent = runtime.WasapiPlaybackCurrentVolumePercent,
            PlaybackOutputPeak = runtime.WasapiPlaybackOutputPeak,
            PlaybackOutputRms = runtime.WasapiPlaybackOutputRms,
            PlaybackOutputAgeMs = outputAgeMs,
            PlaybackRenderCallbackCount = runtime.WasapiPlaybackRenderCallbackCount,
            PlaybackQueueDepth = runtime.WasapiPlaybackQueueDepth,
            IsAudioEnabled = IsAudioEnabled,
            IsAudioPreviewEnabled = IsAudioPreviewEnabled,
            IsAudioPreviewActive = runtime.IsAudioPreviewActive,
            AudioReaderActive = runtime.AudioReaderActive,
            CaptureAudioPeak = AudioPeak,
            AudioFramesArrived = runtime.AudioFramesArrived
        };

        lock (_audioRampTraceLock)
        {
            _audioRampTraceBuffer[_audioRampTraceHead] = entry;
            _audioRampTraceHead = (_audioRampTraceHead + 1) % AudioRampTraceCapacity;
            if (_audioRampTraceCount < AudioRampTraceCapacity)
            {
                _audioRampTraceCount++;
            }
        }
    }
}

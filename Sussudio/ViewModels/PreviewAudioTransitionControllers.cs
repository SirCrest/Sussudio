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

internal sealed class PreviewAudioVolumeTransitionControllerContext
{
    public required Func<double> GetPreviewVolume { get; init; }
    public required Action<double> SetPreviewVolume { get; init; }
    public required Action<float> SetSessionPreviewVolume { get; init; }
    public required Func<string, double, long> BeginTraceSession { get; init; }
    public required Action<long, string> CompleteTraceSession { get; init; }
    public required Action<string, string?, double?, string?, long?> RecordTracePoint { get; init; }
    public required Action<string, string> Log { get; init; }
}

internal sealed class PreviewAudioVolumeTransitionController
{
    private const int RampDownSteps = 18;
    private const int RampDownDelayMs = 25;
    private const int RampUpSteps = 30;
    private const int RampUpDelayMs = 30;

    private readonly PreviewAudioVolumeTransitionControllerContext _context;

    public PreviewAudioVolumeTransitionController(PreviewAudioVolumeTransitionControllerContext context)
    {
        _context = context;
    }

    public bool SuppressVolumeSave { get; set; }

    public double? VolumeSaveOverride { get; set; }

    public double PersistedVolumeTarget => Math.Clamp(VolumeSaveOverride ?? _context.GetPreviewVolume(), 0.0, 1.0);

    public void HandlePreviewVolumeChanged(double value)
    {
        if (!SuppressVolumeSave)
        {
            VolumeSaveOverride = null;
        }

        _context.SetSessionPreviewVolume((float)Math.Clamp(value, 0.0, 1.0));
        RecordTracePoint("volume-set");
    }

    public double PrimeForAudioTransition(string reason)
    {
        var volumeTarget = PersistedVolumeTarget;
        if (volumeTarget <= 0.001)
        {
            _context.SetPreviewVolume(0);
            VolumeSaveOverride = null;
            return 0;
        }

        VolumeSaveOverride = volumeTarget;
        SuppressVolumeSave = true;
        try
        {
            _context.SetPreviewVolume(0);
        }
        finally
        {
            SuppressVolumeSave = false;
        }

        _context.Log(
            $"PREVIEW_AUDIO_MONITOR_PRIMED reason={reason} targetPct={volumeTarget * 100:0}",
            "PrimePreviewVolumeForAudioTransition");
        RecordTracePoint("primed", reason, volumeTarget);
        return volumeTarget;
    }

    public void RestoreAfterUnavailableAudio(double volumeTarget, string reason)
    {
        volumeTarget = Math.Clamp(volumeTarget, 0.0, 1.0);
        SuppressVolumeSave = true;
        try
        {
            _context.SetPreviewVolume(volumeTarget);
        }
        finally
        {
            SuppressVolumeSave = false;
            VolumeSaveOverride = null;
        }

        _context.Log(
            $"PREVIEW_AUDIO_MONITOR_RESTORE reason={reason} targetPct={volumeTarget * 100:0}",
            "RestorePreviewVolumeAfterUnavailableAudio");
        RecordTracePoint("restore", reason, volumeTarget, "audio-preview-unavailable");
    }

    private long BeginTraceSession(string reason, double targetVolume)
        => _context.BeginTraceSession(reason, targetVolume);

    private void CompleteTraceSession(long sessionId, string reason)
        => _context.CompleteTraceSession(sessionId, reason);

    private void RecordTracePoint(
        string kind,
        string? reason = null,
        double? targetVolume = null,
        string? note = null,
        long? sessionId = null)
        => _context.RecordTracePoint(kind, reason, targetVolume, note, sessionId);

    public Task RampDownForStopAsync(CancellationToken cancellationToken)
        => RampDownForAudioTransitionAsync("preview_stop", cancellationToken);

    public async Task RampDownForAudioTransitionAsync(
        string reason,
        CancellationToken cancellationToken = default,
        bool traceSession = true)
    {
        var persistedVolume = PersistedVolumeTarget;
        var startingVolume = Math.Clamp(_context.GetPreviewVolume(), 0.0, 1.0);
        var traceSessionId = traceSession ? BeginTraceSession(reason, targetVolume: 0) : 0;
        if (persistedVolume > 0.001)
        {
            VolumeSaveOverride = persistedVolume;
        }

        try
        {
            if (startingVolume <= 0.001)
            {
                SuppressVolumeSave = true;
                try
                {
                    _context.SetPreviewVolume(0);
                }
                finally
                {
                    SuppressVolumeSave = false;
                }

                RecordTracePoint("ramp-down-skipped", reason, targetVolume: 0, note: "already-zero");
                return;
            }

            SuppressVolumeSave = true;
            var startLog = string.Equals(reason, "preview_stop", StringComparison.Ordinal)
                ? $"PREVIEW_AUDIO_STOP_RAMP_STARTED fromPct={startingVolume * 100:0}"
                : $"PREVIEW_AUDIO_RAMP_DOWN_STARTED reason={reason} fromPct={startingVolume * 100:0}";
            _context.Log(startLog, "RampPreviewVolumeDownForAudioTransitionAsync");
            RecordTracePoint("ramp-down-start", reason, targetVolume: 0);
            try
            {
                for (var step = 1; step <= RampDownSteps; step++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var t = step / (double)RampDownSteps;
                    var eased = Math.Pow(1.0 - t, 2.0);
                    _context.SetPreviewVolume(startingVolume * eased);
                    await Task.Delay(RampDownDelayMs, cancellationToken);
                }

                _context.SetPreviewVolume(0);
                RecordTracePoint("ramp-down-complete", reason, targetVolume: 0);
                _context.Log(
                    string.Equals(reason, "preview_stop", StringComparison.Ordinal)
                        ? "PREVIEW_AUDIO_STOP_RAMP_COMPLETED"
                        : $"PREVIEW_AUDIO_RAMP_DOWN_COMPLETED reason={reason}",
                    "RampPreviewVolumeDownForAudioTransitionAsync");
            }
            finally
            {
                SuppressVolumeSave = false;
            }
        }
        finally
        {
            if (traceSession)
            {
                CompleteTraceSession(traceSessionId, reason);
            }
        }
    }

    public async Task RampUpForAudioTransitionAsync(
        double volumeTarget,
        string reason,
        CancellationToken cancellationToken = default,
        bool traceSession = true)
    {
        volumeTarget = Math.Clamp(volumeTarget, 0.0, 1.0);
        var traceSessionId = traceSession ? BeginTraceSession(reason, volumeTarget) : 0;
        if (volumeTarget <= 0.001)
        {
            try
            {
                _context.SetPreviewVolume(0);
                VolumeSaveOverride = null;
                RecordTracePoint("ramp-up-skipped", reason, volumeTarget, "target-zero");
            }
            finally
            {
                if (traceSession)
                {
                    CompleteTraceSession(traceSessionId, reason);
                }
            }

            return;
        }

        VolumeSaveOverride = volumeTarget;
        SuppressVolumeSave = true;
        _context.Log(
            $"PREVIEW_AUDIO_RAMP_UP_STARTED reason={reason} targetPct={volumeTarget * 100:0}",
            "RampPreviewVolumeUpForAudioTransitionAsync");
        RecordTracePoint("ramp-up-start", reason, volumeTarget);
        try
        {
            for (var step = 1; step <= RampUpSteps; step++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var t = step / (double)RampUpSteps;
                var eased = 1.0 - Math.Pow(1.0 - t, 3.0);
                _context.SetPreviewVolume(volumeTarget * eased);
                await Task.Delay(RampUpDelayMs, cancellationToken);
            }

            _context.SetPreviewVolume(volumeTarget);
            RecordTracePoint("ramp-up-complete", reason, volumeTarget);
            _context.Log(
                $"PREVIEW_AUDIO_RAMP_UP_COMPLETED reason={reason}",
                "RampPreviewVolumeUpForAudioTransitionAsync");
        }
        finally
        {
            SuppressVolumeSave = false;
            VolumeSaveOverride = null;
            if (traceSession)
            {
                CompleteTraceSession(traceSessionId, reason);
            }
        }
    }
}

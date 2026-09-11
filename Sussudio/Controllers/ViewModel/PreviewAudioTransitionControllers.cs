using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;

namespace Sussudio.Controllers;

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
internal sealed class AudioRampTraceRecorder : IDisposable
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
    private bool _disposed;

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
        CancellationTokenSource cts;
        long sessionId;
        lock (_lock)
        {
            if (_disposed)
            {
                return 0;
            }

            var previousCts = _samplerCts;
            cts = new CancellationTokenSource();
            _samplerCts = cts;
            sessionId = _activeSessionId + 1;
            _activeSessionId = sessionId;
            _sessionStartTimestamp = Stopwatch.GetTimestamp();
            _activeReason = reason;
            _targetVolume = Math.Clamp(targetVolume, 0.0, 1.0);
            _samplingActive = true;
            previousCts?.Cancel();
        }

        try
        {
            RecordPoint("session-start", reason, targetVolume);
        }
        catch
        {
            RetireSampler(cts);
            throw;
        }

        _ = RunSamplerAsync(sessionId, cts);
        return sessionId;
    }

    public void CompleteSession(long sessionId, string reason)
    {
        if (sessionId <= 0)
        {
            return;
        }

        try
        {
            RecordPoint("session-complete", reason, sessionId: sessionId);
        }
        finally
        {
            _ = StopSamplerAfterDelayAsync(sessionId, AudioRampTracePostCompleteSampleMs);
        }
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
            if (_disposed) return;
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
            if (_disposed) return;
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
            RetireSampler(cts);
        }
    }

    private void RetireSampler(CancellationTokenSource cts)
    {
        lock (_lock)
        {
            if (ReferenceEquals(_samplerCts, cts))
            {
                _samplerCts = null;
                _samplingActive = false;
            }

            // Cancellation shares this lock, so no caller can cancel a disposed source.
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

        lock (_lock)
        {
            if (_disposed || _activeSessionId != sessionId)
            {
                return;
            }

            var cts = _samplerCts;
            _samplerCts = null;
            _samplingActive = false;
            cts?.Cancel();
        }
    }

    public void Dispose()
    {
        lock (_lock)
        {
            if (_disposed) return;
            _disposed = true;
            _samplingActive = false;
            var cts = _samplerCts;
            _samplerCts = null;
            cts?.Cancel();
        }
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
    public Func<int, CancellationToken, Task> DelayAsync { get; init; } = Task.Delay;
}

// The operation spans prime, backend startup/reconfiguration, and restoration.
// Individual interpolation writers are revoked when the user chooses a level.
internal readonly record struct PreviewAudioVolumeOperation(long Generation);

internal readonly record struct PreviewAudioVolumeWriter(
    long OperationGeneration,
    long WriterGeneration,
    long UserRevision,
    double StartingVolume,
    double TargetVolume,
    bool MutesOutput,
    CancellationToken CancellationToken);

internal sealed class PreviewAudioVolumeTransitionController : IDisposable
{
    private const int RampDownSteps = 18;
    private const int RampDownDelayMs = 25;
    private const int RampUpSteps = 30;
    private const int RampUpDelayMs = 30;

    private readonly PreviewAudioVolumeTransitionControllerContext _context;
    // Normal mutations are UI-affine. This short boundary also prevents the
    // background disposal fallback from overtaking an admitted session write.
    private readonly object _sync = new();
    private CancellationTokenSource? _operationCancellation;
    private CancellationTokenSource? _writerCancellation;
    private long _operationGeneration;
    private long _writerGeneration;
    private long _userRevision;
    private double _requestedVolume;
    private double _effectiveVolume;
    private bool _holdingMuted;
    private bool _publishingEffectiveVolume;
    private bool _disposed;

    public PreviewAudioVolumeTransitionController(PreviewAudioVolumeTransitionControllerContext context)
    {
        _context = context;
        _requestedVolume = _effectiveVolume = Math.Clamp(context.GetPreviewVolume(), 0.0, 1.0);
    }

    public double RequestedVolume
    {
        get { lock (_sync) { return _requestedVolume; } }
    }

    public double EffectiveVolume
    {
        get { lock (_sync) { return _effectiveVolume; } }
    }

    // Settings loading still assigns the observable property directly. Internal
    // publication is guarded only for its synchronous property notification.
    public void HandlePreviewVolumeChanged(double value)
    {
        lock (_sync)
        {
            if (_publishingEffectiveVolume || _disposed) return;
        }
        SetUserVolume(value);
    }

    public void SetUserVolume(double value)
    {
        CancellationTokenSource? previousWriter;
        lock (_sync)
        {
            if (_disposed) return;
            _requestedVolume = Math.Clamp(value, 0.0, 1.0);
            _userRevision++;
            previousWriter = RevokeWriterCore();
            PublishEffectiveVolumeCore(_holdingMuted ? 0 : _requestedVolume);
        }
        CancelAndDispose(previousWriter);
        RecordTracePoint("volume-set", targetVolume: RequestedVolume, note: "user-request");
    }

    public PreviewAudioVolumeOperation BeginTransition(string reason, bool primeMuted = false)
    {
        CancellationTokenSource? previousOperation;
        CancellationTokenSource? previousWriter;
        PreviewAudioVolumeOperation operation;
        lock (_sync)
        {
            if (_disposed) return default;
            previousOperation = _operationCancellation;
            previousWriter = RevokeWriterCore();
            _operationCancellation = new CancellationTokenSource();
            operation = new PreviewAudioVolumeOperation(++_operationGeneration);
            _holdingMuted = primeMuted;
            if (primeMuted) PublishEffectiveVolumeCore(0);
        }
        CancelAndDispose(previousWriter);
        CancelAndDispose(previousOperation);
        if (primeMuted)
        {
            _context.Log(
                $"PREVIEW_AUDIO_MONITOR_PRIMED reason={reason} targetPct={RequestedVolume * 100:0}",
                "PrimePreviewVolumeForAudioTransition");
            RecordTracePoint("primed", reason, RequestedVolume);
        }
        return operation;
    }

    public PreviewAudioVolumeOperation PrimeForAudioTransition(string reason)
        => BeginTransition(reason, primeMuted: true);

    public PreviewAudioVolumeWriter? BeginWriter(
        PreviewAudioVolumeOperation operation,
        bool muteOutput,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        CancellationTokenSource? previousWriter;
        PreviewAudioVolumeWriter writer;
        lock (_sync)
        {
            if (!IsCurrentOperationCore(operation)) return null;
            previousWriter = RevokeWriterCore();
            _holdingMuted = muteOutput;
            _writerCancellation = CancellationTokenSource.CreateLinkedTokenSource(
                _operationCancellation!.Token, cancellationToken);
            writer = new PreviewAudioVolumeWriter(
                operation.Generation, _writerGeneration, _userRevision,
                _effectiveVolume, muteOutput ? 0 : _requestedVolume,
                muteOutput, _writerCancellation.Token);
        }
        CancelAndDispose(previousWriter);
        return writer;
    }

    public bool TryApplyTransient(PreviewAudioVolumeWriter writer, double value)
    {
        lock (_sync)
        {
            if (!IsCurrentWriterCore(writer)) return false;
            PublishEffectiveVolumeCore(value);
            return true;
        }
    }

    public bool TryCompleteWriter(PreviewAudioVolumeWriter writer)
    {
        CancellationTokenSource? completedWriter;
        lock (_sync)
        {
            if (!IsCurrentWriterCore(writer)) return false;
            PublishEffectiveVolumeCore(writer.TargetVolume);
            completedWriter = RevokeWriterCore();
        }
        // Completion invalidates late frames but need not signal cancellation.
        completedWriter?.Dispose();
        return true;
    }

    public void EndWriter(PreviewAudioVolumeWriter writer)
    {
        CancellationTokenSource? previousWriter;
        lock (_sync)
        {
            if (writer.OperationGeneration != _operationGeneration ||
                writer.WriterGeneration != _writerGeneration) return;
            previousWriter = RevokeWriterCore();
        }
        CancelAndDispose(previousWriter);
    }

    public void RestoreAfterUnavailableAudio(PreviewAudioVolumeOperation operation, string reason)
    {
        CancellationTokenSource? previousWriter;
        lock (_sync)
        {
            if (!IsCurrentOperationCore(operation)) return;
            previousWriter = RevokeWriterCore();
            _holdingMuted = false;
            PublishEffectiveVolumeCore(_requestedVolume);
        }
        CancelAndDispose(previousWriter);
        _context.Log(
            $"PREVIEW_AUDIO_MONITOR_RESTORE reason={reason} targetPct={RequestedVolume * 100:0}",
            "RestorePreviewVolumeAfterUnavailableAudio");
        RecordTracePoint("restore", reason, RequestedVolume, "audio-preview-unavailable");
    }

    public async Task<PreviewAudioVolumeOperation> RampDownForStopAsync(CancellationToken cancellationToken)
    {
        var operation = BeginTransition("preview_stop");
        try
        {
            await RampDownForAudioTransitionAsync(operation, "preview_stop", cancellationToken);
            return operation;
        }
        catch
        {
            RestoreAfterUnavailableAudio(operation, "preview_stop_failed");
            throw;
        }
    }

    public Task RampDownForAudioTransitionAsync(
        PreviewAudioVolumeOperation operation,
        string reason,
        CancellationToken cancellationToken = default,
        bool traceSession = true)
        => RunRampAsync(operation, reason, muteOutput: true, cancellationToken, traceSession);

    public Task RampUpForAudioTransitionAsync(
        PreviewAudioVolumeOperation operation,
        string reason,
        CancellationToken cancellationToken = default,
        bool traceSession = true)
        => RunRampAsync(operation, reason, muteOutput: false, cancellationToken, traceSession);

    private async Task RunRampAsync(
        PreviewAudioVolumeOperation operation,
        string reason,
        bool muteOutput,
        CancellationToken cancellationToken,
        bool traceSession)
    {
        var candidate = BeginWriter(operation, muteOutput, cancellationToken);
        if (candidate is not { } writer) return;
        var direction = muteOutput ? "down" : "up";
        var traceSessionId = traceSession ? _context.BeginTraceSession(reason, writer.TargetVolume) : 0;
        try
        {
            if (Math.Abs(writer.StartingVolume - writer.TargetVolume) <= 0.001)
            {
                TryCompleteWriter(writer);
                RecordTracePoint($"ramp-{direction}-skipped", reason, writer.TargetVolume,
                    muteOutput ? "already-zero" : "target-zero");
                return;
            }

            _context.Log(
                muteOutput && reason == "preview_stop"
                    ? $"PREVIEW_AUDIO_STOP_RAMP_STARTED fromPct={writer.StartingVolume * 100:0}"
                    : $"PREVIEW_AUDIO_RAMP_{direction.ToUpperInvariant()}_STARTED reason={reason} targetPct={writer.TargetVolume * 100:0}",
                "RunPreviewAudioVolumeRampAsync");
            RecordTracePoint($"ramp-{direction}-start", reason, writer.TargetVolume);
            var steps = muteOutput ? RampDownSteps : RampUpSteps;
            var delayMs = muteOutput ? RampDownDelayMs : RampUpDelayMs;
            for (var step = 1; step <= steps; step++)
            {
                writer.CancellationToken.ThrowIfCancellationRequested();
                var t = step / (double)steps;
                var value = muteOutput
                    ? writer.StartingVolume * Math.Pow(1.0 - t, 2.0)
                    : writer.StartingVolume + (writer.TargetVolume - writer.StartingVolume) * (1.0 - Math.Pow(1.0 - t, 3.0));
                if (!TryApplyTransient(writer, value)) return;
                await _context.DelayAsync(delayMs, writer.CancellationToken);
            }

            cancellationToken.ThrowIfCancellationRequested();
            if (TryCompleteWriter(writer))
            {
                RecordTracePoint($"ramp-{direction}-complete", reason, writer.TargetVolume);
                _context.Log(
                    muteOutput && reason == "preview_stop"
                        ? "PREVIEW_AUDIO_STOP_RAMP_COMPLETED"
                        : $"PREVIEW_AUDIO_RAMP_{direction.ToUpperInvariant()}_COMPLETED reason={reason}",
                    "RunPreviewAudioVolumeRampAsync");
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested && writer.CancellationToken.IsCancellationRequested)
        {
            // User supersession ends interpolation; the requested backend mute,
            // stop or input switch must still be allowed to finish.
        }
        finally
        {
            EndWriter(writer);
            if (traceSession) _context.CompleteTraceSession(traceSessionId, reason);
        }
    }

    public void Dispose()
    {
        CancellationTokenSource? operation;
        CancellationTokenSource? writer;
        lock (_sync)
        {
            if (_disposed) return;
            _disposed = true;
            _operationGeneration++;
            operation = _operationCancellation;
            _operationCancellation = null;
            writer = RevokeWriterCore();
        }
        CancelAndDispose(writer);
        CancelAndDispose(operation);
    }

    private bool IsCurrentOperationCore(PreviewAudioVolumeOperation operation)
        => !_disposed && operation.Generation != 0 && operation.Generation == _operationGeneration;

    private bool IsCurrentWriterCore(PreviewAudioVolumeWriter writer)
        => !_disposed && _writerCancellation != null &&
           writer.OperationGeneration == _operationGeneration &&
           writer.WriterGeneration == _writerGeneration &&
           writer.UserRevision == _userRevision && !writer.CancellationToken.IsCancellationRequested;

    private CancellationTokenSource? RevokeWriterCore()
    {
        var previous = _writerCancellation;
        _writerCancellation = null;
        _writerGeneration++;
        return previous;
    }

    private void PublishEffectiveVolumeCore(double value)
    {
        _effectiveVolume = Math.Clamp(value, 0.0, 1.0);
        _publishingEffectiveVolume = true;
        try
        {
            _context.SetPreviewVolume(_effectiveVolume);
            _context.SetSessionPreviewVolume((float)_effectiveVolume);
        }
        finally
        {
            _publishingEffectiveVolume = false;
        }
        RecordTracePoint("volume-set");
    }

    private static void CancelAndDispose(CancellationTokenSource? cancellation)
    {
        if (cancellation == null) return;
        try { cancellation.Cancel(); }
        finally { cancellation.Dispose(); }
    }

    private void RecordTracePoint(
        string kind,
        string? reason = null,
        double? targetVolume = null,
        string? note = null,
        long? sessionId = null)
        => _context.RecordTracePoint(kind, reason, targetVolume, note, sessionId);
}

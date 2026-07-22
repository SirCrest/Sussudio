using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.Services.Audio;
using Sussudio.Services.Contracts;
using Sussudio.Services.Preview;
using Sussudio.Services.Runtime;

namespace Sussudio.Services.Flashback;

/// <summary>
/// Presentation-layer state machine for the Flashback timeline. It chooses
/// whether preview/audio should show live capture or decoded file playback, but
/// it never starts, stops, or throttles the capture pipeline.
/// </summary>
internal sealed partial class FlashbackPlaybackController : IDisposable
{
    // --- Dependencies ---
    private readonly FlashbackBufferManager _bufferManager;

    // --- Lifecycle ---
    private IPreviewFrameSink? _previewSink;
    private ILiveVideoSource? _videoCapture;
    private volatile WasapiAudioPlayback? _audioPlayback;
    private volatile WasapiAudioCapture? _audioCapture;
    private volatile bool _initialized;
    private volatile int _disposedFlag;

    // --- Preview detach timeout and deferred reattach recovery ---
    private int _previewDetachStopTimeoutActive;
    private int _deferredPreviewAttachApplyRetryScheduled;
    private IPreviewFrameSink? _pendingPreviewSinkAfterDetachTimeout;
    private ILiveVideoSource? _pendingVideoCaptureAfterDetachTimeout;

    // --- State (read from UI thread, written primarily from playback thread) ---
    private volatile FlashbackPlaybackState _state = FlashbackPlaybackState.Live;
    private long _playbackPositionTicks;
    private volatile string _decoderHwAccel = "N/A";

    // --- A/V sync tracking (ffplay-style audio-master clock) ---
    private long _lastAudioPtsTicks;  // PTS of last audio chunk delivered to WASAPI
    private long _lastVideoPtsTicks;  // PTS of last video frame displayed

    // --- Scrub state restoration (M16 fix) ---
    private bool _wasPlayingBeforeScrub;

    private enum CommandKind
    {
        Seek,
        BeginScrub,
        UpdateScrub,
        EndScrub,
        Play,
        Pause,
        GoLive,
        Nudge,
        Stop,
        // Not a real playback command -- only used to attribute PreWarm's
        // EnsurePlaybackThread call in diagnostics/failure logging. Never
        // enqueued onto the command channel.
        Warm
    }

    private readonly struct PlaybackCommand
    {
        public CommandKind Kind { get; init; }
        public TimeSpan Position { get; init; }
        public TimeSpan Delta { get; init; }
        public bool HasPositionOverride { get; init; }
        public SeekIntentSlot? SeekSlot { get; init; }
        public ScrubUpdateIntentSlot? ScrubUpdateSlot { get; init; }
        public long QueuedTimestamp { get; init; }
    }

    private sealed class SeekIntentSlot
    {
        public SeekIntentSlot(long ticks)
        {
            LatestTicks = ticks;
        }

        public long LatestTicks;
    }

    private sealed class ScrubUpdateIntentSlot
    {
        public ScrubUpdateIntentSlot(long ticks)
        {
            LatestTicks = ticks;
        }

        public long LatestTicks;
    }

    private long _commandsEnqueued;
    private long _commandsProcessed;
    private long _commandsDropped;
    private int _pendingCommands;
    private int _maxPendingCommands;
    private long _lastCommandQueueLatencyMs;
    private long _maxCommandQueueLatencyMs;
    private string _maxCommandQueueLatencyCommand = "None";
    private long _lastCommandQueuedUtcUnixMs;
    private long _lastCommandProcessedUtcUnixMs;
    private string _lastCommandQueued = "None";
    private string _lastCommandProcessed = "None";
    private int _activeCommandKind = -1;
    private long _activeCommandStartedTimestamp;

    public long CommandsEnqueued => Interlocked.Read(ref _commandsEnqueued);
    public long CommandsProcessed => Interlocked.Read(ref _commandsProcessed);
    public long CommandsDropped => Interlocked.Read(ref _commandsDropped);
    public long CommandsSkippedNotReady => Interlocked.Read(ref _commandsSkippedNotReady);
    public long ScrubUpdatesCoalesced => Interlocked.Read(ref _scrubUpdatesCoalesced);
    public long SeekCommandsCoalesced => Interlocked.Read(ref _seekCommandsCoalesced);
    public int CommandQueueCapacityCommands => CommandQueueCapacity;
    public int PendingCommands => Volatile.Read(ref _pendingCommands);
    public int MaxPendingCommands => Volatile.Read(ref _maxPendingCommands);
    public long LastCommandQueueLatencyMs => Interlocked.Read(ref _lastCommandQueueLatencyMs);
    public long MaxCommandQueueLatencyMs => Interlocked.Read(ref _maxCommandQueueLatencyMs);
    public string MaxCommandQueueLatencyCommand => Volatile.Read(ref _maxCommandQueueLatencyCommand);
    public long LastCommandQueuedUtcUnixMs => Interlocked.Read(ref _lastCommandQueuedUtcUnixMs);
    public long LastCommandProcessedUtcUnixMs => Interlocked.Read(ref _lastCommandProcessedUtcUnixMs);
    public long LastCommandFailureUtcUnixMs => Interlocked.Read(ref _lastCommandFailureUtcUnixMs);
    public string LastCommandQueued => Volatile.Read(ref _lastCommandQueued);
    public string LastCommandProcessed => Volatile.Read(ref _lastCommandProcessed);
    public string LastCommandFailure => Volatile.Read(ref _lastCommandFailure);
    public bool PlaybackThreadAlive => _playbackThread is { IsAlive: true };

    private long _latestScrubUpdateTicks;
    private long _scrubUpdatesCoalesced;
    private long _seekCommandsCoalesced;
    private long _commandsSkippedNotReady;
    private long _lastCommandFailureUtcUnixMs;
    private readonly object _seekSlotSync = new();
    private SeekIntentSlot? _queuedSeekSlot;
    private ScrubUpdateIntentSlot? _queuedScrubUpdateSlot;
    private string _lastCommandFailure = string.Empty;

    private bool IsReady => _initialized && _disposedFlag == 0;

    public FlashbackPlaybackController(FlashbackBufferManager bufferManager)
    {
        _bufferManager = bufferManager ?? throw new ArgumentNullException(nameof(bufferManager));
        _commandChannel = CreateCommandChannel();
    }

    /// <summary>
    /// When true, the decoder attempts D3D11VA GPU decode. When false, forces software decode.
    /// Can be toggled at runtime - takes effect on next decoder creation.
    /// </summary>
    public bool GpuDecodeEnabled { get; set; } = true;

    public FlashbackPlaybackState State => _state;

    public TimeSpan PlaybackPosition
    {
        get => TimeSpan.FromTicks(Interlocked.Read(ref _playbackPositionTicks));
        private set => Interlocked.Exchange(ref _playbackPositionTicks, value.Ticks);
    }

    /// <summary>
    /// Distance from the live edge in absolute PTS space. Immune to the
    /// frozenValidStart vs currentValidStartPts coordinate mismatch that
    /// makes PlaybackPosition exceed BufferedDuration after segment eviction.
    /// </summary>
    public TimeSpan GapFromLive
    {
        get
        {
            var latest = _bufferManager.LatestPts;
            var lastFrame = TimeSpan.FromTicks(Interlocked.Read(ref _lastVideoPtsTicks));
            if (lastFrame == TimeSpan.Zero)
            {
                if (_state == FlashbackPlaybackState.Live) return TimeSpan.Zero;

                // No frame has been decoded yet since leaving Live (e.g. between
                // issuing Pause/Seek and the playback thread displaying its first
                // frame) -- _lastVideoPtsTicks briefly reads 0 and would otherwise
                // report a "-0:00" gap instead of the real distance from live.
                // Estimate the same way HandleEndOfSegment's fallback does:
                // PlaybackPosition is relative to the valid-start captured when
                // leaving Live, which is still _bufferManager.ValidStartPts here
                // because no time has passed for eviction to move it before the
                // first frame lands.
                var estimatedAbsPts = SaturatingAdd(PlaybackPosition, _bufferManager.ValidStartPts);
                var estimatedGap = latest - estimatedAbsPts;
                return estimatedGap > TimeSpan.Zero ? estimatedGap : TimeSpan.Zero;
            }
            var gap = latest - lastFrame;
            return gap > TimeSpan.Zero ? gap : TimeSpan.Zero;
        }
    }

    public bool IsInitialized => _initialized;
    public bool IsDisposed => _disposedFlag != 0;
    public string DecoderHwAccel => _decoderHwAccel;

    /// <summary>
    /// Returns true (and logs a skip) when the controller is not ready to accept commands.
    /// </summary>
    private bool IsNotReady(CommandKind kind, TimeSpan? position = null)
    {
        if (IsReady) return false;
        return RejectCommand(
            kind,
            "not_ready",
            $"not_ready initialized={_initialized} disposed={_disposedFlag != 0}",
            true,
            position);
    }

    private void MarkCommandQueued(CommandKind kind)
    {
        Interlocked.Exchange(ref _lastCommandQueuedUtcUnixMs, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        Volatile.Write(ref _lastCommandQueued, kind.ToString());
        ClearLastCommandFailure();
    }

    private void SetLastSubmitFailure(string failure)
    {
        Volatile.Write(ref _lastSubmitFailure, failure);
        Interlocked.Exchange(ref _lastSubmitFailureUtcUnixMs, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
    }

    private void ClearLastSubmitFailure()
    {
        Volatile.Write(ref _lastSubmitFailure, string.Empty);
        Interlocked.Exchange(ref _lastSubmitFailureUtcUnixMs, 0);
    }

    private void RecordPlaybackDroppedFrame(string reason)
    {
        Interlocked.Increment(ref _playbackDroppedFrames);
        Volatile.Write(ref _lastPlaybackDropReason, reason);
        Interlocked.Exchange(ref _lastPlaybackDropUtcUnixMs, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
    }

    private void TrackCoalescedScrubUpdate()
    {
        var dropped = Interlocked.Increment(ref _commandsDropped);
        var coalesced = Interlocked.Increment(ref _scrubUpdatesCoalesced);
        if (coalesced == 1 || coalesced % 120 == 0)
        {
            Logger.Log($"FLASHBACK_PLAYBACK_SCRUB_COALESCED count={coalesced} dropped={dropped}");
        }
    }

    private void TrackCoalescedSeekCommand()
    {
        var coalesced = Interlocked.Increment(ref _seekCommandsCoalesced);
        if (coalesced == 1 || coalesced % 120 == 0)
        {
            Logger.Log($"FLASHBACK_PLAYBACK_SEEK_COALESCED count={coalesced}");
        }
    }

    private void TrackCommandDequeued(PlaybackCommand command)
    {
        Interlocked.Increment(ref _commandsProcessed);
        DecrementPendingCommands();
        TrackCommandQueueLatency(command);
        Interlocked.Exchange(ref _lastCommandProcessedUtcUnixMs, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        Volatile.Write(ref _lastCommandProcessed, command.Kind.ToString());
    }

    private void TrackCommandQueueLatency(PlaybackCommand command)
    {
        if (command.QueuedTimestamp <= 0)
        {
            return;
        }

        var elapsedTicks = Stopwatch.GetTimestamp() - command.QueuedTimestamp;
        var latencyMs = Math.Max(0, (long)(elapsedTicks * 1000.0 / Stopwatch.Frequency));
        Interlocked.Exchange(ref _lastCommandQueueLatencyMs, latencyMs);
        UpdateMaxCommandQueueLatency(command.Kind, latencyMs);
    }

    private void UpdateMaxCommandQueueLatency(CommandKind commandKind, long latencyMs)
    {
        while (true)
        {
            var current = Interlocked.Read(ref _maxCommandQueueLatencyMs);
            if (latencyMs <= current)
            {
                return;
            }

            if (Interlocked.CompareExchange(ref _maxCommandQueueLatencyMs, latencyMs, current) == current)
            {
                Volatile.Write(ref _maxCommandQueueLatencyCommand, commandKind.ToString());
                return;
            }
        }
    }

    private void UpdateMaxPendingCommands(int value)
        => AtomicMax.Update(ref _maxPendingCommands, value);

    private void DecrementPendingCommands()
    {
        while (true)
        {
            var current = Volatile.Read(ref _pendingCommands);
            if (current <= 0)
            {
                return;
            }

            if (Interlocked.CompareExchange(ref _pendingCommands, current - 1, current) == current)
            {
                return;
            }
        }
    }

    private static string FormatActiveCommandKind(int rawKind)
    {
        if (rawKind < 0) return "None";
        return Enum.IsDefined(typeof(CommandKind), rawKind)
            ? ((CommandKind)rawKind).ToString()
            : rawKind.ToString(CultureInfo.InvariantCulture);
    }

    private double GetActiveCommandElapsedMs(long nowTimestamp)
    {
        var started = Volatile.Read(ref _activeCommandStartedTimestamp);
        if (started <= 0) return 0;
        return Stopwatch.GetElapsedTime(started, nowTimestamp).TotalMilliseconds;
    }

    public void Initialize(
        IPreviewFrameSink previewSink,
        ILiveVideoSource videoCapture,
        WasapiAudioPlayback? audioPlayback,
        WasapiAudioCapture? audioCapture)
    {
        var applyRouting = false;
        lock (_playbackThreadSync)
        {
            ObjectDisposedException.ThrowIf(_disposedFlag != 0, this);
            if (TryDeferPreviewAttachAfterStopTimeoutUnsafe(previewSink, videoCapture, "init"))
            {
                _audioPlayback = audioPlayback;
                _audioCapture = audioCapture;
                return;
            }

            _previewSink = previewSink ?? throw new ArgumentNullException(nameof(previewSink));
            _videoCapture = videoCapture ?? throw new ArgumentNullException(nameof(videoCapture));
            _audioPlayback = audioPlayback;
            _audioCapture = audioCapture;
            _initialized = true;
            Logger.Log("FLASHBACK_PLAYBACK_INIT");
            applyRouting = true;
        }

        if (applyRouting)
        {
            ApplyPreviewRoutingForState("init");
            ApplyAudioRoutingForState("init");
        }
    }

    /// <summary>
    /// Updates audio references after WASAPI components become available.
    /// Called from CaptureService after preview audio playback starts,
    /// since WASAPI init happens after flashback controller init.
    /// </summary>
    public void UpdateAudioComponents(WasapiAudioPlayback? audioPlayback, WasapiAudioCapture? audioCapture)
    {
        lock (_playbackThreadSync)
        {
            if (_disposedFlag != 0)
            {
                Logger.Log("FLASHBACK_PLAYBACK_AUDIO_UPDATE_SKIP reason=disposed");
                return;
            }

            _audioPlayback = audioPlayback;
            _audioCapture = audioCapture;
            Logger.Log($"FLASHBACK_PLAYBACK_AUDIO_UPDATE playback={audioPlayback != null} capture={audioCapture != null} state={_state}");
        }

        ApplyAudioRoutingForState("audio_update");
    }

    public void UpdatePreviewComponents(IPreviewFrameSink? previewSink, ILiveVideoSource? videoCapture)
    {
        var applyRouting = false;
        lock (_playbackThreadSync)
        {
            if (_disposedFlag != 0)
            {
                Logger.Log("FLASHBACK_PLAYBACK_PREVIEW_UPDATE_SKIP reason=disposed");
                return;
            }

            if (TryDeferPreviewAttachAfterStopTimeoutUnsafe(previewSink, videoCapture, "update"))
            {
                return;
            }

            _previewSink = previewSink;
            _videoCapture = videoCapture;
            _initialized = previewSink != null && videoCapture != null;
            Logger.Log($"FLASHBACK_PLAYBACK_PREVIEW_UPDATE sink={previewSink != null} capture={videoCapture != null}");
            applyRouting = _initialized;
        }

        if (applyRouting)
        {
            ApplyPreviewRoutingForState("preview_update");
        }
    }

    /// <summary>
    /// Pre-starts the playback thread (thread creation, MMCSS registration)
    /// without issuing any command, so the first real Pause/Seek/BeginScrub
    /// doesn't pay that one-time cost. Decoder creation and file-open remain
    /// lazy -- those only happen inside a command handler on the playback
    /// thread -- so this only removes the thread-startup portion of the
    /// first-interaction latency. Safe to call repeatedly; a no-op if the
    /// thread is already running.
    /// </summary>
    public void PreWarm()
    {
        if (!_initialized || _disposedFlag != 0)
        {
            return;
        }

        try
        {
            var ok = EnsurePlaybackThread(CommandKind.Warm);
            Logger.Log($"FLASHBACK_PLAYBACK_PREWARM ok={ok}");
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_PLAYBACK_PREWARM ok=false type={ex.GetType().Name} msg='{ex.Message}'");
        }
    }

    public bool BeginScrub(TimeSpan position)
    {
        if (IsNotReady(CommandKind.BeginScrub, position)) return false;
        if (!EnsurePlaybackThread(CommandKind.BeginScrub)) return false;
        Interlocked.Exchange(ref _latestScrubUpdateTicks, position.Ticks);
        return SendCommand(new PlaybackCommand { Kind = CommandKind.BeginScrub, Position = position });
    }

    public bool Seek(TimeSpan position)
    {
        if (IsNotReady(CommandKind.Seek, position)) return false;
        if (!EnsurePlaybackThread(CommandKind.Seek)) return false;
        return SendSeekCommand(position);
    }

    public bool UpdateScrub(TimeSpan position)
    {
        if (IsNotReady(CommandKind.UpdateScrub, position)) return false;
        if (!PlaybackThreadAlive) return RejectCommand(CommandKind.UpdateScrub, "thread_not_running", "thread_not_running", false, position);
        return SendUpdateScrubCommand(position);
    }

    public bool EndScrub() => EndScrubAt(null);

    public bool EndScrubAt(TimeSpan position) => EndScrubAt((TimeSpan?)position);

    private bool EndScrubAt(TimeSpan? position)
    {
        if (IsNotReady(CommandKind.EndScrub, position)) return false;
        if (State == FlashbackPlaybackState.Live && !PlaybackThreadAlive)
        {
            MarkCommandNoOp(CommandKind.EndScrub, "live_thread_not_running", position);
            return false;
        }
        if (!PlaybackThreadAlive) return RejectCommand(CommandKind.EndScrub, "thread_not_running", "thread_not_running", false, position);
        return SendEndScrubCommand(position);
    }

    public bool Play()
    {
        if (IsNotReady(CommandKind.Play)) return false;
        if (!EnsurePlaybackThread(CommandKind.Play)) return false;
        return SendCommand(new PlaybackCommand { Kind = CommandKind.Play });
    }

    public bool Pause()
    {
        if (IsNotReady(CommandKind.Pause)) return false;
        if (!EnsurePlaybackThread(CommandKind.Pause)) return false; // Thread must be running to handle Live->Paused
        return SendCommand(new PlaybackCommand { Kind = CommandKind.Pause });
    }

    public bool GoLive()
    {
        if (IsNotReady(CommandKind.GoLive)) return false;
        if (State == FlashbackPlaybackState.Live && !PlaybackThreadAlive)
        {
            MarkCommandNoOp(CommandKind.GoLive, "live_thread_not_running");
            return false;
        }
        if (!EnsurePlaybackThread(CommandKind.GoLive)) return false;
        return SendCommand(new PlaybackCommand { Kind = CommandKind.GoLive });
    }

    public bool NudgePosition(TimeSpan delta)
    {
        if (IsNotReady(CommandKind.Nudge)) return false;
        if (State == FlashbackPlaybackState.Live && !PlaybackThreadAlive)
        {
            MarkCommandNoOp(CommandKind.Nudge, "live_thread_not_running", delta: delta);
            return false;
        }
        if (!EnsurePlaybackThread(CommandKind.Nudge)) return false;
        return SendCommand(new PlaybackCommand { Kind = CommandKind.Nudge, Delta = delta });
    }

    private bool SendCommand(PlaybackCommand command)
    {
        lock (_seekSlotSync)
        {
            if (!SendCommandCore(command))
            {
                return false;
            }

            if (command.Kind != CommandKind.Seek)
            {
                _queuedSeekSlot = null;
            }

            if (command.Kind != CommandKind.UpdateScrub)
            {
                _queuedScrubUpdateSlot = null;
            }

            return true;
        }
    }

    private bool SendCommandCore(PlaybackCommand command)
    {
        if (_disposedFlag != 0 && command.Kind != CommandKind.Stop)
        {
            return RejectCommand(command.Kind, "disposed", "disposed", false);
        }

        var queuedCommand = new PlaybackCommand
        {
            Kind = command.Kind,
            Position = command.Position,
            Delta = command.Delta,
            HasPositionOverride = command.HasPositionOverride,
            SeekSlot = command.SeekSlot,
            ScrubUpdateSlot = command.ScrubUpdateSlot,
            QueuedTimestamp = Stopwatch.GetTimestamp()
        };

        var pending = Interlocked.Increment(ref _pendingCommands);
        var droppedOldest = false;
        var droppedCommand = default(PlaybackCommand);
        if (!_commandChannel.Writer.TryWrite(queuedCommand) &&
            (!IsCommandChannelOpenForDropRetry() ||
             !TryDropOldestQueuedCommandForNewCommand(out droppedCommand) ||
             !(droppedOldest = _commandChannel.Writer.TryWrite(queuedCommand))))
        {
            DecrementPendingCommands();
            Interlocked.Increment(ref _commandsDropped);
            var detail = FormatCommandDetail(command);
            SetLastCommandFailure($"write_failed:{command.Kind}{detail}");
            Logger.Log($"FLASHBACK_PLAYBACK_CMD_DROP kind={command.Kind}{detail}");
            return false;
        }

        if (droppedOldest)
        {
            TrackDroppedQueuedCommand(droppedCommand, queuedCommand.Kind);
        }

        Interlocked.Increment(ref _commandsEnqueued);
        UpdateMaxPendingCommands(pending);
        MarkCommandQueued(command.Kind);
        return true;
    }

    private bool IsCommandChannelOpenForDropRetry()
    {
        try
        {
            var canWrite = _commandChannel.Writer.WaitToWriteAsync();
            return !canWrite.IsCompletedSuccessfully || canWrite.Result;
        }
        catch (Exception ex) when (ex is ChannelClosedException or InvalidOperationException)
        {
            return false;
        }
    }

    private bool TryDropOldestQueuedCommandForNewCommand(out PlaybackCommand droppedCommand)
    {
        if (!_commandChannel.Reader.TryRead(out droppedCommand))
        {
            return false;
        }

        DecrementPendingCommands();
        return true;
    }

    private void TrackDroppedQueuedCommand(PlaybackCommand droppedCommand, CommandKind newCommandKind)
    {
        ClearQueuedCommandSlotForDroppedCommand(droppedCommand);

        if (droppedCommand.Kind == CommandKind.Stop)
        {
            Logger.Log($"FLASHBACK_PLAYBACK_CMD_DROP_OLD kind=Stop new_kind={newCommandKind} reason=channel_full");
            return;
        }

        Interlocked.Increment(ref _commandsDropped);
        var detail = FormatCommandDetail(droppedCommand);
        Logger.Log($"FLASHBACK_PLAYBACK_CMD_DROP_OLD kind={droppedCommand.Kind}{detail} new_kind={newCommandKind} reason=channel_full");
    }

    private bool SendSeekCommand(TimeSpan position)
    {
        lock (_seekSlotSync)
        {
            if (_queuedSeekSlot is { } queuedSlot)
            {
                _queuedScrubUpdateSlot = null;
                queuedSlot.LatestTicks = position.Ticks;
                TrackCoalescedSeekCommand();
                ClearLastCommandFailure();
                return true;
            }

            var slot = new SeekIntentSlot(position.Ticks);
            _queuedSeekSlot = slot;
            if (!SendCommandCore(new PlaybackCommand { Kind = CommandKind.Seek, Position = position, SeekSlot = slot }))
            {
                ClearQueuedSeekSlotUnsafe(slot);
                return false;
            }

            _queuedScrubUpdateSlot = null;
            return true;
        }
    }

    private bool SendUpdateScrubCommand(TimeSpan position)
    {
        lock (_seekSlotSync)
        {
            Interlocked.Exchange(ref _latestScrubUpdateTicks, position.Ticks);
            if (_queuedScrubUpdateSlot is { } queuedSlot)
            {
                _queuedSeekSlot = null;
                queuedSlot.LatestTicks = position.Ticks;
                TrackCoalescedScrubUpdate();
                ClearLastCommandFailure();
                return true;
            }

            var slot = new ScrubUpdateIntentSlot(position.Ticks);
            _queuedScrubUpdateSlot = slot;
            if (!SendCommandCore(new PlaybackCommand { Kind = CommandKind.UpdateScrub, Position = position, ScrubUpdateSlot = slot }))
            {
                ClearQueuedScrubUpdateSlotUnsafe(slot);
                return false;
            }

            _queuedSeekSlot = null;
            return true;
        }
    }

    private bool SendEndScrubCommand(TimeSpan? position)
    {
        lock (_seekSlotSync)
        {
            var commandTicks = position?.Ticks ??
                               _queuedScrubUpdateSlot?.LatestTicks ??
                               Interlocked.Read(ref _latestScrubUpdateTicks);
            var commandPosition = TimeSpan.FromTicks(commandTicks);
            if (position.HasValue)
            {
                Interlocked.Exchange(ref _latestScrubUpdateTicks, position.Value.Ticks);
            }

            if (!SendCommandCore(new PlaybackCommand
            {
                Kind = CommandKind.EndScrub,
                Position = commandPosition,
                HasPositionOverride = position.HasValue
            }))
            {
                return false;
            }

            _queuedSeekSlot = null;
            _queuedScrubUpdateSlot = null;
            return true;
        }
    }

    private PlaybackCommand ResolveSeekCommandPosition(PlaybackCommand command)
    {
        var slot = command.SeekSlot;
        if (slot is null)
        {
            return command;
        }

        lock (_seekSlotSync)
        {
            var resolved = command with { Position = TimeSpan.FromTicks(slot.LatestTicks) };
            if (ReferenceEquals(_queuedSeekSlot, slot))
            {
                _queuedSeekSlot = null;
            }

            return resolved;
        }
    }

    private PlaybackCommand ResolveScrubUpdateCommandPosition(PlaybackCommand command)
    {
        var slot = command.ScrubUpdateSlot;
        if (slot is null)
        {
            return command;
        }

        lock (_seekSlotSync)
        {
            var resolved = command with { Position = TimeSpan.FromTicks(slot.LatestTicks) };
            if (ReferenceEquals(_queuedScrubUpdateSlot, slot))
            {
                _queuedScrubUpdateSlot = null;
            }

            return resolved;
        }
    }

    private void ClearQueuedSeekSlotUnsafe(SeekIntentSlot slot)
    {
        if (ReferenceEquals(_queuedSeekSlot, slot))
        {
            _queuedSeekSlot = null;
        }
    }

    private void ClearQueuedScrubUpdateSlotUnsafe(ScrubUpdateIntentSlot slot)
    {
        if (ReferenceEquals(_queuedScrubUpdateSlot, slot))
        {
            _queuedScrubUpdateSlot = null;
        }
    }

    private void ClearQueuedCommandSlotsBarrier()
    {
        lock (_seekSlotSync)
        {
            _queuedSeekSlot = null;
            _queuedScrubUpdateSlot = null;
        }
    }

    private void ClearQueuedCommandSlotForDroppedCommand(PlaybackCommand command)
    {
        lock (_seekSlotSync)
        {
            if (command.SeekSlot != null && ReferenceEquals(_queuedSeekSlot, command.SeekSlot))
            {
                _queuedSeekSlot = null;
            }

            if (command.ScrubUpdateSlot != null && ReferenceEquals(_queuedScrubUpdateSlot, command.ScrubUpdateSlot))
            {
                _queuedScrubUpdateSlot = null;
            }
        }
    }

    private static bool ShouldYieldScrubUpdateToQueuedControl(Channel<PlaybackCommand> commandChannel)
    {
        if (!commandChannel.Reader.TryPeek(out var next))
        {
            return false;
        }

        return next.Kind is CommandKind.EndScrub or CommandKind.Play or CommandKind.GoLive or CommandKind.Stop;
    }

    private static bool ShouldYieldSeekToQueuedPlay(Channel<PlaybackCommand> commandChannel)
    {
        if (!commandChannel.Reader.TryPeek(out var next))
        {
            return false;
        }

        return next.Kind is CommandKind.Play or CommandKind.GoLive or CommandKind.Stop;
    }

    private static bool ShouldYieldPauseFromLiveToQueuedSeekOrPlay(Channel<PlaybackCommand> commandChannel)
    {
        if (!commandChannel.Reader.TryPeek(out var next))
        {
            return false;
        }

        return next.Kind is CommandKind.Seek or CommandKind.Play or CommandKind.GoLive or CommandKind.Stop;
    }

    private bool RejectCommand(
        CommandKind kind,
        string failure,
        string reason,
        bool returnValue,
        TimeSpan? position = null)
    {
        Interlocked.Increment(ref _commandsSkippedNotReady);
        var detail = FormatCommandDetail(position: position);
        SetLastCommandFailure($"{failure}:{kind}{detail}");
        Logger.Log($"FLASHBACK_PLAYBACK_CMD_SKIP kind={kind} reason={reason}{detail}");
        return returnValue;
    }

    private void SetNoFileFailure(CommandKind kind, TimeSpan position)
    {
        SetLastCommandFailure($"no_file:{kind}{FormatCommandDetail(position: position)}");
    }

    private void SetReopenFailure(string reason, string detail, TimeSpan position)
    {
        SetLastCommandFailure($"reopen_failed:{reason}:{detail}{FormatCommandDetail(position: position)}");
    }

    private void SetSeekDisplayFailure(CommandKind kind, string detail, TimeSpan position)
    {
        SetLastCommandFailure($"seek_display_failed:{kind}:{detail}{FormatCommandDetail(position: position)}");
    }

    private static string FormatCommandDetail(PlaybackCommand command)
        => FormatCommandDetail(command.Position, command.Delta);

    private static string FormatCommandDetail(TimeSpan? position = null, TimeSpan? delta = null)
    {
        if (position.HasValue)
        {
            return $" pos_ms={position.Value.TotalMilliseconds.ToString("0.###", CultureInfo.InvariantCulture)}";
        }

        if (delta.HasValue)
        {
            return $" delta_ms={delta.Value.TotalMilliseconds.ToString("0.###", CultureInfo.InvariantCulture)}";
        }

        return string.Empty;
    }

    private void SetLastCommandFailure(string failure)
    {
        Volatile.Write(ref _lastCommandFailure, failure);
        Interlocked.Exchange(ref _lastCommandFailureUtcUnixMs, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
    }

    private void ClearLastCommandFailure()
    {
        Volatile.Write(ref _lastCommandFailure, string.Empty);
        Interlocked.Exchange(ref _lastCommandFailureUtcUnixMs, 0);
    }

    private void MarkCommandNoOp(CommandKind kind, string reason, TimeSpan? position = null, TimeSpan? delta = null)
    {
        ClearLastCommandFailure();
        Logger.Log($"FLASHBACK_PLAYBACK_CMD_NOOP kind={kind} reason={reason}{FormatCommandDetail(position, delta)}");
    }

    public void PrepareForPreviewDetach()
    {
        if (_disposedFlag != 0)
        {
            Logger.Log("FLASHBACK_PLAYBACK_PREVIEW_DETACH_SKIP reason=disposed");
            return;
        }

        Logger.Log($"FLASHBACK_PLAYBACK_PREVIEW_DETACH state={_state} thread_alive={PlaybackThreadAlive}");
        if (!StopPlaybackThread(PreviewDetachThreadStopTimeout, "preview_detach"))
        {
            Logger.Log("FLASHBACK_PLAYBACK_PREVIEW_DETACH_ABORT reason=thread_stop_failed");
            RestoreLiveAudio();
            SafeResumePreviewSubmission("preview_detach_timeout");
            DetachPreviewComponentsAfterStopTimeout();
            return;
        }

        ReleasePlaybackFrameForLive("preview_detach");
        RestoreLiveAudio();
        SafeResumePreviewSubmission("preview_detach");
        SetState(FlashbackPlaybackState.Live);
    }

    public void Dispose()
    {
        if (Interlocked.CompareExchange(ref _disposedFlag, 1, 0) != 0) return;

        Logger.Log($"FLASHBACK_PLAYBACK_DISPOSE_REQUEST state={_state} initialized={_initialized}");
        StopPlaybackThread(PlaybackThreadStopTimeout, "dispose");
        _initialized = false;
        Logger.Log("FLASHBACK_PLAYBACK_DISPOSED");
    }

    public event Action<FlashbackPlaybackState, FlashbackPlaybackState, string>? StateChanged;

    private void SetState(FlashbackPlaybackState newState, string reason = "")
    {
        var oldState = _state;
        if (oldState == newState) return;
        _state = newState;
        Logger.Log($"FLASHBACK_PLAYBACK_STATE {oldState} -> {newState} reason='{reason}'");
        try
        {
            StateChanged?.Invoke(oldState, newState, reason);
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_PLAYBACK_STATE_EVENT_WARN type={ex.GetType().Name} msg='{ex.Message}'");
        }
    }

    private void DetachPreviewComponentsAfterStopTimeout()
    {
        lock (_playbackThreadSync)
        {
            Volatile.Write(ref _previewDetachStopTimeoutActive, 1);
            _pendingPreviewSinkAfterDetachTimeout = null;
            _pendingVideoCaptureAfterDetachTimeout = null;
            _previewSink = null;
            _videoCapture = null;
            _initialized = false;
        }

        Logger.Log("FLASHBACK_PLAYBACK_PREVIEW_DETACH_DEFER_OWNED_CLEANUP reason=thread_alive");
    }

    private bool TryDeferPreviewAttachAfterStopTimeoutUnsafe(
        IPreviewFrameSink? previewSink,
        ILiveVideoSource? videoCapture,
        string operation)
    {
        if (previewSink == null || videoCapture == null)
        {
            return false;
        }

        if (Volatile.Read(ref _previewDetachStopTimeoutActive) == 0 || !PlaybackThreadAlive)
        {
            return false;
        }

        _pendingPreviewSinkAfterDetachTimeout = previewSink;
        _pendingVideoCaptureAfterDetachTimeout = videoCapture;
        _initialized = false;
        Logger.Log($"FLASHBACK_PLAYBACK_PREVIEW_ATTACH_DEFER op={operation} reason=thread_alive_after_detach_timeout");
        return true;
    }

    private void ApplyDeferredPreviewAttachAfterStopTimeout()
    {
        IPreviewFrameSink? pendingSink;
        ILiveVideoSource? pendingCapture;
        var lockTaken = false;
        try
        {
            Monitor.TryEnter(_playbackThreadSync, 0, ref lockTaken);
            if (!lockTaken)
            {
                Logger.Log("FLASHBACK_PLAYBACK_PREVIEW_ATTACH_DEFER_APPLY_SKIP reason=lock_busy");
                ScheduleDeferredPreviewAttachApplyRetry();
                return;
            }

            Volatile.Write(ref _previewDetachStopTimeoutActive, 0);
            Interlocked.Exchange(ref _deferredPreviewAttachApplyRetryScheduled, 0);
            pendingSink = _pendingPreviewSinkAfterDetachTimeout;
            pendingCapture = _pendingVideoCaptureAfterDetachTimeout;
            _pendingPreviewSinkAfterDetachTimeout = null;
            _pendingVideoCaptureAfterDetachTimeout = null;

            if (_disposedFlag != 0 || pendingSink == null || pendingCapture == null)
            {
                return;
            }

            _previewSink = pendingSink;
            _videoCapture = pendingCapture;
            _initialized = true;
        }
        finally
        {
            if (lockTaken)
            {
                Monitor.Exit(_playbackThreadSync);
            }
        }

        Logger.Log("FLASHBACK_PLAYBACK_PREVIEW_ATTACH_DEFER_APPLIED reason=thread_exit");
        ApplyPreviewRoutingForState("deferred_preview_attach");
        ApplyAudioRoutingForState("deferred_preview_attach");
    }

    private void ScheduleDeferredPreviewAttachApplyRetry()
    {
        if (Volatile.Read(ref _previewDetachStopTimeoutActive) == 0)
        {
            return;
        }

        if (Interlocked.CompareExchange(ref _deferredPreviewAttachApplyRetryScheduled, 1, 0) != 0)
        {
            return;
        }

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(25).ConfigureAwait(false);
                Interlocked.Exchange(ref _deferredPreviewAttachApplyRetryScheduled, 0);
                if (Volatile.Read(ref _previewDetachStopTimeoutActive) != 0)
                {
                    ApplyDeferredPreviewAttachAfterStopTimeout();
                }
            }
            catch (Exception ex)
            {
                Interlocked.Exchange(ref _deferredPreviewAttachApplyRetryScheduled, 0);
                Logger.Log($"FLASHBACK_PLAYBACK_PREVIEW_ATTACH_DEFER_RETRY_WARN type={ex.GetType().Name} msg='{ex.Message}'");
            }
        });
    }

    public readonly record struct PlaybackCadenceMetrics(
        int SampleCount,
        double P95FrameMs,
        double P99FrameMs,
        double MaxFrameMs,
        long SlowFrameCount,
        double SlowFramePercent,
        double OnePercentLowFps,
        double FivePercentLowFps,
        double SampleDurationMs,
        double[] RecentFrameIntervalsMs);

    public readonly record struct PlaybackDecodeMetrics(
        int SampleCount,
        double AvgMs,
        double P95Ms,
        double P99Ms,
        double MaxMs);

    // Playback cadence metrics are written on the playback thread and read from
    // UI/diagnostics snapshots.
    private long _playbackFrameCount;
    private long _playbackPreviewPresentId;
    private long _playbackLateFrames;
    private long _playbackDroppedFrames;
    private long _playbackSegmentSwitches;
    private long _playbackFmp4Reopens;
    private long _playbackReopenAudioNullWindowCount;
    private long _playbackWriteHeadWaits;
    private long _playbackNearLiveSnaps;
    private long _playbackDecodeErrorSnaps;
    private long _playbackSubmitFailures;
    private long _lastPlaybackDropUtcUnixMs;
    private string _lastPlaybackDropReason = string.Empty;
    private long _lastSubmitFailureUtcUnixMs;
    private string _lastSubmitFailure = string.Empty;
    private long _lastSegmentSwitchUtcUnixMs;
    private long _lastFmp4ReopenUtcUnixMs;
    private long _lastWriteHeadWaitGapMs;
    private double _playbackTargetFps;
    private double _playbackObservedFps;
    private double _playbackAvgFrameMs;
    private readonly Stopwatch _playbackFpsClock = new();
    private const int PlaybackCadenceSampleCapacity = 240;
    private readonly object _playbackCadenceLock = new();
    private readonly double[] _playbackFrameIntervalsMs = new double[PlaybackCadenceSampleCapacity];
    private int _playbackFrameIntervalHead;
    private int _playbackFrameIntervalCount;
    private long _playbackSlowFrameCount;

    private readonly object _playbackDecodeLock = new();
    private readonly double[] _playbackDecodeDurationsMs = new double[PlaybackCadenceSampleCapacity];
    private int _playbackDecodeDurationHead;
    private int _playbackDecodeDurationCount;
    private double _playbackMaxDecodeTotalMs;
    private double _playbackMaxDecodeReceiveMs;
    private double _playbackMaxDecodeFeedMs;
    private double _playbackMaxDecodeReadMs;
    private double _playbackMaxDecodeSendMs;
    private double _playbackMaxDecodeAudioMs;
    private double _playbackMaxDecodeConvertMs;
    private string _playbackMaxDecodePhase = string.Empty;
    private long _playbackMaxDecodeUtcUnixMs;
    private long _playbackMaxDecodePositionMs;

    private long _playbackSeekForwardDecodeCapHits;
    private int _lastPlaybackSeekHitForwardDecodeCap;

    public long PlaybackFrameCount => Interlocked.Read(ref _playbackFrameCount);
    public long PlaybackLateFrames => Interlocked.Read(ref _playbackLateFrames);
    public long PlaybackDroppedFrames => Interlocked.Read(ref _playbackDroppedFrames);
    public long PlaybackSegmentSwitches => Interlocked.Read(ref _playbackSegmentSwitches);
    public long PlaybackFmp4Reopens => Interlocked.Read(ref _playbackFmp4Reopens);
    public long PlaybackReopenAudioNullWindowCount => Interlocked.Read(ref _playbackReopenAudioNullWindowCount);
    public long PlaybackWriteHeadWaits => Interlocked.Read(ref _playbackWriteHeadWaits);
    public long PlaybackNearLiveSnaps => Interlocked.Read(ref _playbackNearLiveSnaps);
    public long PlaybackDecodeErrorSnaps => Interlocked.Read(ref _playbackDecodeErrorSnaps);
    public long PlaybackSubmitFailures => Interlocked.Read(ref _playbackSubmitFailures);
    public long LastPlaybackDropUtcUnixMs => Interlocked.Read(ref _lastPlaybackDropUtcUnixMs);
    public string LastPlaybackDropReason => Volatile.Read(ref _lastPlaybackDropReason);
    public long LastSubmitFailureUtcUnixMs => Interlocked.Read(ref _lastSubmitFailureUtcUnixMs);
    public string LastSubmitFailure => Volatile.Read(ref _lastSubmitFailure);
    public long LastSegmentSwitchUtcUnixMs => Interlocked.Read(ref _lastSegmentSwitchUtcUnixMs);
    public long LastFmp4ReopenUtcUnixMs => Interlocked.Read(ref _lastFmp4ReopenUtcUnixMs);
    public long LastWriteHeadWaitGapMs => Interlocked.Read(ref _lastWriteHeadWaitGapMs);
    public double PlaybackTargetFps => _playbackTargetFps;
    public double PlaybackObservedFps => _playbackObservedFps;
    public double PlaybackAvgFrameMs => _playbackAvgFrameMs;
    public string PlaybackMaxDecodePhase => Volatile.Read(ref _playbackMaxDecodePhase);
    public double PlaybackMaxDecodeReceiveMs => _playbackMaxDecodeReceiveMs;
    public double PlaybackMaxDecodeFeedMs => _playbackMaxDecodeFeedMs;
    public double PlaybackMaxDecodeReadMs => _playbackMaxDecodeReadMs;
    public double PlaybackMaxDecodeSendMs => _playbackMaxDecodeSendMs;
    public double PlaybackMaxDecodeAudioMs => _playbackMaxDecodeAudioMs;
    public double PlaybackMaxDecodeConvertMs => _playbackMaxDecodeConvertMs;
    public long PlaybackMaxDecodeUtcUnixMs => Interlocked.Read(ref _playbackMaxDecodeUtcUnixMs);
    public long PlaybackMaxDecodePositionMs => Interlocked.Read(ref _playbackMaxDecodePositionMs);
    public long PlaybackSeekForwardDecodeCapHits => Interlocked.Read(ref _playbackSeekForwardDecodeCapHits);
    public bool LastPlaybackSeekHitForwardDecodeCap => Volatile.Read(ref _lastPlaybackSeekHitForwardDecodeCap) != 0;

    public PlaybackCadenceMetrics GetPlaybackCadenceMetrics()
    {
        double[] samples;
        lock (_playbackCadenceLock)
        {
            if (_playbackFrameIntervalCount == 0)
            {
                return new PlaybackCadenceMetrics(0, 0, 0, 0, Interlocked.Read(ref _playbackSlowFrameCount), 0, 0, 0, 0, Array.Empty<double>());
            }

            samples = new double[_playbackFrameIntervalCount];
            var oldest = (_playbackFrameIntervalHead - _playbackFrameIntervalCount + _playbackFrameIntervalsMs.Length) % _playbackFrameIntervalsMs.Length;
            for (var i = 0; i < samples.Length; i++)
            {
                samples[i] = _playbackFrameIntervalsMs[(oldest + i) % _playbackFrameIntervalsMs.Length];
            }
        }

        var sum = 0.0;
        for (var i = 0; i < samples.Length; i++)
        {
            sum += samples[i];
        }

        var sorted = (double[])samples.Clone();
        Array.Sort(sorted);
        var p95 = PercentileHelpers.FromSorted(sorted, 0.95);
        var p99 = PercentileHelpers.FromSorted(sorted, 0.99);
        var max = sorted[^1];
        var slow = Interlocked.Read(ref _playbackSlowFrameCount);
        var totalFrames = Math.Max(1, Interlocked.Read(ref _playbackFrameCount));
        var slowPercent = slow * 100.0 / totalFrames;
        var onePercentLowFps = p99 > 0 ? 1000.0 / p99 : 0;
        var fivePercentLowFps = p95 > 0 ? 1000.0 / p95 : 0;
        return new PlaybackCadenceMetrics(samples.Length, p95, p99, max, slow, slowPercent, onePercentLowFps, fivePercentLowFps, sum, samples);
    }

    public PlaybackDecodeMetrics GetPlaybackDecodeMetrics()
    {
        double[] samples;
        lock (_playbackDecodeLock)
        {
            if (_playbackDecodeDurationCount == 0)
            {
                return new PlaybackDecodeMetrics(0, 0, 0, 0, 0);
            }

            samples = new double[_playbackDecodeDurationCount];
            var oldest = (_playbackDecodeDurationHead - _playbackDecodeDurationCount + _playbackDecodeDurationsMs.Length) % _playbackDecodeDurationsMs.Length;
            for (var i = 0; i < samples.Length; i++)
            {
                samples[i] = _playbackDecodeDurationsMs[(oldest + i) % _playbackDecodeDurationsMs.Length];
            }
        }

        var total = 0.0;
        for (var i = 0; i < samples.Length; i++)
        {
            total += samples[i];
        }

        Array.Sort(samples);
        return new PlaybackDecodeMetrics(
            samples.Length,
            total / samples.Length,
            PercentileHelpers.FromSorted(samples, 0.95),
            PercentileHelpers.FromSorted(samples, 0.99),
            samples[^1]);
    }

    private void TrackPlaybackCadence(double intervalMs, double expectedFrameMs)
    {
        if (intervalMs <= 0 || double.IsNaN(intervalMs) || double.IsInfinity(intervalMs))
        {
            return;
        }

        lock (_playbackCadenceLock)
        {
            _playbackFrameIntervalsMs[_playbackFrameIntervalHead] = intervalMs;
            _playbackFrameIntervalHead = (_playbackFrameIntervalHead + 1) % _playbackFrameIntervalsMs.Length;
            if (_playbackFrameIntervalCount < _playbackFrameIntervalsMs.Length)
            {
                _playbackFrameIntervalCount++;
            }
        }

        if (expectedFrameMs > 0 && intervalMs > expectedFrameMs * 1.5)
        {
            Interlocked.Increment(ref _playbackSlowFrameCount);
        }
    }

    private bool TryDecodeNextVideoFrameWithMetrics(
        FlashbackDecoder decoder,
        out DecodedVideoFrame frame,
        CancellationToken cancellationToken = default)
    {
        var start = Stopwatch.GetTimestamp();
        var decoded = decoder.TryDecodeNextVideoFrame(out frame, cancellationToken);
        if (decoded)
        {
            var elapsedMs = (Stopwatch.GetTimestamp() - start) * 1000.0 / Stopwatch.Frequency;
            TrackPlaybackDecodeDuration(elapsedMs, decoder.LastDecodePhaseTimings);
        }

        return decoded;
    }

    private void TrackPlaybackDecodeDuration(
        double elapsedMs,
        FlashbackDecoder.PlaybackDecodePhaseTimings phaseTimings)
    {
        if (elapsedMs <= 0 || double.IsNaN(elapsedMs) || double.IsInfinity(elapsedMs))
        {
            return;
        }

        lock (_playbackDecodeLock)
        {
            if (_playbackDecodeDurationCount == 0 ||
                elapsedMs >= _playbackMaxDecodeTotalMs)
            {
                _playbackMaxDecodeTotalMs = elapsedMs;
                _playbackMaxDecodeReceiveMs = phaseTimings.ReceiveMs;
                _playbackMaxDecodeFeedMs = phaseTimings.FeedMs;
                _playbackMaxDecodeReadMs = phaseTimings.ReadMs;
                _playbackMaxDecodeSendMs = phaseTimings.SendMs;
                _playbackMaxDecodeAudioMs = phaseTimings.AudioMs;
                _playbackMaxDecodeConvertMs = phaseTimings.ConvertMs;
                Interlocked.Exchange(ref _playbackMaxDecodeUtcUnixMs, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
                Interlocked.Exchange(ref _playbackMaxDecodePositionMs, (long)Math.Max(0, PlaybackPosition.TotalMilliseconds));
                Volatile.Write(ref _playbackMaxDecodePhase, ResolveDominantDecodePhase(phaseTimings));
            }

            _playbackDecodeDurationsMs[_playbackDecodeDurationHead] = elapsedMs;
            _playbackDecodeDurationHead = (_playbackDecodeDurationHead + 1) % _playbackDecodeDurationsMs.Length;
            if (_playbackDecodeDurationCount < _playbackDecodeDurationsMs.Length)
            {
                _playbackDecodeDurationCount++;
            }
        }
    }

    private static string ResolveDominantDecodePhase(FlashbackDecoder.PlaybackDecodePhaseTimings phaseTimings)
    {
        var phase = "receive";
        var max = phaseTimings.ReceiveMs;
        if (phaseTimings.FeedMs > max) { phase = "feed"; max = phaseTimings.FeedMs; }
        if (phaseTimings.ReadMs > max) { phase = "read"; max = phaseTimings.ReadMs; }
        if (phaseTimings.SendMs > max) { phase = "send"; max = phaseTimings.SendMs; }
        if (phaseTimings.AudioMs > max) { phase = "audio"; max = phaseTimings.AudioMs; }
        if (phaseTimings.ConvertMs > max) { phase = "convert"; }
        return phase;
    }

    private bool SeekToWithCapTelemetry(
        FlashbackDecoder decoder,
        TimeSpan seekTarget,
        string reason,
        CancellationToken cancellationToken)
    {
        Volatile.Write(ref _lastPlaybackSeekHitForwardDecodeCap, 0);
        var succeeded = decoder.SeekTo(seekTarget, cancellationToken);
        if (decoder.LastSeekHitForwardDecodeCap)
        {
            Volatile.Write(ref _lastPlaybackSeekHitForwardDecodeCap, 1);
            Interlocked.Increment(ref _playbackSeekForwardDecodeCapHits);
            Logger.Log(
                $"FLASHBACK_PLAYBACK_SEEK_FORWARD_DECODE_CAP reason={reason} " +
                $"target_ms={(long)seekTarget.TotalMilliseconds} success={succeeded}");
        }

        return succeeded;
    }

    private void ResetPlaybackMetrics()
    {
        Interlocked.Exchange(ref _playbackFrameCount, 0);
        Interlocked.Exchange(ref _playbackPreviewPresentId, 0);
        Interlocked.Exchange(ref _playbackLateFrames, 0);
        Interlocked.Exchange(ref _playbackDroppedFrames, 0);
        Interlocked.Exchange(ref _playbackAudioMasterDelayDoubles, 0);
        Interlocked.Exchange(ref _playbackAudioMasterDelayShrinks, 0);
        Interlocked.Exchange(ref _playbackAudioMasterFallbacks, 0);
        Interlocked.Exchange(ref _playbackAudioMasterUnavailableFallbacks, 0);
        Interlocked.Exchange(ref _playbackAudioMasterStaleFallbacks, 0);
        Interlocked.Exchange(ref _playbackAudioMasterDriftOutlierFallbacks, 0);
        Volatile.Write(ref _playbackAudioMasterLastFallbackReason, string.Empty);
        _playbackAudioMasterLastFallbackDriftMs = 0;
        _playbackAudioMasterLastFallbackClockAgeMs = 0;
        ClearPendingAudioMasterFallback();
        Volatile.Write(ref _lastPlaybackDropReason, string.Empty);
        Interlocked.Exchange(ref _lastPlaybackDropUtcUnixMs, 0);
        Interlocked.Exchange(ref _playbackSubmitFailures, 0);
        ClearLastSubmitFailure();
        // Reset audio clock extrapolation so stale PTS doesn't cause a jump.
        Interlocked.Exchange(ref _audioClockPtsTicks, 0);
        Interlocked.Exchange(ref _audioClockWallTicks, 0);
        _playbackTargetFps = 0;
        _playbackObservedFps = 0;
        _playbackAvgFrameMs = 0;
        Interlocked.Exchange(ref _lastPlaybackCadencePtsTicks, -1);
        Interlocked.Exchange(ref _playbackPtsCadenceMismatchCount, 0);
        Interlocked.Exchange(ref _lastPlaybackPtsCadenceMismatchUtcUnixMs, 0);
        _lastPlaybackPtsCadenceDeltaMs = 0;
        _lastPlaybackPtsCadenceExpectedMs = 0;
        Interlocked.Exchange(ref _playbackSeekForwardDecodeCapHits, 0);
        Volatile.Write(ref _lastPlaybackSeekHitForwardDecodeCap, 0);
        _playbackFpsClock.Reset();
        Interlocked.Exchange(ref _playbackSlowFrameCount, 0);
        lock (_playbackCadenceLock)
        {
            Array.Clear(_playbackFrameIntervalsMs);
            _playbackFrameIntervalHead = 0;
            _playbackFrameIntervalCount = 0;
        }

        lock (_playbackDecodeLock)
        {
            Array.Clear(_playbackDecodeDurationsMs);
            _playbackDecodeDurationHead = 0;
            _playbackDecodeDurationCount = 0;
            _playbackMaxDecodeTotalMs = 0;
            _playbackMaxDecodeReceiveMs = 0;
            _playbackMaxDecodeFeedMs = 0;
            _playbackMaxDecodeReadMs = 0;
            _playbackMaxDecodeSendMs = 0;
            _playbackMaxDecodeAudioMs = 0;
            _playbackMaxDecodeConvertMs = 0;
            Interlocked.Exchange(ref _playbackMaxDecodeUtcUnixMs, 0);
            Interlocked.Exchange(ref _playbackMaxDecodePositionMs, 0);
            Volatile.Write(ref _playbackMaxDecodePhase, string.Empty);
        }
    }


    // --- Decoder file lifecycle ---

    private static readonly TimeSpan ActiveFmp4ReopenNearLiveGuard = TimeSpan.FromMilliseconds(250);
    private static readonly TimeSpan AdjacentSegmentSeekFallbackWindow = TimeSpan.FromSeconds(3);

    private string? _currentOpenFilePath;

    private FlashbackDecoder CreateDecoder()
    {
        var useGpu = GpuDecodeEnabled;
        Logger.Log($"FLASHBACK_PLAYBACK_DECODER_CREATE gpu={useGpu}");
        var decoder = new FlashbackDecoder();

        // Get D3D11 device pointers for GPU-direct decode (skip if GPU decode disabled).
        var devicePtr = IntPtr.Zero;
        var contextPtr = IntPtr.Zero;
        if (useGpu)
        {
            var d3dManager = _videoCapture?.D3DManager;
            devicePtr = d3dManager?.Device?.NativePointer ?? IntPtr.Zero;
            contextPtr = d3dManager?.ImmediateContext?.NativePointer ?? IntPtr.Zero;
        }
        decoder.Initialize(devicePtr, contextPtr);

        RestoreAudioCallback(decoder);
        return decoder;
    }

    private void EnsureFileOpen(FlashbackDecoder decoder, ref bool fileOpen, TimeSpan? targetPts = null)
    {
        // Determine which segment file contains the target position.
        var filePath = targetPts.HasValue
            ? _bufferManager.GetValidSegmentFileForPosition(targetPts.Value)
            : _bufferManager.ActiveFilePath;

        if (string.IsNullOrWhiteSpace(filePath))
        {
            Logger.Log("FLASHBACK_PLAYBACK_NO_FILE");
            if (decoder.IsOpen)
            {
                CloseDecoderFileBestEffort(decoder, "ensure_file_open_no_file");
            }

            fileOpen = false;
            _currentOpenFilePath = null;
            _decoderHwAccel = "N/A";
            return;
        }

        // If already open on the correct file, nothing to do.
        if (fileOpen && decoder.IsOpen && IsSamePlaybackPath(filePath, _currentOpenFilePath))
            return;

        try
        {
            if (decoder.IsOpen)
            {
                CloseDecoderFileBestEffort(decoder, "ensure_file_open");
                fileOpen = false;
                _currentOpenFilePath = null;
                _decoderHwAccel = "N/A";
            }

            decoder.OpenFile(filePath);
            fileOpen = true;
            _currentOpenFilePath = filePath;
            _decoderHwAccel = decoder.IsD3D11HwAccelerated ? "D3D11VA" : "Software";
            Logger.Log($"FLASHBACK_PLAYBACK_FILE_OPEN path='{filePath}' hw_accel={_decoderHwAccel}");
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_PLAYBACK_FILE_OPEN_ERROR path='{filePath}' type={ex.GetType().Name} error='{ex.Message}'");
            if (decoder.IsOpen)
            {
                CloseDecoderFileBestEffort(decoder, "ensure_file_open_error");
            }
            _decoderHwAccel = "N/A";
            fileOpen = false;
            _currentOpenFilePath = null;
        }
    }

    private static bool IsDecoderFileReady(FlashbackDecoder decoder, bool fileOpen)
        => fileOpen && decoder.IsOpen;

    private bool TrySeekWithActiveFmp4Reopen(
        FlashbackDecoder decoder,
        ref bool fileOpen,
        TimeSpan seekTarget,
        string reason,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (SeekToWithCapTelemetry(decoder, seekTarget, reason, cancellationToken))
        {
            return true;
        }

        // Active fMP4 segment: demuxer fragment index is stale. Reopen and retry.
        // MPEG-TS handles appended data via eof_reached reset and does not need reopening.
        if (IsActiveFmp4Segment(_currentOpenFilePath) && _currentOpenFilePath != null)
        {
            if (ShouldSkipActiveFmp4ReopenNearLive(seekTarget, reason))
            {
                SetReopenFailure(reason, "near_live", seekTarget);
                return false;
            }

            return TryReopenCurrentFileAndSeek(decoder, ref fileOpen, seekTarget, reason, cancellationToken);
        }

        if (TrySeekAdjacentSegmentStart(decoder, ref fileOpen, seekTarget, reason, out _, cancellationToken))
        {
            return true;
        }

        SetReopenFailure(reason, "seek_failed", seekTarget);
        Logger.Log($"FLASHBACK_PLAYBACK_SEEK_FAIL reason={reason} offset_ms={(long)seekTarget.TotalMilliseconds}");
        return false;
    }

    private bool TryReopenCurrentFileAndSeek(
        FlashbackDecoder decoder,
        ref bool fileOpen,
        TimeSpan seekTarget,
        string reason,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var currentPath = _currentOpenFilePath;
        if (string.IsNullOrWhiteSpace(currentPath))
        {
            SetReopenFailure(reason, "no_current_file", seekTarget);
            Logger.Log($"FLASHBACK_PLAYBACK_REOPEN_SKIP reason={reason} detail=no_current_file");
            return false;
        }

        try
        {
            Logger.Log($"FLASHBACK_PLAYBACK_REOPEN reason={reason} offset_ms={(long)seekTarget.TotalMilliseconds}");
            ReopenDecoderPlaybackFile(
                decoder,
                currentPath,
                ref fileOpen,
                updateCurrentOpenPath: true,
                closeOnlyWhenOpen: true);
            cancellationToken.ThrowIfCancellationRequested();
            if (SeekToWithCapTelemetry(decoder, seekTarget, reason, cancellationToken))
            {
                return true;
            }

            SetReopenFailure(reason, "seek_failed", seekTarget);
            Logger.Log($"FLASHBACK_PLAYBACK_REOPEN_SEEK_FAIL reason={reason} path='{currentPath}' offset_ms={(long)seekTarget.TotalMilliseconds}");
            return false;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            SetReopenFailure(reason, ex.GetType().Name, seekTarget);
            Logger.Log($"FLASHBACK_PLAYBACK_REOPEN_ERROR reason={reason} path='{currentPath}' type={ex.GetType().Name} msg='{ex.Message}'");
            MarkDecoderPlaybackFileClosed(ref fileOpen);
            return false;
        }
    }

    private bool TryReopenCurrentFileAndSeekKeyframe(
        FlashbackDecoder decoder,
        ref bool fileOpen,
        TimeSpan seekTarget,
        string reason,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var currentPath = _currentOpenFilePath;
        if (string.IsNullOrWhiteSpace(currentPath))
        {
            SetReopenFailure(reason, "no_current_file", seekTarget);
            Logger.Log($"FLASHBACK_PLAYBACK_REOPEN_SKIP reason={reason} detail=no_current_file");
            return false;
        }

        try
        {
            Logger.Log($"FLASHBACK_PLAYBACK_REOPEN_KEYFRAME reason={reason} offset_ms={(long)seekTarget.TotalMilliseconds}");
            ReopenDecoderPlaybackFile(
                decoder,
                currentPath,
                ref fileOpen,
                updateCurrentOpenPath: true,
                closeOnlyWhenOpen: true);
            cancellationToken.ThrowIfCancellationRequested();
            if (decoder.SeekToKeyframe(seekTarget, cancellationToken))
            {
                return true;
            }

            SetReopenFailure(reason, "keyframe_seek_failed", seekTarget);
            Logger.Log($"FLASHBACK_PLAYBACK_REOPEN_KEYFRAME_SEEK_FAIL reason={reason} path='{currentPath}' offset_ms={(long)seekTarget.TotalMilliseconds}");
            return false;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            SetReopenFailure(reason, ex.GetType().Name, seekTarget);
            Logger.Log($"FLASHBACK_PLAYBACK_REOPEN_KEYFRAME_ERROR reason={reason} path='{currentPath}' type={ex.GetType().Name} msg='{ex.Message}'");
            MarkDecoderPlaybackFileClosed(ref fileOpen);
            return false;
        }
    }

    private bool ShouldSkipActiveFmp4ReopenNearLive(TimeSpan seekTarget, string reason)
    {
        var latestPts = _bufferManager.LatestPts;
        if (latestPts <= TimeSpan.Zero)
        {
            return false;
        }

        var distanceFromLive = seekTarget >= latestPts
            ? TimeSpan.Zero
            : latestPts - seekTarget;
        if (distanceFromLive > ActiveFmp4ReopenNearLiveGuard)
        {
            return false;
        }

        Logger.Log($"FLASHBACK_PLAYBACK_REOPEN_SKIP_NEAR_LIVE reason={reason} target_ms={(long)seekTarget.TotalMilliseconds} latest_ms={(long)latestPts.TotalMilliseconds} distance_ms={(long)distanceFromLive.TotalMilliseconds} guard_ms={(long)ActiveFmp4ReopenNearLiveGuard.TotalMilliseconds}");
        return true;
    }

    private bool TrySeekAdjacentSegmentStart(
        FlashbackDecoder decoder,
        ref bool fileOpen,
        TimeSpan seekTarget,
        string reason,
        out TimeSpan effectiveSeekTarget,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        effectiveSeekTarget = seekTarget;
        var currentPath = _currentOpenFilePath;
        if (string.IsNullOrWhiteSpace(currentPath))
        {
            return false;
        }

        var nextPath = _bufferManager.GetNextSegmentFile(currentPath);
        if (string.IsNullOrWhiteSpace(nextPath) || IsSamePlaybackPath(nextPath, currentPath))
        {
            return false;
        }

        var nextStart = _bufferManager.GetSegmentStartPts(nextPath);
        if (!nextStart.HasValue)
        {
            return false;
        }

        var targetGap = (nextStart.Value - seekTarget).Duration();
        if (targetGap > AdjacentSegmentSeekFallbackWindow)
        {
            return false;
        }

        effectiveSeekTarget = seekTarget < nextStart.Value ? nextStart.Value : seekTarget;
        try
        {
            Logger.Log(
                $"FLASHBACK_PLAYBACK_ADJACENT_SEGMENT_SEEK reason={reason} " +
                $"from='{System.IO.Path.GetFileName(currentPath)}' next='{System.IO.Path.GetFileName(nextPath)}' " +
                $"target_ms={(long)seekTarget.TotalMilliseconds} effective_ms={(long)effectiveSeekTarget.TotalMilliseconds}");
            ReopenDecoderPlaybackFile(
                decoder,
                nextPath,
                ref fileOpen,
                updateCurrentOpenPath: true,
                closeOnlyWhenOpen: true);
            cancellationToken.ThrowIfCancellationRequested();
            if (SeekToWithCapTelemetry(decoder, effectiveSeekTarget, reason, cancellationToken))
            {
                Interlocked.Increment(ref _playbackSegmentSwitches);
                Interlocked.Exchange(ref _lastSegmentSwitchUtcUnixMs, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
                ResetPlaybackPtsCadenceBaseline();
                return true;
            }

            SetReopenFailure(reason, "adjacent_seek_failed", effectiveSeekTarget);
            Logger.Log($"FLASHBACK_PLAYBACK_ADJACENT_SEGMENT_SEEK_FAIL reason={reason} path='{nextPath}' offset_ms={(long)effectiveSeekTarget.TotalMilliseconds}");
            return false;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            SetReopenFailure(reason, ex.GetType().Name, effectiveSeekTarget);
            Logger.Log($"FLASHBACK_PLAYBACK_ADJACENT_SEGMENT_SEEK_ERROR reason={reason} path='{nextPath}' type={ex.GetType().Name} msg='{ex.Message}'");
            MarkDecoderPlaybackFileClosed(ref fileOpen);
            return false;
        }
    }

    private void ReopenDecoderPlaybackFile(
        FlashbackDecoder decoder,
        string path,
        ref bool fileOpen,
        bool updateCurrentOpenPath,
        bool closeOnlyWhenOpen)
    {
        if (!closeOnlyWhenOpen || decoder.IsOpen)
        {
            decoder.CloseFile();
        }

        fileOpen = false;
        decoder.OpenFile(path);
        fileOpen = true;
        if (updateCurrentOpenPath)
        {
            _currentOpenFilePath = path;
        }

        _decoderHwAccel = decoder.IsD3D11HwAccelerated ? "D3D11VA" : "Software";
    }

    private void MarkDecoderPlaybackFileClosed(ref bool fileOpen)
    {
        _decoderHwAccel = "N/A";
        fileOpen = false;
        _currentOpenFilePath = null;
    }

    private static void CloseDecoderFileBestEffort(FlashbackDecoder decoder, string operation)
    {
        try
        {
            if (decoder.IsOpen) decoder.CloseFile();
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_PLAYBACK_DECODER_CLOSE_WARN op={operation} type={ex.GetType().Name} msg='{ex.Message}'");
        }
    }

    private void CleanupDecoder(ref FlashbackDecoder? decoder, ref bool fileOpen)
    {
        var cleanupStarted = Stopwatch.GetTimestamp();
        var wasOpen = decoder?.IsOpen ?? false;
        Logger.Log($"FLASHBACK_PLAYBACK_DECODER_CLEANUP was_open={wasOpen}");
        var releaseStarted = Stopwatch.GetTimestamp();
        ReleasePreviousHeldFrame();
        var releaseMs = Stopwatch.GetElapsedTime(releaseStarted).TotalMilliseconds;
        var closeMs = 0d;
        var disposeMs = 0d;
        if (decoder != null)
        {
            var decoderToDispose = decoder;
            decoder = null;
            try
            {
                if (decoderToDispose.IsOpen)
                {
                    var closeStarted = Stopwatch.GetTimestamp();
                    decoderToDispose.CloseFile();
                    closeMs = Stopwatch.GetElapsedTime(closeStarted).TotalMilliseconds;
                }
            }
            catch (Exception ex)
            {
                closeMs = Stopwatch.GetElapsedTime(cleanupStarted).TotalMilliseconds;
                Logger.Log($"FLASHBACK_PLAYBACK_DECODER_CLEANUP_WARN op=close type={ex.GetType().Name} msg='{ex.Message}'");
            }

            try
            {
                var disposeStarted = Stopwatch.GetTimestamp();
                decoderToDispose.Dispose();
                disposeMs = Stopwatch.GetElapsedTime(disposeStarted).TotalMilliseconds;
            }
            catch (Exception ex)
            {
                disposeMs = Stopwatch.GetElapsedTime(cleanupStarted).TotalMilliseconds;
                Logger.Log($"FLASHBACK_PLAYBACK_DECODER_CLEANUP_WARN op=dispose type={ex.GetType().Name} msg='{ex.Message}'");
            }
        }
        fileOpen = false;
        _currentOpenFilePath = null;
        _decoderHwAccel = "N/A";
        var totalMs = Stopwatch.GetElapsedTime(cleanupStarted).TotalMilliseconds;
        Logger.Log(
            $"FLASHBACK_PLAYBACK_DECODER_CLEANUP_COMPLETE was_open={wasOpen} " +
            $"release_ms={releaseMs:0.###} close_ms={closeMs:0.###} dispose_ms={disposeMs:0.###} total_ms={totalMs:0.###}");
    }

    // --- In/Out points and playback position mapping ---
    // --- In/Out points ---

    private long _inPointTicks = long.MinValue;
    private long _outPointTicks = long.MinValue;
    private long _inPointFilePtsTicks = long.MinValue;
    private long _outPointFilePtsTicks = long.MinValue;

    public TimeSpan? InPoint
    {
        get
        {
            var t = Interlocked.Read(ref _inPointTicks);
            return t == long.MinValue ? null : TimeSpan.FromTicks(t);
        }
        set
        {
            var normalized = value.HasValue ? NormalizeMarkerPosition(value.Value) : (TimeSpan?)null;
            Interlocked.Exchange(ref _inPointTicks, normalized?.Ticks ?? long.MinValue);
            Interlocked.Exchange(ref _inPointFilePtsTicks, normalized.HasValue ? SaturatingAdd(normalized.Value, _bufferManager.ValidStartPts).Ticks : long.MinValue);
        }
    }

    public TimeSpan? OutPoint
    {
        get
        {
            var t = Interlocked.Read(ref _outPointTicks);
            return t == long.MinValue ? null : TimeSpan.FromTicks(t);
        }
        set
        {
            var normalized = value.HasValue ? NormalizeMarkerPosition(value.Value) : (TimeSpan?)null;
            Interlocked.Exchange(ref _outPointTicks, normalized?.Ticks ?? long.MinValue);
            Interlocked.Exchange(ref _outPointFilePtsTicks, normalized.HasValue ? SaturatingAdd(normalized.Value, _bufferManager.ValidStartPts).Ticks : long.MinValue);
        }
    }

    public TimeSpan? InPointFilePts
    {
        get
        {
            var t = Interlocked.Read(ref _inPointFilePtsTicks);
            return t == long.MinValue ? null : TimeSpan.FromTicks(t);
        }
    }

    public TimeSpan? OutPointFilePts
    {
        get
        {
            var t = Interlocked.Read(ref _outPointFilePtsTicks);
            return t == long.MinValue ? null : TimeSpan.FromTicks(t);
        }
    }

    public TimeSpan SetInPoint() => SetInPointAt(null);

    /// <summary>
    /// Pin the in-point at an explicit user-intended position rather than the
    /// controller's last decoded keyframe. The UI should pass the position the
    /// user is visually pointing at (its FlashbackPlaybackPosition), which during
    /// scrubbing is the user's drag target rather than the keyframe-snapped
    /// PlaybackPosition the controller publishes after each decode. Without this
    /// overload, mid-GOP "click In" landed on the prior keyframe and the marker
    /// could appear hundreds of milliseconds before where the playhead sat.
    /// </summary>
    public TimeSpan SetInPointAt(TimeSpan position) => SetInPointAt((TimeSpan?)position);

    private TimeSpan SetInPointAt(TimeSpan? overridePosition)
    {
        if (_disposedFlag != 0)
        {
            SetLastCommandFailure("disposed:SetInPoint");
            Logger.Log("FLASHBACK_PLAYBACK_SET_IN_SKIP reason=disposed");
            return PlaybackPosition;
        }

        var pos = overridePosition.HasValue
            ? NormalizeMarkerPosition(overridePosition.Value)
            : PlaybackPosition;
        ClearLastCommandFailure();
        InPoint = pos;
        var outTicks = Interlocked.Read(ref _outPointTicks);
        if (outTicks != long.MinValue && outTicks <= pos.Ticks)
        {
            OutPoint = null;
            Logger.Log("FLASHBACK_PLAYBACK_CLEAR_OUT invalid_range");
        }

        Logger.Log($"FLASHBACK_PLAYBACK_SET_IN pos_ms={(long)pos.TotalMilliseconds} source={(overridePosition.HasValue ? "ui_override" : "playback")}");
        return pos;
    }

    public TimeSpan SetOutPoint() => SetOutPointAt(null);

    /// <summary>
    /// Pin the out-point at an explicit user-intended position. See
    /// <see cref="SetInPointAt(TimeSpan)"/> for the rationale: the UI's visual
    /// playhead and the controller's keyframe-snapped PlaybackPosition can
    /// differ by hundreds of milliseconds during scrubbing.
    /// </summary>
    public TimeSpan SetOutPointAt(TimeSpan position) => SetOutPointAt((TimeSpan?)position);

    private TimeSpan SetOutPointAt(TimeSpan? overridePosition)
    {
        if (_disposedFlag != 0)
        {
            SetLastCommandFailure("disposed:SetOutPoint");
            Logger.Log("FLASHBACK_PLAYBACK_SET_OUT_SKIP reason=disposed");
            return PlaybackPosition;
        }

        var pos = overridePosition.HasValue
            ? NormalizeMarkerPosition(overridePosition.Value)
            : PlaybackPosition;
        ClearLastCommandFailure();
        OutPoint = pos;
        var inTicks = Interlocked.Read(ref _inPointTicks);
        if (inTicks != long.MinValue && inTicks >= pos.Ticks)
        {
            InPoint = null;
            Logger.Log("FLASHBACK_PLAYBACK_CLEAR_IN invalid_range");
        }

        Logger.Log($"FLASHBACK_PLAYBACK_SET_OUT pos_ms={(long)pos.TotalMilliseconds} source={(overridePosition.HasValue ? "ui_override" : "playback")}");
        return pos;
    }

    public void ClearInOutPoints()
    {
        if (_disposedFlag != 0)
        {
            SetLastCommandFailure("disposed:ClearInOutPoints");
            Logger.Log("FLASHBACK_PLAYBACK_CLEAR_INOUT_SKIP reason=disposed");
            return;
        }

        InPoint = null;
        OutPoint = null;
        ClearLastCommandFailure();
        Logger.Log("FLASHBACK_PLAYBACK_CLEAR_INOUT");
    }

    public void RestoreInOutPoints(
        TimeSpan? inPoint,
        TimeSpan? outPoint,
        TimeSpan? inPointFilePts,
        TimeSpan? outPointFilePts)
    {
        InPoint = inPoint;
        OutPoint = outPoint;

        if (inPoint.HasValue && inPointFilePts.HasValue && inPointFilePts.Value >= TimeSpan.Zero)
        {
            Interlocked.Exchange(ref _inPointFilePtsTicks, inPointFilePts.Value.Ticks);
        }

        if (outPoint.HasValue && outPointFilePts.HasValue && outPointFilePts.Value >= TimeSpan.Zero)
        {
            Interlocked.Exchange(ref _outPointFilePtsTicks, outPointFilePts.Value.Ticks);
        }
    }

    private bool CheckOutPoint(TimeSpan position, Stopwatch pacingStopwatch)
    {
        var outTicks = Interlocked.Read(ref _outPointTicks);
        if (outTicks != long.MinValue && position >= TimeSpan.FromTicks(outTicks))
        {
            Logger.Log($"FLASHBACK_PLAYBACK_HIT_OUTPOINT pos_ms={(long)position.TotalMilliseconds}");
            SafePauseRendering("out_point");
            pacingStopwatch.Stop();
            SetState(FlashbackPlaybackState.Paused);
            return true;
        }

        return false;
    }

    private TimeSpan NormalizeMarkerPosition(TimeSpan position)
    {
        if (position <= TimeSpan.Zero)
        {
            return TimeSpan.Zero;
        }

        var bufferDuration = _bufferManager.BufferedDuration;
        return position > bufferDuration ? bufferDuration : position;
    }

    /// <summary>
    /// Returns true if the given path is the active fMP4 segment. The reopen-and-retry
    /// workaround only applies to fMP4 (fragment index goes stale); transport streams
    /// handle appended data via eof_reached reset and don't need reopening.
    /// </summary>
    private bool IsActiveFmp4Segment(string? path)
        => path != null
        && IsSamePlaybackPath(path, _bufferManager.ActiveFilePath)
        && path.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase);

    private TimeSpan ClampPosition(TimeSpan position) => ClampPosition(position, null);

    /// <summary>
    /// Clamp a scrub/seek position to the currently usable buffer range, optionally
    /// account for segment eviction that has happened since a scrub session captured
    /// its frozen reference. Without the eviction adjustment, a long-held scrub at
    /// position 0 maps via SaturatingAdd(pos, frozenValidStart) to a file PTS that
    /// has been evicted - EnsureFileOpen fails and the user gets a sudden snap-to-
    /// live instead of clamping to the new oldest available position.
    /// </summary>
    private TimeSpan ClampPosition(TimeSpan position, TimeSpan? frozenValidStart)
    {
        var bufferDuration = _bufferManager.BufferedDuration;
        var inTicks = Interlocked.Read(ref _inPointTicks);
        var min = inTicks == long.MinValue ? TimeSpan.Zero : TimeSpan.FromTicks(inTicks);
        var outTicks = Interlocked.Read(ref _outPointTicks);
        var max = outTicks == long.MinValue ? bufferDuration : TimeSpan.FromTicks(outTicks);
        if (max > bufferDuration) max = bufferDuration;
        if (frozenValidStart.HasValue)
        {
            // Eviction may have advanced ValidStartPts past the scrub session's
            // captured reference. Positions in the evicted gap (in scrub coords)
            // would resolve to file PTS values whose segments no longer exist.
            // Promote min so those positions clamp up to the new oldest valid
            // position rather than failing the file lookup downstream.
            var currentValidStart = _bufferManager.ValidStartPts;
            if (currentValidStart > frozenValidStart.Value)
            {
                var evictedDelta = currentValidStart - frozenValidStart.Value;
                if (evictedDelta > min)
                {
                    min = evictedDelta;
                }
            }
        }
        if (min > max) min = max;
        if (position < min) return min;
        if (position > max) return max;
        return position;
    }

    private static TimeSpan SaturatingAdd(TimeSpan left, TimeSpan right)
    {
        var leftTicks = left.Ticks;
        var rightTicks = right.Ticks;
        if (rightTicks > 0 && leftTicks > long.MaxValue - rightTicks)
            return TimeSpan.MaxValue;
        if (rightTicks < 0 && leftTicks < long.MinValue - rightTicks)
            return TimeSpan.MinValue;
        return TimeSpan.FromTicks(leftTicks + rightTicks);
    }

    private static TimeSpan SaturatingSubtract(TimeSpan left, TimeSpan right)
    {
        var leftTicks = left.Ticks;
        var rightTicks = right.Ticks;
        if (rightTicks < 0 && leftTicks > long.MaxValue + rightTicks)
            return TimeSpan.MaxValue;
        if (rightTicks > 0 && leftTicks < long.MinValue + rightTicks)
            return TimeSpan.MinValue;
        return TimeSpan.FromTicks(leftTicks - rightTicks);
    }

    private static bool IsSamePlaybackPath(string? left, string? right)
    {
        if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
        {
            return false;
        }

        try
        {
            return string.Equals(
                Path.GetFullPath(left),
                Path.GetFullPath(right),
                StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_PLAYBACK_PATH_COMPARE_WARN left='{left}' right='{right}' type={ex.GetType().Name} msg='{ex.Message}'");
            return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
        }
    }

    // --- Audio routing and prebuffer ---

    private const double PlaybackAudioPrebufferTargetMs = 180.0;
    private const double PlaybackAudioPrebufferDiscardThresholdMs = 250.0;
    private const int PlaybackAudioPrebufferTimeoutMs = 1000;
    private const int PlaybackAudioPrebufferRetryDelayMs = 20;
    private const int PlaybackAudioPrebufferDecodeFrameBudget = 96;
    // Cap on decoded video frames held across the audio prebuffer. CPU frames only:
    // a D3D11VA frame pins a decoder-pool surface, and pool depth is not guaranteed
    // to cover the prebuffer budget, so hardware frames keep the release+rewind path.
    private const int PlaybackAudioPrebufferMaxHeldFrames = 32;
    private const int AudioRenderStateTransitionTimeoutMs = 100;
    // Must stay strictly greater than ActiveFmp4ReopenNearLiveGuard (250ms):
    // clamped targets at exactly the guard distance would fall inside the
    // skip-reopen zone and lose the stale-fragment-index recovery path.
    private static readonly TimeSpan MinimumPlaybackLiveLead =
        TimeSpan.FromMilliseconds(Math.Max(300.0, PlaybackAudioPrebufferTargetMs));

    private void ApplyAudioRoutingForState(string operation)
    {
        if (_disposedFlag != 0)
        {
            return;
        }

        switch (_state)
        {
            case FlashbackPlaybackState.Live:
                RestoreLiveAudio();
                break;
            case FlashbackPlaybackState.Playing:
                SuppressLiveAudio();
                SafeResumeRendering(operation);
                break;
            case FlashbackPlaybackState.Paused:
            case FlashbackPlaybackState.Scrubbing:
                SuppressLiveAudio();
                SafePauseRendering(operation);
                break;
        }
    }

    private void ApplyPreviewRoutingForState(string operation)
    {
        if (_disposedFlag != 0)
        {
            return;
        }

        if (_state == FlashbackPlaybackState.Live)
        {
            SafeResumePreviewSubmission(operation);
        }
        else
        {
            SafeSuppressPreviewSubmission(operation);
        }
    }

    private void SuppressLiveAudio()
    {
        try
        {
            _audioCapture?.SetPlayback(null);
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_PLAYBACK_AUDIO_WARN op=suppress_live_set_playback type={ex.GetType().Name} msg='{ex.Message}'");
        }

        SafeFlushPlayback("suppress_live_audio");
    }

    private void RestoreLiveAudio()
    {
        SafeFlushPlayback("restore_live_audio");
        // Reconnect audio feed before resuming rendering to avoid silence/stutter.
        try
        {
            if (_audioCapture != null && _audioPlayback != null)
            {
                _audioCapture.SetPlayback(_audioPlayback);
            }
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_PLAYBACK_AUDIO_WARN op=restore_live_set_playback type={ex.GetType().Name} msg='{ex.Message}'");
        }

        SafeResumeRendering("restore_live_audio");
    }

    private void SafeSuppressPreviewSubmission(string operation)
    {
        try
        {
            _videoCapture?.SuppressPreviewSubmission();
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_PLAYBACK_PREVIEW_WARN op=suppress operation={operation} type={ex.GetType().Name} msg='{ex.Message}'");
        }
    }

    private void SafeResumePreviewSubmission(string operation)
    {
        try
        {
            _videoCapture?.ResumePreviewSubmission();
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_PLAYBACK_PREVIEW_WARN op=resume operation={operation} type={ex.GetType().Name} msg='{ex.Message}'");
        }
    }

    private void SafePauseRendering(string operation)
    {
        try
        {
            var audioPlayback = _audioPlayback;
            audioPlayback?.PauseRendering();
            if (audioPlayback != null &&
                !audioPlayback.WaitForRenderingPaused(AudioRenderStateTransitionTimeoutMs))
            {
                Logger.Log(
                    $"FLASHBACK_PLAYBACK_AUDIO_RENDER_STATE_TIMEOUT op=pause operation={operation} " +
                    $"expected=paused timeout_ms={AudioRenderStateTransitionTimeoutMs}");
            }
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_PLAYBACK_AUDIO_WARN op=pause operation={operation} type={ex.GetType().Name} msg='{ex.Message}'");
        }
    }

    private void SafeResumeRendering(string operation)
    {
        try
        {
            var audioPlayback = _audioPlayback;
            audioPlayback?.ResumeRendering();
            if (audioPlayback != null &&
                !audioPlayback.WaitForRenderingRunning(AudioRenderStateTransitionTimeoutMs))
            {
                Logger.Log(
                    $"FLASHBACK_PLAYBACK_AUDIO_RENDER_STATE_TIMEOUT op=resume operation={operation} " +
                    $"expected=running timeout_ms={AudioRenderStateTransitionTimeoutMs}");
            }
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_PLAYBACK_AUDIO_WARN op=resume operation={operation} type={ex.GetType().Name} msg='{ex.Message}'");
        }
    }

    private void SafeResumePlaybackRendering(string operation)
    {
        try
        {
            var audioPlayback = _audioPlayback;
            audioPlayback?.ResumeRendering(
                PlaybackAudioPrebufferTargetMs,
                PlaybackAudioPrebufferTimeoutMs);
            if (audioPlayback != null &&
                !audioPlayback.WaitForRenderingRunning(AudioRenderStateTransitionTimeoutMs))
            {
                Logger.Log(
                    $"FLASHBACK_PLAYBACK_AUDIO_RENDER_STATE_TIMEOUT op=resume_playback operation={operation} " +
                    $"expected=running timeout_ms={AudioRenderStateTransitionTimeoutMs}");
            }
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_PLAYBACK_AUDIO_WARN op=resume_playback operation={operation} type={ex.GetType().Name} msg='{ex.Message}'");
        }
    }

    private void SafeFlushPlayback(string operation)
    {
        try
        {
            _audioPlayback?.Flush();
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_PLAYBACK_AUDIO_WARN op=flush operation={operation} type={ex.GetType().Name} msg='{ex.Message}'");
        }
    }

    private TimeSpan ClampPlaybackTargetToMinimumLiveLead(
        TimeSpan target,
        TimeSpan frozenValidStart,
        string operation)
    {
        var latestPts = _bufferManager.LatestPts;
        if (latestPts <= frozenValidStart ||
            latestPts - frozenValidStart <= MinimumPlaybackLiveLead)
        {
            return target;
        }

        var latestSafeTarget = latestPts - MinimumPlaybackLiveLead;
        if (latestSafeTarget < frozenValidStart)
        {
            latestSafeTarget = frozenValidStart;
        }

        if (target <= latestSafeTarget)
        {
            return target;
        }

        Logger.Log(
            $"FLASHBACK_PLAYBACK_LIVE_LEAD_CLAMP operation={operation} " +
            $"target_ms={(long)target.TotalMilliseconds} clamped_ms={(long)latestSafeTarget.TotalMilliseconds} " +
            $"latest_ms={(long)latestPts.TotalMilliseconds} lead_ms={(long)MinimumPlaybackLiveLead.TotalMilliseconds}");
        return latestSafeTarget;
    }

    private void PrimePlaybackAudioBuffer(
        FlashbackDecoder decoder,
        Queue<DecodedVideoFrame> prebufferedFrames,
        Channel<PlaybackCommand> commandChannel,
        ref bool fileOpen,
        TimeSpan resumeTarget,
        string operation,
        CancellationToken cancellationToken,
        bool logResult = true)
    {
        var audioPlayback = _audioPlayback;
        if (audioPlayback == null)
        {
            return;
        }

        var start = Stopwatch.GetTimestamp();
        var decodedFrames = 0;
        var timedOut = false;
        var reachedEnd = false;
        var eofRetries = 0;
        var skippedForSoftwareBudget = false;
        var discarded = false;
        var rewound = false;
        var releasedAnyFrame = false;
        var prebufferReleasedFrames = 0;
        var prebufferAudioGateTicks = 0L;
        var commandPending = false;
        var pendingCommandKind = CommandKind.Stop;

        while (decodedFrames < PlaybackAudioPrebufferDecodeFrameBudget)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (commandChannel.Reader.TryPeek(out var pendingCommand))
            {
                commandPending = true;
                pendingCommandKind = pendingCommand.Kind;
                break;
            }

            if (audioPlayback.PlaybackBufferedDurationMs >= PlaybackAudioPrebufferTargetMs)
            {
                break;
            }

            var elapsedMs = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
            if (elapsedMs >= PlaybackAudioPrebufferTimeoutMs)
            {
                timedOut = true;
                break;
            }

            if (ShouldSnapLiveForSoftwarePlaybackBudget(decoder, out _, out _))
            {
                skippedForSoftwareBudget = true;
                break;
            }

            if (!TryDecodeNextVideoFrameWithMetrics(decoder, out var frame, cancellationToken))
            {
                reachedEnd = true;
                var waitMs = Math.Min(
                    PlaybackAudioPrebufferRetryDelayMs,
                    Math.Max(1, PlaybackAudioPrebufferTimeoutMs - (int)Stopwatch.GetElapsedTime(start).TotalMilliseconds));
                eofRetries++;
                if (cancellationToken.WaitHandle.WaitOne(waitMs))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    break;
                }

                continue;
            }

            decodedFrames++;
            if (!releasedAnyFrame &&
                !frame.IsD3D11Texture &&
                prebufferedFrames.Count < PlaybackAudioPrebufferMaxHeldFrames)
            {
                prebufferedFrames.Enqueue(frame);
            }
            else
            {
                if (!releasedAnyFrame && prebufferedFrames.Count > 0)
                {
                    // Cap hit (or hw frame appeared): fall back wholesale to the
                    // rewind path. All-or-nothing — a partial kept queue plus a
                    // forward decoder position would leave a hole in the middle.
                    ClearPrebufferedFrames(prebufferedFrames, $"prebuffer_cap_{operation}");
                }
                releasedAnyFrame = true;
                ReleaseHeldFrameBestEffort(frame, $"audio_prebuffer_{operation}");
                prebufferReleasedFrames++;
            }

            if (Stopwatch.GetElapsedTime(start).TotalMilliseconds >= PlaybackAudioPrebufferTimeoutMs)
            {
                timedOut = true;
                break;
            }
        }

        var bufferedMs = audioPlayback.PlaybackBufferedDurationMs;
        prebufferAudioGateTicks = Interlocked.Read(ref _lastAudioPtsTicks);
        if (bufferedMs > PlaybackAudioPrebufferDiscardThresholdMs)
        {
            // Discard releases any frames kept above, so the rewind must run
            // even if none were released in the main loop above.
            releasedAnyFrame = true;
            ClearPrebufferedFrames(prebufferedFrames, $"prebuffer_discard_{operation}");
            try
            {
                audioPlayback.Flush();
            }
            catch (Exception ex)
            {
                Logger.Log($"FLASHBACK_PLAYBACK_AUDIO_WARN op=prebuffer_discard_flush operation={operation} type={ex.GetType().Name} msg='{ex.Message}'");
            }

            bufferedMs = audioPlayback.PlaybackBufferedDurationMs;
            prebufferAudioGateTicks = 0;
            discarded = true;
        }

        if (releasedAnyFrame && decodedFrames > 0)
        {
            rewound = TryRewindPlaybackAudioPrebuffer(decoder, ref fileOpen, resumeTarget, operation, prebufferAudioGateTicks, cancellationToken);
        }

        if (logResult || timedOut || reachedEnd || skippedForSoftwareBudget)
        {
            Logger.Log(
                $"FLASHBACK_PLAYBACK_AUDIO_PREBUFFER operation={operation} frames={decodedFrames} released_frames={prebufferReleasedFrames} buffered_ms={bufferedMs:F1} target_ms={PlaybackAudioPrebufferTargetMs:F1} discard_threshold_ms={PlaybackAudioPrebufferDiscardThresholdMs:F1} audio_gate_ms={(long)TimeSpan.FromTicks(Math.Max(0, prebufferAudioGateTicks)).TotalMilliseconds} elapsed_ms={Stopwatch.GetElapsedTime(start).TotalMilliseconds:F1} timed_out={timedOut} eos={reachedEnd} eof_retries={eofRetries} command_pending={commandPending} pending_command={pendingCommandKind} software_budget={skippedForSoftwareBudget} discarded={discarded} rewound={rewound} released_any={releasedAnyFrame} held={prebufferedFrames.Count}");
        }
    }

    private bool TryRewindPlaybackAudioPrebuffer(
        FlashbackDecoder decoder,
        ref bool fileOpen,
        TimeSpan resumeTarget,
        string operation,
        long prebufferAudioGateTicks,
        CancellationToken cancellationToken)
    {
        try
        {
            decoder.AudioChunkCallback = null;
            cancellationToken.ThrowIfCancellationRequested();
            var audioGateTicks = Math.Max(resumeTarget.Ticks, Math.Max(0, prebufferAudioGateTicks));
            if (!TrySeekWithActiveFmp4Reopen(decoder, ref fileOpen, resumeTarget, $"prebuffer_discard_{operation}", cancellationToken))
            {
                Logger.Log($"FLASHBACK_PLAYBACK_AUDIO_PREBUFFER_REWIND_FAIL operation={operation} target_ms={(long)resumeTarget.TotalMilliseconds}");
                RestoreAudioCallback(decoder, audioGateTicks, prebufferAudioGateTicks);
                return false;
            }

            RestoreAudioCallback(decoder, audioGateTicks, prebufferAudioGateTicks);
            Logger.Log($"FLASHBACK_PLAYBACK_AUDIO_PREBUFFER_REWIND operation={operation} target_ms={(long)resumeTarget.TotalMilliseconds} audio_gate_ms={(long)TimeSpan.FromTicks(Math.Max(0, prebufferAudioGateTicks)).TotalMilliseconds}");
            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_PLAYBACK_AUDIO_WARN op=prebuffer_rewind operation={operation} target_ms={(long)resumeTarget.TotalMilliseconds} type={ex.GetType().Name} msg='{ex.Message}'");
            var audioGateTicks = Math.Max(resumeTarget.Ticks, Math.Max(0, prebufferAudioGateTicks));
            RestoreAudioCallback(decoder, audioGateTicks, prebufferAudioGateTicks);
            return false;
        }
    }

    private void RestoreAudioCallback(
        FlashbackDecoder decoder,
        long audioStartGateTicks = 0,
        long lastAcceptedAudioPtsTicks = 0)
    {
        // Audio start gate: drop any audio chunk with PTS before this value.
        // This filters stale audio from keyframe-to-target decode after a seek.
        var normalizedLastAcceptedAudioPtsTicks = Math.Max(0, lastAcceptedAudioPtsTicks);
        var videoPtsGate = audioStartGateTicks > 0
            ? Math.Max(audioStartGateTicks, normalizedLastAcceptedAudioPtsTicks)
            : Interlocked.Read(ref _lastVideoPtsTicks);
        Interlocked.Exchange(ref _lastAudioPtsTicks, normalizedLastAcceptedAudioPtsTicks);

        if (_audioPlayback == null)
        {
            decoder.AudioChunkCallback = null;
            return;
        }

        decoder.AudioChunkCallback = chunk =>
        {
            var pb = _audioPlayback;
            if (pb == null)
            {
                ReturnPlaybackAudioChunkBestEffort(chunk, "playback_missing_audio_sink");
                return;
            }

            if (!TryValidatePlaybackAudioChunk(chunk, out var invalidReason))
            {
                Logger.Log($"FLASHBACK_PLAYBACK_AUDIO_DROP reason={invalidReason} pts_ms={(long)chunk.Pts.TotalMilliseconds} valid_bytes={chunk.ValidLength} buffer_bytes={chunk.Samples?.Length ?? 0}");
                ReturnPlaybackAudioChunkBestEffort(chunk, $"playback_audio_{invalidReason}");
                return;
            }

            // Skip invalid or non-monotonic PTS (L8 fix).
            var prevPts = Interlocked.Read(ref _lastAudioPtsTicks);
            if (chunk.Pts.Ticks <= 0 || chunk.Pts.Ticks <= prevPts)
            {
                ReturnPlaybackAudioChunkBestEffort(chunk, "playback_audio_non_monotonic_pts");
                return;
            }

            // Skip audio from the keyframe-to-target forward decode after a seek.
            if (videoPtsGate > 0 && chunk.Pts.Ticks < videoPtsGate)
            {
                ReturnPlaybackAudioChunkBestEffort(chunk, "playback_audio_before_gate");
                return;
            }

            Interlocked.Exchange(ref _lastAudioPtsTicks, chunk.Pts.Ticks);
            pb.EnqueuePooledSamples(chunk.Samples, chunk.ValidLength, chunk.Pts.Ticks);
        };
    }

    private static bool TryValidatePlaybackAudioChunk(DecodedAudioChunk chunk, out string reason)
    {
        if (chunk.Samples == null)
        {
            reason = "null_samples";
            return false;
        }

        if (chunk.ValidLength <= 0)
        {
            reason = "invalid_length";
            return false;
        }

        if (chunk.ValidLength > chunk.Samples.Length)
        {
            reason = "length_exceeds_buffer";
            return false;
        }

        const int playbackAudioBlockAlign = 2 * sizeof(float);
        if (chunk.ValidLength % playbackAudioBlockAlign != 0)
        {
            reason = "unaligned_length";
            return false;
        }

        reason = string.Empty;
        return true;
    }

    private static void ReturnPlaybackAudioChunkBestEffort(DecodedAudioChunk chunk, string operation)
    {
        try
        {
            if (chunk.Samples is { Length: > 0 })
            {
                ArrayPool<byte>.Shared.Return(chunk.Samples);
            }
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_PLAYBACK_AUDIO_RETURN_WARN op={operation} type={ex.GetType().Name} msg='{ex.Message}'");
        }
    }

    // Last sampled audio rendering PTS plus the wall-clock anchor used for
    // extrapolated audio-master pacing between WASAPI render callbacks.
    private long _audioClockPtsTicks;
    private long _audioClockWallTicks;

    private long _playbackAudioMasterDelayDoubles;
    private long _playbackAudioMasterDelayShrinks;
    private long _playbackAudioMasterFallbacks;
    private long _playbackAudioMasterUnavailableFallbacks;
    private long _playbackAudioMasterStaleFallbacks;
    private long _playbackAudioMasterDriftOutlierFallbacks;
    private string _playbackAudioMasterLastFallbackReason = string.Empty;
    private double _playbackAudioMasterLastFallbackDriftMs;
    private double _playbackAudioMasterLastFallbackClockAgeMs;
    private string _pendingAudioMasterFallbackReason = string.Empty;
    private double _pendingAudioMasterFallbackDriftMs;
    private long _pendingAudioMasterFallbackClockAgeTicks;

    private const long AudioMasterClockStaleThresholdTicks = TimeSpan.TicksPerMillisecond * 200;

    public long PlaybackAudioMasterDelayDoubles => Interlocked.Read(ref _playbackAudioMasterDelayDoubles);
    public long PlaybackAudioMasterDelayShrinks => Interlocked.Read(ref _playbackAudioMasterDelayShrinks);
    public long PlaybackAudioMasterFallbacks => Interlocked.Read(ref _playbackAudioMasterFallbacks);
    public long PlaybackAudioMasterUnavailableFallbacks => Interlocked.Read(ref _playbackAudioMasterUnavailableFallbacks);
    public long PlaybackAudioMasterStaleFallbacks => Interlocked.Read(ref _playbackAudioMasterStaleFallbacks);
    public long PlaybackAudioMasterDriftOutlierFallbacks => Interlocked.Read(ref _playbackAudioMasterDriftOutlierFallbacks);
    public string PlaybackAudioMasterLastFallbackReason => Volatile.Read(ref _playbackAudioMasterLastFallbackReason);
    public double PlaybackAudioMasterLastFallbackDriftMs => _playbackAudioMasterLastFallbackDriftMs;
    public double PlaybackAudioMasterLastFallbackClockAgeMs => _playbackAudioMasterLastFallbackClockAgeMs;

    /// <summary>
    /// Audio-video drift in milliseconds. Positive = audio ahead, negative = audio behind.
    /// Uses the PTS of the chunk WASAPI is currently rendering (not just enqueued).
    /// </summary>
    public double AvDriftMs
    {
        get
        {
            var renderingPts = _audioPlayback?.RenderingPtsTicks ?? 0;
            var videoPts = Interlocked.Read(ref _lastVideoPtsTicks);
            if (renderingPts == 0 || videoPts == 0) return 0;
            return TimeSpan.FromTicks(renderingPts - videoPts).TotalMilliseconds;
        }
    }

    // --- Audio-master playback pacing ---

    private void RefreshAudioMasterClock()
    {
        var audioPb = _audioPlayback;
        var renderingPts = audioPb?.RenderingPtsTicks ?? 0;
        if (renderingPts > 0 && renderingPts != Volatile.Read(ref _audioClockPtsTicks))
        {
            Interlocked.Exchange(ref _audioClockPtsTicks, renderingPts);
            Interlocked.Exchange(ref _audioClockWallTicks, Stopwatch.GetTimestamp());
        }
    }

    private bool TryGetFreshAudioMasterClock(
        out long extrapolatedAudioTicks,
        out long wallElapsedTicks,
        out bool hasAudioClockSample)
    {
        RefreshAudioMasterClock();

        var audioClockPts = Volatile.Read(ref _audioClockPtsTicks);
        hasAudioClockSample = audioClockPts > 0;
        extrapolatedAudioTicks = 0;
        wallElapsedTicks = 0;
        if (!hasAudioClockSample)
        {
            return false;
        }

        var audioClockWall = Volatile.Read(ref _audioClockWallTicks);
        var wallElapsed = Stopwatch.GetTimestamp() - audioClockWall;
        wallElapsedTicks = (long)((double)wallElapsed / Stopwatch.Frequency * TimeSpan.TicksPerSecond);
        if (wallElapsedTicks > AudioMasterClockStaleThresholdTicks)
        {
            return false;
        }

        extrapolatedAudioTicks = audioClockPts + wallElapsedTicks;
        return true;
    }

    /// <summary>
    /// Re-syncs the cached audio clock from WASAPI (matching the resync done by
    /// <see cref="PaceFrameInterval"/>) and returns the extrapolated drift in
    /// milliseconds (positive = video ahead of audio). Returns false if the audio clock
    /// is unavailable, has never been sampled, or is stale (>200ms since last update) -
    /// callers must fall back to wall-clock pacing in that case.
    /// </summary>
    private bool TryComputeAudioMasterDriftMs(long videoPtsTicks, out double driftMs)
    {
        driftMs = 0;
        if (!TryGetFreshAudioMasterClock(out var extrapolatedAudioTicks, out _, out _))
        {
            return false;
        }

        driftMs = (videoPtsTicks - extrapolatedAudioTicks) / (double)TimeSpan.TicksPerMillisecond;
        return true;
    }

    /// <summary>
    /// Audio-master pacing. Video and audio are decoded from the same interleaved
    /// container on the same thread - their PTS are the source of truth.
    /// Without suppression, audio and video start at the same file position after
    /// seek, so the initial offset should be near-zero. This method corrects any
    /// drift that develops over time (hardware clock vs decode rate).
    /// Falls back to wall-clock pacing when audio is unavailable.
    /// </summary>
    private void PaceFrameInterval(Stopwatch pacingStopwatch, TimeSpan frameDuration, long videoPtsTicks)
    {
        // If the audio clock hasn't been updated in >200ms, WASAPI is likely underrunning -
        // fall through to wall-clock pacing instead of extrapolating against a stale sample.
        if (TryGetFreshAudioMasterClock(out var extrapolatedAudioTicks, out var wallElapsedTicks, out var hasAudioClockSample))
        {
            // diff > 0 = video ahead of audio, < 0 = video behind.
            var diffTicks = videoPtsTicks - extrapolatedAudioTicks;
            var diffMs = diffTicks / (double)TimeSpan.TicksPerMillisecond;
            var nominalDelayMs = frameDuration.TotalMilliseconds;

            // At HFR, per-frame corrections are visible, so correct
            // proportionally once drift is outside the lip-sync band instead
            // of accepting a persistent 100ms error.
            const double syncThresholdMs = 40.0;
            const double MaxAudioMasterCorrectionMs = 500.0;
            const double AudioMasterCorrectionGain = 0.10;
            const double MaxAudioMasterCorrectionFrameRatio = 0.25;

            if (Math.Abs(diffMs) > MaxAudioMasterCorrectionMs)
            {
                // WASAPI render PTS can lag decoded video by the endpoint buffer/device
                // latency after resume. Do not let that stale clock halve video cadence.
                RecordAudioMasterFallback("drift-outlier", diffMs, wallElapsedTicks);
                WallClockPace(pacingStopwatch, frameDuration);
                return;
            }

            ClearPendingAudioMasterFallback();

            double adjustedDelayMs;
            if (diffMs > syncThresholdMs)
            {
                // Video ahead: add bounded delay so audio can catch up.
                Interlocked.Increment(ref _playbackAudioMasterDelayDoubles);
                var correctionMs = Math.Min(
                    (diffMs - syncThresholdMs) * AudioMasterCorrectionGain,
                    nominalDelayMs * MaxAudioMasterCorrectionFrameRatio);
                adjustedDelayMs = nominalDelayMs + Math.Max(0, correctionMs);
            }
            else if (diffMs < -syncThresholdMs)
            {
                // Video behind: shave bounded delay; frame skip owns larger
                // video-behind errors before this point.
                Interlocked.Increment(ref _playbackAudioMasterDelayShrinks);
                var correctionMs = Math.Min(
                    (-diffMs - syncThresholdMs) * AudioMasterCorrectionGain,
                    nominalDelayMs * MaxAudioMasterCorrectionFrameRatio);
                adjustedDelayMs = Math.Max(0, nominalDelayMs - Math.Max(0, correctionMs));
                if (adjustedDelayMs <= 0)
                {
                    Interlocked.Increment(ref _playbackLateFrames);
                }
            }
            else
            {
                // Within threshold - smooth wall-clock cadence.
                adjustedDelayMs = nominalDelayMs;
            }

            if (adjustedDelayMs > 0)
            {
                var targetTicks = (long)(adjustedDelayMs / 1000.0 * Stopwatch.Frequency);
                var remaining = targetTicks - pacingStopwatch.ElapsedTicks;
                if (remaining > 0)
                {
                    var spinThresholdTicks = 2L * Stopwatch.Frequency / 1000;
                    if (remaining > spinThresholdTicks)
                    {
                        var sleepMs = (int)((remaining - spinThresholdTicks) * 1000 / Stopwatch.Frequency);
                        if (sleepMs > 0)
                        {
                            Thread.Sleep(sleepMs);
                        }
                    }

                    while (pacingStopwatch.ElapsedTicks < targetTicks)
                    {
                        Thread.SpinWait(1);
                    }
                }
            }

            return;
        }

        // Fallback: no audio clock available - pure wall-clock pacing.
        var fallbackReason = hasAudioClockSample ? "stale-clock" : "unavailable";
        RecordAudioMasterFallback(fallbackReason, 0, hasAudioClockSample ? wallElapsedTicks : 0);
        WallClockPace(pacingStopwatch, frameDuration);
    }

    private void WallClockPace(Stopwatch pacingStopwatch, TimeSpan frameDuration)
    {
        var targetTicks = (long)(frameDuration.TotalSeconds * Stopwatch.Frequency);
        var remaining = targetTicks - pacingStopwatch.ElapsedTicks;
        if (remaining > 0)
        {
            var spinThresholdTicks = 2L * Stopwatch.Frequency / 1000;
            if (remaining > spinThresholdTicks)
            {
                var sleepMs = (int)((remaining - spinThresholdTicks) * 1000 / Stopwatch.Frequency);
                if (sleepMs > 0)
                {
                    Thread.Sleep(sleepMs);
                }
            }

            while (pacingStopwatch.ElapsedTicks < targetTicks)
            {
                Thread.SpinWait(1);
            }
        }
        else
        {
            Interlocked.Increment(ref _playbackLateFrames);
        }
    }

    private void RecordAudioMasterFallback(string reason, double driftMs, long clockAgeTicks)
    {
        if (!IsTransientAudioMasterFallbackCandidate(reason))
        {
            CommitPendingAudioMasterFallback();
            CommitAudioMasterFallback(reason, driftMs, clockAgeTicks);
            return;
        }

        if (string.IsNullOrEmpty(_pendingAudioMasterFallbackReason))
        {
            _pendingAudioMasterFallbackReason = reason;
            _pendingAudioMasterFallbackDriftMs = driftMs;
            _pendingAudioMasterFallbackClockAgeTicks = clockAgeTicks;
            return;
        }

        CommitPendingAudioMasterFallback();
        CommitAudioMasterFallback(reason, driftMs, clockAgeTicks);
    }

    private static bool IsTransientAudioMasterFallbackCandidate(string reason)
        => string.Equals(reason, "unavailable", StringComparison.Ordinal) ||
           string.Equals(reason, "stale-clock", StringComparison.Ordinal) ||
           string.Equals(reason, "drift-outlier", StringComparison.Ordinal);

    private void ClearPendingAudioMasterFallback()
    {
        _pendingAudioMasterFallbackReason = string.Empty;
        _pendingAudioMasterFallbackDriftMs = 0;
        _pendingAudioMasterFallbackClockAgeTicks = 0;
    }

    private void CommitPendingAudioMasterFallback()
    {
        if (string.IsNullOrEmpty(_pendingAudioMasterFallbackReason))
        {
            return;
        }

        CommitAudioMasterFallback(
            _pendingAudioMasterFallbackReason,
            _pendingAudioMasterFallbackDriftMs,
            _pendingAudioMasterFallbackClockAgeTicks);
        ClearPendingAudioMasterFallback();
    }

    private void CommitAudioMasterFallback(string reason, double driftMs, long clockAgeTicks)
    {
        Interlocked.Increment(ref _playbackAudioMasterFallbacks);
        switch (reason)
        {
            case "unavailable":
                Interlocked.Increment(ref _playbackAudioMasterUnavailableFallbacks);
                break;
            case "stale-clock":
                Interlocked.Increment(ref _playbackAudioMasterStaleFallbacks);
                break;
            case "drift-outlier":
                Interlocked.Increment(ref _playbackAudioMasterDriftOutlierFallbacks);
                break;
        }

        Volatile.Write(ref _playbackAudioMasterLastFallbackReason, reason);
        _playbackAudioMasterLastFallbackDriftMs = driftMs;
        _playbackAudioMasterLastFallbackClockAgeMs = clockAgeTicks <= 0
            ? 0
            : clockAgeTicks / (double)TimeSpan.TicksPerMillisecond;
    }
}

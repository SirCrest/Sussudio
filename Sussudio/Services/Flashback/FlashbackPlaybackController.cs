using System;
using System.Diagnostics;
using System.Globalization;
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
        Stop
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
            if (lastFrame == TimeSpan.Zero) return TimeSpan.Zero;
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

    private void SetState(FlashbackPlaybackState newState)
    {
        var oldState = _state;
        if (oldState == newState) return;
        _state = newState;
        Logger.Log($"FLASHBACK_PLAYBACK_STATE {oldState} -> {newState}");
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
        var p95 = PercentileFromSorted(sorted, 0.95);
        var p99 = PercentileFromSorted(sorted, 0.99);
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
            PercentileFromSorted(samples, 0.95),
            PercentileFromSorted(samples, 0.99),
            samples[^1]);
    }

    private static double PercentileFromSorted(double[] sortedSamples, double percentile)
    {
        if (sortedSamples.Length == 0)
        {
            return 0;
        }

        var index = (int)Math.Ceiling(percentile * sortedSamples.Length) - 1;
        index = Math.Clamp(index, 0, sortedSamples.Length - 1);
        return sortedSamples[index];
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
}

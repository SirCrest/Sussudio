using System;
using System.Buffers;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Channels;
using Sussudio.Models;
using Sussudio.Services.Audio;
using Sussudio.Services.Preview;
using Sussudio.Services.Recording;
using Sussudio.Services.Runtime;

namespace Sussudio.Services.Flashback;

internal sealed class FlashbackPlaybackController : IDisposable
{
    private static readonly TimeSpan ActiveFmp4ReopenNearLiveGuard = TimeSpan.FromMilliseconds(250);
    private static readonly TimeSpan AdjacentSegmentSeekFallbackWindow = TimeSpan.FromSeconds(3);

    // --- Command types marshalled to the playback thread ---
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

    public readonly record struct PlaybackCadenceMetrics(
        int SampleCount,
        double P95FrameMs,
        double P99FrameMs,
        double MaxFrameMs,
        long SlowFrameCount,
        double SlowFramePercent,
        double OnePercentLowFps);

    public readonly record struct PlaybackDecodeMetrics(
        int SampleCount,
        double AvgMs,
        double P95Ms,
        double P99Ms,
        double MaxMs);

    // --- Dependencies ---
    private readonly FlashbackBufferManager _bufferManager;
    private IPreviewFrameSink? _previewSink;
    private ILiveVideoSource? _videoCapture;
    private volatile WasapiAudioPlayback? _audioPlayback;
    private volatile WasapiAudioCapture? _audioCapture;

    // --- State (read from UI thread, written primarily from playback thread) ---
    private volatile FlashbackPlaybackState _state = FlashbackPlaybackState.Live;
    private long _playbackPositionTicks;
    private volatile bool _initialized;
    private volatile int _disposedFlag;
    private volatile string _decoderHwAccel = "N/A";

    /// <summary>
    /// When true, the decoder attempts D3D11VA GPU decode. When false, forces software decode.
    /// Can be toggled at runtime — takes effect on next decoder creation.
    /// </summary>
    public bool GpuDecodeEnabled { get; set; } = true;

    // --- A/V sync tracking (ffplay-style audio-master clock) ---
    private long _lastAudioPtsTicks;  // PTS of last audio chunk delivered to WASAPI
    private long _lastVideoPtsTicks;  // PTS of last video frame displayed
    // Audio clock extrapolation: between WASAPI rendering PTS updates (~21ms for AAC),
    // we estimate the current audio position by adding wall-clock elapsed time.
    private long _audioClockPtsTicks;       // Last sampled audio rendering PTS
    private long _audioClockWallTicks;      // Stopwatch.GetTimestamp() when _audioClockPtsTicks was sampled
    private long _playbackDroppedFrames;    // Frames dropped because video was too far behind audio
    private long _playbackAudioMasterDelayDoubles;
    private long _playbackAudioMasterDelayShrinks;
    private long _playbackAudioMasterFallbacks;
    private long _playbackAudioMasterUnavailableFallbacks;
    private long _playbackAudioMasterStaleFallbacks;
    private long _playbackAudioMasterDriftOutlierFallbacks;
    private string _playbackAudioMasterLastFallbackReason = string.Empty;
    private double _playbackAudioMasterLastFallbackDriftMs;
    private double _playbackAudioMasterLastFallbackClockAgeMs;
    private readonly string _playbackMmcssTask = Environment.GetEnvironmentVariable("SUSSUDIO_FLASHBACK_PLAYBACK_MMCSS_TASK") ?? "Playback";
    private readonly int _playbackMmcssPriority = EnvironmentHelpers.GetIntFromEnv("SUSSUDIO_FLASHBACK_PLAYBACK_MMCSS_PRIORITY", 1, -2, 2);

    // --- Playback cadence metrics (written on playback thread, read from UI/diag) ---
    private long _playbackFrameCount;
    private long _playbackLateFrames;
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
    private long _lastPlaybackCadencePtsTicks = -1;
    private long _playbackPtsCadenceMismatchCount;
    private long _lastPlaybackPtsCadenceMismatchUtcUnixMs;
    private double _lastPlaybackPtsCadenceDeltaMs;
    private double _lastPlaybackPtsCadenceExpectedMs;
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
    private long _commandsEnqueued;
    private long _commandsProcessed;
    private long _commandsDropped;
    private long _commandsSkippedNotReady;
    private int _pendingCommands;
    private int _maxPendingCommands;
    private long _lastCommandQueueLatencyMs;
    private long _maxCommandQueueLatencyMs;
    private long _lastCommandQueuedUtcUnixMs;
    private long _lastCommandProcessedUtcUnixMs;
    private long _lastCommandFailureUtcUnixMs;
    private string _lastCommandQueued = "None";
    private string _lastCommandProcessed = "None";
    private string _lastCommandFailure = string.Empty;
    private int _activeCommandKind = -1;
    private long _activeCommandStartedTimestamp;
    private long _latestScrubUpdateTicks;
    private readonly object _seekSlotSync = new();
    private SeekIntentSlot? _queuedSeekSlot;
    private ScrubUpdateIntentSlot? _queuedScrubUpdateSlot;
    private long _scrubUpdatesCoalesced;
    private long _seekCommandsCoalesced;

    // --- Deferred frame release for D3D11VA (C1 fix) ---
    // The renderer's render thread hasn't copied the texture yet when we release.
    // Keep the previous frame alive until the next frame is submitted.
    private DecodedVideoFrame _previousHeldFrame;
    private bool _hasPreviousHeldFrame;

    // --- Scrub state restoration (M16 fix) ---
    private bool _wasPlayingBeforeScrub;

    // --- In/Out points ---
    private long _inPointTicks = long.MinValue;
    private long _outPointTicks = long.MinValue;

    public TimeSpan? InPoint
    {
        get
        {
            var t = Interlocked.Read(ref _inPointTicks);
            return t == long.MinValue ? null : TimeSpan.FromTicks(t);
        }
        set => Interlocked.Exchange(ref _inPointTicks, value.HasValue ? NormalizeMarkerPosition(value.Value).Ticks : long.MinValue);
    }

    public TimeSpan? OutPoint
    {
        get
        {
            var t = Interlocked.Read(ref _outPointTicks);
            return t == long.MinValue ? null : TimeSpan.FromTicks(t);
        }
        set => Interlocked.Exchange(ref _outPointTicks, value.HasValue ? NormalizeMarkerPosition(value.Value).Ticks : long.MinValue);
    }

    // --- Playback thread ---
    private const int CommandQueueCapacity = 256;
    private const double FallbackPlaybackFrameRate = 60.0;
    private const double MaxPlaybackFrameRate = 1000.0;
    private const double MaxContinuousSoftwarePlaybackPixelRate = 3840.0 * 2160.0 * 60.0;
    private readonly object _playbackThreadSync = new();
    private Thread? _playbackThread;
    private int _playbackThreadStarted;
    private CancellationTokenSource? _playCts;
    private Channel<PlaybackCommand> _commandChannel = CreateCommandChannel();

    public FlashbackPlaybackController(FlashbackBufferManager bufferManager)
    {
        _bufferManager = bufferManager ?? throw new ArgumentNullException(nameof(bufferManager));
    }

    // --- Public properties ---

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

    // --- Lifecycle ---

    public void Initialize(
        IPreviewFrameSink previewSink,
        ILiveVideoSource videoCapture,
        WasapiAudioPlayback? audioPlayback,
        WasapiAudioCapture? audioCapture)
    {
        lock (_playbackThreadSync)
        {
            ObjectDisposedException.ThrowIf(_disposedFlag != 0, this);
            _previewSink = previewSink ?? throw new ArgumentNullException(nameof(previewSink));
            _videoCapture = videoCapture ?? throw new ArgumentNullException(nameof(videoCapture));
            _audioPlayback = audioPlayback;
            _audioCapture = audioCapture;
            _initialized = true;
            Logger.Log("FLASHBACK_PLAYBACK_INIT");
        }
    }

    /// <summary>
    /// Updates audio references after WASAPI components become available.
    /// Called from CaptureService after StartWasapiPlaybackAsync completes,
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
        lock (_playbackThreadSync)
        {
            if (_disposedFlag != 0)
            {
                Logger.Log("FLASHBACK_PLAYBACK_PREVIEW_UPDATE_SKIP reason=disposed");
                return;
            }

            _previewSink = previewSink;
            _videoCapture = videoCapture;
            _initialized = previewSink != null && videoCapture != null;
            Logger.Log($"FLASHBACK_PLAYBACK_PREVIEW_UPDATE sink={previewSink != null} capture={videoCapture != null}");
        }
    }

    public void PrepareForPreviewDetach()
    {
        if (_disposedFlag != 0)
        {
            Logger.Log("FLASHBACK_PLAYBACK_PREVIEW_DETACH_SKIP reason=disposed");
            return;
        }

        Logger.Log($"FLASHBACK_PLAYBACK_PREVIEW_DETACH state={_state} thread_alive={PlaybackThreadAlive}");
        if (!StopPlaybackThread())
        {
            Logger.Log("FLASHBACK_PLAYBACK_PREVIEW_DETACH_ABORT reason=thread_stop_failed");
            return;
        }

        ReleasePlaybackFrameForLive("preview_detach");
        RestoreLiveAudio();
        SafeResumePreviewSubmission("preview_detach");
        SetState(FlashbackPlaybackState.Live);
    }

    // --- State transitions (called from UI thread) ---

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

    public bool UpdateScrub(TimeSpan position)
    {
        if (IsNotReady(CommandKind.UpdateScrub, position)) return false;
        if (!PlaybackThreadAlive) return RejectCommand(CommandKind.UpdateScrub, "thread_not_running", "thread_not_running", false, position);
        return SendUpdateScrubCommand(position);
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

    public bool EndScrub() => EndScrubAt(null);

    public bool EndScrubAt(TimeSpan position) => EndScrubAt((TimeSpan?)position);

    private bool EndScrubAt(TimeSpan? position)
    {
        if (IsNotReady(CommandKind.EndScrub, position)) return false;
        if (State == FlashbackPlaybackState.Live && !PlaybackThreadAlive)
        {
            MarkCommandNoOp(CommandKind.EndScrub, "live_thread_not_running", position);
            return true;
        }
        if (!PlaybackThreadAlive) return RejectCommand(CommandKind.EndScrub, "thread_not_running", "thread_not_running", false, position);
        return SendEndScrubCommand(position);
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

    public bool Play()
    {
        if (IsNotReady(CommandKind.Play)) return false;
        if (!EnsurePlaybackThread(CommandKind.Play)) return false;
        return SendCommand(new PlaybackCommand { Kind = CommandKind.Play });
    }

    public bool Pause()
    {
        if (IsNotReady(CommandKind.Pause)) return false;
        if (!EnsurePlaybackThread(CommandKind.Pause)) return false; // Thread must be running to handle Live→Paused
        return SendCommand(new PlaybackCommand { Kind = CommandKind.Pause });
    }

    public bool GoLive()
    {
        if (IsNotReady(CommandKind.GoLive)) return false;
        if (State == FlashbackPlaybackState.Live && !PlaybackThreadAlive)
        {
            MarkCommandNoOp(CommandKind.GoLive, "live_thread_not_running");
            return true;
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
            return true;
        }
        if (!EnsurePlaybackThread(CommandKind.Nudge)) return false;
        return SendCommand(new PlaybackCommand { Kind = CommandKind.Nudge, Delta = delta });
    }

    // --- In/Out point helpers ---

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

    // --- Dispose ---

    public void Dispose()
    {
        if (Interlocked.CompareExchange(ref _disposedFlag, 1, 0) != 0) return;

        Logger.Log($"FLASHBACK_PLAYBACK_DISPOSE_REQUEST state={_state} initialized={_initialized}");
        StopPlaybackThread();
        _initialized = false;
        Logger.Log("FLASHBACK_PLAYBACK_DISPOSED");
    }

    // --- Command dispatch ---

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
        if (!_commandChannel.Writer.TryWrite(queuedCommand))
        {
            DecrementPendingCommands();
            Interlocked.Increment(ref _commandsDropped);
            var detail = FormatCommandDetail(command);
            SetLastCommandFailure($"write_failed:{command.Kind}{detail}");
            Logger.Log($"FLASHBACK_PLAYBACK_CMD_DROP kind={command.Kind}{detail}");
            return false;
        }

        Interlocked.Increment(ref _commandsEnqueued);
        UpdateMaxPendingCommands(pending);
        MarkCommandQueued(command.Kind);
        return true;
    }

    private bool EnsurePlaybackThread(CommandKind commandKind)
    {
        lock (_playbackThreadSync)
        {
        if (_disposedFlag != 0) return RejectCommand(commandKind, "disposed", "disposed", false);
        if (Volatile.Read(ref _playbackThreadStarted) != 0)
        {
            if (_playbackThread is { IsAlive: true })
            {
                return true;
            }

            Logger.Log("FLASHBACK_PLAYBACK_THREAD_RECOVER reason=stale_stopped");
            DrainAbandonedCommandsOnThreadExit(_commandChannel);
            DisposePlaybackCtsBestEffort(_playCts, "recover_stale_thread");
            _playCts = null;
            _playbackThread = null;
            Interlocked.Exchange(ref _pendingCommands, 0);
            ClearQueuedCommandSlotsBarrier();
            Volatile.Write(ref _playbackThreadStarted, 0);
        }

        if (Interlocked.CompareExchange(ref _playbackThreadStarted, 1, 0) != 0)
            return true;

        // Recreate the command channel — the previous one was completed by StopPlaybackThread.
        // A completed channel silently drops all TryWrite calls.
        _commandChannel = CreateCommandChannel();
        var commandChannel = _commandChannel;
        _playCts = new CancellationTokenSource();
        var threadCts = _playCts;
        _playbackThread = new Thread(() => PlaybackThreadEntry(threadCts, commandChannel))
        {
            Name = "FlashbackPlayback",
            IsBackground = true,
            Priority = ThreadPriority.AboveNormal
        };
        try
        {
            _playbackThread.Start();
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_PLAYBACK_THREAD_START_FAIL type={ex.GetType().Name} msg='{ex.Message}'");
            DisposePlaybackCtsBestEffort(_playCts, "thread_start_fail");
            _playCts = null;
            _playbackThread = null;
            Interlocked.Exchange(ref _playbackThreadStarted, 0);
            return RejectCommand(
                commandKind,
                $"thread_start_failed:{ex.GetType().Name}:{ex.Message}",
                $"thread_start_failed type={ex.GetType().Name}",
                false);
        }
        Logger.Log("FLASHBACK_PLAYBACK_THREAD_START");
        return true;
        }
    }

    private bool StopPlaybackThread()
    {
        lock (_playbackThreadSync)
        {
            var stopStarted = Stopwatch.GetTimestamp();
            var thread = _playbackThread;
            var threadWasAlive = Volatile.Read(ref _playbackThreadStarted) != 0 && thread is { IsAlive: true };
            var activeKindAtRequest = FormatActiveCommandKind(Volatile.Read(ref _activeCommandKind));
            var activeElapsedMsAtRequest = GetActiveCommandElapsedMs(stopStarted);
            if (Volatile.Read(ref _playbackThreadStarted) != 0 && thread is { IsAlive: true })
            {
                SendCommand(new PlaybackCommand { Kind = CommandKind.Stop });
            }

            _commandChannel.Writer.TryComplete();

            try
            {
                _playCts?.Cancel();
            }
            catch (Exception ex)
            {
                Logger.Log($"FLASHBACK_PLAYBACK_CANCEL_WARN type={ex.GetType().Name} msg='{ex.Message}'");
            }

            var threadExited = true;
            if (thread is { IsAlive: true })
            {
                if (ReferenceEquals(Thread.CurrentThread, thread))
                {
                    Logger.Log("FLASHBACK_PLAYBACK_THREAD_JOIN_SKIP reason=self");
                    SetLastCommandFailure("thread_join_skipped:self");
                    threadExited = false;
                }
                else if (!thread.Join(TimeSpan.FromSeconds(3)))
                {
                    Logger.Log("FLASHBACK_PLAYBACK_THREAD_JOIN_TIMEOUT");
                    SetLastCommandFailure("thread_join_timeout");
                    threadExited = false;
                }
            }

            var stopElapsedMs = Stopwatch.GetElapsedTime(stopStarted).TotalMilliseconds;
            Logger.Log(
                $"FLASHBACK_PLAYBACK_STOP_THREAD_COMPLETE duration_ms={stopElapsedMs:0.###} " +
                $"thread_was_alive={threadWasAlive} thread_exited={threadExited} " +
                $"active_at_request={activeKindAtRequest} active_ms_at_request={activeElapsedMsAtRequest:0.###} " +
                $"pending={Volatile.Read(ref _pendingCommands)}");

            if (threadExited)
            {
                DisposePlaybackCtsBestEffort(_playCts, "stop_thread");
                _playCts = null;
                _playbackThread = null;
                Interlocked.Exchange(ref _pendingCommands, 0);
                ClearQueuedCommandSlotsBarrier();
                Volatile.Write(ref _playbackThreadStarted, 0);
            }

            return threadExited;
        }
    }

    // --- Playback thread ---

    private void PlaybackThreadEntry(CancellationTokenSource cts, Channel<PlaybackCommand> commandChannel)
    {
        FlashbackDecoder? decoder = null;
        var pacingStopwatch = new Stopwatch();
        var frameDuration = TimeSpan.Zero;
        var isPlaying = false;
        var isScrubbing = false;
        var fileOpen = false;
        var frozenValidStart = TimeSpan.Zero; // captured when leaving Live, used for position mapping
        TimeSpan? pendingExactResumeTarget = null;

        // Set 1ms timer resolution for accurate Thread.Sleep pacing.
        // Without this, Sleep(8) at 120fps sleeps ~15ms (default granularity) → half-speed.
        timeBeginPeriod(1);
        using var mmcss = MmcssThreadRegistration.TryRegister(_playbackMmcssTask, _playbackMmcssPriority, message => Logger.Log(message));
        try
        {
            Logger.Log("FLASHBACK_PLAYBACK_THREAD_ENTER");
            while (true)
            {
                PlaybackCommand cmd;
                if (isPlaying)
                {
                    if (!commandChannel.Reader.TryRead(out cmd))
                    {
                        if (cts.IsCancellationRequested)
                        {
                            Logger.Log("FLASHBACK_PLAYBACK_THREAD_EXIT cancellation_requested");
                            CleanupDecoder(ref decoder, ref fileOpen);
                            Interlocked.Exchange(ref _lastAudioPtsTicks, 0);
                            Interlocked.Exchange(ref _lastVideoPtsTicks, 0);
                            RestoreLiveAudio();
                            SafeResumePreviewSubmission("thread_cancelled");
                            SetState(FlashbackPlaybackState.Live);
                            return;
                        }

                        if (decoder is { IsOpen: true })
                        {
                            if (!PaceAndDecodeFrame(decoder, commandChannel, pacingStopwatch, ref frameDuration, ref fileOpen, frozenValidStart, cts.Token))
                            {
                                isPlaying = false;
                            }
                        }
                        continue;
                    }
                    TrackCommandDequeued(cmd);
                }
                else
                {
                    if (!commandChannel.Reader.TryRead(out cmd))
                    {
                        var canRead = commandChannel.Reader.WaitToReadAsync(cts.Token).AsTask().GetAwaiter().GetResult();
                        if (!canRead)
                        {
                            Logger.Log("FLASHBACK_PLAYBACK_THREAD_EXIT channel_closed");
                            isScrubbing = false;
                            CleanupDecoder(ref decoder, ref fileOpen);
                            Interlocked.Exchange(ref _lastAudioPtsTicks, 0);
                            Interlocked.Exchange(ref _lastVideoPtsTicks, 0);
                            RestoreLiveAudio();
                            SafeResumePreviewSubmission("channel_closed");
                            SetState(FlashbackPlaybackState.Live);
                            return;
                        }

                        if (_disposedFlag != 0)
                        {
                            Logger.Log("FLASHBACK_PLAYBACK_THREAD_EXIT");
                            isScrubbing = false;
                            CleanupDecoder(ref decoder, ref fileOpen);
                            Interlocked.Exchange(ref _lastAudioPtsTicks, 0);
                            Interlocked.Exchange(ref _lastVideoPtsTicks, 0);
                            RestoreLiveAudio();
                            SafeResumePreviewSubmission("thread_disposed");
                            SetState(FlashbackPlaybackState.Live);
                            return;
                        }
                        if (!commandChannel.Reader.TryRead(out cmd))
                        {
                            continue;
                        }
                    }
                    TrackCommandDequeued(cmd);
                }

                var commandStarted = Stopwatch.GetTimestamp();
                Volatile.Write(ref _activeCommandKind, (int)cmd.Kind);
                Volatile.Write(ref _activeCommandStartedTimestamp, commandStarted);
                try
                {
                    switch (cmd.Kind)
                    {
                        case CommandKind.Stop:
                            isPlaying = false;
                            isScrubbing = false;
                            pendingExactResumeTarget = null;
                            CleanupDecoder(ref decoder, ref fileOpen);
                            Interlocked.Exchange(ref _lastAudioPtsTicks, 0);
                            Interlocked.Exchange(ref _lastVideoPtsTicks, 0);
                            RestoreLiveAudio();
                            SafeResumePreviewSubmission("thread_stop");
                            SetState(FlashbackPlaybackState.Live);
                            Logger.Log("FLASHBACK_PLAYBACK_THREAD_EXIT");
                            return;

                        case CommandKind.Seek:
                        cmd = ResolveSeekCommandPosition(cmd);
                        while (commandChannel.Reader.TryPeek(out var newerSeek) &&
                               newerSeek.Kind == CommandKind.Seek)
                        {
                            if (!commandChannel.Reader.TryRead(out newerSeek))
                            {
                                break;
                            }

                            TrackCommandDequeued(newerSeek);
                            newerSeek = ResolveSeekCommandPosition(newerSeek);
                            cmd = newerSeek;
                        }

                        _wasPlayingBeforeScrub = isPlaying || State == FlashbackPlaybackState.Live;
                        isPlaying = false;
                        isScrubbing = false;
                        frozenValidStart = _bufferManager.ValidStartPts;
                        SafeSuppressPreviewSubmission("seek");
                        SuppressLiveAudio();
                        SafePauseRendering("seek");

                        cmd = cmd with { Position = ClampPosition(cmd.Position, frozenValidStart) };
                        var seekResumeTarget = SaturatingAdd(cmd.Position, frozenValidStart);
                        decoder ??= CreateDecoder();
                        EnsureFileOpen(decoder, ref fileOpen, seekResumeTarget);
                        cts.Token.ThrowIfCancellationRequested();
                        if (!IsDecoderFileReady(decoder, fileOpen))
                        {
                            pendingExactResumeTarget = null;
                            SetNoFileFailure(CommandKind.Seek, cmd.Position);
                            Logger.Log("FLASHBACK_PLAYBACK_SEEK_NO_FILE - restoring live");
                            ReleasePlaybackFrameForLive("seek_no_file");
                            RestoreLiveAudio();
                            SafeResumePreviewSubmission("seek_no_file");
                            SafeResumeRendering("seek_no_file");
                            SetState(FlashbackPlaybackState.Live);
                            break;
                        }

                        if (!SeekAndDisplayKeyframe(decoder, ref fileOpen, cmd.Position, frozenValidStart, CommandKind.Seek, cts.Token))
                        {
                            isPlaying = false;
                            isScrubbing = false;
                            pendingExactResumeTarget = null;
                            RestoreLiveAfterSeekDisplayFailure(decoder, ref fileOpen, "seek_display_failed");
                            break;
                        }
                        isPlaying = _wasPlayingBeforeScrub;
                        if (isPlaying)
                        {
                            pendingExactResumeTarget = null;
                            ResetPlaybackMetrics();
                            pacingStopwatch.Restart();
                            var coalescedSeekTarget = seekResumeTarget;
                            decoder.AudioChunkCallback = null;
                            if (!TrySeekWithActiveFmp4Reopen(decoder, ref fileOpen, coalescedSeekTarget, "seek_resume", cts.Token))
                            {
                                isPlaying = false;
                                pendingExactResumeTarget = null;
                                RestoreLiveAfterSeekDisplayFailure(decoder, ref fileOpen, "seek_resume_failed");
                                break;
                            }
                            if (TrySnapLiveForSoftwarePlaybackBudget(decoder, ref fileOpen, "seek_resume"))
                            {
                                isPlaying = false;
                                break;
                            }
                            frameDuration = ResolveFrameDuration(decoder);
                            RestoreAudioCallback(decoder, coalescedSeekTarget.Ticks);
                            SafeFlushPlayback("seek_resume");
                            SafeResumeRendering("seek_resume");
                        }
                        else
                        {
                            pendingExactResumeTarget = seekResumeTarget;
                        }
                        SetState(isPlaying ? FlashbackPlaybackState.Playing : FlashbackPlaybackState.Paused);
                        Logger.Log($"FLASHBACK_PLAYBACK_SEEK pos_ms={(long)PlaybackPosition.TotalMilliseconds} resumePlay={isPlaying}");
                        break;

                    case CommandKind.BeginScrub:
                        pendingExactResumeTarget = null;
                        // Only capture the resume-state on first entry into Scrubbing.
                        // A second BeginScrub arriving while we're already scrubbing
                        // (UI re-press race, MCP automation racing pointer-pressed)
                        // would otherwise sample isPlaying=false (set by the prior
                        // BeginScrub) and State=Scrubbing, clobbering the original
                        // capture and causing EndScrub to land in Paused instead of
                        // resuming Playing/Live.
                        if (!isScrubbing)
                        {
                            _wasPlayingBeforeScrub = isPlaying || State == FlashbackPlaybackState.Live;
                            frozenValidStart = _bufferManager.ValidStartPts;
                        }
                        else
                        {
                            var proposedValidStart = _bufferManager.ValidStartPts;
                            Logger.Log($"FLASHBACK_PLAYBACK_BEGIN_SCRUB_DUPLICATE existing_frozen_ms={frozenValidStart.TotalMilliseconds:F0} new_proposed_ms={proposedValidStart.TotalMilliseconds:F0}");
                        }
                        isPlaying = false;
                        isScrubbing = true;
                        SafeSuppressPreviewSubmission("begin_scrub");
                        SuppressLiveAudio();
                        SafePauseRendering("begin_scrub");
                        SetState(FlashbackPlaybackState.Scrubbing);

                        cmd = cmd with { Position = ClampPosition(cmd.Position, frozenValidStart) };
                        decoder ??= CreateDecoder();
                        EnsureFileOpen(decoder, ref fileOpen, SaturatingAdd(cmd.Position, frozenValidStart));
                        cts.Token.ThrowIfCancellationRequested();
                        if (!IsDecoderFileReady(decoder, fileOpen))
                        {
                            Logger.Log("FLASHBACK_PLAYBACK_SCRUB_NO_FILE — restoring live");
                            isScrubbing = false;
                            pendingExactResumeTarget = null;
                            SetNoFileFailure(CommandKind.BeginScrub, cmd.Position);
                            ReleasePlaybackFrameForLive("scrub_no_file");
                            RestoreLiveAudio();
                            SafeResumePreviewSubmission("scrub_no_file");
                            SafeResumeRendering("scrub_no_file");
                            SetState(FlashbackPlaybackState.Live);
                            break;
                        }
                        if (!SeekAndDisplayKeyframe(decoder, ref fileOpen, cmd.Position, frozenValidStart, CommandKind.BeginScrub, cts.Token))
                        {
                            isScrubbing = false;
                            pendingExactResumeTarget = null;
                            RestoreLiveAfterSeekDisplayFailure(decoder, ref fileOpen, "begin_scrub_display_failed");
                        }
                        break;

                    case CommandKind.UpdateScrub:
                        pendingExactResumeTarget = null;
                        cmd = ResolveScrubUpdateCommandPosition(cmd);
                        if (!isScrubbing)
                        {
                            MarkCommandNoOp(CommandKind.UpdateScrub, "not_scrubbing", cmd.Position);
                            break;
                        }
                        // Drain stale UpdateScrub commands only. Leave control commands queued
                        // so their latency/accounting stays tied to the original command.
                        while (commandChannel.Reader.TryPeek(out var newer) &&
                               newer.Kind == CommandKind.UpdateScrub)
                        {
                            if (!commandChannel.Reader.TryRead(out newer))
                            {
                                break;
                            }

                            TrackCommandDequeued(newer);
                            newer = ResolveScrubUpdateCommandPosition(newer);
                            cmd = newer;
                        }
                        cmd = cmd with { Position = ClampPosition(cmd.Position, frozenValidStart) };
                        decoder ??= CreateDecoder();
                        EnsureFileOpen(decoder, ref fileOpen, SaturatingAdd(cmd.Position, frozenValidStart));
                        cts.Token.ThrowIfCancellationRequested();
                        if (!IsDecoderFileReady(decoder, fileOpen))
                        {
                            SetNoFileFailure(CommandKind.UpdateScrub, cmd.Position);
                            isScrubbing = false;
                            pendingExactResumeTarget = null;
                            ReleasePlaybackFrameForLive("scrub_update_no_file");
                            RestoreLiveAudio();
                            SafeResumePreviewSubmission("scrub_update_no_file");
                            SafeResumeRendering("scrub_update_no_file");
                            SetState(FlashbackPlaybackState.Live);
                            Logger.Log($"FLASHBACK_PLAYBACK_SCRUB_UPDATE_NO_FILE pos_ms={(long)cmd.Position.TotalMilliseconds}");
                            break;
                        }
                        if (!SeekAndDisplayKeyframe(decoder, ref fileOpen, cmd.Position, frozenValidStart, CommandKind.UpdateScrub, cts.Token))
                        {
                            isScrubbing = false;
                            pendingExactResumeTarget = null;
                            RestoreLiveAfterSeekDisplayFailure(decoder, ref fileOpen, "scrub_update_display_failed");
                        }
                        break;

                    case CommandKind.EndScrub:
                        if (!isScrubbing)
                        {
                            MarkCommandNoOp(CommandKind.EndScrub, "not_scrubbing", cmd.Position);
                            break;
                        }
                        var endScrubPosition = ClampPosition(cmd.Position, frozenValidStart);
                        PlaybackPosition = endScrubPosition;
                        isScrubbing = false;
                        isPlaying = _wasPlayingBeforeScrub;
                        var endScrubTarget = SaturatingAdd(endScrubPosition, frozenValidStart);
                        if (isPlaying)
                        {
                            pendingExactResumeTarget = null;
                            ResetPlaybackMetrics();
                            pacingStopwatch.Restart();

                            // Re-seek to the current position using SeekTo (not SeekToKeyframe).
                            // SeekTo forward-decodes from keyframe to target, which advances
                            // BOTH the video and audio codecs to the same PTS. Without this,
                            // the audio codec is stuck at the keyframe (~1s behind video).
                            if (decoder is { IsOpen: true })
                            {
                                decoder.AudioChunkCallback = null; // null during forward-decode
                                if (!TrySeekWithActiveFmp4Reopen(decoder, ref fileOpen, endScrubTarget, "end_scrub", cts.Token))
                                {
                                    isPlaying = false;
                                    pendingExactResumeTarget = null;
                                    RestoreLiveAfterSeekDisplayFailure(decoder, ref fileOpen, "end_scrub_seek_failed");
                                    break;
                                }
                                if (TrySnapLiveForSoftwarePlaybackBudget(decoder, ref fileOpen, "end_scrub"))
                                {
                                    isPlaying = false;
                                    break;
                                }
                                frameDuration = ResolveFrameDuration(decoder);
                            }
                            if (decoder != null)
                            {
                                RestoreAudioCallback(decoder, endScrubTarget.Ticks);
                            }
                            SafeFlushPlayback("end_scrub_resume");
                            SafeResumeRendering("end_scrub_resume");
                        }
                        else
                        {
                            pendingExactResumeTarget = endScrubTarget;
                        }
                        SetState(isPlaying ? FlashbackPlaybackState.Playing : FlashbackPlaybackState.Paused);
                        var endScrubBufDur = _bufferManager.BufferedDuration;
                        Logger.Log($"FLASHBACK_ENDSCRUB pos_ms={(long)PlaybackPosition.TotalMilliseconds} bufferDur_ms={(long)endScrubBufDur.TotalMilliseconds} gapFromLive_ms={SaturatingSubtract(endScrubBufDur, PlaybackPosition).TotalMilliseconds:F0} resumePlay={isPlaying}");
                        break;

                    case CommandKind.Play:
                        if (isPlaying)
                        {
                            MarkCommandNoOp(CommandKind.Play, "already_playing");
                            break;
                        }
                        isScrubbing = false;
                        isPlaying = true;
                        SafeSuppressPreviewSubmission("play");
                        SuppressLiveAudio();
                        SafePauseRendering("play");
                        ResetPlaybackMetrics();
                        pacingStopwatch.Restart();

                        if (State == FlashbackPlaybackState.Live)
                            frozenValidStart = _bufferManager.ValidStartPts;
                        decoder ??= CreateDecoder();
                        var prevFile = _currentOpenFilePath;
                        var pendingPlayTarget = pendingExactResumeTarget ?? SaturatingAdd(PlaybackPosition, frozenValidStart);
                        EnsureFileOpen(decoder, ref fileOpen, pendingPlayTarget);
                        if (!IsDecoderFileReady(decoder, fileOpen))
                        {
                            Logger.Log("FLASHBACK_PLAYBACK_PLAY_NO_FILE — restoring live");
                            SetNoFileFailure(CommandKind.Play, PlaybackPosition);
                            isPlaying = false;
                            pendingExactResumeTarget = null;
                            ReleasePlaybackFrameForLive("play_no_file");
                            RestoreLiveAudio();
                            SafeResumePreviewSubmission("play_no_file");
                            SafeResumeRendering("play_no_file");
                            SetState(FlashbackPlaybackState.Live);
                            break;
                        }
                        var requireExactResumeSeek = pendingExactResumeTarget.HasValue;
                        var seekTarget = pendingPlayTarget;
                        if (State == FlashbackPlaybackState.Paused &&
                            IsSamePlaybackPath(prevFile, _currentOpenFilePath) &&
                            !requireExactResumeSeek)
                        {
                            // Resume from Paused — decoder is already positioned at the
                            // correct frame (set by Pause or scrub). Skip the expensive
                            // re-seek which flushes codec state and decodes forward from
                            // a keyframe, potentially landing on a different frame.
                            Logger.Log($"FLASHBACK_PLAYBACK_RESUME_NO_SEEK pos_ms={(long)PlaybackPosition.TotalMilliseconds}");
                        }
                        else
                        {
                            // Playing from Live or file changed — full seek required.
                            // Audio callback is null during SeekTo so audio packets between
                            // keyframe and target are skipped (not decoded). After seek,
                            // the audio codec is clean and the next audio packet in the file
                            // is at the video target position. No suppression needed.
                            decoder.AudioChunkCallback = null;
                            if (requireExactResumeSeek)
                            {
                                Logger.Log($"FLASHBACK_PLAYBACK_RESUME_EXACT_SEEK target_ms={(long)seekTarget.TotalMilliseconds} display_pos_ms={(long)PlaybackPosition.TotalMilliseconds}");
                            }
                            if (!TrySeekWithActiveFmp4Reopen(decoder, ref fileOpen, seekTarget, "play", cts.Token))
                            {
                                isPlaying = false;
                                pendingExactResumeTarget = null;
                                RestoreLiveAfterSeekDisplayFailure(decoder, ref fileOpen, "play_seek_failed");
                                break;
                            }
                            if (TrySnapLiveForSoftwarePlaybackBudget(decoder, ref fileOpen, "play"))
                            {
                                isPlaying = false;
                                pendingExactResumeTarget = null;
                                break;
                            }
                        }
                        pendingExactResumeTarget = null;
                        frameDuration = ResolveFrameDuration(decoder);
                        RestoreAudioCallback(decoder, seekTarget.Ticks);
                        SafeFlushPlayback("play");
                        SafeResumeRendering("play");

                        SetState(FlashbackPlaybackState.Playing);
                        Logger.Log($"FLASHBACK_PLAYBACK_PLAY pos_ms={(long)PlaybackPosition.TotalMilliseconds}");
                        break;

                    case CommandKind.Pause:
                        if (isPlaying)
                        {
                            // Pause from Playing state — last decoded frame is already displayed
                            // and held via _previousHeldFrame. PlaybackPosition is already set
                            // to the last decoded frame's PTS. No seek needed.
                            isPlaying = false;
                            SafePauseRendering("pause");
                            pacingStopwatch.Stop();
                            SetState(FlashbackPlaybackState.Paused);
                            Logger.Log($"FLASHBACK_PLAYBACK_PAUSE pos_ms={(long)PlaybackPosition.TotalMilliseconds}");
                        }
                        else if (State == FlashbackPlaybackState.Live)
                        {
                            // Pause from Live state — freeze at current buffer edge
                            SafeSuppressPreviewSubmission("pause_from_live");
                            SuppressLiveAudio();
                            SafePauseRendering("pause_from_live");

                            frozenValidStart = _bufferManager.ValidStartPts;
                            var pauseTarget = ResolvePauseFromLiveTarget(frozenValidStart);
                            var pausePos = ClampPosition(SaturatingSubtract(pauseTarget, frozenValidStart), frozenValidStart);
                            decoder ??= CreateDecoder();
                            EnsureFileOpen(decoder, ref fileOpen, SaturatingAdd(pausePos, frozenValidStart));
                            cts.Token.ThrowIfCancellationRequested();
                            if (!IsDecoderFileReady(decoder, fileOpen))
                            {
                                pendingExactResumeTarget = null;
                                SetNoFileFailure(CommandKind.Pause, pausePos);
                                ReleasePlaybackFrameForLive("pause_from_live_no_file");
                                RestoreLiveAudio();
                                SafeResumePreviewSubmission("pause_from_live_no_file");
                                SafeResumeRendering("pause_from_live_no_file");
                                SetState(FlashbackPlaybackState.Live);
                                Logger.Log($"FLASHBACK_PLAYBACK_PAUSE_FROM_LIVE_NO_FILE pos_ms={(long)pausePos.TotalMilliseconds}");
                                break;
                            }

                            if (!SeekAndDisplayKeyframe(decoder, ref fileOpen, pausePos, frozenValidStart, CommandKind.Pause, cts.Token))
                            {
                                pendingExactResumeTarget = null;
                                RestoreLiveAfterSeekDisplayFailure(decoder, ref fileOpen, "pause_from_live_display_failed");
                                break;
                            }

                            pendingExactResumeTarget = SaturatingAdd(PlaybackPosition, frozenValidStart);

                            SetState(FlashbackPlaybackState.Paused);
                            Logger.Log($"FLASHBACK_PLAYBACK_PAUSE_FROM_LIVE pos_ms={(long)PlaybackPosition.TotalMilliseconds} target_ms={(long)pauseTarget.TotalMilliseconds} frozen_frame=true");
                        }
                        break;

                    case CommandKind.GoLive:
                        isPlaying = false;
                        isScrubbing = false;
                        pendingExactResumeTarget = null;
                        CleanupDecoder(ref decoder, ref fileOpen);
                        Interlocked.Exchange(ref _lastAudioPtsTicks, 0);
                        Interlocked.Exchange(ref _lastVideoPtsTicks, 0);
                        RestoreLiveAudio();
                        SafeResumePreviewSubmission("go_live");
                        SetState(FlashbackPlaybackState.Live);
                        Logger.Log("FLASHBACK_PLAYBACK_GO_LIVE");
                        return;

                    case CommandKind.Nudge:
                        pendingExactResumeTarget = null;
                        var nudgedPos = SaturatingAdd(PlaybackPosition, cmd.Delta);
                        nudgedPos = ClampPosition(nudgedPos, frozenValidStart);
                        decoder ??= CreateDecoder();
                        EnsureFileOpen(decoder, ref fileOpen, SaturatingAdd(nudgedPos, frozenValidStart));
                        cts.Token.ThrowIfCancellationRequested();
                        if (!IsDecoderFileReady(decoder, fileOpen))
                        {
                            SetNoFileFailure(CommandKind.Nudge, nudgedPos);
                            PlaybackPosition = nudgedPos;
                            isPlaying = false;
                            isScrubbing = false;
                            ReleasePlaybackFrameForLive("nudge_no_file");
                            RestoreLiveAudio();
                            SafeResumePreviewSubmission("nudge_no_file");
                            SafeResumeRendering("nudge_no_file");
                            SetState(FlashbackPlaybackState.Live);
                            Logger.Log($"FLASHBACK_PLAYBACK_NUDGE_NO_FILE pos_ms={(long)nudgedPos.TotalMilliseconds}");
                            break;
                        }

                        // F7 fix: forward nudge decodes next frame for frame-accuracy;
                        // backward nudge requires full seek (keyframe snap acceptable)
                        if (cmd.Delta.Ticks > 0)
                        {
                            var got = TryDecodeNextVideoFrameWithMetrics(decoder, out var nudgeFrame, cts.Token);
                            if (got)
                            {
                                if (!TrySubmitAndHoldFrame(nudgeFrame, "nudge"))
                                {
                                    break;
                                }
                                var actualPos = SaturatingSubtract(nudgeFrame.Pts, frozenValidStart);
                                if (actualPos < TimeSpan.Zero) actualPos = TimeSpan.Zero;
                                PlaybackPosition = actualPos;
                                break;
                            }
                            // Forward decode failed (EOF) — fall through to full seek
                        }
                        if (!SeekAndDisplayKeyframe(decoder, ref fileOpen, nudgedPos, frozenValidStart, CommandKind.Nudge, cts.Token))
                        {
                            isPlaying = false;
                            isScrubbing = false;
                            RestoreLiveAfterSeekDisplayFailure(decoder, ref fileOpen, "nudge_display_failed");
                        }
                        break;
                    }
                }
                finally
                {
                    var commandElapsedMs = Stopwatch.GetElapsedTime(commandStarted).TotalMilliseconds;
                    Volatile.Write(ref _activeCommandStartedTimestamp, 0);
                    Volatile.Write(ref _activeCommandKind, -1);
                    Logger.Log($"FLASHBACK_PLAYBACK_CMD_COMPLETE kind={cmd.Kind} duration_ms={commandElapsedMs:0.###}");
                }
            }
        }
        catch (OperationCanceledException)
        {
            Logger.Log("FLASHBACK_PLAYBACK_THREAD_CANCELLED");
            CleanupDecoder(ref decoder, ref fileOpen);
            Interlocked.Exchange(ref _lastAudioPtsTicks, 0);
            Interlocked.Exchange(ref _lastVideoPtsTicks, 0);
            RestoreLiveAudio();
            SafeResumePreviewSubmission("thread_cancelled");
            SetState(FlashbackPlaybackState.Live);
        }
        catch (Exception ex)
        {
            SetLastCommandFailure(ex.GetType().Name + ":" + ex.Message);
            Logger.Log($"FLASHBACK_PLAYBACK_FATAL type={ex.GetType().Name} error='{ex.Message}'");
            CleanupDecoder(ref decoder, ref fileOpen);
            Interlocked.Exchange(ref _lastAudioPtsTicks, 0);
            Interlocked.Exchange(ref _lastVideoPtsTicks, 0);
            RestoreLiveAudio();
            SafeResumePreviewSubmission("thread_fatal");
            SetState(FlashbackPlaybackState.Live);
        }
        finally
        {
            timeEndPeriod(1);
            CompleteCommandChannelForThreadExit(commandChannel);
            DrainAbandonedCommandsOnThreadExit(commandChannel);
            var ownsPlaybackThread = ReferenceEquals(Thread.CurrentThread, _playbackThread);
            var ownsCts = ReferenceEquals(cts, _playCts);
            if (ownsPlaybackThread)
            {
                _playbackThread = null;
            }
            if (ownsCts)
            {
                _playCts = null;
            }
            DisposePlaybackCtsBestEffort(cts, "thread_exit");
            if (ownsPlaybackThread || ownsCts)
            {
                Volatile.Write(ref _playbackThreadStarted, 0);
            }
        }

        Logger.Log("FLASHBACK_PLAYBACK_THREAD_EXIT");
    }

    private static void DisposePlaybackCtsBestEffort(CancellationTokenSource? cts, string operation)
    {
        if (cts == null) return;

        try
        {
            cts.Dispose();
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_PLAYBACK_CTS_DISPOSE_WARN op={operation} type={ex.GetType().Name} msg='{ex.Message}'");
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

    private void DrainAbandonedCommandsOnThreadExit(Channel<PlaybackCommand> commandChannel)
    {
        var abandoned = 0;
        while (commandChannel.Reader.TryRead(out var command))
        {
            DecrementPendingCommands();
            if (command.Kind != CommandKind.Stop)
            {
                abandoned++;
            }
        }

        if (abandoned > 0)
        {
            Interlocked.Add(ref _commandsDropped, abandoned);
            if (string.IsNullOrEmpty(Volatile.Read(ref _lastCommandFailure)))
            {
                SetLastCommandFailure($"abandoned_on_exit:{abandoned}");
            }
            Logger.Log($"FLASHBACK_PLAYBACK_CMD_ABANDONED count={abandoned}");
        }

        if (Volatile.Read(ref _pendingCommands) > 0)
        {
            Interlocked.Exchange(ref _pendingCommands, 0);
        }

        ClearQueuedCommandSlotsBarrier();
    }

    private static void CompleteCommandChannelForThreadExit(Channel<PlaybackCommand> commandChannel)
    {
        try
        {
            commandChannel.Writer.TryComplete();
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_PLAYBACK_CHANNEL_COMPLETE_WARN type={ex.GetType().Name} msg='{ex.Message}'");
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

    // --- Decode helpers ---

    private FlashbackDecoder CreateDecoder()
    {
        var useGpu = GpuDecodeEnabled;
        Logger.Log($"FLASHBACK_PLAYBACK_DECODER_CREATE gpu={useGpu}");
        var decoder = new FlashbackDecoder();

        // Get D3D11 device pointers for GPU-direct decode (skip if GPU decode disabled)
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

    private string? _currentOpenFilePath;

    private void EnsureFileOpen(FlashbackDecoder decoder, ref bool fileOpen, TimeSpan? targetPts = null)
    {
        // Determine which segment file contains the target position
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

        // If already open on the correct file, nothing to do
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

    private bool TrySeekWithActiveFmp4Reopen(
        FlashbackDecoder decoder,
        ref bool fileOpen,
        TimeSpan seekTarget,
        string reason,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (decoder.SeekTo(seekTarget, cancellationToken))
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
            if (decoder.IsOpen)
            {
                decoder.CloseFile();
            }

            fileOpen = false;
            decoder.OpenFile(nextPath);
            fileOpen = true;
            _currentOpenFilePath = nextPath;
            _decoderHwAccel = decoder.IsD3D11HwAccelerated ? "D3D11VA" : "Software";
            cancellationToken.ThrowIfCancellationRequested();
            if (decoder.SeekTo(effectiveSeekTarget, cancellationToken))
            {
                Interlocked.Increment(ref _playbackSegmentSwitches);
                Interlocked.Exchange(ref _lastSegmentSwitchUtcUnixMs, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
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
            _decoderHwAccel = "N/A";
            fileOpen = false;
            _currentOpenFilePath = null;
            return false;
        }
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
            if (decoder.IsOpen)
            {
                decoder.CloseFile();
            }

            fileOpen = false;
            decoder.OpenFile(currentPath);
            fileOpen = true;
            _currentOpenFilePath = currentPath;
            _decoderHwAccel = decoder.IsD3D11HwAccelerated ? "D3D11VA" : "Software";
            cancellationToken.ThrowIfCancellationRequested();
            if (decoder.SeekTo(seekTarget, cancellationToken))
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
            _decoderHwAccel = "N/A";
            fileOpen = false;
            _currentOpenFilePath = null;
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
            if (decoder.IsOpen)
            {
                decoder.CloseFile();
            }

            fileOpen = false;
            decoder.OpenFile(currentPath);
            fileOpen = true;
            _currentOpenFilePath = currentPath;
            _decoderHwAccel = decoder.IsD3D11HwAccelerated ? "D3D11VA" : "Software";
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
            _decoderHwAccel = "N/A";
            fileOpen = false;
            _currentOpenFilePath = null;
            return false;
        }
    }

    private void ReleasePreviousHeldFrame()
    {
        if (_hasPreviousHeldFrame)
        {
            ReleaseHeldFrameBestEffort(_previousHeldFrame, "previous_frame");
            _previousHeldFrame = default;
            _hasPreviousHeldFrame = false;
        }
    }

    private void ReleasePlaybackFrameForLive(string operation)
    {
        Interlocked.Exchange(ref _lastAudioPtsTicks, 0);
        Interlocked.Exchange(ref _lastVideoPtsTicks, 0);

        if (_hasPreviousHeldFrame)
        {
            Logger.Log($"FLASHBACK_PLAYBACK_RELEASE_HELD_FOR_LIVE op={operation}");
        }

        ReleasePreviousHeldFrame();
    }

    private void RestoreLiveAfterSeekDisplayFailure(FlashbackDecoder decoder, ref bool fileOpen, string operation)
    {
        CloseDecoderFileBestEffort(decoder, operation);
        fileOpen = false;
        _currentOpenFilePath = null;
        _decoderHwAccel = "N/A";
        ReleasePlaybackFrameForLive(operation);
        RestoreLiveAudio();
        SafeResumePreviewSubmission(operation);
        SafeResumeRendering(operation);
        SetState(FlashbackPlaybackState.Live);
    }

    private bool TrySubmitAndHoldFrame(DecodedVideoFrame frame, string operation)
    {
        var previewSink = Volatile.Read(ref _previewSink);
        if (previewSink == null)
        {
            Interlocked.Increment(ref _playbackSubmitFailures);
            SetLastSubmitFailure($"{operation}:missing_preview_sink");
            ReleaseHeldFrameBestEffort(frame, $"{operation}_missing_preview_sink");
            Logger.Log($"FLASHBACK_PLAYBACK_SUBMIT_SKIP op={operation} reason=missing_preview_sink");
            return false;
        }

        if (!TryValidatePreviewFrame(frame, out var skipReason))
        {
            Interlocked.Increment(ref _playbackSubmitFailures);
            SetLastSubmitFailure($"{operation}:{skipReason}");
            ReleaseHeldFrameBestEffort(frame, $"{operation}_{skipReason}");
            Logger.Log($"FLASHBACK_PLAYBACK_SUBMIT_SKIP op={operation} reason={skipReason}");
            return false;
        }

        try
        {
            SubmitFrame(previewSink, frame);
            ReleasePreviousHeldFrame();
            _previousHeldFrame = frame;
            _hasPreviousHeldFrame = true;
            ClearLastSubmitFailure();
            return true;
        }
        catch (Exception ex)
        {
            Interlocked.Increment(ref _playbackSubmitFailures);
            SetLastSubmitFailure($"{operation}:submit_fail:{ex.GetType().Name}");
            ReleaseHeldFrameBestEffort(frame, $"{operation}_submit_fail");
            Logger.Log($"FLASHBACK_PLAYBACK_SUBMIT_FAIL op={operation} type={ex.GetType().Name} msg='{ex.Message}'");
            return false;
        }
    }

    private static bool TryValidatePreviewFrame(DecodedVideoFrame frame, out string reason)
    {
        if (frame.Width <= 0 || frame.Height <= 0 || (frame.Width & 1) != 0 || (frame.Height & 1) != 0)
        {
            reason = "invalid_dimensions";
            return false;
        }

        if (frame.IsD3D11Texture)
        {
            if (frame.TexturePtr == IntPtr.Zero)
            {
                reason = "null_texture";
                return false;
            }

            if (frame.SubresourceIndex < 0)
            {
                reason = "invalid_subresource";
                return false;
            }

            reason = string.Empty;
            return true;
        }

        if (frame.Data == IntPtr.Zero)
        {
            reason = "null_data";
            return false;
        }

        if (frame.DataLength <= 0)
        {
            reason = "invalid_data_length";
            return false;
        }

        if (!TryCalculatePreviewFrameBytes(frame.Width, frame.Height, frame.IsHdr, out var expectedBytes))
        {
            reason = "invalid_dimensions";
            return false;
        }

        if (frame.DataLength < expectedBytes)
        {
            reason = "short_data_length";
            return false;
        }

        reason = string.Empty;
        return true;
    }

    private static bool TryCalculatePreviewFrameBytes(int width, int height, bool isHdr, out int bytes)
    {
        bytes = 0;
        if (width <= 0 || height <= 0 || (width & 1) != 0 || (height & 1) != 0)
        {
            return false;
        }

        var pixels = (long)width * height;
        var calculated = isHdr
            ? pixels * 3
            : pixels + width * (long)(height / 2);
        if (calculated <= 0 || calculated > int.MaxValue)
        {
            return false;
        }

        bytes = (int)calculated;
        return true;
    }

    private static void ReleaseHeldFrameBestEffort(DecodedVideoFrame frame, string operation)
    {
        try
        {
            FlashbackDecoder.ReleaseHeldFrame(frame);
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_PLAYBACK_RELEASE_HELD_FRAME_WARN op={operation} type={ex.GetType().Name} msg='{ex.Message}'");
        }
    }

    private bool SeekAndDisplayKeyframe(
        FlashbackDecoder decoder,
        ref bool fileOpen,
        TimeSpan bufferPosition,
        TimeSpan validStartPts,
        CommandKind kind,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        // Suppress audio delivery during scrub — prevents audio accumulation
        // in the WASAPI queue. Audio callback is re-enabled on Play/EndScrub.
        decoder.AudioChunkCallback = null;
        SafeFlushPlayback("seek_display_keyframe");

        bufferPosition = ClampPosition(bufferPosition, validStartPts);

        if (!decoder.IsOpen)
        {
            // No file — use requested position as fallback
            PlaybackPosition = bufferPosition;
            SetSeekDisplayFailure(kind, "no_file", bufferPosition);
            Logger.Log($"FLASHBACK_PLAYBACK_SEEK_NO_FILE pos_ms={(long)bufferPosition.TotalMilliseconds}");
            return false;
        }

        try
        {
            // Map buffer position to file PTS (offset by frozen valid start)
            var filePts = SaturatingAdd(bufferPosition, validStartPts);
            cancellationToken.ThrowIfCancellationRequested();

            // Clamp to current valid range: if eviction advanced ValidStartPts past
            // frozenValidStart, positions near the left edge map to evicted data.
            var currentValidStart = _bufferManager.ValidStartPts;
            if (filePts < currentValidStart)
            {
                filePts = currentValidStart;
                bufferPosition = SaturatingSubtract(filePts, validStartPts);
                if (bufferPosition < TimeSpan.Zero) bufferPosition = TimeSpan.Zero;
            }

            if (!decoder.SeekToKeyframe(filePts, cancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();
                // Active fMP4 segment: demuxer caches fragment index at open time.
                // New fragments written since open aren't visible — reopen and retry.
                // Only for fMP4; .ts handles appended data via eof_reached reset.
                if (IsActiveFmp4Segment(_currentOpenFilePath) && _currentOpenFilePath != null)
                {
                    if (!ShouldSkipActiveFmp4ReopenNearLive(filePts, "seek_keyframe"))
                    {
                        Logger.Log($"FLASHBACK_PLAYBACK_SEEK_REOPEN_ACTIVE offset_ms={(long)filePts.TotalMilliseconds}");
                        if (TryReopenCurrentFileAndSeekKeyframe(decoder, ref fileOpen, filePts, "seek_keyframe", cancellationToken))
                            goto seekSuccess;
                    }
                }

                PlaybackPosition = bufferPosition;
                SetSeekDisplayFailure(kind, "seek_failed", bufferPosition);
                Logger.Log($"FLASHBACK_PLAYBACK_SEEK_FAIL offset_ms={(long)filePts.TotalMilliseconds}");
                return false;
            }
            seekSuccess:
            cancellationToken.ThrowIfCancellationRequested();

            var gotFrame = TryDecodeNextVideoFrameWithMetrics(decoder, out var frame, cancellationToken);
            var frameOwned = gotFrame;
            try
            {
                if (!gotFrame &&
                    TrySeekAdjacentSegmentStart(decoder, ref fileOpen, filePts, $"seek_display:{kind}", out var adjacentFilePts, cancellationToken))
                {
                    filePts = adjacentFilePts;
                    cancellationToken.ThrowIfCancellationRequested();
                    gotFrame = TryDecodeNextVideoFrameWithMetrics(decoder, out frame, cancellationToken);
                    frameOwned = gotFrame;
                }

                if (gotFrame)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var submitted = TrySubmitAndHoldFrame(frame, "seek");
                    frameOwned = false;
                    if (!submitted)
                    {
                        PlaybackPosition = bufferPosition;
                        SetSeekDisplayFailure(kind, "submit_failed", bufferPosition);
                        return false;
                    }
                    Interlocked.Exchange(ref _lastVideoPtsTicks, frame.Pts.Ticks);

                    // Set position to actual decoded frame PTS mapped back to buffer position
                    var actualPosition = SaturatingSubtract(frame.Pts, validStartPts);
                    if (actualPosition < TimeSpan.Zero) actualPosition = TimeSpan.Zero;
                    PlaybackPosition = actualPosition;
                }
                else
                {
                    // No frame decoded — use requested position as fallback
                    PlaybackPosition = bufferPosition;
                    SetSeekDisplayFailure(kind, "no_frame", bufferPosition);
                }
            }
            finally
            {
                if (frameOwned)
                {
                    ReleaseHeldFrameBestEffort(frame, "seek_cancelled");
                }
            }

            Logger.Log($"FLASHBACK_PLAYBACK_SEEK_OK pos_ms={(long)PlaybackPosition.TotalMilliseconds} file_pts_ms={(long)filePts.TotalMilliseconds} got_frame={gotFrame}");
            return gotFrame;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            // On error, use requested position as fallback
            PlaybackPosition = bufferPosition;
            SetSeekDisplayFailure(kind, ex.GetType().Name, bufferPosition);
            Logger.Log($"FLASHBACK_PLAYBACK_SEEK_ERROR type={ex.GetType().Name} error='{ex.Message}'");
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

    /// <summary>
    /// Decodes and submits the next frame at real-time pace.
    /// Decode-first structure: do the work, then wait for the remainder of the frame interval.
    /// Uses sleep + spin-wait hybrid for sub-millisecond accuracy at 120fps.
    /// When the decoder can't keep up (drift > 200ms), skips frames without display
    /// to maintain audio synchronization.
    /// Returns true if still playing, false if transitioned to another state.
    /// </summary>
    private bool PaceAndDecodeFrame(
        FlashbackDecoder decoder,
        Channel<PlaybackCommand> commandChannel,
        Stopwatch pacingStopwatch,
        ref TimeSpan frameDuration,
        ref bool fileOpen,
        TimeSpan frozenValidStart,
        CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!TryDecodeNextVideoFrameWithMetrics(decoder, out var videoFrame, cancellationToken))
            {
                return HandleEndOfSegment(decoder, commandChannel, pacingStopwatch, frozenValidStart, ref fileOpen, cancellationToken);
            }
            if (ShouldSnapLiveForSoftwarePlaybackBudget(decoder, out _, out _))
            {
                ReleaseHeldFrameBestEffort(videoFrame, "software_decode_over_budget");
                SnapLiveForSoftwarePlaybackBudget(decoder, ref fileOpen, "playback_decode");
                return false;
            }

            // Frame skip: when video falls significantly behind audio, decode-and-discard
            // frames to catch up rather than falling further behind. This handles codecs
            // whose decode time exceeds the frame interval (e.g. AV1 at 4K@120fps where
            // each decode takes ~25ms but frame interval is 8.33ms).
            //
            // The drift recompute MUST re-sync the audio clock each iteration: a single
            // skip can take ~25ms, during which the WASAPI render thread has likely
            // advanced _audioClockPtsTicks. Extrapolating from the original capture
            // diverges from the actual audio clock the longer the loop runs and can
            // either exit early (false-recovered) or burn the full skip cap unnecessarily.
            const double FrameSkipThresholdMs = 500.0;
            const int MaxSkipFrames = 30; // cap to prevent infinite skip loops
            if (TryComputeAudioMasterDriftMs(videoFrame.Pts.Ticks, out var driftMs) &&
                driftMs < -FrameSkipThresholdMs)
            {
                var skipped = 0;
                while (skipped < MaxSkipFrames && driftMs < -FrameSkipThresholdMs)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    // Release the frame without displaying it
                    ReleaseHeldFrameBestEffort(videoFrame, "av_sync_skip");
                    RecordPlaybackDroppedFrame("av_sync_skip");
                    skipped++;

                    if (!TryDecodeNextVideoFrameWithMetrics(decoder, out videoFrame, cancellationToken))
                    {
                        // EOS during skip — log partial progress so the diagnostic gap
                        // doesn't hide a long catch-up burst that the user may notice.
                        if (skipped > 0)
                        {
                            Logger.Log($"FLASHBACK_PLAYBACK_FRAME_SKIP_EOS count={skipped} drift_at_eos_ms={driftMs:F1}");
                        }
                        return HandleEndOfSegment(decoder, commandChannel, pacingStopwatch, frozenValidStart, ref fileOpen, cancellationToken);
                    }
                    if (ShouldSnapLiveForSoftwarePlaybackBudget(decoder, out _, out _))
                    {
                        if (skipped > 0)
                        {
                            Logger.Log($"FLASHBACK_PLAYBACK_FRAME_SKIP_BUDGET count={skipped} drift_at_budget_ms={driftMs:F1}");
                        }
                        ReleaseHeldFrameBestEffort(videoFrame, "software_decode_over_budget");
                        SnapLiveForSoftwarePlaybackBudget(decoder, ref fileOpen, "playback_skip");
                        return false;
                    }

                    // Recompute with a freshly-sampled audio clock; if WASAPI is now
                    // stale or unavailable, exit the skip loop to avoid extrapolating
                    // off a stale reference for the rest of the catch-up.
                    if (!TryComputeAudioMasterDriftMs(videoFrame.Pts.Ticks, out driftMs))
                    {
                        break;
                    }
                }

                if (skipped > 0)
                {
                    Logger.Log($"FLASHBACK_PLAYBACK_FRAME_SKIP count={skipped} drift_after_ms={driftMs:F1}");
                }
            }

            if (!TrySubmitAndHoldFrame(videoFrame, "playback"))
            {
                SetState(FlashbackPlaybackState.Paused);
                Logger.Log($"FLASHBACK_PLAYBACK_SUBMIT_STOP pos_ms={(long)PlaybackPosition.TotalMilliseconds}");
                return false;
            }
            Interlocked.Exchange(ref _lastVideoPtsTicks, videoFrame.Pts.Ticks);

            var newPosition = SaturatingSubtract(videoFrame.Pts, frozenValidStart);
            if (newPosition < TimeSpan.Zero) newPosition = TimeSpan.Zero;
            PlaybackPosition = newPosition;

            if (CheckOutPoint(newPosition, pacingStopwatch))
                return false;

            if (CheckNearLiveEdge(decoder, videoFrame.Pts, newPosition, ref fileOpen))
                return false;

            // Use the encoder's frame rate as ground truth — the buffer manager knows
            // exactly what rate we told NVENC to encode at. TS container metadata can
            // report doubled rates (e.g. 240 for 120fps) and the decoder's PTS calibration
            // needs ~10 frames to correct. The encode rate is authoritative from frame 1.
            frameDuration = ResolveFrameDuration(decoder);
            TrackDecodedPtsCadence(videoFrame.Pts, frameDuration);

            PaceFrameInterval(pacingStopwatch, frameDuration, videoFrame.Pts.Ticks);
            UpdateCadenceMetrics(pacingStopwatch, frameDuration.TotalMilliseconds);

            // Log A/V drift every ~1 second for diagnostics
            if (_playbackFrameCount % 120 == 0)
            {
                var drift = AvDriftMs;
                var audioClock = Volatile.Read(ref _audioClockPtsTicks);
                Logger.Log($"FLASHBACK_AV_DRIFT frame={_playbackFrameCount} drift_ms={drift:F1} videoPts_ms={(long)videoFrame.Pts.TotalMilliseconds} audioClock_ms={audioClock / TimeSpan.TicksPerMillisecond}");
            }

            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            SnapToLiveOnError(decoder, ex, ref fileOpen);
            return false;
        }
    }

    private bool HandleEndOfSegment(
        FlashbackDecoder decoder,
        Channel<PlaybackCommand> commandChannel,
        Stopwatch pacingStopwatch,
        TimeSpan frozenValidStart,
        ref bool fileOpen,
        CancellationToken cancellationToken)
    {
        // Use absolute PTS to measure distance from live edge.
        // PlaybackPosition uses frozenValidStart (captured at scrub time) while
        // BufferedDuration uses the current ValidStartPts (moves as segments are
        // evicted). Mixing these coordinate systems causes a permanently negative
        // gap once eviction advances ValidStartPts past frozenValidStart.
        var latestAbsPts = _bufferManager.LatestPts;
        var lastFrameAbsPts = TimeSpan.FromTicks(Interlocked.Read(ref _lastVideoPtsTicks));
        // Fallback: if no frame was decoded yet, estimate from PlaybackPosition
        if (lastFrameAbsPts == TimeSpan.Zero)
            lastFrameAbsPts = SaturatingAdd(PlaybackPosition, frozenValidStart);
        var gapFromLive = SaturatingSubtract(latestAbsPts, lastFrameAbsPts).TotalMilliseconds;
        var pos = PlaybackPosition;
        var currentOpenFilePath = _currentOpenFilePath;

        if (IsActiveFmp4Segment(currentOpenFilePath) &&
            CheckNearLiveEdge(decoder, lastFrameAbsPts, pos, ref fileOpen, requireFrameWarmup: false))
        {
            pacingStopwatch.Restart();
            return false;
        }

        if (gapFromLive > 2000)
        {
            var nextFile = currentOpenFilePath != null
                ? _bufferManager.GetNextSegmentFile(currentOpenFilePath)
                : null;
            if (nextFile != null && !IsSamePlaybackPath(nextFile, currentOpenFilePath))
            {
                var nextSegmentStart = _bufferManager.GetSegmentStartPts(nextFile);
                if (currentOpenFilePath != null &&
                    nextSegmentStart.HasValue &&
                    nextSegmentStart.Value - lastFrameAbsPts > TimeSpan.FromMilliseconds(250))
                {
                    Interlocked.Increment(ref _playbackFmp4Reopens);
                    Interlocked.Exchange(ref _lastFmp4ReopenUtcUnixMs, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
                    Logger.Log($"FLASHBACK_PLAYBACK_FMP4_REOPEN_BEFORE_SEGMENT_SWITCH pos_ms={(long)pos.TotalMilliseconds} resumePts_ms={(long)lastFrameAbsPts.TotalMilliseconds} nextStart_ms={(long)nextSegmentStart.Value.TotalMilliseconds}");
                    try
                    {
                        decoder.CloseFile();
                        fileOpen = false;
                        decoder.OpenFile(currentOpenFilePath);
                        fileOpen = true;
                        _decoderHwAccel = decoder.IsD3D11HwAccelerated ? "D3D11VA" : "Software";
                        var preReopenLastAudioPts = Interlocked.Read(ref _lastAudioPtsTicks);
                        Interlocked.Increment(ref _playbackReopenAudioNullWindowCount);
                        decoder.AudioChunkCallback = null;
                        cancellationToken.ThrowIfCancellationRequested();
                        if (decoder.SeekTo(lastFrameAbsPts, cancellationToken))
                        {
                            // Gate audio at the post-seek video PTS (seek target), not at
                            // _lastAudioPtsTicks. _lastAudioPtsTicks reflects pre-seek state;
                            // using it suppresses audio if the seek lands earlier, or creates
                            // a gap if it lands later, causing WASAPI underruns and A/V desync.
                            var audioGateTicks = lastFrameAbsPts.Ticks;
                            Logger.Log($"FLASHBACK_PLAYBACK_REOPEN_AUDIO_GATE gate_ms={(long)lastFrameAbsPts.TotalMilliseconds} source=PostSeekVideoPts last_audio_ms={preReopenLastAudioPts / TimeSpan.TicksPerMillisecond} seek_target_ms={(long)lastFrameAbsPts.TotalMilliseconds}");
                            RestoreAudioCallback(decoder, audioGateTicks);
                            pacingStopwatch.Restart();
                            return true;
                        }
                    }
                    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        Logger.Log($"FLASHBACK_PLAYBACK_FMP4_REOPEN_BEFORE_SEGMENT_SWITCH_ERROR path='{currentOpenFilePath}' type={ex.GetType().Name} msg='{ex.Message}'");
                    }
                }

                Interlocked.Increment(ref _playbackSegmentSwitches);
                Interlocked.Exchange(ref _lastSegmentSwitchUtcUnixMs, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
                Logger.Log($"FLASHBACK_PLAYBACK_SEGMENT_SWITCH pos_ms={(long)pos.TotalMilliseconds} next='{System.IO.Path.GetFileName(nextFile)}'");
                try
                {
                    decoder.CloseFile();
                    fileOpen = false;
                    decoder.OpenFile(nextFile);
                    fileOpen = true;
                    _currentOpenFilePath = nextFile;
                    _decoderHwAccel = decoder.IsD3D11HwAccelerated ? "D3D11VA" : "Software";
                    // Gate audio at last played position, not seek target — audio between
                    // the last played sample and the seek point would otherwise be dropped,
                    // causing an audible gap at segment boundaries.
                    var audioGate = Interlocked.Read(ref _lastAudioPtsTicks);
                    decoder.AudioChunkCallback = null;
                    var segSwitchTarget = SaturatingAdd(pos, frozenValidStart);
                    if (nextSegmentStart.HasValue && segSwitchTarget < nextSegmentStart.Value)
                        segSwitchTarget = nextSegmentStart.Value;
                    cancellationToken.ThrowIfCancellationRequested();
                    if (!decoder.SeekTo(segSwitchTarget, cancellationToken))
                    {
                        SetReopenFailure("segment_switch", "seek_failed", segSwitchTarget);
                        Logger.Log($"FLASHBACK_PLAYBACK_SEGMENT_SWITCH_SEEK_FAIL path='{nextFile}' offset_ms={(long)segSwitchTarget.TotalMilliseconds}");
                        RestoreLiveAfterSeekDisplayFailure(decoder, ref fileOpen, "segment_switch_seek_failed");
                        return false;
                    }
                    RestoreAudioCallback(decoder, audioGate);
                    pacingStopwatch.Restart();
                    return true;
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    Logger.Log($"FLASHBACK_PLAYBACK_SEGMENT_SWITCH_ERROR path='{nextFile}' type={ex.GetType().Name} msg='{ex.Message}'");
                    SnapToLiveOnError(decoder, ex, ref fileOpen);
                    return false;
                }
            }

            // Active fMP4 segment: the demuxer cached the file structure at open
            // time and won't see new fragments without re-opening. Close and re-open
            // the same file, then seek to where playback left off.
            // Only for fMP4; .ts handles appended data via eof_reached reset.
            if (IsActiveFmp4Segment(currentOpenFilePath) && currentOpenFilePath != null)
            {
                Interlocked.Increment(ref _playbackFmp4Reopens);
                Interlocked.Exchange(ref _lastFmp4ReopenUtcUnixMs, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
                var resumeTarget = lastFrameAbsPts;
                var currentSegmentStart = _bufferManager.GetSegmentStartPts(currentOpenFilePath);
                if (currentSegmentStart.HasValue && resumeTarget < currentSegmentStart.Value)
                    resumeTarget = currentSegmentStart.Value;
                Logger.Log($"FLASHBACK_PLAYBACK_FMP4_REOPEN pos_ms={(long)pos.TotalMilliseconds} resumePts_ms={(long)resumeTarget.TotalMilliseconds}");
                try
                {
                    decoder.CloseFile();
                    fileOpen = false;
                    decoder.OpenFile(currentOpenFilePath);
                    fileOpen = true;
                    _decoderHwAccel = decoder.IsD3D11HwAccelerated ? "D3D11VA" : "Software";
                    var preReopenLastAudioPts = Interlocked.Read(ref _lastAudioPtsTicks);
                    Interlocked.Increment(ref _playbackReopenAudioNullWindowCount);
                    decoder.AudioChunkCallback = null;
                    cancellationToken.ThrowIfCancellationRequested();
                    if (!decoder.SeekTo(resumeTarget, cancellationToken))
                    {
                        SetReopenFailure("fmp4_reopen", "seek_failed", resumeTarget);
                        Logger.Log($"FLASHBACK_PLAYBACK_FMP4_REOPEN_SEEK_FAIL path='{currentOpenFilePath}' offset_ms={(long)resumeTarget.TotalMilliseconds}");
                        RestoreLiveAfterSeekDisplayFailure(decoder, ref fileOpen, "fmp4_reopen_seek_failed");
                        return false;
                    }
                    // Gate audio at the post-seek video PTS (seek target), not at
                    // _lastAudioPtsTicks. _lastAudioPtsTicks reflects pre-seek state;
                    // using it suppresses audio if the seek lands earlier, or creates
                    // a gap if it lands later, causing WASAPI underruns and A/V desync.
                    var audioGateTicks = resumeTarget.Ticks;
                    Logger.Log($"FLASHBACK_PLAYBACK_REOPEN_AUDIO_GATE gate_ms={(long)resumeTarget.TotalMilliseconds} source=PostSeekVideoPts last_audio_ms={preReopenLastAudioPts / TimeSpan.TicksPerMillisecond} seek_target_ms={(long)resumeTarget.TotalMilliseconds}");
                    RestoreAudioCallback(decoder, audioGateTicks);
                    pacingStopwatch.Restart();
                    return true;
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    Logger.Log($"FLASHBACK_PLAYBACK_FMP4_REOPEN_ERROR path='{currentOpenFilePath}' type={ex.GetType().Name} msg='{ex.Message}'");
                    SnapToLiveOnError(decoder, ex, ref fileOpen);
                    return false;
                }
            }
        }

        if (commandChannel.Reader.TryPeek(out _) || _disposedFlag != 0)
        {
            pacingStopwatch.Restart();
            return true;
        }

        Interlocked.Increment(ref _playbackWriteHeadWaits);
        Interlocked.Exchange(ref _lastWriteHeadWaitGapMs, Math.Max(0, (long)gapFromLive));
        Logger.Log($"FLASHBACK_PLAYBACK_WRITE_HEAD_WAIT gapFromLive_ms={gapFromLive:F0} pos_ms={(long)pos.TotalMilliseconds} lastFrameAbsPts_ms={(long)lastFrameAbsPts.TotalMilliseconds} latestPts_ms={(long)latestAbsPts.TotalMilliseconds}");
        if (cancellationToken.WaitHandle.WaitOne(50))
        {
            return false;
        }

        pacingStopwatch.Restart();
        return true;
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

    private bool CheckNearLiveEdge(
        FlashbackDecoder decoder,
        TimeSpan absoluteFramePts,
        TimeSpan bufferPosition,
        ref bool fileOpen,
        bool requireFrameWarmup = true)
    {
        var absoluteLatestPts = _bufferManager.LatestPts;
        var gapFromLive = SaturatingSubtract(absoluteLatestPts, absoluteFramePts);
        if ((!requireFrameWarmup || Interlocked.Read(ref _playbackFrameCount) > 60) &&
            gapFromLive <= TimeSpan.FromMilliseconds(2000))
        {
            Interlocked.Increment(ref _playbackNearLiveSnaps);
            var gapMs = gapFromLive.TotalMilliseconds;
            Logger.Log($"FLASHBACK_PLAYBACK_NEAR_LIVE_SNAP pos_ms={(long)bufferPosition.TotalMilliseconds} framePts_ms={(long)absoluteFramePts.TotalMilliseconds} latestPts_ms={(long)absoluteLatestPts.TotalMilliseconds} gapFromLive_ms={gapMs:F0} frameCount={_playbackFrameCount}");
            CloseDecoderFileBestEffort(decoder, "near_live");
            fileOpen = false;
            _currentOpenFilePath = null;
            _decoderHwAccel = "N/A";
            Interlocked.Exchange(ref _lastAudioPtsTicks, 0);
            Interlocked.Exchange(ref _lastVideoPtsTicks, 0);
            ReleasePlaybackFrameForLive("near_live");
            RestoreLiveAudio();
            SafeResumePreviewSubmission("near_live");
            SetState(FlashbackPlaybackState.Live);
            return true;
        }
        return false;
    }

    private TimeSpan ResolvePauseFromLiveTarget(TimeSpan frozenValidStart)
    {
        var latestPts = _bufferManager.LatestPts;
        if (latestPts <= frozenValidStart)
        {
            return frozenValidStart;
        }

        var fps = _bufferManager.EncodeFrameRate;
        if (!double.IsFinite(fps) || fps <= 0)
        {
            fps = FallbackPlaybackFrameRate;
        }

        fps = Math.Min(fps, MaxPlaybackFrameRate);
        var backoff = TimeSpan.FromSeconds(1.0 / fps);
        if (latestPts - frozenValidStart <= backoff)
        {
            return latestPts;
        }

        return latestPts - backoff;
    }

    private TimeSpan ResolveFrameDuration(FlashbackDecoder decoder)
    {
        // The encode rate is authoritative when present. Decoder/container metadata
        // can be wrong, and invalid floating-point values must never tear down playback.
        var fps = ResolvePlaybackFrameRate(decoder);
        _playbackTargetFps = fps;
        return TimeSpan.FromSeconds(1.0 / fps);
    }

    private double ResolvePlaybackFrameRate(FlashbackDecoder decoder)
    {
        var fps = _bufferManager.EncodeFrameRate;
        if (!double.IsFinite(fps) || fps <= 0)
        {
            fps = decoder.FrameRate;
        }

        if (!double.IsFinite(fps) || fps <= 0)
        {
            fps = FallbackPlaybackFrameRate;
        }

        fps = Math.Min(fps, MaxPlaybackFrameRate);
        return fps;
    }

    private bool TrySnapLiveForSoftwarePlaybackBudget(FlashbackDecoder decoder, ref bool fileOpen, string operation)
    {
        if (!ShouldSnapLiveForSoftwarePlaybackBudget(decoder, out _, out _))
        {
            UpdateDecoderHwAccel(decoder);
            return false;
        }

        SnapLiveForSoftwarePlaybackBudget(decoder, ref fileOpen, operation);
        return true;
    }

    private bool ShouldSnapLiveForSoftwarePlaybackBudget(
        FlashbackDecoder decoder,
        out double fps,
        out double pixelRate)
    {
        UpdateDecoderHwAccel(decoder);
        fps = ResolvePlaybackFrameRate(decoder);
        pixelRate = Math.Max(0, decoder.VideoWidth) * (double)Math.Max(0, decoder.VideoHeight) * fps;
        return GpuDecodeEnabled &&
               !decoder.IsD3D11HwAccelerated &&
               pixelRate > MaxContinuousSoftwarePlaybackPixelRate;
    }

    private void SnapLiveForSoftwarePlaybackBudget(FlashbackDecoder decoder, ref bool fileOpen, string operation)
    {
        ShouldSnapLiveForSoftwarePlaybackBudget(decoder, out var fps, out var pixelRate);
        Interlocked.Increment(ref _playbackDecodeErrorSnaps);
        RecordPlaybackDroppedFrame("software_decode_over_budget");
        var pos = PlaybackPosition;
        SetLastCommandFailure($"software_decode_over_budget:{operation}{FormatCommandDetail(position: pos)}");
        Logger.Log(
            $"FLASHBACK_PLAYBACK_SOFTWARE_DECODE_SNAP_TO_LIVE op={operation} width={decoder.VideoWidth} height={decoder.VideoHeight} fps={fps:F2} pixel_rate={pixelRate:F0} max_pixel_rate={MaxContinuousSoftwarePlaybackPixelRate:F0}");
        CloseDecoderFileBestEffort(decoder, operation);
        fileOpen = false;
        _currentOpenFilePath = null;
        _decoderHwAccel = "N/A";
        ReleasePlaybackFrameForLive(operation);
        RestoreLiveAudio();
        SafeResumePreviewSubmission(operation);
        SafeResumeRendering(operation);
        SetState(FlashbackPlaybackState.Live);
    }

    private void UpdateDecoderHwAccel(FlashbackDecoder decoder)
    {
        _decoderHwAccel = decoder.IsD3D11HwAccelerated ? "D3D11VA" : "Software";
    }

    /// <summary>
    /// Audio-master pacing. Video and audio are decoded from the same interleaved
    /// container on the same thread — their PTS are the source of truth.
    /// Without suppression, audio and video start at the same file position after
    /// seek, so the initial offset should be near-zero. This method corrects any
    /// drift that develops over time (hardware clock vs decode rate).
    /// Falls back to wall-clock pacing when audio is unavailable.
    /// </summary>
    private void PaceFrameInterval(Stopwatch pacingStopwatch, TimeSpan frameDuration, long videoPtsTicks)
    {
        var audioPb = _audioPlayback;
        var renderingPts = audioPb?.RenderingPtsTicks ?? 0;

        // Update audio clock extrapolation state when WASAPI reports a new PTS
        if (renderingPts > 0 && renderingPts != Volatile.Read(ref _audioClockPtsTicks))
        {
            Interlocked.Exchange(ref _audioClockPtsTicks, renderingPts);
            Interlocked.Exchange(ref _audioClockWallTicks, Stopwatch.GetTimestamp());
        }

        var audioClockPts = Volatile.Read(ref _audioClockPtsTicks);
        var audioClockWall = Volatile.Read(ref _audioClockWallTicks);
        var wallElapsed = Stopwatch.GetTimestamp() - audioClockWall;
        var wallElapsedTicks = (long)((double)wallElapsed / Stopwatch.Frequency * TimeSpan.TicksPerSecond);

        // If the audio clock hasn't been updated in >200ms, WASAPI is likely underrunning —
        // fall through to wall-clock pacing instead of extrapolating against a stale sample.
        const long StaleThresholdTicks = TimeSpan.TicksPerMillisecond * 200;
        if (audioClockPts > 0 && wallElapsedTicks <= StaleThresholdTicks)
        {
            // Extrapolate: audioClock = lastSampledPts + wallElapsedSinceSample
            var extrapolatedAudioTicks = audioClockPts + wallElapsedTicks;

            // diff > 0 = video ahead of audio, < 0 = video behind
            var diffTicks = videoPtsTicks - extrapolatedAudioTicks;
            var diffMs = diffTicks / (double)TimeSpan.TicksPerMillisecond;
            var nominalDelayMs = frameDuration.TotalMilliseconds;

            // At HFR, per-frame corrections are very visible. Short fMP4
            // fragments keep audio close, so tolerate sub-100ms drift and only
            // correct when sync moves outside that band.
            const double syncThresholdMs = 100.0;
            const double MaxAudioMasterCorrectionMs = 250.0;

            if (Math.Abs(diffMs) > MaxAudioMasterCorrectionMs)
            {
                // WASAPI render PTS can lag decoded video by the endpoint buffer/device
                // latency after resume. Do not let that stale clock halve video cadence.
                RecordAudioMasterFallback("drift-outlier", diffMs, wallElapsedTicks);
                WallClockPace(pacingStopwatch, frameDuration);
                return;
            }

            double adjustedDelayMs;
            if (diffMs > syncThresholdMs)
            {
                // Video ahead: add a tiny correction without tanking HFR cadence.
                Interlocked.Increment(ref _playbackAudioMasterDelayDoubles);
                var correctionMs = Math.Min(diffMs - syncThresholdMs, Math.Min(0.1, nominalDelayMs * 0.02));
                adjustedDelayMs = nominalDelayMs + Math.Max(0, correctionMs);
            }
            else if (diffMs < -syncThresholdMs)
            {
                // Video behind: shave a tiny correction without creating bursts.
                Interlocked.Increment(ref _playbackAudioMasterDelayShrinks);
                var correctionMs = Math.Min(-diffMs - syncThresholdMs, Math.Min(0.1, nominalDelayMs * 0.02));
                adjustedDelayMs = Math.Max(0, nominalDelayMs - Math.Max(0, correctionMs));
                if (adjustedDelayMs <= 0)
                    Interlocked.Increment(ref _playbackLateFrames);
            }
            else
            {
                // Within threshold — smooth wall-clock cadence
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
                        if (sleepMs > 0) Thread.Sleep(sleepMs);
                    }
                    while (pacingStopwatch.ElapsedTicks < targetTicks)
                        Thread.SpinWait(1);
                }
            }
            return;
        }

        // Fallback: no audio clock available — pure wall-clock pacing
        var fallbackReason = audioClockPts <= 0 ? "unavailable" : "stale-clock";
        RecordAudioMasterFallback(fallbackReason, 0, audioClockPts <= 0 ? 0 : wallElapsedTicks);
        WallClockPace(pacingStopwatch, frameDuration);
    }

    private void RecordAudioMasterFallback(string reason, double driftMs, long clockAgeTicks)
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

    /// <summary>
    /// Re-syncs the cached audio clock from WASAPI (matching the resync done at the top
    /// of <see cref="PaceFrameInterval"/>) and returns the extrapolated drift in
    /// milliseconds (positive = video ahead of audio). Returns false if the audio clock
    /// is unavailable, has never been sampled, or is stale (>200ms since last update) —
    /// callers must fall back to wall-clock pacing in that case.
    /// </summary>
    private bool TryComputeAudioMasterDriftMs(long videoPtsTicks, out double driftMs)
    {
        driftMs = 0;

        var audioPb = _audioPlayback;
        var renderingPts = audioPb?.RenderingPtsTicks ?? 0;
        if (renderingPts > 0 && renderingPts != Volatile.Read(ref _audioClockPtsTicks))
        {
            Interlocked.Exchange(ref _audioClockPtsTicks, renderingPts);
            Interlocked.Exchange(ref _audioClockWallTicks, Stopwatch.GetTimestamp());
        }

        var audioClockPts = Volatile.Read(ref _audioClockPtsTicks);
        if (audioClockPts <= 0)
        {
            return false;
        }

        var audioClockWall = Volatile.Read(ref _audioClockWallTicks);
        var wallElapsed = Stopwatch.GetTimestamp() - audioClockWall;
        var wallElapsedTicks = (long)((double)wallElapsed / Stopwatch.Frequency * TimeSpan.TicksPerSecond);
        const long StaleThresholdTicks = TimeSpan.TicksPerMillisecond * 200;
        if (wallElapsedTicks > StaleThresholdTicks)
        {
            return false;
        }

        var extrapolatedAudioTicks = audioClockPts + wallElapsedTicks;
        driftMs = (videoPtsTicks - extrapolatedAudioTicks) / (double)TimeSpan.TicksPerMillisecond;
        return true;
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
                if (sleepMs > 0) Thread.Sleep(sleepMs);
            }
            while (pacingStopwatch.ElapsedTicks < targetTicks)
                Thread.SpinWait(1);
        }
        else
        {
            Interlocked.Increment(ref _playbackLateFrames);
        }
    }

    private void TrackDecodedPtsCadence(TimeSpan pts, TimeSpan expectedFrameDuration)
    {
        if (pts <= TimeSpan.Zero || expectedFrameDuration <= TimeSpan.Zero)
        {
            return;
        }

        var currentTicks = pts.Ticks;
        var previousTicks = Volatile.Read(ref _lastPlaybackCadencePtsTicks);
        if (previousTicks <= 0)
        {
            Interlocked.Exchange(ref _lastPlaybackCadencePtsTicks, currentTicks);
            return;
        }

        var deltaTicks = currentTicks - previousTicks;
        var deltaMs = deltaTicks / (double)TimeSpan.TicksPerMillisecond;
        var expectedMs = expectedFrameDuration.TotalMilliseconds;
        var toleranceMs = Math.Max(2.0, expectedMs * 0.25);
        if (deltaTicks <= 0)
        {
            RecordPlaybackPtsCadenceMismatch(deltaMs, expectedMs, toleranceMs, pts);
            return;
        }

        Interlocked.Exchange(ref _lastPlaybackCadencePtsTicks, currentTicks);
        if (deltaTicks > TimeSpan.TicksPerSecond)
        {
            return;
        }

        if (Math.Abs(deltaMs - expectedMs) <= toleranceMs)
        {
            return;
        }

        RecordPlaybackPtsCadenceMismatch(deltaMs, expectedMs, toleranceMs, pts);
    }

    private void RecordPlaybackPtsCadenceMismatch(double deltaMs, double expectedMs, double toleranceMs, TimeSpan pts)
    {
        var count = Interlocked.Increment(ref _playbackPtsCadenceMismatchCount);
        _lastPlaybackPtsCadenceDeltaMs = deltaMs;
        _lastPlaybackPtsCadenceExpectedMs = expectedMs;
        Interlocked.Exchange(ref _lastPlaybackPtsCadenceMismatchUtcUnixMs, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        if (count <= 3 || count % 120 == 0)
        {
            Logger.Log(
                $"FLASHBACK_PLAYBACK_PTS_CADENCE_MISMATCH count={count} " +
                $"delta_ms={deltaMs:0.###} expected_ms={expectedMs:0.###} tolerance_ms={toleranceMs:0.###} " +
                $"pts_ms={(long)pts.TotalMilliseconds} target_fps={_playbackTargetFps:0.###}");
        }
    }

    private void UpdateCadenceMetrics(Stopwatch pacingStopwatch, double expectedFrameMs)
    {
        var frameNum = Interlocked.Increment(ref _playbackFrameCount);
        var intervalMs = pacingStopwatch.Elapsed.TotalMilliseconds;
        pacingStopwatch.Restart();
        TrackPlaybackCadence(intervalMs, expectedFrameMs);

        if (frameNum % 60 == 0)
        {
            // Rolling window over the cadence ring (~2 s at 120 fps) so transient dips
            // are not smoothed away by the cumulative average over a long session.
            double sumMs;
            int count;
            lock (_playbackCadenceLock)
            {
                count = _playbackFrameIntervalCount;
                sumMs = 0;
                for (var i = 0; i < count; i++)
                {
                    sumMs += _playbackFrameIntervalsMs[i];
                }
            }

            if (count > 0 && sumMs > 0)
            {
                _playbackAvgFrameMs = sumMs / count;
                _playbackObservedFps = count * 1000.0 / sumMs;
            }
        }
    }

    private void SnapToLiveOnError(FlashbackDecoder decoder, Exception ex, ref bool fileOpen)
    {
        Interlocked.Increment(ref _playbackDecodeErrorSnaps);
        var pos = PlaybackPosition;
        var bufDur = _bufferManager.BufferedDuration;
        var gapMs = SaturatingSubtract(bufDur, pos).TotalMilliseconds;
        SetLastCommandFailure($"decode_error:{ex.GetType().Name}{FormatCommandDetail(position: pos)}");
        Logger.Log($"FLASHBACK_PLAYBACK_DECODE_ERROR_SNAP_TO_LIVE type={ex.GetType().Name} error='{ex.Message}' pos_ms={(long)pos.TotalMilliseconds} bufferDur_ms={(long)bufDur.TotalMilliseconds} gapFromLive_ms={gapMs:F0} frameCount={_playbackFrameCount}");
        Logger.Log($"FLASHBACK_PLAYBACK_DECODE_ERROR_STACK {ex.StackTrace?.Replace("\r\n", " | ")}");
        CloseDecoderFileBestEffort(decoder, "decode_error");
        fileOpen = false;
        _currentOpenFilePath = null;
        _decoderHwAccel = "N/A";
        Interlocked.Exchange(ref _lastAudioPtsTicks, 0);
        Interlocked.Exchange(ref _lastVideoPtsTicks, 0);
        ReleasePlaybackFrameForLive("decode_error");
        RestoreLiveAudio();
        SafeResumePreviewSubmission("decode_error");
        SetState(FlashbackPlaybackState.Live);
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

    /// <summary>
    /// Submits a decoded frame to the preview renderer — GPU texture or raw CPU data.
    /// </summary>
    private static void SubmitFrame(IPreviewFrameSink previewSink, DecodedVideoFrame frame)
    {
        var submitTick = Stopwatch.GetTimestamp();
        if (frame.IsD3D11Texture)
        {
            if (frame.TexturePtr == IntPtr.Zero)
            {
                Logger.Log("FLASHBACK_PLAYBACK_SUBMIT_SKIP reason=null_texture");
                return;
            }
            previewSink.SubmitTexture(
                frame.TexturePtr, frame.SubresourceIndex,
                frame.Width, frame.Height, frame.IsHdr, arrivalTick: submitTick, schedulerSubmitTick: submitTick);
        }
        else
        {
            previewSink.SubmitRawFrame(
                frame.Data, frame.DataLength,
                frame.Width, frame.Height, frame.IsHdr, arrivalTick: submitTick, schedulerSubmitTick: submitTick);
        }
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

    // --- Position mapping ---

    private TimeSpan ClampPosition(TimeSpan position) => ClampPosition(position, null);

    /// <summary>
    /// Clamp a scrub/seek position to the currently usable buffer range, optionally
    /// account for segment eviction that has happened since a scrub session captured
    /// its frozen reference. Without the eviction adjustment, a long-held scrub at
    /// position 0 maps via SaturatingAdd(pos, frozenValidStart) to a file PTS that
    /// has been evicted — EnsureFileOpen fails and the user gets a sudden snap-to-
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

    private TimeSpan NormalizeMarkerPosition(TimeSpan position)
    {
        if (position <= TimeSpan.Zero)
        {
            return TimeSpan.Zero;
        }

        var bufferDuration = _bufferManager.BufferedDuration;
        return position > bufferDuration ? bufferDuration : position;
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

    // --- State management ---

    private void SetState(FlashbackPlaybackState newState)
    {
        var oldState = _state;
        if (oldState == newState) return;
        _state = newState;
        Logger.Log($"FLASHBACK_PLAYBACK_STATE {oldState} -> {newState}");
    }

    public bool IsInitialized => _initialized;
    public bool IsDisposed => _disposedFlag != 0;
    public string DecoderHwAccel => _decoderHwAccel;
    public long PlaybackFrameCount => Interlocked.Read(ref _playbackFrameCount);
    public long PlaybackLateFrames => Interlocked.Read(ref _playbackLateFrames);
    public long PlaybackDroppedFrames => Interlocked.Read(ref _playbackDroppedFrames);
    public long PlaybackAudioMasterDelayDoubles => Interlocked.Read(ref _playbackAudioMasterDelayDoubles);
    public long PlaybackAudioMasterDelayShrinks => Interlocked.Read(ref _playbackAudioMasterDelayShrinks);
    public long PlaybackAudioMasterFallbacks => Interlocked.Read(ref _playbackAudioMasterFallbacks);
    public long PlaybackAudioMasterUnavailableFallbacks => Interlocked.Read(ref _playbackAudioMasterUnavailableFallbacks);
    public long PlaybackAudioMasterStaleFallbacks => Interlocked.Read(ref _playbackAudioMasterStaleFallbacks);
    public long PlaybackAudioMasterDriftOutlierFallbacks => Interlocked.Read(ref _playbackAudioMasterDriftOutlierFallbacks);
    public string PlaybackAudioMasterLastFallbackReason => Volatile.Read(ref _playbackAudioMasterLastFallbackReason);
    public double PlaybackAudioMasterLastFallbackDriftMs => _playbackAudioMasterLastFallbackDriftMs;
    public double PlaybackAudioMasterLastFallbackClockAgeMs => _playbackAudioMasterLastFallbackClockAgeMs;
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
    public long PlaybackPtsCadenceMismatchCount => Interlocked.Read(ref _playbackPtsCadenceMismatchCount);
    public long LastPlaybackPtsCadenceMismatchUtcUnixMs => Interlocked.Read(ref _lastPlaybackPtsCadenceMismatchUtcUnixMs);
    public double LastPlaybackPtsCadenceDeltaMs => _lastPlaybackPtsCadenceDeltaMs;
    public double LastPlaybackPtsCadenceExpectedMs => _lastPlaybackPtsCadenceExpectedMs;
    public string PlaybackMaxDecodePhase => Volatile.Read(ref _playbackMaxDecodePhase);
    public double PlaybackMaxDecodeReceiveMs => _playbackMaxDecodeReceiveMs;
    public double PlaybackMaxDecodeFeedMs => _playbackMaxDecodeFeedMs;
    public double PlaybackMaxDecodeReadMs => _playbackMaxDecodeReadMs;
    public double PlaybackMaxDecodeSendMs => _playbackMaxDecodeSendMs;
    public double PlaybackMaxDecodeAudioMs => _playbackMaxDecodeAudioMs;
    public double PlaybackMaxDecodeConvertMs => _playbackMaxDecodeConvertMs;
    public long PlaybackMaxDecodeUtcUnixMs => Interlocked.Read(ref _playbackMaxDecodeUtcUnixMs);
    public long PlaybackMaxDecodePositionMs => Interlocked.Read(ref _playbackMaxDecodePositionMs);
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
    public long LastCommandQueuedUtcUnixMs => Interlocked.Read(ref _lastCommandQueuedUtcUnixMs);
    public long LastCommandProcessedUtcUnixMs => Interlocked.Read(ref _lastCommandProcessedUtcUnixMs);
    public long LastCommandFailureUtcUnixMs => Interlocked.Read(ref _lastCommandFailureUtcUnixMs);
    public string LastCommandQueued => Volatile.Read(ref _lastCommandQueued);
    public string LastCommandProcessed => Volatile.Read(ref _lastCommandProcessed);
    public string LastCommandFailure => Volatile.Read(ref _lastCommandFailure);
    public bool PlaybackThreadAlive => _playbackThread is { IsAlive: true };

    public PlaybackCadenceMetrics GetPlaybackCadenceMetrics()
    {
        double[] samples;
        lock (_playbackCadenceLock)
        {
            if (_playbackFrameIntervalCount == 0)
            {
                return new PlaybackCadenceMetrics(0, 0, 0, 0, Interlocked.Read(ref _playbackSlowFrameCount), 0, 0);
            }

            samples = new double[_playbackFrameIntervalCount];
            var oldest = (_playbackFrameIntervalHead - _playbackFrameIntervalCount + _playbackFrameIntervalsMs.Length) % _playbackFrameIntervalsMs.Length;
            for (var i = 0; i < samples.Length; i++)
            {
                samples[i] = _playbackFrameIntervalsMs[(oldest + i) % _playbackFrameIntervalsMs.Length];
            }
        }

        Array.Sort(samples);
        var p95 = PercentileFromSorted(samples, 0.95);
        var p99 = PercentileFromSorted(samples, 0.99);
        var max = samples[^1];
        var slow = Interlocked.Read(ref _playbackSlowFrameCount);
        var totalFrames = Math.Max(1, Interlocked.Read(ref _playbackFrameCount));
        var slowPercent = slow * 100.0 / totalFrames;
        var onePercentLowFps = p99 > 0 ? 1000.0 / p99 : 0;
        return new PlaybackCadenceMetrics(samples.Length, p95, p99, max, slow, slowPercent, onePercentLowFps);
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

    private bool IsReady => _initialized && _disposedFlag == 0;

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

    private void MarkCommandQueued(CommandKind kind)
    {
        Interlocked.Exchange(ref _lastCommandQueuedUtcUnixMs, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        Volatile.Write(ref _lastCommandQueued, kind.ToString());
        ClearLastCommandFailure();
    }

    private void MarkCommandNoOp(CommandKind kind, string reason, TimeSpan? position = null, TimeSpan? delta = null)
    {
        ClearLastCommandFailure();
        Logger.Log($"FLASHBACK_PLAYBACK_CMD_NOOP kind={kind} reason={reason}{FormatCommandDetail(position, delta)}");
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

    private void ClearLastCommandFailure()
    {
        Volatile.Write(ref _lastCommandFailure, string.Empty);
        Interlocked.Exchange(ref _lastCommandFailureUtcUnixMs, 0);
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
        UpdateMaxLong(ref _maxCommandQueueLatencyMs, latencyMs);
    }

    private void UpdateMaxPendingCommands(int value)
        => UpdateMaxInt(ref _maxPendingCommands, value);

    private static void UpdateMaxInt(ref int target, int value)
    {
        while (true)
        {
            var current = Volatile.Read(ref target);
            if (value <= current)
            {
                return;
            }

            if (Interlocked.CompareExchange(ref target, value, current) == current)
            {
                return;
            }
        }
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

    private static void UpdateMaxLong(ref long target, long value)
    {
        while (true)
        {
            var current = Interlocked.Read(ref target);
            if (value <= current)
            {
                return;
            }

            if (Interlocked.CompareExchange(ref target, value, current) == current)
            {
                return;
            }
        }
    }

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

    private static Channel<PlaybackCommand> CreateCommandChannel()
        => Channel.CreateBounded<PlaybackCommand>(
            new BoundedChannelOptions(CommandQueueCapacity)
            {
                SingleReader = true,
                SingleWriter = false,
                FullMode = BoundedChannelFullMode.Wait
            });

    private void ResetPlaybackMetrics()
    {
        Interlocked.Exchange(ref _playbackFrameCount, 0);
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
        Volatile.Write(ref _lastPlaybackDropReason, string.Empty);
        Interlocked.Exchange(ref _lastPlaybackDropUtcUnixMs, 0);
        Interlocked.Exchange(ref _playbackSubmitFailures, 0);
        ClearLastSubmitFailure();
        // Reset audio clock extrapolation so stale PTS doesn't cause a jump
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

    private void RestoreAudioCallback(FlashbackDecoder decoder, long audioStartGateTicks = 0)
    {
        // Audio start gate: drop any audio chunk with PTS before this value.
        // This filters stale audio from keyframe→target that the decoder re-processes
        // after a seek. Callers pass the seek target PTS as the gate.
        var videoPtsGate = audioStartGateTicks > 0
            ? audioStartGateTicks
            : Interlocked.Read(ref _lastVideoPtsTicks);
        Interlocked.Exchange(ref _lastAudioPtsTicks, 0);

        if (_audioPlayback == null)
        {
            decoder.AudioChunkCallback = null;
            return;
        }

        if (_audioPlayback != null)
        {
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

                // Skip invalid or non-monotonic PTS (L8 fix)
                var prevPts = Interlocked.Read(ref _lastAudioPtsTicks);
                if (chunk.Pts.Ticks <= 0 || chunk.Pts.Ticks < prevPts)
                {
                    ReturnPlaybackAudioChunkBestEffort(chunk, "playback_audio_non_monotonic_pts");
                    return;
                }

                // Skip audio before the video position at seek time — these are
                // from the keyframe→target forward decode and would cause drift.
                if (videoPtsGate > 0 && chunk.Pts.Ticks < videoPtsGate)
                {
                    ReturnPlaybackAudioChunkBestEffort(chunk, "playback_audio_before_gate");
                    return;
                }

                Interlocked.Exchange(ref _lastAudioPtsTicks, chunk.Pts.Ticks);
                pb.EnqueuePooledSamples(chunk.Samples, chunk.ValidLength, chunk.Pts.Ticks);
            };
        }
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
        // F4 fix: reconnect audio feed BEFORE starting rendering to avoid silence/stutter
        try
        {
            if (_audioCapture != null && _audioPlayback != null)
                _audioCapture.SetPlayback(_audioPlayback);
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
            _audioPlayback?.PauseRendering();
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
            _audioPlayback?.ResumeRendering();
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_PLAYBACK_AUDIO_WARN op=resume operation={operation} type={ex.GetType().Name} msg='{ex.Message}'");
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

    // --- Timer resolution P/Invoke (1ms sleep granularity for 120fps pacing) ---

    [DllImport("winmm.dll", ExactSpelling = true)]
    private static extern uint timeBeginPeriod(uint uMilliseconds);

    [DllImport("winmm.dll", ExactSpelling = true)]
    private static extern uint timeEndPeriod(uint uMilliseconds);
}

using System;
using System.Buffers;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Channels;
using ElgatoCapture.Models;
using ElgatoCapture.Services.Audio;
using ElgatoCapture.Services.Preview;
using ElgatoCapture.Services.Recording;

namespace ElgatoCapture.Services.Flashback;

internal sealed class FlashbackPlaybackController : IDisposable
{
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
        public long QueuedTimestamp { get; init; }
    }

    public readonly record struct PlaybackCadenceMetrics(
        int SampleCount,
        double P95FrameMs,
        double P99FrameMs,
        double MaxFrameMs,
        long SlowFrameCount,
        double SlowFramePercent,
        double OnePercentLowFps);

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

    // --- Playback cadence metrics (written on playback thread, read from UI/diag) ---
    private long _playbackFrameCount;
    private long _playbackLateFrames;
    private long _playbackSegmentSwitches;
    private long _playbackFmp4Reopens;
    private long _playbackWriteHeadWaits;
    private long _playbackNearLiveSnaps;
    private long _playbackDecodeErrorSnaps;
    private long _lastSegmentSwitchUtcUnixMs;
    private long _lastFmp4ReopenUtcUnixMs;
    private long _lastWriteHeadWaitGapMs;
    private double _playbackObservedFps;
    private double _playbackAvgFrameMs;
    private readonly Stopwatch _playbackFpsClock = new();
    private const int PlaybackCadenceSampleCapacity = 240;
    private readonly object _playbackCadenceLock = new();
    private readonly double[] _playbackFrameIntervalsMs = new double[PlaybackCadenceSampleCapacity];
    private int _playbackFrameIntervalHead;
    private int _playbackFrameIntervalCount;
    private long _playbackSlowFrameCount;
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
    private string _lastCommandQueued = "None";
    private string _lastCommandProcessed = "None";
    private string _lastCommandFailure = string.Empty;
    private long _latestScrubUpdateTicks;
    private int _scrubUpdateCommandQueued;

    // --- Deferred frame release for D3D11VA (C1 fix) ---
    // The renderer's render thread hasn't copied the texture yet when we release.
    // Keep the previous frame alive until the next frame is submitted.
    private DecodedVideoFrame _previousHeldFrame;
    private bool _hasPreviousHeldFrame;

    // --- Audio PTS suppression (H2/H3 fix) ---
    // After seek, suppress audio chunks with PTS < target to avoid stale GOP audio
    private long _suppressAudioUntilPtsTicks;

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
        set => Interlocked.Exchange(ref _inPointTicks, value?.Ticks ?? long.MinValue);
    }

    public TimeSpan? OutPoint
    {
        get
        {
            var t = Interlocked.Read(ref _outPointTicks);
            return t == long.MinValue ? null : TimeSpan.FromTicks(t);
        }
        set => Interlocked.Exchange(ref _outPointTicks, value?.Ticks ?? long.MinValue);
    }

    // --- Playback thread ---
    private Thread? _playbackThread;
    private int _playbackThreadStarted;
    private CancellationTokenSource? _playCts;
    private Channel<PlaybackCommand> _commandChannel =
        Channel.CreateUnbounded<PlaybackCommand>(new UnboundedChannelOptions { SingleReader = true });

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
        _previewSink = previewSink ?? throw new ArgumentNullException(nameof(previewSink));
        _videoCapture = videoCapture ?? throw new ArgumentNullException(nameof(videoCapture));
        _audioPlayback = audioPlayback;
        _audioCapture = audioCapture;
        _initialized = true;
        Logger.Log("FLASHBACK_PLAYBACK_INIT");
    }

    /// <summary>
    /// Updates audio references after WASAPI components become available.
    /// Called from CaptureService after StartWasapiPlaybackAsync completes,
    /// since WASAPI init happens after flashback controller init.
    /// </summary>
    public void UpdateAudioComponents(WasapiAudioPlayback? audioPlayback, WasapiAudioCapture? audioCapture)
    {
        _audioPlayback = audioPlayback;
        _audioCapture = audioCapture;
        Logger.Log($"FLASHBACK_PLAYBACK_AUDIO_UPDATE playback={audioPlayback != null} capture={audioCapture != null}");
    }

    // --- State transitions (called from UI thread) ---

    public void BeginScrub(TimeSpan position)
    {
        if (IsNotReady(CommandKind.BeginScrub)) return;
        EnsurePlaybackThread();
        SendCommand(new PlaybackCommand { Kind = CommandKind.BeginScrub, Position = position });
    }

    public void Seek(TimeSpan position)
    {
        if (IsNotReady(CommandKind.Seek)) return;
        EnsurePlaybackThread();
        SendCommand(new PlaybackCommand { Kind = CommandKind.Seek, Position = position });
    }

    public void UpdateScrub(TimeSpan position)
    {
        if (IsNotReady(CommandKind.UpdateScrub)) return;
        Interlocked.Exchange(ref _latestScrubUpdateTicks, position.Ticks);
        if (Interlocked.CompareExchange(ref _scrubUpdateCommandQueued, 1, 0) != 0)
        {
            Interlocked.Increment(ref _commandsDropped);
            return;
        }

        if (!SendCommand(new PlaybackCommand { Kind = CommandKind.UpdateScrub, Position = position }))
        {
            Interlocked.Exchange(ref _scrubUpdateCommandQueued, 0);
        }
    }

    public void EndScrub()
    {
        if (IsNotReady(CommandKind.EndScrub)) return;
        SendCommand(new PlaybackCommand { Kind = CommandKind.EndScrub });
    }

    public void Play()
    {
        if (IsNotReady(CommandKind.Play)) return;
        EnsurePlaybackThread();
        SendCommand(new PlaybackCommand { Kind = CommandKind.Play });
    }

    public void Pause()
    {
        if (IsNotReady(CommandKind.Pause)) return;
        EnsurePlaybackThread(); // Thread must be running to handle Live→Paused
        SendCommand(new PlaybackCommand { Kind = CommandKind.Pause });
    }

    public void GoLive()
    {
        if (IsNotReady(CommandKind.GoLive)) return;
        if (State == FlashbackPlaybackState.Live && !PlaybackThreadAlive) return;
        EnsurePlaybackThread();
        SendCommand(new PlaybackCommand { Kind = CommandKind.GoLive });
    }

    public void NudgePosition(TimeSpan delta)
    {
        if (IsNotReady(CommandKind.Nudge)) return;
        if (State == FlashbackPlaybackState.Live && !PlaybackThreadAlive) return;
        EnsurePlaybackThread();
        SendCommand(new PlaybackCommand { Kind = CommandKind.Nudge, Delta = delta });
    }

    // --- In/Out point helpers ---

    public TimeSpan SetInPoint()
    {
        var pos = PlaybackPosition;
        InPoint = pos;
        Logger.Log($"FLASHBACK_PLAYBACK_SET_IN pos_ms={(long)pos.TotalMilliseconds}");
        return pos;
    }

    public TimeSpan SetOutPoint()
    {
        var pos = PlaybackPosition;
        OutPoint = pos;
        Logger.Log($"FLASHBACK_PLAYBACK_SET_OUT pos_ms={(long)pos.TotalMilliseconds}");
        return pos;
    }

    public void ClearInOutPoints()
    {
        InPoint = null;
        OutPoint = null;
        Logger.Log("FLASHBACK_PLAYBACK_CLEAR_INOUT");
    }

    // --- Dispose ---

    public void Dispose()
    {
        if (Interlocked.CompareExchange(ref _disposedFlag, 1, 0) != 0) return;

        Logger.Log($"FLASHBACK_PLAYBACK_DISPOSE_REQUEST state={_state} initialized={_initialized}");
        StopPlaybackThread();
        Logger.Log("FLASHBACK_PLAYBACK_DISPOSED");
    }

    // --- Command dispatch ---

    private bool SendCommand(PlaybackCommand command)
    {
        var queuedCommand = new PlaybackCommand
        {
            Kind = command.Kind,
            Position = command.Position,
            Delta = command.Delta,
            QueuedTimestamp = Stopwatch.GetTimestamp()
        };

        if (!_commandChannel.Writer.TryWrite(queuedCommand))
        {
            Interlocked.Increment(ref _commandsDropped);
            _lastCommandFailure = $"write_failed:{command.Kind}";
            Logger.Log($"FLASHBACK_PLAYBACK_CMD_DROP kind={command.Kind}");
            return false;
        }

        Interlocked.Increment(ref _commandsEnqueued);
        UpdateMaxPendingCommands(Interlocked.Increment(ref _pendingCommands));
        Interlocked.Exchange(ref _lastCommandQueuedUtcUnixMs, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        _lastCommandQueued = command.Kind.ToString();
        return true;
    }

    private void EnsurePlaybackThread()
    {
        if (_disposedFlag != 0) return;
        if (Interlocked.CompareExchange(ref _playbackThreadStarted, 1, 0) != 0)
            return;

        // Recreate the command channel — the previous one was completed by StopPlaybackThread.
        // A completed channel silently drops all TryWrite calls.
        _commandChannel = Channel.CreateUnbounded<PlaybackCommand>(new UnboundedChannelOptions { SingleReader = true });
        _playCts = new CancellationTokenSource();
        var threadCts = _playCts;
        _playbackThread = new Thread(() => PlaybackThreadEntry(threadCts))
        {
            Name = "FlashbackPlayback",
            IsBackground = true,
            Priority = ThreadPriority.AboveNormal
        };
        try
        {
            _playbackThread.Start();
        }
        catch
        {
            /* Cleanup must not throw — tear down CTS before re-throwing thread start failure */
            _playCts.Dispose();
            _playCts = null;
            Interlocked.Exchange(ref _playbackThreadStarted, 0);
            throw;
        }
        Logger.Log("FLASHBACK_PLAYBACK_THREAD_START");
    }

    private void StopPlaybackThread()
    {
        var thread = _playbackThread;
        if (Volatile.Read(ref _playbackThreadStarted) != 0 && thread is { IsAlive: true })
        {
            SendCommand(new PlaybackCommand { Kind = CommandKind.Stop });
        }

        _commandChannel.Writer.TryComplete();

        try { _playCts?.Cancel(); } catch { /* Best-effort: CTS cancel during stop must not prevent thread join */ }

        var threadExited = true;
        if (thread is { IsAlive: true })
        {
            if (!thread.Join(TimeSpan.FromSeconds(3)))
            {
                Logger.Log("FLASHBACK_PLAYBACK_THREAD_JOIN_TIMEOUT");
                threadExited = false;
            }
        }

        if (threadExited)
        {
            _playCts?.Dispose();
            _playCts = null;
            _playbackThread = null;
            Volatile.Write(ref _playbackThreadStarted, 0);
        }
    }

    // --- Playback thread ---

    private void PlaybackThreadEntry(CancellationTokenSource cts)
    {
        FlashbackDecoder? decoder = null;
        var pacingStopwatch = new Stopwatch();
        var frameDuration = TimeSpan.Zero;
        var isPlaying = false;
        var isScrubbing = false;
        var fileOpen = false;
        var frozenValidStart = TimeSpan.Zero; // captured when leaving Live, used for position mapping

        // Set 1ms timer resolution for accurate Thread.Sleep pacing.
        // Without this, Sleep(8) at 120fps sleeps ~15ms (default granularity) → half-speed.
        timeBeginPeriod(1);
        try
        {
            Logger.Log("FLASHBACK_PLAYBACK_THREAD_ENTER");
            while (true)
            {
                PlaybackCommand cmd;
                if (isPlaying)
                {
                    if (!_commandChannel.Reader.TryRead(out cmd))
                    {
                        if (cts.IsCancellationRequested)
                        {
                            Logger.Log("FLASHBACK_PLAYBACK_THREAD_EXIT cancellation_requested");
                            CleanupDecoder(ref decoder, ref fileOpen);
                            RestoreLiveAudio();
                            SafeResumePreviewSubmission("thread_cancelled");
                            SetState(FlashbackPlaybackState.Live);
                            return;
                        }

                        if (decoder is { IsOpen: true })
                        {
                            if (!PaceAndDecodeFrame(decoder, pacingStopwatch, ref frameDuration, ref fileOpen, frozenValidStart, cts.Token))
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
                    if (!_commandChannel.Reader.TryRead(out cmd))
                    {
                        var canRead = _commandChannel.Reader.WaitToReadAsync(cts.Token).AsTask().GetAwaiter().GetResult();
                        if (!canRead)
                        {
                            Logger.Log("FLASHBACK_PLAYBACK_THREAD_EXIT channel_closed");
                            return;
                        }

                        if (_disposedFlag != 0)
                        {
                            Logger.Log("FLASHBACK_PLAYBACK_THREAD_EXIT");
                            return;
                        }
                        if (!_commandChannel.Reader.TryRead(out cmd))
                        {
                            continue;
                        }
                    }
                    TrackCommandDequeued(cmd);
                }

                switch (cmd.Kind)
                {
                    case CommandKind.Stop:
                        CleanupDecoder(ref decoder, ref fileOpen);
                        Logger.Log("FLASHBACK_PLAYBACK_THREAD_EXIT");
                        return;

                    case CommandKind.Seek:
                        while (_commandChannel.Reader.TryPeek(out var newerSeek) &&
                               newerSeek.Kind == CommandKind.Seek)
                        {
                            if (!_commandChannel.Reader.TryRead(out newerSeek))
                            {
                                break;
                            }

                            TrackCommandDequeued(newerSeek);
                            cmd = newerSeek;
                        }

                        _wasPlayingBeforeScrub = isPlaying || State == FlashbackPlaybackState.Live;
                        isPlaying = false;
                        isScrubbing = false;
                        frozenValidStart = _bufferManager.ValidStartPts;
                        SafeSuppressPreviewSubmission("seek");
                        SuppressLiveAudio();
                        SafePauseRendering("seek");

                        decoder ??= CreateDecoder();
                        EnsureFileOpen(decoder, ref fileOpen, cmd.Position + frozenValidStart);
                        if (!decoder.IsOpen)
                        {
                            Logger.Log("FLASHBACK_PLAYBACK_SEEK_NO_FILE - restoring live");
                            RestoreLiveAudio();
                            SafeResumePreviewSubmission("seek_no_file");
                            SafeResumeRendering("seek_no_file");
                            SetState(FlashbackPlaybackState.Live);
                            break;
                        }

                        SeekAndDisplayKeyframe(decoder, cmd.Position, frozenValidStart);
                        isPlaying = _wasPlayingBeforeScrub;
                        if (isPlaying)
                        {
                            ResetPlaybackMetrics();
                            pacingStopwatch.Restart();
                            var coalescedSeekTarget = PlaybackPosition + frozenValidStart;
                            decoder.AudioChunkCallback = null;
                            if (!decoder.SeekTo(coalescedSeekTarget) && IsActiveFmp4Segment(_currentOpenFilePath))
                            {
                                TryReopenCurrentFileAndSeek(decoder, ref fileOpen, coalescedSeekTarget, "seek");
                            }
                            frameDuration = TimeSpan.FromSeconds(1.0 / Math.Max(decoder.FrameRate, 1.0));
                            RestoreAudioCallback(decoder, coalescedSeekTarget.Ticks);
                            SafeFlushPlayback("seek_resume");
                            SafeResumeRendering("seek_resume");
                        }
                        SetState(isPlaying ? FlashbackPlaybackState.Playing : FlashbackPlaybackState.Paused);
                        Logger.Log($"FLASHBACK_PLAYBACK_SEEK pos_ms={(long)PlaybackPosition.TotalMilliseconds} resumePlay={isPlaying}");
                        break;

                    case CommandKind.BeginScrub:
                        _wasPlayingBeforeScrub = isPlaying || State == FlashbackPlaybackState.Live;
                        isPlaying = false;
                        isScrubbing = true;
                        frozenValidStart = _bufferManager.ValidStartPts;
                        SafeSuppressPreviewSubmission("begin_scrub");
                        SuppressLiveAudio();
                        SafePauseRendering("begin_scrub");
                        SetState(FlashbackPlaybackState.Scrubbing);

                        decoder ??= CreateDecoder();
                        EnsureFileOpen(decoder, ref fileOpen, cmd.Position + frozenValidStart);
                        if (!decoder.IsOpen)
                        {
                            Logger.Log("FLASHBACK_PLAYBACK_SCRUB_NO_FILE — restoring live");
                            isScrubbing = false;
                            RestoreLiveAudio();
                            SafeResumePreviewSubmission("scrub_no_file");
                            SafeResumeRendering("scrub_no_file");
                            SetState(FlashbackPlaybackState.Live);
                            break;
                        }
                        SeekAndDisplayKeyframe(decoder, cmd.Position, frozenValidStart);
                        break;

                    case CommandKind.UpdateScrub:
                        Interlocked.Exchange(ref _scrubUpdateCommandQueued, 0);
                        cmd = cmd with { Position = TimeSpan.FromTicks(Interlocked.Read(ref _latestScrubUpdateTicks)) };
                        if (!isScrubbing) break;
                        // Drain stale UpdateScrub commands only. Leave control commands queued
                        // so their latency/accounting stays tied to the original command.
                        while (_commandChannel.Reader.TryPeek(out var newer) &&
                               newer.Kind == CommandKind.UpdateScrub)
                        {
                            if (!_commandChannel.Reader.TryRead(out newer))
                            {
                                break;
                            }

                            TrackCommandDequeued(newer);
                            Interlocked.Exchange(ref _scrubUpdateCommandQueued, 0);
                            newer = newer with { Position = TimeSpan.FromTicks(Interlocked.Read(ref _latestScrubUpdateTicks)) };
                            cmd = newer;
                        }
                        decoder ??= CreateDecoder();
                        EnsureFileOpen(decoder, ref fileOpen, cmd.Position + frozenValidStart);
                        SeekAndDisplayKeyframe(decoder, cmd.Position, frozenValidStart);
                        break;

                    case CommandKind.EndScrub:
                        if (!isScrubbing) break;
                        isScrubbing = false;
                        isPlaying = _wasPlayingBeforeScrub;
                        if (isPlaying)
                        {
                            ResetPlaybackMetrics();
                            pacingStopwatch.Restart();

                            // Re-seek to the current position using SeekTo (not SeekToKeyframe).
                            // SeekTo forward-decodes from keyframe to target, which advances
                            // BOTH the video and audio codecs to the same PTS. Without this,
                            // the audio codec is stuck at the keyframe (~1s behind video).
                            var endScrubTarget = PlaybackPosition + frozenValidStart;
                            if (decoder is { IsOpen: true })
                            {
                                decoder.AudioChunkCallback = null; // null during forward-decode
                                if (!decoder.SeekTo(endScrubTarget) && IsActiveFmp4Segment(_currentOpenFilePath))
                                {
                                    TryReopenCurrentFileAndSeek(decoder, ref fileOpen, endScrubTarget, "end_scrub");
                                }
                                frameDuration = TimeSpan.FromSeconds(1.0 / Math.Max(decoder.FrameRate, 1.0));
                            }
                            if (decoder != null)
                            {
                                RestoreAudioCallback(decoder, endScrubTarget.Ticks);
                            }
                            SafeFlushPlayback("end_scrub_resume");
                            SafeResumeRendering("end_scrub_resume");
                        }
                        SetState(isPlaying ? FlashbackPlaybackState.Playing : FlashbackPlaybackState.Paused);
                        var endScrubBufDur = _bufferManager.BufferedDuration;
                        Logger.Log($"FLASHBACK_ENDSCRUB pos_ms={(long)PlaybackPosition.TotalMilliseconds} bufferDur_ms={(long)endScrubBufDur.TotalMilliseconds} gapFromLive_ms={(endScrubBufDur - PlaybackPosition).TotalMilliseconds:F0} resumePlay={isPlaying}");
                        break;

                    case CommandKind.Play:
                        if (isPlaying) break;
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
                        EnsureFileOpen(decoder, ref fileOpen, PlaybackPosition + frozenValidStart);
                        if (!decoder.IsOpen)
                        {
                            Logger.Log("FLASHBACK_PLAYBACK_PLAY_NO_FILE — restoring live");
                            isPlaying = false;
                            RestoreLiveAudio();
                            SafeResumePreviewSubmission("play_no_file");
                            SafeResumeRendering("play_no_file");
                            SetState(FlashbackPlaybackState.Live);
                            break;
                        }
                        var seekTarget = PlaybackPosition + frozenValidStart;
                        if (State == FlashbackPlaybackState.Paused && prevFile == _currentOpenFilePath)
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
                            if (!decoder.SeekTo(seekTarget))
                            {
                                // Active fMP4 segment: demuxer fragment index is stale — reopen and retry.
                                // Only applies to fMP4 (.mp4); transport streams (.ts) handle appended data
                                // via eof_reached reset and don't need reopening.
                                if (IsActiveFmp4Segment(_currentOpenFilePath) && _currentOpenFilePath != null)
                                {
                                    TryReopenCurrentFileAndSeek(decoder, ref fileOpen, seekTarget, "play");
                                }
                            }
                        }
                        frameDuration = TimeSpan.FromSeconds(1.0 / Math.Max(decoder.FrameRate, 1.0));
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
                            var pausePos = _bufferManager.BufferedDuration;
                            PlaybackPosition = pausePos;

                            SetState(FlashbackPlaybackState.Paused);
                            Logger.Log($"FLASHBACK_PLAYBACK_PAUSE_FROM_LIVE pos_ms={(long)pausePos.TotalMilliseconds} frozen_preview=true");
                        }
                        break;

                    case CommandKind.GoLive:
                        isPlaying = false;
                        isScrubbing = false;
                        CleanupDecoder(ref decoder, ref fileOpen);
                        Interlocked.Exchange(ref _lastAudioPtsTicks, 0);
                        Interlocked.Exchange(ref _lastVideoPtsTicks, 0);
                        Interlocked.Exchange(ref _suppressAudioUntilPtsTicks, 0); // F8 fix: clear stale suppression
                        RestoreLiveAudio();
                        SafeResumePreviewSubmission("go_live");
                        SetState(FlashbackPlaybackState.Live);
                        Logger.Log("FLASHBACK_PLAYBACK_GO_LIVE");
                        return;

                    case CommandKind.Nudge:
                        var nudgedPos = PlaybackPosition + cmd.Delta;
                        nudgedPos = ClampPosition(nudgedPos);
                        decoder ??= CreateDecoder();
                        EnsureFileOpen(decoder, ref fileOpen, nudgedPos + frozenValidStart);
                        if (!decoder.IsOpen)
                        {
                            PlaybackPosition = nudgedPos;
                            Logger.Log($"FLASHBACK_PLAYBACK_NUDGE_NO_FILE pos_ms={(long)nudgedPos.TotalMilliseconds}");
                            break;
                        }

                        // F7 fix: forward nudge decodes next frame for frame-accuracy;
                        // backward nudge requires full seek (keyframe snap acceptable)
                        if (cmd.Delta.Ticks > 0)
                        {
                            var got = decoder.TryDecodeNextVideoFrame(out var nudgeFrame);
                            if (got)
                            {
                                if (!TrySubmitAndHoldFrame(nudgeFrame, "nudge"))
                                {
                                    break;
                                }
                                var actualPos = nudgeFrame.Pts - frozenValidStart;
                                if (actualPos < TimeSpan.Zero) actualPos = TimeSpan.Zero;
                                PlaybackPosition = actualPos;
                                break;
                            }
                            // Forward decode failed (EOF) — fall through to full seek
                        }
                        SeekAndDisplayKeyframe(decoder, nudgedPos, frozenValidStart);
                        break;
                }
            }
        }
        catch (OperationCanceledException)
        {
            Logger.Log("FLASHBACK_PLAYBACK_THREAD_CANCELLED");
            CleanupDecoder(ref decoder, ref fileOpen);
            RestoreLiveAudio();
            SafeResumePreviewSubmission("thread_cancelled");
            SetState(FlashbackPlaybackState.Live);
        }
        catch (Exception ex)
        {
            _lastCommandFailure = ex.GetType().Name + ":" + ex.Message;
            Logger.Log($"FLASHBACK_PLAYBACK_FATAL error='{ex.Message}'");
            CleanupDecoder(ref decoder, ref fileOpen);
            RestoreLiveAudio();
            SafeResumePreviewSubmission("thread_fatal");
            SetState(FlashbackPlaybackState.Live);
        }
        finally
        {
            timeEndPeriod(1);
            DrainAbandonedCommandsOnThreadExit();
            if (ReferenceEquals(Thread.CurrentThread, _playbackThread))
            {
                _playbackThread = null;
            }
            if (ReferenceEquals(cts, _playCts))
            {
                _playCts = null;
            }
            cts.Dispose();
            Volatile.Write(ref _playbackThreadStarted, 0);
        }

        Logger.Log("FLASHBACK_PLAYBACK_THREAD_EXIT");
    }

    private void DrainAbandonedCommandsOnThreadExit()
    {
        var abandoned = 0;
        while (_commandChannel.Reader.TryRead(out var command))
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
            _lastCommandFailure = $"abandoned_on_exit:{abandoned}";
            Logger.Log($"FLASHBACK_PLAYBACK_CMD_ABANDONED count={abandoned}");
        }

        if (Volatile.Read(ref _pendingCommands) > 0)
        {
            Interlocked.Exchange(ref _pendingCommands, 0);
        }

        Interlocked.Exchange(ref _scrubUpdateCommandQueued, 0);
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
            return;
        }

        // If already open on the correct file, nothing to do
        if (fileOpen && decoder.IsOpen && filePath == _currentOpenFilePath)
            return;

        try
        {
            if (decoder.IsOpen) decoder.CloseFile();
            decoder.OpenFile(filePath);
            fileOpen = true;
            _currentOpenFilePath = filePath;
            _decoderHwAccel = decoder.IsD3D11HwAccelerated ? "D3D11VA" : "Software";
            Logger.Log($"FLASHBACK_PLAYBACK_FILE_OPEN path='{filePath}' hw_accel={_decoderHwAccel}");
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_PLAYBACK_FILE_OPEN_ERROR path='{filePath}' error='{ex.Message}'");
            fileOpen = false;
            _currentOpenFilePath = null;
        }
    }

    private void CleanupDecoder(ref FlashbackDecoder? decoder, ref bool fileOpen)
    {
        Logger.Log($"FLASHBACK_PLAYBACK_DECODER_CLEANUP was_open={decoder?.IsOpen ?? false}");
        ReleasePreviousHeldFrame();
        if (decoder != null)
        {
            if (decoder.IsOpen) decoder.CloseFile();
            decoder.Dispose();
            decoder = null;
        }
        fileOpen = false;
        _currentOpenFilePath = null;
        _decoderHwAccel = "N/A";
    }

    private bool TryReopenCurrentFileAndSeek(FlashbackDecoder decoder, ref bool fileOpen, TimeSpan seekTarget, string reason)
    {
        var currentPath = _currentOpenFilePath;
        if (string.IsNullOrWhiteSpace(currentPath))
        {
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
            return decoder.SeekTo(seekTarget);
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_PLAYBACK_REOPEN_ERROR reason={reason} path='{currentPath}' type={ex.GetType().Name} msg='{ex.Message}'");
            fileOpen = false;
            _currentOpenFilePath = null;
            return false;
        }
    }

    private void ReleasePreviousHeldFrame()
    {
        if (_hasPreviousHeldFrame)
        {
            FlashbackDecoder.ReleaseHeldFrame(_previousHeldFrame);
            _previousHeldFrame = default;
            _hasPreviousHeldFrame = false;
        }
    }

    private bool TrySubmitAndHoldFrame(DecodedVideoFrame frame, string operation)
    {
        if (frame.IsD3D11Texture && frame.TexturePtr == IntPtr.Zero)
        {
            FlashbackDecoder.ReleaseHeldFrame(frame);
            Logger.Log($"FLASHBACK_PLAYBACK_SUBMIT_SKIP op={operation} reason=null_texture");
            return false;
        }

        ReleasePreviousHeldFrame();
        try
        {
            SubmitFrame(frame);
            _previousHeldFrame = frame;
            _hasPreviousHeldFrame = true;
            return true;
        }
        catch (Exception ex)
        {
            FlashbackDecoder.ReleaseHeldFrame(frame);
            Logger.Log($"FLASHBACK_PLAYBACK_SUBMIT_FAIL op={operation} type={ex.GetType().Name} msg='{ex.Message}'");
            return false;
        }
    }

    private void SeekAndDisplayKeyframe(FlashbackDecoder decoder, TimeSpan bufferPosition, TimeSpan validStartPts)
    {
        // Suppress audio delivery during scrub — prevents audio accumulation
        // in the WASAPI queue. Audio callback is re-enabled on Play/EndScrub.
        decoder.AudioChunkCallback = null;
        SafeFlushPlayback("seek_display_keyframe");

        bufferPosition = ClampPosition(bufferPosition);

        if (!decoder.IsOpen)
        {
            // No file — use requested position as fallback
            PlaybackPosition = bufferPosition;
            Logger.Log($"FLASHBACK_PLAYBACK_SEEK_NO_FILE pos_ms={(long)bufferPosition.TotalMilliseconds}");
            return;
        }

        try
        {
            // Map buffer position to file PTS (offset by frozen valid start)
            var filePts = bufferPosition + validStartPts;

            // Clamp to current valid range: if eviction advanced ValidStartPts past
            // frozenValidStart, positions near the left edge map to evicted data.
            var currentValidStart = _bufferManager.ValidStartPts;
            if (filePts < currentValidStart)
            {
                filePts = currentValidStart;
                bufferPosition = filePts - validStartPts;
                if (bufferPosition < TimeSpan.Zero) bufferPosition = TimeSpan.Zero;
            }

            if (!decoder.SeekToKeyframe(filePts))
            {
                // Active fMP4 segment: demuxer caches fragment index at open time.
                // New fragments written since open aren't visible — reopen and retry.
                // Only for fMP4; .ts handles appended data via eof_reached reset.
                if (IsActiveFmp4Segment(_currentOpenFilePath) && _currentOpenFilePath != null)
                {
                    Logger.Log($"FLASHBACK_PLAYBACK_SEEK_REOPEN_ACTIVE offset_ms={(long)filePts.TotalMilliseconds}");
                    decoder.CloseFile();
                    decoder.OpenFile(_currentOpenFilePath);
                    if (decoder.SeekToKeyframe(filePts))
                        goto seekSuccess;
                }

                PlaybackPosition = bufferPosition;
                Logger.Log($"FLASHBACK_PLAYBACK_SEEK_FAIL offset_ms={(long)filePts.TotalMilliseconds}");
                return;
            }
            seekSuccess:

            var gotFrame = decoder.TryDecodeNextVideoFrame(out var frame);
            if (gotFrame)
            {
                if (!TrySubmitAndHoldFrame(frame, "seek"))
                {
                    PlaybackPosition = bufferPosition;
                    return;
                }
                Interlocked.Exchange(ref _lastVideoPtsTicks, frame.Pts.Ticks);

                // Set position to actual decoded frame PTS mapped back to buffer position
                var actualPosition = frame.Pts - validStartPts;
                if (actualPosition < TimeSpan.Zero) actualPosition = TimeSpan.Zero;
                PlaybackPosition = actualPosition;
            }
            else
            {
                // No frame decoded — use requested position as fallback
                PlaybackPosition = bufferPosition;
            }

            Logger.Log($"FLASHBACK_PLAYBACK_SEEK_OK pos_ms={(long)PlaybackPosition.TotalMilliseconds} file_pts_ms={(long)filePts.TotalMilliseconds} got_frame={gotFrame}");
        }
        catch (Exception ex)
        {
            // On error, use requested position as fallback
            PlaybackPosition = bufferPosition;
            Logger.Log($"FLASHBACK_PLAYBACK_SEEK_ERROR error='{ex.Message}'");
        }
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
        Stopwatch pacingStopwatch,
        ref TimeSpan frameDuration,
        ref bool fileOpen,
        TimeSpan frozenValidStart,
        CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!decoder.TryDecodeNextVideoFrame(out var videoFrame))
            {
                return HandleEndOfSegment(decoder, pacingStopwatch, frozenValidStart);
            }

            // Frame skip: when video falls significantly behind audio, decode-and-discard
            // frames to catch up rather than falling further behind. This handles codecs
            // whose decode time exceeds the frame interval (e.g. AV1 at 4K@120fps where
            // each decode takes ~25ms but frame interval is 8.33ms).
            const double FrameSkipThresholdMs = 200.0;
            const int MaxSkipFrames = 30; // cap to prevent infinite skip loops
            var audioClockPts = Volatile.Read(ref _audioClockPtsTicks);
            if (audioClockPts > 0)
            {
                var audioClockWall = Volatile.Read(ref _audioClockWallTicks);
                var wallElapsed = Stopwatch.GetTimestamp() - audioClockWall;
                var wallElapsedTicks = (long)((double)wallElapsed / Stopwatch.Frequency * TimeSpan.TicksPerSecond);
                var extrapolatedAudioTicks = audioClockPts + wallElapsedTicks;
                var driftMs = (videoFrame.Pts.Ticks - extrapolatedAudioTicks) / (double)TimeSpan.TicksPerMillisecond;

                if (driftMs < -FrameSkipThresholdMs)
                {
                    var skipped = 0;
                    while (skipped < MaxSkipFrames && driftMs < -FrameSkipThresholdMs)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        // Release the frame without displaying it
                        FlashbackDecoder.ReleaseHeldFrame(videoFrame);
                        Interlocked.Increment(ref _playbackDroppedFrames);
                        skipped++;

                        if (!decoder.TryDecodeNextVideoFrame(out videoFrame))
                            return HandleEndOfSegment(decoder, pacingStopwatch, frozenValidStart);

                        // Recompute drift with the new frame's PTS
                        wallElapsed = Stopwatch.GetTimestamp() - audioClockWall;
                        wallElapsedTicks = (long)((double)wallElapsed / Stopwatch.Frequency * TimeSpan.TicksPerSecond);
                        extrapolatedAudioTicks = audioClockPts + wallElapsedTicks;
                        driftMs = (videoFrame.Pts.Ticks - extrapolatedAudioTicks) / (double)TimeSpan.TicksPerMillisecond;
                    }

                    if (skipped > 0)
                    {
                        Logger.Log($"FLASHBACK_PLAYBACK_FRAME_SKIP count={skipped} drift_after_ms={driftMs:F1}");
                    }
                }
            }

            if (!TrySubmitAndHoldFrame(videoFrame, "playback"))
            {
                return false;
            }
            Interlocked.Exchange(ref _lastVideoPtsTicks, videoFrame.Pts.Ticks);

            var newPosition = videoFrame.Pts - frozenValidStart;
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
            var groundTruthFps = _bufferManager.EncodeFrameRate;
            if (groundTruthFps > 0)
                frameDuration = TimeSpan.FromSeconds(1.0 / groundTruthFps);
            else
                frameDuration = TimeSpan.FromSeconds(1.0 / Math.Max(decoder.FrameRate, 1.0));

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
        catch (Exception ex)
        {
            SnapToLiveOnError(decoder, ex, ref fileOpen);
            return false;
        }
    }

    private bool HandleEndOfSegment(
        FlashbackDecoder decoder,
        Stopwatch pacingStopwatch,
        TimeSpan frozenValidStart)
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
            lastFrameAbsPts = PlaybackPosition + frozenValidStart;
        var gapFromLive = (latestAbsPts - lastFrameAbsPts).TotalMilliseconds;
        var pos = PlaybackPosition;

        if (gapFromLive > 2000)
        {
            var currentOpenFilePath = _currentOpenFilePath;
            var nextFile = currentOpenFilePath != null
                ? _bufferManager.GetNextSegmentFile(currentOpenFilePath)
                : null;
            if (nextFile != null && nextFile != currentOpenFilePath)
            {
                Interlocked.Increment(ref _playbackSegmentSwitches);
                Interlocked.Exchange(ref _lastSegmentSwitchUtcUnixMs, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
                Logger.Log($"FLASHBACK_PLAYBACK_SEGMENT_SWITCH pos_ms={(long)pos.TotalMilliseconds} next='{System.IO.Path.GetFileName(nextFile)}'");
                decoder.CloseFile();
                decoder.OpenFile(nextFile);
                _currentOpenFilePath = nextFile;
                // Gate audio at last played position, not seek target — audio between
                // the last played sample and the seek point would otherwise be dropped,
                // causing an audible gap at segment boundaries.
                var audioGate = Interlocked.Read(ref _lastAudioPtsTicks);
                decoder.AudioChunkCallback = null;
                var segSwitchTarget = pos + frozenValidStart;
                var nextSegmentStart = _bufferManager.GetSegmentStartPts(nextFile);
                if (nextSegmentStart.HasValue && segSwitchTarget < nextSegmentStart.Value)
                    segSwitchTarget = nextSegmentStart.Value;
                decoder.SeekTo(segSwitchTarget);
                RestoreAudioCallback(decoder, audioGate);
                pacingStopwatch.Restart();
                return true;
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
                decoder.CloseFile();
                decoder.OpenFile(currentOpenFilePath);
                var fmpAudioGate = Interlocked.Read(ref _lastAudioPtsTicks);
                decoder.AudioChunkCallback = null;
                decoder.SeekTo(resumeTarget);
                RestoreAudioCallback(decoder, fmpAudioGate);
                pacingStopwatch.Restart();
                return true;
            }
        }

        if (_commandChannel.Reader.TryPeek(out _) || _disposedFlag != 0)
        {
            pacingStopwatch.Restart();
            return true;
        }

        Interlocked.Increment(ref _playbackWriteHeadWaits);
        Interlocked.Exchange(ref _lastWriteHeadWaitGapMs, Math.Max(0, (long)gapFromLive));
        Logger.Log($"FLASHBACK_PLAYBACK_WRITE_HEAD_WAIT gapFromLive_ms={gapFromLive:F0} pos_ms={(long)pos.TotalMilliseconds} lastFrameAbsPts_ms={(long)lastFrameAbsPts.TotalMilliseconds} latestPts_ms={(long)latestAbsPts.TotalMilliseconds}");
        Thread.Sleep(50);
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
        ref bool fileOpen)
    {
        var absoluteLatestPts = _bufferManager.LatestPts;
        if (Interlocked.Read(ref _playbackFrameCount) > 60 &&
            absoluteLatestPts - absoluteFramePts <= TimeSpan.FromMilliseconds(2000))
        {
            Interlocked.Increment(ref _playbackNearLiveSnaps);
            var gapMs = (absoluteLatestPts - absoluteFramePts).TotalMilliseconds;
            Logger.Log($"FLASHBACK_PLAYBACK_NEAR_LIVE_SNAP pos_ms={(long)bufferPosition.TotalMilliseconds} framePts_ms={(long)absoluteFramePts.TotalMilliseconds} latestPts_ms={(long)absoluteLatestPts.TotalMilliseconds} gapFromLive_ms={gapMs:F0} frameCount={_playbackFrameCount}");
            if (decoder.IsOpen) decoder.CloseFile();
            fileOpen = false;
            Interlocked.Exchange(ref _lastAudioPtsTicks, 0);
            Interlocked.Exchange(ref _lastVideoPtsTicks, 0);
            Interlocked.Exchange(ref _suppressAudioUntilPtsTicks, 0);
            RestoreLiveAudio();
            SafeResumePreviewSubmission("near_live");
            SetState(FlashbackPlaybackState.Live);
            return true;
        }
        return false;
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

            // ffplay: sync_threshold = clamp(frame_duration, 40ms, 100ms)
            var syncThresholdMs = Math.Clamp(nominalDelayMs, 40.0, 100.0);

            double adjustedDelayMs;
            if (diffMs > syncThresholdMs)
            {
                // Video ahead — double delay to let audio catch up
                adjustedDelayMs = nominalDelayMs * 2;
            }
            else if (diffMs < -syncThresholdMs)
            {
                // Video behind — shrink delay to catch up
                adjustedDelayMs = Math.Max(0, nominalDelayMs + diffMs);
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

    private void UpdateCadenceMetrics(Stopwatch pacingStopwatch, double expectedFrameMs)
    {
        var frameNum = Interlocked.Increment(ref _playbackFrameCount);
        var intervalMs = pacingStopwatch.Elapsed.TotalMilliseconds;
        pacingStopwatch.Restart();
        TrackPlaybackCadence(intervalMs, expectedFrameMs);

        if (frameNum == 1)
        {
            _playbackFpsClock.Restart();
        }
        else if (frameNum % 60 == 0)
        {
            var wallMs = _playbackFpsClock.ElapsedMilliseconds;
            if (wallMs > 0)
            {
                _playbackObservedFps = frameNum * 1000.0 / wallMs;
                _playbackAvgFrameMs = wallMs / (double)frameNum;
            }
        }
    }

    private void SnapToLiveOnError(FlashbackDecoder decoder, Exception ex, ref bool fileOpen)
    {
        Interlocked.Increment(ref _playbackDecodeErrorSnaps);
        var pos = PlaybackPosition;
        var bufDur = _bufferManager.BufferedDuration;
        var gapMs = (bufDur - pos).TotalMilliseconds;
        Logger.Log($"FLASHBACK_PLAYBACK_DECODE_ERROR_SNAP_TO_LIVE error='{ex.Message}' pos_ms={(long)pos.TotalMilliseconds} bufferDur_ms={(long)bufDur.TotalMilliseconds} gapFromLive_ms={gapMs:F0} frameCount={_playbackFrameCount}");
        Logger.Log($"FLASHBACK_PLAYBACK_DECODE_ERROR_STACK {ex.StackTrace?.Replace("\r\n", " | ")}");
        if (decoder.IsOpen) decoder.CloseFile();
        fileOpen = false;
        Interlocked.Exchange(ref _lastAudioPtsTicks, 0);
        Interlocked.Exchange(ref _lastVideoPtsTicks, 0);
        Interlocked.Exchange(ref _suppressAudioUntilPtsTicks, 0);
        RestoreLiveAudio();
        SafeResumePreviewSubmission("decode_error");
        SetState(FlashbackPlaybackState.Live);
    }

    /// <summary>
    /// Submits a decoded frame to the preview renderer — GPU texture or raw CPU data.
    /// </summary>
    private void SubmitFrame(DecodedVideoFrame frame)
    {
        if (frame.IsD3D11Texture)
        {
            if (frame.TexturePtr == IntPtr.Zero)
            {
                Logger.Log("FLASHBACK_PLAYBACK_SUBMIT_SKIP reason=null_texture");
                return;
            }
            _previewSink?.SubmitTexture(
                frame.TexturePtr, frame.SubresourceIndex,
                frame.Width, frame.Height, frame.IsHdr, arrivalTick: 0);
        }
        else
        {
            _previewSink?.SubmitRawFrame(
                frame.Data, frame.DataLength,
                frame.Width, frame.Height, frame.IsHdr, arrivalTick: 0);
        }
    }

    /// <summary>
    /// Returns true if the given path is the active fMP4 segment. The reopen-and-retry
    /// workaround only applies to fMP4 (fragment index goes stale); transport streams
    /// handle appended data via eof_reached reset and don't need reopening.
    /// </summary>
    private bool IsActiveFmp4Segment(string? path)
        => path != null
        && path == _bufferManager.ActiveFilePath
        && path.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase);

    // --- Position mapping ---

    private TimeSpan ClampPosition(TimeSpan position)
    {
        var inTicks = Interlocked.Read(ref _inPointTicks);
        var min = inTicks == long.MinValue ? TimeSpan.Zero : TimeSpan.FromTicks(inTicks);
        var outTicks = Interlocked.Read(ref _outPointTicks);
        var max = outTicks == long.MinValue ? _bufferManager.BufferedDuration : TimeSpan.FromTicks(outTicks);
        if (position < min) return min;
        if (position > max) return max;
        return position;
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
    public string DecoderHwAccel => _decoderHwAccel;
    public long PlaybackFrameCount => Interlocked.Read(ref _playbackFrameCount);
    public long PlaybackLateFrames => Interlocked.Read(ref _playbackLateFrames);
    public long PlaybackDroppedFrames => Interlocked.Read(ref _playbackDroppedFrames);
    public long PlaybackSegmentSwitches => Interlocked.Read(ref _playbackSegmentSwitches);
    public long PlaybackFmp4Reopens => Interlocked.Read(ref _playbackFmp4Reopens);
    public long PlaybackWriteHeadWaits => Interlocked.Read(ref _playbackWriteHeadWaits);
    public long PlaybackNearLiveSnaps => Interlocked.Read(ref _playbackNearLiveSnaps);
    public long PlaybackDecodeErrorSnaps => Interlocked.Read(ref _playbackDecodeErrorSnaps);
    public long LastSegmentSwitchUtcUnixMs => Interlocked.Read(ref _lastSegmentSwitchUtcUnixMs);
    public long LastFmp4ReopenUtcUnixMs => Interlocked.Read(ref _lastFmp4ReopenUtcUnixMs);
    public long LastWriteHeadWaitGapMs => Interlocked.Read(ref _lastWriteHeadWaitGapMs);
    public double PlaybackObservedFps => _playbackObservedFps;
    public double PlaybackAvgFrameMs => _playbackAvgFrameMs;
    public long CommandsEnqueued => Interlocked.Read(ref _commandsEnqueued);
    public long CommandsProcessed => Interlocked.Read(ref _commandsProcessed);
    public long CommandsDropped => Interlocked.Read(ref _commandsDropped);
    public long CommandsSkippedNotReady => Interlocked.Read(ref _commandsSkippedNotReady);
    public int PendingCommands => Volatile.Read(ref _pendingCommands);
    public int MaxPendingCommands => Volatile.Read(ref _maxPendingCommands);
    public long LastCommandQueueLatencyMs => Interlocked.Read(ref _lastCommandQueueLatencyMs);
    public long MaxCommandQueueLatencyMs => Interlocked.Read(ref _maxCommandQueueLatencyMs);
    public long LastCommandQueuedUtcUnixMs => Interlocked.Read(ref _lastCommandQueuedUtcUnixMs);
    public long LastCommandProcessedUtcUnixMs => Interlocked.Read(ref _lastCommandProcessedUtcUnixMs);
    public string LastCommandQueued => _lastCommandQueued;
    public string LastCommandProcessed => _lastCommandProcessed;
    public string LastCommandFailure => _lastCommandFailure;
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
    private bool IsNotReady(CommandKind kind)
    {
        if (IsReady) return false;
        Interlocked.Increment(ref _commandsSkippedNotReady);
        _lastCommandFailure = $"not_ready:{kind}";
        Logger.Log($"FLASHBACK_PLAYBACK_CMD_SKIP kind={kind} reason=not_ready initialized={_initialized} disposed={_disposedFlag != 0}");
        return true;
    }

    private void TrackCommandDequeued(PlaybackCommand command)
    {
        Interlocked.Increment(ref _commandsProcessed);
        DecrementPendingCommands();
        TrackCommandQueueLatency(command);
        Interlocked.Exchange(ref _lastCommandProcessedUtcUnixMs, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        _lastCommandProcessed = command.Kind.ToString();
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

    private void ResetPlaybackMetrics()
    {
        Interlocked.Exchange(ref _playbackFrameCount, 0);
        Interlocked.Exchange(ref _playbackLateFrames, 0);
        Interlocked.Exchange(ref _playbackDroppedFrames, 0);
        // Reset audio clock extrapolation so stale PTS doesn't cause a jump
        Interlocked.Exchange(ref _audioClockPtsTicks, 0);
        Interlocked.Exchange(ref _audioClockWallTicks, 0);
        _playbackObservedFps = 0;
        _playbackAvgFrameMs = 0;
        _playbackFpsClock.Reset();
        Interlocked.Exchange(ref _playbackSlowFrameCount, 0);
        lock (_playbackCadenceLock)
        {
            Array.Clear(_playbackFrameIntervalsMs);
            _playbackFrameIntervalHead = 0;
            _playbackFrameIntervalCount = 0;
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

        if (_audioPlayback != null)
        {
            decoder.AudioChunkCallback = chunk =>
            {
                var pb = _audioPlayback;
                if (pb == null)
                {
                    if (chunk.Samples is { Length: > 0 }) ArrayPool<byte>.Shared.Return(chunk.Samples);
                    return;
                }

                // Skip invalid or non-monotonic PTS (L8 fix)
                var prevPts = Interlocked.Read(ref _lastAudioPtsTicks);
                if (chunk.Pts.Ticks <= 0 || chunk.Pts.Ticks < prevPts)
                {
                    if (chunk.Samples is { Length: > 0 }) ArrayPool<byte>.Shared.Return(chunk.Samples);
                    return;
                }

                // Skip audio before the video position at seek time — these are
                // from the keyframe→target forward decode and would cause drift.
                if (videoPtsGate > 0 && chunk.Pts.Ticks < videoPtsGate)
                {
                    if (chunk.Samples is { Length: > 0 }) ArrayPool<byte>.Shared.Return(chunk.Samples);
                    return;
                }

                Interlocked.Exchange(ref _lastAudioPtsTicks, chunk.Pts.Ticks);
                pb.EnqueuePooledSamples(chunk.Samples, chunk.ValidLength, chunk.Pts.Ticks);
            };
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
            Logger.Log($"FLASHBACK_PLAYBACK_AUDIO_WARN op=suppress_live_set_playback msg='{ex.Message}'");
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
            Logger.Log($"FLASHBACK_PLAYBACK_AUDIO_WARN op=restore_live_set_playback msg='{ex.Message}'");
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
            Logger.Log($"FLASHBACK_PLAYBACK_PREVIEW_WARN op=suppress operation={operation} msg='{ex.Message}'");
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
            Logger.Log($"FLASHBACK_PLAYBACK_PREVIEW_WARN op=resume operation={operation} msg='{ex.Message}'");
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
            Logger.Log($"FLASHBACK_PLAYBACK_AUDIO_WARN op=pause operation={operation} msg='{ex.Message}'");
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
            Logger.Log($"FLASHBACK_PLAYBACK_AUDIO_WARN op=resume operation={operation} msg='{ex.Message}'");
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
            Logger.Log($"FLASHBACK_PLAYBACK_AUDIO_WARN op=flush operation={operation} msg='{ex.Message}'");
        }
    }

    // --- Timer resolution P/Invoke (1ms sleep granularity for 120fps pacing) ---

    [DllImport("winmm.dll", ExactSpelling = true)]
    private static extern uint timeBeginPeriod(uint uMilliseconds);

    [DllImport("winmm.dll", ExactSpelling = true)]
    private static extern uint timeEndPeriod(uint uMilliseconds);
}

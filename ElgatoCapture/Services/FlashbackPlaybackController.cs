using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Channels;
using ElgatoCapture.Models;

namespace ElgatoCapture.Services;

internal sealed class FlashbackPlaybackController : IDisposable
{
    // --- Command types marshalled to the playback thread ---
    private enum CommandKind
    {
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
    }

    // --- Dependencies ---
    private readonly FlashbackBufferManager _bufferManager;
    private IPreviewFrameSink? _previewSink;
    private UnifiedVideoCapture? _videoCapture;
    private WasapiAudioPlayback? _audioPlayback;
    private WasapiAudioCapture? _audioCapture;

    // --- State (read from UI thread, written primarily from playback thread) ---
    private volatile FlashbackPlaybackState _state = FlashbackPlaybackState.Live;
    private long _playbackPositionTicks;
    private volatile bool _initialized;
    private bool _disposed;
    private volatile string _decoderHwAccel = "N/A";

    // --- A/V sync tracking ---
    private long _lastAudioPtsTicks;  // PTS of last audio chunk delivered to WASAPI
    private long _lastVideoPtsTicks;  // PTS of last video frame displayed

    // --- Playback cadence metrics (written on playback thread, read from UI/diag) ---
    private long _playbackFrameCount;
    private long _playbackLateFrames;
    private double _playbackObservedFps;
    private double _playbackAvgFrameMs;
    private readonly Stopwatch _playbackFpsClock = new();

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
    private readonly Channel<PlaybackCommand> _commandChannel =
        Channel.CreateUnbounded<PlaybackCommand>(new UnboundedChannelOptions { SingleReader = true });

    // --- Events ---
    public event Action<FlashbackPlaybackState>? StateChanged;

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

    // --- Lifecycle ---

    public void Initialize(
        IPreviewFrameSink previewSink,
        UnifiedVideoCapture videoCapture,
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
        if (!IsReady)
        {
            Logger.Log($"FLASHBACK_PLAYBACK_CMD_SKIP kind={CommandKind.BeginScrub} reason=not_ready initialized={_initialized} disposed={_disposed}");
            return;
        }
        EnsurePlaybackThread();
        SendCommand(new PlaybackCommand { Kind = CommandKind.BeginScrub, Position = position });
    }

    public void UpdateScrub(TimeSpan position)
    {
        if (!IsReady)
        {
            Logger.Log($"FLASHBACK_PLAYBACK_CMD_SKIP kind={CommandKind.UpdateScrub} reason=not_ready initialized={_initialized} disposed={_disposed}");
            return;
        }
        SendCommand(new PlaybackCommand { Kind = CommandKind.UpdateScrub, Position = position });
    }

    public void EndScrub()
    {
        if (!IsReady)
        {
            Logger.Log($"FLASHBACK_PLAYBACK_CMD_SKIP kind={CommandKind.EndScrub} reason=not_ready initialized={_initialized} disposed={_disposed}");
            return;
        }
        SendCommand(new PlaybackCommand { Kind = CommandKind.EndScrub });
    }

    public void Play()
    {
        if (!IsReady)
        {
            Logger.Log($"FLASHBACK_PLAYBACK_CMD_SKIP kind={CommandKind.Play} reason=not_ready initialized={_initialized} disposed={_disposed}");
            return;
        }
        EnsurePlaybackThread();
        SendCommand(new PlaybackCommand { Kind = CommandKind.Play });
    }

    public void Pause()
    {
        if (!IsReady)
        {
            Logger.Log($"FLASHBACK_PLAYBACK_CMD_SKIP kind={CommandKind.Pause} reason=not_ready initialized={_initialized} disposed={_disposed}");
            return;
        }
        EnsurePlaybackThread(); // Thread must be running to handle Live→Paused
        SendCommand(new PlaybackCommand { Kind = CommandKind.Pause });
    }

    public void GoLive()
    {
        if (!IsReady)
        {
            Logger.Log($"FLASHBACK_PLAYBACK_CMD_SKIP kind={CommandKind.GoLive} reason=not_ready initialized={_initialized} disposed={_disposed}");
            return;
        }
        SendCommand(new PlaybackCommand { Kind = CommandKind.GoLive });
    }

    public void NudgePosition(TimeSpan delta)
    {
        if (!IsReady)
        {
            Logger.Log($"FLASHBACK_PLAYBACK_CMD_SKIP kind={CommandKind.Nudge} reason=not_ready initialized={_initialized} disposed={_disposed}");
            return;
        }
        SendCommand(new PlaybackCommand { Kind = CommandKind.Nudge, Delta = delta });
    }

    // --- In/Out point helpers ---

    public void SetInPoint()
    {
        InPoint = PlaybackPosition;
        Logger.Log($"FLASHBACK_PLAYBACK_SET_IN pos_ms={(long)PlaybackPosition.TotalMilliseconds}");
    }

    public void SetOutPoint()
    {
        OutPoint = PlaybackPosition;
        Logger.Log($"FLASHBACK_PLAYBACK_SET_OUT pos_ms={(long)PlaybackPosition.TotalMilliseconds}");
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
        if (_disposed) return;
        _disposed = true;

        Logger.Log($"FLASHBACK_PLAYBACK_DISPOSE_BEGIN state={_state} initialized={_initialized}");
        StopPlaybackThread();
        Logger.Log("FLASHBACK_PLAYBACK_DISPOSED");
    }

    // --- Command dispatch ---

    private void SendCommand(PlaybackCommand command)
    {
        if (!_commandChannel.Writer.TryWrite(command))
        {
            Logger.Log($"FLASHBACK_PLAYBACK_CMD_DROP kind={command.Kind}");
        }
    }

    private void EnsurePlaybackThread()
    {
        if (Interlocked.CompareExchange(ref _playbackThreadStarted, 1, 0) != 0)
            return;

        _playbackThread = new Thread(PlaybackThreadEntry)
        {
            Name = "FlashbackPlayback",
            IsBackground = true,
            Priority = ThreadPriority.AboveNormal
        };
        _playbackThread.Start();
        Logger.Log("FLASHBACK_PLAYBACK_THREAD_START");
    }

    private void StopPlaybackThread()
    {
        SendCommand(new PlaybackCommand { Kind = CommandKind.Stop });

        var thread = _playbackThread;
        if (thread is { IsAlive: true })
        {
            if (!thread.Join(TimeSpan.FromSeconds(3)))
            {
                Logger.Log("FLASHBACK_PLAYBACK_THREAD_JOIN_TIMEOUT");
            }
        }

        _playbackThread = null;
        Volatile.Write(ref _playbackThreadStarted, 0);
    }

    // --- Playback thread ---

    private void PlaybackThreadEntry()
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
                        if (decoder is { IsOpen: true })
                        {
                            if (!PaceAndDecodeFrame(decoder, pacingStopwatch, ref frameDuration, ref fileOpen, frozenValidStart))
                            {
                                isPlaying = false;
                            }
                        }
                        continue;
                    }
                }
                else
                {
                    if (!_commandChannel.Reader.TryRead(out cmd))
                    {
                        _commandChannel.Reader.WaitToReadAsync().AsTask().GetAwaiter().GetResult();
                        if (_disposed)
                        {
                            Logger.Log("FLASHBACK_PLAYBACK_THREAD_EXIT");
                            return;
                        }
                        if (!_commandChannel.Reader.TryRead(out cmd))
                        {
                            continue;
                        }
                    }
                }

                switch (cmd.Kind)
                {
                    case CommandKind.Stop:
                        CleanupDecoder(ref decoder, ref fileOpen);
                        Logger.Log("FLASHBACK_PLAYBACK_THREAD_EXIT");
                        return;

                    case CommandKind.BeginScrub:
                        isPlaying = false;
                        isScrubbing = true;
                        frozenValidStart = _bufferManager.ValidStartPts;
                        _videoCapture?.SuppressPreviewSubmission();
                        SuppressLiveAudio();
                        _audioPlayback?.PauseRendering();
                        SetState(FlashbackPlaybackState.Scrubbing);

                        decoder ??= CreateDecoder();
                        EnsureFileOpen(decoder, ref fileOpen, cmd.Position + frozenValidStart);
                        SeekAndDisplayKeyframe(decoder, cmd.Position, frozenValidStart);
                        break;

                    case CommandKind.UpdateScrub:
                        if (!isScrubbing) break;
                        decoder ??= CreateDecoder();
                        EnsureFileOpen(decoder, ref fileOpen, cmd.Position + frozenValidStart);
                        SeekAndDisplayKeyframe(decoder, cmd.Position, frozenValidStart);
                        break;

                    case CommandKind.EndScrub:
                        if (!isScrubbing) break;
                        isScrubbing = false;
                        isPlaying = true;
                        ResetPlaybackMetrics();
                        pacingStopwatch.Restart();
                        if (decoder is { IsOpen: true })
                        {
                            frameDuration = TimeSpan.FromSeconds(1.0 / Math.Max(decoder.FrameRate, 1.0));
                        }
                        RestoreAudioCallback(decoder);
                        _audioPlayback?.Flush();
                        _audioPlayback?.ResumeRendering();
                        SetState(FlashbackPlaybackState.Playing);
                        var endScrubBufDur = _bufferManager.BufferedDuration;
                        Logger.Log($"FLASHBACK_ENDSCRUB_AUTO_PLAY pos_ms={(long)PlaybackPosition.TotalMilliseconds} bufferDur_ms={(long)endScrubBufDur.TotalMilliseconds} gapFromLive_ms={(endScrubBufDur - PlaybackPosition).TotalMilliseconds:F0}");
                        break;

                    case CommandKind.Play:
                        if (isPlaying) break;
                        isScrubbing = false;
                        isPlaying = true;
                        _videoCapture?.SuppressPreviewSubmission();
                        SuppressLiveAudio();
                        _audioPlayback?.PauseRendering();
                        ResetPlaybackMetrics();
                        pacingStopwatch.Restart();

                        if (State == FlashbackPlaybackState.Live)
                            frozenValidStart = _bufferManager.ValidStartPts;
                        decoder ??= CreateDecoder();
                        EnsureFileOpen(decoder, ref fileOpen, PlaybackPosition + frozenValidStart);
                        if (decoder.IsOpen)
                        {
                            // Suppress audio during seek-forward to prevent enqueuing
                            // stale audio between the keyframe and the target position.
                            // GOP can be up to 2s — that's the user-reported desync.
                            decoder.AudioChunkCallback = null;
                            decoder.SeekTo(PlaybackPosition);
                            frameDuration = TimeSpan.FromSeconds(1.0 / Math.Max(decoder.FrameRate, 1.0));
                        }
                        RestoreAudioCallback(decoder);
                        _audioPlayback?.Flush();
                        _audioPlayback?.ResumeRendering();

                        SetState(FlashbackPlaybackState.Playing);
                        Logger.Log($"FLASHBACK_PLAYBACK_PLAY pos_ms={(long)PlaybackPosition.TotalMilliseconds}");
                        break;

                    case CommandKind.Pause:
                        if (isPlaying)
                        {
                            // Pause from Playing state
                            isPlaying = false;
                            _audioPlayback?.PauseRendering();
                            pacingStopwatch.Stop();
                            SetState(FlashbackPlaybackState.Paused);
                            Logger.Log($"FLASHBACK_PLAYBACK_PAUSE pos_ms={(long)PlaybackPosition.TotalMilliseconds}");
                        }
                        else if (State == FlashbackPlaybackState.Live)
                        {
                            // Pause from Live state — freeze at current buffer edge
                            _videoCapture?.SuppressPreviewSubmission();
                            SuppressLiveAudio();
                            _audioPlayback?.PauseRendering();

                            frozenValidStart = _bufferManager.ValidStartPts;
                            var pausePos = _bufferManager.BufferedDuration;
                            PlaybackPosition = pausePos;

                            decoder ??= CreateDecoder();
                            EnsureFileOpen(decoder, ref fileOpen, pausePos + frozenValidStart);
                            SeekAndDisplayKeyframe(decoder, pausePos, frozenValidStart);

                            SetState(FlashbackPlaybackState.Paused);
                            Logger.Log($"FLASHBACK_PLAYBACK_PAUSE_FROM_LIVE pos_ms={(long)pausePos.TotalMilliseconds}");
                        }
                        break;

                    case CommandKind.GoLive:
                        isPlaying = false;
                        isScrubbing = false;
                        CleanupDecoder(ref decoder, ref fileOpen);
                        RestoreLiveAudio();
                        _videoCapture?.ResumePreviewSubmission();
                        SetState(FlashbackPlaybackState.Live);
                        Logger.Log("FLASHBACK_PLAYBACK_GO_LIVE");
                        break;

                    case CommandKind.Nudge:
                        var nudgedPos = PlaybackPosition + cmd.Delta;
                        nudgedPos = ClampPosition(nudgedPos);
                        if (decoder != null)
                        {
                            EnsureFileOpen(decoder, ref fileOpen, nudgedPos + frozenValidStart);
                            SeekAndDisplayKeyframe(decoder, nudgedPos, frozenValidStart);
                        }
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_PLAYBACK_FATAL error='{ex.Message}'");
            CleanupDecoder(ref decoder, ref fileOpen);
            try { RestoreLiveAudio(); } catch { /* best effort */ }
            try { _videoCapture?.ResumePreviewSubmission(); } catch { /* best effort */ }
            SetState(FlashbackPlaybackState.Live);
        }
        finally
        {
            timeEndPeriod(1);
            Interlocked.Exchange(ref _playbackThreadStarted, 0);
        }

        Logger.Log("FLASHBACK_PLAYBACK_THREAD_EXIT");
    }

    // --- Decode helpers ---

    private FlashbackDecoder CreateDecoder()
    {
        Logger.Log("FLASHBACK_PLAYBACK_DECODER_CREATE");
        var decoder = new FlashbackDecoder();

        // Get D3D11 device pointers for GPU-direct decode
        var d3dManager = _videoCapture?.D3DManager;
        var devicePtr = d3dManager?.Device?.NativePointer ?? IntPtr.Zero;
        var contextPtr = d3dManager?.ImmediateContext?.NativePointer ?? IntPtr.Zero;
        decoder.Initialize(devicePtr, contextPtr);

        // Wire up inline audio: decoded audio chunks are delivered as a side
        // effect of reading video packets — no separate audio loop needed.
        var playback = _audioPlayback;
        if (playback != null)
        {
            decoder.AudioChunkCallback = chunk =>
            {
                Interlocked.Exchange(ref _lastAudioPtsTicks, chunk.Pts.Ticks);
                playback.EnqueuePooledSamples(chunk.Samples, chunk.ValidLength, chunk.Pts.Ticks);
            };
        }

        return decoder;
    }

    private string? _currentOpenFilePath;

    private void EnsureFileOpen(FlashbackDecoder decoder, ref bool fileOpen, TimeSpan? targetPts = null)
    {
        // Determine which segment file contains the target position
        var filePath = targetPts.HasValue
            ? _bufferManager.GetSegmentFileForPosition(targetPts.Value)
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

    private void SeekAndDisplayKeyframe(FlashbackDecoder decoder, TimeSpan bufferPosition, TimeSpan validStartPts = default)
    {
        // Suppress audio delivery during scrub — prevents audio accumulation
        // in the WASAPI queue. Audio callback is re-enabled on Play/EndScrub.
        decoder.AudioChunkCallback = null;
        _audioPlayback?.Flush();

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
            // Map buffer position to file PTS (offset by valid start)
            var filePts = bufferPosition + (validStartPts > TimeSpan.Zero ? validStartPts : _bufferManager.ValidStartPts);

            if (!decoder.SeekToKeyframe(filePts))
            {
                // Seek failed — use requested position as fallback
                PlaybackPosition = bufferPosition;
                Logger.Log($"FLASHBACK_PLAYBACK_SEEK_FAIL offset_ms={(long)filePts.TotalMilliseconds}");
                return;
            }

            var gotFrame = decoder.TryDecodeNextVideoFrame(out var frame);
            if (gotFrame)
            {
                try
                {
                    SubmitFrame(frame);
                }
                finally
                {
                    FlashbackDecoder.ReleaseHeldFrame(frame);
                }

                // Set position to actual decoded frame PTS mapped back to buffer position
                var actualPosition = frame.Pts - (validStartPts > TimeSpan.Zero ? validStartPts : _bufferManager.ValidStartPts);
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
    /// Returns true if still playing, false if transitioned to another state.
    /// </summary>
    private bool PaceAndDecodeFrame(
        FlashbackDecoder decoder,
        Stopwatch pacingStopwatch,
        ref TimeSpan frameDuration,
        ref bool fileOpen,
        TimeSpan frozenValidStart)
    {
        try
        {
            if (!decoder.TryDecodeNextVideoFrame(out var videoFrame))
            {
                var bufDur = _bufferManager.BufferedDuration;
                var pos = PlaybackPosition;
                var gap = (bufDur - pos).TotalMilliseconds;

                // EOF could mean: (a) we've caught up to the write head of the active segment,
                // or (b) we've reached the end of a completed segment and need to switch files.
                // If there's a large gap to the live edge, try opening the next segment.
                if (gap > 2000)
                {
                    var nextFilePts = pos + frozenValidStart;
                    var nextFile = _bufferManager.GetSegmentFileForPosition(nextFilePts);
                    if (nextFile != null && nextFile != _currentOpenFilePath)
                    {
                        Logger.Log($"FLASHBACK_PLAYBACK_SEGMENT_SWITCH pos_ms={(long)pos.TotalMilliseconds} next='{System.IO.Path.GetFileName(nextFile)}'");
                        decoder.CloseFile();
                        decoder.OpenFile(nextFile);
                        _currentOpenFilePath = nextFile;
                        // Seek to our current position in the new file
                        decoder.AudioChunkCallback = null;
                        decoder.SeekTo(pos);
                        RestoreAudioCallback(decoder);
                        pacingStopwatch.Restart();
                        return true;
                    }
                }

                // At the write head of the active segment — wait for encoder to produce more data
                Logger.Log($"FLASHBACK_PLAYBACK_WRITE_HEAD_WAIT gap_ms={gap:F0} pos_ms={(long)pos.TotalMilliseconds} bufferDur_ms={(long)bufDur.TotalMilliseconds}");
                Thread.Sleep(50);
                pacingStopwatch.Restart();
                return true;
            }

            try
            {
                SubmitFrame(videoFrame);
            }
            finally
            {
                FlashbackDecoder.ReleaseHeldFrame(videoFrame);
            }
            Interlocked.Exchange(ref _lastVideoPtsTicks, videoFrame.Pts.Ticks);

            // Map file PTS back to buffer position using frozen valid start
            var newPosition = videoFrame.Pts - frozenValidStart;
            if (newPosition < TimeSpan.Zero) newPosition = TimeSpan.Zero;
            PlaybackPosition = newPosition;

            // Check OutPoint
            var outTicks = Interlocked.Read(ref _outPointTicks);
            if (outTicks != long.MinValue && newPosition >= TimeSpan.FromTicks(outTicks))
            {
                Logger.Log($"FLASHBACK_PLAYBACK_HIT_OUTPOINT pos_ms={(long)newPosition.TotalMilliseconds}");
                SetState(FlashbackPlaybackState.Paused);
                return false;
            }

            // Audio is decoded inline during FeedNextVideoPacket via AudioChunkCallback.
            // No separate audio loop — A/V stays naturally interleaved.

            // Check if near live edge (within 2s of buffered end)
            var bufferedDuration = _bufferManager.BufferedDuration;
            if (newPosition >= bufferedDuration - TimeSpan.FromMilliseconds(2000))
            {
                var gapMs = (bufferedDuration - newPosition).TotalMilliseconds;
                Logger.Log($"FLASHBACK_PLAYBACK_NEAR_LIVE_SNAP pos_ms={(long)newPosition.TotalMilliseconds} bufferDur_ms={(long)bufferedDuration.TotalMilliseconds} gapFromLive_ms={gapMs:F0} frameCount={_playbackFrameCount}");
                if (decoder.IsOpen) decoder.CloseFile();
                RestoreLiveAudio();
                _videoCapture?.ResumePreviewSubmission();
                SetState(FlashbackPlaybackState.Live);
                return false;
            }

            // Pace: wait for remainder of frame interval after decode work.
            var targetTicks = (long)(frameDuration.TotalSeconds * Stopwatch.Frequency);
            var actualElapsed = pacingStopwatch.ElapsedTicks;
            var remaining = targetTicks - actualElapsed;
            if (remaining > 0)
            {
                // Coarse sleep: yield CPU, leaving 2ms margin for spin
                var spinThresholdTicks = 2L * Stopwatch.Frequency / 1000;
                if (remaining > spinThresholdTicks)
                {
                    var sleepMs = (int)((remaining - spinThresholdTicks) * 1000 / Stopwatch.Frequency);
                    if (sleepMs > 0) Thread.Sleep(sleepMs);
                }
                // Fine spin-wait for sub-ms accuracy
                while (pacingStopwatch.ElapsedTicks < targetTicks)
                    Thread.SpinWait(1);
            }
            else
            {
                // Frame took longer than the budget — count as late
                Interlocked.Increment(ref _playbackLateFrames);
            }

            // Cadence metrics: compute rolling FPS every 60 frames
            var frameNum = Interlocked.Increment(ref _playbackFrameCount);
            var totalFrameMs = pacingStopwatch.ElapsedTicks * 1000.0 / Stopwatch.Frequency;
            pacingStopwatch.Restart();

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

            return true;
        }
        catch (Exception ex)
        {
            var pos = PlaybackPosition;
            var bufDur = _bufferManager.BufferedDuration;
            var gapMs = (bufDur - pos).TotalMilliseconds;
            Logger.Log($"FLASHBACK_PLAYBACK_DECODE_ERROR_SNAP_TO_LIVE error='{ex.Message}' pos_ms={(long)pos.TotalMilliseconds} bufferDur_ms={(long)bufDur.TotalMilliseconds} gapFromLive_ms={gapMs:F0} frameCount={_playbackFrameCount}");
            Logger.Log($"FLASHBACK_PLAYBACK_DECODE_ERROR_STACK {ex.StackTrace?.Replace("\r\n", " | ")}");
            // Can't recover — go live
            if (decoder.IsOpen) decoder.CloseFile();
            RestoreLiveAudio();
            _videoCapture?.ResumePreviewSubmission();
            SetState(FlashbackPlaybackState.Live);
            return false;
        }
    }

    /// <summary>
    /// Submits a decoded frame to the preview renderer — GPU texture or raw CPU data.
    /// </summary>
    private void SubmitFrame(DecodedVideoFrame frame)
    {
        if (frame.IsD3D11Texture)
        {
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
        try { StateChanged?.Invoke(newState); }
        catch (Exception ex) { Logger.Log($"FLASHBACK_PLAYBACK_STATE_HANDLER_FAIL type={ex.GetType().Name} msg={ex.Message}"); }
    }

    public bool IsInitialized => _initialized;
    public string DecoderHwAccel => _decoderHwAccel;
    public long PlaybackFrameCount => Interlocked.Read(ref _playbackFrameCount);
    public long PlaybackLateFrames => Interlocked.Read(ref _playbackLateFrames);
    public double PlaybackObservedFps => _playbackObservedFps;
    public double PlaybackAvgFrameMs => _playbackAvgFrameMs;

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

    private bool IsReady => _initialized && !_disposed;

    private void ResetPlaybackMetrics()
    {
        Interlocked.Exchange(ref _playbackFrameCount, 0);
        Interlocked.Exchange(ref _playbackLateFrames, 0);
        _playbackObservedFps = 0;
        _playbackAvgFrameMs = 0;
        _playbackFpsClock.Reset();
    }

    private void RestoreAudioCallback(FlashbackDecoder decoder)
    {
        var playback = _audioPlayback;
        if (playback != null)
        {
            decoder.AudioChunkCallback = chunk =>
            {
                Interlocked.Exchange(ref _lastAudioPtsTicks, chunk.Pts.Ticks);
                playback.EnqueuePooledSamples(chunk.Samples, chunk.ValidLength, chunk.Pts.Ticks);
            };
        }
    }

    private void SuppressLiveAudio()
    {
        _audioCapture?.SetPlayback(null);
        _audioPlayback?.Flush();
    }

    private void RestoreLiveAudio()
    {
        _audioPlayback?.Flush();
        _audioPlayback?.ResumeRendering();
        if (_audioCapture != null && _audioPlayback != null)
            _audioCapture.SetPlayback(_audioPlayback);
    }

    // --- Timer resolution P/Invoke (1ms sleep granularity for 120fps pacing) ---

    [DllImport("winmm.dll", ExactSpelling = true)]
    private static extern uint timeBeginPeriod(uint uMilliseconds);

    [DllImport("winmm.dll", ExactSpelling = true)]
    private static extern uint timeEndPeriod(uint uMilliseconds);
}

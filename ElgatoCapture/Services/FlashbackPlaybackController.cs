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
    private TimeSpan _playbackPosition;
    private volatile bool _initialized;
    private bool _disposed;
    private volatile string _decoderHwAccel = "N/A";

    // --- Playback cadence metrics (written on playback thread, read from UI/diag) ---
    private long _playbackFrameCount;
    private long _playbackLateFrames;
    private double _playbackObservedFps;
    private double _playbackAvgFrameMs;
    private readonly Stopwatch _playbackFpsClock = new();

    // --- In/Out points ---
    public TimeSpan? InPoint { get; set; }
    public TimeSpan? OutPoint { get; set; }

    // --- Playback thread ---
    private Thread? _playbackThread;
    private readonly Channel<PlaybackCommand> _commandChannel =
        Channel.CreateUnbounded<PlaybackCommand>(new UnboundedChannelOptions { SingleReader = true });

    // --- Events ---
    public event Action<FlashbackPlaybackState>? StateChanged;
    public event Action<TimeSpan>? PositionChanged;

    public FlashbackPlaybackController(FlashbackBufferManager bufferManager)
    {
        _bufferManager = bufferManager ?? throw new ArgumentNullException(nameof(bufferManager));
    }

    // --- Public properties ---

    public FlashbackPlaybackState State => _state;

    public TimeSpan PlaybackPosition
    {
        get
        {
            lock (this) return _playbackPosition;
        }
        private set
        {
            lock (this) _playbackPosition = value;
        }
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
        if (_playbackThread is { IsAlive: true }) return;

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
                            if (!PaceAndDecodeFrame(decoder, pacingStopwatch, ref frameDuration))
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
                        _videoCapture?.SuppressPreviewSubmission();
                        SuppressLiveAudio();
                        SetState(FlashbackPlaybackState.Scrubbing);

                        decoder ??= CreateDecoder();
                        EnsureFileOpen(decoder, ref fileOpen);
                        SeekAndDisplayKeyframe(decoder, cmd.Position);
                        break;

                    case CommandKind.UpdateScrub:
                        if (!isScrubbing) break;
                        decoder ??= CreateDecoder();
                        EnsureFileOpen(decoder, ref fileOpen);
                        SeekAndDisplayKeyframe(decoder, cmd.Position);
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
                        SetState(FlashbackPlaybackState.Playing);
                        Logger.Log($"FLASHBACK_PLAYBACK_PLAY pos_ms={(long)PlaybackPosition.TotalMilliseconds}");
                        break;

                    case CommandKind.Play:
                        if (isPlaying) break;
                        isScrubbing = false;
                        isPlaying = true;
                        _videoCapture?.SuppressPreviewSubmission();
                        SuppressLiveAudio();
                        ResetPlaybackMetrics();
                        pacingStopwatch.Restart();

                        decoder ??= CreateDecoder();
                        EnsureFileOpen(decoder, ref fileOpen);
                        if (decoder.IsOpen)
                        {
                            decoder.SeekTo(PlaybackPosition);
                            frameDuration = TimeSpan.FromSeconds(1.0 / Math.Max(decoder.FrameRate, 1.0));
                        }

                        SetState(FlashbackPlaybackState.Playing);
                        Logger.Log($"FLASHBACK_PLAYBACK_PLAY pos_ms={(long)PlaybackPosition.TotalMilliseconds}");
                        break;

                    case CommandKind.Pause:
                        if (!isPlaying) break;
                        isPlaying = false;
                        pacingStopwatch.Stop();
                        SetState(FlashbackPlaybackState.Paused);
                        Logger.Log($"FLASHBACK_PLAYBACK_PAUSE pos_ms={(long)PlaybackPosition.TotalMilliseconds}");
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
                            EnsureFileOpen(decoder, ref fileOpen);
                            SeekAndDisplayKeyframe(decoder, nudgedPos);
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
        return decoder;
    }

    private void EnsureFileOpen(FlashbackDecoder decoder, ref bool fileOpen)
    {
        if (fileOpen && decoder.IsOpen)
            return;

        var filePath = _bufferManager.ActiveFilePath;
        if (string.IsNullOrWhiteSpace(filePath))
        {
            Logger.Log("FLASHBACK_PLAYBACK_NO_FILE");
            return;
        }

        try
        {
            if (decoder.IsOpen) decoder.CloseFile();
            decoder.OpenFile(filePath);
            fileOpen = true;
            _decoderHwAccel = decoder.IsD3D11HwAccelerated ? "D3D11VA" : "Software";
            Logger.Log($"FLASHBACK_PLAYBACK_FILE_OPEN path='{filePath}' hw_accel={_decoderHwAccel}");
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_PLAYBACK_FILE_OPEN_ERROR path='{filePath}' error='{ex.Message}'");
            fileOpen = false;
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
        _decoderHwAccel = "N/A";
    }

    private void SeekAndDisplayKeyframe(FlashbackDecoder decoder, TimeSpan bufferPosition)
    {
        bufferPosition = ClampPosition(bufferPosition);
        PlaybackPosition = bufferPosition;
        PositionChanged?.Invoke(bufferPosition);

        if (!decoder.IsOpen)
        {
            Logger.Log($"FLASHBACK_PLAYBACK_SEEK_NO_FILE pos_ms={(long)bufferPosition.TotalMilliseconds}");
            return;
        }

        try
        {
            // Map buffer position to file PTS (offset by valid start)
            var filePts = bufferPosition + _bufferManager.ValidStartPts;

            if (!decoder.SeekToKeyframe(filePts))
            {
                Logger.Log($"FLASHBACK_PLAYBACK_SEEK_FAIL offset_ms={(long)filePts.TotalMilliseconds}");
                return;
            }

            var gotFrame = decoder.TryDecodeNextVideoFrame(out var frame);
            if (gotFrame)
            {
                SubmitFrame(frame);
            }

            Logger.Log($"FLASHBACK_PLAYBACK_SEEK_OK pos_ms={(long)bufferPosition.TotalMilliseconds} file_pts_ms={(long)filePts.TotalMilliseconds} got_frame={gotFrame}");
        }
        catch (Exception ex)
        {
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
        ref TimeSpan frameDuration)
    {
        try
        {
            if (!decoder.TryDecodeNextVideoFrame(out var videoFrame))
            {
                // EOF — reached live edge. Go live.
                Logger.Log("FLASHBACK_PLAYBACK_REACHED_LIVE_EDGE");
                if (decoder.IsOpen) decoder.CloseFile();
                RestoreLiveAudio();
                _videoCapture?.ResumePreviewSubmission();
                SetState(FlashbackPlaybackState.Live);
                return false;
            }

            SubmitFrame(videoFrame);

            // Map file PTS back to buffer position
            var newPosition = videoFrame.Pts - _bufferManager.ValidStartPts;
            if (newPosition < TimeSpan.Zero) newPosition = TimeSpan.Zero;
            PlaybackPosition = newPosition;
            PositionChanged?.Invoke(newPosition);

            // Check OutPoint
            if (OutPoint.HasValue && newPosition >= OutPoint.Value)
            {
                Logger.Log($"FLASHBACK_PLAYBACK_HIT_OUTPOINT pos_ms={(long)newPosition.TotalMilliseconds}");
                SetState(FlashbackPlaybackState.Paused);
                return false;
            }

            // Decode and submit audio (capped to prevent timing spikes)
            if (_audioPlayback != null)
            {
                var audioLimit = 4;
                while (audioLimit-- > 0 && decoder.TryDecodeNextAudioChunk(out var audioChunk))
                {
                    _audioPlayback.EnqueuePooledSamples(audioChunk.Samples, audioChunk.ValidLength);
                    if (audioChunk.Pts > videoFrame.Pts) break;
                }
            }

            // Check if near live edge (within 0.5s of buffered end)
            var bufferedDuration = _bufferManager.BufferedDuration;
            if (newPosition >= bufferedDuration - TimeSpan.FromMilliseconds(500))
            {
                Logger.Log("FLASHBACK_PLAYBACK_NEAR_LIVE_EDGE auto-transitioning");
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
            Logger.Log($"FLASHBACK_PLAYBACK_DECODE_ERROR error='{ex.Message}'");
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
        var min = InPoint ?? TimeSpan.Zero;
        var max = OutPoint ?? _bufferManager.BufferedDuration;
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
        StateChanged?.Invoke(newState);
    }

    public bool IsInitialized => _initialized;
    public string DecoderHwAccel => _decoderHwAccel;
    public long PlaybackFrameCount => Interlocked.Read(ref _playbackFrameCount);
    public long PlaybackLateFrames => Interlocked.Read(ref _playbackLateFrames);
    public double PlaybackObservedFps => _playbackObservedFps;
    public double PlaybackAvgFrameMs => _playbackAvgFrameMs;

    private bool IsReady => _initialized && !_disposed;

    private void ResetPlaybackMetrics()
    {
        Interlocked.Exchange(ref _playbackFrameCount, 0);
        Interlocked.Exchange(ref _playbackLateFrames, 0);
        _playbackObservedFps = 0;
        _playbackAvgFrameMs = 0;
        _playbackFpsClock.Reset();
    }

    private void SuppressLiveAudio()
    {
        _audioCapture?.SetPlayback(null);
    }

    private void RestoreLiveAudio()
    {
        if (_audioCapture != null && _audioPlayback != null)
            _audioCapture.SetPlayback(_audioPlayback);
    }

    private void ThrowIfNotInitialized()
    {
        if (!_initialized)
            throw new InvalidOperationException("FlashbackPlaybackController has not been initialized.");
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    // --- Timer resolution P/Invoke (1ms sleep granularity for 120fps pacing) ---

    [DllImport("winmm.dll", ExactSpelling = true)]
    private static extern uint timeBeginPeriod(uint uMilliseconds);

    [DllImport("winmm.dll", ExactSpelling = true)]
    private static extern uint timeEndPeriod(uint uMilliseconds);
}

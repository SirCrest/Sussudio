using System;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.Services.Audio;
using Sussudio.Services.Contracts;
using Sussudio.Services.Preview;

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
}

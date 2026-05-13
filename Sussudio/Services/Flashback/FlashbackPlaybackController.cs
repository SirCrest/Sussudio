using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.Services.Audio;
using Sussudio.Services.Contracts;
using Sussudio.Services.Preview;
using Sussudio.Services.Recording;
using Sussudio.Services.Runtime;

namespace Sussudio.Services.Flashback;

/// <summary>
/// Presentation-layer state machine for the Flashback timeline. It chooses
/// whether preview/audio should show live capture or decoded file playback, but
/// it never starts, stops, or throttles the capture pipeline.
/// </summary>
internal sealed partial class FlashbackPlaybackController : IDisposable
{
    private static readonly TimeSpan ActiveFmp4ReopenNearLiveGuard = TimeSpan.FromMilliseconds(250);
    private static readonly TimeSpan AdjacentSegmentSeekFallbackWindow = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan PlaybackThreadStopTimeout = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan PreviewDetachThreadStopTimeout = TimeSpan.FromSeconds(10);
    private const double PlaybackAudioPrebufferTargetMs = 180.0;
    private const double PlaybackAudioPrebufferDiscardThresholdMs = 250.0;
    private const int PlaybackAudioPrebufferTimeoutMs = 1000;
    private const int PlaybackAudioPrebufferRetryDelayMs = 20;
    private const int PlaybackAudioPrebufferDecodeFrameBudget = 96;

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
    private int _previewDetachStopTimeoutActive;
    private int _deferredPreviewAttachApplyRetryScheduled;
    private IPreviewFrameSink? _pendingPreviewSinkAfterDetachTimeout;
    private ILiveVideoSource? _pendingVideoCaptureAfterDetachTimeout;
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
    private string _pendingAudioMasterFallbackReason = string.Empty;
    private double _pendingAudioMasterFallbackDriftMs;
    private long _pendingAudioMasterFallbackClockAgeTicks;
    private readonly string _playbackMmcssTask = Environment.GetEnvironmentVariable("SUSSUDIO_FLASHBACK_PLAYBACK_MMCSS_TASK") ?? "Playback";
    private readonly int _playbackMmcssPriority = EnvironmentHelpers.GetIntFromEnv("SUSSUDIO_FLASHBACK_PLAYBACK_MMCSS_PRIORITY", 1, -2, 2);

    // --- Playback cadence metrics (written on playback thread, read from UI/diag) ---
    private long _playbackFrameCount;
    private long _playbackPreviewPresentId;
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
    private long _playbackSeekForwardDecodeCapHits;
    private int _lastPlaybackSeekHitForwardDecodeCap;
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
    private string _maxCommandQueueLatencyCommand = "None";
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

    // --- Playback thread ---
    private const int CommandQueueCapacity = 256;
    private const double FallbackPlaybackFrameRate = 60.0;
    private const double MaxPlaybackFrameRate = 1000.0;
    private const double ContinuousPlaybackNearLiveSnapFrames = 3.0;
    private static readonly TimeSpan ContinuousPlaybackNearLiveSnapMinimum = TimeSpan.FromMilliseconds(100);
    private static readonly TimeSpan RecoveryNearLiveSnapThreshold = TimeSpan.FromMilliseconds(2000);
    private const double MaxContinuousSoftwarePlaybackPixelRate = 3840.0 * 2160.0 * 60.0;
    private readonly object _playbackThreadSync = new();
    private Thread? _playbackThread;
    private int _playbackThreadStarted;
    private CancellationTokenSource? _playCts;
    private Channel<PlaybackCommand> _commandChannel;

    public FlashbackPlaybackController(FlashbackBufferManager bufferManager)
    {
        _bufferManager = bufferManager ?? throw new ArgumentNullException(nameof(bufferManager));
        _commandChannel = CreateCommandChannel();
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
        if (!EnsurePlaybackThread(CommandKind.Pause)) return false; // Thread must be running to handle Live→Paused
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

    // --- Dispose ---

    public void Dispose()
    {
        if (Interlocked.CompareExchange(ref _disposedFlag, 1, 0) != 0) return;

        Logger.Log($"FLASHBACK_PLAYBACK_DISPOSE_REQUEST state={_state} initialized={_initialized}");
        StopPlaybackThread(PlaybackThreadStopTimeout, "dispose");
        _initialized = false;
        Logger.Log("FLASHBACK_PLAYBACK_DISPOSED");
    }

    // (Command dispatch, coalescing, thread management, and audio prebuffer extracted to partials)
    // See: FlashbackPlaybackController.CommandQueue.cs, .Thread.cs, .AudioPrebuffer.cs

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

    private void RestoreLiveAfterPlaybackSubmitFailure(FlashbackDecoder decoder, ref bool fileOpen, string operation)
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
            var previewPresentId = Interlocked.Increment(ref _playbackPreviewPresentId);
            var countForPresentCadence = string.Equals(operation, "playback", StringComparison.Ordinal);
            SubmitFrame(previewSink, frame, previewPresentId, countForPresentCadence);
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
                    PlaybackPosition = bufferPosition;
                    RecordSeekDisplayDecodeFailure(kind, bufferPosition, filePts);
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

    private void RecordSeekDisplayDecodeFailure(CommandKind kind, TimeSpan bufferPosition, TimeSpan filePts)
    {
        Interlocked.Increment(ref _playbackDecodeErrorSnaps);
        RecordPlaybackDroppedFrame("seek_display_no_frame");
        SetSeekDisplayFailure(kind, "no_frame", bufferPosition);
        Logger.Log(
            $"FLASHBACK_PLAYBACK_SEEK_NO_FRAME_SNAP_TO_LIVE kind={kind} pos_ms={(long)bufferPosition.TotalMilliseconds} file_pts_ms={(long)filePts.TotalMilliseconds}");
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

    private bool TryReadNextPlaybackFrame(
        FlashbackDecoder decoder,
        Queue<DecodedVideoFrame> prebufferedFrames,
        out DecodedVideoFrame frame,
        CancellationToken cancellationToken)
    {
        if (prebufferedFrames.Count > 0)
        {
            frame = prebufferedFrames.Dequeue();
            return true;
        }

        return TryDecodeNextVideoFrameWithMetrics(decoder, out frame, cancellationToken);
    }

    private void ClearPrebufferedFrames(Queue<DecodedVideoFrame> prebufferedFrames, string operation)
    {
        if (prebufferedFrames.Count == 0)
        {
            return;
        }

        var released = 0;
        while (prebufferedFrames.Count > 0)
        {
            var frame = prebufferedFrames.Dequeue();
            ReleaseHeldFrameBestEffort(frame, $"prebuffer_{operation}");
            released++;
        }

        Logger.Log($"FLASHBACK_PLAYBACK_AUDIO_PREBUFFER_CLEAR operation={operation} frames={released}");
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
        Queue<DecodedVideoFrame> prebufferedFrames,
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
            if (!TryReadNextPlaybackFrame(decoder, prebufferedFrames, out var videoFrame, cancellationToken))
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
                    if (commandChannel.Reader.TryPeek(out _))
                    {
                        ReleaseHeldFrameBestEffort(videoFrame, "av_sync_skip_command_pending");
                        Logger.Log($"FLASHBACK_PLAYBACK_FRAME_SKIP_COMMAND_PENDING count={skipped} drift_ms={driftMs:F1}");
                        return true;
                    }

                    // Release the frame without displaying it
                    ReleaseHeldFrameBestEffort(videoFrame, "av_sync_skip");
                    RecordPlaybackDroppedFrame("av_sync_skip");
                    skipped++;

                    if (!TryReadNextPlaybackFrame(decoder, prebufferedFrames, out videoFrame, cancellationToken))
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
                Logger.Log($"FLASHBACK_PLAYBACK_SUBMIT_STOP pos_ms={(long)PlaybackPosition.TotalMilliseconds}");
                RestoreLiveAfterPlaybackSubmitFailure(decoder, ref fileOpen, "playback_submit_failed");
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
                        if (SeekToWithCapTelemetry(decoder, lastFrameAbsPts, "fmp4_reopen_before_segment_switch", cancellationToken))
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
                    if (!SeekToWithCapTelemetry(decoder, segSwitchTarget, "segment_switch", cancellationToken))
                    {
                        SetReopenFailure("segment_switch", "seek_failed", segSwitchTarget);
                        Logger.Log($"FLASHBACK_PLAYBACK_SEGMENT_SWITCH_SEEK_FAIL path='{nextFile}' offset_ms={(long)segSwitchTarget.TotalMilliseconds}");
                        RestoreLiveAfterSeekDisplayFailure(decoder, ref fileOpen, "segment_switch_seek_failed");
                        return false;
                    }
                    RestoreAudioCallback(decoder, audioGate);
                    ResetPlaybackPtsCadenceBaseline();
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
                    if (!SeekToWithCapTelemetry(decoder, resumeTarget, "fmp4_reopen", cancellationToken))
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

    private bool CheckNearLiveEdge(
        FlashbackDecoder decoder,
        TimeSpan absoluteFramePts,
        TimeSpan bufferPosition,
        ref bool fileOpen,
        bool requireFrameWarmup = true)
    {
        var absoluteLatestPts = _bufferManager.LatestPts;
        var gapFromLive = SaturatingSubtract(absoluteLatestPts, absoluteFramePts);
        var snapThreshold = requireFrameWarmup
            ? ResolveContinuousPlaybackNearLiveSnapThreshold()
            : RecoveryNearLiveSnapThreshold;
        if ((!requireFrameWarmup || Interlocked.Read(ref _playbackFrameCount) > 60) &&
            gapFromLive <= snapThreshold)
        {
            Interlocked.Increment(ref _playbackNearLiveSnaps);
            var gapMs = gapFromLive.TotalMilliseconds;
            Logger.Log($"FLASHBACK_PLAYBACK_NEAR_LIVE_SNAP pos_ms={(long)bufferPosition.TotalMilliseconds} framePts_ms={(long)absoluteFramePts.TotalMilliseconds} latestPts_ms={(long)absoluteLatestPts.TotalMilliseconds} gapFromLive_ms={gapMs:F0} threshold_ms={(long)snapThreshold.TotalMilliseconds} frameCount={_playbackFrameCount}");
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

    private TimeSpan ResolveContinuousPlaybackNearLiveSnapThreshold()
    {
        var fps = _playbackTargetFps;
        if (!double.IsFinite(fps) || fps <= 0)
        {
            fps = _bufferManager.EncodeFrameRate;
        }

        if (!double.IsFinite(fps) || fps <= 0)
        {
            fps = FallbackPlaybackFrameRate;
        }

        var framesThreshold = TimeSpan.FromSeconds(ContinuousPlaybackNearLiveSnapFrames / Math.Min(fps, MaxPlaybackFrameRate));
        return framesThreshold > ContinuousPlaybackNearLiveSnapMinimum
            ? framesThreshold
            : ContinuousPlaybackNearLiveSnapMinimum;
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

            ClearPendingAudioMasterFallback();

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

    private void ResetPlaybackPtsCadenceBaseline()
        => Interlocked.Exchange(ref _lastPlaybackCadencePtsTicks, 0);

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
    private static void SubmitFrame(
        IPreviewFrameSink previewSink,
        DecodedVideoFrame frame,
        long previewPresentId,
        bool countForPresentCadence)
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
                frame.Width, frame.Height, frame.IsHdr,
                new PreviewFrameTracking(
                    ArrivalTick: submitTick,
                    SourceSequenceNumber: -1,
                    PreviewPresentId: previewPresentId,
                    SchedulerSubmitTick: submitTick,
                    SourcePtsTicks: frame.Pts.Ticks,
                    CountForPresentCadence: countForPresentCadence));
        }
        else
        {
            previewSink.SubmitRawFrame(
                frame.Data, frame.DataLength,
                frame.Width, frame.Height, frame.IsHdr,
                new PreviewFrameTracking(
                    ArrivalTick: submitTick,
                    SourceSequenceNumber: -1,
                    PreviewPresentId: previewPresentId,
                    SchedulerSubmitTick: submitTick,
                    SourcePtsTicks: frame.Pts.Ticks,
                    CountForPresentCadence: countForPresentCadence));
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
    public long PlaybackSeekForwardDecodeCapHits => Interlocked.Read(ref _playbackSeekForwardDecodeCapHits);
    public bool LastPlaybackSeekHitForwardDecodeCap => Volatile.Read(ref _lastPlaybackSeekHitForwardDecodeCap) != 0;
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
    public string MaxCommandQueueLatencyCommand => Volatile.Read(ref _maxCommandQueueLatencyCommand);
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

using System;
using System.Threading;
using Sussudio.Services.Audio;
using Sussudio.Services.Contracts;
using Sussudio.Services.Preview;

namespace Sussudio.Services.Flashback;

internal sealed partial class FlashbackPlaybackController
{
    // --- Lifecycle ---

    private IPreviewFrameSink? _previewSink;
    private ILiveVideoSource? _videoCapture;
    private volatile WasapiAudioPlayback? _audioPlayback;
    private volatile WasapiAudioCapture? _audioCapture;
    private volatile bool _initialized;
    private volatile int _disposedFlag;

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

    // --- Dispose ---

    public void Dispose()
    {
        if (Interlocked.CompareExchange(ref _disposedFlag, 1, 0) != 0) return;

        Logger.Log($"FLASHBACK_PLAYBACK_DISPOSE_REQUEST state={_state} initialized={_initialized}");
        StopPlaybackThread(PlaybackThreadStopTimeout, "dispose");
        _initialized = false;
        Logger.Log("FLASHBACK_PLAYBACK_DISPOSED");
    }
}

using System;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.Services.Contracts;
using Sussudio.Services.Preview;

namespace Sussudio.Services.Flashback;

internal sealed partial class FlashbackPlaybackController
{
    // --- Preview detach timeout and deferred reattach recovery ---

    private int _previewDetachStopTimeoutActive;
    private int _deferredPreviewAttachApplyRetryScheduled;
    private IPreviewFrameSink? _pendingPreviewSinkAfterDetachTimeout;
    private ILiveVideoSource? _pendingVideoCaptureAfterDetachTimeout;

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

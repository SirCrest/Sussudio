using System;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.Services.Audio;
using Sussudio.Services.Flashback;
using Sussudio.Services.Gpu;
using Sussudio.Services.Preview;
using Sussudio.Services.Recording;

namespace Sussudio.Services.Capture;

// Resource holders owned by CaptureService. The service partials own transition
// policy and event projection; these classes own live capture/recording handles
// and their cleanup-adjacent state.
internal sealed class PreviewAudioGraphResources
{
    private bool _captureFaulted;
    private string? _captureFaultMessage;

    public WasapiAudioCapture? ProgramCapture;
    public WasapiAudioCapture? MicrophoneCapture;
    public WasapiAudioPlayback? Playback;
    public float PreviewVolume = 1.0f;
    public bool IsMonitoringMuted;

    public void SetPreviewVolume(float volume)
    {
        PreviewVolume = Math.Clamp(volume, 0f, 1f);
        if (!IsMonitoringMuted)
        {
            Playback?.SetVolume(PreviewVolume);
        }
    }

    public void SetMonitoringMuted(bool muted)
    {
        IsMonitoringMuted = muted;
        Playback?.SetVolume(muted ? 0f : PreviewVolume);
    }

    public string ClassifyCaptureFailureSource(object? sender)
    {
        return ReferenceEquals(sender, ProgramCapture)
            ? "program"
            : ReferenceEquals(sender, MicrophoneCapture)
                ? "microphone"
                : "unknown";
    }

    public void RecordCaptureFault(string source, Exception ex)
    {
        Volatile.Write(ref _captureFaulted, true);
        Volatile.Write(ref _captureFaultMessage, $"{source}: {ex.Message}");
    }

    public void ResetCaptureFault()
    {
        Volatile.Write(ref _captureFaulted, false);
        Volatile.Write(ref _captureFaultMessage, null);
    }

    public PreviewAudioCaptureFaultSnapshot ConsumeCaptureFault()
    {
        var faulted = Volatile.Read(ref _captureFaulted);
        var message = Volatile.Read(ref _captureFaultMessage);
        ResetCaptureFault();
        return new PreviewAudioCaptureFaultSnapshot(faulted, message);
    }

    public async Task StartPlaybackAsync(
        CancellationToken cancellationToken,
        FlashbackPlaybackController? flashbackPlaybackController)
    {
        var capture = ProgramCapture;
        if (capture == null)
        {
            return;
        }

        var playback = Playback;
        if (playback == null)
        {
            var newPlayback = new WasapiAudioPlayback();
            try
            {
                await newPlayback.InitializeAsync(cancellationToken).ConfigureAwait(false);
                newPlayback.SetVolume(0);
                newPlayback.Start();
                Playback = newPlayback;
                Logger.Log("WASAPI audio playback started.");
                newPlayback.SetVolume(IsMonitoringMuted ? 0f : PreviewVolume);
                playback = newPlayback;
            }
            catch (Exception ex)
            {
                Logger.Log($"WASAPI_PLAYBACK_START_FAIL type={ex.GetType().Name} msg='{ex.Message}'");
                if (ReferenceEquals(Playback, newPlayback))
                {
                    Playback = null;
                }

                StopPlaybackBestEffort(newPlayback, "start_fail");
                DisposePlaybackBestEffort(newPlayback);
                throw;
            }
        }

        try
        {
            capture.SetPlayback(playback);
        }
        catch (Exception ex)
        {
            Logger.Log($"WASAPI_PLAYBACK_ATTACH_FAIL type={ex.GetType().Name} msg='{ex.Message}'");
            StopPlayback(flashbackPlaybackController);
            throw;
        }

        // WASAPI starts after Flashback init; keep the playback controller's
        // audio references synchronized once playback becomes available.
        flashbackPlaybackController?.UpdateAudioComponents(playback, capture);
    }

    public void StopPlayback(FlashbackPlaybackController? flashbackPlaybackController)
    {
        flashbackPlaybackController?.UpdateAudioComponents(null, null);
        var playback = Playback;
        Playback = null;
        SafeClearCapturePlayback(ProgramCapture, "stop_playback");
        if (playback != null)
        {
            StopPlaybackBestEffort(playback, "stop_playback");
            DisposePlaybackBestEffort(playback);
        }
    }

    public void DetachCapture(
        WasapiAudioCapture? capture,
        EventHandler<AudioLevelEventArgs> audioLevelUpdated,
        EventHandler<Exception> captureFailed,
        FlashbackPlaybackController? flashbackPlaybackController)
    {
        if (capture == null)
        {
            StopPlayback(flashbackPlaybackController);
            return;
        }

        capture.AudioLevelUpdated -= audioLevelUpdated;
        capture.CaptureFailed -= captureFailed;
        SafeClearCapturePlayback(capture, "detach_capture");
        StopPlayback(flashbackPlaybackController);
    }

    private static void SafeClearCapturePlayback(WasapiAudioCapture? capture, string operation)
    {
        if (capture == null)
        {
            return;
        }

        try
        {
            capture.SetPlayback(null);
        }
        catch (Exception ex)
        {
            Logger.Log($"WASAPI audio playback detach warning op={operation}: {ex.Message}");
        }
    }

    private static void DisposePlaybackBestEffort(WasapiAudioPlayback playback)
    {
        try
        {
            playback.Dispose();
            Logger.Log("WASAPI audio playback disposed.");
        }
        catch (Exception ex)
        {
            Logger.Log($"WASAPI audio playback dispose warning: {ex.Message}");
        }
    }

    private static void StopPlaybackBestEffort(WasapiAudioPlayback playback, string operation)
    {
        try
        {
            playback.Stop();
        }
        catch (Exception ex)
        {
            Logger.Log($"WASAPI audio playback stop warning op={operation}: {ex.Message}");
        }
    }
}

internal sealed class CaptureRecordingBackendResources
{
    public LibAvRecordingSink? LibAvSink { get; set; }
    public IRecordingSink? Sink { get; set; }
    public RecordingContext? Context { get; set; }
    public CaptureSettings? SettingsSnapshot { get; set; }
    public Task? PendingLibAvDrainTask { get; set; }

    public bool HasActiveBackend => Sink != null || LibAvSink != null;

    public bool IsFlashbackBackend(FlashbackEncoderSink? flashbackSink)
        => ReferenceEquals(Sink, flashbackSink);

    public void InstallLibAv(
        LibAvRecordingSink libAvSink,
        IRecordingSink recordingSink,
        RecordingContext context,
        CaptureSettings settings)
    {
        LibAvSink = libAvSink;
        Sink = recordingSink;
        Context = context;
        SettingsSnapshot = settings;
        PendingLibAvDrainTask = null;
    }

    public void InstallFlashback(
        FlashbackEncoderSink flashbackSink,
        RecordingContext context,
        CaptureSettings settings)
    {
        Sink = flashbackSink;
        LibAvSink = null;
        Context = context;
        SettingsSnapshot = settings;
        PendingLibAvDrainTask = null;
    }

    public ActiveRecordingBackend DetachLibAvBackend()
    {
        var backend = new ActiveRecordingBackend(Sink, LibAvSink, Context);
        Sink = null;
        LibAvSink = null;
        PendingLibAvDrainTask = null;
        return backend;
    }

    public RecordingContext? DetachFlashbackBackend()
    {
        var context = Context;
        Sink = null;
        return context;
    }

    public void ClearActiveBackend()
    {
        Sink = null;
        LibAvSink = null;
        PendingLibAvDrainTask = null;
    }

    public void ClearContextAndSettings()
    {
        Context = null;
        SettingsSnapshot = null;
    }

    public void ClearAll()
    {
        ClearActiveBackend();
        ClearContextAndSettings();
    }

    public void ClearPendingLibAvDrainIfCompletedSuccessfully()
    {
        if (PendingLibAvDrainTask?.IsCompletedSuccessfully == true)
        {
            PendingLibAvDrainTask = null;
        }
    }

    public void ThrowIfPendingLibAvDrainBlocksReentry()
    {
        var pendingLibAvDrainTask = PendingLibAvDrainTask;
        if (pendingLibAvDrainTask == null)
        {
            return;
        }

        if (pendingLibAvDrainTask.IsCompletedSuccessfully)
        {
            PendingLibAvDrainTask = null;
            return;
        }

        if (pendingLibAvDrainTask.IsFaulted)
        {
            throw new InvalidOperationException(
                "Previous recording backend failed to finalize cleanly. Check the logs and retry.",
                pendingLibAvDrainTask.Exception?.GetBaseException());
        }

        if (pendingLibAvDrainTask.IsCanceled)
        {
            throw new InvalidOperationException("Previous recording backend cleanup was canceled. Check the logs and retry.");
        }

        throw new InvalidOperationException("Previous recording backend is still finalizing. Please wait a moment and try again.");
    }

    internal readonly record struct ActiveRecordingBackend(
        IRecordingSink? Sink,
        LibAvRecordingSink? LibAvSink,
        RecordingContext? Context);
}

internal sealed class CaptureVideoPipelineResources
{
    public UnifiedVideoCapture? Capture { get; set; }
    public IPreviewFrameSink? PreviewFrameSink { get; set; }
    public UnifiedVideoCapture.MjpegPipelineTimingMetrics LastMjpegPipelineTimingMetrics { get; private set; }
    public ParallelMjpegDecodePipeline.PipelineTimingMetrics? LastFullMjpegPipelineTimingMetrics { get; private set; }

    public int NegotiatedVideoWidth => Capture?.Width ?? 0;
    public int NegotiatedVideoHeight => Capture?.Height ?? 0;
    public double NegotiatedVideoFps => Capture?.Fps ?? 0;

    public void InstallCapture(UnifiedVideoCapture capture)
    {
        Capture = capture;
    }

    public UnifiedVideoCapture? TakeCapture()
    {
        var capture = Capture;
        Capture = null;
        return capture;
    }

    public void ClearCapture()
    {
        Capture = null;
    }

    public void SetPreviewFrameSink(IPreviewFrameSink? sink)
    {
        PreviewFrameSink = sink;
        Capture?.SetPreviewSink(sink);
    }

    public void CacheMjpegTimingMetrics(UnifiedVideoCapture? capture)
    {
        if (capture == null)
        {
            return;
        }

        var timingSnapshot = capture.GetMjpegPipelineTimingSnapshot();
        LastMjpegPipelineTimingMetrics = timingSnapshot.Summary;
        LastFullMjpegPipelineTimingMetrics = timingSnapshot.Details;
    }

    public void ResetCachedMjpegTimingMetrics()
    {
        LastMjpegPipelineTimingMetrics = default;
        LastFullMjpegPipelineTimingMetrics = null;
    }

    public ParallelMjpegDecodePipeline.PipelineTimingMetrics? GetMjpegPipelineTimingDetails()
    {
        return Capture?.GetFullMjpegPipelineTimingMetrics() ?? LastFullMjpegPipelineTimingMetrics;
    }

    public CaptureMjpegTimingSnapshot GetMjpegTimingSnapshot(UnifiedVideoCapture? capture)
    {
        var timingSnapshot = capture?.GetMjpegPipelineTimingSnapshot();
        return new CaptureMjpegTimingSnapshot(
            timingSnapshot?.Summary ?? LastMjpegPipelineTimingMetrics,
            timingSnapshot?.Details ?? LastFullMjpegPipelineTimingMetrics);
    }

    public Task ScheduleDeferredUnifiedVideoCaptureCleanup(
        Task sinkCompletionTask,
        UnifiedVideoCapture unifiedVideoCapture,
        string reason)
    {
        try
        {
            unifiedVideoCapture.SetPreviewSink(null);
        }
        catch (Exception ex)
        {
            Logger.Log($"UNIFIED_VIDEO_DEFERRED_PREVIEW_DETACH_WARN reason='{reason}' type={ex.GetType().Name} msg={ex.Message}");
        }

        return Task.Run(async () =>
        {
            Exception? cleanupFailure = null;
            try
            {
                await sinkCompletionTask.ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.Log($"UNIFIED_VIDEO_DEFERRED_WAIT_WARN reason='{reason}' type={ex.GetType().Name} msg={ex.Message}");
            }
            finally
            {
                try
                {
                    await unifiedVideoCapture.StopAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    cleanupFailure ??= ex;
                    Logger.Log($"UNIFIED_VIDEO_DEFERRED_STOP_WARN reason='{reason}' type={ex.GetType().Name} msg={ex.Message}");
                }

                try
                {
                    await unifiedVideoCapture.DisposeAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    cleanupFailure ??= ex;
                    Logger.Log($"UNIFIED_VIDEO_DEFERRED_DISPOSE_WARN reason='{reason}' type={ex.GetType().Name} msg={ex.Message}");
                }

                Logger.Log($"UNIFIED_VIDEO_DEFERRED_CLEANUP_END reason='{reason}'");

                if (cleanupFailure != null)
                {
                    throw new InvalidOperationException(
                        $"Deferred unified video cleanup failed for reason '{reason}'.",
                        cleanupFailure);
                }
            }
        });
    }

    internal readonly record struct CaptureMjpegTimingSnapshot(
        UnifiedVideoCapture.MjpegPipelineTimingMetrics Summary,
        ParallelMjpegDecodePipeline.PipelineTimingMetrics? Details);
}

internal readonly record struct PreviewAudioCaptureFaultSnapshot(
    bool Faulted,
    string? Message);

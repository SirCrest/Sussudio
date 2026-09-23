using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Channels;
using Sussudio.Models;
using Sussudio.Services.Preview;
using Sussudio.Services.Runtime;
using CommandKind = Sussudio.Services.Flashback.FlashbackPlaybackCommandMailbox.CommandKind;
using PlaybackCommand = Sussudio.Services.Flashback.FlashbackPlaybackCommandMailbox.Command;

namespace Sussudio.Services.Flashback;

internal sealed partial class FlashbackPlaybackController
{
    // --- Playback thread ---

    // Bounded forward-decode budget for pause-from-live frame accuracy (below):
    // one GOP at the current encode frame rate, clamped so a missing/zero
    // encode frame rate still gets a usable budget and a corrupt value can't
    // spin the decode loop indefinitely.
    private const int PauseFromLiveMinForwardDecodeFrames = 30;
    private const int PauseFromLiveMaxForwardDecodeFramesCap = 240;

    private int PauseFromLiveMaxForwardDecodeFrames =>
        Math.Clamp((int)Math.Ceiling(_bufferManager.EncodeFrameRate), PauseFromLiveMinForwardDecodeFrames, PauseFromLiveMaxForwardDecodeFramesCap);

    private static readonly TimeSpan PlaybackThreadStopTimeout = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan PreviewDetachThreadStopTimeout = TimeSpan.FromSeconds(10);

    private readonly object _playbackThreadSync = new();
    private readonly string _playbackMmcssTask = Environment.GetEnvironmentVariable("SUSSUDIO_FLASHBACK_PLAYBACK_MMCSS_TASK") ?? "Playback";
    private readonly int _playbackMmcssPriority = EnvironmentHelpers.GetIntFromEnv("SUSSUDIO_FLASHBACK_PLAYBACK_MMCSS_PRIORITY", 1, -2, 2);

    private Thread? _playbackThread;
    private int _playbackThreadStarted;
    private CancellationTokenSource? _playCts;

    [DllImport("winmm.dll", ExactSpelling = true)]
    private static extern uint timeBeginPeriod(uint uMilliseconds);

    [DllImport("winmm.dll", ExactSpelling = true)]
    private static extern uint timeEndPeriod(uint uMilliseconds);

    // Created once by the playback thread; never shared with another generation.
    // Existing command/exit paths retain decoder and queued-frame cleanup ordering.
    private sealed class PlaybackWorkerState
    {
        public FlashbackDecoder? Decoder;
        public readonly Stopwatch PacingStopwatch = new();
        public TimeSpan FrameDuration = TimeSpan.Zero;
        public bool IsPlaying;
        public bool IsScrubbing;
        public bool FileOpen;
        public TimeSpan FrozenValidStart = TimeSpan.Zero; // position origin when leaving Live
        public TimeSpan? PendingExactResumeTarget;
        public readonly Queue<DecodedVideoFrame> PrebufferedFrames = new();
    }

    private void PlaybackThreadEntry(
        CancellationTokenSource cts,
        FlashbackPlaybackCommandMailbox.Generation commandGeneration)
    {
        var worker = new PlaybackWorkerState();

        // Set 1ms timer resolution for accurate Thread.Sleep pacing.
        // Without this, Sleep(8) at 120fps sleeps ~15ms (default granularity) -> half-speed.
        timeBeginPeriod(1);
        using var mmcss = MmcssThreadRegistration.TryRegister(_playbackMmcssTask, _playbackMmcssPriority, message => Logger.Log(message));
        try
        {
            Logger.Log("FLASHBACK_PLAYBACK_THREAD_ENTER");
            while (true)
            {
                PlaybackCommand cmd;
                if (worker.IsPlaying)
                {
                    if (!_commandMailbox.TryReadForPlayback(commandGeneration, out cmd))
                    {
                        if (cts.IsCancellationRequested)
                        {
                            Logger.Log("FLASHBACK_PLAYBACK_THREAD_EXIT cancellation_requested");
                            RestoreLiveForPlaybackThreadExit(worker, "thread_cancelled");
                            return;
                        }

                        if (worker.Decoder is { IsOpen: true })
                        {
                            if (!PaceAndDecodeFrame(worker.Decoder, worker.PrebufferedFrames, commandGeneration.Reader, worker.PacingStopwatch, ref worker.FrameDuration, ref worker.FileOpen, worker.FrozenValidStart, cts.Token))
                            {
                                worker.IsPlaying = false;
                                ClearPrebufferedFrames(worker.PrebufferedFrames, "playback_stopped");
                            }
                        }
                        continue;
                    }
                }
                else
                {
                    if (!_commandMailbox.TryReadForPlayback(commandGeneration, out cmd))
                    {
                        var canRead = commandGeneration.Reader.WaitToReadAsync(cts.Token).AsTask().GetAwaiter().GetResult();
                        if (!canRead)
                        {
                            Logger.Log("FLASHBACK_PLAYBACK_THREAD_EXIT channel_closed");
                            worker.IsScrubbing = false;
                            RestoreLiveForPlaybackThreadExit(worker, "channel_closed");
                            return;
                        }

                        if (_disposedFlag != 0)
                        {
                            Logger.Log("FLASHBACK_PLAYBACK_THREAD_EXIT");
                            worker.IsScrubbing = false;
                            RestoreLiveForPlaybackThreadExit(worker, "thread_disposed");
                            return;
                        }
                        if (!_commandMailbox.TryReadForPlayback(commandGeneration, out cmd))
                        {
                            continue;
                        }
                    }
                }

                if (!ExecutePlaybackCommand(worker, ref cmd, commandGeneration.Reader, cts))
                {
                    return;
                }
            }
        }
        catch (OperationCanceledException)
        {
            Logger.Log("FLASHBACK_PLAYBACK_THREAD_CANCELLED");
            RestoreLiveForPlaybackThreadExit(worker, "thread_cancelled");
        }
        catch (Exception ex)
        {
            SetLastCommandFailure(ex.GetType().Name + ":" + ex.Message);
            Logger.Log($"FLASHBACK_PLAYBACK_FATAL type={ex.GetType().Name} error='{ex.Message}'");
            RestoreLiveForPlaybackThreadExit(worker, "thread_fatal");
        }
        finally
        {
            CompletePlaybackThreadExit(worker.PrebufferedFrames, cts, commandGeneration);
        }

        Logger.Log("FLASHBACK_PLAYBACK_THREAD_EXIT");
    }

    private bool EnsurePlaybackThread(CommandKind commandKind)
    {
        lock (_playbackThreadSync)
        {
            if (_disposedFlag != 0)
            {
                RecordCommandRejection(commandKind, "disposed", "disposed");
                return false;
            }
            if (Volatile.Read(ref _playbackThreadStarted) != 0)
            {
                if (_playbackThread is { IsAlive: true })
                {
                    return true;
                }

                Logger.Log("FLASHBACK_PLAYBACK_THREAD_RECOVER reason=stale_stopped");
                DrainAbandonedCommandsOnThreadExit(_commandMailbox.CurrentGeneration);
                DisposePlaybackCtsBestEffort(_playCts, "recover_stale_thread");
                _playCts = null;
                _playbackThread = null;
                _commandMailbox.ResetPendingAndClearSlots();
                Volatile.Write(ref _playbackThreadStarted, 0);
            }

            if (Interlocked.CompareExchange(ref _playbackThreadStarted, 1, 0) != 0)
                return true;

            // Recreate the bounded mailbox generation because stopping completes it.
            var commandGeneration = _commandMailbox.RenewGeneration();
            _playCts = new CancellationTokenSource();
            var threadCts = _playCts;
            _playbackThread = new Thread(() => PlaybackThreadEntry(threadCts, commandGeneration))
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
                RecordCommandRejection(
                    commandKind,
                    $"thread_start_failed:{ex.GetType().Name}:{ex.Message}",
                    $"thread_start_failed type={ex.GetType().Name}");
                return false;
            }
            Logger.Log("FLASHBACK_PLAYBACK_THREAD_START");
            return true;
        }
    }

    private bool StopPlaybackThread(TimeSpan timeout, string operation)
    {
        lock (_playbackThreadSync)
        {
            var stopStarted = Stopwatch.GetTimestamp();
            var thread = _playbackThread;
            var threadWasAlive = Volatile.Read(ref _playbackThreadStarted) != 0 && thread is { IsAlive: true };
            var activeKindAtRequest = FormatActiveCommandKind(Volatile.Read(ref _activeCommandKind));
            var activeElapsedMsAtRequest = GetActiveCommandElapsedMs(stopStarted);
            var commandGeneration = _commandMailbox.CurrentGeneration;
            if (Volatile.Read(ref _playbackThreadStarted) != 0 && thread is { IsAlive: true })
            {
                SendCommand(new PlaybackCommand { Kind = CommandKind.Stop });
            }

            _commandMailbox.Complete(commandGeneration);

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
                else if (!thread.Join(timeout))
                {
                    Logger.Log($"FLASHBACK_PLAYBACK_THREAD_JOIN_TIMEOUT op={operation} timeout_ms={timeout.TotalMilliseconds:0}");
                    SetLastCommandFailure($"thread_join_timeout:{operation}");
                    threadExited = false;
                }
            }

            var stopElapsedMs = Stopwatch.GetElapsedTime(stopStarted).TotalMilliseconds;
            Logger.Log(
                $"FLASHBACK_PLAYBACK_STOP_THREAD_COMPLETE op={operation} duration_ms={stopElapsedMs:0.###} " +
                $"thread_was_alive={threadWasAlive} thread_exited={threadExited} " +
                $"active_at_request={activeKindAtRequest} active_ms_at_request={activeElapsedMsAtRequest:0.###} " +
                $"pending={_commandMailbox.PendingCommands}");

            if (threadExited)
            {
                ApplyDeferredPreviewAttachAfterStopTimeout();
                DisposePlaybackCtsBestEffort(_playCts, "stop_thread");
                _playCts = null;
                _playbackThread = null;
                _commandMailbox.ResetPendingAndClearSlots();
                Volatile.Write(ref _playbackThreadStarted, 0);
            }

            return threadExited;
        }
    }

    private void CompletePlaybackThreadExit(
        Queue<DecodedVideoFrame> prebufferedFrames,
        CancellationTokenSource cts,
        FlashbackPlaybackCommandMailbox.Generation commandGeneration)
    {
        ClearPrebufferedFrames(prebufferedFrames, "thread_exit");
        timeEndPeriod(1);
        _commandMailbox.Complete(commandGeneration);
        DrainAbandonedCommandsOnThreadExit(commandGeneration);
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
        ApplyDeferredPreviewAttachAfterStopTimeout();
    }

    private void DrainAbandonedCommandsOnThreadExit(FlashbackPlaybackCommandMailbox.Generation commandGeneration)
    {
        var abandoned = _commandMailbox.DrainAbandoned(commandGeneration);

        if (abandoned > 0)
        {
            if (string.IsNullOrEmpty(Volatile.Read(ref _lastCommandFailure)))
            {
                SetLastCommandFailure($"abandoned_on_exit:{abandoned}");
            }
        }
    }

    private void RestoreLiveAfterNoFile(string operation)
    {
        ReleasePlaybackFrameForLive(operation);
        RestoreLiveAudio();
        SafeResumePreviewSubmission(operation);
        SafeResumeRendering(operation);
        SetState(FlashbackPlaybackState.Live, operation);
    }

    private void RestoreLiveForPlaybackThreadExit(
        PlaybackWorkerState worker,
        string operation)
    {
        ClearPrebufferedFrames(worker.PrebufferedFrames, operation);
        CleanupDecoder(ref worker.Decoder, ref worker.FileOpen);
        Interlocked.Exchange(ref _lastAudioPtsTicks, 0);
        Interlocked.Exchange(ref _lastVideoPtsTicks, 0);
        RestoreLiveAudio();
        SafeResumePreviewSubmission(operation);
        SetState(FlashbackPlaybackState.Live, operation);
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

    // --- Playback-thread command dispatch and compact command handlers ---

    private bool ExecutePlaybackCommand(
        PlaybackWorkerState worker,
        ref PlaybackCommand cmd,
        ChannelReader<PlaybackCommand> commandChannel,
        CancellationTokenSource cts)
    {
        var commandStarted = Stopwatch.GetTimestamp();
        Volatile.Write(ref _activeCommandKind, (int)cmd.Kind);
        Volatile.Write(ref _activeCommandStartedTimestamp, commandStarted);
        try
        {
            switch (cmd.Kind)
            {
                case CommandKind.Stop:
                    HandleStopCommand(worker);
                    return false;

                case CommandKind.Seek:
                    HandleSeekCommand(worker, ref cmd, commandChannel, cts);
                    break;

                case CommandKind.BeginScrub:
                    HandleBeginScrubCommand(worker, ref cmd, cts);
                    break;

                case CommandKind.UpdateScrub:
                    HandleUpdateScrubCommand(worker, ref cmd, commandChannel, cts);
                    break;

                case CommandKind.EndScrub:
                    HandleEndScrubCommand(worker, cmd, commandChannel, cts);
                    break;

                case CommandKind.Play:
                    HandlePlayCommand(worker, commandChannel, cts);
                    break;

                case CommandKind.Pause:
                    HandlePauseCommand(worker, commandChannel, cts);
                    break;

                case CommandKind.GoLive:
                    HandleGoLiveCommand(worker);
                    break;

                case CommandKind.Nudge:
                    HandleNudgeCommand(worker, cmd, cts);
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

        return true;
    }

    private void HandleGoLiveCommand(PlaybackWorkerState worker)
    {
        worker.IsPlaying = false;
        worker.IsScrubbing = false;
        worker.PendingExactResumeTarget = null;
        RestoreLiveForPlaybackThreadExit(worker, "go_live");
        Logger.Log("FLASHBACK_PLAYBACK_GO_LIVE");
        return;
    }

    private void HandleStopCommand(PlaybackWorkerState worker)
    {
        worker.IsPlaying = false;
        worker.IsScrubbing = false;
        worker.PendingExactResumeTarget = null;
        RestoreLiveForPlaybackThreadExit(worker, "thread_stop");
        Logger.Log("FLASHBACK_PLAYBACK_THREAD_EXIT");
    }

    private void HandlePlayCommand(
        PlaybackWorkerState worker,
        ChannelReader<PlaybackCommand> commandChannel,
        CancellationTokenSource cts)
    {
        if (worker.IsPlaying)
        {
            MarkCommandNoOp(CommandKind.Play, "already_playing");
            return;
        }
        worker.IsScrubbing = false;
        worker.IsPlaying = true;
        SafeSuppressPreviewSubmission("play");
        SuppressLiveAudio();
        SafePauseRendering("play");
        ResetPlaybackMetrics();
        worker.PacingStopwatch.Restart();

        if (State == FlashbackPlaybackState.Live)
            worker.FrozenValidStart = _bufferManager.ValidStartPts;
        worker.Decoder ??= CreateDecoder();
        var prevFile = _currentOpenFilePath;
        var pendingPlayTarget = ClampPlaybackTargetToMinimumLiveLead(
            worker.PendingExactResumeTarget ?? SaturatingAdd(PlaybackPosition, worker.FrozenValidStart),
            worker.FrozenValidStart,
            "play");
        EnsureFileOpen(worker.Decoder, ref worker.FileOpen, pendingPlayTarget);
        if (!IsDecoderFileReady(worker.Decoder, worker.FileOpen))
        {
            Logger.Log("FLASHBACK_PLAYBACK_PLAY_NO_FILE — restoring live");
            ClearPrebufferedFrames(worker.PrebufferedFrames, "play_no_file");
            SetNoFileFailure(CommandKind.Play, PlaybackPosition);
            worker.IsPlaying = false;
            worker.PendingExactResumeTarget = null;
            RestoreLiveAfterNoFile("play_no_file");
            return;
        }
        var requireExactResumeSeek = worker.PendingExactResumeTarget.HasValue;
        var seekTarget = pendingPlayTarget;
        var resumeWithoutSeek = State == FlashbackPlaybackState.Paused &&
            IsSamePlaybackPath(prevFile, _currentOpenFilePath) &&
            !requireExactResumeSeek;
        if (resumeWithoutSeek)
        {
            Logger.Log($"FLASHBACK_PLAYBACK_RESUME_NO_SEEK pos_ms={(long)PlaybackPosition.TotalMilliseconds}");
        }
        else
        {
            ClearPrebufferedFrames(worker.PrebufferedFrames, "play_seek");
            worker.Decoder.AudioChunkCallback = null;
            if (requireExactResumeSeek)
            {
                Logger.Log($"FLASHBACK_PLAYBACK_RESUME_EXACT_SEEK target_ms={(long)seekTarget.TotalMilliseconds} display_pos_ms={(long)PlaybackPosition.TotalMilliseconds}");
            }
            if (!TrySeekWithActiveFmp4Reopen(worker.Decoder, ref worker.FileOpen, seekTarget, "play", cts.Token))
            {
                worker.IsPlaying = false;
                worker.PendingExactResumeTarget = null;
                RestoreLiveAfterSeekDisplayFailure(worker.Decoder, ref worker.FileOpen, "play_seek_failed");
                return;
            }
            if (TrySnapLiveForSoftwarePlaybackBudget(worker.Decoder, ref worker.FileOpen, "play"))
            {
                worker.IsPlaying = false;
                worker.PendingExactResumeTarget = null;
                return;
            }
        }
        worker.PendingExactResumeTarget = null;
        worker.FrameDuration = ResolveFrameDuration(worker.Decoder);
        RestoreAudioCallback(worker.Decoder, seekTarget.Ticks);
        SafeFlushPlayback("play");
        // Retained pictures precede the decoder's current position. Consume them
        // before priming can decode ahead, invalidate borrowed data, or rewind.
        if (!resumeWithoutSeek || worker.PrebufferedFrames.Count == 0)
        {
            PrimePlaybackAudioBuffer(worker.Decoder, worker.PrebufferedFrames, commandChannel, ref worker.FileOpen, seekTarget, "play", cts.Token);
        }
        SafeResumePlaybackRendering("play");
        worker.PacingStopwatch.Restart();

        SetState(FlashbackPlaybackState.Playing, "user");
        Logger.Log($"FLASHBACK_PLAYBACK_PLAY pos_ms={(long)PlaybackPosition.TotalMilliseconds}");
        return;
    }

    private void HandlePauseCommand(
        PlaybackWorkerState worker,
        ChannelReader<PlaybackCommand> commandChannel,
        CancellationTokenSource cts)
    {
        if (worker.IsPlaying)
        {
            worker.IsPlaying = false;
            SafePauseRendering("pause");
            worker.PacingStopwatch.Stop();
            SetState(FlashbackPlaybackState.Paused, "user");
            Logger.Log($"FLASHBACK_PLAYBACK_PAUSE pos_ms={(long)PlaybackPosition.TotalMilliseconds}");
        }
        else if (State == FlashbackPlaybackState.Live)
        {
            SafeSuppressPreviewSubmission("pause_from_live");
            SuppressLiveAudio();
            SafePauseRendering("pause_from_live");

            worker.FrozenValidStart = _bufferManager.ValidStartPts;
            var pauseTarget = ResolvePauseFromLiveTarget(worker.FrozenValidStart);
            var pausePos = ClampPosition(SaturatingSubtract(pauseTarget, worker.FrozenValidStart), worker.FrozenValidStart);
            if (_commandMailbox.ShouldYieldPauseFromLiveToQueuedSeekOrPlay(commandChannel))
            {
                PlaybackPosition = pausePos;
                worker.PendingExactResumeTarget = SaturatingAdd(pausePos, worker.FrozenValidStart);
                SetState(FlashbackPlaybackState.Paused, "user");
                Logger.Log($"FLASHBACK_PLAYBACK_PAUSE_FROM_LIVE_DEFER_DISPLAY pos_ms={(long)pausePos.TotalMilliseconds}");
                return;
            }
            ClearPrebufferedFrames(worker.PrebufferedFrames, "pause_from_live");
            worker.Decoder ??= CreateDecoder();
            EnsureFileOpen(worker.Decoder, ref worker.FileOpen, SaturatingAdd(pausePos, worker.FrozenValidStart));
            cts.Token.ThrowIfCancellationRequested();
            if (!IsDecoderFileReady(worker.Decoder, worker.FileOpen))
            {
                worker.PendingExactResumeTarget = null;
                SetNoFileFailure(CommandKind.Pause, pausePos);
                RestoreLiveAfterNoFile("pause_from_live_no_file");
                Logger.Log($"FLASHBACK_PLAYBACK_PAUSE_FROM_LIVE_NO_FILE pos_ms={(long)pausePos.TotalMilliseconds}");
                return;
            }

            if (!SeekAndDisplayKeyframe(worker.Decoder, ref worker.FileOpen, pausePos, worker.FrozenValidStart, CommandKind.Pause, cts.Token))
            {
                worker.PendingExactResumeTarget = null;
                RestoreLiveAfterSeekDisplayFailure(worker.Decoder, ref worker.FileOpen, "pause_from_live_display_failed");
                return;
            }

            // The keyframe just displayed can be up to one GOP behind pauseTarget
            // (the live-derived pause point). Forward-decode toward it so the user
            // sees roughly the frame they paused on instead of a stale keyframe.
            // On decode failure this leaves the keyframe display in place and does
            // not snap to live -- scrub-settle-grade refinement is out of scope here.
            DecodeForwardToPauseTarget(worker.Decoder, commandChannel, pauseTarget, worker.FrozenValidStart, PauseFromLiveMaxForwardDecodeFrames, cts.Token);

            worker.PendingExactResumeTarget = SaturatingAdd(PlaybackPosition, worker.FrozenValidStart);

            SetState(FlashbackPlaybackState.Paused, "user");
            Logger.Log($"FLASHBACK_PLAYBACK_PAUSE_FROM_LIVE pos_ms={(long)PlaybackPosition.TotalMilliseconds} target_ms={(long)pauseTarget.TotalMilliseconds} frozen_frame=true");
        }
        return;
    }

    private void HandleSeekCommand(
        PlaybackWorkerState worker,
        ref PlaybackCommand cmd,
        ChannelReader<PlaybackCommand> commandChannel,
        CancellationTokenSource cts)
    {
        cmd = _commandMailbox.ResolveLatestPositionAndReleaseCoalescingSlot(cmd);
        while (commandChannel.TryPeek(out var newerSeek) &&
               newerSeek.Kind == CommandKind.Seek)
        {
            if (!commandChannel.TryRead(out newerSeek))
            {
                break;
            }

            _commandMailbox.TrackCommandDequeued(newerSeek);
            newerSeek = _commandMailbox.ResolveLatestPositionAndReleaseCoalescingSlot(newerSeek);
            cmd = newerSeek;
        }

        ClearPrebufferedFrames(worker.PrebufferedFrames, "seek");
        _wasPlayingBeforeScrub = worker.IsPlaying || State == FlashbackPlaybackState.Live;
        worker.IsPlaying = false;
        worker.IsScrubbing = false;
        worker.FrozenValidStart = _bufferManager.ValidStartPts;
        SafeSuppressPreviewSubmission("seek");
        SuppressLiveAudio();
        SafePauseRendering("seek");

        cmd = cmd with { Position = ClampPosition(cmd.Position, worker.FrozenValidStart) };
        var seekResumeTarget = ClampPlaybackTargetToMinimumLiveLead(
            SaturatingAdd(cmd.Position, worker.FrozenValidStart),
            worker.FrozenValidStart,
            "seek");
        cmd = cmd with { Position = ClampPosition(SaturatingSubtract(seekResumeTarget, worker.FrozenValidStart), worker.FrozenValidStart) };
        if (_commandMailbox.ShouldYieldSeekToQueuedPlay(commandChannel))
        {
            PlaybackPosition = cmd.Position;
            worker.PendingExactResumeTarget = seekResumeTarget;
            MarkCommandNoOp(CommandKind.Seek, "superseded_by_play", cmd.Position);
            SetState(FlashbackPlaybackState.Paused, "user");
            return;
        }
        worker.Decoder ??= CreateDecoder();
        EnsureFileOpen(worker.Decoder, ref worker.FileOpen, seekResumeTarget);
        cts.Token.ThrowIfCancellationRequested();
        if (!IsDecoderFileReady(worker.Decoder, worker.FileOpen))
        {
            worker.PendingExactResumeTarget = null;
            SetNoFileFailure(CommandKind.Seek, cmd.Position);
            Logger.Log("FLASHBACK_PLAYBACK_SEEK_NO_FILE - restoring live");
            RestoreLiveAfterNoFile("seek_no_file");
            return;
        }

        if (!SeekAndDisplayKeyframe(worker.Decoder, ref worker.FileOpen, cmd.Position, worker.FrozenValidStart, CommandKind.Seek, cts.Token))
        {
            worker.IsPlaying = false;
            worker.IsScrubbing = false;
            worker.PendingExactResumeTarget = null;
            RestoreLiveAfterSeekDisplayFailure(worker.Decoder, ref worker.FileOpen, "seek_display_failed");
            return;
        }
        worker.IsPlaying = _wasPlayingBeforeScrub;
        if (worker.IsPlaying)
        {
            worker.PendingExactResumeTarget = null;
            ResetPlaybackMetrics();
            worker.PacingStopwatch.Restart();
            var coalescedSeekTarget = seekResumeTarget;
            worker.Decoder.AudioChunkCallback = null;
            if (!TrySeekWithActiveFmp4Reopen(worker.Decoder, ref worker.FileOpen, coalescedSeekTarget, "seek_resume", cts.Token))
            {
                worker.IsPlaying = false;
                worker.PendingExactResumeTarget = null;
                RestoreLiveAfterSeekDisplayFailure(worker.Decoder, ref worker.FileOpen, "seek_resume_failed");
                return;
            }
            if (TrySnapLiveForSoftwarePlaybackBudget(worker.Decoder, ref worker.FileOpen, "seek_resume"))
            {
                worker.IsPlaying = false;
                return;
            }
            worker.FrameDuration = ResolveFrameDuration(worker.Decoder);
            RestoreAudioCallback(worker.Decoder, coalescedSeekTarget.Ticks);
            SafeFlushPlayback("seek_resume");
            PrimePlaybackAudioBuffer(worker.Decoder, worker.PrebufferedFrames, commandChannel, ref worker.FileOpen, coalescedSeekTarget, "seek_resume", cts.Token);
            SafeResumePlaybackRendering("seek_resume");
            worker.PacingStopwatch.Restart();
        }
        else
        {
            worker.PendingExactResumeTarget = seekResumeTarget;
        }
        SetState(worker.IsPlaying ? FlashbackPlaybackState.Playing : FlashbackPlaybackState.Paused, "user");
        Logger.Log($"FLASHBACK_PLAYBACK_SEEK pos_ms={(long)PlaybackPosition.TotalMilliseconds} resumePlay={worker.IsPlaying}");
        return;
    }

    private void HandleBeginScrubCommand(
        PlaybackWorkerState worker,
        ref PlaybackCommand cmd,
        CancellationTokenSource cts)
    {
        ClearPrebufferedFrames(worker.PrebufferedFrames, "begin_scrub");
        worker.PendingExactResumeTarget = null;
        // Only capture the resume-state on first entry into Scrubbing.
        // A second BeginScrub arriving while we're already scrubbing
        // (UI re-press race, MCP automation racing pointer-pressed)
        // would otherwise sample isPlaying=false (set by the prior
        // BeginScrub) and State=Scrubbing, clobbering the original
        // capture and causing EndScrub to land in Paused instead of
        // resuming Playing/Live.
        if (!worker.IsScrubbing)
        {
            _wasPlayingBeforeScrub = worker.IsPlaying || State == FlashbackPlaybackState.Live;
            worker.FrozenValidStart = _bufferManager.ValidStartPts;
        }
        else
        {
            var proposedValidStart = _bufferManager.ValidStartPts;
            Logger.Log($"FLASHBACK_PLAYBACK_BEGIN_SCRUB_DUPLICATE existing_frozen_ms={worker.FrozenValidStart.TotalMilliseconds:F0} new_proposed_ms={proposedValidStart.TotalMilliseconds:F0}");
        }
        worker.IsPlaying = false;
        worker.IsScrubbing = true;
        SafeSuppressPreviewSubmission("begin_scrub");
        SuppressLiveAudio();
        SafePauseRendering("begin_scrub");
        SetState(FlashbackPlaybackState.Scrubbing, "user");

        cmd = cmd with { Position = ClampPosition(cmd.Position, worker.FrozenValidStart) };
        worker.Decoder ??= CreateDecoder();
        EnsureFileOpen(worker.Decoder, ref worker.FileOpen, SaturatingAdd(cmd.Position, worker.FrozenValidStart));
        cts.Token.ThrowIfCancellationRequested();
        if (!IsDecoderFileReady(worker.Decoder, worker.FileOpen))
        {
            Logger.Log("FLASHBACK_PLAYBACK_SCRUB_NO_FILE — restoring live");
            worker.IsScrubbing = false;
            worker.PendingExactResumeTarget = null;
            SetNoFileFailure(CommandKind.BeginScrub, cmd.Position);
            RestoreLiveAfterNoFile("scrub_no_file");
            return;
        }
        if (!SeekAndDisplayKeyframe(worker.Decoder, ref worker.FileOpen, cmd.Position, worker.FrozenValidStart, CommandKind.BeginScrub, cts.Token))
        {
            worker.IsScrubbing = false;
            worker.PendingExactResumeTarget = null;
            RestoreLiveAfterSeekDisplayFailure(worker.Decoder, ref worker.FileOpen, "begin_scrub_display_failed");
        }
        return;
    }

    private void HandleUpdateScrubCommand(
        PlaybackWorkerState worker,
        ref PlaybackCommand cmd,
        ChannelReader<PlaybackCommand> commandChannel,
        CancellationTokenSource cts)
    {
        cmd = _commandMailbox.ResolveLatestPositionAndReleaseCoalescingSlot(cmd);
        if (!worker.IsScrubbing)
        {
            MarkCommandNoOp(CommandKind.UpdateScrub, "not_scrubbing", cmd.Position);
            return;
        }
        ClearPrebufferedFrames(worker.PrebufferedFrames, "update_scrub");
        worker.PendingExactResumeTarget = null;
        // Drain stale UpdateScrub commands only. Leave control commands queued
        // so their latency/accounting stays tied to the original command.
        while (commandChannel.TryPeek(out var newer) &&
               newer.Kind == CommandKind.UpdateScrub)
        {
            if (!commandChannel.TryRead(out newer))
            {
                break;
            }

            _commandMailbox.TrackCommandDequeued(newer);
            newer = _commandMailbox.ResolveLatestPositionAndReleaseCoalescingSlot(newer);
            cmd = newer;
        }
        cmd = cmd with { Position = ClampPosition(cmd.Position, worker.FrozenValidStart) };
        if (_commandMailbox.ShouldYieldScrubUpdateToQueuedControl(commandChannel))
        {
            PlaybackPosition = cmd.Position;
            MarkCommandNoOp(CommandKind.UpdateScrub, "superseded_by_control", cmd.Position);
            return;
        }
        worker.Decoder ??= CreateDecoder();
        EnsureFileOpen(worker.Decoder, ref worker.FileOpen, SaturatingAdd(cmd.Position, worker.FrozenValidStart));
        cts.Token.ThrowIfCancellationRequested();
        if (!IsDecoderFileReady(worker.Decoder, worker.FileOpen))
        {
            SetNoFileFailure(CommandKind.UpdateScrub, cmd.Position);
            worker.IsScrubbing = false;
            worker.PendingExactResumeTarget = null;
            RestoreLiveAfterNoFile("scrub_update_no_file");
            Logger.Log($"FLASHBACK_PLAYBACK_SCRUB_UPDATE_NO_FILE pos_ms={(long)cmd.Position.TotalMilliseconds}");
            return;
        }
        if (!SeekAndDisplayKeyframe(worker.Decoder, ref worker.FileOpen, cmd.Position, worker.FrozenValidStart, CommandKind.UpdateScrub, cts.Token))
        {
            worker.IsScrubbing = false;
            worker.PendingExactResumeTarget = null;
            RestoreLiveAfterSeekDisplayFailure(worker.Decoder, ref worker.FileOpen, "scrub_update_display_failed");
        }
        return;
    }

    private void HandleEndScrubCommand(
        PlaybackWorkerState worker,
        PlaybackCommand cmd,
        ChannelReader<PlaybackCommand> commandChannel,
        CancellationTokenSource cts)
    {
        if (!worker.IsScrubbing)
        {
            MarkCommandNoOp(CommandKind.EndScrub, "not_scrubbing", cmd.Position);
            return;
        }
        ClearPrebufferedFrames(worker.PrebufferedFrames, "end_scrub");
        var requestedEndScrubPosition = ClampPosition(cmd.Position, worker.FrozenValidStart);
        var endScrubTarget = ClampPlaybackTargetToMinimumLiveLead(
            SaturatingAdd(requestedEndScrubPosition, worker.FrozenValidStart),
            worker.FrozenValidStart,
            "end_scrub");
        var endScrubPosition = ClampPosition(SaturatingSubtract(endScrubTarget, worker.FrozenValidStart), worker.FrozenValidStart);
        PlaybackPosition = endScrubPosition;
        worker.IsScrubbing = false;
        worker.IsPlaying = _wasPlayingBeforeScrub;
        if (worker.IsPlaying)
        {
            worker.PendingExactResumeTarget = null;
            ResetPlaybackMetrics();
            worker.PacingStopwatch.Restart();

            if (worker.Decoder is { IsOpen: true })
            {
                worker.Decoder.AudioChunkCallback = null;
                if (!TrySeekWithActiveFmp4Reopen(worker.Decoder, ref worker.FileOpen, endScrubTarget, "end_scrub", cts.Token))
                {
                    worker.IsPlaying = false;
                    worker.PendingExactResumeTarget = null;
                    RestoreLiveAfterSeekDisplayFailure(worker.Decoder, ref worker.FileOpen, "end_scrub_seek_failed");
                    return;
                }
                if (TrySnapLiveForSoftwarePlaybackBudget(worker.Decoder, ref worker.FileOpen, "end_scrub"))
                {
                    worker.IsPlaying = false;
                    return;
                }
                worker.FrameDuration = ResolveFrameDuration(worker.Decoder);
            }
            if (worker.Decoder != null)
            {
                RestoreAudioCallback(worker.Decoder, endScrubTarget.Ticks);
                SafeFlushPlayback("end_scrub_resume");
                PrimePlaybackAudioBuffer(worker.Decoder, worker.PrebufferedFrames, commandChannel, ref worker.FileOpen, endScrubTarget, "end_scrub_resume", cts.Token);
                SafeResumePlaybackRendering("end_scrub_resume");
            }
            worker.PacingStopwatch.Restart();
        }
        else
        {
            worker.PendingExactResumeTarget = endScrubTarget;
        }
        SetState(worker.IsPlaying ? FlashbackPlaybackState.Playing : FlashbackPlaybackState.Paused, "user");
        var endScrubBufDur = _bufferManager.BufferedDuration;
        Logger.Log($"FLASHBACK_ENDSCRUB pos_ms={(long)PlaybackPosition.TotalMilliseconds} bufferDur_ms={(long)endScrubBufDur.TotalMilliseconds} gapFromLive_ms={SaturatingSubtract(endScrubBufDur, PlaybackPosition).TotalMilliseconds:F0} resumePlay={worker.IsPlaying}");
        return;
    }

    private void HandleNudgeCommand(
        PlaybackWorkerState worker,
        PlaybackCommand cmd,
        CancellationTokenSource cts)
    {
        worker.PendingExactResumeTarget = null;
        var nudgedPos = SaturatingAdd(PlaybackPosition, cmd.Delta);
        nudgedPos = ClampPosition(nudgedPos, worker.FrozenValidStart);
        worker.Decoder ??= CreateDecoder();
        var previousFile = _currentOpenFilePath;
        EnsureFileOpen(worker.Decoder, ref worker.FileOpen, SaturatingAdd(nudgedPos, worker.FrozenValidStart));
        cts.Token.ThrowIfCancellationRequested();
        if (!IsDecoderFileReady(worker.Decoder, worker.FileOpen))
        {
            ClearPrebufferedFrames(worker.PrebufferedFrames, "nudge_no_file");
            SetNoFileFailure(CommandKind.Nudge, nudgedPos);
            PlaybackPosition = nudgedPos;
            worker.IsPlaying = false;
            worker.IsScrubbing = false;
            RestoreLiveAfterNoFile("nudge_no_file");
            Logger.Log($"FLASHBACK_PLAYBACK_NUDGE_NO_FILE pos_ms={(long)nudgedPos.TotalMilliseconds}");
            return;
        }

        if (!IsSamePlaybackPath(previousFile, _currentOpenFilePath))
        {
            ClearPrebufferedFrames(worker.PrebufferedFrames, "nudge_source_changed");
        }
        if (cmd.Delta.Ticks > 0)
        {
            var got = TryReadNextPlaybackFrame(worker.Decoder, worker.PrebufferedFrames, out var nudgeFrame, cts.Token);
            if (got)
            {
                if (!TrySubmitAndHoldFrame(nudgeFrame, "nudge"))
                {
                    return;
                }
                var actualPos = SaturatingSubtract(nudgeFrame.Pts, worker.FrozenValidStart);
                if (actualPos < TimeSpan.Zero) actualPos = TimeSpan.Zero;
                PlaybackPosition = actualPos;
                return;
            }
        }
        ClearPrebufferedFrames(worker.PrebufferedFrames, "nudge_seek");
        if (!SeekAndDisplayKeyframe(worker.Decoder, ref worker.FileOpen, nudgedPos, worker.FrozenValidStart, CommandKind.Nudge, cts.Token))
        {
            worker.IsPlaying = false;
            worker.IsScrubbing = false;
            RestoreLiveAfterSeekDisplayFailure(worker.Decoder, ref worker.FileOpen, "nudge_display_failed");
        }
        return;
    }
}

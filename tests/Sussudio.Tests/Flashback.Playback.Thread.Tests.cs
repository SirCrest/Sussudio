using System.Threading.Tasks;

static partial class Program
{
    private static string ReadFlashbackPlaybackControllerPlaybackSource()
        => ReadFlashbackPlaybackControllerSource();

    private static Task FlashbackPlaybackController_PlaybackThreadExit_RearmsWorkerStart()
    {
        var sourceText = ReadFlashbackPlaybackControllerPlaybackSource();
        var threadLoopText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.ThreadLoop.cs")
            .Replace("\r\n", "\n");
        var threadSeekCommandsText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.ThreadSeekCommands.cs")
            .Replace("\r\n", "\n");
        var threadSeekScrubCommandsText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.ThreadSeekScrubCommands.cs")
            .Replace("\r\n", "\n");
        var threadPlayCommandText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.ThreadPlayCommand.cs")
            .Replace("\r\n", "\n");
        var threadCommandsText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.ThreadCommands.cs")
            .Replace("\r\n", "\n");
        var commandModelsText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.CommandModels.cs")
            .Replace("\r\n", "\n");
        var rootText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.cs")
            .Replace("\r\n", "\n");

        AssertContains(threadLoopText, "private void PlaybackThreadEntry(CancellationTokenSource cts, Channel<PlaybackCommand> commandChannel)");
        AssertContains(commandModelsText, "private enum CommandKind");
        AssertContains(commandModelsText, "private readonly struct PlaybackCommand");
        AssertContains(commandModelsText, "public SeekIntentSlot? SeekSlot { get; init; }");
        AssertContains(commandModelsText, "public ScrubUpdateIntentSlot? ScrubUpdateSlot { get; init; }");
        AssertDoesNotContain(rootText, "private enum CommandKind");
        AssertDoesNotContain(rootText, "private readonly struct PlaybackCommand");
        AssertContains(threadLoopText, "[DllImport(\"winmm.dll\", ExactSpelling = true)]");
        AssertContains(threadLoopText, "private static extern uint timeBeginPeriod(uint uMilliseconds);");
        AssertContains(threadLoopText, "private static extern uint timeEndPeriod(uint uMilliseconds);");
        AssertContains(threadLoopText, "Logger.Log(\"FLASHBACK_PLAYBACK_THREAD_ENTER\");");
        AssertContains(threadSeekCommandsText, "private void HandleSeekCommand(");
        AssertContains(threadSeekScrubCommandsText, "private void HandleBeginScrubCommand(");
        AssertContains(threadSeekScrubCommandsText, "private void HandleUpdateScrubCommand(");
        AssertContains(threadSeekScrubCommandsText, "private void HandleEndScrubCommand(");
        AssertDoesNotContain(threadSeekScrubCommandsText, "private void HandleSeekCommand(");
        AssertDoesNotContain(threadCommandsText, "private void HandleSeekCommand(");
        AssertDoesNotContain(threadCommandsText, "private void HandleBeginScrubCommand(");
        AssertDoesNotContain(threadCommandsText, "private void HandleUpdateScrubCommand(");
        AssertDoesNotContain(threadCommandsText, "private void HandleEndScrubCommand(");
        AssertContains(threadPlayCommandText, "private void HandlePlayCommand(");
        AssertContains(threadPlayCommandText, "PrimePlaybackAudioBuffer(decoder, prebufferedFrames, ref fileOpen, seekTarget, \"play\", cts.Token);");
        AssertDoesNotContain(threadCommandsText, "private void HandlePlayCommand(");
        AssertContains(threadCommandsText, "private void HandlePauseCommand(");
        AssertContains(threadCommandsText, "private void HandleGoLiveCommand(");
        AssertContains(threadCommandsText, "private void HandleNudgeCommand(");
        AssertContains(threadSeekCommandsText, "cmd = ResolveSeekCommandPosition(cmd);");
        AssertContains(threadSeekScrubCommandsText, "SafeSuppressPreviewSubmission(\"begin_scrub\")");
        AssertContains(threadCommandsText, "Logger.Log(\"FLASHBACK_PLAYBACK_GO_LIVE\");");
        AssertContains(threadLoopText, "HandleSeekCommand(ref cmd, commandChannel, cts, ref decoder, ref fileOpen, ref isPlaying, ref isScrubbing, ref frozenValidStart, ref pendingExactResumeTarget, ref frameDuration, prebufferedFrames, pacingStopwatch);");
        AssertContains(threadLoopText, "HandleGoLiveCommand(ref decoder, ref fileOpen, ref isPlaying, ref isScrubbing, ref pendingExactResumeTarget);");
        AssertDoesNotContain(threadLoopText, "cmd = ResolveSeekCommandPosition(cmd);");
        AssertDoesNotContain(threadLoopText, "SafeSuppressPreviewSubmission(\"begin_scrub\")");
        AssertDoesNotContain(threadLoopText, "Logger.Log(\"FLASHBACK_PLAYBACK_GO_LIVE\");");
        AssertContains(sourceText, "if (Volatile.Read(ref _playbackThreadStarted) != 0 && thread is { IsAlive: true })\n            {\n                SendCommand(new PlaybackCommand { Kind = CommandKind.Stop });\n            }");
        AssertContains(sourceText, "case CommandKind.Stop:\n                            isPlaying = false;\n                            isScrubbing = false;\n                            pendingExactResumeTarget = null;\n                            RestoreLiveForPlaybackThreadExit(ref decoder, ref fileOpen, \"thread_stop\");");
        AssertContains(sourceText, "private void RestoreLiveForPlaybackThreadExit(");
        AssertContains(sourceText, "Interlocked.Exchange(ref _lastVideoPtsTicks, 0);\n        RestoreLiveAudio();\n        SafeResumePreviewSubmission(operation);\n        SetState(FlashbackPlaybackState.Live);");
        AssertDoesNotContain(sourceText, "_suppressAudioUntilPtsTicks");
        AssertContains(sourceText, "if (State == FlashbackPlaybackState.Live && !PlaybackThreadAlive)\n        {\n            MarkCommandNoOp(CommandKind.GoLive, \"live_thread_not_running\");\n            return false;\n        }");
        AssertContains(sourceText, "if (State == FlashbackPlaybackState.Live && !PlaybackThreadAlive)\n        {\n            MarkCommandNoOp(CommandKind.Nudge, \"live_thread_not_running\", delta: delta);\n            return false;\n        }");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_CMD_NOOP kind={kind} reason={reason}{FormatCommandDetail(position, delta)}");
        AssertContains(sourceText, "private bool EnsurePlaybackThread(CommandKind commandKind)");
        AssertContains(sourceText, "private readonly object _playbackThreadSync = new();");
        AssertContains(sourceText, "lock (_playbackThreadSync)");
        AssertContains(sourceText, "if (_disposedFlag != 0) return RejectCommand(commandKind, \"disposed\", \"disposed\", false);");
        AssertContains(sourceText, "if (Volatile.Read(ref _playbackThreadStarted) != 0)\n        {\n            if (_playbackThread is { IsAlive: true })");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_THREAD_RECOVER reason=stale_stopped");
        AssertContains(sourceText, "Logger.Log(\"FLASHBACK_PLAYBACK_THREAD_RECOVER reason=stale_stopped\");\n            DrainAbandonedCommandsOnThreadExit(_commandChannel);");
        AssertContains(sourceText, "DisposePlaybackCtsBestEffort(_playCts, \"recover_stale_thread\");");
        AssertContains(sourceText, "Volatile.Write(ref _playbackThreadStarted, 0);\n        }\n\n        if (Interlocked.CompareExchange(ref _playbackThreadStarted, 1, 0) != 0)");
        AssertContains(sourceText, "ObjectDisposedException.ThrowIf(_disposedFlag != 0, this);");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_AUDIO_UPDATE_SKIP reason=disposed");
        AssertContains(sourceText, "private const int CommandQueueCapacity = 256;");
        AssertContains(sourceText, "public int CommandQueueCapacityCommands => CommandQueueCapacity;");
        AssertContains(sourceText, "private Channel<PlaybackCommand> _commandChannel;");
        AssertContains(sourceText, "_commandChannel = CreateCommandChannel();");
        AssertContains(sourceText, "_commandChannel = CreateCommandChannel();");
        AssertContains(sourceText, "private Channel<PlaybackCommand> CreateCommandChannel()");
        AssertContains(sourceText, "Channel.CreateBounded<PlaybackCommand>");
        AssertContains(sourceText, "new BoundedChannelOptions(CommandQueueCapacity)");
        AssertContains(sourceText, "FullMode = BoundedChannelFullMode.Wait");
        AssertContains(sourceText, "private bool IsCommandChannelOpenForDropRetry()");
        AssertContains(sourceText, "private bool TryDropOldestQueuedCommandForNewCommand(out PlaybackCommand droppedCommand)");
        AssertContains(sourceText, "private void TrackDroppedQueuedCommand(PlaybackCommand droppedCommand, CommandKind newCommandKind)");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_CMD_DROP_OLD kind={droppedCommand.Kind}{detail} new_kind={newCommandKind} reason=channel_full");
        AssertContains(sourceText, "private void ClearQueuedCommandSlotForDroppedCommand(PlaybackCommand command)");
        AssertDoesNotContain(sourceText, "Channel.CreateUnbounded<PlaybackCommand>");
        AssertContains(sourceText, "catch (Exception ex)\n        {\n            Logger.Log($\"FLASHBACK_PLAYBACK_THREAD_START_FAIL type={ex.GetType().Name} msg='{ex.Message}'\");");
        AssertContains(sourceText, "DisposePlaybackCtsBestEffort(_playCts, \"thread_start_fail\");");
        AssertContains(sourceText, "_playbackThread = null;\n            Interlocked.Exchange(ref _playbackThreadStarted, 0);");
        AssertContains(sourceText, "return RejectCommand(\n                commandKind,\n                $\"thread_start_failed:{ex.GetType().Name}:{ex.Message}\",\n                $\"thread_start_failed type={ex.GetType().Name}\",\n                false);");
        AssertContains(sourceText, "Logger.Log(\"FLASHBACK_PLAYBACK_GO_LIVE\");\n        return;");
        AssertContains(sourceText, "var commandChannel = _commandChannel;");
        AssertContains(sourceText, "_playbackThread = new Thread(() => PlaybackThreadEntry(threadCts, commandChannel))");
        AssertContains(sourceText, "private void PlaybackThreadEntry(CancellationTokenSource cts, Channel<PlaybackCommand> commandChannel)");
        AssertContains(sourceText, "SUSSUDIO_FLASHBACK_PLAYBACK_MMCSS_TASK");
        AssertContains(sourceText, "SUSSUDIO_FLASHBACK_PLAYBACK_MMCSS_PRIORITY");
        AssertContains(sourceText, "using var mmcss = MmcssThreadRegistration.TryRegister(_playbackMmcssTask, _playbackMmcssPriority, message => Logger.Log(message));");
        AssertContains(sourceText, "var canRead = commandChannel.Reader.WaitToReadAsync(cts.Token).AsTask().GetAwaiter().GetResult();");
        AssertContains(sourceText, "if (!canRead)\n                        {\n                            Logger.Log(\"FLASHBACK_PLAYBACK_THREAD_EXIT channel_closed\");\n                            isScrubbing = false;\n                            RestoreLiveForPlaybackThreadExit(ref decoder, ref fileOpen, \"channel_closed\");");
        AssertContains(sourceText, "RestoreLiveForPlaybackThreadExit(ref decoder, ref fileOpen, \"thread_disposed\");\n                            return;\n                        }");
        AssertContains(sourceText, "if (_disposedFlag != 0)\n                        {\n                            Logger.Log(\"FLASHBACK_PLAYBACK_THREAD_EXIT\");\n                            isScrubbing = false;\n                            RestoreLiveForPlaybackThreadExit(ref decoder, ref fileOpen, \"thread_disposed\");");
        AssertContains(sourceText, "catch (OperationCanceledException)\n        {\n            Logger.Log(\"FLASHBACK_PLAYBACK_THREAD_CANCELLED\");");
        AssertContains(sourceText, "catch (Exception ex)\n            {\n                Logger.Log($\"FLASHBACK_PLAYBACK_CANCEL_WARN type={ex.GetType().Name} msg='{ex.Message}'\");\n            }");
        AssertContains(sourceText, "finally\n        {\n            ClearPrebufferedFrames(prebufferedFrames, \"thread_exit\");\n            timeEndPeriod(1);");
        AssertContains(sourceText, "var threadExited = true;");
        AssertContains(sourceText, "if (ReferenceEquals(Thread.CurrentThread, thread))\n                {\n                    Logger.Log(\"FLASHBACK_PLAYBACK_THREAD_JOIN_SKIP reason=self\");\n                    SetLastCommandFailure(\"thread_join_skipped:self\");\n                    threadExited = false;\n                }");
        AssertContains(sourceText, "private static readonly TimeSpan PlaybackThreadStopTimeout = TimeSpan.FromSeconds(3);");
        AssertContains(sourceText, "private static readonly TimeSpan PreviewDetachThreadStopTimeout = TimeSpan.FromSeconds(10);");
        AssertContains(sourceText, "Logger.Log($\"FLASHBACK_PLAYBACK_THREAD_JOIN_TIMEOUT op={operation} timeout_ms={timeout.TotalMilliseconds:0}\");\n                    SetLastCommandFailure($\"thread_join_timeout:{operation}\");\n                    threadExited = false;");
        AssertContains(sourceText, "SetLastCommandFailure(\"thread_join_skipped:self\");");
        AssertContains(sourceText, "SetLastCommandFailure($\"thread_join_timeout:{operation}\");");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_STOP_THREAD_COMPLETE op={operation} duration_ms=");
        AssertContains(sourceText, "thread_was_alive={threadWasAlive} thread_exited={threadExited}");
        AssertContains(sourceText, "active_at_request={activeKindAtRequest} active_ms_at_request={activeElapsedMsAtRequest:0.###}");
        AssertContains(sourceText, "if (threadExited)\n            {\n                ApplyDeferredPreviewAttachAfterStopTimeout();\n                DisposePlaybackCtsBestEffort(_playCts, \"stop_thread\");");
        AssertContains(sourceText, "Interlocked.Exchange(ref _pendingCommands, 0);\n                ClearQueuedCommandSlotsBarrier();\n                Volatile.Write(ref _playbackThreadStarted, 0);");
        AssertContains(sourceText, "Volatile.Write(ref _activeCommandKind, (int)cmd.Kind);");
        AssertContains(sourceText, "Volatile.Write(ref _activeCommandStartedTimestamp, commandStarted);");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_CMD_COMPLETE kind={cmd.Kind} duration_ms={commandElapsedMs:0.###}");
        AssertContains(sourceText, "private static string FormatActiveCommandKind(int rawKind)");
        AssertContains(sourceText, "private double GetActiveCommandElapsedMs(long nowTimestamp)");
        AssertContains(sourceText, "if (cts.IsCancellationRequested)\n                        {\n                            Logger.Log(\"FLASHBACK_PLAYBACK_THREAD_EXIT cancellation_requested\");");
        AssertContains(sourceText, "Logger.Log(\"FLASHBACK_PLAYBACK_THREAD_EXIT cancellation_requested\");\n                            RestoreLiveForPlaybackThreadExit(ref decoder, ref fileOpen, \"thread_cancelled\");");
        AssertContains(sourceText, "PaceAndDecodeFrame(decoder, prebufferedFrames, commandChannel, pacingStopwatch, ref frameDuration, ref fileOpen, frozenValidStart, cts.Token)");
        AssertContains(sourceText, "SeekAndDisplayKeyframe(decoder, ref fileOpen, cmd.Position, frozenValidStart, CommandKind.Seek, cts.Token)");
        AssertContains(sourceText, "SeekAndDisplayKeyframe(decoder, ref fileOpen, cmd.Position, frozenValidStart, CommandKind.BeginScrub, cts.Token)");
        AssertContains(sourceText, "SeekAndDisplayKeyframe(decoder, ref fileOpen, cmd.Position, frozenValidStart, CommandKind.UpdateScrub, cts.Token)");
        AssertContains(sourceText, "SeekAndDisplayKeyframe(decoder, ref fileOpen, nudgedPos, frozenValidStart, CommandKind.Nudge, cts.Token)");
        AssertContains(sourceText, "TrySeekWithActiveFmp4Reopen(decoder, ref fileOpen, coalescedSeekTarget, \"seek_resume\", cts.Token)");
        AssertContains(sourceText, "TrySeekWithActiveFmp4Reopen(decoder, ref fileOpen, endScrubTarget, \"end_scrub\", cts.Token)");
        AssertContains(sourceText, "TrySeekWithActiveFmp4Reopen(decoder, ref fileOpen, seekTarget, \"play\", cts.Token)");
        AssertContains(sourceText, "TryDecodeNextVideoFrameWithMetrics(decoder, out var nudgeFrame, cts.Token)");
        AssertContains(sourceText, "CancellationToken cancellationToken)\n    {\n        try\n        {\n            cancellationToken.ThrowIfCancellationRequested();");
        var playbackFramesText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.PlaybackFrames.cs")
            .Replace("\r\n", "\n");
        var playbackLoopText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.PlaybackLoop.cs")
            .Replace("\r\n", "\n");
        AssertContains(playbackFramesText, "private bool TryReadNextPlaybackFrame(");
        AssertContains(playbackFramesText, "private void ClearPrebufferedFrames(");
        AssertContains(playbackFramesText, "private bool TryResolveAudioDriftFrameSkip(");
        AssertContains(playbackLoopText, "TryResolveAudioDriftFrameSkip(");
        AssertDoesNotContain(playbackLoopText, "private bool TryReadNextPlaybackFrame(");
        AssertDoesNotContain(playbackLoopText, "private void ClearPrebufferedFrames(");
        AssertDoesNotContain(playbackLoopText, "private bool TryResolveAudioDriftFrameSkip(");
        AssertContains(sourceText, "while (skipped < MaxSkipFrames && driftMs < -FrameSkipThresholdMs)\n        {\n            cancellationToken.ThrowIfCancellationRequested();");
        AssertContains(sourceText, "if (commandChannel.Reader.TryPeek(out _))\n            {\n                ReleaseHeldFrameBestEffort(videoFrame, \"av_sync_skip_command_pending\");");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_FRAME_SKIP_COMMAND_PENDING count={skipped}");
        AssertContains(sourceText, "const double FrameSkipThresholdMs = 500.0;");
        // Frame-skip catch-up loop must re-sync the audio clock each iteration so a
        // long catch-up burst does not extrapolate from a stale wall-time anchor.
        AssertContains(sourceText, "private bool TryComputeAudioMasterDriftMs(long videoPtsTicks, out double driftMs)");
        AssertContains(sourceText, "if (!TryComputeAudioMasterDriftMs(videoFrame.Pts.Ticks, out var driftMs) ||\n            driftMs >= -FrameSkipThresholdMs)");
        AssertContains(sourceText, "if (!TryComputeAudioMasterDriftMs(videoFrame.Pts.Ticks, out driftMs))\n            {\n                break;\n            }");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_FRAME_SKIP_EOS count={skipped}");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_FRAME_SKIP_BUDGET count={skipped}");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_FMP4_REOPEN_BEFORE_SEGMENT_SWITCH");
        AssertContains(sourceText, "nextSegmentStart.Value - lastFrameAbsPts > TimeSpan.FromMilliseconds(250)");
        AssertContains(sourceText, "catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)\n        {\n            throw;\n        }\n        catch (Exception ex)\n        {\n            SnapToLiveOnError(decoder, ex, ref fileOpen);");
        AssertContains(sourceText, "SafeResumePreviewSubmission(operation);");
        AssertContains(sourceText, "catch (OperationCanceledException)\n        {\n            Logger.Log(\"FLASHBACK_PLAYBACK_THREAD_CANCELLED\");\n            RestoreLiveForPlaybackThreadExit(ref decoder, ref fileOpen, \"thread_cancelled\");");
        AssertContains(sourceText, "Logger.Log($\"FLASHBACK_PLAYBACK_FATAL type={ex.GetType().Name} error='{ex.Message}'\");\n            RestoreLiveForPlaybackThreadExit(ref decoder, ref fileOpen, \"thread_fatal\");");
        AssertContains(sourceText, "var decoderToDispose = decoder;\n            decoder = null;");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_DECODER_CLEANUP_WARN op=close");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_DECODER_CLEANUP_WARN op=dispose");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_DECODER_CLEANUP_COMPLETE was_open={wasOpen}");
        AssertContains(sourceText, "release_ms={releaseMs:0.###} close_ms={closeMs:0.###} dispose_ms={disposeMs:0.###} total_ms={totalMs:0.###}");
        AssertContains(sourceText, "fileOpen = false;\n        _currentOpenFilePath = null;\n        _decoderHwAccel = \"N/A\";");
        AssertContains(sourceText, "CompleteCommandChannelForThreadExit(commandChannel);\n            DrainAbandonedCommandsOnThreadExit(commandChannel);");
        AssertContains(sourceText, "private static void CompleteCommandChannelForThreadExit(Channel<PlaybackCommand> commandChannel)");
        AssertContains(sourceText, "commandChannel.Writer.TryComplete();");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_CHANNEL_COMPLETE_WARN");
        AssertContains(sourceText, "Interlocked.Add(ref _commandsDropped, abandoned);");
        AssertContains(sourceText, "if (string.IsNullOrEmpty(Volatile.Read(ref _lastCommandFailure)))\n            {\n                SetLastCommandFailure($\"abandoned_on_exit:{abandoned}\");\n            }");
        AssertContains(sourceText, "Interlocked.Exchange(ref _pendingCommands, 0);");
        AssertContains(sourceText, "var ownsPlaybackThread = ReferenceEquals(Thread.CurrentThread, _playbackThread);");
        AssertContains(sourceText, "var ownsCts = ReferenceEquals(cts, _playCts);");
        AssertContains(sourceText, "if (ownsPlaybackThread)\n            {\n                _playbackThread = null;\n            }");
        AssertContains(sourceText, "_playbackThread = null;");
        AssertContains(sourceText, "StopPlaybackThread(PlaybackThreadStopTimeout, \"dispose\");\n        _initialized = false;\n        Logger.Log(\"FLASHBACK_PLAYBACK_DISPOSED\");");
        AssertContains(sourceText, "if (_disposedFlag != 0 && command.Kind != CommandKind.Stop)\n        {\n            return RejectCommand(command.Kind, \"disposed\", \"disposed\", false);\n        }");
        AssertContains(sourceText, "if (ownsCts)\n            {\n                _playCts = null;\n            }\n            DisposePlaybackCtsBestEffort(cts, \"thread_exit\");");
        AssertContains(sourceText, "private static void DisposePlaybackCtsBestEffort(CancellationTokenSource? cts, string operation)");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_CTS_DISPOSE_WARN");
        AssertContains(sourceText, "if (ownsPlaybackThread || ownsCts)\n            {\n                Volatile.Write(ref _playbackThreadStarted, 0);\n            }");
        AssertContains(sourceText, "Interlocked.Increment(ref _commandsEnqueued);\n        UpdateMaxPendingCommands(pending);\n        MarkCommandQueued(command.Kind);\n        return true;");

        return Task.CompletedTask;
    }
}

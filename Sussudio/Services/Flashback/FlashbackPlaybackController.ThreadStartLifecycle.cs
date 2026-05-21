using System;
using System.Threading;

namespace Sussudio.Services.Flashback;

internal sealed partial class FlashbackPlaybackController
{
    // --- Playback thread start/recovery lifecycle ---

    private bool EnsurePlaybackThread(CommandKind commandKind)
    {
        lock (_playbackThreadSync)
        {
            if (_disposedFlag != 0) return RejectCommand(commandKind, "disposed", "disposed", false);
            if (Volatile.Read(ref _playbackThreadStarted) != 0)
            {
                if (_playbackThread is { IsAlive: true })
                {
                    return true;
                }

                Logger.Log("FLASHBACK_PLAYBACK_THREAD_RECOVER reason=stale_stopped");
                DrainAbandonedCommandsOnThreadExit(_commandChannel);
                DisposePlaybackCtsBestEffort(_playCts, "recover_stale_thread");
                _playCts = null;
                _playbackThread = null;
                Interlocked.Exchange(ref _pendingCommands, 0);
                ClearQueuedCommandSlotsBarrier();
                Volatile.Write(ref _playbackThreadStarted, 0);
            }

            if (Interlocked.CompareExchange(ref _playbackThreadStarted, 1, 0) != 0)
                return true;

            // Recreate the command channel because StopPlaybackThread completes it.
            _commandChannel = CreateCommandChannel();
            var commandChannel = _commandChannel;
            _playCts = new CancellationTokenSource();
            var threadCts = _playCts;
            _playbackThread = new Thread(() => PlaybackThreadEntry(threadCts, commandChannel))
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
                return RejectCommand(
                    commandKind,
                    $"thread_start_failed:{ex.GetType().Name}:{ex.Message}",
                    $"thread_start_failed type={ex.GetType().Name}",
                    false);
            }
            Logger.Log("FLASHBACK_PLAYBACK_THREAD_START");
            return true;
        }
    }
}

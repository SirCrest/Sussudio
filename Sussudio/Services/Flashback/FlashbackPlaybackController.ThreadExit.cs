using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using Sussudio.Models;

namespace Sussudio.Services.Flashback;

internal sealed partial class FlashbackPlaybackController
{
    private void CompletePlaybackThreadExit(
        Queue<DecodedVideoFrame> prebufferedFrames,
        CancellationTokenSource cts,
        Channel<PlaybackCommand> commandChannel)
    {
        ClearPrebufferedFrames(prebufferedFrames, "thread_exit");
        timeEndPeriod(1);
        CompleteCommandChannelForThreadExit(commandChannel);
        DrainAbandonedCommandsOnThreadExit(commandChannel);
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
}

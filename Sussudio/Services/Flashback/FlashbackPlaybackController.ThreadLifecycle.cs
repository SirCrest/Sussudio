using System;
using System.Threading;

namespace Sussudio.Services.Flashback;

internal sealed partial class FlashbackPlaybackController
{
    // --- Playback thread state and timeout policy ---

    private static readonly TimeSpan PlaybackThreadStopTimeout = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan PreviewDetachThreadStopTimeout = TimeSpan.FromSeconds(10);

    private readonly object _playbackThreadSync = new();
    private Thread? _playbackThread;
    private int _playbackThreadStarted;
    private CancellationTokenSource? _playCts;
}

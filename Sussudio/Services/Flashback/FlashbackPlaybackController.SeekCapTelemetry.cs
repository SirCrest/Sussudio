using System;
using System.Threading;

namespace Sussudio.Services.Flashback;

internal sealed partial class FlashbackPlaybackController
{
    private long _playbackSeekForwardDecodeCapHits;
    private int _lastPlaybackSeekHitForwardDecodeCap;

    public long PlaybackSeekForwardDecodeCapHits => Interlocked.Read(ref _playbackSeekForwardDecodeCapHits);
    public bool LastPlaybackSeekHitForwardDecodeCap => Volatile.Read(ref _lastPlaybackSeekHitForwardDecodeCap) != 0;

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
}

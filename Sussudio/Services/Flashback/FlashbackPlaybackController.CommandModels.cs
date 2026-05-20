using System;

namespace Sussudio.Services.Flashback;

internal sealed partial class FlashbackPlaybackController
{
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
}

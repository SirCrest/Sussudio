using System;

namespace Sussudio.ViewModels;

/// <summary>
/// Flashback playback and scrub command routing.
/// </summary>
public partial class MainViewModel
{
    /// <summary>
    /// Forwards a scrub-begin command to the active flashback playback controller.
    /// Returns true when the controller accepted the command (timeline was
    /// running and not stopped); false when flashback is disabled or the
    /// controller refused.
    /// </summary>
    public bool FlashbackBeginScrub(TimeSpan position)
    {
        return _sessionCoordinator.FlashbackBeginScrub(position);
    }

    public bool FlashbackSeek(TimeSpan position)
    {
        return _sessionCoordinator.FlashbackSeek(position);
    }

    public bool FlashbackUpdateScrub(TimeSpan position)
    {
        return _sessionCoordinator.FlashbackUpdateScrub(position);
    }

    public bool FlashbackEndScrub()
    {
        return _sessionCoordinator.FlashbackEndScrub();
    }

    public bool FlashbackEndScrubAt(TimeSpan position)
    {
        return _sessionCoordinator.FlashbackEndScrubAt(position);
    }

    public bool FlashbackPlay()
    {
        return _sessionCoordinator.FlashbackPlay();
    }

    public bool FlashbackPause()
    {
        return _sessionCoordinator.FlashbackPause();
    }

    public bool FlashbackGoLive()
    {
        return _sessionCoordinator.FlashbackGoLive();
    }

    public bool FlashbackNudge(TimeSpan delta)
    {
        return _sessionCoordinator.FlashbackNudge(delta);
    }
}

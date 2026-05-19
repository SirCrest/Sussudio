using System;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;

namespace Sussudio.ViewModels;

/// <summary>
/// Flashback playback, scrub, marker, and automation action command routing.
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

    public Task<bool> ExecuteFlashbackActionAsync(
        AutomationFlashbackAction action,
        TimeSpan? position = null,
        CancellationToken cancellationToken = default)
        => InvokeOnUiThreadAsync(() => ExecuteFlashbackAction(action, position), cancellationToken);

    private bool ExecuteFlashbackAction(AutomationFlashbackAction action, TimeSpan? position)
    {
        switch (action)
        {
            case AutomationFlashbackAction.Play:
                if (position.HasValue)
                {
                    if (!FlashbackSeek(position.Value))
                    {
                        return false;
                    }

                    return FlashbackPlay();
                }

                return FlashbackPlay();
            case AutomationFlashbackAction.Pause:
                return FlashbackPause();
            case AutomationFlashbackAction.GoLive:
                return FlashbackGoLive();
            case AutomationFlashbackAction.Seek:
                return FlashbackSeek(position ?? TimeSpan.Zero);
            case AutomationFlashbackAction.BeginScrub:
                return FlashbackBeginScrub(position ?? TimeSpan.Zero);
            case AutomationFlashbackAction.UpdateScrub:
                return FlashbackUpdateScrub(position ?? TimeSpan.Zero);
            case AutomationFlashbackAction.EndScrub:
                return position.HasValue
                    ? FlashbackEndScrubAt(position.Value)
                    : FlashbackEndScrub();
            case AutomationFlashbackAction.SetInPoint:
                return _sessionCoordinator.FlashbackSetInPoint().HasValue;
            case AutomationFlashbackAction.SetOutPoint:
                return _sessionCoordinator.FlashbackSetOutPoint().HasValue;
            case AutomationFlashbackAction.ClearInOutPoints:
                return _sessionCoordinator.FlashbackClearInOutPoints();
            default:
                throw new InvalidOperationException($"Unsupported flashback action '{action}'.");
        }
    }

    public TimeSpan? FlashbackSetInPoint()
        => _sessionCoordinator.FlashbackSetInPoint();

    /// <summary>
    /// Pin the flashback in-point at an explicit user-intended position.
    /// The UI calls this with the visual playhead location so a marker placed
    /// during scrubbing lands where the user is pointing instead of at the
    /// keyframe-snapped <c>PlaybackPosition</c> the controller publishes after
    /// each decode (which can lag by hundreds of milliseconds mid-GOP).
    /// </summary>
    public TimeSpan? FlashbackSetInPointAt(TimeSpan position)
        => _sessionCoordinator.FlashbackSetInPointAt(position);

    public TimeSpan? FlashbackSetOutPoint()
        => _sessionCoordinator.FlashbackSetOutPoint();

    /// <summary>
    /// Pin the flashback out-point at an explicit user-intended position.
    /// See <see cref="FlashbackSetInPointAt"/> for rationale.
    /// </summary>
    public TimeSpan? FlashbackSetOutPointAt(TimeSpan position)
        => _sessionCoordinator.FlashbackSetOutPointAt(position);

    public bool FlashbackClearInOutPoints()
        => _sessionCoordinator.FlashbackClearInOutPoints();
}

using System;

namespace Sussudio.ViewModels;

/// <summary>
/// Flashback in/out marker command routing.
/// </summary>
public partial class MainViewModel
{
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

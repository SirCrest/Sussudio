using System;

namespace Sussudio.Services.Capture;

public sealed partial class CaptureSessionCoordinator
{
    internal bool FlashbackBeginScrub(TimeSpan position)
    {
        if (!TryGetActiveFlashback(nameof(FlashbackBeginScrub), out var controller)) return false;
        return controller.BeginScrub(position);
    }

    internal bool FlashbackSeek(TimeSpan position)
    {
        if (!TryGetActiveFlashback(nameof(FlashbackSeek), out var controller)) return false;
        return controller.Seek(position);
    }

    internal bool FlashbackUpdateScrub(TimeSpan position)
    {
        if (!TryGetActiveFlashback(nameof(FlashbackUpdateScrub), out var controller)) return false;
        return controller.UpdateScrub(position);
    }

    internal bool FlashbackEndScrub()
    {
        if (!TryGetActiveFlashback(nameof(FlashbackEndScrub), out var controller)) return false;
        return controller.EndScrub();
    }

    internal bool FlashbackEndScrubAt(TimeSpan position)
    {
        if (!TryGetActiveFlashback(nameof(FlashbackEndScrubAt), out var controller)) return false;
        return controller.EndScrubAt(position);
    }

    internal bool FlashbackPlay()
    {
        if (!TryGetActiveFlashback(nameof(FlashbackPlay), out var controller)) return false;
        return controller.Play();
    }

    internal bool FlashbackPause()
    {
        if (!TryGetActiveFlashback(nameof(FlashbackPause), out var controller)) return false;
        return controller.Pause();
    }

    internal bool FlashbackGoLive()
    {
        if (!TryGetActiveFlashback(nameof(FlashbackGoLive), out var controller)) return false;
        return controller.GoLive();
    }

    internal bool FlashbackNudge(TimeSpan delta)
    {
        if (!TryGetActiveFlashback(nameof(FlashbackNudge), out var controller)) return false;
        return controller.NudgePosition(delta);
    }

    internal TimeSpan? FlashbackSetInPoint()
    {
        return TryGetActiveFlashback(nameof(FlashbackSetInPoint), out var controller)
            ? controller.SetInPoint()
            : null;
    }

    internal TimeSpan? FlashbackSetInPointAt(TimeSpan position)
    {
        return TryGetActiveFlashback(nameof(FlashbackSetInPointAt), out var controller)
            ? controller.SetInPointAt(position)
            : null;
    }

    internal TimeSpan? FlashbackSetOutPoint()
    {
        return TryGetActiveFlashback(nameof(FlashbackSetOutPoint), out var controller)
            ? controller.SetOutPoint()
            : null;
    }

    internal TimeSpan? FlashbackSetOutPointAt(TimeSpan position)
    {
        return TryGetActiveFlashback(nameof(FlashbackSetOutPointAt), out var controller)
            ? controller.SetOutPointAt(position)
            : null;
    }

    internal bool FlashbackClearInOutPoints()
    {
        if (!TryGetActiveFlashback(nameof(FlashbackClearInOutPoints), out var controller)) return false;
        controller.ClearInOutPoints();
        return true;
    }
}

using System;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;

namespace Sussudio.ViewModels;

/// <summary>
/// Automation-facing Flashback playback action dispatch.
/// </summary>
public partial class MainViewModel
{
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
                return FlashbackSetInPoint().HasValue;
            case AutomationFlashbackAction.SetOutPoint:
                return FlashbackSetOutPoint().HasValue;
            case AutomationFlashbackAction.ClearInOutPoints:
                return FlashbackClearInOutPoints();
            default:
                throw new InvalidOperationException($"Unsupported flashback action '{action}'.");
        }
    }
}

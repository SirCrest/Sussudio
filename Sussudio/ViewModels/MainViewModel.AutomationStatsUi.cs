using System;
using System.Threading;
using System.Threading.Tasks;

namespace Sussudio.ViewModels;

/// <summary>
/// Automation commands that mutate stats dock and frame-time overlay visibility.
/// </summary>
public partial class MainViewModel
{
    public Action<string, bool>? StatsSectionVisibilityHandler { get; set; }
    public Action<bool>? FrameTimeOverlayVisibilityHandler { get; set; }

    public Task SetStatsSectionVisibleAsync(string section, bool visible, CancellationToken cancellationToken = default)
    {
        return InvokeOnUiThreadAsync(() =>
        {
            StatsSectionVisibilityHandler?.Invoke(section, visible);
            return Task.CompletedTask;
        }, cancellationToken);
    }

    public Task SetStatsVisibleAsync(bool visible, CancellationToken cancellationToken = default)
    {
        return InvokeOnUiThreadAsync(() =>
        {
            IsStatsVisible = visible;
            return Task.CompletedTask;
        }, cancellationToken);
    }

    public Task SetFrameTimeOverlayVisibleAsync(bool visible, CancellationToken cancellationToken = default)
    {
        return InvokeOnUiThreadAsync(() =>
        {
            FrameTimeOverlayVisibilityHandler?.Invoke(visible);
            return Task.CompletedTask;
        }, cancellationToken);
    }
}

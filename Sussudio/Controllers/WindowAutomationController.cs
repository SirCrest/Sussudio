using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using Sussudio.Models;
using Sussudio.ViewModels;

namespace Sussudio.Controllers;

internal sealed class WindowAutomationControllerContext
{
    public required DispatcherQueue DispatcherQueue { get; init; }
    public required MainViewModel ViewModel { get; init; }
    public required Func<AppWindow> GetAppWindow { get; init; }
    public required Func<IntPtr> GetWindowHandle { get; init; }
    public required Func<Action, CancellationToken, Task> InvokeOnUiThreadAsync { get; init; }
}

internal sealed class WindowAutomationController
{
    private readonly WindowAutomationControllerContext _context;

    public WindowAutomationController(WindowAutomationControllerContext context)
    {
        _context = context;
    }

    public Task MinimizeAsync(CancellationToken cancellationToken = default) =>
        PresenterActionAsync(p => p.Minimize(), cancellationToken);

    public Task MaximizeAsync(CancellationToken cancellationToken = default) =>
        PresenterActionAsync(p => p.Maximize(), cancellationToken);

    public Task RestoreAsync(CancellationToken cancellationToken = default) =>
        PresenterActionAsync(p => p.Restore(), cancellationToken);

    public Task OpenRecordingsFolderAsync(CancellationToken cancellationToken = default)
    {
        return _context.InvokeOnUiThreadAsync(() =>
        {
            var path = _context.ViewModel.OutputPath;
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new InvalidOperationException("Output path is not set.");
            }

            if (!Directory.Exists(path))
            {
                throw new DirectoryNotFoundException($"Output path does not exist: {path}");
            }

            Process.Start("explorer.exe", path);
        }, cancellationToken);
    }

    public Task MoveToAsync(int x, int y, CancellationToken cancellationToken = default)
    {
        return _context.InvokeOnUiThreadAsync(() =>
        {
            var appWindow = _context.GetAppWindow();
            appWindow.Move(new Windows.Graphics.PointInt32(x, y));
        }, cancellationToken);
    }

    public Task ResizeToAsync(int width, int height, CancellationToken cancellationToken = default)
    {
        return _context.InvokeOnUiThreadAsync(() =>
        {
            var appWindow = _context.GetAppWindow();
            appWindow.Resize(new Windows.Graphics.SizeInt32(Math.Max(1, width), Math.Max(1, height)));
        }, cancellationToken);
    }

    public Task SnapToRegionAsync(AutomationWindowAction region, CancellationToken cancellationToken = default)
    {
        return _context.InvokeOnUiThreadAsync(() =>
        {
            var appWindow = _context.GetAppWindow();
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(_context.GetWindowHandle());
            var displayArea = DisplayArea.GetFromWindowId(windowId, DisplayAreaFallback.Primary);
            var work = displayArea.WorkArea;

            if (appWindow.Presenter is OverlappedPresenter presenter &&
                presenter.State == OverlappedPresenterState.Maximized)
            {
                presenter.Restore();
            }

            int x, y, w, h;
            switch (region)
            {
                case AutomationWindowAction.SnapLeft:
                    x = work.X; y = work.Y; w = work.Width / 2; h = work.Height; break;
                case AutomationWindowAction.SnapRight:
                    x = work.X + work.Width / 2; y = work.Y; w = work.Width - work.Width / 2; h = work.Height; break;
                case AutomationWindowAction.SnapTopLeft:
                    x = work.X; y = work.Y; w = work.Width / 2; h = work.Height / 2; break;
                case AutomationWindowAction.SnapTopRight:
                    x = work.X + work.Width / 2; y = work.Y; w = work.Width - work.Width / 2; h = work.Height / 2; break;
                case AutomationWindowAction.SnapBottomLeft:
                    x = work.X; y = work.Y + work.Height / 2; w = work.Width / 2; h = work.Height - work.Height / 2; break;
                case AutomationWindowAction.SnapBottomRight:
                    x = work.X + work.Width / 2; y = work.Y + work.Height / 2; w = work.Width - work.Width / 2; h = work.Height - work.Height / 2; break;
                case AutomationWindowAction.Center:
                    var curSize = appWindow.Size;
                    w = curSize.Width; h = curSize.Height;
                    x = work.X + (work.Width - w) / 2; y = work.Y + (work.Height - h) / 2; break;
                default:
                    return;
            }

            appWindow.MoveAndResize(new Windows.Graphics.RectInt32(x, y, w, h));
        }, cancellationToken);
    }

    private Task PresenterActionAsync(Action<OverlappedPresenter> action, CancellationToken cancellationToken = default)
    {
        return _context.InvokeOnUiThreadAsync(() =>
        {
            var appWindow = _context.GetAppWindow();
            if (appWindow.Presenter is OverlappedPresenter presenter)
            {
                action(presenter);
            }
        }, cancellationToken);
    }
}

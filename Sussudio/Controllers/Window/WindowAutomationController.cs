using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using Sussudio.Models;
using Sussudio.Services.Automation;
using Sussudio.Services.Contracts;
using Sussudio.Services.Recording;
using Sussudio.Tools;
using Sussudio.ViewModels;
using Windows.Graphics;

namespace Sussudio.Controllers;

internal sealed class WindowAutomationHostLifecycleController : IAsyncDisposable
{
    private readonly IAutomationDiagnosticsHub _diagnosticsHub;
    private readonly NamedPipeAutomationServer _pipeServer;
    private readonly bool _tokenRequired;
    private readonly string _pipeName;
    private int _started;

    public WindowAutomationHostLifecycleController(
        IAutomationViewModel viewModel,
        Func<CancellationToken, Task<PreviewRuntimeSnapshot>> previewSnapshotProvider,
        IAutomationWindowControl windowControl)
    {
        var automationToken = Environment.GetEnvironmentVariable(AutomationPipeProtocol.AutomationKeyEnvVar);
        var automationPipeName = Environment.GetEnvironmentVariable("SUSSUDIO_AUTOMATION_PIPE");
        if (string.IsNullOrWhiteSpace(automationPipeName))
        {
            automationPipeName = NamedPipeAutomationServer.DefaultPipeName;
        }

        var automationPorts = AutomationViewModelPorts.From(viewModel);
        _diagnosticsHub = new AutomationDiagnosticsHub(
            automationPorts.SnapshotQuery,
            previewSnapshotProvider,
            new RecordingVerifier());
        var automationDispatcher = new AutomationCommandDispatcher(
            automationPorts,
            _diagnosticsHub,
            windowControl,
            automationToken);

        _tokenRequired = !string.IsNullOrWhiteSpace(automationToken);
        _pipeName = automationPipeName;
        _pipeServer = new NamedPipeAutomationServer(
            automationDispatcher,
            _pipeName,
            _tokenRequired);
    }

    public void Start()
    {
        if (Interlocked.Exchange(ref _started, 1) != 0)
        {
            return;
        }

        if (_pipeServer.Start())
        {
            _diagnosticsHub.Start();
            Logger.Log(
                $"Automation control ready on pipe '{_pipeName}' (token required={_tokenRequired}).");
        }
        else
        {
            Logger.Log(
                $"Automation control disabled on pipe '{_pipeName}' (token required={_tokenRequired}).");
        }
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            await _pipeServer.DisposeAsync();
        }
        catch (Exception ex)
        {
            Logger.Log($"Automation shutdown cleanup failed: {ex.Message}");
        }

        try
        {
            await _diagnosticsHub.DisposeAsync();
        }
        catch (Exception ex)
        {
            Logger.Log($"Automation diagnostics shutdown cleanup failed: {ex.Message}");
        }
    }
}

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

            var currentSize = region == AutomationWindowAction.Center
                ? appWindow.Size
                : default;
            var targetBounds = WindowSnapRegionLayoutPolicy.ResolveTargetBounds(region, work, currentSize);
            if (targetBounds is not { } bounds)
            {
                return;
            }

            appWindow.MoveAndResize(bounds);
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

internal static class WindowSnapRegionLayoutPolicy
{
    public static RectInt32? ResolveTargetBounds(
        AutomationWindowAction region,
        RectInt32 workArea,
        SizeInt32 currentSize)
    {
        return region switch
        {
            AutomationWindowAction.SnapLeft => new RectInt32(
                workArea.X,
                workArea.Y,
                workArea.Width / 2,
                workArea.Height),
            AutomationWindowAction.SnapRight => new RectInt32(
                workArea.X + workArea.Width / 2,
                workArea.Y,
                workArea.Width - workArea.Width / 2,
                workArea.Height),
            AutomationWindowAction.SnapTopLeft => new RectInt32(
                workArea.X,
                workArea.Y,
                workArea.Width / 2,
                workArea.Height / 2),
            AutomationWindowAction.SnapTopRight => new RectInt32(
                workArea.X + workArea.Width / 2,
                workArea.Y,
                workArea.Width - workArea.Width / 2,
                workArea.Height / 2),
            AutomationWindowAction.SnapBottomLeft => new RectInt32(
                workArea.X,
                workArea.Y + workArea.Height / 2,
                workArea.Width / 2,
                workArea.Height - workArea.Height / 2),
            AutomationWindowAction.SnapBottomRight => new RectInt32(
                workArea.X + workArea.Width / 2,
                workArea.Y + workArea.Height / 2,
                workArea.Width - workArea.Width / 2,
                workArea.Height - workArea.Height / 2),
            AutomationWindowAction.Center => new RectInt32(
                workArea.X + (workArea.Width - currentSize.Width) / 2,
                workArea.Y + (workArea.Height - currentSize.Height) / 2,
                currentSize.Width,
                currentSize.Height),
            _ => null
        };
    }
}

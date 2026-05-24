using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Sussudio.Models;
using Sussudio.Services.Automation;
using Sussudio.Services.Preview;

namespace Sussudio.ViewModels;

/// <summary>
/// UI-facing state coordinator. MainViewModel translates user settings and
/// automation requests into serialized CaptureService operations while keeping
/// WinUI properties, saved settings, and diagnostics summaries coherent.
/// </summary>
public partial class MainViewModel : ObservableObject, IDisposable, IAsyncDisposable, IAutomationViewModel
{
    private IntPtr _windowHandle;
    private const string LiveInfoUnavailable = "\u2014";

    public Task RefreshDevicesAsync(CancellationToken cancellationToken = default)
        => _deviceRefreshController.RefreshDevicesAsync(cancellationToken);

    internal void SetPreviewFrameSink(IPreviewFrameSink? sink)
    {
        _captureService.SetPreviewFrameSink(sink);
    }

    internal void CancelPendingPreviewRestart()
        => _previewLifecycleController.CancelPendingPreviewRestart();

    private Task InitializeDeviceAsync(CancellationToken cancellationToken = default)
        => _previewLifecycleController.InitializeDeviceAsync(cancellationToken);

    public Task StartPreviewAsync(bool userInitiated = true, CancellationToken cancellationToken = default)
        => _previewLifecycleController.StartPreviewAsync(userInitiated, cancellationToken);

    public Task SetPreviewEnabledAsync(bool enabled, CancellationToken cancellationToken = default)
        => _previewLifecycleController.SetPreviewEnabledAsync(enabled, cancellationToken);

    public Task StopPreviewAsync()
        => StopPreviewAsync(userInitiated: true, teardownPipeline: false, CancellationToken.None);

    public Task StopPreviewAsync(bool userInitiated)
        => StopPreviewAsync(userInitiated, teardownPipeline: false, CancellationToken.None);

    public Task StopPreviewAsync(bool userInitiated, bool teardownPipeline)
        => StopPreviewAsync(userInitiated, teardownPipeline, CancellationToken.None);

    public Task ApplySelectedDeviceAsync(CaptureDevice device, CancellationToken cancellationToken = default)
        => _previewLifecycleController.ApplySelectedDeviceAsync(device, cancellationToken);

    private Task ReinitializeDeviceAsync(string reason)
        => _previewLifecycleController.ReinitializeDeviceAsync(reason);

    public Task StopPreviewAsync(bool userInitiated, bool teardownPipeline, CancellationToken cancellationToken)
        => _previewLifecycleController.StopPreviewAsync(userInitiated, teardownPipeline, cancellationToken);

    public Action<string, bool>? StatsSectionVisibilityHandler { get; set; }
    public Action<bool>? FrameTimeOverlayVisibilityHandler { get; set; }

    public void SetWindowHandle(IntPtr handle)
    {
        _windowHandle = handle;
    }

    private static void ReplaceCollection<T>(ObservableCollection<T> target, IReadOnlyList<T> source)
    {
        target.Clear();
        foreach (var item in source)
        {
            target.Add(item);
        }
    }

    [ObservableProperty]
    public partial bool IsStatsVisible { get; set; }

    [ObservableProperty]
    public partial bool IsSettingsVisible { get; set; }

    [ObservableProperty]
    public partial bool IsPreviewing { get; set; }

    [ObservableProperty]
    public partial bool IsPreviewReinitializing { get; set; }

    [ObservableProperty]
    public partial bool IsInitialized { get; set; }

    private readonly SemaphoreSlim _previewReinitializeGate = new(1, 1);
    private int _previewReinitializeGeneration;
    private bool _cancelPreviewRestartAfterReinitialize;

    public event EventHandler? PreviewStartRequested;
    public event EventHandler? PreviewStopRequested;
    public event Func<string, Task>? PreviewReinitRequested;
    public event Func<Task>? PreviewRendererStopRequested;

    public Task SetSettingsVisibleAsync(bool visible, CancellationToken cancellationToken = default)
    {
        return InvokeOnUiThreadAsync(() =>
        {
            IsSettingsVisible = visible;
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

    public Task SetStatsSectionVisibleAsync(string section, bool visible, CancellationToken cancellationToken = default)
    {
        return InvokeOnUiThreadAsync(() =>
        {
            StatsSectionVisibilityHandler?.Invoke(section, visible);
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

    public Task SetFlashbackTimelineVisibleAsync(bool visible, CancellationToken cancellationToken = default)
    {
        return InvokeOnUiThreadAsync(() =>
        {
            IsFlashbackTimelineVisible = visible;
            return Task.CompletedTask;
        }, cancellationToken);
    }

    [ObservableProperty]
    public partial string StatusText { get; set; } = "Ready";

    [ObservableProperty]
    public partial string LiveResolution { get; set; } = LiveInfoUnavailable;

    [ObservableProperty]
    public partial string LiveFrameRate { get; set; } = LiveInfoUnavailable;

    [ObservableProperty]
    public partial string LivePixelFormat { get; set; } = LiveInfoUnavailable;

    [ObservableProperty]
    public partial string DiskSpaceInfo { get; set; } = "";

    private int _disposeState;

    private void CancelActiveFlashbackExportForDispose()
    {
        Interlocked.Increment(ref _flashbackExportOperationId);
        var exportCts = Interlocked.Exchange(ref _exportCts, null);
        CancelFlashbackExportCts(exportCts);
        if (exportCts != null)
        {
            DisposeFlashbackExportCtsBestEffort(exportCts, "viewmodel_dispose");
        }
    }

    // REVIEWED 2026-04-07: IDisposable fallback only. MainWindow.Closed calls
    // await ViewModel.DisposeAsync(); this sync path is for GC finalizer safety.
    public void Dispose()
        => _disposalController.Dispose();

    public async ValueTask DisposeAsync()
        => await _disposalController.DisposeAsync().ConfigureAwait(false);
}

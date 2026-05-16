using System;
using System.Threading;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media.Imaging;
using Sussudio.Models;
using Sussudio.Services.Preview;
using Sussudio.ViewModels;

namespace Sussudio.Controllers;

internal sealed class PreviewRendererHostControllerContext
{
    public required MainViewModel ViewModel { get; init; }
    public required DispatcherQueue DispatcherQueue { get; init; }
    public required Func<SwapChainPanel> GetPreviewSwapChainPanel { get; init; }
    public required Action<SwapChainPanel> SetPreviewSwapChainPanel { get; init; }
    public required Grid PreviewContentGrid { get; init; }
    public required Image PreviewImage { get; init; }
    public required SizeChangedEventHandler PreviewContentGridSizeChangedHandler { get; init; }
    public required SizeChangedEventHandler PreviewSwapChainPanelSizeChangedHandler { get; init; }
    public required Func<bool> IsPreviewReinitAnimating { get; init; }
    public required Action ClearPreviewReinitAnimatingForShutdown { get; init; }
    public required Func<string> GetPreviewStartupAttemptLabel { get; init; }
    public required Func<bool> IsPreviewFirstVisualConfirmed { get; init; }
    public required Action<string> ConfirmPreviewFirstVisual { get; init; }
    public required Action<string> MarkStartupFailed { get; init; }
    public required Action StopPreviewStartupWatchdog { get; init; }
    public required Action RevealPreviewUnavailablePlaceholder { get; init; }
    public required Action<string> SchedulePreviewStartupFailureStop { get; init; }
    public required Action ClearVideoFrameShadow { get; init; }
    public required Action SetupVideoFrameShadow { get; init; }
    public required Action<Visibility> SetGpuPreviewVisibility { get; init; }
    public required Action ResetPreviewSignalState { get; init; }
    public required Action ResetPreviewResizeTelemetry { get; init; }
    public required Action StopPreviewFadeInTimer { get; init; }
    public required Action ResetPreviewContentTransform { get; init; }
    public required Action UpdateVideoContentOverlays { get; init; }
    public required Action MarkPreviewRendererAttached { get; init; }
    public required Action<PreviewStartupStrategy, PreviewStartupSignalFlags> ConfigurePreviewStartupSignals { get; init; }
    public required Action<string> Log { get; init; }
}

internal sealed partial class PreviewRendererHostController
{
    private readonly PreviewRendererHostControllerContext _context;
    private SoftwareBitmapSource? _previewSource;
    private D3D11PreviewRenderer? _d3dRenderer;
    private long _previewFramesArrived;
    private long _previewFramesDisplayed;
    private long _previewFramesDropped;
    private long _previewLastPresentedTick;
    private double _previewMinPresentationIntervalMs;
    private long _lastRendererStopTick;
    private long _rendererReinitUnsafeWindows;

    public PreviewRendererHostController(PreviewRendererHostControllerContext context)
    {
        _context = context;
        _previewMinPresentationIntervalMs = PreviewRendererStartupPlanBuilder.ResolveExpectedIntervalMs(
            _context.ViewModel.SelectedFormat);
    }

    public D3D11PreviewRenderer? Renderer => _d3dRenderer;

    public bool HasD3DRenderer => _d3dRenderer != null;

    public bool IsCpuPreviewSourceAttached => _previewSource != null;

    public long FramesArrived => Interlocked.Read(ref _previewFramesArrived);

    public long FramesDisplayed => Interlocked.Read(ref _previewFramesDisplayed);

    public long FramesDropped => Interlocked.Read(ref _previewFramesDropped);

    public long LastPresentedTick => Interlocked.Read(ref _previewLastPresentedTick);

    public double PreviewMinPresentationIntervalMs => _previewMinPresentationIntervalMs;

    public long RendererReinitUnsafeWindows => Interlocked.Read(ref _rendererReinitUnsafeWindows);

    public int? PendingFrameCount => _d3dRenderer?.PendingFrameCount;

    public void OnPanelSizeChanged(double width, double height, double scale)
        => _d3dRenderer?.OnPanelSizeChanged(width, height, scale);

    public void SetHdrPassthroughEnabled(bool enabled)
        => _d3dRenderer?.SetHdrPassthroughEnabled(enabled);
}

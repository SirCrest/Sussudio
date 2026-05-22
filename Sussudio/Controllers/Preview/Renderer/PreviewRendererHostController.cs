using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
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

internal sealed class PreviewRendererHostController
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

    public void StopForShutdown()
    {
        _context.ClearPreviewReinitAnimatingForShutdown();
        _context.StopPreviewFadeInTimer();
        _context.ResetPreviewContentTransform();
        CleanupPreviewResources();
    }

    public Task StartAsync()
    {
        // Observability: detect the unsafe window where a new renderer could be allocated
        // while the prior instance still has an active swap chain (see reasoning_d3d11_preview.md).
        RecordPreviewRendererReinitUnsafeWindow(_d3dRenderer, _context.IsPreviewReinitAnimating());

        Interlocked.Exchange(ref _previewFramesArrived, 0);
        Interlocked.Exchange(ref _previewFramesDisplayed, 0);
        Interlocked.Exchange(ref _previewFramesDropped, 0);
        _context.ResetPreviewResizeTelemetry();
        Interlocked.Exchange(ref _previewLastPresentedTick, 0);
        var startupPlan = BuildPreviewRendererStartupPlan();
        _previewMinPresentationIntervalMs = startupPlan.PreviewMinPresentationIntervalMs;

        if (startupPlan.UseD3DRenderer)
        {
            StartD3DRenderer(startupPlan);
        }
        else
        {
            StartCpuRenderer();
        }

        return Task.CompletedTask;
    }

    public Task StopAsync()
    {
        CleanupPreviewResources();
        Interlocked.Exchange(ref _previewLastPresentedTick, 0);
        _previewMinPresentationIntervalMs = 1000.0 / 60.0;
        MarkPreviewRendererStopped();
        _context.Log("Preview renderer stopped.");
        return Task.CompletedTask;
    }

    public Task StopRendererForReinitTeardownAsync()
    {
        var renderer = _d3dRenderer;
        if (renderer != null)
        {
            _context.Log("PREVIEW_REINIT_RENDERER_STOP: stopping render thread before pipeline teardown");
            try
            {
                DisposeD3DPreviewRendererForReinit();
            }
            catch (TimeoutException ex)
            {
                // Render thread did not exit before its stop timeout. The renderer's
                // stop path has already logged details and the fresh attach path will
                // replace the panel surface if needed. Swallow the exception so reinit
                // can continue rather than crashing the UI thread mid-resolution-change.
                _context.Log($"PREVIEW_REINIT_RENDERER_STOP_TIMEOUT: {ex.Message}; continuing reinit with orphan render thread expected to exit shortly.");
            }
        }

        return Task.CompletedTask;
    }

    public void DisposeD3DPreviewRendererForReinit()
    {
        var renderer = _d3dRenderer;
        if (renderer == null)
        {
            return;
        }

        _context.ViewModel.SetPreviewFrameSink(null);
        renderer.FirstFrameRendered -= OnD3DRendererFirstFrameRendered;
        renderer.RenderThreadFailed -= OnD3DRendererRenderThreadFailed;
        renderer.Stop();
        renderer.RetireSharedDeviceReferenceForReinit();
        // Do not call Dispose() on the retired renderer in the reinit path.
        // Stop() has already unbound the panel and released the render-thread
        // D3D resources. Disposing the remaining shared-device COM wrapper while
        // WinUI/capture are also crossing native teardown can raise a corrupted
        // AccessViolationException. The old managed wrapper is intentionally
        // abandoned during mode switches; shutdown still disposes the active
        // renderer normally.
        _d3dRenderer = null;
    }

    private PreviewRendererStartupPlan BuildPreviewRendererStartupPlan()
        => PreviewRendererStartupPlanBuilder.Build(
            _context.ViewModel.IsPreviewing,
            _context.ViewModel.SelectedFormat,
            _context.ViewModel.IsPreviewing ? _context.ViewModel.BuildCurrentSettings() : null,
            _context.ViewModel.IsPreviewing ? _context.ViewModel.ProbeVideoSource() : null);

    private void StartCpuRenderer()
    {
        // Fallback CPU preview path: SoftwareBitmapSource -> Image (unchanged)
        _context.SetupVideoFrameShadow();
        _context.PreviewContentGrid.SizeChanged += _context.PreviewContentGridSizeChangedHandler;
        _context.ViewModel.SetPreviewFrameSink(null);
        _context.ConfigurePreviewStartupSignals(PreviewStartupStrategy.CpuSoftwareBitmap, PreviewStartupSignalFlags.FirstVisual);
        _previewSource = new SoftwareBitmapSource();
        _context.MarkPreviewRendererAttached();
        _context.PreviewImage.Source = _previewSource;
        _context.PreviewImage.Visibility = Visibility.Visible;
        _context.SetGpuPreviewVisibility(Visibility.Collapsed);
        _context.Log($"Preview renderer started (mode=CpuSoftwareBitmap, expectedIntervalMs={_previewMinPresentationIntervalMs:0.###}).");
        _context.Log($"PREVIEW_RENDERER_ATTACHED mode=CpuSoftwareBitmap attempt={_context.GetPreviewStartupAttemptLabel()}");
    }

    private void StartD3DRenderer(PreviewRendererStartupPlan startupPlan)
    {
        var rendererWidth = startupPlan.RendererWidth;
        var rendererHeight = startupPlan.RendererHeight;
        var rendererFps = startupPlan.RendererFps;
        var isHdr = startupPlan.IsHdr;

        var replacingReinitSurface = _d3dRenderer != null && _context.IsPreviewReinitAnimating();
        if (replacingReinitSurface)
        {
            // Reinit can switch SDR/HDR format, resolution, or frame cadence.
            // A previously bound WinUI SwapChainPanel can keep native DXGI/COM
            // state tied to the old swap chain, so pair the fresh renderer with
            // a fresh panel surface instead of rebinding to the old one.
            DisposeD3DPreviewRendererForReinit();
        }

        var renderer = CreateFreshD3DPreviewRenderer(replacingReinitSurface);

        renderer.SetExpectedFrameRate(rendererFps);

        if (!replacingReinitSurface)
        {
            // Wire SizeChanged and make panel visible BEFORE starting the render
            // thread so the renderer has the panel's pixel dimensions from the start.
            _context.SetupVideoFrameShadow();
            _context.PreviewContentGrid.SizeChanged += _context.PreviewContentGridSizeChangedHandler;
        }

        var previewSwapChainPanel = _context.GetPreviewSwapChainPanel();
        previewSwapChainPanel.SizeChanged += _context.PreviewSwapChainPanelSizeChangedHandler;
        _context.SetGpuPreviewVisibility(Visibility.Visible);
        _context.PreviewImage.Visibility = Visibility.Collapsed;

        // Pre-seed panel size and renderer dimensions.
        // Force layout so the container has ActualWidth/Height, then
        // UpdateVideoContentOverlays sets the panel to fitW x fitH.
        previewSwapChainPanel.UpdateLayout();
        _context.UpdateVideoContentOverlays();
        previewSwapChainPanel.UpdateLayout();
        var panelW = previewSwapChainPanel.ActualWidth;
        var panelH = previewSwapChainPanel.ActualHeight;
        if (panelW > 0 && panelH > 0)
        {
            var scale = previewSwapChainPanel.XamlRoot?.RasterizationScale ?? 1.0;
            renderer.OnPanelSizeChanged(panelW, panelH, scale);
        }

        renderer.Start(rendererWidth, rendererHeight, rendererFps, isHdr);
        if (isHdr && _context.ViewModel.IsTrueHdrPreviewEnabled)
        {
            renderer.SetHdrPassthroughEnabled(true);
        }

        _context.ViewModel.SetPreviewFrameSink(_d3dRenderer);
        _context.ConfigurePreviewStartupSignals(
            PreviewStartupStrategy.D3D11VideoProcessor,
            PreviewStartupSignalFlags.FirstVisual);
        _context.MarkPreviewRendererAttached();

        _context.Log("Preview renderer started (mode=D3D11VideoProcessor).");
        _context.Log($"PREVIEW_RENDERER_ATTACHED mode=D3D11VideoProcessor attempt={_context.GetPreviewStartupAttemptLabel()}");
    }

    private void CleanupPreviewResources()
    {
        // Clean up composition shadow
        _context.ClearVideoFrameShadow();

        // Clean up D3D11 preview
        _context.PreviewContentGrid.SizeChanged -= _context.PreviewContentGridSizeChangedHandler;
        var renderer = _d3dRenderer;
        _d3dRenderer = null;
        if (renderer != null)
        {
            _context.GetPreviewSwapChainPanel().SizeChanged -= _context.PreviewSwapChainPanelSizeChangedHandler;
            renderer.FirstFrameRendered -= OnD3DRendererFirstFrameRendered;
            renderer.RenderThreadFailed -= OnD3DRendererRenderThreadFailed;
            renderer.Stop();
            renderer.Dispose();
        }

        _context.ViewModel.SetPreviewFrameSink(null);
        _context.SetGpuPreviewVisibility(Visibility.Collapsed);
        _context.ResetPreviewSignalState();

        // Clean up CPU preview
        _context.PreviewImage.Source = null;
        _context.PreviewImage.Visibility = Visibility.Collapsed;
        _previewSource = null;
    }

    private void OnD3DRendererFirstFrameRendered()
    {
        _context.Log($"PREVIEW_D3D11_FIRST_FRAME attempt={_context.GetPreviewStartupAttemptLabel()}");
        _context.ConfirmPreviewFirstVisual("D3D11FirstFrame");
    }

    private void OnD3DRendererRenderThreadFailed(string reason)
    {
        _context.Log($"PREVIEW_D3D11_RENDER_THREAD_FAILED attempt={_context.GetPreviewStartupAttemptLabel()} reason={reason}");
        if (!_context.ViewModel.IsPreviewing || _context.IsPreviewFirstVisualConfirmed())
        {
            return;
        }

        var failureReason = $"d3d-render-thread-failed:{reason}";
        _context.MarkStartupFailed(failureReason);
        _context.StopPreviewStartupWatchdog();
        _context.RevealPreviewUnavailablePlaceholder();
        _context.SchedulePreviewStartupFailureStop(failureReason);
    }

    private D3D11PreviewRenderer CreateFreshD3DPreviewRenderer(bool replaceSwapChainSurface)
    {
        if (replaceSwapChainSurface)
        {
            ReplacePreviewSwapChainPanelSurface();
        }

        var renderer = new D3D11PreviewRenderer(_context.GetPreviewSwapChainPanel(), _context.DispatcherQueue);
        renderer.FirstFrameRendered += OnD3DRendererFirstFrameRendered;
        renderer.RenderThreadFailed += OnD3DRendererRenderThreadFailed;
        _d3dRenderer = renderer;
        return renderer;
    }

    private void RecordPreviewRendererReinitUnsafeWindow(D3D11PreviewRenderer? previousRenderer, bool reinitAnimating)
    {
        if (previousRenderer == null || reinitAnimating)
        {
            return;
        }

        var previousSwapChainAddress = previousRenderer.SwapChainAddress;
        var previousIsRendering = previousRenderer.IsRendering;
        var timeSinceStopMs = Environment.TickCount64 - Interlocked.Read(ref _lastRendererStopTick);
        Interlocked.Increment(ref _rendererReinitUnsafeWindows);
        _context.Log($"D3D11_RENDERER_REINIT_UNSAFE_WINDOW prev_present={previousSwapChainAddress} reinit_animating={reinitAnimating} prev_rendering={previousIsRendering} time_since_last_stop_ms={timeSinceStopMs}");
    }

    private void MarkPreviewRendererStopped()
    {
        Interlocked.Exchange(ref _lastRendererStopTick, Environment.TickCount64);
    }

    private void ReplacePreviewSwapChainPanelSurface()
    {
        var oldPanel = _context.GetPreviewSwapChainPanel();
        oldPanel.SizeChanged -= _context.PreviewSwapChainPanelSizeChangedHandler;

        var automationId = AutomationProperties.GetAutomationId(oldPanel);
        var freshPanel = new SwapChainPanel
        {
            Width = oldPanel.Width,
            Height = oldPanel.Height,
            MinWidth = oldPanel.MinWidth,
            MinHeight = oldPanel.MinHeight,
            MaxWidth = oldPanel.MaxWidth,
            MaxHeight = oldPanel.MaxHeight,
            Visibility = oldPanel.Visibility,
            HorizontalAlignment = oldPanel.HorizontalAlignment,
            VerticalAlignment = oldPanel.VerticalAlignment,
            Margin = oldPanel.Margin,
            Opacity = oldPanel.Opacity,
            RenderTransform = oldPanel.RenderTransform,
            RenderTransformOrigin = oldPanel.RenderTransformOrigin,
            IsHitTestVisible = oldPanel.IsHitTestVisible
        };
        AutomationProperties.SetAutomationId(freshPanel, automationId);

        var childIndex = _context.PreviewContentGrid.Children.IndexOf(oldPanel);
        if (childIndex >= 0)
        {
            _context.PreviewContentGrid.Children.RemoveAt(childIndex);
            _context.PreviewContentGrid.Children.Insert(childIndex, freshPanel);
        }
        else
        {
            _context.PreviewContentGrid.Children.Add(freshPanel);
        }

        _context.SetPreviewSwapChainPanel(freshPanel);
        _context.Log("PREVIEW_REINIT_SWAPCHAIN_PANEL_REPLACED: created fresh WinUI SwapChainPanel surface");
    }
}

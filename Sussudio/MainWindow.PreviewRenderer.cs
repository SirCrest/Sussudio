using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Hosting;
using Sussudio.Models;
using Sussudio.Services.Capture;
using Sussudio.Services.Preview;

namespace Sussudio;

// Preview renderer host wiring. This partial owns SwapChainPanel attachment,
// renderer sizing, and the bridge between live/Flashback frames and D3D11.
public sealed partial class MainWindow
{
    private SoftwareBitmapSource? _previewSource;
    private D3D11PreviewRenderer? _d3dRenderer;
    private long _previewFramesArrived;
    private long _previewFramesDisplayed;
    private long _previewFramesDropped;
    private long _previewLastPresentedTick;
    private long _lastRendererStopTick;
    private long _rendererReinitUnsafeWindows;
    private double _previewMinPresentationIntervalMs;

    public long RendererReinitUnsafeWindows => Interlocked.Read(ref _rendererReinitUnsafeWindows);

    private double ResolvePreviewExpectedIntervalMs()
    {
        var sourceFps = ViewModel.SelectedFormat?.FrameRateExact ?? 0;
        if (sourceFps <= 0)
        {
            sourceFps = 60;
        }

        return Math.Max(1.0, 1000.0 / sourceFps);
    }

    private void OnD3DRendererFirstFrameRendered()
    {
        Logger.Log($"PREVIEW_D3D11_FIRST_FRAME attempt={_previewStartupAttemptId ?? "none"}");
        ConfirmPreviewFirstVisual("D3D11FirstFrame");
    }
    private void OnD3DRendererRenderThreadFailed(string reason)
    {
        Logger.Log($"PREVIEW_D3D11_RENDER_THREAD_FAILED attempt={_previewStartupAttemptId ?? "none"} reason={reason}");
        if (!ViewModel.IsPreviewing || _previewFirstVisualConfirmed)
        {
            return;
        }

        var failureReason = $"d3d-render-thread-failed:{reason}";
        SetPreviewStartupState(PreviewStartupState.Failed, failureReason);
        StopPreviewStartupWatchdog();
        RevealPreviewUnavailablePlaceholder();
        SchedulePreviewStartupFailureStop(failureReason);
    }
    private D3D11PreviewRenderer CreateFreshD3DPreviewRenderer(bool replaceSwapChainSurface)
    {
        if (replaceSwapChainSurface)
        {
            ReplacePreviewSwapChainPanelSurface();
        }

        var renderer = new D3D11PreviewRenderer(PreviewSwapChainPanel, _dispatcherQueue);
        renderer.FirstFrameRendered += OnD3DRendererFirstFrameRendered;
        renderer.RenderThreadFailed += OnD3DRendererRenderThreadFailed;
        _d3dRenderer = renderer;
        return renderer;
    }
    private void DisposeD3DPreviewRendererForReinit()
    {
        var renderer = _d3dRenderer;
        if (renderer == null)
        {
            return;
        }

        ViewModel.SetPreviewFrameSink(null);
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
    private void ReplacePreviewSwapChainPanelSurface()
    {
        var oldPanel = PreviewSwapChainPanel;
        oldPanel.SizeChanged -= OnPreviewSwapChainPanelSizeChanged;

        var automationId = Microsoft.UI.Xaml.Automation.AutomationProperties.GetAutomationId(oldPanel);
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
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetAutomationId(freshPanel, automationId);

        var childIndex = PreviewContentGrid.Children.IndexOf(oldPanel);
        if (childIndex >= 0)
        {
            PreviewContentGrid.Children.RemoveAt(childIndex);
            PreviewContentGrid.Children.Insert(childIndex, freshPanel);
        }
        else
        {
            PreviewContentGrid.Children.Add(freshPanel);
        }

        PreviewSwapChainPanel = freshPanel;
        Logger.Log("PREVIEW_REINIT_SWAPCHAIN_PANEL_REPLACED: created fresh WinUI SwapChainPanel surface");
    }
    private void CleanupPreviewResources()
    {
        // Clean up composition shadow
        _videoShadowVisual = null;
        ElementCompositionPreview.SetElementChildVisual(VideoShadowHost, null);

        // Clean up D3D11 preview
        PreviewContentGrid.SizeChanged -= OnPreviewContentGridSizeChanged;
        var renderer = _d3dRenderer;
        _d3dRenderer = null;
        if (renderer != null)
        {
            PreviewSwapChainPanel.SizeChanged -= OnPreviewSwapChainPanelSizeChanged;
            renderer.FirstFrameRendered -= OnD3DRendererFirstFrameRendered;
            renderer.RenderThreadFailed -= OnD3DRendererRenderThreadFailed;
            renderer.Stop();
            renderer.Dispose();
        }
        ViewModel.SetPreviewFrameSink(null);
        SetGpuPreviewVisibility(Visibility.Collapsed);
        ResetPreviewSignalState();

        // Clean up CPU preview
        PreviewImage.Source = null;
        PreviewImage.Visibility = Visibility.Collapsed;
        _previewSource = null;
    }
    private void StopPreviewForShutdown()
    {
        _isPreviewReinitAnimating = false;
        Logger.Log($"D3D11_RENDERER_REINIT_FLAG flag=false caller={nameof(StopPreviewForShutdown)}");
        StopPreviewFadeInTimer();
        ResetPreviewContentTransform();
        CleanupPreviewResources();
    }
    private Task StartPreviewRendererAsync()
    {
        // Observability: detect the unsafe window where a new renderer could be allocated
        // while the prior instance still has an active swap chain (see reasoning_d3d11_preview.md).
        var prevRenderer = _d3dRenderer;
        var reinitAnimating = _isPreviewReinitAnimating;
        if (prevRenderer != null && !reinitAnimating)
        {
            var prevSwapChainAddress = prevRenderer.SwapChainAddress;
            var prevIsRendering = prevRenderer.IsRendering;
            var timeSinceStopMs = Environment.TickCount64 - Interlocked.Read(ref _lastRendererStopTick);
            Interlocked.Increment(ref _rendererReinitUnsafeWindows);
            Logger.Log($"D3D11_RENDERER_REINIT_UNSAFE_WINDOW prev_present={prevSwapChainAddress} reinit_animating={reinitAnimating} prev_rendering={prevIsRendering} time_since_last_stop_ms={timeSinceStopMs}");
        }

        _previewFramesArrived = 0;
        _previewFramesDisplayed = 0;
        _previewFramesDropped = 0;
        ResetPreviewResizeTelemetry();
        _previewLastPresentedTick = 0;
        _previewMinPresentationIntervalMs = ResolvePreviewExpectedIntervalMs();

        var useD3dRenderer = ViewModel.IsPreviewing;
        if (useD3dRenderer)
        {
            var settings = ViewModel.BuildCurrentSettings();
            var sourceProbe = ViewModel.ProbeVideoSource();
            var isHdr = settings != null && HdrOutputPolicy.IsEnabled(settings);
            var width = (int)(settings?.Width ?? 1920);
            var height = (int)(settings?.Height ?? 1080);
            var fps = settings?.FrameRate ?? 60.0;
            var negotiatedWidth = sourceProbe.SessionActive ? sourceProbe.CurrentWidth : 0;
            var negotiatedHeight = sourceProbe.SessionActive ? sourceProbe.CurrentHeight : 0;
            var negotiatedFps = sourceProbe.SessionActive ? sourceProbe.CurrentFrameRate : 0.0;
            var rendererWidth = negotiatedWidth > 0 ? negotiatedWidth : width;
            var rendererHeight = negotiatedHeight > 0 ? negotiatedHeight : height;
            var rendererFps = negotiatedFps > 0 ? negotiatedFps : fps;
            _previewMinPresentationIntervalMs = Math.Max(1.0, 1000.0 / rendererFps);

            var replacingReinitSurface = _d3dRenderer != null && _isPreviewReinitAnimating;
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
                SetupVideoFrameShadow();
                PreviewContentGrid.SizeChanged += OnPreviewContentGridSizeChanged;
            }
            PreviewSwapChainPanel.SizeChanged += OnPreviewSwapChainPanelSizeChanged;
            SetGpuPreviewVisibility(Visibility.Visible);
            PreviewImage.Visibility = Visibility.Collapsed;

            // Pre-seed panel size and renderer dimensions.
            // Force layout so the container has ActualWidth/Height, then
            // UpdateVideoContentOverlays sets the panel to fitW x fitH.
            PreviewSwapChainPanel.UpdateLayout();
            UpdateVideoContentOverlays();
            PreviewSwapChainPanel.UpdateLayout();
            var panelW = PreviewSwapChainPanel.ActualWidth;
            var panelH = PreviewSwapChainPanel.ActualHeight;
            if (panelW > 0 && panelH > 0)
            {
                var scale = PreviewSwapChainPanel.XamlRoot?.RasterizationScale ?? 1.0;
                renderer.OnPanelSizeChanged(panelW, panelH, scale);
            }

            renderer.Start(rendererWidth, rendererHeight, rendererFps, isHdr);
            if (isHdr && ViewModel.IsTrueHdrPreviewEnabled)
            {
                renderer.SetHdrPassthroughEnabled(true);
            }

            ViewModel.SetPreviewFrameSink(_d3dRenderer);
            _previewStartupExpectGpuDualSignals = false; // D3D renderer uses FirstFrameRendered, not MediaPlayer signals
            ConfigurePreviewStartupSignals(
                PreviewStartupStrategy.D3D11VideoProcessor,
                PreviewStartupSignalFlags.FirstVisual);
            _previewRendererAttachedUtc = DateTimeOffset.UtcNow;

            Logger.Log("Preview renderer started (mode=D3D11VideoProcessor).");
            Logger.Log($"PREVIEW_RENDERER_ATTACHED mode=D3D11VideoProcessor attempt={_previewStartupAttemptId ?? "none"}");
        }
        else
        {
            // Fallback CPU preview path: SoftwareBitmapSource -> Image (unchanged)
            SetupVideoFrameShadow();
            PreviewContentGrid.SizeChanged += OnPreviewContentGridSizeChanged;
            ViewModel.SetPreviewFrameSink(null);
            _previewStartupExpectGpuDualSignals = false;
            ConfigurePreviewStartupSignals(PreviewStartupStrategy.CpuSoftwareBitmap, PreviewStartupSignalFlags.FirstVisual);
            _previewSource = new SoftwareBitmapSource();
            _previewRendererAttachedUtc = DateTimeOffset.UtcNow;
            PreviewImage.Source = _previewSource;
            PreviewImage.Visibility = Visibility.Visible;
            SetGpuPreviewVisibility(Visibility.Collapsed);
            Logger.Log($"Preview renderer started (mode=CpuSoftwareBitmap, expectedIntervalMs={_previewMinPresentationIntervalMs:0.###}).");
            Logger.Log($"PREVIEW_RENDERER_ATTACHED mode=CpuSoftwareBitmap attempt={_previewStartupAttemptId ?? "none"}");
        }

        return Task.CompletedTask;
    }
    private Task StopPreviewRendererAsync()
    {
        CleanupPreviewResources();
        _previewLastPresentedTick = 0;
        _previewMinPresentationIntervalMs = 1000.0 / 60.0;
        Interlocked.Exchange(ref _lastRendererStopTick, Environment.TickCount64);
        Logger.Log("Preview renderer stopped.");
        return Task.CompletedTask;
    }
}

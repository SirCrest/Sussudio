using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Hosting;
using Sussudio.Controllers;
using Sussudio.Models;
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
    private double _previewMinPresentationIntervalMs;

    private PreviewRendererStartupPlan BuildPreviewRendererStartupPlan()
        => PreviewRendererStartupPlanBuilder.Build(
            ViewModel.IsPreviewing,
            ViewModel.SelectedFormat,
            ViewModel.IsPreviewing ? ViewModel.BuildCurrentSettings() : null,
            ViewModel.IsPreviewing ? ViewModel.ProbeVideoSource() : null);

    private void OnD3DRendererFirstFrameRendered()
    {
        Logger.Log($"PREVIEW_D3D11_FIRST_FRAME attempt={PreviewStartupAttemptLabel}");
        ConfirmPreviewFirstVisual("D3D11FirstFrame");
    }
    private void OnD3DRendererRenderThreadFailed(string reason)
    {
        Logger.Log($"PREVIEW_D3D11_RENDER_THREAD_FAILED attempt={PreviewStartupAttemptLabel} reason={reason}");
        if (!ViewModel.IsPreviewing || IsPreviewFirstVisualConfirmed)
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
    private void CleanupPreviewResources()
    {
        // Clean up composition shadow
        ClearVideoFrameShadow();

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
        RecordPreviewRendererReinitUnsafeWindow(_d3dRenderer, _isPreviewReinitAnimating);

        _previewFramesArrived = 0;
        _previewFramesDisplayed = 0;
        _previewFramesDropped = 0;
        ResetPreviewResizeTelemetry();
        _previewLastPresentedTick = 0;
        var startupPlan = BuildPreviewRendererStartupPlan();
        _previewMinPresentationIntervalMs = startupPlan.PreviewMinPresentationIntervalMs;

        if (startupPlan.UseD3DRenderer)
        {
            var rendererWidth = startupPlan.RendererWidth;
            var rendererHeight = startupPlan.RendererHeight;
            var rendererFps = startupPlan.RendererFps;
            var isHdr = startupPlan.IsHdr;

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
            MarkPreviewRendererAttached();

            Logger.Log("Preview renderer started (mode=D3D11VideoProcessor).");
            Logger.Log($"PREVIEW_RENDERER_ATTACHED mode=D3D11VideoProcessor attempt={PreviewStartupAttemptLabel}");
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
            MarkPreviewRendererAttached();
            PreviewImage.Source = _previewSource;
            PreviewImage.Visibility = Visibility.Visible;
            SetGpuPreviewVisibility(Visibility.Collapsed);
            Logger.Log($"Preview renderer started (mode=CpuSoftwareBitmap, expectedIntervalMs={_previewMinPresentationIntervalMs:0.###}).");
            Logger.Log($"PREVIEW_RENDERER_ATTACHED mode=CpuSoftwareBitmap attempt={PreviewStartupAttemptLabel}");
        }

        return Task.CompletedTask;
    }
    private Task StopPreviewRendererAsync()
    {
        CleanupPreviewResources();
        _previewLastPresentedTick = 0;
        _previewMinPresentationIntervalMs = 1000.0 / 60.0;
        MarkPreviewRendererStopped();
        Logger.Log("Preview renderer stopped.");
        return Task.CompletedTask;
    }
}

using Microsoft.UI.Xaml;
using Sussudio.Models;
using Sussudio.Services.Preview;

namespace Sussudio.Controllers;

internal sealed partial class PreviewRendererHostController
{
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
}

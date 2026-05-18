using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Imaging;
using Sussudio.Models;

namespace Sussudio.Controllers;

internal sealed partial class PreviewRendererHostController
{
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
}

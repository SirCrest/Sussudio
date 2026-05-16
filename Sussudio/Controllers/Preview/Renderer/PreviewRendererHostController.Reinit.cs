using System;
using System.Threading;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Sussudio.Services.Preview;

namespace Sussudio.Controllers;

internal sealed partial class PreviewRendererHostController
{
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

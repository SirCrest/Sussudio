using System;
using System.Numerics;
using System.Threading;
using Vortice.DXGI;

namespace Sussudio.Services.Preview;

internal sealed partial class D3D11PreviewRenderer
{
    private int _compositionTransformDirty;
    private int _panelPixelWidth = 1;
    private int _panelPixelHeight = 1;
    private double _panelLogicalWidth = 1.0;
    private double _panelLogicalHeight = 1.0;
    private double _rasterizationScale = 1.0;

    public void OnPanelSizeChanged(double logicalWidth, double logicalHeight, double rasterizationScale)
    {
        if (logicalWidth <= 0 || logicalHeight <= 0 || rasterizationScale <= 0) return;

        Volatile.Write(ref _panelLogicalWidth, logicalWidth);
        Volatile.Write(ref _panelLogicalHeight, logicalHeight);
        var pixelWidth = Math.Max(1, (int)(logicalWidth * rasterizationScale));
        var pixelHeight = Math.Max(1, (int)(logicalHeight * rasterizationScale));

        Volatile.Write(ref _panelPixelWidth, pixelWidth);
        Volatile.Write(ref _panelPixelHeight, pixelHeight);
        Volatile.Write(ref _rasterizationScale, rasterizationScale);
        Interlocked.Exchange(ref _compositionTransformDirty, 1);
        SignalFrameReady("panel_size_changed");
        Logger.Log($"D3D11 preview resize requested width={pixelWidth} height={pixelHeight} scale={rasterizationScale}.");
    }

    private void ApplyCompositionScaleTransform(IDXGISwapChain1 swapChain)
    {
        using var swapChain2 = swapChain.QueryInterfaceOrNull<IDXGISwapChain2>();
        if (swapChain2 == null)
        {
            return;
        }

        var panelLogicalW = Volatile.Read(ref _panelLogicalWidth);
        var panelLogicalH = Volatile.Read(ref _panelLogicalHeight);
        var swapW = (double)Math.Max(1, _configuredOutputWidth);
        var swapH = (double)Math.Max(1, _configuredOutputHeight);

        if (panelLogicalW <= 0 || panelLogicalH <= 0)
        {
            swapChain2.MatrixTransform = Matrix3x2.Identity;
            return;
        }

        var uniformScale = (float)Math.Min(panelLogicalW / swapW, panelLogicalH / swapH);
        var offsetX = (float)((panelLogicalW - swapW * uniformScale) * 0.5);
        var offsetY = (float)((panelLogicalH - swapH * uniformScale) * 0.5);

        swapChain2.MatrixTransform = new Matrix3x2(
            uniformScale, 0,
            0, uniformScale,
            offsetX, offsetY);

        Logger.Log($"D3D11 preview composition transform set scale={uniformScale:F4} offset=({offsetX:F1},{offsetY:F1}) panel={panelLogicalW:F0}x{panelLogicalH:F0} swap={swapW}x{swapH}.");
    }
}

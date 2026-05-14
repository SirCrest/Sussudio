using System;
using System.Threading;
using Vortice.Direct3D11;
using Vortice.Mathematics;

namespace Sussudio.Services.Preview;

internal sealed partial class D3D11PreviewRenderer
{
    private Viewport ComputeLetterboxViewport(int sourceWidth, int sourceHeight)
    {
        var outputWidth = _configuredOutputWidth > 0
            ? _configuredOutputWidth
            : Math.Max(1, Volatile.Read(ref _startupWidth));
        var outputHeight = _configuredOutputHeight > 0
            ? _configuredOutputHeight
            : Math.Max(1, Volatile.Read(ref _startupHeight));
        var destinationRect = ComputeLetterboxRect(sourceWidth, sourceHeight, outputWidth, outputHeight);
        return new Viewport(
            destinationRect.Left,
            destinationRect.Top,
            Math.Max(1, destinationRect.Right - destinationRect.Left),
            Math.Max(1, destinationRect.Bottom - destinationRect.Top),
            0.0f,
            1.0f);
    }

    private void UpdateViewportConstantBuffer(Viewport viewport)
    {
        if (_viewportCB == null || _deviceContext == null)
        {
            return;
        }

        var mapped = _deviceContext.Map(_viewportCB, 0, MapMode.WriteDiscard);
        unsafe
        {
            var data = (float*)mapped.DataPointer;
            data[0] = viewport.X;
            data[1] = viewport.Y;
            data[2] = viewport.Width;
            data[3] = viewport.Height;
        }

        _deviceContext.Unmap(_viewportCB, 0);
        _cbArray[0] = _viewportCB;
        _deviceContext.PSSetConstantBuffers(0, 1, _cbArray);
    }

    private static Vortice.RawRect ComputeLetterboxRect(int srcWidth, int srcHeight, int dstWidth, int dstHeight)
    {
        if (srcWidth <= 0 || srcHeight <= 0 || dstWidth <= 0 || dstHeight <= 0)
        {
            return new Vortice.RawRect(0, 0, dstWidth, dstHeight);
        }

        var srcAspect = (double)srcWidth / srcHeight;
        var dstAspect = (double)dstWidth / dstHeight;

        int fitWidth, fitHeight;
        if (srcAspect > dstAspect)
        {
            // Source is wider - letterbox (bars top/bottom)
            fitWidth = dstWidth;
            fitHeight = (int)(dstWidth / srcAspect);
        }
        else
        {
            // Source is taller - pillarbox (bars left/right)
            fitHeight = dstHeight;
            fitWidth = (int)(dstHeight * srcAspect);
        }

        var x = (dstWidth - fitWidth) / 2;
        var y = (dstHeight - fitHeight) / 2;
        return new Vortice.RawRect(x, y, x + fitWidth, y + fitHeight);
    }
}

using System;
using System.Threading;
using Vortice.Direct3D11;
using Vortice.DXGI;

namespace Sussudio.Services.Preview;

internal sealed partial class D3D11PreviewRenderer
{
    private (IDXGISwapChain1 SwapChain, int PixelWidth, int PixelHeight) InitializeCompositionSwapChain(ID3D11Device device)
    {
        var factoryResult = DXGI.CreateDXGIFactory2(false, out _factory);
        if (factoryResult.Failure || _factory == null)
        {
            throw new InvalidOperationException($"CreateDXGIFactory2 failed: 0x{factoryResult.Code:X8}.");
        }

        var pixelWidth = Math.Max(1, Volatile.Read(ref _startupWidth));
        var pixelHeight = Math.Max(1, Volatile.Read(ref _startupHeight));

        var swapChainFormat = Format.B8G8R8A8_UNorm;
        _hdrCapableSwapChain = false;
        _swapChain3?.Dispose();
        _swapChain3 = null;

        if (_configuredHdr)
        {
            swapChainFormat = Format.R10G10B10A2_UNorm;
        }

        var swapChainFlags = _waitableSwapChainEnabled
            ? SwapChainFlags.FrameLatencyWaitableObject
            : SwapChainFlags.None;

        var swapChainDescription = CreateCompositionSwapChainDescription(
            pixelWidth,
            pixelHeight,
            swapChainFormat,
            swapChainFlags);

        _swapChain = _factory.CreateSwapChainForComposition(device, swapChainDescription, null);
        ConfigureFrameLatencyWaitableObject();

        if (_configuredHdr)
        {
            EnsureHdrCapableSwapChainOrFallbackToSdr(device, pixelWidth, pixelHeight, swapChainFlags);
        }

        _configuredOutputWidth = pixelWidth;
        _configuredOutputHeight = pixelHeight;
        return (_swapChain, pixelWidth, pixelHeight);
    }

    private void EnsureHdrCapableSwapChainOrFallbackToSdr(
        ID3D11Device device,
        int pixelWidth,
        int pixelHeight,
        SwapChainFlags swapChainFlags)
    {
        _swapChain3 = _swapChain!.QueryInterfaceOrNull<IDXGISwapChain3>();
        if (_swapChain3 != null)
        {
            var srgbSupport = _swapChain3.CheckColorSpaceSupport(ColorSpaceType.RgbFullG22NoneP709);
            var hdr10Support = _swapChain3.CheckColorSpaceSupport(ColorSpaceType.RgbFullG2084NoneP2020);

            var srgbOk = (srgbSupport & SwapChainColorSpaceSupportFlags.Present) != 0;
            var hdr10Ok = (hdr10Support & SwapChainColorSpaceSupportFlags.Present) != 0;

            if (srgbOk && hdr10Ok)
            {
                _hdrCapableSwapChain = true;
                var wantHdr = Volatile.Read(ref _hdrPassthroughEnabled) != 0;
                var initialColorSpace = wantHdr
                    ? ColorSpaceType.RgbFullG2084NoneP2020
                    : ColorSpaceType.RgbFullG22NoneP709;
                _swapChain3.SetColorSpace1(initialColorSpace);
                _outputColorSpaceLabel = wantHdr ? "HDR10-PQ (BT.2020)" : "sRGB (BT.709)";
                Interlocked.Exchange(ref _swapChainColorSpaceDirty, 0);
                Logger.Log($"D3D11 preview HDR-capable swap chain: srgb={srgbOk} hdr10={hdr10Ok} initial={initialColorSpace}.");
                return;
            }

            Logger.Log($"D3D11 preview HDR color space check: srgb={srgbOk}({srgbSupport}) hdr10={hdr10Ok}({hdr10Support}). Falling back to B8G8R8A8.");
            RecreateSdrCompositionSwapChain(device, pixelWidth, pixelHeight, swapChainFlags);
            return;
        }

        Logger.Log("D3D11 preview IDXGISwapChain3 unavailable - HDR passthrough not supported.");
        RecreateSdrCompositionSwapChain(device, pixelWidth, pixelHeight, swapChainFlags);
    }

    private void RecreateSdrCompositionSwapChain(
        ID3D11Device device,
        int pixelWidth,
        int pixelHeight,
        SwapChainFlags swapChainFlags)
    {
        _swapChain3?.Dispose();
        _swapChain3 = null;
        _swapChain2?.Dispose();
        _swapChain2 = null;
        _frameLatencyWaitHandle = IntPtr.Zero;
        _swapChain!.Dispose();

        var fallbackDescription = CreateCompositionSwapChainDescription(
            pixelWidth,
            pixelHeight,
            Format.B8G8R8A8_UNorm,
            swapChainFlags);
        _swapChain = _factory!.CreateSwapChainForComposition(device, fallbackDescription, null);
        ConfigureFrameLatencyWaitableObject();
    }

    private SwapChainDescription1 CreateCompositionSwapChainDescription(
        int pixelWidth,
        int pixelHeight,
        Format format,
        SwapChainFlags swapChainFlags)
        => new(
            (uint)pixelWidth,
            (uint)pixelHeight,
            format,
            false,
            Usage.RenderTargetOutput,
            (uint)_swapChainBufferCount,
            Scaling.Stretch,
            SwapEffect.FlipSequential,
            AlphaMode.Ignore,
            swapChainFlags);
}

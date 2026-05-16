using System;
using System.Threading;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;

namespace Sussudio.Services.Preview;

internal sealed partial class D3D11PreviewRenderer
{
    private void InitializeD3D()
    {
        CleanupD3DResources();

        var sharedDeviceActive = TryInitializeWithSharedDevice(out var featureLevel);
        if (!sharedDeviceActive)
        {
            CreateRendererOwnedDevice(out featureLevel);
        }

        if (_device == null || _deviceContext == null)
        {
            throw new InvalidOperationException("D3D11 device initialization did not produce a valid device/context.");
        }

        var device = _device;
        var deviceContext = _deviceContext;
        _device3?.Dispose();
        _device3 = device.QueryInterfaceOrNull<ID3D11Device3>();
        Interlocked.Exchange(ref _sharedDeviceActive, sharedDeviceActive ? 1 : 0);

        _multithread = device.QueryInterfaceOrNull<ID3D11Multithread>();
        _multithread?.SetMultithreadProtected(true);

        // Keep the compositor queue shallow. This defaults to 2 for latency,
        // but is env-tunable while we measure DWM pacing behavior.
        using var dxgiDevice1 = device.QueryInterfaceOrNull<IDXGIDevice1>();
        dxgiDevice1?.SetMaximumFrameLatency((uint)_dxgiMaxFrameLatency);

        _videoDevice = device.QueryInterfaceOrNull<ID3D11VideoDevice>();
        _videoContext = deviceContext.QueryInterfaceOrNull<ID3D11VideoContext>();
        _videoContext1 = deviceContext.QueryInterfaceOrNull<ID3D11VideoContext1>();
        if (_videoDevice == null || _videoContext == null || _videoContext1 == null)
        {
            throw new InvalidOperationException("D3D11 video interfaces are unavailable.");
        }

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

        var swapChainDescription = new SwapChainDescription1(
            (uint)pixelWidth,
            (uint)pixelHeight,
            swapChainFormat,
            false,
            Usage.RenderTargetOutput,
            (uint)_swapChainBufferCount,
            Scaling.Stretch,
            SwapEffect.FlipSequential,
            AlphaMode.Ignore,
            swapChainFlags);

        _swapChain = _factory.CreateSwapChainForComposition(device, swapChainDescription, null);
        ConfigureFrameLatencyWaitableObject();
        if (_configuredHdr)
        {
            _swapChain3 = _swapChain.QueryInterfaceOrNull<IDXGISwapChain3>();
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
                }
                else
                {
                    Logger.Log($"D3D11 preview HDR color space check: srgb={srgbOk}({srgbSupport}) hdr10={hdr10Ok}({hdr10Support}). Falling back to B8G8R8A8.");
                    _swapChain3.Dispose();
                    _swapChain3 = null;
                    _swapChain2?.Dispose();
                    _swapChain2 = null;
                    _frameLatencyWaitHandle = IntPtr.Zero;
                    _swapChain.Dispose();
                    swapChainDescription = new SwapChainDescription1(
                        (uint)pixelWidth,
                        (uint)pixelHeight,
                        Format.B8G8R8A8_UNorm,
                        false,
                        Usage.RenderTargetOutput,
                        (uint)_swapChainBufferCount,
                        Scaling.Stretch,
                        SwapEffect.FlipSequential,
                        AlphaMode.Ignore,
                        swapChainFlags);
                    _swapChain = _factory.CreateSwapChainForComposition(device, swapChainDescription, null);
                    ConfigureFrameLatencyWaitableObject();
                }
            }
            else
            {
                Logger.Log("D3D11 preview IDXGISwapChain3 unavailable - HDR passthrough not supported.");
                _swapChain2?.Dispose();
                _swapChain2 = null;
                _frameLatencyWaitHandle = IntPtr.Zero;
                _swapChain.Dispose();
                swapChainDescription = new SwapChainDescription1(
                    (uint)pixelWidth,
                    (uint)pixelHeight,
                    Format.B8G8R8A8_UNorm,
                    false,
                    Usage.RenderTargetOutput,
                    (uint)_swapChainBufferCount,
                    Scaling.Stretch,
                    SwapEffect.FlipSequential,
                    AlphaMode.Ignore,
                    swapChainFlags);
                _swapChain = _factory.CreateSwapChainForComposition(device, swapChainDescription, null);
                ConfigureFrameLatencyWaitableObject();
            }
        }

        _configuredOutputWidth = pixelWidth;
        _configuredOutputHeight = pixelHeight;
        ConfigureMediaPresentDuration();
        ApplyCompositionScaleTransform(_swapChain);
        BindSwapChainToPanel(_swapChain);
        CompileTonemapShaders();

        Logger.Log($"D3D11 preview device created featureLevel={featureLevel} shared={sharedDeviceActive}.");
        Logger.Log($"D3D11 preview swap chain created width={pixelWidth} height={pixelHeight} buffers={_swapChainBufferCount} renderQueue={_maxPendingFrames} sync={_presentSyncInterval} latency={_dxgiMaxFrameLatency} waitable={_waitableSwapChainEnabled}.");
    }

    private void ConfigureMediaPresentDuration()
    {
        if (!_mediaPresentDurationEnabled || _swapChain == null)
        {
            return;
        }

        using var mediaSwapChain = _swapChain.QueryInterfaceOrNull<IDXGISwapChainMedia>();
        if (mediaSwapChain == null)
        {
            Logger.Log("D3D11 preview media present duration unavailable: IDXGISwapChainMedia not supported.");
            return;
        }

        var fps = Math.Max(1.0, _startupFps);
        var desiredDuration = (uint)Math.Max(1, (int)Math.Round(10_000_000.0 / fps));
        try
        {
            mediaSwapChain.CheckPresentDurationSupport(
                desiredDuration,
                out var closestSmaller,
                out var closestLarger);
            Logger.Log(
                $"D3D11 preview media present duration support desired={desiredDuration} " +
                $"smaller={closestSmaller} larger={closestLarger}");

            mediaSwapChain.SetPresentDuration(desiredDuration);
            Logger.Log($"D3D11 preview media present duration set desired={desiredDuration} fps={fps:0.###}");
        }
        catch (Exception ex)
        {
            Logger.Log($"D3D11 preview media present duration failed: {ex.GetType().Name} hr=0x{ex.HResult:X8} msg={ex.Message}");
        }
    }

    private void CreateRendererOwnedDevice(out FeatureLevel featureLevel)
    {
        var featureLevels = new[] { FeatureLevel.Level_11_0 };
        var flags = DeviceCreationFlags.BgraSupport | DeviceCreationFlags.VideoSupport;

        var result = D3D11.D3D11CreateDevice(
            adapter: null,
            DriverType.Hardware,
            flags,
            featureLevels,
            out _device,
            out featureLevel,
            out _deviceContext);

        if (result.Failure)
        {
            Logger.Log($"D3D11 hardware device creation failed: 0x{result.Code:X8}. Falling back to WARP.");
            result = D3D11.D3D11CreateDevice(
                adapter: null,
                DriverType.Warp,
                flags,
                featureLevels,
                out _device,
                out featureLevel,
                out _deviceContext);
        }

        if (result.Failure || _device == null || _deviceContext == null)
        {
            throw new InvalidOperationException($"D3D11CreateDevice failed: 0x{result.Code:X8}.");
        }
    }
}

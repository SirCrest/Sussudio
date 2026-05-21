using System;
using System.Threading;
using Vortice.Direct3D11;
using Vortice.DXGI;

namespace Sussudio.Services.Preview;

internal sealed partial class D3D11PreviewRenderer
{
    private ID3D11Device? _device;
    private ID3D11DeviceContext? _deviceContext;
    private ID3D11Device3? _device3;
    private ID3D11Multithread? _multithread;
    private ID3D11VideoDevice? _videoDevice;
    private ID3D11VideoContext? _videoContext;
    private ID3D11VideoContext1? _videoContext1;
    private IDXGIFactory2? _factory;
    private IDXGISwapChain1? _swapChain;
    private IDXGISwapChain2? _swapChain2;
    private IDXGISwapChain3? _swapChain3;
    private long _swapChainAddress;
    private ID3D11Texture2D? _swapChainBackBuffer;
    private ID3D11RenderTargetView? _swapChainRTV;
    private ID3D11VideoProcessorEnumerator? _videoProcessorEnumerator;
    private ID3D11VideoProcessor? _videoProcessor;
    private ID3D11VideoProcessorOutputView? _outputView;

    private void CleanupD3DResources()
    {
        DisposeProcessorResources();
        DisposeFrameCaptureStagingResources();
        DisposeInputTextureResources();

        // Stop() unbinds the panel before waking the render thread, while the
        // swap chain is still alive. Cleanup can then release the DXGI objects
        // without leaving SwapChainPanel holding a stale native reference.
        Interlocked.CompareExchange(ref _swapChainBound, 0, 1);
        Interlocked.Exchange(ref _swapChainAddress, 0);

        _swapChain3?.Dispose();
        _swapChain3 = null;
        _swapChain2?.Dispose();
        _swapChain2 = null;
        _frameLatencyWaitHandle = IntPtr.Zero;
        _swapChain?.Dispose();
        _swapChain = null;
        _factory?.Dispose();
        _factory = null;
        _videoContext1?.Dispose();
        _videoContext1 = null;
        _videoContext?.Dispose();
        _videoContext = null;
        _videoDevice?.Dispose();
        _videoDevice = null;
        DisposeShaderPipelineResources();
        _multithread?.Dispose();
        _multithread = null;
        _device3?.Dispose();
        _device3 = null;
        _deviceContext?.Dispose();
        _deviceContext = null;
        _device?.Dispose();
        _device = null;

        _configuredInputWidth = 0;
        _configuredInputHeight = 0;
        _configuredOutputWidth = 0;
        _configuredOutputHeight = 0;
        _configuredInputFormat = Format.Unknown;
        _hdrCapableSwapChain = false;
        Interlocked.Exchange(ref _sharedDeviceActive, 0);
    }

}

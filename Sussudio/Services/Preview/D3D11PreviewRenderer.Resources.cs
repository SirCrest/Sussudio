using System;
using System.Threading;
using Vortice.Direct3D11;
using Vortice.DXGI;
using Vortice.Mathematics;

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

    private void EnsurePipeline(int width, int height, bool isHdr, bool useExternalTexture)
    {
        if (_swapChain == null || _videoDevice == null || _videoContext == null || _videoContext1 == null)
        {
            throw new InvalidOperationException("D3D11 preview pipeline is not initialized.");
        }

        // Keep the internal render target aligned with the source-sized swap chain.
        var outputWidth = _configuredOutputWidth > 0
            ? _configuredOutputWidth
            : Math.Max(1, Volatile.Read(ref _startupWidth));
        var outputHeight = _configuredOutputHeight > 0
            ? _configuredOutputHeight
            : Math.Max(1, Volatile.Read(ref _startupHeight));
        var needRecreate = _videoProcessor == null ||
                           _videoProcessorEnumerator == null ||
                           _configuredInputWidth != width ||
                           _configuredInputHeight != height ||
                           _configuredHdr != isHdr;

        if (needRecreate)
        {
            DisposeProcessorResources();

            var fps = Math.Max(1.0, _startupFps);
            var fpsNum = (uint)Math.Max(1, (int)Math.Round(fps * 1000.0));
            var frameRate = new Rational(fpsNum, 1000u);
            var contentDescription = new VideoProcessorContentDescription
            {
                InputFrameFormat = VideoFrameFormat.Progressive,
                InputFrameRate = frameRate,
                InputWidth = (uint)width,
                InputHeight = (uint)height,
                OutputFrameRate = frameRate,
                OutputWidth = (uint)outputWidth,
                OutputHeight = (uint)outputHeight,
                Usage = VideoUsage.PlaybackNormal
            };

            _videoProcessorEnumerator = _videoDevice.CreateVideoProcessorEnumerator(contentDescription);
            _videoProcessor = _videoDevice.CreateVideoProcessor(_videoProcessorEnumerator, 0);
            RecreateOutputView();

            var sourceRect = new Vortice.RawRect(0, 0, width, height);
            var destinationRect = ComputeLetterboxRect(width, height, outputWidth, outputHeight);
            var outputTargetRect = new Vortice.RawRect(0, 0, outputWidth, outputHeight);
            _videoContext.VideoProcessorSetStreamFrameFormat(_videoProcessor, 0, VideoFrameFormat.Progressive);
            _videoContext.VideoProcessorSetStreamAutoProcessingMode(_videoProcessor, 0, false);
            _videoContext.VideoProcessorSetStreamSourceRect(_videoProcessor, 0, true, sourceRect);
            _videoContext.VideoProcessorSetStreamDestRect(_videoProcessor, 0, true, destinationRect);
            _videoContext.VideoProcessorSetOutputTargetRect(_videoProcessor, true, outputTargetRect);
            _videoContext.VideoProcessorSetOutputBackgroundColor(_videoProcessor, false, new VideoColor());
            ApplyColorSpaces(isHdr);

            _configuredInputWidth = width;
            _configuredInputHeight = height;
            _configuredHdr = isHdr;

            Logger.Log($"D3D11 video processor created input={width}x{height} output={outputWidth}x{outputHeight} hdr={isHdr}.");
        }

        if (!useExternalTexture)
        {
            EnsureInputResources(width, height, isHdr);
        }
    }

    private void EnsureSwapChainRTV()
    {
        if (_device == null || _swapChain == null)
        {
            throw new InvalidOperationException("D3D11 preview swap chain render target state is unavailable.");
        }

        if (_swapChainBackBuffer == null)
        {
            _swapChainBackBuffer = _swapChain.GetBuffer<ID3D11Texture2D>(0);
        }

        if (_swapChainRTV == null)
        {
            _swapChainRTV = _device.CreateRenderTargetView(_swapChainBackBuffer, null);
        }
    }

    private void RecreateOutputView()
    {
        if (_swapChain == null || _videoDevice == null || _videoProcessorEnumerator == null || _device == null)
        {
            throw new InvalidOperationException("D3D11 output view recreation requires swap chain and video enumerator.");
        }

        _outputView?.Dispose();
        _outputView = null;
        _swapChainRTV?.Dispose();
        _swapChainRTV = null;
        _swapChainBackBuffer?.Dispose();
        _swapChainBackBuffer = null;

        _swapChainBackBuffer = _swapChain.GetBuffer<ID3D11Texture2D>(0);
        EnsureSwapChainRTV();
        var outputViewDescription = new VideoProcessorOutputViewDescription
        {
            ViewDimension = VideoProcessorOutputViewDimension.Texture2D,
            Texture2D = new Texture2DVideoProcessorOutputView { MipSlice = 0 }
        };

        _outputView = _videoDevice.CreateVideoProcessorOutputView(_swapChainBackBuffer, _videoProcessorEnumerator, outputViewDescription);
    }

    private void ApplyColorSpaces(bool isHdr)
    {
        if (_videoContext1 == null || _videoProcessor == null) return;

        var fullRange = Volatile.Read(ref _fullRangeInput);
        var inputColorSpace = isHdr
            ? ColorSpaceType.YcbcrStudioG2084LeftP2020
            : fullRange
                ? ColorSpaceType.YcbcrFullG22LeftP709
                : ColorSpaceType.YcbcrStudioG22LeftP709;
        var outputColorSpace = ColorSpaceType.RgbFullG22NoneP709;

        _videoContext1.VideoProcessorSetStreamColorSpace1(_videoProcessor, 0, inputColorSpace);
        _videoContext1.VideoProcessorSetOutputColorSpace1(_videoProcessor, outputColorSpace);

        _inputColorSpaceLabel = inputColorSpace.ToString();
        _outputColorSpaceLabel = outputColorSpace.ToString();
        Logger.Log($"D3D11 preview color space input={_inputColorSpaceLabel} output={_outputColorSpaceLabel} mode=VideoProcessor.");
    }

    private void DisposeProcessorResources()
    {
        DisposeProcessorInputResources();
        DisposeNv12ShaderResourceViews();
        _outputView?.Dispose();
        _outputView = null;
        _swapChainRTV?.Dispose();
        _swapChainRTV = null;
        _swapChainBackBuffer?.Dispose();
        _swapChainBackBuffer = null;
        _videoProcessor?.Dispose();
        _videoProcessor = null;
        _videoProcessorEnumerator?.Dispose();
        _videoProcessorEnumerator = null;
    }

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

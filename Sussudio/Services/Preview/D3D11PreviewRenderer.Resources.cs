using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.UI.Dispatching;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;
using Vortice.Mathematics;

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

    private void ConfigureFrameLatencyWaitableObject()
    {
        _frameLatencyWaitHandle = IntPtr.Zero;
        _swapChain2?.Dispose();
        _swapChain2 = null;

        if (!_waitableSwapChainEnabled || _swapChain == null)
        {
            return;
        }

        _swapChain2 = _swapChain.QueryInterfaceOrNull<IDXGISwapChain2>();
        if (_swapChain2 == null)
        {
            Logger.Log("D3D11 preview waitable swap chain unavailable: IDXGISwapChain2 not supported.");
            return;
        }

        _swapChain2.MaximumFrameLatency = (uint)_dxgiMaxFrameLatency;
        _frameLatencyWaitHandle = _swapChain2.FrameLatencyWaitableObject;
        Logger.Log($"D3D11 preview waitable swap chain configured handle=0x{_frameLatencyWaitHandle.ToInt64():X} latency={_dxgiMaxFrameLatency}.");
    }

    private void WaitForFrameLatencySignal()
    {
        if (!_waitableSwapChainEnabled || _frameLatencyWaitHandle == IntPtr.Zero)
        {
            return;
        }

        var waitStart = Stopwatch.GetTimestamp();
        var result = WaitForSingleObject(_frameLatencyWaitHandle, 8);
        TrackFrameLatencyWait(result, Stopwatch.GetTimestamp() - waitStart);
        if (result != WaitObject0 && result != WaitTimeout)
        {
            Logger.Log($"D3D11 preview waitable swap chain wait returned {result}.");
        }
    }

    private bool TryInitializeWithSharedDevice(out FeatureLevel featureLevel)
    {
        featureLevel = FeatureLevel.Level_11_0;

        ID3D11Device? sharedDevice = null;
        lock (_lifecycleLock)
        {
            if (_sharedDevice == null || _sharedDevice.NativePointer == IntPtr.Zero)
            {
                return false;
            }

            Marshal.AddRef(_sharedDevice.NativePointer);
            sharedDevice = new ID3D11Device(_sharedDevice.NativePointer);
        }

        try
        {
            _device = sharedDevice;
            sharedDevice = null;
            _deviceContext = _device.ImmediateContext;
            if (_deviceContext == null)
            {
                throw new InvalidOperationException("Shared D3D11 device returned a null immediate context.");
            }

            featureLevel = _device.FeatureLevel;
            return true;
        }
        catch (Exception ex)
        {
            sharedDevice?.Dispose();
            _deviceContext?.Dispose();
            _deviceContext = null;
            _device?.Dispose();
            _device = null;
            Logger.Log($"D3D11 shared device init failed: {ex.GetType().Name} hr=0x{ex.HResult:X8} msg={ex.Message}; falling back to renderer-owned device.");
            return false;
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

    private void EnsureInputResources(int width, int height, bool isHdr)
    {
        if (_device == null || _videoDevice == null || _videoProcessorEnumerator == null)
        {
            throw new InvalidOperationException("D3D11 device state is incomplete for input texture creation.");
        }

        var targetFormat = isHdr ? Format.P010 : Format.NV12;
        if (_inputTexture != null &&
            _stagingTexture != null &&
            _inputView != null &&
            _configuredInputWidth == width &&
            _configuredInputHeight == height &&
            _configuredInputFormat == targetFormat)
        {
            return;
        }

        _inputView?.Dispose();
        _inputView = null;
        _inputTexture?.Dispose();
        _inputTexture = null;
        _stagingTexture?.Dispose();
        _stagingTexture = null;

        var inputDescription = new Texture2DDescription(
            targetFormat,
            (uint)width,
            (uint)height,
            1,
            1,
            BindFlags.None,
            ResourceUsage.Default,
            CpuAccessFlags.None,
            1,
            0,
            ResourceOptionFlags.None);

        var stagingDescription = new Texture2DDescription(
            targetFormat,
            (uint)width,
            (uint)height,
            1,
            1,
            BindFlags.None,
            ResourceUsage.Staging,
            CpuAccessFlags.Write,
            1,
            0,
            ResourceOptionFlags.None);

        _inputTexture = _device.CreateTexture2D(inputDescription);
        _stagingTexture = _device.CreateTexture2D(stagingDescription);

        var inputViewDescription = new VideoProcessorInputViewDescription
        {
            ViewDimension = VideoProcessorInputViewDimension.Texture2D,
            Texture2D = new Texture2DVideoProcessorInputView { MipSlice = 0, ArraySlice = 0 }
        };

        _inputView = _videoDevice.CreateVideoProcessorInputView(_inputTexture, _videoProcessorEnumerator, inputViewDescription);
        _configuredInputFormat = targetFormat;
    }

    private void EnsureHdrInputResources(int width, int height)
    {
        if (_device == null)
        {
            throw new InvalidOperationException("D3D11 device state is incomplete for HDR shader input texture creation.");
        }

        if (_hdrPlaneViewsUnavailable)
        {
            return;
        }

        if (_hdrInputTexture != null &&
            _hdrStagingTexture != null &&
            _hdrYPlaneSRV != null &&
            _hdrUVPlaneSRV != null &&
            _hdrInputConfiguredWidth == width &&
            _hdrInputConfiguredHeight == height)
        {
            return;
        }

        _hdrYPlaneSRV?.Dispose();
        _hdrYPlaneSRV = null;
        _hdrUVPlaneSRV?.Dispose();
        _hdrUVPlaneSRV = null;
        _hdrInputTexture?.Dispose();
        _hdrInputTexture = null;
        _hdrStagingTexture?.Dispose();
        _hdrStagingTexture = null;

        var inputDescription = new Texture2DDescription(
            Format.P010,
            (uint)width,
            (uint)height,
            1,
            1,
            BindFlags.ShaderResource,
            ResourceUsage.Default,
            CpuAccessFlags.None,
            1,
            0,
            ResourceOptionFlags.None);

        var stagingDescription = new Texture2DDescription(
            Format.P010,
            (uint)width,
            (uint)height,
            1,
            1,
            BindFlags.None,
            ResourceUsage.Staging,
            CpuAccessFlags.Write,
            1,
            0,
            ResourceOptionFlags.None);

        _hdrInputTexture = _device.CreateTexture2D(inputDescription);
        _hdrStagingTexture = _device.CreateTexture2D(stagingDescription);

        _hdrYPlaneSRV = CreateHdrPlaneView(Format.R16_UNorm, planeSlice: 0);
        _hdrUVPlaneSRV = CreateHdrPlaneView(Format.R16G16_UNorm, planeSlice: 1);

        if (_hdrYPlaneSRV == null && _hdrUVPlaneSRV == null)
        {
            _hdrInputTexture.Dispose();
            _hdrInputTexture = null;
            _hdrStagingTexture.Dispose();
            _hdrStagingTexture = null;
            _hdrPlaneViewsUnavailable = true;
            return;
        }

        _hdrInputConfiguredWidth = width;
        _hdrInputConfiguredHeight = height;
    }

    private ID3D11ShaderResourceView? CreateHdrPlaneView(Format format, uint planeSlice)
    {
        if (_device == null || _hdrInputTexture == null)
        {
            throw new InvalidOperationException("HDR shader input texture has not been created.");
        }

        if (_device3 != null)
        {
            var srvDesc = new ShaderResourceViewDescription1(
                _hdrInputTexture,
                ShaderResourceViewDimension.Texture2D,
                format,
                0,
                1,
                0,
                1,
                planeSlice);

            return _device3.CreateShaderResourceView1(_hdrInputTexture, srvDesc);
        }

        Logger.Log("D3D11_RENDERER_WARN Device3 not available for P010 plane views — HDR shader path disabled, falling back to VideoProcessor");
        return null;
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

    private void BindSwapChainToPanel(IDXGISwapChain1 swapChain)
    {
        // ISwapChainPanelNative.SetSwapChain must be called on the UI thread
        // because _panel is a XAML element. Marshal from the render thread.
        //
        // Reinit deadlock guard: if the UI thread is blocked in StopRenderThread().Join()
        // waiting for this render thread to exit, dispatching here would deadlock until
        // the Join times out. Two safeguards: (a) the wait below polls _stopRequested in
        // short chunks so it can bail early, (b) the queued lambda re-checks both
        // _stopRequested and that _swapChain still equals the chain we are trying to
        // bind — if either has changed, the renderer has been stopped or the chain
        // superseded, and SetSwapChain on a stale (possibly disposed) chain would AV.
        var done = new ManualResetEventSlim(false);
        Exception? uiError = null;
        var aborted = false;

        var enqueued = _dispatcherQueue.TryEnqueue(() =>
        {
            try
            {
                if (Volatile.Read(ref _stopRequested) != 0 ||
                    !ReferenceEquals(_swapChain, swapChain))
                {
                    uiError = new OperationCanceledException(
                        "Swap chain binding superseded before reaching the UI thread.");
                    return;
                }
                if (_panel?.XamlRoot == null)
                {
                    uiError = new InvalidOperationException(
                        "Panel is no longer in the visual tree; swap chain binding skipped.");
                    return;
                }
                var panelNative = WinRT.CastExtensions.As<ISwapChainPanelNative>(_panel);
                panelNative.SetSwapChain(swapChain.NativePointer);
                Interlocked.Exchange(ref _swapChainAddress, swapChain.NativePointer.ToInt64());
                Interlocked.Exchange(ref _swapChainBound, 1);
            }
            catch (Exception ex)
            {
                uiError = ex;
            }
            finally
            {
                try { done.Set(); }
                catch { /* race with dispose if we aborted; safe to ignore */ }
            }
        });

        if (!enqueued)
        {
            done.Dispose();
            throw new InvalidOperationException("Failed to enqueue swap chain binding to UI thread.");
        }

        const int waitChunkMs = 50;
        const int maxWaitMs = 5000;
        var elapsedMs = 0;
        var completed = false;
        while (elapsedMs < maxWaitMs)
        {
            if (done.Wait(waitChunkMs))
            {
                completed = true;
                break;
            }
            elapsedMs += waitChunkMs;
            if (Volatile.Read(ref _stopRequested) != 0)
            {
                aborted = true;
                Logger.Log($"D3D11 preview swap-chain binding aborted at {elapsedMs}ms: stop requested during UI dispatcher wait.");
                break;
            }
        }

        if (!completed)
        {
            // Leave `done` undisposed — the queued lambda may still run later and
            // call done.Set(). Disposing now would race with that and risk an
            // ObjectDisposedException on the UI thread. The lambda's stale-chain
            // guard above prevents it from binding a disposed swap chain.
            if (aborted)
            {
                return;
            }
            throw new TimeoutException("Swap chain binding to UI thread timed out.");
        }

        done.Dispose();
        if (uiError != null)
        {
            if (uiError is OperationCanceledException)
            {
                Logger.Log("D3D11 preview swap-chain binding cancelled on UI thread; renderer shutting down.");
                return;
            }
            throw new InvalidOperationException("Swap chain binding failed on UI thread.", uiError);
        }
    }

    private void UnbindSwapChainFromPanel()
    {
        // Must run on UI thread since _panel is a XAML element.
        // Called from render thread during cleanup, so marshal via dispatcher.
        try
        {
            using var done = new ManualResetEventSlim(false);
            var enqueued = _dispatcherQueue.TryEnqueue(() =>
            {
                try
                {
                    // Guard: if the panel is no longer in the visual tree, its native
                    // COM backing may be released. AccessViolationException from a stale
                    // vtable pointer is a corrupted-state exception that .NET Core cannot
                    // catch — it terminates the process. Skip the call entirely.
                    if (_panel?.XamlRoot == null)
                    {
                        done.Set();
                        return;
                    }

                    var panelNative = WinRT.CastExtensions.As<ISwapChainPanelNative>(_panel);
                    panelNative.SetSwapChain(IntPtr.Zero);
                }
                catch
                {
                    // Best-effort: panel may already be torn down during app shutdown.
                    Logger.Log("D3D11 preview swap chain unbind skipped: UI callback failed during cleanup.");
                }
                finally
                {
                    done.Set();
                }
            });

            if (enqueued)
            {
                if (!done.Wait(TimeSpan.FromSeconds(2)))
                {
                    Logger.Log("D3D11 preview swap chain unbind timed out on UI thread during cleanup.");
                }
            }
            else
            {
                Logger.Log("D3D11 preview swap chain unbind enqueue failed during cleanup.");
            }
        }
        catch (Exception ex)
        {
            // Dispatcher may be shut down — safe to ignore during cleanup.
            Logger.Log($"D3D11 preview swap chain unbind ignored during cleanup: {ex.GetType().Name}: {ex.Message}");
        }
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
            swapChain2.MatrixTransform = System.Numerics.Matrix3x2.Identity;
            return;
        }

        var uniformScale = (float)Math.Min(panelLogicalW / swapW, panelLogicalH / swapH);
        var offsetX = (float)((panelLogicalW - swapW * uniformScale) * 0.5);
        var offsetY = (float)((panelLogicalH - swapH * uniformScale) * 0.5);

        swapChain2.MatrixTransform = new System.Numerics.Matrix3x2(
            uniformScale, 0,
            0, uniformScale,
            offsetX, offsetY);

        Logger.Log($"D3D11 preview composition transform set scale={uniformScale:F4} offset=({offsetX:F1},{offsetY:F1}) panel={panelLogicalW:F0}x{panelLogicalH:F0} swap={swapW}x{swapH}.");
    }
}

using System;
using System.Runtime.InteropServices;
using System.Threading;
using SharpGen.Runtime;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;

namespace Sussudio.Services.Preview;

internal sealed partial class D3D11PreviewRenderer
{
    private ID3D11Device? _sharedDevice;
    private int _sharedDeviceResetPending;
    private int _sharedDeviceActive;

    public void SetSharedDevice(ID3D11Device sharedDevice)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(sharedDevice);
        if (sharedDevice.NativePointer == IntPtr.Zero)
        {
            throw new ArgumentException("Shared D3D11 device pointer is null.", nameof(sharedDevice));
        }

        ID3D11Device? previous;
        lock (_lifecycleLock)
        {
            Marshal.AddRef(sharedDevice.NativePointer);
            previous = _sharedDevice;
            _sharedDevice = new ID3D11Device(sharedDevice.NativePointer);
        }

        previous?.Dispose();
        Interlocked.Exchange(ref _sharedDeviceActive, 0);

        // The render thread flips _isRendering before its first InitializeD3D().
        // If the capture service applies the shared device in that startup
        // window, the initial InitializeD3D() will already consume _sharedDevice;
        // queuing a reset would immediately unbind/dispose/recreate the freshly
        // bound swap chain. Only reset once D3D resources actually exist.
        if (Volatile.Read(ref _isRendering) != 0 &&
            (_device != null || _swapChain != null))
        {
            Interlocked.Exchange(ref _sharedDeviceResetPending, 1);
            SignalFrameReady("shared_device_reset");
        }
    }

    public void RetireSharedDeviceReferenceForReinit()
    {
        // Mode reinit retires this renderer after Stop() has already released
        // the render-thread resources. The remaining shared-device wrapper is a
        // duplicate COM reference obtained from the capture backend's
        // SharedD3DDeviceManager. Disposing that wrapper while the old capture
        // pipeline is also disposing its manager has produced corrupted-state
        // AccessViolationException crashes in SharpGen/Vortice. Abandon the
        // duplicate reference for this rare mode-switch path; the active
        // renderer gets a fresh shared device from the new capture pipeline.
        _sharedDevice = null;
        Interlocked.Exchange(ref _sharedDeviceActive, 0);
        Interlocked.Exchange(ref _sharedDeviceResetPending, 0);
    }

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

        var (swapChain, pixelWidth, pixelHeight) = InitializeCompositionSwapChain(device);
        ConfigureMediaPresentDuration();
        ApplyCompositionScaleTransform(swapChain);
        BindSwapChainToPanel(swapChain);
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

    private void HandleDeviceLost(Exception ex)
    {
        Logger.Log($"D3D11 preview device lost ({ex.GetType().Name}); recreating device.");

        // If Stop() is pending, bail. Stop() will unbind the swap chain from
        // the panel while D3D resources are still alive, then the finally block
        // will clean up. Proceeding here would dispose the swap chain while
        // Stop() may be concurrently calling SetSwapChain(null) on the panel -
        // the native call would hit freed memory and trigger an
        // AccessViolationException that .NET 8 cannot catch.
        if (Volatile.Read(ref _stopRequested) != 0) return;

        CleanupD3DResources();
        while (TryDequeuePendingFrame(out var stalePending))
        {
            TrackFrameDropped(stalePending, "device-lost");
            stalePending.Dispose();
        }

        // Re-check: Stop() may have been called during cleanup. Proceeding
        // into InitializeD3D->BindSwapChainToPanel would dispatch to the UI
        // thread, which may be blocked on Join - a 5-second deadlock.
        if (Volatile.Read(ref _stopRequested) != 0) return;

        InitializeD3D();
        Interlocked.Exchange(ref _compositionTransformDirty, 1);
    }

    private static bool IsDeviceLostException(Exception ex)
    {
        if (ex is SharpGenException sharpGenException)
        {
            return sharpGenException.ResultCode == Vortice.DXGI.ResultCode.DeviceRemoved ||
                   sharpGenException.ResultCode == Vortice.DXGI.ResultCode.DeviceReset;
        }

        if (ex is COMException comException)
        {
            return comException.HResult == unchecked((int)0x887A0005) ||
                   comException.HResult == unchecked((int)0x887A0007);
        }

        return false;
    }
}

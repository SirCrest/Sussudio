using System;
using System.Runtime.InteropServices;
using System.Threading;
using Vortice.Direct3D;
using Vortice.Direct3D11;

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
}

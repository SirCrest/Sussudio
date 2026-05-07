using System;
using System.Runtime.InteropServices;
using System.Threading;
using Vortice.Direct3D;
using Vortice.Direct3D11;

namespace Sussudio.Services.Preview;

// Shared D3D11 device plus MF DXGI device-manager handle used by the source
// reader and preview renderer. The manager owns reset/disposal ordering so GPU
// surfaces can be shared without each feature creating its own device.
internal sealed class SharedD3DDeviceManager : IDisposable
{
    private readonly object _sync = new();
    private ID3D11Device? _device;
    private ID3D11Multithread? _multithread;
    private IntPtr _dxgiDeviceManagerPtr;
    private int _disposed;

    public SharedD3DDeviceManager()
    {
        try
        {
            Initialize();
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    public ID3D11Device Device
    {
        get
        {
            lock (_sync)
            {
                ThrowIfDisposed();
                return _device ?? throw new ObjectDisposedException(nameof(SharedD3DDeviceManager));
            }
        }
    }

    public IntPtr DxgiDeviceManagerPtr
    {
        get
        {
            lock (_sync)
            {
                ThrowIfDisposed();
                return _dxgiDeviceManagerPtr;
            }
        }
    }

    public ID3D11DeviceContext ImmediateContext
    {
        get
        {
            lock (_sync)
            {
                ThrowIfDisposed();
                return _device?.ImmediateContext
                    ?? throw new ObjectDisposedException(nameof(SharedD3DDeviceManager));
            }
        }
    }

    public uint ResetToken { get; private set; }

    public bool TryCreateDeviceReference(out ID3D11Device? device, out string reason)
    {
        lock (_sync)
        {
            if (Volatile.Read(ref _disposed) != 0)
            {
                device = null;
                reason = "disposed";
                return false;
            }

            var currentDevice = _device;
            if (currentDevice == null)
            {
                device = null;
                reason = "missing_device";
                return false;
            }

            var nativePointer = currentDevice.NativePointer;
            if (nativePointer == IntPtr.Zero)
            {
                device = null;
                reason = "null_device_pointer";
                return false;
            }

            Marshal.AddRef(nativePointer);
            device = new ID3D11Device(nativePointer);
            reason = "ok";
            return true;
        }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        lock (_sync)
        {
            if (_dxgiDeviceManagerPtr != IntPtr.Zero)
            {
                Marshal.Release(_dxgiDeviceManagerPtr);
                _dxgiDeviceManagerPtr = IntPtr.Zero;
            }

            _multithread?.Dispose();
            _multithread = null;
            _device?.Dispose();
            _device = null;
        }
    }

    private void Initialize()
    {
        var featureLevels = new[] { FeatureLevel.Level_11_0 };
        var flags = DeviceCreationFlags.BgraSupport | DeviceCreationFlags.VideoSupport;

        var result = D3D11.D3D11CreateDevice(
            adapter: null,
            DriverType.Hardware,
            flags,
            featureLevels,
            out var device,
            out var featureLevel,
            out var context);

        context?.Dispose();

        if (result.Failure)
        {
            Logger.Log($"SHARED_D3D_DEVICE_CREATE_WARN mode=hardware hr=0x{result.Code:X8} fallback=warp");
            result = D3D11.D3D11CreateDevice(
                adapter: null,
                DriverType.Warp,
                flags,
                featureLevels,
                out device,
                out featureLevel,
                out context);
            context?.Dispose();
        }

        if (result.Failure || device == null)
        {
            throw new InvalidOperationException($"Shared D3D11 device creation failed (hr=0x{result.Code:X8}).");
        }

        _device = device;
        _multithread = _device.QueryInterfaceOrNull<ID3D11Multithread>();
        _multithread?.SetMultithreadProtected(true);

        var hr = MfInterop.MFCreateDXGIDeviceManager(out var resetToken, out var deviceManagerPtr);
        if (hr < 0 || deviceManagerPtr == IntPtr.Zero)
        {
            throw new InvalidOperationException($"MFCreateDXGIDeviceManager failed (hr=0x{hr:X8}).");
        }

        _dxgiDeviceManagerPtr = deviceManagerPtr;
        ResetToken = resetToken;

        hr = MfInterop.ResetDxgiDeviceManager(_dxgiDeviceManagerPtr, _device.NativePointer, resetToken);
        if (hr < 0)
        {
            throw new InvalidOperationException($"IMFDXGIDeviceManager.ResetDevice failed (hr=0x{hr:X8}).");
        }

        Logger.Log(
            "SHARED_D3D_DEVICE_CREATE " +
            $"feature_level={featureLevel} reset_token={ResetToken} manager=0x{_dxgiDeviceManagerPtr.ToInt64():X16}");
    }

    private void ThrowIfDisposed()
    {
        if (Volatile.Read(ref _disposed) != 0)
        {
            throw new ObjectDisposedException(nameof(SharedD3DDeviceManager));
        }
    }

    private static class MfInterop
    {
        [DllImport("mfplat.dll", ExactSpelling = true)]
        internal static extern int MFCreateDXGIDeviceManager(out uint pResetToken, out IntPtr ppDeviceManager);

        internal static unsafe int ResetDxgiDeviceManager(IntPtr deviceManagerPtr, IntPtr devicePtr, uint resetToken)
        {
            if (deviceManagerPtr == IntPtr.Zero)
            {
                throw new ArgumentException("DXGI device manager pointer is null.", nameof(deviceManagerPtr));
            }

            if (devicePtr == IntPtr.Zero)
            {
                throw new ArgumentException("D3D11 device pointer is null.", nameof(devicePtr));
            }

            var vtable = *(IntPtr**)deviceManagerPtr;
            // IMFDXGIDeviceManager vtable: 0-2 IUnknown, 3 CloseDeviceHandle, 4 GetVideoService,
            // 5 LockDevice, 6 OpenDeviceHandle, 7 ResetDevice, 8 TestDevice, 9 UnlockDevice
            var resetDevice = (delegate* unmanaged[Stdcall]<IntPtr, IntPtr, uint, int>)vtable[7];
            return resetDevice(deviceManagerPtr, devicePtr, resetToken);
        }
    }
}

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Vortice.DXGI;

namespace Sussudio.Services.Preview;

internal sealed partial class D3D11PreviewRenderer
{
    private const uint WaitObject0 = 0;
    private const uint WaitTimeout = 258;
    private IntPtr _frameLatencyWaitHandle;

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

    [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
    private static extern uint WaitForSingleObject(IntPtr handle, uint milliseconds);
}

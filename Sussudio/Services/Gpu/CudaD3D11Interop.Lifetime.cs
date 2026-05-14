using System;
using System.Threading;

namespace Sussudio.Services.Gpu;

internal sealed unsafe partial class CudaD3D11InteropBridge
{
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return;

        // 1. Unregister CUDA resources while context is still alive
        if (_registeredResourceY != IntPtr.Zero)
        {
            TryUnregisterResource(_registeredResourceY);
            _registeredResourceY = IntPtr.Zero;
        }

        if (_registeredResourceUV != IntPtr.Zero)
        {
            TryUnregisterResource(_registeredResourceUV);
            _registeredResourceUV = IntPtr.Zero;
        }

        // 2. Dispose D3D11 textures (CUDA no longer references them)
        _helperTextureY?.Dispose();
        _helperTextureY = null;
        _helperTextureUV?.Dispose();
        _helperTextureUV = null;
        _stagingTexture?.Dispose();
        _stagingTexture = null;
        _defaultTexture?.Dispose();
        _defaultTexture = null;

        // 3. Release CUDA primary context after all CUDA and D3D11 resources are freed
        cuDevicePrimaryCtxRelease(_cuDevice);

        // 4. Release COM references obtained in the constructor. ImmediateContext and
        // QueryInterfaceOrNull both AddRef, so we must Dispose (Release) to balance.
        // Each consumer holds its own refcounted reference — releasing ours does not
        // affect other consumers (preview renderer, encoder) of the same device.
        _multithread?.Dispose();
        _deviceContext?.Dispose();

        _initialized = false;
        Logger.Log($"CUDA_D3D11_INTEROP_DISPOSED zero_copy={_zeroCopyAvailable}");
    }

    private void TryUnregisterResource(IntPtr resource)
    {
        try
        {
            cuCtxSetCurrent(_cudaCtx);
            cuGraphicsUnregisterResource(resource);
        }
        catch
        {
            // Best-effort: CUDA context or resource may already be destroyed during teardown — non-fatal
        }
    }
}

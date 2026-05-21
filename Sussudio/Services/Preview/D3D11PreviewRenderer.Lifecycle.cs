using System;
using System.Threading;
using Vortice.DXGI;

namespace Sussudio.Services.Preview;

internal sealed partial class D3D11PreviewRenderer
{
    private readonly object _lifecycleLock = new();
    private Thread? _renderThread;
    private int _disposed;
    private int _isRendering;
    private int _startupWidth;
    private int _startupHeight;
    private double _startupFps = 60.0;

    public void Start(int width, int height, double fps, bool isHdr)
    {
        ThrowIfDisposed();
        if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
        if (fps <= 0) throw new ArgumentOutOfRangeException(nameof(fps));

        Stop();

        lock (_lifecycleLock)
        {
            _startupWidth = width;
            _startupHeight = height;
            _startupFps = fps;

            Volatile.Write(ref _naturalWidth, width);
            Volatile.Write(ref _naturalHeight, height);
            if (Volatile.Read(ref _panelPixelWidth) <= 0) Volatile.Write(ref _panelPixelWidth, width);
            if (Volatile.Read(ref _panelPixelHeight) <= 0) Volatile.Write(ref _panelPixelHeight, height);

            _configuredInputWidth = 0;
            _configuredInputHeight = 0;
            _configuredOutputWidth = 0;
            _configuredOutputHeight = 0;
            _configuredInputFormat = Format.Unknown;
            _configuredHdr = isHdr;
            _outputFrameIndex = 0;
            Volatile.Write(ref _rendererMode, RendererModeNone);

            Interlocked.Exchange(ref _stopRequested, 0);
            Interlocked.Exchange(ref _compositionTransformDirty, 1);
            ResetFirstFrameNotification();
            Interlocked.Exchange(ref _sharedDeviceResetPending, 0);
            ResetFrameReady("start");

            _renderThread = new Thread(RenderThreadMain)
            {
                IsBackground = true,
                Priority = ThreadPriority.AboveNormal,
                Name = "D3D11PreviewRenderer"
            };
            _renderThread.Start();
        }

        Logger.Log($"D3D11 preview renderer start width={width} height={height} fps={fps:0.###} hdr={isHdr}.");
    }

    private void ThrowIfDisposed()
    {
        if (Volatile.Read(ref _disposed) != 0)
        {
            throw new ObjectDisposedException(nameof(D3D11PreviewRenderer));
        }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
        Stop();
        _sharedDevice?.Dispose();
        _sharedDevice = null;
        _frameReadyEvent.Dispose();
    }
}

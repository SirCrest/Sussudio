using System;
using System.Threading;
using System.Threading.Tasks;

namespace Sussudio.Services.Capture;

// Negotiated capture mode for one MfSourceReaderVideoCapture session. Bundling
// these is mostly about not having a 9-positional-parameter call site — the
// device link stays separate because it identifies the device, not the mode.
public sealed record VideoCaptureNegotiationOptions(
    int Width,
    int Height,
    double Fps,
    bool RequireP010,
    string? RequestedPixelFormat = null,
    bool UseMjpegHighFrameRateMode = false,
    IntPtr DxgiDeviceManager = default,
    bool UseExternalMjpegDecode = false);

public sealed partial class MfSourceReaderVideoCapture : IAsyncDisposable
{
    public delegate void RawFrameCallback(ReadOnlySpan<byte> frameData, int width, int height, long arrivalTick);
    public delegate void DualFrameCallback(IntPtr gpuTexture, int gpuSubresource, ReadOnlySpan<byte> cpuData, int width, int height, long arrivalTick);

    private readonly object _sync = new();
    private IMFSourceReader? _sourceReader;
    private IMFMediaSource? _mediaSource;
    private CancellationTokenSource? _readCts;
    private Task? _readTask;
    private bool _isInitialized;
    private bool _startupHeld;
    private bool _sourceReaderD3DEnabled;
    private IntPtr _dxgiDeviceManagerPtr;
    private int _width;
    private int _height;
    private double _fps;
    private bool _isP010;
    private bool _isCompressedMjpgOutput;
    private bool _isHighFrameRateMjpegMode;
    private bool _strictD3DOutputRequired;
    private bool _strictTextureOutputRequired;
    private string _deviceSymbolicLink = string.Empty;
    private string _nativeInputFormat = "unknown";
    private string _negotiatedFormat = "unknown";
    private int _fatalErrorSignaled;
    private long _framesDelivered;
    private long _framesDropped;
    private int _isReadSampleOutstanding;
    private long _readSampleOutstandingStartTickMs;
    private long _lastFrameDeliveredTickMs;
    private int _vtableDiagDone;
    private int _dxgiBufferProbeDone;
    private int _dxgiResourceFailureCount;
    private bool _skipCpuReadback;
    public long FramesDelivered => Interlocked.Read(ref _framesDelivered);
    public long FramesDropped => Interlocked.Read(ref _framesDropped);
    public string NegotiatedFormat => Volatile.Read(ref _negotiatedFormat);
    public string NativeInputFormat => Volatile.Read(ref _nativeInputFormat);
    public bool IsP010 => Volatile.Read(ref _isP010);
    public bool IsCompressedMjpgOutput => Volatile.Read(ref _isCompressedMjpgOutput);
    public bool IsD3DOutputEnabled => Volatile.Read(ref _sourceReaderD3DEnabled);
    public bool IsHighFrameRateMjpegMode => Volatile.Read(ref _isHighFrameRateMjpegMode);
    public int Width => Volatile.Read(ref _width);
    public int Height => Volatile.Read(ref _height);
    public double Fps => Volatile.Read(ref _fps);
    public bool SkipCpuReadback
    {
        get => Volatile.Read(ref _skipCpuReadback);
        set => Volatile.Write(ref _skipCpuReadback, value);
    }
    public event EventHandler<Exception>? FatalErrorOccurred;
    public bool IsReadSampleOutstanding => Volatile.Read(ref _isReadSampleOutstanding) != 0;
    public long ReadSampleOutstandingMs
    {
        get
        {
            if (Volatile.Read(ref _isReadSampleOutstanding) == 0)
            {
                return 0;
            }

            var startedTickMs = Interlocked.Read(ref _readSampleOutstandingStartTickMs);
            return startedTickMs <= 0
                ? 0
                : Math.Max(0, Environment.TickCount64 - startedTickMs);
        }
    }
    public long LastFrameDeliveredTickMs => Interlocked.Read(ref _lastFrameDeliveredTickMs);
}

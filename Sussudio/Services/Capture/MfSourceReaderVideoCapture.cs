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

    public static int GetFrameSizeBytes(int width, int height, bool isP010)
        => isP010 ? width * height * 3 : (width * height * 3) / 2;

    private static int GetRowBytes(int width, bool isP010)
        => isP010 ? width * 2 : width;

    private unsafe static void CopyYuvWithStride(
        byte* sourceStart,
        int stride,
        Span<byte> destination,
        int width,
        int height,
        bool isP010)
    {
        var rowBytes = GetRowBytes(width, isP010);
        var uvHeight = height / 2;
        var yBytes = rowBytes * height;
        var uvBytes = rowBytes * uvHeight;
        if (destination.Length < yBytes + uvBytes)
        {
            throw new ArgumentException("Destination span is too small for packed frame.");
        }

        var strideAbs = Math.Abs(stride);
        if (strideAbs < rowBytes)
        {
            throw new InvalidOperationException(
                $"Source stride ({stride}) is smaller than packed row width ({rowBytes}).");
        }

        var yDest = destination[..yBytes];
        var uvDest = destination.Slice(yBytes, uvBytes);
        var yStart = sourceStart;
        var uvStart = sourceStart + (stride * height);

        if (stride < 0)
        {
            yStart = sourceStart + (stride * (height - 1));
            uvStart = sourceStart + (stride * (height + uvHeight - 1));
        }

        for (var row = 0; row < height; row++)
        {
            var src = stride >= 0
                ? yStart + (row * stride)
                : yStart - (row * strideAbs);
            var dst = yDest.Slice(row * rowBytes, rowBytes);
            new ReadOnlySpan<byte>(src, rowBytes).CopyTo(dst);
        }

        for (var row = 0; row < uvHeight; row++)
        {
            var src = stride >= 0
                ? uvStart + (row * stride)
                : uvStart - (row * strideAbs);
            var dst = uvDest.Slice(row * rowBytes, rowBytes);
            new ReadOnlySpan<byte>(src, rowBytes).CopyTo(dst);
        }
    }

    private static int InferPackedStride(int currentLength, int height)
    {
        if (currentLength <= 0 || height <= 0)
        {
            return 0;
        }

        var totalRows = height + (height / 2);
        return totalRows > 0 ? currentLength / totalRows : 0;
    }

    private static string SubtypeGuidToName(Guid subtype)
    {
        if (subtype == MfGuids.MFVideoFormat_P010) return "P010";
        if (subtype == MfGuids.MFVideoFormat_NV12) return "NV12";
        if (subtype == new Guid(0x32595559, 0x0000, 0x0010, 0x80, 0x00, 0x00, 0xAA, 0x00, 0x38, 0x9B, 0x71)) return "YUY2";
        if (subtype == new Guid(0x47504A4D, 0x0000, 0x0010, 0x80, 0x00, 0x00, 0xAA, 0x00, 0x38, 0x9B, 0x71)) return "MJPG";
        if (subtype == new Guid(0x00000014, 0x0000, 0x0010, 0x80, 0x00, 0x00, 0xAA, 0x00, 0x38, 0x9B, 0x71)) return "RGB24";
        // FourCC-style: first 4 bytes of GUID are the FourCC.
        var bytes = subtype.ToByteArray();
        if (bytes[4] == 0 && bytes[5] == 0 && bytes[6] == 0x10 && bytes[7] == 0)
        {
            var fourcc = new char[4];
            for (var i = 0; i < 4; i++)
            {
                fourcc[i] = bytes[i] >= 0x20 && bytes[i] <= 0x7E ? (char)bytes[i] : '?';
            }

            return new string(fourcc);
        }

        return subtype.ToString("B");
    }
}

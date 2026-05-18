using System;
using System.Threading;
using Sussudio.Services.Audio;

namespace Sussudio.Services.Capture;

public sealed partial class MfSourceReaderVideoCapture
{
    private readonly record struct SourceReaderNegotiatedMode(
        Guid Subtype,
        int Width,
        int Height,
        double Fps,
        string Description);

    private SourceReaderNegotiatedMode ApplyCurrentMediaTypeAndReconcileActualOutput(
        IMFSourceReader sourceReader,
        IMFMediaType selectedMediaType,
        SourceReaderNegotiatedMode selectedMode,
        bool useConvertedMjpegNv12)
    {
        IMFMediaType? actualMediaType = null;
        try
        {
            MfInteropHelpers.ThrowIfFailed(
                sourceReader.SetCurrentMediaType(
                    MfConstants.MF_SOURCE_READER_FIRST_VIDEO_STREAM,
                    IntPtr.Zero,
                    selectedMediaType),
                $"IMFSourceReader.SetCurrentMediaType({SubtypeGuidToName(selectedMode.Subtype)})");

            MfInteropHelpers.ThrowIfFailed(
                sourceReader.GetCurrentMediaType(
                    MfConstants.MF_SOURCE_READER_FIRST_VIDEO_STREAM,
                    out actualMediaType),
                "IMFSourceReader.GetCurrentMediaType");

            return actualMediaType != null
                ? ReconcileActualMediaType(actualMediaType, selectedMode, useConvertedMjpegNv12)
                : selectedMode;
        }
        finally
        {
            WasapiComInterop.ReleaseComObject(ref actualMediaType);
        }
    }

    private static SourceReaderNegotiatedMode ReconcileActualMediaType(
        IMFMediaType actualMediaType,
        SourceReaderNegotiatedMode selectedMode,
        bool useConvertedMjpegNv12)
    {
        var subtype = selectedMode.Subtype;
        var width = selectedMode.Width;
        var height = selectedMode.Height;
        var fps = selectedMode.Fps;
        var description = selectedMode.Description;

        if (MfInteropHelpers.TryGetGuid(actualMediaType, ref MfGuids.MF_MT_SUBTYPE, out var actualSubtype))
        {
            subtype = actualSubtype;
        }

        if (TryGetFrameSize(actualMediaType, out var actualWidth, out var actualHeight))
        {
            width = actualWidth;
            height = actualHeight;
        }

        if (TryGetFrameRate(actualMediaType, out var actualFpsNumerator, out var actualFpsDenominator) &&
            actualFpsDenominator > 0)
        {
            fps = (double)actualFpsNumerator / actualFpsDenominator;
        }

        if (useConvertedMjpegNv12)
        {
            description = $"{SubtypeGuidToName(subtype)} <= MJPG {width}x{height}@{fps:0.###}";
        }

        return new SourceReaderNegotiatedMode(subtype, width, height, fps, description);
    }

    private void ValidateNegotiatedOutputMode(
        SourceReaderNegotiatedMode mode,
        bool useConvertedMjpegNv12,
        bool useRawMjpgOutput,
        bool sourceReaderD3DEnabled)
    {
        if (useConvertedMjpegNv12)
        {
            if (!sourceReaderD3DEnabled)
            {
                throw new InvalidOperationException("4K120 MJPG mode requires D3D11-backed decoded output.");
            }

            if (mode.Subtype != MfGuids.MFVideoFormat_NV12)
            {
                throw new InvalidOperationException(
                    $"4K120 MJPG mode requires decoded NV12 output, but negotiated {SubtypeGuidToName(mode.Subtype)}.");
            }
        }
        else if (useRawMjpgOutput && mode.Subtype != MfGuids.MFVideoFormat_MJPG)
        {
            throw new InvalidOperationException(
                $"External MJPG decode requires native MJPG output, but negotiated {SubtypeGuidToName(mode.Subtype)}.");
        }
    }

    private string ResolveNativeInputFormatName(
        SourceReaderNegotiatedMode mode,
        bool useConvertedMjpegNv12,
        bool useRawMjpgOutput)
    {
        var nativeFormatName = SubtypeGuidToName(mode.Subtype);
        if (!useConvertedMjpegNv12 && !useRawMjpgOutput)
        {
            // Bandwidth heuristic: raw uncompressed NV12/YUY2 at this res+fps may exceed USB 3.0 (~5 Gbps).
            // If so, the device must be delivering compressed (MJPG) regardless of what MF negotiated.
            var rawBitsPerSecond = (double)mode.Width * mode.Height * 1.5 * mode.Fps * 8;
            const double usb30BandwidthBits = 5e9;
            if (rawBitsPerSecond > usb30BandwidthBits &&
                !string.Equals(nativeFormatName, "MJPG", StringComparison.OrdinalIgnoreCase))
            {
                Log($"MF_NATIVE_FORMAT_OVERRIDE negotiated={nativeFormatName} raw_bps={rawBitsPerSecond:0} usb_bps={usb30BandwidthBits:0} => MJPG");
                nativeFormatName = "MJPG";
            }
        }
        else
        {
            nativeFormatName = "MJPG";
        }

        return nativeFormatName;
    }

    private void CommitInitializedRuntimeState(
        string deviceSymbolicLink,
        SourceReaderNegotiatedMode mode,
        ref IMFMediaSource? mediaSource,
        ref IMFSourceReader? sourceReader,
        ref bool startupHeld,
        bool sourceReaderD3DEnabled,
        IntPtr dxgiDeviceManager,
        bool useConvertedMjpegNv12,
        bool useRawMjpgOutput)
    {
        _deviceSymbolicLink = deviceSymbolicLink;
        _width = mode.Width;
        _height = mode.Height;
        _fps = mode.Fps;
        SetExpectedFrameRate(_fps);
        _isP010 = useRawMjpgOutput ? false : mode.Subtype == MfGuids.MFVideoFormat_P010;
        _isCompressedMjpgOutput = useRawMjpgOutput;
        _isHighFrameRateMjpegMode = useConvertedMjpegNv12 || useRawMjpgOutput;
        _strictD3DOutputRequired = useConvertedMjpegNv12;
        _strictTextureOutputRequired = useConvertedMjpegNv12;

        Volatile.Write(ref _nativeInputFormat, ResolveNativeInputFormatName(mode, useConvertedMjpegNv12, useRawMjpgOutput));
        Volatile.Write(ref _negotiatedFormat, mode.Description);
        Interlocked.Exchange(ref _framesDelivered, 0);
        Interlocked.Exchange(ref _framesDropped, 0);
        Volatile.Write(ref _isReadSampleOutstanding, 0);
        Interlocked.Exchange(ref _readSampleOutstandingStartTickMs, 0);
        Interlocked.Exchange(ref _lastFrameDeliveredTickMs, 0);
        Interlocked.Exchange(ref _dxgiBufferProbeDone, 0);
        Interlocked.Exchange(ref _dxgiResourceFailureCount, 0);
        Interlocked.Exchange(ref _fatalErrorSignaled, 0);

        lock (_sync)
        {
            _mediaSource = mediaSource;
            _sourceReader = sourceReader;
            _startupHeld = startupHeld;
            _sourceReaderD3DEnabled = sourceReaderD3DEnabled;
            _dxgiDeviceManagerPtr = sourceReaderD3DEnabled ? dxgiDeviceManager : IntPtr.Zero;
            _isInitialized = true;
            mediaSource = null;
            sourceReader = null;
            startupHeld = false;
        }
    }
}

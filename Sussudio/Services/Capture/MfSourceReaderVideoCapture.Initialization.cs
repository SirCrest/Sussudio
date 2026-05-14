using System;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Services.Audio;

namespace Sussudio.Services.Capture;

public sealed partial class MfSourceReaderVideoCapture
{
    public Task InitializeAsync(string deviceSymbolicLink, VideoCaptureNegotiationOptions options)
    {
        if (string.IsNullOrWhiteSpace(deviceSymbolicLink))
        {
            throw new ArgumentException("Video device symbolic link is required.", nameof(deviceSymbolicLink));
        }

        if (options.Width <= 0 || options.Height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "Video width/height must be positive.");
        }

        if (options.Fps <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "Video frame rate must be positive.");
        }

        var width = options.Width;
        var height = options.Height;
        var fps = options.Fps;
        var requireP010 = options.RequireP010;
        var requestedPixelFormat = options.RequestedPixelFormat;
        var useMjpegHighFrameRateMode = options.UseMjpegHighFrameRateMode;
        var dxgiDeviceManager = options.DxgiDeviceManager;
        var useExternalMjpegDecode = options.UseExternalMjpegDecode;

        lock (_sync)
        {
            if (_isInitialized)
            {
                throw new InvalidOperationException("IMF source reader capture is already initialized.");
            }
        }

        IMFMediaSource? mediaSource = null;
        IMFSourceReader? sourceReader = null;
        IMFAttributes? readerAttributes = null;
        IMFMediaType? selectedMediaType = null;
        IMFMediaType? actualMediaType = null;
        var startupHeld = false;
        var sourceReaderD3DEnabled = false;
        var disableConverters = true;
        var requestedSourceSubtypeName = requestedPixelFormat;
        var useConvertedMjpegNv12 = useMjpegHighFrameRateMode &&
                                    !requireP010 &&
                                    !useExternalMjpegDecode &&
                                    string.Equals(requestedPixelFormat, "MJPG", StringComparison.OrdinalIgnoreCase);
        var useRawMjpgOutput = useMjpegHighFrameRateMode &&
                               !requireP010 &&
                               useExternalMjpegDecode &&
                                    string.Equals(requestedPixelFormat, "MJPG", StringComparison.OrdinalIgnoreCase);

        try
        {
            MfInteropHelpers.AddStartupReference();
            startupHeld = true;

            mediaSource = CreateMediaSource(deviceSymbolicLink);

            disableConverters = !useConvertedMjpegNv12 && !useRawMjpgOutput;
            MfInteropHelpers.ThrowIfFailed(
                MfInterop.MFCreateAttributes(out readerAttributes, useConvertedMjpegNv12 ? 3 : 2),
                "MFCreateAttributes(reader)");
            if (useConvertedMjpegNv12)
            {
                MfInteropHelpers.ThrowIfFailed(
                    readerAttributes.SetUINT32(ref MfGuids.MF_READWRITE_ENABLE_HARDWARE_TRANSFORMS, 1),
                    "IMFAttributes.SetUINT32(MF_READWRITE_ENABLE_HARDWARE_TRANSFORMS)");
            }
            if (disableConverters)
            {
                MfInteropHelpers.ThrowIfFailed(
                    readerAttributes.SetUINT32(ref MfGuids.MF_READWRITE_DISABLE_CONVERTERS, 1),
                    "IMFAttributes.SetUINT32(MF_READWRITE_DISABLE_CONVERTERS)");
            }

            if (!useRawMjpgOutput &&
                dxgiDeviceManager != IntPtr.Zero &&
                TrySetSourceReaderD3DManager(readerAttributes, dxgiDeviceManager))
            {
                sourceReaderD3DEnabled = true;
            }
            else if (useConvertedMjpegNv12)
            {
                throw new InvalidOperationException("4K120 MJPG mode requires D3D11-backed SourceReader output.");
            }

            var createSourceReaderHr = MfInterop.MFCreateSourceReaderFromMediaSource(mediaSource, readerAttributes, out sourceReader);
            if (createSourceReaderHr < 0 && sourceReaderD3DEnabled)
            {
                if (useConvertedMjpegNv12)
                {
                    MfInteropHelpers.ThrowIfFailed(createSourceReaderHr, "MFCreateSourceReaderFromMediaSource(hfr_mjpeg_d3d)");
                }

                Log(
                    "MF_SOURCE_READER_D3D_INIT_WARN " +
                    $"stage=CreateSourceReader hr=0x{createSourceReaderHr:X8} " +
                    "fallback=cpu_only");

                WasapiComInterop.ReleaseComObject(ref sourceReader);
                WasapiComInterop.ReleaseComObject(ref readerAttributes);

                MfInteropHelpers.ThrowIfFailed(
                    MfInterop.MFCreateAttributes(out readerAttributes, 1),
                    "MFCreateAttributes(reader_cpu_fallback)");
                if (disableConverters)
                {
                    MfInteropHelpers.ThrowIfFailed(
                        readerAttributes.SetUINT32(ref MfGuids.MF_READWRITE_DISABLE_CONVERTERS, 1),
                        "IMFAttributes.SetUINT32(MF_READWRITE_DISABLE_CONVERTERS)");
                }

                sourceReaderD3DEnabled = false;
                createSourceReaderHr = MfInterop.MFCreateSourceReaderFromMediaSource(mediaSource, readerAttributes, out sourceReader);
            }

            MfInteropHelpers.ThrowIfFailed(createSourceReaderHr, "MFCreateSourceReaderFromMediaSource");

            var requestedSubtype = requireP010
                ? MfGuids.MFVideoFormat_P010
                : MfGuids.MFVideoFormat_NV12;

            Guid negotiatedSubtype;
            int negotiatedWidth;
            int negotiatedHeight;
            double negotiatedFps;
            string negotiatedDescription;
            if (useConvertedMjpegNv12)
            {
                requestedSourceSubtypeName = "MJPG";
                selectedMediaType = SelectConvertedMediaType(
                    sourceReader,
                    width,
                    height,
                    fps,
                    MfGuids.MFVideoFormat_MJPG,
                    requestedSubtype,
                    out negotiatedSubtype,
                    out negotiatedWidth,
                    out negotiatedHeight,
                    out negotiatedFps,
                    out negotiatedDescription);
            }
            else if (useRawMjpgOutput)
            {
                requestedSourceSubtypeName = "MJPG";
                selectedMediaType = SelectMediaType(
                    sourceReader,
                    width,
                    height,
                    fps,
                    MfGuids.MFVideoFormat_MJPG,
                    out negotiatedSubtype,
                    out negotiatedWidth,
                    out negotiatedHeight,
                    out negotiatedFps,
                    out negotiatedDescription);
            }
            else
            {
                selectedMediaType = SelectMediaType(
                    sourceReader,
                    width,
                    height,
                    fps,
                    requestedSubtype,
                    out negotiatedSubtype,
                    out negotiatedWidth,
                    out negotiatedHeight,
                    out negotiatedFps,
                    out negotiatedDescription);
            }

            MfInteropHelpers.ThrowIfFailed(
                sourceReader.SetCurrentMediaType(
                    MfConstants.MF_SOURCE_READER_FIRST_VIDEO_STREAM,
                    IntPtr.Zero,
                    selectedMediaType),
                $"IMFSourceReader.SetCurrentMediaType({SubtypeGuidToName(negotiatedSubtype)})");

            MfInteropHelpers.ThrowIfFailed(
                sourceReader.GetCurrentMediaType(
                    MfConstants.MF_SOURCE_READER_FIRST_VIDEO_STREAM,
                    out actualMediaType),
                "IMFSourceReader.GetCurrentMediaType");

            if (actualMediaType != null)
            {
                if (MfInteropHelpers.TryGetGuid(actualMediaType, ref MfGuids.MF_MT_SUBTYPE, out var actualSubtype))
                {
                    negotiatedSubtype = actualSubtype;
                }

                if (TryGetFrameSize(actualMediaType, out var actualWidth, out var actualHeight))
                {
                    negotiatedWidth = actualWidth;
                    negotiatedHeight = actualHeight;
                }

                if (TryGetFrameRate(actualMediaType, out var actualFpsNumerator, out var actualFpsDenominator) &&
                    actualFpsDenominator > 0)
                {
                    negotiatedFps = (double)actualFpsNumerator / actualFpsDenominator;
                }

                if (useConvertedMjpegNv12)
                {
                    negotiatedDescription =
                        $"{SubtypeGuidToName(negotiatedSubtype)} <= MJPG {negotiatedWidth}x{negotiatedHeight}@{negotiatedFps:0.###}";
                }
            }

            if (useConvertedMjpegNv12)
            {
                if (!sourceReaderD3DEnabled)
                {
                    throw new InvalidOperationException("4K120 MJPG mode requires D3D11-backed decoded output.");
                }

                if (negotiatedSubtype != MfGuids.MFVideoFormat_NV12)
                {
                    throw new InvalidOperationException(
                        $"4K120 MJPG mode requires decoded NV12 output, but negotiated {SubtypeGuidToName(negotiatedSubtype)}.");
                }
            }
            else if (useRawMjpgOutput && negotiatedSubtype != MfGuids.MFVideoFormat_MJPG)
            {
                throw new InvalidOperationException(
                    $"External MJPG decode requires native MJPG output, but negotiated {SubtypeGuidToName(negotiatedSubtype)}.");
            }

            _deviceSymbolicLink = deviceSymbolicLink;
            _width = negotiatedWidth;
            _height = negotiatedHeight;
            _fps = negotiatedFps;
            SetExpectedFrameRate(_fps);
            _isP010 = useRawMjpgOutput ? false : negotiatedSubtype == MfGuids.MFVideoFormat_P010;
            _isCompressedMjpgOutput = useRawMjpgOutput;
            _isHighFrameRateMjpegMode = useConvertedMjpegNv12 || useRawMjpgOutput;
            _strictD3DOutputRequired = useConvertedMjpegNv12;
            _strictTextureOutputRequired = useConvertedMjpegNv12;
            var nativeFormatName = SubtypeGuidToName(negotiatedSubtype);
            if (!useConvertedMjpegNv12 && !useRawMjpgOutput)
            {
                // Bandwidth heuristic: raw uncompressed NV12/YUY2 at this res+fps may exceed USB 3.0 (~5 Gbps).
                // If so, the device must be delivering compressed (MJPG) regardless of what MF negotiated.
                var rawBitsPerSecond = (double)negotiatedWidth * negotiatedHeight * 1.5 * negotiatedFps * 8;
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
            Volatile.Write(ref _nativeInputFormat, nativeFormatName);
            Volatile.Write(ref _negotiatedFormat, negotiatedDescription);
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

            Log(
                "MF_SOURCE_READER_INIT " +
                $"device='{deviceSymbolicLink}' " +
                $"requested={width}x{height}@{fps:0.###} " +
                $"requested_source_subtype='{requestedSourceSubtypeName ?? (requireP010 ? "P010" : "NV12")}' " +
                $"native_input='{_nativeInputFormat}' " +
                $"negotiated='{_negotiatedFormat}' " +
                $"d3d_manager_enabled={sourceReaderD3DEnabled} " +
                $"mf_readwrite_disable_converters={disableConverters.ToString().ToLowerInvariant()} " +
                $"mf_readwrite_enable_hardware_transforms={useConvertedMjpegNv12.ToString().ToLowerInvariant()}");
        }
        catch (Exception ex)
        {
            Log(
                "MF_SOURCE_READER_INIT_FAIL " +
                $"device='{deviceSymbolicLink}' " +
                $"requested={width}x{height}@{fps:0.###} " +
                $"requested_source_subtype='{requestedSourceSubtypeName ?? (requireP010 ? "P010" : "NV12")}' " +
                $"d3d_manager_requested={(dxgiDeviceManager != IntPtr.Zero)} " +
                $"type={ex.GetType().Name} msg={ex.Message}");
            throw;
        }
        finally
        {
            WasapiComInterop.ReleaseComObject(ref actualMediaType);
            WasapiComInterop.ReleaseComObject(ref selectedMediaType);
            WasapiComInterop.ReleaseComObject(ref readerAttributes);
            WasapiComInterop.ReleaseComObject(ref sourceReader);
            WasapiComInterop.ReleaseComObject(ref mediaSource);

            if (startupHeld)
            {
                MfInteropHelpers.ReleaseStartupReference();
            }
        }

        return Task.CompletedTask;
    }
}
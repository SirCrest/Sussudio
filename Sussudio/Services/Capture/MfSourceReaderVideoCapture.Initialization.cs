using System;
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

            var negotiatedMode = ApplyCurrentMediaTypeAndReconcileActualOutput(
                sourceReader,
                selectedMediaType,
                new SourceReaderNegotiatedMode(
                    negotiatedSubtype,
                    negotiatedWidth,
                    negotiatedHeight,
                    negotiatedFps,
                    negotiatedDescription),
                useConvertedMjpegNv12);
            ValidateNegotiatedOutputMode(
                negotiatedMode,
                useConvertedMjpegNv12,
                useRawMjpgOutput,
                sourceReaderD3DEnabled);
            CommitInitializedRuntimeState(
                deviceSymbolicLink,
                negotiatedMode,
                ref mediaSource,
                ref sourceReader,
                ref startupHeld,
                sourceReaderD3DEnabled,
                dxgiDeviceManager,
                useConvertedMjpegNv12,
                useRawMjpgOutput);

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

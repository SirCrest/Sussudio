// Tests that prevent app service code from drifting into stale namespaces.
static partial class Program
{
    private static void AssertServiceNamespaceSourceOwnership(string repoRoot)
    {
        AssertServiceNamespaceServicesLayerOwnership(repoRoot);
        AssertServiceNamespaceMainViewModelSourceOwnership(repoRoot);
    }

    private static void AssertServiceNamespaceServicesLayerOwnership(string repoRoot)
    {
        var deviceServiceText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "Services", "Capture", "DeviceService.cs"));
        var deviceServiceRootText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "Services", "Capture", "DeviceService.cs"));
        var deviceServiceFormatCacheText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "Services", "Capture", "DeviceService.FormatCache.cs"));
        var deviceServiceFormatProbeText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "Services", "Capture", "DeviceService.FormatProbe.cs"));
        AssertContains(deviceServiceText, "NativeXuInterfacePath = ResolveNativeXuInterfacePath(videoDevice.SymbolicLink)");
        AssertContains(deviceServiceText, "Native XU interface resolution found no matching interface");
        AssertDoesNotContain(deviceServiceText, "SelectOnlyUnambiguousDeviceGroup");
        AssertContains(deviceServiceRootText, "public async Task<ObservableCollection<CaptureDevice>> EnumerateVideoCaptureDevicesAsync(");
        AssertContains(deviceServiceFormatCacheText, "internal sealed class CachedMediaFormat");
        AssertContains(deviceServiceFormatCacheText, "private static void TryLoadFormatCache(CaptureDevice device)");
        AssertContains(deviceServiceFormatProbeText, "public void BeginBackgroundFormatProbe(CaptureDevice device, long requestId = 0)");
        AssertContains(deviceServiceFormatProbeText, "private async Task<bool> QuerySupportedFormatsAsync(CaptureDevice device)");
        AssertContains(deviceServiceRootText, "private static void AttachBestAudioDevice(");
        AssertContains(deviceServiceRootText, "private static int ScoreAudioAssociation(");
        AssertEqual(
            false,
            File.Exists(Path.Combine(repoRoot, "Sussudio", "Services", "Capture", "DeviceService.AudioAssociation.cs")),
            "audio endpoint association folded into DeviceService.cs");
        AssertContains(deviceServiceRootText, "private static string? ResolveNativeXuInterfacePath(string deviceId)");
        AssertDoesNotContain(deviceServiceRootText, "private static void TryLoadFormatCache(CaptureDevice device)");
        AssertDoesNotContain(deviceServiceRootText, "private async Task<bool> QuerySupportedFormatsAsync(CaptureDevice device)");

        var nativeXuAtProviderText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "Services", "Telemetry", "NativeXuAtCommandProvider.cs"));
        var nativeXuAtRollingPollText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "Services", "Telemetry", "NativeXuAtCommandProvider.RollingPoll.cs"));
        var nativeXuDeviceSupportText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "Services", "Capture", "NativeXu", "NativeXuDeviceSupport.cs"));
        AssertContains(nativeXuAtProviderText, "device.NativeXuInterfacePath");
        AssertContains(nativeXuAtProviderText, "NativeXuDeviceSupport.TryGetSupported4kXIds(device, out var vendorId, out var productId)");
        AssertContains(nativeXuAtProviderText, "NativeXuDeviceSupport.EnumerateSelectedInterfaces(vendorId, productId, device)");
        AssertContains(nativeXuAtProviderText, "NativeXuDeviceSupport.TryAcquireTransportGateAsync(cancellationToken)");
        AssertDoesNotContain(nativeXuAtProviderText, "new KsExtensionUnitNative.KsInterfacePath(selectedInterfacePath, Guid.Empty)");
        AssertContains(nativeXuAtProviderText, "nativexu-interface-ambiguous");
        AssertDoesNotContain(nativeXuAtProviderText, "missing_selected_interface");
        AssertContains(nativeXuAtRollingPollText, "_rollingInterfacePath");
        AssertContains(nativeXuAtProviderText, "cancellationToken.ThrowIfCancellationRequested()");
        AssertContains(nativeXuDeviceSupportText, "internal static class NativeXuDeviceSupport");
        AssertContains(nativeXuDeviceSupportText, "public static readonly Guid ExtensionUnitGuid");
        AssertContains(nativeXuDeviceSupportText, "private static readonly SemaphoreSlim TransportGate");
        AssertContains(nativeXuDeviceSupportText, "public static IReadOnlyList<KsExtensionUnitNative.KsInterfacePath> EnumerateSelectedInterfaces(");
        AssertContains(nativeXuDeviceSupportText, "public static bool HasSelectedInterface(CaptureDevice? device, string operation)");
        AssertContains(nativeXuDeviceSupportText, "public static bool TryGetSupported4kXIds(");
        AssertContains(nativeXuDeviceSupportText, "public static bool TryParseVendorProductIds(");
        AssertContains(nativeXuDeviceSupportText, "public static bool IsSupported4kXDevice(");
        AssertContains(nativeXuDeviceSupportText, "new KsExtensionUnitNative.KsInterfacePath(selectedInterfacePath, Guid.Empty)");
        AssertContains(nativeXuDeviceSupportText, "return Array.Empty<KsExtensionUnitNative.KsInterfacePath>()");
        AssertContains(nativeXuDeviceSupportText, "missing_selected_interface");

        var nativeXuAudioServiceText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "Services", "Audio", "NativeXuAudioControlService.cs"));
        AssertContains(nativeXuAudioServiceText, "ReadPreferredPayloadAsync(device, cancellationToken)");
        AssertContains(nativeXuAudioServiceText, "device?.NativeXuInterfacePath");
        AssertContains(nativeXuAudioServiceText, "missing-selected-interface");
        AssertContains(nativeXuAudioServiceText, "NATIVEXU_AUDIO_PAYLOAD_READ missing-selected-interface");
        AssertContains(nativeXuAudioServiceText, "EnumerateCandidates(vendorId, productId, device?.NativeXuInterfacePath)");
        AssertContains(nativeXuAudioServiceText, "NativeXuDeviceSupport.EnumerateSelectedInterfacePath(selectedInterfacePath)");
        AssertContains(nativeXuAudioServiceText, "NativeXuDeviceSupport.TryAcquireTransportGateAsync(cancellationToken)");
        AssertContains(nativeXuAudioServiceText, "TryXuGetDirect(");
        AssertContains(nativeXuAudioServiceText, "TryXuSetViaOutput(");

        var cudaInteropInitializationText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "Services", "Gpu", "CudaD3D11Interop.Initialization.cs"));
        var cudaInteropCopyText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "Services", "Gpu", "CudaD3D11Interop.Copy.cs"));
        var cudaInteropNativeText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "Services", "Gpu", "CudaD3D11Interop.Native.cs"));
        AssertEqual(
            false,
            File.Exists(Path.Combine(repoRoot, "Sussudio", "Services", "Gpu", "CudaD3D11Interop.cs")),
            "CUDA/D3D11 interop state-only partial");
        AssertEqual(
            false,
            File.Exists(Path.Combine(repoRoot, "Sussudio", "Services", "Gpu", "CudaD3D11Interop.Lifetime.cs")),
            "CUDA/D3D11 interop lifetime is consolidated with bridge initialization");
        AssertContains(cudaInteropInitializationText, "internal sealed unsafe partial class CudaD3D11InteropBridge : IDisposable");
        AssertContains(cudaInteropInitializationText, "private static readonly object D3D11InteropLock");
        AssertContains(cudaInteropInitializationText, "public IntPtr TextureNativePointer");
        AssertContains(cudaInteropInitializationText, "public CudaD3D11InteropBridge(");
        AssertContains(cudaInteropInitializationText, "private bool TryInitializeZeroCopyResources");
        AssertContains(cudaInteropInitializationText, "CUDA_D3D11_INTEROP_CTX_INIT");
        AssertContains(cudaInteropInitializationText, "CUDA_D3D11_ZEROCOPY_REGISTER_OK");
        AssertDoesNotContain(cudaInteropInitializationText, "public void CopyFrameToTexture");
        AssertDoesNotContain(cudaInteropInitializationText, "DllImport(\"nvcuda.dll\")");
        AssertDoesNotContain(cudaInteropInitializationText, "private struct CUDA_MEMCPY2D");
        AssertContains(cudaInteropCopyText, "public void CopyFrameToTexture");
        AssertContains(cudaInteropCopyText, "private void CopyFrameZeroCopy");
        AssertContains(cudaInteropCopyText, "private void CopyFrameStaging");
        AssertContains(cudaInteropCopyText, "cuGraphicsMapResources");
        AssertContains(cudaInteropCopyText, "MapMode.Write");
        AssertContains(cudaInteropInitializationText, "public void Dispose()");
        AssertContains(cudaInteropInitializationText, "private void TryUnregisterResource");
        AssertContains(cudaInteropInitializationText, "cuDevicePrimaryCtxRelease");
        AssertContains(cudaInteropNativeText, "private const uint CU_MEMORYTYPE_DEVICE");
        AssertContains(cudaInteropNativeText, "DllImport(\"nvcuda.dll\")");
        AssertContains(cudaInteropNativeText, "private struct CUDA_MEMCPY2D");

        var nvdecInitializationText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "Services", "Gpu", "NvdecMjpegDecoder.Initialization.cs"));
        var nvdecSharedInitializationText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "Services", "Gpu", "NvdecMjpegDecoder.SharedInitialization.cs"));
        var nvdecDecodeText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "Services", "Gpu", "NvdecMjpegDecoder.Decode.cs"));
        var nvdecDownloadText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "Services", "Gpu", "NvdecMjpegDecoder.Download.cs"));
        AssertContains(nvdecInitializationText, "internal sealed unsafe partial class NvdecMjpegDecoder : IDisposable");
        AssertContains(nvdecInitializationText, "private AVCodecContext* _decoderCtx;");
        AssertDoesNotContain(nvdecInitializationText, "public AVFrame* DecodeFrame(");
        AssertDoesNotContain(nvdecInitializationText, "public bool TryDownloadToCpu(");
        AssertEqual(
            false,
            File.Exists(Path.Combine(repoRoot, "Sussudio", "Services", "Gpu", "NvdecMjpegDecoder.cs")),
            "NVDEC decoder state-only partial");
        AssertEqual(
            false,
            File.Exists(Path.Combine(repoRoot, "Sussudio", "Services", "Gpu", "NvdecMjpegDecoder.Lifetime.cs")),
            "NVDEC decoder lifetime folded into initialization/resource ownership");
        AssertContains(nvdecInitializationText, "public void Initialize(int width, int height)");
        AssertContains(nvdecInitializationText, "av_hwdevice_ctx_create(&hwDeviceCtx");
        AssertContains(nvdecInitializationText, "NVDEC_MJPEG_FRAMES_CTX_OK");
        AssertContains(nvdecSharedInitializationText, "public void Initialize(int width, int height, AVBufferRef* sharedHwDeviceCtx");
        AssertContains(nvdecSharedInitializationText, "ffmpeg.av_buffer_ref(sharedHwDeviceCtx)");
        AssertContains(nvdecSharedInitializationText, "NVDEC_MJPEG_DECODER_INIT_SHARED");
        AssertDoesNotContain(nvdecInitializationText, "public void Initialize(int width, int height, AVBufferRef* sharedHwDeviceCtx");
        AssertContains(nvdecInitializationText, "FfmpegRuntimeInit.EnsureInitialized");
        AssertContains(nvdecSharedInitializationText, "FfmpegRuntimeInit.EnsureInitialized");
        AssertContains(nvdecDecodeText, "public AVFrame* DecodeFrame(");
        AssertContains(nvdecDecodeText, "public IntPtr GetCudaContext()");
        AssertContains(nvdecDownloadText, "public bool TryDownloadToCpu(");
        AssertContains(nvdecDownloadText, "private void EnsurePackedBufferCapacity");
        AssertContains(nvdecDownloadText, "private static void CopyPlane");
        AssertContains(nvdecInitializationText, "public void Dispose()");
        AssertContains(nvdecInitializationText, "private static string GetErrorString");

        var captureServiceText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "Services", "Capture", "CaptureService.cs"));
        var captureServiceTelemetryText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "Services", "Capture", "CaptureService.Telemetry.cs"));
        AssertContains(captureServiceTelemetryText, "pollGeneration != Volatile.Read(ref _telemetryPollGeneration)");
        AssertContains(captureServiceText, "_telemetryPollSync");
        AssertContains(captureServiceTelemetryText, "lock (_telemetryPollSync)");
        AssertContains(captureServiceTelemetryText, "StartTelemetryPollCoreLocked");
        AssertContains(captureServiceTelemetryText, "StartTelemetryPollCore");
        AssertContains(captureServiceTelemetryText, "Telemetry poll start deferred until canceled poll exits");
        AssertContains(captureServiceTelemetryText, "private SourceSignalTelemetrySnapshot BuildFallbackTelemetry()");
        AssertContains(captureServiceTelemetryText, "private static SourceSignalTelemetrySnapshot MergeTelemetryWithFallback(");
    }
}

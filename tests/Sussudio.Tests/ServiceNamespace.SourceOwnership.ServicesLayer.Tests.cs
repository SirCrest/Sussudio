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
        var deviceServiceText =
            File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "Services", "Capture", "DeviceService.cs"))
            + "\n" + File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "Services", "Capture", "DeviceService.NativeXu.cs"));
        var deviceServiceRootText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "Services", "Capture", "DeviceService.cs"));
        var deviceServiceFormatCacheText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "Services", "Capture", "DeviceService.FormatCache.cs"));
        var deviceServiceFormatProbeText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "Services", "Capture", "DeviceService.FormatProbe.cs"));
        var deviceServiceAudioAssociationText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "Services", "Capture", "DeviceService.AudioAssociation.cs"));
        var deviceServiceNativeXuText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "Services", "Capture", "DeviceService.NativeXu.cs"));
        AssertContains(deviceServiceText, "NativeXuInterfacePath = ResolveNativeXuInterfacePath(videoDevice.SymbolicLink)");
        AssertContains(deviceServiceText, "Native XU interface resolution found no matching interface");
        AssertDoesNotContain(deviceServiceText, "SelectOnlyUnambiguousDeviceGroup");
        AssertContains(deviceServiceRootText, "public async Task<ObservableCollection<CaptureDevice>> EnumerateVideoCaptureDevicesAsync(");
        AssertContains(deviceServiceFormatCacheText, "internal sealed class CachedMediaFormat");
        AssertContains(deviceServiceFormatCacheText, "private static void TryLoadFormatCache(CaptureDevice device)");
        AssertContains(deviceServiceFormatProbeText, "public void BeginBackgroundFormatProbe(CaptureDevice device, long requestId = 0)");
        AssertContains(deviceServiceFormatProbeText, "private async Task<bool> QuerySupportedFormatsAsync(CaptureDevice device)");
        AssertContains(deviceServiceAudioAssociationText, "private static void AttachBestAudioDevice(");
        AssertContains(deviceServiceNativeXuText, "private static string? ResolveNativeXuInterfacePath(string deviceId)");
        AssertDoesNotContain(deviceServiceRootText, "private static void TryLoadFormatCache(CaptureDevice device)");
        AssertDoesNotContain(deviceServiceRootText, "private async Task<bool> QuerySupportedFormatsAsync(CaptureDevice device)");
        AssertDoesNotContain(deviceServiceRootText, "private static string? ResolveNativeXuInterfacePath(string deviceId)");

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

        var cudaInteropText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "Services", "Gpu", "CudaD3D11Interop.cs"));
        var cudaInteropInitializationText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "Services", "Gpu", "CudaD3D11Interop.Initialization.cs"));
        var cudaInteropCopyText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "Services", "Gpu", "CudaD3D11Interop.Copy.cs"));
        var cudaInteropLifetimeText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "Services", "Gpu", "CudaD3D11Interop.Lifetime.cs"));
        var cudaInteropNativeText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "Services", "Gpu", "CudaD3D11Interop.Native.cs"));
        AssertContains(cudaInteropText, "internal sealed unsafe partial class CudaD3D11InteropBridge");
        AssertDoesNotContain(cudaInteropText, "public CudaD3D11InteropBridge(");
        AssertDoesNotContain(cudaInteropText, "public void CopyFrameToTexture");
        AssertDoesNotContain(cudaInteropText, "public void Dispose()");
        AssertDoesNotContain(cudaInteropText, "TryInitializeZeroCopyResources");
        AssertDoesNotContain(cudaInteropText, "TryUnregisterResource");
        AssertDoesNotContain(cudaInteropText, "DllImport(\"nvcuda.dll\")");
        AssertDoesNotContain(cudaInteropText, "private struct CUDA_MEMCPY2D");
        AssertContains(cudaInteropInitializationText, "public CudaD3D11InteropBridge(");
        AssertContains(cudaInteropInitializationText, "private bool TryInitializeZeroCopyResources");
        AssertContains(cudaInteropInitializationText, "CUDA_D3D11_INTEROP_CTX_INIT");
        AssertContains(cudaInteropInitializationText, "CUDA_D3D11_ZEROCOPY_REGISTER_OK");
        AssertContains(cudaInteropCopyText, "public void CopyFrameToTexture");
        AssertContains(cudaInteropCopyText, "private void CopyFrameZeroCopy");
        AssertContains(cudaInteropCopyText, "private void CopyFrameStaging");
        AssertContains(cudaInteropCopyText, "cuGraphicsMapResources");
        AssertContains(cudaInteropCopyText, "MapMode.Write");
        AssertContains(cudaInteropLifetimeText, "public void Dispose()");
        AssertContains(cudaInteropLifetimeText, "private void TryUnregisterResource");
        AssertContains(cudaInteropLifetimeText, "cuDevicePrimaryCtxRelease");
        AssertContains(cudaInteropNativeText, "private const uint CU_MEMORYTYPE_DEVICE");
        AssertContains(cudaInteropNativeText, "DllImport(\"nvcuda.dll\")");
        AssertContains(cudaInteropNativeText, "private struct CUDA_MEMCPY2D");

        var nvdecText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "Services", "Gpu", "NvdecMjpegDecoder.cs"));
        var nvdecInitializationText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "Services", "Gpu", "NvdecMjpegDecoder.Initialization.cs"));
        var nvdecSharedInitializationText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "Services", "Gpu", "NvdecMjpegDecoder.SharedInitialization.cs"));
        var nvdecDecodeText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "Services", "Gpu", "NvdecMjpegDecoder.Decode.cs"));
        var nvdecDownloadText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "Services", "Gpu", "NvdecMjpegDecoder.Download.cs"));
        var nvdecLifetimeText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "Services", "Gpu", "NvdecMjpegDecoder.Lifetime.cs"));
        AssertContains(nvdecText, "internal sealed unsafe partial class NvdecMjpegDecoder");
        AssertDoesNotContain(nvdecText, "public void Initialize(");
        AssertDoesNotContain(nvdecText, "public AVFrame* DecodeFrame(");
        AssertDoesNotContain(nvdecText, "public bool TryDownloadToCpu(");
        AssertDoesNotContain(nvdecText, "public void Dispose()");
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
        AssertContains(nvdecLifetimeText, "public void Dispose()");
        AssertContains(nvdecLifetimeText, "private static string GetErrorString");

        var captureServiceText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "Services", "Capture", "CaptureService.cs"));
        var captureServiceTelemetryText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "Services", "Capture", "CaptureService.Telemetry.cs"));
        var captureServiceTelemetryFallbackText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "Services", "Capture", "CaptureService.TelemetryFallback.cs"));
        AssertContains(captureServiceTelemetryText, "pollGeneration != Volatile.Read(ref _telemetryPollGeneration)");
        AssertContains(captureServiceText, "_telemetryPollSync");
        AssertContains(captureServiceTelemetryText, "lock (_telemetryPollSync)");
        AssertContains(captureServiceTelemetryText, "StartTelemetryPollCoreLocked");
        AssertContains(captureServiceTelemetryText, "StartTelemetryPollCore");
        AssertContains(captureServiceTelemetryText, "Telemetry poll start deferred until canceled poll exits");
        AssertDoesNotContain(captureServiceTelemetryText, "private SourceSignalTelemetrySnapshot BuildFallbackTelemetry()");
        AssertDoesNotContain(captureServiceTelemetryText, "private static SourceSignalTelemetrySnapshot MergeTelemetryWithFallback(");
        AssertContains(captureServiceTelemetryFallbackText, "private SourceSignalTelemetrySnapshot BuildFallbackTelemetry()");
        AssertContains(captureServiceTelemetryFallbackText, "private static SourceSignalTelemetrySnapshot MergeTelemetryWithFallback(");
    }
}

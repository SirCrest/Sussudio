// Tests that prevent app service code from drifting into stale namespaces.
static partial class Program
{
    private static void AssertServiceNamespaceSourceOwnership(string repoRoot)
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
        AssertContains(nativeXuAtProviderText, "device?.NativeXuInterfacePath");
        AssertContains(nativeXuAtProviderText, "new KsExtensionUnitNative.KsInterfacePath(selectedInterfacePath, Guid.Empty)");
        AssertContains(nativeXuAtProviderText, "return Array.Empty<KsExtensionUnitNative.KsInterfacePath>()");
        AssertContains(nativeXuAtProviderText, "nativexu-interface-ambiguous");
        AssertContains(nativeXuAtProviderText, "missing_selected_interface");
        AssertContains(nativeXuAtRollingPollText, "_rollingInterfacePath");
        AssertContains(nativeXuAtProviderText, "cancellationToken.ThrowIfCancellationRequested()");
        AssertContains(nativeXuAtProviderText, "EnumerateKsInterfaces(vendorId, productId, device)");

        var nativeXuAudioServiceText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "Services", "Audio", "NativeXuAudioControlService.cs"));
        var nativeXuAudioTransportText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "Services", "Audio", "NativeXuAudioControlService.Transport.cs"));
        AssertContains(nativeXuAudioServiceText, "ReadPreferredPayloadAsync(device, cancellationToken)");
        AssertContains(nativeXuAudioTransportText, "device?.NativeXuInterfacePath");
        AssertContains(nativeXuAudioTransportText, "missing-selected-interface");
        AssertContains(nativeXuAudioTransportText, "NATIVEXU_AUDIO_PAYLOAD_READ missing-selected-interface");
        AssertContains(nativeXuAudioTransportText, "new KsExtensionUnitNative.KsInterfacePath(selectedInterfacePath, Guid.Empty)");
        AssertContains(nativeXuAudioTransportText, "EnumerateCandidates(vendorId, productId, device?.NativeXuInterfacePath)");

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
        var nvdecDecodeText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "Services", "Gpu", "NvdecMjpegDecoder.Decode.cs"));
        var nvdecDownloadText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "Services", "Gpu", "NvdecMjpegDecoder.Download.cs"));
        var nvdecLifetimeText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "Services", "Gpu", "NvdecMjpegDecoder.Lifetime.cs"));
        AssertContains(nvdecText, "internal sealed unsafe partial class NvdecMjpegDecoder");
        AssertDoesNotContain(nvdecText, "public void Initialize(");
        AssertDoesNotContain(nvdecText, "public AVFrame* DecodeFrame(");
        AssertDoesNotContain(nvdecText, "public bool TryDownloadToCpu(");
        AssertDoesNotContain(nvdecText, "public void Dispose()");
        AssertContains(nvdecInitializationText, "public void Initialize(int width, int height)");
        AssertContains(nvdecInitializationText, "public void Initialize(int width, int height, AVBufferRef* sharedHwDeviceCtx");
        AssertContains(nvdecInitializationText, "FfmpegRuntimeInit.EnsureInitialized");
        AssertContains(nvdecDecodeText, "public AVFrame* DecodeFrame(");
        AssertContains(nvdecDecodeText, "public IntPtr GetCudaContext()");
        AssertContains(nvdecDownloadText, "public bool TryDownloadToCpu(");
        AssertContains(nvdecDownloadText, "private void EnsurePackedBufferCapacity");
        AssertContains(nvdecDownloadText, "private static void CopyPlane");
        AssertContains(nvdecLifetimeText, "public void Dispose()");
        AssertContains(nvdecLifetimeText, "private static string GetErrorString");

        var captureServiceText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "Services", "Capture", "CaptureService.cs"));
        var captureServiceTelemetryText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "Services", "Capture", "CaptureService.Telemetry.cs"));
        AssertContains(captureServiceTelemetryText, "pollGeneration != Volatile.Read(ref _telemetryPollGeneration)");
        AssertContains(captureServiceText, "_telemetryPollSync");
        AssertContains(captureServiceTelemetryText, "lock (_telemetryPollSync)");
        AssertContains(captureServiceTelemetryText, "StartTelemetryPollCoreLocked");
        AssertContains(captureServiceTelemetryText, "StartTelemetryPollCore");
        AssertContains(captureServiceTelemetryText, "Telemetry poll start deferred until canceled poll exits");

        var audioControlsText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "ViewModels", "MainViewModel.AudioControls.cs"));
        AssertContains(audioControlsText, "RefreshDeviceAudioControlsAsync(");
        AssertContains(audioControlsText, "ReadStateAsync(device, cancellationToken)");
        AssertContains(audioControlsText, "Device audio mode failure readback ignored");
        AssertContains(audioControlsText, "failureState.Mode");
        AssertContains(audioControlsText, "failureState.AnalogGainPercent");
        AssertContains(audioControlsText, "private async Task<bool> ApplyDeviceAudioModeAsync");
        AssertContains(audioControlsText, "CaptureDevice? targetDevice = null");
        AssertContains(audioControlsText, "private async Task<bool> ApplyAnalogAudioGainAsync");
        AssertContains(audioControlsText, "IsCurrentSelectedDevice(device)");

        var mainViewModelText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "ViewModels", "MainViewModel.cs"));
        var mainViewModelAudioPropertyChangesText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "ViewModels", "MainViewModel.AudioPropertyChanges.cs"));
        var mainViewModelDispatchingText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "ViewModels", "MainViewModel.Dispatching.cs"));
        var mainViewModelRuntimeText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "ViewModels", "MainViewModel.Runtime.cs"));
        AssertContains(mainViewModelDispatchingText, "private bool EnqueueUiOperation");
        AssertContains(mainViewModelDispatchingText, "UI_OPERATION_SKIP op='{operationName}' reason=disposing");
        AssertContains(mainViewModelDispatchingText, "UI_OPERATION_SKIP op='{operationName}' reason=disposing_after_enqueue");
        AssertContains(mainViewModelDispatchingText, "UI_OPERATION_ENQUEUE_FAILED op='{operationName}'");
        AssertContains(mainViewModelDispatchingText, "INVOKE_UI_OPERATION_ENQUEUE_FAILED kind=async");
        AssertContains(mainViewModelDispatchingText, "INVOKE_UI_OPERATION_ENQUEUE_FAILED kind=value");
        AssertDoesNotContain(mainViewModelText, "private bool EnqueueUiOperation");
        AssertContains(mainViewModelAudioPropertyChangesText, "allowDuringDispose: true");
        AssertContains(mainViewModelRuntimeText, "CAPTURE_STATUS_UI_ENQUEUE_FAILED status='{status}'");
        AssertContains(mainViewModelRuntimeText, "CAPTURE_ERROR_UI_ENQUEUE_FAILED type={ex.GetType().Name} msg='{ex.Message}'");
        AssertDoesNotContain(mainViewModelText, "CAPTURE_STATUS_UI_ENQUEUE_FAILED status='{status}'");
        var deviceManagementText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "ViewModels", "MainViewModel.DeviceManagement.cs"));
        var deviceFormatProbesText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "ViewModels", "MainViewModel.DeviceFormatProbes.cs"));
        AssertContains(deviceManagementText, "CancelPendingAudioControlWork");
        AssertContains(deviceManagementText, "_deviceAudioModeCts");
        AssertContains(deviceManagementText, "_deviceAudioRefreshCts");
        AssertContains(deviceManagementText, "AUDIO_DEVICES_CHANGED_UI_ENQUEUE_FAILED");
        AssertContains(deviceFormatProbesText, "private void OnDeviceFormatProbeCompleted");
        AssertContains(deviceFormatProbesText, "FORMAT_PROBE_UI_ENQUEUE_FAILED deviceId='{e.DeviceId}' requestId={e.RequestId}");
        AssertDoesNotContain(deviceManagementText, "private void OnDeviceFormatProbeCompleted");
        AssertContains(
            File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "ViewModels", "MainViewModel.Telemetry.cs")),
            "SOURCE_TELEMETRY_UI_ENQUEUE_FAILED");
        var recordingCapabilityRefreshText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "ViewModels", "MainViewModel.RecordingCapabilityRefresh.cs"));
        AssertContains(recordingCapabilityRefreshText, "RECORDING_FORMATS_UI_ENQUEUE_FAILED");
        AssertContains(recordingCapabilityRefreshText, "SPLIT_ENCODE_MODES_UI_ENQUEUE_FAILED");
        AssertDoesNotContain(
            File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "ViewModels", "MainViewModel.Settings.cs")),
            "RECORDING_FORMATS_UI_ENQUEUE_FAILED");
        AssertContains(
            File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "Services", "Preview", "D3D11PreviewRenderer.Rendering.cs"))
            + "\n" + File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "Services", "Preview", "D3D11PreviewRenderer.Resources.cs"))
            + "\n" + File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "Services", "Preview", "D3D11PreviewRenderer.PanelBinding.cs")),
            "D3D_FIRST_FRAME_UI_ENQUEUE_FAILED");
        AssertContains(
            File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "Services", "Preview", "D3D11PreviewRenderer.Rendering.cs"))
            + "\n" + File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "Services", "Preview", "D3D11PreviewRenderer.Resources.cs"))
            + "\n" + File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "Services", "Preview", "D3D11PreviewRenderer.PanelBinding.cs")),
            "D3D11 preview swap chain unbind enqueue failed during cleanup.");
    }
}

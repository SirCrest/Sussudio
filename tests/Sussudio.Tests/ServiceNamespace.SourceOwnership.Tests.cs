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
        var deviceAudioModeText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "ViewModels", "MainViewModel.DeviceAudioMode.cs"));
        var deviceAudioRefreshText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "ViewModels", "MainViewModel.DeviceAudioRefresh.cs"));
        var analogAudioGainText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "ViewModels", "MainViewModel.AnalogAudioGain.cs"));
        AssertContains(deviceAudioRefreshText, "RefreshDeviceAudioControlsAsync(");
        AssertContains(deviceAudioRefreshText, "ReadStateAsync(device, cancellationToken)");
        AssertContains(deviceAudioRefreshText, "NATIVEXU_AUDIO_RESTORE_READ_ONLY");
        AssertDoesNotContain(audioControlsText, "RefreshDeviceAudioControlsAsync(");
        AssertContains(deviceAudioModeText, "Device audio mode failure readback ignored");
        AssertContains(deviceAudioModeText, "failureState.Mode");
        AssertContains(deviceAudioModeText, "failureState.AnalogGainPercent");
        AssertContains(deviceAudioModeText, "private async Task<bool> ApplyDeviceAudioModeAsync");
        AssertContains(deviceAudioModeText, "CaptureDevice? targetDevice = null");
        AssertContains(analogAudioGainText, "private async Task<bool> ApplyAnalogAudioGainAsync");
        AssertContains(analogAudioGainText, "NativeXuAtCommandProvider.SetAnalogGainAsync(device, gainByte, persistFlash: false, cancellationToken)");
        AssertContains(analogAudioGainText, "NativeXuAtCommandProvider.SetAnalogGainAsync(device, gainByte, persistFlash: true, token)");
        AssertDoesNotContain(audioControlsText, "private async Task<bool> ApplyAnalogAudioGainAsync");
        AssertDoesNotContain(audioControlsText, "private async Task<bool> ApplyDeviceAudioModeAsync");
        AssertContains(audioControlsText, "private bool IsCurrentSelectedDevice(CaptureDevice device)");
        AssertContains(deviceAudioModeText, "IsCurrentSelectedDevice(device)");
        AssertDoesNotContain(audioControlsText, "TryApplyAtDeviceAudioModeAsync");
        AssertDoesNotContain(audioControlsText, "SetInputSourceAsync");

        var mainViewModelText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "ViewModels", "MainViewModel.cs"));
        var mainViewModelAudioPropertyChangesText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "ViewModels", "MainViewModel.AudioPropertyChanges.cs"));
        var mainViewModelAudioInputPropertyChangesText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "ViewModels", "MainViewModel.AudioInputPropertyChanges.cs"));
        var mainViewModelMicrophonePropertyChangesText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "ViewModels", "MainViewModel.MicrophonePropertyChanges.cs"));
        var mainViewModelDeviceAudioPropertyChangesText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "ViewModels", "MainViewModel.DeviceAudioPropertyChanges.cs"));
        var mainViewModelCaptureModePropertyChangesText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "ViewModels", "MainViewModel.CaptureModePropertyChanges.cs"));
        var mainViewModelDispatchingText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "ViewModels", "MainViewModel.Dispatching.cs"));
        var mainViewModelRuntimeText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "ViewModels", "MainViewModel.Runtime.cs"));
        var mainViewModelRuntimeWiringText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "ViewModels", "MainViewModel.RuntimeWiring.cs"));
        var mainViewModelDiskSpacePresentationText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "ViewModels", "MainViewModel.DiskSpacePresentation.cs"));
        var outputDriveSpacePresentationBuilderText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "ViewModels", "OutputDriveSpacePresentationBuilder.cs"));
        var mainViewModelPowerResumeText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "ViewModels", "MainViewModel.PowerResume.cs"));
        var mainViewModelLiveSignalPresentationText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "ViewModels", "MainViewModel.LiveSignalPresentation.cs"));
        var mainViewModelCaptureRuntimeEventsText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "ViewModels", "MainViewModel.CaptureRuntimeEvents.cs"));
        var mainViewModelDisposalText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "ViewModels", "MainViewModel.Disposal.cs"));
        AssertContains(mainViewModelDispatchingText, "private bool EnqueueUiOperation");
        AssertContains(mainViewModelDispatchingText, "UI_OPERATION_SKIP op='{operationName}' reason=disposing");
        AssertContains(mainViewModelDispatchingText, "UI_OPERATION_SKIP op='{operationName}' reason=disposing_after_enqueue");
        AssertContains(mainViewModelDispatchingText, "UI_OPERATION_ENQUEUE_FAILED op='{operationName}'");
        AssertContains(mainViewModelDispatchingText, "INVOKE_UI_OPERATION_ENQUEUE_FAILED kind=async");
        AssertContains(mainViewModelDispatchingText, "INVOKE_UI_OPERATION_ENQUEUE_FAILED kind=value");
        AssertDoesNotContain(mainViewModelText, "private bool EnqueueUiOperation");
        AssertContains(mainViewModelAudioPropertyChangesText, "OnIsAudioEnabledChanged");
        AssertContains(mainViewModelAudioInputPropertyChangesText, "OnIsCustomAudioInputEnabledChanged");
        AssertContains(mainViewModelAudioInputPropertyChangesText, "OnSelectedAudioInputDeviceChanged");
        AssertContains(mainViewModelMicrophonePropertyChangesText, "OnIsMicrophoneEnabledChanged");
        AssertContains(mainViewModelMicrophonePropertyChangesText, "OnSelectedMicrophoneDeviceChanged");
        AssertContains(mainViewModelDeviceAudioPropertyChangesText, "OnSelectedDeviceAudioModeChanged");
        AssertContains(mainViewModelDeviceAudioPropertyChangesText, "OnAnalogAudioGainPercentChanged");
        AssertContains(mainViewModelDeviceAudioPropertyChangesText, "allowDuringDispose: true");
        AssertContains(mainViewModelCaptureModePropertyChangesText, "partial void OnSelectedResolutionChanged(string? value)");
        AssertContains(mainViewModelCaptureModePropertyChangesText, "partial void OnSelectedFormatChanged(MediaFormat? value)");
        AssertContains(mainViewModelCaptureModePropertyChangesText, "partial void OnSelectedVideoFormatChanged(string value)");
        AssertContains(mainViewModelCaptureModePropertyChangesText, "partial void OnMjpegDecoderCountChanged(int value)");
        AssertContains(mainViewModelCaptureModePropertyChangesText, "BuildCaptureSettings().UseMjpegHighFrameRateMode");
        AssertDoesNotContain(mainViewModelAudioPropertyChangesText, "OnSelectedDeviceAudioModeChanged");
        AssertDoesNotContain(mainViewModelAudioPropertyChangesText, "OnSelectedMicrophoneDeviceChanged");
        AssertDoesNotContain(mainViewModelAudioPropertyChangesText, "OnSelectedAudioInputDeviceChanged");
        AssertContains(mainViewModelRuntimeText, "private void SetupTimer()");
        AssertContains(mainViewModelRuntimeText, "UpdateDiskSpace();");
        AssertContains(mainViewModelDiskSpacePresentationText, "private void UpdateDiskSpace()");
        AssertContains(mainViewModelDiskSpacePresentationText, "DiskSpaceInfo = OutputDriveSpacePresentationBuilder.Build(OutputPath);");
        AssertContains(outputDriveSpacePresentationBuilderText, "new DriveInfo(Path.GetPathRoot(outputPath) ?? \"C:\");");
        AssertContains(outputDriveSpacePresentationBuilderText, "return $\"Free: {freeGb:F1} GB\";");
        AssertContains(outputDriveSpacePresentationBuilderText, "Suppressed exception in MainViewModel.RefreshDiskSpace");
        AssertContains(mainViewModelPowerResumeText, "private void OnSystemPowerModeChanged");
        AssertContains(mainViewModelPowerResumeText, "e.Mode != PowerModes.Resume");
        AssertContains(mainViewModelPowerResumeText, "ReinitializeDeviceAsync(\"system resume\")");
        AssertContains(mainViewModelLiveSignalPresentationText, "partial void OnIsPreviewingChanged(bool value)");
        AssertContains(mainViewModelLiveSignalPresentationText, "ResetLiveCaptureInfo();");
        AssertDoesNotContain(mainViewModelRuntimeText, "private void UpdateDiskSpace()");
        AssertDoesNotContain(mainViewModelRuntimeText, "private void OnSystemPowerModeChanged");
        AssertDoesNotContain(mainViewModelRuntimeText, "partial void OnIsPreviewingChanged(bool value)");
        AssertContains(mainViewModelRuntimeWiringText, "private void AttachRuntimeWiring()");
        AssertContains(mainViewModelRuntimeWiringText, "private void DetachRuntimeWiring()");
        AssertContains(mainViewModelRuntimeWiringText, "private void InitializeRuntimePresentation()");
        AssertContains(mainViewModelRuntimeWiringText, "_deviceService.FormatProbeCompleted += OnDeviceFormatProbeCompleted;");
        AssertContains(mainViewModelRuntimeWiringText, "_captureService.StatusChanged += OnCaptureStatusChanged;");
        AssertContains(mainViewModelRuntimeWiringText, "_captureService.ErrorOccurred += OnCaptureError;");
        AssertContains(mainViewModelRuntimeWiringText, "_captureService.PreCleanupRequested += OnCapturePreCleanupRequested;");
        AssertContains(mainViewModelRuntimeWiringText, "_captureService.FrameCaptured += OnFrameCaptured;");
        AssertContains(mainViewModelRuntimeWiringText, "_captureService.AudioLevelUpdated += OnAudioLevelUpdated;");
        AssertContains(mainViewModelRuntimeWiringText, "_captureService.MicrophoneAudioLevelUpdated += OnMicrophoneAudioLevelUpdated;");
        AssertContains(mainViewModelRuntimeWiringText, "_captureService.SourceTelemetryUpdated += OnSourceTelemetryUpdated;");
        AssertContains(mainViewModelRuntimeWiringText, "SystemEvents.PowerModeChanged += OnSystemPowerModeChanged;");
        AssertContains(mainViewModelRuntimeWiringText, "_audioDeviceWatcher.DevicesChanged += OnAudioDevicesChanged;");
        AssertContains(mainViewModelRuntimeWiringText, "_deviceService.FormatProbeCompleted -= OnDeviceFormatProbeCompleted;");
        AssertContains(mainViewModelRuntimeWiringText, "_captureService.StatusChanged -= OnCaptureStatusChanged;");
        AssertContains(mainViewModelRuntimeWiringText, "_captureService.ErrorOccurred -= OnCaptureError;");
        AssertContains(mainViewModelRuntimeWiringText, "_captureService.PreCleanupRequested -= OnCapturePreCleanupRequested;");
        AssertContains(mainViewModelRuntimeWiringText, "_captureService.FrameCaptured -= OnFrameCaptured;");
        AssertContains(mainViewModelRuntimeWiringText, "_captureService.AudioLevelUpdated -= OnAudioLevelUpdated;");
        AssertContains(mainViewModelRuntimeWiringText, "_captureService.MicrophoneAudioLevelUpdated -= OnMicrophoneAudioLevelUpdated;");
        AssertContains(mainViewModelRuntimeWiringText, "_captureService.SourceTelemetryUpdated -= OnSourceTelemetryUpdated;");
        AssertContains(mainViewModelRuntimeWiringText, "SystemEvents.PowerModeChanged -= OnSystemPowerModeChanged;");
        AssertContains(mainViewModelRuntimeWiringText, "_audioDeviceWatcher.DevicesChanged -= OnAudioDevicesChanged;");
        AssertContains(mainViewModelRuntimeWiringText, "ApplySourceTelemetrySnapshot(_latestSourceTelemetry, allowAutoRetarget: false);");
        AssertContains(mainViewModelRuntimeWiringText, "UpdateHdrRuntimeStatusFromCapture();");
        AssertContains(mainViewModelRuntimeWiringText, "UpdateLiveCaptureInfo();");
        AssertContains(mainViewModelRuntimeWiringText, "SetupTimer();");
        AssertContains(mainViewModelRuntimeWiringText, "UpdateDiskSpace();");
        AssertContains(mainViewModelDisposalText, "DetachRuntimeWiring();");
        AssertContains(mainViewModelDisposalText, "_audioDeviceWatcher.Dispose();");
        AssertDoesNotContain(mainViewModelDisposalText, "PowerModeChanged -=");
        AssertDoesNotContain(mainViewModelDisposalText, "AudioLevelUpdated -=");
        AssertDoesNotContain(mainViewModelDiskSpacePresentationText, "OnSystemPowerModeChanged");
        AssertDoesNotContain(mainViewModelDiskSpacePresentationText, "new DriveInfo(");
        AssertDoesNotContain(mainViewModelDiskSpacePresentationText, "Path.GetPathRoot(");
        AssertDoesNotContain(mainViewModelDiskSpacePresentationText, "Trace.TraceWarning(");
        AssertDoesNotContain(mainViewModelPowerResumeText, "private void UpdateDiskSpace()");
        AssertContains(mainViewModelCaptureRuntimeEventsText, "private void OnCaptureStatusChanged(object? sender, string status)");
        AssertContains(mainViewModelCaptureRuntimeEventsText, "private void OnCaptureError(object? sender, Exception ex)");
        AssertContains(mainViewModelCaptureRuntimeEventsText, "private void OnCapturePreCleanupRequested()");
        AssertContains(mainViewModelCaptureRuntimeEventsText, "CAPTURE_STATUS_UI_ENQUEUE_FAILED status='{status}'");
        AssertContains(mainViewModelCaptureRuntimeEventsText, "CAPTURE_ERROR_UI_ENQUEUE_FAILED type={ex.GetType().Name} msg='{ex.Message}'");
        AssertDoesNotContain(mainViewModelRuntimeText, "CAPTURE_STATUS_UI_ENQUEUE_FAILED status='{status}'");
        AssertDoesNotContain(mainViewModelRuntimeText, "private void OnCaptureError(object? sender, Exception ex)");
        AssertDoesNotContain(mainViewModelText, "CAPTURE_STATUS_UI_ENQUEUE_FAILED status='{status}'");
        var deviceManagementText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "ViewModels", "MainViewModel.DeviceManagement.cs"));
        var deviceSelectionText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "ViewModels", "MainViewModel.DeviceSelection.cs"));
        var audioControlCancellationText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "ViewModels", "MainViewModel.AudioControlCancellation.cs"));
        var audioDeviceDiscoveryText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "ViewModels", "MainViewModel.AudioDeviceDiscovery.cs"));
        var audioDeviceSelectionPolicyText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "ViewModels", "AudioDeviceSelectionPolicy.cs"));
        var deviceFormatProbesText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "ViewModels", "MainViewModel.DeviceFormatProbes.cs"));
        AssertContains(deviceManagementText, "public async Task RefreshDevicesAsync(CancellationToken cancellationToken = default)");
        AssertContains(deviceManagementText, "ReplaceCollection(Devices, devices.ToList());");
        AssertContains(deviceSelectionText, "partial void OnSelectedDeviceChanged(CaptureDevice? value)");
        AssertContains(deviceSelectionText, "CancelPendingAudioControlWork();");
        AssertContains(deviceSelectionText, "_deviceAudioRefreshCts");
        AssertContains(deviceSelectionText, "private void RebuildSelectedDeviceCapabilities(CaptureDevice? device, bool resetTelemetryState)");
        AssertContains(deviceSelectionText, "ApplySourceTelemetrySnapshot(");
        AssertContains(deviceSelectionText, "RebuildResolutionOptions();");
        AssertDoesNotContain(deviceManagementText, "partial void OnSelectedDeviceChanged");
        AssertDoesNotContain(deviceManagementText, "private void RebuildSelectedDeviceCapabilities");
        AssertDoesNotContain(deviceManagementText, "partial void OnSelectedResolutionChanged");
        AssertDoesNotContain(deviceManagementText, "partial void OnSelectedFormatChanged");
        AssertDoesNotContain(deviceManagementText, "partial void OnSelectedVideoFormatChanged");
        AssertDoesNotContain(deviceManagementText, "partial void OnMjpegDecoderCountChanged");
        AssertContains(audioControlCancellationText, "private void CancelPendingAudioControlWork()");
        AssertContains(audioControlCancellationText, "_gainFlashDebounceCts");
        AssertContains(audioControlCancellationText, "_gainXuDebounceCts");
        AssertContains(audioControlCancellationText, "_deviceAudioModeCts");
        AssertContains(audioControlCancellationText, "_deviceAudioRefreshCts");
        AssertDoesNotContain(deviceManagementText, "private void CancelPendingAudioControlWork()");
        AssertDoesNotContain(deviceManagementText, "_deviceAudioModeCts");
        AssertContains(audioDeviceDiscoveryText, "private void OnAudioDevicesChanged()");
        AssertContains(audioDeviceDiscoveryText, "private void ApplyStartupAudioDeviceScan(");
        AssertContains(audioDeviceDiscoveryText, "private async Task RefreshAudioDeviceListAsync()");
        AssertContains(audioDeviceDiscoveryText, "_pendingSavedAudioDeviceId = null;");
        AssertContains(audioDeviceDiscoveryText, "_pendingSavedMicrophoneDeviceId = null;");
        AssertContains(audioDeviceDiscoveryText, "AudioDeviceSelectionPolicy.SelectStartup(");
        AssertContains(audioDeviceDiscoveryText, "AudioDeviceSelectionPolicy.SelectRefresh(");
        AssertContains(audioDeviceDiscoveryText, "ReplaceCollection(AudioInputDevices, selection.AvailableDevices);");
        AssertContains(audioDeviceDiscoveryText, "ReplaceCollection(MicrophoneDevices, selection.AvailableDevices);");
        AssertContains(audioDeviceSelectionPolicyText, "internal static AudioDeviceSelection SelectStartup(");
        AssertContains(audioDeviceSelectionPolicyText, "internal static AudioDeviceSelection SelectRefresh(");
        AssertContains(audioDeviceSelectionPolicyText, "FilterOutCaptureCardAudio(audioDevices, captureCardAudioId)");
        AssertContains(audioDeviceSelectionPolicyText, "SelectByPreviousSavedOrFirst(availableDevices, previousMicrophoneId, savedMicrophoneId)");
        AssertDoesNotContain(audioDeviceSelectionPolicyText, "ReplaceCollection(");
        AssertDoesNotContain(audioDeviceSelectionPolicyText, "Logger.Log(");
        AssertContains(audioDeviceDiscoveryText, "AUDIO_DEVICES_CHANGED_UI_ENQUEUE_FAILED");
        AssertContains(deviceManagementText, "ApplyStartupAudioDeviceScan(");
        AssertDoesNotContain(deviceManagementText, "_pendingSavedAudioDeviceId = null;");
        AssertDoesNotContain(deviceManagementText, "_pendingSavedMicrophoneDeviceId = null;");
        AssertDoesNotContain(deviceManagementText, "AUDIO_DEVICES_CHANGED_UI_ENQUEUE_FAILED");
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
            + "\n" + File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "Services", "Preview", "D3D11PreviewRenderer.Present.cs"))
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

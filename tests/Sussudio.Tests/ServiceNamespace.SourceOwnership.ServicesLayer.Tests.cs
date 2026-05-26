// Tests that prevent app service code from drifting into stale namespaces.
static partial class Program
{
    private static void AssertServiceNamespaceSourceOwnership(string repoRoot)
    {
        AssertServiceNamespaceServicesLayerOwnership(repoRoot);
        AssertServiceNamespaceMainViewModelSourceOwnership(repoRoot);
    }

    private static void AssertServiceNamespaceMainViewModelSourceOwnership(string repoRoot)
    {
        AssertServiceNamespaceMainViewModelDeviceAudioSourceOwnership(repoRoot);
        AssertServiceNamespaceMainViewModelRuntimeSourceOwnership(repoRoot);
        AssertServiceNamespaceMainViewModelDeviceAndCaptureSourceOwnership(repoRoot);
    }

    private static void AssertServiceNamespaceMainViewModelDeviceAudioSourceOwnership(string repoRoot)
    {
        var audioStateText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "ViewModels", "MainViewModel.AudioState.cs"));
        var deviceAudioStateText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "ViewModels", "MainViewModel.DeviceAudioState.cs"));
        var deviceAudioModeText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "ViewModels", "MainViewModel.DeviceAudioState.cs"));
        var deviceAudioRefreshText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "ViewModels", "MainViewModel.DeviceAudioState.cs"));
        var deviceAudioRequestControllerText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "Controllers", "ViewModel", "MainViewModelDeviceAudioRequestController.cs"));
        AssertContains(deviceAudioStateText, "public partial ObservableCollection<string> AvailableDeviceAudioModes");
        AssertContains(deviceAudioStateText, "public partial bool IsDeviceAudioControlSupported");
        AssertContains(deviceAudioStateText, "public partial string SelectedDeviceAudioMode");
        AssertContains(deviceAudioStateText, "public partial double AnalogAudioGainPercent");
        AssertContains(deviceAudioStateText, "partial void OnSelectedDeviceAudioModeChanged(string value)");
        AssertContains(deviceAudioStateText, "partial void OnAnalogAudioGainPercentChanged(double value)");
        AssertContains(deviceAudioStateText, "private void RequestAnalogGainFlashPersist(CaptureDevice device, byte gainByte)");
        AssertDoesNotContain(audioStateText, "SelectedDeviceAudioMode");
        AssertDoesNotContain(audioStateText, "AnalogAudioGainPercent");
        AssertContains(deviceAudioRefreshText, "RefreshDeviceAudioControlsAsync(");
        AssertContains(deviceAudioRefreshText, "ReadStateAsync(device, cancellationToken)");
        AssertContains(deviceAudioRefreshText, "NATIVEXU_AUDIO_RESTORE_READ_ONLY");
        AssertContains(deviceAudioStateText, "RefreshDeviceAudioControlsAsync(");
        AssertContains(deviceAudioModeText, "Device audio mode failure readback ignored");
        AssertContains(deviceAudioModeText, "failureState.Mode");
        AssertContains(deviceAudioModeText, "failureState.AnalogGainPercent");
        AssertContains(deviceAudioModeText, "private async Task<bool> ApplyDeviceAudioModeAsync");
        AssertContains(deviceAudioModeText, "CaptureDevice? targetDevice = null");
        AssertContains(deviceAudioStateText, "private async Task<bool> ApplyAnalogAudioGainAsync");
        AssertContains(deviceAudioStateText, "NativeXuAtCommandProvider.SetAnalogGainAsync(device, gainByte, persistFlash: false, cancellationToken)");
        AssertContains(deviceAudioRequestControllerText, "namespace Sussudio.Controllers;");
        AssertContains(deviceAudioRequestControllerText, "internal sealed class MainViewModelDeviceAudioRequestController");
        AssertDoesNotContain(deviceAudioRequestControllerText, "partial class MainViewModelDeviceAudioRequestController");
        AssertContains(deviceAudioRequestControllerText, "internal sealed class MainViewModelDeviceAudioRequestControllerContext");
        AssertContains(deviceAudioRequestControllerText, "public void ScheduleAnalogGainFlashPersist(CaptureDevice device, byte gainByte)");
        AssertContains(deviceAudioRequestControllerText, "NativeXuAtCommandProvider.SetAnalogGainAsync(device, gainByte, persistFlash: true, token)");
        AssertContains(deviceAudioStateText, "private async Task<bool> ApplyDeviceAudioModeAsync");
        AssertContains(deviceAudioStateText, "private bool IsCurrentSelectedDevice(CaptureDevice device)");
        AssertContains(deviceAudioModeText, "IsCurrentSelectedDevice(device)");
        AssertDoesNotContain(deviceAudioStateText, "TryApplyAtDeviceAudioModeAsync");
        AssertDoesNotContain(deviceAudioStateText, "SetInputSourceAsync");
        AssertEqual(
            false,
            File.Exists(Path.Combine(repoRoot, "Sussudio", "ViewModels", "MainViewModel.AudioControls.cs")),
            "MainViewModel shared audio-control helper partial");
        AssertEqual(
            false,
            File.Exists(Path.Combine(repoRoot, "Sussudio", "ViewModels", "MainViewModel.AnalogAudioGain.cs")),
            "MainViewModel analog gain write partial");
        AssertEqual(
            false,
            File.Exists(Path.Combine(repoRoot, "Sussudio", "ViewModels", "MainViewModel.DeviceAudioRefresh.cs")),
            "MainViewModel device-audio refresh folded into device audio state");
        AssertEqual(
            false,
            File.Exists(Path.Combine(repoRoot, "Sussudio", "ViewModels", "MainViewModel.DeviceAudioMode.cs")),
            "MainViewModel device-audio mode folded into device audio state");
    }

    private static void AssertServiceNamespaceMainViewModelDeviceAndCaptureSourceOwnership(string repoRoot)
    {
        var mainViewModelText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "ViewModels", "MainViewModel.cs"));
        var deviceAudioRequestControllerText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "Controllers", "ViewModel", "MainViewModelDeviceAudioRequestController.cs"));
        var mainViewModelDeviceFormatProbeControllerText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "Controllers", "ViewModel", "MainViewModelDeviceFormatProbeController.cs"));
        var mainViewModelDeviceFormatProbeRetargetApplierText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "Controllers", "ViewModel", "MainViewModelDeviceFormatProbeRetargetApplier.cs"));
        var mainViewModelSourceTelemetryControllerText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "Controllers", "ViewModel", "MainViewModelSourceTelemetryController.cs"));
        var mainViewModelDisposalText = mainViewModelText;
        var mainViewModelDisposalControllerText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "Controllers", "ViewModel", "MainViewModelLifecycleController.cs"));
        var deviceRefreshControllerText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "Controllers", "ViewModel", "MainViewModelDeviceRefreshController.cs"));
        var deviceSelectionText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "ViewModels", "MainViewModel.CaptureSelection.cs"));
        var audioStateText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "ViewModels", "MainViewModel.AudioState.cs"));
        var audioDeviceSelectionPolicyText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "ViewModels", "ViewModelSelectionPolicies.cs"));
        AssertEqual(false, File.Exists(Path.Combine(repoRoot, "Sussudio", "ViewModels", "MainViewModel.DeviceSelection.cs")), "old device selection partial folded into capture selection owner");
        AssertContains(mainViewModelText, "public Task RefreshDevicesAsync(CancellationToken cancellationToken = default)");
        AssertContains(mainViewModelText, "=> _deviceRefreshController.RefreshDevicesAsync(cancellationToken);");
        AssertEqual(
            false,
            File.Exists(Path.Combine(repoRoot, "Sussudio", "ViewModels", "MainViewModel.DeviceManagement.cs")),
            "shallow MainViewModel device-management partial");
        AssertContains(deviceRefreshControllerText, "namespace Sussudio.Controllers;");
        AssertContains(deviceRefreshControllerText, "internal sealed class MainViewModelDeviceRefreshController");
        AssertContains(deviceRefreshControllerText, "internal sealed class MainViewModelDeviceRefreshControllerContext");
        AssertContains(deviceRefreshControllerText, "private readonly MainViewModelDeviceRefreshControllerContext _context;");
        AssertDoesNotContain(deviceRefreshControllerText, "private readonly MainViewModel _viewModel;");
        AssertDoesNotContain(deviceRefreshControllerText, "_viewModel.");
        AssertContains(deviceRefreshControllerText, "public async Task RefreshDevicesAsync(CancellationToken cancellationToken = default)");
        AssertContains(deviceRefreshControllerText, "_context.EnumerateCaptureDeviceDiscoveryAsync()");
        AssertContains(deviceRefreshControllerText, "_context.ReplaceDevices(devices.ToList());");
        AssertContains(deviceRefreshControllerText, "_context.BeginBackgroundFormatProbe(discoveredDevice, scanGeneration);");
        AssertContains(deviceRefreshControllerText, "private async Task ApplySuccessfulDeviceScanAsync");
        AssertContains(deviceRefreshControllerText, "await _previewLifecycleController.StartPreviewAsync(userInitiated: false, cancellationToken);");
        AssertDoesNotContain(deviceRefreshControllerText, "await _viewModel.StartPreviewAsync(userInitiated: false, cancellationToken);");
        AssertContains(deviceSelectionText, "partial void OnSelectedDeviceChanged(CaptureDevice? value)");
        AssertContains(deviceSelectionText, "CancelPendingAudioControlWork();");
        AssertContains(deviceSelectionText, "RequestDeviceAudioControlsRefresh(value);");
        AssertDoesNotContain(deviceSelectionText, "_deviceAudioRefreshCts");
        AssertContains(deviceSelectionText, "private void RebuildSelectedDeviceCapabilities(CaptureDevice? device, bool resetTelemetryState)");
        AssertContains(deviceSelectionText, "_sourceTelemetryController.ApplySourceTelemetrySnapshot(");
        AssertContains(deviceSelectionText, "RebuildResolutionOptions();");
        AssertDoesNotContain(mainViewModelText, "partial void OnSelectedDeviceChanged");
        AssertDoesNotContain(mainViewModelText, "private void RebuildSelectedDeviceCapabilities");
        AssertDoesNotContain(mainViewModelText, "MfDeviceEnumerator.EnumerateAudioCaptureEndpointsAsync");
        AssertDoesNotContain(mainViewModelText, "BeginBackgroundFormatProbe");
        AssertDoesNotContain(mainViewModelText, "partial void OnSelectedResolutionChanged");
        AssertDoesNotContain(mainViewModelText, "partial void OnSelectedFormatChanged");
        AssertDoesNotContain(mainViewModelText, "partial void OnSelectedVideoFormatChanged");
        AssertDoesNotContain(mainViewModelText, "partial void OnMjpegDecoderCountChanged");
        AssertContains(deviceAudioRequestControllerText, "namespace Sussudio.Controllers;");
        AssertContains(deviceAudioRequestControllerText, "public void CancelPendingAudioControlWork()");
        AssertContains(deviceAudioRequestControllerText, "_gainFlashDebounceCts");
        AssertContains(deviceAudioRequestControllerText, "_gainXuDebounceCts");
        AssertContains(deviceAudioRequestControllerText, "_deviceAudioModeCts");
        AssertContains(deviceAudioRequestControllerText, "_deviceAudioRefreshCts");
        AssertContains(deviceAudioRequestControllerText, "internal sealed class MainViewModelDeviceAudioRequestControllerContext");
        AssertContains(deviceAudioRequestControllerText, "private readonly MainViewModelDeviceAudioRequestControllerContext _context;");
        AssertDoesNotContain(deviceAudioRequestControllerText, "private readonly MainViewModel _viewModel;");
        AssertDoesNotContain(deviceAudioRequestControllerText, "_viewModel.");
        AssertContains(deviceAudioRequestControllerText, "public void HandleAnalogAudioGainPercentChanged(double value)");
        AssertContains(deviceAudioRequestControllerText, "NativeXuAtCommandProvider.SetAnalogGainAsync(device, gainByte, persistFlash: true, token)");
        AssertDoesNotContain(mainViewModelText, "private void CancelPendingAudioControlWork()");
        AssertDoesNotContain(mainViewModelText, "_deviceAudioModeCts");
        AssertDoesNotContain(mainViewModelDisposalText, "_gainFlashDebounceCts");
        AssertContains(mainViewModelDisposalControllerText, "_context.CancelPendingAudioControlWork();");
        AssertEqual(false, File.Exists(Path.Combine(repoRoot, "Sussudio", "ViewModels", "MainViewModel.AudioDeviceDiscovery.cs")), "audio endpoint discovery adapter folded into audio state owner");
        AssertContains(audioStateText, "private void OnAudioDevicesChanged()");
        AssertContains(audioStateText, "private void ApplyStartupAudioDeviceScan(");
        AssertContains(audioStateText, "private async Task RefreshAudioDeviceListAsync()");
        AssertContains(audioStateText, "_pendingSavedAudioDeviceId = null;");
        AssertContains(audioStateText, "_pendingSavedMicrophoneDeviceId = null;");
        AssertContains(audioStateText, "AudioDeviceSelectionPolicy.SelectStartup(");
        AssertContains(audioStateText, "AudioDeviceSelectionPolicy.SelectRefresh(");
        AssertContains(audioStateText, "ReplaceCollection(AudioInputDevices, selection.AvailableDevices);");
        AssertContains(audioStateText, "ReplaceCollection(MicrophoneDevices, selection.AvailableDevices);");
        AssertContains(audioDeviceSelectionPolicyText, "internal static AudioDeviceSelection SelectStartup(");
        AssertContains(audioDeviceSelectionPolicyText, "internal static AudioDeviceSelection SelectRefresh(");
        AssertContains(audioDeviceSelectionPolicyText, "FilterOutCaptureCardAudio(audioDevices, captureCardAudioId)");
        AssertContains(audioDeviceSelectionPolicyText, "SelectByPreviousSavedOrFirst(availableDevices, previousMicrophoneId, savedMicrophoneId)");
        AssertDoesNotContain(audioDeviceSelectionPolicyText, "ReplaceCollection(");
        AssertDoesNotContain(audioDeviceSelectionPolicyText, "Logger.Log(");
        AssertContains(audioStateText, "AUDIO_DEVICES_CHANGED_UI_ENQUEUE_FAILED");
        AssertContains(deviceRefreshControllerText, "ApplyStartupAudioDeviceScan(");
        AssertDoesNotContain(mainViewModelText, "_pendingSavedAudioDeviceId = null;");
        AssertDoesNotContain(mainViewModelText, "_pendingSavedMicrophoneDeviceId = null;");
        AssertDoesNotContain(mainViewModelText, "AUDIO_DEVICES_CHANGED_UI_ENQUEUE_FAILED");
        AssertContains(mainViewModelDeviceFormatProbeControllerText, "public void OnDeviceFormatProbeCompleted");
        AssertContains(mainViewModelDeviceFormatProbeControllerText, "FORMAT_PROBE_UI_ENQUEUE_FAILED deviceId='{e.DeviceId}' requestId={e.RequestId}");
        AssertContains(mainViewModelDeviceFormatProbeControllerText, "namespace Sussudio.Controllers;");
        AssertContains(mainViewModelDeviceFormatProbeControllerText, "internal sealed class MainViewModelDeviceFormatProbeController");
        AssertContains(mainViewModelDeviceFormatProbeControllerText, "internal sealed class MainViewModelDeviceFormatProbeControllerContext");
        AssertContains(mainViewModelDeviceFormatProbeControllerText, "private readonly MainViewModelDeviceFormatProbeControllerContext _context;");
        AssertDoesNotContain(mainViewModelDeviceFormatProbeControllerText, "private readonly MainViewModel _viewModel;");
        AssertDoesNotContain(mainViewModelDeviceFormatProbeControllerText, "_viewModel.");
        AssertContains(mainViewModelDeviceFormatProbeControllerText, "_retargetApplier.TryApplyDeviceFormatProbeRetarget(");
        AssertDoesNotContain(mainViewModelDeviceFormatProbeControllerText, "private bool TryApplyDeviceFormatProbeRetarget(");
        AssertContains(mainViewModelDeviceFormatProbeRetargetApplierText, "public bool TryApplyDeviceFormatProbeRetarget(");
        AssertContains(mainViewModelDeviceFormatProbeRetargetApplierText, "namespace Sussudio.Controllers;");
        AssertContains(mainViewModelDeviceFormatProbeRetargetApplierText, "internal sealed class MainViewModelDeviceFormatProbeRetargetApplier");
        AssertContains(mainViewModelDeviceFormatProbeRetargetApplierText, "internal sealed class MainViewModelDeviceFormatProbeRetargetApplierContext");
        AssertContains(mainViewModelDeviceFormatProbeRetargetApplierText, "private readonly MainViewModelDeviceFormatProbeRetargetApplierContext _context;");
        AssertDoesNotContain(mainViewModelDeviceFormatProbeRetargetApplierText, "private readonly MainViewModel _viewModel;");
        AssertDoesNotContain(mainViewModelDeviceFormatProbeRetargetApplierText, "_viewModel.");
        AssertContains(mainViewModelDeviceFormatProbeRetargetApplierText, "_context.GetCaptureRuntimeSnapshot();");
        AssertDoesNotContain(mainViewModelText, "private void OnDeviceFormatProbeCompleted");
        AssertContains(mainViewModelSourceTelemetryControllerText, "namespace Sussudio.Controllers;");
        AssertContains(mainViewModelSourceTelemetryControllerText, "internal sealed class MainViewModelSourceTelemetryController");
        AssertContains(mainViewModelSourceTelemetryControllerText, "namespace Sussudio.Controllers;");
        AssertContains(mainViewModelSourceTelemetryControllerText, "internal sealed class MainViewModelSourceTelemetryControllerContext");
        AssertContains(mainViewModelSourceTelemetryControllerText, "private readonly MainViewModelSourceTelemetryControllerContext _context;");
        AssertContains(mainViewModelSourceTelemetryControllerText, "public required Func<SourceSignalTelemetrySnapshot> GetLatestSourceTelemetry { get; init; }");
        AssertContains(mainViewModelSourceTelemetryControllerText, "public required Func<SourceSignalTelemetrySnapshot, DateTimeOffset, string> BuildSourceTelemetrySummary { get; init; }");
        AssertContains(mainViewModelSourceTelemetryControllerText, "public required Func<string?, bool> IsAutoResolutionValue { get; init; }");
        AssertContains(mainViewModelSourceTelemetryControllerText, "public required Action RebuildResolutionOptions { get; init; }");
        AssertDoesNotContain(mainViewModelSourceTelemetryControllerText, "private readonly MainViewModel _viewModel;");
        AssertDoesNotContain(mainViewModelSourceTelemetryControllerText, "_viewModel.");
        AssertContains(mainViewModelSourceTelemetryControllerText, "public void OnSourceTelemetryUpdated(object? sender, SourceSignalTelemetrySnapshot snapshot)");
        AssertContains(mainViewModelSourceTelemetryControllerText, "SOURCE_TELEMETRY_UI_ENQUEUE_FAILED");
        AssertContains(mainViewModelSourceTelemetryControllerText, "private int? _lastTelemetryAgeBucket;");
        AssertContains(mainViewModelSourceTelemetryControllerText, "_context.IsAutoResolutionValue(_context.GetSelectedResolution())");
        AssertContains(mainViewModelSourceTelemetryControllerText, "_context.RebuildResolutionOptions();");
        AssertEqual(
            false,
            File.Exists(Path.Combine(repoRoot, "Sussudio", "ViewModels", "MainViewModel.Telemetry.cs")),
            "old MainViewModel telemetry partial removed after controller extraction");
        var recordingCapabilityControllerText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "Controllers", "ViewModel", "MainViewModelRecordingCapabilityController.cs"));
        AssertContains(recordingCapabilityControllerText, "namespace Sussudio.Controllers;");
        AssertContains(recordingCapabilityControllerText, "internal sealed class MainViewModelRecordingCapabilityControllerContext");
        AssertContains(recordingCapabilityControllerText, "internal sealed class MainViewModelRecordingCapabilityController");
        AssertContains(recordingCapabilityControllerText, "private readonly MainViewModelRecordingCapabilityControllerContext _context;");
        AssertDoesNotContain(recordingCapabilityControllerText, "private readonly MainViewModel _viewModel;");
        AssertDoesNotContain(recordingCapabilityControllerText, "_viewModel.");
        AssertContains(recordingCapabilityControllerText, "RECORDING_FORMATS_UI_ENQUEUE_FAILED");
        AssertContains(recordingCapabilityControllerText, "SPLIT_ENCODE_MODES_UI_ENQUEUE_FAILED");
        AssertDoesNotContain(
            File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "ViewModels", "MainViewModel.SettingsPersistence.cs")),
            "RECORDING_FORMATS_UI_ENQUEUE_FAILED");
        AssertContains(
            File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "Services", "Preview", "D3D11PreviewRenderer.RenderPasses.cs"))
            + "\n" + File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "Services", "Preview", "D3D11PreviewRenderer.Diagnostics.cs"))
            + "\n" + File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "Services", "Preview", "D3D11PreviewRenderer.Resources.cs"))
            + "\n" + File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "Services", "Preview", "D3D11PreviewRenderer.PanelBinding.cs")),
            "D3D_FIRST_FRAME_UI_ENQUEUE_FAILED");
        AssertContains(
            File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "Services", "Preview", "D3D11PreviewRenderer.RenderPasses.cs"))
            + "\n" + File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "Services", "Preview", "D3D11PreviewRenderer.Resources.cs"))
            + "\n" + File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "Services", "Preview", "D3D11PreviewRenderer.PanelBinding.cs")),
            "D3D11 preview swap chain unbind enqueue failed during cleanup.");
    }

    private static void AssertServiceNamespaceServicesLayerOwnership(string repoRoot)
    {
        var deviceServiceText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "Services", "Capture", "DeviceService.cs"));
        var deviceServiceRootText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "Services", "Capture", "DeviceService.cs"));
        var deviceServiceFormatProbeText = deviceServiceText;
        AssertContains(deviceServiceText, "NativeXuInterfacePath = ResolveNativeXuInterfacePath(videoDevice.SymbolicLink)");
        AssertContains(deviceServiceText, "Native XU interface resolution found no matching interface");
        AssertDoesNotContain(deviceServiceText, "SelectOnlyUnambiguousDeviceGroup");
        AssertContains(deviceServiceRootText, "public async Task<ObservableCollection<CaptureDevice>> EnumerateVideoCaptureDevicesAsync(");
        AssertContains(deviceServiceRootText, "public class DeviceService");
        AssertDoesNotContain(deviceServiceRootText, "partial class DeviceService");
        AssertContains(deviceServiceFormatProbeText, "internal sealed class CachedMediaFormat");
        AssertContains(deviceServiceFormatProbeText, "private static void TryLoadFormatCache(CaptureDevice device)");
        AssertContains(deviceServiceFormatProbeText, "public void BeginBackgroundFormatProbe(CaptureDevice device, long requestId = 0)");
        AssertContains(deviceServiceFormatProbeText, "private async Task<bool> QuerySupportedFormatsAsync(CaptureDevice device)");
        AssertEqual(
            false,
            File.Exists(Path.Combine(repoRoot, "Sussudio", "Services", "Capture", "DeviceService.FormatProbe.cs")),
            "DeviceService format probing folded into DeviceService.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(repoRoot, "Sussudio", "Services", "Capture", "DeviceService.FormatCache.cs")),
            "DeviceService format cache folded into format probe owner");
        AssertContains(deviceServiceRootText, "private static void AttachBestAudioDevice(");
        AssertContains(deviceServiceRootText, "private static int ScoreAudioAssociation(");
        AssertEqual(
            false,
            File.Exists(Path.Combine(repoRoot, "Sussudio", "Services", "Capture", "DeviceService.AudioAssociation.cs")),
            "audio endpoint association folded into DeviceService.cs");
        AssertContains(deviceServiceRootText, "private static string? ResolveNativeXuInterfacePath(string deviceId)");

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
        AssertContains(cudaInteropInitializationText, "DllImport(\"nvcuda.dll\")");
        AssertContains(cudaInteropInitializationText, "private struct CUDA_MEMCPY2D");
        AssertContains(cudaInteropCopyText, "public void CopyFrameToTexture");
        AssertContains(cudaInteropCopyText, "private void CopyFrameZeroCopy");
        AssertContains(cudaInteropCopyText, "private void CopyFrameStaging");
        AssertContains(cudaInteropCopyText, "cuGraphicsMapResources");
        AssertContains(cudaInteropCopyText, "MapMode.Write");
        AssertContains(cudaInteropInitializationText, "public void Dispose()");
        AssertContains(cudaInteropInitializationText, "private void TryUnregisterResource");
        AssertContains(cudaInteropInitializationText, "cuDevicePrimaryCtxRelease");
        AssertContains(cudaInteropInitializationText, "private const uint CU_MEMORYTYPE_DEVICE");
        AssertEqual(
            false,
            File.Exists(Path.Combine(repoRoot, "Sussudio", "Services", "Gpu", "CudaD3D11Interop.Native.cs")),
            "CUDA/D3D11 native declarations folded into bridge initialization");

        var nvdecText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "Services", "Gpu", "NvdecMjpegDecoder.cs"));
        AssertContains(nvdecText, "internal sealed unsafe class NvdecMjpegDecoder : IDisposable");
        AssertContains(nvdecText, "private AVCodecContext* _decoderCtx;");
        AssertEqual(
            false,
            File.Exists(Path.Combine(repoRoot, "Sussudio", "Services", "Gpu", "NvdecMjpegDecoder.Initialization.cs")),
            "NVDEC decoder initialization folded into cohesive decoder owner");
        AssertEqual(
            false,
            File.Exists(Path.Combine(repoRoot, "Sussudio", "Services", "Gpu", "NvdecMjpegDecoder.SharedInitialization.cs")),
            "NVDEC shared-context initialization folded into cohesive decoder owner");
        AssertEqual(
            false,
            File.Exists(Path.Combine(repoRoot, "Sussudio", "Services", "Gpu", "NvdecMjpegDecoder.Decode.cs")),
            "NVDEC packet decode folded into cohesive decoder owner");
        AssertEqual(
            false,
            File.Exists(Path.Combine(repoRoot, "Sussudio", "Services", "Gpu", "NvdecMjpegDecoder.Download.cs")),
            "NVDEC CPU download folded into cohesive decoder owner");
        AssertEqual(
            false,
            File.Exists(Path.Combine(repoRoot, "Sussudio", "Services", "Gpu", "NvdecMjpegDecoder.Lifetime.cs")),
            "NVDEC decoder lifetime folded into initialization/resource ownership");
        AssertContains(nvdecText, "public void Initialize(int width, int height)");
        AssertContains(nvdecText, "av_hwdevice_ctx_create(&hwDeviceCtx");
        AssertContains(nvdecText, "NVDEC_MJPEG_FRAMES_CTX_OK");
        AssertContains(nvdecText, "public void Initialize(int width, int height, AVBufferRef* sharedHwDeviceCtx");
        AssertContains(nvdecText, "ffmpeg.av_buffer_ref(sharedHwDeviceCtx)");
        AssertContains(nvdecText, "NVDEC_MJPEG_DECODER_INIT_SHARED");
        AssertContains(nvdecText, "FfmpegRuntimeInit.EnsureInitialized");
        AssertContains(nvdecText, "public AVFrame* DecodeFrame(");
        AssertContains(nvdecText, "public IntPtr GetCudaContext()");
        AssertContains(nvdecText, "public bool TryDownloadToCpu(");
        AssertContains(nvdecText, "private void EnsurePackedBufferCapacity");
        AssertContains(nvdecText, "private static void CopyPlane");
        AssertContains(nvdecText, "public void Dispose()");
        AssertContains(nvdecText, "private static string GetErrorString");

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

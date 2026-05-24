// Tests that keep MainViewModel device, capture, and source-telemetry ownership from drifting back into catch-all partials.
static partial class Program
{
    private static void AssertServiceNamespaceMainViewModelDeviceAndCaptureSourceOwnership(string repoRoot)
    {
        var mainViewModelText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "ViewModels", "MainViewModel.cs"));
        var deviceAudioRequestControllerText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "Controllers", "ViewModel", "MainViewModelDeviceAudioRequestController.cs"));
        var mainViewModelDeviceFormatProbeControllerText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "Controllers", "ViewModel", "MainViewModelDeviceFormatProbeController.cs"));
        var mainViewModelDeviceFormatProbeRetargetApplierText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "Controllers", "ViewModel", "MainViewModelDeviceFormatProbeRetargetApplier.cs"));
        var mainViewModelSourceTelemetryControllerText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "Controllers", "ViewModel", "MainViewModelSourceTelemetryController.cs"));
        var mainViewModelDisposalText = mainViewModelText;
        var mainViewModelDisposalControllerText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "Controllers", "ViewModel", "MainViewModelDisposalController.cs"));
        var deviceRefreshControllerText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "Controllers", "ViewModel", "MainViewModelDeviceRefreshController.cs"));
        var deviceSelectionText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "ViewModels", "MainViewModel.CaptureSelection.cs"));
        var audioStateText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "ViewModels", "MainViewModel.AudioState.cs"));
        var audioDeviceSelectionPolicyText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "ViewModels", "AudioDeviceSelectionPolicy.cs"));
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
}

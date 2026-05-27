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

    private static void AssertServiceNamespaceMainViewModelRuntimeSourceOwnership(string repoRoot)
    {
        var mainViewModelText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "ViewModels", "MainViewModel.cs"));
        var mainViewModelAudioCapturePropertyChangesText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "ViewModels", "MainViewModel.AudioState.cs"));
        var mainViewModelAudioStateText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "ViewModels", "MainViewModel.AudioState.cs"));
        var mainViewModelDeviceAudioRequestControllerText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "Controllers", "ViewModel", "MainViewModelDeviceAudioRequestController.cs"));
        var mainViewModelCaptureModePropertyChangesText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "ViewModels", "MainViewModel.CaptureSelection.cs"));
        var mainViewModelCompositionText = mainViewModelText;
        var mainViewModelUiDispatchControllerText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "Controllers", "ViewModel", "MainViewModelUiDispatchController.cs"));
        var mainViewModelRuntimeLifecycleControllerText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "Controllers", "ViewModel", "MainViewModelLifecycleController.cs"));
        var mainViewModelRuntimeEventIngressControllerText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "Controllers", "ViewModel", "MainViewModelRuntimeEventIngressController.cs"));
        var mainViewModelDisposalControllerText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "Controllers", "ViewModel", "MainViewModelLifecycleController.cs"));
        var mainViewModelRecordingStateText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "ViewModels", "MainViewModel.cs"));
        var mainViewModelRecordingRuntimeText = mainViewModelRecordingStateText;
        var outputDriveSpacePresentationBuilderText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "ViewModels", "ViewModelBuilders.cs"));
        var mainViewModelCapturePresentationText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "ViewModels", "MainViewModel.cs"));
        var mainViewModelDisposalText = mainViewModelText;
        AssertEqual(
            false,
            File.Exists(Path.Combine(repoRoot, "Sussudio", "ViewModels", "MainViewModel.Dispatching.cs")),
            "MainViewModel dispatch adapter partial folded into MainViewModel.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(repoRoot, "Sussudio", "ViewModels", "MainViewModel.Composition.cs")),
            "MainViewModel composition partial folded into MainViewModel.cs");
        AssertContains(mainViewModelCompositionText, "private bool EnqueueUiOperation");
        AssertContains(mainViewModelCompositionText, "_uiDispatchController.Enqueue(operation, operationName, allowDuringDispose);");
        AssertContains(mainViewModelCompositionText, "_uiDispatchController.InvokeAsync(operation, cancellationToken);");
        AssertContains(mainViewModelCompositionText, "private async Task NotifyPreviewReinitRequestedAsync(string reason)");
        AssertContains(mainViewModelCompositionText, "private static async Task AwaitWithTimeoutAsync(Task task, int timeoutMs, string operationName)");
        AssertContains(mainViewModelUiDispatchControllerText, "internal sealed class MainViewModelUiDispatchControllerContext");
        AssertContains(mainViewModelUiDispatchControllerText, "internal sealed class MainViewModelUiDispatchController");
        AssertContains(mainViewModelUiDispatchControllerText, "public bool Enqueue(Func<Task> operation, string operationName, bool allowDuringDispose = false)");
        AssertContains(mainViewModelUiDispatchControllerText, "UI_OPERATION_SKIP op='{operationName}' reason=disposing");
        AssertContains(mainViewModelUiDispatchControllerText, "UI_OPERATION_SKIP op='{operationName}' reason=disposing_after_enqueue");
        AssertContains(mainViewModelUiDispatchControllerText, "UI_OPERATION_ENQUEUE_FAILED op='{operationName}'");
        AssertContains(mainViewModelUiDispatchControllerText, "INVOKE_UI_OPERATION_ENQUEUE_FAILED kind=async");
        AssertContains(mainViewModelUiDispatchControllerText, "INVOKE_UI_OPERATION_ENQUEUE_FAILED kind=value");
        AssertContains(mainViewModelUiDispatchControllerText, "TaskCreationOptions.RunContinuationsAsynchronously");
        AssertContains(mainViewModelUiDispatchControllerText, "_context.DispatcherQueue.HasThreadAccess");
        AssertContains(mainViewModelUiDispatchControllerText, "_context.SetStatusText($\"{operationName} failed: {ex.Message}\");");
        AssertDoesNotContain(mainViewModelCompositionText, "TaskCompletionSource");
        AssertDoesNotContain(mainViewModelCompositionText, "_dispatcherQueue.TryEnqueue");
        AssertContains(mainViewModelText, "private bool EnqueueUiOperation");
        AssertContains(mainViewModelAudioCapturePropertyChangesText, "OnIsAudioEnabledChanged");
        AssertContains(mainViewModelAudioStateText, "OnIsAudioPreviewEnabledChanged");
        AssertEqual(
            false,
            File.Exists(Path.Combine(repoRoot, "Sussudio", "ViewModels", "MainViewModel.AudioCapturePropertyChanges.cs")),
            "MainViewModel.AudioCapturePropertyChanges.cs folded into MainViewModel.AudioState.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(repoRoot, "Sussudio", "ViewModels", "MainViewModel.AudioPreviewPropertyChanges.cs")),
            "MainViewModel audio-preview property-change partial");
        AssertEqual(
            false,
            File.Exists(Path.Combine(repoRoot, "Sussudio", "ViewModels", "MainViewModel.AudioPropertyChanges.cs")),
            "MainViewModel legacy audio property-change partial");
        AssertContains(mainViewModelAudioStateText, "OnIsCustomAudioInputEnabledChanged");
        AssertContains(mainViewModelAudioStateText, "OnSelectedAudioInputDeviceChanged");
        AssertContains(mainViewModelAudioStateText, "private async Task ApplyAudioInputSelectionAsync");
        AssertEqual(
            false,
            File.Exists(Path.Combine(repoRoot, "Sussudio", "ViewModels", "MainViewModel.AudioInputPropertyChanges.cs")),
            "MainViewModel audio-input property-change partial");
        AssertEqual(
            false,
            File.Exists(Path.Combine(repoRoot, "Sussudio", "ViewModels", "MainViewModel.AudioInputSelection.cs")),
            "MainViewModel.AudioInputSelection.cs folded into MainViewModel.AudioState.cs");
        AssertContains(mainViewModelAudioStateText, "OnIsMicrophoneEnabledChanged");
        AssertContains(mainViewModelAudioStateText, "OnSelectedMicrophoneDeviceChanged");
        AssertContains(mainViewModelAudioStateText, "OnMicrophoneVolumeChanged");
        AssertContains(mainViewModelAudioStateText, "SetMicrophoneEndpointVolume");
        AssertEqual(
            false,
            File.Exists(Path.Combine(repoRoot, "Sussudio", "ViewModels", "MainViewModel.MicrophonePropertyChanges.cs")),
            "MainViewModel.MicrophonePropertyChanges.cs folded into MainViewModel.AudioState.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(repoRoot, "Sussudio", "ViewModels", "MainViewModel.MicrophoneVolume.cs")),
            "MainViewModel.MicrophoneVolume.cs folded into MainViewModel.AudioState.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(repoRoot, "Sussudio", "ViewModels", "MainViewModel.DeviceAudioRequests.cs")),
            "MainViewModel device audio request adapter partial");
        AssertContains(
            File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "ViewModels", "MainViewModel.DeviceAudioState.cs")),
            "partial void OnSelectedDeviceAudioModeChanged(string value)");
        AssertContains(
            File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "ViewModels", "MainViewModel.DeviceAudioState.cs")),
            "partial void OnAnalogAudioGainPercentChanged(double value)");
        AssertContains(mainViewModelDeviceAudioRequestControllerText, "public void RequestDeviceAudioControlsRefresh(CaptureDevice? targetDevice)");
        AssertContains(mainViewModelDeviceAudioRequestControllerText, "\"device audio controls refresh\", true");
        AssertContains(mainViewModelDeviceAudioRequestControllerText, "namespace Sussudio.Controllers;");
        AssertContains(mainViewModelDeviceAudioRequestControllerText, "internal sealed class MainViewModelDeviceAudioRequestController");
        AssertDoesNotContain(mainViewModelDeviceAudioRequestControllerText, "partial class MainViewModelDeviceAudioRequestController");
        AssertContains(mainViewModelDeviceAudioRequestControllerText, "internal sealed class MainViewModelDeviceAudioRequestControllerContext");
        AssertContains(mainViewModelDeviceAudioRequestControllerText, "private readonly MainViewModelDeviceAudioRequestControllerContext _context;");
        AssertDoesNotContain(mainViewModelDeviceAudioRequestControllerText, "private readonly MainViewModel _viewModel;");
        AssertDoesNotContain(mainViewModelDeviceAudioRequestControllerText, "_viewModel.");
        AssertContains(mainViewModelCaptureModePropertyChangesText, "partial void OnSelectedResolutionChanged(string? value)");
        AssertContains(mainViewModelCaptureModePropertyChangesText, "partial void OnSelectedFormatChanged(MediaFormat? value)");
        AssertContains(mainViewModelCaptureModePropertyChangesText, "partial void OnSelectedVideoFormatChanged(string value)");
        AssertContains(mainViewModelCaptureModePropertyChangesText, "partial void OnMjpegDecoderCountChanged(int value)");
        AssertContains(mainViewModelCaptureModePropertyChangesText, "BuildCaptureSettings().UseMjpegHighFrameRateMode");
        AssertEqual(
            false,
            File.Exists(Path.Combine(repoRoot, "Sussudio", "ViewModels", "MainViewModel.CaptureModePropertyChanges.cs")),
            "MainViewModel.CaptureModePropertyChanges.cs folded into MainViewModel.CaptureSelection.cs");
        AssertDoesNotContain(mainViewModelAudioCapturePropertyChangesText, "OnSelectedDeviceAudioModeChanged");
        AssertDoesNotContain(mainViewModelAudioStateText, "OnSelectedDeviceAudioModeChanged");
        AssertContains(mainViewModelAudioStateText, "SetAudioMonitoringEnabledWithVolumeTransitionAsync");
        AssertEqual(
            false,
            File.Exists(Path.Combine(repoRoot, "Sussudio", "ViewModels", "MainViewModel.AudioMonitoring.cs")),
            "MainViewModel.AudioMonitoring.cs folded into MainViewModel.AudioState.cs");
        AssertContains(mainViewModelRuntimeLifecycleControllerText, "private void SetupTimer()");
        AssertContains(mainViewModelRuntimeLifecycleControllerText, "namespace Sussudio.Controllers;");
        AssertContains(mainViewModelRuntimeLifecycleControllerText, "internal sealed class MainViewModelRuntimeLifecycleController");
        AssertContains(mainViewModelRuntimeLifecycleControllerText, "internal sealed class MainViewModelRuntimeLifecycleControllerContext");
        AssertContains(mainViewModelRuntimeLifecycleControllerText, "private readonly MainViewModelRuntimeLifecycleControllerContext _context;");
        AssertDoesNotContain(mainViewModelRuntimeLifecycleControllerText, "private readonly MainViewModel _viewModel;");
        AssertDoesNotContain(mainViewModelRuntimeLifecycleControllerText, "_viewModel.");
        AssertContains(mainViewModelRuntimeLifecycleControllerText, "_context.UpdateDiskSpace();");
        AssertContains(mainViewModelRecordingStateText, "public Task ToggleRecordingAsync()");
        AssertContains(mainViewModelRecordingStateText, "internal Task SetRecordingDesiredStateAsync");
        AssertContains(mainViewModelRecordingStateText, "public Task StopRecordingAndWaitAsync(CancellationToken cancellationToken = default)");
        AssertContains(mainViewModelRecordingStateText, "internal Task StopRecordingForEmergencyAsync(CancellationToken cancellationToken = default)");
        AssertContains(mainViewModelText, "public Task ToggleRecordingAsync()");
        AssertContains(mainViewModelText, "internal Task SetRecordingDesiredStateAsync");
        AssertContains(mainViewModelRecordingRuntimeText, "private void UpdateDiskSpace()");
        AssertContains(mainViewModelRecordingRuntimeText, "DiskSpaceInfo = OutputDriveSpacePresentationBuilder.Build(OutputPath);");
        AssertContains(mainViewModelRecordingRuntimeText, "_recordingBitrateSamples.AddSampleAndCompute(now, totalBytes);");
        AssertEqual(
            false,
            File.Exists(Path.Combine(repoRoot, "Sussudio", "ViewModels", "MainViewModel.RecordingRuntime.cs")),
            "MainViewModel.RecordingRuntime.cs folded into MainViewModel.cs");
        AssertContains(mainViewModelRecordingStateText, "internal sealed class BitrateSampleWindow");
        AssertContains(mainViewModelRecordingStateText, "private readonly Queue<(long Tick, long Bytes)> _samples = new();");
        AssertEqual(
            false,
            File.Exists(Path.Combine(repoRoot, "Sussudio", "ViewModels", "BitrateSampleWindow.cs")),
            "BitrateSampleWindow folded into MainViewModel.cs");
        AssertContains(outputDriveSpacePresentationBuilderText, "new DriveInfo(Path.GetPathRoot(outputPath) ?? \"C:\");");
        AssertContains(outputDriveSpacePresentationBuilderText, "return $\"Free: {freeGb:F1} GB\";");
        AssertContains(outputDriveSpacePresentationBuilderText, "Suppressed exception in MainViewModel.RefreshDiskSpace");
        AssertContains(mainViewModelRuntimeEventIngressControllerText, "private void OnSystemPowerModeChanged");
        AssertContains(mainViewModelRuntimeEventIngressControllerText, "e.Mode != PowerModes.Resume");
        AssertContains(mainViewModelRuntimeLifecycleControllerText, "_eventIngressController = _context.CreateEventIngressController();");
        AssertContains(mainViewModelRuntimeEventIngressControllerText, "_context.ReinitializeDeviceAsync(\"audio device invalidated\")");
        AssertContains(mainViewModelRuntimeEventIngressControllerText, "_context.ReinitializeDeviceAsync(\"system resume\")");
        AssertDoesNotContain(mainViewModelRuntimeEventIngressControllerText, "_viewModel.ReinitializeDeviceAsync(\"system resume\")");
        AssertContains(mainViewModelCapturePresentationText, "partial void OnIsPreviewingChanged(bool value)");
        AssertContains(mainViewModelCapturePresentationText, "ResetLiveCaptureInfo();");
        AssertEqual(
            false,
            File.Exists(Path.Combine(repoRoot, "Sussudio", "ViewModels", "MainViewModel.CapturePresentation.cs")),
            "MainViewModel.CapturePresentation.cs folded into capture state");
        AssertDoesNotContain(mainViewModelRuntimeLifecycleControllerText, "private void UpdateDiskSpace()");
        AssertDoesNotContain(mainViewModelRuntimeLifecycleControllerText, "partial void OnIsPreviewingChanged(bool value)");
        AssertContains(mainViewModelRuntimeLifecycleControllerText, "public void Start()");
        AssertContains(mainViewModelRuntimeLifecycleControllerText, "=> _eventIngressController.Attach();");
        AssertContains(mainViewModelRuntimeLifecycleControllerText, "_eventIngressController.Detach();");
        AssertContains(mainViewModelRuntimeLifecycleControllerText, "public void InitializePresentation()");
        AssertContains(mainViewModelRuntimeEventIngressControllerText, "namespace Sussudio.Controllers;");
        AssertContains(mainViewModelRuntimeEventIngressControllerText, "internal sealed class MainViewModelRuntimeEventIngressController");
        AssertDoesNotContain(mainViewModelRuntimeEventIngressControllerText, "partial class MainViewModelRuntimeEventIngressController");
        AssertContains(mainViewModelRuntimeEventIngressControllerText, "internal sealed class MainViewModelRuntimeEventIngressControllerContext");
        AssertContains(mainViewModelRuntimeEventIngressControllerText, "private readonly MainViewModelRuntimeEventIngressControllerContext _context;");
        AssertContains(mainViewModelRuntimeEventIngressControllerText, "public required Func<CaptureRuntimeSnapshot> GetRuntimeSnapshot { get; init; }");
        AssertContains(mainViewModelRuntimeEventIngressControllerText, "public required Func<Func<Task>, string, bool> EnqueueUiOperation { get; init; }");
        AssertDoesNotContain(mainViewModelRuntimeEventIngressControllerText, "private readonly MainViewModel _viewModel;");
        AssertDoesNotContain(mainViewModelRuntimeEventIngressControllerText, "_viewModel.");
        AssertContains(mainViewModelRuntimeEventIngressControllerText, "public void Attach()");
        AssertContains(mainViewModelRuntimeEventIngressControllerText, "_context.AttachFormatProbeCompleted(_context.OnDeviceFormatProbeCompleted);");
        AssertContains(mainViewModelRuntimeEventIngressControllerText, "_context.AttachCaptureStatusChanged(OnCaptureStatusChanged);");
        AssertContains(mainViewModelRuntimeEventIngressControllerText, "_context.AttachCaptureErrorOccurred(OnCaptureError);");
        AssertContains(mainViewModelRuntimeEventIngressControllerText, "_context.AttachCapturePreCleanupRequested(OnCapturePreCleanupRequested);");
        AssertContains(mainViewModelRuntimeEventIngressControllerText, "_context.AttachFrameCaptured(OnFrameCaptured);");
        AssertContains(mainViewModelRuntimeEventIngressControllerText, "_context.AttachAudioLevelUpdated(_context.OnAudioLevelUpdated);");
        AssertContains(mainViewModelRuntimeEventIngressControllerText, "_context.AttachMicrophoneAudioLevelUpdated(_context.OnMicrophoneAudioLevelUpdated);");
        AssertContains(mainViewModelRuntimeEventIngressControllerText, "_context.AttachSourceTelemetryUpdated(_context.OnSourceTelemetryUpdated);");
        AssertContains(mainViewModelRuntimeEventIngressControllerText, "SystemEvents.PowerModeChanged += OnSystemPowerModeChanged;");
        AssertContains(mainViewModelRuntimeEventIngressControllerText, "_context.AttachAudioDevicesChanged(_context.OnAudioDevicesChanged);");
        AssertContains(mainViewModelRuntimeEventIngressControllerText, "public void Detach()");
        AssertContains(mainViewModelRuntimeEventIngressControllerText, "_context.DetachFormatProbeCompleted(_context.OnDeviceFormatProbeCompleted);");
        AssertContains(mainViewModelRuntimeEventIngressControllerText, "_context.DetachCaptureStatusChanged(OnCaptureStatusChanged);");
        AssertContains(mainViewModelRuntimeEventIngressControllerText, "_context.DetachCaptureErrorOccurred(OnCaptureError);");
        AssertContains(mainViewModelRuntimeEventIngressControllerText, "_context.DetachCapturePreCleanupRequested(OnCapturePreCleanupRequested);");
        AssertContains(mainViewModelRuntimeEventIngressControllerText, "_context.DetachFrameCaptured(OnFrameCaptured);");
        AssertContains(mainViewModelRuntimeEventIngressControllerText, "_context.DetachAudioLevelUpdated(_context.OnAudioLevelUpdated);");
        AssertContains(mainViewModelRuntimeEventIngressControllerText, "_context.DetachMicrophoneAudioLevelUpdated(_context.OnMicrophoneAudioLevelUpdated);");
        AssertContains(mainViewModelRuntimeEventIngressControllerText, "_context.DetachSourceTelemetryUpdated(_context.OnSourceTelemetryUpdated);");
        AssertContains(mainViewModelRuntimeEventIngressControllerText, "SystemEvents.PowerModeChanged -= OnSystemPowerModeChanged;");
        AssertContains(mainViewModelRuntimeEventIngressControllerText, "_context.DetachAudioDevicesChanged(_context.OnAudioDevicesChanged);");
        AssertContains(mainViewModelRuntimeLifecycleControllerText, "var latestSourceTelemetry = _context.GetLatestSourceTelemetrySnapshot();");
        AssertContains(mainViewModelRuntimeLifecycleControllerText, "_context.SetLatestSourceTelemetrySnapshot(latestSourceTelemetry);");
        AssertContains(mainViewModelRuntimeLifecycleControllerText, "_context.ApplySourceTelemetrySnapshot(latestSourceTelemetry, false);");
        AssertContains(mainViewModelRuntimeLifecycleControllerText, "_context.UpdateHdrRuntimeStatusFromCapture();");
        AssertContains(mainViewModelRuntimeLifecycleControllerText, "_context.UpdateLiveCaptureInfo();");
        AssertContains(mainViewModelRuntimeLifecycleControllerText, "SetupTimer();");
        AssertContains(mainViewModelRuntimeLifecycleControllerText, "_context.UpdateDiskSpace();");
        AssertContains(mainViewModelRuntimeLifecycleControllerText, "_context.DisposeAudioDeviceWatcher();");
        AssertContains(mainViewModelDisposalText, "private void CancelActiveFlashbackExportForDispose()");
        AssertContains(mainViewModelDisposalText, "_disposalController.Dispose();");
        AssertContains(mainViewModelDisposalControllerText, "namespace Sussudio.Controllers;");
        AssertContains(mainViewModelDisposalControllerText, "internal sealed class MainViewModelDisposalController");
        AssertContains(mainViewModelDisposalControllerText, "internal sealed class MainViewModelDisposalControllerContext");
        AssertContains(mainViewModelDisposalControllerText, "private readonly MainViewModelDisposalControllerContext _context;");
        AssertContains(mainViewModelDisposalControllerText, "await _context.AwaitWithTimeoutAsync(");
        AssertContains(mainViewModelDisposalControllerText, "public required Func<Task, int, string, Task> AwaitWithTimeoutAsync { get; init; }");
        AssertDoesNotContain(mainViewModelDisposalControllerText, "private readonly MainViewModel _viewModel;");
        AssertDoesNotContain(mainViewModelDisposalControllerText, "_viewModel.");
        AssertContains(mainViewModelDisposalControllerText, "_context.CancelActiveFlashbackExport();");
        AssertContains(mainViewModelDisposalControllerText, "_context.StopRuntimeForDispose();");
        AssertContains(mainViewModelDisposalControllerText, "_context.DisposeCaptureService();");
        AssertDoesNotContain(mainViewModelDisposalText, "PowerModeChanged -=");
        AssertDoesNotContain(mainViewModelDisposalText, "AudioLevelUpdated -=");
        AssertDoesNotContain(mainViewModelRecordingRuntimeText, "OnSystemPowerModeChanged");
        AssertDoesNotContain(mainViewModelRecordingRuntimeText, "new DriveInfo(");
        AssertDoesNotContain(mainViewModelRecordingRuntimeText, "Path.GetPathRoot(");
        AssertDoesNotContain(mainViewModelRecordingRuntimeText, "Trace.TraceWarning(");
        AssertContains(mainViewModelRuntimeEventIngressControllerText, "private void OnCaptureStatusChanged(object? sender, string status)");
        AssertContains(mainViewModelRuntimeEventIngressControllerText, "private void OnCaptureError(object? sender, Exception ex)");
        AssertContains(mainViewModelRuntimeEventIngressControllerText, "private void OnCapturePreCleanupRequested()");
        AssertContains(mainViewModelRuntimeEventIngressControllerText, "CAPTURE_STATUS_UI_ENQUEUE_FAILED status='{status}'");
        AssertContains(mainViewModelRuntimeEventIngressControllerText, "CAPTURE_ERROR_UI_ENQUEUE_FAILED type={ex.GetType().Name} msg='{ex.Message}'");
        AssertDoesNotContain(mainViewModelText, "CAPTURE_STATUS_UI_ENQUEUE_FAILED status='{status}'");
    }

    private static void AssertServiceNamespaceMainViewModelDeviceAndCaptureSourceOwnership(string repoRoot)
    {
        var mainViewModelText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "ViewModels", "MainViewModel.cs"));
        var deviceAudioRequestControllerText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "Controllers", "ViewModel", "MainViewModelDeviceAudioRequestController.cs"));
        var mainViewModelDeviceFormatProbeControllerText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "Controllers", "ViewModel", "MainViewModelDeviceDiscoveryControllers.cs"));
        var mainViewModelDeviceFormatProbeRetargetApplierText = mainViewModelDeviceFormatProbeControllerText;
        var mainViewModelSourceTelemetryControllerText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "Controllers", "ViewModel", "MainViewModelCaptureReadinessControllers.cs"));
        var mainViewModelDisposalText = mainViewModelText;
        var mainViewModelDisposalControllerText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "Controllers", "ViewModel", "MainViewModelLifecycleController.cs"));
        var deviceRefreshControllerText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "Controllers", "ViewModel", "MainViewModelDeviceDiscoveryControllers.cs"));
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
        AssertEqual(
            false,
            File.Exists(Path.Combine(repoRoot, "Sussudio", "Controllers", "ViewModel", "MainViewModelDeviceFormatProbeRetargetApplier.cs")),
            "device format probe retarget applier lives with probe event owner");
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
        var recordingCapabilityControllerText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "Controllers", "ViewModel", "MainViewModelCaptureReadinessControllers.cs"));
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
            + "\n" + File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "Services", "Preview", "D3D11PreviewRenderer.Metrics.cs"))
            + "\n" + File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "Services", "Preview", "D3D11PreviewRenderer.Resources.cs"))
            + "\n" + File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "Services", "Preview", "D3D11PreviewRenderer.cs")),
            "D3D_FIRST_FRAME_UI_ENQUEUE_FAILED");
        AssertContains(
            File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "Services", "Preview", "D3D11PreviewRenderer.RenderPasses.cs"))
            + "\n" + File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "Services", "Preview", "D3D11PreviewRenderer.Resources.cs"))
            + "\n" + File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "Services", "Preview", "D3D11PreviewRenderer.cs")),
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
        var nativeXuDeviceSupportText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "Services", "Capture", "NativeXu", "KsExtensionUnitNative.cs"));
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

        var cudaInteropBridgeText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "Services", "Gpu", "CudaD3D11InteropBridge.cs"));
        AssertEqual(
            false,
            File.Exists(Path.Combine(repoRoot, "Sussudio", "Services", "Gpu", "CudaD3D11Interop.cs")),
            "CUDA/D3D11 interop state-only partial");
        AssertEqual(
            false,
            File.Exists(Path.Combine(repoRoot, "Sussudio", "Services", "Gpu", "CudaD3D11Interop.Initialization.cs")),
            "CUDA/D3D11 interop initialization folded into bridge owner");
        AssertEqual(
            false,
            File.Exists(Path.Combine(repoRoot, "Sussudio", "Services", "Gpu", "CudaD3D11Interop.Copy.cs")),
            "CUDA/D3D11 interop copy hot paths folded into bridge owner");
        AssertEqual(
            false,
            File.Exists(Path.Combine(repoRoot, "Sussudio", "Services", "Gpu", "CudaD3D11Interop.Lifetime.cs")),
            "CUDA/D3D11 interop lifetime is consolidated with bridge initialization");
        AssertContains(cudaInteropBridgeText, "internal sealed unsafe class CudaD3D11InteropBridge : IDisposable");
        AssertContains(cudaInteropBridgeText, "private static readonly object D3D11InteropLock");
        AssertContains(cudaInteropBridgeText, "public IntPtr TextureNativePointer");
        AssertContains(cudaInteropBridgeText, "public CudaD3D11InteropBridge(");
        AssertContains(cudaInteropBridgeText, "private bool TryInitializeZeroCopyResources");
        AssertContains(cudaInteropBridgeText, "CUDA_D3D11_INTEROP_CTX_INIT");
        AssertContains(cudaInteropBridgeText, "CUDA_D3D11_ZEROCOPY_REGISTER_OK");
        AssertContains(cudaInteropBridgeText, "public void CopyFrameToTexture");
        AssertContains(cudaInteropBridgeText, "private void CopyFrameZeroCopy");
        AssertContains(cudaInteropBridgeText, "private void CopyFrameStaging");
        AssertContains(cudaInteropBridgeText, "cuGraphicsMapResources");
        AssertContains(cudaInteropBridgeText, "MapMode.Write");
        AssertContains(cudaInteropBridgeText, "DllImport(\"nvcuda.dll\")");
        AssertContains(cudaInteropBridgeText, "private struct CUDA_MEMCPY2D");
        AssertContains(cudaInteropBridgeText, "public void Dispose()");
        AssertContains(cudaInteropBridgeText, "private void TryUnregisterResource");
        AssertContains(cudaInteropBridgeText, "cuDevicePrimaryCtxRelease");
        AssertContains(cudaInteropBridgeText, "private const uint CU_MEMORYTYPE_DEVICE");
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
        var captureServiceTelemetryText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "Services", "Capture", "CaptureService.Snapshots.cs"));
        AssertContains(captureServiceTelemetryText, "pollGeneration != Volatile.Read(ref _telemetryPollGeneration)");
        AssertContains(captureServiceText, "_telemetryPollSync");
        AssertContains(captureServiceTelemetryText, "lock (_telemetryPollSync)");
        AssertContains(captureServiceTelemetryText, "StartTelemetryPollCoreLocked");
        AssertContains(captureServiceTelemetryText, "StartTelemetryPollCore");
        AssertContains(captureServiceTelemetryText, "Telemetry poll start deferred until canceled poll exits");
        AssertContains(captureServiceTelemetryText, "private SourceSignalTelemetrySnapshot BuildFallbackTelemetry()");
        AssertContains(captureServiceTelemetryText, "private static SourceSignalTelemetrySnapshot MergeTelemetryWithFallback(");
        AssertEqual(
            false,
            File.Exists(Path.Combine(repoRoot, "Sussudio", "Services", "Capture", "CaptureService.Telemetry.cs")),
            "CaptureService telemetry polling folded into snapshot diagnostics owner");
    }

    internal static Task MfDeviceEnumerator_SourceOwnershipLivesInCohesiveEnumerator()
    {
        var rootText = ReadMfDeviceEnumeratorFile("MfDeviceEnumerator.cs");

        AssertContains(rootText, "internal static class MfDeviceEnumerator");
        AssertDoesNotContain(rootText, "partial class MfDeviceEnumerator");
        AssertContains(rootText, "private static extern int MFCreateAttributes(");
        AssertContains(rootText, "private static extern int MFCreateSourceReaderFromMediaSource(");
        AssertContains(rootText, "public static Task<List<MfVideoDeviceInfo>> EnumerateVideoDevicesAsync()");
        AssertContains(rootText, "MF video device enumeration failed");
        AssertContains(rootText, "MFEnumDeviceSources(attributes, out activateArray, out var activateCount)");
        AssertContains(rootText, "public static Task<List<AudioInputDevice>> EnumerateAudioCaptureEndpointsAsync()");
        AssertContains(rootText, "ReadAudioEndpointFriendlyName(endpoint, endpointId)");
        AssertContains(rootText, "private static string ReadAudioEndpointFriendlyName(");
        AssertContains(rootText, "public static Task<List<MediaFormat>> ProbeVideoFormatsAsync(string symbolicLink)");
        AssertContains(rootText, "private static string SubtypeGuidToName(Guid subtype)");
        AssertContains(rootText, "private static IMFMediaSource CreateMediaSource(string symbolicLink)");
        AssertContains(rootText, "private static IMFMediaSource CreateMediaSourceByEnumeration(");
        AssertContains(rootText, "MfInteropHelpers.MatchesSymbolicLink(targetSymbolicLink, candidateLink)");
        AssertContains(rootText, "MFCreateDeviceSource(attributes, out var mediaSource)");
        foreach (var removedFile in new[]
        {
            "MfDeviceEnumerator.VideoDevices.cs",
            "MfDeviceEnumerator.AudioEndpoints.cs",
            "MfDeviceEnumerator.FormatProbe.cs",
            "MfDeviceEnumerator.SourceOpening.cs"
        })
        {
            AssertEqual(
                false,
                File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "DeviceDiscovery", removedFile)),
                $"{removedFile} removed");
        }

        return Task.CompletedTask;
    }

    internal static Task CaptureDiscoverySourceOwnership_LivesInFocusedPartials()
    {
        var deviceRootText = ReadRepoFile("Sussudio/Services/Capture/DeviceService.cs").Replace("\r\n", "\n");
        var sourceReaderRootText = ReadRepoFile("Sussudio/Services/Capture/MfSourceReaderVideoCapture.cs").Replace("\r\n", "\n");
        var sourceReaderNegotiationText = ReadRepoFile("Sussudio/Services/Capture/MfSourceReaderVideoCapture.Negotiation.cs").Replace("\r\n", "\n");
        var sourceReaderDeviceEnumerationText = sourceReaderNegotiationText;
        var sourceReaderComContractsText = ReadRepoFile("Sussudio/Services/Capture/MfSourceReaderVideoCapture.ComContracts.cs").Replace("\r\n", "\n");
        var mfInteropHelpersText = ReadRepoFile("Sussudio/Services/Capture/MfInteropHelpers.cs").Replace("\r\n", "\n");

        AssertContains(deviceRootText, "var likelyByCapability = LooksLikeHighBandwidthCapture(captureDevice);");
        AssertContains(deviceRootText, "public async Task<DeviceDiscoveryResult> EnumerateCaptureDeviceDiscoveryAsync(");
        AssertContains(deviceRootText, "public async Task<ObservableCollection<CaptureDevice>> EnumerateVideoCaptureDevicesAsync(");
        AssertContains(deviceRootText, "return discovery.CaptureDevices;");
        AssertContains(deviceRootText, "var audioTask = MfDeviceEnumerator.EnumerateAudioCaptureEndpointsAsync();");
        AssertContains(deviceRootText, "return new DeviceDiscoveryResult(discovered, audioDevices);");
        AssertContains(deviceRootText, "foreach (var candidate in selected.OrderByDescending(GetDevicePriority))");
        AssertContains(deviceRootText, "private static int GetDevicePriority(DeviceCandidate candidate)");
        AssertContains(deviceRootText, "if (candidate.PreferredByName) priority += 400;");
        AssertContains(deviceRootText, "if (candidate.LikelyByCapability) priority += 200;");
        AssertContains(deviceRootText, "if (candidate.HasEnumeratedFormats) priority += 50;");
        AssertContains(deviceRootText, "private static bool LooksLikeHighBandwidthCapture(CaptureDevice device)");

        AssertContains(sourceReaderNegotiationText, "private bool TrySetSourceReaderD3DManager(");
        AssertContains(sourceReaderNegotiationText, "private IMFMediaSource CreateMediaSource(");
        AssertContains(sourceReaderDeviceEnumerationText, "private IMFMediaSource CreateMediaSourceByEnumeration(");
        AssertContains(sourceReaderDeviceEnumerationText, "MfInterop.MFEnumDeviceSources(attrs, out activateArrayPtr, out var activateCount)");
        AssertContains(sourceReaderDeviceEnumerationText, "MfInteropHelpers.MatchesSymbolicLink(targetSymbolicLink, link)");
        AssertContains(mfInteropHelpersText, "public static bool MatchesSymbolicLink(string? target, string? candidate)");
        AssertContains(sourceReaderDeviceEnumerationText, "ReleaseRemainingActivateObjects(activateArrayPtr, activateCount, i + 1);");
        AssertContains(sourceReaderDeviceEnumerationText, "Marshal.ReleaseComObject(activated)");
        AssertContains(sourceReaderDeviceEnumerationText, "Marshal.FreeCoTaskMem(activateArrayPtr);");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "MfSourceReaderVideoCapture.DeviceEnumeration.cs")),
            "source-reader device enumeration fallback folded into negotiation/source-open owner");
        AssertContains(sourceReaderNegotiationText, "private IMFMediaType SelectMediaType(");
        AssertContains(sourceReaderNegotiationText, "private IMFMediaType SelectConvertedMediaType(");
        AssertContains(sourceReaderNegotiationText, "SelectMediaType(");
        AssertContains(sourceReaderNegotiationText, "IMFMediaType.SetGUID(MF_MT_SUBTYPE");
        AssertContains(sourceReaderNegotiationText, "private static void CopyOptionalUInt64(");
        AssertContains(sourceReaderNegotiationText, "private static void CopyOptionalUInt32(");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "MfSourceReaderVideoCapture.ConvertedMediaType.cs")),
            "converted source-reader media type construction folded into negotiation owner");
        AssertContains(sourceReaderNegotiationText, "private static bool TryGetFrameSize(");
        AssertContains(sourceReaderNegotiationText, "private static bool TryGetFrameRate(");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "MfSourceReaderVideoCapture.Interop.cs")),
            "source-reader MF P/Invokes and constants folded into COM contract owner");
        AssertContains(sourceReaderComContractsText, "private static class MfInterop");
        AssertContains(sourceReaderComContractsText, "DllImport(\"mfplat.dll\", ExactSpelling = true)");
        AssertContains(sourceReaderComContractsText, "private static class MfConstants");
        AssertContains(sourceReaderComContractsText, "private static class MfHResults");
        AssertContains(sourceReaderComContractsText, "private static class MfGuids");
        AssertContains(sourceReaderComContractsText, "internal interface IMFSourceReader");
        AssertContains(sourceReaderComContractsText, "internal interface IMFMediaBuffer");
        AssertContains(sourceReaderComContractsText, "internal interface IMFDXGIBuffer");
        AssertContains(sourceReaderComContractsText, "internal interface IMFSample");
        AssertContains(sourceReaderComContractsText, "Flattened IMFSample COM interface");
        AssertContains(sourceReaderComContractsText, "does NOT use C# interface inheritance");
        AssertContains(sourceReaderComContractsText, "[PreserveSig] int _Attr_GetItem(ref Guid guidKey, IntPtr pValue);");
        AssertContains(sourceReaderComContractsText, "int GetSampleTime(out long phnsSampleTime);");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "MfSourceReaderVideoCapture.SampleBufferContracts.cs")),
            "source-reader sample/buffer COM declarations folded into COM contract owner");
        AssertDoesNotContain(sourceReaderRootText, "private IMFMediaSource CreateMediaSource(");
        AssertDoesNotContain(sourceReaderRootText, "private IMFMediaType SelectMediaType(");
        AssertDoesNotContain(sourceReaderRootText, "private static class MfInterop");
        AssertDoesNotContain(sourceReaderRootText, "DllImport(\"mfplat.dll\", ExactSpelling = true)");

        var matches = RequireType("Sussudio.Services.Capture.MfInteropHelpers")
            .GetMethod("MatchesSymbolicLink", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public)
            ?? throw new InvalidOperationException("MfInteropHelpers.MatchesSymbolicLink was not found.");
        AssertEqual(true, (bool)matches.Invoke(null, new object?[] { "DEVICE_A", "device_a" })!, "symbolic-link exact case-insensitive match");
        AssertEqual(true, (bool)matches.Invoke(null, new object?[] { "core", "PREFIX-core-SUFFIX" })!, "symbolic-link candidate contains target");
        AssertEqual(true, (bool)matches.Invoke(null, new object?[] { "PREFIX-core-SUFFIX", "core" })!, "symbolic-link target contains candidate");
        AssertEqual(false, (bool)matches.Invoke(null, new object?[] { "abc", "xyz" })!, "symbolic-link mismatch");
        AssertEqual(false, (bool)matches.Invoke(null, new object?[] { "", "anything" })!, "symbolic-link empty target");
        AssertEqual(false, (bool)matches.Invoke(null, new object?[] { "anything", null })!, "symbolic-link null candidate");

        return Task.CompletedTask;
    }

    private static string ReadMfDeviceEnumeratorFile(string fileName) =>
        ReadRepoFile($"Sussudio/Services/Capture/DeviceDiscovery/{fileName}");
}

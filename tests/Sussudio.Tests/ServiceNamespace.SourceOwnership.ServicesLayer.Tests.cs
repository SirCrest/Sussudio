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
        var mainViewModelCaptureModePropertyChangesText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "ViewModels", "MainViewModel.CaptureModeTransactions.cs"));
        var mainViewModelCompositionText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "ViewModels", "MainViewModel.Composition.cs"));
        var mainViewModelUiDispatchControllerText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "Controllers", "ViewModel", "MainViewModelUiDispatchController.cs"));
        var mainViewModelRuntimeLifecycleControllerText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "Controllers", "ViewModel", "MainViewModelLifecycleController.cs"));
        var mainViewModelRuntimeEventIngressControllerText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "Controllers", "ViewModel", "MainViewModelRuntimeEventIngressController.cs"));
        var mainViewModelDisposalControllerText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "Controllers", "ViewModel", "MainViewModelLifecycleController.cs"));
        var mainViewModelRecordingStateText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "ViewModels", "MainViewModel.RecordingState.cs"));
        var mainViewModelRecordingRuntimeText = mainViewModelRecordingStateText;
        var outputDriveSpacePresentationBuilderText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "ViewModels", "ViewModelBuilders.cs"));
        var mainViewModelCapturePresentationText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "ViewModels", "MainViewModel.CaptureState.cs"));
        var mainViewModelDisposalText = mainViewModelText;
        AssertEqual(
            false,
            File.Exists(Path.Combine(repoRoot, "Sussudio", "ViewModels", "MainViewModel.Dispatching.cs")),
            "MainViewModel dispatch adapter partial folded into composition");
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
        AssertDoesNotContain(mainViewModelText, "private bool EnqueueUiOperation");
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
            "MainViewModel.CaptureModePropertyChanges.cs folded into MainViewModel.CaptureModeTransactions.cs");
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
        AssertDoesNotContain(mainViewModelText, "public Task ToggleRecordingAsync()");
        AssertDoesNotContain(mainViewModelText, "internal Task SetRecordingDesiredStateAsync");
        AssertContains(mainViewModelRecordingRuntimeText, "private void UpdateDiskSpace()");
        AssertContains(mainViewModelRecordingRuntimeText, "DiskSpaceInfo = OutputDriveSpacePresentationBuilder.Build(OutputPath);");
        AssertContains(mainViewModelRecordingRuntimeText, "_recordingBitrateSamples.AddSampleAndCompute(now, totalBytes);");
        AssertEqual(
            false,
            File.Exists(Path.Combine(repoRoot, "Sussudio", "ViewModels", "MainViewModel.RecordingRuntime.cs")),
            "MainViewModel.RecordingRuntime.cs folded into MainViewModel.RecordingState.cs");
        AssertContains(mainViewModelRecordingStateText, "internal sealed class BitrateSampleWindow");
        AssertContains(mainViewModelRecordingStateText, "private readonly Queue<(long Tick, long Bytes)> _samples = new();");
        AssertEqual(
            false,
            File.Exists(Path.Combine(repoRoot, "Sussudio", "ViewModels", "BitrateSampleWindow.cs")),
            "BitrateSampleWindow folded into MainViewModel.RecordingState.cs");
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
        var mainViewModelDeviceFormatProbeControllerText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "Controllers", "ViewModel", "MainViewModelDeviceFormatProbeController.cs"));
        var mainViewModelDeviceFormatProbeRetargetApplierText = mainViewModelDeviceFormatProbeControllerText;
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

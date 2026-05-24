// Tests that keep MainViewModel runtime source ownership from drifting back into catch-all partials.
static partial class Program
{
    private static void AssertServiceNamespaceMainViewModelRuntimeSourceOwnership(string repoRoot)
    {
        var mainViewModelText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "ViewModels", "MainViewModel.cs"));
        var mainViewModelAudioCapturePropertyChangesText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "ViewModels", "MainViewModel.AudioState.cs"));
        var mainViewModelAudioStateText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "ViewModels", "MainViewModel.AudioState.cs"));
        var mainViewModelAudioInputSelectionText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "ViewModels", "MainViewModel.AudioInputSelection.cs"));
        var mainViewModelDeviceAudioRequestControllerText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "Controllers", "ViewModel", "MainViewModelDeviceAudioRequestController.cs"));
        var mainViewModelCaptureModePropertyChangesText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "ViewModels", "MainViewModel.CaptureModeTransactions.cs"));
        var mainViewModelCompositionText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "ViewModels", "MainViewModel.Composition.cs"));
        var mainViewModelUiDispatchControllerText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "Controllers", "ViewModel", "MainViewModelUiDispatchController.cs"));
        var mainViewModelRuntimeLifecycleControllerText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "Controllers", "ViewModel", "MainViewModelRuntimeLifecycleController.cs"));
        var mainViewModelRuntimeEventIngressControllerText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "Controllers", "ViewModel", "MainViewModelRuntimeEventIngressController.cs"));
        var mainViewModelDisposalControllerText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "Controllers", "ViewModel", "MainViewModelDisposalController.cs"));
        var mainViewModelRecordingStateText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "ViewModels", "MainViewModel.RecordingState.cs"));
        var mainViewModelRecordingRuntimeText = mainViewModelRecordingStateText;
        var outputDriveSpacePresentationBuilderText = File.ReadAllText(Path.Combine(repoRoot, "Sussudio", "ViewModels", "ViewModelPresentationBuilders.cs"));
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
        AssertContains(mainViewModelAudioInputSelectionText, "OnIsCustomAudioInputEnabledChanged");
        AssertContains(mainViewModelAudioInputSelectionText, "OnSelectedAudioInputDeviceChanged");
        AssertContains(mainViewModelAudioInputSelectionText, "private async Task ApplyAudioInputSelectionAsync");
        AssertEqual(
            false,
            File.Exists(Path.Combine(repoRoot, "Sussudio", "ViewModels", "MainViewModel.AudioInputPropertyChanges.cs")),
            "MainViewModel audio-input property-change partial");
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
        AssertDoesNotContain(mainViewModelAudioCapturePropertyChangesText, "OnSelectedAudioInputDeviceChanged");
        AssertDoesNotContain(mainViewModelAudioStateText, "OnSelectedDeviceAudioModeChanged");
        AssertDoesNotContain(mainViewModelAudioStateText, "OnSelectedAudioInputDeviceChanged");
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
}

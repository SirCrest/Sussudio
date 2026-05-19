using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Controllers;
using Sussudio.ViewModels;

namespace Sussudio.ViewModels;

public partial class MainViewModel
{
    private sealed class MainViewModelControllerGraph
    {
        private MainViewModelControllerGraph(
            MainViewModelUiDispatchController uiDispatchController,
            MainViewModelRecordingTransitionController recordingTransitionController,
            MainViewModelPreviewLifecycleController previewLifecycleController,
            MainViewModelDeviceAudioRequestController deviceAudioRequestController,
            MainViewModelRecordingCapabilityController recordingCapabilityController,
            MainViewModelCaptureSettingsAutomationController captureSettingsAutomationController,
            MainViewModelRecordingSettingsAutomationController recordingSettingsAutomationController,
            MainViewModelCaptureModeOptionRebuildController captureModeOptionRebuildController,
            MainViewModelDeviceFormatProbeController deviceFormatProbeController,
            MainViewModelSourceTelemetryController sourceTelemetryController,
            MainViewModelDeviceRefreshController deviceRefreshController,
            MainViewModelRuntimeLifecycleController runtimeLifecycleController,
            MainViewModelDisposalController disposalController)
        {
            UiDispatchController = uiDispatchController;
            RecordingTransitionController = recordingTransitionController;
            PreviewLifecycleController = previewLifecycleController;
            DeviceAudioRequestController = deviceAudioRequestController;
            RecordingCapabilityController = recordingCapabilityController;
            CaptureSettingsAutomationController = captureSettingsAutomationController;
            RecordingSettingsAutomationController = recordingSettingsAutomationController;
            CaptureModeOptionRebuildController = captureModeOptionRebuildController;
            DeviceFormatProbeController = deviceFormatProbeController;
            SourceTelemetryController = sourceTelemetryController;
            DeviceRefreshController = deviceRefreshController;
            RuntimeLifecycleController = runtimeLifecycleController;
            DisposalController = disposalController;
        }

        public MainViewModelUiDispatchController UiDispatchController { get; }
        public MainViewModelRecordingTransitionController RecordingTransitionController { get; }
        public MainViewModelPreviewLifecycleController PreviewLifecycleController { get; }
        public MainViewModelDeviceAudioRequestController DeviceAudioRequestController { get; }
        public MainViewModelRecordingCapabilityController RecordingCapabilityController { get; }
        public MainViewModelCaptureSettingsAutomationController CaptureSettingsAutomationController { get; }
        public MainViewModelRecordingSettingsAutomationController RecordingSettingsAutomationController { get; }
        public MainViewModelCaptureModeOptionRebuildController CaptureModeOptionRebuildController { get; }
        public MainViewModelDeviceFormatProbeController DeviceFormatProbeController { get; }
        public MainViewModelSourceTelemetryController SourceTelemetryController { get; }
        public MainViewModelDeviceRefreshController DeviceRefreshController { get; }
        public MainViewModelRuntimeLifecycleController RuntimeLifecycleController { get; }
        public MainViewModelDisposalController DisposalController { get; }

        public static MainViewModelControllerGraph Create(MainViewModel viewModel)
        {
            ArgumentNullException.ThrowIfNull(viewModel);

            var uiDispatchController = CreateUiDispatchController(viewModel);
            var previewLifecycleController = CreatePreviewLifecycleController(viewModel);
            var recordingTransitionController = new MainViewModelRecordingTransitionController(viewModel, previewLifecycleController);
            var deviceAudioRequestController = new MainViewModelDeviceAudioRequestController(viewModel);
            var recordingCapabilityController = CreateRecordingCapabilityController(viewModel);
            var captureSettingsAutomationController = CreateCaptureSettingsAutomationController(viewModel);
            var recordingSettingsAutomationController = CreateRecordingSettingsAutomationController(viewModel);
            var captureModeOptionRebuildController = new MainViewModelCaptureModeOptionRebuildController(viewModel);
            var deviceFormatProbeController = CreateDeviceFormatProbeController(viewModel);
            var sourceTelemetryController = CreateSourceTelemetryController(viewModel);
            var deviceRefreshController = new MainViewModelDeviceRefreshController(viewModel, previewLifecycleController);
            var runtimeLifecycleController = CreateRuntimeLifecycleController(viewModel, previewLifecycleController);
            var disposalController = CreateDisposalController(viewModel);

            return new MainViewModelControllerGraph(
                uiDispatchController,
                recordingTransitionController,
                previewLifecycleController,
                deviceAudioRequestController,
                recordingCapabilityController,
                captureSettingsAutomationController,
                recordingSettingsAutomationController,
                captureModeOptionRebuildController,
                deviceFormatProbeController,
                sourceTelemetryController,
                deviceRefreshController,
                runtimeLifecycleController,
                disposalController);
        }

        private static MainViewModelUiDispatchController CreateUiDispatchController(MainViewModel viewModel)
        {
            return new MainViewModelUiDispatchController(
                new MainViewModelUiDispatchControllerContext
                {
                    DispatcherQueue = viewModel._dispatcherQueue,
                    IsDisposing = () => Volatile.Read(ref viewModel._disposeState) != 0,
                    Log = message => Logger.Log(message),
                    LogException = exception => Logger.LogException(exception),
                    SetStatusText = value => viewModel.StatusText = value,
                });
        }

        private static MainViewModelPreviewLifecycleController CreatePreviewLifecycleController(MainViewModel viewModel)
        {
            return new MainViewModelPreviewLifecycleController(
                new MainViewModelPreviewLifecycleControllerContext
                {
                    SessionCoordinator = viewModel._sessionCoordinator,
                    BuildCaptureSettings = viewModel.BuildCaptureSettings,
                    InvokeOnUiThreadAsync = (operation, cancellationToken) => viewModel.InvokeOnUiThreadAsync(operation, cancellationToken),
                    RampPreviewVolumeDownForStopAsync = viewModel.RampPreviewVolumeDownForStopAsync,
                    CreateReinitializeController = controller => new MainViewModelPreviewReinitializeController(
                        new MainViewModelPreviewReinitializeControllerContext
                        {
                            SelectedDevice = () => viewModel.SelectedDevice,
                            SelectedFormat = () => viewModel.SelectedFormat,
                            IsRecording = () => viewModel.IsRecording,
                            IsInitialized = () => viewModel.IsInitialized,
                            SetIsInitialized = value => viewModel.IsInitialized = value,
                            IsPreviewing = () => viewModel.IsPreviewing,
                            IsPreviewReinitializing = () => viewModel.IsPreviewReinitializing,
                            SetIsPreviewReinitializing = value => viewModel.IsPreviewReinitializing = value,
                            SetStatusText = value => viewModel.StatusText = value,
                            CancelPreviewRestartAfterReinitialize = () => viewModel._cancelPreviewRestartAfterReinitialize,
                            SetCancelPreviewRestartAfterReinitialize = value => viewModel._cancelPreviewRestartAfterReinitialize = value,
                            IncrementReinitializeGeneration = () => Interlocked.Increment(ref viewModel._previewReinitializeGeneration),
                            ReadReinitializeGeneration = () => Volatile.Read(ref viewModel._previewReinitializeGeneration),
                            PendingFlashbackCycleTask = () => viewModel._pendingFlashbackCycleTask,
                            ClearPendingFlashbackCycleIfSameAndCompleted = task =>
                            {
                                if (ReferenceEquals(viewModel._pendingFlashbackCycleTask, task) && task.IsCompleted)
                                {
                                    viewModel._pendingFlashbackCycleTask = null;
                                }
                            },
                            WaitReinitializeGateAsync = viewModel._previewReinitializeGate.WaitAsync,
                            ReleaseReinitializeGate = () => viewModel._previewReinitializeGate.Release(),
                            NotifyPreviewReinitRequestedAsync = viewModel.NotifyPreviewReinitRequestedAsync,
                            NotifyRendererStopAsync = viewModel.NotifyRendererStopAsync,
                        },
                        controller),
                    SelectedDevice = () => viewModel.SelectedDevice,
                    SetSelectedDevice = device => viewModel.SelectedDevice = device,
                    IsInitialized = () => viewModel.IsInitialized,
                    SetIsInitialized = value => viewModel.IsInitialized = value,
                    IsPreviewing = () => viewModel.IsPreviewing,
                    SetIsPreviewing = value => viewModel.IsPreviewing = value,
                    IsPreviewReinitializing = () => viewModel.IsPreviewReinitializing,
                    IsRecording = () => viewModel.IsRecording,
                    ShouldStartAudioPreview = () => viewModel.IsAudioPreviewEnabled && viewModel.IsAudioEnabled,
                    IsAudioPreviewActive = () => viewModel._captureService.IsAudioPreviewActive,
                    SetStatusText = value => viewModel.StatusText = value,
                    RaisePreviewStartRequested = () => viewModel.PreviewStartRequested?.Invoke(viewModel, EventArgs.Empty),
                    RaisePreviewStopRequested = () => viewModel.PreviewStopRequested?.Invoke(viewModel, EventArgs.Empty),
                    ApplyLatestSourceTelemetryForPreviewStart = () =>
                        viewModel._sourceTelemetryController.ApplySourceTelemetrySnapshot(
                            viewModel._captureService.GetLatestSourceTelemetrySnapshot(),
                            allowAutoRetarget: true),
                });
        }

        private static MainViewModelRecordingCapabilityController CreateRecordingCapabilityController(MainViewModel viewModel)
        {
            return new MainViewModelRecordingCapabilityController(
                new MainViewModelRecordingCapabilityControllerContext
                {
                    DefaultRecordingFormat = DefaultRecordingFormat,
                    HevcRecordingFormat = HevcRecordingFormat,
                    Av1RecordingFormat = Av1RecordingFormat,
                    GetAvailableRecordingFormats = () => viewModel.AvailableRecordingFormats,
                    ReplaceAvailableRecordingFormats = formats =>
                    {
                        viewModel.AvailableRecordingFormats.Clear();
                        foreach (var format in formats)
                        {
                            viewModel.AvailableRecordingFormats.Add(format);
                        }
                    },
                    GetSelectedRecordingFormat = () => viewModel.SelectedRecordingFormat,
                    SetSelectedRecordingFormat = value => viewModel.SelectedRecordingFormat = value,
                    NotifySelectedRecordingFormatChanged = () => viewModel.OnPropertyChanged(nameof(SelectedRecordingFormat)),
                    IsHdrEnabled = () => viewModel.IsHdrEnabled,
                    SetStatusText = value => viewModel.StatusText = value,
                    IsFfmpegMissing = () => viewModel.IsFfmpegMissing,
                    SetIsFfmpegMissing = value => viewModel.IsFfmpegMissing = value,
                    HasUiThreadAccess = () => viewModel._dispatcherQueue.HasThreadAccess,
                    TryEnqueueOnUiThread = operation => viewModel._dispatcherQueue.TryEnqueue(() => operation()),
                    GetAvailableSplitEncodeModes = () => viewModel.AvailableSplitEncodeModes,
                    ReplaceAvailableSplitEncodeModes = modes =>
                    {
                        viewModel.AvailableSplitEncodeModes.Clear();
                        foreach (var mode in modes)
                        {
                            viewModel.AvailableSplitEncodeModes.Add(mode);
                        }
                    },
                    GetSelectedSplitEncodeMode = () => viewModel.SelectedSplitEncodeMode,
                    SetSelectedSplitEncodeMode = value => viewModel.SelectedSplitEncodeMode = value,
                    AvailableSplitEncodeModesContains = value => viewModel.AvailableSplitEncodeModes.Contains(value),
                });
        }

        private static MainViewModelCaptureSettingsAutomationController CreateCaptureSettingsAutomationController(MainViewModel viewModel)
        {
            return new MainViewModelCaptureSettingsAutomationController(
                new MainViewModelCaptureSettingsAutomationControllerContext
                {
                    InvokeBooleanOnUiThreadAsync = (operation, cancellationToken) =>
                        viewModel.InvokeOnUiThreadAsync(operation, cancellationToken),
                    InvokeOnUiThreadAsync = (operation, cancellationToken) =>
                        viewModel.InvokeOnUiThreadAsync(operation, cancellationToken),
                    GetAvailableResolutions = () => viewModel.AvailableResolutions,
                    GetAvailableFrameRates = () => viewModel.AvailableFrameRates,
                    GetAvailableVideoFormats = () => viewModel.AvailableVideoFormats,
                    GetSelectedResolution = () => viewModel.SelectedResolution,
                    SetSelectedResolution = value => viewModel.SelectedResolution = value,
                    SetSelectedFrameRate = value => viewModel.SelectedFrameRate = value,
                    SetSelectedVideoFormat = value => viewModel.SelectedVideoFormat = value,
                    SetMjpegDecoderCount = value => viewModel.MjpegDecoderCount = value,
                    SelectAutoFrameRate = viewModel.SelectAutoFrameRate,
                    IsPreviewing = () => viewModel.IsPreviewing,
                    IsInitialized = () => viewModel.IsInitialized,
                    GetSelectedDevice = () => viewModel.SelectedDevice,
                    GetSelectedFormat = () => viewModel.SelectedFormat,
                    SetSuppressFormatChangeReinitialize = value => viewModel._suppressFormatChangeReinitialize = value,
                    ReinitializeDeviceAsync = viewModel.ReinitializeDeviceAsync,
                });
        }

        private static MainViewModelRecordingSettingsAutomationController CreateRecordingSettingsAutomationController(MainViewModel viewModel)
        {
            return new MainViewModelRecordingSettingsAutomationController(
                new MainViewModelRecordingSettingsAutomationControllerContext
                {
                    InvokeRecordingFormatOnUiThreadAsync = (operation, cancellationToken) =>
                        viewModel.InvokeOnUiThreadAsync(operation, cancellationToken),
                    InvokeEncoderSettingsOnUiThreadAsync = (operation, cancellationToken) =>
                        viewModel.InvokeOnUiThreadAsync(operation, cancellationToken),
                    InvokeOnUiThreadAsync = (operation, cancellationToken) =>
                        viewModel.InvokeOnUiThreadAsync(operation, cancellationToken),
                    GetAvailableRecordingFormats = () => viewModel.AvailableRecordingFormats,
                    GetAvailableQualities = () => viewModel.AvailableQualities,
                    GetAvailableSplitEncodeModes = () => viewModel.AvailableSplitEncodeModes,
                    GetAvailablePresets = () => viewModel.AvailablePresets,
                    IsHdrEnabled = () => viewModel.IsHdrEnabled,
                    SetSuppressFlashbackFormatCycle = value => viewModel._suppressFlashbackFormatCycle = value,
                    SetSuppressFlashbackEncoderSettingsCycle = value => viewModel._suppressFlashbackEncoderSettingsCycle = value,
                    SetSelectedRecordingFormat = value => viewModel.SelectedRecordingFormat = value,
                    GetSelectedQuality = () => viewModel.SelectedQuality,
                    SetSelectedQuality = value => viewModel.SelectedQuality = value,
                    GetSelectedSplitEncodeMode = () => viewModel.SelectedSplitEncodeMode,
                    SetSelectedSplitEncodeMode = value => viewModel.SelectedSplitEncodeMode = value,
                    GetSelectedPreset = () => viewModel.SelectedPreset,
                    SetSelectedPreset = value => viewModel.SelectedPreset = value,
                    GetCustomBitrateMbps = () => viewModel.CustomBitrateMbps,
                    SetCustomBitrateMbps = value => viewModel.CustomBitrateMbps = value,
                    SetOutputPath = value => viewModel.OutputPath = value,
                    UpdateRecordingFormatAsync = (format, cancellationToken) =>
                        viewModel._sessionCoordinator.UpdateRecordingFormatAsync(format, cancellationToken),
                    CycleFlashbackEncoderSettingsAsync = (quality, customBitrateMbps, nvencPreset, splitEncodeMode, cancellationToken) =>
                        viewModel._sessionCoordinator.CycleFlashbackEncoderSettingsAsync(
                            quality,
                            customBitrateMbps,
                            nvencPreset,
                            splitEncodeMode,
                            cancellationToken),
                });
        }

        private static MainViewModelDeviceFormatProbeController CreateDeviceFormatProbeController(MainViewModel viewModel)
        {
            return new MainViewModelDeviceFormatProbeController(
                new MainViewModelDeviceFormatProbeControllerContext
                {
                    TryEnqueueOnUiThread = operation => viewModel._dispatcherQueue.TryEnqueue(() => operation()),
                    ReadDeviceScanGeneration = () => Interlocked.Read(ref viewModel._deviceScanGeneration),
                    FindDeviceById = deviceId => viewModel.Devices.FirstOrDefault(
                        device => string.Equals(device.Id, deviceId, StringComparison.OrdinalIgnoreCase)),
                    SetPendingSdrAutoSelectionForDeviceChange = value => viewModel._pendingSdrAutoSelectionForDeviceChange = value,
                    SetPendingSdrAutoFriendlyFrameRateBucket = value => viewModel._pendingSdrAutoFriendlyFrameRateBucket = value,
                    GetSelectedDevice = () => viewModel.SelectedDevice,
                    IsPreviewing = () => viewModel.IsPreviewing,
                    IsInitialized = () => viewModel.IsInitialized,
                    IsRecording = () => viewModel.IsRecording,
                    GetSelectedResolution = () => viewModel.SelectedResolution,
                    GetSelectedFrameRate = () => viewModel.SelectedFrameRate,
                    GetSelectedFormat = () => viewModel.SelectedFormat,
                    SetSuppressFormatChangeReinitialize = value => viewModel._suppressFormatChangeReinitialize = value,
                    RebuildSelectedDeviceCapabilities = (device, resetTelemetryState) =>
                        viewModel.RebuildSelectedDeviceCapabilities(device, resetTelemetryState),
                    CreateRetargetApplier = () => new MainViewModelDeviceFormatProbeRetargetApplier(
                        new MainViewModelDeviceFormatProbeRetargetApplierContext
                        {
                            IsHdrEnabled = () => viewModel.IsHdrEnabled,
                            GetSelectedResolution = () => viewModel.SelectedResolution,
                            SetSelectedResolution = value => viewModel.SelectedResolution = value,
                            GetSelectedFrameRate = () => viewModel.SelectedFrameRate,
                            SetSelectedFrameRate = value => viewModel.SelectedFrameRate = value,
                            GetSelectedFormat = () => viewModel.SelectedFormat,
                            AvailableResolutionsContains = value => viewModel.AvailableResolutions.Any(
                                option => string.Equals(option.Value, value, StringComparison.OrdinalIgnoreCase)),
                            SetIsRebuildingModeOptions = value => viewModel._isRebuildingModeOptions = value,
                            SetIsApplyingAutomaticResolutionSelection = value => viewModel._isApplyingAutomaticResolutionSelection = value,
                            SetSuppressFormatChangeReinitialize = value => viewModel._suppressFormatChangeReinitialize = value,
                            RebuildFrameRateOptions = viewModel.RebuildFrameRateOptions,
                            ReinitializeDeviceAsync = viewModel.ReinitializeDeviceAsync,
                            EnqueueUiOperation = (operation, operationName) => viewModel.EnqueueUiOperation(operation, operationName),
                            GetCaptureRuntimeSnapshot = viewModel.GetCaptureRuntimeSnapshot,
                            UpdateSelectedFormat = viewModel.UpdateSelectedFormat,
                            UpdateTargetSummary = viewModel.UpdateTargetSummary,
                        }),
                });
        }

        private static MainViewModelRuntimeLifecycleController CreateRuntimeLifecycleController(
            MainViewModel viewModel,
            MainViewModelPreviewLifecycleController previewLifecycleController)
        {
            return new MainViewModelRuntimeLifecycleController(
                new MainViewModelRuntimeLifecycleControllerContext
                {
                    CreateEventIngressController = () => CreateRuntimeEventIngressController(viewModel, previewLifecycleController),
                    CreateTimer = viewModel._dispatcherQueue.CreateTimer,
                    GetRuntimeSnapshot = viewModel._captureService.GetRuntimeSnapshot,
                    GetLatestSourceTelemetrySnapshot = viewModel._captureService.GetLatestSourceTelemetrySnapshot,
                    SetLatestSourceTelemetrySnapshot = snapshot => viewModel._latestSourceTelemetry = snapshot,
                    ApplySourceTelemetrySnapshot = viewModel._sourceTelemetryController.ApplySourceTelemetrySnapshot,
                    UpdateHdrRuntimeStatusFromCaptureWithoutSnapshot = () => viewModel.UpdateHdrRuntimeStatusFromCapture(),
                    UpdateHdrRuntimeStatusFromCaptureWithSnapshot = snapshot => viewModel.UpdateHdrRuntimeStatusFromCapture(snapshot),
                    UpdateLiveCaptureInfoWithoutSnapshot = () => viewModel.UpdateLiveCaptureInfo(),
                    UpdateLiveCaptureInfoWithSnapshot = snapshot => viewModel.UpdateLiveCaptureInfo(snapshot),
                    ResetLiveCaptureInfo = viewModel.ResetLiveCaptureInfo,
                    UpdateDiskSpace = viewModel.UpdateDiskSpace,
                    RefreshSourceTelemetrySummaryAge = viewModel._sourceTelemetryController.RefreshSourceTelemetrySummaryAge,
                    IsRecording = () => viewModel.IsRecording,
                    IsPreviewing = () => viewModel.IsPreviewing,
                    IsFlashbackActive = () => viewModel._captureService.IsFlashbackActive,
                    GetRecordingElapsed = () => viewModel._recordingStopwatch.Elapsed,
                    SetRecordingTime = value => viewModel.RecordingTime = value,
                    UpdateRecordingStats = viewModel.UpdateRecordingStats,
                    UpdateFlashbackBitrate = viewModel.UpdateFlashbackBitrate,
                    DisposeAudioDeviceWatcher = viewModel._audioDeviceWatcher.Dispose,
                });
        }

        private static MainViewModelDisposalController CreateDisposalController(MainViewModel viewModel)
        {
            return new MainViewModelDisposalController(
                new MainViewModelDisposalControllerContext
                {
                    TryBeginDispose = () => Interlocked.Exchange(ref viewModel._disposeState, 1) == 0,
                    CancelActiveFlashbackExport = viewModel.CancelActiveFlashbackExportForDispose,
                    CancelPendingAudioControlWork = viewModel._deviceAudioRequestController.CancelPendingAudioControlWork,
                    StopRuntimeForDispose = viewModel._runtimeLifecycleController.StopForDispose,
                    CleanupSessionCoordinatorAsync = () => viewModel._sessionCoordinator.CleanupAsync(),
                    DisposeSessionCoordinatorAsync = () => viewModel._sessionCoordinator.DisposeAsync().AsTask(),
                    DisposeCaptureServiceAsync = () => viewModel._captureService.DisposeAsync().AsTask(),
                    DisposeCaptureService = viewModel._captureService.Dispose,
                });
        }

        private static MainViewModelSourceTelemetryController CreateSourceTelemetryController(MainViewModel viewModel)
        {
            return new MainViewModelSourceTelemetryController(
                new MainViewModelSourceTelemetryControllerContext
                {
                    TryEnqueueOnUiThread = operation => viewModel._dispatcherQueue.TryEnqueue(() => operation()),
                    GetLatestSourceTelemetry = () => viewModel._latestSourceTelemetry,
                    SetLatestSourceTelemetry = snapshot => viewModel._latestSourceTelemetry = snapshot,
                    SetSourceWidth = value => viewModel.SourceWidth = value,
                    SetSourceHeight = value => viewModel.SourceHeight = value,
                    SetSourceIsHdr = value => viewModel.SourceIsHdr = value,
                    IsRecording = () => viewModel.IsRecording,
                    IsHdrEnabled = () => viewModel.IsHdrEnabled,
                    SetIsHdrEnabled = value => viewModel.IsHdrEnabled = value,
                    SetSourceTelemetryAvailability = value => viewModel.SourceTelemetryAvailability = value,
                    SetSourceTelemetryOriginDetail = value => viewModel.SourceTelemetryOriginDetail = value,
                    SetSourceTelemetryConfidence = value => viewModel.SourceTelemetryConfidence = value,
                    SetSourceTelemetryDiagnosticSummary = value => viewModel.SourceTelemetryDiagnosticSummary = value,
                    GetSourceTelemetryTimestampUtc = () => viewModel.SourceTelemetryTimestampUtc,
                    SetSourceTelemetryTimestampUtc = value => viewModel.SourceTelemetryTimestampUtc = value,
                    SetDetectedSourceFrameRate = value => viewModel.DetectedSourceFrameRate = value,
                    SetDetectedSourceFrameRateArg = value => viewModel.DetectedSourceFrameRateArg = value,
                    SetSourceFrameRateOrigin = value => viewModel.SourceFrameRateOrigin = value,
                    GetSourceTelemetrySummaryText = () => viewModel.SourceTelemetrySummaryText,
                    SetSourceTelemetrySummaryText = value => viewModel.SourceTelemetrySummaryText = value,
                    GetLastSourceModeKey = () => viewModel._lastSourceModeKey,
                    SetLastSourceModeKey = value => viewModel._lastSourceModeKey = value,
                    GetSelectedResolution = () => viewModel.SelectedResolution,
                    HasUserOverriddenResolutionForCurrentMode = () => viewModel._hasUserOverriddenResolutionForCurrentMode,
                    SetHasUserOverriddenResolutionForCurrentMode = value => viewModel._hasUserOverriddenResolutionForCurrentMode = value,
                    IsAutoFrameRateSelected = () => viewModel.IsAutoFrameRateSelected,
                    HasUserOverriddenFrameRateForCurrentMode = () => viewModel._hasUserOverriddenFrameRateForCurrentMode,
                    SetHasUserOverriddenFrameRateForCurrentMode = value => viewModel._hasUserOverriddenFrameRateForCurrentMode = value,
                    ForceSourceAutoRetarget = () => viewModel._forceSourceAutoRetarget,
                    SetForceSourceAutoRetarget = value => viewModel._forceSourceAutoRetarget = value,
                    AvailableResolutionCount = () => viewModel.AvailableResolutions.Count,
                    SetPendingModeOptionsRefresh = value => viewModel._pendingModeOptionsRefresh = value,
                    RebuildResolutionOptions = viewModel.RebuildResolutionOptions,
                    UpdateTargetSummary = viewModel.UpdateTargetSummary,
                });
        }

        private static MainViewModelRuntimeEventIngressController CreateRuntimeEventIngressController(
            MainViewModel viewModel,
            MainViewModelPreviewLifecycleController previewLifecycleController)
        {
            return new MainViewModelRuntimeEventIngressController(
                new MainViewModelRuntimeEventIngressControllerContext
                {
                    AttachFormatProbeCompleted = handler => viewModel._deviceService.FormatProbeCompleted += handler,
                    DetachFormatProbeCompleted = handler => viewModel._deviceService.FormatProbeCompleted -= handler,
                    OnDeviceFormatProbeCompleted = viewModel._deviceFormatProbeController.OnDeviceFormatProbeCompleted,
                    AttachCaptureStatusChanged = handler => viewModel._captureService.StatusChanged += handler,
                    DetachCaptureStatusChanged = handler => viewModel._captureService.StatusChanged -= handler,
                    AttachCaptureErrorOccurred = handler => viewModel._captureService.ErrorOccurred += handler,
                    DetachCaptureErrorOccurred = handler => viewModel._captureService.ErrorOccurred -= handler,
                    AttachCapturePreCleanupRequested = handler => viewModel._captureService.PreCleanupRequested += handler,
                    DetachCapturePreCleanupRequested = handler => viewModel._captureService.PreCleanupRequested -= handler,
                    AttachFrameCaptured = handler => viewModel._captureService.FrameCaptured += handler,
                    DetachFrameCaptured = handler => viewModel._captureService.FrameCaptured -= handler,
                    AttachAudioLevelUpdated = handler => viewModel._captureService.AudioLevelUpdated += handler,
                    DetachAudioLevelUpdated = handler => viewModel._captureService.AudioLevelUpdated -= handler,
                    OnAudioLevelUpdated = viewModel.OnAudioLevelUpdated,
                    AttachMicrophoneAudioLevelUpdated = handler => viewModel._captureService.MicrophoneAudioLevelUpdated += handler,
                    DetachMicrophoneAudioLevelUpdated = handler => viewModel._captureService.MicrophoneAudioLevelUpdated -= handler,
                    OnMicrophoneAudioLevelUpdated = viewModel.OnMicrophoneAudioLevelUpdated,
                    AttachSourceTelemetryUpdated = handler => viewModel._captureService.SourceTelemetryUpdated += handler,
                    DetachSourceTelemetryUpdated = handler => viewModel._captureService.SourceTelemetryUpdated -= handler,
                    OnSourceTelemetryUpdated = viewModel._sourceTelemetryController.OnSourceTelemetryUpdated,
                    AttachAudioDevicesChanged = handler => viewModel._audioDeviceWatcher.DevicesChanged += handler,
                    DetachAudioDevicesChanged = handler => viewModel._audioDeviceWatcher.DevicesChanged -= handler,
                    OnAudioDevicesChanged = viewModel.OnAudioDevicesChanged,
                    TryEnqueueOnUiThread = operation => viewModel._dispatcherQueue.TryEnqueue(() => operation()),
                    GetRuntimeSnapshot = viewModel._captureService.GetRuntimeSnapshot,
                    SetStatusText = value => viewModel.StatusText = value,
                    UpdateLiveCaptureInfo = snapshot => viewModel.UpdateLiveCaptureInfo(snapshot),
                    UpdateHdrRuntimeStatusFromCapture = snapshot => viewModel.UpdateHdrRuntimeStatusFromCapture(snapshot),
                    SetIsInitialized = value => viewModel.IsInitialized = value,
                    IsCaptureInitialized = () => viewModel._captureService.IsInitialized,
                    IsInitialized = () => viewModel.IsInitialized,
                    SetIsPreviewing = value => viewModel.IsPreviewing = value,
                    IsVideoPreviewActive = () => viewModel._captureService.IsVideoPreviewActive,
                    IsPreviewing = () => viewModel.IsPreviewing,
                    SetIsRecording = value => viewModel.IsRecording = value,
                    IsCaptureRecording = () => viewModel._captureService.IsRecording,
                    IsRecording = () => viewModel.IsRecording,
                    ResetAudioMeter = viewModel.ResetAudioMeter,
                    GetPreviewRendererStopHandlers = () =>
                    {
                        var handlers = viewModel.PreviewRendererStopRequested;
                        return handlers != null
                            ? Array.ConvertAll(handlers.GetInvocationList(), handler => (Func<Task>)handler)
                            : Array.Empty<Func<Task>>();
                    },
                    ReinitializeDeviceAsync = previewLifecycleController.ReinitializeDeviceAsync,
                    EnqueueUiOperation = (operation, operationName) => viewModel.EnqueueUiOperation(operation, operationName),
                });
        }
    }
}

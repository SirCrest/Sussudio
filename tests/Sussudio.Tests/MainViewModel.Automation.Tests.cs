using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

static partial class Program
{
    private static Task MainViewModelAutomation_RoutesPreviewVolumePersistenceThroughSaveHook()
    {
        var vmType = RequireType("Sussudio.ViewModels.MainViewModel");

        // SavePreviewVolume must exist as the persistence hook
        var savePreviewVolume = vmType.GetMethod(
            "SavePreviewVolume",
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        AssertNotNull(savePreviewVolume, "MainViewModel.SavePreviewVolume");

        // PreviewVolume observable property must exist
        var previewVolume = vmType.GetProperty("PreviewVolume", BindingFlags.Instance | BindingFlags.Public);
        AssertNotNull(previewVolume, "MainViewModel.PreviewVolume");

        // ShowAllCaptureOptions observable property must exist
        var showAll = vmType.GetProperty("ShowAllCaptureOptions", BindingFlags.Instance | BindingFlags.Public);
        AssertNotNull(showAll, "MainViewModel.ShowAllCaptureOptions");

        // IsAudioPreviewEnabled observable property must exist
        var audioPreview = vmType.GetProperty("IsAudioPreviewEnabled", BindingFlags.Instance | BindingFlags.Public);
        AssertNotNull(audioPreview, "MainViewModel.IsAudioPreviewEnabled");

        // Automation interface method must exist
        var getOptionsSnapshot = vmType.GetMethod(
            "GetAutomationOptionsSnapshotAsync",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        AssertNotNull(getOptionsSnapshot, "MainViewModel.GetAutomationOptionsSnapshotAsync");

        return Task.CompletedTask;
    }

    private static Task MainViewModelAutomation_RoutesRecordingThroughSharedTransitionGate()
    {
        var automationText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.Automation.cs")
            .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/ViewModels/MainViewModel.AutomationRecordingSettings.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/ViewModels/MainViewModel.FlashbackExport.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/ViewModels/MainViewModel.FlashbackPlayback.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/ViewModels/MainViewModel.AutomationSnapshots.cs")
                .Replace("\r\n", "\n");
        var captureText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.Capture.cs")
            .Replace("\r\n", "\n");
        var recordingLifecycleText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.RecordingLifecycle.cs")
            .Replace("\r\n", "\n");
        var recordingStateText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.RecordingState.cs")
            .Replace("\r\n", "\n");
        var rootViewModelText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs")
            .Replace("\r\n", "\n");
        var dispatcherText = ReadAutomationCommandDispatcherFamilyText();

        AssertContains(recordingLifecycleText, "internal Task SetRecordingDesiredStateAsync");
        AssertContains(recordingLifecycleText, "public Task ToggleRecordingAsync()\n        => SetRecordingDesiredStateAsync(!IsRecording);");
        AssertContains(recordingLifecycleText, "Recording transition already in progress.");
        AssertContains(recordingLifecycleText, "await inFlight;");
        AssertContains(recordingLifecycleText, "private Task BeginRecordingTransitionAsync(bool enabled, CancellationToken cancellationToken = default)");
        AssertContains(recordingLifecycleText, "var task = RecordingTransitionInnerAsync(enabled, cancellationToken);");
        AssertContains(recordingLifecycleText, "await StartRecordingAsync(cancellationToken);");
        AssertContains(recordingLifecycleText, "await StopRecordingAsync(cancellationToken);");
        AssertContains(recordingLifecycleText, "await BeginRecordingTransitionAsync(enabled, cancellationToken);");
        AssertContains(recordingLifecycleText, "await _sessionCoordinator.StartRecordingAsync(settings, cancellationToken);");
        AssertContains(recordingLifecycleText, "await _sessionCoordinator.StopRecordingAsync(cancellationToken);");
        AssertDoesNotContain(captureText, "private Task BeginRecordingTransitionAsync(bool enabled, CancellationToken cancellationToken = default)");
        AssertDoesNotContain(captureText, "await _sessionCoordinator.StartRecordingAsync(settings, cancellationToken);");
        AssertContains(recordingStateText, "private readonly Stopwatch _recordingStopwatch = new();");
        AssertContains(recordingStateText, "public partial ObservableCollection<string> AvailableRecordingFormats");
        AssertContains(recordingStateText, "public partial string OutputPath");
        AssertContains(recordingStateText, "public partial bool IsRecording");
        AssertDoesNotContain(rootViewModelText, "public partial ObservableCollection<string> AvailableRecordingFormats");
        AssertDoesNotContain(rootViewModelText, "public partial string OutputPath");
        AssertContains(automationText, "return SetRecordingDesiredStateAsync(enabled, cancellationToken);");
        AssertContains(dispatcherText, "return CreateResponse(correlationId, $\"Recording {(enabled ? \"started\" : \"stopped\")}.\"");
        AssertContains(dispatcherText, "var snapshot = await _diagnosticsHub.RefreshSnapshotNowAsync(cancellationToken).ConfigureAwait(false);");
        AssertContains(dispatcherText, "snapshot: snapshot");

        return Task.CompletedTask;
    }

    private static Task MainViewModelAutomation_UsesAsyncFlashbackAndProbeSurface()
    {
        var automationInterfaceType = RequireType("Sussudio.Services.Automation.IAutomationViewModel");
        AssertEqual(
            false,
            automationInterfaceType.GetProperty("IsMicrophoneEnabled") != null,
            "IAutomationViewModel sync microphone setter");
        AssertTaskReturningMethod(automationInterfaceType, "SetMicrophoneEnabledAsync", resultType: null);
        AssertTaskReturningMethod(automationInterfaceType, "SetFlashbackEnabledAsync", resultType: null);
        AssertTaskReturningMethod(automationInterfaceType, "ExecuteFlashbackActionAsync", typeof(bool));
        AssertTaskReturningMethod(
            automationInterfaceType,
            "GetFlashbackSegmentsAsync",
            typeof(IReadOnlyList<>).MakeGenericType(RequireType("Sussudio.Models.FlashbackSegmentInfo")));
        AssertTaskReturningMethod(
            automationInterfaceType,
            "ProbeVideoSourceAsync",
            RequireType("Sussudio.Models.VideoSourceProbeResult"));
        AssertTaskReturningMethod(
            automationInterfaceType,
            "ProbePreviewColorAsync",
            RequireType("Sussudio.Models.PreviewColorProbeResult"));

        var interfaceText = ReadRepoFile("Sussudio/Services/Automation/IAutomationViewModel.cs")
            .Replace("\r\n", "\n");
        var dispatcherText = ReadAutomationCommandDispatcherFamilyText();
        var automationText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.Automation.cs")
            .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/ViewModels/MainViewModel.AutomationRecordingSettings.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/ViewModels/MainViewModel.FlashbackExport.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/ViewModels/MainViewModel.FlashbackPlayback.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/ViewModels/MainViewModel.AutomationSnapshots.cs")
                .Replace("\r\n", "\n");
        var viewModelText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs")
            .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/ViewModels/MainViewModel.Dispatching.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/ViewModels/MainViewModel.AudioPropertyChanges.cs")
                .Replace("\r\n", "\n");
        var coordinatorText = ReadRepoFile("Sussudio/Services/Capture/CaptureSessionCoordinator.cs")
            .Replace("\r\n", "\n");
        var pipeServerText = ReadRepoFile("Sussudio/Services/Automation/NamedPipeAutomationServer.cs")
            .Replace("\r\n", "\n");
        var captureText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.Capture.cs")
            .Replace("\r\n", "\n");
        var deviceManagementText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.DeviceManagement.cs")
            .Replace("\r\n", "\n");
        var captureServiceText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.cs")
            .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.Audio.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.Probes.cs")
                .Replace("\r\n", "\n");

        AssertDoesNotContain(interfaceText, "bool FlashbackPlay();");
        AssertDoesNotContain(interfaceText, "bool FlashbackPause();");
        AssertDoesNotContain(interfaceText, "bool FlashbackGoLive();");
        AssertDoesNotContain(interfaceText, "bool FlashbackBeginScrub(TimeSpan position);");
        AssertDoesNotContain(interfaceText, "bool FlashbackEndScrub();");
        AssertDoesNotContain(interfaceText, "VideoSourceProbeResult ProbeVideoSource();");
        AssertDoesNotContain(interfaceText, "PreviewColorProbeResult ProbePreviewColor();");
        AssertContains(dispatcherText, "await _viewModel.ExecuteFlashbackActionAsync(action, position, cancellationToken).ConfigureAwait(false)");
        AssertContains(dispatcherText, "return CreateFlashbackActionRejectedResponse(");
        AssertContains(dispatcherText, "errorCode: \"flashback-action-failed\"");
        AssertContains(dispatcherText, "RequestedPositionMs = requestedPositionMs");
        AssertContains(dispatcherText, "LastCommandFailureUtcUnixMs = snapshot.FlashbackPlaybackLastCommandFailureUtcUnixMs");
        AssertContains(dispatcherText, "var useSelectionRange = GetBool(payload, \"useSelectionRange\") ?? false;");
        AssertContains(dispatcherText, "var force = GetBool(payload, \"force\") ?? false;");
        AssertContains(dispatcherText, "ExportFlashbackAutomationAsync(seconds, outputPath, useSelectionRange, force, cancellationToken)");
        AssertContains(dispatcherText, "CaptureService.ClassifyFlashbackExportFailureKind(exportResult.StatusMessage)");
        AssertContains(dispatcherText, "FailureKind = failureKind");
        AssertContains(dispatcherText, "if (positionMs.HasValue &&\n                        (!double.IsFinite(positionMs.Value) ||\n                         positionMs.Value < 0 ||\n                         positionMs.Value > TimeSpan.MaxValue.TotalMilliseconds))");
        AssertContains(dispatcherText, "Flashback positionMs must be finite, non-negative, and within TimeSpan range.");
        AssertContains(dispatcherText, "case AutomationFlashbackAction.SetInPoint:");
        AssertContains(dispatcherText, "case AutomationFlashbackAction.SetOutPoint:");
        AssertContains(dispatcherText, "case AutomationFlashbackAction.ClearInOutPoints:");
        AssertContains(dispatcherText, "AutomationFlashbackAction.BeginScrub => RequireDouble(payload, \"positionMs\")");
        AssertContains(dispatcherText, "AutomationFlashbackAction.UpdateScrub => RequireDouble(payload, \"positionMs\")");
        AssertContains(dispatcherText, "AutomationFlashbackAction.EndScrub => GetDouble(payload, \"positionMs\")");
        AssertContains(dispatcherText, "await _viewModel.SetFlashbackEnabledAsync(enabled, cancellationToken).ConfigureAwait(false)");
        AssertContains(dispatcherText, "await _viewModel.GetFlashbackSegmentsAsync(cancellationToken).ConfigureAwait(false)");
        AssertContains(dispatcherText, "await _viewModel.ProbeVideoSourceAsync(cancellationToken).ConfigureAwait(false)");
        AssertContains(dispatcherText, "await _viewModel.ProbePreviewColorAsync(cancellationToken).ConfigureAwait(false)");
        AssertContains(dispatcherText, "await _viewModel.SetMicrophoneEnabledAsync(enabled, cancellationToken).ConfigureAwait(false)");
        AssertDoesNotContain(dispatcherText, "_viewModel.IsMicrophoneEnabled =");
        AssertContains(automationText, "public Task<bool> ExecuteFlashbackActionAsync(");
        AssertContains(automationText, "public void ReportFlashbackPlaybackRejection(string action, string logToken)");
        AssertContains(automationText, "lastFailure={lastFailure}");
        AssertContains(automationText, "StatusText = message;");
        AssertContains(automationText, "case AutomationFlashbackAction.SetInPoint:");
        AssertContains(automationText, "case AutomationFlashbackAction.SetOutPoint:");
        AssertContains(automationText, "case AutomationFlashbackAction.ClearInOutPoints:");
        AssertContains(automationText, "case AutomationFlashbackAction.BeginScrub:");
        AssertContains(automationText, "return FlashbackBeginScrub(position ?? TimeSpan.Zero);");
        AssertContains(automationText, "case AutomationFlashbackAction.UpdateScrub:");
        AssertContains(automationText, "return FlashbackUpdateScrub(position ?? TimeSpan.Zero);");
        AssertContains(automationText, "case AutomationFlashbackAction.EndScrub:");
        AssertContains(automationText, "? FlashbackEndScrubAt(position.Value)\n                    : FlashbackEndScrub();");
        var automationPlayBlock = ExtractTextBetween(
            automationText,
            "case AutomationFlashbackAction.Play:",
            "            case AutomationFlashbackAction.Pause:");
        AssertContains(automationPlayBlock, "if (position.HasValue)");
        AssertContains(automationPlayBlock, "if (!FlashbackSeek(position.Value))");
        AssertContains(automationPlayBlock, "return FlashbackPlay();");
        AssertDoesNotContain(automationPlayBlock, "FlashbackBeginScrub(position.Value);");
        AssertDoesNotContain(automationPlayBlock, "FlashbackEndScrub();");
        AssertContains(automationText, "if (useSelectionRange)");
        AssertContains(automationText, "FLASHBACK_EXPORT_START_UI_ENQUEUE_FAILED source=automation");
        AssertContains(automationText, "if (IsCurrentFlashbackExport(exportId, exportCts))\n            {\n                IsFlashbackExporting = true;\n                FlashbackExportProgress = 0;\n            }");
        AssertContains(automationText, "FLASHBACK_EXPORT_PROGRESS_UI_ENQUEUE_FAILED source=automation percent={p.Percent:0.###}");
        AssertContains(automationText, "FLASHBACK_EXPORT_PROGRESS_UI_ENQUEUE_FAILED source=ui percent={p.Percent:0.###}");
        AssertContains(automationText, "public Task SetFlashbackEnabledAsync(bool enabled, CancellationToken cancellationToken = default)");
        AssertContains(automationText, "InvokeOnUiThreadAsync(() => ExecuteFlashbackAction(action, position), cancellationToken)");
        AssertContains(automationText, "=> FromSynchronousSnapshot(ProbeVideoSource, cancellationToken);");
        AssertContains(automationText, "=> FromSynchronousSnapshot(ProbePreviewColor, cancellationToken);");
        AssertContains(automationText, "=> FromSynchronousSnapshot(GetFlashbackSegments, cancellationToken);");
        AssertContains(automationText, "public Task<CaptureRuntimeSnapshot> GetCaptureRuntimeSnapshotAsync(CancellationToken cancellationToken = default)\n        => FromSynchronousSnapshot(_captureService.GetRuntimeSnapshot, cancellationToken);");
        AssertContains(automationText, "InvokeOnUiThreadAsync(BuildCaptureSettings, cancellationToken)");
        AssertContains(automationText, "await _sessionCoordinator.RestartFlashbackAsync(settings, cancellationToken).ConfigureAwait(false)");
        AssertContains(automationText, "_flashbackBitrateSamples.Clear();\n                return true;\n            },\n            cancellationToken).ConfigureAwait(false);");
        AssertContains(automationText, "=> InvokeOnUiThreadAsync(() => RefreshDevicesAsync(cancellationToken), cancellationToken);");
        AssertContains(automationText, "await StartPreviewAsync(userInitiated: true, cancellationToken);");
        AssertContains(automationText, "await StopPreviewAsync(userInitiated: true, teardownPipeline: false, cancellationToken);");
        AssertContains(captureText, "private async Task InitializeDeviceAsync(CancellationToken cancellationToken = default)");
        AssertContains(captureText, "public async Task StartPreviewAsync(bool userInitiated = true, CancellationToken cancellationToken = default)");
        AssertContains(captureText, "public async Task StopPreviewAsync(bool userInitiated, bool teardownPipeline, CancellationToken cancellationToken)");
        AssertContains(deviceManagementText, "public async Task RefreshDevicesAsync(CancellationToken cancellationToken = default)");
        AssertContains(deviceManagementText, "catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)\n                {\n                    throw;\n                }");
        AssertContains(automationText, "_suppressMicrophoneMonitorUpdate = true;");
        AssertContains(automationText, "await _sessionCoordinator.UpdateMicrophoneMonitorAsync(");
        AssertContains(automationText, "cancellationToken).ConfigureAwait(false);");
        AssertContains(automationText, "IsMicrophoneEnabled = enabled;\n                }\n                finally\n                {\n                    _suppressMicrophoneMonitorUpdate = false;\n                }\n\n                return true;\n            },\n            cancellationToken).ConfigureAwait(false);");
        AssertContains(captureServiceText, "var previousEnabled = _micMonitorEnabled;");
        AssertContains(captureServiceText, "await DisposeMicrophoneCaptureAsync().ConfigureAwait(false);\n\n                _micMonitorEnabled = enabled;");
        AssertContains(captureServiceText, "private const int PreviewFrameCaptureRendererWaitTimeoutMs = 2000;");
        AssertContains(captureServiceText, "while (_isVideoPreviewActive && !cancellationToken.IsCancellationRequested)");
        AssertContains(captureServiceText, "await Task.Delay(PreviewFrameCaptureRendererPollMs, cancellationToken).ConfigureAwait(false);");
        var microphoneUpdateIndex = automationText.IndexOf(
            "await _sessionCoordinator.UpdateMicrophoneMonitorAsync(",
            StringComparison.Ordinal);
        var microphonePersistIndex = automationText.IndexOf(
            "IsMicrophoneEnabled = enabled;",
            StringComparison.Ordinal);
        AssertEqual(
            true,
            microphoneUpdateIndex >= 0 && microphonePersistIndex > microphoneUpdateIndex,
            "automation microphone persists after monitor update");
        AssertContains(viewModelText, "if (_suppressMicrophoneMonitorUpdate)");
        AssertContains(viewModelText, "registration.Dispose();\n                registration = default;\n\n                if (cancellationToken.IsCancellationRequested)");
        AssertContains(coordinatorText, "return Task.FromCanceled(cancellationToken);");
        AssertContains(coordinatorText, "CancellationTokenRegistration cancellationRegistration = default;");
        AssertContains(coordinatorText, "cancellationRegistration = cancellationToken.Register");
        AssertContains(coordinatorText, "DisposeCancellationRegistrationBestEffort(cancellationRegistration, \"enqueue_failed\");");
        AssertContains(coordinatorText, "DisposeCancellationRegistrationBestEffort(workItem.CancellationRegistration, \"begin_process\");");
        AssertContains(coordinatorText, "DisposeCancellationRegistrationBestEffort(pending.CancellationRegistration, \"fail_pending\");");
        AssertContains(coordinatorText, "CAPTURE_COORD_CANCEL_REG_DISPOSE_WARN");
        AssertContains(coordinatorText, "CancelWorkerBestEffort();");
        AssertContains(coordinatorText, "DisposeWorkerCancellationBestEffort(\"worker_completed\");");
        AssertContains(coordinatorText, "CAPTURE_COORD_WORKER_CANCEL_WARN");
        AssertContains(coordinatorText, "CAPTURE_COORD_WORKER_CTS_DISPOSE_WARN");
        AssertContains(coordinatorText, "public bool PropagateCancellationToOperation { get; init; }");
        AssertContains(coordinatorText, "bool propagateCancellationToOperation = false");
        AssertContains(coordinatorText, "propagateCancellationToOperation: true");
        AssertContains(pipeServerText, "var requestCancellation = CancellationTokenSource.CreateLinkedTokenSource(requestTimeout.Token, cancellationToken);");
        AssertContains(pipeServerText, "if (await WaitForDispatchCompletionAsync(dispatchTask, requestCancellation.Token).ConfigureAwait(false))");
        AssertContains(pipeServerText, "using var registration = cancellationToken.Register(");
        AssertContains(pipeServerText, "ObserveTimedOutDispatch(dispatchTask, request.Command, requestTimeout, requestCancellation);");
        AssertContains(pipeServerText, "Request timed out after {_requestTimeoutMs} ms.");
        AssertContains(pipeServerText, "\"request-timeout\"");
        AssertDoesNotContain(dispatcherText, "WaitConditionRefreshCadenceMs");

        return Task.CompletedTask;
    }

    private static void AssertTaskReturningMethod(Type type, string methodName, Type? resultType)
    {
        var method = type.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);
        AssertNotNull(method, $"{type.FullName}.{methodName}");
        AssertEqual(
            true,
            method!.GetParameters().Any(parameter => parameter.ParameterType == typeof(CancellationToken)),
            $"{type.FullName}.{methodName} cancellation token");

        if (resultType == null)
        {
            AssertEqual(typeof(Task).FullName, method.ReturnType.FullName, $"{type.FullName}.{methodName} return type");
            return;
        }

        AssertEqual(true, method.ReturnType.IsGenericType, $"{type.FullName}.{methodName} generic Task return");
        AssertEqual(
            typeof(Task<>).FullName,
            method.ReturnType.GetGenericTypeDefinition().FullName,
            $"{type.FullName}.{methodName} generic Task definition");
        AssertEqual(
            resultType.FullName,
            method.ReturnType.GenericTypeArguments[0].FullName,
            $"{type.FullName}.{methodName} task result");
    }

    private static Task DiagnosticsSnapshotRefresh_IsSerializedForRecordingResponses()
    {
        var diagnosticsHubText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.cs")
            .Replace("\r\n", "\n");
        var diagnosticsEvaluationText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.Evaluation.cs")
            .Replace("\r\n", "\n");
        var diagnosticsEvaluationPolicyText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.EvaluationPolicy.cs")
            .Replace("\r\n", "\n");
        var diagnosticsDiagnosticEvaluationText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.DiagnosticEvaluation.cs")
            .Replace("\r\n", "\n");
        var diagnosticsDiagnosticEvaluationLanesText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.DiagnosticEvaluationLanes.cs")
            .Replace("\r\n", "\n");
        var diagnosticsAlertsText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.Alerts.cs")
            .Replace("\r\n", "\n");
        var diagnosticsSignalAlertsText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SignalAlerts.cs")
            .Replace("\r\n", "\n");
        var diagnosticsFlashbackAlertsText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.FlashbackAlerts.cs")
            .Replace("\r\n", "\n");
        var diagnosticsEventsText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.DiagnosticEvents.cs")
            .Replace("\r\n", "\n");
        var diagnosticsVerificationText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.Verification.cs")
            .Replace("\r\n", "\n");
        var diagnosticsLifecycleText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.Lifecycle.cs")
            .Replace("\r\n", "\n");
        var diagnosticsHdrText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.Hdr.cs")
            .Replace("\r\n", "\n");
        var diagnosticsSnapshotsText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.Snapshots.cs")
            .Replace("\r\n", "\n");
        var diagnosticsSnapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs")
            .Replace("\r\n", "\n");
        var diagnosticsSnapshotStateText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotState.cs")
            .Replace("\r\n", "\n");
        var diagnosticsPreviewPacingText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.PreviewPacing.cs")
            .Replace("\r\n", "\n");
        var diagnosticsOutputFilesText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.OutputFiles.cs")
            .Replace("\r\n", "\n");
        var diagnosticsProcessMetricsText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.ProcessMetrics.cs")
            .Replace("\r\n", "\n");
        var diagnosticsTimelineText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.Timeline.cs")
            .Replace("\r\n", "\n");
        var diagnosticsTimelineProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.TimelineProjection.cs")
            .Replace("\r\n", "\n");
        var diagnosticsText = diagnosticsHubText + "\n" + diagnosticsEvaluationText + "\n" + diagnosticsEvaluationPolicyText + "\n" + diagnosticsDiagnosticEvaluationText + "\n" + diagnosticsDiagnosticEvaluationLanesText + "\n" + diagnosticsAlertsText + "\n" + diagnosticsSignalAlertsText + "\n" + diagnosticsFlashbackAlertsText + "\n" + diagnosticsEventsText + "\n" + diagnosticsVerificationText + "\n" + diagnosticsLifecycleText + "\n" + diagnosticsHdrText + "\n" + diagnosticsSnapshotsText + "\n" + diagnosticsSnapshotProjectionText + "\n" + diagnosticsSnapshotStateText + "\n" + diagnosticsPreviewPacingText + "\n" + diagnosticsOutputFilesText + "\n" + diagnosticsProcessMetricsText + "\n" + diagnosticsTimelineText + "\n" + diagnosticsTimelineProjectionText;
        var countersText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.Counters.cs")
            .Replace("\r\n", "\n");
        var dispatcherText = ReadAutomationCommandDispatcherFamilyText();

        AssertContains(diagnosticsEvaluationPolicyText, "private static string FormatPreviewSlowFrameAlertDetail");
        AssertDoesNotContain(diagnosticsEvaluationText, "private static string FormatPreviewSlowFrameAlertDetail");
        AssertContains(diagnosticsEvaluationText, "private PerformanceEvaluation EvaluatePerformance(");
        AssertContains(diagnosticsDiagnosticEvaluationText, "private static DiagnosticEvaluation BuildDiagnosticEvaluation(");
        AssertContains(diagnosticsDiagnosticEvaluationText, "var lanes = BuildDiagnosticEvaluationLanes(");
        AssertDoesNotContain(diagnosticsDiagnosticEvaluationText, "var sourceTarget =");
        AssertContains(diagnosticsDiagnosticEvaluationLanesText, "private static DiagnosticEvaluationLanes BuildDiagnosticEvaluationLanes(");
        AssertContains(diagnosticsDiagnosticEvaluationLanesText, "private readonly record struct DiagnosticEvaluationLanes(");
        AssertDoesNotContain(diagnosticsEvaluationText, "private static DiagnosticEvaluation BuildDiagnosticEvaluation(");
        AssertDoesNotContain(diagnosticsHubText, "private PerformanceEvaluation EvaluatePerformance(");
        AssertDoesNotContain(diagnosticsHubText, "private static DiagnosticEvaluation BuildDiagnosticEvaluation(");
        AssertContains(diagnosticsAlertsText, "private void UpdateAlerts(AutomationSnapshot snapshot, FlashbackRecordingRecentCounters flashbackRecordingRecent)");
        AssertDoesNotContain(diagnosticsAlertsText, "private void AddEventThrottled(");
        AssertDoesNotContain(diagnosticsAlertsText, "public IReadOnlyList<DiagnosticsEvent> GetRecentEvents");
        AssertContains(diagnosticsEventsText, "private void ObserveFlashbackExportCompletion(AutomationSnapshot snapshot)");
        AssertContains(diagnosticsEventsText, "private void AddEventThrottled(");
        AssertContains(diagnosticsEventsText, "private void SetAlertState(");
        AssertContains(diagnosticsEventsText, "public IReadOnlyList<DiagnosticsEvent> GetRecentEvents");
        AssertContains(diagnosticsAlertsText, "UpdateSignalAlerts(");
        AssertContains(diagnosticsSignalAlertsText, "private void UpdateSignalAlerts(");
        AssertContains(diagnosticsSignalAlertsText, "\"preview-blank\"");
        AssertDoesNotContain(diagnosticsAlertsText, "\"preview-blank\"");
        AssertContains(diagnosticsAlertsText, "UpdateFlashbackAlerts(snapshot, flashbackRecordingRecent);");
        AssertContains(diagnosticsFlashbackAlertsText, "private void UpdateFlashbackAlerts(");
        AssertContains(diagnosticsFlashbackAlertsText, "\"flashback-export-stalled\"");
        AssertContains(diagnosticsFlashbackAlertsText, "\"flashback-playback-slow\"");
        AssertDoesNotContain(diagnosticsAlertsText, "\"flashback-export-stalled\"");
        AssertDoesNotContain(diagnosticsHubText, "private void UpdateAlerts(AutomationSnapshot snapshot, FlashbackRecordingRecentCounters flashbackRecordingRecent)");
        AssertContains(diagnosticsVerificationText, "public async Task<RecordingVerificationResult> VerifyLastRecordingAsync");
        AssertContains(diagnosticsVerificationText, "private static CaptureRuntimeSnapshot ApplyVerificationProfile(");
        AssertContains(diagnosticsVerificationText, "private bool ShouldAutoVerifySnapshot(");
        AssertContains(diagnosticsVerificationText, "private RecordingVerificationResult? CaptureLastVerificationForSnapshot(");
        AssertContains(diagnosticsVerificationText, "private void ScheduleAutoVerificationIfNeeded(");
        AssertDoesNotContain(diagnosticsHubText, "public async Task<RecordingVerificationResult> VerifyLastRecordingAsync");
        AssertContains(diagnosticsPreviewPacingText, "private static PreviewPacingClassification ClassifyPreviewPacing(");
        AssertContains(diagnosticsSnapshotsText, "ClassifyPreviewPacing(");
        AssertDoesNotContain(diagnosticsSnapshotsText, "new PreviewPacingClassificationInput");
        AssertContains(diagnosticsLifecycleText, "public void Start()");
        AssertContains(diagnosticsLifecycleText, "private async Task RunLoopAsync(CancellationToken cancellationToken)");
        AssertDoesNotContain(diagnosticsHubText, "public void Start()");
        AssertContains(diagnosticsHdrText, "private static HdrTruthVerdict BuildHdrTruthVerdict(");
        AssertContains(diagnosticsHdrText, "private static PreviewHdrState BuildPreviewHdrState(");
        AssertContains(diagnosticsHdrText, "private readonly record struct PreviewHdrState(");
        AssertContains(diagnosticsHdrText, "private static bool IsHdrSubtype(string? subtype)");
        AssertDoesNotContain(diagnosticsHubText, "private static HdrTruthVerdict BuildHdrTruthVerdict(");
        AssertContains(diagnosticsSnapshotsText, "var previewHdrState = BuildPreviewHdrState(captureRuntime, viewModelSnapshot, previewRuntime);");
        AssertDoesNotContain(diagnosticsSnapshotsText, "var previewHdrInputDetected =");
        AssertContains(diagnosticsSnapshotsText, "private async Task<AutomationSnapshot> RefreshSnapshotCoreAsync");
        AssertContains(diagnosticsSnapshotProjectionText, "private AutomationSnapshot BuildAutomationSnapshot(");
        AssertContains(diagnosticsSnapshotProjectionText, "new AutomationSnapshot");
        AssertContains(diagnosticsSnapshotProjectionText, "PreviewPacingLikelySlowStage = previewPacingClassification.LikelySlowStage");
        AssertContains(diagnosticsSnapshotsText, "var snapshot = BuildAutomationSnapshot(");
        AssertDoesNotContain(diagnosticsSnapshotsText, "new AutomationSnapshot");
        AssertContains(diagnosticsSnapshotsText, "AppendPerformanceTimelineEntry(snapshot);");
        AssertContains(diagnosticsSnapshotStateText, "private AudioSignalState UpdateAudioSignalState(");
        AssertContains(diagnosticsSnapshotStateText, "private bool UpdateRecordingFileGrowthState(");
        AssertContains(diagnosticsSnapshotStateText, "private readonly record struct AudioSignalState(");
        AssertContains(diagnosticsSnapshotsText, "UpdateAudioSignalState(viewModelSnapshot, nowTick);");
        AssertContains(diagnosticsSnapshotsText, "UpdateRecordingFileGrowthState(");
        AssertDoesNotContain(diagnosticsSnapshotsText, "var audioSignalPresent = viewModelSnapshot.AudioPeak >= AudioSignalThreshold;");
        AssertContains(diagnosticsOutputFilesText, "private LastOutputProbe ProbeLastOutput(");
        AssertContains(diagnosticsOutputFilesText, "private readonly record struct LastOutputProbe(");
        AssertContains(diagnosticsProcessMetricsText, "private ProcessResourceSnapshot CaptureProcessResourceSnapshot()");
        AssertContains(diagnosticsProcessMetricsText, "private double CalculateProcessCpuPercent(double processCpuTotalMs)");
        AssertContains(diagnosticsProcessMetricsText, "private readonly record struct ProcessResourceSnapshot(");
        AssertContains(diagnosticsTimelineText, "public IReadOnlyList<PerformanceTimelineEntry> GetPerformanceTimeline");
        AssertContains(diagnosticsTimelineText, "private void AppendPerformanceTimelineEntry(AutomationSnapshot snapshot)");
        AssertContains(diagnosticsTimelineText, "BuildPerformanceTimelineEntry(snapshot)");
        AssertDoesNotContain(diagnosticsTimelineText, "new PerformanceTimelineEntry\n        {");
        AssertContains(diagnosticsTimelineProjectionText, "private static PerformanceTimelineEntry BuildPerformanceTimelineEntry(AutomationSnapshot snapshot)");
        AssertContains(diagnosticsTimelineProjectionText, "FlashbackPlaybackCommandsEnqueued = snapshot.FlashbackPlaybackCommandsEnqueued");
        AssertDoesNotContain(diagnosticsHubText, "private async Task<AutomationSnapshot> RefreshSnapshotCoreAsync");
        AssertContains(diagnosticsSnapshotsText, "var shouldAutoVerify = ShouldAutoVerifySnapshot(snapshot);");
        AssertContains(diagnosticsSnapshotsText, "var lastVerification = CaptureLastVerificationForSnapshot(recordingStarted);");
        AssertDoesNotContain(diagnosticsSnapshotsText, "_lastVerification = null;");
        AssertContains(diagnosticsSnapshotsText, "ScheduleAutoVerificationIfNeeded(shouldAutoVerify);");
        AssertDoesNotContain(diagnosticsSnapshotsText, "Automatic recording verification started.");
        AssertDoesNotContain(diagnosticsSnapshotsText, "new FileInfo(lastOutputPath).Length");
        AssertDoesNotContain(diagnosticsSnapshotsText, "GC.GetGCMemoryInfo()");
        AssertDoesNotContain(diagnosticsHubText, "private double CalculateProcessCpuPercent(double processCpuTotalMs)");
        AssertContains(diagnosticsText, "private readonly SemaphoreSlim _refreshGate = new(1, 1);");
        AssertContains(diagnosticsText, "await _refreshGate.WaitAsync(cancellationToken).ConfigureAwait(false);");
        AssertContains(diagnosticsText, "return await RefreshSnapshotCoreAsync(cancellationToken).ConfigureAwait(false);");
        AssertContains(dispatcherText, "case AutomationCommandKind.GetSnapshot:\n                {\n                    var snapshot = await _diagnosticsHub.RefreshSnapshotNowAsync(cancellationToken).ConfigureAwait(false);");
        AssertContains(dispatcherText, "var snapshot = await _diagnosticsHub.RefreshSnapshotNowAsync(cancellationToken).ConfigureAwait(false);\n                    var assertions = ParseAssertions(payload);");
        AssertContains(dispatcherText, "private async Task<(bool Met, AutomationSnapshot Snapshot)> WaitForConditionAsync");
        AssertContains(dispatcherText, "return (true, snapshot);");
        AssertContains(dispatcherText, "snapshot: snapshot");
        AssertContains(dispatcherText, "AutomationSnapshot? snapshot = null");
        AssertContains(dispatcherText, "Snapshot = includeSnapshot ? snapshot ?? _diagnosticsHub.GetLatestSnapshot() : null");
        AssertContains(diagnosticsText, "\"flashback-export-stalled\"");
        AssertContains(diagnosticsText, "DiagnosticsCategory.Flashback");
        AssertContains(diagnosticsText, "health.FlashbackExportActive");
        AssertContains(diagnosticsText, "Math.Max(0, snapshot.FlashbackExportLastProgressAgeMs)");
        AssertContains(diagnosticsText, "Math.Max(0, health.FlashbackExportLastProgressAgeMs)");
        AssertContains(diagnosticsText, "elapsedMs={health.FlashbackExportElapsedMs}");
        AssertContains(diagnosticsText, "throughputBps={health.FlashbackExportThroughputBytesPerSec:0.##}");
        AssertContains(diagnosticsText, "kind={exportFailureKind}");
        AssertContains(diagnosticsText, "private const int FlashbackExportStallThresholdMs = 30000;");
        AssertContains(diagnosticsText, "exportLastProgressAgeMs >= FlashbackExportStallThresholdMs");
        AssertContains(diagnosticsText, "\"Flashback export progress is stalled.\"");
        AssertContains(diagnosticsText, "$\"{exportLane} progressAgeMs={exportLastProgressAgeMs}\"");
        AssertContains(diagnosticsText, "private long _lastFlashbackExportCompletionEventId;");
        AssertContains(diagnosticsText, "ObserveFlashbackExportCompletion(snapshot);");
        AssertContains(diagnosticsText, "private void ObserveFlashbackExportCompletion(AutomationSnapshot snapshot)");
        AssertContains(diagnosticsText, "snapshot.FlashbackExportCompletedUtcUnixMs <= 0");
        AssertContains(diagnosticsText, "Interlocked.CompareExchange(\n                ref _lastFlashbackExportCompletionEventId");
        AssertContains(diagnosticsText, "status.Equals(\"Succeeded\", StringComparison.OrdinalIgnoreCase)");
        AssertContains(diagnosticsText, "status.Equals(\"Cancelled\", StringComparison.OrdinalIgnoreCase)");
        AssertContains(diagnosticsText, "snapshot.FlashbackExportFailureKind");
        AssertContains(diagnosticsText, "FlashbackBackendSettingsStale = health.FlashbackBackendSettingsStale");
        AssertContains(diagnosticsText, "backendStale={health.FlashbackBackendSettingsStale}");
        AssertContains(diagnosticsText, "kind={failureKind}");
        AssertContains(diagnosticsText, "Flashback export completed: status={status}");
        AssertContains(diagnosticsText, "\"flashback-playback-command-stalled\"");
        AssertContains(diagnosticsText, "private const int FlashbackPlaybackCommandStallThresholdMs = 1000;");
        AssertContains(diagnosticsText, "\"flashback-playback-command-failed\"");
        AssertContains(diagnosticsText, "private const int FlashbackPlaybackCommandFailureRecentMs = 30000;");
        AssertContains(diagnosticsText, "playbackCommandFailureAgeMs <= FlashbackPlaybackCommandFailureRecentMs");
        AssertContains(diagnosticsText, "Flashback playback command failed recently:");
        AssertContains(diagnosticsText, "\"Flashback playback command failed recently.\"");
        AssertContains(diagnosticsText, "private const double FlashbackPlaybackSlowFpsRatio = 0.75;");
        AssertContains(diagnosticsText, "private const double CaptureOnePercentLowWarningRatio = 0.98;");
        AssertContains(diagnosticsText, "private const double PreviewOnePercentLowWarningRatio = 0.98;");
        AssertContains(diagnosticsText, "private const double FlashbackPlaybackOnePercentLowWarningRatio = 0.98;");
        AssertContains(diagnosticsText, "private const int FlashbackPlaybackOnePercentLowMinimumFrames = 1200;");
        AssertContains(diagnosticsText, "private const long FlashbackTempDriveLowFreeBytes = 5L * 1024L * 1024L * 1024L;");
        AssertContains(diagnosticsText, "private const long FlashbackRecordingBackpressureWarningMs = 100;");
        AssertContains(diagnosticsText, "private const double FlashbackRecordingQueueDepthWarningRatio = 0.75;");
        AssertContains(diagnosticsText, "private const double FlashbackAudioQueueDepthWarningRatio = 0.90;");
        AssertContains(diagnosticsText, "private const long FlashbackRecordingQueueAgeWarningMs = 500;");
        AssertContains(diagnosticsText, "\"flashback-temp-cache-pressure\"");
        AssertContains(diagnosticsText, "snapshot.FlashbackStartupCacheOverBudget");
        AssertContains(diagnosticsText, "snapshot.FlashbackTempDriveFreeBytes < FlashbackTempDriveLowFreeBytes");
        AssertContains(diagnosticsText, "\"flashback_storage\"");
        AssertContains(diagnosticsText, "\"Flashback temp storage is under pressure.\"");
        AssertContains(diagnosticsText, "\"flashback-encoding-failed\"");
        AssertContains(diagnosticsText, "snapshot.FlashbackEncodingFailed");
        AssertContains(diagnosticsText, "Flashback encoder failed: type={snapshot.FlashbackEncodingFailureType ?? \"Unknown\"}");
        AssertContains(diagnosticsText, "\"flashback-recording-degraded\"");
        AssertContains(countersText, "private FlashbackRecordingRecentCounters UpdateFlashbackRecordingRecentCounters(");
        AssertContains(countersText, "Interlocked.Exchange(ref _lastFlashbackVideoSequenceGaps, sequenceGaps)");
        AssertContains(countersText, "Interlocked.Exchange(ref _lastFlashbackGpuFramesDropped, gpuFramesDropped)");
        AssertContains(countersText, "Interlocked.Exchange(ref _lastFlashbackVideoBackpressureEvents, backpressureEvents)");
        AssertContains(diagnosticsText, "var recentFlashbackRecording = UpdateFlashbackRecordingRecentCounters(health, nowTick);");
        AssertContains(diagnosticsText, "UpdateAlerts(snapshot, recentFlashbackRecording);");
        AssertContains(diagnosticsText, "private void UpdateAlerts(AutomationSnapshot snapshot, FlashbackRecordingRecentCounters flashbackRecordingRecent)");
        AssertContains(diagnosticsText, "var flashbackRecordingQueueBacklog =");
        AssertContains(diagnosticsText, "var flashbackAudioQueueBacklog =");
        AssertContains(diagnosticsText, "IsFlashbackRecordingQueueBackedUp(");
        AssertContains(diagnosticsText, "IsFlashbackAudioQueueBackedUp(");
        AssertContains(diagnosticsText, "flashbackRecordingRecentForceRotateGap");
        AssertContains(diagnosticsText, "IsFlashbackForceRotateRejectReason(snapshot.FlashbackVideoQueueLastRejectReason)");
        AssertContains(diagnosticsText, "flashbackRecordingRecent.SequenceGaps > 0");
        AssertContains(diagnosticsText, "(flashbackRecordingRecent.SequenceGaps > 0 && !flashbackRecordingRecentForceRotateGap)");
        AssertContains(diagnosticsText, "flashbackRecordingRecent.GpuFramesDropped > 0");
        AssertContains(diagnosticsText, "flashbackRecordingRecentBackpressure");
        AssertContains(diagnosticsText, "flashbackRecordingQueueBacklog");
        AssertContains(diagnosticsText, "flashbackAudioQueueBacklog");
        AssertContains(diagnosticsText, "snapshot.FlashbackVideoBackpressureLastWaitMs >= FlashbackRecordingBackpressureWarningMs");
        AssertContains(diagnosticsText, "Flashback recording path degraded:");
        AssertContains(diagnosticsText, "\"flashback-export-rotation-gap\"");
        AssertContains(diagnosticsText, "Flashback export rotation skipped live-edge frames:");
        AssertContains(diagnosticsText, "forceRotate={snapshot.FlashbackForceRotateActive}");
        AssertContains(diagnosticsText, "requested={snapshot.FlashbackForceRotateRequested} draining={snapshot.FlashbackForceRotateDraining}");
        AssertContains(diagnosticsText, "FatalCleanupInProgress = health.FatalCleanupInProgress");
        AssertContains(diagnosticsText, "FlashbackCleanupInProgress = health.FlashbackCleanupInProgress");
        AssertContains(diagnosticsText, "recentBackpressureEvents={flashbackRecordingRecent.BackpressureEvents}");
        AssertContains(diagnosticsText, "\"flashback-playback-slow\"");
        AssertContains(diagnosticsText, "\"flashback-playback-target-below-selection\"");
        AssertContains(diagnosticsText, "\"flashback-playback-present-capped\"");
        AssertContains(diagnosticsText, "\"flashback-playback-frametime-degraded\"");
        AssertContains(diagnosticsText, "\"flashback-playback-audio-master-fallback\"");
        AssertContains(diagnosticsText, "\"flashback-playback-audio-queue-backlog\"");
        AssertContains(diagnosticsText, "\"flashback-playback-submit-failures\"");
        AssertContains(diagnosticsText, "snapshot.FlashbackPlaybackSubmitFailures > 0");
        AssertContains(diagnosticsText, "Flashback playback frame submission failed");
        AssertContains(diagnosticsText, "snapshot.FlashbackPlaybackPendingCommands > 0");
        AssertContains(diagnosticsText, "FlashbackPlaybackCommandQueueCapacity");
        AssertContains(diagnosticsText, "FlashbackPlaybackTargetFps = health.FlashbackPlaybackTargetFps");
        AssertContains(diagnosticsText, "FlashbackPlaybackTargetFps = snapshot.FlashbackPlaybackTargetFps");
        AssertContains(diagnosticsText, "FlashbackPlaybackPtsCadenceMismatchCount = health.FlashbackPlaybackPtsCadenceMismatchCount");
        AssertContains(diagnosticsText, "ptsMismatch={snapshot.FlashbackPlaybackPtsCadenceMismatchCount}");
        AssertContains(diagnosticsText, "private static double ResolveFlashbackPlaybackTargetFps(double flashbackPlaybackTargetFps, double fallbackFrameRate)");
        AssertContains(diagnosticsText, "var playbackTargetFps = ResolveFlashbackPlaybackTargetFps(\n            snapshot.FlashbackPlaybackTargetFps,\n            snapshot.SelectedExactFrameRate.GetValueOrDefault(snapshot.SelectedFrameRate));");
        AssertContains(diagnosticsText, "snapshot.FlashbackPlaybackObservedFps < playbackTargetFps * FlashbackPlaybackSlowFpsRatio");
        AssertContains(diagnosticsText, "snapshot.FlashbackPlaybackTargetFps <= selectedCaptureFps * FlashbackPlaybackSlowFpsRatio");
        AssertContains(diagnosticsText, "snapshot.PreviewCadenceObservedFps <= snapshot.FlashbackPlaybackTargetFps * FlashbackPlaybackSlowFpsRatio");
        AssertContains(diagnosticsText, "IsFlashbackPlaybackFrametimeDegraded(\n                snapshot.FlashbackPlaybackState");
        AssertContains(diagnosticsText, "snapshot.FlashbackPlaybackState,\n                playbackTargetFps,\n                snapshot.FlashbackPlaybackFrameCount");
        AssertContains(diagnosticsText, "IsCaptureOnePercentLowDegraded(\n                snapshot.ExpectedCaptureFrameRate");
        AssertContains(diagnosticsText, "IsPreviewOnePercentLowDegraded(\n                snapshot.PreviewCadenceExpectedIntervalMs");
        AssertContains(diagnosticsText, "\"Source/capture 1% low is below target, but sampled visual cadence confirms source-rate output.\"");
        AssertContains(diagnosticsText, "$\"{sourceLane} | {visualLane}\"");
        AssertContains(diagnosticsText, "captureCadenceExpectedFrameRate: health.ExpectedFrameRate");
        AssertContains(diagnosticsText, "captureCadenceOnePercentLowFps: health.CaptureCadenceOnePercentLowFps");
        AssertContains(diagnosticsText, "previewCadenceExpectedIntervalMs: previewRuntime.DisplayCadenceExpectedIntervalMs");
        AssertContains(diagnosticsText, "previewCadenceOnePercentLowFps: previewRuntime.DisplayCadenceOnePercentLowFps");
        AssertContains(diagnosticsText, "reasons.Add($\"capture 1% low {captureCadenceOnePercentLowFps:0.##}fps\")");
        AssertContains(diagnosticsText, "reasons.Add($\"preview 1% low {previewCadenceOnePercentLowFps:0.##}fps\")");
        AssertContains(diagnosticsText, "private static bool IsFlashbackRecordingQueueBackedUp(");
        AssertContains(diagnosticsText, "queueDepth >= Math.Ceiling(queueCapacity * FlashbackRecordingQueueDepthWarningRatio)");
        AssertContains(diagnosticsText, "oldestFrameAgeMs >= FlashbackRecordingQueueAgeWarningMs");
        AssertContains(diagnosticsText, "private static bool IsFlashbackAudioQueueBackedUp(int queueDepth, int queueCapacity)");
        AssertContains(diagnosticsText, "queueDepth >= Math.Ceiling(queueCapacity * FlashbackAudioQueueDepthWarningRatio)");
        AssertContains(diagnosticsText, "private static bool IsFlashbackForceRotateRejectReason(string? reason)");
        AssertContains(diagnosticsText, "string.Equals(reason, \"force_rotate_queue_guard\"");
        AssertContains(diagnosticsText, "snapshot.FlashbackPlaybackOnePercentLowFps");
        AssertContains(diagnosticsText, "frameCount >= FlashbackPlaybackOnePercentLowMinimumFrames");
        AssertContains(diagnosticsText, "cadenceSampleCount >= FlashbackPlaybackOnePercentLowMinimumFrames");
        AssertContains(diagnosticsText, "snapshot.FlashbackPlaybackAudioMasterFallbacks >= snapshot.FlashbackPlaybackFrameCount * FlashbackPlaybackAudioMasterFallbackWarningRatio");
        AssertContains(diagnosticsText, "snapshot.WasapiPlaybackQueueDepth >= FlashbackPlaybackAudioQueueBacklogWarningDepth");
        AssertContains(diagnosticsText, "Flashback playback is using wall-clock pacing instead of audio-master pacing");
        AssertContains(diagnosticsText, "Flashback playback audio queue is backing up");
        AssertContains(diagnosticsText, "Flashback playback is below target rate");
        AssertContains(diagnosticsText, "Flashback playback target is below the selected capture rate");
        AssertContains(diagnosticsText, "Flashback playback is targeting HFR but D3D present cadence is below target");
        AssertContains(diagnosticsText, "Flashback playback frametime degraded");
        AssertContains(diagnosticsText, "snapshot.FlashbackPlaybackLastCommandQueuedUtcUnixMs > snapshot.FlashbackPlaybackLastCommandProcessedUtcUnixMs");
        AssertContains(diagnosticsText, "snapshot.FlashbackPlaybackLastCommandFailureUtcUnixMs > 0");
        AssertContains(diagnosticsText, "Flashback playback command queue has not drained");
        AssertContains(diagnosticsText, "var playbackCommandFailure = string.IsNullOrWhiteSpace(snapshot.FlashbackPlaybackLastCommandFailure)");
        AssertContains(diagnosticsText, "lastFailure={playbackCommandFailure} failureAgeMs={playbackCommandFailureAgeMs}");
        AssertContains(diagnosticsText, "\"flashback_playback\"");
        AssertContains(diagnosticsText, "\"Flashback playback command queue is stalled.\"");
        AssertContains(diagnosticsText, "\"Flashback playback is below target rate.\"");
        AssertContains(diagnosticsText, "\"Flashback playback frametime is below target.\"");
        AssertContains(diagnosticsText, "\"Flashback playback frame submission failed.\"");
        AssertContains(diagnosticsText, "flashback recording active={health.FlashbackActive}");
        AssertContains(diagnosticsText, "fatalCleanup={health.FatalCleanupInProgress} flashbackCleanup={health.FlashbackCleanupInProgress}");
        AssertContains(diagnosticsText, "var recordingIntegrityIncomplete =");
        AssertContains(diagnosticsText, "string.Equals(captureRuntime.RecordingIntegrityStatus, \"Incomplete\", StringComparison.OrdinalIgnoreCase)");
        AssertContains(diagnosticsText, "(recordingIntegrityIncomplete && !isRecording)");
        AssertContains(diagnosticsText, "var flashbackRecordingDegraded =");
        AssertContains(diagnosticsText, "recentFlashbackRecording.EncoderDroppedFrames > 0");
        AssertContains(diagnosticsText, "recentFlashbackRecording.BackpressureEvents > 0");
        AssertContains(diagnosticsText, "health.FlashbackVideoBackpressureLastWaitMs >= FlashbackRecordingBackpressureWarningMs");
        AssertContains(diagnosticsText, "var flashbackBackendSettingsUnexpectedlyStale =");
        AssertContains(diagnosticsText, "health.FlashbackBackendSettingsStale &&\n            !isRecording");
        AssertContains(diagnosticsText, "\"Flashback backend settings differ from requested settings.\"");
        AssertContains(diagnosticsText, "health.FlashbackVideoQueueDepth,\n                 health.FlashbackVideoQueueCapacity,\n                 health.FlashbackVideoQueueOldestFrameAgeMs");
        AssertContains(diagnosticsText, "forceRotate={health.FlashbackForceRotateActive}");
        AssertContains(diagnosticsText, "queueRejects={health.FlashbackVideoQueueRejectedFrames}");
        AssertContains(diagnosticsText, "audioQueue={health.FlashbackAudioQueueDepth}/{health.FlashbackAudioQueueCapacity}");
        AssertContains(diagnosticsText, "lastReject={health.FlashbackVideoQueueLastRejectReason ?? \"None\"}");
        AssertContains(diagnosticsText, "flashbackExportRotationGap");
        AssertContains(diagnosticsText, "\"Flashback export rotation skipped live-edge frames.\"");
        AssertContains(diagnosticsText, "requested={health.FlashbackForceRotateRequested} draining={health.FlashbackForceRotateDraining}");
        AssertContains(diagnosticsText, "\"flashback_recording\"");
        AssertContains(diagnosticsText, "\"Flashback encoder has failed.\"");
        AssertContains(diagnosticsText, "\"Flashback recording path is dropping or backing up.\"");
        AssertContains(diagnosticsText, "queuedAge={playbackCommandQueueAgeMs}ms");
        AssertContains(diagnosticsText, "var playbackCommandFailure = string.IsNullOrWhiteSpace(health.FlashbackPlaybackLastCommandFailure)");
        AssertContains(diagnosticsText, "var playbackTargetFps = ResolveFlashbackPlaybackTargetFps(\n            health.FlashbackPlaybackTargetFps,\n            health.ExpectedFrameRate);");
        AssertContains(diagnosticsText, "lastFailure={playbackCommandFailure} failureAgeMs={playbackCommandFailureAgeMs}");
        AssertContains(diagnosticsText, "playback perf state={health.FlashbackPlaybackState}");
        AssertContains(diagnosticsText, "fps={health.FlashbackPlaybackObservedFps:0.##}/{playbackTargetFps:0.##}");
        AssertContains(diagnosticsText, "target={health.FlashbackPlaybackTargetFps:0.##}");
        AssertContains(diagnosticsText, "encoder={FormatEncoderFrameRate(health)} source={(health.SourceFrameRateExact ?? 0):0.##} present={previewRuntime.DisplayCadenceObservedFps:0.##}");
        AssertContains(diagnosticsText, "private static string FormatEncoderFrameRate(CaptureHealthSnapshot health)");
        AssertContains(diagnosticsText, "ptsMismatch={health.FlashbackPlaybackPtsCadenceMismatchCount} ptsDelta={health.FlashbackPlaybackLastPtsCadenceDeltaMs:0.##}/{health.FlashbackPlaybackLastPtsCadenceExpectedMs:0.##}ms");
        AssertContains(diagnosticsText, "1pctLow={health.FlashbackPlaybackOnePercentLowFps:0.##}fps");
        AssertContains(diagnosticsText, "private const double FlashbackPlaybackAudioMasterFallbackWarningRatio = 0.50;");
        AssertContains(diagnosticsText, "private const int FlashbackPlaybackAudioQueueBacklogWarningDepth = 24;");
        AssertContains(diagnosticsText, "decodeP99={health.FlashbackPlaybackDecodeP99Ms:0.##}ms");
        AssertContains(diagnosticsText, "decodePhase={health.FlashbackPlaybackMaxDecodePhase}");
        AssertContains(diagnosticsText, "decodeSend={health.FlashbackPlaybackMaxDecodeSendMs:0.##}ms");
        AssertContains(diagnosticsText, "decodeAudio={health.FlashbackPlaybackMaxDecodeAudioMs:0.##}ms");
        AssertContains(diagnosticsText, "decodePhase={snapshot.FlashbackPlaybackMaxDecodePhase}");
        AssertContains(diagnosticsText, "audioMasterDouble={health.FlashbackPlaybackAudioMasterDelayDoubles}");
        AssertContains(diagnosticsText, "audioMasterDouble={snapshot.FlashbackPlaybackAudioMasterDelayDoubles}");
        AssertContains(diagnosticsText, "health.FlashbackPlaybackSubmitFailures > 0");
        AssertContains(diagnosticsText, "\"flashback_export\"");
        AssertContains(diagnosticsText, "var flashbackForceRotateRejectWithoutDamage =");
        AssertContains(diagnosticsText, "!flashbackForceRotateRejectWithoutDamage &&\n              recentFlashbackRecording.SequenceGaps > 0");
        AssertContains(diagnosticsText, "health.FlashbackExportActive ||\n             health.FlashbackForceRotateActive ||\n             health.FlashbackForceRotateRequested ||\n             health.FlashbackForceRotateDraining");
        AssertContains(diagnosticsText, "UpdatePreviewJitterRecentCounters(health, nowTick)");
        AssertContains(diagnosticsText, "UpdateD3DRendererRecentCounters(previewRuntime, nowTick)");
        AssertContains(countersText, "private D3DRendererRecentCounters UpdateD3DRendererRecentCounters(");
        AssertContains(countersText, "Interlocked.Exchange(ref _lastD3DFramesSubmitted, submitted)");
        AssertContains(diagnosticsText, "recentSubmitted={recentRendererSubmitted} recentDropped={recentRenderer.Dropped}");
        AssertContains(diagnosticsText, "var previewLastDropReason = string.IsNullOrWhiteSpace(health.MjpegPreviewJitterLastDropReason)");
        AssertContains(diagnosticsText, "clearedDrops={health.MjpegPreviewJitterClearedDropCount}");
        AssertContains(diagnosticsText, "resumeReprimes={health.MjpegPreviewJitterResumeReprimeCount} recentDeadlineDrops={recentPreviewDeadlineDrops} recentUnderflows={recentPreviewUnderflows} lastDropReason={previewLastDropReason}");
        AssertContains(diagnosticsText, "UpdateD3DFrameStatsRecentCounters(previewRuntime, nowTick)");
        AssertContains(diagnosticsText, "recentMissed={recentD3DMissedRefreshes} recentFail={recentD3DStatsFailures}");
        AssertContains(diagnosticsText, "\"capture-cadence-low-1pct\"");
        AssertContains(diagnosticsText, "\"Capture cadence 1% low is below target:");
        AssertContains(diagnosticsText, "\"preview-display-low-1pct\"");
        AssertContains(diagnosticsText, "previewOnePercentLowDegraded && !visualCadenceHealthy");
        AssertContains(diagnosticsText, "\"Preview/display 1% low is below target:");
        AssertContains(diagnosticsText, "FormatVisualCadenceAlertDetail(snapshot)");
        AssertContains(diagnosticsText, "visualChanges={snapshot.VisualCadenceChangeObservedFps:0.##}fps");
        AssertContains(diagnosticsText, "var previewSubmitFailed = string.Equals(");
        AssertContains(diagnosticsText, "health.MjpegPreviewJitterLastDropReason,\n            \"submit-failed\"");
        AssertContains(diagnosticsText, "if (previewSubmitFailed ||\n            (recentPreviewDeadlineDrops > 0 && !visualCadenceHealthy) ||\n            recentPreviewUnderflows > 3)");
        AssertContains(diagnosticsText, "\"Preview scheduler failed to submit frames.\"");
        AssertContains(diagnosticsText, "var presentCadenceOverBudget =\n            previewRuntime.DisplayCadenceExpectedIntervalMs > 0 &&\n            previewRuntime.DisplayCadenceP95IntervalMs > previewRuntime.DisplayCadenceExpectedIntervalMs * 1.5;");
        AssertContains(diagnosticsText, "var previewSlowFrameDetail = FormatPreviewSlowFrameAlertDetail(snapshot);");
        AssertContains(diagnosticsText, "latestSlowFrameReason={reason} over={frame.WorstOverBudgetMs:0.##}ms");
        AssertContains(diagnosticsText, "pipeline={frame.PipelineLatencyMs:0.##}ms pending={frame.PendingFrameCount}");
        AssertContains(diagnosticsText, "inputUpload={frame.InputUploadCpuMs:0.##}ms");
        AssertContains(diagnosticsText, "renderSubmit={frame.RenderSubmitCpuMs:0.##}ms");
        AssertContains(diagnosticsText, "var unsyncedPresentCallSlow =\n            previewRuntime.D3DPresentSyncInterval == 0 &&\n            previewRuntime.D3DPresentCallP95Ms > 4.0;");
        AssertContains(diagnosticsText, "if (presentCadenceOverBudget ||\n            unsyncedPresentCallSlow)");
        AssertContains(diagnosticsText, "if (captureOnePercentLowDegraded)");
        AssertContains(diagnosticsText, "\"Source/capture 1% low is below target.\"");
        AssertContains(diagnosticsText, "if (previewOnePercentLowDegraded)");
        AssertContains(diagnosticsText, "var visualCadenceHealthy =\n            IsVisualCadenceHealthy(");
        AssertContains(diagnosticsText, "Present/display 1% low is below target, but sampled visual cadence confirms source-rate output.");
        AssertContains(diagnosticsText, "if (visualCadenceHealthy)\n            {\n                return new DiagnosticEvaluation(\n                    \"Healthy\",");
        AssertContains(diagnosticsText, "private static bool IsMjpegDuplicateCadenceDetected(CaptureHealthSnapshot health)");
        AssertContains(diagnosticsText, "health.MjpegPacketHashDuplicateFramePercent < 20.0");
        AssertContains(diagnosticsText, "health.MjpegPacketHashUniqueObservedFps <= health.ExpectedFrameRate * 0.75");
        AssertContains(diagnosticsText, "health.VisualCadenceChangeObservedFps <= health.ExpectedFrameRate * 0.75");
        AssertContains(diagnosticsText, "health.SourceFrameRateExact.Value <= health.ExpectedFrameRate * 0.75");
        AssertContains(diagnosticsText, "var mjpegDuplicateCadenceDetected = IsMjpegDuplicateCadenceDetected(health);");
        AssertContains(diagnosticsText, "\"source_signal\"");
        AssertContains(diagnosticsText, "\"Captured HFR MJPEG cadence contains repeated source frames.\"");
        AssertContains(diagnosticsText, "$\"{mjpegDuplicateLane} | {visualLane} | {sourceSignalLane}\"");
        AssertContains(diagnosticsText, "!visualCadenceHealthy &&\n            IsPreviewOnePercentLowDegraded(");
        AssertContains(diagnosticsText, "private static bool IsVisualCadenceHealthy(");
        AssertContains(diagnosticsText, "changeObservedFps >= targetFrameRate * PreviewOnePercentLowWarningRatio");
        AssertContains(diagnosticsText, "repeatFramePercent <= 1.0");
        AssertContains(diagnosticsText, "longestRepeatRun <= 1");
        AssertContains(diagnosticsText, "\"Present/display 1% low is below target.\"");
        AssertContains(countersText, "private MjpegRecentCounters UpdateMjpegRecentCounters(");
        AssertContains(diagnosticsText, "var recentMjpeg = UpdateMjpegRecentCounters(health, nowTick);");
        AssertContains(diagnosticsText, "recentDropped={recentMjpeg.TotalDropped} recentFailures={recentMjpeg.Failures}");
        AssertContains(diagnosticsText, "recentMjpeg.TotalDropped > 0");
        AssertContains(diagnosticsText, "if (recentRendererSubmitted >= DiagnosticThresholds.RendererDropWarningMinSamples &&\n            recentRendererDropPercent > DiagnosticThresholds.RendererDropWarningPercent)");
        AssertDoesNotContain(diagnosticsText, "rendererDropPercent > DiagnosticThresholds.RendererDropWarningPercent) ||\n            previewRuntime.DisplayCadenceSlowFramePercent > 1.0");

        var captureServiceText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.cs")
            .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.Coordination.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.DeferredCleanup.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.Audio.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackExportOperations.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackExportDiagnostics.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackExportPlanning.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackExportFailureClassification.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackOrchestration.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingFinalizeRecord.cs")
                .Replace("\r\n", "\n");
        var flashbackBackendText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackBackendResources.cs")
            .Replace("\r\n", "\n");
        AssertContains(captureServiceText, "private readonly SemaphoreSlim _flashbackExportOperationLock = new(1, 1);");
        AssertContains(captureServiceText, "await _flashbackExportOperationLock.WaitAsync(ct).ConfigureAwait(false);");
        AssertContains(captureServiceText, "FlashbackExporter? snapshotExporter = null,");
        AssertContains(captureServiceText, "var exporter = snapshotExporter;\n            if (exporter == null)\n            {\n                exporter = _flashbackExporter ??= new FlashbackExporter();\n            }");
        AssertOccursBefore(captureServiceText, "if (bufferManager == null)", "var exporter = snapshotExporter;");
        AssertContains(captureServiceText, "var sessionLockHeld = false;");
        AssertContains(captureServiceText, "sessionLockHeld = true;");
        AssertContains(captureServiceText, "if (sessionLockHeld)");
        AssertContains(captureServiceText, "var exportOperationLockHeld = false;");
        AssertContains(captureServiceText, "exportOperationLockHeld = true;");
        AssertContains(captureServiceText, "catch (OperationCanceledException) when (ct.IsCancellationRequested)");
        AssertContains(captureServiceText, "ReleaseFlashbackBackendLeaseIfHeld(ref backendLeaseHeld);");
        AssertContains(captureServiceText, "private void ReleaseFlashbackBackendLeaseIfHeld(ref bool backendLeaseHeld)");
        AssertContains(captureServiceText, "backendLeaseHeld = false;\n        ReleaseSemaphoreBestEffort(_flashbackBackendLeaseLock, \"flashback_backend_lease\");");
        var exportRangeMethod = ExtractTextBetween(
            captureServiceText,
            "internal async Task<FinalizeResult> ExportFlashbackRangeAsync",
            "internal async Task<FinalizeResult> ExportFlashbackLastNSecondsAsync");
        var exportLastNMethod = ExtractTextBetween(
            captureServiceText,
            "internal async Task<FinalizeResult> ExportFlashbackLastNSecondsAsync",
            "private FinalizeResult FailFlashbackExport");
        AssertContains(exportRangeMethod, "FlashbackExporter? flashbackExporter;");
        AssertContains(exportRangeMethod, "flashbackExporter = bufferManager != null\n                ? _flashbackExporter ??= new FlashbackExporter()\n                : _flashbackExporter;");
        AssertContains(exportRangeMethod, "ReleaseFlashbackBackendLeaseIfHeld(ref backendLeaseHeld);\n            if (sessionLockHeld)");
        AssertOccursBefore(exportRangeMethod, "ReleaseFlashbackBackendLeaseIfHeld(ref backendLeaseHeld);", "return await ExportFlashbackCoreAsync(");
        AssertContains(exportRangeMethod, "snapshotExporter: flashbackExporter,");
        AssertContains(exportLastNMethod, "FlashbackExporter? flashbackExporter;");
        AssertContains(exportLastNMethod, "flashbackExporter = bufferManager != null\n                ? _flashbackExporter ??= new FlashbackExporter()\n                : _flashbackExporter;");
        AssertContains(exportLastNMethod, "ReleaseFlashbackBackendLeaseIfHeld(ref backendLeaseHeld);\n            if (sessionLockHeld)");
        AssertOccursBefore(exportLastNMethod, "ReleaseFlashbackBackendLeaseIfHeld(ref backendLeaseHeld);", "return await ExportFlashbackCoreAsync(");
        AssertContains(exportLastNMethod, "snapshotExporter: flashbackExporter,");
        AssertContains(flashbackBackendText, "outerPauseApplied = bufferManager != null;");
        AssertContains(captureServiceText, "return FailFlashbackExport(outputPath, \"Flashback export cancelled.\", inPoint, outPoint);");
        AssertContains(captureServiceText, "var exportId = 0L;");
        AssertContains(captureServiceText, "var evictionPaused = false;");
        AssertContains(captureServiceText, "exportId = BeginFlashbackExportDiagnostics(inPoint, outPoint, outputPath);");
        AssertContains(captureServiceText, "var forceRotateResult = flashbackSink.ForceRotateForExport(inPoint, outPoint, ct);");
        AssertContains(captureServiceText, "segmentPaths = forceRotateResult.SegmentPaths;");
        AssertContains(captureServiceText, "if (forceRotateResult.Status == FlashbackForceRotateStatus.Failed)");
        AssertContains(captureServiceText, "if (forceRotateResult.Status == FlashbackForceRotateStatus.CommittedPending)");
        var forceRotateFailedBlock = ExtractTextBetween(
            captureServiceText,
            "if (forceRotateResult.Status == FlashbackForceRotateStatus.Failed)",
            "if (forceRotateResult.Status == FlashbackForceRotateStatus.CommittedPending)");
        AssertContains(forceRotateFailedBlock, "Flashback export failed: live-edge segment rotation failed.");
        AssertContains(forceRotateFailedBlock, "preserved_segments={preservedArtifacts.Count}");
        AssertContains(forceRotateFailedBlock, "return result;");
        var forceRotateFallbackBlock = ExtractTextBetween(
            captureServiceText,
            "if (segmentPaths.Count == 0)",
            "// Fallback: single-file export if no segments available");
        AssertContains(forceRotateFallbackBlock, "FLASHBACK_EXPORT_FORCE_ROTATE_FALLBACK reason=force_rotate_timeout");
        AssertContains(forceRotateFallbackBlock, "RecordFlashbackExportForceRotateFallback(exportId, segmentPaths.Count, inPoint, outPoint);");
        AssertDoesNotContain(forceRotateFallbackBlock, "force_rotate_failed");
        AssertDoesNotContain(forceRotateFallbackBlock, "Flashback export failed: live-edge segment rotation failed.");
        AssertContains(captureServiceText, "private sealed class FlashbackRecordingBoundarySnapshot");
        AssertContains(captureServiceText, "captureBoundarySnapshot: sink => CaptureFlashbackRecordingBoundarySnapshot(sink, recordingBoundary)");
        AssertContains(flashbackBackendText, "captureBoundarySnapshot?.Invoke(flashbackSink);");
        AssertOccursBefore(flashbackBackendText, "captureBoundarySnapshot?.Invoke(flashbackSink);", "var exportResult = await exportRecordingAsync(");
        AssertContains(captureServiceText, "counters: recordingBoundary.Counters ?? CaptureFlashbackRecordingIntegrityCountersSinceBaseline");
        AssertContains(captureServiceText, "audioCounters: recordingBoundary.AudioCounters ?? GetRecordingAudioCountersSinceBaseline");
        AssertContains(captureServiceText, "evictionPaused = true;");
        AssertContains(captureServiceText, "if (exportId != 0)");
        AssertContains(captureServiceText, "if (evictionPaused)");
        AssertContains(captureServiceText, "ResumeFlashbackEvictionBestEffort(bufferManager, \"flashback_export\");");
        AssertContains(flashbackBackendText, "resumeEvictionBestEffort(bufferManager, \"flashback_recording_finalize\");");
        AssertContains(captureServiceText, "RecordLastFlashbackExportResult(exportId, failure);");
        AssertContains(captureServiceText, "private void RecordLastFlashbackExportResult(long exportId, FinalizeResult result)");
        AssertContains(captureServiceText, "Volatile.Write(ref _lastFlashbackExportResultId, exportId);");
        AssertContains(captureServiceText, "private FinalizeResult FailFlashbackExport(\n        string outputPath,\n        string statusMessage,\n        TimeSpan? inPoint = null,\n        TimeSpan? outPoint = null)");
        AssertContains(captureServiceText, "Logger.Log($\"FLASHBACK_EXPORT_REJECTED status='{statusMessage}' output='{outputPath}'\");");
        AssertContains(captureServiceText, "_lastExportResult = result;");
        AssertContains(captureServiceText, "RecordRejectedFlashbackExportDiagnostics(outputPath, result, inPoint, outPoint);");
        AssertContains(captureServiceText, "private void RecordRejectedFlashbackExportDiagnostics(\n        string outputPath,\n        FinalizeResult result,\n        TimeSpan? inPoint = null,\n        TimeSpan? outPoint = null)");
        AssertContains(captureServiceText, "if (_flashbackExportActive)");
        AssertContains(captureServiceText, "Volatile.Write(ref _lastFlashbackExportResultId, 0);");
        AssertContains(captureServiceText, "FLASHBACK_EXPORT_REJECTED_DIAGNOSTICS_DEFERRED");
        AssertContains(captureServiceText, "active_id={_flashbackExportId}");
        AssertContains(captureServiceText, "if (_flashbackExportId != exportId || !_flashbackExportActive)");
        AssertContains(captureServiceText, "var statusMessage = ex is OperationCanceledException && ct.IsCancellationRequested\n                ? \"Flashback export cancelled.\"\n                : ex.Message;");
        AssertContains(captureServiceText, "FLASHBACK_EXPORT_CORE_FAIL id={exportId} type={ex.GetType().Name}");
        AssertContains(captureServiceText, "var failure = FinalizeResult.Failure(outputPath, statusMessage);");
        AssertContains(captureServiceText, "CompleteFlashbackExportDiagnostics(exportId, failure);\n            }\n            else\n            {\n                RecordRejectedFlashbackExportDiagnostics(outputPath, failure, inPoint, outPoint);\n            }\n            return failure;");
        AssertContains(captureServiceText, "_flashbackExportStartedUtcUnixMs = now;");
        AssertContains(captureServiceText, "_flashbackExportCompletedUtcUnixMs = now;");
        AssertContains(captureServiceText, "var completedUtcUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();");
        AssertContains(captureServiceText, "_flashbackExportCompletedUtcUnixMs = completedUtcUnixMs;");
        AssertContains(captureServiceText, "_flashbackExportLastProgressUtcUnixMs = completedUtcUnixMs;");
        AssertContains(captureServiceText, "ClassifyFlashbackExportFailureKind(result.StatusMessage)");
        AssertContains(captureServiceText, "internal static string ClassifyFlashbackExportFailureKind(string? statusMessage)");
        AssertContains(captureServiceText, "return \"UnavailableDuringRecording\";");
        AssertContains(captureServiceText, "return \"BufferInactive\";");
        AssertContains(captureServiceText, "ContainsFlashbackExportFailureText(statusMessage, \"buffer has no active file\")");
        AssertContains(captureServiceText, "return \"InvalidOutputPath\";");
        AssertContains(captureServiceText, "return \"NoMediaWritten\";");
        AssertContains(captureServiceText, "return FailFlashbackExport(outputPath, \"Flashback buffer not active\", inPoint, outPoint);");
        AssertContains(captureServiceText, "resolveRangeAfterEvictionPaused: manager =>");
        AssertContains(captureServiceText, "var validStart = manager.ValidStartPts;");
        AssertContains(captureServiceText, "var bufferedDuration = manager.BufferedDuration;");
        AssertContains(captureServiceText, "var bufferInPoint = ClampFlashbackBufferPosition(inPoint ?? TimeSpan.Zero, bufferedDuration);");
        AssertContains(captureServiceText, "var bufferOutPoint = outPoint.HasValue\n                        ? ClampFlashbackBufferPosition(outPoint.Value, bufferedDuration)\n                        : TimeSpan.MaxValue;");
        AssertContains(captureServiceText, "var fileInPoint = AddFlashbackPtsOffsetOrMax(bufferInPoint, validStart);");
        AssertContains(captureServiceText, "var fileOutPoint = AddFlashbackPtsOffsetOrMax(bufferOutPoint, validStart);");
        AssertContains(captureServiceText, ".Select(segment => (Key: TryGetFullPath(segment.Path), Segment: segment))");
        AssertContains(captureServiceText, "var pathKey = TryGetFullPath(path);");
        AssertContains(captureServiceText, "segmentInfo.TryGetValue(pathKey, out var info)");
        AssertContains(captureServiceText, "private static string? TryGetFullPath(string? path)");
        AssertContains(captureServiceText, "FLASHBACK_PATH_NORMALIZE_WARN");
        AssertContains(captureServiceText, "fileOutPoint != TimeSpan.MaxValue && fileOutPoint <= fileInPoint");
        AssertContains(captureServiceText, "resolvedRange.FailureMessage ?? \"Flashback export range is empty or invalid.\"");
        AssertContains(captureServiceText, "if (ct.IsCancellationRequested)\n        {\n            return FailFlashbackExport(outputPath, \"Flashback export cancelled.\");\n        }\n\n        if (!double.IsFinite(seconds) || seconds <= 0 || seconds > TimeSpan.MaxValue.TotalSeconds)\n        {\n            return FailFlashbackExport(outputPath, \"Flashback export duration must be finite, greater than zero, and within TimeSpan range.\");\n        }");
        AssertContains(dispatcherText, "if (!double.IsFinite(seconds) ||\n                        seconds <= 0 ||\n                        seconds > TimeSpan.MaxValue.TotalSeconds)");
        AssertContains(dispatcherText, "Flashback export seconds must be finite, greater than zero, and within TimeSpan range.");
        AssertContains(captureServiceText, "? \"Cancelled\"");
        AssertContains(captureServiceText, "private static bool IsFlashbackExportCancelled(string? statusMessage)");
        AssertContains(captureServiceText, "if (exportOperationLockHeld)");
        AssertContains(captureServiceText, "ReleaseSemaphoreBestEffort(_flashbackExportOperationLock, \"flashback_export_operation\");");
        AssertContains(captureServiceText, "DisposeCoordinationLocksBestEffort();");
        AssertContains(captureServiceText, "DisposeSemaphoreBestEffort(_sessionTransitionLock, \"session_transition\");");
        AssertContains(captureServiceText, "DisposeSemaphoreBestEffort(_flashbackBackendLeaseLock, \"flashback_backend_lease\");");
        AssertContains(captureServiceText, "DisposeSemaphoreBestEffort(_flashbackExportOperationLock, \"flashback_export_operation\");");
        AssertContains(captureServiceText, "CAPTURE_SERVICE_SEMAPHORE_DISPOSE_WARN");
        AssertContains(captureServiceText, "private static void ReleaseSemaphoreBestEffort(SemaphoreSlim semaphore, string operation)");
        AssertContains(captureServiceText, "CAPTURE_SERVICE_SEMAPHORE_RELEASE_WARN");
        AssertContains(captureServiceText, "private static void ResumeFlashbackEvictionBestEffort(FlashbackBufferManager? bufferManager, string operation)");
        AssertContains(captureServiceText, "FLASHBACK_EVICTION_RESUME_WARN");
        AssertContains(captureServiceText, "ReleaseSemaphoreBestEffort(_sessionTransitionLock, \"flashback_export_snapshot_session\");");
        AssertContains(captureServiceText, "ReleaseSemaphoreBestEffort(_flashbackBackendLeaseLock, \"flashback_preview_backend_dispose\");");
        AssertDoesNotContain(captureServiceText, "_flashbackBackendLeaseLock.Release();");
        AssertDoesNotContain(captureServiceText, "_flashbackExportOperationLock.Release();");
        AssertContains(captureServiceText, "FLASHBACK_EXPORT_ACTIVE_FILE_FALLBACK");
        AssertContains(captureServiceText, "Segments = BuildFlashbackExportSegments(bufferManager, segmentPaths)");
        AssertContains(captureServiceText, "var startPts = FromSegmentMilliseconds(info.StartPtsMs);");
        AssertContains(captureServiceText, "var endPts = FromSegmentMilliseconds(info.EndPtsMs);");
        AssertContains(captureServiceText, "if (endPts < startPts)\n                {\n                    endPts = startPts;\n                }");
        AssertContains(captureServiceText, "StartPts = startPts,\n                    EndPts = endPts");
        AssertContains(captureServiceText, "private static TimeSpan FromSegmentMilliseconds(long milliseconds)");
        AssertContains(captureServiceText, "return milliseconds >= TimeSpan.MaxValue.TotalMilliseconds\n            ? TimeSpan.MaxValue\n            : TimeSpan.FromMilliseconds(milliseconds);");
        AssertContains(captureServiceText, "private static TimeSpan ClampFlashbackBufferPosition(TimeSpan position, TimeSpan bufferedDuration)");
        AssertContains(captureServiceText, "if (bufferedDuration <= TimeSpan.Zero)\n        {\n            return TimeSpan.Zero;\n        }");
        AssertContains(captureServiceText, "private static TimeSpan AddFlashbackPtsOffsetOrMax(TimeSpan position, TimeSpan offset)");
        AssertContains(captureServiceText, "if (position < TimeSpan.Zero)\n        {\n            position = TimeSpan.Zero;\n        }");
        AssertContains(captureServiceText, "if (offset <= TimeSpan.Zero)\n        {\n            return position;\n        }");
        AssertContains(captureServiceText, "return position > TimeSpan.MaxValue - offset\n            ? TimeSpan.MaxValue\n            : position + offset;");
        AssertContains(captureServiceText, "var rawTotalSegments = progress.TotalSegments;");
        AssertContains(captureServiceText, "var totalSegments = Math.Max(0, rawTotalSegments);");
        AssertContains(captureServiceText, "if (totalSegments > 0 && segmentsProcessed > totalSegments)");
        AssertContains(captureServiceText, "Math.Clamp(rawPercent, 0.0, 100.0)");
        AssertContains(captureServiceText, "FLASHBACK_EXPORT_PROGRESS_NORMALIZED");
        AssertContains(captureServiceText, "raw_segments={rawSegmentsProcessed}/{rawTotalSegments}");
        AssertContains(captureServiceText, "raw_percent={rawPercent:0.###} percent={percent:0.###}");
        AssertContains(captureServiceText, "try\n            {\n                innerProgress?.Report(progress);\n            }\n            catch (Exception ex)\n            {\n                Logger.Log($\"FLASHBACK_EXPORT_PROGRESS_FORWARD_WARN id={exportId} type={ex.GetType().Name} msg='{ex.Message}'\");\n            }");

        var flashbackExporterText = ReadFlashbackExporterSource();
        AssertContains(flashbackExporterText, "if (request.Segments is { Count: > 0 })");
        AssertContains(flashbackExporterText, "var useSegmentTimeline = segment.StartPts.HasValue");
        AssertContains(flashbackExporterText, "var comparePtsUs = useSegmentTimeline");
        AssertContains(flashbackExporterText, "ResolveSegmentBoundaryTimestampRepairUs(");
        AssertContains(flashbackExporterText, "FLASHBACK_EXPORT_SEGMENT_PTS_REPAIR");

        var sourceReaderRootText = ReadRepoFile("Sussudio/Services/Capture/MfSourceReaderVideoCapture.cs")
            .Replace("\r\n", "\n");
        var sourceReaderDiagnosticsText = ReadRepoFile("Sussudio/Services/Capture/MfSourceReaderVideoCapture.Diagnostics.cs")
            .Replace("\r\n", "\n");
        var sourceReaderText = sourceReaderRootText
            + "\n" + sourceReaderDiagnosticsText
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/MfSourceReaderVideoCapture.Cadence.cs")
                .Replace("\r\n", "\n");
        AssertContains(sourceReaderText, "Keep source cadence state coherent with diagnostics snapshots");
        AssertContains(sourceReaderText, "lock (_cadenceLock)");
        AssertContains(sourceReaderDiagnosticsText, "private unsafe void DiagnoseVtable(IMFSample sample)");
        AssertContains(sourceReaderDiagnosticsText, "VTABLE_DIAG RAW slot35_GetSampleTime");
        AssertDoesNotContain(sourceReaderRootText, "private unsafe void DiagnoseVtable(IMFSample sample)");

        var diagnosticSessionText = ReadRepoFile("tools/Common/DiagnosticSessionRunner.cs")
            .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("tools/Common/DiagnosticSessionBackgroundTasks.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("tools/Common/DiagnosticSessionRunState.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("tools/Common/DiagnosticSessionCleanupPolicy.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("tools/Common/DiagnosticSessionFlashbackCycleScenarios.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("tools/Common/DiagnosticSessionFlashbackExports.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("tools/Common/DiagnosticSessionFlashbackExportScenarios.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("tools/Common/DiagnosticSessionFlashbackLifecycleScenarios.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("tools/Common/DiagnosticSessionFlashbackMetrics.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("tools/Common/DiagnosticSessionFlashbackPreviewCycleScenarios.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("tools/Common/DiagnosticSessionFlashbackRejectedExports.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("tools/Common/DiagnosticSessionFlashbackRecordingSettingsScenarios.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("tools/Common/DiagnosticSessionFlashbackSegmentPlaybackScenarios.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("tools/Common/DiagnosticSessionFlashbackSegments.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("tools/Common/DiagnosticSessionFlashbackStressScenario.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("tools/Common/DiagnosticSessionFlashbackValidation.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("tools/Common/DiagnosticSessionFlashbackWaits.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("tools/Common/DiagnosticSessionHealthPolicy.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("tools/Common/DiagnosticSessionJsonArtifacts.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("tools/Common/DiagnosticSessionMetrics.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("tools/Common/DiagnosticSessionPipeRetryPolicy.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("tools/Common/DiagnosticSessionResultFormatter.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("tools/Common/DiagnosticSessionSampler.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("tools/Common/DiagnosticSessionScenarioPlan.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("tools/Common/DiagnosticSessionText.cs")
                .Replace("\r\n", "\n");
        var diagnosticSessionModelsText = ReadRepoFile("tools/Common/DiagnosticSessionModels.cs")
            .Replace("\r\n", "\n");
        var diagnosticScenariosText = ReadRepoFile("tools/Common/DiagnosticSessionScenarios.cs")
            .Replace("\r\n", "\n");
        AssertContains(diagnosticSessionText, "var scenario = DiagnosticSessionScenarios.Normalize(options.Scenario);");
        AssertContains(diagnosticSessionText, "var scenarioPlan = DiagnosticSessionScenarioPlan.From(scenario);");
        AssertContains(diagnosticSessionText, "var backgroundTasks = new DiagnosticSessionBackgroundTasks();");
        AssertContains(diagnosticSessionText, "DiagnosticSessionScenarios.NeedsFlashback(scenario)");
        AssertContains(diagnosticSessionText, "DiagnosticSessionScenarios.NeedsPreview(scenario)");
        AssertContains(diagnosticSessionText, "DiagnosticSessionScenarios.NeedsRecording(scenario)");
        AssertContains(diagnosticSessionText, "scenarioPlan.RequiresFlashbackRecordingReadiness");
        AssertContains(diagnosticSessionText, "scenarioPlan.UsesFlashbackScenarioWarningPolicy");
        AssertContains(diagnosticSessionText, "scenarioPlan.ToleratesSourceSignalHealthWarning");
        AssertContains(diagnosticSessionText, "scenarioPlan.ToleratesFlashbackForceRotateDrainWarning");
        AssertContains(diagnosticSessionText, "scenarioPlan.IsPreviewCycleScenario");
        AssertContains(diagnosticSessionText, "internal sealed class DiagnosticSessionBackgroundTasks");
        AssertContains(diagnosticSessionText, "internal sealed class DiagnosticSessionRunState");
        AssertContains(diagnosticSessionText, "var runState = new DiagnosticSessionRunState(");
        AssertContains(diagnosticSessionText, "backgroundTasks.AwaitScenarioTasksAsync()");
        AssertContains(diagnosticSessionText, "backgroundTasks.ObserveAfterFaultAsync(");
        AssertContains(diagnosticScenariosText, "internal static IReadOnlyList<string> All { get; }");
        AssertContains(diagnosticScenariosText, "internal const string FlashbackPlayback = \"flashback-playback\";");
        AssertContains(diagnosticScenariosText, "internal const string FlashbackStress = \"flashback-stress\";");
        AssertContains(diagnosticScenariosText, "internal const string FlashbackScrubStress = \"flashback-scrub-stress\";");
        AssertContains(diagnosticScenariosText, "internal const string FlashbackRestartCycle = \"flashback-restart-cycle\";");
        AssertContains(diagnosticScenariosText, "internal const string FlashbackEncoderCycle = \"flashback-encoder-cycle\";");
        AssertContains(diagnosticScenariosText, "internal const string FlashbackExportPlayback = \"flashback-export-playback\";");
        AssertContains(diagnosticScenariosText, "internal const string FlashbackSegmentPlayback = \"flashback-segment-playback\";");
        AssertContains(diagnosticScenariosText, "internal const string FlashbackRangeExport = \"flashback-range-export\";");
        AssertContains(diagnosticScenariosText, "internal const string FlashbackRangeExportAudioSwitch = \"flashback-range-export-audio-switch\";");
        AssertContains(diagnosticScenariosText, "internal const string FlashbackLifecycle = \"flashback-lifecycle\";");
        AssertContains(diagnosticScenariosText, "internal const string FlashbackExportConcurrent = \"flashback-export-concurrent\";");
        AssertContains(diagnosticScenariosText, "internal const string FlashbackDisableDuringExport = \"flashback-disable-during-export\";");
        AssertContains(diagnosticScenariosText, "internal const string FlashbackRotatedExport = \"flashback-rotated-export\";");
        AssertContains(diagnosticScenariosText, "internal const string FlashbackPreviewCycle = \"flashback-preview-cycle\";");
        AssertContains(diagnosticScenariosText, "internal const string FlashbackPlaybackPreviewCycle = \"flashback-playback-preview-cycle\";");
        AssertContains(diagnosticScenariosText, "internal const string FlashbackRecording = \"flashback-recording\";");
        AssertContains(diagnosticScenariosText, "internal const string FlashbackRecordingPreviewCycle = \"flashback-recording-preview-cycle\";");
        AssertContains(diagnosticScenariosText, "internal const string FlashbackRecordingSettingsDeferred = \"flashback-recording-settings-deferred\";");
        AssertContains(diagnosticScenariosText, "internal const string FlashbackRecordingExportRejected = \"flashback-recording-export-rejected\";");
        AssertContains(diagnosticScenariosText, "internal const string FlashbackExportRejected = \"flashback-export-rejected\";");
        AssertContains(diagnosticSessionText, "internal readonly record struct DiagnosticSessionScenarioPlan(");
        AssertContains(diagnosticSessionText, "catch (AutomationPipeException ex) when (ex is not AutomationPipeConnectException)");
        AssertContains(diagnosticSessionText, "return BuildLocalFailureResponse(command, ex.Message);");
        AssertContains(diagnosticSessionText, "catch (JsonException ex)");
        AssertContains(diagnosticSessionModelsText, "public sealed class DiagnosticSessionResult");
        AssertContains(diagnosticSessionModelsText, "public string TerminalState { get; set; }");
        AssertContains(diagnosticSessionText, "var livePath = runState.LivePath;");
        AssertContains(diagnosticSessionText, "var initialSnapshotKnown = false;");
        AssertContains(diagnosticSessionText, "skipped state-mutating scenario");
        AssertContains(diagnosticSessionText, "CreateCleanupCts(TimeSpan.FromMilliseconds(recordingCleanupTimeoutMs))");
        AssertContains(diagnosticSessionText, "SetRecordingEnabled\", new Dictionary<string, object?> { [\"enabled\"] = false }, recordingCleanupTimeoutMs");
        AssertContains(diagnosticSessionText, "var shouldStopRecordingForVerification = startedRecording && options.VerifyRecording;");
        AssertContains(diagnosticSessionText, "if (startedRecording && (shouldStopRecordingForVerification || !options.LeaveRunning))");
        AssertContains(diagnosticSessionText, "recording stopped for verification");
        AssertContains(diagnosticSessionText, "var stoppedRecordingForVerification = false;");
        AssertContains(diagnosticSessionText, "stoppedRecordingForVerification = shouldStopRecordingForVerification &&");
        AssertContains(diagnosticSessionText, "var diagnosticHealthSnapshot = stoppedRecordingForVerification");
        AssertContains(diagnosticSessionText, ".WaitAsync(cancellationToken)");
        AssertContains(diagnosticSessionText, "scenarioCts.Cancel();");
        AssertContains(diagnosticSessionText, "WriteSamplingLiveStateBestEffortAsync");
        AssertContains(diagnosticSessionText, "RecordTerminalException(ex, runState.LastStage);");
        AssertContains(diagnosticSessionText, "RecordTerminalException(ex, \"final-snapshot\");");
        AssertContains(diagnosticSessionText, "WriteArtifactBestEffortAsync(\"write-samples\", samplesPath, samples)");
        AssertContains(diagnosticSessionText, "await WriteJsonAsync(summaryPath, result, CancellationToken.None)");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackPendingCommandsAtEnd");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackMaxPendingCommandsObserved");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackMaxCommandQueueLatencyMsObserved");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackCommandsDroppedAtEnd");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackCommandsSkippedNotReadyAtEnd");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackScrubUpdatesCoalescedAtEnd");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackSeekCommandsCoalescedAtEnd");
        AssertContains(diagnosticSessionText, "internal readonly record struct PlaybackCommandHealth");
        AssertContains(diagnosticSessionText, "BuildPlaybackCommandHealth");
        AssertContains(diagnosticSessionText, "nonCoalescedDropped={commandHealth.NonCoalescedDropped}");
        AssertContains(diagnosticSessionText, "coalescedSeek={commandHealth.CoalescedSeek}");
        AssertContains(diagnosticSessionText, "GetCounterDelta(snapshot, baselineSnapshot, \"FlashbackPlaybackSubmitFailures\")");
        AssertContains(diagnosticSessionText, "GetCounterDelta(snapshot, baselineSnapshot, \"FlashbackPlaybackSeekCommandsCoalesced\")");
        AssertContains(diagnosticSessionText, "commandHealth.SubmitFailures > 0");
        AssertContains(diagnosticSessionText, "submitFailures={commandHealth.SubmitFailures}");
        AssertContains(diagnosticSessionText, "GetCounterDelta(snapshot, baselineSnapshot, \"FlashbackPlaybackCommandsDropped\")");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackObservedFpsAtEnd");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackMinObservedFpsObserved");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackAvgFrameMsAtEnd");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackP99FrameMsAtEnd");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackOnePercentLowFpsAtEnd");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackMinOnePercentLowFpsObserved");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackOnePercentLowSampleWindowObserved");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackOnePercentLowMinimumFrames");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackMaxSessionFrameCountObserved");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackMinOnePercentLowOffsetMs");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackMinOnePercentLowFrameCount");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackMinOnePercentLowDecodeP99Ms");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackMinOnePercentLowAudioMasterFallbacks");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackMaxP99FrameMsObserved");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackMaxFrameMsObserved");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackMaxSlowFramePercentObserved");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackDecodeAvgMsAtEnd");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackDecodeP99MsAtEnd");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackMaxDecodePhaseAtEnd");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackMaxDecodePhaseObserved");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackMaxDecodeP99MsObserved");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackMaxDecodeMsObserved");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackSlowFramePercentAtEnd");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackAudioMasterDelayDoublesAtEnd");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackAudioMasterDelayShrinksAtEnd");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackAudioMasterFallbacksAtEnd");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackMaxAudioMasterDelayDoublesObserved");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackMaxAudioMasterDelayShrinksObserved");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackMaxAudioMasterFallbacksObserved");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackMaxAudioBufferedDurationMsObserved");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackMaxAudioQueueDurationMsObserved");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackMaxAbsAvDriftMsObserved");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackDroppedFramesDelta");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackSubmitFailuresAtEnd");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackSubmitFailuresDelta");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackSegmentSwitchesAtEnd");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackFmp4ReopensAtEnd");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackWriteHeadWaitsAtEnd");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackSeekForwardDecodeCapHitsAtEnd");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackSeekForwardDecodeCapHitsDelta");
        AssertContains(diagnosticSessionText, "flashback playback seek forward-decode cap hit during session");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackLastCommandFailureUtcUnixMsAtEnd");
        AssertContains(diagnosticSessionText, "Flashback Playback Commands:");
        AssertContains(diagnosticSessionText, "coalescedScrubEnd={result.FlashbackPlaybackScrubUpdatesCoalescedAtEnd}");
        AssertContains(diagnosticSessionText, "coalescedSeekEnd={result.FlashbackPlaybackSeekCommandsCoalescedAtEnd}");
        AssertContains(diagnosticSessionText, "failureUtcEnd={result.FlashbackPlaybackLastCommandFailureUtcUnixMsAtEnd}");
        AssertContains(diagnosticSessionText, "Flashback Playback Perf:");
        AssertContains(diagnosticSessionText, "new Dictionary<string, object?> { [\"action\"] = \"play\", [\"positionMs\"] = 1000 }");
        AssertContains(diagnosticSessionText, "flashback playback started at 1000ms");
        AssertContains(diagnosticSessionText, "flashback playback returned live");
        AssertContains(diagnosticSessionText, "ValidateFlashbackPlaybackSession(");
        AssertContains(diagnosticSessionText, "visualCadenceMetrics,");
        AssertContains(diagnosticSessionText, "internal static void ValidateFlashbackPlaybackSession(");
        AssertContains(diagnosticSessionText, "flashback playback: no playback frames were observed");
        AssertContains(diagnosticSessionText, "var frameCount = Math.Max(metrics.EndSessionFrameCount, metrics.MaxSessionFrameCountObserved);");
        AssertContains(diagnosticSessionText, "var visualCadenceHealthy = IsVisualCadenceSessionHealthy(visualCadenceMetrics, targetFps);");
        AssertContains(diagnosticSessionText, "if (!visualCadenceHealthy &&");
        AssertContains(diagnosticSessionText, "GetResetAwareCounterDelta(");
        AssertContains(diagnosticSessionText, "public JsonElement BaselineSnapshot { get; init; }");
        AssertContains(diagnosticSessionText, "public long EndSessionFrameCount { get; set; }");
        AssertDoesNotContain(diagnosticSessionText, "flashback playback: observed FPS dipped below floor");
        AssertContains(diagnosticSessionText, "flashback playback: 1% low dipped below floor");
        AssertContains(diagnosticSessionText, "flashback playback: dropped frames increased delta={metrics.DroppedFramesDelta}");
        AssertContains(diagnosticSessionText, "flashback playback: submit failures increased delta={metrics.SubmitFailuresDelta}");
        AssertContains(diagnosticSessionText, "flashback playback: audio buffered duration exceeded budget");
        AssertContains(diagnosticSessionText, "flashback playback: absolute A/V drift exceeded budget");
        AssertContains(diagnosticSessionText, "BuildFlashbackPlaybackSessionMetrics(initialSnapshot, samples, lastSnapshot)");
        AssertContains(diagnosticSessionText, "BuildFlashbackPlaybackResultMetrics(playbackSessionMetrics)");
        AssertContains(diagnosticSessionText, "var baselineFrameCount = GetNullableLong(initialSnapshot, \"FlashbackPlaybackFrameCount\") ?? 0;");
        AssertContains(diagnosticSessionText, "frameCount > baselineFrameCount");
        AssertContains(diagnosticSessionText, "commandsProcessed > baselineCommandsProcessed");
        AssertContains(diagnosticSessionText, "IsPlaybackSnapshotActive(snapshot)");
        AssertContains(diagnosticSessionText, "var sessionFrameCount = frameCount >= baselineFrameCount");
        AssertContains(diagnosticSessionText, "? frameCount - baselineFrameCount");
        AssertContains(diagnosticSessionText, ": frameCount;");
        AssertContains(diagnosticSessionText, "metrics.EndSessionFrameCount = sessionFrameCount;");
        AssertContains(diagnosticSessionText, "targetFps > 0 ? (long)Math.Ceiling(targetFps * 10.0) : 240");
        AssertContains(diagnosticSessionText, "onePercentLow > 0 && sessionFrameCount >= minimumPlaybackFramesForLowPercentile");
        AssertContains(diagnosticSessionText, "metrics.OnePercentLowSampleWindowObserved = true;");
        AssertContains(diagnosticSessionText, "metrics.MaxSessionFrameCountObserved = Math.Max(metrics.MaxSessionFrameCountObserved, sessionFrameCount);");
        AssertContains(diagnosticSessionText, "fpsMin={result.FlashbackPlaybackMinObservedFpsObserved:0.##}");
        AssertContains(diagnosticSessionText, "onePercentLowFpsMin={result.FlashbackPlaybackMinOnePercentLowFpsObserved:0.##}");
        AssertContains(diagnosticSessionText, "onePercentLowWindow={result.FlashbackPlaybackOnePercentLowSampleWindowObserved}");
        AssertContains(diagnosticSessionText, "onePercentLowMinRequiredFrames={result.FlashbackPlaybackOnePercentLowMinimumFrames}");
        AssertContains(diagnosticSessionText, "onePercentLowMaxSessionFrames={result.FlashbackPlaybackMaxSessionFrameCountObserved}");
        AssertContains(diagnosticSessionText, "onePercentLowMinOffsetMs={result.FlashbackPlaybackMinOnePercentLowOffsetMs}");
        AssertContains(diagnosticSessionText, "onePercentLowMinDecodeP99Ms={result.FlashbackPlaybackMinOnePercentLowDecodeP99Ms:0.##}");
        AssertContains(diagnosticSessionText, "onePercentLowMinAudioFallbacks={result.FlashbackPlaybackMinOnePercentLowAudioMasterFallbacks}");
        AssertContains(diagnosticSessionText, "p99FrameMsMax={result.FlashbackPlaybackMaxP99FrameMsObserved:0.##}");
        AssertContains(diagnosticSessionText, "slowPctMax={result.FlashbackPlaybackMaxSlowFramePercentObserved:0.##}");
        AssertContains(diagnosticSessionText, "droppedFramesDelta={result.FlashbackPlaybackDroppedFramesDelta}");
        AssertContains(diagnosticSessionText, "audioMasterDoubleEnd={result.FlashbackPlaybackAudioMasterDelayDoublesAtEnd}");
        AssertContains(diagnosticSessionText, "audioMasterDoubleMax={result.FlashbackPlaybackMaxAudioMasterDelayDoublesObserved}");
        AssertContains(diagnosticSessionText, "audioMasterShrinkEnd={result.FlashbackPlaybackAudioMasterDelayShrinksAtEnd}");
        AssertContains(diagnosticSessionText, "audioMasterShrinkMax={result.FlashbackPlaybackMaxAudioMasterDelayShrinksObserved}");
        AssertContains(diagnosticSessionText, "audioMasterFallbackEnd={result.FlashbackPlaybackAudioMasterFallbacksAtEnd}");
        AssertContains(diagnosticSessionText, "audioMasterFallbackMax={result.FlashbackPlaybackMaxAudioMasterFallbacksObserved}");
        AssertContains(diagnosticSessionText, "audioBufferedMsMax={result.FlashbackPlaybackMaxAudioBufferedDurationMsObserved:0.##}");
        AssertContains(diagnosticSessionText, "audioQueueMsMax={result.FlashbackPlaybackMaxAudioQueueDurationMsObserved:0.##}");
        AssertContains(diagnosticSessionText, "absAvDriftMsMax={result.FlashbackPlaybackMaxAbsAvDriftMsObserved:0.##}");
        AssertContains(diagnosticSessionText, "submitFailuresEnd={result.FlashbackPlaybackSubmitFailuresAtEnd}");
        AssertContains(diagnosticSessionText, "submitFailuresDelta={result.FlashbackPlaybackSubmitFailuresDelta}");
        AssertContains(diagnosticSessionText, "Flashback Playback Decode:");
        AssertContains(diagnosticSessionText, "p99MsMax={result.FlashbackPlaybackMaxDecodeP99MsObserved:0.##}");
        AssertContains(diagnosticSessionText, "maxMsObserved={result.FlashbackPlaybackMaxDecodeMsObserved:0.##}");
        AssertContains(diagnosticSessionText, "phaseObserved={result.FlashbackPlaybackMaxDecodePhaseObserved}");
        AssertContains(diagnosticSessionText, "sendMsObserved={result.FlashbackPlaybackMaxDecodeSendMsObserved:0.##}");
        AssertContains(diagnosticSessionText, "audioMsObserved={result.FlashbackPlaybackMaxDecodeAudioMsObserved:0.##}");
        AssertContains(diagnosticSessionText, "Flashback Playback Stages:");
        AssertContains(diagnosticSessionText, "seekCapHitsDelta={result.FlashbackPlaybackSeekForwardDecodeCapHitsDelta}");
        AssertContains(diagnosticSessionText, "FlashbackRecordingBackendObserved");
        AssertContains(diagnosticSessionText, "PreviewD3DFrameStatsMissedRefreshDelta");
        AssertContains(diagnosticSessionText, "PreviewD3DFrameStatsFailureDelta");
        AssertContains(diagnosticSessionText, "SelectedResolutionAtEnd");
        AssertContains(diagnosticSessionText, "SelectedFrameRateAtEnd");
        AssertContains(diagnosticSessionText, "SelectedExactFrameRateArgAtEnd");
        AssertContains(diagnosticSessionText, "SelectedVideoFormatAtEnd");
        AssertContains(diagnosticSessionText, "VideoRequestedSubtypeAtEnd");
        AssertContains(diagnosticSessionText, "VideoNegotiatedSubtypeAtEnd");
        AssertContains(diagnosticSessionText, "DetectedSourceFrameRateAtEnd");
        AssertContains(diagnosticSessionText, "DetectedSourceFrameRateArgAtEnd");
        AssertContains(diagnosticSessionText, "SourceTelemetrySummaryAtEnd");
        AssertContains(diagnosticSessionText, "Capture Mode:");
        AssertContains(diagnosticSessionText, "selected={FormatOptional(result.SelectedResolutionAtEnd)}");
        AssertContains(diagnosticSessionText, "source={result.SourceWidthAtEnd}x{result.SourceHeightAtEnd}");
        AssertContains(diagnosticSessionText, "PreviewSchedulerDroppedAtEnd");
        AssertContains(diagnosticSessionText, "PreviewSchedulerDeadlineDropsAtEnd");
        AssertContains(diagnosticSessionText, "PreviewSchedulerClearedDropsAtEnd");
        AssertContains(diagnosticSessionText, "PreviewSchedulerUnderflowsAtEnd");
        AssertContains(diagnosticSessionText, "PreviewSchedulerResumeReprimesAtEnd");
        AssertContains(diagnosticSessionText, "PreviewSchedulerDroppedDelta");
        AssertContains(diagnosticSessionText, "PreviewSchedulerDeadlineDropsDelta");
        AssertContains(diagnosticSessionText, "PreviewSchedulerClearedDropsDelta");
        AssertContains(diagnosticSessionText, "PreviewSchedulerUnderflowsDelta");
        AssertContains(diagnosticSessionText, "PreviewSchedulerResumeReprimesDelta");
        AssertContains(diagnosticSessionText, "PreviewSchedulerLastDropReasonAtEnd");
        AssertContains(diagnosticSessionText, "Preview Scheduler:");
        AssertContains(diagnosticSessionText, "droppedDelta={result.PreviewSchedulerDroppedDelta}");
        AssertContains(diagnosticSessionText, "clearedDropsDelta={result.PreviewSchedulerClearedDropsDelta}");
        AssertContains(diagnosticSessionText, "deadlineDropsDelta={result.PreviewSchedulerDeadlineDropsDelta}");
        AssertContains(diagnosticSessionText, "underflowsDelta={result.PreviewSchedulerUnderflowsDelta}");
        AssertContains(diagnosticSessionText, "resumeReprimesDelta={result.PreviewSchedulerResumeReprimesDelta}");
        AssertContains(diagnosticSessionText, "lastDropReasonEnd={FormatOptional(result.PreviewSchedulerLastDropReasonAtEnd)}");
        AssertContains(diagnosticSessionText, "PreviewD3DLatestSlowFrameReason");
        AssertContains(diagnosticSessionText, "PreviewD3DInputUploadCpuP99MsAtEnd");
        AssertContains(diagnosticSessionText, "PreviewD3DInputUploadCpuMaxMsObserved");
        AssertContains(diagnosticSessionText, "PreviewD3DRenderSubmitCpuP99MsAtEnd");
        AssertContains(diagnosticSessionText, "PreviewD3DRenderSubmitCpuMaxMsObserved");
        AssertContains(diagnosticSessionText, "PreviewD3DPresentCallP99MsAtEnd");
        AssertContains(diagnosticSessionText, "PreviewD3DPresentCallMaxMsObserved");
        AssertContains(diagnosticSessionText, "PreviewD3DTotalFrameCpuP99MsAtEnd");
        AssertContains(diagnosticSessionText, "PreviewD3DTotalFrameCpuMaxMsObserved");
        AssertContains(diagnosticSessionText, "VisualCadenceOutputFpsAtEnd");
        AssertContains(diagnosticSessionText, "VisualCadenceChangeFpsAtEnd");
        AssertContains(diagnosticSessionText, "VisualCadenceMinChangeFpsObserved");
        AssertContains(diagnosticSessionText, "VisualCadenceMaxRepeatPercentObserved");
        AssertContains(diagnosticSessionText, "ProcessCpuPercentAtEnd");
        AssertContains(diagnosticSessionText, "ProcessCpuMaxPercentObserved");
        AssertContains(diagnosticSessionText, "Preview D3D Perf:");
        AssertContains(diagnosticSessionText, "Preview D3D CPU Timing:");
        AssertContains(diagnosticSessionText, "Preview Visual Cadence:");
        AssertContains(diagnosticSessionText, "Process Perf:");
        AssertContains(diagnosticSessionText, "PreviewCadenceOnePercentLowFpsAtEnd");
        AssertContains(diagnosticSessionText, "PreviewCadenceMinOnePercentLowFpsObserved");
        AssertContains(diagnosticSessionText, "BuildPreviewD3DMetrics(initialSnapshot, lastSnapshot, samples)");
        AssertContains(diagnosticSessionText, "BuildVisualCadenceSessionMetrics(samples, lastSnapshot)");
        AssertContains(diagnosticSessionText, "private static void ObservePreviewD3DCpuTiming(PreviewD3DMetrics metrics, JsonElement snapshot)");
        AssertContains(diagnosticSessionText, "BuildPreviewCadenceSessionMetrics(samples, lastSnapshot)");
        AssertContains(diagnosticSessionText, "onePercentLowFpsMin={result.PreviewCadenceMinOnePercentLowFpsObserved:0.##}");
        AssertContains(diagnosticSessionText, "latestSlowReason={FormatOptional(result.PreviewD3DLatestSlowFrameReason)}");
        AssertContains(diagnosticSessionText, "inputUploadP99End={result.PreviewD3DInputUploadCpuP99MsAtEnd:0.##}");
        AssertContains(diagnosticSessionText, "presentCallMaxObserved={result.PreviewD3DPresentCallMaxMsObserved:0.##}");
        AssertContains(diagnosticSessionText, "totalFrameMaxObserved={result.PreviewD3DTotalFrameCpuMaxMsObserved:0.##}");
        AssertContains(diagnosticSessionText, "changeFpsMin={result.VisualCadenceMinChangeFpsObserved:0.##}");
        AssertContains(diagnosticSessionText, "repeatPctMax={result.VisualCadenceMaxRepeatPercentObserved:0.###}");
        AssertContains(diagnosticsText, "PreviewCadenceSlowFramePercent = snapshot.PreviewCadenceSlowFramePercent");
        AssertContains(diagnosticsText, "PreviewCadenceOnePercentLowFps = snapshot.PreviewCadenceOnePercentLowFps");
        AssertContains(diagnosticsText, "1pctLow={previewRuntime.DisplayCadenceOnePercentLowFps:0.##}fps");
        AssertContains(diagnosticsText, "PreviewD3DPresentCallP95Ms = snapshot.PreviewD3DPresentCallP95Ms");
        AssertContains(diagnosticsText, "PreviewD3DTotalFrameCpuP95Ms = snapshot.PreviewD3DTotalFrameCpuP95Ms");
        AssertContains(diagnosticsText, "PreviewD3DInputUploadCpuP99Ms = snapshot.PreviewD3DInputUploadCpuP99Ms");
        AssertContains(diagnosticsText, "PreviewD3DRenderSubmitCpuP99Ms = snapshot.PreviewD3DRenderSubmitCpuP99Ms");
        AssertContains(diagnosticsText, "PreviewD3DPresentCallP99Ms = snapshot.PreviewD3DPresentCallP99Ms");
        AssertContains(diagnosticsText, "PreviewD3DTotalFrameCpuP99Ms = snapshot.PreviewD3DTotalFrameCpuP99Ms");
        AssertContains(diagnosticsText, "PreviewD3DFrameStatsRecentMissedRefreshCount = snapshot.PreviewD3DFrameStatsRecentMissedRefreshCount");
        AssertContains(diagnosticsText, "FlashbackPlaybackP99FrameMs = snapshot.FlashbackPlaybackP99FrameMs");
        AssertContains(diagnosticsText, "FlashbackPlaybackDecodeP99Ms = snapshot.FlashbackPlaybackDecodeP99Ms");
        AssertContains(diagnosticsText, "FlashbackPlaybackPendingCommands = snapshot.FlashbackPlaybackPendingCommands");
        AssertContains(diagnosticsText, "FlashbackPlaybackSubmitFailures = snapshot.FlashbackPlaybackSubmitFailures");
        AssertContains(diagnosticsText, "FlashbackExportPercent = snapshot.FlashbackExportPercent");
        AssertContains(diagnosticsText, "FlashbackExportThroughputBytesPerSec = snapshot.FlashbackExportThroughputBytesPerSec");
        AssertContains(diagnosticsText, "FlashbackExportLastProgressAgeMs = snapshot.FlashbackExportLastProgressAgeMs");
        AssertContains(diagnosticSessionText, "FlashbackRecordingFileGrowthObserved");
        AssertContains(diagnosticSessionText, "FlashbackRecordingVideoFramesSubmittedDelta");
        AssertContains(diagnosticSessionText, "FlashbackRecordingVideoEncoderPacketsWrittenDelta");
        AssertContains(diagnosticSessionText, "FlashbackRecordingIntegritySequenceGapsAtEnd");
        AssertContains(diagnosticSessionText, "FlashbackRecordingIntegrityQueueDroppedFramesAtEnd");
        AssertContains(diagnosticSessionText, "FlashbackRecordingIntegritySequenceGapsDelta");
        AssertContains(diagnosticSessionText, "FlashbackRecordingIntegrityQueueDroppedFramesDelta");
        AssertContains(diagnosticSessionText, "firstRecordingSample,\n                \"RecordingIntegritySequenceGaps\")");
        AssertContains(diagnosticSessionText, "firstRecordingSample,\n                \"RecordingIntegrityQueueDroppedFrames\")");
        AssertContains(diagnosticSessionText, "Flashback Recording:");
        AssertContains(diagnosticSessionText, "FlashbackExportMaxElapsedMsObserved");
        AssertContains(diagnosticSessionText, "FlashbackExportMessageAtEnd");
        AssertContains(diagnosticSessionText, "FlashbackExportFailureKindAtEnd");
        AssertContains(diagnosticSessionText, "FlashbackExportOutputPathAtEnd");
        AssertContains(diagnosticSessionText, "FlashbackExportForceRotateFallbacksAtEnd");
        AssertContains(diagnosticSessionText, "FlashbackExportForceRotateFallbacksDelta");
        AssertContains(diagnosticSessionText, "FlashbackExportLastForceRotateFallbackSegmentsAtEnd");
        AssertContains(diagnosticSessionText, "LastExportIdAtEnd");
        AssertContains(diagnosticSessionText, "LastExportSuccessAtEnd");
        AssertContains(diagnosticSessionText, "LastExportMessageAtEnd");
        AssertContains(diagnosticSessionText, "FlashbackExportMaxLastProgressAgeMsObserved");
        AssertContains(diagnosticSessionText, "FlashbackExportMaxOutputBytesObserved");
        AssertContains(diagnosticSessionText, "FlashbackExportMaxThroughputBytesPerSecObserved");
        AssertContains(diagnosticSessionText, "BuildFlashbackExportSessionMetrics(initialSnapshot, samples, lastSnapshot)");
        AssertContains(diagnosticSessionText, "var healthSnapshot = lastSnapshot;");
        AssertContains(diagnosticSessionText, "RecordTerminalException(ex, \"final-snapshot\");");
        AssertContains(diagnosticSessionText, "exportId > baselineExportId");
        AssertContains(diagnosticSessionText, "baselineExportActive && exportId == baselineExportId");
        AssertContains(diagnosticSessionText, "lastExportId == exportId");
        AssertContains(diagnosticSessionText, "DiagnosticSessionScenarios.TryGetFlashbackExportVerificationPath(");
        AssertContains(diagnosticSessionText, "var shouldRunVerification =");
        AssertContains(diagnosticSessionText, "recording verification skipped: scenario does not produce a recording or export artifact");
        AssertContains(diagnosticSessionText, "verificationCommand = \"VerifyFile\"");
        AssertContains(diagnosticSessionText, "[\"verificationProfile\"] = \"flashback-export\"");
        AssertContains(diagnosticScenariosText, "FlashbackRangeExport => Path.Combine(outputDirectory, \"flashback-range-export.mp4\")");
        AssertContains(diagnosticScenariosText, "FlashbackRangeExportAudioSwitch => Path.Combine(outputDirectory, \"flashback-range-export-audio-switch.mp4\")");
        AssertContains(diagnosticScenariosText, "FlashbackExportConcurrent => Path.Combine(outputDirectory, \"flashback-concurrent-a.mp4\")");
        AssertContains(diagnosticScenariosText, "FlashbackRotatedExport => Path.Combine(outputDirectory, \"flashback-rotated-export.mp4\")");
        AssertContains(diagnosticScenariosText, "return exportPath.Length > 0;");
        AssertDoesNotContain(diagnosticScenariosText, "return exportPath.Length > 0 && File.Exists(exportPath);");
        AssertContains(diagnosticSessionText, "expected BufferInactive failure kind");
        AssertContains(diagnosticSessionText, "expected UnavailableDuringRecording failure kind");
        AssertContains(diagnosticSessionText, "flashback rejected export observed status={status} kind={failureKind}");
        AssertContains(diagnosticSessionText, "flashback recording rejected export observed status={status} kind={failureKind}");
        AssertContains(diagnosticSessionText, "Flashback Export:");
        AssertContains(diagnosticSessionText, "failureKindEnd={FormatOptional(result.FlashbackExportFailureKindAtEnd)}");
        AssertContains(diagnosticSessionText, "messageEnd={FormatOptional(result.FlashbackExportMessageAtEnd)}");
        AssertContains(diagnosticSessionText, "forceRotateFallbacksDelta={result.FlashbackExportForceRotateFallbacksDelta}");
        AssertContains(diagnosticSessionText, "lastResultIdEnd={result.LastExportIdAtEnd}");
        AssertContains(diagnosticSessionText, "lastSuccessEnd={FormatOptional(result.LastExportSuccessAtEnd)}");
        AssertContains(diagnosticSessionText, "lastMessageEnd={FormatOptional(result.LastExportMessageAtEnd)}");
        AssertContains(diagnosticSessionText, "pathEnd={FormatOptional(result.FlashbackExportOutputPathAtEnd)}");
        AssertContains(diagnosticSessionText, "maxThroughput={FormatBytes((long)result.FlashbackExportMaxThroughputBytesPerSecObserved)}/s");
        AssertContains(diagnosticSessionText, "BuildFlashbackRecordingMetrics(initialSnapshot, samples)");
        AssertContains(diagnosticSessionText, "seqGapsDelta={result.FlashbackRecordingIntegritySequenceGapsDelta}");
        AssertContains(diagnosticSessionText, "queueDropsDelta={result.FlashbackRecordingIntegrityQueueDroppedFramesDelta}");
        AssertContains(diagnosticSessionText, "Flashback video sequence gaps increased delta={metrics.IntegritySequenceGapsDelta}");
        AssertContains(diagnosticSessionText, "Flashback dropped frames increased delta={metrics.IntegrityQueueDroppedFramesDelta}");
        AssertContains(diagnosticSessionText, "internal static void ValidateCleanupLifecycleRestored(");
        AssertContains(diagnosticSessionText, "cleanup: preview remained active after restore");
        AssertContains(diagnosticSessionText, "cleanup: Flashback remained active after restore");
        AssertContains(diagnosticSessionText, "cleanup: playback did not return live state={state}");
        AssertContains(diagnosticSessionText, "metrics.MaxPendingCommandsObserved = Math.Max(");
        AssertContains(diagnosticSessionText, "if (maxCommandQueueLatencyMs > metrics.MaxCommandQueueLatencyMsObserved)");
        AssertContains(diagnosticSessionText, "metrics.MaxCommandQueueLatencyMsObserved = maxCommandQueueLatencyMs;");
        AssertContains(diagnosticSessionText, "metrics.MaxCommandQueueLatencyCommandObserved = GetString(snapshot, \"FlashbackPlaybackMaxCommandQueueLatencyCommand\") ?? string.Empty;");
        AssertContains(diagnosticSessionText, "internal static async Task RunFlashbackStressAsync(");
        AssertContains(diagnosticSessionText, "internal static async Task RunFlashbackScrubStressAsync(");
        AssertContains(diagnosticSessionText, "flashback scrub stress begin requested");
        AssertContains(diagnosticSessionText, "flashback scrub stress update burst requested");
        AssertContains(diagnosticSessionText, "flashback scrub stress end requested");
        AssertContains(diagnosticSessionText, "GetInt(lastSnapshot, \"FlashbackPlaybackPendingCommands\") == 0 &&\n                string.Equals(");
        AssertContains(diagnosticSessionText, "state={GetString(lastSnapshot, \"FlashbackPlaybackState\") ?? \"Unknown\"}");
        AssertContains(diagnosticSessionText, "flashback scrub stress: playback did not settle live with an empty queue within 10s");
        AssertDoesNotContain(diagnosticSessionText, "flashback scrub stress: playback worker still alive after drain wait");
        AssertContains(diagnosticSessionText, "GetString(lastSnapshot, \"FlashbackPlaybackState\")");
        AssertContains(diagnosticSessionText, "internal static async Task RunFlashbackRestartCycleAsync(");
        AssertContains(diagnosticSessionText, "flashback restart cycle export verified");
        AssertContains(diagnosticSessionText, "internal static async Task RunFlashbackEncoderCycleAsync(");
        AssertContains(diagnosticSessionText, "\"flashback-encoder-cycle-export.mp4\"");
        AssertContains(diagnosticSessionText, "flashback encoder preset restored to");
        AssertContains(diagnosticSessionText, "flashback encoder cycle export verified");
        AssertContains(diagnosticSessionText, "internal static async Task RunFlashbackExportPlaybackAsync(");
        AssertContains(diagnosticSessionText, "flashback export during playback verified");
        AssertContains(diagnosticSessionText, "internal static async Task RunFlashbackSegmentPlaybackAsync(");
        AssertContains(diagnosticSessionText, "internal static async Task<FlashbackSegmentProbe?> WaitForFlashbackCompletedSegmentAsync(");
        AssertContains(diagnosticSessionText, "internal static async Task<bool> WaitForFlashbackSegmentPlaybackHeadroomAsync(");
        AssertContains(diagnosticSessionText, "const int requiredHeadroomMs = 8_000;");
        AssertContains(diagnosticSessionText, "flashback segment playback live headroom established");
        AssertContains(diagnosticSessionText, "flashback segment playback started near boundary");
        AssertContains(diagnosticSessionText, "frameCount >= 180");
        AssertContains(diagnosticSessionText, "playback FPS below source-rate target after warm sample");
        AssertContains(diagnosticSessionText, "internal static async Task RunFlashbackRangeExportAsync(");
        AssertContains(diagnosticSessionText, "\"flashback-range-export.mp4\"");
        AssertContains(diagnosticSessionText, "\"flashback-range-export-audio-switch.mp4\"");
        AssertContains(diagnosticSessionText, "internal static async Task ToggleAudioEnabledDuringFlashbackExportAsync(");
        AssertContains(diagnosticSessionText, "\"SetAudioEnabled\"");
        AssertContains(diagnosticSessionText, "FlashbackExportActive");
        AssertContains(diagnosticSessionText, "[\"useSelectionRange\"] = true");
        AssertContains(diagnosticSessionText, "actions.Add($\"{scenarioLabel} verified\")");
        AssertContains(diagnosticSessionText, "internal static async Task RunFlashbackLifecycleAsync(");
        AssertContains(diagnosticSessionText, "internal static async Task RunFlashbackExportConcurrentAsync(");
        AssertContains(diagnosticSessionText, "async Task<JsonElement> SendRawWithConnectRetryAsync(");
        AssertContains(diagnosticSessionText, "var exportTimeoutMs = AutomationPipeProtocol.GetDefaultResponseTimeout(\"FlashbackExport\");");
        AssertContains(diagnosticSessionText, "var exportTaskA = sendCommandAsync(\"FlashbackExport\", exportPayloadA, exportTimeoutMs);");
        AssertContains(diagnosticSessionText, "var exportTaskB = sendCommandAsync(\"FlashbackExport\", exportPayloadB, exportTimeoutMs);");
        AssertContains(diagnosticSessionText, "flashback concurrent exports verified");
        AssertContains(diagnosticSessionText, "internal static async Task RunFlashbackDisableDuringExportAsync(");
        AssertContains(diagnosticSessionText, "\"flashback-disable-during-export.mp4\"");
        AssertContains(diagnosticSessionText, "var disableTask = SendCommandWithConnectRetryAsync(");
        AssertContains(diagnosticSessionText, "flashback disable/export requests issued");
        AssertContains(diagnosticSessionText, "flashback disable during export verified");
        AssertContains(diagnosticSessionText, "internal static async Task RunFlashbackRotatedExportAsync(");
        AssertContains(diagnosticSessionText, "\"flashback-rotated-export.mp4\"");
        AssertContains(diagnosticSessionText, "flashback rotated segment observed");
        AssertContains(diagnosticSessionText, "TryParseFlashbackExportSegmentCount(exportMessage)");
        AssertContains(diagnosticSessionText, "exportedSegments is null or < 2");
        AssertContains(diagnosticSessionText, "flashback rotated export verified");
        AssertContains(diagnosticSessionText, "internal static async Task RunFlashbackPreviewCycleAsync(");
        AssertContains(diagnosticSessionText, "\"flashback-preview-off-export.mp4\"");
        AssertContains(diagnosticSessionText, "flashback preview cycle export verified");
        AssertContains(diagnosticSessionText, "internal static async Task RunFlashbackPlaybackPreviewCycleAsync(");
        AssertContains(diagnosticSessionText, "\"flashback-playback-preview-cycle.mp4\"");
        AssertContains(diagnosticSessionText, "flashback playback preview cycle preview stopped during playback");
        AssertContains(diagnosticSessionText, "flashback playback preview cycle: playback did not return live after preview stop");
        AssertContains(diagnosticSessionText, "flashback playback preview cycle export verified");
        AssertContains(diagnosticSessionText, "internal static async Task<JsonElement?> WaitForPreviewActiveAsync(");
        AssertContains(diagnosticSessionText, "internal static async Task RunFlashbackRecordingPreviewCycleAsync(");
        AssertContains(diagnosticSessionText, "flashback recording preview cycle preview stopped");
        AssertContains(diagnosticSessionText, "const int recordingCleanupTimeoutMs = 300_000;");
        AssertContains(diagnosticSessionText, "SetRecordingEnabled\", new Dictionary<string, object?> { [\"enabled\"] = false }, recordingCleanupTimeoutMs");
        AssertContains(diagnosticSessionText, "internal static async Task<JsonElement?> WaitForFlashbackRecordingReadyAsync(");
        AssertContains(diagnosticSessionText, "internal static async Task<FlashbackRecordingSettingsDeferredPresetState> RunFlashbackRecordingSettingsDeferredAsync(");
        AssertContains(diagnosticSessionText, "flashback recording settings deferred post-stop buffer verified");
        AssertContains(diagnosticSessionText, "flashback recording settings deferred preset restored to");
        AssertContains(diagnosticSessionText, "RestartFlashback unexpectedly succeeded during recording");
        AssertContains(diagnosticSessionText, "internal static async Task RunFlashbackRecordingExportRejectedAsync(");
        AssertContains(diagnosticSessionText, "\"flashback-recording-rejected-export.mp4\"");
        AssertContains(diagnosticSessionText, "Flashback export is unavailable while Flashback is the active recording backend");
        AssertContains(diagnosticSessionText, "flashback lifecycle disabled during playback");
        AssertContains(diagnosticSessionText, "flashback lifecycle: playback worker still alive after disable");
        AssertContains(diagnosticSessionText, "flashback lifecycle: pending commands remained after disable");
        AssertContains(diagnosticSessionText, "internal static async Task RunFlashbackExportRejectedAsync(");
        AssertContains(diagnosticSessionText, "internal static async Task<bool> WaitForFlashbackStressBufferReadyAsync(");
        AssertContains(diagnosticSessionText, "internal static void ValidateFlashbackRecordingSession(");
        AssertContains(diagnosticSessionText, "\"flashback recording: RecordingBackend never reported Flashback\"");
        AssertContains(diagnosticSessionText, "\"flashback recording: no Flashback video frames submitted to encoder\"");
        AssertContains(diagnosticSessionText, "submittedDelta");
        AssertContains(diagnosticSessionText, "packetsDelta");
        AssertContains(diagnosticSessionText, "RecordingIntegritySequenceGaps");
        AssertContains(diagnosticSessionText, "RecordingIntegrityQueueDroppedFrames");
        AssertContains(diagnosticSessionText, "GetInt(snapshot, \"FlashbackBufferedDurationMs\") >= requiredBufferedDurationMs");
        AssertContains(diagnosticSessionText, "(GetNullableLong(snapshot, \"FlashbackEncodedFrames\") ?? 0) >= requiredEncodedFrames");
        AssertContains(diagnosticSessionText, "const int liveEdgeSafetyMarginMs = 5_000;");
        AssertContains(diagnosticSessionText, "const int leftEdgeSafetyMarginMs = 10_000;");
        AssertContains(diagnosticSessionText, "outPointMs + liveEdgeSafetyMarginMs + leftEdgeSafetyMarginMs");
        AssertContains(diagnosticSessionText, "var rangeEndMs = (int)Math.Clamp(bufferedDurationMs - liveEdgeSafetyMarginMs, 0, int.MaxValue);");
        AssertContains(diagnosticSessionText, "var rangeStartMs = Math.Max(0, rangeEndMs - outPointMs);");
        AssertContains(diagnosticSessionText, "requiredStartMs>={leftEdgeSafetyMarginMs}");
        AssertContains(diagnosticSessionText, "\"flashback stress: Flashback buffer did not become export-ready within 30s\"");
        AssertContains(diagnosticSessionText, "\"FlashbackAction\", new Dictionary<string, object?> { [\"action\"] = \"pause\" }");
        AssertContains(diagnosticSessionText, "new Dictionary<string, object?> { [\"action\"] = \"seek\", [\"positionMs\"] = 500 }");
        AssertContains(diagnosticSessionText, "foreach (var positionMs in new[] { 750, 1_250, 2_000, 3_250, 1_500 })");
        AssertContains(diagnosticSessionText, "actions.Add(\"flashback scrub burst requested\");");
        AssertContains(diagnosticSessionText, "new Dictionary<string, object?> { [\"action\"] = \"begin-scrub\", [\"positionMs\"] = 500 }");
        AssertContains(diagnosticSessionText, "new Dictionary<string, object?> { [\"action\"] = \"update-scrub\", [\"positionMs\"] = positions[i] }");
        AssertContains(diagnosticSessionText, "new Dictionary<string, object?> { [\"action\"] = \"end-scrub\", [\"positionMs\"] = positions[^1] }");
        AssertContains(diagnosticSessionText, "new Dictionary<string, object?> { [\"seconds\"] = 1, [\"outputPath\"] = exportPath }");
        AssertContains(diagnosticSessionText, "internal static Dictionary<string, object?> CreateFlashbackExportVerifyPayload(string filePath)");
        AssertContains(diagnosticSessionText, "\"flashback stress: playback command queue did not drain within 10s \"");
        AssertContains(diagnosticSessionText, "$\"maxPending={GetInt(lastSnapshot, \"FlashbackPlaybackMaxPendingCommands\")} \"");
        AssertContains(diagnosticSessionText, "$\"maxLatencyMs={GetInt(lastSnapshot, \"FlashbackPlaybackMaxCommandQueueLatencyMs\")} \"");
        AssertContains(diagnosticSessionText, "FlashbackPlaybackMaxCommandQueueLatencyCommand");
        AssertContains(diagnosticSessionText, "internal const int FlashbackStressMaxPlaybackPendingCommands = 4;");
        AssertContains(diagnosticSessionText, "internal const int FlashbackStressMaxPlaybackCommandLatencyMs = 750;");
        AssertContains(diagnosticSessionText, "internal const double FlashbackStressPlaybackWarmSeconds = 10.0;");
        AssertContains(diagnosticSessionText, "internal const long FlashbackStressAudioUnavailableFallbackAllowance = 4;");
        AssertContains(diagnosticSessionText, "var playbackBaselineSnapshot = await WaitForFlashbackPlaybackStateAsync(");
        AssertContains(diagnosticSessionText, "\"flashback stress: playback did not enter Playing before warm sample\"");
        AssertContains(diagnosticSessionText, "var warmBaselineSnapshot = playbackBaselineSnapshot?.ValueKind == JsonValueKind.Object");
        AssertContains(diagnosticSessionText, "WaitForFlashbackPlaybackWarmSampleAsync(");
        AssertContains(diagnosticSessionText, "flashback playback warmed frames=");
        AssertContains(diagnosticSessionText, "audioFallbackDelta={warmedAudioFallbackDelta}");
        AssertContains(diagnosticSessionText, "staleDelta={warmedAudioStaleDelta}");
        AssertContains(diagnosticSessionText, "driftOutlierDelta={warmedAudioDriftOutlierDelta}");
        AssertContains(diagnosticSessionText, "\"flashback stress: playback did not warm for");
        AssertContains(diagnosticSessionText, "\"flashback stress: audio-master harmful fallbacks increased during warmed playback \"");
        AssertContains(diagnosticSessionText, "\"flashback stress: audio-master unavailable fallbacks exceeded startup allowance \"");
        AssertContains(diagnosticSessionText, "\"flashback stress: audio-master unclassified fallbacks increased during warmed playback");
        AssertContains(diagnosticSessionText, "internal static void ValidateFlashbackPreviewScheduler(");
        AssertContains(diagnosticSessionText, "\"flashback preview: scheduler deadline drops increased delta=");
        AssertContains(diagnosticSessionText, "\"flashback preview: scheduler underflows increased delta=");
        AssertContains(diagnosticSessionText, "\"flashback preview: D3D frame stats failures increased delta=");
        AssertContains(diagnosticSessionText, "\"flashback preview: present/display pressure \"");
        AssertContains(diagnosticSessionText, "var toleratesPreviewCycleSchedulerSettling =");
        AssertContains(diagnosticSessionText, "scenarioPlan.IsPreviewCycleScenario && visualCadenceHealthy");
        AssertContains(diagnosticSessionText, "var toleratesSparsePreviewSchedulerDeadlineDrops =");
        AssertContains(diagnosticSessionText, "IsSparsePreviewSchedulerDeadlineDropRun(");
        AssertContains(diagnosticSessionText, "internal static bool IsSparsePreviewSchedulerDeadlineDropRun(");
        AssertContains(diagnosticSessionText, "var allowedDrops = Math.Max(2, (long)Math.Ceiling(Math.Max(1, durationSeconds) / 10.0));");
        AssertContains(diagnosticSessionText, "var toleratesSparseScrubSchedulerTransitions =");
        AssertContains(diagnosticSessionText, "scenarioPlan.ToleratesSparsePreviewSchedulerStressTransitions &&");
        AssertContains(diagnosticSessionText, "IsSparsePreviewSchedulerStressRun(");
        AssertContains(diagnosticSessionText, "internal static bool IsSparsePreviewSchedulerStressRun(");
        AssertContains(diagnosticSessionText, "var allowedDeadlineDrops = Math.Max(6, (long)Math.Ceiling(Math.Max(1, durationSeconds) / 45.0));");
        AssertContains(diagnosticSessionText, "var allowedUnderflows = Math.Max(2, (long)Math.Ceiling(Math.Max(1, durationSeconds) / 120.0));");
        AssertContains(diagnosticSessionText, "bool tolerateSchedulerTransitionsWithHealthyVisualCadence");
        AssertContains(diagnosticSessionText, "deadlineDropsDelta > 0 && !tolerateSchedulerTransitionsWithHealthyVisualCadence");
        AssertContains(diagnosticSessionText, "underflowsDelta > 0 && !tolerateSchedulerTransitionsWithHealthyVisualCadence");
        AssertContains(diagnosticSessionText, "var onePercentLowFloor = targetFps * 0.80;");
        AssertContains(diagnosticSessionText, "var visualCadenceHealthy =");
        AssertContains(diagnosticSessionText, "IsVisualCadenceSessionHealthy(visualCadenceMetrics, targetFps)");
        AssertContains(diagnosticSessionText, "if ((onePercentLowMiss && !visualCadenceHealthy) || presentP99Miss || totalP99Miss)");
        AssertContains(diagnosticSessionText, "visualChangeFpsMin={visualCadenceMetrics.MinChangeFpsObserved:0.##}");
        AssertContains(diagnosticSessionText, "var presentP99BudgetMs = targetFrameMs * 1.25;");
        AssertContains(diagnosticSessionText, "var totalP99BudgetMs = targetFrameMs * 1.35;");
        AssertContains(diagnosticSessionText, "latestSlowReason={FormatOptional(previewD3DMetrics.LatestSlowFrameReason)}");
        AssertContains(diagnosticSessionText, "latestSlowPresentCallMs={previewD3DMetrics.LatestSlowFramePresentCallMs:0.##}");
        AssertContains(diagnosticSessionText, "latestSlowPending={previewD3DMetrics.LatestSlowFramePendingFrameCount}");
        AssertContains(diagnosticSessionText, "\"flashback stress: playback command latency exceeded threshold \"");
        AssertContains(diagnosticSessionText, "$\"maxLatencyMs={maxLatencyMs}/{FlashbackStressMaxPlaybackCommandLatencyMs} \"");
        AssertContains(diagnosticSessionText, "$\"maxLatencyCommand={FormatOptional(maxLatencyCommand)}\"");
        AssertContains(diagnosticSessionText, "\"flashback-rejected-export.mp4\"");
        AssertContains(diagnosticSessionText, "$\"flashback export rejected: expected Failed status, got {status}\"");
        AssertContains(diagnosticSessionText, "message.Contains(\"Flashback buffer not active\", StringComparison.OrdinalIgnoreCase)");
        AssertContains(diagnosticSessionText, "internal static async Task<JsonElement?> WaitForFlashbackPlaybackBoundaryCrossAsync(");
        AssertContains(diagnosticSessionText, "internal static async Task<JsonElement?> WaitForFlashbackPlaybackStateAsync(");
        AssertContains(diagnosticSessionText, "actions.Add(\n            \"flashback segment playback observed \"");
        AssertDoesNotContain(diagnosticSessionText, "flashback segment playback: excessive late frames");
        AssertContains(diagnosticSessionText, "var diagnosticHealthObservation = BuildSessionDiagnosticHealthObservation(");
        AssertContains(diagnosticSessionText, "BuildSourceCadenceSessionMetrics(samples, lastSnapshot)");
        AssertContains(diagnosticSessionText, "var sourceReaderFramesDroppedDelta = GetCounterDelta(lastSnapshot, initialSnapshot, \"MfSourceReaderFramesDropped\")");
        AssertContains(diagnosticSessionText, "var videoIngestErrorsDelta = GetCounterDelta(lastSnapshot, initialSnapshot, \"VideoIngestErrorCount\")");
        AssertContains(diagnosticSessionText, "var sparseSourceCaptureCadenceWarning =");
        AssertContains(diagnosticSessionText, "IsSparseSourceCaptureCadenceWarningRun(");
        AssertContains(diagnosticSessionText, "internal static bool IsSparseSourceCaptureCadenceWarningRun(");
        AssertContains(diagnosticSessionText, "var toleratesFlashbackForceRotateDrainWarning =");
        AssertContains(diagnosticSessionText, "IsFlashbackForceRotateDrainDiagnosticHealthObservation(diagnosticHealthObservation)");
        AssertContains(diagnosticSessionText, "flashback force-rotate drain warning tolerated for flashback scenario");
        AssertContains(diagnosticSessionText, "internal static bool IsFlashbackForceRotateDrainDiagnosticHealthObservation(");
        AssertContains(diagnosticSessionText, "lastReject=force_rotate_draining");
        AssertContains(diagnosticSessionText, "sourceReaderFramesDroppedDelta > 0");
        AssertContains(diagnosticSessionText, "videoIngestErrorsDelta > 0");
        AssertContains(diagnosticSessionText, "var allowedSparseEvents = Math.Max(1, (long)Math.Ceiling(Math.Max(1, durationSeconds) / 180.0));");
        AssertContains(diagnosticSessionText, "FlashbackDiagnosticWarmupFraction");
        AssertContains(diagnosticSessionText, "FlashbackDiagnosticMaxWarmupMs");
        AssertContains(diagnosticSessionText, "private static DiagnosticHealthObservation BuildWorstDiagnosticHealthObservationAfterOffset(");
        AssertContains(diagnosticSessionText, "diagnosticHealthSucceeded &&");
        AssertContains(diagnosticSessionText, "var toleratesSourceSignalHealthWarning =");
        AssertContains(diagnosticSessionText, "scenarioPlan.ToleratesSourceSignalHealthWarning");
        AssertContains(diagnosticSessionText, "IsSourceSignalDiagnosticHealthObservation(diagnosticHealthObservation)");
        AssertContains(diagnosticSessionText, "diagnostic health source-signal warning tolerated for export reliability scenario");
        AssertContains(diagnosticSessionText, "IsPreviewSchedulerDiagnosticHealthObservation(diagnosticHealthObservation)");
        AssertContains(diagnosticSessionText, "diagnostic health preview scheduler transition warning tolerated for preview-cycle scenario");
        AssertContains(diagnosticSessionText, "var flashbackWarningsSucceeded = !isFlashbackScenario ||");
        AssertContains(diagnosticSessionText, "IsToleratedFlashbackScenarioWarning(");
        AssertContains(diagnosticSessionText, "flashbackWarningsSucceeded,");
        AssertContains(diagnosticScenariosText, "internal static string HelpList { get; } = string.Join(\"|\", All);");
        AssertContains(diagnosticScenariosText, "All.Contains(normalized, StringComparer.Ordinal)");

        var ssctlProgramText = ReadRepoFile("tools/ssctl/Program.cs")
            .Replace("\r\n", "\n");
        var ssctlCommandHandlersText = ReadRepoFile("tools/ssctl/CommandHandlers.cs")
            .Replace("\r\n", "\n");
        var mcpDiagnosticSessionText = ReadRepoFile("tools/McpServer/Tools/DiagnosticSessionTools.cs")
            .Replace("\r\n", "\n");
        AssertContains(ssctlProgramText, "DiagnosticSessionScenarios.HelpList");
        AssertContains(ssctlCommandHandlersText, "DiagnosticSessionScenarios.HelpList");
        AssertContains(mcpDiagnosticSessionText, "flashback-export-playback");
        AssertContains(mcpDiagnosticSessionText, "flashback-playback");
        AssertContains(mcpDiagnosticSessionText, "flashback-segment-playback");
        AssertContains(mcpDiagnosticSessionText, "flashback-encoder-cycle");
        AssertContains(mcpDiagnosticSessionText, "flashback-range-export");
        AssertContains(mcpDiagnosticSessionText, "flashback-range-export-audio-switch");
        AssertContains(mcpDiagnosticSessionText, "flashback-disable-during-export");
        AssertContains(mcpDiagnosticSessionText, "flashback-rotated-export");
        AssertContains(mcpDiagnosticSessionText, "flashback-preview-cycle");
        AssertContains(mcpDiagnosticSessionText, "flashback-playback-preview-cycle");
        AssertContains(mcpDiagnosticSessionText, "flashback-recording-preview-cycle");
        AssertContains(mcpDiagnosticSessionText, "flashback-recording-settings-deferred");
        AssertContains(mcpDiagnosticSessionText, "flashback-recording-export-rejected");

        return Task.CompletedTask;
    }

    private static Task AutomationProtocol_SetRecordingUsesRecordingSizedTimeout()
    {
        var protocolText = ReadRepoFile("Sussudio.Automation.Contracts/AutomationPipeProtocol.cs")
            .Replace("\r\n", "\n");
        var clientText = ReadRepoFile("tools/AutomationClient/Program.cs")
            .Replace("\r\n", "\n");

        AssertContains(protocolText, "public const int DefaultResponseTimeoutMs = 15000;");
        AssertContains(protocolText, "public const int ExtendedResponseTimeoutMs = 60000;");
        AssertContains(protocolText, "public const int RecordingResponseTimeoutMs = 150000;");
        AssertContains(protocolText, "public const int FlashbackMutationResponseTimeoutMs = 305000;");
        AssertContains(protocolText, "commandName = ResolveCanonicalCommandName(commandName);");
        AssertContains(protocolText, "AutomationCommandCatalog.TryGet(commandName, out var metadata)");
        AssertContains(protocolText, "? metadata.ResponseTimeoutMs");
        var catalogText = ReadRepoFile("Sussudio.Automation.Contracts/AutomationCommandCatalog.cs")
            .Replace("\r\n", "\n");
        AssertContains(catalogText, "AutomationCommandKind.SetRecordingEnabled");
        AssertContains(catalogText, "AutomationPipeProtocol.RecordingResponseTimeoutMs");
        AssertContains(catalogText, "AutomationCommandKind.FlashbackExport");
        AssertContains(catalogText, "AutomationPipeProtocol.FlashbackMutationResponseTimeoutMs");
        AssertDoesNotContain(protocolText, "AlignResponseTimeoutWithServerRequest");
        AssertContains(clientText, "AutomationPipeProtocol.TryGetCommandName(commandValue, out var canonicalCommandName)");
        AssertContains(clientText, "AutomationPipeProtocol.GetDefaultResponseTimeout(timeoutCommandName)");
        AssertContains(clientText, "public int? ResponseTimeoutMs { get; set; }");
        var pipeClientText = ReadRepoFile("tools/Common/AutomationPipeClient.cs")
            .Replace("\r\n", "\n");
        AssertDoesNotContain(pipeClientText, "AlignResponseTimeoutWithServerRequest");

        var protocolType = RequireType("Sussudio.Tools.AutomationPipeProtocol");
        var getDefaultResponseTimeout = protocolType.GetMethod(
            "GetDefaultResponseTimeout",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("AutomationPipeProtocol.GetDefaultResponseTimeout not found.");

        foreach (var acceptedName in new[] { "SetRecordingEnabled", "setrecordingenabled", "set-recording-enabled", "17" })
        {
            var timeoutMs = (int)getDefaultResponseTimeout.Invoke(null, new object[] { acceptedName })!;
            AssertEqual(150000, timeoutMs, $"SetRecordingEnabled timeout for '{acceptedName}'");
        }

        var defaultTimeoutMs = (int)getDefaultResponseTimeout.Invoke(null, new object[] { "GetSnapshot" })!;
        AssertEqual(15000, defaultTimeoutMs, "GetSnapshot timeout remains bounded");

        var flashbackExportTimeoutMs = (int)getDefaultResponseTimeout.Invoke(null, new object[] { "FlashbackExport" })!;
        AssertEqual(305000, flashbackExportTimeoutMs, "FlashbackExport uses flashback mutation timeout");

        foreach (var acceptedName in new[] { "SetFlashbackEnabled", "set-flashback-enabled", "RestartFlashback" })
        {
            var timeoutMs = (int)getDefaultResponseTimeout.Invoke(null, new object[] { acceptedName })!;
            AssertEqual(305000, timeoutMs, $"Flashback mutation timeout for '{acceptedName}' outlives server cancellation");
        }

        return Task.CompletedTask;
    }

    private static Task MainViewModelCapture_RecordingFailuresPropagateToCallers()
    {
        var recordingLifecycleText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.RecordingLifecycle.cs")
            .Replace("\r\n", "\n");

        AssertContains(recordingLifecycleText, "Logger.LogException(ex);");
        AssertContains(recordingLifecycleText, "IsRecording = _sessionCoordinator.Snapshot.IsRecording;");
        AssertContains(
            recordingLifecycleText,
            "catch (OperationCanceledException ex)\n            {\n                transitionError = ex;\n                Logger.Log($\"Recording transition wait canceled: {ex.Message}\");\n            }");
        AssertContains(
            recordingLifecycleText,
            "if (transitionError is OperationCanceledException transitionCanceled && inFlightTarget == (enabled ? 1 : 0))\n            {\n                throw transitionCanceled;\n            }");
        AssertContains(
            recordingLifecycleText,
            "catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)\n        {\n            IsRecording = _sessionCoordinator.Snapshot.IsRecording;\n            StatusText = \"Recording start canceled\";\n            throw;\n        }");
        AssertContains(
            recordingLifecycleText,
            "catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)\n        {\n            IsRecording = _sessionCoordinator.Snapshot.IsRecording;\n            StatusText = \"Stop recording canceled\";\n            throw;\n        }");
        AssertContains(recordingLifecycleText, "StatusText = $\"Recording failed: {ex.Message}\";");
        AssertContains(recordingLifecycleText, "StatusText = $\"Stop recording failed: {ex.Message}\";");
        AssertContains(recordingLifecycleText, "throw;");

        return Task.CompletedTask;
    }

    private static Task MainWindowClose_CancelsCloseUntilRecordingStopCompletes()
    {
        var windowCtorText = ReadRepoFile("Sussudio/MainWindow.xaml.cs")
            .Replace("\r\n", "\n");
        var closeLifecycleText = ReadRepoFile("Sussudio/MainWindow.CloseLifecycle.cs")
            .Replace("\r\n", "\n");

        AssertContains(windowCtorText, "appWindow.Closing += MainWindow_Closing;");
        AssertContains(closeLifecycleText, "args.Cancel = true;");
        AssertContains(closeLifecycleText, "TryStopRecordingBeforeCloseAsync");
        AssertContains(closeLifecycleText, "const int StopBudgetMs = 120_000;");
        AssertContains(closeLifecycleText, "close cancelled to protect recording");
        AssertContains(closeLifecycleText, "RequestWindowClose();");
        AssertContains(closeLifecycleText, "GetWindowCloseCompletionTask(cancellationToken)");
        AssertContains(closeLifecycleText, "CompleteWindowCloseRequest(new InvalidOperationException(ViewModel.StatusText));");
        AssertContains(closeLifecycleText, "CompleteWindowCloseRequest();");
        AssertDoesNotContain(closeLifecycleText, "MP4 may be truncated.");

        return Task.CompletedTask;
    }

    private static Task ExternalProcessProbes_UseBoundedProcessSupervisor()
    {
        var ffmpegText = ReadRepoFile("Sussudio/Services/Runtime/FfmpegRuntimeLocator.cs")
            .Replace("\r\n", "\n");
        var hdrText = ReadRepoFile("Sussudio/Services/Recording/HdrValidationRunner.cs")
            .Replace("\r\n", "\n");

        AssertContains(ffmpegText, "private const int ProbeTimeoutMs = 10_000;");
        AssertContains(ffmpegText, "new ProcessSupervisor().RunAsync");
        AssertContains(ffmpegText, "TimeoutMs = ProbeTimeoutMs");
        AssertContains(ffmpegText, "if (!result.Started || result.TimedOut || result.ExitCode != 0)");
        AssertContains(ffmpegText, "return result.Started && !result.TimedOut && result.ExitCode == 0;");
        AssertContains(hdrText, "private const int ValidationTimeoutMs = 30_000;");
        AssertContains(hdrText, "new ProcessSupervisor().RunAsync");
        AssertContains(hdrText, "validator-timeout");

        return Task.CompletedTask;
    }

    private static Task RecordingStop_PropagatesUnifiedVideoStopFailure()
    {
        var captureServiceText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingFinalizeRecord.cs")
            .Replace("\r\n", "\n");

        AssertContains(captureServiceText, "Unified video recording stop failed");
        AssertContains(captureServiceText, "FinalizeResult.Failure(fallbackOutputPath, $\"Unified video recording stop failed: {ex.Message}\");");
        // Fix #12: sink dispatch became a ternary so the emergency flag can route to libAvSink.StopAsync(emergency, ct).
        AssertContains(captureServiceText, "var sinkResult = libAvSink != null");
        AssertContains(captureServiceText, "? await libAvSink.StopAsync(emergency, cancellationToken).ConfigureAwait(false)");
        AssertContains(captureServiceText, ": await sink.StopAsync(cancellationToken).ConfigureAwait(false);");
        AssertContains(captureServiceText, "if (result.Succeeded)\n                {\n                    result = sinkResult;");

        return Task.CompletedTask;
    }

    private static Task MainWindowScreenshot_CompletesOnDispatcherFailureAndCancellation()
    {
        var windowText = ReadRepoFile("Sussudio/MainWindow.Screenshot.cs")
            .Replace("\r\n", "\n");
        var controllerText = ReadRepoFile("Sussudio/Controllers/WindowScreenshotController.cs")
            .Replace("\r\n", "\n");
        var method = ExtractTextBetween(
            controllerText,
            "public Task<WindowScreenshotResult> CaptureAsync",
            "    private WindowScreenshotResult CaptureCore");

        AssertContains(windowText, "public Task<WindowScreenshotResult> CaptureWindowScreenshotAsync");
        AssertContains(windowText, "=> _windowScreenshotController.CaptureAsync(outputPath, cancellationToken);");
        AssertContains(method, "if (cancellationToken.IsCancellationRequested)");
        AssertContains(method, "Message = \"Screenshot canceled.\"");
        AssertContains(method, "CancellationTokenRegistration cancellationRegistration = default;");
        AssertContains(method, "cancellationToken.Register(() =>");
        AssertContains(method, "_ = completion.Task.ContinueWith(");
        AssertContains(method, "cancellationRegistration.Dispose()");
        AssertContains(method, "if (!_dispatcherQueue.TryEnqueue(() =>");
        AssertContains(method, "Message = \"Failed to enqueue screenshot capture on the UI thread.\"");

        return Task.CompletedTask;
    }

    private static Task PreviewStopCompatibilityOverloads_ArePreserved()
    {
        var captureServiceText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.cs")
            .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.PreviewLifecycle.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.Audio.cs")
                .Replace("\r\n", "\n");
        var coordinatorText = ReadRepoFile("Sussudio/Services/Capture/CaptureSessionCoordinator.cs")
            .Replace("\r\n", "\n");
        var viewModelText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.Capture.cs")
            .Replace("\r\n", "\n");

        AssertContains(captureServiceText, "public Task StopVideoPreviewAsync(CancellationToken cancellationToken = default)");
        AssertContains(captureServiceText, "public Task StopVideoPreviewWithTeardownAsync(CancellationToken cancellationToken = default)");
        AssertContains(captureServiceText, "public Task StopAudioPreviewAsync(CancellationToken cancellationToken = default)");
        AssertContains(captureServiceText, "public Task StopAudioPreviewWithTeardownAsync(CancellationToken cancellationToken = default)");
        AssertDoesNotContain(captureServiceText, "public Task StopVideoPreviewAsync(bool");
        AssertDoesNotContain(captureServiceText, "public Task StopAudioPreviewAsync(bool");
        AssertContains(coordinatorText, "public Task StopVideoPreviewAsync(CancellationToken cancellationToken = default)");
        AssertContains(coordinatorText, "public Task StopVideoPreviewWithTeardownAsync(CancellationToken cancellationToken = default)");
        AssertContains(coordinatorText, "public Task StopAudioPreviewAsync(CancellationToken cancellationToken = default)");
        AssertContains(coordinatorText, "public Task StopAudioPreviewWithTeardownAsync(CancellationToken cancellationToken = default)");
        AssertDoesNotContain(coordinatorText, "public Task StopVideoPreviewAsync(bool");
        AssertDoesNotContain(coordinatorText, "public Task StopAudioPreviewAsync(bool");
        AssertContains(viewModelText, "public Task StopPreviewAsync()\n        => StopPreviewAsync(userInitiated: true, teardownPipeline: false, CancellationToken.None);");
        AssertContains(viewModelText, "public Task StopPreviewAsync(bool userInitiated)\n        => StopPreviewAsync(userInitiated, teardownPipeline: false, CancellationToken.None);");

        return Task.CompletedTask;
    }

    private static Task PreviewStopApiSurface_HasNoDefaultLiteralAmbiguity()
    {
        AssertPreviewStopSurface("Sussudio.Services.Capture.CaptureService");
        AssertPreviewStopSurface("Sussudio.Services.Capture.CaptureSessionCoordinator");
        return Task.CompletedTask;
    }

    private static void AssertPreviewStopSurface(string typeName)
    {
        var type = RequireType(typeName);
        AssertStopSurface(type, "StopVideoPreviewAsync", "StopVideoPreviewWithTeardownAsync");
        AssertStopSurface(type, "StopAudioPreviewAsync", "StopAudioPreviewWithTeardownAsync");
    }

    private static void AssertStopSurface(Type type, string stopMethodName, string teardownMethodName)
    {
        var publicInstance = BindingFlags.Instance | BindingFlags.Public;
        var oneParameterStopOverloads = type.GetMethods(publicInstance)
            .Where(method => method.Name == stopMethodName && method.GetParameters().Length == 1)
            .ToArray();

        AssertEqual(1, oneParameterStopOverloads.Length, $"{type.FullName}.{stopMethodName} one-parameter overload count");
        AssertEqual(
            typeof(CancellationToken).FullName,
            oneParameterStopOverloads[0].GetParameters()[0].ParameterType.FullName,
            $"{type.FullName}.{stopMethodName} single parameter");

        var boolFirstParameterOverloads = type.GetMethods(publicInstance)
            .Where(method =>
            {
                if (method.Name != stopMethodName)
                {
                    return false;
                }

                var parameters = method.GetParameters();
                return parameters.Length > 0 && parameters[0].ParameterType == typeof(bool);
            })
            .ToArray();
        AssertEqual(0, boolFirstParameterOverloads.Length, $"{type.FullName}.{stopMethodName} bool-first overload count");

        var teardownMethod = type.GetMethod(teardownMethodName, publicInstance, binder: null, types: new[] { typeof(CancellationToken) }, modifiers: null);
        AssertNotNull(teardownMethod, $"{type.FullName}.{teardownMethodName}(CancellationToken)");
    }

    private static Task EmergencyRecordingStop_DoesNotDispatchBackToBlockedUiThread()
    {
        var appText = ReadRepoFile("Sussudio/App.xaml.cs")
            .Replace("\r\n", "\n");
        var captureText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.Capture.cs")
            .Replace("\r\n", "\n");
        var recordingLifecycleText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.RecordingLifecycle.cs")
            .Replace("\r\n", "\n");

        AssertContains(recordingLifecycleText, "internal Task StopRecordingForEmergencyAsync");
        // Fix #12: emergency stop now routes through the coordinator's emergency-flagged path
        // so LibAvRecordingSink applies EmergencyStopTimeoutMs (5s) instead of StopTimeoutMs (30s).
        AssertContains(recordingLifecycleText, "=> _sessionCoordinator.StopRecordingForEmergencyAsync(cancellationToken);");
        AssertContains(appText, "var task = viewModel.StopRecordingForEmergencyAsync();");
        AssertContains(appText, "if (e.IsTerminating || !recoverable)");
        AssertDoesNotContain(appText, "Task.Run(async () =>");
        AssertDoesNotContain(appText, "StopRecordingAndWaitAsync().ConfigureAwait(false)");
        AssertDoesNotContain(appText, "viewModel == null || !viewModel.IsRecording");
        AssertDoesNotContain(recordingLifecycleText, "if (!IsRecording)");
        AssertDoesNotContain(captureText, "internal Task StopRecordingForEmergencyAsync");

        return Task.CompletedTask;
    }

    private static Task FlashbackBufferManager_CleansStaleSessionDirectories()
    {
        var bufferText = ReadFlashbackBufferManagerSource();
        var cleanupText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackStartupCacheCleanup.cs")
            .Replace("\r\n", "\n");
        var scannerText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackSessionRecoveryScanner.cs")
            .Replace("\r\n", "\n");
        var playbackSegmentEdgesText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.PlaybackSegmentEdges.cs")
            .Replace("\r\n", "\n");

        // Constants/definitions now live in the extracted helper classes
        AssertContains(cleanupText, "internal static readonly TimeSpan StaleSessionMinAge = TimeSpan.FromHours(12);");
        AssertContains(cleanupText, "private const int MaxStaleSessionDirectoryScansPerInit = 64;");
        AssertContains(cleanupText, "private const int MaxStartupCacheSessionDirectoryScansPerInit = 256;");
        AssertContains(cleanupText, "private const int MaxStartupCacheSessionDirectoriesPerInit = 32;");
        AssertContains(cleanupText, "private const long StartupCacheBudgetMultiplier = 2;");
        AssertContains(cleanupText, "private const int MaxStaleRootSegmentFileScansPerInit = 512;");

        // Call sites remain in the FlashbackBufferManager partial family (now qualified)
        AssertContains(bufferText, "FlashbackStartupCacheCleanup.CleanupStaleRootSegmentFiles(tempDirectory);");
        AssertContains(bufferText, "FlashbackStartupCacheCleanup.CleanupStaleSessionDirectories(tempDirectory, sessionDirectory);");
        AssertContains(bufferText, "var cacheCleanup = FlashbackStartupCacheCleanup.CleanupSessionCacheBudget(");
        AssertContains(bufferText, "FlashbackStartupCacheCleanup.CalculateStartupTempCacheBudgetBytes(_options.MaxDiskBytes));");
        AssertContains(bufferText, "var sessionDirectory = FlashbackSessionRecoveryScanner.BuildSessionDirectory(tempDirectory, sessionId);");

        // Session directory helper definitions now in FlashbackSessionRecoveryScanner
        AssertContains(scannerText, "internal static string BuildSessionDirectory(string tempDirectory, string sessionId)");
        AssertContains(scannerText, "Session id must be a simple file-name component.");
        AssertContains(scannerText, "Session id must resolve inside the flashback temp directory.");
        AssertContains(scannerText, "internal static string NormalizeSegmentExtension(string extension)");
        AssertContains(scannerText, "Flashback segment extension must be .ts or .mp4.");
        AssertContains(scannerText, "internal static bool IsPathUnderDirectory(string fullPath, string fullDirectoryRoot)");
        AssertContains(scannerText, "internal static bool IsReparsePoint(FileSystemInfo info)");
        AssertContains(scannerText, "internal static bool IsPlausibleFlashbackSessionDirectoryName(string name)");

        // NormalizeSegmentExtension call site remains in the FlashbackBufferManager partial family (now qualified)
        AssertContains(bufferText, "var normalizedExtension = FlashbackSessionRecoveryScanner.NormalizeSegmentExtension(extension);");

        // TempDriveAvailableFreeBytes property delegates to the extracted class
        AssertContains(bufferText, "public long TempDriveAvailableFreeBytes => FlashbackStartupCacheCleanup.TryGetTempDriveAvailableFreeBytes(_options.TempDirectory);");

        // Log strings remain in the cleanup class
        AssertContains(cleanupText, "FLASHBACK_STALE_SESSION_SKIP reason=reparse_point");
        AssertContains(cleanupText, "FLASHBACK_STALE_SESSION_SKIP reason=unrecognized_empty_dir");
        AssertContains(cleanupText, "FLASHBACK_CACHE_BUDGET_SKIP reason=outside_temp");
        AssertContains(cleanupText, "FLASHBACK_SESSION_STATS_SKIP reason=reparse_point");
        AssertContains(cleanupText, "if (string.Equals(fullPath, currentFullPath, StringComparison.OrdinalIgnoreCase))");
        AssertContains(cleanupText, "FLASHBACK_CACHE_BUDGET_PRESERVE_SKIP");
        AssertContains(cleanupText, "FLASHBACK_CACHE_BUDGET_CLEANUP");
        AssertContains(cleanupText, "info.EnumerateFiles(\"fb_*\", SearchOption.TopDirectoryOnly)");
        AssertContains(cleanupText, "Directory.EnumerateFiles(tempDirectory, \"fb_*\", SearchOption.TopDirectoryOnly)");
        AssertContains(cleanupText, "Directory.Delete(fullPath, recursive: true);");

        // Segment lookup helpers remain in the FlashbackBufferManager partial family
        AssertContains(bufferText, "if (IsSameSegmentPath(_activeSegmentPath, currentPath))\n                return _activeSegmentPath != null && File.Exists(_activeSegmentPath) ? _activeSegmentPath : null;");
        AssertContains(bufferText, "return GetOldestExistingSegmentPath()\n                ?? (_activeSegmentPath != null && File.Exists(_activeSegmentPath) ? _activeSegmentPath : null);");
        AssertContains(bufferText, "public TimeSpan? GetSegmentStartPts(string path)");
        AssertContains(playbackSegmentEdgesText, "var nextSegmentStart = _bufferManager.GetSegmentStartPts(nextFile);");
        AssertContains(playbackSegmentEdgesText, "if (nextSegmentStart.HasValue && segSwitchTarget < nextSegmentStart.Value)");
        AssertContains(playbackSegmentEdgesText, "var currentSegmentStart = _bufferManager.GetSegmentStartPts(currentOpenFilePath);");
        AssertContains(playbackSegmentEdgesText, "if (currentSegmentStart.HasValue && resumeTarget < currentSegmentStart.Value)");

        return Task.CompletedTask;
    }
}

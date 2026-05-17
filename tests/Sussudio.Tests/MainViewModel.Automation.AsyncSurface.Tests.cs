using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

static partial class Program
{
    private static Task MainViewModelAutomation_UsesAsyncFlashbackAndProbeSurface()
    {
        var automationInterfaceType = RequireType("Sussudio.Services.Automation.IAutomationViewModel");
        AssertEqual(
            false,
            automationInterfaceType.GetProperty("IsMicrophoneEnabled") != null,
            "IAutomationViewModel sync microphone setter");
        AssertTaskReturningMethod(automationInterfaceType, "SetMicrophoneEnabledAsync", resultType: null);
        AssertTaskReturningMethod(automationInterfaceType, "SetHdrEnabledAsync", resultType: null);
        AssertTaskReturningMethod(automationInterfaceType, "SetTrueHdrPreviewEnabledAsync", resultType: null);
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
        var automationAudioText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.AutomationAudio.cs")
            .Replace("\r\n", "\n");
        var automationPreviewText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.AutomationPreview.cs")
            .Replace("\r\n", "\n");
        var automationHdrText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.AutomationHdr.cs")
            .Replace("\r\n", "\n");
        var automationFlashbackText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.AutomationFlashback.cs")
            .Replace("\r\n", "\n");
        var recordingLifecycleText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.RecordingLifecycle.cs")
            .Replace("\r\n", "\n");
        var automationUiText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.AutomationUi.cs")
            .Replace("\r\n", "\n");
        var automationStatsUiText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.AutomationStatsUi.cs")
            .Replace("\r\n", "\n");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.Automation.cs")),
            "MainViewModel automation catch-all partial");
        var automationText = automationAudioText
            + "\n" + automationPreviewText
            + "\n" + automationHdrText
            + "\n" + automationFlashbackText
            + "\n" + recordingLifecycleText
            + "\n" + automationStatsUiText
            + "\n" + ReadRepoFile("Sussudio/ViewModels/MainViewModel.AutomationDeviceSelection.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/ViewModels/MainViewModel.AutomationAudioInputSelection.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/ViewModels/MainViewModel.AutomationRecordingSettings.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/ViewModels/MainViewModel.FlashbackExport.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/ViewModels/MainViewModel.FlashbackExportOperation.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/ViewModels/MainViewModel.FlashbackExportAutomation.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/ViewModels/MainViewModel.FlashbackPlayback.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/ViewModels/MainViewModel.FlashbackPlaybackCommands.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/ViewModels/MainViewModel.AutomationSnapshots.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/ViewModels/MainViewModel.ViewModelRuntimeSnapshot.cs")
                .Replace("\r\n", "\n");
        var uiDispatchControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelUiDispatchController.cs")
            .Replace("\r\n", "\n");
        var viewModelText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs")
            .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/ViewModels/MainViewModel.State.cs")
            .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/ViewModels/MainViewModel.Dispatching.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/ViewModels/MainViewModel.AudioPropertyChanges.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/ViewModels/MainViewModel.AudioInputPropertyChanges.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/ViewModels/MainViewModel.MicrophonePropertyChanges.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/ViewModels/MainViewModel.DeviceAudioRequests.cs")
                .Replace("\r\n", "\n")
            + "\n" + uiDispatchControllerText;
        var coordinatorText = ReadCaptureSessionCoordinatorSource();
        var pipeServerText = (
            ReadRepoFile("Sussudio/Services/Automation/NamedPipeAutomationServer.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Automation/NamedPipeAutomationServer.Connections.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Automation/NamedPipeAutomationServer.Responses.cs"))
            .Replace("\r\n", "\n");
        var captureText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.Capture.cs")
            .Replace("\r\n", "\n");
        var deviceManagementText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.DeviceManagement.cs")
            .Replace("\r\n", "\n");
        var captureServiceText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.cs")
            .Replace("\r\n", "\n")
            + "\n" + ReadCaptureServiceAudioSource()
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
        AssertContains(dispatcherText, "if (positionMs.HasValue &&");
        AssertContains(dispatcherText, "(!double.IsFinite(positionMs.Value) ||");
        AssertContains(dispatcherText, "positionMs.Value < 0 ||");
        AssertContains(dispatcherText, "positionMs.Value > TimeSpan.MaxValue.TotalMilliseconds))");
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
        AssertContains(dispatcherText, "vm.SetHdrEnabledAsync(v, ct)");
        AssertContains(dispatcherText, "vm.SetTrueHdrPreviewEnabledAsync(v, ct)");
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
        AssertContains(automationText, "public Task SetHdrEnabledAsync(bool enabled, CancellationToken cancellationToken = default)");
        AssertContains(automationText, "public Task SetTrueHdrPreviewEnabledAsync(bool enabled, CancellationToken cancellationToken = default)");
        AssertContains(automationText, "InvokeOnUiThreadAsync(() => ExecuteFlashbackAction(action, position), cancellationToken)");
        AssertContains(automationText, "=> FromSynchronousSnapshot(ProbeVideoSource, cancellationToken);");
        AssertContains(automationText, "=> FromSynchronousSnapshot(ProbePreviewColor, cancellationToken);");
        AssertContains(automationText, "=> FromSynchronousSnapshot(GetFlashbackSegments, cancellationToken);");
        AssertContains(automationText, "public Task<CaptureRuntimeSnapshot> GetCaptureRuntimeSnapshotAsync(CancellationToken cancellationToken = default)\n        => FromSynchronousSnapshot(_captureService.GetRuntimeSnapshot, cancellationToken);");
        AssertContains(automationText, "InvokeOnUiThreadAsync(BuildCaptureSettings, cancellationToken)");
        AssertContains(automationText, "await _sessionCoordinator.RestartFlashbackAsync(settings, cancellationToken).ConfigureAwait(false)");
        AssertContains(automationText, "_flashbackBitrateSamples.Clear();\n                return true;\n            },\n            cancellationToken).ConfigureAwait(false);");
        AssertContains(automationAudioText, "public Task SetAudioEnabledAsync(bool enabled, CancellationToken cancellationToken = default)");
        AssertContains(automationAudioText, "public Task SetAudioPreviewEnabledAsync(bool enabled, CancellationToken cancellationToken = default)");
        AssertContains(automationAudioText, "public Task SetPreviewVolumeAsync(double previewVolumePercent, CancellationToken cancellationToken = default)");
        AssertContains(automationAudioText, "public Task SetDeviceAudioModeAsync(string mode, CancellationToken cancellationToken = default)");
        AssertContains(automationAudioText, "public Task SetAnalogAudioGainAsync(double gainPercent, CancellationToken cancellationToken = default)");
        AssertContains(automationAudioText, "public Task SetMicrophoneEnabledAsync(bool enabled, CancellationToken cancellationToken = default)");
        AssertContains(automationAudioText, "private async Task SetMicrophoneEnabledAutomationAsync(bool enabled, CancellationToken cancellationToken)");
        AssertDoesNotContain(automationUiText, "public Task SetPreviewVolumeAsync");
        var automationRecordingSettingsText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.AutomationRecordingSettings.cs")
            .Replace("\r\n", "\n");
        AssertContains(automationRecordingSettingsText, "public async Task SetRecordingFormatAsync(string format, CancellationToken cancellationToken = default)");
        AssertContains(automationRecordingSettingsText, "RecordingSettingsSelectionPolicy.IsHdrCompatible(matched)");
        AssertContains(automationRecordingSettingsText, "public async Task SetQualityAsync(string quality, CancellationToken cancellationToken = default)");
        AssertContains(automationRecordingSettingsText, "RecordingSettingsSelectionPolicy.ParseVideoQuality(SelectedQuality)");
        AssertContains(automationRecordingSettingsText, "public async Task SetSplitEncodeModeAsync(string splitEncodeMode, CancellationToken cancellationToken = default)");
        AssertContains(automationRecordingSettingsText, "splitEncodeMode: settings.SplitEncodeMode");
        AssertContains(automationRecordingSettingsText, "public async Task SetCustomBitrateAsync(double bitrateMbps, CancellationToken cancellationToken = default)");
        AssertContains(automationRecordingSettingsText, "CustomBitrateMbps = RecordingSettingsSelectionPolicy.ClampCustomBitrateMbps(bitrateMbps);");
        AssertContains(automationRecordingSettingsText, "public async Task SetPresetAsync(string preset, CancellationToken cancellationToken = default)");
        AssertContains(automationRecordingSettingsText, "SelectedPreset = matched;");
        AssertContains(automationRecordingSettingsText, "public Task SetOutputPathAsync(string outputPath, CancellationToken cancellationToken = default)");
        AssertContains(automationRecordingSettingsText, "Directory.CreateDirectory(outputPath);");
        AssertContains(automationRecordingSettingsText, "OutputPath = outputPath;");
        foreach (var stalePath in new[]
        {
            "MainViewModel.AutomationRecordingFormat.cs",
            "MainViewModel.AutomationRecordingQuality.cs",
            "MainViewModel.AutomationSplitEncodeMode.cs",
            "MainViewModel.AutomationCustomBitrate.cs",
            "MainViewModel.AutomationEncoderPreset.cs",
            "MainViewModel.AutomationOutputPath.cs"
        })
        {
            AssertEqual(
                false,
                File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", stalePath)),
                $"stale recording settings automation partial {stalePath}");
        }
        AssertContains(automationText, "=> InvokeOnUiThreadAsync(() => RefreshDevicesAsync(cancellationToken), cancellationToken);");
        AssertContains(automationText, "await StartPreviewAsync(userInitiated: true, cancellationToken);");
        AssertContains(automationText, "await StopPreviewAsync(userInitiated: true, teardownPipeline: false, cancellationToken);");
        AssertContains(captureText, "private Task InitializeDeviceAsync(CancellationToken cancellationToken = default)");
        AssertContains(captureText, "=> _previewLifecycleController.InitializeDeviceAsync(cancellationToken);");
        AssertContains(captureText, "public Task StartPreviewAsync(bool userInitiated = true, CancellationToken cancellationToken = default)");
        AssertContains(captureText, "public Task StopPreviewAsync(bool userInitiated, bool teardownPipeline, CancellationToken cancellationToken)");
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
}

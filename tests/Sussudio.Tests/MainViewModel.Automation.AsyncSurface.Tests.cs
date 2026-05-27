using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

static partial class Program
{
    internal static Task MainViewModelAutomation_UsesAsyncFlashbackAndProbeSurface()
    {
        var automationInterfaceType = RequireType("Sussudio.Services.Automation.IAutomationViewModel");
        var readinessPortType = RequireType("Sussudio.Services.Automation.IAutomationReadinessPort");
        var deviceSelectionPortType = RequireType("Sussudio.Services.Automation.IAutomationDeviceSelectionPort");
        var snapshotQueryPortType = RequireType("Sussudio.Services.Automation.IAutomationSnapshotQueryPort");
        var captureSettingsPortType = RequireType("Sussudio.Services.Automation.IAutomationCaptureSettingsPort");
        var audioPortType = RequireType("Sussudio.Services.Automation.IAutomationAudioPort");
        var previewRecordingPortType = RequireType("Sussudio.Services.Automation.IAutomationPreviewRecordingPort");
        var probePortType = RequireType("Sussudio.Services.Automation.IAutomationProbePort");
        AssertEqual(
            true,
            readinessPortType.IsAssignableFrom(automationInterfaceType),
            "IAutomationViewModel inherits readiness port");
        AssertEqual(
            true,
            deviceSelectionPortType.IsAssignableFrom(automationInterfaceType),
            "IAutomationViewModel inherits device-selection port");
        AssertEqual(
            true,
            snapshotQueryPortType.IsAssignableFrom(automationInterfaceType),
            "IAutomationViewModel inherits snapshot-query port");
        AssertEqual(
            true,
            captureSettingsPortType.IsAssignableFrom(automationInterfaceType),
            "IAutomationViewModel inherits capture-settings port");
        AssertEqual(
            true,
            audioPortType.IsAssignableFrom(automationInterfaceType),
            "IAutomationViewModel inherits audio port");
        AssertEqual(
            true,
            previewRecordingPortType.IsAssignableFrom(automationInterfaceType),
            "IAutomationViewModel inherits preview-recording port");
        AssertEqual(
            true,
            probePortType.IsAssignableFrom(automationInterfaceType),
            "IAutomationViewModel inherits probe port");
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
        var viewModelDispatchText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs")
            .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelUiDispatchController.cs")
                .Replace("\r\n", "\n");
        var flashbackSettingsText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.FlashbackState.cs")
            .Replace("\r\n", "\n");
        var flashbackExportText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.FlashbackState.cs")
            .Replace("\r\n", "\n");
        var flashbackExportOperationText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.FlashbackState.cs")
            .Replace("\r\n", "\n");
        var flashbackExportAutomationText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.FlashbackState.cs")
            .Replace("\r\n", "\n");
        var flashbackBufferStatusText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.FlashbackState.cs")
            .Replace("\r\n", "\n");
        var flashbackPlaybackCommandsText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.FlashbackState.cs")
            .Replace("\r\n", "\n");
        var automationFacadeText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.AutomationCommands.cs")
            .Replace("\r\n", "\n");
        var automationText = string.Join(
            "\n",
            flashbackSettingsText,
            flashbackExportText,
            flashbackExportOperationText,
            flashbackExportAutomationText,
            flashbackBufferStatusText,
            flashbackPlaybackCommandsText,
            automationFacadeText);

        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.Automation.cs")),
            "MainViewModel automation catch-all partial");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.AutomationPreview.cs")),
            "MainViewModel automation preview partial");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.AutomationHdr.cs")),
            "MainViewModel automation HDR partial");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.AutomationFlashback.cs")),
            "MainViewModel automation Flashback partial");

        AssertDoesNotContain(interfaceText, "bool FlashbackPlay();");
        AssertDoesNotContain(interfaceText, "bool FlashbackPause();");
        AssertDoesNotContain(interfaceText, "bool FlashbackGoLive();");
        AssertDoesNotContain(interfaceText, "bool FlashbackBeginScrub(TimeSpan position);");
        AssertDoesNotContain(interfaceText, "bool FlashbackEndScrub();");
        AssertDoesNotContain(interfaceText, "VideoSourceProbeResult ProbeVideoSource();");
        AssertDoesNotContain(interfaceText, "PreviewColorProbeResult ProbePreviewColor();");
        AssertContains(dispatcherText, "await _flashbackPort.ExecuteFlashbackActionAsync(action, position, cancellationToken).ConfigureAwait(false)");
        AssertContains(dispatcherText, "return CreateFlashbackActionRejectedResponse(");
        AssertContains(dispatcherText, "errorCode: \"flashback-action-failed\"");
        AssertContains(dispatcherText, "RequestedPositionMs = requestedPositionMs");
        AssertContains(dispatcherText, "LastCommandFailureUtcUnixMs = snapshot.FlashbackPlaybackLastCommandFailureUtcUnixMs");
        AssertContains(dispatcherText, "var useSelectionRange = GetBool(payload, \"useSelectionRange\") ?? false;");
        AssertContains(dispatcherText, "var force = GetBool(payload, \"force\") ?? false;");
        AssertContains(dispatcherText, "ExportFlashbackAutomationAsync(seconds, outputPath, useSelectionRange, force, cancellationToken)");
        AssertContains(dispatcherText, "CaptureService.ClassifyFlashbackExportFailureKind(exportResult.StatusMessage)");
        AssertContains(dispatcherText, "FailureKind = failureKind");
        AssertContains(dispatcherText, "Flashback positionMs must be finite, non-negative, and within TimeSpan range.");
        AssertContains(dispatcherText, "AutomationFlashbackAction.BeginScrub => RequireDouble(payload, \"positionMs\")");
        AssertContains(dispatcherText, "AutomationFlashbackAction.UpdateScrub => RequireDouble(payload, \"positionMs\")");
        AssertContains(dispatcherText, "AutomationFlashbackAction.EndScrub => GetDouble(payload, \"positionMs\")");
        AssertContains(dispatcherText, "private readonly IAutomationReadinessPort _readinessPort;");
        AssertContains(dispatcherText, "private readonly IAutomationDeviceSelectionPort _deviceSelectionPort;");
        AssertContains(dispatcherText, "private readonly IAutomationSnapshotQueryPort _snapshotQueryPort;");
        AssertContains(dispatcherText, "private readonly IAutomationCaptureSettingsPort _captureSettingsPort;");
        AssertContains(dispatcherText, "private readonly IAutomationAudioPort _audioPort;");
        AssertContains(dispatcherText, "private readonly IAutomationPreviewRecordingPort _previewRecordingPort;");
        AssertContains(dispatcherText, "private readonly IAutomationUiPort _uiPort;");
        AssertContains(dispatcherText, "private readonly IAutomationFlashbackPort _flashbackPort;");
        AssertContains(dispatcherText, "private readonly IAutomationProbePort _probePort;");
        AssertDoesNotContain(dispatcherText, "private readonly IAutomationViewModel _viewModel;");
        AssertContains(interfaceText, "internal readonly record struct AutomationViewModelPorts(");
        AssertContains(interfaceText, "public static AutomationViewModelPorts From(IAutomationViewModel viewModel)");
        AssertContains(interfaceText, "ArgumentNullException.ThrowIfNull(viewModel);");
        AssertContains(dispatcherText, "internal AutomationCommandDispatcher(");
        AssertContains(dispatcherText, "AutomationViewModelPorts ports,");
        AssertDoesNotContain(dispatcherText, "public AutomationCommandDispatcher(\n        IAutomationViewModel viewModel,");
        AssertContains(dispatcherText, "_readinessPort = ports.Readiness");
        AssertContains(dispatcherText, "_snapshotQueryPort = ports.SnapshotQuery");
        AssertDoesNotContain(dispatcherText, "_readinessPort = viewModel;");
        AssertDoesNotContain(dispatcherText, "_snapshotQueryPort = viewModel;");
        AssertContains(dispatcherText, "_readinessPort.IsInitialized || _readinessPort.Devices.Count > 0");
        AssertContains(dispatcherText, "await deviceSelectionHandler.InvokeAsync(_deviceSelectionPort, payload, cancellationToken).ConfigureAwait(false);");
        AssertContains(dispatcherText, "await captureSettingsHandler.InvokeAsync(_captureSettingsPort, payload, cancellationToken).ConfigureAwait(false);");
        AssertContains(dispatcherText, "await audioHandler.InvokeAsync(_audioPort, payload, cancellationToken).ConfigureAwait(false);");
        AssertContains(dispatcherText, "await previewRecordingHandler.InvokeAsync(_previewRecordingPort, payload, cancellationToken).ConfigureAwait(false);");
        AssertContains(dispatcherText, "await _deviceSelectionPort.RefreshDevicesForAutomationAsync(cancellationToken).ConfigureAwait(false);");
        AssertContains(dispatcherText, "await _deviceSelectionPort.SelectDeviceAsync(deviceId, deviceName, cancellationToken).ConfigureAwait(false);");
        AssertContains(dispatcherText, "await _snapshotQueryPort.GetAutomationOptionsSnapshotAsync(cancellationToken).ConfigureAwait(false);");
        AssertContains(dispatcherText, "await _audioPort.SetMicrophoneEnabledAsync(enabled, cancellationToken).ConfigureAwait(false);");
        AssertContains(dispatcherText, "await _captureSettingsPort.SetMjpegDecoderCountAsync(decoderCount.Value, cancellationToken).ConfigureAwait(false);");
        AssertContains(dispatcherText, "await _previewRecordingPort.SetRecordingEnabledAsync(enabled, cancellationToken).ConfigureAwait(false);");
        AssertContains(dispatcherText, "await _probePort.ProbeVideoSourceAsync(cancellationToken).ConfigureAwait(false);");
        AssertContains(dispatcherText, "await _probePort.ProbePreviewColorAsync(cancellationToken).ConfigureAwait(false);");
        AssertContains(dispatcherText, "await _uiPort.SetStatsSectionVisibleAsync(section, visible, cancellationToken).ConfigureAwait(false);");
        AssertContains(dispatcherText, "await _snapshotQueryPort.GetAudioRampTraceSnapshotAsync(maxEntries, cancellationToken).ConfigureAwait(false);");
        AssertContains(dispatcherText, "await _flashbackPort.SetFlashbackEnabledAsync(enabled, cancellationToken).ConfigureAwait(false);");
        AssertContains(dispatcherText, "await _flashbackPort.GetFlashbackSegmentsAsync(cancellationToken).ConfigureAwait(false);");
        AssertContains(dispatcherText, "vm.SetHdrEnabledAsync(v, ct)");
        AssertContains(dispatcherText, "vm.SetTrueHdrPreviewEnabledAsync(v, ct)");
        AssertDoesNotContain(dispatcherText, "_viewModel.IsMicrophoneEnabled =");
        AssertContains(viewModelDispatchText, "registration.Dispose();\n                registration = default;\n\n                if (cancellationToken.IsCancellationRequested)");

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
        AssertContains(automationText, "FLASHBACK_EXPORT_PROGRESS_UI_ENQUEUE_FAILED source=automation percent={p.Percent:0.###}");
        AssertContains(automationText, "FLASHBACK_EXPORT_PROGRESS_UI_ENQUEUE_FAILED source=ui percent={p.Percent:0.###}");
        AssertContains(automationText, "public Task SetFlashbackEnabledAsync(bool enabled, CancellationToken cancellationToken = default)");
        AssertContains(automationText, "InvokeOnUiThreadAsync(() => ExecuteFlashbackAction(action, position), cancellationToken)");
        AssertContains(automationText, "=> FromSynchronousSnapshot(GetFlashbackSegments, cancellationToken);");
        AssertContains(automationText, "await _sessionCoordinator.RestartFlashbackAsync(settings, cancellationToken).ConfigureAwait(false)");
        AssertContains(automationText, "_flashbackBitrateSamples.Clear();\n                return true;\n            },\n            cancellationToken).ConfigureAwait(false);");

        return Task.CompletedTask;
    }

    internal static Task MainViewModelAutomation_ViewModelRuntimeSnapshotLivesInFocusedPartial()
    {
        var automationFacadeText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.AutomationCommands.cs")
            .Replace("\r\n", "\n");
        var viewModelRuntimeSnapshotText = automationFacadeText;
        var viewModelRuntimeSnapshotBuilderText = ReadRepoFile("Sussudio/ViewModels/ViewModelBuilders.cs")
            .Replace("\r\n", "\n");
        var agentMapText = ReadRepoFile("docs/architecture/AGENT_MAP.md");
        var cleanupPlanText = ReadRepoFile("docs/architecture/cleanup-plan.md");

        AssertContains(viewModelRuntimeSnapshotText, "public partial class MainViewModel");
        AssertContains(viewModelRuntimeSnapshotText, "public Task<ViewModelRuntimeSnapshot> GetViewModelRuntimeSnapshotAsync(CancellationToken cancellationToken = default)");
        AssertContains(viewModelRuntimeSnapshotText, "var sessionSnapshot = _sessionCoordinator.Snapshot;");
        AssertContains(viewModelRuntimeSnapshotText, "return InvokeOnUiThreadAsync(() =>");
        AssertContains(viewModelRuntimeSnapshotText, "var input = new ViewModelRuntimeSnapshotInput");
        AssertContains(viewModelRuntimeSnapshotText, "return ViewModelRuntimeSnapshotBuilder.Build(input);");
        AssertDoesNotContain(viewModelRuntimeSnapshotText, "=> new ViewModelRuntimeSnapshot");
        AssertContains(viewModelRuntimeSnapshotBuilderText, "internal static class ViewModelRuntimeSnapshotBuilder");
        AssertContains(viewModelRuntimeSnapshotBuilderText, "internal sealed class ViewModelRuntimeSnapshotInput");
        AssertContains(viewModelRuntimeSnapshotBuilderText, "SourceTelemetryAgeSeconds = TelemetryAgeHelper.ComputeAgeSeconds(input.SourceTelemetryTimestampUtc, input.TimestampUtc),");
        AssertContains(viewModelRuntimeSnapshotBuilderText, "CaptureCommandCommandsEnqueued = sessionSnapshot.CommandsEnqueued,");
        AssertContains(viewModelRuntimeSnapshotBuilderText, "CaptureCommandLastCommand = sessionSnapshot.LastCommand?.ToString() ?? \"None\",");
        AssertContains(viewModelRuntimeSnapshotBuilderText, "CaptureCommandLastCorrelationId = sessionSnapshot.LastCorrelationId ?? string.Empty,");
        AssertContains(viewModelRuntimeSnapshotBuilderText, "PreviewVolumePercent = input.PreviewVolume * 100.0,");
        AssertContains(automationFacadeText, "public Task<ViewModelRuntimeSnapshot> GetViewModelRuntimeSnapshotAsync");
        AssertDoesNotContain(automationFacadeText, "=> new ViewModelRuntimeSnapshot");
        AssertContains(automationFacadeText, "public VideoSourceProbeResult ProbeVideoSource() => _captureService.ProbeVideoSource();");
        AssertContains(automationFacadeText, "public PreviewColorProbeResult ProbePreviewColor() => _captureService.ProbePreviewColor();");
        AssertContains(automationFacadeText, "public Task<VideoSourceProbeResult> ProbeVideoSourceAsync(CancellationToken cancellationToken = default)");
        AssertContains(automationFacadeText, "public Task<PreviewColorProbeResult> ProbePreviewColorAsync(CancellationToken cancellationToken = default)");
        AssertContains(automationFacadeText, "public Task<PreviewFrameCaptureResult> CapturePreviewFrameAsync(string outputPath, CancellationToken cancellationToken = default)");
        AssertContains(automationFacadeText, "=> FromSynchronousSnapshot(ProbeVideoSource, cancellationToken);");
        AssertContains(automationFacadeText, "=> FromSynchronousSnapshot(ProbePreviewColor, cancellationToken);");
        AssertContains(automationFacadeText, "public Task<CaptureRuntimeSnapshot> GetCaptureRuntimeSnapshotAsync(CancellationToken cancellationToken = default)\n        => FromSynchronousSnapshot(_captureService.GetRuntimeSnapshot, cancellationToken);");
        AssertContains(agentMapText, "`MainViewModel.AutomationCommands.cs` owns automation-facing view-model runtime snapshot UI-thread capture.");
        AssertContains(agentMapText, "`ViewModelBuilders.cs` owns pure view-model runtime snapshot DTO construction.");
        AssertContains(agentMapText, "also owns automation-facing source/preview probes and preview frame capture.");
        AssertContains(cleanupPlanText, "`MainViewModel.AutomationCommands.cs`; pure view-model runtime snapshot DTO");
        AssertContains(cleanupPlanText, "construction lives in `ViewModelBuilders.cs`");
        AssertContains(cleanupPlanText, "probes, and preview frame capture now live in\n   `MainViewModel.AutomationCommands.cs`");

        return Task.CompletedTask;
    }

    internal static Task AutomationAudioCommands_PreserveRuntimeGuards()
    {
        var automationAudioText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.AutomationCommands.cs")
            .Replace("\r\n", "\n");
        var automationUiText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs")
            .Replace("\r\n", "\n");
        var viewModelText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs")
            .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/ViewModels/MainViewModel.AudioState.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelDeviceAudioRequestController.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelUiDispatchController.cs")
                .Replace("\r\n", "\n");
        var captureServiceText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.cs")
            .Replace("\r\n", "\n")
            + "\n" + ReadCaptureServiceAudioSource()
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.Snapshots.cs")
                .Replace("\r\n", "\n");

        AssertContains(automationAudioText, "public Task SetAudioEnabledAsync(bool enabled, CancellationToken cancellationToken = default)");
        AssertContains(automationAudioText, "public Task SetAudioPreviewEnabledAsync(bool enabled, CancellationToken cancellationToken = default)");
        AssertContains(automationAudioText, "public Task SetPreviewVolumeAsync(double previewVolumePercent, CancellationToken cancellationToken = default)");
        AssertContains(automationAudioText, "PreviewVolume = Math.Clamp(previewVolumePercent / 100.0, 0.0, 1.0);\n            SavePreviewVolume();");
        AssertContains(automationAudioText, "public Task SetDeviceAudioModeAsync(string mode, CancellationToken cancellationToken = default)");
        AssertContains(automationAudioText, "public Task SetAnalogAudioGainAsync(double gainPercent, CancellationToken cancellationToken = default)");
        AssertContains(automationAudioText, "WithAudioControlRefreshSuppressed(() => SelectedDeviceAudioMode = normalizedMode);");
        AssertContains(automationAudioText, "WithAudioControlRefreshSuppressed(() => AnalogAudioGainPercent = clampedGain);");
        AssertContains(automationAudioText, "public Task SetMicrophoneEnabledAsync(bool enabled, CancellationToken cancellationToken = default)");
        AssertContains(automationAudioText, "private async Task SetMicrophoneEnabledAutomationAsync(bool enabled, CancellationToken cancellationToken)");
        AssertContains(automationAudioText, "Logger.Log($\"MIC_TOGGLE_NOOP reason=recording_active_idempotent requested={enabled}\");");
        AssertContains(automationAudioText, "Logger.Log($\"MIC_TOGGLE_REFUSED reason=recording_active requested={enabled} current={request.CurrentMicEnabled}\");");
        AssertContains(automationAudioText, "Cannot change microphone enable state while recording. Stop the recording first.");
        AssertContains(automationAudioText, "_suppressMicrophoneMonitorUpdate = true;");
        AssertContains(automationAudioText, "await _sessionCoordinator.UpdateMicrophoneMonitorAsync(");
        AssertContains(automationAudioText, "cancellationToken).ConfigureAwait(false);");
        AssertContains(automationAudioText, "IsMicrophoneEnabled = enabled;\n                }\n                finally\n                {\n                    _suppressMicrophoneMonitorUpdate = false;\n                }\n\n                return true;\n            },\n            cancellationToken).ConfigureAwait(false);");
        AssertDoesNotContain(automationUiText, "public Task SetPreviewVolumeAsync");
        AssertContains(viewModelText, "if (_suppressMicrophoneMonitorUpdate)");
        AssertContains(captureServiceText, "var previousEnabled = _micMonitorEnabled;");
        AssertContains(captureServiceText, "await DisposeMicrophoneCaptureAsync().ConfigureAwait(false);\n\n                _micMonitorEnabled = enabled;");

        var microphoneUpdateIndex = automationAudioText.IndexOf(
            "await _sessionCoordinator.UpdateMicrophoneMonitorAsync(",
            StringComparison.Ordinal);
        var microphonePersistIndex = automationAudioText.IndexOf(
            "IsMicrophoneEnabled = enabled;",
            StringComparison.Ordinal);
        AssertEqual(
            true,
            microphoneUpdateIndex >= 0 && microphonePersistIndex > microphoneUpdateIndex,
            "automation microphone persists only after monitor update");
        foreach (var stalePath in new[]
        {
            "MainViewModel.AutomationAudio.cs",
            "MainViewModel.AutomationDeviceAudio.cs",
            "MainViewModel.AutomationMicrophone.cs"
        })
        {
            AssertEqual(
                false,
                File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", stalePath)),
                $"stale audio automation partial {stalePath}");
        }

        return Task.CompletedTask;
    }

    internal static Task MainViewModelAutomation_RoutesPreviewVolumePersistenceThroughSaveHook()
    {
        var vmType = RequireType("Sussudio.ViewModels.MainViewModel");

        var savePreviewVolume = vmType.GetMethod(
            "SavePreviewVolume",
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        AssertNotNull(savePreviewVolume, "MainViewModel.SavePreviewVolume");

        var previewVolume = vmType.GetProperty("PreviewVolume", BindingFlags.Instance | BindingFlags.Public);
        AssertNotNull(previewVolume, "MainViewModel.PreviewVolume");

        var audioPreview = vmType.GetProperty("IsAudioPreviewEnabled", BindingFlags.Instance | BindingFlags.Public);
        AssertNotNull(audioPreview, "MainViewModel.IsAudioPreviewEnabled");

        var getOptionsSnapshot = vmType.GetMethod(
            "GetAutomationOptionsSnapshotAsync",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        AssertNotNull(getOptionsSnapshot, "MainViewModel.GetAutomationOptionsSnapshotAsync");

        return Task.CompletedTask;
    }

    internal static Task MainViewModelCapture_RoutesAudioMonitoringThroughCoordinator()
    {
        var coordinatorType = RequireType("Sussudio.Services.Capture.CaptureSessionCoordinator");

        var setPreviewVolume = coordinatorType.GetMethod(
            "SetPreviewVolume", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        AssertNotNull(setPreviewVolume, "CaptureSessionCoordinator.SetPreviewVolume");

        var updateAudioMonitoring = coordinatorType.GetMethod(
            "UpdateAudioMonitoringAsync", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        AssertNotNull(updateAudioMonitoring, "CaptureSessionCoordinator.UpdateAudioMonitoringAsync");

        var updateAudioInput = coordinatorType.GetMethod(
            "UpdateAudioInputAsync", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        AssertNotNull(updateAudioInput, "CaptureSessionCoordinator.UpdateAudioInputAsync");

        var startVideoPreview = coordinatorType.GetMethod(
            "StartVideoPreviewAsync", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        AssertNotNull(startVideoPreview, "CaptureSessionCoordinator.StartVideoPreviewAsync");

        var commandKindType = RequireType("Sussudio.Models.AutomationCommandKind");
        AssertEqual(true,
            Enum.IsDefined(commandKindType, Enum.Parse(commandKindType, "SetAudioPreviewEnabled")),
            "AutomationCommandKind.SetAudioPreviewEnabled exists");

        return Task.CompletedTask;
    }

    internal static Task DiagnosticsLoop_DoesNotRebuildAutomationOptionsEachPoll()
    {
        var diagnosticsHubText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.Snapshots.cs");
        var automationSnapshotText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.AutomationCommands.cs");
        var automationOptionsText = automationSnapshotText;
        var automationOptionsBuilderText = ReadRepoFile("Sussudio/ViewModels/ViewModelBuilders.cs");

        AssertDoesNotContain(diagnosticsHubText, "GetAutomationOptionsSnapshotAsync(cancellationToken)");
        AssertDoesNotContain(diagnosticsHubText, "Options = optionsSnapshot");
        AssertDoesNotContain(automationSnapshotText, "BuildStringOptions(");
        AssertContains(automationOptionsText, "GetAutomationOptionsSnapshotAsync");
        AssertContains(automationOptionsText, "InvokeOnUiThreadAsync(() =>");
        AssertContains(automationOptionsText, "AvailableFrameRates");
        AssertContains(automationOptionsText, "FrameRateTimingPolicy.IsFrameRateMatch(option.Value, selectedFrameRate)");
        AssertContains(automationOptionsText, "AutomationOptionsSnapshotBuilder.Build(input)");
        AssertNoRegex(
            automationOptionsText,
            @"new\s+AutomationOptionsSnapshot\s*\{",
            "MainViewModel automation options DTO construction");
        AssertContains(automationOptionsBuilderText, "internal static class AutomationOptionsSnapshotBuilder");
        AssertContains(automationOptionsBuilderText, "internal sealed class AutomationOptionsSnapshotInput");
        AssertContains(automationOptionsBuilderText, "BuildStringOptions(input.RecordingFormats, input.SelectedRecordingFormat)");
        AssertContains(automationOptionsBuilderText, "MjpegDecoderCounts = Enumerable.Range(1, 8)");
        AssertContains(automationOptionsBuilderText, "DisableReason = option.DisableReason ?? string.Empty");
        AssertContains(automationOptionsBuilderText, "PreviewVolumePercent = input.PreviewVolume * 100.0");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.AutomationOptionsSnapshot.cs")),
            "MainViewModel.AutomationOptionsSnapshot.cs folded into MainViewModel.AutomationCommands.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.AutomationSnapshots.cs")),
            "MainViewModel.AutomationSnapshots.cs folded into MainViewModel.AutomationCommands.cs");

        return Task.CompletedTask;
    }

    private static void AssertTaskReturningMethod(Type type, string methodName, Type? resultType)
    {
        var method = type.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public)
            ?? type.GetInterfaces()
                .Select(interfaceType => interfaceType.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public))
                .FirstOrDefault(candidate => candidate != null);
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

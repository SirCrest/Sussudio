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
            + "\n" + ReadRepoFile("Sussudio/Controllers/UiDispatchControllers.cs")
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
        var automationFacadeText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs")
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
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.AutomationCommands.cs")),
            "MainViewModel automation commands facade folded into MainViewModel.cs");

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

    internal static Task MainViewModelAutomation_RoutesRecordingThroughSharedTransitionGate()
    {
        var rootViewModelText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs")
            .Replace("\r\n", "\n");
        var recordingLifecycleText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs")
            .Replace("\r\n", "\n");
        var recordingTransitionControllerRootText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelRecordingTransitionController.cs")
            .Replace("\r\n", "\n");
        var recordingTransitionControllerText = recordingTransitionControllerRootText;
        var automationText = recordingLifecycleText
            + "\n" + ReadRepoFile("Sussudio/ViewModels/MainViewModel.FlashbackState.cs")
                .Replace("\r\n", "\n");
        var captureText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs")
            .Replace("\r\n", "\n");
        var recordingStateText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs")
            .Replace("\r\n", "\n");
        var recordingRuntimeText = recordingStateText;
        var flashbackStateText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.FlashbackState.cs")
            .Replace("\r\n", "\n");
        var flashbackBufferStatusText = flashbackStateText;
        var runtimeLifecycleControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelLifecycleController.cs")
            .Replace("\r\n", "\n");
        var dispatcherText = ReadAutomationCommandDispatcherFamilyText();

        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.Automation.cs")),
            "MainViewModel automation catch-all partial");
        AssertEqual(
            false,
            File.Exists(Path.Combine(
                GetRepoRoot(),
                "Sussudio",
                "ViewModels",
                "MainViewModel.AutomationRecordingLifecycle.cs")),
            "MainViewModel automation recording lifecycle bridge partial");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.RecordingLifecycle.cs")),
            "MainViewModel recording lifecycle facade partial");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.RecordingState.cs")),
            "MainViewModel recording state folded into MainViewModel.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.Capture.cs")),
            "MainViewModel capture lifecycle facade partial");
        AssertContains(recordingLifecycleText, "public Task SetRecordingEnabledAsync(bool enabled, CancellationToken cancellationToken = default)");
        AssertContains(recordingLifecycleText, "=> SetRecordingDesiredStateAsync(enabled, cancellationToken);");
        AssertContains(recordingLifecycleText, "internal Task SetRecordingDesiredStateAsync");
        AssertContains(recordingLifecycleText, "public Task ToggleRecordingAsync()\n        => _recordingTransitionController.ToggleRecordingAsync();");
        AssertContains(recordingLifecycleText, "=> _recordingTransitionController.SetRecordingDesiredStateAsync(enabled, cancellationToken);");
        AssertContains(rootViewModelText, "public Task ToggleRecordingAsync()");
        AssertContains(rootViewModelText, "public Task SetRecordingEnabledAsync(bool enabled, CancellationToken cancellationToken = default)");
        AssertContains(rootViewModelText, "internal Task SetRecordingDesiredStateAsync");
        AssertContains(recordingTransitionControllerRootText, "namespace Sussudio.Controllers;");
        AssertContains(recordingTransitionControllerRootText, "internal sealed class MainViewModelRecordingTransitionController");
        AssertDoesNotContain(recordingTransitionControllerRootText, "partial class MainViewModelRecordingTransitionController");
        AssertContains(recordingTransitionControllerRootText, "internal sealed class MainViewModelRecordingTransitionControllerContext");
        AssertContains(recordingTransitionControllerRootText, "private readonly MainViewModelRecordingTransitionControllerContext _context;");
        AssertDoesNotContain(recordingTransitionControllerText, "private readonly MainViewModel _viewModel;");
        AssertDoesNotContain(recordingTransitionControllerText, "_viewModel.");
        AssertContains(recordingTransitionControllerText, "Recording transition already in progress.");
        AssertContains(recordingTransitionControllerText, "await inFlight;");
        AssertContains(recordingTransitionControllerText, "private Task BeginRecordingTransitionAsync(bool enabled, CancellationToken cancellationToken = default)");
        AssertContains(recordingTransitionControllerText, "var task = RecordingTransitionInnerAsync(enabled, cancellationToken);");
        AssertContains(recordingTransitionControllerRootText, "await StartRecordingAsync(cancellationToken);");
        AssertContains(recordingTransitionControllerRootText, "await StopRecordingAsync(cancellationToken);");
        AssertContains(recordingTransitionControllerText, "await BeginRecordingTransitionAsync(enabled, cancellationToken);");
        AssertDoesNotContain(recordingLifecycleText, "await _sessionCoordinator.StartRecordingAsync(settings, cancellationToken);");
        AssertDoesNotContain(recordingLifecycleText, "await _sessionCoordinator.StopRecordingAsync(cancellationToken);");
        AssertContains(recordingTransitionControllerRootText, "private async Task StartRecordingAsync(CancellationToken cancellationToken = default)");
        AssertContains(recordingTransitionControllerRootText, "private async Task StopRecordingAsync(CancellationToken cancellationToken = default)");
        AssertContains(recordingTransitionControllerRootText, "await _context.StartRecordingAsync(settings, cancellationToken);");
        AssertContains(recordingTransitionControllerRootText, "await _context.StopRecordingAsync(cancellationToken);");
        AssertDoesNotContain(captureText, "private Task BeginRecordingTransitionAsync(bool enabled, CancellationToken cancellationToken = default)");
        AssertDoesNotContain(captureText, "await _sessionCoordinator.StartRecordingAsync(settings, cancellationToken);");
        AssertContains(recordingStateText, "private readonly Stopwatch _recordingStopwatch = new();");
        AssertContains(recordingStateText, "private readonly BitrateSampleWindow _recordingBitrateSamples = new(BitrateWindowMs);");
        AssertContains(flashbackStateText, "private readonly BitrateSampleWindow _flashbackBitrateSamples = new(BitrateWindowMs);");
        AssertContains(recordingStateText, "public partial ObservableCollection<string> AvailableRecordingFormats");
        AssertContains(recordingStateText, "public partial string OutputPath");
        AssertContains(recordingStateText, "public partial bool IsRecording");
        AssertDoesNotContain(recordingStateText, "_activeRecordingToggleTask");
        AssertDoesNotContain(recordingStateText, "_recordingToggleInProgress");
        AssertContains(recordingRuntimeText, "partial void OnIsRecordingChanged(bool value)");
        AssertContains(recordingRuntimeText, "private void UpdateRecordingStats()");
        AssertContains(recordingRuntimeText, "_recordingBitrateSamples.Clear();");
        AssertContains(recordingRuntimeText, "var smoothed = _recordingBitrateSamples.AddSampleAndCompute(now, totalBytes);");
        AssertContains(recordingRuntimeText, "RecordingSizeInfo = DisplayFormatters.FormatBytes(totalBytes, \"0\");");
        AssertContains(recordingRuntimeText, "RecordingBitrateInfo = smoothed.HasValue ? DisplayFormatters.FormatBitrate(smoothed.Value) : \"--\";");
        AssertContains(flashbackBufferStatusText, "var smoothed = _flashbackBitrateSamples.AddSampleAndCompute(now, diskBytes);");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.FlashbackBufferStatus.cs")),
            "MainViewModel.FlashbackBufferStatus.cs folded into MainViewModel.FlashbackState.cs");
        AssertContains(recordingStateText, "internal sealed class BitrateSampleWindow");
        AssertContains(recordingStateText, "public double? AddSampleAndCompute(long tick, long bytes)");
        AssertContains(recordingStateText, "private static double? ComputeAverageBitrate(Queue<(long Tick, long Bytes)> samples)");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "BitrateSampleWindow.cs")),
            "BitrateSampleWindow folded into MainViewModel.cs");
        AssertContains(recordingRuntimeText, "_pendingModeOptionsRefresh = false;");
        AssertContains(recordingRuntimeText, "RebuildResolutionOptions();");
        AssertContains(runtimeLifecycleControllerText, "_context.UpdateRecordingStats();");
        AssertDoesNotContain(runtimeLifecycleControllerText, "private void UpdateRecordingStats()");
        AssertDoesNotContain(runtimeLifecycleControllerText, "private static double? ComputeAverageBitrate(");
        AssertDoesNotContain(runtimeLifecycleControllerText, "partial void OnIsRecordingChanged(bool value)");
        AssertContains(rootViewModelText, "public partial ObservableCollection<string> AvailableRecordingFormats");
        AssertContains(rootViewModelText, "public partial string OutputPath");
        AssertContains(automationText, "=> SetRecordingDesiredStateAsync(enabled, cancellationToken);");
        AssertContains(dispatcherText, "return CreateResponse(correlationId, $\"Recording {(enabled ? \"started\" : \"stopped\")}.\"");
        AssertContains(dispatcherText, "var snapshot = await _diagnosticsHub.RefreshSnapshotNowAsync(cancellationToken).ConfigureAwait(false);");
        AssertContains(dispatcherText, "snapshot: snapshot");

        return Task.CompletedTask;
    }

    internal static Task MainViewModelAutomation_RecordingSettingsRouteThroughControllerAndFlashbackCycle()
    {
        var viewModelFiles = ReadMainViewModelCodeFiles();
        var viewModelFlashbackStateText = viewModelFiles["MainViewModel.FlashbackState.cs"];
        var flashbackSettingsText = viewModelFiles["MainViewModel.FlashbackState.cs"];
        var flashbackEncoderSettingsText = viewModelFiles["MainViewModel.FlashbackState.cs"];
        var automationSettingsText = viewModelFiles["MainViewModel.cs"];
        var recordingSettingsAutomationControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelSettingsAutomationControllers.cs")
            .Replace("\r\n", "\n");
        var rawFlashbackEncoderSettingsText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.FlashbackState.cs")
            .Replace("\r\n", "\n");

        AssertMemberContains(flashbackEncoderSettingsText, "OnSelectedRecordingFormatChanged", "TrackPendingFlashbackCycleTask(\n                _sessionCoordinator.UpdateRecordingFormatAsync(format),");
        AssertMemberContains(flashbackEncoderSettingsText, "OnSelectedRecordingFormatChanged", "_suppressFlashbackFormatCycle is false");
        AssertContains(rawFlashbackEncoderSettingsText, "TrackPendingFlashbackCycleTask(\n                _sessionCoordinator.UpdateRecordingFormatAsync(format),\n                \"recording format\");");
        AssertContains(viewModelFlashbackStateText, "private bool _suppressFlashbackFormatCycle;");
        AssertMemberContains(automationSettingsText, "SetRecordingFormatAsync", "_recordingSettingsAutomationController.SetRecordingFormatAsync(format, cancellationToken)");
        AssertContains(recordingSettingsAutomationControllerText, "internal sealed class MainViewModelRecordingSettingsAutomationControllerContext");
        AssertContains(recordingSettingsAutomationControllerText, "private readonly MainViewModelRecordingSettingsAutomationControllerContext _context;");
        AssertDoesNotContain(recordingSettingsAutomationControllerText, "private readonly MainViewModel _viewModel;");
        AssertDoesNotContain(recordingSettingsAutomationControllerText, "_viewModel.");
        AssertMemberContains(recordingSettingsAutomationControllerText, "SetRecordingFormatAsync", "SetSuppressFlashbackFormatCycle(true);");
        AssertMemberContains(recordingSettingsAutomationControllerText, "SetRecordingFormatAsync", "RecordingSettingsSelectionPolicy.ParseRecordingFormat(matched)");
        AssertMemberContains(recordingSettingsAutomationControllerText, "SetRecordingFormatAsync", "await _context.UpdateRecordingFormatAsync(recordingFormat, cancellationToken)");
        AssertDoesNotContain(flashbackSettingsText, "public async Task SetRecordingFormatAsync");
        AssertMemberContains(recordingSettingsAutomationControllerText, "SetQualityAsync", "SetSuppressFlashbackEncoderSettingsCycle(true);");
        AssertMemberContains(recordingSettingsAutomationControllerText, "SetQualityAsync", "_context.SetSelectedQuality(matched);");
        AssertMemberContains(recordingSettingsAutomationControllerText, "SetQualityAsync", "settings.Quality,");
        AssertMemberContains(recordingSettingsAutomationControllerText, "SetSplitEncodeModeAsync", "SetSuppressFlashbackEncoderSettingsCycle(true);");
        AssertMemberContains(recordingSettingsAutomationControllerText, "SetSplitEncodeModeAsync", "return BuildEncoderSettings(splitEncodeMode: _context.GetSelectedSplitEncodeMode());");
        AssertMemberContains(recordingSettingsAutomationControllerText, "SetSplitEncodeModeAsync", "settings.SplitEncodeMode,");
        AssertMemberContains(recordingSettingsAutomationControllerText, "SetCustomBitrateAsync", "SetSuppressFlashbackEncoderSettingsCycle(true);");
        AssertMemberContains(recordingSettingsAutomationControllerText, "SetCustomBitrateAsync", "_context.SetCustomBitrateMbps(RecordingSettingsSelectionPolicy.ClampCustomBitrateMbps(bitrateMbps));");
        AssertMemberContains(recordingSettingsAutomationControllerText, "SetCustomBitrateAsync", "settings.Bitrate,");
        AssertMemberContains(recordingSettingsAutomationControllerText, "SetPresetAsync", "SetSuppressFlashbackEncoderSettingsCycle(true);");
        AssertMemberContains(recordingSettingsAutomationControllerText, "SetPresetAsync", "_context.SetSelectedPreset(matched);");
        AssertMemberContains(recordingSettingsAutomationControllerText, "SetPresetAsync", "settings.Preset,");
        AssertMemberContains(flashbackEncoderSettingsText, "OnCustomBitrateMbpsChanged", "TrackFlashbackEncoderSettingsCycle(");
        AssertMemberContains(flashbackEncoderSettingsText, "OnSelectedQualityChanged", "TrackFlashbackEncoderSettingsCycle(");
        AssertMemberContains(flashbackEncoderSettingsText, "OnSelectedPresetChanged", "TrackFlashbackEncoderSettingsCycle(");
        AssertMemberContains(flashbackEncoderSettingsText, "OnSelectedSplitEncodeModeChanged", "TrackFlashbackEncoderSettingsCycle(");
        AssertMemberContains(flashbackEncoderSettingsText, "TrackFlashbackEncoderSettingsCycle", "quality: RecordingSettingsSelectionPolicy.ParseVideoQuality(SelectedQuality)");
        AssertMemberContains(flashbackEncoderSettingsText, "TrackFlashbackEncoderSettingsCycle", "customBitrateMbps: CustomBitrateMbps");
        AssertMemberContains(flashbackEncoderSettingsText, "TrackFlashbackEncoderSettingsCycle", "nvencPreset: SelectedPreset");
        AssertMemberContains(flashbackEncoderSettingsText, "TrackFlashbackEncoderSettingsCycle", "splitEncodeMode: SelectedSplitEncodeMode");
        AssertMemberContains(flashbackEncoderSettingsText, "TrackFlashbackEncoderSettingsCycle", "TrackPendingFlashbackCycleTask(task, description);");
        AssertMemberContains(flashbackEncoderSettingsText, "TrackPendingFlashbackCycleTask", "_pendingFlashbackCycleTask = task;");
        AssertMemberContains(flashbackEncoderSettingsText, "TrackPendingFlashbackCycleTask", "if (ReferenceEquals(_pendingFlashbackCycleTask, t))");
        AssertMemberContains(flashbackEncoderSettingsText, "TrackPendingFlashbackCycleTask", "_pendingFlashbackCycleTask = null;");
        AssertMemberContains(flashbackEncoderSettingsText, "TrackPendingFlashbackCycleTask", "if (t.IsFaulted)");
        AssertMemberContains(flashbackEncoderSettingsText, "TrackPendingFlashbackCycleTask", "else if (t.IsCanceled)");
        AssertContains(rawFlashbackEncoderSettingsText, "CycleFlashbackEncoder({description}) failed");
        AssertContains(rawFlashbackEncoderSettingsText, "CycleFlashbackEncoder({description}) canceled");
        AssertEqual(false, File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.FlashbackEncoderSettings.cs")), "MainViewModel.FlashbackEncoderSettings.cs folded into FlashbackState");
        AssertEqual(false, File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.FlashbackSettings.cs")), "MainViewModel.FlashbackSettings.cs folded into FlashbackState");

        return Task.CompletedTask;
    }

    internal static Task BitrateSampleWindow_PreservesBoundedAverageBehavior()
    {
        var windowType = RequireType("Sussudio.ViewModels.BitrateSampleWindow");
        var window = Activator.CreateInstance(
                         windowType,
                         BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                         binder: null,
                         args: new object[] { 10_000L },
                         culture: null)
                     ?? throw new InvalidOperationException("BitrateSampleWindow instance could not be created.");
        var sampleMethod = windowType.GetMethod("AddSampleAndCompute", BindingFlags.Instance | BindingFlags.Public)
            ?? throw new InvalidOperationException("BitrateSampleWindow.AddSampleAndCompute was not found.");
        var clearMethod = windowType.GetMethod("Clear", BindingFlags.Instance | BindingFlags.Public)
            ?? throw new InvalidOperationException("BitrateSampleWindow.Clear was not found.");

        AssertEqual(null, (double?)sampleMethod.Invoke(window, new object[] { 0L, 100L }), "first sample bitrate");
        AssertNearlyEqual(
            8000.0,
            (double)sampleMethod.Invoke(window, new object[] { 1000L, 1100L })!,
            0.0001,
            "two sample bitrate");
        AssertNearlyEqual(
            4000.0,
            (double)sampleMethod.Invoke(window, new object[] { 11_000L, 6100L })!,
            0.0001,
            "trimmed sample bitrate");

        clearMethod.Invoke(window, null);
        AssertEqual(null, (double?)sampleMethod.Invoke(window, new object[] { 12_000L, 6100L }), "cleared sample bitrate");

        return Task.CompletedTask;
    }

    internal static Task MainViewModelCapture_RecordingFailuresPropagateToCallers()
    {
        var recordingTransitionControllerRootText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelRecordingTransitionController.cs")
            .Replace("\r\n", "\n");
        var recordingTransitionControllerText = recordingTransitionControllerRootText;

        AssertContains(recordingTransitionControllerText, "Logger.LogException(ex);");
        AssertContains(recordingTransitionControllerText, "_context.SetIsRecording(_context.GetSessionIsRecording());");
        AssertContains(recordingTransitionControllerText, "catch (OperationCanceledException ex)");
        AssertContains(recordingTransitionControllerText, "transitionError = ex;");
        AssertContains(recordingTransitionControllerText, "Logger.Log($\"Recording transition wait canceled: {ex.Message}\");");
        AssertContains(recordingTransitionControllerText, "if (transitionError is OperationCanceledException transitionCanceled && inFlightTarget == (enabled ? 1 : 0))");
        AssertContains(recordingTransitionControllerText, "throw transitionCanceled;");
        AssertContains(recordingTransitionControllerText, "catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)");
        AssertContains(recordingTransitionControllerText, "_context.SetStatusText(\"Recording start canceled\");");
        AssertContains(recordingTransitionControllerText, "_context.SetStatusText(\"Stop recording canceled\");");
        AssertContains(recordingTransitionControllerText, "_context.SetStatusText($\"Recording failed: {ex.Message}\");");
        AssertContains(recordingTransitionControllerText, "_context.SetStatusText($\"Stop recording failed: {ex.Message}\");");
        AssertContains(recordingTransitionControllerText, "throw;");

        return Task.CompletedTask;
    }

    internal static Task EmergencyRecordingStop_DoesNotDispatchBackToBlockedUiThread()
    {
        var appText = ReadRepoFile("Sussudio/App.xaml.cs")
            .Replace("\r\n", "\n");
        var rootViewModelText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs")
            .Replace("\r\n", "\n");
        var recordingStateText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs")
            .Replace("\r\n", "\n");

        AssertContains(recordingStateText, "internal Task StopRecordingForEmergencyAsync");
        // Fix #12: emergency stop now routes through the coordinator's emergency-flagged path
        // so LibAvRecordingSink applies EmergencyStopTimeoutMs (5s) instead of StopTimeoutMs (30s).
        AssertContains(recordingStateText, "=> _sessionCoordinator.StopRecordingForEmergencyAsync(cancellationToken);");
        AssertContains(rootViewModelText, "internal Task StopRecordingForEmergencyAsync");
        AssertDoesNotContain(ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelRecordingTransitionController.cs"), "StopRecordingForEmergencyAsync");
        AssertContains(appText, "var task = viewModel.StopRecordingForEmergencyAsync();");
        AssertContains(appText, "if (e.IsTerminating || !recoverable)");
        AssertDoesNotContain(appText, "Task.Run(async () =>");
        AssertDoesNotContain(appText, "StopRecordingAndWaitAsync().ConfigureAwait(false)");
        AssertDoesNotContain(appText, "viewModel == null || !viewModel.IsRecording");
        AssertDoesNotContain(recordingStateText, "if (!IsRecording)");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.Capture.cs")),
            "MainViewModel capture lifecycle facade partial");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.RecordingLifecycle.cs")),
            "MainViewModel recording lifecycle facade partial");

        return Task.CompletedTask;
    }

    internal static Task MainViewModelAutomation_ViewModelRuntimeSnapshotLivesInFocusedPartial()
    {
        var automationFacadeText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs")
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
        AssertContains(agentMapText, "`MainViewModel.cs` owns automation-facing view-model runtime snapshot UI-thread capture.");
        AssertContains(agentMapText, "`ViewModelBuilders.cs` owns pure view-model runtime snapshot DTO construction.");
        AssertContains(agentMapText, "also owns automation-facing source/preview probes and preview frame capture.");
        AssertContains(cleanupPlanText, "`MainViewModel.cs`; pure view-model runtime snapshot DTO");
        AssertContains(cleanupPlanText, "construction lives in `ViewModelBuilders.cs`");
        AssertContains(cleanupPlanText, "probes, and preview frame capture now live in\n   `MainViewModel.cs`");

        return Task.CompletedTask;
    }

    internal static Task AutomationAudioCommands_PreserveRuntimeGuards()
    {
        var automationAudioText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs")
            .Replace("\r\n", "\n");
        var automationUiText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs")
            .Replace("\r\n", "\n");
        var viewModelText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs")
            .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/ViewModels/MainViewModel.AudioState.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelDeviceAudioRequestController.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/Controllers/UiDispatchControllers.cs")
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
        AssertContains(automationUiText, "public Task SetPreviewVolumeAsync");
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
        var automationSnapshotText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs");
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
            "MainViewModel.AutomationOptionsSnapshot.cs folded into MainViewModel.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.AutomationSnapshots.cs")),
            "MainViewModel.AutomationSnapshots.cs folded into MainViewModel.cs");

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

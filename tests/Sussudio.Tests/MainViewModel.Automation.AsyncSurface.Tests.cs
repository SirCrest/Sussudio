using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Text;
using System.Linq;
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
        var recordingTransitionControllerRootText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelCaptureLifecycleControllers.cs")
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
        var recordingTransitionControllerRootText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelCaptureLifecycleControllers.cs")
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
        AssertDoesNotContain(ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelCaptureLifecycleControllers.cs"), "StopRecordingForEmergencyAsync");
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
            + "\n" + ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelDeviceControllers.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/Controllers/UiDispatchControllers.cs")
                .Replace("\r\n", "\n");
        var captureServiceText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.cs")
            .Replace("\r\n", "\n")
            + "\n" + ReadCaptureServiceAudioSource()
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.RuntimeSnapshots.cs")
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


    internal static Task MainViewModelCapture_RoutesFlashbackMutationsThroughCoordinator()
    {
        var coordinatorType = RequireType("Sussudio.Services.Capture.CaptureSessionCoordinator");
        foreach (var methodName in new[]
        {
            "SetFlashbackEnabledAsync",
            "RestartFlashbackAsync",
            "UpdateRecordingFormatAsync",
            "CycleFlashbackEncoderSettingsAsync",
            "UpdateFlashbackSettingsAsync",
            "ExportFlashbackRangeAsync",
            "ExportFlashbackLastNSecondsAsync",
            "GetFlashbackSegments",
            "GetFlashbackPlaybackSnapshot",
            "FlashbackBeginScrub",
            "FlashbackSeek",
            "FlashbackUpdateScrub",
            "FlashbackEndScrub",
            "FlashbackPlay",
            "FlashbackPause",
            "FlashbackGoLive",
            "FlashbackNudge",
            "FlashbackSetInPoint",
            "FlashbackSetOutPoint",
            "FlashbackClearInOutPoints"
        })
        {
            var method = Array.Find(
                coordinatorType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic),
                method => method.Name == methodName);
            AssertNotNull(method, $"CaptureSessionCoordinator.{methodName}");
        }

        var viewModelFiles = ReadMainViewModelCodeFiles();
        var recordingSettingsAutomationControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelSettingsAutomationControllers.cs")
            .Replace("\r\n", "\n");
        var viewModelText = string.Join("\n", viewModelFiles.Values) + "\n" + recordingSettingsAutomationControllerText;
        var viewModelAudioStateText = viewModelFiles["MainViewModel.AudioState.cs"];
        var viewModelFlashbackStateText = viewModelFiles["MainViewModel.FlashbackState.cs"];
        var flashbackSettingsText = viewModelFiles["MainViewModel.FlashbackState.cs"];
        var flashbackExportText = viewModelFiles["MainViewModel.FlashbackState.cs"];
        var flashbackExportOperationText = viewModelFiles["MainViewModel.FlashbackState.cs"];
        var flashbackExportAutomationText = viewModelFiles["MainViewModel.FlashbackState.cs"];
        var flashbackBufferStatusText = viewModelFlashbackStateText;
        var flashbackPlaybackCommandsText = viewModelFlashbackStateText;
        var flashbackPlaybackText = flashbackPlaybackCommandsText;
        var flashbackAutomationText = flashbackSettingsText
            + "\n" + flashbackExportText
            + "\n" + flashbackExportOperationText
            + "\n" + flashbackExportAutomationText
            + "\n" + flashbackBufferStatusText
            + "\n" + flashbackPlaybackCommandsText;
        var audioCapturePropertyChangesText = viewModelFiles["MainViewModel.AudioState.cs"];
        var rawViewModelText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs")
            .Replace("\r\n", "\n");
        var rawAudioCapturePropertyChangesText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.AudioState.cs")
            .Replace("\r\n", "\n");
        var flashbackEncoderSettingsText = viewModelFiles["MainViewModel.FlashbackState.cs"];
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.AutomationFlashback.cs")),
            "MainViewModel automation Flashback partial");
        var rawFlashbackSettingsText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.FlashbackState.cs")
            .Replace("\r\n", "\n");
        var coordinatorText = ReadCaptureSessionCoordinatorSource();
        var captureServiceText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.cs")
            .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackControls.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadCaptureServiceAudioSource();

        AssertContains(coordinatorText, "if (controller is { IsDisposed: false, IsInitialized: true, State: not FlashbackPlaybackState.Disabled })\n        {\n            return true;\n        }");
        AssertMemberContains(flashbackPlaybackText, "GetFlashbackPlaybackSnapshot", "_sessionCoordinator.GetFlashbackPlaybackSnapshot()");
        AssertMemberContains(flashbackPlaybackText, "ReportFlashbackPlaybackRejection", "_sessionCoordinator.GetFlashbackPlaybackSnapshot()");
        AssertMemberContains(flashbackPlaybackText, "ReportFlashbackPlaybackRejection", "StatusText = message;");
        AssertMemberContains(flashbackPlaybackCommandsText, "ExecuteFlashbackActionAsync", "InvokeOnUiThreadAsync(() => ExecuteFlashbackAction(action, position), cancellationToken)");
        AssertMemberContains(flashbackPlaybackCommandsText, "ExecuteFlashbackAction", "return FlashbackBeginScrub(position ?? TimeSpan.Zero)");
        AssertMemberContains(flashbackPlaybackCommandsText, "ExecuteFlashbackAction", "return FlashbackSetInPoint().HasValue");
        AssertMemberDoesNotContain(flashbackPlaybackCommandsText, "ExecuteFlashbackAction", "_sessionCoordinator.FlashbackSetInPoint()");
        AssertMemberContains(flashbackPlaybackCommandsText, "FlashbackBeginScrub", "_sessionCoordinator.FlashbackBeginScrub(position)");
        AssertMemberContains(flashbackPlaybackCommandsText, "FlashbackSeek", "_sessionCoordinator.FlashbackSeek(position)");
        AssertMemberContains(flashbackPlaybackCommandsText, "FlashbackUpdateScrub", "return _sessionCoordinator.FlashbackUpdateScrub(position)");
        AssertMemberContains(flashbackPlaybackCommandsText, "FlashbackEndScrub", "_sessionCoordinator.FlashbackEndScrub()");
        AssertMemberContains(flashbackPlaybackCommandsText, "FlashbackEndScrubAt", "_sessionCoordinator.FlashbackEndScrubAt(position)");
        AssertMemberContains(flashbackPlaybackCommandsText, "FlashbackPlay", "_sessionCoordinator.FlashbackPlay()");
        AssertMemberContains(flashbackPlaybackCommandsText, "FlashbackPause", "_sessionCoordinator.FlashbackPause()");
        AssertMemberContains(flashbackPlaybackCommandsText, "FlashbackGoLive", "_sessionCoordinator.FlashbackGoLive()");
        AssertMemberContains(flashbackPlaybackCommandsText, "FlashbackNudge", "_sessionCoordinator.FlashbackNudge(delta)");
        AssertMemberContains(flashbackPlaybackCommandsText, "FlashbackSetInPoint", "_sessionCoordinator.FlashbackSetInPoint()");
        AssertMemberContains(flashbackPlaybackCommandsText, "FlashbackSetInPointAt", "_sessionCoordinator.FlashbackSetInPointAt(position)");
        AssertMemberContains(flashbackPlaybackCommandsText, "FlashbackSetOutPoint", "_sessionCoordinator.FlashbackSetOutPoint()");
        AssertMemberContains(flashbackPlaybackCommandsText, "FlashbackSetOutPointAt", "_sessionCoordinator.FlashbackSetOutPointAt(position)");
        AssertMemberContains(flashbackPlaybackCommandsText, "FlashbackClearInOutPoints", "=> _sessionCoordinator.FlashbackClearInOutPoints()");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.FlashbackMarkers.cs")),
            "MainViewModel.FlashbackMarkers.cs folded into MainViewModel.FlashbackState.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.FlashbackPlaybackAutomation.cs")),
            "MainViewModel.FlashbackPlaybackAutomation.cs folded into MainViewModel.FlashbackState.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.FlashbackPlayback.cs")),
            "MainViewModel.FlashbackPlayback.cs folded into MainViewModel.FlashbackState.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.FlashbackPlaybackCommands.cs")),
            "MainViewModel.FlashbackPlaybackCommands.cs folded into MainViewModel.FlashbackState.cs");
        AssertMemberContains(flashbackBufferStatusText, "UpdateFlashbackBufferStatus", "_sessionCoordinator.GetFlashbackBufferStatus()");
        AssertMemberContains(flashbackBufferStatusText, "UpdateFlashbackBufferStatus", "_sessionCoordinator.GetFlashbackPlaybackSnapshot()");
        AssertMemberContains(flashbackBufferStatusText, "UpdateFlashbackBufferStatus", "FlashbackInPoint = playback.InPoint;");
        AssertMemberContains(flashbackBufferStatusText, "UpdateFlashbackBufferStatus", "FlashbackOutPoint = playback.OutPoint;");
        AssertMemberContains(flashbackBufferStatusText, "UpdateFlashbackBufferStatus", "FlashbackInPoint = null;");
        AssertMemberContains(flashbackBufferStatusText, "UpdateFlashbackBufferStatus", "FlashbackOutPoint = null;");
        AssertMemberContains(flashbackBufferStatusText, "UpdateFlashbackBufferStatus", "if (FlashbackState != FlashbackPlaybackState.Live)");
        AssertMemberContains(flashbackBufferStatusText, "UpdateFlashbackBufferStatus", "FlashbackState = FlashbackPlaybackState.Live;");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.FlashbackBufferStatus.cs")),
            "MainViewModel.FlashbackBufferStatus.cs folded into MainViewModel.FlashbackState.cs");
        var updateFlashbackBufferStatus = ExtractMemberCode(flashbackBufferStatusText, "UpdateFlashbackBufferStatus");
        var inactivePlaybackSnapshotBranch = ExtractTextBetween(
            updateFlashbackBufferStatus,
            "else\n        {\n            if (FlashbackState != FlashbackPlaybackState.Live)",
            "\n        }\n    }");
        AssertDoesNotContain(inactivePlaybackSnapshotBranch, "FlashbackInPoint = null;");
        AssertDoesNotContain(inactivePlaybackSnapshotBranch, "FlashbackOutPoint = null;");
        AssertMemberContains(flashbackBufferStatusText, "UpdateFlashbackBitrate", "_sessionCoordinator.FlashbackTotalBytesWritten");
        AssertContains(captureServiceText, "public long FlashbackTotalBytesWritten => _flashbackBackend.BufferManager?.TotalBytesWritten ?? 0;");
        AssertContains(captureServiceText, "ClassifyCaptureFailureSource(object? sender)");
        AssertContains(captureServiceText, "ReferenceEquals(sender, ProgramCapture)");
        AssertContains(captureServiceText, "ReferenceEquals(sender, MicrophoneCapture)");
        AssertContains(captureServiceText, "WASAPI_CAPTURE_FAILED source={source}");
        AssertContains(captureServiceText, "_previewAudioGraph.RecordCaptureFault(source, ex);");
        AssertContains(coordinatorText, "if (Volatile.Read(ref _isDisposed))");
        AssertContains(coordinatorText, "Volatile.Write(ref _isDisposed, true);");
        AssertContains(coordinatorText, "Exception failure = Volatile.Read(ref _isDisposed)");
        AssertContains(viewModelFlashbackStateText, "private int _flashbackExportOperationId;");
        AssertMemberContains(flashbackPlaybackText, "GetFlashbackSegments", "_sessionCoordinator.GetFlashbackSegments()");
        AssertMemberContains(flashbackSettingsText, "SetFlashbackEnabledAsync", "_sessionCoordinator.SetFlashbackEnabledAsync(enabled, cancellationToken)");
        AssertMemberContains(flashbackSettingsText, "RestartFlashbackAsync", "InvokeOnUiThreadAsync(BuildCaptureSettings, cancellationToken)");
        AssertMemberContains(flashbackSettingsText, "RestartFlashbackAsync", "_sessionCoordinator.RestartFlashbackAsync(settings, cancellationToken)");

        AssertDoesNotContain(flashbackSettingsText, "public async Task SetRecordingFormatAsync");
        AssertMemberContains(flashbackSettingsText, "OnFlashbackBufferMinutesChanged", "_sessionCoordinator.UpdateFlashbackSettingsAsync(FlashbackBufferMinutes, FlashbackGpuDecode)");
        AssertMemberContains(flashbackSettingsText, "OnFlashbackGpuDecodeChanged", "_sessionCoordinator.UpdateFlashbackSettingsAsync(FlashbackBufferMinutes, FlashbackGpuDecode)");
        AssertMemberContains(flashbackSettingsText, "OnFlashbackBufferMinutesChanged", "Interlocked.Increment(ref _flashbackSettingsRestartGeneration)");
        AssertMemberContains(flashbackSettingsText, "OnFlashbackBufferMinutesChanged", "RestartFlashbackAfterSettingsUpdateAsync(updateTask, restartGeneration)");
        AssertMemberContains(flashbackSettingsText, "RestartFlashbackAfterSettingsUpdateAsync", "Volatile.Read(ref _flashbackSettingsRestartGeneration)");
        AssertMemberContains(flashbackSettingsText, "RestartFlashbackAfterSettingsUpdateAsync", "restartGeneration != Volatile.Read(ref _flashbackSettingsRestartGeneration)");
        AssertMemberContains(flashbackSettingsText, "RestartFlashbackAfterSettingsUpdateAsync", "InvokeOnUiThreadAsync(");
        AssertMemberContains(flashbackSettingsText, "RestartFlashbackAfterSettingsUpdateAsync", "IsPreviewing && !IsRecording && _isLoadingSettings is false");
        AssertMemberContains(flashbackSettingsText, "RestartFlashbackAfterSettingsUpdateAsync", "shouldRestart is false");
        AssertMemberContains(flashbackSettingsText, "RestartFlashbackAfterSettingsUpdateAsync", "await RestartFlashbackAsync().ConfigureAwait(false)");
        AssertMemberContains(flashbackSettingsText, "RestartFlashbackAfterSettingsUpdateAsync", "catch (OperationCanceledException ex)");
        AssertContains(rawFlashbackSettingsText, "RestartFlashbackAfterSettingsUpdate canceled");
        AssertEqual(false, File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.FlashbackSettings.cs")), "MainViewModel.FlashbackSettings.cs folded into FlashbackState");
        AssertEqual(false, File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.FlashbackEncoderSettings.cs")), "MainViewModel.FlashbackEncoderSettings.cs folded into FlashbackState");
        AssertEqual(false, File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.FlashbackExportOperation.cs")), "MainViewModel.FlashbackExportOperation.cs folded into FlashbackExport");
        AssertEqual(false, File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.FlashbackExportAutomation.cs")), "MainViewModel.FlashbackExportAutomation.cs folded into FlashbackExport");
        AssertMemberContains(audioCapturePropertyChangesText, "OnIsAudioEnabledChanged", "var settings = BuildCaptureSettings();");
        AssertMemberContains(rawAudioCapturePropertyChangesText, "OnIsAudioEnabledChanged", "SetAudioMonitoringEnabledWithVolumeTransitionAsync(\n                        true,\n                        \"audio_capture_enable\",");
        AssertMemberContains(audioCapturePropertyChangesText, "OnIsAudioEnabledChanged", "afterMonitoringStarted: () => _sessionCoordinator.RestartFlashbackAsync(settings)");
        AssertMemberContains(rawAudioCapturePropertyChangesText, "OnIsAudioEnabledChanged", "SetAudioMonitoringEnabledWithVolumeTransitionAsync(false, \"audio_capture_disable\", teardownCapture: true)");
        AssertContains(viewModelAudioStateText, "private int _audioEnabledChangeGeneration;");
        AssertContains(viewModelAudioStateText, "private bool _suppressAudioPreviewEnabledChangeOperation;");
        AssertMemberContains(audioCapturePropertyChangesText, "OnIsAudioEnabledChanged", "var changeGeneration = Interlocked.Increment(ref _audioEnabledChangeGeneration);");
        AssertMemberContains(audioCapturePropertyChangesText, "OnIsAudioEnabledChanged", "_suppressAudioPreviewEnabledChangeOperation = true;");
        AssertMemberContains(audioCapturePropertyChangesText, "OnIsAudioEnabledChanged", "changeGeneration != Volatile.Read(ref _audioEnabledChangeGeneration) || !IsAudioEnabled");
        AssertMemberContains(audioCapturePropertyChangesText, "OnIsAudioEnabledChanged", "changeGeneration != Volatile.Read(ref _audioEnabledChangeGeneration) || IsAudioEnabled");
        AssertMemberContains(rawAudioCapturePropertyChangesText, "OnIsAudioEnabledChanged", "AUDIO_TOGGLE_SKIP op=enable");
        AssertMemberContains(rawAudioCapturePropertyChangesText, "OnIsAudioEnabledChanged", "AUDIO_TOGGLE_SKIP op=disable");
        AssertContains(viewModelFlashbackStateText, "private int _flashbackSettingsRestartGeneration;");

        foreach (var memberName in new[]
        {
            "GetFlashbackPlaybackSnapshot",
            "FlashbackBeginScrub",
            "FlashbackSeek",
            "FlashbackUpdateScrub",
            "FlashbackEndScrub",
            "FlashbackEndScrubAt",
            "FlashbackPlay",
            "FlashbackPause",
            "FlashbackGoLive",
            "FlashbackNudge",
            "FlashbackSetInPoint",
            "FlashbackSetInPointAt",
            "FlashbackSetOutPoint",
            "FlashbackSetOutPointAt",
            "FlashbackClearInOutPoints",
            "UpdateFlashbackBufferStatus",
            "UpdateFlashbackBitrate",
            "ExportFlashbackAsync",
            "SaveFlashbackLast5mAsync",
            "ExportFlashbackAutomationAsync",
            "GetFlashbackSegments",
            "SetFlashbackEnabledAsync",
            "RestartFlashbackAsync"
        })
        {
            AssertMemberDoesNotContain(flashbackAutomationText, memberName, "_captureService");
        }

        foreach (var memberName in new[]
        {
            "OnSelectedRecordingFormatChanged",
            "OnCustomBitrateMbpsChanged",
            "OnFlashbackBufferMinutesChanged",
            "OnFlashbackGpuDecodeChanged",
            "OnSelectedQualityChanged",
            "OnSelectedPresetChanged",
            "OnSelectedSplitEncodeModeChanged"
        })
        {
            var sourceText = memberName is "OnFlashbackBufferMinutesChanged" or "OnFlashbackGpuDecodeChanged"
                ? flashbackSettingsText
                : flashbackEncoderSettingsText;
            AssertMemberDoesNotContain(sourceText, memberName, "_captureService");
        }

        AssertNoRegex(
            viewModelText,
            @"\b_captureService\s*\.\s*(SetFlashbackEnabled|RestartFlashbackAsync|UpdateRecordingFormatAsync|CycleFlashbackEncoderSettingsAsync|UpdateFlashbackSettings|ExportFlashback|GetFlashbackSegments|FlashbackPlaybackController|FlashbackBufferManager|FlashbackDiskBytes|FlashbackTotalBytesWritten)\b",
            "MainViewModel flashback mutating/backend capture-service access");
        AssertNoRegex(
            viewModelText,
            @"\b(?:var|CaptureService)\s+\w+\s*=\s*_captureService\s*;",
            "MainViewModel local capture-service aliases");

        return Task.CompletedTask;
    }

    internal static Task CaptureService_FlashbackExportsReleaseBackendLeaseBeforeNativeExport()
    {
        var exportOperationsText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackExportCore.cs")
            .Replace("\r\n", "\n");
        var exportCoreText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackExportCore.cs")
            .Replace("\r\n", "\n");
        var captureServiceText = exportOperationsText
            + "\n" + exportCoreText
            + "\n" + ReadCaptureServiceFlashbackOrchestrationSource()
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.cs")
                .Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/Services/Capture/CaptureService.PreviewLifecycle.cs")
                .Replace("\r\n", "\n");
        var backendResourcesText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackBackendResources.cs")
            .Replace("\r\n", "\n");

        AssertContains(exportOperationsText, "internal async Task<FinalizeResult> ExportFlashbackRangeAsync");
        AssertContains(exportOperationsText, "internal async Task<FinalizeResult> ExportFlashbackLastNSecondsAsync");
        AssertDoesNotContain(exportOperationsText, "resolveRangeAfterEvictionPaused: manager =>");
        AssertContains(exportOperationsText, "private readonly record struct FlashbackExportBackendSnapshot(");
        AssertContains(exportOperationsText, "private async Task<FlashbackExportBackendSnapshotResult> SnapshotFlashbackExportBackendAsync(");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.FlashbackExportBackendSnapshot.cs")),
            "old Flashback export backend snapshot partial removed");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.FlashbackExportRangeResolution.cs")),
            "old Flashback export range-resolution partial removed");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.FlashbackExportForceRotate.cs")),
            "old Flashback export force-rotate partial removed");
        AssertContains(exportCoreText, "private delegate (bool Succeeded, TimeSpan InPoint, TimeSpan OutPoint, string? FailureMessage)");
        AssertContains(exportCoreText, "private static FlashbackExportRangeResolver CreateFlashbackExportRangeResolver(");
        AssertContains(exportCoreText, "private static FlashbackExportRangeResolver CreateFlashbackExportLastNRangeResolver(double seconds)");
        AssertContains(exportOperationsText, "return await ExportFlashbackCoreAsync(");
        AssertContains(exportCoreText, "private async Task<FinalizeResult> ExportFlashbackCoreAsync");
        AssertContains(exportCoreText, "bufferManager.PauseEviction();");
        AssertContains(exportCoreText, "private FlashbackExportPreparationResult PrepareFlashbackExportRequest(");
        AssertContains(exportCoreText, "PrepareFlashbackExportForceRotateSegments(");
        AssertContains(exportCoreText, "private FlashbackExportForceRotatePreparation PrepareFlashbackExportForceRotateSegments(");
        AssertContains(exportCoreText, "ForceRotateForExport");
        AssertContains(exportCoreText, "CreateFlashbackExportThrottleDelayProvider");

        var rangeExport = ExtractMemberCode(exportOperationsText, "ExportFlashbackRangeAsync");
        AssertContains(rangeExport, "SnapshotFlashbackExportBackendAsync(");
        AssertContains(rangeExport, "operationName: \"range\",");
        AssertContains(rangeExport, "sessionReleaseOperation: \"flashback_export_snapshot_session\",");
        AssertContains(rangeExport, "var snapshot = snapshotResult.Snapshot;");
        AssertContains(rangeExport, "snapshotSink: snapshot.Sink,");
        AssertContains(rangeExport, "snapshotBufferManager: snapshot.BufferManager,");
        AssertContains(rangeExport, "snapshotExporter: snapshot.Exporter,");
        AssertContains(rangeExport, "exportOperationLockAlreadyHeld: true,");
        AssertContains(rangeExport, "resolveRangeAfterEvictionPaused: CreateFlashbackExportRangeResolver(");
        AssertContains(rangeExport, "inPointFilePts,");
        AssertContains(rangeExport, "outPointFilePts)");

        var lastNExport = ExtractMemberCode(exportOperationsText, "ExportFlashbackLastNSecondsAsync");
        AssertContains(lastNExport, "SnapshotFlashbackExportBackendAsync(");
        AssertContains(lastNExport, "operationName: \"last_n\",");
        AssertContains(lastNExport, "sessionReleaseOperation: \"flashback_export_last_n_snapshot_session\",");
        AssertContains(lastNExport, "var snapshot = snapshotResult.Snapshot;");
        AssertContains(lastNExport, "snapshotSink: snapshot.Sink,");
        AssertContains(lastNExport, "snapshotBufferManager: snapshot.BufferManager,");
        AssertContains(lastNExport, "snapshotExporter: snapshot.Exporter,");
        AssertContains(lastNExport, "exportOperationLockAlreadyHeld: true,");
        AssertContains(lastNExport, "resolveRangeAfterEvictionPaused: CreateFlashbackExportLastNRangeResolver(seconds)");

        var backendSnapshot = ExtractMemberCode(exportOperationsText, "SnapshotFlashbackExportBackendAsync");
        AssertContains(backendSnapshot, "var bufferManager = _flashbackBackend.BufferManager;");
        AssertContains(backendSnapshot, "var flashbackSink = _flashbackBackend.Sink;");
        AssertContains(backendSnapshot, "var flashbackExporter = bufferManager != null\n                ? _flashbackBackend.Exporter ??= new FlashbackExporter()\n                : _flashbackBackend.Exporter;");
        AssertContains(backendSnapshot, "await _flashbackExportOperationLock.WaitAsync(ct).ConfigureAwait(false);\n            exportOperationLockHeld = true;");
        AssertOccursBefore(backendSnapshot, "await _flashbackExportOperationLock.WaitAsync(ct).ConfigureAwait(false);", "ReleaseFlashbackBackendLeaseIfHeld(ref backendLeaseHeld);");
        AssertContains(backendSnapshot, "ReleaseFlashbackBackendLeaseIfHeld(ref backendLeaseHeld);\n            if (sessionLockHeld)");
        AssertContains(backendSnapshot, "new FlashbackExportBackendSnapshot(bufferManager, flashbackSink, flashbackExporter)");

        var exportCore = ExtractTextBetween(
            exportCoreText,
            "    private async Task<FinalizeResult> ExportFlashbackCoreAsync",
            "\n}\n");
        AssertContains(exportCore, "FlashbackExporter? snapshotExporter = null,");
        AssertContains(exportCore, "bool exportOperationLockAlreadyHeld = false,");
        AssertContains(exportCore, "FlashbackExportRangeResolver? resolveRangeAfterEvictionPaused = null)");
        AssertContains(exportCore, "var exportOperationLockHeld = exportOperationLockAlreadyHeld;");
        AssertContains(exportCore, "if (!exportOperationLockAlreadyHeld)");
        AssertContains(exportCore, "ReleaseFlashbackExportOperationLockIfHeld(ref exportOperationLockHeld);");
        AssertOccursBefore(exportCore, "if (bufferManager == null)", "var exporter = snapshotExporter;");
        AssertContains(exportCore, "var exporter = snapshotExporter;\n            if (exporter == null)\n            {\n                exporter = _flashbackBackend.Exporter ??= new FlashbackExporter();\n            }");
        AssertContains(exportCore, "var preparedExport = PrepareFlashbackExportRequest(");
        AssertContains(exportCore, "if (preparedExport.FailureResult is { } preparationFailure)");
        AssertContains(exportCoreText, "var forceRotateFallbackUsed = false;");
        AssertContains(exportCoreText, "forceRotateFallbackUsed = true;");
        AssertContains(exportCore, "live-edge partial fallback: active segment was not closed before timeout; export may omit the newest frames");
        AssertContains(exportCore, "if (preparedExport.ForceRotateFallbackUsed && result.Succeeded)\n            {\n                result = FinalizeResult.Success(");
        AssertContains(exportCore, "RecordLastFlashbackExportResult(exportId, result);\n            CompleteFlashbackExportDiagnostics(exportId, result);");

        var backendCleanup = ExtractTextBetween(
            backendResourcesText,
            "public async Task<bool> CleanupArtifactsAfterExportAsync",
            "    public async Task<FlashbackPlaybackController> StartPreviewBackendAsync");
        AssertContains(backendCleanup, "FlashbackBackendArtifactCleanupRequest request,");
        AssertContains(backendCleanup, "bool exportOperationLockAlreadyHeld = false)");
        AssertContains(backendCleanup, "var lockAcquired = exportOperationLockAlreadyHeld;");
        AssertContains(backendCleanup, "if (!exportOperationLockAlreadyHeld)");
        AssertContains(backendCleanup, "request.Reason");
        AssertContains(backendCleanup, "request.FlashbackExporter.Dispose();");
        AssertContains(backendCleanup, "request.BufferManager.PurgeAllSegments();");
        AssertContains(backendCleanup, "FLASHBACK_BACKEND_CLEANUP_LOCK_REUSED");
        AssertContains(backendCleanup, "if (lockAcquired && releaseLockOnExit)");
        AssertContains(backendCleanup, "releaseExportOperationLock(mode);");

        var cleanupBridge = ExtractTextBetween(
            captureServiceText,
            "private async Task<bool> CleanupFlashbackBackendArtifactsAfterExportAsync",
            "\n}");
        AssertContains(cleanupBridge, "_flashbackBackend.CleanupArtifactsAfterExportAsync(");
        AssertContains(cleanupBridge, "WaitForFlashbackBackendCleanupExportLockAsync");
        AssertContains(cleanupBridge, "ReleaseFlashbackBackendCleanupExportLock");

        var disposeBackend = ExtractTextBetween(
            captureServiceText,
            "private async Task DisposeFlashbackPreviewBackendAsync",
            "    private async Task DisposeFlashbackPreviewBackendCoreAsync");
        AssertContains(disposeBackend, "await _flashbackExportOperationLock.WaitAsync(cancellationToken).ConfigureAwait(false);");
        AssertContains(disposeBackend, "exportOperationLockAlreadyHeld: true");
        AssertContains(disposeBackend, "ReleaseFlashbackExportOperationLockIfHeld(ref exportOperationLockHeld);");

        var disposeBackendCore = ExtractTextBetween(
            captureServiceText,
            "private async Task DisposeFlashbackPreviewBackendCoreAsync",
            "    private FlashbackPreviewBackendDisposalRequest CreateFlashbackPreviewBackendDisposalRequest");
        AssertContains(disposeBackendCore, "FlashbackPreviewBackendDisposalRequest request)");
        AssertContains(disposeBackendCore, "_flashbackBackend.DisposePreviewBackendAsync(request)");

        var disposeBackendResources = ExtractTextBetween(
            backendResourcesText,
            "public async Task DisposePreviewBackendAsync",
            "    public void ScheduleDeferredArtifactCleanup");
        AssertContains(disposeBackendResources, "request.ExportOperationLockAlreadyHeld");
        AssertContains(disposeBackendResources, "request.PurgeSegments ? \"preview_backend_dispose_purge\" : \"preview_backend_dispose\"");
        AssertContains(disposeBackendResources, "\"preview_backend_dispose\",\n                request.AcquireExportOperationLockAsync,\n                request.ReleaseExportOperationLock,\n                request.ExportOperationLockAlreadyHeld)");

        return Task.CompletedTask;
    }

    internal static Task MainViewModelFlashbackExport_RoutesThroughCoordinatorAndOwnsCtsLifecycle()
    {
        var viewModelFiles = ReadMainViewModelCodeFiles();
        var viewModelFlashbackStateText = viewModelFiles["MainViewModel.FlashbackState.cs"];
        var disposalText = viewModelFiles["MainViewModel.cs"];
        var flashbackExportText = viewModelFiles["MainViewModel.FlashbackState.cs"];
        var flashbackExportOperationText = viewModelFiles["MainViewModel.FlashbackState.cs"];
        var flashbackExportAutomationText = viewModelFiles["MainViewModel.FlashbackState.cs"];
        var rawDisposalText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs")
            .Replace("\r\n", "\n");
        var disposalControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelLifecycleController.cs")
            .Replace("\r\n", "\n");
        var rawFlashbackExportText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.FlashbackState.cs")
            .Replace("\r\n", "\n");
        var rawFlashbackExportOperationText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.FlashbackState.cs")
            .Replace("\r\n", "\n");
        var rawFlashbackExportAutomationText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.FlashbackState.cs")
            .Replace("\r\n", "\n");
        var coordinatorText = ReadCaptureSessionCoordinatorSource();

        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.FlashbackExport.cs")),
            "MainViewModel.FlashbackExport.cs folded into MainViewModel.FlashbackState.cs");
        AssertMemberContains(flashbackExportText, "ExportFlashbackAsync", "_sessionCoordinator.ExportFlashbackRangeAsync(");
        AssertMemberContains(flashbackExportText, "ExportFlashbackAsync", "playback.InPointFilePts");
        AssertMemberContains(flashbackExportText, "ExportFlashbackAsync", "playback.OutPointFilePts");
        AssertContains(coordinatorText, "TimeSpan? InPointFilePts,");
        AssertContains(coordinatorText, "TimeSpan? OutPointFilePts,");
        AssertMemberContains(flashbackExportText, "SaveFlashbackLast5mAsync", "_sessionCoordinator.ExportFlashbackLastNSecondsAsync(");
        AssertContains(rawFlashbackExportText, "EnsureFlashbackActiveForExport(\"export\")");
        AssertContains(rawFlashbackExportText, "EnsureFlashbackActiveForExport(\"save_last_5m\")");
        AssertContains(rawFlashbackExportText, "FLASHBACK_EXPORT_UI_REJECTED op={operation} reason=inactive");
        AssertContains(rawFlashbackExportText, "Flashback export unavailable: flashback is not active.");
        AssertMemberContains(flashbackExportText, "ExportFlashbackAsync", "case ExportFlashbackOutcome.Stale:");
        AssertMemberContains(flashbackExportText, "SaveFlashbackLast5mAsync", "case ExportFlashbackOutcome.Stale:");
        AssertContains(viewModelFlashbackStateText, "private int _flashbackExportOperationId;");
        AssertContains(disposalText, "Interlocked.Increment(ref _flashbackExportOperationId);");
        AssertContains(disposalText, "var exportCts = Interlocked.Exchange(ref _exportCts, null);");
        AssertContains(disposalText, "CancelFlashbackExportCts(exportCts);");
        AssertContains(rawDisposalText, "private void CancelActiveFlashbackExportForDispose()");
        AssertContains(disposalControllerText, "_context.CancelActiveFlashbackExport();");
        AssertContains(disposalControllerText, "_context.StopRuntimeForDispose();");
        AssertOccursBefore(disposalControllerText, "_context.CancelActiveFlashbackExport();", "_context.StopRuntimeForDispose();");
        AssertOccursBefore(disposalControllerText, "_context.StopRuntimeForDispose();", "var stepTimeoutMs = EnvironmentHelpers.GetIntFromEnv(");
        AssertContains(rawDisposalText, "DisposeFlashbackExportCtsBestEffort(exportCts, \"viewmodel_dispose\");");
        AssertContains(flashbackExportOperationText, "private abstract record ExportFlashbackOutcome");
        AssertContains(flashbackExportOperationText, "private async Task<ExportFlashbackOutcome> ExportFlashbackCoreAsync");
        AssertContains(flashbackExportOperationText, "var exportId = Interlocked.Increment(ref _flashbackExportOperationId);");
        AssertContains(flashbackExportOperationText, "CancelFlashbackExportCts(oldExportCts);");
        AssertContains(flashbackExportOperationText, "IsCurrentFlashbackExport(exportId, exportCts)");
        AssertContains(flashbackExportOperationText, "_exportCts = null;");
        AssertContains(flashbackExportOperationText, "ReferenceEquals(_exportCts, exportCts)");
        AssertContains(flashbackExportOperationText, "private static void CancelFlashbackExportCts(CancellationTokenSource? cts)");
        AssertContains(flashbackExportOperationText, "catch (ObjectDisposedException)");
        AssertContains(rawFlashbackExportOperationText, "FLASHBACK_EXPORT_CTS_CANCEL_WARN");
        AssertMemberContains(flashbackExportAutomationText, "ExportFlashbackAutomationAsync", "_sessionCoordinator.ExportFlashbackLastNSecondsAsync(");
        AssertMemberContains(flashbackExportAutomationText, "ExportFlashbackAutomationAsync", "var exportId = Interlocked.Increment(ref _flashbackExportOperationId);");
        AssertMemberContains(flashbackExportAutomationText, "ExportFlashbackAutomationAsync", "CancelFlashbackExportCts(oldExportCts);");
        AssertMemberContains(flashbackExportAutomationText, "ExportFlashbackAutomationAsync", "CancellationTokenSource.CreateLinkedTokenSource(cancellationToken)");
        AssertMemberContains(flashbackExportAutomationText, "ExportFlashbackAutomationAsync", "IsCurrentFlashbackExport(exportId, exportCts)");
        AssertMemberContains(flashbackExportAutomationText, "ExportFlashbackAutomationAsync", "FlashbackExportProgress = p.Percent;");
        AssertMemberContains(flashbackExportAutomationText, "ExportFlashbackAutomationAsync", "exportCts.Token");
        AssertMemberContains(flashbackExportAutomationText, "ExportFlashbackAutomationAsync", "_exportCts = null;");
        AssertMemberContains(flashbackExportAutomationText, "ExportFlashbackAutomationAsync", "if (!_dispatcherQueue.TryEnqueue(");
        AssertMemberContains(flashbackExportAutomationText, "ExportFlashbackAutomationAsync", "finally");
        AssertContains(rawFlashbackExportAutomationText, "IsFlashbackExporting = false;\n                    FlashbackExportProgress = 0;\n                    _exportCts = null;");
        AssertContains(flashbackExportOperationText, "private static void DisposeFlashbackExportCtsBestEffort(CancellationTokenSource cts, string operation)");
        AssertContains(rawFlashbackExportOperationText, "FLASHBACK_EXPORT_CTS_DISPOSE_WARN");
        AssertContains(rawFlashbackExportOperationText, "DisposeFlashbackExportCtsBestEffort(exportCts, \"ui_current\");");
        AssertContains(rawFlashbackExportOperationText, "DisposeFlashbackExportCtsBestEffort(exportCts, \"ui_stale\");");
        AssertContains(rawFlashbackExportAutomationText, "DisposeFlashbackExportCtsBestEffort(exportCts, \"automation_dispatcher_cleanup\");");
        AssertContains(rawFlashbackExportAutomationText, "DisposeFlashbackExportCtsBestEffort(exportCts, \"automation_inline_cleanup\");");
        AssertEqual(false, File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.FlashbackExportOperation.cs")), "MainViewModel.FlashbackExportOperation.cs folded into FlashbackExport");
        AssertEqual(false, File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.FlashbackExportAutomation.cs")), "MainViewModel.FlashbackExportAutomation.cs folded into FlashbackExport");
        AssertDoesNotContain(
            flashbackExportText + "\n" + flashbackExportOperationText + "\n" + flashbackExportAutomationText,
            "exportCts.Dispose();");

        return Task.CompletedTask;
    }


    internal static Task AutomationPreviewVolume_PersistsThroughSettingsPath()
    {
        var automationUiText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs").Replace("\r\n", "\n");
        var automationAudioText = automationUiText;
        var settingsProjectionText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.SettingsPersistence.cs").Replace("\r\n", "\n");

        AssertContains(automationAudioText, "PreviewVolume = Math.Clamp(previewVolumePercent / 100.0, 0.0, 1.0);\n            SavePreviewVolume();");
        AssertContains(settingsProjectionText, "PreviewVolume = input.PreviewVolume,");
        AssertContains(automationAudioText, "public Task SetPreviewVolumeAsync(double previewVolumePercent, CancellationToken cancellationToken = default)");
        AssertContains(automationUiText, "public Action<string, bool>? StatsSectionVisibilityHandler { get; set; }");
        AssertContains(automationUiText, "public Task SetStatsSectionVisibleAsync(string section, bool visible, CancellationToken cancellationToken = default)");
        AssertContains(automationUiText, "public Task SetStatsVisibleAsync(bool visible, CancellationToken cancellationToken = default)");
        AssertContains(automationUiText, "public Task SetFrameTimeOverlayVisibleAsync(bool visible, CancellationToken cancellationToken = default)");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.AutomationStatsUi.cs")),
            "MainViewModel stats UI automation partial");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.AutomationUi.cs")),
            "MainViewModel.AutomationUi.cs folded into MainViewModel.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.AutomationCommands.cs")),
            "MainViewModel.AutomationCommands.cs folded into MainViewModel.cs");
        return Task.CompletedTask;
    }

    internal static Task AutomationUiSettings_PersistThroughSettingsPath()
    {
        var settingsPersistenceText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.SettingsPersistence.cs").Replace("\r\n", "\n");
        var settingsLoadApplicationText = settingsPersistenceText;
        var settingsProjectionText = settingsPersistenceText[..settingsPersistenceText.IndexOf("public partial class MainViewModel", StringComparison.Ordinal)];
        var captureModeTransactionsText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.CaptureSelection.cs").Replace("\r\n", "\n");
        var settingsServiceText = ReadRepoFile("Sussudio/Services/Runtime/RuntimeHelpers.cs").Replace("\r\n", "\n");

        AssertContains(settingsServiceText, "public bool? IsStatsVisible { get; set; }");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Runtime", "SettingsService.cs")),
            "SettingsService.cs folded into RuntimeHelpers.cs");
        AssertContains(settingsPersistenceText, "private void LoadSettings()");
        AssertContains(settingsPersistenceText, "private void SaveSettings()");
        AssertContains(settingsPersistenceText, "SettingsService.Load()");
        AssertContains(settingsPersistenceText, "SettingsService.Save(settings)");
        AssertContains(settingsPersistenceText, "Directory.Exists");
        AssertContains(settingsPersistenceText, "_isLoadingSettings = true;");
        AssertContains(settingsPersistenceText, "_isLoadingSettings = false;");
        AssertContains(settingsPersistenceText, "MainViewModelSettingsPersistenceProjection.BuildLoadPlan(");
        AssertContains(settingsPersistenceText, "MainViewModelSettingsPersistenceProjection.BuildSaveSettings(");
        AssertContains(settingsPersistenceText, "private void ApplySettingsLoadPlan(MainViewModelSettingsLoadPlan loadPlan)");
        AssertContains(settingsPersistenceText, "ApplyRecordingSettingsLoadPlan(loadPlan);");
        AssertContains(settingsPersistenceText, "ApplyAudioSettingsLoadPlan(loadPlan);");
        AssertContains(settingsPersistenceText, "ApplyUiSettingsLoadPlan(loadPlan);");
        AssertContains(settingsPersistenceText, "ApplyDeviceAudioSettingsLoadPlan(loadPlan);");
        AssertContains(settingsPersistenceText, "ApplyFlashbackSettingsLoadPlan(loadPlan);");
        AssertContains(settingsPersistenceText, "StageDeferredDeviceSettingsLoadPlan(loadPlan);");
        AssertOccursBefore(settingsPersistenceText, "ApplyRecordingSettingsLoadPlan(loadPlan);", "ApplyAudioSettingsLoadPlan(loadPlan);");
        AssertOccursBefore(settingsPersistenceText, "ApplyAudioSettingsLoadPlan(loadPlan);", "ApplyUiSettingsLoadPlan(loadPlan);");
        AssertOccursBefore(settingsPersistenceText, "ApplyUiSettingsLoadPlan(loadPlan);", "ApplyDeviceAudioSettingsLoadPlan(loadPlan);");
        AssertOccursBefore(settingsPersistenceText, "ApplyDeviceAudioSettingsLoadPlan(loadPlan);", "ApplyFlashbackSettingsLoadPlan(loadPlan);");
        AssertOccursBefore(settingsPersistenceText, "ApplyFlashbackSettingsLoadPlan(loadPlan);", "StageDeferredDeviceSettingsLoadPlan(loadPlan);");
        AssertContains(settingsLoadApplicationText, "private void ApplyRecordingSettingsLoadPlan(MainViewModelSettingsLoadPlan loadPlan)");
        AssertContains(settingsLoadApplicationText, "private void ApplyAudioSettingsLoadPlan(MainViewModelSettingsLoadPlan loadPlan)");
        AssertContains(settingsLoadApplicationText, "private void ApplyUiSettingsLoadPlan(MainViewModelSettingsLoadPlan loadPlan)");
        AssertContains(settingsLoadApplicationText, "private void ApplyDeviceAudioSettingsLoadPlan(MainViewModelSettingsLoadPlan loadPlan)");
        AssertContains(settingsLoadApplicationText, "private void ApplyFlashbackSettingsLoadPlan(MainViewModelSettingsLoadPlan loadPlan)");
        AssertContains(settingsLoadApplicationText, "private void StageDeferredDeviceSettingsLoadPlan(MainViewModelSettingsLoadPlan loadPlan)");
        AssertContains(settingsLoadApplicationText, "_pendingSavedDeviceId = loadPlan.PendingDeviceId;");
        AssertContains(settingsLoadApplicationText, "_pendingSavedAudioDeviceId = loadPlan.PendingAudioDeviceId;");
        AssertContains(settingsLoadApplicationText, "_pendingSavedMicrophoneDeviceId = loadPlan.PendingMicrophoneDeviceId;");
        foreach (var removedFile in new[]
        {
            "MainViewModel.SettingsLoadApplication.cs",
            "MainViewModel.SettingsLoadApplication.Recording.cs",
            "MainViewModel.SettingsLoadApplication.Audio.cs",
            "MainViewModel.SettingsLoadApplication.Ui.cs",
            "MainViewModel.SettingsLoadApplication.DeviceAudio.cs",
            "MainViewModel.SettingsLoadApplication.Flashback.cs",
            "MainViewModel.SettingsLoadApplication.PendingDevices.cs"
        })
        {
            AssertEqual(
                false,
                File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", removedFile)),
                $"{removedFile} folded into MainViewModel.SettingsPersistence.cs");
        }
        AssertContains(settingsProjectionText, "internal static class MainViewModelSettingsPersistenceProjection");
        AssertContains(settingsProjectionText, "internal static MainViewModelSettingsLoadPlan BuildLoadPlan(");
        AssertContains(settingsProjectionText, "internal static UserSettings BuildSaveSettings(");
        AssertContains(settingsProjectionText, "internal readonly record struct MainViewModelSettingsLoadInput(");
        AssertContains(settingsProjectionText, "internal readonly record struct MainViewModelSettingsLoadPlan(");
        AssertContains(settingsProjectionText, "internal readonly record struct MainViewModelSettingsSaveInput(");
        foreach (var removedProjectionFile in new[]
        {
            "MainViewModelSettingsPersistenceProjection.cs",
            "MainViewModelSettingsPersistenceProjection.Load.cs",
            "MainViewModelSettingsPersistenceProjection.Save.cs",
            "MainViewModelSettingsPersistenceProjection.Models.cs"
        })
        {
            AssertEqual(
                false,
                File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", removedProjectionFile)),
                $"{removedProjectionFile} folded into MainViewModel.SettingsPersistence.cs");
        }
        AssertDoesNotContain(settingsProjectionText, "SettingsService");
        AssertDoesNotContain(settingsProjectionText, "Logger");
        AssertDoesNotContain(settingsProjectionText, "Directory.");
        AssertDoesNotContain(settingsProjectionText, "MainViewModel.");
        AssertContains(settingsProjectionText, "IsStatsVisible: settings.IsStatsVisible,");
        AssertContains(settingsProjectionText, "IsStatsVisible = input.IsStatsVisible,");
        AssertContains(settingsProjectionText, "Math.Clamp(settings.PreviewVolume.Value, 0.0, 1.0)");
        AssertContains(settingsProjectionText, "Math.Clamp(settings.FlashbackBufferMinutes.Value, 1, 30)");
        AssertContains(settingsProjectionText, "ResolveAvailableValue(");
        AssertDoesNotContain(settingsPersistenceText, "if (settings.ShowAllCaptureOptions.HasValue)");
        AssertDoesNotContain(settingsPersistenceText, "if (settings.IsStatsVisible.HasValue)");
        AssertContains(settingsPersistenceText, "partial void OnIsStatsVisibleChanged(bool value)");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.Settings.cs")),
            "old settings pass-through partial removed");
        AssertDoesNotContain(settingsPersistenceText, "RebuildResolutionOptions();\n        SaveSettings();");

        return Task.CompletedTask;
    }

    internal static Task SettingsPersistenceProjection_LoadPlanPreservesSavedSemantics()
    {
        var settings = CreateSettings(
            ("SelectedDeviceId", "device-1"),
            ("OutputPath", "C:\\Rejected"),
            ("SelectedRecordingFormat", "AV1"),
            ("SelectedQuality", "High"),
            ("SelectedPreset", "P7"),
            ("SelectedSplitEncodeMode", "Auto"),
            ("CustomBitrateMbps", 42d),
            ("IsHdrEnabled", true),
            ("IsAudioEnabled", false),
            ("IsAudioPreviewEnabled", true),
            ("IsCustomAudioInputEnabled", true),
            ("SelectedAudioInputDeviceId", "audio-1"),
            ("IsMicrophoneEnabled", true),
            ("SelectedMicrophoneDeviceId", "mic-1"),
            ("MicrophoneVolume", 150d),
            ("PreviewVolume", -0.25d),
            ("IsStatsVisible", false),
            ("SelectedDeviceAudioMode", "Analog"),
            ("AnalogAudioGainPercent", -5d),
            ("FlashbackGpuDecode", true),
            ("FlashbackBufferMinutes", 99));

        var plan = BuildSettingsLoadPlan(
            settings,
            availableRecordingFormats: new[] { "H264", "HEVC" },
            outputDirectoryExists: path => path == "C:\\Accepted");

        AssertEqual(null, GetPropertyValue(plan, "OutputPath"), "settings load invalid output path");
        AssertEqual(null, GetPropertyValue(plan, "SelectedRecordingFormat"), "settings load unavailable recording format");
        AssertEqual("AV1", GetPropertyValue(plan, "UnavailableRecordingFormat"), "settings load unavailable recording format marker");
        AssertEqual("High", GetPropertyValue(plan, "SelectedQuality"), "settings load selected quality");
        AssertEqual("P7", GetPropertyValue(plan, "SelectedPreset"), "settings load selected preset");
        AssertEqual("Auto", GetPropertyValue(plan, "SelectedSplitEncodeMode"), "settings load selected split encode mode");
        AssertEqual(42d, GetPropertyValue(plan, "CustomBitrateMbps"), "settings load custom bitrate");
        AssertEqual(true, GetPropertyValue(plan, "IsHdrEnabled"), "settings load hdr enabled");
        AssertEqual(false, GetPropertyValue(plan, "IsAudioEnabled"), "settings load audio enabled");
        AssertEqual(true, GetPropertyValue(plan, "IsAudioPreviewEnabled"), "settings load audio preview enabled");
        AssertEqual(true, GetPropertyValue(plan, "IsCustomAudioInputEnabled"), "settings load custom audio input enabled");
        AssertEqual(true, GetPropertyValue(plan, "IsMicrophoneEnabled"), "settings load microphone enabled");
        AssertEqual(100d, GetPropertyValue(plan, "MicrophoneVolume"), "settings load microphone volume clamp");
        AssertEqual("mic-1", GetPropertyValue(plan, "PendingMicrophoneVolumeDeviceId"), "settings load microphone volume device");
        AssertEqual(0d, GetPropertyValue(plan, "PreviewVolume"), "settings load preview volume clamp");
        AssertEqual(false, GetPropertyValue(plan, "IsStatsVisible"), "settings load stats visible");
        AssertEqual("Analog", GetPropertyValue(plan, "SelectedDeviceAudioMode"), "settings load selected device audio mode");
        AssertEqual(0d, GetPropertyValue(plan, "AnalogAudioGainPercent"), "settings load analog gain clamp");
        AssertEqual(true, GetPropertyValue(plan, "FlashbackGpuDecode"), "settings load flashback gpu decode");
        AssertEqual(30, GetPropertyValue(plan, "FlashbackBufferMinutes"), "settings load flashback buffer clamp");
        AssertEqual("device-1", GetPropertyValue(plan, "PendingDeviceId"), "settings load pending device");
        AssertEqual("audio-1", GetPropertyValue(plan, "PendingAudioDeviceId"), "settings load pending audio device");
        AssertEqual("mic-1", GetPropertyValue(plan, "PendingMicrophoneDeviceId"), "settings load pending microphone device");
        AssertEqual("Analog", GetPropertyValue(plan, "PendingDeviceAudioMode"), "settings load pending audio mode");
        AssertEqual(-5d, GetPropertyValue(plan, "PendingAnalogAudioGainPercent"), "settings load pending analog gain");

        return Task.CompletedTask;
    }

    internal static Task SettingsPersistenceProjection_SaveSettingsMapsPersistedValues()
    {
        var settings = BuildSettingsSaveSettings(
            selectedDeviceId: "device-2",
            outputPath: "C:\\Capture",
            selectedRecordingFormat: "HEVC",
            selectedQuality: "Balanced",
            selectedPreset: "P5",
            selectedSplitEncodeMode: "Disabled",
            customBitrateMbps: 55d,
            isHdrEnabled: true,
            isAudioEnabled: true,
            isAudioPreviewEnabled: false,
            isCustomAudioInputEnabled: true,
            selectedAudioInputDeviceId: "audio-2",
            isMicrophoneEnabled: true,
            selectedMicrophoneDeviceId: "mic-2",
            microphoneVolume: 75d,
            previewVolume: 0.625d,
            isStatsVisible: true,
            selectedDeviceAudioMode: "Embedded",
            analogAudioGainPercent: 33d,
            flashbackGpuDecode: false,
            flashbackBufferMinutes: 12);

        AssertEqual("device-2", GetPropertyValue(settings, "SelectedDeviceId"), "settings save selected device");
        AssertEqual("C:\\Capture", GetPropertyValue(settings, "OutputPath"), "settings save output path");
        AssertEqual("HEVC", GetPropertyValue(settings, "SelectedRecordingFormat"), "settings save recording format");
        AssertEqual("Balanced", GetPropertyValue(settings, "SelectedQuality"), "settings save quality");
        AssertEqual("P5", GetPropertyValue(settings, "SelectedPreset"), "settings save preset");
        AssertEqual("Disabled", GetPropertyValue(settings, "SelectedSplitEncodeMode"), "settings save split encode mode");
        AssertEqual(55d, GetPropertyValue(settings, "CustomBitrateMbps"), "settings save custom bitrate");
        AssertEqual(true, GetPropertyValue(settings, "IsHdrEnabled"), "settings save hdr enabled");
        AssertEqual(true, GetPropertyValue(settings, "IsAudioEnabled"), "settings save audio enabled");
        AssertEqual(false, GetPropertyValue(settings, "IsAudioPreviewEnabled"), "settings save audio preview enabled");
        AssertEqual(true, GetPropertyValue(settings, "IsCustomAudioInputEnabled"), "settings save custom audio input enabled");
        AssertEqual("audio-2", GetPropertyValue(settings, "SelectedAudioInputDeviceId"), "settings save selected audio input");
        AssertEqual(true, GetPropertyValue(settings, "IsMicrophoneEnabled"), "settings save microphone enabled");
        AssertEqual("mic-2", GetPropertyValue(settings, "SelectedMicrophoneDeviceId"), "settings save selected microphone");
        AssertEqual(75d, GetPropertyValue(settings, "MicrophoneVolume"), "settings save microphone volume");
        AssertEqual(0.625d, GetPropertyValue(settings, "PreviewVolume"), "settings save preview volume");
        AssertEqual(true, GetPropertyValue(settings, "IsStatsVisible"), "settings save stats visible");
        AssertEqual("Embedded", GetPropertyValue(settings, "SelectedDeviceAudioMode"), "settings save selected device audio mode");
        AssertEqual(33d, GetPropertyValue(settings, "AnalogAudioGainPercent"), "settings save analog gain");
        AssertEqual(false, GetPropertyValue(settings, "FlashbackGpuDecode"), "settings save flashback gpu decode");
        AssertEqual(12, GetPropertyValue(settings, "FlashbackBufferMinutes"), "settings save flashback buffer minutes");

        return Task.CompletedTask;
    }

    internal static Task AutomationCaptureModeChanges_AwaitReinitialization()
    {
        var viewModelStateText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs").Replace("\r\n", "\n");
        var automationSettingsText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs").Replace("\r\n", "\n");
        var captureSettingsAutomationControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelSettingsAutomationControllers.cs").Replace("\r\n", "\n");
        var captureModeTransactionsText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.CaptureSelection.cs").Replace("\r\n", "\n");

        AssertDoesNotContain(viewModelStateText, "private readonly SemaphoreSlim _automationCaptureModeGate = new(1, 1);");
        AssertContains(automationSettingsText, "public Task SetResolutionAsync(string resolution, CancellationToken cancellationToken = default)");
        AssertContains(automationSettingsText, "=> _captureSettingsAutomationController.SetResolutionAsync(resolution, cancellationToken);");
        AssertContains(automationSettingsText, "public Task SetFrameRateAsync(double frameRate, CancellationToken cancellationToken = default)");
        AssertContains(automationSettingsText, "=> _captureSettingsAutomationController.SetFrameRateAsync(frameRate, cancellationToken);");
        AssertContains(automationSettingsText, "public Task SetVideoFormatAsync(string videoFormat, CancellationToken cancellationToken = default)");
        AssertContains(automationSettingsText, "=> _captureSettingsAutomationController.SetVideoFormatAsync(videoFormat, cancellationToken);");
        AssertContains(automationSettingsText, "public Task SetMjpegDecoderCountAsync(int decoderCount, CancellationToken cancellationToken = default)");
        AssertContains(automationSettingsText, "=> _captureSettingsAutomationController.SetMjpegDecoderCountAsync(decoderCount, cancellationToken);");
        AssertDoesNotContain(automationSettingsText, "private async Task SetAutomationCaptureModeAsync(");
        AssertContains(captureSettingsAutomationControllerText, "namespace Sussudio.Controllers;");
        AssertContains(captureSettingsAutomationControllerText, "internal sealed class MainViewModelCaptureSettingsAutomationController");
        AssertContains(captureSettingsAutomationControllerText, "internal sealed class MainViewModelCaptureSettingsAutomationControllerContext");
        AssertContains(captureSettingsAutomationControllerText, "private readonly MainViewModelCaptureSettingsAutomationControllerContext _context;");
        AssertDoesNotContain(captureSettingsAutomationControllerText, "private readonly MainViewModel _viewModel;");
        AssertDoesNotContain(captureSettingsAutomationControllerText, "_viewModel.");
        AssertEqual(
            true,
            captureSettingsAutomationControllerText.Split('\n').Length >= 100,
            "capture settings automation controller is a substantial ownership file");
        AssertContains(captureSettingsAutomationControllerText, "private readonly SemaphoreSlim _captureModeGate = new(1, 1);");
        AssertContains(captureSettingsAutomationControllerText, "public Task SetResolutionAsync(string resolution, CancellationToken cancellationToken = default)");
        AssertContains(captureSettingsAutomationControllerText, "return SetAutomationCaptureModeAsync(\"resolution\"");
        AssertContains(captureSettingsAutomationControllerText, "public Task SetFrameRateAsync(double frameRate, CancellationToken cancellationToken = default)");
        AssertContains(captureSettingsAutomationControllerText, "return SetAutomationCaptureModeAsync(\"frame rate\"");
        AssertContains(captureSettingsAutomationControllerText, "FrameRateTimingPolicy.IsAutoFrameRateValue(frameRate)");
        AssertContains(captureSettingsAutomationControllerText, "_context.SetSelectedFrameRate(matched.Value);");
        AssertContains(captureSettingsAutomationControllerText, "public Task SetVideoFormatAsync(string videoFormat, CancellationToken cancellationToken = default)");
        AssertContains(captureSettingsAutomationControllerText, "return SetAutomationCaptureModeAsync(\"video format\"");
        AssertContains(captureSettingsAutomationControllerText, "_context.SetSelectedVideoFormat(match);");
        AssertContains(captureSettingsAutomationControllerText, "public Task SetMjpegDecoderCountAsync(int decoderCount, CancellationToken cancellationToken = default)");
        AssertContains(captureSettingsAutomationControllerText, "return SetAutomationCaptureModeAsync(\"mjpeg decoder count\"");
        AssertContains(captureSettingsAutomationControllerText, "_context.SetMjpegDecoderCount(Math.Clamp(decoderCount, 1, 8));");
        AssertContains(captureSettingsAutomationControllerText, "private async Task SetAutomationCaptureModeAsync(");
        AssertContains(captureSettingsAutomationControllerText, "await _captureModeGate.WaitAsync(cancellationToken).ConfigureAwait(false);");
        AssertContains(captureSettingsAutomationControllerText, "_context.SetSuppressFormatChangeReinitialize(true);");
        AssertContains(captureSettingsAutomationControllerText, "_context.SetSuppressFormatChangeReinitialize(false);");
        AssertContains(captureSettingsAutomationControllerText, "return wasPreviewing && _context.GetSelectedFormat() != null;");
        AssertContains(captureSettingsAutomationControllerText, "ReinitializeDeviceAsync($\"automation {reason}\")");
        AssertContains(captureSettingsAutomationControllerText, "_captureModeGate.Release();");
        AssertDoesNotContain(captureModeTransactionsText, "_automationCaptureModeGate");
        AssertDoesNotContain(captureModeTransactionsText, "SetAutomationCaptureModeAsync(");
        foreach (var stalePath in new[]
        {
            "MainViewModel.AutomationSettings.cs",
            "MainViewModel.AutomationDeviceSelection.cs",
            "MainViewModel.AutomationCaptureMode.cs",
            "MainViewModel.AutomationCaptureModeGate.cs",
            "MainViewModel.AutomationFrameRate.cs",
            "MainViewModel.AutomationVideoFormat.cs",
            "MainViewModel.AutomationMjpegDecoderCount.cs",
            "MainViewModel.CaptureOptionVisibility.cs",
            "MainViewModel.HdrModeChanges.cs",
            "MainViewModel.AutomationCaptureSettings.cs"
        })
        {
            AssertEqual(
                false,
                File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", stalePath)),
                $"stale capture settings automation partial {stalePath}");
        }

        return Task.CompletedTask;
    }

    internal static Task AutomationDeviceSelection_RoutesThroughApplyReinit()
    {
        var deviceSelectionAutomationText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs").Replace("\r\n", "\n");
        var rootViewModelText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs")
            .Replace("\r\n", "\n");
        var deviceRefreshControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelDeviceControllers.cs")
            .Replace("\r\n", "\n");
        var selectDevice = ExtractTextBetween(
            deviceSelectionAutomationText,
            "public Task SelectDeviceAsync",
            "public Task SelectAudioInputDeviceAsync");
        var selectAudioDevice = ExtractTextBetween(
            deviceSelectionAutomationText,
            "public Task SelectAudioInputDeviceAsync",
            "public Task SetCustomAudioInputEnabledAsync");

        AssertContains(deviceSelectionAutomationText, "public Task RefreshDevicesForAutomationAsync");
        AssertContains(deviceSelectionAutomationText, "=> InvokeOnUiThreadAsync(() => RefreshDevicesAsync(cancellationToken), cancellationToken);");
        AssertContains(deviceSelectionAutomationText, "public Task SelectDeviceAsync");
        AssertContains(deviceSelectionAutomationText, "private CaptureDevice? ResolveDevice");
        AssertContains(deviceSelectionAutomationText, "public Task SelectAudioInputDeviceAsync");
        AssertContains(deviceSelectionAutomationText, "public Task SetCustomAudioInputEnabledAsync");
        AssertContains(deviceSelectionAutomationText, "private AudioInputDevice? ResolveAudioDevice");
        AssertContains(rootViewModelText, "public Task RefreshDevicesAsync(CancellationToken cancellationToken = default)");
        AssertContains(rootViewModelText, "=> _deviceRefreshController.RefreshDevicesAsync(cancellationToken);");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.DeviceManagement.cs")),
            "shallow MainViewModel device-management partial");
        AssertContains(deviceRefreshControllerText, "namespace Sussudio.Controllers;");
        AssertContains(deviceRefreshControllerText, "internal sealed class MainViewModelDeviceRefreshController");
        AssertContains(deviceRefreshControllerText, "internal sealed class MainViewModelDeviceRefreshControllerContext");
        AssertContains(deviceRefreshControllerText, "private readonly MainViewModelDeviceRefreshControllerContext _context;");
        AssertDoesNotContain(deviceRefreshControllerText, "private readonly MainViewModel _viewModel;");
        AssertDoesNotContain(deviceRefreshControllerText, "_viewModel.");
        AssertContains(deviceRefreshControllerText, "catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)\n        {\n            _context.SetStatusText(\"Device scan canceled\");\n            throw;\n        }");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.AutomationAudioInputSelection.cs")),
            "MainViewModel audio input automation partial");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.AutomationDeviceSelection.cs")),
            "MainViewModel device selection automation partial folded into MainViewModel.cs");
        AssertContains(selectDevice, "return InvokeOnUiThreadAsync(async () =>");
        AssertContains(selectDevice, "await ApplySelectedDeviceAsync(target, cancellationToken).ConfigureAwait(true);");
        AssertDoesNotContain(selectDevice, "SelectedDevice = target;");
        AssertContains(selectAudioDevice, "SelectedAudioInputDevice = target;");

        return Task.CompletedTask;
    }

    internal static Task MainViewModelAutomation_HdrEnablementLivesInCaptureSelection()
    {
        var captureModeTransactionsText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.CaptureSelection.cs")
            .Replace("\r\n", "\n");
        var hdrChangeBlock = ExtractMemberCode(
            captureModeTransactionsText,
            "OnIsHdrEnabledChanged");

        AssertContains(captureModeTransactionsText, "public Task SetHdrEnabledAsync(bool enabled, CancellationToken cancellationToken = default)");
        AssertContains(captureModeTransactionsText, "throw new InvalidOperationException(HdrToggleBlockedWhileRecordingMessage);");
        AssertContains(captureModeTransactionsText, "if (enabled && !IsHdrAvailable)");
        AssertContains(captureModeTransactionsText, "throw new InvalidOperationException(\"HDR is not available on the selected device.\");");
        AssertContains(captureModeTransactionsText, "IsHdrEnabled = enabled;");
        AssertContains(captureModeTransactionsText, "public Task SetTrueHdrPreviewEnabledAsync(bool enabled, CancellationToken cancellationToken = default)");
        AssertContains(captureModeTransactionsText, "throw new InvalidOperationException(\"True HDR preview cannot be changed while recording.\");");
        AssertContains(captureModeTransactionsText, "IsTrueHdrPreviewEnabled = enabled;");
        AssertContains(captureModeTransactionsText, "partial void OnIsHdrEnabledChanged(bool value)");
        AssertContains(captureModeTransactionsText, "if (_isRevertingHdrToggle)");
        AssertContains(captureModeTransactionsText, "_pendingSdrAutoSelectionForDeviceChange = false;");
        AssertContains(captureModeTransactionsText, "_pendingSdrAutoFriendlyFrameRateBucket = null;");
        AssertContains(captureModeTransactionsText, "IsHdrEnabled = !value;");
        AssertContains(captureModeTransactionsText, "StatusText = HdrToggleBlockedWhileRecordingMessage;");
        AssertContains(captureModeTransactionsText, "ResetModeSelectionState();");
        AssertContains(captureModeTransactionsText, "RebuildResolutionOptions();");
        AssertContains(captureModeTransactionsText, "RebuildRecordingFormatOptions();");
        AssertContains(captureModeTransactionsText, "EnqueueUiOperation(() => ReinitializeDeviceAsync(\"HDR toggle\"), \"hdr toggle reinitialize\");");
        AssertContains(captureModeTransactionsText, "SaveSettings();");
        AssertOccursBefore(hdrChangeBlock, "if (_isRevertingHdrToggle)", "if (value)");
        AssertOccursBefore(hdrChangeBlock, "if (value)", "if (IsRecording)");
        AssertOccursBefore(hdrChangeBlock, "StatusText = HdrToggleBlockedWhileRecordingMessage;", "if (!_isChangingDevice)");
        AssertOccursBefore(hdrChangeBlock, "ResetModeSelectionState();", "RebuildResolutionOptions();");
        AssertOccursBefore(hdrChangeBlock, "RebuildResolutionOptions();", "RebuildRecordingFormatOptions();");
        AssertOccursBefore(hdrChangeBlock, "RebuildRecordingFormatOptions();", "EnqueueUiOperation(() => ReinitializeDeviceAsync(\"HDR toggle\"), \"hdr toggle reinitialize\");");
        AssertOccursBefore(hdrChangeBlock, "EnqueueUiOperation(() => ReinitializeDeviceAsync(\"HDR toggle\"), \"hdr toggle reinitialize\");", "SaveSettings();");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.AutomationHdr.cs")),
            "MainViewModel HDR automation partial");

        return Task.CompletedTask;
    }

    private static object CreateSettings(params (string Property, object? Value)[] values)
    {
        var settings = CreateInstance("Sussudio.Services.Runtime.UserSettings");
        foreach (var (property, value) in values)
        {
            SetPropertyOrBackingField(settings, property, value);
        }

        return settings;
    }

    private static object BuildSettingsLoadPlan(
        object settings,
        string[] availableRecordingFormats,
        Func<string, bool> outputDirectoryExists)
    {
        var inputType = RequireType("Sussudio.ViewModels.MainViewModelSettingsLoadInput");
        var input = InvokeSingleConstructor(inputType,
            availableRecordingFormats,
            new[] { "High", "Balanced" },
            new[] { "P7", "P5" },
            new[] { "Auto", "Disabled" },
            new[] { "Embedded", "Analog" },
            outputDirectoryExists);

        var projectionType = RequireType("Sussudio.ViewModels.MainViewModelSettingsPersistenceProjection");
        var buildLoadPlan = projectionType.GetMethod(
            "BuildLoadPlan",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("BuildLoadPlan was not found.");

        return buildLoadPlan.Invoke(null, new[] { settings, input })
               ?? throw new InvalidOperationException("BuildLoadPlan returned null.");
    }

    private static object BuildSettingsSaveSettings(
        string? selectedDeviceId,
        string outputPath,
        string selectedRecordingFormat,
        string selectedQuality,
        string selectedPreset,
        string selectedSplitEncodeMode,
        double customBitrateMbps,
        bool isHdrEnabled,
        bool isAudioEnabled,
        bool isAudioPreviewEnabled,
        bool isCustomAudioInputEnabled,
        string? selectedAudioInputDeviceId,
        bool isMicrophoneEnabled,
        string? selectedMicrophoneDeviceId,
        double microphoneVolume,
        double previewVolume,
        bool isStatsVisible,
        string selectedDeviceAudioMode,
        double analogAudioGainPercent,
        bool flashbackGpuDecode,
        int flashbackBufferMinutes)
    {
        var inputType = RequireType("Sussudio.ViewModels.MainViewModelSettingsSaveInput");
        var input = InvokeSingleConstructor(inputType,
            selectedDeviceId,
            outputPath,
            selectedRecordingFormat,
            selectedQuality,
            selectedPreset,
            selectedSplitEncodeMode,
            customBitrateMbps,
            isHdrEnabled,
            isAudioEnabled,
            isAudioPreviewEnabled,
            isCustomAudioInputEnabled,
            selectedAudioInputDeviceId,
            isMicrophoneEnabled,
            selectedMicrophoneDeviceId,
            microphoneVolume,
            previewVolume,
            isStatsVisible,
            selectedDeviceAudioMode,
            analogAudioGainPercent,
            flashbackGpuDecode,
            flashbackBufferMinutes);

        var projectionType = RequireType("Sussudio.ViewModels.MainViewModelSettingsPersistenceProjection");
        var buildSaveSettings = projectionType.GetMethod(
            "BuildSaveSettings",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("BuildSaveSettings was not found.");

        return buildSaveSettings.Invoke(null, new[] { input })
               ?? throw new InvalidOperationException("BuildSaveSettings returned null.");
    }

    private static object InvokeSingleConstructor(Type type, params object?[] arguments)
    {
        var constructor = type.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Single(candidate => candidate.GetParameters().Length == arguments.Length);

        return constructor.Invoke(arguments);
    }
}

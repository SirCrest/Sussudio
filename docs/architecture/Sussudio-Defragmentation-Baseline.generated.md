# Sussudio Defragmentation Baseline - Generated

Generated UTC: 2026-05-22T00:25:16Z
Root: C:\Users\crest\source\repos\Sussudio-cleanup-architecture

## Summary

| Metric | Value |
| --- | ---: |
| Production .cs files | 1792 |
| Test .cs files | 552 |
| Production .cs files under 60 lines | 912 (50.9%) |
| Production .cs files under 80 lines | 1147 (64.0%) |

## Largest partial-type clusters

| Type | Files | Total lines | Sample paths |
| --- | ---: | ---: | --- |
| AutomationDiagnosticsHub | 216 | 11027 | Sussudio/Services/Automation/AutomationDiagnosticsHub.Alerts.cs, Sussudio/Services/Automation/AutomationDiagnosticsHub.Counters.FlashbackRecording.cs, Sussudio/Services/Automation/AutomationDiagnosticsHub.Counters.Mjpeg.cs, Sussudio/Services/Automation/AutomationDiagnosticsHub.Counters.RealtimePreview.cs, Sussudio/Services/Automation/AutomationDiagnosticsHub.cs, Sussudio/Services/Automation/AutomationDiagnosticsHub.DiagnosticEvaluation.cs, Sussudio/Services/Automation/AutomationDiagnosticsHub.DiagnosticEvaluationFlashback.cs, Sussudio/Services/Automation/AutomationDiagnosticsHub.DiagnosticEvaluationFlashback.Export.cs |
| CaptureService | 109 | 10330 | Sussudio/Services/Capture/CaptureService.Audio.cs, Sussudio/Services/Capture/CaptureService.AudioInputSwitching.cs, Sussudio/Services/Capture/CaptureService.AudioPreviewLifecycle.cs, Sussudio/Services/Capture/CaptureService.CaptureFormatTelemetry.cs, Sussudio/Services/Capture/CaptureService.Cleanup.cs, Sussudio/Services/Capture/CaptureService.Coordination.cs, Sussudio/Services/Capture/CaptureService.cs, Sussudio/Services/Capture/CaptureService.DeferredCleanup.cs |
| MainWindow | 95 | 3069 | Sussudio/MainWindow.AudioBindings.cs, Sussudio/MainWindow.AudioMeter.cs, Sussudio/MainWindow.Bindings.cs, Sussudio/MainWindow.ButtonActions.cs, Sussudio/MainWindow.CaptureOptionBindings.cs, Sussudio/MainWindow.CaptureOptionPresentation.cs, Sussudio/MainWindow.CaptureSelectionBindings.AudioSelection.cs, Sussudio/MainWindow.CaptureSelectionBindings.CaptureMode.cs |
| MainViewModel | 81 | 4928 | Sussudio/Controllers/ViewModel/MainViewModelControllerGraph.CaptureModes.cs, Sussudio/Controllers/ViewModel/MainViewModelControllerGraph.CaptureSettingsAutomation.cs, Sussudio/Controllers/ViewModel/MainViewModelControllerGraph.cs, Sussudio/Controllers/ViewModel/MainViewModelControllerGraph.Device.cs, Sussudio/Controllers/ViewModel/MainViewModelControllerGraph.DeviceAudio.cs, Sussudio/Controllers/ViewModel/MainViewModelControllerGraph.DeviceFormatProbe.cs, Sussudio/Controllers/ViewModel/MainViewModelControllerGraph.Presentation.cs, Sussudio/Controllers/ViewModel/MainViewModelControllerGraph.Recording.cs |
| FlashbackPlaybackController | 63 | 5424 | Sussudio/Services/Flashback/FlashbackPlaybackController.AudioCallback.cs, Sussudio/Services/Flashback/FlashbackPlaybackController.AudioMasterClock.cs, Sussudio/Services/Flashback/FlashbackPlaybackController.AudioMasterFallbacks.cs, Sussudio/Services/Flashback/FlashbackPlaybackController.AudioMasterPacing.cs, Sussudio/Services/Flashback/FlashbackPlaybackController.AudioPrebuffer.cs, Sussudio/Services/Flashback/FlashbackPlaybackController.AudioPreviewGuards.cs, Sussudio/Services/Flashback/FlashbackPlaybackController.AudioRouting.cs, Sussudio/Services/Flashback/FlashbackPlaybackController.CommandCoalescing.cs |
| D3D11PreviewRenderer | 56 | 4912 | Sussudio/Services/Preview/D3D11PreviewRenderer.Configuration.cs, Sussudio/Services/Preview/D3D11PreviewRenderer.cs, Sussudio/Services/Preview/D3D11PreviewRenderer.DeviceInitialization.cs, Sussudio/Services/Preview/D3D11PreviewRenderer.DeviceLost.cs, Sussudio/Services/Preview/D3D11PreviewRenderer.DisplayClock.cs, Sussudio/Services/Preview/D3D11PreviewRenderer.DxgiFrameStatistics.cs, Sussudio/Services/Preview/D3D11PreviewRenderer.FirstFrameNotifications.cs, Sussudio/Services/Preview/D3D11PreviewRenderer.FrameLatency.cs |
| FlashbackEncoderSink | 39 | 2917 | Sussudio/Services/Flashback/FlashbackEncoderSink.AudioQueueSubmission.cs, Sussudio/Services/Flashback/FlashbackEncoderSink.cs, Sussudio/Services/Flashback/FlashbackEncoderSink.DiagnosticsReset.cs, Sussudio/Services/Flashback/FlashbackEncoderSink.DisposeLifecycle.cs, Sussudio/Services/Flashback/FlashbackEncoderSink.EncodingLoop.cs, Sussudio/Services/Flashback/FlashbackEncoderSink.EncodingProgress.cs, Sussudio/Services/Flashback/FlashbackEncoderSink.FileSessionHelpers.cs, Sussudio/Services/Flashback/FlashbackEncoderSink.ForceRotate.cs |
| Formatters | 39 | 1230 | tools/ssctl/Formatters.Common.cs, tools/ssctl/Formatters.Diagnostics.cs, tools/ssctl/Formatters.Memory.cs, tools/ssctl/Formatters.Options.cs, tools/ssctl/Formatters.Snapshot.Audio.cs, tools/ssctl/Formatters.Snapshot.AvSync.cs, tools/ssctl/Formatters.Snapshot.CaptureCadence.cs, tools/ssctl/Formatters.Snapshot.CaptureSettings.cs |
| AutomationSnapshotFormatter | 35 | 996 | tools/Common/AutomationSnapshotFormatter.Audio.cs, tools/Common/AutomationSnapshotFormatter.AvSync.cs, tools/Common/AutomationSnapshotFormatter.CaptureCadence.cs, tools/Common/AutomationSnapshotFormatter.CaptureSettings.cs, tools/Common/AutomationSnapshotFormatter.CoreSections.cs, tools/Common/AutomationSnapshotFormatter.cs, tools/Common/AutomationSnapshotFormatter.Diagnostics.cs, tools/Common/AutomationSnapshotFormatter.DisplayValues.cs |
| FlashbackExporter | 35 | 3259 | Sussudio/Services/Flashback/FlashbackExporter.Cancellation.cs, Sussudio/Services/Flashback/FlashbackExporter.cs, Sussudio/Services/Flashback/FlashbackExporter.Execution.cs, Sussudio/Services/Flashback/FlashbackExporter.ExportLock.cs, Sussudio/Services/Flashback/FlashbackExporter.LibAvErrors.cs, Sussudio/Services/Flashback/FlashbackExporter.Lifetime.cs, Sussudio/Services/Flashback/FlashbackExporter.NativeState.cs, Sussudio/Services/Flashback/FlashbackExporter.OutputFiles.cs |
| DiagnosticSessionResultBuilder | 27 | 1456 | tools/Common/DiagnosticSessionResultBuilder.Analysis.cs, tools/Common/DiagnosticSessionResultBuilder.AnalysisValidation.cs, tools/Common/DiagnosticSessionResultBuilder.CaptureResult.cs, tools/Common/DiagnosticSessionResultBuilder.cs, tools/Common/DiagnosticSessionResultBuilder.DiagnosticHealth.cs, tools/Common/DiagnosticSessionResultBuilder.DiagnosticHealthSourceWarnings.cs, tools/Common/DiagnosticSessionResultBuilder.DiagnosticHealthSummary.cs, tools/Common/DiagnosticSessionResultBuilder.DiagnosticHealthTolerance.cs |
| LibAvEncoder | 23 | 3004 | Sussudio/Services/Recording/LibAvEncoder.Audio.cs, Sussudio/Services/Recording/LibAvEncoder.AudioInitialization.cs, Sussudio/Services/Recording/LibAvEncoder.AudioQueue.cs, Sussudio/Services/Recording/LibAvEncoder.AudioSetup.cs, Sussudio/Services/Recording/LibAvEncoder.AudioSubmission.cs, Sussudio/Services/Recording/LibAvEncoder.AvSync.cs, Sussudio/Services/Recording/LibAvEncoder.CodecPolicy.cs, Sussudio/Services/Recording/LibAvEncoder.cs |
| DiagnosticSessionResultFormatter | 22 | 489 | tools/Common/DiagnosticSessionResultFormatter.Artifacts.cs, tools/Common/DiagnosticSessionResultFormatter.CaptureMode.cs, tools/Common/DiagnosticSessionResultFormatter.cs, tools/Common/DiagnosticSessionResultFormatter.Flashback.cs, tools/Common/DiagnosticSessionResultFormatter.FlashbackExport.cs, tools/Common/DiagnosticSessionResultFormatter.FlashbackPlayback.AudioMaster.cs, tools/Common/DiagnosticSessionResultFormatter.FlashbackPlayback.Cadence.cs, tools/Common/DiagnosticSessionResultFormatter.FlashbackPlayback.Commands.cs |
| AutomationCommandDispatcher | 22 | 1705 | Sussudio/Services/Automation/AutomationCommandDispatcher.Assertions.cs, Sussudio/Services/Automation/AutomationCommandDispatcher.AudioControlCommands.cs, Sussudio/Services/Automation/AutomationCommandDispatcher.Authorization.cs, Sussudio/Services/Automation/AutomationCommandDispatcher.CaptureControlCommands.cs, Sussudio/Services/Automation/AutomationCommandDispatcher.CommandParsing.cs, Sussudio/Services/Automation/AutomationCommandDispatcher.cs, Sussudio/Services/Automation/AutomationCommandDispatcher.CustomCommands.cs, Sussudio/Services/Automation/AutomationCommandDispatcher.DeviceCommands.cs |
| PerformanceTimelineTools | 21 | 891 | tools/McpServer/Tools/PerformanceTimelineTools.cs, tools/McpServer/Tools/PerformanceTimelineTools.Formatting.cs, tools/McpServer/Tools/PerformanceTimelineTools.Formatting.Flashback.cs, tools/McpServer/Tools/PerformanceTimelineTools.Formatting.Preview.cs, tools/McpServer/Tools/PerformanceTimelineTools.Rendering.cs, tools/McpServer/Tools/PerformanceTimelineTools.Rendering.Trend.cs, tools/McpServer/Tools/PerformanceTimelineTools.Rendering.Trend.Flashback.cs, tools/McpServer/Tools/PerformanceTimelineTools.Rendering.Trend.Flashback.Export.cs |
| NativeXuAtCommandProvider | 20 | 2698 | Sussudio/Services/Telemetry/NativeXuAtCommandProvider.AnalogGain.cs, Sussudio/Services/Telemetry/NativeXuAtCommandProvider.AtProtocol.cs, Sussudio/Services/Telemetry/NativeXuAtCommandProvider.AudioCommands.cs, Sussudio/Services/Telemetry/NativeXuAtCommandProvider.AudioSwitch.cs, Sussudio/Services/Telemetry/NativeXuAtCommandProvider.cs, Sussudio/Services/Telemetry/NativeXuAtCommandProvider.DeviceCommandReads.cs, Sussudio/Services/Telemetry/NativeXuAtCommandProvider.DeviceCommands.cs, Sussudio/Services/Telemetry/NativeXuAtCommandProvider.DiagnosticSummary.cs |
| CommandHandlers | 19 | 1091 | tools/ssctl/CommandHandlers.Arguments.cs, tools/ssctl/CommandHandlers.AutomationFlow.cs, tools/ssctl/CommandHandlers.CaptureControls.cs, tools/ssctl/CommandHandlers.Context.cs, tools/ssctl/CommandHandlers.cs, tools/ssctl/CommandHandlers.Device.cs, tools/ssctl/CommandHandlers.DiagnosticSession.cs, tools/ssctl/CommandHandlers.Flags.cs |
| AutomationSnapshot | 17 | 903 | Sussudio/Models/Automation/AutomationSnapshot.AudioIngest.cs, Sussudio/Models/Automation/AutomationSnapshot.CaptureCadence.cs, Sussudio/Models/Automation/AutomationSnapshot.CaptureFormat.cs, Sussudio/Models/Automation/AutomationSnapshot.cs, Sussudio/Models/Automation/AutomationSnapshot.FlashbackExport.cs, Sussudio/Models/Automation/AutomationSnapshot.FlashbackPlayback.cs, Sussudio/Models/Automation/AutomationSnapshot.FlashbackRecording.cs, Sussudio/Models/Automation/AutomationSnapshot.Hdr.cs |
| DiagnosticSessionFlashbackExportScenarios | 17 | 1007 | tools/Common/DiagnosticSessionFlashbackExportScenarios.Concurrent.cs, tools/Common/DiagnosticSessionFlashbackExportScenarios.DisableDuringExport.cs, tools/Common/DiagnosticSessionFlashbackExportScenarios.DisableDuringExportValidation.cs, tools/Common/DiagnosticSessionFlashbackExportScenarios.Playback.cs, tools/Common/DiagnosticSessionFlashbackExportScenarios.PlaybackFinalState.cs, tools/Common/DiagnosticSessionFlashbackExportScenarios.PlaybackPostExport.cs, tools/Common/DiagnosticSessionFlashbackExportScenarios.PlaybackPreExport.cs, tools/Common/DiagnosticSessionFlashbackExportScenarios.Range.cs |
| DiagnosticSessionFlashbackMetrics | 16 | 690 | tools/Common/DiagnosticSessionFlashbackMetrics.Export.cs, tools/Common/DiagnosticSessionFlashbackMetrics.ExportObservation.cs, tools/Common/DiagnosticSessionFlashbackMetrics.PlaybackObservation.AudioMaster.cs, tools/Common/DiagnosticSessionFlashbackMetrics.PlaybackObservation.cs, tools/Common/DiagnosticSessionFlashbackMetrics.PlaybackObservation.FrameDecode.cs, tools/Common/DiagnosticSessionFlashbackMetrics.PlaybackObservation.OnePercentLow.cs, tools/Common/DiagnosticSessionFlashbackMetrics.PlaybackObservation.Relevance.cs, tools/Common/DiagnosticSessionFlashbackMetrics.PlaybackResult.AudioMaster.cs |
| DiagnosticSessionFlashbackPreviewCycleScenarios | 15 | 756 | tools/Common/DiagnosticSessionFlashbackPreviewCycleScenarios.Flashback.cs, tools/Common/DiagnosticSessionFlashbackPreviewCycleScenarios.FlashbackExport.cs, tools/Common/DiagnosticSessionFlashbackPreviewCycleScenarios.FlashbackPreStop.cs, tools/Common/DiagnosticSessionFlashbackPreviewCycleScenarios.FlashbackRestartValidation.cs, tools/Common/DiagnosticSessionFlashbackPreviewCycleScenarios.FlashbackStopped.cs, tools/Common/DiagnosticSessionFlashbackPreviewCycleScenarios.Playback.cs, tools/Common/DiagnosticSessionFlashbackPreviewCycleScenarios.PlaybackExport.cs, tools/Common/DiagnosticSessionFlashbackPreviewCycleScenarios.PlaybackPreStop.cs |
| MfSourceReaderVideoCapture | 15 | 2220 | Sussudio/Services/Capture/MfSourceReaderVideoCapture.Cadence.cs, Sussudio/Services/Capture/MfSourceReaderVideoCapture.ConvertedMediaType.cs, Sussudio/Services/Capture/MfSourceReaderVideoCapture.cs, Sussudio/Services/Capture/MfSourceReaderVideoCapture.DeviceEnumeration.cs, Sussudio/Services/Capture/MfSourceReaderVideoCapture.Diagnostics.cs, Sussudio/Services/Capture/MfSourceReaderVideoCapture.DxgiBuffers.cs, Sussudio/Services/Capture/MfSourceReaderVideoCapture.FrameDelivery.cs, Sussudio/Services/Capture/MfSourceReaderVideoCapture.FrameLayout.cs |
| MainViewModelControllerGraph | 15 | 777 | Sussudio/Controllers/ViewModel/MainViewModelControllerGraph.CaptureModes.cs, Sussudio/Controllers/ViewModel/MainViewModelControllerGraph.CaptureSettingsAutomation.cs, Sussudio/Controllers/ViewModel/MainViewModelControllerGraph.cs, Sussudio/Controllers/ViewModel/MainViewModelControllerGraph.Device.cs, Sussudio/Controllers/ViewModel/MainViewModelControllerGraph.DeviceAudio.cs, Sussudio/Controllers/ViewModel/MainViewModelControllerGraph.DeviceFormatProbe.cs, Sussudio/Controllers/ViewModel/MainViewModelControllerGraph.Presentation.cs, Sussudio/Controllers/ViewModel/MainViewModelControllerGraph.Recording.cs |
| DiagnosticSessionResult | 15 | 285 | tools/Common/DiagnosticSessionResult.CaptureSource.cs, tools/Common/DiagnosticSessionResult.cs, tools/Common/DiagnosticSessionResult.FlashbackExport.cs, tools/Common/DiagnosticSessionResult.FlashbackPlayback.AudioMaster.cs, tools/Common/DiagnosticSessionResult.FlashbackPlayback.Cadence.cs, tools/Common/DiagnosticSessionResult.FlashbackPlayback.Commands.cs, tools/Common/DiagnosticSessionResult.FlashbackPlayback.Decode.cs, tools/Common/DiagnosticSessionResult.FlashbackPlayback.OnePercentLow.cs |
| FlashbackDecoder | 14 | 2010 | Sussudio/Services/Flashback/FlashbackDecoder.AudioOutput.cs, Sussudio/Services/Flashback/FlashbackDecoder.cs, Sussudio/Services/Flashback/FlashbackDecoder.D3D11.cs, Sussudio/Services/Flashback/FlashbackDecoder.D3D11Discovery.cs, Sussudio/Services/Flashback/FlashbackDecoder.DecodeLoop.cs, Sussudio/Services/Flashback/FlashbackDecoder.Diagnostics.cs, Sussudio/Services/Flashback/FlashbackDecoder.Guards.cs, Sussudio/Services/Flashback/FlashbackDecoder.Lifetime.cs |
| LibAvRecordingSink | 14 | 1691 | Sussudio/Services/Recording/LibAvRecordingSink.AudioQueues.cs, Sussudio/Services/Recording/LibAvRecordingSink.cs, Sussudio/Services/Recording/LibAvRecordingSink.Diagnostics.cs, Sussudio/Services/Recording/LibAvRecordingSink.EncodingLoop.cs, Sussudio/Services/Recording/LibAvRecordingSink.Lifetime.cs, Sussudio/Services/Recording/LibAvRecordingSink.Options.cs, Sussudio/Services/Recording/LibAvRecordingSink.OutputValidation.cs, Sussudio/Services/Recording/LibAvRecordingSink.PacketDrain.cs |
| FlashbackBufferManager | 13 | 1501 | Sussudio/Services/Flashback/FlashbackBufferManager.cs, Sussudio/Services/Flashback/FlashbackBufferManager.EvictionPause.cs, Sussudio/Services/Flashback/FlashbackBufferManager.Lifecycle.cs, Sussudio/Services/Flashback/FlashbackBufferManager.LiveAccounting.cs, Sussudio/Services/Flashback/FlashbackBufferManager.Math.cs, Sussudio/Services/Flashback/FlashbackBufferManager.Purge.cs, Sussudio/Services/Flashback/FlashbackBufferManager.RecoveryPreserve.cs, Sussudio/Services/Flashback/FlashbackBufferManager.Retention.cs |
| CaptureSessionCoordinator | 11 | 908 | Sussudio/Services/Capture/CaptureSessionCoordinator.Commands.cs, Sussudio/Services/Capture/CaptureSessionCoordinator.cs, Sussudio/Services/Capture/CaptureSessionCoordinator.Disposal.cs, Sussudio/Services/Capture/CaptureSessionCoordinator.Flashback.cs, Sussudio/Services/Capture/CaptureSessionCoordinator.Flashback.Export.cs, Sussudio/Services/Capture/CaptureSessionCoordinator.Flashback.Guards.cs, Sussudio/Services/Capture/CaptureSessionCoordinator.Flashback.Playback.cs, Sussudio/Services/Capture/CaptureSessionCoordinator.Flashback.Status.cs |
| PresentMonProbe | 11 | 1136 | tools/Common/PresentMon/PresentMonProbe.cs, tools/Common/PresentMon/PresentMonProbe.Csv.Correlation.cs, tools/Common/PresentMon/PresentMonProbe.Csv.cs, tools/Common/PresentMon/PresentMonProbe.Csv.Fields.cs, tools/Common/PresentMon/PresentMonProbe.Csv.Rows.cs, tools/Common/PresentMon/PresentMonProbe.Csv.Summary.cs, tools/Common/PresentMon/PresentMonProbe.Csv.SwapChains.cs, tools/Common/PresentMon/PresentMonProbe.Format.cs |
| DiagnosticSessionFlashbackStressScenario | 11 | 646 | tools/Common/DiagnosticSessionFlashbackStressScenario.AudioMaster.cs, tools/Common/DiagnosticSessionFlashbackStressScenario.CommandDrain.cs, tools/Common/DiagnosticSessionFlashbackStressScenario.CommandDrainWait.cs, tools/Common/DiagnosticSessionFlashbackStressScenario.cs, tools/Common/DiagnosticSessionFlashbackStressScenario.Scrub.cs, tools/Common/DiagnosticSessionFlashbackStressScenario.ScrubDrain.cs, tools/Common/DiagnosticSessionFlashbackStressScenario.ScrubUpdates.cs, tools/Common/DiagnosticSessionFlashbackStressScenario.Stress.cs |

## Largest production files

| Lines | Path |
| ---: | --- |
| 814 | Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.AutomationSnapshot.cs |
| 451 | Sussudio/Services/Capture/CaptureService.HealthSnapshotAssembler.cs |
| 303 | tools/KsAudioNodeProbe/Program.NativeInterop.cs |
| 249 | tools/NativeXuAudioProbe/RtkI2cProbe.cs |
| 247 | tools/EgavdsAudioProbe/Program.cs |
| 247 | Sussudio/Services/Flashback/FlashbackDecoder.cs |
| 245 | Sussudio/Services/Capture/MfSourceReaderVideoCapture.Initialization.cs |
| 244 | Sussudio/Services/Capture/MjpegPreviewJitterBuffer.Metrics.cs |
| 243 | Sussudio/Models/Telemetry/SourceSignalTelemetrySnapshot.cs |
| 238 | Sussudio/Services/Flashback/FlashbackDecoder.AudioOutput.cs |
| 237 | tools/NativeXuAudioProbe/Program.I2cCommands.SelectorProbe.cs |
| 236 | Sussudio/Services/Runtime/ProcessSupervisor.cs |
| 235 | Sussudio/Services/Capture/CaptureService.RuntimeSnapshotAssembler.cs |
| 235 | Sussudio/Services/Gpu/ParallelMjpegDecodePipeline.Reorder.cs |
| 234 | Sussudio/Controllers/Preview/Startup/PreviewStartupSessionController.cs |
| 234 | tools/Common/DiagnosticSessionResultBuilder.Flattening.cs |
| 232 | Sussudio/Services/Capture/MjpegPreviewJitterBuffer.cs |
| 232 | Sussudio/Services/Recording/LibAvEncoder.AudioQueue.cs |
| 229 | Sussudio/Services/Telemetry/NativeXuAtCommandProvider.PayloadDecoding.cs |
| 228 | Sussudio/Models/Capture/CaptureSettings.cs |
| 228 | Sussudio/Services/Audio/WasapiAudioPlayback.RenderThread.cs |
| 227 | Sussudio/Controllers/Window/WindowUiDispatchController.cs |
| 226 | Sussudio/Services/Capture/MfSourceReaderVideoCapture.Negotiation.cs |
| 207 | Sussudio/RuntimePaths.cs |
| 225 | Sussudio/Controllers/ViewModel/MainViewModelPreviewLifecycleController.cs |
| 223 | Sussudio/Services/Recording/LibAvEncoder.HardwareFrames.cs |
| 223 | Sussudio/Services/Audio/WasapiAudioCapture.Conversion.cs |
| 222 | Sussudio/Services/Recording/LibAvEncoder.OutputRotation.cs |
| 221 | tools/NativeXuAudioProbe/Program.DefaultExperiment.cs |
| 221 | Sussudio/Services/Recording/LibAvRecordingSink.Queues.cs |

## Sample production files under 60 lines

| Lines | Path |
| ---: | --- |
| 3 | Sussudio/AssemblyInfo.cs |
| 3 | tools/ssctl/AssemblyInfo.cs |
| 5 | tools/Common/DiagnosticSessionFlashbackRecordingSettingsScenarios.DeferredPresetState.cs |
| 6 | Sussudio/GlobalUsings.cs |
| 6 | Sussudio/MainWindow.PreviewStartup.Session.cs |
| 6 | tools/Common/DiagnosticSessionFlashbackMetrics.PlaybackSession.Stages.Model.cs |
| 7 | Sussudio.Automation.Contracts/AutomationUnknownCommandHandling.cs |
| 7 | Sussudio/MainWindow.CaptureSelectionBindings.CollectionSync.cs |
| 7 | Sussudio/MainWindow.CaptureSelectionBindings.PropertyRouter.cs |
| 7 | Sussudio/MainWindow.Flashback.Presentation.cs |
| 7 | Sussudio/MainWindow.PreviewStartup.Signals.cs |
| 7 | Sussudio/MainWindow.PropertyChangedPreview.cs |
| 8 | Sussudio/MainWindow.FullScreen.cs |
| 8 | Sussudio/MainWindow.PreviewRenderer.cs |
| 8 | Sussudio/MainWindow.ShutdownCleanup.cs |
| 8 | tools/Common/DiagnosticSessionFlashbackMetrics.PlaybackResult.Model.cs |
| 8 | tools/Common/DiagnosticSessionFlashbackMetrics.PlaybackSession.Commands.Model.cs |
| 8 | tools/Common/DiagnosticSessionResult.PreviewCadence.cs |
| 8 | tools/NativeXuAudioProbe/ToolLogger.cs |
| 9 | Sussudio/MainWindow.CaptureSelectionBindings.cs |
| 9 | Sussudio/MainWindow.FullScreen.Input.cs |
| 9 | Sussudio/MainWindow.PreviewStartup.cs |
| 9 | Sussudio/MainWindow.PreviewTransitions.cs |
| 9 | Sussudio/MainWindow.ShellChrome.cs |
| 9 | Sussudio/MainWindow.ShutdownCleanup.Event.cs |
| 9 | Sussudio/MainWindow.StatsOverlay.cs |
| 9 | Sussudio/Models/Automation/CaptureRuntimeSnapshot.AvSync.cs |
| 9 | Sussudio/Services/Automation/AutomationDiagnosticsHub.Hdr.cs |
| 9 | tools/Common/DiagnosticSessionOptionalTextFormatter.cs |
| 9 | tools/Common/ToolJsonOptions.cs |
| 10 | Sussudio/MainWindow.CaptureSelectionBindings.AudioSelection.cs |
| 10 | Sussudio/MainWindow.CaptureSelectionBindings.DeviceAudio.cs |
| 10 | Sussudio/MainWindow.Flashback.cs |
| 10 | Sussudio/Models/Automation/PreviewRuntimeSnapshot.GpuPlayback.cs |
| 10 | tools/Common/DiagnosticSessionFlashbackMetrics.PlaybackSession.Cadence.Model.cs |
| 10 | tools/Common/DiagnosticSessionSample.cs |
| 11 | tools/Common/DiagnosticSessionFlashbackMetrics.PlaybackSession.AudioMaster.Model.cs |
| 11 | tools/Common/DiagnosticSessionFlashbackMetrics.PlaybackSession.Model.cs |
| 12 | Sussudio/LoggingJsonContext.cs |
| 12 | Sussudio/MainWindow.StatsOverlay.Sections.cs |
| 12 | Sussudio/Services/Automation/AutomationDiagnosticsHub.FlashbackPlaybackAlerts.cs |
| 12 | Sussudio/Services/Preview/ILiveVideoSource.cs |
| 12 | Sussudio/ViewModels/MainViewModel.SettingsLoadApplication.cs |
| 12 | tools/Common/AutomationSnapshotFormatter.Flashback.Export.cs |
| 12 | tools/Common/DiagnosticSessionResult.Overview.cs |
| 12 | tools/Common/DiagnosticSessionScenarioCatalog.Entries.Combined.cs |
| 12 | tools/NativeXuAudioProbe/ToolCaptureDevice.cs |
| 13 | Sussudio/Controllers/ViewModel/MainViewModelUiDispatchController.Context.cs |
| 13 | Sussudio/Services/Automation/AutomationDiagnosticsHub.DiagnosticEvaluationLanes.Realtime.Mjpeg.cs |
| 13 | Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.cs |
| 13 | Sussudio/ViewModels/MainViewModel.RecordingCapability.cs |
| 13 | tools/Common/AutomationSnapshotFormatter.Flashback.Encoding.cs |
| 13 | tools/Common/AutomationSnapshotFormatter.Flashback.Playback.Commands.cs |
| 13 | tools/Common/AutomationSnapshotFormatter.Response.cs |
| 13 | tools/Common/DiagnosticSessionFlashbackMetrics.PlaybackResult.AudioMaster.Model.cs |
| 13 | tools/Common/DiagnosticSessionFlashbackMetrics.PlaybackResult.ObservedReads.cs |
| 13 | tools/Common/DiagnosticSessionResult.PreviewVisualCadence.cs |

## Notes

Use this as the before/after reference for the active defragmentation goal. A lower file count is not sufficient by itself; each slice should also improve behavioral locality, ownership, or deterministic testability.

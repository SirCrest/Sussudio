# Sussudio Defragmentation Baseline - Generated

Generated UTC: 2026-05-24T00:37:18Z
Root: C:\Users\crest\source\repos\Sussudio-cleanup-architecture

## Summary

| Metric | Value |
| --- | ---: |
| Production .cs files | 896 |
| Test .cs files | 552 |
| Production .cs files under 60 lines | 156 (17.4%) |
| Production .cs files under 80 lines | 251 (28.0%) |

## Largest partial-type clusters

| Type | Files | Total lines | Sample paths |
| --- | ---: | ---: | --- |
| CaptureService | 47 | 9789 | Sussudio/Services/Capture/CaptureService.AudioInputSwitching.cs, Sussudio/Services/Capture/CaptureService.AudioPreviewLifecycle.cs, Sussudio/Services/Capture/CaptureService.CaptureFormatTelemetry.cs, Sussudio/Services/Capture/CaptureService.Cleanup.cs, Sussudio/Services/Capture/CaptureService.cs, Sussudio/Services/Capture/CaptureService.Failures.cs, Sussudio/Services/Capture/CaptureService.FlashbackAudioInputs.cs, Sussudio/Services/Capture/CaptureService.FlashbackBackendFailureCleanup.cs |
| D3D11PreviewRenderer | 39 | 4794 | Sussudio/Services/Preview/D3D11PreviewRenderer.cs, Sussudio/Services/Preview/D3D11PreviewRenderer.DeviceInitialization.cs, Sussudio/Services/Preview/D3D11PreviewRenderer.DeviceLost.cs, Sussudio/Services/Preview/D3D11PreviewRenderer.Diagnostics.cs, Sussudio/Services/Preview/D3D11PreviewRenderer.DisplayClock.cs, Sussudio/Services/Preview/D3D11PreviewRenderer.DxgiFrameStatistics.cs, Sussudio/Services/Preview/D3D11PreviewRenderer.FrameLatency.cs, Sussudio/Services/Preview/D3D11PreviewRenderer.FrameOwnership.cs |
| MainViewModel | 36 | 4567 | Sussudio/Controllers/ViewModel/MainViewModelControllerGraph.cs, Sussudio/ViewModels/MainViewModel.AnalogAudioGain.cs, Sussudio/ViewModels/MainViewModel.AudioCapturePropertyChanges.cs, Sussudio/ViewModels/MainViewModel.AudioDeviceDiscovery.cs, Sussudio/ViewModels/MainViewModel.AudioInputSelection.cs, Sussudio/ViewModels/MainViewModel.AudioMeters.cs, Sussudio/ViewModels/MainViewModel.AudioState.cs, Sussudio/ViewModels/MainViewModel.AutomationAudio.cs |
| AutomationDiagnosticsHub | 33 | 10082 | Sussudio/Services/Automation/AutomationDiagnosticsHub.Alerts.cs, Sussudio/Services/Automation/AutomationDiagnosticsHub.Counters.RealtimePreview.cs, Sussudio/Services/Automation/AutomationDiagnosticsHub.cs, Sussudio/Services/Automation/AutomationDiagnosticsHub.DiagnosticEvaluation.cs, Sussudio/Services/Automation/AutomationDiagnosticsHub.DiagnosticEvaluationFlashback.cs, Sussudio/Services/Automation/AutomationDiagnosticsHub.DiagnosticEvaluationFlashback.Recording.cs, Sussudio/Services/Automation/AutomationDiagnosticsHub.DiagnosticEvaluationLanes.cs, Sussudio/Services/Automation/AutomationDiagnosticsHub.DiagnosticEvaluationRealtime.cs |
| MainWindow | 24 | 2555 | Sussudio/MainWindow.AudioBindings.cs, Sussudio/MainWindow.Bindings.cs, Sussudio/MainWindow.ButtonActions.cs, Sussudio/MainWindow.CaptureOptionBindings.cs, Sussudio/MainWindow.CaptureSelectionBindings.Composition.cs, Sussudio/MainWindow.ControllerInitialization.cs, Sussudio/MainWindow.Flashback.Interactions.cs, Sussudio/MainWindow.Flashback.Presentation.cs |
| Formatters | 21 | 1097 | tools/ssctl/Formatters.Common.cs, tools/ssctl/Formatters.Diagnostics.cs, tools/ssctl/Formatters.Memory.cs, tools/ssctl/Formatters.Options.cs, tools/ssctl/Formatters.Snapshot.Audio.cs, tools/ssctl/Formatters.Snapshot.AvSync.cs, tools/ssctl/Formatters.Snapshot.CaptureCadence.cs, tools/ssctl/Formatters.Snapshot.CaptureSettings.cs |
| PerformanceTimelineTools | 21 | 891 | tools/McpServer/Tools/PerformanceTimelineTools.cs, tools/McpServer/Tools/PerformanceTimelineTools.Formatting.cs, tools/McpServer/Tools/PerformanceTimelineTools.Formatting.Flashback.cs, tools/McpServer/Tools/PerformanceTimelineTools.Formatting.Preview.cs, tools/McpServer/Tools/PerformanceTimelineTools.Rendering.cs, tools/McpServer/Tools/PerformanceTimelineTools.Rendering.Trend.cs, tools/McpServer/Tools/PerformanceTimelineTools.Rendering.Trend.Flashback.cs, tools/McpServer/Tools/PerformanceTimelineTools.Rendering.Trend.Flashback.Export.cs |
| LibAvEncoder | 21 | 3059 | Sussudio/Services/Recording/LibAvEncoder.Audio.cs, Sussudio/Services/Recording/LibAvEncoder.AudioInitialization.cs, Sussudio/Services/Recording/LibAvEncoder.AudioQueue.cs, Sussudio/Services/Recording/LibAvEncoder.AudioSetup.cs, Sussudio/Services/Recording/LibAvEncoder.AudioSubmission.cs, Sussudio/Services/Recording/LibAvEncoder.AvSync.cs, Sussudio/Services/Recording/LibAvEncoder.CodecPolicy.cs, Sussudio/Services/Recording/LibAvEncoder.cs |
| FlashbackPlaybackController | 19 | 5007 | Sussudio/Services/Flashback/FlashbackPlaybackController.AudioMasterPacing.cs, Sussudio/Services/Flashback/FlashbackPlaybackController.AudioRouting.cs, Sussudio/Services/Flashback/FlashbackPlaybackController.CommandQueue.cs, Sussudio/Services/Flashback/FlashbackPlaybackController.CommandTelemetry.cs, Sussudio/Services/Flashback/FlashbackPlaybackController.cs, Sussudio/Services/Flashback/FlashbackPlaybackController.DecoderFiles.cs, Sussudio/Services/Flashback/FlashbackPlaybackController.DecoderReopen.cs, Sussudio/Services/Flashback/FlashbackPlaybackController.Lifecycle.cs |
| CommandHandlers | 19 | 1091 | tools/ssctl/CommandHandlers.Arguments.cs, tools/ssctl/CommandHandlers.AutomationFlow.cs, tools/ssctl/CommandHandlers.CaptureControls.cs, tools/ssctl/CommandHandlers.Context.cs, tools/ssctl/CommandHandlers.cs, tools/ssctl/CommandHandlers.Device.cs, tools/ssctl/CommandHandlers.DiagnosticSession.cs, tools/ssctl/CommandHandlers.Flags.cs |
| NativeXuAtCommandProvider | 18 | 2689 | Sussudio/Services/Telemetry/NativeXuAtCommandProvider.AnalogGain.cs, Sussudio/Services/Telemetry/NativeXuAtCommandProvider.AtProtocol.cs, Sussudio/Services/Telemetry/NativeXuAtCommandProvider.AudioCommands.cs, Sussudio/Services/Telemetry/NativeXuAtCommandProvider.AudioSwitch.cs, Sussudio/Services/Telemetry/NativeXuAtCommandProvider.cs, Sussudio/Services/Telemetry/NativeXuAtCommandProvider.DeviceCommandReads.cs, Sussudio/Services/Telemetry/NativeXuAtCommandProvider.DeviceCommands.cs, Sussudio/Services/Telemetry/NativeXuAtCommandProvider.DiagnosticSummary.cs |
| AutomationCommandDispatcher | 18 | 1675 | Sussudio/Services/Automation/AutomationCommandDispatcher.Assertions.cs, Sussudio/Services/Automation/AutomationCommandDispatcher.AudioControlCommands.cs, Sussudio/Services/Automation/AutomationCommandDispatcher.CaptureControlCommands.cs, Sussudio/Services/Automation/AutomationCommandDispatcher.CommandParsing.cs, Sussudio/Services/Automation/AutomationCommandDispatcher.cs, Sussudio/Services/Automation/AutomationCommandDispatcher.CustomCommands.cs, Sussudio/Services/Automation/AutomationCommandDispatcher.DeviceCommands.cs, Sussudio/Services/Automation/AutomationCommandDispatcher.FlashbackCommands.cs |
| MfSourceReaderVideoCapture | 15 | 2220 | Sussudio/Services/Capture/MfSourceReaderVideoCapture.Cadence.cs, Sussudio/Services/Capture/MfSourceReaderVideoCapture.ConvertedMediaType.cs, Sussudio/Services/Capture/MfSourceReaderVideoCapture.cs, Sussudio/Services/Capture/MfSourceReaderVideoCapture.DeviceEnumeration.cs, Sussudio/Services/Capture/MfSourceReaderVideoCapture.Diagnostics.cs, Sussudio/Services/Capture/MfSourceReaderVideoCapture.DxgiBuffers.cs, Sussudio/Services/Capture/MfSourceReaderVideoCapture.FrameDelivery.cs, Sussudio/Services/Capture/MfSourceReaderVideoCapture.FrameLayout.cs |
| FlashbackEncoderSink | 15 | 2737 | Sussudio/Services/Flashback/FlashbackEncoderSink.cs, Sussudio/Services/Flashback/FlashbackEncoderSink.DisposeLifecycle.cs, Sussudio/Services/Flashback/FlashbackEncoderSink.EncodingLoop.cs, Sussudio/Services/Flashback/FlashbackEncoderSink.EncodingProgress.cs, Sussudio/Services/Flashback/FlashbackEncoderSink.ForceRotateExecution.cs, Sussudio/Services/Flashback/FlashbackEncoderSink.ForceRotateRequests.cs, Sussudio/Services/Flashback/FlashbackEncoderSink.Inputs.Audio.cs, Sussudio/Services/Flashback/FlashbackEncoderSink.Inputs.Video.cs |
| FlashbackExporter | 14 | 3102 | Sussudio/Services/Flashback/FlashbackExporter.Execution.cs, Sussudio/Services/Flashback/FlashbackExporter.Lifecycle.cs, Sussudio/Services/Flashback/FlashbackExporter.OutputFiles.cs, Sussudio/Services/Flashback/FlashbackExporter.PacketTiming.cs, Sussudio/Services/Flashback/FlashbackExporter.RuntimePolicy.cs, Sussudio/Services/Flashback/FlashbackExporter.SegmentPacketReadLoop.cs, Sussudio/Services/Flashback/FlashbackExporter.SegmentPacketRebasing.cs, Sussudio/Services/Flashback/FlashbackExporter.SegmentPacketWriting.cs |
| FlashbackDecoder | 13 | 2004 | Sussudio/Services/Flashback/FlashbackDecoder.AudioOutput.cs, Sussudio/Services/Flashback/FlashbackDecoder.cs, Sussudio/Services/Flashback/FlashbackDecoder.D3D11.cs, Sussudio/Services/Flashback/FlashbackDecoder.D3D11Discovery.cs, Sussudio/Services/Flashback/FlashbackDecoder.DecodeLoop.cs, Sussudio/Services/Flashback/FlashbackDecoder.Diagnostics.cs, Sussudio/Services/Flashback/FlashbackDecoder.Lifetime.cs, Sussudio/Services/Flashback/FlashbackDecoder.Seeking.cs |
| LibAvRecordingSink | 11 | 1667 | Sussudio/Services/Recording/LibAvRecordingSink.AudioQueues.cs, Sussudio/Services/Recording/LibAvRecordingSink.cs, Sussudio/Services/Recording/LibAvRecordingSink.Diagnostics.cs, Sussudio/Services/Recording/LibAvRecordingSink.EncodingLoop.cs, Sussudio/Services/Recording/LibAvRecordingSink.Lifetime.cs, Sussudio/Services/Recording/LibAvRecordingSink.PacketDrain.cs, Sussudio/Services/Recording/LibAvRecordingSink.Queues.cs, Sussudio/Services/Recording/LibAvRecordingSink.Startup.cs |
| PresentMonProbe | 11 | 1136 | tools/Common/PresentMon/PresentMonProbe.cs, tools/Common/PresentMon/PresentMonProbe.Csv.Correlation.cs, tools/Common/PresentMon/PresentMonProbe.Csv.cs, tools/Common/PresentMon/PresentMonProbe.Csv.Fields.cs, tools/Common/PresentMon/PresentMonProbe.Csv.Rows.cs, tools/Common/PresentMon/PresentMonProbe.Csv.Summary.cs, tools/Common/PresentMon/PresentMonProbe.Csv.SwapChains.cs, tools/Common/PresentMon/PresentMonProbe.Format.cs |
| StatsPresentationBuilder | 10 | 940 | Sussudio/ViewModels/StatsPresentationBuilder.cs, Sussudio/ViewModels/StatsPresentationBuilder.DiagnosticRows.cs, Sussudio/ViewModels/StatsPresentationBuilder.DiagnosticSummary.cs, Sussudio/ViewModels/StatsPresentationBuilder.Dock.cs, Sussudio/ViewModels/StatsPresentationBuilder.Encoder.cs, Sussudio/ViewModels/StatsPresentationBuilder.FrameTime.cs, Sussudio/ViewModels/StatsPresentationBuilder.HardwareRows.cs, Sussudio/ViewModels/StatsPresentationBuilder.Status.cs |
| UnifiedVideoCapture | 9 | 1435 | Sussudio/Services/Capture/UnifiedVideoCapture.cs, Sussudio/Services/Capture/UnifiedVideoCapture.FrameIngress.cs, Sussudio/Services/Capture/UnifiedVideoCapture.Initialization.cs, Sussudio/Services/Capture/UnifiedVideoCapture.Lifecycle.cs, Sussudio/Services/Capture/UnifiedVideoCapture.Metrics.cs, Sussudio/Services/Capture/UnifiedVideoCapture.MjpegPipelineLifecycle.cs, Sussudio/Services/Capture/UnifiedVideoCapture.Preview.cs, Sussudio/Services/Capture/UnifiedVideoCapture.SinkFanout.cs |
| ParallelMjpegDecodePipeline | 8 | 1252 | Sussudio/Services/Gpu/ParallelMjpegDecodePipeline.CompressedQueue.cs, Sussudio/Services/Gpu/ParallelMjpegDecodePipeline.cs, Sussudio/Services/Gpu/ParallelMjpegDecodePipeline.Lifecycle.cs, Sussudio/Services/Gpu/ParallelMjpegDecodePipeline.Metrics.cs, Sussudio/Services/Gpu/ParallelMjpegDecodePipeline.Reorder.cs, Sussudio/Services/Gpu/ParallelMjpegDecodePipeline.ReorderEmission.cs, Sussudio/Services/Gpu/ParallelMjpegDecodePipeline.ResourceCleanup.cs, Sussudio/Services/Gpu/ParallelMjpegDecodePipeline.Workers.cs |
| RecordingVerifier | 8 | 1044 | Sussudio/Services/Recording/Verification/RecordingVerifier.Cadence.cs, Sussudio/Services/Recording/Verification/RecordingVerifier.cs, Sussudio/Services/Recording/Verification/RecordingVerifier.Ffprobe.cs, Sussudio/Services/Recording/Verification/RecordingVerifier.ProbeParsing.cs, Sussudio/Services/Recording/Verification/RecordingVerifier.Results.cs, Sussudio/Services/Recording/Verification/RecordingVerifier.Validation.cs, Sussudio/Services/Recording/Verification/RecordingVerifier.Validation.Format.cs, Sussudio/Services/Recording/Verification/RecordingVerifier.Validation.Hdr.cs |
| DiagnosticSessionResultBuilder | 8 | 1388 | tools/Common/DiagnosticSessionResultBuilder.Analysis.cs, tools/Common/DiagnosticSessionResultBuilder.AnalysisValidation.cs, tools/Common/DiagnosticSessionResultBuilder.cs, tools/Common/DiagnosticSessionResultBuilder.DiagnosticHealth.cs, tools/Common/DiagnosticSessionResultBuilder.FlashbackPlaybackResult.cs, tools/Common/DiagnosticSessionResultBuilder.Flattening.cs, tools/Common/DiagnosticSessionResultBuilder.PreviewScheduler.cs, tools/Common/DiagnosticSessionResultBuilder.Projections.cs |
| AutomationSnapshotFormatter | 8 | 806 | tools/Common/AutomationSnapshotFormatter.CaptureCadence.cs, tools/Common/AutomationSnapshotFormatter.cs, tools/Common/AutomationSnapshotFormatter.Flashback.cs, tools/Common/AutomationSnapshotFormatter.MjpegTiming.cs, tools/Common/AutomationSnapshotFormatter.PreviewD3D.cs, tools/Common/AutomationSnapshotFormatter.PreviewD3D.SlowFrames.cs, tools/Common/AutomationSnapshotFormatter.Values.cs, tools/Common/AutomationSnapshotFormatter.VideoPipeline.cs |
| AutomationSnapshot | 8 | 860 | Sussudio/Models/Automation/AutomationSnapshot.AudioIngest.cs, Sussudio/Models/Automation/AutomationSnapshot.CaptureSettings.cs, Sussudio/Models/Automation/AutomationSnapshot.cs, Sussudio/Models/Automation/AutomationSnapshot.Flashback.cs, Sussudio/Models/Automation/AutomationSnapshot.FrameDiagnostics.cs, Sussudio/Models/Automation/AutomationSnapshot.Preview.cs, Sussudio/Models/Automation/AutomationSnapshot.Recording.cs, Sussudio/Models/Automation/AutomationSnapshot.SourceTelemetry.cs |
| FlashbackBufferManager | 8 | 1462 | Sussudio/Services/Flashback/FlashbackBufferManager.cs, Sussudio/Services/Flashback/FlashbackBufferManager.Lifecycle.cs, Sussudio/Services/Flashback/FlashbackBufferManager.LiveAccounting.cs, Sussudio/Services/Flashback/FlashbackBufferManager.Purge.cs, Sussudio/Services/Flashback/FlashbackBufferManager.Retention.cs, Sussudio/Services/Flashback/FlashbackBufferManager.SegmentCompletion.cs, Sussudio/Services/Flashback/FlashbackBufferManager.SegmentMutation.cs, Sussudio/Services/Flashback/FlashbackBufferManager.SegmentQueries.cs |
| DiagnosticSessionFlashbackExportScenarios | 7 | 934 | tools/Common/DiagnosticSessionFlashbackExportScenarios.Concurrent.cs, tools/Common/DiagnosticSessionFlashbackExportScenarios.DisableDuringExport.cs, tools/Common/DiagnosticSessionFlashbackExportScenarios.DisableDuringExportValidation.cs, tools/Common/DiagnosticSessionFlashbackExportScenarios.Playback.cs, tools/Common/DiagnosticSessionFlashbackExportScenarios.Range.cs, tools/Common/DiagnosticSessionFlashbackExportScenarios.Registrations.cs, tools/Common/DiagnosticSessionFlashbackExportScenarios.Rotated.cs |
| CaptureSessionCoordinator | 7 | 875 | Sussudio/Services/Capture/CaptureSessionCoordinator.Commands.cs, Sussudio/Services/Capture/CaptureSessionCoordinator.cs, Sussudio/Services/Capture/CaptureSessionCoordinator.Disposal.cs, Sussudio/Services/Capture/CaptureSessionCoordinator.Flashback.cs, Sussudio/Services/Capture/CaptureSessionCoordinator.Flashback.Playback.cs, Sussudio/Services/Capture/CaptureSessionCoordinator.Queue.cs, Sussudio/Services/Capture/CaptureSessionCoordinator.Snapshot.cs |
| FlashbackBackendResources | 7 | 966 | Sussudio/Services/Flashback/FlashbackBackendResources.ArtifactCleanup.cs, Sussudio/Services/Flashback/FlashbackBackendResources.BufferCycle.cs, Sussudio/Services/Flashback/FlashbackBackendResources.BufferCycle.Lifecycle.cs, Sussudio/Services/Flashback/FlashbackBackendResources.cs, Sussudio/Services/Flashback/FlashbackBackendResources.PreviewDisposal.cs, Sussudio/Services/Flashback/FlashbackBackendResources.RecordingFinalize.cs, Sussudio/Services/Flashback/FlashbackBackendResources.Startup.cs |
| MjpegPreviewJitterBuffer | 7 | 1290 | Sussudio/Services/Capture/MjpegPreviewJitterBuffer.Adaptive.cs, Sussudio/Services/Capture/MjpegPreviewJitterBuffer.cs, Sussudio/Services/Capture/MjpegPreviewJitterBuffer.EmitLoop.cs, Sussudio/Services/Capture/MjpegPreviewJitterBuffer.FrameIngress.cs, Sussudio/Services/Capture/MjpegPreviewJitterBuffer.FramePacing.cs, Sussudio/Services/Capture/MjpegPreviewJitterBuffer.Metrics.cs, Sussudio/Services/Capture/MjpegPreviewJitterBuffer.Queue.cs |

## Largest production files

| Lines | Path |
| ---: | --- |
| 814 | Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.AutomationSnapshot.cs |
| 744 | Sussudio/Services/Automation/AutomationDiagnosticsHub.Timeline.cs |
| 647 | Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs |
| 639 | Sussudio/Controllers/ViewModel/MainViewModelControllerGraph.cs |
| 631 | Sussudio/Services/Audio/NativeXuAudioControlService.cs |
| 631 | Sussudio/Services/Capture/CaptureService.RecordingIntegrity.cs |
| 610 | Sussudio/Controllers/FullScreen/FullScreenController.cs |
| 583 | Sussudio/Controllers/Preview/Renderer/PreviewRuntimeD3DProjection.cs |
| 574 | tools/Common/DiagnosticSessionFlashbackStressScenario.cs |
| 547 | Sussudio/Controllers/Capture/CaptureSelectionBindingController.cs |
| 546 | Sussudio/Services/Flashback/FlashbackPlaybackController.CommandQueue.cs |
| 538 | Sussudio/Services/Capture/NativeXu/KsExtensionUnitNative.cs |
| 531 | Sussudio/Services/Capture/DeviceDiscovery/MfDeviceEnumerator.cs |
| 522 | Sussudio/Services/Capture/CaptureService.HealthSnapshotAssembler.cs |
| 488 | Sussudio/Services/Automation/PreviewPacingSlowStageClassifier.cs |
| 487 | Sussudio/Controllers/ViewModel/MainViewModelCaptureModeOptionRebuildController.cs |
| 471 | Sussudio/ViewModels/CaptureResolutionSelectionPolicy.cs |
| 470 | Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.PreviewD3D.cs |
| 448 | Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.FlashbackPlayback.cs |
| 434 | Sussudio/Services/Capture/CaptureService.RecordingFinalizeLibAvBackend.cs |
| 432 | Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.FlashbackRecording.cs |
| 416 | Sussudio/Services/Automation/AutomationDiagnosticsHub.Snapshots.cs |
| 414 | Sussudio/Services/Automation/AutomationDiagnosticsHub.Alerts.cs |
| 410 | Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.RecordingPipeline.cs |
| 398 | Sussudio/Services/Flashback/FlashbackPlaybackController.AudioRouting.cs |
| 394 | Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.CaptureFormat.cs |
| 392 | Sussudio/Controllers/Preview/Renderer/PreviewRendererHostController.cs |
| 385 | Sussudio/Services/Flashback/FlashbackPlaybackController.ThreadCommands.cs |
| 384 | Sussudio/Services/Capture/VisualCadenceTracker.cs |
| 380 | Sussudio/Services/Capture/CaptureService.HealthSnapshotFlashbackPlayback.cs |

## Sample production files under 60 lines

| Lines | Path |
| ---: | --- |
| 3 | Sussudio/AssemblyInfo.cs |
| 3 | tools/ssctl/AssemblyInfo.cs |
| 6 | Sussudio/GlobalUsings.cs |
| 8 | tools/NativeXuAudioProbe/ToolLogger.cs |
| 9 | tools/Common/DiagnosticSessionOptionalTextFormatter.cs |
| 9 | tools/Common/ToolJsonOptions.cs |
| 12 | Sussudio/LoggingJsonContext.cs |
| 12 | Sussudio/Services/Preview/ILiveVideoSource.cs |
| 12 | tools/NativeXuAudioProbe/ToolCaptureDevice.cs |
| 14 | Sussudio.Automation.Contracts/AutomationPipeSecurityPolicy.cs |
| 14 | Sussudio/Models/Automation/FlashbackSegmentInfo.cs |
| 14 | Sussudio/Services/Contracts/ISourceSignalTelemetryProvider.cs |
| 15 | tools/McpServer/Tools/PerformanceTimelineTools.Rendering.Trend.Flashback.Export.cs |
| 16 | tools/McpServer/Program.cs |
| 16 | tools/ssctl/CommandHandlers.Json.cs |
| 16 | tools/ssctl/Formatters.Snapshot.Audio.cs |
| 17 | Sussudio/Services/Preview/PreviewDisplayClock.cs |
| 17 | tools/KsAudioNodeProbe/Program.Constants.cs |
| 17 | tools/McpServer/Tools/PerformanceTimelineTools.Rows.Model.System.cs |
| 18 | Sussudio/Services/Automation/DiagnosticThresholds.cs |
| 18 | tools/Common/DiagnosticSessionJsonArtifacts.cs |
| 18 | tools/ssctl/CommandHandlers.Context.cs |
| 18 | tools/ssctl/Formatters.Snapshot.CoreSections.cs |
| 19 | Sussudio/Services/Telemetry/DisabledSourceSignalTelemetryProvider.cs |
| 19 | tools/ssctl/Formatters.Snapshot.Recording.cs |
| 20 | tools/McpServer/Tools/PerformanceTimelineTools.Rows.System.cs |
| 20 | tools/ssctl/Formatters.Snapshot.CaptureCadence.cs |
| 21 | tools/McpServer/Tools/PerformanceTimelineTools.Rows.Model.FlashbackExport.cs |
| 22 | Sussudio/ViewModels/DeviceAudioGainMapper.cs |
| 22 | tools/ssctl/SsctlHelpWriter.cs |
| 23 | Sussudio/Models/Automation/PreviewStartup.cs |
| 23 | Sussudio/ViewModels/OutputDriveSpacePresentationBuilder.cs |
| 23 | tools/ssctl/Formatters.Snapshot.Runtime.cs |
| 23 | tools/ssctl/Formatters.Snapshot.Source.cs |
| 24 | tools/McpServer/Tools/PerformanceTimelineTools.Rendering.Trend.Flashback.cs |
| 24 | tools/McpServer/Tools/PerformanceTimelineTools.Rows.FlashbackExport.cs |
| 24 | tools/ssctl/Formatters.Snapshot.DiagnosticLanes.cs |
| 25 | tools/Common/DiagnosticSessionRunner.cs |
| 25 | tools/McpServer/Tools/PerformanceTimelineTools.Formatting.cs |
| 25 | tools/ssctl/CommandHandlers.Flashback.Export.cs |
| 26 | tools/McpServer/Tools/PerformanceTimelineTools.Rows.Model.cs |
| 27 | Sussudio/Services/Contracts/PreviewFrameTracking.cs |
| 27 | tools/ssctl/Formatters.Snapshot.ProcessResources.cs |
| 28 | tools/Common/DiagnosticSessionModels.cs |
| 29 | Sussudio/Services/Capture/HdrOutputPolicy.cs |
| 30 | tools/McpServer/Tools/FlashbackTools.Segments.cs |
| 30 | tools/McpServer/Tools/PerformanceTimelineTools.Formatting.Flashback.cs |
| 30 | tools/ssctl/CommandHandlers.Transport.cs |
| 30 | tools/ssctl/Formatters.Snapshot.Preview.cs |
| 31 | Sussudio/Models/Automation/DiagnosticsEvents.cs |
| 31 | Sussudio/Services/Recording/LibAvEncoder.MuxerOptions.cs |
| 31 | Sussudio/Services/Runtime/TelemetryAgeHelper.cs |
| 31 | Sussudio/Services/Telemetry/NativeXuAtCommandProvider.Selector4.cs |
| 31 | tools/McpServer/Tools/VerificationTools.Parsing.cs |
| 32 | Sussudio/Controllers/Preview/PreviewShadowFadeAnimator.cs |
| 32 | Sussudio/MainWindow.PreviewRuntimeSnapshot.cs |
| 32 | tools/McpServer/Tools/FramePacingVerdictTools.Timeline.cs |
| 32 | tools/McpServer/Tools/PreviewTools.cs |
| 32 | tools/McpServer/Tools/RecordingTools.cs |
| 32 | tools/ssctl/Formatters.Memory.cs |

## Notes

Use this as the before/after reference for the active defragmentation goal. A lower file count is not sufficient by itself; each slice should also improve behavioral locality, ownership, or deterministic testability.

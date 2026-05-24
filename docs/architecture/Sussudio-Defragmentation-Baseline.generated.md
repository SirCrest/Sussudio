# Sussudio Defragmentation Baseline - Generated

Generated UTC: 2026-05-24T16:49:48Z
Root: C:\Users\crest\source\repos\Sussudio-cleanup-architecture

## Summary

| Metric | Value |
| --- | ---: |
| Production .cs files | 676 |
| Test .cs files | 552 |
| Production .cs files under 60 lines | 27 (4.0%) |
| Production .cs files under 80 lines | 48 (7.1%) |

## Largest partial-type clusters

| Type | Files | Total lines | Sample paths |
| --- | ---: | ---: | --- |
| CaptureService | 47 | 9789 | Sussudio/Services/Capture/CaptureService.AudioInputSwitching.cs, Sussudio/Services/Capture/CaptureService.AudioPreviewLifecycle.cs, Sussudio/Services/Capture/CaptureService.CaptureFormatTelemetry.cs, Sussudio/Services/Capture/CaptureService.Cleanup.cs, Sussudio/Services/Capture/CaptureService.cs, Sussudio/Services/Capture/CaptureService.Failures.cs, Sussudio/Services/Capture/CaptureService.FlashbackAudioInputs.cs, Sussudio/Services/Capture/CaptureService.FlashbackBackendFailureCleanup.cs |
| AutomationDiagnosticsHub | 31 | 10064 | Sussudio/Services/Automation/AutomationDiagnosticsHub.Alerts.cs, Sussudio/Services/Automation/AutomationDiagnosticsHub.Counters.RealtimePreview.cs, Sussudio/Services/Automation/AutomationDiagnosticsHub.cs, Sussudio/Services/Automation/AutomationDiagnosticsHub.DiagnosticEvaluationFlashback.cs, Sussudio/Services/Automation/AutomationDiagnosticsHub.DiagnosticEvaluationFlashback.Recording.cs, Sussudio/Services/Automation/AutomationDiagnosticsHub.DiagnosticEvaluationLanes.cs, Sussudio/Services/Automation/AutomationDiagnosticsHub.DiagnosticEvaluationRealtime.cs, Sussudio/Services/Automation/AutomationDiagnosticsHub.DiagnosticEvaluationRealtime.Preview.cs |
| D3D11PreviewRenderer | 25 | 4693 | Sussudio/Services/Preview/D3D11PreviewRenderer.cs, Sussudio/Services/Preview/D3D11PreviewRenderer.DeviceInitialization.cs, Sussudio/Services/Preview/D3D11PreviewRenderer.Diagnostics.cs, Sussudio/Services/Preview/D3D11PreviewRenderer.DxgiFrameStatistics.cs, Sussudio/Services/Preview/D3D11PreviewRenderer.FrameOwnership.cs, Sussudio/Services/Preview/D3D11PreviewRenderer.FrameUpload.cs, Sussudio/Services/Preview/D3D11PreviewRenderer.HdrShaderPass.cs, Sussudio/Services/Preview/D3D11PreviewRenderer.Metrics.cs |
| MainViewModel | 22 | 4501 | Sussudio/Controllers/ViewModel/MainViewModelControllerGraph.cs, Sussudio/ViewModels/MainViewModel.AudioDeviceDiscovery.cs, Sussudio/ViewModels/MainViewModel.AudioInputSelection.cs, Sussudio/ViewModels/MainViewModel.AudioMeters.cs, Sussudio/ViewModels/MainViewModel.AudioState.cs, Sussudio/ViewModels/MainViewModel.AutomationCommands.cs, Sussudio/ViewModels/MainViewModel.AutomationSnapshots.cs, Sussudio/ViewModels/MainViewModel.CaptureModeTransactions.cs |
| LibAvEncoder | 19 | 3086 | Sussudio/Services/Recording/LibAvEncoder.Audio.cs, Sussudio/Services/Recording/LibAvEncoder.AudioInitialization.cs, Sussudio/Services/Recording/LibAvEncoder.AudioQueue.cs, Sussudio/Services/Recording/LibAvEncoder.AudioSetup.cs, Sussudio/Services/Recording/LibAvEncoder.AudioSubmission.cs, Sussudio/Services/Recording/LibAvEncoder.AvSync.cs, Sussudio/Services/Recording/LibAvEncoder.CodecPolicy.cs, Sussudio/Services/Recording/LibAvEncoder.cs |
| NativeXuAtCommandProvider | 17 | 2680 | Sussudio/Services/Telemetry/NativeXuAtCommandProvider.AnalogGain.cs, Sussudio/Services/Telemetry/NativeXuAtCommandProvider.AtProtocol.cs, Sussudio/Services/Telemetry/NativeXuAtCommandProvider.AudioCommands.cs, Sussudio/Services/Telemetry/NativeXuAtCommandProvider.AudioSwitch.cs, Sussudio/Services/Telemetry/NativeXuAtCommandProvider.cs, Sussudio/Services/Telemetry/NativeXuAtCommandProvider.DeviceCommandReads.cs, Sussudio/Services/Telemetry/NativeXuAtCommandProvider.DeviceCommands.cs, Sussudio/Services/Telemetry/NativeXuAtCommandProvider.DiagnosticSummary.cs |
| FlashbackPlaybackController | 17 | 4986 | Sussudio/Services/Flashback/FlashbackPlaybackController.AudioMasterPacing.cs, Sussudio/Services/Flashback/FlashbackPlaybackController.AudioRouting.cs, Sussudio/Services/Flashback/FlashbackPlaybackController.CommandQueue.cs, Sussudio/Services/Flashback/FlashbackPlaybackController.CommandTelemetry.cs, Sussudio/Services/Flashback/FlashbackPlaybackController.cs, Sussudio/Services/Flashback/FlashbackPlaybackController.DecoderFiles.cs, Sussudio/Services/Flashback/FlashbackPlaybackController.DecoderReopen.cs, Sussudio/Services/Flashback/FlashbackPlaybackController.Markers.cs |
| FlashbackEncoderSink | 15 | 2737 | Sussudio/Services/Flashback/FlashbackEncoderSink.cs, Sussudio/Services/Flashback/FlashbackEncoderSink.DisposeLifecycle.cs, Sussudio/Services/Flashback/FlashbackEncoderSink.EncodingLoop.cs, Sussudio/Services/Flashback/FlashbackEncoderSink.EncodingProgress.cs, Sussudio/Services/Flashback/FlashbackEncoderSink.ForceRotateExecution.cs, Sussudio/Services/Flashback/FlashbackEncoderSink.ForceRotateRequests.cs, Sussudio/Services/Flashback/FlashbackEncoderSink.Inputs.Audio.cs, Sussudio/Services/Flashback/FlashbackEncoderSink.Inputs.Video.cs |
| MainWindow | 14 | 2472 | Sussudio/MainWindow.AudioBindings.cs, Sussudio/MainWindow.ButtonActions.cs, Sussudio/MainWindow.CaptureOptionBindings.cs, Sussudio/MainWindow.CaptureSelectionBindings.Composition.cs, Sussudio/MainWindow.Flashback.Interactions.cs, Sussudio/MainWindow.FullScreen.Composition.cs, Sussudio/MainWindow.PreviewRenderer.Composition.cs, Sussudio/MainWindow.PreviewStartup.Session.Composition.cs |
| MfSourceReaderVideoCapture | 14 | 2212 | Sussudio/Services/Capture/MfSourceReaderVideoCapture.Cadence.cs, Sussudio/Services/Capture/MfSourceReaderVideoCapture.ConvertedMediaType.cs, Sussudio/Services/Capture/MfSourceReaderVideoCapture.cs, Sussudio/Services/Capture/MfSourceReaderVideoCapture.DeviceEnumeration.cs, Sussudio/Services/Capture/MfSourceReaderVideoCapture.Diagnostics.cs, Sussudio/Services/Capture/MfSourceReaderVideoCapture.FrameDelivery.cs, Sussudio/Services/Capture/MfSourceReaderVideoCapture.FrameLayout.cs, Sussudio/Services/Capture/MfSourceReaderVideoCapture.Initialization.cs |
| FlashbackExporter | 14 | 3102 | Sussudio/Services/Flashback/FlashbackExporter.Execution.cs, Sussudio/Services/Flashback/FlashbackExporter.Lifecycle.cs, Sussudio/Services/Flashback/FlashbackExporter.OutputFiles.cs, Sussudio/Services/Flashback/FlashbackExporter.PacketTiming.cs, Sussudio/Services/Flashback/FlashbackExporter.RuntimePolicy.cs, Sussudio/Services/Flashback/FlashbackExporter.SegmentPacketReadLoop.cs, Sussudio/Services/Flashback/FlashbackExporter.SegmentPacketRebasing.cs, Sussudio/Services/Flashback/FlashbackExporter.SegmentPacketWriting.cs |
| FlashbackDecoder | 12 | 1995 | Sussudio/Services/Flashback/FlashbackDecoder.AudioOutput.cs, Sussudio/Services/Flashback/FlashbackDecoder.cs, Sussudio/Services/Flashback/FlashbackDecoder.D3D11.cs, Sussudio/Services/Flashback/FlashbackDecoder.D3D11Discovery.cs, Sussudio/Services/Flashback/FlashbackDecoder.DecodeLoop.cs, Sussudio/Services/Flashback/FlashbackDecoder.Lifetime.cs, Sussudio/Services/Flashback/FlashbackDecoder.Seeking.cs, Sussudio/Services/Flashback/FlashbackDecoder.Timestamps.cs |
| CommandHandlers | 10 | 1042 | tools/ssctl/CommandHandlers.Arguments.cs, tools/ssctl/CommandHandlers.AutomationFlow.cs, tools/ssctl/CommandHandlers.CaptureControls.cs, tools/ssctl/CommandHandlers.cs, tools/ssctl/CommandHandlers.Device.cs, tools/ssctl/CommandHandlers.Flashback.Actions.cs, tools/ssctl/CommandHandlers.Flashback.cs, tools/ssctl/CommandHandlers.Observability.cs |
| UnifiedVideoCapture | 9 | 1435 | Sussudio/Services/Capture/UnifiedVideoCapture.cs, Sussudio/Services/Capture/UnifiedVideoCapture.FrameIngress.cs, Sussudio/Services/Capture/UnifiedVideoCapture.Initialization.cs, Sussudio/Services/Capture/UnifiedVideoCapture.Lifecycle.cs, Sussudio/Services/Capture/UnifiedVideoCapture.Metrics.cs, Sussudio/Services/Capture/UnifiedVideoCapture.MjpegPipelineLifecycle.cs, Sussudio/Services/Capture/UnifiedVideoCapture.Preview.cs, Sussudio/Services/Capture/UnifiedVideoCapture.SinkFanout.cs |
| AutomationCommandDispatcher | 9 | 1596 | Sussudio/Services/Automation/AutomationCommandDispatcher.Assertions.cs, Sussudio/Services/Automation/AutomationCommandDispatcher.cs, Sussudio/Services/Automation/AutomationCommandDispatcher.CustomCommands.cs, Sussudio/Services/Automation/AutomationCommandDispatcher.FlashbackCommands.cs, Sussudio/Services/Automation/AutomationCommandDispatcher.Payload.cs, Sussudio/Services/Automation/AutomationCommandDispatcher.PortMappedDispatch.cs, Sussudio/Services/Automation/AutomationCommandDispatcher.Preflight.cs, Sussudio/Services/Automation/AutomationCommandDispatcher.WaitConditions.cs |
| PresentMonProbe | 9 | 1126 | tools/Common/PresentMon/PresentMonProbe.cs, tools/Common/PresentMon/PresentMonProbe.Csv.Correlation.cs, tools/Common/PresentMon/PresentMonProbe.Csv.cs, tools/Common/PresentMon/PresentMonProbe.Csv.Rows.cs, tools/Common/PresentMon/PresentMonProbe.Csv.Summary.cs, tools/Common/PresentMon/PresentMonProbe.Format.cs, tools/Common/PresentMon/PresentMonProbe.Options.cs, tools/Common/PresentMon/PresentMonProbe.Paths.cs |
| LibAvRecordingSink | 9 | 1652 | Sussudio/Services/Recording/LibAvRecordingSink.AudioQueues.cs, Sussudio/Services/Recording/LibAvRecordingSink.cs, Sussudio/Services/Recording/LibAvRecordingSink.EncodingLoop.cs, Sussudio/Services/Recording/LibAvRecordingSink.Lifetime.cs, Sussudio/Services/Recording/LibAvRecordingSink.PacketDrain.cs, Sussudio/Services/Recording/LibAvRecordingSink.Queues.cs, Sussudio/Services/Recording/LibAvRecordingSink.Startup.cs, Sussudio/Services/Recording/LibAvRecordingSink.StopLifecycle.cs |
| DiagnosticSessionResultBuilder | 8 | 1425 | tools/Common/DiagnosticSessionResultBuilder.Analysis.cs, tools/Common/DiagnosticSessionResultBuilder.AnalysisValidation.cs, tools/Common/DiagnosticSessionResultBuilder.cs, tools/Common/DiagnosticSessionResultBuilder.DiagnosticHealth.cs, tools/Common/DiagnosticSessionResultBuilder.FlashbackPlaybackResult.cs, tools/Common/DiagnosticSessionResultBuilder.Flattening.cs, tools/Common/DiagnosticSessionResultBuilder.PreviewScheduler.cs, tools/Common/DiagnosticSessionResultBuilder.Projections.cs |
| ParallelMjpegDecodePipeline | 8 | 1252 | Sussudio/Services/Gpu/ParallelMjpegDecodePipeline.CompressedQueue.cs, Sussudio/Services/Gpu/ParallelMjpegDecodePipeline.cs, Sussudio/Services/Gpu/ParallelMjpegDecodePipeline.Lifecycle.cs, Sussudio/Services/Gpu/ParallelMjpegDecodePipeline.Metrics.cs, Sussudio/Services/Gpu/ParallelMjpegDecodePipeline.Reorder.cs, Sussudio/Services/Gpu/ParallelMjpegDecodePipeline.ReorderEmission.cs, Sussudio/Services/Gpu/ParallelMjpegDecodePipeline.ResourceCleanup.cs, Sussudio/Services/Gpu/ParallelMjpegDecodePipeline.Workers.cs |
| MjpegPreviewJitterBuffer | 7 | 1290 | Sussudio/Services/Capture/MjpegPreviewJitterBuffer.Adaptive.cs, Sussudio/Services/Capture/MjpegPreviewJitterBuffer.cs, Sussudio/Services/Capture/MjpegPreviewJitterBuffer.EmitLoop.cs, Sussudio/Services/Capture/MjpegPreviewJitterBuffer.FrameIngress.cs, Sussudio/Services/Capture/MjpegPreviewJitterBuffer.FramePacing.cs, Sussudio/Services/Capture/MjpegPreviewJitterBuffer.Metrics.cs, Sussudio/Services/Capture/MjpegPreviewJitterBuffer.Queue.cs |
| AutomationSnapshotFormatter | 6 | 792 | tools/Common/AutomationSnapshotFormatter.CaptureCadence.cs, tools/Common/AutomationSnapshotFormatter.cs, tools/Common/AutomationSnapshotFormatter.Flashback.cs, tools/Common/AutomationSnapshotFormatter.MjpegTiming.cs, tools/Common/AutomationSnapshotFormatter.PreviewD3D.cs, tools/Common/AutomationSnapshotFormatter.Values.cs |
| PerformanceTimelineTools | 6 | 793 | tools/McpServer/Tools/PerformanceTimelineTools.cs, tools/McpServer/Tools/PerformanceTimelineTools.Formatting.cs, tools/McpServer/Tools/PerformanceTimelineTools.Rendering.cs, tools/McpServer/Tools/PerformanceTimelineTools.Rows.cs, tools/McpServer/Tools/PerformanceTimelineTools.Rows.Model.cs, tools/McpServer/Tools/PerformanceTimelineTools.Summaries.cs |
| Formatters | 6 | 973 | tools/ssctl/Formatters.Common.cs, tools/ssctl/Formatters.Options.cs, tools/ssctl/Formatters.Snapshot.cs, tools/ssctl/Formatters.Snapshot.Flashback.cs, tools/ssctl/Formatters.Snapshot.Mjpeg.cs, tools/ssctl/Formatters.Timeline.cs |
| WasapiAudioCapture | 6 | 976 | Sussudio/Services/Audio/WasapiAudioCapture.CaptureLoop.cs, Sussudio/Services/Audio/WasapiAudioCapture.Conversion.cs, Sussudio/Services/Audio/WasapiAudioCapture.cs, Sussudio/Services/Audio/WasapiAudioCapture.Diagnostics.cs, Sussudio/Services/Audio/WasapiAudioCapture.Fanout.cs, Sussudio/Services/Audio/WasapiAudioCapture.Initialization.cs |
| FlashbackBufferManager | 6 | 1445 | Sussudio/Services/Flashback/FlashbackBufferManager.cs, Sussudio/Services/Flashback/FlashbackBufferManager.Lifecycle.cs, Sussudio/Services/Flashback/FlashbackBufferManager.LiveAccounting.cs, Sussudio/Services/Flashback/FlashbackBufferManager.Purge.cs, Sussudio/Services/Flashback/FlashbackBufferManager.Retention.cs, Sussudio/Services/Flashback/FlashbackBufferManager.Segments.cs |
| WasapiAudioPlayback | 5 | 854 | Sussudio/Services/Audio/WasapiAudioPlayback.cs, Sussudio/Services/Audio/WasapiAudioPlayback.Initialization.cs, Sussudio/Services/Audio/WasapiAudioPlayback.Queue.cs, Sussudio/Services/Audio/WasapiAudioPlayback.RenderThread.cs, Sussudio/Services/Audio/WasapiAudioPlayback.Volume.cs |
| RecordingVerifier | 5 | 1021 | Sussudio/Services/Recording/Verification/RecordingVerifier.Cadence.cs, Sussudio/Services/Recording/Verification/RecordingVerifier.cs, Sussudio/Services/Recording/Verification/RecordingVerifier.Ffprobe.cs, Sussudio/Services/Recording/Verification/RecordingVerifier.Results.cs, Sussudio/Services/Recording/Verification/RecordingVerifier.Validation.cs |
| FlashbackBackendResources | 5 | 951 | Sussudio/Services/Flashback/FlashbackBackendResources.BufferCycle.cs, Sussudio/Services/Flashback/FlashbackBackendResources.cs, Sussudio/Services/Flashback/FlashbackBackendResources.RecordingFinalize.cs, Sussudio/Services/Flashback/FlashbackBackendResources.Startup.cs, Sussudio/Services/Flashback/FlashbackBackendResources.Teardown.cs |
| DiagnosticSessionFlashbackMetrics | 5 | 762 | tools/Common/DiagnosticSessionFlashbackMetrics.Export.cs, tools/Common/DiagnosticSessionFlashbackMetrics.PlaybackObservation.cs, tools/Common/DiagnosticSessionFlashbackMetrics.PlaybackResult.cs, tools/Common/DiagnosticSessionFlashbackMetrics.PlaybackSession.cs, tools/Common/DiagnosticSessionFlashbackMetrics.Recording.cs |
| DiagnosticSessionFlashbackExportScenarios | 4 | 906 | tools/Common/DiagnosticSessionFlashbackExportScenarios.cs, tools/Common/DiagnosticSessionFlashbackExportScenarios.DisableDuringExport.cs, tools/Common/DiagnosticSessionFlashbackExportScenarios.Playback.cs, tools/Common/DiagnosticSessionFlashbackExportScenarios.Range.cs |

## Largest production files

| Lines | Path |
| ---: | --- |
| 885 | Sussudio/ViewModels/StatsPresentationBuilder.cs |
| 833 | Sussudio/Models/Automation/AutomationSnapshot.cs |
| 814 | Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.AutomationSnapshot.cs |
| 744 | Sussudio/Services/Automation/AutomationDiagnosticsHub.Timeline.cs |
| 647 | Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs |
| 639 | Sussudio/Controllers/ViewModel/MainViewModelControllerGraph.cs |
| 638 | Sussudio/Controllers/Capture/CaptureSelectionBindingController.cs |
| 631 | Sussudio/Services/Audio/NativeXuAudioControlService.cs |
| 631 | Sussudio/Services/Capture/CaptureService.RecordingIntegrity.cs |
| 610 | Sussudio/Controllers/FullScreen/FullScreenController.cs |
| 583 | Sussudio/Controllers/Preview/Renderer/PreviewRuntimeD3DProjection.cs |
| 575 | tools/NativeXuAudioProbe/Program.I2cCommands.cs |
| 574 | tools/Common/DiagnosticSessionFlashbackStressScenario.cs |
| 552 | Sussudio/Services/Automation/PreviewPacingSlowStageClassifier.cs |
| 546 | Sussudio/Services/Flashback/FlashbackPlaybackController.CommandQueue.cs |
| 538 | Sussudio/Services/Capture/NativeXu/KsExtensionUnitNative.cs |
| 531 | Sussudio/Services/Capture/DeviceDiscovery/MfDeviceEnumerator.cs |
| 522 | Sussudio/Services/Capture/CaptureService.HealthSnapshotAssembler.cs |
| 506 | Sussudio/Services/Flashback/FlashbackBufferManager.Segments.cs |
| 493 | Sussudio/Controllers/Preview/Renderer/PreviewRendererHostController.cs |
| 487 | Sussudio/Controllers/ViewModel/MainViewModelCaptureModeOptionRebuildController.cs |
| 471 | Sussudio/ViewModels/CaptureResolutionSelectionPolicy.cs |
| 470 | Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.PreviewD3D.cs |
| 458 | Sussudio/Services/Preview/D3D11PreviewRenderer.Metrics.cs |
| 451 | Sussudio/ViewModels/MainViewModel.AudioState.cs |
| 448 | Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.FlashbackPlayback.cs |
| 434 | Sussudio/Services/Capture/CaptureService.RecordingFinalizeLibAvBackend.cs |
| 432 | Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.FlashbackRecording.cs |
| 416 | Sussudio/Services/Automation/AutomationDiagnosticsHub.Snapshots.cs |
| 414 | Sussudio/Services/Automation/AutomationDiagnosticsHub.Alerts.cs |

## Sample production files under 60 lines

| Lines | Path |
| ---: | --- |
| 3 | Sussudio/AssemblyInfo.cs |
| 3 | tools/ssctl/AssemblyInfo.cs |
| 6 | Sussudio/GlobalUsings.cs |
| 9 | tools/Common/DiagnosticSessionOptionalTextFormatter.cs |
| 9 | tools/Common/ToolJsonOptions.cs |
| 12 | Sussudio/LoggingJsonContext.cs |
| 12 | Sussudio/Services/Preview/ILiveVideoSource.cs |
| 14 | Sussudio.Automation.Contracts/AutomationPipeSecurityPolicy.cs |
| 14 | Sussudio/Services/Contracts/ISourceSignalTelemetryProvider.cs |
| 16 | tools/McpServer/Program.cs |
| 18 | Sussudio/Services/Automation/DiagnosticThresholds.cs |
| 19 | Sussudio/Services/Telemetry/DisabledSourceSignalTelemetryProvider.cs |
| 28 | tools/Common/DiagnosticSessionModels.cs |
| 29 | Sussudio/Services/Capture/HdrOutputPolicy.cs |
| 34 | Sussudio/Services/Flashback/FlashbackDecoder.OutputTypes.cs |
| 37 | tools/KsAudioNodeProbe/Program.cs |
| 39 | Sussudio/Services/Recording/RecordingContracts.cs |
| 40 | tools/NativeXuAudioProbe/Program.Commands.cs |
| 41 | tools/Common/DiagnosticSessionAutomationResponseJson.cs |
| 45 | tools/McpServer/Tools/DiagnosticSessionTools.cs |
| 48 | Sussudio/Services/Capture/CaptureSessionStateMachine.cs |
| 48 | Sussudio/Services/Contracts/AutomationInterfaces.cs |
| 48 | tools/McpServer/Tools/PerformanceTimelineTools.cs |
| 50 | tools/McpServer/PipeClient.cs |
| 54 | Sussudio.Automation.Contracts/AutomationCommandCatalog.Manifest.cs |
| 56 | Sussudio/DisplayFormatters.cs |
| 56 | tools/McpServer/Tools/PreviewTools.cs |

## Notes

Use this as the before/after reference for the active defragmentation goal. A lower file count is not sufficient by itself; each slice should also improve behavioral locality, ownership, or deterministic testability.

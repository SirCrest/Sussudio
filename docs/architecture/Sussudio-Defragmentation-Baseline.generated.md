# Sussudio Defragmentation Baseline - Generated

Generated UTC: 2026-05-25T01:06:51Z
Root: C:\Users\crest\source\repos\Sussudio-cleanup-architecture

## Summary

| Metric | Value |
| --- | ---: |
| Production .cs files | 571 |
| Test .cs files | 552 |
| Production .cs files under 60 lines | 16 (2.8%) |
| Production .cs files under 80 lines | 30 (5.3%) |

## Largest partial-type clusters

| Type | Files | Total lines | Sample paths |
| --- | ---: | ---: | --- |
| CaptureService | 38 | 9729 | Sussudio/Services/Capture/CaptureService.AudioInputSwitching.cs, Sussudio/Services/Capture/CaptureService.AudioPreviewLifecycle.cs, Sussudio/Services/Capture/CaptureService.CaptureFormatTelemetry.cs, Sussudio/Services/Capture/CaptureService.Cleanup.cs, Sussudio/Services/Capture/CaptureService.cs, Sussudio/Services/Capture/CaptureService.Failures.cs, Sussudio/Services/Capture/CaptureService.FlashbackBufferCycle.cs, Sussudio/Services/Capture/CaptureService.FlashbackExportCore.cs |
| AutomationDiagnosticsHub | 29 | 10065 | Sussudio/Services/Automation/AutomationDiagnosticsHub.Alerts.cs, Sussudio/Services/Automation/AutomationDiagnosticsHub.Counters.RealtimePreview.cs, Sussudio/Services/Automation/AutomationDiagnosticsHub.cs, Sussudio/Services/Automation/AutomationDiagnosticsHub.DiagnosticEvaluationFlashback.cs, Sussudio/Services/Automation/AutomationDiagnosticsHub.DiagnosticEvaluationFlashback.Recording.cs, Sussudio/Services/Automation/AutomationDiagnosticsHub.DiagnosticEvaluationLanes.cs, Sussudio/Services/Automation/AutomationDiagnosticsHub.DiagnosticEvaluationRealtime.cs, Sussudio/Services/Automation/AutomationDiagnosticsHub.DiagnosticEvents.cs |
| D3D11PreviewRenderer | 19 | 4647 | Sussudio/Services/Preview/D3D11PreviewRenderer.cs, Sussudio/Services/Preview/D3D11PreviewRenderer.DeviceInitialization.cs, Sussudio/Services/Preview/D3D11PreviewRenderer.Diagnostics.cs, Sussudio/Services/Preview/D3D11PreviewRenderer.DxgiFrameStatistics.cs, Sussudio/Services/Preview/D3D11PreviewRenderer.FrameUpload.cs, Sussudio/Services/Preview/D3D11PreviewRenderer.Metrics.cs, Sussudio/Services/Preview/D3D11PreviewRenderer.NativeInterop.cs, Sussudio/Services/Preview/D3D11PreviewRenderer.Nv12Submission.cs |
| MainViewModel | 17 | 4447 | Sussudio/Controllers/ViewModel/MainViewModelControllerGraph.cs, Sussudio/ViewModels/MainViewModel.AudioInputSelection.cs, Sussudio/ViewModels/MainViewModel.AudioMeters.cs, Sussudio/ViewModels/MainViewModel.AudioState.cs, Sussudio/ViewModels/MainViewModel.AutomationCommands.cs, Sussudio/ViewModels/MainViewModel.AutomationSnapshots.cs, Sussudio/ViewModels/MainViewModel.CaptureModeTransactions.cs, Sussudio/ViewModels/MainViewModel.CaptureSelection.cs |
| FlashbackPlaybackController | 14 | 4954 | Sussudio/Services/Flashback/FlashbackPlaybackController.AudioMasterPacing.cs, Sussudio/Services/Flashback/FlashbackPlaybackController.AudioRouting.cs, Sussudio/Services/Flashback/FlashbackPlaybackController.CommandQueue.cs, Sussudio/Services/Flashback/FlashbackPlaybackController.cs, Sussudio/Services/Flashback/FlashbackPlaybackController.DecoderFiles.cs, Sussudio/Services/Flashback/FlashbackPlaybackController.DecoderReopen.cs, Sussudio/Services/Flashback/FlashbackPlaybackController.Markers.cs, Sussudio/Services/Flashback/FlashbackPlaybackController.Metrics.cs |
| FlashbackEncoderSink | 13 | 2723 | Sussudio/Services/Flashback/FlashbackEncoderSink.cs, Sussudio/Services/Flashback/FlashbackEncoderSink.DisposeLifecycle.cs, Sussudio/Services/Flashback/FlashbackEncoderSink.EncodingLoop.cs, Sussudio/Services/Flashback/FlashbackEncoderSink.EncodingProgress.cs, Sussudio/Services/Flashback/FlashbackEncoderSink.ForceRotate.cs, Sussudio/Services/Flashback/FlashbackEncoderSink.Inputs.cs, Sussudio/Services/Flashback/FlashbackEncoderSink.Options.cs, Sussudio/Services/Flashback/FlashbackEncoderSink.PacketDrain.cs |
| MfSourceReaderVideoCapture | 13 | 2205 | Sussudio/Services/Capture/MfSourceReaderVideoCapture.Cadence.cs, Sussudio/Services/Capture/MfSourceReaderVideoCapture.cs, Sussudio/Services/Capture/MfSourceReaderVideoCapture.DeviceEnumeration.cs, Sussudio/Services/Capture/MfSourceReaderVideoCapture.Diagnostics.cs, Sussudio/Services/Capture/MfSourceReaderVideoCapture.FrameDelivery.cs, Sussudio/Services/Capture/MfSourceReaderVideoCapture.FrameLayout.cs, Sussudio/Services/Capture/MfSourceReaderVideoCapture.Initialization.cs, Sussudio/Services/Capture/MfSourceReaderVideoCapture.InitializedSession.cs |
| LibAvEncoder | 13 | 3047 | Sussudio/Services/Recording/LibAvEncoder.Audio.cs, Sussudio/Services/Recording/LibAvEncoder.AudioInitialization.cs, Sussudio/Services/Recording/LibAvEncoder.AudioQueue.cs, Sussudio/Services/Recording/LibAvEncoder.AvSync.cs, Sussudio/Services/Recording/LibAvEncoder.CodecPolicy.cs, Sussudio/Services/Recording/LibAvEncoder.cs, Sussudio/Services/Recording/LibAvEncoder.HardwareFrames.cs, Sussudio/Services/Recording/LibAvEncoder.HardwareSubmission.cs |
| FlashbackExporter | 13 | 3096 | Sussudio/Services/Flashback/FlashbackExporter.Execution.cs, Sussudio/Services/Flashback/FlashbackExporter.Lifecycle.cs, Sussudio/Services/Flashback/FlashbackExporter.OutputFiles.cs, Sussudio/Services/Flashback/FlashbackExporter.PacketTiming.cs, Sussudio/Services/Flashback/FlashbackExporter.RuntimePolicy.cs, Sussudio/Services/Flashback/FlashbackExporter.SegmentPacketReadLoop.cs, Sussudio/Services/Flashback/FlashbackExporter.SegmentPacketWriting.cs, Sussudio/Services/Flashback/FlashbackExporter.Segments.cs |
| MainWindow | 12 | 2459 | Sussudio/MainWindow.AudioBindings.cs, Sussudio/MainWindow.ButtonActions.cs, Sussudio/MainWindow.CaptureBindings.cs, Sussudio/MainWindow.Flashback.Interactions.cs, Sussudio/MainWindow.PreviewRenderer.Composition.cs, Sussudio/MainWindow.PreviewStartup.Session.Composition.cs, Sussudio/MainWindow.PreviewTransitions.Composition.cs, Sussudio/MainWindow.ShellChrome.Composition.cs |
| NativeXuAtCommandProvider | 12 | 2639 | Sussudio/Services/Telemetry/NativeXuAtCommandProvider.AtProtocol.cs, Sussudio/Services/Telemetry/NativeXuAtCommandProvider.AudioCommands.cs, Sussudio/Services/Telemetry/NativeXuAtCommandProvider.cs, Sussudio/Services/Telemetry/NativeXuAtCommandProvider.DeviceCommands.cs, Sussudio/Services/Telemetry/NativeXuAtCommandProvider.DiagnosticSummary.cs, Sussudio/Services/Telemetry/NativeXuAtCommandProvider.FullSnapshot.cs, Sussudio/Services/Telemetry/NativeXuAtCommandProvider.InterfaceRead.cs, Sussudio/Services/Telemetry/NativeXuAtCommandProvider.PayloadDecoding.cs |
| FlashbackDecoder | 11 | 2019 | Sussudio/Services/Flashback/FlashbackDecoder.AudioOutput.cs, Sussudio/Services/Flashback/FlashbackDecoder.cs, Sussudio/Services/Flashback/FlashbackDecoder.D3D11.cs, Sussudio/Services/Flashback/FlashbackDecoder.D3D11Discovery.cs, Sussudio/Services/Flashback/FlashbackDecoder.DecodeLoop.cs, Sussudio/Services/Flashback/FlashbackDecoder.Lifetime.cs, Sussudio/Services/Flashback/FlashbackDecoder.Seeking.cs, Sussudio/Services/Flashback/FlashbackDecoder.Validation.cs |
| UnifiedVideoCapture | 8 | 1426 | Sussudio/Services/Capture/UnifiedVideoCapture.cs, Sussudio/Services/Capture/UnifiedVideoCapture.FrameIngress.cs, Sussudio/Services/Capture/UnifiedVideoCapture.Initialization.cs, Sussudio/Services/Capture/UnifiedVideoCapture.Lifecycle.cs, Sussudio/Services/Capture/UnifiedVideoCapture.MjpegPipelineLifecycle.cs, Sussudio/Services/Capture/UnifiedVideoCapture.Preview.cs, Sussudio/Services/Capture/UnifiedVideoCapture.SinkFanout.cs, Sussudio/Services/Capture/UnifiedVideoCapture.SinkFanout.Flashback.cs |
| CommandHandlers | 8 | 1032 | tools/ssctl/CommandHandlers.Arguments.cs, tools/ssctl/CommandHandlers.AutomationFlow.cs, tools/ssctl/CommandHandlers.CaptureControls.cs, tools/ssctl/CommandHandlers.cs, tools/ssctl/CommandHandlers.Flashback.cs, tools/ssctl/CommandHandlers.Observability.cs, tools/ssctl/CommandHandlers.Values.cs, tools/ssctl/CommandHandlers.Window.cs |
| LibAvRecordingSink | 8 | 1644 | Sussudio/Services/Recording/LibAvRecordingSink.AudioQueues.cs, Sussudio/Services/Recording/LibAvRecordingSink.cs, Sussudio/Services/Recording/LibAvRecordingSink.Lifetime.cs, Sussudio/Services/Recording/LibAvRecordingSink.PacketDrain.cs, Sussudio/Services/Recording/LibAvRecordingSink.Queues.cs, Sussudio/Services/Recording/LibAvRecordingSink.Startup.cs, Sussudio/Services/Recording/LibAvRecordingSink.StopLifecycle.cs, Sussudio/Services/Recording/LibAvRecordingSink.VideoQueueSubmission.cs |
| AutomationCommandDispatcher | 8 | 1585 | Sussudio/Services/Automation/AutomationCommandDispatcher.Assertions.cs, Sussudio/Services/Automation/AutomationCommandDispatcher.cs, Sussudio/Services/Automation/AutomationCommandDispatcher.CustomCommands.cs, Sussudio/Services/Automation/AutomationCommandDispatcher.FlashbackCommands.cs, Sussudio/Services/Automation/AutomationCommandDispatcher.Payload.cs, Sussudio/Services/Automation/AutomationCommandDispatcher.PortMappedDispatch.cs, Sussudio/Services/Automation/AutomationCommandDispatcher.Preflight.cs, Sussudio/Services/Automation/AutomationCommandDispatcher.WindowCommands.cs |
| DiagnosticSessionResultBuilder | 7 | 1418 | tools/Common/DiagnosticSessionResultBuilder.Analysis.cs, tools/Common/DiagnosticSessionResultBuilder.cs, tools/Common/DiagnosticSessionResultBuilder.DiagnosticHealth.cs, tools/Common/DiagnosticSessionResultBuilder.FlashbackPlaybackResult.cs, tools/Common/DiagnosticSessionResultBuilder.Flattening.cs, tools/Common/DiagnosticSessionResultBuilder.PreviewScheduler.cs, tools/Common/DiagnosticSessionResultBuilder.Projections.cs |
| ParallelMjpegDecodePipeline | 7 | 1245 | Sussudio/Services/Gpu/ParallelMjpegDecodePipeline.CompressedQueue.cs, Sussudio/Services/Gpu/ParallelMjpegDecodePipeline.cs, Sussudio/Services/Gpu/ParallelMjpegDecodePipeline.Lifecycle.cs, Sussudio/Services/Gpu/ParallelMjpegDecodePipeline.Metrics.cs, Sussudio/Services/Gpu/ParallelMjpegDecodePipeline.Reorder.cs, Sussudio/Services/Gpu/ParallelMjpegDecodePipeline.ReorderEmission.cs, Sussudio/Services/Gpu/ParallelMjpegDecodePipeline.Workers.cs |
| FlashbackBufferManager | 6 | 1445 | Sussudio/Services/Flashback/FlashbackBufferManager.cs, Sussudio/Services/Flashback/FlashbackBufferManager.Lifecycle.cs, Sussudio/Services/Flashback/FlashbackBufferManager.LiveAccounting.cs, Sussudio/Services/Flashback/FlashbackBufferManager.Purge.cs, Sussudio/Services/Flashback/FlashbackBufferManager.Retention.cs, Sussudio/Services/Flashback/FlashbackBufferManager.Segments.cs |
| WasapiAudioCapture | 6 | 976 | Sussudio/Services/Audio/WasapiAudioCapture.CaptureLoop.cs, Sussudio/Services/Audio/WasapiAudioCapture.Conversion.cs, Sussudio/Services/Audio/WasapiAudioCapture.cs, Sussudio/Services/Audio/WasapiAudioCapture.Diagnostics.cs, Sussudio/Services/Audio/WasapiAudioCapture.Fanout.cs, Sussudio/Services/Audio/WasapiAudioCapture.Initialization.cs |
| MjpegPreviewJitterBuffer | 6 | 1280 | Sussudio/Services/Capture/MjpegPreviewJitterBuffer.Adaptive.cs, Sussudio/Services/Capture/MjpegPreviewJitterBuffer.cs, Sussudio/Services/Capture/MjpegPreviewJitterBuffer.FrameIngress.cs, Sussudio/Services/Capture/MjpegPreviewJitterBuffer.FramePacing.cs, Sussudio/Services/Capture/MjpegPreviewJitterBuffer.Metrics.cs, Sussudio/Services/Capture/MjpegPreviewJitterBuffer.Queue.cs |
| RecordingVerifier | 5 | 1021 | Sussudio/Services/Recording/Verification/RecordingVerifier.Cadence.cs, Sussudio/Services/Recording/Verification/RecordingVerifier.cs, Sussudio/Services/Recording/Verification/RecordingVerifier.Ffprobe.cs, Sussudio/Services/Recording/Verification/RecordingVerifier.Results.cs, Sussudio/Services/Recording/Verification/RecordingVerifier.Validation.cs |
| Formatters | 4 | 959 | tools/ssctl/Formatters.Common.cs, tools/ssctl/Formatters.Options.cs, tools/ssctl/Formatters.Snapshot.cs, tools/ssctl/Formatters.Timeline.cs |
| NvdecMjpegDecoder | 4 | 610 | Sussudio/Services/Gpu/NvdecMjpegDecoder.Decode.cs, Sussudio/Services/Gpu/NvdecMjpegDecoder.Download.cs, Sussudio/Services/Gpu/NvdecMjpegDecoder.Initialization.cs, Sussudio/Services/Gpu/NvdecMjpegDecoder.SharedInitialization.cs |
| AutomationSnapshotFormatter | 4 | 779 | tools/Common/AutomationSnapshotFormatter.cs, tools/Common/AutomationSnapshotFormatter.Flashback.cs, tools/Common/AutomationSnapshotFormatter.MjpegTiming.cs, tools/Common/AutomationSnapshotFormatter.PreviewD3D.cs |
| WasapiAudioPlayback | 4 | 847 | Sussudio/Services/Audio/WasapiAudioPlayback.cs, Sussudio/Services/Audio/WasapiAudioPlayback.Initialization.cs, Sussudio/Services/Audio/WasapiAudioPlayback.Queue.cs, Sussudio/Services/Audio/WasapiAudioPlayback.RenderThread.cs |
| DeviceService | 3 | 716 | Sussudio/Services/Capture/DeviceService.cs, Sussudio/Services/Capture/DeviceService.FormatCache.cs, Sussudio/Services/Capture/DeviceService.FormatProbe.cs |
| DiagnosticSessionFlashbackMetrics | 3 | 746 | tools/Common/DiagnosticSessionFlashbackMetrics.PlaybackResult.cs, tools/Common/DiagnosticSessionFlashbackMetrics.PlaybackSession.cs, tools/Common/DiagnosticSessionFlashbackMetrics.RecordingExport.cs |
| PresentMonProbe | 3 | 1099 | tools/Common/PresentMon/PresentMonProbe.cs, tools/Common/PresentMon/PresentMonProbe.Csv.cs, tools/Common/PresentMon/PresentMonProbe.Format.cs |
| CudaD3D11InteropBridge | 3 | 587 | Sussudio/Services/Gpu/CudaD3D11Interop.Copy.cs, Sussudio/Services/Gpu/CudaD3D11Interop.Initialization.cs, Sussudio/Services/Gpu/CudaD3D11Interop.Native.cs |

## Largest production files

| Lines | Path |
| ---: | --- |
| 912 | Sussudio/Services/Flashback/FlashbackBackendResources.cs |
| 885 | Sussudio/ViewModels/StatsPresentationBuilder.cs |
| 876 | tools/Common/DiagnosticSessionFlashbackExportScenarios.cs |
| 833 | Sussudio/Models/Automation/AutomationSnapshot.cs |
| 814 | Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.AutomationSnapshot.cs |
| 744 | Sussudio/Services/Automation/AutomationDiagnosticsHub.Timeline.cs |
| 684 | Sussudio/Services/Flashback/FlashbackPlaybackController.CommandQueue.cs |
| 677 | Sussudio/Services/Preview/D3D11PreviewRenderer.Metrics.cs |
| 649 | tools/Common/DiagnosticSessionFlashbackPreviewCycleScenarios.cs |
| 647 | Sussudio/Services/Automation/NamedPipeAutomationServer.cs |
| 647 | Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs |
| 639 | Sussudio/Controllers/ViewModel/MainViewModelControllerGraph.cs |
| 638 | Sussudio/Controllers/Capture/CaptureSelectionBindingController.cs |
| 631 | Sussudio/Services/Audio/NativeXuAudioControlService.cs |
| 631 | Sussudio/Services/Capture/CaptureService.RecordingIntegrity.cs |
| 615 | Sussudio/Services/Flashback/FlashbackPlaybackController.ThreadCommands.cs |
| 610 | Sussudio/Controllers/FullScreen/FullScreenController.cs |
| 583 | Sussudio/Controllers/Preview/Renderer/PreviewRuntimeD3DProjection.cs |
| 580 | Sussudio/Controllers/Window/WindowCloseLifecycleController.cs |
| 578 | Sussudio/Services/Capture/CaptureSessionCoordinator.cs |
| 575 | tools/NativeXuAudioProbe/Program.I2cCommands.cs |
| 574 | tools/Common/DiagnosticSessionFlashbackStressScenario.cs |
| 552 | Sussudio/Services/Automation/PreviewPacingSlowStageClassifier.cs |
| 544 | Sussudio/Services/Automation/AutomationDiagnosticsHub.Alerts.cs |
| 542 | tools/Common/PresentMon/PresentMonProbe.Csv.cs |
| 538 | Sussudio/Services/Capture/NativeXu/KsExtensionUnitNative.cs |
| 531 | Sussudio/Services/Capture/DeviceDiscovery/MfDeviceEnumerator.cs |
| 529 | Sussudio/ViewModels/MainViewModel.AudioState.cs |
| 522 | Sussudio/Services/Capture/CaptureService.HealthSnapshotAssembler.cs |
| 516 | tools/ssctl/Formatters.Snapshot.cs |

## Sample production files under 60 lines

| Lines | Path |
| ---: | --- |
| 3 | Sussudio/AssemblyInfo.cs |
| 3 | tools/ssctl/AssemblyInfo.cs |
| 6 | Sussudio/GlobalUsings.cs |
| 9 | tools/Common/ToolJsonOptions.cs |
| 12 | Sussudio/Services/Preview/ILiveVideoSource.cs |
| 14 | Sussudio.Automation.Contracts/AutomationPipeSecurityPolicy.cs |
| 14 | Sussudio/Services/Contracts/ISourceSignalTelemetryProvider.cs |
| 19 | Sussudio/Services/Telemetry/DisabledSourceSignalTelemetryProvider.cs |
| 28 | tools/Common/DiagnosticSessionModels.cs |
| 37 | tools/KsAudioNodeProbe/Program.cs |
| 39 | Sussudio/Services/Recording/RecordingContracts.cs |
| 40 | tools/NativeXuAudioProbe/Program.Commands.cs |
| 45 | tools/McpServer/Tools/DiagnosticSessionTools.cs |
| 48 | Sussudio/Services/Capture/CaptureSessionStateMachine.cs |
| 48 | Sussudio/Services/Contracts/AutomationInterfaces.cs |
| 56 | Sussudio/DisplayFormatters.cs |

## Notes

Use this as the before/after reference for the active defragmentation goal. A lower file count is not sufficient by itself; each slice should also improve behavioral locality, ownership, or deterministic testability.

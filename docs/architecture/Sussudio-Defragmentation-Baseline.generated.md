# Sussudio Defragmentation Baseline - Generated

Generated UTC: 2026-05-26T17:36:12Z
Root: C:\Users\crest\source\repos\Sussudio-cleanup-architecture

## Summary

| Metric | Value |
| --- | ---: |
| Production .cs files | 342 |
| Test .cs files | 192 |
| Production .cs files under 60 lines | 2 (0.6%) |
| Production .cs files under 80 lines | 6 (1.8%) |

## Largest partial-type clusters

| Type | Files | Total lines | Sample paths |
| --- | ---: | ---: | --- |
| AutomationDiagnosticsHub | 25 | 10039 | Sussudio/Services/Automation/AutomationDiagnosticsHub.Alerts.cs, Sussudio/Services/Automation/AutomationDiagnosticsHub.Counters.RealtimePreview.cs, Sussudio/Services/Automation/AutomationDiagnosticsHub.cs, Sussudio/Services/Automation/AutomationDiagnosticsHub.DiagnosticEvaluationFlashback.cs, Sussudio/Services/Automation/AutomationDiagnosticsHub.DiagnosticEvaluationLanes.cs, Sussudio/Services/Automation/AutomationDiagnosticsHub.DiagnosticEvaluationRealtime.cs, Sussudio/Services/Automation/AutomationDiagnosticsHub.Evaluation.cs, Sussudio/Services/Automation/AutomationDiagnosticsHub.Hdr.cs |
| CaptureService | 21 | 9576 | Sussudio/Services/Capture/CaptureService.AudioPreviewLifecycle.cs, Sussudio/Services/Capture/CaptureService.Cleanup.cs, Sussudio/Services/Capture/CaptureService.cs, Sussudio/Services/Capture/CaptureService.Failures.cs, Sussudio/Services/Capture/CaptureService.FlashbackControls.cs, Sussudio/Services/Capture/CaptureService.FlashbackExportCore.cs, Sussudio/Services/Capture/CaptureService.FlashbackExportDiagnostics.cs, Sussudio/Services/Capture/CaptureService.FlashbackPreviewBackend.cs |
| D3D11PreviewRenderer | 13 | 4748 | Sussudio/Services/Preview/D3D11PreviewRenderer.cs, Sussudio/Services/Preview/D3D11PreviewRenderer.DeviceInitialization.cs, Sussudio/Services/Preview/D3D11PreviewRenderer.DxgiFrameStatistics.cs, Sussudio/Services/Preview/D3D11PreviewRenderer.FrameUpload.cs, Sussudio/Services/Preview/D3D11PreviewRenderer.Metrics.cs, Sussudio/Services/Preview/D3D11PreviewRenderer.PanelBinding.cs, Sussudio/Services/Preview/D3D11PreviewRenderer.PendingFrames.cs, Sussudio/Services/Preview/D3D11PreviewRenderer.RenderPasses.cs |
| MainViewModel | 11 | 4545 | Sussudio/Controllers/ViewModel/MainViewModelControllerGraph.cs, Sussudio/ViewModels/MainViewModel.AudioState.cs, Sussudio/ViewModels/MainViewModel.AutomationCommands.cs, Sussudio/ViewModels/MainViewModel.CaptureSelection.cs, Sussudio/ViewModels/MainViewModel.CaptureState.cs, Sussudio/ViewModels/MainViewModel.cs, Sussudio/ViewModels/MainViewModel.DeviceAudioState.cs, Sussudio/ViewModels/MainViewModel.FlashbackExport.cs |
| LibAvEncoder | 10 | 3025 | Sussudio/Services/Recording/LibAvEncoder.Audio.cs, Sussudio/Services/Recording/LibAvEncoder.AudioInitialization.cs, Sussudio/Services/Recording/LibAvEncoder.AudioQueue.cs, Sussudio/Services/Recording/LibAvEncoder.CodecPolicy.cs, Sussudio/Services/Recording/LibAvEncoder.cs, Sussudio/Services/Recording/LibAvEncoder.HardwareFrames.cs, Sussudio/Services/Recording/LibAvEncoder.Initialization.cs, Sussudio/Services/Recording/LibAvEncoder.OutputRotation.cs |
| FlashbackPlaybackController | 9 | 4909 | Sussudio/Services/Flashback/FlashbackPlaybackController.AudioMasterPacing.cs, Sussudio/Services/Flashback/FlashbackPlaybackController.AudioRouting.cs, Sussudio/Services/Flashback/FlashbackPlaybackController.CommandQueue.cs, Sussudio/Services/Flashback/FlashbackPlaybackController.cs, Sussudio/Services/Flashback/FlashbackPlaybackController.DecoderFiles.cs, Sussudio/Services/Flashback/FlashbackPlaybackController.Markers.cs, Sussudio/Services/Flashback/FlashbackPlaybackController.Metrics.cs, Sussudio/Services/Flashback/FlashbackPlaybackController.PlaybackFrames.cs |
| FlashbackExporter | 8 | 3044 | Sussudio/Services/Flashback/FlashbackExporter.Execution.cs, Sussudio/Services/Flashback/FlashbackExporter.Lifecycle.cs, Sussudio/Services/Flashback/FlashbackExporter.PacketTiming.cs, Sussudio/Services/Flashback/FlashbackExporter.SegmentPacketWriting.cs, Sussudio/Services/Flashback/FlashbackExporter.Segments.cs, Sussudio/Services/Flashback/FlashbackExporter.SingleFilePacketReadLoop.cs, Sussudio/Services/Flashback/FlashbackExporter.Streams.cs, Sussudio/Services/Flashback/FlashbackExporter.Validation.cs |
| NativeXuAtCommandProvider | 8 | 2583 | Sussudio/Services/Telemetry/NativeXuAtCommandProvider.AtProtocol.cs, Sussudio/Services/Telemetry/NativeXuAtCommandProvider.AudioCommands.cs, Sussudio/Services/Telemetry/NativeXuAtCommandProvider.cs, Sussudio/Services/Telemetry/NativeXuAtCommandProvider.DeviceCommands.cs, Sussudio/Services/Telemetry/NativeXuAtCommandProvider.PayloadDecoding.cs, Sussudio/Services/Telemetry/NativeXuAtCommandProvider.RollingPoll.cs, Sussudio/Services/Telemetry/NativeXuAtCommandProvider.SnapshotAssembly.cs, Sussudio/Services/Telemetry/NativeXuAtCommandProvider.TelemetryDetails.cs |
| MfSourceReaderVideoCapture | 7 | 2494 | Sussudio/Services/Capture/MfSourceReaderVideoCapture.ComContracts.cs, Sussudio/Services/Capture/MfSourceReaderVideoCapture.cs, Sussudio/Services/Capture/MfSourceReaderVideoCapture.FrameDelivery.cs, Sussudio/Services/Capture/MfSourceReaderVideoCapture.Initialization.cs, Sussudio/Services/Capture/MfSourceReaderVideoCapture.Lifecycle.cs, Sussudio/Services/Capture/MfSourceReaderVideoCapture.Negotiation.cs, Sussudio/Services/Capture/MfSourceReaderVideoCapture.RawFrameDelivery.cs |
| FlashbackEncoderSink | 7 | 2676 | Sussudio/Services/Flashback/FlashbackEncoderSink.cs, Sussudio/Services/Flashback/FlashbackEncoderSink.DisposeLifecycle.cs, Sussudio/Services/Flashback/FlashbackEncoderSink.EncodingLoop.cs, Sussudio/Services/Flashback/FlashbackEncoderSink.ForceRotate.cs, Sussudio/Services/Flashback/FlashbackEncoderSink.Inputs.cs, Sussudio/Services/Flashback/FlashbackEncoderSink.Queues.cs, Sussudio/Services/Flashback/FlashbackEncoderSink.Startup.cs |
| FlashbackDecoder | 6 | 1982 | Sussudio/Services/Flashback/FlashbackDecoder.AudioOutput.cs, Sussudio/Services/Flashback/FlashbackDecoder.cs, Sussudio/Services/Flashback/FlashbackDecoder.DecodeLoop.cs, Sussudio/Services/Flashback/FlashbackDecoder.Seeking.cs, Sussudio/Services/Flashback/FlashbackDecoder.VideoOutput.cs, Sussudio/Services/Flashback/FlashbackDecoder.VideoSetup.cs |
| UnifiedVideoCapture | 5 | 1395 | Sussudio/Services/Capture/UnifiedVideoCapture.cs, Sussudio/Services/Capture/UnifiedVideoCapture.FrameIngress.cs, Sussudio/Services/Capture/UnifiedVideoCapture.Lifecycle.cs, Sussudio/Services/Capture/UnifiedVideoCapture.Preview.cs, Sussudio/Services/Capture/UnifiedVideoCapture.SinkFanout.cs |
| MainWindow | 5 | 2466 | Sussudio/MainWindow.ControlBindings.cs, Sussudio/MainWindow.Flashback.Interactions.cs, Sussudio/MainWindow.PreviewLifecycle.Composition.cs, Sussudio/MainWindow.ShellChrome.Composition.cs, Sussudio/MainWindow.xaml.cs |
| LibAvRecordingSink | 5 | 1734 | Sussudio/Services/Recording/LibAvRecordingSink.cs, Sussudio/Services/Recording/LibAvRecordingSink.Queues.cs, Sussudio/Services/Recording/LibAvRecordingSink.Startup.cs, Sussudio/Services/Recording/LibAvRecordingSink.StopLifecycle.cs, Sussudio/Services/Recording/LibAvRecordingSink.VideoQueueSubmission.cs |
| FlashbackBufferManager | 4 | 1430 | Sussudio/Services/Flashback/FlashbackBufferManager.cs, Sussudio/Services/Flashback/FlashbackBufferManager.Lifecycle.cs, Sussudio/Services/Flashback/FlashbackBufferManager.Retention.cs, Sussudio/Services/Flashback/FlashbackBufferManager.Segments.cs |
| DiagnosticSessionResultBuilder | 4 | 1473 | tools/Common/DiagnosticSessionResultBuilder.Analysis.cs, tools/Common/DiagnosticSessionResultBuilder.cs, tools/Common/DiagnosticSessionResultBuilder.Flattening.cs, tools/Common/DiagnosticSessionResultBuilder.Projections.cs |
| ParallelMjpegDecodePipeline | 3 | 1207 | Sussudio/Services/Gpu/ParallelMjpegDecodePipeline.cs, Sussudio/Services/Gpu/ParallelMjpegDecodePipeline.Lifecycle.cs, Sussudio/Services/Gpu/ParallelMjpegDecodePipeline.Reorder.cs |
| WasapiAudioCapture | 3 | 953 | Sussudio/Services/Audio/WasapiAudioCapture.CaptureLoop.cs, Sussudio/Services/Audio/WasapiAudioCapture.Conversion.cs, Sussudio/Services/Audio/WasapiAudioCapture.cs |
| MjpegPreviewJitterBuffer | 3 | 1255 | Sussudio/Services/Capture/MjpegPreviewJitterBuffer.cs, Sussudio/Services/Capture/MjpegPreviewJitterBuffer.FrameIngress.cs, Sussudio/Services/Capture/MjpegPreviewJitterBuffer.FramePacing.cs |
| Formatters | 3 | 949 | tools/ssctl/Formatters.Common.cs, tools/ssctl/Formatters.Snapshot.cs, tools/ssctl/Formatters.Timeline.cs |
| PresentMonProbe | 2 | 1204 | tools/Common/PresentMon/PresentMonProbe.cs, tools/Common/PresentMon/PresentMonProbe.Csv.cs |
| RecordingVerifier | 2 | 993 | Sussudio/Services/Recording/Verification/RecordingVerifier.cs, Sussudio/Services/Recording/Verification/RecordingVerifier.Ffprobe.cs |
| AutomationCommandDispatcher | 2 | 1638 | Sussudio/Services/Automation/AutomationCommandDispatcher.cs, Sussudio/Services/Automation/AutomationCommandDispatcher.CustomCommands.cs |
| CudaD3D11InteropBridge | 2 | 581 | Sussudio/Services/Gpu/CudaD3D11Interop.Copy.cs, Sussudio/Services/Gpu/CudaD3D11Interop.Initialization.cs |
| CaptureSessionCoordinator | 2 | 961 | Sussudio/Services/Capture/CaptureSessionCoordinator.cs, Sussudio/Services/Capture/CaptureSessionCoordinator.Flashback.cs |
| LoggingJsonContext | 1 | 306 | Sussudio/Logger.cs |
| App | 1 | 260 | Sussudio/App.xaml.cs |
| DeviceFormatCacheJsonContext | 1 | 697 | Sussudio/Services/Capture/DeviceService.cs |
| SettingsJsonContext | 1 | 104 | Sussudio/Services/Runtime/SettingsService.cs |
| StatsWindow | 1 | 308 | Sussudio/StatsWindow.xaml.cs |

## Largest production files

| Lines | Path |
| ---: | --- |
| 1174 | Sussudio/Services/Flashback/FlashbackPlaybackController.PlaybackFrames.cs |
| 1054 | tools/ssctl/CommandHandlers.cs |
| 1033 | Sussudio/Services/Capture/CaptureService.HealthSnapshots.cs |
| 965 | Sussudio/Services/Flashback/FlashbackPlaybackController.ThreadCommands.cs |
| 912 | Sussudio/Services/Flashback/FlashbackBackendResources.cs |
| 904 | Sussudio/Services/Automation/AutomationCommandDispatcher.CustomCommands.cs |
| 885 | Sussudio/ViewModels/StatsPresentationBuilder.cs |
| 885 | Sussudio/Services/Preview/D3D11PreviewRenderer.Metrics.cs |
| 884 | tools/NativeXuAudioProbe/Program.I2cCommands.cs |
| 876 | tools/Common/DiagnosticSessionFlashbackExportScenarios.cs |
| 868 | Sussudio/Services/Audio/WasapiComInterop.cs |
| 859 | tools/McpServer/Tools/PerformanceTools.cs |
| 840 | Sussudio/Services/Audio/WasapiAudioPlayback.cs |
| 833 | Sussudio/Models/Automation/AutomationSnapshot.cs |
| 814 | Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.AutomationSnapshot.cs |
| 771 | Sussudio/Services/Capture/CaptureService.FlashbackExportCore.cs |
| 764 | Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs |
| 757 | tools/Common/AutomationSnapshotFormatter.cs |
| 744 | Sussudio/Services/Automation/AutomationDiagnosticsHub.Timeline.cs |
| 738 | Sussudio/ViewModels/MainViewModel.AudioState.cs |
| 734 | Sussudio/Services/Automation/AutomationCommandDispatcher.cs |
| 730 | tools/Common/DiagnosticSessionFlashbackMetrics.cs |
| 712 | Sussudio/Services/Capture/CaptureService.RuntimeSnapshots.cs |
| 705 | Sussudio/Services/Capture/CaptureSessionCoordinator.cs |
| 697 | Sussudio/Services/Capture/DeviceService.cs |
| 691 | Sussudio/Services/Flashback/FlashbackExporter.Execution.cs |
| 689 | Sussudio/Services/Flashback/FlashbackExporter.SegmentPacketWriting.cs |
| 684 | Sussudio/Services/Flashback/FlashbackPlaybackController.CommandQueue.cs |
| 683 | Sussudio/Services/Automation/AutomationDiagnosticsHub.Alerts.cs |
| 675 | Sussudio/Controllers/Window/WindowCloseLifecycleController.cs |

## Sample production files under 60 lines

| Lines | Path |
| ---: | --- |
| 14 | Sussudio/Services/Contracts/ISourceSignalTelemetryProvider.cs |
| 56 | Sussudio/DisplayFormatters.cs |

## Notes

Use this as the before/after reference for the active defragmentation goal. A lower file count is not sufficient by itself; each slice should also improve behavioral locality, ownership, or deterministic testability.

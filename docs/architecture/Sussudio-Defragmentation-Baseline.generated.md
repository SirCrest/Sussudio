# Sussudio Defragmentation Baseline - Generated

Generated UTC: 2026-05-27T04:50:17Z
Root: C:\Users\crest\source\repos\Sussudio-cleanup-architecture

## Summary

| Metric | Value |
| --- | ---: |
| Production .cs files | 304 |
| Test .cs files | 148 |
| Production .cs files under 60 lines | 1 (0.3%) |
| Production .cs files under 80 lines | 4 (1.3%) |

## Largest partial-type clusters

| Type | Files | Total lines | Sample paths |
| --- | ---: | ---: | --- |
| CaptureService | 20 | 9564 | Sussudio/Services/Capture/CaptureService.AudioPreviewLifecycle.cs, Sussudio/Services/Capture/CaptureService.Cleanup.cs, Sussudio/Services/Capture/CaptureService.cs, Sussudio/Services/Capture/CaptureService.Failures.cs, Sussudio/Services/Capture/CaptureService.FlashbackControls.cs, Sussudio/Services/Capture/CaptureService.FlashbackExportCore.cs, Sussudio/Services/Capture/CaptureService.FlashbackExportDiagnostics.cs, Sussudio/Services/Capture/CaptureService.FlashbackRecording.cs |
| AutomationDiagnosticsHub | 18 | 9986 | Sussudio/Services/Automation/AutomationDiagnosticsHub.Alerts.cs, Sussudio/Services/Automation/AutomationDiagnosticsHub.cs, Sussudio/Services/Automation/AutomationDiagnosticsHub.DiagnosticEvaluationFlashback.cs, Sussudio/Services/Automation/AutomationDiagnosticsHub.DiagnosticEvaluationRealtime.cs, Sussudio/Services/Automation/AutomationDiagnosticsHub.Evaluation.cs, Sussudio/Services/Automation/AutomationDiagnosticsHub.Hdr.cs, Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Audio.cs, Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.CaptureFormat.cs |
| D3D11PreviewRenderer | 12 | 4739 | Sussudio/Services/Preview/D3D11PreviewRenderer.cs, Sussudio/Services/Preview/D3D11PreviewRenderer.DeviceInitialization.cs, Sussudio/Services/Preview/D3D11PreviewRenderer.FrameUpload.cs, Sussudio/Services/Preview/D3D11PreviewRenderer.Metrics.cs, Sussudio/Services/Preview/D3D11PreviewRenderer.PanelBinding.cs, Sussudio/Services/Preview/D3D11PreviewRenderer.PendingFrames.cs, Sussudio/Services/Preview/D3D11PreviewRenderer.RenderPasses.cs, Sussudio/Services/Preview/D3D11PreviewRenderer.RenderThread.cs |
| MainViewModel | 11 | 4545 | Sussudio/Controllers/ViewModel/MainViewModelControllerGraph.cs, Sussudio/ViewModels/MainViewModel.AudioState.cs, Sussudio/ViewModels/MainViewModel.AutomationCommands.cs, Sussudio/ViewModels/MainViewModel.CaptureSelection.cs, Sussudio/ViewModels/MainViewModel.CaptureState.cs, Sussudio/ViewModels/MainViewModel.cs, Sussudio/ViewModels/MainViewModel.DeviceAudioState.cs, Sussudio/ViewModels/MainViewModel.FlashbackExport.cs |
| FlashbackPlaybackController | 9 | 4909 | Sussudio/Services/Flashback/FlashbackPlaybackController.AudioMasterPacing.cs, Sussudio/Services/Flashback/FlashbackPlaybackController.AudioRouting.cs, Sussudio/Services/Flashback/FlashbackPlaybackController.CommandQueue.cs, Sussudio/Services/Flashback/FlashbackPlaybackController.cs, Sussudio/Services/Flashback/FlashbackPlaybackController.DecoderFiles.cs, Sussudio/Services/Flashback/FlashbackPlaybackController.Markers.cs, Sussudio/Services/Flashback/FlashbackPlaybackController.Metrics.cs, Sussudio/Services/Flashback/FlashbackPlaybackController.PlaybackFrames.cs |
| NativeXuAtCommandProvider | 8 | 2583 | Sussudio/Services/Telemetry/NativeXuAtCommandProvider.AtProtocol.cs, Sussudio/Services/Telemetry/NativeXuAtCommandProvider.AudioCommands.cs, Sussudio/Services/Telemetry/NativeXuAtCommandProvider.cs, Sussudio/Services/Telemetry/NativeXuAtCommandProvider.DeviceCommands.cs, Sussudio/Services/Telemetry/NativeXuAtCommandProvider.PayloadDecoding.cs, Sussudio/Services/Telemetry/NativeXuAtCommandProvider.RollingPoll.cs, Sussudio/Services/Telemetry/NativeXuAtCommandProvider.SnapshotAssembly.cs, Sussudio/Services/Telemetry/NativeXuAtCommandProvider.TelemetryDetails.cs |
| FlashbackExporter | 7 | 3042 | Sussudio/Services/Flashback/FlashbackExporter.Execution.cs, Sussudio/Services/Flashback/FlashbackExporter.Lifecycle.cs, Sussudio/Services/Flashback/FlashbackExporter.SegmentPacketWriting.cs, Sussudio/Services/Flashback/FlashbackExporter.Segments.cs, Sussudio/Services/Flashback/FlashbackExporter.SingleFilePacketReadLoop.cs, Sussudio/Services/Flashback/FlashbackExporter.Streams.cs, Sussudio/Services/Flashback/FlashbackExporter.Validation.cs |
| LibAvEncoder | 6 | 2996 | Sussudio/Services/Recording/LibAvEncoder.Audio.cs, Sussudio/Services/Recording/LibAvEncoder.cs, Sussudio/Services/Recording/LibAvEncoder.HardwareFrames.cs, Sussudio/Services/Recording/LibAvEncoder.Initialization.cs, Sussudio/Services/Recording/LibAvEncoder.OutputLifecycle.cs, Sussudio/Services/Recording/LibAvEncoder.VideoSubmission.cs |
| MfSourceReaderVideoCapture | 6 | 2486 | Sussudio/Services/Capture/MfSourceReaderVideoCapture.ComContracts.cs, Sussudio/Services/Capture/MfSourceReaderVideoCapture.cs, Sussudio/Services/Capture/MfSourceReaderVideoCapture.FrameDelivery.cs, Sussudio/Services/Capture/MfSourceReaderVideoCapture.Initialization.cs, Sussudio/Services/Capture/MfSourceReaderVideoCapture.Lifecycle.cs, Sussudio/Services/Capture/MfSourceReaderVideoCapture.Negotiation.cs |
| FlashbackDecoder | 5 | 1974 | Sussudio/Services/Flashback/FlashbackDecoder.AudioOutput.cs, Sussudio/Services/Flashback/FlashbackDecoder.cs, Sussudio/Services/Flashback/FlashbackDecoder.Playback.cs, Sussudio/Services/Flashback/FlashbackDecoder.VideoOutput.cs, Sussudio/Services/Flashback/FlashbackDecoder.VideoSetup.cs |
| FlashbackEncoderSink | 5 | 2658 | Sussudio/Services/Flashback/FlashbackEncoderSink.cs, Sussudio/Services/Flashback/FlashbackEncoderSink.EncodingLoop.cs, Sussudio/Services/Flashback/FlashbackEncoderSink.ForceRotate.cs, Sussudio/Services/Flashback/FlashbackEncoderSink.Queueing.cs, Sussudio/Services/Flashback/FlashbackEncoderSink.Startup.cs |
| MainWindow | 5 | 2466 | Sussudio/MainWindow.ControlBindings.cs, Sussudio/MainWindow.Flashback.Interactions.cs, Sussudio/MainWindow.PreviewLifecycle.Composition.cs, Sussudio/MainWindow.ShellChrome.Composition.cs, Sussudio/MainWindow.xaml.cs |
| LibAvRecordingSink | 4 | 1724 | Sussudio/Services/Recording/LibAvRecordingSink.cs, Sussudio/Services/Recording/LibAvRecordingSink.Queueing.cs, Sussudio/Services/Recording/LibAvRecordingSink.Startup.cs, Sussudio/Services/Recording/LibAvRecordingSink.StopLifecycle.cs |
| UnifiedVideoCapture | 4 | 1386 | Sussudio/Services/Capture/UnifiedVideoCapture.cs, Sussudio/Services/Capture/UnifiedVideoCapture.FrameIngress.cs, Sussudio/Services/Capture/UnifiedVideoCapture.Lifecycle.cs, Sussudio/Services/Capture/UnifiedVideoCapture.SinkFanout.cs |
| DiagnosticSessionResultBuilder | 3 | 1469 | tools/Common/DiagnosticSessionResultBuilder.Analysis.cs, tools/Common/DiagnosticSessionResultBuilder.cs, tools/Common/DiagnosticSessionResultBuilder.Projections.cs |
| ParallelMjpegDecodePipeline | 3 | 1207 | Sussudio/Services/Gpu/ParallelMjpegDecodePipeline.cs, Sussudio/Services/Gpu/ParallelMjpegDecodePipeline.Lifecycle.cs, Sussudio/Services/Gpu/ParallelMjpegDecodePipeline.Reorder.cs |
| FlashbackBufferManager | 3 | 1421 | Sussudio/Services/Flashback/FlashbackBufferManager.cs, Sussudio/Services/Flashback/FlashbackBufferManager.Lifecycle.cs, Sussudio/Services/Flashback/FlashbackBufferManager.Segments.cs |
| WasapiAudioCapture | 3 | 953 | Sussudio/Services/Audio/WasapiAudioCapture.CaptureLoop.cs, Sussudio/Services/Audio/WasapiAudioCapture.Conversion.cs, Sussudio/Services/Audio/WasapiAudioCapture.cs |
| MjpegPreviewJitterBuffer | 3 | 1255 | Sussudio/Services/Capture/MjpegPreviewJitterBuffer.cs, Sussudio/Services/Capture/MjpegPreviewJitterBuffer.FrameIngress.cs, Sussudio/Services/Capture/MjpegPreviewJitterBuffer.FramePacing.cs |
| Formatters | 3 | 949 | tools/ssctl/Formatters.Common.cs, tools/ssctl/Formatters.Snapshot.cs, tools/ssctl/Formatters.Timeline.cs |
| PresentMonProbe | 2 | 1204 | tools/Common/PresentMon/PresentMonProbe.cs, tools/Common/PresentMon/PresentMonProbe.Csv.cs |
| AutomationCommandDispatcher | 2 | 1638 | Sussudio/Services/Automation/AutomationCommandDispatcher.cs, Sussudio/Services/Automation/AutomationCommandDispatcher.CustomCommands.cs |
| App | 1 | 260 | Sussudio/App.xaml.cs |
| LoggingJsonContext | 1 | 306 | Sussudio/Logger.cs |
| DeviceFormatCacheJsonContext | 1 | 697 | Sussudio/Services/Capture/DeviceService.cs |
| StatsWindow | 1 | 308 | Sussudio/StatsWindow.xaml.cs |
| SettingsJsonContext | 1 | 104 | Sussudio/Services/Runtime/SettingsService.cs |

## Largest production files

| Lines | Path |
| ---: | --- |
| 1193 | Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs |
| 1174 | Sussudio/Services/Flashback/FlashbackPlaybackController.PlaybackFrames.cs |
| 1092 | Sussudio/Services/Preview/D3D11PreviewRenderer.Metrics.cs |
| 1054 | tools/ssctl/CommandHandlers.cs |
| 1054 | Sussudio/ViewModels/StatsPresentationBuilder.cs |
| 1034 | tools/Common/DiagnosticSessionFlashbackExportScenarios.cs |
| 1033 | Sussudio/Services/Capture/CaptureService.HealthSnapshots.cs |
| 981 | Sussudio/Services/Recording/Verification/RecordingVerifier.cs |
| 965 | Sussudio/Services/Flashback/FlashbackPlaybackController.ThreadCommands.cs |
| 950 | Sussudio/Services/Capture/CaptureSessionCoordinator.cs |
| 912 | Sussudio/Services/Flashback/FlashbackBackendResources.cs |
| 904 | Sussudio/Services/Automation/AutomationCommandDispatcher.CustomCommands.cs |
| 904 | Sussudio/Services/Flashback/FlashbackExporter.SegmentPacketWriting.cs |
| 884 | tools/NativeXuAudioProbe/Program.I2cCommands.cs |
| 876 | Sussudio/Services/Recording/LibAvEncoder.Audio.cs |
| 868 | Sussudio/Services/Audio/WasapiComInterop.cs |
| 859 | tools/McpServer/Tools/PerformanceTools.cs |
| 840 | Sussudio/Services/Audio/WasapiAudioPlayback.cs |
| 833 | Sussudio/Models/Automation/AutomationSnapshot.cs |
| 830 | Sussudio/Services/Automation/AutomationDiagnosticsHub.Snapshots.cs |
| 814 | Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.AutomationSnapshot.cs |
| 771 | Sussudio/Services/Capture/CaptureService.FlashbackExportCore.cs |
| 757 | tools/Common/AutomationSnapshotFormatter.cs |
| 744 | Sussudio/Services/Automation/AutomationDiagnosticsHub.Timeline.cs |
| 738 | Sussudio/ViewModels/MainViewModel.AudioState.cs |
| 734 | Sussudio/Services/Automation/AutomationCommandDispatcher.cs |
| 731 | Sussudio/Services/Flashback/FlashbackEncoderSink.Queueing.cs |
| 730 | tools/Common/DiagnosticSessionFlashbackMetrics.cs |
| 717 | Sussudio/Services/Flashback/FlashbackBufferManager.Segments.cs |
| 712 | Sussudio/Services/Capture/CaptureService.RuntimeSnapshots.cs |

## Sample production files under 60 lines

| Lines | Path |
| ---: | --- |
| 14 | Sussudio/Services/Contracts/ISourceSignalTelemetryProvider.cs |

## Notes

Use this as the before/after reference for the active defragmentation goal. A lower file count is not sufficient by itself; each slice should also improve behavioral locality, ownership, or deterministic testability.

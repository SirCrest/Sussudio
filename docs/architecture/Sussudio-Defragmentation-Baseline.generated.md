# Sussudio Defragmentation Baseline - Generated

Generated UTC: 2026-05-31T01:39:18Z
Root: C:\Users\crest\source\repos\Sussudio-cleanup-architecture

## Summary

| Metric | Value |
| --- | ---: |
| Production .cs files | 243 |
| Test .cs files | 128 |
| Production .cs files under 60 lines | 0 (0.0%) |
| Production .cs files under 80 lines | 2 (0.8%) |

## Largest partial-type clusters

| Type | Files | Total lines | Sample paths |
| --- | ---: | ---: | --- |
| AutomationDiagnosticsHub | 14 | 9959 | Sussudio/Services/Automation/AutomationDiagnosticsHub.Alerts.cs, Sussudio/Services/Automation/AutomationDiagnosticsHub.cs, Sussudio/Services/Automation/AutomationDiagnosticsHub.DiagnosticEvaluation.cs, Sussudio/Services/Automation/AutomationDiagnosticsHub.Evaluation.cs, Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Audio.cs, Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.CaptureFormat.cs, Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs, Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flashback.cs |
| CaptureService | 13 | 9478 | Sussudio/Services/Capture/CaptureService.AudioPreviewLifecycle.cs, Sussudio/Services/Capture/CaptureService.cs, Sussudio/Services/Capture/CaptureService.FlashbackControls.cs, Sussudio/Services/Capture/CaptureService.FlashbackExportCore.cs, Sussudio/Services/Capture/CaptureService.FlashbackRecording.cs, Sussudio/Services/Capture/CaptureService.HealthSnapshotAssembler.cs, Sussudio/Services/Capture/CaptureService.HealthSnapshots.cs, Sussudio/Services/Capture/CaptureService.PreviewStart.cs |
| MainViewModel | 8 | 4504 | Sussudio/Controllers/ViewModel/MainViewModelControllerGraph.cs, Sussudio/ViewModels/MainViewModel.AudioState.cs, Sussudio/ViewModels/MainViewModel.AutomationCommands.cs, Sussudio/ViewModels/MainViewModel.CaptureSelection.cs, Sussudio/ViewModels/MainViewModel.cs, Sussudio/ViewModels/MainViewModel.DeviceAudioState.cs, Sussudio/ViewModels/MainViewModel.FlashbackState.cs, Sussudio/ViewModels/MainViewModel.SettingsPersistence.cs |
| D3D11PreviewRenderer | 7 | 4689 | Sussudio/Services/Preview/D3D11PreviewRenderer.cs, Sussudio/Services/Preview/D3D11PreviewRenderer.Metrics.cs, Sussudio/Services/Preview/D3D11PreviewRenderer.RenderPasses.cs, Sussudio/Services/Preview/D3D11PreviewRenderer.RenderThread.cs, Sussudio/Services/Preview/D3D11PreviewRenderer.Resources.cs, Sussudio/Services/Preview/D3D11PreviewRenderer.ShaderRendering.cs, Sussudio/Services/Preview/D3D11PreviewRenderer.Submission.cs |
| FlashbackPlaybackController | 6 | 4887 | Sussudio/Services/Flashback/FlashbackPlaybackController.AudioRouting.cs, Sussudio/Services/Flashback/FlashbackPlaybackController.CommandQueue.cs, Sussudio/Services/Flashback/FlashbackPlaybackController.cs, Sussudio/Services/Flashback/FlashbackPlaybackController.PlaybackFrames.cs, Sussudio/Services/Flashback/FlashbackPlaybackController.Positioning.cs, Sussudio/Services/Flashback/FlashbackPlaybackController.ThreadCommands.cs |
| NativeXuAtCommandProvider | 6 | 2565 | Sussudio/Services/Telemetry/NativeXuAtCommandProvider.AtProtocol.cs, Sussudio/Services/Telemetry/NativeXuAtCommandProvider.cs, Sussudio/Services/Telemetry/NativeXuAtCommandProvider.DeviceCommands.cs, Sussudio/Services/Telemetry/NativeXuAtCommandProvider.RollingPoll.cs, Sussudio/Services/Telemetry/NativeXuAtCommandProvider.SnapshotAssembly.cs, Sussudio/Services/Telemetry/NativeXuAtCommandProvider.TelemetryDetails.cs |
| MfSourceReaderVideoCapture | 5 | 2477 | Sussudio/Services/Capture/MfSourceReaderVideoCapture.ComContracts.cs, Sussudio/Services/Capture/MfSourceReaderVideoCapture.cs, Sussudio/Services/Capture/MfSourceReaderVideoCapture.FrameDelivery.cs, Sussudio/Services/Capture/MfSourceReaderVideoCapture.Initialization.cs, Sussudio/Services/Capture/MfSourceReaderVideoCapture.Negotiation.cs |
| LibAvEncoder | 5 | 2988 | Sussudio/Services/Recording/LibAvEncoder.Audio.cs, Sussudio/Services/Recording/LibAvEncoder.cs, Sussudio/Services/Recording/LibAvEncoder.HardwareFrames.cs, Sussudio/Services/Recording/LibAvEncoder.OutputLifecycle.cs, Sussudio/Services/Recording/LibAvEncoder.VideoSubmission.cs |
| MainWindow | 5 | 2466 | Sussudio/MainWindow.ControlBindings.cs, Sussudio/MainWindow.Flashback.Interactions.cs, Sussudio/MainWindow.PreviewLifecycle.Composition.cs, Sussudio/MainWindow.ShellChrome.Composition.cs, Sussudio/MainWindow.xaml.cs |
| FlashbackEncoderSink | 4 | 2647 | Sussudio/Services/Flashback/FlashbackEncoderSink.cs, Sussudio/Services/Flashback/FlashbackEncoderSink.EncodingLoop.cs, Sussudio/Services/Flashback/FlashbackEncoderSink.ForceRotate.cs, Sussudio/Services/Flashback/FlashbackEncoderSink.Queueing.cs |
| FlashbackDecoder | 4 | 1964 | Sussudio/Services/Flashback/FlashbackDecoder.cs, Sussudio/Services/Flashback/FlashbackDecoder.Playback.cs, Sussudio/Services/Flashback/FlashbackDecoder.VideoOutput.cs, Sussudio/Services/Flashback/FlashbackDecoder.VideoSetup.cs |
| FlashbackExporter | 4 | 3014 | Sussudio/Services/Flashback/FlashbackExporter.Execution.cs, Sussudio/Services/Flashback/FlashbackExporter.Lifecycle.cs, Sussudio/Services/Flashback/FlashbackExporter.SegmentPacketWriting.cs, Sussudio/Services/Flashback/FlashbackExporter.SingleFilePacketReadLoop.cs |
| LibAvRecordingSink | 3 | 1713 | Sussudio/Services/Recording/LibAvRecordingSink.cs, Sussudio/Services/Recording/LibAvRecordingSink.Queueing.cs, Sussudio/Services/Recording/LibAvRecordingSink.StopLifecycle.cs |
| DiagnosticSessionResultBuilder | 3 | 1469 | tools/Common/DiagnosticSessionResultBuilder.Analysis.cs, tools/Common/DiagnosticSessionResultBuilder.cs, tools/Common/DiagnosticSessionResultBuilder.Projections.cs |
| UnifiedVideoCapture | 3 | 1375 | Sussudio/Services/Capture/UnifiedVideoCapture.cs, Sussudio/Services/Capture/UnifiedVideoCapture.FrameIngress.cs, Sussudio/Services/Capture/UnifiedVideoCapture.SinkFanout.cs |
| PresentMonProbe | 2 | 1204 | tools/Common/PresentMon/PresentMonProbe.cs, tools/Common/PresentMon/PresentMonProbe.Csv.cs |
| ParallelMjpegDecodePipeline | 2 | 1197 | Sussudio/Services/Gpu/ParallelMjpegDecodePipeline.cs, Sussudio/Services/Gpu/ParallelMjpegDecodePipeline.Reorder.cs |
| FlashbackBufferManager | 2 | 1413 | Sussudio/Services/Flashback/FlashbackBufferManager.cs, Sussudio/Services/Flashback/FlashbackBufferManager.Segments.cs |
| AutomationCommandDispatcher | 2 | 1638 | Sussudio/Services/Automation/AutomationCommandDispatcher.cs, Sussudio/Services/Automation/AutomationCommandDispatcher.CustomCommands.cs |
| Formatters | 2 | 941 | tools/ssctl/Formatters.Common.cs, tools/ssctl/Formatters.Snapshot.cs |
| MjpegPreviewJitterBuffer | 2 | 1246 | Sussudio/Services/Capture/MjpegPreviewJitterBuffer.cs, Sussudio/Services/Capture/MjpegPreviewJitterBuffer.FramePacing.cs |
| LoggingJsonContext | 1 | 306 | Sussudio/Logger.cs |
| App | 1 | 260 | Sussudio/App.xaml.cs |
| DeviceFormatCacheJsonContext | 1 | 697 | Sussudio/Services/Capture/DeviceService.cs |
| SettingsJsonContext | 1 | 690 | Sussudio/Services/Runtime/RuntimeHelpers.cs |
| StatsWindow | 1 | 308 | Sussudio/StatsWindow.xaml.cs |

## Largest production files

| Lines | Path |
| ---: | --- |
| 1252 | Sussudio/Services/Capture/CaptureService.FlashbackExportCore.cs |
| 1193 | Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs |
| 1174 | Sussudio/Services/Flashback/FlashbackPlaybackController.PlaybackFrames.cs |
| 1144 | Sussudio/Services/Flashback/FlashbackExporter.SegmentPacketWriting.cs |
| 1092 | Sussudio/Services/Preview/D3D11PreviewRenderer.Metrics.cs |
| 1055 | Sussudio/Services/Flashback/FlashbackEncoderSink.cs |
| 1054 | tools/ssctl/CommandHandlers.cs |
| 1054 | Sussudio/ViewModels/StatsPresentationBuilder.cs |
| 1053 | Sussudio/Services/Capture/CaptureService.RecordingLifecycle.cs |
| 1034 | tools/Common/DiagnosticSessionFlashbackExportScenarios.cs |
| 1033 | Sussudio/Services/Capture/CaptureService.HealthSnapshots.cs |
| 981 | Sussudio/Services/Recording/Verification/RecordingVerifier.cs |
| 979 | Sussudio/Services/Automation/AutomationDiagnosticsHub.cs |
| 965 | Sussudio/Services/Flashback/FlashbackPlaybackController.ThreadCommands.cs |
| 960 | Sussudio/Services/Preview/D3D11PreviewRenderer.RenderPasses.cs |
| 954 | Sussudio/Services/Flashback/FlashbackExporter.Execution.cs |
| 950 | Sussudio/Services/Capture/CaptureSessionCoordinator.cs |
| 937 | Sussudio/Services/Audio/WasapiAudioCapture.cs |
| 912 | Sussudio/Services/Flashback/FlashbackBackendResources.cs |
| 905 | Sussudio/Services/Capture/MjpegPreviewJitterBuffer.cs |
| 904 | Sussudio/Services/Automation/AutomationCommandDispatcher.CustomCommands.cs |
| 884 | tools/NativeXuAudioProbe/Program.I2cCommands.cs |
| 876 | Sussudio/Services/Recording/LibAvEncoder.Audio.cs |
| 868 | Sussudio/Services/Audio/WasapiComInterop.cs |
| 859 | tools/McpServer/Tools/PerformanceTools.cs |
| 840 | Sussudio/Services/Audio/WasapiAudioPlayback.cs |
| 833 | Sussudio/Models/Automation/AutomationSnapshot.cs |
| 831 | Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Preview.cs |
| 830 | Sussudio/Services/Automation/AutomationDiagnosticsHub.Snapshots.cs |
| 823 | Sussudio/ViewModels/MainViewModel.cs |

## Sample production files under 60 lines

| Lines | Path |
| ---: | --- |

## Notes

Use this as the before/after reference for the active defragmentation goal. A lower file count is not sufficient by itself; each slice should also improve behavioral locality, ownership, or deterministic testability.

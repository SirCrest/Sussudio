# Sussudio Defragmentation Baseline - Generated

Generated UTC: 2026-05-27T12:53:32Z
Root: C:\Users\crest\source\repos\Sussudio-cleanup-architecture

## Summary

| Metric | Value |
| --- | ---: |
| Production .cs files | 266 |
| Test .cs files | 140 |
| Production .cs files under 60 lines | 1 (0.4%) |
| Production .cs files under 80 lines | 4 (1.5%) |

## Largest partial-type clusters

| Type | Files | Total lines | Sample paths |
| --- | ---: | ---: | --- |
| AutomationDiagnosticsHub | 16 | 9973 | Sussudio/Services/Automation/AutomationDiagnosticsHub.Alerts.cs, Sussudio/Services/Automation/AutomationDiagnosticsHub.cs, Sussudio/Services/Automation/AutomationDiagnosticsHub.DiagnosticEvaluation.cs, Sussudio/Services/Automation/AutomationDiagnosticsHub.Evaluation.cs, Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Audio.cs, Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.CaptureFormat.cs, Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs, Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flashback.cs |
| CaptureService | 16 | 9515 | Sussudio/Services/Capture/CaptureService.AudioPreviewLifecycle.cs, Sussudio/Services/Capture/CaptureService.Cleanup.cs, Sussudio/Services/Capture/CaptureService.cs, Sussudio/Services/Capture/CaptureService.FlashbackControls.cs, Sussudio/Services/Capture/CaptureService.FlashbackExportCore.cs, Sussudio/Services/Capture/CaptureService.FlashbackExportDiagnostics.cs, Sussudio/Services/Capture/CaptureService.FlashbackRecording.cs, Sussudio/Services/Capture/CaptureService.HealthSnapshotAssembler.cs |
| D3D11PreviewRenderer | 9 | 4710 | Sussudio/Services/Preview/D3D11PreviewRenderer.cs, Sussudio/Services/Preview/D3D11PreviewRenderer.DeviceInitialization.cs, Sussudio/Services/Preview/D3D11PreviewRenderer.Metrics.cs, Sussudio/Services/Preview/D3D11PreviewRenderer.RenderPasses.cs, Sussudio/Services/Preview/D3D11PreviewRenderer.RenderThread.cs, Sussudio/Services/Preview/D3D11PreviewRenderer.Resources.cs, Sussudio/Services/Preview/D3D11PreviewRenderer.ScreenshotCapture.cs, Sussudio/Services/Preview/D3D11PreviewRenderer.ShaderRendering.cs |
| MainViewModel | 8 | 4504 | Sussudio/Controllers/ViewModel/MainViewModelControllerGraph.cs, Sussudio/ViewModels/MainViewModel.AudioState.cs, Sussudio/ViewModels/MainViewModel.AutomationCommands.cs, Sussudio/ViewModels/MainViewModel.CaptureSelection.cs, Sussudio/ViewModels/MainViewModel.cs, Sussudio/ViewModels/MainViewModel.DeviceAudioState.cs, Sussudio/ViewModels/MainViewModel.FlashbackState.cs, Sussudio/ViewModels/MainViewModel.SettingsPersistence.cs |
| MfSourceReaderVideoCapture | 6 | 2486 | Sussudio/Services/Capture/MfSourceReaderVideoCapture.ComContracts.cs, Sussudio/Services/Capture/MfSourceReaderVideoCapture.cs, Sussudio/Services/Capture/MfSourceReaderVideoCapture.FrameDelivery.cs, Sussudio/Services/Capture/MfSourceReaderVideoCapture.Initialization.cs, Sussudio/Services/Capture/MfSourceReaderVideoCapture.Lifecycle.cs, Sussudio/Services/Capture/MfSourceReaderVideoCapture.Negotiation.cs |
| FlashbackPlaybackController | 6 | 4887 | Sussudio/Services/Flashback/FlashbackPlaybackController.AudioRouting.cs, Sussudio/Services/Flashback/FlashbackPlaybackController.CommandQueue.cs, Sussudio/Services/Flashback/FlashbackPlaybackController.cs, Sussudio/Services/Flashback/FlashbackPlaybackController.PlaybackFrames.cs, Sussudio/Services/Flashback/FlashbackPlaybackController.Positioning.cs, Sussudio/Services/Flashback/FlashbackPlaybackController.ThreadCommands.cs |
| LibAvEncoder | 6 | 2996 | Sussudio/Services/Recording/LibAvEncoder.Audio.cs, Sussudio/Services/Recording/LibAvEncoder.cs, Sussudio/Services/Recording/LibAvEncoder.HardwareFrames.cs, Sussudio/Services/Recording/LibAvEncoder.Initialization.cs, Sussudio/Services/Recording/LibAvEncoder.OutputLifecycle.cs, Sussudio/Services/Recording/LibAvEncoder.VideoSubmission.cs |
| NativeXuAtCommandProvider | 6 | 2565 | Sussudio/Services/Telemetry/NativeXuAtCommandProvider.AtProtocol.cs, Sussudio/Services/Telemetry/NativeXuAtCommandProvider.cs, Sussudio/Services/Telemetry/NativeXuAtCommandProvider.DeviceCommands.cs, Sussudio/Services/Telemetry/NativeXuAtCommandProvider.RollingPoll.cs, Sussudio/Services/Telemetry/NativeXuAtCommandProvider.SnapshotAssembly.cs, Sussudio/Services/Telemetry/NativeXuAtCommandProvider.TelemetryDetails.cs |
| FlashbackExporter | 5 | 3024 | Sussudio/Services/Flashback/FlashbackExporter.Execution.cs, Sussudio/Services/Flashback/FlashbackExporter.Lifecycle.cs, Sussudio/Services/Flashback/FlashbackExporter.SegmentPacketWriting.cs, Sussudio/Services/Flashback/FlashbackExporter.SingleFilePacketReadLoop.cs, Sussudio/Services/Flashback/FlashbackExporter.Validation.cs |
| FlashbackEncoderSink | 5 | 2658 | Sussudio/Services/Flashback/FlashbackEncoderSink.cs, Sussudio/Services/Flashback/FlashbackEncoderSink.EncodingLoop.cs, Sussudio/Services/Flashback/FlashbackEncoderSink.ForceRotate.cs, Sussudio/Services/Flashback/FlashbackEncoderSink.Queueing.cs, Sussudio/Services/Flashback/FlashbackEncoderSink.Startup.cs |
| MainWindow | 5 | 2466 | Sussudio/MainWindow.ControlBindings.cs, Sussudio/MainWindow.Flashback.Interactions.cs, Sussudio/MainWindow.PreviewLifecycle.Composition.cs, Sussudio/MainWindow.ShellChrome.Composition.cs, Sussudio/MainWindow.xaml.cs |
| FlashbackDecoder | 4 | 1964 | Sussudio/Services/Flashback/FlashbackDecoder.cs, Sussudio/Services/Flashback/FlashbackDecoder.Playback.cs, Sussudio/Services/Flashback/FlashbackDecoder.VideoOutput.cs, Sussudio/Services/Flashback/FlashbackDecoder.VideoSetup.cs |
| UnifiedVideoCapture | 4 | 1386 | Sussudio/Services/Capture/UnifiedVideoCapture.cs, Sussudio/Services/Capture/UnifiedVideoCapture.FrameIngress.cs, Sussudio/Services/Capture/UnifiedVideoCapture.Lifecycle.cs, Sussudio/Services/Capture/UnifiedVideoCapture.SinkFanout.cs |
| DiagnosticSessionResultBuilder | 3 | 1469 | tools/Common/DiagnosticSessionResultBuilder.Analysis.cs, tools/Common/DiagnosticSessionResultBuilder.cs, tools/Common/DiagnosticSessionResultBuilder.Projections.cs |
| LibAvRecordingSink | 3 | 1713 | Sussudio/Services/Recording/LibAvRecordingSink.cs, Sussudio/Services/Recording/LibAvRecordingSink.Queueing.cs, Sussudio/Services/Recording/LibAvRecordingSink.StopLifecycle.cs |
| MjpegPreviewJitterBuffer | 3 | 1255 | Sussudio/Services/Capture/MjpegPreviewJitterBuffer.cs, Sussudio/Services/Capture/MjpegPreviewJitterBuffer.FrameIngress.cs, Sussudio/Services/Capture/MjpegPreviewJitterBuffer.FramePacing.cs |
| PresentMonProbe | 2 | 1204 | tools/Common/PresentMon/PresentMonProbe.cs, tools/Common/PresentMon/PresentMonProbe.Csv.cs |
| ParallelMjpegDecodePipeline | 2 | 1197 | Sussudio/Services/Gpu/ParallelMjpegDecodePipeline.cs, Sussudio/Services/Gpu/ParallelMjpegDecodePipeline.Reorder.cs |
| WasapiAudioCapture | 2 | 947 | Sussudio/Services/Audio/WasapiAudioCapture.CaptureLoop.cs, Sussudio/Services/Audio/WasapiAudioCapture.cs |
| FlashbackBufferManager | 2 | 1413 | Sussudio/Services/Flashback/FlashbackBufferManager.cs, Sussudio/Services/Flashback/FlashbackBufferManager.Segments.cs |
| AutomationCommandDispatcher | 2 | 1638 | Sussudio/Services/Automation/AutomationCommandDispatcher.cs, Sussudio/Services/Automation/AutomationCommandDispatcher.CustomCommands.cs |
| Formatters | 2 | 941 | tools/ssctl/Formatters.Common.cs, tools/ssctl/Formatters.Snapshot.cs |
| LoggingJsonContext | 1 | 306 | Sussudio/Logger.cs |
| App | 1 | 260 | Sussudio/App.xaml.cs |
| DeviceFormatCacheJsonContext | 1 | 697 | Sussudio/Services/Capture/DeviceService.cs |
| SettingsJsonContext | 1 | 104 | Sussudio/Services/Runtime/SettingsService.cs |
| StatsWindow | 1 | 308 | Sussudio/StatsWindow.xaml.cs |

## Largest production files

| Lines | Path |
| ---: | --- |
| 1193 | Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs |
| 1174 | Sussudio/Services/Flashback/FlashbackPlaybackController.PlaybackFrames.cs |
| 1144 | Sussudio/Services/Flashback/FlashbackExporter.SegmentPacketWriting.cs |
| 1092 | Sussudio/Services/Preview/D3D11PreviewRenderer.Metrics.cs |
| 1054 | Sussudio/ViewModels/StatsPresentationBuilder.cs |
| 1054 | tools/ssctl/CommandHandlers.cs |
| 1034 | tools/Common/DiagnosticSessionFlashbackExportScenarios.cs |
| 1033 | Sussudio/Services/Capture/CaptureService.HealthSnapshots.cs |
| 981 | Sussudio/Services/Recording/Verification/RecordingVerifier.cs |
| 965 | Sussudio/Services/Flashback/FlashbackPlaybackController.ThreadCommands.cs |
| 950 | Sussudio/Services/Capture/CaptureSessionCoordinator.cs |
| 912 | Sussudio/Services/Flashback/FlashbackBackendResources.cs |
| 904 | Sussudio/Services/Automation/AutomationCommandDispatcher.CustomCommands.cs |
| 884 | tools/NativeXuAudioProbe/Program.I2cCommands.cs |
| 876 | Sussudio/Services/Recording/LibAvEncoder.Audio.cs |
| 868 | Sussudio/Services/Audio/WasapiComInterop.cs |
| 859 | tools/McpServer/Tools/PerformanceTools.cs |
| 840 | Sussudio/Services/Audio/WasapiAudioPlayback.cs |
| 833 | Sussudio/Models/Automation/AutomationSnapshot.cs |
| 830 | Sussudio/Services/Automation/AutomationDiagnosticsHub.Snapshots.cs |
| 823 | Sussudio/Services/Gpu/ParallelMjpegDecodePipeline.cs |
| 823 | Sussudio/ViewModels/MainViewModel.cs |
| 814 | Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.AutomationSnapshot.cs |
| 790 | tools/NativeXuAudioProbe/Program.cs |
| 781 | Sussudio/Services/Recording/LibAvRecordingSink.cs |
| 771 | Sussudio/Services/Capture/CaptureService.FlashbackExportCore.cs |
| 765 | Sussudio/Services/Automation/AutomationDiagnosticsHub.DiagnosticEvaluation.cs |
| 757 | tools/Common/AutomationSnapshotFormatter.cs |
| 751 | Sussudio/Services/Capture/CaptureService.Snapshots.cs |
| 746 | Sussudio/ViewModels/MainViewModel.FlashbackState.cs |

## Sample production files under 60 lines

| Lines | Path |
| ---: | --- |
| 14 | Sussudio/Services/Contracts/ISourceSignalTelemetryProvider.cs |

## Notes

Use this as the before/after reference for the active defragmentation goal. A lower file count is not sufficient by itself; each slice should also improve behavioral locality, ownership, or deterministic testability.

# Sussudio Defragmentation Baseline - Generated

Generated UTC: 2026-05-31T09:08:28Z
Root: C:\Users\crest\source\repos\Sussudio-cleanup-architecture

## Summary

| Metric | Value |
| --- | ---: |
| Production .cs files | 206 |
| Test .cs files | 108 |
| Production .cs files under 60 lines | 0 (0.0%) |
| Production .cs files under 80 lines | 2 (1.0%) |

## Largest partial-type clusters

| Type | Files | Total lines | Sample paths |
| --- | ---: | ---: | --- |
| CaptureService | 12 | 9470 | Sussudio/Services/Capture/CaptureService.AudioPreviewLifecycle.cs, Sussudio/Services/Capture/CaptureService.cs, Sussudio/Services/Capture/CaptureService.FlashbackControls.cs, Sussudio/Services/Capture/CaptureService.FlashbackExportCore.cs, Sussudio/Services/Capture/CaptureService.FlashbackRecording.cs, Sussudio/Services/Capture/CaptureService.HealthSnapshotAssembler.cs, Sussudio/Services/Capture/CaptureService.HealthSnapshots.cs, Sussudio/Services/Capture/CaptureService.PreviewStart.cs |
| AutomationDiagnosticsHub | 11 | 9941 | Sussudio/Services/Automation/AutomationDiagnosticsHub.Alerts.cs, Sussudio/Services/Automation/AutomationDiagnosticsHub.cs, Sussudio/Services/Automation/AutomationDiagnosticsHub.Evaluation.cs, Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.CaptureFormat.cs, Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs, Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flashback.cs, Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.AutomationSnapshot.cs, Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Media.cs |
| MainViewModel | 7 | 4492 | Sussudio/Controllers/ViewModel/MainViewModelControllerGraph.cs, Sussudio/ViewModels/MainViewModel.AudioState.cs, Sussudio/ViewModels/MainViewModel.AutomationCommands.cs, Sussudio/ViewModels/MainViewModel.CaptureSelection.cs, Sussudio/ViewModels/MainViewModel.cs, Sussudio/ViewModels/MainViewModel.FlashbackState.cs, Sussudio/ViewModels/MainViewModel.SettingsPersistence.cs |
| FlashbackPlaybackController | 6 | 4887 | Sussudio/Services/Flashback/FlashbackPlaybackController.AudioRouting.cs, Sussudio/Services/Flashback/FlashbackPlaybackController.CommandQueue.cs, Sussudio/Services/Flashback/FlashbackPlaybackController.cs, Sussudio/Services/Flashback/FlashbackPlaybackController.PlaybackFrames.cs, Sussudio/Services/Flashback/FlashbackPlaybackController.Positioning.cs, Sussudio/Services/Flashback/FlashbackPlaybackController.ThreadCommands.cs |
| MfSourceReaderVideoCapture | 5 | 2477 | Sussudio/Services/Capture/MfSourceReaderVideoCapture.ComContracts.cs, Sussudio/Services/Capture/MfSourceReaderVideoCapture.cs, Sussudio/Services/Capture/MfSourceReaderVideoCapture.FrameDelivery.cs, Sussudio/Services/Capture/MfSourceReaderVideoCapture.Initialization.cs, Sussudio/Services/Capture/MfSourceReaderVideoCapture.Negotiation.cs |
| D3D11PreviewRenderer | 5 | 4663 | Sussudio/Services/Preview/D3D11PreviewRenderer.cs, Sussudio/Services/Preview/D3D11PreviewRenderer.Metrics.cs, Sussudio/Services/Preview/D3D11PreviewRenderer.RenderPasses.cs, Sussudio/Services/Preview/D3D11PreviewRenderer.Resources.cs, Sussudio/Services/Preview/D3D11PreviewRenderer.ShaderRendering.cs |
| LibAvEncoder | 4 | 2979 | Sussudio/Services/Recording/LibAvEncoder.Audio.cs, Sussudio/Services/Recording/LibAvEncoder.cs, Sussudio/Services/Recording/LibAvEncoder.OutputLifecycle.cs, Sussudio/Services/Recording/LibAvEncoder.VideoFrames.cs |
| MainWindow | 4 | 2457 | Sussudio/MainWindow.ControlBindings.cs, Sussudio/MainWindow.PreviewLifecycle.Composition.cs, Sussudio/MainWindow.ShellChrome.Composition.cs, Sussudio/MainWindow.xaml.cs |
| NativeXuAtCommandProvider | 4 | 2549 | Sussudio/Services/Telemetry/NativeXuAtCommandProvider.AtProtocol.cs, Sussudio/Services/Telemetry/NativeXuAtCommandProvider.cs, Sussudio/Services/Telemetry/NativeXuAtCommandProvider.DeviceCommands.cs, Sussudio/Services/Telemetry/NativeXuAtCommandProvider.SnapshotAssembly.cs |
| FlashbackExporter | 3 | 3000 | Sussudio/Services/Flashback/FlashbackExporter.Execution.cs, Sussudio/Services/Flashback/FlashbackExporter.Lifecycle.cs, Sussudio/Services/Flashback/FlashbackExporter.SegmentPacketWriting.cs |
| FlashbackDecoder | 3 | 1953 | Sussudio/Services/Flashback/FlashbackDecoder.cs, Sussudio/Services/Flashback/FlashbackDecoder.Playback.cs, Sussudio/Services/Flashback/FlashbackDecoder.VideoSetup.cs |
| DiagnosticSessionResultBuilder | 3 | 1469 | tools/Common/DiagnosticSessionResultBuilder.Analysis.cs, tools/Common/DiagnosticSessionResultBuilder.cs, tools/Common/DiagnosticSessionResultBuilder.Projections.cs |
| FlashbackEncoderSink | 3 | 2639 | Sussudio/Services/Flashback/FlashbackEncoderSink.cs, Sussudio/Services/Flashback/FlashbackEncoderSink.EncodingLoop.cs, Sussudio/Services/Flashback/FlashbackEncoderSink.Queueing.cs |
| PresentMonProbe | 2 | 1204 | tools/Common/PresentMon/PresentMonProbe.cs, tools/Common/PresentMon/PresentMonProbe.Csv.cs |
| LibAvRecordingSink | 2 | 1703 | Sussudio/Services/Recording/LibAvRecordingSink.cs, Sussudio/Services/Recording/LibAvRecordingSink.Queueing.cs |
| AutomationCommandDispatcher | 2 | 1638 | Sussudio/Services/Automation/AutomationCommandDispatcher.cs, Sussudio/Services/Automation/AutomationCommandDispatcher.CustomCommands.cs |
| FlashbackBufferManager | 2 | 1413 | Sussudio/Services/Flashback/FlashbackBufferManager.cs, Sussudio/Services/Flashback/FlashbackBufferManager.Segments.cs |
| UnifiedVideoCapture | 2 | 1365 | Sussudio/Services/Capture/UnifiedVideoCapture.cs, Sussudio/Services/Capture/UnifiedVideoCapture.FrameIngress.cs |
| Formatters | 2 | 941 | tools/ssctl/Formatters.Common.cs, tools/ssctl/Formatters.Snapshot.cs |
| StatsWindow | 1 | 308 | Sussudio/StatsWindow.xaml.cs |
| App | 1 | 260 | Sussudio/App.xaml.cs |
| LoggingJsonContext | 1 | 306 | Sussudio/Logger.cs |
| MjpegPreviewJitterBuffer | 1 | 1236 | Sussudio/Services/Capture/MjpegPreviewJitterBuffer.cs |
| ParallelMjpegDecodePipeline | 1 | 1186 | Sussudio/Services/Gpu/ParallelMjpegDecodePipeline.cs |
| SettingsJsonContext | 1 | 690 | Sussudio/Services/Runtime/RuntimeHelpers.cs |
| DeviceFormatCacheJsonContext | 1 | 697 | Sussudio/Services/Capture/DeviceService.cs |

## Largest production files

| Lines | Path |
| ---: | --- |
| 1472 | Sussudio/Controllers/Flashback/FlashbackUiControllers.cs |
| 1363 | Sussudio/Services/Automation/AutomationDiagnosticsHub.Evaluation.cs |
| 1313 | Sussudio/Services/Preview/D3D11PreviewRenderer.cs |
| 1285 | Sussudio/Services/Flashback/FlashbackExporter.Execution.cs |
| 1252 | Sussudio/Services/Capture/CaptureService.FlashbackExportCore.cs |
| 1236 | Sussudio/Services/Capture/MjpegPreviewJitterBuffer.cs |
| 1193 | Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs |
| 1186 | Sussudio/Services/Gpu/ParallelMjpegDecodePipeline.cs |
| 1174 | Sussudio/Services/Flashback/FlashbackPlaybackController.PlaybackFrames.cs |
| 1144 | Sussudio/Services/Flashback/FlashbackExporter.SegmentPacketWriting.cs |
| 1092 | Sussudio/Services/Preview/D3D11PreviewRenderer.Metrics.cs |
| 1065 | Sussudio/Services/Recording/LibAvRecordingSink.cs |
| 1055 | Sussudio/Services/Flashback/FlashbackEncoderSink.cs |
| 1055 | Sussudio/ViewModels/MainViewModel.AudioState.cs |
| 1054 | tools/ssctl/CommandHandlers.cs |
| 1054 | Sussudio/ViewModels/StatsPresentationBuilder.cs |
| 1053 | Sussudio/Services/Capture/CaptureService.RecordingLifecycle.cs |
| 1034 | tools/Common/DiagnosticSessionFlashbackExportScenarios.cs |
| 1033 | Sussudio/Services/Capture/CaptureService.HealthSnapshots.cs |
| 1023 | Sussudio/Controllers/Stats/StatsOverlayCompositionController.cs |
| 1011 | Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flashback.cs |
| 996 | Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Media.cs |
| 981 | Sussudio/Services/Recording/Verification/RecordingVerifier.cs |
| 981 | Sussudio/Services/Capture/CaptureService.RuntimeSnapshots.cs |
| 979 | Sussudio/Services/Automation/AutomationDiagnosticsHub.cs |
| 965 | Sussudio/Services/Flashback/FlashbackPlaybackController.ThreadCommands.cs |
| 960 | Sussudio/Services/Preview/D3D11PreviewRenderer.RenderPasses.cs |
| 950 | Sussudio/Services/Capture/CaptureSessionCoordinator.cs |
| 937 | Sussudio/Services/Audio/WasapiAudioCapture.cs |
| 912 | Sussudio/Services/Flashback/FlashbackBackendResources.cs |

## Sample production files under 60 lines

| Lines | Path |
| ---: | --- |

## Notes

Use this as the before/after reference for the active defragmentation goal. A lower file count is not sufficient by itself; each slice should also improve behavioral locality, ownership, or deterministic testability.

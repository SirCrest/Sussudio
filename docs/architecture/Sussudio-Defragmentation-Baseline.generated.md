# Sussudio Defragmentation Baseline - Generated

Generated UTC: 2026-05-31T22:19:45Z
Root: C:\Users\crest\source\repos\Sussudio-cleanup-architecture

## Summary

| Metric | Value |
| --- | ---: |
| Production .cs files | 178 |
| Test .cs files | 60 |
| Core app .cs files (Sussudio/) | 136 |
| Core app nonblank LoC (Sussudio/) | 89684 |
| Sussudio.Tests .cs files | 58 |
| Sussudio.Tests nonblank LoC | 55992 |
| Production .cs files under 60 lines | 0 (0.0%) |
| Production .cs files under 80 lines | 1 (0.6%) |

## Largest partial-type clusters

| Type | Files | Total lines | Sample paths |
| --- | ---: | ---: | --- |
| AutomationDiagnosticsHub | 10 | 9934 | Sussudio/Services/Automation/AutomationDiagnosticsHub.Alerts.cs, Sussudio/Services/Automation/AutomationDiagnosticsHub.cs, Sussudio/Services/Automation/AutomationDiagnosticsHub.Evaluation.cs, Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.CaptureFormat.cs, Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs, Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flashback.cs, Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.AutomationSnapshot.cs, Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Media.cs |
| CaptureService | 10 | 9452 | Sussudio/Services/Capture/CaptureService.cs, Sussudio/Services/Capture/CaptureService.FlashbackControls.cs, Sussudio/Services/Capture/CaptureService.FlashbackExportCore.cs, Sussudio/Services/Capture/CaptureService.FlashbackRecording.cs, Sussudio/Services/Capture/CaptureService.HealthSnapshots.cs, Sussudio/Services/Capture/CaptureService.PreviewLifecycle.cs, Sussudio/Services/Capture/CaptureService.RecordingIntegrity.cs, Sussudio/Services/Capture/CaptureService.RecordingLifecycle.cs |
| MainViewModel | 6 | 4479 | Sussudio/Controllers/ViewModel/MainViewModelControllerGraph.cs, Sussudio/ViewModels/MainViewModel.AudioState.cs, Sussudio/ViewModels/MainViewModel.CaptureSelection.cs, Sussudio/ViewModels/MainViewModel.cs, Sussudio/ViewModels/MainViewModel.FlashbackState.cs, Sussudio/ViewModels/MainViewModel.SettingsPersistence.cs |
| FlashbackPlaybackController | 5 | 4872 | Sussudio/Services/Flashback/FlashbackPlaybackController.AudioRouting.cs, Sussudio/Services/Flashback/FlashbackPlaybackController.cs, Sussudio/Services/Flashback/FlashbackPlaybackController.PlaybackFrames.cs, Sussudio/Services/Flashback/FlashbackPlaybackController.Positioning.cs, Sussudio/Services/Flashback/FlashbackPlaybackController.ThreadCommands.cs |
| MfSourceReaderVideoCapture | 4 | 2603 | Sussudio/Services/Capture/MfInterop.cs, Sussudio/Services/Capture/MfSourceReaderVideoCapture.cs, Sussudio/Services/Capture/MfSourceReaderVideoCapture.FrameDelivery.cs, Sussudio/Services/Capture/MfSourceReaderVideoCapture.Negotiation.cs |
| D3D11PreviewRenderer | 4 | 4871 | Sussudio/Services/Preview/D3D11PreviewRenderer.cs, Sussudio/Services/Preview/D3D11PreviewRenderer.Metrics.cs, Sussudio/Services/Preview/D3D11PreviewRenderer.RenderPasses.cs, Sussudio/Services/Preview/D3D11PreviewRenderer.Resources.cs |
| FlashbackDecoder | 3 | 1953 | Sussudio/Services/Flashback/FlashbackDecoder.cs, Sussudio/Services/Flashback/FlashbackDecoder.Playback.cs, Sussudio/Services/Flashback/FlashbackDecoder.VideoSetup.cs |
| NativeXuAtCommandProvider | 3 | 2539 | Sussudio/Services/Telemetry/NativeXuAtCommandProvider.cs, Sussudio/Services/Telemetry/NativeXuAtCommandProvider.DeviceCommands.cs, Sussudio/Services/Telemetry/NativeXuAtCommandProvider.SnapshotAssembly.cs |
| FlashbackExporter | 3 | 3000 | Sussudio/Services/Flashback/FlashbackExporter.Execution.cs, Sussudio/Services/Flashback/FlashbackExporter.Lifecycle.cs, Sussudio/Services/Flashback/FlashbackExporter.SegmentPacketWriting.cs |
| FlashbackEncoderSink | 3 | 2639 | Sussudio/Services/Flashback/FlashbackEncoderSink.cs, Sussudio/Services/Flashback/FlashbackEncoderSink.EncodingLoop.cs, Sussudio/Services/Flashback/FlashbackEncoderSink.Queueing.cs |
| LibAvEncoder | 3 | 2972 | Sussudio/Services/Recording/LibAvEncoder.Audio.cs, Sussudio/Services/Recording/LibAvEncoder.cs, Sussudio/Services/Recording/LibAvEncoder.VideoFrames.cs |
| AutomationCommandDispatcher | 2 | 1638 | Sussudio/Services/Automation/AutomationCommandDispatcher.cs, Sussudio/Services/Automation/AutomationCommandDispatcher.CustomCommands.cs |
| MainWindow | 2 | 2434 | Sussudio/MainWindow.Composition.cs, Sussudio/MainWindow.xaml.cs |
| LibAvRecordingSink | 2 | 1889 | Sussudio/Services/Recording/LibAvRecordingSink.cs, Sussudio/Services/Recording/LibAvRecordingSink.Queueing.cs |
| SettingsJsonContext | 1 | 690 | Sussudio/Services/Runtime/RuntimeHelpers.cs |
| App | 1 | 260 | Sussudio/App.xaml.cs |
| StatsWindow | 1 | 308 | Sussudio/StatsWindow.xaml.cs |
| LoggingJsonContext | 1 | 306 | Sussudio/Logger.cs |
| DeviceFormatCacheJsonContext | 1 | 697 | Sussudio/Services/Capture/DeviceService.cs |
| ParallelMjpegDecodePipeline | 1 | 1186 | Sussudio/Services/Gpu/ParallelMjpegDecodePipeline.cs |
| MjpegPreviewJitterBuffer | 1 | 1236 | Sussudio/Services/Capture/MjpegPreviewJitterBuffer.cs |

## Largest production files

| Lines | Path |
| ---: | --- |
| 1562 | Sussudio/Services/Capture/UnifiedVideoCapture.cs |
| 1547 | Sussudio/Services/Capture/CaptureService.HealthSnapshots.cs |
| 1506 | Sussudio/Services/Preview/D3D11PreviewRenderer.Resources.cs |
| 1472 | Sussudio/Controllers/Flashback/FlashbackUiControllers.cs |
| 1455 | tools/Common/DiagnosticSessionResultBuilder.cs |
| 1404 | Sussudio/Services/Flashback/FlashbackBufferManager.cs |
| 1363 | Sussudio/Services/Automation/AutomationDiagnosticsHub.Evaluation.cs |
| 1344 | Sussudio/Services/Flashback/FlashbackPlaybackController.cs |
| 1313 | Sussudio/Services/Preview/D3D11PreviewRenderer.cs |
| 1289 | Sussudio/Services/Recording/LibAvEncoder.cs |
| 1285 | Sussudio/Services/Flashback/FlashbackExporter.Execution.cs |
| 1252 | Sussudio/Services/Capture/CaptureService.FlashbackExportCore.cs |
| 1236 | Sussudio/Services/Capture/MjpegPreviewJitterBuffer.cs |
| 1234 | Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.CaptureFormat.cs |
| 1225 | Sussudio/ViewModels/MainViewModel.cs |
| 1223 | Sussudio/MainWindow.xaml.cs |
| 1211 | Sussudio/MainWindow.Composition.cs |
| 1197 | tools/Common/PresentMon/PresentMonProbe.cs |
| 1193 | Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs |
| 1186 | Sussudio/Services/Gpu/ParallelMjpegDecodePipeline.cs |
| 1174 | Sussudio/Services/Flashback/FlashbackPlaybackController.PlaybackFrames.cs |
| 1166 | Sussudio/ViewModels/ViewModelSelectionPolicies.cs |
| 1144 | Sussudio/Services/Flashback/FlashbackExporter.SegmentPacketWriting.cs |
| 1092 | Sussudio/Services/Preview/D3D11PreviewRenderer.Metrics.cs |
| 1086 | Sussudio/Services/Capture/CaptureService.PreviewLifecycle.cs |
| 1065 | Sussudio/Services/Recording/LibAvRecordingSink.cs |
| 1055 | Sussudio/ViewModels/MainViewModel.AudioState.cs |
| 1055 | Sussudio/Services/Flashback/FlashbackEncoderSink.cs |
| 1054 | Sussudio/ViewModels/StatsPresentationBuilder.cs |
| 1054 | tools/ssctl/CommandHandlers.cs |

## Sample production files under 60 lines

| Lines | Path |
| ---: | --- |

## Notes

Use this as the before/after reference for the active defragmentation goal. A lower file count is not sufficient by itself; each slice should also improve behavioral locality, ownership, or deterministic testability.

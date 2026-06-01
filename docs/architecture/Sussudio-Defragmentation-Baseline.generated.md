# Sussudio Defragmentation Baseline - Generated

Generated UTC: 2026-06-01T13:57:49Z
Root: C:\Users\crest\source\repos\Sussudio-cleanup-architecture

## Summary

| Metric | Value |
| --- | ---: |
| Production .cs files | 152 |
| Test .cs files | 22 |
| Core app .cs files (Sussudio/) | 115 |
| Core app nonblank LoC (Sussudio/) | 89534 |
| Sussudio.Tests .cs files | 20 |
| Sussudio.Tests nonblank LoC | 55962 |
| Production .cs files under 60 lines | 0 (0.0%) |
| Production .cs files under 80 lines | 1 (0.7%) |

## Largest partial-type clusters

| Type | Files | Total lines | Sample paths |
| --- | ---: | ---: | --- |
| CaptureService | 8 | 9865 | Sussudio/Services/Capture/CaptureService.cs, Sussudio/Services/Capture/CaptureService.Flashback.cs, Sussudio/Services/Capture/CaptureService.FlashbackExportCore.cs, Sussudio/Services/Capture/CaptureService.HealthSnapshots.cs, Sussudio/Services/Capture/CaptureService.PreviewLifecycle.cs, Sussudio/Services/Capture/CaptureService.RecordingIntegrity.cs, Sussudio/Services/Capture/CaptureService.RecordingLifecycle.cs, Sussudio/Services/Capture/CaptureService.RuntimeSnapshots.cs |
| AutomationDiagnosticsHub | 8 | 9920 | Sussudio/Services/Automation/AutomationDiagnosticsHub.cs, Sussudio/Services/Automation/AutomationDiagnosticsHub.Evaluation.cs, Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.CaptureFormat.cs, Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs, Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flashback.cs, Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Media.cs, Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Preview.cs, Sussudio/Services/Automation/AutomationDiagnosticsHub.Snapshots.cs |
| MainViewModel | 6 | 4479 | Sussudio/Controllers/ViewModel/MainViewModelControllerGraph.cs, Sussudio/ViewModels/MainViewModel.AudioState.cs, Sussudio/ViewModels/MainViewModel.CaptureSelection.cs, Sussudio/ViewModels/MainViewModel.cs, Sussudio/ViewModels/MainViewModel.FlashbackState.cs, Sussudio/ViewModels/MainViewModel.SettingsPersistence.cs |
| FlashbackPlaybackController | 5 | 4872 | Sussudio/Services/Flashback/FlashbackPlaybackController.AudioRouting.cs, Sussudio/Services/Flashback/FlashbackPlaybackController.cs, Sussudio/Services/Flashback/FlashbackPlaybackController.PlaybackFrames.cs, Sussudio/Services/Flashback/FlashbackPlaybackController.Positioning.cs, Sussudio/Services/Flashback/FlashbackPlaybackController.ThreadCommands.cs |
| D3D11PreviewRenderer | 4 | 4871 | Sussudio/Services/Preview/D3D11PreviewRenderer.cs, Sussudio/Services/Preview/D3D11PreviewRenderer.Metrics.cs, Sussudio/Services/Preview/D3D11PreviewRenderer.RenderPasses.cs, Sussudio/Services/Preview/D3D11PreviewRenderer.Resources.cs |
| FlashbackDecoder | 3 | 1953 | Sussudio/Services/Flashback/FlashbackDecoder.cs, Sussudio/Services/Flashback/FlashbackDecoder.Playback.cs, Sussudio/Services/Flashback/FlashbackDecoder.VideoSetup.cs |
| FlashbackEncoderSink | 3 | 2639 | Sussudio/Services/Flashback/FlashbackEncoderSink.cs, Sussudio/Services/Flashback/FlashbackEncoderSink.EncodingLoop.cs, Sussudio/Services/Flashback/FlashbackEncoderSink.Queueing.cs |
| FlashbackExporter | 3 | 2997 | Sussudio/Services/Flashback/FlashbackExporter.Execution.cs, Sussudio/Services/Flashback/FlashbackExporter.Lifecycle.cs, Sussudio/Services/Flashback/FlashbackExporter.SegmentPacketWriting.cs |
| LibAvEncoder | 3 | 2972 | Sussudio/Services/Recording/LibAvEncoder.Audio.cs, Sussudio/Services/Recording/LibAvEncoder.cs, Sussudio/Services/Recording/LibAvEncoder.VideoFrames.cs |
| NativeXuAtCommandProvider | 3 | 2539 | Sussudio/Services/Telemetry/NativeXuAtCommandProvider.cs, Sussudio/Services/Telemetry/NativeXuAtCommandProvider.DeviceCommands.cs, Sussudio/Services/Telemetry/NativeXuAtCommandProvider.SnapshotAssembly.cs |
| LibAvRecordingSink | 2 | 1886 | Sussudio/Services/Recording/LibAvRecordingSink.cs, Sussudio/Services/Recording/LibAvRecordingSink.Queueing.cs |
| MfSourceReaderVideoCapture | 2 | 2033 | Sussudio/Services/Capture/MfSourceReaderVideoCapture.cs, Sussudio/Services/Capture/MfSourceReaderVideoCapture.FrameDelivery.cs |
| LoggingJsonContext | 1 | 306 | Sussudio/Logger.cs |
| SettingsJsonContext | 1 | 690 | Sussudio/Services/Runtime/RuntimeHelpers.cs |
| App | 1 | 260 | Sussudio/App.xaml.cs |
| DeviceFormatCacheJsonContext | 1 | 697 | Sussudio/Services/Capture/DeviceService.cs |
| StatsWindow | 1 | 308 | Sussudio/StatsWindow.xaml.cs |
| MainWindow | 1 | 2421 | Sussudio/MainWindow.xaml.cs |

## Largest production files

| Lines | Path |
| ---: | --- |
| 2421 | Sussudio/MainWindow.xaml.cs |
| 2001 | Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs |
| 1721 | Sussudio/Services/Capture/CaptureService.RuntimeSnapshots.cs |
| 1632 | Sussudio/Controllers/Stats/StatsOverlayCompositionController.cs |
| 1619 | Sussudio/Services/Automation/AutomationCommandDispatcher.cs |
| 1562 | Sussudio/Services/Capture/UnifiedVideoCapture.cs |
| 1552 | Sussudio/ViewModels/ViewModelSelectionPolicies.cs |
| 1547 | Sussudio/Services/Capture/CaptureService.HealthSnapshots.cs |
| 1506 | Sussudio/Services/Preview/D3D11PreviewRenderer.Resources.cs |
| 1505 | Sussudio/Services/Automation/AutomationDiagnosticsHub.Snapshots.cs |
| 1484 | Sussudio/Services/Capture/MfSourceReaderVideoCapture.cs |
| 1472 | Sussudio/Controllers/Flashback/FlashbackUiControllers.cs |
| 1467 | Sussudio/Services/Gpu/ParallelMjpegDecodePipeline.cs |
| 1455 | tools/Common/DiagnosticSessionResultBuilder.cs |
| 1404 | Sussudio/Services/Flashback/FlashbackBufferManager.cs |
| 1374 | Sussudio/Services/Capture/CaptureService.Flashback.cs |
| 1363 | Sussudio/Services/Automation/AutomationDiagnosticsHub.Evaluation.cs |
| 1344 | Sussudio/Services/Flashback/FlashbackPlaybackController.cs |
| 1313 | Sussudio/Services/Preview/D3D11PreviewRenderer.cs |
| 1290 | Sussudio/ViewModels/StatsPresentationBuilder.cs |
| 1289 | Sussudio/Services/Recording/LibAvEncoder.cs |
| 1285 | Sussudio/Services/Flashback/FlashbackExporter.Execution.cs |
| 1249 | Sussudio/Services/Capture/CaptureService.FlashbackExportCore.cs |
| 1236 | Sussudio/Services/Capture/MjpegPreviewJitterBuffer.cs |
| 1234 | Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.CaptureFormat.cs |
| 1225 | Sussudio/ViewModels/MainViewModel.cs |
| 1204 | Sussudio/Services/Capture/CaptureService.cs |
| 1197 | tools/Common/PresentMon/PresentMonProbe.cs |
| 1174 | Sussudio/Services/Flashback/FlashbackPlaybackController.PlaybackFrames.cs |
| 1143 | Sussudio/Controllers/Capture/CaptureBindingControllers.cs |

## Sample production files under 60 lines

| Lines | Path |
| ---: | --- |

## Notes

Use this as the before/after reference for the active defragmentation goal. A lower file count is not sufficient by itself; each slice should also improve behavioral locality, ownership, or deterministic testability.

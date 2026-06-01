# Sussudio Defragmentation Baseline - Generated

Generated UTC: 2026-06-01T19:38:58Z
Root: C:\Users\crest\source\repos\Sussudio-cleanup-architecture

## Summary

| Metric | Value |
| --- | ---: |
| Production .cs files | 128 |
| Test .cs files | 14 |
| Core app .cs files (Sussudio/) | 93 |
| Core app nonblank LoC (Sussudio/) | 88386 |
| Sussudio.Tests .cs files | 12 |
| Sussudio.Tests nonblank LoC | 55791 |
| Production .cs files under 60 lines | 0 (0.0%) |
| Production .cs files under 80 lines | 1 (0.8%) |

## Largest partial-type clusters

| Type | Files | Total lines | Sample paths |
| --- | ---: | ---: | --- |
| CaptureService | 8 | 9865 | Sussudio/Services/Capture/CaptureService.cs, Sussudio/Services/Capture/CaptureService.Flashback.cs, Sussudio/Services/Capture/CaptureService.FlashbackExportCore.cs, Sussudio/Services/Capture/CaptureService.HealthSnapshots.cs, Sussudio/Services/Capture/CaptureService.PreviewLifecycle.cs, Sussudio/Services/Capture/CaptureService.RecordingIntegrity.cs, Sussudio/Services/Capture/CaptureService.RecordingLifecycle.cs, Sussudio/Services/Capture/CaptureService.RuntimeSnapshots.cs |
| MainViewModel | 4 | 4452 | Sussudio/Controllers/ViewModel/MainViewModelControllerGraph.cs, Sussudio/ViewModels/MainViewModel.AudioState.cs, Sussudio/ViewModels/MainViewModel.cs, Sussudio/ViewModels/MainViewModel.FlashbackState.cs |
| AutomationDiagnosticsHub | 4 | 9893 | Sussudio/Services/Automation/AutomationDiagnosticsHub.cs, Sussudio/Services/Automation/AutomationDiagnosticsHub.Evaluation.cs, Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs, Sussudio/Services/Automation/AutomationDiagnosticsHub.Snapshots.cs |
| D3D11PreviewRenderer | 4 | 4871 | Sussudio/Services/Preview/D3D11PreviewRenderer.cs, Sussudio/Services/Preview/D3D11PreviewRenderer.Metrics.cs, Sussudio/Services/Preview/D3D11PreviewRenderer.RenderPasses.cs, Sussudio/Services/Preview/D3D11PreviewRenderer.Resources.cs |
| FlashbackPlaybackController | 3 | 4855 | Sussudio/Services/Flashback/FlashbackPlaybackController.cs, Sussudio/Services/Flashback/FlashbackPlaybackController.PlaybackFrames.cs, Sussudio/Services/Flashback/FlashbackPlaybackController.ThreadCommands.cs |
| LibAvEncoder | 3 | 2972 | Sussudio/Services/Recording/LibAvEncoder.Audio.cs, Sussudio/Services/Recording/LibAvEncoder.cs, Sussudio/Services/Recording/LibAvEncoder.VideoFrames.cs |
| SettingsJsonContext | 1 | 690 | Sussudio/Services/Runtime/RuntimeHelpers.cs |
| DeviceFormatCacheJsonContext | 1 | 697 | Sussudio/Services/Capture/DeviceService.cs |
| MainWindow | 1 | 2421 | Sussudio/MainWindow.xaml.cs |
| LoggingJsonContext | 1 | 545 | Sussudio/AppRuntime.cs |
| App | 1 | 260 | Sussudio/App.xaml.cs |
| StatsWindow | 1 | 308 | Sussudio/StatsWindow.xaml.cs |

## Largest production files

| Lines | Path |
| ---: | --- |
| 6046 | Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs |
| 2976 | Sussudio/Services/Flashback/FlashbackExporter.cs |
| 2716 | Sussudio/Services/Flashback/FlashbackPlaybackController.cs |
| 2617 | Sussudio/Services/Flashback/FlashbackEncoderSink.cs |
| 2517 | Sussudio/Services/Telemetry/NativeXuAtCommandProvider.cs |
| 2421 | Sussudio/MainWindow.xaml.cs |
| 2024 | Sussudio/Services/Capture/MfSourceReaderVideoCapture.cs |
| 2012 | Sussudio/ViewModels/MainViewModel.cs |
| 1933 | Sussudio/Services/Flashback/FlashbackDecoder.cs |
| 1873 | Sussudio/Services/Recording/LibAvRecordingSink.cs |
| 1721 | Sussudio/Services/Capture/CaptureService.RuntimeSnapshots.cs |
| 1632 | Sussudio/Controllers/Stats/StatsOverlayCompositionController.cs |
| 1619 | Sussudio/Services/Automation/AutomationCommandDispatcher.cs |
| 1562 | Sussudio/Services/Capture/UnifiedVideoCapture.cs |
| 1552 | Sussudio/ViewModels/ViewModelSelectionPolicies.cs |
| 1547 | Sussudio/Services/Capture/CaptureService.HealthSnapshots.cs |
| 1506 | Sussudio/Services/Preview/D3D11PreviewRenderer.Resources.cs |
| 1505 | Sussudio/Services/Automation/AutomationDiagnosticsHub.Snapshots.cs |
| 1472 | Sussudio/Controllers/Flashback/FlashbackUiControllers.cs |
| 1467 | Sussudio/Services/Gpu/ParallelMjpegDecodePipeline.cs |
| 1455 | tools/Common/DiagnosticSessionResultBuilder.cs |
| 1404 | Sussudio/Services/Flashback/FlashbackBufferManager.cs |
| 1374 | Sussudio/Services/Capture/CaptureService.Flashback.cs |
| 1363 | Sussudio/Services/Automation/AutomationDiagnosticsHub.Evaluation.cs |
| 1313 | Sussudio/Services/Preview/D3D11PreviewRenderer.cs |
| 1290 | Sussudio/ViewModels/StatsPresentationBuilder.cs |
| 1289 | Sussudio/Services/Recording/LibAvEncoder.cs |
| 1249 | Sussudio/Services/Capture/CaptureService.FlashbackExportCore.cs |
| 1236 | Sussudio/Services/Capture/MjpegPreviewJitterBuffer.cs |
| 1204 | Sussudio/Services/Capture/CaptureService.cs |

## Sample production files under 60 lines

| Lines | Path |
| ---: | --- |

## Notes

Use this as the before/after reference for the active defragmentation goal. A lower file count is not sufficient by itself; each slice should also improve behavioral locality, ownership, or deterministic testability.

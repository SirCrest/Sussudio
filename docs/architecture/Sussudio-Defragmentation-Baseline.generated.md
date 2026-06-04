# Sussudio Defragmentation Baseline - Generated

Generated UTC: 2026-06-04T04:37:03Z
Root: C:\Users\crest\source\repos\Sussudio

## Summary

| Metric | Value |
| --- | ---: |
| Production .cs files | 122 |
| Test .cs files | 13 |
| Core app .cs files (Sussudio/) | 88 |
| Core app nonblank LoC (Sussudio/) | 90796 |
| Sussudio.Tests .cs files | 12 |
| Sussudio.Tests nonblank LoC | 57818 |
| Production .cs files under 60 lines | 0 (0.0%) |
| Production .cs files under 80 lines | 1 (0.8%) |

## Largest partial-type clusters

| Type | Files | Total lines | Sample paths |
| --- | ---: | ---: | --- |
| CaptureService | 6 | 10188 | Sussudio/Services/Capture/CaptureService.cs, Sussudio/Services/Capture/CaptureService.Flashback.cs, Sussudio/Services/Capture/CaptureService.HealthSnapshots.cs, Sussudio/Services/Capture/CaptureService.PreviewLifecycle.cs, Sussudio/Services/Capture/CaptureService.RecordingLifecycle.cs, Sussudio/Services/Capture/CaptureService.RuntimeSnapshots.cs |
| AutomationDiagnosticsHub | 4 | 9420 | Sussudio/Services/Automation/AutomationDiagnosticsHub.cs, Sussudio/Services/Automation/AutomationDiagnosticsHub.Evaluation.cs, Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs, Sussudio/Services/Automation/AutomationDiagnosticsHub.Snapshots.cs |
| D3D11PreviewRenderer | 3 | 4862 | Sussudio/Services/Preview/D3D11PreviewRenderer.cs, Sussudio/Services/Preview/D3D11PreviewRenderer.RenderPasses.cs, Sussudio/Services/Preview/D3D11PreviewRenderer.Resources.cs |
| FlashbackPlaybackController | 3 | 4855 | Sussudio/Services/Flashback/FlashbackPlaybackController.cs, Sussudio/Services/Flashback/FlashbackPlaybackController.PlaybackFrames.cs, Sussudio/Services/Flashback/FlashbackPlaybackController.ThreadCommands.cs |
| LibAvEncoder | 3 | 2972 | Sussudio/Services/Recording/LibAvEncoder.Audio.cs, Sussudio/Services/Recording/LibAvEncoder.cs, Sussudio/Services/Recording/LibAvEncoder.VideoFrames.cs |
| MainViewModel | 3 | 5042 | Sussudio/ViewModels/MainViewModel.AudioState.cs, Sussudio/ViewModels/MainViewModel.cs, Sussudio/ViewModels/MainViewModel.FlashbackState.cs |
| LoggingJsonContext | 1 | 545 | Sussudio/AppRuntime.cs |
| SettingsJsonContext | 1 | 703 | Sussudio/Services/Runtime/RuntimeHelpers.cs |
| App | 1 | 263 | Sussudio/App.xaml.cs |
| DeviceFormatCacheJsonContext | 1 | 697 | Sussudio/Services/Capture/DeviceService.cs |
| StatsWindow | 1 | 308 | Sussudio/StatsWindow.xaml.cs |
| MainWindow | 1 | 2421 | Sussudio/MainWindow.xaml.cs |

## Largest production files

| Lines | Path |
| ---: | --- |
| 5160 | Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs |
| 3410 | Sussudio/Services/Flashback/FlashbackExporter.cs |
| 3084 | Sussudio/ViewModels/MainViewModel.cs |
| 2716 | Sussudio/Services/Flashback/FlashbackPlaybackController.cs |
| 2644 | Sussudio/Services/Capture/CaptureService.Flashback.cs |
| 2617 | Sussudio/Services/Flashback/FlashbackEncoderSink.cs |
| 2527 | Sussudio/Services/Telemetry/NativeXuAtCommandProvider.cs |
| 2421 | Sussudio/MainWindow.xaml.cs |
| 2396 | Sussudio/Services/Preview/D3D11PreviewRenderer.cs |
| 2024 | Sussudio/Services/Capture/MfSourceReaderVideoCapture.cs |
| 1937 | Sussudio/Services/Flashback/FlashbackDecoder.cs |
| 1918 | Sussudio/Services/Automation/AutomationDiagnosticsHub.Snapshots.cs |
| 1878 | Sussudio/Services/Recording/LibAvRecordingSink.cs |
| 1848 | Sussudio/Services/Capture/CaptureService.RuntimeSnapshots.cs |
| 1737 | Sussudio/Services/Capture/CaptureService.RecordingLifecycle.cs |
| 1717 | Sussudio/Controllers/ViewModel/MainViewModelDeviceControllers.cs |
| 1713 | Sussudio/Services/Automation/AutomationCommandDispatcher.cs |
| 1632 | Sussudio/Controllers/Stats/StatsOverlayCompositionController.cs |
| 1596 | Sussudio/Services/Capture/UnifiedVideoCapture.cs |
| 1596 | Sussudio/ViewModels/ViewModelSelectionPolicies.cs |
| 1558 | Sussudio/Services/Capture/CaptureService.HealthSnapshots.cs |
| 1524 | tools/Common/DiagnosticSessionResultBuilder.cs |
| 1506 | Sussudio/Services/Preview/D3D11PreviewRenderer.Resources.cs |
| 1473 | Sussudio/Services/Flashback/FlashbackBufferManager.cs |
| 1472 | Sussudio/Controllers/Flashback/FlashbackUiControllers.cs |
| 1467 | Sussudio/Services/Gpu/ParallelMjpegDecodePipeline.cs |
| 1363 | Sussudio/Services/Automation/AutomationDiagnosticsHub.Evaluation.cs |
| 1306 | Sussudio/Services/Capture/CaptureService.cs |
| 1290 | Sussudio/ViewModels/StatsPresentationBuilder.cs |
| 1289 | Sussudio/Services/Recording/LibAvEncoder.cs |

## Sample production files under 60 lines

| Lines | Path |
| ---: | --- |

## Notes

Use this as the before/after reference for the active defragmentation goal. A lower file count is not sufficient by itself; each slice should also improve behavioral locality, ownership, or deterministic testability.

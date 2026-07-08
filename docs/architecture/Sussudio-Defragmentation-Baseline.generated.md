# Sussudio Defragmentation Baseline - Generated

Generated UTC: 2026-07-08T22:57:26Z
Root: C:\Users\crest\source\repos\Sussudio

## Summary

| Metric | Value |
| --- | ---: |
| Production .cs files | 122 |
| Test .cs files | 17 |
| Core app .cs files (Sussudio/) | 88 |
| Core app nonblank LoC (Sussudio/) | 92484 |
| Sussudio.Tests .cs files | 16 |
| Sussudio.Tests nonblank LoC | 58874 |
| Production .cs files under 60 lines | 0 (0.0%) |
| Production .cs files under 80 lines | 1 (0.8%) |

## Largest partial-type clusters

| Type | Files | Total lines | Sample paths |
| --- | ---: | ---: | --- |
| CaptureService | 6 | 10325 | Sussudio/Services/Capture/CaptureService.cs, Sussudio/Services/Capture/CaptureService.Flashback.cs, Sussudio/Services/Capture/CaptureService.HealthSnapshots.cs, Sussudio/Services/Capture/CaptureService.PreviewLifecycle.cs, Sussudio/Services/Capture/CaptureService.RecordingLifecycle.cs, Sussudio/Services/Capture/CaptureService.RuntimeSnapshots.cs |
| AutomationDiagnosticsHub | 4 | 9435 | Sussudio/Services/Automation/AutomationDiagnosticsHub.cs, Sussudio/Services/Automation/AutomationDiagnosticsHub.Evaluation.cs, Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs, Sussudio/Services/Automation/AutomationDiagnosticsHub.Snapshots.cs |
| D3D11PreviewRenderer | 3 | 5027 | Sussudio/Services/Preview/D3D11PreviewRenderer.cs, Sussudio/Services/Preview/D3D11PreviewRenderer.RenderPasses.cs, Sussudio/Services/Preview/D3D11PreviewRenderer.Resources.cs |
| FlashbackPlaybackController | 3 | 4979 | Sussudio/Services/Flashback/FlashbackPlaybackController.cs, Sussudio/Services/Flashback/FlashbackPlaybackController.PlaybackFrames.cs, Sussudio/Services/Flashback/FlashbackPlaybackController.ThreadCommands.cs |
| LibAvEncoder | 3 | 2972 | Sussudio/Services/Recording/LibAvEncoder.Audio.cs, Sussudio/Services/Recording/LibAvEncoder.cs, Sussudio/Services/Recording/LibAvEncoder.VideoFrames.cs |
| MainViewModel | 3 | 5071 | Sussudio/ViewModels/MainViewModel.AudioState.cs, Sussudio/ViewModels/MainViewModel.cs, Sussudio/ViewModels/MainViewModel.FlashbackState.cs |
| LoggingJsonContext | 1 | 545 | Sussudio/AppRuntime.cs |
| SettingsJsonContext | 1 | 704 | Sussudio/Services/Runtime/RuntimeHelpers.cs |
| App | 1 | 263 | Sussudio/App.xaml.cs |
| DeviceFormatCacheJsonContext | 1 | 697 | Sussudio/Services/Capture/DeviceService.cs |
| StatsWindow | 1 | 308 | Sussudio/StatsWindow.xaml.cs |
| MainWindow | 1 | 2430 | Sussudio/MainWindow.xaml.cs |

## Largest production files

| Lines | Path |
| ---: | --- |
| 5175 | Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs |
| 3539 | Sussudio/Services/Flashback/FlashbackExporter.cs |
| 3099 | Sussudio/ViewModels/MainViewModel.cs |
| 2840 | Sussudio/Services/Flashback/FlashbackPlaybackController.cs |
| 2718 | Sussudio/Services/Capture/CaptureService.Flashback.cs |
| 2712 | Sussudio/Services/Flashback/FlashbackEncoderSink.cs |
| 2527 | Sussudio/Services/Telemetry/NativeXuAtCommandProvider.cs |
| 2490 | Sussudio/Services/Preview/D3D11PreviewRenderer.cs |
| 2430 | Sussudio/MainWindow.xaml.cs |
| 2090 | Sussudio/Services/Capture/MfSourceReaderVideoCapture.cs |
| 1937 | Sussudio/Services/Flashback/FlashbackDecoder.cs |
| 1918 | Sussudio/Services/Automation/AutomationDiagnosticsHub.Snapshots.cs |
| 1878 | Sussudio/Services/Recording/LibAvRecordingSink.cs |
| 1848 | Sussudio/Services/Capture/CaptureService.RuntimeSnapshots.cs |
| 1757 | Sussudio/Services/Flashback/FlashbackBufferManager.cs |
| 1753 | Sussudio/Services/Capture/UnifiedVideoCapture.cs |
| 1745 | tools/Common/DiagnosticSessionResultBuilder.cs |
| 1739 | Sussudio/Controllers/ViewModel/MainViewModelDeviceControllers.cs |
| 1737 | Sussudio/Services/Capture/CaptureService.RecordingLifecycle.cs |
| 1713 | Sussudio/Services/Automation/AutomationCommandDispatcher.cs |
| 1698 | Sussudio/Services/Gpu/ParallelMjpegDecodePipeline.cs |
| 1632 | Sussudio/Controllers/Stats/StatsOverlayCompositionController.cs |
| 1596 | Sussudio/ViewModels/ViewModelSelectionPolicies.cs |
| 1561 | Sussudio/Services/Capture/CaptureService.HealthSnapshots.cs |
| 1561 | Sussudio/Services/Preview/D3D11PreviewRenderer.Resources.cs |
| 1475 | Sussudio/Controllers/Flashback/FlashbackUiControllers.cs |
| 1366 | Sussudio/Controllers/ViewModel/MainViewModelLifecycleController.cs |
| 1363 | Sussudio/Services/Automation/AutomationDiagnosticsHub.Evaluation.cs |
| 1325 | Sussudio/Services/Capture/CaptureService.cs |
| 1314 | Sussudio/Services/Capture/MjpegPreviewJitterBuffer.cs |

## Sample production files under 60 lines

| Lines | Path |
| ---: | --- |

## Notes

Use this as the before/after reference for the active defragmentation goal. A lower file count is not sufficient by itself; each slice should also improve behavioral locality, ownership, or deterministic testability.

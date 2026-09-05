# Sussudio Defragmentation Baseline - Generated

Generated UTC: 2026-09-05T15:21:19Z
Root: C:\Users\crest\source\repos\Sussudio

## Summary

| Metric | Value |
| --- | ---: |
| Production .cs files | 133 |
| Test .cs files | 28 |
| Core app .cs files (Sussudio/) | 99 |
| Core app nonblank LoC (Sussudio/) | 96364 |
| Sussudio.Tests .cs files | 27 |
| Sussudio.Tests nonblank LoC | 61413 |
| Production .cs files under 60 lines | 1 (0.8%) |
| Production .cs files under 80 lines | 4 (3.0%) |

## Largest partial-type clusters

| Type | Files | Total lines | Sample paths |
| --- | ---: | ---: | --- |
| CaptureService | 6 | 11259 | Sussudio/Services/Capture/CaptureService.cs, Sussudio/Services/Capture/CaptureService.Flashback.cs, Sussudio/Services/Capture/CaptureService.HealthSnapshots.cs, Sussudio/Services/Capture/CaptureService.PreviewLifecycle.cs, Sussudio/Services/Capture/CaptureService.RecordingLifecycle.cs, Sussudio/Services/Capture/CaptureService.RuntimeSnapshots.cs |
| AutomationDiagnosticsHub | 4 | 9305 | Sussudio/Services/Automation/AutomationDiagnosticsHub.cs, Sussudio/Services/Automation/AutomationDiagnosticsHub.Evaluation.cs, Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs, Sussudio/Services/Automation/AutomationDiagnosticsHub.Snapshots.cs |
| D3D11PreviewRenderer | 3 | 5097 | Sussudio/Services/Preview/D3D11PreviewRenderer.cs, Sussudio/Services/Preview/D3D11PreviewRenderer.RenderPasses.cs, Sussudio/Services/Preview/D3D11PreviewRenderer.Resources.cs |
| FlashbackPlaybackController | 3 | 4646 | Sussudio/Services/Flashback/FlashbackPlaybackController.cs, Sussudio/Services/Flashback/FlashbackPlaybackController.PlaybackFrames.cs, Sussudio/Services/Flashback/FlashbackPlaybackController.ThreadCommands.cs |
| LibAvEncoder | 3 | 3049 | Sussudio/Services/Recording/LibAvEncoder.Audio.cs, Sussudio/Services/Recording/LibAvEncoder.cs, Sussudio/Services/Recording/LibAvEncoder.VideoFrames.cs |
| MainViewModel | 3 | 5258 | Sussudio/ViewModels/MainViewModel.AudioState.cs, Sussudio/ViewModels/MainViewModel.cs, Sussudio/ViewModels/MainViewModel.FlashbackState.cs |
| App | 1 | 270 | Sussudio/App.xaml.cs |
| DeviceFormatCacheJsonContext | 1 | 716 | Sussudio/Services/Capture/DeviceService.cs |
| LoggingJsonContext | 1 | 573 | Sussudio/AppRuntime.cs |
| MainWindow | 1 | 2527 | Sussudio/MainWindow.xaml.cs |
| SettingsJsonContext | 1 | 765 | Sussudio/Services/Runtime/RuntimeHelpers.cs |
| StatsWindow | 1 | 308 | Sussudio/StatsWindow.xaml.cs |

## Largest production files

| Lines | Path |
| ---: | --- |
| 5031 | Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs |
| 3112 | Sussudio/ViewModels/MainViewModel.cs |
| 2947 | Sussudio/Services/Flashback/FlashbackEncoderSink.cs |
| 2900 | Sussudio/Services/Flashback/FlashbackExporter.cs |
| 2685 | Sussudio/Services/Capture/CaptureService.Flashback.cs |
| 2530 | Sussudio/Services/Capture/CaptureService.RecordingLifecycle.cs |
| 2527 | Sussudio/MainWindow.xaml.cs |
| 2519 | Sussudio/Services/Preview/D3D11PreviewRenderer.cs |
| 2467 | Sussudio/Services/Flashback/FlashbackPlaybackController.cs |
| 2380 | Sussudio/Services/Telemetry/NativeXuAtCommandProvider.cs |
| 2103 | Sussudio/Services/Capture/MfSourceReaderVideoCapture.cs |
| 2094 | Sussudio/Services/Recording/LibAvRecordingSink.cs |
| 1948 | Sussudio/Services/Flashback/FlashbackDecoder.cs |
| 1932 | Sussudio/Services/Automation/AutomationDiagnosticsHub.Snapshots.cs |
| 1882 | Sussudio/Services/Capture/CaptureService.RuntimeSnapshots.cs |
| 1808 | Sussudio/Services/Flashback/FlashbackBufferManager.cs |
| 1770 | Sussudio/Services/Capture/UnifiedVideoCapture.cs |
| 1745 | tools/Common/DiagnosticSessionResultBuilder.cs |
| 1739 | Sussudio/Controllers/ViewModel/MainViewModelDeviceControllers.cs |
| 1728 | Sussudio/Services/Automation/AutomationCommandDispatcher.cs |
| 1667 | Sussudio/Services/Gpu/ParallelMjpegDecodePipeline.cs |
| 1602 | Sussudio/Services/Preview/D3D11PreviewRenderer.Resources.cs |
| 1596 | Sussudio/ViewModels/ViewModelSelectionPolicies.cs |
| 1561 | Sussudio/Services/Capture/CaptureService.HealthSnapshots.cs |
| 1551 | Sussudio/Controllers/Stats/StatsOverlayCompositionController.cs |
| 1508 | Sussudio/Controllers/Flashback/FlashbackUiControllers.cs |
| 1437 | Sussudio/Services/Audio/WasapiAudioPlayback.cs |
| 1400 | Sussudio/Services/Recording/LibAvEncoder.cs |
| 1391 | Sussudio/Controllers/ViewModel/MainViewModelLifecycleController.cs |
| 1363 | Sussudio/Services/Automation/AutomationDiagnosticsHub.Evaluation.cs |

## Sample production files under 60 lines

| Lines | Path |
| ---: | --- |
| 21 | Sussudio/Models/DeviceAudioModeParser.cs |

## Notes

Use this as the before/after reference for the active defragmentation goal. A lower file count is not sufficient by itself; each slice should also improve behavioral locality, ownership, or deterministic testability.

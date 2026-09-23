# Sussudio Defragmentation Baseline - Generated

Generated UTC: 2026-09-23T07:38:54Z
Root: C:\Users\crest\source\repos\Sussudio

## Summary

| Metric | Value |
| --- | ---: |
| Production .cs files | 149 |
| Test .cs files | 106 |
| Core app .cs files (Sussudio/) | 115 |
| Core app nonblank LoC (Sussudio/) | 93460 |
| Sussudio.Tests .cs files | 105 |
| Sussudio.Tests nonblank LoC | 82197 |
| Production .cs files under 60 lines | 3 (2.0%) |
| Production .cs files under 80 lines | 7 (4.7%) |

## Largest partial-type clusters

| Type | Files | Total lines | Sample paths |
| --- | ---: | ---: | --- |
| CaptureService | 6 | 10925 | Sussudio/Services/Capture/CaptureService.cs, Sussudio/Services/Capture/CaptureService.Flashback.cs, Sussudio/Services/Capture/CaptureService.HealthSnapshots.cs, Sussudio/Services/Capture/CaptureService.PreviewLifecycle.cs, Sussudio/Services/Capture/CaptureService.RecordingLifecycle.cs, Sussudio/Services/Capture/CaptureService.RuntimeSnapshots.cs |
| AutomationDiagnosticsHub | 5 | 5164 | Sussudio/Services/Automation/AutomationDiagnosticsHub.cs, Sussudio/Services/Automation/AutomationDiagnosticsHub.Evaluation.cs, Sussudio/Services/Automation/AutomationDiagnosticsHub.FlashbackEvaluation.cs, Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs, Sussudio/Services/Automation/AutomationDiagnosticsHub.Snapshots.cs |
| D3D11PreviewRenderer | 3 | 5280 | Sussudio/Services/Preview/D3D11PreviewRenderer.cs, Sussudio/Services/Preview/D3D11PreviewRenderer.RenderPasses.cs, Sussudio/Services/Preview/D3D11PreviewRenderer.Resources.cs |
| FlashbackPlaybackController | 3 | 4692 | Sussudio/Services/Flashback/FlashbackPlaybackController.cs, Sussudio/Services/Flashback/FlashbackPlaybackController.PlaybackFrames.cs, Sussudio/Services/Flashback/FlashbackPlaybackController.ThreadCommands.cs |
| LibAvEncoder | 3 | 2956 | Sussudio/Services/Recording/LibAvEncoder.Audio.cs, Sussudio/Services/Recording/LibAvEncoder.cs, Sussudio/Services/Recording/LibAvEncoder.VideoFrames.cs |
| MainViewModel | 3 | 5198 | Sussudio/ViewModels/MainViewModel.AudioState.cs, Sussudio/ViewModels/MainViewModel.cs, Sussudio/ViewModels/MainViewModel.FlashbackState.cs |
| LoggingJsonContext | 1 | 686 | Sussudio/AppRuntime.cs |
| SettingsJsonContext | 1 | 123 | Sussudio/Services/Runtime/SettingsService.cs |
| App | 1 | 235 | Sussudio/App.xaml.cs |
| DeviceFormatCacheJsonContext | 1 | 766 | Sussudio/Services/Capture/DeviceService.cs |
| StatsWindow | 1 | 328 | Sussudio/StatsWindow.xaml.cs |
| MainWindow | 1 | 2439 | Sussudio/MainWindow.xaml.cs |

## Largest production files

| Lines | Path |
| ---: | --- |
| 3081 | Sussudio/ViewModels/MainViewModel.cs |
| 2974 | Sussudio/Services/Flashback/FlashbackExporter.cs |
| 2876 | Sussudio/Services/Flashback/FlashbackEncoderSink.cs |
| 2579 | Sussudio/Services/Preview/D3D11PreviewRenderer.cs |
| 2558 | Sussudio/Services/Capture/CaptureService.RecordingLifecycle.cs |
| 2482 | Sussudio/Services/Flashback/FlashbackPlaybackController.cs |
| 2439 | Sussudio/MainWindow.xaml.cs |
| 2317 | Sussudio/Services/Telemetry/NativeXuAtCommandProvider.cs |
| 2149 | Sussudio/Services/Flashback/FlashbackDecoder.cs |
| 2098 | Sussudio/Services/Capture/CaptureService.Flashback.cs |
| 2079 | Sussudio/Services/Capture/MfSourceReaderVideoCapture.cs |
| 2032 | Sussudio/Services/Capture/CaptureService.RuntimeSnapshots.cs |
| 2024 | Sussudio/Services/Recording/LibAvRecordingSink.cs |
| 1933 | Sussudio/Services/Automation/AutomationDiagnosticsHub.Snapshots.cs |
| 1813 | Sussudio/Services/Flashback/FlashbackBufferManager.cs |
| 1784 | Sussudio/Services/Capture/UnifiedVideoCapture.cs |
| 1744 | Sussudio/Services/Automation/AutomationCommandDispatcher.cs |
| 1710 | Sussudio/Controllers/ViewModel/MainViewModelDeviceControllers.cs |
| 1641 | Sussudio/Services/Capture/Mjpeg/ParallelMjpegDecodePipeline.cs |
| 1600 | Sussudio/Services/Preview/D3D11PreviewRenderer.Resources.cs |
| 1563 | Sussudio/ViewModels/ViewModelSelectionPolicies.cs |
| 1562 | Sussudio/Services/Capture/CaptureService.HealthSnapshots.cs |
| 1546 | Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs |
| 1508 | Sussudio/Controllers/Flashback/FlashbackUiControllers.cs |
| 1475 | Sussudio/Services/Audio/WasapiAudioPlayback.cs |
| 1456 | Sussudio/Controllers/ViewModel/MainViewModelLifecycleController.cs |
| 1448 | Sussudio/Services/Capture/CaptureService.cs |
| 1411 | Sussudio/Services/Recording/LibAvEncoder.cs |
| 1303 | Sussudio/Services/Flashback/FlashbackPlaybackController.PlaybackFrames.cs |
| 1299 | Sussudio/Controllers/Stats/StatsOverlayCompositionController.cs |

## Sample production files under 60 lines

| Lines | Path |
| ---: | --- |
| 21 | Sussudio/Models/Audio/DeviceAudioModeParser.cs |
| 44 | Sussudio/Program.cs |
| 49 | Sussudio/Models/Recording/RecordingSettingsSelection.cs |

## Notes

Use this as the before/after reference for the active defragmentation goal. A lower file count is not sufficient by itself; each slice should also improve behavioral locality, ownership, or deterministic testability.

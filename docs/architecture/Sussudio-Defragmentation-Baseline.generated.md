# Sussudio Defragmentation Baseline - Generated

Generated UTC: 2026-09-09T03:57:15Z
Root: C:\Users\crest\source\repos\Sussudio

## Summary

| Metric | Value |
| --- | ---: |
| Production .cs files | 147 |
| Test .cs files | 86 |
| Core app .cs files (Sussudio/) | 113 |
| Core app nonblank LoC (Sussudio/) | 96347 |
| Sussudio.Tests .cs files | 85 |
| Sussudio.Tests nonblank LoC | 73978 |
| Production .cs files under 60 lines | 3 (2.0%) |
| Production .cs files under 80 lines | 7 (4.8%) |

## Largest partial-type clusters

| Type | Files | Total lines | Sample paths |
| --- | ---: | ---: | --- |
| CaptureService | 6 | 11365 | Sussudio/Services/Capture/CaptureService.cs, Sussudio/Services/Capture/CaptureService.Flashback.cs, Sussudio/Services/Capture/CaptureService.HealthSnapshots.cs, Sussudio/Services/Capture/CaptureService.PreviewLifecycle.cs, Sussudio/Services/Capture/CaptureService.RecordingLifecycle.cs, Sussudio/Services/Capture/CaptureService.RuntimeSnapshots.cs |
| AutomationDiagnosticsHub | 4 | 7437 | Sussudio/Services/Automation/AutomationDiagnosticsHub.cs, Sussudio/Services/Automation/AutomationDiagnosticsHub.Evaluation.cs, Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs, Sussudio/Services/Automation/AutomationDiagnosticsHub.Snapshots.cs |
| D3D11PreviewRenderer | 3 | 5320 | Sussudio/Services/Preview/D3D11PreviewRenderer.cs, Sussudio/Services/Preview/D3D11PreviewRenderer.RenderPasses.cs, Sussudio/Services/Preview/D3D11PreviewRenderer.Resources.cs |
| FlashbackPlaybackController | 3 | 4725 | Sussudio/Services/Flashback/FlashbackPlaybackController.cs, Sussudio/Services/Flashback/FlashbackPlaybackController.PlaybackFrames.cs, Sussudio/Services/Flashback/FlashbackPlaybackController.ThreadCommands.cs |
| LibAvEncoder | 3 | 3055 | Sussudio/Services/Recording/LibAvEncoder.Audio.cs, Sussudio/Services/Recording/LibAvEncoder.cs, Sussudio/Services/Recording/LibAvEncoder.VideoFrames.cs |
| MainViewModel | 3 | 5238 | Sussudio/ViewModels/MainViewModel.AudioState.cs, Sussudio/ViewModels/MainViewModel.cs, Sussudio/ViewModels/MainViewModel.FlashbackState.cs |
| LoggingJsonContext | 1 | 613 | Sussudio/AppRuntime.cs |
| SettingsJsonContext | 1 | 852 | Sussudio/Services/Runtime/RuntimeHelpers.cs |
| App | 1 | 222 | Sussudio/App.xaml.cs |
| DeviceFormatCacheJsonContext | 1 | 765 | Sussudio/Services/Capture/DeviceService.cs |
| StatsWindow | 1 | 328 | Sussudio/StatsWindow.xaml.cs |
| MainWindow | 1 | 2541 | Sussudio/MainWindow.xaml.cs |

## Largest production files

| Lines | Path |
| ---: | --- |
| 3157 | Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs |
| 3121 | Sussudio/ViewModels/MainViewModel.cs |
| 2974 | Sussudio/Services/Flashback/FlashbackExporter.cs |
| 2902 | Sussudio/Services/Flashback/FlashbackEncoderSink.cs |
| 2620 | Sussudio/Services/Preview/D3D11PreviewRenderer.cs |
| 2541 | Sussudio/MainWindow.xaml.cs |
| 2533 | Sussudio/Services/Capture/CaptureService.Flashback.cs |
| 2520 | Sussudio/Services/Capture/CaptureService.RecordingLifecycle.cs |
| 2469 | Sussudio/Services/Flashback/FlashbackPlaybackController.cs |
| 2380 | Sussudio/Services/Telemetry/NativeXuAtCommandProvider.cs |
| 2149 | Sussudio/Services/Flashback/FlashbackDecoder.cs |
| 2105 | Sussudio/Services/Capture/MfSourceReaderVideoCapture.cs |
| 2066 | Sussudio/Services/Recording/LibAvRecordingSink.cs |
| 2033 | Sussudio/Services/Capture/CaptureService.RuntimeSnapshots.cs |
| 1934 | Sussudio/Services/Automation/AutomationDiagnosticsHub.Snapshots.cs |
| 1813 | Sussudio/Services/Flashback/FlashbackBufferManager.cs |
| 1787 | Sussudio/Services/Capture/UnifiedVideoCapture.cs |
| 1775 | Sussudio/Controllers/ViewModel/MainViewModelDeviceControllers.cs |
| 1733 | Sussudio/Services/Automation/AutomationCommandDispatcher.cs |
| 1634 | Sussudio/Services/Capture/Mjpeg/ParallelMjpegDecodePipeline.cs |
| 1600 | Sussudio/Services/Preview/D3D11PreviewRenderer.Resources.cs |
| 1563 | Sussudio/ViewModels/ViewModelSelectionPolicies.cs |
| 1562 | Sussudio/Services/Capture/CaptureService.HealthSnapshots.cs |
| 1508 | Sussudio/Controllers/Flashback/FlashbackUiControllers.cs |
| 1468 | Sussudio/Services/Capture/CaptureService.cs |
| 1437 | Sussudio/Services/Audio/WasapiAudioPlayback.cs |
| 1425 | Sussudio/Controllers/ViewModel/MainViewModelLifecycleController.cs |
| 1406 | Sussudio/Services/Recording/LibAvEncoder.cs |
| 1367 | Sussudio/Services/Automation/AutomationDiagnosticsHub.Evaluation.cs |
| 1317 | Sussudio/Controllers/Stats/StatsOverlayCompositionController.cs |

## Sample production files under 60 lines

| Lines | Path |
| ---: | --- |
| 21 | Sussudio/Models/Audio/DeviceAudioModeParser.cs |
| 44 | Sussudio/Program.cs |
| 49 | Sussudio/Models/Recording/RecordingSettingsSelection.cs |

## Notes

Use this as the before/after reference for the active defragmentation goal. A lower file count is not sufficient by itself; each slice should also improve behavioral locality, ownership, or deterministic testability.

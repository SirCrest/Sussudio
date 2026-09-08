# Sussudio Defragmentation Baseline - Generated

Generated UTC: 2026-09-08T02:12:19Z
Root: C:\Users\crest\source\repos\Sussudio

## Summary

| Metric | Value |
| --- | ---: |
| Production .cs files | 146 |
| Test .cs files | 78 |
| Core app .cs files (Sussudio/) | 112 |
| Core app nonblank LoC (Sussudio/) | 96423 |
| Sussudio.Tests .cs files | 77 |
| Sussudio.Tests nonblank LoC | 70409 |
| Production .cs files under 60 lines | 3 (2.1%) |
| Production .cs files under 80 lines | 7 (4.8%) |

## Largest partial-type clusters

| Type | Files | Total lines | Sample paths |
| --- | ---: | ---: | --- |
| CaptureService | 6 | 11249 | Sussudio/Services/Capture/CaptureService.cs, Sussudio/Services/Capture/CaptureService.Flashback.cs, Sussudio/Services/Capture/CaptureService.HealthSnapshots.cs, Sussudio/Services/Capture/CaptureService.PreviewLifecycle.cs, Sussudio/Services/Capture/CaptureService.RecordingLifecycle.cs, Sussudio/Services/Capture/CaptureService.RuntimeSnapshots.cs |
| AutomationDiagnosticsHub | 4 | 7437 | Sussudio/Services/Automation/AutomationDiagnosticsHub.cs, Sussudio/Services/Automation/AutomationDiagnosticsHub.Evaluation.cs, Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs, Sussudio/Services/Automation/AutomationDiagnosticsHub.Snapshots.cs |
| D3D11PreviewRenderer | 3 | 5304 | Sussudio/Services/Preview/D3D11PreviewRenderer.cs, Sussudio/Services/Preview/D3D11PreviewRenderer.RenderPasses.cs, Sussudio/Services/Preview/D3D11PreviewRenderer.Resources.cs |
| FlashbackPlaybackController | 3 | 4725 | Sussudio/Services/Flashback/FlashbackPlaybackController.cs, Sussudio/Services/Flashback/FlashbackPlaybackController.PlaybackFrames.cs, Sussudio/Services/Flashback/FlashbackPlaybackController.ThreadCommands.cs |
| LibAvEncoder | 3 | 3047 | Sussudio/Services/Recording/LibAvEncoder.Audio.cs, Sussudio/Services/Recording/LibAvEncoder.cs, Sussudio/Services/Recording/LibAvEncoder.VideoFrames.cs |
| MainViewModel | 3 | 5226 | Sussudio/ViewModels/MainViewModel.AudioState.cs, Sussudio/ViewModels/MainViewModel.cs, Sussudio/ViewModels/MainViewModel.FlashbackState.cs |
| App | 1 | 222 | Sussudio/App.xaml.cs |
| DeviceFormatCacheJsonContext | 1 | 755 | Sussudio/Services/Capture/DeviceService.cs |
| LoggingJsonContext | 1 | 613 | Sussudio/AppRuntime.cs |
| MainWindow | 1 | 2540 | Sussudio/MainWindow.xaml.cs |
| SettingsJsonContext | 1 | 785 | Sussudio/Services/Runtime/RuntimeHelpers.cs |
| StatsWindow | 1 | 328 | Sussudio/StatsWindow.xaml.cs |

## Largest production files

| Lines | Path |
| ---: | --- |
| 3157 | Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs |
| 3121 | Sussudio/ViewModels/MainViewModel.cs |
| 2978 | Sussudio/Services/Flashback/FlashbackExporter.cs |
| 2908 | Sussudio/Services/Flashback/FlashbackEncoderSink.cs |
| 2600 | Sussudio/Services/Preview/D3D11PreviewRenderer.cs |
| 2542 | Sussudio/Services/Capture/CaptureService.Flashback.cs |
| 2540 | Sussudio/MainWindow.xaml.cs |
| 2522 | Sussudio/Services/Capture/CaptureService.RecordingLifecycle.cs |
| 2469 | Sussudio/Services/Flashback/FlashbackPlaybackController.cs |
| 2380 | Sussudio/Services/Telemetry/NativeXuAtCommandProvider.cs |
| 2149 | Sussudio/Services/Flashback/FlashbackDecoder.cs |
| 2097 | Sussudio/Services/Capture/MfSourceReaderVideoCapture.cs |
| 2073 | Sussudio/Services/Recording/LibAvRecordingSink.cs |
| 2033 | Sussudio/Services/Capture/CaptureService.RuntimeSnapshots.cs |
| 1934 | Sussudio/Services/Automation/AutomationDiagnosticsHub.Snapshots.cs |
| 1813 | Sussudio/Services/Flashback/FlashbackBufferManager.cs |
| 1786 | Sussudio/Services/Capture/UnifiedVideoCapture.cs |
| 1775 | Sussudio/Controllers/ViewModel/MainViewModelDeviceControllers.cs |
| 1732 | Sussudio/Services/Automation/AutomationCommandDispatcher.cs |
| 1667 | Sussudio/Services/Capture/Mjpeg/ParallelMjpegDecodePipeline.cs |
| 1604 | Sussudio/Services/Preview/D3D11PreviewRenderer.Resources.cs |
| 1563 | Sussudio/ViewModels/ViewModelSelectionPolicies.cs |
| 1562 | Sussudio/Services/Capture/CaptureService.HealthSnapshots.cs |
| 1508 | Sussudio/Controllers/Flashback/FlashbackUiControllers.cs |
| 1455 | Sussudio/Controllers/Stats/StatsOverlayCompositionController.cs |
| 1437 | Sussudio/Services/Audio/WasapiAudioPlayback.cs |
| 1434 | Sussudio/Controllers/ViewModel/MainViewModelLifecycleController.cs |
| 1398 | Sussudio/Services/Recording/LibAvEncoder.cs |
| 1367 | Sussudio/Services/Automation/AutomationDiagnosticsHub.Evaluation.cs |
| 1352 | Sussudio/Services/Capture/CaptureService.cs |

## Sample production files under 60 lines

| Lines | Path |
| ---: | --- |
| 21 | Sussudio/Models/Audio/DeviceAudioModeParser.cs |
| 44 | Sussudio/Program.cs |
| 49 | Sussudio/Models/Recording/RecordingSettingsSelection.cs |

## Notes

Use this as the before/after reference for the active defragmentation goal. A lower file count is not sufficient by itself; each slice should also improve behavioral locality, ownership, or deterministic testability.

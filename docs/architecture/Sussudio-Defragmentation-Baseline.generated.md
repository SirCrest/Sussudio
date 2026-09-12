# Sussudio Defragmentation Baseline - Generated

Generated UTC: 2026-09-12T00:54:35Z
Root: C:\Users\crest\source\repos\Sussudio

## Summary

| Metric | Value |
| --- | ---: |
| Production .cs files | 149 |
| Test .cs files | 90 |
| Core app .cs files (Sussudio/) | 115 |
| Core app nonblank LoC (Sussudio/) | 96396 |
| Sussudio.Tests .cs files | 89 |
| Sussudio.Tests nonblank LoC | 70964 |
| Production .cs files under 60 lines | 3 (2.0%) |
| Production .cs files under 80 lines | 7 (4.7%) |

## Largest partial-type clusters

| Type | Files | Total lines | Sample paths |
| --- | ---: | ---: | --- |
| CaptureService | 6 | 11020 | Sussudio/Services/Capture/CaptureService.cs, Sussudio/Services/Capture/CaptureService.Flashback.cs, Sussudio/Services/Capture/CaptureService.HealthSnapshots.cs, Sussudio/Services/Capture/CaptureService.PreviewLifecycle.cs, Sussudio/Services/Capture/CaptureService.RecordingLifecycle.cs, Sussudio/Services/Capture/CaptureService.RuntimeSnapshots.cs |
| AutomationDiagnosticsHub | 5 | 7451 | Sussudio/Services/Automation/AutomationDiagnosticsHub.cs, Sussudio/Services/Automation/AutomationDiagnosticsHub.Evaluation.cs, Sussudio/Services/Automation/AutomationDiagnosticsHub.FlashbackEvaluation.cs, Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs, Sussudio/Services/Automation/AutomationDiagnosticsHub.Snapshots.cs |
| D3D11PreviewRenderer | 3 | 5320 | Sussudio/Services/Preview/D3D11PreviewRenderer.cs, Sussudio/Services/Preview/D3D11PreviewRenderer.RenderPasses.cs, Sussudio/Services/Preview/D3D11PreviewRenderer.Resources.cs |
| FlashbackPlaybackController | 3 | 4725 | Sussudio/Services/Flashback/FlashbackPlaybackController.cs, Sussudio/Services/Flashback/FlashbackPlaybackController.PlaybackFrames.cs, Sussudio/Services/Flashback/FlashbackPlaybackController.ThreadCommands.cs |
| LibAvEncoder | 3 | 3055 | Sussudio/Services/Recording/LibAvEncoder.Audio.cs, Sussudio/Services/Recording/LibAvEncoder.cs, Sussudio/Services/Recording/LibAvEncoder.VideoFrames.cs |
| MainViewModel | 3 | 5235 | Sussudio/ViewModels/MainViewModel.AudioState.cs, Sussudio/ViewModels/MainViewModel.cs, Sussudio/ViewModels/MainViewModel.FlashbackState.cs |
| LoggingJsonContext | 1 | 614 | Sussudio/AppRuntime.cs |
| SettingsJsonContext | 1 | 907 | Sussudio/Services/Runtime/RuntimeHelpers.cs |
| App | 1 | 222 | Sussudio/App.xaml.cs |
| DeviceFormatCacheJsonContext | 1 | 766 | Sussudio/Services/Capture/DeviceService.cs |
| StatsWindow | 1 | 331 | Sussudio/StatsWindow.xaml.cs |
| MainWindow | 1 | 2539 | Sussudio/MainWindow.xaml.cs |

## Largest production files

| Lines | Path |
| ---: | --- |
| 3157 | Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs |
| 3118 | Sussudio/ViewModels/MainViewModel.cs |
| 2974 | Sussudio/Services/Flashback/FlashbackExporter.cs |
| 2879 | Sussudio/Services/Flashback/FlashbackEncoderSink.cs |
| 2620 | Sussudio/Services/Preview/D3D11PreviewRenderer.cs |
| 2545 | Sussudio/Services/Capture/CaptureService.RecordingLifecycle.cs |
| 2539 | Sussudio/MainWindow.xaml.cs |
| 2469 | Sussudio/Services/Flashback/FlashbackPlaybackController.cs |
| 2380 | Sussudio/Services/Telemetry/NativeXuAtCommandProvider.cs |
| 2177 | Sussudio/Services/Capture/CaptureService.Flashback.cs |
| 2144 | Sussudio/Services/Flashback/FlashbackDecoder.cs |
| 2117 | Sussudio/Services/Capture/MfSourceReaderVideoCapture.cs |
| 2030 | Sussudio/Services/Capture/CaptureService.RuntimeSnapshots.cs |
| 2024 | Sussudio/Services/Recording/LibAvRecordingSink.cs |
| 1933 | Sussudio/Services/Automation/AutomationDiagnosticsHub.Snapshots.cs |
| 1813 | Sussudio/Services/Flashback/FlashbackBufferManager.cs |
| 1783 | Sussudio/Services/Capture/UnifiedVideoCapture.cs |
| 1775 | Sussudio/Controllers/ViewModel/MainViewModelDeviceControllers.cs |
| 1733 | Sussudio/Services/Automation/AutomationCommandDispatcher.cs |
| 1640 | Sussudio/Services/Capture/Mjpeg/ParallelMjpegDecodePipeline.cs |
| 1600 | Sussudio/Services/Preview/D3D11PreviewRenderer.Resources.cs |
| 1563 | Sussudio/ViewModels/ViewModelSelectionPolicies.cs |
| 1562 | Sussudio/Services/Capture/CaptureService.HealthSnapshots.cs |
| 1508 | Sussudio/Controllers/Flashback/FlashbackUiControllers.cs |
| 1450 | Sussudio/Services/Capture/CaptureService.cs |
| 1429 | Sussudio/Services/Audio/WasapiAudioPlayback.cs |
| 1413 | Sussudio/Controllers/ViewModel/MainViewModelLifecycleController.cs |
| 1406 | Sussudio/Services/Recording/LibAvEncoder.cs |
| 1317 | Sussudio/Controllers/Stats/StatsOverlayCompositionController.cs |
| 1305 | Sussudio/Services/Capture/MjpegPreviewJitterBuffer.cs |

## Sample production files under 60 lines

| Lines | Path |
| ---: | --- |
| 21 | Sussudio/Models/Audio/DeviceAudioModeParser.cs |
| 44 | Sussudio/Program.cs |
| 49 | Sussudio/Models/Recording/RecordingSettingsSelection.cs |

## Notes

Use this as the before/after reference for the active defragmentation goal. A lower file count is not sufficient by itself; each slice should also improve behavioral locality, ownership, or deterministic testability.

# Sussudio Defragmentation Baseline - Generated

Generated UTC: 2026-09-24T02:11:54Z
Root: C:\Users\crest\source\repos\Sussudio

## Summary

| Metric | Value |
| --- | ---: |
| Production .cs files | 153 |
| Test .cs files | 106 |
| Core app .cs files (Sussudio/) | 117 |
| Core app nonblank LoC (Sussudio/) | 93169 |
| Sussudio.Tests .cs files | 106 |
| Sussudio.Tests nonblank LoC | 83246 |
| Production .cs files under 60 lines | 5 (3.3%) |
| Production .cs files under 80 lines | 9 (5.9%) |

## Largest partial-type clusters

| Type | Files | Total lines | Sample paths |
| --- | ---: | ---: | --- |
| CaptureService | 6 | 10957 | Sussudio/Services/Capture/CaptureService.cs, Sussudio/Services/Capture/CaptureService.Flashback.cs, Sussudio/Services/Capture/CaptureService.HealthSnapshots.cs, Sussudio/Services/Capture/CaptureService.PreviewLifecycle.cs, Sussudio/Services/Capture/CaptureService.RecordingLifecycle.cs, Sussudio/Services/Capture/CaptureService.RuntimeSnapshots.cs |
| AutomationDiagnosticsHub | 5 | 5145 | Sussudio/Services/Automation/AutomationDiagnosticsHub.cs, Sussudio/Services/Automation/AutomationDiagnosticsHub.Evaluation.cs, Sussudio/Services/Automation/AutomationDiagnosticsHub.FlashbackEvaluation.cs, Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs, Sussudio/Services/Automation/AutomationDiagnosticsHub.Snapshots.cs |
| MainViewModel | 3 | 5182 | Sussudio/ViewModels/MainViewModel.AudioState.cs, Sussudio/ViewModels/MainViewModel.cs, Sussudio/ViewModels/MainViewModel.FlashbackState.cs |
| D3D11PreviewRenderer | 3 | 5280 | Sussudio/Services/Preview/D3D11PreviewRenderer.cs, Sussudio/Services/Preview/D3D11PreviewRenderer.RenderPasses.cs, Sussudio/Services/Preview/D3D11PreviewRenderer.Resources.cs |
| LibAvEncoder | 3 | 2951 | Sussudio/Services/Recording/LibAvEncoder.Audio.cs, Sussudio/Services/Recording/LibAvEncoder.cs, Sussudio/Services/Recording/LibAvEncoder.VideoFrames.cs |
| FlashbackPlaybackController | 3 | 4692 | Sussudio/Services/Flashback/FlashbackPlaybackController.cs, Sussudio/Services/Flashback/FlashbackPlaybackController.PlaybackFrames.cs, Sussudio/Services/Flashback/FlashbackPlaybackController.ThreadCommands.cs |
| FrameRateTimingPolicy | 2 | 1566 | Sussudio/ViewModels/FrameRateTimingPolicy.Match.cs, Sussudio/ViewModels/ViewModelSelectionPolicies.cs |
| NativeXuAudioControlService | 2 | 723 | Sussudio/Services/Audio/NativeXuAudioControlService.cs, tools/NativeXuAudioProbe/NativeXuAudioControlService.Experiments.cs |
| SettingsJsonContext | 1 | 123 | Sussudio/Services/Runtime/SettingsService.cs |
| App | 1 | 234 | Sussudio/App.xaml.cs |
| LoggingJsonContext | 1 | 686 | Sussudio/AppRuntime.cs |
| DeviceFormatCacheJsonContext | 1 | 766 | Sussudio/Services/Capture/DeviceService.cs |
| StatsWindow | 1 | 328 | Sussudio/StatsWindow.xaml.cs |
| MainWindow | 1 | 2402 | Sussudio/MainWindow.xaml.cs |

## Largest production files

| Lines | Path |
| ---: | --- |
| 3086 | Sussudio/ViewModels/MainViewModel.cs |
| 2979 | Sussudio/Services/Flashback/FlashbackExporter.cs |
| 2870 | Sussudio/Services/Flashback/FlashbackEncoderSink.cs |
| 2588 | Sussudio/Services/Capture/CaptureService.RecordingLifecycle.cs |
| 2579 | Sussudio/Services/Preview/D3D11PreviewRenderer.cs |
| 2482 | Sussudio/Services/Flashback/FlashbackPlaybackController.cs |
| 2402 | Sussudio/MainWindow.xaml.cs |
| 2170 | Sussudio/Services/Telemetry/NativeXuAtCommandProvider.cs |
| 2146 | Sussudio/Services/Flashback/FlashbackDecoder.cs |
| 2072 | Sussudio/Services/Capture/CaptureService.Flashback.cs |
| 2030 | Sussudio/Services/Capture/MfSourceReaderVideoCapture.cs |
| 2029 | Sussudio/Services/Capture/CaptureService.RuntimeSnapshots.cs |
| 2024 | Sussudio/Services/Recording/LibAvRecordingSink.cs |
| 1931 | Sussudio/Services/Automation/AutomationDiagnosticsHub.Snapshots.cs |
| 1817 | Sussudio/Services/Capture/UnifiedVideoCapture.cs |
| 1813 | Sussudio/Services/Flashback/FlashbackBufferManager.cs |
| 1759 | Sussudio/Services/Automation/AutomationCommandDispatcher.cs |
| 1750 | Sussudio/Controllers/ViewModel/MainViewModelDeviceControllers.cs |
| 1641 | Sussudio/Services/Capture/Mjpeg/ParallelMjpegDecodePipeline.cs |
| 1600 | Sussudio/Services/Preview/D3D11PreviewRenderer.Resources.cs |
| 1561 | Sussudio/Services/Capture/CaptureService.HealthSnapshots.cs |
| 1555 | Sussudio/ViewModels/ViewModelSelectionPolicies.cs |
| 1541 | Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs |
| 1508 | Sussudio/Controllers/Flashback/FlashbackUiControllers.cs |
| 1478 | Sussudio/Services/Capture/CaptureService.cs |
| 1475 | Sussudio/Services/Audio/WasapiAudioPlayback.cs |
| 1452 | Sussudio/Controllers/ViewModel/MainViewModelLifecycleController.cs |
| 1406 | Sussudio/Services/Recording/LibAvEncoder.cs |
| 1303 | Sussudio/Services/Flashback/FlashbackPlaybackController.PlaybackFrames.cs |
| 1299 | Sussudio/Controllers/Stats/StatsOverlayCompositionController.cs |

## Sample production files under 60 lines

| Lines | Path |
| ---: | --- |
| 11 | Sussudio/ViewModels/FrameRateTimingPolicy.Match.cs |
| 21 | Sussudio/Models/Audio/DeviceAudioModeParser.cs |
| 44 | Sussudio/Program.cs |
| 49 | Sussudio/Models/Recording/RecordingSettingsSelection.cs |
| 52 | Sussudio/Services/Interop/ComObjectReleaser.cs |

## Notes

Use this as the before/after reference for the active defragmentation goal. A lower file count is not sufficient by itself; each slice should also improve behavioral locality, ownership, or deterministic testability.

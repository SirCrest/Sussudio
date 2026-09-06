# Sussudio Defragmentation Baseline - Generated

Generated UTC: 2026-09-06T08:47:50Z
Root: C:\Users\crest\source\repos\Sussudio

## Summary

| Metric | Value |
| --- | ---: |
| Production .cs files | 139 |
| Test .cs files | 48 |
| Core app .cs files (Sussudio/) | 105 |
| Core app nonblank LoC (Sussudio/) | 95405 |
| Sussudio.Tests .cs files | 47 |
| Sussudio.Tests nonblank LoC | 63640 |
| Production .cs files under 60 lines | 1 (0.7%) |
| Production .cs files under 80 lines | 4 (2.9%) |

## Largest partial-type clusters

| Type | Files | Total lines | Sample paths |
| --- | ---: | ---: | --- |
| CaptureService | 6 | 11232 | Sussudio/Services/Capture/CaptureService.cs, Sussudio/Services/Capture/CaptureService.Flashback.cs, Sussudio/Services/Capture/CaptureService.HealthSnapshots.cs, Sussudio/Services/Capture/CaptureService.PreviewLifecycle.cs, Sussudio/Services/Capture/CaptureService.RecordingLifecycle.cs, Sussudio/Services/Capture/CaptureService.RuntimeSnapshots.cs |
| AutomationDiagnosticsHub | 4 | 7431 | Sussudio/Services/Automation/AutomationDiagnosticsHub.cs, Sussudio/Services/Automation/AutomationDiagnosticsHub.Evaluation.cs, Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs, Sussudio/Services/Automation/AutomationDiagnosticsHub.Snapshots.cs |
| D3D11PreviewRenderer | 3 | 5282 | Sussudio/Services/Preview/D3D11PreviewRenderer.cs, Sussudio/Services/Preview/D3D11PreviewRenderer.RenderPasses.cs, Sussudio/Services/Preview/D3D11PreviewRenderer.Resources.cs |
| FlashbackPlaybackController | 3 | 4646 | Sussudio/Services/Flashback/FlashbackPlaybackController.cs, Sussudio/Services/Flashback/FlashbackPlaybackController.PlaybackFrames.cs, Sussudio/Services/Flashback/FlashbackPlaybackController.ThreadCommands.cs |
| LibAvEncoder | 3 | 3049 | Sussudio/Services/Recording/LibAvEncoder.Audio.cs, Sussudio/Services/Recording/LibAvEncoder.cs, Sussudio/Services/Recording/LibAvEncoder.VideoFrames.cs |
| MainViewModel | 3 | 5275 | Sussudio/ViewModels/MainViewModel.AudioState.cs, Sussudio/ViewModels/MainViewModel.cs, Sussudio/ViewModels/MainViewModel.FlashbackState.cs |
| App | 1 | 283 | Sussudio/App.xaml.cs |
| DeviceFormatCacheJsonContext | 1 | 716 | Sussudio/Services/Capture/DeviceService.cs |
| LoggingJsonContext | 1 | 573 | Sussudio/AppRuntime.cs |
| MainWindow | 1 | 2556 | Sussudio/MainWindow.xaml.cs |
| SettingsJsonContext | 1 | 765 | Sussudio/Services/Runtime/RuntimeHelpers.cs |
| StatsWindow | 1 | 328 | Sussudio/StatsWindow.xaml.cs |

## Largest production files

| Lines | Path |
| ---: | --- |
| 3157 | Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs |
| 3112 | Sussudio/ViewModels/MainViewModel.cs |
| 2947 | Sussudio/Services/Flashback/FlashbackEncoderSink.cs |
| 2905 | Sussudio/Services/Flashback/FlashbackExporter.cs |
| 2595 | Sussudio/Services/Capture/CaptureService.Flashback.cs |
| 2579 | Sussudio/Services/Preview/D3D11PreviewRenderer.cs |
| 2556 | Sussudio/MainWindow.xaml.cs |
| 2519 | Sussudio/Services/Capture/CaptureService.RecordingLifecycle.cs |
| 2467 | Sussudio/Services/Flashback/FlashbackPlaybackController.cs |
| 2380 | Sussudio/Services/Telemetry/NativeXuAtCommandProvider.cs |
| 2103 | Sussudio/Services/Capture/MfSourceReaderVideoCapture.cs |
| 2090 | Sussudio/Services/Recording/LibAvRecordingSink.cs |
| 1963 | Sussudio/Services/Capture/CaptureService.RuntimeSnapshots.cs |
| 1956 | Sussudio/Services/Flashback/FlashbackDecoder.cs |
| 1932 | Sussudio/Services/Automation/AutomationDiagnosticsHub.Snapshots.cs |
| 1808 | Sussudio/Services/Flashback/FlashbackBufferManager.cs |
| 1775 | Sussudio/Services/Capture/UnifiedVideoCapture.cs |
| 1745 | tools/Common/DiagnosticSessionResultBuilder.cs |
| 1734 | Sussudio/Controllers/ViewModel/MainViewModelDeviceControllers.cs |
| 1727 | Sussudio/Services/Automation/AutomationCommandDispatcher.cs |
| 1667 | Sussudio/Services/Gpu/ParallelMjpegDecodePipeline.cs |
| 1603 | Sussudio/Services/Preview/D3D11PreviewRenderer.Resources.cs |
| 1596 | Sussudio/ViewModels/ViewModelSelectionPolicies.cs |
| 1561 | Sussudio/Services/Capture/CaptureService.HealthSnapshots.cs |
| 1508 | Sussudio/Controllers/Flashback/FlashbackUiControllers.cs |
| 1454 | Sussudio/Controllers/Stats/StatsOverlayCompositionController.cs |
| 1437 | Sussudio/Services/Audio/WasapiAudioPlayback.cs |
| 1400 | Sussudio/Services/Recording/LibAvEncoder.cs |
| 1382 | Sussudio/Controllers/ViewModel/MainViewModelLifecycleController.cs |
| 1363 | Sussudio/Services/Automation/AutomationDiagnosticsHub.Evaluation.cs |

## Sample production files under 60 lines

| Lines | Path |
| ---: | --- |
| 21 | Sussudio/Models/DeviceAudioModeParser.cs |

## Notes

Use this as the before/after reference for the active defragmentation goal. A lower file count is not sufficient by itself; each slice should also improve behavioral locality, ownership, or deterministic testability.

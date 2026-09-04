# Sussudio Defragmentation Baseline - Generated

Generated UTC: 2026-09-03T09:50:41Z
Root: C:\Users\crest\source\repos\Sussudio

## Summary

| Metric | Value |
| --- | ---: |
| Production .cs files | 126 |
| Test .cs files | 23 |
| Core app .cs files (Sussudio/) | 92 |
| Core app nonblank LoC (Sussudio/) | 96495 |
| Sussudio.Tests .cs files | 22 |
| Sussudio.Tests nonblank LoC | 60549 |
| Production .cs files under 60 lines | 1 (0.8%) |
| Production .cs files under 80 lines | 2 (1.6%) |

## Largest partial-type clusters

| Type | Files | Total lines | Sample paths |
| --- | ---: | ---: | --- |
| CaptureService | 6 | 11673 | Sussudio/Services/Capture/CaptureService.cs, Sussudio/Services/Capture/CaptureService.Flashback.cs, Sussudio/Services/Capture/CaptureService.HealthSnapshots.cs, Sussudio/Services/Capture/CaptureService.PreviewLifecycle.cs, Sussudio/Services/Capture/CaptureService.RecordingLifecycle.cs, Sussudio/Services/Capture/CaptureService.RuntimeSnapshots.cs |
| AutomationDiagnosticsHub | 4 | 9509 | Sussudio/Services/Automation/AutomationDiagnosticsHub.cs, Sussudio/Services/Automation/AutomationDiagnosticsHub.Evaluation.cs, Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs, Sussudio/Services/Automation/AutomationDiagnosticsHub.Snapshots.cs |
| D3D11PreviewRenderer | 3 | 5023 | Sussudio/Services/Preview/D3D11PreviewRenderer.cs, Sussudio/Services/Preview/D3D11PreviewRenderer.RenderPasses.cs, Sussudio/Services/Preview/D3D11PreviewRenderer.Resources.cs |
| FlashbackPlaybackController | 3 | 5095 | Sussudio/Services/Flashback/FlashbackPlaybackController.cs, Sussudio/Services/Flashback/FlashbackPlaybackController.PlaybackFrames.cs, Sussudio/Services/Flashback/FlashbackPlaybackController.ThreadCommands.cs |
| LibAvEncoder | 3 | 3011 | Sussudio/Services/Recording/LibAvEncoder.Audio.cs, Sussudio/Services/Recording/LibAvEncoder.cs, Sussudio/Services/Recording/LibAvEncoder.VideoFrames.cs |
| MainViewModel | 3 | 5260 | Sussudio/ViewModels/MainViewModel.AudioState.cs, Sussudio/ViewModels/MainViewModel.cs, Sussudio/ViewModels/MainViewModel.FlashbackState.cs |
| App | 1 | 270 | Sussudio/App.xaml.cs |
| DeviceFormatCacheJsonContext | 1 | 716 | Sussudio/Services/Capture/DeviceService.cs |
| LoggingJsonContext | 1 | 573 | Sussudio/AppRuntime.cs |
| MainWindow | 1 | 2561 | Sussudio/MainWindow.xaml.cs |
| SettingsJsonContext | 1 | 737 | Sussudio/Services/Runtime/RuntimeHelpers.cs |
| StatsWindow | 1 | 308 | Sussudio/StatsWindow.xaml.cs |

## Largest production files

| Lines | Path |
| ---: | --- |
| 5235 | Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs |
| 3541 | Sussudio/Services/Flashback/FlashbackExporter.cs |
| 3114 | Sussudio/ViewModels/MainViewModel.cs |
| 2988 | Sussudio/Services/Capture/CaptureService.Flashback.cs |
| 2890 | Sussudio/Services/Flashback/FlashbackEncoderSink.cs |
| 2875 | Sussudio/Services/Flashback/FlashbackPlaybackController.cs |
| 2641 | Sussudio/Services/Capture/CaptureService.RecordingLifecycle.cs |
| 2561 | Sussudio/MainWindow.xaml.cs |
| 2527 | Sussudio/Services/Telemetry/NativeXuAtCommandProvider.cs |
| 2486 | Sussudio/Services/Preview/D3D11PreviewRenderer.cs |
| 2132 | Sussudio/Services/Recording/LibAvRecordingSink.cs |
| 2099 | Sussudio/Services/Capture/MfSourceReaderVideoCapture.cs |
| 1948 | Sussudio/Services/Flashback/FlashbackDecoder.cs |
| 1932 | Sussudio/Services/Automation/AutomationDiagnosticsHub.Snapshots.cs |
| 1882 | Sussudio/Services/Capture/CaptureService.RuntimeSnapshots.cs |
| 1768 | Sussudio/Services/Flashback/FlashbackBufferManager.cs |
| 1753 | Sussudio/Services/Capture/UnifiedVideoCapture.cs |
| 1745 | tools/Common/DiagnosticSessionResultBuilder.cs |
| 1740 | Sussudio/Services/Automation/AutomationCommandDispatcher.cs |
| 1739 | Sussudio/Controllers/ViewModel/MainViewModelDeviceControllers.cs |
| 1697 | Sussudio/Services/Gpu/ParallelMjpegDecodePipeline.cs |
| 1632 | Sussudio/Controllers/Stats/StatsOverlayCompositionController.cs |
| 1596 | Sussudio/ViewModels/ViewModelSelectionPolicies.cs |
| 1561 | Sussudio/Services/Preview/D3D11PreviewRenderer.Resources.cs |
| 1561 | Sussudio/Services/Capture/CaptureService.HealthSnapshots.cs |
| 1508 | Sussudio/Controllers/Flashback/FlashbackUiControllers.cs |
| 1462 | Sussudio/Services/Audio/WasapiAudioPlayback.cs |
| 1367 | Sussudio/Controllers/ViewModel/MainViewModelLifecycleController.cs |
| 1363 | Sussudio/Services/Automation/AutomationDiagnosticsHub.Evaluation.cs |
| 1354 | Sussudio/Services/Capture/CaptureService.cs |

## Sample production files under 60 lines

| Lines | Path |
| ---: | --- |
| 21 | Sussudio/Models/DeviceAudioModeParser.cs |

## Notes

Use this as the before/after reference for the active defragmentation goal. A lower file count is not sufficient by itself; each slice should also improve behavioral locality, ownership, or deterministic testability.

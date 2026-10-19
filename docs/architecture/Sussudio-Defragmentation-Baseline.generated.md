# Sussudio Defragmentation Baseline - Generated

Generated UTC: 2026-10-19T01:24:01Z
Root: C:\Users\crest\source\repos\Sussudio

## Summary

| Metric | Value |
| --- | ---: |
| Production .cs files | 238 |
| Test .cs files | 132 |
| Core app .cs files (Sussudio/) | 178 |
| Core app nonblank LoC (Sussudio/) | 106706 |
| Sussudio.Tests .cs files | 132 |
| Sussudio.Tests nonblank LoC | 60608 |
| Production .cs files under 60 lines | 20 (8.4%) |
| Production .cs files under 80 lines | 27 (11.3%) |

## Largest partial-type clusters

| Type | Files | Total lines | Sample paths |
| --- | ---: | ---: | --- |
| CaptureService | 6 | 11064 | Sussudio/Services/Capture/CaptureService.cs, Sussudio/Services/Capture/CaptureService.Flashback.cs, Sussudio/Services/Capture/CaptureService.HealthSnapshots.cs, Sussudio/Services/Capture/CaptureService.PreviewLifecycle.cs, Sussudio/Services/Capture/CaptureService.RecordingLifecycle.cs, Sussudio/Services/Capture/CaptureService.RuntimeSnapshots.cs |
| AutomationDiagnosticsHub | 5 | 5168 | Sussudio/Services/Automation/AutomationDiagnosticsHub.cs, Sussudio/Services/Automation/AutomationDiagnosticsHub.Evaluation.cs, Sussudio/Services/Automation/AutomationDiagnosticsHub.FlashbackEvaluation.cs, Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs, Sussudio/Services/Automation/AutomationDiagnosticsHub.Snapshots.cs |
| AutomationCommandDispatcher | 4 | 2507 | Sussudio/Services/Automation/AutomationCommandDispatcher.AudioProbe.cs, Sussudio/Services/Automation/AutomationCommandDispatcher.cs, Sussudio/Services/Automation/AutomationCommandDispatcher.PerfTelemetry.cs, Sussudio/Services/Automation/AutomationCommandDispatcher.UiAutomation.cs |
| D3D11PreviewRenderer | 4 | 5995 | Sussudio/Services/Preview/D3D11PreviewRenderer.cs, Sussudio/Services/Preview/D3D11PreviewRenderer.GpuTiming.cs, Sussudio/Services/Preview/D3D11PreviewRenderer.RenderPasses.cs, Sussudio/Services/Preview/D3D11PreviewRenderer.Resources.cs |
| LibAvEncoder | 3 | 2999 | Sussudio/Services/Recording/LibAvEncoder.Audio.cs, Sussudio/Services/Recording/LibAvEncoder.cs, Sussudio/Services/Recording/LibAvEncoder.VideoFrames.cs |
| MainViewModel | 3 | 5400 | Sussudio/ViewModels/MainViewModel.AudioState.cs, Sussudio/ViewModels/MainViewModel.cs, Sussudio/ViewModels/MainViewModel.FlashbackState.cs |
| FlashbackPlaybackController | 3 | 4619 | Sussudio/Services/Flashback/FlashbackPlaybackController.cs, Sussudio/Services/Flashback/FlashbackPlaybackController.PlaybackFrames.cs, Sussudio/Services/Flashback/FlashbackPlaybackController.ThreadCommands.cs |
| CommandHandlers | 3 | 1619 | tools/ssctl/CommandHandlers.cs, tools/ssctl/CommandHandlers.Perf.cs, tools/ssctl/CommandHandlers.Senses.cs |
| FrameRateTimingPolicy | 2 | 1554 | Sussudio/ViewModels/FrameRateTimingPolicy.Match.cs, Sussudio/ViewModels/ViewModelSelectionPolicies.cs |
| NativeXuAudioControlService | 2 | 723 | Sussudio/Services/Audio/NativeXuAudioControlService.cs, tools/NativeXuAudioProbe/NativeXuAudioControlService.Experiments.cs |
| App | 1 | 238 | Sussudio/App.xaml.cs |
| LoggingJsonContext | 1 | 691 | Sussudio/AppRuntime.cs |
| SettingsJsonContext | 1 | 123 | Sussudio/Services/Runtime/SettingsService.cs |
| DeviceFormatCacheJsonContext | 1 | 770 | Sussudio/Services/Capture/DeviceService.cs |
| StatsWindow | 1 | 328 | Sussudio/StatsWindow.xaml.cs |
| MainWindow | 1 | 2348 | Sussudio/MainWindow.xaml.cs |

## Largest production files

| Lines | Path |
| ---: | --- |
| 3135 | Sussudio/ViewModels/MainViewModel.cs |
| 2971 | Sussudio/Services/Flashback/FlashbackExporter.cs |
| 2914 | Sussudio/Services/Flashback/FlashbackEncoderSink.cs |
| 2878 | Sussudio/Services/Preview/D3D11PreviewRenderer.cs |
| 2657 | Sussudio/Services/Flashback/FlashbackDecoder.cs |
| 2650 | Sussudio/Services/Capture/CaptureService.RecordingLifecycle.cs |
| 2396 | Sussudio/Services/Flashback/FlashbackPlaybackController.cs |
| 2348 | Sussudio/MainWindow.xaml.cs |
| 2232 | Sussudio/Services/Telemetry/NativeXuAtCommandProvider.cs |
| 2074 | Sussudio/Services/Capture/CaptureService.Flashback.cs |
| 2056 | Sussudio/Services/Capture/MfSourceReaderVideoCapture.cs |
| 2012 | Sussudio/Services/Capture/CaptureService.RuntimeSnapshots.cs |
| 1990 | Sussudio/Services/Automation/AutomationDiagnosticsHub.Snapshots.cs |
| 1974 | Sussudio/Services/Flashback/FlashbackBufferManager.cs |
| 1886 | Sussudio/Services/Recording/LibAvRecordingSink.cs |
| 1885 | Sussudio/Services/Capture/UnifiedVideoCapture.cs |
| 1807 | Sussudio/Services/Automation/AutomationCommandDispatcher.cs |
| 1751 | Sussudio/Controllers/ViewModel/MainViewModelDeviceControllers.cs |
| 1710 | Sussudio/Services/Preview/D3D11PreviewRenderer.Resources.cs |
| 1667 | Sussudio/Services/Capture/Mjpeg/ParallelMjpegDecodePipeline.cs |
| 1595 | Sussudio/Controllers/Flashback/FlashbackUiControllers.cs |
| 1545 | Sussudio/Services/Capture/CaptureService.HealthSnapshots.cs |
| 1543 | Sussudio/ViewModels/ViewModelSelectionPolicies.cs |
| 1526 | Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs |
| 1525 | Sussudio/Services/Capture/CaptureService.cs |
| 1505 | Sussudio/Controllers/ViewModel/MainViewModelLifecycleController.cs |
| 1492 | Sussudio/Services/Audio/WasapiAudioPlayback.cs |
| 1438 | Sussudio/Services/Recording/LibAvEncoder.cs |
| 1342 | Sussudio/Services/Capture/MjpegPreviewJitterBuffer.cs |
| 1314 | Sussudio/Controllers/Stats/StatsOverlayCompositionController.cs |

## Sample production files under 60 lines

| Lines | Path |
| ---: | --- |
| 11 | Sussudio/ViewModels/FrameRateTimingPolicy.Match.cs |
| 12 | Sussudio/Services/Performance/PerfWindows.cs |
| 19 | Sussudio/Services/Performance/LogTap.cs |
| 21 | Sussudio/Models/Audio/DeviceAudioModeParser.cs |
| 22 | Sussudio/Services/Performance/PerfCounter.cs |
| 37 | Sussudio/Services/Gpu/NvmlDemandSensor.cs |
| 42 | Sussudio/Services/Performance/AudioOutputProbeModels.cs |
| 44 | Sussudio/Program.cs |
| 44 | Sussudio/Services/Performance/PerfScope.cs |
| 46 | Sussudio/Services/Runtime/LogWriterBatch.cs |
| 46 | tools/Common/Performance/FrameTraceAnalyzer.cs |
| 47 | Sussudio/Services/Performance/TuningKnobs.cs |
| 49 | Sussudio/Models/Recording/RecordingSettingsSelection.cs |
| 49 | Sussudio/Services/Automation/UiSelectionVia.cs |
| 52 | Sussudio/Services/Interop/ComObjectReleaser.cs |
| 52 | Sussudio/Services/Performance/PerfIngressClock.cs |
| 54 | Sussudio/Services/Automation/UiVisibilityRules.cs |
| 54 | Sussudio/Services/Preview/PreviewRefreshCadencePolicy.cs |
| 56 | Sussudio/Services/Performance/AudioWavWriter.cs |
| 56 | tools/Common/Performance/PerfJson.cs |

## Notes

Use this as the before/after reference for the active defragmentation goal. A lower file count is not sufficient by itself; each slice should also improve behavioral locality, ownership, or deterministic testability.

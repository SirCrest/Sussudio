# Sussudio Agent Map

Last reviewed: 2026-05-16.

This file maps the current repo shape to named owners, entry points, invariants,
and fast checks. It is intentionally mechanical so future agents can find the
right file without guessing from old chat transcripts.

## Architecture Ownership Entry Points

These rows are the first places to look when changing a subsystem. Some entries
are still genuinely large; others are small roots for split families. Prefer
extracting new behavior into a named collaborator or feature folder instead of
expanding these ownership roots.

When splitting or moving code, update this map in the same commit, add or update
the focused ownership test, and search every `ReadRepoFile(...)` reference that
mentions the moved files.

| Area | Current large files | Preferred next owner |
|------|---------------------|----------------------|
| Diagnostic sessions | `tools/Common/DiagnosticSessionRunner.cs`, `tools/Common/DiagnosticSessionRunExecution.cs`, `tools/Common/DiagnosticSessionRunContext.cs`, `tools/Common/DiagnosticSessionRunContext.PhaseContexts.cs`, `tools/Common/DiagnosticSessionRunExecution.Completion.cs`, `tools/Common/DiagnosticSessionScenarioPhaseRunner.cs`, `tools/Common/DiagnosticSessionScenarioPhaseRunner.Models.cs`, `tools/Common/DiagnosticSessionScenarioPhaseRunner.Sampling.cs`, `tools/Common/DiagnosticSessionScenarioPhaseCompletion.cs` | public runner compatibility wrapper, phase sequencing, mutable run infrastructure, scenario/completion context construction, post-cleanup completion phase and result-build request handoff, named scenario phase execution/context/state/result, scenario sampling, post-sampling/fault-drain completion, run bootstrap/options normalization, scenario catalog, startup/cleanup/recording-check/post-run snapshot helpers, result formatter, plus per-scenario runners |
| Offline regression harness | `tests/Sussudio.Tests/Program.cs`, `tests/Sussudio.Tests/HarnessCheckCatalog*.cs` | runner entry point, topic check catalogs, xUnit slices, and focused contract tests such as `StatsPresentation.*.Tests.cs` |
| Capture runtime | `Sussudio/Services/Capture/CaptureService.cs`, `CaptureService.Initialization.cs`, `CaptureService.Audio.cs`, `CaptureService.AudioPreviewLifecycle.cs`, `CaptureService.AudioInputSwitching.cs`, `CaptureService.MicrophoneMonitor.cs`, `CaptureService.WasapiPlayback.cs`, `CaptureService.Cleanup.cs`, `CaptureService.Coordination.cs`, `CaptureService.DisposalLifecycle.cs`, `CaptureService.ResourceRelease.cs`, `CaptureService.DeferredCleanup.cs`, `CaptureService.Failures.cs`, `CaptureService.FailureCleanup.cs`, `CaptureService.FlashbackBackendFailureCleanup.cs`, `CaptureService.FlashbackControls.cs`, `CaptureService.FlashbackAudioInputs.cs`, `CaptureService.FlashbackPreviewBackend.cs`, `CaptureService.FlashbackPreviewBackendDisposal.cs`, `CaptureService.FlashbackBufferCycle.cs`, `CaptureService.FlashbackExportDiagnostics.cs`, `CaptureService.FlashbackExportFailureClassification.cs`, `CaptureService.FlashbackExportOperations.cs`, `CaptureService.FlashbackExportCore.cs`, `CaptureService.FlashbackExportPlanning.cs`, `CaptureService.FlashbackRecording.cs`, `CaptureService.HealthSnapshots.cs`, `CaptureService.HealthSnapshotAssembler.cs`, `CaptureService.HealthSnapshotCaptureCadence.cs`, `CaptureService.HealthSnapshotFlashbackBuffer.cs`, `CaptureService.HealthSnapshotFlashbackQueues.cs`, `CaptureService.HealthSnapshotMjpeg.cs`, `CaptureService.HealthSnapshotSourceTelemetry.cs`, `CaptureService.HealthSnapshotFlashbackExport.cs`, `CaptureService.HealthSnapshotFlashbackPlayback.cs`, `CaptureService.HealthSnapshotRecording.cs`, `CaptureService.HealthSnapshotRecordingActiveBackend.cs`, `CaptureService.PreviewStart.cs`, `CaptureService.PreviewAudioGraph.cs`, `CaptureService.PreviewStop.cs`, `CaptureService.PreviewReuse.cs`, `CaptureService.PreviewDisposal.cs`, `CaptureService.PreviewPipeline.cs`, `CaptureService.Probes.cs`, `CaptureService.RecordingIntegrity.cs`, `CaptureService.RecordingIntegrity.Models.cs`, `CaptureService.RecordingIntegrity.Summary.cs`, `CaptureService.RecordingIntegrity.Counters.cs`, `CaptureService.RecordingIntegrity.Audio.cs`, `CaptureService.RecordingIntegrity.Logging.cs`, `CaptureService.RecordingLifecycle.cs`, `CaptureService.RecordingStartState.cs`, `CaptureService.RecordingStartFlashback.cs`, `CaptureService.RecordingStartLibAv.cs`, `CaptureService.RecordingStartLibAv.AudioInputs.cs`, `CaptureService.RecordingStopLifecycle.cs`, `CaptureService.RecordingFinalizeFlashbackBackend.cs`, `CaptureService.RecordingFinalizeFlashbackBackendReconcile.cs`, `CaptureService.RecordingFinalizeLibAvBackend.cs`, `CaptureService.RecordingFinalizeLibAvPreviewRestore.cs`, `CaptureService.RecordingFinalizeFlashback.cs`, `CaptureService.RecordingFinalizeFlashbackBoundary.cs`, `CaptureService.RecordingOutcomeState.cs`, `CaptureService.RecordingRollback.cs`, `CaptureService.RuntimeSnapshots.cs`, `CaptureService.RuntimeSnapshotAssembler.cs`, `CaptureService.RuntimeSnapshotIngestAudio.cs`, `CaptureService.RuntimeSnapshotReaderTransport.cs`, `CaptureService.RuntimeSnapshotHdrPipeline.cs`, `CaptureService.RuntimeSnapshotSourceTelemetry.cs`, `CaptureService.RuntimeSnapshotRecordingIntegrity.cs`, `CaptureService.Snapshots.cs`, `CaptureService.SnapshotRecordingStats.cs`, `CaptureService.SnapshotRecordingFormat.cs`, `CaptureService.SnapshotObservedFrames.cs`, `CaptureService.SnapshotAvSync.cs`, `CaptureService.SnapshotTelemetry.cs`, `CaptureService.ObservedPixelTelemetry.cs`, `CaptureService.Telemetry.cs`, `CaptureService.CaptureFormatTelemetry.cs` | service state and construction owner, initialization owner, preview volume/audio event owner, audio-preview lifecycle owner, live audio input switching owner, microphone monitoring owner, WASAPI playback routing owner, cleanup owner, transition owner, disposal-triggered cleanup/final disposed-state owner, resource release helper owner, deferred cleanup owner, failure callback and failure-telemetry owner, fatal cleanup launch/session-state owner, Flashback backend failure cleanup/device-lost owner, Flashback control and restart orchestration owner, Flashback audio input restoration owner, Flashback preview backend startup owner, Flashback preview backend disposal owner, Flashback buffer cycle coordination owner, Flashback export diagnostics/progress owner, Flashback export failure taxonomy, Flashback export entry owner, Flashback export core owner, Flashback export planning/throttle owner, Flashback recording policy owner, health snapshot sampler, health snapshot DTO assembler, capture cadence health projection, Flashback buffer/backend health projection, Flashback queue health projection, MJPEG health snapshot projection, source telemetry health projection, Flashback export health projection, Flashback playback health projection, recording health orchestration, active recording backend health projection, preview start transition owner, preview audio graph owner, preview pipeline owner, probe owner, recording integrity active-backend resolver, integrity DTOs, integrity summary classification, integrity counter capture, audio integrity capture, integrity logging, recording start transition/router owner, recording start rollback-state holder, Flashback recording start owner, LibAv recording start owner, LibAv recording audio-input startup owner, recording stop transition/finalization router owner, Flashback recording backend finalization owner, Flashback recording backend reconciliation owner, LibAv recording backend finalization owner, LibAv recording preview-restore owner, Flashback recording export-finalize helper owner, Flashback recording boundary snapshot owner, recording outcome-state owner, transient recording rollback owner, runtime snapshot sampler, runtime snapshot DTO assembler, runtime ingest/audio projection, runtime reader/transport projection, runtime HDR/encoder pipeline projection, runtime source-telemetry projection, runtime recording-integrity projection, diagnostics compatibility and shared snapshot utilities, recording stats snapshot policy, recording format snapshot policy, observed frame snapshot telemetry, A/V sync snapshot policy, source telemetry snapshot policy, observed pixel telemetry owner, source telemetry polling owner, capture-format telemetry owner, resource managers |
| App project build workflow | `Sussudio/Sussudio.csproj`, `Sussudio/Sussudio.Build.targets` | app identity/assets/packages/runtime config in the project file; publish flags, locale stripping, and latest-build staging in imported targets |
| Device discovery | `Sussudio/Services/Capture/DeviceService.cs`, `DeviceService.FormatCache.cs`, `DeviceService.FormatProbe.cs`, `DeviceService.Scoring.cs`, `DeviceService.AudioAssociation.cs`, `DeviceService.NativeXu.cs`, `Sussudio/Services/Capture/DeviceDiscovery/MfDeviceEnumerator.cs`, `Sussudio/Services/Capture/DeviceDiscovery/MfDeviceEnumerator.VideoDevices.cs`, `Sussudio/Services/Capture/DeviceDiscovery/MfDeviceEnumerator.AudioEndpoints.cs`, `Sussudio/Services/Capture/DeviceDiscovery/MfDeviceEnumerator.FormatProbe.cs` | device enumeration orchestration, persisted format cache, inline/background format probing, priority/capability scoring, audio endpoint association, Native XU interface path resolution, shared MF constants/P/Invokes, MF video device enumeration, WASAPI capture endpoint enumeration, native MF format probing and source fallback |
| Native XU KS bridge | `Sussudio/Services/Capture/NativeXu/KsExtensionUnitNative.cs`, `Sussudio/Services/Capture/NativeXu/KsExtensionUnitNative.Interfaces.cs`, `Sussudio/Services/Capture/NativeXu/KsExtensionUnitNative.Handles.cs`, `Sussudio/Services/Capture/NativeXu/KsExtensionUnitNative.Topology.cs`, `Sussudio/Services/Capture/NativeXu/KsExtensionUnitNative.Transfers.cs`, `Sussudio/Services/Capture/NativeXu/KsExtensionUnitNative.Interop.cs` | KS category constants and DTOs, SetupAPI interface enumeration, file-handle open policy, topology node parsing, XU GET/SET transfer helpers, P/Invoke declarations and structs |
| Capture source reader | `Sussudio/Services/Capture/MfSourceReaderVideoCapture.cs`, `MfSourceReaderVideoCapture.Initialization.cs`, `MfSourceReaderVideoCapture.ReadLoop.cs`, `MfSourceReaderVideoCapture.FrameDelivery.cs`, `MfSourceReaderVideoCapture.Cadence.cs`, `MfSourceReaderVideoCapture.Diagnostics.cs`, `MfSourceReaderVideoCapture.DxgiBuffers.cs`, `MfSourceReaderVideoCapture.FrameLayout.cs`, `MfSourceReaderVideoCapture.Lifecycle.cs`, `MfSourceReaderVideoCapture.Negotiation.cs`, `MfSourceReaderVideoCapture.Interop.cs` | source-reader state and public counters, initialization/negotiated state application, Media Foundation read loop, sample-to-frame delivery, source cadence metrics, debug-only COM diagnostics, DXGI texture extraction, packed YUV frame layout and subtype labels, reader start/stop/dispose lifecycle, device opening and media-type negotiation, MF P/Invoke and COM interface definitions |
| Capture fan-out | `Sussudio/Services/Capture/UnifiedVideoCapture.cs`, `UnifiedVideoCapture.FrameIngress.cs`, `UnifiedVideoCapture.Lifecycle.cs`, `UnifiedVideoCapture.MjpegPipelineLifecycle.cs`, `UnifiedVideoCapture.SinkFanout.cs`, `UnifiedVideoCapture.Metrics.cs`, `UnifiedVideoCapture.Preview.cs` | public control/config surface, source-reader frame ingress and fatal-error signaling, shared source-reader lifecycle, CPU MJPEG pipeline/jitter lifecycle, recording/Flashback sink queue fan-out, diagnostic metric/snapshot projection, preview sink submission and visual-cadence handling |
| Audio capture | `Sussudio/Services/Audio/WasapiAudioCapture.cs`, `WasapiAudioCapture.CaptureLoop.cs`, `WasapiAudioCapture.Fanout.cs`, `WasapiAudioCapture.Conversion.cs`, `WasapiAudioCapture.Diagnostics.cs` | WASAPI device lifecycle, capture thread/packet drain, sink/playback/hot writer fan-out, f32le 48 kHz stereo conversion/resampling helpers, and callback/glitch metric projection |
| MJPEG preview pacing | `Sussudio/Services/Capture/MjpegPreviewJitterBuffer.cs`, `MjpegPreviewJitterBuffer.FrameIngress.cs`, `MjpegPreviewJitterBuffer.EmitLoop.cs`, `MjpegPreviewJitterBuffer.Queue.cs`, `MjpegPreviewJitterBuffer.Adaptive.cs`, `MjpegPreviewJitterBuffer.Metrics.cs` | construction, suppression, and disposal lifecycle, decoded preview-frame ingress and pooled payload ownership, paced emit loop and renderer submission, queue ordering and reprime recovery, adaptive deadline/depth policy, jitter-buffer metric records and timing sample projection |
| MJPEG decode pipeline | `Sussudio/Services/Gpu/ParallelMjpegDecodePipeline.cs`, `ParallelMjpegDecodePipeline.Reorder.cs`, `ParallelMjpegDecodePipeline.Lifecycle.cs`, `ParallelMjpegDecodePipeline.Metrics.cs`, `NvdecMjpegDecoder.cs`, `NvdecMjpegDecoder.Initialization.cs`, `NvdecMjpegDecoder.Decode.cs`, `NvdecMjpegDecoder.Download.cs`, `NvdecMjpegDecoder.Lifetime.cs`, `CudaD3D11Interop.cs`, `CudaD3D11Interop.Initialization.cs`, `CudaD3D11Interop.Copy.cs`, `CudaD3D11Interop.Lifetime.cs`, `CudaD3D11Interop.Native.cs` | CPU MJPEG worker/decode ingress, decoded-frame ordering and emission, stop/dispose/resource cleanup and fatal callback signaling, pipeline timing and packet-hash metrics, NVDEC decoder state, NVDEC initialization, decode/context access, CPU download/copy helpers, NVDEC disposal/error text, CUDA-to-D3D11 bridge state, bridge setup/zero-copy registration, zero-copy and staging copy behavior, bridge disposal/resource unregister, CUDA native constants/P/Invoke declarations |
| Automation diagnostics | `Sussudio/Services/Automation/AutomationDiagnosticsHub.cs`, `AutomationDiagnosticsHub.Alerts.cs`, `AutomationDiagnosticsHub.SignalAlerts.cs`, `AutomationDiagnosticsHub.FlashbackRecordingAlerts.cs`, `AutomationDiagnosticsHub.FlashbackPlaybackAlerts.cs`, `AutomationDiagnosticsHub.DiagnosticEvents.cs`, `AutomationDiagnosticsHub.DiagnosticEvaluation.cs`, `AutomationDiagnosticsHub.DiagnosticEvaluationFlashback.cs`, `AutomationDiagnosticsHub.DiagnosticEvaluationRealtime.cs`, `AutomationDiagnosticsHub.DiagnosticEvaluationRealtime.Preview.cs`, `AutomationDiagnosticsHub.DiagnosticEvaluationLanes.cs`, `AutomationDiagnosticsHub.EvaluationModels.cs`, `AutomationDiagnosticsHub.Evaluation.cs`, `AutomationDiagnosticsHub.EvaluationPolicy.cs`, `AutomationDiagnosticsHub.Hdr.cs`, `AutomationDiagnosticsHub.Lifecycle.cs`, `AutomationDiagnosticsHub.OutputFiles.cs`, `AutomationDiagnosticsHub.PreviewPacing.cs`, `AutomationDiagnosticsHub.ProcessMetrics.cs`, `AutomationDiagnosticsHub.Snapshots.cs`, `AutomationDiagnosticsHub.SnapshotProjection.cs`, `AutomationDiagnosticsHub.SnapshotProjection.Composition.cs`, `AutomationDiagnosticsHub.SnapshotProjection.Flattening.cs`, `AutomationDiagnosticsHub.SnapshotProjection.SnapshotStatus.cs`, `AutomationDiagnosticsHub.SnapshotProjection.SnapshotEvaluation.cs`, `AutomationDiagnosticsHub.SnapshotProjection.Audio.cs`, `AutomationDiagnosticsHub.SnapshotProjection.CaptureIngest.cs`, `AutomationDiagnosticsHub.SnapshotProjection.WasapiAudio.cs`, `AutomationDiagnosticsHub.SnapshotProjection.AvSync.cs`, `AutomationDiagnosticsHub.SnapshotProjection.CaptureCommands.cs`, `AutomationDiagnosticsHub.SnapshotProjection.CaptureFormat.cs`, `AutomationDiagnosticsHub.SnapshotProjection.CaptureTransport.cs`, `AutomationDiagnosticsHub.SnapshotProjection.CaptureCadence.cs`, `AutomationDiagnosticsHub.SnapshotProjection.Mjpeg.cs`, `AutomationDiagnosticsHub.SnapshotProjection.MjpegTiming.cs`, `AutomationDiagnosticsHub.SnapshotProjection.MjpegPreviewJitter.cs`, `AutomationDiagnosticsHub.SnapshotProjection.MjpegPacketHash.cs`, `AutomationDiagnosticsHub.SnapshotProjection.FlashbackExport.cs`, `AutomationDiagnosticsHub.SnapshotProjection.FlashbackPlayback.cs`, `AutomationDiagnosticsHub.SnapshotProjection.FlashbackRecording.cs`, `AutomationDiagnosticsHub.SnapshotProjection.FlashbackRecordingQueues.cs`, `AutomationDiagnosticsHub.SnapshotProjection.PreviewD3D.cs`, `AutomationDiagnosticsHub.SnapshotProjection.PreviewD3DCpuTiming.cs`, `AutomationDiagnosticsHub.SnapshotProjection.PreviewRuntime.cs`, `AutomationDiagnosticsHub.SnapshotProjection.ProcessResources.cs`, `AutomationDiagnosticsHub.SnapshotProjection.RecordingIntegrity.cs`, `AutomationDiagnosticsHub.SnapshotProjection.RecordingPipeline.cs`, `AutomationDiagnosticsHub.SnapshotProjection.RecordingOutput.cs`, `AutomationDiagnosticsHub.SnapshotProjection.SourceSignal.cs`, `AutomationDiagnosticsHub.SnapshotProjection.SourceTelemetry.cs`, `AutomationDiagnosticsHub.SnapshotProjection.UserSettings.cs`, `AutomationDiagnosticsHub.SnapshotProjection.HdrPipeline.cs`, `AutomationDiagnosticsHub.Counters.cs`, `AutomationDiagnosticsHub.SnapshotState.cs`, `AutomationDiagnosticsHub.Timeline.cs`, `AutomationDiagnosticsHub.TimelineProjection.cs`, `AutomationDiagnosticsHub.Verification.cs` | additional collectors/controllers when hub orchestration grows |
| Automation snapshot models | `Sussudio/Models/Automation/AutomationSnapshot*.cs`, `AutomationCommandProtocol.cs`, `AutomationOptionsSnapshot.cs`, `CaptureRuntimeSnapshot.cs`, `DiagnosticsEvents.cs`, `FlashbackSegmentInfo.cs`, `PerformanceTimelineEntry.cs`, `PreviewRuntimeSnapshot.cs`, `RecordingVerification.cs`, `VideoSourceProbe.cs`, `ViewModelRuntimeSnapshot.cs`, `WindowAutomation.cs` | automation evidence DTO partials by domain, command protocol DTOs, automation options DTO, capture runtime DTO, diagnostics events, Flashback segment DTOs, performance timeline entry, preview runtime DTO, recording verification DTOs, video source probe DTOs, view-model runtime DTO, window automation DTOs |
| Source telemetry | `Sussudio/Services/Telemetry/NativeXuAtCommandProvider.cs`, `NativeXuAtCommandProvider.AtProtocol.cs`, `NativeXuAtCommandProvider.AudioCommands.cs`, `NativeXuAtCommandProvider.AnalogGain.cs`, `NativeXuAtCommandProvider.AudioSwitch.cs`, `NativeXuAtCommandProvider.Selector4.cs`, `NativeXuAtCommandProvider.DeviceCommands.cs`, `NativeXuAtCommandProvider.DiagnosticSummary.cs`, `NativeXuAtCommandProvider.FullSnapshot.cs`, `NativeXuAtCommandProvider.PayloadDecoding.cs`, `NativeXuAtCommandProvider.RollingPoll.cs`, `NativeXuAtCommandProvider.RollingCommandGroups.cs`, `NativeXuAtCommandProvider.SnapshotAssembly.cs`, `NativeXuAtCommandProvider.TelemetryDetails.Build.cs`, `NativeXuAtCommandProvider.TelemetryDetails.AudioInput.cs`, `NativeXuAtCommandProvider.TelemetryDetails.Formatters.cs` | Native XU selected-interface validation and ReadAsync orchestration, AT-command transport/parsing, public HDMI/Analog audio route and gain command entry points, analog gain register mapping and writes, HDMI/Analog switch sequence, selector-4 I2C payload writes, generic public device-command surface, diagnostic summary formatting, reference full-snapshot command acquisition, source payload decoding/scalar helpers, active rolling poll cadence/cache, rolling command group dispatch and per-command cancellation helpers, source snapshot assembly from AT-command results, source telemetry detail row assembly, flash-audio input interpretation, AT detail value formatting |
| App service contracts | `Sussudio/Services/Contracts/AutomationInterfaces.cs`, `Sussudio/Services/Contracts/IPreviewFrameSink.cs`, `Sussudio/Services/Contracts/ISourceSignalTelemetryProvider.cs`, `Sussudio/Services/Contracts/RecordingContracts.cs`, `Sussudio/Services/Contracts/PooledVideoFrame.cs`, `Sussudio/Services/Contracts/PooledVideoFrameLease.cs`, `Sussudio/Services/Contracts/PreviewFrameTracking.cs` | shared in-process app-service contracts and pooled-frame ownership types; keep these separate from `Sussudio.Automation.Contracts` wire/protocol contracts |
| Recording | `Sussudio/Services/Recording/LibAvEncoder.cs`, `LibAvEncoder.Initialization.cs`, `LibAvEncoder.Audio.cs`, `LibAvEncoder.AudioSubmission.cs`, `LibAvEncoder.AudioInitialization.cs`, `LibAvEncoder.CodecPolicy.cs`, `LibAvEncoder.AvSync.cs`, `LibAvEncoder.PacketWriting.cs`, `LibAvEncoder.FrameCopy.cs`, `LibAvEncoder.VideoSubmission.cs`, `LibAvEncoder.Diagnostics.cs`, `LibAvEncoder.AudioSetup.cs`, `LibAvEncoder.HdrSideData.cs`, `LibAvEncoder.Models.cs`, `LibAvEncoder.VideoSetup.cs`, `LibAvEncoder.HardwareFrames.cs`, `LibAvEncoder.MuxerOptions.cs`, `LibAvEncoder.OutputRotation.cs`, `LibAvEncoder.ResourceCleanup.cs`, `LibAvRecordingSink.cs`, `LibAvRecordingSink.Startup.cs`, `LibAvRecordingSink.VideoSession.cs`, `LibAvRecordingSink.StopLifecycle.cs`, `LibAvRecordingSink.Diagnostics.cs`, `LibAvRecordingSink.Lifetime.cs`, `LibAvRecordingSink.Options.cs`, `LibAvRecordingSink.OutputValidation.cs`, `LibAvRecordingSink.EncodingLoop.cs`, `LibAvRecordingSink.Queues.cs`, `LibAvRecordingSink.VideoQueueSubmission.cs`, `LibAvRecordingSink.QueueCleanup.cs`, `LibAvRecordingSink.AudioQueues.cs`, `Sussudio/Services/Recording/Verification/RecordingVerifier.cs`, `Sussudio/Services/Recording/Verification/RecordingVerifier.Ffprobe.cs`, `Sussudio/Services/Recording/Verification/RecordingVerifier.ProbeParsing.cs`, `Sussudio/Services/Recording/Verification/RecordingVerifier.Validation.cs`, `Sussudio/Services/Recording/Verification/RecordingVerifier.Results.cs`, `Sussudio/Services/Recording/Verification/RecordingVerifier.Cadence.cs` | encoder core state, encoder runtime/open initialization, audio/microphone shared state and drains, public audio submission, audio stream initialization, codec/options policy, A/V sync diagnostics, video packet drain/write helpers, packed software-frame copy helpers, video frame submission paths, open/error/device-removed diagnostics, video codec setup, hardware-frame setup, muxer option policy, output rotation/reopen/reset, final close/native cleanup, sink state/construction, recording sink startup shell, recording sink video/GPU/CUDA session queue initialization, metric reset, and video diagnostics reset, recording sink stop/finalize lifecycle, recording sink diagnostics surface, dispose/deferred cleanup, encoder option creation, stopped-output validation, recording sink encode-drain loop, recording sink video/GPU/CUDA public enqueue adapters plus shared signal/failure/depth helpers, recording sink video/GPU/CUDA queue admission and packet records, recording sink queue cleanup and pooled packet return helpers, recording sink audio queue surface, verifier orchestration/finalizer, ffprobe process work, probe parsing, validation policy, result/taxonomy shaping, verifier cadence analysis |
| Flashback | `FlashbackDecoder.cs`, `FlashbackDecoder.D3D11.cs`, `FlashbackDecoder.VideoOutput.cs`, `FlashbackDecoder.VideoSetup.cs`, `FlashbackDecoder.AudioOutput.cs`, `FlashbackDecoder.Timestamps.cs`, `FlashbackDecoder.Seeking.cs`, `FlashbackDecoder.DecodeLoop.cs`, `FlashbackDecoder.Validation.cs`, `FlashbackDecoder.Lifetime.cs`, `FlashbackDecoder.Diagnostics.cs`, `FlashbackDecoder.Guards.cs`, `FlashbackDecoder.OutputTypes.cs`, `FlashbackPlaybackController.cs`, `FlashbackPlaybackController.DecoderFiles.cs`, `FlashbackPlaybackController.DecoderReopen.cs`, `FlashbackPlaybackController.Lifecycle.cs`, `FlashbackPlaybackController.Commands.cs`, `FlashbackPlaybackController.CommandQueue.cs`, `FlashbackPlaybackController.CommandCoalescing.cs`, `FlashbackPlaybackController.CommandTelemetry.cs`, `FlashbackPlaybackController.ThreadLoop.cs`, `FlashbackPlaybackController.ThreadSeekCommands.cs`, `FlashbackPlaybackController.ThreadSeekScrubCommands.cs`, `FlashbackPlaybackController.ThreadCommands.cs`, `FlashbackPlaybackController.ThreadLifecycle.cs`, `FlashbackPlaybackController.ThreadCleanup.cs`, `FlashbackPlaybackController.AudioRouting.cs`, `FlashbackPlaybackController.AudioPrebuffer.cs`, `FlashbackPlaybackController.AudioMasterPacing.cs`, `FlashbackPlaybackController.PreviewFrames.cs`, `FlashbackPlaybackController.SeekDisplay.cs`, `FlashbackPlaybackController.PlaybackLoop.cs`, `FlashbackPlaybackController.PlaybackSegmentEdges.cs`, `FlashbackPlaybackController.PlaybackTiming.cs`, `FlashbackPlaybackController.Markers.cs`, `FlashbackPlaybackController.PositionMapping.cs`, `FlashbackPlaybackController.Metrics.cs`, `FlashbackPlaybackController.MetricsCollection.cs`, `FlashbackEncoderSink.cs`, `FlashbackEncoderSink.Startup.cs`, `FlashbackEncoderSink.EncodingLoop.cs`, `FlashbackEncoderSink.PacketDrain.cs`, `FlashbackEncoderSink.SegmentRotation.cs`, `FlashbackEncoderSink.ForceRotate.cs`, `FlashbackEncoderSink.ForceRotateExecution.cs`, `FlashbackEncoderSink.Inputs.cs`, `FlashbackEncoderSink.Lifetime.cs`, `FlashbackEncoderSink.Options.cs`, `FlashbackEncoderSink.VideoQueueSubmission.cs`, `FlashbackEncoderSink.Queues.cs`, `FlashbackEncoderSink.QueueCleanup.cs`, `FlashbackEncoderSink.Recording.cs`, `FlashbackEncoderSink.RuntimeState.cs`, `FlashbackBufferManager.cs`, `FlashbackBufferManager.SegmentMutation.cs`, `FlashbackBufferManager.Lifecycle.cs`, `FlashbackBufferManager.SegmentQueries.cs`, `FlashbackBufferManager.Math.cs`, `FlashbackBufferManager.Retention.cs`, `FlashbackBufferManager.EvictionPause.cs`, `FlashbackStartupCacheCleanup.cs`, `FlashbackStartupSessionCacheBudget.cs`, `FlashbackExporter.cs`, `FlashbackExporter.SingleFile.cs`, `FlashbackExporter.SingleFilePacketWriting.cs`, `FlashbackExporter.Segments.cs`, `FlashbackExporter.SegmentPacketWriting.cs`, `FlashbackExporter.SegmentRangeProjection.cs`, `FlashbackExporter.SegmentSkipTracking.cs`, `FlashbackExporter.SegmentTemplate.cs`, `FlashbackExporter.SegmentValidation.cs`, `FlashbackExporter.TempFiles.cs`, `FlashbackExporter.Requests.cs`, `FlashbackExporter.Lifetime.cs`, `FlashbackExporter.Execution.cs`, `FlashbackExporter.PacketTiming.cs`, `FlashbackExporter.PacketBuffers.cs`, `FlashbackExporter.Streams.cs`, `FlashbackExporter.StreamTemplates.cs`, `FlashbackExporter.OutputFiles.cs`, `FlashbackExporter.Progress.cs`, `FlashbackExporter.WriterPacing.cs`, `FlashbackExporter.ExportLock.cs`, `FlashbackExporter.OutputValidation.cs`, `FlashbackExporter.PathValidation.cs`, `FlashbackExporter.SegmentSelection.cs`, `FlashbackExporter.NativeState.cs`, `FlashbackExporter.Cancellation.cs`, `FlashbackExporter.LibAvErrors.cs`, `FlashbackExporter.TimeMath.cs` | decoder lifecycle/open/dispose shell, D3D11VA decoder discovery/initialization, video frame output/conversion, video codec setup and software output-buffer allocation, decoder audio packet delivery and bounded audio output, decoder timestamp/seek conversion helpers, decoder keyframe/exact seek control flow, decoder video frame receive and packet feed loop, decoder stream/frame validation helpers, decoder file-close native cleanup and held-frame release, decoder phase timing and FFmpeg error formatting, decoder state guards, decoded video/audio output DTOs, playback core, decoder file open/cleanup, active fMP4 reopen and seek recovery, component lifecycle and dispose, public playback command facade, command queue/drop policy, seek/scrub coalescing, command readiness/telemetry bookkeeping, playback thread command dispatch loop, playback-thread seek command execution, playback-thread scrub command execution, playback-thread play/pause/go-live/nudge command execution, playback thread lifecycle, playback thread cleanup, audio callback/routing/render helpers, audio prebuffer/rewind, audio-master pacing/fallbacks, decoded frame submission/ownership, seek/scrub frame display, continuous playback loop, segment-edge switching/reopen/write-head handling, timing/cadence policy, marker owner, position/file-PTS mapping, public metrics surface, metric collection/reset, encoder core state/helpers, encoder startup/queue initialization, encode loop orchestration, packet drains and progress, segment rotation/failure recovery, export force-rotation handshake, encoding-thread force-rotate execution, producer/callback input surface, stop/dispose lifecycle, encoder options/packet helpers, encoder video/GPU queue admission, shared encoder queue helpers, queued-buffer cleanup, retroactive recording lifecycle, public counters/status, buffer live counters and byte/PTS updates, buffer segment mutation surface, buffer initialize/dispose/recovery-preserve lifecycle, buffer segment query/projection helpers, buffer math and saturated accounting helpers, buffer retention/purge/eviction, buffer eviction-pause state and recording PTS range capture, startup stale-root/session cleanup and free-space probing, startup session cache budget enforcement, shared exporter native state, single-file export shell, single-file packet remux core, multi-segment export lifecycle, multi-segment packet writing/remux core, segment export range/window projection, segment template setup, segment validation policy, temp-file cleanup, export request routing, exporter disposal, export execution scheduling, packet timestamp helpers, packet buffer lifetime helpers, stream/context setup, stream-template/layout compatibility, final output replacement, export progress helpers, export writer pacing/throttle helpers, export lock handling, export failure results, output validation, path validation, segment selection, native cleanup, linked cancellation, FFmpeg error formatting, export time math |
| Preview rendering | `D3D11PreviewRenderer.cs`, `D3D11PreviewRenderer.Configuration.cs`, `D3D11PreviewRenderer.NativeInterop.cs`, `D3D11PreviewRenderer.Lifecycle.cs`, `D3D11PreviewRenderer.RenderThread.cs`, `D3D11PreviewRenderer.SharedDevice.cs`, `D3D11PreviewRenderer.FrameTypes.cs`, `D3D11PreviewRenderer.FrameOwnership.cs`, `D3D11PreviewRenderer.DxgiFrameStatistics.cs`, `D3D11PreviewRenderer.Submission.cs`, `D3D11PreviewRenderer.Rendering.cs`, `D3D11PreviewRenderer.Present.cs`, `D3D11PreviewRenderer.ShaderRendering.cs`, `D3D11PreviewRenderer.DeviceLost.cs`, `D3D11PreviewRenderer.DeviceInitialization.cs`, `D3D11PreviewRenderer.FrameUpload.cs`, `D3D11PreviewRenderer.FrameLatency.cs`, `D3D11PreviewRenderer.Viewport.cs`, `D3D11PreviewRenderer.Resources.cs`, `D3D11PreviewRenderer.InputResources.cs`, `D3D11PreviewRenderer.PanelBinding.cs`, `D3D11PreviewRenderer.PendingFrames.cs`, `D3D11PreviewRenderer.Metrics.cs`, `D3D11PreviewRenderer.MetricsTracking.cs`, `D3D11PreviewRenderer.SlowFrameDiagnostics.cs`, `D3D11PreviewRenderer.ScreenshotCapture.cs`, `D3D11PreviewRenderer.ScreenshotEncoding.cs`, `D3D11PreviewRenderer.ShaderSources.cs` | renderer host/public state, env-tuned runtime configuration, native panel/shader/DWM interop, render-thread lifecycle and disposal, render-thread loop/orchestration plus shared-device reset consumption, composition-transform wake handling, pending-frame consumption, and render-thread failure telemetry, shared-device COM reference handoff, reinit retirement, reset scheduling, and initialization bridge, pending-frame and metrics model types, submitted/rendered/dropped frame ownership telemetry, DXGI frame statistics and display-clock projection, public frame submission entry points, VideoProcessor path, shared present/accounting transaction, NV12/HDR shader draw paths, device-lost classification and recovery, D3D device/swap-chain initialization, raw-frame and external-texture upload helpers, frame-latency waitable swap-chain setup/waits, viewport and letterbox helpers, D3D pipeline/view/disposal resources, raw/HDR input texture resources, swap-chain panel binding and composition transforms, pending-frame queue/signaling, read-only present/latency metrics, render-loop metric window tracking/reset, slow-frame diagnostic ring and reason projection, screenshot capture GPU/readback flow, screenshot BMP/error/buffer encoding, shader source, timing models |
| UI shell | `MainWindow.*.cs` XAML adapters plus `Sussudio/Controllers/*Controller.cs` shell controllers | keep shell adapters thin and start new UI behavior in named controllers/policies with ownership tests |
| Presentation | `MainViewModel.*.cs` facade/feature partial family plus focused `Sussudio/ViewModels` policy/presentation helpers | keep the root facade stable while moving pure feature state, policy, and presentation logic into named owners |

## Automation

Primary owner: `Sussudio.Automation.Contracts/`

Entry points:

- `AutomationCommandKind.cs` owns numeric command IDs. Append only; never
  renumber or reuse values.
- `AutomationCommandCatalog.cs` owns command metadata table, payload shape,
  readiness gating, timeout policy, CLI help, and MCP descriptions.
- `AutomationCommandCatalog.Manifest.cs` owns manifest DTO projection and stable
  manifest JSON serialization.
- `AutomationCommandCatalog.PathValidation.cs` owns path-policy types and path
  validation for path-bearing automation commands.
- `AutomationPipeProtocol.cs` owns pipe names, auth env var, manifest revision,
  command resolution, and request envelope shape.
- `AutomationPipeSecurityPolicy.cs` owns the fallback-security predicate shared
  by app and tests.
- `tests/Sussudio.Tests/AutomationToolContracts.CommandKinds.Tests.cs` owns the
  golden numeric command-ID table. Routing tests should assert captured
  `request.command` values through `AssertAutomationCommandId`, not raw numbers
  or direct golden-table lookups.

Do not reintroduce linked source for these files from `tools/Common`. Consumers
should reference `Sussudio.Automation.Contracts`.
`tools/Common` is the shared helper module for clients, formatters, diagnostic
sessions, and probes; it should not own command IDs, catalog metadata, protocol
constants, or pipe security policy.

Fast checks:

```powershell
dotnet build Sussudio.slnx -p:Platform=x64 --no-restore
dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore
```

Automation diagnostics ownership:

- `Sussudio/Services/Automation/AutomationCommandDispatcher.cs` owns the
  command envelope, manifest/auth/readiness gates, trivial-handler dispatch, and
  error shaping.
- `Sussudio/Services/Automation/AutomationCommandDispatcher.AudioControlCommands.cs`
  owns device-audio mode, analog audio gain, and microphone-enable command
  bodies behind the custom command router.
- `Sussudio/Services/Automation/AutomationCommandDispatcher.CaptureControlCommands.cs`
  owns MJPEG decoder count, output-path, and recording-enable command bodies,
  including recording-response snapshot refresh, behind the custom command
  router.
- `Sussudio/Services/Automation/AutomationCommandDispatcher.CustomCommands.cs`
  owns the custom automation command router for multi-field payloads, special
  response shapes, capture routing, and domain command handoff.
- `Sussudio/Services/Automation/AutomationCommandDispatcher.UiSettingsCommands.cs`
  owns UI/settings automation command application, including show-all capture
  options, preview volume, stats visibility, settings visibility, frame-time
  overlay visibility, Flashback timeline visibility, and stats-section
  expand/collapse response text.
- `Sussudio/Services/Automation/AutomationCommandDispatcher.DiagnosticCommands.cs`
  owns diagnostic readback command bodies for recent events, performance
  timeline, and audio ramp traces behind the custom command router.
- `Sussudio/Services/Automation/AutomationCommandDispatcher.IntrospectionCommands.cs`
  owns read-only snapshot and manifest response command bodies behind the
  custom command router.
- `Sussudio/Services/Automation/AutomationCommandDispatcher.DeviceCommands.cs`
  owns device refresh, capture-device selection, audio-input selection, and
  capture-options readback command bodies behind the custom command router.
- `Sussudio/Services/Automation/AutomationCommandDispatcher.FlashbackCommands.cs`
  owns Flashback action/export/segment/restart/enable command bodies behind the
  custom command router.
- `Sussudio/Services/Automation/AutomationCommandDispatcher.VerificationCommands.cs`
  owns file and last-recording verification command bodies behind the custom
  command router.
- `Sussudio/Services/Automation/AutomationCommandDispatcher.VisualCaptureCommands.cs`
  owns video-source probe, preview-color probe, preview-frame capture, window
  screenshot capture, default capture output paths, and capture response
  status shaping behind the custom command router.
- `Sussudio/Services/Automation/AutomationCommandDispatcher.TrivialHandlers.cs`
  owns the table of simple one-property capture and pipeline commands that
  delegate straight to the automation view-model port.
- `Sussudio/Services/Automation/AutomationCommandDispatcher.Authorization.cs`
  owns auth-token fallback lookup, constant-time token comparison, and auth
  failure logging.
- `Sussudio/Services/Automation/AutomationCommandDispatcher.CommandParsing.cs`
  owns command metadata lookups, path validation forwarding, and enum payload
  parsing.
- `Sussudio/Services/Automation/AutomationCommandDispatcher.Responses.cs`
  owns response shaping, acknowledged responses, and Flashback rejection
  diagnostics.
- `Sussudio/Services/Automation/AutomationCommandDispatcher.WindowActions.cs`
  owns window automation action execution.
- `Sussudio/Services/Automation/AutomationCommandDispatcher.WindowCommands.cs`
  owns full-screen, recordings-folder, arm-close, and window-action command
  bodies, including close-arm gating, behind the custom command router.
- `Sussudio/Services/Automation/AutomationCommandDispatcher.WaitConditions.cs`
  owns WaitForCondition command response shaping, wait-condition polling, and
  snapshot predicates.
- `Sussudio/Services/Automation/AutomationCommandDispatcher.Assertions.cs`
  owns AssertSnapshot command response shaping, payload parsing, and snapshot
  comparison helpers.
- `Sussudio/Services/Automation/AutomationCommandDispatcher.Payload.cs` owns
  JSON payload extraction helpers for dispatcher command bodies.
- `Sussudio/Services/Automation/AutomationCommandHandler.cs` owns the shared
  trivial-handler wrapper used by simple one-property automation commands,
  including the payload field name/type metadata checked against the shared
  automation command catalog.
- `Sussudio/Services/Automation/NamedPipeAutomationServer.cs` owns automation
  pipe constructor/configuration state.
- `Sussudio/Services/Automation/NamedPipeAutomationServer.Lifecycle.cs` owns
  server start/stop/dispose and the accept loop.
- `Sussudio/Services/Automation/NamedPipeAutomationServer.Connections.cs` owns
  per-connection JSON framing, request timeouts, dispatch observation, and
  response writing.
- `Sussudio/Services/Automation/NamedPipeAutomationServer.Security.cs` owns
  Windows pipe security descriptor setup, fallback policy, P/Invoke, and secure
  stream creation.
- `Sussudio/Services/Automation/NamedPipeAutomationServer.Responses.cs` owns
  error/timeout responses and fallback trace logging.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.cs` owns polling,
  field/constructor state, and timeline flow. `AutomationDiagnosticsHub.Counters.cs`
  owns recent-counter baseline state and delta updates used by diagnostics evaluation.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.Alerts.cs` owns alert
  rule evaluation, active-alert transitions, and Flashback alert group routing.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.SignalAlerts.cs` owns
  signal alert orchestration plus preview, capture, audio, and recording signal
  alert rules.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.FlashbackRecordingAlerts.cs`
  owns Flashback recording alert orchestration, shared condition setup, export
  progress and force-rotation gap alerts, temp-cache pressure alerts, encoder
  failure alerts, and recording path degradation alerts.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.FlashbackPlaybackAlerts.cs`
  owns Flashback playback alert orchestration, playback command queue and
  command failure alerts, playback performance orchestration, audio-master
  fallback and audio-queue backlog alerts, and frame-submission failure alerts.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.FlashbackPlaybackPerformanceAlerts.Cadence.cs`
  owns Flashback playback target-rate, present-cadence, slow-playback, and
  frametime alert rules.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.DiagnosticEvents.cs`
  owns diagnostics event publication, event throttling, Flashback export
  completion events, and recent event storage.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.DiagnosticEvaluation.cs`
  owns diagnostic verdict orchestration and final healthy/mixed fallback.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.DiagnosticEvaluationFlashback.cs`
  owns Flashback-specific diagnostic verdict ordering.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.DiagnosticEvaluationFlashback.Storage.cs`
  owns Flashback storage pressure diagnostic verdicts.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.DiagnosticEvaluationFlashback.Recording.cs`
  owns Flashback recording diagnostic verdict ordering plus encoder failure,
  export-rotation gap, backend staleness, and recording degradation verdicts.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.DiagnosticEvaluationFlashback.RecordingConditions.cs`
  owns Flashback recording diagnostic condition assembly.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.DiagnosticEvaluationFlashback.Export.cs`
  owns active and stalled Flashback export diagnostic verdicts.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.DiagnosticEvaluationFlashback.Playback.cs`
  owns Flashback playback command, performance, frametime, and submission
  diagnostic verdicts.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.DiagnosticEvaluationRealtime.cs`
  owns realtime diagnostic verdict ordering.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.DiagnosticEvaluationRealtime.State.cs`
  owns idle and warmup diagnostic verdicts.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.DiagnosticEvaluationRealtime.Recording.cs`
  owns recording integrity and audio integrity diagnostic verdicts.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.DiagnosticEvaluationRealtime.Source.cs`
  owns source/capture cadence diagnostic verdicts.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.DiagnosticEvaluationRealtime.Mjpeg.cs`
  owns MJPEG duplicate source-signal and decode/reorder diagnostic verdicts.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.DiagnosticEvaluationRealtime.Preview.cs`
  owns realtime preview diagnostic verdict ordering plus the renderer pacing
  verdict.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.DiagnosticEvaluationRealtime.PreviewScheduler.cs`
  owns preview scheduler diagnostic verdicts.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.DiagnosticEvaluationRealtime.PreviewPresent.cs`
  owns present/display cadence and preview display 1% low diagnostic verdicts.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.DiagnosticEvaluationLanes.cs`
  owns diagnostic lane text orchestration and lane DTOs used by diagnostic
  verdicts.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.DiagnosticEvaluationLanes.Realtime.cs`
  owns source, MJPEG decode/source-signal, preview scheduler/render/present/
  visual, recording, and audio diagnostic lane text formatting.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.DiagnosticEvaluationLanes.Flashback.cs`
  owns Flashback recording, export, temp-cache, playback-command, and playback
  performance diagnostic lane text formatting.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.Evaluation.cs` owns
  performance scoring.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.EvaluationPolicy.cs`
  owns shared alert-detail formatting and health classifiers used by alerts
  and diagnostic evaluation.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.Hdr.cs` owns HDR truth
  classification, preview HDR/tone-map state projection, and HDR pixel-format
  helpers used by automation snapshots.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.Lifecycle.cs` owns
  diagnostics hub start/stop/dispose behavior and the polling loop.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.OutputFiles.cs` owns
  cached last-output file existence and size probing.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.PreviewPacing.cs` owns
  automation snapshot input projection for preview pacing stage classification.
- `Sussudio/Services/Automation/PreviewPacingClassificationModels.cs` owns the
  preview pacing classifier DTOs; `PreviewPacingSlowStageClassifier.cs` owns
  pure slow-stage classification ordering and non-D3D lane policy; and
  `PreviewPacingSlowStageClassifier.D3D.cs` owns D3D stage dominance policy.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.ProcessMetrics.cs`
  owns process CPU, memory, GC, and thread-pool sampling.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.Snapshots.cs` owns
  snapshot refresh and read-only snapshot access.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs`
  owns the `BuildAutomationSnapshot` shell and dispatch into projection
  composition/flattening.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Composition.cs`
  owns projection-set composition from runtime/view-model snapshots and
  diagnostic classifiers.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.cs`
  owns the final `AutomationSnapshot` DTO initializer that flattens named
  projection records into the automation wire snapshot.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.SnapshotStatus.cs`
  owns timestamp, view-model lifecycle/audio flags, verification-in-progress,
  session state, and status-text projection consumed by `AutomationSnapshot`.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.SnapshotEvaluation.cs`
  owns performance score, diagnostic lane, preview pacing classifier, and
  performance threshold projection consumed by `AutomationSnapshot`.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Audio.cs`
  owns view-model audio signal projection, audio drop counter projection, and
  derived real-time/file-writer drop totals, and composes ingest and WASAPI
  projection owners into the automation snapshot audio/ingest DTO fields.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.CaptureIngest.cs`
  owns capture audio/video reader, source-reader, and ingest counter projection
  consumed by `AutomationSnapshot`.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.WasapiAudio.cs`
  owns WASAPI capture/playback callback, queue, gap, glitch, and latency
  projection consumed by `AutomationSnapshot`.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.AvSync.cs`
  owns live A/V sync drift and encoder correction projection consumed by
  `AutomationSnapshot`.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.CaptureCommands.cs`
  owns capture session command queue counters, latency, last-command, and
  last-error projection inputs consumed by `AutomationSnapshot`.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.CaptureFormat.cs`
  owns requested, actual, negotiated, observed, and encoder format projection
  inputs consumed by `AutomationSnapshot`.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.CaptureTransport.cs`
  owns capture memory preference, requested/negotiated video subtype, and
  frame-ledger projection consumed by `AutomationSnapshot`.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.CaptureCadence.cs`
  owns source capture cadence projection inputs consumed by
  `AutomationSnapshot`.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.VisualCadence.cs`
  owns preview visual cadence and center-crop visual cadence projection inputs
  consumed by `AutomationSnapshot`.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Mjpeg.cs`
  owns CPU MJPEG totals, compressed queue, and failure projection inputs
  consumed by `AutomationSnapshot`.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.MjpegTiming.cs`
  owns CPU MJPEG decode, interop-copy, callback, reorder, pipeline timing,
  decoder count, and per-decoder projection inputs consumed by
  `AutomationSnapshot`.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.MjpegPreviewJitter.cs`
  owns MJPEG preview jitter queue, timing, drop, underflow, and adaptive-depth
  projection inputs consumed by `AutomationSnapshot`.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.MjpegPacketHash.cs`
  owns MJPEG packet duplicate-run / unique-frame projection inputs consumed by
  `AutomationSnapshot`.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.FlashbackExport.cs`
  owns active Flashback export progress, failure, force-rotate fallback, and
  final Flashback export last-result projection consumed by
  `AutomationSnapshot`.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.FlashbackRecording.cs`
  owns Flashback recording, buffer, backend, encoder configuration, export
  verification, codec-downgrade fallback, temp-drive, and startup cache
  projection consumed by
  `AutomationSnapshot`.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.FlashbackRecordingQueues.cs`
  owns Flashback video, GPU, and audio queue/backpressure projection consumed
  by `AutomationSnapshot`.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.FlashbackPlayback.cs`
  owns Flashback playback state, frame cadence metrics, audio-master
  delay/fallback, seek-cap/decode timing, and playback command queue projection
  consumed by `AutomationSnapshot`.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.PreviewD3D.cs`
  owns D3D preview swap-chain, renderer state, submitted/rendered/dropped frame
  flow, waitable frame-latency, and frame-statistics projection consumed by
  `AutomationSnapshot`.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.PreviewD3DCpuTiming.cs`
  owns D3D CPU upload/render/present/total-frame timing and pipeline latency
  projection consumed by `AutomationSnapshot`.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.PreviewRuntime.cs`
  owns preview frame counters, estimated pipeline latency, display-cadence,
  startup/readiness, GPU playback state, preview HDR state, renderer mode, and
  preview color-context projection consumed by `AutomationSnapshot`.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.ProcessResources.cs`
  owns process memory, CPU, GC, and thread-pool projection consumed by
  `AutomationSnapshot`.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.RecordingIntegrity.cs`
  owns recording-integrity projection inputs consumed by `AutomationSnapshot`.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.RecordingPipeline.cs`
  owns encoder queue ages, conversion queue depths, and recording
  video/GPU/CUDA health inputs consumed by `AutomationSnapshot`.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.RecordingOutput.cs`
  owns recording backend/audio-path/mux-result projection, recording UI output
  text, accumulated recording bytes, file-growth state, last finalized output
  metadata, and last verification result projection consumed by
  `AutomationSnapshot`.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.SourceSignal.cs`
  owns detected source frame-rate fallback, source dimensions/HDR, and raw
  source signal metadata projection consumed by `AutomationSnapshot`.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.SourceTelemetry.cs`
  owns source telemetry fallback policy, age calculation, and source-target
  summary inputs consumed by `AutomationSnapshot`.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.UserSettings.cs`
  owns selected device, selected capture/recording options, preview volume, and
  stats visibility projection consumed by `AutomationSnapshot`.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.HdrPipeline.cs`
  owns HDR availability/request state, runtime/readiness fallback, HDR
  warmup/downgrade, pipeline parity, telemetry-alignment, and HDR truth verdict
  projection consumed by `AutomationSnapshot`.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotState.cs` owns
  stateful snapshot bookkeeping for audio mute suspicion and recording file
  growth tracking.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.Timeline.cs` owns
  performance-timeline ring reads and append mechanics.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.TimelineProjection.cs`
  owns `AutomationSnapshot` to `PerformanceTimelineEntry` projection.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.Verification.cs` owns
  recording/file verification commands, automatic post-recording verification
  scheduling, recording-start verification reset, and verification-profile
  adaptation.

## Capture Runtime

Primary current owner: `Sussudio/Services/Capture/`

Important entry points:

- `CaptureSessionCoordinator.cs` owns construction and shared state fields.
- `CaptureSessionCoordinator.Commands.cs` owns the public non-Flashback
  lifecycle/audio command facade into the serialized worker.
- `CaptureSessionCoordinator.Models.cs` owns command enums, queue receipts,
  session snapshots, and Flashback playback/buffer status projections.
- `CaptureSessionCoordinator.Queue.cs` owns work-item creation, command
  enqueueing, worker-loop execution, command coalescing, cancellation/failure
  accounting, and pending-command counters.
- `CaptureSessionCoordinator.Snapshot.cs` owns queue/session snapshot
  projection, last-command state, pending-command age bookkeeping, and queue
  latency accounting.
- `CaptureSessionCoordinator.Disposal.cs` owns dispose/drain/cancel lifecycle
  for the worker queue and cancellation token source.
- `CaptureSessionCoordinator.Flashback.cs` owns queued Flashback mutations,
  read-only Flashback status/projections, export forwarding, and active
  playback-controller readiness checks.
- `CaptureSessionTransitionPolicy.cs` owns pure transition legality and
  steady-state resolution for `CaptureService`.
- `DeviceService.cs` owns capture/audio device enumeration orchestration and
  discovery summary state.
- `DeviceService.FormatCache.cs` owns persisted format-cache DTOs and
  load/save/delete helpers.
- `DeviceService.FormatProbe.cs` owns inline/background Media Foundation format
  probing and pixel-format/frame-rate normalization.
- `DeviceService.Scoring.cs` owns device priority and capability scoring.
- `DeviceService.AudioAssociation.cs` owns matching capture devices to audio
  endpoints.
- `DeviceService.NativeXu.cs` owns native XU interface path resolution for
  supported devices.
- `Sussudio/Services/Capture/DeviceDiscovery/MfDeviceEnumerator.cs` owns shared Media Foundation constants, GUIDs, and
  P/Invoke declarations.
- `Sussudio/Services/Capture/DeviceDiscovery/MfDeviceEnumerator.VideoDevices.cs` owns native MF video-device enumeration.
- `Sussudio/Services/Capture/DeviceDiscovery/MfDeviceEnumerator.AudioEndpoints.cs` owns WASAPI capture endpoint
  enumeration and friendly-name reads.
- `Sussudio/Services/Capture/DeviceDiscovery/MfDeviceEnumerator.FormatProbe.cs` owns native video format probing, MF
  source fallback activation, and subtype/FourCC naming.
- `CaptureService.cs` owns shared service state, construction, and the
  event/property surface. It should not receive unrelated UI, Flashback,
  recording lifecycle, or diagnostics behavior.
- `CaptureService.Initialization.cs` owns the public initialization transition,
  initial selected device/settings capture, negotiated-format seeding, the
  initial observed-pixel telemetry reset call, fallback source telemetry,
  telemetry refresh, NTSC frame-rate correction, and initialized status event.
- `CaptureService.Audio.cs` owns preview volume/mute plus WASAPI audio-level
  and capture-failure event projection.
- `CaptureService.AudioPreviewLifecycle.cs` owns audio-preview start/stop
  lifecycle, late WASAPI capture startup, playback start, preview rollback, and
  optional capture teardown.
- `CaptureService.AudioInputSwitching.cs` owns live audio input switching, the
  committed old/new capture handoff, Flashback audio attach, and deferred
  cancellation checks.
- `CaptureService.MicrophoneMonitor.cs` owns microphone monitoring, mic-level
  event projection, preview-time mic writer attachment, post-recording mic
  monitor restart, and mic capture cleanup.
- `CaptureService.WasapiPlayback.cs` owns WASAPI playback startup/shutdown,
  audio-monitor attach/detach order, and playback best-effort cleanup helpers.
- `CaptureService.Cleanup.cs` owns explicit cleanup transitions, app shutdown
  teardown, Flashback segment preservation when cleanup finalization fails, and
  the final cleanup session-state reset.
- `CaptureService.Coordination.cs` owns transition serialization, steady-state
  resolution, normal `_sessionState` transition writes, and
  initialization/disposal guards.
- `CaptureService.DisposalLifecycle.cs` owns disposal-triggered cleanup and
  final disposed-state writes.
- `CaptureService.ResourceRelease.cs` owns best-effort semaphore
  release/disposal, coordination-lock disposal, Flashback backend/export
  held-lock release helpers, and Flashback eviction resume warnings.
- `CaptureService.DeferredCleanup.cs` owns Flashback artifact-cleanup adapter
  handoff, export-lock wait/release delegation, deferred unified-video cleanup
  after drains, and the pending LibAv drain reentry guard.
- `CaptureService.Failures.cs` owns fatal capture/recording/Flashback backend
  failure callbacks plus last-failure telemetry state fields, lock, mutation
  helpers, clear helpers, and snapshot reads.
- `CaptureService.FailureCleanup.cs` owns fatal capture cleanup launch,
  generation-stale guards, and fatal cleanup `_sessionState` writes into
  cleaning-up/faulted states.
- `CaptureService.FlashbackBackendFailureCleanup.cs` owns Flashback backend
  cleanup launch, GPU device-lost classification, recovery segment preservation,
  and generation-stale guards. It must not write `_sessionState`.
- `CaptureService.FlashbackControls.cs` owns Flashback public state, segment
  access, enable/disable mutations, restart entry points, and committed restart
  orchestration after preview backend teardown.
- `CaptureService.FlashbackBufferSettings.cs` owns Flashback buffer/GPU
  settings updates and live playback-controller GPU decode propagation.
- `CaptureService.FlashbackEncoderSettings.cs` owns active encoding-setting
  application, recording-format changes, encoder setting cycles, and rollback
  after failed Flashback buffer cycles.
- `CaptureService.FlashbackAudioInputs.cs` owns WASAPI and microphone input
  restoration for Flashback preview/recording backends.
- `CaptureService.FlashbackPreviewBackend.cs` owns Flashback preview backend
  transition coordination: AV1 encoder support probing, video/audio readiness
  waiting, resource-owner request construction, and deferred cleanup handoff.
  Startup construction, install, playback initialization, producer attachment,
  and startup rollback live in `FlashbackBackendResources.Startup.cs`; teardown
  mechanics and backend artifact cleanup live in
  `FlashbackBackendResources.ArtifactCleanup.cs`.
- `CaptureService.FlashbackPreviewBackendDisposal.cs` owns Flashback preview
  backend teardown lock ordering, purge-policy resolution, service callback
  binding, and cancellation-token choice before delegating resource mechanics.
- `CaptureService.FlashbackBufferCycle.cs` owns buffer-cycle transition
  coordination: backend/export lock ordering, purge-preserve decisions, and
  full rebuild fallbacks. Sink-only resource mechanics live in
  `FlashbackBackendResources.BufferCycle.cs`.
- `CaptureService.FlashbackExportDiagnostics.cs` owns Flashback export
  diagnostic state, progress forwarding, rejection records, and force-rotate
  fallback counters.
- `CaptureService.FlashbackExportFailureClassification.cs` owns the export
  failure-kind taxonomy shared by capture diagnostics and automation responses.
- `CaptureService.FlashbackExportOperations.cs` owns Flashback export entry
  points, backend snapshotting, and range resolution before the shared export
  core runs.
- `CaptureService.FlashbackExportCore.cs` owns the shared export pipeline:
  eviction pause, force-rotate, exporter request construction, diagnostics
  completion, and cleanup.
- `CaptureService.FlashbackExportPlanning.cs` owns segment metadata mapping,
  live-export throttle policy, range clamps, and PTS offset helpers.
- `CaptureService.FlashbackRecording.cs` owns Flashback recording backend
  ownership checks, session context construction, frame-rate rational policy,
  codec/HDR guardrails, and recording topology validation.
- `CaptureService.HealthSnapshots.cs` samples health snapshot field groups and
  final service-state/scalar handoff values consumed by diagnostics and
  automation health checks.
- `CaptureService.HealthSnapshotAssembler.cs` owns final
  pure `CaptureHealthSnapshot` DTO construction from captured fields.
- `CaptureService.HealthSnapshotCaptureCadence.cs` owns source-reader capture
  cadence field projection for health snapshots.
- `CaptureService.HealthSnapshotFlashbackBuffer.cs` owns Flashback buffer,
  startup-cache, backend-staleness reason policy, and encoder summary field
  projection for health snapshots.
- `CaptureService.HealthSnapshotFlashbackQueues.cs` owns Flashback live queue,
  force-rotate, backpressure, and GPU queue field projection for health
  snapshots.
- `CaptureService.HealthSnapshotMjpeg.cs` owns MJPEG timing, jitter,
  packet-hash, visual-cadence, and per-decoder field projection for health
  snapshots.
- `CaptureService.HealthSnapshotSourceTelemetry.cs` owns source telemetry,
  backend, suppression, and circuit-state field projection for health
  snapshots.
- `CaptureService.HealthSnapshotFlashbackExport.cs` owns the locked Flashback
  export diagnostic field copy, elapsed/progress-age/file-length helpers, and
  derived progress/throughput projection used by health snapshots.
- `CaptureService.HealthSnapshotFlashbackPlayback.cs` owns Flashback playback
  health snapshot orchestration, state/frame/segment/PTS/seek-cap/A/V drift
  projection, playback cadence metrics, decode timing and max-phase metrics,
  audio-master pacing/fallback fields, and command-queue telemetry fields.
- `CaptureService.HealthSnapshotRecording.cs` owns recording health snapshot
  orchestration, LibAv-only CUDA queue projection, and the
  `RecordingHealthSnapshotFields` handoff.
- `CaptureService.HealthSnapshotRecordingActiveBackend.cs` owns active
  recording backend selection, LibAv-vs-Flashback sink fallback, failure
  precedence, and backend-specific queue/counter normalization for health
  snapshots.
- `CaptureService.PreviewStart.cs` owns video-preview start transition,
  retained-backend fast-path reattachment, preview-start rollback, and fresh
  preview backend startup ordering.
- `CaptureService.PreviewAudioGraph.cs` owns preview WASAPI capture startup,
  video-only audio fallback logging, preview playback attach, preview-time
  microphone monitor startup, and partially-started audio rollback.
- `CaptureService.PreviewStop.cs` owns video-preview stop transitions,
  keep-pipeline-alive detach semantics, and stopped-state/telemetry commit.
- `CaptureService.PreviewReuse.cs` owns retained video/Flashback backend reuse
  checks and capture-settings cloning.
- `CaptureService.PreviewDisposal.cs` owns preview pipeline disposal ordering,
  deferred video cleanup, Flashback backend disposal, WASAPI disposal, and
  microphone cleanup.
- `CaptureService.PreviewPipeline.cs` owns preview frame sink attachment,
  shared D3D preview-device handoff, negotiated video getters, and cached MJPEG
  pipeline timing details.
- `CaptureService.Probes.cs` owns read-only automation probes and preview-frame
  capture waits.
- `CaptureService.RecordingIntegrity.cs` owns active recording integrity backend
  resolution; `.Models.cs` owns private counter DTOs; `.Summary.cs` owns
  status/reason classification; `.Counters.cs` owns video/backend counter
  capture and baseline deltas; `.Audio.cs` owns audio counter capture and
  baseline deltas; `.Logging.cs` owns the structured `RECORDING_INTEGRITY` log
  line.
- `CaptureService.RecordingLifecycle.cs` owns public recording start
  transition routing and delegates failed-start cleanup to the rollback owner;
  `CaptureService.RecordingStartState.cs` owns the private rollback-state holder;
  `CaptureService.RecordingStartFlashback.cs` owns Flashback recording backend
  startup and fast-path reuse; `CaptureService.RecordingStartLibAv.cs` owns
  standard LibAv recording startup and
  `CaptureService.RecordingStartLibAv.AudioInputs.cs` owns standard LibAv
  recording audio-input startup, WASAPI sink attachment, preview playback
  preservation, and recording microphone capture wiring.
  `CaptureService.RecordingStopLifecycle.cs`
  owns normal and emergency recording stop transition routing plus the
  stop/finalize router for active Flashback and LibAv backends.
- `CaptureService.RecordingFinalizeFlashbackBackend.cs` owns active Flashback
  recording backend finalization: live-edge finalize/export handoff,
  finalize-in-progress choreography, Flashback recording integrity summaries,
  cancellation-result classification, outcome publication, and Flashback-
  specific microphone monitor restart.
- `CaptureService.RecordingFinalizeFlashbackBackendReconcile.cs` owns
  post-finalize Flashback backend reconciliation: failed-finalize recovery
  preservation, deferred settings apply, buffer cycling, buffer-cycle failure
  classification, recovery preservation, and backend cleanup launch.
- `CaptureService.RecordingFinalizeLibAvBackend.cs` owns standard LibAv
  recording finalization: unified-video recording stop and optional teardown,
  WASAPI recording detach/disposal, LibAv sink normal/emergency stop and drain
  tracking, encoder/runtime and recording-integrity summaries, and the visible
  final outcome publication before delayed cancellation throws.
- `CaptureService.RecordingFinalizeLibAvPreviewRestore.cs` owns standard LibAv
  live-preview restoration after recording: pending Flashback enable-after-
  recording detection, guarded Flashback preview backend restore, failed-restore
  rollback and purge, standard post-recording microphone monitor restart, and
  the `FLASHBACK_ENABLE_AFTER_RECORDING_*` breadcrumbs.
- `CaptureService.RecordingOutcomeState.cs` owns publication of the last
  recording output path, finalize status, finalize timestamp, and preserved
  artifact fields for both recording-start and recording-finalize outcomes.
- `CaptureService.RecordingFinalizeFlashback.cs` owns Flashback recording
  export finalization and cancellation-result classification.
- `CaptureService.RecordingFinalizeFlashbackBoundary.cs` owns Flashback
  recording live-edge boundary snapshots, including idempotent
  `EndFlashbackRecordingAccounting()` calls, source-frame counters, recording
  integrity counters, and audio integrity counters.
- `CaptureService.RecordingRollback.cs` owns transient backend teardown after
  recording-start failures, including the failure log/last-failure update,
  Flashback rollback accounting, rollback artifact cleanup, best-effort sink,
  WASAPI, unified-video, and deferred LibAv drain cleanup.
- `CaptureService.RuntimeSnapshots.cs` samples runtime snapshot inputs consumed by UI,
  automation, and verification, then delegates final DTO construction.
- `CaptureService.RuntimeSnapshotAssembler.cs` owns final `CaptureRuntimeSnapshot` DTO construction
  from already-sampled field groups.
- `CaptureService.RuntimeSnapshotIngestAudio.cs` owns runtime snapshot projection
  for video ingest, source-reader health, WASAPI capture, and playback output
  counters.
- `CaptureService.RuntimeSnapshotReaderTransport.cs` owns runtime snapshot
  projection for requested/negotiated reader subtypes, reader transport source,
  memory preference, frame-ledger summary, and preview renderer mode.
- `CaptureService.RuntimeSnapshotHdrPipeline.cs` owns runtime HDR/encoder
  pipeline parity, downgrade reason, and encoder format projection.
- `CaptureService.RuntimeSnapshotSourceTelemetry.cs` owns runtime snapshot
  projection for source telemetry details, age, circuit state, and alignment.
- `CaptureService.RuntimeSnapshotRecordingIntegrity.cs` owns runtime snapshot
  projection for recording-integrity summary fields.
- `CaptureService.Snapshots.cs` owns the diagnostics-snapshot compatibility
  entry point plus shared tick-age snapshot helper policy.
- `CaptureService.SnapshotRecordingStats.cs` owns recording byte-count snapshot
  projection for active LibAv bytes, active Flashback buffer estimates,
  finalized-output file fallback, and failure flagging.
- `CaptureService.SnapshotRecordingFormat.cs` owns encoder codec, output pixel
  format, video profile, and requested frame-rate argument projection.
- `CaptureService.SnapshotObservedFrames.cs` owns observed frame-format
  telemetry projection from explicit counters and the private
  `ObservedFrameSnapshotFields` owner shared by runtime/health assemblers.
- `CaptureService.SnapshotAvSync.cs` owns A/V sync drift state, baseline reset,
  live source/audio drift sampling, encoder correction telemetry, and the
  health-snapshot A/V sync field projection.
- `CaptureService.SnapshotTelemetry.cs` owns source telemetry snapshot
  presentation policy, telemetry/request alignment, and HDR warmup state
  classification.
- `CaptureService.ObservedPixelTelemetry.cs` owns observed pixel-format
  normalization, reset, and explicit counter updates.
- `CaptureService.Telemetry.cs` owns source telemetry polling and fallback merge.
- `CaptureService.CaptureFormatTelemetry.cs` owns capture-format runtime
  telemetry, NTSC frame-rate correction, and frame-rate argument formatting.
- `UnifiedVideoCapture.cs` owns public control/configuration surface, capture
  fields, counters, and recording/Flashback attachment state.
- `UnifiedVideoCapture.FrameIngress.cs` owns source-reader frame arrival
  routing, MJPEG decoded-frame emission fan-out, capture-arrival ledger
  records, pixel-format observer dispatch, and fatal-error dedupe/signaling.
- `UnifiedVideoCapture.Lifecycle.cs` owns source-reader initialization,
  read-loop start/stop, preview-reinit disposal, and capture fatal-error
  callbacks.
- `UnifiedVideoCapture.MjpegPipelineLifecycle.cs` owns CPU MJPEG decode
  pipeline construction, preview jitter buffer setup/disposal, MJPEG pipeline
  stop/retention semantics, and MJPEG fatal-error callbacks.
- `UnifiedVideoCapture.SinkFanout.cs` owns recording and Flashback sink enqueue
  helpers, non-blocking queue rejection accounting, and Flashback recording
  sequence-gap accounting.
- `UnifiedVideoCapture.Metrics.cs` owns source-reader cadence forwarding, MJPEG
  pipeline/jitter/hash metrics, preview visual cadence metrics, and frame ledger
  summary projection.
- `UnifiedVideoCapture.Preview.cs` owns preview sink assignment, live-preview
  suppression drains, MJPEG decoded preview-frame routing, raw preview
  submission, and visual-cadence reset/recording helpers.

Invariants:

- Starting or stopping recording must not restart live preview unless the
  transition explicitly requires it.
- Capture lifecycle legality should be expressed in
  `CaptureSessionTransitionPolicy`, not scattered through ad hoc boolean checks.
- Mutating capture lifecycle state should go through serialized coordinator or
  transition-lock paths.
- Snapshot display state should be derived from service/runtime snapshots, not
  hand-updated independently in multiple event handlers.

## Recording

Primary current owner: `Sussudio/Services/Recording/`

Entry points:

- `LibAvEncoder.cs` owns encoder fields and stable public core state.
- `LibAvEncoder.Initialization.cs` owns FFmpeg initialization forwarding and
  encoder open/setup orchestration.
- `LibAvEncoder.Audio.cs` owns audio/microphone stream state, public status
  properties, interleaved packet writes, pending-sample flush, and accumulator
  ingress.
- `LibAvEncoder.AudioQueue.cs` owns audio sample queueing, drift-corrected
  encode chunks, planar sample copies, and prepared-frame drains.
- `LibAvEncoder.AudioSubmission.cs` owns public audio/microphone sample entry
  points, payload alignment checks, accumulator handoff, and stream-chunk
  submission.
- `LibAvEncoder.AudioInitialization.cs` owns audio and microphone AAC stream
  creation, codec opening, stream time-base setup, resampler/frame/buffer setup
  calls, and microphone-specific setup.
- `LibAvEncoder.AudioSetup.cs` owns AAC codec context configuration, resampler
  setup, frame allocation, accumulator allocation, and sample-queue allocation.
- `LibAvEncoder.HdrSideData.cs` owns HDR mastering-display and content-light
  side-data attachment for software and hardware video frames.
- `LibAvEncoder.Models.cs` owns encoder option and rotation-result DTOs.
- `LibAvEncoder.CodecPolicy.cs` owns validation, bitstream-filter selection,
  NVENC preset/split-encode mapping, frame-size math, sample-format support,
  and rational conversion helpers.
- `LibAvEncoder.AvSync.cs` owns A/V sync drift correction and diagnostics.
- `LibAvEncoder.PacketWriting.cs` owns video packet draining, bitstream-filter
  draining, timestamp rescaling, and interleaved video packet writes.
- `LibAvEncoder.FrameCopy.cs` owns packed NV12/P010 software-frame copies.
- `LibAvEncoder.Diagnostics.cs` owns open-state guards, FFmpeg error strings,
  structured libav exceptions, and D3D11 device-removed checks.
- `LibAvEncoder.VideoSetup.cs` owns video codec context setup, NVENC options,
  and video bitstream-filter setup.
- `LibAvEncoder.HardwareFrames.cs` owns D3D11/CUDA hardware frame setup,
  ArraySize=1 texture-pool creation, and hardware-frame fallback cleanup.
- `LibAvEncoder.VideoSubmission.cs` owns CPU packed frame submission, D3D11
  texture submission, CUDA frame submission, forced-keyframe handling, per-frame
  HDR side-data attachment/removal, and video packet drains.
- `LibAvEncoder.MuxerOptions.cs` owns MP4 muxer option policy for open and
  rotated outputs.
- `LibAvEncoder.OutputRotation.cs` owns output rotation, IO close/reopen,
  stream reinitialization, video bitstream-filter reset, and segment runtime
  reset.
- `LibAvEncoder.ResourceCleanup.cs` owns flush/final close, dispose, trailer
  writing, and native resource cleanup/freeing.
- `Sussudio/Services/Recording/Verification/RecordingVerifier.cs` owns strict verification orchestration and keeps the
  public verifier surface stable.
- `Sussudio/Services/Recording/Verification/RecordingVerifier.Ffprobe.cs` owns ffprobe path resolution, process specs,
  accessibility checks, and HDR side-data probing.
- `Sussudio/Services/Recording/Verification/RecordingVerifier.ProbeParsing.cs` owns scalar/key-value parsing of ffprobe
  output.
- `Sussudio/Services/Recording/Verification/RecordingVerifier.Validation.cs` owns container, codec, dimensions, frame
  rate, HDR, and cadence mismatch policy.
- `Sussudio/Services/Recording/Verification/RecordingVerifier.Results.cs` owns early failure results, primary mismatch
  parsing, HDR parity, and mismatch taxonomy.
- `Sussudio/Services/Recording/Verification/RecordingVerifier.Cadence.cs` owns ffprobe frame timestamp parsing and
  cadence/drop/jitter metric calculation.

## Flashback

Primary current owner: `Sussudio/Services/Flashback/`

Entry points:

- `FlashbackBackendResources.cs` owns preview backend resource grouping,
  recovery-preserve state, recording-finalize handoff, and producer attach/detach
  request shaping for video, audio, and microphone feeds.
  `FlashbackBackendResources.Startup.cs` owns preview backend startup
  construction/install/playback initialization and startup rollback cleanup.
  `FlashbackBackendResources.BufferCycle.cs` owns sink-only buffer-cycle
  mechanics.
  `FlashbackBackendResources.ArtifactCleanup.cs` owns preview-backend teardown,
  sink stop/dispose, backend clear, and artifact cleanup request/retry/dispose/purge
  mechanics. The backend resource owner
  receives export-lock wait/release delegates from `CaptureService` rather than
  owning service semaphores directly during preview backend startup, cycling,
  and teardown. `CaptureService`
  remains the transition/readiness coordinator and delegates backend mechanics
  into this owner.
- `FlashbackBufferManager.cs` owns buffer live counters, byte/PTS accounting updates, and core state.
- `FlashbackBufferManager.SegmentMutation.cs` owns active segment path generation, active segment start/abandonment, completion registration, and same-path segment extension.
- `FlashbackBufferManager.Lifecycle.cs` owns initialization, segment extension setup, recovery-preserve markers, disposal, and disposed-state guards.
- `FlashbackBufferManager.SegmentQueries.cs` owns read-only segment counts, active-path projection, segment path lookup, start-PTS lookup, and segment-info projection.
- `FlashbackBufferManager.Retention.cs` owns segment purge, eviction selection, and guarded file deletion behavior.
- `FlashbackBufferManager.EvictionPause.cs` owns eviction-pause state, recording PTS range capture, and pause-driven disk warning state.
- `FlashbackStartupCacheCleanup.cs` owns startup stale-root/stale-session cleanup and temp-drive free-space probing.
- `FlashbackStartupSessionCacheBudget.cs` owns startup session-cache budget calculation, session-directory stats, oldest-session eviction, and cache-budget cleanup telemetry.
- `FlashbackDecoder.cs` owns decoder lifecycle, initialization, file open/close, and dispose shell.
- `FlashbackDecoder.DecodeLoop.cs` owns video frame receive, packet feeding, inline audio interleave during video reads, live-file EOF clearing, and decode phase timing state.
- `FlashbackDecoder.Seeking.cs` owns keyframe/exact seek control flow, pending-frame transfer, seek-cap diagnostics, and seek-buffer flushing.
- `FlashbackDecoder.D3D11.cs` owns D3D11VA decoder selection, get-format callback behavior, hardware-config diagnostics, and hardware decoder context setup.
- `FlashbackDecoder.VideoOutput.cs` owns decoded video frame output, plane copies, and YUV-to-NV12/P010 conversion.
- `FlashbackDecoder.AudioOutput.cs` owns audio codec/resampler initialization, audio packet delivery, callback failure handling, resampler output conversion, and bounded audio buffer sizing.
- `FlashbackDecoder.Timestamps.cs` owns PTS-to-TimeSpan conversion, seek timestamp conversion, best-effort frame timestamp selection, and recoverable seek log suppression.
- `FlashbackDecoder.Validation.cs` owns decoded frame-size calculation, video-dimension validation, D3D11/software decoded-frame validation, input stream-count bounds, and stream-index bounds.
- `FlashbackPlaybackController*.cs` owns playback, scrub, and marker control.
- `FlashbackPlaybackController.DecoderFiles.cs` owns decoder creation, active file identity, file open checks, best-effort close handling, and decoder cleanup.
- `FlashbackPlaybackController.DecoderReopen.cs` owns active fMP4 reopen retry, adjacent-segment seek fallback, keyframe-reopen recovery, and near-live reopen guards.
- `FlashbackPlaybackController.Lifecycle.cs` owns initialize/update component references, preview-detach cleanup, deferred reattach, and dispose.
- `FlashbackPlaybackController.Commands.cs` owns public playback command entry points for scrub, seek, play/pause, go-live, and nudge.
- `FlashbackPlaybackController.CommandQueue.cs` owns command queue writes and queue drop policy.
- `FlashbackPlaybackController.CommandCoalescing.cs` owns seek/scrub coalescing slot state, queued-command resolution, and control-yield peek policy.
- `FlashbackPlaybackController.CommandTelemetry.cs` owns command readiness guards, failure-detail formatting, and queue command telemetry bookkeeping.
- `FlashbackPlaybackController.ThreadLoop.cs` owns `PlaybackThreadEntry`,
  command dequeue, active-command telemetry, `Stop` handling, and dispatch to
  playback-thread command handlers, plus timer-resolution P/Invoke for playback
  pacing sleeps.
- `FlashbackPlaybackController.ThreadSeekCommands.cs` owns playback-thread seek
  command execution, including coalesced seek resolution, exact resume targets,
  playback resume handoff, and audio/preview suppression/resume ordering.
- `FlashbackPlaybackController.ThreadSeekScrubCommands.cs` owns playback-thread
  scrub begin/update/end command execution, including frozen valid-start sampling,
  scrub update coalescing, exact resume targets, and audio/preview
  suppression/resume ordering.
- `FlashbackPlaybackController.ThreadCommands.cs` owns playback-thread command
  execution for play, pause, go-live, and nudge, including thread-local playback
  state, exact resume targets, and audio/preview suppression/resume ordering.
- `FlashbackPlaybackController.ThreadLifecycle.cs` owns playback thread start/stop, channel recreation/completion, abandoned command draining, and join/cancel diagnostics.
- `FlashbackPlaybackController.ThreadCleanup.cs` owns playback-thread live-restore cleanup and playback CTS disposal warnings.
- `FlashbackPlaybackController.AudioRouting.cs` owns decoder audio callback wiring, playback chunk validation/return, live audio suppress/restore, preview submission suppression, and audio renderer pause/resume/flush helpers.
- `FlashbackPlaybackController.AudioPrebuffer.cs` owns playback startup/seek audio prebuffering and decoder rewind after decode-ahead audio priming.
- `FlashbackPlaybackController.AudioMasterPacing.cs` owns audio-master pacing, fallback accounting, audio clock drift calculation, and wall-clock sleep/spin pacing.
- `FlashbackPlaybackController.PreviewFrames.cs` owns decoded frame validation, preview submission, held-frame release, and live-restore after submit failures.
- `FlashbackPlaybackController.SeekDisplay.cs` owns seek/scrub keyframe display, file-PTS mapping for displayed seek frames, and seek-display failure accounting.
- `FlashbackPlaybackController.PlaybackLoop.cs` owns continuous playback frame progression, A/V skip decisions, decode-error live recovery, and near-live snap handling.
- `FlashbackPlaybackController.PlaybackSegmentEdges.cs` owns segment switching, fMP4 reopen recovery, write-head waits, and end-of-segment continuation policy.
- `FlashbackPlaybackController.PlaybackTiming.cs` owns frame-rate resolution, software-decode budget snaps, pause-from-live target calculation, and decoded PTS/cadence tracking.
- `FlashbackPlaybackController.Markers.cs` owns in/out marker state, marker API, marker normalization, and out-point pause checks.
- `FlashbackPlaybackController.PositionMapping.cs` owns scrub/seek clamp policy, saturating timestamp math, active fMP4 segment detection, and playback path comparison.
- `FlashbackPlaybackController.Metrics.cs` owns playback diagnostic counters and cadence/decode summary records.
- `FlashbackPlaybackController.MetricsCollection.cs` owns private metric collection, percentile math, seek-cap telemetry, decode timing wrappers, and metric reset.
- `FlashbackEncoderSink.Startup.cs` owns session validation, buffer session creation, encoder initialization, queue creation, background task startup, and startup rollback.
- `FlashbackEncoderSink.Options.cs` owns encoder option construction, recording-to-Flashback session mapping, packet records, file-size/session-id helpers, and buffer/COM return helpers.
- `FlashbackEncoderSink.VideoQueueSubmission.cs` owns video/GPU queue admission, enqueue rejection guards/logging, and TryWrite depth accounting.
- `FlashbackEncoderSink.Queues.cs` owns queue completion/signaling, shared queue-depth accounting, force-rotate audio queue guard policy, failure notification, and hot audio packet enqueue.
- `FlashbackEncoderSink.QueueCleanup.cs` owns queued-buffer cleanup and best-effort return/release of queued video, audio, microphone, and GPU packets.
- `FlashbackEncoderSink.EncodingLoop.cs` owns the background encode loop, normal drain ordering, force-rotate dispatch, cancellation handling, fatal cleanup, and final segment registration.
- `FlashbackEncoderSink.PacketDrain.cs` owns bounded video/GPU/audio/microphone packet drains, encoder PTS resolution, latest-PTS and disk-byte refresh, and frame-encoded event dispatch.
- `FlashbackEncoderSink.SegmentRotation.cs` owns rolling segment rotation, active-segment registration, disk-byte refresh after rotation, and rotation-failure recovery.
- `FlashbackEncoderSink.ForceRotate.cs` owns export force-rotate state/status/idle waits, export force-rotate requests, the `ForceRotateRequest` state machine, timeout/cancellation classification, pending-request cleanup, and force-rotate drain abort policy.
- `FlashbackEncoderSink.ForceRotateExecution.cs` owns encoding-thread force-rotate request capture, queue drain-to-rotate ordering, commit/rotation execution, result completion, failure logging, and draining-gate cleanup.
- `FlashbackEncoderSink.Inputs.cs` owns raw/lease/GPU video enqueue entry points, audio/microphone enqueue entry points, and hot WASAPI writer adapters.
- `FlashbackEncoderSink.Lifetime.cs` owns `StopAsync`, `Dispose`/`DisposeAsync`, deferred cleanup, cancellation/disposal helpers, and stop-drain timeout classification.
- `FlashbackEncoderSink.Recording.cs` owns the `IRecordingSink.StartAsync` adapter, retroactive recording begin/cancel/end lifecycle, recording PTS boundaries, and recording availability checks.
- `FlashbackEncoderSink.RuntimeState.cs` owns public counters, queue-depth/status projections, encoder format summaries, fatal-error callback registration, and the frame-encoded event surface.
- `FlashbackExporter.cs` owns shared native export state and constants.
- `FlashbackExporter.SingleFile.cs` owns the single-file export shell:
  validation, seek/setup, final output replacement, success result shaping, and
  single-export lock release.
- `FlashbackExporter.SingleFilePacketWriting.cs` owns single-file packet
  allocation/read/remux, buffered timestamp-base flush, drift logging, and
  no-packet validation.
- `FlashbackExporter.Segments.cs` owns multi-segment export validation
  dispatch, temp-output preparation, final output replacement, and export-lock
  release.
- `FlashbackExporter.SegmentPacketWriting.cs` owns the multi-segment
  packet-copy/remux orchestration: output-template initialization, packet read
  loop, progress reporting, segment offset updates, and requested-segment skip
  validation.
- `FlashbackExporter.SegmentPacketWriteState.cs` owns per-segment packet write
  state and decisions: timestamp-base discovery, buffered-packet rescue/flush,
  timestamp rebasing, segment-boundary repair, DTS monotonicity, and native
  packet write outcomes.
- `FlashbackExporter.SegmentRangeProjection.cs` owns per-segment export
  range/window projection and empty effective-range skip classification.
- `FlashbackExporter.SegmentInputPreflight.cs` owns per-segment input open,
  stream-info lookup, stream-count checks, layout-mismatch skip tracking, and
  close-on-failed-preflight behavior.
- `FlashbackExporter.SegmentSkipTracking.cs` owns requested-segment skip
  counting and failure-message policy for multi-segment exports.
- `FlashbackExporter.SegmentTemplate.cs` owns selection of the first usable
  segment output template, stream-map initialization, and template-skip logs.
- `FlashbackExporter.SegmentValidation.cs` owns multi-segment export input
  validation and readable-segment byte estimation for progress.
- `FlashbackExporter.Requests.cs` owns public export request routing.
- `FlashbackExporter.Lifetime.cs` owns exporter disposal and cancellation of
  active exports during disposal.
- `FlashbackExporter.Execution.cs` owns export task scheduling, linked cancellation
  wrapper disposal, background thread priority, and segment snapshots.
- `FlashbackExporter.PacketTiming.cs` owns packet timestamp normalization and
  segment boundary timestamp repair.
- `FlashbackExporter.PacketBuffers.cs` owns packet clone/free helpers and
  buffered packet flushes.
- `FlashbackExporter.Streams.cs` owns input/output FFmpeg context setup,
  stream count validation, and output header writing.
- `FlashbackExporter.StreamTemplates.cs` owns stream-template copying and
  segment stream-layout compatibility checks.
- `FlashbackExporter.OutputFiles.cs` owns active output trailer/IO close
  finalization, temp-output validation, atomic destination replacement,
  overwrite policy, and invalid final-output cleanup.
- `FlashbackExporter.Progress.cs` owns progress normalization/reporting and
  heartbeat cadence.
- `FlashbackExporter.WriterPacing.cs` owns export writer adaptive throttling,
  fixed sleep/yield pacing, and per-export throttle provider scoping.
- `FlashbackExporter.TempFiles.cs` owns temp output cleanup, stale temp
  preparation, and orphaned `.mp4.tmp` cleanup.
- `FlashbackExporter.ExportLock.cs` owns export-lock wait, release, timeout,
  cancellation, and disposal warning policy.
- `FlashbackExporter.OutputValidation.cs` owns completed-output length probing
  and final output validation text.
- `FlashbackExporter.PathValidation.cs` owns normalized path comparison and
  output path validation.
- `FlashbackExporter.SegmentSelection.cs` owns export-range validation and
  segment/export-range overlap classification.
- `FlashbackExporter.NativeState.cs` owns active input/output close and native
  FFmpeg cleanup.
- `FlashbackExporter.Cancellation.cs` owns linked export cancellation-source
  creation, best-effort disposal, dispose-CTS reference cleanup, and shared
  cancelled/disposed export failure result creation.
- `FlashbackExporter.LibAvErrors.cs` owns FFmpeg error string formatting and
  throwing.
- `FlashbackExporter.TimeMath.cs` owns time-span timestamp conversion,
  saturated time arithmetic, and non-negative byte/count saturation.

Invariants:

- Disable means the timeline should be hidden/locked out.
- Scrub frames must not contaminate live/playback cadence metrics.
- Export must not overwrite without the explicit force path.

## UI Shell And Presentation

Primary current owners:

- `Sussudio/MainWindow.*.cs` for shell, renderer, fullscreen, screenshots,
  animations, and window lifecycle.
- `Sussudio/Controllers/FullScreen/FullScreenController.cs` owns fullscreen public
  toggle/state and shared context; `Sussudio/Controllers/FullScreen/FullScreenController.Transitions.cs` owns
  enter/exit orchestration, `Sussudio/Controllers/FullScreen/FullScreenController.Animation.cs` owns rect
  animation and size waits, `Sussudio/Controllers/FullScreen/FullScreenController.Chrome.cs` owns chrome/material
  state, and `Sussudio/Controllers/FullScreen/FullScreenController.Controls.cs` owns overlay pointer/auto-hide
  behavior. Keep `MainWindow.FullScreen.cs` as the XAML-facing adapter.
  `MainWindow.FullScreenFlashbackBridge.cs` owns the Flashback fullscreen
  keyboard gate/adapter, timeline visibility, and scrub-end bridging.
- `Sussudio/Controllers/Screenshot/Window/WindowScreenshotController.cs` owns automation whole-
  window screenshot dispatch, UI-thread enqueue/cancellation, and failure
  wrapping. `Sussudio/Controllers/Screenshot/Window/WindowScreenshotNativeCapture.cs` owns native
  PrintWindow capture, GDI/DIB lifetime, output directory creation, and
  screenshot result shaping. `Sussudio/Controllers/Screenshot/Window/WindowScreenshotImageEncoder.cs`
  owns the pure PNG/BMP byte-stream encoding helpers. Keep
  `MainWindow.Screenshot.cs` as the `IAutomationWindowControl` adapter.
- `Sussudio/Controllers/Screenshot/Preview/PreviewScreenshotPlanPolicy.cs` owns the pure preview-
  frame screenshot output-directory fallback, file naming, status text, and log
  text policy. `Sussudio/Controllers/Screenshot/Preview/PreviewScreenshotController.cs` owns the
  XAML preview-frame screenshot button workflow: directory creation,
  preview-frame capture, logging side effects, and button enable/disable state.
  `MainWindow.Screenshot.cs` is the XAML-facing adapter for both preview-frame
  screenshots and whole-window automation screenshots.
- `Sussudio/Controllers/Window/WindowAutomationController.cs` owns window geometry
  automation plus the recordings-folder command: UI-thread dispatch, AppWindow
  and DisplayArea access, maximized presenter restore, and side effects.
  `Sussudio/Controllers/Window/WindowSnapRegionLayoutPolicy.cs` owns the pure
  snap-region rectangle math. `MainWindow.WindowAutomation.cs` is the
  `IAutomationWindowControl` adapter; recording-aware close handling stays with
  the close lifecycle/finalization owners.
- `Sussudio/Controllers/Window/WindowAutomationHostLifecycleController.cs` owns shell
  automation host lifecycle: automation token/pipe-name resolution, diagnostics
  hub construction, command dispatcher construction, named-pipe server
  construction, once-only startup, ready/disabled logging, and pipe-before-hub
  shutdown disposal. `Sussudio/MainWindow.AutomationHost.cs` is the shell-facing
  start/dispose adapter.
- `Sussudio/MainWindow.Startup.cs` owns first-load startup, initial
  ViewModel/device refresh, automation startup timing, and the launch entrance
  trigger. Window close routing/finalization ownership is detailed in the
  window close section below:
  `Sussudio/Controllers/Window/WindowCloseLifecycleController.cs`,
  `Sussudio/Controllers/Window/WindowCloseRecordingFinalizationController.cs`,
  `Sussudio/MainWindow.CloseLifecycle.cs`, and
  `Sussudio/MainWindow.ShutdownCleanup.cs`.
- `Sussudio/Controllers/Preview/PreviewResizeTelemetryController.cs` owns top-level
  preview resize telemetry throttling and reset state for preview compositor
  transforms. `MainWindow.WindowSizing.cs` is the `SizeChanged` adapter.
  `Sussudio/Controllers/Preview/Renderer/PreviewRendererStartupPlanBuilder.cs` owns renderer
  startup dimension/fps/HDR/min-present-interval planning.
  `Sussudio/Controllers/Preview/Renderer/PreviewRendererHostController.cs` owns hosted preview
  renderer context, public runtime state, counters, and simple renderer surface
  methods. `Sussudio/Controllers/Preview/Renderer/PreviewRendererHostController.Lifecycle.cs`
  owns start/stop/shutdown flow, renderer startup planning, and cleanup.
  `Sussudio/Controllers/Preview/Renderer/PreviewRendererHostController.D3D.cs` owns D3D renderer
  startup and event/failure handling. `Sussudio/Controllers/Preview/Renderer/PreviewRendererHostController.Cpu.cs`
  owns CPU preview fallback attachment.
  `Sussudio/Controllers/Preview/Renderer/PreviewRendererHostController.Reinit.cs` owns D3D
  reinit disposal, unsafe-window telemetry, stop tick accounting, and fresh
  SwapChainPanel replacement. `MainWindow.PreviewRenderer.cs` is the XAML-facing
  host/reinit adapter surface.
  `Sussudio/Controllers/Preview/PreviewSurfacePresentationController.cs` owns preview
  surface content-fit sizing and GPU panel visibility.
  `Sussudio/Controllers/Preview/PreviewSurfaceShadowController.cs` owns
  video/control-bar composition shadow visuals, bounds alignment, clear behavior,
  and fade routing. `MainWindow.PreviewSurface.cs` is the XAML-facing adapter.
- `Sussudio/MainWindow.PreviewRuntimeSnapshot.cs` owns the stable automation
  preview snapshot UI-dispatch adapter and UI-thread-only preview state
  sampling. `Sussudio/Controllers/Preview/Renderer/PreviewRuntimeSnapshotController.cs`
  owns the read-only preview runtime snapshot composition and blank/stall
  suspicion. `Sussudio/Controllers/Preview/Renderer/PreviewRuntimeD3DProjection.cs`
  owns renderer frame-counter override, cadence projection, D3D diagnostic
  fields, estimated pipeline latency, and GPU playback projection.
  Window close routing/finalization ownership is detailed in the window close
  section below:
  `Sussudio/Controllers/Window/WindowCloseLifecycleController.cs`,
  `Sussudio/Controllers/Window/WindowCloseRecordingFinalizationController.cs`,
  `Sussudio/MainWindow.CloseLifecycle.cs`, and
  `Sussudio/MainWindow.ShutdownCleanup.cs`.
- `Sussudio/MainWindow.StatusStripPresentation.cs` keeps the XAML-facing title
  update hook; `Sussudio/Controllers/Window/WindowTitleController.cs` owns window title
  base/build-stamp formatting and the recording-time suffix used by property
  changes.
- `Sussudio/Controllers/Shell/StatusStripPresentationController.cs` owns bottom
  status-strip projection: status text, recording time, disk warning,
  disk-space text, recording size, recording bitrate, the status-strip
  `PropertyChanged` router, the recording-only title-refresh callback, and the
  Flashback bitrate fallback used while Flashback is enabled and recording is
  idle. `Sussudio/MainWindow.StatusStripPresentation.cs` is the XAML-facing
  adapter and builds the ViewModel snapshot passed into the controller.
- `Sussudio/Controllers/Window/WindowCloseLifecycleController.cs` owns window close
  request flags, completion TCS, cleanup latch, recording-stop handoff flags,
  close-in-progress exception classification, and automation close dispatch
  orchestration.
- `Sussudio/Controllers/Window/WindowCloseRequestController.cs` owns actual
  close request execution: `Close()`, completion timing after non-recording
  closes, close-in-progress success handling, COM `Application.Current.Exit()`
  fallback, and requested-state reset after unexpected failures.
- `Sussudio/Controllers/Window/WindowCloseRecordingFinalizationController.cs` owns
  recording finalization side effects during pre-close and post-close cleanup:
  the 120-second stop budget, `StopRecordingAndWaitAsync` wait race, timeout/
  failure breadcrumbs, status text, and shutdown-content dim/restore policy.
- `Sussudio/Controllers/Window/WindowAppClosingController.cs` owns
  `AppWindow.Closing` decision choreography: trigger logging, recording-aware
  close cancellation, duplicate-stop guard, pre-close recording stop handoff,
  completion, and second-close request.
- `Sussudio/Controllers/Window/WindowShutdownCleanupController.cs` owns the
  post-`Closed` cleanup order: cleanup latch, close completion, closing-state
  mark, timer stops, event detaches, preview shutdown, post-close recording
  finalization handoff, automation disposal, NVML disposal, and ViewModel
  disposal.
- `Sussudio/MainWindow.CloseLifecycle.cs` owns the XAML/AppWindow close adapter:
  `RegisterCloseLifecycle`, `CloseAsync`, and the stable
  `RequestWindowClose()` adapter.
- `Sussudio/MainWindow.ShutdownCleanup.cs` is the XAML-facing `Closed` adapter
  and wires MainWindow cleanup delegates into `WindowShutdownCleanupController`.
- `Sussudio/Controllers/Window/NativeWindowBootstrapController.cs` owns native window
  bootstrap: `AppWindow` lookup, ViewModel window handle handoff,
  minimum-size subclassing, DWM cloak/dark-mode setup, first-composed-frame
  shell reveal scheduling/cancellation, initial shell size, icon, and native
  helpers used by shell startup and automation controllers.
  `Sussudio/MainWindow.NativeWindow.cs` is the XAML-facing adapter and keeps
  the `_hwnd` field consumed by screenshot and window automation paths.
- `Sussudio/Controllers/Window/WindowUiDispatchController.cs` owns MainWindow
  UI-thread direct execution, dispatcher enqueue/cancellation/error wrapping,
  preview-snapshot-style result dispatch with three-attempt enqueue retry, and
  guarded async event-handler status updates used by automation adapters and
  XAML event handlers. `Sussudio/MainWindow.Dispatching.cs` keeps the stable
  private MainWindow adapter names for callers.
- `Sussudio/MainWindow.Bindings.cs` owns the root `SetupBindings()`
  startup binding sequence and leaves feature-specific binding clusters in
  focused partials or controllers, including initial recording lockout,
  device-selection change hooks, stats visibility sync, and status-strip
  projection.
- `Sussudio/MainWindow.PreviewActions.cs` owns only the preview button XAML
  command adapter. `PreviewButtonActionController` owns the preview
  fade/reinit/start/stop command behavior. One-line XAML command bridges for
  capture-device, recording, output-path, and preview-screenshot buttons live in
  their feature adapter partials beside the owning controllers.
- `Sussudio/MainWindow.PropertyChanged.cs` owns only the root ViewModel
  PropertyChanged event envelope, property-name normalization, and route order.
  Capture-selection and status-strip adapters are still considered first through
  `MainWindow.CaptureSelectionBindings.cs` and
  `MainWindow.StatusStripPresentation.cs`; broad domain property-name switches
  and status-strip routing logic live in focused controllers/partials.
- `Sussudio/Controllers/Preview/PreviewShadowFadeAnimator.cs` owns shared
  compositor opacity fade helpers for preview shadow visuals. XAML-facing
  adapters call it without adding state or dispatcher hops.
- `Sussudio/Controllers/Audio/Meter/AudioMeterController.cs` owns audio/microphone meter
  setup and shared runtime fields.
  `Sussudio/Controllers/Audio/Meter/AudioMeterController.Context.cs` owns the XAML/view-model
  dependency bag, `Sussudio/Controllers/Audio/Meter/AudioMeterController.MeterState.cs`
  owns smoothing, peak/range markers, microphone meter clipping, reset behavior,
  timer lifetime, and `TranslateMarker`, and
  `Sussudio/Controllers/Audio/Meter/AudioMeterController.PresentationAnimations.cs` owns
  monitoring/disabled animations plus rounded content clips.
  `Sussudio/MainWindow.AudioMeter.cs` is its XAML-facing adapter.
  `Sussudio/Controllers/Audio/AudioControlBindingController.cs` owns the audio-control
  binding context, initial audio/microphone projection, preview-volume binding
  and priming, audio/microphone/device-audio selection handlers,
  record/preview/custom-audio/microphone toggle handlers, audio-meter
  activation, initial meter presentation, and device-audio gain/meter resize
  hooks.
  `Sussudio/MainWindow.AudioBindings.cs` is its XAML-facing adapter.
- `Sussudio/Controllers/Stats/StatsOverlayController.cs` owns stats dock visibility
  orchestration, stats/frame-time toggle event hookup and checked/unchecked
  handling, stats toggle-to-view model sync, frame-time overlay visibility, and
  polling lifetime.
  `Sussudio/MainWindow.StatsOverlay.cs` owns the stats overlay XAML adapter,
  binding setup, visibility commands, and composition of the frame-time and
  stats dock graph owners beside the overlay controller.
  `Sussudio/Controllers/Stats/StatsDockControllerGraph.cs` owns stats dock
  presentation, diagnostic row, hardware row, and refresh-controller graph
  construction because the dock is only driven by the overlay controller.
  `Sussudio/Controllers/Stats/StatsOverlayController.DockAnimation.cs` owns stats dock
  show/hide storyboard construction, dock visibility mutations, and completion
  state.
  `Sussudio/Controllers/Stats/StatsDockRefreshController.cs` owns stats dock refresh
  orchestration: snapshot acquisition, dock presentation build/apply,
  diagnostics visibility gating, and decode/GPU row refresh ordering.
  `Sussudio/Controllers/Stats/StatsDockPresentationController.cs` owns
  stats dock metric text, visibility, and status brush application after the
  presentation model is built. `Sussudio/Controllers/Stats/StatsSectionChromeController.cs`
  owns stats dock section expand/collapse chrome and automation-visible section
  visibility application; `Sussudio/MainWindow.StatsOverlay.cs` is the
  XAML/automation adapter for that stats shell wiring.
  `Sussudio/Controllers/Stats/StatsWindowPresentationController.cs`
  owns detached stats-window metric text and delegates dynamic telemetry detail
  rendering to `Sussudio/Controllers/Stats/StatsWindowTelemetryDetailsController.cs`.
  `Sussudio/Controllers/Stats/StatsSnapshotProvider.cs` owns shell stats snapshot
  orchestration from capture-health, renderer metrics, and view state, including
  renderer cadence/recent-sample acquisition and null fallback policy.
  `Sussudio/MainWindow.StatsOverlay.cs` is the XAML-facing adapter for stats
  snapshot provider composition, stats overlay composition, stats dock
  visibility, polling, section chrome, and refresh-controller hooks.
- `tests/Sussudio.Tests/StatsOverlay.Lifecycle.Tests.cs` owns legacy harness
  contract checks for stats overlay lifecycle wiring and stats section chrome.
- `tests/Sussudio.Tests/StatsDockPresentation.Tests.cs` owns legacy harness
  contract checks for stats dock refresh orchestration, diagnostic row update
  delegation, hardware row projection, and row chrome pooling.
- `Sussudio/Controllers/Stats/StatsDiagnosticRowsController.cs` owns diagnostic row
  presentation, empty-state rows, group headers, and diagnostic row pooling.
  `Sussudio/Controllers/Stats/StatsDockRowChromeController.cs` owns dynamic decode/GPU
  simple row pools.
  `Sussudio/Controllers/Stats/StatsDockRowChromePresenter.cs` owns shared stats
  dock row creation, label/value text mutation, visibility toggles, and dock row
  style application.
  `Sussudio/Controllers/Stats/StatsDockRefreshController.cs` delegates diagnostic row
  presentation to `StatsDiagnosticRowsController`.
- `Sussudio/Controllers/Stats/StatsHardwareRowsController.cs` owns hardware row
  refresh, availability, and decode/GPU minimum pool sizing before delegating row
  chrome. `Sussudio/Controllers/Stats/StatsHardwareRowsInputProvider.cs` owns
  live MJPEG/NVML input acquisition and decode availability policy;
  `Sussudio/Controllers/Stats/StatsHardwareRowsInputBuilder.cs` owns pure
  telemetry projection into the hardware-row presentation input DTOs;
  `Sussudio/ViewModels/StatsPresentationBuilder.HardwareRows.cs` owns pure
  decode/GPU row text projection over presentation inputs, and
  `StatsDockRowChromeController` owns decode/GPU row pooling while
  `StatsDockRowChromePresenter` owns shared row chrome and
  `StatsDockRefreshController` owns when decode/GPU rows refresh.
- `Sussudio/Controllers/Stats/FrameTimeOverlayPresentationController.cs` owns compact
  frame-time overlay text application and graph-line mutation, while
  `Sussudio/Controllers/Stats/FrameTimeOverlayGeometry.cs` owns frame-time
  canvas sizing, sample projection, and expected-line geometry.
  `Sussudio/MainWindow.StatsOverlay.cs` owns the XAML-facing compact overlay
  adapter and presentation-controller composition beside the stats overlay
  visibility route.
  `Sussudio/ViewModels/StatsPresentationBuilder.cs` owns shared stats
  formatting helpers.
  `Sussudio/ViewModels/StatsPresentationBuilder.Dock.cs` owns stats dock
  summary construction and HDMI/capture/preview resolution text.
  `Sussudio/ViewModels/StatsPresentationBuilder.FrameTime.cs` owns compact
  preview-stat formatting, range/sample text policy, and frame-time overlay
  presentation. `Sussudio/ViewModels/StatsPresentationBuilder.Visual.cs` owns
  visual-cadence FPS/repeat/motion text formatting and expected visual-repeat
  drift helpers.
  `Sussudio/ViewModels/StatsPresentationBuilder.Encoder.cs` owns encoder dock
  visibility, codec label, bitrate, and encoder drift text formatting.
  `Sussudio/ViewModels/StatsPresentationBuilder.DiagnosticRows.cs` owns
  diagnostic row construction and source-summary parsing.
  `Sussudio/Controllers/Stats/StatsHardwareRowsInputProvider.cs` owns live
  MJPEG/NVML sampling callbacks for hardware rows.
  `Sussudio/Controllers/Stats/StatsHardwareRowsInputBuilder.cs` owns pure
  telemetry-to-presentation-input projection for MJPEG/NVML hardware rows.
  `Sussudio/ViewModels/StatsPresentationBuilder.HardwareRows.cs` owns decode
  and GPU row text projection over presentation inputs.
  `Sussudio/ViewModels/StatsPresentationBuilder.DiagnosticSummary.cs` owns
  frame-lane diagnostic health summary classification.
  `Sussudio/ViewModels/StatsPresentationBuilder.Window.cs` owns detached
  stats-window text and telemetry-detail presentation.
  `Sussudio/ViewModels/StatsPresentationBuilder.Status.cs`
  owns stats lane status classification and consumes the visual-repeat drift
  result.
  `Sussudio/ViewModels/StatsPresentationModels.cs` owns the internal DTO
  records/enums consumed by the stats overlay and stats-window controllers.
  `Sussudio/ViewModels/StatsSnapshot.cs` owns the UI stats snapshot DTO, and
  `Sussudio/ViewModels/StatsSnapshotBuilder.cs` owns capture-health, renderer,
  and shell view-state projection into that DTO after acquisition.
- `Sussudio/ViewModels/CaptureModeOptionsBuilder.cs` owns pure resolution and
  video-format option construction, HDR mode enablement, and source aspect-ratio
  filtering. Shell files bind and display those options.
- `tests/Sussudio.Tests/StatsPresentation.Contract.Tests.cs` owns stats
  presentation display-repeat visual cadence contract checks.
  `tests/Sussudio.Tests/StatsPresentation.Ownership.Tests.cs` owns
  builder/controller/DTO source-shape assertions,
  `tests/Sussudio.Tests/StatsPresentation.SourceTelemetry.Tests.cs` owns HDMI
  source telemetry panel projection checks,
  `tests/Sussudio.Tests/StatsPresentation.Window.Tests.cs` owns detached-window
  formatting, `tests/Sussudio.Tests/StatsPresentation.Encoder.Tests.cs` owns
  dock encoder formatting,
  `tests/Sussudio.Tests/StatsPresentation.FrameTime.Tests.cs` owns compact
  preview summary, frame-time range policy, and frame-time graph geometry checks,
  `tests/Sussudio.Tests/StatsHardwareRowsController.Tests.cs` owns hardware row
  presentation/input behavior checks, and
  `tests/Sussudio.Tests/MainViewModel.DiskSpacePresentation.Tests.cs` owns disk
  space presentation bridge checks.
- `tests/Sussudio.Tests/MainWindowUiContract.AutomationIds.Tests.cs` owns
  MainWindow automation ID inventory checks.
- `tests/Sussudio.Tests/MainWindowUiContract.WindowAutomation.Tests.cs` owns
  MainWindow full-screen and window automation source contract checks.
  `tests/Sussudio.Tests/WindowSnapRegionLayoutPolicy.Tests.cs` owns snap-region
  rectangle policy behavior checks.
- `tests/Sussudio.Tests/MainWindowUiContract.Dispatching.Tests.cs` owns
  MainWindow UI-dispatching contract checks.
- `tests/Sussudio.Tests/MainWindowUiContract.StatsSnapshot.Tests.cs` owns stats
  snapshot builder contract checks.
- `tests/Sussudio.Tests/MainWindow.ShellOwnership.Chrome.Tests.cs` owns
  MainWindow shell chrome ownership assertions for the settings shelf and window
  title.
- `tests/Sussudio.Tests/MainWindow.ShellOwnership.Startup.Tests.cs` owns
  MainWindow startup/launch ownership assertions for splash loading phrases,
  splash pacing policy, launch entrance animation, and first-load hosting.
- `tests/Sussudio.Tests/MainWindow.ShellOwnership.PreviewRuntime.Tests.cs`
  owns MainWindow preview resize telemetry, preview surface/shadow controller,
  and preview runtime/snapshot ownership assertions.
- `tests/Sussudio.Tests/MainWindow.ShellOwnership.WindowLifecycle.Tests.cs`
  owns MainWindow close lifecycle, native bootstrap, and first-frame native
  reveal ownership assertions.
- `tests/Sussudio.Tests/MainWindow.ControllerOwnership.Visual.Tests.cs` owns
  MainWindow controller-adapter ownership assertions for control bar, shell
  elevation, preview-transition, preview startup overlay, preview fade-in, and
  recording-button chrome controller.
- `tests/Sussudio.Tests/MainWindow.ControllerOwnership.Tests.cs` owns
  MainWindow property-change routing ownership assertions across focused
  controller adapters.
- `tests/Sussudio.Tests/MainWindow.ControllerOwnership.Interaction.Tests.cs`
  owns MainWindow controller-adapter ownership assertions for recording action,
  live signal info, status-strip presentation, preview audio fade, microphone
  controls.
- `tests/Sussudio.Tests/MainWindow.ControllerOwnership.Layout.Tests.cs` owns
  MainWindow responsive shell layout controller-adapter and breakpoint/placement
  policy assertions.
- `tests/Sussudio.Tests/MainWindow.ControllerOwnership.Capture.SelectionBindings.Tests.cs`
  owns capture selection binding/sync controller-adapter ownership assertions.
- `tests/Sussudio.Tests/MainWindow.ControllerOwnership.Capture.SelectionNormalizer.Tests.cs`
  owns capture ComboBox selection normalizer fallback-policy assertions.
- `tests/Sussudio.Tests/MainWindow.ControllerOwnership.Capture.DeviceActions.Tests.cs`
  owns capture refresh/apply button controller-adapter ownership assertions.
- `tests/Sussudio.Tests/MainWindow.ControllerOwnership.Capture.OptionPresentation.Tests.cs`
  owns capture option presentation controller-adapter ownership assertions.
- `tests/Sussudio.Tests/MainWindow.ControllerOwnership.Capture.OptionPresentationPolicy.Tests.cs`
  owns capture option presentation affordance-policy assertions.
- `tests/Sussudio.Tests/MainWindow.ControllerOwnership.Capture.OptionBindings.Tests.cs`
  owns capture/recording option binding controller-adapter ownership assertions.
- `tests/Sussudio.Tests/MainWindow.ControllerOwnership.Capture.OptionTooltipFormatter.Tests.cs`
  owns capture option HDR/FPS tooltip text-policy assertions.
- `tests/Sussudio.Tests/MainWindow.ControllerOwnership.Output.Tests.cs` owns
  MainWindow output path display/action ownership assertions.
- `tests/Sussudio.Tests/MainWindow.ControllerOwnership.PreviewScreenshot.Tests.cs`
  owns MainWindow preview screenshot workflow and text-policy ownership
  assertions.
- `tests/Sussudio.Tests/MainWindow.FlashbackOwnership.Polling.Tests.cs` owns
  Flashback status/playback polling controller ownership assertions.
- `tests/Sussudio.Tests/MainWindow.FlashbackOwnership.Timeline.Tests.cs` owns
  Flashback timeline track layout controller ownership assertions.
- `tests/Sussudio.Tests/MainWindow.FlashbackOwnership.Playhead.Tests.cs` owns
  Flashback playhead/CTI motion controller ownership assertions.
- `tests/Sussudio.Tests/MainWindow.FlashbackOwnership.Markers.Tests.cs` owns
  Flashback marker presentation controller ownership assertions.
- `tests/Sussudio.Tests/MainWindow.FlashbackOwnership.PlaybackPresentation.Tests.cs`
  owns Flashback playback presentation/coordinator ownership assertions.
- `tests/Sussudio.Tests/MainWindow.FlashbackOwnership.ExportProgress.Tests.cs`
  owns Flashback export progress presentation ownership assertions.
- `tests/Sussudio.Tests/MainWindow.FlashbackOwnership.Settings.Tests.cs` owns
  Flashback settings binding and command controller ownership assertions.
- `tests/Sussudio.Tests/MainViewModel.Automation.Preview.Tests.cs` owns
  automation preview enable/disable command ownership assertions.
- `tests/Sussudio.Tests/MainViewModel.Automation.Hdr.Tests.cs` owns
  automation HDR/true-HDR preview enablement guard assertions plus HDR mode
  change side-effect ownership assertions.
- `tests/Sussudio.Tests/MainViewModel.Automation.PreviewVolume.Tests.cs` owns
  preview-volume persistence and automation options surface assertions.
- `tests/Sussudio.Tests/MainViewModel.Automation.RecordingTransition.Tests.cs`
  owns recording-transition routing through the shared lifecycle gate.
- `tests/Sussudio.Tests/MainViewModel.Automation.AsyncSurface.Tests.cs` owns
  async automation view-model surface, Flashback/probe routing, cancellation,
  timeout, audio, preview, and device-routing assertions that have not yet
  moved to narrower owners.
- `tests/Sussudio.Tests/MainViewModel.Automation.Audio.Tests.cs` owns
  automation audio/microphone command entry-point and runtime-guard assertions.
- `tests/Sussudio.Tests/MainViewModel.Automation.UiSettings.Tests.cs` owns
  automation UI-setting persistence and frame-time/stat visibility contracts.
- `tests/Sussudio.Tests/MainViewModel.Automation.CaptureMode.Tests.cs` owns
  automation capture-mode reinitialization and device-selection routing
  contracts.
- `tests/Sussudio.Tests/MainViewModel.Automation.DiagnosticsRefresh.Tests.cs`
  owns the serialized diagnostics refresh ownership check and coordinates the
  focused diagnostics refresh assertion helpers.
- `tests/Sussudio.Tests/MainViewModel.Automation.DiagnosticsRefresh.RefreshPipeline.Tests.cs`
  owns diagnostics refresh pipeline, refresh gate, snapshot, and dispatcher
  ownership assertions.
- `tests/Sussudio.Tests/MainViewModel.Automation.DiagnosticsRefresh.AlertEvents.Tests.cs`
  owns diagnostics alert/event ownership assertions for UpdateAlerts,
  diagnostic events, signal alerts, and Flashback alert routing.
- `tests/Sussudio.Tests/MainViewModel.Automation.DiagnosticsRefresh.FlashbackAlerts.Tests.cs`
  owns Flashback alert, counter, completion-event, and diagnostics lane
  coverage for the diagnostics refresh family.
- `tests/Sussudio.Tests/MainViewModel.Automation.DiagnosticsRefresh.FlashbackExport.Tests.cs`
  owns capture-service and dispatcher Flashback export operation ownership
  assertions used by diagnostics refresh.
- `tests/Sussudio.Tests/MainViewModel.Automation.DiagnosticsRefresh.CoreOwnership.Tests.cs`
  owns diagnostics refresh core ownership assertions for evaluation policy,
  diagnostic evaluation lanes, verification, preview pacing, lifecycle, HDR,
  and the initial snapshot/BuildAutomationSnapshot shape.
- `tests/Sussudio.Tests/MainViewModel.Automation.DiagnosticsRefresh.SourceReaderOwnership.Tests.cs`
  owns source-reader partial ownership assertions used by diagnostics refresh.
- `tests/Sussudio.Tests/MainViewModel.Automation.DiagnosticsRefresh.DiagnosticSessionCore.Tests.cs`
  owns diagnostic-session orchestration, model, and startup/stop contract
  assertions.
- `tests/Sussudio.Tests/MainViewModel.Automation.DiagnosticsRefresh.DiagnosticSessionPlayback.Tests.cs`
  owns diagnostic-session Flashback playback metrics and result assertions.
- `tests/Sussudio.Tests/MainViewModel.Automation.DiagnosticsRefresh.DiagnosticSessionPreview.Tests.cs`
  owns diagnostic-session preview, visual cadence, D3D, and process metric
  assertions.
- `tests/Sussudio.Tests/MainViewModel.Automation.DiagnosticsRefresh.DiagnosticSessionExportRecording.Tests.cs`
  owns diagnostic-session Flashback export, recording, artifact, cleanup, and
  storage metric assertions.
- `tests/Sussudio.Tests/MainViewModel.Automation.DiagnosticsRefresh.DiagnosticSessionScenarios.Tests.cs`
  owns diagnostic-session scenario, health-policy, Flashback stress, and
  warning-tolerance assertions.
- `tests/Sussudio.Tests/MainViewModel.Automation.DiagnosticsRefresh.DiagnosticSessionToolSurface.Tests.cs`
  owns `ssctl` and MCP diagnostic-session command-surface assertions.
- `tests/Sussudio.Tests/MainViewModel.Automation.DiagnosticsRefresh.SnapshotProjection.Tests.cs`
  owns diagnostics snapshot projection ownership assertions for
  snapshot projection source text and named BuildProjection boundaries.
- `tests/Sussudio.Tests/MainViewModel.Automation.DiagnosticsRefresh.SourceFamily.cs`
  owns the diagnostics hub source-family reader used by refresh ownership
  assertions.
- `tests/Sussudio.Tests/MainViewModel.Automation.DiagnosticsRefresh.SourceReaders.cs`
  owns diagnostics refresh source/fixture readers for capture service, source
  reader, and tool-surface source text used by refresh ownership assertions.
  `tests/Sussudio.Tests/DiagnosticSession.SourceReaders.cs` owns shared
  diagnostic-session source-family readers used by refresh, MCP, and tool
  ownership assertions.
- `tests/Sussudio.Tests/MainViewModel.Automation.DiagnosticsRefresh.PreviewRuntime.Tests.cs`
  owns diagnostics snapshot preview runtime projection ownership assertions.
- `tests/Sussudio.Tests/MainViewModel.Automation.DiagnosticsRefresh.SnapshotStatus.Tests.cs`
  owns diagnostics snapshot status projection ownership assertions.
- `tests/Sussudio.Tests/MainViewModel.Automation.DiagnosticsRefresh.Hdr.Tests.cs`
  owns diagnostics HDR truth verdict behavior.
- `tests/Sussudio.Tests/MainViewModel.Automation.DiagnosticsLoop.Tests.cs`
  owns diagnostics-loop polling contracts that keep options snapshots out of
  hot diagnostics refresh paths.
- Keep new automation diagnostics projection ownership assertions in the focused
  owner files:
  `MainViewModel.Automation.DiagnosticsProjection.Snapshot.Tests.cs`,
  `.Audio.Tests.cs`, `.Capture.Tests.cs`, `.Mjpeg.Tests.cs`,
  `.Recording.Tests.cs`, `.System.Tests.cs`, `.Preview.Tests.cs`, and
  `.Flashback.Tests.cs`.
- `tests/Sussudio.Tests/MainViewModel.Automation.RuntimeSafety.Tests.cs` owns
  automation timeout, recording failure propagation, safe close, screenshot,
  preview-stop surface, process supervisor, and emergency-stop assertions.
- `tests/Sussudio.Tests/MainViewModel.Automation.FlashbackCleanup.Tests.cs`
  owns Flashback startup-cache and session-recovery cleanup ownership
  assertions that used to live in the automation test catch-all.
- `tests/Sussudio.Tests/MainViewModel.Capture.SettingsProjection.Tests.cs`
  owns capture settings projection ownership assertions, including the focused
  frame-rate request projector used by `BuildCaptureSettings`.
- `tests/Sussudio.Tests/MainViewModel.Capture.AudioMonitoring.Tests.cs` owns
  capture audio-monitoring coordinator surface assertions.
- `tests/Sussudio.Tests/MainViewModel.Capture.OutputPath.Tests.cs` owns
  assertions that output folder picker ownership stays out of `MainViewModel`.
- `tests/Sussudio.Tests/MainViewModel.Capture.TestHelpers.cs` owns shared
  MainViewModel source-inspection helpers for capture-facing tests.
- `tests/Sussudio.Tests/MainViewModel.Capture.SelectionPolicy.FrameRates.Tests.cs`
  owns frame-rate source filtering, automatic frame-rate choice,
  `ShowAllCaptureOptions` behavior, and timing-policy ownership assertions.
- `tests/Sussudio.Tests/MainViewModel.Capture.SelectionPolicy.Resolution.Tests.cs`
  owns resolution-selection policy assertions, including HDR and SDR source
  retarget behavior.
- `tests/Sussudio.Tests/MainViewModel.Capture.SelectionPolicy.ModeSelection.Tests.cs`
  owns mode-selection reset and resolved automatic frame-rate application
  assertions.
- `tests/Sussudio.Tests/MainViewModel.Capture.SelectionPolicy.DeviceFormatProbeRetarget.Tests.cs`
  owns late device-format probe retarget policy behavior assertions.
- `tests/Sussudio.Tests/MainViewModel.Capture.SelectionPolicy.RecordingFormat.Tests.cs`
  owns recording format selection policy ownership assertions.
- `tests/Sussudio.Tests/MainViewModel.Capture.SelectionPolicy.RuntimeFlags.Tests.cs`
  owns runtime error-projection ownership assertions.
- `tests/Sussudio.Tests/MainViewModel.Capture.SelectionPolicy.Helpers.cs` owns
  shared reflection, option-list, and capture-mode model construction helpers
  for the selection-policy test family.
- `tests/Sussudio.Tests/MainViewModel.Capture.PreviewStartup.Ownership.Tests.cs`
  owns preview startup controller/adapter source ownership assertions.
- `tests/Sussudio.Tests/MainViewModel.Capture.PreviewStartup.Watchdog.Tests.cs`
  owns preview startup watchdog timeout and failure-stop contract assertions.
- `tests/Sussudio.Tests/MainViewModel.Capture.PreviewStartup.SessionReinit.Tests.cs`
  owns preview startup session-state and reinitialize-transition assertions.
- `tests/Sussudio.Tests/MainViewModel.Capture.PreviewStartup.Signals.Tests.cs`
  owns readiness-signal controller and startup/failure formatter assertions.
- `tests/Sussudio.Tests/MainViewModel.Capture.PreviewStartup.StartupStopOrdering.Tests.cs`
  owns startup discovery/probe ordering, preview reveal priming, and preview
  stop audio-ramp ordering assertions.
- `tests/Sussudio.Tests/MainViewModel.Capture.FlashbackExport.Tests.cs` owns
  Flashback export backend-lease and export-operation lock assertions.
- `tests/Sussudio.Tests/MainViewModel.Capture.FlashbackRouting.ViewModel.Tests.cs`
  owns MainViewModel Flashback coordinator-routing assertions, including the
  automation Flashback enable/restart entry-point owner.
- `tests/Sussudio.Tests/MainViewModel.Capture.FlashbackRouting.Scrub.Tests.cs`
  owns Flashback scrub, release/cancel/capture-lost, and fullscreen Flashback
  bridge assertions: shortcut gating, timeline visibility, and scrub-end
  handoff.
- `tests/Sussudio.Tests/MainViewModel.Capture.FlashbackRouting.Toggle.Tests.cs`
  owns Flashback timeline toggle rollback and lockout assertions.
- `tests/Sussudio.Tests/MainViewModel.Capture.FlashbackBackend.PreviewPipeline.Tests.cs`
  owns retained Flashback preview backend, audio restoration, and preview stop
  rollback assertions.
- `tests/Sussudio.Tests/MainViewModel.Capture.FlashbackBackend.Teardown.Tests.cs`
  owns device-switch teardown ordering between video stop, Flashback backend
  disposal, and preview reinit disposal.
- `tests/Sussudio.Tests/MainViewModel.Capture.FlashbackBackend.LifecycleLogs.Tests.cs`
  owns Flashback lifecycle outcome log-token, codec no-downgrade, export
  force-rotate, and buffer-cycle assertions.
- `tests/Sussudio.Tests/MainViewModel.Capture.FlashbackFrameRate.Tests.cs`
  owns Flashback delivered-cadence rational and enable/disable preview-state
  assertions.
- `tests/Sussudio.Tests/MainViewModel.AudioRuntime.Tests.cs` owns audio
  monitoring visual state and audio-ramp trace telemetry ownership assertions.
- `tests/Sussudio.Tests/DeviceModels.AudioInput.Tests.cs` owns audio input
  model display-name contract checks.
- `tests/Sussudio.Tests/DeviceModels.AudioLevel.Tests.cs` owns audio level
  event model contract checks.
- `tests/Sussudio.Tests/DeviceModels.CaptureDevice.Tests.cs` owns capture
  device metadata and default collection contract checks.
- `tests/Sussudio.Tests/XUnit.MediaFormatTests.cs` owns MediaFormat
  equality and hash-code contract checks.
- `tests/Sussudio.Tests/DeviceModels.PropertyAssertions.cs` owns shared device
  model property reflection assertions.
- `tests/Sussudio.Tests/SnapshotModels.Tests.cs` owns shared snapshot-model
  spec DTOs and registration state.
- `tests/Sussudio.Tests/SnapshotModels.PropertyAssertions.cs` owns shared
  snapshot property-list, nullability, and helper assertion methods.
- `tests/Sussudio.Tests/SnapshotModels.ReflectionJson.cs` owns shared
  reflection JSON round-trip and registered-property coverage helpers.
- `tests/Sussudio.Tests/SnapshotModels.Automation.SourceTelemetry.Tests.cs`
  owns source-signal projection drift guards.
- `tests/Sussudio.Tests/SnapshotModels.Automation.CpuMjpeg.Tests.cs` owns
  automation snapshot CPU MJPEG metric shape checks.
- `tests/Sussudio.Tests/SnapshotModels.Automation.Options.Tests.cs` owns
  automation options DTO shape checks.
- `tests/Sussudio.Tests/SnapshotModels.Automation.CpuMjpegContractSpec.cs` owns
  the CPU MJPEG automation snapshot property-list contract used by that check.
- `tests/Sussudio.Tests/SnapshotModels.CaptureDiagnostics.Tests.cs` owns
  CaptureDiagnosticsSnapshot default, round-trip, and reflection JSON checks.
- `tests/Sussudio.Tests/SnapshotModels.CaptureDiagnostics.PropertySpec.cs`
  owns the CaptureDiagnosticsSnapshot registered property spec.
- `tests/Sussudio.Tests/SnapshotModels.CaptureHealth.Tests.cs` owns the
  CaptureHealthSnapshot registered orchestration check;
  `SnapshotModels.CaptureHealth.Defaults.Tests.cs` owns defaults and inherited
  diagnostics assertions;
  `SnapshotModels.CaptureHealth.SourceTelemetryDetail.Tests.cs` owns
  SourceTelemetryDetailEntry direct and JSON assertions;
  `SnapshotModels.CaptureHealth.RoundTrip.Tests.cs` owns the populated
  CaptureHealthSnapshot fixture and direct round-trip assertions;
  `SnapshotModels.CaptureHealth.Json.Tests.cs` owns CaptureHealthSnapshot
  reflection JSON assertions; and `SnapshotModels.CaptureHealth.PropertySpec.cs`
  owns the CaptureHealthSnapshot and SourceTelemetryDetailEntry property-list
  contracts.
- `tests/Sussudio.Tests/SnapshotModels.SourceSignalTelemetry.Tests.cs` owns
  SourceSignalTelemetrySnapshot and SourceTelemetryDetailEntry contract checks.
- `tests/Sussudio.Tests/NativeXuAtCommandProvider.Tests.cs` owns Native XU
  telemetry provider ownership, rolling command-group split, shared snapshot
  assembly ownership, and supported 4K X product-revision checks.
- `tests/Sussudio.Tests/CaptureDiscovery.SourceOwnership.Tests.cs` owns
  DeviceService scoring and source-reader negotiation/interop ownership
  assertions.
- `tests/Sussudio.Tests/CapturePolicies.HdrOutputPolicy.Tests.cs` owns the
  offline harness HdrOutputPolicy and HDR output environment-switch checks.
- `tests/Sussudio.Tests/RecordingQueue.Tests.cs` owns shared recording queue
  source readers and source-block extraction helpers.
- `tests/Sussudio.Tests/RecordingQueue.OverloadPolicy.Tests.cs` owns the
  recording/Flashback queue overload, fatal-failure, lifecycle, and recording
  backend policy assertion. `RecordingQueue.OverloadPolicy.SourceReaders.cs`
  owns source-loading setup for the overload policy assertion.
  `RecordingQueue.OverloadPolicy.LibAvSpec.cs` owns the LibAv overload and
  queue-depth assertion subgroup.
  `RecordingQueue.OverloadPolicy.FlashbackSpec.cs` owns the Flashback overload,
  fatal-failure, queue-depth, and frame-validation assertion subgroup.
  `RecordingQueue.OverloadPolicy.FlashbackBuffer.cs` owns Flashback buffer
  recovery, eviction, active-segment, and enqueue-gating assertions.
  `RecordingQueue.OverloadPolicy.Telemetry.cs` owns unified capture, health
  snapshot, and automation formatter telemetry assertions.
  `CaptureService.RecordingOwnership.Tests.cs` owns
  CaptureService recording lifecycle, rollback, and recording outcome-state
  file-ownership assertions.
- `tests/Sussudio.Tests/RecordingQueue.LibAvSink.Tests.cs` owns LibAv recording
  sink output validation, try-enqueue, video-session setup, video-queue
  submission, queue-cleanup, drain-loop, encoding-loop, startup sequencing,
  stop-lifecycle, and lifetime-helper ownership assertions.
- `tests/Sussudio.Tests/RecordingQueue.Wasapi.Tests.cs` owns WASAPI capture-loop, hot-write,
  conversion, diagnostics, COM contract, and bounded stop assertions.
- `tests/Sussudio.Tests/RecordingQueue.CaptureFanout.Tests.cs` owns
  UnifiedVideoCapture frame-ingress, sink fan-out, and CaptureService Flashback
  backend aggregate ownership assertions.
- `tests/Sussudio.Tests/CaptureService.FlashbackOrchestrationSource.Tests.cs`
  owns the source family helper for Flashback backend orchestration partials
  and recording finalization partials plus the focused-partial ownership
  contracts, including LibAv live-preview restoration, post-recording
  microphone monitor restart, and recording outcome-state ownership.
- `tests/Sussudio.Tests/CaptureCadence.Tests.cs` owns packet-hash duplicate
  cadence and visual-cadence crop sampling assertions.
- `tests/Sussudio.Tests/UnifiedVideoCapture.Runtime.Tests.cs` owns
  UnifiedVideoCapture CPU-MJPEG format reporting and stop-failure retention
  behavior scenarios.
- `tests/Sussudio.Tests/CaptureService.RuntimeSnapshots.Tests.cs` owns
  CaptureService runtime snapshot behavior scenarios for observed formats,
  source-telemetry alignment, HDR pipeline parity, inactive thread probes, and
  runtime projection ownership for ingest/audio, reader transport, HDR pipeline,
  source telemetry, and recording integrity.
- `tests/Sussudio.Tests/CaptureService.PreviewLifecycle.Tests.cs` owns
  video-only preview fallback, missing audio endpoint, and preview backend log
  contracts.
- `tests/Sussudio.Tests/CaptureService.InitializationOwnership.Tests.cs` owns
  the CaptureService initialization focused-partial ownership contract.
- `tests/Sussudio.Tests/CaptureService.Failures.Tests.cs` owns CaptureService
  last-failure telemetry source placement, capture fatal cleanup, Flashback
  backend failure cleanup source placement, and faulted-session state
  assertions.
- `tests/Sussudio.Tests/CaptureService.SessionStateOwnership.Tests.cs` owns
  CaptureService `_sessionState` writer-file and writer-count ownership plus
  the no-session-state-write guard for Flashback backend failure cleanup.
- `tests/Sussudio.Tests/CaptureService.HealthSnapshots.Tests.cs` owns
  CaptureService health/diagnostics snapshot behavior scenarios for structured
  source telemetry and health field ownership for capture cadence, source
  telemetry, MJPEG, Flashback buffer/backend, Flashback queue state, Flashback
  export, Flashback playback, AV-sync, and recording.
- `tests/Sussudio.Tests/CaptureService.HealthSnapshots.MjpegCachedMetrics.Tests.cs`
  owns cached MJPEG timing propagation for health and diagnostics snapshots.
- `tests/Sussudio.Tests/RecordingVerifier.Integration.Tests.cs` owns shared
  fake process-supervisor, runtime-snapshot, verifier-construction, and
  verification-invocation helpers for recording verifier integration tests.
- `tests/Sussudio.Tests/RecordingVerifier.Integration.Failures.Tests.cs` owns
  ffprobe unavailable/nonzero failure scenarios.
- `tests/Sussudio.Tests/RecordingVerifier.Integration.Priority.Tests.cs` owns
  ffprobe process-priority assertions.
- `tests/Sussudio.Tests/RecordingVerifier.Integration.Codec.Tests.cs` owns
  HEVC/H264 codec success and codec-mismatch scenarios.
- `tests/Sussudio.Tests/RecordingVerifier.Integration.Flashback.Tests.cs`
  owns Flashback export/recording verification format scenarios.
- `tests/Sussudio.Tests/RecordingVerifier.Integration.Mismatches.Tests.cs`
  owns resolution and frame-rate mismatch scenarios.
- `tests/Sussudio.Tests/RecordingVerifier.Integration.Hdr.Tests.cs` owns HDR
  validation success and colorimetry/pixel-format mismatch scenarios.
- `tests/Sussudio.Tests/RecordingVerifier.Integration.Cadence.Tests.cs` owns
  NTSC frame-rate tolerance scenarios.
- `tests/Sussudio.Tests/D3D11PreviewRenderer.Geometry.Tests.cs` owns letterbox,
  black-edge counting, and PNG CRC helper contract tests.
- `tests/Sussudio.Tests/D3D11PreviewRenderer.Cadence.Tests.cs` owns present
  cadence metric shape and suppression baseline tests.
- `tests/Sussudio.Tests/D3D11PreviewRenderer.DiagnosticsContract.Tests.cs`
  owns renderer diagnostics source-shape, frame queue, frame ownership, and
  public renderer diagnostics API contract assertions.
- `tests/Sussudio.Tests/D3D11PreviewRenderer.DiagnosticsContract.SnapshotModels.Tests.cs`
  owns preview runtime, automation snapshot, nested renderer metrics, preview
  tracking, and slow-frame diagnostic reflection contracts.
- `tests/Sussudio.Tests/D3D11PreviewRenderer.DiagnosticsContract.PerformanceTimeline.Tests.cs`
  owns `PerformanceTimelineEntry` preview, Flashback, export, and process
  diagnostics reflection contracts.
- `tests/Sussudio.Tests/D3D11PreviewRenderer.DiagnosticsContract.SourceReaders.cs`
  owns source-loading setup for the preview-renderer diagnostics contract
  assertion.
- `tests/Sussudio.Tests/D3D11PreviewRenderer.SourceOwnership.ContractsAndMetrics.Tests.cs` owns
  configuration, native interop, frame type/ownership, DXGI frame-stat,
  slow-frame, and metric-tracking assertions.
  `D3D11PreviewRenderer.SourceOwnership.RenderPipeline.Tests.cs` owns panel
  binding, shared-device, device initialization, input-resource, upload,
  frame-latency, viewport, shader-rendering, and shader-source assertions.
  `D3D11PreviewRenderer.SourceOwnership.RenderThread.Tests.cs` owns
  render-thread loop/failure telemetry and Present shared present-accounting
  source-ownership assertions.
  `D3D11PreviewRenderer.SourceOwnership.RuntimeCapture.Tests.cs` owns public
  submission, lifecycle, and screenshot assertions.
- `tests/Sussudio.Tests/D3D11PreviewRenderer.DeviceLost.Tests.cs` owns device
  lost classification and recovery ownership assertions.
- `tests/Sussudio.Tests/D3D11PreviewRenderer.FrameFlow.Tests.cs` owns pending
  frame draining, frame-capture cancellation, and shared D3D device reference
  lifecycle assertions.
- `tests/Sussudio.Tests/GpuTelemetry.Nvml.Tests.cs` owns NVML snapshot
  computed-property and unit-conversion contract checks.
- `tests/Sussudio.Tests/RuntimeContracts.Tests.cs` owns RuntimePaths,
  FFmpeg runtime location, MMCSS registration, ProcessSpec, and
  ProcessRunResult contract checks.
- `tests/Sussudio.Tests/ProjectBuildContracts.Tests.cs` owns project-file build
  and publish policy contract checks.
- `tests/Sussudio.Tests/RecordingContracts.Models.Tests.cs` owns recording
  service contract DTO checks such as GpuPipelineHandles,
  RecordingContextRequest, and FinalizeResult.
- `tests/Sussudio.Tests/RecordingArtifactManager.Tests.cs` owns temp artifact
  finalize/rollback behavior for recording output cleanup.
- `tests/Sussudio.Tests/LibAvEncoder.Options.Tests.cs` owns LibAvEncoder
  ValidateOptions reflection coverage for valid options, output path and
  dimension rejection, HDR codec/P010 constraints, and frame-rate
  numerator/denominator pairing.
- `tests/Sussudio.Tests/LibAvEncoder.*.Tests.cs` and
  `tests/Sussudio.Tests/LibAvEncoder.Helpers.cs` own LibAvEncoder codec policy,
  frame-size, diagnostics, HDR metadata, output lifecycle, source-ownership,
  and shared source-reading helpers.
- `tests/Sussudio.Tests/AutomationCommandDispatcher.Tests.cs` owns the live
  dispatcher source-family reader; `AutomationCommandDispatcher.*.Tests.cs`
  and `AutomationCommandDispatcher.Helpers.cs` own authorization, manifest,
  Flashback failure response, Flashback command placement, verification command
  placement, command-kind handling, and helper coverage.
- `tests/Sussudio.Tests/AutomationCommandDispatcher.Payload.Tests.cs` owns
  dispatcher JSON payload extraction helper coverage and the trivial-handler
  payload-field parity check against `AutomationCommandCatalog`, plus the
  custom `GetAudioRampTrace.maxEntries` metadata guardrail.
- `tests/Sussudio.Tests/AutomationCommandDispatcher.Readiness.Tests.cs` owns
  dispatcher readiness gating, window close, preview health, and UI automation
  readiness-independent coverage.
- `tests/Sussudio.Tests/AutomationCommandDispatcher.ReadyIndependent.Tests.cs`
  owns ready-independent no-hardware command coverage and harness payload/fake
  device support.
- `tests/Sussudio.Tests/AutomationToolContracts.Tests.cs` owns shared
  reflection helpers for automation tool contract tests.
- `tests/Sussudio.Tests/AutomationCommandGoldenTable.cs` owns the shared golden
  automation command table used by protocol, manifest, and MCP tests.
- `tests/Sussudio.Tests/AutomationContracts.ProtocolXunit.Tests.cs` owns fast
  xUnit coverage for pure `Sussudio.Automation.Contracts` command IDs,
  manifest IDs, pipe protocol command resolution, timeout, auth-token, envelope,
  and `CommandMap` contracts.
- `tests/Sussudio.Tests/AutomationToolContracts.CommandKinds.Tests.cs` owns
  legacy harness coverage for window action enum membership and keeps the
  `ExpectedAutomationCommands()` adapter used by protocol/MCP helpers.
- `tests/Sussudio.Tests/AutomationToolContracts.Protocol.Tests.cs` owns
  legacy harness coverage for pipe-connect failure, tool delegation, script
  freshness, and response-state contract tests.
- `tests/Sussudio.Tests/AutomationToolContracts.Catalog.Tests.cs` owns
  automation command catalog, manifest, path policy, and manifest
  serialization contract tests.
- `tests/Sussudio.Tests/AutomationToolContracts.Reliability.Tests.cs` owns the
  reliability-gates script contract test.
- `tests/Sussudio.Tests/ArchitectureDocs.AgentMapReferences.Tests.cs` owns
  AGENT_MAP file/folder reference drift checks;
  `ArchitectureDocs.SourceReferencePaths.Tests.cs` owns literal `ReadRepoFile`
  source-shape path drift checks;
  `ArchitectureDocs.AgentMapOwnershipPaths.Tests.cs` owns test-owner code-span
  coverage; `ArchitectureDocs.AgentMapAutomation.Tests.cs` owns README
  automation consumer checklist coverage; `ArchitectureDocs.AgentMapPresentation.Tests.cs`
  owns UI presentation ownership code-span coverage;
  `ArchitectureDocs.CleanupPlanReferences.Tests.cs` owns cleanup-plan
  file/folder reference drift checks; `ArchitectureDocs.AgentMapCaptureRuntime.Tests.cs`
  owns CaptureService ownership-file coverage;
  `ArchitectureDocs.AgentMapToolAutomation.Tests.cs` owns exact-path coverage
  for shared tool automation partial families; and
  `ArchitectureDocs.AgentMapHelpers.cs` owns the shared AGENT_MAP token,
  consumer, and ownership-file discovery helpers.
- `tests/Sussudio.Tests/AutomationToolContracts.SnapshotFormatter*.Tests.cs`
  owns the shared/ssctl snapshot formatter contract family: typed accessors,
  core section formatting, section-order, and Flashback opt-in smoke checks
  stay in `.Tests.cs`; Flashback output rendering lives in `.Flashback.Tests.cs`,
  Preview D3D output rendering lives in `.PreviewD3D.Tests.cs`, and shared
  formatter source ownership lives in `.Ownership.Tests.cs`.
- `tests/Sussudio.Tests/Formatters.Tests.cs` owns ssctl formatted snapshot
  output smoke checks. `tests/Sussudio.Tests/Formatters.SnapshotOwnership.Tests.cs`
  owns ssctl formatter source ownership assertions, while
  `tests/Sussudio.Tests/Formatters.Timeline.Tests.cs` owns timeline table and
  summary output checks.
- `tests/Sussudio.Tests/CommandHandlers.Helpers.cs` owns source-family
  reader, routing-capture helpers, and `AssertSsctlCommandRequest`, which routes
  captured ssctl `request.command` checks through the shared golden command table
  instead of per-test numeric IDs. `CommandHandlers.Routing.Tests.cs` owns the
  pipe-captured routing contract checks for device, capture controls,
  recordings, Flashback, window, manifest, observability, automation-flow, UI
  visibility, and verification commands.
  `CommandHandlers.SourceOwnership.Tests.cs` owns ssctl handler partial-family
  source ownership assertions, and `CommandHandlers.Help.Tests.cs` owns ssctl
  help/catalog force-flag coverage.
- `tests/Sussudio.Tests/PresentMonProbe.Tests.cs` owns PresentMon parser
  behavior contracts for swap-chain selection, artifact filtering, CSV field
  versions, and app-present correlation.
- `tests/Sussudio.Tests/PresentMonProbe.SourceOwnership.Tests.cs` owns
  PresentMonProbe split-family source ownership assertions.
- `tests/Sussudio.Tests/ToolAssemblyLoading.Helpers.cs` owns shared tool
  assembly loading, isolated load contexts, freshness checks, and tool build
  command mapping used by the legacy harness and xUnit slices.
- `tests/Sussudio.Tests/HarnessCheckCatalog.cs` owns ordered offline harness
  topic sequencing, shared catalog registration helpers, and the pass-through
  routing for automation diagnostics, presentation preview, MCP diagnostics,
  and Flashback catalog groups. Keep `Program.cs` as the runner, not the
  assertion registry.
- `tests/Sussudio.Tests/HarnessCheckCatalog.CoreRuntime.cs` owns runtime,
  telemetry, recording verifier, LibAv encoder, and basic app contract check
  registration.
- Focused `tests/Sussudio.Tests/HarnessCheckCatalog.AutomationDiagnostics.*.cs`
  partials own automation-diagnostics app shell,
  MainWindow surface, dispatcher, pipe/auth, ViewModel/Flashback UI,
  capture/Flashback routing, and snapshot projection registration groups.
- `tests/Sussudio.Tests/ServiceNamespace.Tests.cs` owns the harness-visible
  service namespace/source ownership orchestrator.
- `tests/Sussudio.Tests/ServiceNamespace.FolderRules.Tests.cs` owns service
  folder-to-namespace architecture assertions and flat `Sussudio.Services`
  import bans.
- `tests/Sussudio.Tests/ServiceNamespace.Helpers.Tests.cs` owns the shared source
  enumeration, project XML, and C# comment/string stripping helpers used by
  service namespace architecture assertions.
- `tests/Sussudio.Tests/ServiceNamespace.NativeXuProbe.Tests.cs` owns
  NativeXuAudioProbe linked-source, split-source, locator, and no-reflection
  source ownership assertions.
- `tests/Sussudio.Tests/ServiceNamespace.SourceOwnership.Tests.cs` owns
  DeviceService, GPU interop, decoder, capture telemetry, audio controls,
  UI-enqueue, format-probe, and preview renderer source ownership assertions.
- `tests/Sussudio.Tests/ServiceNamespace.AutomationContracts.Tests.cs` owns
  AutomationCommandKind project/source ownership alignment across the app and
  automation tools.
- `tests/Sussudio.Tests/ServiceNamespace.ServiceContracts.Tests.cs` owns the
  app-service contract boundary assertions that keep `Sussudio/Services/Contracts`
  separate from `Sussudio.Automation.Contracts` wire/protocol ownership.
- Focused `tests/Sussudio.Tests/HarnessCheckCatalog.PresentationPreview.*.cs`
  partials own presentation-preview capture/root
  policy, MainViewModel, MainWindow, stats, D3D renderer, and preview pacing
  registration groups.
- `tests/Sussudio.Tests/PreviewPacingOwnership.Tests.cs` owns preview pacing
  classifier source ownership and automation-snapshot wiring assertions;
  `tests/Sussudio.Tests/PreviewPacingClassifier.Tests.cs` owns behavioral
  classifier cases.
- `tests/Sussudio.Tests/HarnessCheckCatalog.RecordingModels.cs` coordinates
  recording/model check registration and owns compact registration groups for
  LibAv sink, capture runtime, recording contracts/artifacts/stats, capture
  settings, recording context, device/media models, automation contracts,
  source-signal telemetry, and HDR policy checks.
  `HarnessCheckCatalog.RecordingModels.FlashbackBuffer.cs` owns the large
  Flashback buffer registration group.
- Focused `tests/Sussudio.Tests/HarnessCheckCatalog.Flashback.*.cs` partials
  own Flashback model, playback, decoder, encoder sink, and exporter
  registration groups.
- `tests/Sussudio.Tests/HarnessCheckCatalog.ToolContracts.cs` owns recording
  pipeline, NVML, capture-session/process, automation protocol, tool formatter,
  and RTK probe check registration.
- `tests/Sussudio.Tests/HarnessCore.SourceText.cs` owns shared repo-root/file
  reads and source-text extraction helpers used by harness ownership checks.
- `tests/Sussudio.Tests/HarnessCore.Reflection.cs` owns shared reflection,
  private-field/property access, enum/type lookup, and field-value fixture
  helpers.
- `tests/Sussudio.Tests/HarnessCore.ObjectFactories.cs` owns synthetic
  capture, settings, and recording-context object factories.
- `tests/Sussudio.Tests/HarnessCore.AsyncLifecycle.cs` owns capture-service
  initialization, async disposal, and polling wait helpers.
- `tests/Sussudio.Tests/HarnessCore.Assertions.cs` owns generic harness
  assertion helpers: `AssertEqual<T>`, `AssertNearlyEqual`, `AssertContains`,
  `AssertDoesNotContain`, `AssertNotNull`, and string line-ending
  normalization.
- `tests/Sussudio.Tests/MjpegTimingMetrics.Helpers.cs` owns synthetic MJPEG
  timing metric factories and the closed-pipeline emit delegate used by
  harness-level MJPEG and snapshot tests.
- `tests/Sussudio.Tests/CaptureConfigurationModels.Tests.cs` owns shared
  reflection helpers for capture configuration model contract tests.
- `tests/Sussudio.Tests/CaptureConfigurationModels.Options.Tests.cs` owns
  capture mode option display metadata and builder policy tests.
- `tests/Sussudio.Tests/CaptureConfigurationModels.Settings.Tests.cs` owns
  capture settings defaults, output path/file naming, bitrate policy,
  split-encode support, and MJPEG HFR policy tests.
- `tests/Sussudio.Tests/CaptureConfigurationModels.EncoderSupport.Tests.cs`
  owns encoder availability and preferred encoder policy tests.
- `tests/Sussudio.Tests/CaptureConfigurationModels.Flashback.Tests.cs` owns
  Flashback buffer, session, playback, export progress, segment, and request
  DTO contract tests.
- `tests/Sussudio.Tests/CaptureConfigurationModels.RecordingPipeline.Tests.cs`
  owns recording pipeline queue capacity and drop-policy tests.
- Focused capture session coordinator coverage lives in
  `tests/Sussudio.Tests/CaptureSessionCoordinator.Api.Tests.cs`,
  `CaptureSessionCoordinator.Contracts`,
  `CaptureSessionCoordinator.Queue`, `CaptureSessionCoordinator.Flashback`,
  `CaptureSessionCoordinator.Ownership`, and `CaptureSessionTransitionPolicy`
  files; command/source ownership checks include the focused
  `CaptureSessionCoordinator.Commands` partial. Shared reflective harness helpers live in
  `CaptureSessionCoordinator.Helpers.cs`.
- `tests/Sussudio.Tests/PooledVideoFrame.Tests.cs` owns shared pooled-frame
  reflection, frame factory, jitter-buffer factory, and tracking pool helpers.
- `tests/Sussudio.Tests/PooledVideoFrame.Leases.Tests.cs` owns pooled video
  frame lease lifecycle and MJPEG pooled-frame fan-out contract tests.
- `tests/Sussudio.Tests/PooledVideoFrame.MjpegJitterPolicy.Tests.cs` owns the
  MJPEG preview jitter frame-ingress, emit-loop, adaptive deadline policy,
  queue, and metrics source-ownership assertions.
- `tests/Sussudio.Tests/PooledVideoFrame.MjpegJitterQueue.Tests.cs` owns
  MJPEG preview jitter queue/drop/reprime behavior tests.
- `tests/Sussudio.Tests/PooledVideoFrame.QueuedLeaseRelease.Tests.cs` owns
  D3D pending-frame and recording/Flashback queued lease return tests.
- `tests/Sussudio.Tests/McpToolSurface.Tests.cs` owns MCP surface compatibility
  checks that span raw app state, capture options, capture settings, and UI
  settings tools. It also owns source guards that fixed-command MCP automation
  routes call `AutomationCommandKind` enum overloads at the pipe seam while
  preserving existing command labels and wire IDs.
- Keep MCP command-routing route/formatter assertions in the focused
  `CommandRouting.Capture`, `CommandRouting.Host`,
  `CommandRouting.Recording`, `CommandRouting.Formatting`,
  `CommandRouting.Device`, `CommandRouting.Pipeline`, `CommandRouting.Ui`, and
  `CommandRouting.Verification` owner files. Captured command-ID assertions use
  the shared `AssertAutomationCommandId` helper so the golden command table is
  the only test-owned numeric ID list.
- `tests/Sussudio.Tests/McpToolSurface.DiagnosticSession.Tool.Tests.cs` owns
  MCP `run_diagnostic_session` success/failure artifact contract tests.
- `tests/Sussudio.Tests/McpToolSurface.DiagnosticSession.Ownership.Tests.cs`
  owns diagnostic-session helper ownership assertions for core runner,
  scenario, cleanup, sampling, and metric collaborators.
- Diagnostic-session infrastructure ownership checks live in focused files:
  runner/initial-snapshot checks in `InfrastructureOwnership.Runner`, pipe
  retry and command-channel checks in `InfrastructureOwnership.CommandChannel`,
  run-state/live-state/context/bootstrap/output-lock checks in
  `InfrastructureOwnership.RunContext`, and scenario/completion phase checks in
  `InfrastructureOwnership.Execution`.
- `tests/Sussudio.Tests/McpToolSurface.DiagnosticSession.ResultOwnership.Tests.cs`
  owns diagnostic-session model ownership assertions, with builder, formatter,
  summary-writer/artifact, JSON/shared-text, and infrastructure assertions split
  into the adjacent `ResultOwnership.*.Tests.cs` files. Builder result assertions
  are further split across `ResultOwnership.Builder.*.Tests.cs` files by core,
  preview scheduler, overview/capture, Flashback, preview, analysis, and
  artifact handoff ownership.
- `tests/Sussudio.Tests/McpToolSurface.DiagnosticSession.Flashback.HealthPolicy.Tests.cs`
  owns diagnostic-session Flashback warmup health-policy ownership assertions.
- `tests/Sussudio.Tests/McpToolSurface.DiagnosticSession.Flashback.Scenarios.Tests.cs`
  owns diagnostic-session Flashback cycle, preview-cycle, rejected-export,
  segment-playback, recording-settings, and lifecycle scenario ownership
  assertions.
- `tests/Sussudio.Tests/McpToolSurface.DiagnosticSession.Flashback.Stress.Tests.cs`
  owns diagnostic-session Flashback stress and audio-master fallback
  classification ownership assertions.
- `tests/Sussudio.Tests/McpToolSurface.DiagnosticSession.Flashback.Metrics.Tests.cs`
  owns diagnostic-session Flashback metric projection ownership assertions.
- `tests/Sussudio.Tests/McpToolSurface.DiagnosticSession.Flashback.Waits.Tests.cs`
  owns diagnostic-session Flashback snapshot polling wait ownership assertions.
- `tests/Sussudio.Tests/McpToolSurface.DiagnosticSession.Flashback.Validation.Tests.cs`
  owns diagnostic-session Flashback warning-policy ownership assertions.
- `tests/Sussudio.Tests/McpToolSurface.DiagnosticSession.Flashback.Export.Tests.cs`
  owns diagnostic-session Flashback export ownership assertions for export
  scenario flows, export helpers, and shared segment wait/parsing collaborators.
- Focused diagnostic-session runner behavior coverage lives in
  `McpToolSurface.DiagnosticSession.Runner.Artifacts`,
  `Runner.HealthPolicy`,
  `Runner.FlashbackPlayback`, `Runner.InitialSnapshot`, `Runner.PipeRetry`,
  and `Runner.Concurrency` files that execute the reflective runner against
  synthetic command delegates.
- `tests/Sussudio.Tests/McpToolSurface.DiagnosticSession.Runner.Helpers.cs`
  owns shared reflective runner setup for diagnostic-session runner behavior
  tests: loading `ssctl`, creating `DiagnosticSessionOptions`, invoking
  `DiagnosticSessionRunner.RunAsync`, and parsing synthetic JSON responses.
- `tests/Sussudio.Tests/McpToolSurface.Performance.*.Tests.cs` owns MCP
  performance timeline contract, Flashback timeline formatting, and
  frame-pacing verdict tests, plus PresentMon MCP correlation and option
  precedence coverage.
- `tests/Sussudio.Tests/McpToolSurface.WindowPreview.*.Tests.cs` owns MCP
  wait, window action, preview toggle, Flashback toggle, screenshot, and probe
  tests.
- `tests/Sussudio.Tests/McpToolSurface.WindowPreview.Probes.Tests.cs` owns MCP
  preview color probe and video source probe formatting tests.
- `tests/Sussudio.Tests/McpToolSurface.Helpers.Process.cs`,
  `McpToolSurface.Helpers.Reflection.cs`,
  `McpToolSurface.Helpers.PipeCapture.cs`, and
  `McpToolSurface.Helpers.Assertions.cs` own shared MCP process/JSON-RPC,
  reflection/tool-result, pipe-capture, and JSON assertion helpers respectively.
- `tests/Sussudio.Tests/Flashback.Tests.cs` owns shared Flashback test helper
  source readers and helper methods only.
- `tests/Sussudio.Tests/Flashback.Buffer.Tests.cs` owns Flashback buffer option
  and initialization contract tests.
- `tests/Sussudio.Tests/Flashback.Buffer.Helpers.cs` owns shared Flashback
  buffer test factories, completed-segment insertion, and sized-file helpers.
- `tests/Sussudio.Tests/Flashback.Buffer.SourceOwnership.Tests.cs` owns
  Flashback buffer-manager partial ownership assertions.
- `tests/Sussudio.Tests/Flashback.Buffer.Segments.Tests.cs` owns Flashback
  buffer segment completion, accounting, disposal, and recovery-preserve tests.
- `tests/Sussudio.Tests/Flashback.Buffer.SegmentLookups.Tests.cs` owns
  Flashback buffer segment query, path lookup, PTS, active path, segment-count,
  and segment-list behavior tests.
- `tests/Sussudio.Tests/Flashback.Buffer.Retention.Tests.cs` owns Flashback
  buffer eviction and purge retention tests.
- `tests/Sussudio.Tests/Flashback.Buffer.EvictionPauseOwnership.Tests.cs` owns
  Flashback buffer eviction-pause ownership assertions.
- `tests/Sussudio.Tests/Flashback.Buffer.Retention.StartupCleanup.Tests.cs`
  owns Flashback buffer startup-generated segment cleanup, legacy root cleanup,
  unrelated temp-directory preservation, and startup-cache budget tests.
- `tests/Sussudio.Tests/Flashback.Buffer.Validation.Tests.cs` owns Flashback
  buffer session-id and segment-extension validation tests.
- `tests/Sussudio.Tests/Flashback.EncoderSink.Tests.cs` owns Flashback encoder
  sink frame-rate, queue, drain-loop, lifecycle, and packet-validation tests.
- `tests/Sussudio.Tests/Flashback.EncoderSink.ForceRotate.Tests.cs` owns
  Flashback encoder sink force-rotate and segment-registration recovery tests.
- `tests/Sussudio.Tests/Flashback.Exporter.Basic.Tests.cs` owns Flashback
  exporter request-surface smoke tests, export throttle, and failure
  classifier tests.
- `tests/Sussudio.Tests/Flashback.Exporter.Infrastructure.Tests.cs` owns
  Flashback exporter task-wrapper infrastructure tests.
- `tests/Sussudio.Tests/Flashback.Exporter.Ownership.Tests.cs` owns
  Flashback exporter source-ownership tests.
- `tests/Sussudio.Tests/Flashback.Exporter.Cleanup.Tests.cs` owns Flashback
  exporter orphan temp-file cleanup and output-directory scan guard tests.
- `tests/Sussudio.Tests/Flashback.Exporter.Cancellation.Tests.cs` owns
  Flashback exporter cancellation precedence and cancelled lock-wait behavior.
- `tests/Sussudio.Tests/Flashback.Exporter.Lifetime.Tests.cs` owns Flashback
  exporter disposal timeout and active native-state lifetime guards.
- `tests/Sussudio.Tests/Flashback.Exporter.SegmentPaths.Tests.cs` owns
  Flashback exporter segment path, duplicate path, and missing segment tests.
- `tests/Sussudio.Tests/Flashback.Exporter.Segments.Tests.cs` owns Flashback
  exporter range validation, buffered-packet owner assertions, and
  buffered-packet failure cleanup tests.
- `tests/Sussudio.Tests/Flashback.Exporter.Segments.Progress.Tests.cs` owns
  Flashback exporter progress and progress-adjacent cleanup/finalization source
  assertions.
- `tests/Sussudio.Tests/Flashback.Exporter.PacketTiming.Tests.cs` owns
  Flashback exporter timestamp saturation and packet timestamp normalization
  tests.
- `tests/Sussudio.Tests/Flashback.Exporter.Streams.Tests.cs` owns Flashback
  exporter stream-count bounds and template stream-copy owner/call-site tests.
- `tests/Sussudio.Tests/Flashback.Exporter.SegmentTemplate.Tests.cs` owns
  Flashback exporter segment template selection, stream-layout validation, and
  requested-segment skip policy tests.
- `tests/Sussudio.Tests/Flashback.Exporter.OutputPaths.Tests.cs` owns Flashback
  exporter output path validation, source-overwrite guards, and blocked
  temp-path tests.
- `tests/Sussudio.Tests/Flashback.Exporter.OutputFinalization.Tests.cs` owns
  Flashback exporter final-output replacement, overwrite refusal/force
  behavior, and final validation cleanup tests.
- `tests/Sussudio.Tests/Flashback.Playback.State.Tests.cs` owns Flashback
  playback state, position, snap-live, and no-op command tests.
- `tests/Sussudio.Tests/Flashback.Playback.Markers.Tests.cs` owns Flashback
  playback in/out marker API, normalization, disposal, and marker clamp tests.
- `tests/Sussudio.Tests/Flashback.Playback.Thread.Tests.cs` owns Flashback
  playback thread recovery and live-preview transition tests.
- Flashback playback command queue capacity/drop-oldest,
  scrub-coalescing source ownership, and seek-slot barrier behavior coverage live in focused
  `tests/Sussudio.Tests/Flashback.Playback.CommandQueue.Capacity.Tests.cs`,
  `CommandQueue.ScrubCoalescing`, and
  `CommandQueue.SeekSlots` owner files.
- `tests/Sussudio.Tests/Flashback.Playback.Cadence.Tests.cs` owns Flashback
  playback cadence, submit-failure, fMP4 reopen, and metrics reset tests.
- `tests/Sussudio.Tests/Flashback.Decoder.Tests.cs` owns Flashback decoder
  audio, timestamp, stream-bound, validation, lifetime, and callback tests.
- `tests/Sussudio.Tests/Flashback.Support.Tests.cs` owns cross-cutting Flashback
  support/logging contract tests.
- `Sussudio/Controllers/Flashback/FlashbackTimelineController.cs` owns Flashback
  timeline visibility, lockout, toggle synchronization, show/hide animation
  state, and timeline track layout sizing. `MainWindow.FlashbackTimeline.cs`
  is the XAML-facing adapter; command semantics live in
  `FlashbackCommandController`.
- `Sussudio/Controllers/Flashback/FlashbackScrubInteractionController.cs` owns active
  Flashback pointer-scrub state, scrub throttling, release/cancel/capture-lost
  cleanup, fullscreen scrub termination, lockout clearing, and scrub visual
  updates. `MainWindow.FlashbackScrub.cs` is the XAML-facing adapter.
  `Sussudio/Controllers/Flashback/FlashbackTimelineGeometry.cs` owns pure timeline
  fraction/duration math used by scrub and playhead presentation.
- `Sussudio/Controllers/Flashback/FlashbackPlayheadMotionController.cs` owns the
  Flashback playhead motion context, public entry points, and shared state for
  visual placement and CTI timing. `Sussudio/Controllers/Flashback/FlashbackPlayheadMotionController.Cti.cs`
  owns playback-state sampling, scrub/window gating, live right-edge pinning,
  long-horizon extrapolation scheduling, and CTI anchor timing. `Sussudio/Controllers/Flashback/FlashbackPlayheadMotionController.Visuals.cs`
  owns compositor visual setup, snap placement, magnetic pointer-scrub movement,
  linear keyframe animation, and label clamp/positioning.
  `Sussudio/MainWindow.FlashbackPlayhead.cs` is the XAML-facing adapter.
- `Sussudio/Controllers/Flashback/FlashbackMarkerPresentationController.cs` owns
  Flashback marker placement, selection-region layout, and compact duration
  text formatting. `MainWindow.Flashback.cs` is the XAML-facing marker adapter.
- `Sussudio/Controllers/Flashback/FlashbackPlaybackPresentationController.cs` owns
  Flashback play/pause glyph policy, Go Live enabled state, buffer-duration
  text, and floating playhead label text. `MainWindow.Flashback.cs` wires the
  playback presentation controller and playback UI coordinator.
- `Sussudio/Controllers/Flashback/FlashbackPlaybackUiCoordinator.cs` owns Flashback
  playback UI sequencing: track-resize snap/position/marker/CTI refresh order,
  playback state polling start/stop, buffer-fill/position/marker refresh order,
  and position-label updates with CTI re-anchor gating.
- `Sussudio/Controllers/Flashback/FlashbackCommandController.cs` owns Flashback command
  semantics for in/out points, clear, play/pause, Go Live, fullscreen keyboard
  shortcuts including left/right nudge rejection logging, export, save-last-5m,
  enable-toggle rollback, and apply/restart. `MainWindow.FlashbackCommands.cs`
  preserves the XAML event-handler surface as a thin adapter.
- `Sussudio/Controllers/Flashback/FlashbackExportProgressPresentationController.cs` owns
  Flashback export progress-bar value, visibility, and reset-on-complete
  semantics. `MainWindow.Flashback.cs` wires the export progress presentation
  controller with the rest of the Flashback presentation surface.
- `Sussudio/Controllers/Flashback/FlashbackSettingsBindingController.cs` owns Flashback
  settings-control initialization, GPU decode toggle binding/sync, buffer
  duration combo selection/sync, and buffer-duration change logging.
  `MainWindow.FlashbackSettingsBindings.cs` is the XAML-facing adapter; enable
  toggle rollback and apply/restart command behavior live in
  `FlashbackCommandController`.
- `Sussudio/Controllers/Flashback/FlashbackPollingController.cs` owns Flashback status
  and playback-position polling timers. `MainWindow.FlashbackPolling.cs` is the
  XAML-facing adapter; CTI anchor timing lives in
  `Sussudio/Controllers/Flashback/FlashbackPlayheadMotionController.Cti.cs`.
- `Sussudio/Controllers/Shell/SettingsShelfController.cs` owns settings shelf
  visibility, the animation gate, and show/hide storyboard construction.
  `MainWindow.SettingsShelf.cs` is the XAML-facing adapter.
- `Sussudio/Controllers/Launch/Splash/SplashLoadingPhraseCatalog.cs` owns splash phrase
  file lookup, Markdown-ish parsing, cached defaults, and exception fallback.
  `Sussudio/Controllers/Launch/Splash/SplashLoadingPhrasePacingPolicy.cs` owns
  randomized splash phrase interval/mode selection.
  `Sussudio/Controllers/Launch/Splash/SplashLoadingPhraseController.cs` owns
  DispatcherTimer lifecycle and two-line text animation.
- `Sussudio/Controllers/Launch/Entrance/LaunchEntranceAnimationController.cs` owns launch
  entrance context and initial hidden/scaled shell state.
  `Sussudio/Controllers/Launch/Entrance/LaunchEntranceAnimationController.Splash.cs` owns the
  splash fade, one-shot splash playback state, loading-phrase start/stop
  ordering, and handoff into shell entrance.
  `Sussudio/Controllers/Launch/Entrance/LaunchEntranceAnimationController.Shell.cs` owns shell
  chrome/button/stats entrance choreography, deferred preview reveal logging,
  active-storyboard cleanup, and the delayed control-bar shadow fade routed
  through `PreviewShadowFadeAnimator`. `MainWindow.LaunchEntrance.cs` is
  the XAML-facing adapter for launch entrance and splash phrase controller
  wiring.
- `Sussudio/Controllers/Shell/ControlBarAnimationController.cs` owns the control-bar
  button list used by launch entrance animation plus hover/press/release scale
  behavior. `MainWindow.ShellChrome.cs` is the XAML-facing adapter.
- `Sussudio/Controllers/Shell/ShellElevationController.cs` owns static shell
  ThemeShadow and translation setup for the control bar and record button.
  `MainWindow.ShellChrome.cs` is the XAML-facing adapter.
- `Sussudio/Controllers/Preview/PreviewTransitionAnimationController.cs` owns preview
  shell/content fade and scale transitions, unavailable-placeholder fades, and
  startup/unavailable presentation prep. `MainWindow.PreviewTransitions.cs` is
  the XAML-facing adapter for transition, delayed fade-in, and startup overlay
  presentation; shared video-shadow fades route through `PreviewShadowFadeAnimator`.
- `Sussudio/Controllers/Preview/PreviewButtonPresentationController.cs` owns preview
  button glyph and tooltip presentation for Start Preview and Stop Preview.
  `MainWindow.PropertyChangedPreview.cs` wires preview button presentation into
  preview lifecycle property/event routing.
- `Sussudio/Controllers/Preview/PreviewButtonActionController.cs` owns preview button
  command choreography: pending-reinit cancel, user stop intent, audio/visual
  fade-out ordering, preview start/stop calls, reinit animation reset, and
  unavailable-placeholder reveal. `MainWindow.PreviewActions.cs` keeps the XAML
  event name stable.
- `Sussudio/Controllers/Recording/RecordingStatePresentationPolicy.cs` owns pure
  recording-state lockout decisions: recording-time capture/audio control
  enablement, analog gain enablement, transition button enablement, FFmpeg
  button enablement, and settled record-button content visibility.
  `Sussudio/Controllers/Recording/RecordingStatePresentationController.cs` owns
  ViewModel-derived lockout/HDR/title/audio-meter policy application and
  delegates record-button chrome.
  `Sussudio/Controllers/Recording/Button/RecordingButtonChromeController.cs` owns demo-visible
  record-button chrome: recording glow, Rec pulse, starting spinner,
  normal/recording content, padding, enabled-state application, and the
  circle/pill width morph. `MainWindow.PropertyChangedRecording.cs` wires the
  chrome controller and recording-state presentation adapter.
- `Sussudio/Controllers/Recording/Button/RecordingButtonActionController.cs` owns the recording
  button command workflow and preview-state logging after a start.
  `MainWindow.RecordingActions.cs` is the XAML-facing adapter.
- `Sussudio/Controllers/Shell/LiveSignalInfoController.cs` owns live-signal pill
  text application, visibility state, show/hide debounce timers, and the small
  scale/fade animation. `MainWindow.LiveSignalInfo.cs` is the XAML-facing
  adapter. `Sussudio/ViewModels/LiveSignalTextPresentationBuilder.cs` owns the
  view-model live-signal label formatting and pixel-format/codec suffix policy.
- `Sussudio/Controllers/Preview/PreviewAudioFadeController.cs` owns preview-volume
  fade-in/fade-out state, saved target volume, storyboard lifetime, and volume
  save suppression. `MainWindow.PreviewAudioFade.cs` is the XAML-facing adapter.
- `Sussudio/Controllers/Preview/PreviewReinitTransitionController.cs` owns preview
  reinit animation active state, first-visual transition clears, startup-reset
  preservation, completion presentation decisions, and the
  `D3D11_RENDERER_REINIT_FLAG` / `PREVIEW_REINIT_ANIMATE_*` logs.
  `MainWindow.PreviewReinit.cs` remains the XAML/MainWindow adapter for
  renderer-stop-before-teardown and reinit completion side effects.
- `Sussudio/Controllers/Preview/Startup/PreviewStartupSessionController.cs` owns preview
  startup attempt/state bookkeeping, timestamps, cached failure/missing-signal
  details, and first-visual confirmation state. `Sussudio/MainWindow.PreviewStartup.cs`
  is the XAML/MainWindow-facing adapter that preserves logging and UI side
  effects. `Sussudio/Controllers/Preview/Startup/PreviewStartupWatchdogController.cs` owns
  watchdog/telemetry timers, timeout configuration, timeout recovery, and
  failure-stop scheduling. `Sussudio/MainWindow.PreviewStartupWatchdog.cs` wires
  the MainWindow/XAML-facing adapter and timeout diagnostic payload.
  `Sussudio/Controllers/Preview/Startup/PreviewStartupSignalCoordinator.cs` owns readiness-
  signal coordination: readiness-signal state handoff, missing-signal updates,
  playback-progress diagnostics, startup signal log strings, GPU position
  counter state, and first-visual confirmation decisions. `MainWindow.PreviewStartupSignals.cs`
  is the XAML/MainWindow-facing adapter that supplies live preview state,
  renderer visibility details, logging, and confirmation callbacks.
  `Sussudio/Controllers/Preview/Startup/PreviewStartupReadinessSignalController.cs` owns
  readiness-signal required/received state, missing-signal calculation,
  playback-advance threshold checks, and readiness result snapshots.
  `Sussudio/Controllers/Preview/Startup/PreviewStartupSignalFormatter.cs` owns missing-signal
  and signal-list string formatting.
  `Sussudio/Controllers/Preview/Startup/PreviewStartupFailureTextFormatter.cs` owns preview
  startup timeout reason, timeout status, and failure-stop status text.
  `Sussudio/Controllers/Preview/PreviewLifecycleEventController.cs` owns preview-
  specific ViewModel event lifecycle and the preview property-change router for
  preview start/stop/reinit state. `MainWindow.PropertyChangedPreview.cs` is the
  XAML/MainWindow-facing adapter that preserves event handler signatures and
  delegates into the controller.
  `Sussudio/Controllers/Preview/PreviewReinitTransitionController.cs` owns preview
  reinit animation active state, first-visual transition clears, startup-reset
  preservation, completion presentation decisions, and reinit transition logs.
  `MainWindow.PreviewReinit.cs` keeps the renderer-stop-before-teardown handoff
  and XAML presentation side effects.
  Keep preview startup fields out of the composition root.
- `Sussudio/Controllers/Preview/PreviewFadeInController.cs` owns delayed preview
  reveal after first visual: rendered-frame threshold, fade-in timer, renderer
  replacement fallback, and preview-audio fade start ordering.
  `MainWindow.PreviewTransitions.cs` wires the XAML-facing adapter. Keep
  timeout/watchdog recovery in `PreviewStartupWatchdogController`.
- `Sussudio/Controllers/Preview/Startup/PreviewStartupOverlayController.cs` owns preview-
  startup loading overlay presentation while the app waits for visual
  confirmation: ProgressRing activation, fade-in/fade-out routing, and the
  reinit-collapse opacity reset. `MainWindow.PreviewTransitions.cs` is the
  XAML-facing adapter.
- `Sussudio/Controllers/Preview/PreviewResizeTelemetryController.cs` owns top-level
  preview resize log throttling and reset state. `MainWindow.WindowSizing.cs`
  is the XAML-facing adapter for `SizeChanged`; preview surface presentation
  lives in `PreviewSurfacePresentationController`, and preview shadow visuals
  live in `PreviewSurfaceShadowController`.
- `Sussudio/MainWindow.PropertyChangedRecording.cs` owns only the
  recording-specific property-change router and adapter surface, delegating
  record-button, glow, pulse, and recording-time lockout projection through
  `RecordingStatePresentationController` and `RecordingStatePresentationPolicy`.
- `Sussudio/MainWindow.OutputPath.cs` owns the output-path property-change
  router and XAML-facing button/display adapter, delegating display updates to
  `OutputPathDisplayController` and browse/open commands to
  `OutputPathActionController`.
- `Sussudio/MainWindow.PropertyChangedCaptureOptions.cs` owns capture-option
  and source-signal property-change routing for HDR toggles, telemetry
  tooltips, source overlay updates, bitrate visibility, and show-all-options
  synchronization.
- `Sussudio/MainWindow.PropertyChangedShell.cs` owns shell visibility
  property-change routing for stats and settings chrome.
- `Sussudio/MainWindow.LiveSignalInfo.cs` owns the live source-signal
  property-change router for the live signal pill.
- `Sussudio/Controllers/Flashback/FlashbackPropertyChangedController.cs` owns
  Flashback-specific property-change routing for timeline lockout, markers,
  playhead updates, export progress, and settings-control synchronization.
  `Sussudio/MainWindow.PropertyChangedFlashback.cs` is the XAML/MainWindow
  adapter that composes the route table callbacks.
- `Sussudio/Controllers/Audio/AudioControlPresentationController.cs` owns audio and
  microphone property-change projections: audio toggles, monitoring meter
  state, preview volume slider sync, microphone enablement, and microphone
  volume sync. `Sussudio/MainWindow.PropertyChangedAudio.cs` owns the audio
  property-change router and XAML-facing adapter.
- `Sussudio/Controllers/Audio/MicrophoneControlsController.cs` owns microphone volume
  slider synchronization, save triggers, shelf enablement, and mic-meter row
  animation state. `MainWindow.MicrophoneControls.cs` is the XAML-facing adapter.
- `Sussudio/Controllers/Shell/ControlBarLabelVisibilityController.cs` owns applying
  responsive visibility for the complete control-bar label set.
  `Sussudio/Controllers/Shell/ResponsiveShellLayoutController.cs` owns applying
  capture-settings grid placement to XAML elements.
  `Sussudio/Controllers/Shell/ResponsiveShellLayoutPolicy.cs` owns
  the control-bar label breakpoint and narrow/wide placement policy.
  `MainWindow.ResponsiveShellLayout.cs` is the XAML-facing adapter.
- `Sussudio/Controllers/Capture/CaptureSelectionBindingController.cs` owns
  the capture-selection binding controller shell, context lifetime, and XAML
  control dependency bag, capture/audio/microphone/encoder collection wiring,
  collection-change debounce/queued sync, and available-option property-change
  rebinding,
  `Sussudio/Controllers/Capture/CaptureSelectionBindingController.DeviceSelection.cs`
  owns capture-device ComboBox/ViewModel synchronization, pending-device apply
  state, and selected-device property-change mismatch logging,
  `Sussudio/Controllers/Capture/CaptureSelectionBindingController.AudioSelection.cs`
  owns audio-input and microphone ComboBox/ViewModel synchronization,
  `Sussudio/Controllers/Capture/CaptureSelectionBindingController.CaptureModeSelection.cs`
  owns resolution and frame-rate ComboBox/ViewModel synchronization,
  `Sussudio/Controllers/Capture/CaptureSelectionBindingController.RecordingSelection.cs`
  owns recording format/quality/preset/split-encode ComboBox synchronization
  and shared string ComboBox selection application,
  `Sussudio/Controllers/Capture/CaptureComboBoxSelectionNormalizer.cs` owns pure
  capture/audio/microphone/resolution/frame-rate/string ComboBox
  selection and fallback matching, and
  `Sussudio/Controllers/Capture/CaptureSelectionBindingController.DeviceAudio.cs` owns
  device-audio mode/gain control projection.
  `Sussudio/Controllers/Capture/CaptureSelectionBindingController.PropertyChanges.cs`
  owns the capture-selection `PropertyChanged` router.
  `MainWindow.CaptureSelectionBindings.cs` is the XAML-facing adapter.
- `Sussudio/Controllers/Audio/AudioControlBindingController.cs` owns the audio-control
  binding context, initial audio/microphone projection, preview-volume binding
  and priming, audio/microphone/device-audio selection handlers,
  record/preview/custom-audio/microphone toggle handlers, audio-meter
  activation, initial meter presentation, and device-audio gain/meter resize
  hooks.
  Device-audio mode/gain control projection stays in
  `Sussudio/Controllers/Capture/CaptureSelectionBindingController.DeviceAudio.cs`.
  `Sussudio/MainWindow.AudioBindings.cs` is its XAML-facing adapter.
- `Sussudio/Controllers/Capture/CaptureDeviceActionController.cs` owns the capture-
  device refresh/apply button workflows and preserves the explicit apply/reinit
  path. `MainWindow.CaptureDeviceActions.cs` is the XAML-facing adapter.
- `Sussudio/Controllers/Capture/CaptureOptionPresentationPolicy.cs` owns pure
  capture-option presentation decisions: HDR toggle enablement, MJPEG decoder
  count visibility, bitrate/preset visibility, audio clipping visibility, and
  initial decoder-count clamping. `CaptureOptionPresentationController.cs`
  owns XAML control application, decoder-count selection handling, and
  delegation to pure policy/tooltip helpers.
  `Sussudio/Controllers/Capture/CaptureOptionTooltipFormatter.cs` owns pure HDR hint
  and FPS telemetry tooltip text policy.
  `MainWindow.CaptureOptionPresentation.cs` is the XAML-facing adapter.
- `Sussudio/Controllers/Capture/CaptureOptionBindingController.cs` owns the capture
  option binding adapter in one file: XAML/view-model adapter context,
  initialization, resolution/frame-rate selection, recording option event
  bindings, HDR/true-HDR forwarding, and show-all option binding, reusing
  `CaptureComboBoxSelectionNormalizer` for shared frame-rate auto/exact
  matching.
  `MainWindow.CaptureOptionBindings.cs` is the XAML-facing capture and
  recording option adapter.
- `Sussudio/Controllers/Recording/Output/OutputPathDisplayController.cs` owns recording output-
  path textbox, tooltip, and resize-event updates.
  `Sussudio/Controllers/Recording/Output/OutputPathDisplayTextFormatter.cs` owns pure output-
  path truncation text policy. `MainWindow.OutputPath.cs` is the XAML-facing
  adapter used by binding setup and property changes.
- `Sussudio/Controllers/Recording/Output/OutputPathActionController.cs` owns recording output-
  path browse/open-recordings button workflows. `MainWindow.OutputPath.cs` is
  the XAML-facing adapter.
- `Sussudio/ViewModels/MainViewModel.*.cs` for root presentation state and
  automation-facing compatibility. `MainViewModel.cs` owns compatibility-facade
  construction, dependency assignment, collaborator construction, and small bridge
  methods. `MainViewModel.State.cs` owns shared shell/status/live-info flags
  and non-preview coordination gates; `MainViewModel.PreviewState.cs` owns
  preview lifecycle flags, preview reinitialize coordination, and preview
  request events; `MainViewModel.CaptureState.cs` owns capture-selection, source
  telemetry, and HDR state; `MainViewModel.AudioState.cs` owns audio,
  microphone, and device-audio state; `MainViewModel.FlashbackState.cs` owns
  Flashback timeline/export state. `MainViewModel.AudioMeters.cs` owns live
  audio/microphone meter callback state; keep callback-thread meter targets
  out of the root facade file. `AudioRampTraceRecorder.cs` owns the audio ramp
  diagnostic ring buffer and sampler, while `MainViewModel.AudioRampTrace.cs`
  keeps the automation-facing adapter methods. `PreviewAudioVolumeTransitionController`
  owns preview-volume save suppression/override state plus the preview-audio
  ramp constants, easing, priming, restoring, and property-to-session volume
  forwarding. `MainViewModel.AudioMonitoring.cs` owns preview monitoring
  call sites and coordinator sequencing. `MainViewModel.AudioInputSelection.cs`
  owns audio input retargeting and preview-monitoring ramp handoff.
  `MainViewModel.MicrophoneVolume.cs` owns microphone endpoint volume
  synchronization and persistence.
  `MainViewModel.DeviceAudioRefresh.cs` owns device-native audio-control support
  probing, readback, and pending saved-state reconciliation.
  `MainViewModel.DeviceAudioMode.cs` owns device-native audio mode switching
  and failure readback through the supported native-XU switch command surface,
  not the legacy AT input-source fallback path. `MainViewModel.AudioControls.cs`
  owns shared audio-control guards and mode normalization.
  `MainViewModel.AnalogAudioGain.cs` owns analog
  gain XU writes, flash-persist debounce, and settings persistence.
  `Sussudio/ViewModels/DeviceAudioGainMapper.cs` owns the pure percent-to-XU-
  byte analog gain curve used by device-native gain application.
  `MainViewModel.AudioControlCancellation.cs` owns cancellation cleanup for
  pending device-audio refresh, mode, XU gain, and flash-persist work.
  `MainViewModel.AudioPropertyChanges.cs` owns audio capture/preview property
  handlers. `MainViewModel.AudioInputPropertyChanges.cs` owns custom audio
  input property handlers. `MainViewModel.MicrophonePropertyChanges.cs` owns
  microphone monitor and selected-microphone property handlers.
  `MainViewModel.DeviceAudioPropertyChanges.cs` owns device-native audio mode
  and analog gain property handlers. `MainViewModel.CaptureModePropertyChanges.cs`
  owns capture-mode property handlers for selected resolution, selected format,
  selected video format, and MJPEG decoder count changes.
  `Sussudio/Controllers/ViewModel/MainViewModelUiDispatchController.cs` owns
  shared view-model UI dispatcher enqueue/invoke policy, disposal skip logging,
  cancellation handoff, enqueue-failure logging, and status projection.
  `MainViewModel.Dispatching.cs` owns the stable private adapter names plus
  preview event fan-out for the partial family.
  `Sussudio/Controllers/ViewModel/MainViewModelRuntimeLifecycleController.cs`
  owns runtime event subscription/unsubscription, system-resume preview rebind
  handling, capture status/error fan-out, capture pre-cleanup renderer stop
  fan-out, frame-captured callbacks, periodic timer refresh orchestration, and
  initial source-telemetry/HDR/live-info/timer/disk-space bootstrap.
  `Sussudio/Controllers/ViewModel/MainViewModelDeviceFormatProbeController.cs`
  owns late device-format probe event ingress, UI enqueue/generation checks,
  selected-device capability refresh, UI-side retarget application, session
  mismatch checks, and active-capture restore.
  `MainViewModel.RecordingRuntime.cs` owns recording-runtime counters and the DiskSpaceInfo assignment bridge,
  while `Sussudio/ViewModels/OutputDriveSpacePresentationBuilder.cs` owns output drive probing,
  fallback, formatting, and suppressed-warning logging.
  `MainViewModel.RecordingRuntime.cs` owns
  recording size/bitrate projection and recording-state reset reactions.
  `MainViewModel.LiveSignalPresentation.cs` owns live-capture info projection
  from `CaptureRuntimeSnapshot`, including audio-preview activity and
  live-resolution/frame-rate/pixel-format assignment, preview-stop live-info
  reset, and delegates label formatting to
  `Sussudio/ViewModels/LiveSignalTextPresentationBuilder.cs`.
  `MainViewModel.CaptureSettings.cs` owns capture settings projection from UI
  selection and observed runtime/source state, while
  `MainViewModel.CaptureSettingsFrameRate.cs` owns frame-rate request
  projection: selected option seed, auto-resolved effective FPS, negotiated
  rational/source-telemetry overrides, and rational/decimal fallbacks.
  `MainViewModel.Capture.cs` is the compatibility facade for device
  initialization, preview start/stop, selected-device apply, and preview
  reinitialization. `Sussudio/Controllers/ViewModel/MainViewModelPreviewLifecycleController.cs`
  owns the underlying preview lifecycle operations: device initialization,
  preview start/stop, selected-device apply, debounced reinitialization,
  Flashback-cycle wait-before-reinit, renderer-stop handoff, teardown restart,
  and reinit gate release.
  `MainViewModel.RecordingLifecycle.cs` owns the stable recording facade:
  toggle, desired-state, graceful-stop, and emergency-stop entry points.
  `Sussudio/Controllers/ViewModel/MainViewModelRecordingTransitionController.cs`
  owns recording toggle serialization, desired-state routing, graceful stop,
  emergency stop, transition gating, concrete start/stop operation execution,
  and failure/cancellation state repair.
  `MainViewModel.RecordingState.cs` owns recording option selections, output
  path, counters, and observable transition flags.
  `MainViewModel.Disposal.cs` owns bounded teardown, watcher disposal, and
  export-cancellation cleanup.
  `MainViewModel.AutomationSnapshots.cs` owns automation-facing capture runtime,
  health, and recording snapshot projection. `MainViewModel.AutomationSnapshots.cs`
  also owns automation-facing source/preview probes and preview frame capture.
  `MainViewModel.ViewModelRuntimeSnapshot.cs` owns automation-facing view-model runtime snapshot UI-thread capture.
  `ViewModelRuntimeSnapshotBuilder.cs` owns pure view-model runtime snapshot DTO construction.
  `MainViewModel.AutomationOptionsSnapshot.cs` owns automation-facing options
  UI-thread snapshot capture for CLI/MCP clients, while
  `AutomationOptionsSnapshotBuilder.cs` owns the pure selected-control-state DTO
  construction.
  `MainViewModel.FlashbackPlaybackCommands.cs` owns Flashback playback, scrub,
  nudge, marker, and automation action command routing plus rejection status
  projection. `MainViewModel.FlashbackPlayback.cs` owns read-only Flashback
  playback snapshot access plus buffer, bitrate, playback-state, in/out marker,
  and gap-from-live UI projection. `MainViewModel.FlashbackExport.cs` owns
  Flashback UI export commands, save-picker flow, active-export guard, and
  user-facing export result/status handling.
  `MainViewModel.FlashbackExportOperation.cs` owns shared Flashback export
  operation lifecycle: outcome classification, core export execution,
  current-operation checks, progress/cancellation handoff, and CTS cleanup.
  `MainViewModel.FlashbackExportAutomation.cs` owns automation-facing Flashback
  export command execution, linked cancellation, and dispatcher cleanup.
  `MainViewModel.FlashbackPlayback.cs` also owns read-only Flashback segment
  projection for UI, CLI, and MCP callers.
  `MainViewModel.FrameRateOptions.cs` owns frame-rate selection reactions and
  auto-selection entry points. `MainViewModel.FrameRateOptionRebuild.cs` owns
  frame-rate option rebuilding and observable collection mutation.
  `MainViewModel.FrameRateAutoSelectionPolicy.cs`
  owns pure frame-rate option choice: pending SDR bucket preference,
  Source-rate nearest match with timing-family tie-break, generic auto fallback,
  and previous/manual selection fallback.
  `MainViewModel.ModeSelectionState.cs` owns shared frame-rate selection reset,
  resolved automatic frame-rate application, disabled frame-rate reason
  projection, and capture-mode reset flags.
  `MainViewModel.FrameRateSourceFilterPolicy.cs` owns source-rate filtering and
  `ShowAllCaptureOptions` unlock policy. `MainViewModel.CaptureOptionVisibility.cs`
  owns `ShowAllCaptureOptions` change handling and deferred rebuild behavior.
  `Sussudio/ViewModels/FrameRateTimingPolicy.cs` owns pure frame-rate timing
  family and variant models, rational parsing, friendly/exact frame-rate
  matching, timing-family ranking, and preferred-format ranking helpers used by
  frame-rate, resolution, capture-settings, and automation projections.
  `MainViewModel.FrameRateTiming.cs` owns the stateful wrappers that resolve
  timing variants and source/preferred timing from resolution capabilities,
  runtime snapshots, selected formats, source telemetry, and UI selection state.
  `MainViewModel.FormatSelection.cs` owns pixel-format option building and
  selected capture-format selection policy.
  `MainViewModel.RecordingFormatOptions.cs` owns recording-format policy
  application to observable state. `MainViewModel.HdrModeChanges.cs` owns
  HDR toggle side effects: recording-time revert/status, mode option rebuilds,
  immediate reinitialize scheduling, and settings persistence.
  `Sussudio/ViewModels/RecordingSettingsSelectionPolicy.cs` owns pure recording
  codec filtering, selected-codec fallback policy, string-to-model format/quality
  parsing, and custom bitrate clamp policy shared by UI and automation.
  Video device enumeration and collection replacement stay in
  `MainViewModel.DeviceManagement.cs`; selected capture-device reactions,
  capability projection, source telemetry reset, and device-native audio-control
  refresh handoff live in `MainViewModel.DeviceSelection.cs`; capture-mode property-change hooks live
  in `MainViewModel.CaptureModePropertyChanges.cs`; startup audio-list and
  watcher-driven audio endpoint refresh adaptation lives in `MainViewModel.AudioDeviceDiscovery.cs`.
  `Sussudio/ViewModels/AudioDeviceSelectionPolicy.cs` owns pure capture-card
  endpoint filtering plus previous/saved/default audio and microphone selection
  fallback policy.
  `Sussudio/Controllers/ViewModel/MainViewModelDeviceFormatProbeController.cs`
  owns late device-format probe reconciliation, format collection mutation,
  capability refresh after background probes, enqueue/failure logging, UI-side
  late-probe retarget application, HDR/SDR reinitialize dispatch, MJPG HFR
  preserve, session mismatch check, and active-capture restore.
  `Sussudio/ViewModels/DeviceFormatProbeRetargetPolicy.cs`
  owns the pure late-probe decision policy for HDR retarget, SDR NV12 retarget,
  MJPG HFR preservation, session mismatch, and active-capture restore.
  `MainViewModel.AutoResolutionOptions.cs` owns automatic resolution dropdown
  option construction. `MainViewModel.ResolutionOptions.cs` owns the automatic
  resolution-selection adapter over current ViewModel state plus resolution
  dropdown mutation.
  `Sussudio/ViewModels/AutoCaptureSelectionPolicy.cs` owns automatic resolution
  ranking and source-aware frame-rate selection.
  `MainViewModel.AutoResolutionState.cs` owns effective Source resolution state,
  auto-value detection, and effective resolution query helpers.
  `MainViewModel.AutoResolutionPresentation.cs` owns
  auto-resolution display text used by status and telemetry presentation.
  `Sussudio/ViewModels/CaptureResolutionSelectionPolicy.cs` owns the pure
  resolution selection facade. `CaptureResolutionSelectionPolicy.Support.cs`
  owns parsing and frame-rate support checks. `CaptureResolutionSelectionPolicy.Ranking.cs`
  owns nearest-resolution ranking.
  `CaptureResolutionSelectionPolicy.Source.cs` owns source-telemetry-aware
  resolution matching, `CaptureResolutionSelectionPolicy.Hdr.cs` owns HDR
  frame-rate-preserving retarget and support-hint selection,
  `CaptureResolutionSelectionPolicy.Sdr.cs` owns SDR auto/fallback resolution
  selection, and `CaptureResolutionSelectionPolicy.Models.cs` owns the policy
  request/result records.
  `MainViewModel.ResolutionSelectionPolicy.cs` delegates state-backed
  capability queries to that helper.
  `MainViewModel.Telemetry.cs` owns source telemetry projection and
  source-aware auto-retargeting hints. `MainViewModel.TargetPresentation.cs`
  owns HDR runtime state/readiness projection from capture runtime snapshots
  and target-summary property application.
  `Sussudio/ViewModels/SourceTelemetryPresentationBuilder.cs`
  owns source telemetry summary, telemetry age, and target-summary display text formatting.
  `MainViewModel.Settings.cs` owns settings initialization and simple
  persistence reactions. `MainViewModel.SettingsPersistence.cs` owns the impure
  settings load/save adapter, while `MainViewModelSettingsPersistenceProjection.cs`
  owns persisted-settings validation, clamping, deferred-selection handoff, and
  save DTO projection.
  `MainViewModel.FlashbackEncoderSettings.cs` owns active
  Flashback reactions to recording-format, encoder quality/preset/split, and
  bitrate changes. `MainViewModel.FlashbackSettings.cs` owns active Flashback
  reactions to buffer-duration and GPU-decode setting changes.
  `MainViewModel.AutomationUi.cs` owns UI-only automation mutators
  for settings visibility, Flashback timeline visibility, and show-all capture
  options. `MainViewModel.AutomationStatsUi.cs` owns stats dock/section
  visibility and frame-time overlay display. `MainViewModel.AutomationAudio.cs`
  owns automation command entry points for audio enablement, audio-preview
  enablement, preview-volume clamp/persist, device-native mode/gain
  application, and microphone enablement with recording-time refusal and
  idempotent handling.
  `MainViewModel.AutomationPreview.cs` owns automation
  preview enable/disable idempotence, pending-reinit cancellation, and
  start/stop routing. `MainViewModel.AutomationHdr.cs` owns automation HDR and
  true-HDR preview recording-time guard enforcement and availability checks.
  `MainViewModel.AutomationFlashback.cs` owns automation Flashback
  enable/restart routing through the capture session coordinator.
  `MainViewModel.AutomationDeviceSelection.cs` owns automation device refresh
  and capture-device selection. `MainViewModel.AutomationAudioInputSelection.cs`
  owns audio-input selection and custom audio-input enablement.
  `MainViewModel.AutomationCaptureSettings.cs` owns capture resolution,
  frame-rate, video-format, MJPEG decoder worker-count automation, and the
  shared reinitialization gate used after active capture-mode changes.
  `MainViewModel.AutomationRecordingSettings.cs` owns recording format
  automation, HDR compatibility enforcement, recording encoder quality,
  NVENC split-encode mode, custom encoder bitrate clamp policy, encoder
  preset, and output-path automation mutation.
  `MainViewModel.RecordingCapabilityRefresh.cs` owns startup FFmpeg capability
  probes for recording formats and split-encode modes.

Refactor direction:

- Keep `MainWindow.xaml.cs` as a shell/composition root over time.
- `MainWindow.xaml.cs` owns only construction and phased controller
  initialization. Keep the phase methods grouped by runtime surface
  (window/shell, Flashback, presentation, preview, recording, launch/status,
  preview actions, audio, capture, output) so adding a controller does not turn
  the root back into an undifferentiated list.
- Keep `MainWindow.*` partials thin as XAML adapters over named controllers.
  Preview startup, preview runtime snapshot dispatch/sampling, MainWindow UI
  dispatching, stats projection, and Flashback playback/export presentation
  already have named owners; start the next UI cleanup from remaining broad
  adapters not covered by controller ownership tests.
- Keep `MainViewModel` as a compatibility facade while moving feature state to
  capture, recording, audio, Flashback, diagnostics, and automation adapters.
- `MainViewModelDependencies.cs` owns the default service graph for the root
  compatibility view model until a fuller app composition root injects feature
  view models and narrower ports.

## Tooling And Diagnostics

Primary owners:

- `tools/ssctl/` for the preferred CLI.
- `tools/McpServer/` for MCP bridge tools.
- `tools/Common/` for shared tool helpers that are not contracts, including
  pipe client, snapshot formatting, diagnostic sessions, diagnostic scenario
  cataloging, diagnostic-session pipe retry policy, PresentMon probing, and
  shared JSON options.
- `tools/Common/AutomationPipeClient/` owns the shared pipe-client helper family
  used by ssctl, MCP, diagnostic sessions, and smoke tools.
- `tools/Common/AutomationPipeClient/AutomationPipeClient.Transport.cs` owns
  named-pipe connect orchestration, request/response framing, and response
  timeout.
- `tools/Common/AutomationPipeClient/AutomationPipeClient.ConnectErrors.cs` owns
  pipe connect failure classification and exact CLI/MCP diagnostic error codes.
- `tools/Common/AutomationPipeClient/AutomationPipeClient.Commands.cs` owns
  command envelope sending and `not_ready` retry behavior.
- `tools/Common/AutomationPipeClient/AutomationPipeClient.ResponseState.cs` owns
  tolerant response state parsing.
- `tools/Common/AutomationPipeClient/AutomationPipeClient.Models.cs` owns pipe
  command result and exception types.
- `tools/Common/AutomationPipeClient/AutomationSyntheticErrorResponse.cs` owns
  the shared structured error-envelope factory and common transport/protocol
  exception-to-envelope mapper used by ssctl and MCP adapters. Adapter-specific
  unknown-command policy stays at the ssctl/MCP edges.
- Fixed MCP routes whose commands exist in `AutomationCommandKind` should call
  the typed MCP `PipeClient.SendCommandAsync(AutomationCommandKind, ...)`
  overload at the pipe seam. Do not list converted routes here; the shared
  catalog, per-file MCP owner bullets, and `McpToolSurface.*` source guards are
  the source of truth. String command names remain only for catalog/manifest-backed
  dynamic batches and diagnostic-session command callbacks.
- `tools/Common/AutomationPipeClient/AutomationResponseState.cs` owns tolerant
  response-state DTOs shared by the pipe client and tool surfaces.
- `tools/AutomationClient/Program.cs` owns the low-level pipe client entry
  flow, cancellation handling, shared-protocol command resolution, timeout
  selection, response printing, and the local options DTO for scripts and ad
  hoc automation calls.
- `tools/AutomationClient/Program.Arguments.cs` owns AutomationClient flag
  parsing and help text.
- `tools/AutomationClient/Program.Payload.cs` owns AutomationClient JSON,
  base64, and key/value payload construction.
- `tools/AutomationClient/README.md` owns AutomationClient usage notes.
- `tools/send-automation-command.ps1` owns the PowerShell helper wrapper and
  its AutomationClient rebuild freshness inputs.
- `tools/ssctl/CommandHandlers.cs` owns top-level CLI routing only.
- `tools/ssctl/CommandHandlers.Observability.cs` owns state, diagnostics,
  options, manifest, timeline, memory, and audio-ramp commands.
  `tools/ssctl/CommandHandlers.PresentMon.cs` owns `presentmon` command
  parsing, swap-chain discovery, and probe invocation.
  `tools/ssctl/CommandHandlers.DiagnosticSession.cs` owns
  `diagnostic-session` command parsing and runner invocation.
- `tools/ssctl/CommandHandlers.CaptureControls.cs` owns preview/record,
  screenshot/frame capture, and `set` capture/audio/output mutations, including
  the shared set-value payload helper. Fixed ssctl automation routes should
  call shared enum overloads with `AutomationCommandKind` values; labels and
  wire command IDs remain catalog owned. Dynamic diagnostic-session runner
  command names stay string-based at the transport seam.
- `tools/ssctl/CommandHandlers.Device.cs` owns device refresh/list/select,
  audio-input selection, and custom-audio enablement.
- `tools/ssctl/CommandHandlers.Window.cs` owns window close arming, window
  state/geometry actions, fullscreen toggles, snap commands, and the
  recordings-folder CLI command.
- `tools/ssctl/CommandHandlers.AutomationFlow.cs` owns wait/assert/probe
  scripting flow commands.
- `tools/ssctl/CommandHandlers.UiVisibility.cs` owns stats, settings, and
  frame-time visibility commands.
- `tools/ssctl/CommandHandlers.Verification.cs` owns recording/file
  verification commands.
- `tools/ssctl/CommandHandlers.Flashback.cs` owns Flashback enablement,
  timeline, segment, restart, and top-level Flashback command routing.
  `tools/ssctl/CommandHandlers.Flashback.Actions.cs` owns Flashback playback,
  scrub, marker/range, position parsing, and `FlashbackAction` payload shaping.
  `tools/ssctl/CommandHandlers.Flashback.Export.cs` owns Flashback export CLI
  flag parsing, output-path defaulting, parent-directory creation, and
  `FlashbackExport` payload shaping.
- `tools/NativeXuAudioProbe/Program.cs` owns probe command routing and command
  workflows; `Program.Models.cs` owns probe experiment/readback DTOs and
  result-diff records; `Program.Commands.cs` owns Native XU command IDs and
  shared raw-payload formatting;
  `Program.AtCommands.cs` owns direct AT read/write/input subcommands;
  `Program.DefaultExperiment.cs` owns the default baseline/experiment/restore
  runner;
  `Program.I2cCommands.cs` owns the exploratory `i2c-cmd` router/basic
  get/set/scan paths; `Program.I2cCommands.SelectorProbe.cs` owns selector
  transport probing for that command family;
  `Program.I2cCommands.HighSelectorProbe.cs` owns high-selector probing;
  `Program.I2cCommands.TopologyProbe.cs` owns topology/property-set probing;
  `Program.I2cCommands.Verify.cs` owns I2C SET/readback/restore verification;
  `Program.I2cSwitch.cs` owns the captured audio-switch replay workflow;
  `Program.ExperimentPayloads.cs` owns experiment payload construction;
  `Program.I2cTransport.cs` owns I2C-over-AT transport helpers; and
  `Program.ServiceProbe.cs` owns service-control smoke/payload workflows.
- `tools/KsAudioNodeProbe/Program.cs` owns KS audio node probe command flow;
  `Program.Constants.cs` owns probe constants; `Program.NativeTypes.cs` owns
  native interop DTOs; and `Program.NativeInterop.cs` owns SetupAPI, file-handle,
  KS property transfer, topology enumeration, and Win32 formatting helpers.
- `tools/ssctl/Program.cs` owns the process entry point, Ctrl-C cancellation,
  CLI option parsing, and exit-code shaping.
- `tools/ssctl/SsctlHelpWriter.cs` owns the `ssctl` help facade.
  `tools/ssctl/SsctlHelpWriter.Sections.cs` owns operator-facing help section
  text and catalog-backed CLI help lines.
- `tools/ssctl/CommandHandlers.Context.cs` owns the per-invocation command
  context wrapper.
- `tools/ssctl/CommandHandlers.Flags.cs` owns flag consumption and optional
  flag value parsing. `CommandHandlers.Arguments.cs` owns usage validation,
  required words, and argument joining. `CommandHandlers.Json.cs` owns
  command-handler JSON detection/pretty-printing. `CommandHandlers.Values.cs`
  owns primitive parsing, Flashback numeric validation, on/off and show/hide
  parsing, recording format normalization, snap action mapping, and assertion
  value parsing.
- `tools/ssctl/CommandHandlers.Transport.cs` owns shared command sending,
  `AutomationCommandKind` command resolution for handlers, and response
  exit-code shaping; command-family payload helpers stay with their owning
  command partials.
- The `tools/ssctl/Formatters.*.cs` partial family is the console projection
  facade only.
- `tools/ssctl/Formatters.Snapshot.cs` owns app snapshot orchestration and
  section ordering only.
- `tools/ssctl/Formatters.Snapshot.CoreSections.cs` owns the Sussudio state,
  capture-command summary, audio, recording/output, legacy performance, and
  Memory/GC embedded snapshot text.
- `tools/ssctl/Formatters.Snapshot.CaptureSettings.cs` owns capture settings
  text and friendly/exact frame-rate summary formatting.
- `tools/ssctl/Formatters.Snapshot.VideoPipeline.cs` owns video ingest,
  recording queue, encoder failure, GPU/CUDA queue, freshness, and video
  diagnostic snapshot text.
- `tools/ssctl/Formatters.Snapshot.DiagnosticLanes.cs` owns diagnostic health,
  summary, evidence, and frame-lane snapshot text.
- `tools/ssctl/Formatters.Snapshot.CaptureCadence.cs` owns capture cadence,
  packet fingerprint, and visual-cadence snapshot text.
- `tools/ssctl/Formatters.Snapshot.AvSync.cs` owns embedded snapshot AV-sync
  drift text.
- `tools/ssctl/Formatters.Snapshot.Flashback.cs` owns Flashback snapshot
  active/failure gating, section ordering, encoder, buffer, temp-cache,
  queue-latency, backpressure, failure, GPU queue, playback state,
  command-queue, cadence, decode, frame, stage, A/V drift, and export
  progress/result snapshot text.
- `tools/ssctl/Formatters.Snapshot.Mjpeg.cs` owns MJPEG timing snapshot text.
- `tools/ssctl/Formatters.Snapshot.Preview.cs` owns preview renderer-mode
  routing, GPU-media-source text, and non-D3D preview fallback text.
- `tools/ssctl/Formatters.Snapshot.PreviewD3D.cs` owns D3D preview renderer
  snapshot text: routing/header order, CPU timing, pipeline-latency,
  frame-ownership, frame-latency wait, DXGI frame-stat text, and delegation to
  the shared slow-frame formatter.
- `tools/ssctl/Formatters.Snapshot.ThreadHealth.cs` owns source-reader and
  WASAPI thread-health snapshot text.
- `tools/ssctl/Formatters.Snapshot.Source.cs` owns source telemetry snapshot
  text.
- `tools/ssctl/Formatters.Diagnostics.cs` owns recent diagnostic-event output.
- `tools/ssctl/Formatters.Options.cs` owns capture option and device lists.
- `tools/ssctl/Formatters.Timeline.cs` owns performance timeline response
  validation and top-level orchestration.
- `tools/ssctl/Formatters.Timeline.Rows.cs` owns performance timeline JSON row
  projection; `tools/ssctl/Formatters.Timeline.Rows.Model.cs` owns the private
  row model.
- `tools/ssctl/Formatters.Timeline.Rendering.cs` owns performance timeline
  table output.
- `tools/ssctl/Formatters.Timeline.Summaries.cs` owns first-vs-last trend
  summary text.
- `tools/ssctl/Formatters.Memory.cs` owns standalone memory and GC summaries.
- `tools/ssctl/Formatters.Common.cs` owns shared result/JSON helpers.
- `tools/McpServer/Tools/PerformanceTimelineTools.cs` owns the public MCP
  tool entry point and command response handling.
- `tools/McpServer/Tools/PerformanceTimelineTools.Rows.cs` owns timeline JSON
  row projection; `tools/McpServer/Tools/PerformanceTimelineTools.Rows.Model.cs`
  owns the private row model.
- `tools/McpServer/Tools/PerformanceTimelineTools.Rendering.cs` owns timeline
  table text rendering. `tools/McpServer/Tools/PerformanceTimelineTools.Rendering.Trend.cs`
  owns first-vs-last trend text and target-summary orchestration.
- `tools/McpServer/Tools/PerformanceTimelineTools.Formatting.cs` owns compact
  cell, byte, D3D bottleneck, cleanup, export, and optional-value formatting.
- `tools/McpServer/Tools/PerformanceTimelineTools.Summaries.cs` owns 1%-low
  target summaries, pressure summaries, counters, and budget predicates.
- `tools/McpServer/Tools/FramePacingVerdictTools.cs` owns the public
  `get_frame_pacing_verdict` MCP tool entry point, pipe command orchestration,
  and response shaping. `FramePacingVerdictTools.Channels.cs` owns snapshot
  cadence channel projection and recent-interval parsing.
  `FramePacingVerdictTools.Timeline.cs` owns performance-timeline projection.
  `FramePacingVerdictTools.Policy.cs` owns target-FPS inference, readiness,
  half-rate, hidden-stutter, ratio, and verdict policy.
  `FramePacingVerdictTools.Rendering.cs` owns the operator-facing verdict text.
  `FramePacingVerdictTools.Models.cs` owns the private channel and timeline
  records.
- `tools/McpServer/Tools/FlashbackTools.cs` owns Flashback enable/apply MCP
  commands and the Flashback MCP tool type.
  `tools/McpServer/Tools/FlashbackTools.Actions.cs` owns playback/scrub action
  normalization, validation, and payload shaping.
  `tools/McpServer/Tools/FlashbackTools.Export.cs` owns export duration/path
  validation, default path selection, export payload shaping, and export result
  text. `tools/McpServer/Tools/FlashbackTools.Segments.cs` owns segment-list
  command routing and segment result text.
- `tools/McpServer/Tools/VerificationTools.cs` owns the public verification MCP
  methods, command names, payload shaping, and verification response timeout
  policy. `tools/McpServer/Tools/VerificationTools.Assertions.cs` owns
  assertion JSON parsing and `JsonElement.Clone()` lifetime safety.
  `tools/McpServer/Tools/VerificationTools.Formatting.cs` owns recording,
  file, assertion, mismatch, and failure result text.
  `tools/McpServer/Tools/VerificationTools.Parsing.cs` owns verification lookup
  from `Data.Verification` and `Snapshot.LastVerification`.
- `tools/McpServer/Tools/PreviewFrameCaptureTools.cs` owns the public preview
  frame-capture MCP entry point, default output path, payload shaping, enum
  command routing, and failure/missing-data response handling.
  `tools/McpServer/Tools/PreviewFrameCaptureTools.Rendering.cs` owns the
  operator-facing report layout and section ordering.
  `tools/McpServer/Tools/PreviewFrameCaptureTools.Histogram.cs` owns 16-bin
  histogram projection, padding, scaling, and bar rendering.
  `tools/McpServer/Tools/PreviewFrameCaptureTools.Diagnosis.cs` owns blank,
  dark, bright, letterbox, pillarbox, low-contrast, and aspect-ratio diagnosis
  policy.
- `tools/McpServer/Tools/PresentMonTools.cs` owns public PresentMon MCP entry
  points, structured-content shape, option precedence, and probe invocation.
  `tools/McpServer/Tools/PresentMonTools.Correlation.cs` owns app-snapshot
  correlation fallback, preview-present field extraction, and malformed
  snapshot/pipe-failure fallback behavior.
- `tools/Common/DiagnosticSessionOptions.cs` owns diagnostic session run
  options.
- `tools/Common/DiagnosticSessionResult.cs` owns diagnostic-session summary DTO
  fields: core metadata, artifact paths, terminal state, actions, warnings,
  overview, capture/source, preview, Flashback recording, and Flashback export.
  `DiagnosticSessionResult.FlashbackPlayback.cs` owns Flashback playback
  command, cadence, decode, audio-master, and stage fields.
- `tools/Common/DiagnosticSessionSample.cs` owns sampled snapshot DTOs.
- `tools/Common/DiagnosticSessionResultBuilder.cs` owns diagnostic-session
  result phase orchestration, artifact-write handoff, summary-write handoff,
  final summary emission, and final-result orchestration from analysis and
  artifact paths into the named projection set and flattening owner. It also
  owns Flashback playback projection composition from focused playback
  projection owners. Keep `summary.json` field shape stable in the builder
  family.
- `tools/Common/DiagnosticSessionResultBuilder.Flattening.cs` owns final
  `DiagnosticSessionResult` DTO assignment from the projection set. Keep
  domain projection composition in the projection owners and root projection-set
  assembly, not in this initializer.
- `tools/Common/DiagnosticSessionResultBuilder.OverviewResult.cs` owns
  diagnostic-session outcome policy plus overview DTO projection for process
  CPU, recording verification, and PresentMon fields.
- `tools/Common/DiagnosticSessionResultBuilder.Analysis.cs` owns
  diagnostic-session metric preparation and named validation handoffs before
  summary construction.
- `tools/Common/DiagnosticSessionResultBuilder.DiagnosticHealth.cs` owns
  diagnostic-session health verdict composition, warning tolerance, and health
  warning text emitted during result construction.
- `tools/Common/DiagnosticSessionResultBuilder.FlashbackWarnings.cs` owns
  Flashback-specific analysis warning text for playback forward-decode caps and
  export force-rotate fallback observations.
- `tools/Common/DiagnosticSessionResultBuilder.PreviewScheduler.cs` owns
  diagnostic-session preview-scheduler analysis handoff values: MJPEG
  jitter-buffer counters, deltas, last drop/underflow reasons, underflow ages,
  and max schedule-late aggregation.
- `tools/Common/DiagnosticSessionResultBuilder.PreviewSchedulerValidation.cs`
  owns Flashback preview-scheduler validation orchestration during result
  analysis: target-FPS fallback, visual-cadence tolerance checks, sparse
  deadline/drop tolerance selection, and the call into shared Flashback
  preview validation.
- `tools/Common/DiagnosticSessionResultBuilder.FlashbackPlaybackCommandsResult.cs`
  owns Flashback playback command queue DTO projection values.
- `tools/Common/DiagnosticSessionResultBuilder.FlashbackPlaybackCadenceResult.cs`
  owns Flashback playback cadence, 1% low, slow-frame, and dropped-frame DTO
  projection values.
- `tools/Common/DiagnosticSessionResultBuilder.FlashbackPlaybackDecodeResult.cs`
  owns Flashback playback decode timing DTO projection values.
- `tools/Common/DiagnosticSessionResultBuilder.FlashbackPlaybackAudioMasterResult.cs`
  owns Flashback playback audio-master and A/V drift DTO projection values.
- `tools/Common/DiagnosticSessionResultBuilder.FlashbackPlaybackStagesResult.cs`
  owns Flashback playback submit, segment, write-head, near-live, and seek-cap
  DTO projection values.
- `tools/Common/DiagnosticSessionResultBuilder.FlashbackRecordingResult.cs`
  owns Flashback recording backend, growth, and integrity DTO projection
  values consumed by the final result initializer.
- `tools/Common/DiagnosticSessionResultBuilder.FlashbackExportResult.cs` owns
  Flashback export status, force-rotate fallback, last-result, and progress
  DTO projection values consumed by the final result initializer.
- `tools/Common/DiagnosticSessionResultBuilder.CaptureResult.cs` owns capture
  selection, negotiated format, source geometry, detected cadence, HDR, and
  source-telemetry DTO projection values consumed by the final result
  initializer.
- `tools/Common/DiagnosticSessionResultBuilder.PreviewD3DResult.cs` owns
  preview D3D frame-stats, slow-frame, and CPU-timing DTO projection values
  consumed by the final result initializer.
- `tools/Common/DiagnosticSessionResultBuilder.PreviewVisualCadenceResult.cs`
  owns preview visual-cadence DTO projection values consumed by the final
  result initializer.
- `tools/Common/DiagnosticSessionResultBuilder.PreviewResult.cs` owns preview
  cadence and scheduler DTO projection values consumed by the final result
  initializer. It maps `analysis.PreviewScheduler` and should not reread MJPEG
  jitter-buffer snapshot keys, D3D metrics, or visual-cadence metrics directly.
- `tools/Common/DiagnosticSessionResultBuilder.Models.cs` owns the builder
  request record and private analysis handoff record, including the single
  `PreviewScheduler` record property used by preview result projection.
- `tools/Common/DiagnosticSessionSummaryWriter.cs` owns diagnostic-session
  `summary.json` writes and summary-write failure repair of the returned
  result object.
- `tools/Common/DiagnosticSessionResultArtifacts.cs` owns diagnostic-session
  result artifact path construction and pre-summary sample, frame-ledger, and
  timeline artifact writes.
- `tools/Common/DiagnosticSessionJsonArtifacts.cs` owns diagnostic-session JSON
  artifact writing, frame-ledger extraction, and automation response shape
  helpers.
- `tools/Common/DiagnosticSessionInitialSnapshot.cs` owns the diagnostic-session
  baseline snapshot capture via `AutomationCommandKind.GetSnapshot`,
  unknown-state warning, and initial-snapshot exception recording.
- `tools/Common/DiagnosticSessionRunState.cs` owns diagnostic-session terminal
  exception state, last-stage tracking, and best-effort artifact write failure
  recording.
- `tools/Common/DiagnosticSessionLiveStateWriter.cs` owns the best-effort
  `session-live.json` breadcrumb path, payload shape, health projection,
  warning projection, terminal override mapping, and sampling write throttle.
- `tools/Common/DiagnosticSessionRunBootstrap.cs` owns diagnostic-session
  scenario normalization, scenario-plan selection, duration/sample clamping,
  session identity, output-directory creation, and runner process metadata.
- `tools/Common/DiagnosticSessionRunContext.cs` owns diagnostic-session mutable run infrastructure:
  bootstrap, actions, warnings, samples, run state, live-state writer, command
  channel, scenario cancellation source, initial snapshot state, and disposal.
- `tools/Common/DiagnosticSessionRunContext.PhaseContexts.cs` owns diagnostic-session scenario/completion context construction:
  explicit phase handoff DTO assembly, cancellation/run token forwarding, stage
  callbacks, live-state callbacks, and terminal-state callback wiring.
- `tools/Common/DiagnosticSessionRunExecution.cs` owns diagnostic-session phase
  sequencing around context creation, initial snapshot capture, scenario phase
  invocation, cleanup, and completion-phase handoff.
- `tools/Common/DiagnosticSessionRunExecution.Completion.cs` owns the
  post-cleanup evidence/result sequence: recording checks, post-run timeline
  and final snapshot capture, result-build handoff, and terminal live-state
  write.
- `tools/Common/DiagnosticSessionScenarioPhaseRunner.cs` owns the named
  diagnostic-session scenario phase: state-mutation gating, setup/startup,
  sampling/completion delegation, fault drain delegation, and the cleanup result
  consumed by `RunAsync`.
- `tools/Common/DiagnosticSessionScenarioPhaseRunner.Models.cs` owns the
  explicit scenario phase context, mutable phase state, and immutable phase
  result handoff records.
- `tools/Common/DiagnosticSessionScenarioPhaseRunner.Sampling.cs` owns
  diagnostic-session scenario sampling: live-state sampling setup, sample-loop
  invocation, and handoff to the phase completion owner.
- `tools/Common/DiagnosticSessionScenarioPhaseCompletion.cs` owns
  diagnostic-session scenario post-sampling completion and fault drain:
  scenario background task awaits, recording-settings deferred await,
  rejected-export handling, PresentMon await, and interrupted background-task
  drain.
- `tools/Common/DiagnosticSessionOutputLock.cs` owns the per-output-directory
  exclusive lock that prevents concurrent diagnostic sessions from writing the
  same artifact set.
- `tools/Common/DiagnosticSessionBackgroundTasks.cs` owns diagnostic-session
  background task registration, deterministic await order, and normal PresentMon
  task completion.
- `tools/Common/DiagnosticSessionBackgroundTasks.FaultDrain.cs` owns
  interrupted background-task warning collection and fault drain.
- `tools/Common/DiagnosticSessionBackgroundTasks.Models.cs` owns the small
  background-task handoff records.
- `tools/Common/DiagnosticSessionScenarioStartup.cs` owns diagnostic-session
  optional background startup orchestration.
- `tools/Common/DiagnosticSessionScenarioStartup.Registrations.cs` owns
  Flashback scenario registration orchestration and delegates task registration
  to the focused scenario owners, including deferred Flashback recording-settings
  task registration. Keep task stage names stable there.
- `tools/Common/DiagnosticSessionScenarioStartup.Playback.cs` owns the direct
  Flashback playback start command, playback buffer readiness warning, and
  playback-state wait.
- `tools/Common/DiagnosticSessionPresentMonStartup.cs` owns optional PresentMon
  launch, correlation snapshot capture, and `presentmon.csv` output selection
  for diagnostic sessions.
- `tools/Common/DiagnosticSessionScenarioSetup.cs` owns diagnostic-session
  initial state mutations before sampling: enabling or disabling Flashback for
  scenarios, starting preview, starting recording, and waiting for the
  associated readiness conditions. Keep fixed setup mutations on
  `DiagnosticSessionCommandChannel` typed `AutomationCommandKind` sends.
- `tools/Common/DiagnosticSessionCleanupActions.cs` owns diagnostic-session
  cleanup flow, ordering, stage/action naming, and the cleanup result handoff
  record.
- `tools/Common/DiagnosticSessionCleanupActions.Recording.cs` owns
  diagnostic-session recording stop for verification through typed
  `AutomationCommandKind.SetRecordingEnabled`.
- `tools/Common/DiagnosticSessionCleanupActions.StateRestore.cs` owns
  Flashback playback go-live restore, preview stop, and Flashback enable-state
  restore through typed `AutomationCommandKind.FlashbackAction`,
  `SetPreviewEnabled`, and `SetFlashbackEnabled` sends.
- `tools/Common/DiagnosticSessionRecordingChecks.cs` owns post-cleanup
  diagnostic-session recording checks: deferred Flashback recording-settings
  restore, verification handoff, and Flashback recording validation. Keep the
  `settings-deferred-restore` and `recording-validation` stage names stable
  there.
- `tools/Common/DiagnosticSessionRecordingVerification.cs` owns post-cleanup
  last-recording or Flashback export verification command selection, payload
  shape, 60-second timeout, cloned verification result, and skipped-verification
  action text. Keep the `recording-verification` stage name stable there.
- `tools/Common/DiagnosticSessionPostRunSnapshots.cs` owns post-run
  diagnostic-session snapshot fetches: performance timeline collection and
  final health snapshot refresh. Keep the `timeline` and `final-snapshot` stage
  names stable there.
- `tools/Common/DiagnosticSessionCleanupPolicy.cs` owns cleanup restore
  validation after diagnostic sessions stop recording, preview, Flashback, or
  playback state.
- `tools/Common/DiagnosticSessionFlashbackCycleScenarios.Restart.cs` owns the
  restart-cycle command flow, including playback priming, restart validation,
  export verification, and restart-cycle warning/action strings.
- `tools/Common/DiagnosticSessionFlashbackCycleScenarios.Encoder.cs` owns the
  encoder-cycle command flow, including preset cycling, buffer readiness,
  export verification, preset restoration, and encoder-cycle warning/action
  strings.
- `tools/Common/DiagnosticSessionFlashbackCycleScenarios.Registrations.cs` owns
  Flashback restart/encoder cycle diagnostic task registration, priorities,
  task labels, and started action strings.
- `tools/Common/DiagnosticSessionMetrics.Models.cs` owns metric DTOs.
- `tools/Common/DiagnosticSessionMetrics.Cadence.cs` owns source, preview, and
  visual cadence projection from sampled snapshots plus visual-cadence health
  classification.
- `tools/Common/DiagnosticSessionMetrics.PreviewD3D.cs` owns D3D slow-frame and
  CPU timing summaries.
- `tools/Common/DiagnosticSessionMetrics.PlaybackCommands.cs` owns playback
  command-health deltas.
- `tools/Common/DiagnosticSessionMetrics.Counters.cs` owns shared counter-delta
  helpers.
- `tools/Common/DiagnosticSessionFlashbackExports.cs` owns rotated-export
  segment-count parsing, strict export verification payload construction, and
  range-selection cleanup.
- `tools/Common/DiagnosticSessionFlashbackExports.AudioSwitch.cs` owns the
  audio-toggle companion used by the range export audio-switch scenario.
- Flashback export diagnostic scenario flows live in focused files:
  `tools/Common/DiagnosticSessionFlashbackExportScenarios.Concurrent.cs`
  owns concurrent export, `tools/Common/DiagnosticSessionFlashbackExportScenarios.DisableDuringExport.cs`
  owns disable-during-export, `tools/Common/DiagnosticSessionFlashbackExportScenarios.Rotated.cs`
  owns rotated export, `tools/Common/DiagnosticSessionFlashbackExportScenarios.Playback.cs`
  owns export-during-playback, and
  `tools/Common/DiagnosticSessionFlashbackExportScenarios.Range.cs` owns
  selection-range export.
  `tools/Common/DiagnosticSessionFlashbackExportScenarios.RangeValidation.cs`
  owns range duration/status validation, and
  `tools/Common/DiagnosticSessionFlashbackExportScenarios.RangeCleanup.cs`
  owns post-cleanup playback command-health validation.
  `tools/Common/DiagnosticSessionFlashbackExportScenarios.Registrations.cs`
  owns the export scenario task registration handoff from diagnostic-session
  startup.
- `tools/Common/DiagnosticSessionFlashbackLifecycleScenarios.cs` owns
  Flashback playback disable/re-enable lifecycle diagnostic task registration
  and flow.
- `tools/Common/DiagnosticSessionFlashbackMetrics.Models.cs` owns session/result
  DTOs. `tools/Common/DiagnosticSessionFlashbackMetrics.Recording.cs`,
  `tools/Common/DiagnosticSessionFlashbackMetrics.PlaybackSession.cs`,
  `tools/Common/DiagnosticSessionFlashbackMetrics.PlaybackResult.cs`, and
  `tools/Common/DiagnosticSessionFlashbackMetrics.Export.cs` own read-only
  recording, playback session metric accumulation, result-copy, and export
  metric projections. Playback session accumulation includes 1% low window
  capture, frame/decode maxima, and audio-master maxima.
  Export metrics include force-rotate fallback total, delta, and last fallback
  segment count; keep those counters derived outside export-observed relevance gating.
- `tools/Common/DiagnosticSessionFlashbackPreviewCycleScenarios.Registrations.cs`
  owns Flashback preview-cycle diagnostic task registration, priorities, task
  labels, and started action strings.
- `tools/Common/DiagnosticSessionFlashbackPreviewCycleScenarios.Flashback.cs`
  owns normal Flashback preview stop/restart diagnostic flow and preview state
  checks.
- `tools/Common/DiagnosticSessionFlashbackPreviewCycleScenarios.FlashbackExport.cs`
  owns normal Flashback preview-cycle export-while-preview-off verification.
- `tools/Common/DiagnosticSessionFlashbackPreviewCycleScenarios.Playback.cs`
  owns playback-under-preview-stop diagnostic flow and preview state checks.
- `tools/Common/DiagnosticSessionFlashbackPreviewCycleScenarios.PlaybackExport.cs`
  owns playback-preview-cycle export-while-preview-off verification.
- `tools/Common/DiagnosticSessionFlashbackPreviewCycleScenarios.Recording.cs`
  owns Flashback-recording-backed preview stop/restart diagnostic flow.
- `tools/Common/DiagnosticSessionFlashbackRejectedExports.cs` owns Flashback
  rejected-export diagnostic scenario dispatch.
- `tools/Common/DiagnosticSessionFlashbackRejectedExports.Inactive.cs` owns
  inactive-buffer rejected-export failure-kind and last-result assertions.
- `tools/Common/DiagnosticSessionFlashbackRejectedExports.Recording.cs` owns
  active-Flashback-recording rejected-export failure-kind and backend-stability
  assertions.
- `tools/Common/DiagnosticSessionFlashbackRecordingSettingsScenarios.DuringRecording.cs`
  owns deferred preset state plus preset mutation and restart/disable rejection
  checks while Flashback is recording.
- `tools/Common/DiagnosticSessionFlashbackRecordingSettingsScenarios.PostStop.cs`
  owns post-stop preset verification, encoder-frame check, and original-preset
  restore.
- `tools/Common/DiagnosticSessionFlashbackSegmentPlaybackScenarios.cs` owns the
  Flashback completed-segment playback scenario registration and choreography.
- `tools/Common/DiagnosticSessionFlashbackSegmentPlaybackScenarios.Validation.cs`
  owns completed-segment playback snapshot, FPS, command-health, and boundary
  warning policy.
- `tools/Common/DiagnosticSessionFlashbackSegmentPlaybackScenarios.RecordingAssist.cs`
  owns recording-assisted segment rotation and best-effort stop cleanup for
  segment playback diagnostics.
- `tools/Common/DiagnosticSessionFlashbackSegments.cs` owns read-only
  diagnostic-session Flashback segment parsing, completed-segment waits, and
  playable-boundary headroom waits. Do not add state-mutating scenario steps
  there.
- `tools/Common/DiagnosticSessionFlashbackStressScenario.cs` owns Flashback
  stress thresholds and stress/scrub-stress task registration.
- `tools/Common/DiagnosticSessionFlashbackStressScenario.Stress.cs` owns the
  main Flashback stress command sequence and export verify.
- `tools/Common/DiagnosticSessionFlashbackStressScenario.WarmPlayback.cs` owns
  warmed-playback frame/FPS/1% low and audio-master fallback checks.
- `tools/Common/DiagnosticSessionFlashbackStressScenario.CommandDrain.cs` owns
  post-go-live playback command drain, latency, and final-state checks.
- `tools/Common/DiagnosticSessionFlashbackStressScenario.Scrub.cs` owns the
  scrub-stress command burst.
- `tools/Common/DiagnosticSessionFlashbackStressScenario.ScrubDrain.cs` owns
  scrub-stress post-go-live drain, command-health, latency, and final-state
  checks.
- `tools/Common/DiagnosticSessionFlashbackStressScenario.AudioMaster.cs` owns
  warmed-playback audio-master fallback classification.
- `tools/Common/DiagnosticSessionFlashbackWaits.cs` owns read-only snapshot
  polling waits used by Flashback diagnostic scenarios, including preview
  active, Flashback active, recording-ready, and buffer-ready waits.
- `tools/Common/DiagnosticSessionFlashbackWaits.Playback.cs` owns Flashback
  playback boundary, state, warmup, and position polling waits.
- `tools/Common/DiagnosticSessionFlashbackValidation.Recording.cs` owns
  Flashback recording warning policy over projected recording metrics.
- `tools/Common/DiagnosticSessionFlashbackValidation.Playback.cs` owns Flashback
  playback warning policy over projected playback and visual cadence metrics.
- `tools/Common/DiagnosticSessionFlashbackValidation.Preview.cs` owns Flashback
  preview scheduler warning policy over scheduler, cadence, and D3D metrics.
- `tools/Common/DiagnosticSessionHealthPolicy.cs` owns diagnostic-session health
  observation, severity, and Flashback warmup filtering.
- `tools/Common/DiagnosticSessionHealthTolerances.cs` owns diagnostic-session
  source/preview/Flashback health-observation classifiers, sparse-cadence
  tolerances, and tolerated Flashback warning classification.
- `tools/Common/DiagnosticSessionSampler.cs` owns snapshot sample collection.
  Preserve its ordering: append the cloned sample before running checkpoint
  callbacks.
- `tools/Common/DiagnosticSessionResultFormatter.cs` owns the public
  human-readable diagnostic-session text flow used by ssctl and MCP, including
  the simple capture-mode, recording-verification, PresentMon, and
  process-performance summary rows plus small result text helpers such as
  frame-rate formatting. Keep `DiagnosticSessionRunner.Format(...)` as the
  stable compatibility wrapper.
- `tools/Common/DiagnosticSessionResultFormatter.Overview.cs` owns the
  diagnostic-session header, summary, and evidence section.
- `tools/Common/DiagnosticSessionResultFormatter.Flashback.cs` owns Flashback
  diagnostic-session text section ordering plus playback command,
  playback stage/seek-cap, recording, and export lines.
  `DiagnosticSessionResultFormatter.FlashbackPlayback.Performance.cs` owns
  playback cadence/audio-master performance lines,
  `DiagnosticSessionResultFormatter.FlashbackPlayback.Decode.cs` owns playback
  decode timing lines.
- `tools/Common/DiagnosticSessionResultFormatter.Preview.cs` owns preview
  diagnostic-session text section ordering plus preview scheduler, D3D
  performance/slow-frame, D3D CPU timing, and visual-cadence lines.
- `tools/Common/DiagnosticSessionResultFormatter.Artifacts.cs` owns artifact,
  action, and warning sections.
- `tools/Common/DiagnosticSessionOptionalTextFormatter.cs` owns shared
  diagnostic-session optional text formatting used by scenarios, result
  builders, result formatters, and validation policies.
- `tools/Common/AutomationSnapshotFormatter.cs` owns the top-level shared
  automation snapshot console text flow and delegates each named output
  section to a focused partial.
  `tools/Common/AutomationSnapshotFormatter.CoreSections.cs` owns the state,
  capture-command queue, selected-device, initialized/preview/recording, audio,
  recording summary, legacy performance, process CPU, memory, GC, and
  thread-pool text.
  `tools/Common/AutomationSnapshotFormatter.CaptureSettings.cs` owns capture
  option, recording format, HDR, pipeline, and compact UI setting text.
  `tools/Common/AutomationSnapshotFormatter.VideoPipeline.cs` owns video reader,
  encoder queue, queue-latency, backpressure, failure, GPU/CUDA queue,
  freshness, diagnostics, and thread-health routing text.
  `tools/Common/AutomationSnapshotFormatter.Diagnostics.cs` owns diagnostic
  health, summary, evidence, and frame-lane text.
  `tools/Common/AutomationSnapshotFormatter.CaptureCadence.cs` owns capture
  cadence, low-FPS, jitter/drop, MJPEG packet fingerprint, sampled visual
  cadence, AV-sync text, source-signal text, and routing to MJPEG/preview
  sections.
  `tools/Common/AutomationSnapshotFormatter.Values.cs` owns tolerant JSON
  accessors and typed JSON coercion.
  `tools/Common/AutomationSnapshotFormatter.DisplayValues.cs` owns shared
  byte, number, interval, frame-budget, and tick-age display helpers, while
  `tools/Common/AutomationSnapshotFormatter.Flashback.cs` owns the Flashback
  gate, header, subsection ordering, encoder, buffer, cache, queue, failure,
  backpressure, GPU queue, playback, and export text. The
  `tools/Common/AutomationSnapshotFormatter.MjpegTiming.cs`,
  `tools/Common/AutomationSnapshotFormatter.Preview.cs`,
  `tools/Common/AutomationSnapshotFormatter.PreviewD3D.cs`,
  `tools/Common/AutomationSnapshotFormatter.PreviewD3D.SlowFrames.cs`,
  and `tools/Common/AutomationSnapshotFormatter.ThreadHealth.cs` own the named
  snapshot sections. Within the D3D formatter family, `.PreviewD3D.cs` keeps
  routing/header order plus CPU timing, pipeline-latency, frame-ownership,
  frame-latency wait, and DXGI frame-stat text, while `.SlowFrames.cs` owns
  reusable slow-frame diagnostics and diagnostic millisecond formatting.
- `tools/Common/DiagnosticSessionPipeRetryPolicy.cs` owns diagnostic-session
  connect retry classification and local failure-response envelopes.
- `tools/Common/DiagnosticSessionCommandChannel.cs` owns serialized
  diagnostic-session automation command sending, connect-retry wrapping,
  command failure accounting, and `AutomationCommandKind`-to-catalog
  command-name resolution for fixed channel-owned commands, including setup and
  cleanup lifecycle mutations. Keep the underlying runner delegate
  string-compatible.
- `tools/Common/DiagnosticSessionCommandChannel.WaitConditions.cs` owns
  diagnostic-session wait command helpers, `WaitForCondition` payload shaping,
  and routing that fixed wait command through the channel's
  `AutomationCommandKind` overload.
- `tools/Common/DiagnosticSessionScenarioCatalog.cs` owns diagnostic-session
  scenario ordering, setup requirements, export verification filenames, and the
  plan assigned to each normalized scenario.
- `tools/Common/DiagnosticSessionScenarioPlan.cs` owns the scenario plan DTO and
  grouped warning/validation policies, including the preview-cycle grouped
  predicate, used by the runner. Keep new scenario booleans there instead of
  adding string comparisons in `DiagnosticSessionRunner`.
- `tools/Common/PresentMon/PresentMonProbe.Models.cs` owns PresentMon option/result,
  summary, swap-chain, correlation, and metric DTOs.
- `tools/Common/PresentMon/PresentMonProbe.ResultMessage.cs` owns PresentMon result-message
  shaping for success, expected-swap-chain mismatch, and no-frame outcomes.
- `tools/Common/PresentMon/PresentMonProbe.Format.cs` owns PresentMon result text rendering
  used by diagnostic-session output surfaces.
- `tools/Common/PresentMon/PresentMonProbe.Csv.cs` owns PresentMon CSV parse overloads,
  selected-row filtering, summary assembly, and handoff to row/swap-chain/
  warning/correlation helpers.
- `tools/Common/PresentMon/PresentMonProbe.Csv.Rows.cs` owns PresentMon CSV row ingestion,
  header index construction, schema-presence detection, blank-line skipping,
  row index assignment, and row projection from header-indexed fields.
- `tools/Common/PresentMon/PresentMonProbe.Csv.Fields.cs` owns header/field parsing,
  scalar field/metric reads, and CSV line tokenization.
- `tools/Common/PresentMon/PresentMonProbe.Csv.SwapChains.cs` owns swap-chain
  normalization, artifact filtering, and selected-chain summaries.
- `tools/Common/PresentMon/PresentMonProbe.Csv.Correlation.cs` owns app-present
  correlation and displayed/not-displayed outcome classification.
- `tools/Common/PresentMon/PresentMonProbe.Csv.Summary.cs` owns warnings, counted text
  fields, and percentile metric aggregation.
- `tools/Common/PresentMon/PresentMonProbe.Csv.Models.cs` owns the private parsed CSV
  handoff and row shapes.
- `tools/Common/PresentMon/PresentMonProbe.cs` owns PresentMon public run orchestration,
  command-line construction, and argument quoting.
- `tools/Common/PresentMon/PresentMonProbe.Paths.cs` owns target process, PresentMon
  executable, and output-path resolution.
- `tools/Common/PresentMon/PresentMonProbe.Process.cs` owns process supervision,
  stdout/stderr drain, timeout kill, and temp CSV cleanup.

Invariants:

- Do not add new automation metadata to tool-specific files if it belongs in
  `Sussudio.Automation.Contracts`.
- Fixed CLI/MCP automation routes should use `AutomationCommandKind` overloads
  at the pipe seam; keep string command names only for operator-facing labels,
  catalog/manifest-backed dynamic batches, and diagnostic-session runner
  command-channel delegates.
- Long-running Flashback operations must use catalog timeouts, not hard-coded
  shorter client defaults.
- Diagnostic sessions are evidence surfaces; preserve summary JSON stability
  when refactoring runners.
- Preserve diagnostic-session artifact filenames and JSON shapes when moving
  artifact helpers; tests read `summary.json`, `session-live.json`, samples,
  frame ledger, and timeline outputs.
- Preserve diagnostic-session terminal-state semantics: canceled wins when the
  caller token is canceled, otherwise terminal exceptions fail and clean runs
  complete. `session-live.json` is best-effort breadcrumb output.
- Preserve diagnostic metric projection semantics; these helpers must stay
  read-only over sampled snapshots and must not send automation commands.
- Preserve Flashback metric projection semantics; this helper should only read
  sampled snapshots and derive deltas/statuses, not mutate playback/export
  state.
- Preserve Flashback validation warning thresholds; these warnings feed
  diagnostic-session pass/fail summaries and should stay explainable in result
  text.
- Preserve health policy semantics when moving tolerance logic; warmup filtering
  must still ignore only transient low-severity Flashback startup observations.
- Preserve sampler checkpoint ordering; checkpoint callbacks are allowed to
  observe the sample that was just appended.
- Preserve diagnostic-session background task await order when moving scenario
  startup; interrupted-task warnings are evidence and should keep stable stage
  names.
- Preserve diagnostic-session scenario sampling/completion order when moving
  runner code: sampling live state and sample loop stay in the sampling owner;
  scenario task await, deferred recording-settings await, rejected-export
  handling, PresentMon await, and fault drain stay in the completion owner.
  Their sequence and stage names are evidence and must remain stable.
- Preserve diagnostic-session cleanup stage/action names when moving cleanup
  mutations; downstream result text and failure reports use those names as
  evidence.
- Preserve result text compatibility when refactoring diagnostic-session
  formatting; ssctl and MCP both flow through `DiagnosticSessionRunner.Format`.
- Preserve pipe error-code semantics when refactoring diagnostic-session retry:
  `pipe-access-denied` is permanent, while connect failed/timeout are retried.
- Add new diagnostic-session scenario names in
  `tools/Common/DiagnosticSessionScenarios.cs`, then add their requirements and
  plan metadata in `tools/Common/DiagnosticSessionScenarioCatalog.cs` before
  wiring scenario behavior into `DiagnosticSessionRunner`.
- Keep diagnostic-session grouped policy derivation in
  `tools/Common/DiagnosticSessionScenarioPlan.cs`; the runner should consume
  named properties instead of comparing normalized scenario strings directly.

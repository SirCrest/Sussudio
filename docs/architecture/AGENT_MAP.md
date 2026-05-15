# Sussudio Agent Map

Last reviewed: 2026-05-15.

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
| Diagnostic sessions | `tools/Common/DiagnosticSessionRunner.cs`, `tools/Common/DiagnosticSessionRunExecution.cs`, `tools/Common/DiagnosticSessionRunExecution.Scenario.cs`, `tools/Common/DiagnosticSessionScenarioPhaseRunner.cs`, `tools/Common/DiagnosticSessionRunExecution.ResultRequest.cs` | public runner compatibility wrapper, mutable run phase plan, scenario phase handoff, named scenario phase execution/context/state/result, result-build request handoff, run bootstrap/options normalization, scenario catalog, startup/cleanup/recording-check/post-run snapshot helpers, result formatter, plus per-scenario runners |
| Offline regression harness | `tests/Sussudio.Tests/Program.cs`, `tests/Sussudio.Tests/HarnessCheckCatalog*.cs` | runner entry point, topic check catalogs, xUnit slices, and focused contract tests such as `StatsPresentation.*.Tests.cs` |
| Capture runtime | `Sussudio/Services/Capture/CaptureService.cs`, `CaptureService.Initialization.cs`, `CaptureService.Audio.cs`, `CaptureService.MicrophoneMonitor.cs`, `CaptureService.WasapiPlayback.cs`, `CaptureService.Cleanup.cs`, `CaptureService.Coordination.cs`, `CaptureService.DeferredCleanup.cs`, `CaptureService.Failures.cs`, `CaptureService.FlashbackControls.cs`, `CaptureService.FlashbackOrchestration.cs`, `CaptureService.FlashbackAudioInputs.cs`, `CaptureService.FlashbackPreviewBackend.cs`, `CaptureService.FlashbackPreviewBackendDisposal.cs`, `CaptureService.FlashbackBufferCycle.cs`, `CaptureService.FlashbackExportDiagnostics.cs`, `CaptureService.FlashbackExportFailureClassification.cs`, `CaptureService.FlashbackExportOperations.cs`, `CaptureService.FlashbackExportPlanning.cs`, `CaptureService.FlashbackRecording.cs`, `CaptureService.HealthSnapshots.cs`, `CaptureService.HealthSnapshotCaptureCadence.cs`, `CaptureService.HealthSnapshotFlashbackBuffer.cs`, `CaptureService.HealthSnapshotFlashbackQueues.cs`, `CaptureService.HealthSnapshotMjpeg.cs`, `CaptureService.HealthSnapshotSourceTelemetry.cs`, `CaptureService.PreviewLifecycle.cs`, `CaptureService.PreviewPipeline.cs`, `CaptureService.Probes.cs`, `CaptureService.RecordingIntegrity.cs`, `CaptureService.RecordingIntegrity.Models.cs`, `CaptureService.RecordingIntegrity.Summary.cs`, `CaptureService.RecordingIntegrity.Counters.cs`, `CaptureService.RecordingIntegrity.Audio.cs`, `CaptureService.RecordingIntegrity.Logging.cs`, `CaptureService.RecordingLifecycle.cs`, `CaptureService.RecordingRollback.cs`, `CaptureService.RuntimeSnapshots.cs`, `CaptureService.RuntimeSnapshotIngestAudio.cs`, `CaptureService.RuntimeSnapshotHdrPipeline.cs`, `CaptureService.RuntimeSnapshotSourceTelemetry.cs`, `CaptureService.RuntimeSnapshotRecordingIntegrity.cs`, `CaptureService.Snapshots.cs`, `CaptureService.SnapshotRecordingFormat.cs`, `CaptureService.SnapshotObservedFrames.cs`, `CaptureService.SnapshotAvSync.cs`, `CaptureService.SnapshotTelemetry.cs`, `CaptureService.ObservedPixelTelemetry.cs`, `CaptureService.Telemetry.cs` | service state and construction owner, initialization owner, audio preview/input switching owner, microphone monitoring owner, WASAPI playback routing owner, cleanup owner, transition/disposal owner, deferred cleanup owner, failure owner, Flashback control owner, Flashback restart orchestration owner, Flashback audio input restoration owner, Flashback preview backend startup owner, Flashback preview backend disposal owner, Flashback buffer cycle owner, Flashback export diagnostics/progress owner, Flashback export failure taxonomy, Flashback export entry/core owner, Flashback export planning/throttle owner, Flashback recording policy owner, health snapshot builder, capture cadence health projection, Flashback buffer/backend health projection, Flashback queue health projection, MJPEG health snapshot projection, source telemetry health projection, preview lifecycle owner, preview pipeline owner, probe owner, recording integrity active-backend resolver, integrity DTOs, integrity summary classification, integrity counter capture, audio integrity capture, integrity logging, recording start/stop transition owner, transient recording rollback owner, runtime snapshot builder, runtime ingest/audio projection, runtime HDR/encoder pipeline projection, runtime source-telemetry projection, runtime recording-integrity projection, diagnostics compatibility and shared snapshot utilities, recording format snapshot policy, observed frame snapshot telemetry, A/V sync snapshot policy, source telemetry snapshot policy, observed pixel telemetry owner, telemetry owner, resource managers |
| Device discovery | `Sussudio/Services/Capture/DeviceService.cs`, `DeviceService.FormatCache.cs`, `DeviceService.FormatProbe.cs`, `DeviceService.Scoring.cs`, `DeviceService.AudioAssociation.cs`, `DeviceService.NativeXu.cs`, `MfDeviceEnumerator.cs`, `MfDeviceEnumerator.VideoDevices.cs`, `MfDeviceEnumerator.AudioEndpoints.cs`, `MfDeviceEnumerator.FormatProbe.cs` | device enumeration orchestration, persisted format cache, inline/background format probing, priority/capability scoring, audio endpoint association, Native XU interface path resolution, shared MF constants/P/Invokes, MF video device enumeration, WASAPI capture endpoint enumeration, native MF format probing and source fallback |
| Native XU KS bridge | `Sussudio/Services/Capture/KsExtensionUnitNative.cs`, `KsExtensionUnitNative.Interfaces.cs`, `KsExtensionUnitNative.Handles.cs`, `KsExtensionUnitNative.Topology.cs`, `KsExtensionUnitNative.Transfers.cs`, `KsExtensionUnitNative.Interop.cs` | KS category constants and DTOs, SetupAPI interface enumeration, file-handle open policy, topology node parsing, XU GET/SET transfer helpers, P/Invoke declarations and structs |
| Capture source reader | `Sussudio/Services/Capture/MfSourceReaderVideoCapture.cs`, `MfSourceReaderVideoCapture.Initialization.cs`, `MfSourceReaderVideoCapture.ReadLoop.cs`, `MfSourceReaderVideoCapture.FrameDelivery.cs`, `MfSourceReaderVideoCapture.Cadence.cs`, `MfSourceReaderVideoCapture.Diagnostics.cs`, `MfSourceReaderVideoCapture.DxgiBuffers.cs`, `MfSourceReaderVideoCapture.FrameLayout.cs`, `MfSourceReaderVideoCapture.Lifecycle.cs`, `MfSourceReaderVideoCapture.Negotiation.cs`, `MfSourceReaderVideoCapture.Interop.cs` | source-reader state and public counters, initialization/negotiated state application, Media Foundation read loop, sample-to-frame delivery, source cadence metrics, debug-only COM diagnostics, DXGI texture extraction, packed YUV frame layout and subtype labels, reader start/stop/dispose lifecycle, device opening and media-type negotiation, MF P/Invoke and COM interface definitions |
| Capture fan-out | `Sussudio/Services/Capture/UnifiedVideoCapture.cs`, `UnifiedVideoCapture.Lifecycle.cs`, `UnifiedVideoCapture.SinkFanout.cs`, `UnifiedVideoCapture.Metrics.cs`, `UnifiedVideoCapture.Preview.cs` | frame arrival routing, shared source-reader lifecycle, recording/Flashback sink queue fan-out, diagnostic metric/snapshot projection, preview sink submission and visual-cadence handling |
| Audio capture | `Sussudio/Services/Audio/WasapiAudioCapture.cs`, `WasapiAudioCapture.CaptureLoop.cs`, `WasapiAudioCapture.Fanout.cs`, `WasapiAudioCapture.Conversion.cs`, `WasapiAudioCapture.Diagnostics.cs` | WASAPI device lifecycle, capture thread/packet drain, sink/playback/hot writer fan-out, f32le 48 kHz stereo conversion/resampling helpers, and callback/glitch metric projection |
| MJPEG preview pacing | `Sussudio/Services/Capture/MjpegPreviewJitterBuffer.cs`, `MjpegPreviewJitterBuffer.EmitLoop.cs`, `MjpegPreviewJitterBuffer.Queue.cs`, `MjpegPreviewJitterBuffer.Adaptive.cs`, `MjpegPreviewJitterBuffer.Metrics.cs` | decoded preview-frame construction/lifecycle, paced emit loop and renderer submission, queue ordering and reprime recovery, adaptive deadline/depth policy, jitter-buffer metric records and timing sample projection |
| MJPEG decode pipeline | `Sussudio/Services/Gpu/ParallelMjpegDecodePipeline.cs`, `ParallelMjpegDecodePipeline.Reorder.cs`, `ParallelMjpegDecodePipeline.Lifecycle.cs`, `ParallelMjpegDecodePipeline.Metrics.cs`, `NvdecMjpegDecoder.cs`, `NvdecMjpegDecoder.Initialization.cs`, `NvdecMjpegDecoder.Decode.cs`, `NvdecMjpegDecoder.Download.cs`, `NvdecMjpegDecoder.Lifetime.cs`, `CudaD3D11Interop.cs`, `CudaD3D11Interop.Initialization.cs`, `CudaD3D11Interop.Copy.cs`, `CudaD3D11Interop.Lifetime.cs`, `CudaD3D11Interop.Native.cs` | CPU MJPEG worker/decode ingress, decoded-frame ordering and emission, stop/dispose/resource cleanup and fatal callback signaling, pipeline timing and packet-hash metrics, NVDEC decoder state, NVDEC initialization, decode/context access, CPU download/copy helpers, NVDEC disposal/error text, CUDA-to-D3D11 bridge state, bridge setup/zero-copy registration, zero-copy and staging copy behavior, bridge disposal/resource unregister, CUDA native constants/P/Invoke declarations |
| Automation diagnostics | `Sussudio/Services/Automation/AutomationDiagnosticsHub.cs`, `AutomationDiagnosticsHub.Alerts.cs`, `AutomationDiagnosticsHub.SignalAlerts.cs`, `AutomationDiagnosticsHub.FlashbackAlerts.cs`, `AutomationDiagnosticsHub.FlashbackRecordingAlerts.cs`, `AutomationDiagnosticsHub.FlashbackPlaybackAlerts.cs`, `AutomationDiagnosticsHub.FlashbackPlaybackCommandAlerts.cs`, `AutomationDiagnosticsHub.FlashbackPlaybackPerformanceAlerts.cs`, `AutomationDiagnosticsHub.DiagnosticEvents.cs`, `AutomationDiagnosticsHub.DiagnosticEvaluation.cs`, `AutomationDiagnosticsHub.DiagnosticEvaluationFlashback.cs`, `AutomationDiagnosticsHub.DiagnosticEvaluationRealtime.cs`, `AutomationDiagnosticsHub.DiagnosticEvaluationLanes.cs`, `AutomationDiagnosticsHub.EvaluationModels.cs`, `AutomationDiagnosticsHub.Evaluation.cs`, `AutomationDiagnosticsHub.EvaluationPolicy.cs`, `AutomationDiagnosticsHub.Hdr.cs`, `AutomationDiagnosticsHub.Lifecycle.cs`, `AutomationDiagnosticsHub.OutputFiles.cs`, `AutomationDiagnosticsHub.PreviewPacing.cs`, `AutomationDiagnosticsHub.ProcessMetrics.cs`, `AutomationDiagnosticsHub.Snapshots.cs`, `AutomationDiagnosticsHub.SnapshotProjection.cs`, `AutomationDiagnosticsHub.SnapshotProjection.SnapshotStatus.cs`, `AutomationDiagnosticsHub.SnapshotProjection.SnapshotEvaluation.cs`, `AutomationDiagnosticsHub.SnapshotProjection.Audio.cs`, `AutomationDiagnosticsHub.SnapshotProjection.AudioDrops.cs`, `AutomationDiagnosticsHub.SnapshotProjection.AudioSignal.cs`, `AutomationDiagnosticsHub.SnapshotProjection.CaptureIngest.cs`, `AutomationDiagnosticsHub.SnapshotProjection.WasapiAudio.cs`, `AutomationDiagnosticsHub.SnapshotProjection.AvSync.cs`, `AutomationDiagnosticsHub.SnapshotProjection.CaptureCommands.cs`, `AutomationDiagnosticsHub.SnapshotProjection.CaptureFormat.cs`, `AutomationDiagnosticsHub.SnapshotProjection.CaptureTransport.cs`, `AutomationDiagnosticsHub.SnapshotProjection.CaptureCadence.cs`, `AutomationDiagnosticsHub.SnapshotProjection.Mjpeg.cs`, `AutomationDiagnosticsHub.SnapshotProjection.MjpegTiming.cs`, `AutomationDiagnosticsHub.SnapshotProjection.MjpegPreviewJitter.cs`, `AutomationDiagnosticsHub.SnapshotProjection.MjpegPacketHash.cs`, `AutomationDiagnosticsHub.SnapshotProjection.FlashbackExport.cs`, `AutomationDiagnosticsHub.SnapshotProjection.FlashbackExportLastResult.cs`, `AutomationDiagnosticsHub.SnapshotProjection.FlashbackPlayback.cs`, `AutomationDiagnosticsHub.SnapshotProjection.FlashbackPlaybackAudioMaster.cs`, `AutomationDiagnosticsHub.SnapshotProjection.FlashbackPlaybackDecode.cs`, `AutomationDiagnosticsHub.SnapshotProjection.FlashbackPlaybackCommands.cs`, `AutomationDiagnosticsHub.SnapshotProjection.FlashbackRecording.cs`, `AutomationDiagnosticsHub.SnapshotProjection.FlashbackRecordingStartupCache.cs`, `AutomationDiagnosticsHub.SnapshotProjection.FlashbackRecordingQueues.cs`, `AutomationDiagnosticsHub.SnapshotProjection.PreviewD3D.cs`, `AutomationDiagnosticsHub.SnapshotProjection.PreviewD3DCpuTiming.cs`, `AutomationDiagnosticsHub.SnapshotProjection.PreviewD3DFrameFlow.cs`, `AutomationDiagnosticsHub.SnapshotProjection.PreviewD3DFrameLatencyWait.cs`, `AutomationDiagnosticsHub.SnapshotProjection.PreviewD3DFrameStats.cs`, `AutomationDiagnosticsHub.SnapshotProjection.PreviewRuntime.cs`, `AutomationDiagnosticsHub.SnapshotProjection.PreviewRuntimeCadence.cs`, `AutomationDiagnosticsHub.SnapshotProjection.PreviewRuntimeStartup.cs`, `AutomationDiagnosticsHub.SnapshotProjection.ProcessResources.cs`, `AutomationDiagnosticsHub.SnapshotProjection.RecordingBackend.cs`, `AutomationDiagnosticsHub.SnapshotProjection.RecordingIntegrity.cs`, `AutomationDiagnosticsHub.SnapshotProjection.RecordingPipeline.cs`, `AutomationDiagnosticsHub.SnapshotProjection.RecordingOutput.cs`, `AutomationDiagnosticsHub.SnapshotProjection.SourceSignal.cs`, `AutomationDiagnosticsHub.SnapshotProjection.SourceTelemetry.cs`, `AutomationDiagnosticsHub.SnapshotProjection.UserSettings.cs`, `AutomationDiagnosticsHub.SnapshotProjection.RecordingSettings.cs`, `AutomationDiagnosticsHub.SnapshotProjection.HdrPipeline.cs`, `AutomationDiagnosticsHub.Counters.cs`, `AutomationDiagnosticsHub.Deltas.cs`, `AutomationDiagnosticsHub.SnapshotState.cs`, `AutomationDiagnosticsHub.Timeline.cs`, `AutomationDiagnosticsHub.TimelineProjection.cs`, `AutomationDiagnosticsHub.Verification.cs` | additional collectors/controllers when hub orchestration grows |
| Automation snapshot models | `Sussudio/Models/Automation/AutomationSnapshot*.cs`, `AutomationCommandProtocol.cs`, `AutomationOptionsSnapshot.cs`, `CaptureRuntimeSnapshot.cs`, `DiagnosticsEvents.cs`, `FlashbackSegmentInfo.cs`, `PerformanceTimelineEntry.cs`, `PreviewRuntimeSnapshot.cs`, `RecordingVerification.cs`, `VideoSourceProbe.cs`, `ViewModelRuntimeSnapshot.cs`, `WindowAutomation.cs` | automation evidence DTO partials by domain, command protocol DTOs, automation options DTO, capture runtime DTO, diagnostics events, Flashback segment DTOs, performance timeline entry, preview runtime DTO, recording verification DTOs, video source probe DTOs, view-model runtime DTO, window automation DTOs |
| Source telemetry | `Sussudio/Services/Telemetry/NativeXuAtCommandProvider.cs`, `NativeXuAtCommandProvider.AtProtocol.cs`, `NativeXuAtCommandProvider.AudioCommands.cs`, `NativeXuAtCommandProvider.AnalogGain.cs`, `NativeXuAtCommandProvider.AudioSwitch.cs`, `NativeXuAtCommandProvider.Selector4.cs`, `NativeXuAtCommandProvider.DeviceCommands.cs`, `NativeXuAtCommandProvider.DiagnosticSummary.cs`, `NativeXuAtCommandProvider.FullSnapshot.cs`, `NativeXuAtCommandProvider.PayloadDecoding.cs`, `NativeXuAtCommandProvider.RollingPoll.cs`, `NativeXuAtCommandProvider.RollingCommandGroups.cs`, `NativeXuAtCommandProvider.TelemetryDetails.Build.cs`, `NativeXuAtCommandProvider.TelemetryDetails.AudioInput.cs`, `NativeXuAtCommandProvider.TelemetryDetails.Formatters.cs` | Native XU selected-interface validation and ReadAsync orchestration, AT-command transport/parsing, public HDMI/Analog audio route and gain command entry points, analog gain register mapping and writes, HDMI/Analog switch sequence, selector-4 I2C payload writes, generic public device-command surface, diagnostic summary formatting, reference full-snapshot reader, source payload decoding/scalar helpers, active rolling poll cadence/cache/snapshot assembly, rolling command group dispatch and per-command cancellation helpers, source telemetry detail row assembly, flash-audio input interpretation, AT detail value formatting |
| App service contracts | `Sussudio/Services/Contracts/AutomationInterfaces.cs`, `Sussudio/Services/Contracts/IPreviewFrameSink.cs`, `Sussudio/Services/Contracts/ISourceSignalTelemetryProvider.cs`, `Sussudio/Services/Contracts/RecordingContracts.cs`, `Sussudio/Services/Contracts/PooledVideoFrame.cs`, `Sussudio/Services/Contracts/PooledVideoFrameLease.cs`, `Sussudio/Services/Contracts/PreviewFrameTracking.cs` | shared in-process app-service contracts and pooled-frame ownership types; keep these separate from `Sussudio.Automation.Contracts` wire/protocol contracts |
| Recording | `Sussudio/Services/Recording/LibAvEncoder.cs`, `LibAvEncoder.Audio.cs`, `LibAvEncoder.AudioSubmission.cs`, `LibAvEncoder.AudioInitialization.cs`, `LibAvEncoder.CodecPolicy.cs`, `LibAvEncoder.AvSync.cs`, `LibAvEncoder.PacketWriting.cs`, `LibAvEncoder.FrameCopy.cs`, `LibAvEncoder.VideoSubmission.cs`, `LibAvEncoder.Diagnostics.cs`, `LibAvEncoder.AudioSetup.cs`, `LibAvEncoder.HdrSideData.cs`, `LibAvEncoder.Models.cs`, `LibAvEncoder.VideoSetup.cs`, `LibAvEncoder.OutputLifecycle.cs`, `LibAvRecordingSink.cs`, `LibAvRecordingSink.Diagnostics.cs`, `LibAvRecordingSink.Lifetime.cs`, `LibAvRecordingSink.Options.cs`, `LibAvRecordingSink.OutputValidation.cs`, `LibAvRecordingSink.EncodingLoop.cs`, `LibAvRecordingSink.Queues.cs`, `LibAvRecordingSink.QueueCleanup.cs`, `LibAvRecordingSink.AudioQueues.cs`, `RecordingVerifier.cs`, `RecordingVerifier.Ffprobe.cs`, `RecordingVerifier.ProbeParsing.cs`, `RecordingVerifier.Validation.cs`, `RecordingVerifier.Results.cs`, `RecordingVerifier.Cadence.cs` | encoder runtime/lifecycle, audio/microphone shared state and drains, public audio submission, audio stream initialization, codec/options policy, A/V sync diagnostics, video packet drain/write helpers, packed software-frame copy helpers, video frame submission paths, open/error/device-removed diagnostics, video codec/hardware setup, rotation/output cleanup, sink start/stop lifecycle, recording sink diagnostics surface, dispose/deferred cleanup, encoder option creation, stopped-output validation, recording sink encode-drain loop, recording sink video/GPU/CUDA queue surface, recording sink queue cleanup and pooled packet return helpers, recording sink audio queue surface, verifier orchestration/finalizer, ffprobe process work, probe parsing, validation policy, result/taxonomy shaping, verifier cadence analysis |
| Flashback | `FlashbackDecoder.cs`, `FlashbackDecoder.D3D11.cs`, `FlashbackDecoder.VideoOutput.cs`, `FlashbackDecoder.VideoSetup.cs`, `FlashbackDecoder.AudioOutput.cs`, `FlashbackDecoder.Timestamps.cs`, `FlashbackDecoder.Seeking.cs`, `FlashbackDecoder.Validation.cs`, `FlashbackDecoder.Lifetime.cs`, `FlashbackDecoder.Diagnostics.cs`, `FlashbackDecoder.Guards.cs`, `FlashbackDecoder.OutputTypes.cs`, `FlashbackPlaybackController.cs`, `FlashbackPlaybackController.DecoderFiles.cs`, `FlashbackPlaybackController.DecoderReopen.cs`, `FlashbackPlaybackController.Lifecycle.cs`, `FlashbackPlaybackController.Commands.cs`, `FlashbackPlaybackController.CommandQueue.cs`, `FlashbackPlaybackController.CommandCoalescing.cs`, `FlashbackPlaybackController.CommandTelemetry.cs`, `FlashbackPlaybackController.Thread.cs`, `FlashbackPlaybackController.ThreadLifecycle.cs`, `FlashbackPlaybackController.ThreadCleanup.cs`, `FlashbackPlaybackController.ThreadTimer.cs`, `FlashbackPlaybackController.AudioRouting.cs`, `FlashbackPlaybackController.AudioPrebuffer.cs`, `FlashbackPlaybackController.AudioMasterPacing.cs`, `FlashbackPlaybackController.PreviewFrames.cs`, `FlashbackPlaybackController.SeekDisplay.cs`, `FlashbackPlaybackController.PlaybackLoop.cs`, `FlashbackPlaybackController.PlaybackSegmentEdges.cs`, `FlashbackPlaybackController.PlaybackTiming.cs`, `FlashbackPlaybackController.Markers.cs`, `FlashbackPlaybackController.PositionMapping.cs`, `FlashbackPlaybackController.Metrics.cs`, `FlashbackPlaybackController.MetricsCollection.cs`, `FlashbackEncoderSink.cs`, `FlashbackEncoderSink.Startup.cs`, `FlashbackEncoderSink.EncodingLoop.cs`, `FlashbackEncoderSink.PacketDrain.cs`, `FlashbackEncoderSink.SegmentRotation.cs`, `FlashbackEncoderSink.ForceRotate.cs`, `FlashbackEncoderSink.Inputs.cs`, `FlashbackEncoderSink.Lifetime.cs`, `FlashbackEncoderSink.Options.cs`, `FlashbackEncoderSink.Queues.cs`, `FlashbackEncoderSink.Recording.cs`, `FlashbackEncoderSink.RuntimeState.cs`, `FlashbackBufferManager.cs`, `FlashbackBufferManager.SegmentMutation.cs`, `FlashbackBufferManager.Lifecycle.cs`, `FlashbackBufferManager.SegmentQueries.cs`, `FlashbackBufferManager.Math.cs`, `FlashbackBufferManager.Retention.cs`, `FlashbackBufferManager.EvictionPause.cs`, `FlashbackExporter.cs`, `FlashbackExporter.SingleFile.cs`, `FlashbackExporter.Segments.cs`, `FlashbackExporter.SegmentSkipTracking.cs`, `FlashbackExporter.SegmentTemplate.cs`, `FlashbackExporter.SegmentValidation.cs`, `FlashbackExporter.TempFiles.cs`, `FlashbackExporter.Requests.cs`, `FlashbackExporter.Lifetime.cs`, `FlashbackExporter.Execution.cs`, `FlashbackExporter.PacketTiming.cs`, `FlashbackExporter.PacketBuffers.cs`, `FlashbackExporter.Streams.cs`, `FlashbackExporter.StreamTemplates.cs`, `FlashbackExporter.OutputFiles.cs`, `FlashbackExporter.Progress.cs`, `FlashbackExporter.ExportLock.cs`, `FlashbackExporter.Results.cs`, `FlashbackExporter.OutputValidation.cs`, `FlashbackExporter.PathValidation.cs`, `FlashbackExporter.SegmentSelection.cs`, `FlashbackExporter.NativeState.cs`, `FlashbackExporter.Cancellation.cs`, `FlashbackExporter.LibAvErrors.cs`, `FlashbackExporter.TimeMath.cs` | decoder lifecycle/open/decode control flow, D3D11VA decoder discovery/initialization, video frame output/conversion, video codec setup and software output-buffer allocation, decoder audio packet delivery and bounded audio output, decoder timestamp/seek conversion helpers, decoder keyframe/exact seek control flow, decoder stream/frame validation helpers, decoder file-close native cleanup and held-frame release, decoder phase timing and FFmpeg error formatting, decoder state guards, decoded video/audio output DTOs, playback core, decoder file open/cleanup, active fMP4 reopen and seek recovery, component lifecycle and dispose, public playback command facade, command queue/drop policy, seek/scrub coalescing, command readiness/telemetry bookkeeping, playback thread state and loop helpers, playback thread lifecycle, playback thread cleanup, timer-resolution P/Invoke, audio callback/routing/render helpers, audio prebuffer/rewind, audio-master pacing/fallbacks, decoded frame submission/ownership, seek/scrub frame display, continuous playback loop, segment-edge switching/reopen/write-head handling, timing/cadence policy, marker owner, position/file-PTS mapping, public metrics surface, metric collection/reset, encoder core state/helpers, encoder startup/queue initialization, encode loop orchestration, packet drains and progress, segment rotation/failure recovery, export force-rotation handshake, producer/callback input surface, stop/dispose lifecycle, encoder options/packet helpers, encoder queue helpers, retroactive recording lifecycle, public counters/status, buffer live counters and byte/PTS updates, buffer segment mutation surface, buffer initialize/dispose/recovery-preserve lifecycle, buffer segment query/projection helpers, buffer math and saturated accounting helpers, buffer retention/purge/eviction, buffer eviction-pause state and recording PTS range capture, shared exporter native state, single-file export packet-copy/remux core, multi-segment export packet-copy/remux core, segment template setup, segment validation policy, temp-file cleanup, export request routing, exporter disposal, export execution scheduling, packet timestamp helpers, packet buffer lifetime helpers, stream/context setup, stream-template/layout compatibility, final output replacement, export progress/throttle helpers, export lock handling, export failure results, output validation, path validation, segment selection, native cleanup, linked cancellation, FFmpeg error formatting, export time math |
| Preview rendering | `D3D11PreviewRenderer.cs`, `D3D11PreviewRenderer.Configuration.cs`, `D3D11PreviewRenderer.NativeInterop.cs`, `D3D11PreviewRenderer.Lifecycle.cs`, `D3D11PreviewRenderer.FrameTypes.cs`, `D3D11PreviewRenderer.FrameOwnership.cs`, `D3D11PreviewRenderer.DxgiFrameStatistics.cs`, `D3D11PreviewRenderer.Submission.cs`, `D3D11PreviewRenderer.Rendering.cs`, `D3D11PreviewRenderer.ShaderRendering.cs`, `D3D11PreviewRenderer.DeviceLost.cs`, `D3D11PreviewRenderer.DeviceInitialization.cs`, `D3D11PreviewRenderer.FrameUpload.cs`, `D3D11PreviewRenderer.FrameLatency.cs`, `D3D11PreviewRenderer.Viewport.cs`, `D3D11PreviewRenderer.Resources.cs`, `D3D11PreviewRenderer.InputResources.cs`, `D3D11PreviewRenderer.PanelBinding.cs`, `D3D11PreviewRenderer.PendingFrames.cs`, `D3D11PreviewRenderer.Metrics.cs`, `D3D11PreviewRenderer.MetricsTracking.cs`, `D3D11PreviewRenderer.SlowFrameDiagnostics.cs`, `D3D11PreviewRenderer.ScreenshotCapture.cs`, `D3D11PreviewRenderer.ScreenshotEncoding.cs`, `D3D11PreviewRenderer.ShaderSources.cs` | renderer host/public state, env-tuned runtime configuration, native panel/shader/DWM interop, render-thread lifecycle and disposal, pending-frame and metrics model types, submitted/rendered/dropped frame ownership telemetry, DXGI frame statistics and display-clock projection, public frame submission entry points, render loop/VideoProcessor/present paths, NV12/HDR shader draw paths, device-lost classification and recovery, D3D device/shared-device/swap-chain initialization, raw-frame and external-texture upload helpers, frame-latency waitable swap-chain setup/waits, viewport and letterbox helpers, D3D pipeline/view/disposal resources, raw/HDR input texture resources, swap-chain panel binding and composition transforms, pending-frame queue/signaling, read-only present/latency metrics, render-loop metric window tracking/reset, slow-frame diagnostic ring and reason projection, screenshot capture GPU/readback flow, screenshot BMP/error/buffer encoding, shader source, timing models |
| UI shell | `MainWindow.*.cs` XAML adapters plus `Sussudio/Controllers/*Controller.cs` shell controllers | keep shell adapters thin and start new UI behavior in named controllers/policies with ownership tests |
| Presentation | `MainViewModel.*.cs` facade/feature partial family plus focused `Sussudio/ViewModels` policy/presentation helpers | keep the root facade stable while moving pure feature state, policy, and presentation logic into named owners |

## Automation

Primary owner: `Sussudio.Automation.Contracts/`

Entry points:

- `AutomationCommandKind.cs` owns numeric command IDs. Append only; never
  renumber or reuse values.
- `AutomationCommandCatalog.cs` owns command metadata, payload shape, readiness
  gating, timeout policy, path policy, CLI help, and MCP descriptions.
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

Fast checks:

```powershell
dotnet build Sussudio.slnx -p:Platform=x64 --no-restore
dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore
```

Automation diagnostics ownership:

- `Sussudio/Services/Automation/AutomationCommandDispatcher.cs` owns the
  command envelope, manifest/auth/readiness gates, trivial-handler dispatch, and
  error shaping.
- `Sussudio/Services/Automation/AutomationCommandDispatcher.CustomCommands.cs`
  owns the custom automation command router for multi-field payloads, special
  response shapes, diagnostics, and capture routing.
- `Sussudio/Services/Automation/AutomationCommandDispatcher.FlashbackCommands.cs`
  owns Flashback action/export/segment/restart/enable command bodies behind the
  custom command router.
- `Sussudio/Services/Automation/AutomationCommandDispatcher.VerificationCommands.cs`
  owns file and last-recording verification command bodies behind the custom
  command router.
- `Sussudio/Services/Automation/AutomationCommandDispatcher.TrivialHandlers.cs`
  owns the table of simple one-property commands that delegate straight to the
  automation view-model port.
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
- `Sussudio/Services/Automation/AutomationCommandDispatcher.WaitConditions.cs`
  owns wait-condition polling and snapshot predicates.
- `Sussudio/Services/Automation/AutomationCommandDispatcher.Assertions.cs`
  owns AssertSnapshot payload parsing and snapshot comparison helpers.
- `Sussudio/Services/Automation/AutomationCommandDispatcher.Payload.cs` owns
  JSON payload extraction helpers for dispatcher command bodies.
- `Sussudio/Services/Automation/AutomationCommandHandler.cs` owns the shared
  trivial-handler wrapper used by simple one-property automation commands.
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
  snapshot refresh serialization, counters, and timeline flow.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.Alerts.cs` owns alert
  rule evaluation and active-alert transitions.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.SignalAlerts.cs` owns
  preview, capture, audio-signal, and recording-growth alert rules.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.FlashbackAlerts.cs`
  owns Flashback alert orchestration.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.FlashbackRecordingAlerts.cs`
  owns Flashback export, storage, encoder, and recording alert rules.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.FlashbackPlaybackAlerts.cs`
  owns Flashback playback alert orchestration.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.FlashbackPlaybackCommandAlerts.cs`
  owns Flashback playback command queue and command failure alert rules.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.FlashbackPlaybackPerformanceAlerts.cs`
  owns Flashback playback cadence, audio pacing, and submit-failure alert
  rules.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.DiagnosticEvents.cs`
  owns diagnostics event publication, event throttling, Flashback export
  completion events, and recent event storage.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.DiagnosticEvaluation.cs`
  owns diagnostic verdict orchestration and final healthy/mixed fallback.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.DiagnosticEvaluationFlashback.cs`
  owns Flashback-specific diagnostic verdict ordering and summaries.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.DiagnosticEvaluationRealtime.cs`
  owns idle, warmup, recording/audio, source/MJPEG, preview, renderer, and
  present/display diagnostic verdicts.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.DiagnosticEvaluationLanes.cs`
  owns diagnostic lane text formatting used by diagnostic verdicts.
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
  owns `AutomationSnapshot` DTO property projection from runtime/view-model
  snapshots and diagnostic classifiers.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.SnapshotStatus.cs`
  owns timestamp, view-model lifecycle/audio flags, verification-in-progress,
  session state, and status-text projection consumed by `AutomationSnapshot`.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.SnapshotEvaluation.cs`
  owns performance score, diagnostic lane, preview pacing classifier, and
  performance threshold projection consumed by `AutomationSnapshot`.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Audio.cs`
  composes audio, ingest, and WASAPI projection owners into the automation
  snapshot audio/ingest DTO fields.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.AudioDrops.cs`
  owns audio drop counter projection and derived real-time/file-writer drop
  totals consumed by `AutomationSnapshot`.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.AudioSignal.cs`
  owns view-model audio peak/clipping and derived signal-present/muted
  projection consumed by `AutomationSnapshot`.
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
  owns source capture cadence, preview visual cadence, and center-crop visual
  cadence projection inputs consumed by `AutomationSnapshot`.
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
  owns active Flashback export progress, failure, and force-rotate fallback
  projection consumed by `AutomationSnapshot`.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.FlashbackExportLastResult.cs`
  owns final Flashback export last-result projection consumed by
  `AutomationSnapshot`.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.FlashbackRecording.cs`
  owns Flashback recording, buffer, backend, encoder configuration, export
  verification, and codec-downgrade fallback projection consumed by
  `AutomationSnapshot`.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.FlashbackRecordingStartupCache.cs`
  owns Flashback temp-drive and startup cache projection consumed by
  `AutomationSnapshot`.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.FlashbackRecordingQueues.cs`
  owns Flashback video, GPU, and audio queue/backpressure projection consumed
  by `AutomationSnapshot`.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.FlashbackPlayback.cs`
  owns Flashback playback state and frame cadence metrics consumed by
  `AutomationSnapshot`.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.FlashbackPlaybackAudioMaster.cs`
  owns Flashback playback audio-master delay/fallback projection consumed by
  `AutomationSnapshot`.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.FlashbackPlaybackDecode.cs`
  owns Flashback playback seek-cap and decode timing projection consumed by
  `AutomationSnapshot`.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.FlashbackPlaybackCommands.cs`
  owns Flashback playback thread and command queue counter/latency/failure
  projection consumed by `AutomationSnapshot`.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.PreviewD3D.cs`
  owns D3D preview swap-chain and renderer state projection consumed by
  `AutomationSnapshot`.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.PreviewD3DCpuTiming.cs`
  owns D3D CPU upload/render/present/total-frame timing and pipeline latency
  projection consumed by `AutomationSnapshot`.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.PreviewD3DFrameFlow.cs`
  owns D3D submitted/rendered/dropped frame identity, drop reason, and
  slow-frame projection consumed by `AutomationSnapshot`.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.PreviewD3DFrameLatencyWait.cs`
  owns D3D waitable frame-latency counter and timing projection consumed by
  `AutomationSnapshot`.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.PreviewD3DFrameStats.cs`
  owns D3D frame-statistics success/failure, missed-refresh, and present-count
  projection consumed by `AutomationSnapshot`.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.PreviewRuntime.cs`
  owns preview frame counters, estimated pipeline latency, GPU playback state,
  preview HDR state, and preview color-context projection consumed by
  `AutomationSnapshot`.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.PreviewRuntimeCadence.cs`
  owns preview display-cadence interval, jitter, slow-frame, and low-FPS
  projection consumed by `AutomationSnapshot`.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.PreviewRuntimeStartup.cs`
  owns preview startup/readiness signals, recovery, blank/stall, and
  renderer-mode projection consumed by `AutomationSnapshot`.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.ProcessResources.cs`
  owns process memory, CPU, GC, and thread-pool projection consumed by
  `AutomationSnapshot`.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.RecordingIntegrity.cs`
  owns recording-integrity projection inputs consumed by `AutomationSnapshot`.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.RecordingBackend.cs`
  owns recording backend, audio path mode, and mux-result projection consumed by
  `AutomationSnapshot`.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.RecordingPipeline.cs`
  owns encoder queue ages, conversion queue depths, and recording
  video/GPU/CUDA health inputs consumed by `AutomationSnapshot`.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.RecordingOutput.cs`
  owns recording UI output text, accumulated recording bytes, file-growth
  state, last finalized output metadata, and last verification result
  projection consumed by `AutomationSnapshot`.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.SourceSignal.cs`
  owns detected source frame-rate fallback, source dimensions/HDR, and raw
  source signal metadata projection consumed by `AutomationSnapshot`.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.SourceTelemetry.cs`
  owns source telemetry fallback policy, age calculation, and source-target
  summary inputs consumed by `AutomationSnapshot`.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.UserSettings.cs`
  owns selected device, selected capture options, preview volume, and stats
  visibility projection consumed by `AutomationSnapshot`.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.RecordingSettings.cs`
  owns selected recording option projection consumed by `AutomationSnapshot`.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.HdrPipeline.cs`
  owns HDR availability/request state, runtime/readiness fallback, HDR
  warmup/downgrade, pipeline parity, and telemetry-alignment projection consumed
  by `AutomationSnapshot`.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.HdrTruth.cs`
  owns HDR truth verdict projection consumed by `AutomationSnapshot`.
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
- `MfDeviceEnumerator.cs` owns shared Media Foundation constants, GUIDs, and
  P/Invoke declarations.
- `MfDeviceEnumerator.VideoDevices.cs` owns native MF video-device enumeration.
- `MfDeviceEnumerator.AudioEndpoints.cs` owns WASAPI capture endpoint
  enumeration and friendly-name reads.
- `MfDeviceEnumerator.FormatProbe.cs` owns native video format probing, MF
  source fallback activation, and subtype/FourCC naming.
- `CaptureService.cs` owns shared service state, construction, and the
  event/property surface. It should not receive unrelated UI, Flashback,
  recording lifecycle, or diagnostics behavior.
- `CaptureService.Initialization.cs` owns the public initialization transition,
  initial selected device/settings capture, negotiated-format seeding, the
  initial observed-pixel telemetry reset call, fallback source telemetry,
  telemetry refresh, NTSC frame-rate correction, and initialized status event.
- `CaptureService.Audio.cs` owns audio preview, capture-failure handling, and
  live audio input switching.
- `CaptureService.MicrophoneMonitor.cs` owns microphone monitoring, mic-level
  event projection, preview-time mic writer attachment, and mic capture cleanup.
- `CaptureService.WasapiPlayback.cs` owns WASAPI playback startup/shutdown,
  audio-monitor attach/detach order, and playback best-effort cleanup helpers.
- `CaptureService.Cleanup.cs` owns explicit cleanup transitions, app shutdown
  teardown, and Flashback segment preservation when cleanup finalization fails.
- `CaptureService.Coordination.cs` owns transition serialization, steady-state
  resolution, disposal, and best-effort semaphore/eviction cleanup helpers.
- `CaptureService.DeferredCleanup.cs` owns Flashback backend/export lock release
  helpers plus deferred Flashback and unified-video cleanup after drains.
- `CaptureService.Failures.cs` owns fatal capture/recording/Flashback backend
  failure callbacks, last-failure telemetry, and fault cleanup launchers.
- `CaptureService.FlashbackControls.cs` owns Flashback public state, segment
  access, enable/settings mutations, restarts, format changes, and encoder
  setting cycles.
- `CaptureService.FlashbackOrchestration.cs` owns committed restart
  orchestration after preview backend teardown.
- `CaptureService.FlashbackAudioInputs.cs` owns WASAPI and microphone input
  restoration for Flashback preview/recording backends.
- `CaptureService.FlashbackPreviewBackend.cs` owns Flashback preview backend
  startup: buffer manager, encoder sink, exporter, playback controller, and
  producer attachment.
- `CaptureService.FlashbackPreviewBackendDisposal.cs` owns Flashback preview
  backend teardown, detach order, and deferred artifact cleanup scheduling.
- `CaptureService.FlashbackBufferCycle.cs` owns sink-only buffer cycling after
  recording stops and fallback full rebuilds.
- `CaptureService.FlashbackExportDiagnostics.cs` owns Flashback export
  diagnostic state, progress forwarding, rejection records, and force-rotate
  fallback counters.
- `CaptureService.FlashbackExportFailureClassification.cs` owns the export
  failure-kind taxonomy shared by capture diagnostics and automation responses.
- `CaptureService.FlashbackExportOperations.cs` owns Flashback export entry
  points and the core export operation flow.
- `CaptureService.FlashbackExportPlanning.cs` owns segment metadata mapping,
  live-export throttle policy, range clamps, and PTS offset helpers.
- `CaptureService.FlashbackRecording.cs` owns Flashback recording backend
  ownership checks, session context construction, frame-rate rational policy,
  codec/HDR guardrails, and recording topology validation.
- `CaptureService.HealthSnapshots.cs` builds health snapshots consumed by
  diagnostics and automation health checks.
- `CaptureService.HealthSnapshots.AvSync.cs` owns A/V sync field projection for
  health snapshots while sharing drift calculation policy with runtime snapshots.
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
  state, cadence, decode, audio-master, and command-queue field projection for
  health snapshots.
- `CaptureService.HealthSnapshotRecording.cs` owns active recording and
  Flashback-recording queue/failure field projection for health snapshots.
- `CaptureService.PreviewLifecycle.cs` owns video preview start/stop,
  retained-backend reuse checks, preview-start rollback, and preview pipeline
  disposal ordering.
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
  transition, Flashback recording fast-path reuse, standard LibAv recording
  startup, and start rollback ordering. `CaptureService.RecordingStopLifecycle.cs`
  owns normal and emergency recording stop transition routing.
- `CaptureService.RecordingFinalizeRecord.cs` owns recording stop/finalize
  routing for active Flashback and LibAv backends plus shared post-stop state
  cleanup.
- `CaptureService.RecordingFinalizeFlashback.cs` owns Flashback recording
  export finalization, live-edge boundary snapshots, and cancellation-result
  classification.
- `CaptureService.RecordingRollback.cs` owns transient backend teardown after
  recording-start failures, including best-effort sink, WASAPI, unified-video,
  and deferred LibAv drain cleanup.
- `CaptureService.RuntimeSnapshots.cs` builds runtime snapshots consumed by UI,
  automation, and verification.
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
- `CaptureService.SnapshotRecordingFormat.cs` owns encoder codec, output pixel
  format, video profile, and requested frame-rate argument projection.
- `CaptureService.SnapshotObservedFrames.cs` owns observed frame-format
  telemetry projection from explicit counters.
- `CaptureService.SnapshotAvSync.cs` owns A/V sync drift snapshot helpers for
  live source/audio drift and encoder correction telemetry.
- `CaptureService.SnapshotTelemetry.cs` owns source telemetry snapshot
  presentation policy, telemetry/request alignment, and HDR warmup state
  classification.
- `CaptureService.ObservedPixelTelemetry.cs` owns observed pixel-format
  normalization, reset, and explicit counter updates.
- `CaptureService.Telemetry.cs` owns source telemetry polling, fallback merge,
  and NTSC frame-rate correction.
- `UnifiedVideoCapture.cs` owns frame arrival routing for preview, recording,
  and Flashback.
- `UnifiedVideoCapture.Lifecycle.cs` owns source-reader initialization,
  read-loop start/stop, preview-reinit disposal, and capture/MJPEG fatal-error
  callbacks.
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

- `LibAvEncoder.cs` owns encoder fields, FFmpeg initialization forwarding,
  encoder initialization, output rotation, and final close/dispose flow.
- `LibAvEncoder.Audio.cs` owns audio/microphone stream state, public status
  properties, packet writing, and pending-sample drain helpers.
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
  D3D11/CUDA hardware frames, texture pools, and video bitstream-filter setup.
- `LibAvEncoder.VideoSubmission.cs` owns CPU packed frame submission, D3D11
  texture submission, CUDA frame submission, forced-keyframe handling, per-frame
  HDR side-data attachment/removal, and video packet drains.
- `LibAvEncoder.OutputLifecycle.cs` owns output IO close/reopen, stream
  reinitialization, MP4 muxer options, segment runtime resets, and native
  cleanup/freeing.
- `RecordingVerifier.cs` owns strict verification orchestration and keeps the
  public verifier surface stable.
- `RecordingVerifier.Ffprobe.cs` owns ffprobe path resolution, process specs,
  accessibility checks, and HDR side-data probing.
- `RecordingVerifier.ProbeParsing.cs` owns scalar/key-value parsing of ffprobe
  output.
- `RecordingVerifier.Validation.cs` owns container, codec, dimensions, frame
  rate, HDR, and cadence mismatch policy.
- `RecordingVerifier.Results.cs` owns early failure results, primary mismatch
  parsing, HDR parity, and mismatch taxonomy.
- `RecordingVerifier.Cadence.cs` owns ffprobe frame timestamp parsing and
  cadence/drop/jitter metric calculation.

## Flashback

Primary current owner: `Sussudio/Services/Flashback/`

Entry points:

- `FlashbackBackendResources.cs` owns backend resource grouping.
- `FlashbackBufferManager.cs` owns buffer live counters, byte/PTS accounting updates, and core state.
- `FlashbackBufferManager.SegmentMutation.cs` owns active segment path generation, active segment start/abandonment, completion registration, and same-path segment extension.
- `FlashbackBufferManager.Lifecycle.cs` owns initialization, segment extension setup, recovery-preserve markers, disposal, and disposed-state guards.
- `FlashbackBufferManager.SegmentQueries.cs` owns read-only segment counts, active-path projection, segment path lookup, start-PTS lookup, and segment-info projection.
- `FlashbackBufferManager.Retention.cs` owns segment purge, eviction selection, and guarded file deletion behavior.
- `FlashbackBufferManager.EvictionPause.cs` owns eviction-pause state, recording PTS range capture, and pause-driven disk warning state.
- `FlashbackDecoder.cs` owns decoder lifecycle, file open/close, decode control flow, and native cleanup.
- `FlashbackDecoder.Seeking.cs` owns keyframe/exact seek control flow, pending-frame transfer, seek-cap diagnostics, and seek-buffer flushing.
- `FlashbackDecoder.D3D11.cs` owns D3D11VA decoder selection, get-format callback behavior, hardware-config diagnostics, and hardware decoder context setup.
- `FlashbackDecoder.VideoOutput.cs` owns decoded video frame output, D3D11 surface validation, software frame validation, plane copies, and YUV-to-NV12/P010 conversion.
- `FlashbackDecoder.AudioOutput.cs` owns audio codec/resampler initialization, audio packet delivery, callback failure handling, resampler output conversion, and bounded audio buffer sizing.
- `FlashbackDecoder.Timestamps.cs` owns PTS-to-TimeSpan conversion, seek timestamp conversion, best-effort frame timestamp selection, and recoverable seek log suppression.
- `FlashbackDecoder.Validation.cs` owns decoded frame-size calculation, video-dimension validation, input stream-count bounds, and stream-index bounds.
- `FlashbackPlaybackController*.cs` owns playback, scrub, and marker control.
- `FlashbackPlaybackController.DecoderFiles.cs` owns decoder creation, active file identity, file open checks, best-effort close handling, and decoder cleanup.
- `FlashbackPlaybackController.DecoderReopen.cs` owns active fMP4 reopen retry, adjacent-segment seek fallback, keyframe-reopen recovery, and near-live reopen guards.
- `FlashbackPlaybackController.Lifecycle.cs` owns initialize/update component references, preview-detach cleanup, deferred reattach, and dispose.
- `FlashbackPlaybackController.Commands.cs` owns public playback command entry points for scrub, seek, play/pause, go-live, and nudge.
- `FlashbackPlaybackController.CommandQueue.cs` owns command queue writes and queue drop policy.
- `FlashbackPlaybackController.CommandCoalescing.cs` owns seek/scrub coalescing slots, queued-command resolution, and control-yield peek policy.
- `FlashbackPlaybackController.CommandTelemetry.cs` owns command readiness guards, failure-detail formatting, and queue command telemetry bookkeeping.
- `FlashbackPlaybackController.Thread.cs` is the playback-thread shell.
- `FlashbackPlaybackController.ThreadLoop.cs` owns `PlaybackThreadEntry` and
  worker-loop command dispatch.
- `FlashbackPlaybackController.ThreadLifecycle.cs` owns playback thread start/stop, channel recreation/completion, abandoned command draining, and join/cancel diagnostics.
- `FlashbackPlaybackController.ThreadCleanup.cs` owns playback-thread live-restore cleanup and playback CTS disposal warnings.
- `FlashbackPlaybackController.ThreadTimer.cs` owns timer-resolution P/Invoke for playback pacing sleeps.
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
- `FlashbackEncoderSink.Queues.cs` owns queue completion/signaling, queue-depth accounting, enqueue rejection guards/logging, hot audio packet enqueue, and remaining-buffer cleanup.
- `FlashbackEncoderSink.EncodingLoop.cs` owns the background encode loop, drain ordering, force-rotate drain orchestration, cancellation handling, fatal cleanup, and final segment registration.
- `FlashbackEncoderSink.PacketDrain.cs` owns bounded video/GPU/audio/microphone packet drains, encoder PTS resolution, latest-PTS and disk-byte refresh, and frame-encoded event dispatch.
- `FlashbackEncoderSink.SegmentRotation.cs` owns rolling segment rotation, active-segment registration, disk-byte refresh after rotation, and rotation-failure recovery.
- `FlashbackEncoderSink.ForceRotate.cs` owns export force-rotate requests, timeout/cancellation classification, pending-request cleanup, and force-rotate drain abort policy.
- `FlashbackEncoderSink.Inputs.cs` owns raw/lease/GPU video enqueue entry points, audio/microphone enqueue entry points, and hot WASAPI writer adapters.
- `FlashbackEncoderSink.Lifetime.cs` owns `StopAsync`, `Dispose`/`DisposeAsync`, deferred cleanup, cancellation/disposal helpers, and stop-drain timeout classification.
- `FlashbackEncoderSink.Recording.cs` owns the `IRecordingSink.StartAsync` adapter, retroactive recording begin/cancel/end lifecycle, recording PTS boundaries, and recording availability checks.
- `FlashbackEncoderSink.RuntimeState.cs` owns public counters, queue-depth/status projections, encoder format summaries, fatal-error callback registration, and the frame-encoded event surface.
- `FlashbackExporter.cs` owns shared native export state and constants.
- `FlashbackExporter.SingleFile.cs` owns the single-file packet-copy/remux core.
- `FlashbackExporter.Segments.cs` owns the multi-segment packet-copy/remux core.
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
  wrapper disposal, background thread priority, adaptive throttling, and segment snapshots.
- `FlashbackExporter.PacketTiming.cs` owns packet timestamp normalization and
  segment boundary timestamp repair.
- `FlashbackExporter.PacketBuffers.cs` owns packet clone/free helpers and
  buffered packet flushes.
- `FlashbackExporter.Streams.cs` owns input/output FFmpeg context setup,
  stream count validation, and output header writing.
- `FlashbackExporter.StreamTemplates.cs` owns stream-template copying and
  segment stream-layout compatibility checks.
- `FlashbackExporter.OutputFiles.cs` owns temp-output validation, atomic
  destination replacement, overwrite policy, and invalid final-output cleanup.
- `FlashbackExporter.Progress.cs` owns progress normalization/reporting,
  heartbeat cadence, and export writer throttle/yield policy.
- `FlashbackExporter.TempFiles.cs` owns temp output cleanup, stale temp
  preparation, and orphaned `.mp4.tmp` cleanup.
- `FlashbackExporter.ExportLock.cs` owns export-lock wait, release, timeout,
  cancellation, and disposal warning policy.
- `FlashbackExporter.Results.cs` owns shared cancelled/disposed export failure
  result creation.
- `FlashbackExporter.OutputValidation.cs` owns completed-output length probing
  and final output validation text.
- `FlashbackExporter.PathValidation.cs` owns normalized path comparison and
  output path validation.
- `FlashbackExporter.SegmentSelection.cs` owns export-range validation and
  segment/export-range overlap classification.
- `FlashbackExporter.NativeState.cs` owns active input/output close and native
  FFmpeg cleanup.
- `FlashbackExporter.Cancellation.cs` owns linked export cancellation-source
  creation, best-effort disposal, and dispose-CTS reference cleanup.
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
- `Sussudio/Controllers/FullScreenController.cs` owns fullscreen public
  toggle/state and shared context; `FullScreenController.Transitions.cs` owns
  enter/exit orchestration, `FullScreenController.Animation.cs` owns rect
  animation and size waits, `FullScreenController.Chrome.cs` owns chrome/material
  state, and `FullScreenController.Controls.cs` owns overlay pointer/auto-hide
  behavior. Keep `MainWindow.FullScreen.cs` as the XAML-facing adapter.
  `MainWindow.FullScreenFlashbackBridge.cs` owns Flashback fullscreen keyboard
  shortcuts, timeline visibility, and scrub-end bridging.
- `Sussudio/Controllers/WindowScreenshotController.cs` owns automation whole-
  window screenshot dispatch, UI-thread enqueue/cancellation, and failure
  wrapping. `Sussudio/Controllers/WindowScreenshotNativeCapture.cs` owns native
  PrintWindow capture, GDI/DIB lifetime, output directory creation, and
  screenshot result shaping. `Sussudio/Controllers/WindowScreenshotImageEncoder.cs`
  owns the pure PNG/BMP byte-stream encoding helpers. Keep
  `MainWindow.Screenshot.cs` as the `IAutomationWindowControl` adapter.
- `Sussudio/Controllers/PreviewScreenshotController.cs` owns the XAML preview-
  frame screenshot button workflow: output directory fallback, file naming,
  preview-frame capture, status text, logging, and button enable/disable state.
  `MainWindow.PreviewScreenshot.cs` is the XAML-facing adapter.
- `Sussudio/Controllers/WindowAutomationController.cs` owns window geometry
  automation plus the recordings-folder command. `MainWindow.WindowAutomation.cs`
  is the `IAutomationWindowControl` adapter; recording-aware close handling
  stays with the close lifecycle owner.
- `Sussudio/MainWindow.AutomationHost.cs` owns shell automation composition:
  automation token/pipe-name resolution, diagnostics hub construction,
  command dispatcher construction, and named-pipe server construction.
- `Sussudio/MainWindow.Startup.cs` owns first-load startup, first-frame
  uncloak scheduling, initial ViewModel/device refresh, automation pipe hosting,
  and the launch entrance trigger. Close/finalize behavior stays in
  `MainWindow.CloseLifecycle.cs`.
- `Sussudio/Controllers/PreviewResizeTelemetryController.cs` owns top-level
  preview resize telemetry throttling and reset state for preview compositor
  transforms. `MainWindow.WindowSizing.cs` is the `SizeChanged` adapter.
  `MainWindow.PreviewRenderer.cs` owns preview renderer instances, frame
  counters, expected-present interval, and renderer cadence state.
  `MainWindow.PreviewRendererReinit.cs` owns preview renderer reinit safety
  telemetry, fresh SwapChainPanel replacement, and retired-renderer handoff.
  `MainWindow.PreviewSurface.cs` owns preview surface sizing, GPU panel
  visibility, and video/control-bar composition shadows.
- `Sussudio/MainWindow.PreviewRuntimeSnapshot.cs` owns the UI-thread automation
  preview snapshot provider that dispatches to the renderer/startup snapshot
  adapter and gathers UI-thread-only state. `Sussudio/Controllers/PreviewRuntimeSnapshotController.cs`
  owns the read-only preview runtime snapshot construction, including renderer
  metrics, blank/stall suspicion, cadence projection, and D3D diagnostic fields.
  Close/finalize behavior stays with `MainWindow.CloseLifecycle.cs`.
- `Sussudio/MainWindow.WindowTitle.cs` keeps the XAML-facing title update hook;
  `Sussudio/Controllers/WindowTitleController.cs` owns window title
  base/build-stamp formatting and the recording-time suffix used by property
  changes.
- `Sussudio/Controllers/StatusStripPresentationController.cs` owns bottom
  status-strip projection: status text, recording time, disk warning,
  disk-space text, recording size, recording bitrate, and the Flashback bitrate
  fallback used while Flashback is enabled and recording is idle.
  `Sussudio/MainWindow.StatusStripPresentation.cs` is the XAML-facing adapter,
  owns the status-strip `PropertyChanged` router, and preserves the
  recording-only title refresh on recording-time updates.
- `Sussudio/Controllers/WindowCloseLifecycleController.cs` owns window close
  request flags, completion TCS, cleanup latch, recording-stop handoff flags,
  close-in-progress exception classification, and automation close dispatch
  orchestration.
- `Sussudio/MainWindow.CloseLifecycle.cs` owns the XAML/AppWindow close adapter:
  `AppWindow.Closing`, recording-aware pre-close protection, input dimming/
  restoration while recording stops, and the actual `Close()`/Exit fallback.
- `Sussudio/MainWindow.ShutdownCleanup.cs` owns `Closed` shutdown cleanup:
  timer stops, event detaches, preview shutdown, automation diagnostics disposal,
  NVML disposal, and ViewModel disposal.
- `Sussudio/Controllers/NativeWindowBootstrapController.cs` owns native window
  bootstrap: `AppWindow` lookup, ViewModel window handle handoff,
  minimum-size subclassing, DWM cloak/dark-mode setup, initial shell size, icon,
  and native helpers used by shell startup and automation controllers.
  `Sussudio/MainWindow.NativeWindow.cs` is the XAML-facing adapter and keeps
  the `_hwnd` field consumed by screenshot and window automation paths.
- `Sussudio/MainWindow.Dispatching.cs` owns UI-thread enqueue helpers and
  guarded async event-handler execution used by automation adapters and XAML
  event handlers.
- `Sussudio/MainWindow.Bindings.cs` owns the root `SetupBindings()`
  orchestration and leaves feature-specific binding clusters in focused
  partials or controllers, including initial status-strip projection.
- `Sussudio/MainWindow.EventHandlers.cs` owns the preview button handler because
  it coordinates preview fade/reinit behavior. One-line XAML command bridges for
  capture-device, recording, output-path, and preview-screenshot buttons live in
  their feature adapter partials beside the owning controllers.
- `Sussudio/MainWindow.PropertyChanged.cs` owns only the root ViewModel
  PropertyChanged event envelope, property-name normalization, and route order.
  Capture-selection and status-strip routers are still considered first through
  `MainWindow.CaptureSelectionBindings.cs` and
  `MainWindow.StatusStripPresentation.cs`; broad domain property-name switches
  live in focused `MainWindow.PropertyChanged*.cs` partials.
- `Sussudio/Controllers/CompositionShadowFadeAnimator.cs` owns shared
  compositor opacity fade helpers for shell shadow visuals. XAML-facing
  adapters call it without adding state or dispatcher hops.
- `Sussudio/Controllers/AudioMeterController.cs` owns audio/microphone meter
  setup and shared runtime fields.
  `Sussudio/Controllers/AudioMeterController.Context.cs` owns the XAML/view-model
  dependency bag, `Sussudio/Controllers/AudioMeterController.MeterState.cs`
  owns smoothing, peak/range markers, microphone meter clipping, reset behavior,
  timer lifetime, and `TranslateMarker`, and
  `Sussudio/Controllers/AudioMeterController.PresentationAnimations.cs` owns
  monitoring/disabled animations plus rounded content clips.
  `Sussudio/MainWindow.AudioMeter.cs` is its XAML-facing adapter.
  `Sussudio/Controllers/AudioControlBindingController.cs` owns
  audio/microphone initial control projection and event hookup during
  `SetupBindings()`. `Sussudio/MainWindow.AudioBindings.cs` is its
  XAML-facing adapter.
- `Sussudio/Controllers/StatsOverlayController.cs` owns stats dock visibility
  orchestration, frame-time overlay visibility, and polling lifetime.
  `Sussudio/Controllers/StatsOverlayController.DockAnimation.cs` owns stats dock
  show/hide storyboard construction, dock visibility mutations, and completion
  state.
  `Sussudio/Controllers/StatsDockRefreshController.cs` owns stats dock refresh
  orchestration: snapshot acquisition, dock presentation build/apply,
  diagnostics visibility gating, and decode/GPU row refresh ordering.
  `Sussudio/Controllers/StatsDockPresentationController.cs` owns
  stats dock metric text, visibility, and status brush application after the
  presentation model is built. `Sussudio/Controllers/StatsSectionChromeController.cs`
  owns stats dock section expand/collapse chrome and automation-visible section
  visibility application, and `Sussudio/MainWindow.StatsSections.cs` is its
  XAML/automation adapter. `Sussudio/Controllers/StatsWindowPresentationController.cs`
  owns detached stats-window metric text and telemetry-detail row rendering.
  `Sussudio/Controllers/StatsSnapshotProvider.cs` owns shell stats snapshot
  orchestration from capture-health, renderer metrics, and view state.
  `Sussudio/Controllers/StatsSnapshotProvider.RenderMetrics.cs` owns renderer
  cadence/recent-sample acquisition and null fallback policy.
  `Sussudio/MainWindow.StatsSnapshot.cs` is the XAML-facing adapter.
  `MainWindow.StatsOverlay.cs` is the XAML-facing adapter for stats dock
  visibility, polling, and refresh controllers.
- `tests/Sussudio.Tests/StatsOverlay.Contract.Tests.cs` owns legacy harness
  contract checks for stats overlay lifecycle wiring, stats section chrome,
  stats dock refresh orchestration, and diagnostic row pooling.
- `Sussudio/Controllers/StatsDiagnosticRowsController.cs` owns dynamic
  decode/GPU/diagnostic row pools, empty-state rows, group headers, and
  diagnostic row style updates. `Sussudio/Controllers/StatsDockRefreshController.cs`
  delegates diagnostic row presentation to it.
- `Sussudio/Controllers/FrameTimeOverlayPresentationController.cs` owns compact
  frame-time overlay text projection and graph line drawing. Keep frame-time
  canvas math there, while `Sussudio/MainWindow.FrameTimeOverlay.cs` owns the
  XAML-facing compact overlay adapter and presentation-controller composition.
  `Sussudio/ViewModels/StatsPresentationBuilder.cs` owns shared stats
  formatting helpers.
  `Sussudio/ViewModels/StatsPresentationBuilder.Dock.cs` owns stats dock
  summary construction and HDMI/capture/preview resolution text.
  `Sussudio/ViewModels/StatsPresentationBuilder.FrameTime.cs` owns compact
  preview-stat formatting, range/sample text policy, and frame-time overlay
  presentation. `Sussudio/ViewModels/StatsPresentationBuilder.Visual.cs` owns
  visual-cadence FPS/repeat/motion text formatting.
  `Sussudio/ViewModels/StatsPresentationBuilder.Encoder.cs` owns encoder dock
  visibility, codec label, bitrate, and encoder drift text formatting.
  `Sussudio/ViewModels/StatsPresentationBuilder.DiagnosticRows.cs` owns
  diagnostic row construction and source-summary parsing.
  `Sussudio/ViewModels/StatsPresentationBuilder.HardwareRows.cs` owns decode
  and GPU row text projection over presentation inputs.
  `Sussudio/ViewModels/StatsPresentationBuilder.DiagnosticSummary.cs` owns
  frame-lane diagnostic health summary classification.
  `Sussudio/ViewModels/StatsPresentationBuilder.Window.cs` owns detached
  stats-window text and telemetry-detail presentation.
  `Sussudio/ViewModels/StatsPresentationBuilder.Status.cs`
  owns stats lane status classification and visual-repeat drift policy.
  `Sussudio/ViewModels/StatsPresentationModels.cs` owns the internal DTO
  records/enums consumed by the stats overlay and stats-window controllers.
  `Sussudio/ViewModels/StatsSnapshot.cs` owns the UI stats snapshot DTO, and
  `Sussudio/ViewModels/StatsSnapshotBuilder.cs` owns capture-health, renderer,
  and shell view-state projection into that DTO after acquisition.
- `Sussudio/ViewModels/CaptureModeOptionsBuilder.cs` owns pure resolution and
  video-format option construction, HDR mode enablement, and source aspect-ratio
  filtering. Shell files bind and display those options.
- `tests/Sussudio.Tests/StatsPresentation.Contract.Tests.cs` is the stats
  presentation contract marker shell. `StatsPresentation.Ownership.Tests.cs`
  owns builder/controller/DTO source-shape assertions,
  `StatsPresentation.SourceTelemetry.Tests.cs` owns HDMI source telemetry panel
  projection checks, `StatsPresentation.Window.Tests.cs` owns detached-window
  formatting, `StatsPresentation.Encoder.Tests.cs` owns dock encoder formatting,
  and `StatsPresentation.FrameTime.Tests.cs` owns compact preview summary and
  frame-time range policy checks.
- `tests/Sussudio.Tests/MainWindowUiContract.AutomationIds.Tests.cs` owns
  MainWindow automation ID inventory checks.
- `tests/Sussudio.Tests/MainWindowUiContract.WindowAutomation.Tests.cs` owns
  MainWindow full-screen and window automation contract checks.
- `tests/Sussudio.Tests/MainWindowUiContract.Dispatching.Tests.cs` owns
  MainWindow UI-dispatching contract checks.
- `tests/Sussudio.Tests/MainWindowUiContract.StatsSnapshot.Tests.cs` owns stats
  snapshot builder contract checks.
- `tests/Sussudio.Tests/MainWindowUiContract.Tests.cs` is the MainWindow UI
  contract marker shell.
- `tests/Sussudio.Tests/MainWindow.ShellOwnership.Tests.cs` is the MainWindow
  shell-ownership marker shell.
- `tests/Sussudio.Tests/MainWindow.ShellOwnership.Chrome.Tests.cs` owns
  MainWindow shell chrome ownership assertions for the settings shelf and window
  title.
- `tests/Sussudio.Tests/MainWindow.ShellOwnership.Startup.Tests.cs` owns
  MainWindow startup/launch ownership assertions for splash loading phrases,
  launch entrance animation, and first-load hosting.
- `tests/Sussudio.Tests/MainWindow.ShellOwnership.PreviewRuntime.Tests.cs`
  owns MainWindow preview resize telemetry and preview runtime/snapshot ownership
  assertions.
- `tests/Sussudio.Tests/MainWindow.ShellOwnership.WindowLifecycle.Tests.cs`
  owns MainWindow close lifecycle and native bootstrap ownership assertions.
- `tests/Sussudio.Tests/MainWindow.ControllerOwnership.Visual.Tests.cs` owns
  MainWindow controller-adapter ownership assertions for control bar, shell
  elevation, preview-transition, preview startup overlay, preview fade-in, and
  record-button width visual controllers.
- `tests/Sussudio.Tests/MainWindow.ControllerOwnership.Tests.cs` is the
  MainWindow controller-adapter ownership marker shell.
- `tests/Sussudio.Tests/MainWindow.ControllerOwnership.Interaction.Tests.cs`
  owns MainWindow controller-adapter ownership assertions for recording action,
  live signal info, status-strip presentation, preview audio fade, microphone
  controls.
- `tests/Sussudio.Tests/MainWindow.ControllerOwnership.Layout.Tests.cs` owns
  MainWindow responsive shell layout controller-adapter and breakpoint/placement
  policy assertions.
- `tests/Sussudio.Tests/MainWindow.ControllerOwnership.Capture.Tests.cs` owns
  MainWindow capture selection, capture device action, capture option
  presentation, and capture option binding setup ownership assertions.
- `tests/Sussudio.Tests/MainWindow.ControllerOwnership.Output.Tests.cs` owns
  MainWindow output path display/actions and preview screenshot workflow
  ownership assertions.
- `tests/Sussudio.Tests/MainWindow.FlashbackOwnership.Tests.cs` owns MainWindow
  Flashback polling, playhead motion, and marker-presentation ownership
  assertions.
- `tests/Sussudio.Tests/MainViewModel.Automation.Tests.cs` is the automation
  view-model test family marker shell.
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
  reader, diagnostic-session, and tool-surface source text used by refresh
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
- `tests/Sussudio.Tests/MainViewModel.Automation.DiagnosticsProjection.Tests.cs`
  is now only the automation diagnostics projection test family marker shell.
  Keep new projection ownership assertions in the focused owner files:
  `DiagnosticsProjection.Snapshot.Tests.cs`, `DiagnosticsProjection.Audio.Tests.cs`,
  `DiagnosticsProjection.Capture.Tests.cs`, `DiagnosticsProjection.Mjpeg.Tests.cs`,
  `DiagnosticsProjection.Recording.Tests.cs`, `DiagnosticsProjection.System.Tests.cs`,
  `DiagnosticsProjection.Preview.Tests.cs`, and
  `DiagnosticsProjection.Flashback.Tests.cs`.
- `tests/Sussudio.Tests/MainViewModel.Automation.RuntimeSafety.Tests.cs` owns
  automation timeout, recording failure propagation, safe close, screenshot,
  preview-stop surface, process supervisor, and emergency-stop assertions.
- `tests/Sussudio.Tests/MainViewModel.Automation.FlashbackCleanup.Tests.cs`
  owns Flashback startup-cache and session-recovery cleanup ownership
  assertions that used to live in the automation test catch-all.
- `tests/Sussudio.Tests/MainViewModel.Capture.SettingsProjection.Tests.cs`
  owns capture settings projection ownership assertions.
- `tests/Sussudio.Tests/MainViewModel.Capture.AudioMonitoring.Tests.cs` owns
  capture audio-monitoring coordinator surface assertions.
- `tests/Sussudio.Tests/MainViewModel.Capture.OutputPath.Tests.cs` owns
  output folder picker ownership assertions.
- `tests/Sussudio.Tests/MainViewModel.Capture.TestHelpers.cs` owns shared
  MainViewModel source-inspection helpers for capture-facing tests.
- `tests/Sussudio.Tests/MainViewModel.Capture.SelectionPolicy.Tests.cs` owns
  capture option, resolution-selection policy, frame-rate timing, live
  pixel-format, and runtime error-projection ownership assertions.
- `tests/Sussudio.Tests/MainViewModel.Capture.PreviewStartup.Tests.cs` owns
  preview startup, preview reveal, and preview stop ordering assertions.
- `tests/Sussudio.Tests/MainViewModel.Capture.FlashbackExport.Tests.cs` owns
  Flashback export backend-lease and export-operation lock assertions.
- `tests/Sussudio.Tests/MainViewModel.Capture.FlashbackRouting.Tests.cs` is the
  Flashback routing test family marker shell.
- `tests/Sussudio.Tests/MainViewModel.Capture.FlashbackRouting.ViewModel.Tests.cs`
  owns MainViewModel Flashback coordinator-routing assertions.
- `tests/Sussudio.Tests/MainViewModel.Capture.FlashbackRouting.Scrub.Tests.cs`
  owns Flashback scrub, release/cancel/capture-lost, and fullscreen Flashback
  bridge assertions: shortcut gating, timeline visibility, and scrub-end
  handoff.
- `tests/Sussudio.Tests/MainViewModel.Capture.FlashbackRouting.Toggle.Tests.cs`
  owns Flashback timeline toggle rollback and lockout assertions.
- `tests/Sussudio.Tests/MainViewModel.Capture.FlashbackBackend.Tests.cs` is the
  Flashback backend test family marker shell.
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
- `tests/Sussudio.Tests/DeviceModels.Tests.cs` is the device model marker
  shell.
- `tests/Sussudio.Tests/DeviceModels.AudioInput.Tests.cs` owns audio input
  model display-name contract checks.
- `tests/Sussudio.Tests/DeviceModels.AudioLevel.Tests.cs` owns audio level
  event model contract checks.
- `tests/Sussudio.Tests/DeviceModels.CaptureDevice.Tests.cs` owns capture
  device metadata and default collection contract checks.
- `tests/Sussudio.Tests/DeviceModels.MediaFormat.Tests.cs` owns MediaFormat
  equality and hash-code contract checks.
- `tests/Sussudio.Tests/DeviceModels.PropertyAssertions.cs` owns shared device
  model property reflection assertions.
- `tests/Sussudio.Tests/SnapshotModels.Tests.cs` owns shared snapshot-model
  spec DTOs and registration state.
- `tests/Sussudio.Tests/SnapshotModels.PropertyAssertions.cs` owns shared
  snapshot property-list, nullability, and helper assertion methods.
- `tests/Sussudio.Tests/SnapshotModels.ReflectionJson.cs` owns shared
  reflection JSON round-trip and registered-property coverage helpers.
- `tests/Sussudio.Tests/SnapshotModels.Automation.Tests.cs` is the automation
  snapshot model marker shell.
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
  telemetry provider ownership, rolling command-group split, and supported 4K X
  product-revision checks.
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
  CaptureService recording lifecycle and rollback file-ownership assertions.
- `tests/Sussudio.Tests/RecordingQueue.LibAvSink.Tests.cs` owns LibAv recording
  sink output validation, try-enqueue, queue-cleanup, drain-loop, encoding-loop,
  and lifecycle ownership assertions.
- `tests/Sussudio.Tests/RecordingQueue.Wasapi.Tests.cs` owns WASAPI capture-loop, hot-write,
  conversion, diagnostics, COM contract, and bounded stop assertions.
- `tests/Sussudio.Tests/RecordingQueue.CaptureFanout.Tests.cs` owns
  UnifiedVideoCapture sink fan-out and CaptureService Flashback backend
  aggregate ownership assertions.
- `tests/Sussudio.Tests/CaptureService.FlashbackOrchestrationSource.Tests.cs`
  owns the source family helper for Flashback backend orchestration partials
  and recording finalization partials plus the focused-partial ownership
  contracts.
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
- `tests/Sussudio.Tests/CaptureService.Failures.Tests.cs` owns capture fatal
  cleanup and faulted-session state assertions.
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
- `tests/Sussudio.Tests/D3D11PreviewRenderer.Tests.cs` is the preview-renderer
  test family marker shell.
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
- `tests/Sussudio.Tests/D3D11PreviewRenderer.SourceOwnership.Tests.cs` is the
  renderer source-ownership marker shell.
  `D3D11PreviewRenderer.SourceOwnership.ContractsAndMetrics.Tests.cs` owns
  configuration, native interop, frame type/ownership, DXGI frame-stat,
  slow-frame, and metric-tracking assertions.
  `D3D11PreviewRenderer.SourceOwnership.RenderPipeline.Tests.cs` owns panel
  binding, device initialization, input-resource, upload, frame-latency,
  viewport, shader-rendering, and shader-source assertions.
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
- `tests/Sussudio.Tests/LibAvEncoder.Tests.cs` is the marker shell for
  LibAvEncoder harness checks; `LibAvEncoder.*.Tests.cs` and
  `LibAvEncoder.Helpers.cs` own codec policy, frame-size, diagnostics,
  HDR metadata, output lifecycle, source-ownership, and shared source-reading
  helpers.
- `tests/Sussudio.Tests/AutomationCommandDispatcher.Tests.cs` owns the live
  dispatcher source-family reader; `AutomationCommandDispatcher.*.Tests.cs`
  and `AutomationCommandDispatcher.Helpers.cs` own authorization, manifest,
  Flashback failure response, Flashback command placement, verification command
  placement, command-kind handling, and helper coverage.
- `tests/Sussudio.Tests/AutomationCommandDispatcher.Payload.Tests.cs` owns
  dispatcher JSON payload extraction helper coverage.
- `tests/Sussudio.Tests/AutomationCommandDispatcher.Readiness.Tests.cs` owns
  dispatcher readiness gating, window close, preview health, and UI automation
  readiness-independent coverage.
- `tests/Sussudio.Tests/AutomationCommandDispatcher.ReadyIndependent.Tests.cs`
  owns ready-independent no-hardware command coverage and harness payload/fake
  device support.
- `tests/Sussudio.Tests/AutomationToolContracts.Tests.cs` owns shared
  reflection helpers for automation tool contract tests.
- `tests/Sussudio.Tests/AutomationToolContracts.CommandKinds.Tests.cs` owns
  automation command enum numeric IDs, window action enum membership, and the
  shared golden automation command table used by protocol/MCP tests.
- `tests/Sussudio.Tests/AutomationToolContracts.Protocol.Tests.cs` owns
  automation pipe protocol, pipe-connect failure, and response-state contract
  tests.
- `tests/Sussudio.Tests/AutomationToolContracts.Catalog.Tests.cs` owns
  automation command catalog, manifest, path policy, and manifest
  serialization contract tests.
- `tests/Sussudio.Tests/AutomationToolContracts.Reliability.Tests.cs` owns the
  reliability-gates script contract test.
- `tests/Sussudio.Tests/ArchitectureDocs.Tests.cs` is the architecture-doc test
  family marker shell. `ArchitectureDocs.AgentMapReferences.Tests.cs` owns
  AGENT_MAP file/folder reference drift checks;
  `ArchitectureDocs.AgentMapOwnershipPaths.Tests.cs` owns test-owner code-span
  coverage; `ArchitectureDocs.AgentMapAutomation.Tests.cs` owns README
  automation consumer checklist coverage; `ArchitectureDocs.AgentMapPresentation.Tests.cs`
  owns UI presentation ownership code-span coverage;
  `ArchitectureDocs.AgentMapToolAutomation.Tests.cs` owns exact-path coverage
  for shared tool automation partial families; and
  `ArchitectureDocs.AgentMapHelpers.cs` owns the shared AGENT_MAP token,
  consumer, and ownership-file discovery helpers.
- `tests/Sussudio.Tests/AutomationToolContracts.SnapshotFormatter*.Tests.cs`
  owns the shared/ssctl snapshot formatter contract family: typed accessors,
  core section formatting, section-order, and Flashback opt-in smoke checks
  stay in `.Tests.cs`, response accessor checks live in
  `.ResponseAccessors.Tests.cs`, Flashback output rendering lives in
  `.Flashback.Tests.cs`, Preview D3D output rendering lives in
  `.PreviewD3D.Tests.cs`, shared formatter source ownership lives in
  `.Ownership.Tests.cs`, shared-vs-ssctl field parity lives in
  `.Parity.Tests.cs`, and MJPEG timing rendering lives in `.MjpegTiming.Tests.cs`.
- `tests/Sussudio.Tests/Formatters.Tests.cs` owns ssctl formatted snapshot
  output smoke checks. `tests/Sussudio.Tests/Formatters.SnapshotOwnership.Tests.cs`
  owns ssctl formatter source ownership assertions, while
  `tests/Sussudio.Tests/Formatters.Timeline.Tests.cs` owns timeline table and
  summary output checks.
- `tests/Sussudio.Tests/CommandHandlers.Tests.cs` is the ssctl command-handler
  test family marker shell. `CommandHandlers.Helpers.cs` owns source-family
  reader, routing-capture helpers, and `AssertSsctlCommandRequest`, which routes
  captured ssctl `request.command` checks through the shared golden command table
  instead of per-test numeric IDs. Pipe-captured routing coverage is split by
  command group across `CommandHandlers.Routing.Device.Tests.cs`,
  `CommandHandlers.Routing.CaptureControls.Tests.cs`,
  `CommandHandlers.Routing.Recordings.Tests.cs`,
  `CommandHandlers.Routing.Flashback.Tests.cs`,
  `CommandHandlers.Routing.Window.Tests.cs`,
  `CommandHandlers.Routing.Manifest.Tests.cs`,
  `CommandHandlers.Routing.Observability.Tests.cs`,
  `CommandHandlers.Routing.AutomationFlow.Tests.cs`,
  `CommandHandlers.Routing.UiVisibility.Tests.cs`, and
  `CommandHandlers.Routing.Verification.Tests.cs`.
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
  topic sequencing and shared catalog registration helpers. Keep `Program.cs`
  as the runner, not the assertion registry.
- `tests/Sussudio.Tests/HarnessCheckCatalog.CoreRuntime.cs` owns runtime,
  telemetry, recording verifier, LibAv encoder, and basic app contract check
  registration.
- `tests/Sussudio.Tests/HarnessCheckCatalog.AutomationDiagnostics.cs`
  coordinates automation-diagnostics check registration; focused
  `HarnessCheckCatalog.AutomationDiagnostics.*.cs` partials own app shell,
  MainWindow surface, dispatcher, pipe/auth, ViewModel/Flashback UI,
  capture/Flashback routing, snapshot projection, and protocol registration
  groups.
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
- `tests/Sussudio.Tests/HarnessCheckCatalog.PresentationPreview.cs` coordinates
  presentation-preview check registration; the focused
  `HarnessCheckCatalog.PresentationPreview.*.cs` partials own capture/root
  policy, MainViewModel, MainWindow, stats, D3D renderer, and preview pacing
  registration groups.
- `tests/Sussudio.Tests/HarnessCheckCatalog.McpDiagnosticsPipeline.cs` owns MCP,
  diagnostic-session, unified-video, MJPEG, D3D pending-frame, and recording
  queue check registration.
- `tests/Sussudio.Tests/HarnessCheckCatalog.RecordingModels.cs` coordinates
  recording/model check registration; focused
  `HarnessCheckCatalog.RecordingModels.*.cs` partials own LibAv sink,
  capture runtime, recording contracts/artifacts/stats, capture settings,
  Flashback buffer, recording context, device/media models, automation
  contracts, runtime paths, source-signal telemetry, and HDR policy
  registration groups.
- `tests/Sussudio.Tests/HarnessCheckCatalog.Flashback.cs` coordinates Flashback
  check registration; `HarnessCheckCatalog.Flashback.*.cs` partials own model,
  playback, decoder, encoder sink, and exporter registration groups.
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
- `tests/Sussudio.Tests/CaptureSessionCoordinator.Tests.cs` is the capture
  session coordinator marker shell. Focused coordinator coverage lives in
  `CaptureSessionCoordinator.Api`, `CaptureSessionCoordinator.Contracts`,
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
  MJPEG preview jitter adaptive deadline policy source-ownership assertion.
- `tests/Sussudio.Tests/PooledVideoFrame.MjpegJitterQueue.Tests.cs` owns
  MJPEG preview jitter queue/drop/reprime behavior tests.
- `tests/Sussudio.Tests/PooledVideoFrame.QueuedLeaseRelease.Tests.cs` owns
  D3D pending-frame and recording/Flashback queued lease return tests.
- `tests/Sussudio.Tests/McpToolSurface.Tests.cs` owns MCP surface compatibility
  checks that span raw app state, capture options, capture settings, and UI
  settings tools.
- `tests/Sussudio.Tests/McpToolSurface.CommandRouting.Tests.cs` is now the
  MCP command-routing test family marker shell. Keep route/formatter assertions
  in the focused `CommandRouting.Capture`, `CommandRouting.Host`,
  `CommandRouting.Recording`, `CommandRouting.Formatting`,
  `CommandRouting.Device`, `CommandRouting.Pipeline`, `CommandRouting.Ui`, and
  `CommandRouting.Verification` owner files. Captured command-ID assertions use
  the shared `AssertAutomationCommandId` helper so the golden command table is
  the only test-owned numeric ID list.
- `tests/Sussudio.Tests/McpToolSurface.DiagnosticSession.Tests.cs` is the
  diagnostic-session MCP surface index shell.
- `tests/Sussudio.Tests/McpToolSurface.DiagnosticSession.Tool.Tests.cs` owns
  MCP `run_diagnostic_session` success/failure artifact contract tests.
- `tests/Sussudio.Tests/McpToolSurface.DiagnosticSession.Ownership.Tests.cs`
  owns diagnostic-session helper ownership assertions for core runner,
  scenario, cleanup, sampling, and metric collaborators.
- `tests/Sussudio.Tests/McpToolSurface.DiagnosticSession.InfrastructureOwnership.Tests.cs`
  owns diagnostic-session infrastructure ownership assertions for initial
  snapshot capture, pipe retry, command channel, run state, and output lock.
- `tests/Sussudio.Tests/McpToolSurface.DiagnosticSession.ResultOwnership.Tests.cs`
  owns diagnostic-session model ownership assertions, with builder, formatter,
  summary-writer/artifact, JSON/shared-text, and infrastructure assertions split
  into the adjacent `ResultOwnership.*.Tests.cs` files.
- `tests/Sussudio.Tests/McpToolSurface.DiagnosticSession.Flashback.Tests.cs` is
  the diagnostic-session Flashback ownership marker shell.
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
- `tests/Sussudio.Tests/McpToolSurface.DiagnosticSession.Runner.Tests.cs` is
  the diagnostic-session runner behavior marker shell. Focused runner behavior
  coverage lives beside it in `Runner.Artifacts`, `Runner.HealthPolicy`,
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
- `tests/Sussudio.Tests/Flashback.Playback.CommandQueue.Tests.cs` is the
  Flashback playback command queue marker shell. Capacity/drop-oldest,
  scrub-coalescing, and seek-slot barrier coverage lives in focused
  `CommandQueue.Capacity`, `CommandQueue.ScrubCoalescing`, and
  `CommandQueue.SeekSlots` owner files.
- `tests/Sussudio.Tests/Flashback.Playback.Cadence.Tests.cs` owns Flashback
  playback cadence, submit-failure, fMP4 reopen, and metrics reset tests.
- `tests/Sussudio.Tests/Flashback.Decoder.Tests.cs` owns Flashback decoder
  audio, timestamp, stream-bound, validation, lifetime, and callback tests.
- `tests/Sussudio.Tests/Flashback.Support.Tests.cs` owns cross-cutting Flashback
  support/logging contract tests.
- `Sussudio/Controllers/StatsHardwareRowsController.cs` owns MJPEG/NVML row
  refresh, availability, and row-pool delegation;
  `Sussudio/ViewModels/StatsPresentationBuilder.HardwareRows.cs` owns pure
  decode/GPU row text projection over presentation inputs, and
  `StatsDockRefreshController` owns when decode/GPU rows refresh.
- `Sussudio/Controllers/FlashbackTimelineController.cs` owns Flashback
  timeline visibility, lockout, toggle synchronization, and show/hide
  animation state. `MainWindow.FlashbackTimeline.cs` is the XAML-facing
  adapter; command semantics live in `FlashbackCommandController`.
- `Sussudio/Controllers/FlashbackScrubInteractionController.cs` owns active
  Flashback pointer-scrub state, scrub throttling, release/cancel/capture-lost
  cleanup, fullscreen scrub termination, lockout clearing, and scrub visual
  updates. `MainWindow.FlashbackScrub.cs` is the XAML-facing adapter.
  `Sussudio/Controllers/FlashbackTimelineGeometry.cs` owns pure timeline
  fraction/duration math used by scrub and playhead presentation.
- `Sussudio/MainWindow.FlashbackPlayhead.cs` owns Flashback current-time-
  indicator compositor visual setup, snap placement, and magnetic pointer-scrub
  movement. `Sussudio/MainWindow.FlashbackPlayhead.CtiMotion.cs` owns
  long-horizon linear playhead extrapolation and CTI anchor timing.
- `Sussudio/Controllers/FlashbackMarkerPresentationController.cs` owns
  Flashback marker placement, selection-region layout, and compact duration
  text formatting. `MainWindow.FlashbackMarkers.cs` is the XAML-facing adapter.
- `Sussudio/Controllers/FlashbackPlaybackPresentationController.cs` owns
  Flashback play/pause glyph policy, Go Live enabled state, buffer-duration
  text, and floating playhead label text. `MainWindow.FlashbackPlaybackPresentation.cs`
  wires the XAML controls; `MainWindow.Flashback.cs` owns playback-polling
  start/stop and CTI re-anchor calls.
- `Sussudio/Controllers/FlashbackCommandController.cs` owns Flashback command
  semantics for in/out points, clear, play/pause, Go Live, export, save-last-5m,
  enable-toggle rollback, and apply/restart. `MainWindow.FlashbackCommands.cs`
  preserves the XAML event-handler surface as a thin adapter.
- `Sussudio/Controllers/FlashbackExportProgressPresentationController.cs` owns
  Flashback export progress-bar value, visibility, and reset-on-complete
  semantics. `MainWindow.FlashbackExportProgressPresentation.cs` is the
  XAML-facing adapter.
- `Sussudio/Controllers/FlashbackSettingsBindingController.cs` owns Flashback
  settings-control initialization, GPU decode toggle binding/sync, buffer
  duration combo selection/sync, and buffer-duration change logging.
  `MainWindow.FlashbackSettingsBindings.cs` is the XAML-facing adapter; enable
  toggle rollback and apply/restart command behavior live in
  `FlashbackCommandController`.
- `Sussudio/Controllers/FlashbackPollingController.cs` owns Flashback status
  and playback-position polling timers. `MainWindow.FlashbackPolling.cs` is the
  XAML-facing adapter; CTI anchor timing stays with playhead motion.
- `Sussudio/Controllers/SettingsShelfController.cs` owns settings shelf
  visibility, the animation gate, and show/hide storyboard construction.
  `MainWindow.SettingsShelf.cs` is the XAML-facing adapter.
- `Sussudio/Controllers/SplashLoadingPhraseCatalog.cs` owns splash phrase
  file lookup, Markdown-ish parsing, cached defaults, and exception fallback.
  `Sussudio/Controllers/SplashLoadingPhraseController.cs` owns timer pacing and
  two-line text animation. `MainWindow.SplashLoading.cs` is the XAML-facing
  adapter.
- `Sussudio/Controllers/LaunchEntranceAnimationController.cs` owns the splash-
  to-shell launch choreography, initial hidden/scaled shell state, and one-shot
  entrance state. `MainWindow.LaunchEntrance.cs` is the XAML-facing adapter;
  the delayed control-bar shadow fade routes through
  `CompositionShadowFadeAnimator`.
- `Sussudio/Controllers/ControlBarAnimationController.cs` owns the control-bar
  button list used by launch entrance animation plus hover/press/release scale
  behavior. `MainWindow.ControlBarAnimations.cs` is the XAML-facing adapter.
- `Sussudio/Controllers/ShellElevationController.cs` owns static shell
  ThemeShadow and translation setup for the control bar and record button.
  `MainWindow.ShellElevation.cs` is the XAML-facing adapter.
- `Sussudio/Controllers/PreviewTransitionAnimationController.cs` owns preview
  shell/content fade and scale transitions, unavailable-placeholder fades, and
  startup/unavailable presentation prep. `MainWindow.PreviewTransitions.cs` is
  the XAML-facing adapter; shared video-shadow fades route through
  `CompositionShadowFadeAnimator`.
- `Sussudio/Controllers/PreviewButtonPresentationController.cs` owns preview
  button glyph and tooltip presentation for Start Preview and Stop Preview.
  `MainWindow.PreviewButtonPresentation.cs` is the XAML-facing adapter; keep
  `PreviewButton_Click` command behavior outside this controller.
- `Sussudio/Controllers/RecordButtonAnimationController.cs` owns the recording
  button circle/pill width morph used by recording state changes.
  `MainWindow.RecordButtonAnimations.cs` is the XAML-facing adapter.
- `Sussudio/Controllers/RecordingStatePresentationController.cs` owns
  recording-state UI projection: record-button content, recording glow,
  recording pulse, FFmpeg/transition button enablement, and recording-time
  control lockouts. `MainWindow.PropertyChangedRecording.cs` is the
  XAML-facing adapter.
- `Sussudio/Controllers/RecordingButtonActionController.cs` owns the recording
  button command workflow and preview-state logging after a start.
  `MainWindow.RecordingActions.cs` is the XAML-facing adapter.
- `Sussudio/Controllers/LiveSignalInfoController.cs` owns live-signal pill
  text application, visibility state, show/hide debounce timers, and the small
  scale/fade animation. `MainWindow.LiveSignalInfo.cs` is the XAML-facing
  adapter. `Sussudio/ViewModels/LiveSignalTextPresentationBuilder.cs` owns the
  view-model live-signal label formatting and pixel-format/codec suffix policy.
- `Sussudio/Controllers/PreviewAudioFadeController.cs` owns preview-volume
  fade-in/fade-out state, saved target volume, storyboard lifetime, and volume
  save suppression. `MainWindow.PreviewAudioFade.cs` is the XAML-facing adapter.
- `Sussudio/MainWindow.PreviewStartup.cs` owns preview startup state and
  first-visual confirmation. `Sussudio/MainWindow.PreviewStartupWatchdog.cs`
  owns watchdog/telemetry timers, timeout configuration, timeout recovery, and
  failure-stop scheduling.
  `MainWindow.PreviewStartupSignals.cs` owns readiness-signal
  collection and playback-progress diagnostics. `Sussudio/Controllers/PreviewStartupReadinessSignalController.cs`
  owns readiness-signal required/received state, missing-signal calculation,
  playback-advance threshold checks, and readiness result snapshots.
  `Sussudio/Controllers/PreviewStartupSignalFormatter.cs` owns missing-signal
  and signal-list string formatting.
  `Sussudio/Controllers/PreviewStartupFailureTextFormatter.cs` owns preview
  startup timeout reason, timeout status, and failure-stop status text.
  `MainWindow.PropertyChangedPreview.cs` owns preview-specific ViewModel events
  and the preview property-change router for preview start/stop/reinit state.
  Keep preview startup fields out of the composition root.
- `Sussudio/Controllers/PreviewFadeInController.cs` owns delayed preview
  reveal after first visual: rendered-frame threshold, fade-in timer, renderer
  replacement fallback, and preview-audio fade start ordering.
  `MainWindow.PreviewFadeIn.cs` is the XAML-facing adapter. Keep
  timeout/watchdog recovery in `MainWindow.PreviewStartupWatchdog.cs`.
- `Sussudio/Controllers/PreviewStartupOverlayController.cs` owns preview-
  startup loading overlay presentation while the app waits for visual
  confirmation: ProgressRing activation, fade-in/fade-out routing, and the
  reinit-collapse opacity reset. `MainWindow.PreviewStartupOverlay.cs` is the
  XAML-facing adapter.
- `Sussudio/Controllers/PreviewResizeTelemetryController.cs` owns top-level
  preview resize log throttling and reset state. `MainWindow.WindowSizing.cs`
  is the XAML-facing adapter for `SizeChanged`; preview surface sizing remains
  with `MainWindow.PreviewSurface.cs`.
- `Sussudio/MainWindow.PropertyChangedRecording.cs` owns only the
  recording-specific property-change router and adapter surface, delegating
  record-button, glow, pulse, and recording-time lockout projection to
  `RecordingStatePresentationController`.
- `Sussudio/MainWindow.PropertyChangedOutput.cs` owns the output-path
  property-change router and delegates display updates to
  `OutputPathDisplayController`.
- `Sussudio/MainWindow.PropertyChangedCaptureOptions.cs` owns capture-option
  and source-signal property-change routing for HDR toggles, telemetry
  tooltips, source overlay updates, bitrate visibility, and show-all-options
  synchronization.
- `Sussudio/MainWindow.PropertyChangedShell.cs` owns shell visibility
  property-change routing for stats and settings chrome.
- `Sussudio/MainWindow.PropertyChangedLiveSignal.cs` owns the live source-signal
  property-change router for the live signal pill.
- `Sussudio/MainWindow.PropertyChangedFlashback.cs` owns Flashback-specific
  property-change routing for timeline lockout, markers, playhead updates,
  export progress, and settings-control synchronization.
- `Sussudio/Controllers/AudioControlPresentationController.cs` owns audio and
  microphone property-change projections: audio toggles, monitoring meter
  state, preview volume slider sync, microphone enablement, and microphone
  volume sync. `Sussudio/MainWindow.PropertyChangedAudio.cs` owns the audio
  property-change router and XAML-facing adapter.
- `Sussudio/Controllers/MicrophoneControlsController.cs` owns microphone volume
  slider synchronization, save triggers, shelf enablement, and mic-meter row
  animation state. `MainWindow.MicrophoneControls.cs` is the XAML-facing adapter.
- `Sussudio/Controllers/ResponsiveShellLayoutController.cs` owns applying
  responsive control-bar label visibility and capture-settings grid placement
  to XAML elements. `Sussudio/Controllers/ResponsiveShellLayoutPolicy.cs` owns
  the breakpoint and narrow/wide placement policy. `MainWindow.ResponsiveShellLayout.cs`
  is the XAML-facing adapter.
- `Sussudio/Controllers/CaptureSelectionBindingController.cs` owns
  capture/audio/microphone/encoder selection synchronization, selected-device
  property-change reconciliation, recording string selection handlers, and
  pending-device apply state.
  `Sussudio/Controllers/CaptureSelectionBindingController.Context.cs`
  owns the XAML control dependency bag,
  `Sussudio/Controllers/CaptureSelectionBindingController.SelectionSync.cs` owns
  collection-change debounce/queued sync plus available-option property-change
  rebinding, and
  `Sussudio/Controllers/CaptureSelectionBindingController.DeviceAudio.cs` owns
  device-audio mode/gain control projection. `MainWindow.CaptureSelectionBindings.cs`
  is the XAML-facing adapter and owns the capture-selection `PropertyChanged`
  router.
- `Sussudio/Controllers/AudioControlBindingController.cs` owns audio/microphone
  initial control projection and event hookup: record/preview toggles, preview
  volume priming, custom audio/microphone selection, device-audio mode/gain event
  hookup, and meter resize hooks. Device-audio mode/gain control projection stays
  in `Sussudio/Controllers/CaptureSelectionBindingController.DeviceAudio.cs`.
  `Sussudio/MainWindow.AudioBindings.cs` is its XAML-facing adapter.
- `Sussudio/Controllers/CaptureDeviceActionController.cs` owns the capture-
  device refresh/apply button workflows and preserves the explicit apply/reinit
  path. `MainWindow.CaptureDeviceActions.cs` is the XAML-facing adapter.
- `Sussudio/Controllers/CaptureOptionPresentationController.cs` owns
  presentation-only rules for capture option affordances: HDR readiness hints,
  FPS telemetry tooltips, MJPEG decoder count selection/visibility, bitrate
  mode visibility, and audio clipping visibility.
  `Sussudio/Controllers/CaptureOptionTooltipFormatter.cs` owns pure HDR hint
  and FPS telemetry tooltip text policy.
  `MainWindow.CaptureOptionPresentation.cs` is the XAML-facing adapter.
- `Sussudio/Controllers/CaptureOptionBindingController.cs` owns the capture
  option binding controller shell and context lifetime.
  `Sussudio/Controllers/CaptureOptionBindingController.Context.cs` owns the
  XAML/view-model adapter context, `CaptureOptionBindingController.Initialization.cs`
  owns video-format collection binding plus initial capture/recording selection
  projection, and `CaptureOptionBindingController.SelectionHandlers.cs` owns
  resolution/frame-rate selection handlers plus video-format/custom-bitrate/HDR
  event bindings during `SetupBindings()` and custom-bitrate property-change
  value projection. Recording format, quality, preset, and split-encode string
  selection handlers live with
  `CaptureSelectionBindingController`.
  `MainWindow.CaptureOptionBindings.cs` and
  `MainWindow.RecordingOptionBindings.cs` are XAML-facing adapters.
- `Sussudio/Controllers/OutputPathDisplayController.cs` owns recording output-
  path textbox, tooltip, and resize-event updates.
  `Sussudio/Controllers/OutputPathDisplayTextFormatter.cs` owns pure output-
  path truncation text policy. `MainWindow.OutputPathDisplay.cs` is the
  XAML-facing adapter used by binding setup and property changes.
- `Sussudio/Controllers/OutputPathActionController.cs` owns recording output-
  path browse/open-recordings button workflows. `MainWindow.OutputPathActions.cs`
  is the XAML-facing adapter.
- `Sussudio/ViewModels/MainViewModel.*.cs` for root presentation state and
  automation-facing compatibility. `MainViewModel.cs` owns compatibility-facade
  construction, dependency assignment, event subscription, and small bridge
  methods. `MainViewModel.State.cs` owns shared shell/runtime flags and
  coordination gates; `MainViewModel.CaptureState.cs` owns capture-selection,
  source telemetry, and HDR state; `MainViewModel.AudioState.cs` owns audio,
  microphone, and device-audio state; `MainViewModel.FlashbackState.cs` owns
  Flashback timeline/export state. `MainViewModel.AudioMeters.cs` owns live
  audio/microphone meter callback state; keep callback-thread meter targets
  out of the root facade file. `MainViewModel.AudioRampTrace.cs` owns the audio
  ramp diagnostic ring buffer and sampler. `PreviewAudioVolumeTransitionController`
  owns preview-volume save suppression/override state plus the preview-audio
  ramp constants, easing, priming, restoring, and property-to-session volume
  forwarding. Keep preview monitoring call sites, audio input retargeting, and
  coordinator sequencing in `MainViewModel.AudioMonitoring.cs`.
  `MainViewModel.MicrophoneVolume.cs` owns microphone endpoint volume
  synchronization and persistence.
  `MainViewModel.AudioControls.cs` owns device-native audio mode/gain management
  through the supported native-XU switch/gain command surface, not the legacy
  AT input-source fallback path.
  `Sussudio/ViewModels/DeviceAudioGainMapper.cs` owns the pure percent-to-XU-
  byte analog gain curve used by device-native gain application.
  `MainViewModel.AudioControlCancellation.cs` owns cancellation cleanup for
  pending device-audio refresh, mode, XU gain, and flash-persist work.
  `MainViewModel.AudioPropertyChanges.cs` owns audio capture/preview property
  handlers. `MainViewModel.AudioInputPropertyChanges.cs` owns custom audio
  input property handlers. `MainViewModel.MicrophonePropertyChanges.cs` owns
  microphone monitor and selected-microphone property handlers.
  `MainViewModel.DeviceAudioPropertyChanges.cs` owns device-native audio mode
  and analog gain property handlers.
  `MainViewModel.Dispatching.cs` owns shared
  dispatcher enqueue/invoke helpers and preview event fan-out for the partial
  family. `MainViewModel.Runtime.cs` owns timer refreshes, recording bitrate
  display, capture status/error fan-out, and resume cleanup callbacks.
  `MainViewModel.LiveSignalPresentation.cs` owns live-capture info projection
  from `CaptureRuntimeSnapshot`, including audio-preview activity and
  live-resolution/frame-rate/pixel-format assignment, and delegates label
  formatting to `Sussudio/ViewModels/LiveSignalTextPresentationBuilder.cs`.
  `MainViewModel.CaptureSettings.cs` owns capture settings projection from UI
  selection and observed runtime/source state.
  `MainViewModel.Capture.cs` owns device initialization, preview start/stop,
  and selected-device apply. `MainViewModel.PreviewReinitialization.cs` owns
  debounced preview reinitialization, Flashback-cycle wait-before-reinit,
  renderer-stop handoff, teardown restart, and reinit gate release.
  `MainViewModel.OutputPathSelection.cs` owns output folder picker and path assignment.
  `MainViewModel.RecordingLifecycle.cs` owns recording toggle serialization,
  graceful stop, emergency stop, and start/stop recording transitions.
  `MainViewModel.RecordingState.cs` owns recording option selections, output
  path, counters, and transition flags.
  `MainViewModel.Disposal.cs` owns bounded teardown, event unsubscription, and
  export-cancellation cleanup.
  `MainViewModel.AutomationSnapshots.cs` owns automation-facing capture runtime,
  health, recording, and probe snapshot projection.
  `MainViewModel.ViewModelRuntimeSnapshot.cs` owns automation-facing view-model runtime snapshot projection.
  `MainViewModel.AutomationOptionsSnapshot.cs` owns automation-facing options
  and selected-control-state projection for CLI/MCP clients.
  `MainViewModel.FlashbackPlayback.cs` owns
  Flashback playback commands, marker commands, and buffer/bitrate status
  projection. `MainViewModel.FlashbackExport.cs` owns Flashback UI/automation
  export flow, progress/cancellation state, and segment projection.
  `MainViewModel.FrameRateOptions.cs` owns frame-rate option rebuilding,
  observable collection mutation, and automatic frame-rate selection.
  `MainViewModel.ModeSelectionState.cs` owns shared frame-rate selection reset,
  resolved automatic frame-rate application, disabled frame-rate reason
  projection, and capture-mode reset flags.
  `MainViewModel.FrameRateSourceFilterPolicy.cs` owns source-rate filtering and
  `ShowAllCaptureOptions` unlock policy. `MainViewModel.CaptureOptionVisibility.cs`
  owns `ShowAllCaptureOptions` change handling and deferred rebuild behavior.
  `MainViewModel.FrameRateTiming.cs` owns shared frame-rate timing family,
  rational parsing, source-rate fallback, and preferred-format ranking helpers
  used by frame-rate, resolution, capture-settings, and automation projections.
  `MainViewModel.FormatSelection.cs` owns pixel-format option building,
  recording-format policy application to observable state, HDR toggle side
  effects, and selected capture-format selection policy.
  `Sussudio/ViewModels/RecordingFormatSelectionPolicy.cs` owns pure recording
  codec filtering and selected-codec fallback policy shared by UI and automation.
  Video device enumeration and selected-device capability rebuilds stay in
  `MainViewModel.DeviceManagement.cs`; startup audio-list selection,
  watcher-driven audio endpoint refresh, and capture-card endpoint filtering
  live in `MainViewModel.AudioDeviceDiscovery.cs`.
  `MainViewModel.DeviceFormatProbes.cs` owns late device-format probe
  reconciliation, capability refresh after background probes, UI-side
  restoration, logging, and reinitialize dispatch. `Sussudio/ViewModels/DeviceFormatProbeRetargetPolicy.cs`
  owns the pure late-probe decision policy for HDR retarget, SDR NV12 retarget,
  MJPG HFR preservation, session mismatch, and active-capture restore.
  `MainViewModel.AutoResolutionOptions.cs` owns automatic resolution ranking,
  source-aware auto-selection, and auto-resolved dimension/frame-rate state.
  `Sussudio/ViewModels/CaptureResolutionSelectionPolicy.cs` owns pure
  source-aware, HDR-aware, and SDR fallback resolution selection decisions.
  `MainViewModel.ResolutionSelectionPolicy.cs` delegates state-backed
  capability queries to that helper. `MainViewModel.ResolutionOptions.cs` owns
  resolution dropdown mutation and effective resolution display/query helpers.
  `MainViewModel.Telemetry.cs` owns source telemetry projection and
  source-aware auto-retargeting hints. `Sussudio/ViewModels/SourceTelemetryPresentationBuilder.cs`
  owns source telemetry summary, telemetry age, and target-summary display text.
  `MainViewModel.Settings.cs` owns settings load/save and simple
  persistence reactions. `MainViewModel.FlashbackSettings.cs` owns active
  Flashback reactions to recording-format, encoder, buffer, and GPU-decode
  setting changes. `MainViewModel.AutomationUi.cs` owns UI-only automation mutators
  for stats/settings visibility, frame-time overlay display, Flashback timeline
  visibility, and show-all capture options. `MainViewModel.AutomationAudio.cs`
  owns automation command entry points for audio enablement, audio-preview
  enablement, preview-volume clamp/persist, device-native mode/gain
  application, and microphone enablement with recording-time refusal and
  idempotent handling.
  `MainViewModel.AutomationDeviceSelection.cs` owns automation device refresh,
  capture-device selection, audio-input selection, and custom audio-input
  enablement.
  `MainViewModel.AutomationCaptureMode.cs` owns automation mutators for
  resolution, frame rate, video format, MJPEG decoder count, and the shared
  reinitialization gate used after active capture-mode changes.
  `MainViewModel.AutomationRecordingSettings.cs` owns recording format,
  encoder preset/quality/split-mode/custom-bitrate, and output-path automation
  mutators. `MainViewModel.RecordingCapabilityRefresh.cs` owns startup FFmpeg
  capability probes for recording formats and split-encode modes.
  Remaining automation command mutation code for Flashback enable/restart, HDR,
  preview, and recording desired state stays in `MainViewModel.Automation.cs`.

Refactor direction:

- Keep `MainWindow.xaml.cs` as a shell/composition root over time.
- Keep `MainWindow.*` partials thin as XAML adapters over named controllers.
  Preview startup, stats projection, and Flashback playback/export presentation
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
- `tools/Common/AutomationPipeClient.cs` is the shared automation pipe client
  marker shell used by ssctl, MCP, diagnostic sessions, and smoke tools.
- `tools/Common/AutomationPipeClient.Transport.cs` owns named-pipe connect
  orchestration, request/response framing, and response timeout.
- `tools/Common/AutomationPipeClient.ConnectErrors.cs` owns pipe connect
  failure classification and exact CLI/MCP diagnostic error codes.
- `tools/Common/AutomationPipeClient.Commands.cs` owns command envelope sending
  and `not_ready` retry behavior.
- `tools/Common/AutomationPipeClient.ResponseState.cs` owns tolerant response
  state parsing.
- `tools/Common/AutomationPipeClient.Models.cs` owns pipe command result and
  exception types.
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
  screenshot/frame capture, and `set` capture/audio/output mutations.
- `tools/ssctl/CommandHandlers.Device.cs` owns device refresh/list/select,
  audio-input selection, and custom-audio enablement.
- `tools/ssctl/CommandHandlers.Window.cs` owns window close arming, window
  state/geometry actions, fullscreen toggles, and snap commands.
- `tools/ssctl/CommandHandlers.Recordings.cs` owns the recordings-folder CLI
  command.
- `tools/ssctl/CommandHandlers.AutomationFlow.cs` owns wait/assert/probe
  scripting flow commands.
- `tools/ssctl/CommandHandlers.UiVisibility.cs` owns stats, settings, and
  frame-time visibility commands.
- `tools/ssctl/CommandHandlers.Verification.cs` owns recording/file
  verification commands.
- `tools/ssctl/CommandHandlers.Flashback.cs` owns Flashback enablement,
  playback, scrub, marker, segment, and restart commands.
  `tools/ssctl/CommandHandlers.Flashback.Export.cs` owns Flashback export CLI
  flag parsing, output-path defaulting, parent-directory creation, and
  `FlashbackExport` payload shaping.
- `tools/NativeXuAudioProbe/Program.cs` owns probe command routing and command
  workflows; `Program.Models.cs` owns probe experiment/readback DTOs and
  result-diff records; `Program.Commands.cs` owns Native XU command IDs;
  `Program.AtCommands.cs` owns direct AT read/write/input subcommands;
  `Program.DefaultExperiment.cs` owns the default baseline/experiment/restore
  runner; `Program.Formatting.cs` owns shared byte formatting;
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
  text, and `tools/ssctl/SsctlHelpWriter.Catalog.cs` owns catalog-backed CLI
  help lines.
- `tools/ssctl/CommandHandlers.Context.cs` owns the per-invocation command
  context wrapper.
- `tools/ssctl/CommandHandlers.Flags.cs` owns flag consumption and optional
  flag value parsing. `CommandHandlers.Arguments.cs` owns usage validation,
  required words, and argument joining. `CommandHandlers.Json.cs` owns
  command-handler JSON detection/pretty-printing. `CommandHandlers.Values.cs`
  owns primitive parsing, Flashback numeric validation, on/off and show/hide
  parsing, recording format normalization, snap action mapping, and assertion
  value parsing.
- `tools/ssctl/CommandHandlers.Transport.cs` owns shared command sending and
  response exit-code shaping.
- `tools/ssctl/Formatters.cs` is the console projection facade only.
- `tools/ssctl/Formatters.Snapshot.cs` owns app snapshot orchestration and
  section ordering only.
- `tools/ssctl/Formatters.Snapshot.State.cs` owns the Sussudio state and
  capture-command summary.
- `tools/ssctl/Formatters.Snapshot.CaptureSettings.cs` owns capture settings
  text and friendly/exact frame-rate summary formatting.
- `tools/ssctl/Formatters.Snapshot.Audio.cs` owns audio snapshot text.
- `tools/ssctl/Formatters.Snapshot.VideoPipeline.cs` owns video ingest,
  recording queue, encoder failure, GPU/CUDA queue, freshness, and video
  diagnostic snapshot text.
- `tools/ssctl/Formatters.Snapshot.Recording.cs` owns recording/output,
  backend, integrity, audio-integrity, and last-output snapshot text.
- `tools/ssctl/Formatters.Snapshot.DiagnosticLanes.cs` owns diagnostic health,
  summary, evidence, and frame-lane snapshot text.
- `tools/ssctl/Formatters.Snapshot.Performance.cs` owns legacy performance
  score, summary, and pipeline-latency snapshot text.
- `tools/ssctl/Formatters.Snapshot.CaptureCadence.cs` owns capture cadence,
  packet fingerprint, and visual-cadence snapshot text.
- `tools/ssctl/Formatters.Snapshot.AvSync.cs` owns embedded snapshot AV-sync
  drift text.
- `tools/ssctl/Formatters.Snapshot.Flashback.cs` owns Flashback snapshot
  active/failure gating and section ordering.
  `tools/ssctl/Formatters.Snapshot.Flashback.Encoding.cs` owns Flashback
  encoder, buffer, temp-cache, queue-latency, backpressure, failure, and GPU
  queue snapshot text. `tools/ssctl/Formatters.Snapshot.Flashback.Playback.cs`
  owns Flashback playback state, command-queue, cadence, decode, frame, stage,
  and A/V drift snapshot text.
  `tools/ssctl/Formatters.Snapshot.Flashback.Export.cs` owns Flashback export
  progress/result snapshot text.
- `tools/ssctl/Formatters.Snapshot.Memory.cs` owns embedded snapshot Memory/GC
  text.
- `tools/ssctl/Formatters.Snapshot.Mjpeg.cs` owns MJPEG timing snapshot text.
- `tools/ssctl/Formatters.Snapshot.Preview.cs` owns preview renderer-mode
  routing, GPU-media-source text, and non-D3D preview fallback text.
  `tools/ssctl/Formatters.Snapshot.PreviewD3D.cs` owns D3D preview renderer
  snapshot text.
- `tools/ssctl/Formatters.Snapshot.ThreadHealth.cs` owns source-reader and
  WASAPI thread-health snapshot text.
- `tools/ssctl/Formatters.Snapshot.Source.cs` owns source telemetry snapshot
  text.
- `tools/ssctl/Formatters.Diagnostics.cs` owns recent diagnostic-event output.
- `tools/ssctl/Formatters.Options.cs` owns capture option and device lists.
- `tools/ssctl/Formatters.Timeline.cs` owns performance timeline response
  validation and top-level orchestration.
- `tools/ssctl/Formatters.Timeline.Rows.cs` owns performance timeline JSON row
  projection and the private row model.
- `tools/ssctl/Formatters.Timeline.Rendering.cs` owns performance timeline
  table output.
- `tools/ssctl/Formatters.Timeline.Summaries.cs` owns first-vs-last trend
  summary text.
- `tools/ssctl/Formatters.Memory.cs` owns standalone memory and GC summaries.
- `tools/ssctl/Formatters.Common.cs` owns shared result/JSON helpers.
- `tools/McpServer/Tools/PerformanceTimelineTools.cs` owns the public MCP
  tool entry point and command response handling.
- `tools/McpServer/Tools/PerformanceTimelineTools.Rows.cs` owns timeline JSON
  row projection and the private row model.
- `tools/McpServer/Tools/PerformanceTimelineTools.Rendering.cs` owns timeline
  table, trend, Flashback command, export, and target-summary text rendering.
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
  frame-capture MCP entry point, default output path, payload shaping, command
  routing, and failure/missing-data response handling.
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
- `tools/Common/DiagnosticSessionResult.cs` owns diagnostic-session core
  summary metadata, artifact paths, terminal state, actions, and warnings.
  `DiagnosticSessionResult.Capture.cs` owns capture/source summary fields.
  `DiagnosticSessionResult.FlashbackPlayback.cs` owns Flashback playback
  command, cadence, decode, audio-master, and stage fields.
  `DiagnosticSessionResult.FlashbackRecording.cs` owns Flashback recording
  backend/growth/integrity fields. `DiagnosticSessionResult.FlashbackExport.cs`
  owns Flashback export status/progress fields.
  `DiagnosticSessionResult.Preview.cs` owns preview cadence, scheduler, D3D,
  and visual-cadence fields. `DiagnosticSessionResult.Overview.cs` owns
  process CPU, recording verification, and PresentMon fields.
- `tools/Common/DiagnosticSessionSample.cs` owns sampled snapshot DTOs.
- `tools/Common/DiagnosticSessionResultBuilder.cs` owns diagnostic-session
  result phase orchestration, artifact-write handoff, summary-write handoff,
  and final summary emission. Keep `summary.json` field shape stable in the
  builder family.
- `tools/Common/DiagnosticSessionResultBuilder.Result.cs` owns
  diagnostic-session final DTO construction from named result projections.
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
- `tools/Common/DiagnosticSessionResultBuilder.FlashbackPlaybackResult.cs` owns
  Flashback playback result projection composition from focused playback
  projection owners.
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
  baseline `GetSnapshot` capture, unknown-state warning, and initial-snapshot
  exception recording.
- `tools/Common/DiagnosticSessionRunState.cs` owns diagnostic-session terminal
  exception state, last-stage tracking, live-state breadcrumbs, and
  best-effort artifact write failure recording.
- `tools/Common/DiagnosticSessionRunBootstrap.cs` owns diagnostic-session
  scenario normalization, scenario-plan selection, duration/sample clamping,
  session identity, output-directory creation, and runner process metadata.
- `tools/Common/DiagnosticSessionRunExecution.cs` owns diagnostic-session phase
  sequencing around initial snapshot capture, scenario phase invocation,
  cleanup, recording checks, post-run snapshots, and result handoff.
- `tools/Common/DiagnosticSessionRunExecution.Scenario.cs` owns the scenario
  phase handoff from the run-execution root.
- `tools/Common/DiagnosticSessionScenarioPhaseRunner.cs` owns the named
  diagnostic-session scenario phase: context/state/result records,
  state-mutation gating, setup/startup, sampling, background task awaits,
  rejected-export handling, PresentMon await, fault drain, and the cleanup
  result consumed by `RunAsync`.
- `tools/Common/DiagnosticSessionRunExecution.ResultRequest.cs` owns the final
  diagnostic-session result-build request mapping so the runner execution root
  keeps the phase sequence readable.
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
  non-export Flashback scenario task registration. Keep task stage names stable
  there.
- `tools/Common/DiagnosticSessionScenarioStartup.DeferredSettings.cs` owns
  deferred Flashback recording-settings task registration.
- `tools/Common/DiagnosticSessionScenarioStartup.Playback.cs` owns the direct
  Flashback playback start command, playback buffer readiness warning, and
  playback-state wait.
- `tools/Common/DiagnosticSessionPresentMonStartup.cs` owns optional PresentMon
  launch, correlation snapshot capture, and `presentmon.csv` output selection
  for diagnostic sessions.
- `tools/Common/DiagnosticSessionScenarioSetup.cs` owns diagnostic-session
  initial state mutations before sampling: enabling or disabling Flashback for
  scenarios, starting preview, starting recording, and waiting for the
  associated readiness conditions.
- `tools/Common/DiagnosticSessionCleanupActions.cs` owns diagnostic-session
  cleanup flow and recording stop for verification. Keep cleanup stage/action
  names stable in the cleanup family.
- `tools/Common/DiagnosticSessionCleanupActions.StateRestore.cs` owns
  Flashback playback go-live restore, preview stop, and Flashback enable-state
  restore.
- `tools/Common/DiagnosticSessionCleanupActions.Models.cs` owns the cleanup
  result handoff record.
- `tools/Common/DiagnosticSessionRecordingChecks.cs` owns post-cleanup
  diagnostic-session recording checks: deferred Flashback recording-settings
  restore, last-recording or Flashback export verification, and Flashback
  recording validation. Keep the `settings-deferred-restore`,
  `recording-verification`, and `recording-validation` stage names stable
  there.
- `tools/Common/DiagnosticSessionPostRunSnapshots.cs` owns post-run
  diagnostic-session snapshot fetches: performance timeline collection and
  final health snapshot refresh. Keep the `timeline` and `final-snapshot` stage
  names stable there.
- `tools/Common/DiagnosticSessionCleanupPolicy.cs` owns cleanup restore
  validation after diagnostic sessions stop recording, preview, Flashback, or
  playback state.
- `tools/Common/DiagnosticSessionFlashbackCycleScenarios.cs` owns Flashback
  diagnostic restart-cycle and encoder-cycle command flows, including export
  verification and preset restoration.
- `tools/Common/DiagnosticSessionMetrics.cs` is the diagnostic-session metric
  projection marker shell.
- `tools/Common/DiagnosticSessionMetrics.Models.cs` owns metric DTOs.
- `tools/Common/DiagnosticSessionMetrics.SourceCadence.cs` owns source-cadence
  projection from sampled snapshots.
- `tools/Common/DiagnosticSessionMetrics.PreviewCadence.cs` owns preview and
  visual cadence projection plus visual-cadence health classification.
- `tools/Common/DiagnosticSessionMetrics.PreviewD3D.cs` owns D3D slow-frame and
  CPU timing summaries.
- `tools/Common/DiagnosticSessionMetrics.PlaybackCommands.cs` owns playback
  command-health deltas.
- `tools/Common/DiagnosticSessionMetrics.Counters.cs` owns shared counter-delta
  helpers.
- `tools/Common/DiagnosticSessionFlashbackExports.cs` owns Flashback export
  diagnostic helpers: strict export verification payloads, rotated-export
  segment-count parsing, range-selection cleanup, and the audio-toggle
  companion used by the range export audio-switch scenario.
- `tools/Common/DiagnosticSessionFlashbackExportScenarios.cs` is the Flashback
  export diagnostic scenario marker shell. Concurrent export,
  disable-during-export, rotated export, export-during-playback, and
  selection-range export flows live in focused files:
  `tools/Common/DiagnosticSessionFlashbackExportScenarios.Concurrent.cs`,
  `tools/Common/DiagnosticSessionFlashbackExportScenarios.DisableDuringExport.cs`,
  `tools/Common/DiagnosticSessionFlashbackExportScenarios.Rotated.cs`,
  `tools/Common/DiagnosticSessionFlashbackExportScenarios.Playback.cs`, and
  `tools/Common/DiagnosticSessionFlashbackExportScenarios.Range.cs`.
  `tools/Common/DiagnosticSessionFlashbackExportScenarios.Registrations.cs`
  owns the export scenario task registration handoff from diagnostic-session
  startup.
- `tools/Common/DiagnosticSessionFlashbackLifecycleScenarios.cs` owns
  Flashback playback disable/re-enable lifecycle diagnostic task registration
  and flow.
- `tools/Common/DiagnosticSessionFlashbackMetrics.cs` is the Flashback metric
  projection marker shell.
  `tools/Common/DiagnosticSessionFlashbackMetrics.Models.cs` owns session/result
  DTOs. `tools/Common/DiagnosticSessionFlashbackMetrics.Recording.cs`,
  `tools/Common/DiagnosticSessionFlashbackMetrics.PlaybackSession.cs`,
  `tools/Common/DiagnosticSessionFlashbackMetrics.PlaybackResult.cs`, and
  `tools/Common/DiagnosticSessionFlashbackMetrics.Export.cs` own read-only
  recording, playback, result-copy, and export metric projections. Export
  metrics include force-rotate fallback total, delta, and last fallback segment
  count; keep those counters derived outside export-observed relevance gating.
- `tools/Common/DiagnosticSessionFlashbackPreviewCycleScenarios.cs` is the
  Flashback preview-cycle marker shell and predicate owner.
- `tools/Common/DiagnosticSessionFlashbackPreviewCycleScenarios.Flashback.cs`
  owns normal Flashback preview stop/restart diagnostic flow.
- `tools/Common/DiagnosticSessionFlashbackPreviewCycleScenarios.Playback.cs`
  owns playback-under-preview-stop diagnostic flow.
- `tools/Common/DiagnosticSessionFlashbackPreviewCycleScenarios.Recording.cs`
  owns Flashback-recording-backed preview stop/restart diagnostic flow.
- `tools/Common/DiagnosticSessionFlashbackRejectedExports.cs` owns Flashback
  rejected-export diagnostic scenario dispatch and flows for inactive buffers
  and active Flashback recording backends.
- `tools/Common/DiagnosticSessionFlashbackRecordingSettingsScenarios.cs` is the
  Flashback recording-settings diagnostic marker shell.
- `tools/Common/DiagnosticSessionFlashbackRecordingSettingsScenarios.Models.cs`
  owns deferred preset state shared by startup, background task, and post-stop
  recording checks.
- `tools/Common/DiagnosticSessionFlashbackRecordingSettingsScenarios.DuringRecording.cs`
  owns preset mutation and restart/disable rejection checks while Flashback is
  recording.
- `tools/Common/DiagnosticSessionFlashbackRecordingSettingsScenarios.PostStop.cs`
  owns post-stop preset verification, encoder-frame check, and original-preset
  restore.
- `tools/Common/DiagnosticSessionFlashbackSegmentPlaybackScenarios.cs` owns the
  Flashback completed-segment playback scenario.
- `tools/Common/DiagnosticSessionFlashbackSegmentPlaybackScenarios.RecordingAssist.cs`
  owns recording-assisted segment rotation and best-effort stop cleanup for
  segment playback diagnostics.
- `tools/Common/DiagnosticSessionFlashbackSegments.cs` owns read-only
  diagnostic-session Flashback segment parsing, completed-segment waits, and
  playable-boundary headroom waits. Do not add state-mutating scenario steps
  there.
- `tools/Common/DiagnosticSessionFlashbackStressScenario.cs` owns Flashback
  stress thresholds.
- `tools/Common/DiagnosticSessionFlashbackStressScenario.Stress.cs` owns the
  main Flashback stress command sequence and export verify.
- `tools/Common/DiagnosticSessionFlashbackStressScenario.WarmPlayback.cs` owns
  warmed-playback frame/FPS/1% low and audio-master fallback checks.
- `tools/Common/DiagnosticSessionFlashbackStressScenario.CommandDrain.cs` owns
  post-go-live playback command drain, latency, and final-state checks.
- `tools/Common/DiagnosticSessionFlashbackStressScenario.Scrub.cs` owns the
  scrub-stress command burst and drain checks.
- `tools/Common/DiagnosticSessionFlashbackStressScenario.AudioMaster.cs` owns
  warmed-playback audio-master fallback classification.
- `tools/Common/DiagnosticSessionFlashbackWaits.cs` owns read-only snapshot
  polling waits used by Flashback diagnostic scenarios, including preview
  active, Flashback active, recording-ready, and buffer-ready waits.
- `tools/Common/DiagnosticSessionFlashbackWaits.Playback.cs` owns Flashback
  playback boundary, state, warmup, and position polling waits.
- `tools/Common/DiagnosticSessionFlashbackValidation.cs` owns Flashback
  diagnostic-session warning policy for recording, playback, and preview
  scheduler metrics.
- `tools/Common/DiagnosticSessionHealthPolicy.cs` owns diagnostic-session health
  observation, severity, and Flashback warmup filtering.
- `tools/Common/DiagnosticSessionHealthTolerances.cs` owns diagnostic-session
  source/preview/Flashback health-observation classifiers, sparse-cadence
  tolerances, and tolerated Flashback warning classification.
- `tools/Common/DiagnosticSessionSampler.cs` owns snapshot sample collection.
  Preserve its ordering: append the cloned sample before running checkpoint
  callbacks.
- `tools/Common/DiagnosticSessionResultFormatter.cs` owns the public
  human-readable diagnostic-session text flow used by ssctl and MCP.
- `tools/Common/DiagnosticSessionResultFormatter.Overview.cs` owns header,
  capture-mode, recording-verification, PresentMon, and process sections.
- `tools/Common/DiagnosticSessionResultFormatter.Flashback.cs` owns Flashback
  diagnostic-session text section ordering.
  `DiagnosticSessionResultFormatter.FlashbackPlayback.Commands.cs` owns
  playback command lines,
  `DiagnosticSessionResultFormatter.FlashbackPlayback.Performance.cs` owns
  playback cadence/audio-master performance lines,
  `DiagnosticSessionResultFormatter.FlashbackPlayback.Decode.cs` owns playback
  decode timing lines,
  `DiagnosticSessionResultFormatter.FlashbackPlayback.Stages.cs` owns playback
  stage/seek-cap lines,
  `DiagnosticSessionResultFormatter.FlashbackRecording.cs` owns recording
  lines, and `DiagnosticSessionResultFormatter.FlashbackExport.cs` owns export
  lines.
- `tools/Common/DiagnosticSessionResultFormatter.Preview.cs` owns preview
  diagnostic-session text section ordering.
  `DiagnosticSessionResultFormatter.Preview.Scheduler.cs` owns preview
  scheduler lines,
  `DiagnosticSessionResultFormatter.Preview.D3DPerformance.cs` owns preview
  D3D performance/slow-frame lines,
  `DiagnosticSessionResultFormatter.Preview.D3DCpuTiming.cs` owns preview D3D
  CPU timing lines, and
  `DiagnosticSessionResultFormatter.Preview.VisualCadence.cs` owns preview
  visual-cadence lines.
- `tools/Common/DiagnosticSessionResultFormatter.Artifacts.cs` owns artifact,
  action, and warning sections.
- `tools/Common/DiagnosticSessionResultFormatter.Helpers.cs` owns small text
  helpers such as frame-rate formatting. Keep
  `DiagnosticSessionRunner.Format(...)` as the stable compatibility wrapper.
- `tools/Common/DiagnosticSessionText.cs` owns shared diagnostic-session text
  helpers used by the runner, formatter, and validation policies.
- `tools/Common/AutomationSnapshotFormatter.cs` owns the top-level shared
  automation snapshot console text flow and delegates each named output
  section to a focused partial.
  `tools/Common/AutomationSnapshotFormatter.State.cs` owns the state,
  capture-command queue, selected-device, and initialized/preview/recording
  text.
  `tools/Common/AutomationSnapshotFormatter.CaptureSettings.cs` owns capture
  option, recording format, HDR, pipeline, and compact UI setting text.
  `tools/Common/AutomationSnapshotFormatter.Audio.cs` owns audio enablement,
  preview/custom input, signal, reader, and frame-count text.
  `tools/Common/AutomationSnapshotFormatter.VideoPipeline.cs` owns video reader,
  encoder queue, queue-latency, backpressure, failure, GPU/CUDA queue,
  freshness, diagnostics, and thread-health routing text.
  `tools/Common/AutomationSnapshotFormatter.Recording.cs` owns recording
  summary, integrity, audio integrity, and last-output text.
  `tools/Common/AutomationSnapshotFormatter.Diagnostics.cs` owns diagnostic
  health, summary, evidence, and frame-lane text.
  `tools/Common/AutomationSnapshotFormatter.Performance.cs` owns legacy
  performance score, summary, and pipeline-latency text.
  `tools/Common/AutomationSnapshotFormatter.Memory.cs` owns process CPU,
  working-set/private/managed memory, GC, and thread-pool text.
  `tools/Common/AutomationSnapshotFormatter.CaptureCadence.cs` owns capture
  cadence, low-FPS, jitter/drop, MJPEG packet fingerprint, sampled visual
  cadence, and routing to MJPEG/AV-sync/preview/source sections.
  `tools/Common/AutomationSnapshotFormatter.Values.cs` owns tolerant JSON
  accessors and typed JSON coercion.
  `tools/Common/AutomationSnapshotFormatter.DisplayValues.cs` owns shared
  byte, number, interval, frame-budget, and tick-age display helpers, while
  `tools/Common/AutomationSnapshotFormatter.Flashback.cs` owns the Flashback
  gate, header, and subsection ordering.
  `tools/Common/AutomationSnapshotFormatter.Flashback.Encoding.cs` owns
  Flashback encoder, buffer, cache, queue, failure, backpressure, and GPU queue
  text. `tools/Common/AutomationSnapshotFormatter.Flashback.Playback.cs` owns
  Flashback playback status, command queue, cadence, decode, frame, stage, and
  A/V drift text. `tools/Common/AutomationSnapshotFormatter.Flashback.Export.cs`
  owns Flashback export progress/result text. The
  `tools/Common/AutomationSnapshotFormatter.MjpegTiming.cs`,
  `tools/Common/AutomationSnapshotFormatter.AvSync.cs`,
  `tools/Common/AutomationSnapshotFormatter.Preview.cs`,
  `tools/Common/AutomationSnapshotFormatter.PreviewD3D.cs`,
  `tools/Common/AutomationSnapshotFormatter.ThreadHealth.cs`, and
  `tools/Common/AutomationSnapshotFormatter.Source.cs` own the named snapshot
  sections.
- `tools/Common/DiagnosticSessionPipeRetryPolicy.cs` owns diagnostic-session
  connect retry classification and local failure-response envelopes.
- `tools/Common/DiagnosticSessionCommandChannel.cs` owns serialized
  diagnostic-session automation command sending, connect-retry wrapping,
  command failure accounting, and `WaitForCondition` command payload shaping.
- `tools/Common/DiagnosticSessionScenarioPlan.cs` owns normalized scenario
  flags and grouped warning/validation policies used by the runner. Keep new
  scenario booleans there instead of adding string comparisons in
  `DiagnosticSessionRunner`.
- `tools/Common/PresentMonProbe.Models.cs` owns PresentMon option/result,
  summary, swap-chain, correlation, and metric DTOs.
- `tools/Common/PresentMonProbe.ResultMessage.cs` owns PresentMon result-message
  shaping for success, expected-swap-chain mismatch, and no-frame outcomes.
- `tools/Common/PresentMonProbe.Format.cs` owns PresentMon result text rendering
  used by diagnostic-session output surfaces.
- `tools/Common/PresentMonProbe.Csv.cs` owns PresentMon CSV parse overloads,
  selected-row filtering, summary assembly, and handoff to row/swap-chain/
  warning/correlation helpers.
- `tools/Common/PresentMonProbe.Csv.Rows.cs` owns PresentMon CSV row ingestion,
  header index construction, schema-presence detection, blank-line skipping,
  row index assignment, and row projection from header-indexed fields.
- `tools/Common/PresentMonProbe.Csv.Fields.cs` owns header/field parsing,
  scalar field/metric reads, and CSV line tokenization.
- `tools/Common/PresentMonProbe.Csv.SwapChains.cs` owns swap-chain
  normalization, artifact filtering, and selected-chain summaries.
- `tools/Common/PresentMonProbe.Csv.Correlation.cs` owns app-present
  correlation and displayed/not-displayed outcome classification.
- `tools/Common/PresentMonProbe.Csv.Summary.cs` owns warnings, counted text
  fields, and percentile metric aggregation.
- `tools/Common/PresentMonProbe.Csv.Models.cs` owns the private parsed CSV
  handoff and row shapes.
- `tools/Common/PresentMonProbe.cs` owns PresentMon public run orchestration.
- `tools/Common/PresentMonProbe.Paths.cs` owns target process, PresentMon
  executable, and output-path resolution.
- `tools/Common/PresentMonProbe.Arguments.cs` owns PresentMon command-line
  construction and argument quoting.
- `tools/Common/PresentMonProbe.Process.cs` owns process supervision,
  stdout/stderr drain, timeout kill, and temp CSV cleanup.

Invariants:

- Do not add new automation metadata to tool-specific files if it belongs in
  `Sussudio.Automation.Contracts`.
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
- Preserve diagnostic-session cleanup stage/action names when moving cleanup
  mutations; downstream result text and failure reports use those names as
  evidence.
- Preserve result text compatibility when refactoring diagnostic-session
  formatting; ssctl and MCP both flow through `DiagnosticSessionRunner.Format`.
- Preserve pipe error-code semantics when refactoring diagnostic-session retry:
  `pipe-access-denied` is permanent, while connect failed/timeout are retried.
- Add new diagnostic-session scenario names in
  `tools/Common/DiagnosticSessionScenarios.cs` before wiring scenario behavior
  into `DiagnosticSessionRunner`.
- Keep diagnostic-session scenario flag derivation in
  `tools/Common/DiagnosticSessionScenarioPlan.cs`; the runner should consume
  named properties instead of comparing normalized scenario strings directly.

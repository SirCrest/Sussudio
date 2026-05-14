# Sussudio Agent Map

Last reviewed: 2026-05-13.

This file maps the current repo shape to named owners, entry points, invariants,
and fast checks. It is intentionally mechanical so future agents can find the
right file without guessing from old chat transcripts.

## High-Risk Large Files

These files are allowed to be large today, but they are not good expansion
targets. Prefer extracting new behavior into a named collaborator or feature
folder.

| Area | Current large files | Preferred next owner |
|------|---------------------|----------------------|
| Diagnostic sessions | `tools/Common/DiagnosticSessionRunner.cs` | scenario catalog, startup/cleanup/recording-check/post-run snapshot helpers, result formatter, plus per-scenario runners |
| Offline regression harness | `tests/Sussudio.Tests/Program.cs` | xUnit slices and focused contract tests such as `StatsPresentation.Contract.Tests.cs` |
| Capture runtime | `Sussudio/Services/Capture/CaptureService.cs`, `CaptureService.Audio.cs`, `CaptureService.Cleanup.cs`, `CaptureService.Coordination.cs`, `CaptureService.DeferredCleanup.cs`, `CaptureService.Failures.cs`, `CaptureService.FlashbackControls.cs`, `CaptureService.FlashbackExportDiagnostics.cs`, `CaptureService.FlashbackExportFailureClassification.cs`, `CaptureService.FlashbackExportOperations.cs`, `CaptureService.FlashbackExportPlanning.cs`, `CaptureService.FlashbackRecording.cs`, `CaptureService.HealthSnapshots.cs`, `CaptureService.PreviewLifecycle.cs`, `CaptureService.PreviewPipeline.cs`, `CaptureService.Probes.cs`, `CaptureService.RecordingIntegrity.cs`, `CaptureService.RuntimeSnapshots.cs`, `CaptureService.Snapshots.cs`, `CaptureService.SnapshotAvSync.cs`, `CaptureService.SnapshotTelemetry.cs`, `CaptureService.Telemetry.cs` | lifecycle owner, audio owner, cleanup owner, transition/disposal owner, deferred cleanup owner, failure owner, Flashback control owner, Flashback export diagnostics/progress owner, Flashback export failure taxonomy, Flashback export entry/core owner, Flashback export planning/throttle owner, Flashback recording policy owner, health snapshot builder, preview lifecycle owner, preview pipeline owner, probe owner, recording integrity owner, runtime snapshot builder, shared snapshot helper policy, A/V sync snapshot policy, source telemetry snapshot policy, telemetry owner, resource managers |
| Capture source reader | `Sussudio/Services/Capture/MfSourceReaderVideoCapture.cs`, `MfSourceReaderVideoCapture.Cadence.cs`, `MfSourceReaderVideoCapture.Diagnostics.cs`, `MfSourceReaderVideoCapture.DxgiBuffers.cs`, `MfSourceReaderVideoCapture.FrameLayout.cs`, `MfSourceReaderVideoCapture.Lifecycle.cs`, `MfSourceReaderVideoCapture.Negotiation.cs`, `MfSourceReaderVideoCapture.Interop.cs` | Media Foundation read loop/frame delivery, source cadence metrics, debug-only COM diagnostics, DXGI texture extraction, packed YUV frame layout and subtype labels, reader start/stop/dispose lifecycle, device opening and media-type negotiation, MF P/Invoke and COM interface definitions |
| Capture fan-out | `Sussudio/Services/Capture/UnifiedVideoCapture.cs`, `UnifiedVideoCapture.SinkFanout.cs`, `UnifiedVideoCapture.Metrics.cs`, `UnifiedVideoCapture.Preview.cs` | shared source-reader lifecycle and frame arrival routing, recording/Flashback sink queue fan-out, diagnostic metric/snapshot projection, preview sink submission and visual-cadence handling |
| MJPEG preview pacing | `Sussudio/Services/Capture/MjpegPreviewJitterBuffer.cs`, `MjpegPreviewJitterBuffer.Queue.cs`, `MjpegPreviewJitterBuffer.Adaptive.cs`, `MjpegPreviewJitterBuffer.Metrics.cs` | decoded preview-frame emit loop/lifecycle, queue ordering and reprime recovery, adaptive deadline/depth policy, jitter-buffer metric records and timing sample projection |
| MJPEG decode pipeline | `Sussudio/Services/Gpu/ParallelMjpegDecodePipeline.cs`, `ParallelMjpegDecodePipeline.Lifecycle.cs`, `ParallelMjpegDecodePipeline.Metrics.cs` | CPU MJPEG worker/reorder/emit loops, stop/dispose/resource cleanup and fatal callback signaling, pipeline timing and packet-hash metrics |
| Automation diagnostics | `Sussudio/Services/Automation/AutomationDiagnosticsHub.cs`, `AutomationDiagnosticsHub.Alerts.cs`, `AutomationDiagnosticsHub.SignalAlerts.cs`, `AutomationDiagnosticsHub.FlashbackAlerts.cs`, `AutomationDiagnosticsHub.FlashbackRecordingAlerts.cs`, `AutomationDiagnosticsHub.FlashbackPlaybackAlerts.cs`, `AutomationDiagnosticsHub.FlashbackPlaybackCommandAlerts.cs`, `AutomationDiagnosticsHub.FlashbackPlaybackPerformanceAlerts.cs`, `AutomationDiagnosticsHub.DiagnosticEvents.cs`, `AutomationDiagnosticsHub.DiagnosticEvaluation.cs`, `AutomationDiagnosticsHub.DiagnosticEvaluationFlashback.cs`, `AutomationDiagnosticsHub.DiagnosticEvaluationRealtime.cs`, `AutomationDiagnosticsHub.DiagnosticEvaluationLanes.cs`, `AutomationDiagnosticsHub.Evaluation.cs`, `AutomationDiagnosticsHub.EvaluationPolicy.cs`, `AutomationDiagnosticsHub.Hdr.cs`, `AutomationDiagnosticsHub.Lifecycle.cs`, `AutomationDiagnosticsHub.OutputFiles.cs`, `AutomationDiagnosticsHub.PreviewPacing.cs`, `AutomationDiagnosticsHub.ProcessMetrics.cs`, `AutomationDiagnosticsHub.Snapshots.cs`, `AutomationDiagnosticsHub.SnapshotProjection.cs`, `AutomationDiagnosticsHub.SnapshotProjection.SnapshotStatus.cs`, `AutomationDiagnosticsHub.SnapshotProjection.SnapshotEvaluation.cs`, `AutomationDiagnosticsHub.SnapshotProjection.Audio.cs`, `AutomationDiagnosticsHub.SnapshotProjection.AudioSignal.cs`, `AutomationDiagnosticsHub.SnapshotProjection.CaptureIngest.cs`, `AutomationDiagnosticsHub.SnapshotProjection.WasapiAudio.cs`, `AutomationDiagnosticsHub.SnapshotProjection.AvSync.cs`, `AutomationDiagnosticsHub.SnapshotProjection.CaptureCommands.cs`, `AutomationDiagnosticsHub.SnapshotProjection.CaptureFormat.cs`, `AutomationDiagnosticsHub.SnapshotProjection.CaptureTransport.cs`, `AutomationDiagnosticsHub.SnapshotProjection.CaptureCadence.cs`, `AutomationDiagnosticsHub.SnapshotProjection.Mjpeg.cs`, `AutomationDiagnosticsHub.SnapshotProjection.MjpegPreviewJitter.cs`, `AutomationDiagnosticsHub.SnapshotProjection.MjpegPacketHash.cs`, `AutomationDiagnosticsHub.SnapshotProjection.FlashbackExport.cs`, `AutomationDiagnosticsHub.SnapshotProjection.FlashbackPlayback.cs`, `AutomationDiagnosticsHub.SnapshotProjection.FlashbackPlaybackAudioMaster.cs`, `AutomationDiagnosticsHub.SnapshotProjection.FlashbackPlaybackDecode.cs`, `AutomationDiagnosticsHub.SnapshotProjection.FlashbackPlaybackCommands.cs`, `AutomationDiagnosticsHub.SnapshotProjection.FlashbackRecording.cs`, `AutomationDiagnosticsHub.SnapshotProjection.FlashbackRecordingStartupCache.cs`, `AutomationDiagnosticsHub.SnapshotProjection.FlashbackRecordingQueues.cs`, `AutomationDiagnosticsHub.SnapshotProjection.PreviewD3D.cs`, `AutomationDiagnosticsHub.SnapshotProjection.PreviewD3DCpuTiming.cs`, `AutomationDiagnosticsHub.SnapshotProjection.PreviewD3DFrameFlow.cs`, `AutomationDiagnosticsHub.SnapshotProjection.PreviewD3DFrameLatencyWait.cs`, `AutomationDiagnosticsHub.SnapshotProjection.PreviewD3DFrameStats.cs`, `AutomationDiagnosticsHub.SnapshotProjection.PreviewRuntime.cs`, `AutomationDiagnosticsHub.SnapshotProjection.PreviewRuntimeCadence.cs`, `AutomationDiagnosticsHub.SnapshotProjection.PreviewRuntimeStartup.cs`, `AutomationDiagnosticsHub.SnapshotProjection.ProcessResources.cs`, `AutomationDiagnosticsHub.SnapshotProjection.RecordingBackend.cs`, `AutomationDiagnosticsHub.SnapshotProjection.RecordingIntegrity.cs`, `AutomationDiagnosticsHub.SnapshotProjection.RecordingPipeline.cs`, `AutomationDiagnosticsHub.SnapshotProjection.RecordingOutput.cs`, `AutomationDiagnosticsHub.SnapshotProjection.SourceSignal.cs`, `AutomationDiagnosticsHub.SnapshotProjection.SourceTelemetry.cs`, `AutomationDiagnosticsHub.SnapshotProjection.UserSettings.cs`, `AutomationDiagnosticsHub.SnapshotProjection.HdrPipeline.cs`, `AutomationDiagnosticsHub.SnapshotState.cs`, `AutomationDiagnosticsHub.Timeline.cs`, `AutomationDiagnosticsHub.TimelineProjection.cs`, `AutomationDiagnosticsHub.Verification.cs` | additional collectors/controllers when hub orchestration grows |
| Automation snapshot models | `Sussudio/Models/Automation/AutomationSnapshot.cs`, `CaptureRuntimeSnapshot.cs`, `PreviewRuntimeSnapshot.cs`, `PerformanceTimelineEntry.cs`, `ViewModelRuntimeSnapshot.cs` | automation evidence DTO, capture runtime DTO, preview runtime DTO, performance timeline entry, view-model runtime DTO |
| Source telemetry | `Sussudio/Services/Telemetry/NativeXuAtCommandProvider.cs`, `NativeXuAtCommandProvider.AtProtocol.cs`, `NativeXuAtCommandProvider.DeviceCommands.cs`, `NativeXuAtCommandProvider.DiagnosticSummary.cs`, `NativeXuAtCommandProvider.FullSnapshot.cs`, `NativeXuAtCommandProvider.TelemetryDetails.cs` | Native XU telemetry polling/snapshot assembly, AT-command transport/parsing, public device-command surface, diagnostic summary formatting, reference full-snapshot reader, source telemetry detail presentation |
| Recording | `Sussudio/Services/Recording/LibAvEncoder.cs`, `LibAvEncoder.Audio.cs`, `LibAvEncoder.CodecPolicy.cs`, `LibAvEncoder.AvSync.cs`, `LibAvEncoder.PacketWriting.cs`, `LibAvEncoder.FrameCopy.cs`, `LibAvEncoder.Diagnostics.cs`, `LibAvEncoder.VideoSetup.cs`, `LibAvEncoder.OutputLifecycle.cs`, `LibAvRecordingSink.cs`, `LibAvRecordingSink.EncodingLoop.cs`, `LibAvRecordingSink.Queues.cs`, `RecordingVerifier.cs`, `RecordingVerifier.Cadence.cs` | encoder runtime/lifecycle, audio/microphone stream setup and sample drains, codec/options policy, A/V sync diagnostics, video packet drain/write helpers, packed software-frame copy helpers, open/error/device-removed diagnostics, video codec/hardware setup, rotation/output cleanup, sink lifecycle/options, recording sink encode-drain loop, recording sink queue surface, verifier/finalizer, verifier cadence analysis |
| Flashback | `FlashbackDecoder.cs`, `FlashbackDecoder.D3D11.cs`, `FlashbackDecoder.VideoOutput.cs`, `FlashbackDecoder.VideoSetup.cs`, `FlashbackDecoder.AudioOutput.cs`, `FlashbackDecoder.Timestamps.cs`, `FlashbackDecoder.Validation.cs`, `FlashbackDecoder.Lifetime.cs`, `FlashbackDecoder.Diagnostics.cs`, `FlashbackDecoder.Guards.cs`, `FlashbackDecoder.OutputTypes.cs`, `FlashbackPlaybackController.cs`, `FlashbackPlaybackController.DecoderFiles.cs`, `FlashbackPlaybackController.DecoderReopen.cs`, `FlashbackPlaybackController.Lifecycle.cs`, `FlashbackPlaybackController.Commands.cs`, `FlashbackPlaybackController.CommandQueue.cs`, `FlashbackPlaybackController.CommandCoalescing.cs`, `FlashbackPlaybackController.CommandTelemetry.cs`, `FlashbackPlaybackController.ThreadLifecycle.cs`, `FlashbackPlaybackController.ThreadCleanup.cs`, `FlashbackPlaybackController.ThreadTimer.cs`, `FlashbackPlaybackController.AudioRouting.cs`, `FlashbackPlaybackController.AudioPrebuffer.cs`, `FlashbackPlaybackController.AudioMasterPacing.cs`, `FlashbackPlaybackController.PreviewFrames.cs`, `FlashbackPlaybackController.SeekDisplay.cs`, `FlashbackPlaybackController.PlaybackLoop.cs`, `FlashbackPlaybackController.PlaybackSegmentEdges.cs`, `FlashbackPlaybackController.PlaybackTiming.cs`, `FlashbackPlaybackController.Markers.cs`, `FlashbackPlaybackController.PositionMapping.cs`, `FlashbackPlaybackController.Metrics.cs`, `FlashbackPlaybackController.MetricsCollection.cs`, `FlashbackEncoderSink.cs`, `FlashbackEncoderSink.EncodingLoop.cs`, `FlashbackEncoderSink.SegmentRotation.cs`, `FlashbackEncoderSink.ForceRotate.cs`, `FlashbackEncoderSink.Inputs.cs`, `FlashbackEncoderSink.Lifetime.cs`, `FlashbackEncoderSink.Options.cs`, `FlashbackEncoderSink.Queues.cs`, `FlashbackEncoderSink.Recording.cs`, `FlashbackEncoderSink.RuntimeState.cs`, `FlashbackBufferManager.cs`, `FlashbackBufferManager.Lifecycle.cs`, `FlashbackBufferManager.SegmentQueries.cs`, `FlashbackBufferManager.Math.cs`, `FlashbackBufferManager.Retention.cs`, `FlashbackExporter.cs`, `FlashbackExporter.SingleFile.cs`, `FlashbackExporter.Segments.cs`, `FlashbackExporter.Requests.cs`, `FlashbackExporter.Lifetime.cs`, `FlashbackExporter.Execution.cs`, `FlashbackExporter.PacketTiming.cs`, `FlashbackExporter.Streams.cs`, `FlashbackExporter.OutputFiles.cs`, `FlashbackExporter.Infrastructure.cs` | decoder lifecycle/open/seek/control flow, D3D11VA decoder discovery/initialization, video frame output/conversion, video codec setup and software output-buffer allocation, decoder audio packet delivery and bounded audio output, decoder timestamp/seek conversion helpers, decoder stream/frame validation helpers, decoder file-close native cleanup and held-frame release, decoder phase timing and FFmpeg error formatting, decoder state guards, decoded video/audio output DTOs, playback core, decoder file open/cleanup, active fMP4 reopen and seek recovery, component lifecycle and dispose, public playback command facade, command queue/drop policy, seek/scrub coalescing, command readiness/telemetry bookkeeping, playback thread lifecycle, playback thread cleanup, timer-resolution P/Invoke, audio callback/routing/render helpers, audio prebuffer/rewind, audio-master pacing/fallbacks, decoded frame submission/ownership, seek/scrub frame display, continuous playback loop, segment-edge switching/reopen/write-head handling, timing/cadence policy, marker owner, position/file-PTS mapping, public metrics surface, metric collection/reset, encoder startup/helpers, encode loop and packet drains, segment rotation/failure recovery, export force-rotation handshake, producer/callback input surface, stop/dispose lifecycle, encoder options/packet helpers, encoder queue helpers, retroactive recording lifecycle, public counters/status, buffer live counters and segment mutation surface, buffer initialize/dispose/recovery-preserve lifecycle, buffer segment query/projection helpers, buffer math and saturated accounting helpers, buffer retention/purge/eviction, shared exporter native state, single-file export packet-copy/remux core, multi-segment export packet-copy/remux core, export request routing, exporter disposal, export execution scheduling, packet timestamp and buffer helpers, stream/template setup, final output replacement, export infrastructure |
| Preview rendering | `D3D11PreviewRenderer.cs`, `D3D11PreviewRenderer.FrameTypes.cs`, `D3D11PreviewRenderer.FrameOwnership.cs`, `D3D11PreviewRenderer.DxgiFrameStatistics.cs`, `D3D11PreviewRenderer.Submission.cs`, `D3D11PreviewRenderer.Rendering.cs`, `D3D11PreviewRenderer.DeviceLost.cs`, `D3D11PreviewRenderer.FrameUpload.cs`, `D3D11PreviewRenderer.FrameLatency.cs`, `D3D11PreviewRenderer.Viewport.cs`, `D3D11PreviewRenderer.Resources.cs`, `D3D11PreviewRenderer.PanelBinding.cs`, `D3D11PreviewRenderer.PendingFrames.cs`, `D3D11PreviewRenderer.Metrics.cs`, `D3D11PreviewRenderer.ScreenshotCapture.cs`, `D3D11PreviewRenderer.ShaderSources.cs` | renderer host, pending-frame and metrics model types, submitted/rendered/dropped frame ownership telemetry, DXGI frame statistics and display-clock projection, public frame submission entry points, render loop/draw paths, device-lost classification and recovery, raw-frame and external-texture upload helpers, frame-latency waitable swap-chain setup/waits, viewport and letterbox helpers, D3D device/pipeline resources, swap-chain panel binding and composition transforms, pending-frame queue/signaling, present/latency metrics, screenshot capture, shader source, timing models |
| UI shell | `MainWindow.*.cs` partial family | named controllers under an app shell folder |
| Presentation | `MainViewModel.*.cs` partial family | feature view models behind the root facade |

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

Do not reintroduce linked source for these files from `tools/Common`. Consumers
should reference `Sussudio.Automation.Contracts`.

Fast checks:

```powershell
dotnet build Sussudio.slnx -p:Platform=x64 --no-restore
dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore
```

Automation diagnostics ownership:

- `Sussudio/Services/Automation/AutomationCommandDispatcher.cs` owns the
  command router, switch bodies, trivial-handler table, and initialization
  readiness check.
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
  owns CPU MJPEG decode, reorder, compressed queue, and per-decoder projection
  inputs consumed by `AutomationSnapshot`.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.MjpegPreviewJitter.cs`
  owns MJPEG preview jitter queue, timing, drop, underflow, and adaptive-depth
  projection inputs consumed by `AutomationSnapshot`.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.MjpegPacketHash.cs`
  owns MJPEG packet duplicate-run / unique-frame projection inputs consumed by
  `AutomationSnapshot`.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.FlashbackExport.cs`
  owns active Flashback export progress, failure, force-rotate fallback, and
  last-result projection consumed by `AutomationSnapshot`.
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
  owns preview frame counters, GPU playback state, preview HDR state, and
  preview color-context projection consumed by `AutomationSnapshot`.
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
  owns selected device, selected capture/recording options, preview volume, and
  stats visibility projection consumed by `AutomationSnapshot`.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.HdrPipeline.cs`
  owns HDR availability/request state, runtime/readiness fallback, HDR
  warmup/downgrade, pipeline parity, and telemetry-alignment projection consumed
  by `AutomationSnapshot`.
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

- `CaptureSessionCoordinator.cs` serializes lifecycle mutations.
- `CaptureSessionCoordinator.Models.cs` owns command enums, queue receipts,
  session snapshots, and Flashback playback/buffer status projections.
- `CaptureSessionCoordinator.Flashback.cs` owns queued Flashback mutations,
  read-only Flashback status/projections, export forwarding, and active
  playback-controller readiness checks.
- `CaptureSessionTransitionPolicy.cs` owns pure transition legality and
  steady-state resolution for `CaptureService`.
- `CaptureService.cs` still owns too many resource lifetimes and should not
  receive unrelated UI, Flashback, or diagnostics behavior.
- `CaptureService.Audio.cs` owns audio preview, microphone monitoring, live
  audio input switching, and WASAPI playback attach/detach order.
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
- `CaptureService.PreviewLifecycle.cs` owns video preview start/stop,
  retained-backend reuse checks, preview-start rollback, and preview pipeline
  disposal ordering.
- `CaptureService.PreviewPipeline.cs` owns preview frame sink attachment,
  shared D3D preview-device handoff, negotiated video getters, and cached MJPEG
  pipeline timing details.
- `CaptureService.Probes.cs` owns read-only automation probes and preview-frame
  capture waits.
- `CaptureService.RecordingIntegrity.cs` owns recording integrity counters,
  baseline deltas, audio integrity classification, and summary logging policy.
- `CaptureService.RuntimeSnapshots.cs` builds runtime snapshots consumed by UI,
  automation, and verification.
- `CaptureService.Snapshots.cs` owns shared snapshot helper policy and the
  diagnostics-snapshot compatibility entry point.
- `CaptureService.SnapshotAvSync.cs` owns A/V sync drift snapshot helpers for
  live source/audio drift and encoder correction telemetry.
- `CaptureService.SnapshotTelemetry.cs` owns source telemetry snapshot
  presentation policy, telemetry/request alignment, and HDR warmup state
  classification.
- `CaptureService.Telemetry.cs` owns source telemetry polling, fallback merge,
  NTSC frame-rate correction, and observed pixel-format accounting.
- `UnifiedVideoCapture.cs` owns shared source-reader lifecycle and frame arrival
  routing for preview, recording, and Flashback.
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

- `RecordingVerifier.cs` owns strict ffprobe orchestration, stream/container/HDR
  verification, mismatch taxonomy, early-failure shaping, and ffprobe path
  resolution.
- `RecordingVerifier.Cadence.cs` owns ffprobe frame timestamp parsing and
  cadence/drop/jitter metric calculation.

## Flashback

Primary current owner: `Sussudio/Services/Flashback/`

Entry points:

- `FlashbackBackendResources.cs` owns backend resource grouping.
- `FlashbackBufferManager.cs` owns buffer live counters and segment mutation.
- `FlashbackBufferManager.Lifecycle.cs` owns initialization, segment extension setup, recovery-preserve markers, disposal, and disposed-state guards.
- `FlashbackBufferManager.SegmentQueries.cs` owns read-only segment counts, active-path projection, segment path lookup, start-PTS lookup, and segment-info projection.
- `FlashbackBufferManager.Retention.cs` owns purge, eviction, delete, disk-warning, and recording-boundary retention behavior.
- `FlashbackDecoder.cs` owns decoder lifecycle, file open/close, seek/decode control flow, and native cleanup.
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
- `FlashbackEncoderSink.Options.cs` owns encoder option construction, recording-to-Flashback session mapping, packet records, file-size/session-id helpers, and buffer/COM return helpers.
- `FlashbackEncoderSink.Queues.cs` owns queue completion/signaling, queue-depth accounting, enqueue rejection guards/logging, hot audio packet enqueue, and remaining-buffer cleanup.
- `FlashbackEncoderSink.EncodingLoop.cs` owns the background encode loop, bounded video/GPU/audio/microphone drains, encoder PTS resolution, and frame-encoded event dispatch.
- `FlashbackEncoderSink.SegmentRotation.cs` owns rolling segment rotation, active-segment registration, disk-byte refresh after rotation, and rotation-failure recovery.
- `FlashbackEncoderSink.ForceRotate.cs` owns export force-rotate requests, timeout/cancellation classification, pending-request cleanup, and force-rotate drain abort policy.
- `FlashbackEncoderSink.Inputs.cs` owns raw/lease/GPU video enqueue entry points, audio/microphone enqueue entry points, and hot WASAPI writer adapters.
- `FlashbackEncoderSink.Lifetime.cs` owns `StopAsync`, `Dispose`/`DisposeAsync`, deferred cleanup, cancellation/disposal helpers, and stop-drain timeout classification.
- `FlashbackEncoderSink.Recording.cs` owns the `IRecordingSink.StartAsync` adapter, retroactive recording begin/cancel/end lifecycle, recording PTS boundaries, and recording availability checks.
- `FlashbackEncoderSink.RuntimeState.cs` owns public counters, queue-depth/status projections, encoder format summaries, fatal-error callback registration, and the frame-encoded event surface.
- `FlashbackExporter.cs` owns shared native export state and constants.
- `FlashbackExporter.SingleFile.cs` owns the single-file packet-copy/remux core.
- `FlashbackExporter.Segments.cs` owns the multi-segment packet-copy/remux core.
- `FlashbackExporter.Requests.cs` owns public export request routing.
- `FlashbackExporter.Lifetime.cs` owns exporter disposal and cancellation of
  active exports during disposal.
- `FlashbackExporter.Execution.cs` owns export task scheduling, linked cancellation
  wrapper disposal, background thread priority, adaptive throttling, and segment snapshots.
- `FlashbackExporter.PacketTiming.cs` owns packet timestamp normalization, segment
  boundary timestamp repair, packet clone/free helpers, and buffered packet flushes.
- `FlashbackExporter.Streams.cs` owns input/output FFmpeg context setup, stream
  count validation, stream-template copying, and segment stream-layout checks.
- `FlashbackExporter.OutputFiles.cs` owns temp-output validation, atomic
  destination replacement, overwrite policy, and invalid final-output cleanup.
- `FlashbackExporter.Infrastructure.cs` owns export lock/disposal helpers,
  progress normalization/throttling, native cleanup, cancellation sources,
  FFmpeg error strings, time-span timestamp math, and orphan temp cleanup.

Invariants:

- Disable means the timeline should be hidden/locked out.
- Scrub frames must not contaminate live/playback cadence metrics.
- Export must not overwrite without the explicit force path.

## UI Shell And Presentation

Primary current owners:

- `Sussudio/MainWindow.*.cs` for shell, renderer, fullscreen, screenshots,
  animations, and window lifecycle.
- `Sussudio/Controllers/FullScreenController.cs` owns fullscreen transition
  state, overlay reparenting, button state, and auto-hide timer behavior. Keep
  `MainWindow.FullScreen.cs` as the XAML-facing adapter and Flashback shortcut
  bridge.
- `Sussudio/Controllers/WindowScreenshotController.cs` owns automation whole-
  window screenshot dispatch, native PrintWindow capture, and PNG/BMP encoding.
  Keep `MainWindow.Screenshot.cs` as the `IAutomationWindowControl` adapter.
- `Sussudio/Controllers/PreviewScreenshotController.cs` owns the XAML preview-
  frame screenshot button workflow: output directory fallback, file naming,
  preview-frame capture, status text, logging, and button enable/disable state.
  `MainWindow.PreviewScreenshot.cs` is the XAML-facing adapter.
- `Sussudio/Controllers/WindowAutomationController.cs` owns window geometry
  automation plus the recordings-folder command. `MainWindow.WindowAutomation.cs`
  is the `IAutomationWindowControl` adapter; recording-aware close handling
  stays with `MainWindow.CloseLifecycle.cs`.
- `Sussudio/MainWindow.Startup.cs` owns first-load startup, first-frame
  uncloaking, initial ViewModel/device refresh, automation pipe hosting, and
  the launch entrance trigger. Close/finalize behavior stays in
  `MainWindow.CloseLifecycle.cs`.
- `Sussudio/MainWindow.WindowSizing.cs` owns top-level shell resize telemetry
  for preview compositor transforms. `MainWindow.PreviewRenderer.cs` owns
  preview renderer instances, frame counters, expected-present interval, and
  renderer cadence state. `MainWindow.PreviewSurface.cs` owns preview surface
  sizing, GPU panel visibility, and video/control-bar composition shadows.
- `Sussudio/MainWindow.PreviewRuntimeSnapshot.cs` owns the UI-thread automation
  preview snapshot provider that dispatches to the renderer/startup snapshot
  projection, plus the read-only preview runtime snapshot construction.
  Close/finalize behavior stays with `MainWindow.CloseLifecycle.cs`.
- `Sussudio/MainWindow.WindowTitle.cs` owns window title base/build-stamp
  formatting and the recording-time suffix used by property changes.
- `Sussudio/MainWindow.CloseLifecycle.cs` owns `AppWindow.Closing`,
  automation close completion, and recording-aware pre-close protection.
- `Sussudio/MainWindow.ShutdownCleanup.cs` owns `Closed` shutdown cleanup:
  timer stops, event detaches, preview shutdown, automation diagnostics disposal,
  NVML disposal, and ViewModel disposal.
- `Sussudio/MainWindow.NativeWindow.cs` owns native `AppWindow` lookup and DWM
  cloak/dark-mode helpers used by shell startup and automation controllers.
- `Sussudio/MainWindow.Dispatching.cs` owns UI-thread enqueue helpers and
  guarded async event-handler execution used by automation adapters and XAML
  event handlers.
- `Sussudio/Controllers/AudioMeterController.cs` owns audio/microphone meter
  smoothing, timer lifetime, peak/range markers, and meter clip rendering.
  Keep microphone row layout animation in `MainWindow.Bindings.cs` until that
  binding surface is split separately.
- `Sussudio/Controllers/StatsOverlayController.cs` owns stats dock visibility,
  frame-time overlay visibility, polling lifetime, and dock show/hide
  animations. `MainWindow.StatsOverlay.cs` still owns metric text projection
  and snapshot assembly for now.
- `tests/Sussudio.Tests/StatsOverlay.Contract.Tests.cs` owns legacy harness
  contract checks for stats overlay lifecycle wiring, source-telemetry panel
  projection, and diagnostic row pooling.
- `Sussudio/Controllers/StatsDiagnosticRowsController.cs` owns dynamic
  decode/GPU/diagnostic row pools, empty-state rows, group headers, and
  diagnostic row style updates. `MainWindow.StatsOverlay.cs` still owns metric
  text assignment and snapshot assembly for now.
- `Sussudio/MainWindow.FrameTimeOverlay.cs` owns compact frame-time overlay
  text projection and graph line drawing. Keep frame-time canvas math there,
  while `StatsPresentationBuilder` owns the range/sample text policy.
- `tests/Sussudio.Tests/StatsPresentation.Contract.Tests.cs` owns legacy
  harness contract checks for stats presentation and frame-time overlay policy.
  Keep new stats presentation ownership assertions there instead of growing
  `tests/Sussudio.Tests/Program.cs`.
- `Sussudio/MainWindow.StatsHardwareSections.cs` owns decode and GPU stats
  row projection. It should gather current MJPEG/NVML values and delegate row
  element reuse to `StatsDiagnosticRowsController`.
- `Sussudio/Controllers/FlashbackTimelineController.cs` owns Flashback
  timeline visibility, lockout, toggle synchronization, and show/hide
  animation state. `MainWindow.FlashbackTimeline.cs` is the XAML-facing
  adapter; scrub/playback commands remain in `MainWindow.Flashback.cs`.
- `Sussudio/MainWindow.FlashbackScrub.cs` owns active Flashback pointer-scrub
  state, scrub throttling, release/cancel/capture-lost cleanup, and timeline
  fraction/duration geometry helpers used by marker and playhead presentation.
- `Sussudio/MainWindow.FlashbackPlayhead.cs` owns Flashback current-time-
  indicator compositor visuals, magnetic scrub movement, long-horizon linear
  playhead extrapolation, and CTI anchor timing.
- `Sussudio/MainWindow.FlashbackMarkers.cs` owns Flashback marker placement,
  selection-region layout, and compact duration text formatting.
- `Sussudio/Controllers/FlashbackPollingController.cs` owns Flashback status
  and playback-position polling timers. `MainWindow.FlashbackPolling.cs` is the
  XAML-facing adapter; CTI anchor timing stays with playhead motion.
- `Sussudio/Controllers/SettingsShelfController.cs` owns settings shelf
  visibility, the animation gate, and show/hide storyboard construction.
  `MainWindow.SettingsShelf.cs` is the XAML-facing adapter.
- `Sussudio/Controllers/SplashLoadingPhraseController.cs` owns splash phrase
  loading, timer pacing, and two-line text animation. `MainWindow.SplashLoading.cs`
  is the XAML-facing adapter.
- `Sussudio/Controllers/LaunchEntranceAnimationController.cs` owns the splash-
  to-shell launch choreography, initial hidden/scaled shell state, and one-shot
  entrance state. `MainWindow.LaunchEntrance.cs` is the XAML-facing adapter.
- `Sussudio/Controllers/ControlBarAnimationController.cs` owns the control-bar
  button list used by launch entrance animation plus hover/press/release scale
  behavior. `MainWindow.ControlBarAnimations.cs` is the XAML-facing adapter.
- `Sussudio/Controllers/ShellElevationController.cs` owns static shell
  ThemeShadow and translation setup for the control bar and record button.
  `MainWindow.ShellElevation.cs` is the XAML-facing adapter.
- `Sussudio/Controllers/PreviewTransitionAnimationController.cs` owns preview
  shell/content fade and scale transitions, unavailable-placeholder fades, and
  startup/unavailable presentation prep. `MainWindow.PreviewTransitions.cs` is
  the XAML-facing adapter.
- `Sussudio/Controllers/RecordButtonAnimationController.cs` owns the recording
  button circle/pill width morph used by recording state changes.
  `MainWindow.RecordButtonAnimations.cs` is the XAML-facing adapter.
- `Sussudio/Controllers/RecordingButtonActionController.cs` owns the recording
  button command workflow and preview-state logging after a start.
  `MainWindow.RecordingActions.cs` is the XAML-facing adapter.
- `Sussudio/Controllers/LiveSignalInfoController.cs` owns live-signal pill
  visibility state, show/hide debounce timers, and the small scale/fade
  animation. `MainWindow.LiveSignalInfo.cs` is the XAML-facing adapter.
- `Sussudio/Controllers/PreviewAudioFadeController.cs` owns preview-volume
  fade-in/fade-out state, saved target volume, storyboard lifetime, and volume
  save suppression. `MainWindow.PreviewAudioFade.cs` is the XAML-facing adapter.
- `Sussudio/MainWindow.PreviewStartup.cs` owns preview startup state,
  watchdog/telemetry timers, first-visual confirmation, and timeout recovery.
  `MainWindow.PreviewStartupSignals.cs` owns readiness-signal
  collection, missing-signal formatting, and playback-progress diagnostics.
  `MainWindow.PropertyChangedPreview.cs` owns preview-specific ViewModel events
  and property-change projections for preview start/stop/reinit state. Keep
  preview startup fields out of the composition root.
- `Sussudio/MainWindow.PreviewFadeIn.cs` owns delayed reveal after first visual:
  the rendered-frame threshold, fade-in timer, and preview-audio fade start.
  Keep timeout/watchdog recovery in `MainWindow.PreviewStartup.cs`.
- `Sussudio/MainWindow.PreviewStartupOverlay.cs` owns preview-startup loading
  overlay presentation while the app waits for visual confirmation.
- `Sussudio/MainWindow.PropertyChangedRecording.cs` owns recording-specific
  property-change projections for the record button, recording glow, and
  recording-time control lockouts.
- `Sussudio/MainWindow.PropertyChangedFlashback.cs` owns Flashback-specific
  property-change projections for timeline lockout, markers, playhead updates,
  export progress, and settings-control synchronization.
- `Sussudio/MainWindow.PropertyChangedAudio.cs` owns audio and microphone
  property-change projections: audio toggles, monitoring meter state, preview
  volume slider sync, microphone enablement, and microphone volume sync.
- `Sussudio/Controllers/MicrophoneControlsController.cs` owns microphone volume
  slider synchronization, save triggers, shelf enablement, and mic-meter row
  animation state. `MainWindow.MicrophoneControls.cs` is the XAML-facing adapter.
- `Sussudio/Controllers/ResponsiveShellLayoutController.cs` owns control-bar
  label visibility and capture-settings narrow/wide grid placement.
  `MainWindow.ResponsiveShellLayout.cs` is the XAML-facing adapter.
- `Sussudio/Controllers/CaptureSelectionBindingController.cs` owns
  capture/audio/microphone/encoder selection synchronization, collection-change
  debounce, pending-device apply state, and device-audio mode/gain control
  projection. `MainWindow.CaptureSelectionBindings.cs` is the XAML-facing
  adapter.
- `Sussudio/Controllers/CaptureDeviceActionController.cs` owns the capture-
  device refresh/apply button workflows and preserves the explicit apply/reinit
  path. `MainWindow.CaptureDeviceActions.cs` is the XAML-facing adapter.
- `Sussudio/MainWindow.CaptureOptionPresentation.cs` owns presentation-only
  rules for capture option affordances: HDR readiness hints, FPS telemetry
  tooltips, MJPEG decoder count selection/visibility, bitrate mode visibility,
  and audio clipping visibility.
- `Sussudio/Controllers/OutputPathDisplayController.cs` owns recording output-
  path truncation and tooltip updates. `MainWindow.OutputPathDisplay.cs` is the
  XAML-facing adapter used by binding setup and property changes.
- `Sussudio/Controllers/OutputPathActionController.cs` owns recording output-
  path browse/open-recordings button workflows. `MainWindow.OutputPathActions.cs`
  is the XAML-facing adapter.
- `Sussudio/ViewModels/MainViewModel.*.cs` for root presentation state and
  automation-facing compatibility. `MainViewModel.AudioMeters.cs` owns live
  audio/microphone meter callback state; keep callback-thread meter targets
  out of the root facade file. `MainViewModel.AudioRampTrace.cs` owns the audio
  ramp diagnostic ring buffer and sampler; keep preview monitoring call sites
  in `MainViewModel.AudioMonitoring.cs`. `MainViewModel.MicrophoneVolume.cs`
  owns microphone endpoint volume synchronization and persistence.
  `MainViewModel.AudioControls.cs` owns device-native audio mode/gain management.
  `MainViewModel.AudioPropertyChanges.cs` owns audio, microphone, and
  device-audio observable property handlers.
  `MainViewModel.Dispatching.cs` owns shared
  dispatcher enqueue/invoke helpers and preview event fan-out for the partial
  family. `MainViewModel.Runtime.cs` owns live runtime text, timer refreshes,
  recording bitrate display, capture status/error fan-out, and resume cleanup
  callbacks. `MainViewModel.CaptureSettings.cs` owns capture settings
  projection from UI selection and observed runtime/source state.
  `MainViewModel.Capture.cs` owns device initialization, preview start/stop,
  selected-device apply, output-path browsing, and preview reinitialization.
  `MainViewModel.RecordingLifecycle.cs` owns recording toggle serialization,
  graceful stop, emergency stop, and start/stop recording transitions.
  `MainViewModel.RecordingState.cs` owns recording option selections, output
  path, counters, and transition flags.
  `MainViewModel.Disposal.cs` owns bounded teardown, event unsubscription, and
  export-cancellation cleanup.
  `MainViewModel.AutomationSnapshots.cs` owns automation-facing snapshot,
  probe, and options projection. `MainViewModel.FlashbackPlayback.cs` owns
  Flashback playback commands, marker commands, and buffer/bitrate status
  projection. `MainViewModel.FlashbackExport.cs` owns Flashback UI/automation
  export flow, progress/cancellation state, and segment projection.
  `MainViewModel.FrameRateOptions.cs` owns frame-rate option rebuilding,
  source-rate filtering, and automatic frame-rate selection.
  `MainViewModel.FrameRateTiming.cs` owns shared frame-rate timing family,
  rational parsing, source-rate fallback, and preferred-format ranking helpers
  used by frame-rate, resolution, capture-settings, and automation projections.
  Device enumeration and selected-device capability rebuilds stay in
  `MainViewModel.DeviceManagement.cs`.
  `MainViewModel.DeviceFormatProbes.cs` owns late device-format probe
  reconciliation, capability refresh after background probes, and active-preview
  HDR/SDR/session-mismatch retarget checks.
  `MainViewModel.AutoResolutionOptions.cs` owns automatic resolution ranking,
  source-aware auto-selection, and auto-resolved dimension/frame-rate state.
  `MainViewModel.ResolutionSelectionPolicy.cs` owns source-aware, HDR-aware,
  and SDR fallback resolution selection helpers. `MainViewModel.ResolutionOptions.cs`
  owns the resolution dropdown rebuild and effective resolution display/query
  helpers. `MainViewModel.Settings.cs` owns settings load/save and simple
  persistence reactions. `MainViewModel.FlashbackSettings.cs` owns active
  Flashback reactions to recording-format, encoder, buffer, and GPU-decode
  setting changes. `MainViewModel.AutomationUi.cs` owns UI-only automation mutators
  for stats/settings visibility, frame-time overlay display, Flashback timeline
  visibility, show-all capture options, and preview volume persistence.
  `MainViewModel.AutomationCaptureMode.cs` owns automation mutators for
  resolution, frame rate, video format, MJPEG decoder count, and the shared
  reinitialization gate used after active capture-mode changes.
  `MainViewModel.AutomationRecordingSettings.cs` owns recording format,
  encoder preset/quality/split-mode/custom-bitrate, and output-path automation
  mutators. `MainViewModel.RecordingOptionsRefresh.cs` owns startup refresh for
  FFmpeg-backed recording formats and split-encode modes.
  Remaining automation command mutation code stays in `MainViewModel.Automation.cs`.

Refactor direction:

- Keep `MainWindow.xaml.cs` as a shell/composition root over time.
- Prefer named controllers for preview startup, remaining stats projection
  pieces, timeline UI, and other shell behavior that currently lives in
  partials.
- Keep `MainViewModel` as a compatibility facade while moving feature state to
  capture, recording, audio, Flashback, diagnostics, and automation adapters.

## Tooling And Diagnostics

Primary owners:

- `tools/ssctl/` for the preferred CLI.
- `tools/McpServer/` for MCP bridge tools.
- `tools/Common/` for shared tool helpers that are not contracts, including
  pipe client, snapshot formatting, diagnostic sessions, diagnostic scenario
  cataloging, diagnostic-session pipe retry policy, PresentMon probing, and
  shared JSON options.
- `tools/ssctl/CommandHandlers.cs` owns top-level CLI routing and command
  group handlers.
- `tools/ssctl/CommandHandlers.Context.cs` owns the per-invocation command
  context wrapper.
- `tools/ssctl/CommandHandlers.Parsing.cs` owns flag parsing, value parsing,
  usage validation, path/argument joining, and CLI value normalization.
- `tools/ssctl/CommandHandlers.Transport.cs` owns shared command sending and
  response exit-code shaping.
- `tools/Common/DiagnosticSessionModels.cs` owns diagnostic session options,
  result, and sample DTOs. Keep summary/live JSON shape changes there rather
  than expanding the runner header.
- `tools/Common/DiagnosticSessionResultBuilder.cs` owns diagnostic-session
  result analysis and summary JSON construction. Keep `summary.json` field
  shape stable there.
- `tools/Common/DiagnosticSessionSummaryWriter.cs` owns diagnostic-session
  `summary.json` writes and summary-write failure repair of the returned
  result object.
- `tools/Common/DiagnosticSessionResultArtifacts.cs` owns diagnostic-session
  result artifact path construction and pre-summary sample, frame-ledger, and
  timeline artifact writes.
- `tools/Common/DiagnosticSessionJsonArtifacts.cs` owns diagnostic-session JSON
  artifact writing, frame-ledger extraction, and automation response shape
  helpers.
- `tools/Common/DiagnosticSessionRunState.cs` owns diagnostic-session terminal
  exception state, last-stage tracking, live-state breadcrumbs, and
  best-effort artifact write failure recording.
- `tools/Common/DiagnosticSessionOutputLock.cs` owns the per-output-directory
  exclusive lock that prevents concurrent diagnostic sessions from writing the
  same artifact set.
- `tools/Common/DiagnosticSessionBackgroundTasks.cs` owns diagnostic-session
  background task registration, deterministic await/drain order, PresentMon
  task completion, and interrupted-task warning collection.
- `tools/Common/DiagnosticSessionScenarioStartup.cs` owns diagnostic-session
  optional background startup: Flashback scenario task registration, deferred
  recording-settings task registration, and the direct Flashback playback start
  command. Keep task stage names stable there.
- `tools/Common/DiagnosticSessionPresentMonStartup.cs` owns optional PresentMon
  launch, correlation snapshot capture, and `presentmon.csv` output selection
  for diagnostic sessions.
- `tools/Common/DiagnosticSessionScenarioSetup.cs` owns diagnostic-session
  initial state mutations before sampling: enabling or disabling Flashback for
  scenarios, starting preview, starting recording, and waiting for the
  associated readiness conditions.
- `tools/Common/DiagnosticSessionCleanupActions.cs` owns diagnostic-session
  cleanup mutations: recording stop for verification, Flashback playback
  go-live restore, preview stop, and Flashback enable-state restore. Keep
  cleanup stage/action names stable there.
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
- `tools/Common/DiagnosticSessionMetrics.cs` owns read-only projection from
  diagnostic snapshots into session metrics: source cadence, preview cadence,
  visual cadence, D3D slow-frame summaries, playback command health, and
  counter deltas.
- `tools/Common/DiagnosticSessionFlashbackExports.cs` owns Flashback export
  diagnostic helpers: strict export verification payloads, rotated-export
  segment-count parsing, range-selection cleanup, and the audio-toggle
  companion used by the range export audio-switch scenario.
- `tools/Common/DiagnosticSessionFlashbackExportScenarios.cs` owns Flashback
  export diagnostic command flows: concurrent exports, disable-during-export,
  rotated exports, export during playback, and selection-range exports.
- `tools/Common/DiagnosticSessionFlashbackLifecycleScenarios.cs` owns
  Flashback playback disable/re-enable lifecycle diagnostic flow.
- `tools/Common/DiagnosticSessionFlashbackMetrics.cs` owns read-only
  diagnostic-session Flashback metric projection for recording, playback, and
  export sessions, including the playback result fields copied into
  `DiagnosticSessionResult`.
- `tools/Common/DiagnosticSessionFlashbackPreviewCycleScenarios.cs` owns
  Flashback diagnostic preview stop/restart flows for normal Flashback,
  playback, and recording-backed scenarios.
- `tools/Common/DiagnosticSessionFlashbackRejectedExports.cs` owns Flashback
  rejected-export diagnostic scenarios for inactive buffers and active
  Flashback recording backends.
- `tools/Common/DiagnosticSessionFlashbackRecordingSettingsScenarios.cs` owns
  Flashback recording-settings deferral checks and post-stop preset restore.
- `tools/Common/DiagnosticSessionFlashbackSegmentPlaybackScenarios.cs` owns the
  Flashback completed-segment playback scenario and its recording-assisted
  segment-rotation cleanup helper.
- `tools/Common/DiagnosticSessionFlashbackSegments.cs` owns read-only
  diagnostic-session Flashback segment parsing, completed-segment waits, and
  playable-boundary headroom waits. Do not add state-mutating scenario steps
  there.
- `tools/Common/DiagnosticSessionFlashbackStressScenario.cs` owns the
  Flashback stress and scrub-stress command sequences, playback-command
  thresholds, and audio-master fallback classifier shared by stress
  diagnostics.
- `tools/Common/DiagnosticSessionFlashbackWaits.cs` owns read-only snapshot
  polling waits used by Flashback diagnostic scenarios, including playback
  state, playback warmup, preview active, Flashback active, and Flashback
  recording-ready waits.
- `tools/Common/DiagnosticSessionFlashbackValidation.cs` owns Flashback
  diagnostic-session warning policy for recording, playback, and preview
  scheduler metrics.
- `tools/Common/DiagnosticSessionHealthPolicy.cs` owns diagnostic-session health
  severity, warmup filtering, sparse-cadence tolerance, and tolerated Flashback
  warning classification.
- `tools/Common/DiagnosticSessionSampler.cs` owns snapshot sample collection.
  Preserve its ordering: append the cloned sample before running checkpoint
  callbacks.
- `tools/Common/DiagnosticSessionResultFormatter.cs` owns the human-readable
  diagnostic-session text used by ssctl and MCP. Keep
  `DiagnosticSessionRunner.Format(...)` as the stable compatibility wrapper.
- `tools/Common/DiagnosticSessionText.cs` owns shared diagnostic-session text
  helpers used by the runner, formatter, and validation policies.
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
- `tools/Common/PresentMonProbe.Format.cs` owns PresentMon result text
  rendering used by diagnostic-session output surfaces.
- `tools/Common/PresentMonProbe.Csv.cs` owns PresentMon CSV parsing,
  swap-chain selection, app-present correlation, warnings, and metric
  aggregation.
- `tools/Common/PresentMonProbe.cs` owns PresentMon process execution, path
  resolution, command-line construction, and temp CSV cleanup.

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

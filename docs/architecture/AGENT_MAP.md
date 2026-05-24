# Sussudio Agent Map

Last reviewed: 2026-05-21.

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

| Area | Current owners / split families | Preferred next owner |
|------|---------------------|----------------------|
| Diagnostic sessions | `tools/Common/DiagnosticSessionRunner.cs`, `tools/Common/DiagnosticSessionRunContext.cs`, `tools/Common/DiagnosticSessionScenarioPhaseRunner.cs`, `tools/Common/DiagnosticSessionScenarioPhaseModels.cs` | public runner compatibility surface plus phase sequencing, cohesive mutable run context, initial snapshot state, live-state handoff, run context disposal, scenario/completion context construction, post-cleanup completion phase, completion context handoff, result-build request mapping, named scenario phase execution with consolidated context/result/state models, scenario sampling, post-sampling completion order/fault-drain delegation, run bootstrap/options normalization, scenario catalog, startup/cleanup/recording-check/post-run snapshot helpers, result formatter, plus per-scenario runners |
| Offline regression harness | `tests/Sussudio.Tests/Program.cs`, `tests/Sussudio.Tests/HarnessCheckCatalog*.cs` | runner entry point, topic check catalogs, xUnit slices, and focused contract tests such as `StatsPresentation.*.Tests.cs` |
| Capture runtime | `Sussudio/Services/Capture/CaptureService.cs`, `CaptureService.AudioPreviewLifecycle.cs`, `CaptureService.AudioInputSwitching.cs`, `CaptureService.MicrophoneMonitor.cs`, `PreviewAudioGraphResources.cs`, `CaptureRecordingBackendResources.cs`, `CaptureVideoPipelineResources.cs`, `CaptureService.Cleanup.cs`, `CaptureService.Failures.cs`, `CaptureService.FlashbackState.cs`, `CaptureService.FlashbackSettings.cs`, `CaptureService.FlashbackPreviewBackend.cs`, `CaptureService.FlashbackBufferCycle.cs`, `CaptureService.FlashbackExportDiagnostics.cs`, `CaptureService.FlashbackExportFailureClassification.cs`, `CaptureService.FlashbackExportOperations.cs`, `CaptureService.FlashbackExportCore.cs`, `CaptureService.FlashbackExportPlanning.cs`, `CaptureService.FlashbackRecording.cs`, `CaptureService.HealthSnapshots.cs`, `CaptureService.HealthSnapshotAssembler.cs`, `CaptureService.HealthSnapshotFlashbackBackend.cs`, `CaptureService.HealthSnapshotFlashbackPlayback.cs`, `CaptureService.HealthSnapshotRecording.cs`, `CaptureService.PreviewStart.cs`, `CaptureService.PreviewAudioGraph.cs`, `CaptureService.PreviewStop.cs`, `CaptureService.RecordingIntegrity.cs`, `CaptureService.RecordingLifecycle.cs`, `CaptureService.RecordingStartFlashback.cs`, `CaptureService.RecordingStartLibAv.cs`, `CaptureService.RecordingFinalizeFlashbackBackend.cs`, `CaptureService.RecordingFinalizeLibAvBackend.cs`, `CaptureService.RecordingRollback.cs`, `CaptureService.RuntimeSnapshots.cs`, `CaptureService.RuntimeSnapshotAssembler.cs`, `CaptureService.RuntimeSnapshotHdrPipeline.cs`, `CaptureService.RuntimeSnapshotSourceTelemetry.cs`, `CaptureService.Snapshots.cs`, `CaptureService.Telemetry.cs`, `CaptureService.CaptureFormatTelemetry.cs` | service state, construction, public event/property surface, initialization owner, transition transaction/state-sampling owner, and lifecycle guards, audio preview lifecycle/volume/event owner, live audio input switching owner, microphone monitor state/event/disposal/update/restart owner, preview audio resource owner, active recording backend resource owner, video pipeline resource owner, cleanup/disposal and resource-release helper owner, failure callback, failure-telemetry, fatal cleanup, Flashback backend failure cleanup/device-lost owner, Flashback public state, segment access, enable/disable, and restart owner, Flashback settings/buffer/GPU/format/encoder-cycle owner, Flashback preview backend startup/disposal and artifact-cleanup adapter owner, Flashback buffer cycle coordination owner, Flashback export diagnostics/progress/fallback lifecycle and health projection owner, Flashback export failure taxonomy, Flashback export entry/routing and backend snapshot/lock handoff owner, Flashback export core lifetime, request assembly, range-resolution, buffer-position clamps, PTS offset math, and force-rotate preparation owner, Flashback export segment-planning/throttle owner, Flashback recording backend/capability/session-context/frame-rate owner, health snapshot sampler with capture cadence/MJPEG and source telemetry projections, health snapshot DTO assembler and handoff owner, Flashback backend and queue health projection, Flashback playback health projection, recording health orchestration and active backend health projection, preview start/recycle/fast-path/reuse predicates/fresh-pipeline/video-pipeline handoff owner, preview audio graph owner, preview stop/disposal transition owner, read-only automation probe owner, recording integrity active-backend resolver, counter/audio DTO capture, normalized summary input, status/reason evaluation, and integrity logging owner, recording start transition/router, context request assembly, rollback-state holder, and recording outcome-state owner, Flashback recording start owner, Flashback recording backend finalization/export-finalize/boundary snapshot/reconciliation owner, LibAv recording start/video/audio startup owner, recording stop transition/finalization router owner, LibAv recording finalization/video-boundary/sink/idle-preview/preview-restore owner, transient recording rollback owner, runtime snapshot sampler with ingest/audio, reader/transport, and recording-integrity projections, runtime snapshot DTO assembler, runtime HDR/encoder pipeline projection, runtime source-telemetry projection, diagnostics compatibility, read-only automation probes, preview-frame capture waits, and shared snapshot utilities/recording stats/format/observed frames/A/V sync/source telemetry snapshot policy, source telemetry polling/fallback merge owner, capture-format and observed pixel telemetry owner, resource managers |
| App shell | `Sussudio/App.xaml.cs` | XAML partial root, FFmpeg startup check, global handler hookup, recoverable/fatal exception policy plus emergency recording finalization, single-instance guard, startup identity logging, and MainWindow activation |
| Logging | `Sussudio/Logger.cs`, `Sussudio/LoggingJsonContext.cs` | nonblocking log writer state, rotation, channel saturation fallback, direct write path, diagnostics/system evidence, structured snapshot JSON routing, exception formatting, and fatal breadcrumbs; source-generated JSON context for known log payloads |
| Runtime paths | `Sussudio/RuntimePaths.cs` | public cached repo/temp/log path API; repo-root and log-root resolution policy, latest-build fallback, marker discovery, guarded directory creation, and trace fallback diagnostics |
| App project build workflow | `Sussudio/Sussudio.csproj`, `Sussudio/Sussudio.Build.targets` | app identity/assets/packages/runtime config in the project file; publish flags, locale stripping, and latest-build staging in imported targets |
| Device discovery | `Sussudio/Services/Capture/DeviceService.cs`, `DeviceService.FormatCache.cs`, `DeviceService.FormatProbe.cs`, `Sussudio/Services/Capture/MfInteropHelpers.cs`, `Sussudio/Services/Capture/DeviceDiscovery/MfDeviceEnumerator.cs` | device enumeration orchestration, priority/capability scoring, Native XU interface path resolution, audio endpoint association, persisted format cache, inline/background format probing, shared MF startup/attribute helpers and symbolic-link matching, shared MF constants/P/Invokes, MF video device enumeration, WASAPI capture endpoint enumeration, native MF format probing and subtype/FourCC naming, direct/fallback MF source activation |
| Native XU KS bridge | `Sussudio/Services/Capture/NativeXu/KsExtensionUnitNative.cs`, `Sussudio/Services/Capture/NativeXu/NativeXuDeviceSupport.cs` | KS category constants and DTOs, SetupAPI interface enumeration, file-handle open policy, topology node parsing, XU GET/SET transfer helpers, P/Invoke declarations and structs; shared 4K X identity/selected-interface/transport-gate support |
| Capture source reader | `Sussudio/Services/Capture/MfSourceReaderVideoCapture.cs`, `MfSourceReaderVideoCapture.Initialization.cs`, `MfSourceReaderVideoCapture.InitializedSession.cs`, `MfSourceReaderVideoCapture.ReadLoop.cs`, `MfSourceReaderVideoCapture.FrameDelivery.cs`, `MfSourceReaderVideoCapture.RawFrameDelivery.cs`, `MfSourceReaderVideoCapture.Cadence.cs`, `MfSourceReaderVideoCapture.Diagnostics.cs`, `MfSourceReaderVideoCapture.FrameLayout.cs`, `MfSourceReaderVideoCapture.Lifecycle.cs`, `MfSourceReaderVideoCapture.Negotiation.cs`, `MfSourceReaderVideoCapture.DeviceEnumeration.cs`, `MfSourceReaderVideoCapture.Interop.cs`, `MfSourceReaderVideoCapture.ComContracts.cs`, `MfSourceReaderVideoCapture.SampleBufferContracts.cs` | source-reader state and public counters, initialization orchestration and reader construction, actual-output reconciliation and initialized runtime-state commit, Media Foundation read loop, sample-to-frame dispatch, DXGI texture extraction, and dual GPU/CPU delivery orchestration, raw/compressed CPU frame delivery helpers, source cadence metrics, debug-only COM diagnostics, packed YUV frame layout and subtype labels, reader start/stop/dispose lifecycle, direct device opening, native media-type selection, and converted output media-type construction, device-enumeration open fallback and candidate reporting, MF P/Invoke/constants/GUIDs, general Media Foundation COM interface definitions, flattened sample and buffer COM interface definitions |
| Capture fan-out | `Sussudio/Services/Capture/UnifiedVideoCapture.cs`, `UnifiedVideoCapture.FrameIngress.cs`, `UnifiedVideoCapture.Initialization.cs`, `UnifiedVideoCapture.Lifecycle.cs`, `UnifiedVideoCapture.MjpegPipelineLifecycle.cs`, `UnifiedVideoCapture.SinkFanout.cs`, `UnifiedVideoCapture.SinkFanout.Flashback.cs`, `UnifiedVideoCapture.Preview.cs` | public control/config surface, source-reader frame ingress and fatal-error signaling, source-reader/D3D/MJPEG initialization and state commit, shared source-reader start/stop/dispose lifecycle, CPU MJPEG pipeline/jitter lifecycle, recording sink queue fan-out, Flashback sink queue fan-out, diagnostic metric/snapshot projection, preview sink submission and visual-cadence handling |
| Capture cadence trackers | `Sussudio/Services/Capture/FrameFingerprintCadenceTracker.cs`, `Sussudio/Services/Capture/VisualCadenceTracker.cs` | source-packet hash cadence ingestion and duplicate-pattern metrics/statistics; visual-cadence state, frame-ingest orchestration, decoded-frame luma sampling/crop comparison, and metrics DTO construction/statistics/motion-confidence projection |
| Audio capture | `Sussudio/Services/Audio/WasapiAudioCapture.cs`, `WasapiAudioCapture.Initialization.cs`, `WasapiAudioCapture.CaptureLoop.cs`, `WasapiAudioCapture.Fanout.cs`, `WasapiAudioCapture.Conversion.cs`, `WasapiAudioCapture.Diagnostics.cs` | WASAPI state and start/stop/dispose lifecycle, endpoint binding/format negotiation/AudioClient startup, capture thread/packet drain, converted-packet sink/playback/hot writer fan-out, f32le 48 kHz stereo conversion/resampling helpers, and callback/glitch metric projection |
| Audio playback | `Sussudio/Services/Audio/WasapiAudioPlayback.cs`, `WasapiAudioPlayback.Initialization.cs`, `WasapiAudioPlayback.Queue.cs`, `WasapiAudioPlayback.RenderThread.cs`, `WasapiAudioPlayback.Volume.cs` | playback state and start/stop/pause/resume/flush/dispose lifecycle, render endpoint binding/format validation/AudioClient startup, chunk queue and buffered-duration accounting, render-thread callback/prebuffer/buffer-fill execution, volume ramps, and output-level telemetry |
| Audio models | `Sussudio/Models/Audio/AudioModels.cs` | audio endpoint options, meter event args, diagnostic path-mode enum, and audio-ramp trace DTOs kept together as a small audio model leaf surface |
| WASAPI interop | `Sussudio/Services/Audio/WasapiComInterop.cs`, `WasapiComInterop.Formats.cs`, `WasapiComInterop.DeviceClients.cs`, `WasapiComInterop.CoreAudio.Contracts.cs`, `WasapiComInterop.AudioClient.Contracts.cs` | native constants/P/Invokes and COM release helpers, format allocation/parsing, device enumerator and endpoint volume helpers plus AudioClient activation/AudioClient3 initialization, shared audio format/PROPVARIANT structs, Core Audio device/property contracts, and AudioClient/capture/render/endpoint-volume contracts |
| MJPEG preview pacing | `Sussudio/Services/Capture/MjpegPreviewJitterBuffer.cs`, `MjpegPreviewJitterBuffer.FrameIngress.cs`, `MjpegPreviewJitterBuffer.FramePacing.cs`, `MjpegPreviewJitterBuffer.Queue.cs`, `MjpegPreviewJitterBuffer.Adaptive.cs`, `MjpegPreviewJitterBuffer.Metrics.cs` | construction, suppression, disposal lifecycle, paced emit loop control flow, decoded preview-frame ingress and pooled payload ownership, display-clock alignment and renderer submission, queue ordering and reprime recovery, adaptive deadline/depth policy, jitter-buffer metric records and timing sample projection |
| MJPEG decode pipeline | `Sussudio/Services/Gpu/ParallelMjpegDecodePipeline.cs`, `ParallelMjpegDecodePipeline.Workers.cs`, `ParallelMjpegDecodePipeline.CompressedQueue.cs`, `ParallelMjpegDecodePipeline.Reorder.cs`, `ParallelMjpegDecodePipeline.ReorderEmission.cs`, `ParallelMjpegDecodePipeline.Lifecycle.cs`, `ParallelMjpegDecodePipeline.Metrics.cs`, `SoftwareMjpegDecoder.cs`, `SoftwareMjpegDecoder.Decode.cs`, `NvdecMjpegDecoder.Initialization.cs`, `NvdecMjpegDecoder.SharedInitialization.cs`, `NvdecMjpegDecoder.Decode.cs`, `NvdecMjpegDecoder.Download.cs`, `CudaD3D11Interop.Initialization.cs`, `CudaD3D11Interop.Copy.cs`, `CudaD3D11Interop.Native.cs` | pipeline construction/startup sequencing, CPU MJPEG worker decode-loop execution and decoder ownership, compressed input admission/byte budget/depth accounting, decoded-frame ordering and missing-sequence state, decoded-frame emission and preview notification, stop/dispose/shutdown joins/fatal callback signaling, decoder/work-item/reorder-frame resource cleanup, pipeline timing and packet-hash metrics, software MJPEG decoder initialization/lifetime, software MJPEG decode/copy hot path, NVDEC decoder state plus standalone CUDA device/frame-pool initialization, disposal, and error text, NVDEC shared CUDA device/frame-pool adoption, decode/context access, CPU download/copy helpers, CUDA-to-D3D11 bridge state, public texture handles, bridge setup/zero-copy registration, bridge disposal/resource unregister, zero-copy and staging copy behavior, CUDA native constants/P/Invoke declarations |
| GPU telemetry | `Sussudio/Services/Gpu/NvmlMonitor.cs`, `NvmlMonitor.NativeInterop.cs` | optional NVML telemetry snapshot/polling lifecycle and graceful unavailable behavior; raw NVML constants, structs, library loading, device-name helper, and P/Invoke declarations |
| Automation diagnostics | `Sussudio/Services/Automation/AutomationDiagnosticsHub.cs`, `AutomationDiagnosticsHub.Alerts.cs`, `AutomationDiagnosticsHub.DiagnosticEvents.cs`, `AutomationDiagnosticsHub.DiagnosticEvaluationFlashback.cs`, `AutomationDiagnosticsHub.DiagnosticEvaluationRealtime.cs`, `AutomationDiagnosticsHub.DiagnosticEvaluationLanes.cs`, `AutomationDiagnosticsHub.Evaluation.cs`, `AutomationDiagnosticsHub.Snapshots.cs`, `AutomationDiagnosticsHub.SnapshotProjection.cs`, `AutomationDiagnosticsHub.SnapshotProjection.Flattening.AutomationSnapshot.cs`, `AutomationDiagnosticsHub.SnapshotProjection.Audio.cs`, `AutomationDiagnosticsHub.SnapshotProjection.CaptureFormat.cs`, `AutomationDiagnosticsHub.SnapshotProjection.VisualCadence.cs`, `AutomationDiagnosticsHub.SnapshotProjection.Mjpeg.cs`, `AutomationDiagnosticsHub.SnapshotProjection.MjpegPreviewJitter.cs`, `AutomationDiagnosticsHub.SnapshotProjection.FlashbackExport.cs`, `AutomationDiagnosticsHub.SnapshotProjection.FlashbackPlayback.cs`, `AutomationDiagnosticsHub.SnapshotProjection.FlashbackRecording.cs`, `AutomationDiagnosticsHub.SnapshotProjection.PreviewD3D.cs`, `AutomationDiagnosticsHub.SnapshotProjection.PreviewRuntime.cs`, `AutomationDiagnosticsHub.SnapshotProjection.RecordingIntegrity.cs`, `AutomationDiagnosticsHub.SnapshotProjection.RecordingPipeline.cs`, `AutomationDiagnosticsHub.SnapshotProjection.SourceSignal.cs`, `AutomationDiagnosticsHub.SnapshotProjection.UserSettings.cs`, `AutomationDiagnosticsHub.Counters.RealtimePreview.cs`, `AutomationDiagnosticsHub.Timeline.cs`, `AutomationDiagnosticsHub.Verification.cs` | additional collectors/controllers when hub orchestration grows |
| Automation snapshot models | `Sussudio/Models/Automation/AutomationSnapshot.cs`, `AutomationCommandProtocol.cs`, `AutomationOptionsSnapshot.cs`, `AutomationSupportModels.cs`, `CaptureRuntimeSnapshot.cs`, `PerformanceTimelineEntry.cs`, `PreviewRuntimeSnapshot.cs`, `RecordingVerification.cs`, `VideoSourceProbe.cs`, `ViewModelRuntimeSnapshot.cs` | consolidated automation evidence DTO for app/capture/audio/preview/recording/Flashback diagnostics, command protocol DTOs, automation options DTO, support DTOs/enums for diagnostics events, Flashback segments, preview startup, screenshot capture, and window screenshots, consolidated capture runtime DTO surface, consolidated performance timeline DTO surface, consolidated preview runtime DTO surface, recording verification DTOs, video source probe DTOs, and view-model runtime DTO |
| Capture snapshot models | `Sussudio/Models/Capture/CaptureDiagnosticsSnapshot.cs`, `CaptureHealthSnapshot.cs` | consolidated diagnostics core/format/HDR, source telemetry, capture cadence, recording/audio queue, Flashback queue, MJPEG, and visual-cadence fields; consolidated health source/queue/AV-sync and Flashback backend/playback/export health fields |
| Capture leaf models | `Sussudio/Models/Capture/CaptureModels.cs`, `CaptureSettings.cs`, `CaptureSessionTransitionPolicy.cs`, `FrameLedgerModels.cs` | device/options/session-state leaf types, capture settings contract, explicit transition legality policy, and frame-ledger event DTOs kept as the small capture model surface |
| Recording models | `Sussudio/Models/Recording/MediaFormat.cs`, `RecordingModels.cs` | media format display/equality/HDR helper policy kept separate from the consolidated recording leaf DTO/options surface for encoder support, integrity summary, pipeline queue options, and recording byte stats |
| Source telemetry | `Sussudio/Services/Telemetry/NativeXuAtCommandProvider.cs`, `NativeXuAtCommandProvider.InterfaceRead.cs`, `NativeXuAtCommandProvider.AtProtocol.cs`, `NativeXuAtCommandProvider.AudioCommands.cs`, `NativeXuAtCommandProvider.DeviceCommands.cs`, `NativeXuAtCommandProvider.DiagnosticSummary.cs`, `NativeXuAtCommandProvider.FullSnapshot.cs`, `NativeXuAtCommandProvider.PayloadDecoding.cs`, `NativeXuAtCommandProvider.RollingPoll.cs`, `NativeXuAtCommandProvider.RollingCommandGroups.cs`, `NativeXuAtCommandProvider.SnapshotAssembly.cs`, `NativeXuAtCommandProvider.TelemetryDetails.cs` | ReadAsync validation/gating through shared Native XU device support, selected-interface open/topology/node scan and node-read failure classification, AT-command transport/parsing including selector-4 I2C payload writes, public HDMI/Analog audio route and gain command entry points plus HDMI/Analog switch sequence, analog gain register mapping and writes, generic public SET-command surface and generic public read-only AT-command surface, diagnostic summary formatting, reference full-snapshot command acquisition, source payload decoding/scalar helpers, active rolling poll cadence/cache, rolling command group dispatch and per-command cancellation helpers, full/rolling AT-command handoff contract, VIC/frame-rate lookup policy, source snapshot assembly from AT-command results, source telemetry detail row assembly, flash-audio input, analog-gain detail interpretation, audio-origin policy, and AT detail value formatting |
| App service contracts | `Sussudio/Services/Contracts/AutomationInterfaces.cs`, `Sussudio/Services/Contracts/IPreviewFrameSink.cs`, `Sussudio/Services/Contracts/ISourceSignalTelemetryProvider.cs`, `Sussudio/Services/Contracts/RecordingContracts.cs`, `Sussudio/Services/Contracts/PooledVideoFrame.cs` | shared in-process app-service contracts and pooled-frame ownership types; `PooledVideoFrame.cs` owns the reference-counted frame and lease pair; `IPreviewFrameSink.cs` also owns the `PreviewFrameTracking` submit metadata value; keep these separate from `Sussudio.Automation.Contracts` wire/protocol contracts |
| Recording | `Sussudio/Services/Recording/LibAvEncoder.cs`, `LibAvEncoder.Initialization.cs`, `LibAvEncoder.Audio.cs`, `LibAvEncoder.AudioInitialization.cs`, `LibAvEncoder.CodecPolicy.cs`, `LibAvEncoder.AvSync.cs`, `LibAvEncoder.VideoSubmission.cs`, `LibAvEncoder.HardwareSubmission.cs`, `LibAvEncoder.HdrSideData.cs`, `LibAvEncoder.HardwareFrames.cs`, `LibAvEncoder.OutputRotation.cs`, `LibAvEncoder.ResourceCleanup.cs`, `LibAvRecordingSink.cs`, `LibAvRecordingSink.Startup.cs`, `LibAvRecordingSink.StopLifecycle.cs`, `LibAvRecordingSink.Lifetime.cs`, `LibAvRecordingSink.PacketDrain.cs`, `LibAvRecordingSink.Queues.cs`, `LibAvRecordingSink.VideoQueueSubmission.cs`, `LibAvRecordingSink.AudioQueues.cs`, `Sussudio/Services/Recording/Verification/RecordingVerifier.cs`, `Sussudio/Services/Recording/Verification/RecordingVerifier.Ffprobe.cs`, `Sussudio/Services/Recording/Verification/RecordingVerifier.Validation.cs`, `Sussudio/Services/Recording/Verification/RecordingVerifier.Results.cs`, `Sussudio/Services/Recording/Verification/RecordingVerifier.Cadence.cs` | encoder core state plus option/result DTOs, open/error/device-removed diagnostics, encoder runtime/open initialization including option validation and video codec setup, audio/microphone shared state, public audio submission, and drains, audio stream initialization including setup helpers, codec policy, A/V sync diagnostics, video packet drain/write helpers, CPU packed-frame submission and packed software-frame copy helpers, D3D11/CUDA hardware-frame submission, D3D11/CUDA hardware-frame setup, output rotation/reopen/reset plus muxer option policy, final close/trailer/logging plus native resource release/reset, sink state/construction, read-only diagnostics surface, and recording sink encode loop orchestration, recording sink startup shell, encoder option creation, video/GPU/CUDA session queue initialization, metric reset, and video diagnostics reset, recording sink stop/finalize lifecycle and stopped-output validation, dispose/deferred cleanup, recording sink video/GPU/CUDA/audio/microphone packet drains, recording sink video/GPU/CUDA public enqueue adapters plus shared signal/failure/depth helpers, recording sink video/GPU/CUDA queue admission, queue cleanup, pooled packet return helpers, and packet records, recording sink audio queue surface, verifier orchestration/finalizer, ffprobe process work and probe parsing, dimensions/frame-rate/cadence/container/codec/HDR validation policy, result/taxonomy shaping, verifier cadence analysis |
| Flashback | `FlashbackDecoder.cs`, `FlashbackDecoder.D3D11.cs`, `FlashbackDecoder.D3D11Discovery.cs`, `FlashbackDecoder.VideoOutput.cs`, `FlashbackDecoder.VideoConversion.cs`, `FlashbackDecoder.VideoSetup.cs`, `FlashbackDecoder.AudioOutput.cs`, `FlashbackDecoder.Seeking.cs`, `FlashbackDecoder.DecodeLoop.cs`, `FlashbackDecoder.Validation.cs`, `FlashbackDecoder.Lifetime.cs`, `FlashbackPlaybackController.cs`, `FlashbackPlaybackController.DecoderFiles.cs`, `FlashbackPlaybackController.DecoderReopen.cs`, `FlashbackPlaybackController.CommandQueue.cs`, `FlashbackPlaybackController.ThreadCommands.cs`, `FlashbackPlaybackController.ThreadLifecycle.cs`, `FlashbackPlaybackController.AudioRouting.cs`, `FlashbackPlaybackController.AudioMasterPacing.cs`, `FlashbackPlaybackController.PreviewFrames.cs`, `FlashbackPlaybackController.PlaybackSegmentEdges.cs`, `FlashbackPlaybackController.PlaybackTiming.cs`, `FlashbackPlaybackController.Markers.cs`, `FlashbackPlaybackController.Metrics.cs`, `FlashbackEncoderSink.cs`, `FlashbackEncoderSink.Startup.cs`, `FlashbackEncoderSink.EncodingLoop.cs`, `FlashbackEncoderSink.PacketDrain.cs`, `FlashbackEncoderSink.EncodingProgress.cs`, `FlashbackEncoderSink.ForceRotate.cs`, `FlashbackEncoderSink.Inputs.cs`, `FlashbackEncoderSink.Options.cs`, `FlashbackEncoderSink.VideoQueueSubmission.cs`, `FlashbackEncoderSink.Queues.cs`, `FlashbackEncoderSink.Recording.cs`, `FlashbackEncoderSink.RuntimeState.cs`, `FlashbackBufferManager.cs`, `FlashbackBufferManager.LiveAccounting.cs`, `FlashbackBufferManager.Segments.cs`, `FlashbackBufferManager.Lifecycle.cs`, `FlashbackBufferManager.Retention.cs`, `FlashbackBufferManager.Purge.cs`, `FlashbackStartupCacheCleanup.cs`, `FlashbackStartupSessionCacheBudget.cs`, `FlashbackExporter.SingleFile.cs`, `FlashbackExporter.SingleFilePacketReadLoop.cs`, `FlashbackExporter.Segments.cs`, `FlashbackExporter.SegmentPacketWriting.cs`, `FlashbackExporter.SegmentPacketReadLoop.cs`, `FlashbackExporter.SegmentTemplate.cs`, `FlashbackExporter.Lifecycle.cs`, `FlashbackExporter.Execution.cs`, `FlashbackExporter.PacketTiming.cs`, `FlashbackExporter.Streams.cs`, `FlashbackExporter.OutputFiles.cs`, `FlashbackExporter.Validation.cs`, `FlashbackExporter.RuntimePolicy.cs` | decoder lifecycle/open/dispose shell plus state guards, FFmpeg error formatting, and decoded video/audio output DTOs, D3D11 device-context initialization and hardware decoder context setup, D3D11VA decoder discovery and hardware-config diagnostics, video frame output, hardware/software selection, and decoded PTS helpers, software plane copy/conversion kernels, video codec setup and software output-buffer allocation, decoder audio packet delivery and bounded audio output, decoder seek conversion helpers, decoder keyframe/exact seek control flow, decoder video frame receive, packet feed loop, recoverable seek log suppression, and decode phase timing accumulation, decoder stream/frame validation helpers, decoder file-close native cleanup and held-frame release, playback core, decoder file open/identity, decoder cleanup/close telemetry, active fMP4 reopen, seek recovery, and adjacent-segment seek fallback, segment-edge fMP4 reopen and audio gate recovery, component lifecycle, dispose, preview-detach deferred reattach lifecycle, and public playback command facade and consolidated command queue/drop/coalescing/yield/failure/metric surface, command telemetry bookkeeping, consolidated playback-thread lifecycle, playback thread command dequeue/wait loop, playback-thread command dispatch and completion telemetry, playback-thread seek/scrub begin/update command execution, playback-thread end-scrub resume execution, playback-thread play command execution, playback-thread pause command execution, playback-thread nudge command execution, terminal go-live/stop dispatch execution, audio callback/routing/render helpers, audio prebuffer/rewind, audio-master clock/A/V drift projection, audio-master pacing/fallback projections, decoded frame preview submission and validation/byte sizing, seek/scrub keyframe display, seek/scrub decoded-frame display handoff, live playback recovery, continuous playback loop, segment-edge routing/write-head handling, next-segment switch transaction, timing policy, decoded PTS cadence state/telemetry/projection, software-decode budget snap policy, marker command, state, file-PTS, and range owner, position/file-PTS mapping, consolidated playback metrics and seek-cap telemetry, encoder core state/construction, encoder startup transaction, startup queue construction, startup failure rollback, startup validation, queue-capacity policy, startup metric/counter reset, and video diagnostics reset, encode loop orchestration, video/GPU packet drains, audio/microphone packet drains, encoded-frame progress publication plus segment rotation/failure recovery, consolidated export force-rotation status/idle projection, request admission/result classification, lifecycle cleanup, request state machine, and encoding-thread force-rotate execution, video producer input surface, audio/microphone writer input surface, consolidated stop/dispose lifecycle, encoder option/session construction and recording-to-Flashback mapping, file/session helpers, consolidated encoder video/GPU queue admission, lifecycle/input guards, TryWrite depth accounting, and rejection telemetry, shared encoder queue helpers, packet DTOs, pooled packet buffer ownership, video enqueue result classification, best-effort video packet cleanup, GPU texture release helpers, plus queued-buffer cleanup, encoder audio/microphone queue admission and backlog eviction accounting, consolidated recording state/gates, start/rollback, and finalization, public runtime counters, public queue telemetry, public encoder status/format projections, saturated PTS conversion, non-negative byte/duration math, and best-effort eviction resume fallback, buffer core state, shared buffer math and saturated accounting helpers, buffer live byte/PTS accounting updates, buffer segment mutation surface, buffer segment completion/extension, buffer initialize/dispose lifecycle, buffer recovery-preserve markers, buffer segment query helpers, buffer segment path safety, buffer segment status/projection helpers, buffer retention/eviction, buffer purge/delete-all, buffer eviction-pause state and recording PTS range capture, startup stale-root/session cleanup and free-space probing, startup session cache budget enforcement, exporter lifecycle/disposal/native state/native cleanup/lock handling, single-file export shell, single-file packet result validation, single-file active input packet pump, multi-segment export lifecycle, multi-segment packet writing/remux orchestration, active segment packet pump/write state/outcomes plus segment timestamp rebasing/native writes, segment export range/window projection, segment template setup, segment validation policy, temp-file cleanup, export request routing/scheduling, packet timestamp helpers, packet buffer lifetime helpers, stream/context setup, stream-template/layout compatibility, final output replacement, export runtime progress/pacing policy, export failure results, export validation, FFmpeg error formatting |
| Flashback playback command handlers | `FlashbackPlaybackController.ThreadCommands.cs` | playback-thread command dispatch plus seek/scrub begin/update, end-scrub resume, play/pause, nudge/frame-step recovery, and terminal go-live/stop live-restore exits |
| Preview rendering | `D3D11PreviewRenderer.cs`, `D3D11PreviewRenderer.NativeInterop.cs`, `D3D11PreviewRenderer.StopLifecycle.cs`, `D3D11PreviewRenderer.RenderThread.cs`, `D3D11PreviewRenderer.Diagnostics.cs`, `D3D11PreviewRenderer.DxgiFrameStatistics.cs`, `D3D11PreviewRenderer.Submission.cs`, `D3D11PreviewRenderer.Nv12Submission.cs`, `D3D11PreviewRenderer.RenderPasses.cs`, `D3D11PreviewRenderer.ShaderRendering.cs`, `D3D11PreviewRenderer.DeviceInitialization.cs`, `D3D11PreviewRenderer.FrameUpload.cs`, `D3D11PreviewRenderer.Resources.cs`, `D3D11PreviewRenderer.VideoProcessorPipeline.cs`, `D3D11PreviewRenderer.PanelBinding.cs`, `D3D11PreviewRenderer.PendingFrames.cs`, `D3D11PreviewRenderer.Metrics.cs`, `D3D11PreviewRenderer.ScreenshotCapture.cs`, `PreviewScreenshotCapture.cs`, `PreviewPng16Encoder.cs`, `PreviewShaderSources.cs` | renderer public facade, construction, constants, env-tuned runtime configuration, render-thread startup/disposal state, and user-facing state accessors, native panel/shader/DWM interop, stop/unbind/native-call fence lifecycle, render-thread loop/orchestration plus shared-device reset consumption, frame-latency waitable swap-chain state/setup/waits, composition-transform wake handling, pending-frame consumption, renderer diagnostics, render-thread failure telemetry state, first-frame notification state, DXGI frame statistics state and display-clock projection, public raw/lease/single-texture frame submission entry points, dual-plane NV12 submission and HDR transition telemetry, render-pass selection plus VideoProcessor, NV12 shader, and HDR shader execution, viewport/letterbox helpers, and shared present/accounting transaction, shader state, cached shader resources, and shader compilation resources, D3DCompiler blob interop, shared-device COM reference handoff, reinit retirement, reset scheduling, device-lost classification/recovery, D3D device/swap-chain initialization, VideoProcessor input-view resolution, external-texture input-view helpers, raw frame direct/staging upload helpers, D3D object fields, input texture resources, HDR shader input resources, and top-level cleanup orchestration, video-processor pipeline setup/teardown, output-view/RTV reuse, and VideoProcessor color-space updates, swap-chain panel native bind/unbind state, panel size/transform state, pending-frame lifetime, queue/signaling state, read-only present-cadence metrics state/projection, read-only latency/render/wait metrics state/projection, submitted/rendered/dropped frame ownership state and telemetry, expected-frame-rate metric window sizing/reset, renderer metric model types and shared metric sample helpers, render-loop metric window tracking, renderer diagnostics, slow-frame ring/projection/reason classification and DXGI refresh-slip capture, screenshot capture request/result/PNG completion lifecycle, GPU/readback flow, and staging reuse/teardown, preview BMP/PNG pixel analysis and file encoding, HLSL shader sources, timing models |
| Preview render-thread helpers | `D3D11PreviewRenderer.RenderThread.cs` | render-loop shell, shared-device rebind/reset consumption, composition-transform wake handling, queued-frame render dispatch, and final render-thread drain/state reset |
| UI shell | `MainWindow.*.cs` XAML adapters plus `Sussudio/Controllers/*Controller.cs` shell controllers | keep shell adapters thin and start new UI behavior in named controllers/policies with ownership tests |
| Presentation | `MainViewModel.*.cs` facade/feature partial family, `Sussudio/Controllers/ViewModel/MainViewModelControllerGraph.cs`, plus focused `Sussudio/ViewModels` policy/presentation helpers | keep the root facade stable while moving pure feature state, controller graph construction, policy, and presentation logic into named owners |

Preview renderer notes:

- `Sussudio/Services/Preview/D3D11PreviewRenderer.cs` owns the renderer public
  facade plus render-thread startup state, startup reset, and renderer disposal.
  `D3D11PreviewRenderer.StopLifecycle.cs` owns public stop/reinit stop,
  unbind-before-join ordering, native-call drain fencing, render-pass native-call
  entry/exit guards, and pending-frame shutdown cleanup.
- `Sussudio/Services/Preview/D3D11PreviewRenderer.RenderThread.cs` owns the
  render-thread loop shell: MMCSS registration, frame-ready wait, dispatch
  ordering, shared-device reset consumption/rebind, composition-transform wake
  handling, pending-frame consumption/render dispatch, final drain, and renderer
  mode reset.
  `D3D11PreviewRenderer.Diagnostics.cs` owns renderer diagnostics:
  render-thread failure counters, latest failure fields, UI failure
  notification, first-frame reset/UI notification, slow-frame diagnostic
  ring/projection, and slow-frame reason classification.
- `Sussudio/Services/Preview/D3D11PreviewRenderer.RenderPasses.cs` owns
  render-pass selection plus VideoProcessor, NV12 shader, and HDR shader pass
  execution. Keep pass precedence, timing bucket attribution, viewport and
  letterbox helpers, present accounting, HDR fallback logging, native-call guard
  consumption, shader-resource binding, draw calls, and shader-mode present
  messages there.
- `Sussudio/Services/Preview/D3D11PreviewRenderer.Submission.cs` owns raw,
  pooled-lease, and single shared-texture submission entry points.
  `D3D11PreviewRenderer.Nv12Submission.cs` owns dual-plane NV12 submission,
  HDR transition logging, COM reference ownership, and NV12 pending-frame
  construction.
- `Sussudio/Services/Preview/D3D11PreviewRenderer.DeviceInitialization.cs`
  owns `InitializeD3D` orchestration, shared-vs-owned device setup, shared-device
  COM reference handoff/reinit retirement/reset scheduling, device-lost
  classification and recovery, video interface acquisition, media present
  duration setup, initial panel binding, shader compilation handoff, and
  renderer-owned device fallback.
- `Sussudio/Services/Preview/D3D11PreviewRenderer.ShaderRendering.cs` owns
  shader resource/cache state, NV12 SRV reuse, shader bytecode compilation
  orchestration, shader/sampler/viewport constant-buffer creation, and
  compile-fallback logging.
  `D3D11PreviewRenderer.NativeInterop.cs` owns `D3DCompileNative`
  invocation plus `ID3DBlob` byte/error-string extraction.
  `PreviewShaderSources.cs` owns HLSL source strings and renderer mode labels.
- `Sussudio/Services/Preview/D3D11PreviewRenderer.SwapChainInitialization.cs`
  owns composition swap-chain creation, startup dimensions, HDR swap-chain
  capability probing, SDR swap-chain fallback, initial color-space selection,
  and configured output size publication.
- `Sussudio/Services/Preview/D3D11PreviewRenderer.Resources.cs` owns D3D
  device/swap-chain/video-processor fields, VideoProcessor input texture
  resources, HDR shader input resources, and top-level cleanup orchestration.
  `D3D11PreviewRenderer.VideoProcessorPipeline.cs` owns video-processor
  recreation orchestration, processor-resource teardown, output-view/RTV reuse,
  and VideoProcessor input/output color-space updates.
  Family-specific teardown stays next to creation: input texture cleanup in
  `D3D11PreviewRenderer.Resources.cs`, shader/SRV cleanup in
  `D3D11PreviewRenderer.ShaderRendering.cs`, and preview-frame capture staging
  cleanup in `D3D11PreviewRenderer.ScreenshotCapture.cs`.
- `Sussudio/Services/Preview/D3D11PreviewRenderer.PanelBinding.cs` owns native
  SwapChainPanel bind/unbind dispatch, stale-chain guards, panel size,
  rasterization scale, dirty-transform signaling, and swap-chain composition
  matrix updates.
- `Sussudio/Services/Preview/D3D11PreviewRenderer.Metrics.cs` owns present
  cadence, pipeline latency, render CPU timing, frame-latency wait metric state,
  sample tracking, expected-frame-rate window resizing, metric reset/clear
  lifecycle, read-only projections, and recent sample copies.
  renderer metric record structs plus shared ring-copy, timing-summary,
  tick-to-ms, and render-stage validation helpers.
- `Sussudio/Services/Preview/D3D11PreviewRenderer.DxgiFrameStatistics.cs`
  owns DXGI `GetFrameStatistics` sampling, optional DWM flush, counter deltas,
  missed-refresh accounting, visible-frame tick estimation, and
  `IPreviewDisplayClock` snapshot projection.
- `Sussudio/Services/Preview/D3D11PreviewRenderer.PendingFrames.cs` owns pending-frame queue
  control, frame-ready signaling, and `IPreviewFrameQueueControl`.
- `Sussudio/Services/Preview/D3D11PreviewRenderer.Diagnostics.cs` owns recent
  slow-frame snapshot access, thresholding, sample assembly, the slow-frame
  ring writer, slow-frame reason token classification, and the DXGI
  refresh-slip snapshot used by slow-frame diagnostics.
- `Sussudio/Services/Preview/D3D11PreviewRenderer.ScreenshotCapture.cs` owns
  preview-frame capture request state, timeout/cancellation, pending-request
  cleanup, render-thread request exchange, GPU readback, BMP/PNG dispatch before
  present, error result construction, capture-result logging, off-thread PNG
  completion/encode-gate state, and staging-texture reuse/teardown.
- `Sussudio/Services/Preview/PreviewScreenshotCapture.cs` owns preview-frame
  screenshot pixel analysis, mapped-frame buffer copying, BMP capture/header
  writing, and 16-bit PNG frame capture. `Sussudio/Services/Preview/PreviewPng16Encoder.cs`
  owns the PNG container and CRC helpers.

## Automation

Primary owner: `Sussudio.Automation.Contracts/`

Entry points:

- `AutomationCommandKind.cs` owns numeric command IDs. Append only; never
  renumber or reuse values.
- `AutomationCommandCatalog.cs` owns command lookup, canonical name resolution,
  and default metadata helpers. `AutomationCommandCatalog.Entries.cs` owns the
  command metadata table registration orchestration plus grouped core, capture,
  UI, Flashback, and verification metadata rows; keep payload shape, readiness
  gating, timeout policy, CLI help, and MCP descriptions beside the command
  family they describe.
- `AutomationCommandCatalog.Manifest.cs` owns manifest DTO projection and stable
  manifest JSON serialization.
- `AutomationCommandCatalog.PathValidation.cs` owns path-policy types and path
  validation for path-bearing automation commands.
- `AutomationPipeProtocol.cs` owns pipe names, auth env var, manifest revision,
  command resolution, and request envelope shape.
- `AutomationPipeClientModels.cs` owns the pipe command result handoff, pipe
  client exception taxonomy, tolerant response-state parsing, synthetic
  error-envelope factory, exception-to-error-code mapping, and
  throw-vs-synthetic unknown-command policy shared by command transports and
  retry policy.
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
constants, pipe-client handoff DTOs, response-state field parsing, synthetic
automation error envelopes, unknown-command policy, or pipe security policy.

Fast checks:

```powershell
dotnet build Sussudio.slnx -p:Platform=x64 --no-restore
dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore
```

Automation diagnostics ownership:

- `Sussudio/Services/Automation/AutomationCommandDispatcher.cs` owns the
  command envelope, correlation setup, dispatch pipeline shell, and error
  shaping. Construct it with `AutomationViewModelPorts`; this dispatcher root
  should not expose or store the aggregate automation ViewModel dependency.
- `Sussudio/Services/Automation/AutomationCommandDispatcher.Preflight.cs`
  owns manifest revision validation, authentication command handling,
  unauthorized-command rejection, and device-readiness gating.
- `Sussudio/Services/Automation/AutomationCommandDispatcher.PortMappedDispatch.cs`
  owns UI/settings command application, the compatibility no-op for the public
  show-all capture options command, stats-section expand/collapse response
  text, and port-typed trivial-handler dispatch before the custom command
  router.
- `Sussudio/Services/Automation/AutomationCommandDispatcher.CustomCommands.cs`
  owns the custom automation command router for multi-field payloads, special
  response shapes, capture routing, domain command handoff, read-only
  snapshot/manifest/diagnostic/timeline/audio-ramp readback commands,
  verification commands, visual probe/capture commands, and the small
  device-selection, audio-control, capture-control, output-path, and
  recording-enable command bodies it dispatches.
- `Sussudio/Services/Automation/AutomationCommandDispatcher.FlashbackCommands.cs`
  owns Flashback action/export/segment/restart/enable command bodies behind the
  custom command router.
- `Sussudio/Services/Automation/IAutomationViewModel.cs` owns the aggregate
  automation ViewModel contract plus feature-shaped ports for readiness,
  snapshot queries, device selection, capture settings, audio, preview/recording,
  UI, Flashback, and probes. It also owns `AutomationViewModelPorts`, the
  composition-time adapter from the aggregate compatibility contract to named
  port targets. Keep those ports grouped in this file until a consumer needs a
  separate file; do not create many tiny interface files for line-count optics.
  `AutomationCommandDispatcher.Preflight.cs` owns manifest revision, auth-token,
  and readiness gating, `AutomationCommandDispatcher.PortMappedDispatch.cs`
  owns the port-grouped tables and ordered dispatch for UI/settings plus simple
  one-property commands, and `AutomationCommandDispatcher.CustomCommands.cs` consumes the
  device-selection, audio, capture-settings, preview/recording, snapshot-query,
  diagnostics, probe, and window-control ports for custom command bodies,
  including WaitForCondition response shaping, wait-condition polling, and
  snapshot predicates.
- `Sussudio/Services/Automation/AutomationCommandDispatcher.cs` also owns
  shared response shaping, acknowledged responses, and Flashback rejection
  diagnostics for the dispatcher family.
- `Sussudio/Services/Automation/AutomationCommandDispatcher.WindowCommands.cs`
  owns full-screen, recordings-folder, arm-close, and window-action command
  bodies, including close-arm gating and low-level window action execution,
  behind the custom command router.
- `Sussudio/Services/Automation/AutomationCommandDispatcher.Assertions.cs`
  owns AssertSnapshot command response shaping, payload parsing, and snapshot
  comparison helpers.
- `Sussudio/Services/Automation/AutomationCommandDispatcher.Payload.cs` owns
  JSON payload extraction helpers, command metadata lookups, path validation
  forwarding, and enum payload parsing for dispatcher command bodies.
- `Sussudio/Services/Automation/AutomationCommandHandler.cs` owns the shared
  target-typed trivial-handler wrapper used by simple one-property automation
  commands, including the payload field name/type metadata checked against the
  shared automation command catalog.
- `Sussudio/Services/Automation/NamedPipeAutomationServer.cs` owns automation
  pipe constructor/configuration state, server start/stop/dispose, the accept
  loop, per-connection safety/disposal, request-session handoff, error/timeout
  responses, and fallback trace logging.
- `Sussudio/Services/Automation/NamedPipeAutomationServer.ConnectionSession.cs`
  owns per-request JSON framing, client PID logging, dispatch timeouts, late
  dispatch observation, and response writing.
- `Sussudio/Services/Automation/NamedPipeAutomationServer.Security.cs` owns
  Windows pipe security descriptor setup, fallback policy, P/Invoke, and secure
  stream creation.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.cs` owns polling,
  field/constructor state, start/stop/dispose behavior, and the polling loop.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.Counters.RealtimePreview.cs`
  owns preview jitter, MJPEG, D3D, and Flashback recording recent-counter
  baselines and delta updates.
  The hub constructor should take `IAutomationSnapshotQueryPort` directly because
  snapshot refresh and verification are read-only over that port.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.Alerts.cs` owns alert
  rule evaluation, active-alert transitions, signal alert orchestration and rules
  for preview blank/stall/startup/cadence/display 1% low, capture cadence
  drop/1% low, audio muted signal, recording output growth, Flashback alert
  group routing, Flashback recording alert orchestration, export progress/
  force-rotation gap alerts, temp-cache pressure alerts, encoder failure alerts,
  recording path degradation alerts, Flashback playback alert orchestration,
  Flashback playback performance alert routing, and frame-submission failure
  alerts.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.Alerts.cs` owns
  Flashback playback alert orchestration, command queue/failure alerts,
  target-rate/present-cadence/slow-playback/frametime alerts, submit-failure
  alerts, audio-master fallback alerts, and audio-queue backlog alerts.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.DiagnosticEvents.cs`
  owns diagnostics event publication, event throttling, Flashback export
  completion events, and recent event storage.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.Verification.cs` owns
  manual recording/file verification entry points, flashback-export
  verification profile shaping, event publication for explicit verification,
  last-verification snapshot state, post-recording auto-verification gating, and
  background scheduling.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.DiagnosticEvaluationFlashback.cs`
  owns Flashback-specific diagnostic verdict ordering plus Flashback storage
  pressure, active/stalled export, playback command, playback performance,
  frametime, and submission diagnostic verdicts.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.DiagnosticEvaluationFlashback.Recording.cs`
  owns Flashback recording diagnostic verdict ordering plus encoder failure,
  export-rotation gap, backend staleness, recording degradation verdicts, and
  Flashback recording diagnostic condition assembly.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.DiagnosticEvaluationRealtime.cs`
  owns realtime diagnostic verdict ordering plus idle, warmup, recording
  integrity, audio integrity, source/capture cadence, duplicate source-signal,
  MJPEG decode/reorder diagnostic verdicts, realtime preview scheduler,
  renderer pacing, present/display cadence, and preview display 1% low
  diagnostic verdicts.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.DiagnosticEvaluationLanes.cs`
  owns diagnostic lane text orchestration, MJPEG decode lane formatting,
  source cadence/source-signal lane formatting, recording/audio lane formatting,
  preview scheduler/renderer/present/display/visual-cadence lane formatting,
  Flashback recording/export/playback lane formatting, and lane DTOs used by
  diagnostic verdicts.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.Evaluation.cs` owns
  performance scoring, root diagnostic verdict orchestration, final
  healthy/mixed diagnostic fallback, shared alert-detail formatting, and health
  classifiers used by alerts and diagnostic evaluation.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.Hdr.cs` owns HDR truth
  classification from capture pipeline, source-HDR, and verification metadata
  evidence, plus preview HDR input detection, HDR pixel-format helpers used by
  preview state, and tone-map state projection.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.Snapshots.cs` owns
  automation snapshot input projection for preview pacing stage classification.
- `Sussudio/Services/Automation/PreviewPacingSlowStageClassifier.cs` owns the
  preview pacing classifier DTOs plus pure slow-stage classification ordering:
  source capture, visual duplicate/low-motion, MJPEG decode, preview jitter
  scheduler, compositor-miss, renderer-submit, and D3D dominance
  predicates/evidence.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.Snapshots.cs` owns
  public snapshot read/refresh APIs, refresh-gate serialization, core snapshot
  refresh orchestration, cached last-output file existence/size probing,
  process CPU/memory/GC/thread-pool sampling, latest-snapshot publication,
  timeline append, event notification, and auto-verification handoff.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs`
  owns the `BuildAutomationSnapshot` shell, projection-set composition from
  runtime/view-model snapshots and diagnostic classifiers, projection-to-
  flattened-set dispatch, invocation of every focused final-domain flattener,
  the private flattened projection-set handoff, plus live A/V sync drift and
  encoder correction projection and final A/V sync projection-to-
  `AutomationSnapshot` field flattening.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.AutomationSnapshot.cs`
  owns the final `AutomationSnapshot` DTO initializer that flattens named
  projection records into the automation wire snapshot. Keep this final
  `init`-property wire-contract adapter intact unless a deliberate snapshot
  construction pattern exists; do not add mutable setters or shallow fragment
  records just to reduce line count.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs`
  owns root snapshot construction, timestamp/status projection, view-model
  lifecycle/audio flags, verification-in-progress, session state, status-text
  projection, performance score, diagnostic lane, preview pacing classifier,
  performance threshold projection, AV-sync projection, capture command
  projection, and final status/evaluation/AV-sync/capture-command flattening.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Audio.cs`
  owns audio/ingest projection routing, view-model audio peak/clipping and
  detected audio-signal projection inputs, capture-ingest and WASAPI projection
  groups, capture audio/video reader, source-reader and ingest counters, WASAPI
  capture/playback callback, queue, gap, glitch, and latency projection, final
  audio/ingest/source-reader/WASAPI projection-to-`AutomationSnapshot`
  flattening, audio drop counter projection, derived real-time/file-writer drop
  totals, and final audio-drop projection-to-`AutomationSnapshot` field
  flattening.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs`
  owns snapshot construction routing, AV-sync projection/flattening, capture
  session command queue counters, latency, last-command, last-error projection
  inputs consumed by `AutomationSnapshot`, and final capture-command
  projection-to-`AutomationSnapshot` field flattening.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.CaptureFormat.cs`
  owns capture-format projection routing and groups requested, HDR-request,
  actual, negotiated, reader-observation, and encoder format modules consumed
  by `AutomationSnapshot`, plus HDR activation/auto-downgrade projection,
  actual capture dimensions/frame-rate projection, requested capture
  format/quality/HDR toggle/audio toggle, negotiated capture
  dimensions/frame-rate/pixel format, source-reader subtype and observed
  pixel/surface format projection inputs, encoder format/codec/profile and
  ten-bit confirmation projection, capture memory preference, requested/
  negotiated video subtype, frame-ledger projection, final capture-format
  flattening, and final capture-transport projection-to-`AutomationSnapshot`
  field flattening.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.VisualCadence.cs`
  owns source capture cadence, preview visual cadence, and center-crop visual
  cadence projection inputs consumed by `AutomationSnapshot`, plus final source
  capture cadence, visual cadence, and center-crop visual cadence projection-to-
  `AutomationSnapshot` field flattening.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Mjpeg.cs`
  owns CPU MJPEG totals, compressed queue, failure,
  decode/interop-copy/callback/reorder/pipeline timing, decoder count,
  per-decoder, and packet duplicate-run / unique-frame projection inputs
  consumed by `AutomationSnapshot`, plus final CPU MJPEG totals, compressed
  queue, timing, and packet-hash field flattening.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.MjpegPreviewJitter.cs`
  owns MJPEG preview jitter projection routing, queue counters, timing samples,
  adaptive drop/depth counters, last scheduler event projection, and final
  projection-to-`AutomationSnapshot` flattening.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.FlashbackExport.cs`
  owns active Flashback export progress, failure, force-rotate fallback, and
  final Flashback export last-result projection consumed by
  `AutomationSnapshot`, plus final Flashback export projection-to-
  `AutomationSnapshot` field flattening.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.FlashbackRecording.cs`
  owns Flashback recording failure, cleanup, force-rotate,
  temp-drive/startup-cache, active output/runtime, backend settings drift,
  export-verification, codec downgrade, encoder identity/bitrate/dimensions/
  frame-rate, focused projection routing, and final projection-to-
  `AutomationSnapshot` flattening.
  It also owns Flashback video, GPU, and audio queue/backpressure projection plus
  flattened queue/backpressure fields consumed by `AutomationSnapshot`.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.FlashbackPlayback.cs`
  owns Flashback playback state/frame summary, audio-master delay/fallback
  projection, playback event/cadence/PTS-cadence/A/V drift projection,
  seek-cap/decode timing projection, playback command queue projection, and
  final flattened playback fields consumed by `AutomationSnapshot`.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.PreviewD3D.cs`
  owns D3D preview swap-chain and renderer-state projection plus composition of
  D3D leaf projections consumed by `AutomationSnapshot`, plus final D3D
  projection-to-`AutomationSnapshot` flattening, renderer-state fields, and D3D
  pipeline-latency projection, waitable frame-latency projection, and DXGI
  frame-statistics projection including recent missed-refresh and stats failure
  deltas, D3D CPU upload/render/present/total-frame timing, and submitted/
  rendered/dropped frame ownership plus recent slow-frame projection consumed by
  `AutomationSnapshot`.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.PreviewRuntime.cs`
  owns preview runtime projection routing, preview frame counters, estimated
  pipeline latency, preview surface visibility, renderer attachment, GPU
  playback state/position, preview HDR/tone-map/color metadata, and the frame,
  cadence, surface, startup, GPU-playback, and color groups consumed by
  `AutomationSnapshot`, plus preview display-cadence projection inputs, preview
  startup/readiness and renderer mode projection inputs, and final preview
  runtime flattening.
  It also owns process memory, CPU, GC, and thread-pool projection consumed by
  `AutomationSnapshot`, plus final process resource
  projection-to-`AutomationSnapshot` field flattening.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.RecordingIntegrity.cs`
  owns recording-integrity projection routing, status/reason, video-frame
  counters, queue/backpressure, audio integrity, A/V sync projection inputs, and
  final flattening consumed by `AutomationSnapshot`.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.RecordingPipeline.cs`
  owns recording-pipeline projection routing, encoder queue age/count/failure
  health, conversion/ffmpeg/video ingest queue health, recording video queue
  latency, backpressure, encoder-output health, GPU/CUDA queue health, recording
  backend/audio-path/mux-result projection, recording UI output text,
  accumulated recording bytes, file-growth state, last finalized output
  metadata, last verification result projection consumed by `AutomationSnapshot`,
  and final recording pipeline/backend/output projection-to-`AutomationSnapshot`
  field flattening.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.SourceSignal.cs`
  owns detected source frame-rate fallback, source dimensions/HDR, raw source
  signal metadata projection, source telemetry fallback policy, age calculation,
  source-target summary inputs, final source projection flattening orchestration,
  source dimensions, frame-rate, HDR, video/audio format, firmware, input, USB,
  HDCP, raw timing field flattening, and final source telemetry availability,
  confidence, detail, age, backend, suppression, circuit-state, summary, and
  target-summary field flattening consumed by `AutomationSnapshot`.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.UserSettings.cs`
  owns selected device, selected capture/recording options, preview volume, and
  stats visibility projection consumed by `AutomationSnapshot`, plus final
  selected device, selected capture/recording options, preview volume, and stats
  visibility projection-to-`AutomationSnapshot` field flattening.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.Hdr.cs` owns HDR truth
  classification from capture runtime, UI state, and recording verification plus
  HDR availability/request state, runtime/readiness fallback, HDR
  warmup/downgrade, pipeline parity, telemetry-alignment, and HDR truth verdict
  projection plus final HDR pipeline projection-to-`AutomationSnapshot` field
  flattening.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.Snapshots.cs` owns
  stateful snapshot bookkeeping for audio mute suspicion and recording file
  growth tracking.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.Timeline.cs` owns
  performance-timeline ring reads, append mechanics, final `AutomationSnapshot`
  to `PerformanceTimelineEntry` assignment, timestamp, observed capture/preview
  FPS, encoder video queue depth/drop, capture cadence, process, memory, GC,
  thread-pool, pipeline-latency, Flashback export progress, force-rotate
  fallback, preview cadence, visual cadence, MJPEG packet/jitter, D3D preview,
  preview-pacing, Flashback playback timeline projection composition, grouped
  handoff, playback cadence, decode timing, command queue/coalescing,
  audio-master fallback, playback stage/failure, backend settings, queue reject,
  cleanup, and force-rotate timeline projection.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.Verification.cs` owns
  manual recording/file verification commands, verification-profile adaptation,
  explicit verification events, automatic post-recording verification
  scheduling, and recording-start verification reset.
## Capture Runtime

Primary current owner: `Sussudio/Services/Capture/`

Important entry points:

- `CaptureSessionCoordinator.cs` owns construction, shared state fields, the
  public non-Flashback lifecycle/audio command facade into the serialized
  worker, and queue/session snapshot projection.
- `CaptureSessionCoordinator.Models.cs` owns command enums, queue receipts,
  session snapshots, and Flashback playback/buffer status projections.
- `CaptureSessionCoordinator.Queue.cs` owns work-item creation, command
  enqueueing, enqueue-failure handling, disposed-state ingress guards,
  worker-loop execution, command coalescing, operation cancellation/failure
  accounting, pending-command failure drain, and pending-command counter
  decrement policy.
- `CaptureSessionCoordinator.Disposal.cs` owns dispose/drain/cancel lifecycle
  for the worker queue and cancellation token source.
- `CaptureSessionCoordinator.Flashback.cs` owns queued Flashback mutations,
  read-only Flashback status, playback snapshot projection, Flashback export
  and segment query forwarding, playback/scrub/marker/go-live command adapters,
  and active playback-controller readiness checks and rejection logging.
- `CaptureSessionTransitionPolicy.cs` owns pure transition legality and
  steady-state resolution for `CaptureService`;
  `Sussudio/Services/Capture/CaptureSessionStateMachine.cs` owns mutable
  session state, transition generation, and state mutation methods used by
  normal transitions, cleanup, disposal, and fatal cleanup.
- `DeviceService.cs` owns capture/audio device enumeration orchestration, the
  combined discovery result used by startup refresh, discovery summary state,
  device priority/capability scoring, audio endpoint association, and native XU
  interface path resolution for supported devices.
- `DeviceService.FormatCache.cs` owns persisted format-cache DTOs and
  load/save/delete helpers.
- `DeviceService.FormatProbe.cs` owns inline/background Media Foundation format
  probing and pixel-format/frame-rate normalization.
- `Sussudio/Services/Capture/NativeXu/NativeXuDeviceSupport.cs` owns supported
  4K X VID/PID recognition, selected-interface projection, and the shared
  native XU transport gate used by telemetry, audio controls, discovery, and
  NativeXuAudioProbe linked-source builds.
- `Sussudio/Services/Capture/DeviceDiscovery/MfDeviceEnumerator.cs` owns shared Media Foundation constants, GUIDs,
  P/Invoke declarations, native MF video-device enumeration, WASAPI capture
  endpoint enumeration and friendly-name reads, native video format probing,
  subtype/FourCC naming, direct symbolic-link MF source activation, and
  enumeration fallback.
- `CaptureService.cs` owns shared service state, construction, the
  event/property surface, and the public initialization transition with initial
  selected device/settings capture, negotiated-format seeding, observed-pixel
  telemetry reset, fallback source telemetry, telemetry refresh, NTSC
  frame-rate correction, and initialized status event. It should not receive
  unrelated UI, Flashback, recording lifecycle, or diagnostics behavior.
- `CaptureService.AudioPreviewLifecycle.cs` owns preview volume/mute commands,
  WASAPI audio-level and capture-failure event projection, audio-preview
  start/stop lifecycle, late WASAPI capture startup, playback start, preview
  rollback, and optional capture teardown.
- `PreviewAudioGraphResources.cs` owns live program WASAPI capture, microphone
  capture, playback startup/shutdown, audio-monitor attach/detach order, preview
  volume/mute application, playback best-effort cleanup helpers, and
  capture-fault telemetry. CaptureService callers use this aggregate directly
  instead of private root shims for audio preview resources.
- `CaptureRecordingBackendResources.cs` owns active recording backend resources:
  LibAv/Flashback sink identity, recording context/settings snapshot, pending
  LibAv drain task tracking/reentry policy, and explicit install/detach/clear
  operations used directly by recording start, finalization, rollback, snapshot,
  and cleanup paths without root `CaptureService` shim properties.
- `CaptureService.AudioInputSwitching.cs` owns live audio input switching, the
  committed old/new capture handoff, Flashback audio attach, and deferred
  cancellation checks.
- `CaptureService.MicrophoneMonitor.cs` owns microphone-monitor shared state,
  mic-level event projection, mic writer-detach/disposal cleanup, the public
  monitor update transaction, preview-time Flashback mic writer attachment,
  and post-recording mic monitor restart/reattachment.
- `CaptureService.Cleanup.cs` owns explicit cleanup transitions,
  disposal-triggered cleanup, dispose flow, app shutdown teardown, Flashback
  segment preservation when cleanup finalization fails, calls to root
  cleanup and disposed-state helpers, best-effort semaphore
  release/disposal, coordination-lock disposal, Flashback backend/export
  held-lock release helpers, and Flashback eviction resume warnings.
- `CaptureService.cs` owns transition serialization,
  transition-state entry, steady-state input sampling and resolution, fault
  publication, transition-lock release, cleanup/disposal state helpers,
  current-state projection, public initialization, and initialization/disposal guards.
- `CaptureService.Failures.cs` owns fatal capture/recording/Flashback backend
  failure callbacks, fatal capture cleanup launch, Flashback backend cleanup
  launch, GPU device-lost classification, recovery segment preservation,
  generation-stale guards, last-failure telemetry state fields, lock, mutation
  helpers, clear helpers, and snapshot reads. It routes cleaning-up/faulted
  transitions through root CaptureService transition helpers and must not write
  session state directly.
- `CaptureService.FlashbackState.cs` owns Flashback public state, segment
  access, enable/disable transition gating, restart entry points, and committed
  restart orchestration after preview backend teardown.
- `CaptureService.FlashbackSettings.cs` owns Flashback buffer/GPU settings
  updates, live playback-controller GPU decode propagation, recording-format
  changes, active encoding-setting application, encoder-setting cycles, and
  rollback after failed Flashback buffer cycles.
- `CaptureService.FlashbackRecording.cs` owns Flashback recording backend ownership checks,
  WASAPI and microphone input restoration for Flashback preview/recording
  backends, audio attachment, frame-encoded fan-out, recording topology
  validation, and Flashback session context construction.
- `CaptureService.FlashbackPreviewBackend.cs` owns Flashback preview backend
  transition coordination: AV1 encoder support probing, video/audio readiness
  waiting, resource-owner request construction, deferred cleanup handoff,
  artifact-cleanup export-lock delegation, teardown lock ordering, purge-policy
  resolution, service callback binding, cancellation-token choice, and
  preview backend disposal request construction.
  Startup construction, install, playback initialization, and rollback cleanup live in
  `FlashbackBackendResources.Startup.cs`; producer attach/detach request
  contracts and feed wiring live in `FlashbackBackendResources.cs`;
  teardown mechanics and backend artifact cleanup live in
  `FlashbackBackendResources.Teardown.cs`.
- `CaptureService.FlashbackBufferCycle.cs` owns buffer-cycle transition
  coordination: backend/export lock ordering, purge-preserve decisions, and
  full rebuild fallbacks. Sink-only resource mechanics live in
  `FlashbackBackendResources.BufferCycle.cs`: playback disposal, old-sink
  stop/dispose, replacement sink startup, playback restore, and failed
  replacement cleanup.
- `CaptureService.FlashbackExportDiagnostics.cs` owns Flashback export attempt
  lifecycle, result, rejection, completion diagnostic state, progress
  forwarding/normalization, force-rotate fallback counters, locked diagnostic
  field copy, elapsed/progress-age/file-length helpers, and derived
  progress/throughput projection used by health snapshots.
- `CaptureService.FlashbackExportFailureClassification.cs` owns the export
  failure-kind taxonomy shared by capture diagnostics and automation responses.
- `CaptureService.FlashbackExportOperations.cs` owns Flashback export entry
  points, lock-scoped backend snapshotting, session/backend lock release before
  native export, and routing into range-resolution and shared core owners.
- `CaptureService.FlashbackExportCore.cs` owns range and last-N
  post-eviction range resolution, buffer position clamps, and PTS offset math.
- `CaptureService.FlashbackExportCore.cs` owns the shared export lifetime:
  export-operation locking, eviction pause/resume, diagnostics completion,
  exporter execution, active-file fallback, `FlashbackExportRequest`
  construction, throttle-provider wiring, partial-fallback result marking,
  cleanup, and live-edge force-rotate export preparation including
  failure/committed-pending outcomes, timeout fallback segment discovery, and
  related diagnostics/logging.
- `CaptureService.FlashbackExportPlanning.cs` owns segment metadata mapping,
  live-export throttle policy, segment path normalization, and segment PTS
  timestamp repair.
- `CaptureService.FlashbackRecording.cs` owns Flashback recording backend
  ownership checks, audio attachment, encoded-frame forwarding, and recording
  topology validation, Flashback recording session-context construction, codec
  selection, GPU handle handoff, HDR guardrails, delivered-cadence frame-rate
  rational preservation/inference, and legacy Flashback export
  verification/downgrade snapshot fields.
- `CaptureService.HealthSnapshots.cs` samples health snapshot field groups,
  invokes the focused field builders, and populates the final
  service-state/scalar handoff consumed by diagnostics and automation health
  checks.
- `CaptureService.HealthSnapshots.cs` owns the read-only health snapshot
  sampler, including source-cadence metric projection, MJPEG timing, preview
  jitter, visual cadence, packet hash, per-decoder projection, and the matching
  health field records.
- `CaptureService.HealthSnapshotAssembler.cs` owns final
  pure `CaptureHealthSnapshot` DTO construction from captured fields and the
  private assembly handoff record consumed by that map. Keep this
  allocation-neutral `init`-property map intact unless a deliberate snapshot
  construction pattern exists; sampling and domain projection belong in the
  focused health snapshot partials, not in post-construction mutators.
- `CaptureService.HealthSnapshotFlashbackBackend.cs` owns Flashback buffer,
  startup-cache, backend-staleness reason policy, encoder summary, live
  Flashback audio/video queue, force-rotate, backpressure, and GPU queue field
  projection for health snapshots.
- `CaptureService.HealthSnapshots.cs` also owns source telemetry backend,
  suppression, and circuit-state field projection for health snapshots.
- `CaptureService.HealthSnapshotFlashbackPlayback.cs` owns Flashback playback
  health snapshot orchestration, the aggregate playback health field record,
  state/frame/segment/PTS/seek-cap/submit-failure/A/V drift sampling, playback
  cadence metric sampling, decode timing and max-phase metric sampling,
  audio-master pacing/fallback sampling, playback command telemetry sampling,
  and each matching private field record.
- `CaptureService.HealthSnapshotRecording.cs` owns recording health snapshot
  orchestration, LibAv-only CUDA queue projection, active recording backend
  selection, LibAv-vs-Flashback sink fallback, failure precedence,
  backend-specific queue/counter normalization, and the
  `RecordingHealthSnapshotFields` handoff.
- `CaptureService.PreviewStart.cs` owns the video-preview start transition
  entry point and sequencing, preview pipeline and Flashback backend recycle
  decisions before start, retained-backend fast-path reattachment, reuse
  predicates and capture-settings cloning, fresh UVC startup, preview-start
  rollback, and fresh preview backend startup ordering.
- `CaptureService.PreviewAudioGraph.cs` owns preview WASAPI capture startup,
  video-only audio fallback logging, preview playback attach, preview-time
  microphone monitor startup, and partially-started audio rollback.
- `CaptureService.PreviewStop.cs` owns video-preview stop transitions,
  keep-pipeline-alive detach semantics, stopped-state/telemetry commit, preview
  pipeline disposal ordering, Flashback backend disposal, WASAPI disposal, and
  microphone cleanup.
- `CaptureVideoPipelineResources.cs` owns active unified-video capture storage,
  preview-frame sink storage, negotiated video getters, and cached MJPEG
  pipeline timing snapshots, plus deferred unified-video cleanup after LibAv
  drains. CaptureService callers use this aggregate directly instead of private
  root shims for the active capture and preview sink.
- `CaptureService.PreviewStart.cs` owns preview frame sink attachment, shared
  D3D preview-device handoff, unified-video fatal/pixel callback attach/detach,
  and preview start/reuse/fresh-pipeline orchestration.
- `CaptureService.Snapshots.cs` owns read-only automation probes,
  preview-frame capture waits, and diagnostics compatibility snapshot helpers.
- `CaptureService.RecordingIntegrity.cs` owns active recording integrity backend
  resolution; `.Models.cs` owns private counter DTOs; `.Summary.cs` owns
  final `RecordingIntegritySummary` DTO construction and structured
  `RECORDING_INTEGRITY` log rendering, normalized video/audio summary handoff
  fields, status, reason, and audio-status classification; `.Counters.cs` owns
  video/backend counter capture and baseline deltas; `.Audio.cs` owns audio
  counter capture and baseline deltas.
- `CaptureService.RecordingLifecycle.cs` owns public recording start
  transition routing, recording output-folder resolution, LibAv and Flashback
  `RecordingContextRequest` assembly, the private rollback-state holder, and
  delegation of failed-start cleanup to the rollback owner.
  `CaptureService.RecordingStartFlashback.cs` owns Flashback recording backend
  startup and fast-path reuse; `CaptureService.RecordingStartLibAv.cs` owns
  standard LibAv recording startup sequencing, video-capture reuse/creation,
  source-reader compatibility checks, preview sink/shared-device handoff, video
  pipeline installation, audio-input startup, WASAPI sink attachment, preview
  playback preservation, and recording microphone capture wiring.
  `CaptureService.RecordingLifecycle.cs`
  owns normal and emergency recording stop transition routing plus the
  stop/finalize router for active Flashback and LibAv backends.
- `CaptureService.RecordingFinalizeFlashbackBackend.cs` owns active Flashback
  recording backend finalization: live-edge finalize/export handoff,
  finalize-in-progress choreography, Flashback recording integrity summaries,
  cancellation-result classification, post-finalize backend reconciliation,
  failed-finalize recovery preservation, deferred settings apply, buffer
  cycling, buffer-cycle failure classification, outcome publication, backend
  cleanup launch, and Flashback-specific microphone monitor restart.
- `CaptureService.RecordingFinalizeLibAvBackend.cs` owns standard LibAv
  recording finalization sequencing, unified-video recording stop,
  source-reader boundary diagnostics, WASAPI recording sink detach, microphone
  capture disposal before sink stop, LibAv sink normal/emergency stop, sink
  disposal, LibAv drain task tracking, inactive-preview teardown after
  recording, audio-fault folding, encoder/runtime and recording-integrity
  summaries, final state completion, pending Flashback enable-after-recording
  detection, guarded Flashback preview backend restore, failed-restore rollback
  and purge, standard post-recording microphone monitor restart,
  `FLASHBACK_ENABLE_AFTER_RECORDING_*` breadcrumbs, preview-restore ordering,
  and the visible final outcome publication before delayed cancellation throws.
- `CaptureService.RecordingLifecycle.cs` also owns publication of the last
  recording output path, finalize status, finalize timestamp, and preserved
  artifact fields for both recording-start and recording-finalize outcomes.
- `CaptureService.RecordingFinalizeFlashbackBackend.cs` also owns Flashback
  recording export finalization, cancellation-result classification, and
  live-edge boundary snapshots, including idempotent
  `EndFlashbackRecordingAccounting()` calls, source-frame counters, recording
  integrity counters, and audio integrity counters.
- `CaptureService.RecordingRollback.cs` owns transient backend teardown after
  recording-start failures, including the failure log/last-failure update,
  Flashback rollback accounting, rollback artifact cleanup, best-effort sink,
  WASAPI, unified-video, and deferred LibAv drain cleanup.
- `CaptureService.RuntimeSnapshots.cs` samples runtime snapshot inputs consumed by UI,
  automation, and verification, owns video ingest/source-reader/WASAPI playback
  and reader/transport projections, recording-integrity summary projection, and
  their private handoff models, then delegates final DTO construction.
- `CaptureService.RuntimeSnapshotAssembler.cs` owns final `CaptureRuntimeSnapshot` DTO construction
  from already-sampled field groups and the private runtime snapshot assembly
  handoff contract consumed by that map.
- `CaptureService.RuntimeSnapshotHdrPipeline.cs` owns runtime HDR/encoder
  pipeline parity, downgrade reason, encoder format projection, and HDR warmup
  state/count projection/classification, and its private HDR pipeline/warmup handoff models.
- `CaptureService.RuntimeSnapshotSourceTelemetry.cs` owns runtime snapshot
  projection for source telemetry details, frame-rate origin, age, request
  alignment, and its private source-telemetry handoff model.
- `CaptureService.Snapshots.cs` owns diagnostics-snapshot compatibility,
  shared tick-age snapshot helper policy, recording byte-count projection,
  recording-format labels, observed frame-format telemetry projection, A/V sync
  drift state/health fields, and source telemetry backend/suppression/circuit
  policy shared by runtime and health projections.
- `Sussudio/Services/Capture/CaptureService.Telemetry.cs` owns source telemetry
  polling, provider reads, fallback snapshot construction, and merge policy.
- `CaptureService.CaptureFormatTelemetry.cs` owns capture-format runtime
  telemetry, observed pixel-format normalization/reset/counters, NTSC
  frame-rate correction, and frame-rate argument formatting.
- `UnifiedVideoCapture.cs` owns public control/configuration surface, capture
  fields, counters, and recording/Flashback attachment state.
- `UnifiedVideoCapture.FrameIngress.cs` owns source-reader frame arrival
  routing, MJPEG decoded-frame emission fan-out, capture-arrival ledger
  records, pixel-format observer dispatch, and fatal-error dedupe/signaling.
- `UnifiedVideoCapture.Initialization.cs` owns source-reader/D3D/MJPEG
  initialization and committed runtime state reset.
- `UnifiedVideoCapture.Lifecycle.cs` owns read-loop start/stop, preview-reinit
  disposal, and capture fatal-error callbacks.
- `UnifiedVideoCapture.MjpegPipelineLifecycle.cs` owns CPU MJPEG decode
  pipeline construction, preview jitter buffer setup/disposal, MJPEG pipeline
  stop/retention semantics, and MJPEG fatal-error callbacks.
- `UnifiedVideoCapture.SinkFanout.cs` owns recording sink enqueue helpers,
  recording non-blocking queue rejection accounting, and legacy recording
  encoder fallback adapters. `UnifiedVideoCapture.SinkFanout.Flashback.cs`
  owns Flashback sink enqueue helpers, Flashback queue rejection accounting, and
  Flashback recording sequence-gap accounting.
- `UnifiedVideoCapture.cs` also owns source-reader cadence forwarding, MJPEG
  pipeline/jitter/hash metrics, preview visual cadence metrics, and frame-ledger
  summary projection over the root capture fan-out state.
- `UnifiedVideoCapture.Preview.cs` owns preview sink assignment, live-preview
  suppression drains, MJPEG decoded preview-frame routing, raw preview
  submission, and visual-cadence reset/recording helpers.
- `FrameFingerprintCadenceTracker.cs` owns source-packet hash cadence ingestion,
  duplicate-run counters, fast packet hashing, duplicate-pattern metrics DTO
  construction, interval statistics, unique-interval projection, and pattern
  labels.
- `VisualCadenceTracker.cs` owns visual-cadence state, reset, frame validation,
  output/change ingestion, repeat-run bookkeeping, decoded-frame luma sampling,
  crop selection, sample-buffer promotion, rolling sample writes, stopwatch
  elapsed-time conversion, metrics DTOs, snapshot construction,
  delta/output/change statistics, and motion-confidence labels.
- `ParallelMjpegDecodePipeline.cs` owns construction, callback storage, channel
  creation, and startup sequencing.
- `ParallelMjpegDecodePipeline.Workers.cs` owns CPU MJPEG worker thread
  creation/naming, decoder array ownership, worker decode-loop execution, and
  worker liveness checks.
- `ParallelMjpegDecodePipeline.CompressedQueue.cs` owns compressed input
  admission, startup invalid-MJPG drops, byte-budget rejection, queue-depth
  accounting, and packet-hash recording.
- `ParallelMjpegDecodePipeline.Reorder.cs` owns decoded-frame ordering,
  missing-sequence handling, decoded reorder state, and decoded reorder
  capacity policy.
- `ParallelMjpegDecodePipeline.ReorderEmission.cs` owns emit-loop ordered
  draining, preview decoded-frame notification, and reorder/pipeline latency
  samples recorded during emission.
- `ParallelMjpegDecodePipeline.Lifecycle.cs` owns stop/dispose, worker/emitter
  shutdown joins, emitter signaling, fatal callback dispatch, remaining-time
  calculations, decoder disposal, queued work-item return, remaining
  reorder-frame disposal, and emit-signal disposal during final resource
  cleanup.

Invariants:

- Starting or stopping recording must not restart live preview unless the
  transition explicitly requires it.
- Capture lifecycle legality should be expressed in
  `CaptureSessionTransitionPolicy`, not scattered through ad hoc boolean checks.
- Mutating capture lifecycle state should go through serialized coordinator or
  transition-lock paths.
- Capture operations that only serialize in-place mutations may pass the
  current state to `RunTransitionAsync`; lifecycle-changing operations should
  pass an explicit target `CaptureSessionState`.
- Snapshot display state should be derived from service/runtime snapshots, not
  hand-updated independently in multiple event handlers.

## Recording

Primary current owner: `Sussudio/Services/Recording/`

Entry points:

- `LibAvEncoder.cs` owns encoder fields and stable public core state.
- `LibAvEncoder.Initialization.cs` owns FFmpeg initialization forwarding and
  encoder open/setup orchestration, option validation, video codec context
  setup, NVENC private options, and video bitstream-filter setup.
- `LibAvEncoder.Audio.cs` owns audio/microphone stream state, public status
  properties, public sample entry points, payload alignment checks,
  accumulator handoff, stream-chunk submission, interleaved packet writes,
  pending-sample flush, and accumulator ingress.
- `LibAvEncoder.AudioQueue.cs` owns audio sample queueing, drift-corrected
  encode chunks, planar sample copies, and prepared-frame drains.
- `LibAvEncoder.AudioInitialization.cs` owns audio and microphone AAC stream
  creation, codec opening, stream time-base setup, resampler/frame/buffer setup
  calls, microphone-specific setup, AAC codec context configuration, resampler
  setup, frame allocation, accumulator allocation, and sample-queue allocation.
- `LibAvEncoder.HdrSideData.cs` owns HDR mastering-display and content-light
  side-data attachment for software and hardware video frames, including
  parsing/applying mastering-display metadata strings.
- `LibAvEncoder.cs` owns encoder core state plus the encoder option and
  rotation-result DTOs consumed by the rest of the encoder family.
- `LibAvEncoder.CodecPolicy.cs` owns bitstream-filter selection, NVENC
  preset/split-encode mapping, frame-size math, sample-format support, and
  rational conversion helpers.
- `LibAvEncoder.AvSync.cs` owns A/V sync drift correction and diagnostics.
- `LibAvEncoder.cs` owns encoder fields, stable public core state,
  open-state guards, FFmpeg error strings, structured libav exceptions, and
  D3D11 device-removed checks.
- `LibAvEncoder.HardwareFrames.cs` owns D3D11 hardware frame setup,
  CUDA hardware frame context adoption, ArraySize=1 texture-pool creation,
  and hardware-frame fallback cleanup.
- `LibAvEncoder.VideoSubmission.cs` owns CPU packed-frame submission,
  packed-frame copy, forced-keyframe handling, per-frame HDR side-data
  attachment/removal, video packet drains, bitstream-filter drains, timestamp
  rescaling, and interleaved video packet writes.
- `LibAvEncoder.HardwareSubmission.cs` owns D3D11 and CUDA hardware-frame
  submission, including texture-pool copy/reference setup, GPU device-removed
  checks, hardware-frame PTS/keyframe assignment, HDR side-data attachment,
  EAGAIN packet drains, and hardware-frame unref cleanup.
- `LibAvEncoder.OutputRotation.cs` owns output rotation, IO close/reopen,
  stream reinitialization, video bitstream-filter reset, segment runtime reset,
  and MP4 muxer option policy for open and rotated outputs.
- `LibAvEncoder.ResourceCleanup.cs` owns flush/final close, dispose, trailer
  writing, close-result logging, final output telemetry, native
  frame/context/buffer release, hardware texture pool release, and encoder
  state reset.
- `Sussudio/Services/Recording/RecordingArtifactManager.cs` owns recording
  context creation, temp/final output file naming, HDR-active context fields,
  mux success/failure finalization, final-output validation, rollback,
  preserved temp-artifact discovery, and best-effort artifact deletion.
- `Sussudio/Services/Recording/Verification/RecordingVerifier.cs` owns strict verification orchestration and keeps the
  public verifier surface stable.
- `Sussudio/Services/Recording/Verification/RecordingVerifier.Ffprobe.cs` owns ffprobe path resolution, process specs,
  accessibility checks, HDR side-data probing, and scalar/key-value parsing of ffprobe output.
- `Sussudio/Services/Recording/Verification/RecordingVerifier.Validation.cs` owns dimensions, frame rate, cadence,
  container/codec, Flashback export format, and HDR mismatch policy.
- `Sussudio/Services/Recording/Verification/RecordingVerifier.Results.cs` owns early failure results, primary mismatch
  parsing, HDR parity, and mismatch taxonomy.
- `Sussudio/Services/Recording/Verification/RecordingVerifier.Cadence.cs` owns ffprobe frame timestamp parsing and
  cadence/drop/jitter metric calculation.

## Flashback

Primary current owner: `Sussudio/Services/Flashback/`

Entry points:

- `FlashbackBackendResources.cs` owns preview backend resource grouping,
  install/take/clear state, recovery-preserve flag storage and policy,
  recording-finalize handoff, producer attach/detach request shapes, and video,
  audio, and microphone feed wiring.
  `FlashbackBackendResources.Startup.cs` owns preview backend startup
  construction/install/playback initialization and startup failure rollback
  cleanup: producer detach, playback/sink/exporter/buffer cleanup, deferred
  cleanup scheduling, and final backend clear.
  `FlashbackBackendResources.BufferCycle.cs` owns sink-only buffer-cycle
  orchestration, purge/finalize decisions, full-rebuild fallback outcomes,
  playback disposal, old-sink stop/dispose, replacement sink startup/playback
  restore, and failed replacement cleanup.
  `FlashbackBackendResources.Teardown.cs` owns preview-backend teardown,
  sink stop/dispose, backend clear, artifact cleanup request/retry/dispose/purge
  mechanics. The backend resource owner
  receives export-lock wait/release delegates from `CaptureService` rather than
  owning service semaphores directly during preview backend startup, cycling,
  and teardown. `CaptureService`
  remains the transition/readiness coordinator and reads/writes the backend
  aggregate directly, without private resource shim properties.
- `FlashbackBufferManager.cs` owns buffer core state and read-only live counters.
- `FlashbackBufferManager.LiveAccounting.cs` owns latest-PTS reset/update,
  sink-cycle active segment finalization, encoder frame-rate truth, and
  disk-byte accounting updates.
- `FlashbackBufferManager.Segments.cs` owns active segment path generation, active segment start, generated-path abandonment, completion registration, duplicate-path rejection, and same-path segment extension.
- `FlashbackBufferManager.Lifecycle.cs` owns initialization, segment extension setup, disposal, disposed-state guards, and recovery-preserve state/marker files.
- `FlashbackBufferManager.Segments.cs` owns segment path lookup,
  range selection, start-PTS lookup, session-directory path safety checks,
  read-only segment counts, active-path projection, active segment start PTS
  calculation, and segment-info projection.
- `FlashbackBufferManager.Retention.cs` owns eviction-pause state, recording
  PTS range capture, pause-driven disk warning state, eviction selection,
  eviction file deletion, and disk-budget/window retention policy.
- `FlashbackBufferManager.Purge.cs` owns explicit segment purge, full session purge, and guarded purge file deletion behavior.
- `FlashbackStartupCacheCleanup.cs` owns startup stale-root/stale-session cleanup and temp-drive free-space probing.
- `FlashbackStartupSessionCacheBudget.cs` owns startup session-cache budget calculation, session-directory stats, oldest-session eviction, and cache-budget cleanup telemetry.
- `FlashbackDecoder.cs` owns decoder lifecycle, file open/close, dispose shell,
  and decoded video/audio output DTOs.
- `FlashbackDecoder.DecodeLoop.cs` owns video frame receive, packet feeding, inline audio interleave during video reads, live-file EOF clearing, recoverable seek log suppression, and decode phase timing state.
- `FlashbackDecoder.Seeking.cs` owns keyframe/exact seek control flow, seek timestamp conversion helpers, pending-frame transfer, seek-cap diagnostics, and seek-buffer flushing.
- `FlashbackDecoder.D3D11.cs` owns D3D11 device-context initialization, get-format callback behavior, and hardware decoder context setup.
- `FlashbackDecoder.D3D11Discovery.cs` owns D3D11VA decoder selection and hardware-config diagnostics.
- `FlashbackDecoder.VideoOutput.cs` owns decoded video frame output, hardware/software frame selection, PTS-to-TimeSpan conversion, and best-effort frame timestamp selection.
- `FlashbackDecoder.VideoConversion.cs` owns software plane copies and YUV-to-NV12/P010 conversion kernels.
- `FlashbackDecoder.AudioOutput.cs` owns audio codec/resampler initialization, audio packet delivery, callback failure handling, resampler output conversion, and bounded audio buffer sizing.
- `FlashbackDecoder.Validation.cs` owns decoded frame-size calculation, video-dimension validation, D3D11/software decoded-frame validation, input stream-count bounds, and stream-index bounds.
- `FlashbackPlaybackController*.cs` owns playback, scrub, and marker control.
- `FlashbackPlaybackController.DecoderFiles.cs` owns decoder creation, active file
  identity, file open checks, shared decoder close/open identity transitions,
  best-effort decoder file close handling, held-frame release during teardown,
  decoder close/dispose timing, and cleanup telemetry.
- `FlashbackPlaybackController.DecoderReopen.cs` owns active fMP4 reopen
  retry, keyframe-reopen recovery, near-live reopen guards, adjacent-segment
  seek fallback policy, segment-start probing, segment switch telemetry, and
  adjacent-seek failure handling.
- `FlashbackPlaybackController.CommandQueue.cs` owns the playback-thread
  command enum and payload contract shared by queue and thread-command owners.
- `FlashbackPlaybackController.cs` owns construction, the `FlashbackBufferManager`
  dependency, component reference lifecycle, preview-detach/deferred reattach
  lifecycle, public playback state surface, GPU-decode toggle, live-gap
  projection, decoder HW state, playback PTS anchors, scrub resume state,
  disposal, and state-transition logging.
- `FlashbackPlaybackController.CommandQueue.cs` owns public playback command
  entry points for scrub, seek, play/pause, go-live, and nudge, command queue
  writes/drop policy, seek/scrub coalesced command admission, queued-position
  resolution, playback-thread control-yield peek policy, public command/thread
  metrics, command status counters, pending-command accounting, active-command
  timing, queue command telemetry bookkeeping, command readiness/failure state,
  and no-op logging.
- `FlashbackPlaybackController.ThreadLifecycle.cs` owns playback thread state,
  timeouts, start/recovery, stop/cancel/join diagnostics, command-channel
  lifetime, scheduling policy, exit transactions, live-restore cleanup, and CTS
  disposal warnings, plus `PlaybackThreadEntry`, command dequeue/waiting,
  cancellation exits, and continuous-playback pacing handoff.
- `FlashbackPlaybackController.ThreadCommands.cs` owns playback-thread
  active-command telemetry, command-complete logging, the dispatch switch, seek
  and scrub begin/update command execution, end-scrub resume, coalesced
  seek/scrub resolution, exact resume targets, playback resume handoff, frozen
  valid-start sampling, scrub-display failure recovery, audio/preview
  suppression/resume ordering, and terminal go-live/stop live-restore handoff.
- `FlashbackPlaybackController.ThreadCommands.cs` owns playback-thread play
  command execution, including exact resume, file-open/reopen, audio prebuffer,
  and rendering resume ordering.
- `FlashbackPlaybackController.ThreadCommands.cs` owns playback-thread
  pause command execution, including pause-from-live freeze/display ordering,
  exact resume targets, and audio/preview suppression.
- `FlashbackPlaybackController.ThreadCommands.cs` owns playback-thread
  nudge command execution, including frame-step decode, no-file recovery, and
  seek-display failure recovery.
- `FlashbackPlaybackController.AudioRouting.cs` owns live audio
  suppress/restore, decoder audio callback wiring, playback chunk
  validation/return, playback PTS gate handling, pooled audio-buffer return
  warnings, playback-state audio/preview routing, best-effort preview
  submission guards, audio renderer pause/resume/flush guards, playback
  startup/seek audio prebuffering, target/timeout/frame-budget policy, and
  decoder rewind after decode-ahead audio priming.
- `FlashbackPlaybackController.AudioMasterPacing.cs` owns audio-master pacing
  clock sample state, stale-clock detection, read-only A/V drift projection,
  clock-drift computation, correction policy, delay-adjustment counter
  projection, fallback accounting/classification, fallback reason/drift/clock-age
  telemetry, and wall-clock sleep/spin pacing.
- `FlashbackPlaybackController.PreviewFrames.cs` owns decoded frame
  preview-sink selection, submission telemetry, renderer calls, validation,
  GPU/CPU skip reasons, NV12/P010 byte-size policy, and held-frame handoff.
- `FlashbackPlaybackController.PlaybackFrames.cs` owns continuous playback frame
  decode/submit pacing, seek/scrub keyframe display, keyframe seek/reopen retry,
  file-PTS mapping for displayed seek frames, seek/scrub decoded-frame
  acquisition, adjacent-segment fallback display, held-frame release, no-frame
  seek-display failure accounting, frame-skip catch-up, decode-error snap,
  near-live snap, and playback failure recovery back to live state.
- `FlashbackPlaybackController.PlaybackFrames.cs` owns playback-frame dequeue/decode selection, prebuffer cleanup, A/V drift frame-skip catch-up policy, held playback frame backing state, release-for-live reset policy, best-effort decoded frame release warnings, continuous playback frame progression, decoded-frame submission flow, live-recovery policy invocation, cadence pacing, and A/V drift diagnostics.
- `FlashbackPlaybackController.PlaybackSegmentEdges.cs` owns segment-edge routing, write-head waits, next-segment switching, active fMP4 reopen/reseek recovery, post-switch audio gates, and PTS cadence baseline reset.
- `FlashbackPlaybackController.PlaybackTiming.cs` owns frame-rate resolution,
  continuous-playback snap threshold policy, pause-from-live target calculation,
  software-decode budget detection, decoder hardware-acceleration status
  refresh, over-budget snap telemetry, recovery handoff, rolling playback
  cadence metric updates, decoded PTS cadence state/projection/tracking,
  mismatch telemetry, and cadence-baseline reset.
- `FlashbackPlaybackController.Markers.cs` owns the marker command API, in/out marker state, file-PTS projection, marker normalization, invalid-range clearing, recovery restore, out-point pause checks, scrub/seek clamp policy, saturating timestamp math, active fMP4 segment detection, and playback path comparison.
- `FlashbackPlaybackController.Metrics.cs` owns playback cadence/decode metric
  DTOs, percentile projection, private metric counters, read-only projections,
  cadence/decode sample rings, playback metric reset, seek-cap telemetry, decode
  timing wrappers, max decode phase state, and dominant decode phase resolution.
- `FlashbackEncoderSink.Startup.cs` owns buffer session creation, generated session ID formatting, encoder initialization, active-segment setup, startup queue allocation, session validation, frame-rate fallback/clamping, startup metric/counter reset, video diagnostics reset, start-failure rollback, PTS continuation, background task startup, and start-transaction orchestration.
- `FlashbackEncoderSink.RuntimeState.cs` owns public runtime counters, queue telemetry, encoder status/format projections, saturated PTS conversion, non-negative byte/duration math, and best-effort eviction resume fallback.
- `FlashbackEncoderSink.Options.cs` owns encoder option construction, segment extension policy, transport container selection, session frame-rate rational validation, recording-to-Flashback session mapping, recording-format codec mapping, split-encode wire mapping, and recording frame-rate argument parsing.
- `FlashbackEncoderSink.VideoQueueSubmission.cs` owns video/GPU queue admission transactions, queue-full classification, producer wakeup signaling, enqueue lifecycle guards, producer input rejection reasons, TryWrite depth accounting, max-depth updates, failed-write depth rollback, rejection counters, last-reason state, and throttled queue rejection logs.
- `FlashbackEncoderSink.Queues.cs` owns queue completion/signaling, shared queue-depth accounting, cancellation waits, failure notification, video/audio/GPU packet DTOs, video enqueue result classification, ArrayPool packet buffer rent/return helpers, leased video packet disposal, queued-buffer cleanup, and best-effort return/release of queued video, audio, microphone, and GPU packets.
- `FlashbackEncoderSink.VideoQueueSubmission.cs` owns video/GPU/audio/microphone queue admission transactions, queue-full classification, force-rotate audio queue guard policy, producer wakeup signaling, enqueue lifecycle guards, input rejection reasons, channel writes, depth accounting, rejection counters, backlog eviction accounting, and throttled queue diagnostics.
- `FlashbackEncoderSink.EncodingLoop.cs` owns the background encode loop, normal drain ordering, force-rotate dispatch, cancellation handling, fatal cleanup, and final segment registration.
- `FlashbackEncoderSink.PacketDrain.cs` owns bounded video/GPU/audio/microphone packet drains, frame-size defense, queue-depth accounting, encoder submission, GPU texture release, and pooled buffer returns.
- `FlashbackEncoderSink.EncodingProgress.cs` owns encoder PTS resolution, latest-PTS and disk-byte refresh, frame-encoded event dispatch, segment-rotation triggering, active-segment registration, and rotation-failure recovery.
- `FlashbackEncoderSink.ForceRotate.cs` owns export force-rotate state/status projections, idle waits, request admission/publication, timeout/cancellation result classification, committed-pending grace handling, pending-request cancellation, empty completion on stop/dispose/failure, force-rotate drain abort policy, the `ForceRotateRequest` state machine, encoding-thread request capture, queue drain-to-rotate ordering, commit/rotation execution, result completion, failure logging, and draining-gate cleanup.
- `FlashbackEncoderSink.Inputs.cs` owns raw/lease/GPU video enqueue entry points, video/GPU rejection guards, frame-size validation, texture AddRef ownership, audio/microphone enqueue entry points, force-rotate audio rejection guards, and hot WASAPI writer adapters.
- `FlashbackEncoderSink.DisposeLifecycle.cs` owns `StopAsync`, stop drain
  timeout classification, final stop result reporting, `Dispose`/`DisposeAsync`,
  deferred cleanup, final dispose reset, cancellation/disposal helpers, and
  best-effort encoder/buffer manager disposal.
- `FlashbackEncoderSink.Recording.cs` owns recording PTS boundary state, active-recording projection, begin-recording availability checks, the `IRecordingSink.StartAsync` adapter, recording begin validation, eviction-pause handoff, active-state publication, start rollback, recording end rejection/failure/success results, end-PTS capture, eviction resume, PTS clamping, and ready logging.
- `FlashbackEncoderSink.RuntimeState.cs` owns public frame/audio/disk counters, drop counters, rotation-failure counts, frame-encoded events, queue-depth/capacity/max-depth projections, queue rejection summaries, GPU queue projections, video queue latency/backpressure metrics, encoding failure status, audio/microphone enablement, fatal-error callback registration, encoder format summaries, HDR P010 projection, and encoding completion task exposure.
- `FlashbackExporter.Lifecycle.cs` owns shared native export state, constants,
  exporter disposal, active-export cancellation, lock handling, and native
  cleanup.
- `FlashbackExporter.SingleFile.cs` owns the single-file export shell:
  validation, seek/setup, final output replacement, success result shaping, and
  single-export lock release.
- `FlashbackExporter.SingleFilePacketReadLoop.cs` owns single-file packet
  result validation, read-loop dispatch, drift logging, the active input packet
  pump, stream filtering, out-point clipping, timestamp rebasing, native
  interleaved writes, writer throttling, per-read packet unref, progress
  heartbeat, final packet cleanup, write state, timestamp-base discovery,
  early-packet buffering, and EOF partial-base rescue.
- `FlashbackExporter.Segments.cs` owns multi-segment export validation
  dispatch, temp-output preparation, final output replacement, and export-lock
  release.
- `FlashbackExporter.SegmentPacketWriting.cs` owns the multi-segment
  packet-copy/remux orchestration: output-template initialization, segment
  input sequencing, segment export range/window projection, segment offset
  updates, completion progress, and requested-segment skip validation.
- `FlashbackExporter.SegmentPacketReadLoop.cs` owns the active segment packet
  pump and its write state: native frame reads, per-read packet unref, stream
  filtering, timestamp-base discovery, buffered packet transition/rescue/flush,
  rebased packet writes, writer throttling, and EOF partial-base rescue/freeing.
- `FlashbackExporter.SegmentPacketReadLoop.cs` also owns segment timestamp
  rebasing, segment-boundary repair, DTS monotonicity, and native packet writes.
- `FlashbackExporter.SegmentTemplate.cs` owns selection of the first usable
  segment output template, stream-map initialization, template-skip logs,
  per-segment input open, stream-info lookup, stream-count checks,
  layout-mismatch skip tracking, and close-on-failed-preflight behavior.
- `FlashbackExporter.Lifecycle.cs` owns exporter disposal, active-export
  cancellation during disposal, linked cancellation-source helpers, export-lock
  wait/release/disposal policy, native input/output close, and native FFmpeg
  cleanup.
- `FlashbackExporter.Execution.cs` owns public export request routing, export
  task scheduling, linked cancellation wrapper disposal, background thread
  priority, and segment snapshots.
- `FlashbackExporter.PacketTiming.cs` owns export time-span conversion,
  saturated time arithmetic, non-negative byte/count saturation, packet
  timestamp normalization, segment boundary timestamp repair, packet clone/free
  helpers, and buffered packet flushes.
- `FlashbackExporter.Streams.cs` owns input/output FFmpeg context setup,
  stream count validation, stream-template copying, segment stream-layout
  compatibility checks, and output header writing.
- `FlashbackExporter.OutputFiles.cs` owns temp output cleanup, stale temp
  preparation, orphaned `.mp4.tmp` cleanup, active output trailer/IO close
  finalization, temp-output validation, atomic destination replacement,
  overwrite policy, and invalid final-output cleanup.
- `FlashbackExporter.RuntimePolicy.cs` owns progress normalization/reporting,
  heartbeat cadence, export writer adaptive throttling, fixed sleep/yield
  pacing, and per-export throttle provider scoping.
- `FlashbackExporter.Validation.cs` owns completed-output length probing, final
  output validation text, normalized path comparison, output path validation,
  export-range validation, segment/export-range overlap classification,
  multi-segment export input validation, and readable-segment byte estimation
  for progress.
- `FlashbackExporter.Lifecycle.cs` owns FFmpeg error string formatting and
  throwing alongside native cleanup and lifetime handling.

Invariants:

- Disable means the timeline should be hidden/locked out.
- Scrub frames must not contaminate live/playback cadence metrics.
- Export must not overwrite without the explicit force path.

## UI Shell And Presentation

Primary current owners:

- `Sussudio/MainWindow.*.cs` for shell, renderer, fullscreen, screenshots,
  animations, and window lifecycle.
- `Sussudio/Controllers/FullScreen/FullScreenController.cs` owns fullscreen public
  toggle/state, enter/exit orchestration, rect animation and size waits,
  chrome/material state, overlay pointer/auto-hide behavior, and full-screen key
  routing behind the shared full-screen context.
  behavior plus full-screen key routing and timeline eligibility.
  `Sussudio/MainWindow.ShellChrome.Composition.cs` wires the controller context,
  button/menu/double-tap and automation command adapters, key routing, pointer,
  and auto-hide adapters. Flashback command execution remains in
  `Sussudio/Controllers/Flashback/FlashbackCommandController.cs`.
- `Sussudio/Controllers/Screenshot/Window/WindowScreenshotController.cs` owns automation whole-
  window screenshot dispatch, UI-thread enqueue/cancellation, and failure
  wrapping. `Sussudio/Controllers/Screenshot/Window/WindowScreenshotNativeCapture.cs` owns native
  PrintWindow capture, GDI/DIB lifetime, output directory creation, and
  screenshot result shaping. `Sussudio/Controllers/Screenshot/Window/WindowScreenshotImageEncoder.cs`
  owns the pure PNG/BMP byte-stream encoding helpers. Keep whole-window
  screenshot automation on `MainWindow.WindowShell.cs` with the rest of the
  `IAutomationWindowControl` adapter.
- `Sussudio/Controllers/Screenshot/Preview/PreviewScreenshotController.cs` owns
  the pure preview-frame screenshot output-directory fallback, file naming,
  status/log text policy, and XAML preview-frame screenshot button workflow:
  directory creation, preview-frame capture, logging side effects, and button
  enable/disable state.
  `MainWindow.ButtonActions.cs` is the XAML-facing adapter for preview-frame
  screenshots.
- `Sussudio/Controllers/Window/WindowAutomationController.cs` owns window geometry
  automation plus the recordings-folder command: UI-thread dispatch, AppWindow
  and DisplayArea access, maximized presenter restore, side effects, and pure
  snap-region rectangle math. `MainWindow.WindowShell.cs` is the
  `IAutomationWindowControl` adapter; recording-aware close handling stays with
  the close lifecycle/finalization owners.
- `Sussudio/Controllers/Window/WindowAutomationHostLifecycleController.cs` owns shell
  automation host lifecycle: automation token/pipe-name resolution, diagnostics
  hub construction, command dispatcher construction, named-pipe server
  construction, once-only startup, ready/disabled logging, and pipe-before-hub
  shutdown disposal. `Sussudio/MainWindow.ShellChrome.Composition.cs` starts
  the controller after initial device refresh; `Sussudio/MainWindow.xaml.cs`
  passes the controller dispose delegate into the shutdown cleanup controller.
- `Sussudio/MainWindow.ShellChrome.Composition.cs` owns the XAML-facing shell
  launch/chrome adapter surface: native shell bootstrap callbacks, control-bar
  animation callbacks, launch entrance/startup callbacks, settings shelf
  callbacks, static shell elevation, shell property-change routing callbacks,
  and splash phrase start/stop callbacks. Window close routing/finalization ownership is detailed in the
  window close section below:
  `Sussudio/Controllers/Window/WindowCloseLifecycleController.cs`,
  `Sussudio/Controllers/Window/WindowCloseRecordingFinalizationController.cs`,
  `Sussudio/MainWindow.WindowShell.cs`, and `Sussudio/MainWindow.xaml.cs`.
- `Sussudio/Controllers/Preview/Renderer/PreviewRendererHostController.cs` owns top-level
  preview resize telemetry throttling and reset state for preview compositor
  transforms. `Sussudio/MainWindow.PreviewRenderer.Composition.cs` wires the
  renderer host context, `SizeChanged` adapter, renderer-host reset handoff,
  stable start/stop, shutdown, and reinit-unsafe-window automation adapters.
  `Sussudio/Controllers/Preview/Renderer/PreviewRendererHostController.cs` owns hosted preview
  renderer context, public runtime state, counters, start/stop/shutdown flow,
  renderer startup dimension/fps/HDR/min-present-interval planning, CPU fallback
  attachment, D3D renderer startup and event/failure handling, cleanup, D3D reinit renderer-stop/timeout policy,
  disposal, unsafe-window telemetry, stop tick accounting, and fresh
  SwapChainPanel replacement.
  `Sussudio/Controllers/Preview/PreviewSurfacePresentationController.cs` owns preview
  surface content-fit sizing and GPU panel visibility.
  `Sussudio/Controllers/Preview/PreviewSurfaceShadowController.cs` owns
  video/control-bar composition shadow visuals, bounds alignment, clear behavior,
  and compositor opacity fade routing. `MainWindow.PreviewRenderer.Composition.cs`
  is the XAML-facing adapter for preview renderer and surface wiring.
- `Sussudio/MainWindow.PreviewRenderer.Composition.cs` owns the stable
  automation preview snapshot adapter and context wiring alongside preview
  renderer host composition.
  `Sussudio/Controllers/Preview/Renderer/PreviewRuntimeSnapshotSamplingController.cs`
  owns the UI-dispatch handoff, UI-thread-only preview runtime field sampling,
  startup missing-signal refresh, and sampled-input assembly.
  `Sussudio/Controllers/Preview/Renderer/PreviewRuntimeSnapshotController.cs`
  owns read-only preview runtime snapshot construction orchestration and the
  UI-thread sampled preview snapshot input contract shared by the snapshot
  controller and D3D projection builder.
  `Sussudio/Controllers/Preview/Renderer/PreviewRuntimeSnapshotMapper.cs`
  owns final preview runtime snapshot DTO flattening from sampled input, D3D
  projection, and surface/startup/GPU playback projection policies.
  `Sussudio/Controllers/Preview/Renderer/PreviewRuntimeSnapshotController.cs`
  owns the health input factory, preview startup elapsed timing, and blank/stall
  suspicion policy beside snapshot construction.
  `Sussudio/Controllers/Preview/Renderer/PreviewRuntimeD3DProjection.cs`
  owns the renderer projection data contract, D3D policy records, policy
  evaluation order, and assignment from evaluated policy records. It keeps the
  named policy classes for D3D-vs-CPU frame counters, renderer state, display
  cadence, render CPU timing, pipeline latency, frame ownership, DXGI frame
  statistics, and frame-latency wait defaults in one cohesive projection owner.
  Window close routing/finalization ownership is detailed in the window close
  section below:
  `Sussudio/Controllers/Window/WindowCloseLifecycleController.cs`,
  `Sussudio/Controllers/Window/WindowCloseRecordingFinalizationController.cs`,
  `Sussudio/MainWindow.WindowShell.cs`, and `Sussudio/MainWindow.xaml.cs`.
- `Sussudio/MainWindow.StatusStripPresentation.cs` keeps the XAML-facing title
  update hook; `Sussudio/Controllers/Shell/ShellChromeController.cs` owns window title
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
  close-in-progress exception classification, automation close dispatch
  orchestration, and actual close request execution: `Close()`, completion timing after non-recording
  closes, close-in-progress success handling, COM `Application.Current.Exit()`
  fallback, requested-state reset after unexpected failures, and `AppWindow.Closing`
  decision choreography.
- `Sussudio/Controllers/Window/WindowCloseRecordingFinalizationController.cs` owns
  recording finalization side effects during pre-close and post-close cleanup:
  the 120-second stop budget, `StopRecordingAndWaitAsync` wait race, timeout/
  failure breadcrumbs, status text, and shutdown-content dim/restore policy.
- `Sussudio/Controllers/Window/WindowShutdownCleanupController.cs` owns the
  post-`Closed` cleanup order: cleanup latch, close completion, closing-state
  mark, timer stops, event detaches, preview shutdown, post-close recording
  finalization handoff, automation disposal, NVML disposal, and ViewModel
  disposal.
- `Sussudio/MainWindow.WindowShell.cs` owns the XAML/AppWindow close adapter:
  `RegisterCloseLifecycle`, `CloseAsync`, and the stable
  `RequestWindowClose()` adapter.
- `Sussudio/MainWindow.xaml.cs` wires MainWindow cleanup
  delegates and the stable `Closed` event adapter into
  `WindowShutdownCleanupController`, and owns the timer,
  event-detach, stats, recording-visual, and preview-size cleanup delegate
  adapters.
- `Sussudio/Controllers/Window/NativeWindowBootstrapController.cs` owns native window
  bootstrap: `AppWindow` lookup, ViewModel window handle handoff,
  minimum-size subclassing, DWM cloak/dark-mode setup, first-composed-frame
  shell reveal scheduling/cancellation, initial shell size, icon, and native
  helpers used by shell startup and automation controllers.
  `Sussudio/MainWindow.ShellChrome.Composition.cs` is the XAML-facing shell
  launch/chrome native-window adapter and keeps the `_hwnd` field consumed by screenshot and window
  automation paths.
- `Sussudio/Controllers/Window/WindowUiDispatchController.cs` owns MainWindow
  UI-thread direct execution, dispatcher enqueue/cancellation/error wrapping,
  preview-snapshot-style result dispatch with three-attempt enqueue retry, and
  guarded async event-handler status updates used by automation adapters and
  XAML event handlers. `Sussudio/MainWindow.WindowShell.cs` keeps the stable
  private MainWindow adapter names for callers.
- `Sussudio/MainWindow.xaml.cs` owns the root `SetupBindings()`
  startup binding sequence and leaves feature-specific binding clusters in
  focused partials or controllers, including initial recording lockout,
  device-selection change hooks, stats visibility sync, and status-strip
  projection.
- `Sussudio/MainWindow.PreviewTransitions.Composition.cs`
  owns the preview-transition XAML command and callback adapter surface.
  `PreviewButtonActionController` owns the preview
  fade/reinit/start/stop command behavior. One-line XAML command bridges for
  capture-device, recording, output-path, and preview-screenshot buttons live in
  their feature adapter partials beside the owning controllers.
- `Sussudio/MainWindow.xaml.cs` owns only the root ViewModel
  PropertyChanged event envelope and router composition.
  `Sussudio/Controllers/Shell/MainWindowPropertyChangedRouter.cs` owns
  property-name normalization and route order. Capture-selection and
  status-strip adapters are still considered first through the
  `Sussudio/MainWindow.CaptureBindings.cs` adapter and
  `MainWindow.StatusStripPresentation.cs`; broad domain property-name switches
  and status-strip routing logic live in focused controllers/partials.
- `Sussudio/Controllers/Preview/PreviewSurfaceShadowController.cs` owns shared
  compositor opacity fade helpers for preview shadow visuals without adding
  dispatcher hops.
- `Sussudio/Controllers/Audio/Meter/AudioMeterController.cs` owns audio/microphone meter
  setup, the XAML/view-model dependency bag, runtime fields, smoothing,
  peak/range markers, microphone meter clipping, reset behavior, timer lifetime,
  `TranslateMarker`, monitoring/disabled animations, and rounded content clips.
  `Sussudio/MainWindow.AudioBindings.cs` is its XAML-facing adapter.
  `Sussudio/Controllers/Audio/AudioControlBindingController.cs` owns the audio-control
  binding context, initial audio/microphone projection, preview-volume binding and priming,
  audio/microphone/device-audio selection handlers,
  record/preview/custom-audio/microphone toggle handlers, audio-meter activation,
  initial meter presentation, and device-audio gain/meter resize hooks.
  `Sussudio/MainWindow.AudioBindings.cs` is its XAML-facing adapter.
- `Sussudio/Controllers/Stats/StatsOverlayController.cs` owns stats dock visibility
  orchestration, stats/frame-time toggle event hookup and checked/unchecked
  handling, stats toggle-to-view model sync, frame-time overlay visibility,
  polling lifetime, stats dock show/hide storyboard construction, dock
  visibility mutations, and animation completion state.
  `Sussudio/Controllers/Stats/StatsOverlayCompositionController.cs` owns the
  stats overlay runtime facade, construction-order entry point, and graph
  factory wiring: snapshot provider, frame-time presentation, dock graph,
  overlay controller, and section chrome controller.
  `Sussudio/Controllers/Stats/StatsOverlayCompositionController.Contexts.cs`
  owns the grouped stats composition context contracts for shell controls,
  snapshot sources, dock targets, hardware sources, and frame-time targets.
  `Sussudio/MainWindow.StatsOverlay.Composition.cs` owns the XAML-facing stats
  overlay adapter surface: composition controller instantiation, shell-control
  wiring, snapshot sources, dock targets, MJPEG/NVML sources, compact frame-time
  targets, lifecycle/polling commands, and section chrome event adapters.
  `Sussudio/Controllers/Stats/StatsDockControllerGraph.cs` owns stats dock
  presentation, diagnostic row, hardware row, and refresh-controller graph
  construction plus the dock graph context contract because the dock is only
  driven by the overlay controller.
  `Sussudio/Controllers/Stats/StatsDockRefreshController.cs` owns stats dock refresh
  orchestration: snapshot acquisition, dock presentation build/apply,
  diagnostics visibility gating, and decode/GPU row refresh ordering.
  `Sussudio/Controllers/Stats/StatsDockPresentationController.cs` owns
  stats dock metric text, visibility, and status brush application after the
  presentation model is built. `Sussudio/Controllers/Stats/StatsOverlayCompositionController.cs`
  keeps the local section chrome controller that owns stats dock section
  expand/collapse chrome and automation-visible section visibility application;
  `Sussudio/MainWindow.StatsOverlay.Composition.cs`
  owns the XAML/automation adapter for that stats shell wiring.
  `Sussudio/Controllers/Stats/StatsWindowPresentationController.cs`
  owns detached stats-window metric text and dynamic telemetry detail rendering.
  `Sussudio/Controllers/Stats/StatsOverlayCompositionController.cs` owns shell stats snapshot
  orchestration from capture-health, renderer metrics, and view state, including
  renderer cadence/recent-sample acquisition and null fallback policy.
  `Sussudio/MainWindow.StatsOverlay.Composition.cs` is the XAML-facing
  surface for stats visibility, polling, snapshot source wiring, frame-time
  targets, and section chrome commands; stats provider/controller context
  contracts live in
  `Sussudio/Controllers/Stats/StatsOverlayCompositionController.Contexts.cs`
  and provider/controller composition lives in
  `Sussudio/Controllers/Stats/StatsOverlayCompositionController.cs`.
- `tests/Sussudio.Tests/StatsOverlay.Lifecycle.Tests.cs` owns xUnit contract
  checks for stats overlay lifecycle wiring and stats section chrome.
- `tests/Sussudio.Tests/StatsDockPresentation.Tests.cs` owns xUnit contract
  checks for stats dock refresh orchestration, diagnostic row update
  delegation, hardware row projection, and row chrome pooling.
- `Sussudio/Controllers/Stats/StatsDiagnosticRowsController.cs` owns diagnostic row
  presentation, empty-state rows, group headers, and diagnostic row pooling.
  `Sussudio/Controllers/Stats/StatsDockRowChromePresenter.cs` owns shared stats
  dock row creation, label/value text mutation, visibility toggles, dock row
  style application, and dynamic decode/GPU simple row pools.
  `Sussudio/Controllers/Stats/StatsDockRefreshController.cs` delegates diagnostic row
  presentation to `StatsDiagnosticRowsController`, and owns hardware row
  refresh, availability, and decode/GPU minimum pool sizing before delegating row
  chrome. `Sussudio/Controllers/Stats/StatsDockRefreshController.cs` also keeps
  the hardware input provider that owns live MJPEG/NVML input acquisition,
  decode availability policy, and pure telemetry projection into the hardware-row
  presentation input DTOs;
  `Sussudio/ViewModels/StatsPresentationBuilder.cs` owns pure decode/GPU row
  text projection over presentation inputs, and
  `StatsDockRowChromePresenter` owns shared row chrome plus decode/GPU row
  pooling while
  `StatsDockRefreshController` owns when decode/GPU rows refresh.
- `Sussudio/Controllers/Stats/FrameTimeOverlayPresentationController.cs` owns compact
  frame-time overlay text application, graph-line mutation, canvas sizing,
  sample projection, and expected-line geometry.
  `Sussudio/MainWindow.StatsOverlay.Composition.cs` owns the XAML-facing
  compact overlay adapter beside the stats overlay visibility route.
  `Sussudio/ViewModels/StatsPresentationBuilder.cs` owns the cohesive pure stats
  presentation projection surface: shared formatting helpers, stats dock summary
  construction, HDMI/capture/preview resolution text, compact preview-stat
  formatting, range/sample text policy, frame-time overlay presentation,
  visual-cadence FPS/repeat/motion text formatting, expected visual-repeat drift
  helpers, encoder dock visibility, codec label, bitrate, encoder drift text
  formatting, diagnostic row construction, and source-summary parsing.
  `Sussudio/Controllers/Stats/StatsDockRefreshController.cs` owns the local
  hardware input provider for live MJPEG/NVML sampling callbacks and pure
  telemetry-to-presentation-input projection for hardware rows.
  `Sussudio/ViewModels/StatsPresentationBuilder.cs` also owns decode/GPU row
  text projection over presentation inputs, frame-lane diagnostic health summary
  classification, detached stats-window text, telemetry-detail presentation,
  stats lane status classification, and the visual-repeat drift result.
  `Sussudio/ViewModels/StatsPresentationModels.cs` owns the internal DTO
  records/enums consumed by the stats overlay and stats-window controllers.
  `Sussudio/ViewModels/StatsSnapshot.cs` owns the UI stats snapshot DTO, and
  `Sussudio/ViewModels/StatsSnapshotBuilder.cs` owns capture-health, renderer,
  and shell view-state projection into that DTO after acquisition.
- `Sussudio/ViewModels/CaptureModeOptionsBuilder.cs` owns pure resolution and
  video-format option construction, HDR mode enablement, and source aspect-ratio
  filtering. Shell files bind and display those options.
- `tests/Sussudio.Tests/XUnit.StatsPresentation.Formatting.Tests.cs` owns
  detached-window, dock encoder, display-repeat visual-cadence, and compact
  preview summary behavior checks.
- `tests/Sussudio.Tests/XUnit.StatsPresentation.FrameTime.Tests.cs` owns
  frame-time range and frame-time graph geometry behavior checks, plus shared
  StatsPresentation xUnit reflection/file helpers.
  `tests/Sussudio.Tests/StatsPresentation.Ownership.Tests.cs` owns
  builder/controller/DTO source-shape assertions,
  `tests/Sussudio.Tests/StatsPresentation.SourceTelemetry.Tests.cs` owns HDMI
  source telemetry panel projection checks,
  `tests/Sussudio.Tests/XUnit.StatsHardwareRowsTests.cs` owns hardware row
  presentation behavior checks, `tests/Sussudio.Tests/XUnit.StatsHardwareRows.InputProvider.Tests.cs`
  owns hardware row input-provider behavior checks, and
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
- `tests/Sussudio.Tests/MainWindowUiContract.StatsSnapshot.Tests.cs` owns
  xUnit stats snapshot builder contract checks.
- `tests/Sussudio.Tests/MainWindow.ShellOwnership.Chrome.Tests.cs` owns
  MainWindow shell chrome ownership assertions for the settings shelf, window
  title, live signal info, and status-strip presentation.
- `tests/Sussudio.Tests/MainWindow.ShellOwnership.Startup.SplashPhrase.Tests.cs`
  owns MainWindow startup/launch ownership assertions for splash loading
  phrases and splash pacing policy.
- `tests/Sussudio.Tests/MainWindow.ShellOwnership.Startup.Launch.Tests.cs`
  owns MainWindow startup/launch ownership assertions for launch entrance
  animation and first-load hosting.
- `tests/Sussudio.Tests/MainWindow.ShellOwnership.PreviewRuntime.Tests.cs`
  owns MainWindow preview resize telemetry and preview renderer startup-plan
  fallback policy assertions.
- `tests/Sussudio.Tests/MainWindow.ShellOwnership.PreviewRuntime.RendererHost.Tests.cs`
  owns MainWindow preview renderer host lifecycle, D3D startup/reinit, startup
  plan, and stats adapter ownership assertions.
- `tests/Sussudio.Tests/MainWindow.ShellOwnership.PreviewRuntime.Snapshot.Tests.cs`
  owns preview runtime snapshot adapter, input, controller, mapper, and
  snapshot projection policy ownership assertions.
- `tests/Sussudio.Tests/MainWindow.ShellOwnership.PreviewRuntime.D3DProjection.Tests.cs`
  owns preview runtime D3D projection root, leaf partial, builder, and policy
  ownership assertions.
- `tests/Sussudio.Tests/MainWindow.ShellOwnership.PreviewRuntime.Surface.Tests.cs`
  owns MainWindow preview surface presentation and shadow controller ownership
  assertions.
- `tests/Sussudio.Tests/PreviewRuntimeSnapshotController.D3DPolicies.Tests.cs`
  owns preview runtime snapshot D3D frame-counter CPU fallback and projection
  composition regression checks.
- `tests/Sussudio.Tests/PreviewRuntimeSnapshotController.D3DPolicies.RendererTiming.Tests.cs`
  owns preview runtime snapshot D3D renderer-state, display-cadence,
  render-CPU-timing, and pipeline-latency null-renderer regression checks.
- `tests/Sussudio.Tests/PreviewRuntimeSnapshotController.D3DPolicies.FrameFlow.Tests.cs`
  owns preview runtime snapshot D3D frame-statistics, frame-latency-wait, and
  frame-ownership null-renderer regression checks.
- `tests/Sussudio.Tests/PreviewRuntimeSnapshotController.Integration.Tests.cs`
  owns preview runtime snapshot controller Build integration regression checks.
- `tests/Sussudio.Tests/PreviewRuntimeSnapshotController.Health.Tests.cs` owns
  preview runtime snapshot health policy and health input factory regression
  checks.
- `tests/Sussudio.Tests/PreviewRuntimeSnapshotController.ProjectionPolicies.Tests.cs`
  owns preview runtime snapshot surface, startup, and GPU playback projection
  policy regression checks.
- `tests/Sussudio.Tests/MainWindow.ShellOwnership.NativeBootstrap.Tests.cs`
  owns MainWindow native bootstrap, adapter, and first-frame reveal ownership
  assertions.
- `tests/Sussudio.Tests/MainWindow.ShellOwnership.WindowLifecycle.Tests.cs`
  owns MainWindow window-lifecycle composition and ownership documentation
  assertions.
- `tests/Sussudio.Tests/MainWindow.ShellOwnership.WindowLifecycle.CloseControllers.Tests.cs`
  owns MainWindow close lifecycle state, close request, and app-closing
  controller ownership assertions.
- `tests/Sussudio.Tests/MainWindow.ShellOwnership.WindowLifecycle.CloseProtection.Tests.cs`
  owns the recording-stop close protection contract for MainWindow close.
- `tests/Sussudio.Tests/MainWindow.ShellOwnership.WindowLifecycle.RecordingFinalization.Tests.cs`
  owns MainWindow close recording-finalization ownership and stop-wait policy
  assertions.
- `tests/Sussudio.Tests/MainWindow.ShellOwnership.WindowLifecycle.ShutdownCleanup.Tests.cs`
  owns MainWindow post-close shutdown cleanup and automation-host disposal
  ownership assertions.
- `tests/Sussudio.Tests/MainWindow.ControllerOwnership.Visual.ShellPreview.Tests.cs`
  owns MainWindow controller-adapter ownership assertions for control bar,
  shell elevation, preview-transition, preview startup overlay, and preview
  fade-in controllers.
- `tests/Sussudio.Tests/MainWindow.ControllerOwnership.Visual.Recording.Tests.cs`
  owns MainWindow controller-adapter ownership assertions for recording-button
  chrome, recording-state presentation, and recording-state presentation policy.
- `tests/Sussudio.Tests/MainWindow.ControllerOwnership.Tests.cs` owns
  MainWindow property-change routing ownership assertions across focused
  controller adapters.
- `tests/Sussudio.Tests/MainWindow.ControllerOwnership.Interaction.Tests.cs`
  owns MainWindow controller-adapter ownership assertions for recording action,
  preview audio fade, and preview button presentation.
- `tests/Sussudio.Tests/MainWindow.ControllerOwnership.AudioPresentation.Tests.cs`
  owns MainWindow controller-adapter ownership assertions for audio control
  presentation and microphone controls.
- `tests/Sussudio.Tests/MainWindow.ControllerOwnership.Layout.Tests.cs` owns
  MainWindow responsive shell layout controller-adapter and breakpoint/placement
  policy assertions.
- `tests/Sussudio.Tests/MainWindow.ControllerOwnership.Capture.SelectionBindings.Tests.cs`
  owns capture selection binding XAML-adapter and controller-shell ownership
  assertions.
- `tests/Sussudio.Tests/MainWindow.ControllerOwnership.Capture.SelectionBindings.PropertyRouter.Tests.cs`
  owns capture selection `PropertyChanged` router ownership assertions.
- `tests/Sussudio.Tests/MainWindow.ControllerOwnership.Capture.SelectionBindings.CollectionSync.Tests.cs`
  owns capture selection collection sync, queued sync, and available-option
  rebinding ownership assertions.
- `tests/Sussudio.Tests/MainWindow.ControllerOwnership.Capture.SelectionBindings.SelectionOwners.Tests.cs`
  owns capture device, audio input, capture mode, recording selection, and
  selection-normalizer placement assertions.
- `tests/Sussudio.Tests/MainWindow.ControllerOwnership.Capture.SelectionBindings.DeviceAudio.Tests.cs`
  owns capture selection device-audio projection ownership assertions.
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
- `tests/Sussudio.Tests/MainWindow.ControllerOwnership.Screenshot.Tests.cs`
  owns MainWindow preview screenshot workflow, preview screenshot text-policy,
  and whole-window screenshot ownership assertions.
- `tests/Sussudio.Tests/MainWindow.FlashbackOwnership.Polling.Tests.cs` owns
  Flashback status/playback polling controller ownership assertions.
- `tests/Sussudio.Tests/MainWindow.FlashbackOwnership.TimelinePresentation.Tests.cs`
  owns Flashback timeline track layout, marker presentation, and export progress
  presentation controller ownership assertions.
- `tests/Sussudio.Tests/MainWindow.FlashbackOwnership.Playhead.Tests.cs` owns
  Flashback playhead/CTI motion controller ownership assertions.
- `tests/Sussudio.Tests/MainWindow.FlashbackOwnership.PlaybackPresentation.Tests.cs`
  owns Flashback playback presentation/coordinator ownership assertions.
- `tests/Sussudio.Tests/MainWindow.FlashbackOwnership.Settings.Tests.cs` owns
  Flashback settings binding and command controller ownership assertions.
- `tests/Sussudio.Tests/MainWindow.FlashbackOwnership.Helpers.cs` owns the
  shared source reader for the consolidated Flashback interaction and
  presentation adapter files.
- `tests/Sussudio.Tests/MainViewModel.Automation.Preview.Tests.cs` owns
  automation preview enable/disable and start/stop routing through the preview
  lifecycle controller.
- `tests/Sussudio.Tests/MainViewModel.Automation.Hdr.Tests.cs` owns
  automation HDR/true-HDR preview enablement guard assertions in the
  capture-mode transaction owner plus HDR mode change side-effect ownership
  assertions.
- `tests/Sussudio.Tests/MainViewModel.Automation.PreviewVolume.Tests.cs` owns
  preview-volume persistence and automation options surface assertions.
- `tests/Sussudio.Tests/MainViewModel.Automation.RecordingTransition.Tests.cs`
  owns automation recording routing through the shared transition gate and
  recording runtime ownership assertions.
- `tests/Sussudio.Tests/MainViewModel.Automation.RecordingTransition.Failures.Tests.cs`
  owns recording start/stop failure propagation assertions.
- `tests/Sussudio.Tests/MainViewModel.Automation.RecordingTransition.EmergencyStop.Tests.cs`
  owns emergency recording-stop dispatcher/coordinator routing assertions.
- `tests/Sussudio.Tests/MainViewModel.Automation.RecordingTransition.Bitrate.Tests.cs`
  owns bounded bitrate sample-window behavior assertions.
- `tests/Sussudio.Tests/MainViewModel.Automation.RecordingSettingsFlashbackCycle.Tests.cs`
  owns recording-setting automation routing for Flashback encoder cycles.
- `tests/Sussudio.Tests/MainViewModel.Automation.AsyncSurface.Tests.cs` owns
  the `IAutomationViewModel` async surface contract plus Flashback/probe
  dispatcher routing and UI-dispatch cancellation disposal assertions.
- `tests/Sussudio.Tests/MainViewModel.Automation.Audio.Tests.cs` owns
  automation audio/microphone command entry-point, microphone monitor
  suppression, and runtime-guard assertions.
- `tests/Sussudio.Tests/MainViewModel.Automation.UiSettings.Tests.cs` owns
  automation UI-setting persistence and frame-time/stat visibility contracts.
- `tests/Sussudio.Tests/MainViewModel.Automation.CaptureMode.Tests.cs` owns
  automation capture-mode reinitialization, device refresh, and
  device/audio-input selection routing contracts.
- `tests/Sussudio.Tests/MainViewModel.Automation.DiagnosticsRefresh.Tests.cs`
  owns the serialized diagnostics refresh ownership check and coordinates the
  focused diagnostics refresh assertion helpers.
- `tests/Sussudio.Tests/MainViewModel.Automation.DiagnosticsRefresh.RefreshPipeline.Tests.cs`
  owns diagnostics refresh pipeline, refresh gate, snapshot, and dispatcher
  ownership assertions.
- `tests/Sussudio.Tests/MainViewModel.Automation.DiagnosticsRefresh.AlertEvents.Tests.cs`
  owns diagnostics alert/event ownership assertions for UpdateAlerts,
  diagnostic events, signal alerts, and Flashback alert routing.
- `tests/Sussudio.Tests/MainViewModel.Automation.DiagnosticsRefresh.FlashbackAlerts.RecordingAndStorage.Tests.cs`
  owns Flashback export, storage, recording, and force-rotate alert coverage
  for the diagnostics refresh family.
- `tests/Sussudio.Tests/MainViewModel.Automation.DiagnosticsRefresh.FlashbackAlerts.PlaybackAndPreview.Tests.cs`
  owns Flashback playback, preview cadence, MJPEG, and renderer alert coverage
  for the diagnostics refresh family.
- `tests/Sussudio.Tests/MainViewModel.Automation.DiagnosticsRefresh.FlashbackExport.Tests.cs`
  owns capture-service and dispatcher Flashback export operation ownership
  assertions used by diagnostics refresh.
- `tests/Sussudio.Tests/MainViewModel.Automation.DiagnosticsRefresh.CoreOwnership.Tests.cs`
  owns the diagnostics refresh core ownership orchestrator.
- `tests/Sussudio.Tests/MainViewModel.Automation.DiagnosticsRefresh.EvaluationOwnership.Tests.cs`
  owns diagnostics refresh evaluation-policy, diagnostic evaluation, realtime
  and Flashback diagnostic lane ownership assertions.
- `tests/Sussudio.Tests/MainViewModel.Automation.DiagnosticsRefresh.RuntimeOwnership.Tests.cs`
  owns diagnostics refresh verification, snapshot query-port, preview pacing,
  lifecycle, and HDR ownership assertions.
- `tests/Sussudio.Tests/MainViewModel.Automation.DiagnosticsRefresh.SnapshotConstructionOwnership.Tests.cs`
  owns initial snapshot, BuildAutomationSnapshot composition, and snapshot
  flattening ownership assertions.
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
  owns diagnostics-refresh snapshot projection integration wiring: the refresh
  path must route through the named projection-set composition and flattened
  projection handoff instead of reasserting every leaf projection contract.
- `tests/Sussudio.Tests/MainViewModel.Automation.DiagnosticsRefresh.SourceFamily.cs`
  owns the diagnostics hub source-family reader and core/runtime source-text
  fields used by refresh ownership assertions. Its focused partials own the
  large grouped helper surfaces:
  `tests/Sussudio.Tests/MainViewModel.Automation.DiagnosticsRefresh.SourceFamily.DiagnosticEvaluation.cs`
  owns diagnostic evaluation and diagnostic lane source fields;
  `tests/Sussudio.Tests/MainViewModel.Automation.DiagnosticsRefresh.SourceFamily.Alerts.cs`
  owns signal, Flashback recording, and Flashback playback alert source fields;
  `tests/Sussudio.Tests/MainViewModel.Automation.DiagnosticsRefresh.SourceFamily.SnapshotProjection.cs`
  owns snapshot projection and projection-flattening source fields; and
  `tests/Sussudio.Tests/MainViewModel.Automation.DiagnosticsRefresh.SourceFamily.Aggregate.cs`
  owns the aggregate `SourceFamilyText` composition.
- `tests/Sussudio.Tests/MainViewModel.Automation.DiagnosticsRefresh.SourceReaders.cs`
  owns diagnostics refresh source/fixture readers for capture service, source
  reader, and tool-surface source text used by refresh ownership assertions.
  `tests/Sussudio.Tests/DiagnosticSession.SourceReaders.cs` owns shared
  diagnostic-session source-family readers used by refresh, MCP, and tool
  ownership assertions.
- `tests/Sussudio.Tests/MainViewModel.Automation.DiagnosticsRefresh.PreviewRuntime.Tests.cs`
  owns diagnostics snapshot preview runtime projection ownership assertions.
- `tests/Sussudio.Tests/MainViewModel.Automation.DiagnosticsRefresh.Hdr.Tests.cs`
  owns diagnostics HDR truth verdict behavior.
- `tests/Sussudio.Tests/MainViewModel.Automation.DiagnosticsLoop.Tests.cs`
  owns diagnostics-loop polling contracts that keep options snapshots out of
  hot diagnostics refresh paths.
- `tests/Sussudio.Tests/XUnit.AutomationDiagnosticsLoopContractsTests.cs`
  owns xUnit execution for the former legacy diagnostics-loop polling catalog
  check.
- Keep new automation diagnostics projection ownership assertions in the focused
  owner files; do not rebuild the old mega refresh assertion there:
  `MainViewModel.Automation.DiagnosticsProjection.Snapshot.Tests.cs`,
  `.Audio.Tests.cs`, `.Capture.Tests.cs`,
  `.CaptureFormatTransport.Tests.cs`, `.SourceCadence.Tests.cs`,
  `.Mjpeg.Tests.cs`,
  `.Recording.Tests.cs`, `.System.Tests.cs`, `.Preview.Tests.cs`, and
  `.Flashback.Tests.cs`.
- `tests/Sussudio.Tests/MainViewModel.Automation.FlashbackCleanup.Tests.cs`
  owns Flashback startup-cache and session-recovery cleanup ownership
  assertions that used to live in the automation test catch-all.
- `tests/Sussudio.Tests/MainViewModel.Capture.SettingsProjection.Tests.cs`
  owns capture settings projection ownership assertions, including the focused
  frame-rate request projector used by `BuildCaptureSettings`.
- `tests/Sussudio.Tests/MainViewModel.Capture.AudioMonitoring.Tests.cs` owns
  capture audio-monitoring coordinator surface assertions.
- `tests/Sussudio.Tests/MainViewModel.AudioControls.GainAndMonitoring.Tests.cs`
  owns analog gain curve mapping and preview audio monitoring volume-ramp
  ownership assertions.
- `tests/Sussudio.Tests/MainViewModel.AudioControls.DeviceAudio.Tests.cs` owns
  device audio refresh, saved-state guard, and device-audio request-controller
  ownership assertions.
- `tests/Sussudio.Tests/MainViewModel.NativeXuAudioControlService.AudioMeters.Tests.cs`
  owns native XU audio-control service cohesion, profile, payload workflow, raw
  transport ownership, and audio meter callback-state assertions.
- `tests/Sussudio.Tests/XUnit.PresentationPreviewMainViewModelAudioControlsContractsTests.cs`
  owns the xUnit execution surface for MainViewModel audio-control source and
  behavior contracts after their removal from the legacy offline harness catalog.
- `tests/Sussudio.Tests/MainViewModel.DependencyComposition.Tests.cs` owns the
  MainViewModel dependency-composition seam assertions for root construction,
  controller graph creation, state partial ownership, and default dependency
  factory wiring.
- `tests/Sussudio.Tests/MainViewModel.DependencyComposition.UiDispatch.Tests.cs`
  owns the MainViewModel UI-dispatch controller graph and dependency-context
  assertions.
- `tests/Sussudio.Tests/MainViewModel.DependencyComposition.Presentation.Tests.cs`
  owns the MainViewModel preview lifecycle, preview reinitialize, preview-state,
  and presentation controller graph dependency-context assertions.
- `tests/Sussudio.Tests/MainViewModel.DependencyComposition.Recording.Tests.cs`
  owns the MainViewModel recording-transition controller graph and
  dependency-context assertions.
- `tests/Sussudio.Tests/MainViewModel.DependencyComposition.CaptureDevice.Tests.cs`
  owns the MainViewModel capture/device composition assertions for device
  refresh, device-native audio, capture mode rebuild, capture settings
  automation, recording settings/capability, and late format-probe retarget
  controller contexts.
- `tests/Sussudio.Tests/MainViewModel.DependencyComposition.Runtime.Tests.cs`
  owns the MainViewModel source-telemetry, runtime lifecycle, runtime event
  ingress, subscription, and disposal controller dependency-context assertions.
- `tests/Sussudio.Tests/MainViewModel.Capture.OutputPath.Tests.cs` owns
  assertions that output folder picker ownership stays out of `MainViewModel`.
- `tests/Sussudio.Tests/MainViewModel.Capture.TestHelpers.cs` owns shared
  MainViewModel source-inspection helpers for capture-facing tests.
- `tests/Sussudio.Tests/MainViewModel.Capture.SelectionPolicy.FrameRates.Ownership.Tests.cs`
  owns frame-rate source-filter, automatic-selection, always-on capture-option,
  and timing-policy ownership assertions.
- `tests/Sussudio.Tests/MainViewModel.Capture.SelectionPolicy.FrameRates.PolicyBehavior.Tests.cs`
  owns automatic frame-rate choice and pure timing-policy behavior assertions.
- `tests/Sussudio.Tests/XUnit.PresentationPreviewFrameRateSelectionContractsTests.cs`
  owns xUnit execution for the former legacy presentation-preview frame-rate
  selection/timing catalog group.
- `tests/Sussudio.Tests/MainViewModel.Capture.SelectionPolicy.VideoFormat.Tests.cs`
  owns selected capture-format and mode-tuple video-format filtering policy
  assertions.
- `tests/Sussudio.Tests/MainViewModel.Capture.SelectionPolicy.Resolution.Ownership.Tests.cs`
  owns resolution-selection source-shape assertions for option rebuild,
  auto-selection state, and pure policy placement.
- `tests/Sussudio.Tests/MainViewModel.Capture.SelectionPolicy.Resolution.Behavior.Tests.cs`
  owns resolution-selection policy behavior assertions, including HDR and SDR
  source retarget behavior.
- `tests/Sussudio.Tests/XUnit.PresentationPreviewResolutionSelectionContractsTests.cs`
  owns xUnit execution for the former legacy presentation-preview
  resolution-selection catalog group.
- `tests/Sussudio.Tests/MainViewModel.Capture.SelectionPolicy.ModeSelection.Tests.cs`
  owns mode-selection reset and resolved automatic frame-rate application
  assertions.
- `tests/Sussudio.Tests/MainViewModel.Capture.SelectionPolicy.DeviceFormatProbeRetarget.Tests.cs`
  owns late device-format probe retarget policy behavior assertions.
- `tests/Sussudio.Tests/XUnit.PresentationPreviewDeviceFormatProbeRetargetContractsTests.cs`
  owns xUnit execution for the former legacy presentation-preview late
  device-format probe retarget catalog group.
- `tests/Sussudio.Tests/MainViewModel.Capture.SelectionPolicy.RecordingFormat.Tests.cs`
  owns recording format selection policy ownership assertions.
- `tests/Sussudio.Tests/XUnit.PresentationPreviewCaptureSelectionPolicyContractsTests.cs`
  owns xUnit execution for the former legacy presentation-preview
  mode-selection, capture-format, and recording-settings selection catalog group.
- `tests/Sussudio.Tests/MainViewModel.Capture.SelectionPolicy.RuntimeFlags.Tests.cs`
  owns runtime error-projection ownership assertions.
- `tests/Sussudio.Tests/MainViewModel.Capture.SelectionPolicy.Helpers.cs` owns
  shared reflection, option-list, and capture-mode model construction helpers
  for the selection-policy test family.
- `tests/Sussudio.Tests/MainViewModel.Capture.PreviewStartup.Watchdog.Tests.cs`
  owns preview startup watchdog controller/adapter ownership, timeout, and
  failure-stop contract assertions.
- `tests/Sussudio.Tests/MainViewModel.Capture.PreviewStartup.SessionReinit.Tests.cs`
  owns the source-shape ownership assertion that wires preview startup session
  and reinit adapters to focused controllers.
- `tests/Sussudio.Tests/MainViewModel.Capture.PreviewStartup.SessionController.Tests.cs`
  owns preview startup session controller attempt-state and orchestration
  behavior assertions.
- `tests/Sussudio.Tests/MainViewModel.Capture.PreviewStartup.ReinitTransition.Tests.cs`
  owns preview reinit transition controller presentation and animation-state
  behavior assertions.
- `tests/Sussudio.Tests/MainViewModel.Capture.PreviewStartup.ReinitFlashbackCycle.Tests.cs`
  owns ViewModel preview reinitialization waiting for pending Flashback encoder
  settings cycles.
- `tests/Sussudio.Tests/MainViewModel.Capture.PreviewStartup.Signals.Tests.cs`
  owns preview startup signal controller/adapter ownership, readiness-signal
  controller, and startup/failure formatter assertions.
- `tests/Sussudio.Tests/MainViewModel.Capture.PreviewStartup.StartupStopOrdering.Tests.cs`
  owns preview lifecycle-event and fade-in source-shape ownership assertions.
- `tests/Sussudio.Tests/XUnit.PresentationPreviewStartupOwnershipContractsTests.cs`
  owns xUnit execution for the former legacy presentation-preview preview-startup
  source-shape ownership catalog group.
- `tests/Sussudio.Tests/XUnit.PresentationPreviewStartupBehaviorContractsTests.cs`
  owns xUnit execution for the former legacy presentation-preview preview-startup
  controller behavior catalog group.
- `tests/Sussudio.Tests/XUnit.PresentationPreviewStartupSignalContractsTests.cs`
  owns xUnit execution for the former legacy presentation-preview preview-startup
  signal/failure text catalog group.
- `tests/Sussudio.Tests/XUnit.PresentationPreviewCapturePreviewLifecycleContractsTests.cs`
  owns xUnit execution for the former legacy presentation-preview
  capture preview-lifecycle/audio-fallback catalog group.
- `tests/Sussudio.Tests/MainViewModel.Capture.PreviewStartup.DeviceDiscoveryOrdering.Tests.cs`
  owns startup ordering assertions that device discovery begins before
  recording-capability probe completion.
- `tests/Sussudio.Tests/MainViewModel.Capture.PreviewStartup.PreviewRevealOrdering.Tests.cs`
  owns preview reveal priming assertions for UI, audio, fade-in, launch, and
  unavailable-placeholder ordering.
- `tests/Sussudio.Tests/MainViewModel.Capture.PreviewStartup.StopAudioRampOrdering.Tests.cs`
  owns preview stop ordering assertions that audio ramps down before preview
  teardown.
- `tests/Sussudio.Tests/XUnit.PresentationPreviewStartupOrderingContractsTests.cs`
  owns xUnit execution for the former legacy presentation-preview preview
  startup ordering catalog group.
- `tests/Sussudio.Tests/MainWindow.PreviewStartupOwnership.Helpers.cs` owns
  the shared source reader for the split `MainWindow.PreviewStartup.*.cs`
  adapter family.
- `tests/Sussudio.Tests/MainWindow.PreviewTransitionsOwnership.Helpers.cs` owns
  the shared source reader for the consolidated `MainWindow.PreviewTransitions.Composition.cs`
  adapter.
- `tests/Sussudio.Tests/MainWindow.ShellChromeOwnership.Helpers.cs` owns the
  shared source reader for the consolidated `MainWindow.ShellChrome.Composition.cs`
  adapter.
- `tests/Sussudio.Tests/MainWindow.StatsOverlayOwnership.Helpers.cs` owns the
  shared source reader for the consolidated `MainWindow.StatsOverlay.Composition.cs`
  adapter.
- `tests/Sussudio.Tests/MainWindow.CaptureSelectionBindingsOwnership.Helpers.cs`
  owns the shared source reader for the split
  `MainWindow.CaptureBindings.cs` adapter.
- `tests/Sussudio.Tests/MainWindow.FullScreenOwnership.Helpers.cs` owns the
  shared source reader for the consolidated `MainWindow.ShellChrome.Composition.cs`
  adapter.
- `tests/Sussudio.Tests/MainWindow.PreviewRendererOwnership.Helpers.cs` owns the
  shared source reader for the consolidated `MainWindow.PreviewRenderer.Composition.cs`
  adapter.
- `tests/Sussudio.Tests/MainWindow.ShutdownCleanupOwnership.Helpers.cs` owns the
  shared source reader for shutdown cleanup delegates now folded into
  `MainWindow.xaml.cs`.
- `tests/Sussudio.Tests/MainWindow.PropertyChangedPreviewOwnership.Helpers.cs`
  owns the shared source reader for the consolidated
  preview lifecycle adapter in `MainWindow.PreviewRenderer.Composition.cs`.
- `tests/Sussudio.Tests/MainViewModel.Capture.FlashbackExport.Tests.cs` owns
  Flashback export backend-lease, export-operation lock, ViewModel export
  routing, and export CTS lifecycle assertions.
- `tests/Sussudio.Tests/MainViewModel.Capture.FlashbackRouting.ViewModel.Tests.cs`
  owns MainViewModel Flashback coordinator-routing assertions, negative
  `_captureService` access guards, and the Flashback settings owner for
  automation enable/restart entry points.
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
- `tests/Sussudio.Tests/XUnit.SmallContractsTests.cs` owns ported audio input,
  audio level event, capture device metadata/default collection, and automation
  window action enum contract checks.
- `tests/Sussudio.Tests/XUnit.MediaFormatTests.cs` owns MediaFormat
  equality and hash-code contract checks.
- `tests/Sussudio.Tests/XUnit.SnapshotModelsTests.cs` and its
  `SnapshotModels.*` partials own the xUnit snapshot-model contract suite.
  `SnapshotModels.Tests.cs` owns shared snapshot-model spec DTOs and
  registration state; `SnapshotModels.PropertyAssertions.cs` owns shared
  property-list, nullability, and helper assertion methods; and
  `SnapshotModels.ReflectionJson.cs` owns shared reflection JSON round-trip and
  registered-property coverage helpers.
- `tests/Sussudio.Tests/SnapshotModels.Automation.CpuMjpeg.Tests.cs` owns
  xUnit automation snapshot CPU MJPEG metric shape checks.
- `tests/Sussudio.Tests/SnapshotModels.Automation.MjpegPreview.Tests.cs` owns
  xUnit automation snapshot MJPEG preview jitter and packet-hash metric shape
  checks.
- `tests/Sussudio.Tests/SnapshotModels.Automation.PreviewDiagnostics.Tests.cs`
  owns xUnit automation snapshot preview D3D, diagnostic lane, and preview
  pacing metric shape checks.
- `tests/Sussudio.Tests/SnapshotModels.Automation.CaptureCommands.Tests.cs`
  owns xUnit automation snapshot capture-command queue metric shape checks.
- `tests/Sussudio.Tests/SnapshotModels.Automation.Recording.Tests.cs` owns
  xUnit automation snapshot recording video metric shape checks.
- `tests/Sussudio.Tests/SnapshotModels.Automation.FlashbackRecording.Tests.cs`
  owns xUnit automation snapshot Flashback recording, cleanup, backend, and
  queue metric shape checks.
- `tests/Sussudio.Tests/SnapshotModels.Automation.FlashbackPlayback.Tests.cs`
  owns xUnit automation snapshot Flashback playback, cadence, decode, and
  command metric shape checks.
- `tests/Sussudio.Tests/SnapshotModels.Automation.FlashbackExport.Tests.cs`
  owns xUnit automation snapshot Flashback export metric shape checks.
- `tests/Sussudio.Tests/SnapshotModels.Automation.VisualCadence.Tests.cs`
  owns xUnit automation snapshot visual cadence metric shape checks.
- `tests/Sussudio.Tests/SnapshotModels.Automation.Options.Tests.cs` owns
  xUnit automation options DTO shape checks.
- `tests/Sussudio.Tests/SnapshotModels.Automation.CpuMjpegContractSpec.cs` owns
  the CPU MJPEG automation snapshot property-list contract plus shared
  AutomationSnapshot property assertions used by focused automation snapshot
  shape checks.
- `tests/Sussudio.Tests/SnapshotModels.CaptureDiagnostics.Tests.cs` owns xUnit
  CaptureDiagnosticsSnapshot default, round-trip, reflection JSON, and MJPEG
  source-ownership checks.
- `tests/Sussudio.Tests/SnapshotModels.CaptureDiagnostics.PropertySpec.cs`
  owns the CaptureDiagnosticsSnapshot registered property spec.
- `tests/Sussudio.Tests/SnapshotModels.CaptureHealth.Tests.cs` owns xUnit
  CaptureHealthSnapshot registered orchestration and source-ownership checks;
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
- `tests/Sussudio.Tests/XUnit.SnapshotModelsTests.cs` owns the
  SourceSignalTelemetrySnapshot and source telemetry automation projection
  contract checks.
- `tests/Sussudio.Tests/NativeXuAtCommandProvider.Tests.cs` owns Native XU
  telemetry provider ownership, rolling command-group split, shared snapshot
  assembly ownership, and supported 4K X product-revision checks.
- `tests/Sussudio.Tests/CaptureDiscovery.SourceOwnership.Tests.cs` owns
  DeviceService scoring, source-reader negotiation/interop ownership, and MF
  symbolic-link matching assertions.
- `tests/Sussudio.Tests/XUnit.CapturePoliciesTests.cs` owns the ported
  HdrOutputPolicy and HDR output environment-switch behavior checks.
- `tests/Sussudio.Tests/RecordingQueue.Tests.cs` owns shared recording queue
  source readers and source-block extraction helpers.
- `tests/Sussudio.Tests/RecordingQueue.OverloadPolicy.Tests.cs` owns the
  recording/Flashback queue overload, fatal-failure, lifecycle, and recording
  backend start-policy assertion.
- `tests/Sussudio.Tests/RecordingQueue.OverloadPolicy.Finalize.Tests.cs` owns
  recording backend finalization, Flashback cleanup, microphone restart, and
  post-finalize telemetry assertions. `RecordingQueue.OverloadPolicy.SourceReaders.cs`
  owns source-loading setup for the overload policy assertions.
- `tests/Sussudio.Tests/RecordingQueue.OverloadPolicy.BufferCycle.Tests.cs`
  owns Flashback buffer-cycle committed-token, detach/attach, playback
  controller handoff, and recording-backend ownership policy assertions.
  `RecordingQueue.OverloadPolicy.LibAvSpec.cs` owns the LibAv overload and
  queue-depth assertion subgroup.
  `RecordingQueue.OverloadPolicy.FlashbackSpec.cs` owns the Flashback overload,
  fatal-failure, queue-depth, and frame-validation assertion subgroup.
  `RecordingQueue.OverloadPolicy.FlashbackBuffer.cs` owns Flashback buffer
  recovery, eviction, active-segment, and enqueue-gating assertions.
  `RecordingQueue.OverloadPolicy.Telemetry.cs` owns unified capture, health
  snapshot, and automation formatter telemetry assertions.
  `CaptureService.RecordingLifecycleOwnership.Tests.cs` owns CaptureService
  recording lifecycle, recording-stop finalization failure propagation, and
  active recording backend resource aggregate ownership assertions.
  `CaptureService.RecordingOutcomeOwnership.Tests.cs` owns CaptureService
  recording start rollback and recording outcome-state file-ownership
  assertions.
- `tests/Sussudio.Tests/RecordingQueue.LibAvSink.Queue.Tests.cs` owns LibAv
  recording sink try-enqueue, video-queue submission, audio queue, and
  queue-cleanup ownership assertions.
- `tests/Sussudio.Tests/RecordingQueue.LibAvSink.Lifecycle.Tests.cs` owns
  LibAv recording sink output validation, video-session setup, drain-loop,
  encoding-loop, startup sequencing, stop-lifecycle, and lifetime-helper
  ownership assertions.
- `tests/Sussudio.Tests/RecordingQueue.Wasapi.Tests.cs` owns WASAPI capture-loop, hot-write,
  conversion, diagnostics, COM contract, and bounded stop assertions.
- `tests/Sussudio.Tests/RecordingQueue.CaptureFanout.Tests.cs` owns
  UnifiedVideoCapture frame-ingress, sink fan-out, and CaptureService Flashback
  backend aggregate ownership assertions.
- `tests/Sussudio.Tests/CaptureService.FlashbackOrchestrationSource.Tests.cs`
  owns the source family helper for Flashback backend orchestration partials
  and recording finalization partials plus the focused-partial ownership
  contracts, including LibAv live-preview restoration and recording
  outcome-state ownership.
- `tests/Sussudio.Tests/CaptureService.AudioOwnershipSource.Tests.cs` owns
  CaptureService audio source-family helpers, audio focused-partial ownership,
  PreviewAudioGraphResources ownership, and post-recording microphone monitor
  restart assertions.
  `tests/Sussudio.Tests/XUnit.RecordingPipelineContractsTests.cs` owns the
  xUnit execution surface for these recording queue, LibAv sink, WASAPI,
  capture fan-out, and CaptureService recording ownership contracts after their
  removal from the legacy harness catalog.
- `tests/Sussudio.Tests/CaptureCadence.Tests.cs` owns packet-hash duplicate
  cadence and visual-cadence crop sampling assertions.
- `tests/Sussudio.Tests/UnifiedVideoCapture.Runtime.Tests.cs` owns
  UnifiedVideoCapture CPU-MJPEG format reporting and stop-failure retention
  behavior scenarios.
- `tests/Sussudio.Tests/MjpegPipeline.Timing.Tests.cs` owns CPU MJPEG timing
  metric math, stopwatch timeout helpers, and timing/decoder shape checks.
- `tests/Sussudio.Tests/MjpegPipeline.Tests.cs` owns CPU MJPEG pipeline
  source-shape, focused-partial ownership, startup-drop, known-loss, and
  shared-reorder behavior checks.
  `tests/Sussudio.Tests/XUnit.MjpegPipelineContractsTests.cs` owns the xUnit
  execution surface for these CPU MJPEG runtime, cadence, pooled-frame,
  preview-jitter, and queued lease-release contracts after their removal from
  the legacy harness catalog.
- `tests/Sussudio.Tests/CaptureService.RuntimeSnapshots.Behavior.Tests.cs` owns
  CaptureService runtime snapshot behavior scenarios for observed formats,
  source-telemetry alignment, HDR pipeline parity, and inactive thread probes.
- `tests/Sussudio.Tests/CaptureService.RuntimeSnapshots.ProjectionOwnership.Tests.cs`
  owns runtime projection ownership for ingest/audio, reader transport, HDR
  pipeline, source telemetry, and recording integrity.
- `tests/Sussudio.Tests/RecordingIntegrity.Contracts.Tests.cs` owns recording
  integrity summary defaults, automation snapshot field contracts, and
  automation projection ownership checks.
- `tests/Sussudio.Tests/RecordingIntegrity.Tests.cs` owns recording integrity
  summary policy, Flashback recording scoped sequence gaps, CaptureService
  focused-partial ownership, and shared formatter rendering checks.
- `tests/Sussudio.Tests/CaptureService.Snapshots.Tests.cs` owns CaptureService
  diagnostics-snapshot compatibility, recording format/profile helper, HDR
  warmup-state, and recording-stats ownership assertions.
- `tests/Sussudio.Tests/CaptureService.Snapshots.Telemetry.Tests.cs` owns
  CaptureService observed pixel telemetry, source telemetry backend/circuit,
  tick-age, and telemetry-alignment helper assertions.
- `tests/Sussudio.Tests/CaptureService.PreviewLifecycle.Tests.cs` owns
  video-only preview fallback, missing audio endpoint, preview-stop API surface,
  and preview backend log contracts.
- `tests/Sussudio.Tests/CaptureService.InitializationOwnership.Tests.cs` owns
  the CaptureService initialization focused-partial ownership contract.
- `tests/Sussudio.Tests/CaptureService.Failures.Tests.cs` owns CaptureService
  last-failure telemetry source placement, capture fatal cleanup, Flashback
  backend failure cleanup source placement, and faulted-session state
  assertions.
- `tests/Sussudio.Tests/CaptureService.SessionStateOwnership.Tests.cs` owns
  CaptureService session-state-machine ownership, asserts that lifecycle
  partials route state changes through transition/state-machine helpers, and
  keeps the no-direct-session-state-write guard for failure cleanup.
- `tests/Sussudio.Tests/CaptureService.HealthSnapshots.AssemblyAndSamplerOwnership.Tests.cs`
  owns CaptureService health snapshot assembly, capture-cadence, MJPEG, and
  AV-sync ownership assertions plus shared health snapshot assertion helpers.
- `tests/Sussudio.Tests/CaptureService.HealthSnapshots.FlashbackOwnership.Tests.cs`
  owns CaptureService health snapshot Flashback export, buffer/backend, queue,
  and playback ownership assertions.
- `tests/Sussudio.Tests/CaptureService.HealthSnapshots.RecordingAndSourceTelemetryOwnership.Tests.cs`
  owns CaptureService health snapshot recording and source-telemetry ownership
  assertions plus the structured source telemetry behavior scenario.
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
  screenshot black-edge counting, and preview PNG encoder CRC/capture contract
  tests.
- `tests/Sussudio.Tests/D3D11PreviewRenderer.Cadence.Tests.cs` owns present
  cadence metric shape and suppression baseline tests.
- `tests/Sussudio.Tests/D3D11PreviewRenderer.DiagnosticsContract.Tests.cs`
  owns renderer diagnostics source-shape, frame queue, frame ownership, and
  public renderer diagnostics API contract assertions.
- `tests/Sussudio.Tests/D3D11PreviewRenderer.DiagnosticsContract.SnapshotModels.Tests.cs`
  owns preview runtime, automation snapshot, nested renderer metrics, preview
  tracking, and slow-frame diagnostic reflection contracts.
- `tests/Sussudio.Tests/D3D11PreviewRenderer.DiagnosticsContract.PerformanceTimeline.Tests.cs`
  owns `PerformanceTimelineEntry.cs` preview, Flashback playback, Flashback
  export, and process diagnostics source-shape plus reflection contracts.
- `tests/Sussudio.Tests/D3D11PreviewRenderer.SourceOwnership.ContractsAndMetrics.Tests.cs` owns
  configuration, native interop, frame type/ownership state, DXGI frame-stat
  state, slow-frame state, metric-window lifecycle, and metric-tracking
  method/state assertions.
  `D3D11PreviewRenderer.SourceOwnership.RenderSetup.Tests.cs` owns panel
  binding, shared-device, device initialization, input-resource, input-view,
  and raw-upload state assertions.
  `D3D11PreviewRenderer.SourceOwnership.RenderPasses.Tests.cs` owns
  frame-latency, viewport, render-pass, shader-rendering, and shader-source
  assertions.
  `D3D11PreviewRenderer.SourceOwnership.RenderThread.Tests.cs` owns
  render-thread loop, first-frame notification, failure telemetry method/state,
  and Present shared present-accounting source-ownership assertions.
  `D3D11PreviewRenderer.SourceOwnership.RuntimeCapture.Tests.cs` owns public
  submission state, lifecycle/stop-lifecycle state, pending-frame state, and
  screenshot assertions.
- `tests/Sussudio.Tests/D3D11PreviewRenderer.DeviceLost.Tests.cs` owns device
  lost classification and recovery ownership assertions.
- `tests/Sussudio.Tests/D3D11PreviewRenderer.FrameFlow.Tests.cs` owns pending
  frame draining, frame-capture cancellation, and shared D3D device reference
  lifecycle assertions.
- `tests/Sussudio.Tests/GpuTelemetry.Nvml.Tests.cs` owns NVML snapshot
  computed-property/unit-conversion checks and `NvmlMonitor` native interop
  ownership assertions.
- `tests/Sussudio.Tests/RuntimeContracts.Tests.cs` owns RuntimePaths,
  RuntimePaths resolution-policy source ownership, FFmpeg runtime location,
  bounded external process supervision, MMCSS registration, ProcessSpec, and
  ProcessRunResult contract checks.
  `FfmpegRuntimeLocator.cs` owns app-local/PATH runtime and tool resolution plus
  cached FFmpeg encoder/split-encode capability probes through bounded
  `ProcessSupervisor` calls.
  `FfmpegRuntimeInit.cs` owns one-time native initialization, FFmpeg log callback
  routing, and recoverable seek-log suppression.
- `tests/Sussudio.Tests/ProjectBuildContracts.Tests.cs` owns project-file build
  and publish policy contract helpers. `tests/Sussudio.Tests/XUnit.ProjectBuildContractsTests.cs`
  owns xUnit execution for those checks after their removal from the legacy
  offline harness catalog.
- `tests/Sussudio.Tests/XUnit.RecordingContractsTests.cs` owns recording
  service contract DTO checks such as GpuPipelineHandles,
  RecordingContextRequest, FinalizeResult, and RecordingStats.
- `tests/Sussudio.Tests/RecordingArtifactManager.Tests.cs` owns xUnit temp
  artifact finalize/rollback behavior for recording output cleanup.
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
- `tests/Sussudio.Tests/AutomationCommandDispatcher.Payload.Extraction.Tests.cs`
  owns dispatcher JSON payload extraction helper coverage.
- `tests/Sussudio.Tests/AutomationCommandDispatcher.Payload.Catalog.Tests.cs`
  owns dispatcher payload defaults, trivial-handler payload-field parity
  checks against `AutomationCommandCatalog`, and the custom
  `GetAudioRampTrace.maxEntries` metadata guardrail.
- `tests/Sussudio.Tests/AutomationCommandDispatcher.Readiness.Tests.cs` owns
  dispatcher readiness gating, window close, preview health, stale wait-refresh
  cadence guards, and UI automation readiness-independent coverage.
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
- `tests/Sussudio.Tests/AutomationToolContracts.ProtocolXunit.Tests.cs` owns
  automation client timeout policy, advanced command-map alignment,
  pipe-connect failure, tool delegation, script freshness, and response-state
  contract tests. It uses `RuntimeContractSource.ReadAutomationPipeClientSource()`
  for the shared AutomationPipeClient source family.
- `tests/Sussudio.Tests/AutomationToolContracts.Catalog.Tests.cs` owns shared
  implementations for automation command catalog and command policy metadata
  contract tests.
- `tests/Sussudio.Tests/AutomationToolContracts.Manifest.Tests.cs` owns shared
  implementations for automation manifest projection, path policy validation,
  and manifest serialization contract tests.
- `tests/Sussudio.Tests/AutomationToolContracts.Reliability.Tests.cs` owns the
  shared implementation for the reliability-gates script contract test.
- `tests/Sussudio.Tests/XUnit.AutomationCatalogContractsTests.cs` owns the
  xUnit execution surface for catalog, manifest, path-policy, and
  reliability-gates checks after their removal from the legacy offline harness
  catalog.
- `tests/Sussudio.Tests/ArchitectureDocs.AgentMapOwnershipPaths.Tests.cs` owns
  shared implementations for consolidated AGENT_MAP reference drift,
  test-owner code-span, README automation consumer, UI/presentation ownership,
  CaptureService ownership, Flashback preview startup wording, shared tool
  automation exact-path, duplicate tools/Common owner, and empty test
  marker-shell checks.
  `tests/Sussudio.Tests/XUnit.ArchitectureDocsAgentMapOwnershipTests.cs` owns
  the xUnit execution surface for those AGENT_MAP ownership checks after their
  removal from the legacy offline harness catalog.
  `tests/Sussudio.Tests/ArchitectureDocs.ReferenceIntegrity.Tests.cs` owns
  literal `ReadRepoFile` source-shape path drift, cleanup-plan file/folder
  reference drift, and the shared implementation for xUnit migration inventory
  checks.
  `tests/Sussudio.Tests/XUnit.ArchitectureDocsReferenceIntegrityTests.cs` owns
  the xUnit execution surface for those pure architecture-doc reference checks
  after their removal from the legacy offline harness catalog.
  `tests/Sussudio.Tests/ArchitectureDocs.MarkdownReferenceHelpers.cs` owns
  shared Markdown code-span path-token extraction and resolution helpers.
  `tests/Sussudio.Tests/ArchitectureDocs.OwnershipFileEnumerators.cs` owns
  AGENT_MAP consumer coverage, ownership-file discovery, exact code-span policy,
  and xUnit inventory helpers.
- `tests/Sussudio.Tests/AutomationToolContracts.SnapshotFormatter*.Tests.cs`
  owns the shared/ssctl snapshot formatter contract family: typed accessors,
  core section formatting, section-order, and Flashback opt-in smoke checks
  stay in `.Tests.cs`; Flashback output rendering lives in `.Flashback.Tests.cs`,
  Preview D3D output rendering lives in `.PreviewD3D.Tests.cs`, and shared
  formatter source ownership lives in `.Ownership.Tests.cs`.
  `tests/Sussudio.Tests/XUnit.AutomationSnapshotFormatterContractsTests.cs`
  owns the xUnit execution surface for those shared snapshot formatter checks
  after their removal from the legacy offline harness catalog.
- `tests/Sussudio.Tests/Formatters.Tests.cs` owns ssctl formatted snapshot
  output smoke checks. `tests/Sussudio.Tests/Formatters.SnapshotOwnership.Tests.cs`
  owns ssctl formatter source ownership assertions, while
  `tests/Sussudio.Tests/Formatters.Timeline.Tests.cs` owns timeline table and
  summary output checks.
  `tests/Sussudio.Tests/XUnit.SsctlFormatterContractsTests.cs` owns the xUnit
  execution surface for those ssctl formatter checks after their removal from
  the legacy offline harness catalog.
- `tests/Sussudio.Tests/RuntimeContracts.Tests.cs` owns
  `RuntimeContractSource`, including shared tool source-family readers used by
  legacy harness and xUnit contract tests.
- `tests/Sussudio.Tests/CommandHandlers.Helpers.cs` owns source-family
  reader, routing-capture helpers, and `AssertSsctlCommandRequest`, which routes
  captured ssctl `request.command` checks through the shared golden command table
  instead of per-test numeric IDs. `CommandHandlers.Routing.Control.Tests.cs`
  owns pipe-captured routing contract checks for device, capture controls,
  recordings, window, and manifest commands.
  `CommandHandlers.Routing.Flashback.Tests.cs` owns Flashback and observability
  routing checks. `CommandHandlers.Routing.Workflow.Tests.cs` owns automation-flow,
  UI visibility, and verification routing checks.
  `CommandHandlers.SourceOwnership.Tests.cs` owns ssctl handler partial-family
  source ownership assertions, and `CommandHandlers.Help.Tests.cs` owns ssctl
  help/catalog force-flag coverage.
  `tests/Sussudio.Tests/XUnit.SsctlCommandHandlerContractsTests.cs` owns the
  xUnit execution surface for those command-handler routing, source ownership,
  and help checks after their removal from the legacy offline harness catalog.
- `tests/Sussudio.Tests/PresentMonProbe.Tests.cs` owns PresentMon parser
  behavior contracts for swap-chain selection, artifact filtering, CSV field
  versions, and app-present correlation.
- `tests/Sussudio.Tests/PresentMonProbe.SourceOwnership.Tests.cs` owns
  PresentMonProbe split-family source ownership assertions.
- `tests/Sussudio.Tests/XUnit.ToolProbeContractsTests.cs` owns the xUnit
  execution surface for PresentMon parser/source-ownership, ssctl pipe
  transport, KS audio-node, and EGAVDS probe checks after their removal from
  the legacy offline harness catalog.
- `tests/Sussudio.Tests/ToolAssemblyLoading.Helpers.cs` owns shared tool
  assembly loading, isolated load contexts, freshness checks, and tool build
  command mapping used by the legacy harness and xUnit slices.
- `tests/Sussudio.Tests/HarnessCheckCatalog.cs` owns ordered offline harness
  topic sequencing, shared catalog registration helpers, and the pass-through
  routing for automation diagnostics, presentation preview, MCP diagnostics,
  and remaining legacy catalog groups. Keep `Program.cs` as the runner, not the
  assertion registry.
- `tests/Sussudio.Tests/XUnit.CoreRuntimeContractsTests.cs` owns the former
  core-runtime registration group for runtime telemetry, capture-service
  snapshot, recording-integrity, NativeXu, frame-ledger, and basic app contract
  checks after their removal from the legacy offline harness catalog.
- `tests/Sussudio.Tests/XUnit.CoreRuntimeRecordingContractsTests.cs` owns the
  former core-runtime recording registration group for recording verifier,
  LibAv encoder, Flashback integrity, recording-facing shared formatter, and
  dedicated LibAv verification script checks after their removal from the
  legacy offline harness catalog.
- `Sussudio/Services/Runtime/RuntimeHelpers.cs` owns pure runtime helper types
  for AtomicMax, TelemetryAgeHelper, EnvironmentHelpers, and RingBufferHelpers.
  `tests/Sussudio.Tests/XUnit.RuntimeHelpersTests.cs` owns their behavior
  contracts.
- `tests/Sussudio.Tests/XUnit.AutomationAppSurfaceContractsTests.cs` owns the
  former automation-diagnostics app-surface registration group for App exception
  policy, converter/display formatting, LoggingJsonContext, MainWindow
  automation IDs and window/full-screen/dispatch adapters, pipe/auth policy,
  and Stream Deck auth-envelope checks after their removal from the legacy
  offline harness catalog.
- `tests/Sussudio.Tests/XUnit.AutomationViewModelFlashbackUiContractsTests.cs`
  owns the former automation-diagnostics ViewModel/Flashback UI registration
  group for automation command routes, async
  Flashback/probe surface, runtime snapshot ownership, scrub/toggle behavior,
  timeline geometry, and Flashback presentation controller ownership after
  their removal from the legacy offline harness catalog.
- `tests/Sussudio.Tests/XUnit.AutomationDispatcherContractsTests.cs` owns the
  former automation-diagnostics dispatcher registration group for payload
  parsing, catalog metadata, readiness classification, authorization, manifest,
  command coverage, and focused dispatcher command-owner checks after their
  removal from the legacy offline harness catalog.
- `tests/Sussudio.Tests/XUnit.AutomationCaptureFlashbackRoutingContractsTests.cs`
  owns the former automation-diagnostics capture/Flashback routing registration
  group for Flashback routing, capture transition policy, capture session
  coordinator contracts, service namespace/source ownership, and diagnostics
  snapshot refresh serialization after their removal from the legacy offline
  harness catalog.
- `tests/Sussudio.Tests/XUnit.AutomationSnapshotProjectionContractsTests.cs`
  owns the former automation-diagnostics snapshot-projection registration group
  for snapshot status/evaluation, audio, capture/settings, source/cadence,
  MJPEG, recording, process/A/V sync, preview, and Flashback projection
  ownership after their removal from the legacy offline harness catalog.
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
- `tests/Sussudio.Tests/ServiceNamespace.SourceOwnership.ServicesLayer.Tests.cs`
  owns DeviceService, NativeXu support, GPU interop, decoder, and capture
  telemetry source ownership assertions.
- `tests/Sussudio.Tests/ServiceNamespace.SourceOwnership.MainViewModelSource.Tests.cs`
  owns the MainViewModel source ownership orchestrator.
- `tests/Sussudio.Tests/ServiceNamespace.SourceOwnership.MainViewModelDeviceAudio.Tests.cs`
  owns MainViewModel device-native audio state, mode/gain, and request-controller
  source ownership assertions.
- `tests/Sussudio.Tests/ServiceNamespace.SourceOwnership.MainViewModelRuntime.Tests.cs`
  owns MainViewModel UI dispatch, property-change, runtime lifecycle/event-ingress,
  recording runtime, and disposal source ownership assertions.
- `tests/Sussudio.Tests/ServiceNamespace.SourceOwnership.MainViewModelDeviceAndCapture.Tests.cs`
  owns MainViewModel device refresh, capture device selection, format probe,
  source telemetry, recording capability, and preview renderer enqueue source
  ownership assertions.
- `tests/Sussudio.Tests/ServiceNamespace.AutomationContracts.Tests.cs` owns
  AutomationCommandKind project/source ownership alignment across the app and
  automation tools.
- `tests/Sussudio.Tests/ServiceNamespace.ServiceContracts.Tests.cs` owns the
  app-service contract boundary assertions that keep `Sussudio/Services/Contracts`
  separate from `Sussudio.Automation.Contracts` wire/protocol ownership.
- Focused `tests/Sussudio.Tests/HarnessCheckCatalog.PresentationPreview.*.cs`
  partials own presentation-preview capture/root
  policy, MainViewModel, MainWindow, stats, D3D renderer, and preview pacing
  registration groups. `tests/Sussudio.Tests/XUnit.PresentationPreviewHarnessRegistrationTests.cs`
  owns the xUnit execution surface that audits those registration groups against
  the focused UI ownership test inventory.
- `tests/Sussudio.Tests/XUnit.PresentationPreviewMainViewModelInitialContractsTests.cs`
  owns the former presentation-preview MainViewModel initial registration group
  for recording transition start/stop failure propagation after its removal from
  the legacy offline harness catalog.
- `tests/Sussudio.Tests/XUnit.PresentationPreviewMainWindowInitialContractsTests.cs`
  owns the former presentation-preview MainWindow initial registration group for
  close cancellation, window screenshot helper ownership, and property changed
  routing delegation after its removal from the legacy offline harness catalog.
- `tests/Sussudio.Tests/XUnit.PresentationPreviewWindowLifecycleContractsTests.cs`
  owns the former presentation-preview MainWindow window lifecycle group for
  native bootstrap, close lifecycle split, close request/app closing, recording
  finalization, and shutdown cleanup checks after their removal from the legacy
  offline harness catalog.
- `tests/Sussudio.Tests/XUnit.PresentationPreviewLaunchStartupContractsTests.cs`
  owns the former presentation-preview MainWindow launch/startup group for
  splash loading phrase ownership, splash pacing policy, launch entrance
  animation, and startup hosting checks after their removal from the legacy
  offline harness catalog.
- `tests/Sussudio.Tests/XUnit.PresentationPreviewScreenshotContractsTests.cs`
  owns the former presentation-preview MainWindow preview screenshot workflow
  and plan-policy checks after their removal from the legacy offline harness
  catalog.
- `tests/Sussudio.Tests/XUnit.PresentationPreviewShellChromeContractsTests.cs`
  owns the former presentation-preview MainWindow shell chrome, window title,
  live signal, and status-strip checks after their removal from the legacy
  offline harness catalog.
- `tests/Sussudio.Tests/XUnit.PresentationPreviewVisualShellContractsTests.cs`
  owns the former presentation-preview MainWindow visual shell group for
  control-bar hover animation, shell elevation, preview transition, startup
  overlay, and fade-in reveal checks after their removal from the legacy
  offline harness catalog.
- `tests/Sussudio.Tests/XUnit.PresentationPreviewRuntimeShellContractsTests.cs`
  owns the former presentation-preview MainWindow preview runtime shell/host
  group for resize telemetry, renderer host state, snapshot mapping, D3D
  projection ownership, surface/shadow ownership, and startup-plan fallback
  checks after their removal from the legacy offline harness catalog.
- `tests/Sussudio.Tests/XUnit.PresentationPreviewRuntimePolicyContractsTests.cs`
  owns the former presentation-preview MainWindow preview runtime policy group
  for snapshot health/projection policies and D3D projection policy defaults
  after their removal from the legacy offline harness catalog.
- `tests/Sussudio.Tests/XUnit.PresentationPreviewRecordingContractsTests.cs`
  owns the former presentation-preview MainWindow recording button chrome,
  state presentation, lockout policy, and button-action checks after their
  removal from the legacy offline harness catalog.
- `tests/Sussudio.Tests/XUnit.PresentationPreviewAudioControlContractsTests.cs`
  owns the former presentation-preview MainWindow preview audio fade, audio
  presentation, preview button presentation, and microphone control checks
  after their removal from the legacy offline harness catalog.
- `tests/Sussudio.Tests/XUnit.PresentationPreviewMainViewModelAudioControlsContractsTests.cs`
  owns the former presentation-preview MainViewModel audio-control group for
  analog gain mapping, preview audio monitoring volume persistence, microphone
  and device guards, device-audio request lifetime, audio-device selection
  policy, native XU audio-control service cohesion, and audio meter callback
  ownership checks after their removal from the legacy offline harness catalog.
- `tests/Sussudio.Tests/XUnit.PresentationPreviewResponsiveLayoutContractsTests.cs`
  owns the former presentation-preview MainWindow responsive shell layout and
  breakpoint policy checks after their removal from the legacy offline harness
  catalog.
- `tests/Sussudio.Tests/XUnit.PresentationPreviewCaptureSelectionContractsTests.cs`
  owns the former presentation-preview MainWindow capture selection binding,
  routing, collection sync, focused owner, device-audio projection, and
  normalizer checks after their removal from the legacy offline harness catalog.
- `tests/Sussudio.Tests/XUnit.PresentationPreviewCaptureOptionContractsTests.cs`
  owns the former presentation-preview MainWindow capture device action, option
  presentation, affordance policy, option binding, and tooltip formatter checks
  after their removal from the legacy offline harness catalog.
- `tests/Sussudio.Tests/XUnit.PresentationPreviewOutputPathContractsTests.cs`
  owns the former presentation-preview MainWindow output path display,
  truncation formatter, and button-action checks after their removal from the
  legacy offline harness catalog.
- `tests/Sussudio.Tests/XUnit.PresentationPreviewMainViewModelOutputPathContractsTests.cs`
  owns the former presentation-preview MainViewModel output path and disk-space
  presentation group for retired output picker partial ownership, invalid-path
  fallback behavior, and focused free-space presentation helper ownership after
  their removal from the legacy offline harness catalog.
- `tests/Sussudio.Tests/XUnit.PresentationPreviewMainViewModelSourceTelemetryContractsTests.cs`
  owns the former presentation-preview MainViewModel source-telemetry
  presentation group for source/target summary formatting, focused source
  telemetry helper ownership, and live-signal pixel-format fallback order after
  their removal from the legacy offline harness catalog.
- `tests/Sussudio.Tests/XUnit.PresentationPreviewMainViewModelDependencyCompositionContractsTests.cs`
  owns the former presentation-preview MainViewModel dependency-composition
  group for root dependency seam, UI dispatch, presentation, recording,
  capture/device, and runtime controller context ownership checks after their
  removal from the legacy offline harness catalog.
- `tests/Sussudio.Tests/XUnit.PresentationPreviewMainViewModelRuntimeContractsTests.cs`
  owns the final former presentation-preview MainViewModel runtime group for
  automation preview/HDR/volume routing, audio monitoring, capture settings
  projection, preview lifecycle ownership, and audio ramp trace telemetry after
  their removal from the legacy offline harness catalog.
- `tests/Sussudio.Tests/XUnit.PresentationPreviewCaptureRuntimeGuardContractsTests.cs`
  owns the former presentation-preview capture runtime guardrail group for
  recording stop failure propagation, preview stop overload/API compatibility,
  and emergency recording stop threading after their removal from the legacy
  offline harness catalog.
- `tests/Sussudio.Tests/XUnit.PresentationPreviewCaptureFlashbackBufferContractsTests.cs`
  owns the former presentation-preview capture Flashback buffer startup/recovery
  group for stale session cleanup and recovery-preserve behavior after their
  removal from the legacy offline harness catalog.
- `tests/Sussudio.Tests/XUnit.PresentationPreviewD3DPacingContractsTests.cs`
  owns the former presentation-preview D3D pacing group for transition-drain,
  frame-capture cancellation, and shared-device reference lifecycle checks after
  their removal from the legacy offline harness catalog.
- `tests/Sussudio.Tests/XUnit.PresentationPreviewD3DGeometryContractsTests.cs`
  owns the former presentation-preview D3D geometry/screenshot group for
  letterbox, black-edge, PNG CRC, and 16-bit PNG capture checks after their
  removal from the legacy offline harness catalog.
- `tests/Sussudio.Tests/XUnit.PresentationPreviewD3DCadenceContractsTests.cs`
  owns the former presentation-preview D3D present-cadence group for cadence
  DTO shape and suppression-baseline behavior checks after their removal from
  the legacy offline harness catalog.
- `tests/Sussudio.Tests/XUnit.PresentationPreviewD3DDeviceLostContractsTests.cs`
  owns the former presentation-preview D3D device-lost group for device-lost
  classification and recovery ownership checks after their removal from the
  legacy offline harness catalog.
- `tests/Sussudio.Tests/XUnit.PresentationPreviewD3DDiagnosticsContractsTests.cs`
  owns the former presentation-preview D3D diagnostics group for swap-chain and
  render timing, snapshot-model, and performance-timeline contract checks after
  their removal from the legacy offline harness catalog.
- `tests/Sussudio.Tests/XUnit.PresentationPreviewD3DContractsAndMetricsOwnershipTests.cs`
  owns the former presentation-preview D3D contracts/metrics source-ownership
  group for configuration, native interop, frame types, frame ownership, DXGI
  frame statistics, slow-frame diagnostics, and metric tracking checks after
  their removal from the legacy offline harness catalog.
- `tests/Sussudio.Tests/XUnit.PresentationPreviewD3DRuntimeCaptureOwnershipTests.cs`
  owns the former presentation-preview D3D runtime-capture source-ownership
  group for public frame submission and lifecycle checks after their removal
  from the legacy offline harness catalog.
- `tests/Sussudio.Tests/XUnit.PresentationPreviewD3DRenderSetupOwnershipTests.cs`
  owns the former presentation-preview D3D render setup/resource source-ownership
  group for panel binding, shared-device handoff, frame upload, input resources,
  and device initialization checks after their removal from the legacy offline
  harness catalog.
- `tests/Sussudio.Tests/XUnit.PresentationPreviewD3DRenderPipelineOwnershipTests.cs`
  owns the former presentation-preview D3D render-pipeline source-ownership
  group for render passes, shader rendering cache, shader compilation/source
  contracts, frame-latency wait, render thread, present accounting, viewport helpers, and screenshot
  encoding checks after their removal from the legacy offline D3D harness
  catalog. The empty `HarnessCheckCatalog.PresentationPreview.D3D.cs` hook was
  removed after the final D3D group moved to xUnit.
- `tests/Sussudio.Tests/PreviewPacingOwnership.Tests.cs` owns preview pacing
  classifier source ownership and automation-snapshot wiring assertions;
  `tests/Sussudio.Tests/PreviewPacingClassifier.Tests.cs` owns behavioral
  classifier cases.
- `tests/Sussudio.Tests/XUnit.RecordingModelContractsTests.cs` owns the former
  legacy recording-model execution surface for LibAv sink loop/source-ownership
  checks, capture runtime failure/runtime-flag checks, and the large Flashback
  buffer manager behavior/source-ownership group.
- `tests/Sussudio.Tests/XUnit.FlashbackExporterContractsTests.cs` owns the
  xUnit execution surface for the former legacy Flashback exporter registration
  group. `tests/Sussudio.Tests/XUnit.FlashbackDecoderContractsTests.cs`
  owns the xUnit execution surface for the former legacy Flashback decoder
  registration group. `tests/Sussudio.Tests/XUnit.FlashbackPlaybackContractsTests.cs`
  owns the xUnit execution surface for the former legacy Flashback playback
  registration groups. `tests/Sussudio.Tests/XUnit.FlashbackEncoderSinkContractsTests.cs`
  owns the xUnit execution surface for the former legacy Flashback encoder sink
  registration groups.
- `tests/Sussudio.Tests/XUnit.ToolModelContractsTests.cs` owns the xUnit
  execution surface for the former legacy NVML snapshot and
  CaptureSessionSnapshot default-state tool-contract checks.
  `tests/Sussudio.Tests/XUnit.NativeToolProbeContractsTests.cs` owns the xUnit
  execution surface for the former legacy RTK I2C probe unsafe-native-path
  guard check.
- `tests/Sussudio.FfmpegEncodeLab/Program.cs` owns standalone HDR encode-lab
  orchestration; `Program.Encoding.cs` owns FFmpeg argument and AV1 encoder
  selection policy; `Program.Support.cs` owns CLI parsing, tool-path resolution,
  and child-process log capture.
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
  reflection helpers for remaining legacy capture configuration model
  contract tests.
- `tests/Sussudio.Tests/XUnit.CaptureConfigurationModelsTests.cs` owns shared
  reflection helpers for capture configuration xUnit contract checks.
- `tests/Sussudio.Tests/XUnit.CaptureModeOptionsTests.cs` owns capture mode
  option display metadata and option-builder behavior checks.
- `tests/Sussudio.Tests/XUnit.CaptureSettingsContractsTests.cs` owns capture
  settings defaults, output path/file naming, bitrate policy, and MJPEG HFR
  policy checks.
- `tests/Sussudio.Tests/XUnit.RecordingConfigurationPolicyTests.cs` owns
  recording selection policy, encoder support, and recording pipeline option
  contract checks.
- `tests/Sussudio.Tests/XUnit.FlashbackModelsTests.cs` owns xUnit coverage for
  Flashback buffer option sizing, session, playback-state, export progress,
  segment, and request DTO contract tests.
  `XUnit.FlashbackModels.PropertyAssertions.cs` owns the shared
  reflection/nullability assertion helpers for that contract suite.
- Focused capture session coordinator coverage lives in
  `tests/Sussudio.Tests/CaptureSessionCoordinator.Api.Tests.cs`,
  `CaptureSessionCoordinator.Contracts`,
  `CaptureSessionCoordinator.Queue`, `CaptureSessionCoordinator.Flashback`,
  `CaptureSessionCoordinator.Ownership`, and `CaptureSessionTransitionPolicy`
  files; command/source ownership checks include the consolidated coordinator
  root and focused Flashback coordinator partials. Shared reflective harness helpers live in
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
- `tests/Sussudio.Tests/McpToolSurface.DiagnosticSession.Tool.Artifacts.Tests.cs`
  owns MCP `run_diagnostic_session` success artifact contract tests.
- `tests/Sussudio.Tests/McpToolSurface.DiagnosticSession.Tool.Failures.Tests.cs`
  owns MCP `run_diagnostic_session` failure artifact contract tests.
  `tests/Sussudio.Tests/XUnit.McpToolSurfaceContractsTests.cs` owns the xUnit
  execution surface for the general MCP tool-surface, command-routing,
  host/pipe, verification, Flashback tool, and diagnostic-session tool entry
  contracts after their removal from the legacy harness catalog.
- Diagnostic-session helper ownership checks live in focused lifecycle files:
  planning/setup checks in
  `tests/Sussudio.Tests/McpToolSurface.DiagnosticSession.Ownership.Planning.Tests.cs`,
  execution/startup/sampling checks in
  `tests/Sussudio.Tests/McpToolSurface.DiagnosticSession.Ownership.Execution.Tests.cs`,
  and teardown/reporting/metrics checks in
  `tests/Sussudio.Tests/McpToolSurface.DiagnosticSession.Ownership.TeardownAndReporting.Tests.cs`.
- Diagnostic-session infrastructure ownership checks live in focused files:
  runner/initial-snapshot checks in `InfrastructureOwnership.Runner`, pipe
  retry and command-channel checks in `InfrastructureOwnership.CommandChannel`,
  run-state/live-state/context/bootstrap/output-lock checks in
  `InfrastructureOwnership.RunContext`, and scenario/completion phase checks in
  `InfrastructureOwnership.Execution`.
- `tests/Sussudio.Tests/McpToolSurface.DiagnosticSession.ResultOwnership.Tests.cs`
  owns diagnostic-session model ownership assertions, with builder summary-write
  failure, formatter, artifact, JSON/shared-text, and infrastructure assertions
  split into the adjacent `ResultOwnership.*.Tests.cs` files. Builder result
  assertions are split by ownership band: `ResultOwnership.Builder.Tests.cs`
  covers core, preview scheduler, and overview/capture checks;
  `ResultOwnership.Builder.Flashback.Tests.cs` covers Flashback playback,
  recording, and export result projections; and
  `ResultOwnership.Builder.PreviewAndCompletion.Tests.cs` covers preview,
  analysis-warning, diagnostic-health, and artifact-handoff ownership.
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
- `tests/Sussudio.Tests/XUnit.McpDiagnosticSessionInfrastructureContractsTests.cs`
  owns xUnit execution for the former legacy diagnostic-session infrastructure
  runner/model ownership catalog band.
- `tests/Sussudio.Tests/XUnit.McpDiagnosticSessionResultSurfaceContractsTests.cs`
  owns xUnit execution for the former legacy diagnostic-session result-surface
  ownership catalog band.
- `tests/Sussudio.Tests/XUnit.McpDiagnosticSessionCommandRunContextContractsTests.cs`
  owns xUnit execution for the former legacy diagnostic-session command-channel
  and run-context ownership catalog band.
- `tests/Sussudio.Tests/XUnit.McpDiagnosticSessionScenarioExecutionContractsTests.cs`
  owns xUnit execution for the former legacy diagnostic-session general scenario
  execution and post-run ownership catalog band.
- `tests/Sussudio.Tests/XUnit.McpDiagnosticSessionFlashbackContractsTests.cs`
  owns xUnit execution for the former legacy diagnostic-session Flashback
  scenario, export/helper, metric, wait, validation, stress, and audio-master
  fallback catalog band.
- `tests/Sussudio.Tests/XUnit.McpDiagnosticSessionCoreContractsTests.cs`
  owns xUnit execution for the former legacy diagnostic-session sampler,
  metric projection, and health tolerance catalog band.
- `tests/Sussudio.Tests/XUnit.McpDiagnosticSessionRunnerBehaviorContractsTests.cs`
  owns xUnit execution for the former legacy diagnostic-session runner behavior
  catalog band.
- `tests/Sussudio.Tests/HarnessCheckCatalog.cs` keeps the compatibility runner
  entry point; diagnostic-session checks now execute through xUnit wrappers.
- `tests/Sussudio.Tests/McpToolSurface.DiagnosticSession.Runner.Helpers.cs`
  owns shared reflective runner setup for diagnostic-session runner behavior
  tests: loading `ssctl`, creating `DiagnosticSessionOptions`, invoking
  `DiagnosticSessionRunner.RunAsync`, and parsing synthetic JSON responses.
- `tests/Sussudio.Tests/McpToolSurface.Performance.*.Tests.cs` owns MCP
  performance timeline contract, Flashback timeline formatting, and
  frame-pacing verdict tests, plus PresentMon MCP correlation and option
  precedence coverage. The performance timeline contract uses
  `McpToolSurface.Performance.TimelineContract.*.Tests.cs` partials for shared
  source loading, source-ownership assertions, rendering text contracts, and
  `PerformanceTimelineEntry` projection contracts.
  `tests/Sussudio.Tests/XUnit.McpPerformanceToolContractsTests.cs` owns the
  xUnit execution surface for these performance/probe contracts after their
  removal from the legacy harness catalog.
- `tests/Sussudio.Tests/McpToolSurface.WindowPreview.*.Tests.cs` owns MCP
  wait, window action, preview toggle, Flashback toggle, screenshot, and probe
  tests. `tests/Sussudio.Tests/XUnit.McpWindowPreviewToolContractsTests.cs`
  owns the xUnit execution surface for the wait/window/screenshot/preview-frame/
  preview-toggle/probe checks after their removal from the legacy harness
  catalog.
- `tests/Sussudio.Tests/McpToolSurface.Helpers.Process.cs`,
  `McpToolSurface.Helpers.Reflection.cs`,
  `McpToolSurface.Helpers.PipeCapture.cs`, and
  `McpToolSurface.Helpers.Assertions.cs` own shared MCP process/JSON-RPC,
  reflection/tool-result, pipe-capture, and JSON assertion helpers respectively.
- `tests/Sussudio.Tests/Flashback.Tests.cs` owns shared Flashback test helper
  source readers and helper methods only.
- `tests/Sussudio.Tests/Flashback.Buffer.Tests.cs` owns Flashback buffer manager
  initialization contract tests.
- `tests/Sussudio.Tests/Flashback.Buffer.Helpers.cs` owns shared Flashback
  buffer test factories, completed-segment insertion, and sized-file helpers.
- `tests/Sussudio.Tests/Flashback.Buffer.SourceOwnership.Tests.cs` owns
  Flashback buffer-manager partial ownership assertions.
- `tests/Sussudio.Tests/Flashback.Buffer.Segments.Validation.Tests.cs` owns
  Flashback buffer segment completion metadata and outside-path rejection
  tests.
- `tests/Sussudio.Tests/Flashback.Buffer.Segments.Accounting.Tests.cs` owns
  Flashback buffer segment diagnostics, PTS clamp, byte accounting, and
  same-path extension tests.
- `tests/Sussudio.Tests/Flashback.Buffer.Segments.DisposalRecovery.Tests.cs`
  owns Flashback buffer disposed-state no-op and recovery-preserve tests.
- `tests/Sussudio.Tests/Flashback.Buffer.SegmentLookups.Tests.cs` owns
  Flashback buffer segment position lookup, next-segment path lookup, path
  normalization, and segment-start PTS behavior tests.
- `tests/Sussudio.Tests/Flashback.Buffer.SegmentQueries.Tests.cs` owns
  Flashback buffer segment range query, active path, segment-count, and
  segment-list behavior tests.
- `tests/Sussudio.Tests/Flashback.Buffer.Retention.Eviction.Tests.cs` owns
  Flashback buffer eviction accounting and eviction-pause behavior tests.
- `tests/Sussudio.Tests/Flashback.Buffer.Retention.Purge.Tests.cs` owns
  Flashback buffer purge retention and active-byte accounting tests.
- `tests/Sussudio.Tests/Flashback.Buffer.EvictionPauseOwnership.Tests.cs` owns
  Flashback buffer eviction-pause ownership assertions.
- `tests/Sussudio.Tests/Flashback.Buffer.Retention.StartupCleanup.Tests.cs`
  owns Flashback buffer startup-generated segment cleanup, legacy root cleanup,
  unrelated temp-directory preservation, and startup-cache budget tests.
- `tests/Sussudio.Tests/Flashback.Buffer.Validation.Tests.cs` owns Flashback
  buffer session-id and segment-extension validation tests.
- `tests/Sussudio.Tests/Flashback.EncoderSink.Tests.cs` owns Flashback encoder
  sink frame-rate, option, startup rollback, runtime counter, and PTS guard
  tests.
- `tests/Sussudio.Tests/Flashback.EncoderSink.QueuesAndDrain.Tests.cs` owns
  Flashback encoder sink queue rejection, lifecycle cleanup, packet-validation,
  and drain-loop ordering tests.
- `tests/Sussudio.Tests/Flashback.EncoderSink.ForceRotate.Tests.cs` owns
  Flashback encoder sink force-rotate and segment-registration recovery tests.
- `tests/Sussudio.Tests/XUnit.FlashbackEncoderSinkContractsTests.cs` owns the
  xUnit execution surface for the former legacy Flashback encoder sink
  frame-rate, codec, counter, queue, force-rotate, packet-drain, startup, and
  source-ownership checks after their removal from the legacy harness catalog.
- `tests/Sussudio.Tests/Flashback.Exporter.Basic.Tests.cs` owns Flashback
  exporter request-surface smoke tests, path/request validation, and export
  throttle tests.
- `tests/Sussudio.Tests/Flashback.Exporter.FailureClassifier.Tests.cs` owns
  Flashback export failure classifier source ownership and status-message
  mapping tests.
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
- `tests/Sussudio.Tests/XUnit.FlashbackExporterContractsTests.cs` owns the
  xUnit execution surface for the former legacy Flashback exporter cleanup,
  request validation, failure classification, segment, cancellation, output
  path/finalization, and source-ownership checks after their removal from the
  legacy harness catalog.
- `tests/Sussudio.Tests/Flashback.Playback.State.Tests.cs` owns Flashback
  playback initial state, pre-initialize command no-ops, successful no-op
  failure clearing, and coalesced command state tests.
- `tests/Sussudio.Tests/Flashback.Playback.SourceShape.Tests.cs` owns
  Flashback playback command-position clamping, saturating timestamp arithmetic,
  segment-open recovery, near-live snap, snap-live identity cleanup,
  pause-from-live display, and paused nudge source-shape tests.
- `tests/Sussudio.Tests/Flashback.Playback.Markers.Tests.cs` owns Flashback
  playback in/out marker API, normalization, disposal, and marker clamp tests.
- `tests/Sussudio.Tests/Flashback.Playback.Thread.Tests.cs` owns Flashback
  playback thread recovery tests.
- `tests/Sussudio.Tests/Flashback.Playback.Transitions.AudioPreviewGuards.Tests.cs`
  owns Flashback playback live-preview transition, audio guard, and
  audio-master projection/source ownership tests.
- Flashback playback command queue capacity/drop-oldest,
  scrub-coalescing source ownership, and seek-slot barrier/failure behavior
  coverage live in focused
  `tests/Sussudio.Tests/Flashback.Playback.CommandQueue.Capacity.Tests.cs`,
  `tests/Sussudio.Tests/Flashback.Playback.CommandQueue.ScrubCoalescing.Tests.cs`,
  `tests/Sussudio.Tests/Flashback.Playback.CommandQueue.SeekSlots.Tests.cs`, and
  `tests/Sussudio.Tests/Flashback.Playback.CommandQueue.SeekSlots.FailureModes.Tests.cs`
  owner files.
- `tests/Sussudio.Tests/Flashback.Playback.Cadence.Tests.cs` owns Flashback
  playback frame-duration, decoded-PTS cadence projection/telemetry, and
  decode metrics reset/projection tests.
- `tests/Sussudio.Tests/Flashback.Playback.Submission.Tests.cs` owns Flashback
  playback decoded-frame submit-failure, preview frame submission, held-frame
  ownership, and live-recovery ownership tests.
- `tests/Sussudio.Tests/Flashback.Playback.Reopen.Tests.cs` owns Flashback
  playback fMP4 reopen, seek-display, and seek recovery tests.
- `tests/Sussudio.Tests/XUnit.FlashbackPlaybackContractsTests.cs` owns the
  xUnit execution surface for the former legacy Flashback playback startup,
  command-queue, source-shape, cadence, submission, reopen, transition-guard,
  and metric-reset checks after their removal from the legacy harness catalog.
- `tests/Sussudio.Tests/Flashback.Decoder.Tests.cs` owns Flashback decoder
  audio, timestamp, stream-bound, validation, lifetime, and callback tests.
- `tests/Sussudio.Tests/XUnit.FlashbackDecoderContractsTests.cs` owns the
  xUnit execution surface for the former legacy Flashback decoder frame-buffer,
  source-ownership, state/lifetime, timestamp, audio, frame-validation, and
  cancellation checks after their removal from the legacy harness catalog.
- `tests/Sussudio.Tests/Flashback.Support.Tests.cs` owns cross-cutting Flashback
  support/logging contract tests.
- `Sussudio/Controllers/Flashback/FlashbackTimelineController.cs` owns Flashback
  timeline visibility, lockout, toggle synchronization, and timeline track layout sizing.
  `Sussudio/Controllers/Flashback/FlashbackTimelineAnimationController.cs`
  owns show/hide storyboard state, immediate collapse, and fullscreen animation
  reset. `Sussudio/MainWindow.Flashback.Interactions.cs` owns the XAML-facing
  command, polling, playhead, scrub, settings, timeline, and presentation
  adapter surface.
  Command semantics live in
  `FlashbackCommandController`.
- `Sussudio/Controllers/Flashback/FlashbackScrubInteractionController.cs` owns active
  Flashback pointer-scrub state, scrub throttling, release/cancel/capture-lost
  cleanup, fullscreen scrub termination, lockout clearing, and scrub visual
  updates. `Sussudio/MainWindow.Flashback.Interactions.cs` is the XAML-facing
  adapter.
  `Sussudio/Controllers/Flashback/FlashbackScrubInteractionController.cs` also
  owns pure timeline fraction/duration math used by scrub and playhead
  presentation.
- `Sussudio/Controllers/Flashback/FlashbackPlayheadMotionController.cs` owns the
  Flashback playhead motion context, public entry points, shared state,
  playback-state sampling, scrub/window gating, live right-edge pinning,
  long-horizon extrapolation scheduling, CTI anchor timing, compositor visual
  setup, snap placement, magnetic pointer-scrub movement, linear keyframe
  animation, and label clamp/positioning.
  `Sussudio/MainWindow.Flashback.Interactions.cs` is the XAML-facing adapter.
- `Sussudio/Controllers/Flashback/FlashbackMarkerPresentationController.cs` owns
  Flashback marker placement, selection-region layout, and compact duration
  text formatting. `Sussudio/MainWindow.Flashback.Interactions.cs` wires marker
  presentation callbacks.
- `Sussudio/Controllers/Flashback/FlashbackPlaybackUiCoordinator.cs` owns Flashback
  playback UI sequencing: track-resize snap/position/marker/CTI refresh order,
  playback state polling start/stop, play/pause glyph policy, Go Live enabled
  state, buffer-duration text, buffer-fill/position/marker refresh order, and
  position-label updates with CTI re-anchor gating.
- `Sussudio/Controllers/Flashback/FlashbackCommandController.cs` owns Flashback command
  semantics for in/out points, clear, play/pause, Go Live, fullscreen keyboard
  shortcuts including left/right nudge rejection logging, export, save-last-5m,
  enable-toggle rollback, and apply/restart. `Sussudio/MainWindow.Flashback.Interactions.cs`
  preserves the XAML command event-handler surface.
- `Sussudio/Controllers/Flashback/FlashbackPropertyChangedController.cs` also owns
  Flashback export progress-bar value, visibility, and reset-on-complete
  semantics. `Sussudio/MainWindow.Flashback.Interactions.cs` wires the
  export progress presentation controller.
- `Sussudio/Controllers/Flashback/FlashbackSettingsBindingController.cs` owns Flashback
  settings-control initialization, GPU decode toggle binding/sync, buffer
  duration combo selection/sync, and buffer-duration change logging.
  `Sussudio/MainWindow.Flashback.Interactions.cs` is the XAML-facing settings
  adapter; enable toggle rollback and apply/restart command behavior live in
  `FlashbackCommandController`.
- `Sussudio/Controllers/Flashback/FlashbackPollingController.cs` owns Flashback status
  and playback-position polling timers. `Sussudio/MainWindow.Flashback.Interactions.cs`
  is the XAML-facing adapter; CTI anchor timing lives in
  `Sussudio/Controllers/Flashback/FlashbackPlayheadMotionController.cs`.
- `Sussudio/Controllers/Shell/SettingsShelfController.cs` owns settings shelf
  visibility, the animation gate, and show/hide storyboard construction.
  `Sussudio/MainWindow.ShellChrome.Composition.cs` is the XAML-facing adapter.
- `Sussudio/Controllers/Launch/Splash/SplashLoadingPhraseController.cs` owns
  splash phrase file lookup, Markdown-ish parsing, cached defaults, exception
  fallback, randomized interval/mode selection, DispatcherTimer lifecycle, and
  two-line text animation.
  `Sussudio/MainWindow.ShellChrome.Composition.cs` is the XAML-facing phrase
  start/stop adapter.
- `Sussudio/Controllers/Launch/LaunchStartupController.cs` owns loaded-time
  startup ordering: native shell reveal scheduling, initial ViewModel settings
  load, preview audio fade priming before device refresh, no-preview fallback
  presentation, automation host start, and splash/entrance trigger.
  `Sussudio/MainWindow.ShellChrome.Composition.cs` is the XAML-facing Loaded
  adapter and shell launch context wiring owner.
- `Sussudio/Controllers/Launch/Entrance/LaunchEntranceAnimationController.cs` owns launch
  entrance context, initial hidden/scaled shell state, splash fade, one-shot
  splash playback state, loading-phrase start/stop ordering, handoff into shell
  entrance, shell chrome/button/stats entrance choreography, deferred preview
  reveal logging, active-storyboard cleanup, and the delayed control-bar shadow
  fade routed through `PreviewSurfaceShadowController`.
  `Sussudio/MainWindow.ShellChrome.Composition.cs` is the XAML-facing launch
  entrance adapter.
- `Sussudio/Controllers/Shell/ControlBarAnimationController.cs` owns the control-bar
  button list used by launch entrance animation plus hover/press/release scale
  behavior. `Sussudio/MainWindow.ShellChrome.Composition.cs` is the XAML-facing
  adapter.
- `Sussudio/Controllers/Shell/ShellChromeController.cs` owns static shell
  ThemeShadow and translation setup for the control bar and record button plus
  shell property-change routing across stats overlay and settings shelf
  controllers.
  `Sussudio/MainWindow.ShellChrome.Composition.cs` is the XAML-facing adapter.
- `Sussudio/Controllers/Preview/PreviewTransitionAnimationController.cs` owns preview
  shell/content fade and scale transitions, video-shadow fade timing,
  unavailable-placeholder fades, and startup/unavailable presentation prep.
  `Sussudio/MainWindow.PreviewTransitions.Composition.cs` wires
  preview-transition animation callbacks; video-shadow fade callbacks route
  through `PreviewSurfaceShadowController`.
- `Sussudio/Controllers/Preview/PreviewButtonActionController.cs` owns preview
  button glyph/tooltip presentation for Start Preview and Stop Preview plus
  preview button command choreography: pending-reinit cancel, user stop intent,
  audio/visual fade-out ordering, preview start/stop calls, reinit animation
  reset, and unavailable-placeholder reveal.
  `Sussudio/MainWindow.PreviewRenderer.Composition.cs` wires preview button presentation callbacks and preview
  lifecycle property/event routing.
- `Sussudio/MainWindow.PreviewTransitions.Composition.cs`
  keeps the XAML event name stable as part of the preview transition/presentation
  adapter.
- `Sussudio/Controllers/Recording/RecordingStatePresentationController.cs` owns
  pure recording-state lockout decisions, recording property-change routing,
  ViewModel-derived lockout/HDR/title/audio-meter policy application, and
  delegates record-button chrome.
  `Sussudio/Controllers/Recording/Button/RecordingButtonChromeController.cs` owns demo-visible
  record-button chrome: recording glow, Rec pulse, starting spinner,
  normal/recording content, padding, enabled-state application, and the
  circle/pill width morph. `MainWindow.ButtonActions.cs` wires the
  chrome controller, recording action adapter, and recording-state presentation adapter.
- `Sussudio/Controllers/Recording/Button/RecordingButtonChromeController.cs` owns the recording
  button command workflow and preview-state logging after a start.
  `MainWindow.ButtonActions.cs` is the XAML-facing adapter for recording and
  capture-device button workflows.
- `Sussudio/Controllers/Shell/LiveSignalInfoController.cs` owns live-signal pill
  text application, visibility state, show/hide debounce timers, and the small
  scale/fade animation. `MainWindow.StatusStripPresentation.cs` is the XAML-facing
  adapter. `Sussudio/ViewModels/ViewModelPresentationBuilders.cs` owns the
  view-model live-signal label formatting and pixel-format/codec suffix policy.
- `Sussudio/Controllers/Preview/PreviewAudioFadeController.cs` owns preview-volume
  fade-in/fade-out state, saved target volume, storyboard lifetime, and volume
  save suppression. `Sussudio/MainWindow.PreviewTransitions.Composition.cs` is the
  XAML-facing adapter.
- `Sussudio/Controllers/Preview/PreviewReinitTransitionController.cs` owns preview
  reinit animation active state, first-visual transition clears, startup-reset
  preservation, completion presentation decisions, and the
  `D3D11_RENDERER_REINIT_FLAG` / `PREVIEW_REINIT_ANIMATE_*` logs.
  `Sussudio/MainWindow.PreviewTransitions.Composition.cs` is the XAML/MainWindow
  adapter that supplies renderer-stop-before-teardown and UI callback endpoints
  for reinit completion.
- `Sussudio/Controllers/Preview/Startup/PreviewStartupSessionController.cs` owns preview
  startup attempt/state bookkeeping, timestamps, cached failure/missing-signal
  details, state/log transitions, first-visual confirmation sequencing,
  signal-window predicates, snapshot missing-signal refresh gates, and reset
  orchestration.
  `Sussudio/MainWindow.PreviewStartup.Session.Composition.cs` wires UI/runtime
  callbacks into the session, watchdog, and signal controllers, stable state
  projections, startup state, renderer-attached, first-visual, begin-attempt,
  reset adapters, raw timeout diagnostic snapshots, live preview signal state,
  renderer visibility details, logging, and confirmation callbacks.
  `Sussudio/Controllers/Preview/Startup/PreviewStartupWatchdogController.cs` owns
  watchdog/telemetry timers, timeout configuration, timeout recovery, and
  failure-stop scheduling. The MainWindow/XAML-facing adapter stays in
  `Sussudio/MainWindow.PreviewStartup.Session.Composition.cs`.
  `Sussudio/Controllers/Preview/Startup/PreviewStartupSignalCoordinator.cs` owns readiness-
  signal coordination: readiness-signal state handoff, missing-signal updates,
  playback-progress diagnostics, startup signal log strings, GPU position
  counter state, and first-visual confirmation decisions.
  `Sussudio/MainWindow.PreviewStartup.Session.Composition.cs` wires the
  coordinator context, stable signal snapshot properties used by automation,
  GPU signal, missing-signal, playback-snapshot, and first-visual adapter callbacks.
  `Sussudio/Controllers/Preview/Startup/PreviewStartupReadinessSignalController.cs` owns
  readiness-signal required/received state, missing-signal calculation,
  signal-list formatting, timeout diagnostic payload formatting,
  playback-advance threshold checks, and readiness result snapshots.
  `PreviewStartupWatchdogController.cs`
  owns preview startup timeout reason, timeout status, and failure-stop status text.
  `Sussudio/Controllers/Preview/PreviewLifecycleEventController.cs` owns preview-
  specific ViewModel event lifecycle and the preview property-change router for
  preview start/stop/reinit state.
  `Sussudio/MainWindow.PreviewRenderer.Composition.cs` wires preview button
  presentation callbacks and
  preserves preview event-handler signatures and delegates into the controller.
  `Sussudio/Controllers/Preview/PreviewReinitTransitionController.cs` owns preview
  reinit animation active state, first-visual transition clears, startup-reset
  preservation, completion presentation decisions, and reinit transition logs.
  `Sussudio/MainWindow.PreviewTransitions.Composition.cs` keeps the renderer-stop-before-teardown
  handoff and XAML callback endpoints for completion presentation.
  Keep preview startup fields out of the composition root.
- `Sussudio/Controllers/Preview/PreviewFadeInController.cs` owns delayed preview
  reveal after first visual: rendered-frame threshold, fade-in timer, renderer
  replacement fallback, and preview-audio fade start ordering.
  `Sussudio/MainWindow.PreviewTransitions.Composition.cs` wires the XAML-facing adapter. Keep
  timeout/watchdog recovery in `PreviewStartupWatchdogController`.
- `Sussudio/Controllers/Preview/PreviewTransitionAnimationController.cs` owns preview-
  startup loading overlay presentation while the app waits for visual
  confirmation: ProgressRing activation, fade-in/fade-out routing, and the
  reinit-collapse opacity reset. `Sussudio/MainWindow.PreviewTransitions.Composition.cs`
  is the XAML-facing adapter.
- `Sussudio/Controllers/Preview/Renderer/PreviewRendererHostController.cs` owns top-level
  preview resize log throttling and reset state.
  `Sussudio/MainWindow.PreviewRenderer.Composition.cs` wires renderer-host
  context callbacks, the XAML-facing `SizeChanged` adapter, renderer-host reset
  handoff, renderer start/stop/shutdown, and reinit-unsafe-window adapters;
  reinit renderer-stop/timeout policy lives with `PreviewRendererHostController.cs`;
  preview surface presentation lives in `PreviewSurfacePresentationController`,
  and preview shadow visuals live in `PreviewSurfaceShadowController`.
- `Sussudio/MainWindow.ButtonActions.cs` is the XAML-facing recording
  adapter. Recording-specific property-name routing, record-button, glow, pulse,
  and recording-time lockout projection live in
  `RecordingStatePresentationController`.
- `Sussudio/MainWindow.ButtonActions.cs` is the XAML-facing recording,
  device, and output-path button/display adapter. `OutputPathController` owns
  output-path property-change routing, textbox updates, and browse/open
  commands.
- `Sussudio/MainWindow.CaptureBindings.cs` is the XAML-facing adapter
  for capture option setup, event binding, and capture-option/source-signal
  property-change routing; the property-name router lives in
  `CaptureOptionBindingController`.
- `Sussudio/MainWindow.ShellChrome.Composition.cs` is the XAML-facing
  shell property-change adapter.
  `Sussudio/Controllers/Shell/ShellChromeController.cs` owns the shell
  property-change route order across `StatsOverlayCompositionController` and
  `SettingsShelfController`; stats visibility behavior still lives in the stats
  composition controller, while settings visibility behavior still lives in the
  settings shelf controller.
- `Sussudio/MainWindow.StatusStripPresentation.cs` is the XAML-facing live signal
  adapter. `LiveSignalInfoController` owns live source-signal property-change
  routing and pill presentation.
- `Sussudio/Controllers/Flashback/FlashbackPropertyChangedController.cs` owns
  Flashback-specific property-change routing for timeline lockout, markers,
  playhead updates, export progress, and settings-control synchronization.
  `Sussudio/MainWindow.xaml.cs` is the XAML/MainWindow property-change
  adapter that composes the Flashback route table callbacks alongside the root
  ViewModel router.
- `Sussudio/Controllers/Audio/AudioControlPresentationController.cs` owns audio and
  microphone property-change routing/projections: audio toggles, monitoring
  meter state, preview volume slider sync, microphone enablement, and microphone
  volume sync. `Sussudio/MainWindow.AudioBindings.cs` is the
  XAML-facing audio/microphone presentation adapter.
- `Sussudio/Controllers/Audio/MicrophoneControlsController.cs` owns microphone volume
  slider synchronization, save triggers, shelf enablement, and mic-meter row
  animation state. `MainWindow.AudioBindings.cs` is the XAML-facing
  audio/microphone presentation adapter.
- `Sussudio/Controllers/Shell/ResponsiveShellLayoutController.cs` owns the
  control-bar label breakpoint, narrow/wide placement policy, responsive
  visibility for the complete control-bar label set, and capture-settings grid
  placement to XAML elements.
  `MainWindow.ShellChrome.Composition.cs` is the XAML-facing adapter.
- `Sussudio/Controllers/Capture/CaptureSelectionBindingController.cs` owns
  the capture-selection binding controller shell, context lifetime, XAML
  control dependency bag, capture/audio/microphone/encoder collection source
  wiring, collection-change debounce/queued sync, available-option
  property-change rebinding, capture-device ComboBox/ViewModel synchronization,
  pending-device apply state, selected-device property-change mismatch logging,
  audio-input and microphone ComboBox/ViewModel synchronization, resolution and
  frame-rate ComboBox/ViewModel synchronization, recording
  format/quality/preset/split-encode ComboBox synchronization, shared string
  ComboBox selection application, device-audio mode/gain control projection,
  and the capture-selection `PropertyChanged` router. The local
  `CaptureComboBoxSelectionNormalizer` in
  `Sussudio/Controllers/Capture/CaptureSelectionBindingController.cs` owns pure
  capture/audio/microphone/resolution/frame-rate/string ComboBox selection and
  fallback matching.
  `Sussudio/MainWindow.CaptureBindings.cs` owns controller
  instantiation, XAML dependency wiring, collection/property-change adapters,
  and the thin XAML-facing selection bridges for device, audio, device-audio,
  capture-mode, and recording option selection.
- `Sussudio/Controllers/Audio/AudioControlBindingController.cs` owns the audio-control
  binding context, initial audio/microphone projection, preview-volume binding and priming,
  audio/microphone/device-audio selection handlers,
  record/preview/custom-audio/microphone toggle handlers, audio-meter activation,
  initial meter presentation, and device-audio gain/meter resize hooks.
  Device-audio mode/gain control projection stays in
  `Sussudio/Controllers/Capture/CaptureSelectionBindingController.cs`.
  `Sussudio/MainWindow.AudioBindings.cs` is its XAML-facing adapter.
- `Sussudio/Controllers/Capture/CaptureOptionPresentationController.cs` owns the capture-
  device refresh/apply button workflows and preserves the explicit apply/reinit
  path. `MainWindow.ButtonActions.cs` is the XAML-facing adapter for
  recording, capture-device, and output-path button/display bridges.
- `Sussudio/Controllers/Capture/CaptureOptionPresentationController.cs` owns pure
  capture-option presentation decisions, XAML control application,
  decoder-count selection handling, and HDR hint/FPS telemetry tooltip text
  policy.
  `MainWindow.CaptureBindings.cs` is the XAML-facing adapter.
- `Sussudio/Controllers/Capture/CaptureOptionBindingController.cs` owns the
  capture option binding adapter context, setup, UI event attachment,
  initialization, resolution/frame-rate selection, recording option event
  bindings, show-all binding, HDR/true-HDR click binding,
  `CaptureComboBoxSelectionNormalizer` use for shared frame-rate auto/exact
  matching, capture-option/source-signal property-change routing,
  custom-bitrate control sync, HDR/true-HDR ViewModel-to-control sync, preview
  HDR passthrough forwarding, and delegated presentation callbacks for option
  affordances, telemetry tooltips, and source overlay refreshes.
  `MainWindow.CaptureBindings.cs` is the XAML-facing capture and
  recording option adapter, including the small property-change forwarding
  method that delegates to this controller.
- `Sussudio/Controllers/Recording/Output/OutputPathController.cs` owns recording output-
  path textbox, tooltip, resize-event updates, and browse/open-recordings button
  workflows plus pure output-path truncation text policy.
  `MainWindow.ButtonActions.cs` is the XAML-facing adapter used by binding
  setup, property changes, and button events.
- `Sussudio/ViewModels/MainViewModel.*.cs` for root presentation state and
  automation-facing compatibility. `MainViewModel.cs` owns the public
  compatibility-facade shell, shared shell/status/live-info state, native
  window handle state, UI collection replacement, non-preview coordination
  gates, and small bridge methods, while
  `MainViewModel.Composition.cs` owns construction, dependency assignment,
  collaborator fields, controller graph handoff, and startup lifecycle kick-off.
  `MainViewModel.cs` owns preview lifecycle compatibility entry
  points, preview-sink handoff, preview lifecycle flags,
  preview reinitialize coordination, and preview request events; `MainViewModel.CaptureState.cs` owns capture-selection
  state, option collections, HDR capture/runtime presentation state, and
  source signal/source-telemetry presentation state; `MainViewModel.AudioState.cs` owns audio and
  microphone state plus audio-preview property-change routing; `MainViewModel.DeviceAudioState.cs` owns device-native
  audio/XU UI state; `MainViewModel.FlashbackState.cs` owns Flashback
  timeline/export state plus buffer, bitrate, playback-state, in/out marker,
  and gap-from-live UI projection. `MainViewModel.AudioMeters.cs` owns live
  audio/microphone meter callback state; keep callback-thread meter targets
  out of the root facade file. `Sussudio/ViewModels/AudioRampTraceRecorder.cs`
  owns audio ramp diagnostic state, bounded ring-buffer storage, snapshot
  projection, trace session start/complete, trace-point capture, sampler loop,
  and delayed sampler shutdown.
  `MainViewModel.AudioState.cs` keeps the automation-facing audio-ramp trace
  adapter methods plus trace recorder and preview-volume transition controller
  wiring. `PreviewAudioVolumeTransitionController.cs`
  owns preview-volume save suppression/override state, priming, restoring,
  trace adapters, property-to-session volume forwarding, preview-audio ramp
  constants, easing, and async ramp-down/ramp-up execution.
  `MainViewModel.AudioState.cs` owns
  audio capture enablement and Flashback restart/teardown routing.
  `MainViewModel.AudioState.cs` owns audio-preview monitoring toggle routing,
  preview-volume save suppression/override properties, change notification,
  ramp adapter methods, persisted preview-volume save routing, preview
  monitoring coordinator sequencing, microphone observable state, endpoint
  volume synchronization, persistence, and microphone property-change routing.
  `MainViewModel.AudioInputSelection.cs`
  owns custom audio-input property handlers, retargeting, and
  preview-monitoring ramp handoff.
  `Sussudio/Controllers/ViewModel/MainViewModelDeviceAudioRequestController.cs`
  is a top-level `Sussudio.Controllers` owner for device-native audio request
  lifetime: selected-device refresh scheduling, mode-change scheduling, shared
  debounce CTS fields, UI enqueue lifetime, graph-port context contract,
  analog-gain property-change scheduling, UI/XU request debounce,
  flash-persist debounce, and cancellation cleanup. The compatibility
  property-change adapters stay with the observable device-audio state in
  `MainViewModel.DeviceAudioState.cs`.
  `MainViewModel.DeviceAudioState.cs` owns device-native audio-control support
  probing, readback, pending saved-state reconciliation, mode switching, and
  failure readback through the supported native-XU switch command surface,
  not the legacy AT input-source fallback path. It also owns shared
  audio-control guards, mode normalization, analog gain XU writes,
  settings persistence, and the pure percent-to-XU-byte analog gain curve helper
  used by device-native gain application.
  `MainViewModel.AudioState.cs` owns audio capture property
  handlers. `MainViewModel.AudioState.cs` owns audio-preview property
  handlers, microphone monitor property handlers, and selected-microphone
  property handlers.
  `MainViewModel.CaptureModeTransactions.cs`
  owns capture-mode property handlers for selected resolution, selected format,
  selected video format, and MJPEG decoder count changes.
  `Sussudio/Controllers/ViewModel/MainViewModelUiDispatchController.cs` owns
  shared view-model UI dispatcher enqueue/invoke policy, disposal skip logging,
  cancellation handoff, enqueue-failure logging, status projection, and the UI
  dispatch graph-port contract for dispatcher access, disposal state, logging,
  exception logging, and status text projection.
  `MainViewModel.Composition.cs` owns the stable private UI-dispatch adapter
  names plus preview event fan-out for the partial family, beside the
  controller graph construction that consumes those ports.
  `Sussudio/Controllers/ViewModel/MainViewModelRuntimeLifecycleController.cs`
  is a top-level `Sussudio.Controllers` owner for periodic timer refresh orchestration, initial
  source-telemetry/HDR/live-info/timer/disk-space bootstrap, and the
  runtime lifecycle graph-port contract for timer creation, runtime
  snapshot sampling, telemetry bootstrap, live-info/HDR projection, recording
  stats refresh, Flashback bitrate refresh, disk-space refresh, and watcher
  disposal, while
  `Sussudio/Controllers/ViewModel/MainViewModelRuntimeEventIngressController.cs`
  is a top-level `Sussudio.Controllers` owner for runtime event handling through graph-built context ports:
  system-resume preview rebind handling, audio-device-invalidated rebind
  scheduling through the preview lifecycle owner, capture status/error fan-out,
  capture pre-cleanup renderer stop fan-out, frame-captured callbacks, the
  runtime event ingress graph-port contract, and event
  subscription/unsubscription ordering including the desktop power-resume
  signal.
  `Sussudio/Controllers/ViewModel/MainViewModelDeviceFormatProbeController.cs`
  is a top-level `Sussudio.Controllers` owner for late device-format probe event
  ingress, UI enqueue/generation checks, selected-device capability refresh,
  and handoff to the retarget applier through graph-built context ports.
  `Sussudio/Controllers/ViewModel/MainViewModelDeviceFormatProbeRetargetApplier.cs`
  is a top-level `Sussudio.Controllers` owner for UI-side late-probe retarget
  application, HDR/SDR reinitialize dispatch, MJPG HFR preserve, session
  mismatch checks, and active-capture restore through graph-built context ports.
  `MainViewModel.RecordingState.cs` owns recording-runtime counters and the DiskSpaceInfo assignment bridge,
  while `Sussudio/ViewModels/ViewModelPresentationBuilders.cs` owns output drive probing,
  fallback, formatting, and suppressed-warning logging.
  `MainViewModel.RecordingState.cs` owns
  recording size/bitrate label assignment, recording-state reset reactions, and
  the bounded byte-sample smoothing helper shared by recording and Flashback
  bitrate presentation.
  `MainViewModel.CaptureState.cs` owns capture presentation adapters:
  live-capture info projection from `CaptureRuntimeSnapshot`, including
  audio-preview activity and live-resolution/frame-rate/pixel-format
  assignment, preview-stop live-info reset, HDR runtime state/readiness
  projection, target-summary property application, and auto-resolution display
  text used by status and telemetry presentation. It delegates live-signal
  label formatting to
  `Sussudio/ViewModels/ViewModelPresentationBuilders.cs`.
  `MainViewModel.CaptureState.cs` owns the impure capture-settings adapter that
  samples UI selection and observed runtime/source state.
  `Sussudio/ViewModels/CaptureSettingsProjectionBuilder.cs`
  owns final `CaptureSettings` assembly, audio/microphone device application,
  pure projection policy/input DTOs, selected frame-rate option seed,
  auto-resolved effective FPS, negotiated rational/source-telemetry overrides,
  rational/decimal fallbacks, requested pixel format, and MJPEG decode forcing.
  `MainViewModel.cs` keeps the compatibility facade entry points
  for device initialization, preview start/stop, selected-device apply, and
  preview reinitialization. `Sussudio/Controllers/ViewModel/MainViewModelPreviewLifecycleController.cs`
  is a top-level `Sussudio.Controllers` owner for the underlying preview
  lifecycle operations: device initialization, preview start/stop,
  selected-device apply, and the reinitialize facade.
  It also owns the preview lifecycle graph-port contract for preview
  state/events, capture/session operations, source telemetry refresh, UI
  dispatch, audio-preview activity, and preview-volume ramp-down.
  Sibling ViewModel controllers receive that preview lifecycle owner directly
  from `MainViewModelControllerGraph` instead of routing controller-to-controller
  calls back through the root facade.
  `Sussudio/Controllers/ViewModel/MainViewModelPreviewReinitializeController.cs`
  is a top-level `Sussudio.Controllers` owner for debounced reinitialization,
  restart-cancellation state, Flashback-cycle wait-before-reinit,
  renderer-stop handoff, teardown restart, and reinit gate release.
  It also owns the graph-built reinitialization port contract for selected
  device/format state, generation coalescing, pending Flashback-cycle waits,
  debounce/timeout policy, renderer notifications, restart cancellation, and
  reinit gate access.
  `MainViewModel.RecordingState.cs` owns the stable recording facade:
  toggle, desired-state, graceful-stop, the direct emergency-stop coordinator
  bridge, recording option selections, output path, counters, and observable
  transition flags.
  `Sussudio/Controllers/ViewModel/MainViewModelRecordingTransitionController.cs`
  is a top-level `Sussudio.Controllers` owner for recording toggle
  serialization, desired-state routing, graceful stop, transition gating, and
  in-flight transition wait/error propagation, graph-port context contract,
  concrete start/stop operation execution, failure/cancellation state repair,
  recording timer state, status/count presentation updates, and direct use of
  the preview lifecycle owner for recording startup initialization.
  `Sussudio/Controllers/ViewModel/MainViewModelDisposalController.cs` is a
  top-level `Sussudio.Controllers` owner for bounded teardown, dispose timeout policy, watcher disposal, coordinator
  cleanup/dispose, capture-service async-dispose fallback, disposal-step
  logging, and the disposal graph-port contract for one-shot disposal entry, teardown
  cancellations, runtime stop, coordinator cleanup/dispose, and capture-service
  async/sync disposal fallback, plus the bounded wait helper port that keeps
  timeout behavior explicit. `MainViewModel.cs` is the public refresh/dispose
  adapter and owns active Flashback export cancellation during teardown.
  `MainViewModel.AutomationSnapshots.cs` owns automation-facing capture runtime,
  health, and recording snapshot projection. `MainViewModel.AutomationSnapshots.cs`
  also owns automation-facing source/preview probes and preview frame capture.
  `MainViewModel.AutomationSnapshots.cs` owns automation-facing view-model runtime snapshot UI-thread capture.
  `ViewModelRuntimeSnapshotBuilder.cs` owns pure view-model runtime snapshot DTO construction.
  `MainViewModel.AutomationSnapshots.cs` owns automation-facing options
  UI-thread snapshot capture for CLI/MCP clients, while
  `AutomationOptionsSnapshotBuilder.cs` owns the pure selected-control-state DTO
  construction.
  `MainViewModel.FlashbackPlaybackCommands.cs` owns read-only Flashback
  playback snapshot and segment access, rejection status projection for UI,
  CLI, and MCP callers, scrub, nudge, in/out marker command routing, and
  automation-facing Flashback playback action dispatch.
  `MainViewModel.FlashbackState.cs` owns buffer, bitrate,
  playback-state, in/out marker, and gap-from-live UI projection.
  `MainViewModel.FlashbackExport.cs` owns Flashback UI export commands,
  save-picker flow, active-export guard, user-facing export result/status
  handling, shared export operation lifecycle, progress handoff, stale-result
  classification, current-operation checks, CTS cancellation/disposal cleanup,
  and automation-facing export execution with linked cancellation and dispatcher
  cleanup.
  `MainViewModel.CaptureSelection.cs` owns capture-device selection reactions,
  effective resolution helpers, frame-rate selection reactions, and
  auto-selection entry points. `MainViewModel.CaptureModeTransactions.cs` keeps
  the resolution, frame-rate, selected-format, and video-format rebuild
  compatibility adapters alongside capture-mode transaction state, while
  `Sussudio/Controllers/ViewModel/MainViewModelCaptureModeOptionRebuildController.cs`
  owns the cohesive capture-mode option rebuild transaction, including
  frame-rate option rebuilding, source-rate filtering handoff, auto/source
  option selection, observable frame-rate collection mutation, and selected
  frame-rate application through graph-built context ports.
  `Sussudio/ViewModels/FrameRateAutoSelectionPolicy.cs`
  owns pure frame-rate option choice: pending SDR bucket preference,
  Source-rate nearest match with timing-family tie-break, generic auto fallback,
  and previous/manual selection fallback.
  `MainViewModel.CaptureState.cs` owns shared frame-rate selection reset,
  resolved automatic frame-rate application, disabled frame-rate reason
  projection, and capture-mode reset flags.
  `Sussudio/ViewModels/FrameRateSourceFilterPolicy.cs` owns source-rate filtering
  with capture options always visible. `MainViewModel.CaptureModeTransactions.cs`
  owns deferred rebuild behavior, capture-mode reinitialization serialization,
  and duplicate-reinit suppression.
  `Sussudio/ViewModels/FrameRateTimingPolicy.cs` owns pure frame-rate timing
  family and variant models, rational parsing, friendly/exact frame-rate
  matching, timing-family ranking, and preferred-format ranking helpers used by
  frame-rate, resolution, capture-settings, and automation projections.
  `Sussudio/Controllers/ViewModel/MainViewModelFrameRateTimingResolver.cs`
  owns the stateful resolver that resolves timing variants and source/preferred
  timing from resolution capabilities, runtime snapshots, selected formats,
  source telemetry, UI selection state, and its graph-built context ports.
  `MainViewModel.CaptureModeTransactions.cs` keeps selected-format and video-format
  rebuild compatibility adapters, while
  `Sussudio/Controllers/ViewModel/MainViewModelCaptureModeOptionRebuildController.cs`
  is a top-level `Sussudio.Controllers` owner for selected-format assignment,
  video-format option collection mutation, capture-format request shaping,
  and the capture-mode option rebuild graph-port contract for option
  collections, stable Source/Auto sentinel values, source telemetry,
  resolution/frame-rate selection state, automatic retarget flags,
  format-change suppression, and projected status text.
  `Sussudio/ViewModels/CaptureFormatSelectionPolicy.cs`
  owns pure selected capture-format choice and mode-tuple video-format filtering.
  `Sussudio/Controllers/ViewModel/MainViewModelRecordingCapabilityController.cs`
  is a top-level `Sussudio.Controllers` owner for startup FFmpeg capability
  probes for recording formats and split-encode modes through graph-built
  context ports, UI enqueue failure logging, and recording-format policy
  application to observable state.
  `MainViewModel.CaptureModeTransactions.cs`
  owns HDR toggle side effects: recording-time revert/status, mode option
  rebuilds, immediate reinitialize scheduling, and settings persistence.
  `Sussudio/ViewModels/RecordingSettingsSelectionPolicy.cs` owns pure recording
  codec filtering, selected-codec fallback policy, string-to-model format/quality
  parsing, and custom bitrate clamp policy shared by UI and automation.
  the root `MainViewModel.cs` keeps the public capture-device refresh facade,
  while `Sussudio/Controllers/ViewModel/MainViewModelDeviceRefreshController.cs`
  is a top-level `Sussudio.Controllers` owner for startup refresh
  orchestration: requesting the combined discovery result, applying
  audio-device startup selection, replacing the capture-device collection,
  starting background format probes, restoring saved capture-device selection,
  and directly auto-starting preview through the preview lifecycle owner.
  It also owns the device-refresh graph-port contract for discovery, startup
  audio selection, device collection mutation, background format probes,
  selection restore, and scan status projection. The shallow `MainViewModel.DeviceManagement.cs`
  partial was retired instead of keeping another sub-100-line facade. Selected
  capture-device reactions, capability projection, source telemetry reset, and
  device-native audio-control refresh handoff live in `MainViewModel.CaptureSelection.cs`; capture-mode property-change hooks live
  in `MainViewModel.CaptureModeTransactions.cs`; startup audio-list and
  watcher-driven audio endpoint refresh adaptation live in `MainViewModel.AudioState.cs`.
  `Sussudio/ViewModels/AudioDeviceSelectionPolicy.cs` owns pure capture-card
  endpoint filtering plus previous/saved/default audio and microphone selection
  fallback policy.
  `Sussudio/Controllers/ViewModel/MainViewModelDeviceFormatProbeController.cs`
  is a top-level `Sussudio.Controllers` owner for late device-format probe
  reconciliation, format collection mutation, capability refresh after
  background probes, enqueue/failure logging, and handoff to the retarget
  applier.
  It also owns the late-probe reconciliation graph-port contract for UI
  enqueue, device-scan generation, selected-device lookup/state, active capture
  guards, suppress-format-change state, capability rebuild, and retarget
  applier construction.
  `Sussudio/Controllers/ViewModel/MainViewModelDeviceFormatProbeRetargetApplier.cs`
  is a top-level `Sussudio.Controllers` owner for UI-side late-probe retarget
  application, HDR/SDR reinitialize dispatch, MJPG HFR preserve, session
  mismatch check, and active-capture restore behavior.
  It also owns the late-probe retarget graph-port contract for capture-mode
  state, resolution/frame-rate mutation, reinitialize dispatch, runtime
  snapshot checks, frame-rate rebuild, and target-summary refresh.
  `Sussudio/ViewModels/DeviceFormatProbeRetargetPolicy.cs`
  owns the pure late-probe decision policy for HDR retarget, SDR NV12 retarget,
  MJPG HFR preservation, session mismatch, and active-capture restore.
  `MainViewModel.CaptureModeTransactions.cs` keeps the compatibility adapter for
  resolution option rebuild callers.
  `Sussudio/Controllers/ViewModel/MainViewModelCaptureModeOptionRebuildController.cs`
  owns resolution option rebuilds inside the top-level capture option
  rebuild controller: automatic resolution dropdown option construction,
  automatic resolution-selection adaptation over current ViewModel state,
  auto-resolution state refresh, and resolution dropdown mutation through
  graph-built context ports.
  `MainViewModel.CaptureSelection.cs` owns effective Source resolution state
  and state-backed delegates to the pure selection policy.
  `Sussudio/ViewModels/AutoCaptureSelectionPolicy.cs` owns automatic resolution
  ranking and source-aware frame-rate selection.
  `Sussudio/ViewModels/CaptureResolutionSelectionPolicy.cs` owns the pure
  resolution selection policy: source-telemetry-aware resolution matching, HDR
  frame-rate-preserving retarget and support-hint selection, SDR auto/fallback
  resolution selection, parsing, frame-rate support checks, nearest-resolution
  ranking, and the policy request/result records.
  State-backed capability queries for callers that live across the ViewModel
  partial family stay in `MainViewModel.CaptureSelection.cs`; observable
  resolution dropdown mutation routes through the top-level
  `MainViewModelCaptureModeOptionRebuildController.cs`.
  `Sussudio/Controllers/ViewModel/MainViewModelSourceTelemetryController.cs`
  is a top-level `Sussudio.Controllers` owner for source telemetry ingress behavior, projection, enum-string caching,
  summary-age refresh, source-aware auto-retargeting hints, and the source telemetry graph-port contract consumed by source telemetry
  ingress and projection, including the pure summary builder and auto-resolution
  predicate ports that keep facade-private helpers explicit.
  `Sussudio/ViewModels/ViewModelPresentationBuilders.cs`
  owns source telemetry summary, telemetry age, and target-summary display text formatting.
  `MainViewModel.SettingsPersistence.cs` owns settings initialization, simple
  persistence reactions, the impure settings load/save adapter, validated
  load-plan application order, feature-specific state assignment, and deferred
  device/audio/microphone selection staging.
  `MainViewModelSettingsPersistenceProjection.cs` owns persisted-settings
  validation, clamping, deferred-selection handoff, save DTO projection, and
  load/save projection contracts.
  `MainViewModel.FlashbackState.cs` owns active Flashback reactions to
  recording-format, encoder quality/preset/split, bitrate, buffer-duration,
  and GPU-decode setting changes.
  `MainViewModel.cs` owns UI-only automation mutators
  for settings visibility, Flashback timeline visibility, show-all capture
  options, stats dock/section visibility, and frame-time overlay display.
  `MainViewModel.AutomationCommands.cs` owns automation command entry points for
  app audio enablement, audio-preview enablement, preview-volume
  clamp/persist, device-native mode/gain application, and microphone
  enablement with recording-time refusal and idempotent handling.
  `MainViewModelPreviewLifecycleController.cs` owns top-level automation preview
  enable/disable idempotence, pending-reinit cancellation, and start/stop
  routing behind the `MainViewModel.cs` compatibility facade.
  `MainViewModel.CaptureModeTransactions.cs` owns automation HDR and true-HDR
  preview recording-time guard enforcement and availability checks alongside
  HDR mode change side effects.
  `MainViewModel.FlashbackState.cs` owns automation Flashback
  enable/restart routing through the capture session coordinator alongside
  buffer/GPU setting reactions.
  `MainViewModel.AutomationCommands.cs` owns automation device refresh,
  capture-device selection, audio-input selection, and custom audio-input
  enablement.
  `MainViewModel.AutomationCommands.cs` keeps the stable public automation
  facade for capture resolution, frame-rate, video-format, MJPEG decoder
  worker-count, recording format, encoder, and output-path settings.
  `Sussudio/Controllers/ViewModel/MainViewModelCaptureSettingsAutomationController.cs`
  is a top-level `Sussudio.Controllers` owner for UI-thread setting mutations,
  validation, MJPEG decoder clamping, and active capture-mode reinitialization
  routing.
  It also owns the capture-settings automation graph-port contract for option
  collections, selected capture-mode state, preview reinitialization checks,
  UI-thread dispatch, and format-change suppression.
  `MainViewModel.CaptureModeTransactions.cs` owns capture-mode/HDR
  property-change side effects outside the capture-settings automation
  controller.
  `Sussudio/Controllers/ViewModel/MainViewModelRecordingSettingsAutomationController.cs`
  is a top-level `Sussudio.Controllers` owner for UI-thread setting mutations,
  HDR compatibility enforcement, Flashback cycle suppression, coordinator side
  effects, bitrate clamp policy, encoder preset, and output-path directory
  creation.
  It also owns the recording-settings automation graph-port contract for UI
  dispatch, option collections, suppression flags, selected encoder/output
  state, recording-format coordinator updates, and Flashback encoder setting
  cycles.
  `Sussudio/Controllers/ViewModel/MainViewModelRecordingCapabilityController.cs`
  is a top-level `Sussudio.Controllers` owner for startup FFmpeg capability
  probes for recording formats and split-encode modes plus observable
  recording-format option rebuilds.
  `Sussudio/ViewModels/MainViewModel.RecordingState.cs` keeps recording-runtime
  counters, disk-space assignment, and the stable recording-capability facade
  methods used by settings initialization and HDR mode-change rebuild callers.
  It also owns the recording-capability graph-port contract for default encoder
  names, observable recording/split-encode option collections, selected
  recording format state, HDR/status state, FFmpeg-missing state, and UI
  dispatch.

Refactor direction:

- Keep `MainWindow.xaml.cs` as a shell/composition root over time.
- `MainWindow.xaml.cs` owns construction, startup event wiring, and phased controller
  initialization. Keep the phase methods grouped by runtime surface
  (window/shell, Flashback, presentation, preview, recording, launch/status,
  preview actions, audio, capture, output) so adding a controller does not turn
  the composition root back into an undifferentiated list.
- Keep `MainWindow.*` partials thin as XAML adapters over named controllers.
  Preview startup, preview runtime snapshot dispatch/sampling, MainWindow UI
  dispatching, stats projection, and Flashback playback/export presentation
  already have named owners. The Flashback adapter family is split across
  focused `MainWindow.Flashback.*.cs` partials, and the preview-startup adapter
  family is split across focused `MainWindow.PreviewStartup.*.cs` partials.
  The preview-transition adapter family is consolidated in
  `MainWindow.PreviewTransitions.Composition.cs`; start the next UI cleanup from
  remaining broad adapters not covered by controller ownership tests.
- Keep `MainViewModel` as a compatibility facade while moving feature state to
  capture, recording, audio, Flashback, diagnostics, and automation adapters.
- `Sussudio/Services/Automation/IAutomationViewModel.cs` keeps the aggregate
  compatibility contract, grouped feature ports, and `AutomationViewModelPorts`
  composition adapter. Keep those ports in one file until a consumer needs a
  stronger reason to split them. The automation dispatcher consumes readiness/device-selection/snapshot-query,
  capture-settings, audio, and preview-recording ports for matching command
  families, plus the UI, Flashback, and probe ports for matching focused
  command partials. Audio-ramp trace reads use the snapshot-query port. Window
  screenshots remain on `IAutomationWindowControl`. `AutomationDiagnosticsHub`
  consumes the snapshot-query port for read-only diagnostic and verification
  snapshots.
- `MainViewModel.Composition.cs` owns the default service graph for the root
  compatibility view model until a fuller app composition root injects feature
  view models and narrower ports.
- `Sussudio/Controllers/ViewModel/MainViewModelControllerGraph.cs` owns
  construction order for the view-model controller graph plus UI-dispatch and
  device-audio, device-refresh, capture-settings automation, source telemetry,
  runtime event-ingress, recording, preview lifecycle/reinitialize, capture
  option rebuild, device-format probe, runtime lifecycle, and disposal graph
  ports. Keep
  service construction in `MainViewModel.Composition.cs`, and keep
  `_runtimeLifecycleController.Start()` plus initial presentation timing in the
  root constructor after all graph fields have been assigned.

## Tooling And Diagnostics

Primary owners:

- `tools/ssctl/` for the preferred CLI.
- `tools/McpServer/` for MCP bridge tools.
- `tools/McpServer/Program.cs` owns MCP host bootstrap, stdio transport
  registration, tool discovery, and the `PipeClient` DI adapter over the shared
  automation command transport.
- `tools/Common/` for shared tool helpers that are not contracts, including
  pipe client, snapshot formatting, diagnostic sessions, diagnostic scenario
  cataloging, diagnostic-session pipe retry policy, PresentMon probing, and
  shared JSON options.
- `tools/Common/AutomationPipeClient/` owns the shared pipe-client helper family
  used by ssctl, MCP, diagnostic sessions, and smoke tools.
- `tools/Common/AutomationPipeClient/AutomationPipeClient.Transport.cs` owns
  named-pipe connect orchestration, pipe connect failure classification,
  exact CLI/MCP diagnostic error codes, request/response framing, and response
  timeout.
- `tools/Common/AutomationPipeClient/AutomationPipeClient.Commands.cs` owns
  command envelope sending, typed `AutomationCommandKind` command-id routing,
  `not_ready` retry behavior, and response-state parsing handoff to
  `Sussudio.Automation.Contracts/AutomationPipeClientModels.cs`.
- `tools/Common/AutomationPipeClient/AutomationCommandTransport.cs` owns
  command-specific timeout selection for string and typed commands, shared
  response-element validation, synthetic error shaping, and the handoff to
  `Sussudio.Automation.Contracts/AutomationPipeClientModels.cs`.
- Fixed MCP routes whose commands exist in `AutomationCommandKind` should call
  the typed MCP `PipeClient.SendCommandAsync(AutomationCommandKind, ...)`
  overload at the pipe seam; fixed ssctl routes should do the same through
  `PipeTransport.SendCommandAsync(AutomationCommandKind, ...)`. The shared
  command transport must keep those enum calls typed until the request envelope
  is created. Do not list converted routes here; the shared catalog, per-file
  MCP owner bullets, and `McpToolSurface.*` source guards are the source of
  truth. String command names remain only for catalog/manifest-backed dynamic
  batches and diagnostic-session command callbacks.
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
- `tools/ssctl/CommandHandlers.Observability.cs` owns diagnostic and
  observability CLI commands: state, diagnostics, options, manifest, timeline,
  memory, audio-ramp, `presentmon` parsing/swap-chain discovery/probe
  invocation, and `diagnostic-session` parsing/runner invocation.
- `tools/ssctl/CommandHandlers.CaptureControls.cs` owns preview/record,
  screenshot/frame capture, device refresh/list/select, audio-input selection,
  custom-audio enablement, and `set` capture/audio/output mutations, including
  the shared set-value payload helper. Fixed ssctl automation routes should
  call shared enum overloads with `AutomationCommandKind` values; labels and
  wire command IDs remain catalog owned. Dynamic diagnostic-session runner
  command names stay string-based at the transport seam.
- `tools/ssctl/CommandHandlers.Window.cs` owns window close arming, window
  state/geometry actions, fullscreen toggles, snap commands, the
  recordings-folder CLI command, stats visibility, settings visibility, and
  frame-time overlay visibility commands.
- `tools/ssctl/CommandHandlers.AutomationFlow.cs` owns wait/assert/probe and
  recording/file verification scripting flow commands.
- `tools/ssctl/CommandHandlers.Flashback.cs` owns Flashback enablement,
  timeline, segment, restart, playback/scrub/marker/range actions, position
  parsing, export flag parsing, output-path defaulting, parent-directory
  creation, and `FlashbackAction`/`FlashbackExport` payload shaping.
- `tools/NativeXuAudioProbe/Program.cs` owns probe command routing, command
  workflows, and probe-local runtime shims for linked app service sources;
  `Program.Commands.cs` owns Native XU command IDs and shared
  raw-payload formatting;
  `Program.AtCommands.cs` owns direct AT read/write/input subcommands;
  `Program.DefaultExperiment.cs` owns the default baseline/experiment/restore
  runner, experiment spec records, and analog-gain sequence;
  `Program.DefaultExperiment.Reporting.cs` owns default experiment AT
  read/decode/diff/snapshot reporting plus readback/result-diff records;
  `Program.I2cCommands.cs` owns the exploratory `i2c-cmd` command family:
  router, basic get/set/scan paths, selector transport probing,
  high-selector probing, topology/property-set probing, and I2C
  SET/readback/restore verification;
  `Program.I2cLegacyProbe.cs` owns the legacy `i2c-probe` selector scan and
  raw/AT-wrapped I2C frame experiment;
  `Program.I2cSwitch.cs` owns the captured audio-switch replay workflow;
  `Program.DefaultExperiment.cs` owns default experiment payload construction;
  `Program.I2cTransport.cs` owns I2C-over-AT transport helpers; and
  `Program.ServiceProbe.cs` owns service-control smoke/payload workflows.
- `tools/KsAudioNodeProbe/Program.cs` owns KS audio node probe argument parsing,
  interface selection, open failure handling, and workflow dispatch;
  `Program.ScanWorkflows.cs` owns set-and-hold, topology, brute-force,
  and full-probe orchestration; `Program.ScanWorkflows.Extended.cs` owns
  extended-node mutation tests, ADC volume, mux, and mute probe workflows; and
  `Program.NativeInterop.cs` owns SetupAPI, file-handle, KS property transfer,
  native interop constants/DTOs, topology enumeration, and Win32 formatting
  helpers.
- `tools/EgavdsAudioProbe/Program.cs` owns EGAVDS audio probe command flow,
  device lookup, audio input/gain actions, and result text; `Program.NativeInterop.cs`
  owns SWIG callback registration, EGAVDeviceSupport entry points, SetupAPI
  entry points, and native interface DTOs.
- `tools/ssctl/Program.cs` owns the process entry point, Ctrl-C cancellation,
  CLI option parsing, and exit-code shaping.
- `tools/ssctl/SsctlHelpWriter.cs` owns the `ssctl` help facade,
  operator-facing help section text, and catalog-backed CLI help lines.
- `tools/ssctl/CommandHandlers.cs` owns the root command dispatcher, the
  per-invocation command context wrapper, shared command sending, and response
  exit-code shaping.
- `tools/ssctl/CommandHandlers.Arguments.cs` owns usage validation, required
  words, argument joining, flag consumption, optional flag value parsing, and
  command-handler JSON detection/pretty-printing. `CommandHandlers.Values.cs`
  owns primitive parsing, Flashback numeric validation, on/off and show/hide
  parsing, recording format normalization, snap action mapping, and assertion
  value parsing.
- The `tools/ssctl/Formatters.*.cs` partial family is the console projection
  facade only.
- `tools/ssctl/Formatters.Snapshot.cs` owns app snapshot orchestration, section
  ordering, and simple row sections for Sussudio state/capture-command summary,
  audio, capture settings, friendly/exact frame-rate summary formatting,
  runtime video-pipeline text, thread-health section order plus source-reader
  and WASAPI row text, recording, diagnostics, legacy performance, process CPU,
  Memory/GC, thread-pool, capture cadence, low-FPS, jitter/drop, MJPEG packet
  fingerprint, sampled visual cadence, AV-sync drift, encoder correction,
  preview renderer-mode routing, GPU playback summary, non-D3D
  fallback frame/cadence, D3D renderer section text, D3D CPU timing,
  pipeline-latency, frame-latency wait, frame ownership, DXGI frame-stat text,
  slow-frame formatter delegation, source dimensions, source frame-rate
  summary, HDR, and source telemetry snapshot text.
- `tools/ssctl/Formatters.Snapshot.Flashback.cs` owns Flashback snapshot
  active/failure gating, section and encoding subsection ordering, Flashback
  encoder/buffer/cache/cleanup text, queue-latency/backpressure/failure/GPU
  queue text, export progress/result text, playback state/command-queue text,
  and playback cadence/decode/frame/stage/A/V drift text.
- `tools/ssctl/Formatters.Snapshot.Mjpeg.cs` owns MJPEG timing snapshot
  activation, header, output order, decode/copy/callback/per-decoder timing,
  compressed queue/drop/reorder/pipeline timing, and preview-jitter queue,
  input/output/latency, ownership, and underflow snapshot text.
- `tools/ssctl/Formatters.Options.cs` owns capture option and device lists.
- `tools/ssctl/Formatters.Timeline.cs` owns performance timeline response
  validation, JSON row projection, private row model, table output, and
  first-vs-last trend summary text.
- `tools/ssctl/Formatters.Common.cs` owns shared result/JSON helpers, recent
  diagnostic-event output, and standalone memory/GC summaries.
- `tools/McpServer/Tools/AppStateTools.cs` owns the public app-state,
  diagnostic-event, and memory/GC/thread-pool MCP entry points.
- `tools/McpServer/Tools/CaptureSettingsTools.cs` owns the public device,
  capture settings, pipeline settings, and structured capture-options MCP entry
  points.
- `tools/McpServer/Tools/WindowTools.cs` owns the public window action,
  full-screen, recordings-folder, and UI visibility/settings MCP entry points.
- `tools/McpServer/Tools/PerformanceTimelineTools.cs` owns the public MCP
  tool entry point and command response handling.
- `tools/McpServer/Tools/PerformanceTimelineTools.Rows.cs` owns timeline JSON
  row projection orchestration and the root cadence, preview/MJPEG/D3D,
  Flashback playback, Flashback export, and system projection field groups.
  `tools/McpServer/Tools/PerformanceTimelineTools.Rows.Model.cs` owns the
  private row model for the same table and trend-rendering fields.
- `tools/McpServer/Tools/PerformanceTimelineTools.Rendering.cs` owns timeline
  table text rendering, first-vs-last trend text, preview cadence,
  visual/MJPEG fingerprint, jitter, D3D, slow-stage, Flashback playback,
  command, failure, cleanup, stage, export trend text, and target-summary
  orchestration.
- `tools/McpServer/Tools/PerformanceTimelineTools.Formatting.cs` owns compact
  cell, command-message, optional-value, preview jitter-depth, D3D bottleneck,
  Flashback stage, cleanup, export, and byte-rate formatting.
- `tools/McpServer/Tools/PerformanceTimelineTools.Summaries.cs` owns 1%-low
  target summaries, shared summary predicates, preview, Flashback, and system
  pressure summaries, and pressure counters.
- `tools/McpServer/Tools/FramePacingVerdictTools.cs` owns the public
  `get_frame_pacing_verdict` MCP tool entry point, pipe command orchestration,
  response shaping, performance-timeline projection, snapshot cadence channel
  projection, recent-interval parsing, readiness and verdict policy, private
  row/channel records, and the operator-facing verdict text.
- `tools/McpServer/Tools/FlashbackTools.cs` owns the Flashback MCP tool type:
  enable/apply commands, segment-list command routing/text, playback/scrub
  action normalization, validation and payload shaping, plus export duration/path
  validation, default path selection, export payload shaping, and export result
  text.
- `tools/McpServer/Tools/VerificationTools.cs` owns the public verification MCP
  methods, command names, payload shaping, and verification response timeout
  policy, assertion JSON parsing and `JsonElement.Clone()` lifetime safety,
  and verification lookup from `Data.Verification` and
  `Snapshot.LastVerification`.
  `tools/McpServer/Tools/VerificationTools.Formatting.cs` owns recording,
  file, assertion, mismatch, and failure result text.
- `tools/McpServer/Tools/PreviewFrameCaptureTools.cs` owns the public preview
  frame-capture and window-screenshot MCP entry points, default output paths,
  payload shaping, enum command routing, failure/missing-data response handling, operator-facing
  report layout, 16-bin histogram projection, and blank/dark/bright/framing
  diagnosis policy.
- `tools/McpServer/Tools/PresentMonTools.cs` owns public PresentMon MCP entry
  points, structured-content shape, probe invocation, and app-snapshot
  request/fallback behavior. Shared option precedence and preview-present field
  extraction live in `tools/Common/PresentMon/PresentMonProbe.Options.cs`.
- `tools/Common/DiagnosticSessionModels.cs` owns diagnostic session run
  options, sampled snapshot DTOs, shared tool invocation defaults, and the ssctl
  usage string.
- `tools/Common/DiagnosticSessionScenarioCatalog.cs` owns scenario name
  constants, MCP-compatible scenario description text, the CLI help-list
  constant, the `Names` projection, normalization, entry lookup, requirement
  queries, and export-verification lookup.
  `tools/Common/DiagnosticSessionScenarioCatalog.Entries.cs` owns scenario
  ordering plus core, Flashback playback, Flashback export/lifecycle, Flashback
  recording/rejection, and combined scenario metadata.
- `tools/Common/DiagnosticSessionResult.cs` owns diagnostic-session summary DTO
  fields: core metadata, artifact paths, terminal state, actions, warnings,
  end-of-run overview, capture/source, Flashback playback/recording/export,
  preview cadence, preview scheduler, and preview D3D result fields.
- `tools/Common/DiagnosticSessionResultBuilder.cs` owns diagnostic-session
  result phase orchestration, artifact-write handoff, summary-write handoff,
  final summary emission, summary-write failure repair, and final-result
  orchestration from analysis and artifact paths into the named projection set
  and flattening owner. It also owns Flashback playback projection composition
  from focused playback projection owners, plus the result-build request
  handoff created by `DiagnosticSessionRunner.cs` and consumed by the result
  builder. Keep `summary.json` field shape stable in the builder family.
- `tools/Common/DiagnosticSessionResultBuilder.Flattening.cs` owns final
  `DiagnosticSessionResult` DTO assignment from the projection set. Keep
  domain projection composition in the projection owners and projection-set
  owner, not in this initializer.
- `tools/Common/DiagnosticSessionResultBuilder.Projections.cs` owns the
  private projection-set handoff record, projection-set assembly, and the
  small result projection records/builders for overview, capture, Flashback
  recording/export, preview cadence/scheduler, preview D3D, and visual
  cadence.
- `tools/Common/DiagnosticSessionResultBuilder.Analysis.cs` owns
  diagnostic-session metric preparation for validation/result projections,
  analysis warning emission, Flashback playback/export analysis warning text,
  threshold guards, tolerated Flashback scenario warning classification, and the
  handoff into the named analysis-validation owner before summary construction.
  It also owns the private analysis handoff record, including the single
  `PreviewScheduler` record property used by preview-scheduler result
  projection.
- `tools/Common/DiagnosticSessionResultBuilder.AnalysisValidation.cs` owns
  diagnostic-session validation handoff order for Flashback playback, cleanup
  lifecycle restore, preview scheduler analysis, and diagnostic health,
  including cleanup restore warnings after diagnostic sessions stop recording,
  preview, Flashback, or playback state.
- `tools/Common/DiagnosticSessionResultBuilder.DiagnosticHealth.cs` owns
  diagnostic-session health summary snapshot selection, health verdict
  composition, source-reader/ingest warning deltas for sparse source-capture
  tolerance, sparse preview-scheduler warning tolerance, tolerated-warning
  reason selection, and health warning text emitted during result construction.
- `tools/Common/DiagnosticSessionResultBuilder.PreviewScheduler.cs` owns
  diagnostic-session preview-scheduler analysis handoff values: MJPEG
  jitter-buffer counters, deltas, last drop/underflow reasons, underflow ages,
  max schedule-late aggregation, and Flashback preview-scheduler validation
  orchestration during result analysis: target-FPS fallback, visual-cadence
  tolerance checks, sparse deadline/drop tolerance selection, and the call into
  shared Flashback preview validation.
- `tools/Common/DiagnosticSessionResultBuilder.FlashbackPlaybackResult.cs` owns
  Flashback playback result projection composition plus the command, cadence,
  1% low, decode, audio-master, and stage DTO value maps consumed by the final
  result initializer.
- `tools/Common/DiagnosticSessionResultArtifacts.cs` owns diagnostic-session
  result artifact path construction, pre-summary sample, frame-ledger, and
  timeline artifact writes, frame-ledger trace shaping, and shared JSON object
  creation / artifact serialization helpers.
- `tools/Common/DiagnosticSessionRunState.cs` owns diagnostic-session terminal
  exception state, last-stage tracking, and best-effort artifact write failure
  recording.
- `tools/Common/DiagnosticSessionLiveStateWriter.cs` owns the best-effort
  `session-live.json` breadcrumb path, payload shape, health projection,
  warning projection, terminal override mapping, and sampling live-state write
  throttle.
- `tools/Common/DiagnosticSessionRunContext.cs` owns diagnostic-session core mutable run infrastructure:
  bootstrap, scenario normalization, scenario-plan selection, duration/sample
  clamping, session identity, output-directory creation, runner process
  metadata, actions, warnings, samples, run state, command channel, scenario
  cancellation source, initial snapshot state, baseline snapshot capture,
  automation response shape helpers for snapshot and verification envelopes,
  unknown-state warning, live-state handoff, run-context disposal, and
  scenario/completion context construction.
- `tools/Common/DiagnosticSessionRunner.cs` owns the public diagnostic-session
  compatibility surface, phase sequencing around context creation, initial
  snapshot capture, scenario phase invocation, cleanup, post-cleanup evidence/result sequence, result-build
  request mapping, post-run performance timeline and final health snapshot fetches, result-build
  invocation, terminal live-state write, and completion context handoff consumed by the post-cleanup completion phase. Keep the
  `timeline` and `final-snapshot` stage names stable there. It also owns the
  per-output-directory exclusive lock that prevents concurrent diagnostic
  sessions from writing the same artifact set.
- `tools/Common/DiagnosticSessionScenarioPhaseRunner.cs` owns the named
  diagnostic-session scenario phase: state-mutation gating, setup/startup,
  scenario sampling, snapshot sample collection, completion delegation, fault
  drain delegation, and the cleanup result consumed by `RunAsync`. Preserve
  sample-loop ordering: append the cloned sample before running checkpoint
  callbacks.
- `tools/Common/DiagnosticSessionScenarioPhaseModels.cs` owns the explicit
  scenario phase input handoff, mutable in-flight phase state, and immutable
  scenario phase result handoff consumed by completion.
- `tools/Common/DiagnosticSessionBackgroundTasks.cs` owns diagnostic-session
  scenario background task registration, deterministic await order, normal
  registered scenario completion, PresentMon and deferred recording-settings
  task tracking, interrupted task observation, warning collection, and the drain
  result handoff.
- `tools/Common/DiagnosticSessionScenarioStartup.cs` owns diagnostic-session
  optional background startup orchestration, Flashback scenario registration
  delegation, deferred Flashback recording-settings task registration, and the
  direct Flashback playback start command. It also owns optional PresentMon
  launch, correlation snapshot capture, and `presentmon.csv` output selection
  for diagnostic sessions. Keep task stage names stable there.
- `tools/Common/DiagnosticSessionScenarioSetup.cs` owns diagnostic-session
  initial setup ordering, Flashback enable/disable for scenario requirements,
  preview start and video-flow readiness wait, recording start and Flashback
  recording-readiness wait, plus setup result records. Keep fixed setup
  mutations on `DiagnosticSessionCommandChannel` typed `AutomationCommandKind`
  sends.
- `tools/Common/DiagnosticSessionCleanupActions.cs` owns diagnostic-session
  cleanup flow, ordering, stage/action naming, cleanup result handoff, recording
  stop for verification, Flashback playback go-live restore, preview stop, and
  Flashback enable-state restore through typed automation commands.
- `tools/Common/DiagnosticSessionRecordingChecks.cs` owns post-cleanup
  diagnostic-session recording checks: deferred Flashback recording-settings
  restore, last-recording or Flashback export verification command selection,
  payload shape, 60-second timeout, cloned verification result, skipped-
  verification action text, and Flashback recording validation. Keep the
  `settings-deferred-restore`, `recording-verification`, and
  `recording-validation` stage names stable there.
- `tools/Common/DiagnosticSessionFlashbackCycleScenarios.cs` owns Flashback
  restart/encoder cycle diagnostic task registration, restart-cycle playback
  priming/restart/refill/export verification, and encoder-cycle preset cycling,
  snapshot validation, export verification, and original-preset restore.
- `tools/Common/DiagnosticSessionMetrics.cs` owns read-only diagnostic-session
  metric DTOs and projections: source/preview/visual cadence aggregation,
  visual-cadence health classification, D3D metric aggregation, playback
  command-health deltas, and shared counter-delta helpers.
- `tools/Common/DiagnosticSessionFlashbackExports.cs` owns rotated-export
  segment-count parsing, strict export verification payload construction, and
  range-selection cleanup, plus the audio-toggle companion used by the range
  export audio-switch scenario.
- Flashback export diagnostic scenario flows live in focused files:
  `tools/Common/DiagnosticSessionFlashbackExportScenarios.cs` owns export
  scenario task registration, concurrent export, and rotated export;
  `tools/Common/DiagnosticSessionFlashbackExportScenarios.DisableDuringExport.cs`
  owns disable-during-export command coordination, file verification, and
  post-disable/re-enable state checks;
  `tools/Common/DiagnosticSessionFlashbackExportScenarios.Playback.cs`
  owns export-during-playback command choreography, the pre-export Playing
  sample, post-export playback continuity validation, and final go-live playback
  command-health validation, and
  `tools/Common/DiagnosticSessionFlashbackExportScenarios.Range.cs` owns
  selection-range export orchestration, range buffer-readiness waits, near-live
  range projection, playback seeking plus in/out marker mutation, range
  duration/status validation, and post-cleanup playback command-health
  validation.
- `tools/Common/DiagnosticSessionFlashbackLifecycleScenarios.cs` owns
  Flashback playback disable/re-enable lifecycle diagnostic command flow,
  scenario registration, priority, task label, started action, post-disable
  playback-thread/queue health checks, and post-re-enable active-state
  validation.
- `tools/Common/DiagnosticSessionFlashbackMetrics.RecordingExport.cs` owns the
  `FlashbackRecordingSessionMetrics` and `FlashbackExportSessionMetrics`
  handoff shapes, read-only recording metric projection, export-relevance and
  snapshot max aggregation, export metric orchestration, and final force-rotate
  fallback counters.
  `tools/Common/DiagnosticSessionFlashbackMetrics.PlaybackSession.cs` owns the
  `FlashbackPlaybackSessionMetrics` handoff state, playback session metric
  orchestration, and end-of-session playback counter deltas. It covers observed
  identity, baseline/end snapshots, command, cadence, 1% low, decode,
  audio-master, and stage metric fields.
  `tools/Common/DiagnosticSessionFlashbackMetrics.PlaybackObservation.cs` owns
  playback snapshot observation dispatch, active/relevant snapshot gating,
  session frame-count projection, 1% low window capture, frame/decode maxima,
  and audio-master maxima.
  `tools/Common/DiagnosticSessionFlashbackMetrics.PlaybackResult.cs` owns the
  `FlashbackPlaybackResultMetrics` handoff shape, final result metric
  construction, observed-gated primitive reads, and the grouped end-snapshot
  command, cadence, decode, audio-master, and stage metric reads.
  Export metrics include force-rotate fallback total, delta, and last fallback
  segment count; keep those counters derived outside export-observed relevance gating.
- `tools/Common/DiagnosticSessionFlashbackPreviewCycleScenarios.cs` owns
  Flashback preview-cycle diagnostic task registration, priorities, task labels,
  started action strings, normal Flashback preview-cycle stop/restart command
  choreography, pre-stop encoded-frame capture, preview-off Flashback/encoder
  validation, export-while-preview-off verification, and restart frame-flow
  validation.
- `tools/Common/DiagnosticSessionFlashbackPreviewCycleScenarios.Playback.cs`
  owns playback-under-preview-stop diagnostic command choreography, pre-stop
  playback-frame warmup, preview-stopped playback/live-state validation,
  export-while-preview-off verification, and restart frame-flow validation.
- `tools/Common/DiagnosticSessionFlashbackPreviewCycleScenarios.Recording.cs`
  owns Flashback-recording-backed preview stop/restart diagnostic command
  choreography, readiness and pre-stop encoder counter capture, preview-off
  recording/backend/counter validation, and restart frame-flow validation.
- `tools/Common/DiagnosticSessionFlashbackRejectedExports.cs` owns Flashback
  rejected-export diagnostic scenario dispatch, inactive-buffer failure-kind
  assertions, and active-Flashback-recording backend-stability assertions.
- `tools/Common/DiagnosticSessionFlashbackRecordingSettingsScenarios.cs`
  owns deferred recording-settings preset state, during-recording preset
  mutation, restart/disable rejection-message policy, active-recording
  backend/file/counter stability checks, post-stop preset verification,
  encoder-frame checks, and original-preset restore verification.
- `tools/Common/DiagnosticSessionFlashbackSegmentPlaybackScenarios.cs` owns the
  Flashback completed-segment playback scenario: task registration, target
  acquisition, boundary-crossing playback, go-live restore, snapshot/FPS/
  command-health validation, and recording-assisted segment rotation fallback.
- `tools/Common/DiagnosticSessionFlashbackSegments.cs` owns read-only
  `FlashbackGetSegments` response parsing, completed-segment discovery,
  playable completed-segment target selection, buffered-boundary projection,
  playback headroom polling, and the parsed segment DTOs. Do not add
  state-mutating scenario steps to the segment helper.
- `tools/Common/DiagnosticSessionFlashbackStressScenario.cs` owns Flashback
  stress thresholds, stress/scrub-stress task registration, main stress and
  scrub-stress command choreography, stress export verification, warmed-playback
  frame/FPS/1% low checks, audio-master fallback delta capture/classification,
  shared command-drain polling, and command-health/latency/final-state warning
  policy.
- `tools/Common/DiagnosticSessionFlashbackWaits.cs` owns read-only snapshot
  polling waits for preview-active state, Flashback-active state,
  Flashback-backed recording readiness, stress buffer readiness, playback
  state, boundary crossing, warmed-playback frame-count/FPS, and position
  convergence.
- `tools/Common/DiagnosticSessionFlashbackValidation.cs` owns Flashback
  recording, playback, and preview scheduler warning policy over already
  projected metrics.
- `tools/Common/DiagnosticSessionHealthPolicy.cs` owns diagnostic-session health
  observation, severity, and Flashback warmup filtering.
- `tools/Common/DiagnosticSessionHealthTolerances.cs` owns diagnostic-session
  source/preview/Flashback health-observation classifiers, sparse-cadence
  tolerances, and tolerated Flashback warning classification.
- `tools/Common/DiagnosticSessionResultFormatter.cs` owns the public
  human-readable diagnostic-session text flow used by ssctl and MCP plus
  all rendered section rows: overview/health/evidence, capture mode, recording
  verification, PresentMon, Flashback playback/recording/export, preview
  scheduler, preview D3D, visual cadence, process performance, artifacts,
  actions, warnings, and shared optional text formatting used by scenarios,
  result builders, result formatters, and validation policies. Keep
  `DiagnosticSessionRunner.Format(...)` as the stable compatibility wrapper.
- `tools/Common/AutomationSnapshotFormatter.cs` owns the top-level shared
  automation snapshot console text flow, state/capture-command queue,
  selected-device and initialized/preview/recording text, audio enablement,
  capture option, recording format, HDR, pipeline, compact UI setting text,
  preview, signal, clipping, reader and audio-frame text, video reader, encoder
  queue, queue-latency, backpressure, failure, GPU/CUDA queue, freshness,
  diagnostics, thread-health row text, recording output, backend, integrity,
  audio-integrity and last-finalize text, diagnostic health, summary, evidence
  and frame-lane text, plus legacy performance, process CPU, memory, GC, and
  thread-pool text.
  `tools/Common/AutomationSnapshotFormatter.CaptureCadence.cs` owns capture
  cadence, low-FPS, jitter/drop, MJPEG packet fingerprint, sampled visual
  cadence, AV-sync drift and encoder correction text, preview routing, source
  dimensions, source frame-rate summary, HDR, source telemetry text, and routing
  to MJPEG/Preview D3D sections.
  `tools/Common/AutomationSnapshotFormatter.Values.cs` owns automation
  response-success detection, tolerant JSON string/bool/numeric accessors, and
  shared byte, number, interval, frame-budget, and tick-age display helpers,
  while `tools/Common/AutomationSnapshotFormatter.Flashback.cs` owns the Flashback
  gate, header, subsection ordering, encoding status/health text, export
  progress/result text, playback command text, and playback cadence/decode/frame
  stage/A/V drift text.
  `tools/Common/AutomationSnapshotFormatter.MjpegTiming.cs` owns MJPEG timing
  activation, header, output order, decode/copy/callback/per-decoder timing
  text, compressed queue/drop-reason/reorder/pipeline timing text, and MJPEG
  preview-jitter queue/input/output/latency/ownership/underflow text. The
  `tools/Common/AutomationSnapshotFormatter.PreviewD3D.cs` owns the remaining
  named D3D snapshot section: routing/header order, CPU timing, pipeline-latency,
  frame-latency wait text, frame-ownership, DXGI frame-stat text, reusable
  slow-frame diagnostics, and diagnostic millisecond formatting.
- `tools/Common/DiagnosticSessionPipeRetryPolicy.cs` owns diagnostic-session
  connect retry classification and local failure-response envelopes.
- `tools/Common/DiagnosticSessionCommandChannel.cs` owns serialized
  diagnostic-session automation command sending, command failure accounting,
  and `AutomationCommandKind`-to-catalog command-name resolution for fixed
  channel-owned commands, including setup and cleanup lifecycle mutations, raw
  command send overloads, connect-retry wrapping, local failure-response
  fallback when connect retry returns no response, and fixed wait command
  payload shaping. Keep the underlying runner delegate string-compatible.
- `tools/Common/DiagnosticSessionScenarioPlan.cs` owns the scenario plan DTO,
  creation factory, catalog lookup handoff, and grouped warning/validation
  policies, including the preview-cycle grouped predicate, used by the runner.
  Keep new scenario booleans and grouped derivations in the plan instead of
  adding string comparisons in `DiagnosticSessionRunner`.
- `tools/Common/PresentMon/PresentMonProbe.Models.cs` owns PresentMon option/result,
  summary, swap-chain, app-correlation summary, and metric DTOs.
- `tools/Common/PresentMon/PresentMonProbe.Options.cs` owns
  `PresentMonProbeCorrelation`, shared option precedence/defaulting, and
  preview snapshot correlation field extraction.
- `tools/Common/PresentMon/PresentMonProbe.Format.cs` owns PresentMon result text rendering
  used by diagnostic-session output surfaces.
- `tools/Common/PresentMon/PresentMonProbe.Csv.cs` owns PresentMon CSV parse overloads,
  selected-row filtering, summary assembly, swap-chain normalization/selection,
  header/field parsing, scalar metric reads, CSV line tokenization, and handoff
  to row/warning/correlation helpers.
- `tools/Common/PresentMon/PresentMonProbe.Csv.Rows.cs` owns PresentMon CSV row ingestion,
  header index construction, schema-presence detection, blank-line skipping,
  row index assignment, private parsed CSV row shapes, and row projection from
  header-indexed fields.
- `tools/Common/PresentMon/PresentMonProbe.Csv.Correlation.cs` owns app-present
  correlation and displayed/not-displayed outcome classification.
- `tools/Common/PresentMon/PresentMonProbe.Csv.Summary.cs` owns warnings, counted text
  fields, and percentile metric aggregation.
- `tools/Common/PresentMon/PresentMonProbe.cs` owns PresentMon public run orchestration,
  command-line construction, argument quoting, and probe-result message shaping.
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
- Add new diagnostic-session scenario names and requirement/query helpers in
  `tools/Common/DiagnosticSessionScenarioCatalog.cs` only when a new entry shape
  needs them, plus export verification metadata and plan metadata in
  `tools/Common/DiagnosticSessionScenarioCatalog.Entries.cs` before wiring
  scenario behavior into `DiagnosticSessionRunner`. Preserve the final order
  there.
- Keep diagnostic-session grouped policy derivation in
  `tools/Common/DiagnosticSessionScenarioPlan.cs`; the runner should consume
  named properties instead of comparing normalized scenario strings directly.

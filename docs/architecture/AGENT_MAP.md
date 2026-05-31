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
| Diagnostic sessions | `tools/Common/DiagnosticSessionRunner.cs`, `tools/Common/DiagnosticSessionRunContext.cs`, `tools/Common/DiagnosticSessionResult.cs` | public runner compatibility surface plus phase sequencing, named scenario phase execution, scenario sampling, post-sampling completion order/fault-drain/background-task delegation, cohesive mutable run context, initial snapshot state, live-state handoff, run context disposal, scenario/completion context construction, post-cleanup completion phase, completion context handoff, result-build request mapping, consolidated context/result/state/result DTO models, run bootstrap/options normalization, scenario catalog, startup/cleanup/recording-check/post-run snapshot helpers, result formatter, plus per-scenario runners |
| Offline regression harness | `tests/Sussudio.Tests/HarnessCore.cs`, focused `tests/Sussudio.Tests/XUnit.*.cs` slices | runner entry point, compatibility no-op check shim, shared legacy `Program` harness helpers, xUnit slices, and focused contract tests such as `XUnit.StatsPresentation.Formatting.Tests.cs` |
| Capture runtime | `Sussudio/Services/Capture/CaptureService.cs`, `CaptureService.PreviewLifecycle.cs`, `CapturePipelineResources.cs`, `CaptureService.FlashbackControls.cs`, `CaptureService.FlashbackExportCore.cs`, `CaptureService.FlashbackRecording.cs`, `CaptureService.HealthSnapshots.cs`, `CaptureService.RecordingIntegrity.cs`, `CaptureService.RecordingLifecycle.cs`, `CaptureService.RuntimeSnapshots.cs`, `CaptureService.Snapshots.cs` | service state, construction, public event/property surface, initialization owner, transition transaction/state-sampling owner, and lifecycle guards, preview start/stop/recycle/fast-path/reuse predicates/fresh-pipeline/video-pipeline handoff/disposal transition owner, audio preview lifecycle/volume/event/startup/rollback and live audio input switching owner, microphone monitor state/event/disposal/update/restart owner, preview audio resource owner, active recording backend resource owner, video pipeline resource owner, cleanup/disposal, resource-release helper, failure callback, failure-telemetry, fatal cleanup, and Flashback backend failure cleanup/device-lost owner, Flashback public state, segment access, enable/disable, restart, settings, buffer/GPU/format, encoder-cycle owner, preview backend startup/disposal, artifact-cleanup adapter, and Flashback buffer cycle coordination owner, Flashback export diagnostics/progress/fallback lifecycle, failure taxonomy, health projection, entry/routing and backend snapshot/lock handoff, core lifetime, request assembly, segment metadata mapping, live-export throttle policy, segment path normalization, segment PTS timestamp repair, range-resolution, buffer-position clamps, PTS offset math, and force-rotate preparation owner, Flashback recording backend/capability/session-context/frame-rate/start/finalize/export-finalize/boundary snapshot/reconciliation owner, health snapshot sampler with capture cadence/MJPEG/source telemetry, Flashback backend/queue, Flashback playback, and recording health field projections, health snapshot DTO assembler and handoff owner, read-only automation probe owner, recording integrity active-backend resolver, counter/audio DTO capture, normalized summary input, status/reason evaluation, and integrity logging owner, recording start transition/router, context request assembly, rollback-state holder, transient recording rollback, standard LibAv recording start/video/audio startup, and recording outcome-state owner, recording stop transition/finalization router owner, LibAv recording finalization/video-boundary/sink/idle-preview/preview-restore owner, runtime snapshot sampler with ingest/audio, reader/transport, recording-integrity, HDR/encoder pipeline, source-telemetry projections, private assembly handoff models, and final runtime snapshot DTO construction, diagnostics compatibility, read-only automation probes, preview-frame capture waits, shared snapshot utilities/recording stats/format/observed frames/A/V sync/source telemetry snapshot policy, source telemetry polling/fallback merge, capture-format and observed pixel telemetry owner, resource managers |
| App shell | `Sussudio/App.xaml.cs` | XAML partial root, FFmpeg startup check, global handler hookup, recoverable/fatal exception policy plus emergency recording finalization, single-instance guard, startup identity logging, and MainWindow activation |
| App surface helpers | `Sussudio/AppSurface.cs` | compact app-facing display formatters plus the XAML bool/inverse/visibility converter types used by hand-bound WinUI controls; keep public converter type names and `Sussudio.DisplayFormatters` stable |
| Logging | `Sussudio/Logger.cs` | nonblocking log writer state, rotation, channel saturation fallback, direct write path, diagnostics/system evidence, structured snapshot JSON routing, exception formatting, fatal breadcrumbs, and source-generated JSON context for known log payloads |
| Runtime paths | `Sussudio/RuntimePaths.cs` | public cached repo/temp/log path API; repo-root and log-root resolution policy, latest-build fallback, marker discovery, guarded directory creation, and trace fallback diagnostics |
| App project build workflow | `Sussudio/Sussudio.csproj`, `Sussudio/Sussudio.Build.targets` | app identity/assets/packages/runtime config in the project file; publish flags, locale stripping, and latest-build staging in imported targets |
| Device discovery | `Sussudio/Services/Capture/DeviceService.cs`, `Sussudio/Services/Capture/MfInterop.cs`, `Sussudio/Services/Capture/DeviceDiscovery/MfDeviceEnumerator.cs` | device enumeration orchestration, priority/capability scoring, Native XU interface path resolution, audio endpoint association, persisted format cache, inline/background format probing, shared MF startup/attribute helpers and symbolic-link matching, shared MF constants/P/Invokes, MF video device enumeration, WASAPI capture endpoint enumeration, native MF format probing and subtype/FourCC naming, direct/fallback MF source activation |
| Native XU KS bridge | `Sussudio/Services/Capture/NativeXu/KsExtensionUnitNative.cs` | KS category constants and DTOs, SetupAPI interface enumeration, file-handle open policy, topology node parsing, XU GET/SET transfer helpers, P/Invoke declarations and structs; shared 4K X identity/selected-interface/transport-gate support |
| Capture source reader | `Sussudio/Services/Capture/MfSourceReaderVideoCapture.cs`, `MfSourceReaderVideoCapture.FrameDelivery.cs`, `MfSourceReaderVideoCapture.Negotiation.cs`, `MfInterop.cs` | source-reader state, public counters, shared packed-frame sizing/stride/subtype helpers, initialization validation/orchestration, reader construction, actual-output reconciliation, initialized runtime-state commit, active reader start/stop/dispose plus Media Foundation read loop and source cadence metrics, sample-to-frame dispatch, compressed MJPG extraction, raw CPU frame extraction, 2D buffer handling, packed-stride CPU copies, DXGI texture extraction, dual GPU/CPU delivery orchestration, and debug-only COM diagnostics, direct device opening, device-enumeration open fallback and candidate reporting, native media-type selection, and converted output media-type construction, general Media Foundation COM interface definitions plus MF startup/attribute helpers, P/Invoke/constants/GUIDs, and flattened sample/buffer COM interface definitions |
| Capture fan-out | `Sussudio/Services/Capture/UnifiedVideoCapture.cs` | public control/config surface, source-reader/D3D/MJPEG initialization and state commit, shared source-reader start/stop/dispose lifecycle plus CPU MJPEG stop/dispose/fatal handling, source-reader frame ingress, preview submission, visual-cadence handling, fatal-error signaling, recording and Flashback sink queue fan-out, and diagnostic metric/snapshot projection |
| Capture cadence trackers | `Sussudio/Services/Capture/CaptureCadenceTrackers.cs` | source-packet hash cadence ingestion and duplicate-pattern metrics/statistics; visual-cadence state, frame-ingest orchestration, decoded-frame luma sampling/crop comparison, and metrics DTO construction/statistics/motion-confidence projection |
| Audio capture | `Sussudio/Services/Audio/WasapiAudioCapture.cs` | WASAPI state, endpoint binding/format negotiation/AudioClient startup, start/stop/dispose lifecycle, initialization-time metric resets, callback/glitch metric projection, capture thread/packet drain, WASAPI sample decode, f32le 48 kHz stereo conversion/resampling, pooled converted-packet buffers, converted-packet sink/playback/hot writer fan-out, and hot writer task-completion enforcement |
| Audio playback | `Sussudio/Services/Audio/WasapiAudioPlayback.cs` | playback state, render endpoint binding/format validation/AudioClient startup, start/stop/pause/resume/flush/dispose lifecycle, chunk queue, pooled-sample ingress, buffered-duration accounting, render-thread callback/prebuffer/buffer-fill execution, render-side PTS advancement, volume ramps, and output-level telemetry |
| WASAPI interop | `Sussudio/Services/Audio/WasapiComInterop.cs` | native constants/P/Invokes, COM release helpers, format allocation/parsing, device enumerator and endpoint volume helpers, debounced endpoint-change watcher, AudioClient activation/AudioClient3 initialization, shared audio format/PROPVARIANT structs, Core Audio device/property contracts, and AudioClient/capture/render/endpoint-volume contracts |
| MJPEG preview pacing | `Sussudio/Services/Capture/MjpegPreviewJitterBuffer.cs` | construction, suppression/reprime and disposal lifecycle, paced emit loop control flow, display-clock alignment, renderer submission, tick waits, deadline drops, adaptive target-depth policy, jitter-buffer metric records, timing sample projection, decoded preview-frame ingress, pooled payload ownership, queue ordering/dequeue selection, and reprime recovery |
| MJPEG decode pipeline | `Sussudio/Services/Gpu/ParallelMjpegDecodePipeline.cs`, `SoftwareMjpegDecoder.cs`, `NvdecMjpegDecoder.cs`, `CudaD3D11InteropBridge.cs` | pipeline construction/startup sequencing, bounded work-channel construction, compressed input admission/byte budget/depth accounting, CPU MJPEG worker decode-loop execution and decoder ownership, pipeline timing and packet-hash metrics, stop/dispose/shutdown joins/fatal callback signaling, decoder/work-item/reorder-frame resource cleanup, decoded-frame ordering, missing-sequence state, decoded-frame emission and preview notification, software MJPEG decoder initialization/lifetime, decode/copy hot path, NVDEC decoder state, standalone CUDA device/frame-pool initialization, shared CUDA device/frame-pool adoption, decode/context access, CPU download/copy helpers, disposal, and error text, CUDA-to-D3D11 bridge state, public texture handles, bridge setup/zero-copy registration, bridge disposal/resource unregister, CUDA native constants/P/Invoke declarations, and zero-copy/staging copy behavior |
| GPU telemetry | `Sussudio/Services/Gpu/NvmlMonitor.cs` | optional NVML telemetry snapshot/polling lifecycle, graceful unavailable behavior, raw NVML constants, structs, library loading, device-name helper, and P/Invoke declarations |
| Automation diagnostics | `Sussudio/Services/Automation/AutomationDiagnosticsHub.cs`, `AutomationDiagnosticsHub.Alerts.cs`, `AutomationDiagnosticsHub.Evaluation.cs`, `AutomationDiagnosticsHub.Snapshots.cs`, `AutomationDiagnosticsHub.SnapshotProjection.cs`, `AutomationDiagnosticsHub.SnapshotProjection.Flattening.AutomationSnapshot.cs`, `AutomationDiagnosticsHub.SnapshotProjection.Media.cs`, `AutomationDiagnosticsHub.SnapshotProjection.CaptureFormat.cs`, `AutomationDiagnosticsHub.SnapshotProjection.Mjpeg.cs`, `AutomationDiagnosticsHub.SnapshotProjection.Flashback.cs`, `AutomationDiagnosticsHub.SnapshotProjection.Preview.cs` | additional collectors/controllers when hub orchestration grows |
| Automation snapshot models | `Sussudio/Models/Automation/AutomationSnapshot.cs`, `AutomationRuntimeModels.cs`, `AutomationSupportModels.cs` | consolidated automation evidence DTO for app/capture/audio/preview/recording/Flashback diagnostics; `AutomationRuntimeModels.cs` owns capture runtime, preview runtime, and performance timeline DTO surfaces; `AutomationSupportModels.cs` owns command protocol DTOs/converters, automation options DTOs, support DTOs/enums for diagnostics events, Flashback segments, preview startup, screenshot/window capture, recording verification, video source/color probe, and view-model runtime snapshot DTOs |
| Capture snapshot models | `Sussudio/Models/Capture/CaptureSnapshotModels.cs` | consolidated diagnostics core/format/HDR, source telemetry, capture cadence, recording/audio queue, Flashback queue, MJPEG, and visual-cadence fields plus inherited health source/queue/AV-sync and Flashback backend/playback/export health fields |
| Capture leaf models | `Sussudio/Models/Capture/CaptureModels.cs` | device/options/settings/session-state leaf types, audio endpoint/event/path/trace DTOs used by capture and monitoring, explicit transition legality policy, mutable capture session state machine, and frame-ledger event DTOs kept together as the capture model surface |
| Recording models | `Sussudio/Models/Recording/RecordingModels.cs` | consolidated recording and Flashback model surface for media format display/equality/HDR helper policy, encoder support, integrity summary, pipeline queue options, recording byte stats, Flashback buffer/session/playback/export DTOs, and Flashback force-rotate result status |
| Source telemetry | `Sussudio/Services/Telemetry/NativeXuAtCommandProvider.cs`, `NativeXuAtCommandProvider.DeviceCommands.cs`, `NativeXuAtCommandProvider.SnapshotAssembly.cs` | ReadAsync validation/gating through shared Native XU device support, selected-interface open/topology/node scan, node-read failure classification, active rolling poll cadence/cache plus rolling command group dispatch and per-command cancellation helpers, AT-command transport/parsing including selector-4 I2C payload writes, source payload decoding/scalar helpers, public HDMI/Analog audio route and gain command entry points plus HDMI/Analog switch sequence, analog gain register mapping and writes, generic public SET-command surface and generic public read-only AT-command surface, reference full-snapshot command acquisition, full/rolling AT-command handoff contract, VIC/frame-rate lookup policy, source snapshot assembly from AT-command results, diagnostic summary formatting, source telemetry detail row assembly, flash-audio input, analog-gain detail interpretation, audio-origin policy, and AT detail value formatting |
| App service contracts | `Sussudio/Services/Contracts/ServiceInterfaces.cs`, `Sussudio/Services/Contracts/ISourceSignalTelemetryProvider.cs`, `Sussudio/Services/Contracts/RecordingContracts.cs`, `Sussudio/Services/Contracts/PooledVideoFrame.cs` | shared in-process app-service contracts and pooled-frame ownership types; `ServiceInterfaces.cs` owns automation window/diagnostics/command-dispatcher interfaces, preview sink, and `PreviewFrameTracking`; `ISourceSignalTelemetryProvider.cs` owns the probe-linked source signal telemetry provider contract plus `Sussudio.Models` source telemetry DTO/enums/detail rows; `RecordingContracts.cs` owns recording context/finalize DTOs plus raw, lease, GPU, and CUDA frame encoder contracts; `PooledVideoFrame.cs` owns the reference-counted frame and lease pair; keep these separate from `Sussudio.Automation.Contracts` wire/protocol contracts |
| Recording | `Sussudio/Services/Recording/LibAvEncoder.cs`, `LibAvEncoder.Audio.cs`, `LibAvEncoder.VideoFrames.cs`, `LibAvRecordingSink.cs`, `LibAvRecordingSink.Queueing.cs`, `Sussudio/Services/Recording/Verification/RecordingVerifier.cs` | encoder core state plus option/result DTOs, open/error/device-removed diagnostics, encoder runtime/open initialization including option validation, video codec setup, codec/filter policy, NVENC preset/split-encode mapping, rational conversion, frame-size math, and audio sample-format support, output rotation/reopen/reset, muxer option policy, final close/trailer/logging, and native resource release/reset, audio/microphone stream state, public audio submission, audio and microphone AAC stream initialization, sample queue/drain mechanics, A/V sync diagnostics, and audio packet writes, video packet drain/write helpers, CPU packed-frame submission, packed software-frame copy helpers, HDR side-data attachment/parsing for submitted frames, D3D11/CUDA hardware-frame setup and submission, sink state/construction, read-only diagnostics surface, recording sink startup shell, encoder option creation, video/GPU/CUDA session queue initialization, metric reset, video diagnostics reset, encode loop orchestration, video/GPU/CUDA/audio/microphone packet drains, dispose/deferred cleanup, recording sink stop/finalize lifecycle, stopped-output validation, and bounded HDR script validation, recording sink video/GPU/CUDA public enqueue adapters, hot audio/microphone write adapters, video/GPU/CUDA/audio/microphone queue admission, audio queue eviction, queue cleanup, audio/video/GPU/CUDA packet DTOs, pooled packet return helpers, shared video queue latency/backpressure tracker, and shared signal/failure/depth helpers, verifier orchestration/finalizer/result taxonomy plus dimensions/frame-rate/cadence/container/codec/HDR validation policy, ffprobe process work plus scalar/HDR/cadence probe parsing |
| Flashback | `FlashbackDecoder.cs`, `FlashbackDecoder.VideoSetup.cs`, `FlashbackDecoder.Playback.cs`, `FlashbackPlaybackController.cs`, `FlashbackPlaybackController.Positioning.cs`, `FlashbackPlaybackController.ThreadCommands.cs`, `FlashbackPlaybackController.AudioRouting.cs`, `FlashbackEncoderSink.cs`, `FlashbackEncoderSink.EncodingLoop.cs`, `FlashbackEncoderSink.Queueing.cs`, `FlashbackBufferManager.cs`, `FlashbackStartupCacheCleanup.cs`, `FlashbackExporter.SegmentPacketWriting.cs`, `FlashbackExporter.Lifecycle.cs`, `FlashbackExporter.Execution.cs` | decoder lifecycle/open/dispose shell plus state guards, FFmpeg error formatting, decoded stream/frame validation helpers, and decoded video/audio output DTOs, video codec setup, D3D11 device-context initialization, D3D11VA/software fallback selection, hardware decoder context setup, D3D11VA decoder discovery, hardware-config diagnostics, frame-rate metadata, MJPEG decode policy, software output-buffer allocation, decoded video frame output, hardware/software frame selection, decoded PTS helpers, and software plane copy/conversion kernels, decoder seek conversion helpers, decoder keyframe/exact seek control flow, decoder video frame receive, packet feed loop, inline audio packet delivery, audio codec/resampler initialization, callback failure handling, bounded audio output, recoverable seek log suppression, and decode phase timing accumulation, decoder file-close native cleanup and held-frame release, playback core, decoder file open/identity, decoder cleanup/close telemetry, active fMP4 reopen, seek recovery, and adjacent-segment seek fallback, segment-edge fMP4 reopen and audio gate recovery, component lifecycle, dispose, preview-detach deferred reattach lifecycle, consolidated playback metrics and seek-cap telemetry, public playback command facade, command payload contract, command queue/drop/coalescing/yield/failure/metric surface, and command telemetry bookkeeping, consolidated playback-thread lifecycle, playback thread command dequeue/wait loop, playback-thread command dispatch and completion telemetry, playback-thread seek/scrub begin/update command execution, playback-thread end-scrub resume execution, playback-thread play command execution, playback-thread pause command execution, playback-thread nudge command execution, terminal go-live/stop dispatch execution, audio callback/routing/render helpers, audio prebuffer/rewind, audio-master clock/A/V drift projection, audio-master pacing/fallback projections, decoded frame preview submission and validation/byte sizing, seek/scrub keyframe display, seek/scrub decoded-frame display handoff, live playback recovery, continuous playback loop, segment-edge routing/write-head handling, next-segment switch transaction, timing policy, decoded PTS cadence state/telemetry/projection, software-decode budget snap policy, marker command, state, file-PTS, and range owner, position/file-PTS mapping, encoder core state/construction, encoder startup transaction, startup queue construction, startup failure rollback, startup validation, queue-capacity policy, startup metric/counter reset, and video diagnostics reset, encode loop orchestration, video/GPU packet drains, audio/microphone packet drains, encoded-frame progress publication plus segment rotation/failure recovery, consolidated export force-rotation status/idle projection, request admission/result classification, lifecycle cleanup, request state machine, and encoding-thread force-rotate execution, consolidated video/GPU/audio/microphone producer input surface, lifecycle/input guards, queue admission, TryWrite depth accounting, rejection telemetry, hot WASAPI writer adapters, consolidated stop/dispose lifecycle, encoder option/session construction and recording-to-Flashback mapping, file/session helpers, shared encoder queue helpers, packet DTOs, pooled packet buffer ownership, video enqueue result classification, best-effort video packet cleanup, GPU texture release helpers, plus queued-buffer cleanup, consolidated recording state/gates, start/rollback, and finalization, public runtime counters, public queue telemetry, public encoder status/format projections, saturated PTS conversion, non-negative byte/duration math, and best-effort eviction resume fallback, buffer core state, shared buffer math and saturated accounting helpers, buffer live byte/PTS accounting updates, buffer initialize/dispose lifecycle, buffer recovery-preserve markers, purge/delete-all cleanup, guarded purge file deletion, buffer segment mutation surface, buffer segment completion/extension, buffer segment query helpers, buffer segment path safety, buffer segment status/projection helpers, buffer retention/eviction, buffer eviction-pause state and recording PTS range capture, startup stale-root/session cleanup, free-space probing, and startup session-cache budget enforcement, exporter lifecycle/disposal/native state/native cleanup/lock handling, input/output stream setup, stream-template/layout compatibility, single-file packet result validation, single-file active input packet pump, multi-segment export lifecycle/template/preflight, multi-segment packet writing/remux orchestration, active segment packet pump/write state/outcomes plus segment timestamp rebasing/native writes, packet timestamp helpers, packet buffer lifetime helpers, segment export range/window projection, segment validation policy, temp-file cleanup, output/path/range validation, export request routing/scheduling, single-file export shell, export runtime progress/pacing policy, stream/context setup, stream-template/layout compatibility, final output replacement, export failure results, export validation, FFmpeg error formatting |
| Flashback playback command handlers | `FlashbackPlaybackController.ThreadCommands.cs` | playback-thread command dispatch plus seek/scrub begin/update, end-scrub resume, play/pause, nudge/frame-step recovery, and terminal go-live/stop live-restore exits |
| Preview rendering | `D3D11PreviewRenderer.cs`, `D3D11PreviewRenderer.RenderPasses.cs`, `D3D11PreviewRenderer.Resources.cs`, `D3D11PreviewRenderer.Metrics.cs`, `PreviewScreenshotCapture.cs` | renderer public facade, construction, constants, env-tuned runtime configuration, render-thread startup/disposal state, public stop/reinit-stop lifecycle, user-facing state accessors, native SwapChainPanel bind/unbind interop, panel size/transform state, stop/unbind/native-call fence lifecycle, render-thread loop/orchestration plus shared-device reset consumption/rebind, frame-latency waitable swap-chain state/setup/waits, composition-transform wake handling, public raw/lease/single-texture frame submission entry points, dual-plane NV12 submission, HDR transition telemetry, pending-frame lifetime, queue/signaling state, queued-frame render dispatch, pending-frame consumption, explicit queue-drain control, final render-thread drain/state reset, renderer diagnostics, render-thread failure telemetry state, first-frame notification state, DXGI frame statistics state, optional `DwmFlush` interop, and display-clock projection, render-pass selection plus VideoProcessor, NV12 shader, and HDR shader execution, VideoProcessor input-view resolution, external-texture input-view helpers, raw frame direct/staging upload helpers, viewport/letterbox helpers, shared present/accounting transaction, screenshot capture request/result/PNG completion lifecycle, GPU/readback before present, and staging reuse/teardown, shader state, cached shader resources, shader compilation resources, HLSL shader sources, renderer mode labels, and D3DCompiler blob interop, shared-device COM reference handoff, reinit retirement, reset scheduling, device-lost classification/recovery, D3D device/swap-chain initialization, D3D object fields, input texture resources, HDR shader input resources, top-level cleanup orchestration, video-processor pipeline setup/teardown, output-view/RTV reuse, and VideoProcessor color-space updates, read-only present-cadence metrics state/projection, read-only latency/render/wait metrics state/projection, submitted/rendered/dropped frame ownership state and telemetry, expected-frame-rate metric window sizing/reset, renderer metric model types and shared metric sample helpers, render-loop metric window tracking, renderer diagnostics, slow-frame ring/projection/reason classification and DXGI refresh-slip capture, preview BMP/PNG pixel analysis and file encoding, timing models |
| UI shell | `MainWindow.*.cs` XAML adapters plus `Sussudio/Controllers/*Controller.cs` shell controllers | keep shell adapters thin and start new UI behavior in named controllers/policies with ownership tests |
| Presentation | `MainViewModel.*.cs` facade/feature partial family, `Sussudio/Controllers/ViewModel/MainViewModelControllerGraph.cs`, plus focused `Sussudio/ViewModels` policy/presentation helpers | keep the root facade stable while moving pure feature state, controller graph construction, policy, and presentation logic into named owners |

Preview renderer notes:

- `Sussudio/Services/Preview/D3D11PreviewRenderer.cs` owns the renderer public
  facade plus render-thread startup state, startup reset, renderer disposal,
  public stop/reinit stop, unbind-before-join ordering, native-call drain
  fencing, render-pass native-call entry/exit guards, pending-frame shutdown
  cleanup, render-thread loop shell, MMCSS registration, frame-ready wait,
  dispatch ordering, shared-device reset consumption/rebind,
  composition-transform wake handling, pending-frame consumption/render
  dispatch, frame-latency waitable swap-chain pacing, raw/lease/single-texture
  and dual-plane NV12 submission entry points, pending-frame lifetime,
  queue/signaling state, explicit pending-frame drain control, final drain, and
  renderer mode reset.
  `D3D11PreviewRenderer.Metrics.cs` owns renderer diagnostics:
  render-thread failure counters, latest failure fields, UI failure
  notification, first-frame reset/UI notification, slow-frame diagnostic
  ring/projection, and slow-frame reason classification.
- `Sussudio/Services/Preview/D3D11PreviewRenderer.RenderPasses.cs` owns
  render-pass selection plus VideoProcessor, NV12 shader, and HDR shader pass
  execution. Keep pass precedence, timing bucket attribution, viewport and
  letterbox helpers, present accounting, preview-frame capture request state,
  timeout/cancellation, pending-request cleanup, render-thread request exchange,
  GPU readback before present, BMP/PNG dispatch, error result construction,
  capture-result logging, off-thread PNG completion/encode-gate state, staging
  texture reuse/teardown, HDR fallback logging, native-call guard consumption,
  shader-resource binding, draw calls, and shader-mode present messages there.
- `Sussudio/Services/Preview/D3D11PreviewRenderer.Resources.cs` owns D3D
  device/swap-chain/video-processor fields, `InitializeD3D` orchestration,
  shared-vs-owned device setup, shared-device COM reference handoff/reinit
  retirement/reset scheduling, device-lost classification and recovery, video
  interface acquisition, media present duration setup, composition swap-chain
  creation, startup dimensions, HDR swap-chain capability probing, SDR
  swap-chain fallback, initial color-space selection, configured output size
  publication, initial panel binding, shader compilation handoff,
  renderer-owned device fallback, shader resource/cache state, NV12 SRV reuse,
  shader bytecode compilation orchestration, `D3DCompileNative` invocation plus
  `ID3DBlob` byte/error-string extraction, shader/sampler/viewport
  constant-buffer creation, compile-fallback logging, HLSL source strings,
  renderer mode labels, VideoProcessor input texture resources, HDR shader
  input resources, top-level cleanup orchestration, video-processor recreation
  orchestration, processor-resource teardown, output-view/RTV reuse, and
  VideoProcessor input/output color-space updates.
  Family-specific teardown stays next to creation: input texture cleanup in
  `D3D11PreviewRenderer.Resources.cs` and preview-frame capture staging cleanup
  in `D3D11PreviewRenderer.RenderPasses.cs`.
- `Sussudio/Services/Preview/D3D11PreviewRenderer.cs` owns native
  SwapChainPanel COM interface, bind/unbind dispatch, stale-chain guards, panel size,
  rasterization scale, dirty-transform signaling, and swap-chain composition
  matrix updates.
- `Sussudio/Services/Preview/D3D11PreviewRenderer.Metrics.cs` owns present
  cadence, pipeline latency, render CPU timing, frame-latency wait metric state,
  sample tracking, expected-frame-rate window resizing, metric reset/clear
  lifecycle, read-only projections, recent sample copies, renderer metric record
  structs, shared ring-copy, timing-summary, tick-to-ms, render-stage validation
  helpers, recent slow-frame snapshot access, thresholding, sample assembly, the
  slow-frame ring writer, slow-frame reason token classification, render-thread
  failure notification state, first-frame notification state, DXGI
  `GetFrameStatistics` sampling, optional DWM flush, counter deltas,
  missed-refresh accounting, visible-frame tick estimation,
  `IPreviewDisplayClock` snapshot projection, and the DXGI refresh-slip snapshot
  used by slow-frame diagnostics.
- `Sussudio/Services/Preview/PreviewScreenshotCapture.cs` owns preview-frame
  screenshot pixel analysis, mapped-frame buffer copying, BMP capture/header
  writing, 16-bit PNG frame capture, and the PNG container/chunk/CRC helpers.

## Automation

Primary owner: `Sussudio.Automation.Contracts/`

Entry points:

- `AutomationCommandKind.cs` owns numeric command IDs. Append only; never
  renumber or reuse values.
- `AutomationCommandCatalog.cs` owns command lookup, canonical name resolution,
  default metadata helpers, path-policy types/validation, manifest DTO
  projection, stable manifest JSON serialization, and command metadata table
  registration orchestration plus grouped core, capture, UI, Flashback, and
  verification metadata rows; keep payload shape, readiness gating, timeout
  policy, CLI help, MCP descriptions, and path-policy assignments beside the
  command family they describe.
- `AutomationPipeProtocol.cs` owns pipe names, auth env var, manifest revision,
  command resolution, request envelope shape, the fallback-security predicate
  shared by app and tests, pipe command result handoff, pipe client exception
  taxonomy, tolerant response-state parsing, synthetic error-envelope factory,
  exception-to-error-code mapping, and throw-vs-synthetic unknown-command
  policy shared by command transports and retry policy.
- `tests/Sussudio.Tests/AutomationToolContracts.Tests.cs` owns the golden
  numeric command-ID adapter. Routing tests should assert captured
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
  command envelope, correlation setup, manifest revision validation,
  authentication command handling, unauthorized-command rejection,
  device-readiness gating, payload extraction, command metadata/path helpers,
  enum payload parsing, shared response shaping, Flashback rejection
  diagnostics, UI/settings command application, the show-all compatibility
  no-op, stats-section expand/collapse response text, and port-typed
  trivial-handler dispatch before the custom command router. Construct it with
  `AutomationViewModelPorts`; this dispatcher root should not expose or store
  the aggregate automation ViewModel dependency.
- `Sussudio/Services/Automation/AutomationCommandDispatcher.CustomCommands.cs`
  owns the custom automation command router for multi-field payloads, special
  response shapes, capture routing, domain command handoff, read-only
  snapshot/manifest/diagnostic/timeline/audio-ramp readback commands,
  verification commands, visual probe/capture commands, and the small
  device-selection, audio-control, capture-control, output-path, and
  recording-enable command bodies it dispatches, plus Flashback action/export/
  segment/restart/enable command bodies behind the custom command router.
- `Sussudio/Services/Automation/IAutomationViewModel.cs` owns the aggregate
  automation ViewModel contract plus feature-shaped ports for readiness,
  snapshot queries, device selection, capture settings, audio, preview/recording,
  UI, Flashback, and probes. It also owns `AutomationViewModelPorts`, the
  composition-time adapter from the aggregate compatibility contract to named
  port targets. Keep those ports grouped in this file until a consumer needs a
  separate file; do not create many tiny interface files for line-count optics.
  `AutomationCommandDispatcher.cs` owns manifest revision, auth-token, and
  readiness gating beside the command envelope, the port-grouped tables and
  ordered dispatch for UI/settings plus simple one-property commands, and the
  payload/path/enum helpers used by all dispatcher command bodies, plus the
  target-typed trivial-handler wrapper used by the one-property command tables.
  `AutomationCommandDispatcher.CustomCommands.cs` consumes the
  device-selection, audio, capture-settings, preview/recording, snapshot-query,
  diagnostics, probe, and window-control ports for custom command bodies,
  including AssertSnapshot response shaping, assertion payload parsing, snapshot
  comparison helpers, WaitForCondition response shaping, wait-condition polling,
  snapshot predicates, full-screen, recordings-folder, arm-close, close-arm
  gating, and low-level window action execution. It also consumes the Flashback
  port for Flashback action/export/segment/restart/enable command bodies.
- `Sussudio/Services/Automation/NamedPipeAutomationServer.cs` owns automation
  pipe constructor/configuration state, server start/stop/dispose, the accept
  loop, per-connection safety/disposal, request-session handoff, error/timeout
  responses, fallback trace logging, per-request JSON framing, client PID
  logging, dispatch timeouts, late dispatch observation, response writing,
  Windows pipe security descriptor setup, fallback policy, P/Invoke, and secure
  stream creation.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.cs` owns polling,
  field/constructor state, start/stop/dispose behavior, and the polling loop.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.Snapshots.cs` owns
  preview jitter, MJPEG, D3D, and Flashback recording recent-counter baselines
  and delta updates because those baselines are sampled only by the snapshot
  refresh loop.
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
  alerts, audio-master fallback alerts, audio-queue backlog alerts, diagnostics
  event publication, event throttling, Flashback export completion events, and
  recent event storage.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.Snapshots.cs` also owns
  manual recording/file verification entry points, flashback-export
  verification profile shaping, event publication for explicit verification,
  last-verification snapshot state, post-recording auto-verification gating, and
  background scheduling.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.Evaluation.cs` owns
  performance scoring, root diagnostic verdict orchestration, final
  healthy/mixed diagnostic fallback, Flashback-specific diagnostic verdict
  ordering, Flashback storage pressure, recording encoder failure,
  export-rotation gap, backend staleness, recording degradation, Flashback
  recording diagnostic condition assembly, active/stalled export, playback
  command, playback performance, frametime, and submission diagnostic verdicts,
  realtime diagnostic verdict ordering, idle/warmup/recording/audio/source/MJPEG
  and preview verdicts, shared renderer-drop threshold constants, diagnostic
  lane text orchestration, MJPEG decode lane formatting, source
  cadence/source-signal lane formatting, recording/audio lane formatting,
  preview scheduler/renderer/present/display/visual-cadence lane formatting,
  Flashback recording/export/playback lane formatting, lane DTOs used by
  diagnostic verdicts, shared alert-detail formatting, and health classifiers
  used by alerts and diagnostic evaluation.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.CaptureFormat.cs`
  owns HDR truth classification from capture pipeline, source-HDR, and
  verification metadata evidence, plus preview HDR input detection, HDR
  pixel-format helpers used by preview state, and tone-map state projection.
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
  timeline append, event notification, and auto-verification handoff, plus
  manual recording/file verification commands, verification-profile adaptation,
  explicit verification events, automatic post-recording verification
  scheduling, and recording-start verification reset.
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
  performance threshold projection, selected device/capture/recording settings,
  preview volume/stats visibility projection, AV-sync projection, capture
  command projection, and final status/evaluation/settings/AV-sync/capture-
  command flattening.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Media.cs`
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
  inputs consumed by `AutomationSnapshot`, final capture-command
  projection-to-`AutomationSnapshot` field flattening, source capture cadence,
  preview visual cadence, center-crop visual cadence, source signal metadata,
  source telemetry fallback/age policy, source-target summary inputs, and final
  source/cadence projection flattening.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.CaptureFormat.cs`
  owns capture-format projection routing and groups requested, HDR-request,
  actual, negotiated, reader-observation, and encoder format modules consumed
  by `AutomationSnapshot`, plus HDR activation/auto-downgrade projection,
  actual capture dimensions/frame-rate projection, requested capture
  format/quality/HDR toggle/audio toggle, negotiated capture
  dimensions/frame-rate/pixel format, source-reader subtype and observed
  pixel/surface format projection inputs, encoder format/codec/profile and
  ten-bit confirmation projection, HDR truth classification from capture
  runtime, UI state, and recording verification, HDR availability/request state,
  runtime/readiness fallback, HDR warmup/downgrade, pipeline parity, telemetry
  alignment, HDR truth verdict projection, preview HDR input detection,
  tone-map state projection, capture memory preference, requested/negotiated
  video subtype, frame-ledger projection, final capture-format flattening, and
  final capture-transport/HDR-pipeline projection-to-`AutomationSnapshot` field
  flattening.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Mjpeg.cs`
  owns CPU MJPEG totals, compressed queue, failure,
  decode/interop-copy/callback/reorder/pipeline timing, decoder count,
  per-decoder, and packet duplicate-run / unique-frame projection inputs
  consumed by `AutomationSnapshot`, plus final CPU MJPEG totals, compressed
  queue, timing, packet-hash field flattening, MJPEG preview jitter projection
  routing, queue counters, timing samples, adaptive drop/depth counters, last
  scheduler event projection, and final preview-jitter projection-to-
  `AutomationSnapshot` flattening.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flashback.cs`
  owns active Flashback export progress, failure, force-rotate fallback, final
  Flashback export last-result projection, recording failure, cleanup,
  force-rotate, temp-drive/startup-cache, active output/runtime, backend
  settings drift, export-verification, codec downgrade, encoder
  identity/bitrate/dimensions/frame-rate, focused projection routing, and final
  projection-to-`AutomationSnapshot` flattening.
  It also owns Flashback video, GPU, and audio queue/backpressure projection plus
  flattened queue/backpressure fields consumed by `AutomationSnapshot`, and
  Flashback playback state/frame summary, audio-master delay/fallback
  projection, playback event/cadence/PTS-cadence/A/V drift projection,
  seek-cap/decode timing projection, playback command queue projection, and
  final flattened playback fields consumed by `AutomationSnapshot`.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Preview.cs`
  owns preview runtime projection routing, preview frame counters, estimated
  pipeline latency, preview surface visibility, renderer attachment, GPU
  playback state/position, preview HDR/tone-map/color metadata, the frame,
  cadence, surface, startup, GPU-playback, and color groups consumed by
  `AutomationSnapshot`, preview display-cadence projection inputs, preview
  startup/readiness and renderer mode projection inputs, D3D preview swap-chain
  and renderer-state projection, D3D pipeline-latency projection, waitable frame-
  latency projection, DXGI frame-statistics projection including recent missed-
  refresh and stats failure deltas, D3D CPU upload/render/present/total-frame
  timing, submitted/rendered/dropped frame ownership, recent slow-frame
  projection, and final preview runtime/D3D flattening.
  It also owns process memory, CPU, GC, and thread-pool projection consumed by
  `AutomationSnapshot`, plus final process resource
  projection-to-`AutomationSnapshot` field flattening.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Media.cs`
  owns recording-integrity projection routing, status/reason, video-frame
  counters, queue/backpressure, audio integrity, A/V sync projection inputs,
  recording-pipeline projection routing, encoder queue age/count/failure health,
  conversion/ffmpeg/video ingest queue health, recording video queue latency,
  backpressure, encoder-output health, GPU/CUDA queue health, recording
  backend/audio-path/mux-result projection, recording UI output text,
  accumulated recording bytes, file-growth state, last finalized output
  metadata, last verification result projection consumed by `AutomationSnapshot`,
  and final recording integrity/pipeline/backend/output projection-to-
  `AutomationSnapshot` field flattening.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.Snapshots.cs` owns
  stateful snapshot bookkeeping for audio mute suspicion and recording file
  growth tracking.
- `Sussudio/Services/Automation/AutomationDiagnosticsHub.cs` owns
  performance-timeline ring reads, append mechanics, final `AutomationSnapshot`
  to `PerformanceTimelineEntry` assignment, timestamp, observed capture/preview
  FPS, encoder video queue depth/drop, capture cadence, process, memory, GC,
  thread-pool, pipeline-latency, Flashback export progress, force-rotate
  fallback, preview cadence, visual cadence, MJPEG packet/jitter, D3D preview,
  preview-pacing, Flashback playback timeline projection composition, grouped
  handoff, playback cadence, decode timing, command queue/coalescing,
  audio-master fallback, playback stage/failure, backend settings, queue reject,
  cleanup, and force-rotate timeline projection.
## Capture Runtime

Primary current owner: `Sussudio/Services/Capture/`

Important entry points:

- `CaptureSessionCoordinator.cs` owns construction, shared state fields, the
  public lifecycle/audio/Flashback command facade into the serialized worker,
  queue/session snapshot projection, work-item creation, command enqueueing,
  enqueue-failure handling, disposed-state ingress guards, worker-loop
  execution, command coalescing, operation cancellation/failure accounting,
  pending-command failure drain, and pending-command counter decrement policy,
  plus dispose/drain/cancel lifecycle for the worker queue and cancellation
  token source.
- `CaptureSessionCoordinator.cs` also owns command enums, queue receipts,
  session snapshots, queued Flashback mutations, read-only Flashback status,
  Flashback playback/buffer status projections, Flashback export and segment
  query forwarding, playback/scrub/marker/go-live command adapters, and active
  playback-controller readiness checks and rejection logging.
- `CaptureModels.cs` owns pure transition legality,
  steady-state resolution, mutable session state, transition generation, and
  state mutation methods used by normal transitions, cleanup, disposal, and
  fatal cleanup.
- `DeviceService.cs` owns capture/audio device enumeration orchestration, the
  combined discovery result used by startup refresh, discovery summary state,
  device priority/capability scoring, audio endpoint association, native XU
  interface path resolution for supported devices, inline/background Media
  Foundation format probing, persisted format-cache DTOs and load/save/delete
  helpers, and pixel-format/frame-rate normalization.
- `Sussudio/Services/Capture/NativeXu/KsExtensionUnitNative.cs` owns supported
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
- `CaptureService.PreviewLifecycle.cs` owns preview volume/mute commands,
  WASAPI audio-level and capture-failure event projection, preview WASAPI
  capture startup, video-only audio fallback logging, playback attach, preview
  rollback, preview-time microphone monitor startup, audio-preview start/stop
  lifecycle, optional capture teardown, live audio input switching, committed
  old/new capture handoff, Flashback audio attach, and deferred cancellation
  checks.
- `CapturePipelineResources.cs` owns the capture service resource holders:
  `PreviewAudioGraphResources` owns live program WASAPI capture, microphone
  capture, playback startup/shutdown, audio-monitor attach/detach order, preview
  volume/mute application, playback best-effort cleanup helpers, and
  capture-fault telemetry. CaptureService callers use this aggregate directly
  instead of private root shims for audio preview resources.
  `CaptureRecordingBackendResources` owns active recording backend resources:
  LibAv/Flashback sink identity, recording context/settings snapshot, pending
  LibAv drain task tracking/reentry policy, and explicit install/detach/clear
  operations used directly by recording start, finalization, rollback, snapshot,
  and cleanup paths without root `CaptureService` shim properties.
- `CaptureService.PreviewLifecycle.cs` owns microphone-monitor shared state,
  mic-level event projection, mic writer-detach/disposal cleanup, the public
  monitor update transaction, preview-time Flashback mic writer attachment,
  and post-recording mic monitor restart/reattachment.
- `CaptureService.cs` owns explicit cleanup transitions,
  disposal-triggered cleanup, dispose flow, app shutdown teardown, Flashback
  segment preservation when cleanup finalization fails, calls to root
  cleanup and disposed-state helpers, best-effort semaphore
  release/disposal, coordination-lock disposal, Flashback backend/export
  held-lock release helpers, Flashback eviction resume warnings, fatal
  capture/recording/Flashback backend failure callbacks, fatal capture cleanup
  launch, Flashback backend cleanup launch, GPU device-lost classification,
  recovery segment preservation, generation-stale guards, last-failure
  telemetry state fields, lock, mutation helpers, clear helpers, and snapshot
  reads. It routes cleaning-up/faulted transitions through root CaptureService
  transition helpers and must not write session state directly.
- `CaptureService.cs` owns transition serialization,
  transition-state entry, steady-state input sampling and resolution, fault
  publication, transition-lock release, cleanup/disposal state helpers,
  current-state projection, public initialization, and initialization/disposal guards.
- `CaptureService.FlashbackControls.cs` owns Flashback public state, segment
  access, enable/disable transition gating, restart entry points, committed
  restart orchestration after preview backend teardown, buffer/GPU settings
  updates, live playback-controller GPU decode propagation, recording-format
  changes, active encoding-setting application, encoder-setting cycles,
  rollback after failed Flashback buffer cycles, preview backend startup/disposal
  transition coordination, AV1 encoder support probing, video/audio readiness
  waiting, resource-owner request construction, deferred cleanup handoff,
  artifact-cleanup export-lock delegation, teardown lock ordering, purge-policy
  resolution, service callback binding, cancellation-token choice, and preview backend disposal request construction.
- `CaptureService.FlashbackRecording.cs` owns Flashback recording backend ownership checks,
  WASAPI and microphone input restoration for Flashback preview/recording
  backends, audio attachment, frame-encoded fan-out, recording topology
  validation, and Flashback session context construction.
- `FlashbackBackendResources.cs` owns startup construction, install, playback
  initialization, rollback cleanup, producer attach/detach request contracts,
  feed wiring, teardown mechanics, and backend artifact cleanup.
- `CaptureService.FlashbackControls.cs` owns buffer-cycle transition
  coordination: backend/export lock ordering, purge-preserve decisions, and
  full rebuild fallbacks. Sink-only resource mechanics live in
  `FlashbackBackendResources.cs`: playback disposal, old-sink stop/dispose,
  replacement sink startup, playback restore, and failed replacement cleanup.
- `CaptureService.FlashbackExportCore.cs` owns Flashback export entry points,
  lock-scoped backend snapshotting, session/backend lock release before native
  export, post-eviction range and last-N resolution, buffer position clamps,
  PTS offset math, shared export lifetime, export-operation locking, eviction
  pause/resume, diagnostics start/completion, exporter execution, active-file
  fallback, `FlashbackExportRequest` construction, segment metadata mapping,
  throttle-provider wiring, live-export throttle policy, segment path
  normalization, segment PTS timestamp repair, partial-fallback result marking,
  cleanup, and live-edge force-rotate export preparation including
  failure/committed-pending outcomes, timeout fallback segment discovery, and
  related diagnostics/logging. It also owns export result/rejection diagnostic
  state, progress forwarding/normalization, force-rotate fallback counters,
  locked diagnostic field copy, elapsed/progress-age/file-length helpers,
  derived progress/throughput projection used by health snapshots, and the
  export failure-kind taxonomy shared by capture diagnostics and automation
  responses.
- `CaptureService.FlashbackRecording.cs` owns Flashback recording backend
  ownership checks, audio attachment, encoded-frame forwarding, and recording
  topology validation, Flashback recording session-context construction, codec
  selection, GPU handle handoff, HDR guardrails, delivered-cadence frame-rate
  rational preservation/inference, and legacy Flashback export
  verification/downgrade snapshot fields.
- `CaptureService.HealthSnapshots.cs` samples health snapshot field groups,
  owns the private field builders, the service-state/scalar handoff, and the
  final `CaptureHealthSnapshot` DTO construction consumed by diagnostics and
  automation health checks.
- `CaptureService.HealthSnapshots.cs` owns the read-only health snapshot
  sampler, including source-cadence metric projection, MJPEG timing, preview
  jitter, visual cadence, packet hash, per-decoder projection, source
  telemetry backend/circuit projection, Flashback backend/queue projection,
  Flashback playback projection, recording health projection, and the matching
  health field records.
- Keep the health snapshot assembly handoff record and allocation-neutral
  `init`-property map in `CaptureService.HealthSnapshots.cs` unless a
  deliberate snapshot construction pattern exists; do not split it into
  post-construction mutators or shallow fragment records.
- `CaptureService.HealthSnapshots.cs` also owns Flashback buffer,
  startup-cache, backend-staleness reason policy, encoder summary, live
  Flashback audio/video queue, force-rotate, backpressure, and GPU queue field
  projection; Flashback playback state/frame/segment/PTS/seek-cap/
  submit-failure/A/V drift, cadence, decode timing, audio-master, and command
  telemetry sampling; and recording health orchestration, LibAv-only CUDA queue
  projection, active recording backend selection, LibAv-vs-Flashback sink
  fallback, failure precedence, backend-specific queue/counter normalization,
  and their private field records.
- `CaptureService.PreviewLifecycle.cs` owns the video-preview start/stop transition
  entry points and sequencing, preview pipeline and Flashback backend recycle
  decisions before start, retained-backend fast-path reattachment, reuse
  predicates and capture-settings cloning, fresh UVC startup, preview-start
  rollback, fresh preview backend startup ordering, keep-pipeline-alive detach
  semantics, stopped-state/telemetry commit, preview pipeline disposal ordering,
  Flashback backend disposal, WASAPI disposal, and microphone cleanup.
- `CapturePipelineResources.cs` also owns `CaptureVideoPipelineResources`:
  active unified-video capture storage,
  preview-frame sink storage, negotiated video getters, and cached MJPEG
  pipeline timing snapshots, plus deferred unified-video cleanup after LibAv
  drains. CaptureService callers use this aggregate directly instead of private
  root shims for the active capture and preview sink.
- `CaptureService.PreviewLifecycle.cs` owns preview frame sink attachment, shared
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
  `RecordingContextRequest` assembly, the private rollback-state holder,
  standard LibAv recording startup sequencing, video-capture reuse/creation,
  source-reader compatibility checks, preview sink/shared-device handoff, video
  pipeline installation, audio-input startup, WASAPI sink attachment, preview
  playback preservation, recording microphone capture wiring, and failed-start
  rollback cleanup.
  `CaptureService.FlashbackRecording.cs` owns Flashback recording backend
  startup, fast-path reuse, live-edge finalize/export handoff,
  finalize-in-progress choreography, Flashback recording integrity summaries,
  cancellation-result classification, post-finalize backend reconciliation,
  failed-finalize recovery preservation, deferred settings apply, buffer
  cycling, buffer-cycle failure classification, outcome publication, backend
  cleanup launch, and Flashback-specific microphone monitor restart.
  `CaptureService.RecordingLifecycle.cs`
  owns normal and emergency recording stop transition routing plus the
  stop/finalize router for active Flashback and LibAv backends.
- `CaptureService.RecordingLifecycle.cs` owns standard LibAv
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
  artifact fields for both recording-start and recording-finalize outcomes,
  plus transient backend teardown after recording-start failures, including the
  failure log/last-failure update, Flashback rollback accounting, rollback
  artifact cleanup, best-effort sink, WASAPI, unified-video, and deferred LibAv
  drain cleanup.
- `CaptureService.FlashbackRecording.cs` also owns Flashback
  recording export finalization, cancellation-result classification, and
  live-edge boundary snapshots, including idempotent
  `EndFlashbackRecordingAccounting()` calls, source-frame counters, recording
  integrity counters, and audio integrity counters.
- `CaptureService.RuntimeSnapshots.cs` samples runtime snapshot inputs consumed by UI,
  automation, and verification, owns video ingest/source-reader/WASAPI playback
  and reader/transport projections, recording-integrity summary projection,
  HDR pipeline/warmup projection, source-telemetry detail/frame-rate-origin/age/
  alignment projection, `HdrOutputPolicy`, their private handoff models, and
  final DTO construction.
- `CaptureService.RuntimeSnapshots.cs` also owns final `CaptureRuntimeSnapshot` DTO construction
  from already-sampled field groups and the private runtime snapshot assembly
  handoff contract consumed by that map.
- `CaptureService.Snapshots.cs` owns diagnostics-snapshot compatibility,
  read-only automation probes, preview-frame capture waits, shared tick-age
  snapshot helper policy, recording byte-count projection, recording-format
  labels, observed frame-format telemetry projection, A/V sync drift
  state/health fields, source telemetry backend/suppression/circuit policy
  shared by runtime and health projections, source telemetry polling, provider
  reads, fallback snapshot construction, merge policy, capture-format runtime
  telemetry, observed pixel-format normalization/reset/counters, NTSC frame-rate
  correction, and frame-rate argument formatting.
- `UnifiedVideoCapture.cs` owns public control/configuration surface, capture
  fields, counters, recording/Flashback attachment state, source-reader/D3D/MJPEG
  initialization, committed runtime state reset, read-loop start/stop,
  preview-reinit disposal, CPU MJPEG pipeline construction, stop/retention
  semantics, preview jitter buffer setup/disposal, capture/MJPEG fatal-error
  callbacks, source-reader frame arrival routing, MJPEG decoded-frame emission
  fan-out, capture-arrival ledger records, pixel-format observer dispatch,
  preview sink assignment, live-preview suppression drains, MJPEG decoded
  preview-frame routing, raw preview submission, visual-cadence reset/recording
  helpers, fatal-error dedupe/signaling, recording and Flashback sink enqueue
  helpers, recording and Flashback queue rejection accounting, legacy recording
  encoder fallback adapters, Flashback recording sequence-gap accounting,
  the `FrameLedger` ring-buffer helper, source-reader cadence forwarding, MJPEG
  pipeline/jitter/hash metrics, preview visual cadence metrics, and frame-ledger
  summary projection over the root capture fan-out state.
- `CaptureCadenceTrackers.cs` owns the two capture cadence tracker types:
  `FrameFingerprintCadenceTracker` for source-packet hash cadence ingestion,
  duplicate-run counters, fast packet hashing, duplicate-pattern metrics DTO
  construction, interval statistics, unique-interval projection, and pattern
  labels; and `VisualCadenceTracker` for visual-cadence state, reset, frame
  validation, output/change ingestion, repeat-run bookkeeping, decoded-frame
  luma sampling, crop selection, sample-buffer promotion, rolling sample writes,
  stopwatch elapsed-time conversion, metrics DTOs, snapshot construction,
  delta/output/change statistics, and motion-confidence labels.
- `ParallelMjpegDecodePipeline.cs` owns construction, callback storage, channel
  creation, compressed input admission, startup invalid-MJPG drops, byte-budget
  rejection, queue-depth accounting, queue-full rejection, packet-hash
  recording, CPU MJPEG worker thread creation/naming, decoder array ownership,
  worker decode-loop execution, worker liveness checks, startup sequencing,
  stop/dispose, worker/emitter shutdown joins, emitter signaling, fatal callback
  dispatch, remaining-time calculations, decoder disposal, queued work-item
  return, remaining reorder-frame disposal, emit-signal disposal during final
  resource cleanup, decoded-frame ordering, missing-sequence handling, decoded
  reorder state/capacity policy, emit-loop ordered draining, preview
  decoded-frame notification, and reorder/pipeline latency samples recorded
  during emission.

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

- `LibAvEncoder.cs` owns encoder fields, stable public core state, FFmpeg
  initialization forwarding, encoder open/setup orchestration, option
  validation, video codec context setup, NVENC private options, and video
  bitstream-filter setup.
- `LibAvEncoder.Audio.cs` owns audio/microphone stream state, public status
  properties, public sample entry points, payload alignment checks,
  accumulator handoff, audio sample queueing, drift-corrected encode chunks,
  planar sample copies, prepared-frame drains, A/V sync diagnostics, stream
  packet writes, pending-sample flush, accumulator ingress, and audio/microphone
  AAC stream initialization.
- `LibAvEncoder.cs` owns encoder core state plus the encoder option and
  rotation-result DTOs consumed by the rest of the encoder family.
- `LibAvEncoder.cs` owns bitstream-filter selection, NVENC preset/split-encode
  mapping, frame-size math, sample-format support, rational conversion helpers,
  encoder open/setup, open-state guards, FFmpeg error strings, structured libav
  exceptions, and D3D11 device-removed checks.
- `LibAvEncoder.VideoFrames.cs` owns D3D11 hardware frame setup,
  CUDA hardware frame context adoption, ArraySize=1 texture-pool creation,
  hardware-frame fallback cleanup, D3D11 and CUDA hardware-frame submission,
  CPU packed-frame submission, packed-frame copy, forced-keyframe handling,
  GPU device-removed checks, hardware/software frame PTS/keyframe assignment,
  per-frame HDR side-data attachment/removal, HDR mastering-display metadata
  parsing, video packet drains, bitstream-filter drains, timestamp rescaling,
  interleaved video packet writes, and hardware-frame unref cleanup.
- `LibAvEncoder.cs` also owns output rotation, IO close/reopen, stream
  reinitialization, video bitstream-filter reset, segment runtime reset, MP4
  muxer option policy for open and rotated outputs, flush/final close, dispose,
  trailer writing, close-result logging, final output telemetry, native
  frame/context/buffer release, hardware texture pool release, and encoder
  state reset.
- `Sussudio/Services/Recording/RecordingArtifactManager.cs` owns recording
  context creation, temp/final output file naming, HDR-active context fields,
  mux success/failure finalization, final-output validation, rollback,
  preserved temp-artifact discovery, and best-effort artifact deletion.
- `Sussudio/Services/Recording/Verification/RecordingVerifier.cs` owns strict verification orchestration, early
  failure results, dimensions/frame-rate/cadence/container/codec/HDR validation policy, Flashback export
  format resolution, primary mismatch parsing, HDR parity, mismatch taxonomy, ffprobe path resolution, process specs,
  accessibility checks, HDR side-data probing, cadence frame timestamp analysis, scalar/key-value/JSON parsing of
  ffprobe output, and the public verifier surface.

## Flashback

Primary current owner: `Sussudio/Services/Flashback/`

Entry points:

- `FlashbackBackendResources.cs` owns preview backend resource grouping,
  install/take/clear state, recovery-preserve flag storage and policy,
  recording-finalize handoff, producer attach/detach request shapes, video,
  audio, and microphone feed wiring, preview backend startup
  construction/install/playback initialization, startup failure rollback
  cleanup, sink-only buffer-cycle orchestration, purge/finalize decisions,
  full-rebuild fallback outcomes, playback disposal, old-sink stop/dispose,
  replacement sink startup/playback restore, failed replacement cleanup,
  preview-backend teardown, sink stop/dispose, backend clear, and artifact
  cleanup request/retry/dispose/purge mechanics. The backend resource owner
  receives export-lock wait/release delegates from `CaptureService` rather than
  owning service semaphores directly during preview backend startup, cycling,
  and teardown. `CaptureService`
  remains the transition/readiness coordinator and reads/writes the backend
  aggregate directly, without private resource shim properties.
- `FlashbackBufferManager.cs` owns buffer core state, read-only live counters,
  latest-PTS reset/update, sink-cycle active segment finalization, encoder
  frame-rate truth, initialization, segment-extension setup, disposal,
  disposed-state guards, recovery-preserve state/marker files, explicit segment
  purge, full session purge, guarded purge file deletion, disk-byte accounting
  updates, active segment path generation, active segment start, generated-path
  abandonment, completion registration, duplicate-path rejection, same-path
  segment extension, segment path lookup, range selection, start-PTS lookup,
  session-directory path safety checks, read-only segment counts, active-path
  projection, active segment start PTS calculation, segment-info projection,
  eviction-pause state, recording PTS range capture, pause-driven disk warning
  state, eviction selection, eviction file deletion, and disk-budget/window
  retention policy.
- `FlashbackStartupCacheCleanup.cs` owns startup stale-root/stale-session cleanup, temp-drive free-space probing, session-directory naming/path-safety scanner helpers, startup session-cache budget calculation, session-directory stats, oldest-session eviction, and cache-budget cleanup telemetry.
- `FlashbackDecoder.cs` owns decoder lifecycle, file open/close, dispose shell,
  stream-count/index bounds, decoded frame-size/dimension validation,
  D3D11/software decoded-frame validation, and decoded video/audio output DTOs.
- `FlashbackDecoder.Playback.cs` owns keyframe/exact seek control flow, seek timestamp conversion helpers, pending-frame transfer, seek-cap diagnostics, seek-buffer flushing, video frame receive, packet feeding, inline audio interleave during video reads, audio codec/resampler initialization, audio callback failure handling, bounded audio output, live-file EOF clearing, recoverable seek log suppression, and decode phase timing state.
- `FlashbackDecoder.VideoSetup.cs` owns video codec setup, D3D11 device-context initialization, get-format callback behavior, hardware decoder context setup, D3D11VA/software fallback selection, D3D11VA decoder selection, hardware-config diagnostics, frame-rate metadata, MJPEG single-thread decode policy, software output-buffer allocation, decoded video frame output, hardware/software frame selection, PTS-to-TimeSpan conversion, best-effort frame timestamp selection, software plane copies, and YUV-to-NV12/P010 conversion kernels.
- `FlashbackPlaybackController*.cs` owns playback, scrub, and marker control.
- `FlashbackPlaybackController.Positioning.cs` owns decoder creation, active file
  identity, file open checks, shared decoder close/open identity transitions,
  best-effort decoder file close handling, held-frame release during teardown,
  decoder close/dispose timing, cleanup telemetry, active fMP4 reopen retry,
  keyframe-reopen recovery, near-live reopen guards, adjacent-segment seek
  fallback policy, segment-start probing, segment switch telemetry, and
  adjacent-seek failure handling.
- `FlashbackPlaybackController.cs` owns construction, the `FlashbackBufferManager`
  dependency, component reference lifecycle, preview-detach/deferred reattach
  lifecycle, public playback state surface, GPU-decode toggle, live-gap
  projection, decoder HW state, playback PTS anchors, scrub resume state,
  disposal, state-transition logging, the playback-thread command enum and
  payload contract, public playback command entry points for scrub, seek,
  play/pause, go-live, and nudge, command queue writes/drop policy,
  seek/scrub coalesced command admission, queued-position resolution,
  playback-thread control-yield peek policy, public command/thread metrics,
  command status counters, pending-command accounting, active-command timing,
  queue command telemetry bookkeeping, command readiness/failure state, and
  no-op logging.
- `FlashbackPlaybackController.ThreadCommands.cs` owns playback-thread
  state, timeouts, start/recovery, stop/cancel/join diagnostics,
  command-channel lifetime, scheduling policy, exit transactions, live-restore
  cleanup, CTS disposal warnings, `PlaybackThreadEntry`, command
  dequeue/waiting, cancellation exits, continuous-playback pacing handoff,
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
  startup/seek audio prebuffering, target/timeout/frame-budget policy, decoder
  rewind after decode-ahead audio priming, audio-master clock sample state,
  stale-clock detection, read-only A/V drift projection, clock-drift
  computation, correction policy, delay-adjustment counter projection,
  fallback accounting/classification, fallback reason/drift/clock-age telemetry,
  and wall-clock sleep/spin pacing.
- `FlashbackPlaybackController.PlaybackFrames.cs` owns continuous playback frame
  decode/submit pacing, seek/scrub keyframe display, keyframe seek/reopen retry,
  file-PTS mapping for displayed seek frames, seek/scrub decoded-frame
  acquisition, adjacent-segment fallback display, preview-sink selection,
  submission telemetry, renderer calls, validation, GPU/CPU skip reasons,
  NV12/P010 byte-size policy, held-frame handoff/release, no-frame seek-display
  failure accounting, frame-skip catch-up, frame-rate resolution,
  continuous-playback snap threshold policy, pause-from-live target calculation,
  software-decode budget detection, decoder hardware-acceleration status
  refresh, over-budget snap telemetry, rolling playback cadence metric updates,
  decoded PTS cadence state/projection/tracking, mismatch telemetry,
  cadence-baseline reset, segment-edge routing, write-head waits, next-segment
  switching, active fMP4 reopen/reseek recovery, post-switch audio gates,
  decode-error snap, near-live snap, and playback failure recovery back to live
  state.
- `FlashbackPlaybackController.PlaybackFrames.cs` owns playback-frame dequeue/decode selection, prebuffer cleanup, A/V drift frame-skip catch-up policy, held playback frame backing state, release-for-live reset policy, best-effort decoded frame release warnings, continuous playback frame progression, decoded-frame submission flow, live-recovery policy invocation, cadence pacing, and A/V drift diagnostics.
- `FlashbackPlaybackController.Positioning.cs` owns the marker command API, in/out marker state, file-PTS projection, marker normalization, invalid-range clearing, recovery restore, out-point pause checks, scrub/seek clamp policy, saturating timestamp math, active fMP4 segment detection, and playback path comparison.
- `FlashbackPlaybackController.cs` owns component lifecycle, dispose,
  preview-detach deferred reattach lifecycle, playback cadence/decode metric
  DTOs, percentile projection, private metric counters, read-only projections,
  cadence/decode sample rings, playback metric reset, seek-cap telemetry, decode
  timing wrappers, max decode phase state, and dominant decode phase resolution.
- `FlashbackEncoderSink.cs` owns construction, root field state, buffer session creation, generated session ID formatting, encoder option construction, segment extension policy, transport container selection, session frame-rate rational validation, recording-to-Flashback session mapping, recording-format codec mapping, split-encode wire mapping, recording frame-rate argument parsing, encoder initialization, active-segment setup, startup queue allocation, session validation, frame-rate fallback/clamping, startup metric/counter reset, video diagnostics reset, start-failure rollback, PTS continuation, background task startup, start-transaction orchestration, public runtime counters, queue telemetry, encoder status/format projections, recording PTS boundary state, active-recording projection, begin-recording availability checks, the `IRecordingSink.StartAsync` adapter, recording begin validation, eviction-pause handoff, active-state publication, recording end rejection/failure/success results, end-PTS capture, eviction resume, PTS clamping, ready logging, saturated PTS conversion, non-negative byte/duration math, best-effort eviction resume fallback, `StopAsync`, stop drain timeout classification, final stop result reporting, `Dispose`/`DisposeAsync`, deferred cleanup, final dispose reset, cancellation/disposal helpers, and best-effort encoder/buffer manager disposal.

- `FlashbackEncoderSink.Queueing.cs` owns queue completion/signaling, shared queue-depth accounting, cancellation waits, failure notification, video/audio/GPU packet DTOs, video enqueue result classification, ArrayPool packet buffer rent/return helpers, leased video packet disposal, queued-buffer cleanup, best-effort return/release of queued video, audio, microphone, and GPU packets, raw/lease/GPU video enqueue entry points, video/GPU rejection guards, frame-size validation, texture AddRef ownership, audio/microphone enqueue entry points, force-rotate audio rejection guards, hot WASAPI writer adapters, video/GPU/audio/microphone queue admission transactions, queue-full classification, producer wakeup signaling, enqueue lifecycle guards, channel writes, depth accounting, rejection counters, backlog eviction accounting, and throttled queue diagnostics.
- `FlashbackEncoderSink.EncodingLoop.cs` owns the background encode loop, normal drain ordering, force-rotate dispatch, export force-rotate state/status projections, idle waits, request admission/publication, timeout/cancellation result classification, committed-pending grace handling, pending-request cancellation, empty completion on stop/dispose/failure, force-rotate drain abort policy, the `ForceRotateRequest` state machine, encoding-thread request capture, queue drain-to-rotate ordering, commit/rotation execution, result completion, failure logging, draining-gate cleanup, cancellation handling, fatal cleanup, final segment registration, bounded video/GPU/audio/microphone packet drains, frame-size defense, queue-depth accounting, encoder submission, GPU texture release, pooled buffer returns, encoder PTS resolution, latest-PTS and disk-byte refresh, frame-encoded event dispatch, segment-rotation triggering, active-segment registration, and rotation-failure recovery.
- `FlashbackEncoderSink.cs` owns public frame/audio/disk counters, drop counters, rotation-failure counts, frame-encoded events, queue-depth/capacity/max-depth projections, queue rejection summaries, GPU queue projections, video queue latency/backpressure metrics, encoding failure status, audio/microphone enablement, fatal-error callback registration, encoder format summaries, HDR P010 projection, and encoding completion task exposure.
- `FlashbackExporter.Lifecycle.cs` owns shared native export state, constants,
  exporter disposal, active-export cancellation, lock handling, and native
  cleanup.
- `FlashbackExporter.Execution.cs` owns export request routing/scheduling,
  single-file export shell validation, seek/setup, packet result validation,
  read-loop dispatch, drift logging, the active input packet pump, stream
  filtering, out-point clipping, timestamp rebasing, native interleaved writes,
  writer throttling, per-read packet unref, progress heartbeat, final packet
  cleanup, write state, timestamp-base discovery, early-packet buffering, EOF
  partial-base rescue, final output replacement, success result shaping,
  single-export lock release, progress/pacing policy, and per-export throttle
  provider scoping.
- `FlashbackExporter.SegmentPacketWriting.cs` owns multi-segment export validation
  dispatch, temp-output preparation, output-template selection,
  template-skip diagnostics, per-segment input open, stream-info lookup,
  stream-count checks, layout-mismatch skip tracking, final output
  replacement, and export-lock release.
- `FlashbackExporter.SegmentPacketWriting.cs` owns the multi-segment
  packet-copy/remux orchestration: output-template initialization, segment
  input sequencing, segment export range/window projection, segment offset
  updates, completion progress, requested-segment skip validation, active
  segment packet pump/write state, native frame reads, per-read packet unref,
  stream filtering, timestamp-base discovery, buffered packet
  transition/rescue/flush, rebased packet writes, writer throttling, EOF
  partial-base rescue/freeing, segment timestamp rebasing, segment-boundary
  repair, DTS monotonicity, and native packet writes.
- `FlashbackExporter.Lifecycle.cs` owns exporter disposal, active-export
  cancellation during disposal, linked cancellation-source helpers, export-lock
  wait/release/disposal policy, native input/output close, and native FFmpeg
  cleanup.
- `FlashbackExporter.Execution.cs` owns public export request routing, export
  task scheduling, linked cancellation wrapper disposal, background thread
  priority, segment snapshots, progress normalization/reporting, heartbeat
  cadence, export writer adaptive throttling, fixed sleep/yield pacing, and
  per-export throttle provider scoping.
- `FlashbackExporter.SegmentPacketWriting.cs` owns multi-segment packet writing,
  export time-span conversion, saturated time arithmetic, non-negative
  byte/count saturation, packet timestamp normalization, segment boundary
  timestamp repair, packet clone/free helpers, and buffered packet flushes.
- `FlashbackExporter.Lifecycle.cs` owns input/output FFmpeg context setup,
  stream count validation, stream-template copying, segment stream-layout
  compatibility checks, and output header writing.
- `FlashbackExporter.Execution.cs` owns temp output cleanup, stale temp
  preparation, orphaned `.mp4.tmp` cleanup, active output trailer/IO close
  finalization, temp-output validation, atomic destination replacement,
  overwrite policy, invalid final-output cleanup, completed-output length
  probing, final output validation text, normalized path comparison, output
  path validation, export-range validation, segment/export-range overlap
  classification, multi-segment export input validation, and readable-segment
  byte estimation for progress.
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
  `Sussudio/MainWindow.Composition.cs` wires the controller context,
  button/menu/double-tap and automation command adapters, key routing, pointer,
  and auto-hide adapters. Flashback command execution lives in
  `Sussudio/Controllers/Flashback/FlashbackUiControllers.cs`.
- `Sussudio/Controllers/Screenshot/ScreenshotControllers.cs` owns automation
  whole-window screenshot dispatch plus preview-frame screenshot button workflow:
  UI-thread enqueue/cancellation, failure wrapping, native PrintWindow capture,
  GDI/DIB lifetime, output directory creation, screenshot result shaping, pure
  PNG/BMP byte-stream encoding helpers, pure preview-frame screenshot output-directory fallback,
  file naming, status/log text policy, preview-frame capture, logging side
  effects, and button enable/disable state. Keep whole-window screenshot
  automation on `MainWindow.Composition.cs` with the rest of the
  `IAutomationWindowControl` adapter; `MainWindow.xaml.cs` is the
  XAML-facing adapter for preview-frame screenshots.
- `Sussudio/Controllers/Window/WindowAutomationController.cs` owns window geometry
  automation plus the recordings-folder command and shell automation host
  lifecycle: UI-thread dispatch, AppWindow and DisplayArea access, maximized
  presenter restore, side effects, pure snap-region rectangle math, automation
  token/pipe-name resolution, diagnostics hub construction, command dispatcher
  construction, named-pipe server construction, once-only startup, ready/disabled
  logging, and pipe-before-hub shutdown disposal. `MainWindow.Composition.cs`
  is the `IAutomationWindowControl` adapter and starts the host controller after
  initial device refresh; `Sussudio/MainWindow.xaml.cs` passes the host dispose
  delegate into the shutdown cleanup controller. Recording-aware close handling
  stays with the close lifecycle/finalization owners.
- `Sussudio/MainWindow.Composition.cs` owns the XAML-facing shell
  launch/chrome adapter surface: native shell bootstrap callbacks, control-bar
  animation callbacks, launch entrance/startup callbacks, settings shelf
  callbacks, static shell elevation, shell property-change routing callbacks,
  and splash phrase start/stop callbacks. Window close routing/finalization ownership is detailed in the
  window close section below:
  `Sussudio/Controllers/Window/WindowCloseLifecycleController.cs`,
  `Sussudio/MainWindow.Composition.cs`, and `Sussudio/MainWindow.xaml.cs`.
- `Sussudio/Controllers/Preview/Renderer/PreviewRendererHostController.cs` owns top-level
  preview resize telemetry throttling and reset state for preview compositor
  transforms. `Sussudio/MainWindow.Composition.cs` wires the
  renderer host context, `SizeChanged` adapter, renderer-host reset handoff,
  stable start/stop, shutdown, and reinit-unsafe-window automation adapters.
  `Sussudio/Controllers/Preview/Renderer/PreviewRendererHostController.cs` owns hosted preview
  renderer context, public runtime state, counters, start/stop/shutdown flow,
  renderer startup dimension/fps/HDR/min-present-interval planning, CPU fallback
  attachment, D3D renderer startup and event/failure handling, cleanup, D3D reinit renderer-stop/timeout policy,
  disposal, unsafe-window telemetry, stop tick accounting, and fresh
  SwapChainPanel replacement.
  `Sussudio/Controllers/Preview/PreviewTransitionAnimationController.cs` owns preview
  surface content-fit sizing, GPU panel visibility, video/control-bar
  composition shadow visuals, bounds alignment, clear behavior, compositor
  opacity fade routing, preview shell/content transitions, startup overlay,
  and reinit transition state. `MainWindow.Composition.cs`
  is the XAML-facing adapter for preview renderer and surface wiring.
- `Sussudio/MainWindow.Composition.cs` owns the stable
  automation preview snapshot adapter and context wiring alongside preview
  renderer host composition.
  `Sussudio/Controllers/Preview/Renderer/PreviewRuntimeSnapshotController.cs`
  owns the UI-dispatch sampling wrapper, UI-thread-only preview runtime field
  sampling, startup missing-signal refresh, sampled-input assembly, read-only
  preview runtime snapshot construction orchestration, and the UI-thread
  sampled preview snapshot input contract shared by the snapshot controller and
  D3D projection builder; final preview runtime snapshot DTO flattening from
  sampled input and D3D projection; surface/startup/GPU playback projection policies;
  the health input factory; preview startup elapsed timing; and
  blank/stall suspicion policy.
  `Sussudio/Controllers/Preview/Renderer/PreviewRuntimeD3DProjection.cs`
  owns the renderer projection data contract, D3D policy records, policy
  evaluation order, and assignment from evaluated policy records. It keeps the
  named policy classes for D3D-vs-CPU frame counters, renderer state, display
  cadence, render CPU timing, pipeline latency, frame ownership, DXGI frame
  statistics, and frame-latency wait defaults in one cohesive projection owner.
  Window close routing/finalization ownership is detailed in the window close
  section below:
  `Sussudio/Controllers/Window/WindowCloseLifecycleController.cs`,
  `Sussudio/MainWindow.Composition.cs`, and `Sussudio/MainWindow.xaml.cs`.
- `Sussudio/MainWindow.Composition.cs` keeps the XAML-facing title
  update hook; `Sussudio/Controllers/Shell/ShellChromeController.cs` owns window title
  base/build-stamp formatting and the recording-time suffix used by property
  changes, plus bottom status-strip projection: status text, recording time, disk warning,
  disk-space text, recording size, recording bitrate, the status-strip
  `PropertyChanged` router, the recording-only title-refresh callback, and the
  Flashback bitrate fallback used while Flashback is enabled and recording is
  idle. `Sussudio/MainWindow.Composition.cs` is the XAML-facing
  adapter and builds the ViewModel snapshot passed into the controller.
- `Sussudio/Controllers/Window/WindowCloseLifecycleController.cs` owns window close
  request flags, completion TCS, cleanup latch, recording-stop handoff flags,
  close-in-progress exception classification, automation close dispatch
  orchestration, actual close request execution, recording finalization side
  effects, and post-`Closed` cleanup order: `Close()`, completion timing after
  non-recording closes, close-in-progress success handling, COM
  `Application.Current.Exit()` fallback, requested-state reset after unexpected
  failures, `AppWindow.Closing` decision choreography, the 120-second stop
  budget, `StopRecordingAndWaitAsync` wait race, timeout/failure breadcrumbs,
  status text, shutdown-content dim/restore policy, timer stops, event detaches,
  preview shutdown, post-close recording finalization handoff, automation
  disposal, NVML disposal, and ViewModel disposal.
- `Sussudio/MainWindow.Composition.cs` owns the XAML/AppWindow close adapter:
  `RegisterCloseLifecycle`, `CloseAsync`, and the stable
  `RequestWindowClose()` adapter.
- `Sussudio/MainWindow.xaml.cs` wires MainWindow cleanup
  delegates and the stable `Closed` event adapter into
  `WindowShutdownCleanupController`, and owns the timer,
  event-detach, stats, recording-visual, and preview-size cleanup delegate
  adapters.
- `Sussudio/Controllers/Window/WindowCloseLifecycleController.cs` also owns native window
  bootstrap: `AppWindow` lookup, ViewModel window handle handoff,
  minimum-size subclassing, DWM cloak/dark-mode setup, first-composed-frame
  shell reveal scheduling/cancellation, initial shell size, icon, and native
  helpers used by shell startup and automation controllers.
  `Sussudio/MainWindow.Composition.cs` is the XAML-facing shell
  launch/chrome native-window adapter and keeps the `_hwnd` field consumed by screenshot and window
  automation paths.
- `Sussudio/Controllers/UiDispatchControllers.cs` owns MainWindow
  UI-thread direct execution, dispatcher enqueue/cancellation/error wrapping,
  preview-snapshot-style result dispatch with three-attempt enqueue retry, and
  guarded async event-handler status updates used by automation adapters and
  XAML event handlers. `Sussudio/MainWindow.Composition.cs` keeps the stable
  private MainWindow adapter names for callers.
- `Sussudio/MainWindow.xaml.cs` owns the root `SetupBindings()`
  startup binding sequence and leaves feature-specific binding clusters in
  focused partials or controllers, including initial recording lockout,
  device-selection change hooks, stats visibility sync, and status-strip
  projection.
- `Sussudio/MainWindow.Composition.cs`
  owns the preview-transition XAML command and callback adapter surface.
  `PreviewButtonActionController` owns the preview
  fade/reinit/start/stop command behavior. One-line XAML command bridges for
  capture-device, recording, output-path, and preview-screenshot buttons live in
  their feature adapter partials beside the owning controllers.
- `Sussudio/MainWindow.xaml.cs` owns the root ViewModel
  PropertyChanged event envelope, property-name normalization, and route order.
  Capture-selection and
  status-strip adapters are still considered first through the
  `Sussudio/MainWindow.xaml.cs` adapter and
  `MainWindow.Composition.cs`; broad domain property-name switches
  and status-strip routing logic live in focused controllers/partials.
- `Sussudio/Controllers/Preview/PreviewTransitionAnimationController.cs` owns shared
  compositor opacity fade helpers for preview shadow visuals without adding
  dispatcher hops.
- `Sussudio/Controllers/Audio/AudioControlBindingController.cs` owns the audio-control
  binding context, initial audio/microphone projection, preview-volume binding and priming,
  audio/microphone/device-audio selection handlers,
  record/preview/custom-audio/microphone toggle handlers, audio-meter activation,
  initial meter presentation, device-audio gain/meter resize hooks, and
  audio/microphone property-change projections for audio toggles, monitoring
  meter state, preview-volume slider sync, microphone enablement, microphone
  volume sync, audio/microphone meter setup, the XAML/view-model meter
  dependency bag, runtime meter fields, smoothing, peak/range markers,
  microphone meter clipping, reset behavior, timer lifetime, `TranslateMarker`,
  monitoring/disabled animations, and rounded content clips.
  `Sussudio/MainWindow.xaml.cs` is its XAML-facing adapter.
- `Sussudio/Controllers/Stats/StatsOverlayCompositionController.cs` owns stats
  dock visibility orchestration, stats/frame-time toggle event hookup and
  checked/unchecked handling, stats toggle-to-view model sync, frame-time
  overlay visibility, polling lifetime, stats dock show/hide storyboard
  construction, dock visibility mutations, animation completion state, the
  stats overlay runtime facade, construction-order entry point, and graph
  factory wiring: snapshot provider, frame-time presentation, dock graph,
  overlay controller, and section chrome controller.
  The grouped stats composition context contracts for shell controls, snapshot
  sources, dock targets, hardware sources, and frame-time targets live with
  `Sussudio/Controllers/Stats/StatsOverlayCompositionController.cs`.
  `Sussudio/MainWindow.Composition.cs` owns the XAML-facing stats
  overlay adapter surface: composition controller instantiation, shell-control
  wiring, snapshot sources, dock targets, MJPEG/NVML sources, compact frame-time
  targets, lifecycle/polling commands, and section chrome event adapters.
  `Sussudio/Controllers/Stats/StatsOverlayCompositionController.cs` owns stats dock
  presentation, diagnostic row, hardware row, and refresh-controller graph
  construction plus the dock graph context contract because the dock is only
  driven by the overlay controller.
  `Sussudio/Controllers/Stats/StatsDockRefreshController.cs` owns stats dock refresh
  orchestration: snapshot acquisition, dock presentation build/apply,
  diagnostics visibility gating, decode/GPU row refresh ordering, stats dock
  metric text, visibility, and status brush application after the presentation
  model is built. `Sussudio/Controllers/Stats/StatsOverlayCompositionController.cs`
  keeps the local section chrome controller that owns stats dock section
  expand/collapse chrome and automation-visible section visibility application;
  `Sussudio/MainWindow.Composition.cs`
  owns the XAML/automation adapter for that stats shell wiring.
  `Sussudio/StatsWindow.xaml.cs` also owns detached stats-window metric text and
  dynamic telemetry detail rendering.
  `Sussudio/Controllers/Stats/StatsOverlayCompositionController.cs` owns shell stats snapshot
  orchestration from capture-health, renderer metrics, and view state, including
  renderer cadence/recent-sample acquisition and null fallback policy.
  `Sussudio/MainWindow.Composition.cs` is the XAML-facing
  surface for stats visibility, polling, snapshot source wiring, frame-time
  targets, and section chrome commands; stats provider/controller context
  contracts and provider/controller composition live in
  `Sussudio/Controllers/Stats/StatsOverlayCompositionController.cs`.
- `tests/Sussudio.Tests/MainWindow.ControllerOwnership.Tests.cs` owns contract
  checks for stats overlay lifecycle wiring and stats section chrome through
  the MainWindow controller ownership surface; xUnit wrappers for those facts
  live in `tests/Sussudio.Tests/XUnit.PresentationPreviewContractsTests.cs`.
- `tests/Sussudio.Tests/XUnit.StatsPresentation.Formatting.Tests.cs` owns xUnit
  contract checks for stats presentation formatting plus stats dock refresh
  orchestration, diagnostic row update delegation, hardware row projection,
  source-shape ownership, HDMI source telemetry panels, and row chrome pooling.
- `Sussudio/Controllers/Stats/StatsDockRefreshController.cs` owns stats dock row
  chrome alongside refresh orchestration: shared row creation, label/value text
  mutation, visibility toggles, dock row style application, dynamic decode/GPU
  simple row pools, diagnostic row presentation, empty-state rows, group
  headers, diagnostic row pooling, hardware row refresh, availability, and
  decode/GPU minimum pool sizing. It also keeps
  the hardware input provider that owns live MJPEG/NVML input acquisition,
  decode availability policy, and pure telemetry projection into the hardware-row
  presentation input DTOs;
  `Sussudio/ViewModels/StatsPresentationBuilder.cs` owns pure decode/GPU row
  text projection over presentation inputs, and
  `StatsDockRowChromePresenter` owns shared row chrome plus decode/GPU row
  pooling inside the refresh owner that decides when decode/GPU rows refresh.
- `Sussudio/Controllers/Stats/StatsOverlayCompositionController.cs` owns compact
  frame-time overlay text application, graph-line mutation, canvas sizing,
  sample projection, and expected-line geometry alongside the stats overlay
  composition graph that routes polling snapshots into it.
  `Sussudio/MainWindow.Composition.cs` owns the XAML-facing
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
  The internal presentation DTO records/enums live in the same file so stats
  text/model contracts can be reviewed with the builder.
  `Sussudio/ViewModels/StatsSnapshot.cs` owns the UI stats snapshot DTO plus
  capture-health, renderer, and shell view-state projection into that DTO after
  acquisition.
- `Sussudio/ViewModels/ViewModelSelectionPolicies.cs` owns pure resolution and
  video-format option construction, HDR mode enablement, and source aspect-ratio
  filtering. Shell files bind and display those options.
- `tests/Sussudio.Tests/XUnit.StatsPresentation.Formatting.Tests.cs` owns
  detached-window, dock encoder, display-repeat visual-cadence, compact preview
  summary, frame-time range, frame-time graph geometry behavior checks, stats
  dock source-shape assertions, builder/controller/DTO ownership assertions,
  HDMI source telemetry panel projection checks, hardware row presentation and
  input-provider behavior checks, and shared StatsPresentation/StatsHardwareRows
  xUnit reflection/file helpers.
- `tests/Sussudio.Tests/MainWindowUiContract.Tests.cs` owns MainWindow
  automation ID inventory, full-screen/window automation, UI-dispatching source
  contract checks, and xUnit stats snapshot builder contract checks.
- `tests/Sussudio.Tests/MainWindow.ShellOwnership.Startup.Launch.Tests.cs`
  owns MainWindow startup/launch ownership assertions for launch entrance
  animation, first-load hosting, splash loading phrases, and splash pacing
  policy.
- `tests/Sussudio.Tests/MainWindow.ShellOwnership.PreviewRuntime.Tests.cs`
  owns MainWindow preview resize telemetry, preview renderer startup-plan
  fallback policy, preview surface/shadow, renderer host lifecycle, D3D
  startup/reinit, stats adapter, preview runtime snapshot mapping, and D3D
  projection ownership assertions.
- `tests/Sussudio.Tests/PreviewRuntimeSnapshotController.Tests.cs` owns preview
  runtime snapshot controller Build integration, D3D policy null-renderer
  defaults, health policy/input factory, and surface/startup/GPU playback
  projection policy regression checks.
- `tests/Sussudio.Tests/MainWindow.ShellOwnership.WindowLifecycle.Tests.cs`
  owns MainWindow native bootstrap, adapter, first-frame reveal, window-lifecycle
  composition, close lifecycle state, close request/app-closing controllers,
  recording-stop close protection, recording-finalization stop-wait policy,
  post-close shutdown cleanup, automation-host disposal, and ownership
  documentation assertions.
- `tests/Sussudio.Tests/MainWindow.ControllerOwnership.Tests.cs` owns
  MainWindow property-change routing ownership assertions across focused
  controller adapters, visual shell/preview controller-adapter ownership for
  control bar, shell elevation, shell chrome settings shelf/title/live signal
  info/status-strip presentation, preview-transition, preview startup overlay,
  and preview fade-in controllers, plus window-title formatting,
  recording-button chrome, responsive shell layout adapter/controller ownership,
  responsive breakpoint/placement and snap-region rectangle policy checks,
  output path display/action ownership, output picker and output drive
  free-space presentation bridge checks, preview screenshot workflow/text-policy,
  whole-window screenshot ownership checks, recording-state presentation, and
  recording-state presentation policy assertions, plus recording action,
  preview audio fade, preview button presentation, audio control presentation,
  and microphone control ownership assertions.
- `tests/Sussudio.Tests/MainWindow.ControllerOwnership.Capture.SelectionBindings.Tests.cs`
  owns capture selection binding XAML-adapter, controller shell,
  `PropertyChanged` routing, collection sync, queued sync, available-option
  rebinding, capture device/audio input/capture mode/recording selection,
  selection-normalizer placement and fallback-policy behavior, and device-audio
  projection ownership assertions.
- `tests/Sussudio.Tests/MainWindow.ControllerOwnership.Capture.OptionPresentation.Tests.cs`
  owns capture/recording option binding controller-adapter ownership,
  capture option presentation controller-adapter, refresh/apply button
  controller-adapter, affordance-policy, and HDR/FPS tooltip text-policy
  assertions.
- `tests/Sussudio.Tests/MainWindow.FlashbackOwnership.Tests.cs` owns Flashback
  status/playback polling, timeline track layout, marker/export presentation,
  playhead/CTI motion, playback presentation/coordinator, settings binding, and
  command controller ownership assertions.
- `tests/Sussudio.Tests/HarnessCore.cs` owns the shared
  MainWindow source readers used by root, Flashback, preview, shell-chrome,
  capture-binding, and stats-overlay ownership assertions.
- `tests/Sussudio.Tests/MainViewModel.Automation.AsyncSurface.Tests.cs` owns
  the `IAutomationViewModel` async surface contract plus Flashback/probe
  dispatcher routing, UI-dispatch cancellation disposal assertions,
  automation audio/microphone command entry points, microphone monitor
  suppression, recording transition routing through the shared transition gate,
  recording runtime ownership assertions, recording-setting automation routing
  for Flashback encoder cycles, emergency recording-stop dispatcher/coordinator
  routing assertions, bounded bitrate sample-window behavior assertions,
  preview-volume persistence, automation options surface, and capture
  audio-monitoring coordinator surface and runtime-guard assertions.
- `tests/Sussudio.Tests/MainViewModel.Automation.UiSettings.Tests.cs` owns
  automation UI-setting persistence, frame-time/stat visibility contracts,
  capture-mode reinitialization, device refresh, device/audio-input selection
  routing contracts, HDR/true-HDR preview enablement guards, and HDR mode
  change side-effect ownership assertions.
- `tests/Sussudio.Tests/MainViewModel.Automation.DiagnosticsRefresh.Tests.cs`
  owns the serialized diagnostics refresh ownership check, core ownership
  orchestration, runtime/HDR verification checks, refresh pipeline/gate,
  evaluation-policy, diagnostic evaluation, realtime and Flashback diagnostic
  lane ownership assertions, snapshot/dispatcher assertions, preview-runtime
  projection assertions, diagnostics alert/event ownership assertions,
  source-reader partial ownership assertions used by diagnostics refresh,
  diagnostics-refresh snapshot projection integration wiring,
  diagnostic-session preview metric assertions, and diagnostic-session
  core/export/recording ownership assertions, initial snapshot construction,
  BuildAutomationSnapshot composition, and snapshot flattening ownership
  assertions.
- `tests/Sussudio.Tests/MainViewModel.Automation.DiagnosticsRefresh.Flashback.Tests.cs`
  owns diagnostics-refresh Flashback alert coverage, capture-service and
  dispatcher Flashback export operation ownership assertions, and
  diagnostic-session Flashback playback metrics/result, scenario,
  health-policy, stress, and warning-tolerance assertions.
- `tests/Sussudio.Tests/MainViewModel.Automation.DiagnosticsRefresh.Tests.cs`
  owns diagnostics refresh source/fixture readers for the diagnostics hub,
  capture service, source reader, and tool-surface source text used by refresh
  ownership assertions, including diagnostic-evaluation, alerts,
  snapshot-projection, aggregate `SourceFamilyText` composition, and the
  corresponding diagnostics-refresh ownership assertions.
- `tests/Sussudio.Tests/McpToolSurface.DiagnosticSession.Ownership.Tests.cs`
  owns shared diagnostic-session source-family readers used by refresh, MCP,
  and tool ownership assertions alongside the broad diagnostic-session
  ownership checks.
- `tests/Sussudio.Tests/MainViewModel.Automation.AsyncSurface.Tests.cs`
  owns automation async surface and automation snapshot/options source-shape
  assertions, including the diagnostics-loop contract that keeps options
  snapshots out of hot diagnostics refresh paths.
- `tests/Sussudio.Tests/XUnit.AutomationContractsTests.cs`
  owns xUnit execution for the former legacy diagnostics-loop polling catalog
  check.
- `tests/Sussudio.Tests/MainViewModel.Automation.DiagnosticsProjection.Tests.cs`
  owns focused automation diagnostics projection ownership assertions for
  snapshot root, audio/ingest, capture commands/settings, capture
  format/transport, source/cadence, MJPEG, recording, preview, and Flashback
  projection families; keep this surface separate from the old mega diagnostics
  refresh assertion.
- `tests/Sussudio.Tests/MainViewModel.AudioControls.GainAndMonitoring.Tests.cs`
  owns analog gain curve mapping, audio monitoring visual state, preview audio
  monitoring volume-ramp, audio meter callback-state, audio-ramp trace
  telemetry, audio device selection policy, device audio refresh, saved-state
  guard, device-audio request-controller ownership assertions, and native XU
  audio-control service cohesion, profile, payload workflow, and raw transport
  ownership assertions.
- `tests/Sussudio.Tests/XUnit.PresentationPreviewContractsTests.cs`
  owns the xUnit execution surface for MainViewModel source and behavior
  contracts after their removal from the legacy offline harness catalog.
- `tests/Sussudio.Tests/MainViewModel.DependencyComposition.Tests.cs` owns the
  MainViewModel dependency-composition seam assertions for root construction,
  controller graph creation, UI-dispatch, recording-transition, preview
  lifecycle/reinitialize/state/automation, capture/device, source-telemetry,
  runtime lifecycle/event ingress/subscription/disposal dependency contexts,
  state partial ownership, and default dependency factory wiring.
- `tests/Sussudio.Tests/HarnessCore.cs` owns shared source-inspection helpers,
  including MainViewModel source readers, member extraction, comment/string
  stripping, regex assertions, and token-order assertions used by capture,
  Flashback, automation, MCP, recording, stats, and docs tests.
- `tests/Sussudio.Tests/MainViewModel.Capture.SelectionPolicy.FrameRates.PolicyBehavior.Tests.cs`
  owns frame-rate source-filter, automatic-selection, always-on capture-option,
  timing-policy ownership assertions, automatic frame-rate choice, and pure
  timing-policy behavior assertions.
- `tests/Sussudio.Tests/XUnit.PresentationPreviewContractsTests.cs`
  owns xUnit execution for the former legacy presentation-preview frame-rate
  selection/timing catalog group.
- `tests/Sussudio.Tests/MainViewModel.Capture.SelectionPolicy.Tests.cs`
  owns selected capture-format and mode-tuple video-format filtering policy
  assertions plus compact selection-policy ownership assertions for
  mode-selection reset, resolved automatic frame-rate application, and
  recording format selection policy ownership. It also owns capture settings
  projection ownership assertions, including the focused frame-rate request
  projector used by `BuildCaptureSettings`.
- `tests/Sussudio.Tests/MainViewModel.Capture.SelectionPolicy.Resolution.Behavior.Tests.cs`
  owns resolution-selection source-shape assertions for option rebuild,
  auto-selection state, pure policy placement, and policy behavior assertions
  including HDR and SDR source retarget behavior.
- `tests/Sussudio.Tests/XUnit.PresentationPreviewContractsTests.cs`
  owns xUnit execution for the former legacy presentation-preview MainWindow
  and adjacent selection/runtime guard catalog groups after their removal from
  the legacy offline harness catalog.
- `tests/Sussudio.Tests/XUnit.PresentationPreviewContractsTests.cs`
  owns xUnit execution for the former legacy presentation-preview late
  device-format probe retarget catalog group.
- `tests/Sussudio.Tests/XUnit.PresentationPreviewContractsTests.cs`
  owns xUnit execution for the former legacy presentation-preview
  mode-selection, capture-format, and recording-settings selection catalog group.
- `tests/Sussudio.Tests/MainViewModel.Capture.SelectionPolicy.Tests.cs` owns
  capture format, recording settings, capture settings projection, and late
  device-format probe retarget selection-policy ownership/behavior checks plus
  the shared reflection, option-list, and capture-mode model construction
  helpers for the selection-policy test family.
- `tests/Sussudio.Tests/MainViewModel.Capture.PreviewStartup.Tests.cs` owns
  preview startup signal, watchdog, lifecycle-event, fade-in, preview-stop
  audio-ramp, device-discovery-before-recording-capability, UI/audio preview
  reveal ordering, timeout, failure-stop, formatter assertions, preview startup
  session/reinit adapter source-shape ownership, MainViewModel preview
  lifecycle/reinitialize controller placement, preview startup session
  controller attempt-state and orchestration behavior, preview reinit transition
  controller presentation and animation-state behavior, plus pending Flashback
  encoder settings cycle waits during preview reinitialization.
- `tests/Sussudio.Tests/XUnit.PresentationPreviewContractsTests.cs` owns
  xUnit execution for the former legacy presentation-preview preview-startup
  source-shape ownership, controller behavior, signal/failure text, and
  startup ordering catalog groups.
- `tests/Sussudio.Tests/XUnit.PresentationPreviewContractsTests.cs` also
  owns xUnit execution for the former legacy presentation-preview capture
  preview-lifecycle/audio-fallback catalog group.
- Preview-startup ordering xUnit execution also lives in
  `tests/Sussudio.Tests/XUnit.PresentationPreviewContractsTests.cs`.
- `tests/Sussudio.Tests/HarnessCore.cs` owns the shared
  source readers for the consolidated `MainWindow.Composition.cs`
  adapter family, shell-chrome, stats-overlay, capture-binding,
  Flashback, and preview-renderer adapters.
- Fullscreen tests use the shared shell-chrome helper for
  `MainWindow.Composition.cs`; shutdown cleanup tests use the shared
  MainWindow root helper for `MainWindow.xaml.cs`.
- `tests/Sussudio.Tests/HarnessCore.cs` also owns the source
  reader for property-changed preview assertions over
  `MainWindow.Composition.cs`.
- `tests/Sussudio.Tests/MainViewModel.Capture.FlashbackRouting.ViewModel.Tests.cs`
  owns MainViewModel Flashback coordinator-routing assertions, negative
  `_captureService` access guards, Flashback settings owner checks for
  automation enable/restart entry points, Flashback export backend-lease and
  export-operation lock assertions, ViewModel export routing, and export CTS
  lifecycle assertions.
- `tests/Sussudio.Tests/MainViewModel.Capture.FlashbackRouting.Interactions.Tests.cs`
  owns Flashback scrub, release/cancel/capture-lost, fullscreen Flashback
  bridge, timeline toggle rollback, and lockout assertions: shortcut gating,
  timeline visibility, and scrub-end handoff.
- `tests/Sussudio.Tests/MainViewModel.Capture.FlashbackBackend.PreviewPipeline.Tests.cs`
  owns retained Flashback preview backend, audio restoration, preview stop
  rollback assertions, device-switch teardown ordering between video stop,
  Flashback backend disposal, preview reinit disposal, Flashback lifecycle
  outcome log-token, codec no-downgrade, export force-rotate, buffer-cycle,
  delivered-cadence rational, and enable/disable preview-state assertions.
- `tests/Sussudio.Tests/XUnit.CoreRuntimeContractsTests.cs` owns the core
  runtime xUnit execution surface, including the `SmallContractsTests` wrapper
  for ported audio input, audio level event, capture device metadata/default
  collection, and automation window action enum contract checks.
- `tests/Sussudio.Tests/XUnit.CaptureConfigurationModelsTests.cs` owns
  MediaFormat equality/hash-code checks alongside the broader capture
  configuration model contract surface.
- `tests/Sussudio.Tests/XUnit.SnapshotModelsTests.cs` owns the xUnit
  snapshot-model contract suite. It keeps shared snapshot-model spec DTOs,
  registration state, reflection JSON round-trip, registered-property coverage,
  property-list, nullability, and helper assertion methods beside the facts
  that use them. It also owns AutomationSnapshot and AutomationOptions DTO
  shape checks for CPU MJPEG, MJPEG preview, preview diagnostics,
  capture-command/cadence, recording, Flashback recording/playback/export,
  visual cadence, and advanced control-state options; CaptureDiagnosticsSnapshot
  property spec/default/round-trip/reflection JSON/source-ownership checks;
  CaptureHealthSnapshot and SourceTelemetryDetailEntry DTO contracts; and
  SourceSignalTelemetrySnapshot plus source telemetry automation projection
  contract checks.
- `tests/Sussudio.Tests/NativeXuAtCommandProvider.Tests.cs` owns Native XU
  telemetry provider ownership, root active-read/rolling-poll locality, shared snapshot
  assembly ownership, cohesive KS bridge source/probe-link ownership, and
  supported 4K X product-revision checks.
- `tests/Sussudio.Tests/ServiceNamespace.SourceOwnership.ServicesLayer.Tests.cs`
  owns DeviceService scoring, cohesive MF device enumerator ownership,
  source-reader negotiation/interop ownership, and MF symbolic-link matching
  assertions with the broader service-layer source-ownership checks.
- `tests/Sussudio.Tests/XUnit.CoreRuntimeContractsTests.cs` owns the core
  runtime xUnit execution surface plus the ported HdrOutputPolicy, HDR output
  environment-switch, disabled source-telemetry-provider behavior checks, and
  small no-hardware model/protocol contract wrappers.
- `tests/Sussudio.Tests/RecordingQueue.OverloadPolicy.Tests.cs` owns the
  shared recording queue source readers, source-block extraction helpers,
  recording/Flashback queue overload, fatal-failure, lifecycle, recording
  backend start-policy, source-loading, buffer-cycle, LibAv/Flashback overload,
  buffer recovery, recording backend finalization, Flashback cleanup,
  microphone restart, post-finalize telemetry, and health/automation telemetry
  assertions.
  `CaptureService.RecordingLifecycleOwnership.Tests.cs` owns CaptureService
  recording lifecycle, recording-stop finalization failure propagation, active
  recording backend resource aggregate, recording start rollback, and recording
  outcome-state file-ownership assertions.
- `tests/Sussudio.Tests/RecordingQueue.LibAvSink.Lifecycle.Tests.cs` owns
  LibAv recording sink try-enqueue, video-queue submission, audio queue,
  queue-cleanup, output validation, video-session setup, drain-loop,
  encoding-loop, startup sequencing, stop-lifecycle, and lifetime-helper
  ownership assertions.
- `tests/Sussudio.Tests/RecordingQueue.CaptureFanout.Tests.cs` owns
  WASAPI capture-loop, hot-write, conversion, root diagnostics, COM contract,
  bounded stop assertions, UnifiedVideoCapture frame-ingress, CPU-MJPEG format
  reporting, stop-failure MJPEG pipeline retention, sink fan-out, CaptureService
  Flashback backend aggregate ownership assertions, the shared source family
  helper for Flashback backend orchestration/recording finalization partials,
  focused Flashback orchestration partial ownership contracts, LibAv
  live-preview restoration, and recording outcome-state ownership.
  `tests/Sussudio.Tests/XUnit.RecordingContractsTests.cs` owns the
  xUnit execution surface for these recording queue, LibAv sink, WASAPI,
  capture fan-out, and CaptureService recording ownership contracts after their
  removal from the legacy harness catalog.
- `tests/Sussudio.Tests/MjpegPipeline.Tests.cs` owns CPU MJPEG pipeline
  source-shape, focused-partial ownership, startup-drop, known-loss,
  packet-hash duplicate cadence, visual-cadence crop sampling, shared-reorder
  behavior checks, and the xUnit execution surface for CPU MJPEG runtime,
  timing metric math, stopwatch timeout helper, software decoder shape,
  cadence, pooled-frame, preview-jitter, and queued lease-release contracts
  after their removal from the legacy harness catalog.
- `tests/Sussudio.Tests/CaptureService.RuntimeSnapshots.ProjectionOwnership.Tests.cs`
  owns runtime projection ownership and behavior scenarios for ingest/audio,
  reader transport, observed formats, HDR pipeline/parity, source telemetry/
  alignment, inactive thread probes, frame-ledger recent-event contracts, and
  recording integrity.
- `tests/Sussudio.Tests/RecordingIntegrity.Tests.cs` owns recording integrity
  summary defaults, automation snapshot field contracts, automation projection
  ownership checks, summary policy, Flashback recording scoped sequence gaps,
  CaptureService focused-partial ownership, and shared formatter rendering
  checks.
- `tests/Sussudio.Tests/CaptureService.Snapshots.Tests.cs` owns CaptureService
  diagnostics-snapshot compatibility, recording format/profile helper, HDR
  warmup-state, recording-stats ownership, observed pixel telemetry, source
  telemetry backend/circuit, tick-age, and telemetry-alignment helper
  assertions.
- `tests/Sussudio.Tests/CaptureService.PreviewLifecycle.Tests.cs` owns
  video-only preview fallback, missing audio endpoint, preview-stop API surface,
  preview backend log contracts, CaptureService audio source-family helpers,
  audio focused-partial ownership, PreviewAudioGraphResources ownership, and
  post-recording microphone monitor restart assertions.
- `tests/Sussudio.Tests/CaptureService.SessionStateOwnership.Tests.cs` owns
  CaptureService initialization, session-state-machine and transition-policy
  ownership, asserts that lifecycle partials route state changes through
  transition/state-machine helpers, owns last-failure telemetry and Flashback
  backend failure cleanup source placement, and keeps the
  no-direct-session-state-write/faulted-session guards for failure cleanup.
- `tests/Sussudio.Tests/CaptureService.HealthSnapshots.AssemblyAndSamplerOwnership.Tests.cs`
  owns CaptureService health snapshot assembly, capture-cadence, MJPEG,
  AV-sync, Flashback export/buffer/queue/playback, recording, and
  source-telemetry ownership assertions plus structured source telemetry,
  cached MJPEG timing propagation for health and diagnostics snapshots, the
  synthetic MJPEG timing metric factories used by those scenarios, and shared
  health snapshot assertion helpers.
- `tests/Sussudio.Tests/RecordingVerifier.Integration.Tests.cs` owns the
  recording verifier integration seam: fake process-supervisor,
  runtime-snapshot, verifier-construction, verification-invocation helpers,
  early failure paths, verifier contract/source-shape assertions, result DTO
  property coverage, dedicated LibAv verification script contract, ffprobe
  failure/priority scenarios, HEVC/H264 codec success and mismatch, Flashback
  verification format, resolution/frame-rate mismatch, HDR validation, and
  NTSC frame-rate tolerance scenarios.
- `tests/Sussudio.Tests/D3D11PreviewRenderer.DiagnosticsContract.Tests.cs`
  owns renderer diagnostics source-shape, frame queue, frame ownership, present
  cadence metric shape/suppression baseline, public renderer diagnostics API
  contract assertions, preview runtime, automation snapshot, nested renderer
  metrics, preview tracking, slow-frame diagnostic reflection contracts, and
  `AutomationRuntimeModels.cs` preview, Flashback playback, Flashback export,
  and process diagnostics reflection contracts.
- `tests/Sussudio.Tests/D3D11PreviewRenderer.SourceOwnership.RenderPipeline.Tests.cs` owns
  configuration, native interop, frame type/ownership state, DXGI frame-stat
  state, slow-frame state, metric-window lifecycle, metric-tracking method/state,
  panel binding, shared-device, device initialization, input-resource,
  input-view, raw-upload, frame-latency, viewport, letterbox, render-pass,
  shader-rendering, and shader-source assertions for the renderer core and
  render-pipeline source-ownership surface.
  `D3D11PreviewRenderer.SourceOwnership.RuntimeCapture.Tests.cs` owns public
  submission state, lifecycle/stop-lifecycle state, pending-frame state and
  draining, render-thread loop, device-lost classification/recovery,
  first-frame notification, failure telemetry method/state, Present shared
  present-accounting source-ownership, screenshot and frame-capture
  cancellation, shared D3D device reference lifecycle, black-edge counting,
  preview PNG encoder CRC, and preview PNG capture assertions.
- `tests/Sussudio.Tests/AutomationToolContracts.Tests.cs` and
  `tests/Sussudio.Tests/XUnit.ToolContractsTests.cs` own NVML snapshot
  computed-property/unit-conversion checks and `NvmlMonitor` native interop
  ownership assertions alongside the tool-model contract group.
- `tests/Sussudio.Tests/XUnit.CoreRuntimeContractsTests.cs` owns RuntimePaths,
  RuntimePaths resolution-policy source ownership, FFmpeg runtime location,
  bounded external process supervision, MMCSS registration, ProcessSpec, and
  ProcessRunResult contract checks alongside the broader no-hardware core
  runtime xUnit surface.
  `FfmpegRuntimeLocator.cs` owns app-local/PATH runtime and tool resolution plus
  cached FFmpeg encoder/split-encode capability probes through bounded
  `ProcessSupervisor` calls, one-time native initialization, FFmpeg log callback
  routing, and recoverable seek-log suppression.
- `tests/Sussudio.Tests/XUnit.AutomationContractsTests.cs` owns app-surface
  legacy `Program` helpers plus project-file build/publish policy contract
  helpers and xUnit execution for those checks after their removal from the
  legacy offline harness catalog.
- `tests/Sussudio.Tests/XUnit.RecordingContractsTests.cs` owns recording
  service contract DTO checks such as GpuPipelineHandles,
  RecordingContextRequest, FinalizeResult, and RecordingStats, plus the
  xUnit execution surface for recording pipeline, recording-model/Flashback
  buffer, recording verifier, LibAv encoder, Flashback integrity, shared
  formatter, and dedicated LibAv verification script contracts after their
  removal from the legacy offline harness catalog. Keep the public wrapper
  classes in this file unless a group needs independent fixture state.
- `tests/Sussudio.Tests/XUnit.RecordingContractsTests.cs` owns recording
  service contract DTO checks plus xUnit temp artifact finalize/rollback
  behavior for recording output cleanup.
- `tests/Sussudio.Tests/LibAvEncoder.Contracts.Tests.cs` owns LibAvEncoder
  codec policy, diagnostics/frame-size helpers, HDR metadata, ValidateOptions
  reflection coverage, source-ownership and output lifecycle layout
  assertions, and shared source-reading helpers.
- `tests/Sussudio.Tests/AutomationCommandDispatcher.CommandOwnership.Tests.cs`
  owns the live dispatcher source-family reader, shared dispatcher/proxy
  helpers, consolidated root dispatcher authorization and manifest behavior,
  Flashback failure response, Flashback command placement, verification command
  placement, command-kind handling, dispatcher JSON payload extraction helper
  coverage, payload defaults, trivial-handler payload-field parity checks
  against `AutomationCommandCatalog`, the custom
  `GetAudioRampTrace.maxEntries` metadata guardrail, dispatcher readiness
  gating, ready-independent no-hardware command coverage, window close, preview
  health, stale wait-refresh cadence guards, UI automation
  readiness-independent coverage, and harness payload/fake device support.
- `tests/Sussudio.Tests/AutomationToolContracts.Tests.cs` owns shared
  reflection helpers plus automation command kind, catalog metadata, manifest,
  path-policy, reliability-gates contract checks, and the expected command-ID
  table used by automation protocol/tool tests.
- `tests/Sussudio.Tests/XUnit.AutomationContractsTests.cs` owns fast xUnit
  coverage for pure `Sussudio.Automation.Contracts` command IDs, manifest IDs,
  pipe protocol command resolution, timeout, auth-token, envelope, and
  `CommandMap` contracts.
- `tests/Sussudio.Tests/AutomationToolContracts.Tests.cs` owns legacy harness
  coverage for window action enum membership and keeps the
  `ExpectedAutomationCommands()` adapter used by protocol/MCP helpers.
- `tests/Sussudio.Tests/AutomationToolContracts.Tests.cs` also owns shared
  implementations for automation command catalog metadata, manifest projection,
  path policy validation, manifest serialization, reliability-gates script
  contract tests, and the direct `AutomationToolContractsProtocolXunitTests`
  coverage for automation client timeout policy, advanced command-map
  alignment, pipe-connect failure, tool delegation, script freshness, and
  response-state contracts. It uses
  `RuntimeContractSource.ReadAutomationPipeClientSource()` for the shared
  AutomationPipeClient source family.
- `tests/Sussudio.Tests/XUnit.AutomationContractsTests.cs` owns the
  xUnit execution surface for catalog, manifest, path-policy, and
  reliability-gates checks after their removal from the legacy offline harness
  catalog.
- `tests/Sussudio.Tests/ArchitectureDocs.ReferenceIntegrity.Tests.cs` owns
  shared implementations for consolidated AGENT_MAP reference drift,
  test-owner code-span, README automation consumer, UI/presentation ownership,
  CaptureService ownership, Flashback preview startup wording, shared tool
  automation exact-path, duplicate tools/Common owner, empty test marker-shell
  checks, literal `ReadRepoFile` source-shape path drift, cleanup-plan
  file/folder reference drift, xUnit migration inventory checks, shared Markdown
  code-span path-token extraction and resolution helpers, AGENT_MAP consumer
  coverage, ownership-file discovery, exact code-span policy, xUnit inventory
  helpers, and the xUnit execution surface for those architecture-doc checks
  after their removal from the legacy offline harness catalog.
- `tests/Sussudio.Tests/AutomationToolContracts.SnapshotFormatter.Tests.cs`
    owns the shared/ssctl snapshot formatter contract family: typed accessors,
    core section formatting, section-order, Flashback opt-in smoke checks,
    source ownership, Flashback output rendering, and Preview D3D output rendering stay in
    `.Tests.cs`; shared formatter source ownership lives in `.Ownership.Tests.cs`.
  `tests/Sussudio.Tests/XUnit.ToolContractsTests.cs` owns the xUnit execution
  surface for those shared snapshot formatter checks plus the focused formatter
  JSON parsing and shared snapshot-field alignment checks.
- `tests/Sussudio.Tests/XUnit.ToolContractsTests.cs` owns ssctl formatted
  snapshot and timeline output smoke checks plus ssctl formatter source
  ownership assertions after their removal from the legacy offline harness
  catalog.
- `tests/Sussudio.Tests/XUnit.CoreRuntimeContractsTests.cs` owns
  `RuntimeContractSource`, including shared tool source-family readers used by
  legacy harness and xUnit contract tests.
- `tests/Sussudio.Tests/CommandHandlers.Routing.Tests.cs` owns pipe-captured
  routing contract checks for device, capture controls, recordings, Flashback,
  window, manifest, observability, automation-flow, UI visibility, and
  verification commands, ssctl handler partial-family source ownership
  assertions, ssctl help/catalog force-flag coverage, the source-family reader,
  routing-capture helpers, and `AssertSsctlCommandRequest`, which routes
  captured ssctl `request.command` checks through the shared golden command
  table instead of per-test numeric IDs.
  `tests/Sussudio.Tests/XUnit.ToolContractsTests.cs` owns the xUnit execution
  surface for those command-handler routing, source ownership, and help checks
  after their removal from the legacy offline harness catalog.
- `tests/Sussudio.Tests/ToolProbeContracts.Tests.cs` owns tool-probe behavior
  and source-ownership contracts for PresentMon parser swap-chain selection,
  artifact filtering, CSV field versions, app-present correlation, ssctl pipe
  transport command/retry/error shaping, KS audio-node probe ownership, and
  EGAVDS probe ownership.
- `tests/Sussudio.Tests/XUnit.ToolContractsTests.cs` owns the xUnit execution
  surface for PresentMon parser/source-ownership, ssctl pipe transport, KS
  audio-node, EGAVDS probe, RTK I2C unsafe-native-path, NVML snapshot, and
  CaptureSessionSnapshot tool-model checks after their removal from the legacy
  offline harness catalog. Keep the public wrapper classes in this file unless
  a group needs an independent fixture or executable helper state.
- `tests/Sussudio.Tests/HarnessCore.cs` owns shared tool assembly loading,
  isolated load contexts, freshness checks, and tool build command mapping used
  by the legacy harness and xUnit slices.
- `tests/Sussudio.Tests/HarnessCore.cs` owns the offline compatibility runner,
  xUnit target-assembly bootstrap, and no-op `RunAllChecksAsync` shim;
  executable checks now live in focused xUnit slices and contract files rather
  than a harness catalog.
- `tests/Sussudio.Tests/XUnit.CoreRuntimeContractsTests.cs` owns the former
  core-runtime registration group for runtime telemetry, capture-service
  snapshot, recording-integrity, NativeXu, frame-ledger, and basic app contract
  checks after their removal from the legacy offline harness catalog.
- `tests/Sussudio.Tests/XUnit.RecordingContractsTests.cs` also owns the former
  core-runtime recording registration group for recording verifier, LibAv
  encoder, Flashback integrity, recording-facing shared formatter, and
  dedicated LibAv verification script checks after their removal from the
  legacy offline harness catalog.
- `Sussudio/Services/Runtime/RuntimeHelpers.cs` owns runtime helper types
  shared across multiple services: AtomicMax, TelemetryAgeHelper,
  EnvironmentHelpers, RingBufferHelpers, shared minimum-window-size Win32
  subclassing, LocalAppData user-settings persistence and source-generated JSON
  context, bounded external process supervision contracts and runner, and
  best-effort MMCSS worker registration.
  `tests/Sussudio.Tests/XUnit.CoreRuntimeContractsTests.cs` owns their behavior
  and native-entry-point contracts.
- `tests/Sussudio.Tests/XUnit.AutomationContractsTests.cs` owns the former
  automation-diagnostics xUnit execution groups for app-surface, ViewModel and
  Flashback UI, dispatcher, capture/Flashback routing, snapshot projection,
  catalog/manifest/path-policy/reliability, and diagnostics-loop checks after
  their removal from the legacy offline harness catalog. Keep the public
  wrapper classes in this file unless a group needs an independent fixture or
  executable helper state.
- `tests/Sussudio.Tests/ServiceNamespace.FolderRules.Tests.cs` owns service
  folder-to-namespace architecture assertions, flat `Sussudio.Services`
  import bans, and the harness-visible service namespace/source ownership
  orchestrator, plus app-service contract boundary assertions that keep
  `Sussudio/Services/Contracts` separate from `Sussudio.Automation.Contracts`
  wire/protocol ownership, and AutomationCommandKind project/source ownership
  alignment across the app and automation tools, plus the shared source
  enumeration, project XML, and C# comment/string stripping helpers used by
  service namespace architecture assertions, plus NativeXuAudioProbe
  linked-source, split-source, locator, RTK unsafe-path behavior, and
  no-reflection source ownership assertions.
- `tests/Sussudio.Tests/ServiceNamespace.SourceOwnership.ServicesLayer.Tests.cs`
  owns DeviceService, NativeXu support, GPU interop, decoder, capture
  telemetry, MainViewModel source ownership orchestration assertions, and
  MainViewModel device-native audio state, mode/gain, request-controller,
  device refresh, capture device selection, format probe, source telemetry,
  recording capability, preview renderer enqueue, UI dispatch, property-change,
  runtime lifecycle/event-ingress, recording runtime, and disposal source
  ownership assertions.
- Focused `tests/Sussudio.Tests/XUnit.PresentationPreview*.cs` slices own
  presentation-preview capture/root policy, MainViewModel, MainWindow, stats,
  D3D renderer, preview pacing, and harness-registration execution surfaces.
- `tests/Sussudio.Tests/XUnit.PresentationPreviewContractsTests.cs`
  owns the former presentation-preview MainWindow xUnit execution groups:
  window lifecycle, launch/startup, preview screenshot workflow, shell chrome,
  visual shell, recording controls, audio controls, responsive layout, capture
  selection, resolution selection, capture runtime guardrails, initial
  MainWindow checks, preview runtime shell/policy checks, capture option checks,
  and output path checks. Keep the public wrapper classes in this file unless a
  group needs an independent fixture or executable helper state.
- `tests/Sussudio.Tests/XUnit.PresentationPreviewContractsTests.cs`
  owns the former presentation-preview MainViewModel xUnit execution groups:
  initial recording-transition failure propagation, audio controls and
  monitoring, output path and disk-space presentation, source telemetry text,
  dependency-composition seams, automation/runtime routing, capture settings,
  preview lifecycle ownership, and audio ramp trace telemetry. Keep the public
  wrapper classes in this file unless a group needs an independent fixture or
  executable helper state.
- `tests/Sussudio.Tests/XUnit.PresentationPreviewContractsTests.cs`
  owns the former presentation-preview capture Flashback buffer startup/recovery
  group for stale session cleanup and recovery-preserve behavior after their
  removal from the legacy offline harness catalog.
- `tests/Sussudio.Tests/XUnit.PresentationPreviewContractsTests.cs` owns the
  xUnit execution surface for the former presentation-preview D3D harness
  groups: pacing, geometry/screenshot, present cadence, device-lost,
  diagnostics, contracts/metrics ownership, runtime capture, render setup, and
  render pipeline checks.
- `tests/Sussudio.Tests/PreviewPacingClassifier.Tests.cs` owns preview pacing
  classifier source ownership, automation-snapshot wiring assertions, and
  behavioral classifier cases.
- `tests/Sussudio.Tests/XUnit.RecordingContractsTests.cs` also owns the former
  legacy recording-model execution surface for LibAv sink loop/source-ownership
  checks, capture runtime failure/runtime-flag checks, and the large Flashback
  buffer manager behavior/source-ownership group.
- `tests/Sussudio.Tests/XUnit.FlashbackContractsTests.cs` owns the xUnit
  execution surface for Flashback buffer option sizing, session/playback/export
  DTO contracts, and the former legacy Flashback exporter, decoder, playback,
  and encoder sink registration groups while preserving the same test classes
  and method names.
- `tests/Sussudio.Tests/XUnit.ToolContractsTests.cs` owns the xUnit execution
  surface for the former legacy NVML snapshot, CaptureSessionSnapshot
  default-state, and RTK I2C unsafe-native-path tool-contract checks.
- `tests/Sussudio.FfmpegEncodeLab/Program.cs` owns standalone HDR encode-lab
  orchestration, CLI parsing, tool-path resolution, child-process log capture,
  FFmpeg argument construction, and AV1 encoder selection policy.
- `tests/Sussudio.Tests/HarnessCore.cs` owns shared harness primitives:
  generic assertions, repo-root/file reads, automation snapshot source family
  readers, source-text extraction, reflection/private-field access,
  enum/type lookup, capture configuration reflection helpers, synthetic
  capture/settings/recording-context factories, capture-service initialization,
  async disposal, polling waits, and field-value fixture helpers.
- `tests/Sussudio.Tests/XUnit.CaptureConfigurationModelsTests.cs` owns shared
  reflection helpers plus capture mode option display metadata, option-builder
  behavior, capture settings defaults, output path/file naming, bitrate policy,
  MJPEG HFR policy, recording selection policy, encoder support, and recording
  pipeline option xUnit contract checks.
- Focused capture session coordinator coverage lives in
  `tests/Sussudio.Tests/CaptureSessionCoordinator.Api.Tests.cs` and
  `CaptureModels` files; API/model/source ownership checks include the
  consolidated coordinator root, focused Flashback coordinator partials,
  coordinator queue/cancellation/rejection contracts, and shared reflective
  harness helpers.
- `tests/Sussudio.Tests/PooledVideoFrame.Tests.cs` owns shared pooled-frame
  reflection, frame factory, jitter-buffer factory, tracking pool helpers,
  pooled video frame lease lifecycle, MJPEG pooled-frame fan-out contracts, and
  queued lease return coverage for D3D pending-frame, recording, and Flashback
  paths.
- `tests/Sussudio.Tests/PooledVideoFrame.MjpegJitterQueue.Tests.cs` owns
  MJPEG preview jitter frame-ingress, emit-loop, adaptive deadline policy,
  queue, metrics source-ownership assertions, and queue/drop/reprime behavior
  tests.
- `tests/Sussudio.Tests/McpToolSurface.CommandRouting.Tests.cs` owns MCP
  command-routing route/formatter assertions plus surface compatibility checks
  that span raw app state, capture options, capture settings, and UI settings
  tools. It also owns source guards that fixed-command MCP automation routes
  call `AutomationCommandKind` enum overloads at the pipe seam while preserving
  existing command labels and wire IDs.
- Keep MCP command-routing route/formatter assertions in the focused sections
  of this file for the
  `CommandRouting.Capture`, `CommandRouting.Host`,
  `CommandRouting.Recording`, `CommandRouting.Formatting`,
  `CommandRouting.Device`, `CommandRouting.Pipeline`, `CommandRouting.Ui`, and
  `CommandRouting.Verification` owner files. Captured command-ID assertions use
  the shared `AssertAutomationCommandId` helper so the golden command table is
  the only test-owned numeric ID list.
- `tests/Sussudio.Tests/McpToolSurface.DiagnosticSession.Runner.Tests.cs`
  owns MCP `run_diagnostic_session` success/failure artifact contract tests
  alongside the focused diagnostic-session runner behavior tests.
  `tests/Sussudio.Tests/XUnit.ToolContractsTests.cs` owns the xUnit
  execution surface for the general MCP tool-surface, command-routing,
  host/pipe, verification, Flashback tool, diagnostic-session tool entry,
  performance/probe, and window/preview tool contracts after their removal from
  the legacy harness catalog.
- `tests/Sussudio.Tests/McpToolSurface.DiagnosticSession.Ownership.Tests.cs`
  owns diagnostic-session helper ownership checks for planning/setup,
  execution/startup/sampling, teardown/reporting, post-run snapshots,
  recording verification, and shared session metrics.
- Diagnostic-session infrastructure ownership checks live in
  `tests/Sussudio.Tests/McpToolSurface.DiagnosticSession.InfrastructureOwnership.Tests.cs`:
  runner/initial-snapshot, pipe retry/command-channel,
  run-state/live-state/context/bootstrap/output-lock, and scenario/completion
  phase checks stay grouped in one infrastructure spec.
- `tests/Sussudio.Tests/McpToolSurface.DiagnosticSession.ResultOwnership.Tests.cs`
  owns diagnostic-session model ownership assertions, formatter ownership,
  builder summary-write failure, artifact, JSON/shared-text checks, core
  result-builder construction, preview scheduler, overview/capture checks,
  Flashback playback, recording, and export result projections, preview result
  projections, analysis-warning, diagnostic-health, and artifact-handoff
  ownership.
- `tests/Sussudio.Tests/McpToolSurface.DiagnosticSession.Flashback.Scenarios.Tests.cs`
  owns diagnostic-session Flashback warmup health-policy, warning-policy,
  snapshot polling wait, cycle, preview-cycle, rejected-export,
  segment-playback, export, recording-settings, lifecycle, stress,
  audio-master fallback classification, export-helper, segment wait/parsing,
  and Flashback metric projection ownership assertions.
- `tests/Sussudio.Tests/McpToolSurface.DiagnosticSession.Runner.Tests.cs` owns
  focused diagnostic-session runner behavior coverage: reflective runner
  setup, artifacts, health policy, Flashback playback, initial snapshot,
  pipe retry, and concurrency checks against synthetic command delegates.
- `tests/Sussudio.Tests/XUnit.ToolContractsTests.cs` owns the
  xUnit execution surface for the former legacy diagnostic-session catalog
  bands: infrastructure, result surface, command/run context, scenario
  execution, Flashback scenarios/helpers/metrics/waits/validation/stress,
  sampler/metric/health core checks, and runner behavior checks. Keep the
  public wrapper classes in this file unless a band needs an independent
  fixture or executable helper state.
- `tests/Sussudio.Tests/HarnessCore.cs` keeps the compatibility runner entry
  point; diagnostic-session checks now execute through xUnit wrappers.
- `tests/Sussudio.Tests/McpToolSurface.DiagnosticSession.Runner.Tests.cs`
  owns shared reflective runner setup and diagnostic-session runner behavior
  tests: loading `ssctl`, creating `DiagnosticSessionOptions`, invoking
  `DiagnosticSessionRunner.RunAsync`, parsing synthetic JSON responses, and
  validating artifacts, health policy, Flashback playback, initial snapshot,
  pipe retry, and concurrency behavior.
- `tests/Sussudio.Tests/McpToolSurface.Performance.*.Tests.cs` owns MCP
  performance timeline contract, Flashback timeline formatting, PresentMon MCP
  correlation and option precedence coverage, and frame-pacing verdict tests.
  `McpToolSurface.Performance.Tools.Tests.cs` keeps shared performance-tool
  source loading, timeline source-ownership assertions, rendering text
  contracts, Flashback command-counter formatting checks,
  `PerformanceTimelineEntry` projection contracts, PresentMon correlation
  coverage, and frame-pacing verdict source-shape plus behavior checks
  together.
  `tests/Sussudio.Tests/XUnit.ToolContractsTests.cs` owns the xUnit
  execution surface for these performance/probe contracts after their removal
  from the legacy harness catalog.
- `tests/Sussudio.Tests/McpToolSurface.WindowPreview.Tests.cs` owns MCP wait,
  window action, preview toggle, Flashback toggle, screenshot,
  preview-frame-capture, and probe tests. `tests/Sussudio.Tests/XUnit.ToolContractsTests.cs`
  owns the xUnit execution surface for the wait/window/screenshot/
  preview-frame/preview-toggle/probe checks after their removal from the
  legacy harness catalog.
- `tests/Sussudio.Tests/McpToolSurface.Helpers.cs` owns shared MCP
  process/JSON-RPC, reflection/tool-result, pipe-capture, and JSON assertion
  helpers.
- `tests/Sussudio.Tests/HarnessCore.cs` owns shared Flashback test helper
  source readers, helper methods, buffer test factories, completed-segment
  insertion, and sized-file helpers used across focused Flashback test files.
- `tests/Sussudio.Tests/Flashback.Buffer.Segments.Validation.Tests.cs` owns
  Flashback buffer segment completion metadata, outside-path rejection,
  disposed-state no-op, recovery-preserve, segment diagnostics, PTS clamp, byte
  accounting, same-path extension tests, segment position lookup, next-segment
  path lookup, path normalization, segment-start PTS, segment range query,
  active path, segment-count, segment-list behavior, and buffer-manager source
  ownership assertions for root state, segment mutation/query, lifecycle,
  purge, and eviction-pause placement.
- `tests/Sussudio.Tests/Flashback.Buffer.Retention.Eviction.Tests.cs` owns
  Flashback buffer eviction accounting, purge retention, active-byte accounting,
  eviction-pause behavior, and initialization recording-PTS reset tests.
- `tests/Sussudio.Tests/Flashback.Buffer.Retention.StartupCleanup.Tests.cs`
  owns Flashback buffer startup-generated segment cleanup, legacy root cleanup,
  stale session directory cleanup, session-recovery scanner ownership,
  unrelated temp-directory preservation, startup-cache budget, session-id, and
  segment-extension validation tests.
- `tests/Sussudio.Tests/Flashback.EncoderSink.Tests.cs` owns Flashback encoder
  sink frame-rate, option, startup rollback, runtime counter, PTS guard, queue
  rejection, lifecycle cleanup, packet-validation, drain-loop ordering,
  force-rotate, and segment-registration recovery tests.
- `tests/Sussudio.Tests/XUnit.FlashbackContractsTests.cs` owns the xUnit
  execution surface for the former legacy Flashback encoder sink frame-rate,
  codec, counter, queue, force-rotate, packet-drain, startup, and
  source-ownership checks after their removal from the legacy harness catalog.
- `tests/Sussudio.Tests/Flashback.Exporter.Behavior.Tests.cs` owns Flashback
  exporter request-surface smoke tests, path/request validation, cancellation
  precedence, cancelled lock-wait behavior, export throttle tests,
  failure-classifier status-message mapping, range validation, buffered-packet
  failure cleanup, progress/finalization assertions, timestamp saturation,
  segment template selection, stream-layout validation, and requested-segment
  skip policy tests.
- `tests/Sussudio.Tests/Flashback.Exporter.Ownership.Tests.cs` owns
  Flashback exporter source-ownership tests, task-wrapper infrastructure,
  disposal timeout/native-state lifetime guards, and stream-count/template
  stream-copy owner/call-site tests.
- `tests/Sussudio.Tests/Flashback.Exporter.OutputPaths.Tests.cs` owns Flashback
  exporter segment path, duplicate path, missing segment, output path
  validation, source-overwrite guard, blocked temp-path tests, final-output
  replacement, overwrite refusal/force behavior, final validation cleanup,
  orphan temp-file cleanup, and output-directory scan guard tests.
- `tests/Sussudio.Tests/XUnit.FlashbackContractsTests.cs` owns the xUnit
  execution surface for the former legacy Flashback exporter cleanup, request
  validation, failure classification, segment, cancellation, output
  path/finalization, and source-ownership checks after their removal from the
  legacy harness catalog.
- `tests/Sussudio.Tests/Flashback.Playback.SourceShape.Tests.cs` owns
  Flashback playback root state, pre-initialize command no-ops, no-op/coalesced
  command state, command-position clamping, saturating timestamp arithmetic,
  segment-open recovery, near-live snap, snap-live identity cleanup,
  pause-from-live display, paused nudge, live-preview transition, audio guard,
  and audio-master projection/source ownership tests.
- `tests/Sussudio.Tests/Flashback.Playback.Reopen.Tests.cs` owns Flashback
  playback fMP4 reopen, seek-display, seek recovery, in/out marker API,
  normalization, disposal, and marker clamp tests.
- `tests/Sussudio.Tests/Flashback.Playback.CommandQueue.Tests.cs` owns
  Flashback playback command queue capacity/drop-oldest, scrub-coalescing
  source ownership, seek-slot barrier/failure behavior, playback thread
  recovery, command dispatch, thread lifecycle, and command telemetry
  coverage.
- `tests/Sussudio.Tests/Flashback.Playback.Frames.Tests.cs` owns Flashback
  playback frame-duration, decoded-PTS cadence projection/telemetry, decode
  metrics reset/projection, decoded-frame submit-failure, preview frame
  submission, held-frame ownership, and live-recovery ownership tests.
- `tests/Sussudio.Tests/XUnit.FlashbackContractsTests.cs` owns the xUnit
  execution surface for the former legacy Flashback playback startup,
  command-queue, source-shape, cadence, submission, reopen, transition-guard,
  and metric-reset checks after their removal from the legacy harness catalog.
- `tests/Sussudio.Tests/Flashback.Decoder.Tests.cs` owns Flashback decoder
  audio, timestamp, stream-bound, validation, lifetime, callback,
  source-shape, D3D11VA setup, and support/logging contract tests.
- `tests/Sussudio.Tests/XUnit.FlashbackContractsTests.cs` owns the xUnit
  execution surface for the former legacy Flashback decoder frame-buffer,
  source-ownership, state/lifetime, timestamp, audio, frame-validation, and
  cancellation checks after their removal from the legacy harness catalog.
- `Sussudio/Controllers/Flashback/FlashbackUiControllers.cs` owns Flashback
  timeline visibility, lockout, toggle synchronization, timeline track layout
  sizing, show/hide storyboard state, immediate collapse, fullscreen animation
  reset, active pointer-scrub state, scrub throttling, release/cancel/capture-lost
  cleanup, fullscreen scrub termination, lockout clearing, scrub visual updates,
  pure timeline fraction/duration math, playhead motion context, playback-state
  sampling, scrub/window gating, live right-edge pinning, long-horizon
  extrapolation scheduling, CTI anchor timing, compositor visual setup, snap
  placement, magnetic pointer-scrub movement, linear keyframe animation, and
  label clamp/positioning. `Sussudio/MainWindow.xaml.cs` owns the XAML-facing
  command, polling, playhead, scrub, settings, timeline, and presentation
  adapter surface.
- `Sussudio/Controllers/Flashback/FlashbackUiControllers.cs` owns
  Flashback marker placement, selection-region layout, and compact duration
  text formatting. `Sussudio/MainWindow.xaml.cs` wires marker
  presentation callbacks.
- `Sussudio/Controllers/Flashback/FlashbackUiControllers.cs` also owns
  Flashback playback UI sequencing: track-resize snap/position/marker/CTI
  refresh order, playback state polling start/stop, play/pause glyph policy,
  Go Live enabled state, buffer-duration text, buffer-fill/position/marker
  refresh order, and position-label updates with CTI re-anchor gating.
- `Sussudio/Controllers/Flashback/FlashbackUiControllers.cs` also owns
  Flashback command semantics for in/out points, clear, play/pause, Go Live,
  fullscreen keyboard shortcuts including left/right nudge rejection logging,
  export, save-last-5m, enable-toggle rollback, apply/restart, and the XAML
  command event-handler surface adapter.
- `Sussudio/Controllers/Flashback/FlashbackUiControllers.cs` also owns
  Flashback export progress-bar value, visibility, and reset-on-complete
  semantics. `Sussudio/MainWindow.xaml.cs` wires the
  export progress presentation controller.
- `Sussudio/Controllers/Flashback/FlashbackUiControllers.cs` owns Flashback
  settings-control initialization, GPU decode toggle binding/sync, buffer
  duration combo selection/sync, and buffer-duration change logging.
  `Sussudio/MainWindow.xaml.cs` is the XAML-facing settings
  adapter; enable toggle rollback and apply/restart command behavior live in
  `FlashbackUiControllers.cs`.
- `Sussudio/Controllers/Flashback/FlashbackUiControllers.cs` owns Flashback status
  and playback-position polling timers. `Sussudio/MainWindow.xaml.cs`
  is the XAML-facing adapter; CTI anchor timing lives with Flashback UI
  playhead motion in `FlashbackUiControllers.cs`.
- `Sussudio/Controllers/Shell/ShellChromeController.cs` owns settings shelf
  visibility, the animation gate, and show/hide storyboard construction.
  `Sussudio/MainWindow.Composition.cs` is the XAML-facing adapter.
- `Sussudio/Controllers/Launch/LaunchFlowController.cs` owns loaded-time
  startup ordering and launch entrance choreography: native shell reveal
  scheduling, initial ViewModel settings load, preview audio fade priming before
  device refresh, no-preview fallback presentation, automation host start,
  splash/entrance trigger, initial hidden/scaled shell state, splash fade,
  one-shot splash playback state, loading-phrase start/stop ordering, splash
  phrase file lookup, Markdown-ish parsing, cached defaults, exception fallback,
  randomized interval/mode selection, DispatcherTimer lifecycle, two-line text
  animation, handoff into shell entrance, shell chrome/button/stats entrance
  choreography, deferred preview reveal logging, active-storyboard cleanup, and
  the delayed control-bar shadow fade routed through the shadow controller in
  `PreviewTransitionAnimationController.cs`. `Sussudio/MainWindow.Composition.cs`
  is the XAML-facing Loaded, phrase start/stop, and launch entrance adapter.
- `Sussudio/Controllers/Shell/ShellChromeController.cs` owns control-bar
  button entrance/hover/press/release animation, static shell ThemeShadow and
  translation setup for the control bar and record button, plus shell
  property-change routing across stats overlay and settings shelf controllers,
  settings shelf animation, status-strip projection, and window title formatting.
  `Sussudio/MainWindow.Composition.cs` is the XAML-facing adapter.
- `Sussudio/Controllers/Preview/PreviewTransitionAnimationController.cs` owns preview
  shell/content fade and scale transitions, video-shadow fade timing,
  unavailable-placeholder fades, startup/unavailable presentation prep, preview
  reinit animation active state, first-visual transition clears, startup-reset
  preservation, completion presentation decisions, and the
  `D3D11_RENDERER_REINIT_FLAG` / `PREVIEW_REINIT_ANIMATE_*` logs.
  `Sussudio/MainWindow.Composition.cs` wires
  preview-transition animation callbacks; video-shadow fade callbacks route
  through the preview surface shadow controller in this owner.
- `Sussudio/Controllers/Preview/PreviewLifecycleEventController.cs` owns preview
  button glyph/tooltip presentation for Start Preview and Stop Preview plus
  preview button command choreography: pending-reinit cancel, user stop intent,
  audio/visual fade-out ordering, preview start/stop calls, reinit animation
  reset, unavailable-placeholder reveal, and delayed preview reveal after first
  visual while preserving the `PreviewButtonActionController`,
  `PreviewButtonPresentationController`, and `PreviewFadeInController` types.
  `Sussudio/MainWindow.Composition.cs` wires preview button presentation callbacks and preview
  lifecycle property/event routing.
- `Sussudio/MainWindow.Composition.cs`
  keeps the XAML event name stable as part of the preview transition/presentation
  adapter.
- `Sussudio/Controllers/Recording/RecordingControlsControllers.cs` owns
  recording visual behavior: pure recording-state lockout decisions, recording
  property-change routing, ViewModel-derived lockout/HDR/title/audio-meter
  policy application, delegated record-button chrome, recording glow, Rec pulse,
  starting spinner, normal/recording content, padding, enabled-state
  application, and the circle/pill width morph. `MainWindow.xaml.cs`
  wires the chrome controller, recording action adapter, and recording-state
  presentation adapter.
- `Sussudio/Controllers/Recording/RecordingControlsControllers.cs` owns the recording
  button command workflow and preview-state logging after a start.
  `MainWindow.xaml.cs` is the XAML-facing adapter for recording and
  capture-device button workflows.
- `Sussudio/Controllers/Shell/ShellChromeController.cs` owns live-signal pill
  text application, visibility state, show/hide debounce timers, and the small
  scale/fade animation beside the rest of shell chrome. `MainWindow.Composition.cs`
  is the XAML-facing adapter. `Sussudio/ViewModels/ViewModelBuilders.cs` owns
  the view-model live-signal label formatting and pixel-format/codec suffix
  policy.
- `Sussudio/Controllers/Preview/PreviewLifecycleEventController.cs` owns preview-volume
  fade-in/fade-out state, saved target volume, storyboard lifetime, volume
  save suppression, preview start/stop/reinit event routing, and preview button
  presentation/fade-in timing. `Sussudio/MainWindow.Composition.cs`
  is the XAML-facing adapter.
- `Sussudio/Controllers/Preview/PreviewTransitionAnimationController.cs` owns preview
  reinit animation active state, first-visual transition clears, startup-reset
  preservation, completion presentation decisions, and the
  `D3D11_RENDERER_REINIT_FLAG` / `PREVIEW_REINIT_ANIMATE_*` logs.
  `Sussudio/MainWindow.Composition.cs` is the XAML/MainWindow
  adapter that supplies renderer-stop-before-teardown and UI callback endpoints
  for reinit completion.
- `Sussudio/Controllers/Preview/Startup/PreviewStartupControllers.cs` owns preview
  startup attempt/state bookkeeping, timestamps, cached failure/missing-signal
  details, state/log transitions, first-visual confirmation sequencing,
  signal-window predicates, snapshot missing-signal refresh gates, reset
  orchestration, watchdog/telemetry timers, timeout configuration, timeout
  recovery, failure-stop scheduling, readiness-signal state handoff,
  required/received state, missing-signal calculation and updates,
  playback-progress diagnostics, startup signal log strings, GPU position
  counter state, first-visual confirmation decisions, signal-list formatting,
  timeout diagnostic payload formatting, playback-advance threshold checks, and
  readiness result snapshots.
  `Sussudio/MainWindow.Composition.cs` wires UI/runtime
  callbacks into the session, watchdog, and signal controllers, stable state
  projections, startup state, renderer-attached, first-visual, begin-attempt,
  reset adapters, raw timeout diagnostic snapshots, live preview signal state,
  renderer visibility details, logging, and confirmation callbacks.
  `PreviewStartupControllers.cs` also owns preview startup timeout reason,
  timeout status, and failure-stop status text.
  `Sussudio/Controllers/Preview/PreviewLifecycleEventController.cs` owns preview-
  specific ViewModel event lifecycle and the preview property-change router for
  preview start/stop/reinit state.
  `Sussudio/MainWindow.Composition.cs` wires preview button
  presentation callbacks and
  preserves preview event-handler signatures and delegates into the controller.
  `Sussudio/Controllers/Preview/PreviewTransitionAnimationController.cs` owns preview
  reinit animation active state, first-visual transition clears, startup-reset
  preservation, completion presentation decisions, and reinit transition logs.
  `Sussudio/MainWindow.Composition.cs` keeps the renderer-stop-before-teardown
  handoff and XAML callback endpoints for completion presentation.
  Keep preview startup fields out of the composition root.
- `Sussudio/Controllers/Preview/PreviewLifecycleEventController.cs` owns delayed
  preview reveal after first visual: rendered-frame threshold, fade-in timer,
  renderer replacement fallback, and preview-audio fade start ordering.
  `Sussudio/MainWindow.Composition.cs` wires the XAML-facing adapter. Keep
  timeout/watchdog recovery in `PreviewStartupWatchdogController`.
- `Sussudio/Controllers/Preview/PreviewTransitionAnimationController.cs` owns preview-
  startup loading overlay presentation while the app waits for visual
  confirmation: ProgressRing activation, fade-in/fade-out routing, and the
  reinit-collapse opacity reset. It also owns preview reinit animation state and
  completion presentation decisions. `Sussudio/MainWindow.Composition.cs`
  is the XAML-facing adapter.
- `Sussudio/Controllers/Preview/Renderer/PreviewRendererHostController.cs` owns top-level
  preview resize log throttling and reset state.
  `Sussudio/MainWindow.Composition.cs` wires renderer-host
  context callbacks, the XAML-facing `SizeChanged` adapter, renderer-host reset
  handoff, renderer start/stop/shutdown, and reinit-unsafe-window adapters;
  reinit renderer-stop/timeout policy lives with `PreviewRendererHostController.cs`;
  preview surface presentation and preview shadow visuals live in
  `PreviewTransitionAnimationController.cs`.
- `Sussudio/MainWindow.xaml.cs` is the XAML-facing recording
  adapter. Recording-specific property-name routing, record-button, glow, pulse,
  and recording-time lockout projection live in
  `RecordingStatePresentationController`.
- `Sussudio/MainWindow.xaml.cs` is the XAML-facing recording,
  device, and output-path button/display adapter. `OutputPathController` owns
  output-path property-change routing, textbox updates, and browse/open
  commands.
- `Sussudio/MainWindow.xaml.cs` is the XAML-facing adapter
  for capture option setup, event binding, and capture-option/source-signal
  property-change routing; the property-name router lives in
  `CaptureOptionBindingController`.
- `Sussudio/MainWindow.Composition.cs` is the XAML-facing
  shell property-change adapter.
  `Sussudio/Controllers/Shell/ShellChromeController.cs` owns the shell
  property-change route order across `StatsOverlayCompositionController` and
  `SettingsShelfController`; stats visibility behavior still lives in the stats
  composition controller, while settings visibility behavior lives with shell
  chrome in `ShellChromeController`.
- `Sussudio/MainWindow.Composition.cs` is the XAML-facing live signal
  adapter. `ShellChromeController.cs` owns live source-signal property-change
  routing and pill presentation.
- `Sussudio/Controllers/Flashback/FlashbackUiControllers.cs` owns
  Flashback-specific property-change routing for timeline lockout, markers,
  playhead updates, export progress, and settings-control synchronization.
  `Sussudio/MainWindow.xaml.cs` is the XAML/MainWindow property-change
  adapter that composes the Flashback route table callbacks alongside the root
  ViewModel router.
- `Sussudio/Controllers/Audio/AudioControlBindingController.cs` owns audio and
  microphone property-change routing/projections: audio toggles, monitoring
  meter state, preview volume slider sync, microphone enablement, and microphone
  volume sync, alongside initial audio/microphone projection, event hookup,
  microphone volume slider synchronization, save triggers, shelf enablement, and
  mic-meter row animation state.
  `Sussudio/MainWindow.xaml.cs` is the XAML-facing audio/microphone
  presentation adapter.
- `Sussudio/Controllers/Shell/ShellChromeController.cs` owns the responsive
  control-bar label breakpoint, narrow/wide placement policy, responsive
  visibility for the complete control-bar label set, and capture-settings grid
  placement to XAML elements beside shell chrome animation/status/title owners.
  `MainWindow.Composition.cs` is the XAML-facing adapter.
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
  `Sussudio/MainWindow.xaml.cs` owns controller
  instantiation, XAML dependency wiring, collection/property-change adapters,
  and the thin XAML-facing selection bridges for device, audio, device-audio,
  capture-mode, and recording option selection.
- `Sussudio/Controllers/Audio/AudioControlBindingController.cs` owns the audio-control
  binding context, initial audio/microphone projection, preview-volume binding and priming,
  audio/microphone/device-audio selection handlers,
  record/preview/custom-audio/microphone toggle handlers, audio-meter activation,
  initial meter presentation, device-audio gain/meter resize hooks, and
  audio/microphone property-change projections for audio toggles, monitoring
  meter state, preview-volume slider sync, microphone enablement, and
  microphone volume sync. It also keeps microphone volume slider synchronization,
  save triggers, shelf enablement, and mic-meter row animation state because
  those are driven only by the audio-control binding/presentation flow.
  Device-audio mode/gain control projection stays in
  `Sussudio/Controllers/Capture/CaptureSelectionBindingController.cs`.
  `Sussudio/MainWindow.xaml.cs` is its XAML-facing adapter.
- `Sussudio/Controllers/Capture/CaptureSelectionBindingController.cs` owns the capture-
  device refresh/apply button workflows and preserves the explicit apply/reinit
  path alongside capture-device selection synchronization.
  `MainWindow.xaml.cs` is the XAML-facing adapter for recording,
  capture-device, and output-path button/display bridges.
- `Sussudio/Controllers/Capture/CaptureOptionBindingController.cs` owns the
  capture option binding adapter context, setup, UI event attachment,
  initialization, resolution/frame-rate selection, recording option event
  bindings, show-all binding, HDR/true-HDR click binding,
  `CaptureComboBoxSelectionNormalizer` use for shared frame-rate auto/exact
  matching, capture-option/source-signal property-change routing,
  custom-bitrate control sync, HDR/true-HDR ViewModel-to-control sync, preview
  HDR passthrough forwarding, pure capture-option presentation decisions, XAML
  control application, decoder-count selection handling, HDR hint/FPS telemetry
  tooltip text policy, and delegated presentation callbacks for option
  affordances, telemetry tooltips, and source overlay refreshes.
  `MainWindow.xaml.cs` is the XAML-facing capture and
  recording option adapter, including the small property-change forwarding
  method that delegates to this controller.
- `Sussudio/Controllers/Recording/RecordingControlsControllers.cs` owns recording output-
  path textbox, tooltip, resize-event updates, and browse/open-recordings button
  workflows plus pure output-path truncation text policy.
  `MainWindow.xaml.cs` is the XAML-facing adapter used by binding
  setup, property changes, and button events.
- `Sussudio/ViewModels/MainViewModel.*.cs` for root presentation state and
  automation-facing compatibility. `MainViewModel.cs` owns the public
  compatibility-facade shell, shared shell/status/live-info state, native
  window handle state, UI collection replacement, construction, dependency
  assignment, collaborator fields, controller graph handoff, startup lifecycle
  kick-off, non-preview coordination gates, and small bridge methods.
  `MainViewModel.cs` owns preview lifecycle compatibility entry
  points, preview-sink handoff, preview lifecycle flags,
  preview reinitialize coordination, and preview request events; `MainViewModel.cs` owns capture-selection
  state, option collections, HDR capture/runtime presentation state, and
  source signal/source-telemetry presentation state; `MainViewModel.AudioState.cs` owns audio,
  microphone, device-native audio/XU UI state, live meter callback state,
  custom audio-input retargeting, preview-monitoring ramp handoff, and
  audio-preview property-change routing; `MainViewModel.FlashbackState.cs` owns Flashback
  timeline/export state plus buffer, bitrate, playback-state, in/out marker,
  and gap-from-live UI projection. Keep callback-thread meter targets
  in `MainViewModel.AudioState.cs` and out of the root facade file.
  `Sussudio/ViewModels/PreviewAudioTransitionControllers.cs`
  owns audio ramp diagnostic state, bounded ring-buffer storage, snapshot
  projection, trace session start/complete, trace-point capture, sampler loop,
  delayed sampler shutdown, preview-volume save suppression/override state,
  priming, restoring, trace adapters, property-to-session volume forwarding,
  preview-audio ramp constants, easing, and async ramp-down/ramp-up execution.
  `MainViewModel.AudioState.cs` keeps the automation-facing audio-ramp trace
  adapter methods plus trace recorder and preview-volume transition controller
  wiring.
  `MainViewModel.AudioState.cs` owns
  audio capture enablement and Flashback restart/teardown routing.
  `MainViewModel.AudioState.cs` owns audio-preview monitoring toggle routing,
  preview-volume save suppression/override properties, change notification,
  ramp adapter methods, persisted preview-volume save routing, preview
  monitoring coordinator sequencing, custom audio-input property handlers,
  retargeting, preview-monitoring ramp handoff, microphone observable state,
  endpoint volume synchronization, persistence, and microphone property-change
  routing.
  `Sussudio/Controllers/ViewModel/MainViewModelDeviceAudioRequestController.cs`
  is a top-level `Sussudio.Controllers` owner for device-native audio request
  lifetime: selected-device refresh scheduling, mode-change scheduling, shared
  debounce CTS fields, UI enqueue lifetime, graph-port context contract,
  analog-gain property-change scheduling, UI/XU request debounce,
  flash-persist debounce, and cancellation cleanup. The compatibility
  property-change adapters stay with the observable device-audio state in
  `MainViewModel.AudioState.cs`.
  `MainViewModel.AudioState.cs` owns device-native audio-control support
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
  `MainViewModel.CaptureSelection.cs`
  owns capture-mode property handlers for selected resolution, selected format,
  selected video format, and MJPEG decoder count changes.
  `Sussudio/Controllers/UiDispatchControllers.cs` owns
  shared view-model UI dispatcher enqueue/invoke policy, disposal skip logging,
  cancellation handoff, enqueue-failure logging, status projection, and the UI
  dispatch graph-port contract for dispatcher access, disposal state, logging,
  exception logging, and status text projection.
  `MainViewModel.cs` owns the stable private UI-dispatch adapter
  names plus preview event fan-out for the partial family, beside the
  controller graph construction that consumes those ports.
  `Sussudio/Controllers/ViewModel/MainViewModelLifecycleController.cs`
  is a top-level `Sussudio.Controllers` owner for periodic timer refresh orchestration, initial
  source-telemetry/HDR/live-info/timer/disk-space bootstrap, and the
  runtime lifecycle graph-port contract for timer creation, runtime
  snapshot sampling, telemetry bootstrap, live-info/HDR projection, recording
  stats refresh, Flashback bitrate refresh, disk-space refresh, and watcher
  disposal, plus runtime event handling through graph-built context ports:
  system-resume preview rebind handling, audio-device-invalidated rebind
  scheduling through the preview lifecycle owner, capture status/error fan-out,
  capture pre-cleanup renderer stop fan-out, frame-captured callbacks, the
  runtime event ingress graph-port contract, and event
  subscription/unsubscription ordering including the desktop power-resume
  signal.
  `Sussudio/Controllers/ViewModel/MainViewModelDeviceDiscoveryControllers.cs`
  is a top-level `Sussudio.Controllers` owner for late device-format probe event
  ingress, UI enqueue/generation checks, selected-device capability refresh,
  UI-side late-probe retarget application, HDR/SDR reinitialize dispatch,
  MJPG HFR preserve, session mismatch checks, and active-capture restore
  through graph-built context ports.
  `MainViewModel.cs` owns recording-runtime counters and the DiskSpaceInfo assignment bridge,
  while `Sussudio/ViewModels/ViewModelBuilders.cs` owns output drive probing,
  fallback, formatting, and suppressed-warning logging.
  `MainViewModel.cs` owns
  recording size/bitrate label assignment, recording-state reset reactions, and
  the bounded byte-sample smoothing helper shared by recording and Flashback
  bitrate presentation.
  `MainViewModel.cs` owns capture presentation adapters:
  live-capture info projection from `CaptureRuntimeSnapshot`, including
  audio-preview activity and live-resolution/frame-rate/pixel-format
  assignment, preview-stop live-info reset, HDR runtime state/readiness
  projection, target-summary property application, and auto-resolution display
  text used by status and telemetry presentation. It delegates live-signal
  label formatting to
  `Sussudio/ViewModels/ViewModelBuilders.cs`.
  `MainViewModel.cs` owns the impure capture-settings adapter that
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
  `Sussudio/Controllers/ViewModel/MainViewModelPreviewLifecycleController.cs`
  is a top-level `Sussudio.Controllers` owner for debounced reinitialization,
  restart-cancellation state, Flashback-cycle wait-before-reinit,
  renderer-stop handoff, teardown restart, and reinit gate release.
  It also owns the graph-built reinitialization port contract for selected
  device/format state, generation coalescing, pending Flashback-cycle waits,
  debounce/timeout policy, renderer notifications, restart cancellation, and
  reinit gate access.
  `MainViewModel.cs` owns the stable recording facade:
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
  `Sussudio/Controllers/ViewModel/MainViewModelLifecycleController.cs` is a
  top-level `Sussudio.Controllers` owner for bounded teardown, dispose timeout policy, watcher disposal, coordinator
  cleanup/dispose, capture-service async-dispose fallback, disposal-step
  logging, and the disposal graph-port contract for one-shot disposal entry, teardown
  cancellations, runtime stop, coordinator cleanup/dispose, and capture-service
  async/sync disposal fallback, plus the bounded wait helper port that keeps
  timeout behavior explicit. `MainViewModel.cs` is the public refresh/dispose
  adapter and owns active Flashback export cancellation during teardown.
  `MainViewModel.cs` owns automation-facing command,
  capture runtime, health, and recording snapshot projection.
  `MainViewModel.cs` also owns automation-facing source/preview probes and preview frame capture.
  `MainViewModel.cs` owns automation-facing view-model runtime snapshot UI-thread capture.
  `ViewModelBuilders.cs` owns pure view-model runtime snapshot DTO construction.
  `tests/Sussudio.Tests/ViewModelBuilders.Tests.cs` owns executable coverage for
  those pure view-model DTO builders plus source telemetry and live-signal text
  presentation helpers.
  `MainViewModel.cs` owns automation-facing options
  UI-thread snapshot capture for CLI/MCP clients, while
  `ViewModelBuilders.cs` owns the pure selected-control-state DTO
  construction.
  `MainViewModel.FlashbackState.cs` owns buffer, bitrate, playback-state,
  in/out marker, gap-from-live UI projection, read-only Flashback playback
  snapshot and segment access, rejection status projection for UI, CLI, and
  MCP callers, scrub, nudge, in/out marker command routing, and
  automation-facing Flashback playback action dispatch.
  `MainViewModel.FlashbackState.cs` owns Flashback UI export commands,
  save-picker flow, active-export guard, user-facing export result/status
  handling, shared export operation lifecycle, progress handoff, stale-result
  classification, current-operation checks, CTS cancellation/disposal cleanup,
  and automation-facing export execution with linked cancellation and dispatcher
  cleanup.
  `MainViewModel.CaptureSelection.cs` owns capture-device selection reactions,
  effective resolution helpers, frame-rate selection reactions, and
  auto-selection entry points. `MainViewModel.CaptureSelection.cs` keeps
  the resolution, frame-rate, selected-format, and video-format rebuild
  compatibility adapters alongside capture-mode transaction state, while
  `Sussudio/Controllers/ViewModel/MainViewModelCaptureModeOptionRebuildController.cs`
  owns the cohesive capture-mode option rebuild transaction, including
  frame-rate option rebuilding, source-rate filtering handoff, auto/source
  option selection, observable frame-rate collection mutation, and selected
  frame-rate application through graph-built context ports.
  `Sussudio/ViewModels/FrameRateTimingPolicy.cs`
  owns pure frame-rate option choice: pending SDR bucket preference,
  Source-rate nearest match with timing-family tie-break, generic auto fallback,
  and previous/manual selection fallback.
  `MainViewModel.cs` owns shared frame-rate selection reset,
  resolved automatic frame-rate application, disabled frame-rate reason
  projection, and capture-mode reset flags.
  `Sussudio/ViewModels/FrameRateTimingPolicy.cs` also owns source-rate filtering
  with capture options always visible. `MainViewModel.CaptureSelection.cs`
  owns deferred rebuild behavior, capture-mode reinitialization serialization,
  and duplicate-reinit suppression.
  `Sussudio/ViewModels/FrameRateTimingPolicy.cs` owns pure frame-rate timing
  family and variant models, rational parsing, friendly/exact frame-rate
  matching, timing-family ranking, and preferred-format ranking helpers used by
  frame-rate, resolution, capture-settings, and automation projections.
  `Sussudio/Controllers/ViewModel/MainViewModelCaptureModeOptionRebuildController.cs`
  owns the stateful resolver that resolves timing variants and source/preferred
  timing from resolution capabilities, runtime snapshots, selected formats,
  source telemetry, UI selection state, and its graph-built context ports.
  `MainViewModel.CaptureSelection.cs` keeps selected-format and video-format
  rebuild compatibility adapters, while
  `Sussudio/Controllers/ViewModel/MainViewModelCaptureModeOptionRebuildController.cs`
  is a top-level `Sussudio.Controllers` owner for selected-format assignment,
  video-format option collection mutation, capture-format request shaping,
  and the capture-mode option rebuild graph-port contract for option
  collections, stable Source/Auto sentinel values, source telemetry,
  resolution/frame-rate selection state, automatic retarget flags,
  format-change suppression, and projected status text.
  `Sussudio/ViewModels/ViewModelSelectionPolicies.cs`
  owns pure selected capture-format choice and mode-tuple video-format filtering.
  `Sussudio/Controllers/ViewModel/MainViewModelCaptureReadinessControllers.cs`
  is a top-level `Sussudio.Controllers` owner for startup FFmpeg capability
  probes for recording formats and split-encode modes through graph-built
  context ports, UI enqueue failure logging, recording-format policy
  application to observable state, source telemetry ingress behavior,
  projection, enum-string caching, summary-age refresh, source-aware
  auto-retargeting hints, and the source telemetry graph-port contract consumed
  by source telemetry ingress and projection.
  `MainViewModel.CaptureSelection.cs`
  owns HDR toggle side effects: recording-time revert/status, mode option
  rebuilds, immediate reinitialize scheduling, and settings persistence.
  `Sussudio/ViewModels/CaptureSettingsProjectionBuilder.cs` also owns pure recording
  codec filtering, selected-codec fallback policy, string-to-model format/quality
  parsing, and custom bitrate clamp policy shared by UI and automation.
  the root `MainViewModel.cs` keeps the public capture-device refresh facade,
  while `Sussudio/Controllers/ViewModel/MainViewModelDeviceDiscoveryControllers.cs`
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
  in `MainViewModel.CaptureSelection.cs`; startup audio-list and
  watcher-driven audio endpoint refresh adaptation live in `MainViewModel.AudioState.cs`.
  `Sussudio/ViewModels/ViewModelSelectionPolicies.cs` owns pure capture-card
  endpoint filtering plus previous/saved/default audio and microphone selection
  fallback policy.
  `Sussudio/Controllers/ViewModel/MainViewModelDeviceDiscoveryControllers.cs`
  is a top-level `Sussudio.Controllers` owner for late device-format probe
  reconciliation, format collection mutation, capability refresh after
  background probes, enqueue/failure logging, and UI-side late-probe retarget
  application.
  It also owns the late-probe reconciliation graph-port contract for UI
  enqueue, device-scan generation, selected-device lookup/state, active capture
  guards, suppress-format-change state, capability rebuild, and retarget
  applier construction. It also owns HDR/SDR reinitialize dispatch, MJPG HFR
  preserve, session mismatch check, active-capture restore behavior, and the
  late-probe retarget graph-port contract for capture-mode
  state, resolution/frame-rate mutation, reinitialize dispatch, runtime
  snapshot checks, frame-rate rebuild, and target-summary refresh.
  `Sussudio/ViewModels/ViewModelSelectionPolicies.cs`
  owns the pure late-probe decision policy for HDR retarget, SDR NV12 retarget,
  MJPG HFR preservation, session mismatch, and active-capture restore.
  `MainViewModel.CaptureSelection.cs` keeps the compatibility adapter for
  resolution option rebuild callers.
  `Sussudio/Controllers/ViewModel/MainViewModelCaptureModeOptionRebuildController.cs`
  owns resolution option rebuilds inside the top-level capture option
  rebuild controller: automatic resolution dropdown option construction,
  automatic resolution-selection adaptation over current ViewModel state,
  auto-resolution state refresh, and resolution dropdown mutation through
  graph-built context ports.
  `MainViewModel.CaptureSelection.cs` owns effective Source resolution state
  and state-backed delegates to the pure selection policy.
  `Sussudio/ViewModels/ViewModelSelectionPolicies.cs` owns automatic resolution
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
  `Sussudio/Controllers/ViewModel/MainViewModelCaptureReadinessControllers.cs`
  also keeps the pure summary builder and auto-resolution predicate ports that
  keep facade-private helpers explicit.
  `Sussudio/ViewModels/ViewModelBuilders.cs`
  owns source telemetry summary, telemetry age, and target-summary display text formatting.
  `MainViewModel.SettingsPersistence.cs` owns settings initialization, simple
  persistence reactions, the impure settings load/save adapter, persisted-settings
  validation, clamping, deferred-selection handoff, save DTO projection,
  load/save projection contracts, validated load-plan application order,
  feature-specific state assignment, and deferred device/audio/microphone
  selection staging.
  `MainViewModel.FlashbackState.cs` owns active Flashback reactions to
  recording-format, encoder quality/preset/split, bitrate, buffer-duration,
  and GPU-decode setting changes.
  `MainViewModel.cs` owns UI-only automation mutators
  for settings visibility, Flashback timeline visibility, show-all capture
  options, stats dock/section visibility, and frame-time overlay display.
  `MainViewModel.cs` owns automation command entry points for
  app audio enablement, audio-preview enablement, preview-volume
  clamp/persist, device-native mode/gain application, and microphone
  enablement with recording-time refusal and idempotent handling.
  `MainViewModelPreviewLifecycleController.cs` owns top-level automation preview
  enable/disable idempotence, pending-reinit cancellation, and start/stop
  routing behind the `MainViewModel.cs` compatibility facade.
  `MainViewModel.CaptureSelection.cs` owns automation HDR and true-HDR
  preview recording-time guard enforcement and availability checks alongside
  HDR mode change side effects.
  `MainViewModel.FlashbackState.cs` owns automation Flashback
  enable/restart routing through the capture session coordinator alongside
  buffer/GPU setting reactions.
  `MainViewModel.cs` owns automation device refresh,
  capture-device selection, audio-input selection, and custom audio-input
  enablement.
  `MainViewModel.cs` keeps the stable public automation
  facade for capture resolution, frame-rate, video-format, MJPEG decoder
  worker-count, recording format, encoder, and output-path settings.
  `Sussudio/Controllers/ViewModel/MainViewModelSettingsAutomationControllers.cs`
  is a top-level `Sussudio.Controllers` owner for UI-thread setting mutations,
  validation, MJPEG decoder clamping, and active capture-mode reinitialization
  routing.
  It also owns the capture-settings automation graph-port contract for option
  collections, selected capture-mode state, preview reinitialization checks,
  UI-thread dispatch, and format-change suppression.
  `MainViewModel.CaptureSelection.cs` owns capture-mode/HDR
  property-change side effects outside the capture-settings automation
  controller.
  `Sussudio/Controllers/ViewModel/MainViewModelSettingsAutomationControllers.cs`
  is a top-level `Sussudio.Controllers` owner for UI-thread setting mutations,
  HDR compatibility enforcement, Flashback cycle suppression, coordinator side
  effects, bitrate clamp policy, encoder preset, and output-path directory
  creation.
  It also owns the recording-settings automation graph-port contract for UI
  dispatch, option collections, suppression flags, selected encoder/output
  state, recording-format coordinator updates, and Flashback encoder setting
  cycles.
  `Sussudio/Controllers/ViewModel/MainViewModelCaptureReadinessControllers.cs`
  is a top-level `Sussudio.Controllers` owner for startup FFmpeg capability
  probes for recording formats and split-encode modes plus observable
  recording-format option rebuilds.
  `Sussudio/ViewModels/MainViewModel.cs` keeps recording-runtime
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
  already have named owners. The thin Flashback XAML-facing adapter methods
  live in `MainWindow.xaml.cs` with construction and controller initialization
  order, while behavior remains in named Flashback controllers. The preview-startup and
  preview-transition adapter family is consolidated in
  `MainWindow.Composition.cs`; start the next UI cleanup from
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
- `MainViewModel.cs` owns the default service graph for the root
  compatibility view model until a fuller app composition root injects feature
  view models and narrower ports.
- `Sussudio/Controllers/ViewModel/MainViewModelControllerGraph.cs` owns
  construction order for the view-model controller graph plus UI-dispatch and
  device-audio, device-refresh, capture-settings automation, source telemetry,
  runtime event-ingress, recording, preview lifecycle/reinitialize, capture
  option rebuild, device-format probe, runtime lifecycle, and disposal graph
  ports. Keep
  service construction in `MainViewModel.cs`, and keep
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
- `tools/Common/AutomationPipeClient/AutomationPipeClient.cs` owns command
  envelope sending, typed `AutomationCommandKind` command-id routing,
  `not_ready` retry behavior, response-state parsing handoff to
  `Sussudio.Automation.Contracts/AutomationPipeProtocol.cs`, named-pipe
  connect orchestration, pipe connect failure classification, exact CLI/MCP
  diagnostic error codes, request/response framing, response timeout,
  command-specific timeout selection for string and typed commands, shared
  response-element validation, synthetic error shaping, and the handoff to
  `Sussudio.Automation.Contracts/AutomationPipeProtocol.cs`.
- Fixed MCP routes whose commands exist in `AutomationCommandKind` should call
  the typed MCP `PipeClient.SendCommandAsync(AutomationCommandKind, ...)`
  overload at the pipe seam; fixed ssctl routes should do the same through
  `PipeTransport.SendCommandAsync(AutomationCommandKind, ...)`, which lives
  with `tools/ssctl/CommandHandlers.cs` so command parsing, payload shaping,
  response exit-code handling, and ssctl-specific pipe policy stay in one
  command-surface file. The shared command transport must keep those enum calls
  typed until the request envelope is created. Do not list converted routes
  here; the shared catalog, per-file MCP owner bullets, and `McpToolSurface.*`
  source guards are the source of truth. String command names remain only for
  catalog/manifest-backed dynamic batches and diagnostic-session command
  callbacks.
- `tools/AutomationClient/Program.cs` owns the low-level pipe client entry
  flow, cancellation handling, shared-protocol command resolution, timeout
  selection, response printing, the local options DTO, flag parsing/help text,
  and JSON/base64/key-value payload construction for scripts and ad hoc
  automation calls. Keep this low-level tool as one cohesive CLI entrypoint.
- `tools/AutomationClient/README.md` owns AutomationClient usage notes.
- `tools/send-automation-command.ps1` owns the PowerShell helper wrapper and
  its AutomationClient rebuild freshness inputs.
- `tools/ssctl/CommandHandlers.cs` owns the complete ssctl command-handler
  surface: top-level CLI routing, the per-invocation command context wrapper,
  shared command sending, response exit-code shaping, usage validation,
  required words, argument joining, flag consumption, optional flag value
  parsing, command-handler JSON detection/pretty-printing, primitive parsing,
  Flashback export numeric validation, on/off and show/hide parsing, recording
  format normalization, snap action mapping, assertion value parsing,
  wait/assert/probe plus recording/file verification scripting flow commands,
  diagnostic and observability commands, presentmon parsing/swap-chain
  discovery/probe invocation, diagnostic-session parsing/runner invocation,
  preview/record/screenshot/frame commands, device commands, set-value capture
  and audio mutations, window and shell visibility commands, recordings-folder
  commands, and Flashback timeline/playback/scrub/marker/range/export payload
  shaping. Fixed ssctl automation routes should call shared enum overloads with
  `AutomationCommandKind` values; labels and wire command IDs remain catalog
  owned. Dynamic diagnostic-session runner command names stay string-based at
  the transport seam. Do not reintroduce `CommandHandlers.*.cs` partial files
  unless a command family becomes an independently tested collaborator with a
  real boundary.
- `tools/NativeXuAudioProbe/Program.cs` owns probe command routing, direct
  AT read/write/input subcommands, the captured audio-switch replay workflow,
  RTK I2C unsafe-native-path probe workflow, service-control smoke/payload
  workflows, supported-device lookup, and
  probe-local runtime shims for linked app service sources;
  `Program.DefaultExperiment.cs` owns the default baseline/experiment/restore
  runner, experiment spec records, shared Native XU command IDs, shared
  raw-payload formatting, analog-gain sequence, default experiment AT
  read/decode/diff/snapshot reporting, and readback/result-diff records;
  `Program.I2cCommands.cs` owns the exploratory `i2c-cmd` command family:
  router, basic get/set/scan paths, selector transport probing,
  high-selector probing, topology/property-set probing, and I2C
  SET/readback/restore verification, plus the legacy `i2c-probe` selector
  scan and raw/AT-wrapped I2C frame experiment; and
  `Program.I2cCommands.cs` also owns I2C-over-AT transport helpers.
- `tools/KsAudioNodeProbe/Program.cs` owns KS audio node probe argument parsing,
  interface selection, open failure handling, workflow dispatch, SetupAPI,
  file-handle, KS property transfer, native interop constants/DTOs, topology
  enumeration, and Win32 formatting helpers;
  `Program.ScanWorkflows.cs` owns set-and-hold, topology, brute-force,
  full-probe orchestration, extended-node mutation tests, ADC volume, mux, and
  mute probe workflows.
- `tools/EgavdsAudioProbe/Program.cs` owns EGAVDS audio probe command flow,
  device lookup, audio input/gain actions, result text, SWIG callback
  registration, EGAVDeviceSupport entry points, SetupAPI entry points, and
  native interface DTOs.
- `tools/ssctl/Program.cs` owns the process entry point, Ctrl-C cancellation,
  CLI option parsing, exit-code shaping, the `ssctl` help facade,
  operator-facing help section text, and catalog-backed CLI help lines.
- `tools/ssctl/CommandHandlers.cs` owns the root command dispatcher, the
  per-invocation command context wrapper, shared command sending, response
  exit-code shaping, usage validation, required words, argument joining, flag
  consumption, optional flag value parsing, command-handler JSON
  detection/pretty-printing, primitive parsing, Flashback export numeric
  validation, on/off and show/hide parsing, recording format normalization,
  snap action mapping, and assertion value parsing.
- `tools/ssctl/Formatters.cs` is the unified console projection facade. It owns
  shared result/JSON helpers, recent diagnostic-event output, standalone
  memory/GC summaries, capture option summaries, device-list output,
  performance timeline response validation, JSON row projection, private row
  model, table output, first-vs-last trend summary text, app snapshot
  orchestration, section ordering, and simple row sections for Sussudio state/
  capture-command summary, audio, capture settings, friendly/exact frame-rate
  summary formatting, runtime video-pipeline text, thread-health section order
  plus source-reader and WASAPI row text, recording, diagnostics, legacy
  performance, process CPU, Memory/GC, thread-pool, capture cadence, low-FPS,
  jitter/drop, MJPEG packet fingerprint, sampled visual cadence, AV-sync drift,
  encoder correction, preview renderer-mode routing, GPU playback summary,
  non-D3D fallback frame/cadence, D3D renderer section text, D3D CPU timing,
  pipeline-latency, frame-latency wait, frame ownership, DXGI frame-stat text,
  slow-frame formatter delegation, source dimensions, source frame-rate
  summary, HDR, source telemetry, Flashback active/failure gating, Flashback
  section and encoding subsection ordering, Flashback encoder/buffer/cache/
  cleanup text, Flashback export/playback text, MJPEG timing activation,
  decode/copy/callback/per-decoder timing, compressed queue/drop/reorder/
  pipeline timing, and preview-jitter snapshot text.
- `tools/McpServer/Tools/AppStateTools.cs` owns the public app-state,
  diagnostic-event, memory/GC/thread-pool, and diagnostic-session MCP entry
  points while preserving the `AppStateTools`, `DiagnosticsTools`,
  `MemoryDiagnosticsTools`, and `DiagnosticSessionTools` tool types.
- `tools/McpServer/Tools/AutomationControlTools.cs` owns the public device,
  capture settings, pipeline settings, structured capture-options, window
  action, full-screen, recordings-folder, UI visibility/settings,
  preview-toggle, recording-toggle, condition-wait, Flashback control, and
  verification MCP entry points while preserving the `CaptureSettingsTools`,
  `DeviceTools`, `CaptureOptionsTools`, `PipelineSettingsTools`,
  `WindowTools`, `UiSettingsTools`, `PreviewTools`, `RecordingTools`,
  `WaitTools`, `FlashbackTools`, and `VerificationTools` tool types.
- `tools/McpServer/Tools/PreviewInspectionTools.cs` owns the public preview
  color, video-source probe, preview-frame capture, and window-screenshot MCP
  entry points while preserving the `PreviewColorProbeTools`,
  `VideoSourceProbeTools`, `PreviewFrameCaptureTools`, and
  `WindowScreenshotTools` tool types.
- `tools/McpServer/Tools/PerformanceTools.cs` owns the public performance MCP
  tool entry points, including timeline command response handling, timeline
  JSON row projection orchestration, root cadence, preview/MJPEG/D3D, Flashback
  playback, Flashback export, and system projection field groups, the private
  row model, timeline table text rendering, first-vs-last trend text, preview
  cadence, visual/MJPEG fingerprint, jitter, D3D, slow-stage, Flashback
  playback, command, failure, cleanup, stage, export trend text, 1%-low target
  summaries, preview, Flashback, and system pressure summaries, plus compact
  cell, command-message, optional-value, preview jitter-depth, D3D bottleneck,
  Flashback stage, cleanup, export, byte-rate formatting, shared summary
  predicates, pressure counters, PresentMon MCP entry points, structured-content
  shape, probe invocation, and app-snapshot request/fallback behavior.
- `tools/McpServer/Tools/FramePacingVerdictTools.cs` owns the public
  `get_frame_pacing_verdict` MCP tool entry point, pipe command orchestration,
  response shaping, performance-timeline projection, snapshot cadence channel
  projection, recent-interval parsing, readiness and verdict policy, private
  row/channel records, and the operator-facing verdict text.
- Shared PresentMon option precedence and preview-present field extraction live
  in `tools/Common/PresentMon/PresentMonProbe.cs`.
- `tools/Common/DiagnosticSessionResult.cs` owns diagnostic session run
  options, sampled snapshot DTOs, shared tool invocation defaults, the ssctl
  usage string, explicit scenario phase input handoff, mutable in-flight phase
  state, immutable scenario phase result handoff consumed by completion, and
  the diagnostic-session summary DTO fields: core metadata, artifact paths,
  terminal state, actions, warnings, end-of-run overview, capture/source,
  Flashback playback/recording/export, preview cadence, preview scheduler, and
  preview D3D result fields.
- `tools/Common/DiagnosticSessionScenarioCatalog.cs` owns scenario name
  constants, MCP-compatible scenario description text, the CLI help-list
  constant, the `Names` projection, normalization, entry lookup, requirement
  queries, export-verification lookup, scenario ordering, and core, Flashback
  playback, Flashback export/lifecycle, Flashback recording/rejection, and
  combined scenario metadata. It also owns the scenario plan DTO, creation
  factory, catalog lookup handoff, and grouped warning/validation policies,
  including the preview-cycle grouped predicate, used by the runner.
- `tools/Common/DiagnosticSessionResultBuilder.cs` owns diagnostic-session
  result phase orchestration, artifact-write handoff, summary-write handoff,
  final summary emission, summary-write failure repair, and final-result
  orchestration from analysis and artifact paths into the named projection set,
  plus final `DiagnosticSessionResult` DTO assignment from the projection set.
  It also owns result artifact path construction, pre-summary sample,
  frame-ledger, and timeline artifact writes, frame-ledger trace shaping,
  shared JSON object creation / artifact serialization helpers, Flashback
  playback projection composition from focused playback projection owners, plus
  the result-build request handoff created by `DiagnosticSessionRunner.cs` and
  consumed by the result builder. Keep `summary.json` field shape stable in the
  builder family.
- `tools/Common/DiagnosticSessionResultBuilder.Projections.cs` owns the
  private projection-set handoff record, projection-set assembly, and the
  result projection records/builders for overview, capture, Flashback
  playback/recording/export, preview cadence/scheduler, preview D3D, and
  visual cadence. Flashback playback projection includes command, cadence,
  1% low, decode, audio-master, and stage DTO value maps consumed by the final
  result initializer.
- `tools/Common/DiagnosticSessionResultBuilder.Analysis.cs` owns
  diagnostic-session metric preparation for validation/result projections,
  analysis warning emission, Flashback playback/export analysis warning text,
  threshold guards, tolerated Flashback scenario warning classification,
  diagnostic-session validation handoff order for Flashback playback, cleanup
  lifecycle restore, preview scheduler analysis, and diagnostic health. It also
  owns cleanup restore warnings after diagnostic sessions stop recording,
  preview, Flashback, or playback state, plus the private analysis handoff
  record, including the single `PreviewScheduler` record property used by
  preview-scheduler result projection. Preview-scheduler analysis includes MJPEG
  jitter-buffer counters, deltas, last drop/underflow reasons, underflow ages,
  max schedule-late aggregation, target-FPS fallback, visual-cadence tolerance
  checks, sparse deadline/drop tolerance selection, and the call into shared
  Flashback preview validation. Diagnostic-health analysis includes health
  summary snapshot selection, health verdict composition, source-reader/ingest
  warning deltas for sparse source-capture tolerance, sparse preview-scheduler
  warning tolerance, tolerated-warning reason selection, and health warning text
  emitted during result construction.
- `tools/Common/DiagnosticSessionRunContext.cs` owns diagnostic-session core mutable run infrastructure:
  bootstrap, scenario normalization, scenario-plan selection, duration/sample
  clamping, session identity, output-directory creation, runner process
  metadata, actions, warnings, samples, terminal exception state, last-stage
  tracking, best-effort artifact write failure recording, command channel,
  best-effort `session-live.json` breadcrumb path, payload shape, health
  projection, warning projection, terminal override mapping, and sampling
  live-state write throttle,
  scenario cancellation source, initial snapshot state, baseline snapshot capture,
  automation response shape helpers for snapshot and verification envelopes,
  unknown-state warning, live-state handoff, run-context disposal, and
  scenario/completion context construction.
- `tools/Common/DiagnosticSessionRunner.cs` owns the public diagnostic-session
  compatibility surface, phase sequencing around context creation, initial
  snapshot capture, named scenario phase invocation and execution, cleanup,
  post-cleanup evidence/result sequence, result-build request mapping,
  post-run performance timeline and final health snapshot fetches, result-build
  invocation, terminal live-state write, and completion context handoff consumed by the post-cleanup completion phase. It also owns scenario sampling, snapshot
  sample collection, post-sampling completion order, fault-drain delegation,
  scenario background task registration, deterministic await order, normal
  registered scenario completion, PresentMon and deferred recording-settings
  task tracking, interrupted task observation, warning collection, and the drain
  result handoff. Preserve sample-loop ordering: append the cloned sample before
  running checkpoint callbacks. Keep the `timeline` and `final-snapshot` stage
  names stable there. It also owns the per-output-directory exclusive lock that
  prevents concurrent diagnostic sessions from writing the same artifact set.
- `tools/Common/DiagnosticSessionScenarioActivation.cs` owns diagnostic-session
  initial setup and optional background startup orchestration: Flashback
  enable/disable for scenario requirements, preview start and video-flow
  readiness wait, recording start and Flashback recording-readiness wait,
  setup/startup result records, Flashback scenario registration delegation,
  deferred Flashback recording-settings task registration, direct Flashback
  playback start command, optional PresentMon launch, correlation snapshot
  capture, and `presentmon.csv` output selection. Keep fixed setup mutations on
  `DiagnosticSessionCommandChannel` typed `AutomationCommandKind` sends and task
  stage names stable there.
- `tools/Common/DiagnosticSessionPostRunActions.cs` owns diagnostic-session
  cleanup and recording-check flow: cleanup ordering, stage/action naming,
  cleanup result handoff, recording stop for verification, Flashback playback
  go-live restore, preview stop, Flashback enable-state restore through typed
  automation commands, deferred Flashback recording-settings restore, last-
  recording or Flashback export verification command selection, payload shape,
  60-second timeout, cloned verification result, skipped-verification action
  text, and Flashback recording validation. Keep the `cleanup-*`,
  `settings-deferred-restore`, `recording-verification`, and
  `recording-validation` stage names stable there.
- `tools/Common/DiagnosticSessionFlashbackCycleScenarios.cs` owns Flashback
  restart/encoder/lifecycle cycle diagnostic task registration, restart-cycle
  playback priming/restart/refill/export verification, encoder-cycle preset
  cycling, snapshot validation, export verification, original-preset restore,
  playback disable/re-enable command flow, post-disable playback-thread/queue
  health checks, and post-re-enable active-state validation.
- `tools/Common/DiagnosticSessionMetrics.cs` owns read-only diagnostic-session
  metric DTOs and projections: source/preview/visual cadence aggregation,
  visual-cadence health classification, D3D metric aggregation, playback
  command-health deltas, and shared counter-delta helpers.
- `tools/Common/DiagnosticSessionFlashbackSupport.cs` owns Flashback diagnostic
  support helpers: rotated-export segment-count parsing, strict export
  verification payload construction, range-selection cleanup, the audio-toggle
  companion used by the range export audio-switch scenario, read-only
  `FlashbackGetSegments` response parsing, completed-segment discovery,
  playable completed-segment target selection, buffered-boundary projection,
  playback headroom polling, read-only snapshot polling waits for preview,
  Flashback, recording, stress-buffer, playback-state, warmed-playback, and
  position convergence, parsed segment DTOs, and Flashback recording, playback,
  and preview scheduler warning policy over already projected metrics. Keep
  state-mutating scenario steps in the scenario owners.
- `tools/Common/DiagnosticSessionFlashbackExportScenarios.cs` owns Flashback
  export diagnostic scenario task registration plus concurrent export, rotated
  export, disable-during-export command coordination, export-during-playback
  choreography, selection-range export orchestration, rejected-export dispatch,
  inactive-buffer failure-kind assertions, and active-Flashback-recording
  backend-stability assertions. Keep the scenario registration, command flows,
  verification, cleanup, and playback command-health checks together in this
  scenario-family owner.
- `tools/Common/DiagnosticSessionFlashbackMetrics.cs` owns Flashback diagnostic
  recording/export, playback-session, and playback-result metric projection. It
  includes the `FlashbackRecordingSessionMetrics`,
  `FlashbackExportSessionMetrics`, `FlashbackPlaybackSessionMetrics`, and
  `FlashbackPlaybackResultMetrics` handoff shapes; read-only recording metric
  projection; export-relevance and snapshot max aggregation; playback snapshot
  observation dispatch; active/relevant snapshot gating; session frame-count
  projection; 1% low window capture; frame/decode/audio-master maxima;
  end-of-session playback counter deltas; final result construction; and the
  grouped command, cadence, decode, audio-master, and stage end-snapshot reads.
  Export metrics include force-rotate fallback total, delta, and last fallback
  segment count; keep those counters derived outside export-observed relevance gating.
- `tools/Common/DiagnosticSessionFlashbackPreviewCycleScenarios.cs` owns
  Flashback preview-cycle diagnostic task registration, priorities, task labels,
  started action strings, normal Flashback preview-cycle stop/restart command
  choreography, pre-stop encoded-frame capture, preview-off Flashback/encoder
  validation, export-while-preview-off verification, playback-under-preview-stop
  validation, recording-backed readiness/counter validation, and restart
  frame-flow validation.
- `tools/Common/DiagnosticSessionFlashbackRecordingSettingsScenarios.cs`
  owns deferred recording-settings preset state, during-recording preset
  mutation, restart/disable rejection-message policy, active-recording
  backend/file/counter stability checks, post-stop preset verification,
  encoder-frame checks, and original-preset restore verification.
- `tools/Common/DiagnosticSessionFlashbackSegmentPlaybackScenarios.cs` owns the
  Flashback completed-segment playback scenario: task registration, target
  acquisition, boundary-crossing playback, go-live restore, snapshot/FPS/
  command-health validation, and recording-assisted segment rotation fallback.
- `tools/Common/DiagnosticSessionFlashbackStressScenario.cs` owns Flashback
  stress thresholds, stress/scrub-stress task registration, main stress and
  scrub-stress command choreography, stress export verification, warmed-playback
  frame/FPS/1% low checks, audio-master fallback delta capture/classification,
  shared command-drain polling, and command-health/latency/final-state warning
  policy.
- `tools/Common/DiagnosticSessionHealthPolicy.cs` owns diagnostic-session health
  observation, severity, Flashback warmup filtering, source/preview/Flashback
  health-observation classifiers, sparse-cadence tolerances, and tolerated
  Flashback warning classification.
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
  and frame-lane text, legacy performance, process CPU, memory, GC, thread-pool
  text, capture cadence, low-FPS, jitter/drop, MJPEG packet fingerprint, sampled
  visual cadence, AV-sync drift and encoder correction text, preview routing,
  source dimensions, source frame-rate summary, HDR, source telemetry text,
  routing to MJPEG/Preview D3D sections, Flashback gate/header/subsection
  ordering, encoding status/health text, export progress/result text, playback
  command text, playback cadence/decode/frame stage/A/V drift text, MJPEG
  timing activation/header/output order, decode/copy/callback/per-decoder
  timing text, compressed queue/drop-reason/reorder/pipeline timing text, MJPEG
  preview-jitter queue/input/output/latency/ownership/underflow text, D3D
  routing/header order, CPU timing, pipeline-latency, frame-latency wait text,
  frame-ownership, DXGI frame-stat text, reusable slow-frame diagnostics,
  diagnostic millisecond formatting, automation response-success detection,
  tolerant JSON string/bool/numeric accessors, and shared byte, number,
  interval, frame-budget, and tick-age display helpers.
- `tools/Common/DiagnosticSessionCommandChannel.cs` owns serialized
  diagnostic-session automation command sending, command failure accounting,
  and `AutomationCommandKind`-to-catalog command-name resolution for fixed
  channel-owned commands, including setup and cleanup lifecycle mutations, raw
  command send overloads, connect-retry wrapping, local failure-response
  fallback when connect retry returns no response, pipe connect retry
  classification, local failure-response envelopes, and fixed wait command
  payload shaping. Keep the underlying runner delegate string-compatible.
- Keep new scenario booleans and grouped derivations with
  `DiagnosticSessionScenarioPlan` in
  `tools/Common/DiagnosticSessionScenarioCatalog.cs` instead of adding string
  comparisons in `DiagnosticSessionRunner`.
- `tools/Common/PresentMon/PresentMonProbe.cs` owns the complete PresentMon
  probe: option/result, summary, swap-chain, app-correlation summary, and metric
  DTOs; public option construction; preview snapshot correlation extraction;
  run orchestration; target process/PresentMon executable/output-path
  resolution; command-line construction; argument quoting; process supervision;
  stdout/stderr drain; timeout kill; temp CSV cleanup; probe-result message
  shaping; result text rendering used by diagnostic-session output surfaces;
  CSV parse overloads; selected-row filtering; summary assembly; swap-chain
  normalization/selection; header/field parsing; scalar metric reads; CSV line
  tokenization; row ingestion; header index construction; schema-presence
  detection; private parsed CSV row shapes; app-present correlation; displayed/
  not-displayed outcome classification; warnings; counted text fields; and
  percentile metric aggregation.

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
  `tools/Common/DiagnosticSessionScenarioCatalog.cs` before wiring scenario
  behavior into `DiagnosticSessionRunner`. Preserve the final order there.
- Keep diagnostic-session grouped policy derivation in
  `DiagnosticSessionScenarioPlan` inside
  `tools/Common/DiagnosticSessionScenarioCatalog.cs`; the runner should consume
  named properties instead of comparing normalized scenario strings directly.

# Architecture Cleanup Plan

Last reviewed: 2026-05-14.

## Objective

Make the repo feel intentionally laid out and safe to change without moving
capture, preview, recording, Flashback, or automation behavior by vibes alone.
Performance and runtime semantics stay primary; file layout changes must earn
their keep with smaller ownership boundaries and passing checks.

## Completed Slices

Automation contracts have been extracted into `Sussudio.Automation.Contracts/`.
This removes the old linked-source arrangement where app and tools compiled
protocol/catalog files from `tools/Common`.

Changed ownership:

- `AutomationCommandKind.cs`
- `AutomationCommandCatalog.cs`
- `AutomationPipeProtocol.cs`
- `AutomationPipeSecurityPolicy.cs`

Diagnostic session scenario names and scenario-level metadata now live in
`tools/Common/DiagnosticSessionScenarios.cs`; the runner still owns execution
flow and summary writing.

Automation diagnostics now have named partial owners instead of one large hub
body. `AutomationDiagnosticsHub.cs` is the compact field/constructor and
counter state owner. `AutomationDiagnosticsHub.Snapshots.cs` owns snapshot
refresh and read-only snapshot access. `AutomationDiagnosticsHub.SnapshotProjection.cs`
owns `AutomationSnapshot` DTO property projection from runtime/view-model
snapshots and diagnostic classifiers. `AutomationDiagnosticsHub.SnapshotState.cs`
owns stateful snapshot bookkeeping for audio mute suspicion and recording file
growth tracking. `AutomationDiagnosticsHub.Timeline.cs`
owns performance-timeline ring reads and append mechanics.
`AutomationDiagnosticsHub.TimelineProjection.cs` owns `AutomationSnapshot` to
`PerformanceTimelineEntry` projection.
`AutomationDiagnosticsHub.SnapshotProjection.SnapshotStatus.cs` owns timestamp,
view-model lifecycle/audio flags, verification-in-progress, session state, and
status-text projection consumed by the automation snapshot DTO.
`AutomationDiagnosticsHub.SnapshotProjection.SnapshotEvaluation.cs` owns
performance score, diagnostic lane, preview pacing classifier, and performance
threshold projection consumed by the automation snapshot DTO.
`AutomationDiagnosticsHub.SnapshotProjection.Audio.cs` composes audio, ingest,
and WASAPI projection owners into the automation snapshot audio/ingest DTO
fields.
`AutomationDiagnosticsHub.SnapshotProjection.AudioSignal.cs` owns view-model
audio peak/clipping and derived signal-present/muted projection consumed by the
automation snapshot DTO.
`AutomationDiagnosticsHub.SnapshotProjection.CaptureIngest.cs` owns capture
audio/video reader, source-reader, and ingest counter projection consumed by the
automation snapshot DTO.
`WasapiAudioCapture.Conversion.cs` owns WASAPI sample decode, f32le 48 kHz
stereo conversion, resampling, and pooled converted packet buffers. Keep
capture-thread lifecycle in `WasapiAudioCapture.cs`.
`WasapiAudioCapture.Fanout.cs` owns recording/Flashback/playback attachment
points and hot writer task-completion enforcement.
`WasapiAudioCapture.Diagnostics.cs` owns audio-level event projection, callback
interval, discontinuity, timestamp-error, glitch, and audio-level event counters.
`WasapiAudioPlayback.Volume.cs` owns render-side volume ramps and output-level
telemetry used by audio ramp traces. Keep WASAPI initialization and render-thread
lifecycle in `WasapiAudioPlayback.cs`.
`WasapiAudioPlayback.Queue.cs` owns playback chunk queue state, pooled-sample
ingress, queue depth/frame accounting, buffered-duration projection, and pooled
chunk returns.
`WasapiAudioPlayback.RenderThread.cs` owns the WASAPI render-thread loop,
pause/resume execution, resume prebuffer wait, endpoint buffer writes, render
buffer filling, and render-side PTS advancement.
`WasapiComInterop.Contracts.cs` owns WASAPI/Core Audio enums, structs, records,
and COM interface declarations. `WasapiComInterop.cs` owns helper methods,
format allocation/parsing, COM activation/release, endpoint volume, and
AudioClient3 initialization.
`NativeXuAudioControlService.Profiles.cs` owns 4K X selector-3 byte indexes,
HDMI/Analog reference payloads, gain-profile placeholders, hex parsing, and
payload decode/confidence helpers. `NativeXuAudioControlService.Transport.cs`
owns selector-3 XU read/modify/write, candidate enumeration, raw payload
normalization/rehydration, and transport gate acquisition/release.
`NativeXuAudioControlService.cs` owns the public service flow and snapshot DTOs.
`AutomationDiagnosticsHub.SnapshotProjection.WasapiAudio.cs` owns WASAPI
capture/playback callback, queue, gap, glitch, and latency projection consumed
by the automation snapshot DTO.
`AutomationDiagnosticsHub.SnapshotProjection.CaptureCommands.cs` owns capture
session command queue counters, latency, last-command, and last-error
projection inputs consumed by the automation snapshot DTO.
`AutomationDiagnosticsHub.SnapshotProjection.CaptureFormat.cs` owns requested,
actual, negotiated, observed, and encoder format projection inputs consumed by
the automation snapshot DTO.
`AutomationDiagnosticsHub.SnapshotProjection.CaptureTransport.cs` owns capture
memory preference, requested/negotiated video subtype, and frame-ledger
projection consumed by the automation snapshot DTO.
`AutomationDiagnosticsHub.SnapshotProjection.CaptureCadence.cs` owns source
capture cadence, preview visual cadence, and center-crop visual cadence
projection inputs consumed by the automation snapshot DTO.
`AutomationDiagnosticsHub.SnapshotProjection.Mjpeg.cs` owns CPU MJPEG totals,
compressed queue, and failure projection inputs consumed by the automation
snapshot DTO.
`AutomationDiagnosticsHub.SnapshotProjection.MjpegTiming.cs` owns CPU MJPEG
decode, interop-copy, callback, reorder, pipeline timing, decoder count, and
per-decoder projection inputs consumed by the automation snapshot DTO.
`AutomationDiagnosticsHub.SnapshotProjection.MjpegPreviewJitter.cs` owns MJPEG
preview jitter queue, timing, drop, underflow, and adaptive-depth projection
inputs consumed by the automation snapshot DTO.
`AutomationDiagnosticsHub.SnapshotProjection.MjpegPacketHash.cs` owns MJPEG
packet duplicate-run / unique-frame projection inputs consumed by the automation
snapshot DTO.
`AutomationDiagnosticsHub.SnapshotProjection.FlashbackExport.cs` owns active
Flashback export progress, failure, force-rotate fallback, and last-result
projection consumed by the automation snapshot DTO.
`AutomationDiagnosticsHub.SnapshotProjection.FlashbackRecording.cs` owns
Flashback recording, buffer, backend, and encoder configuration projection,
including the export verification and codec-downgrade fallback policy consumed
by the automation snapshot DTO.
`AutomationDiagnosticsHub.SnapshotProjection.FlashbackRecordingStartupCache.cs`
owns Flashback temp-drive and startup cache projection consumed by the
automation snapshot DTO.
`AutomationDiagnosticsHub.SnapshotProjection.FlashbackRecordingQueues.cs` owns
Flashback video, GPU, and audio queue/backpressure projection consumed by the
automation snapshot DTO.
`AutomationDiagnosticsHub.SnapshotProjection.FlashbackPlayback.cs` owns
Flashback playback state and frame cadence metrics consumed by the automation
snapshot DTO.
`AutomationDiagnosticsHub.SnapshotProjection.FlashbackPlaybackAudioMaster.cs`
owns Flashback playback audio-master delay/fallback projection consumed by the
automation snapshot DTO.
`AutomationDiagnosticsHub.SnapshotProjection.FlashbackPlaybackDecode.cs` owns
Flashback playback seek-cap and decode timing projection consumed by the
automation snapshot DTO.
`AutomationDiagnosticsHub.SnapshotProjection.FlashbackPlaybackCommands.cs` owns
Flashback playback thread and command queue counter/latency/failure projection
consumed by the automation snapshot DTO.
`AutomationDiagnosticsHub.SnapshotProjection.PreviewD3D.cs` owns D3D preview
swap-chain and renderer state projection consumed by the automation snapshot
DTO.
`AutomationDiagnosticsHub.SnapshotProjection.PreviewD3DCpuTiming.cs` owns D3D
CPU upload/render/present/total-frame timing and pipeline latency projection
consumed by the automation snapshot DTO.
`AutomationDiagnosticsHub.SnapshotProjection.PreviewD3DFrameFlow.cs` owns D3D
submitted/rendered/dropped frame identity, drop reason, and slow-frame
projection consumed by the automation snapshot DTO.
`AutomationDiagnosticsHub.SnapshotProjection.PreviewD3DFrameLatencyWait.cs`
owns D3D waitable frame-latency counter and timing projection consumed by the
automation snapshot DTO.
`AutomationDiagnosticsHub.SnapshotProjection.PreviewD3DFrameStats.cs` owns D3D
frame-statistics success/failure, missed-refresh, and present-count projection
consumed by the automation snapshot DTO.
`AutomationDiagnosticsHub.SnapshotProjection.PreviewRuntime.cs` owns preview
frame counters, GPU playback state, preview HDR state, and preview
color-context projection consumed by the automation snapshot DTO.
`AutomationDiagnosticsHub.SnapshotProjection.PreviewRuntimeCadence.cs` owns
preview display-cadence interval, jitter, slow-frame, and low-FPS projection
consumed by the automation snapshot DTO.
`AutomationDiagnosticsHub.SnapshotProjection.PreviewRuntimeStartup.cs` owns
preview startup/readiness signals, recovery, blank/stall, and renderer-mode
projection consumed by the automation snapshot DTO.
`AutomationDiagnosticsHub.SnapshotProjection.ProcessResources.cs` owns process
memory, CPU, GC, and thread-pool projection consumed by the automation snapshot
DTO.
`AutomationDiagnosticsHub.SnapshotProjection.AvSync.cs` owns live A/V sync
drift and encoder correction projection consumed by the automation snapshot DTO.
`AutomationDiagnosticsHub.SnapshotProjection.RecordingIntegrity.cs` owns
recording-integrity projection inputs consumed by the automation snapshot DTO.
`AutomationDiagnosticsHub.SnapshotProjection.RecordingBackend.cs` owns
recording backend, audio path mode, and mux-result projection consumed by the
automation snapshot DTO.
`AutomationDiagnosticsHub.SnapshotProjection.RecordingPipeline.cs` owns encoder
queue ages, conversion queue depths, and recording video/GPU/CUDA health inputs
consumed by the automation snapshot DTO.
`AutomationDiagnosticsHub.SnapshotProjection.RecordingOutput.cs` owns recording
UI output text, accumulated recording bytes, file-growth state, last finalized
output metadata, and last verification result projection consumed by the
automation snapshot DTO.
`AutomationDiagnosticsHub.SnapshotProjection.SourceSignal.cs` owns detected
source frame-rate fallback, source dimensions/HDR, and raw source signal
metadata projection consumed by the automation snapshot DTO.
`AutomationDiagnosticsHub.SnapshotProjection.SourceTelemetry.cs` owns source
telemetry fallback policy, age calculation, and source-target summary inputs
consumed by the automation snapshot DTO.
`AutomationDiagnosticsHub.SnapshotProjection.UserSettings.cs` owns selected
device, selected capture/recording options, preview volume, and stats
visibility projection consumed by the automation snapshot DTO.
`AutomationDiagnosticsHub.SnapshotProjection.HdrPipeline.cs` owns HDR
availability/request state, runtime/readiness fallback, HDR warmup/downgrade,
pipeline parity, and telemetry-alignment projection consumed by the automation
snapshot DTO.
`AutomationDiagnosticsHub.Alerts.cs` owns alert rule evaluation and active-alert
transitions. `AutomationDiagnosticsHub.SignalAlerts.cs` owns preview, capture,
audio-signal, and recording-growth alert rules.
`AutomationDiagnosticsHub.FlashbackAlerts.cs` owns Flashback alert
orchestration. `AutomationDiagnosticsHub.FlashbackRecordingAlerts.cs` owns
Flashback export, storage, encoder, and recording alert rules.
`AutomationDiagnosticsHub.FlashbackPlaybackAlerts.cs` owns Flashback playback
alert orchestration. `AutomationDiagnosticsHub.FlashbackPlaybackCommandAlerts.cs`
owns playback command queue and command failure alert rules.
`AutomationDiagnosticsHub.FlashbackPlaybackPerformanceAlerts.cs` owns playback
cadence, audio pacing, and submit-failure alert rules.
`AutomationDiagnosticsHub.DiagnosticEvents.cs` owns diagnostics event
publication, event throttling, Flashback export completion events, and recent
event storage.
`AutomationDiagnosticsHub.DiagnosticEvaluation.cs` owns diagnostic verdict
orchestration and the final healthy/mixed fallback.
`AutomationDiagnosticsHub.DiagnosticEvaluationFlashback.cs` owns Flashback-specific
diagnostic verdict ordering and summaries.
`AutomationDiagnosticsHub.DiagnosticEvaluationRealtime.cs` owns idle, warmup,
recording/audio, source/MJPEG, preview, renderer, and present/display
diagnostic verdicts.
`AutomationDiagnosticsHub.DiagnosticEvaluationLanes.cs` owns diagnostic lane text
formatting used by diagnostic verdicts.
`AutomationDiagnosticsHub.Evaluation.cs` owns performance scoring.
`AutomationDiagnosticsHub.EvaluationPolicy.cs` owns shared alert-detail
formatting and health classifiers used by both alerts and diagnostic
evaluation.
`AutomationDiagnosticsHub.Hdr.cs` owns HDR truth classification and preview
HDR/tone-map state projection.
`AutomationDiagnosticsHub.Lifecycle.cs` owns start/stop/dispose and the polling
loop. `AutomationDiagnosticsHub.OutputFiles.cs` owns cached last-output file
existence and size probing. `AutomationDiagnosticsHub.PreviewPacing.cs` owns
automation snapshot input projection for preview pacing stage classification.
`AutomationDiagnosticsHub.ProcessMetrics.cs` owns process CPU, memory, GC, and
thread-pool sampling.
`AutomationDiagnosticsHub.Verification.cs` owns recording/file verification
commands, automatic post-recording verification scheduling, and
recording-start verification reset, and verification-profile adaptation.

Automation command dispatch now keeps the root router focused on the command
envelope: manifest revision checks, auth/readiness gates, trivial-handler
dispatch, and error shaping. `AutomationCommandDispatcher.CustomCommands.cs`
owns custom switch bodies for commands that need multi-field payloads, special
response shapes, diagnostics, or capture/Flashback routing.
`AutomationCommandDispatcher.TrivialHandlers.cs` owns the simple one-property
command table. Named partials own support responsibilities:
`AutomationCommandDispatcher.Authorization.cs` handles auth-token lookup and
constant-time comparison;
`AutomationCommandDispatcher.CommandParsing.cs` handles command metadata,
path-validation forwarding, and enum payload parsing;
`AutomationCommandDispatcher.Responses.cs` handles response shaping and
Flashback rejection diagnostics; `AutomationCommandDispatcher.WindowActions.cs`
handles window automation; `AutomationCommandDispatcher.WaitConditions.cs`
handles wait polling and snapshot predicates; and
`AutomationCommandDispatcher.Assertions.cs` handles AssertSnapshot parsing and
comparison helpers. `AutomationCommandDispatcher.Payload.cs` owns JSON payload
extraction helpers, and `AutomationCommandHandler.cs` owns the reusable
trivial-handler wrapper.

Automation pipe hosting is split across `NamedPipeAutomationServer.*.cs`.
Keep constructor/configuration state in the root file, server start/stop and
accept-loop behavior in `NamedPipeAutomationServer.Lifecycle.cs`, per-connection
JSON framing and dispatch timeouts in `NamedPipeAutomationServer.Connections.cs`,
Windows pipe security/PInvoke in `NamedPipeAutomationServer.Security.cs`, and
error/timeout responses plus fallback tracing in `NamedPipeAutomationServer.Responses.cs`.

`tools/ssctl/CommandHandlers.cs` is now only the top-level CLI router.
`CommandHandlers.Observability.cs` owns state, diagnostics, options, manifest,
timeline, memory, audio-ramp, PresentMon, and diagnostic-session commands.
`CommandHandlers.CaptureControls.cs` owns preview/record/screenshot/frame and
`set` capture/audio/output mutations. `CommandHandlers.DeviceWindow.cs` owns
device, window, and recordings commands. `CommandHandlers.AutomationFlow.cs`
owns wait/assert/probe/stats/settings/frame-time and verification commands.
`CommandHandlers.Flashback.cs` owns Flashback CLI commands. Support partials
remain: `CommandHandlers.Context.cs` owns per-invocation command context,
`CommandHandlers.Parsing.cs` owns CLI flag/value/usage parsing, and
`CommandHandlers.Transport.cs` owns shared command sending plus response
exit-code shaping.

`tools/ssctl/Formatters.cs` is only the projection facade for console output.
Keep app snapshot orchestration in `Formatters.Snapshot.cs`, Flashback snapshot
text in `Formatters.Snapshot.Flashback.cs`, MJPEG timing text in
`Formatters.Snapshot.Mjpeg.cs`, preview renderer text in
`Formatters.Snapshot.Preview.cs`, diagnostic-event text in
`Formatters.Diagnostics.cs`, capture option/device text in `Formatters.Options.cs`,
performance timeline tables in `Formatters.Timeline.cs`, memory/GC summaries in
`Formatters.Memory.cs`, and shared JSON/result helpers in
`Formatters.Common.cs`.

`tools/Common/AutomationSnapshotFormatter.cs` is now the shared automation
snapshot formatter facade for top-level text flow. Tolerant JSON accessors and
byte/interval helpers live in `AutomationSnapshotFormatter.Values.cs`; the
Flashback, MJPEG timing, AV sync, preview/slow-frame diagnostics, and source
sections live in focused formatter partials. Tests that reason about formatter
source use `ReadAutomationSnapshotFormatterSource()` so ownership checks cover
the full partial family.

`tests/Sussudio.Tests/McpToolSurface.DiagnosticSession.Tests.cs` is now only
the diagnostic-session MCP surface index shell. Diagnostic-session coverage is
split into `McpToolSurface.DiagnosticSession.Tool.Tests.cs` for MCP tool
artifact contracts, `McpToolSurface.DiagnosticSession.Ownership.Tests.cs` for
core helper ownership assertions, `McpToolSurface.DiagnosticSession.Flashback.Tests.cs`
for Flashback scenario/metrics/wait/export ownership assertions, and
`McpToolSurface.DiagnosticSession.Runner.Tests.cs` as a marker shell for
focused reflective runner behavior tests. The runner behavior files now own
final-snapshot artifact failures, sparse source-cadence health tolerance,
Flashback export/playback command flow, unknown-initial-snapshot mutation
safety, synthetic pipe-connect retry, and concurrent-output-directory lockout.

`tests/Sussudio.Tests/Flashback.Tests.cs` is now only the shared helper shell.
Flashback regression coverage is split into buffer, encoder-sink, exporter
basic, exporter segment/range, exporter output-file, playback state, playback
thread, playback command-queue, playback cadence, decoder, and support partial
files. Buffer coverage is further split into option/init contracts, shared
helpers, source-ownership assertions, segment/query behavior, retention/cache
behavior, and validation owners. Playback command-queue coverage is further
split into capacity/drop policy, scrub coalescing, and seek-slot barrier
owners. Keep new Flashback tests in the closest owner file instead of
regrowing the root helper shell.

`tests/Sussudio.Tests/MainViewModel.Automation.Tests.cs` is now only the
automation view-model surface and shared reflection-helper shell. Automation
view-model regression coverage is split into diagnostics refresh, diagnostics
projection ownership, runtime-safety behavior, and Flashback cleanup ownership
partials. Keep new automation tests in the closest owner file instead of
regrowing the root catch-all.

`tests/Sussudio.Tests/MainViewModel.Capture.Tests.cs` is now only the
capture-facing MainViewModel surface and shared source-inspection helper shell.
Capture regression coverage is split into preview startup, Flashback export
locking, Flashback coordinator/UI routing, Flashback backend lifecycle, and
Flashback frame-rate/enable-disable owner files.

`tests/Sussudio.Tests/SnapshotModels.Tests.cs` is now the shared snapshot-model
reflection/spec helper shell. Snapshot model contract coverage is split into
CaptureDiagnosticsSnapshot, CaptureHealthSnapshot, and
SourceSignalTelemetrySnapshot owner files.

`tests/Sussudio.Tests/RecordingQueue.Tests.cs` is now the shared recording
queue source-reader helper shell. Recording queue coverage is split into queue
overload policy, LibAv sink, WASAPI, and capture fan-out / Flashback backend
owner files.

`tests/Sussudio.Tests/D3D11PreviewRenderer.Tests.cs` is now only the
preview-renderer test family marker shell. D3D preview renderer coverage is
split into geometry/screenshot helper contracts, cadence contracts, the large
diagnostics contract, source ownership assertions, device-lost behavior, and
frame-flow/shared-device assertions.

`tests/Sussudio.Tests/AutomationToolContracts.Tests.cs` now keeps only shared
reflection helpers. Automation tool contract coverage is split into protocol
and pipe-failure contracts, catalog/manifest/path-policy contracts,
reliability-gates script checks, shared/ssctl snapshot formatter contracts, and
PresentMon parser contracts.
Shared tool assembly loading and stale-build detection now live in
`tests/Sussudio.Tests/ToolAssemblyLoading.Helpers.cs` so the legacy harness body
no longer owns tool DLL resolution or freshness policy.
Shared repo-file reads, reflection/property access, assertion helpers, wait
helpers, and synthetic capture/recording object factories now live in
`tests/Sussudio.Tests/HarnessCore.Helpers.cs` instead of the legacy harness
body.
Synthetic MJPEG timing metric factories and the closed-pipeline emit delegate
now live in `tests/Sussudio.Tests/MjpegTimingMetrics.Helpers.cs`.

`tests/Sussudio.Tests/CaptureConfigurationModels.Tests.cs` now keeps only
shared reflection helpers. Capture configuration model coverage is split into
capture mode options, capture settings/MJPEG HFR policy, encoder support,
Flashback DTO contracts, and recording pipeline option contracts.

`tests/Sussudio.Tests/PooledVideoFrame.Tests.cs` now keeps only shared
pooled-frame and jitter-buffer helpers. Pooled-frame coverage is split into
lease lifecycle/fan-out contracts, MJPEG jitter adaptive policy, MJPEG jitter
queue/drop/reprime behavior, and queued lease release contracts for D3D,
recording, and Flashback paths.

`tests/Sussudio.Tests/MainViewModel.Automation.DiagnosticsProjection.Tests.cs`
is now only the automation diagnostics projection test family marker shell.
Projection ownership checks are split into snapshot/status, audio, capture and
source, MJPEG, recording, system resources and A/V sync, preview, and Flashback
owner files.

Fullscreen transition mechanics now live under the
`Sussudio/Controllers/FullScreenController.*.cs` family. Keep the root controller
to the public toggle/state surface, `FullScreenController.Transitions.cs` to
enter/exit orchestration, `FullScreenController.Animation.cs` to rect animation,
`FullScreenController.Chrome.cs` to chrome/material state, and
`FullScreenController.Controls.cs` to overlay pointer/auto-hide behavior.
`MainWindow.FullScreen.cs` remains the XAML event adapter and Flashback
keyboard/scrub bridge.

Automation whole-window screenshot capture now lives in
`Sussudio/Controllers/WindowScreenshotController.cs`. `MainWindow.Screenshot.cs`
is only the automation adapter.

Preview-frame screenshot button behavior now lives in
`Sussudio/Controllers/PreviewScreenshotController.cs`.
`MainWindow.PreviewScreenshot.cs` is the XAML-facing adapter for output
directory fallback, file naming, preview-frame capture, status text, logging,
and button enable/disable state.
Renderer-level preview frame capture request state and timeout/cancellation
handling now live with the capture implementation in
`Sussudio/Services/Preview/D3D11PreviewRenderer.ScreenshotCapture.cs`.
Screenshot BMP/error result construction, mapped-frame buffer copying, and
capture pixel statistics now live in
`Sussudio/Services/Preview/D3D11PreviewRenderer.ScreenshotEncoding.cs`.

Window geometry automation and the recordings-folder command now live in
`Sussudio/Controllers/WindowAutomationController.cs`.
`MainWindow.WindowAutomation.cs` is the `IAutomationWindowControl` adapter.
Recording-aware close behavior remains in `MainWindow.CloseLifecycle.cs`.

UI-thread dispatching helpers and guarded async event-handler execution now
live in `Sussudio/MainWindow.Dispatching.cs`. Window close completion and
recording-aware close handling remain in `MainWindow.CloseLifecycle.cs`.

`tests/Sussudio.Tests/McpToolSurface.CommandRouting.Tests.cs` is now only the
MCP command-routing test family marker shell. MCP command-routing coverage is
split into capture, host/pipe, recording, formatter batching, device, pipeline,
UI, and verification owner files.

First-load startup, first-frame uncloaking, initial ViewModel/device refresh,
automation pipe hosting, and the launch entrance trigger now live in
`Sussudio/MainWindow.Startup.cs`. Window close completion and recording-aware
close handling remain in `MainWindow.CloseLifecycle.cs`.

Top-level shell resize telemetry for preview compositor transforms now lives in
`Sussudio/MainWindow.WindowSizing.cs`. Preview surface sizing, GPU panel
visibility, and video/control-bar composition shadows now live in
`Sussudio/MainWindow.PreviewSurface.cs`. `MainWindow.PreviewRenderer.cs` keeps
preview renderer instances, frame counters, expected-present interval, and
renderer cadence state.
`Sussudio/MainWindow.PreviewRuntimeSnapshot.cs` owns the UI-thread automation
preview snapshot provider and read-only preview runtime snapshot construction.
Close/finalize handling remains in
`MainWindow.CloseLifecycle.cs`.

Window title base/build-stamp formatting and the recording-time suffix now live
in `Sussudio/MainWindow.WindowTitle.cs`.

Window close lifecycle and native window helpers are now explicit:
`Sussudio/MainWindow.CloseLifecycle.cs` owns `AppWindow.Closing`, automation
close completion, and recording-aware pre-close protection. `MainWindow.ShutdownCleanup.cs`
owns `Closed` shutdown cleanup: timer stops, event detaches, preview shutdown,
automation diagnostics disposal, NVML disposal, and ViewModel disposal.
`Sussudio/MainWindow.NativeWindow.cs` owns native `AppWindow` lookup and DWM
cloak/dark-mode helpers.

Audio and microphone meter rendering now lives in
`Sussudio/Controllers/AudioMeterController.cs`. Audio/microphone initial control
projection and event hookup now live in `Sussudio/MainWindow.AudioBindings.cs`;
video-format collection setup, decoder-count seeding, initial capture/recording
option projection, and code-attached resolution/frame-rate handlers now live in
`Sussudio/MainWindow.CaptureOptionBindings.cs`.
The remaining non-audio control-bar binding code stays in `MainWindow.Bindings.cs`.

Capture session transition legality now lives in
`Sussudio/Models/Capture/CaptureSessionTransitionPolicy.cs`. `CaptureService`
uses it before entering a transition and delegates steady-state resolution to
the same pure policy; resource ownership has not moved in this slice.
Capture session coordinator command enums, queue receipt records, session
snapshots, and Flashback playback/buffer status projections now live in
`Sussudio/Services/Capture/CaptureSessionCoordinator.Models.cs`.
`CaptureSessionCoordinator.cs` owns construction, shared state fields, and
public lifecycle/audio mutation routing. Queue work item creation, command
enqueueing, worker-loop execution, coalescing, cancellation/failure accounting,
and pending-command counters now live in `CaptureSessionCoordinator.Queue.cs`.
Queue/session snapshot projection, last-command state, pending-command age
bookkeeping, and queue latency accounting now live in
`CaptureSessionCoordinator.Snapshot.cs`. Dispose/drain/cancel lifecycle for the
worker queue and cancellation token source now lives in
`CaptureSessionCoordinator.Disposal.cs`.
`tests/Sussudio.Tests/CaptureSessionCoordinator.Tests.cs` is now only the
coordinator test-family marker shell. API/command/snapshot contracts, focused
source-ownership contracts, queue behavior, Flashback/cancellation behavior,
transition policy, and shared reflection harness helpers now live in separate
named files beside it.
Queued Flashback mutations, read-only Flashback status/projection helpers,
export forwarding, and active playback-controller readiness checks now live in
`Sussudio/Services/Capture/CaptureSessionCoordinator.Flashback.cs`.

Device discovery ownership is split across `DeviceService.*.cs`. Keep root
enumeration orchestration in `DeviceService.cs`, format cache serialization in
`DeviceService.FormatCache.cs`, inline/background format probing in
`DeviceService.FormatProbe.cs`, device priority/capability scoring in
`DeviceService.Scoring.cs`, audio endpoint association in
`DeviceService.AudioAssociation.cs`, and native XU interface path resolution in
`DeviceService.NativeXu.cs`.

Native XU Kernel Streaming calls are split across `KsExtensionUnitNative.*.cs`.
Keep constants and DTOs in the root, SetupAPI interface enumeration in
`.Interfaces.cs`, handle opening in `.Handles.cs`, topology node parsing in
`.Topology.cs`, XU GET/SET transfer shapes in `.Transfers.cs`, and P/Invoke
struct declarations in `.Interop.cs`. `tools/NativeXuAudioProbe` links this
whole partial family explicitly, so update its project file with every new
partial.

Native device enumeration ownership is split across `MfDeviceEnumerator.*.cs`.
Keep shared Media Foundation constants, GUIDs, and P/Invoke declarations in
`MfDeviceEnumerator.cs`, MF video-device enumeration in
`MfDeviceEnumerator.VideoDevices.cs`, WASAPI capture endpoint enumeration and
friendly-name reads in `MfDeviceEnumerator.AudioEndpoints.cs`, and native video
format probing/source fallback/subtype naming in
`MfDeviceEnumerator.FormatProbe.cs`.

Capture service source telemetry and observed pixel-format accounting now live
in `Sussudio/Services/Capture/CaptureService.Telemetry.cs`. The root capture
service owns shared state, construction, initialization, and public event
surface, but telemetry polling, fallback merging, NTSC frame-rate correction,
and pixel-format counters are no longer embedded in the lifecycle/orchestration
file.

Capture audio preview and live input switching now live in
`Sussudio/Services/Capture/CaptureService.Audio.cs`. Preview-time microphone
monitoring lives in
`Sussudio/Services/Capture/CaptureService.MicrophoneMonitor.cs`, and WASAPI
playback attach/detach ordering lives in
`Sussudio/Services/Capture/CaptureService.WasapiPlayback.cs`. These files
preserve the root service transition lock while keeping mic cleanup and
playback routing from collapsing back into the general audio partial.

Explicit capture cleanup now lives in
`Sussudio/Services/Capture/CaptureService.Cleanup.cs`. That file owns the
public cleanup transition, shutdown teardown order, failed Flashback recording
segment preservation, deferred LibAv/unified-video cleanup handoff, WASAPI
capture disposal, mic teardown, telemetry stop, and final session-state reset.

Capture transition coordination and disposal now live in
`Sussudio/Services/Capture/CaptureService.Coordination.cs`. That file owns
`RunTransitionAsync`, steady-state resolution, initialization/disposal guards,
async disposal cleanup, and best-effort semaphore/eviction cleanup helpers used
by the other capture-service partials.

Deferred capture cleanup now lives in
`Sussudio/Services/Capture/CaptureService.DeferredCleanup.cs`. That file owns
Flashback backend/export lock release helpers, deferred Flashback artifact
cleanup after encoder/export drains, deferred unified-video cleanup after LibAv
drains, and the pending LibAv drain reentry guard.

Capture read-only automation probes now live in
`Sussudio/Services/Capture/CaptureService.Probes.cs`. Video source probing,
preview color probing, and preview-frame screenshot waits are separated from
runtime lifecycle mutation code.

Fatal capture and backend failure handling now lives in
`Sussudio/Services/Capture/CaptureService.Failures.cs`. That file owns fatal
error callbacks, last-failure telemetry, GPU device-lost classification, and
the async cleanup launchers that move the service into faulted states.

Flashback-facing capture controls now live in
`Sussudio/Services/Capture/CaptureService.FlashbackControls.cs`. That file owns
public Flashback state, segment access, enable/settings mutations, restarts,
recording-format changes, and encoder-setting cycles while backend resource
construction stays in the Flashback orchestration partial.

Flashback recording policy and session-context helpers now live in
`Sussudio/Services/Capture/CaptureService.FlashbackRecording.cs`. That file owns
Flashback backend ownership checks, audio attach, session-context construction,
frame-rate rational inference, codec/HDR guardrails, encoded-frame forwarding,
and recording topology validation.

Recording start/stop lifecycle now lives in
`Sussudio/Services/Capture/CaptureService.RecordingLifecycle.cs`. That file owns
the public recording transition surface, Flashback recording fast-path reuse,
standard LibAv recording startup, start-rollback ordering, and the emergency
stop overload that feeds finalization.

Transient recording-start rollback cleanup now lives in
`Sussudio/Services/Capture/CaptureService.RecordingRollback.cs`. That file owns
best-effort teardown for partially started sinks, WASAPI capture, unified-video
capture, and deferred LibAv drain cleanup after a failed recording start.

Flashback export failure classification now lives in
`Sussudio/Services/Capture/CaptureService.FlashbackExportFailureClassification.cs`.
Keep the export failure-kind taxonomy there because automation responses and
capture diagnostics both consume it.

Flashback export entry points and the core export flow now live in
`Sussudio/Services/Capture/CaptureService.FlashbackExportOperations.cs`.
Keep range export, last-N export, backend lease handoff, native export
dispatch, and export cleanup ordering there.

Flashback export planning now lives in
`Sussudio/Services/Capture/CaptureService.FlashbackExportPlanning.cs`. Keep
segment metadata mapping, live-export throttle policy, buffer range clamps, and
PTS offset helpers there so the export operation partial stays focused on
orchestration.

Flashback export diagnostics now lives in
`Sussudio/Services/Capture/CaptureService.FlashbackExportDiagnostics.cs`.
Keep export attempt state, progress forwarding, rejection records,
force-rotate fallback counters, and completion status projection there.

Preview sink and MJPEG timing handoff now lives in
`Sussudio/Services/Capture/CaptureService.PreviewPipeline.cs`. That file owns
preview-frame sink attachment, late Flashback playback preview wiring, shared
D3D preview-device handoff, negotiated video getters, and cached MJPEG pipeline
timing details.

Preview start/stop lifecycle now lives in
`Sussudio/Services/Capture/CaptureService.PreviewLifecycle.cs`. That file owns
video-preview start/stop transitions, retained-backend reuse checks, Flashback
backend reuse policy, preview-start rollback, and preview pipeline disposal
ordering.

Recording integrity policy is now split under
`Sussudio/Services/Capture/CaptureService.RecordingIntegrity*.cs`. The root
partial resolves the active backend, `.Models.cs` owns the private counter DTOs,
`.Summary.cs` owns integrity status/reason classification, `.Counters.cs` owns
video/backend counter capture and baseline deltas, `.Audio.cs` owns audio
counter capture and baseline deltas, and `.Logging.cs` owns the structured
`RECORDING_INTEGRITY` log line. Snapshot partials consume that policy instead
of containing it.

LibAv encoder codec and options policy now lives in
`Sussudio/Services/Recording/LibAvEncoder.CodecPolicy.cs`. Keep option
validation, bitstream-filter selection, NVENC preset/split-encode mapping,
frame-size math, sample-format support, and rational conversion helpers there;
leave live send/drain/finalize paths in the owner partials.

LibAv encoder A/V sync diagnostics now live in
`Sussudio/Services/Recording/LibAvEncoder.AvSync.cs`. Keep drift-correction
thresholds, sync counters, current-drift reporting, and sync warning logs there
so the root encoder stays focused on initialization, lifecycle, and teardown.

LibAv encoder packet writing now lives in
`Sussudio/Services/Recording/LibAvEncoder.PacketWriting.cs`. Keep video encoder
packet drains, bitstream-filter packet drains, timestamp rescaling, packet
stream-index assignment, packet write accounting, and interleaved video packet
writes there.

LibAv encoder packed software-frame copy helpers now live in
`Sussudio/Services/Recording/LibAvEncoder.FrameCopy.cs`. Keep packed NV12/P010
plane sizing, source-buffer validation, and stride-aware plane copies there.

LibAv encoder diagnostics and error helpers now live in
`Sussudio/Services/Recording/LibAvEncoder.Diagnostics.cs`. Keep open-state
guards, FFmpeg error string conversion, structured libav exceptions, and
D3D11 device-removed checks there.

LibAv encoder audio stream handling now lives in
`Sussudio/Services/Recording/LibAvEncoder.Audio.cs`. Keep audio/microphone
stream state, public status properties, packet writing, pending-sample flush,
and sample queue/drain helpers there; leave encoder initialization, rotation,
and finalization in `LibAvEncoder.cs`.

LibAv encoder audio submission now lives in
`Sussudio/Services/Recording/LibAvEncoder.AudioSubmission.cs`. Keep the public
audio/microphone sample entry points, payload alignment checks, accumulator
handoff, and stream-chunk submission there.

LibAv encoder audio stream initialization now lives in
`Sussudio/Services/Recording/LibAvEncoder.AudioInitialization.cs`. Keep audio
and microphone AAC stream creation, codec opening, stream time-base setup,
resampler/frame/buffer setup calls, and microphone-specific setup there.

LibAv encoder audio setup helpers now live in
`Sussudio/Services/Recording/LibAvEncoder.AudioSetup.cs`. Keep AAC codec
context configuration, resampler setup, audio frame allocation, accumulator
allocation, and sample-queue allocation there.

LibAv encoder HDR frame side-data helpers now live in
`Sussudio/Services/Recording/LibAvEncoder.HdrSideData.cs`. Keep software-frame
and hardware-frame HDR mastering display and content-light metadata attachment
there.

LibAv encoder models now live in
`Sussudio/Services/Recording/LibAvEncoder.Models.cs`. Keep `LibAvEncoderOptions`
and `RotateOutputResult` there so the root encoder remains runtime behavior
rather than DTO storage.

LibAv encoder video setup now lives in
`Sussudio/Services/Recording/LibAvEncoder.VideoSetup.cs`. Keep video codec
context configuration, NVENC private option application, D3D11/CUDA hardware
frames setup, texture-pool creation, and video bitstream-filter initialization
there; leave rotation and finalization in `LibAvEncoder.cs`.

LibAv encoder video submission now lives in
`Sussudio/Services/Recording/LibAvEncoder.VideoSubmission.cs`. Keep CPU packed
frame submission, D3D11 texture submission, CUDA frame submission, forced
keyframe handling, per-frame HDR side-data attachment/removal, and video packet
drains there.

LibAv encoder output lifecycle now lives in
`Sussudio/Services/Recording/LibAvEncoder.OutputLifecycle.cs`. Keep rotation IO
close/reopen, stream reinitialization, MP4 muxer option application, segment
runtime resets, and native cleanup/freeing there; keep generic error helpers in
`LibAvEncoder.Diagnostics.cs`.

LibAv recording sink queue ownership now lives in
`Sussudio/Services/Recording/LibAvRecordingSink.Queues.cs`. Keep public
video/GPU/CUDA enqueue entry points, video/GPU/CUDA queue-depth accounting,
failure signaling, video packet records, and pooled video buffer return helpers
there. Hot audio/microphone WASAPI write adapters, audio queue eviction,
audio remaining-buffer cleanup, and `AudioSamplePacket` now live in
`LibAvRecordingSink.AudioQueues.cs`. Keep start/stop lifecycle in
`LibAvRecordingSink.cs`, read-only telemetry and encoder drift accessors in
`LibAvRecordingSink.Diagnostics.cs`, dispose/deferred cleanup in
`LibAvRecordingSink.Lifetime.cs`, encoder option creation in
`LibAvRecordingSink.Options.cs`, and stopped-output validation in
`LibAvRecordingSink.OutputValidation.cs`.

LibAv recording sink encode-drain ownership now lives in
`Sussudio/Services/Recording/LibAvRecordingSink.EncodingLoop.cs`. Keep the
background encoding loop, bounded video/GPU/CUDA/audio/microphone drain
batches, cancellation cleanup, frame-encoded event dispatch, and fatal encoder
failure handling there.

Recording verifier ownership is split across focused partials. Keep strict
verification orchestration in `Sussudio/Services/Recording/RecordingVerifier.cs`,
ffprobe process/spec/side-data probing in `RecordingVerifier.Ffprobe.cs`, probe
scalar parsing in `RecordingVerifier.ProbeParsing.cs`, stream/container/HDR and
cadence validation policy in `RecordingVerifier.Validation.cs`, result/taxonomy
shaping in `RecordingVerifier.Results.cs`, and ffprobe frame timestamp cadence
analysis in `RecordingVerifier.Cadence.cs`.
`tests/Sussudio.Tests/RecordingVerifier.Integration.Tests.cs` now keeps only
shared fake process-supervisor, runtime snapshot, verifier construction, and
verification invocation helpers. Recording verifier integration scenarios are
split into ffprobe failure, process-priority, codec, Flashback verification
format, mismatch, HDR, and cadence owner files.

Native XU source telemetry detail presentation now lives in
`Sussudio/Services/Telemetry/NativeXuAtCommandProvider.TelemetryDetails.cs`.
Keep telemetry detail rows, display formatting, HDR transfer labels, and
flash-audio source interpretation there so the root provider stays focused on
transport gating, rolling command polling, and snapshot construction.

Native XU diagnostic summary strings now live in
`Sussudio/Services/Telemetry/NativeXuAtCommandProvider.DiagnosticSummary.cs`.
Keep the `nativexu:` token contract and extended AT result field formatting in
that file.

Native XU public device commands now live in
`Sussudio/Services/Telemetry/NativeXuAtCommandProvider.DeviceCommands.cs`.
Keep generic AT SET/GET wrappers, named SET wrappers, and probe-facing raw AT
reads there. The root provider remains responsible for selected-interface
validation and dispatch into telemetry polling.

Native XU audio command sequences now live in
`Sussudio/Services/Telemetry/NativeXuAtCommandProvider.AudioCommands.cs`. Keep
HDMI/Analog switching, analog gain writes, gain-register mapping, selector-4
I2C payload writes, and flash persistence sequencing there.

Native XU reference full-snapshot reads now live in
`Sussudio/Services/Telemetry/NativeXuAtCommandProvider.FullSnapshot.cs`. Keep
the legacy all-command source snapshot path there; the root provider owns
selected-interface validation and dispatch into the active rolling poll path.

Native XU active rolling polling now lives in
`Sussudio/Services/Telemetry/NativeXuAtCommandProvider.RollingPoll.cs`. Keep
rolling command groups, cached AT-command fields, VIC/frame-rate lookup, and
active snapshot assembly there.

Native XU payload decoding now lives in
`Sussudio/Services/Telemetry/NativeXuAtCommandProvider.PayloadDecoding.cs`.
Keep AVI InfoFrame decoding, HDR metadata decoding, scalar/ascii payload reads,
frame-rate rational inference, confidence scoring, and boolean token helpers
there.

Flashback encoder sink startup now lives in
`Sussudio/Services/Flashback/FlashbackEncoderSink.Startup.cs`. Keep session
validation, buffer session creation, encoder initialization, queue creation,
background task startup, and startup rollback there.

Flashback encoder sink options and packet helpers now live in
`Sussudio/Services/Flashback/FlashbackEncoderSink.Options.cs`. Keep
recording-context mapping, encoder option creation, segment extension policy,
packet records, and buffer/COM release helpers there so
`FlashbackEncoderSink.cs` stays focused on construction, core state, and small
shared helpers.

Flashback encoder queue helpers now live in
`Sussudio/Services/Flashback/FlashbackEncoderSink.Queues.cs`. Keep queue
completion/signaling, queue-depth accounting, enqueue rejection guards/logging,
hot audio packet enqueue, and queued-buffer cleanup there.

Flashback encoder loop orchestration now lives in
`Sussudio/Services/Flashback/FlashbackEncoderSink.EncodingLoop.cs`. Keep the
background encode loop, drain ordering, force-rotate drain orchestration,
cancellation handling, fatal cleanup, and final segment registration there.

Flashback encoder packet drains now live in
`Sussudio/Services/Flashback/FlashbackEncoderSink.PacketDrain.cs`. Keep bounded
video/GPU/audio/microphone packet drains, encoder PTS resolution, latest-PTS and
disk-byte refresh, and frame-encoded event dispatch there.

Flashback encoder rolling segment rotation now lives in
`Sussudio/Services/Flashback/FlashbackEncoderSink.SegmentRotation.cs`. Keep
active-segment completion/registration, disk-byte refresh after rotation, and
rotation-failure recovery there.

Flashback encoder export force-rotation now lives in
`Sussudio/Services/Flashback/FlashbackEncoderSink.ForceRotate.cs`. Keep
`ForceRotateForExport`, request timeout/cancellation handling, pending-request
cleanup, and force-rotate drain abort classification there.

Flashback encoder producer entry points now live in
`Sussudio/Services/Flashback/FlashbackEncoderSink.Inputs.cs`. Keep raw/lease/GPU
video enqueue entry points, audio/microphone enqueue entry points, force-rotate
input rejection guards, and hot WASAPI writer adapters there.

Flashback encoder stop/dispose ownership now lives in
`Sussudio/Services/Flashback/FlashbackEncoderSink.Lifetime.cs`. Keep `StopAsync`,
`Dispose`/`DisposeAsync`, deferred cleanup, cancellation/disposal helpers, and
stop-drain timeout classification there.

Flashback encoder retroactive recording lifecycle now lives in
`Sussudio/Services/Flashback/FlashbackEncoderSink.Recording.cs`. Keep the
`IRecordingSink.StartAsync` adapter, `CanBeginRecording`, recording begin/cancel/end,
recording PTS boundaries, and recording eviction-pause handshake there.

Flashback encoder public runtime state now lives in
`Sussudio/Services/Flashback/FlashbackEncoderSink.RuntimeState.cs`. Keep public
counters, queue-depth/status projections, encoder format summaries,
fatal-error callback registration, and the frame-encoded event surface there.

Flashback decoder audio output now lives in
`Sussudio/Services/Flashback/FlashbackDecoder.AudioOutput.cs`. Keep audio packet
delivery, audio codec/resampler initialization, audio callback failure handling,
resampler output conversion, and bounded audio sample/byte sizing there. D3D11VA decoder selection and hardware
configuration diagnostics now live in
`Sussudio/Services/Flashback/FlashbackDecoder.D3D11.cs`. Decoded video frame
output, D3D11/software frame validation, plane copies, and YUV-to-NV12/P010
conversion now live in
`Sussudio/Services/Flashback/FlashbackDecoder.VideoOutput.cs`. Keep file
open/close, decode control flow, and native cleanup in the root decoder until
those areas get their own focused slices. Keyframe/exact seek control flow,
pending-frame transfer, seek-cap diagnostics, and seek-buffer flushing now live
in `Sussudio/Services/Flashback/FlashbackDecoder.Seeking.cs`. Shared PTS
conversion, seek timestamp conversion, best-effort frame timestamp selection,
and recoverable seek log suppression now live in
`Sussudio/Services/Flashback/FlashbackDecoder.Timestamps.cs`.
Decoded frame-size calculation, video-dimension validation, input stream-count
bounds, and stream-index bounds now live in
`Sussudio/Services/Flashback/FlashbackDecoder.Validation.cs`.
File-close native cleanup, software buffer returns, pending held-frame release,
decoder state reset, and held-frame best-effort release helpers now live in
`Sussudio/Services/Flashback/FlashbackDecoder.Lifetime.cs`.
Decode phase timing accumulation and FFmpeg decoder error formatting now live in
`Sussudio/Services/Flashback/FlashbackDecoder.Diagnostics.cs`. Open/disposed
state guards now live in
`Sussudio/Services/Flashback/FlashbackDecoder.Guards.cs`.
Decoded video/audio output DTOs now live in
`Sussudio/Services/Flashback/FlashbackDecoder.OutputTypes.cs` so the root
decoder file ends at its control-flow owner instead of carrying trailing model
types.
Video codec setup, D3D11VA/software fallback selection, frame-rate metadata
initialization, MJPEG single-thread decode policy, and software output-buffer
allocation now live in
`Sussudio/Services/Flashback/FlashbackDecoder.VideoSetup.cs`.

Flashback buffer retention now lives in
`Sussudio/Services/Flashback/FlashbackBufferManager.Retention.cs`. Keep segment
purge, eviction, guarded file deletion, disk-warning state, and recording
start/end retention boundaries there. The root buffer manager keeps session
live counters, byte/PTS accounting helpers, and PTS/disk updates.
Flashback buffer segment mutation now lives in
`Sussudio/Services/Flashback/FlashbackBufferManager.SegmentMutation.cs`. Keep
active segment path generation, active segment start/abandonment, completion
registration, and same-path segment extension there.
Flashback buffer initialization, segment-extension setup, recovery-preserve
markers, disposal, and disposed-state guards now live in
`Sussudio/Services/Flashback/FlashbackBufferManager.Lifecycle.cs`.
Flashback buffer segment counts, active-path projection, segment file lookup,
start-PTS lookup, and segment-info projection now live in
`Sussudio/Services/Flashback/FlashbackBufferManager.SegmentQueries.cs`.
Flashback buffer saturated math, PTS range clamps, completed-segment byte
summation, and normalized segment-path comparisons now live in
`Sussudio/Services/Flashback/FlashbackBufferManager.Math.cs`.

Flashback exporter request routing now lives in
`Sussudio/Services/Flashback/FlashbackExporter.Requests.cs`. Keep the public
`ExportAsync` null/disposed guards, segment path normalization, adaptive
throttle provider handoff, and single-versus-segment export selection there.

Flashback exporter lifetime behavior now lives in
`Sussudio/Services/Flashback/FlashbackExporter.Lifetime.cs`. Keep public
`Dispose`, active export cancellation, native-state cleanup on dispose, and
dispose-timeout logging there.

Flashback exporter execution scheduling now lives in
`Sussudio/Services/Flashback/FlashbackExporter.Execution.cs`. Keep the
single/multi-segment task wrappers, linked cancellation source disposal,
background thread priority, adaptive throttle provider scoping, and segment
snapshots there so native export cores stay behind focused entry points.

Flashback exporter single-file packet-copy/remux behavior now lives in
`Sussudio/Services/Flashback/FlashbackExporter.SingleFile.cs`. Keep the
single `.ts` export validation, seek, packet buffering, timestamp base
normalization, and single-export lock release there.

Flashback exporter multi-segment packet-copy/remux behavior now lives in
`Sussudio/Services/Flashback/FlashbackExporter.Segments.cs`. Keep segment
validation dispatch, skipped-requested-segment classification, per-segment
packet buffering, continuous timestamp repair, and segment-export lock release
there. Output-template selection and template-skip diagnostics live in
`Sussudio/Services/Flashback/FlashbackExporter.SegmentTemplate.cs`. The root
exporter keeps shared native state, constants, and fields only.

Flashback exporter infrastructure now lives in
`Sussudio/Services/Flashback/FlashbackExporter.Infrastructure.cs`. Keep export
lock/disposal helpers, native cleanup, cancellation-source handling, FFmpeg
error strings, timestamp math, and saturated arithmetic there so
`FlashbackExporter.cs` stays focused on export native state and shared policy.
Progress normalization/reporting, heartbeat cadence, and export writer
throttle/yield policy live in
`Sussudio/Services/Flashback/FlashbackExporter.Progress.cs`. Packet timestamp
normalization, segment boundary timestamp repair, packet clone/free helpers,
and buffered packet flushes live in
`Sussudio/Services/Flashback/FlashbackExporter.PacketTiming.cs`. FFmpeg input
and output context setup, stream count validation, stream-template copying, and
segment stream-layout checks live in
`Sussudio/Services/Flashback/FlashbackExporter.Streams.cs`. Temp output
validation, atomic replacement, overwrite policy, and invalid final-output cleanup live in
`Sussudio/Services/Flashback/FlashbackExporter.OutputFiles.cs`.
Temp output cleanup, stale temp preparation, and orphan `.mp4.tmp` cleanup live
in `Sussudio/Services/Flashback/FlashbackExporter.TempFiles.cs`.

D3D preview renderer metrics now live in
`Sussudio/Services/Preview/D3D11PreviewRenderer.Metrics.cs`. Keep read-only
present cadence, pipeline latency, render CPU timing, frame-latency wait metric
snapshots, recent sample copies, and timing summaries there. Render-loop metric
window updates, expected-frame-rate window resizing, and metric reset logic now
live in `D3D11PreviewRenderer.MetricsTracking.cs`. Keep slow-frame diagnostic
ring/write logic in `D3D11PreviewRenderer.SlowFrameDiagnostics.cs`, lifecycle in
`D3D11PreviewRenderer.Lifecycle.cs`, queueing in
`D3D11PreviewRenderer.PendingFrames.cs`, VideoProcessor/present work in
`D3D11PreviewRenderer.Rendering.cs`, and shader drawing in
`D3D11PreviewRenderer.ShaderRendering.cs`.

D3D preview renderer nested frame and metrics model types now live in
`Sussudio/Services/Preview/D3D11PreviewRenderer.FrameTypes.cs`. Keep the
`PendingFrame` lifetime wrapper and renderer metric record structs there so the
root renderer stays focused on construction, public state, and cross-cutting
interop declarations.

D3D preview renderer frame submission now lives in
`Sussudio/Services/Preview/D3D11PreviewRenderer.Submission.cs`. Keep public raw
frame, lease, texture, and NV12 plane submission entry points plus the NV12
pending-frame adapter there; keep render-thread start/stop and disposal in
`D3D11PreviewRenderer.Lifecycle.cs` and panel sizing in the root renderer.

D3D preview renderer lifecycle now lives in
`Sussudio/Services/Preview/D3D11PreviewRenderer.Lifecycle.cs`. Keep
render-thread start/stop, reinit stop, native-call drain fencing, pending-frame
shutdown cleanup, and renderer disposal there.

D3D preview renderer frame upload now lives in
`Sussudio/Services/Preview/D3D11PreviewRenderer.FrameUpload.cs`. Keep
video-processor input view resolution, external texture input-view creation,
direct raw-frame texture updates, and staging uploads there; keep present
tracking in `D3D11PreviewRenderer.Rendering.cs`.

D3D preview renderer shader drawing now lives in
`Sussudio/Services/Preview/D3D11PreviewRenderer.ShaderRendering.cs`. Keep NV12
plane shader rendering, HDR tonemap/passthrough shader rendering, reusable
shader class-instance arrays, and NV12 SRV caching there; keep the render loop,
VideoProcessor path, present accounting, and slow-frame diagnostic call sites in
`D3D11PreviewRenderer.Rendering.cs`.

D3D preview renderer slow-frame diagnostics now live in
`Sussudio/Services/Preview/D3D11PreviewRenderer.SlowFrameDiagnostics.cs`. Keep
recent slow-frame snapshot access, diagnostic thresholding, DXGI refresh-slip
reason construction, and the slow-frame ring buffer writer there; keep cadence
and CPU timing windows in `D3D11PreviewRenderer.Metrics.cs`.

D3D preview renderer viewport and letterbox helpers now live in
`Sussudio/Services/Preview/D3D11PreviewRenderer.Viewport.cs`. Keep
`ComputeLetterboxViewport`, `UpdateViewportConstantBuffer`, and
`ComputeLetterboxRect` there; keep shader draw path ordering in
`D3D11PreviewRenderer.ShaderRendering.cs` and D3D resource creation in
`D3D11PreviewRenderer.Resources.cs`.

D3D preview renderer submitted/rendered/dropped frame ownership tracking now
lives in `Sussudio/Services/Preview/D3D11PreviewRenderer.FrameOwnership.cs`.
Keep frame ownership snapshot projection and submitted/presented/dropped
ownership state updates there; keep cadence, latency, DXGI, and slow-frame
timing in `D3D11PreviewRenderer.Metrics.cs`, with slow-frame diagnostic
projection in `D3D11PreviewRenderer.SlowFrameDiagnostics.cs`.

D3D preview renderer DXGI frame statistics and display-clock projection now
live in `Sussudio/Services/Preview/D3D11PreviewRenderer.DxgiFrameStatistics.cs`.
Keep `GetFrameStatistics`, optional `DwmFlush`, visible-frame tick estimation,
and `IPreviewDisplayClock` snapshot construction there; keep slow-frame
diagnostic consumption of the latest DXGI counters in
`D3D11PreviewRenderer.SlowFrameDiagnostics.cs`.

D3D preview renderer frame-latency waitable swap-chain setup now lives in
`Sussudio/Services/Preview/D3D11PreviewRenderer.FrameLatency.cs`. Keep
`ConfigureFrameLatencyWaitableObject`, `WaitForFrameLatencySignal`, the native
`WaitForSingleObject` import, and wait-result constants there so resource
construction and render drawing stay focused.

D3D preview renderer device-lost recovery now lives in
`Sussudio/Services/Preview/D3D11PreviewRenderer.DeviceLost.cs`. Keep device
loss classification, device-lost frame drops, stop-guarded cleanup, and
reinitialize scheduling there; keep generic resource disposal in
`D3D11PreviewRenderer.Resources.cs`.

D3D preview renderer device initialization now lives in
`Sussudio/Services/Preview/D3D11PreviewRenderer.DeviceInitialization.cs`. Keep
shared-device handoff, renderer-owned device fallback, swap-chain creation, HDR
swap-chain capability probing, media present duration setup, and initial panel
binding there.

D3D preview renderer resource management now lives in
`Sussudio/Services/Preview/D3D11PreviewRenderer.Resources.cs`. Keep
video-processor setup, swap-chain RTV/output view creation, color-space
application, and D3D resource disposal there.
Raw-frame and HDR shader input texture allocation now lives in
`Sussudio/Services/Preview/D3D11PreviewRenderer.InputResources.cs`. Keep
NV12/P010 input textures, staging textures, input views, and HDR plane SRV
creation there. Device-lost recovery has its own focused owner; keep render
loop and present paths in `D3D11PreviewRenderer.Rendering.cs`, and shader draw
paths in `D3D11PreviewRenderer.ShaderRendering.cs`.

D3D preview renderer swap-chain panel binding now lives in
`Sussudio/Services/Preview/D3D11PreviewRenderer.PanelBinding.cs`. Keep
UI-thread `SetSwapChain` bind/unbind marshaling and composition scale
transforms there; keep device and view allocation in
`D3D11PreviewRenderer.Resources.cs`.

D3D preview pending-frame queue ownership now lives in
`Sussudio/Services/Preview/D3D11PreviewRenderer.PendingFrames.cs`. Keep
enqueue, backlog trimming, frame-ready signal/reset wrappers, explicit pending
drains, and pending-count accounting there; keep render-loop consumption in
`D3D11PreviewRenderer.Rendering.cs` and frame ownership metrics in
`D3D11PreviewRenderer.FrameOwnership.cs`.

Media Foundation source-reader negotiation now lives in
`Sussudio/Services/Capture/MfSourceReaderVideoCapture.Negotiation.cs`. Keep
DXGI manager attachment, device-source open/enumeration fallback, native
media-type selection, converted-type construction, frame-size/frame-rate
attribute reads, and optional media-attribute copy helpers there; keep
high-level source-reader state fields in the root source-reader file.

Media Foundation source-reader initialization now lives in
`Sussudio/Services/Capture/MfSourceReaderVideoCapture.Initialization.cs`. Keep
public initialization validation, startup-reference acquisition/release, reader
attribute construction, negotiated/actual media-type application, runtime field
reset, and initialization success/failure logging there.

Media Foundation source-reader read-loop ownership now lives in
`Sussudio/Services/Capture/MfSourceReaderVideoCapture.ReadLoop.cs`. Keep the
read thread priority, `ReadSample` outstanding-state tracking, sample timestamp
cadence handoff, frame-delivery invocation, frame-drop accounting, and fatal
D3D output failure break behavior there.

Media Foundation source-reader frame delivery now lives in
`Sussudio/Services/Capture/MfSourceReaderVideoCapture.FrameDelivery.cs`. Keep
sample-to-buffer conversion, compressed MJPG byte extraction, raw CPU frame
delivery, dual GPU/CPU delivery, 2D buffer handling, readback fallback, packed
stride copies, and GPU texture release there.

Media Foundation interop declarations now live in
`Sussudio/Services/Capture/MfSourceReaderVideoCapture.Interop.cs`. Keep MF
P/Invoke declarations, constants/HRESULTs/GUIDs, and flattened COM interface
layouts there; keep behavioral source-reader logic in the root and negotiation
partials.

Media Foundation source cadence metrics now live in
`Sussudio/Services/Capture/MfSourceReaderVideoCapture.Cadence.cs`. Keep the
public cadence snapshot record, expected-rate/window sizing, stop-time cadence
reset, timestamp interval tracking, and percentile/drop estimate calculations
there; keep sample reading and frame delivery in their named source-reader
partials.

Media Foundation source-reader diagnostics now live in
`Sussudio/Services/Capture/MfSourceReaderVideoCapture.Diagnostics.cs`. Keep the
debug-only COM vtable diagnostic there; keep sample reading, frame delivery,
and read-loop control flow in their named source-reader partials.

Media Foundation source-reader DXGI buffer extraction now lives in
`Sussudio/Services/Capture/MfSourceReaderVideoCapture.DxgiBuffers.cs`. Keep
IMFDXGIBuffer texture/subresource extraction, D3D texture IID lookup, and DXGI
fallback diagnostics there; keep frame delivery in
`MfSourceReaderVideoCapture.FrameDelivery.cs` and reader start/stop/dispose in
the lifecycle partial.

Media Foundation packed-frame layout helpers now live in
`Sussudio/Services/Capture/MfSourceReaderVideoCapture.FrameLayout.cs`. Keep
frame-size/row-byte calculation, packed-stride inference, stride-aware YUV
copying, and source subtype labels there; keep frame delivery in
`MfSourceReaderVideoCapture.FrameDelivery.cs` and reader start/stop/dispose in
the lifecycle partial.

Media Foundation source-reader lifecycle now lives in
`Sussudio/Services/Capture/MfSourceReaderVideoCapture.Lifecycle.cs`. Keep
public start/stop/dispose, reader/source COM release, lifecycle logging, and
fatal-error callback dispatch there; keep initialization and frame delivery in
their named source-reader partials.

Unified capture diagnostic metric projection now lives in
`Sussudio/Services/Capture/UnifiedVideoCapture.Metrics.cs`. Keep MJPEG timing
records, source-reader cadence forwarding, MJPEG jitter/hash metrics, preview
visual cadence metrics, and frame-ledger summary projection there; keep
top-level frame arrival routing in `UnifiedVideoCapture.cs`.

Unified capture source-session lifecycle now lives in
`Sussudio/Services/Capture/UnifiedVideoCapture.Lifecycle.cs`. Keep source-reader
initialization, read-loop start/stop, preview-reinit disposal, and capture/MJPEG
fatal-error callbacks there.

Unified capture recording/Flashback sink fan-out now lives in
`Sussudio/Services/Capture/UnifiedVideoCapture.SinkFanout.cs`. Keep recording
and Flashback enqueue helpers, non-blocking queue rejection accounting, legacy
encoder fallback enqueue adapters, and Flashback recording sequence-gap
accounting there; keep frame arrival callbacks in `UnifiedVideoCapture.cs`.

Unified capture preview routing now lives in
`Sussudio/Services/Capture/UnifiedVideoCapture.Preview.cs`. Keep preview sink
assignment, live-preview suppression/resume drains, MJPEG preview-frame decoded
callbacks, raw preview submission, and visual-cadence reset/recording helpers
there; keep recording and Flashback enqueue paths in
`UnifiedVideoCapture.SinkFanout.cs`.

MJPEG preview jitter-buffer metrics now live in
`Sussudio/Services/Capture/MjpegPreviewJitterBuffer.Metrics.cs`. Keep metrics
records, snapshot construction, timing samples, selected/dropped-frame
telemetry, and tick/millisecond conversion helpers there; keep queue ordering,
deadline drops, adaptive target depth, and emit-loop pacing in their focused
owners.

MJPEG preview jitter-buffer queueing and adaptive deadline policy now have
their own owners. `MjpegPreviewJitterBuffer.Queue.cs` owns queue depth, ordered
frame insertion/dequeue, missing-sequence recovery, clear behavior, and resume
reprime accounting. `MjpegPreviewJitterBuffer.Adaptive.cs` owns hard/soft
deadline drops, adjusted output cadence, target-depth increase/decrease, and
latency-pressure classification. `MjpegPreviewJitterBuffer.EmitLoop.cs` owns
the paced emit loop, display-clock alignment, frame submission to the preview
sink, tick waits, timer-resolution P/Invoke, and MMCSS registration. Keep the
root file focused on construction, public enqueue/suppression lifecycle, and
dispose-time queue teardown.

Parallel MJPEG decode pipeline timing now lives in
`Sussudio/Services/Gpu/ParallelMjpegDecodePipeline.Metrics.cs`. Keep timing
record structs, timing snapshot construction, per-decoder sample windows,
packet-hash metric access, and stopwatch conversion helpers there; keep worker
decode ingress in the root pipeline.

Parallel MJPEG decode pipeline decoded-frame ordering and emission now lives in
`Sussudio/Services/Gpu/ParallelMjpegDecodePipeline.Reorder.cs`. Keep strict
missing-sequence waits, known-missing skips, preview decoded-frame notification,
and ordered final drain there.

Parallel MJPEG decode pipeline lifecycle now lives in
`Sussudio/Services/Gpu/ParallelMjpegDecodePipeline.Lifecycle.cs`. Keep
stop/dispose, emitter signaling, shutdown joins, cleanup of remaining work and
reorder frames, fatal-callback dispatch, and remaining-timeout helpers there.

CUDA/D3D11 preview interop ownership is split by runtime boundary:
`Sussudio/Services/Gpu/CudaD3D11Interop.cs` keeps bridge state and public
texture handles, `CudaD3D11Interop.Initialization.cs` owns constructor setup and
zero-copy registration, `CudaD3D11Interop.Copy.cs` owns the zero-copy and staging
frame-copy paths, `CudaD3D11Interop.Lifetime.cs` owns disposal and CUDA resource
unregistration, and `CudaD3D11Interop.Native.cs` owns CUDA constants, P/Invoke
entry points, and the `CUDA_MEMCPY2D` native struct. Keep D3D11 locking,
primary-context ownership, and fallback-to-staging behavior unchanged.

NVDEC MJPEG decoder ownership is now split around the hot-path boundaries:
`Sussudio/Services/Gpu/NvdecMjpegDecoder.cs` keeps shared decoder state,
`NvdecMjpegDecoder.Initialization.cs` owns both context initialization paths,
`NvdecMjpegDecoder.Decode.cs` owns packet decode and CUDA context access,
`NvdecMjpegDecoder.Download.cs` owns CPU download/packed-buffer copies, and
`NvdecMjpegDecoder.Lifetime.cs` owns disposal plus FFmpeg error text. Keep
shared-context ownership and disposal order unchanged when touching these files.

Automation snapshot contracts now live in named model files under
`Sussudio/Models/Automation/`. The broad automation evidence DTO is split as an
`AutomationSnapshot*.cs` partial family by domain: root lifecycle/diagnostics,
user settings, HDR, audio/ingest, recording, capture format, source telemetry,
preview, MJPEG/cadence, system health, and Flashback. Other snapshot contracts
remain in `CaptureRuntimeSnapshot.cs`, `PreviewRuntimeSnapshot.cs`,
`PerformanceTimelineEntry.cs`, `FlashbackSegmentInfo.cs`, and
`ViewModelRuntimeSnapshot.cs`. Do not recreate a broad
`AutomationRuntimeSnapshots.cs` catch-all; add new DTO fields to the partial
that matches the snapshot surface they own.

Native XU AT-command transport and payload parsing now live in
`Sussudio/Services/Telemetry/NativeXuAtCommandProvider.AtProtocol.cs`. Keep raw
AT read/write frames, LRC/envelope handling, device-ID parsing, and command
failure formatting there; keep payload decoders in
`NativeXuAtCommandProvider.PayloadDecoding.cs`, and keep rolling telemetry
polling and active snapshot assembly in `NativeXuAtCommandProvider.RollingPoll.cs`.

Runtime capture snapshot projection now lives in
`Sussudio/Services/Capture/CaptureService.RuntimeSnapshots.cs`. That file owns
the read-only `CaptureRuntimeSnapshot` DTO construction consumed by UI,
automation, and verification; video ingest, source-reader health, WASAPI
capture, and playback output counter projection now lives in
`Sussudio/Services/Capture/CaptureService.RuntimeSnapshotIngestAudio.cs`, HDR
pipeline parity/downgrade projection now lives in
`Sussudio/Services/Capture/CaptureService.RuntimeSnapshotHdrPipeline.cs`,
source telemetry detail/age/alignment projection now lives in
`Sussudio/Services/Capture/CaptureService.RuntimeSnapshotSourceTelemetry.cs`,
and recording-integrity summary projection now lives in
`Sussudio/Services/Capture/CaptureService.RuntimeSnapshotRecordingIntegrity.cs`.
Recording-format and observed-frame helper policy live in focused snapshot
partials.

Capture health snapshot projection now lives in
`Sussudio/Services/Capture/CaptureService.HealthSnapshots.cs`. That file owns
the diagnostics/automation health DTO construction for source telemetry and
shared fields; MJPEG timing, jitter, packet-hash, visual-cadence, and
per-decoder projection lives in
`Sussudio/Services/Capture/CaptureService.HealthSnapshotMjpeg.cs`;
source telemetry, backend, suppression, and circuit-state projection lives in
`Sussudio/Services/Capture/CaptureService.HealthSnapshotSourceTelemetry.cs`;
capture cadence projection lives in
`Sussudio/Services/Capture/CaptureService.HealthSnapshotCaptureCadence.cs`;
Flashback buffer, startup-cache, backend-staleness, and encoder summary
projection lives in
`Sussudio/Services/Capture/CaptureService.HealthSnapshotFlashbackBuffer.cs`;
Flashback live queue, force-rotate, backpressure, and GPU queue projection lives
in `Sussudio/Services/Capture/CaptureService.HealthSnapshotFlashbackQueues.cs`;
active recording queue/failure projection lives in
`Sussudio/Services/Capture/CaptureService.HealthSnapshotRecording.cs`,
Flashback export diagnostic and derived progress/throughput projection lives in
`Sussudio/Services/Capture/CaptureService.HealthSnapshotFlashbackExport.cs`,
and Flashback playback state/cadence/decode/command projection lives in
`Sussudio/Services/Capture/CaptureService.HealthSnapshotFlashbackPlayback.cs`.
The general snapshot partial is now the diagnostics-snapshot compatibility
entry point plus shared Flashback/file/tick snapshot helpers.

Recording-format snapshot policy now lives in
`Sussudio/Services/Capture/CaptureService.SnapshotRecordingFormat.cs`. Keep
encoder codec labels, output pixel-format/profile labels, and requested
frame-rate argument projection there.

Observed frame-format snapshot policy now lives in
`Sussudio/Services/Capture/CaptureService.SnapshotObservedFrames.cs`. Keep the
explicit `Interlocked.Read` counter projection there; do not infer fake P010 or
NV12 frame counts from requested settings.

Source telemetry snapshot policy now lives in
`Sussudio/Services/Capture/CaptureService.SnapshotTelemetry.cs`. Keep telemetry
backend labels, frame-rate origin labels, suppression/circuit-state mapping,
request/telemetry alignment, and HDR warmup state classification there.

A/V sync snapshot policy now lives in
`Sussudio/Services/Capture/CaptureService.SnapshotAvSync.cs`. Keep live
source/audio drift calculations and encoder drift/correction projection there.

Stats dock and frame-time overlay lifecycle now live in
`Sussudio/Controllers/StatsOverlayController.cs`. `MainWindow.StatsOverlay.cs`
still renders metric values and assembles snapshots, but polling, visibility
state, dynamic diagnostic row pools, and dock animations are out of the shell
fields.
Stats overlay lifecycle, source-telemetry panel, and diagnostic row pooling
contract checks now live in
`tests/Sussudio.Tests/StatsOverlay.Contract.Tests.cs`.
Frame-time overlay graph drawing now lives in
`Sussudio/MainWindow.FrameTimeOverlay.cs`; `MainWindow.StatsOverlay.cs` keeps
the stats dock projection and snapshot adapter.
Decode and GPU hardware stats row projection now lives in
`Sussudio/MainWindow.StatsHardwareSections.cs`; row element pooling still
belongs to `StatsDiagnosticRowsController`.
Stats presentation and frame-time overlay contract checks now live in
`tests/Sussudio.Tests/StatsPresentation.Contract.Tests.cs` instead of expanding
the legacy harness body in `tests/Sussudio.Tests/Program.cs`.
Stats diagnostic summary/row parsing now lives in
`Sussudio/ViewModels/StatsPresentationBuilder.Diagnostics.cs`; keep the root
`StatsPresentationBuilder.cs` focused on dock and frame-time presentation.
Stats lane status classification and visual-repeat drift policy now live in
`Sussudio/ViewModels/StatsPresentationBuilder.Status.cs`.
Stats presentation DTO records/enums now live in
`Sussudio/ViewModels/StatsPresentationModels.cs`.

Dynamic stats diagnostic row pools now live in
`Sussudio/Controllers/StatsDiagnosticRowsController.cs`. It owns decode/GPU
row reuse, telemetry diagnostics empty state, group headers, and diagnostic row
style updates.

Flashback timeline visibility, lockout, toggle synchronization, and show/hide
animation state now live in
`Sussudio/Controllers/FlashbackTimelineController.cs`.
`MainWindow.FlashbackTimeline.cs` is the XAML-facing adapter; scrub/playback
commands remain in `MainWindow.Flashback.cs`.

Active Flashback pointer-scrub state now lives in
`Sussudio/MainWindow.FlashbackScrub.cs`. It owns scrub throttling,
release/cancel/capture-lost cleanup, and the timeline fraction/duration
geometry helpers that marker and playhead presentation share.

Flashback CTI/playhead compositor state now lives in
`Sussudio/MainWindow.FlashbackPlayhead.cs`. It owns magnetic scrub movement,
long-horizon linear playhead extrapolation, and CTI anchor timing; the broader
Flashback partial keeps command handling and toggle/apply workflows.

Flashback marker placement and compact duration text now live in
`Sussudio/MainWindow.FlashbackMarkers.cs`, including in/out marker visibility,
selection-region layout, and `m:ss` formatting.

Flashback playback in/out marker state and marker command handling now live in
`Sussudio/Services/Flashback/FlashbackPlaybackController.Markers.cs`. Keep
marker normalization and out-point pause checks there; keep decode pacing,
seek, and segment-opening flow in the playback controller core/thread partials.

Flashback playback position/file-PTS mapping now lives in
`Sussudio/Services/Flashback/FlashbackPlaybackController.PositionMapping.cs`.
It owns scrub/seek clamping, marker-bound range limits, saturating timestamp
math, active fMP4 segment detection, and playback path comparison.

Flashback playback diagnostics now live in
`Sussudio/Services/Flashback/FlashbackPlaybackController.Metrics.cs`. That
partial owns public playback counters plus cadence/decode summary records.
Private metric collection, percentile math, seek-cap telemetry, decode timing
wrappers, and metric reset behavior now live in
`Sussudio/Services/Flashback/FlashbackPlaybackController.MetricsCollection.cs`.

Flashback playback public command entry points now live in
`Sussudio/Services/Flashback/FlashbackPlaybackController.Commands.cs`. Keep
scrub, seek, play/pause, go-live, and nudge request gating there; keep command
queue coalescing and playback-thread execution in the queue/thread partials.
Seek/scrub coalescing slots, queued-position resolution, and control-yield
peek policy now live in
`Sussudio/Services/Flashback/FlashbackPlaybackController.CommandCoalescing.cs`.
Keep raw queue write/drop policy in the command queue partial.
Command readiness/failure formatting and queue telemetry bookkeeping live in
`Sussudio/Services/Flashback/FlashbackPlaybackController.CommandTelemetry.cs`.
Keep command status counters and last-failure/latency updates there instead of
growing command channel mechanics.
Playback thread start/stop, command-channel recreation, abandoned-command
draining, and join/cancel diagnostics now live in
`Sussudio/Services/Flashback/FlashbackPlaybackController.ThreadLifecycle.cs`.
Keep queue write/coalescing/drop policy in the command queue partial.
Playback thread exit cleanup now lives in
`Sussudio/Services/Flashback/FlashbackPlaybackController.ThreadCleanup.cs`.
Keep repeated live-restore cleanup and playback CTS disposal warnings there
instead of duplicating teardown blocks inside the worker loop.
Playback timer-resolution P/Invoke now lives in
`Sussudio/Services/Flashback/FlashbackPlaybackController.ThreadTimer.cs`;
keep it isolated from thread command execution and audio prebuffer logic.

Flashback playback audio routing now lives in
`Sussudio/Services/Flashback/FlashbackPlaybackController.AudioRouting.cs`.
Keep decoder audio callbacks, playback chunk validation/return, live audio
suppress/restore, preview submission suppression, and audio renderer pause/
resume/flush helpers there; keep decode-ahead prebuffer and rewind behavior in
the audio prebuffer partial.

Flashback playback component lifecycle now lives in
`Sussudio/Services/Flashback/FlashbackPlaybackController.Lifecycle.cs`. Keep
initialization, audio/preview component reference updates, preview-detach
cleanup, deferred reattach, and disposal there; keep decoder file handling and
playback pacing in the controller core/thread partials.

Flashback playback decoded-frame submission now lives in
`Sussudio/Services/Flashback/FlashbackPlaybackController.PreviewFrames.cs`.
Keep frame validation, preview submission, held-frame ownership/release, and
live-restore-after-submit-failure helpers there; keep seek and playback loops
in the core/thread partials.

Flashback playback decoder file handling now lives in
`Sussudio/Services/Flashback/FlashbackPlaybackController.DecoderFiles.cs`.
Keep decoder creation, active segment file identity, file open checks, and
decoder cleanup there. Active fMP4 reopen retry, adjacent-segment seek fallback,
and keyframe-reopen recovery now live in
`Sussudio/Services/Flashback/FlashbackPlaybackController.DecoderReopen.cs`.
Keep seek-display and playback pacing in the controller core/thread partials.

Flashback playback seek/scrub frame display now lives in
`Sussudio/Services/Flashback/FlashbackPlaybackController.SeekDisplay.cs`.
Keep keyframe seek display, displayed-frame PTS mapping, adjacent-segment
fallback for seek display, and seek-display failure accounting there; keep
continuous playback pacing in the controller core/thread partials.

Flashback continuous playback progression now lives in
`Sussudio/Services/Flashback/FlashbackPlaybackController.PlaybackLoop.cs`.
Keep playback frame reads, A/V skip decisions, decoded-frame submission flow,
decode-error snap-to-live, and near-live snap handling there. Segment switching,
fMP4 reopen recovery, and write-head waits now live in
`Sussudio/Services/Flashback/FlashbackPlaybackController.PlaybackSegmentEdges.cs`.

Flashback playback timing and cadence now live in
`Sussudio/Services/Flashback/FlashbackPlaybackController.PlaybackTiming.cs`.
Keep frame-rate resolution, pause-from-live target calculation,
software-decode budget snaps, and decoded PTS/cadence tracking there.
Audio-master pacing, fallback accounting, clock-drift calculation, and
wall-clock sleep/spin pacing now live in
`Sussudio/Services/Flashback/FlashbackPlaybackController.AudioMasterPacing.cs`.
Decoder close best-effort handling now lives with decoder file ownership, and
decode-error snap-to-live recovery lives with the continuous playback loop, so
the root controller can remain a field/property facade plus shared state setter.

Flashback status and playback-position polling timers now live in
`Sussudio/Controllers/FlashbackPollingController.cs`.
`MainWindow.FlashbackPolling.cs` is the XAML-facing adapter; CTI anchor timing
stays with playhead motion.

Settings shelf visibility, the animation gate, and show/hide storyboard
construction now live in
`Sussudio/Controllers/SettingsShelfController.cs`. `MainWindow.SettingsShelf.cs`
is the XAML-facing adapter.

Splash phrase loading, randomized timer pacing, and the two-line splash text
animation now live in `Sussudio/Controllers/SplashLoadingPhraseController.cs`.
`MainWindow.SplashLoading.cs` is the XAML-facing adapter.

Splash-to-shell launch entrance choreography, initial hidden/scaled shell state,
and one-shot playback state now live in
`Sussudio/Controllers/LaunchEntranceAnimationController.cs`.
`MainWindow.LaunchEntrance.cs` is the XAML-facing adapter.

Control-bar button ownership and hover/press/release scale behavior now live in
`Sussudio/Controllers/ControlBarAnimationController.cs`.
`MainWindow.ControlBarAnimations.cs` is the XAML-facing adapter.

Static shell ThemeShadow and translation setup for the control bar and record
button now live in `Sussudio/Controllers/ShellElevationController.cs`.
`MainWindow.ShellElevation.cs` is the XAML-facing adapter.

Preview shell/content fade and scale transitions plus unavailable-placeholder
presentation now live in
`Sussudio/Controllers/PreviewTransitionAnimationController.cs`.
`MainWindow.PreviewTransitions.cs` is the XAML-facing adapter; composition
shadow animation remains in `MainWindow.Animations.cs`.

Record-button circle/pill width animation now lives in
`Sussudio/Controllers/RecordButtonAnimationController.cs`.
`MainWindow.RecordButtonAnimations.cs` is the XAML-facing adapter.

Recording button command execution and preview-state logging after a recording
start now live in `Sussudio/Controllers/RecordingButtonActionController.cs`.
`MainWindow.RecordingActions.cs` is the XAML-facing adapter.

Live-signal pill visibility state, show/hide debounce timers, and the small
scale/fade animation now live in
`Sussudio/Controllers/LiveSignalInfoController.cs`. `MainWindow.LiveSignalInfo.cs`
is the XAML-facing adapter.

Preview-volume fade-in/fade-out state, saved target volume, storyboard lifetime,
and volume save suppression now live in
`Sussudio/Controllers/PreviewAudioFadeController.cs`.
`MainWindow.PreviewAudioFade.cs` is the XAML-facing adapter.

Preview startup state, watchdog/telemetry timers, first-visual confirmation,
and timeout recovery now live in `Sussudio/MainWindow.PreviewStartup.cs`
instead of the composition-root constructor partial. Readiness-signal collection,
missing-signal formatting, and playback-progress diagnostics live in
`Sussudio/MainWindow.PreviewStartupSignals.cs`. This keeps the root shell
focused on wiring while leaving the existing startup state machine behavior
unchanged.
Delayed preview reveal after first visual now lives in
`Sussudio/MainWindow.PreviewFadeIn.cs`; watchdog/timeout recovery remains in
`MainWindow.PreviewStartup.cs`.
Preview startup loading overlay presentation now lives in
`Sussudio/MainWindow.PreviewStartupOverlay.cs`.

Preview-specific ViewModel events and property-change projections now live in
`Sussudio/MainWindow.PropertyChangedPreview.cs`. The broad
`MainWindow.PropertyChanged.cs` dispatcher still routes `PropertyChanged`
notifications, but preview start/stop/reinit choreography has a named owner.

Recording-specific ViewModel property projections now live in
`Sussudio/MainWindow.PropertyChangedRecording.cs`: record-button morphing,
recording glow, and the recording-time lockout state for capture/audio controls.

Flashback-specific ViewModel property projections now live in
`Sussudio/MainWindow.PropertyChangedFlashback.cs`: timeline lockout, marker and
playhead refresh, export progress, and Flashback settings-control sync.

Audio and microphone-specific ViewModel property projections now live in
`Sussudio/MainWindow.PropertyChangedAudio.cs`: audio toggles, monitoring meter
state, preview volume slider sync, microphone enablement, and microphone volume
sync.

Microphone volume slider synchronization, save triggers, shelf enablement, and
mic-meter row animation state now live in
`Sussudio/Controllers/MicrophoneControlsController.cs`.
`MainWindow.MicrophoneControls.cs` is the XAML-facing adapter.

Control-bar label visibility and capture-settings narrow/wide grid placement
now live in `Sussudio/Controllers/ResponsiveShellLayoutController.cs`.
`MainWindow.ResponsiveShellLayout.cs` is the XAML-facing adapter.

Capture, audio, microphone, and encoder selection synchronization now lives in
the `Sussudio/Controllers/CaptureSelectionBindingController*.cs` family. The
root controller owns selection reconciliation and pending-device apply state,
`.Context.cs` owns the XAML control dependency bag, `.SelectionSync.cs` owns
collection-change debounce/queued sync, and `.DeviceAudio.cs` owns device-audio
mode/gain projection while `MainWindow.CaptureSelectionBindings.cs` keeps the
old method names for `PropertyChanged` and binding setup.

Capture-device refresh/apply button workflows now live in
`Sussudio/Controllers/CaptureDeviceActionController.cs`.
`MainWindow.CaptureDeviceActions.cs` is the XAML-facing adapter and keeps the
explicit apply/reinit path separate from selection synchronization.

Presentation-only rules for capture option affordances now live in
`Sussudio/MainWindow.CaptureOptionPresentation.cs`: HDR readiness hints, FPS
telemetry tooltips, MJPEG decoder count selection/visibility, bitrate mode
visibility, and audio clipping visibility.

Recording output-path truncation and tooltip updates now live in
`Sussudio/Controllers/OutputPathDisplayController.cs`.
`MainWindow.OutputPathDisplay.cs` is the XAML-facing adapter used by binding
setup and property changes.

Recording output-path browse/open-recordings button workflows now live in
`Sussudio/Controllers/OutputPathActionController.cs`.
`MainWindow.OutputPathActions.cs` is the XAML-facing adapter.

Diagnostic session DTOs now live in focused model files:
`tools/Common/DiagnosticSessionOptions.cs`,
`tools/Common/DiagnosticSessionResult.cs`, and
`tools/Common/DiagnosticSessionSample.cs`. `DiagnosticSessionRunner.cs` still
owns orchestration and scenario execution, but the public
options/result/sample contracts are separated from runner behavior.

Diagnostic-session result text now lives in a focused partial family rooted at
`tools/Common/DiagnosticSessionResultFormatter.cs`. The root owns the public
`Format(...)` flow, `.Overview.cs` owns header/capture/verification/PresentMon
and process sections, `.Flashback.cs` owns Flashback playback/recording/export
sections, `.Preview.cs` owns preview scheduler/D3D/visual cadence sections,
`.Artifacts.cs` owns artifact/action/warning sections, and `.Helpers.cs` owns
small text helpers. The runner keeps `Format(...)` as a compatibility wrapper
so existing ssctl and MCP callers do not need to know about the formatter owner.

Diagnostic-session result construction now lives in
`tools/Common/DiagnosticSessionResultBuilder.cs`. The root owns result phase
orchestration, artifact-write handoff, summary-write handoff, and final
summary emission while the runner keeps the phase sequence.
`DiagnosticSessionResultBuilder.Result.cs` owns the
`DiagnosticSessionResult` DTO projection and success calculation. Diagnostic
health analysis, Flashback warning tolerance, metric gathering, and
result-build handoff models live beside it in
`DiagnosticSessionResultBuilder.Analysis.cs` and
`DiagnosticSessionResultBuilder.Models.cs`.

Diagnostic-session summary writing now lives in
`tools/Common/DiagnosticSessionSummaryWriter.cs`. It owns `summary.json` writes
and summary-write failure repair of the returned result object.

Diagnostic-session result artifact setup now lives in
`tools/Common/DiagnosticSessionResultArtifacts.cs`. It owns result artifact path
construction and pre-summary sample, frame-ledger, and timeline writes while
the result builder keeps summary field construction.

Shared diagnostic-session text helpers now live in
`tools/Common/DiagnosticSessionText.cs`. Keep cross-cutting string helpers
there instead of reintroducing private duplicates in the runner, formatter, or
validation policy files.

MCP performance timeline projection is split across the
`tools/McpServer/Tools/PerformanceTimelineTools.*.cs` family. Keep the public
tool entry point and table/trend rendering in the root file, JSON-to-row
projection and the private row model in `PerformanceTimelineTools.Rows.cs`,
compact value/byte/export/D3D formatting helpers in
`PerformanceTimelineTools.Formatting.cs`, and target/pressure summaries in
`PerformanceTimelineTools.Summaries.cs`.

Diagnostic-session pipe retry/error classification now lives in
`tools/Common/DiagnosticSessionPipeRetryPolicy.cs`, keeping access-denied as a
permanent failure and connect failed/timeout responses retryable.

Diagnostic-session command sending now lives in
`tools/Common/DiagnosticSessionCommandChannel.cs`. It owns serialized command
execution, connect-retry wrapping, command failure accounting, and
`WaitForCondition` payload shaping while the runner keeps phase orchestration.

Diagnostic-session JSON artifact helpers now live in
`tools/Common/DiagnosticSessionJsonArtifacts.cs`. The runner still owns the
session lifecycle, but JSON writing, frame-ledger extraction, and snapshot /
verification response extraction have a smaller home.

Diagnostic-session initial snapshot capture now lives in
`tools/Common/DiagnosticSessionInitialSnapshot.cs`. It owns the baseline
`GetSnapshot`, the unknown-state warning, and initial-snapshot exception
recording while the runner keeps phase ordering.

Diagnostic-session run state now lives in
`tools/Common/DiagnosticSessionRunState.cs`. It owns last-stage tracking,
terminal exception classification, `session-live.json` breadcrumbs, and
best-effort artifact write failure recording while the runner keeps the
scenario flow readable.

Diagnostic-session output locking now lives in
`tools/Common/DiagnosticSessionOutputLock.cs`. It owns the
`.sussudio-diag.lock` file, exclusive `FileShare.None` open, delete-on-close
cleanup, and concurrent-output-directory failure message while the runner only
acquires and disposes the lock.

Diagnostic-session background task tracking now lives in
`tools/Common/DiagnosticSessionBackgroundTasks.cs`. The root owns scenario task
registration, deterministic await order, and normal PresentMon completion.
`DiagnosticSessionBackgroundTasks.FaultDrain.cs` owns interrupted-task warning
collection and fault drain. `DiagnosticSessionBackgroundTasks.Models.cs` owns
the small background-task handoff records.

Diagnostic-session scenario startup now lives in a focused partial family.
`tools/Common/DiagnosticSessionScenarioStartup.cs` owns the public startup
orchestration call. `DiagnosticSessionScenarioStartup.Registrations.cs` owns
non-export Flashback scenario task registration,
`DiagnosticSessionScenarioStartup.DeferredSettings.cs` owns deferred Flashback
recording-settings task registration,
`DiagnosticSessionScenarioStartup.ExportRegistrations.cs` owns Flashback export
task registration, and
`DiagnosticSessionScenarioStartup.Playback.cs` owns the direct Flashback
playback start command and playback-state wait. The runner now delegates
startup and keeps the setup/sampling/cleanup/summary phase flow.

Diagnostic-session PresentMon startup now lives in
`tools/Common/DiagnosticSessionPresentMonStartup.cs`. It owns optional
PresentMon launch, correlation snapshot capture, and `presentmon.csv` output
selection while scenario startup keeps scenario task registration.

Diagnostic-session scenario setup now lives in
`tools/Common/DiagnosticSessionScenarioSetup.cs`. It owns initial state
mutations before sampling: Flashback enable/disable for scenario requirements,
preview start, recording start, and readiness waits.

Diagnostic-session cleanup mutations now live in
`tools/Common/DiagnosticSessionCleanupActions.cs`. The root owns recording stop
for verification and the public cleanup flow. Flashback playback go-live
restore, preview stop, and Flashback enable-state restore live beside it in
`DiagnosticSessionCleanupActions.StateRestore.cs`. The cleanup result record
lives in `DiagnosticSessionCleanupActions.Models.cs`, while
`DiagnosticSessionCleanupPolicy.cs` remains the post-cleanup warning validator.

Diagnostic-session recording checks now live in
`tools/Common/DiagnosticSessionRecordingChecks.cs`. It owns deferred Flashback
recording-settings restore, last-recording or Flashback export verification,
and Flashback recording validation while the runner keeps the high-level
post-cleanup phase order.

Diagnostic-session post-run snapshot fetches now live in
`tools/Common/DiagnosticSessionPostRunSnapshots.cs`. It owns performance
timeline artifact input and final health snapshot refresh while the runner
keeps the high-level post-cleanup phase order.

Diagnostic-session scenario flagging now lives in
`tools/Common/DiagnosticSessionScenarioPlan.cs`. It owns normalized scenario
booleans plus grouped warning/validation policy switches so the runner does not
grow direct scenario string comparisons.

Diagnostic-session cleanup restore validation now lives in
`tools/Common/DiagnosticSessionCleanupPolicy.cs`. It owns warnings for preview,
Flashback, and playback state that remain active after the runner attempts
cleanup.

Diagnostic-session Flashback cycle scenarios now live in
`tools/Common/DiagnosticSessionFlashbackCycleScenarios.cs`. They own the
restart-cycle and encoder-cycle command flows, export verification, and preset
restoration while the runner only starts the scenario tasks.

Diagnostic-session sampling now lives in
`tools/Common/DiagnosticSessionSampler.cs`. Keep the sample append before the
optional checkpoint callback so checkpoint failures cannot orphan an unseen
sample.

Diagnostic-session metric projection now lives in a focused partial family
rooted at `tools/Common/DiagnosticSessionMetrics.cs`. The root is only a
marker shell. `DiagnosticSessionMetrics.Models.cs` owns metric DTOs,
`.SourceCadence.cs` owns source cadence projection, `.PreviewCadence.cs` owns
preview/visual cadence projection and health classification, `.PreviewD3D.cs`
owns D3D slow-frame and CPU timing summaries, `.PlaybackCommands.cs` owns
playback command-health deltas, and `.Counters.cs` owns shared counter-delta
helpers.

Diagnostic-session Flashback export helpers now live in
`tools/Common/DiagnosticSessionFlashbackExports.cs`. They own strict export
verification payload construction, rotated-export segment-count parsing,
range-selection cleanup, and the range export audio-switch companion command
while scenario command sequencing lives in a separate owner.

Diagnostic-session Flashback export scenarios now live in a focused partial
family rooted at `tools/Common/DiagnosticSessionFlashbackExportScenarios.cs`.
The root is only a marker shell; concurrent export, disable-during-export,
rotated export, export during playback, and selection-range export flows each
have their own named file while the runner only starts the scenario tasks.

Diagnostic-session Flashback lifecycle checks now live in
`tools/Common/DiagnosticSessionFlashbackLifecycleScenarios.cs`. They own the
pause/seek/play disable-and-re-enable flow and post-disable playback queue
assertions while the runner only starts the lifecycle task.

Diagnostic-session Flashback metric projection now lives in a focused partial
family rooted at `tools/Common/DiagnosticSessionFlashbackMetrics.cs`. The root
is only a marker shell; DTOs, recording metrics, playback session aggregation,
playback result copying, and export metrics each have named owner files. These
helpers remain snapshot-only projections and must not send automation commands.

Diagnostic-session Flashback preview-cycle scenarios now live in a focused
partial family. `tools/Common/DiagnosticSessionFlashbackPreviewCycleScenarios.cs`
is the marker shell and preview-cycle predicate owner. `.Flashback.cs`,
`.Playback.cs`, and `.Recording.cs` own preview stop/restart flows for normal
Flashback, playback, and recording-backed diagnostics while the runner only
starts the scenario tasks.

Diagnostic-session Flashback rejected-export scenarios now live in
`tools/Common/DiagnosticSessionFlashbackRejectedExports.cs`. They own inactive
buffer and active-recording rejection flows, including failure-kind and
post-rejection state assertions.

Diagnostic-session Flashback recording-settings deferral now lives in
`tools/Common/DiagnosticSessionFlashbackRecordingSettingsScenarios.cs`. It owns
preset mutation rejection during Flashback recording plus post-stop preset
verification and restore.

Diagnostic-session Flashback segment playback now lives in
`tools/Common/DiagnosticSessionFlashbackSegmentPlaybackScenarios.cs`. It owns
completed-segment playback crossing. Recording-assisted segment rotation and
best-effort stop cleanup live beside it in
`DiagnosticSessionFlashbackSegmentPlaybackScenarios.RecordingAssist.cs`, while
`DiagnosticSessionFlashbackSegments.cs` stays read-only segment parsing and
wait policy.

Diagnostic-session Flashback segment handling now lives in
`tools/Common/DiagnosticSessionFlashbackSegments.cs`. It owns segment DTOs,
`FlashbackGetSegments` parsing, completed-segment waits, and playable-boundary
headroom waits while the runner keeps scenario command sequencing.

Diagnostic-session Flashback snapshot waits now live in
`tools/Common/DiagnosticSessionFlashbackWaits.cs`. The root owns read-only
polling loops for preview active, Flashback active, recording-ready, and
buffer-ready checks. `DiagnosticSessionFlashbackWaits.Playback.cs` owns
playback boundary, state, warmup, and position polling while the runner keeps
scenario command sequencing.

Diagnostic-session Flashback stress orchestration now lives in a focused
partial family. `tools/Common/DiagnosticSessionFlashbackStressScenario.cs` owns
stress thresholds, `.Stress.cs` owns the main stress command sequence,
`.Scrub.cs` owns scrub-stress command bursts and drain checks, and
`.AudioMaster.cs` owns warmed-playback audio-master fallback classification
while the runner only starts the scenario tasks.

Diagnostic-session Flashback validation now lives in
`tools/Common/DiagnosticSessionFlashbackValidation.cs`. It owns recording,
playback, and preview-scheduler warning thresholds over already projected
metrics while the runner retains scenario orchestration.

Diagnostic-session health policy now lives in
`tools/Common/DiagnosticSessionHealthPolicy.cs`. It owns health severity,
Flashback warmup filtering, sparse cadence tolerances, and tolerated warning
classification while the runner still owns scenario execution and warning
emission.

Shared automation pipe client ownership is split from a single helper into a
focused partial family. `tools/Common/AutomationPipeClient.cs` is the public
client marker shell, `AutomationPipeClient.Transport.cs` owns named-pipe
connect/write/read and pipe error classification, `AutomationPipeClient.Commands.cs`
owns command envelope sending and `not_ready` retry policy,
`AutomationPipeClient.ResponseState.cs` owns tolerant response-state parsing,
and `AutomationPipeClient.Models.cs` owns command result and exception types.

PresentMon model and text ownership is split from the probe runner.
`tools/Common/PresentMonProbe.Models.cs` owns PresentMon options, result,
summary, swap-chain, correlation, and metric DTOs.
`tools/Common/PresentMonProbe.Format.cs` owns result text formatting while
`tools/Common/PresentMonProbe.Csv.cs` owns CSV parse overloads, row projection,
and summary assembly. `PresentMonProbe.Csv.Fields.cs` owns header/field parsing
and CSV line tokenization. `PresentMonProbe.Csv.SwapChains.cs` owns swap-chain
normalization, artifact filtering, and selected-chain summaries.
`PresentMonProbe.Csv.Correlation.cs` owns app-present correlation, while
`PresentMonProbe.Csv.Summary.cs` owns warnings, counted text fields, and
percentile metric aggregation. `PresentMonProbe.Csv.Models.cs` owns the private
parsed-row shape. `PresentMonProbe.cs` keeps the public run orchestration and
result-message shaping. `PresentMonProbe.Paths.cs` owns target process,
PresentMon executable, and output-path resolution. `PresentMonProbe.Arguments.cs`
owns command-line construction and argument quoting. `PresentMonProbe.Process.cs`
owns process supervision, stdout/stderr drain, timeout kill, and temp CSV
cleanup.

Remaining `tools/Common` ownership:

- `AutomationPipeClient.cs`
- `AutomationPipeClient.Transport.cs`
- `AutomationPipeClient.Commands.cs`
- `AutomationPipeClient.ResponseState.cs`
- `AutomationPipeClient.Models.cs`
- `DiagnosticSessionBackgroundTasks.cs`
- `DiagnosticSessionBackgroundTasks.FaultDrain.cs`
- `DiagnosticSessionBackgroundTasks.Models.cs`
- `DiagnosticSessionCleanupActions.cs`
- `DiagnosticSessionCleanupActions.StateRestore.cs`
- `DiagnosticSessionCleanupActions.Models.cs`
- `DiagnosticSessionCleanupPolicy.cs`
- `DiagnosticSessionRecordingChecks.cs`
- `DiagnosticSessionFlashbackCycleScenarios.cs`
- `DiagnosticSessionFlashbackExports.cs`
- `DiagnosticSessionFlashbackLifecycleScenarios.cs`
- `DiagnosticSessionFlashbackPreviewCycleScenarios.cs`
- `DiagnosticSessionFlashbackPreviewCycleScenarios.Flashback.cs`
- `DiagnosticSessionFlashbackPreviewCycleScenarios.Playback.cs`
- `DiagnosticSessionFlashbackPreviewCycleScenarios.Recording.cs`
- `DiagnosticSessionFlashbackRejectedExports.cs`
- `DiagnosticSessionFlashbackRecordingSettingsScenarios.cs`
- `DiagnosticSessionFlashbackSegmentPlaybackScenarios.cs`
- `DiagnosticSessionFlashbackSegmentPlaybackScenarios.RecordingAssist.cs`
- `DiagnosticSessionFlashbackSegments.cs`
- `DiagnosticSessionFlashbackStressScenario.cs`
- `DiagnosticSessionFlashbackStressScenario.Stress.cs`
- `DiagnosticSessionFlashbackStressScenario.Scrub.cs`
- `DiagnosticSessionFlashbackStressScenario.AudioMaster.cs`
- `DiagnosticSessionFlashbackWaits.cs`
- `DiagnosticSessionFlashbackWaits.Playback.cs`
- `DiagnosticSessionFlashbackValidation.cs`
- `DiagnosticSessionHealthPolicy.cs`
- `DiagnosticSessionJsonArtifacts.cs`
- `DiagnosticSessionInitialSnapshot.cs`
- `DiagnosticSessionMetrics.cs`
- `DiagnosticSessionMetrics.Models.cs`
- `DiagnosticSessionMetrics.SourceCadence.cs`
- `DiagnosticSessionMetrics.PreviewCadence.cs`
- `DiagnosticSessionMetrics.PreviewD3D.cs`
- `DiagnosticSessionMetrics.PlaybackCommands.cs`
- `DiagnosticSessionMetrics.Counters.cs`
- `DiagnosticSessionOptions.cs`
- `DiagnosticSessionResult.cs`
- `DiagnosticSessionSample.cs`
- `DiagnosticSessionPipeRetryPolicy.cs`
- `DiagnosticSessionCommandChannel.cs`
- `DiagnosticSessionPostRunSnapshots.cs`
- `DiagnosticSessionResultArtifacts.cs`
- `DiagnosticSessionResultBuilder.cs`
- `DiagnosticSessionResultBuilder.Result.cs`
- `DiagnosticSessionResultBuilder.Analysis.cs`
- `DiagnosticSessionResultBuilder.Models.cs`
- `DiagnosticSessionResultFormatter.cs`
- `DiagnosticSessionResultFormatter.Overview.cs`
- `DiagnosticSessionResultFormatter.Flashback.cs`
- `DiagnosticSessionResultFormatter.Preview.cs`
- `DiagnosticSessionResultFormatter.Artifacts.cs`
- `DiagnosticSessionResultFormatter.Helpers.cs`
- `DiagnosticSessionSummaryWriter.cs`
- `DiagnosticSessionRunState.cs`
- `DiagnosticSessionSampler.cs`
- `DiagnosticSessionScenarioPlan.cs`
- `DiagnosticSessionScenarioSetup.cs`
- `DiagnosticSessionScenarioStartup.cs`
- `DiagnosticSessionScenarioStartup.Registrations.cs`
- `DiagnosticSessionScenarioStartup.DeferredSettings.cs`
- `DiagnosticSessionScenarioStartup.ExportRegistrations.cs`
- `DiagnosticSessionScenarioStartup.Playback.cs`
- `DiagnosticSessionPresentMonStartup.cs`
- `DiagnosticSessionText.cs`
- `DiagnosticSessionRunner.cs`
- `AutomationResponseState.cs`
- `JsonOptions.cs`
- `PresentMonProbe.cs`
- `PresentMonProbe.Paths.cs`
- `PresentMonProbe.Arguments.cs`
- `PresentMonProbe.Process.cs`

## Next Slices

1. Continue decomposing diagnostic-session runner internals by owner.

   `tools/Common/DiagnosticSessionRunner.cs` is still large. Scenario catalog
   initial scenario setup, optional scenario startup, cleanup mutation
   ownership, post-cleanup recording checks, post-run snapshot fetches, command
   send/failure plumbing, and result construction are extracted; next, split
   remaining production runner families or pivot to the next large owner. The
   reflective runner behavior tests are already split by scenario, so keep new
   runner coverage in the focused owner file that matches the behavior. Keep
   JSON summary shape unchanged.

2. Reduce custom regression harness size.

   `tests/Sussudio.Tests/Program.cs` should keep the legacy runner entry point,
   but checks should keep migrating into focused xUnit files or focused
   partial contract files while the dual-stack harness remains. MCP tool
   surface tests are now split into command-routing, diagnostic-session tool,
   diagnostic-session ownership, diagnostic-session result ownership,
   diagnostic-session Flashback, diagnostic-session runner, performance,
   window/preview, window/preview probes, and helper partial files. Flashback
   tests are also split by buffer, encoder, exporter, exporter cleanup,
   playback, decoder, and support owners. Capture
   session coordinator tests
   are split into API/contracts, queue behavior, Flashback behavior,
   transition policy, ownership, and harness-helper owners. MainViewModel
   automation tests are split into surface, diagnostics refresh, diagnostics projection,
   runtime-safety, and Flashback cleanup owners. MainViewModel capture tests
   are split into preview startup, Flashback export, Flashback routing,
   Flashback backend, and Flashback frame-rate/lifecycle owners. Continue with
   low-risk contract groups first. Snapshot-model contract tests are split by
   CaptureDiagnostics, CaptureHealth, and source-signal telemetry model owner.
   Recording queue tests are split into overload policy, LibAv sink, WASAPI,
   and capture fan-out/backend owners. D3D preview renderer tests are split
   into geometry, cadence, diagnostics-contract, source-ownership, device-lost,
   and frame-flow owners. Automation tool contract tests are split into
   protocol, catalog/manifest, reliability-gates, and snapshot formatter
   owners. Capture configuration model tests are split into option, settings,
   encoder support, Flashback DTO, and recording pipeline owners. Pooled-frame
   tests are split into lease lifecycle, MJPEG jitter policy, MJPEG jitter
   queue behavior, and queued lease release owners. Flashback buffer segment
   tests are split between mutation/accounting/disposal coverage and segment
   lookup/list projection coverage.

3. Continue converting MainWindow partial concerns into controllers.

   `FullScreen`, automation `Screenshot`, and audio meter rendering are
   extracted. `StatsOverlay` lifecycle, frame-time overlay drawing, and
   hardware stats sections are extracted; next UI candidates are preview
   startup, Flashback timeline UI, and the remaining stats row/snapshot
   projection. Keep XAML bindings stable.

4. Move MainViewModel feature state behind a facade.

   Preserve the root `MainViewModel` public surface while introducing feature
   view models or adapters for capture selection, recording, audio, Flashback,
   diagnostics, and automation. `MainViewModelDependencies.cs` now owns the
   default service graph for the root compatibility view model, which gives the
   next facade slices a small construction seam without changing XAML bindings
   or automation contracts. The live audio/microphone meter callback state
   now has a named owner in `MainViewModel.AudioMeters.cs`; keep future meter
   behavior there instead of growing the root facade file. Audio ramp trace
   buffering/sampling now lives in `MainViewModel.AudioRampTrace.cs`; keep the
   preview monitoring call sites in `MainViewModel.AudioMonitoring.cs`.
   Microphone endpoint volume synchronization and persistence now live in
   `MainViewModel.MicrophoneVolume.cs`; device-native audio mode/gain
   management stays in `MainViewModel.AudioControls.cs`. UI-facing state is
   split by owner: `MainViewModel.State.cs` owns shared shell/runtime flags and
   coordination gates, `MainViewModel.CaptureState.cs` owns capture-selection,
   source, and HDR state, `MainViewModel.AudioState.cs` owns audio/microphone/
   device-audio state, and `MainViewModel.FlashbackState.cs` owns Flashback
   timeline/export state. Keep the root `MainViewModel.cs` focused on the
   compatibility facade, dependency assignment, event subscription, and small
   bridge methods. Audio, microphone, and device-audio observable property
   handlers now live in `MainViewModel.AudioPropertyChanges.cs`. Shared
   dispatcher enqueue/invoke helpers now live in `MainViewModel.Dispatching.cs`,
   and live runtime text/timer/status/error handling now lives in
   `MainViewModel.Runtime.cs`. Capture settings projection from UI/runtime state
   now lives in `MainViewModel.CaptureSettings.cs`, leaving
   `MainViewModel.Capture.cs` focused on device/preview/reinitialize
   transitions. Recording toggle serialization, graceful stop, emergency stop,
   and start/stop recording transitions now live in
   `MainViewModel.RecordingLifecycle.cs`. Recording option selections, output
   path, counters, and transition flags now live in
   `MainViewModel.RecordingState.cs`. Bounded teardown and event unsubscription now live
   in `MainViewModel.Disposal.cs`. Automation-facing snapshot/probe/options
   projection now lives in `MainViewModel.AutomationSnapshots.cs`. Flashback
   playback commands, marker commands, and buffer/bitrate status projection now
   live in `MainViewModel.FlashbackPlayback.cs`. Flashback UI/automation export
   flow, progress/cancellation state, and segment projection now live in
   `MainViewModel.FlashbackExport.cs`. Frame-rate option rebuilding, source-rate
   filtering, and automatic frame-rate selection now live in
   `MainViewModel.FrameRateOptions.cs`. Shared frame-rate timing family,
   rational parsing, source-rate fallback, and preferred-format ranking now live
   in `MainViewModel.FrameRateTiming.cs`; keep device enumeration and selected
   device capability rebuilds in `MainViewModel.DeviceManagement.cs`. Pure
   recording codec filtering and selected-codec fallback policy now live in
   `Sussudio/ViewModels/RecordingFormatSelectionPolicy.cs`, while
   `MainViewModel.FormatSelection.cs`
   keeps collection mutation, HDR side effects, and selected capture-format
   policy.
   Late-arriving device format probe reconciliation and active-preview retarget
   checks now live in `MainViewModel.DeviceFormatProbes.cs`.
   Automatic resolution ranking, source-aware auto-selection, and auto-resolved
   dimension/frame-rate state now live in `MainViewModel.AutoResolutionOptions.cs`.
   Source-aware, HDR-aware, and SDR fallback resolution selection policy now
   lives in `MainViewModel.ResolutionSelectionPolicy.cs`; keep dropdown rebuild
   and effective resolution display in `MainViewModel.ResolutionOptions.cs`.
   Settings persistence and load/save option restoration stay in
   `MainViewModel.Settings.cs`; active Flashback reactions to recording format,
   encoder quality/preset/split/bitrate, and buffer/GPU decode changes now live
   in `MainViewModel.FlashbackSettings.cs`.
   UI-only automation mutators now live in `MainViewModel.AutomationUi.cs`.
   Recording format, encoder preset/quality/split-mode/custom-bitrate, and
   output-path automation mutators now live in
   `MainViewModel.AutomationRecordingSettings.cs`.
   Capture-mode automation mutators for resolution, frame rate, video format,
   and MJPEG decoder count now live in
   `MainViewModel.AutomationCaptureMode.cs`.
   Startup FFmpeg capability probes for recording formats and split-encode modes
   now live in `MainViewModel.RecordingCapabilityRefresh.cs`.
   Keep the remaining command mutation code in `MainViewModel.Automation.cs`.

5. Extract capture resource owners behind the transition policy.

   The policy is now the legality/steady-state owner. The next deeper capture
   slices should keep it authoritative while introducing smaller owners for
   audio graph, recording controller, Flashback backend resources, and video
   pipeline lifetime.

## Guardrails

- Preserve public automation command names and numeric IDs.
- Preserve manifest revision rules in `AutomationCommandKind`.
- Preserve XAML binding names until a focused binding migration changes them.
- Preserve Flashback disable lockout behavior.
- Preserve preview/recording no-restart semantics unless a test proves the
  transition intentionally restarts.
- Run `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore` after each
  structural slice.
- Run the console harness when source ownership, automation, capture, recording,
  or Flashback contracts move.

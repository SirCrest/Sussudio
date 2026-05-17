# Architecture Cleanup Plan

Last reviewed: 2026-05-16.

## Objective

Make the repo feel intentionally laid out and safe to change without moving
capture, preview, recording, Flashback, or automation behavior by vibes alone.
Performance and runtime semantics stay primary; file layout changes must earn
their keep with smaller ownership boundaries and passing checks.

## Granularity Guardrail

Do not split files just to make the line count smaller. A new file needs a
specific runtime or ownership reason: a distinct lifecycle, policy, protocol
contract, hot-path boundary, UI controller concern, or testable responsibility
that future agents can find faster by name. Sub-100-line files are acceptable
only when they carry one of those deliberate boundaries; otherwise keep the
code grouped and document the owner in place.

## Completed Slices

Automation contracts have been extracted into `Sussudio.Automation.Contracts/`.
This removes the old linked-source arrangement where app and tools compiled
protocol/catalog files from `tools/Common`.

Changed ownership:

- `AutomationCommandKind.cs`
- `AutomationCommandCatalog.cs`
- `AutomationCommandCatalog.Manifest.cs`
- `AutomationCommandCatalog.PathValidation.cs`
- `AutomationPipeProtocol.cs`
- `AutomationPipeSecurityPolicy.cs`

Diagnostic session scenario names still live in
`tools/Common/DiagnosticSessionScenarios.cs`, while scenario-level metadata now
lives in `tools/Common/DiagnosticSessionScenarioCatalog.cs`; the runner still
owns execution flow and summary writing.

Automation diagnostics now have named partial owners instead of one large hub
body. `AutomationDiagnosticsHub.cs` is the compact field/constructor owner.
`AutomationDiagnosticsHub.Counters.cs` owns recent-counter baseline state and
delta updates used by diagnostics evaluation. `AutomationDiagnosticsHub.Snapshots.cs` owns snapshot
refresh and read-only snapshot access. `AutomationDiagnosticsHub.SnapshotProjection.cs`
owns the `BuildAutomationSnapshot` shell and dispatch into projection
composition/flattening. `AutomationDiagnosticsHub.SnapshotProjection.Composition.cs`
owns projection-set composition from runtime/view-model snapshots and diagnostic
classifiers. `AutomationDiagnosticsHub.SnapshotProjection.Flattening.cs`
owns the final `AutomationSnapshot` DTO initializer that flattens the named
projection records into the automation wire snapshot. `AutomationDiagnosticsHub.SnapshotState.cs`
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
`AutomationDiagnosticsHub.SnapshotProjection.Audio.cs` owns view-model audio
signal projection, audio drop counter projection, derived real-time/file-writer
drop totals, and composes ingest and WASAPI projection owners into the
automation snapshot audio/ingest DTO fields.
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
capture cadence projection inputs consumed by the automation snapshot DTO.
`AutomationDiagnosticsHub.SnapshotProjection.VisualCadence.cs` owns preview
visual cadence and center-crop visual cadence projection inputs consumed by the
automation snapshot DTO.
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
including the export verification, codec-downgrade fallback, temp-drive, and
startup cache policy consumed by the automation snapshot DTO.
`AutomationDiagnosticsHub.SnapshotProjection.FlashbackRecordingQueues.cs` owns
Flashback video, GPU, and audio queue/backpressure projection consumed by the
automation snapshot DTO.
`AutomationDiagnosticsHub.SnapshotProjection.FlashbackPlayback.cs` owns
Flashback playback state, frame cadence metrics, audio-master delay/fallback,
seek-cap/decode timing, and playback command queue projection consumed by the
automation snapshot DTO.
`AutomationDiagnosticsHub.SnapshotProjection.PreviewD3D.cs` owns D3D preview
swap-chain, renderer state, submitted/rendered/dropped frame flow, waitable
frame-latency, and frame-statistics projection consumed by the automation
snapshot DTO.
`AutomationDiagnosticsHub.SnapshotProjection.PreviewD3DCpuTiming.cs` owns D3D
CPU upload/render/present/total-frame timing and pipeline latency projection
consumed by the automation snapshot DTO.
`AutomationDiagnosticsHub.SnapshotProjection.PreviewRuntime.cs` owns preview
frame counters, estimated pipeline latency, display-cadence, startup/readiness,
GPU playback state, preview HDR state, renderer mode, and preview color-context
projection consumed by the automation snapshot DTO.
`AutomationDiagnosticsHub.SnapshotProjection.ProcessResources.cs` owns process
memory, CPU, GC, and thread-pool projection consumed by the automation snapshot
DTO.
`AutomationDiagnosticsHub.SnapshotProjection.AvSync.cs` owns live A/V sync
drift and encoder correction projection consumed by the automation snapshot DTO.
`AutomationDiagnosticsHub.SnapshotProjection.RecordingIntegrity.cs` owns
recording-integrity projection inputs consumed by the automation snapshot DTO.
`AutomationDiagnosticsHub.SnapshotProjection.RecordingPipeline.cs` owns encoder
queue ages, conversion queue depths, and recording video/GPU/CUDA health inputs
consumed by the automation snapshot DTO.
`AutomationDiagnosticsHub.SnapshotProjection.RecordingOutput.cs` owns recording
backend/audio-path/mux-result projection, UI output text, accumulated recording
bytes, file-growth state, last finalized output metadata, and last verification
result projection consumed by the automation snapshot DTO.
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
pipeline parity, telemetry-alignment, and HDR truth verdict projection consumed
by the automation snapshot DTO.
`AutomationDiagnosticsHub.Alerts.cs` owns alert rule evaluation and active-alert
transitions. `AutomationDiagnosticsHub.SignalAlerts.cs` owns signal alert
orchestration plus preview, capture, audio, and recording signal alert rules.
`AutomationDiagnosticsHub.Alerts.cs` also routes Flashback recording and
playback alert groups. `AutomationDiagnosticsHub.FlashbackRecordingAlerts.cs`
owns Flashback recording alert orchestration, shared condition setup, export
progress and force-rotation gap alerts, temp-cache pressure alerts, encoder
failure alerts, and recording path degradation alerts.
`AutomationDiagnosticsHub.FlashbackPlaybackAlerts.cs` owns Flashback playback
alert orchestration, playback command queue and command failure alerts, playback
performance orchestration, audio-master fallback and audio-queue backlog alerts,
and frame-submission failure alerts.
`AutomationDiagnosticsHub.FlashbackPlaybackPerformanceAlerts.Cadence.cs` owns
playback target-rate, present-cadence, slow-playback, and frametime alert
rules.
`AutomationDiagnosticsHub.DiagnosticEvents.cs` owns diagnostics event
publication, event throttling, Flashback export completion events, and recent
event storage.
`AutomationDiagnosticsHub.DiagnosticEvaluation.cs` owns diagnostic verdict
orchestration and the final healthy/mixed fallback.
`AutomationDiagnosticsHub.DiagnosticEvaluationFlashback.cs` owns Flashback-specific
diagnostic verdict ordering.
`AutomationDiagnosticsHub.DiagnosticEvaluationFlashback.Storage.cs` owns
Flashback storage pressure diagnostic verdicts.
`AutomationDiagnosticsHub.DiagnosticEvaluationFlashback.Recording.cs` owns
Flashback recording diagnostic verdict ordering plus encoder failure,
export-rotation gap, backend staleness, and recording degradation verdicts.
`AutomationDiagnosticsHub.DiagnosticEvaluationFlashback.RecordingConditions.cs`
owns Flashback recording diagnostic condition assembly.
`AutomationDiagnosticsHub.DiagnosticEvaluationFlashback.Export.cs` owns active
and stalled Flashback export diagnostic verdicts.
`AutomationDiagnosticsHub.DiagnosticEvaluationFlashback.Playback.cs` owns
Flashback playback command, performance, frametime, and submission diagnostic
verdicts.
`AutomationDiagnosticsHub.DiagnosticEvaluationRealtime.cs` owns realtime
diagnostic verdict ordering.
`AutomationDiagnosticsHub.DiagnosticEvaluationRealtime.State.cs` owns idle and
warmup diagnostic verdicts.
`AutomationDiagnosticsHub.DiagnosticEvaluationRealtime.Recording.cs` owns
recording integrity and audio integrity diagnostic verdicts.
`AutomationDiagnosticsHub.DiagnosticEvaluationRealtime.Source.cs` owns
source/capture cadence diagnostic verdicts.
`AutomationDiagnosticsHub.DiagnosticEvaluationRealtime.Mjpeg.cs` owns MJPEG
duplicate source-signal and decode/reorder diagnostic verdicts.
`AutomationDiagnosticsHub.DiagnosticEvaluationRealtime.Preview.cs` owns realtime
preview diagnostic verdict ordering plus the renderer pacing verdict.
`AutomationDiagnosticsHub.DiagnosticEvaluationRealtime.PreviewScheduler.cs` owns
preview scheduler diagnostic verdicts.
`AutomationDiagnosticsHub.DiagnosticEvaluationRealtime.PreviewPresent.cs` owns
present/display cadence and preview display 1% low diagnostic verdicts.
`AutomationDiagnosticsHub.DiagnosticEvaluationLanes.cs` owns diagnostic lane text
orchestration and lane DTOs used by diagnostic verdicts.
`AutomationDiagnosticsHub.DiagnosticEvaluationLanes.Realtime.cs` owns source,
MJPEG decode/source-signal, preview scheduler/render/present/visual, recording,
and audio diagnostic lane text formatting.
`AutomationDiagnosticsHub.DiagnosticEvaluationLanes.Flashback.cs` owns
Flashback recording, export, temp-cache, playback-command, and playback
performance diagnostic lane text formatting.
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
`PreviewPacingClassificationModels.cs` owns the preview pacing DTOs,
`PreviewPacingSlowStageClassifier.cs` owns classification ordering and non-D3D
lane policy, and `PreviewPacingSlowStageClassifier.D3D.cs` owns D3D stage
dominance policy.
`AutomationDiagnosticsHub.ProcessMetrics.cs` owns process CPU, memory, GC, and
thread-pool sampling.
`AutomationDiagnosticsHub.Verification.cs` owns recording/file verification
commands, automatic post-recording verification scheduling, and
recording-start verification reset, and verification-profile adaptation.

Automation command dispatch now keeps the root router focused on the command
envelope: manifest revision checks, auth/readiness gates, trivial-handler
dispatch, and error shaping. `AutomationCommandDispatcher.CustomCommands.cs`
owns the custom command switch/router for commands that need multi-field
payloads, special response shapes, or capture/Flashback routing.
`AutomationCommandDispatcher.AudioControlCommands.cs` owns device-audio mode,
analog audio gain, and microphone-enable command bodies behind the custom
command router.
`AutomationCommandDispatcher.CaptureControlCommands.cs` owns MJPEG decoder
count, output-path, and recording-enable command bodies, including the
recording-response snapshot refresh, behind the custom command router.
`AutomationCommandDispatcher.DiagnosticCommands.cs` owns diagnostic readback
command bodies for recent events, performance timeline, and audio ramp traces
behind the custom command router.
`AutomationCommandDispatcher.IntrospectionCommands.cs` owns read-only snapshot
and manifest response command bodies behind the custom command router.
`AutomationCommandDispatcher.DeviceCommands.cs` owns device refresh,
capture-device selection, audio-input selection, and capture-options readback
command bodies behind the custom command router.
`AutomationCommandDispatcher.UiSettingsCommands.cs` owns UI settings command
bodies, including stats section visibility, behind the custom command router.
`AutomationCommandDispatcher.FlashbackCommands.cs` owns Flashback action,
export, segment, restart, and enable command bodies behind the custom command
router.
`AutomationCommandDispatcher.VerificationCommands.cs` owns file and
last-recording verification command bodies.
`AutomationCommandDispatcher.VisualCaptureCommands.cs` owns video-source probe,
preview-color probe, preview-frame capture, window screenshot capture, default
capture output paths, and capture response status shaping behind the custom
command router.
`AutomationCommandDispatcher.TrivialHandlers.cs` owns the simple one-property
command table. Named partials own support responsibilities:
`AutomationCommandDispatcher.Authorization.cs` handles auth-token lookup and
constant-time comparison;
`AutomationCommandDispatcher.CommandParsing.cs` handles command metadata,
path-validation forwarding, and enum payload parsing;
`AutomationCommandDispatcher.Responses.cs` handles response shaping and
Flashback rejection diagnostics; `AutomationCommandDispatcher.WindowActions.cs`
handles low-level window automation action execution;
`AutomationCommandDispatcher.WindowCommands.cs` handles full-screen,
recordings-folder, arm-close, and window-action command bodies, including
close-arm gating; `AutomationCommandDispatcher.WaitConditions.cs` handles
WaitForCondition response shaping, wait polling, and snapshot predicates; and
`AutomationCommandDispatcher.Assertions.cs` handles AssertSnapshot response
shaping, parsing, and comparison helpers. `AutomationCommandDispatcher.Payload.cs` owns JSON payload
extraction helpers, and `AutomationCommandHandler.cs` owns the reusable
trivial-handler wrapper plus the payload field name/type metadata checked
against the shared automation command catalog.

Automation pipe hosting is split across `NamedPipeAutomationServer.*.cs`.
Keep constructor/configuration state in the root file, server start/stop and
accept-loop behavior in `NamedPipeAutomationServer.Lifecycle.cs`, per-connection
JSON framing and dispatch timeouts in `NamedPipeAutomationServer.Connections.cs`,
Windows pipe security/PInvoke in `NamedPipeAutomationServer.Security.cs`, and
error/timeout responses plus fallback tracing in `NamedPipeAutomationServer.Responses.cs`.

App project build workflow is split so `Sussudio/Sussudio.csproj` stays focused
on app identity, assets, packages, runtime config, and project references, while
`Sussudio/Sussudio.Build.targets` owns publish flags, English-only locale
stripping, and repo-local `latest-build` staging.

`tools/ssctl/CommandHandlers.cs` is now only the top-level CLI router.
`CommandHandlers.Observability.cs` owns state, diagnostics, options, manifest,
timeline, memory, and audio-ramp commands. `CommandHandlers.PresentMon.cs`
owns `presentmon` command parsing, swap-chain discovery, and probe invocation.
`CommandHandlers.DiagnosticSession.cs` owns `diagnostic-session` command
parsing and runner invocation.
`tools/ssctl/Program.cs` owns only process entry, Ctrl-C cancellation, CLI
option parsing, and exit-code shaping; `tools/ssctl/SsctlHelpWriter.cs` owns
the help facade, `tools/ssctl/SsctlHelpWriter.Sections.cs` owns
operator-facing help section text, and `tools/ssctl/SsctlHelpWriter.Catalog.cs`
owns catalog-backed help lines.
`CommandHandlers.CaptureControls.cs` owns preview/record/screenshot/frame and
`set` capture/audio/output mutations, including the shared set-value payload
helper. `CommandHandlers.Device.cs` owns device
refresh/list/select, audio-input selection, and custom-audio enablement.
`CommandHandlers.Window.cs` owns window close arming, state/geometry actions,
fullscreen toggles, snap commands, and the recordings-folder CLI command.
`CommandHandlers.AutomationFlow.cs` owns
wait/assert/probe scripting flow commands. `CommandHandlers.UiVisibility.cs`
owns stats, settings, and frame-time visibility commands.
`CommandHandlers.Verification.cs` owns recording/file verification commands.
`CommandHandlers.Flashback.cs` owns Flashback enablement, timeline, segment,
restart, and top-level Flashback command routing.
`CommandHandlers.Flashback.Actions.cs` owns Flashback playback/scrub/marker/
range CLI actions, position parsing, and `FlashbackAction` payload shaping.
`CommandHandlers.Flashback.Export.cs` owns Flashback export flags, output path
defaulting, directory creation, and payload shape. Support partials remain:
`CommandHandlers.Context.cs` owns
per-invocation command context,
`CommandHandlers.Flags.cs` owns flag consumption and optional flag values,
`CommandHandlers.Arguments.cs` owns usage validation and argument joining,
`CommandHandlers.Json.cs` owns JSON detection/pretty-printing, `CommandHandlers.Values.cs`
owns primitive/domain value parsing, and `CommandHandlers.Transport.cs` owns
shared command sending plus response exit-code shaping. Command-family payload
helpers stay with their owning command partials.

`tools/ssctl/Formatters.cs` is only the projection facade for console output.
Keep app snapshot orchestration and section ordering in `Formatters.Snapshot.cs`,
state/capture-command, audio, recording, legacy performance, and Memory/GC text
in `Formatters.Snapshot.CoreSections.cs`, capture settings and friendly/exact
frame-rate text in `Formatters.Snapshot.CaptureSettings.cs`, video-pipeline
text in `Formatters.Snapshot.VideoPipeline.cs`, diagnostic health/frame-lane
text in `Formatters.Snapshot.DiagnosticLanes.cs`, capture cadence text in
`Formatters.Snapshot.CaptureCadence.cs`, Flashback snapshot gating/order,
encoder/buffer/queue, playback/cadence/drift, and export text in
`Formatters.Snapshot.Flashback.cs`, embedded snapshot AV-sync drift text in
`Formatters.Snapshot.AvSync.cs`, MJPEG timing text in
`Formatters.Snapshot.Mjpeg.cs`, preview renderer-mode routing and non-D3D
fallback text in `Formatters.Snapshot.Preview.cs`, D3D preview renderer text in
`Formatters.Snapshot.PreviewD3D.cs` including routing/header order, CPU timing,
pipeline latency, frame ownership, frame-latency wait, DXGI frame-stat text, and
delegation to the shared slow-frame formatter, thread-health text in
`Formatters.Snapshot.ThreadHealth.cs`, source telemetry snapshot text in
`Formatters.Snapshot.Source.cs`, diagnostic-event text in
`Formatters.Diagnostics.cs`, capture option/device text in `Formatters.Options.cs`,
performance timeline orchestration in `Formatters.Timeline.cs`, timeline row
projection in `Formatters.Timeline.Rows.cs`, the private row model in
`Formatters.Timeline.Rows.Model.cs`, table output in
`Formatters.Timeline.Rendering.cs`, trend summaries in
`Formatters.Timeline.Summaries.cs`, standalone memory/GC summaries in
`Formatters.Memory.cs`, and shared JSON/result helpers in
`Formatters.Common.cs`.

`tools/Common/AutomationSnapshotFormatter.cs` is now the shared automation
snapshot formatter facade for top-level text flow. State/audio/recording/
performance/memory core sections live in
`AutomationSnapshotFormatter.CoreSections.cs`; capture settings,
video pipeline, diagnostics, and capture cadence text live in
`AutomationSnapshotFormatter.CaptureSettings.cs`,
`AutomationSnapshotFormatter.VideoPipeline.cs`,
`AutomationSnapshotFormatter.Diagnostics.cs`,
`AutomationSnapshotFormatter.CaptureCadence.cs`. Tolerant JSON accessors live in
`AutomationSnapshotFormatter.Values.cs`, while byte/number/interval,
frame-budget, and tick-age display helpers live in
`AutomationSnapshotFormatter.DisplayValues.cs`; the Flashback gate/header/order
lives in `AutomationSnapshotFormatter.Flashback.cs` with encoder, buffer,
cache, queue, failure, playback, and export text kept together. Capture cadence
also owns its AV-sync and source-signal leaf sections because those sections are
only emitted from the cadence tail. MJPEG timing, preview routing, D3D preview
text, and thread-health live in the remaining focused formatter partials. The
`AutomationSnapshotFormatter.PreviewD3D.cs` owner keeps D3D header/routing,
CPU timing, frame-flow, frame-latency wait, and DXGI frame stats together while
preserving output order. Slow-frame diagnostics stay in
`AutomationSnapshotFormatter.PreviewD3D.SlowFrames.cs` because `ssctl` reuses
that formatter directly. Tests that
reason about formatter source use `ReadAutomationSnapshotFormatterSource()` so
ownership checks cover the full partial family.

Diagnostic-session MCP surface coverage is split into
`McpToolSurface.DiagnosticSession.Tool.Tests.cs` for MCP tool
artifact contracts, `McpToolSurface.DiagnosticSession.Ownership.Tests.cs` for
core helper ownership assertions,
`McpToolSurface.DiagnosticSession.Flashback.*.Tests.cs` for Flashback
scenario/metrics/wait/export ownership assertions,
`McpToolSurface.DiagnosticSession.InfrastructureOwnership.*.Tests.cs` for
focused infrastructure ownership tests, and
`McpToolSurface.DiagnosticSession.Runner.*.Tests.cs` for focused reflective
runner behavior tests. The runner behavior files now own
final-snapshot artifact failures, sparse source-cadence health tolerance,
Flashback export/playback command flow, unknown-initial-snapshot mutation
safety, synthetic pipe-connect retry, and concurrent-output-directory lockout.
Infrastructure ownership files now split runner/initial-snapshot, pipe
retry/command channel, run context, and scenario/completion phase assertions.

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

Automation view-model regression coverage is split into preview-volume persistence, recording
transition routing, async Flashback/probe surface assertions, diagnostics
refresh, diagnostics projection ownership, runtime-safety behavior, audio
command guards, UI settings, capture-mode routing, and Flashback cleanup
ownership partials. Keep new automation tests in the closest owner file instead
of regrowing the root catch-all.

`tests/Sussudio.Tests/MainViewModel.Capture.TestHelpers.cs` owns shared
capture-facing MainViewModel source-inspection helpers. Capture regression
coverage is split across the `tests/Sussudio.Tests/MainViewModel.Capture.*.cs`
family, including preview startup, Flashback export locking, Flashback
coordinator/UI routing, Flashback backend lifecycle, capture selection policy,
output path, audio monitoring, reinitialization, and Flashback
frame-rate/enable-disable owner files.

`tests/Sussudio.Tests/SnapshotModels.Tests.cs` is now the shared snapshot-model
reflection/spec helper shell. Snapshot model contract coverage is split into
CaptureDiagnosticsSnapshot, CaptureHealthSnapshot, and
SourceSignalTelemetrySnapshot owner files.

`tests/Sussudio.Tests/RecordingQueue.Tests.cs` is now the shared recording
queue source-reader helper shell. Recording queue coverage is split into queue
overload policy, LibAv sink, WASAPI, and capture fan-out / Flashback backend
owner files.

D3D preview renderer coverage is
split into geometry/screenshot helper contracts, cadence contracts, the large
diagnostics contract, device-lost behavior, and frame-flow/shared-device
assertions. Source ownership coverage lives in focused ContractsAndMetrics,
RenderPipeline, RenderThread, RuntimeCapture owner files.

`tests/Sussudio.Tests/AutomationToolContracts.Tests.cs` now keeps only shared
reflection helpers. Pure `Sussudio.Automation.Contracts` command ID, manifest
ID, protocol resolution, timeout/auth/envelope, and `CommandMap` checks now
have fast xUnit coverage in
`tests/Sussudio.Tests/AutomationContracts.ProtocolXunit.Tests.cs`, backed by
the single golden command table in
`tests/Sussudio.Tests/AutomationCommandGoldenTable.cs`. The legacy protocol
harness stays focused on pipe-failure contracts, tool delegation, script
freshness, and response-state parsing. Automation tool contract coverage is
otherwise split into catalog/manifest/path-policy contracts, reliability-gates
script checks, shared/ssctl snapshot formatter contracts, and PresentMon parser
contracts. Shared formatter tests now mirror the formatter partials: the root
snapshot-formatter test owns accessors, invalid-response handling, section
ordering, core section formatting, and the Flashback opt-in gate; Flashback
output, Preview D3D output, and source ownership live in focused
`AutomationToolContracts.SnapshotFormatter.*.Tests.cs` owners registered with
the offline harness. ssctl formatter output smoke checks stay in
`Formatters.Tests.cs`, while `Formatters.SnapshotOwnership.Tests.cs` owns ssctl
formatter source ownership assertions.
ssctl command-handler routing coverage now lives in one grouped
`CommandHandlers.Routing.Tests.cs` owner for device, capture controls,
recordings, Flashback, window, manifest, observability, automation-flow, UI
visibility, and verification commands, with source ownership kept separate in
`CommandHandlers.SourceOwnership.Tests.cs`. Captured ssctl
`request.command` ID assertions now flow through `AssertSsctlCommandRequest`,
which delegates to the shared golden-table-backed `AssertAutomationCommandId`
helper instead of duplicating numeric IDs in routing tests. Fixed ssctl source
guards also live in `CommandHandlers.SourceOwnership.Tests.cs`; they require
`AutomationCommandKind` enum overloads at routing call sites while leaving
labels and wire IDs catalog-backed, with the dynamic diagnostic-session runner
channel intentionally remaining string-based.
`tests/Sussudio.Tests/ArchitectureDocs.AgentMapReferences.Tests.cs` owns
AGENT_MAP reference resolution.
`tests/Sussudio.Tests/ArchitectureDocs.SourceReferencePaths.Tests.cs` owns
literal `ReadRepoFile` source-shape path resolution.
`tests/Sussudio.Tests/ArchitectureDocs.AgentMapOwnershipPaths.Tests.cs` owns
test-owner code-span coverage.
`tests/Sussudio.Tests/ArchitectureDocs.AgentMapAutomation.Tests.cs` owns
automation consumer checklist coverage.
`tests/Sussudio.Tests/ArchitectureDocs.AgentMapPresentation.Tests.cs` owns
UI/presentation ownership coverage.
`tests/Sussudio.Tests/ArchitectureDocs.AgentMapCaptureRuntime.Tests.cs` owns
CaptureService ownership coverage.
`tests/Sussudio.Tests/ArchitectureDocs.AgentMapToolAutomation.Tests.cs` owns
shared tool automation path coverage.
`tests/Sussudio.Tests/ArchitectureDocs.CleanupPlanReferences.Tests.cs` owns
cleanup-plan file/folder reference drift checks and architecture-doc test-family
coverage.
`tests/Sussudio.Tests/ArchitectureDocs.AgentMapHelpers.cs` owns the shared
AGENT_MAP token, consumer, and file-family helper logic.
Shared tool assembly loading and stale-build detection now live in
`tests/Sussudio.Tests/ToolAssemblyLoading.Helpers.cs` so the legacy harness body
no longer owns tool DLL resolution or freshness policy.
Shared harness helpers now live in focused owners instead of the legacy harness
body: repo-file/source text helpers live in
`tests/Sussudio.Tests/HarnessCore.SourceText.cs`, reflection/property access
lives in `tests/Sussudio.Tests/HarnessCore.Reflection.cs`, assertions live in
`tests/Sussudio.Tests/HarnessCore.Assertions.cs`, wait helpers live in
`tests/Sussudio.Tests/HarnessCore.AsyncLifecycle.cs`, and synthetic
capture/recording object factories live in
`tests/Sussudio.Tests/HarnessCore.ObjectFactories.cs`.
Synthetic MJPEG timing metric factories and the closed-pipeline emit delegate
now live in `tests/Sussudio.Tests/MjpegTimingMetrics.Helpers.cs`.

`tests/Sussudio.Tests/CaptureConfigurationModels.Tests.cs` now keeps only
shared reflection helpers. Capture configuration model coverage is split into
capture mode options, capture settings/MJPEG HFR policy, encoder support,
Flashback DTO contracts, and recording pipeline option contracts.

`tests/Sussudio.Tests/PooledVideoFrame.Tests.cs` now keeps only shared
pooled-frame and jitter-buffer helpers. Pooled-frame coverage is split into
lease lifecycle/fan-out contracts, MJPEG jitter frame-ingress/adaptive policy,
MJPEG jitter queue/drop/reprime behavior, and queued lease release contracts
for D3D, recording, and Flashback paths.

Projection ownership checks are split into snapshot/status, audio, capture and
source, MJPEG, recording, system resources and A/V sync, preview, and Flashback
owner files.

Fullscreen transition mechanics now live under the
`Sussudio/Controllers/FullScreen/FullScreenController.*.cs` family. Keep the
root controller to the public toggle/state surface,
`FullScreenController.Transitions.cs` to enter/exit orchestration,
`FullScreenController.Animation.cs` to rect animation,
`FullScreenController.Chrome.cs` to chrome/material state, and
`FullScreenController.Controls.cs` to overlay pointer/auto-hide behavior.
`MainWindow.FullScreen.cs` remains the XAML event adapter.
`MainWindow.FullScreenFlashbackBridge.cs` owns the Flashback fullscreen keyboard
gate/adapter, timeline visibility, and scrub-end bridging.

Automation whole-window screenshot capture now lives in
`Sussudio/Controllers/Screenshot/Window/WindowScreenshotController.cs`, which now only owns
UI-thread dispatch, cancellation, and failure wrapping. Native PrintWindow/GDI
capture and screenshot result shaping live in
`Sussudio/Controllers/Screenshot/Window/WindowScreenshotNativeCapture.cs`, while pure PNG/BMP
byte-stream encoding lives in
`Sussudio/Controllers/Screenshot/Window/WindowScreenshotImageEncoder.cs`. `MainWindow.Screenshot.cs`
is only the automation adapter.

Preview-frame screenshot button behavior now lives in
`Sussudio/Controllers/Screenshot/Preview/PreviewScreenshotController.cs`.
`Sussudio/Controllers/Screenshot/Preview/PreviewScreenshotPlanPolicy.cs` owns the pure output
directory fallback, file naming, status text, and log text policy.
`MainWindow.PreviewScreenshot.cs` is the XAML-facing adapter; the controller
keeps directory creation, preview-frame capture, logging side effects, and
button enable/disable state.
Renderer-level preview frame capture request state and timeout/cancellation
handling now live with the capture implementation in
`Sussudio/Services/Preview/D3D11PreviewRenderer.ScreenshotCapture.cs`.
Screenshot BMP/error result construction, mapped-frame buffer copying, and
capture pixel statistics now live in
`Sussudio/Services/Preview/D3D11PreviewRenderer.ScreenshotEncoding.cs`.

Window geometry automation and the recordings-folder command now live in
`Sussudio/Controllers/Window/WindowAutomationController.cs`. Display-area/AppWindow
access, UI-thread dispatch, presenter restore, and side effects stay there, while
`Sussudio/Controllers/Window/WindowSnapRegionLayoutPolicy.cs` owns the pure snap-region
rectangle math for window actions.
`MainWindow.WindowAutomation.cs` is the `IAutomationWindowControl` adapter.
Close lifecycle state remains separate from geometry automation; see the
explicit window close lifecycle section below for the close-state and recording
finalization owners.

UI-thread dispatching helpers, preview-snapshot-style result dispatch with
three-attempt enqueue retry, and guarded async event-handler execution now live
in `Sussudio/Controllers/Window/WindowUiDispatchController.cs`.
`Sussudio/MainWindow.Dispatching.cs` keeps the stable private MainWindow adapter
names for callers. Window close completion, close-request dispatch, and
recording finalization are covered by the explicit window close lifecycle
section below.

MCP command-routing coverage is split into capture, host/pipe, recording,
formatter batching, device, pipeline,
UI, and verification owner files. Captured `request.command` ID assertions now
flow through `AssertAutomationCommandId`, which reads the golden command table
instead of duplicating numeric IDs in routing tests. Cross-tool source guards
in `McpToolSurface.Tests.cs` require fixed-command MCP automation routes to use
`AutomationCommandKind` enum overloads at the pipe seam while preserving existing
labels and wire IDs. Catalog/manifest-backed dynamic batches and
diagnostic-session runner command-channel delegates intentionally remain
string-based.

First-load startup, initial ViewModel/device refresh, automation startup timing,
and the launch entrance trigger now live in `Sussudio/MainWindow.Startup.cs`.
Automation host composition, once-only
startup, ready/disabled logging, and pipe-before-hub shutdown disposal now live
in `Sussudio/Controllers/Window/WindowAutomationHostLifecycleController.cs`, with
`Sussudio/MainWindow.AutomationHost.cs` kept as the shell adapter. Window close
completion lives in `Sussudio/Controllers/Window/WindowCloseLifecycleController.cs`;
recording-aware close finalization now lives in
`Sussudio/Controllers/Window/WindowCloseRecordingFinalizationController.cs`.

Top-level shell resize telemetry throttling for preview compositor transforms
now lives in `Sussudio/Controllers/Preview/PreviewResizeTelemetryController.cs`.
`Sussudio/MainWindow.WindowSizing.cs` is the `SizeChanged` adapter. Preview
surface sizing and GPU panel visibility now live in
`Sussudio/Controllers/Preview/PreviewSurfacePresentationController.cs`, while
video/control-bar composition shadow visuals, bounds alignment, clear behavior,
and fade routing live in
`Sussudio/Controllers/Preview/PreviewSurfaceShadowController.cs`.
`Sussudio/MainWindow.PreviewSurface.cs` is the XAML-facing adapter.
`Sussudio/Controllers/Preview/Renderer/PreviewRendererStartupPlanBuilder.cs` owns renderer
startup dimension/fps/HDR/min-present-interval planning.
`Sussudio/Controllers/Preview/Renderer/PreviewRendererHostController.cs` owns hosted preview
renderer context, public runtime state, counters, and simple renderer surface
methods. `Sussudio/Controllers/Preview/Renderer/PreviewRendererHostController.Lifecycle.cs` owns
start/stop/shutdown flow, renderer startup planning, and cleanup.
`Sussudio/Controllers/Preview/Renderer/PreviewRendererHostController.D3D.cs` owns D3D renderer
startup and event/failure handling.
`Sussudio/Controllers/Preview/Renderer/PreviewRendererHostController.Cpu.cs` owns CPU preview
fallback attachment. `Sussudio/Controllers/Preview/Renderer/PreviewRendererHostController.Reinit.cs`
owns D3D reinit disposal, unsafe-window telemetry, stop tick accounting, fresh
SwapChainPanel replacement, and retired-renderer handoff during D3D renderer
mode switches. `MainWindow.PreviewRenderer.cs` is the XAML-facing host/reinit
adapter surface.
`Sussudio/MainWindow.PreviewRuntimeSnapshot.cs` owns the stable automation
preview snapshot UI-dispatch adapter and UI-thread-only preview state sampling.
Read-only preview runtime snapshot construction now lives in
`Sussudio/Controllers/Preview/Renderer/PreviewRuntimeSnapshotController.cs`,
which owns preview-state composition and blank/stall suspicion.
`Sussudio/Controllers/Preview/Renderer/PreviewRuntimeD3DProjection.cs` owns
renderer frame-counter override, cadence projection, D3D diagnostic fields,
estimated pipeline latency, and GPU playback projection.
Close routing/finalization handling remains in the explicit window close
lifecycle owners below.

Window title base/build-stamp formatting and the recording-time suffix now live
in `Sussudio/Controllers/Window/WindowTitleController.cs`; `MainWindow.StatusStripPresentation.cs`
keeps the XAML-facing initialization and title assignment hook because title
refreshes are driven by status/recording presentation.

Window close lifecycle and native window helpers are now explicit:
`Sussudio/Controllers/Window/WindowCloseLifecycleController.cs` owns close request
flags, completion TCS, cleanup latch, close-in-progress classification, and
automation close dispatch orchestration.
`Sussudio/Controllers/Window/WindowCloseRequestController.cs` owns actual close
request execution: `Close()`, completion timing after non-recording closes,
close-in-progress success handling, COM `Application.Current.Exit()` fallback,
and requested-state reset after unexpected failures.
`Sussudio/Controllers/Window/WindowCloseRecordingFinalizationController.cs` owns the
recording finalization side effects during pre-close and post-close cleanup:
the 120-second stop budget, `StopRecordingAndWaitAsync` wait race, timeout/
failure breadcrumbs, status text, and shutdown-content dim/restore policy.
`Sussudio/MainWindow.CloseLifecycle.cs` owns `AppWindow.Closing`,
recording-aware pre-close cancellation/completion choreography, and the stable
`RequestWindowClose()` adapter.
`Sussudio/Controllers/Window/WindowShutdownCleanupController.cs` owns post-`Closed`
cleanup order: cleanup latch, close completion, closing-state mark, timer stops,
event detaches, preview shutdown, post-close recording finalization handoff,
automation diagnostics disposal, NVML disposal, and ViewModel disposal.
`Sussudio/MainWindow.ShutdownCleanup.cs` is the XAML-facing `Closed` adapter and
wires MainWindow cleanup delegates into the controller.
Native `AppWindow` lookup, ViewModel window handle handoff, minimum-size
subclassing, DWM cloak/dark-mode setup, first-composed-frame shell reveal
scheduling/cancellation, initial shell size, icon, and uncloaking now live in
`Sussudio/Controllers/Window/NativeWindowBootstrapController.cs`.
`Sussudio/MainWindow.NativeWindow.cs` is the XAML-facing adapter and keeps the
`_hwnd` field consumed by screenshot and window automation paths.
MainWindow shell ownership tests mirror these runtime owners through focused
`MainWindow.ShellOwnership.*.Tests.cs` files for chrome, startup, preview
runtime, and window lifecycle contracts.
MainWindow Flashback ownership tests mirror the Flashback controller owners
through focused `MainWindow.FlashbackOwnership.*.Tests.cs` files: the root is
only the marker shell, while polling, timeline layout, playhead/CTI motion,
marker presentation, playback presentation/coordinator behavior, export
progress, and settings/command binding each have a named test owner.

Audio and microphone meter rendering now lives in the
`Sussudio/Controllers/Audio/Meter/AudioMeterController*.cs` family: the root
controller owns setup, `AudioMeterController.Context.cs` owns XAML/view-model
dependencies, `AudioMeterController.MeterState.cs` owns smoothing, markers,
resets, timer lifetime, and `TranslateMarker`, and
`AudioMeterController.PresentationAnimations.cs` owns monitoring/disabled
animations and rounded clips. Audio/microphone initial control projection and
event hookup now live in
`Sussudio/Controllers/Audio/AudioControlBindingController.cs`: it owns the
audio-control binding context, initial audio/microphone projection,
preview-volume binding and priming, audio/microphone/device-audio selection
handlers, record/preview/custom-audio/microphone toggle handlers, audio-meter
activation, initial meter presentation, and device-audio gain/meter resize
hooks. `Sussudio/MainWindow.AudioBindings.cs` is the XAML-facing adapter;
video-format collection setup, initial capture/recording option projection, and
code-attached resolution/frame-rate handlers now live in
`Sussudio/Controllers/Capture/CaptureOptionBindingController.cs`, with
`MainWindow.CaptureOptionBindings.cs` left as the XAML-facing capture and
recording option adapter.
Flashback settings-control initialization, GPU decode binding/sync, and buffer
duration combo sync now live in
`Sussudio/Controllers/Flashback/FlashbackSettingsBindingController.cs`.
The remaining non-audio control-bar binding code stays in `MainWindow.Bindings.cs`.

Capture session transition legality now lives in
`Sussudio/Models/Capture/CaptureSessionTransitionPolicy.cs`. `CaptureService`
uses it before entering a transition and delegates steady-state resolution to
the same pure policy; resource ownership has not moved in this slice.
Capture session coordinator command enums, queue receipt records, session
snapshots, and Flashback playback/buffer status projections now live in
`Sussudio/Services/Capture/CaptureSessionCoordinator.Models.cs`.
`CaptureSessionCoordinator.cs` owns construction and shared state fields.
`CaptureSessionCoordinator.Commands.cs` owns the public non-Flashback
lifecycle/audio command facade into the serialized worker. Queue work item
creation, command enqueueing, worker-loop execution, coalescing,
cancellation/failure accounting, and pending-command counters now live in
`CaptureSessionCoordinator.Queue.cs`.
Queue/session snapshot projection, last-command state, pending-command age
bookkeeping, and queue latency accounting now live in
`CaptureSessionCoordinator.Snapshot.cs`. Dispose/drain/cancel lifecycle for the
worker queue and cancellation token source now lives in
`CaptureSessionCoordinator.Disposal.cs`.
Capture session coordinator API/command/snapshot contracts, focused
source-ownership contracts, queue behavior, Flashback/cancellation behavior,
transition policy, and shared reflection harness helpers now live in separate
named files.
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

Native XU Kernel Streaming calls are grouped under
`Sussudio/Services/Capture/NativeXu/`. Keep constants and DTOs in the root,
SetupAPI interface enumeration in `.Interfaces.cs`, handle opening in
`.Handles.cs`, topology node parsing in `.Topology.cs`, XU GET/SET transfer
shapes in `.Transfers.cs`, and P/Invoke struct declarations in `.Interop.cs`.
`tools/NativeXuAudioProbe` links this whole partial family explicitly, so
update its project file with every new partial.

Native device enumeration ownership is grouped under
`Sussudio/Services/Capture/DeviceDiscovery/`. Keep shared Media Foundation
constants, GUIDs, and P/Invoke declarations in `MfDeviceEnumerator.cs`, MF
video-device enumeration in `MfDeviceEnumerator.VideoDevices.cs`, WASAPI capture
endpoint enumeration and friendly-name reads in
`MfDeviceEnumerator.AudioEndpoints.cs`, and native video format probing/source
fallback/subtype naming in `MfDeviceEnumerator.FormatProbe.cs`.

Capture service source telemetry polling and fallback merging now live in
`Sussudio/Services/Capture/CaptureService.Telemetry.cs`. Capture-format runtime
telemetry, NTSC frame-rate correction, and frame-rate argument formatting now
live in
`Sussudio/Services/Capture/CaptureService.CaptureFormatTelemetry.cs`.
Observed pixel-format normalization, resets, and explicit counter updates now live in
`Sussudio/Services/Capture/CaptureService.ObservedPixelTelemetry.cs`. The root
capture service owns shared state, construction, and public event surface, but
these diagnostics are no longer embedded in the lifecycle/orchestration file.

Capture service initialization now lives in
`Sussudio/Services/Capture/CaptureService.Initialization.cs`. That file owns
the public initialization transition, initial selected device/settings capture,
negotiated-format seeding, the initial observed-pixel telemetry reset call,
fallback source telemetry, source telemetry refresh, NTSC frame-rate correction,
and initialized status event.

Capture preview volume/mute and WASAPI audio-level/failure event projection now
live in `Sussudio/Services/Capture/CaptureService.Audio.cs`. Audio-preview
start/stop lifecycle lives in
`Sussudio/Services/Capture/CaptureService.AudioPreviewLifecycle.cs`, and live
audio input switching lives in
`Sussudio/Services/Capture/CaptureService.AudioInputSwitching.cs`. Preview-time
microphone monitoring lives in
`Sussudio/Services/Capture/CaptureService.MicrophoneMonitor.cs`, and WASAPI
playback attach/detach ordering lives in
`Sussudio/Services/Capture/CaptureService.WasapiPlayback.cs`. These files
preserve the root service transition lock while keeping preview lifecycle,
input switching, mic cleanup, post-recording mic monitor restart, and playback
routing from collapsing back into a general audio partial.

Explicit capture cleanup now lives in
`Sussudio/Services/Capture/CaptureService.Cleanup.cs`. That file owns the
public cleanup transition, shutdown teardown order, failed Flashback recording
segment preservation, deferred LibAv/unified-video cleanup handoff, WASAPI
capture disposal, mic teardown, telemetry stop, and final session-state reset.

Capture transition coordination now lives in
`Sussudio/Services/Capture/CaptureService.Coordination.cs`. That file owns
`RunTransitionAsync`, normal `_sessionState` transition writes, steady-state
resolution, and initialization/disposal guards. Best-effort resource release
helpers are delegated to
`Sussudio/Services/Capture/CaptureService.ResourceRelease.cs`.

Disposal-triggered cleanup and final disposed-state writes now live in
`Sussudio/Services/Capture/CaptureService.DisposalLifecycle.cs`. Coordination
lock disposal is delegated to
`Sussudio/Services/Capture/CaptureService.ResourceRelease.cs`.

Capture resource release helpers now live in
`Sussudio/Services/Capture/CaptureService.ResourceRelease.cs`. That file owns
best-effort semaphore release/disposal, coordination-lock disposal, Flashback
backend/export held-lock release helpers, and Flashback eviction resume warnings
used by lifecycle/export/cleanup partials.

Deferred capture cleanup now lives in
`Sussudio/Services/Capture/CaptureService.DeferredCleanup.cs`. That file owns
the Flashback artifact cleanup adapter handoff and export-lock delegation,
deferred unified-video cleanup after LibAv drains, and the pending LibAv drain
reentry guard. Flashback backend artifact cleanup request/retry/dispose/purge
mechanics live in
`Sussudio/Services/Flashback/FlashbackBackendResources.ArtifactCleanup.cs`.

Capture read-only automation probes now live in
`Sussudio/Services/Capture/CaptureService.Probes.cs`. Video source probing,
preview color probing, and preview-frame screenshot waits are separated from
runtime lifecycle mutation code.

Fatal capture and backend failure handling now lives in
`Sussudio/Services/Capture/CaptureService.Failures.cs`. That file owns fatal
error callbacks and the recording/Flashback last-failure telemetry state
fields, lock, mutation helpers, clear helpers, and snapshot reads.

Fatal failure cleanup launch now lives in
`Sussudio/Services/Capture/CaptureService.FailureCleanup.cs`. That file owns
the fatal capture cleanup launcher, generation-stale guards, and the
session-state writes that move the service into cleaning-up/faulted states.
Flashback backend failure cleanup now lives in
`Sussudio/Services/Capture/CaptureService.FlashbackBackendFailureCleanup.cs`.
That file owns the Flashback backend cleanup launcher, GPU device-lost
classification, recovery segment preservation, and generation-stale guards, and
must not write `_sessionState`.

Flashback-facing capture controls now live in focused CaptureService partials:
`Sussudio/Services/Capture/CaptureService.FlashbackControls.cs` owns public
Flashback state, segment access, enable/disable mutations, restart entry
points, and committed restart orchestration after preview backend teardown.
`Sussudio/Services/Capture/CaptureService.FlashbackBufferSettings.cs`
owns buffer/GPU settings updates and live playback-controller GPU decode
propagation. `Sussudio/Services/Capture/CaptureService.FlashbackEncoderSettings.cs`
owns active encoding-setting application, recording-format changes,
encoder-setting cycles, and rollback after failed Flashback buffer cycles while
backend resource construction stays in the Flashback preview backend partials.

Flashback recording policy and session-context helpers now live in
`Sussudio/Services/Capture/CaptureService.FlashbackRecording.cs`. That file owns
Flashback backend ownership checks, session-context construction, frame-rate
rational inference, codec/HDR guardrails, encoded-frame forwarding, and
recording topology validation. Preview-backend producer wiring now belongs to
`Sussudio/Services/Flashback/FlashbackBackendResources.cs`, which owns the
video/audio/microphone attach and detach request shapes used by preview startup,
buffer cycling, and teardown. `Sussudio/Services/Flashback/FlashbackBackendResources.Startup.cs`
owns preview backend startup construction/install/playback initialization and
startup rollback cleanup. `Sussudio/Services/Flashback/FlashbackBackendResources.BufferCycle.cs`
owns sink-only buffer-cycle mechanics, while `Sussudio/Services/Flashback/FlashbackBackendResources.ArtifactCleanup.cs`
owns backend artifact cleanup request/retry/dispose/purge mechanics.
`CaptureService` supplies the service-level export-lock adapter and full rebuild
fallback orchestration.

Recording start lifecycle now lives in
`Sussudio/Services/Capture/CaptureService.RecordingLifecycle.cs`. That file owns
the public recording start transition surface, startup-path routing, and
delegation to the recording-start rollback owner.
`CaptureService.RecordingStartState.cs` owns the private rollback-state holder,
`CaptureService.RecordingStartFlashback.cs` owns Flashback recording fast-path
reuse and backend startup, and
`CaptureService.RecordingStartLibAv.cs` owns standard LibAv recording startup.
`CaptureService.RecordingStartLibAv.AudioInputs.cs` owns standard LibAv
recording audio-input startup, including WASAPI sink attachment, preview
playback preservation, and recording microphone capture wiring.
Recording
stop lifecycle now lives in
`Sussudio/Services/Capture/CaptureService.RecordingStopLifecycle.cs`, including
normal stop routing, the emergency stop overload that feeds finalization, and
the stop/finalize dispatcher for active Flashback and LibAv backends.
`Sussudio/Services/Capture/CaptureService.RecordingFinalizeFlashbackBackend.cs`
owns active Flashback recording backend finalization: live-edge finalize/export
handoff, finalize-in-progress choreography, Flashback recording-integrity
summaries, cancellation-result classification, outcome publication, and
Flashback-specific microphone monitor restart.
`Sussudio/Services/Capture/CaptureService.RecordingFinalizeFlashbackBackendReconcile.cs`
owns post-finalize Flashback backend reconciliation: failed-finalize recovery
preservation, deferred settings apply, buffer cycling, buffer-cycle failure
classification, recovery preservation, and backend cleanup launch.
`Sussudio/Services/Capture/CaptureService.RecordingFinalizeLibAvBackend.cs`
owns standard LibAv recording finalization: unified-video recording stop and
optional teardown, WASAPI recording detach/disposal, LibAv sink normal/emergency
stop and drain tracking, encoder/runtime and recording-integrity summaries,
and the visible final outcome publication before delayed cancellation throws.
`Sussudio/Services/Capture/CaptureService.RecordingFinalizeLibAvPreviewRestore.cs`
owns standard LibAv live-preview restoration after recording: pending Flashback
enable-after-recording detection, guarded Flashback preview backend restore,
failed-restore rollback and purge, standard post-recording microphone monitor
restart, and the `FLASHBACK_ENABLE_AFTER_RECORDING_*` breadcrumbs. Recording
outcome field publication is delegated to
`Sussudio/Services/Capture/CaptureService.RecordingOutcomeState.cs` and
post-recording microphone monitor restart mechanics to
`Sussudio/Services/Capture/CaptureService.MicrophoneMonitor.cs`.
`Sussudio/Services/Capture/CaptureService.RecordingFinalizeFlashbackBoundary.cs`
owns Flashback recording live-edge boundary snapshots, including idempotent
`EndFlashbackRecordingAccounting()` calls, source-frame counters, recording
integrity counters, and audio integrity counters.
`Sussudio/Services/Capture/CaptureService.RecordingOutcomeState.cs` owns the
helper boundary that publishes recording-start and recording-finalize outcome
fields (`_lastOutputPath`, `_lastFinalizeStatus`, `_lastFinalizeUtc`, and
`_lastPreservedArtifacts`) without leaving direct write blocks in lifecycle or
finalization partials.

Transient recording-start rollback cleanup now lives in
`Sussudio/Services/Capture/CaptureService.RecordingRollback.cs`. That file owns
failed-start logging, last-failure publication, Flashback recording rollback
accounting, artifact rollback, and best-effort teardown for partially started
sinks, WASAPI capture, unified-video capture, and deferred LibAv drain cleanup
after a failed recording start.

Flashback export failure classification now lives in
`Sussudio/Services/Capture/CaptureService.FlashbackExportFailureClassification.cs`.
Keep the export failure-kind taxonomy there because automation responses and
capture diagnostics both consume it.

Flashback export entry points now live in
`Sussudio/Services/Capture/CaptureService.FlashbackExportOperations.cs`.
Keep range export, last-N export, backend snapshotting, and session-lock release
before native export there. The shared export pipeline now lives in
`Sussudio/Services/Capture/CaptureService.FlashbackExportCore.cs`; keep
eviction pause, force-rotate fallback, exporter request construction,
diagnostics completion, and cleanup there.

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

Preview lifecycle now lives in focused CaptureService partials:
`Sussudio/Services/Capture/CaptureService.PreviewStart.cs` owns video-preview
start transitions, retained-backend fast-path reattachment, preview-start
rollback, and fresh preview backend startup ordering;
`Sussudio/Services/Capture/CaptureService.PreviewAudioGraph.cs` owns preview
WASAPI capture startup, video-only audio fallback logging, preview playback
attach, preview-time microphone monitor startup, and partially-started audio
rollback;
`Sussudio/Services/Capture/CaptureService.PreviewStop.cs` owns video-preview
stop transitions, keep-pipeline-alive detach semantics, and stopped-state/
telemetry commit; `Sussudio/Services/Capture/CaptureService.PreviewReuse.cs`
owns retained video/Flashback backend reuse checks and capture-settings cloning;
`Sussudio/Services/Capture/CaptureService.PreviewDisposal.cs` owns preview
pipeline disposal ordering, deferred video cleanup, Flashback backend disposal,
WASAPI disposal, and microphone cleanup.

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
so the root encoder stays focused on rotation lifecycle and teardown.

LibAv encoder initialization now lives in
`Sussudio/Services/Recording/LibAvEncoder.Initialization.cs`. Keep FFmpeg
runtime initialization forwarding and the public encoder open/setup sequence
there, including native allocation order, hardware-frame fallback behavior,
muxer-option lifetime, open-state timing, and startup failure cleanup.

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
stream state, public status properties, interleaved packet writes,
pending-sample flush, and accumulator ingress there.
Audio queue/drain mechanics now live in
`Sussudio/Services/Recording/LibAvEncoder.AudioQueue.cs`; keep sample
queue/drain helpers, drift-corrected encode chunks, planar sample copies, and
prepared-frame drains there.

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
context configuration, NVENC private option application, and video bitstream-filter
initialization there. D3D11/CUDA hardware frames setup and ArraySize=1 texture-pool
creation now live in `Sussudio/Services/Recording/LibAvEncoder.HardwareFrames.cs`.
Output rotation now lives in `LibAvEncoder.OutputRotation.cs`; final close and
native cleanup now live in `LibAvEncoder.ResourceCleanup.cs`.

LibAv encoder video submission now lives in
`Sussudio/Services/Recording/LibAvEncoder.VideoSubmission.cs`. Keep CPU packed
frame submission, D3D11 texture submission, CUDA frame submission, forced
keyframe handling, per-frame HDR side-data attachment/removal, and video packet
drains there.

LibAv encoder output lifecycle is split across focused partials.
`Sussudio/Services/Recording/LibAvEncoder.MuxerOptions.cs` owns MP4 muxer
option policy for open and rotated outputs. `LibAvEncoder.OutputRotation.cs`
owns rotation IO close/reopen, stream reinitialization, bitstream-filter reset,
and segment runtime resets. `LibAvEncoder.ResourceCleanup.cs` owns
flush/final close, dispose, trailer writing, and native cleanup/freeing; keep
generic error helpers in `LibAvEncoder.Diagnostics.cs`.

LibAv recording sink queue ownership now lives in
`Sussudio/Services/Recording/LibAvRecordingSink.Queues.cs`. Keep public
video/GPU/CUDA enqueue entry points, caller-side validation, and shared
work-signal/fatal-failure/queue-depth-underflow helpers there.
Video/GPU/CUDA queue admission policy, TryWrite depth accounting, overload
fatal signaling, and video packet records now live in
`LibAvRecordingSink.VideoQueueSubmission.cs`. Video/GPU/CUDA queue cleanup,
pooled video buffer leasing, and pooled packet return helpers now live in
`LibAvRecordingSink.QueueCleanup.cs`. Hot audio/microphone WASAPI write
adapters, audio queue eviction, audio remaining-buffer cleanup, and
`AudioSamplePacket` now live in
`LibAvRecordingSink.AudioQueues.cs`. `LibAvRecordingSink.VideoSession.cs` owns
per-recording video session setup: hardware-frame queue selection,
video/GPU/CUDA channel creation, width/height session state, video/GPU/CUDA
metric reset, and video diagnostics reset before the encoding task starts.
`LibAvRecordingSink.Startup.cs` owns the `IRecordingSink.StartAsync` adapter,
FFmpeg/runtime initialization, encoder option application, audio/microphone
queue setup, startup sequencing, encoding-task creation, start logging, and
startup rollback cleanup. `LibAvRecordingSink.StopLifecycle.cs` owns public and emergency
`StopAsync` routing, `_started` clearing, encode-drain deadline selection,
emergency cancellation/flush fallback, encoding-failure classification, HDR
validation, stopped-output validation handoff, stop logging, and
`FinalizeResult` shaping. Keep root state/construction in
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
verification orchestration in `Sussudio/Services/Recording/Verification/RecordingVerifier.cs`,
ffprobe process/spec/side-data probing in
`Sussudio/Services/Recording/Verification/RecordingVerifier.Ffprobe.cs`, probe
scalar parsing in
`Sussudio/Services/Recording/Verification/RecordingVerifier.ProbeParsing.cs`,
stream/container/HDR and cadence validation policy in
`Sussudio/Services/Recording/Verification/RecordingVerifier.Validation.cs`,
result/taxonomy shaping in
`Sussudio/Services/Recording/Verification/RecordingVerifier.Results.cs`, and
ffprobe frame timestamp cadence analysis in
`Sussudio/Services/Recording/Verification/RecordingVerifier.Cadence.cs`.
`tests/Sussudio.Tests/RecordingVerifier.Integration.Tests.cs` now keeps only
shared fake process-supervisor, runtime snapshot, verifier construction, and
verification invocation helpers. Recording verifier integration scenarios are
split into ffprobe failure, process-priority, codec, Flashback verification
format, mismatch, HDR, and cadence owner files.

Native XU source telemetry detail row assembly now lives in
`Sussudio/Services/Telemetry/NativeXuAtCommandProvider.TelemetryDetails.Build.cs`.
Flash-audio input interpretation and input-source display text live in
`Sussudio/Services/Telemetry/NativeXuAtCommandProvider.TelemetryDetails.AudioInput.cs`.
AT detail byte/number/hex/ascii display formatters live in
`Sussudio/Services/Telemetry/NativeXuAtCommandProvider.TelemetryDetails.Formatters.cs`.
Keep all three linked from `tools/NativeXuAudioProbe/NativeXuAudioProbe.csproj`
whenever this partial family changes, since that tool links shared provider
files explicitly instead of project-referencing the app.

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
the public `SwitchAudioInputAsync` and `SetAnalogGainAsync` entry points there.
Analog gain register mapping/writes now live in
`NativeXuAtCommandProvider.AnalogGain.cs`, HDMI/Analog codec switch sequencing
now lives in `NativeXuAtCommandProvider.AudioSwitch.cs`, and selector-4 I2C
payload writes now live in `NativeXuAtCommandProvider.Selector4.cs`.

Native XU reference full-snapshot reads now live in
`Sussudio/Services/Telemetry/NativeXuAtCommandProvider.FullSnapshot.cs`. Keep
the legacy all-command AT-command acquisition and full-snapshot logging policy
there; the root provider owns selected-interface validation and dispatch into
the active rolling poll path.

Native XU active rolling polling now lives in
`Sussudio/Services/Telemetry/NativeXuAtCommandProvider.RollingPoll.cs`. Keep
poll cadence gates, cached AT-command fields, incomplete-cache handling, and
group advancement there. Rolling command batch construction/refresh and
per-command cancellation checks now live in
`NativeXuAtCommandProvider.RollingCommandGroups.cs`.

Native XU source snapshot assembly now lives in
`Sussudio/Services/Telemetry/NativeXuAtCommandProvider.SnapshotAssembly.cs`.
Keep VIC/frame-rate lookup, AT-command-result decode, diagnostic/detail
assembly, flash-audio analog-gain row insertion, and full-vs-rolling logging
and audio-origin policy switches there.

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
completion/signaling, shared queue-depth accounting, force-rotate audio queue
guard policy, failure notification, and hot audio packet enqueue there.
Flashback encoder video/GPU queue admission now lives in
`Sussudio/Services/Flashback/FlashbackEncoderSink.VideoQueueSubmission.cs`.
Keep video/GPU enqueue acceptance/rejection, TryWrite depth accounting, queue
full classification, and rejection telemetry there.

Flashback encoder queued-buffer cleanup now lives in
`Sussudio/Services/Flashback/FlashbackEncoderSink.QueueCleanup.cs`. Keep
remaining queued video/audio/microphone/GPU buffer return and depth reset there.

Flashback encoder loop orchestration now lives in
`Sussudio/Services/Flashback/FlashbackEncoderSink.EncodingLoop.cs`. Keep the
background encode loop, normal drain ordering, force-rotate dispatch,
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
force-rotate state/status/idle waits, `ForceRotateForExport`, the
`ForceRotateRequest` state machine, request timeout/cancellation handling,
pending-request cleanup, and force-rotate drain abort classification there.

Flashback encoder force-rotate execution now lives in
`Sussudio/Services/Flashback/FlashbackEncoderSink.ForceRotateExecution.cs`.
Keep encoding-thread force-rotate request capture, queue drain-to-rotate
ordering, commit/rotation execution, result completion, failure logging, and
draining-gate cleanup there.

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
output, plane copies, and YUV-to-NV12/P010 conversion now live in
`Sussudio/Services/Flashback/FlashbackDecoder.VideoOutput.cs`. Keep file
open/close and initialization/disposal lifecycle in the root decoder. Video
frame receive, packet feeding, inline audio interleave during video reads, and
decode phase timing state now live in
`Sussudio/Services/Flashback/FlashbackDecoder.DecodeLoop.cs`. Keyframe/exact seek control flow,
pending-frame transfer, seek-cap diagnostics, and seek-buffer flushing now live
in `Sussudio/Services/Flashback/FlashbackDecoder.Seeking.cs`. Shared PTS
conversion, seek timestamp conversion, best-effort frame timestamp selection,
and recoverable seek log suppression now live in
`Sussudio/Services/Flashback/FlashbackDecoder.Timestamps.cs`.
Decoded frame-size calculation, video-dimension validation, D3D11/software
decoded-frame validation, input stream-count bounds, and stream-index bounds now live in
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
purge, eviction selection, and guarded file deletion there. Eviction pause
state, recording PTS range capture, and pause-driven disk-warning state now live
in `FlashbackBufferManager.EvictionPause.cs`. The root buffer manager keeps
session live counters, byte/PTS accounting helpers, and PTS/disk updates.
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

Flashback startup cleanup now lives in
`Sussudio/Services/Flashback/FlashbackStartupCacheCleanup.cs`. Keep stale root
segment cleanup, stale session-directory cleanup, recovery-preserve marker
skips, and temp-drive free-space probing there. Startup session-cache budget
enforcement now lives in
`Sussudio/Services/Flashback/FlashbackStartupSessionCacheBudget.cs`. Keep
startup cache budget calculation, session-directory stats, preserved-session
skips, oldest-session deletion, and cache-budget cleanup telemetry there.

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
background thread priority, and segment
snapshots there so native export cores stay behind focused entry points.

Flashback exporter single-file export shell now lives in
`Sussudio/Services/Flashback/FlashbackExporter.SingleFile.cs`. Keep the
single `.ts` export validation, seek/setup, final output replacement, success
result shaping, and single-export lock release there. Single-file packet
allocation/read/remux, buffered timestamp-base flush, drift logging, and
no-packet validation now live in
`Sussudio/Services/Flashback/FlashbackExporter.SingleFilePacketWriting.cs`.

Flashback exporter multi-segment packet-copy/remux behavior now lives in
`Sussudio/Services/Flashback/FlashbackExporter.Segments.cs`. Keep segment
validation dispatch, temp-output preparation, final output replacement, and
segment-export lock release there. Segment packet writing now lives in
`Sussudio/Services/Flashback/FlashbackExporter.SegmentPacketWriting.cs`; keep
output-template initialization, packet read-loop orchestration, segment offset
updates, progress reporting, and requested-segment skip validation there.
Per-segment packet write state and decisions live in
`Sussudio/Services/Flashback/FlashbackExporter.SegmentPacketWriteState.cs`; keep
timestamp-base discovery, buffered-packet rescue/flush, timestamp rebasing,
segment-boundary repair, DTS monotonicity, and native packet write outcomes
there. Per-segment export range/window
projection and empty effective-range skip classification live in
`Sussudio/Services/Flashback/FlashbackExporter.SegmentRangeProjection.cs`.
Skipped-requested-segment classification and failure-message policy live in
`Sussudio/Services/Flashback/FlashbackExporter.SegmentSkipTracking.cs`.
Output-template selection and template-skip diagnostics live in
`Sussudio/Services/Flashback/FlashbackExporter.SegmentTemplate.cs`. Per-segment
input open, stream-info lookup, stream-count checks, and layout-mismatch skip
tracking live in
`Sussudio/Services/Flashback/FlashbackExporter.SegmentInputPreflight.cs`. The
root exporter keeps shared native state, constants, and fields only.

Flashback exporter lock policy now lives in
`Sussudio/Services/Flashback/FlashbackExporter.ExportLock.cs`. Shared
cancelled/disposed result creation lives in
`Sussudio/Services/Flashback/FlashbackExporter.Results.cs`. Completed-output
length validation lives in
`Sussudio/Services/Flashback/FlashbackExporter.OutputValidation.cs`, normalized
path comparison and output path validation live in
`Sussudio/Services/Flashback/FlashbackExporter.PathValidation.cs`, and
export-range validation plus segment/export-range overlap classification live in
`Sussudio/Services/Flashback/FlashbackExporter.SegmentSelection.cs`. Native
input/output cleanup lives in
`Sussudio/Services/Flashback/FlashbackExporter.NativeState.cs`, linked export
cancellation-source helpers live in
`Sussudio/Services/Flashback/FlashbackExporter.Cancellation.cs`, FFmpeg error
string formatting/throwing lives in
`Sussudio/Services/Flashback/FlashbackExporter.LibAvErrors.cs`, and timestamp
math/saturated arithmetic lives in
`Sussudio/Services/Flashback/FlashbackExporter.TimeMath.cs` so
`FlashbackExporter.cs` stays focused on export native state and shared policy.
Progress normalization/reporting and heartbeat cadence live in
`Sussudio/Services/Flashback/FlashbackExporter.Progress.cs`. Export writer
adaptive throttling, fixed sleep/yield pacing, and per-export throttle provider
scoping live in
`Sussudio/Services/Flashback/FlashbackExporter.WriterPacing.cs`. Packet timestamp
normalization and segment boundary timestamp repair live in
`Sussudio/Services/Flashback/FlashbackExporter.PacketTiming.cs`. Packet clone/free
helpers and buffered packet flushes live in
`Sussudio/Services/Flashback/FlashbackExporter.PacketBuffers.cs`. FFmpeg input and
output context setup, stream count validation, and output header writing live in
`Sussudio/Services/Flashback/FlashbackExporter.Streams.cs`. Stream-template copying
and segment stream-layout compatibility checks live in
`Sussudio/Services/Flashback/FlashbackExporter.StreamTemplates.cs`. Temp output
validation, active output trailer/IO close finalization, atomic replacement,
overwrite policy, and invalid final-output cleanup live in
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
`D3D11PreviewRenderer.Lifecycle.cs`, render-thread orchestration in
`D3D11PreviewRenderer.RenderThread.cs`, queueing in
`D3D11PreviewRenderer.PendingFrames.cs`, VideoProcessor work in
`D3D11PreviewRenderer.Rendering.cs`, shared present accounting in
`D3D11PreviewRenderer.Present.cs`, and shader drawing in
`D3D11PreviewRenderer.ShaderRendering.cs`.

D3D preview renderer nested frame and metrics model types now live in
`Sussudio/Services/Preview/D3D11PreviewRenderer.FrameTypes.cs`. Keep the
`PendingFrame` lifetime wrapper and renderer metric record structs there so the
root renderer stays focused on construction, public state, panel sizing, and
user-facing state changes.

D3D preview renderer runtime knobs now live in
`Sussudio/Services/Preview/D3D11PreviewRenderer.Configuration.cs`. Keep the
measured 4K120 cadence defaults, swap-chain queue/latency env overrides,
DXGI statistics toggles, MMCSS settings, and stop-fence timeouts there. Native
interop declarations now live in
`Sussudio/Services/Preview/D3D11PreviewRenderer.NativeInterop.cs`. Keep
`ISwapChainPanelNative`, `ID3DBlob`, `D3DCompileNative`, and `DwmFlush` there;
leave `WaitForSingleObject` in `D3D11PreviewRenderer.FrameLatency.cs`.

D3D preview renderer frame submission now lives in
`Sussudio/Services/Preview/D3D11PreviewRenderer.Submission.cs`. Keep public raw
frame, lease, texture, and NV12 plane submission entry points plus the NV12
pending-frame adapter there; keep render-thread start/stop and disposal in
`D3D11PreviewRenderer.Lifecycle.cs` and panel sizing in the root renderer.

D3D preview renderer lifecycle now lives in
`Sussudio/Services/Preview/D3D11PreviewRenderer.Lifecycle.cs`. Keep
render-thread start/stop, reinit stop, native-call drain fencing, pending-frame
shutdown cleanup, and renderer disposal there.

D3D preview renderer render-thread orchestration now lives in
`Sussudio/Services/Preview/D3D11PreviewRenderer.RenderThread.cs`. Keep the
MMCSS registration, frame-ready wait loop, shared-device reset consumption,
composition-transform wake handling, pending-frame consumption, stale-generation
drops, device-lost handoff, final pending-frame drain, frame-capture failure,
and render-thread failure telemetry there; keep actual VideoProcessor render
path in `D3D11PreviewRenderer.Rendering.cs`, shared present accounting in
`D3D11PreviewRenderer.Present.cs`, and shader draw paths in
`D3D11PreviewRenderer.ShaderRendering.cs`.

D3D preview renderer frame upload now lives in
`Sussudio/Services/Preview/D3D11PreviewRenderer.FrameUpload.cs`. Keep
video-processor input view resolution, external texture input-view creation,
direct raw-frame texture updates, and staging uploads there; keep present
tracking in `D3D11PreviewRenderer.Present.cs`.

D3D preview renderer shader drawing now lives in
`Sussudio/Services/Preview/D3D11PreviewRenderer.ShaderRendering.cs`. Keep NV12
plane shader rendering, HDR tonemap/passthrough shader rendering, reusable
shader class-instance arrays, and NV12 SRV caching there; keep render-thread
orchestration in `D3D11PreviewRenderer.RenderThread.cs`, and keep VideoProcessor
path in `D3D11PreviewRenderer.Rendering.cs`, and keep present accounting and
slow-frame diagnostic call sites in `D3D11PreviewRenderer.Present.cs`.

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

D3D preview renderer shared-device handoff now lives in
`Sussudio/Services/Preview/D3D11PreviewRenderer.SharedDevice.cs`. Keep
`SetSharedDevice`, `RetireSharedDeviceReferenceForReinit`, shared-device COM
reference duplication/release policy, reset request scheduling, and
`TryInitializeWithSharedDevice` there; keep render-thread reset consumption in
`D3D11PreviewRenderer.RenderThread.cs`.

D3D preview renderer device initialization now lives in
`Sussudio/Services/Preview/D3D11PreviewRenderer.DeviceInitialization.cs`. Keep
`InitializeD3D` orchestration, renderer-owned device fallback, swap-chain
creation, HDR swap-chain capability probing, media present duration setup, and
initial panel binding there.

D3D preview renderer resource management now lives in
`Sussudio/Services/Preview/D3D11PreviewRenderer.Resources.cs`. Keep
video-processor setup, swap-chain RTV/output view creation, color-space
application, and D3D resource disposal there.
Raw-frame and HDR shader input texture allocation now lives in
`Sussudio/Services/Preview/D3D11PreviewRenderer.InputResources.cs`. Keep
NV12/P010 input textures, staging textures, input views, and HDR plane SRV
creation there. Device-lost recovery has its own focused owner; keep render
loop consumption in `D3D11PreviewRenderer.RenderThread.cs`, present paths in
`D3D11PreviewRenderer.Present.cs`, and shader draw paths in
`D3D11PreviewRenderer.ShaderRendering.cs`.

D3D preview renderer swap-chain panel binding now lives in
`Sussudio/Services/Preview/D3D11PreviewRenderer.PanelBinding.cs`. Keep
UI-thread `SetSwapChain` bind/unbind marshaling and composition scale
transforms there; keep device and view allocation in
`D3D11PreviewRenderer.Resources.cs`.

D3D preview pending-frame queue ownership now lives in
`Sussudio/Services/Preview/D3D11PreviewRenderer.PendingFrames.cs`. Keep
enqueue, backlog trimming, frame-ready signal/reset wrappers, explicit pending
drains, and pending-count accounting there; keep render-loop consumption in
`D3D11PreviewRenderer.RenderThread.cs` and frame ownership metrics in
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
top-level frame arrival routing in `UnifiedVideoCapture.FrameIngress.cs`.

Unified capture frame ingress now lives in
`Sussudio/Services/Capture/UnifiedVideoCapture.FrameIngress.cs`. Keep source
frame arrival callbacks, MJPEG pipeline frame emission, capture-arrival ledger
records, pixel-format observer dispatch, and fatal-error signaling there; keep
public control/configuration methods in `UnifiedVideoCapture.cs`.

Unified capture source-session lifecycle now lives in
`Sussudio/Services/Capture/UnifiedVideoCapture.Lifecycle.cs`. Keep source-reader
initialization, read-loop start/stop, preview-reinit disposal, and capture
fatal-error callbacks there. CPU MJPEG decode pipeline construction, preview
jitter buffer setup/disposal, MJPEG stop retention, and MJPEG fatal-error
routing live in
`Sussudio/Services/Capture/UnifiedVideoCapture.MjpegPipelineLifecycle.cs`.

Unified capture recording/Flashback sink fan-out now lives in
`Sussudio/Services/Capture/UnifiedVideoCapture.SinkFanout.cs`. Keep recording
and Flashback enqueue helpers, non-blocking queue rejection accounting, legacy
encoder fallback enqueue adapters, and Flashback recording sequence-gap
accounting there; keep frame arrival callbacks in
`UnifiedVideoCapture.FrameIngress.cs`.

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
their own owners. `MjpegPreviewJitterBuffer.FrameIngress.cs` owns decoded
preview-frame ingress, the nested buffered payload type, ArrayPool/lease
ownership transfer, input-interval recording, queue-full admission drops, and
enqueue signaling. `MjpegPreviewJitterBuffer.Queue.cs` owns queue depth, ordered
frame insertion/dequeue, missing-sequence recovery, clear behavior, and resume
reprime accounting. `MjpegPreviewJitterBuffer.Adaptive.cs` owns hard/soft
deadline drops, adjusted output cadence, target-depth increase/decrease, and
latency-pressure classification. `MjpegPreviewJitterBuffer.EmitLoop.cs` owns
the paced emit loop, display-clock alignment, frame submission to the preview
sink, tick waits, timer-resolution P/Invoke, and MMCSS registration. Keep the
root file focused on construction, suppression/reprime lifecycle, and
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
`NativeXuAtCommandProvider.PayloadDecoding.cs`, keep rolling telemetry polling
in `NativeXuAtCommandProvider.RollingPoll.cs`, keep shared source snapshot
assembly in `NativeXuAtCommandProvider.SnapshotAssembly.cs`, and keep rolling
command batch dispatch in `NativeXuAtCommandProvider.RollingCommandGroups.cs`.

Runtime capture snapshot projection now lives in
`Sussudio/Services/Capture/CaptureService.RuntimeSnapshots.cs` now samples the
read-only runtime inputs consumed by UI, automation, and verification, then
delegates final DTO construction.
`Sussudio/Services/Capture/CaptureService.RuntimeSnapshotAssembler.cs` owns final
`CaptureRuntimeSnapshot` DTO construction from already-sampled field groups.
Video ingest, source-reader health, WASAPI capture, and playback output counter
projection lives in
`Sussudio/Services/Capture/CaptureService.RuntimeSnapshotIngestAudio.cs`,
requested/negotiated reader transport, memory preference, frame-ledger, and
preview renderer-mode projection now lives in
`Sussudio/Services/Capture/CaptureService.RuntimeSnapshotReaderTransport.cs`,
HDR pipeline parity/downgrade projection now lives in
`Sussudio/Services/Capture/CaptureService.RuntimeSnapshotHdrPipeline.cs`,
source telemetry detail/age/alignment projection now lives in
`Sussudio/Services/Capture/CaptureService.RuntimeSnapshotSourceTelemetry.cs`,
and recording-integrity summary projection now lives in
`Sussudio/Services/Capture/CaptureService.RuntimeSnapshotRecordingIntegrity.cs`.
Recording-format and observed-frame helper policy live in focused snapshot
partials.

Capture health snapshot sampling now lives in
`Sussudio/Services/Capture/CaptureService.HealthSnapshots.cs`. That file
captures current service references, invokes focused field builders, and
hands final service-state/scalar values to the assembler; pure
diagnostics/automation DTO construction lives in
`Sussudio/Services/Capture/CaptureService.HealthSnapshotAssembler.cs`. MJPEG
timing, jitter, packet-hash, visual-cadence, and per-decoder projection lives in
`Sussudio/Services/Capture/CaptureService.HealthSnapshotMjpeg.cs`;
source telemetry, backend, suppression, and circuit-state projection lives in
`Sussudio/Services/Capture/CaptureService.HealthSnapshotSourceTelemetry.cs`;
capture cadence projection lives in
`Sussudio/Services/Capture/CaptureService.HealthSnapshotCaptureCadence.cs`;
Flashback buffer, startup-cache, backend-staleness reason policy, and encoder
summary projection lives in
`Sussudio/Services/Capture/CaptureService.HealthSnapshotFlashbackBuffer.cs`;
Flashback live queue, force-rotate, backpressure, and GPU queue projection lives
in `Sussudio/Services/Capture/CaptureService.HealthSnapshotFlashbackQueues.cs`;
recording health orchestration and LibAv-only CUDA queue projection live in
`Sussudio/Services/Capture/CaptureService.HealthSnapshotRecording.cs`, while
active recording backend selection, LibAv-vs-Flashback fallback, and
backend-specific queue/counter normalization live in
`Sussudio/Services/Capture/CaptureService.HealthSnapshotRecordingActiveBackend.cs`;
Flashback export diagnostic and derived progress/throughput projection lives in
`Sussudio/Services/Capture/CaptureService.HealthSnapshotFlashbackExport.cs`.
Flashback playback health snapshot orchestration, state/frame fields, cadence
metrics, decode timing, audio-master fallback fields, and command telemetry
now live together in
`Sussudio/Services/Capture/CaptureService.HealthSnapshotFlashbackPlayback.cs`.
The general snapshot partial is now the diagnostics-snapshot compatibility
entry point plus shared tick-age snapshot helper policy. Flashback
backend-staleness reason policy now stays with the buffer health partial, while
export elapsed/progress-age/file-length helpers stay with the export health
partial.

Recording byte-count snapshot policy now lives in
`Sussudio/Services/Capture/CaptureService.SnapshotRecordingStats.cs`. Keep
active LibAv byte polling, active Flashback buffer estimates, finalized-output
file fallback, and failure flagging there without adding transition-lock waits
or state mutation.

Recording-format snapshot policy now lives in
`Sussudio/Services/Capture/CaptureService.SnapshotRecordingFormat.cs`. Keep
encoder codec labels, output pixel-format/profile labels, and requested
frame-rate argument projection there.

Observed frame-format snapshot policy now lives in
`Sussudio/Services/Capture/CaptureService.SnapshotObservedFrames.cs`. Keep the
explicit `Interlocked.Read` counter projection and private
`ObservedFrameSnapshotFields` owner there; do not infer fake P010 or NV12 frame
counts from requested settings.

Source telemetry snapshot policy now lives in
`Sussudio/Services/Capture/CaptureService.SnapshotTelemetry.cs`. Keep telemetry
backend labels, frame-rate origin labels, suppression/circuit-state mapping,
request/telemetry alignment, and HDR warmup state classification there.

A/V sync snapshot policy, health field projection, and drift baseline state now
live in `Sussudio/Services/Capture/CaptureService.SnapshotAvSync.cs`. Keep live
source/audio drift calculations and encoder drift/correction projection there.

Stats dock, stats toggle, and frame-time overlay lifecycle now live in
`Sussudio/Controllers/Stats/StatsOverlayController.cs`. Stats overlay controller
graph construction and stats dock presentation/diagnostic/hardware/refresh
controller graph wiring now live in `Sussudio/MainWindow.StatsOverlay.cs`;
the same file is the XAML-facing adapter for stats overlay binding setup,
stats dock visibility, and polling commands. Stats toggle
event hookup and checked/unchecked behavior,
initial/property-changed visibility sync, polling, visibility state, dock
refresh ordering, dynamic diagnostic row pools, dock metric value/brush
application, and dock animations are out of the event adapter.
Stats dock show/hide animation mechanics now live in
`Sussudio/Controllers/Stats/StatsOverlayController.DockAnimation.cs`, keeping
storyboards, dock visibility mutations, dock width/fade targets, and animation
completion state out of the polling/visibility orchestration controller root.
Stats dock refresh orchestration now lives in
`Sussudio/Controllers/Stats/StatsDockRefreshController.cs`: snapshot acquisition,
dock presentation build/apply, diagnostics visibility gating, and decode/GPU
row refresh ordering.
Stats dock metric value, visibility, and status brush application now live in
`Sussudio/Controllers/Stats/StatsDockPresentationController.cs`.
Stats section expand/collapse chrome and automation-visible section application
now live in `Sussudio/Controllers/Stats/StatsSectionChromeController.cs`.
`Sussudio/MainWindow.StatsOverlay.cs` is the XAML/automation adapter for the
stats shell wiring.
Detached stats-window metric text now lives in
`Sussudio/Controllers/Stats/StatsWindowPresentationController.cs`, while dynamic
telemetry-detail clearing, empty state, group headers, and row rendering live
in `Sussudio/Controllers/Stats/StatsWindowTelemetryDetailsController.cs`, with
`Sussudio/StatsWindow.xaml.cs` kept to lifecycle, sizing, polling, controller
composition, and always-on-top behavior.
Stats overlay lifecycle, stats dock refresh, stats section chrome, and
diagnostic row pooling contract checks now live in two focused owners:
`tests/Sussudio.Tests/StatsOverlay.Lifecycle.Tests.cs` covers overlay
lifecycle and section chrome, while
`tests/Sussudio.Tests/StatsDockPresentation.Tests.cs` covers dock presentation
application, diagnostic rows, hardware rows, and row chrome pooling. Source telemetry panel
projection checks live with stats presentation coverage in
`tests/Sussudio.Tests/StatsPresentation.SourceTelemetry.Tests.cs`.
Frame-time overlay compact text application and graph-line mutation now live in
`Sussudio/Controllers/Stats/FrameTimeOverlayPresentationController.cs`;
frame-time canvas sizing, sample projection, and expected-line geometry live in
`Sussudio/Controllers/Stats/FrameTimeOverlayGeometry.cs`;
`Sussudio/MainWindow.StatsOverlay.cs` is the XAML-facing compact overlay
adapter and owns the presentation-controller composition beside the stats
overlay visibility route, while
`Sussudio/Controllers/Stats/StatsDockRefreshController.cs` keeps the stats dock
projection refresh adapter.
Decode and GPU hardware stats row refresh/application over presentation inputs
now lives in `Sussudio/Controllers/Stats/StatsHardwareRowsController.cs`;
live MJPEG/NVML sampling and decode availability policy live in
`Sussudio/Controllers/Stats/StatsHardwareRowsInputProvider.cs`;
pure MJPEG/NVML telemetry-to-presentation-input projection lives in
`Sussudio/Controllers/Stats/StatsHardwareRowsInputBuilder.cs`; pure row text
projection over presentation inputs lives in
`Sussudio/ViewModels/StatsPresentationBuilder.HardwareRows.cs`;
decode/GPU row element pooling and style application live in
`Sussudio/Controllers/Stats/StatsDockRowChromeController.cs`; diagnostics empty-state
chrome, group-header chrome, diagnostic row pooling, and diagnostic row style
application live in `Sussudio/Controllers/Stats/StatsDiagnosticRowsController.cs`;
`StatsDockRefreshController` owns when decode/GPU rows refresh.
Stats presentation contract checks now live in focused
`tests/Sussudio.Tests/StatsPresentation.*.Tests.cs` owners for builder
ownership, source telemetry, detached-window formatting, encoder formatting,
and frame-time overlay policy instead of expanding the legacy harness body in
`tests/Sussudio.Tests/Program.cs`.
Stats diagnostic row construction and source-summary parsing now live in
`Sussudio/ViewModels/StatsPresentationBuilder.DiagnosticRows.cs`; frame-lane
diagnostic health summary classification now lives in
`Sussudio/ViewModels/StatsPresentationBuilder.DiagnosticSummary.cs`; frame-time
overlay presentation/range/sample text policy now lives in
`Sussudio/ViewModels/StatsPresentationBuilder.FrameTime.cs`; visual-cadence
FPS/repeat/motion text formatting and expected visual-repeat drift helpers now
live in `Sussudio/ViewModels/StatsPresentationBuilder.Visual.cs`; encoder dock
visibility, codec label, bitrate, and encoder drift text formatting now lives
in `Sussudio/ViewModels/StatsPresentationBuilder.Encoder.cs`; detached
stats-window text and telemetry-detail presentation now lives in
`Sussudio/ViewModels/StatsPresentationBuilder.Window.cs`; stats dock summary
construction and HDMI/capture/preview resolution text now lives in
`Sussudio/ViewModels/StatsPresentationBuilder.Dock.cs`; keep
`Sussudio/ViewModels/StatsPresentationBuilder.cs` focused on shared
formatting helpers.
Stats lane status classification now lives in
`Sussudio/ViewModels/StatsPresentationBuilder.Status.cs`, which consumes the
visual-repeat drift result.
Stats presentation DTO records/enums now live in
`Sussudio/ViewModels/StatsPresentationModels.cs`.
The UI stats snapshot contract lives in `Sussudio/ViewModels/StatsSnapshot.cs`;
shell snapshot orchestration plus renderer cadence/recent-sample acquisition
lives in `Sussudio/Controllers/Stats/StatsSnapshotProvider.cs`;
`Sussudio/MainWindow.StatsOverlay.cs` is the XAML-facing provider composition
adapter; and projection from capture health, renderer metrics, and shell view state lives in
`Sussudio/ViewModels/StatsSnapshotBuilder.cs`.
Pure capture option construction lives in
`Sussudio/ViewModels/CaptureModeOptionsBuilder.cs`.

Dynamic stats dock row chrome now lives in
`Sussudio/Controllers/Stats/StatsDockRowChromeController.cs`. It owns decode/GPU row
reuse. `Sussudio/Controllers/Stats/StatsDockRowChromePresenter.cs` owns shared
stats dock row creation, text mutation, visibility toggles, and row style
application. `Sussudio/Controllers/Stats/StatsDiagnosticRowsController.cs`
owns diagnostic row presentation, telemetry diagnostics empty state, group
headers, and diagnostic row pooling, while
`Sussudio/Controllers/Stats/StatsHardwareRowsInputProvider.cs` owns live
MJPEG/NVML input acquisition and `Sussudio/Controllers/Stats/StatsHardwareRowsController.cs`
owns hardware row availability, text-row presentation building, and minimum
pool sizing before delegating row chrome.

Flashback timeline visibility, lockout, toggle synchronization, and show/hide
animation state now live in
`Sussudio/Controllers/Flashback/FlashbackTimelineController.cs`.
`MainWindow.FlashbackTimeline.cs` is the XAML-facing adapter; command semantics
live in `Sussudio/Controllers/Flashback/FlashbackCommandController.cs`.

Active Flashback pointer-scrub state now lives in
`Sussudio/Controllers/Flashback/FlashbackScrubInteractionController.cs`. It owns scrub
throttling, release/cancel/capture-lost cleanup, fullscreen scrub termination,
lockout clearing, scrub visual updates, and pointer lifecycle around scrub
commands. `MainWindow.FlashbackScrub.cs` is the XAML-facing adapter. Timeline
fraction/duration math used by scrub and playhead presentation now lives in
`Sussudio/Controllers/Flashback/FlashbackTimelineGeometry.cs`.

Flashback CTI/playhead compositor motion now lives in
`Sussudio/Controllers/Flashback/FlashbackPlayheadMotionController.cs`. It owns visual
setup, snap placement, magnetic scrub movement, long-horizon linear playhead
extrapolation, snap-on-open state, and CTI anchor timing.
`Sussudio/MainWindow.FlashbackPlayhead.cs` is the XAML-facing adapter;
command handling and toggle/apply workflows now live in the command controller.

Flashback marker placement and compact duration text now live in
`Sussudio/Controllers/Flashback/FlashbackMarkerPresentationController.cs`, including
in/out marker visibility, selection-region layout, and `m:ss` formatting.
`MainWindow.FlashbackMarkers.cs` is the XAML-facing adapter.

Flashback playback presentation now lives in
`Sussudio/Controllers/Flashback/FlashbackPlaybackPresentationController.cs`: play/pause
glyph policy, Go Live enabled state, buffer-duration text, and floating
playhead label text. `MainWindow.Flashback.cs` wires the playback presentation
controller and playback UI coordinator.

Flashback playback UI sequencing now lives in
`Sussudio/Controllers/Flashback/FlashbackPlaybackUiCoordinator.cs`: track-resize
snap/position/marker/CTI refresh order, playback state polling start/stop,
buffer-fill/position/marker refresh order, and position-label updates with CTI
re-anchor gating.

Flashback export progress presentation now lives in
`Sussudio/Controllers/Flashback/FlashbackExportProgressPresentationController.cs`:
progress-bar value, visibility, and reset-on-complete semantics.
`MainWindow.Flashback.cs` wires the Flashback presentation controllers.

Flashback command semantics now live in
`Sussudio/Controllers/Flashback/FlashbackCommandController.cs`: in/out point commands,
clear, play/pause, Go Live, fullscreen keyboard shortcuts including left/right
nudge rejection logging, export, save-last-5m, enable-toggle rollback, and
apply/restart. `MainWindow.FlashbackCommands.cs` preserves the existing XAML
event-handler names as a thin adapter.

Flashback settings bindings now live in
`Sussudio/Controllers/Flashback/FlashbackSettingsBindingController.cs`: initial settings
projection, GPU decode toggle binding and reverse-sync, buffer duration combo
selection, and `FLASHBACK_UI_BUFFER_DURATION_CHANGED` logging. The async
Flashback enable/disable rollback path and apply/restart command now live in
`FlashbackCommandController`; `MainWindow.FlashbackSettingsBindings.cs` is only
the settings XAML-facing adapter.

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
scrub, seek, play/pause, go-live, and nudge request gating there; keep raw
queue writes/drop policy in the queue partial and playback-thread execution in
the thread partials. Seek/scrub coalescing slot state, queued-position
resolution, and control-yield peek policy now live in
`Sussudio/Services/Flashback/FlashbackPlaybackController.CommandCoalescing.cs`.
Do not grow the root controller with new coalescing slot fields.
Command readiness/failure formatting and queue telemetry bookkeeping live in
`Sussudio/Services/Flashback/FlashbackPlaybackController.CommandTelemetry.cs`.
Keep command status counters and last-failure/latency updates there instead of
growing command channel mechanics.
Playback thread start/stop, command-channel recreation, abandoned-command
draining, and join/cancel diagnostics now live in
`Sussudio/Services/Flashback/FlashbackPlaybackController.ThreadLifecycle.cs`.
Keep queue write/coalescing/drop policy in the command queue partial.
The playback worker loop now lives in
`Sussudio/Services/Flashback/FlashbackPlaybackController.ThreadLoop.cs`; keep
`PlaybackThreadEntry` command dispatch there and do not reintroduce an empty
thread shell marker.
Playback-thread seek command execution now lives in
`Sussudio/Services/Flashback/FlashbackPlaybackController.ThreadSeekCommands.cs`.
Keep coalesced seek resolution, exact resume targets, playback resume handoff,
and audio/preview suppression/resume ordering there instead of growing the
generic playback command partial. Playback-thread scrub begin/update/end command
execution now lives in
`Sussudio/Services/Flashback/FlashbackPlaybackController.ThreadSeekScrubCommands.cs`.
Keep frozen valid-start sampling, scrub update coalescing, exact resume targets,
and audio/preview suppression/resume ordering there.
Playback-thread play/pause/go-live/nudge command execution remains in
`Sussudio/Services/Flashback/FlashbackPlaybackController.ThreadCommands.cs`.
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
`Sussudio/Controllers/Flashback/FlashbackPollingController.cs`.
`MainWindow.FlashbackPolling.cs` is the XAML-facing adapter; CTI anchor timing
lives in `Sussudio/Controllers/Flashback/FlashbackPlayheadMotionController.cs`.

Settings shelf visibility, the animation gate, and show/hide storyboard
construction now live in
`Sussudio/Controllers/Shell/SettingsShelfController.cs`. `MainWindow.SettingsShelf.cs`
is the XAML-facing adapter.

Splash phrase file lookup, Markdown-ish parsing, cached defaults, and exception
fallback now live in `Sussudio/Controllers/Launch/Splash/SplashLoadingPhraseCatalog.cs`.
Randomized interval/mode selection now lives in
`Sussudio/Controllers/Launch/Splash/SplashLoadingPhrasePacingPolicy.cs`.
`Sussudio/Controllers/Launch/Splash/SplashLoadingPhraseController.cs` owns
DispatcherTimer lifecycle and the two-line splash text animation.
Its MainWindow wiring is folded into `MainWindow.LaunchEntrance.cs` because
launch entrance owns the only phrase start/stop choreography.

Launch entrance ownership is split by phase:
`Sussudio/Controllers/Launch/Entrance/LaunchEntranceAnimationController.cs` owns context and
initial hidden/scaled shell state,
`Sussudio/Controllers/Launch/Entrance/LaunchEntranceAnimationController.Splash.cs` owns splash
fade, loading-phrase start/stop ordering, one-shot splash playback state, and
handoff into shell entrance, and
`Sussudio/Controllers/Launch/Entrance/LaunchEntranceAnimationController.Shell.cs` owns shell
chrome/button/stats entrance choreography, deferred preview reveal logging,
active-storyboard cleanup, and control-bar shadow fade. `MainWindow.LaunchEntrance.cs`
is the XAML-facing adapter for launch entrance and splash phrase controller
wiring.

Control-bar button ownership and hover/press/release scale behavior now live in
`Sussudio/Controllers/Shell/ControlBarAnimationController.cs`.
`MainWindow.ShellChrome.cs` is the XAML-facing adapter.

Static shell ThemeShadow and translation setup for the control bar and record
button now live in `Sussudio/Controllers/Shell/ShellElevationController.cs`.
`MainWindow.ShellChrome.cs` is the XAML-facing adapter.

Preview shell/content fade and scale transitions plus unavailable-placeholder
presentation now live in
`Sussudio/Controllers/Preview/PreviewTransitionAnimationController.cs`.
`MainWindow.PreviewTransitions.cs` is the XAML-facing adapter for transition,
delayed fade-in, and startup overlay presentation; shared compositor
shadow opacity fades live in
`Sussudio/Controllers/Preview/PreviewShadowFadeAnimator.cs`.

Preview button glyph/tooltip presentation for Start Preview and Stop Preview
now lives in `Sussudio/Controllers/Preview/PreviewButtonPresentationController.cs`.
`MainWindow.PropertyChangedPreview.cs` wires preview button presentation into
preview lifecycle property/event routing. Preview
button command choreography now lives in
`Sussudio/Controllers/Preview/PreviewButtonActionController.cs`, while
`MainWindow.PreviewActions.cs` keeps the XAML event name stable.

Demo-visible record-button chrome now lives in
`Sussudio/Controllers/Recording/Button/RecordingButtonChromeController.cs`: recording glow,
Rec pulse, starting spinner, normal/recording content, padding, enabled-state
application, and the circle/pill width morph.
`MainWindow.PropertyChangedRecording.cs` wires the chrome controller with the
recording-state presentation adapter.

Recording button command execution and preview-state logging after a recording
start now live in `Sussudio/Controllers/Recording/Button/RecordingButtonActionController.cs`.
`MainWindow.RecordingActions.cs` is the XAML-facing adapter.

Live-signal pill text application, visibility state, show/hide debounce timers,
and the small scale/fade animation now live in
`Sussudio/Controllers/Shell/LiveSignalInfoController.cs`. `MainWindow.LiveSignalInfo.cs`
is the XAML-facing adapter, while
`Sussudio/ViewModels/LiveSignalTextPresentationBuilder.cs` owns label formatting.
Source telemetry summary, telemetry age, and target-summary display text
formatting now live in `Sussudio/ViewModels/SourceTelemetryPresentationBuilder.cs`.
Target-summary property application lives in
`Sussudio/ViewModels/MainViewModel.TargetSummaryPresentation.cs`. HDR runtime
state/readiness projection from capture runtime snapshots lives in
`Sussudio/ViewModels/MainViewModel.HdrRuntimePresentation.cs`.

Preview-volume fade-in/fade-out state, saved target volume, storyboard lifetime,
and volume save suppression now live in
`Sussudio/Controllers/Preview/PreviewAudioFadeController.cs`.
`MainWindow.PreviewAudioFade.cs` is the XAML-facing adapter.
Preview-audio volume transition mechanics now live in
`Sussudio/ViewModels/PreviewAudioVolumeTransitionController.cs`: it owns the
save suppression/override state, ramp constants/easing, transition priming and
restore behavior, and property-to-session volume forwarding. Monitoring
enable/disable orchestration, audio input retargeting, and coordinator
sequencing remain in `Sussudio/ViewModels/MainViewModel.AudioMonitoring.cs`.

Preview reinit animation active state, first-visual transition clears,
startup-reset preservation, completion presentation decisions, and
`D3D11_RENDERER_REINIT_FLAG` / `PREVIEW_REINIT_ANIMATE_*` logs now live in
`Sussudio/Controllers/Preview/PreviewReinitTransitionController.cs`.
`MainWindow.PreviewReinit.cs` remains the XAML/MainWindow adapter for
renderer-stop-before-teardown and reinit completion side effects.

Preview startup attempt/state bookkeeping, timestamps, cached failure/
missing-signal details, and first-visual confirmation state now live in
`Sussudio/Controllers/Preview/Startup/PreviewStartupSessionController.cs` instead of a
MainWindow field bundle. `Sussudio/MainWindow.PreviewStartup.cs` is the
XAML/MainWindow-facing adapter that preserves logging and UI side effects.
Watchdog/telemetry timers, timeout configuration, timeout recovery, and failure-stop scheduling live in
`Sussudio/Controllers/Preview/Startup/PreviewStartupWatchdogController.cs`;
`Sussudio/MainWindow.PreviewStartupWatchdog.cs` wires the MainWindow/XAML-facing
adapter and timeout diagnostic payload. Readiness-signal coordination now lives
in `Sussudio/Controllers/Preview/Startup/PreviewStartupSignalCoordinator.cs`: missing-signal
updates, playback-progress diagnostics, startup signal log strings, GPU
position counter state, and first-visual confirmation decisions. The
`Sussudio/MainWindow.PreviewStartupSignals.cs` partial is the XAML/MainWindow
adapter that supplies live preview state, renderer visibility details, logging,
and confirmation callbacks. Readiness-signal required/received state,
missing-signal calculation, playback-advance threshold checks, and readiness
result snapshots live in
`Sussudio/Controllers/Preview/Startup/PreviewStartupReadinessSignalController.cs`. Missing-signal
and signal-list string formatting lives in
`Sussudio/Controllers/Preview/Startup/PreviewStartupSignalFormatter.cs`. Timeout reason,
timeout status, and failure-stop status text live in
`Sussudio/Controllers/Preview/Startup/PreviewStartupFailureTextFormatter.cs`. This keeps the
root shell focused on wiring while leaving the existing startup state machine
behavior unchanged.
Delayed preview reveal after first visual now lives in
`Sussudio/Controllers/Preview/PreviewFadeInController.cs`; the adapter remains
`Sussudio/MainWindow.PreviewTransitions.cs`. Watchdog/timeout recovery remains in
`Sussudio/Controllers/Preview/Startup/PreviewStartupWatchdogController.cs`.
Preview startup loading overlay presentation now lives in
`Sussudio/Controllers/Preview/Startup/PreviewStartupOverlayController.cs`.
`MainWindow.PreviewTransitions.cs` is the XAML-facing adapter; watchdog and
timeout recovery stay in `Sussudio/Controllers/Preview/Startup/PreviewStartupWatchdogController.cs`.
Top-level preview resize telemetry throttling now lives in
`Sussudio/Controllers/Preview/PreviewResizeTelemetryController.cs`.
`MainWindow.WindowSizing.cs` remains the `SizeChanged` adapter; preview surface
presentation lives with `PreviewSurfacePresentationController`, and preview
shadow visuals live with `PreviewSurfaceShadowController`.

Preview-specific ViewModel event lifecycle and preview property-change routing
now live in `Sussudio/Controllers/Preview/PreviewLifecycleEventController.cs`.
`Sussudio/MainWindow.PropertyChangedPreview.cs` is the XAML/MainWindow-facing
adapter that preserves event handler signatures and delegates into the
controller. The broad `MainWindow.PropertyChanged.cs` dispatcher now owns only
the `PropertyChanged` event envelope, property-name normalization, and visible
route order. Preview reinit transition state and log ownership now live in
`Sussudio/Controllers/Preview/PreviewReinitTransitionController.cs`, while
`Sussudio/MainWindow.PreviewReinit.cs` keeps the renderer-stop-before-teardown
handoff and XAML completion side effects.

Bottom status-strip projection now lives in
`Sussudio/Controllers/Shell/StatusStripPresentationController.cs`, while
`Sussudio/MainWindow.StatusStripPresentation.cs` is the XAML-facing adapter and
builds the ViewModel snapshot passed into the controller. The controller owns
the status-strip `PropertyChanged` router and preserves the recording-only
window-title refresh on recording-time updates.

Pure recording-state lockout decisions now live in
`Sussudio/Controllers/Recording/RecordingStatePresentationPolicy.cs`: recording-time
capture/audio control enablement, analog gain enablement, transition button
enablement, FFmpeg button enablement, and settled record-button content
visibility. Recording-state UI projection now lives in
`Sussudio/Controllers/Recording/RecordingStatePresentationController.cs`: ViewModel-derived
lockout/HDR/title/audio-meter policy application and delegation to
`Sussudio/Controllers/Recording/Button/RecordingButtonChromeController.cs` for record-button
chrome.
`MainWindow.PropertyChangedRecording.cs` is the XAML-facing adapter and
recording property-name router.

Output-path, capture-option, shell-visibility, and live source-signal
property-name routing now live in focused adapters:
`Sussudio/MainWindow.OutputPath.cs`,
`Sussudio/MainWindow.PropertyChangedCaptureOptions.cs`,
`Sussudio/MainWindow.PropertyChangedShell.cs`, and
`Sussudio/MainWindow.LiveSignalInfo.cs`. Keep the root dispatcher
limited to route order, and add new property-name cases to the nearest focused
partial.

Flashback-specific ViewModel property adapter dispatch now lives in
`Sussudio/MainWindow.PropertyChangedFlashback.cs`: timeline lockout, marker and
playhead refresh, export progress, and Flashback settings-control sync.

Audio and microphone-specific ViewModel property projections now live in
`Sussudio/Controllers/Audio/AudioControlPresentationController.cs`: audio toggles,
monitoring meter state, preview volume slider sync, microphone enablement, and
microphone volume sync. `Sussudio/MainWindow.PropertyChangedAudio.cs` is the
XAML-facing adapter.

Microphone volume slider synchronization, save triggers, shelf enablement, and
mic-meter row animation state now live in
`Sussudio/Controllers/Audio/MicrophoneControlsController.cs`.
`MainWindow.MicrophoneControls.cs` is the XAML-facing adapter.

Responsive shell layout is split between
`Sussudio/Controllers/Shell/ResponsiveShellLayoutPolicy.cs`, which owns the
control-bar label breakpoint and capture-settings narrow/wide grid-slot policy,
`Sussudio/Controllers/Shell/ControlBarLabelVisibilityController.cs`, which applies
that policy to the complete control-bar label set, and
`Sussudio/Controllers/Shell/ResponsiveShellLayoutController.cs`, which applies
capture-settings grid placement to XAML elements.
`MainWindow.ResponsiveShellLayout.cs` is the XAML-facing adapter.
Responsive layout ownership checks live in
`tests/Sussudio.Tests/MainWindow.ControllerOwnership.Layout.Tests.cs`.

Capture, audio, microphone, and encoder selection synchronization now lives in
the `Sussudio/Controllers/Capture/CaptureSelectionBindingController*.cs` family. The
root controller owns the controller shell and context lifetime,
`.Context.cs` owns the XAML control dependency bag, `.CollectionBindings.cs`
owns capture/audio/microphone/encoder collection wiring, `.SelectionSync.cs` owns
collection-change debounce/queued sync plus available-option property-change
rebinding, `.DeviceSelection.cs` owns capture-device selection, pending-device
apply state, and mismatch logging, `.AudioSelection.cs` owns audio input and
microphone selection, `.CaptureModeSelection.cs` owns resolution and frame-rate
selection, `.RecordingSelection.cs` owns recording format/quality/preset/
split-encode selection, `.StringSelection.cs` owns shared string ComboBox
selection application,
`Sussudio/Controllers/Capture/CaptureComboBoxSelectionNormalizer.cs` owns pure capture/audio/microphone/
resolution/frame-rate/string ComboBox selection and fallback matching, and
`.DeviceAudio.cs` owns device-audio mode/gain projection. `.PropertyChanges.cs`
owns the capture-selection `PropertyChanged` router, while
`MainWindow.CaptureSelectionBindings.cs` keeps the old method names as the
XAML-facing adapter for binding setup and cross-controller calls.

Capture-device refresh/apply button workflows now live in
`Sussudio/Controllers/Capture/CaptureDeviceActionController.cs`.
`MainWindow.CaptureDeviceActions.cs` is the XAML-facing adapter and keeps the
explicit apply/reinit path separate from selection synchronization.

Pure capture-option presentation decisions now live in
`Sussudio/Controllers/Capture/CaptureOptionPresentationPolicy.cs`: HDR toggle
enablement, MJPEG decoder count visibility, bitrate/preset visibility, audio
clipping visibility, and initial decoder-count clamping. XAML control
application, decoder-count selection handling, and delegation to policy/tooltip
helpers live in `Sussudio/Controllers/Capture/CaptureOptionPresentationController.cs`.
Pure HDR readiness hint and FPS telemetry tooltip text policy now lives in
`Sussudio/Controllers/Capture/CaptureOptionTooltipFormatter.cs`.
`MainWindow.CaptureOptionPresentation.cs` is the XAML-facing adapter and keeps
the existing method names for binding setup, property-change projection, and
the XAML decoder-count selection event.

Capture option binding setup now lives in
`Sussudio/Controllers/Capture/CaptureOptionBindingController.cs`. The single
controller owns the full capture-option binding adapter: XAML/view-model
context, video-format and initial decoder projection, initial selection
projection, resolution/frame-rate selection handlers, recording option event
bindings for format, quality, preset, split-encode, video format, and custom
bitrate, custom-bitrate property-change value projection, HDR/true-HDR click
binding and ViewModel-to-control sync, preview HDR passthrough forwarding, and
`ShowAllCaptureOptionsToggle` click binding/sync. This deliberately folds the
former tiny partial files back into one auditable adapter while preserving the
same MainWindow-facing method surface. The controller delegates presentation
affordances back through the capture-option presentation adapter.
`MainWindow.CaptureOptionBindings.cs` keeps the old capture and recording option
method names used by `SetupBindings()`.

MainWindow capture ownership tests now mirror these runtime owners instead of
living in one capture test grab-bag. Selection bindings, selection normalizer
policy, device actions, option presentation, option affordance policy, option
bindings, and option tooltip formatting each have a focused
`MainWindow.ControllerOwnership.Capture.*.Tests.cs` file registered with the
presentation-preview harness coverage check.

Recording output-path textbox, tooltip, and resize-event updates now live in
`Sussudio/Controllers/Recording/Output/OutputPathDisplayController.cs`; pure truncation text
policy now lives in `Sussudio/Controllers/Recording/Output/OutputPathDisplayTextFormatter.cs`.
`MainWindow.OutputPath.cs` is the XAML-facing adapter used by binding
setup and property changes.

Recording output-path browse/open-recordings button workflows now live in
`Sussudio/Controllers/Recording/Output/OutputPathActionController.cs`.
`MainWindow.OutputPath.cs` is the XAML-facing adapter.

Diagnostic session DTOs now live in focused model files:
`tools/Common/DiagnosticSessionOptions.cs`,
`tools/Common/DiagnosticSessionResult.cs`,
`tools/Common/DiagnosticSessionResult.Capture.cs`,
`tools/Common/DiagnosticSessionResult.FlashbackPlayback.cs`,
`tools/Common/DiagnosticSessionResult.FlashbackRecording.cs`,
`tools/Common/DiagnosticSessionResult.FlashbackExport.cs`,
`tools/Common/DiagnosticSessionResult.Preview.cs`,
`tools/Common/DiagnosticSessionResult.Overview.cs`, and
`tools/Common/DiagnosticSessionSample.cs`. `DiagnosticSessionRunner.cs` owns the
public compatibility entry points; `DiagnosticSessionRunExecution.cs` owns the
visible run phase sequence around context creation, initial snapshot, scenario
execution, cleanup, and completion handoff. `DiagnosticSessionRunContext.cs` owns mutable per-run infrastructure: bootstrap, actions, warnings, samples,
run state, live-state writer, command channel, scenario cancellation source,
initial snapshot state, and disposal. `DiagnosticSessionRunContext.PhaseContexts.cs` owns scenario/completion context construction and the callback/token
handoffs passed into those phases.
`DiagnosticSessionRunExecution.Completion.cs` owns the
post-cleanup evidence/result sequence for recording checks, post-run timeline
and final snapshot capture, result-build handoff, and terminal live-state write.
`DiagnosticSessionRunExecution.cs` hands scenario execution directly to
`DiagnosticSessionScenarioPhaseRunner.cs`, which owns the main scenario phase
for setup/startup, sampling/completion delegation, and fault drain delegation.
`DiagnosticSessionScenarioPhaseRunner.Models.cs` owns the
explicit phase context/state/result records.
`DiagnosticSessionScenarioPhaseRunner.Sampling.cs` owns scenario sampling and
post-sampling completion: live-state sampling setup, sample-loop invocation,
scenario background task awaits, recording-settings deferred await,
rejected-export handling, PresentMon await, and background-task fault drain.
`DiagnosticSessionRunExecution.Completion.cs` owns the final result-build
request mapping consumed by the completion phase.
The public options/result/sample contracts are separated from runner behavior. The result
DTO root owns core session metadata, terminal state, artifacts, actions, and
warnings; the result partials own capture/source, Flashback playback,
Flashback recording, Flashback export, preview, process, recording
verification, and PresentMon fields.

Diagnostic-session result text now lives in a focused partial family rooted at
`tools/Common/DiagnosticSessionResultFormatter.cs`. The root owns the public
`Format(...)` flow plus the simple capture-mode, recording-verification,
PresentMon, and process-performance summary rows. `.Overview.cs` owns the
header/summary/evidence section, `.Flashback.cs` owns Flashback section ordering
plus simple playback command, playback stage/seek-cap, recording, and export
rows, `.FlashbackPlayback.Performance.cs` owns playback cadence/audio-master
performance text, `.FlashbackPlayback.Decode.cs` owns playback decode text,
`.Preview.cs` owns preview section ordering plus preview scheduler, D3D
performance/slow-frame, D3D CPU timing, and visual cadence text. `.Artifacts.cs`
owns artifact/action/warning sections, and `.Helpers.cs` owns
small text helpers. The runner keeps `Format(...)` as a compatibility wrapper
so existing ssctl and MCP callers do not need to know about the formatter owner.

Diagnostic-session result construction now lives in
`tools/Common/DiagnosticSessionResultBuilder.cs`. The root owns result phase
orchestration, artifact-write handoff, summary-write handoff, and final
summary emission while the runner keeps the phase sequence. It also owns
final-result orchestration from analysis and artifact paths into the named
projection set and flattening owner, plus Flashback playback projection
composition from focused playback projection owners.
`DiagnosticSessionResultBuilder.Flattening.cs` owns final
`DiagnosticSessionResult` DTO assignment from the projection set; keep domain
projection composition outside this initializer. The root owns projection-set
assembly from overview, capture, Flashback, preview, D3D, and visual-cadence
projection owners. Overview
outcome policy plus process CPU, recording verification, and PresentMon DTO
projection values live in `DiagnosticSessionResultBuilder.OverviewResult.cs`.
Diagnostic
metric gathering and result-build handoff models live beside it in
`DiagnosticSessionResultBuilder.Analysis.cs` and
`DiagnosticSessionResultBuilder.Models.cs`. Diagnostic health verdict
composition, warning tolerance, and health warning text now live in
`DiagnosticSessionResultBuilder.DiagnosticHealth.cs`. Flashback-specific
analysis warning text for playback forward-decode caps and export force-rotate
fallback observations lives in `DiagnosticSessionResultBuilder.FlashbackWarnings.cs`.
Preview-scheduler analysis handoff values live in
`DiagnosticSessionResultBuilder.PreviewScheduler.cs`: MJPEG jitter-buffer
counter/delta reads, last drop/underflow reason and age reads, and
max/schedule-late aggregation. `DiagnosticSessionResultAnalysis.PreviewScheduler`
is the single record property that carries those values into
`DiagnosticSessionResultBuilder.PreviewResult.cs`; the preview result partial
maps that handoff to `DiagnosticSessionResult` fields without rereading MJPEG
jitter-buffer snapshot keys. Flashback preview-scheduler validation orchestration
now lives in `DiagnosticSessionResultBuilder.PreviewSchedulerValidation.cs`,
including target-FPS fallback, visual-cadence tolerance checks, sparse
deadline/drop tolerance selection, and the call into shared Flashback preview
validation. Preview D3D frame-stats, slow-frame, and CPU-timing
result projection values live in `DiagnosticSessionResultBuilder.PreviewD3DResult.cs`
so D3D summary fields are kept out of the broader preview projection. Preview
visual-cadence result projection values live in
`DiagnosticSessionResultBuilder.PreviewVisualCadenceResult.cs` so visual
cadence summary fields have the same focused owner.
Flashback playback result composition lives in the root result builder, while command
queue, cadence/1% low, decode timing, audio-master/A/V drift, and stage/seek
DTO projection values live in focused `FlashbackPlayback*Result.cs` partials
so result construction can consume one named playback projection while
preserving the existing `summary.json` field shape. Flashback recording backend, growth, and
integrity DTO projection values live in
`DiagnosticSessionResultBuilder.FlashbackRecordingResult.cs`, and Flashback
export status, force-rotate fallback, last-result, and progress DTO projection
values live in `DiagnosticSessionResultBuilder.FlashbackExportResult.cs`.
Export force-rotate fallback counters now travel with
`FlashbackExportSessionMetrics` instead of loose analysis record fields.
Capture selection, negotiated format, source geometry, detected cadence, HDR,
and source-telemetry DTO projection values live in
`DiagnosticSessionResultBuilder.CaptureResult.cs`.

Diagnostic-session summary writing now lives in
`tools/Common/DiagnosticSessionSummaryWriter.cs`. It owns `summary.json` writes
and summary-write failure repair of the returned result object.

Diagnostic-session result artifact setup now lives in
`tools/Common/DiagnosticSessionResultArtifacts.cs`. It owns result artifact path
construction and pre-summary sample, frame-ledger, and timeline writes while
the result builder keeps summary field construction.

Shared diagnostic-session optional text formatting now lives in
`tools/Common/DiagnosticSessionOptionalTextFormatter.cs`. Keep cross-cutting
`FormatOptional(...)` handling there instead of reintroducing private
duplicates in scenario, result builder, formatter, or validation policy files.

MCP performance timeline projection is split across the
`tools/McpServer/Tools/PerformanceTimelineTools.*.cs` family. Keep the public
tool entry point and command response handling in the root file, JSON-to-row
projection in `PerformanceTimelineTools.Rows.cs`, the private row model in
`PerformanceTimelineTools.Rows.Model.cs`,
timeline table text rendering in `PerformanceTimelineTools.Rendering.cs`,
first-vs-last trend text and target-summary orchestration in
`PerformanceTimelineTools.Rendering.Trend.cs`,
compact value/byte/export/D3D formatting helpers in
`PerformanceTimelineTools.Formatting.cs`, and target/pressure summaries in
`PerformanceTimelineTools.Summaries.cs`.
The frame-pacing verdict MCP tool follows the same shape: keep MCP attributes,
method signature, pipe command orchestration, and response shaping in
`FramePacingVerdictTools.cs`; keep channel/timeline projection, readiness and
verdict policy, operator-facing text, and private records in the named
`FramePacingVerdictTools.*.cs` partials.
The Flashback MCP tool type is split by command responsibility: keep
enable/apply commands and the tool type in `FlashbackTools.cs`, playback/scrub
action validation in `FlashbackTools.Actions.cs`, export validation/payload/text
in `FlashbackTools.Export.cs`, and segment-list formatting in
`FlashbackTools.Segments.cs`.
The verification MCP tool follows the same ownership rule: keep public
`verify_recording`, `assert_snapshot`, and `verify_file` methods, command
names, payloads, and 60s verification timeouts in `VerificationTools.cs`; keep
assertion JSON parsing and clone lifetime safety in
`VerificationTools.Assertions.cs`; keep recording/file/assertion text in
`VerificationTools.Formatting.cs`; and keep `Data.Verification` /
`Snapshot.LastVerification` lookup in `VerificationTools.Parsing.cs`.
Preview frame capture MCP reporting is split without changing visible text:
keep the public `capture_preview_frame` entry point, default output path,
payload, and enum-backed `CapturePreviewFrame` routing in
`PreviewFrameCaptureTools.cs`;
keep report section layout in `PreviewFrameCaptureTools.Rendering.cs`; keep
16-bin histogram math/rendering in `PreviewFrameCaptureTools.Histogram.cs`; and
keep anomaly diagnosis policy and aspect checks in
`PreviewFrameCaptureTools.Diagnosis.cs`.
PresentMon MCP stays intentionally shallow: keep `capture_presentmon`,
`capture_presentmon_raw`, structured-content shape, option precedence, and
`PresentMonProbe.RunAsync` invocation in `PresentMonTools.cs`; keep only
snapshot-derived correlation fallback and preview-present field extraction in
`PresentMonTools.Correlation.cs`.

Diagnostic-session pipe retry/error classification now lives in
`tools/Common/DiagnosticSessionPipeRetryPolicy.cs`, keeping access-denied as a
permanent failure and connect failed/timeout responses retryable.

Diagnostic-session command sending now lives in
`tools/Common/DiagnosticSessionCommandChannel.cs`. It owns serialized command
execution, connect-retry wrapping, command failure accounting, and enum-backed
command-name resolution for fixed diagnostic-session commands. Scenario setup
and cleanup pass the channel itself for lifecycle mutations so
`SetFlashbackEnabled`, `SetPreviewEnabled`, `SetRecordingEnabled`, and
`FlashbackAction` flow through `AutomationCommandKind` overloads; the runner
keeps phase orchestration and its public string delegate compatibility.
Diagnostic-session wait command helpers now live in
`tools/Common/DiagnosticSessionCommandChannel.WaitConditions.cs`, which owns
`WaitForCondition` payload shaping and routes the fixed wait command through
`AutomationCommandKind.WaitForCondition`.

Diagnostic-session JSON artifact helpers now live in
`tools/Common/DiagnosticSessionJsonArtifacts.cs`. The runner still owns the
session lifecycle, but JSON writing, frame-ledger extraction, and snapshot /
verification response extraction have a smaller home.

Diagnostic-session initial snapshot capture now lives in
`tools/Common/DiagnosticSessionInitialSnapshot.cs`. It owns the baseline
snapshot capture through `AutomationCommandKind.GetSnapshot`, the unknown-state
warning, and initial-snapshot exception recording while the runner keeps phase
ordering.

Diagnostic-session run context now lives in
`tools/Common/DiagnosticSessionRunContext.cs`. `DiagnosticSessionRunContext.cs` owns mutable per-run infrastructure: bootstrap, actions, warnings, samples,
run state, live-state writer, command channel, scenario cancellation source,
initial snapshot state, and disposal. `DiagnosticSessionRunContext.PhaseContexts.cs` owns scenario/completion context construction and the explicit
callback/token handoffs consumed by scenario and completion phases.

Diagnostic-session run state now lives in
`tools/Common/DiagnosticSessionRunState.cs`. It owns last-stage tracking,
terminal exception classification, and best-effort artifact write failure
recording while the runner keeps the scenario flow readable.

Diagnostic-session live breadcrumbs now live in
`tools/Common/DiagnosticSessionLiveStateWriter.cs`. It owns the
`session-live.json` path, payload shape, health and warning projection,
terminal override mapping, and sampling write throttle.

Diagnostic-session run bootstrap now lives in
`tools/Common/DiagnosticSessionRunBootstrap.cs`. It owns scenario
normalization, scenario-plan selection, duration/sample clamping, session
identity, output-directory creation, and runner process metadata while the
runner keeps command-channel lifetime and phase ordering.

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
Flashback scenario registration orchestration and delegates task registration to
the focused scenario owners, including deferred Flashback recording-settings
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
preview start, recording start, and readiness waits. Fixed setup mutations
should use `DiagnosticSessionCommandChannel` typed `AutomationCommandKind`
sends.

Diagnostic-session cleanup mutations now live in
`tools/Common/DiagnosticSessionCleanupActions.cs`. The root owns the public
cleanup flow and ordering. Recording stop for verification lives in
`DiagnosticSessionCleanupActions.Recording.cs` through typed
`AutomationCommandKind.SetRecordingEnabled`. Flashback playback go-live restore,
preview stop, and Flashback enable-state restore live beside it in
`DiagnosticSessionCleanupActions.StateRestore.cs` through typed
`AutomationCommandKind.FlashbackAction`, `SetPreviewEnabled`, and
`SetFlashbackEnabled` sends. The cleanup result record lives with the public
cleanup flow in `DiagnosticSessionCleanupActions.cs`, while
`DiagnosticSessionCleanupPolicy.cs` remains the post-cleanup warning validator.

Diagnostic-session recording checks now live in
`tools/Common/DiagnosticSessionRecordingChecks.cs`. It owns deferred Flashback
recording-settings restore, verification handoff, and Flashback recording
validation while the runner keeps the high-level post-cleanup phase order.
Post-cleanup last-recording or Flashback export verification command selection,
payload shape, 60-second timeout, cloned verification result, and skipped-
verification action text now live in
`tools/Common/DiagnosticSessionRecordingVerification.cs`.

Diagnostic-session post-run snapshot fetches now live in
`tools/Common/DiagnosticSessionPostRunSnapshots.cs`. It owns performance
timeline artifact input and final health snapshot refresh while the runner
keeps the high-level post-cleanup phase order.

Diagnostic-session scenario metadata now lives in
`tools/Common/DiagnosticSessionScenarioCatalog.cs`. It owns scenario ordering,
setup requirements, export verification filenames, and the plan assigned to
each normalized scenario. `tools/Common/DiagnosticSessionScenarioPlan.cs` owns
the plan DTO plus grouped warning/validation policy switches, including the
preview-cycle grouped predicate, so the runner does not grow direct scenario
string comparisons.

Diagnostic-session cleanup restore validation now lives in
`tools/Common/DiagnosticSessionCleanupPolicy.cs`. It owns warnings for preview,
Flashback, and playback state that remain active after the runner attempts
cleanup.

Diagnostic-session Flashback cycle scenarios now live in named partial owners.
`DiagnosticSessionFlashbackCycleScenarios.Restart.cs` owns the restart-cycle
command flow, playback priming, restart validation, export verification, and
restart-cycle warning/action strings. `DiagnosticSessionFlashbackCycleScenarios.Encoder.cs`
owns preset cycling, buffer readiness, export verification, preset restoration,
and encoder-cycle warning/action strings. `.Registrations.cs` owns task
registration, priority, task-label, and started-action wiring while startup only
delegates selected cycle scenario registration. Do not reintroduce an empty
family root.

Diagnostic-session sampling now lives in
`tools/Common/DiagnosticSessionSampler.cs`. Keep the sample append before the
optional checkpoint callback so checkpoint failures cannot orphan an unseen
sample.

Diagnostic-session metric projection now lives in named partial owners.
`DiagnosticSessionMetrics.Models.cs` owns metric DTOs, `.Cadence.cs` owns
source, preview, and visual cadence projection plus visual-cadence health
classification, `.PreviewD3D.cs` owns D3D slow-frame and CPU timing summaries,
`.PlaybackCommands.cs` owns playback command-health deltas, and `.Counters.cs`
owns shared counter-delta helpers. Do not reintroduce an empty family root.

Diagnostic-session Flashback export helpers now live in
`tools/Common/DiagnosticSessionFlashbackExports.cs`, which owns strict export
verification payload construction, rotated-export segment-count parsing, and
range-selection cleanup. `DiagnosticSessionFlashbackExports.AudioSwitch.cs`
owns the range export audio-switch companion command because it performs a
stateful toggle/restore workflow. Scenario command sequencing lives in separate
scenario owners.

Diagnostic-session Flashback export scenarios now live in a focused partial
family of named owners. Concurrent export, disable-during-export, rotated
export, export during playback, and selection-range export flows each have
their own named file. `DiagnosticSessionFlashbackExportScenarios.Registrations.cs`
owns export scenario task registration while diagnostic-session startup makes a
single qualified call into that owner. Do not reintroduce an empty family root.

Diagnostic-session Flashback lifecycle checks now live in
`tools/Common/DiagnosticSessionFlashbackLifecycleScenarios.cs`. They own the
pause/seek/play disable-and-re-enable task registration, flow, and post-disable
playback queue assertions while startup only delegates to the lifecycle owner.

Diagnostic-session Flashback metric projection now lives in a focused partial
family of named owners. DTOs, recording metrics, playback session aggregation,
playback result copying, and export metrics each have named owner files. Export
metrics also own force-rotate fallback total, delta, and last fallback segment
count, derived outside export-observed relevance gating. These helpers remain
snapshot-only projections and must not send automation commands. Do not
reintroduce an empty family root.

MCP fixed command routes should use `AutomationCommandKind` overloads when the
command is part of the shared catalog. Keep this as an ownership rule, not a
per-route table: record only new file ownership or deliberate exceptions here.
String command names remain only for catalog/manifest-backed dynamic batches,
diagnostic-session command callbacks, and intentionally unconverted compatibility
surfaces with focused coverage.

Diagnostic-session Flashback preview-cycle scenarios now live in a focused
partial family. `.Registrations.cs` owns task registration, priority,
task-label, and started-action wiring while preview-cycle scenario selection
stays in `DiagnosticSessionScenarioCatalog.cs` and grouped preview-cycle policy
stays in `DiagnosticSessionScenarioPlan.cs`.
`.Flashback.cs`, `.Playback.cs`, and `.Recording.cs` own preview stop/restart
flows for normal Flashback, playback, and recording-backed diagnostics. Normal
Flashback and playback-preview-cycle export-while-preview-off verification live
in `.FlashbackExport.cs` and `.PlaybackExport.cs` while startup only delegates
selected scenario registration.

Diagnostic-session Flashback rejected-export scenarios now live in the
`tools/Common/DiagnosticSessionFlashbackRejectedExports*.cs` partial family.
The root owns selected-scenario dispatch, `.Inactive.cs` owns inactive-buffer
failure-kind and last-result assertions, and `.Recording.cs` owns
active-Flashback-recording failure-kind and backend-stability assertions.

Diagnostic-session Flashback recording-settings deferral now lives in named
partial owners. Deferred preset state lives in
`DiagnosticSessionFlashbackRecordingSettingsScenarios.Models.cs`, active
recording mutation/rejection checks live in
`DiagnosticSessionFlashbackRecordingSettingsScenarios.DuringRecording.cs`, and
post-stop preset verification, encoder-frame check, and original-preset restore
live in `DiagnosticSessionFlashbackRecordingSettingsScenarios.PostStop.cs`. Do
not reintroduce an empty family root.

Diagnostic-session Flashback segment playback now lives in
`tools/Common/DiagnosticSessionFlashbackSegmentPlaybackScenarios*.cs`. The root
owns completed-segment playback task registration and crossing choreography,
`.Validation.cs` owns post-boundary snapshot/FPS/command-health warning policy,
and recording-assisted segment rotation plus best-effort stop cleanup live beside it in
`DiagnosticSessionFlashbackSegmentPlaybackScenarios.RecordingAssist.cs` while
`DiagnosticSessionFlashbackSegments.cs` stays read-only segment parsing and wait
policy.

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
stress thresholds and stress/scrub-stress task registration, `.Stress.cs` owns the main stress command sequence,
`.WarmPlayback.cs` owns warmed-playback frame/FPS/1% low and audio-master
fallback checks, `.CommandDrain.cs` owns post-go-live playback command drain
checks, `.Scrub.cs` owns scrub-stress command bursts, `.ScrubDrain.cs` owns
scrub-stress post-go-live drain and command-health checks, and `.AudioMaster.cs`
owns warmed-playback audio-master fallback classification while the runner only
starts the scenario tasks.

Diagnostic-session Flashback validation now lives in concrete
`tools/Common/DiagnosticSessionFlashbackValidation*.cs` partial owners.
`.Recording.cs`, `.Playback.cs`, and `.Preview.cs` own their respective warning
thresholds over already projected metrics while the runner retains scenario
orchestration. Do not reintroduce an empty family root.

Diagnostic-session health policy now lives in
`tools/Common/DiagnosticSessionHealthPolicy.cs`. It owns health severity,
observation, and Flashback warmup filtering.
`tools/Common/DiagnosticSessionHealthTolerances.cs` owns source/preview/Flashback
health-observation classifiers, sparse cadence tolerances, and tolerated warning
classification while the runner still owns scenario execution and warning emission.

Shared automation pipe client ownership is split from a single helper into a
focused partial family under `tools/Common/AutomationPipeClient/`.
`AutomationPipeClient.Transport.cs` owns named-pipe connect orchestration,
write/read framing, and response timeout, `AutomationPipeClient.ConnectErrors.cs`
owns pipe connect failure classification and exact CLI/MCP diagnostic error
codes, `AutomationPipeClient.Commands.cs` owns command envelope sending and
`not_ready` retry policy, `AutomationPipeClient.ResponseState.cs` owns tolerant
response-state parsing, `AutomationPipeClient.Models.cs` owns command result
and exception types, `AutomationSyntheticErrorResponse.cs` owns shared
structured error-envelope creation and common transport/protocol exception
mapping for ssctl/MCP adapters, and
`AutomationResponseState.cs` owns tolerant response-state DTOs shared by the
pipe client and tool surfaces.

PresentMon model and text ownership is split from the probe runner.
`tools/Common/PresentMon/PresentMonProbe.Models.cs` owns PresentMon options, result,
summary, swap-chain, correlation, and metric DTOs.
`tools/Common/PresentMon/PresentMonProbe.ResultMessage.cs` owns success, expected-swap-chain
mismatch, and no-frame result-message shaping.
`tools/Common/PresentMon/PresentMonProbe.Format.cs` owns result text formatting while
`tools/Common/PresentMon/PresentMonProbe.Csv.cs` owns CSV parse overloads, selected-row
filtering, summary assembly, and handoff to row/swap-chain/warning/correlation
helpers. `tools/Common/PresentMon/PresentMonProbe.Csv.Rows.cs` owns row ingestion, header index
construction, schema-presence detection, blank-line skipping, row index
assignment, and row projection from header-indexed fields.
`tools/Common/PresentMon/PresentMonProbe.Csv.Fields.cs` owns header/field parsing, scalar field/metric
reads, and CSV line tokenization. `tools/Common/PresentMon/PresentMonProbe.Csv.SwapChains.cs` owns
swap-chain normalization, artifact filtering, and selected-chain summaries.
`tools/Common/PresentMon/PresentMonProbe.Csv.Correlation.cs` owns app-present correlation, while
`tools/Common/PresentMon/PresentMonProbe.Csv.Summary.cs` owns warnings, counted text fields, and
percentile metric aggregation. `tools/Common/PresentMon/PresentMonProbe.Csv.Models.cs` owns the private
parsed CSV handoff and row shapes. `tools/Common/PresentMon/PresentMonProbe.cs` keeps the public run
orchestration, command-line construction, and argument quoting.
`tools/Common/PresentMon/PresentMonProbe.Paths.cs` owns target process,
PresentMon executable, and output-path resolution. `tools/Common/PresentMon/PresentMonProbe.Process.cs`
owns process supervision, stdout/stderr drain, timeout kill, and temp CSV
cleanup.

Remaining `tools/Common` ownership:

- `AutomationPipeClient/AutomationPipeClient.Transport.cs`
- `AutomationPipeClient/AutomationPipeClient.ConnectErrors.cs`
- `AutomationPipeClient/AutomationPipeClient.Commands.cs`
- `AutomationPipeClient/AutomationPipeClient.ResponseState.cs`
- `AutomationPipeClient/AutomationPipeClient.Models.cs`
- `AutomationPipeClient/AutomationSyntheticErrorResponse.cs`
- `AutomationPipeClient/AutomationResponseState.cs`
- `DiagnosticSessionBackgroundTasks.cs`
- `DiagnosticSessionBackgroundTasks.FaultDrain.cs`
- `DiagnosticSessionBackgroundTasks.Models.cs`
- `DiagnosticSessionCleanupActions.cs`
- `DiagnosticSessionCleanupActions.Recording.cs`
- `DiagnosticSessionCleanupActions.StateRestore.cs`
- `DiagnosticSessionCleanupPolicy.cs`
- `DiagnosticSessionRecordingChecks.cs`
- `DiagnosticSessionRecordingVerification.cs`
- `DiagnosticSessionFlashbackCycleScenarios.Restart.cs`
- `DiagnosticSessionFlashbackCycleScenarios.Encoder.cs`
- `DiagnosticSessionFlashbackCycleScenarios.Registrations.cs`
- `DiagnosticSessionFlashbackExports.cs`
- `DiagnosticSessionFlashbackExports.AudioSwitch.cs`
- `DiagnosticSessionFlashbackExportScenarios.Concurrent.cs`
- `DiagnosticSessionFlashbackExportScenarios.DisableDuringExport.cs`
- `DiagnosticSessionFlashbackExportScenarios.Playback.cs`
- `DiagnosticSessionFlashbackExportScenarios.RangeCleanup.cs`
- `DiagnosticSessionFlashbackExportScenarios.Range.cs`
- `DiagnosticSessionFlashbackExportScenarios.RangeValidation.cs`
- `DiagnosticSessionFlashbackExportScenarios.Registrations.cs`
- `DiagnosticSessionFlashbackExportScenarios.Rotated.cs`
- `DiagnosticSessionFlashbackLifecycleScenarios.cs`
- `DiagnosticSessionFlashbackMetrics.Export.cs`
- `DiagnosticSessionFlashbackMetrics.Models.cs`
- `DiagnosticSessionFlashbackMetrics.PlaybackResult.cs`
- `DiagnosticSessionFlashbackMetrics.PlaybackSession.cs`
- `DiagnosticSessionFlashbackMetrics.Recording.cs`
- `DiagnosticSessionFlashbackPreviewCycleScenarios.Registrations.cs`
- `DiagnosticSessionFlashbackPreviewCycleScenarios.Flashback.cs`
- `DiagnosticSessionFlashbackPreviewCycleScenarios.FlashbackExport.cs`
- `DiagnosticSessionFlashbackPreviewCycleScenarios.Playback.cs`
- `DiagnosticSessionFlashbackPreviewCycleScenarios.PlaybackExport.cs`
- `DiagnosticSessionFlashbackPreviewCycleScenarios.Recording.cs`
- `DiagnosticSessionFlashbackRejectedExports.cs`
- `DiagnosticSessionFlashbackRejectedExports.Inactive.cs`
- `DiagnosticSessionFlashbackRejectedExports.Recording.cs`
- `DiagnosticSessionFlashbackRecordingSettingsScenarios.Models.cs`
- `DiagnosticSessionFlashbackRecordingSettingsScenarios.DuringRecording.cs`
- `DiagnosticSessionFlashbackRecordingSettingsScenarios.PostStop.cs`
- `DiagnosticSessionFlashbackSegmentPlaybackScenarios.cs`
- `DiagnosticSessionFlashbackSegmentPlaybackScenarios.Validation.cs`
- `DiagnosticSessionFlashbackSegmentPlaybackScenarios.RecordingAssist.cs`
- `DiagnosticSessionFlashbackSegments.cs`
- `DiagnosticSessionFlashbackStressScenario.cs`
- `DiagnosticSessionFlashbackStressScenario.Stress.cs`
- `DiagnosticSessionFlashbackStressScenario.WarmPlayback.cs`
- `DiagnosticSessionFlashbackStressScenario.CommandDrain.cs`
- `DiagnosticSessionFlashbackStressScenario.Scrub.cs`
- `DiagnosticSessionFlashbackStressScenario.ScrubDrain.cs`
- `DiagnosticSessionFlashbackStressScenario.AudioMaster.cs`
- `DiagnosticSessionFlashbackWaits.cs`
- `DiagnosticSessionFlashbackWaits.Playback.cs`
- `DiagnosticSessionFlashbackValidation.Recording.cs`
- `DiagnosticSessionFlashbackValidation.Playback.cs`
- `DiagnosticSessionFlashbackValidation.Preview.cs`
- `DiagnosticSessionHealthPolicy.cs`
- `DiagnosticSessionHealthTolerances.cs`
- `DiagnosticSessionJsonArtifacts.cs`
- `DiagnosticSessionInitialSnapshot.cs`
- `DiagnosticSessionMetrics.Cadence.cs`
- `DiagnosticSessionMetrics.Models.cs`
- `DiagnosticSessionMetrics.PreviewD3D.cs`
- `DiagnosticSessionMetrics.PlaybackCommands.cs`
- `DiagnosticSessionMetrics.Counters.cs`
- `DiagnosticSessionOptions.cs`
- `DiagnosticSessionResult.cs`
- `DiagnosticSessionResult.Capture.cs`
- `DiagnosticSessionResult.FlashbackPlayback.cs`
- `DiagnosticSessionResult.FlashbackRecording.cs`
- `DiagnosticSessionResult.FlashbackExport.cs`
- `DiagnosticSessionResult.Preview.cs`
- `DiagnosticSessionResult.Overview.cs`
- `DiagnosticSessionSample.cs`
- `DiagnosticSessionPipeRetryPolicy.cs`
- `DiagnosticSessionCommandChannel.cs`
- `DiagnosticSessionPostRunSnapshots.cs`
- `DiagnosticSessionResultArtifacts.cs`
- `DiagnosticSessionResultBuilder.cs`
- `DiagnosticSessionResultBuilder.Flattening.cs`
- `DiagnosticSessionResultBuilder.OverviewResult.cs`
- `DiagnosticSessionResultBuilder.Analysis.cs`
- `DiagnosticSessionResultBuilder.DiagnosticHealth.cs`
- `DiagnosticSessionResultBuilder.FlashbackWarnings.cs`
- `DiagnosticSessionResultBuilder.FlashbackPlaybackCommandsResult.cs`
- `DiagnosticSessionResultBuilder.FlashbackPlaybackCadenceResult.cs`
- `DiagnosticSessionResultBuilder.FlashbackPlaybackDecodeResult.cs`
- `DiagnosticSessionResultBuilder.FlashbackPlaybackAudioMasterResult.cs`
- `DiagnosticSessionResultBuilder.FlashbackPlaybackStagesResult.cs`
- `DiagnosticSessionResultBuilder.FlashbackRecordingResult.cs`
- `DiagnosticSessionResultBuilder.FlashbackExportResult.cs`
- `DiagnosticSessionResultBuilder.CaptureResult.cs`
- `DiagnosticSessionResultBuilder.PreviewScheduler.cs`
- `DiagnosticSessionResultBuilder.PreviewSchedulerValidation.cs`
- `DiagnosticSessionResultBuilder.PreviewD3DResult.cs`
- `DiagnosticSessionResultBuilder.PreviewVisualCadenceResult.cs`
- `DiagnosticSessionResultBuilder.PreviewResult.cs`
- `DiagnosticSessionResultBuilder.Models.cs`
- `DiagnosticSessionResultFormatter.cs`
- `DiagnosticSessionResultFormatter.Overview.cs`
- `DiagnosticSessionResultFormatter.Flashback.cs`
- `DiagnosticSessionResultFormatter.FlashbackPlayback.Performance.cs`
- `DiagnosticSessionResultFormatter.FlashbackPlayback.Decode.cs`
- `DiagnosticSessionResultFormatter.Preview.cs`
- `DiagnosticSessionResultFormatter.Artifacts.cs`
- `DiagnosticSessionResultFormatter.Helpers.cs`
- `DiagnosticSessionSummaryWriter.cs`
- `DiagnosticSessionRunState.cs`
- `DiagnosticSessionLiveStateWriter.cs`
- `DiagnosticSessionRunBootstrap.cs`
- `DiagnosticSessionRunContext.PhaseContexts.cs`
- `DiagnosticSessionSampler.cs`
- `DiagnosticSessionScenarioCatalog.cs`
- `DiagnosticSessionScenarioPlan.cs`
- `DiagnosticSessionScenarioSetup.cs`
- `DiagnosticSessionScenarioStartup.cs`
- `DiagnosticSessionScenarioStartup.Registrations.cs`
- `DiagnosticSessionScenarioStartup.Playback.cs`
- `DiagnosticSessionPresentMonStartup.cs`
- `DiagnosticSessionOptionalTextFormatter.cs`
- `DiagnosticSessionRunner.cs`
- `DiagnosticSessionRunExecution.cs`
- `DiagnosticSessionRunExecution.Completion.cs`
- `DiagnosticSessionScenarioPhaseRunner.cs`
- `DiagnosticSessionScenarioPhaseRunner.Models.cs`
- `ToolJsonOptions.cs`
- `tools/Common/PresentMon/PresentMonProbe.cs`
- `tools/Common/PresentMon/PresentMonProbe.Paths.cs`
- `tools/Common/PresentMon/PresentMonProbe.Process.cs`
- `tools/Common/PresentMon/PresentMonProbe.ResultMessage.cs`

## Next Slices

1. Continue decomposing diagnostic-session runner internals by owner.

   `tools/Common/DiagnosticSessionRunner.cs` is now the small public wrapper,
   while `tools/Common/DiagnosticSessionRunExecution.cs` owns the visible run
   phase sequence and `tools/Common/DiagnosticSessionRunContext.cs` owns the
   mutable per-run infrastructure, with
   `tools/Common/DiagnosticSessionRunContext.PhaseContexts.cs` owning explicit
   scenario/completion context construction. `DiagnosticSessionRunExecution.Completion.cs` owns the
   post-cleanup evidence/result sequence, while
   `DiagnosticSessionScenarioPhaseRunner.cs` owns the main scenario execution
   phase. `DiagnosticSessionScenarioPhaseRunner.Models.cs`
   owns the explicit scenario context/state/result handoff, with
   `DiagnosticSessionScenarioPhaseRunner.Sampling.cs` owning sampling,
   post-sampling background-task completion, rejected-export handling,
   PresentMon await, and fault drain. Scenario catalog, initial scenario setup, optional scenario
   startup, cleanup mutation ownership, post-cleanup recording checks,
   post-run snapshot fetches, command send/failure plumbing, and result
   construction are extracted; next, split remaining production runner
   families or pivot to the next large owner. The
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
   into geometry, cadence, diagnostics-contract, source-ownership marker plus
   ContractsAndMetrics/RenderPipeline/RuntimeCapture owners, device-lost, and
   frame-flow owners. Automation tool contract tests are split into
   protocol, catalog/manifest, reliability-gates, and snapshot formatter
   owners. Capture configuration model tests are split into option, settings,
   encoder support, Flashback DTO, and recording pipeline owners. Pooled-frame
   tests are split into lease lifecycle, MJPEG jitter policy, MJPEG jitter
   queue behavior, and queued lease release owners. MainWindow shell ownership
   tests are split into chrome, startup, preview runtime, and window lifecycle
   owners. Flashback buffer segment
   tests are split between mutation/accounting/disposal coverage and segment
   lookup/list projection coverage.

3. Continue converting MainWindow partial concerns into controllers.

   `FullScreen`, automation `Screenshot`, MainWindow UI dispatching, preview
   runtime snapshot dispatch/sampling, audio meter rendering, preview startup,
   Flashback playback/export presentation, and stats overlay/row/snapshot
   projection are extracted behind named controllers or builders.
   `MainWindow.xaml.cs` now keeps the controller initialization list grouped
   into shell, Flashback, presentation, preview, recording, launch/status,
   preview action, audio, capture, and output phases so the composition root
   stays navigable as new controllers appear.
   Start the next UI cleanup from remaining broad adapters not already covered
   by controller ownership tests. Keep XAML bindings stable.

4. Move MainViewModel feature state behind a facade.

   Preserve the root `MainViewModel` public surface while introducing feature
   view models or adapters for capture selection, recording, audio, Flashback,
   diagnostics, and automation. `MainViewModelDependencies.cs` now owns the
   default service graph for the root compatibility view model, which gives the
   next facade slices a small construction seam without changing XAML bindings
   or automation contracts. The live audio/microphone meter callback state
   now has a named owner in `MainViewModel.AudioMeters.cs`; keep future meter
   behavior there instead of growing the root facade file. Audio ramp trace
   buffering/sampling now lives in `AudioRampTraceRecorder.cs`, with
   `MainViewModel.AudioRampTrace.cs` kept as the automation-facing adapter; keep
   preview monitoring call sites and coordinator sequencing in
   `MainViewModel.AudioMonitoring.cs`, while audio input retargeting and
   preview-monitoring ramp handoff live in `MainViewModel.AudioInputSelection.cs`.
   Microphone endpoint volume synchronization and persistence now live in
   `MainViewModel.MicrophoneVolume.cs`; device-native audio-control support
   probing, readback, and pending saved-state reconciliation now live in
   `MainViewModel.DeviceAudioRefresh.cs`; mode switching and failure readback
   live in `MainViewModel.DeviceAudioMode.cs`; shared audio-control guards stay
   in `MainViewModel.AudioControls.cs`, while analog gain writes live in
   `MainViewModel.AnalogAudioGain.cs`. UI-facing state is
   split by owner: `MainViewModel.State.cs` owns shared shell/status/live-info
   flags and non-preview coordination gates, `MainViewModel.PreviewState.cs`
   owns preview lifecycle flags, preview reinitialize coordination, and preview
   request events, `MainViewModel.CaptureState.cs` owns capture-selection,
   source, and HDR state, `MainViewModel.AudioState.cs` owns audio/microphone/
   device-audio state, and `MainViewModel.FlashbackState.cs` owns Flashback
   timeline/export state. Keep the root `MainViewModel.cs` focused on the
   compatibility facade, dependency assignment, collaborator construction, and small
   bridge methods. Audio capture/preview property handlers now live in
   `MainViewModel.AudioPropertyChanges.cs`, custom audio input handlers live in
   `MainViewModel.AudioInputPropertyChanges.cs`, microphone monitor/device
   selection handlers live in `MainViewModel.MicrophonePropertyChanges.cs`,
   device-native audio mode/gain handlers live in
   `MainViewModel.DeviceAudioPropertyChanges.cs`, and capture-mode property
   handlers live in `MainViewModel.CaptureModePropertyChanges.cs`. Shared
   view-model UI dispatcher enqueue/invoke policy now lives in
   `Sussudio/Controllers/ViewModel/MainViewModelUiDispatchController.cs`, while
   `MainViewModel.Dispatching.cs` keeps the stable private adapter names and
   preview event fan-out;
   periodic timer refresh orchestration now lives in `MainViewModel.Runtime.cs`,
   runtime event subscription/unsubscription and initial source-telemetry/HDR/live-info/
   timer/disk-space bootstrap now live in `MainViewModel.RuntimeWiring.cs`,
   output drive free-space assignment now lives in
   `MainViewModel.DiskSpacePresentation.cs`, while output drive probing,
   fallback, formatting, and suppressed-warning logging now live in
   `OutputDriveSpacePresentationBuilder.cs`, system-resume preview rebind
   handling now lives in `MainViewModel.PowerResume.cs`, capture status/error and pre-cleanup callbacks
   now live in `MainViewModel.CaptureRuntimeEvents.cs`, recording size/bitrate
   projection and recording-state reset reactions now live in
   `MainViewModel.RecordingRuntime.cs`, and
   live-capture info projection from runtime snapshots now lives in
   `MainViewModel.LiveSignalPresentation.cs`, including audio-preview activity
   and live resolution/frame-rate/pixel-format assignment plus preview-stop
   live-info reset; live-signal label formatting now lives in
   `Sussudio/ViewModels/LiveSignalTextPresentationBuilder.cs`. Capture
   settings projection from UI/runtime state now lives in
   `MainViewModel.CaptureSettings.cs`, with frame-rate request projection split
   to `MainViewModel.CaptureSettingsFrameRate.cs` for selected-option seeding,
   auto-resolved effective FPS, runtime/source rational overrides, and
   rational/decimal fallbacks; `MainViewModel.Capture.cs` stays focused on
   device initialization, preview start/stop, and selected-device apply.
   Debounced preview reinitialization, Flashback-cycle wait-before-reinit,
   renderer-stop handoff, teardown restart, and gate release now live in
   `MainViewModel.PreviewReinitialization.cs`. Output folder browse/open-recordings button workflows now live in
   `Sussudio/Controllers/Recording/Output/OutputPathActionController.cs`. Recording toggle serialization, desired-state routing, graceful stop, emergency stop,
   and the transition gate now live in `MainViewModel.RecordingLifecycle.cs`.
   Concrete recording start/stop operation execution and failure/cancellation
   state repair live in `MainViewModel.RecordingOperations.cs`. Recording option selections, output
   path, counters, and transition flags now live in
   `MainViewModel.RecordingState.cs`. Bounded teardown and watcher disposal now live
   in `MainViewModel.Disposal.cs`. Automation-facing capture runtime, health,
   and recording snapshot projection now lives in
   `MainViewModel.AutomationSnapshots.cs`; source/preview probes and preview
   frame capture live in `MainViewModel.AutomationProbes.cs`; automation-facing
   view-model runtime snapshot UI-thread capture now lives in
   `MainViewModel.ViewModelRuntimeSnapshot.cs`; pure view-model runtime snapshot DTO
   construction lives in `ViewModelRuntimeSnapshotBuilder.cs`;
   automation options UI-thread snapshot capture now lives in
   `MainViewModel.AutomationOptionsSnapshot.cs`; pure selected-control-state DTO
   construction lives in `AutomationOptionsSnapshotBuilder.cs`.
   Flashback playback, scrub, nudge, marker, and automation action command routing
   plus rejection status projection now live in
   `MainViewModel.FlashbackPlaybackCommands.cs`; read-only Flashback playback
   snapshot access plus buffer, bitrate, playback-state, in/out marker, and
   gap-from-live UI projection live in `MainViewModel.FlashbackPlayback.cs`.
   Flashback UI export commands, save-picker flow, active-export guard, and
   user-facing export result/status handling now live in
   `MainViewModel.FlashbackExport.cs`. Shared Flashback export operation
   lifecycle, including outcome classification, core export execution,
   current-operation checks, progress/cancellation handoff, and CTS cleanup,
   now lives in `MainViewModel.FlashbackExportOperation.cs`.
   Automation-facing Flashback export command execution, linked cancellation,
   and dispatcher cleanup now live in
   `MainViewModel.FlashbackExportAutomation.cs`. Read-only Flashback segment
   projection for UI, CLI, and MCP callers now lives in
   `MainViewModel.FlashbackSegments.cs`. Frame-rate selection reactions and
   auto-selection entry points now live in `MainViewModel.FrameRateOptions.cs`,
   while frame-rate option rebuilding and observable collection mutation live in
   `MainViewModel.FrameRateOptionRebuild.cs`. Pure
   frame-rate option choice, including pending SDR bucket preference,
   Source-rate nearest match with timing-family tie-break, generic auto fallback,
   and previous/manual selection fallback, now lives in
   `MainViewModel.FrameRateAutoSelectionPolicy.cs`. Shared frame-rate selection reset,
   resolved automatic frame-rate application, disabled frame-rate reason
   projection, and capture-mode reset flags live in
   `MainViewModel.ModeSelectionState.cs`. Source-rate filtering and
   `ShowAllCaptureOptions` unlock policy live in
   `MainViewModel.FrameRateSourceFilterPolicy.cs`, while `ShowAllCaptureOptions`
   change handling and deferred rebuild behavior live in
   `MainViewModel.CaptureOptionVisibility.cs`. Pure frame-rate timing family,
   timing-variant projection, rational parsing, friendly/exact frame-rate
   matching, and preferred-format ranking now live in
   `Sussudio/ViewModels/FrameRateTimingPolicy.cs`, while
   `MainViewModel.FrameRateTiming.cs` keeps the stateful wrappers over
   resolution capabilities, runtime snapshots, source telemetry, selected
   formats, and UI selection state;
   keep device enumeration and collection replacement in
   `MainViewModel.DeviceManagement.cs`, while selected capture-device reactions,
   capability projection, source telemetry reset, and device-native audio-control
   refresh handoff live in `MainViewModel.DeviceSelection.cs`. Capture-mode property-change
   hooks live in `MainViewModel.CaptureModePropertyChanges.cs` and startup
   audio-list and watcher-driven audio endpoint refresh adaptation live in
   `MainViewModel.AudioDeviceDiscovery.cs`. Pure audio-device filtering and
   previous/saved/default audio and microphone selection fallback policy now
   lives in `Sussudio/ViewModels/AudioDeviceSelectionPolicy.cs`. Pure
   recording codec filtering and selected-codec fallback policy now live in
   `Sussudio/ViewModels/RecordingFormatSelectionPolicy.cs`, while observable
   recording-format option mutation lives in
   `MainViewModel.RecordingFormatOptions.cs`. `MainViewModel.FormatSelection.cs`
   keeps pixel-format option mutation and selected capture-format policy, while
   `MainViewModel.HdrModeChanges.cs` owns HDR toggle side effects: recording-time
   revert/status, mode option rebuilds, immediate reinitialize scheduling, and
   settings persistence.
    Late-arriving device format probe reconciliation, collection mutation,
    selected-device capability refresh, and enqueue/failure logging live in
    `MainViewModel.DeviceFormatProbes.cs`; UI-side late-probe retarget
    application now lives in `MainViewModel.DeviceFormatProbeRetarget.cs`, while
    pure late-probe retarget decisions live in
    `Sussudio/ViewModels/DeviceFormatProbeRetargetPolicy.cs`.
    The presentation-preview ownership tests for this capture selection policy
    area are split across the
    `tests/Sussudio.Tests/MainViewModel.Capture.SelectionPolicy.*.cs` family so
    frame-rate, resolution, mode-selection, late-probe, recording-format, and
    runtime-flag assertions stay near their matching policy owners.
    Automatic resolution dropdown option construction now lives in
    `MainViewModel.AutoResolutionOptions.cs`; automatic resolution-selection
    state adaptation now lives in `MainViewModel.ResolutionOptions.cs`,
    while automatic resolution ranking and source-aware frame-rate selection now
    live in `Sussudio/ViewModels/AutoCaptureSelectionPolicy.cs`; effective Source resolution state,
   auto-value detection, and effective resolution query helpers live in
   `MainViewModel.AutoResolutionState.cs`; auto-resolution display text used by
   status and telemetry presentation lives in
   `MainViewModel.AutoResolutionPresentation.cs`.
   Pure resolution selection policy now lives in the
   `Sussudio/ViewModels/CaptureResolutionSelectionPolicy*.cs` family:
   `CaptureResolutionSelectionPolicy.cs` owns the facade,
   `CaptureResolutionSelectionPolicy.Support.cs` owns parsing and frame-rate
   support checks, `CaptureResolutionSelectionPolicy.Ranking.cs` owns
   nearest-resolution ranking, `CaptureResolutionSelectionPolicy.Source.cs`
   owns source-aware selection,
   `CaptureResolutionSelectionPolicy.Hdr.cs` owns HDR retarget/support-hint
   selection, `CaptureResolutionSelectionPolicy.Sdr.cs` owns SDR auto/fallback
   selection, and `CaptureResolutionSelectionPolicy.Models.cs` owns the
   request/result records.
   `MainViewModel.ResolutionSelectionPolicy.cs` only keeps state-backed
   delegates for callers that still live across the partial family; keep
   dropdown rebuild, collection mutation, and property notifications in
   `MainViewModel.ResolutionOptions.cs`.
   Source telemetry summary, telemetry age, and target-summary display text
   formatting now live in `Sussudio/ViewModels/SourceTelemetryPresentationBuilder.cs`;
   target-summary property application lives in
   `MainViewModel.TargetSummaryPresentation.cs`; HDR runtime state/readiness
   projection lives in `MainViewModel.HdrRuntimePresentation.cs`; keep snapshot
   application and source-aware auto-retargeting in `MainViewModel.Telemetry.cs`.
   Settings initialization and simple persistence reactions stay in
   `MainViewModel.Settings.cs`; the impure settings load/save adapter stays in
   `MainViewModel.SettingsPersistence.cs`, while
   `MainViewModelSettingsPersistenceProjection.cs` owns persisted-settings
   validation, clamping, deferred-selection handoff, and save DTO projection;
   active Flashback reactions to recording format
   and encoder quality/preset/split/bitrate now live in
   `MainViewModel.FlashbackEncoderSettings.cs`; buffer/GPU decode reactions stay
   in `MainViewModel.FlashbackSettings.cs`.
   Pure analog audio gain percent/XU-byte curve mapping now lives in
   `Sussudio/ViewModels/DeviceAudioGainMapper.cs`; async native-XU device
   audio-control refresh/readback stays in `MainViewModel.DeviceAudioRefresh.cs`,
   mode switching and failure readback live in `MainViewModel.DeviceAudioMode.cs`,
   shared audio-control guards stay in `MainViewModel.AudioControls.cs`, and
   `MainViewModel.AnalogAudioGain.cs` owns analog gain XU writes,
   debounce-to-flash, and settings persistence. Use the supported native-XU
   switch/gain command surface rather than the legacy AT input-source fallback
   path.
   UI-only automation mutators for settings visibility, Flashback timeline
   visibility, and show-all capture options now live in
   `MainViewModel.AutomationUi.cs`; stats dock/section visibility and
   frame-time overlay display now live in `MainViewModel.AutomationStatsUi.cs`.
   Automation command entry points for audio enablement, audio-preview
   enablement, preview-volume clamp/persist, device-native mode/gain
   application, and microphone enablement with recording-time
   refusal/idempotent handling now live in `MainViewModel.AutomationAudio.cs`.
   Automation preview enable/disable idempotence, pending-reinit cancellation,
   and preview start/stop routing now live in
   `MainViewModel.AutomationPreview.cs`.
   Automation HDR and true-HDR preview recording-time guard enforcement and HDR
   availability checks now live in `MainViewModel.AutomationHdr.cs`.
   Automation Flashback enable/restart routing through the capture session
   coordinator now lives in `MainViewModel.AutomationFlashback.cs`.
   Automation device refresh and capture-device selection now live in
   `MainViewModel.AutomationDeviceSelection.cs`; audio-input selection and
   custom audio-input enablement now live in
   `MainViewModel.AutomationAudioInputSelection.cs`.
   Recording format automation, HDR compatibility enforcement, encoder
   quality, NVENC split-encode mode, custom bitrate clamp policy, encoder
   preset, and output-path automation now live in
   `MainViewModel.AutomationRecordingSettings.cs`.
   The automation recording desired-state bridge into the shared recording
   transition gate now lives in `MainViewModel.RecordingLifecycle.cs`.
   Capture resolution, frame-rate, video-format, MJPEG decoder worker-count
   automation, and the shared capture-mode reinitialization gate now live in
   `MainViewModel.AutomationCaptureSettings.cs`.
   Startup FFmpeg capability probes for recording formats and split-encode modes
   now live in `MainViewModel.RecordingCapabilityRefresh.cs`.
   The old `MainViewModel.Automation.cs` catch-all has been retired.

5. Extract capture resource owners behind the transition policy.

   The policy is now the legality/steady-state owner. The next deeper capture
   slices should keep it authoritative while introducing smaller owners for
   audio graph, recording controller, Flashback backend resources, and video
   pipeline lifetime. `FlashbackBackendResources.cs` now owns the preview
   backend resource set and producer attach/detach wiring.
   `FlashbackBackendResources.Startup.cs` owns startup construction,
   install/playback initialization, and startup rollback cleanup.
   `FlashbackBackendResources.BufferCycle.cs` owns sink-only buffer-cycle
   mechanics, and `FlashbackBackendResources.ArtifactCleanup.cs` owns backend
   artifact cleanup mechanics. Keep later Flashback backend mechanics there
   before inventing another small owner;
   `CaptureService.FlashbackPreviewBackend.cs` should stay the transition
   coordinator for AV1 probing, readiness waiting, and cleanup handoff.

## Guardrails

- Preserve public automation command names and numeric IDs.
- Use `AutomationCommandKind` overloads for fixed CLI/MCP automation routes;
  keep string command names only for labels, catalog-backed dynamic batches,
  and diagnostic-session runner command-channel delegates.
- Preserve manifest revision rules in `AutomationCommandKind`.
- Preserve XAML binding names until a focused binding migration changes them.
- Preserve Flashback disable lockout behavior.
- Preserve preview/recording no-restart semantics unless a test proves the
  transition intentionally restarts.
- Run `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore` after each
  structural slice.
- Run the console harness when source ownership, automation, capture, recording,
  or Flashback contracts move.

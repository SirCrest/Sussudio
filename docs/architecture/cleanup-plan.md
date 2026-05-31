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

App shell startup and exception policy now live in the XAML partial root without
changing runtime behavior. `Sussudio/App.xaml.cs` owns XAML initialization,
global exception handler hookup, system logging, FFmpeg runtime initialization,
recoverable exception classification, WinUI/AppDomain unhandled exception
handlers, emergency recording finalization, the single-instance mutex guard,
startup identity logging, and MainWindow activation. The class remains partial
only for the generated XAML half.

Logger diagnostics live with the nonblocking writer without changing the public
static logging surface. `Sussudio/Logger.cs` owns initialization, rotation,
bounded channel enqueueing, dropped-message fallback, direct file writes,
`LogEvent`, system evidence collection, exception formatting, structured
snapshot JSON routing through `LoggingJsonContext`, and fatal breadcrumbs.
`Logger.cs` also owns the source-generated JSON context boundary for known log
payloads.

Runtime path resolution lives with the public cached path API without changing
repo/temp/log path behavior. `Sussudio/RuntimePaths.cs` owns the public
`GetRepo*` API, lazy cache fields,
repo-root marker discovery, latest-build parent fallback, log-root override and
fallback policy, guarded directory creation, and trace fallback diagnostics.

FFmpeg runtime location lives with capability probing without changing the
public locator surface. `Sussudio/Services/Runtime/FfmpegRuntimeLocator.cs` owns
app-local, Program Files, and PATH-based runtime/tool resolution, cached encoder
and split-encode capability probes, the bounded `ProcessSupervisor` calls and
timeout policy used by startup recording capability checks, one-time native
initialization, FFmpeg log callback routing, and recoverable seek-log
suppression.

Automation contracts have been extracted into `Sussudio.Automation.Contracts/`.
This removes the old linked-source arrangement where app and tools compiled
protocol/catalog files, pipe-client handoff DTOs, response parsing, synthetic
error shaping, unknown-command policy, and pipe security policy from
`tools/Common`.

Changed ownership:

- `AutomationCommandKind.cs`
- `AutomationCommandCatalog.cs`
- `AutomationPipeProtocol.cs`

Diagnostic session scenario names, CLI help text, MCP-compatible description
text, normalization, setup requirements, export verification metadata, ordering,
and scenario-level plan lookup now live together in
`tools/Common/DiagnosticSessionScenarioCatalog.cs`; the runner still owns
execution flow and summary writing.

Automation diagnostics now have named partial owners instead of one large hub
body. `AutomationDiagnosticsHub.cs` is the compact field/constructor, start/
stop/dispose, and polling-loop owner.
`AutomationDiagnosticsHub.Snapshots.cs` owns public snapshot read/refresh APIs,
refresh-gate serialization, preview jitter, MJPEG, D3D, and Flashback recording
recent-counter baselines and delta updates used by the refresh loop, core
snapshot refresh orchestration, latest-snapshot publication, timeline append,
event notification, and auto-verification handoff.
`AutomationDiagnosticsHub.SnapshotProjection.cs`
owns the `BuildAutomationSnapshot` shell, projection-set composition from
runtime/view-model snapshots and diagnostic classifiers, projection-to-flattened
set dispatch, invocation of every focused final-domain flattener, the private
flattened projection set handoff, plus live A/V sync drift and encoder
correction projection and final A/V sync projection-to-`AutomationSnapshot`
field flattening.
`AutomationDiagnosticsHub.SnapshotProjection.Flattening.AutomationSnapshot.cs`
owns the final `AutomationSnapshot` DTO initializer that flattens the named
projection records into the automation wire snapshot. This final initializer is
intentionally a single `init`-property map: do not split it by adding mutable
setters or shallow fragment records unless a deliberate snapshot construction
pattern is introduced first. `AutomationDiagnosticsHub.Snapshots.cs` owns
stateful snapshot bookkeeping for audio mute suspicion and recording file growth
tracking. `AutomationDiagnosticsHub.cs` owns performance-timeline ring
reads, append mechanics, final `AutomationSnapshot` to
`PerformanceTimelineEntry` assignment, timestamp, observed capture/preview FPS,
encoder video queue depth/drop, capture cadence, process, memory, GC,
thread-pool, pipeline-latency, Flashback export progress, force-rotate fallback,
preview cadence, visual cadence, MJPEG packet/jitter, D3D preview,
preview-pacing, root Flashback playback timeline projection composition, final
grouped handoff, playback cadence, decode timing, command queue/coalescing,
audio-master fallback, playback stage/failure, backend settings, queue reject,
cleanup, and force-rotate timeline projection.
`AutomationDiagnosticsHub.SnapshotProjection.cs` owns root snapshot construction,
timestamp/status projection, view-model lifecycle/audio flags,
verification-in-progress, session state, status-text projection, performance
score, diagnostic lane, preview pacing classifier, performance threshold
projection, selected device/capture/recording settings, preview volume/stats
visibility projection, AV-sync projection, capture command projection, and final
status/evaluation/settings/AV-sync/capture-command flattening.
`AutomationDiagnosticsHub.SnapshotProjection.Media.cs` owns audio/ingest
projection routing, view-model audio peak/clipping and detected audio-signal
projection inputs, capture-ingest and WASAPI projection groups, capture
audio/video reader, source-reader and ingest counters, WASAPI capture/playback
callback, queue, gap, glitch, and latency projection, and flattened audio
signal/ingest/source-reader/WASAPI fields consumed by the automation snapshot
DTO, plus audio drop counter projection, derived real-time/file-writer drop
totals, and final audio-drop projection-to-`AutomationSnapshot` field
flattening.
`WasapiAudioCapture.cs` owns WASAPI capture state, endpoint binding,
mix-format negotiation, AudioClient startup, capture event/client acquisition,
initialization-time metric resets, start/stop/dispose, capture-thread
lifecycle, audio-level event projection, callback interval, discontinuity,
timestamp-error, glitch, audio-level event counters, packet drain, WASAPI
sample decode, f32le 48 kHz stereo conversion, resampling, pooled converted
packet buffers, recording/Flashback/playback attachment points,
converted-packet dispatch, and hot writer task-completion enforcement.
`WasapiAudioPlayback.cs` owns playback state, WASAPI render endpoint binding,
format validation, AudioClient startup, render event/client acquisition,
initialization-time metric resets, start/stop/pause/resume/flush/dispose
lifecycle, render-thread startup, playback chunk queue state, pooled-sample
ingress, queue depth/frame accounting, buffered-duration projection, pooled
chunk returns, the WASAPI render-thread loop, pause/resume execution, resume
prebuffer wait, endpoint buffer writes, render buffer filling, render-side PTS
advancement, volume ramps, and output-level telemetry used by audio ramp traces.
`WasapiComInterop.cs` owns native constants/P/Invokes, shared COM
release/failure helpers, WASAPI/Core Audio enums, audio-format records,
WAVEFORMAT structs, PROPERTYKEY, PropVariant lifetime handling, Core Audio
device, collection, property-store, and notification COM interfaces,
AudioClient/capture/render/endpoint-volume COM interfaces, float-stereo format
allocation, WASAPI format parsing, sample-type classification, device
enumerator activation, endpoint volume helpers, AudioClient activation, and
AudioClient3 shared-stream initialization.
`NativeXuAudioControlService.cs` owns the public service flow, snapshot DTOs,
4K X selector-3 byte indexes, HDMI/Analog reference payloads, gain-profile
placeholders, hex parsing, payload decode/confidence helpers, selector-3
payload read/update workflow, verification against mutated control bytes,
dev-specific candidate enumeration, raw XU GET/SET, raw payload
normalization/rehydration, and retrying the shared native transport gate from
`KsExtensionUnitNative.cs`.
`AutomationDiagnosticsHub.SnapshotProjection.cs` owns snapshot construction
routing, AV-sync projection/flattening, capture session command queue counters,
latency, last-command, last-error projection inputs, selected device/capture/
recording settings, preview volume, and stats visibility consumed by the
automation snapshot DTO, plus final capture-command and settings projection-to-
`AutomationSnapshot` field flattening.
`AutomationDiagnosticsHub.SnapshotProjection.CaptureFormat.cs` owns
capture-format projection routing and groups requested, HDR-request, actual,
negotiated, reader-observation, and encoder format modules consumed by the
automation snapshot DTO, plus HDR activation/auto-downgrade projection, actual
capture dimensions/frame-rate projection, requested capture format/quality/HDR
toggle/audio toggle, negotiated capture dimensions/frame-rate/pixel format,
source-reader subtype and observed pixel/surface format projection inputs,
encoder format/codec/profile/ten-bit confirmation projection, capture memory
preference, requested/negotiated video subtype, frame-ledger projection, final
capture-format flattening, and final capture-transport projection-to-
`AutomationSnapshot` field flattening.
`AutomationDiagnosticsHub.SnapshotProjection.cs` owns preview source capture
cadence, visual cadence, and center-crop visual cadence projection inputs
consumed by the automation snapshot DTO, plus final source capture cadence,
visual cadence, and center-crop visual cadence projection-to-`AutomationSnapshot`
field flattening.
`AutomationDiagnosticsHub.SnapshotProjection.Mjpeg.cs` owns CPU MJPEG totals,
compressed queue, failure, decode/interop-copy/callback/reorder/pipeline
timing, decoder count, per-decoder, and packet duplicate-run / unique-frame
projection inputs consumed by the automation snapshot DTO, plus final CPU MJPEG
totals, compressed queue, timing, packet-hash field flattening, MJPEG preview
jitter projection routing, queue counters, timing samples, adaptive drop/depth
counters, last scheduler event projection, and final preview-jitter
projection-to-`AutomationSnapshot` flattening.
`AutomationDiagnosticsHub.SnapshotProjection.Flashback.cs` owns active
Flashback export progress, failure, force-rotate fallback, last-result
projection, recording failure, cleanup, force-rotate, temp-drive/startup-cache,
active output/runtime, backend settings drift, export-verification, codec
downgrade, encoder identity/bitrate/dimensions/frame-rate, focused projection
routing, and final projection-to-`AutomationSnapshot` flattening.
It also owns Flashback video, GPU, and audio queue/backpressure projection plus
flattened queue/backpressure fields consumed by the automation snapshot DTO.
It also owns Flashback playback state/frame summary, audio-master delay/fallback
projection, playback event/cadence/PTS-cadence/A/V drift projection,
seek-cap/decode timing projection, playback command queue projection, and final
flattened playback fields consumed by the automation snapshot DTO.
`AutomationDiagnosticsHub.SnapshotProjection.Preview.cs` owns preview runtime
projection routing, preview frame counters, estimated pipeline latency, preview
surface visibility, renderer attachment, GPU playback state/position, preview
HDR/tone-map/color metadata, display-cadence/startup/readiness and renderer mode
projection inputs, D3D preview swap-chain and renderer-state projection, D3D
pipeline-latency projection, waitable frame-latency projection, DXGI frame
statistics including recent missed-refresh and stats failure deltas, D3D CPU
upload/render/present/total-frame timing, submitted/rendered/dropped frame
ownership, recent slow-frame projection, and final preview runtime/D3D
projection-to-`AutomationSnapshot` flattening.
`AutomationDiagnosticsHub.SnapshotProjection.cs` owns process memory, CPU, GC,
and thread-pool projection consumed by the automation snapshot DTO, plus final
process resource projection-to-`AutomationSnapshot` field flattening alongside
the core snapshot status/evaluation projections.
`AutomationDiagnosticsHub.SnapshotProjection.Media.cs` owns recording-
integrity projection routing, status/reason, video-frame counters, queue/
backpressure, audio integrity, A/V sync projection inputs, recording-pipeline
projection routing, encoder queue age/count/failure health, conversion/ffmpeg/
video ingest queue health, recording video queue latency, backpressure,
encoder-output health, GPU/CUDA queue health, recording backend/audio-path/
mux-result projection, UI output text, accumulated recording bytes, file-growth
state, last finalized output metadata, last verification result projection
consumed by the automation snapshot DTO, and final recording integrity/pipeline/
backend/output projection-to-`AutomationSnapshot` field flattening.
`AutomationDiagnosticsHub.SnapshotProjection.cs` owns detected source frame-rate
fallback, source dimensions/HDR, raw source signal metadata projection, source
telemetry fallback policy, age calculation, source-target summary inputs, final
source projection flattening orchestration, source dimensions, frame-rate, HDR,
video/audio format, firmware, input, USB, HDCP, raw timing field flattening, and
final source telemetry availability, confidence, detail, age, backend,
suppression, circuit-state, summary, and target-summary field flattening.
`AutomationDiagnosticsHub.SnapshotProjection.CaptureFormat.cs` owns HDR truth
classification from capture runtime, UI state, and recording verification plus
HDR availability/request state, runtime/readiness fallback, HDR warmup/
downgrade, pipeline parity, telemetry-alignment, and HDR truth verdict
projection consumed by the automation snapshot DTO plus final HDR pipeline
projection-to-`AutomationSnapshot` field flattening.
`AutomationDiagnosticsHub.Alerts.cs` owns alert rule evaluation, active-alert
transitions, signal alert orchestration and rules for preview blank/stall/
startup/cadence/display 1% low, capture cadence drop/1% low, audio muted
signal, recording output growth, Flashback alert group routing, Flashback
recording alert orchestration, export progress/force-rotation gap alerts,
temp-cache pressure alerts, encoder failure alerts, recording path degradation
alerts, Flashback playback alert orchestration, Flashback playback performance
alert routing, and frame-submission failure alerts.
`AutomationDiagnosticsHub.Alerts.cs` also owns Flashback playback alert
orchestration, command queue/failure alerts, target-rate/present-cadence/
slow-playback/frametime alerts, submit-failure alerts, audio-master fallback
alerts, audio-queue backlog alerts, diagnostics event publication, event
throttling, Flashback export completion events, and recent event storage.
`AutomationDiagnosticsHub.Snapshots.cs` also owns manual recording/file
verification entry points, flashback-export verification profile shaping, event
publication for explicit verification, last-verification snapshot state,
post-recording auto-verification gating, and background scheduling.
`AutomationDiagnosticsHub.Evaluation.cs` owns diagnostic scoring, root
diagnostic verdict orchestration, Flashback-specific and realtime diagnostic
verdict ordering, final healthy/mixed diagnostic fallback, diagnostic lane text
orchestration, MJPEG decode lane formatting, recording/audio lane formatting,
source cadence/source-signal lane formatting, preview
scheduler/renderer/present/display/visual-cadence lane formatting, Flashback
recording/export/playback lane formatting, lane DTOs used by diagnostic
verdicts, shared renderer-drop threshold constants, shared alert-detail
formatting, and health classifiers used by alerts and diagnostic evaluation.
`AutomationDiagnosticsHub.SnapshotProjection.CaptureFormat.cs` owns HDR truth
classification from capture pipeline, source-HDR, and verification metadata
evidence, plus preview HDR input detection, HDR pixel-format helpers used by
preview state, and tone-map state projection.
`AutomationDiagnosticsHub.cs` owns start/stop/dispose, the polling loop, and performance-timeline ring reads/projection.
`AutomationDiagnosticsHub.Snapshots.cs` owns public snapshot read/refresh
APIs, refresh-gate serialization, core snapshot refresh orchestration, cached
last-output file existence/size probing, process CPU/memory/GC/thread-pool
sampling, latest-snapshot publication, timeline append, event notification, and
auto-verification handoff, manual recording/file verification commands,
verification-profile adaptation, explicit verification events, automatic
post-recording verification scheduling, recording-start verification reset, plus
automation snapshot input projection for preview pacing stage classification.
`PreviewPacingSlowStageClassifier.cs` owns the preview pacing DTOs plus pure
slow-stage classification ordering: source capture, visual duplicate/low-motion,
MJPEG decode, preview jitter scheduler, compositor-miss, renderer-submit, and
D3D dominance predicates/evidence.
Automation command dispatch now keeps the root router focused on the command
envelope, correlation setup, manifest revision checks, auth command handling,
unauthorized-command rejection, readiness gating, dispatch pipeline shell, and
error shaping, plus payload extraction, path/catalog helpers, UI/settings
pre-routing, and port-typed trivial-handler dispatch.
`AutomationCommandDispatcher.CustomCommands.cs`
owns the custom command switch/router for commands that need multi-field
payloads, special response shapes, capture/Flashback routing, or domain command
handoff. `IAutomationViewModel.cs` now keeps the aggregate automation ViewModel
constructor contract while defining feature-shaped ports for readiness, snapshot
queries, device selection, capture settings, audio, preview/recording, UI,
Flashback, and probes in one file. It also owns `AutomationViewModelPorts`, the
composition-time adapter that turns the aggregate compatibility contract into
named port targets for the automation host. Keep these ports grouped there
until a consumer needs a separate file; avoid tiny interface files that only
reduce line count. The dispatcher no longer exposes or stores the aggregate
ViewModel; its port-bundle constructor assigns narrow ports and invokes
trivial/UI handler tables through matching port targets. The dispatcher root
consumes the readiness port for device-ready gating. Device commands consume
the device-selection/snapshot-query ports, audio commands consume the audio
port, and the root dispatcher routes capture-settings plus
preview-recording ports for MJPEG decoder, output path, recording, preview, and
related one-field commands. Visual probe commands
consume the probe port while window screenshots remain on the window-control
surface. Stats-section UI commands consume the UI port, audio-ramp trace reads
consume the snapshot-query port, and Flashback commands consume the Flashback
port.
`AutomationDiagnosticsHub` consumes the snapshot-query port for read-only
runtime, health, and recording verification snapshots. Its constructor should
take `IAutomationSnapshotQueryPort` directly instead of advertising the full
aggregate automation surface.
`AutomationCommandDispatcher.CustomCommands.cs` owns the custom command router
plus read-only snapshot, manifest, diagnostic event, performance timeline,
audio ramp trace, verification, visual probe/capture, and the small
device-selection, audio-control, capture-control, output-path, and
recording-enable command bodies it dispatches, including the recording-response
snapshot refresh, plus Flashback action, export, segment, restart, and enable
command bodies behind the custom command router.
`AutomationCommandDispatcher.cs` owns manifest revision, auth-token, and
readiness gating beside shared response shaping and Flashback rejection
diagnostics, UI/settings command application, the show-all compatibility no-op,
stats-section response text, simple one-property capture and pipeline command
tables, ordered dispatch through those tables, JSON payload extraction helpers,
command metadata lookups, path-validation forwarding, and enum payload parsing.
Named partials own support responsibilities:
`AutomationCommandDispatcher.CustomCommands.cs`
also owns AssertSnapshot response shaping, assertion payload parsing, snapshot
comparison helpers, WaitForCondition response shaping, wait polling, and
snapshot predicates, full-screen, recordings-folder, arm-close, close-arm gating,
and low-level window automation action execution. The reusable target-typed
trivial-handler wrapper lives with `AutomationCommandDispatcher.cs` because it
only supports the dispatcher's port-grouped one-property command tables.

Automation pipe hosting now lives in `NamedPipeAutomationServer.cs`. Keep
constructor/configuration state, server start/stop and accept-loop behavior,
per-connection safety/disposal, request-session handoff, error/timeout
responses, fallback tracing, per-request JSON framing, client PID logging,
dispatch timeouts, late-dispatch observation, response writing, and Windows
pipe security/PInvoke in that server owner.

App project build workflow is split so `Sussudio/Sussudio.csproj` stays focused
on app identity, assets, packages, runtime config, and project references, while
`Sussudio/Sussudio.Build.targets` owns publish flags, English-only locale
stripping, and repo-local `latest-build` staging.

`tools/ssctl/CommandHandlers.cs` owns the complete ssctl command-handler
surface: top-level CLI router, per-invocation command context, shared command
sending, the `PipeTransport` wrapper's ssctl-specific timeout and structured
error envelope policy, response exit-code shaping, generic argument helpers,
flag parsing, JSON detection/pretty printing, primitive/domain value parsing,
wait/assert/probe plus recording/file verification scripting flow commands,
diagnostic and observability commands, `presentmon` parsing/swap-chain
discovery/probe invocation, `diagnostic-session` parsing/runner invocation,
preview/record/screenshot/frame commands, device commands, set-value
capture/audio/output mutations, window and shell visibility commands,
recordings-folder commands, and Flashback timeline/playback/scrub/marker/range
and export payload shapes. Keep command-family section comments inside this
single owner; do not reintroduce `CommandHandlers.*.cs` partial files unless a
family becomes a real independently tested collaborator.
`tools/ssctl/Program.cs` owns process entry, Ctrl-C cancellation, CLI option
parsing, exit-code shaping, the help facade, operator-facing help section text,
and catalog-backed help lines.

`tools/ssctl/Formatters.cs` is the unified projection facade for console
output. Keep app snapshot orchestration, section ordering, and simple state/
capture-command, audio, recording, diagnostics, legacy performance, process
CPU, Memory/GC, thread-pool, capture settings, friendly/exact frame-rate,
capture cadence, embedded AV-sync drift, source-signal, video-pipeline,
thread-health section order and source-reader/WASAPI row text, preview
renderer-mode routing, non-D3D fallback text, D3D preview snapshot text,
Flashback snapshot gating/order, encoding status/health text, export progress/
result text, playback command text, playback cadence/decode/frame/stage/A/V
drift text, MJPEG activation/header/order, decode/copy/callback/per-decoder
timing, compressed-queue, drop-reason, reorder, pipeline timing,
preview-jitter queue, latency, ownership, and underflow text, diagnostic-event
text, capture option/device text, standalone memory/GC summaries, performance
timeline response validation, JSON row projection, private row model, table
output, trend summaries, and shared JSON/result helpers together there, with
shared CLI formatter contracts in the command-handler tests.

`tools/Common/AutomationSnapshotFormatter.cs` is now the shared automation
snapshot formatter owner for top-level text flow plus the small root sections:
state/capture-command summary, capture settings, audio signal, video pipeline,
thread-health rows, recording output/backend/integrity/audio-integrity/
last-finalize, diagnostics, legacy performance, process CPU/Memory/GC/
thread-pool text, capture cadence, MJPEG packet fingerprint, visual cadence,
AV-sync, preview routing, source-signal rows emitted from the cadence tail,
Flashback gate/header/order, encoding status/health text, export
progress/result text, playback command text, playback cadence/decode/frame/
stage/A/V drift text, MJPEG activation/header/order, decode/copy/callback/
per-decoder timing text, compressed queue/drop-reason/reorder/pipeline timing
text, MJPEG preview-jitter queue/latency/ownership/underflow text, D3D preview
header/routing and output order, D3D CPU timing, pipeline latency,
frame-latency wait text, D3D frame ownership, DXGI frame-stat text,
slow-frame diagnostics shared by `ssctl`, snapshot response-success detection,
tolerant JSON string/bool/numeric accessors, and shared byte/number/interval,
frame-budget, and tick-age display helpers. Tests that reason about formatter
source use the shared `RuntimeContractSource` snapshot formatter source reader
from both the legacy harness and xUnit formatter contracts.

Diagnostic-session MCP surface coverage keeps
`McpToolSurface.DiagnosticSession.Runner.Tests.cs` as the MCP tool success/failure
artifact contract owner and focused reflective runner behavior owner,
`McpToolSurface.DiagnosticSession.Ownership.*.Tests.cs` for
planning, execution, teardown, and reporting helper ownership assertions,
`McpToolSurface.DiagnosticSession.Flashback.*.Tests.cs` for Flashback
scenario/metrics/wait/export ownership assertions,
`McpToolSurface.DiagnosticSession.InfrastructureOwnership.*.Tests.cs` for
focused infrastructure ownership tests. The runner behavior files now own
final-snapshot artifact failures, sparse source-cadence health tolerance,
Flashback export/playback command flow, unknown-initial-snapshot mutation
safety, synthetic pipe-connect retry, and concurrent-output-directory lockout.
Infrastructure ownership files now split runner/initial-snapshot, pipe
retry/command channel, run context, and scenario/completion phase assertions.
MCP tool checks now execute through
`tests/Sussudio.Tests/XUnit.ToolContractsTests.cs`, keeping window/preview,
condition wait, screenshots, preview-frame capture, probes, PresentMon
correlation, performance timeline, frame-pacing verdict, command routing,
host/pipe behavior, verification formatting, Flashback tool routing, and
diagnostic-session tool entries in xUnit after their removal from the legacy
harness catalog.

Shared Flashback source readers and buffer helper factories now live in
`tests/Sussudio.Tests/HarnessCore.cs` with the rest of the legacy `Program`
harness helpers.
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
transition routing, async `IAutomationViewModel` surface plus Flashback/probe
dispatcher routing, diagnostics refresh, diagnostics projection ownership,
dispatch cancellation/timeouts under the relevant async-surface, pipe-server,
coordinator-queue, and dispatcher-readiness owners, audio command guards,
preview lifecycle routing, UI settings, capture-mode/device routing, and
Flashback cleanup ownership partials. Keep new automation tests in the closest
owner file instead of regrowing the root catch-all.
The diagnostics-refresh source-family helper now lives in
`tests/Sussudio.Tests/MainViewModel.Automation.DiagnosticsRefresh.Tests.cs`
with the diagnostics-refresh ownership assertions that consume it. Keep grouped
diagnostic evaluation, alert, snapshot-projection, and aggregate text helpers
there unless a new executable fixture needs a separate owner.

`tests/Sussudio.Tests/HarnessCore.cs` owns shared source-inspection helpers,
including MainViewModel source readers, member extraction, comment/string
stripping, regex assertions, and token-order assertions used across capture,
Flashback, automation, MCP, recording, stats, and docs tests. Capture
regression coverage is split across the
`tests/Sussudio.Tests/MainViewModel.Capture.*.cs` family, including preview
startup, Flashback export locking, Flashback coordinator/UI routing, Flashback
backend lifecycle, capture selection policy, output path, audio monitoring,
reinitialization, and Flashback frame-rate/enable-disable owner files.

`tests/Sussudio.Tests/XUnit.SnapshotModelsTests.cs` owns the snapshot-model
xUnit contract suite. Snapshot model coverage for CaptureDiagnosticsSnapshot,
CaptureHealthSnapshot, SourceSignalTelemetrySnapshot,
SourceTelemetryDetailEntry, source telemetry automation projection,
AutomationSnapshot metric-shape bands, and AutomationOptions DTO shape checks
now lives in that single suite with shared reflection/spec helpers beside the
facts that use them.

`Sussudio/Models/Automation/AutomationSnapshot.cs` owns the flattened
automation snapshot DTO properties for app, capture, audio, preview, recording,
and Flashback evidence. Keep broad model buckets from regrowing into partial
sprawl: new evidence fields should land in the existing grouped DTO surface and
get a matching source-ownership assertion in the snapshot-model tests.

`Sussudio/Models/Capture/CaptureSnapshotModels.cs` owns the base diagnostics
DTO surface and its extended health DTO: session state, negotiated format,
observed frame, HDR auto-downgrade, source telemetry, capture cadence,
recording/audio queue, Flashback queue, MJPEG, visual-cadence diagnostics,
source-signal health, queue-age, A/V sync, Flashback encoder/backend, playback,
and export health properties. Keep these inherited capture snapshot DTOs
together unless a future change introduces a real named collaborator instead of
another partial fragment.

`tests/Sussudio.Tests/RecordingQueue.OverloadPolicy.Tests.cs` now owns the
shared recording queue source readers and source-block extraction helpers
beside the core overload-policy checks and LibAv sink queue/lifecycle ownership
assertions. Capture health snapshot ownership coverage is split into
assembly/sampler, Flashback, and recording/source-telemetry files. Recording
queue coverage is split into queue overload/LibAv sink policy, WASAPI, and
capture fan-out / Flashback backend owner files.

D3D preview renderer coverage is
split into geometry/screenshot helper and preview PNG encoder contracts, cadence contracts, the large
diagnostics contract, device-lost behavior, and frame-flow/shared-device
assertions. Source ownership coverage lives in focused RenderPipeline,
RenderThread, and RuntimeCapture owner files.

`tests/Sussudio.Tests/AutomationToolContracts.Tests.cs` keeps shared reflection
helpers plus automation command kind, catalog metadata, manifest/path-policy,
and reliability-gates contract checks. Pure `Sussudio.Automation.Contracts`
command ID, manifest ID, protocol resolution, timeout/auth/envelope, and
`CommandMap` checks now have fast xUnit coverage in
`tests/Sussudio.Tests/XUnit.AutomationContractsTests.cs`, backed by the
expected command-ID table now owned by `AutomationToolContracts.Tests.cs`
beside the legacy automation contract helpers. The legacy protocol
harness file has been retired; `tests/Sussudio.Tests/AutomationToolContracts.Tests.cs`
also owns the direct `AutomationToolContractsProtocolXunitTests` coverage for
automation client timeout policy, advanced command-map alignment,
pipe-failure contracts, tool delegation, script freshness, and response-state
parsing, using `RuntimeContractSource.ReadAutomationPipeClientSource()` for
the shared AutomationPipeClient source family. Automation tool contract coverage is
otherwise split into shared/ssctl snapshot formatter contracts and tool-probe
contracts. `tests/Sussudio.Tests/XUnit.AutomationContractsTests.cs` owns
the xUnit execution surface for catalog, manifest, path-policy, and
reliability-gates checks after their removal from the legacy offline harness
catalog. `tests/Sussudio.Tests/XUnit.ToolContractsTests.cs` owns the xUnit
execution surface for the PresentMon parser, ssctl pipe transport, KS
audio-node, EGAVDS probe, RTK I2C unsafe-native-path guard, and former legacy
NVML snapshot/CaptureSessionSnapshot tool-model checks; the public wrapper
classes remain separate inside that file so test identities stay stable. The
matching `tests/Sussudio.Tests/ToolProbeContracts.Tests.cs` implementation owner
keeps the PresentMon parser, ssctl pipe transport, KS audio-node, and EGAVDS
probe checks together unless one gains an independent fixture or executable
helper state. The legacy `HarnessCheckCatalog.ToolContracts.cs` registration
file has been retired.
Shared formatter tests now mirror the formatter owners: the root
snapshot-formatter test owns accessors, invalid-response handling, section
ordering, core section formatting, and the Flashback opt-in gate; Flashback
output, Preview D3D output, and source ownership live in the focused
`AutomationToolContracts.SnapshotFormatter.Tests.cs` implementation owner,
with `tests/Sussudio.Tests/XUnit.ToolContractsTests.cs` owning their xUnit
execution surface after removal from the legacy offline harness catalog. ssctl
formatter output smoke checks, timeline output contracts, and ssctl formatter
source ownership assertions live in
`tests/Sussudio.Tests/XUnit.ToolContractsTests.cs` through the shared
`RuntimeContractSource` formatter source-family readers after removal from the
legacy offline harness catalog.
ssctl command-handler routing coverage now lives in
`CommandHandlers.Routing.Tests.cs` for device, capture controls, recordings,
Flashback, window, manifest, observability, automation-flow, UI visibility, and
verification commands, source ownership, and help/catalog source-shape coverage,
with xUnit execution owned by `tests/Sussudio.Tests/XUnit.ToolContractsTests.cs`
after removal from the legacy offline harness catalog. Captured ssctl
`request.command` ID assertions now flow through `AssertSsctlCommandRequest`,
which delegates to `AssertAutomationCommandId` instead of duplicating numeric
IDs in routing tests. Fixed ssctl source
guards also live in `CommandHandlers.Routing.Tests.cs`; they require
`AutomationCommandKind` enum overloads at routing call sites while leaving
labels and wire IDs catalog-backed, with the dynamic diagnostic-session runner
channel intentionally remaining string-based.
`tests/Sussudio.Tests/ArchitectureDocs.ReferenceIntegrity.Tests.cs` owns
  shared implementations for consolidated AGENT_MAP reference resolution,
  test-owner code-span coverage, automation consumer checklist coverage,
  UI/presentation ownership coverage, CaptureService ownership coverage,
  Flashback preview startup AGENT_MAP wording, shared tool automation path
  coverage, duplicate tools/Common owner checks, empty test marker-shell checks,
  literal `ReadRepoFile` source-shape path resolution, cleanup-plan file/folder
  reference drift checks, architecture-doc test-family coverage, shared
  Markdown code-span path-token extraction and resolution helpers, AGENT_MAP
  consumer coverage, ownership-file discovery, exact code-span policy, xUnit
  inventory discovery, the xUnit migration inventory guard, and the xUnit
  execution surface for those architecture-doc checks after removal from the
  legacy offline harness catalog.
Shared harness helpers now live in one support boundary,
`tests/Sussudio.Tests/HarnessCore.cs`: generic assertions, repo-file/source
text helpers, reflection/property access, wait helpers, and synthetic
capture/recording object factories, tool assembly loading, isolated load
contexts, and stale-build detection.
Synthetic MJPEG timing metric factories live with the health snapshot ownership
and cached-metrics scenarios that use them in
`tests/Sussudio.Tests/CaptureService.HealthSnapshots.AssemblyAndSamplerOwnership.Tests.cs`.

Shared capture configuration reflection helpers for remaining legacy capture
model checks now live in `tests/Sussudio.Tests/HarnessCore.cs`.
`tests/Sussudio.Tests/XUnit.CaptureConfigurationModelsTests.cs` owns shared
reflection helpers plus capture mode option, capture settings/MJPEG
HFR/bitrate policy, MediaFormat equality/hash-code behavior, recording
selection, encoder support, and recording pipeline option xUnit contract checks
without scattering one contract surface across several wrapper files.
`tests/Sussudio.Tests/XUnit.RecordingContractsTests.cs` owns recording contract
DTO checks plus the former recording pipeline, recording-model, and
core-runtime recording xUnit execution surfaces: recording queue, LibAv sink,
WASAPI, capture fan-out, CaptureService recording ownership, recording
verifier, LibAv encoder, Flashback integrity, recording-facing shared
formatter, dedicated LibAv verification script, capture runtime failure flags,
and Flashback buffer manager behavior/source-ownership checks after their
removal from the legacy offline harness catalog. Keep the public wrapper
classes in this file unless a group needs independent fixture state.

`tests/Sussudio.Tests/PooledVideoFrame.Tests.cs` now keeps only shared
pooled-frame and jitter-buffer helpers. Pooled-frame coverage is split into
lease lifecycle/fan-out/queued-release contracts, MJPEG jitter
frame-ingress/adaptive policy, and MJPEG jitter queue/drop/reprime behavior.
CPU MJPEG pipeline runtime checks now execute through
`tests/Sussudio.Tests/MjpegPipeline.Tests.cs`, keeping pipeline,
cadence, pooled-frame, preview-jitter, and queued lease-release contracts in
xUnit after their removal from the legacy harness catalog.
Flashback xUnit wrapper checks now execute through
`tests/Sussudio.Tests/XUnit.FlashbackContractsTests.cs`, keeping Flashback
buffer option sizing behavior, DTO contracts, reflection/nullability helpers,
encoder sink, playback, decoder, and exporter registration classes together
while preserving their frame-rate, codec, counter, queue, force-rotate,
startup, command-queue, source-shape, cadence, frame-buffer, state/lifetime,
timestamp, audio, request-validation, segment, cancellation, output
path/finalization, and source-ownership contracts after their removal from the
legacy harness catalog.
Core runtime recording checks now execute through
`tests/Sussudio.Tests/XUnit.RecordingContractsTests.cs`, keeping recording
verifier, LibAv encoder, Flashback integrity, shared formatter, and dedicated
LibAv verification script contracts in xUnit after their removal from the
legacy harness catalog.
Core runtime checks now execute through
`tests/Sussudio.Tests/XUnit.CoreRuntimeContractsTests.cs`, keeping runtime
telemetry, capture-service snapshot, NativeXu, frame-ledger, recording-integrity,
RuntimePaths, runtime helper, bounded process-supervision, FFmpeg runtime
location, and basic app contract checks in xUnit after their removal from the
legacy harness catalog. The shared `RuntimeContractSource` tool/source-family
reader helpers also live with this runtime xUnit owner.
Automation checks now execute through
`tests/Sussudio.Tests/XUnit.AutomationContractsTests.cs`, keeping App exception
policy, converter/display formatting, LoggingJsonContext, MainWindow automation
surface, pipe/auth, Stream Deck auth-envelope, ViewModel/Flashback UI,
dispatcher, capture/Flashback routing, snapshot projection, catalog/manifest,
and diagnostics-loop checks in xUnit after their removal from the legacy
harness catalog. The LoggingJsonContext source-generation payload checks also
live in this xUnit owner instead of a standalone legacy `Program` sidecar.
Presentation-preview MainViewModel checks now execute through
`tests/Sussudio.Tests/XUnit.PresentationPreviewContractsTests.cs`,
keeping initial recording transition failure propagation, audio controls and
monitoring, output path and disk-space presentation, source telemetry,
dependency composition, automation/runtime routing, capture settings, preview
lifecycle, and audio ramp trace telemetry in xUnit after their removal from the
legacy harness catalog.
Presentation-preview MainWindow checks now execute through
`tests/Sussudio.Tests/XUnit.PresentationPreviewContractsTests.cs`,
keeping window lifecycle, launch/startup, preview screenshot, shell chrome,
visual shell, recording controls, audio controls, responsive layout, capture
selection, resolution selection, capture runtime guardrail, MainWindow initial,
preview runtime shell/policy, capture option, and output path contracts in xUnit
after their removal from the legacy harness catalog.
MainViewModel presentation-preview contract execution is now owned by that
focused xUnit wrapper file, with no remaining legacy catalog hook.
Presentation-preview capture runtime guardrail checks execute through the
MainWindow xUnit wrapper above, keeping recording stop failure propagation,
preview stop overload/API compatibility, and emergency recording stop threading
contracts in xUnit after
their removal from the legacy harness catalog.
Presentation-preview capture Flashback buffer checks now execute through
`tests/Sussudio.Tests/XUnit.PresentationPreviewContractsTests.cs`,
keeping stale session cleanup and recovery-preserve contracts in xUnit after
their removal from the legacy harness catalog.
Project build/publish policy checks now execute through the app-surface test
cluster in `tests/Sussudio.Tests/XUnit.AutomationContractsTests.cs`, keeping
the English-only publish locale and latest-build staging contracts in xUnit
after their removal from the presentation-preview capture catalog.
Presentation-preview D3D checks now execute through
`tests/Sussudio.Tests/XUnit.PresentationPreviewContractsTests.cs`, keeping
the former D3D pacing, geometry/screenshot, present-cadence, device-lost,
diagnostics, contracts/metrics, runtime-capture, render setup/resource, and
render-pipeline contracts in xUnit after their removal from the legacy D3D
harness catalog. The empty legacy D3D catalog hook was removed after the final
group moved to xUnit.

Fullscreen transition mechanics now live in
`Sussudio/Controllers/FullScreen/FullScreenController.cs`. Keep the controller
as the single owner for public toggle/state, enter/exit orchestration, rect
animation, chrome/material state, overlay pointer/auto-hide behavior, full-screen
key routing, and timeline eligibility. `.Composition.cs` wires controller callbacks directly into the FullScreen
context, `.Commands.cs` owns button/menu/double-tap and automation command
adapters, `.Input.cs` owns key routing, and `.Overlay.cs` owns pointer and
auto-hide adapters. Flashback command execution lives with the Flashback UI
controllers in `Sussudio/Controllers/Flashback/FlashbackUiControllers.cs`.

Automation whole-window screenshot capture now lives in
`Sussudio/Controllers/Screenshot/ScreenshotControllers.cs`, which owns
UI-thread dispatch, cancellation, failure wrapping, native PrintWindow/GDI
capture, output directory creation, screenshot result shaping, and pure PNG/BMP
byte-stream encoding.
Whole-window screenshot automation stays on `MainWindow.Composition.cs` with the
other `IAutomationWindowControl` methods.

Preview-frame screenshot button behavior now lives in
`Sussudio/Controllers/Screenshot/ScreenshotControllers.cs`.
`Sussudio/Controllers/Screenshot/ScreenshotControllers.cs` owns the pure output
directory fallback, file naming, status text, and log text policy.
`MainWindow.xaml.cs` is the XAML-facing adapter; the controller keeps
directory creation, preview-frame capture, logging side effects, and button
enable/disable state.
Renderer-level preview frame capture request state, timeout/cancellation
handling, render-thread GPU readback, before-present screenshot dispatch,
error-result construction, capture-result logging, and off-thread PNG
completion gate state now live in
`Sussudio/Services/Preview/D3D11PreviewRenderer.RenderPasses.cs`.
Preview-frame capture staging-resource reuse and teardown also live there.
Preview-frame BMP/PNG pixel conversion, mapped-frame buffer copying, luminance
analysis, and letterbox/pillarbox measurement live in
`Sussudio/Services/Preview/PreviewScreenshotCapture.cs`: mapped-frame copying,
shared pixel analysis, 16-bit PNG frame capture, BMP capture/header writing,
and the 16-bit PNG file container/chunk/CRC helpers.

Window geometry automation and the recordings-folder command now live in
`Sussudio/Controllers/Window/WindowAutomationController.cs`. Display-area/AppWindow
access, UI-thread dispatch, presenter restore, side effects, and pure
snap-region rectangle math for window actions stay there.
`MainWindow.Composition.cs` is the `IAutomationWindowControl` adapter.
Close lifecycle state remains separate from geometry automation; see the
explicit window close lifecycle section below for the close-state and recording
finalization owners.

UI-thread dispatching helpers, preview-snapshot-style result dispatch with
three-attempt enqueue retry, and guarded async event-handler execution now live
in `Sussudio/Controllers/UiDispatchControllers.cs`.
`Sussudio/MainWindow.Composition.cs` keeps the stable private MainWindow adapter
names for callers. Window close completion, close-request dispatch, and
recording finalization are covered by the explicit window close lifecycle
section below.

MCP command-routing coverage is split into capture, host/pipe, recording,
formatter batching, device, pipeline,
UI, and verification owner files. Captured `request.command` ID assertions now
flow through `AssertAutomationCommandId` instead of duplicating numeric IDs in
routing tests. Cross-tool source guards now live with the route owner in
`McpToolSurface.CommandRouting.Tests.cs` and require fixed-command MCP
automation routes to use `AutomationCommandKind` enum overloads at the pipe
seam while preserving existing labels and wire IDs. Catalog/manifest-backed
dynamic batches and
diagnostic-session runner command-channel delegates intentionally remain
string-based.

First-load startup, initial ViewModel/device refresh, automation startup timing,
and the launch entrance trigger now live in
`Sussudio/Controllers/Launch/LaunchFlowController.cs`.
`Sussudio/MainWindow.Composition.cs` owns the XAML-facing shell
launch/chrome adapter surface, including the Loaded adapter and native shell
bootstrap wiring.
Automation host composition, once-only
startup, ready/disabled logging, and pipe-before-hub shutdown disposal now live
with the window automation command owner in
`Sussudio/Controllers/Window/WindowAutomationController.cs`.
`Sussudio/Controllers/Launch/LaunchFlowController.cs` starts that
controller after initial device refresh, and
`Sussudio/MainWindow.xaml.cs` passes its async dispose
delegate into the shutdown controller. Window close
completion lives in `Sussudio/Controllers/Window/WindowCloseLifecycleController.cs`;
recording-aware close finalization lives there too.

Top-level shell resize telemetry throttling for preview compositor transforms
now lives in `Sussudio/Controllers/Preview/Renderer/PreviewRendererHostController.cs`.
`Sussudio/MainWindow.Composition.cs`
wires renderer-host context callbacks, the `SizeChanged` adapter, renderer-host
reset handoff, and stable start/stop/shutdown/reinit-unsafe-window automation
adapters. Preview surface sizing, GPU panel visibility, video/control-bar
composition shadow visuals, bounds alignment, clear behavior, compositor
opacity fade routing, preview shell/content transitions, startup overlay, and
reinit transition state now live together in
`Sussudio/Controllers/Preview/PreviewTransitionAnimationController.cs`.
`Sussudio/MainWindow.Composition.cs` is the XAML-facing adapter
for preview renderer and surface wiring.
`Sussudio/Controllers/Preview/Renderer/PreviewRendererHostController.cs` owns renderer
startup dimension/fps/HDR/min-present-interval planning.
`Sussudio/Controllers/Preview/Renderer/PreviewRendererHostController.cs` owns hosted preview
renderer context, public runtime state, counters, start/stop/shutdown flow,
renderer startup planning, CPU fallback attachment, D3D renderer startup and
event/failure handling, cleanup, D3D reinit renderer-stop/timeout policy,
disposal, unsafe-window telemetry, stop tick accounting, fresh SwapChainPanel
replacement, and retired-renderer handoff during D3D renderer mode switches.
`Sussudio/MainWindow.Composition.cs` owns the stable automation
preview snapshot adapter and context wiring alongside preview renderer host
composition.
`Sussudio/Controllers/Preview/Renderer/PreviewRuntimeSnapshotController.cs`,
owns the UI-dispatch sampling wrapper, UI-thread-only preview runtime field
sampling, startup missing-signal refresh, sampled-input assembly, read-only
preview-state orchestration, and the UI-thread sampled preview snapshot input
contract shared by the snapshot controller and D3D projection builder. It also
owns final preview runtime snapshot DTO flattening from sampled input and D3D
projection, surface/startup/GPU playback projection policies, the health input factory,
preview startup elapsed timing, and blank/stall suspicion policy.
`Sussudio/Controllers/Preview/Renderer/PreviewRuntimeD3DProjection.cs` owns the
renderer projection data contract, D3D policy records, policy evaluation order,
and assignment from evaluated policy records. It keeps the named policy classes
for D3D-vs-CPU frame counters, renderer state, display cadence, render CPU
timing, pipeline latency, frame ownership, DXGI frame statistics, and
frame-latency wait defaults in one cohesive projection owner.
Close routing/finalization handling remains in the explicit window close
lifecycle owners below.

Window title base/build-stamp formatting and the recording-time suffix now live
in `Sussudio/Controllers/Shell/ShellChromeController.cs`; `MainWindow.Composition.cs`
keeps the XAML-facing initialization and title assignment hook because title
refreshes are driven by status/recording presentation.

Window close lifecycle and native window helpers are now explicit:
`Sussudio/Controllers/Window/WindowCloseLifecycleController.cs` owns close request
flags, completion TCS, cleanup latch, close-in-progress classification,
automation close dispatch orchestration, actual close request execution,
recording finalization side effects, and post-close cleanup order:
`Close()`, completion timing after non-recording closes,
close-in-progress success handling, COM `Application.Current.Exit()` fallback,
requested-state reset after unexpected failures, and `AppWindow.Closing`
decision choreography, the 120-second stop budget, `StopRecordingAndWaitAsync`
wait race, timeout/failure breadcrumbs, status text, shutdown-content
dim/restore policy, timer stops, event detaches, preview shutdown,
post-close recording finalization handoff, automation diagnostics disposal,
NVML disposal, and ViewModel disposal.
`Sussudio/MainWindow.Composition.cs` is the XAML/AppWindow close adapter and
keeps `RegisterCloseLifecycle`, `CloseAsync`, and `RequestWindowClose()` stable.
`Sussudio/MainWindow.xaml.cs`
wires MainWindow cleanup delegates and the stable `Closed` event adapter into
the controller, and owns the timer,
event-detach, stats, recording-visual, and preview-size cleanup delegate
adapters.
Native `AppWindow` lookup, ViewModel window handle handoff, minimum-size
subclassing, DWM cloak/dark-mode setup, first-composed-frame shell reveal
scheduling/cancellation, initial shell size, icon, and uncloaking now live in
`Sussudio/Controllers/Window/WindowCloseLifecycleController.cs`.
`Sussudio/MainWindow.Composition.cs` is the XAML-facing shell
launch/chrome native-window adapter and keeps the `_hwnd` field consumed by screenshot and window
automation paths.
MainWindow shell ownership tests mirror these runtime owners through focused
`MainWindow.ShellOwnership.*.Tests.cs` files for chrome, startup, preview
runtime, native bootstrap, and window lifecycle contracts.
Preview runtime source-shape coverage is split across renderer-host,
snapshot, D3D-projection, and surface test owners so failures point at the
runtime owner that actually drifted instead of one combined harness check.
MainWindow Flashback ownership tests mirror the Flashback controller owners
through `MainWindow.FlashbackOwnership.Tests.cs`: polling, timeline
presentation, playhead/CTI motion, playback presentation/coordinator behavior,
and settings/command binding remain named test methods in one Flashback
ownership spec.

Audio and microphone meter rendering, initial audio/microphone control
projection, and event hookup now live in
`Sussudio/Controllers/Audio/AudioControlBindingController.cs`: the controller
owns the audio-control binding context, initial audio/microphone projection,
preview-volume binding and priming, audio/microphone/device-audio selection
handlers, record/preview/custom-audio/microphone toggle handlers, audio-meter
activation, initial meter presentation, device-audio gain/meter resize hooks,
audio/microphone property-change projections for audio toggles, monitoring
meter state, preview-volume slider sync, microphone enablement, microphone
volume sync, meter setup, XAML/view-model meter dependencies, smoothing,
markers, resets, timer lifetime, `TranslateMarker`, monitoring/disabled
animations, and rounded clips.
`Sussudio/MainWindow.xaml.cs` is the XAML-facing audio, microphone,
and audio-meter adapter;
video-format collection setup, initial capture/recording option projection, and
code-attached resolution/frame-rate handlers now live in
`Sussudio/Controllers/Capture/CaptureOptionBindingController.cs`, with
`MainWindow.xaml.cs` left as the XAML-facing capture and
recording option adapter.
Flashback settings-control initialization, GPU decode binding/sync, and buffer
duration combo sync now live in
`Sussudio/Controllers/Flashback/FlashbackUiControllers.cs`.
`MainWindow.xaml.cs` owns construction, startup event wiring, and the root
`SetupBindings()` sequence; device-selection change hooks, initial recording
lockout projection, and stats visibility sync route through their existing
feature adapters/controllers.

Capture session transition legality, mutable session state, and transition
generation now live in
`Sussudio/Models/Capture/CaptureModels.cs`. The state machine
applies the policy before entering a transition and delegates steady-state
resolution to the same pure policy. `CaptureService.cs` owns serialized transition execution,
transition-state entry, steady-state input sampling and resolution, fault
publication, lock release, public initialization, and cleanup/disposal/current-state
helpers, so cleanup, disposal,
and fatal cleanup keep their flow ownership without writing session state
directly. The policy/state-machine pair is a transition-entry gate plus steady-state resolver, not a full workflow graph: in-place serialized
mutations may pass the current state to the transition lock, while
lifecycle-changing operations should pass an explicit target
`CaptureSessionState`. Active recording backend resource ownership now lives in
`Sussudio/Services/Capture/CapturePipelineResources.cs`.
Capture session coordinator command enums, queue receipt records, session
snapshots, queued Flashback mutations, read-only Flashback status, Flashback
playback/buffer status projections, export and segment query forwarding,
playback/scrub/marker command adapters, and active playback-controller
readiness/rejection logging now live in
`Sussudio/Services/Capture/CaptureSessionCoordinator.cs` with construction,
shared state fields, the public
lifecycle/audio/Flashback command facade into the serialized worker, and
queue/session snapshot projection, queue work item creation, command enqueueing,
enqueue-failure handling, disposed-state ingress guards, worker-loop execution,
command coalescing, operation cancellation/failure accounting, pending-command
failure drain, pending-command counter decrement policy, and dispose/drain/cancel
lifecycle for the worker queue and cancellation token source.
Capture session coordinator API/command/snapshot contracts, focused
source-ownership contracts, queue behavior, Flashback/cancellation behavior,
transition policy, and shared reflection harness helpers now live in the
coordinator API test owner.

Device discovery ownership lives in `DeviceService.cs`. Keep capture/audio
enumeration orchestration, the combined discovery result, device
priority/capability scoring, audio endpoint association, native XU interface
path resolution, format cache serialization, and inline/background format
probing together there.

Native XU Kernel Streaming calls are grouped under
`Sussudio/Services/Capture/NativeXu/`. Keep KS category constants, DTOs,
SetupAPI interface enumeration, handle opening, topology node parsing, XU
GET/SET transfer shapes, and P/Invoke struct declarations in
`KsExtensionUnitNative.cs`. Keep shared 4K X identity, selected-interface
projection, and native transport gate ownership in `KsExtensionUnitNative.cs`.
`tools/NativeXuAudioProbe` links the bridge file explicitly, so update its
project file when this bridge changes. `tests/Sussudio.Tests/NativeXuAtCommandProvider.Tests.cs`
owns the cohesive KS bridge and probe-link ownership checks.

Native device enumeration ownership is grouped under
`Sussudio/Services/Capture/DeviceDiscovery/`. Keep Media Foundation constants,
GUIDs, P/Invoke declarations, MF video-device enumeration, WASAPI capture
endpoint enumeration and friendly-name reads, native video format probing,
subtype/FourCC naming, and direct plus enumeration-fallback MF source
activation in `MfDeviceEnumerator.cs`.

Capture service source telemetry polling, provider reads, fallback snapshot
construction, merge policy, capture-format runtime telemetry, NTSC frame-rate
correction, frame-rate argument formatting, and observed pixel-format
normalization/reset/counters now live with the read-only diagnostics and
automation probe helpers in
`Sussudio/Services/Capture/CaptureService.Snapshots.cs`. The root capture service
owns shared state, construction, and public event surface, but these diagnostics
are no longer embedded in the lifecycle/orchestration file.

Capture service initialization now lives in
`Sussudio/Services/Capture/CaptureService.cs` with shared service state,
construction, and the public event/property surface. The root owns the public
initialization transition, initial selected device/settings capture,
negotiated-format seeding, the initial observed-pixel telemetry reset call,
fallback source telemetry, source telemetry refresh, NTSC frame-rate correction,
and initialized status event.

WASAPI audio-level/failure event projection, audio-preview start/stop
lifecycle, and live audio input switching now live in
`Sussudio/Services/Capture/CaptureService.PreviewLifecycle.cs`. Preview-time
microphone monitoring lives in
`Sussudio/Services/Capture/CaptureService.PreviewLifecycle.cs` for shared
state, mic-level forwarding, writer-detach/disposal cleanup, the public update
transaction, preview-time Flashback mic writer attachment, and post-recording
restart/reattachment.
Flashback preview/recording backend audio input restoration is folded into
`Sussudio/Services/Capture/CaptureService.FlashbackRecording.cs` beside
Flashback audio attachment and recording topology validation.
`Sussudio/Services/Capture/CapturePipelineResources.cs` owns the live
program WASAPI capture, microphone capture, playback startup/shutdown,
audio-monitor attach/detach order, preview volume/mute application, playback
cleanup helpers, capture-fault telemetry, active recording backend resources,
and video pipeline resources. CaptureService callers use these aggregates
directly instead of private root resource shims. These files preserve
the root service transition lock while keeping preview lifecycle, input
switching, mic cleanup, post-recording mic monitor restart, and playback routing
from collapsing back into a general audio partial.

Explicit capture cleanup now lives in
`Sussudio/Services/Capture/CaptureService.cs`. That file owns the
public cleanup transition, shutdown teardown order, failed Flashback recording
segment preservation, deferred LibAv/unified-video cleanup handoff, WASAPI
capture disposal, mic teardown, telemetry stop, the call to CaptureService's
final session-state reset helper, fatal capture/recording/Flashback backend
failure callbacks, fatal cleanup launches, Flashback backend cleanup,
GPU device-lost classification, recovery segment preservation, generation-stale
guards, and last-failure telemetry state/projection.

Capture transition execution now lives in
`Sussudio/Services/Capture/CaptureService.cs` beside the public initialization
entry point. That file owns `RunTransitionAsync`, transition-state entry,
steady-state resolution, fault publication, transition-lock release,
current-state/generation projection, steady-state input sampling,
cleanup/disposal state helpers, and initialization/disposal guards. Mutable
session state and transition generation live with
`Sussudio/Models/Capture/CaptureModels.cs`. Cleanup, disposal,
and fatal cleanup paths call those helpers while preserving their special
teardown order.
Best-effort resource release helpers are delegated to
`Sussudio/Services/Capture/CaptureService.cs`.

Disposal-triggered cleanup and dispose flow live with explicit cleanup in
`Sussudio/Services/Capture/CaptureService.cs`; disposed-state writes
route through root CaptureService transition helpers. Coordination lock disposal is delegated to
`Sussudio/Services/Capture/CaptureService.cs`.

Capture resource release helpers now live in
`Sussudio/Services/Capture/CaptureService.cs` alongside disposal and
shutdown teardown. That file owns best-effort semaphore release/disposal,
coordination-lock disposal, Flashback backend/export held-lock release helpers,
and Flashback eviction resume warnings used by lifecycle/export/cleanup
partials.

Deferred Flashback artifact cleanup adapter handoff and export-lock delegation
now live with the Flashback controls owner in
`Sussudio/Services/Capture/CaptureService.FlashbackControls.cs`.
Deferred unified-video cleanup after LibAv drains lives with the video pipeline
resource owner. Pending LibAv drain task state and reentry policy live in
`Sussudio/Services/Capture/CapturePipelineResources.cs`. Flashback backend
artifact cleanup request/retry/dispose/purge mechanics live in
`Sussudio/Services/Flashback/FlashbackBackendResources.cs`.

Capture read-only automation probes now live in
`Sussudio/Services/Capture/CaptureService.Snapshots.cs` alongside diagnostics
and automation snapshot projection. Video source probing, preview color probing,
and preview-frame screenshot waits stay separated from runtime lifecycle
mutation code.

Flashback-facing capture controls now live in focused CaptureService partials:
`Sussudio/Services/Capture/CaptureService.FlashbackControls.cs` owns public
Flashback state, segment access, enable/disable transition gating, restart
entry points, committed restart orchestration after preview backend teardown,
buffer/GPU settings updates, live playback-controller GPU decode propagation,
recording-format changes, active encoding-setting application, encoder-setting
cycles, and rollback after failed Flashback buffer cycles
while backend resource construction stays in the Flashback preview backend
partials.

Flashback recording backend ownership, audio attachment, encoded-frame
forwarding, and recording topology validation now live in
`Sussudio/Services/Capture/CaptureService.FlashbackRecording.cs`. Flashback
recording session-context policy, codec selection, GPU handle handoff, HDR
guardrails, delivered-cadence frame-rate rational preservation/inference, and
legacy Flashback export verification/downgrade snapshot fields stay with that
same Flashback recording owner.
Preview-backend resource state now belongs to
`Sussudio/Services/Flashback/FlashbackBackendResources.cs`, which owns the
preview backend resource grouping, install/take/clear state, and
recovery-preserve flag storage and policy. It also owns recording-finalize
handoff plus the video/audio/microphone attach and detach request shapes and
feed wiring used by preview startup, buffer cycling, teardown, and rollback.
It owns preview backend startup construction/install/playback initialization,
startup failure rollback cleanup, sink-only buffer-cycle orchestration,
purge/finalize decisions, full-rebuild fallback outcomes, playback disposal,
old-sink stop/dispose, replacement sink startup/playback restore, failed
replacement cleanup, backend artifact cleanup request/retry/dispose/purge
mechanics, preview-backend teardown mechanics, sink stop/dispose, and backend clear. `CaptureService`
supplies the service-level export-lock adapter, purge-policy resolution,
cancellation-token choice, and full rebuild fallback orchestration while using
the backend aggregate directly instead of private root resource shims.

Recording start lifecycle now lives in
`Sussudio/Services/Capture/CaptureService.RecordingLifecycle.cs`. That file owns
the public recording start transition surface, startup-path routing, the private
rollback-state holder, recording output-folder resolution, LibAv and Flashback
`RecordingContextRequest` assembly, standard LibAv recording startup sequencing,
video-capture reuse/creation, source-reader compatibility checks, preview
sink/shared-device handoff, video pipeline installation, audio-input startup,
WASAPI sink attachment, preview playback preservation, recording microphone
capture wiring, and recording-start rollback cleanup.
`CaptureService.FlashbackRecording.cs`
owns Flashback recording fast-path reuse, backend startup, live-edge
finalize/export handoff, boundary snapshots, post-finalize reconciliation, and
Flashback-specific microphone monitor restart.
Recording
stop lifecycle now lives in
`Sussudio/Services/Capture/CaptureService.RecordingLifecycle.cs`, including
normal stop routing, the emergency stop overload that feeds finalization, and
the stop/finalize dispatcher for active Flashback and LibAv backends. The same
file owns standard LibAv recording finalization sequencing: unified-video
recording stop, source-reader boundary diagnostics, WASAPI recording sink
detach, microphone capture disposal before sink stop, LibAv sink
normal/emergency stop, sink disposal, LibAv drain task tracking,
inactive-preview teardown after recording, audio-fault folding,
encoder/runtime and recording-integrity summaries, final state completion,
pending Flashback enable-after-recording detection, guarded Flashback preview
backend restore, failed-restore rollback
and purge, standard post-recording microphone monitor restart,
`FLASHBACK_ENABLE_AFTER_RECORDING_*` breadcrumbs, preview-restore ordering, and
the visible final outcome publication before delayed cancellation throws.
Recording outcome field publication now lives with
`Sussudio/Services/Capture/CaptureService.RecordingLifecycle.cs`; post-recording
microphone monitor restart mechanics live in
`Sussudio/Services/Capture/CaptureService.PreviewLifecycle.cs`.
The live-edge boundary snapshot in
`Sussudio/Services/Capture/CaptureService.FlashbackRecording.cs`
keeps idempotent `EndFlashbackRecordingAccounting()` calls, source-frame
counters, recording integrity counters, and audio integrity counters with the
backend finalization path that consumes them.
`Sussudio/Services/Capture/CaptureService.RecordingLifecycle.cs` owns the
helper boundary that publishes recording-start and recording-finalize outcome
fields (`_lastOutputPath`, `_lastFinalizeStatus`, `_lastFinalizeUtc`, and
`_lastPreservedArtifacts`) without leaving direct write blocks in start or
finalization call-site partials. It also owns failed-start logging, last-failure
publication, Flashback recording rollback accounting, artifact rollback, and
best-effort teardown for partially started sinks, WASAPI capture, unified-video
capture, and deferred LibAv drain cleanup after a failed recording start.

Flashback export failure classification now lives with export diagnostics in
`Sussudio/Services/Capture/CaptureService.FlashbackExportCore.cs`.
Keep the export failure-kind taxonomy there because automation responses and
capture diagnostics both consume the diagnostic result classification.

Flashback export entry points now live in
`Sussudio/Services/Capture/CaptureService.FlashbackExportCore.cs`.
Keep range export, last-N export, lock-scoped backend reference capture,
session/backend lock release before native export, and routing into
range-resolution and shared-core owners there. Flashback export range
resolution now lives in
`Sussudio/Services/Capture/CaptureService.FlashbackExportCore.cs`.
Keep range and last-N post-eviction range resolution, buffer position clamps,
and PTS offset math there. The shared export lifetime now lives in
`Sussudio/Services/Capture/CaptureService.FlashbackExportCore.cs`; keep
export-operation locking, eviction pause/resume, diagnostics completion,
exporter execution, active-file fallback, `FlashbackExportRequest`
construction, throttle-provider wiring, partial-fallback result marking, and
cleanup there. Segment metadata mapping, live-export throttle policy, segment
path normalization, and segment PTS timestamp repair also live there because
they are part of `FlashbackExportRequest` assembly. Flashback export
force-rotate preparation also lives there; keep failure/committed-pending
outcomes, timeout fallback segment discovery, and related diagnostics/logging
with the request assembly path.

Flashback export diagnostics now lives in
`Sussudio/Services/Capture/CaptureService.FlashbackExportCore.cs`.
Keep export attempt lifecycle, result, rejection, completion diagnostic state,
progress forwarding/normalization, force-rotate fallback counters, and
failure-kind classification there.

Shared video-pipeline lifecycle handoff now lives in
`Sussudio/Services/Capture/CaptureService.PreviewLifecycle.cs`, alongside preview
reuse/fresh-start orchestration. That file owns preview-frame sink attachment,
late Flashback playback preview wiring, shared D3D preview-device handoff, and
fatal/pixel callback attach/detach.
Active video capture storage, preview-frame sink storage, negotiated video
getters, cached MJPEG pipeline timing snapshots, and deferred unified-video
cleanup after LibAv drains now live in
`Sussudio/Services/Capture/CapturePipelineResources.cs`; CaptureService
callers use that aggregate directly instead of private root resource shims.

Preview lifecycle now lives in one CaptureService owner:
`Sussudio/Services/Capture/CaptureService.PreviewLifecycle.cs` owns the
video-preview start/stop transition entry points and sequencing, preview
pipeline and Flashback backend recycle decisions before start, retained-backend
fast-path reattachment, retained video/Flashback backend reuse checks,
capture-settings cloning, fresh UVC startup, preview-start rollback, fresh
preview backend startup ordering, keep-pipeline-alive detach semantics,
stopped-state/telemetry commit, preview pipeline disposal ordering, Flashback
backend disposal, WASAPI disposal, microphone cleanup, preview WASAPI capture
startup, video-only audio fallback logging, preview playback attach,
preview-time microphone monitor startup, partially-started audio rollback,
committed live audio input switching, and Flashback audio attach. Shared
unified-video cleanup mechanics still delegate to the video pipeline resource
owner.

Recording integrity policy now lives in
`Sussudio/Services/Capture/CaptureService.RecordingIntegrity.cs`. That owner
keeps active-backend resolution, private video/audio counter DTOs, baseline
deltas, final `RecordingIntegritySummary` DTO construction, normalized
video/audio summary handoff fields, integrity status, reason, audio-status
classification, and the structured `RECORDING_INTEGRITY` log line together.
Snapshot partials consume that policy instead of containing it.

LibAv encoder initialization now lives in
`Sussudio/Services/Recording/LibAvEncoder.cs` with the core encoder state it
mutates. Keep FFmpeg
runtime initialization forwarding and the public encoder open/setup sequence
there, including native allocation order, hardware-frame fallback behavior,
muxer-option lifetime, open-state timing, startup failure cleanup, required
path/codec/dimension/frame-rate/bitrate checks, audio/microphone/HDR guards,
video codec context configuration, bitstream-filter selection, NVENC
preset/split-encode mapping, frame-size math, sample-format support, rational
conversion helpers, NVENC private option application, and video
bitstream-filter initialization.

LibAv encoder packet writing now lives in
`Sussudio/Services/Recording/LibAvEncoder.VideoFrames.cs` beside CPU/GPU video
submission. Keep video encoder packet drains, bitstream-filter packet drains,
timestamp rescaling, packet stream-index assignment, packet write accounting,
and interleaved video packet writes there.

LibAv encoder core state now lives in
`Sussudio/Services/Recording/LibAvEncoder.cs`. Keep encoder fields, stable
public state, open-state guards, FFmpeg error string conversion, structured
libav exceptions, D3D11 device-removed checks, open/setup orchestration, and
private setup policy there.

LibAv encoder audio stream handling now lives in
`Sussudio/Services/Recording/LibAvEncoder.Audio.cs`. Keep audio/microphone
stream state, public status properties, public audio/microphone sample entry
points, payload alignment checks, accumulator handoff, interleaved packet
writes, pending-sample flush, accumulator ingress, sample queue/drain helpers,
drift-corrected encode chunks, planar sample copies, prepared-frame drains,
drift-correction thresholds, sync counters, current-drift reporting, sync
warning logs, audio and microphone AAC stream creation, codec opening, stream
time-base setup, resampler/frame/buffer setup, microphone-specific setup, AAC
codec context configuration, frame allocation, accumulator allocation, and
sample-queue allocation there. Do not re-split `LibAvEncoder.AudioQueue.cs` or
`LibAvEncoder.AudioInitialization.cs` unless audio becomes a named collaborator
instead of another encoder partial.

LibAv encoder HDR frame side-data helpers now live with video frame submission in
`Sussudio/Services/Recording/LibAvEncoder.VideoFrames.cs`. Keep software-frame
and hardware-frame HDR mastering display and content-light metadata attachment
there, including parsing/applying mastering-display metadata strings.

LibAv encoder option/result models now live with the core encoder state in
`Sussudio/Services/Recording/LibAvEncoder.cs`. Keep `LibAvEncoderOptions` and
`RotateOutputResult` there unless they become a shared contract outside the
encoder family.

LibAv encoder CPU, D3D11, and CUDA video frame submission now lives in
`Sussudio/Services/Recording/LibAvEncoder.VideoFrames.cs`: CPU packed-frame
submission/copy, D3D11 hardware frames setup, CUDA hardware frame context
adoption, ArraySize=1 texture-pool creation, D3D11/CUDA hardware-frame
submission, texture-pool copy/reference setup, GPU device-removed checks,
software/hardware-frame PTS/keyframe assignment, HDR side-data attachment,
EAGAIN packet drains, and hardware-frame unref cleanup.
Output rotation, final close, trailer/logging, native frame/context/buffer
release, and encoder state reset now live with the core encoder state in
`LibAvEncoder.cs`.

LibAv encoder video submission now lives in
`Sussudio/Services/Recording/LibAvEncoder.VideoFrames.cs`. Keep CPU packed
frame submission, packed NV12/P010 plane sizing, source-buffer validation,
stride-aware plane copies, forced keyframe handling, D3D11/CUDA hardware
submission, per-frame HDR side-data attachment/removal, and video packet
drains there.

LibAv encoder output lifecycle lives with the core encoder state in
`Sussudio/Services/Recording/LibAvEncoder.cs`. Keep rotation IO close/reopen,
stream reinitialization, bitstream-filter reset, segment runtime resets, MP4
muxer option policy for open and rotated outputs, flush/final close, dispose,
trailer writing, close-result logging, final output telemetry, native
frame/context/buffer release, hardware texture pool release, and encoder state
reset together with the fields and open-state guards they mutate.

Recording artifact context creation stays in
`Sussudio/Services/Recording/RecordingArtifactManager.cs`, including temp/final
output file naming, HDR-active context fields, mux success/failure cleanup,
final-output validation, rollback, preserved temp-artifact discovery, and
best-effort artifact deletion.

LibAv recording sink queue ownership now lives in
`Sussudio/Services/Recording/LibAvRecordingSink.Queueing.cs`. Keep public
video/GPU/CUDA enqueue entry points, hot audio/microphone WASAPI write adapters,
caller-side validation, audio queue eviction, audio remaining-buffer cleanup,
the audio packet DTO, shared video queue latency/backpressure tracker, and
shared work-signal/fatal-failure/queue-depth-underflow helpers there.
`tests/Sussudio.Tests/RecordingQueue.OverloadPolicy.Tests.cs` owns the queue,
submission, and cleanup assertions for this family alongside LibAv sink
lifecycle checks.
Video/GPU/CUDA queue admission policy, TryWrite depth accounting, overload
fatal signaling, queue cleanup, pooled video buffer leasing, pooled packet
return helpers, video packet records, and queue dwell-time metric sampling also live in
`LibAvRecordingSink.Queueing.cs`. `LibAvRecordingSink.cs` owns root state,
construction, read-only telemetry, encoder drift accessors, the
`IRecordingSink.StartAsync` adapter, FFmpeg/runtime initialization, encoder
option creation/application, per-recording video session setup, hardware-frame
queue selection, video/GPU/CUDA channel creation, width/height session state,
video/GPU/CUDA metric reset, video diagnostics reset, audio/microphone queue
setup, startup sequencing, encoding-task creation, start logging, startup
rollback cleanup, the background encode loop, dispose/deferred cleanup, public
and emergency `StopAsync` routing, `_started` clearing, encode-drain deadline
selection, emergency cancellation/flush fallback, encoding-failure
classification, HDR script validation through the bounded process supervisor,
stopped-output validation handoff, stop logging, and `FinalizeResult` shaping.

LibAv recording sink encode-loop and packet-drain ownership now lives in
`Sussudio/Services/Recording/LibAvRecordingSink.cs`. Keep the
background loop ordering, second audio/microphone drain pass, cancellation
cleanup, fatal encoder failure handling, bounded video/GPU/CUDA drain batches,
unbounded LibAv audio/microphone drains, frame-encoded event dispatch, GPU
texture release, CUDA frame free, and pooled buffer returns there.
`tests/Sussudio.Tests/RecordingQueue.OverloadPolicy.Tests.cs`
owns the LibAv sink queue, lifecycle, output-validation, drain-loop, and
packet-drain assertions.

Recording verifier ownership lives in
`Sussudio/Services/Recording/Verification/RecordingVerifier.cs`. Keep strict
verification orchestration, early failure results, primary mismatch parsing, HDR
parity, mismatch taxonomy, ffprobe process/spec/side-data probing, probe scalar
parsing, and ffprobe frame timestamp cadence analysis together there. Dimensions,
frame-rate, cadence, container/codec format, Flashback export verification
format resolution, and HDR validation policy stay with the orchestration and
result shaping that consume them.
`tests/Sussudio.Tests/RecordingVerifier.Integration.Tests.cs` keeps the
recording verifier integration seam together: shared fake process-supervisor,
runtime snapshot, verifier construction, verification invocation helpers, and
the early failure paths, source-shape/result DTO/script contracts, ffprobe
failure, process-priority, codec, Flashback verification format, mismatch, HDR,
and cadence scenarios that use that seam.

Native XU source telemetry detail assembly now lives with source snapshot
assembly in
`Sussudio/Services/Telemetry/NativeXuAtCommandProvider.SnapshotAssembly.cs`.
Keep detail row construction, flash-audio input interpretation, analog-gain
detail row insertion, input-source display text, and AT detail
byte/number/hex/ascii display formatters together there. Keep it linked from
`tools/NativeXuAudioProbe/NativeXuAudioProbe.csproj` whenever this partial
family changes, since that tool links shared provider files explicitly instead
of project-referencing the app.

Native XU public device and audio commands now live in
`Sussudio/Services/Telemetry/NativeXuAtCommandProvider.DeviceCommands.cs`.
Keep generic AT SET wrappers, named SET wrappers, and probe-facing raw AT reads
there with the public `SwitchAudioInputAsync` and `SetAnalogGainAsync` entry
points, the HDMI/Analog codec switch sequence, and the analog gain register
mapping/writes. Public device command routing now stays in one owner while
shared device support continues to enforce identity, selected-interface, and
transport gates.
Shared device identity, selected-interface projection, and native transport
gating live in `Sussudio/Services/Capture/NativeXu/KsExtensionUnitNative.cs`;
the root provider dispatches through that support into telemetry polling.

Selector-4 I2C payload writes now live with the root provider's AT transport
helpers in `Sussudio/Services/Telemetry/NativeXuAtCommandProvider.cs`.

Native XU selected-interface reading and active rolling polling now live in the root provider,
`Sussudio/Services/Telemetry/NativeXuAtCommandProvider.cs`, with public
`ReadAsync` validation, transport gate ownership, and interface enumeration.
Keep interface open failures, topology reads, dev-specific node selection,
per-node rolling-read iteration, and node-read failure classification there so
the public read path stays in one cohesive owner. Keep poll cadence gates,
cached AT-command fields, incomplete-cache handling, group advancement, rolling
command batch construction/refresh, per-command cancellation checks, raw AT
read/write frame construction, LRC/envelope handling, selector-4 I2C payload
writes, payload decoders, scalar helpers, and command failure formatting there
with the node-read path that calls them.

Native XU source snapshot assembly now lives in
`Sussudio/Services/Telemetry/NativeXuAtCommandProvider.SnapshotAssembly.cs`,
including source telemetry detail row formatting and audio-origin projection.
Keep the reference full-snapshot AT-command acquisition, full/rolling
AT-command-result handoff contract, VIC/frame-rate lookup policy,
AT-command-result decode into `SourceSignalTelemetrySnapshot`, diagnostic/detail
assembly, the `nativexu:` diagnostic-summary token contract, extended AT result
field formatting, and full-vs-rolling logging switches there.
Flash-audio analog-gain row insertion and snapshot audio-origin policy belong
to the audio-input telemetry detail partial.
Native XU payload decoding now lives with the root provider's AT transport
helpers in `Sussudio/Services/Telemetry/NativeXuAtCommandProvider.cs`. Keep AVI
InfoFrame decoding, HDR metadata decoding, scalar/ascii payload reads,
frame-rate rational inference, confidence scoring, and boolean token helpers
there with the frame/LRC/envelope helpers that feed them.

Flashback encoder sink startup now lives in
`Sussudio/Services/Flashback/FlashbackEncoderSink.cs` with the root lifetime
state it initializes and rolls back. Keep buffer session creation, generated
session ID formatting, encoder initialization, active-segment setup, startup
queue allocation, session validation, frame-rate fallback/clamping, startup
metric/counter reset, video diagnostics reset, start-failure rollback, PTS
continuation, background task startup, and start-transaction orchestration
there.

Flashback encoder root state now lives in
`Sussudio/Services/Flashback/FlashbackEncoderSink.cs`. Keep construction,
field ownership, public runtime counters, queue telemetry, encoder
status/format projections, startup transaction state, saturated PTS conversion,
non-negative byte/duration math, and best-effort eviction resume fallback
there.

Flashback encoder startup orchestration now owns generated session ID
formatting, encoder option creation, segment extension policy, transport
container selection, session frame-rate rational validation, `RecordingContext`
to `FlashbackSessionContext` projection, recording-format codec mapping,
split-encode mode wire mapping, and recording frame-rate argument parsing in
`Sussudio/Services/Flashback/FlashbackEncoderSink.cs`.

Flashback encoder queueing now lives in
`Sussudio/Services/Flashback/FlashbackEncoderSink.Queueing.cs`. Keep
video/audio/GPU packet DTOs, video enqueue result classification, ArrayPool
rent/return helpers, leased video packet disposal, best-effort video packet
cleanup, GPU texture release helpers, queued-buffer cleanup, queue
completion/signaling, shared queue-depth accounting, cancellation waits, failure
notification, remaining queued video/audio/microphone/GPU buffer return, depth
reset, raw/lease/GPU video input validation, texture AddRef ownership,
audio/microphone entry points, hot WASAPI writer adapters,
accepted/rejected/overloaded enqueue transactions, queue-full classification,
force-rotate audio queue guard policy, producer wakeup signaling,
disposed/not-started/cancelled/force-rotate/failure rejection reasons, channel
writes, queue-depth increments, max-depth updates, failed-write depth rollback,
last-reason state, backlog-eviction accounting, rejection counters, audio-drop
diagnostics, and throttled queue rejection logs there.

Flashback encoder loop orchestration now lives in
`Sussudio/Services/Flashback/FlashbackEncoderSink.EncodingLoop.cs`. Keep the
background encode loop, normal drain ordering, force-rotate dispatch,
force-rotate state, status projections, idle waits, `ForceRotateForExport`,
request publication, timeout/cancellation result classification,
committed-pending grace handling, pending-request cancellation, empty completion
on stop/dispose/failure, drain abort classification, the `ForceRotateRequest`
state machine, encoding-thread request capture, queue drain-to-rotate ordering,
commit/rotation execution, result completion, failure logging, draining-gate
cleanup, cancellation handling, fatal cleanup, final segment registration,
bounded video/GPU/audio/microphone packet drains, frame-size defense,
queue-depth accounting, encoder submission, GPU texture release, pooled buffer
returns, encoder PTS resolution, latest-PTS and disk-byte refresh,
frame-encoded event dispatch, segment-rotation triggering, active-segment
completion/registration, and rotation-failure recovery there.

Flashback encoder producer entry points live with queueing in
`Sussudio/Services/Flashback/FlashbackEncoderSink.Queueing.cs`. Keep raw, lease,
and GPU video enqueue entry points, frame-size validation, video/GPU input
rejection guards, texture AddRef ownership, audio/microphone enqueue entry
points, force-rotate input rejection guards, and hot WASAPI writer adapters
there.

Flashback encoder public runtime state now lives in
`Sussudio/Services/Flashback/FlashbackEncoderSink.cs`. Keep public
frame/audio/disk counters, drop counters, rotation-failure counts, frame-encoded
events, queue-depth/capacity/max-depth projections, queue rejection summaries,
GPU queue projections, video queue latency/backpressure metrics, encoding
failure status, audio/microphone enablement, fatal-error callback registration,
encoder format summaries, HDR P010 projection, recording PTS boundary state,
active-recording projection, begin-recording availability checks, the
`IRecordingSink.StartAsync` adapter, recording begin/end validation,
eviction-pause handoff/resume, start rollback, PTS clamping, ready logging,
encoding completion task exposure, `StopAsync`, stop-drain timeout
classification, final stop result reporting, `Dispose`/`DisposeAsync`,
deferred cleanup, final dispose reset, cancellation/disposal helpers, and
best-effort encoder/buffer manager disposal there.

Flashback decoder audio output now lives with the playback packet feed in
`Sussudio/Services/Flashback/FlashbackDecoder.Playback.cs`. Keep audio packet
delivery, audio codec/resampler initialization, audio callback failure handling,
resampler output conversion, bounded audio sample/byte sizing, inline audio
interleave during video reads, and decode phase timing there. Decoded video frame
output, PTS-to-TimeSpan conversion, and best-effort frame timestamp selection now live in
`Sussudio/Services/Flashback/FlashbackDecoder.VideoSetup.cs` with software
plane copies and YUV-to-NV12/P010 conversion kernels. Keep file
  open/close and disposal lifecycle in the root decoder. Keyframe/exact seek
  control flow, pending-frame transfer, seek-cap diagnostics, seek-buffer
  flushing, seek timestamp conversion helpers, video frame receive, packet
  feeding, recoverable seek log suppression, and decode phase timing state now live in
`Sussudio/Services/Flashback/FlashbackDecoder.Playback.cs`.
Decoded frame-size calculation, video-dimension validation, D3D11/software
decoded-frame validation, input stream-count bounds, and stream-index bounds now live in
the root decoder at `Sussudio/Services/Flashback/FlashbackDecoder.cs`.
File-close native cleanup, software buffer returns, pending held-frame release,
decoder state reset, held-frame best-effort release helpers, open/disposed
state guards, and FFmpeg decoder error formatting now live in the root decoder;
  decode phase timing accumulation lives with
`Sussudio/Services/Flashback/FlashbackDecoder.Playback.cs`.
Decoded video/audio output DTOs now live in the root decoder beside the
decoder's public output surface, instead of a sub-40-line output-type fragment.
Video codec setup, D3D11 device-context initialization, get-format callback
behavior, hardware decoder context setup, D3D11VA/software fallback selection,
D3D11VA decoder selection, hardware-configuration diagnostics, frame-rate
metadata initialization, MJPEG single-thread decode policy, and software
output-buffer allocation, decoded video output, and software conversion kernels now live in
`Sussudio/Services/Flashback/FlashbackDecoder.VideoSetup.cs`.

Flashback buffer lifecycle, live accounting, segment mutation/query, purge, and
retention now live in `Sussudio/Services/Flashback/FlashbackBufferManager.cs`.
The file is intentionally above the soft line target because one buffer session
owns the lock, active segment path, completed segment index, PTS counters,
disk-byte counters, recovery-preserve state, and eviction-pause state. Keep
buffer core state, read-only live counters, saturated math, normalized path
comparison, latest-PTS reset/update, sink-cycle active segment finalization,
encoder frame-rate truth, initialization, segment-extension setup, disposal,
disposed-state guards, recovery-preserve markers, explicit purge/delete-all
cleanup, guarded purge deletion, active segment path generation, active segment
start/abandonment, completion registration, duplicate-path rejection,
same-path segment extension, segment file lookup, range selection,
start-PTS lookup, session-directory path safety, read-only segment counts,
active-path projection, active segment start PTS calculation, segment-info
projection, segment eviction selection, eviction file deletion,
disk-budget/window retention policy, eviction pause state, recording PTS range
capture, and pause-driven disk-warning state there.

Flashback startup cleanup now lives in
`Sussudio/Services/Flashback/FlashbackStartupCacheCleanup.cs`. Keep stale root
segment cleanup, stale session-directory cleanup, recovery-preserve marker
skips, temp-drive free-space probing, session-directory naming/path-safety
scanner helpers, startup cache budget calculation, session-directory stats,
preserved-session skips, oldest-session deletion, and cache-budget cleanup
telemetry there.

Flashback exporter lifecycle behavior now lives in
`Sussudio/Services/Flashback/FlashbackExporter.Lifecycle.cs`. Keep shared native
export state, constants, public `Dispose`, active export cancellation, linked
export cancellation-source helpers, shared cancelled/disposed result creation,
export-lock wait/release/disposal, native input/output cleanup, native-state
cleanup on dispose, and dispose-timeout logging there.

Flashback exporter execution scheduling and runtime export policy now live in
`Sussudio/Services/Flashback/FlashbackExporter.Execution.cs`. Keep public
`ExportAsync` null/disposed guards, segment path normalization, adaptive
throttle provider handoff, single-versus-segment export selection,
single/multi-segment task wrappers, linked cancellation source disposal,
background thread priority, segment snapshots, progress normalization/reporting,
heartbeat cadence, export writer adaptive throttling, fixed sleep/yield pacing,
and per-export throttle provider scoping there so native export cores stay
behind focused entry points.

Flashback exporter single-file export shell now lives in
`Sussudio/Services/Flashback/FlashbackExporter.Execution.cs` with the request
routing and task wrapper that call it. Keep the single `.ts` export validation,
seek/setup, single-file packet result validation, active input packet pump,
native frame reads, per-read packet unref, stream filtering, out-point clipping,
timestamp rebasing, inline remux writes, writer throttling, progress heartbeat,
final packet cleanup, packet write state, timestamp-base discovery, buffered
packet transition, EOF partial-base rescue, final output replacement, success
result shaping, and single-export lock release there.

Flashback exporter multi-segment packet-copy/remux behavior now lives in
`Sussudio/Services/Flashback/FlashbackExporter.SegmentPacketWriting.cs`. Keep segment
validation dispatch, temp-output preparation, output-template selection,
template-skip diagnostics, per-segment input open, stream-info lookup,
stream-count checks, layout-mismatch skip tracking, final output replacement,
and segment-export lock release there. Segment packet writing now lives in
`Sussudio/Services/Flashback/FlashbackExporter.SegmentPacketWriting.cs`; keep
output-template initialization, segment input sequencing, segment export
range/window projection, segment offset updates, completion progress, and
requested-segment skip validation there. The
active segment packet pump lives in
`Sussudio/Services/Flashback/FlashbackExporter.SegmentPacketWriting.cs`; keep
native frame reads, per-read packet unref, stream filtering, timestamp-base
discovery, buffered packet transition, rebased packet writes, writer throttling,
and EOF partial-base rescue/freeing there, along with the per-segment packet
write state, buffered-packet rescue/flush, and native packet write outcome
state. Segment timestamp rebasing, segment-boundary repair,
DTS monotonicity, and native packet writes live there too.
`FlashbackExporter.Lifecycle.cs` keeps shared native state, constants, and
fields.

Flashback exporter validation policy now lives in
`Sussudio/Services/Flashback/FlashbackExporter.Execution.cs` with the request
execution and output replacement paths that consume it. Keep completed-output
length validation, normalized path comparison, output path validation,
export-range validation, segment/export-range overlap classification,
multi-segment input validation, and readable-segment byte estimation there.
FFmpeg error string formatting/throwing lives in
`Sussudio/Services/Flashback/FlashbackExporter.Lifecycle.cs`, and timestamp
math/saturated arithmetic lives in
`Sussudio/Services/Flashback/FlashbackExporter.SegmentPacketWriting.cs` so
`FlashbackExporter.Lifecycle.cs` stays focused on export native state and
lifetime policy while packet timestamp policy stays with packet writing.
Packet timestamp normalization, export time-span conversion, saturated time arithmetic, segment
boundary timestamp repair, packet clone/free helpers, and buffered packet
flushes live in
`Sussudio/Services/Flashback/FlashbackExporter.SegmentPacketWriting.cs`. FFmpeg input and
output context setup, stream count validation, and output header writing live in
`Sussudio/Services/Flashback/FlashbackExporter.Lifecycle.cs`. Stream-template copying
and segment stream-layout compatibility checks live in
`Sussudio/Services/Flashback/FlashbackExporter.Lifecycle.cs`. Temp output
validation, active output trailer/IO close finalization, atomic replacement,
overwrite policy, and invalid final-output cleanup live in
`Sussudio/Services/Flashback/FlashbackExporter.Execution.cs`.
Temp output cleanup, stale temp preparation, and orphan `.mp4.tmp` cleanup live
in `Sussudio/Services/Flashback/FlashbackExporter.Execution.cs`.

D3D preview renderer metrics now live in
`Sussudio/Services/Preview/D3D11PreviewRenderer.Metrics.cs`. Keep present
cadence, pipeline latency, render CPU timing, frame-latency wait metric state,
sample tracking, expected-frame-rate window resizing, metric reset/clear
lifecycle, read-only projections, and recent sample copies there. Shared
ring-copy, timing-summary, tick-to-ms, render-stage validation helpers, and
metric record structs live there too. Renderer implementation fields should
live with the partial that mutates or projects them: keep
renderer diagnostic ring/write state, render-thread failure state, first-frame
notification state, and slow-frame reason classification in
`D3D11PreviewRenderer.Metrics.cs`; startup/disposal lifecycle state,
stop/unbind/native-call fence state, render-loop shell orchestration,
shared-device reset/rebind consumption, composition-transform wake handling,
pending-frame render dispatch, final render-thread drain, and renderer-mode
reset in the renderer root facade,
queue state and signaling in the renderer root facade, D3D device/swap-chain, input texture, HDR shader input resources, shader resource/cache state, and shader compilation in
`D3D11PreviewRenderer.Resources.cs`, swap-chain panel binding state in
`D3D11PreviewRenderer.cs`, render-thread waitable frame-latency
pacing in `D3D11PreviewRenderer.cs`, render-pass selection plus
VideoProcessor, NV12 shader, and HDR shader execution plus shared present
accounting in `D3D11PreviewRenderer.RenderPasses.cs`. Keep the
renderer root limited to facade/lifecycle/render-loop orchestration, panel
binding, composition transforms, frame-latency pacing, user-facing accessors,
public frame submission, queue signaling, and public state toggles; leave resource, render-pass, and
metrics implementation state in their focused owners.

D3D preview renderer queue-owned frame lifetime and metrics model types now live
with their caller owners. Keep the `PendingFrame` lifetime wrapper in
`Sussudio/Services/Preview/D3D11PreviewRenderer.cs` beside
queue admission/drain/drop behavior and render-loop consumption; keep renderer metric record structs and
shared metric sample helpers in
`Sussudio/Services/Preview/D3D11PreviewRenderer.Metrics.cs` beside metric
state, mutation, reset, and read-only projections.

D3D preview renderer runtime knobs live in
`Sussudio/Services/Preview/D3D11PreviewRenderer.cs` with the renderer facade.
Keep the measured 4K120 cadence defaults, swap-chain queue/latency env
overrides, DXGI statistics toggles, MMCSS settings, and stop-fence timeouts there. Native
interop declarations now live with their behavior owners: keep
`ISwapChainPanelNative` in `D3D11PreviewRenderer.cs`,
`ID3DBlob` and `D3DCompileNative` in `D3D11PreviewRenderer.Resources.cs`,
and `DwmFlush` in `D3D11PreviewRenderer.Metrics.cs`; leave `WaitForSingleObject` with render-thread frame pacing in
`D3D11PreviewRenderer.cs`.

D3D preview renderer shader compilation now lives in
`Sussudio/Services/Preview/D3D11PreviewRenderer.Resources.cs`. Keep
shader bytecode creation, sampler creation, viewport constant-buffer creation,
and compile-fallback logging with shader resource/cache state; keep
`D3DCompileNative` invocation, `ID3DBlob` byte extraction, and compile-error
string extraction, HLSL text, and renderer mode labels with D3D resources.

D3D preview renderer frame submission now lives in
`Sussudio/Services/Preview/D3D11PreviewRenderer.cs`. Keep public raw
frame, lease, single shared-texture, and dual-plane NV12 submission entry
points there, including NV12 shader guard, HDR transition logging, COM
AddRef/release, pending-frame adapters, pending-frame queue state, signaling,
and explicit pending-frame drain control beside render-thread start/disposal,
panel sizing, stop, and reinit-stop lifecycle.

D3D preview renderer public lifecycle now lives in the root
`Sussudio/Services/Preview/D3D11PreviewRenderer.cs` facade. Keep render-thread
startup state, startup reset, renderer disposal, public stop/reinit stop,
unbind-before-join ordering, native-call drain fencing, pending-frame shutdown
cleanup, and render-pass native-call entry/exit guard helpers there.

D3D preview renderer render-thread orchestration now lives in the root
`Sussudio/Services/Preview/D3D11PreviewRenderer.cs` facade. Keep MMCSS
registration, frame-ready wait-loop ordering, shared-device reset
consumption/rebind, composition-transform wake handling, pending-frame
consumption, stale-generation drops, frame-latency wait, render dispatch,
device-lost handoff, failure notification handoff, final pending-frame
drain/frame-capture failure, and renderer-mode reset there; keep render-thread
failure telemetry and first-frame notification reset/UI enqueue in
`D3D11PreviewRenderer.Metrics.cs`, render-pass selection,
VideoProcessor execution, shader draw execution, and shared present accounting in
`D3D11PreviewRenderer.RenderPasses.cs`, and shader resource/cache state in
`D3D11PreviewRenderer.Resources.cs`.

D3D preview renderer render-pass selection now lives in
`Sussudio/Services/Preview/D3D11PreviewRenderer.RenderPasses.cs`. Keep
VideoProcessor execution, HDR fallback logging, timing bucket attribution, pass
precedence, native-call guard consumption, NV12/HDR shader-resource binding,
draw calls, passthrough/tonemap mode selection, shader-mode present messages,
VideoProcessor input view resolution, external texture input-view creation, raw
frame byte/lease upload, direct texture update fallback, and staging copy
mechanics there. Shader resource/cache state now lives in
`Sussudio/Services/Preview/D3D11PreviewRenderer.Resources.cs`. Keep shader
fields, reusable shader class-instance arrays, and NV12 SRV caching there; keep
render-thread orchestration in `D3D11PreviewRenderer.cs`, and keep
present accounting and slow-frame diagnostic call sites with render-pass
completion in `D3D11PreviewRenderer.RenderPasses.cs`.

D3D preview renderer diagnostics now live in
`Sussudio/Services/Preview/D3D11PreviewRenderer.Metrics.cs`. Keep
recent slow-frame snapshot access, diagnostic thresholding, the slow-frame
ring buffer writer, slow-frame reason token classification, render-thread
failure telemetry, first-frame UI notification, and DXGI refresh-slip capture
there; keep cadence and CPU timing windows in
`D3D11PreviewRenderer.Metrics.cs`.

D3D preview renderer viewport and letterbox helpers now live with render-pass
execution in `Sussudio/Services/Preview/D3D11PreviewRenderer.RenderPasses.cs`.
Keep `ComputeLetterboxViewport`, `UpdateViewportConstantBuffer`, and
`ComputeLetterboxRect` there with shader draw path ordering; keep D3D resource
creation in `D3D11PreviewRenderer.Resources.cs`.

D3D preview renderer submitted/rendered/dropped frame ownership tracking now
lives in `Sussudio/Services/Preview/D3D11PreviewRenderer.Metrics.cs`. Keep
frame ownership snapshot projection and submitted/presented/dropped ownership
state updates with cadence, latency, frame-latency timing, DXGI statistics, and
slow-frame diagnostic projection there.

D3D preview renderer DXGI frame statistics now live in
`Sussudio/Services/Preview/D3D11PreviewRenderer.Metrics.cs`.
Keep `GetFrameStatistics`, optional `DwmFlush`, DXGI counter deltas, and missed
refresh accounting there. Display-clock projection also lives there; keep
visible-frame tick estimation and `IPreviewDisplayClock` snapshot construction
with the DXGI statistics state it samples. Keep pending-frame lifetime, queue control, and `IPreviewFrameQueueControl` in
`Sussudio/Services/Preview/D3D11PreviewRenderer.cs`. Keep
slow-frame diagnostic consumption of the latest DXGI counters in the same metrics
owner.

D3D preview renderer frame-latency waitable swap-chain setup and waits now live
in `Sussudio/Services/Preview/D3D11PreviewRenderer.cs`. Keep
`ConfigureFrameLatencyWaitableObject`, `WaitForFrameLatencySignal`, the native
`WaitForSingleObject` import, and wait-result constants with render-thread
pacing so frame dequeue, wait, render dispatch, and wait metrics are reviewed
together.

D3D preview renderer device initialization and resource management now live in
`Sussudio/Services/Preview/D3D11PreviewRenderer.Resources.cs`. Keep
`InitializeD3D` orchestration, shared-vs-owned device setup, shared-device COM
reference duplication/release policy, reinit retirement, reset request
scheduling, `TryInitializeWithSharedDevice`, device-loss classification,
device-lost frame drops, stop-guarded cleanup, reinitialize scheduling, video
interface acquisition, media present duration setup, initial panel binding,
renderer-owned device fallback, composition swap-chain creation, startup
dimensions, HDR swap-chain capability probing, SDR swap-chain fallback, initial
color-space selection, configured output size publication, top-level D3D
resource cleanup orchestration, NV12/P010 VideoProcessor input textures,
staging textures, input views, HDR P010 shader input/staging textures, plane
SRV creation, Device3 fallback, input texture cleanup, video-processor creation
orchestration, processor-resource teardown, swap-chain RTV/output view reuse,
and VideoProcessor input/output color-space updates there.
Shader/SRV teardown stays with
`D3D11PreviewRenderer.Resources.cs`, and preview-frame capture staging
teardown stays with `D3D11PreviewRenderer.RenderPasses.cs`; keep
swap-chain color-space application with render-pass selection in
`D3D11PreviewRenderer.RenderPasses.cs`.
Keep render loop consumption in `D3D11PreviewRenderer.cs`,
present paths with render-pass completion and shader draw paths in
`D3D11PreviewRenderer.RenderPasses.cs`.

D3D preview renderer swap-chain panel binding now lives in
`Sussudio/Services/Preview/D3D11PreviewRenderer.cs`. Keep
UI-thread `SetSwapChain` bind/unbind marshaling and composition scale
transforms there; keep device and view allocation in
`D3D11PreviewRenderer.Resources.cs`.

D3D preview pending-frame queue ownership now lives in
`Sussudio/Services/Preview/D3D11PreviewRenderer.cs`. Keep
enqueue, backlog trimming, frame-ready signal/reset wrappers, explicit pending
drains, pending-count accounting, and render-loop consumption there; keep frame ownership metrics in
`D3D11PreviewRenderer.Metrics.cs`.

Media Foundation source-reader negotiation now lives in
`Sussudio/Services/Capture/MfSourceReaderVideoCapture.Negotiation.cs`. Keep
DXGI manager attachment, direct device-source open, native media-type selection,
frame-size/frame-rate attribute reads, and converted output media-type
construction, device-enumeration open fallback, and candidate reporting there.
Keep the Source Reader transform boundary beside native media-type selection so
the selected native type and requested NV12/P010 output type can be reviewed
together without changing negotiation semantics. Keep high-level source-reader
state fields in the root source-reader file.

Media Foundation source-reader initialization orchestration and active lifetime
now live together in
`Sussudio/Services/Capture/MfSourceReaderVideoCapture.cs`. Keep
public initialization validation, startup-reference acquisition/release, reader
attribute construction, source media-type selection, actual-output
reconciliation, strict negotiated-output validation, runtime field reset,
COM/startup ownership handoff, initialization success/failure logging,
start/stop/dispose, read thread priority, `ReadSample` outstanding-state
tracking, sample timestamp cadence handoff, frame-delivery invocation,
frame-drop accounting, and fatal D3D output failure break behavior there.

Media Foundation source-reader frame delivery now lives in
`Sussudio/Services/Capture/MfSourceReaderVideoCapture.FrameDelivery.cs`. Keep
sample-to-buffer conversion, compressed MJPG routing and byte extraction, raw
CPU frame delivery, 2D buffer handling, packed-stride CPU copies, dual GPU/CPU
delivery orchestration, dual-frame CPU payload extraction, readback fallback
selection, and GPU texture release there.

Media Foundation interop declarations and shared helpers now live in
`Sussudio/Services/Capture/MfInterop.cs`. Keep general Media Foundation COM
interfaces, flattened `IMFSample` vtable layout, MF buffer interfaces,
MFStartup/MFShutdown ref-counting, typed `IMFAttributes` accessors, symbolic-link
matching, MF P/Invoke declarations, constants/HRESULTs, and GUIDs together
there. Preserve interface method order and placeholder slots exactly; keep
behavioral source-reader logic in the root and negotiation partials.

Media Foundation source cadence metrics now live with active source-reader
lifetime in `Sussudio/Services/Capture/MfSourceReaderVideoCapture.cs`.
Keep the public cadence snapshot record, expected-rate/window sizing, stop-time
cadence reset, timestamp interval tracking, and percentile/drop estimate
calculations near the read loop that observes Media Foundation timestamps; keep
sample-to-buffer delivery in the named frame/raw delivery partials.

Media Foundation source-reader diagnostics now live with frame delivery in
`Sussudio/Services/Capture/MfSourceReaderVideoCapture.FrameDelivery.cs`. Keep
the debug-only COM vtable diagnostic there beside the sample dispatch that
invokes it.

Media Foundation source-reader frame delivery now keeps IMFDXGIBuffer
texture/subresource extraction, D3D texture IID lookup, DXGI fallback
diagnostics, and dual GPU/CPU delivery orchestration in
`MfSourceReaderVideoCapture.FrameDelivery.cs`; keep raw/compressed CPU frame
delivery helpers in the same file with sample-to-buffer dispatch and reader
start/stop/dispose in the root source-reader file.

Media Foundation packed-frame layout helpers now live with the source-reader
state in `Sussudio/Services/Capture/MfSourceReaderVideoCapture.cs`. Keep
frame-size/row-byte calculation, packed-stride inference, stride-aware YUV
copying, and source subtype labels there; keep frame delivery in
`MfSourceReaderVideoCapture.FrameDelivery.cs` and reader start/stop/dispose in
the root source-reader file.

Media Foundation source-reader lifecycle now lives in
`Sussudio/Services/Capture/MfSourceReaderVideoCapture.cs`. Keep
public start/stop/dispose, reader/source COM release, lifecycle logging, and
fatal-error callback dispatch there; keep initialization and frame delivery in
their named source-reader partials.

Unified capture source-session lifecycle, frame ingress, sink fan-out, and
diagnostic metric projection now live in
`Sussudio/Services/Capture/UnifiedVideoCapture.cs`. The file is intentionally
larger than the usual review target because one live source-reader session owns
the capture fields, public control/configuration methods, source-reader/D3D/MJPEG
initialization, committed runtime state reset, read-loop start/stop,
preview-reinit disposal, CPU MJPEG pipeline construction and stop retention,
preview jitter buffer setup/teardown, capture/MJPEG fatal-error callbacks,
source-frame arrival callbacks, MJPEG pipeline frame emission,
capture-arrival ledger records, pixel-format observer dispatch, fatal-error
signaling, preview sink assignment, live-preview suppression/resume drains,
MJPEG preview-frame decoded callbacks, raw preview submission,
visual-cadence reset/recording helpers, recording enqueue helpers, Flashback
enqueue helpers, queue rejection accounting, legacy encoder fallback enqueue
adapters, Flashback recording sequence-gap accounting, the `FrameLedger`
ring-buffer helper, source-reader cadence forwarding, MJPEG timing records,
MJPEG jitter/hash metrics, preview visual cadence metrics, and frame-ledger
summary projection.

Capture cadence tracker ingestion now lives in
`Sussudio/Services/Capture/CaptureCadenceTrackers.cs`. Keep
`VisualCadenceTracker` state, reset, frame validation, output/change ingestion,
repeat-run bookkeeping, luma sampling, crop selection, one-pass
current-vs-previous comparison, sample-buffer promotion, rolling sample writes,
elapsed-time conversion, metrics DTO construction, ring-buffer snapshot copying,
delta/output/change statistics, and motion-confidence labels there. Keep
`FrameFingerprintCadenceTracker` frame recording, duplicate-run counters, fast
packet hashing, metrics DTO construction, ring-buffer snapshot copying,
unique-interval projection, duplicate-percent statistics, and pattern labels in
the same cadence tracker owner.

MJPEG preview jitter-buffer metrics, decoded-frame queue ingress, and frame pacing now live in
`Sussudio/Services/Capture/MjpegPreviewJitterBuffer.cs` with the root lifecycle
state they sample and mutate. Keep metrics records, snapshot construction,
timing samples, selected/dropped-frame telemetry, tick/millisecond conversion
helpers, the nested buffered payload type, ArrayPool/lease ownership transfer,
input-interval recording, queue-full admission drops, enqueue signaling, queue
depth, ordered frame insertion/dequeue, missing-sequence recovery, clear
behavior, and resume reprime accounting there beside construction, paced emit
loop control flow, MMCSS registration, thread lifecycle, suppression/reprime
lifecycle, dispose-time queue teardown, display-clock alignment, frame
submission to the preview sink, tick waits, hard/soft deadline drops, adjusted
output cadence, target-depth increase/decrease, latency-pressure
classification, and timer-resolution P/Invoke.

Parallel MJPEG compressed input admission now lives with the bounded work-channel
owner in `Sussudio/Services/Gpu/ParallelMjpegDecodePipeline.cs`. Keep startup
invalid-MJPG drops, work-item construction, compressed byte-budget rejection,
queue-depth accounting, queue-full rejection, and packet-hash recording beside
pipeline construction and channel creation.

Parallel MJPEG worker execution now lives with the bounded work-channel owner in
`Sussudio/Services/Gpu/ParallelMjpegDecodePipeline.cs`. Keep decoder array
ownership, worker thread creation/naming, worker decode-loop execution, worker
liveness checks, construction, callback storage, channel creation, compressed
input admission, and startup sequencing together.
Software MJPEG decode/copy execution now lives with its decoder state in
`Sussudio/Services/Gpu/SoftwareMjpegDecoder.cs`. Keep FFmpeg decoder context
allocation, frame/packet ownership, hot MJPEG send/receive,
format/dimension validation, one-time diagnostics, YUV420-to-NV12 copy,
disposal, and error-string helpers together unless a real named collaborator
emerges.

Parallel MJPEG decode pipeline timing now lives in
`Sussudio/Services/Gpu/ParallelMjpegDecodePipeline.cs` with the bounded
work-channel and worker state it samples. Keep timing record structs, timing
snapshot construction, per-decoder sample windows, packet-hash metric access,
and stopwatch conversion helpers there beside worker decode ingress.

Parallel MJPEG decode pipeline decoded-frame ordering now lives in
`Sussudio/Services/Gpu/ParallelMjpegDecodePipeline.cs` with the rest of the
bounded work-channel owner. Keep strict missing-sequence waits,
known-missing skips, decoded reorder state, decoded reorder capacity policy,
emit-loop ordered draining, preview decoded-frame notification, and
reorder/pipeline latency samples there beside construction, queue admission,
worker execution, lifecycle, and metrics.

Parallel MJPEG decode pipeline lifecycle lives in
`Sussudio/Services/Gpu/ParallelMjpegDecodePipeline.cs` with construction,
worker startup, and worker state. Keep stop/dispose, emitter signaling, shutdown
joins, fatal-callback dispatch, remaining-timeout helpers, decoder disposal,
queued work-item return, remaining reorder-frame disposal, and emit-signal
disposal with the root pipeline owner.

CUDA/D3D11 preview interop ownership lives in
`Sussudio/Services/Gpu/CudaD3D11InteropBridge.cs`: bridge state, public texture
handles, constructor setup, zero-copy registration, disposal, CUDA resource
unregistration, native CUDA constants, P/Invoke entry points, the
`CUDA_MEMCPY2D` native struct, and the zero-copy/staging frame-copy paths.
Keep D3D11 locking, primary-context ownership, and fallback-to-staging behavior
unchanged.

NVDEC MJPEG decoder ownership now lives in
`Sussudio/Services/Gpu/NvdecMjpegDecoder.cs`: shared decoder state, standalone
CUDA device and hardware-frame pool initialization, caller-provided CUDA
device/frame context adoption, packet decode, CUDA context access, CPU
download/packed-buffer copies, disposal, and FFmpeg error text. Keep
shared-context ownership, hot-path decode/download behavior, and disposal order
unchanged when touching this file.

NVML telemetry ownership now lives in `Sussudio/Services/Gpu/NvmlMonitor.cs`,
which owns optional diagnostic polling, snapshot publication, timer/lifetime
behavior, graceful unavailable handling, raw NVML constants, structs, library
loading, device-name lookup, and P/Invoke declarations. Keep the monitor
diagnostic-only and do not let missing NVML support affect capture, preview, or
recording startup. NVML snapshot/unit conversion and monitor interop ownership
checks now live with the tool-model contract group in
`tests/Sussudio.Tests/AutomationToolContracts.Tests.cs` and
`tests/Sussudio.Tests/XUnit.ToolContractsTests.cs`.

Automation snapshot contracts now live in named model files under
`Sussudio/Models/Automation/`. The broad automation evidence DTO is split as an
`AutomationSnapshot*.cs` partial family by domain: root lifecycle/diagnostics
with source telemetry, user settings, HDR, audio/ingest, recording, capture
format, preview, MJPEG/cadence, system health, and Flashback. Other snapshot contracts
now remain in `AutomationRuntimeModels.cs` and `AutomationSupportModels.cs`;
the runtime file owns the capture runtime DTO surface, preview runtime DTO
surface, and performance timeline DTO surface. The support file owns command
protocol DTOs and converters plus the small automation evidence DTOs for
diagnostics events, Flashback segments, preview startup, screenshot/window
capture, recording verification, video source/color probes, and view-model
runtime snapshots. The preview runtime DTO section owns surface/frame health,
startup, display cadence, D3D renderer diagnostics, and GPU playback fields;
the timeline DTO section owns capture/preview cadence, preview/MJPEG/D3D,
Flashback playback, Flashback export, and process/system health fields. Keep
runtime evidence DTOs in `AutomationRuntimeModels.cs`
unless a future model grows behavior or external linked-source constraints.

Native XU AT-command transport and payload parsing now live in
`Sussudio/Services/Telemetry/NativeXuAtCommandProvider.cs`. Keep raw
AT read/write frames, LRC/envelope handling, selector-4 I2C payload writes,
device-ID parsing, payload decoders, scalar helpers, and command failure
formatting there with rolling telemetry polling and the active read path, and
keep shared source snapshot assembly in
`NativeXuAtCommandProvider.SnapshotAssembly.cs`.

Runtime capture snapshot projection now lives in
`Sussudio/Services/Capture/CaptureService.RuntimeSnapshots.cs` now samples the
read-only runtime inputs consumed by UI, automation, and verification, and owns
final `CaptureRuntimeSnapshot` DTO construction.
Runtime snapshot behavior and source projection ownership coverage live together
in `tests/Sussudio.Tests/CaptureService.RuntimeSnapshots.ProjectionOwnership.Tests.cs`.
The private runtime snapshot assembly handoff contract lives with the runtime
snapshot sampler that consumes it.
The automation runtime model surface lives in `AutomationRuntimeModels.cs`, with
sectioned runtime fields grouped by the same domain as the sampled field groups.
Video ingest, source-reader health, WASAPI capture, playback output counter,
requested/negotiated reader transport, memory preference, frame-ledger, preview
renderer-mode projection, recording-integrity summary projection, HDR pipeline
parity/downgrade, warmup state/count projection, source telemetry
detail/frame-rate-origin/age/alignment projection, the `HdrOutputPolicy`
environment gate used by capture setup and preview readiness checks, and their
private handoff models now live with the runtime snapshot sampler in
`Sussudio/Services/Capture/CaptureService.RuntimeSnapshots.cs`.
Recording-format and observed-frame helper policy live in focused snapshot
partials.

Capture health snapshot sampling now lives in
`Sussudio/Services/Capture/CaptureService.HealthSnapshots.cs`. That file
captures current service references, owns the private field builders, and
populates the final service-state/scalar handoff used by the health DTO map.
Source-cadence metric projection, MJPEG timing, preview jitter, visual cadence,
packet hash, per-decoder projection, and their health field records live with
the read-only sampler in
`Sussudio/Services/Capture/CaptureService.HealthSnapshots.cs`; pure
diagnostics/automation DTO construction and the private assembler field handoff
contract live there too. The assembler remains intentionally allocation-neutral
final DTO construction from captured fields; do not split it into
post-construction mutators or shallow fragment records just to reduce line
count.
source telemetry, backend, suppression, and circuit-state projection lives with
the health snapshot sampler in
`Sussudio/Services/Capture/CaptureService.HealthSnapshots.cs`;
Flashback buffer, startup-cache, backend-staleness reason policy, encoder
summary, live Flashback audio/video queue, force-rotate, backpressure, and GPU
queue projection lives in
`Sussudio/Services/Capture/CaptureService.HealthSnapshots.cs`;
recording health orchestration and LibAv-only CUDA queue projection live in
`Sussudio/Services/Capture/CaptureService.HealthSnapshots.cs`, along
with active recording backend selection, LibAv-vs-Flashback fallback, and
backend-specific queue/counter normalization;
Flashback export diagnostics and derived health progress/throughput projection
lives in
`Sussudio/Services/Capture/CaptureService.FlashbackExportCore.cs`.
Flashback playback health snapshot orchestration now lives in
`Sussudio/Services/Capture/CaptureService.HealthSnapshots.cs`
with the aggregate playback field record, state/frame/segment/PTS/seek-cap/
submit-failure/A/V drift sampling, playback cadence metric sampling, decode
timing and max-phase metric sampling, audio-master pacing/fallback sampling,
playback command telemetry sampling, and each matching private field record.
The general snapshot partial is now the diagnostics-snapshot compatibility
entry point plus shared tick-age snapshot helper policy. Flashback
backend-staleness reason policy now stays with the health snapshot sampler, while
export elapsed/progress-age/file-length helpers stay with the export
diagnostics partial.

Recording byte-count, recording-format, observed frame-format, source
telemetry, and A/V sync snapshot policy now live in
`Sussudio/Services/Capture/CaptureService.Snapshots.cs`. Keep these helpers
read-only: no transition-lock waits or state mutation. Active LibAv byte
polling, active Flashback buffer estimates, finalized-output file fallback,
failure flagging, encoder codec/output/profile labels, requested frame-rate
argument projection, explicit observed-frame `Interlocked.Read` counters,
source telemetry backend/suppression/circuit-state mapping, A/V sync drift
baseline state, and encoder drift/correction projection stay together as the
diagnostics/runtime snapshot helper surface. Runtime frame-rate origin labels,
request/telemetry alignment, HDR warmup state classification, and HDR pipeline
parity projection live with
`Sussudio/Services/Capture/CaptureService.RuntimeSnapshots.cs`.

Stats dock, stats toggle, frame-time overlay lifecycle, and stats overlay
controller composition now live in
`Sussudio/Controllers/Stats/StatsOverlayCompositionController.cs`. It owns the
runtime facade, construction-order entry point, snapshot provider, frame-time
presentation, dock graph, overlay controller, section chrome factory wiring,
stats/frame-time toggle event hookup, visibility sync, polling lifetime, dock
show/hide storyboard mechanics, and grouped composition context DTOs for shell
controls, snapshot sources, dock targets, hardware sources, and frame-time
targets;
stats dock presentation/diagnostic/hardware/refresh controller graph wiring
and the dock graph context contract now live in
`Sussudio/Controllers/Stats/StatsOverlayCompositionController.cs`;
`Sussudio/MainWindow.Composition.cs` owns the XAML-facing stats
overlay adapter surface: binding setup, stats dock visibility, refresh hooks,
snapshot inputs, frame-time targets, section commands, and polling commands.
Stats toggle event hookup and checked/unchecked behavior, initial/property-changed
visibility sync, polling, visibility state, dock refresh ordering, dynamic
diagnostic row pools, dock metric value/brush application, and dock animations
are out of the event adapter. Stats dock show/hide animation mechanics now live
in `Sussudio/Controllers/Stats/StatsOverlayCompositionController.cs` with the
polling/visibility orchestration they mutate.
Stats dock refresh orchestration now lives in
`Sussudio/Controllers/Stats/StatsDockRefreshController.cs`: snapshot acquisition,
dock presentation build/apply, diagnostics visibility gating, and decode/GPU
row refresh ordering. Stats dock metric value, visibility, and status brush
application also live there.
Stats section expand/collapse chrome and automation-visible section application
now live in the local section chrome controller inside
`Sussudio/Controllers/Stats/StatsOverlayCompositionController.cs`.
`Sussudio/MainWindow.Composition.cs` owns the XAML/automation
adapter for the stats shell wiring and delegates controller/provider composition to
`Sussudio/Controllers/Stats/StatsOverlayCompositionController.cs`.
Detached stats-window metric text now lives in
`Sussudio/StatsWindow.xaml.cs`, along with dynamic telemetry-detail clearing,
empty state, group headers, row rendering, lifecycle, sizing, polling,
controller composition, and always-on-top behavior.
Stats overlay lifecycle, stats dock refresh, stats section chrome, and
diagnostic row pooling contract checks now live in two focused owners:
`tests/Sussudio.Tests/MainWindow.ControllerOwnership.Tests.cs` covers overlay
lifecycle and section chrome through the MainWindow controller ownership
surface, while
`tests/Sussudio.Tests/XUnit.StatsPresentation.Formatting.Tests.cs` covers dock
presentation application, diagnostic rows, hardware rows, row chrome pooling,
stats presentation ownership, source telemetry panel projection checks, and
executable stats presentation formatting behavior.
Frame-time overlay compact text application and graph-line mutation now live in
`Sussudio/Controllers/Stats/StatsOverlayCompositionController.cs`, along with
frame-time canvas sizing, sample projection, and expected-line geometry;
`Sussudio/MainWindow.Composition.cs` owns the XAML-facing compact
overlay adapter beside the stats overlay visibility route, while grouped stats
composition context contracts and presentation-controller graph composition live
in
`Sussudio/Controllers/Stats/StatsOverlayCompositionController.cs`.
`Sussudio/Controllers/Stats/StatsDockRefreshController.cs` keeps the stats dock
projection refresh adapter.
Decode and GPU hardware stats row refresh/application over presentation inputs
now lives in `Sussudio/Controllers/Stats/StatsDockRefreshController.cs`;
live MJPEG/NVML sampling and decode availability policy live in the local
hardware input provider in that refresh-controller file, alongside pure
MJPEG/NVML telemetry-to-presentation-input projection; pure row text
projection over presentation inputs lives in
`Sussudio/ViewModels/StatsPresentationBuilder.cs`;
decode/GPU row element pooling, shared row chrome, diagnostics empty-state
chrome, group-header chrome, diagnostic row pooling, and diagnostic row style
application also live in `Sussudio/Controllers/Stats/StatsDockRefreshController.cs`;
`StatsDockRefreshController` owns when decode/GPU rows refresh.
Stats presentation contract checks now live in
`tests/Sussudio.Tests/XUnit.StatsPresentation.Formatting.Tests.cs` for builder
ownership, source telemetry, stats dock, row chrome, frame-time overlay policy,
detached-window, encoder, expected-display-repeat, compact preview summary,
frame-time range, frame-time graph geometry behavior, hardware decode/GPU row,
and input-provider behavior instead of expanding the legacy harness body in
`tests/Sussudio.Tests/HarnessCore.cs`.
Stats presentation text projection is now consolidated in
`Sussudio/ViewModels/StatsPresentationBuilder.cs`: diagnostic row construction,
source-summary parsing, frame-lane diagnostic health summary classification,
frame-time overlay presentation/range/sample text policy, visual-cadence
FPS/repeat/motion text formatting, expected visual-repeat drift helpers, encoder
dock visibility, codec label, bitrate, encoder drift text formatting, detached
stats-window text, telemetry-detail presentation, stats dock summary
construction, HDMI/capture/preview resolution text, shared formatting helpers,
stats lane status classification, and the visual-repeat drift result all live in
one pure builder instead of ten partial fragments.
Stats presentation DTO records/enums now live with
`Sussudio/ViewModels/StatsPresentationBuilder.cs`.
The UI stats snapshot contract and projection from capture health, renderer
metrics, and shell view state live in `Sussudio/ViewModels/StatsSnapshot.cs`;
shell snapshot orchestration plus renderer cadence/recent-sample acquisition
lives in `Sussudio/Controllers/Stats/StatsOverlayCompositionController.cs`;
`Sussudio/MainWindow.Composition.cs` is the XAML-facing provider
composition adapter.
Pure capture option construction lives in
`Sussudio/ViewModels/ViewModelSelectionPolicies.cs`.

Dynamic stats dock row chrome now lives in
`Sussudio/Controllers/Stats/StatsDockRefreshController.cs`. It owns decode/GPU
row reuse, shared stats dock row creation, text mutation, visibility toggles,
row style application, diagnostic row presentation, telemetry diagnostics empty
state, group headers, diagnostic row pooling, live MJPEG/NVML input acquisition
through its local hardware input provider, hardware row availability, text-row
presentation building, and minimum pool sizing.

Flashback timeline visibility, lockout, toggle synchronization, track layout
sizing, show/hide storyboard state, immediate collapse, and fullscreen animation
reset now live in
`Sussudio/Controllers/Flashback/FlashbackUiControllers.cs`.
`Sussudio/MainWindow.xaml.cs` owns the consolidated
XAML-facing adapter surface for commands, polling, playhead motion, scrub
input, settings, timeline visibility, and presentation. Command semantics also live
with the Flashback UI controllers in
`Sussudio/Controllers/Flashback/FlashbackUiControllers.cs`.

Active Flashback pointer-scrub state now lives in
`Sussudio/Controllers/Flashback/FlashbackUiControllers.cs`. It owns scrub
throttling, release/cancel/capture-lost cleanup, fullscreen scrub termination,
lockout clearing, scrub visual updates, and pointer lifecycle around scrub
commands. `Sussudio/MainWindow.xaml.cs` is the XAML-facing
scrub adapter.
Timeline fraction/duration math used by scrub and playhead presentation now lives
beside scrub interaction in
`Sussudio/Controllers/Flashback/FlashbackUiControllers.cs`.

Flashback CTI/playhead compositor motion now lives in
`Sussudio/Controllers/Flashback/FlashbackUiControllers.cs`. The
controller owns context, public entry points, shared state, playback-state
sampling, scrub/window gating, live right-edge pinning, long-horizon
extrapolation scheduling, CTI anchor timing, compositor visual setup, snap
placement, magnetic scrub movement, linear keyframe animation, and label
clamp/positioning.
`Sussudio/MainWindow.xaml.cs` is the XAML-facing playhead
adapter; command handling and toggle/apply workflows now live in the command
controller.

Flashback marker placement and compact duration text now live in
`Sussudio/Controllers/Flashback/FlashbackUiControllers.cs`, including
in/out marker visibility, selection-region layout, and `m:ss` formatting.
`Sussudio/MainWindow.xaml.cs`
wires marker presentation callbacks.

Flashback playback UI sequencing now lives in
`Sussudio/Controllers/Flashback/FlashbackUiControllers.cs`: track-resize
snap/position/marker/CTI refresh order, playback state polling start/stop,
play/pause glyph policy, Go Live enabled state, buffer-duration text, floating
playhead label text, buffer-fill/position/marker refresh order, and
position-label updates with CTI re-anchor gating.

Flashback export progress presentation now lives in
`Sussudio/Controllers/Flashback/FlashbackUiControllers.cs`:
progress-bar value, visibility, and reset-on-complete semantics.
`Sussudio/MainWindow.xaml.cs` wires the export progress
presentation controller.

Flashback command semantics now live in
`Sussudio/Controllers/Flashback/FlashbackUiControllers.cs`: in/out point commands,
clear, play/pause, Go Live, fullscreen keyboard shortcuts including left/right
nudge rejection logging, export, save-last-5m, enable-toggle rollback, and
apply/restart. `Sussudio/MainWindow.xaml.cs` preserves the
existing XAML event-handler names for command buttons and toggles.

Flashback settings bindings now live in
`Sussudio/Controllers/Flashback/FlashbackUiControllers.cs`: initial settings
projection, GPU decode toggle binding and reverse-sync, buffer duration combo
selection, and `FLASHBACK_UI_BUFFER_DURATION_CHANGED` logging. The async
Flashback enable/disable rollback path and apply/restart command also live in
`FlashbackUiControllers.cs`; `Sussudio/MainWindow.xaml.cs`
is the settings XAML-facing adapter.

Flashback playback marker commands, in/out marker state, file-PTS restore,
marker normalization, invalid-range clearing, and out-point pause checks now
live in
`Sussudio/Services/Flashback/FlashbackPlaybackController.Positioning.cs`; keep
decode pacing and command execution flow in the playback-frame and thread-command
partials.

Flashback playback position/file-PTS mapping now lives in
`Sussudio/Services/Flashback/FlashbackPlaybackController.Positioning.cs`.
It owns scrub/seek clamping, marker-bound range limits, saturating timestamp
math, active fMP4 segment detection, and playback path comparison.

Playback lifecycle and metrics now live in
`Sussudio/Services/Flashback/FlashbackPlaybackController.cs`: component
lifecycle, dispose, preview detach/deferred reattach recovery, cadence summary
DTOs, decode summary DTOs, percentile math, low-FPS derivation, private metric
counters, read-only projections, cadence/decode sample rings, metric reset
behavior, seek-cap telemetry, decode timing wrappers, max decode phase state,
and dominant decode phase resolution.

Flashback playback public command entry points now live with root lifecycle and
metrics ownership in
`Sussudio/Services/Flashback/FlashbackPlaybackController.cs`. Keep scrub, seek,
play/pause, go-live, and nudge request gating there with raw queue writes/drop
policy, command identity and payload shape, seek/scrub coalesced command
admission, queued-position resolution, queued-slot barriers, and playback-thread
control-yield peek policy.
Keep playback-thread execution in the thread command owner.
Public read-only command counters, command queue latency/timestamps, last
command failure projection, playback-thread liveness, command readiness guards,
skipped-not-ready accounting, command status counters, pending-command
accounting, active-command timing, queue telemetry bookkeeping,
failure-detail formatting, last-command failure state, and no-op logging now live in
`Sussudio/Services/Flashback/FlashbackPlaybackController.cs`.
Playback thread lifecycle, command dispatch, and active-command completion
telemetry now live in
`Sussudio/Services/Flashback/FlashbackPlaybackController.ThreadCommands.cs`;
keep state fields, stop timeout policy, start/recovery, start-failure rollback,
stop/cancel/join diagnostics, post-stop cleanup, command-channel
capacity/state, bounded-channel recreation/completion, abandoned-command
draining, scheduling policy, exit transactions, live-restore cleanup, CTS
disposal warnings, `PlaybackThreadEntry` queue waiting, cancellation exits,
playback pacing handoff, the command switch, and command-complete logging there
instead of expanding the root controller. Keep queue write/coalescing/drop policy
in the command queue partial.
Playback-thread seek, scrub begin/update, end-scrub resume, and paused
exact-resume target handling now live in
`Sussudio/Services/Flashback/FlashbackPlaybackController.ThreadCommands.cs`.
Keep coalesced seek/scrub resolution, exact resume targets, playback resume
handoff, frozen valid-start sampling, scrub-display failure recovery,
end-scrub seek/reopen, playback audio prebuffer priming, and audio/preview
suppression/resume ordering there.
Playback-thread play command execution now lives in
`Sussudio/Services/Flashback/FlashbackPlaybackController.ThreadCommands.cs`.
Keep exact resume, file-open/reopen, audio prebuffer, and rendering resume
ordering there. Pause command execution now lives in
`Sussudio/Services/Flashback/FlashbackPlaybackController.ThreadCommands.cs`;
keep pause-from-live freeze/display ordering, exact resume targets, and
audio/preview suppression there. Nudge command execution now lives in
`Sussudio/Services/Flashback/FlashbackPlaybackController.ThreadCommands.cs`;
keep frame-step decode, no-file recovery, and seek-display failure recovery
there. Terminal go-live/stop command execution lives with the dispatch switch in
`Sussudio/Services/Flashback/FlashbackPlaybackController.ThreadCommands.cs`.
Flashback playback audio routing now lives in
`Sussudio/Services/Flashback/FlashbackPlaybackController.AudioRouting.cs`.
Keep live audio suppress/restore, decoder audio callback wiring, playback
chunk validation/return, playback PTS gate handling, pooled audio-buffer return
warnings, and playback-state audio/preview routing there alongside best-effort
preview submission guards, audio renderer pause/resume/flush guards,
decode-ahead prebuffer target/timeout/frame-budget policy, rewind behavior,
audio-master clock sample state, stale-clock detection, read-only A/V drift
projection, clock-drift computation, pacing correction policy,
delay-adjustment counters, fallback accounting/classification, pending fallback
suppression, read-only fallback reason/drift/clock-age telemetry projection,
and wall-clock sleep/spin pacing.

Flashback playback component lifecycle now lives in
`Sussudio/Services/Flashback/FlashbackPlaybackController.cs`. Keep
construction, initialization, audio/preview component reference updates,
lifecycle/dispose state, disposal, preview-detach cleanup, failed-stop detach
timeout state, deferred preview reattach state, and deferred reattach retry
scheduling there. Keep playback positioning/file handling in
`FlashbackPlaybackController.Positioning.cs` and playback pacing in the
playback-frame/thread-command partials.

Flashback playback decoded-frame submission now lives with held playback frame
ownership in
`Sussudio/Services/Flashback/FlashbackPlaybackController.PlaybackFrames.cs`.
Keep preview-sink selection, submission telemetry, renderer calls,
decoded-frame validity checks, GPU/CPU frame skip reasons, NV12/P010 byte-size
policy, held-frame handoff, release-for-live reset policy, and best-effort
decoded frame release warnings there.
seek-display and playback-submit failure recovery plus decode-error snap,
near-live snap, and software-decode-budget recovery back to live playback state
now live in
`Sussudio/Services/Flashback/FlashbackPlaybackController.PlaybackFrames.cs`;
keep seek commands in their named partial.

Flashback playback decoder file handling now lives in
`Sussudio/Services/Flashback/FlashbackPlaybackController.Positioning.cs`.
Keep decoder creation, active segment file identity, file open checks, and
decoder close/open identity transitions there, alongside best-effort decoder
file close handling, held-frame release during teardown, decoder close/dispose
timing, cleanup telemetry, active fMP4 reopen retry, keyframe-reopen recovery,
near-live reopen guards, adjacent-segment seek fallback policy, segment-start
probing, segment switch telemetry, and adjacent-seek failure handling.
Segment-edge fMP4 reopen/reseek recovery and fMP4 reopen audio-gate restoration
now live with playback frame progression in
`Sussudio/Services/Flashback/FlashbackPlaybackController.PlaybackFrames.cs`.
Keep seek-display and playback pacing in the controller core/thread partials.

Flashback continuous playback progression and timing policy now live with
playback frame reads in
`Sussudio/Services/Flashback/FlashbackPlaybackController.PlaybackFrames.cs`.
Keep seek/scrub keyframe display, active fMP4 keyframe retry, displayed-frame
PTS mapping, seek/scrub decoded-frame acquisition, adjacent-segment fallback
display, no-frame seek-display failure accounting, decoded-frame submission
flow, live-recovery policy invocation, cadence pacing, A/V drift diagnostics,
prebuffer cleanup, A/V drift frame-skip catch-up policy, frame-rate resolution,
pause-from-live target calculation, continuous-playback snap policy,
software-decode budget detection, decoder hardware-acceleration status refresh,
over-budget snap telemetry, rolling playback cadence metric updates, decoded
PTS cadence state/projection/tracking, mismatch telemetry, and cadence-baseline
reset there. Decode-error and near-live snap policy, including the recovery
near-live snap threshold, belongs in the playback live recovery owner.
Segment-edge routing decisions, write-head waits, next-segment switch
transactions, next-file probing, decoder open/seek, switch counters, audio
gates, cadence-baseline reset, and active fMP4 reopen/reseek recovery during
segment-edge handling now live in
`Sussudio/Services/Flashback/FlashbackPlaybackController.PlaybackFrames.cs`.
The live-state recovery implementation is local to
`Sussudio/Services/Flashback/FlashbackPlaybackController.PlaybackFrames.cs`.
Decoder close best-effort handling now lives with decoder file ownership, and
decode-error snap-to-live recovery lives with the continuous playback loop, so
the root controller can remain the construction and core state shell. Public
playback state, GPU-decode toggling, live-gap projection, decoder HW state,
playback PTS anchors, scrub resume state, and state-transition logging now live
in `Sussudio/Services/Flashback/FlashbackPlaybackController.cs`.

Flashback status and playback-position polling timers now live in
`Sussudio/Controllers/Flashback/FlashbackUiControllers.cs`.
`Sussudio/MainWindow.xaml.cs` is the XAML-facing polling
adapter; CTI anchor timing lives with the Flashback UI playhead motion owner.

Settings shelf visibility, the animation gate, and show/hide storyboard
construction now live with shell chrome in
`Sussudio/Controllers/Shell/ShellChromeController.cs`.
`Sussudio/MainWindow.Composition.cs` is the XAML-facing settings
shelf adapter.

Loaded-time startup ordering now lives in
`Sussudio/Controllers/Launch/LaunchFlowController.cs`: native shell reveal
scheduling, initial ViewModel settings load, preview audio fade priming before
device refresh, no-preview placeholder fallback, automation host start in the
finally path, and splash/entrance trigger.
`Sussudio/MainWindow.Composition.cs` preserves the XAML event
handler and shell launch context wiring.

Launch entrance ownership lives with loaded-time startup in the same launch-flow owner:
`Sussudio/Controllers/Launch/LaunchFlowController.cs` owns context,
initial hidden/scaled shell state, splash fade, loading-phrase start/stop
ordering, splash phrase file lookup, Markdown-ish parsing, cached defaults,
exception fallback, randomized interval/mode selection, DispatcherTimer
lifecycle, two-line splash text animation, one-shot splash playback state,
handoff into shell entrance, shell chrome/button/stats entrance choreography,
deferred preview reveal logging, active-storyboard cleanup, and control-bar
shadow fade.
`Sussudio/MainWindow.Composition.cs`
is the XAML-facing adapter for launch entrance wiring.

Control-bar button ownership, hover/press/release scale behavior, static shell
ThemeShadow and translation setup for the control bar and record button now live
in `Sussudio/Controllers/Shell/ShellChromeController.cs`, alongside shell
property-change routing across stats overlay and settings shelf controllers,
settings shelf animation, status-strip projection, and window title formatting.
`Sussudio/MainWindow.Composition.cs` is the XAML-facing adapter.

Preview shell/content fade and scale transitions, video-shadow fade timing,
unavailable-placeholder presentation, and preview reinit transition state now live in
`Sussudio/Controllers/Preview/PreviewTransitionAnimationController.cs`.
`Sussudio/MainWindow.Composition.cs` wires preview-transition
animation callbacks; video-shadow fade callbacks and shared compositor shadow
opacity fades route through the preview surface shadow controller kept in
`PreviewTransitionAnimationController.cs`.

Preview button glyph/tooltip presentation for Start Preview and Stop Preview
now lives with preview lifecycle events in
`Sussudio/Controllers/Preview/PreviewLifecycleEventController.cs`.
`Sussudio/MainWindow.Composition.cs`
wires preview button presentation callbacks and preview
lifecycle property/event routing. Preview
button command choreography now lives in
`Sussudio/Controllers/Preview/PreviewLifecycleEventController.cs`, while
`Sussudio/MainWindow.Composition.cs` keeps the XAML event
name stable as part of the preview transition/presentation adapter.

Demo-visible record-button chrome now lives in
`Sussudio/Controllers/Recording/RecordingControlsControllers.cs`: recording glow,
Rec pulse, starting spinner, normal/recording content, padding, enabled-state
application, the circle/pill width morph, recording-state lockout decisions,
recording property-change routing, ViewModel-derived HDR/title/audio-meter
policy application, and the `RecordingStatePresentationController` facade.
`MainWindow.xaml.cs` wires the chrome controller with the
recording action and recording-state presentation adapters.

Recording button command execution and preview-state logging after a recording
start now live in `Sussudio/Controllers/Recording/RecordingControlsControllers.cs`.
`MainWindow.xaml.cs` is the XAML-facing adapter for recording,
capture-device, and output-path button workflows.

Live-signal pill text application, visibility state, show/hide debounce timers,
and the small scale/fade animation now live in
`Sussudio/Controllers/Shell/ShellChromeController.cs` beside the rest of shell
chrome. `MainWindow.Composition.cs` is the XAML-facing adapter, while
`Sussudio/ViewModels/ViewModelBuilders.cs` owns label formatting.
Source telemetry summary, telemetry age, and target-summary display text
formatting now live in `Sussudio/ViewModels/ViewModelBuilders.cs`.
HDR runtime state/readiness projection from capture runtime snapshots,
target-summary property application, live-signal info projection, and
auto-resolution display text live together in
`Sussudio/ViewModels/MainViewModel.cs`.

Preview-volume fade-in/fade-out state, saved target volume, storyboard lifetime,
and volume save suppression now live with preview start/stop/reinit event
routing in `Sussudio/Controllers/Preview/PreviewLifecycleEventController.cs`.
`Sussudio/MainWindow.Composition.cs` is the XAML-facing adapter.
Preview-audio volume transition mechanics and ramp diagnostics now live in
`Sussudio/ViewModels/PreviewAudioTransitionControllers.cs`, which owns
save suppression/override state, transition priming and restore behavior,
trace adapters, property-to-session volume forwarding, ramp constants/easing,
async ramp-down/ramp-up execution, bounded trace storage, trace session
start/complete, trace-point capture, sampler loop, and delayed sampler
shutdown.
`Sussudio/ViewModels/MainViewModel.AudioState.cs` is the
view-model compatibility facade for preview-volume save suppression, override,
change notification, ramp adapter methods, and monitoring enable/disable
orchestration with coordinator sequencing. Audio capture
enablement and Flashback restart/teardown routing live in
`Sussudio/ViewModels/MainViewModel.AudioState.cs`, while
audio-preview monitoring toggles live in
`Sussudio/ViewModels/MainViewModel.AudioState.cs`.

Preview reinit animation active state, first-visual transition clears,
startup-reset preservation, completion presentation decisions, and
`D3D11_RENDERER_REINIT_FLAG` / `PREVIEW_REINIT_ANIMATE_*` logs now live in
`Sussudio/Controllers/Preview/PreviewTransitionAnimationController.cs`.
`Sussudio/MainWindow.Composition.cs` is the XAML/MainWindow
adapter that supplies renderer-stop-before-teardown and UI callback endpoints
for reinit completion.

Preview startup attempt/state bookkeeping, timestamps, cached failure/
missing-signal details, state/log transitions, first-visual confirmation
sequencing, signal-window predicates, snapshot missing-signal refresh gates,
and reset orchestration now live in
`Sussudio/Controllers/Preview/Startup/PreviewStartupControllers.cs` instead of a
MainWindow field bundle.
`Sussudio/MainWindow.Composition.cs` wires UI/runtime
callbacks into the session, watchdog, and signal controllers, stable state
projections, startup state, renderer-attached, first-visual, begin-attempt,
reset adapters, raw timeout diagnostic snapshots, live preview signal state,
renderer visibility details, logging, and confirmation callbacks.
Watchdog/telemetry timers, timeout configuration, timeout recovery, failure-stop
scheduling, readiness-signal coordination, missing-signal updates,
readiness-signal required/received state, missing-signal calculation,
playback-progress diagnostics, startup signal log strings, GPU position counter
state, first-visual confirmation decisions, playback-advance threshold checks,
readiness result snapshots, signal-list formatting, and timeout diagnostic
payload formatting live in
`Sussudio/Controllers/Preview/Startup/PreviewStartupControllers.cs`;
the MainWindow/XAML-facing adapter stays in
`Sussudio/MainWindow.Composition.cs`. Timeout reason,
timeout status, and failure-stop status text also live inside
`Sussudio/Controllers/Preview/Startup/PreviewStartupControllers.cs`, where the timeout
and failure-stop decisions are made. This keeps the root shell focused on wiring
while leaving the existing startup state machine behavior unchanged.
Delayed preview reveal after first visual now lives in
`Sussudio/Controllers/Preview/PreviewLifecycleEventController.cs`; the adapter is
`Sussudio/MainWindow.Composition.cs`. Watchdog/timeout recovery remains in
`Sussudio/Controllers/Preview/Startup/PreviewStartupControllers.cs`.
Preview startup loading overlay presentation now lives in
`Sussudio/Controllers/Preview/PreviewTransitionAnimationController.cs`.
`Sussudio/MainWindow.Composition.cs` is the XAML-facing adapter; watchdog and
timeout recovery stay in `Sussudio/Controllers/Preview/Startup/PreviewStartupControllers.cs`.
Top-level preview resize telemetry throttling now lives in
`Sussudio/Controllers/Preview/Renderer/PreviewRendererHostController.cs`.
`Sussudio/MainWindow.Composition.cs` wires renderer-host context
callbacks, the `SizeChanged` adapter, renderer-host reset handoff, renderer
start/stop/shutdown, and reinit-unsafe-window adapters; reinit renderer-stop/timeout policy lives with
`PreviewRendererHostController.cs`; preview surface presentation and shadow
visuals live together with `PreviewTransitionAnimationController.cs`.

Preview-specific ViewModel event lifecycle and preview property-change routing
now live in `Sussudio/Controllers/Preview/PreviewLifecycleEventController.cs`.
`Sussudio/MainWindow.Composition.cs`
wires button presentation callbacks and preserves event
handler signatures and delegates into the controller. The broad
`MainWindow.xaml.cs` dispatcher now owns only the `PropertyChanged`
event envelope, property-name normalization, and visible route order. Preview
reinit transition state and log ownership now live in
`Sussudio/Controllers/Preview/PreviewTransitionAnimationController.cs`, while
`Sussudio/MainWindow.Composition.cs` keeps the renderer-stop-before-teardown
handoff and XAML callback endpoints for completion presentation.

Bottom status-strip projection now lives with shell chrome in
`Sussudio/Controllers/Shell/ShellChromeController.cs`, while
`Sussudio/MainWindow.Composition.cs` is the XAML-facing adapter and
builds the ViewModel snapshot passed into the controller. The controller owns
the status-strip `PropertyChanged` router and preserves the recording-only
window-title refresh on recording-time updates. Flashback bitrate presentation
also routes through this controller so the recording bitrate text keeps one UI
owner.

Pure recording-state lockout decisions and recording-state UI projection now live in
`Sussudio/Controllers/Recording/RecordingControlsControllers.cs`: recording-time
capture/audio control enablement, analog gain enablement, transition button
enablement, FFmpeg button enablement, settled record-button content visibility,
ViewModel-derived property-name routing, lockout/HDR/title/audio-meter policy application, and record-button
chrome.
`MainWindow.xaml.cs` is the XAML-facing recording adapter.

Capture-option property-name routing still lives in the focused
`Sussudio/MainWindow.xaml.cs` adapter. Output-path routing
lives in `OutputPathController` inside `RecordingControlsControllers.cs`, shell visibility route order lives in
`ShellChromeController` over `StatsOverlayCompositionController` and
`SettingsShelfController` through
`Sussudio/MainWindow.Composition.cs`, and live
source-signal routing lives in `ShellChromeController`. Keep the root dispatcher limited to route order,
and add new property-name cases to the nearest focused owner.

Flashback-specific ViewModel property adapter dispatch now lives in
`Sussudio/Controllers/Flashback/FlashbackUiControllers.cs`:
timeline lockout, marker and playhead refresh, export progress, and Flashback
settings-control sync. `Sussudio/MainWindow.xaml.cs` is the
XAML/MainWindow property-change adapter that composes the Flashback route table
callbacks alongside the root ViewModel router.

Audio and microphone-specific ViewModel property projections now live with
audio control setup in `Sussudio/Controllers/Audio/AudioControlBindingController.cs`:
audio toggles, monitoring meter state, preview volume slider sync, microphone
enablement, microphone volume sync, microphone volume slider synchronization,
save triggers, shelf enablement, and mic-meter row animation state. The
controller also owns the audio property-change router;
`Sussudio/MainWindow.xaml.cs` is the XAML-facing audio/microphone
presentation adapter.

Responsive shell layout is owned by
`Sussudio/Controllers/Shell/ShellChromeController.cs`, which keeps the
control-bar label breakpoint, capture-settings narrow/wide grid-slot policy,
responsive visibility for the complete control-bar label set, and
capture-settings grid placement together with the rest of shell chrome.
`MainWindow.Composition.cs` remains the XAML-facing adapter.
Responsive layout ownership checks live in
`tests/Sussudio.Tests/MainWindow.ControllerOwnership.Tests.cs`.

Capture, audio, microphone, and encoder selection synchronization now lives in
`Sussudio/Controllers/Capture/CaptureSelectionBindingController.cs`. The
controller owns its context lifetime, XAML control dependency bag,
capture/audio/microphone/encoder collection wiring, collection-change
debounce/queued sync, available-option property-change rebinding,
capture-device selection, pending-device apply state, mismatch logging, audio
input and microphone selection, resolution and frame-rate selection, recording
format/quality/preset/split-encode selection, shared string ComboBox selection
application, device-audio mode/gain projection, and the capture-selection
`PropertyChanged` router. The local `CaptureComboBoxSelectionNormalizer` in
`Sussudio/Controllers/Capture/CaptureSelectionBindingController.cs` owns pure
capture/audio/microphone/resolution/frame-rate/string ComboBox selection and
fallback matching, while
`Sussudio/MainWindow.xaml.cs` keeps controller
instantiation, XAML dependency wiring, collection/property-change adapters, and
the thin XAML-facing selection bridges for device, audio, device-audio,
capture-mode, and recording option selection while preserving the old
method names for binding setup and cross-controller calls.

Capture-device refresh/apply button workflows now live in
`Sussudio/Controllers/Capture/CaptureSelectionBindingController.cs`.
`MainWindow.xaml.cs` is the XAML-facing adapter and keeps the explicit
apply/reinit path beside selection synchronization.

Pure capture-option presentation decisions now live in
`Sussudio/Controllers/Capture/CaptureOptionBindingController.cs`: HDR toggle
enablement, MJPEG decoder count visibility, bitrate/preset visibility, audio
clipping visibility, and initial decoder-count clamping. XAML control
application, decoder-count selection handling, delegation to policy helpers,
and pure HDR readiness hint/FPS telemetry tooltip text policy live in
`Sussudio/Controllers/Capture/CaptureOptionBindingController.cs`.
`MainWindow.xaml.cs` is the XAML-facing adapter and keeps
the existing method names for binding setup, property-change projection, and
the XAML decoder-count selection event.

Capture option binding setup now lives in
`Sussudio/Controllers/Capture/CaptureOptionBindingController.cs`. It keeps the
capture-option binding adapter context, video-format and initial decoder
projection, initial selection projection, resolution/frame-rate selection
handlers, recording option event bindings for format, quality, preset,
split-encode, video format, and custom bitrate, HDR/true-HDR click binding,
`ShowAllCaptureOptionsToggle` click binding, capture-option/source-signal
property-change routing, custom-bitrate property-change value projection,
HDR/true-HDR ViewModel-to-control sync, preview HDR passthrough forwarding,
option affordance application, telemetry tooltip policy, decoder-count
presentation, and presentation callback routing for option affordances, telemetry tooltips, and
source overlay refreshes.
`Sussudio/MainWindow.xaml.cs` now owns the XAML-facing
binding setup methods and the small property-change forwarding method, so
there is no separate pass-through partial just for capture-option property
changes.
`MainWindow.xaml.cs` keeps the old capture and recording option
method names used by `SetupBindings()`.

MainWindow capture ownership tests now mirror these runtime owners instead of
living in one capture test grab-bag. Selection bindings, selection normalizer
policy, device actions, option presentation, option affordance policy, option
bindings, and option tooltip formatting each have a focused
`MainWindow.ControllerOwnership.Capture.*.Tests.cs` file registered with the
presentation-preview harness coverage check.

Recording output-path textbox, tooltip, resize-event updates, browse, and
open-recordings button workflows now live in
`Sussudio/Controllers/Recording/RecordingControlsControllers.cs`, along with
pure truncation text policy.
`OutputPathController` also owns the output-path property-change route;
`MainWindow.xaml.cs` is the XAML-facing adapter used by binding setup,
property changes, and button events.

Diagnostic session DTOs live in `tools/Common/DiagnosticSessionResult.cs`,
which owns run options, sampled snapshot DTOs, shared tool invocation defaults,
the ssctl diagnostic-session usage string, explicit scenario phase input
handoff, immutable completion handoff, mutable in-flight phase state, and the
final summary result DTO,
while `DiagnosticSessionScenarioCatalog.cs` owns scenario name constants, the
MCP-compatible scenario description, the CLI help-list constant, normalization,
entry lookup, requirement queries, export verification artifact lookup, and
scenario ordering by composing core, Flashback playback, Flashback
export/lifecycle, Flashback recording/rejection, and combined scenario
requirement metadata.
`DiagnosticSessionRunner.cs` owns the public compatibility entry points and the
visible run phase sequence around context creation, initial snapshot, scenario
execution, cleanup, and completion handoff. `DiagnosticSessionRunContext.cs`
owns the cohesive mutable per-run context: bootstrap, actions, warnings,
samples, run state, command channel, scenario cancellation source, initial
snapshot state and capture, live-state writer handoff, disposal, and
scenario/completion context construction with the callback/token handoffs passed
into those phases.
`DiagnosticSessionRunner.cs` owns the post-cleanup evidence/result sequence
for recording checks, post-run timeline and final snapshot capture, result-build
request mapping, result-build invocation, and terminal live-state write.
`DiagnosticSessionRunner.cs` owns the completion context handoff consumed by the
post-cleanup phase and the main scenario phase for setup/startup, sampling,
completion delegation, and fault drain delegation.
`DiagnosticSessionRunner.cs` owns post-sampling completion order and
fault-drain delegation beside the scenario phase sequence: registered background
work before rejected-export handling, rejected-export handling before PresentMon
completion, and interrupted drain handoff.
`DiagnosticSessionRunner.cs` owns the final result-build
request mapping consumed by the completion phase.
The public options/result/sample contracts are separated from runner behavior. The result
DTO root owns core session metadata, terminal state, artifacts, actions, and
warnings; the result partials own capture/source, Flashback playback command
queue, Flashback playback cadence, Flashback playback 1% low sample-window
evidence, Flashback playback decode, Flashback playback audio-master, Flashback
playback submit/stage/seek, Flashback recording, Flashback export, preview
cadence, preview scheduler, preview D3D, preview visual cadence, process,
recording verification, and PresentMon fields.

Diagnostic-session result text now lives in
`tools/Common/DiagnosticSessionResultFormatter.cs`. The formatter owns the
public `Format(...)` flow, section ordering, and all rendered rows: overview,
capture mode, recording verification, PresentMon, Flashback playback/recording/
export, preview scheduler, preview D3D, visual cadence, process performance,
artifacts, actions, warnings, and shared optional text helpers.
The runner keeps `Format(...)` as a compatibility wrapper so existing ssctl
and MCP callers do not need to know about the formatter owner.

Diagnostic-session result construction now lives in
`tools/Common/DiagnosticSessionResultBuilder.cs`. The root owns result phase
orchestration, artifact-write handoff, summary-write handoff, and final
summary emission plus summary-write failure repair while the runner keeps the
phase sequence. It also owns final-result orchestration from analysis and
artifact paths into the named projection-set owner, plus final
`DiagnosticSessionResult` DTO assignment from the projection set. Keep domain
projection composition in the projection owner rather than in the initializer.
`DiagnosticSessionResultBuilder.Projections.cs` owns projection-set assembly,
the private projection-set handoff record, and the result projection
records/builders for overview, capture, Flashback playback/recording/export,
preview cadence/scheduler, preview D3D, and visual cadence. The detailed
Flashback playback command queue, cadence/slow-frame/dropped-frame, 1% low,
audio-master, decode timing, and stage DTO value maps live with that projection
owner. Diagnostic metric gathering for validation/result projections and
analysis warning emission live in
`DiagnosticSessionResultBuilder.Analysis.cs`, which also owns the private
analysis handoff record plus Flashback playback/export warning text, threshold
guards, tolerated Flashback scenario warning classification, and the named
validation handoff order for Flashback playback, cleanup lifecycle restore,
preview scheduler analysis, and diagnostic health. Preview-scheduler analysis
handoff values and validation orchestration live there too: MJPEG jitter-buffer
counter/delta reads, last drop/underflow reason and age reads,
max/schedule-late aggregation, target-FPS fallback, visual-cadence tolerance
checks, sparse deadline/drop tolerance selection, and the call into shared
Flashback preview validation.
`DiagnosticSessionResultBuilder.cs` owns the result-build request handoff
created by `DiagnosticSessionRunner.cs` beside the summary orchestration that
consumes it. Diagnostic health summary snapshot selection, health summary text
projection, verdict composition, diagnostic-health warning tolerance, sparse
source-cadence warning tolerance, sparse preview-scheduler warning tolerance,
source-reader/ingest warning deltas, tolerated-warning reason selection, and
emitted health warning text live in `DiagnosticSessionResultBuilder.Analysis.cs`.
`DiagnosticSessionResultAnalysis.PreviewScheduler` is the single record
property that carries those values into the scheduler result projection without
rereading MJPEG jitter-buffer snapshot keys. Preview cadence, visual cadence,
and D3D frame-stats/slow-frame/
CPU-timing result projection values live with the other small projection
builders in `DiagnosticSessionResultBuilder.Projections.cs`. The D3D fields
still travel through a distinct `PreviewD3D` projection set member so renderer
timing semantics stay separate from scheduler/jitter policy.
Flashback recording backend/growth/integrity DTO projection values and export
status/progress DTO projection values live in
`DiagnosticSessionResultBuilder.Projections.cs`; result construction
still consumes named Flashback projections while preserving the existing
`summary.json` field shape.
Export force-rotate fallback counters now travel with
`FlashbackExportSessionMetrics` instead of loose analysis record fields.
Capture selection, negotiated format, source geometry, detected cadence, HDR,
and source-telemetry DTO projection values live in
`DiagnosticSessionResultBuilder.Projections.cs`.

Diagnostic-session result artifact setup now lives in
`tools/Common/DiagnosticSessionResultBuilder.cs` beside summary writing. It owns
result artifact path construction, pre-summary sample, frame-ledger, and
timeline writes while the builder keeps summary field construction.

Shared diagnostic-session optional text formatting now lives in
`tools/Common/DiagnosticSessionResultFormatter.cs` alongside the human-readable
result text owner. Keep cross-cutting `FormatOptional(...)` handling there
instead of reintroducing private duplicates in scenario, result builder,
formatter, or validation policy files.

MCP performance tooling now lives in
`tools/McpServer/Tools/PerformanceTools.cs`. Keep the public timeline tool entry
point, command response handling, JSON-to-row projection
orchestration, root cadence, preview/MJPEG/D3D, Flashback playback, Flashback
export, system row projection fields, private row model, timeline table text
rendering, first-vs-last trend text, preview cadence, visual/MJPEG fingerprint,
jitter, D3D, slow-stage, Flashback playback, command, failure, cleanup, stage,
export trend text, target-summary orchestration, target, preview, Flashback,
and system pressure summaries there alongside compact value, command-message,
preview jitter-depth, D3D bottleneck, Flashback stage, cleanup, export,
byte-rate formatting helpers, shared summary predicates, and pressure counters.
Keep the PresentMon MCP entry points, structured-content shape, probe
invocation, and app-snapshot request/fallback behavior there too.
The frame-pacing verdict MCP tool follows the same shape: keep MCP attributes,
method signature, pipe command orchestration, response shaping, channel/timeline
projection, readiness and verdict policy, operator-facing text, and private
records together in `FramePacingVerdictTools.cs`.
MCP automation command-control wrappers stay in
`AutomationControlTools.cs`: keep device/capture/pipeline settings,
structured capture options, window/UI settings, preview/recording control,
condition waits, Flashback enable/apply/segments/action/export, and
verification methods together while preserving the public
`CaptureSettingsTools`, `DeviceTools`, `CaptureOptionsTools`,
`PipelineSettingsTools`, `WindowTools`, `UiSettingsTools`, `PreviewTools`,
`RecordingTools`, `WaitTools`, `FlashbackTools`, and `VerificationTools` type
names. Keep heavier readback or evidence surfaces separate in
`AppStateTools.cs`, `PerformanceTools.cs`, `PreviewInspectionTools.cs`, and
other files with independent policy.
Preview visual inspection MCP methods stay in `PreviewInspectionTools.cs`; keep
the public `PreviewColorProbeTools`, `VideoSourceProbeTools`,
`PreviewFrameCaptureTools`, and `WindowScreenshotTools` type names stable while
avoiding one-method probe/capture files. Keep preview color/source probe text,
the public `capture_preview_frame` entry point, default output path, payload,
enum-backed `CapturePreviewFrame` routing, report layout, 16-bin histogram
math/rendering, anomaly diagnosis policy, aspect checks, and whole-window
screenshot response formatting together there.
PresentMon MCP stays intentionally shallow: keep `capture_presentmon`,
`capture_presentmon_raw`, structured-content shape, and `PresentMonProbe.RunAsync`
invocation in `PerformanceTools.cs`; keep the app-snapshot request and malformed
snapshot/pipe-failure fallback there too.
Shared option precedence and preview-present field extraction belong to
`tools/Common/PresentMon/PresentMonProbe.cs`.

Diagnostic-session command sending now lives in
`tools/Common/DiagnosticSessionCommandChannel.cs`. It owns serialized command
execution, command failure accounting, and enum-backed command-name resolution
for fixed diagnostic-session commands, raw command send overloads,
connect-retry wrapping, local failure-response fallback when connect retry
returns no response, pipe retry/error classification, access-denied permanent
failure policy, connect failed/timeout retry policy, and wait command helper
payload shaping. Scenario setup and cleanup pass the channel itself for lifecycle mutations so
`SetFlashbackEnabled`, `SetPreviewEnabled`, `SetRecordingEnabled`, and
`FlashbackAction` flow through `AutomationCommandKind` overloads; the runner
keeps phase orchestration and its public string delegate compatibility.

Diagnostic-session JSON artifact helpers now live in
`tools/Common/DiagnosticSessionResultBuilder.cs` beside pre-summary artifact
path construction and writes. The runner still owns the session lifecycle,
while JSON object creation, best-effort file writes, and frame-ledger trace
construction stay in the result-builder helper section. Snapshot / verification
response-shape extraction now lives in
`tools/Common/DiagnosticSessionRunContext.cs` beside the mutable run
infrastructure that consumes initial snapshots and hands response helpers to
scenario files.

Diagnostic-session initial snapshot capture now lives in
`tools/Common/DiagnosticSessionRunContext.cs` beside the mutable initial
snapshot fields. It owns the baseline snapshot capture through
`AutomationCommandKind.GetSnapshot`, the unknown-state warning, and
initial-snapshot exception recording while the runner keeps phase ordering.

Diagnostic-session run context now lives in
`tools/Common/DiagnosticSessionRunContext.cs`. `DiagnosticSessionRunContext.cs`
owns the cohesive mutable per-run context: bootstrap, actions, warnings,
samples, terminal exception state, last-stage tracking, best-effort artifact
write failure recording, command channel, scenario cancellation source, initial
snapshot state and capture, live-state writer handoff, disposal, and
scenario/completion context construction with the explicit callback/token
handoffs consumed by scenario and completion phases.

Diagnostic-session live breadcrumbs now live in
`tools/Common/DiagnosticSessionRunContext.cs` beside the mutable run context
that owns their lifecycle. It owns the `session-live.json` path, payload shape,
health and warning projection, terminal override mapping, and sampling
live-state write throttle.

Diagnostic-session run bootstrap now lives in
`tools/Common/DiagnosticSessionRunContext.cs` beside the mutable per-run
context that consumes it. It owns scenario normalization, scenario-plan
selection, duration/sample clamping, session identity, output-directory
creation, and runner process metadata while the runner keeps command-channel
lifetime and phase ordering.

Diagnostic-session output locking now lives in
`tools/Common/DiagnosticSessionRunner.cs` beside the phase sequence that
acquires it. It owns the `.sussudio-diag.lock` file, exclusive
`FileShare.None` open, delete-on-close cleanup, and concurrent-output-directory
failure message.

Diagnostic-session background task tracking now lives in
`tools/Common/DiagnosticSessionRunner.cs`. It owns scenario task registration,
deterministic await order, normal registered scenario completion, PresentMon and
deferred Flashback recording-settings task tracking, interrupted-session
observation, warning collection, and the drain handoff record.

Diagnostic-session scenario activation now lives in
`tools/Common/DiagnosticSessionScenarioActivation.cs`, which owns initial setup
ordering and result handoff, Flashback enable/disable for scenario
requirements, preview start and video-flow wait, recording start and Flashback
recording-readiness wait, public startup orchestration, Flashback scenario
registration delegation, deferred Flashback recording-settings task
registration, direct Flashback playback start command/playback-state wait,
optional PresentMon launch, correlation snapshot capture, and `presentmon.csv`
output selection while delegating option/correlation policy to
`tools/Common/PresentMon/PresentMonProbe.cs`. The runner keeps the
setup/startup/sampling/cleanup/summary phase flow. Fixed setup mutations should
use `DiagnosticSessionCommandChannel` typed `AutomationCommandKind` sends.

Diagnostic-session post-run actions now live in
`tools/Common/DiagnosticSessionPostRunActions.cs`. It owns the public cleanup
flow and ordering, recording stop for verification, Flashback playback go-live
restore, preview stop, Flashback enable-state restore, typed automation command
sends, cleanup result record, deferred Flashback recording-settings restore,
last-recording or Flashback export verification command selection, payload
shape, 60-second timeout, cloned verification result, skipped-verification
action text, and Flashback recording validation while the runner keeps the
high-level post-cleanup phase order. Result analysis validation owns the
post-cleanup warning validator.

Diagnostic-session post-run snapshot fetches now live in
`tools/Common/DiagnosticSessionRunner.cs` beside the completion phase that
orders them. It owns performance timeline artifact input and final health
snapshot refresh.

Diagnostic-session scenario metadata now lives in
`tools/Common/DiagnosticSessionScenarioCatalog.cs`. The catalog owns
normalization and entry lookup, scenario names, HelpList/Description text,
the `Names` projection, requirement queries, export-verification lookup, and
scenario ordering by composing focused entry groups for core, Flashback
playback, Flashback export, Flashback recording, and combined setup
requirement metadata, export verification filenames, and plan assigned to each
normalized scenario group. It also owns the plan DTO, creation factory,
catalog lookup handoff, and grouped warning/validation policy switches,
including the preview-cycle grouped predicate, so the runner does not grow
direct scenario string comparisons.

Diagnostic-session cleanup restore validation now lives in
`tools/Common/DiagnosticSessionResultBuilder.Analysis.cs`. It owns warnings
for preview, Flashback, and playback state that remain active after the runner
attempts cleanup.

Diagnostic-session Flashback cycle scenarios now live in
`DiagnosticSessionFlashbackCycleScenarios.cs`, which owns restart/encoder/
lifecycle cycle task registration, restart-cycle playback priming/restart/
refill/export verification, encoder-cycle preset cycling, snapshot validation,
export verification, original-preset restore, playback disable/re-enable
lifecycle command flow, post-disable playback-thread/queue health, and
post-re-enable active-state validation. Startup only delegates selected cycle
and lifecycle scenario registration.

Diagnostic-session sampling now lives in
`tools/Common/DiagnosticSessionRunner.cs` beside the scenario phase sequence
that invokes it. Keep the sample append before the optional checkpoint callback
so checkpoint failures cannot orphan an unseen sample.

Diagnostic-session metric projection now lives in
`tools/Common/DiagnosticSessionMetrics.cs`. It owns read-only metric DTOs and
projections: source, preview, and visual cadence aggregation, visual-cadence
health classification, D3D metric aggregation, playback command-health deltas,
and shared counter-delta helpers.

Diagnostic-session Flashback support helpers now live in
`tools/Common/DiagnosticSessionFlashbackSupport.cs`, which owns strict export
verification payload construction, rotated-export segment-count parsing,
range-selection cleanup, read-only segment parsing/waits/playback headroom
polling, and recording/playback/preview warning validation. Keep scenario
command sequencing in separate scenario owners.

Diagnostic-session Flashback export scenarios now live in
`DiagnosticSessionFlashbackExportScenarios.cs`. It owns scenario task
registration plus concurrent export, rotated export, disable-during-export
command coordination, export-during-playback command choreography, selection
range orchestration, verification, cleanup, and playback command-health
validation for the export scenario family.
Diagnostic-session startup makes a single qualified call into the export
scenario owner. Do not reintroduce one-method registration partials.

Diagnostic-session Flashback metric projection now lives in
`tools/Common/DiagnosticSessionFlashbackMetrics.cs`. Recording/export metrics,
playback-session observation and aggregation, and playback result copying stay
in one concrete behavior owner instead of an empty partial family. Playback
session metrics own observation dispatch, active/relevant snapshot gating,
session frame-count projection, 1% low capture, frame/decode maxima,
audio-master maxima, and end-of-session playback counter deltas. Export metrics
also own force-rotate fallback total, delta, and last fallback segment count,
derived outside export-observed relevance gating. These helpers remain
snapshot-only projections and must not send automation commands.

MCP fixed command routes should use `AutomationCommandKind` overloads when the
command is part of the shared catalog. Keep this as an ownership rule, not a
per-route table: record only new file ownership or deliberate exceptions here.
String command names remain only for catalog/manifest-backed dynamic batches,
diagnostic-session command callbacks, and intentionally unconverted compatibility
surfaces with focused coverage.

Diagnostic-session Flashback preview-cycle scenarios now live in a focused
scenario-family owner. `DiagnosticSessionFlashbackPreviewCycleScenarios.cs`
owns task registration, priority, task-label, started-action wiring, and the
preview stop/restart command choreography for normal Flashback, playback, and
recording-backed diagnostics. Preview-cycle scenario selection stays in the
`DiagnosticSessionScenarioCatalog` family, including grouped preview-cycle
policy.

Diagnostic-session Flashback rejected-export scenarios now live with the
Flashback export scenario owner in
`tools/Common/DiagnosticSessionFlashbackExportScenarios.cs`. The export scenario
owner covers selected rejected-export dispatch, inactive-buffer failure-kind and
last-result assertions, and active-Flashback-recording failure-kind and
backend-stability assertions.

Diagnostic-session Flashback recording-settings deferral now lives in
`DiagnosticSessionFlashbackRecordingSettingsScenarios.cs`, which owns deferred
preset state, during-recording command choreography, restart/disable rejection
policy, active recording backend/file/counter stability checks, post-stop preset
verification, encoder-frame checks, and original-preset restore verification.

Diagnostic-session Flashback segment playback now lives in
`tools/Common/DiagnosticSessionFlashbackSegmentPlaybackScenarios.cs`, which owns
task registration, completed-segment boundary-crossing choreography, playback
target acquisition, recording-assisted retry routing, go-live restore,
post-boundary snapshot/FPS/command-health warning policy, and best-effort
recording cleanup. `DiagnosticSessionFlashbackSupport.cs` stays the read-only
segment parsing and wait-policy helper.

Diagnostic-session Flashback segment handling is a section of
`DiagnosticSessionFlashbackSupport.cs`. It owns `FlashbackGetSegments` response
parsing, the parsed segment probe DTO, completed-segment discovery waits,
playable completed-segment target selection plus the playback-target DTO, and
playback-boundary headroom polling while the runner keeps scenario command
sequencing.

Diagnostic-session Flashback snapshot waits are also a support section of
`tools/Common/DiagnosticSessionFlashbackSupport.cs`. The
`DiagnosticSessionFlashbackWaits` helper owns read-only polling loops for
preview-active state, Flashback-active state, Flashback-backed recording
readiness, stress buffer readiness, playback state, boundary crossing,
warmed-playback frame-count/FPS, and position convergence while the runner
keeps scenario command sequencing.

Diagnostic-session Flashback metrics live in
`tools/Common/DiagnosticSessionFlashbackMetrics.cs`, including the
`FlashbackRecordingSessionMetrics`, `FlashbackExportSessionMetrics`,
`FlashbackPlaybackSessionMetrics`, and `FlashbackPlaybackResultMetrics` handoff
shapes; recording metric projection; export-relevance and snapshot max
aggregation; metric orchestration; final force-rotate fallback counters;
playback snapshot observation; active/relevant snapshot gating; session
frame-count projection; 1% low window capture; frame/decode maxima;
audio-master maxima; end-of-session playback counter deltas; final playback
result construction; and observed-gated command, cadence, decode, audio-master,
and stage end-snapshot reads. Preserve the final `init` DTO construction unless
a broader construction pattern replaces it deliberately.

Diagnostic-session Flashback stress orchestration now lives in
`tools/Common/DiagnosticSessionFlashbackStressScenario.cs`, which owns stress
thresholds, stress/scrub-stress task registration, main stress and scrub-stress
command choreography, stress export verification, warmed-playback frame/FPS/1%
low checks, audio-master fallback delta capture/classification, shared
live/empty-queue drain polling, and command-health/latency/final-state warning
policy while the runner only starts the scenario tasks.

Diagnostic-session Flashback validation is also a section of
`tools/Common/DiagnosticSessionFlashbackSupport.cs`. It owns recording,
playback, and preview scheduler warning thresholds over already projected
metrics while the runner retains scenario orchestration.

Diagnostic-session health policy now lives in
`tools/Common/DiagnosticSessionHealthPolicy.cs`. It owns health severity,
observation, Flashback warmup filtering, source/preview/Flashback
health-observation classifiers, sparse cadence tolerances, and tolerated warning
classification while the runner still owns scenario execution and warning emission.

Shared automation pipe client ownership lives under
`tools/Common/AutomationPipeClient/`. `AutomationPipeClient.cs` owns command
envelope sending, typed `AutomationCommandKind` command-id routing,
`not_ready` retry policy, named-pipe connect orchestration, connect failure
classification with exact CLI/MCP diagnostic error codes, write/read framing,
response timeout, command-specific timeout selection for string and typed
commands, shared response-element validation, synthetic error shaping, and
handoff to
`Sussudio.Automation.Contracts/AutomationPipeProtocol.cs`.
`AutomationPipeClient.cs` owns tolerant response-state parsing handoff to
`Sussudio.Automation.Contracts/AutomationPipeProtocol.cs`; that contract file
also owns the command result handoff, pipe client exception taxonomy,
response-state parsing, unknown-command handling, structured error-envelope
creation, and common transport/protocol exception mapping for the shared command
transport.

PresentMon public DTOs, runner behavior, result formatting, and CSV parsing
live together in `tools/Common/PresentMon/PresentMonProbe.cs`: options, result,
summary, swap-chain, app-correlation summary, metric DTOs, public option
construction, preview snapshot correlation extraction, run orchestration,
target process/PresentMon executable/output-path resolution, command-line
construction, argument quoting, process supervision, stdout/stderr drain,
timeout kill, temp CSV cleanup, probe-result message shaping, result text
formatting, CSV parse overloads, selected-row filtering, summary assembly,
swap-chain normalization/selection, header/field parsing, scalar metric reads,
CSV line tokenization, row ingestion, header index construction,
schema-presence detection, blank-line skipping, row index assignment, private
parsed CSV row shapes, row projection from header-indexed fields, app-present
correlation, warnings, counted text fields, and percentile metric aggregation.

EGAVDS audio probing keeps the CLI command flow, SetupAPI device lookup,
audio input/gain actions, SWIG callback registration, EGAVDeviceSupport
imports, SetupAPI imports, and native interface DTOs in
`tools/EgavdsAudioProbe/Program.cs`.

Remaining `tools/Common` ownership:

- `AutomationPipeClient/AutomationPipeClient.cs`
- `DiagnosticSessionPostRunActions.cs`
- `DiagnosticSessionFlashbackCycleScenarios.cs`
- `DiagnosticSessionFlashbackSupport.cs`
- `DiagnosticSessionFlashbackExportScenarios.cs`
- `DiagnosticSessionFlashbackMetrics.cs`
- `DiagnosticSessionFlashbackPreviewCycleScenarios.cs`
- `DiagnosticSessionFlashbackRecordingSettingsScenarios.cs`
- `DiagnosticSessionFlashbackSegmentPlaybackScenarios.cs`
- `DiagnosticSessionFlashbackStressScenario.cs`
- `DiagnosticSessionHealthPolicy.cs`
- `DiagnosticSessionMetrics.cs`
- `DiagnosticSessionResult.cs`
- `DiagnosticSessionCommandChannel.cs`
- `DiagnosticSessionResultBuilder.cs`
- `DiagnosticSessionResultBuilder.Projections.cs`
- `DiagnosticSessionResultBuilder.Analysis.cs`
- `DiagnosticSessionResultFormatter.cs`
- `DiagnosticSessionRunContext.cs`
- `DiagnosticSessionScenarioCatalog.cs`
- `DiagnosticSessionScenarioCatalog.cs`
- `DiagnosticSessionScenarioActivation.cs`
- `DiagnosticSessionRunner.cs`
- `tools/Common/PresentMon/PresentMonProbe.cs`

## Next Slices

Small-file hygiene applies to every slice below: prefer a named owner when the
runtime responsibility is real, but do not create or keep sub-100-line files
just to make a partial family look tidy. A small file should pay for itself by
owning a stable contract, hot-path lifetime, XAML adapter surface, shared tool
surface, or test boundary that would be harder to audit if merged. If a tiny
file only holds private DTOs, constants, or pass-through helpers for one nearby
owner, fold it back into that owner and update the source-shape tests and
`docs/architecture/AGENT_MAP.md` in the same slice.

1. Keep diagnostic-session runner internals aligned by owner.

   `tools/Common/DiagnosticSessionRunner.cs` owns the public compatibility
   surface plus the visible run phase sequence, while
   `tools/Common/DiagnosticSessionRunContext.cs` owns the
   cohesive mutable per-run context: snapshot, live-state, disposal, and
   explicit scenario/completion context construction.
   `DiagnosticSessionRunner.cs` owns the
   post-cleanup evidence/result sequence, completion context handoff, and
   result-build request mapping, plus the main scenario execution phase
   including scenario sampling. `DiagnosticSessionResult.cs`
   owns the explicit scenario context/result/state handoffs and final summary
   DTO surface, with
   `DiagnosticSessionRunner.cs` owning post-sampling completion ordering,
   fault-drain delegation, and background task completion. Scenario catalog,
   initial scenario setup, optional scenario
   startup, cleanup mutation ownership, post-cleanup recording checks,
   post-run snapshot fetches, command send/failure plumbing, and result
   construction are extracted; avoid further runner splits unless a new
   responsibility boundary is clearly larger than the existing focused owners,
   and otherwise pivot to the next large owner. The
   reflective runner behavior tests are already split by scenario, so keep new
   runner coverage in the focused owner file that matches the behavior. Keep
   JSON summary shape unchanged.

2. Reduce custom regression harness size.

   `tests/Sussudio.Tests/HarnessCore.cs` keeps the legacy runner entry point and
   no-op `RunAllChecksAsync` compatibility shim. Keep the executable runner as
   the offline `dotnet exec` validation shim until the
   repo deliberately retires that workflow; new checks should live in focused
   xUnit files or focused partial contract files. MCP tool
   surface tests are now split into command-routing, diagnostic-session tool,
   diagnostic-session ownership, diagnostic-session result ownership,
   diagnostic-session builder result bands, diagnostic-session Flashback,
   diagnostic-session runner, diagnostic-session infrastructure xUnit execution,
   diagnostic-session result-surface xUnit execution, diagnostic-session
   command/run-context xUnit execution, diagnostic-session scenario execution
   xUnit execution, diagnostic-session Flashback xUnit execution,
   diagnostic-session core xUnit execution, diagnostic-session runner-behavior
   xUnit execution, performance,
   window/preview, window/preview
   probes, and helper partial files. Flashback
   tests are also split by buffer, encoder, exporter, exporter cleanup,
   playback, decoder, and support owners. Capture
   session coordinator tests
   are split into API/contracts, queue behavior, Flashback behavior,
   transition policy, ownership, and harness-helper owners. MainViewModel
   automation tests are split into surface, diagnostics refresh, diagnostics projection,
   runtime-safety, and Flashback cleanup owners. The diagnostics-refresh
   snapshot-projection test is now a compact integration wiring smoke; detailed
   projection source-shape contracts live in the focused
   `MainViewModel.Automation.DiagnosticsProjection.*.Tests.cs` files, with
   capture diagnostics projection ownership split across command/settings,
   format/transport/HDR, source/cadence, MJPEG, recording, system, preview, and
   Flashback owners. MainViewModel capture tests are split into preview startup,
   Flashback export, Flashback routing,
   Flashback backend, and Flashback frame-rate/lifecycle owners. Continue with
   low-risk contract groups first. Snapshot-model contract tests are split by
   CaptureDiagnostics, CaptureHealth, and source-signal telemetry model owner.
   Recording queue tests are split into overload policy, LibAv sink, WASAPI,
   and capture fan-out/backend owners. These recording pipeline ownership
   checks now execute through
   `tests/Sussudio.Tests/XUnit.RecordingContractsTests.cs` after their removal
   from the legacy harness catalog. Recording model execution checks also run
   through `tests/Sussudio.Tests/XUnit.RecordingContractsTests.cs` after their
   removal from the legacy harness catalog. D3D preview renderer tests are split
   into geometry, cadence, diagnostics-contract, source-ownership marker plus
   RenderPipeline/RuntimeCapture owners, device-lost, and frame-flow owners.
   Automation tool contract tests are split into
   protocol, catalog/manifest, reliability-gates, and snapshot formatter
   owners. Capture configuration model tests have consolidated xUnit coverage
   for options/settings/encoder support, recording pipeline contracts, and
   Flashback DTO contracts. Pooled-frame
   tests are split into lease lifecycle, MJPEG jitter policy, MJPEG jitter
   queue behavior, and queued lease release owners. MainWindow shell ownership
   tests are split into chrome, startup, preview runtime, and window lifecycle
   owners. MainViewModel service-namespace source ownership is split into
   device-audio, runtime, and device/capture owners behind the original
   orchestrator. MainViewModel dependency-composition ownership now keeps
   capture/device controller dependency-context assertions in a focused
   capture-device owner, and source-telemetry, runtime lifecycle/event-ingress,
   and disposal controller dependency-context assertions in a focused runtime
   owner instead of the root composition catch-all. Flashback buffer segment
   tests are split between validation, accounting, disposal/recovery, and
   segment lookup/list projection coverage. Preview startup session/reinit
   harness coverage is split between source ownership, session controller,
   reinit transition controller, and pending Flashback-cycle wait owners.
   `tests/Sussudio.Tests/XUnit.PresentationPreviewContractsTests.cs`
   owns xUnit execution for the preview-startup source-shape ownership,
   controller behavior, signal/failure-text, and ordering checks after their
   removal from the legacy presentation-preview capture catalog.
   It also owns xUnit execution for the capture preview-lifecycle/audio-fallback
   checks after their removal from the legacy presentation-preview capture
   catalog.
   Preview startup ordering coverage is split between lifecycle-event
   ownership, device-discovery ordering, reveal priming, and stop audio-ramp
   owners. Startup ordering xUnit execution also lives in
   `XUnit.PresentationPreviewContractsTests.cs`, and the legacy catalog
   hook is removed.
   MainViewModel automation recording-transition coverage is split
   between shared transition-gate routing, failure propagation, emergency stop,
   bitrate sampling, and recording-settings/Flashback-cycle owners.
   Diagnostics refresh core ownership is split behind a small orchestrator into
   evaluation, runtime/HDR, and snapshot-projection owners.
   MainWindow window-lifecycle coverage separates close-protection behavior
   from close lifecycle and shutdown cleanup ownership.

3. Continue converting MainWindow partial concerns into controllers.

   `FullScreen`, automation `Screenshot`, MainWindow UI dispatching, preview
   runtime snapshot dispatch/sampling, audio meter rendering, preview startup,
   Flashback playback/export presentation, and stats overlay/row/snapshot
   projection are extracted behind named controllers or builders. The
   Flashback XAML-facing adapter family is now folded into
   `MainWindow.xaml.cs` so command, polling, playhead, scrub, settings,
   timeline, and presentation wrappers stay with MainWindow construction and
   controller initialization order while behavior remains in named controllers.
   The preview-startup XAML-facing adapter family is now consolidated in
   `MainWindow.Composition.cs` with the preview transition
   adapter so session, signal, watchdog, fade, button, and transition callback
   surfaces can be audited together.
   The preview-transition XAML-facing adapter family is now consolidated in
   `MainWindow.Composition.cs` so audio fade, button action,
   delayed fade-in, startup overlay, animation, and reinit callback surfaces can
   be audited from one adapter file.
   `MainWindow.xaml.cs` now owns construction, startup event wiring, and the controller initialization
   list grouped into shell, Flashback, presentation, preview, recording,
   launch/status, preview action, audio, capture, and output phases so the
   composition root stays navigable as new controllers appear.
   Start the next UI cleanup from remaining broad adapters not already covered
   by controller ownership tests. Keep XAML bindings stable.

4. Move MainViewModel feature state behind a facade.

   Preserve the root `MainViewModel` public surface while introducing feature
   view models or adapters for capture selection, recording, audio, Flashback,
   diagnostics, and automation. `MainViewModel.cs` owns the default
   service graph for the root compatibility view model, which gives the next
   facade slices a small construction seam without changing XAML bindings or
   automation contracts. The live audio/microphone meter callback state now
   lives with audio state in `MainViewModel.AudioState.cs`; keep future meter
   behavior there instead of growing the root facade file. Audio ramp trace
   state, bounded ring-buffer storage, snapshot projection, trace session
   start/complete, trace-point capture, sampler loop, delayed sampler
   shutdown, and preview-volume transition mechanics live in
   `Sussudio/ViewModels/PreviewAudioTransitionControllers.cs`, with
   `MainViewModel.AudioState.cs` kept as the automation-facing adapter and
   trace/preview-volume controller wiring owner;
   preview-volume save/override, ramp adapter methods, preview monitoring
   coordinator sequencing, audio-preview property handlers, audio capture
   property handlers, custom audio-input property handlers, retargeting, and
   preview-monitoring ramp handoff now live in `MainViewModel.AudioState.cs`.
   Microphone observable state, endpoint volume synchronization, persistence,
   and property-change routing now live in `MainViewModel.AudioState.cs`;
   device-native audio request lifetime,
   selected-device refresh, mode request scheduling, shared debounce CTS fields,
   cancellation cleanup, graph-built context ports, analog-gain request
   scheduling, UI/XU debounce, and flash-persist debounce now live in
   `Sussudio/Controllers/ViewModel/MainViewModelDeviceAudioRequestController.cs`;
   device-native
   audio-control support probing, readback, pending saved-state reconciliation,
   mode switching, failure readback, shared audio-control guards, and analog
   gain writes now live with audio/microphone UI state in
   `MainViewModel.AudioState.cs`. UI-facing state is
   split by owner: `MainViewModel.cs` owns shared shell/status/live-info flags,
   native window handle state, UI collection replacement, and non-preview
   coordination gates, `MainViewModel.cs`
   owns preview lifecycle compatibility entry points, preview-sink handoff,
   preview lifecycle flags, preview reinitialize coordination, and preview
   request events, `MainViewModel.cs` owns capture-selection
   state, option collections, HDR capture/runtime presentation state, and
   source signal/source-telemetry presentation state, and `MainViewModel.AudioState.cs` owns audio/microphone,
   device-native audio/XU UI state, and audio-preview property-change routing,
   while `MainViewModel.FlashbackState.cs` owns Flashback timeline/export
   state plus buffer, bitrate, playback-state, in/out marker, and gap-from-live
   UI projection. Keep `MainViewModel.cs` focused on the public compatibility-facade
   shell, construction seam, dependency assignment, collaborator fields,
   controller graph handoff, startup lifecycle kick-off, and small bridge methods.
   `Sussudio/Controllers/ViewModel/MainViewModelControllerGraph.cs`
   owns controller graph construction order plus UI-dispatch, device-audio,
   device-refresh, capture-settings automation, source telemetry, runtime
   event-ingress, recording, preview lifecycle/reinitialize, capture option
   rebuild, device-format probe, runtime lifecycle, and disposal graph ports.
   `MainViewModel.cs` continues to own service construction. Audio
   capture property handlers now live in
   `MainViewModel.AudioState.cs`, audio-preview property
   handlers live in `MainViewModel.AudioState.cs`, microphone monitor/device
   selection handlers also live in `MainViewModel.AudioState.cs`,
   capture-mode property handlers live in `MainViewModel.CaptureSelection.cs`. Shared
   view-model UI dispatcher enqueue/invoke policy now lives in
   `Sussudio/Controllers/UiDispatchControllers.cs`.
   The UI dispatch graph-port contract for dispatcher access, disposal state,
   logging, exception logging, and status text projection lives with
   `Sussudio/Controllers/UiDispatchControllers.cs`, while
   `MainViewModel.cs` keeps the stable private adapter names and
   preview event fan-out beside the controller graph handoff;
   periodic timer refresh orchestration and initial
   source-telemetry/HDR/live-info/timer/disk-space bootstrap through
   graph-built context ports now live in
   `Sussudio/Controllers/ViewModel/MainViewModelLifecycleController.cs`,
   The runtime lifecycle graph-port contract for timer creation, runtime
   snapshot sampling, telemetry bootstrap, live-info/HDR projection, recording
   stats refresh, Flashback bitrate refresh, disk-space refresh, watcher
   disposal, runtime event handling through graph-built context ports, and the
   runtime lifecycle graph-port contract live in
   `Sussudio/Controllers/ViewModel/MainViewModelLifecycleController.cs`,
   including system-resume preview rebind handling, audio-device-invalidated rebind
   scheduling through the preview lifecycle owner, capture status/error fan-out,
   capture pre-cleanup renderer stop fan-out, frame-captured callbacks, the
   graph-port contract, and event subscription/unsubscription ordering,
   output drive free-space assignment now lives in
   `MainViewModel.cs`, while output drive probing,
   fallback, formatting, and suppressed-warning logging now live in
   `ViewModelBuilders.cs`. Recording size/bitrate label
   assignment, recording-state reset reactions, and bounded byte-sample
   smoothing shared by recording and Flashback bitrate presentation also live in
   `MainViewModel.cs`, and
   capture presentation adapters now live in
   `MainViewModel.cs`: live-capture info projection from
   runtime snapshots, audio-preview activity, live resolution/frame-rate/pixel-format
   assignment, preview-stop live-info reset, HDR runtime state/readiness
   projection, target-summary property application, and auto-resolution display
   text; live-signal label formatting now lives in
   `Sussudio/ViewModels/ViewModelBuilders.cs`. Capture
   settings projection from UI/runtime state is sampled by the capture-state
   owner in `MainViewModel.cs` and projected by
   `Sussudio/ViewModels/CaptureSettingsProjectionBuilder.cs`, which owns final
   `CaptureSettings` assembly, audio/microphone device application, pure
   projection policy, and input DTOs:
   selected-option seeding, auto-resolved effective FPS, runtime/source rational
   overrides, rational/decimal fallbacks, requested pixel format, and MJPEG
   decode forcing.
   `MainViewModel.cs` keeps the stable compatibility facade entry
   points for device initialization, preview start/stop, selected-device apply,
   and preview reinitialization. Preview lifecycle
   implementation now lives in the top-level
   `Sussudio/Controllers/ViewModel/MainViewModelPreviewLifecycleController.cs`:
   device initialization, preview start/stop, selected-device apply, and the
   reinitialize facade. The preview lifecycle graph-port contract now lives
   with that controller for preview state/events, capture/session operations,
   source telemetry refresh, UI dispatch, audio-preview activity, and
   preview-volume ramp-down.
   Sibling ViewModel controllers receive that preview
   lifecycle owner directly from `MainViewModelControllerGraph` instead of
   routing controller-to-controller calls back through the root facade.
   `Sussudio/Controllers/ViewModel/MainViewModelPreviewLifecycleController.cs`
   is a top-level `Sussudio.Controllers` owner for debounced reinitialization, restart-cancellation state,
   Flashback-cycle wait-before-reinit, renderer-stop handoff, teardown restart,
   and gate release.
   It also owns the graph-built reinitialization port contract for selected
   device/format state, generation coalescing, pending Flashback-cycle waits,
   debounce/timeout policy, renderer notifications, restart cancellation, and
   reinit gate access.
   Output folder display plus browse/open-recordings button workflows now live in
   `Sussudio/Controllers/Recording/RecordingControlsControllers.cs`.
   Recording facade entry points, including the direct emergency-stop
   coordinator bridge, now live in `MainViewModel.cs`, while
   recording toggle serialization,
   desired-state routing, graceful stop, transition gating, and in-flight
   transition wait/error propagation now live in the top-level
   `Sussudio/Controllers/ViewModel/MainViewModelRecordingTransitionController.cs`,
   along with the graph-port contract for UI dispatch, recording/session state,
   capture settings construction, coordinator start/stop calls, recording timer
   state, status/count presentation updates, concrete start/stop execution,
   failure/cancellation state repair, and direct use of the preview lifecycle
   owner for recording startup initialization.
   Recording option selections, output path, counters, and transition flags also
   live in `MainViewModel.cs`. Bounded teardown, dispose timeout policy,
   watcher disposal, coordinator cleanup/dispose, and capture-service
   async-dispose fallback through graph-built context ports now live in
   `Sussudio/Controllers/ViewModel/MainViewModelLifecycleController.cs`.
   The disposal graph-port contract for one-shot disposal entry, teardown
   cancellations, runtime stop, coordinator cleanup/dispose, and capture-service
   async/sync disposal fallback lives with that controller.
   `MainViewModel.cs` remains the public refresh/dispose adapter and active
   Flashback export cancellation owner. Automation-facing command entry points,
   capture runtime, health, recording snapshot projection, source/preview
   probes, and preview frame capture now live in
   `MainViewModel.cs`; automation-facing view-model runtime snapshot UI-thread capture now lives in
   `MainViewModel.cs`; pure view-model runtime snapshot DTO
   construction lives in `ViewModelBuilders.cs`, with executable builder,
   source telemetry, and live-signal text coverage in
   `tests/Sussudio.Tests/ViewModelBuilders.Tests.cs`;
   automation options UI-thread snapshot capture now lives in
   `MainViewModel.cs`; pure selected-control-state DTO
   construction lives in `ViewModelBuilders.cs`.
   `tests/Sussudio.Tests/XUnit.AutomationContractsTests.cs`
   owns xUnit execution for the diagnostics-loop polling check after its removal
   from the legacy presentation-preview capture catalog.
   Buffer, bitrate, playback-state, in/out marker, gap-from-live UI projection,
   read-only Flashback playback snapshot access, read-only segment projection
   for UI, CLI, and MCP callers, rejection status projection, playback, scrub,
   nudge, in/out marker command routing, and automation-facing Flashback
   playback action dispatch now live in `MainViewModel.FlashbackState.cs`.
   Flashback UI export commands, save-picker flow, active-export guard,
   user-facing export result/status handling, shared export operation
   lifecycle, progress handoff, stale-result classification,
   current-operation checks, CTS cancellation/disposal cleanup, and
   automation-facing export execution with linked cancellation and dispatcher
   cleanup now live in `MainViewModel.FlashbackState.cs`.
   Capture-device selection,
   effective resolution helpers, frame-rate selection reactions, and
   auto-selection entry points now live in `MainViewModel.CaptureSelection.cs`.
   `MainViewModel.CaptureSelection.cs` keeps the resolution, frame-rate,
   selected-format, and video-format rebuild compatibility adapters, while
   frame-rate option rebuilding and observable collection mutation through graph-built context ports live in
   `Sussudio/Controllers/ViewModel/MainViewModelCaptureModeOptionRebuildController.cs`. Pure
   frame-rate option choice, including pending SDR bucket preference,
   Source-rate nearest match with timing-family tie-break, generic auto fallback,
   and previous/manual selection fallback, now lives in
   `Sussudio/ViewModels/FrameRateTimingPolicy.cs`. The ownership checks for
   frame-rate source filtering, automatic selection, always-on capture options,
   timing-policy placement, automatic-selection behavior, and pure timing-policy
   behavior checks live together in
   `MainViewModel.Capture.SelectionPolicy.ResolutionFrameRate.Tests.cs`.
   `tests/Sussudio.Tests/XUnit.PresentationPreviewContractsTests.cs`
   owns xUnit execution for those frame-rate selection/timing checks after
   their removal from the legacy presentation-preview capture catalog.
   Shared frame-rate selection reset,
   resolved automatic frame-rate application, disabled frame-rate reason
   projection, and capture-mode reset flags live in
   `MainViewModel.cs`. Source-rate filtering now assumes
   capture options are always visible in
   `Sussudio/ViewModels/FrameRateTimingPolicy.cs`, while deferred rebuild
   behavior, duplicate-reinit suppression, and the active capture-mode automation
   gate live in
   `MainViewModel.CaptureSelection.cs`. Pure frame-rate timing family,
   timing-variant projection, rational parsing, friendly/exact frame-rate
   matching, and preferred-format ranking now live in
   `Sussudio/ViewModels/FrameRateTimingPolicy.cs`, while
   `Sussudio/Controllers/ViewModel/MainViewModelCaptureModeOptionRebuildController.cs`
   owns the stateful resolver over resolution capabilities, runtime snapshots,
   source telemetry, selected formats, UI selection state, and its graph-built
   context ports;
   the root `MainViewModel.cs` keeps the public capture-device refresh
   compatibility facade, while the top-level
   `Sussudio/Controllers/ViewModel/MainViewModelDeviceDiscoveryControllers.cs`
   owns startup refresh orchestration: requesting the combined discovery result,
   applying audio-device startup selection, replacing the capture-device collection,
   starting background format probes, restoring saved capture-device selection,
   and directly auto-starting preview through the preview lifecycle owner.
   It also owns the device-refresh graph-port contract for discovery, startup
   audio selection, device collection mutation, background format probes,
   selection restore, and scan status projection. The shallow `MainViewModel.DeviceManagement.cs`
   partial was retired rather than preserving a sub-100-line facade. Selected
   capture-device reactions, capability projection, source telemetry reset, and
   device-native audio-control refresh handoff live in `MainViewModel.CaptureSelection.cs`. Capture-mode property-change
   hooks live in `MainViewModel.CaptureSelection.cs`; startup audio-list
   and watcher-driven audio endpoint refresh adaptation are folded into
   `MainViewModel.AudioState.cs` beside the audio collections and saved-device
   restore state. Pure audio-device filtering and
   previous/saved/default audio and microphone selection fallback policy now
   lives in `Sussudio/ViewModels/ViewModelSelectionPolicies.cs`. Pure
   recording codec filtering, selected-codec fallback policy, string-to-model
   format/quality parsing, and custom bitrate clamp policy now live in
   `Sussudio/ViewModels/CaptureSettingsProjectionBuilder.cs`, while startup
   FFmpeg capability probes and observable recording-format option mutation through graph-built context ports live
   with source telemetry readiness in the top-level
   `Sussudio/Controllers/ViewModel/MainViewModelCaptureReadinessControllers.cs`. `MainViewModel.CaptureSelection.cs`
   keeps selected-format and video-format rebuild compatibility adapters, while
   `Sussudio/Controllers/ViewModel/MainViewModelCaptureModeOptionRebuildController.cs`
   is now a top-level `Sussudio.Controllers` owner for selected-format
   assignment, pixel-format option collection mutation, capture-format
   request shaping, and the capture-mode option rebuild graph-port contract for
   option collections, stable Source/Auto sentinel values, source telemetry,
   selection state, automatic retarget flags, format-change suppression, and
   projected status text.
   `Sussudio/ViewModels/ViewModelSelectionPolicies.cs` owns the pure
   selected-format and mode-tuple video-format filtering policy.
   `MainViewModel.CaptureSelection.cs` owns HDR toggle side effects:
   recording-time revert/status, mode option rebuilds, immediate reinitialize
   scheduling, and settings persistence.
    Late-arriving device format probe reconciliation, collection mutation,
    selected-device capability refresh, enqueue/failure logging, and retarget
    handoff live in the top-level
    `Sussudio/Controllers/ViewModel/MainViewModelDeviceDiscoveryControllers.cs`;
    its graph-port contract now lives with that controller. UI-side late-probe
    retarget application, session mismatch checks, active-capture restore, and
    the retarget applier graph-port contract also live there, while
    pure late-probe retarget decisions live in
    `Sussudio/ViewModels/ViewModelSelectionPolicies.cs`.
    `tests/Sussudio.Tests/XUnit.PresentationPreviewContractsTests.cs`
    owns xUnit execution for the late device-format probe retarget ownership,
    behavior, and application checks after their removal from the legacy
    presentation-preview capture catalog.
    The presentation-preview ownership tests for this capture selection policy
    area are split across the
    `tests/Sussudio.Tests/MainViewModel.Capture.SelectionPolicy.*.cs` family so
    frame-rate, resolution, mode-selection, late-probe, recording-format,
    capture-settings projection, and runtime-flag assertions stay near their
    matching policy owners.
    `tests/Sussudio.Tests/XUnit.PresentationPreviewContractsTests.cs`
    owns xUnit execution for the mode-selection, capture-format,
    recording-settings selection, and capture-settings projection checks after
    their removal from the legacy presentation-preview capture catalog.
    Resolution option rebuild callers stay stable through the
    `MainViewModel.CaptureSelection.cs` adapter. Resolution option
    rebuild ownership now lives in
    `Sussudio/Controllers/ViewModel/MainViewModelCaptureModeOptionRebuildController.cs`:
    automatic resolution dropdown option construction inside the promoted
    top-level capture option rebuild controller family, automatic
    resolution-selection adaptation, auto-resolution state refresh, and
    resolution dropdown mutation through graph-built context ports. Effective Source resolution state and
    state-backed delegates to the pure selection policy live in
    `MainViewModel.CaptureSelection.cs`.
   Automatic resolution ranking and source-aware frame-rate selection now
    live in `Sussudio/ViewModels/ViewModelSelectionPolicies.cs`; auto-resolution
    display text used by status and telemetry presentation lives in
    `MainViewModel.cs`.
   `tests/Sussudio.Tests/XUnit.PresentationPreviewContractsTests.cs`
   owns xUnit execution for the resolution-selection ownership and behavior
   checks after their removal from the legacy presentation-preview capture catalog.
   Pure resolution selection policy now lives in
   `Sussudio/ViewModels/ViewModelSelectionPolicies.cs`: source-aware matching,
   HDR retarget/support-hint selection, SDR auto/fallback selection, parsing,
   frame-rate support checks, nearest-resolution ranking, and the request/result
   records stay together with the broader pure ViewModel selection-policy owner.
   Resolution and frame-rate selection harness coverage lives in
   `MainViewModel.Capture.SelectionPolicy.ResolutionFrameRate.Tests.cs`, which
   owns source-shape placement assertions plus HDR, SDR, auto-capture,
   source-filter, automatic frame-rate, and timing-policy behavior contracts.
   State-backed delegates for callers that still live across the partial family
   stay in `MainViewModel.CaptureSelection.cs`, while dropdown rebuild,
   collection mutation, and property notifications route through the top-level
    `MainViewModelCaptureModeOptionRebuildController.cs`.
   Source telemetry summary, telemetry age, and target-summary display text
   formatting now live in `Sussudio/ViewModels/ViewModelBuilders.cs`;
   HDR runtime state/readiness projection and target-summary property
   application live in `MainViewModel.cs`; keep snapshot
   application, source telemetry ingress behavior, telemetry age refresh,
   enum-string caching, source-aware auto-retargeting, and source telemetry
   graph-port contract in
   `Sussudio/Controllers/ViewModel/MainViewModelCaptureReadinessControllers.cs`.
   Settings initialization, simple persistence reactions, the impure settings
   load/save adapter, persisted-settings validation, clamping, deferred-selection
   projection, save DTO projection, load/save projection contracts, validated
   load-plan application order, feature-specific state assignment, and deferred
   device/audio/microphone selection staging stay in
   `MainViewModel.SettingsPersistence.cs`;
   active Flashback reactions to recording format,
   encoder quality/preset/split/bitrate, buffer duration, and GPU decode now
   live in `MainViewModel.FlashbackState.cs`.
   Pure analog audio gain percent/XU-byte curve mapping now lives in
   `MainViewModel.AudioState.cs` with the shared audio-control guards;
   device-native audio request lifetime, including mode property-change adapters, UI enqueue lifetime,
   shared debounce CTS fields, graph-port context contract, cancellation
   cleanup, gain property-change adapters, XU debounce, and flash-persist
   debounce, stays in
   `Sussudio/Controllers/ViewModel/MainViewModelDeviceAudioRequestController.cs`;
   async native-XU
   device audio-control refresh/readback, mode switching, failure readback,
   shared audio-control guards, analog gain XU writes, and settings
   persistence stay with `MainViewModel.AudioState.cs`. Use
   the supported native-XU switch/gain command surface rather than the legacy
   AT input-source fallback path.
   UI-only automation mutators for settings visibility, Flashback timeline
   visibility, stats dock/section visibility, and frame-time overlay display now
   live in `MainViewModel.cs`; the public show-all capture options
   command remains accepted as a dispatcher-level compatibility no-op.
   Automation command entry points for app audio enablement, audio-preview
   enablement, preview-volume clamp/persist, device-native mode/gain
   application, and microphone enablement with recording-time
   refusal/idempotent handling now live in `MainViewModel.cs`.
   Automation preview enable/disable idempotence, pending-reinit cancellation,
   and preview start/stop routing now live in
   top-level `MainViewModelPreviewLifecycleController.cs` plus graph-built
   `MainViewModelPreviewLifecycleController.cs` reinitialize context ports, with the stable
   `MainViewModel.cs` compatibility facade preserving the automation surface.
   Automation HDR and true-HDR preview recording-time guard enforcement and HDR
   availability checks now live in `MainViewModel.CaptureSelection.cs`
   beside HDR mode change side effects.
   Automation Flashback enable/restart routing through the capture session
   coordinator now lives in `MainViewModel.FlashbackState.cs` alongside
   buffer/GPU setting reactions.
   Automation device refresh, capture-device selection, audio-input selection,
   and custom audio-input enablement now live in
   `MainViewModel.cs`.
   Recording format, encoder, and output-path automation entry points now stay
   in the `MainViewModel.cs` compatibility facade,
   while UI-thread mutations, HDR compatibility enforcement, Flashback cycle
   suppression, coordinator side effects, custom bitrate clamping, encoder
   preset, and output-path directory creation live in
   the top-level
   `Sussudio/Controllers/ViewModel/MainViewModelSettingsAutomationControllers.cs`.
   It also owns the recording-settings automation graph-port contract for UI
   dispatch, option collections, suppression flags, selected encoder/output
   state, recording-format coordinator updates, and Flashback encoder setting
   cycles.
   The automation recording desired-state bridge enters through
   `MainViewModel.cs` and is serialized by
   the top-level
   `Sussudio/Controllers/ViewModel/MainViewModelRecordingTransitionController.cs`,
   with graph-built context ports and start/stop execution in the same owner.
   The emergency recording-stop bridge also enters through
   `MainViewModel.cs` but routes directly to
   `CaptureSessionCoordinator.StopRecordingForEmergencyAsync`
   so it keeps bypassing UI-thread dispatch and normal transition gates.
   Capture resolution, frame-rate, video-format, and MJPEG decoder worker-count
   automation entry points now stay in the
   `MainViewModel.cs` compatibility facade, while
   UI-thread mutations, validation, MJPEG decoder clamping, and active
   capture-mode reinitialization routing live in the top-level
   `Sussudio/Controllers/ViewModel/MainViewModelSettingsAutomationControllers.cs`.
   It also owns the capture-settings automation graph-port contract for option
   collections, selected capture-mode state, preview reinitialization checks,
   UI-thread dispatch, and format-change suppression.
   Startup FFmpeg capability probes for recording formats and split-encode modes
   plus observable recording-format option rebuilds now live with source telemetry readiness in the top-level
   `Sussudio/Controllers/ViewModel/MainViewModelCaptureReadinessControllers.cs`.
   `Sussudio/ViewModels/MainViewModel.cs` keeps recording-runtime
   counters, disk-space assignment, and the stable recording-capability facade
   methods used by settings initialization and HDR mode-change rebuild callers.
   It also owns the recording-capability graph-port contract for default encoder
   names, observable recording/split-encode option collections, selected
   recording format state, HDR/status state, FFmpeg-missing state, and UI
   dispatch.
   The old `MainViewModel.Automation.cs` catch-all has been retired.

5. Extract capture resource owners behind the transition policy.

   The policy is now the legality/steady-state owner. Recent capture slices
   kept it authoritative while introducing smaller owners for the audio graph,
   Flashback backend resources, active recording backend resources, and active
   video pipeline resources.
   `FlashbackBackendResources.cs` now owns the preview backend resource set,
   install/take/clear state, recovery-preserve flag storage and policy,
   recording-finalize handoff, producer attach/detach request shapes, and video,
   audio, and microphone feed wiring. It also owns startup construction,
   install/playback initialization, startup failure rollback cleanup,
   sink-only buffer-cycle orchestration, purge/finalize decisions,
   full-rebuild fallback outcomes, playback disposal, old-sink stop/dispose,
   replacement sink startup/playback restore, failed replacement cleanup,
   backend teardown, and artifact cleanup mechanics. CaptureService callers now use that aggregate directly
   instead of private root resource shim properties. Keep later Flashback backend
   mechanics in the matching focused owner before inventing another small owner;
   `CaptureService.FlashbackControls.cs` stays the transition coordinator for
   AV1 probing, readiness waiting, cleanup handoff, and preview backend disposal
   request construction.
   `CapturePipelineResources.cs` now owns active capture resource holders:
   preview audio graph resources, recording backend resources, and video
   pipeline resources. Recording start, finalization, rollback, snapshot,
   cleanup, preview lifecycle, and audio preview paths use those aggregates
   directly instead of routing through private root shim properties. Keep later
   capture resource mechanics there unless the behavior needs a larger, proven
   boundary.

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

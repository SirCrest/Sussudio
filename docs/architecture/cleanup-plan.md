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

App shell startup and exception policy now live in named partial owners without
changing runtime behavior. `Sussudio/App.xaml.cs` stays the constructor/resource
root for XAML initialization, global exception handler hookup, system logging,
and FFmpeg runtime initialization. `Sussudio/App.ExceptionPolicy.cs` owns
recoverable exception classification, WinUI/AppDomain unhandled exception
handlers, and emergency recording finalization. `Sussudio/App.LaunchLifecycle.cs`
owns the single-instance mutex guard, startup identity logging, and MainWindow
activation.

Logger diagnostics are split from the nonblocking writer without changing the
public static logging surface. `Sussudio/Logger.cs` stays the hot-path writer
owner for initialization, rotation, bounded channel enqueueing, dropped-message
fallback, direct file writes, and `LogEvent`. `Sussudio/Logger.Diagnostics.cs`
owns system evidence collection, exception formatting, structured snapshot JSON
routing through `LoggingJsonContext`, and fatal breadcrumbs. Keep WMI/system
evidence and JSON payload routing out of the writer root so the saturation and
shutdown behavior remains easy to audit.

Runtime path resolution is split from the public cached path API without
changing repo/temp/log path behavior. `Sussudio/RuntimePaths.cs` owns the public
`GetRepo*` API and lazy cache fields. `Sussudio/RuntimePaths.Resolution.cs` owns
repo-root marker discovery, latest-build parent fallback, log-root override and
fallback policy, guarded directory creation, and trace fallback diagnostics.

FFmpeg runtime location is split from capability probing without changing the
public locator surface. `Sussudio/Services/Runtime/FfmpegRuntimeLocator.cs` owns
app-local, Program Files, and PATH-based runtime/tool resolution.
`FfmpegRuntimeLocator.Probes.cs` owns cached encoder and split-encode capability
probes, including the bounded `ProcessSupervisor` calls and timeout policy used
by startup recording capability checks.

Automation contracts have been extracted into `Sussudio.Automation.Contracts/`.
This removes the old linked-source arrangement where app and tools compiled
protocol/catalog files, pipe-client handoff DTOs, response parsing, synthetic
error shaping, unknown-command policy, and pipe security policy from
`tools/Common`.

Changed ownership:

- `AutomationCommandKind.cs`
- `AutomationCommandCatalog.cs`
- `AutomationCommandCatalog.Entries.cs`
- `AutomationCommandCatalog.Entries.Core.cs`
- `AutomationCommandCatalog.Entries.Capture.cs`
- `AutomationCommandCatalog.Entries.Ui.cs`
- `AutomationCommandCatalog.Entries.Flashback.cs`
- `AutomationCommandCatalog.Entries.Verification.cs`
- `AutomationCommandCatalog.Manifest.cs`
- `AutomationCommandCatalog.PathValidation.cs`
- `AutomationPipeClientModels.cs`
- `AutomationPipeProtocol.cs`
- `AutomationResponseState.cs`
- `AutomationSyntheticErrorResponse.cs`
- `AutomationUnknownCommandHandling.cs`
- `AutomationPipeSecurityPolicy.cs`

Diagnostic session scenario names, CLI help text, MCP-compatible description
text, normalization, setup requirements, export verification metadata, ordering,
and scenario-level plan lookup now live together in
`tools/Common/DiagnosticSessionScenarioCatalog.cs`; the runner still owns
execution flow and summary writing.

Automation diagnostics now have named partial owners instead of one large hub
body. `AutomationDiagnosticsHub.cs` is the compact field/constructor owner.
`AutomationDiagnosticsHub.Counters.RealtimePreview.cs` owns preview jitter and
D3D recent-counter baselines and delta updates.
`AutomationDiagnosticsHub.Counters.Mjpeg.cs` owns MJPEG recent-counter baselines
and delta updates.
`AutomationDiagnosticsHub.Counters.FlashbackRecording.cs` owns Flashback
recording recent-counter baselines and delta updates.
`AutomationDiagnosticsHub.Snapshots.cs` owns snapshot refresh and read-only
snapshot access. `AutomationDiagnosticsHub.SnapshotProjection.cs`
owns the `BuildAutomationSnapshot` shell and dispatch into projection
composition/flattening. `AutomationDiagnosticsHub.SnapshotProjection.Composition.cs`
owns projection-set composition from runtime/view-model snapshots and diagnostic
classifiers. `AutomationDiagnosticsHub.SnapshotProjection.Flattening.cs`
owns the final `AutomationSnapshot` DTO initializer that flattens the named
projection records into the automation wire snapshot. This final flattener is
intentionally a single `init`-property map: do not split it by adding mutable
setters or shallow fragment records unless a deliberate snapshot construction
pattern is introduced first. `AutomationDiagnosticsHub.SnapshotState.cs`
owns stateful snapshot bookkeeping for audio mute suspicion and recording file
growth tracking. `AutomationDiagnosticsHub.Timeline.cs`
owns performance-timeline ring reads and append mechanics.
`AutomationDiagnosticsHub.TimelineProjection.cs` owns final
`AutomationSnapshot` to `PerformanceTimelineEntry` assignment.
`AutomationDiagnosticsHub.TimelineProjection.Core.cs` owns timestamp,
observed capture/preview FPS, encoder video queue depth/drop, and capture
cadence timeline projection.
`AutomationDiagnosticsHub.TimelineProjection.Preview.cs` owns preview cadence,
visual cadence, MJPEG packet/jitter, D3D preview, and preview-pacing timeline
projection.
`AutomationDiagnosticsHub.TimelineProjection.FlashbackPlayback.cs` owns the
Flashback playback snapshot-to-performance-timeline projection group.
`AutomationDiagnosticsHub.TimelineProjection.FlashbackExport.cs` owns the
Flashback export progress and force-rotate fallback timeline projection group.
`AutomationDiagnosticsHub.TimelineProjection.System.cs` owns process, memory,
GC, thread-pool, and pipeline-latency timeline projection.
`AutomationDiagnosticsHub.SnapshotProjection.SnapshotStatus.cs` owns timestamp,
view-model lifecycle/audio flags, verification-in-progress, session state, and
status-text projection consumed by the automation snapshot DTO.
`AutomationDiagnosticsHub.SnapshotProjection.Flattening.SnapshotStatus.cs`
owns final snapshot status projection-to-`AutomationSnapshot` field flattening.
`AutomationDiagnosticsHub.SnapshotProjection.SnapshotEvaluation.cs` owns
performance score, diagnostic lane, preview pacing classifier, and performance
threshold projection consumed by the automation snapshot DTO.
`AutomationDiagnosticsHub.SnapshotProjection.Flattening.SnapshotEvaluation.cs`
owns final snapshot evaluation projection-to-`AutomationSnapshot` field
flattening.
`AutomationDiagnosticsHub.SnapshotProjection.Audio.cs` owns audio/ingest
projection routing and groups audio signal, capture-ingest, and WASAPI
projection inputs consumed by the automation snapshot DTO.
`AutomationDiagnosticsHub.SnapshotProjection.Audio.Signal.cs` owns view-model
audio peak/clipping and detected audio-signal projection inputs.
`AutomationDiagnosticsHub.SnapshotProjection.AudioDrops.cs` owns audio drop
counter projection and derived real-time/file-writer drop totals.
`AutomationDiagnosticsHub.SnapshotProjection.Flattening.AudioAndIngest.cs`
owns final audio and ingest projection-to-`AutomationSnapshot` field
flattening and routes grouped signal, ingest, source-reader, WASAPI capture,
and WASAPI playback flattening modules.
`AutomationDiagnosticsHub.SnapshotProjection.Flattening.AudioAndIngest.Signal.cs`
owns final audio peak, clipping, signal-present, and muted-suspected field
flattening.
`AutomationDiagnosticsHub.SnapshotProjection.Flattening.AudioAndIngest.CaptureIngest.cs`
owns final audio/video reader active state and ingest counter field flattening.
`AutomationDiagnosticsHub.SnapshotProjection.Flattening.AudioAndIngest.SourceReader.cs`
owns final source-reader delivery, drop, outstanding-read, and channel-depth
field flattening.
`AutomationDiagnosticsHub.SnapshotProjection.Flattening.AudioAndIngest.WasapiCapture.cs`
owns final WASAPI capture callback, gap, glitch, timestamp, silence, and
audio-level event field flattening.
`AutomationDiagnosticsHub.SnapshotProjection.Flattening.AudioAndIngest.WasapiPlayback.cs`
owns final WASAPI playback render, queue, buffered-duration, endpoint-duration,
stream-latency, and last-render field flattening.
`AutomationDiagnosticsHub.SnapshotProjection.Flattening.AudioDrops.cs` owns
final audio-drop projection-to-`AutomationSnapshot` field flattening.
`AutomationDiagnosticsHub.SnapshotProjection.CaptureIngest.cs` owns capture
audio/video reader, source-reader, and ingest counter projection consumed by the
automation snapshot DTO.
`WasapiAudioCapture.Initialization.cs` owns WASAPI endpoint binding, mix-format
negotiation, AudioClient startup, capture event/client acquisition, and
initialization-time metric resets. `WasapiAudioCapture.Conversion.cs` owns
WASAPI sample decode, f32le 48 kHz stereo conversion, resampling, and pooled
converted packet buffers. Keep state, start/stop/dispose, and capture-thread
lifecycle in `WasapiAudioCapture.cs`.
`WasapiAudioCapture.Fanout.cs` owns recording/Flashback/playback attachment
points, converted-packet dispatch from the capture thread, and hot writer
task-completion enforcement.
`WasapiAudioCapture.Diagnostics.cs` owns audio-level event projection, callback
interval, discontinuity, timestamp-error, glitch, and audio-level event counters.
`WasapiAudioPlayback.Initialization.cs` owns WASAPI render endpoint binding,
format validation, AudioClient startup, render event/client acquisition, and
initialization-time metric resets. `WasapiAudioPlayback.cs` keeps playback
state, start/stop/pause/resume/flush/dispose lifecycle, and render-thread
startup. `WasapiAudioPlayback.Volume.cs` owns render-side volume ramps and
output-level telemetry used by audio ramp traces.
`WasapiAudioPlayback.Queue.cs` owns playback chunk queue state, pooled-sample
ingress, queue depth/frame accounting, buffered-duration projection, and pooled
chunk returns.
`WasapiAudioPlayback.RenderThread.cs` owns the WASAPI render-thread loop,
pause/resume execution, resume prebuffer wait, endpoint buffer writes, render
buffer filling, and render-side PTS advancement.
`WasapiComInterop.CoreAudio.Contracts.cs` owns WASAPI/Core Audio enums,
audio-format records, WAVEFORMAT structs, PROPERTYKEY, PropVariant lifetime
handling, and Core Audio device, collection, property-store, and notification
COM interfaces.
`WasapiComInterop.AudioClient.Contracts.cs` owns AudioClient, capture/render
client, and endpoint-volume COM interfaces. `WasapiComInterop.cs` owns native
constants/P/Invokes and shared COM release/failure helpers.
`WasapiComInterop.Formats.cs` owns float-stereo format allocation, WASAPI format
parsing, and sample-type classification. `WasapiComInterop.DeviceClients.cs`
owns device enumerator activation, endpoint volume helpers, AudioClient
activation, and AudioClient3 shared-stream initialization.
`NativeXuAudioControlService.Profiles.cs` owns 4K X selector-3 byte indexes,
HDMI/Analog reference payloads, gain-profile placeholders, hex parsing, and
payload decode/confidence helpers. `NativeXuAudioControlService.Transport.cs`
owns selector-3 payload read/update workflow and verification against mutated
control bytes. `NativeXuAudioControlService.RawTransport.cs` owns dev-specific
candidate enumeration, raw XU GET/SET, raw payload normalization/rehydration,
and retrying the shared native transport gate from `NativeXuDeviceSupport.cs`.
`NativeXuAudioControlService.cs` owns the public service flow and snapshot DTOs.
`AutomationDiagnosticsHub.SnapshotProjection.WasapiAudio.cs` owns WASAPI
capture/playback callback, queue, gap, glitch, and latency projection consumed
by the automation snapshot DTO.
`AutomationDiagnosticsHub.SnapshotProjection.CaptureCommands.cs` owns capture
session command queue counters, latency, last-command, and last-error
projection inputs consumed by the automation snapshot DTO.
`AutomationDiagnosticsHub.SnapshotProjection.Flattening.CaptureCommands.cs`
owns final capture-command projection-to-`AutomationSnapshot` field flattening.
`AutomationDiagnosticsHub.SnapshotProjection.CaptureFormat.cs` owns
capture-format projection routing and groups requested, HDR-request, actual,
negotiated, reader-observation, and encoder format input modules consumed by
the automation snapshot DTO.
`AutomationDiagnosticsHub.SnapshotProjection.CaptureFormat.Requested.cs` owns
requested capture format, quality, HDR toggle, and audio-toggle projection
inputs.
`AutomationDiagnosticsHub.SnapshotProjection.CaptureFormat.HdrRequest.cs` owns
HDR activation and auto-downgrade projection inputs.
`AutomationDiagnosticsHub.SnapshotProjection.CaptureFormat.Actual.cs` owns
actual capture dimensions and frame-rate projection inputs.
`AutomationDiagnosticsHub.SnapshotProjection.CaptureFormat.Negotiated.cs` owns
negotiated capture dimensions, frame-rate, pixel format, and media subtype
token projection inputs.
`AutomationDiagnosticsHub.SnapshotProjection.CaptureFormat.ReaderObservation.cs`
owns source-reader subtype and observed pixel/surface format projection inputs.
`AutomationDiagnosticsHub.SnapshotProjection.CaptureFormat.Encoder.cs` owns
encoder format, codec, profile, and ten-bit confirmation projection inputs.
`AutomationDiagnosticsHub.SnapshotProjection.Flattening.CaptureFormat.cs`
owns final capture-format projection-to-`AutomationSnapshot` field flattening.
`AutomationDiagnosticsHub.SnapshotProjection.Flattening.CaptureFormat.Requested.cs`
owns final requested capture format, quality, HDR toggle, and audio-toggle field
flattening.
`AutomationDiagnosticsHub.SnapshotProjection.Flattening.CaptureFormat.HdrRequest.cs`
owns final HDR activation and auto-downgrade field flattening.
`AutomationDiagnosticsHub.SnapshotProjection.Flattening.CaptureFormat.Actual.cs`
owns final actual capture dimensions and frame-rate field flattening.
`AutomationDiagnosticsHub.SnapshotProjection.Flattening.CaptureFormat.Negotiated.cs`
owns final negotiated capture dimensions, frame-rate, pixel format, and media
subtype token field flattening.
`AutomationDiagnosticsHub.SnapshotProjection.Flattening.CaptureFormat.ReaderObservation.cs`
owns final source-reader subtype and observed pixel/surface format field
flattening.
`AutomationDiagnosticsHub.SnapshotProjection.Flattening.CaptureFormat.Encoder.cs`
owns final encoder format, codec, profile, and ten-bit confirmation field
flattening.
`AutomationDiagnosticsHub.SnapshotProjection.CaptureTransport.cs` owns capture
memory preference, requested/negotiated video subtype, and frame-ledger
projection consumed by the automation snapshot DTO.
`AutomationDiagnosticsHub.SnapshotProjection.Flattening.CaptureTransport.cs`
owns final capture transport projection-to-`AutomationSnapshot` field
flattening.
`AutomationDiagnosticsHub.SnapshotProjection.CaptureCadence.cs` owns source
capture cadence projection inputs consumed by the automation snapshot DTO.
`AutomationDiagnosticsHub.SnapshotProjection.Flattening.CaptureCadence.cs`
owns final source capture cadence projection-to-`AutomationSnapshot` field
flattening.
`AutomationDiagnosticsHub.SnapshotProjection.VisualCadence.cs` owns preview
visual cadence and center-crop visual cadence projection inputs consumed by the
automation snapshot DTO.
`AutomationDiagnosticsHub.SnapshotProjection.Flattening.VisualCadence.cs` owns
final visual cadence and center-crop visual cadence projection-to-`AutomationSnapshot`
field flattening.
`AutomationDiagnosticsHub.SnapshotProjection.Mjpeg.cs` owns CPU MJPEG totals,
compressed queue, and failure projection inputs consumed by the automation
snapshot DTO.
`AutomationDiagnosticsHub.SnapshotProjection.Flattening.Mjpeg.cs` owns final
CPU MJPEG totals, compressed queue, and failure projection-to-`AutomationSnapshot`
field flattening.
`AutomationDiagnosticsHub.SnapshotProjection.MjpegTiming.cs` owns CPU MJPEG
decode, interop-copy, callback, reorder, pipeline timing, decoder count, and
per-decoder projection inputs consumed by the automation snapshot DTO.
`AutomationDiagnosticsHub.SnapshotProjection.Flattening.MjpegTiming.cs` owns
final CPU MJPEG timing projection-to-`AutomationSnapshot` field flattening.
`AutomationDiagnosticsHub.SnapshotProjection.MjpegPreviewJitter.cs` owns MJPEG
preview jitter projection routing consumed by the automation snapshot DTO.
Its focused owners split queue counters, timing samples, adaptive drop/depth
counters, and last scheduler event projection inputs:
`AutomationDiagnosticsHub.SnapshotProjection.MjpegPreviewJitter.Queue.cs`,
`AutomationDiagnosticsHub.SnapshotProjection.MjpegPreviewJitter.Timing.cs`,
`AutomationDiagnosticsHub.SnapshotProjection.MjpegPreviewJitter.Adaptive.cs`,
and `AutomationDiagnosticsHub.SnapshotProjection.MjpegPreviewJitter.Events.cs`.
`AutomationDiagnosticsHub.SnapshotProjection.Flattening.MjpegPreviewJitter.cs`
owns final MJPEG preview jitter projection-to-`AutomationSnapshot` routing.
Its focused flattening owners mirror queue counters, timing samples, adaptive
drop/depth counters, and last scheduler event fields:
`AutomationDiagnosticsHub.SnapshotProjection.Flattening.MjpegPreviewJitter.Queue.cs`,
`AutomationDiagnosticsHub.SnapshotProjection.Flattening.MjpegPreviewJitter.Timing.cs`,
`AutomationDiagnosticsHub.SnapshotProjection.Flattening.MjpegPreviewJitter.Adaptive.cs`,
and `AutomationDiagnosticsHub.SnapshotProjection.Flattening.MjpegPreviewJitter.Events.cs`.
`AutomationDiagnosticsHub.SnapshotProjection.MjpegPacketHash.cs` owns MJPEG
packet duplicate-run / unique-frame projection inputs consumed by the automation
snapshot DTO.
`AutomationDiagnosticsHub.SnapshotProjection.Flattening.MjpegPacketHash.cs`
owns final MJPEG packet duplicate-run / unique-frame projection-to-`AutomationSnapshot`
field flattening.
`AutomationDiagnosticsHub.SnapshotProjection.FlashbackExport.cs` owns active
Flashback export progress, failure, force-rotate fallback, and last-result
projection consumed by the automation snapshot DTO.
`AutomationDiagnosticsHub.SnapshotProjection.Flattening.FlashbackExport.cs`
owns final Flashback export projection-to-`AutomationSnapshot` field
flattening.
`AutomationDiagnosticsHub.SnapshotProjection.FlashbackRecording.cs` owns
Flashback recording failure, cleanup, force-rotate, and focused projection
routing consumed by the automation snapshot DTO.
`AutomationDiagnosticsHub.SnapshotProjection.FlashbackRecording.StartupCache.cs`
owns Flashback temp-drive and startup-cache policy projection consumed by the
automation snapshot DTO.
`AutomationDiagnosticsHub.SnapshotProjection.FlashbackRecordingQueues.cs` owns
Flashback video, GPU, and audio queue/backpressure projection consumed by the
automation snapshot DTO.
`AutomationDiagnosticsHub.SnapshotProjection.FlashbackRecording.Runtime.cs`
owns Flashback active recording output/runtime projection consumed by the
automation snapshot DTO.
`AutomationDiagnosticsHub.SnapshotProjection.FlashbackRecording.Backend.cs`
owns Flashback backend settings drift, export-verification, and codec downgrade
projection consumed by the automation snapshot DTO.
`AutomationDiagnosticsHub.SnapshotProjection.FlashbackRecording.Encoder.cs`
owns Flashback encoder identity, bitrate, dimension, and frame-rate projection
consumed by the automation snapshot DTO.
`AutomationDiagnosticsHub.SnapshotProjection.Flattening.FlashbackRecording.cs`
owns final Flashback recording projection-to-`AutomationSnapshot` field
flattening plus failure, cleanup, and force-rotate fields.
`AutomationDiagnosticsHub.SnapshotProjection.Flattening.FlashbackRecording.StartupCache.cs`
owns flattened Flashback startup-cache storage fields.
`AutomationDiagnosticsHub.SnapshotProjection.Flattening.FlashbackRecording.Queues.cs`
owns flattened Flashback video, GPU, and audio queue/backpressure fields.
`AutomationDiagnosticsHub.SnapshotProjection.Flattening.FlashbackRecording.Runtime.cs`
owns flattened active recording output/runtime fields.
`AutomationDiagnosticsHub.SnapshotProjection.Flattening.FlashbackRecording.Backend.cs`
owns flattened backend settings drift, export-verification, and codec downgrade
fields.
`AutomationDiagnosticsHub.SnapshotProjection.Flattening.FlashbackRecording.Encoder.cs`
owns flattened encoder identity, bitrate, dimension, and frame-rate fields.
`AutomationDiagnosticsHub.SnapshotProjection.FlashbackPlayback.cs` owns
Flashback playback state/frame summary and routing to the playback leaf
projections consumed by the automation snapshot DTO.
`AutomationDiagnosticsHub.SnapshotProjection.FlashbackPlayback.AudioMaster.cs`
owns audio-master delay/fallback projection,
`AutomationDiagnosticsHub.SnapshotProjection.FlashbackPlayback.Timing.cs` owns
playback event, cadence, PTS-cadence, and A/V drift projection,
`AutomationDiagnosticsHub.SnapshotProjection.FlashbackPlayback.Decode.cs` owns
seek-cap/decode timing projection, and
`AutomationDiagnosticsHub.SnapshotProjection.FlashbackPlayback.Commands.cs`
owns playback command queue projection.
`AutomationDiagnosticsHub.SnapshotProjection.Flattening.FlashbackPlayback.cs`
owns final Flashback playback projection-to-`AutomationSnapshot` field
flattening plus root playback-state fields.
`AutomationDiagnosticsHub.SnapshotProjection.Flattening.FlashbackPlayback.AudioMaster.cs`
owns flattened Flashback playback audio-master fallback fields.
`AutomationDiagnosticsHub.SnapshotProjection.Flattening.FlashbackPlayback.Timing.cs`
owns flattened Flashback playback event, cadence, PTS-cadence, and A/V drift
fields.
`AutomationDiagnosticsHub.SnapshotProjection.Flattening.FlashbackPlayback.Decode.cs`
owns flattened Flashback playback decode-cap and decode-timing fields.
`AutomationDiagnosticsHub.SnapshotProjection.Flattening.FlashbackPlayback.Commands.cs`
owns flattened Flashback playback command queue fields.
`AutomationDiagnosticsHub.SnapshotProjection.PreviewD3D.cs` owns D3D preview
swap-chain and renderer-state projection plus composition of D3D leaf
projections consumed by the automation snapshot DTO.
`AutomationDiagnosticsHub.SnapshotProjection.Flattening.PreviewD3D.cs` owns
the final D3D projection-to-`AutomationSnapshot` field flattening consumed by
the root snapshot initializer plus root renderer-state fields.
`AutomationDiagnosticsHub.SnapshotProjection.Flattening.PreviewD3D.CpuTiming.cs`
owns flattened D3D CPU upload/render/present/total-frame timing fields.
`AutomationDiagnosticsHub.SnapshotProjection.Flattening.PreviewD3D.LatencyAndStats.cs`
owns flattened D3D pipeline-latency, waitable-frame-latency, and DXGI frame-stat
fields.
`AutomationDiagnosticsHub.SnapshotProjection.Flattening.PreviewD3D.FrameFlow.cs`
owns flattened submitted/rendered/dropped frame ownership and recent slow-frame
fields.
`AutomationDiagnosticsHub.SnapshotProjection.PreviewD3DCpuTiming.cs` owns D3D
CPU upload/render/present/total-frame timing consumed by the automation snapshot
DTO.
`AutomationDiagnosticsHub.SnapshotProjection.PreviewD3D.PipelineLatency.cs`
owns D3D pipeline-latency projection consumed by the automation snapshot DTO.
`AutomationDiagnosticsHub.SnapshotProjection.PreviewD3D.FrameFlow.cs` owns
submitted/rendered/dropped frame ownership and recent slow-frame projection.
`AutomationDiagnosticsHub.SnapshotProjection.PreviewD3D.FrameLatencyWait.cs`
owns waitable frame-latency projection.
`AutomationDiagnosticsHub.SnapshotProjection.PreviewD3D.FrameStats.cs` owns
DXGI frame-statistics projection, including recent missed-refresh and stats
failure deltas.
`AutomationDiagnosticsHub.SnapshotProjection.PreviewRuntime.cs` owns preview
runtime projection routing and groups frame, cadence, surface, startup,
GPU-playback, and color input modules consumed by the automation snapshot DTO.
`AutomationDiagnosticsHub.SnapshotProjection.PreviewRuntime.Frame.cs` owns
preview frame counters and estimated pipeline latency projection inputs.
`AutomationDiagnosticsHub.SnapshotProjection.PreviewRuntime.Cadence.cs` owns
preview display-cadence projection inputs.
`AutomationDiagnosticsHub.SnapshotProjection.PreviewRuntime.Surface.cs` owns
preview surface visibility and renderer-attachment projection inputs.
`AutomationDiagnosticsHub.SnapshotProjection.PreviewRuntime.Startup.cs` owns
preview startup/readiness and renderer mode projection inputs.
`AutomationDiagnosticsHub.SnapshotProjection.PreviewRuntime.GpuPlayback.cs` owns
preview GPU playback state and position projection inputs.
`AutomationDiagnosticsHub.SnapshotProjection.PreviewRuntime.Color.cs` owns
preview HDR, tone-map, color-context, and adapter metadata projection inputs.
`AutomationDiagnosticsHub.SnapshotProjection.Flattening.PreviewRuntime.cs`
owns final preview runtime projection-to-`AutomationSnapshot` field flattening.
`AutomationDiagnosticsHub.SnapshotProjection.Flattening.PreviewRuntime.Frame.cs`
owns final preview frame-counter and estimated-pipeline-latency field
flattening.
`AutomationDiagnosticsHub.SnapshotProjection.Flattening.PreviewRuntime.Cadence.cs`
owns final preview display-cadence field flattening.
`AutomationDiagnosticsHub.SnapshotProjection.Flattening.PreviewRuntime.Surface.cs`
owns final preview surface visibility and renderer-attachment field flattening.
`AutomationDiagnosticsHub.SnapshotProjection.Flattening.PreviewRuntime.Startup.cs`
owns final preview startup/readiness field flattening.
`AutomationDiagnosticsHub.SnapshotProjection.Flattening.PreviewRuntime.GpuPlayback.cs`
owns final preview GPU playback state and position field flattening.
`AutomationDiagnosticsHub.SnapshotProjection.Flattening.PreviewRuntime.Color.cs`
owns final preview HDR, tone-map, color-context, and adapter metadata field
flattening.
`AutomationDiagnosticsHub.SnapshotProjection.ProcessResources.cs` owns process
memory, CPU, GC, and thread-pool projection consumed by the automation snapshot
DTO.
`AutomationDiagnosticsHub.SnapshotProjection.Flattening.ProcessResources.cs`
owns final process resource projection-to-`AutomationSnapshot` field
flattening.
`AutomationDiagnosticsHub.SnapshotProjection.AvSync.cs` owns live A/V sync
drift and encoder correction projection consumed by the automation snapshot DTO.
`AutomationDiagnosticsHub.SnapshotProjection.Flattening.AvSync.cs` owns final
A/V sync projection-to-`AutomationSnapshot` field flattening.
`AutomationDiagnosticsHub.SnapshotProjection.RecordingIntegrity.cs` owns
recording-integrity projection routing consumed by the automation snapshot
DTO. Its focused owners split status/reason, video-frame counters,
queue/backpressure, audio integrity, and A/V sync projection inputs:
`AutomationDiagnosticsHub.SnapshotProjection.RecordingIntegrity.Summary.cs`,
`AutomationDiagnosticsHub.SnapshotProjection.RecordingIntegrity.Video.cs`,
`AutomationDiagnosticsHub.SnapshotProjection.RecordingIntegrity.Backpressure.cs`,
`AutomationDiagnosticsHub.SnapshotProjection.RecordingIntegrity.Audio.cs`, and
`AutomationDiagnosticsHub.SnapshotProjection.RecordingIntegrity.AvSync.cs`.
`AutomationDiagnosticsHub.SnapshotProjection.Flattening.RecordingIntegrity.cs`
owns final recording-integrity projection-to-`AutomationSnapshot` routing.
Its focused flattening owners mirror status/reason, video-frame counters,
queue/backpressure, audio integrity, and A/V sync fields:
`AutomationDiagnosticsHub.SnapshotProjection.Flattening.RecordingIntegrity.Summary.cs`,
`AutomationDiagnosticsHub.SnapshotProjection.Flattening.RecordingIntegrity.Video.cs`,
`AutomationDiagnosticsHub.SnapshotProjection.Flattening.RecordingIntegrity.Backpressure.cs`,
`AutomationDiagnosticsHub.SnapshotProjection.Flattening.RecordingIntegrity.Audio.cs`, and
`AutomationDiagnosticsHub.SnapshotProjection.Flattening.RecordingIntegrity.AvSync.cs`.
`AutomationDiagnosticsHub.SnapshotProjection.RecordingPipeline.cs` owns
recording-pipeline projection routing and groups encoder, ingest, video-queue,
and GPU/CUDA health input modules consumed by the automation snapshot DTO.
`AutomationDiagnosticsHub.SnapshotProjection.RecordingPipeline.Encoder.cs` owns
encoder queue age/count/failure health input projection.
`AutomationDiagnosticsHub.SnapshotProjection.RecordingPipeline.Ingest.cs` owns
conversion, ffmpeg, and video ingest queue health input projection.
`AutomationDiagnosticsHub.SnapshotProjection.RecordingPipeline.VideoQueue.cs`
owns recording video queue latency, backpressure, and encoder-output health
input projection.
`AutomationDiagnosticsHub.SnapshotProjection.RecordingPipeline.HardwareQueues.cs`
owns recording GPU and CUDA queue health input projection.
`AutomationDiagnosticsHub.SnapshotProjection.Flattening.RecordingPipeline.cs`
owns final recording-pipeline projection-to-`AutomationSnapshot` field
flattening and routes the grouped encoder, ingest, video-queue, and GPU/CUDA
queue flattening modules.
`AutomationDiagnosticsHub.SnapshotProjection.Flattening.RecordingPipeline.Encoder.cs`
owns final encoder age/count/failure field flattening.
`AutomationDiagnosticsHub.SnapshotProjection.Flattening.RecordingPipeline.Ingest.cs`
owns final conversion, ffmpeg, and video ingest queue field flattening.
`AutomationDiagnosticsHub.SnapshotProjection.Flattening.RecordingPipeline.VideoQueue.cs`
owns final recording video queue latency, backpressure, and encoder-output field
flattening.
`AutomationDiagnosticsHub.SnapshotProjection.Flattening.RecordingPipeline.HardwareQueues.cs`
owns final recording GPU and CUDA queue field flattening.
`AutomationDiagnosticsHub.SnapshotProjection.RecordingOutput.cs` owns recording
backend/audio-path/mux-result projection, UI output text, accumulated recording
bytes, file-growth state, last finalized output metadata, and last verification
result projection consumed by the automation snapshot DTO.
`AutomationDiagnosticsHub.SnapshotProjection.Flattening.RecordingOutput.cs`
owns final recording backend and output projection-to-`AutomationSnapshot` field
flattening.
`AutomationDiagnosticsHub.SnapshotProjection.SourceSignal.cs` owns detected
source frame-rate fallback, source dimensions/HDR, and raw source signal
metadata projection consumed by the automation snapshot DTO.
`AutomationDiagnosticsHub.SnapshotProjection.SourceTelemetry.cs` owns source
telemetry fallback policy, age calculation, and source-target summary inputs
consumed by the automation snapshot DTO.
`AutomationDiagnosticsHub.SnapshotProjection.Flattening.Source.cs` owns final
source projection flattening and routes final source signal and source
telemetry fields through focused flattening modules.
`AutomationDiagnosticsHub.SnapshotProjection.Flattening.Source.Signal.cs`
owns final source dimensions, frame-rate, HDR, video/audio format, firmware,
input, USB, HDCP, and raw timing field flattening.
`AutomationDiagnosticsHub.SnapshotProjection.Flattening.Source.Telemetry.cs`
owns final source telemetry availability, confidence, detail, age, backend,
suppression, circuit-state, summary, and target-summary field flattening.
`AutomationDiagnosticsHub.SnapshotProjection.UserSettings.cs` owns selected
device, selected capture/recording options, preview volume, and stats
visibility projection consumed by the automation snapshot DTO.
`AutomationDiagnosticsHub.SnapshotProjection.Flattening.Settings.cs` owns final
selected device, selected capture/recording options, preview volume, and stats
visibility projection-to-`AutomationSnapshot` field flattening.
`AutomationDiagnosticsHub.SnapshotProjection.HdrPipeline.cs` owns HDR
availability/request state, runtime/readiness fallback, HDR warmup/downgrade,
pipeline parity, telemetry-alignment, and HDR truth verdict projection consumed
by the automation snapshot DTO.
`AutomationDiagnosticsHub.SnapshotProjection.Flattening.HdrPipeline.cs` owns
final HDR pipeline projection-to-`AutomationSnapshot` field flattening.
`AutomationDiagnosticsHub.Alerts.cs` owns alert rule evaluation and active-alert
transitions. `AutomationDiagnosticsHub.SignalAlerts.cs` owns signal alert
orchestration. `AutomationDiagnosticsHub.SignalAlerts.Preview.cs` owns preview
blank, stall, startup, cadence, and display 1% low signal alert rules.
`AutomationDiagnosticsHub.SignalAlerts.Capture.cs` owns capture cadence drop and
1% low signal alert rules.
`AutomationDiagnosticsHub.SignalAlerts.AudioRecording.cs` owns audio muted
signal and recording output-growth alert rules.
`AutomationDiagnosticsHub.Alerts.cs` also routes Flashback recording and
playback alert groups. `AutomationDiagnosticsHub.FlashbackRecordingAlerts.cs`
owns Flashback recording alert orchestration and shared condition setup.
`AutomationDiagnosticsHub.FlashbackRecordingAlerts.Export.cs` owns export
progress and force-rotation gap alerts.
`AutomationDiagnosticsHub.FlashbackRecordingAlerts.Storage.cs` owns temp-cache
pressure alerts.
`AutomationDiagnosticsHub.FlashbackRecordingAlerts.Encoder.cs` owns encoder
failure alerts.
`AutomationDiagnosticsHub.FlashbackRecordingAlerts.Degradation.cs` owns
recording path degradation alerts.
`AutomationDiagnosticsHub.FlashbackPlaybackAlerts.cs` owns Flashback playback
alert orchestration.
`AutomationDiagnosticsHub.FlashbackPlaybackAlerts.Commands.cs` owns playback
command queue and command failure alerts.
`AutomationDiagnosticsHub.FlashbackPlaybackPerformanceAlerts.cs` owns Flashback
playback performance alert orchestration.
`AutomationDiagnosticsHub.FlashbackPlaybackPerformanceAlerts.Audio.cs` owns
audio-master fallback and audio-queue backlog alerts.
`AutomationDiagnosticsHub.FlashbackPlaybackPerformanceAlerts.Cadence.cs` owns
playback target-rate, present-cadence, slow-playback, and frametime alert
rules.
`AutomationDiagnosticsHub.FlashbackPlaybackPerformanceAlerts.Submit.cs` owns
frame-submission failure alerts.
`AutomationDiagnosticsHub.DiagnosticEvents.cs` owns diagnostics event
publication, event throttling, Flashback export completion events, and recent
event storage.
`AutomationDiagnosticsHub.Verification.cs` owns manual recording/file
verification entry points and event publication for explicit verification.
`AutomationDiagnosticsHub.Verification.Auto.cs` owns last-verification snapshot
state, post-recording auto-verification gating, and background scheduling.
`AutomationDiagnosticsHub.Verification.Profile.cs` owns flashback-export
verification profile shaping.
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
`AutomationDiagnosticsHub.DiagnosticEvaluationLanes.Realtime.Source.cs` owns
source cadence and source-signal diagnostic lane text formatting.
`AutomationDiagnosticsHub.DiagnosticEvaluationLanes.Realtime.Mjpeg.cs` owns
MJPEG decode diagnostic lane text formatting.
`AutomationDiagnosticsHub.DiagnosticEvaluationLanes.Realtime.Preview.cs` owns
preview scheduler, renderer, present/display, and visual-cadence diagnostic
lane text formatting.
`AutomationDiagnosticsHub.DiagnosticEvaluationLanes.Realtime.Recording.cs`
owns recording and audio diagnostic lane text formatting.
`AutomationDiagnosticsHub.DiagnosticEvaluationLanes.Flashback.Recording.cs`
owns Flashback recording diagnostic lane text formatting.
`AutomationDiagnosticsHub.DiagnosticEvaluationLanes.Flashback.Export.cs` owns
Flashback export and temp-cache diagnostic lane text formatting.
`AutomationDiagnosticsHub.DiagnosticEvaluationLanes.Flashback.Playback.cs` owns
Flashback playback command and playback performance diagnostic lane text
formatting.
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
`PreviewPacingSlowStageClassifier.cs` owns classification ordering,
`PreviewPacingSlowStageClassifier.Lanes.SourceVisual.cs` owns source capture
and visual duplicate/low-motion predicates/evidence,
`PreviewPacingSlowStageClassifier.Lanes.DecodeJitter.cs` owns MJPEG decode and
preview jitter scheduler predicates/evidence,
`PreviewPacingSlowStageClassifier.Lanes.Render.cs` owns compositor-miss and
renderer-submit predicates/evidence, and
`PreviewPacingSlowStageClassifier.D3D.cs` owns D3D stage dominance policy.
`AutomationDiagnosticsHub.ProcessMetrics.cs` owns process CPU, memory, GC, and
thread-pool sampling.
`AutomationDiagnosticsHub.Verification.cs` owns manual recording/file
verification commands and explicit verification events.
`AutomationDiagnosticsHub.Verification.Auto.cs` owns automatic post-recording
verification scheduling and recording-start verification reset.
`AutomationDiagnosticsHub.Verification.Profile.cs` owns verification-profile
adaptation.

Automation command dispatch now keeps the root router focused on the command
envelope: manifest revision checks, auth/readiness gates, port-typed
trivial-handler dispatch, and error shaping. `AutomationCommandDispatcher.CustomCommands.cs`
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
trivial/UI handler tables through matching port targets. The dispatcher consumes the readiness port for
device-ready gating and the device-selection/snapshot-query ports for device
commands, the audio port for device-audio/microphone commands, and the
capture-settings plus preview-recording ports for MJPEG decoder, output path,
recording, preview, and related one-field commands. Visual probe commands
consume the probe port while window screenshots remain on the window-control
surface. Stats-section UI commands consume the UI port, audio-ramp trace reads
consume the snapshot-query port, and Flashback commands consume the Flashback
port.
`AutomationDiagnosticsHub` consumes the snapshot-query port for read-only
runtime, health, and recording verification snapshots. Its constructor should
take `IAutomationSnapshotQueryPort` directly instead of advertising the full
aggregate automation surface.
`AutomationCommandDispatcher.UiSettingsCommands.cs` owns UI/settings
automation command application, including show-all capture options, preview
volume, stats visibility, settings visibility, frame-time overlay visibility,
Flashback timeline visibility, and stats-section expand/collapse response text.
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
capture and pipeline command table. Named partials own support responsibilities:
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
accept-loop behavior in `NamedPipeAutomationServer.Lifecycle.cs`,
per-connection safety/disposal and request-session handoff in
`NamedPipeAutomationServer.Connections.cs`, per-request JSON framing, client PID
logging, dispatch timeouts, late-dispatch observation, and response writing in
`NamedPipeAutomationServer.ConnectionSession.cs`, Windows pipe security/PInvoke
in `NamedPipeAutomationServer.Security.cs`, and error/timeout responses plus
fallback tracing in `NamedPipeAutomationServer.Responses.cs`.

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
operator-facing help section text plus catalog-backed help lines.
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

The `tools/ssctl/Formatters.*.cs` partial family is only the projection facade
for console output. Keep app snapshot orchestration and section ordering in `Formatters.Snapshot.cs`,
state/capture-command, audio, legacy performance, and Memory/GC text
in `Formatters.Snapshot.CoreSections.cs`, recording output, backend, integrity,
audio-integrity, and last-finalize text in `Formatters.Snapshot.Recording.cs`,
capture settings and friendly/exact
frame-rate text in `Formatters.Snapshot.CaptureSettings.cs`, capture cadence,
embedded AV-sync drift, and source-signal text in
`Formatters.Snapshot.CaptureCadence.cs`, video-pipeline text in
`Formatters.Snapshot.Runtime.cs`, preview renderer-mode routing/non-D3D
fallback text in `Formatters.Snapshot.Preview.cs`,
diagnostic health/frame-lane text in `Formatters.Snapshot.DiagnosticLanes.cs`,
Flashback snapshot gating/order and encoding subsection order in
`Formatters.Snapshot.Flashback.cs` and
`Formatters.Snapshot.Flashback.Encoding.cs`, Flashback encoder/buffer/cache
text in `Formatters.Snapshot.Flashback.Encoding.Status.cs`, Flashback
queue-latency, backpressure, failure, and GPU queue text in
`Formatters.Snapshot.Flashback.Encoding.Health.cs`, Flashback export progress, result,
throughput, force-rotate fallback, range, output path, and message text in
`Formatters.Snapshot.Flashback.Export.cs`, Flashback playback status and
command text in `Formatters.Snapshot.Flashback.Playback.Commands.cs`,
Flashback playback cadence, decode, frame, stage, and A/V drift text in
`Formatters.Snapshot.Flashback.Playback.Performance.cs`, MJPEG
activation/header/order in `Formatters.Snapshot.Mjpeg.cs`,
decode/copy/callback/per-decoder timing text in
`Formatters.Snapshot.Mjpeg.Decode.cs`, compressed-queue, drop-reason, reorder,
and pipeline timing text in `Formatters.Snapshot.Mjpeg.Pipeline.cs`, MJPEG
preview-jitter queue, latency, ownership, and underflow text in
`Formatters.Snapshot.Mjpeg.PreviewJitter.cs`, D3D preview
renderer routing/header text in `Formatters.Snapshot.PreviewD3D.cs`, D3D CPU
timing, pipeline latency, and frame-latency wait text in
`Formatters.Snapshot.PreviewD3D.Timing.cs`, D3D frame ownership and DXGI
frame-stat text in `Formatters.Snapshot.PreviewD3D.FrameFlow.cs`, delegation to
the shared slow-frame formatter in the D3D root, thread-health section order in
`Formatters.Snapshot.ThreadHealth.cs`, source-reader text in
`Formatters.Snapshot.ThreadHealth.SourceReader.cs`, WASAPI capture text in
`Formatters.Snapshot.ThreadHealth.WasapiCapture.cs`, WASAPI playback text in
`Formatters.Snapshot.ThreadHealth.WasapiPlayback.cs`, diagnostic-event text in
`Formatters.Diagnostics.cs`, capture option/device text in `Formatters.Options.cs`,
performance timeline orchestration in `Formatters.Timeline.cs`, timeline row
projection in `Formatters.Timeline.Rows.cs`, the private row model in
`Formatters.Timeline.Rows.Model.cs`, table output in
`Formatters.Timeline.Rendering.cs`, trend summaries in
`Formatters.Timeline.Summaries.cs`, standalone memory/GC summaries in
`Formatters.Memory.cs`, and shared JSON/result helpers in
`Formatters.Common.cs`.

`tools/Common/AutomationSnapshotFormatter.cs` is now the shared automation
snapshot formatter facade for top-level text flow. State and capture-command
summary text lives in `AutomationSnapshotFormatter.CoreSections.cs`; audio
signal text lives in `AutomationSnapshotFormatter.Audio.cs`; recording output,
backend, integrity, audio-integrity, and last-finalize text lives in
`AutomationSnapshotFormatter.Recording.cs`; legacy performance plus process
CPU, Memory/GC, and thread-pool text lives in
`AutomationSnapshotFormatter.ProcessResources.cs`; capture settings, video
pipeline, diagnostics, and capture cadence text live in
`AutomationSnapshotFormatter.CaptureSettings.cs`,
`AutomationSnapshotFormatter.VideoPipeline.cs`,
`AutomationSnapshotFormatter.Diagnostics.cs`,
`AutomationSnapshotFormatter.CaptureCadence.cs`. Snapshot response-success
detection lives in `AutomationSnapshotFormatter.Response.cs`; tolerant JSON
string/bool accessors live in `AutomationSnapshotFormatter.Values.cs`; numeric
JSON parsing lives in `AutomationSnapshotFormatter.Values.Numeric.cs`; while
byte/number/interval, frame-budget, and tick-age display helpers live in
`AutomationSnapshotFormatter.DisplayValues.cs`; the Flashback gate/header/order
lives in `AutomationSnapshotFormatter.Flashback.cs`; Flashback encoding
subsection order lives in `AutomationSnapshotFormatter.Flashback.Encoding.cs`.
Flashback encoder, buffer, temp-cache, and cleanup text lives in
`AutomationSnapshotFormatter.Flashback.Encoding.Status.cs`, while Flashback
queue-latency, backpressure, failure, and GPU queue text lives in
`AutomationSnapshotFormatter.Flashback.Encoding.Health.cs`. Flashback export progress,
result, throughput, force-rotate fallback, range, output path, and message text
lives in `AutomationSnapshotFormatter.Flashback.Export.cs`. Flashback playback
status and command text lives in
`AutomationSnapshotFormatter.Flashback.Playback.Commands.cs`, while Flashback
playback cadence, decode, frame, stage, and A/V drift text lives in
`AutomationSnapshotFormatter.Flashback.Playback.Performance.cs`. Capture cadence owns the
capture cadence, MJPEG packet fingerprint, and visual cadence rows, while
`AutomationSnapshotFormatter.AvSync.cs` owns AV-sync text and
`AutomationSnapshotFormatter.Source.cs` owns source-signal text emitted from
the cadence tail. MJPEG activation/header/order lives in
`AutomationSnapshotFormatter.MjpegTiming.cs`; decode/copy/callback/per-decoder
timing text lives in `AutomationSnapshotFormatter.MjpegTiming.Decode.cs`;
compressed queue, drop-reason, reorder, and pipeline timing text lives in
`AutomationSnapshotFormatter.MjpegTiming.Pipeline.cs`; MJPEG preview-jitter
queue, latency, ownership, and underflow text lives in
`AutomationSnapshotFormatter.MjpegTiming.PreviewJitter.cs`. Preview routing,
D3D preview text, and thread-health live in the remaining focused formatter partials. The
`AutomationSnapshotFormatter.PreviewD3D.cs` owner keeps D3D header/routing and
output order; `AutomationSnapshotFormatter.PreviewD3D.Timing.cs` owns D3D CPU
timing, pipeline latency, and frame-latency wait text; and
`AutomationSnapshotFormatter.PreviewD3D.FrameFlow.cs` owns D3D frame ownership
and DXGI frame-stat text. Slow-frame diagnostics stay in
`AutomationSnapshotFormatter.PreviewD3D.SlowFrames.cs` because `ssctl` reuses
that formatter directly. `AutomationSnapshotFormatter.ThreadHealth.cs` owns
thread-health section order, while `.ThreadHealth.SourceReader.cs`,
`.ThreadHealth.WasapiCapture.cs`, and `.ThreadHealth.WasapiPlayback.cs` own
their respective text rows. Tests that reason about formatter source use the
shared `RuntimeContractSource` snapshot formatter source-family readers so
ownership checks cover the full partial family from both the legacy harness and
xUnit formatter contracts.

Diagnostic-session MCP surface coverage is split into
`McpToolSurface.DiagnosticSession.Tool.Artifacts.Tests.cs` and
`McpToolSurface.DiagnosticSession.Tool.Failures.Tests.cs` for MCP tool
artifact contracts, `McpToolSurface.DiagnosticSession.Ownership.*.Tests.cs` for
planning, execution, teardown, and reporting helper ownership assertions,
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
Window/preview MCP tool checks for condition wait, window actions, screenshots,
preview-frame capture, preview toggles, and preview/video-source probes now
execute through `tests/Sussudio.Tests/XUnit.McpWindowPreviewToolContractsTests.cs`
after their removal from the legacy harness catalog.
MCP performance/probe checks for PresentMon correlation, performance timeline
formatting/contracts, and frame-pacing verdict policy now execute through
`tests/Sussudio.Tests/XUnit.McpPerformanceToolContractsTests.cs` after their
removal from the legacy harness catalog.
General MCP tool-surface checks for command routing, host/pipe behavior,
verification formatting, Flashback tool routing, and diagnostic-session tool
entries now execute through
`tests/Sussudio.Tests/XUnit.McpToolSurfaceContractsTests.cs` after their removal
from the legacy harness catalog.

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
transition routing, async `IAutomationViewModel` surface plus Flashback/probe
dispatcher routing, diagnostics refresh, diagnostics projection ownership,
dispatch cancellation/timeouts under the relevant async-surface, pipe-server,
coordinator-queue, and dispatcher-readiness owners, audio command guards,
preview lifecycle routing, UI settings, capture-mode/device routing, and
Flashback cleanup ownership partials. Keep new automation tests in the closest
owner file instead of regrowing the root catch-all.
The diagnostics-refresh source-family helper keeps the reader/root runtime
fields in
`tests/Sussudio.Tests/MainViewModel.Automation.DiagnosticsRefresh.SourceFamily.cs`;
grouped diagnostic evaluation, alert, snapshot-projection, and aggregate text
helpers live in focused SourceFamily partials so source-shape assertions do
not regrow one catch-all helper.

`tests/Sussudio.Tests/MainViewModel.Capture.TestHelpers.cs` owns shared
capture-facing MainViewModel source-inspection helpers. Capture regression
coverage is split across the `tests/Sussudio.Tests/MainViewModel.Capture.*.cs`
family, including preview startup, Flashback export locking, Flashback
coordinator/UI routing, Flashback backend lifecycle, capture selection policy,
output path, audio monitoring, reinitialization, and Flashback
frame-rate/enable-disable owner files.

`tests/Sussudio.Tests/XUnit.SnapshotModelsTests.cs` and its `SnapshotModels.*`
partials now own the snapshot-model xUnit contract suite. Snapshot model
coverage is split into CaptureDiagnosticsSnapshot, CaptureHealthSnapshot,
SourceSignalTelemetrySnapshot, SourceTelemetryDetailEntry, and source telemetry
automation projection owner files. AutomationSnapshot CPU MJPEG and
AutomationOptions DTO checks are ported into the same partial family, with
shared reflection/spec helpers kept there. AutomationSnapshot metric-shape
checks are split by DTO surface across CPU/MJPEG, MJPEG preview and packet hash,
preview diagnostics, capture commands, recording, Flashback recording,
Flashback playback, Flashback export, and visual cadence owner files.

`Sussudio/Models/Capture/CaptureHealthSnapshot.Flashback.cs` now owns the
Flashback playback, Flashback encoder/backend, and Flashback export health DTO
properties. Keep source-signal, queue-age, and A/V sync extension fields in the
root `CaptureHealthSnapshot.cs` compatibility partial.
`Sussudio/Models/Capture/CaptureDiagnosticsSnapshot.Mjpeg.cs` owns the base
diagnostics DTO's MJPEG decode, jitter, packet-hash, visual-cadence, and
per-decoder telemetry properties. Keep session state, source telemetry,
recording queue, Flashback queue, and audio-drop fields in the root
`CaptureDiagnosticsSnapshot.cs` compatibility partial until those families are
large enough to justify their own owner.

`tests/Sussudio.Tests/RecordingQueue.Tests.cs` is now the shared recording
queue source-reader helper shell. Capture health snapshot ownership coverage is
split into assembly/sampler, Flashback, and recording/source-telemetry files.
Recording queue coverage is split into queue overload policy, LibAv sink,
WASAPI, and capture fan-out / Flashback backend owner files.

D3D preview renderer coverage is
split into geometry/screenshot helper and preview PNG encoder contracts, cadence contracts, the large
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
harness file has been retired; `tests/Sussudio.Tests/AutomationToolContracts.ProtocolXunit.Tests.cs`
owns automation client timeout policy, advanced command-map alignment,
pipe-failure contracts, tool delegation, script freshness, and response-state
parsing, using `RuntimeContractSource.ReadAutomationPipeClientSource()` for
the shared AutomationPipeClient source family. Automation tool contract coverage is
otherwise split into catalog/manifest/path-policy contracts, reliability-gates
script checks, shared/ssctl snapshot formatter contracts, and tool-probe
contracts. `tests/Sussudio.Tests/XUnit.AutomationCatalogContractsTests.cs` owns
the xUnit execution surface for catalog, manifest, path-policy, and
reliability-gates checks after their removal from the legacy offline harness
catalog. `tests/Sussudio.Tests/XUnit.ToolProbeContractsTests.cs` owns the
xUnit execution surface for the PresentMon parser, ssctl pipe transport, KS
audio-node, and EGAVDS probe checks after their removal from the legacy
offline harness catalog. `tests/Sussudio.Tests/XUnit.NativeToolProbeContractsTests.cs`
owns the RTK I2C unsafe-native-path guard check, and
`tests/Sussudio.Tests/XUnit.ToolModelContractsTests.cs` owns the former legacy
NVML snapshot/CaptureSessionSnapshot tool-model checks; the legacy
`HarnessCheckCatalog.ToolContracts.cs` registration file has been retired.
Shared formatter tests now mirror the formatter partials: the root
snapshot-formatter test owns accessors, invalid-response handling, section
ordering, core section formatting, and the Flashback opt-in gate; Flashback
output, Preview D3D output, and source ownership live in focused
`AutomationToolContracts.SnapshotFormatter.*.Tests.cs` implementation owners,
with `tests/Sussudio.Tests/XUnit.AutomationSnapshotFormatterContractsTests.cs`
owning their xUnit execution surface after removal from the legacy offline
harness catalog. ssctl formatter output smoke checks stay in
`Formatters.Tests.cs`, while `Formatters.SnapshotOwnership.Tests.cs` owns ssctl
formatter source ownership assertions through the shared `RuntimeContractSource`
formatter source-family readers, `Formatters.Timeline.Tests.cs` owns timeline
output contracts, and `tests/Sussudio.Tests/XUnit.SsctlFormatterContractsTests.cs`
owns their xUnit execution surface after removal from the legacy offline
harness catalog.
ssctl command-handler routing coverage now lives in focused
`CommandHandlers.Routing.Control/Flashback/Workflow.Tests.cs` owners for device,
capture controls, recordings, Flashback, window, manifest, observability,
automation-flow, UI visibility, and verification commands, with source ownership
kept separate in `CommandHandlers.SourceOwnership.Tests.cs` and xUnit execution
owned by `tests/Sussudio.Tests/XUnit.SsctlCommandHandlerContractsTests.cs` after
removal from the legacy offline harness catalog. Captured ssctl
`request.command` ID assertions now flow through `AssertSsctlCommandRequest`,
which delegates to the shared golden-table-backed `AssertAutomationCommandId`
helper instead of duplicating numeric IDs in routing tests. Fixed ssctl source
guards also live in `CommandHandlers.SourceOwnership.Tests.cs`; they require
`AutomationCommandKind` enum overloads at routing call sites while leaving
labels and wire IDs catalog-backed, with the dynamic diagnostic-session runner
channel intentionally remaining string-based.
`tests/Sussudio.Tests/ArchitectureDocs.AgentMapOwnershipPaths.Tests.cs` owns
shared implementations for consolidated AGENT_MAP reference resolution,
test-owner code-span coverage, automation consumer checklist coverage,
UI/presentation ownership coverage, CaptureService ownership coverage,
Flashback preview startup AGENT_MAP wording, shared tool automation path
coverage, duplicate tools/Common owner checks, and empty test marker-shell
checks.
`tests/Sussudio.Tests/XUnit.ArchitectureDocsAgentMapOwnershipTests.cs` owns the
xUnit execution surface for those AGENT_MAP ownership checks after their
removal from the legacy offline harness catalog.
`tests/Sussudio.Tests/ArchitectureDocs.ReferenceIntegrity.Tests.cs` owns
literal `ReadRepoFile` source-shape path resolution, cleanup-plan file/folder
reference drift checks, architecture-doc test-family coverage, and the shared
implementations for the xUnit migration inventory guard.
`tests/Sussudio.Tests/XUnit.ArchitectureDocsReferenceIntegrityTests.cs` owns
the xUnit execution surface for those pure architecture-doc reference checks
after their removal from the legacy offline harness catalog.
`tests/Sussudio.Tests/ArchitectureDocs.MarkdownReferenceHelpers.cs` owns shared
Markdown code-span path-token extraction and resolution helpers.
`tests/Sussudio.Tests/ArchitectureDocs.OwnershipFileEnumerators.cs` owns
AGENT_MAP consumer coverage, ownership-file discovery, exact code-span policy,
and xUnit inventory discovery.
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
shared reflection helpers for remaining legacy capture model checks.
`tests/Sussudio.Tests/XUnit.CaptureConfigurationModelsTests.cs` owns shared
reflection helpers for capture configuration xUnit contract checks.
Focused xUnit coverage is split across
`tests/Sussudio.Tests/XUnit.CaptureModeOptionsTests.cs`,
`tests/Sussudio.Tests/XUnit.CaptureSettingsContractsTests.cs`, and
`tests/Sussudio.Tests/XUnit.RecordingConfigurationPolicyTests.cs` so capture
mode options, capture settings/MJPEG HFR/bitrate policy, recording selection,
encoder support, and recording pipeline option checks stay near their
production owners without creating one-fact files.
`tests/Sussudio.Tests/XUnit.FlashbackModelsTests.cs` owns Flashback buffer
option sizing behavior and DTO contracts, with reflection/nullability assertion
helpers in
`tests/Sussudio.Tests/XUnit.FlashbackModels.PropertyAssertions.cs`.
`tests/Sussudio.Tests/XUnit.RecordingModelContractsTests.cs` owns the former
legacy recording-model execution surface for LibAv sink loop/source-ownership
checks, capture runtime failure/runtime-flag checks, and Flashback buffer
manager behavior/source-ownership checks after their removal from the legacy
offline harness catalog.

`tests/Sussudio.Tests/PooledVideoFrame.Tests.cs` now keeps only shared
pooled-frame and jitter-buffer helpers. Pooled-frame coverage is split into
lease lifecycle/fan-out contracts, MJPEG jitter frame-ingress/adaptive policy,
MJPEG jitter queue/drop/reprime behavior, and queued lease release contracts
for D3D, recording, and Flashback paths.
CPU MJPEG pipeline runtime checks now execute through
`tests/Sussudio.Tests/XUnit.MjpegPipelineContractsTests.cs`, keeping pipeline,
cadence, pooled-frame, preview-jitter, and queued lease-release contracts in
xUnit after their removal from the legacy harness catalog.
Flashback encoder sink checks now execute through
`tests/Sussudio.Tests/XUnit.FlashbackEncoderSinkContractsTests.cs`, keeping
frame-rate, codec, counter, queue, force-rotate, packet-drain, startup, and
source-ownership contracts in xUnit after their removal from the legacy harness
catalog.
Flashback playback checks now execute through
`tests/Sussudio.Tests/XUnit.FlashbackPlaybackContractsTests.cs`, keeping
startup, command-queue, source-shape, cadence, submission, reopen,
transition-guard, and metric-reset contracts in xUnit after their removal from
the legacy harness catalog.
Flashback decoder checks now execute through
`tests/Sussudio.Tests/XUnit.FlashbackDecoderContractsTests.cs`, keeping
frame-buffer, source-ownership, state/lifetime, timestamp, audio,
frame-validation, and cancellation contracts in xUnit after their removal from
the legacy harness catalog.
Flashback exporter checks now execute through
`tests/Sussudio.Tests/XUnit.FlashbackExporterContractsTests.cs`, keeping
cleanup, request validation, failure classification, segment, cancellation,
output path/finalization, and source-ownership contracts in xUnit after their
removal from the legacy harness catalog.
Core runtime recording checks now execute through
`tests/Sussudio.Tests/XUnit.CoreRuntimeRecordingContractsTests.cs`, keeping
recording verifier, LibAv encoder, Flashback integrity, shared formatter, and
dedicated LibAv verification script contracts in xUnit after their removal from
the legacy harness catalog.
Core runtime checks now execute through
`tests/Sussudio.Tests/XUnit.CoreRuntimeContractsTests.cs`, keeping runtime
telemetry, capture-service snapshot, NativeXu, frame-ledger, recording-integrity,
and basic app contract checks in xUnit after their removal from the legacy
harness catalog.
Automation app-surface checks now execute through
`tests/Sussudio.Tests/XUnit.AutomationAppSurfaceContractsTests.cs`, keeping App
exception policy, converter/display formatting, LoggingJsonContext, MainWindow
automation surface, pipe/auth, and Stream Deck auth-envelope checks in xUnit
after their removal from the legacy harness catalog.
Automation ViewModel/Flashback UI checks now execute through
`tests/Sussudio.Tests/XUnit.AutomationViewModelFlashbackUiContractsTests.cs`,
keeping automation settings, audio/device/capture/recording routes, async
Flashback/probe surface, runtime snapshot ownership, scrub/toggle behavior,
timeline geometry, and Flashback presentation controller ownership checks in
xUnit after their removal from the legacy harness catalog.
Automation dispatcher checks now execute through
`tests/Sussudio.Tests/XUnit.AutomationDispatcherContractsTests.cs`, keeping
payload parsing, catalog metadata, readiness classification, authorization,
manifest, command coverage, and focused dispatcher command-owner checks in
xUnit after their removal from the legacy harness catalog.
Automation capture/Flashback routing checks now execute through
`tests/Sussudio.Tests/XUnit.AutomationCaptureFlashbackRoutingContractsTests.cs`,
keeping Flashback routing, capture transition policy, capture session
coordinator contracts, service namespace/source ownership, and diagnostics
snapshot refresh serialization checks in xUnit after their removal from the
legacy harness catalog.
Automation diagnostics snapshot-projection checks now execute through
`tests/Sussudio.Tests/XUnit.AutomationSnapshotProjectionContractsTests.cs`,
keeping snapshot/status, audio, capture and source, MJPEG, recording, system
resources and A/V sync, preview, and Flashback owner checks in xUnit after their
removal from the legacy harness catalog.
Presentation-preview MainViewModel initial checks now execute through
`tests/Sussudio.Tests/XUnit.PresentationPreviewMainViewModelInitialContractsTests.cs`,
keeping recording transition start/stop failure propagation checks in xUnit
after their removal from the legacy harness catalog.
Presentation-preview MainWindow initial checks now execute through
`tests/Sussudio.Tests/XUnit.PresentationPreviewMainWindowInitialContractsTests.cs`,
keeping close cancellation, window screenshot helper ownership, and property
changed routing delegation checks in xUnit after their removal from the legacy
harness catalog.
Presentation-preview window lifecycle checks now execute through
`tests/Sussudio.Tests/XUnit.PresentationPreviewWindowLifecycleContractsTests.cs`,
keeping native bootstrap, close lifecycle split, close request/app closing,
recording finalization, and shutdown cleanup contracts in xUnit after their
removal from the legacy harness catalog.
Presentation-preview launch/startup checks now execute through
`tests/Sussudio.Tests/XUnit.PresentationPreviewLaunchStartupContractsTests.cs`,
keeping splash loading phrase ownership, splash pacing policy, launch entrance
animation, and startup hosting contracts in xUnit after their removal from the
legacy harness catalog.
Presentation-preview preview screenshot checks now execute through
`tests/Sussudio.Tests/XUnit.PresentationPreviewScreenshotContractsTests.cs`,
keeping button workflow and plan-policy contracts in xUnit after their removal
from the legacy harness catalog.
Presentation-preview shell chrome checks now execute through
`tests/Sussudio.Tests/XUnit.PresentationPreviewShellChromeContractsTests.cs`,
keeping settings shelf, window title, live signal, and status-strip contracts
in xUnit after their removal from the legacy harness catalog.
Presentation-preview visual shell checks now execute through
`tests/Sussudio.Tests/XUnit.PresentationPreviewVisualShellContractsTests.cs`,
keeping control-bar hover animation, shell elevation, preview transition,
startup overlay, and fade-in reveal contracts in xUnit after their removal from
the legacy harness catalog.
Presentation-preview preview runtime shell checks now execute through
`tests/Sussudio.Tests/XUnit.PresentationPreviewRuntimeShellContractsTests.cs`,
keeping resize telemetry, renderer host state, snapshot mapping, D3D projection
ownership, surface/shadow ownership, and startup-plan fallback contracts in
xUnit after their removal from the legacy harness catalog.
Presentation-preview preview runtime policy checks now execute through
`tests/Sussudio.Tests/XUnit.PresentationPreviewRuntimePolicyContractsTests.cs`,
keeping snapshot health/projection policies and D3D projection policy defaults
in xUnit after their removal from the legacy harness catalog.
Presentation-preview recording checks now execute through
`tests/Sussudio.Tests/XUnit.PresentationPreviewRecordingContractsTests.cs`,
keeping recording button chrome, state presentation, lockout policy, and
button-action contracts in xUnit after their removal from the legacy harness
catalog.
Presentation-preview audio/control checks now execute through
`tests/Sussudio.Tests/XUnit.PresentationPreviewAudioControlContractsTests.cs`,
keeping preview audio fade, audio presentation, preview button presentation,
and microphone control contracts in xUnit after their removal from the legacy
harness catalog.
Presentation-preview MainViewModel audio-control checks now execute through
`tests/Sussudio.Tests/XUnit.PresentationPreviewMainViewModelAudioControlsContractsTests.cs`,
keeping analog gain mapping, preview audio monitoring volume persistence,
microphone and device guards, device-audio request lifetime, audio-device
selection policy, native XU audio-control profiles/transport, and audio meter
callback ownership contracts in xUnit after their removal from the legacy
harness catalog.
Presentation-preview responsive layout checks now execute through
`tests/Sussudio.Tests/XUnit.PresentationPreviewResponsiveLayoutContractsTests.cs`,
keeping responsive shell layout and breakpoint policy contracts in xUnit after
their removal from the legacy harness catalog.
Presentation-preview capture selection checks now execute through
`tests/Sussudio.Tests/XUnit.PresentationPreviewCaptureSelectionContractsTests.cs`,
keeping selection binding, property routing, collection sync, focused owner,
device-audio projection, and normalizer contracts in xUnit after their removal
from the legacy harness catalog.
Presentation-preview capture option checks now execute through
`tests/Sussudio.Tests/XUnit.PresentationPreviewCaptureOptionContractsTests.cs`,
keeping capture device action, option presentation, affordance policy, option
binding, and tooltip formatter contracts in xUnit after their removal from the
legacy harness catalog.
Presentation-preview output path checks now execute through
`tests/Sussudio.Tests/XUnit.PresentationPreviewOutputPathContractsTests.cs`,
keeping output path display, truncation formatter, and button-action contracts
in xUnit after their removal from the legacy harness catalog.
Presentation-preview MainViewModel output path checks now execute through
`tests/Sussudio.Tests/XUnit.PresentationPreviewMainViewModelOutputPathContractsTests.cs`,
keeping retired output picker partial ownership, invalid-path fallback behavior,
and focused free-space presentation helper ownership in xUnit after their
removal from the legacy harness catalog.
Presentation-preview MainViewModel source-telemetry presentation checks now
execute through
`tests/Sussudio.Tests/XUnit.PresentationPreviewMainViewModelSourceTelemetryContractsTests.cs`,
keeping source/target summary formatting, focused source telemetry helper
ownership, and live-signal pixel-format fallback order contracts in xUnit after
their removal from the legacy harness catalog.
Presentation-preview MainViewModel dependency-composition checks now execute
through
`tests/Sussudio.Tests/XUnit.PresentationPreviewMainViewModelDependencyCompositionContractsTests.cs`,
keeping root dependency seam, UI dispatch, presentation, recording,
capture/device, and runtime controller context ownership contracts in xUnit
after their removal from the legacy harness catalog.
Presentation-preview MainViewModel runtime checks now execute through
`tests/Sussudio.Tests/XUnit.PresentationPreviewMainViewModelRuntimeContractsTests.cs`,
keeping automation preview/HDR/volume routing, audio monitoring, capture
settings projection, preview lifecycle ownership, and audio ramp trace telemetry
contracts in xUnit after their removal from the legacy harness catalog.
MainViewModel presentation-preview contract execution is now owned by the
focused xUnit wrappers above, with no remaining legacy catalog hook.
Presentation-preview capture runtime guardrail checks now execute through
`tests/Sussudio.Tests/XUnit.PresentationPreviewCaptureRuntimeGuardContractsTests.cs`,
keeping recording stop failure propagation, preview stop overload/API
compatibility, and emergency recording stop threading contracts in xUnit after
their removal from the legacy harness catalog.
Presentation-preview capture Flashback buffer checks now execute through
`tests/Sussudio.Tests/XUnit.PresentationPreviewCaptureFlashbackBufferContractsTests.cs`,
keeping stale session cleanup and recovery-preserve contracts in xUnit after
their removal from the legacy harness catalog.
Project build/publish policy checks now execute through
`tests/Sussudio.Tests/XUnit.ProjectBuildContractsTests.cs`, keeping the
English-only publish locale and latest-build staging contracts in xUnit after
their removal from the presentation-preview capture catalog.
Presentation-preview D3D pacing checks now execute through
`tests/Sussudio.Tests/XUnit.PresentationPreviewD3DPacingContractsTests.cs`,
keeping transition-drain, frame-capture cancellation, and shared-device
reference lifecycle contracts in xUnit after their removal from the legacy
harness catalog.
Presentation-preview D3D geometry/screenshot checks now execute through
`tests/Sussudio.Tests/XUnit.PresentationPreviewD3DGeometryContractsTests.cs`,
keeping letterbox, black-edge, PNG CRC, and 16-bit PNG capture contracts in
xUnit after their removal from the legacy harness catalog.
Presentation-preview D3D present-cadence checks now execute through
`tests/Sussudio.Tests/XUnit.PresentationPreviewD3DCadenceContractsTests.cs`,
keeping cadence DTO shape and suppression-baseline behavior contracts in xUnit
after their removal from the legacy harness catalog.
Presentation-preview D3D device-lost checks now execute through
`tests/Sussudio.Tests/XUnit.PresentationPreviewD3DDeviceLostContractsTests.cs`,
keeping device-lost classification and recovery ownership contracts in xUnit
after their removal from the legacy harness catalog.
Presentation-preview D3D diagnostics checks now execute through
`tests/Sussudio.Tests/XUnit.PresentationPreviewD3DDiagnosticsContractsTests.cs`,
keeping swap-chain/render timing, snapshot-model, and performance-timeline
contracts in xUnit after their removal from the legacy harness catalog.
Presentation-preview D3D contracts/metrics source-ownership checks now execute
through
`tests/Sussudio.Tests/XUnit.PresentationPreviewD3DContractsAndMetricsOwnershipTests.cs`,
keeping configuration, native interop, frame types, frame ownership, DXGI frame
statistics, slow-frame diagnostics, and metric tracking contracts in xUnit after
their removal from the legacy harness catalog.
Presentation-preview D3D runtime-capture source-ownership checks now execute
through
`tests/Sussudio.Tests/XUnit.PresentationPreviewD3DRuntimeCaptureOwnershipTests.cs`,
keeping public frame submission and lifecycle contracts in xUnit after their
removal from the legacy harness catalog.
Presentation-preview D3D render setup/resource source-ownership checks now
execute through
`tests/Sussudio.Tests/XUnit.PresentationPreviewD3DRenderSetupOwnershipTests.cs`,
keeping panel binding, shared-device handoff, frame upload, input resources,
and device initialization contracts in xUnit after their removal from the
legacy harness catalog.
Presentation-preview D3D render-pipeline source-ownership checks now execute
through
`tests/Sussudio.Tests/XUnit.PresentationPreviewD3DRenderPipelineOwnershipTests.cs`,
keeping render passes, shader rendering cache, shader sources, frame-latency
wait, render thread, present accounting, viewport helpers, and screenshot
encoding contracts in xUnit after their removal from the legacy D3D harness
catalog. The empty legacy D3D catalog hook was removed after this final group
moved to xUnit.

Fullscreen transition mechanics now live under the
`Sussudio/Controllers/FullScreen/FullScreenController.*.cs` family. Keep the
root controller to the public toggle/state surface,
`FullScreenController.Transitions.cs` to enter/exit orchestration,
`FullScreenController.Animation.cs` to rect animation,
`FullScreenController.Chrome.cs` to chrome/material state, and
`FullScreenController.Controls.cs` to overlay pointer/auto-hide behavior.
`MainWindow.FullScreen.cs` remains the XAML event adapter and keeps the
Flashback fullscreen keyboard gate, timeline visibility callback, and scrub-end
bridge routed into the Flashback controllers.

Automation whole-window screenshot capture now lives in
`Sussudio/Controllers/Screenshot/Window/WindowScreenshotController.cs`, which now only owns
UI-thread dispatch, cancellation, and failure wrapping. Native PrintWindow/GDI
capture and screenshot result shaping live in
`Sussudio/Controllers/Screenshot/Window/WindowScreenshotNativeCapture.cs`, while pure PNG/BMP
byte-stream encoding lives in
`Sussudio/Controllers/Screenshot/Window/WindowScreenshotImageEncoder.cs`. `MainWindow.Screenshot.cs`
is the shared screenshot XAML/automation adapter.

Preview-frame screenshot button behavior now lives in
`Sussudio/Controllers/Screenshot/Preview/PreviewScreenshotController.cs`.
`Sussudio/Controllers/Screenshot/Preview/PreviewScreenshotPlanPolicy.cs` owns the pure output
directory fallback, file naming, status text, and log text policy.
`MainWindow.Screenshot.cs` is the XAML-facing adapter; the controller keeps
directory creation, preview-frame capture, logging side effects, and button
enable/disable state.
Renderer-level preview frame capture request state, timeout/cancellation
handling, staging-resource ownership, and screenshot error result construction
now live in
`Sussudio/Services/Preview/D3D11PreviewRenderer.ScreenshotRequests.cs`.
Render-thread GPU readback and before-present screenshot dispatch stay in
`Sussudio/Services/Preview/D3D11PreviewRenderer.ScreenshotCapture.cs`.
Preview-frame BMP/PNG pixel conversion, mapped-frame buffer copying, luminance
analysis, and letterbox/pillarbox measurement live in the
`Sussudio/Services/Preview/PreviewScreenshotCapture*.cs` family:
`PreviewScreenshotCapture.cs` owns mapped-frame copying and shared pixel
analysis, `PreviewScreenshotCapture.Png.cs` owns 16-bit PNG frame capture,
`PreviewScreenshotCapture.Bmp.cs` owns BMP capture/header writing, while
`Sussudio/Services/Preview/PreviewPng16Encoder.cs` owns the 16-bit PNG file
container, chunk writing, output-directory creation, and CRC helpers.

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
and the launch entrance trigger now live in
`Sussudio/Controllers/Launch/LaunchStartupController.cs`.
`Sussudio/MainWindow.ShellChrome.cs` keeps the XAML-facing Loaded adapter and
native shell bootstrap wiring.
Automation host composition, once-only
startup, ready/disabled logging, and pipe-before-hub shutdown disposal now live
in `Sussudio/Controllers/Window/WindowAutomationHostLifecycleController.cs`.
`Sussudio/Controllers/Launch/LaunchStartupController.cs` starts that
controller after initial device refresh, and `Sussudio/MainWindow.ShutdownCleanup.cs`
passes its async dispose delegate into the shutdown controller. Window close
completion lives in `Sussudio/Controllers/Window/WindowCloseLifecycleController.cs`;
recording-aware close finalization now lives in
`Sussudio/Controllers/Window/WindowCloseRecordingFinalizationController.cs`.

Top-level shell resize telemetry throttling for preview compositor transforms
now lives in `Sussudio/Controllers/Preview/PreviewResizeTelemetryController.cs`.
`Sussudio/MainWindow.PreviewRenderer.cs` owns the `SizeChanged` adapter and
renderer-host reset handoff. Preview surface sizing and GPU panel visibility now live in
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
start/stop/shutdown flow, renderer startup planning, CPU fallback attachment, and cleanup.
`Sussudio/Controllers/Preview/Renderer/PreviewRendererHostController.D3D.cs` owns D3D renderer
startup and event/failure handling.
`Sussudio/Controllers/Preview/Renderer/PreviewRendererHostController.Reinit.cs`
owns D3D reinit renderer-stop/timeout policy, disposal, unsafe-window
telemetry, stop tick accounting, fresh SwapChainPanel replacement, and
retired-renderer handoff during D3D renderer mode switches.
`MainWindow.PreviewRenderer.cs` is the XAML-facing host adapter surface.
`Sussudio/MainWindow.PreviewRuntimeSnapshot.cs` owns the stable automation
preview snapshot UI-dispatch adapter and UI-thread-only preview state sampling.
Read-only preview runtime snapshot construction now lives in
`Sussudio/Controllers/Preview/Renderer/PreviewRuntimeSnapshotController.cs`,
which owns preview-state orchestration.
`Sussudio/Controllers/Preview/Renderer/PreviewRuntimeSnapshotInput.cs` owns
the UI-thread sampled preview snapshot input contract shared by the snapshot
controller and D3D projection builder.
`Sussudio/Controllers/Preview/Renderer/PreviewRuntimeSnapshotMapper.cs` owns
final preview runtime snapshot DTO flattening from sampled input, D3D
projection, and named projection policies.
`Sussudio/Controllers/Preview/Renderer/PreviewRuntimeSnapshotSurfaceProjectionPolicy.cs`
owns previewing, renderer attachment, visibility, frame-count, and blank/stall
health projection into the runtime snapshot.
`Sussudio/Controllers/Preview/Renderer/PreviewRuntimeSnapshotStartupProjectionPolicy.cs`
owns sampled preview-startup field projection into the runtime snapshot,
including startup health elapsed time.
`Sussudio/Controllers/Preview/Renderer/PreviewRuntimeSnapshotGpuPlaybackProjectionPolicy.cs`
owns GPU playback fields in the runtime snapshot, including renderer-projected
playback state/dimensions and sampled position event count.
`Sussudio/Controllers/Preview/Renderer/PreviewRuntimeSnapshotHealthInputFactory.cs`
owns blank/stall suspicion input projection from sampled input, D3D projection,
and controller-provided clock/tick values.
`Sussudio/Controllers/Preview/Renderer/PreviewRuntimeSnapshotHealthPolicy.cs`
owns preview startup elapsed timing plus blank/stall suspicion policy.
`Sussudio/Controllers/Preview/Renderer/PreviewRuntimeD3DFrameCounterPolicy.cs`
owns the D3D-vs-CPU frame-counter fallback rules used by preview diagnostics.
`Sussudio/Controllers/Preview/Renderer/PreviewRuntimeD3DRendererStatePolicy.cs`
owns renderer state projection defaults, including renderer mode fallback,
swap-chain details, render-thread failure fields, color spaces, recent slow
frames, and GPU playback state.
`Sussudio/Controllers/Preview/Renderer/PreviewRuntimeD3DDisplayCadencePolicy.cs`
owns D3D display cadence projection defaults, including recent present intervals,
low-FPS summaries, jitter, and slow-frame percentages.
`Sussudio/Controllers/Preview/Renderer/PreviewRuntimeD3DRenderCpuTimingPolicy.cs`
owns D3D render CPU timing projection defaults for input upload, render submit,
present call, and total-frame timing metrics.
`Sussudio/Controllers/Preview/Renderer/PreviewRuntimeD3DPipelineLatencyPolicy.cs`
owns D3D pipeline latency projection defaults and the estimated preview pipeline
latency value.
`Sussudio/Controllers/Preview/Renderer/PreviewRuntimeD3DFrameOwnershipPolicy.cs`
owns D3D submitted/rendered/dropped frame-identity projection defaults,
including null-renderer source sequence sentinels and last-drop reason.
`Sussudio/Controllers/Preview/Renderer/PreviewRuntimeD3DFrameStatisticsPolicy.cs`
owns DXGI frame-statistics projection defaults, including the null-renderer
present-count sentinels used by preview diagnostics.
`Sussudio/Controllers/Preview/Renderer/PreviewRuntimeD3DFrameLatencyWaitPolicy.cs`
owns D3D frame-latency wait projection defaults, including null-renderer wait
handle state, wait counters, last result, and wait timing metrics.
`Sussudio/Controllers/Preview/Renderer/PreviewRuntimeD3DProjection.cs` owns
the renderer projection data contract root, frame-counter fields, and
frame-counter assignment from the evaluated policy record.
`PreviewRuntimeD3DProjection.RendererState.cs`,
`PreviewRuntimeD3DProjection.DisplayCadence.cs`,
`PreviewRuntimeD3DProjection.RenderCpuTiming.cs`,
`PreviewRuntimeD3DProjection.PipelineLatency.cs`,
`PreviewRuntimeD3DProjection.FrameOwnership.cs`,
`PreviewRuntimeD3DProjection.FrameStatistics.cs`, and
`PreviewRuntimeD3DProjection.FrameLatencyWait.cs` own the matching projection
field groups and assignment from their matching policy records. These
projection properties keep public getters with private setters so callers see a
read-only projection while each partial owns its assembly mapping.
`Sussudio/Controllers/Preview/Renderer/PreviewRuntimeD3DProjection.Builder.cs`
owns policy evaluation order and delegates projection assembly to those field
owners.
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
`Sussudio/Controllers/Window/WindowAppClosingController.cs` owns
`AppWindow.Closing` decision choreography: trigger logging, recording-aware
close cancellation, duplicate-stop guard, pre-close recording stop handoff,
completion, and second-close request.
`Sussudio/MainWindow.CloseLifecycle.cs` is the XAML/AppWindow close adapter and
keeps `RegisterCloseLifecycle`, `CloseAsync`, and `RequestWindowClose()` stable.
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
`Sussudio/MainWindow.ShellChrome.cs` is the XAML-facing shell launch/chrome
adapter and keeps the `_hwnd` field consumed by screenshot and window
automation paths.
MainWindow shell ownership tests mirror these runtime owners through focused
`MainWindow.ShellOwnership.*.Tests.cs` files for chrome, startup, preview
runtime, native bootstrap, and window lifecycle contracts.
Preview runtime source-shape coverage is split across renderer-host,
snapshot, D3D-projection, and surface test owners so failures point at the
runtime owner that actually drifted instead of one combined harness check.
MainWindow Flashback ownership tests mirror the Flashback controller owners
through focused `MainWindow.FlashbackOwnership.*.Tests.cs` files: polling,
timeline presentation, playhead/CTI motion, playback presentation/coordinator
behavior, and settings/command binding each have a named test owner.

Audio and microphone meter rendering now lives in the
`Sussudio/Controllers/Audio/Meter/AudioMeterController*.cs` family: the root
controller owns setup and XAML/view-model dependencies,
`AudioMeterController.MeterState.cs` owns smoothing, markers, resets, timer
lifetime, and `TranslateMarker`, and
`AudioMeterController.PresentationAnimations.cs` owns monitoring/disabled
animations and rounded clips. Audio/microphone initial control projection and
event hookup now live in
`Sussudio/Controllers/Audio/AudioControlBindingController.cs` and
`Sussudio/Controllers/Audio/AudioControlBindingController.Bindings.cs`: the
root owns the audio-control binding context, while the bindings partial owns
initial audio/microphone projection, preview-volume binding and priming,
audio/microphone/device-audio selection handlers,
record/preview/custom-audio/microphone toggle handlers, audio-meter activation,
initial meter presentation, and device-audio gain/meter resize hooks.
`Sussudio/MainWindow.AudioBindings.cs` is the XAML-facing adapter;
video-format collection setup, initial capture/recording option projection, and
code-attached resolution/frame-rate handlers now live in
`Sussudio/Controllers/Capture/CaptureOptionBindingController.Bindings.cs`, with
`Sussudio/Controllers/Capture/CaptureOptionBindingController.cs` keeping the
adapter context and `MainWindow.CaptureOptionBindings.cs` left as the
XAML-facing capture and recording option adapter.
Flashback settings-control initialization, GPU decode binding/sync, and buffer
duration combo sync now live in
`Sussudio/Controllers/Flashback/FlashbackSettingsBindingController.cs`.
`MainWindow.Bindings.cs` is only the startup binding sequence; device-selection
change hooks, initial recording lockout projection, and stats visibility sync
route through their existing feature adapters/controllers.

Capture session transition legality now lives in
`Sussudio/Models/Capture/CaptureSessionTransitionPolicy.cs`. Mutable capture
session state and transition generation now live in
`Sussudio/Services/Capture/CaptureSessionStateMachine.cs`, which applies the
policy before entering a transition and delegates steady-state resolution to the
same pure policy. `CaptureService.Coordination.cs` owns transition
serialization, steady-state input sampling, and state-machine delegation, so
cleanup, disposal, and fatal cleanup keep their flow ownership without writing
session state directly. The policy/state-machine pair is a transition-entry
gate plus steady-state resolver, not a full workflow graph: in-place serialized
mutations may pass the current state to the transition lock, while
lifecycle-changing operations should pass an explicit target
`CaptureSessionState`. Active recording backend resource ownership now lives in
`Sussudio/Services/Capture/CaptureRecordingBackendResources.cs`.
Capture session coordinator command enums, queue receipt records, session
snapshots, and Flashback playback/buffer status projections now live in
`Sussudio/Services/Capture/CaptureSessionCoordinator.Models.cs`.
`CaptureSessionCoordinator.cs` owns construction and shared state fields.
`CaptureSessionCoordinator.Commands.cs` owns the public non-Flashback
lifecycle/audio command facade into the serialized worker. Queue work item
creation, command enqueueing, enqueue-failure handling, and disposed-state
ingress guards now live in `CaptureSessionCoordinator.Queue.cs`. Worker-loop
execution, command coalescing, operation cancellation/failure accounting,
pending-command failure drain, and pending-command counter decrement policy now
live in `CaptureSessionCoordinator.QueueExecution.cs`.
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
capture/audio enumeration orchestration and the combined discovery result in
`DeviceService.cs`, format cache serialization in
`DeviceService.FormatCache.cs`, inline/background format probing in
`DeviceService.FormatProbe.cs`, device priority/capability scoring in
`DeviceService.Scoring.cs`, audio endpoint association in
`DeviceService.AudioAssociation.cs`, and native XU interface path resolution in
`DeviceService.NativeXu.cs`.

Native XU Kernel Streaming calls are grouped under
`Sussudio/Services/Capture/NativeXu/`. Keep constants and DTOs in the root,
shared 4K X identity, selected-interface projection, and native transport gate
ownership in `NativeXuDeviceSupport.cs`, SetupAPI interface enumeration in
`.Interfaces.cs`, handle opening in `.Handles.cs`, topology node parsing in
`.Topology.cs`, XU GET/SET transfer shapes in `.Transfers.cs`, and P/Invoke
struct declarations in `.Interop.cs`.
`tools/NativeXuAudioProbe` links this whole partial family explicitly, so
update its project file with every new partial.

Native device enumeration ownership is grouped under
`Sussudio/Services/Capture/DeviceDiscovery/`. Keep shared Media Foundation
constants, GUIDs, and P/Invoke declarations in `MfDeviceEnumerator.cs`, MF
video-device enumeration in `MfDeviceEnumerator.VideoDevices.cs`, WASAPI capture
endpoint enumeration and friendly-name reads in
`MfDeviceEnumerator.AudioEndpoints.cs`, native video format probing and
subtype/FourCC naming in `MfDeviceEnumerator.FormatProbe.cs`, and direct plus
enumeration-fallback MF source activation in `MfDeviceEnumerator.SourceOpening.cs`.

Capture service source telemetry polling and provider reads now live in
`Sussudio/Services/Capture/CaptureService.Telemetry.cs`, while fallback snapshot
construction and merge policy live in
`Sussudio/Services/Capture/CaptureService.TelemetryFallback.cs`. Capture-format
runtime telemetry, NTSC frame-rate correction, and frame-rate argument formatting
now live in
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

WASAPI audio-level/failure event projection now lives in
`Sussudio/Services/Capture/CaptureService.Audio.cs`. Audio-preview start/stop
lifecycle lives in
`Sussudio/Services/Capture/CaptureService.AudioPreviewLifecycle.cs`, and live
audio input switching lives in
`Sussudio/Services/Capture/CaptureService.AudioInputSwitching.cs`. Preview-time
microphone monitoring lives in
`Sussudio/Services/Capture/CaptureService.MicrophoneMonitor.cs`.
`Sussudio/Services/Capture/PreviewAudioGraphResources.cs` owns the live
program WASAPI capture, microphone capture, playback startup/shutdown,
audio-monitor attach/detach order, preview volume/mute application, playback
cleanup helpers, and capture-fault telemetry. These files preserve the root
service transition lock while keeping preview lifecycle, input switching, mic
cleanup, post-recording mic monitor restart, and playback routing from
collapsing back into a general audio partial.

Explicit capture cleanup now lives in
`Sussudio/Services/Capture/CaptureService.Cleanup.cs`. That file owns the
public cleanup transition, shutdown teardown order, failed Flashback recording
segment preservation, deferred LibAv/unified-video cleanup handoff, WASAPI
capture disposal, mic teardown, telemetry stop, and the call to Coordination's
final session-state reset helper.

Capture transition coordination now lives in
`Sussudio/Services/Capture/CaptureService.Coordination.cs`. That file owns
`RunTransitionAsync`, steady-state input sampling, state-machine delegation, and
initialization/disposal guards. Mutable session state and transition generation
live in `Sussudio/Services/Capture/CaptureSessionStateMachine.cs`. Cleanup,
disposal, and fatal cleanup paths call those helpers while preserving their
special teardown order.
Best-effort resource release helpers are delegated to
`Sussudio/Services/Capture/CaptureService.ResourceRelease.cs`.

Disposal-triggered cleanup and dispose flow now live in
`Sussudio/Services/Capture/CaptureService.DisposalLifecycle.cs`; disposed-state
writes route through Coordination. Coordination lock disposal is delegated to
`Sussudio/Services/Capture/CaptureService.ResourceRelease.cs`.

Capture resource release helpers now live in
`Sussudio/Services/Capture/CaptureService.ResourceRelease.cs`. That file owns
best-effort semaphore release/disposal, coordination-lock disposal, Flashback
backend/export held-lock release helpers, and Flashback eviction resume warnings
used by lifecycle/export/cleanup partials.

Deferred capture cleanup now lives in
`Sussudio/Services/Capture/CaptureService.DeferredCleanup.cs`. That file owns
the Flashback artifact cleanup adapter handoff and export-lock delegation.
Deferred unified-video cleanup after LibAv drains lives with the video pipeline
resource owner. Pending LibAv drain task state and reentry policy live in
`Sussudio/Services/Capture/CaptureRecordingBackendResources.cs`. Flashback backend
artifact cleanup request/retry/dispose/purge mechanics live in
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
the fatal capture cleanup launcher and generation-stale guards; cleaning-up and
faulted state writes route through Coordination helpers.
Flashback backend failure cleanup now lives in
`Sussudio/Services/Capture/CaptureService.FlashbackBackendFailureCleanup.cs`.
That file owns the Flashback backend cleanup launcher, GPU device-lost
classification, recovery segment preservation, and generation-stale guards, and
must not write session state directly.

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

Flashback recording backend ownership, audio attachment, encoded-frame
forwarding, and recording topology validation now live in
`Sussudio/Services/Capture/CaptureService.FlashbackRecording.cs`. Flashback
recording session-context policy now lives in
`Sussudio/Services/Capture/CaptureService.FlashbackRecording.SessionContext.cs`;
keep codec/HDR guardrails, frame-rate rational inference, and compatibility
snapshot fields there. Preview-backend producer wiring now belongs to
`Sussudio/Services/Flashback/FlashbackBackendResources.cs`, which owns the
video/audio/microphone attach and detach request shapes used by preview startup,
buffer cycling, and teardown. `Sussudio/Services/Flashback/FlashbackBackendResources.Startup.cs`
owns preview backend startup construction/install/playback initialization and
startup rollback cleanup. `Sussudio/Services/Flashback/FlashbackBackendResources.BufferCycle.cs`
owns sink-only buffer-cycle mechanics, while `Sussudio/Services/Flashback/FlashbackBackendResources.ArtifactCleanup.cs`
owns backend artifact cleanup request/retry/dispose/purge mechanics and
`Sussudio/Services/Flashback/FlashbackBackendResources.PreviewDisposal.cs`
owns preview-backend teardown mechanics, sink stop/dispose, and backend clear. `CaptureService`
supplies the service-level export-lock adapter, purge-policy resolution,
cancellation-token choice, and full rebuild fallback orchestration.

Recording start lifecycle now lives in
`Sussudio/Services/Capture/CaptureService.RecordingLifecycle.cs`. That file owns
the public recording start transition surface, startup-path routing, the private
rollback-state holder, and delegation to the recording-start rollback owner.
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
summaries, cancellation-result classification, post-finalize backend
reconciliation, failed-finalize recovery preservation, deferred settings apply,
buffer cycling, buffer-cycle failure classification, outcome publication,
backend cleanup launch, and Flashback-specific microphone monitor restart.
`Sussudio/Services/Capture/CaptureService.RecordingFinalizeLibAvBackend.cs`
owns standard LibAv recording finalization sequencing: audio-fault folding,
encoder/runtime and recording-integrity summaries, final state completion,
preview-restore ordering, and the visible final outcome publication before
delayed cancellation throws.
`Sussudio/Services/Capture/CaptureService.RecordingFinalizeLibAvResources.cs`
owns standard LibAv recording resource finalization: unified-video recording
stop, WASAPI recording detach/disposal, LibAv sink normal/emergency stop and
drain tracking, inactive-preview teardown, and LibAv finalization step result
records.
`Sussudio/Services/Capture/CaptureService.RecordingFinalizeLibAvPreviewRestore.cs`
owns standard LibAv live-preview restoration after recording: pending Flashback
enable-after-recording detection, guarded Flashback preview backend restore,
failed-restore rollback and purge, standard post-recording microphone monitor
restart, and the `FLASHBACK_ENABLE_AFTER_RECORDING_*` breadcrumbs. Recording
outcome field publication is delegated to
`Sussudio/Services/Capture/CaptureService.RecordingOutcomeState.cs` and
post-recording microphone monitor restart mechanics to
`Sussudio/Services/Capture/CaptureService.MicrophoneMonitor.cs`.
`Sussudio/Services/Capture/CaptureService.RecordingFinalizeFlashback.cs` owns
Flashback recording export finalization plus live-edge boundary snapshots,
including idempotent `EndFlashbackRecordingAccounting()` calls, source-frame
counters, recording integrity counters, and audio integrity counters.
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
Keep range export, last-N export, the shared backend snapshot helper, and
session-lock release before native export there. The shared export lifetime now
lives in `Sussudio/Services/Capture/CaptureService.FlashbackExportCore.cs`;
keep export-operation locking, eviction pause/resume, diagnostics completion,
exporter execution, and cleanup there. Shared request preparation now lives in
`Sussudio/Services/Capture/CaptureService.FlashbackExportRequestPreparation.cs`;
keep force-rotate outcomes, fallback segment discovery, active-file fallback,
request construction, and partial-fallback result marking there.

Flashback export planning now lives in
`Sussudio/Services/Capture/CaptureService.FlashbackExportPlanning.cs`. Keep
segment metadata mapping, live-export throttle policy, buffer range clamps, and
PTS offset helpers there so the export operation partial stays focused on
orchestration.

Flashback export diagnostics now lives in
`Sussudio/Services/Capture/CaptureService.FlashbackExportDiagnostics.cs`.
Keep export attempt state, progress forwarding, rejection records,
force-rotate fallback counters, and completion status projection there.

Shared video-pipeline lifecycle handoff now lives in
`Sussudio/Services/Capture/CaptureService.VideoPipelineLifecycle.cs`. That file
owns preview-frame sink attachment, late Flashback playback preview wiring,
shared D3D preview-device handoff, and fatal/pixel callback attach/detach.
Active video capture storage, preview-frame sink storage, negotiated video
getters, cached MJPEG pipeline timing snapshots, and deferred unified-video
cleanup after LibAv drains now live in
`Sussudio/Services/Capture/CaptureVideoPipelineResources.cs`.

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
pipeline disposal ordering, Flashback backend disposal, WASAPI disposal, and
microphone cleanup while delegating shared unified-video cleanup mechanics to
the video pipeline resource owner.

Recording integrity policy is now split under
`Sussudio/Services/Capture/CaptureService.RecordingIntegrity*.cs`. The root
partial resolves the active backend, `.Models.cs` owns the private counter DTOs,
`.Summary.cs` owns integrity status/reason classification plus the structured
`RECORDING_INTEGRITY` log line, `.Counters.cs` owns video/backend counter
capture and baseline deltas, and `.Audio.cs` owns audio counter capture and
baseline deltas. Snapshot partials consume that policy instead of containing it.

LibAv encoder option validation now lives in
`Sussudio/Services/Recording/LibAvEncoder.OptionsValidation.cs`. Keep required
path/codec/dimension/frame-rate/bitrate checks plus audio, microphone, and HDR
guards there. LibAv encoder codec policy stays in
`Sussudio/Services/Recording/LibAvEncoder.CodecPolicy.cs`; keep bitstream-filter
selection, NVENC preset/split-encode mapping, frame-size math, sample-format
support, and rational conversion helpers there; leave live send/drain/finalize
paths in the owner partials.

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
initialization there. D3D11 hardware frames setup and ArraySize=1 texture-pool
creation now live in `Sussudio/Services/Recording/LibAvEncoder.HardwareFrames.cs`;
CUDA hardware frame context adoption lives in
`Sussudio/Services/Recording/LibAvEncoder.HardwareFrames.Cuda.cs`.
Output rotation now lives in `LibAvEncoder.OutputRotation.cs`; final close and
trailer/logging now live in `LibAvEncoder.ResourceCleanup.cs`; native
frame/context/buffer release and encoder state reset now live in
`LibAvEncoder.NativeResourceRelease.cs`.

LibAv encoder video submission now lives in
`Sussudio/Services/Recording/LibAvEncoder.VideoSubmission.cs`. Keep CPU packed
frame submission, forced keyframe handling, per-frame HDR side-data
attachment/removal, and video packet drains there. D3D11 and CUDA hardware-frame
submission now live in
`Sussudio/Services/Recording/LibAvEncoder.HardwareSubmission.cs`; keep
texture-pool copy/reference setup, GPU device-removed checks, hardware-frame
PTS/keyframe assignment, HDR side-data attachment, EAGAIN packet drains, and
hardware-frame unref cleanup there.

LibAv encoder output lifecycle is split across focused partials.
`Sussudio/Services/Recording/LibAvEncoder.MuxerOptions.cs` owns MP4 muxer
option policy for open and rotated outputs. `LibAvEncoder.OutputRotation.cs`
owns rotation IO close/reopen, stream reinitialization, bitstream-filter reset,
and segment runtime resets. `LibAvEncoder.ResourceCleanup.cs` owns
flush/final close, dispose, trailer writing, close-result logging, and final
output telemetry. `LibAvEncoder.NativeResourceRelease.cs` owns native
frame/context/buffer release, hardware texture pool release, and encoder state
reset; keep generic error helpers in `LibAvEncoder.Diagnostics.cs`.

Recording artifact context creation stays in
`Sussudio/Services/Recording/RecordingArtifactManager.cs`, including temp/final
output file naming and HDR-active context fields. Recording artifact finalization
now lives in
`Sussudio/Services/Recording/RecordingArtifactManager.Finalization.cs`, including
mux success/failure cleanup, final-output validation, rollback, preserved
temp-artifact discovery, and best-effort artifact deletion.

LibAv recording sink queue ownership now lives in
`Sussudio/Services/Recording/LibAvRecordingSink.Queues.cs`. Keep public
video/GPU/CUDA enqueue entry points, caller-side validation, and shared
work-signal/fatal-failure/queue-depth-underflow helpers there.
`tests/Sussudio.Tests/RecordingQueue.LibAvSink.Queue.Tests.cs` owns the queue,
submission, and cleanup assertions for this family.
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

LibAv recording sink encode-loop ownership now lives in
`Sussudio/Services/Recording/LibAvRecordingSink.EncodingLoop.cs`. Keep the
background loop ordering, second audio/microphone drain pass, cancellation
cleanup, and fatal encoder failure handling there. Queue-to-encoder packet
drain ownership now lives in
`Sussudio/Services/Recording/LibAvRecordingSink.PacketDrain.cs`. Keep bounded
video/GPU/CUDA drain batches, unbounded LibAv audio/microphone drains,
frame-encoded event dispatch, GPU texture release, CUDA frame free, and pooled
buffer returns there. `tests/Sussudio.Tests/RecordingQueue.LibAvSink.Lifecycle.Tests.cs`
owns the LibAv sink lifecycle, output-validation, drain-loop, and packet-drain
assertions.

Recording verifier ownership is split across focused partials. Keep strict
verification orchestration in `Sussudio/Services/Recording/Verification/RecordingVerifier.cs`,
ffprobe process/spec/side-data probing in
`Sussudio/Services/Recording/Verification/RecordingVerifier.Ffprobe.cs`, probe
scalar parsing in
`Sussudio/Services/Recording/Verification/RecordingVerifier.ProbeParsing.cs`,
dimensions, frame-rate, and cadence validation policy in
`Sussudio/Services/Recording/Verification/RecordingVerifier.Validation.cs`,
container/codec format validation and Flashback export verification format
resolution in
`Sussudio/Services/Recording/Verification/RecordingVerifier.Validation.Format.cs`,
HDR verification policy in
`Sussudio/Services/Recording/Verification/RecordingVerifier.Validation.Hdr.cs`,
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
Flash-audio input interpretation, analog-gain detail row insertion, and
input-source display text live in
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
Keep generic AT SET wrappers and named SET wrappers there. Probe-facing raw AT
reads now live in
`Sussudio/Services/Telemetry/NativeXuAtCommandProvider.DeviceCommandReads.cs`
so read-only hardware inspection stays separate from mutating command paths.
Shared device identity, selected-interface projection, and native transport
gating live in `Sussudio/Services/Capture/NativeXu/NativeXuDeviceSupport.cs`;
the root provider dispatches through that support into telemetry polling.

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
there; the root provider uses shared Native XU device support before dispatching
into the active rolling poll path.

Native XU active rolling polling now lives in
`Sussudio/Services/Telemetry/NativeXuAtCommandProvider.RollingPoll.cs`. Keep
poll cadence gates, cached AT-command fields, incomplete-cache handling, and
group advancement there. Rolling command batch construction/refresh and
per-command cancellation checks now live in
`NativeXuAtCommandProvider.RollingCommandGroups.cs`.

Native XU selected-interface reading now lives in
`Sussudio/Services/Telemetry/NativeXuAtCommandProvider.InterfaceRead.cs`. Keep
interface open failures, topology reads, dev-specific node selection, per-node
rolling-read iteration, and node-read failure classification there. The root
provider keeps public `ReadAsync` validation, transport gate ownership, and
interface enumeration.

Native XU source snapshot assembly now lives in
`Sussudio/Services/Telemetry/NativeXuAtCommandProvider.SnapshotAssembly.cs`.
Keep VIC/frame-rate lookup, AT-command-result decode, diagnostic/detail
assembly, and full-vs-rolling logging and audio-origin policy switches there.
Flash-audio analog-gain row insertion belongs to the audio-input telemetry
detail partial.

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
`Sussudio/Services/Flashback/FlashbackEncoderSink.Lifetime.cs` and
`Sussudio/Services/Flashback/FlashbackEncoderSink.DisposeLifecycle.cs`. Keep
`StopAsync`, stop-drain timeout classification, and final stop result reporting
in `FlashbackEncoderSink.Lifetime.cs`; keep `Dispose`/`DisposeAsync`, deferred
cleanup, final dispose reset, cancellation/disposal helpers, and best-effort
encoder/buffer manager disposal in `FlashbackEncoderSink.DisposeLifecycle.cs`.

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
resampler output conversion, and bounded audio sample/byte sizing there. D3D11 device-context initialization, get-format callback behavior, and hardware
decoder context setup now live in
`Sussudio/Services/Flashback/FlashbackDecoder.D3D11.cs`. D3D11VA decoder
selection and hardware-configuration diagnostics now live in
`Sussudio/Services/Flashback/FlashbackDecoder.D3D11Discovery.cs`. Decoded video frame
output and hardware/software frame selection now live in
`Sussudio/Services/Flashback/FlashbackDecoder.VideoOutput.cs`. Software plane
copies and YUV-to-NV12/P010 conversion kernels now live in
`Sussudio/Services/Flashback/FlashbackDecoder.VideoConversion.cs`. Keep file
open/close and disposal lifecycle in the root decoder. Video
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
eviction selection, eviction file deletion, and disk-budget/window retention
policy there. Explicit purge/delete-all lifecycle behavior now lives in
`Sussudio/Services/Flashback/FlashbackBufferManager.Purge.cs`; keep
`PurgeCompletedSegments`, `PurgeAllSegments`, `PurgeAllSegmentsCore`, and
guarded purge deletion there. Eviction pause state, recording PTS range capture,
and pause-driven disk-warning state now live in
`FlashbackBufferManager.EvictionPause.cs`. The root buffer manager keeps
core state and read-only live counters. PTS reset/update, sink-cycle active
segment finalization, encoder frame-rate truth, and disk-byte accounting
updates live in
`Sussudio/Services/Flashback/FlashbackBufferManager.LiveAccounting.cs`.
Flashback buffer segment mutation now lives in
`Sussudio/Services/Flashback/FlashbackBufferManager.SegmentMutation.cs`. Keep
active segment path generation and active segment start/abandonment there.
Flashback buffer segment completion now lives in
`Sussudio/Services/Flashback/FlashbackBufferManager.SegmentCompletion.cs`. Keep
completion registration, duplicate-path rejection, and same-path segment
extension there.
Flashback buffer initialization, segment-extension setup, recovery-preserve
markers, disposal, and disposed-state guards now live in
`Sussudio/Services/Flashback/FlashbackBufferManager.Lifecycle.cs`.
Flashback buffer segment file lookup, range selection, and start-PTS lookup now
live in `Sussudio/Services/Flashback/FlashbackBufferManager.SegmentQueries.cs`.
Segment counts, active-path projection, active segment start PTS calculation,
and segment-info projection now live in
`Sussudio/Services/Flashback/FlashbackBufferManager.SegmentStatus.cs`.
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
result shaping, and single-export lock release there. Single-file packet result
validation now lives in
`Sussudio/Services/Flashback/FlashbackExporter.SingleFilePacketWriting.cs`.
The single-file active input packet pump lives in
`Sussudio/Services/Flashback/FlashbackExporter.SingleFilePacketReadLoop.cs`;
keep native frame reads, per-read packet unref, stream filtering, timestamp-base
discovery, buffered packet transition, inline remux writes, writer throttling,
and EOF partial-base rescue/freeing there.

Flashback exporter multi-segment packet-copy/remux behavior now lives in
`Sussudio/Services/Flashback/FlashbackExporter.Segments.cs`. Keep segment
validation dispatch, temp-output preparation, final output replacement, and
segment-export lock release there. Segment packet writing now lives in
`Sussudio/Services/Flashback/FlashbackExporter.SegmentPacketWriting.cs`; keep
output-template initialization, segment input sequencing, segment offset
updates, completion progress, and requested-segment skip validation there. The
active segment packet pump lives in
`Sussudio/Services/Flashback/FlashbackExporter.SegmentPacketReadLoop.cs`; keep
native frame reads, per-read packet unref, stream filtering, timestamp-base
discovery, buffered packet transition, rebased packet writes, writer throttling,
and EOF partial-base rescue/freeing there.
Per-segment packet write state and decisions live in
`Sussudio/Services/Flashback/FlashbackExporter.SegmentPacketWriteState.cs`; keep
timestamp-base discovery, buffered-packet rescue/flush, and native packet
write outcome state there. Segment timestamp rebasing, segment-boundary repair,
DTS monotonicity, and native packet writes live in
`Sussudio/Services/Flashback/FlashbackExporter.SegmentPacketRebasing.cs`.
Per-segment export range/window
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
`Sussudio/Services/Flashback/FlashbackExporter.ExportLock.cs`. Completed-output
length validation lives in
`Sussudio/Services/Flashback/FlashbackExporter.OutputValidation.cs`, normalized
path comparison and output path validation live in
`Sussudio/Services/Flashback/FlashbackExporter.PathValidation.cs`, and
export-range validation plus segment/export-range overlap classification live in
`Sussudio/Services/Flashback/FlashbackExporter.SegmentSelection.cs`. Native
input/output cleanup lives in
`Sussudio/Services/Flashback/FlashbackExporter.NativeState.cs`, linked export
cancellation-source helpers plus shared cancelled/disposed result creation live in
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
`Sussudio/Services/Preview/D3D11PreviewRenderer.PresentCadenceMetrics.cs` and
`Sussudio/Services/Preview/D3D11PreviewRenderer.Metrics.cs`. Keep read-only
present cadence state/projection and recent present interval copies in
`D3D11PreviewRenderer.PresentCadenceMetrics.cs`; keep pipeline latency, render
CPU timing, frame-latency wait metric snapshots, recent non-present sample
copies, and timing summaries in `D3D11PreviewRenderer.Metrics.cs`. Render-loop metric
window updates, expected-frame-rate window resizing, and metric reset logic now
live in `D3D11PreviewRenderer.MetricsTracking.cs`. Renderer implementation
fields should live with the partial that mutates or projects them: keep
slow-frame diagnostic ring/write state in
`D3D11PreviewRenderer.SlowFrameDiagnostics.cs`, lifecycle state in
`D3D11PreviewRenderer.Lifecycle.cs`, render-thread failure and first-frame state
in `D3D11PreviewRenderer.RenderThread.cs`, queue state and signaling in
`D3D11PreviewRenderer.PendingFrames.cs`, D3D device/swap-chain resources in
`D3D11PreviewRenderer.Resources.cs`, input texture resources in
`D3D11PreviewRenderer.InputResources.cs`, swap-chain panel binding state in
`D3D11PreviewRenderer.PanelBinding.cs`, waitable frame-latency state in
`D3D11PreviewRenderer.FrameLatency.cs`, render-pass selection plus
VideoProcessor execution in `D3D11PreviewRenderer.RenderPasses.cs`, NV12/HDR
shader draw execution in `D3D11PreviewRenderer.ShaderPasses.cs`, shared present
accounting in `D3D11PreviewRenderer.Present.cs`, and shader resource/cache state in
`D3D11PreviewRenderer.ShaderRendering.cs`. Do not re-centralize renderer
implementation state in `D3D11PreviewRenderer.cs`; the root should keep the
public facade, construction references, constants, user-facing accessors, and
public state toggles.

D3D preview renderer nested frame and metrics model types now live in
`Sussudio/Services/Preview/D3D11PreviewRenderer.FrameTypes.cs`. Keep the
`PendingFrame` lifetime wrapper and renderer metric record structs there so the
root renderer stays focused on construction, constants, user-facing state
accessors, and public state changes.

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
frame, lease, and single shared-texture submission entry points there. The
dual-plane NV12 submission guard, HDR transition logging, COM AddRef/release,
and pending-frame adapter live in
`Sussudio/Services/Preview/D3D11PreviewRenderer.Nv12Submission.cs`; keep
render-thread start/stop and disposal in `D3D11PreviewRenderer.Lifecycle.cs`
and panel sizing in the root renderer.

D3D preview renderer lifecycle now lives in
`Sussudio/Services/Preview/D3D11PreviewRenderer.Lifecycle.cs`. Keep
render-thread start/stop, reinit stop, native-call drain fencing, pending-frame
shutdown cleanup, renderer disposal, and render-pass native-call entry/exit
guard helpers there.

D3D preview renderer render-thread orchestration now lives in
`Sussudio/Services/Preview/D3D11PreviewRenderer.RenderThread.cs`. Keep the
MMCSS registration, frame-ready wait loop, shared-device reset consumption,
composition-transform wake handling, pending-frame consumption, stale-generation
drops, device-lost handoff, final pending-frame drain, frame-capture failure,
and render-thread failure telemetry there; keep render-pass selection and
VideoProcessor execution in `D3D11PreviewRenderer.RenderPasses.cs`, shader draw
execution in `D3D11PreviewRenderer.ShaderPasses.cs`, shared present accounting in
`D3D11PreviewRenderer.Present.cs`, and shader resource/cache state in
`D3D11PreviewRenderer.ShaderRendering.cs`.

D3D preview renderer frame upload now lives in
`Sussudio/Services/Preview/D3D11PreviewRenderer.FrameUpload.cs`. Keep
video-processor input view resolution, external texture input-view creation,
direct raw-frame texture updates, and staging uploads there; keep present
tracking in `D3D11PreviewRenderer.Present.cs`.

D3D preview renderer render-pass selection now lives in
`Sussudio/Services/Preview/D3D11PreviewRenderer.RenderPasses.cs`. Keep
VideoProcessor execution, HDR fallback logging, timing bucket attribution, and pass
precedence there. NV12/HDR shader draw execution now lives in
`Sussudio/Services/Preview/D3D11PreviewRenderer.ShaderPasses.cs`; keep native-call
guard consumption, shader-resource binding, draw calls, and shader-mode present
messages there. Shader resource/cache state now lives in
`Sussudio/Services/Preview/D3D11PreviewRenderer.ShaderRendering.cs`. Keep shader
fields, reusable shader class-instance arrays, and NV12 SRV caching there; keep
render-thread orchestration in `D3D11PreviewRenderer.RenderThread.cs`, and keep
present accounting and slow-frame diagnostic call sites in
`D3D11PreviewRenderer.Present.cs`.

D3D preview renderer slow-frame diagnostics now live in
`Sussudio/Services/Preview/D3D11PreviewRenderer.SlowFrameDiagnostics.cs`. Keep
recent slow-frame snapshot access, diagnostic thresholding, DXGI refresh-slip
reason construction, and the slow-frame ring buffer writer there; keep cadence
windows in `D3D11PreviewRenderer.PresentCadenceMetrics.cs` and CPU timing
windows in `D3D11PreviewRenderer.Metrics.cs`.

D3D preview renderer viewport and letterbox helpers now live in
`Sussudio/Services/Preview/D3D11PreviewRenderer.Viewport.cs`. Keep
`ComputeLetterboxViewport`, `UpdateViewportConstantBuffer`, and
`ComputeLetterboxRect` there; keep shader draw path ordering in
`D3D11PreviewRenderer.RenderPasses.cs` and D3D resource creation in
`D3D11PreviewRenderer.Resources.cs`.

D3D preview renderer submitted/rendered/dropped frame ownership tracking now
lives in `Sussudio/Services/Preview/D3D11PreviewRenderer.FrameOwnership.cs`.
Keep frame ownership snapshot projection and submitted/presented/dropped
ownership state updates there; keep cadence, latency, DXGI, and slow-frame
timing in `D3D11PreviewRenderer.PresentCadenceMetrics.cs`,
`D3D11PreviewRenderer.Metrics.cs`, and `D3D11PreviewRenderer.DxgiFrameStatistics.cs`,
with slow-frame diagnostic projection in `D3D11PreviewRenderer.SlowFrameDiagnostics.cs`.

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
`InitializeD3D` orchestration, shared-vs-owned device setup, video interface
acquisition, media present duration setup, initial panel binding, and renderer-
owned device fallback there. Composition swap-chain creation, startup dimensions,
HDR swap-chain capability probing, SDR swap-chain fallback, and initial color-
space selection now live in
`Sussudio/Services/Preview/D3D11PreviewRenderer.SwapChainInitialization.cs`.

D3D preview renderer resource management now lives in
`Sussudio/Services/Preview/D3D11PreviewRenderer.Resources.cs`. Keep
video-processor setup, swap-chain RTV/output view creation, and top-level D3D
resource cleanup orchestration there. Input/HDR texture teardown stays with
`D3D11PreviewRenderer.InputResources.cs`, shader/SRV teardown stays with
`D3D11PreviewRenderer.ShaderRendering.cs`, and preview-frame capture staging
teardown stays with `D3D11PreviewRenderer.ScreenshotCapture.cs`; keep
swap-chain color-space application with render-pass selection in
`D3D11PreviewRenderer.RenderPasses.cs`.
Raw-frame and HDR shader input texture allocation now lives in
`Sussudio/Services/Preview/D3D11PreviewRenderer.InputResources.cs`. Keep
NV12/P010 input textures, staging textures, input views, HDR plane SRV
creation, and input/HDR texture cleanup there. Device-lost recovery has its own
focused owner; keep render loop consumption in
`D3D11PreviewRenderer.RenderThread.cs`, present paths in
`D3D11PreviewRenderer.Present.cs`, and shader draw paths in
`D3D11PreviewRenderer.RenderPasses.cs`.

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
DXGI manager attachment, direct device-source open, native media-type selection,
and frame-size/frame-rate attribute reads there. Converted output media-type
construction now lives in
`Sussudio/Services/Capture/MfSourceReaderVideoCapture.ConvertedMediaType.cs`;
that small file is deliberate because it owns the Source Reader transform
boundary where the selected native source type is copied into the requested
NV12/P010 output type without changing negotiation semantics. Keep
device-enumeration open fallback and candidate reporting in
`Sussudio/Services/Capture/MfSourceReaderVideoCapture.DeviceEnumeration.cs`;
keep high-level source-reader state fields in the root source-reader file.

Media Foundation source-reader initialization orchestration now lives in
`Sussudio/Services/Capture/MfSourceReaderVideoCapture.Initialization.cs`. Keep
public initialization validation, startup-reference acquisition/release, reader
attribute construction, source media-type selection, and initialization
success/failure logging there. Actual-output reconciliation, strict
negotiated-output validation, runtime field reset, and COM/startup ownership
handoff after successful initialization live in
`Sussudio/Services/Capture/MfSourceReaderVideoCapture.InitializedSession.cs`.

Media Foundation source-reader read-loop ownership now lives in
`Sussudio/Services/Capture/MfSourceReaderVideoCapture.ReadLoop.cs`. Keep the
read thread priority, `ReadSample` outstanding-state tracking, sample timestamp
cadence handoff, frame-delivery invocation, frame-drop accounting, and fatal
D3D output failure break behavior there.

Media Foundation source-reader frame delivery now lives in
`Sussudio/Services/Capture/MfSourceReaderVideoCapture.FrameDelivery.cs`. Keep
sample-to-buffer conversion, compressed MJPG routing, dual GPU/CPU delivery
orchestration, readback fallback selection, and GPU texture release there.

Media Foundation source-reader raw frame buffer delivery now lives in
`Sussudio/Services/Capture/MfSourceReaderVideoCapture.RawFrameDelivery.cs`.
Keep compressed MJPG byte extraction, raw CPU frame delivery, 2D buffer
handling, packed-stride CPU copies, and dual-frame CPU payload extraction there.

Media Foundation interop declarations now live in
`Sussudio/Services/Capture/MfSourceReaderVideoCapture.Interop.cs` and
`Sussudio/Services/Capture/MfSourceReaderVideoCapture.ComContracts.cs`, with
sample/buffer contracts in
`Sussudio/Services/Capture/MfSourceReaderVideoCapture.SampleBufferContracts.cs`.
Keep MF P/Invoke declarations, constants/HRESULTs/GUIDs in the interop file.
Keep general Media Foundation COM interfaces in the contracts file. Keep the
flattened `IMFSample` vtable layout and MF buffer interfaces in the
sample/buffer contracts file. Preserve interface method order and placeholder
slots exactly; keep behavioral source-reader logic in the root and negotiation
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
`MfSourceReaderVideoCapture.FrameDelivery.cs` plus
`MfSourceReaderVideoCapture.RawFrameDelivery.cs` and reader start/stop/dispose
in the lifecycle partial.

Media Foundation packed-frame layout helpers now live in
`Sussudio/Services/Capture/MfSourceReaderVideoCapture.FrameLayout.cs`. Keep
frame-size/row-byte calculation, packed-stride inference, stride-aware YUV
copying, and source subtype labels there; keep frame delivery in
`MfSourceReaderVideoCapture.FrameDelivery.cs` plus
`MfSourceReaderVideoCapture.RawFrameDelivery.cs` and reader start/stop/dispose
in the lifecycle partial.

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
`Sussudio/Services/Capture/UnifiedVideoCapture.Lifecycle.cs`. Keep read-loop
start/stop, preview-reinit disposal, and capture fatal-error callbacks there.
Source-reader/D3D/MJPEG initialization and committed runtime state reset now
live in `Sussudio/Services/Capture/UnifiedVideoCapture.Initialization.cs`.
CPU MJPEG decode pipeline construction, preview jitter buffer setup/disposal,
MJPEG stop retention, and MJPEG fatal-error routing live in
`Sussudio/Services/Capture/UnifiedVideoCapture.MjpegPipelineLifecycle.cs`.

Unified capture recording sink fan-out now lives in
`Sussudio/Services/Capture/UnifiedVideoCapture.SinkFanout.cs`, and Flashback
sink fan-out now lives in
`Sussudio/Services/Capture/UnifiedVideoCapture.SinkFanout.Flashback.cs`. Keep
recording enqueue helpers, recording queue rejection accounting, and legacy
encoder fallback enqueue adapters in the recording fan-out file; keep Flashback
enqueue helpers, Flashback queue rejection accounting, and Flashback recording
sequence-gap accounting in the Flashback fan-out file; keep frame arrival callbacks in
`UnifiedVideoCapture.FrameIngress.cs`.

Unified capture preview routing now lives in
`Sussudio/Services/Capture/UnifiedVideoCapture.Preview.cs`. Keep preview sink
assignment, live-preview suppression/resume drains, MJPEG preview-frame decoded
callbacks, raw preview submission, and visual-cadence reset/recording helpers
there; keep recording and Flashback enqueue paths in
`UnifiedVideoCapture.SinkFanout.cs` and
`UnifiedVideoCapture.SinkFanout.Flashback.cs`.

Decoded visual-cadence frame ingestion now lives in
`Sussudio/Services/Capture/VisualCadenceTracker.cs`. Keep state, reset, frame
validation, output/change ingestion, and repeat-run bookkeeping there. Luma
sampling now lives in
`Sussudio/Services/Capture/VisualCadenceTracker.Sampling.cs`. Keep crop
selection, one-pass current-vs-previous comparison, sample-buffer promotion,
rolling sample writes, and elapsed-time conversion there; keep metrics DTO
construction, ring-buffer snapshot copying, delta/output/change statistics, and
motion-confidence labels in
`Sussudio/Services/Capture/VisualCadenceTracker.Metrics.cs`.

Source-packet fingerprint cadence ingestion now lives in
`Sussudio/Services/Capture/FrameFingerprintCadenceTracker.cs`. Keep frame
recording, duplicate-run counters, and fast packet hashing there; keep metrics
DTO construction, ring-buffer snapshot copying, unique-interval projection,
duplicate-percent statistics, and pattern labels in
`Sussudio/Services/Capture/FrameFingerprintCadenceTracker.Metrics.cs`.

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
the paced emit loop control flow and MMCSS registration.
`MjpegPreviewJitterBuffer.FramePacing.cs` owns display-clock alignment, frame
submission to the preview sink, tick waits, and timer-resolution P/Invoke. Keep
the root file focused on construction, suppression/reprime lifecycle, and
dispose-time queue teardown.

Parallel MJPEG compressed input admission now lives in
`Sussudio/Services/Gpu/ParallelMjpegDecodePipeline.CompressedQueue.cs`. Keep
startup invalid-MJPG drops, work-item construction, compressed byte-budget
rejection, queue-depth accounting, queue-full rejection, and packet-hash
recording there.

Parallel MJPEG worker execution now lives in
`Sussudio/Services/Gpu/ParallelMjpegDecodePipeline.Workers.cs`. Keep decoder
array ownership, worker thread creation/naming, worker decode-loop execution,
and worker liveness checks there; keep the root pipeline focused on
construction, callback storage, channel creation, and startup sequencing.
Software MJPEG decode/copy execution now lives in
`Sussudio/Services/Gpu/SoftwareMjpegDecoder.Decode.cs`. Keep FFmpeg decoder
context allocation, frame/packet ownership, disposal, and error-string helpers
in `Sussudio/Services/Gpu/SoftwareMjpegDecoder.cs`; keep the hot MJPEG
send/receive, format/dimension validation, one-time diagnostics, and YUV420 to
NV12 copy path in the decode partial.

Parallel MJPEG decode pipeline timing now lives in
`Sussudio/Services/Gpu/ParallelMjpegDecodePipeline.Metrics.cs`. Keep timing
record structs, timing snapshot construction, per-decoder sample windows,
packet-hash metric access, and stopwatch conversion helpers there; keep worker
decode ingress in the root pipeline.

Parallel MJPEG decode pipeline decoded-frame ordering now lives in
`Sussudio/Services/Gpu/ParallelMjpegDecodePipeline.Reorder.cs`. Keep strict
missing-sequence waits, known-missing skips, decoded reorder state, and decoded
reorder capacity policy there. Emit-loop ordered draining, preview
decoded-frame notification, and reorder/pipeline latency samples recorded during
emission now live in
`Sussudio/Services/Gpu/ParallelMjpegDecodePipeline.ReorderEmission.cs`.

Parallel MJPEG decode pipeline lifecycle now lives in
`Sussudio/Services/Gpu/ParallelMjpegDecodePipeline.Lifecycle.cs`. Keep
stop/dispose, emitter signaling, shutdown joins, fatal-callback dispatch, and
remaining-timeout helpers there. Final resource cleanup now lives in
`Sussudio/Services/Gpu/ParallelMjpegDecodePipeline.ResourceCleanup.cs`; keep
decoder disposal, queued work-item return, remaining reorder-frame disposal, and
emit-signal disposal there.

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
`NvdecMjpegDecoder.Initialization.cs` owns standalone CUDA device and
hardware-frame pool initialization, `NvdecMjpegDecoder.SharedInitialization.cs`
owns adoption of caller-provided CUDA device/frame contexts,
`NvdecMjpegDecoder.Decode.cs` owns packet decode and CUDA context access,
`NvdecMjpegDecoder.Download.cs` owns CPU download/packed-buffer copies, and
`NvdecMjpegDecoder.Lifetime.cs` owns disposal plus FFmpeg error text. Keep
shared-context ownership and disposal order unchanged when touching these files.

NVML telemetry ownership is now split between
`Sussudio/Services/Gpu/NvmlMonitor.cs`, which owns optional diagnostic polling,
snapshot publication, timer/lifetime behavior, and graceful unavailable
handling, and `Sussudio/Services/Gpu/NvmlMonitor.NativeInterop.cs`, which owns
raw NVML constants, structs, library loading, device-name lookup, and P/Invoke
declarations. Keep native declarations out of the polling lifecycle file so
the diagnostic-only runtime contract stays easy to audit.

Automation snapshot contracts now live in named model files under
`Sussudio/Models/Automation/`. The broad automation evidence DTO is split as an
`AutomationSnapshot*.cs` partial family by domain: root lifecycle/diagnostics,
user settings, HDR, audio/ingest, recording, capture format, source telemetry,
preview, MJPEG/cadence, system health, and Flashback. Other snapshot contracts
remain in `CaptureRuntimeSnapshot.cs`, `PreviewRuntimeSnapshot.cs`,
`PerformanceTimelineEntry*.cs`, `FlashbackSegmentInfo.cs`, and
`ViewModelRuntimeSnapshot.cs`. The performance timeline DTO is split by
diagnostics surface: root capture/preview cadence, preview/MJPEG/D3D,
Flashback playback, Flashback export, and process/system health. Do not
recreate a broad `AutomationRuntimeSnapshots.cs` catch-all; add new DTO fields
to the partial that matches the snapshot surface they own.

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
Behavior coverage for runtime snapshot states lives in
`tests/Sussudio.Tests/CaptureService.RuntimeSnapshots.Behavior.Tests.cs`; source
projection ownership coverage lives in
`tests/Sussudio.Tests/CaptureService.RuntimeSnapshots.ProjectionOwnership.Tests.cs`.
`Sussudio/Services/Capture/CaptureService.RuntimeSnapshotAssembler.cs` owns final
`CaptureRuntimeSnapshot` DTO construction from already-sampled field groups.
`Sussudio/Services/Capture/CaptureService.RuntimeSnapshotModels.cs` owns the
private runtime snapshot assembly and projection handoff models as one
substantial model owner instead of per-section tiny files.
Video ingest, source-reader health, WASAPI capture, and playback output counter
projection lives in
`Sussudio/Services/Capture/CaptureService.RuntimeSnapshotIngestAudio.cs`,
requested/negotiated reader transport, memory preference, frame-ledger, and
preview renderer-mode projection now lives in
`Sussudio/Services/Capture/CaptureService.RuntimeSnapshotReaderTransport.cs`,
HDR pipeline parity/downgrade and warmup state/count projection now lives in
`Sussudio/Services/Capture/CaptureService.RuntimeSnapshotHdrPipeline.cs`,
source telemetry detail/age/alignment projection now lives in
`Sussudio/Services/Capture/CaptureService.RuntimeSnapshotSourceTelemetry.cs`,
and recording-integrity summary projection now lives in
`Sussudio/Services/Capture/CaptureService.RuntimeSnapshotRecordingIntegrity.cs`.
Recording-format and observed-frame helper policy live in focused snapshot
partials.

Capture health snapshot sampling now lives in
`Sussudio/Services/Capture/CaptureService.HealthSnapshots.cs`. That file
captures current service references, invokes the focused field builders, and
populates the final service-state/scalar handoff passed to the assembler.
Source-cadence metric projection lives in
`Sussudio/Services/Capture/CaptureService.HealthSnapshotCaptureCadence.cs`.
MJPEG timing, preview jitter, visual cadence, packet hash, and per-decoder
projection lives in
`Sussudio/Services/Capture/CaptureService.HealthSnapshotMjpeg.cs`; pure
diagnostics/automation DTO construction lives in
`Sussudio/Services/Capture/CaptureService.HealthSnapshotAssembler.cs`. The
private assembler field records, including `CaptureHealthSnapshotAssemblyFields`,
`CaptureCadenceHealthSnapshotFields`, and `MjpegHealthSnapshotFields`, live
together in
`Sussudio/Services/Capture/CaptureService.HealthSnapshotAssembler.Models.cs`
as one substantial model owner. The assembler remains intentionally
allocation-neutral final DTO construction from captured fields; do not split it
into post-construction mutators or shallow fragment records just to reduce line
count.
source telemetry, backend, suppression, and circuit-state projection lives in
`Sussudio/Services/Capture/CaptureService.HealthSnapshotSourceTelemetry.cs`;
Flashback buffer, startup-cache, backend-staleness reason policy, encoder
summary, live queue, force-rotate, backpressure, and GPU queue projection lives
in `Sussudio/Services/Capture/CaptureService.HealthSnapshotFlashbackBackend.cs`;
recording health orchestration and LibAv-only CUDA queue projection live in
`Sussudio/Services/Capture/CaptureService.HealthSnapshotRecording.cs`, while
active recording backend selection, LibAv-vs-Flashback fallback, and
backend-specific queue/counter normalization live in
`Sussudio/Services/Capture/CaptureService.HealthSnapshotRecordingActiveBackend.cs`;
Flashback export diagnostic and derived progress/throughput projection lives in
`Sussudio/Services/Capture/CaptureService.HealthSnapshotFlashbackExport.cs`.
Flashback playback health snapshot orchestration now lives in
`Sussudio/Services/Capture/CaptureService.HealthSnapshotFlashbackPlayback.cs`;
state/frame/segment/PTS/seek-cap/submit-failure/A/V drift sampling lives in
`Sussudio/Services/Capture/CaptureService.HealthSnapshotFlashbackPlayback.State.cs`;
playback cadence metric sampling lives in
`Sussudio/Services/Capture/CaptureService.HealthSnapshotFlashbackPlayback.Cadence.cs`;
decode timing and max-phase metric sampling lives in
`Sussudio/Services/Capture/CaptureService.HealthSnapshotFlashbackPlayback.Decode.cs`;
audio-master pacing/fallback sampling lives in
`Sussudio/Services/Capture/CaptureService.HealthSnapshotFlashbackPlayback.AudioMaster.cs`;
and playback command telemetry sampling lives in
`Sussudio/Services/Capture/CaptureService.HealthSnapshotFlashbackPlayback.Commands.cs`,
while the private playback health projection field records live in the single
substantial model owner
`Sussudio/Services/Capture/CaptureService.HealthSnapshotFlashbackPlayback.Models.cs`.
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
composition stays split by role: `StatsOverlayCompositionController.cs` owns the
runtime facade and construction-order entry point, while
`StatsOverlayCompositionController.Contexts.cs` owns the grouped composition
context DTOs for shell controls, snapshot sources, dock targets, hardware
sources, and frame-time targets. `StatsOverlayCompositionController.Graph.cs`
owns snapshot provider, frame-time presentation, dock graph, overlay controller,
and section chrome factory wiring from those contexts instead of a flat
dependency bag;
stats dock presentation/diagnostic/hardware/refresh controller graph wiring
now lives in `Sussudio/Controllers/Stats/StatsDockControllerGraph.cs`;
the overlay partial is the XAML-facing adapter for stats overlay binding setup,
stats dock visibility, refresh hooks, and polling commands. Stats toggle
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
stats shell wiring and delegates controller/provider composition to
`Sussudio/Controllers/Stats/StatsOverlayCompositionController.cs`.
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
adapter beside the stats overlay visibility route, while
`Sussudio/Controllers/Stats/StatsOverlayCompositionController.Contexts.cs`
owns the grouped stats composition context contracts and
`Sussudio/Controllers/Stats/StatsOverlayCompositionController.Graph.cs` owns
presentation-controller graph composition from those contexts.
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
ownership, source telemetry, and frame-time overlay policy, plus
`tests/Sussudio.Tests/XUnit.StatsPresentation.Formatting.Tests.cs` for
detached-window, encoder, expected-display-repeat, and compact preview summary
behavior, `tests/Sussudio.Tests/XUnit.StatsPresentation.FrameTime.Tests.cs` for
frame-time range and frame-time graph geometry behavior, and
`tests/Sussudio.Tests/XUnit.StatsHardwareRowsTests.cs` for hardware decode/GPU
row behavior instead of expanding the legacy harness body in
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

Flashback timeline visibility, lockout, toggle synchronization, and track
layout sizing now live in
`Sussudio/Controllers/Flashback/FlashbackTimelineController.cs`. Show/hide
storyboard state, immediate collapse, and fullscreen animation reset live in
`Sussudio/Controllers/Flashback/FlashbackTimelineAnimationController.cs`.
`MainWindow.Flashback.cs` is the XAML-facing Flashback adapter; command semantics
live in `Sussudio/Controllers/Flashback/FlashbackCommandController.cs`.

Active Flashback pointer-scrub state now lives in
`Sussudio/Controllers/Flashback/FlashbackScrubInteractionController.cs`. It owns scrub
throttling, release/cancel/capture-lost cleanup, fullscreen scrub termination,
lockout clearing, scrub visual updates, and pointer lifecycle around scrub
commands. `MainWindow.Flashback.cs` is the XAML-facing adapter. Timeline
fraction/duration math used by scrub and playhead presentation now lives in
`Sussudio/Controllers/Flashback/FlashbackTimelineGeometry.cs`.

Flashback CTI/playhead compositor motion now lives in
`Sussudio/Controllers/Flashback/FlashbackPlayheadMotionController.cs`,
`Sussudio/Controllers/Flashback/FlashbackPlayheadMotionController.Cti.cs`, and
`Sussudio/Controllers/Flashback/FlashbackPlayheadMotionController.Visuals.cs`.
The root file owns context, public entry points, and shared state; `.Cti.cs`
owns playback-state sampling, scrub/window gating, live right-edge pinning,
long-horizon extrapolation scheduling, and CTI anchor timing; `.Visuals.cs` owns
compositor visual setup, snap placement, magnetic scrub movement, linear
keyframe animation, and label clamp/positioning.
`Sussudio/MainWindow.Flashback.cs` is the XAML-facing adapter;
command handling and toggle/apply workflows now live in the command controller.

Flashback marker placement and compact duration text now live in
`Sussudio/Controllers/Flashback/FlashbackMarkerPresentationController.cs`, including
in/out marker visibility, selection-region layout, and `m:ss` formatting.
`MainWindow.Flashback.cs` is the XAML-facing marker adapter.

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
apply/restart. `MainWindow.Flashback.cs` preserves the existing XAML
event-handler names as part of the consolidated Flashback adapter.

Flashback settings bindings now live in
`Sussudio/Controllers/Flashback/FlashbackSettingsBindingController.cs`: initial settings
projection, GPU decode toggle binding and reverse-sync, buffer duration combo
selection, and `FLASHBACK_UI_BUFFER_DURATION_CHANGED` logging. The async
Flashback enable/disable rollback path and apply/restart command now live in
`FlashbackCommandController`; `MainWindow.Flashback.cs` is the settings
XAML-facing adapter.

Flashback playback in/out marker state and file-PTS restore now live in
`Sussudio/Services/Flashback/FlashbackPlaybackController.MarkersState.cs`.
Marker commands, marker normalization, invalid-range clearing, and out-point
pause checks stay in
`Sussudio/Services/Flashback/FlashbackPlaybackController.Markers.cs`; keep
decode pacing, seek, and segment-opening flow in the playback controller
core/thread partials.

Flashback playback position/file-PTS mapping now lives in
`Sussudio/Services/Flashback/FlashbackPlaybackController.PositionMapping.cs`.
It owns scrub/seek clamping, marker-bound range limits, saturating timestamp
math, active fMP4 segment detection, and playback path comparison.

Cadence summary DTO construction, percentile math, and low-FPS derivation live
in `Sussudio/Services/Flashback/FlashbackPlaybackController.PlaybackCadenceMetrics.cs`.
Decode summary DTO construction and decode timing percentile projection live in
`Sussudio/Services/Flashback/FlashbackPlaybackController.PlaybackDecodeMetrics.cs`.
Private playback metric counters, read-only counter projection, and cadence sample rings now live in
`Sussudio/Services/Flashback/FlashbackPlaybackController.MetricsCollection.cs`.
Playback metric reset behavior now lives in
`Sussudio/Services/Flashback/FlashbackPlaybackController.MetricReset.cs`.
Seek-cap telemetry state, read-only projection, decoder seek forwarding, and
forward-decode-cap hit logging now live in
`Sussudio/Services/Flashback/FlashbackPlaybackController.SeekCapTelemetry.cs`.
Playback decode duration rings, max decode phase timing state, read-only max
decode projection, decode timing wrappers, and dominant decode phase resolution
now live in
`Sussudio/Services/Flashback/FlashbackPlaybackController.PlaybackDecodeMetricsCollection.cs`.

Flashback playback public command entry points now live in
`Sussudio/Services/Flashback/FlashbackPlaybackController.Commands.cs`. Keep
scrub, seek, play/pause, go-live, and nudge request gating there; keep raw
queue writes/drop policy in the queue partial and playback-thread execution in
the thread partials. Playback-thread command identity and payload shape live in
`Sussudio/Services/Flashback/FlashbackPlaybackController.CommandModels.cs`;
keep new command fields there so queue, coalescing, telemetry, and thread
owners share one private payload contract. Seek/scrub coalesced command admission now lives in
`Sussudio/Services/Flashback/FlashbackPlaybackController.CommandCoalescing.cs`.
Seek/scrub coalescing slot state, queued-position resolution, and queued-slot
barriers now live in
`Sussudio/Services/Flashback/FlashbackPlaybackController.CommandCoalescingSlots.cs`.
Playback-thread control-yield peek policy now lives in
`Sussudio/Services/Flashback/FlashbackPlaybackController.CommandControlYieldPolicy.cs`.
Do not grow the root controller with new coalescing slot fields.
Command status counters, pending-command accounting, active-command timing, and
queue telemetry bookkeeping live in
`Sussudio/Services/Flashback/FlashbackPlaybackController.CommandTelemetry.cs`.
Public read-only command counters, command queue latency/timestamps, last
command failure projection, and playback-thread liveness now live in
`Sussudio/Services/Flashback/FlashbackPlaybackController.CommandMetrics.cs`.
Command readiness guards, skipped-not-ready accounting, failure-detail
formatting, last-command failure state, and no-op logging now live in
`Sussudio/Services/Flashback/FlashbackPlaybackController.CommandFailures.cs`.
Keep command failure updates there instead of growing command channel mechanics.
Playback thread state, CTS lifetime, stop timeout policy, thread start/recovery,
and join/cancel diagnostics now live in
`Sussudio/Services/Flashback/FlashbackPlaybackController.ThreadLifecycle.cs`.
Playback-thread command channel capacity/state, bounded-channel recreation,
thread-exit completion, and abandoned-command draining now live in
`Sussudio/Services/Flashback/FlashbackPlaybackController.ThreadChannel.cs`.
Keep queue write/coalescing/drop policy in the command queue partial.
The playback worker loop now lives in
`Sussudio/Services/Flashback/FlashbackPlaybackController.ThreadLoop.cs`; keep
`PlaybackThreadEntry` command dispatch there, and do not reintroduce an empty
thread shell marker. Playback-thread scheduling policy now lives in
`Sussudio/Services/Flashback/FlashbackPlaybackController.ThreadScheduling.cs`;
keep timer-resolution P/Invoke plus MMCSS task/priority env policy there so
the worker loop remains focused on command flow.
Playback-thread exit transactions now live in
`Sussudio/Services/Flashback/FlashbackPlaybackController.ThreadExit.cs`; keep
prebuffer release, timer-resolution release, command-channel completion/drain,
thread/CTS ownership clearing, and deferred preview attach there instead of
inside the command dispatch loop.
Playback-thread seek command execution now lives in
`Sussudio/Services/Flashback/FlashbackPlaybackController.ThreadSeekCommands.cs`.
Keep coalesced seek resolution, exact resume targets, playback resume handoff,
and audio/preview suppression/resume ordering there instead of growing the
generic playback command partial. Playback-thread scrub begin/update/end command
execution now lives in
`Sussudio/Services/Flashback/FlashbackPlaybackController.ThreadSeekScrubCommands.cs`.
Keep frozen valid-start sampling, scrub update coalescing, and scrub-display
failure recovery there. Playback-thread end-scrub resume and paused exact-resume
target handling now live in
`Sussudio/Services/Flashback/FlashbackPlaybackController.ThreadEndScrubCommand.cs`.
Keep end-scrub seek/reopen, playback audio prebuffer priming, and rendering
resume ordering there.
Playback-thread play command execution now lives in
`Sussudio/Services/Flashback/FlashbackPlaybackController.ThreadPlayCommand.cs`.
Keep exact resume, file-open/reopen, audio prebuffer, and rendering resume
ordering there. Pause/go-live/stop/nudge command execution remains in
`Sussudio/Services/Flashback/FlashbackPlaybackController.ThreadCommands.cs`.
Playback-thread live-restore cleanup now lives in
`Sussudio/Services/Flashback/FlashbackPlaybackController.ThreadCleanup.cs`.
Keep repeated live-restore cleanup and playback CTS disposal warnings there
instead of duplicating state-reset blocks inside command handlers.
Flashback playback audio routing now lives in
`Sussudio/Services/Flashback/FlashbackPlaybackController.AudioRouting.cs`.
Keep live audio suppress/restore and playback-state audio/preview routing
there. Best-effort preview submission guards and audio renderer pause/resume/
flush guards now live in
`Sussudio/Services/Flashback/FlashbackPlaybackController.AudioPreviewGuards.cs`.
Decoder audio callback wiring, playback chunk validation/return, playback PTS
gate handling, and pooled audio-buffer return warnings now live in
`Sussudio/Services/Flashback/FlashbackPlaybackController.AudioCallback.cs`;
keep decode-ahead prebuffer target/timeout/frame-budget policy and rewind
behavior in the audio prebuffer partial.

Flashback playback component lifecycle now lives in
`Sussudio/Services/Flashback/FlashbackPlaybackController.Lifecycle.cs`. Keep
initialization, audio/preview component reference updates, lifecycle/dispose
state, and disposal there. Preview-detach cleanup, failed-stop detach timeout
state, deferred preview reattach state, and deferred reattach retry scheduling
now live in
`Sussudio/Services/Flashback/FlashbackPlaybackController.PreviewDetachLifecycle.cs`.
Keep decoder file handling and playback pacing in the controller core/thread
partials.

Flashback playback decoded-frame submission now lives in
`Sussudio/Services/Flashback/FlashbackPlaybackController.PreviewFrames.cs`.
Keep preview-sink selection, submission telemetry, renderer calls, and held-frame
handoff there. Decoded-frame validity checks, GPU/CPU frame skip reasons, and
NV12/P010 byte-size policy now live in
`Sussudio/Services/Flashback/FlashbackPlaybackController.PreviewFrameValidation.cs`.
Held playback frame backing state, release-for-live reset policy, and best-effort decoded
frame release warnings now live in
`Sussudio/Services/Flashback/FlashbackPlaybackController.PlaybackFrameOwnership.cs`;
seek-display and playback-submit failure recovery plus decode-error, near-live,
and software-decode-budget recovery back to live playback state now live in
`Sussudio/Services/Flashback/FlashbackPlaybackController.PlaybackLiveRecovery.cs`;
keep seek and playback loops in the core/thread partials.

Flashback playback decoder file handling now lives in
`Sussudio/Services/Flashback/FlashbackPlaybackController.DecoderFiles.cs`.
Keep decoder creation, active segment file identity, file open checks, and
decoder close/open identity transitions there. Best-effort decoder file close
handling, held-frame release during teardown, decoder close/dispose timing, and
cleanup telemetry now live in
`Sussudio/Services/Flashback/FlashbackPlaybackController.DecoderCleanup.cs`.
Active fMP4 reopen retry, keyframe-reopen recovery, and near-live reopen guards now live in
`Sussudio/Services/Flashback/FlashbackPlaybackController.DecoderReopen.cs`.
Adjacent-segment seek fallback policy, segment-start probing, segment switch
telemetry, and adjacent-seek failure handling now live in
`Sussudio/Services/Flashback/FlashbackPlaybackController.DecoderAdjacentSegmentSeek.cs`.
Segment-edge fMP4 reopen/reseek recovery and fMP4 reopen audio-gate restoration
now live in
`Sussudio/Services/Flashback/FlashbackPlaybackController.DecoderSegmentReopen.cs`.
Keep seek-display and playback pacing in the controller core/thread partials.

Flashback playback seek/scrub frame display now lives in
`Sussudio/Services/Flashback/FlashbackPlaybackController.SeekDisplay.cs`.
Keep keyframe seek display, displayed-frame PTS mapping, adjacent-segment
fallback for seek display, and seek-display failure accounting there; keep
continuous playback pacing in the controller core/thread partials.

Flashback continuous playback progression now lives in
`Sussudio/Services/Flashback/FlashbackPlaybackController.PlaybackLoop.cs`.
Keep decoded-frame submission flow, decode-error snap triggers, and near-live
snap detection, including the recovery near-live snap threshold, there; recovery
back to live state belongs in the playback live recovery owner. Playback frame reads, prebuffer cleanup, and A/V drift
frame-skip catch-up policy live in
`Sussudio/Services/Flashback/FlashbackPlaybackController.PlaybackFrames.cs`.
Segment-edge routing decisions and write-head waits now live in
`Sussudio/Services/Flashback/FlashbackPlaybackController.PlaybackSegmentEdges.cs`;
the next-segment switch transaction, including next-file probing, decoder
open/seek, switch counters, audio gate, and cadence-baseline reset, now lives in
`Sussudio/Services/Flashback/FlashbackPlaybackController.PlaybackSegmentSwitch.cs`.
Active fMP4 reopen/reseek recovery during segment-edge handling, including the
shared decoder reopen transaction and post-seek audio gate, lives in
`Sussudio/Services/Flashback/FlashbackPlaybackController.DecoderSegmentReopen.cs`.

Flashback playback timing policy now lives in
`Sussudio/Services/Flashback/FlashbackPlaybackController.PlaybackTiming.cs`.
Keep frame-rate resolution, pause-from-live target calculation,
continuous-playback snap policy, and rolling playback cadence metric updates
there. Decoded PTS cadence state, read-only projection, tracking, mismatch
telemetry, and cadence-baseline reset now live in
`Sussudio/Services/Flashback/FlashbackPlaybackController.PlaybackPtsCadence.cs`.
Software-decode budget detection, decoder hardware-acceleration status refresh,
over-budget snap telemetry, and recovery handoff now live in
`Sussudio/Services/Flashback/FlashbackPlaybackController.PlaybackSoftwareBudget.cs`;
the live-state recovery implementation remains in
`Sussudio/Services/Flashback/FlashbackPlaybackController.PlaybackLiveRecovery.cs`.
Audio-master clock sample state, stale-clock detection, read-only A/V drift
projection, and clock-drift computation now live in
`Sussudio/Services/Flashback/FlashbackPlaybackController.AudioMasterClock.cs`;
audio-master pacing correction policy, delay-adjustment counters, and wall-clock
sleep/spin pacing now live in
`Sussudio/Services/Flashback/FlashbackPlaybackController.AudioMasterPacing.cs`.
Audio-master fallback accounting state, fallback classification, pending
fallback suppression, and read-only fallback reason/drift/clock-age telemetry
projection and updates now live in
`Sussudio/Services/Flashback/FlashbackPlaybackController.AudioMasterFallbacks.cs`.
Decoder close best-effort handling now lives with decoder file ownership, and
decode-error snap-to-live recovery lives with the continuous playback loop, so
the root controller can remain a construction shell. Public playback state,
GPU-decode toggling, live-gap projection, decoder HW state, playback PTS
anchors, scrub resume state, and state-transition logging now live in
`Sussudio/Services/Flashback/FlashbackPlaybackController.PlaybackState.cs`.

Flashback status and playback-position polling timers now live in
`Sussudio/Controllers/Flashback/FlashbackPollingController.cs`.
`MainWindow.Flashback.cs` is the XAML-facing adapter; CTI anchor timing
lives in `Sussudio/Controllers/Flashback/FlashbackPlayheadMotionController.Cti.cs`.

Settings shelf visibility, the animation gate, and show/hide storyboard
construction now live in
`Sussudio/Controllers/Shell/SettingsShelfController.cs`. `MainWindow.ShellChrome.cs`
is the XAML-facing settings shelf adapter.

Splash phrase file lookup, Markdown-ish parsing, cached defaults, and exception
fallback now live in `Sussudio/Controllers/Launch/Splash/SplashLoadingPhraseCatalog.cs`.
Randomized interval/mode selection now lives in
`Sussudio/Controllers/Launch/Splash/SplashLoadingPhrasePacingPolicy.cs`.
`Sussudio/Controllers/Launch/Splash/SplashLoadingPhraseController.cs` owns
DispatcherTimer lifecycle and the two-line splash text animation.
Its MainWindow wiring is folded into `MainWindow.ShellChrome.cs` because launch
entrance owns the only phrase start/stop choreography and now shares the shell
launch adapter.

Loaded-time startup ordering now lives in
`Sussudio/Controllers/Launch/LaunchStartupController.cs`: native shell reveal
scheduling, initial ViewModel settings load, preview audio fade priming before
device refresh, no-preview placeholder fallback, automation host start in the
finally path, and splash/entrance trigger. `MainWindow.ShellChrome.cs` preserves
the XAML event handler and context wiring.

Launch entrance ownership is split by phase:
`Sussudio/Controllers/Launch/Entrance/LaunchEntranceAnimationController.cs` owns context and
initial hidden/scaled shell state,
`Sussudio/Controllers/Launch/Entrance/LaunchEntranceAnimationController.Splash.cs` owns splash
fade, loading-phrase start/stop ordering, one-shot splash playback state, and
handoff into shell entrance, and
`Sussudio/Controllers/Launch/Entrance/LaunchEntranceAnimationController.Shell.cs` owns shell
chrome/button/stats entrance choreography, deferred preview reveal logging,
active-storyboard cleanup, and control-bar shadow fade. `MainWindow.ShellChrome.cs`
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
`MainWindow.PreviewTransitions.cs` keeps the XAML event name stable as part of
the preview transition/presentation adapter.

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
HDR runtime state/readiness projection from capture runtime snapshots,
target-summary property application, live-signal info projection, and
auto-resolution display text live together in
`Sussudio/ViewModels/MainViewModel.CapturePresentation.cs`.

Preview-volume fade-in/fade-out state, saved target volume, storyboard lifetime,
and volume save suppression now live in
`Sussudio/Controllers/Preview/PreviewAudioFadeController.cs`.
`MainWindow.PreviewTransitions.cs` is the XAML-facing adapter.
Preview-audio volume transition mechanics now live in the
`Sussudio/ViewModels/PreviewAudioVolumeTransitionController*.cs` family.
`PreviewAudioVolumeTransitionController.cs` owns save suppression/override
state, transition priming and restore behavior, trace adapters, and
property-to-session volume forwarding. `PreviewAudioVolumeTransitionController.Ramps.cs`
owns the ramp constants/easing and async ramp-down/ramp-up execution.
Monitoring enable/disable orchestration and coordinator sequencing remain in
`Sussudio/ViewModels/MainViewModel.AudioMonitoring.cs`; audio capture and
audio-preview property-change handlers live in
`Sussudio/ViewModels/MainViewModel.AudioPropertyChanges.cs`.

Preview reinit animation active state, first-visual transition clears,
startup-reset preservation, completion presentation decisions, and
`D3D11_RENDERER_REINIT_FLAG` / `PREVIEW_REINIT_ANIMATE_*` logs now live in
`Sussudio/Controllers/Preview/PreviewReinitTransitionController.cs`.
`MainWindow.PreviewTransitions.cs` is the XAML/MainWindow adapter for
renderer-stop-before-teardown and reinit completion side effects.

Preview startup attempt/state bookkeeping, timestamps, cached failure/
missing-signal details, state/log transitions, first-visual confirmation
sequencing, and reset orchestration now live in
`Sussudio/Controllers/Preview/Startup/PreviewStartupSessionController.cs` instead of a
MainWindow field bundle. `Sussudio/MainWindow.PreviewStartup.cs` is the
XAML/MainWindow-facing adapter that supplies UI/runtime callbacks for startup
session state, watchdog/timeout payloads, and readiness-signal handoff.
Watchdog/telemetry timers, timeout configuration, timeout recovery, and failure-stop scheduling live in
`Sussudio/Controllers/Preview/Startup/PreviewStartupWatchdogController.cs`;
`Sussudio/MainWindow.PreviewStartup.cs` wires the MainWindow/XAML-facing
adapter and timeout diagnostic payload. Readiness-signal coordination now lives
in `Sussudio/Controllers/Preview/Startup/PreviewStartupSignalCoordinator.cs`: missing-signal
updates, playback-progress diagnostics, startup signal log strings, GPU
position counter state, and first-visual confirmation decisions. The
`Sussudio/MainWindow.PreviewStartup.cs` partial is the XAML/MainWindow
adapter that supplies live preview state, renderer visibility details, logging,
and confirmation callbacks. Readiness-signal required/received state,
missing-signal calculation, playback-advance threshold checks, and readiness
result snapshots live in
`Sussudio/Controllers/Preview/Startup/PreviewStartupReadinessSignalController.cs`. Missing-signal
and signal-list string formatting lives in
`Sussudio/Controllers/Preview/Startup/PreviewStartupSignalFormatter.cs`. Timeout reason,
timeout status, and failure-stop status text live inside
`Sussudio/Controllers/Preview/Startup/PreviewStartupWatchdogController.cs`, where the timeout
and failure-stop decisions are made. This keeps the root shell focused on wiring
while leaving the existing startup state machine behavior unchanged.
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
`MainWindow.PreviewRenderer.cs` owns the `SizeChanged` adapter and renderer-host
reset handoff; reinit renderer-stop/timeout policy lives with
`PreviewRendererHostController.Reinit.cs`; preview surface presentation lives with
`PreviewSurfacePresentationController`, and preview shadow visuals live with
`PreviewSurfaceShadowController`.

Preview-specific ViewModel event lifecycle and preview property-change routing
now live in `Sussudio/Controllers/Preview/PreviewLifecycleEventController.cs`.
`Sussudio/MainWindow.PropertyChangedPreview.cs` is the XAML/MainWindow-facing
adapter that preserves event handler signatures and delegates into the
controller. The broad `MainWindow.PropertyChanged.cs` dispatcher now owns only
the `PropertyChanged` event envelope, property-name normalization, and visible
route order. Preview reinit transition state and log ownership now live in
`Sussudio/Controllers/Preview/PreviewReinitTransitionController.cs`, while
`Sussudio/MainWindow.PreviewTransitions.cs` keeps the renderer-stop-before-teardown
handoff and XAML completion side effects.

Bottom status-strip projection now lives in
`Sussudio/Controllers/Shell/StatusStripPresentationController.cs`, while
`Sussudio/MainWindow.StatusStripPresentation.cs` is the XAML-facing adapter and
builds the ViewModel snapshot passed into the controller. The controller owns
the status-strip `PropertyChanged` router and preserves the recording-only
window-title refresh on recording-time updates. Flashback bitrate presentation
also routes through this controller so the recording bitrate text keeps one UI
owner.

Pure recording-state lockout decisions now live in
`Sussudio/Controllers/Recording/RecordingStatePresentationPolicy.cs`: recording-time
capture/audio control enablement, analog gain enablement, transition button
enablement, FFmpeg button enablement, and settled record-button content
visibility. Recording-state UI projection now lives in
`Sussudio/Controllers/Recording/RecordingStatePresentationController.cs`: ViewModel-derived
property-name routing, lockout/HDR/title/audio-meter policy application, and delegation to
`Sussudio/Controllers/Recording/Button/RecordingButtonChromeController.cs` for record-button
chrome.
`MainWindow.PropertyChangedRecording.cs` is the XAML-facing adapter.

Capture-option property-name routing still lives in the focused
`Sussudio/MainWindow.CaptureOptionBindings.cs` adapter. Output-path routing
lives in `OutputPathController`, shell visibility route order lives in
`ShellPropertyChangedController` over `StatsOverlayCompositionController` and
`SettingsShelfController`, and live source-signal routing lives in
`LiveSignalInfoController`. Keep the root dispatcher limited to route order,
and add new property-name cases to the nearest focused owner.

Flashback-specific ViewModel property adapter dispatch now lives in
`Sussudio/Controllers/Flashback/FlashbackPropertyChangedController.cs`:
timeline lockout, marker and playhead refresh, export progress, and Flashback
settings-control sync. `Sussudio/MainWindow.PropertyChangedFlashback.cs` is the
XAML/MainWindow adapter that composes the route table callbacks.

Audio and microphone-specific ViewModel property projections now live in
`Sussudio/Controllers/Audio/AudioControlPresentationController.cs`: audio toggles,
monitoring meter state, preview volume slider sync, microphone enablement, and
microphone volume sync. The controller also owns the audio property-change
router so `Sussudio/MainWindow.PropertyChangedAudio.cs` stays the XAML-facing
adapter.

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
root controller owns the controller shell, context lifetime, XAML control
dependency bag. `.CollectionSync.cs` owns capture/audio/microphone/encoder
collection wiring, collection-change debounce/queued sync, and available-option
property-change rebinding. `.DeviceSelection.cs` owns capture-device selection,
pending-device apply state, and mismatch logging, `.AudioSelection.cs` owns
audio input and microphone selection, `.CaptureModeSelection.cs` owns resolution
and frame-rate selection, `.RecordingSelection.cs` owns recording format/quality/preset/
split-encode selection and shared string ComboBox selection application,
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

Capture option binding setup now lives in the
`Sussudio/Controllers/Capture/CaptureOptionBindingController.*.cs` family.
`Sussudio/Controllers/Capture/CaptureOptionBindingController.cs` keeps the
capture-option binding adapter context. `CaptureOptionBindingController.Bindings.cs`
owns video-format and initial decoder projection, initial selection projection,
resolution/frame-rate selection handlers, recording option event bindings for
format, quality, preset, split-encode, video format, and custom bitrate,
HDR/true-HDR click binding, and `ShowAllCaptureOptionsToggle` click binding.
`CaptureOptionBindingController.PropertyChanges.cs` owns capture-option/source-
signal property-change routing, custom-bitrate property-change value
projection, HDR/true-HDR ViewModel-to-control sync, preview HDR passthrough
forwarding, and presentation callback routing for option affordances,
telemetry tooltips, and source overlay refreshes. The split preserves the same
MainWindow-facing method surface while avoiding tiny responsibility shards.
`Sussudio/MainWindow.CaptureOptionBindings.cs` now owns the XAML-facing
binding setup methods and the small property-change forwarding method, so
there is no separate pass-through partial just for capture-option property
changes.
`MainWindow.CaptureOptionBindings.cs` keeps the old capture and recording option
method names used by `SetupBindings()`.

MainWindow capture ownership tests now mirror these runtime owners instead of
living in one capture test grab-bag. Selection bindings, selection normalizer
policy, device actions, option presentation, option affordance policy, option
bindings, and option tooltip formatting each have a focused
`MainWindow.ControllerOwnership.Capture.*.Tests.cs` file registered with the
presentation-preview harness coverage check.

Recording output-path textbox, tooltip, resize-event updates, browse, and
open-recordings button workflows now live in
`Sussudio/Controllers/Recording/Output/OutputPathController.cs`; pure truncation
text policy stays in
`Sussudio/Controllers/Recording/Output/OutputPathDisplayTextFormatter.cs`.
`OutputPathController` also owns the output-path property-change route;
`MainWindow.OutputPath.cs` is the XAML-facing adapter used by binding setup and
button events.

Diagnostic session DTOs live in feature-oriented model files:
`tools/Common/DiagnosticSessionOptions.cs`,
`tools/Common/DiagnosticSessionResult.cs`,
`tools/Common/DiagnosticSessionResult.Overview.cs`,
`tools/Common/DiagnosticSessionResult.CaptureSource.cs`,
`tools/Common/DiagnosticSessionResult.PreviewCadence.cs`,
`tools/Common/DiagnosticSessionResult.PreviewScheduler.cs`,
`tools/Common/DiagnosticSessionResult.PreviewD3D.cs`,
`tools/Common/DiagnosticSessionResult.PreviewVisualCadence.cs`,
`tools/Common/DiagnosticSessionResult.FlashbackPlayback.Commands.cs`,
`tools/Common/DiagnosticSessionResult.FlashbackPlayback.Cadence.cs`,
`tools/Common/DiagnosticSessionResult.FlashbackPlayback.Decode.cs`,
`tools/Common/DiagnosticSessionResult.FlashbackPlayback.AudioMaster.cs`,
`tools/Common/DiagnosticSessionResult.FlashbackPlayback.Stage.cs`,
`tools/Common/DiagnosticSessionResult.FlashbackRecording.cs`,
`tools/Common/DiagnosticSessionResult.FlashbackExport.cs`,
`tools/Common/DiagnosticSessionSample.cs`. `DiagnosticSessionOptions.cs` also owns
shared tool invocation defaults and the ssctl diagnostic-session usage string,
while `DiagnosticSessionScenarioCatalog.cs` owns scenario name constants,
normalization, the MCP-compatible scenario description, the CLI help-list
constant, setup requirement queries, and export verification artifact lookup.
`DiagnosticSessionScenarioCatalog.Entries.cs` owns scenario ordering by
composing focused entry groups. Core, Flashback playback, Flashback export/
lifecycle, Flashback recording/rejection, and combined scenario requirement
metadata now live in the matching `DiagnosticSessionScenarioCatalog.Entries.*.cs`
partials.
`DiagnosticSessionRunner.cs` owns the
public compatibility entry points; `DiagnosticSessionRunExecution.cs` owns the
visible run phase sequence around context creation, initial snapshot, scenario
execution, cleanup, and completion handoff. `DiagnosticSessionRunContext.cs`
owns core mutable per-run infrastructure: bootstrap, actions, warnings,
samples, run state, command channel, and scenario cancellation source.
`DiagnosticSessionRunContext.InitialSnapshot.cs` owns initial snapshot state and
capture, `DiagnosticSessionRunContext.LiveState.cs` owns live-state writer
handoff, and `DiagnosticSessionRunContext.Lifetime.cs` owns run-context
disposal. `DiagnosticSessionRunContext.PhaseContexts.cs` owns
scenario/completion context construction and the callback/token handoffs passed
into those phases.
`DiagnosticSessionRunExecution.Completion.cs` owns the
post-cleanup evidence/result sequence for recording checks, post-run timeline
and final snapshot capture, result-build invocation, and terminal live-state
write. `DiagnosticSessionRunExecution.CompletionContext.cs` owns the completion
context handoff consumed by the post-cleanup phase, while
`DiagnosticSessionRunExecution.ResultBuildRequest.cs` owns result-build request
mapping from completion evidence and run bootstrap metadata.
`DiagnosticSessionRunExecution.cs` hands scenario execution directly to
`DiagnosticSessionScenarioPhaseRunner.cs`, which owns the main scenario phase
for setup/startup, sampling/completion delegation, and fault drain delegation.
`DiagnosticSessionScenarioPhaseRunner.Models.cs` owns the
explicit phase context/state/result records.
`DiagnosticSessionScenarioPhaseRunner.Sampling.cs` owns scenario sampling:
live-state sampling setup, sample-loop invocation, and handoff to completion.
`DiagnosticSessionScenarioPhaseCompletion.cs` owns post-sampling completion
order and fault-drain delegation: registered background work before
rejected-export handling, rejected-export handling before PresentMon
completion, and interrupted drain handoff.
`DiagnosticSessionRunExecution.ResultBuildRequest.cs` owns the final result-build
request mapping consumed by the completion phase.
The public options/result/sample contracts are separated from runner behavior. The result
DTO root owns core session metadata, terminal state, artifacts, actions, and
warnings; the result partials own capture/source, Flashback playback command
queue, Flashback playback cadence, Flashback playback decode, Flashback
playback audio-master, Flashback playback stage/seek, Flashback recording,
Flashback export, preview cadence, preview scheduler, preview D3D, preview
visual cadence, process, recording verification, and PresentMon fields.

Diagnostic-session result text now lives in a focused partial family rooted at
`tools/Common/DiagnosticSessionResultFormatter.cs`. The root owns the public
`Format(...)` flow and section ordering. `.Overview.cs` owns the
header/summary/evidence section, `.CaptureMode.cs` owns the capture-mode row and
frame-rate text formatting, `.RecordingVerification.cs` owns recording
verification text, `.PresentMon.cs` owns PresentMon text, and
`.ProcessPerformance.cs` owns process-performance text. `.Flashback.cs` owns
Flashback section ordering plus simple playback command and playback
stage/seek-cap rows,
`.FlashbackRecording.cs` owns Flashback recording summary text,
`.FlashbackExport.cs` owns Flashback export summary text, and
`.FlashbackPlayback.Performance.cs` owns playback cadence/audio-master
performance text. `.FlashbackPlayback.Decode.cs` owns playback decode text,
`.Preview.cs` owns preview section ordering plus preview scheduler text,
`.PreviewD3D.cs` owns D3D performance/slow-frame and CPU timing text, and
`.PreviewVisualCadence.cs` owns visual cadence text. `.Artifacts.cs` owns
artifact/action/warning sections, and
`DiagnosticSessionOptionalTextFormatter.cs` owns shared optional text helpers.
The runner keeps `Format(...)` as a compatibility wrapper so existing ssctl
and MCP callers do not need to know about the formatter owner.

Diagnostic-session result construction now lives in
`tools/Common/DiagnosticSessionResultBuilder.cs`. The root owns result phase
orchestration, artifact-write handoff, summary-write handoff, and final
summary emission plus summary-write failure repair while the runner keeps the
phase sequence. It also owns final-result orchestration from analysis and
artifact paths into the named projection set and flattening owner, plus
Flashback playback projection composition from focused playback projection
owners.
`DiagnosticSessionResultBuilder.Flattening.cs` owns final
`DiagnosticSessionResult` DTO assignment from the projection set; keep domain
projection composition outside this initializer. The root owns projection-set
assembly from overview, capture, Flashback, preview, D3D, and visual-cadence
projection owners. Overview
outcome policy plus process CPU end/max-observed aggregation, recording
verification, and PresentMon DTO projection values live in
`DiagnosticSessionResultBuilder.OverviewResult.cs`. Diagnostic metric gathering
for validation/result projections and analysis warning emission live in
`DiagnosticSessionResultBuilder.Analysis.cs`, while named validation handoff
order lives in `DiagnosticSessionResultBuilder.AnalysisValidation.cs`.
Flashback playback/export analysis warning text, thresholds, and tolerated
Flashback scenario warning classification live in
`DiagnosticSessionResultBuilder.FlashbackWarnings.cs`; result-build request,
analysis, and projection-set handoff models live in
`DiagnosticSessionResultBuilder.Models.cs`. Diagnostic health verdict
composition, warning tolerance, and health warning text now live in
`DiagnosticSessionResultBuilder.DiagnosticHealth.cs`. Diagnostic health summary
snapshot selection and health summary text projection live in
`DiagnosticSessionResultBuilder.DiagnosticHealthSummary.cs`; source-reader/
ingest warning deltas for sparse source-capture tolerance live in
`DiagnosticSessionResultBuilder.DiagnosticHealthSourceWarnings.cs`.
Preview-scheduler analysis handoff values live in
`DiagnosticSessionResultBuilder.PreviewScheduler.cs`: MJPEG jitter-buffer
counter/delta reads, last drop/underflow reason and age reads,
max/schedule-late aggregation, and preview-scheduler result projection values.
`DiagnosticSessionResultAnalysis.PreviewScheduler` is the single record
property that carries those values into the scheduler result projection without
rereading MJPEG jitter-buffer snapshot keys. Flashback preview-scheduler validation orchestration
now lives in `DiagnosticSessionResultBuilder.PreviewSchedulerValidation.cs`,
including target-FPS fallback, visual-cadence tolerance checks, sparse
deadline/drop tolerance selection, and the call into shared Flashback preview
validation. Preview cadence, visual cadence, and D3D frame-stats/slow-frame/
CPU-timing result projection values are split by runtime metric owner:
`DiagnosticSessionResultBuilder.PreviewResult.cs` owns preview-cadence result
projection values, `DiagnosticSessionResultBuilder.PreviewVisualCadenceResult.cs`
owns visual-cadence result projection values, and
`DiagnosticSessionResultBuilder.PreviewD3DResult.cs` owns D3D frame-stats,
slow-frame, and CPU-timing result projection values. The D3D fields still
travel through a distinct `PreviewD3D` projection set member so renderer timing
semantics stay separate from scheduler/jitter policy.
Flashback playback result composition lives in
`DiagnosticSessionResultBuilder.FlashbackPlaybackResult.cs`, which keeps the
playback projection record as one cohesive handoff into the result projection
set.
The detailed playback result DTO value maps are split by runtime metric owner:
command queue values live in
`DiagnosticSessionResultBuilder.FlashbackPlaybackCommandsResult.cs`, cadence/
1% low/slow-frame/dropped-frame values live in
`DiagnosticSessionResultBuilder.FlashbackPlaybackCadenceResult.cs`, decode
timing values live in
`DiagnosticSessionResultBuilder.FlashbackPlaybackDecodeResult.cs`, and
submit/segment/write-head/near-live/seek-cap stage values live in
`DiagnosticSessionResultBuilder.FlashbackPlaybackStagesResult.cs`.
Audio-master fallback, buffering, and A/V-drift result values live in
`DiagnosticSessionResultBuilder.FlashbackPlaybackAudioMasterResult.cs`. Each
focused Flashback playback result owner keeps its projection record next to the
mapping that fills it.
Flashback recording backend/growth/integrity DTO projection values live in
`DiagnosticSessionResultBuilder.FlashbackRecordingResult.cs`, while export
status/progress DTO projection values live in
`DiagnosticSessionResultBuilder.FlashbackExportResult.cs`; result construction
still consumes named Flashback projections while preserving the existing
`summary.json` field shape.
Export force-rotate fallback counters now travel with
`FlashbackExportSessionMetrics` instead of loose analysis record fields.
Capture selection, negotiated format, source geometry, detected cadence, HDR,
and source-telemetry DTO projection values live in
`DiagnosticSessionResultBuilder.CaptureResult.cs`.

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
projection orchestration and root cadence fields in `PerformanceTimelineTools.Rows.cs`,
preview/MJPEG/D3D row projection in `PerformanceTimelineTools.Rows.Preview.cs`,
Flashback playback row projection in
`PerformanceTimelineTools.Rows.FlashbackPlayback.cs`, Flashback export row
projection in `PerformanceTimelineTools.Rows.FlashbackExport.cs`, system row
projection in `PerformanceTimelineTools.Rows.System.cs`, the private row model
in `PerformanceTimelineTools.Rows.Model*.cs` split by root cadence,
preview/MJPEG/D3D, Flashback playback, Flashback export, and system fields,
timeline table text rendering in `PerformanceTimelineTools.Rendering.cs`,
first-vs-last trend text and target-summary orchestration in
`PerformanceTimelineTools.Rendering.Trend.cs`, preview cadence, visual/MJPEG
fingerprint, jitter, D3D, and slow-stage trend text in
`PerformanceTimelineTools.Rendering.Trend.Preview.cs`, Flashback playback,
command, failure, cleanup, and stage trend text in
`PerformanceTimelineTools.Rendering.Trend.Flashback.cs`, Flashback export trend
text in `PerformanceTimelineTools.Rendering.Trend.Flashback.Export.cs`,
compact value and command-message formatting helpers in
`PerformanceTimelineTools.Formatting.cs`, preview jitter-depth and D3D
bottleneck formatting in `PerformanceTimelineTools.Formatting.Preview.cs`,
Flashback stage, cleanup, export, and byte-rate formatting in
`PerformanceTimelineTools.Formatting.Flashback.cs`, and
target summaries in `PerformanceTimelineTools.Summaries.cs`, with preview,
Flashback, and system pressure summaries in
`PerformanceTimelineTools.Summaries.Pressure.cs`.
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
`capture_presentmon_raw`, structured-content shape, and `PresentMonProbe.RunAsync`
invocation in `PresentMonTools.cs`; keep only the app-snapshot request and
malformed snapshot/pipe-failure fallback in `PresentMonTools.Correlation.cs`.
Shared option precedence and preview-present field extraction belong to
`tools/Common/PresentMon/PresentMonProbe.Options.cs`.

Diagnostic-session pipe retry/error classification now lives in
`tools/Common/DiagnosticSessionPipeRetryPolicy.cs`, keeping access-denied as a
permanent failure and connect failed/timeout responses retryable.

Diagnostic-session command sending now lives in
`tools/Common/DiagnosticSessionCommandChannel.cs`. It owns serialized command
execution, command failure accounting, and enum-backed command-name resolution
for fixed diagnostic-session commands.
`DiagnosticSessionCommandChannel.RawSending.cs` owns raw command send overloads,
connect-retry wrapping, and local failure-response fallback when connect retry
returns no response. Scenario setup and cleanup pass the channel itself for
lifecycle mutations so
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
`tools/Common/DiagnosticSessionRunContext.cs`. `DiagnosticSessionRunContext.cs`
owns core mutable per-run infrastructure: bootstrap, actions, warnings,
samples, run state, command channel, and scenario cancellation source.
`DiagnosticSessionRunContext.InitialSnapshot.cs` owns initial snapshot state and
capture, `DiagnosticSessionRunContext.LiveState.cs` owns live-state writer
handoff, and `DiagnosticSessionRunContext.Lifetime.cs` owns run-context
disposal. `DiagnosticSessionRunContext.PhaseContexts.cs` owns
scenario/completion context construction and the explicit callback/token
handoffs consumed by scenario and completion phases.

Diagnostic-session run state now lives in
`tools/Common/DiagnosticSessionRunState.cs`. It owns last-stage tracking,
terminal exception classification, and best-effort artifact write failure
recording while the runner keeps the scenario flow readable.

Diagnostic-session live breadcrumbs now live in
`tools/Common/DiagnosticSessionLiveStateWriter.cs`. It owns the
`session-live.json` path, payload shape, health and warning projection,
and terminal override mapping. `DiagnosticSessionLiveStateWriter.Sampling.cs`
owns the sampling live-state write throttle and delegates to the breadcrumb
writer.

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
registration, deterministic await order, and normal registered scenario
completion. `DiagnosticSessionBackgroundTasks.PresentMon.cs` owns PresentMon
task registration, normal completion, and interrupted-session observation,
`DiagnosticSessionBackgroundTasks.RecordingSettingsDeferred.cs` owns deferred
Flashback recording-settings registration, normal completion, and
interrupted-session observation, and
`DiagnosticSessionBackgroundTasks.Models.cs` owns the small background-task
registration and drain handoff records.
`DiagnosticSessionBackgroundTasks.FaultDrain.cs` owns interrupted drain
orchestration and generic scenario-task warning collection.

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
selection while delegating option/correlation policy to
`tools/Common/PresentMon/PresentMonProbe.Options.cs`; scenario startup keeps
scenario task registration.

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

Diagnostic-session scenario metadata now lives in the
`tools/Common/DiagnosticSessionScenarioCatalog.*.cs` family. The root catalog
owns scenario names, HelpList/Description text, normalization, requirement
queries, and export-verification lookup. `DiagnosticSessionScenarioCatalog.Entries.cs`
owns scenario ordering by spreading focused entry groups; `.Entries.Core.cs`,
`.Entries.FlashbackPlayback.cs`, `.Entries.FlashbackExport.cs`,
`.Entries.FlashbackRecording.cs`, and `.Entries.Combined.cs` own the setup
requirement metadata, export verification filenames, and plan assigned to each
normalized scenario group.
`tools/Common/DiagnosticSessionScenarioPlan.cs` owns the plan DTO, creation
factory, and catalog lookup handoff. Grouped warning/validation policy switches
now live in `tools/Common/DiagnosticSessionScenarioPlan.Policies.cs`, including
the preview-cycle grouped predicate, so the runner does not grow direct
scenario string comparisons.

Diagnostic-session cleanup restore validation now lives in
`tools/Common/DiagnosticSessionCleanupPolicy.cs`. It owns warnings for preview,
Flashback, and playback state that remain active after the runner attempts
cleanup.

Diagnostic-session Flashback cycle scenarios now live in named partial owners.
`DiagnosticSessionFlashbackCycleScenarios.Restart.cs` owns the restart-cycle
command choreography, playback priming, restart, buffer refill, and delegation
to focused validation/export owners. Restart-cycle post-restart active-state,
playback-worker, and pending-command warning policy live in `.RestartValidation.cs`,
while restart-cycle export request and verification live in `.RestartExport.cs`.
`DiagnosticSessionFlashbackCycleScenarios.Encoder.cs`
owns preset cycling and buffer-readiness command choreography. Encoder-cycle
post-cycle snapshot warnings live in `.EncoderValidation.cs`, export request and
verification live in `.EncoderExport.cs`, and original-preset restore plus
post-restore readiness live in `.EncoderRestore.cs`. `.Registrations.cs` owns
task registration, priority, task-label, and started-action wiring while startup
only delegates selected cycle scenario registration. Do not reintroduce an empty
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
family of named owners. Concurrent export, disable-during-export command
coordination, rotated export, export-during-playback command choreography, and
selection-range export flows each have their own named file.
Disable-during-export file verification and post-disable/re-enable state checks
live in `DiagnosticSessionFlashbackExportScenarios.DisableDuringExportValidation.cs`.
Export-during-playback validation is split by runtime phase:
`DiagnosticSessionFlashbackExportScenarios.PlaybackPreExport.cs` owns the
pre-export Playing sample, `.PlaybackPostExport.cs` owns post-export playback
continuity, and `.PlaybackFinalState.cs` owns final go-live command-health
validation.
`DiagnosticSessionFlashbackExportScenarios.Registrations.cs` owns export
scenario task registration orchestration while diagnostic-session startup makes
a single qualified call into that owner. Export playback registration lives in
`.Registrations.Playback.cs`, range/audio-switch registration lives in
`.Registrations.Range.cs`, and concurrent/disable/rotated coordination
registration lives in `.Registrations.Coordination.cs`. Do not reintroduce an
empty family root.

Diagnostic-session Flashback lifecycle checks now live in
`tools/Common/DiagnosticSessionFlashbackLifecycleScenarios*.cs`. The root owns
pause/seek/play disable-and-re-enable command flow, `.Registrations.cs` owns
task registration, priority, label, and started action, and `.Validation.cs`
owns post-disable playback-thread/queue health plus post-re-enable active-state
validation while startup only delegates to the lifecycle owner.

Diagnostic-session Flashback metric projection now lives in a focused partial
family of named owners. Recording, playback-session, playback-result, and
export DTO shapes have separate model owner files. Recording metrics, playback
session aggregation, playback result copying, and export metrics each have
named behavior owner files.
Playback observation keeps active/relevant snapshot gating in the root while
1% low capture, frame/decode maxima, and audio-master maxima live in focused
observation partials. Export metrics also own force-rotate fallback total,
delta, and last fallback segment count, derived outside export-observed
relevance gating. These helpers remain snapshot-only projections and must not
send automation commands. Do not reintroduce an empty family root.

MCP fixed command routes should use `AutomationCommandKind` overloads when the
command is part of the shared catalog. Keep this as an ownership rule, not a
per-route table: record only new file ownership or deliberate exceptions here.
String command names remain only for catalog/manifest-backed dynamic batches,
diagnostic-session command callbacks, and intentionally unconverted compatibility
surfaces with focused coverage.

Diagnostic-session Flashback preview-cycle scenarios now live in a focused
partial family. `.Registrations.cs` owns task registration, priority,
task-label, and started-action wiring while preview-cycle scenario selection
stays in the `DiagnosticSessionScenarioCatalog` family and grouped
preview-cycle policy stays in `DiagnosticSessionScenarioPlan.Policies.cs`.
`.Flashback.cs`, `.Playback.cs`, and `.Recording.cs` own preview stop/restart
command choreography for normal Flashback, playback, and recording-backed
diagnostics. Normal Flashback preview-cycle validation is split by runtime
phase: `.FlashbackPreStop.cs` owns pre-stop encoded-frame capture,
`.FlashbackStopped.cs` owns preview-off Flashback/encoder validation, and
`.FlashbackRestartValidation.cs` owns restart frame-flow validation.
Playback-preview-cycle validation is also split by runtime phase:
`.PlaybackPreStop.cs` owns pre-stop frame warmup, `.PlaybackStopped.cs` owns
preview-stopped state validation, and `.PlaybackRestart.cs` owns restart
frame-flow validation. Normal Flashback and playback-preview-cycle
export-while-preview-off verification live in `.FlashbackExport.cs` and
`.PlaybackExport.cs`. Recording-backed readiness and pre-stop encoder counter
capture live in `.RecordingCounters.cs`, preview-off recording/backend/counter
validation lives in `.RecordingValidation.cs`, and restart frame-flow validation
lives in `.RecordingRestartValidation.cs` while startup only delegates selected
scenario registration.

Diagnostic-session Flashback rejected-export scenarios now live in the
`tools/Common/DiagnosticSessionFlashbackRejectedExports*.cs` partial family.
The root owns selected-scenario dispatch, `.Inactive.cs` owns inactive-buffer
failure-kind and last-result assertions, and `.Recording.cs` owns
active-Flashback-recording failure-kind and backend-stability assertions.

Diagnostic-session Flashback recording-settings deferral now lives in named
partial owners. `DiagnosticSessionFlashbackRecordingSettingsScenarios.Models.cs`
owns deferred preset state. During-recording command choreography lives in
`DiagnosticSessionFlashbackRecordingSettingsScenarios.DuringRecording.cs`,
restart/disable rejection-message policy lives in
`DiagnosticSessionFlashbackRecordingSettingsScenarios.DuringRecordingRejections.cs`,
and active recording backend/file/counter stability checks live in
`DiagnosticSessionFlashbackRecordingSettingsScenarios.DuringRecordingValidation.cs`.
Post-stop preset verification and encoder-frame checks live in
`DiagnosticSessionFlashbackRecordingSettingsScenarios.PostStop.cs`, while
original-preset restore command execution and post-restore verification live in
`DiagnosticSessionFlashbackRecordingSettingsScenarios.PostStopRestore.cs`. Do not
reintroduce an empty family root.

Diagnostic-session Flashback segment playback now lives in
`tools/Common/DiagnosticSessionFlashbackSegmentPlaybackScenarios*.cs`. The root
owns completed-segment boundary-crossing choreography, `.Registrations.cs` owns
task registration, priority, label, and started action, `.Target.cs` owns
playback target acquisition plus recording-assisted retry routing,
`.LiveRestore.cs` owns go-live restore and final playback-state warning policy,
`.Validation.cs` owns post-boundary snapshot/FPS/command-health warning policy, and
recording-assisted segment rotation plus best-effort stop cleanup live beside it
in `DiagnosticSessionFlashbackSegmentPlaybackScenarios.RecordingAssist.cs`
while the `DiagnosticSessionFlashbackSegments.*` family stays read-only segment
parsing and wait policy.

Diagnostic-session Flashback segment handling now lives in a small read-only
family. `DiagnosticSessionFlashbackSegments.Models.cs` owns segment DTOs,
`.Parsing.cs` owns `FlashbackGetSegments` response parsing,
`.CompletedWaits.cs` owns completed-segment discovery waits,
`.PlaybackTargetWaits.cs` owns playable completed-segment target selection, and
`.PlaybackHeadroomWaits.cs` owns playback-boundary headroom polling while the
runner keeps scenario command sequencing.

Diagnostic-session Flashback snapshot waits now live in
`tools/Common/DiagnosticSessionFlashbackWaits*.cs`. The root owns read-only
polling loops for preview-active and Flashback-active state.
`DiagnosticSessionFlashbackWaits.RecordingReady.cs` owns Flashback-backed
recording readiness polling, `.BufferReady.cs` owns stress buffer readiness
polling, `DiagnosticSessionFlashbackWaits.Playback.cs` owns playback state
polling, `.PlaybackBoundary.cs` owns boundary-crossing polling,
`.PlaybackWarmSample.cs` owns warmed-playback frame-count/FPS polling, and
`.PlaybackPosition.cs` owns position convergence polling while the runner keeps
scenario command sequencing.

Diagnostic-session Flashback playback result metrics now keep final
`FlashbackPlaybackResultMetrics` construction in
`tools/Common/DiagnosticSessionFlashbackMetrics.PlaybackResult.cs`, with
end-snapshot reads split into named owners: `.PlaybackResult.Commands.cs` owns
command queue and command failure fields, `.PlaybackResult.Cadence.cs` owns
frame cadence and dropped-frame fields, `.PlaybackResult.Decode.cs` owns decode
timing and max-phase fields, `.PlaybackResult.AudioMaster.cs` owns audio-master
fallback fields, `.PlaybackResult.Stages.cs` owns playback stage counters and
seek-forward decode-cap deltas, and `.PlaybackResult.Projections.cs` owns the
private grouped handoff records. Preserve the final `init` DTO construction in
the root unless a broader construction pattern replaces it deliberately.

Diagnostic-session Flashback stress orchestration now lives in a focused
partial family. `tools/Common/DiagnosticSessionFlashbackStressScenario.cs` owns
stress thresholds and stress/scrub-stress task registration, `.Stress.cs` owns
the main stress command sequence, `.StressExport.cs` owns stress export request
and verification,
`.WarmPlayback.cs` owns warmed-playback frame/FPS/1% low checks and delegates
audio-master delta capture to `.WarmPlaybackAudio.cs`. `.CommandDrainWait.cs`
owns shared live/empty-queue drain polling for stress playback commands,
`.CommandDrain.cs` owns post-go-live playback command-health/latency/final-state
warning policy, `.Scrub.cs` owns scrub-stress command choreography,
`.ScrubUpdates.cs` owns concurrent scrub update-burst dispatch and failed-update
warning policy, `.ScrubDrain.cs` owns scrub-stress post-go-live
command-health/latency/final-state warning policy, and `.AudioMaster.cs` owns
warmed-playback audio-master fallback classification while the runner only
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
typed `AutomationCommandKind` command-id routing plus `not_ready` retry policy,
`AutomationCommandTransport.cs` owns command-specific timeout selection for
string and typed commands, shared response-element validation, synthetic error
shaping, and handoff to
`Sussudio.Automation.Contracts/AutomationUnknownCommandHandling.cs`,
`AutomationPipeClient.Commands.cs` owns tolerant response-state parsing handoff
to `Sussudio.Automation.Contracts/AutomationResponseState.cs`,
`Sussudio.Automation.Contracts/AutomationPipeClientModels.cs` owns the command
result handoff and pipe client exception taxonomy, and
`Sussudio.Automation.Contracts/AutomationSyntheticErrorResponse.cs` owns shared
structured error-envelope creation and common transport/protocol exception
mapping for the shared command transport.

PresentMon model ownership and result formatting are split from the probe runner.
`tools/Common/PresentMon/PresentMonProbe.Models.cs` owns PresentMon options, result,
summary, swap-chain, app-correlation summary, and metric DTOs.
`tools/Common/PresentMon/PresentMonProbe.Options.cs` owns the shared
`PresentMonProbeCorrelation` handoff, option precedence/defaulting, and
app-snapshot preview correlation field extraction.
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
orchestration, command-line construction, argument quoting, and probe-result message shaping.
`tools/Common/PresentMon/PresentMonProbe.Paths.cs` owns target process,
PresentMon executable, and output-path resolution. `tools/Common/PresentMon/PresentMonProbe.Process.cs`
owns process supervision, stdout/stderr drain, timeout kill, and temp CSV
cleanup.

EGAVDS audio probing keeps the CLI command flow, SetupAPI device lookup, and
audio input/gain actions in `tools/EgavdsAudioProbe/Program.cs`; SWIG callback
registration, EGAVDeviceSupport imports, SetupAPI imports, and native interface
DTOs live in `tools/EgavdsAudioProbe/Program.NativeInterop.cs`.

Remaining `tools/Common` ownership:

- `AutomationPipeClient/AutomationPipeClient.Transport.cs`
- `AutomationPipeClient/AutomationPipeClient.ConnectErrors.cs`
- `AutomationPipeClient/AutomationPipeClient.Commands.cs`
- `AutomationPipeClient/AutomationCommandTransport.cs`
- `DiagnosticSessionBackgroundTasks.cs`
- `DiagnosticSessionBackgroundTasks.PresentMon.cs`
- `DiagnosticSessionBackgroundTasks.RecordingSettingsDeferred.cs`
- `DiagnosticSessionBackgroundTasks.Models.cs`
- `DiagnosticSessionBackgroundTasks.FaultDrain.cs`
- `DiagnosticSessionCleanupActions.cs`
- `DiagnosticSessionCleanupActions.Recording.cs`
- `DiagnosticSessionCleanupActions.StateRestore.cs`
- `DiagnosticSessionCleanupPolicy.cs`
- `DiagnosticSessionRecordingChecks.cs`
- `DiagnosticSessionRecordingVerification.cs`
- `DiagnosticSessionFlashbackCycleScenarios.Restart.cs`
- `DiagnosticSessionFlashbackCycleScenarios.RestartValidation.cs`
- `DiagnosticSessionFlashbackCycleScenarios.RestartExport.cs`
- `DiagnosticSessionFlashbackCycleScenarios.Encoder.cs`
- `DiagnosticSessionFlashbackCycleScenarios.EncoderValidation.cs`
- `DiagnosticSessionFlashbackCycleScenarios.EncoderExport.cs`
- `DiagnosticSessionFlashbackCycleScenarios.EncoderRestore.cs`
- `DiagnosticSessionFlashbackCycleScenarios.Registrations.cs`
- `DiagnosticSessionFlashbackExports.cs`
- `DiagnosticSessionFlashbackExports.AudioSwitch.cs`
- `DiagnosticSessionFlashbackExportScenarios.Concurrent.cs`
- `DiagnosticSessionFlashbackExportScenarios.DisableDuringExport.cs`
- `DiagnosticSessionFlashbackExportScenarios.DisableDuringExportValidation.cs`
- `DiagnosticSessionFlashbackExportScenarios.Playback.cs`
- `DiagnosticSessionFlashbackExportScenarios.PlaybackPreExport.cs`
- `DiagnosticSessionFlashbackExportScenarios.PlaybackPostExport.cs`
- `DiagnosticSessionFlashbackExportScenarios.PlaybackFinalState.cs`
- `DiagnosticSessionFlashbackExportScenarios.RangeCleanup.cs`
- `DiagnosticSessionFlashbackExportScenarios.Range.cs`
- `DiagnosticSessionFlashbackExportScenarios.RangeSelection.cs`
- `DiagnosticSessionFlashbackExportScenarios.RangeSelection.Markers.cs`
- `DiagnosticSessionFlashbackExportScenarios.RangeSelection.Models.cs`
- `DiagnosticSessionFlashbackExportScenarios.RangeValidation.cs`
- `DiagnosticSessionFlashbackExportScenarios.Registrations.cs`
- `DiagnosticSessionFlashbackExportScenarios.Registrations.Playback.cs`
- `DiagnosticSessionFlashbackExportScenarios.Registrations.Range.cs`
- `DiagnosticSessionFlashbackExportScenarios.Registrations.Coordination.cs`
- `DiagnosticSessionFlashbackExportScenarios.Rotated.cs`
- `DiagnosticSessionFlashbackLifecycleScenarios.cs`
- `DiagnosticSessionFlashbackLifecycleScenarios.Registrations.cs`
- `DiagnosticSessionFlashbackLifecycleScenarios.Validation.cs`
- `DiagnosticSessionFlashbackMetrics.Export.cs`
- `DiagnosticSessionFlashbackMetrics.Models.Recording.cs`
- `DiagnosticSessionFlashbackMetrics.Models.PlaybackSession.cs`
- `DiagnosticSessionFlashbackMetrics.Models.PlaybackResult.cs`
- `DiagnosticSessionFlashbackMetrics.Models.Export.cs`
- `DiagnosticSessionFlashbackMetrics.PlaybackObservation.cs`
- `DiagnosticSessionFlashbackMetrics.PlaybackObservation.OnePercentLow.cs`
- `DiagnosticSessionFlashbackMetrics.PlaybackObservation.FrameDecode.cs`
- `DiagnosticSessionFlashbackMetrics.PlaybackObservation.AudioMaster.cs`
- `DiagnosticSessionFlashbackMetrics.PlaybackResult.cs`
- `DiagnosticSessionFlashbackMetrics.PlaybackResult.Commands.cs`
- `DiagnosticSessionFlashbackMetrics.PlaybackResult.Cadence.cs`
- `DiagnosticSessionFlashbackMetrics.PlaybackResult.Decode.cs`
- `DiagnosticSessionFlashbackMetrics.PlaybackResult.AudioMaster.cs`
- `DiagnosticSessionFlashbackMetrics.PlaybackResult.Stages.cs`
- `DiagnosticSessionFlashbackMetrics.PlaybackResult.Projections.cs`
- `DiagnosticSessionFlashbackMetrics.PlaybackSession.cs`
- `DiagnosticSessionFlashbackMetrics.Recording.cs`
- `DiagnosticSessionFlashbackPreviewCycleScenarios.Registrations.cs`
- `DiagnosticSessionFlashbackPreviewCycleScenarios.Flashback.cs`
- `DiagnosticSessionFlashbackPreviewCycleScenarios.FlashbackPreStop.cs`
- `DiagnosticSessionFlashbackPreviewCycleScenarios.FlashbackStopped.cs`
- `DiagnosticSessionFlashbackPreviewCycleScenarios.FlashbackRestartValidation.cs`
- `DiagnosticSessionFlashbackPreviewCycleScenarios.FlashbackExport.cs`
- `DiagnosticSessionFlashbackPreviewCycleScenarios.Playback.cs`
- `DiagnosticSessionFlashbackPreviewCycleScenarios.PlaybackPreStop.cs`
- `DiagnosticSessionFlashbackPreviewCycleScenarios.PlaybackStopped.cs`
- `DiagnosticSessionFlashbackPreviewCycleScenarios.PlaybackRestart.cs`
- `DiagnosticSessionFlashbackPreviewCycleScenarios.PlaybackExport.cs`
- `DiagnosticSessionFlashbackPreviewCycleScenarios.Recording.cs`
- `DiagnosticSessionFlashbackPreviewCycleScenarios.RecordingCounters.cs`
- `DiagnosticSessionFlashbackPreviewCycleScenarios.RecordingValidation.cs`
- `DiagnosticSessionFlashbackPreviewCycleScenarios.RecordingRestartValidation.cs`
- `DiagnosticSessionFlashbackRejectedExports.cs`
- `DiagnosticSessionFlashbackRejectedExports.Inactive.cs`
- `DiagnosticSessionFlashbackRejectedExports.Recording.cs`
- `DiagnosticSessionFlashbackRecordingSettingsScenarios.Models.cs`
- `DiagnosticSessionFlashbackRecordingSettingsScenarios.DuringRecording.cs`
- `DiagnosticSessionFlashbackRecordingSettingsScenarios.DuringRecordingRejections.cs`
- `DiagnosticSessionFlashbackRecordingSettingsScenarios.DuringRecordingValidation.cs`
- `DiagnosticSessionFlashbackRecordingSettingsScenarios.PostStop.cs`
- `DiagnosticSessionFlashbackRecordingSettingsScenarios.PostStopRestore.cs`
- `DiagnosticSessionFlashbackSegmentPlaybackScenarios.cs`
- `DiagnosticSessionFlashbackSegmentPlaybackScenarios.Registrations.cs`
- `DiagnosticSessionFlashbackSegmentPlaybackScenarios.Target.cs`
- `DiagnosticSessionFlashbackSegmentPlaybackScenarios.LiveRestore.cs`
- `DiagnosticSessionFlashbackSegmentPlaybackScenarios.Validation.cs`
- `DiagnosticSessionFlashbackSegmentPlaybackScenarios.RecordingAssist.cs`
- `DiagnosticSessionFlashbackSegments.CompletedWaits.cs`
- `DiagnosticSessionFlashbackSegments.PlaybackTargetWaits.cs`
- `DiagnosticSessionFlashbackSegments.PlaybackHeadroomWaits.cs`
- `DiagnosticSessionFlashbackSegments.Models.cs`
- `DiagnosticSessionFlashbackSegments.Parsing.cs`
- `DiagnosticSessionFlashbackStressScenario.cs`
- `DiagnosticSessionFlashbackStressScenario.Stress.cs`
- `DiagnosticSessionFlashbackStressScenario.StressExport.cs`
- `DiagnosticSessionFlashbackStressScenario.WarmPlayback.cs`
- `DiagnosticSessionFlashbackStressScenario.WarmPlaybackAudio.cs`
- `DiagnosticSessionFlashbackStressScenario.CommandDrainWait.cs`
- `DiagnosticSessionFlashbackStressScenario.CommandDrain.cs`
- `DiagnosticSessionFlashbackStressScenario.Scrub.cs`
- `DiagnosticSessionFlashbackStressScenario.ScrubUpdates.cs`
- `DiagnosticSessionFlashbackStressScenario.ScrubDrain.cs`
- `DiagnosticSessionFlashbackStressScenario.AudioMaster.cs`
- `DiagnosticSessionFlashbackWaits.cs`
- `DiagnosticSessionFlashbackWaits.RecordingReady.cs`
- `DiagnosticSessionFlashbackWaits.BufferReady.cs`
- `DiagnosticSessionFlashbackWaits.Playback.cs`
- `DiagnosticSessionFlashbackWaits.PlaybackBoundary.cs`
- `DiagnosticSessionFlashbackWaits.PlaybackWarmSample.cs`
- `DiagnosticSessionFlashbackWaits.PlaybackPosition.cs`
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
- `DiagnosticSessionResult.Overview.cs`
- `DiagnosticSessionResult.CaptureSource.cs`
- `DiagnosticSessionResult.PreviewCadence.cs`
- `DiagnosticSessionResult.PreviewScheduler.cs`
- `DiagnosticSessionResult.PreviewD3D.cs`
- `DiagnosticSessionResult.PreviewVisualCadence.cs`
- `DiagnosticSessionResult.FlashbackPlayback.Commands.cs`
- `DiagnosticSessionResult.FlashbackPlayback.Cadence.cs`
- `DiagnosticSessionResult.FlashbackPlayback.Decode.cs`
- `DiagnosticSessionResult.FlashbackPlayback.AudioMaster.cs`
- `DiagnosticSessionResult.FlashbackPlayback.Stage.cs`
- `DiagnosticSessionResult.FlashbackRecording.cs`
- `DiagnosticSessionResult.FlashbackExport.cs`
- `DiagnosticSessionSample.cs`
- `DiagnosticSessionPipeRetryPolicy.cs`
- `DiagnosticSessionCommandChannel.cs`
- `DiagnosticSessionCommandChannel.RawSending.cs`
- `DiagnosticSessionPostRunSnapshots.cs`
- `DiagnosticSessionResultArtifacts.cs`
- `DiagnosticSessionResultBuilder.cs`
- `DiagnosticSessionResultBuilder.Flattening.cs`
- `DiagnosticSessionResultBuilder.OverviewResult.cs`
- `DiagnosticSessionResultBuilder.Analysis.cs`
- `DiagnosticSessionResultBuilder.AnalysisValidation.cs`
- `DiagnosticSessionResultBuilder.FlashbackWarnings.cs`
- `DiagnosticSessionResultBuilder.DiagnosticHealth.cs`
- `DiagnosticSessionResultBuilder.DiagnosticHealthSummary.cs`
- `DiagnosticSessionResultBuilder.DiagnosticHealthSourceWarnings.cs`
- `DiagnosticSessionResultBuilder.FlashbackPlaybackResult.cs`
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
- `DiagnosticSessionResultBuilder.PreviewResult.cs`
- `DiagnosticSessionResultBuilder.PreviewVisualCadenceResult.cs`
- `DiagnosticSessionResultBuilder.PreviewD3DResult.cs`
- `DiagnosticSessionResultBuilder.Models.cs`
- `DiagnosticSessionResultFormatter.cs`
- `DiagnosticSessionResultFormatter.Overview.cs`
- `DiagnosticSessionResultFormatter.CaptureMode.cs`
- `DiagnosticSessionResultFormatter.RecordingVerification.cs`
- `DiagnosticSessionResultFormatter.PresentMon.cs`
- `DiagnosticSessionResultFormatter.ProcessPerformance.cs`
- `DiagnosticSessionResultFormatter.Flashback.cs`
- `DiagnosticSessionResultFormatter.FlashbackRecording.cs`
- `DiagnosticSessionResultFormatter.FlashbackExport.cs`
- `DiagnosticSessionResultFormatter.FlashbackPlayback.Performance.cs`
- `DiagnosticSessionResultFormatter.FlashbackPlayback.Decode.cs`
- `DiagnosticSessionResultFormatter.Preview.cs`
- `DiagnosticSessionResultFormatter.PreviewD3D.cs`
- `DiagnosticSessionResultFormatter.PreviewVisualCadence.cs`
- `DiagnosticSessionResultFormatter.Artifacts.cs`
- `DiagnosticSessionRunState.cs`
- `DiagnosticSessionLiveStateWriter.cs`
- `DiagnosticSessionLiveStateWriter.Sampling.cs`
- `DiagnosticSessionRunBootstrap.cs`
- `DiagnosticSessionRunContext.InitialSnapshot.cs`
- `DiagnosticSessionRunContext.LiveState.cs`
- `DiagnosticSessionRunContext.Lifetime.cs`
- `DiagnosticSessionRunContext.PhaseContexts.cs`
- `DiagnosticSessionSampler.cs`
- `DiagnosticSessionScenarioCatalog.cs`
- `DiagnosticSessionScenarioCatalog.Entries.cs`
- `DiagnosticSessionScenarioCatalog.Entries.Core.cs`
- `DiagnosticSessionScenarioCatalog.Entries.FlashbackPlayback.cs`
- `DiagnosticSessionScenarioCatalog.Entries.FlashbackExport.cs`
- `DiagnosticSessionScenarioCatalog.Entries.FlashbackRecording.cs`
- `DiagnosticSessionScenarioCatalog.Entries.Combined.cs`
- `DiagnosticSessionScenarioPlan.cs`
- `DiagnosticSessionScenarioPlan.Policies.cs`
- `DiagnosticSessionScenarioSetup.cs`
- `DiagnosticSessionScenarioStartup.cs`
- `DiagnosticSessionScenarioStartup.Registrations.cs`
- `DiagnosticSessionScenarioStartup.Playback.cs`
- `DiagnosticSessionPresentMonStartup.cs`
- `DiagnosticSessionOptionalTextFormatter.cs`
- `DiagnosticSessionRunner.cs`
- `DiagnosticSessionRunExecution.cs`
- `DiagnosticSessionRunExecution.Completion.cs`
- `DiagnosticSessionRunExecution.CompletionContext.cs`
- `DiagnosticSessionRunExecution.ResultBuildRequest.cs`
- `DiagnosticSessionScenarioPhaseRunner.cs`
- `DiagnosticSessionScenarioPhaseRunner.Models.cs`
- `ToolJsonOptions.cs`
- `tools/Common/PresentMon/PresentMonProbe.cs`
- `tools/Common/PresentMon/PresentMonProbe.Paths.cs`
- `tools/Common/PresentMon/PresentMonProbe.Process.cs`

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

   `tools/Common/DiagnosticSessionRunner.cs` is now the small public wrapper,
   while `tools/Common/DiagnosticSessionRunExecution.cs` owns the visible run
   phase sequence and `tools/Common/DiagnosticSessionRunContext.cs` owns the
   core mutable per-run infrastructure, with
   `tools/Common/DiagnosticSessionRunContext.InitialSnapshot.cs`,
   `tools/Common/DiagnosticSessionRunContext.LiveState.cs`, and
   `tools/Common/DiagnosticSessionRunContext.Lifetime.cs` owning snapshot,
   live-state, and disposal responsibilities, and
   `tools/Common/DiagnosticSessionRunContext.PhaseContexts.cs` owning explicit
   scenario/completion context construction. `DiagnosticSessionRunExecution.Completion.cs` owns the
   post-cleanup evidence/result sequence, with
   `DiagnosticSessionRunExecution.CompletionContext.cs` owning the completion
   context handoff and `DiagnosticSessionRunExecution.ResultBuildRequest.cs`
   owning result-build request mapping, while
   `DiagnosticSessionScenarioPhaseRunner.cs` owns the main scenario execution
   phase. `DiagnosticSessionScenarioPhaseRunner.Models.cs`
   owns the explicit scenario context/state/result handoff, with
   `DiagnosticSessionScenarioPhaseRunner.Sampling.cs` owning sampling and
   `DiagnosticSessionScenarioPhaseCompletion.cs` owning post-sampling
   completion ordering and fault-drain delegation while background task
   completion lives in `DiagnosticSessionBackgroundTasks.cs`. Scenario catalog,
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

   `tests/Sussudio.Tests/Program.cs` should keep the legacy runner entry point,
   and `tests/Sussudio.Tests/HarnessCheckCatalog.cs` is currently empty. Keep
   the executable runner as the offline `dotnet exec` validation shim until the
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
   `tests/Sussudio.Tests/XUnit.RecordingPipelineContractsTests.cs` after their
   removal from the legacy harness catalog. Recording model execution checks now
   run through `tests/Sussudio.Tests/XUnit.RecordingModelContractsTests.cs`
   after their removal from the legacy harness catalog. D3D preview renderer tests are split
   into geometry, cadence, diagnostics-contract, source-ownership marker plus
   ContractsAndMetrics/RenderPipeline/RuntimeCapture owners, device-lost, and
   frame-flow owners. Automation tool contract tests are split into
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
   `tests/Sussudio.Tests/XUnit.PresentationPreviewStartupOwnershipContractsTests.cs`
   owns xUnit execution for the preview-startup source-shape ownership checks
   after their removal from the legacy presentation-preview capture catalog.
   `tests/Sussudio.Tests/XUnit.PresentationPreviewStartupBehaviorContractsTests.cs`
   owns xUnit execution for the preview-startup controller behavior checks after
   their removal from the legacy presentation-preview capture catalog.
   `tests/Sussudio.Tests/XUnit.PresentationPreviewStartupSignalContractsTests.cs`
   owns xUnit execution for the preview-startup signal and failure-text checks
   after their removal from the legacy presentation-preview capture catalog.
   `tests/Sussudio.Tests/XUnit.PresentationPreviewCapturePreviewLifecycleContractsTests.cs`
   owns xUnit execution for the capture preview-lifecycle/audio-fallback checks
   after their removal from the legacy presentation-preview capture catalog.
   Preview startup ordering coverage is split between lifecycle-event
   ownership, device-discovery ordering, reveal priming, and stop audio-ramp
   owners. `tests/Sussudio.Tests/XUnit.PresentationPreviewStartupOrderingContractsTests.cs`
   owns xUnit execution for the former legacy presentation-preview capture
   catalog's final ordering checks, and the legacy catalog hook is removed.
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
   state, bounded ring-buffer storage, and snapshot projection live in
   `Sussudio/ViewModels/AudioRampTraceRecorder.cs`. Trace session start/complete,
   trace-point capture, sampler loop, and delayed sampler shutdown live in
   `Sussudio/ViewModels/AudioRampTraceRecorder.Capture.cs`, with
   `Sussudio/ViewModels/MainViewModel.AudioRampTrace.cs` kept as the
   automation-facing adapter and trace/preview-volume controller wiring owner;
   keep preview monitoring
   coordinator sequencing in
   `MainViewModel.AudioMonitoring.cs`, audio capture/audio-preview property
   handlers in `MainViewModel.AudioPropertyChanges.cs`, while custom audio-input property
   handlers, retargeting, and preview-monitoring ramp handoff live in
   `MainViewModel.AudioInputSelection.cs`.
   Microphone endpoint volume synchronization and persistence now live in
   `MainViewModel.MicrophoneVolume.cs`; device-native audio request lifetime,
   selected-device refresh, mode request scheduling, shared debounce CTS fields,
   cancellation cleanup, and graph-built context ports now live in
   `Sussudio/Controllers/ViewModel/MainViewModelDeviceAudioRequestController.cs`;
   analog-gain request scheduling, UI/XU debounce, and flash-persist debounce
   live in
   `Sussudio/Controllers/ViewModel/MainViewModelDeviceAudioRequestController.Gain.cs`;
   device-native
   audio-control support probing, readback, and pending saved-state reconciliation
   now live in `MainViewModel.DeviceAudioRefresh.cs`; mode switching and failure
   readback live in `MainViewModel.DeviceAudioMode.cs`; shared audio-control
   guards stay in `MainViewModel.AudioControls.cs`, while analog gain writes
   live in `MainViewModel.AnalogAudioGain.cs`. UI-facing state is
   split by owner: `MainViewModel.State.cs` owns shared shell/status/live-info
   flags, native window handle state, UI collection replacement, and
   non-preview coordination gates, `MainViewModel.PreviewState.cs`
   owns preview lifecycle compatibility entry points, preview-sink handoff,
   preview lifecycle flags, preview reinitialize coordination, and preview
   request events, `MainViewModel.CaptureState.cs` owns capture-selection,
   source, and HDR state, `MainViewModel.AudioState.cs` owns audio/microphone
   state, `MainViewModel.DeviceAudioState.cs` owns device-native audio/XU UI
   state, and `MainViewModel.FlashbackState.cs` owns Flashback timeline/export
   state. Keep the root `MainViewModel.cs` focused on the
   compatibility facade, dependency assignment, startup timing, and small
   bridge methods. `Sussudio/Controllers/ViewModel/MainViewModelControllerGraph.cs`
   owns controller graph construction order, while
   `Sussudio/Controllers/ViewModel/MainViewModelControllerGraph.UiDispatch.cs`
   owns UI-dispatch graph ports,
   `Sussudio/Controllers/ViewModel/MainViewModelControllerGraph.Presentation.cs`
   owns preview lifecycle graph ports,
   `Sussudio/Controllers/ViewModel/MainViewModelControllerGraph.CaptureSettingsAutomation.cs`
   owns capture settings automation graph ports, and
   `Sussudio/Controllers/ViewModel/MainViewModelControllerGraph.CaptureModes.cs`
   owns capture option rebuild graph ports, and
   `Sussudio/Controllers/ViewModel/MainViewModelControllerGraph.DeviceAudio.cs`
   owns device-native audio request graph ports, and
   `Sussudio/Controllers/ViewModel/MainViewModelControllerGraph.DeviceFormatProbe.cs`
   owns late format probe graph ports, and
   `Sussudio/Controllers/ViewModel/MainViewModelControllerGraph.Device.cs`
   owns device refresh graph ports, and
   `Sussudio/Controllers/ViewModel/MainViewModelControllerGraph.SourceTelemetry.cs`
   owns source telemetry graph ports, and
   `Sussudio/Controllers/ViewModel/MainViewModelControllerGraph.RecordingCapability.cs`
   owns recording capability graph ports, and
   `Sussudio/Controllers/ViewModel/MainViewModelControllerGraph.RecordingSettingsAutomation.cs`
   owns recording settings automation graph ports, and
   `Sussudio/Controllers/ViewModel/MainViewModelControllerGraph.Recording.cs`
   owns recording transition graph ports, and
   `Sussudio/Controllers/ViewModel/MainViewModelControllerGraph.RuntimeDisposal.cs`
   owns disposal graph ports, and
   `Sussudio/Controllers/ViewModel/MainViewModelControllerGraph.RuntimeEventIngress.cs`
   owns runtime event-ingress graph ports, and
   `Sussudio/Controllers/ViewModel/MainViewModelControllerGraph.Runtime.cs`
   owns runtime lifecycle graph ports.
   `MainViewModelDependencies.cs` continues to own service construction. Audio capture/preview property handlers now live in
   `MainViewModel.AudioPropertyChanges.cs`, microphone monitor/device
   selection handlers live in `MainViewModel.MicrophonePropertyChanges.cs`,
   capture-mode property handlers live in `MainViewModel.CaptureModePropertyChanges.cs`. Shared
   view-model UI dispatcher enqueue/invoke policy now lives in
   `Sussudio/Controllers/ViewModel/MainViewModelUiDispatchController.cs`.
   The UI dispatch graph-port contract for dispatcher access, disposal state,
   logging, exception logging, and status text projection lives in
   `Sussudio/Controllers/ViewModel/MainViewModelUiDispatchController.Context.cs`, while
   `MainViewModel.Dispatching.cs` keeps the stable private adapter names and
   preview event fan-out;
   periodic timer refresh orchestration and initial
   source-telemetry/HDR/live-info/timer/disk-space bootstrap through
   graph-built context ports now live in
   `Sussudio/Controllers/ViewModel/MainViewModelRuntimeLifecycleController.cs`,
   The runtime lifecycle graph-port contract for timer creation, runtime
   snapshot sampling, telemetry bootstrap, live-info/HDR projection, recording
   stats refresh, Flashback bitrate refresh, disk-space refresh, and watcher
   disposal lives in
   `Sussudio/Controllers/ViewModel/MainViewModelRuntimeLifecycleController.Context.cs`,
   while runtime event handling through graph-built context ports now lives in
   `Sussudio/Controllers/ViewModel/MainViewModelRuntimeEventIngressController.cs`
   for system-resume preview rebind handling, audio-device-invalidated rebind
   scheduling through the preview lifecycle owner, capture status/error fan-out,
   capture pre-cleanup renderer stop fan-out, and frame-captured callbacks.
   The runtime event ingress graph-port contract now lives in
   `Sussudio/Controllers/ViewModel/MainViewModelRuntimeEventIngressController.Context.cs`
   so handler logic and subscription wiring consume the same explicit port
   surface without owning construction details.
   Runtime event subscription/unsubscription ordering through graph-built
   context ports now lives in
   `Sussudio/Controllers/ViewModel/MainViewModelRuntimeEventIngressController.Subscriptions.cs`,
   output drive free-space assignment now lives in
   `MainViewModel.RecordingRuntime.cs`, while output drive probing,
   fallback, formatting, and suppressed-warning logging now live in
   `OutputDriveSpacePresentationBuilder.cs`. Recording size/bitrate label
   assignment and recording-state reset reactions also live in
   `MainViewModel.RecordingRuntime.cs`, while
   `Sussudio/ViewModels/BitrateSampleWindow.cs` owns bounded byte-sample
   smoothing shared by recording and Flashback bitrate presentation, and
   capture presentation adapters now live in
   `MainViewModel.CapturePresentation.cs`: live-capture info projection from
   runtime snapshots, audio-preview activity, live resolution/frame-rate/pixel-format
   assignment, preview-stop live-info reset, HDR runtime state/readiness
   projection, target-summary property application, and auto-resolution display
   text; live-signal label formatting now lives in
   `Sussudio/ViewModels/LiveSignalTextPresentationBuilder.cs`. Capture
   settings projection from UI/runtime state is sampled by
   `MainViewModel.CaptureSettings.cs` and projected by
   `Sussudio/ViewModels/CaptureSettingsProjectionBuilder.cs`, which owns final
   `CaptureSettings` assembly and audio/microphone device application. Pure
   projection policy and input DTOs now live in
   `Sussudio/ViewModels/CaptureSettingsProjectionBuilder.Policy.cs`:
   selected-option seeding, auto-resolved effective FPS, runtime/source rational
   overrides, rational/decimal fallbacks, requested pixel format, and MJPEG
   decode forcing.
   `MainViewModel.PreviewState.cs` keeps the stable compatibility facade entry
   points for device initialization, preview start/stop, selected-device apply,
   and preview reinitialization. Preview lifecycle
   implementation now lives in
   `Sussudio/Controllers/ViewModel/MainViewModelPreviewLifecycleController.cs`:
   device initialization, preview start/stop, selected-device apply, and the
   reinitialize facade. The preview lifecycle graph-port contract now lives in
   `Sussudio/Controllers/ViewModel/MainViewModelPreviewLifecycleController.Context.cs`
   for preview state/events, capture/session operations, source telemetry
   refresh, UI dispatch, audio-preview activity, and preview-volume ramp-down.
   Sibling ViewModel controllers receive that preview
   lifecycle owner directly from `MainViewModelControllerGraph` instead of
   routing controller-to-controller calls back through the root facade.
   `Sussudio/Controllers/ViewModel/MainViewModelPreviewReinitializeController.cs`
   owns debounced reinitialization, restart-cancellation state,
   Flashback-cycle wait-before-reinit, renderer-stop handoff, teardown restart,
   and gate release.
   `Sussudio/Controllers/ViewModel/MainViewModelPreviewReinitializeController.Context.cs`
   owns the graph-built reinitialization port contract for selected
   device/format state, generation coalescing, pending Flashback-cycle waits,
   renderer notifications, restart cancellation, and reinit gate access.
   Output folder display plus browse/open-recordings button workflows now live in
   `Sussudio/Controllers/Recording/Output/OutputPathController.cs`.
   Recording facade entry points, including the direct emergency-stop
   coordinator bridge, now live in `MainViewModel.RecordingState.cs`, while
   recording toggle serialization,
   desired-state routing, graceful stop, transition gating, and in-flight
   transition wait/error propagation now live in
   `Sussudio/Controllers/ViewModel/MainViewModelRecordingTransitionController.cs`;
   `Sussudio/Controllers/ViewModel/MainViewModelRecordingTransitionController.Context.cs`
   owns the recording transition graph-port contract for UI dispatch,
   recording/session state, capture settings construction, coordinator start/stop
   calls, recording timer state, and status/count presentation updates;
   concrete start/stop operation execution plus failure/cancellation state
   repair through graph-built context ports, including direct use of the preview
   lifecycle owner for recording startup initialization, live in
   `Sussudio/Controllers/ViewModel/MainViewModelRecordingTransitionController.Operations.cs`.
   Recording option selections, output path, counters, and transition flags also
   live in `MainViewModel.RecordingState.cs`. Bounded teardown, dispose timeout policy,
   watcher disposal, coordinator cleanup/dispose, and capture-service
   async-dispose fallback through graph-built context ports now live in
   `Sussudio/Controllers/ViewModel/MainViewModelDisposalController.cs`.
   The disposal graph-port contract for one-shot disposal entry, teardown
   cancellations, runtime stop, coordinator cleanup/dispose, and capture-service
   async/sync disposal fallback lives in
   `Sussudio/Controllers/ViewModel/MainViewModelDisposalController.Context.cs`.
   `MainViewModel.Disposal.cs` remains the public dispose adapter and active
   Flashback export cancellation owner. Automation-facing capture runtime, health,
   recording snapshot projection, source/preview probes, and preview
   frame capture also live in `MainViewModel.AutomationSnapshots.cs`; automation-facing
   view-model runtime snapshot UI-thread capture now lives in
   `MainViewModel.ViewModelRuntimeSnapshot.cs`; pure view-model runtime snapshot DTO
   construction lives in `ViewModelRuntimeSnapshotBuilder.cs`;
   automation options UI-thread snapshot capture now lives in
   `MainViewModel.AutomationOptionsSnapshot.cs`; pure selected-control-state DTO
   construction lives in `AutomationOptionsSnapshotBuilder.cs`.
   `tests/Sussudio.Tests/XUnit.AutomationDiagnosticsLoopContractsTests.cs`
   owns xUnit execution for the diagnostics-loop polling check after its removal
   from the legacy presentation-preview capture catalog.
   Flashback playback, scrub, nudge, marker, and automation action command routing
   now live in `MainViewModel.FlashbackPlaybackCommands.cs`; read-only
   Flashback playback snapshot access plus rejection status projection, buffer,
   bitrate, playback-state, in/out marker, gap-from-live UI projection, and
   read-only segment projection for UI, CLI, and MCP callers live in
   `MainViewModel.FlashbackPlayback.cs`.
   Flashback UI export commands, save-picker flow, active-export guard, and
   user-facing export result/status handling now live in
   `MainViewModel.FlashbackExport.cs`. Shared Flashback export operation
   lifecycle, including outcome classification, core export execution,
   current-operation checks, progress/cancellation handoff, and CTS cleanup,
   now lives in `MainViewModel.FlashbackExportOperation.cs`.
   Automation-facing Flashback export command execution, linked cancellation,
   and dispatcher cleanup now live in
   `MainViewModel.FlashbackExportAutomation.cs`. Frame-rate selection reactions and
   auto-selection entry points now live in `MainViewModel.FrameRateOptions.cs`.
   `MainViewModel.CaptureModeTransactions.cs` keeps the resolution, frame-rate,
   selected-format, and video-format rebuild compatibility adapters, while
   frame-rate option rebuilding and observable collection mutation through graph-built context ports live in
   `Sussudio/Controllers/ViewModel/MainViewModelCaptureModeOptionRebuildController.FrameRate.cs`. Pure
   frame-rate option choice, including pending SDR bucket preference,
   Source-rate nearest match with timing-family tie-break, generic auto fallback,
   and previous/manual selection fallback, now lives in
   `MainViewModel.FrameRateAutoSelectionPolicy.cs`. The ownership checks for
   frame-rate source filtering, automatic selection, `ShowAllCaptureOptions`, and
   timing-policy placement live in
   `MainViewModel.Capture.SelectionPolicy.FrameRates.Ownership.Tests.cs`, while
   automatic-selection and pure timing-policy behavior checks live in
   `MainViewModel.Capture.SelectionPolicy.FrameRates.PolicyBehavior.Tests.cs`.
   `tests/Sussudio.Tests/XUnit.PresentationPreviewFrameRateSelectionContractsTests.cs`
   owns xUnit execution for those frame-rate selection/timing checks after
   their removal from the legacy presentation-preview capture catalog.
   Shared frame-rate selection reset,
   resolved automatic frame-rate application, disabled frame-rate reason
   projection, and capture-mode reset flags live in
   `MainViewModel.ModeSelectionState.cs`. Source-rate filtering and
   `ShowAllCaptureOptions` unlock policy live in
   `MainViewModel.FrameRateSourceFilterPolicy.cs`, while `ShowAllCaptureOptions`
   change handling, deferred rebuild behavior, duplicate-reinit suppression,
   and the active capture-mode automation gate live in
   `MainViewModel.CaptureModeTransactions.cs`. Pure frame-rate timing family,
   timing-variant projection, rational parsing, friendly/exact frame-rate
   matching, and preferred-format ranking now live in
   `Sussudio/ViewModels/FrameRateTimingPolicy.cs`, while
   `MainViewModel.FrameRateTiming.cs` keeps the stateful wrappers over
   resolution capabilities, runtime snapshots, source telemetry, selected
   formats, and UI selection state;
   the root `MainViewModel.cs` keeps the public capture-device refresh
   compatibility facade, while
   `Sussudio/Controllers/ViewModel/MainViewModelDeviceRefreshController.cs`
   owns startup refresh orchestration: requesting the combined discovery result,
   applying audio-device startup selection, replacing the capture-device collection,
   starting background format probes, restoring saved capture-device selection,
   and directly auto-starting preview through the preview lifecycle owner.
   `Sussudio/Controllers/ViewModel/MainViewModelDeviceRefreshController.Context.cs`
   owns the device-refresh graph-port contract for discovery, startup audio
   selection, device collection mutation, background format probes, selection
   restore, and scan status projection. The shallow `MainViewModel.DeviceManagement.cs`
   partial was retired rather than preserving a sub-100-line facade. Selected
   capture-device reactions, capability projection, source telemetry reset, and
   device-native audio-control refresh handoff live in `MainViewModel.DeviceSelection.cs`. Capture-mode property-change
   hooks live in `MainViewModel.CaptureModePropertyChanges.cs` and startup
   audio-list and watcher-driven audio endpoint refresh adaptation live in
   `MainViewModel.AudioDeviceDiscovery.cs`. Pure audio-device filtering and
   previous/saved/default audio and microphone selection fallback policy now
   lives in `Sussudio/ViewModels/AudioDeviceSelectionPolicy.cs`. Pure
   recording codec filtering, selected-codec fallback policy, string-to-model
   format/quality parsing, and custom bitrate clamp policy now live in
   `Sussudio/ViewModels/RecordingSettingsSelectionPolicy.cs`, while startup
   FFmpeg capability probes and observable recording-format option mutation through graph-built context ports live
   in `Sussudio/Controllers/ViewModel/MainViewModelRecordingCapabilityController.cs`. `MainViewModel.CaptureModeTransactions.cs`
   keeps selected-format and video-format rebuild compatibility adapters, while
   `Sussudio/Controllers/ViewModel/MainViewModelCaptureModeOptionRebuildController.cs`
   owns selected-format assignment, pixel-format option collection mutation, and
   capture-format request shaping for the controller family.
   `Sussudio/Controllers/ViewModel/MainViewModelCaptureModeOptionRebuildController.Context.cs`
   owns the capture-mode option rebuild graph-port contract for option
   collections, source telemetry, selection state, automatic retarget flags,
   format-change suppression, and projected status text.
   `Sussudio/ViewModels/CaptureFormatSelectionPolicy.cs` owns the pure
   selected-format and mode-tuple video-format filtering policy.
   `MainViewModel.CaptureModeTransactions.cs` owns HDR toggle side effects:
   recording-time revert/status, mode option rebuilds, immediate reinitialize
   scheduling, and settings persistence.
    Late-arriving device format probe reconciliation, collection mutation,
    selected-device capability refresh, enqueue/failure logging, and retarget
    handoff live in
    `Sussudio/Controllers/ViewModel/MainViewModelDeviceFormatProbeController.cs`;
    its graph-port contract now lives in
    `Sussudio/Controllers/ViewModel/MainViewModelDeviceFormatProbeController.Context.cs`;
    UI-side late-probe retarget application, session mismatch checks, and
    active-capture restore live in
    `Sussudio/Controllers/ViewModel/MainViewModelDeviceFormatProbeRetargetApplier.cs`;
    its graph-port contract now lives in
    `Sussudio/Controllers/ViewModel/MainViewModelDeviceFormatProbeRetargetApplier.Context.cs`, while
    pure late-probe retarget decisions live in
    `Sussudio/ViewModels/DeviceFormatProbeRetargetPolicy.cs`.
    `tests/Sussudio.Tests/XUnit.PresentationPreviewDeviceFormatProbeRetargetContractsTests.cs`
    owns xUnit execution for the late device-format probe retarget ownership,
    behavior, and application checks after their removal from the legacy
    presentation-preview capture catalog.
    The presentation-preview ownership tests for this capture selection policy
    area are split across the
    `tests/Sussudio.Tests/MainViewModel.Capture.SelectionPolicy.*.cs` family so
    frame-rate, resolution, mode-selection, late-probe, recording-format, and
    runtime-flag assertions stay near their matching policy owners.
    `tests/Sussudio.Tests/XUnit.PresentationPreviewCaptureSelectionPolicyContractsTests.cs`
    owns xUnit execution for the mode-selection, capture-format, and
    recording-settings selection checks after their removal from the legacy
    presentation-preview capture catalog.
    Resolution option rebuild callers stay stable through the
    `MainViewModel.CaptureModeTransactions.cs` adapter. Resolution option
    rebuild ownership now lives in
    `Sussudio/Controllers/ViewModel/MainViewModelCaptureModeOptionRebuildController.Resolution.cs`:
    automatic resolution dropdown option construction inside the capture option
    rebuild controller family, automatic
    resolution-selection adaptation, auto-resolution state refresh, and
    resolution dropdown mutation through graph-built context ports. Effective Source resolution state and
    state-backed delegates to the pure selection policy live in
    `MainViewModel.ResolutionOptions.cs`.
    Automatic resolution ranking and source-aware frame-rate selection now
    live in `Sussudio/ViewModels/AutoCaptureSelectionPolicy.cs`; auto-resolution
    display text used by status and telemetry presentation lives in
    `MainViewModel.CapturePresentation.cs`.
   `tests/Sussudio.Tests/XUnit.PresentationPreviewResolutionSelectionContractsTests.cs`
   owns xUnit execution for the resolution-selection ownership and behavior
   checks after their removal from the legacy presentation-preview capture catalog.
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
   Resolution-selection harness coverage is split along the same boundary:
   `MainViewModel.Capture.SelectionPolicy.Resolution.Ownership.Tests.cs`
   owns source-shape placement assertions, while
   `MainViewModel.Capture.SelectionPolicy.Resolution.Behavior.Tests.cs` owns
   HDR, SDR, and auto-capture policy behavior contracts.
   State-backed delegates for callers that still live across the partial family
   stay in `MainViewModel.ResolutionOptions.cs`, while dropdown rebuild,
   collection mutation, and property notifications route through
    `MainViewModelCaptureModeOptionRebuildController.Resolution.cs`.
   Source telemetry summary, telemetry age, and target-summary display text
   formatting now live in `Sussudio/ViewModels/SourceTelemetryPresentationBuilder.cs`;
   HDR runtime state/readiness projection and target-summary property
   application live in `MainViewModel.CapturePresentation.cs`; keep snapshot
   application, source telemetry ingress behavior, telemetry age refresh,
   enum-string caching, and source-aware auto-retargeting in
   `Sussudio/Controllers/ViewModel/MainViewModelSourceTelemetryController.cs`.
   The source telemetry graph-port contract now lives in
   `Sussudio/Controllers/ViewModel/MainViewModelSourceTelemetryController.Context.cs`.
   Settings initialization, simple persistence reactions, and the impure
   settings load/save adapter stay in `MainViewModel.SettingsPersistence.cs`.
   `MainViewModel.SettingsLoadApplication.cs` owns applying validated load plans
   to ViewModel state and deferred device/audio/microphone selections, while
   `MainViewModelSettingsPersistenceProjection.cs` owns persisted-settings
   validation, clamping, deferred-selection handoff, and save DTO projection;
   active Flashback reactions to recording format
   and encoder quality/preset/split/bitrate now live in
   `MainViewModel.FlashbackEncoderSettings.cs`; buffer/GPU decode reactions stay
   in `MainViewModel.FlashbackSettings.cs`.
   Pure analog audio gain percent/XU-byte curve mapping now lives in
   `Sussudio/ViewModels/DeviceAudioGainMapper.cs`; device-native audio request
   lifetime, including mode property-change adapters, UI enqueue lifetime,
   shared debounce CTS fields, and cancellation cleanup, stays in
   `Sussudio/Controllers/ViewModel/MainViewModelDeviceAudioRequestController.cs`;
   the device-native audio request graph-port contract now lives in
   `Sussudio/Controllers/ViewModel/MainViewModelDeviceAudioRequestController.Context.cs`;
   gain property-change adapters, XU debounce, and flash-persist debounce stay
   in
   `Sussudio/Controllers/ViewModel/MainViewModelDeviceAudioRequestController.Gain.cs`;
   async native-XU
   device audio-control refresh/readback stays in
   `MainViewModel.DeviceAudioRefresh.cs`, mode switching and failure readback
   live in `MainViewModel.DeviceAudioMode.cs`, shared audio-control guards stay
   in `MainViewModel.AudioControls.cs`, and `MainViewModel.AnalogAudioGain.cs`
   owns analog gain XU writes and settings persistence. Use
   the supported native-XU switch/gain command surface rather than the legacy
   AT input-source fallback path.
   UI-only automation mutators for settings visibility, Flashback timeline
   visibility, show-all capture options, stats dock/section visibility, and
   frame-time overlay display now live in `MainViewModel.AutomationUi.cs`.
   Automation command entry points for audio enablement, audio-preview
   enablement, preview-volume clamp/persist, device-native mode/gain
   application, and microphone enablement with recording-time
   refusal/idempotent handling now live in `MainViewModel.AutomationAudio.cs`.
   Automation preview enable/disable idempotence, pending-reinit cancellation,
   and preview start/stop routing now live in
   `MainViewModelPreviewLifecycleController.cs` plus
   graph-built `MainViewModelPreviewReinitializeController.cs` context ports, with the stable
   `MainViewModel.PreviewState.cs` compatibility facade preserving the automation surface.
   Automation HDR and true-HDR preview recording-time guard enforcement and HDR
   availability checks now live in `MainViewModel.CaptureModeTransactions.cs`
   beside HDR mode change side effects.
   Automation Flashback enable/restart routing through the capture session
   coordinator now lives in `MainViewModel.FlashbackSettings.cs` alongside
   buffer/GPU setting reactions.
   Automation device refresh, capture-device selection, audio-input selection,
   and custom audio-input enablement now live in
   `MainViewModel.AutomationDeviceSelection.cs`.
   Recording format, encoder, and output-path automation entry points now stay
   in the `MainViewModel.AutomationSettings.cs` compatibility facade,
   while UI-thread mutations, HDR compatibility enforcement, Flashback cycle
   suppression, coordinator side effects, custom bitrate clamping, encoder
   preset, and output-path directory creation live in
   `Sussudio/Controllers/ViewModel/MainViewModelRecordingSettingsAutomationController.cs`.
   `Sussudio/Controllers/ViewModel/MainViewModelRecordingSettingsAutomationController.Context.cs`
   owns the recording-settings automation graph-port contract for UI dispatch,
   option collections, suppression flags, selected encoder/output state,
   recording-format coordinator updates, and Flashback encoder setting cycles.
   The automation recording desired-state bridge enters through
   `MainViewModel.RecordingState.cs` and is serialized by
   `Sussudio/Controllers/ViewModel/MainViewModelRecordingTransitionController.cs`,
   with graph-built context ports and start/stop execution in
   `Sussudio/Controllers/ViewModel/MainViewModelRecordingTransitionController.Operations.cs`.
   The emergency recording-stop bridge also enters through
   `MainViewModel.RecordingState.cs` but routes directly to
   `CaptureSessionCoordinator.StopRecordingForEmergencyAsync`
   so it keeps bypassing UI-thread dispatch and normal transition gates.
   Capture resolution, frame-rate, video-format, and MJPEG decoder worker-count
   automation entry points now stay in the
   `MainViewModel.AutomationSettings.cs` compatibility facade, while
   UI-thread mutations, validation, MJPEG decoder clamping, and active
   capture-mode reinitialization routing live in
   `Sussudio/Controllers/ViewModel/MainViewModelCaptureSettingsAutomationController.cs`.
   `Sussudio/Controllers/ViewModel/MainViewModelCaptureSettingsAutomationController.Context.cs`
   owns the capture-settings automation graph-port contract for option
   collections, selected capture-mode state, preview reinitialization checks,
   UI-thread dispatch, and format-change suppression.
   Startup FFmpeg capability probes for recording formats and split-encode modes
   plus observable recording-format option rebuilds now live in
   `Sussudio/Controllers/ViewModel/MainViewModelRecordingCapabilityController.cs`.
   `Sussudio/Controllers/ViewModel/MainViewModelRecordingCapabilityController.Context.cs`
   owns the recording-capability graph-port contract for default encoder names,
   observable recording/split-encode option collections, selected recording
   format state, HDR/status state, FFmpeg-missing state, and UI dispatch.
   The old `MainViewModel.Automation.cs` catch-all has been retired.

5. Extract capture resource owners behind the transition policy.

   The policy is now the legality/steady-state owner. Recent capture slices
   kept it authoritative while introducing smaller owners for the audio graph,
   Flashback backend resources, active recording backend resources, and active
   video pipeline resources.
   `FlashbackBackendResources.cs` now owns the preview backend resource set and
   producer attach/detach wiring.
   `FlashbackBackendResources.Startup.cs` owns startup construction,
   install/playback initialization, and startup rollback cleanup.
   `FlashbackBackendResources.BufferCycle.cs` owns sink-only buffer-cycle
   mechanics, `FlashbackBackendResources.PreviewDisposal.cs` owns backend
   teardown, and `FlashbackBackendResources.ArtifactCleanup.cs` owns artifact
   cleanup mechanics. Keep later Flashback backend mechanics in the matching
   focused owner before inventing another small owner;
   `CaptureService.FlashbackPreviewBackend.cs` should stay the transition
   coordinator for AV1 probing, readiness waiting, and cleanup handoff.
   `CaptureRecordingBackendResources.cs` now owns active recording backend
   resources: LibAv/Flashback sink identity, active recording context/settings,
   pending LibAv drain task tracking, and pending-drain reentry policy. Keep
   later recording backend resource mechanics there unless the behavior needs a
   larger, proven boundary.

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

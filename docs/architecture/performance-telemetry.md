# Performance telemetry: audit and agent instrumentation

Date: 2026-10-14. Branch: `claude/performance-monitoring-overhaul-96f1f2`.
Baseline commit: `a50ead960`.

This document has two jobs. Section 1 records what the app measured before
this overhaul and what was wrong with it, with live evidence where it exists.
Sections 2-6 are the contract for the telemetry core added on this branch:
what it measures, how an agent reads and controls it, and the invariants
that keep it cheap. Section 7 is the agent playbook.

## 1. Audit of the existing instrumentation

Live baseline: 4K59.94 SDR NV12 from an Elgato 4K X, D3D11 VideoProcessor
preview on a 59 Hz display, Flashback recording HEVC NVENC P1, Debug build,
RTX 4090 and Ryzen 9 7950X. Artifacts: `artifacts/perf-overhaul/baseline/`.

### 1.1 Structure

- Every measured series is a fixed `double[]` ring that the snapshot poll
  copies under a lock and sorts. Nothing is a histogram, so nothing can be
  merged across time or subtracted to measure an interval.
- Windows are count-based and differ per metric: `max(600, fps*20)` (source,
  present, render CPU, pipeline latency), 300 (MJPEG decode), 600 (jitter),
  720 (visual and fingerprint cadence), 256 (recording queue), 100 (WASAPI
  callback), 4096 (frame ledger), 240 x 500 ms (timeline). Some trackers mix
  cumulative and windowed values in one result.
- There is no way to ask "what happened between t0 and t1". The hub's
  "recent" counters are deltas against the previous poll, so any on-demand
  read shortens the next window.
- No ETW, EventSource, Meter, PDH, GPU timestamp query, or per-thread CPU
  measurement exists anywhere in the app or tools.
- All tuning is through about 55 `SUSSUDIO_*` environment variables read at
  construction or in static initializers. An agent can only change them by
  relaunching the app.

### 1.2 Defects found

| # | Finding | Evidence |
|---|---|---|
| D1 | The preview slow-stage classifier requires 30 s of samples (`PreviewPacingSlowStageClassifier.MinSampleDurationMs`), but the source and present windows hold 20 s (`CadenceWindowSeconds = 20`). Sample-ready classification can never run at 30 fps or above; only the hard-signal shortcuts fire. `get_frame_pacing_verdict` defaults to the same unreachable 30 s. **Fixed in slice 1:** both now require 15 s. | Live: `PreviewCadenceSampleDurationMs = 20011.7`, `CaptureCadenceSampleDurationMs = 20013.0` after five minutes |
| D2 | The frame ledger defines 18 stages and emits 6. Decode, upload, render, present and encode stages are never written, the device timestamp is always null, and the preview present id is not recorded, so the ledger cannot be joined to the renderer. One global lock is taken several times per frame. | `UnifiedVideoCapture.cs` FrameLedger; `CaptureModels.cs` FrameLedgerStage |
| D3 | Pipeline latency starts after `ReadSample` returns. USB, driver, Frame Server and source-reader queueing are invisible. | `MfSourceReaderVideoCapture.cs` ReadLoop `arrivalTick` |
| D4 | WASAPI callback intervals use `Environment.TickCount64` (about 15.6 ms resolution) and keep avg/max over 100 samples. The severe-gap threshold collapses to 100 ms. Render has no interval, jitter or underrun-duration measurement. | `WasapiAudioCapture.TrackCaptureCallback`; `WasapiAudioPlayback` |
| D5 | Recording and Flashback have no encode, mux-write or disk timing. Only the CPU video lane feeds the queue-latency tracker, at TickCount64 resolution; GPU and CUDA lanes have no dwell timing. | `LibAvRecordingSink` `VideoQueueLatencyTracker` |
| D6 | Measured but never exported: NVML GPU/NVENC/NVDEC utilization, clocks, power (stats UI only); input-view cache statistics (log only); mux and rotation phase timings (log only); WASAPI engine period (init log only); per-reason renderer drops (only the last reason is kept). | `NvmlMonitor`, `D3D11PreviewRenderer.RenderPasses.cs`, `LibAvEncoder.LastMuxPhaseTiming` |
| D7 | Misleading values: encoder `_droppedFrameCount` counts failures, not load drops; byte counters are packet bytes, not disk writes; `SourceReaderFrameChannelDepth` is the recording queue depth; integrity `AudioFramesArrived`, `AudioFramesWrittenToSink` and `AudioSamplesEncoded` are the same number; `EstimatedPipelineLatencyMs` is truncated to a `long`; capture A/V drift is frame-count based with hard-coded 48 kHz and nominal fps; the MJPEG summary reports an interop-copy time that is never measured. | Survey, section 1 notes |
| D8 | PresentMon, the only display-truth source, needs administrator rights or the Performance Log Users group. Without it the agent has no OS-level present data. | Live: `failed to start trace session: access denied` |
| D9 | The diagnostics hub polls every 500 ms whether or not anyone reads it, building an 807-field, 121 KB snapshot with about four UI-thread dispatches per tick (the preview runtime snapshot is requested three times by overload shape) and copying and sorting about seven 1,200-sample rings. Its own cost is not measured, so the observer effect is unknown. `SnapshotUpdated` has no subscribers. | Live: `ssctl --json state` = 121,261 bytes; about 0.4 MB/s steady allocation; `PreviewRuntimeSnapshotControllers.cs`, `MainViewModel.cs` snapshot dispatch |
| D10 | Every automation acknowledgement embeds the cached snapshot (`CreateResponse(includeSnapshot: true)` by default), so a burst of control commands serializes about 120 KB each. | `AutomationCommandDispatcher.CreateResponse` |
| D11 | Full-rate frame data never reaches the agent: the frame-time graph's 10 s QPC history is UI-only, and the hub exposes only the latest 64 of 4,096 frame-ledger events every 500 ms. The 2-minute performance timeline omits audio, A/V sync, recording, encoder, NVML and MJPEG fields. | `PreviewFrameTimeHistory`, `FrameLedger`, `PerformanceTimelineEntry` |
| D12 | The agent cannot read UI state (overlay, settings, InfoBars, section expansion, window rect) or press controls that have no purpose-built command (Flashback nudge and keyboard transport, timeline scrubbing). | Automation surface survey |

### 1.3 Gaps an agent cannot work around today

- No distributions with true percentiles over a chosen interval.
- No per-frame, cross-stage trace that explains a single hitch.
- No GPU execution time, per-thread CPU, GPU engine utilization, VRAM, DWM
  composition timing, GC pause durations, or UI-thread responsiveness.
- No way to hear: app-internal meters do not prove audible output, and
  monitoring latency is unmeasured.
- No runtime control of tuning knobs, so no A/B experiment without a relaunch.
- No generic UI access: a human can read and press any control; the agent can
  only use the 57 purpose-built commands.

## 2. Telemetry core

Owner: `Sussudio/Services/Performance/` (namespace `Sussudio.Services.Performance`;
`Services/Telemetry` already means source-signal telemetry from the capture
device). Performance telemetry is process-wide and ambient,
like `Logger`: hot paths record into static owners without constructor
plumbing. The folder depends only on `Services/Runtime` and the logger.
Capture, Preview, Recording, Flashback, Audio and Automation record into it;
it never calls back into them.

### 2.1 `PerfHistogram`

Log-linear histogram of non-negative `long` values (microseconds for time,
bytes or percent otherwise).

- Values 0-63 have exact buckets. Above that each power of two has 32
  sub-buckets, so a bucket's width is at most 1/32 of its lower bound. The
  reported value of a bucket is its midpoint: relative error at most 1.6 %.
- Range: 0 to 2^32 - 1. Larger values clamp into the top bucket and still
  update `max`. Negative values clamp to 0. Recording never throws.
- `Record(long value)`: one `Interlocked.Increment` on the bucket, one
  `Interlocked.Add` on the sum, and a CAS only when a new maximum is seen.
  No allocation, no lock. Safe from any number of threads.
- `Snapshot()`: copies counts, sum and max. Concurrent writers may land in
  the copy or not; counts never go backwards.
- `PerfHistogramSnapshot.Subtract(baseline)`: per-bucket delta clamped at
  zero. Percentiles of a delta are exact to bucket precision; the delta's
  max is the upper bound of the highest non-empty delta bucket, clamped to
  the lifetime max, and is reported as approximate.

### 2.2 `PerfMetrics` catalog

`PerfMetrics.cs` declares every metric as a static field with a stable dotted
name, unit, and one-line description. Names are wire contract (tools and
experiment specs refer to them); append, never rename. Kinds:

- **Histogram** (`PerfHistogram`): durations, intervals, latencies,
  utilization samples.
- **Counter** (`PerfCounter`, a padded `long`): monotonic event counts.

Initial catalog (time unit is microseconds unless noted):

| Name | Kind | Recorded where |
|---|---|---|
| `capture.ingress_lag` | hist | read loop: now minus MF sample time, only when the sample clock is the QPC domain (see 2.6) |
| `capture.readsample_wait` | hist | read loop: time blocked in `ReadSample` |
| `capture.arrival_interval` | hist | read loop: QPC interval between successful reads |
| `capture.deliver` | hist | read loop: `DeliverFrame` duration (all consumer fan-out on the read thread) |
| `capture.visual_scan` | hist | luma visual-cadence scan duration |
| `mjpeg.decode` | hist | decode worker, per frame |
| `mjpeg.queue_wait` | hist | compressed enqueue to worker dequeue |
| `mjpeg.pipeline` | hist | arrival to strict-order emit |
| `preview.jitter_residence` | hist | jitter buffer enqueue to submit |
| `preview.queue_dwell` | hist | renderer submit until the render thread selects the frame to render (a frame skipped as stale is a drop, not a dwell) |
| `preview.frame_latency_wait` | hist | waitable swap-chain wait |
| `preview.input_upload` | hist | render CPU stage |
| `preview.render_submit` | hist | render CPU stage |
| `preview.present_call` | hist | `Present` call duration |
| `preview.render_total` | hist | total render CPU per frame |
| `preview.gpu_render` | hist | D3D11 timestamp queries around the render passes (when GPU timing is enabled) |
| `preview.present_interval` | hist | present-to-present on cadence frames |
| `preview.pipeline_latency` | hist | arrival to estimated visible; live frames only (Flashback playback frames, source sequence below zero, are excluded) |
| `preview.ingress_to_visible` | hist | MF sample time to estimated visible; QPC domain and live frames only; the sample time reaches the renderer through the ingress sample ledger (2.6) |
| `recording.queue_dwell` | hist | video packet enqueue to encode-loop dequeue, all lanes (CPU, D3D11, CUDA), QPC |
| `recording.encode_submit` | hist | `avcodec_send_frame` plus the receive/drain loop of one frame, including the nested `mux_write` time |
| `recording.mux_write` | hist | video-packet `av_interleaved_write_frame` including I/O (nested inside `encode_submit`; audio packets are not recorded) |
| `flashback.queue_dwell` | hist | as recording |
| `flashback.encode_submit` | hist | as recording |
| `flashback.mux_write` | hist | as recording |
| `flashback.segment_rotation` | hist | `RotateSegment` total on every exit, success or failure (flush, trailer, new segment, buffer bookkeeping) |
| `flashback.playback_decode` | hist | steady-state continuous playback only: hardware read-ahead plus the next-frame read of one paced iteration; seek, scrub, step and pause-forward decodes are not recorded |
| `flashback.playback_present_interval` | hist | interval between paced presents on the playback thread during continuous playback (the interval the existing cadence tracker measures) |
| `audio.capture_interval` | hist | WASAPI capture event interval, QPC (the HDMI and microphone capture instances share the metric) |
| `audio.capture_drain` | hist | capture packet drain duration |
| `audio.render_interval` | hist | WASAPI render event interval, QPC; the baseline resets on pause and resume so a pause is never one long interval |
| `audio.render_queue_ms` | hist (ms) | monitor queue depth in milliseconds, sampled per render event |
| `ui.dispatcher_latency` | hist | UI responsiveness probe (2.8) |
| `gc.pause` | hist | each GC suspension, from the runtime event listener |
| `gpu.engine.3d` / `.video_encode` / `.video_decode` / `.copy` | hist (percent) | PDH GPU Engine utilization for this process, 1 Hz while sensors run |
| `gpu.nvml.gpu_util` / `.encoder_util` / `.decoder_util` | hist (percent) | each NVML poll |
| `telemetry.hub_refresh` | hist | diagnostics hub snapshot refresh |
| `telemetry.hub_alloc` | hist (bytes) | process-wide allocated bytes during one refresh; an upper bound, because other threads allocate meanwhile |
| `automation.command` | hist | automation command dispatch |
| `preview.drop.<reason>` | counter | renderer drops by reason, from a fixed table (2.2.1) |
| `preview.jitter_drop.<reason>` | counter | jitter-buffer drops by reason, fixed table; a skipped run of missing sequence numbers adds its length |
| `preview.jitter_underflow.<reason>` | counter | jitter-buffer underflows by reason, fixed table |
| `capture.read_errors` | counter | read-loop exceptions |
| `ui.stall_50ms` | counter | UI probe waits over 50 ms |
| `audio.render_underrun` | counter | render events with an empty queue |

### 2.2.1 Record sites and reason tables

Every site is observation only: no allocation, no new lock, no blocking, and a
stopped trace costs one volatile read. Counters are the only thing recorded
inside an existing lock. Where each metric is recorded (by method; line numbers
drift):

| Metric (and trace stage) | Recorded in |
|---|---|
| `capture.readsample_wait`, `capture.arrival_interval`, `capture.ingress_lag`, `capture.deliver`, `capture.read_errors`; `capture.read`, `capture.deliver`, `capture.sample_time` | `MfSourceReaderVideoCapture.ReadLoop` through `MfReadLoopTelemetry` (one instance per read loop) |
| `capture.visual_scan`; `capture.visual_scan` | `UnifiedVideoCapture.TrackPreviewVisualFrame`, around the tracker calls and outside their locks; recorded when the scan ran without throwing |
| `mjpeg.queue_wait`, `mjpeg.decode`, `mjpeg.pipeline`; same stage names | `ParallelMjpegDecodePipeline`: queue wait at worker dequeue (before the shed check), decode on the success path only (`Arg` = worker index + 1), pipeline in `DrainReadyFrames` before the emit callback and outside `_reorderLock` |
| `preview.jitter_residence`; same, `preview.jitter_drop.*`, `preview.jitter_underflow.*` | `MjpegPreviewJitterBuffer.SubmitFrame` (enqueue stamp to submit, outside `_sync`), `RecordDroppedFrame`, `RecordUnderflow`, `TryRecordResumeReprimeMiss` |
| `preview.queue_dwell`, `preview.frame_latency_wait`, `preview.input_upload`, `preview.render_submit`, `preview.present_call`, `preview.render_total`, `preview.present_interval`, `preview.pipeline_latency`, `preview.ingress_to_visible`, `preview.drop.*`; matching stages and `preview.drop` | `D3D11PreviewRenderer` (`EnqueuePendingFrame`, the render loop, `WaitForFrameLatencySignal`, `TrackFrameDropped`, `TrackPresentCadence`, `TrackPipelineLatency`, `TrackRenderCpuTiming`) and `PresentAndTrackFrame` in `D3D11PreviewRenderer.RenderPasses.cs` |
| `recording.*`, `flashback.queue_dwell`, `flashback.encode_submit`, `flashback.mux_write` | `LibAvRecordingSink.DrainVideoPackets`/`DrainGpuPackets`/`DrainCudaPackets` and `FlashbackEncoderSink.DrainVideoPackets`/`DrainGpuPackets` for queue dwell (QPC stamp on the packet at enqueue, recorded after dequeue and outside `_videoQueueSync`); `LibAvEncoder.SendVideoFrame`/`SendGpuVideoFrame`/`SendCudaVideoFrame` and `WritePacket` for encode and mux, through the injected `EncoderPerfMetrics` role |
| `flashback.segment_rotation` | `FlashbackEncoderSink.RotateSegment` through `PerfScope` (covers both returns and the terminal rethrow) |
| `flashback.playback_decode`, `flashback.playback_present_interval` | `FlashbackPlaybackController.PaceAndDecodeFrame` and `UpdateCadenceMetrics` |
| `audio.capture_interval`, `audio.capture_drain`; `audio.capture` | `WasapiAudioCapture.CaptureThreadMain`, beside the existing `TrackCaptureCallback` (its TickCount64 trackers are unchanged) |
| `audio.render_interval`, `audio.render_queue_ms`, `audio.render_underrun` | `WasapiAudioPlayback.RenderAvailableFrames` (interval and queue depth per render event, before `_chunkLock`); underrun counter beside each `_renderSilenceCount` increment |
| `gpu.nvml.*` | `NvmlMonitor` poll, for each value NVML returned |
| `telemetry.hub_refresh`, `telemetry.hub_alloc`; `telemetry.hub_refresh` | `AutomationDiagnosticsHub.RefreshSnapshotAsync`, after the refresh gate, including a failed refresh |
| `automation.command`; `automation.command` | `AutomationCommandDispatcher.ExecuteAsync` finally block; the trace id is the command kind |

Not recorded yet: `preview.gpu_render`/`preview.gpu_render` (GPU timestamp
queries, slice 2b), the UI probe, GPU engine and GC-adjacent sensors (slice
2b), and the reserved `audio.render` trace stage (the render interval and queue
depth have no span to draw). Trace placement is approximate for two preview
stages: `preview.input_upload` is anchored at the start of the render and
`preview.render_submit` ends where `Present` begins, because the render passes
do not carry stage start times out; durations are exact.

Reason tables keep the drop and underflow counters bounded and allocation
free. The renderer and the jitter buffer pass open-ended reason strings; each
maps through a fixed switch to a small enum code, and anything unknown, null or
empty lands in the last entry, `other`. A reason of the form `<x>:stale` maps to
`stale`. Code 0 is reserved ("none"), so a trace instant with no reason is
distinguishable.

- `preview.drop.<reason>` (code = position, from 1): `renderer-stopped`,
  `renderer-backlog`, `explicit-drain`, `live-preview-suppressed`,
  `live-preview-resumed`, `shared-device-reset`, `stale-generation`, `stale`,
  `render-skipped`, `render-failed`, `stale-before-render`, `renderer-exit`,
  `renderer-stop`, `device-lost`, `other`.
- `preview.jitter_drop.<reason>`: `queue-full`, `late-sequence`,
  `missing-sequence`, `submit-failed`, `hard-deadline`, `soft-deadline`,
  `cleared`, `suppressed`, `other`.
- `preview.jitter_underflow.<reason>`: `empty-queue`, `waiting-for-sequence`,
  `selection-blocked`, `resume-reprime`, `other`.

The `preview.drop` trace instant carries the same code as its `arg`; the dump
also writes the reason name.

### 2.3 Measurement windows

`PerfWindows` holds up to 16 named windows. `begin` captures a baseline of
every histogram and counter plus thread CPU, process CPU, I/O and GC totals.
`read` returns the delta without closing; `end` returns it and closes.
Beginning an existing name restarts it and says so. The delta report is the
same shape as the lifetime report, so tools have one parser.

Report semantics (slice 1): a lifetime report is measured from a zero baseline
taken at process creation (cumulative counters start at zero; DWM frame counts
from the first reading). Process and thread CPU are percent of one core over
the report interval; a thread that started mid-window or exited before the
window ended is divided by the part of the interval it ran, and a retired
thread with no CPU in the interval is omitted. `Process.IoReadBytes` and
`IoWriteBytes` are deltas over the interval, memory and handle counts are
instantaneous. Marker lists in a window report contain only markers added
since the window began.

### 2.4 `FrameTrace` flight recorder

A lock-free ring of 40-byte events, off by default. `start` allocates the
ring (power of two, 2^12 to 2^22 events, default 2^18), `stop` freezes it,
`dump` writes Chrome Trace Event JSON that Perfetto and `chrome://tracing`
open and that `ssctl perf trace analyze` reads. When disabled each record
site costs one volatile read.

- Event: `{ long Stamp; long Ts; long Dur; long Id; ushort Kind; ushort Arg; int Tid }`
  (40 bytes). `Ts` and `Dur` are QPC ticks; `Id` is the frame's arrival
  timestamp for frame events (see Frame identity below); `Kind` indexes
  `FrameTraceEventKind`; `Arg` is an optional 16-bit payload, zero when unused.
  Today `Arg` carries the drop reason code on `preview.drop` and the worker
  index plus one on `mjpeg.decode`. The dump writes `args.arg` only when it is
  non-zero, and `args.reason` (the reason name) for `preview.drop`.
- Writers claim a slot with `Interlocked.Increment`, write `Stamp = -1`,
  fill the fields, then publish `Stamp = index`. The dumper copies a slot and
  keeps it only if the stamp read before and after equals the expected
  index, so a torn slot is dropped, never emitted.
- Markers (`perf mark`) and thread names are kept beside the ring and
  emitted as instant and metadata events.
- Lifecycle (slice 1): `start` while a trace is running is an error (stop it
  first); `stop` freezes the ring and the frozen buffer stays readable until
  the next `start`; `dump` with no trace ever recorded is an error and `dump`
  overwrites an existing file. GC pauses (runtime `EventListener`, GC keyword)
  are recorded as `GcPause` spans when a trace is running and always feed the
  `gc.pause` histogram.
- Frame identity: every frame-stage event uses the frame's **arrival QPC
  timestamp** (`arrivalTick`, taken in the MF read loop right after
  `ReadSample` returns) as `Id`. It is unique per frame and already travels
  with the frame through the raw, MJPEG, preview, recording and Flashback
  paths. Sequence numbers do not work as a join key: the MJPEG pipeline
  assigns its own sequence, which diverges from the source sequence after a
  startup drop, and the GPU lanes carry none. Flashback playback frames use
  their submit tick, which is also unique. Events with no frame (GC, UI
  stall, hub refresh, automation command, mux write after encoder lookahead)
  use `Id = -1`; the analyzer attributes them by time overlap. The dump's
  `originQpc` and `qpcFrequency` let the analyzer place `Id` on the trace
  time axis, so `ts - (Id - originQpc)` is a stage's latency from arrival.
- `capture.sample_time` is an instant event at the MF sample time converted
  to QPC (only when the ingress clock is the QPC domain), so the analyzer can
  compute per-frame ingress-to-visible exactly.
- Stages added in slice 2a beyond the slice-1 list: `capture.sample_time`
  (kind 31) and `telemetry.hub_refresh` (kind 32, id -1). `automation.command`
  uses the command kind as its id. Encoder-level spans take the frame's
  arrival timestamp as id when the packet carries it (CPU lane with a lease)
  and -1 otherwise (raw buffers, GPU and CUDA lanes carry no identity);
  `*.mux_write` is always -1.

### 2.5 Runtime tuning knobs

`TuningKnobs` catalogs every `SUSSUDIO_*` performance knob: type, default,
range, when it is read (`apply`), owner file, description. Setting a knob
writes the process environment variable, so every existing read site picks
it up at its next read without code changes. The launch value is captured
at first use and `reset` restores it. Overrides are never persisted.

`apply` values: `live` (read continuously), `preview-restart`,
`recording-start`, `flashback-restart`, `app-restart` (read in a static
initializer). Responses state what must restart. Values outside the range
or of the wrong type are rejected, not clamped. Every change logs
`TUNING_KNOB_SET name= old= new= apply=` and appears in the next report.

The catalog (slice 1) lists 51 `SUSSUDIO_*` knobs. `SUSSUDIO_AUTOMATION_TOKEN`
(credential), `SUSSUDIO_AUTOMATION_PIPE` (the pipe the command arrives on),
`SUSSUDIO_LOG_ROOT` and `SUSSUDIO_RECOVERY_DIRECTORY` (storage roots read once
at startup) are deliberately not settable at runtime; `knobs` lists them under
`Excluded` with the reason. MMCSS task-name knobs are string choices. `apply`
scopes are read from each consumer's read site; where the read site could not
be confirmed from source the catalog uses the stricter scope.

New live knobs added on this branch:

| Knob | Default | Effect |
|---|---|---|
| `SUSSUDIO_DIAGNOSTICS_POLL_MS` | 500 | diagnostics hub poll interval, 100-10000 (slice 1) |
| `SUSSUDIO_TELEMETRY_SENSORS` | auto | `auto`: sensors run while a window is open or a trace runs; `on`; `off` |
| `SUSSUDIO_PREVIEW_GPU_TIMING` | auto | GPU timestamp queries; `auto` follows sensor demand |

### 2.6 Ingress clock

The read loop compares each MF sample time with `QPC` converted to 100 ns.
For the first 120 samples it records the offset; if every offset lies in
[0, 2 s) the sample clock is the QPC domain and `capture.ingress_lag` is
absolute. Otherwise ingress lag is not recorded and the report says
`ingressClock=relative` with the observed minimum offset. Nothing in the
pipeline consumes this classification.

Details (slice 2a): `QpcMath` converts QPC ticks to 100 ns by splitting whole
seconds from the remainder, with a saturating `Int128` fallback, because
`ticks * 10_000_000` overflows a `long` after 25.6 hours of uptime at 10 MHz.
`IngressClockClassifier` consumes the offsets: the first offset outside
[0, 2 s) classifies `relative` at once, the 120th in-range offset classifies
`qpc`, and the classification never flips. A zero or negative MF sample time is
skipped and does not count toward the 120. The result is published through
`PerfIngressClock` (kind and smallest observed offset); each read loop starts
with `unknown`, so a restarted capture is classified afresh. `capture.ingress_lag`
is recorded only once the clock is classified `qpc`, so the first 119 samples
(the ones that decide the classification) are not in the histogram.

Once the clock is `qpc`, the read loop also stores each frame's sample time as
a QPC tick in `PerfIngressSamples`, a 1024-slot direct-mapped ledger keyed by
the frame's arrival timestamp. The renderer looks the frame up by the same
timestamp to compute `preview.ingress_to_visible`. This avoids adding a field
to the capture delegates, `PooledVideoFrame`, the MJPEG work item and
`PreviewFrameTracking`, and `SourcePtsTicks` is not repurposed. A slot is
written by the single read-loop thread (invalidate the key, write the value,
publish the key) and read lock-free (check the key, read the value, barrier,
re-check the key), so a reader sees a miss or the matching pair, never another
frame's sample. A newer frame that maps to the same slot evicts the older one;
with 1024 slots and a handful of frames in flight this does not happen in
steady state, and a miss records nothing rather than a wrong value.

### 2.7 Thread CPU

`HotThreads.Register(role)` is called at the start of every dedicated hot
thread (beside its MMCSS registration) and disposed on exit. Reports read
`GetThreadTimes` and `QueryThreadCycleTime` per registered thread and give
CPU percent of one core over the window, priority, and whether MMCSS
registration succeeded. Exited threads keep their final totals in a bounded
retired list so a window that spans a preview restart still adds up.

Registered roles (slice 2a): `capture.read` (the read loop on its pool thread,
disposed when the loop exits), `mjpeg.worker-<n>`, `mjpeg.emitter`,
`mjpeg.jitter`, `preview.render`, `recording.encode`, `flashback.encode`,
`flashback.playback`, `audio.capture` (one per capture instance) and
`audio.render`. Each passes whether its MMCSS registration succeeded.

### 2.8 Sensors

Run only while demanded (an open window, a running trace, or the knob set
to `on`):

- UI responsiveness: every 100 ms, enqueue a no-op on the UI dispatcher and
  record the wait (`ui.dispatcher_latency`, `ui.stall_50ms`).
- GPU engines: PDH `\GPU Engine(pid_<pid>*)\Utilization Percentage` grouped
  by engine type, and `\GPU Process Memory(pid_<pid>*)\Dedicated Usage`,
  sampled at 1 Hz.

Always on, read on demand:

- Process: `GetProcessTimes`, `GetProcessMemoryInfo`, `GetProcessIoCounters`,
  handle and thread counts.
- GC: collection counts, `GC.GetTotalPauseDuration`, allocated bytes, and a
  runtime `EventListener` for per-GC pause (`gc.pause`, trace events).
- DWM: `DwmGetCompositionTimingInfo` refresh period and composed-frame count.
- VRAM: `IDXGIAdapter3.QueryVideoMemoryInfo` for adapters this process uses.
- NVML values recorded at each existing NVML poll.
- Log tap: the logger writer keeps the last 4,096 lines and per-caller and
  per-tag line counts for spam detection.

### 2.9 Ears: audio output probe

`AudioOutputProbe` captures this process's rendered audio through Windows
process loopback (`ActivateAudioInterfaceAsync` with
`AUDIOCLIENT_ACTIVATION_TYPE_PROCESS_LOOPBACK`, Windows 10 2004+), at 48 kHz
float stereo, for 0.5-30 s. It reports per-channel RMS and peak dBFS,
silent-block percent, clicks (sample-to-sample jumps far above the local
envelope), and dropouts (exact-zero runs inside otherwise active audio), and
optionally writes a WAV. With `latency` it arms a tap on the program capture
path (48 kHz float stereo after conversion) for the same interval and
cross-correlates input with output: coarse 1 ms envelopes over 0-1000 ms,
then sample-level refinement. The result is the in-app monitoring latency
from capture delivery to the mixer; it excludes device input and endpoint
output latency, which the report states. Silent input reports
`insufficient-signal` rather than a number.

### 2.10 Fingers: UI tree and invoke

`GetUiTree` walks the live XAML tree on the UI thread and returns named or
interactive elements with type, name, automation id and name, visible text,
enabled, effective visibility, toggle state, range value, selection, bounds
in window DIPs, and the automation patterns each supports.
`InvokeUiElement` resolves a target by `x:Name`, then automation id, then
automation name (must be unique, otherwise the candidates are returned), and
performs `invoke`, `toggle`, `set-value`, `select`, `expand`, `collapse` or
`focus` through the element's automation peer, exactly as UI Automation
would. Hidden or disabled targets are rejected unless `force` is set.

## 3. Automation commands

Appended after `SetFlashbackGpuDecode = 57`; manifest revision 2 to 3.

| Id | Name | Payload | ssctl | MCP |
|---|---|---|---|---|
| 58 | `GetPerfReport` | `{ window?, filter?, includeIdle? }` | `perf report` | `get_perf_report` |
| 59 | `PerfWindow` | `{ action: begin\|read\|end\|cancel\|list, name? }` | `perf window ...` | `perf_window` |
| 60 | `AddPerfMarker` | `{ text }` | `perf mark <text>` | `add_perf_marker` |
| 61 | `FrameTrace` | `{ action: start\|stop\|status\|dump, capacity?, outputPath? }` | `perf trace ...` | `frame_trace` |
| 62 | `GetTuningKnobs` | `{ filter? }` | `knobs` | `get_tuning_knobs` |
| 63 | `SetTuningKnob` | `{ name, value?, reset? }` | `knob set\|reset` | `set_tuning_knob` |
| 64 | `GetLogTail` | `{ maxEntries?, contains? }` | `log tail` | `get_log_tail` |
| 65 | `CaptureAudioOutput` | `{ seconds?, outputPath?, measureLatency? }` | `audio listen` | `listen_audio_output` |
| 66 | `GetUiTree` | `{ root?, maxDepth?, interactiveOnly? }` | `ui tree` | `get_ui_tree` |
| 67 | `InvokeUiElement` | `{ target, action?, value?, force? }` | `ui invoke` | `invoke_ui_element` |

Tool-side only (no app command): `ssctl perf trace analyze <file>`,
`ssctl perf experiment <spec.json>`, MCP `analyze_frame_trace`,
`run_perf_experiment`.

### 3.1 Perf report shape

```json
{
  "Window": "baseline", "Since": "process-start|window",
  "StartedUtc": "2026-10-14T20:00:00.0000000Z", "GeneratedUtc": "2026-10-14T20:00:30.0120000Z",
  "DurationMs": 30012.4,
  "Metrics": [ { "Name": "preview.present_interval", "Unit": "ms", "Count": 1799,
                 "RatePerSec": 59.94, "Mean": 16.68, "P50": 16.67, "P90": 16.9,
                 "P99": 18.2, "P999": 21.0, "Max": 21.3, "MaxApprox": true } ],
  "Counters": [ { "Name": "preview.drop.stale-before-render", "Delta": 2, "RatePerSec": 0.07 } ],
  "Threads": [ { "Role": "preview.render", "ThreadId": 1234, "CpuPercent": 6.1,
                 "Cycles": 123456789, "Priority": "AboveNormal", "Mmcss": true, "Alive": true } ],
  "Process": { "CpuPercentOfOneCore": 18.2, "PrivateBytes": 0, "WorkingSetBytes": 0,
               "IoReadBytes": 0, "IoWriteBytes": 0, "Handles": 0, "Threads": 0 },
  "Gc": { "Gen0": 0, "Gen1": 0, "Gen2": 0, "PauseMs": 0.0, "AllocatedBytes": 0 },
  "Gpu": { "Engines": [ { "Type": "VideoEncode", "MeanPercent": 31.0, "P95Percent": 35.0 } ],
           "DedicatedBytes": null, "Adapters": [ ] },
  "Dwm": { "RefreshHz": 59.94, "FramesComposedPerSec": 59.9 },
  "Capture": { "IngressClock": "qpc|relative|unknown", "IngressClockMinOffsetMs": null },
  "Sensors": { "Active": true, "Reason": "window:baseline" },
  "Trace": { "Running": false, "Events": 0, "Capacity": 0 },
  "Knobs": [ { "Name": "SUSSUDIO_PREVIEW_JITTER_TARGET_DEPTH", "Value": "1", "Launch": null, "Apply": "preview-restart" } ],
  "Markers": [ { "TimestampUtc": "...", "Text": "variant B" } ]
}
```

Time metrics are reported in milliseconds; percent and byte metrics in their
own units. `filter` is a name prefix (case-insensitive). Metrics with zero
count are omitted unless `includeIdle` is true. `Gpu.DedicatedBytes` is null
and `Gpu.Adapters` empty until the GPU/VRAM sensors land; `Gpu.Engines` is
built from the `gpu.*` percent metrics that have samples. `Sensors.Active`
means demand exists (an open window or a running trace), with `Reason`
`window:<names>`, `trace` or `idle`. `Dwm` values are null when DWM timing is
unavailable. `Threads` is empty until dedicated threads call
`HotThreads.Register`. `Capture.IngressClockMinOffsetMs` is the smallest
observed sample-time offset the ingress classification used.

The command-catalog payload shapes use `string` for `action` (the allowed
values are listed in the command description), because the catalog shape
grammar has no enumeration type. A rejected `SetTuningKnob` still returns
`Data` with `Failure` (`UnknownKnob`, `WrongType`, `OutOfRange`, ...) beside
the `invalid-request` error code.

## 4. Invariants

- Recording sites are allocation-free and lock-free; a disabled trace or
  idle sensor costs one volatile read.
- Telemetry never changes pipeline behavior. Defaults of every existing knob
  are unchanged; new knobs default to current behavior.
- Wire names (command names, metric names, knob names, report fields) are
  appended, never renamed.
- Sensors that wake threads (UI probe, PDH) run only on demand.
- File outputs go through the catalog path policy (`WriteFile`).

## 5. Failure modes covered by isolated tests

Written before the code, per the testing policy:

- Histogram: negative, zero, 63/64 boundary, and `long.MaxValue` values;
  concurrent writers lose no counts; snapshot during writes never throws or
  decreases; empty percentiles are zero with count zero; precision within
  1.6 % on known distributions; delta never negative.
- Windows: restart of an existing name; 17th window rejected; read and end
  of an unknown name rejected; end removes the window.
- Trace: disabled record writes nothing; wraparound keeps the newest
  `capacity` events in order; a slot whose stamp changes during copy is
  dropped; capacity clamps to a power of two in range; marker text is JSON
  escaped; dump while running is consistent.
- Knobs: unknown name, wrong type, and out-of-range values rejected; reset
  restores the launch value including "unset"; apply scope reported.
- Wire contract: command ids appended with the manifest revision bump;
  payload keys from the shared vocabulary.
- Analyzer: malformed and truncated trace JSON produce an error, not a
  crash; frames missing stages are counted, not dropped.
- QPC to 100 ns conversion and ingress classification (slice 2a, written
  before the code): `ticks * 10_000_000` overflows a `long` after 25.6 hours
  of uptime at 10 MHz, so the conversion must stay exact far beyond that;
  non-integer frequency ratios (3 MHz, 24 MHz, 1 GHz) round-trip within one
  tick; negative or zero input never throws; fewer than 120 usable samples
  stay `unknown` and record no lag; one offset outside [0, 2 s) among the
  first 120 classifies `relative` at once and permanently for that capture
  start; 120 in-range offsets classify `qpc`; a zero or negative MF sample
  time is skipped and does not count toward the 120; the classification never
  flips after it is made; the smallest observed offset is the one published;
  a reset returns to `unknown` so a restarted capture is classified afresh.
- Ingress sample hand-off (slice 2a, written before the code): a recorded
  frame is found by its arrival timestamp; an empty slot (arrival 0) never
  matches, and neither does a zero or negative key; a newer frame that maps to
  the same slot makes the older lookup miss and never returns the newer
  frame's sample; a reader racing the single writer sees either a miss or the
  matching pair, never a key paired with another frame's sample (the writer
  invalidates the key before it touches the value).
- Reason tables (slice 2a): every reason string the renderer or jitter buffer
  passes maps to a table entry without allocating; an unknown string, null and
  empty collapse into `other` so the counter set stays bounded; `{x}:stale`
  maps to `stale`; every table entry has a registered counter with the wire
  name `<prefix><name>`; out-of-range codes clamp to `other`.
- Trace slot packing (slice 2a): the 16-bit event kind and 16-bit arg share
  the field the kind used alone; an arg survives the ring round trip, an
  arg of zero is omitted from the dump, and a kind never bleeds into the arg.

- Packet stamps (slice 2a): a video packet built without the sink's factories
  has no QPC enqueue stamp (zero) and is skipped by `*.queue_dwell` instead of
  recording a dwell since process start; a dequeue stamp earlier than the
  enqueue stamp records zero (histograms clamp negative durations).

Everything else is verified end to end against the live app (section 6).

## 6. Validation

`scripts\validate.ps1` plus live runs whose artifacts live under
`artifacts/perf-overhaul/` and can be re-run: a perf window over steady
preview, a frame trace dumped and analyzed, a knob A/B experiment, an audio
listen, and a UI tree read and invoke.

## 7. Agent playbook

Measure an interval:

```powershell
ssctl perf window begin steady
Start-Sleep 30
ssctl perf window end steady --json > artifacts/steady.json
```

Explain a hitch: `ssctl perf trace start`, reproduce, `ssctl perf trace dump
artifacts/hitch.trace.json`, `ssctl perf trace analyze
artifacts/hitch.trace.json`. The analyzer lists the slowest frames with their
per-stage breakdown and any GC pause, UI stall or hub refresh that
overlapped them. Open the same file in https://ui.perfetto.dev to look.

A/B a knob without relaunching: write a spec and run `ssctl perf experiment
spec.json`. The runner interleaves variants, restarts what each knob needs,
warms up, measures, restores every knob, and writes `result.json` and
`summary.md`.

Hear the app: `ssctl audio listen --seconds 5 --latency --wav
artifacts/out.wav`.

Use the UI like a person: `ssctl ui tree`, then `ssctl ui invoke <name>
[toggle|set-value <v>|select <item>]`, then `ssctl screenshot` to look.

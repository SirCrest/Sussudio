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
  read shortens the next window. (Fixed later: on-demand refreshes no longer
  move those baselines, D15.)
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
| D9 | The diagnostics hub polls every 500 ms whether or not anyone reads it, building an 807-field, 121 KB snapshot with about four UI-thread dispatches per tick (the preview runtime snapshot is requested three times by overload shape) and copying and sorting about seven 1,200-sample rings. Its own cost is not measured, so the observer effect is unknown. `SnapshotUpdated` has no subscribers. **Measured and reduced in the hub observer-cost slice (2.11):** a refresh cost 22 ms (p50) and 324 KB; it now costs 1.8 ms and 133 KB. | Live: `ssctl --json state` = 121,261 bytes; about 0.4 MB/s steady allocation; `PreviewRuntimeSnapshotControllers.cs`, `MainViewModel.cs` snapshot dispatch |
| D10 | Every automation acknowledgement embeds the cached snapshot (`CreateResponse(includeSnapshot: true)` by default), so a burst of control commands serializes about 120 KB each. | `AutomationCommandDispatcher.CreateResponse` |
| D11 | Full-rate frame data never reaches the agent: the frame-time graph's 10 s QPC history is UI-only, and the hub exposes only the latest 64 of 4,096 frame-ledger events every 500 ms. The 2-minute performance timeline omits audio, A/V sync, recording, encoder, NVML and MJPEG fields. | `PreviewFrameTimeHistory`, `FrameLedger`, `PerformanceTimelineEntry` |
| D12 | The agent cannot read UI state (overlay, settings, InfoBars, section expansion, window rect) or press controls that have no purpose-built command (Flashback nudge and keyboard transport, timeline scrubbing). | Automation surface survey |
| D13 | The DWM composed-frame rate was read as the display rate. With the preview presenting 4K60 on a 120 Hz display (independent flip or an overlay plane, so DWM does not compose it), `DwmGetCompositionTimingInfo` showed 0.057 composed frames per second, which looks like a stalled display. DWM's refresh counter is not a hardware vblank counter either: it advances with composition activity. **Fixed in slice 2b:** the report adds a `Display` section from the swap chain's DXGI frame statistics and classifies the DWM rate against the present rate (2.8.5). | Live: `Dwm.FramesComposedPerSec = 0.057` at 4K60 on a 120 Hz display; an idle 120 Hz desktop measured 0.2 refresh-counter increments per second |
| D14 | `ssctl --json timeline --max N` printed N + 1 timestamps with the last one repeated. The timeline held exactly N entries; the response envelope embedded the cached snapshot (D10), and the snapshot's `TimestampUtc` equals the newest timeline entry's. **Fixed in slice 2b:** the timeline response carries no snapshot. | `AutomationCommandDispatcher.ExecuteGetPerformanceTimelineCommand`; `CreateResponse` default `includeSnapshot: true` |
| D15 | The performance timeline was not a cadence timeline. Every on-demand refresh (`GetSnapshot`, `AssertSnapshot`, the recording toggle, a recording or file verification) appended an entry and advanced the baselines behind the "recent" counters and process CPU percent. A client polling `ssctl state` therefore packed extra entries between poll ticks, and the next tick's entry covered only the time since that read, so a missed refresh counted in the on-demand snapshot never appeared in the timeline. **Fixed in the loose-ends slice:** only the poll tick appends and advances baselines (`SnapshotRefreshKind`); an on-demand snapshot's "recent" counts and CPU percent cover the time since the last poll tick. The wire shape is unchanged. | `AutomationDiagnosticsHub.Snapshots.cs` `RefreshSnapshotCoreAsync`; `Verify-PerfTelemetry.ps1` timeline cadence check |

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

Slice 2b closes GPU execution time, GPU engine utilization, VRAM, DWM and display truth, and UI-thread responsiveness (2.8).

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
| `preview.queue_dwell` | hist | renderer submit until the render thread starts rendering the frame. Recorded only for a frame that reaches the render pass, after every drop check: a frame skipped as stale, dropped by a stopping renderer or dropped for a superseded generation is a drop, not a dwell. A frame the render pass then skips is recorded (it did wait) |
| `preview.frame_latency_wait` | hist | waitable swap-chain wait |
| `preview.input_upload` | hist | render CPU stage |
| `preview.render_submit` | hist | render CPU stage |
| `preview.present_call` | hist | `Present` call duration |
| `preview.render_total` | hist | total render CPU per frame |
| `preview.gpu_render` | hist | GPU execution time of the preview render pass from D3D11 timestamp queries: the shader `Draw` or the `VideoProcessorBlt`, not the HDR input upload; recorded only while GPU timing is wanted, an upper bound on a shared device. A span of zero GPU ticks is counted in `preview.gpu_timing_unmeasured` instead of being recorded as a 0 us render, and a real span under 1 us is recorded as 1 us, so the histogram holds no zeros (2.8.4) |
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
| `ui.dispatcher_latency` | hist | UI responsiveness probe: a no-op enqueued on the UI dispatcher every 100 ms, enqueue to run (2.8.2) |
| `gc.pause` | hist | each GC suspension, from the runtime event listener |
| `gpu.engine.3d` / `.video_encode` / `.video_decode` / `.copy` / `.other` | hist (percent) | PDH GPU Engine utilization for this process, summed over its engine instances of that type, 1 Hz while sensors run (2.8.3). Recorded in hundredths of a percent (`CentiPercent`, so the integer histogram keeps two decimals) and reported as percent; `other` is every remaining engine type |
| `gpu.nvml.gpu_util` / `.encoder_util` / `.decoder_util` | hist (percent) | each NVML poll |
| `telemetry.hub_refresh` | hist | diagnostics hub snapshot refresh |
| `telemetry.hub_alloc` | hist (bytes) | process-wide allocated bytes during one refresh; an upper bound, because other threads allocate meanwhile |
| `automation.command` | hist | automation command dispatch |
| `preview.drop.<reason>` | counter | renderer drops by reason, from a fixed table (2.2.1) |
| `preview.jitter_drop.<reason>` | counter | jitter-buffer drops by reason, fixed table; a skipped run of missing sequence numbers adds its length |
| `preview.jitter_underflow.<reason>` | counter | jitter-buffer underflows by reason, fixed table |
| `capture.read_errors` | counter | read-loop exceptions |
| `ui.stall_50ms` | counter | UI probe waits over 50 ms |
| `preview.gpu_timing_skipped` | counter | frames not GPU-timed while timing was wanted: every query slot still pending, or a disjoint or implausible timestamp result |
| `preview.gpu_timing_unmeasured` | counter | GPU timing spans whose begin and end timestamps were identical (zero GPU ticks): the timed pass was not measurable on this queue (2.8.4); counted here and not recorded in `preview.gpu_render` |
| `preview.ingress_lookup_miss` | counter | live frames presented while the ingress clock was classified QPC whose MF sample time was no longer in the ingress sample ledger (evicted, or read before the clock was classified); such a frame has no `preview.ingress_to_visible` sample, so a rising count says the percentiles of that metric are computed from fewer frames than were presented (2.6) |
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
| `preview.jitter_residence`; same, `preview.jitter_drop.*`, `preview.jitter_underflow.*` | `MjpegPreviewJitterBuffer.SubmitFrame` (enqueue stamp to submit, outside `_sync`), `RecordDroppedFrame` (the counter add only, under `_sync` when the caller holds it; the trace instant outside it), `RecordUnderflow`, `TryRecordResumeReprimeMiss` |
| `preview.queue_dwell`, `preview.frame_latency_wait`, `preview.input_upload`, `preview.render_submit`, `preview.present_call`, `preview.render_total`, `preview.present_interval`, `preview.pipeline_latency`, `preview.ingress_to_visible`, `preview.drop.*`; matching stages and `preview.drop` | `D3D11PreviewRenderer` (`EnqueuePendingFrame`, the render loop, `WaitForFrameLatencySignal`, `TrackFrameDropped`, `TrackPresentCadence`, `TrackPipelineLatency`, `TrackRenderCpuTiming`) and `PresentAndTrackFrame` in `D3D11PreviewRenderer.RenderPasses.cs` |
| `preview.gpu_render`, `preview.gpu_timing_skipped`, `preview.gpu_timing_unmeasured`; `preview.gpu_render` | `GpuTimestampRing` records a finished query when a later frame reads it back. The renderer opens and closes the span with `BeginGpuTiming(frameId, path)`/`EndGpuTiming` (`D3D11PreviewRenderer.GpuTiming.cs`; the path names the render path so counts can be split per path) around the `Draw` of the NV12 and HDR shader paths and the `VideoProcessorBlt` of the video-processor path in `D3D11PreviewRenderer.RenderPasses.cs`. The trace span starts at the CPU submit time, lasts the GPU duration, and uses the frame's arrival tick as its id (2.8.4) |
| `ui.dispatcher_latency`, `ui.stall_50ms`; `ui.dispatcher_latency` | `UiResponsivenessProbe.OnDispatcher`, on the UI thread when the probe's no-op runs; the trace span (UI thread track, id -1) only for waits above 16 ms |
| `gpu.engine.*` | `GpuEngineSensor.SampleOnce` on its sensor thread, once a second, from the second PDH sample on (rate counters need two) |
| `recording.*`, `flashback.queue_dwell`, `flashback.encode_submit`, `flashback.mux_write` | `LibAvRecordingSink.DrainVideoPackets`/`DrainGpuPackets`/`DrainCudaPackets` and `FlashbackEncoderSink.DrainVideoPackets`/`DrainGpuPackets` for queue dwell (QPC stamp on the packet at enqueue, recorded after dequeue and outside `_videoQueueSync`; the GPU packet also carries the frame's arrival tick, passed by the UVC call site through `TryEnqueueGpuVideoFrame`, and uses it as the trace id); `LibAvEncoder.SendVideoFrame`/`SendGpuVideoFrame`/`SendCudaVideoFrame` and `WritePacket` for encode and mux, through the injected `EncoderPerfMetrics` role |
| `flashback.segment_rotation` | `FlashbackEncoderSink.RotateSegment` through `PerfScope` (covers both returns and the terminal rethrow) |
| `flashback.playback_decode`, `flashback.playback_present_interval` | `FlashbackPlaybackController.PaceAndDecodeFrame` and `UpdateCadenceMetrics` |
| `audio.capture_interval`, `audio.capture_drain`; `audio.capture` | `WasapiAudioCapture.CaptureThreadMain`, beside the existing `TrackCaptureCallback` (its TickCount64 trackers are unchanged) |
| `audio.render_interval`, `audio.render_queue_ms`, `audio.render_underrun` | `WasapiAudioPlayback.RenderAvailableFrames` (interval and queue depth per render event, before `_chunkLock`); underrun counter beside each `_renderSilenceCount` increment |
| `gpu.nvml.*` | `NvmlMonitor` poll, for each value NVML returned |
| `telemetry.hub_refresh`, `telemetry.hub_alloc`; `telemetry.hub_refresh` | `AutomationDiagnosticsHub.RefreshSnapshotAsync`, after the refresh gate, including a failed refresh |
| `automation.command`; `automation.command` | `AutomationCommandDispatcher.ExecuteAsync` finally block; the trace id is the command kind |

Not recorded yet: the reserved `audio.render` trace stage (the render interval
and queue depth have no span to draw). Trace placement is approximate for two preview
stages: `preview.input_upload` is anchored at the start of the render and
`preview.render_submit` ends where `Present` begins, because the render passes
do not carry stage start times out; durations are exact.

Reason tables keep the drop and underflow counters bounded and allocation
free. The renderer and the jitter buffer pass open-ended reason strings; each
maps through a fixed switch to a small enum code, and anything unknown, null or
empty lands in the last entry, `other`. A reason of the form `<x>:stale` maps to
`stale`. Code 0 is reserved ("none"), so a trace instant with no reason is
distinguishable.

Classification never runs inside a lock. A call site that knows its reason
passes the code as a literal beside the string (`PreviewDropReason.RendererBacklog`,
`JitterDropReason.QueueFull`), so under `_lifecycleLock` (renderer) and `_sync`
(jitter buffer) a drop costs only the interlocked counter add. A loop that
drops many frames under the lock (the renderer's pending drain, the jitter
buffer's `ClearQueue`) counts inside the lock and adds the total once after it
(`ClearQueue` records one `RecordDroppedFrame` for the whole run), and the
trace instants are recorded after the lock is released. The counter is a plain
interlocked add; it is not "free of cost" under the lock, it is bounded and
never blocks.

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

`end` builds its report before it closes the window. Closing a window can
release the sensor demand (the UI probe and the GPU engine sensor stop, GPU
timing goes idle), so a report built afterwards read the interval it described
from sensors that had already stopped: `Sensors` said `idle`, the GPU timing
line said `wanted=no` while still `active`, and `Gpu.DedicatedBytes` was null.
`PerfWindowSet.End(name, build)` runs the builder with the window still open,
then removes it only if it is still that window (two callers ending one name:
exactly one gets a report; a window restarted meanwhile is left open) and only
then raises `Changed`. A builder that throws leaves the window open and
releases nothing. The experiment runner closes each run's window through the
same `PerfWindow end` command, so its per-run reports have the same property.

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
- Writers claim an index with `Interlocked.Increment` on the head, take
  ownership of the slot by compare-exchanging its last published stamp for the
  in-progress mark (-1), fill the fields, then publish `Stamp = index + 1`
  (zero means never written). The reader copies a slot and keeps it only if
  the stamp read before and after equals `index + 1`, so a slot that is
  mid-write or was overwritten during the copy is skipped, never emitted torn.
  A writer that was preempted between claiming its index and storing, long
  enough for the ring to wrap, finds the slot owned by a newer lap (a newer
  stamp, or the in-progress mark): it drops its own, stale event instead of
  storing fields a reader could pair with the other writer's stamp, and counts
  it as `abandoned`. This costs the writers one compare-exchange (a fence they
  already paid in the old protocol's first store) and no extra memory.
- The dump streams the ring and never copies it. `BeginRead` fixes the range
  (the newest `capacity` indices), `ReadChunk` fills a caller buffer of 1024
  events at a time, and the recorder walks the ring twice through one pooled
  chunk: the first pass finds the time origin (the earliest retained
  timestamp) and the set of thread ids that appear, the second writes. A dump
  therefore allocates tens of kilobytes however large the ring is (a default
  2^18-event ring is 10.5 MB and the largest 168 MB, which an array copy would
  have placed on the large object heap on every dump). The dump's `sussudio`
  object reports `recorded`, `retained`, `overwritten` (events that left the
  ring) and `abandoned` (events dropped by the stale-writer rule above).
- `start` reuses the previous trace's ring when its capacity is unchanged and
  no dump is reading it (the slots are cleared, so no old stamp can match a new
  index); otherwise it allocates. Repeated traces therefore do not each
  allocate a ring on the large object heap.
- Markers (`perf mark`) and thread names are kept beside the ring and
  emitted as instant and metadata events. Thread names are labels by managed
  thread id, set by `HotThreads.Register`. When a registration retires its label
  is removed only if the id still maps to that registration (managed ids are
  recycled, so a newer thread that took the id keeps its own name), and a
  bounded list of 256 retired labels keeps `<role> (retired)` so the events a
  finished thread left in the ring are still named, honestly marked. The dump
  names a thread that was alive during the traced interval, not only the ones
  alive at dump time.
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
  (kind 31), `telemetry.hub_refresh` (kind 32, id -1) and `telemetry.gpu_engines` (kind 33, id -1, 2.8.3). `automation.command`
  uses the command kind as its id. Encoder-level spans take the frame's
  arrival timestamp as id when the packet carries it: the CPU lane with a
  lease, and the D3D11 GPU lane, whose packet carries the arrival tick the UVC
  call site passes to `TryEnqueueGpuVideoFrame` (its `queue_dwell` and
  `encode_submit` therefore join to the frame's other stages). The id is -1
  otherwise (raw buffers, the CUDA lane, and a GPU frame whose producer gave
  no tick); `*.mux_write` is always -1.

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
`TUNING_KNOB_SET name= old= new= apply=` and appears in the next report. A
call that changes nothing (a reset of a knob that is already unset, or a set
to the value it already has) succeeds but writes no line, so the log never
says `old=<unset> new=<unset>`.

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
| `SUSSUDIO_TELEMETRY_SENSORS` | auto | live, `auto\|on\|off`: `auto` runs the demand-driven sensors (UI probe, GPU engine counters) while a perf window is open or a frame trace runs; `on` always; `off` never (2.8.1) |
| `SUSSUDIO_PREVIEW_GPU_TIMING` | auto | live, `auto\|on\|off`: D3D11 timestamp queries around the preview render pass; `on` and `off` win, `auto` follows the sensor demand, `off` creates no query objects, and the WARP software adapter never runs them (2.8.4) |

Both are read as parsed modes. Text set by hand is case-insensitive, `1` and `true` mean `on`, `0` and `false` mean `off`, and anything unrecognised means `auto`; the knob store itself accepts only the three choices. The report's `Sensors` section states the mode in force.

While a perf experiment holds a knob (2.12), an automation change to it without
the experiment's lease id is refused with `invalid-state`, failure `Leased`, and a
`TUNING_KNOB_REFUSED` log line; the error names the experiment.

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
a QPC tick in `PerfIngressSamples`, a 4-way set-associative ledger keyed by
the frame's arrival timestamp: 256 sets of four 16-byte (arrival, sample)
entries, one 64-byte cache line per set (the table is pinned and aligned to a
cache line). The renderer looks the frame up by the same timestamp to compute
`preview.ingress_to_visible`. This avoids adding a field to the capture
delegates, `PooledVideoFrame`, the MJPEG work item and `PreviewFrameTracking`,
and `SourcePtsTicks` is not repurposed. An entry is written by the single
read-loop thread (invalidate the key, write the value, publish the key) and
read lock-free (probe the four ways for the key, read the value, barrier,
re-check the key), so a reader sees a miss or the matching pair, never another
frame's sample.

A frame hashes to one set. The writer takes the way that already holds the key,
else an empty way, else evicts the OLDEST entry in the set (the smallest arrival
tick). A frame is therefore evicted only when four newer frames land in its set
while it is still in flight. Eviction does happen: the frames that stay in
flight longest are the slow tail, and a direct-mapped table lost roughly one
frame in ten of those at 100 newer frames, which biased `p99.9` and `max` of
`preview.ingress_to_visible` low. A lookup that misses records nothing rather
than a wrong value, and the renderer counts it in `preview.ingress_lookup_miss`
(a live frame presented while the clock was `qpc` that had no sample), so the
number of frames behind that metric's percentiles can be checked against
`preview.present_interval`.

### 2.7 Thread CPU

`HotThreads.Register(role)` is called at the start of every dedicated hot
thread (beside its MMCSS registration) and disposed on exit. Reports read
`GetThreadTimes` and `QueryThreadCycleTime` per registered thread and give
CPU percent of one core over the window, priority, and whether MMCSS
registration succeeded. Exited threads keep their final totals in a bounded
retired list so a window that spans a preview restart still adds up.

`Priority` is one consistent label. The Win32 base priorities keep their names
(`Idle`, `Lowest`, `BelowNormal`, `Normal`, `AboveNormal`, `Highest`,
`TimeCritical`); any other value is a priority the MMCSS scheduler set and is
shown as `mmcss/<n>` for a thread registered with MMCSS (`raw/<n>` for any other
thread), with the raw number kept in the label. It was a bare `13` or `19`
before, in the same field as the names.

Registered roles (slice 2a): `capture.read` (the read loop on its pool thread,
disposed when the loop exits), `mjpeg.worker-<n>`, `mjpeg.emitter`,
`mjpeg.jitter`, `preview.render`, `recording.encode`, `flashback.encode`,
`flashback.playback`, `audio.capture` (one per capture instance) and
`audio.render`. Each passes whether its MMCSS registration succeeded.

### 2.8 Sensors

Demand-driven sensors start a thread or create GPU objects, so they run only while someone wants the numbers (2.8.1). Everything
else is read on demand or at an existing poll and costs nothing while nobody asks. The report's `Sensors` section states whether
demand exists and what each sensor is doing (3.1).

#### 2.8.1 Demand and the supervisor

`SUSSUDIO_TELEMETRY_SENSORS` (`auto|on|off`, default `auto`, live) decides. `off` wins over everything, `on` wins over demand, and
`auto` is active while any perf window is open or a frame trace is running. `Sensors.Reason` says which: `knob:off`, `knob:on`,
`window:<names>`, `trace` or `idle` (a window is named in preference to a trace). `SUSSUDIO_PREVIEW_GPU_TIMING` (same values) has
the last word for GPU timing: `on` and `off` win, and `auto` follows the sensor demand, including the sensors knob (sensors `off`
also quiets automatic GPU timing).

`TelemetrySensorSupervisor` (`SensorDemand.cs`) starts and stops the thread sensors (`IDemandSensor`: the UI probe and the GPU
engine sensor) and has no timer or poll of its own. The owners of the three demand inputs raise `Changed` after a real change,
outside their locks, with handler exceptions swallowed: `PerfWindowSet`, `FrameTraceRecorder` and `TuningKnobStore`. The
`TelemetrySensors` facade (configured by `MainWindow` at startup, stopped by window shutdown cleanup) subscribes once and calls
`Reevaluate()`. Start and stop are idempotent. A sensor that throws on start or stop is reported as `supervisor: <error>` in
`Sensors.Details` and does not stop the others. An idle app has no sensor thread and no timer.

The render thread cannot read the environment or take a lock per frame, so GPU timing asks a `CachedDemand`, which re-evaluates
from cached integer modes at most once a second (a probe that throws keeps the previous answer). Between evaluations the question
costs one timestamp compare and one volatile read. GPU timing therefore reacts to a window opening within about a second.

#### 2.8.2 UI responsiveness

`UiResponsivenessProbe` (`UiResponsivenessProbe.cs`, thread `telemetry.ui_probe`) wakes every 100 ms and enqueues a no-op through an
injected `Func<Action,bool>`; `MainWindow` supplies `DispatcherQueue.TryEnqueue` with one cached handler, so the probe has no WinUI
dependency. When the no-op runs on the UI thread it records the enqueue-to-run wait into `ui.dispatcher_latency`, increments
`ui.stall_50ms` above 50 ms, and, while a frame trace runs, draws a `UiDispatcherLatency` span on the UI thread's track for waits
above 16 ms (one 60 Hz frame). At most one probe is outstanding, so a blocked UI thread gets no backlog and the probe measures the
stall instead of adding to it. A rejected or throwing enqueue is counted, never raised. The details line reads
`ui_probe: running enqueued=.. completed=.. skipped_busy=.. enqueue_failures=..`.

#### 2.8.3 GPU engines, dedicated memory and VRAM

`GpuEngineSensor` (`GpuEngineSensor.cs`, thread `telemetry.gpu_engines`) samples PDH once a second through `NativePdh`, behind the
`IGpuCounterReader` seam: `\GPU Engine(*)\Utilization Percentage` and `\GPU Process Memory(*)\Dedicated Usage`. Instances are
filtered to this process by the exact prefix `pid_<pid>_` (a bare `pid_N*` also matches every longer pid that starts with N) and
summed per engine type into `gpu.engine.3d`, `.video_encode`, `.video_decode`, `.copy` and `.other`. A sum can exceed 100 when the
process uses several engines or adapters of one type. `Gpu.DedicatedBytes` is the process's dedicated GPU memory from the same
sample.

Each PDH call is a `telemetry.gpu_engines` trace span on the sensor thread (kind 33, id -1; arg 1 is the open of the counters, arg 0
a sample read, which is the collect that expands the `GPU Engine` wildcard), so the sensor's cost sits on the timeline beside the
stages it might disturb. Measured on the RTX 4090 (SDR preview, Flashback on, Debug build): the first open in a process took about
250 ms, every later open about 0.5 ms and a read 0.4 to 2.3 ms, all on the sensor thread, and neither the open nor the reads lined up
with a stall in the encoder, mux, UI dispatcher or hub-refresh spans (6 cycles of sensor start, 8 s trace and stop: no encoder or
mux span past 15 ms, the UI dispatcher at most 24 ms, `telemetry.hub_refresh` at most 67 ms). The same 6 cycles with
`SUSSUDIO_TELEMETRY_SENSORS` off, where no PDH ran at all, had two Flashback `mux_write` spans of 33 to 41 ms (most likely
segment-file writes) and `telemetry.hub_refresh` up to 49 ms, so spans of that size are baseline and not sensor cost. The one-off 1 s
stall seen in the first live window was not reproduced in either series and is not attributed to PDH.

A missing sample is never recorded as 0 %. Rate counters need two samples, so the first tick records nothing. A tick in which PDH has
no instance for this process (it has not touched the GPU yet, or just released its device) records nothing and counts as pending.
PDH that cannot open or read is `unavailable`: the sensor retries with backoff and reopens the counters if they fail later, and
every reader it abandons is disposed. A genuine 0 % is recorded as 0. The details line reads `gpu_engines: stopped`,
`gpu_engines: unavailable, retrying (open_failures=.., last_error=..)` or `gpu_engines: running samples=N`, with a note after three
pending ticks. `Gpu.DedicatedBytes` is null whenever the sensor is stopped, pending or unavailable.

VRAM is not a thread. `GpuMemorySensor.Read()` runs when a report is built: it enumerates the DXGI adapters and calls
`IDXGIAdapter3.QueryVideoMemoryInfo` on the local and non-local segment groups, so `Gpu.Adapters` holds instantaneous per-process
budget and usage (the budget is what the OS allows this process now and shrinks under pressure from other applications). An adapter
that cannot report is skipped; a failed enumeration leaves `Adapters` empty and `AdaptersError` says why. The WARP adapter appears
with `Software: true`.

#### 2.8.4 GPU render timing

`preview.gpu_render` is the GPU execution time of the preview render pass, from D3D11 timestamp queries
(`D3D11TimestampQueries.cs`: a disjoint query plus begin and end timestamps per slot, behind the `IGpuTimestampBackend` seam;
`GpuTimestampRing.cs`: the ring; `D3D11PreviewRenderer.GpuTiming.cs`: the renderer side).

- **Span.** The `Draw` of the NV12 and HDR shader paths, or the `VideoProcessorBlt` of the video-processor path. The HDR pre-draw
  input upload is deliberately not in the span: it is input transfer and is already `preview.input_upload` on the CPU side, and
  folding it in would make the metric mean "copy plus draw" and stop it isolating what the shader costs. Clear, state binding and
  `Present` are outside it too.
- **Ring.** Six slots (cap eight). A frame opens a slot immediately before its draw, after every early return of the render pass, so a
  query is never opened for a frame that does not draw, and the draw sits in a `try/finally` so an exception cannot leave one open.
  Results are read on later frames with a non-blocking `GetData(..., DoNotFlush)` (`S_FALSE` means not ready). A frame never waits for
  the GPU: with every slot still pending the frame is skipped and counted in `preview.gpu_timing_skipped`. A disjoint result (the GPU
  clock changed during the span) and an implausible one (negative, or a zero frequency) are skipped and counted; a result that has not
  arrived after a second is abandoned so the ring keeps cycling.
- **Failure.** A backend that fails or throws (a removed device) fails the ring. Timing then stays off and costs nothing until demand
  ends or the device is re-created, and the exception never reaches the render loop.
- **Zero-tick spans.** A begin and an end timestamp that are identical (zero GPU ticks) cannot be told from a pass that was not
  timed on that queue, and they were 11.5 % of frames with Flashback on and 59 % with it off, recorded as exact 0 us renders. They
  are not a measurement, so `GpuTimestampRing` counts them in `preview.gpu_timing_unmeasured`, does not record them in
  `preview.gpu_render` and does not draw a trace span for them. A span that really is shorter than 1 us is recorded as 1 us, so the
  histogram never holds a zero and an unmeasured span can never be confused with a fast one. The ring keeps recorded and unmeasured
  counts per render path (`GpuTimingPath`: `VideoProcessor`, `Nv12Shader`, `HdrShader`), logs them when it is released
  (`D3D11_PREVIEW_GPU_TIMING released ... vp=rec/unm nv12=rec/unm hdr=rec/unm`) and the state line names the path in use.
  *Evidenced:* in the live runs on the RTX 4090 (SDR source, `PreviewRendererMode: D3D11VideoProcessor`) every unmeasured span came
  from the `VideoProcessorBlt` path. With Flashback on it was 8 to 12 % of that path's frames (`vp=433/48 nv12=0/0 hdr=0/0`),
  scattered at random rather than periodic and not tied to any other stage's duration; with Flashback off, in a later run, it was
  1,490 of 1,856 frames (`vp=366/1490 nv12=0/0 hdr=0/0`, 80 %; the first measurement was 59 %), so the fraction differs a lot between
  runs and configurations and is not a constant of the path (the cause of the difference was not investigated). They are not sub-microsecond spans: the timestamps were identical. The shader
  paths were not exercised in those runs, so how they behave is not measured. *Hypothesis, not verified:* the blit can complete on a
  hardware video-processing queue that does not write the timestamps the query reads. Nothing in the app can repair that reading, so
  the meaning per path is: on the shader paths a span brackets a `Draw` the same context executes, and a zero would be a pass that did
  not reach the GPU timer; on the `VideoProcessorBlt` path `preview.gpu_render` is the subset of frames the timer could see. Treat it
  as a sample of those frames and read `preview.gpu_timing_unmeasured` beside it.
- **Clock.** GPU timestamps are in the GPU's own clock, not QPC, so only the duration is a GPU measurement. The `PreviewGpuRender`
  trace span is anchored at the CPU time the commands were submitted, lasts the GPU duration, and carries the frame's arrival tick as
  its id, so the analyzer can join it to that frame's CPU stages. It is written when the result is read back, a few frames later; its
  position is the submit time, not a claim about when the GPU ran.
- **Shared device.** The preview usually runs on the capture pipeline's shared device and its multithread-protected immediate
  context. Commands from other threads that the context accepts between the span's begin and end are inside it, and the GPU may be
  draining other queued work, so `preview.gpu_render` is an upper bound under contention and exact only on a quiet device. A
  renderer-owned device has no such contamination. The state line says `shared_device=`.
- **WARP.** Never on the software adapter: a CPU rasterizer's timestamps measure CPU emulation and the queries cost real time there.
  The state reads `disabled: software (WARP) adapter <name>` and no query object exists.
- **Off.** With demand off, no query object exists; the cost is the cached demand check per frame.
- **Device lost and re-initialisation.** `CleanupD3DResources` releases the queries (state `released: device or swap chain reset`)
  before it disposes the context. They are re-created lazily on the first frame after `InitializeD3D` if demand still exists.
- **State.** `Sensors.Details` carries `gpu_timing: mode=<mode> wanted=<yes|no> state=<state>` where the state is `not created`,
  `active adapter=<name> slots=<n> shared_device=<bool> path=<VideoProcessor|Nv12Shader|HdrShader> (upper bound under contention; zero-tick spans are counted in preview.gpu_timing_unmeasured)`, `off: no demand`,
  `disabled: software (WARP) adapter <name>`, `unavailable: <reason>`, `failed: <reason>` or
  `released: device or swap chain reset`.

#### 2.8.5 DWM and display truth

`Dwm.FramesComposedPerSec` is DWM's composition pass rate (`DwmGetCompositionTimingInfo` `FrameNumber`), not the rate at which frames
reach the display. A preview that reaches the screen by independent flip, direct flip or an overlay plane is scanned out without DWM
composing it, so a 4K60 preview on a 120 Hz display read 0.057 composed frames per second while presenting normally. As a display
rate that looks like a stalled display; it is not. DWM's `RefreshCount` is not a hardware vblank counter either: it advances with
composition activity (an idle 120 Hz desktop measured 0.2 per second), so it is reported only as `Dwm.RefreshCountPerSec`.

The display truth is the preview swap chain's own DXGI frame statistics. The renderer already samples them every N presents
(`SUSSUDIO_PREVIEW_DXGI_FRAME_STATS`, default on; `..._SAMPLE_INTERVAL`, default 2); slice 2b adds no per-present call, only a
generation counter bumped by each device re-creation. The report's `Display` section differences two samples and divides both
counters by the QPC time between their `SyncQPCTime` stamps (not by the report clock, which may be far from the last sample if the
preview stopped): `PresentsPerSec` (the swap chain's `PresentCount`), `VblankPerSec` (`SyncRefreshCount`, the display's vertical sync
count) and `PresentsPerVblank` (0.5 for 60 fps on a 120 Hz display). `Display.Available` is false, with a `Note`, when no renderer is
sampling, when a lifetime report is asked for (no baseline: begin a window with the preview running), when the swap chain or device
was re-created during the interval (the counters restarted), when no new sample arrived (the preview was not presenting), or when a
counter went backwards.

`Dwm.CompositionPath` reads the DWM rate against `Display.PresentsPerSec`: `idle` (under one present a second, so unrelated screen
activity says nothing about the preview), `bypass` (composed under a quarter of presents, so the preview skips DWM), `composed` (at
least three quarters), `mixed` (between) and `unknown` (a reading is missing; a missing reading is never assumed to be zero).
`Dwm.Interpretation` says the same in words with the refresh and present rates.

#### 2.8.6 Always on, read on demand

- Process: `GetProcessTimes`, `GetProcessMemoryInfo`, `GetProcessIoCounters`, handle and thread counts.
- GC: collection counts, `GC.GetTotalPauseDuration`, allocated bytes, and a runtime `EventListener` for per-GC pause (`gc.pause`,
  trace events).
- DWM: `DwmGetCompositionTimingInfo` refresh period, composed-pass count and refresh counter (2.8.5).
- NVML values recorded at each existing NVML poll.
- Log tap: the logger writer keeps the last 4,096 lines and per-caller and per-tag line counts for spam detection. A tap that
  throws never ends the writer loop: the failure is reported once through the logger's fallback trace and the next line is
  written normally. `NATIVEXU_AT` (the capture card's AT-command answers) is logged when a command's answer is first seen or
  changes in the bytes the line shows (its first 32), and, only with `Logger.VerboseEnabled`, once per 30 s per command as a
  sign that the poll is still reading. `NATIVEXU_TOPOLOGY` is logged when a path's node layout is first seen or changes. The
  rolling poll re-reads every command and the topology each cycle and used to write 7,678 of 10,043 tap lines in one live window
  with identical text. Nothing parses these lines.

### 2.9 Ears: audio output probe

`CaptureAudioOutput` (command 65) hears what Sussudio actually plays: it records
this process's own rendered audio and returns an analysis. Owner:
`AudioOutputProbe` (probe), `AudioSignalAnalyzer` and `AudioLatencyAnalyzer`
(pure analysis), `AudioMonitorTap` (the tap ring), `AudioWavWriter`, and
`WasapiProcessLoopbackInterop` (activation). Payload
`{ seconds?, outputPath?, measureLatency? }`: `seconds` is 0.5 to 30 (default 5)
and out-of-range values are rejected, never clamped; `outputPath` is a
`WriteFile` path that must end in `.wav`.

**Capture.** The stream is the audio the app sends to the mixer, taken with
`ActivateAudioInterfaceAsync("VAD\Process_Loopback")` in include-process-tree
mode for this process id, so system sounds and other applications are excluded
and the endpoint volume does not apply. It needs Windows build 20348 or later
on x64 (`unsupported-os` otherwise). The activation completion handler is agile
(a 5 s timeout bounds it). The capture runs on its own MTA thread
(`AudioOutputProbe.Capture`, above-normal priority), event driven in 50 ms wait
slices, draining at most 256 packets per pass, at 48 kHz float32 stereo. The
analysis and the WAV write run on a worker after the capture stopped. The app's
audio threads are never blocked and never allocate for the probe; the only code
on them is the tap hook below, and only while armed. A capture buffer that fills
before the requested time elapses stops the capture early and sets
`Diagnostics.BufferTruncated`.

**Silence.** While the process renders nothing, the loopback stream delivers
continuous 10 ms packets of exact zeros (observed with a scratch harness against
the live Windows mixer, not yet through the running app), so silence is judged
from the analysis, not from the absence of packets. Verdict `silent`, or a
capture with no packets at all (an endpoint change, for example), is status
`no-audio-rendered`.
That is a failed response (`command-failed`) that still carries the analysis and
diagnostics: silent output is never reported as a pass, because it usually means
the preview audio is off or muted.

**Status.** `captured` is the only success. The failures are `unsupported-os`,
`tap-busy` (another latency measurement has the tap), `activation-failed`,
`initialize-failed`, `capture-failed`, `no-audio-rendered` and `wav-write-failed`
(the analysis is still returned, `FailureStage` is `wav`). Activation,
initialize and capture failures carry `FailureStage` and `FailureHResult`. Every
failure is visible; nothing falls back to another source.

**Analysis** (`Data.Analysis`, 10 ms blocks, dBFS floored at -120): RMS and peak
per channel and the channel balance; the share of blocks below -60 dBFS;
clipping (runs of at least two samples at or above 0.999, counted as samples and
events); clicks (a second difference of at least 0.05 and at least ten times the
local RMS second difference, with a cooldown, and neighbours that stay quiet, so
bright tones, noise and level steps do not count while an isolated spike or a DC
step does); dropouts (exact-zero runs of 0.5 to 100 ms with audio above -50 dBFS
for at least 10 ms on both sides, so leading, trailing and between-sound silence
is not a dropout); non-finite samples (counted and neutralised); and the packet
flags (discontinuity, silent, timestamp error) and the largest timestamp gap.
`Verdict` is `empty`, `silent`, `ok` or `issues`, and `Findings` states each
issue in words.

**Latency** (`measureLatency`). A measurement arms `AudioMonitorTap` for the
capture plus two seconds: a preallocated single-writer ring of 48 kHz stereo
with a QPC drain stamp per packet (500 packets per second budgeted). The hook is
`WasapiAudioCapture.OfferToMonitorTap`, called on the capture-delivery thread
after sample conversion and before the packet is handed to playback; only a
capture instance that feeds a preview playback offers (the microphone instance
never does). Disarmed it costs one volatile read of a static reference; armed it
is a QPC stamp and a copy into the ring, with no lock and no allocation. The
reader validates what it copied, so a snapshot that raced the writer loses the
overwritten prefix and never returns torn samples; a packet larger than the
ring is rejected and counted (`Diagnostics.TapRejectedPackets`). Both streams
are laid on a common 48 kHz QPC timeline and reduced to 1 ms RMS envelopes,
cross-correlated over -20 to +1000 ms, and the best lag is refined within 3 ms
at sample level when the raw waveforms correlate (`Resolution` is `sample`, else
`envelope-1ms`). A result is `ok` only with an envelope correlation of at least
0.6, a second distinct peak of at most 0.9 of the best, at least 200 overlapping
bins, both inputs above -60 dBFS and the peak inside the window. Otherwise
`insufficient-signal` (silent input or output, unrelated or repeating audio, a
peak at the window edge) or `unavailable` (no tap packets) with a `Reason`;
the tool never reports a plausible guess.

The measured latency is capture-delivery-to-mixer: from a capture packet being
handed to the app until the same audio appears in the app's process loopback
stream. It excludes device input latency and endpoint output latency, and every
result echoes that in `Latency.Scope`.

CLI: `ssctl audio listen [--seconds N] [--wav PATH] [--latency] [--json]`. MCP:
`listen_audio_output`.

### 2.10 Fingers: UI tree and invoke

`GetUiTree` (66) and `InvokeUiElement` (67) let an agent read and operate any
control as an assistive technology would, through automation peers. Both run on
the UI thread through the window's dispatch controller: owners are
`UiAutomationBridge` (walk and actions, `Sussudio/Controllers/Window`) and
`UiElementReader` (element state), with the decisions that need no visual tree in
pure, unit-tested types (`UiTargetResolver`, `UiActionPlanner`,
`UiSelectionMatcher`, `UiSelectionVia`, `UiDisplayMemberPath`,
`UiVisibilityRules`) and the wire types in `UiAutomationModels`.

**Tree.** The walk starts at the window content and at the child of every open
popup (`VisualTreeHelper.GetOpenPopupsForXamlRoot`), so flyouts and menus are
visible. Each node reports its path, type, `x:Name`, `AutomationId`, automation
name, visible text, enabled state and reason, effective visibility and reason,
bounds in window DIPs, the automation patterns its peer supports (invoke,
toggle, value, range-value, selection, selection-item, expand-collapse,
scroll-item), toggle state, range value, text value, selected item and index,
item texts (up to 50, with the full count), expand state, and its
`KeyboardAccelerator`s. A path such as `window/Grid[0]/Button#RecordButton[3]`
selects exactly one element. Passwords are never reported.
*Visibility* is effective: collapsed or opacity at or below 0.01 on the element
or any ancestor, or a zero-sized element, is hidden, and the reason names the
root-most culprit. *Disabled* includes disabled ancestors. `interactiveOnly`
(default true) lists only elements whose peer exposes invoke, toggle, value,
range-value, selection-item, selection or expand-collapse, flattening the layout
containers around them; `false` lists every element. A realized list or combo
item (`ComboBoxItem`, `ListViewItem`, any `SelectorItem` whose owning selector
and index resolve) reports `SelectionItem` and counts as interactive even though
its own automation peer lists no pattern at all (observed live on the open
dropdown of the Flashback buffer combo, where the items first appeared only with
`--all` and as not interactive): the SelectionItem pattern lives on the item-data
peer the selector's peer creates, so such an item is selected through its owner
(see Actions). The output is bounded:
`maxDepth` 1 to 64 (default 10; nodes at the limit are listed without children and
their descendants are counted), at most 1500 emitted nodes and 20000 visited
elements; `Truncated` and `TruncationReason` say when a cap stopped the walk, so a
partial tree is never mistaken for the whole UI. `root` limits the walk to one
element, resolved like an invoke target.

**Resolving a target.** In order: a path from the tree, then `x:Name`, then
`AutomationId`, then automation name, then visible text. `x:Name` and
`AutomationId` match exactly and case-sensitively; automation name and text are
trimmed, whole-string and case-insensitive. The first level that matches decides;
a lower level is never consulted. Among the matches, hidden ones are dropped
when a visible one exists, then disabled ones, then non-interactive ones (so a
label inside a button does not make the button ambiguous). If more than one
element remains the call is `ambiguous`: the error lists the candidates (up to
10, with the total) each with the path that selects exactly it, and `force` never
picks among them. No match is `not-found` with near-miss suggestions. A single
hidden element is `hidden` and a single disabled one is `disabled` unless `force`
is true; the response lists what `force` overrode.

**Actions.** `action` is `invoke`, `toggle`, `set-value`, `select`, `expand`,
`collapse`, `focus` or `scroll-into-view`; omitted, the control's natural action
is used (a value implies `set-value`). The planner rejects before touching the UI
what the element's facts rule out: an unknown action (listing the valid ones), an
action the element has no pattern for (listing what it supports), `set-value`
without a value or with a non-numeric or out-of-range value on a range control
(rejected, never clamped) or on a read-only text control, `select` with the wrong
value arity, `expand` or `collapse` on a leaf, and a value passed to an action
that takes none. Actions go through the peer's pattern providers
(`IInvokeProvider`, `IToggleProvider`, `IValueProvider`, `IRangeValueProvider`,
`ISelectionItemProvider`, `IExpandCollapseProvider`, `IScrollItemProvider`);
`focus` uses `Focus(FocusState.Programmatic)`.

`select` on a list or combo box takes the item text (exact, case-insensitive,
trimmed) or `#N` for a zero-based index; a bare number is text, because labels
such as "60" are common. The item texts it matches against (also listed in the
tree) are read in this order: a string as is, a `ContentControl` or `UIElement`
item by its content, then for a view-model object the selector's
`ItemsControl.DisplayMemberPath` (a simple or dotted path of public instance
properties with a public getter, read with the invariant culture; a blank path, a
missing or null segment, a throwing getter, an indexer or a value that is not
text-like such as a nested object resolves nothing and never throws), then a
realized container's visible text, and only then `ToString()`. The path comes
before the container text so an item has the same text whether or not its
dropdown is open; without it the resolution and frame-rate combos listed every
item as its type name and could not be selected by text. A value that matches no
item, or several (the error lists the indexes), is rejected before anything runs
as `invalid-value` (`invalid-request`, like an out-of-range slider value) with the
capped item list in the message.

`select` on an item container (a path from the open dropdown) takes no value and
selects that item through its owning selector. Whichever way the item was named,
the selection takes the first usable of three routes, and `Via` says which ran:
`peer:SelectionItem (item-data peer of item #N "text")` (the selector's own peer
lists one item-data peer per item, found by item and order, so duplicates stay
distinct); `peer:SelectionItem (container peer of item #N "text")` when the
realized container's own peer exposes the pattern; and `Selector.SelectedIndex
(item #N "text": <why no item-data peer>; <why no container peer>)`, the same
selection a click produces. The reasons are distinct and true: the peer lists no
item-data peer (or looking it up failed, with the error), and the container is not
realized, has no peer, or has a peer without SelectionItem; the first version
said "no realized container" while the dropdown was open and every container
existed. Observed live: a closed combo box lists no item-data peers and has no
containers (so a select on it reports `Selector.SelectedIndex` with both reasons),
and an open dropdown lists them (so a select, by text or by popup path, reports
the item-data peer and the dropdown closes; the response of a popup-path select
states that its element left the tree). The other actions' `Via` is
`peer:<Pattern>` (`peer:Invoke`, `peer:Toggle`, ...).

**Not supported: `accelerator`.** WinUI has no public API to raise a
`KeyboardAccelerator`, so the action is rejected with a specific message. The
tree lists each element's accelerators; the agent invokes the element that owns
one.

**Response.** The action is performed in one UI-thread dispatch; after a 100 ms
settle delay a second dispatch re-reads the element, and `Element` is its state
after the action (an element that left the tree, such as a flyout item whose
flyout closed, is reported as such). `invoked` means the peer accepted the call;
`force` skips only the pre-check, so a peer may still refuse (a disabled control
usually does), which is `action-failed` with the reason. Statuses: `invoked`,
`not-found`, `ambiguous`, `hidden`, `disabled`, `invalid-action`,
`action-unsupported`, `invalid-value` (a bad value, including a select that
matches no item or several), `action-failed`, `window-unavailable`. Every attempt
is logged as `AUTO-UI-INVOKE`.

CLI: `ssctl ui tree [--root NAME] [--depth N] [--all] [--json]` and
`ssctl ui invoke <target> [action] [value] [--force] [--json]`. MCP:
`get_ui_tree`, `invoke_ui_element`.

### 2.11 Hub observer cost

The diagnostics hub is the app's largest observer. Before this slice one refresh cost 22.4 ms (p50, mean 27.5 ms) and 324 KB, which is
603 KB/s of the process's 820 KB/s allocation (73 %) at steady preview (the hub figure is process-wide during the refresh, so an upper bound). The 500 ms interval, the alert rules and the snapshot wire
contract are unchanged; only what a refresh costs changed.

Where the cost was (a throwaway per-stage probe over 700 ticks; the patch is kept at
`artifacts/perf-overhaul/hub-cost/stage-probe.patch` and the raw table at `stage-probe-raw.json`):

| Stage | Wall per tick | Allocated per tick |
|---|---|---|
| Process resources (`Process.Refresh`, `WorkingSet64`, `PrivateMemorySize64`, `TotalProcessorTime`) | 22.3 ms | 14.2 KB |
| Preview runtime snapshot, requested three times (three UI dispatches, three full builds) | 1.4 ms wall, 3 x 0.33 ms on the UI thread | 225.7 KB |
| Health fragment | 0.69 ms | 23.1 KB |
| Capture runtime fragment | 0.12 ms | 17.9 KB |
| Alerts | 0.06 ms | 8.0 KB |
| Lane strings, projection, timeline, counters, the rest | 0.3 ms | about 16 KB |
| `IntervalCadenceStatistics.Compute` clone and sort (4.07 calls, inside the preview and health rows) | 0.17 ms | 37.4 KB |

The sorts are cheap (0.17 ms a tick); the clones and the process enumeration are what cost.

Three changes, each measured:

- `HubProcessCounters` reads working set, private bytes and kernel plus user time with `K32GetProcessMemoryInfo` and `GetProcessTimes`
  on the current-process pseudo-handle, instead of `Process.Refresh()`, which enumerates every process on the machine. The values
  are the same counters (`WorkingSetSize`, `PrivateUsage`, kernel plus user ticks); the hub's working set and private bytes now match
  the perf report's `ProcessSensors` reading to 0.1 MB in both after runs (421.9 and 550.7 MB in one of them). It removed the 22 ms and 14 KB.
- The preview fragment makes one provider call. The old overload shape called the provider as producer-epoch probe, value and
  producer-epoch probe, so every tick built the full preview snapshot three times and hopped to the UI thread three times to watch a
  window of about 2 ms. One UI work item is consistent by construction, like the view-model fragment, so the before and after
  epochs the old bracket compared are now equal by definition and `preview_runtime_producer_changed` cannot fire; the cross-fragment
  checks in `BuildSnapshotMixedEpochReasons` (for example `view_model_preview_runtime`) are unchanged. The provider has no
  epoch-only read; adding one would change the constructor wiring that two source-text tests pin, for a check that watched a window
  the single hop no longer has. It removed about 150 KB and two UI dispatches a tick.
- `IntervalCadenceStatistics.Compute` sorts in a pooled scratch instead of cloning the window. Callers still pass the array they
  also publish as `RecentIntervalsMs`, so the order must survive; it does (tested).

A/B, interleaved ABBA (before-a, after-a, after-b, before-b), the base build 9dcc8993e against this branch, three 30 s perf windows
each after 15 s of warm-up, steady preview at about 60 fps, same `ssctl`, every window followed by a graceful `window close`.
Mean of six windows per build, range in brackets:

| | Before | After |
|---|---|---|
| `telemetry.hub_refresh` p50 | 22.4 ms (20.7-24.8) | 1.8 ms (1.6-2.1) |
| `telemetry.hub_refresh` mean | 27.5 ms | 3.6 ms |
| `telemetry.hub_refresh` p99 | 80.8 ms (64-103) | 28.2 ms (16-54) |
| `telemetry.hub_refresh` max | 81.8 ms | 28.5 ms |
| `telemetry.hub_alloc` per refresh | 323.8 KB (320-328) | 133.2 KB (132-136) |
| Hub allocation rate | 603 KB/s | 262 KB/s |
| Process allocation rate (`Gc.AllocatedBytes`) | 820 KB/s | 443 KB/s |
| Process CPU, perf report | 47.7 % of a core (44.3-50.8) | 41.5 % (38.5-43.1) |
| `ui.dispatcher_latency` p99 | 19.6 ms | 18.8 ms |

`ui.dispatcher_latency` did not move: that is the UI thread's own stalls, and the refresh p99 that remains (16-54 ms) is the hub
waiting on a UI hop during one of them. GC counts are too small to compare (1 to 3 gen0 collections per window).

The machine was not idle. About 10 of 32 logical cores outside Sussudio were busy in every window (other applications; recorded per
window in `artifacts/perf-overhaul/hub-cost/raw/*-load.json`, with the build-tooling CPU sampled before each window and the window held
until it was under 10 % of a core). An earlier before run that overlapped another worktree's full validation is kept under
`raw/overlapped-with-other-validate/` and not used. The deterministic numbers (allocation) are robust to that; the timing numbers
carry the noise their ranges show.

What is left (133 KB a tick): the preview build is about 66 KB of it (derived from the probe: 75 KB a build minus the 9 KB sort clone), almost all ring copies in `D3D11PreviewRenderer.Get*Metrics`
(about nine copies of a 1,200-sample ring per snapshot, each 9.6 KB, plus the caller's own copy that `Compute` no longer clones). A
`Compute(ReadOnlySpan<double>)` overload fed from pooled scratch filled under the ring lock would remove most of it; it is the
renderer's file and the existing contract test resolves `Compute(double[], double)` by reflection, so it was left. Health (23 KB) and
capture runtime (18 KB) are the next two. Alert message strings (8 KB, about 0.06 ms) are built eagerly each tick; making them lazy
touches about thirty call sites of the alert path for 2.5 % of the allocation, so it was not done.

Demand gating (not done; recommendation only). Pausing the hub while no automation client is connected is not safe as an on/off
switch. The loop is also the app's own observer, not only a cache for clients: auto-verification of a finished recording runs from the
tick that sees `IsRecording` fall, and `LastVerification` then feeds `verify_recording`, the `VerificationReady` wait condition, the HDR
parity alert and the truth verdict; the alert, event and recent-counter baselines, the 2-minute timeline and the audio-mute and
recording-growth timers need continuous sampling; `wait_for_condition` and every control acknowledgement read the cached snapshot
(`GetLatestSnapshot`), which would be stale or empty after an idle gap (only `GetSnapshot`, `AssertSnapshot` and the
recording toggle call `RefreshSnapshotNowAsync`, and those refreshes neither append to the timeline nor move the interval
baselines, D15). Pipe clients are also one-shot connections, so "connected" is not a signal; the only available one is "a
request in the last N seconds". With the refresh now about 2 ms and 133 KB, the saving from a slower idle tick is small; live2 had
already shown a 2,000 ms poll against 500 ms at noise level in CPU. If gating is still wanted, gate on recent request or an active
recording and keep a floor tick, and decide that policy explicitly.

### 2.12 Perf experiment lease

`PerfExperimentLease` (`Sussudio/Services/Performance/PerfExperimentLease.cs`,
command 68) gives one perf experiment at a time ownership of the tuning knobs it
varies. Without it a second experiment, or a hand-typed `knob set`, could change
knobs under a running experiment, and a runner that died left its knobs set.
Every automation knob change goes through it; one lock covers the lease and is
taken before the knob store's.

- `acquire` takes the lease on a list of knobs with a client-chosen id (1 to 64
  characters, the holder's capability, never shown to other clients), a holder
  text (who runs it, 1 to 200 characters) and a time to live (10 to 600 s,
  default 60). While another experiment holds it, `acquire` is refused with
  `invalid-state` and the error and `Data.Active` name the holder. A retry with
  the same id, after a lost reply, renews the lease and keeps the first baseline.
  Malformed requests (a TTL out of range, an unknown knob, an empty id) are
  rejected with `invalid-request`, never clamped.
- The reply's `Baseline` is each leased knob's value and launch value when the
  lease began, read under the knob store's lock. The runner restores to it, not
  to its earlier preflight read, so a knob changed by hand between the two is
  restored to the value it had when the experiment took it.
- While the lease is held, `SetTuningKnob` on a leased knob without the lease
  id is refused (2.5). Knobs outside the lease are unaffected, and a lease id
  sent while no lease is active is ignored.
- `renew` moves the expiry. A holder that stops renewing (a crash, a hang, a
  killed tool) loses the lease at expiry: a timer restores every leased knob
  that is not already at its baseline (a reset when the baseline was the launch
  value, a set otherwise), logs `PERF_EXPERIMENT_LEASE_EXPIRED` and frees the
  lease. A timer callback that races a renew re-arms instead of ending it.
- `release` with the holder's id frees the lease and leaves the knobs alone,
  because the holder restores them itself. `release` with `force` ends anyone's
  lease and restores its knobs the same way (`PERF_EXPERIMENT_LEASE_FORCED`).
  A renew by an id that no longer holds the lease is refused with
  `invalid-state` and says whether the lease expired, was forced or was never
  held (an app restart forgets every lease). Releasing a lease that already
  ended succeeds; releasing another client's lease without `force` is refused.
- `status` shows the holder, knobs, acquired and expiry times, and how the last
  lease ended, with the knobs the app restored and any restore errors.

The runner (section 7) acquires after its read-only preflight and before the
first change, sends the id with every knob change, renews four times per TTL
(between warmup and measure steps and readiness polls), renews once more before
it ends each run's window, and releases after its restore. A refused renew stops
the experiment and the run in progress is not used: either the app restored the
knobs or another experiment owns them. In the restore the runner then reads the
lease status. When nobody holds it, the runner restores as usual and also
restarts the preview or Flashback for knobs the app put back without a restart.
When another experiment holds it, the runner cancels only its own window and
leaves the knobs, preview and recording to that experiment, and the restore is
reported as not verified with the reason.

Limits. The app restores knobs, not the rest of a dead runner's state: a
recording the runner started keeps running and its open window stays open until
someone ends or cancels them. A restored `preview-restart` or
`flashback-restart` knob takes effect at the next restart of that part; the app
does not restart the preview by itself. `ssctl perf lease` shows the lease and
`ssctl perf lease release --force` breaks a stuck one (MCP
`perf_experiment_lease`).

## 3. Automation commands

Appended after `SetFlashbackGpuDecode = 57`; manifest revision 2 to 3.

| Id | Name | Payload | ssctl | MCP |
|---|---|---|---|---|
| 58 | `GetPerfReport` | `{ window?, filter?, includeIdle? }` | `perf report` | `get_perf_report` |
| 59 | `PerfWindow` | `{ action: begin\|read\|end\|cancel\|list, name? }` | `perf window ...` | `perf_window` |
| 60 | `AddPerfMarker` | `{ text }` | `perf mark <text>` | `add_perf_marker` |
| 61 | `FrameTrace` | `{ action: start\|stop\|status\|dump, capacity?, outputPath? }` | `perf trace ...` | `frame_trace` |
| 62 | `GetTuningKnobs` | `{ filter? }` | `knobs` | `get_tuning_knobs` |
| 63 | `SetTuningKnob` | `{ name, value?, reset?, lease? }` | `knob set\|reset` | `set_tuning_knob` |
| 64 | `GetLogTail` | `{ maxEntries?, contains? }` | `log tail` | `get_log_tail` |
| 65 | `CaptureAudioOutput` | `{ seconds?, outputPath?, measureLatency? }` | `audio listen` | `listen_audio_output` |
| 66 | `GetUiTree` | `{ root?, maxDepth?, interactiveOnly? }` | `ui tree` | `get_ui_tree` |
| 67 | `InvokeUiElement` | `{ target, action?, value?, force? }` | `ui invoke` | `invoke_ui_element` |
| 68 | `PerfExperimentLease` | `{ action: status\|acquire\|renew\|release, lease?, holder?, knobs?, ttlSeconds?, force? }` | `perf lease [status\|release --force]` | `perf_experiment_lease` |

Tool-side only (no app command): `ssctl perf trace analyze <file>`,
`ssctl perf experiment <spec.json>`, MCP `analyze_frame_trace`,
`run_perf_experiment`.

Commands 65-67 are ready-independent, return `Data` without an automation
snapshot and use the extended response timeout. A failed `CaptureAudioOutput`
(`command-failed`) or `InvokeUiElement` (`invalid-request` for `not-found`,
`ambiguous`, `invalid-action`, `invalid-value` and `action-unsupported`;
`command-failed` for `hidden`, `disabled`, `action-failed` and
`window-unavailable`) still carries the full result in `Data`, because the
candidates, element state or analysis are how the agent recovers. The MCP tools
return that `Data` as structured content on failure too, unlike the query tools.
The ssctl `audio` and `ui` verbs and the three MCP tools are projections of these
commands only.

Command 68 (`PerfExperimentLease`, 2.12) was appended without another revision
bump. It is ready-independent and returns `Data` without a snapshot; a refused
request (`invalid-state` while another experiment holds the lease or the caller
no longer does, `invalid-request` for a malformed one) still carries `Data` with
the active holder and the last ended lease. ssctl and MCP expose only `status`
and the forced `release`; `acquire` and `renew` belong to the experiment runner.

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
           "DedicatedBytes": 412356608,
           "Adapters": [ { "Name": "<adapter>", "Luid": "0x00000000_0x0000F5A2", "Software": false,
                           "DedicatedVideoMemoryBytes": 0, "LocalBudgetBytes": 0, "LocalUsageBytes": 0,
                           "NonLocalBudgetBytes": 0, "NonLocalUsageBytes": 0 } ],
           "AdaptersError": null },
  "Dwm": { "RefreshHz": 120.0, "FramesComposedPerSec": 0.06, "RefreshCountPerSec": 0.2,
           "CompositionPath": "bypass", "Interpretation": "..." },
  "Display": { "Available": true, "Note": "...", "SampleSeconds": 29.9, "PresentsPerSec": 60.0,
               "VblankPerSec": 120.0, "PresentsPerVblank": 0.5 },
  "Capture": { "IngressClock": "qpc|relative|unknown", "IngressClockMinOffsetMs": null },
  "Sensors": { "Active": true, "Reason": "window:baseline", "Mode": "auto",
               "Details": [ "ui_probe: running ...", "gpu_engines: running ...", "gpu_timing: mode=auto wanted=yes state=active ...",
                            "vram: on demand at report time" ] },
  "Trace": { "Running": false, "Events": 0, "Capacity": 0 },
  "Knobs": [ { "Name": "SUSSUDIO_PREVIEW_JITTER_TARGET_DEPTH", "Value": "1", "Launch": null, "Apply": "preview-restart" } ],
  "Markers": [ { "TimestampUtc": "...", "Text": "variant B" } ]
}
```

Time metrics are reported in milliseconds; percent and byte metrics in their
own units. `filter` is a name prefix (case-insensitive). Metrics with zero
count are omitted unless `includeIdle` is true. `Gpu.Engines` is built from
the `gpu.*` percent metrics that have samples. `Gpu.DedicatedBytes` is this
process's dedicated GPU memory from the PDH sensor, null while that sensor is
stopped, pending or unavailable. `Gpu.Adapters` is read when the report is
built (one object per DXGI adapter: name, LUID, `Software`, dedicated memory,
and this process's local and non-local budget and usage in bytes);
`AdaptersError` says why it is empty when the read failed. The `Adapters`
element type changed from string (it was always empty) to an object.
`Sensors.Active` means demand exists, with `Reason` `knob:off`, `knob:on`,
`window:<names>`, `trace` or `idle`; `Mode` is the sensors knob in force; each
`Details` line is one sensor or `gpu_timing` state (2.8). `Dwm` values are
null when DWM timing is unavailable; `FramesComposedPerSec` counts
composition passes, not frames shown, and `CompositionPath` and
`Interpretation` read it against `Display.PresentsPerSec` (2.8.5). Prefer
`Display.VblankPerSec` to `Dwm.RefreshCountPerSec` as the display's refresh
rate. `Display` comes from the preview swap chain's DXGI frame statistics and
is `Available: false` with a `Note` when there is no baseline (a lifetime
report), no renderer or no new sample. `Threads` is empty until dedicated
threads call `HotThreads.Register`. `Capture.IngressClockMinOffsetMs` is the smallest
observed sample-time offset the ingress classification used.

The command-catalog payload shapes use `string` for `action` (the allowed
values are listed in the command description), because the catalog shape
grammar has no enumeration type. A rejected `SetTuningKnob` still returns
`Data` with `Failure` (`UnknownKnob`, `WrongType`, `OutOfRange`, ...) beside
the `invalid-request` error code, except `Leased` (a knob a running experiment
holds), which comes with `invalid-state`.

## 4. Invariants

- Recording sites are allocation-free and lock-free; a disabled trace or
  idle sensor costs one volatile read.
- Telemetry never changes pipeline behavior. Defaults of every existing knob
  are unchanged; new knobs default to current behavior.
- Wire names (command names, metric names, knob names, report fields) are
  appended, never renamed.
- Sensors that wake threads (UI probe, PDH) run only on demand, and an idle app has no sensor thread and no timer.
- GPU timestamp queries exist only while wanted, never on the WARP adapter, and the render thread never waits on them: a frame
  that cannot get a slot is skipped.
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
- Hitch cause (live window 3, written before the code): a late arrival that
  explains the interval is named over an automation command, UI stall or hub
  refresh that overlapped longer; a GC pause overlapping a late arrival is
  still named; an arrival gap covering little of the excess does not outrank
  an event covering all of it; with no arrival gap and no grown stage the
  overlapping event is named as before.
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

- Sensor demand and supervisor (slice 2b, written before the code): one answer for every combination of knob mode, open windows
  and trace; the GPU timing knob wins when `on` or `off` and follows the sensors rule in `auto`, including sensors `off`;
  unrecognised knob text means `auto`; the report reason names a window in preference to the trace; the cached demand check
  re-evaluates at most once per interval and keeps its last answer when the probe throws; the supervisor starts sensors on
  demand, stops them when it ends, tolerates a sensor that throws on start or stop (the others still run and the error is
  reported), stops everything on shutdown, and survives concurrent re-evaluation; `Changed` fires on a real change of the window
  set, the trace and the knob store, outside the owner's lock, and a throwing or re-entrant handler cannot fail or deadlock the
  change; both knobs are `auto|on|off` live choices in the real catalog.
- UI probe (slice 2b): at most one probe outstanding, so a stalled dispatcher gets no backlog; a rejected or throwing enqueue is
  counted and the next tick retries; the stall counter and the latency histogram agree on the 50 ms threshold; a clock that moves
  backwards records zero; `Stop` is prompt, harmless when repeated or never started, and the probe restarts.
- GPU engine sensor (slice 2b): an instance of another pid that shares a numeric prefix is not counted; instances sum per engine
  type; NaN, infinite and negative values are ignored; a dedicated figure beyond the `long` range saturates; a pending read and a
  tick with no instance for this process record nothing (never 0 %), while a genuine zero is recorded; unavailable keeps its
  reason and records nothing; counters that become unavailable or throw are reopened with every abandoned reader disposed; open
  failures retry with backoff and a throwing factory is a state, not an exception; `Stop` during a backoff is prompt; `Start` and
  `Stop` are idempotent and a stopped sensor forgets its dedicated figure.
- GPU timestamp ring (slice 2b): every slot still pending skips the frame without waiting or opening a query; disjoint and
  implausible results are skipped and counted; a result that never arrives is abandoned after about a second; a backend that
  refuses, fails or throws at begin, end or poll fails the ring and is never called again; a span left open by an early close
  never dangles, and a second span cannot open over the first; results come back oldest first across many wrap-arounds; slot
  counts are clamped to the cap and to at least one.
- DWM and display (slice 2b): 0.057 composed per second against 60 presents per second on a 120 Hz display classifies as bypass,
  not dropped frames; the boundaries between `bypass`, `mixed` and `composed` are stable; a preview that presented nothing is
  `idle` whatever DWM composed for other windows; a missing reading is `unknown`, never zero; an unknown refresh rate still
  classifies; the renderer-to-report hook survives a provider that throws, a second renderer replacing the first, and an old
  renderer unregistering late.
- Timeline envelope (slice 2b): not an isolated test. The cause was a response-shape default (the envelope embedded the cached
  snapshot), not logic, so it is checked live: `timeline --max N` returns exactly N entries and no envelope snapshot.
- Ears (slice 4, written before the code): digital silence reads `silent` with
  floor levels and never as healthy audio or an error; an empty capture says
  `empty`; a clean tone has no findings; clipping sample and event counts
  agree; a single-sample click is found once at the right time; a short
  exact-zero run inside audio is one dropout and not two clicks; leading,
  trailing and between-sound silence is not a dropout; bright tones, white noise
  and broadband level steps are not clicks, an isolated spike and a DC step are;
  a dropout inside noise is found; one silent channel is an imbalance finding;
  NaN and infinity are counted and neutralised; packet flags and timestamp gaps
  are counted; the silent-block percentage follows the 10 ms blocks; the WAV is
  a valid IEEE-float file that round-trips exactly.
- Latency (slice 4): a delayed, gain-changed copy is measured within 1 ms with
  the right gain; delays near both ends of the window are measured and a delay
  outside it is refused, never reported as another lag; silent input and silent
  output are refused with their own reasons; unrelated audio is refused as low
  confidence; steady or exactly repeating audio is refused as ambiguous; missing
  packets or timestamps are `unavailable`; delivery jitter does not move the
  result; a gap in the loopback stream does not break alignment around it; a
  mismatched waveform (inverted polarity) falls back to `envelope-1ms` and says
  so.
- Tap ring (slice 4): an unwritten ring snapshots to nothing; packets round-trip
  exactly with their start times; a ring written past capacity returns only the
  newest whole packets, contiguous and uncorrupted; a write that laps the ring
  during a snapshot costs it the overwritten prefix and never returns torn
  samples (a test seam makes the race deterministic); a packet table smaller
  than the sample ring still yields a gapless run; a packet larger than the ring
  is rejected and counted; a concurrent writer and snapshotting reader never see
  a corrupted sample; the process-wide tap is a no-op while disarmed, arms
  exactly once, re-arms after disarm, and only the arming ring may disarm it.
- Fingers, target resolution (slice 4): an empty target is rejected before any
  search; no match lists capped near-miss suggestions and never picks a close
  one; precedence is path, `x:Name`, `AutomationId`, automation name, text, and
  a lower level is never consulted after a higher one matched (even unusably);
  `x:Name` and `AutomationId` are exact and case-sensitive, automation name and
  text trimmed and case-insensitive, text never a substring; two usable matches
  are ambiguous with capped candidates whose paths each resolve to exactly that
  element; a hidden twin never blocks the visible element and a label inside a
  button never makes the button ambiguous; a single hidden or disabled element
  is rejected with its reason (both reasons when both), `force` overrides the
  pre-check and says what it overrode, and `force` never picks among ambiguous
  elements.
- Fingers, actions and selection (slice 4): an unknown action lists the valid
  ones and `accelerator` says plainly it is unsupported; an omitted action
  resolves to the natural one and a control with none says so; an action with no
  pattern lists what the element supports; `set-value` needs a value, a range
  value must be finite and inside its limits (rejected, never clamped), a
  read-only text control is rejected; `select` takes no value on an item and
  needs one on a container; `expand` and `collapse` on a leaf are rejected;
  a value on an action that takes none is rejected and an empty value counts as
  absent; `focus` and `scroll-into-view` need no pattern. Item selection matches
  exact text or `#N` and treats a bare number as text; duplicate text is
  ambiguous and lists indexes; no match lists capped items and is
  `invalid-value`; an out-of-range index and an empty list are errors.
- Fingers, item text and selection routes (live window 3 follow-up, written
  before the code): `DisplayMemberPath` with no path, a blank path or no item
  resolves nothing and never throws; a simple property is read by exact name,
  including a computed one; a dotted path walks nested properties and an empty
  segment resolves nothing; a missing property, a null at any segment and a
  throwing getter resolve nothing instead of failing the tree; private, internal,
  static, field and non-public-getter members are not read; an indexer is never
  read and a member hiding an inherited one of another type resolves to the
  hiding member instead of throwing an ambiguous-match error; non-string values
  format invariantly (59.94 reads "59.94" under a comma culture); a nested object
  is not printed as its type name; an empty string is a resolved empty text. The
  `Via` text of the fallback never claims "no realized container" for a container
  that exists, gives distinct texts for a missing container, a container without
  a peer and a peer without SelectionItem, carries a failed lookup's error and
  says plainly when a lookup found nothing, names the item index and text in
  every route, and says which peer selected.
- Fingers, visibility (slice 4): a collapsed element or ancestor is hidden and
  the reason names the root-most culprit; opacity at or below 0.01 on the element
  or any ancestor is hidden; a zero-sized element is hidden and a zero-sized
  ancestor alone is not; dim but visible chains stay visible with no reason.
- Wire to tools (slice 4): the real DTOs, serialized as the pipe server does,
  render every field the ssctl formatter reads (a renamed field would otherwise
  print "-" silently); failed responses still print candidates, element state,
  analysis and failure stage; a response without data prints the message and error
  code; null optional blocks do not crash; a truncated tree says so; nesting is
  kept. The MCP factory keeps a failed call's object payload as structured content
  and still marks the call an error; a failure without an object payload is a
  plain error; success is unchanged; the result owns its data after the response
  document is disposed.

- Hub observer cost (written before the code): the pooled sort scratch is longer than the window and holds an earlier renter's
  values, so a window must use its own length (a 1,024-sample window of 1000 ms followed by a 600-sample window of 1 ms in the same
  size class reports 1 ms, not 1000); window sizes at and around the pool's size classes (1, 15, 16, 17, 1,023 to 1,025) match a
  sort of a private copy; a wrapped ring window (full, write index mid-ring, from `RingBufferHelpers.Copy`) keeps its chronological
  order and matches a sort of a copy; eight concurrent callers on different value bands never see each other's window; an empty window
  is still rejected before a buffer is rented. A concurrent writer to the live ring is not a `Compute` failure mode, because
  `Compute` only sees the private copy made under the owner's lock. `HubProcessCounters` reports the same working set, private
  bytes and CPU time as the `Process` properties it replaced, in the same process (a unit slip or the wrong struct member would be
  silent in a live snapshot).
- Hub snapshot contract (not an isolated test): the 807 snapshot keys, their order and their value kinds are identical before and
  after, compared live (`artifacts/perf-overhaul/hub-cost/contract-compare.json`).

- Hardening pass (listed before the code): in the ingress ledger four frames that share a set all survive, the fifth evicts the smallest arrival
  tick (not the least recently written) and never leaks its sample, rewriting a key replaces it in place, and a reader racing the
  writer sees a miss or the matching pair; a ring read in chunks of any size returns the same events as one big chunk across a wrap;
  a slot lapped mid-read is skipped without shortening its chunk or ending the read; a writer that was preempted across a full
  ring wrap drops its own event, counts it as `abandoned` and never leaves a torn slot a reader accepts; a dump streams through a
  fixed chunk with the origin and the thread set from its first pass, and a ring is reused only for the same capacity and never
  while a dump reads it; a retired registration removes its thread label only if the id still maps to it, and the dump still names
  a thread that ended during the traced interval; a window `end` builds its report before releasing demand, a throwing builder
  leaves the window open, two concurrent ends give exactly one report, a window restarted during the build is not closed; a
  zero-tick GPU span is counted as unmeasured and never recorded as a 0 us render, a disjoint zero-tick span stays skipped, a real
  span under 1 us is recorded as 1 us, and counts are kept per render path; a priority maps to a name or to `mmcss/<n>` and
  `raw/<n>`, never a bare number; a stage that ended at frame arrival is listed as before arrival and never named dominant, a
  stage straddling arrival counts only its part after arrival, a stage that ended after the present is listed after present and never
  named dominant while one that finished before it still counts, and a trace without QPC metadata keeps the read inside the interval;
  a set or reset that changes nothing is not logged. Not isolated-tested: that each drop call site's literal reason code matches its
  reason string (the string-to-code table is tested; a test of the call sites would assert on source text), which was checked by
  reading every pair.

- Experiment lease, app side (written before the code): two experiments starting together produce exactly one holder and the
  other is refused with the holder named and nothing changed; a same-id retry after a lost reply keeps the first baseline; a
  hand change to a leased knob is refused and names the holder while the holder's own changes and unleased knobs still go
  through; a holder that stops renewing loses the lease at expiry, the app restores each leased knob (reset or set to its
  baseline, nothing written for a knob already there) and the lease is free; a renew after expiry, after a forced release or
  with an id that never held it is refused with the reason; the holder's release leaves knobs alone and is harmless twice, another
  client's release is refused unless forced, and a forced release restores the knobs; an expiry timer that fires just after a
  renew does not end the lease; a TTL out of range, an unknown knob, or an empty, over-long or control-character id or holder is
  rejected, never truncated, and takes no lease.
- Experiment lease, runner side (written before the runner used it): a foreign lease rejects the run before any change and
  sends no release; a lost acquire reply is released under the runner's own id; a lease lost mid-run fails the run, drops the
  window that was open and still restores (restarting the preview the app's restore left stale), while a lease taken over by
  another experiment leaves that experiment's knobs and preview alone; waits longer than the TTL renew in time; every knob
  change and restore carries the lease id; the release comes after the restore on a fresh token whether the run completed,
  failed or was canceled, and a failed release is reported; baselines come from the acquire reply, not the preflight read.

Everything else is verified end to end against the live app (section 6).

## 6. Validation

`scripts\validate.ps1` plus live runs whose artifacts live under
`artifacts/perf-overhaul/` and can be re-run: a perf window over steady
preview, a frame trace dumped and analyzed, a knob A/B experiment, an audio
listen, and a UI tree read and invoke. Two scripts make the command surface
repeatable against any build: `scripts/performance/Verify-PerfTelemetry.ps1`
(report, window, marker, trace, knobs, the cadence timeline, the experiment
lease's refusal, forced release and expiry, log tail; 46 checks) and
`scripts/performance/Verify-AgentSenses.ps1` (audio listen, UI tree and
invoke; 24 checks). Each writes `verify-result.json` and exits non-zero on
any failed check. With a silent capture source the audio checks still pass
on the `no-audio-rendered` path; `observations.listenStatus` says which path
ran, and a `captured` run with a measured latency needs audible content.

## 7. Agent playbook

Measure an interval:

```powershell
ssctl perf window begin steady
Start-Sleep 30
ssctl perf window end steady --json > artifacts/steady.json
```

Explain a hitch: `ssctl perf trace start`, reproduce, `ssctl perf trace dump
artifacts/hitch.trace.json`, then analyze the file:

```powershell
ssctl perf trace analyze artifacts/hitch.trace.json [--top N] [--json]
```

`perf trace analyze` is tool-only: it reads the dump file on this machine and
needs no running app (MCP: `analyze_frame_trace` with `path`, optional
`topFrames`). `--top` (1 to 1000, default 10) sets how many of the slowest
frames are listed with their per-stage breakdown. The report has:

- per-stage durations (p50, p99, max, and where the stage ended relative to
  frame arrival; aggregate spans such as `preview.pipeline` are marked and
  never named as a cause);
- derived latencies: `arrival_to_present`, `ingress_to_visible` (MF sample
  time from `capture.sample_time` to the end of the pipeline or present) and
  `ingress_to_arrival`; if no frame has a `capture.sample_time` the report
  says so instead of omitting them silently;
- present cadence with every hitch (an interval over 1.5 times the median and
  at least 2 ms over it), each with a named likely cause: the stage that grew
  most on that frame, a late arrival, or an overlapping event (a GC pause, UI
  stall, automation command or Flashback segment rotation, or any other
  non-audio event of 2 ms or more such as a `telemetry.hub_refresh`), plus any
  marker in the interval. An event's figure is its overlap with the interval
  while a stage's or arrival's is its excess over the median, so they do not
  rank directly: a late arrival or grown stage that covers at least 75 % of
  the interval's excess is named ahead of an overlapping event on another
  thread, which stays listed as context. A GC pause keeps its place by overlap,
  because it stops the capture loop too (live window 3 had a 27 ms interval
  after a 26.5 ms source gap blamed on a 44 ms automation command);
- the slowest frames, events outside frames (`id = -1`), and drops counted per
  reason (`args.reason` of `preview.drop`, the names in `PreviewDropReasons`;
  a dropped slow frame names its reason, and a dump without reasons reports
  the count alone). For an `arrival_to_present` slow frame the dominant stage
  is the longest part of a stage that lies after the frame arrived: a stage
  that ended at arrival (`capture.read`, which is the wait for this frame to
  arrive, so it always came first and always looked longest) is listed on its
  own `before arrival (not in the total)` line and never named; a stage that
  straddles arrival counts only the part after it; for a presented frame a
  stage that ended after the present (the recording and Flashback encoders run
  on their own thread, beside the present) is listed on an `after present (not
  in the total)` line and never named; aggregate spans such as
  `preview.pipeline` never win. Without the dump's QPC metadata arrival cannot
  be placed, so every stage counts as inside the interval.

Arrival comes from the QPC `id` and the dump's metadata. A dump with no
metadata, or ids that are not QPC ticks (sequence numbers from an older
build), falls back to each frame's earliest event and says so in a warning.
Input that is not a complete trace (empty, not JSON, truncated, a missing
file) is exit code 3 with the reason; `--json` prints the same failure as
`{"Success":false,"Message":...}` on stdout. Open the same file in
https://ui.perfetto.dev to look.

A/B a knob without relaunching: write a spec and run it.

```powershell
ssctl perf experiment scripts/performance/experiments/example-jitter-depth.json [--out DIR] [--json]
```

`perf experiment` is also tool-only (MCP: `run_perf_experiment` with `specPath`
or inline `specJson`, optional `outputDirectory`; it can take minutes and the
MCP server runs one experiment at a time). It is a client of existing commands
(`GetSnapshot`, `GetTuningKnobs`, `SetTuningKnob`, `SetPreviewEnabled`,
`SetRecordingEnabled`, `RestartFlashback`, `PerfWindow`, `AddPerfMarker`) and of
the experiment lease (2.12): a second experiment from any client is rejected
before it changes anything, with the holder named, and hand changes to the
running experiment's knobs are refused. If a runner dies, its knobs come back
when its lease expires (60 s); `ssctl perf lease release --force` frees a stuck
lease at once. Progress goes to stderr, the summary (or, with
`--json`, `result.json`) to stdout. Exit code 0 means every run completed and
the restore was verified; anything else, including a spec the app rejects and
a restore that could not be verified, is exit code 3.

Spec (JSON; comments and trailing commas are accepted, unknown fields are
rejected so a misspelled `repeat` cannot run with the default):

```json
{
  "name": "jitter depth",
  "description": "optional",
  "warmupSeconds": 5,
  "measureSeconds": 20,
  "repeats": 3,
  "recording": false,
  "metrics": ["preview.present_interval", "preview.jitter_drop."],
  "verdictStatistic": "p99",
  "variants": [
    { "name": "baseline", "knobs": {} },
    { "name": "deeper", "knobs": { "SUSSUDIO_PREVIEW_JITTER_TARGET_DEPTH": 5 } }
  ]
}
```

`name` and `variants` are required. `warmupSeconds` is 0 to 600 (default 5) and
runs after every restart, before the measure window; `measureSeconds` is 1 to
3600 (default 20); `repeats` is 1 to 20 runs per variant (default 3).
`recording: true` records during every run (needed for `recording-start`
knobs). `metrics` is an optional focus list of exact names or prefixes ending in
`*` or `.`; without it every metric with samples is compared.
`verdictStatistic` is `mean`, `p50`, `p90`, `p99` (default), `p999` or `max`.
There are 1 to 8 variants; the first is the baseline every other variant is
compared with, and a knob value may be a string, number or boolean.

The runner first validates the spec against the app without changing anything
and reports every problem together (`rejected`): an unknown knob, a wrong type,
a value outside the range, an `app-restart` knob (the runner cannot relaunch the
app), a `recording-start` knob without `recording: true`, a `flashback-restart`
knob while Flashback is off, a preview that is not running, or a recording that
is already running. Then it runs the variants interleaved, reversing the
direction each repeat (A B, B A, A B) so slow drift lands on both. Each variant
is applied as a difference from the previous run, knobs a variant does not name
return to their pre-experiment value, and the preview (and Flashback, where a
`flashback-restart` knob needs it) is restarted and read back from the snapshot
before warmup, not trusted from the acknowledgement. Every run is one named
performance window (at most 64 characters, unique), and a marker (`experiment
variant <name> run <n> warmup` and `... measure`) is added at the start of the
warmup and of the measure window so the runs can be found in a frame trace.

A verdict is `better` or `worse` only when the per-repeat range of the verdict
statistic for the variant is entirely below or above the baseline's (lower is
better); touching or overlapping ranges, a single repeat, or a metric with no
samples in one variant are `inconclusive`. A counter is judged only when its
name says it counts something bad (drops, stalls, late frames, errors, ...);
any other counter reports `changed` for a separated difference instead of
better or worse.

Whatever happens, including a failed command or Ctrl-C, the runner then restores
the app on its own timeout: every touched knob goes back (reset to its launch
value if it was not overridden before, set back to its previous value if it
was), a recording it started is stopped, an open window is canceled, the
preview is running again, and the state is read back. `result.json` says whether the restore was verified
and lists anything that was not. Artifacts go to `--out DIR` or
`artifacts/perf-experiments/<timestamp>-<name>`: `result.json` (settings, runs,
per-metric statistics and verdicts, restore, warnings), `summary.md`,
`spec.json` (the variants as run) and `reports/NN-variant-rN.json`, the raw
window report of each run. A failure to write them is a warning and never
changes the status or skips the restore.

Hear the app: `ssctl audio listen --seconds 5 --latency --wav
artifacts/out.wav`.

Use the UI like a person: `ssctl ui tree`, then `ssctl ui invoke <name>
[toggle|set-value <v>|select <item>]`, then `ssctl screenshot` to look.

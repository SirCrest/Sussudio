# Real-Time Capture Engine Rewrite Plan

## Why This Exists

This project is still an experiment. We are not preserving legacy architecture for its own sake,
and we are not optimizing for cautious compatibility. The goal is to build a serious 4K120
capture, preview, recording, and diagnostics system that can prove what happened at every stage.

The app should stop being a UI application with a capture pipeline attached. It should become a
real-time media engine with a UI, CLI, and MCP layer on top.

Current rollback checkpoint before this rewrite scope:

```text
8961079 Checkpoint experimental capture pipeline
```

Before starting a major phase, confirm the worktree is clean or intentionally checkpointed.

## Product Goal

Build an app that is excellent at three jobs:

- Reliable 4K120 recording.
- Low-latency, smooth 4K120 preview.
- Forensic frame pacing diagnostics from capture card to display.

The killer feature is explainable smoothness. The app should not merely say "120 fps"; it should
say which exact stage owned a stutter.

## Core Principles

- Recording is sovereign.
- Preview is important, but preview may adapt, buffer, or drop stale preview-only work before
  recording correctness is threatened.
- Every delivered source frame must have identity.
- Every important pipeline transition must be measurable.
- Silent frame loss is unacceptable.
- Diagnostics must be truthful, layer-specific, and cheap enough to run during real captures.
- The UI should observe engine state, not own pipeline truth.
- Prefer fewer, sharper abstractions over many partial service wrappers.
- Delete weak fallback paths, zombie metrics, and duplicated timing calculations when the new
  engine supersedes them.

## Engineering Posture

"Carmack mode" is a useful shorthand for the taste profile of this rewrite, not a license for
theater or premature micro-optimization.

It means:

- Work from first principles.
- Measure before guessing.
- Keep hot paths simple and legible.
- Treat latency, memory bandwidth, queueing, and clock domains as real design constraints.
- Prefer hard evidence over vibes.
- Delete weak abstractions instead of decorating them.
- Make correctness observable before chasing cleverness.
- Optimize the system as a whole, not just isolated functions.

The goal is not heroic complexity. The goal is the simplest engine that can be measured, understood,
and pushed hard at real 4K120 workloads.

## Non-Goals

- Backward compatibility with old internal service shapes.
- Preserving diagnostics fields that no longer answer a real question.
- Supporting multiple competing preview pipelines indefinitely.
- Hiding overload by silently skipping recording frames or inventing continuity.
- Designing around low-end hardware first.

## What We Know Right Now

Recent 4K120 measurements suggest the early pipeline is mostly healthy:

- Capture cadence has been near 120 fps with no estimated source drops in fresh runs.
- CPU MJPEG decode is expensive per frame but keeps up with six decoders in parallel.
- Recording/Flashback paths need stronger proof, but recent verification was encouraging.
- Preview renderer CPU work is usually well under the frame budget.
- Exact PresentMon swap-chain targeting now works.
- Remaining visible jitter appears more suspicious around final present/compositor/display timing
  or source cadence than around capture/decode overload.

Known source/display wrinkle:

- PS5 may output around 119.88 Hz while the display/card path is effectively 120.00 Hz.
- That can create occasional cadence correction, but it should not explain constant heavy judder.

## Relationship To Existing Plans

This document is the umbrella plan.

The detailed MJPEG work remains in:

```text
docs/mjpeg-pipeline-rewrite-plan.md
```

Use that document for decode queue, pooled frame, reorder, and preview jitter buffer details. This
document owns the broader engine shape, recording correctness model, diagnostics architecture, and
implementation sequencing.

## Target Architecture

```text
Device / Capture Core
  -> Frame Identity
  -> Compressed Ingress Ring
  -> Decode Ring
  -> Ordered Decoded Frame Store
  -> Recording Pipeline
  -> Flashback Pipeline
  -> Preview Scheduler
  -> D3D Renderer
  -> PresentMon / Display Correlator

Frame Ledger / Telemetry Core <= events from every stage above
Control Plane / Snapshot API <= engine state + ledger summaries
UI / CLI / MCP Diagnostics <= Control Plane / Snapshot API
```

The UI, CLI, and MCP server should all read from the same control and diagnostics plane. They
should not each reconstruct truth differently.

## Frame Identity

Every frame delivered by the capture stack gets a stable identity immediately.

Minimum fields:

- `SourceSequence`
- `CaptureArrivalQpc`
- `DeviceTimestamp`, when available
- `InputFormat`
- `Width`
- `Height`
- `FrameRateNominal`
- `CompressedByteLength`

Frame identity belongs at ingress, not after decode or preview.

Delivery identity and visual uniqueness are different concepts.

- Delivery identity is authoritative: the app received source sequence N at time T.
- Compressed fingerprints are diagnostic: useful for spotting repeated or changed MJPEG payloads,
  but not authoritative because payload bytes may vary for non-visual reasons.
- Visual fingerprints are diagnostic: useful when the sampled content is intentionally moving, but
  not authoritative in static menus, flat sky, fades, or noisy content.
- Fingerprint collection should be budgeted and labeled by sample region, resolution, and cost.

## Frame Ledger

The frame ledger is the center of the rewrite.

For each frame, record stage events:

```text
capture_arrived
compressed_queued
decode_started
decode_finished
strict_order_released
recording_enqueued
encoder_submitted
encoder_accepted
encoder_packet_written
flashback_enqueued
preview_enqueued
preview_selected
gpu_upload_started
gpu_upload_finished
render_submitted
present_called
presentmon_present_seen
presentmon_displayed_or_superseded
```

Each event should include:

- Frame sequence.
- QPC timestamp.
- Thread or subsystem name when useful.
- Stage-specific counters, such as queue depth or byte depth.
- Failure or skip reason when applicable.

The ledger does not need to retain infinite history. Keep a bounded recent window in memory and
optionally allow explicit diagnostic captures to write a session trace.

## Clock Domains

Treat clocks as explicit, named domains. Do not casually compare timestamps from different clocks
without recording the conversion or limitation.

Important domains:

- Capture arrival QPC.
- Device/media timestamp, when available.
- Decode worker QPC.
- Recording PTS/timebase.
- Audio capture clock.
- Preview scheduler clock.
- D3D present-call QPC.
- PresentMon timestamps.
- Display refresh cadence.

Every diagnostic value should make clear which clock it came from. The app should preserve source
cadence truth while separately adapting preview to the display cadence. In particular, 119.88 Hz
source cadence into a 120.00 Hz display must be modeled as a cadence relationship, not smeared into
generic jitter.

## Recording Contract

Recording must preserve every delivered source frame in normal operation on suitable hardware.

Rules:

- Recording never drops a frame merely because it is late.
- Recording never silently evicts old frames to make room for new frames.
- Recording receives strict ordered frames.
- If recording cannot keep up, the app enters an explicit recording overload/failure state.
- Preview and expensive diagnostics degrade before recording does.
- Encoder, muxer, and disk backpressure must be visible as health states and counters.

Recording accounting must be able to answer:

- Frames captured.
- Frames decoded.
- Frames submitted to recording.
- Frames accepted by encoder.
- Packets written.
- Sequence gaps.
- Encoder queue depth and max depth.
- Oldest queued recording frame age.
- Total backpressure time.
- Disk/muxer stall count and max stall.
- Final verification result after stop.

After every recording stop, the app should be able to produce an integrity summary:

```text
Recording integrity: clean/degraded/failed
Captured frames: N
Recording accepted frames: N
Encoded frames: N
Missing source sequences: 0
Max encoder queue depth: X
Max mux/disk stall: Y ms
A/V drift: Z ms
```

Recording PTS policy must be explicit:

- Frame sequence continuity is source truth.
- Encoder PTS continuity is recording timeline truth.
- CFR output cadence is based on the negotiated recording FPS and recording frame index.
- If source frames are repeated, encode them as real repeated frames unless a recording mode
  deliberately chooses otherwise.
- Source frame omission is a degraded/failure event, not a normal CFR correction.
- Synthetic CFR duplicate/drop events are output-cadence decisions and must be counted separately
  from delivered source frames.
- If frames are missing because capture/decode failed, record the failure, source sequence gap, and
  resulting PTS decision in the ledger and integrity report.

This tightens the existing CFR policy in `docs/cfr_policy.md`: CFR timing may correct output
cadence, but it must not hide lost source-frame delivery.

## Audio / A/V Sync Contract

Audio is part of recording correctness, not an accessory.

Rules:

- Track audio capture clock and video frame clock separately.
- Record audio start/stop timestamps, discontinuities, buffer depth, and glitches.
- Expose A/V drift during recording and in post-stop verification.
- Preview audio monitoring may degrade independently, but recording audio must follow the same
  explicit failure model as video.
- A recording cannot be called clean if video is perfect but audio drift, discontinuity, or missing
  samples exceed configured limits.
- Track resampling/stitching corrections separately from source audio discontinuities.
- Post-stop verification should compare muxed audio/video duration parity and runtime drift
  counters.

## Flashback Contract

Flashback has multiple roles, and each role needs different priority.

- Normal retention mode may evict old frames by retention policy. Those evictions are expected but
  must be counted separately from overload loss.
- Flashback-as-recording-backend inherits recording sovereignty for the active recording span.
- Flashback export is background work and must not steal budget from active recording.
- Flashback encoder pressure must be visible as queue depth, oldest age, segment continuity, and
  drop/eviction reason.

## Preview Contract

Preview should be smooth and live, but it is not allowed to endanger recording.

Rules:

- Preview has its own scheduler and latency target.
- Preview may skip missing or stale frames for preview only.
- Preview must record which source frame was selected for each render/present.
- Preview should adapt buffer depth based on measured underflow, skipped frames, and present
  jitter.
- Preview should prefer gradual timing correction over bursty catch-up.

The target behavior is not "render as soon as a frame exists." The target is "select the correct
frame for the next display deadline."

## D3D Renderer Contract

The renderer should become a dedicated timing-aware subsystem.

Responsibilities:

- Own the D3D device context and swap chain lifecycle.
- Expose the exact preview swap-chain address.
- Accept frame leases rather than forcing repeated NV12 copies.
- Track CPU timing for upload, draw/submit, present call, and total frame work.
- Record render/present events into the frame ledger.
- Provide enough identifiers for PresentMon correlation.

Longer-term target:

```text
PreviewScheduler selects source frame N for preview present P.
D3DRenderer uploads/submits frame N.
PresentMon sees swap chain S present P.
Display correlator marks present P displayed, delayed, or superseded.
```

## PresentMon / Display Correlation

PresentMon is useful only when tied to the actual preview swap chain.

Rules:

- Prefer exact swap-chain address matching.
- Do not silently use unrelated swap chains when exact targeting was requested.
- Track raw rows, selected rows, excluded rows, selected swap-chain address, and whether the
  expected address matched.
- Correlate app present events with PresentMon rows as closely as possible.
- Define correlation tolerance by clock quality. If app and PresentMon timestamps can be aligned,
  target <= 2 ms mismatch at 120 Hz. If they cannot be aligned, correlate by ordered nearest rows
  and mark the result lower confidence.
- Validate correlation with a synthetic cadence/pattern run before treating it as ground truth.

Open issue:

- Some PresentMon captures do not include `MsBetweenDisplayChange`. When absent, use displayed
  time, not-displayed count, present mode, and exact swap-chain timing, but label the limitation.

## Diagnostics UI Rewrite

Replace the sprawling diagnostics list with layered diagnostics.

Top summary should show values a serious user cares about:

- Capture resolution.
- Preview resolution.
- Source cadence.
- Visual/source uniqueness cadence.
- Decode health.
- Recording health.
- Preview renderer cadence.
- Present/display cadence.
- Current total latency.

The main graph should be a multi-lane frame timeline, not a single ambiguous FPS graph:

```text
Capture arrival
Decode release
Recording enqueue
Preview select
Render submit
Present
Display / superseded
```

Advanced sections:

- Capture.
- MJPEG ingress/decode/reorder.
- Recording encoder/mux/disk.
- Flashback.
- Preview buffer/scheduler.
- GPU upload/render.
- PresentMon/display.
- Audio/video sync.
- System pressure: CPU, GPU, memory, disk.
- Automation/MCP health.

Delete or demote:

- Outdated "score" concepts.
- Counters that always show zero and are not wired to real state.
- Duplicate FPS numbers without clear layer labels.
- Binary "good/bad" fields where actual values are more useful.

Use color to interpret values, but always show the value.

## CLI / MCP Contract

The CLI and MCP tools should be first-class control and diagnostics surfaces.

Rules:

- Use the same snapshot contracts as the UI.
- Add commands for frame-ledger capture and recent frame timeline export.
- Keep PresentMon capture available from CLI/MCP.
- Make app screenshots and window state available for sanity checks.
- Every long-running operation should return enough state to know whether it finished, timed out,
  or is still running.

## Hot Path Engineering Rules

- Avoid per-frame allocations.
- Avoid unnecessary 4K frame copies.
- Prefer pooled buffers and leases.
- Keep capture callbacks short.
- Do not block capture on UI, preview, diagnostics, or disk where avoidable.
- Keep locks small and observable.
- Use bounded queues with explicit health states.
- Make every drop/skip/failure counted with a reason.
- Use monotonic timestamps for timings.

## Budgets And Failure Taxonomy

Every bounded queue or pool needs an explicit budget and an explicit failure mode.

Budget types:

- Frame count.
- Byte count.
- Oldest item age.
- Wait/backpressure duration.
- CPU time per stage.
- GPU time per stage.
- Disk/muxer stall duration.

Failure categories:

- `source_missing`: capture card/source did not deliver an expected frame.
- `capture_error`: device/API failure before a frame entered the app.
- `decode_error`: compressed frame could not be decoded.
- `decode_overload`: decode could not keep up within configured budgets.
- `recording_backpressure`: encoder/muxer/disk could not accept work fast enough.
- `recording_failure`: recording could not preserve the delivered stream.
- `preview_skip`: preview intentionally skipped a frame to stay live.
- `present_superseded`: DWM/compositor/display did not show a submitted present.
- `diagnostic_unavailable`: a measurement source was absent or incomplete.

The app should prefer "degraded with reason" over "looks fine but lied."

Initial candidate memory budgets for 4K120 NV12, to be tuned from measurements:

| Area | Candidate Budget | Failure Behavior |
| --- | ---: | --- |
| Compressed MJPEG ingress | 256-512 MB | Prefer preview degradation; recording overload if strict delivery cannot be preserved. |
| Decoded NV12 pool | 24-48 frames, about 285-570 MiB | Block/degrade preview first; recording overload if pool exhaustion prevents recording delivery. |
| Recording queue | 120-240 frames or 1-2 seconds | Explicit recording backpressure, then recording failure if exceeded. |
| Preview queue | 2-8 frames | Drop stale preview-only frames and adapt target latency. |
| Flashback retention | User retention window, byte-capped | Retention eviction is counted; overload loss is separate and unhealthy. |
| Frame ledger hot window | 10-60 seconds | Drop oldest ledger history only, never media frames. |

The exact values are not sacred. The invariant is sacred: every budget has a number, an owner, and
a visible transition when exceeded.

## Priority / Degradation Order

Under pressure, degrade in this order:

1. Reduce expensive diagnostic sampling.
2. Drop stale preview-only frames.
3. Increase preview latency target.
4. Disable expensive preview overlays/analysis.
5. Warn that Flashback retention or export pressure is high.
6. Enter explicit recording overload only if strict recording budgets are exceeded.

Recording frame loss is not a normal degradation mode.

## Implementation Phases

### Phase 0: Baseline And Guardrails

Goal:

- Start from a clean checkpoint and ensure test/build commands are known-good.

Actions:

- Confirm `git status` is clean.
- Build app, tools, MCP server, and tests.
- Run current runtime snapshot regression tests.
- Keep the current checkpoint hash in this document.
- Create a lightweight implementation log as phases land.

Exit criteria:

- Baseline build/test results recorded.
- No hidden local artifacts dirty the tree.

Known baseline commands from the current checkpoint:

```powershell
dotnet build ElgatoCapture\ElgatoCapture.csproj -c Debug -p:Platform=x64 --no-restore /nr:false /m:1 -p:UseSharedCompilation=false
dotnet build tools\ecctl\ecctl.csproj -c Debug --no-restore /nr:false /m:1 -p:UseSharedCompilation=false
dotnet build tools\McpServer\McpServer.csproj -c Debug --no-restore /nr:false /m:1 -p:UseSharedCompilation=false
dotnet build tools\NativeXuAudioProbe\NativeXuAudioProbe.csproj -c Debug -t:Rebuild --no-restore /nr:false /m:1 -p:UseSharedCompilation=false
dotnet run --project tests\ElgatoCapture.Tests\ElgatoCapture.Tests.csproj -c Debug -p:Platform=x64 --no-restore -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"
git diff --check
```

### Phase 0.5: Baseline Live Harness

Goal:

- Capture trustworthy live evidence before major refactors change behavior.

Actions:

- Add or script timed runs for preview-only, recording-only, Flashback, and combined recording at
  4K120.
- Capture automation snapshots during each run.
- Capture exact-swap-chain PresentMon during preview runs.
- Run ffprobe/runtime verification after recording runs.
- Store summarized results in an implementation log, not as large generated artifacts in git.

Exit criteria:

- There is a repeatable baseline harness that can be rerun after each major phase.
- The harness records enough data to catch recording loss, preview pacing regression, and
  PresentMon targeting mistakes.

### Phase 1: Engine Map And Deletion List

Goal:

- Understand current ownership and identify what will be replaced, moved, or deleted.

Actions:

- Map current capture, decode, recording, Flashback, preview, diagnostics, and automation paths.
- Identify duplicated counters/timing calculations.
- Identify old fallback paths that the new engine can remove.
- Identify high-risk files for phased refactor.

Exit criteria:

- A short design note lists current flow, target flow, and deletion candidates.

### Phase 2: Frame Identity And Ledger Skeleton

Goal:

- Give every source frame a stable identity and create the central recent-history ledger.

Actions:

- Add frame identity structs.
- Add bounded frame ledger storage.
- Add event APIs for pipeline stages.
- Wire capture arrival and basic decode/preview/recording events.
- Expose recent ledger summary in automation snapshot.

Exit criteria:

- A snapshot can show recent frame sequences and stage timestamps.
- Tests prove ordering, bounds, and event retention behavior.

### Phase 3: Recording Accounting And Sovereignty Skeleton

Goal:

- Make recording completeness explicit and measurable with the current delivery model.

Actions:

- Add strict recording frame accounting.
- Add queue depth, oldest age, accepted/submitted/encoded/written counters.
- Add explicit overload/failure states.
- Remove or quarantine silent recording-drop behavior.
- Add post-stop integrity summary.

Exit criteria:

- A recording run can report whether every delivered source frame reached the current recording
  boundary.
- Full strict-consumer proof waits for Phase 5, where the delivery model is split and hardened.
- Tests cover normal completion, queue pressure, and explicit failure.

### Phase 4: Audio And A/V Integrity

Goal:

- Make audio capture and A/V sync as measurable as video.

Actions:

- Add audio clock, buffer, glitch, and discontinuity telemetry.
- Add recording audio accounting.
- Add A/V drift to runtime snapshots and final integrity summaries.
- Verify muxed audio/video duration and drift after stop.

Exit criteria:

- A recording cannot report clean unless both video and audio pass integrity checks.
- Tests cover audio discontinuity and drift reporting.

### Phase 5: MJPEG Decode Ring And Pooled Frames

Goal:

- Implement the detailed MJPEG architecture from `docs/mjpeg-pipeline-rewrite-plan.md`.
- Complete the strict recording/Flashback delivery model promised by Phase 3.

Actions:

- Shared compressed MJPEG ingress queue.
- Byte-capped buffering.
- Six-worker default with room for measured tuning.
- Pooled decoded frame leases.
- Strict recording/Flashback reorder.
- Preview-only deadline reorder.

Exit criteria:

- Recording/Flashback consumers get strict ordered frames.
- A recording run can prove whether every delivered decoded source frame reached recording.
- Preview can skip stale/missing frames without affecting recording.
- Tests simulate delayed decode, missing sequence, slow preview, and slow recording.

### Phase 6: Preview Scheduler And Renderer Ownership

Goal:

- Make preview deadline-aware and make renderer timing correlation-ready.

Actions:

- Create explicit preview scheduler.
- Track selected source frame per preview present.
- Add adaptive preview latency target.
- Add renderer lease submit path.
- Reduce redundant NV12 copies.
- Keep swap-chain address and render CPU timing exposed.

Exit criteria:

- Preview presents can be traced back to source sequence numbers.
- Renderer does not hide old frame ownership or timing.
- Tests cover scheduler deadline behavior.

### Phase 7: PresentMon Correlation

Goal:

- Tie app-level presents to exact swap-chain PresentMon observations.

Actions:

- Add app present IDs or correlation timestamps.
- Capture PresentMon with exact swap-chain address.
- Correlate selected app presents to PresentMon rows.
- Report displayed/superseded/late outcomes when available.

Exit criteria:

- A diagnostics run can distinguish app render cadence from compositor/display cadence.
- Exact swap-chain mismatch is reported clearly.

### Phase 8: Diagnostics UI Overhaul

Goal:

- Replace the current diagnostics sprawl with a layered, value-first system.

Actions:

- Build top summary fields from real engine telemetry.
- Replace ambiguous graphs with frame timeline lanes.
- Group advanced diagnostics by subsystem.
- Remove or demote obsolete score/zombie fields.
- Keep source/crop/preview visual cadence tools, but label their limitations.

Exit criteria:

- A user can tell where a stutter is likely coming from in one screen.
- Advanced sections explain the supporting evidence without duplicate/conflicting numbers.

### Phase 9: Verification And Soak Harness

Goal:

- Prove the system under real 4K120 workloads.

Actions:

- Add CLI/MCP commands for timed diagnostic sessions.
- Record frame-ledger traces during runs.
- Run 4K120 preview-only, recording-only, Flashback, and combined sessions.
- Verify recordings with ffprobe and runtime accounting.
- Track PresentMon during preview sessions.

Exit criteria:

- The app can produce a clean recording integrity report.
- Preview stutter reports identify source, capture, decode, render, present, or display as the
  most likely stage.

### Phase 10: Ruthless Cleanup

Goal:

- Remove superseded abstractions and make the new engine structure obvious.

Actions:

- Delete old unused services and fields.
- Collapse duplicate diagnostics contracts.
- Move files into clear engine-oriented namespaces.
- Trim tests that only protect deleted behavior.
- Update docs to reflect the new architecture.

Exit criteria:

- The codebase shape matches the engine model.
- New contributors can follow the hot path without spelunking through legacy branches.

## Validation Gates Per Phase

Every substantial phase should finish with:

- Local build.
- Relevant unit/regression tests.
- `git diff --check`.
- Live app smoke test when the phase touches runtime behavior.
- CLI/MCP snapshot check when diagnostics changed.
- Live performance conclusions must come from a long-enough timed sample, not a single
  instantaneous snapshot. This is live data, so short windows can produce false confidence
  or false alarms. For 4K120 preview/playback cadence, use at least a 30-second steady-state
  sample and prefer 60 seconds when judging 1%/5% lows or making optimization decisions.
  Record the sample duration, sample interval, cold/warm state, and relevant workload context
  beside the result.
- Independent adversarial review before moving to the next feature area.
- Commit with a clear rollback point.

## Acceptance Thresholds

Initial thresholds for 4K120 validation, to be refined from real data:

- Capture cadence: p95 interval <= 1.10x expected, p99 <= 1.25x expected, no unexplained severe
  gaps over 2.0x expected in a clean run.
- Decode health: decoded output keeps up with source over the run; strict reorder gaps are zero
  unless source/decode failure is explicitly recorded.
- Recording video: missing delivered source sequences = 0 for clean status.
- Recording queue: oldest frame age remains under the configured budget; exceeding it is degraded
  or failed, never hidden.
- Encoder/mux/disk: no unreported submit failures, packet-write failures, or sustained stalls
  beyond budget.
- Audio: no unreported discontinuities; p95 A/V drift within the configured sync budget; final
  muxed audio/video duration parity within the configured tolerance.
- Preview scheduler: preview-only skips are counted; no skip may alter recording accounting.
- PresentMon: expected swap-chain match is required for trusted display diagnostics; not-displayed
  rate and p99 present/display intervals are reported, not averaged away.
- Diagnostics: if a required measurement is absent, report `diagnostic_unavailable` rather than
  substituting a misleading value.

## Adversarial Review Protocol

For each major phase, run an independent reviewer with this stance:

- Assume the implementation has a hidden correctness bug.
- Look for dropped recording frames, hidden fallback behavior, unbounded queues, stale metrics,
  UI-owned truth, clock mistakes, memory copies, and concurrency races.
- Require concrete file/line findings.
- Do not accept "probably fine" for recording correctness.

The main implementation should then patch or explicitly document every finding before moving on.

## Suggested First Work After Context Clear

1. Read this document.
2. Read `docs/mjpeg-pipeline-rewrite-plan.md`.
3. Run `git status --short --branch`.
4. Run the baseline build/test set.
5. Build the Phase 0.5 live harness before invasive refactors.
6. Start Phase 1 by mapping the current pipeline and deletion candidates.

## Definition Of Success

The rewrite succeeds when a real 4K120 test can answer:

- Did the capture card deliver a frame every expected interval?
- Did the source repeat frames?
- Did MJPEG decode keep up?
- Did strict recording receive every delivered frame?
- Did the encoder/muxer/disk preserve them?
- Did preview select and submit frames on time?
- Did the exact swap chain present at the expected cadence?
- Did the compositor/display actually show or supersede those presents?
- If the user saw stutter, which stage caused it?

The final standard is not just smooth video. The final standard is smooth video plus proof.

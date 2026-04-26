# MJPEG 4K120 Pipeline Rewrite Plan

## Goals

- Keep the CPU MJPEG path reliable and understandable.
- Make preview smooth, live, and allowed to drop stale frames when needed.
- Keep recording and Flashback complete: never skip a frame just because it is old.
- Treat recording stability as a first-class product goal: on adequately provisioned hardware,
  recording should preserve every delivered source frame unless the app reaches an explicit,
  diagnosed hard-failure condition.
- Reduce avoidable 4K NV12 memory copies.
- Add enough telemetry to prove whether recording, Flashback, decode, preview, and renderer paths are correct.

## Current Findings

- 4K120 source capture cadence is stable: observed near 120 fps with no estimated source drops in recent measurements.
- Four MJPEG decoders were not enough margin when decode p95 exceeded one worker's 33 ms budget.
- Six MJPEG decoders substantially reduced reorder stalls and is now the default.
- The existing shared MJPEG reorder stage preserves frame order, but it is not a display clock. If a missing earlier sequence arrives late, multiple already-decoded later frames can be emitted back-to-back.
- Preview needs a separate pacing/deadline policy from recording and Flashback.
- Recent Flashback-backed recording verification passed with ffprobe and estimated zero dropped frames, but dedicated non-Flashback LibAv recording still needs the same live proof.

## Target Architecture

```text
MJPEG capture
  -> byte-capped compressed MJPEG ingress queue
  -> shared parallel decode work queue
  -> decoded pooled NV12 frame store
  -> strict recording ordered consumer
  -> strict Flashback ordered consumer
  -> adaptive deadline-aware preview consumer
  -> renderer upload/present
```

## Decoder Scheduling

Replace per-decoder round-robin work queues with one shared compressed work queue.

Benefits:

- Workers pull the next available MJPEG frame when free.
- Slow frames do not pin a specific worker queue as badly.
- Reorder stalls should decrease.
- Backpressure and queue byte-budgeting become easier to reason about.

Each captured MJPEG frame still receives a monotonically increasing sequence number so downstream consumers can reconstruct order.

## Buffering Policy

Use different buffers for different jobs.

- Compressed MJPEG ingress queue: larger and byte-capped, because compressed frames are much cheaper than decoded NV12.
- Decoded NV12 pool: bounded and leased to consumers.
- Preview jitter buffer: small and adaptive, because NV12 is large and preview should stay live.
- Recording/Flashback queues: strict-order and completeness-oriented, with explicit overload reporting instead of silent age-based dropping.

Initial candidate limits:

- Compressed MJPEG queue: 256-512 MB byte budget.
- Preview target: starts at 3 frames.
- Preview min target: 2 frames.
- Preview max target: 8 frames.

## Recording Completeness Contract

Recording is the highest-priority consumer. Preview quality is allowed to degrade first.

Rules:

- If a source frame is delivered to the app, recording should receive it unless capture, decode,
  memory allocation, encoder submission, or storage fails in a diagnosed way.
- Recording must never drop a frame only because it is late.
- Recording must never silently evict old frames to make room for new frames.
- If the recording path exceeds its configured memory, queue, encoder, or disk budget, the app
  should surface an explicit recording overload/failure health state with enough counters to
  explain where the bottleneck occurred.
- Preview may shed work before recording is affected.
- Flashback should follow the same completeness principle while inside the retained buffer window,
  with any retention-policy eviction counted and reported separately from overload loss.

This requires replacing current best-effort recording queue behavior with a strict policy:
queue backpressure, bounded waiting, or explicit failure are acceptable; silent recording frame loss
is not.

## Feature Preservation Strategy

Recording priority should not mean disabling the rest of the app. The goal is smarter engineering:
keep preview, Flashback, diagnostics, overlays, audio, screenshots, and automation available by
making each path cheaper, better isolated, and easier to degrade independently.

Principles:

- Protect the recording path first.
- Reduce shared CPU and memory work before removing features.
- Use explicit budgets per subsystem so one slow consumer cannot quietly starve another.
- Prefer zero-copy or single-copy ownership transfer over repeated 4K frame copies.
- Prefer adaptive preview behavior over static buffering.
- Prefer measured degradation over binary feature shutdown.
- Keep diagnostics cheap enough to run during real captures.

Recommended degradation order under sustained overload:

1. Drop stale preview-only frames.
2. Reduce preview latency target or clock-nudge preview to recover.
3. Lower diagnostic sampling frequency if diagnostics are contributing measurable overhead.
4. Temporarily disable nonessential preview adornments or expensive analysis.
5. Warn when Flashback retention or export pressure threatens the recording path.
6. Enter recording overload/failure only if strict recording budgets are exceeded.

Engineering tactics:

- Lease decoded frames to consumers instead of copying NV12 for each path.
- Give recording/Flashback independent strict consumers so preview pacing cannot block them.
- Keep preview queue depth small and adaptive.
- Keep compressed MJPEG buffering byte-capped and observable.
- Avoid blocking the capture callback on preview or diagnostics work.
- Avoid allocating per frame on the hot path.
- Add enough per-stage timing to identify whether pressure comes from decode, reorder, preview
  upload, encoder submission, muxing, disk, audio, or UI.
- Make expensive diagnostics opt-in or dynamically sampled, but keep core counters always on.

## Pooled Frame Ownership

Introduce a decoded frame lease object, roughly:

```text
PooledVideoFrame
  SequenceNumber
  ArrivalTick
  DecodedTick
  Width
  Height
  PixelFormat
  Buffer
  Length
  RefCount / LeaseCount
```

The decoder writes NV12 once into a pooled buffer. Recording, Flashback, and preview retain/release leases. A consumer only copies when it truly needs independent ownership.

This should remove the current preview-side copy chain:

```text
decoded NV12 buffer
  -> preview jitter buffer copy
  -> renderer queue copy
  -> GPU upload
```

Target:

```text
decoded NV12 pooled frame
  -> leased to recording
  -> leased to Flashback
  -> leased to preview
  -> released after all consumers finish
```

## Reorder Policy

Split reorder behavior by consumer.

### Strict Reorder

Used by recording and Flashback.

- Emits only in sequence order.
- Never skips because a frame is old.
- If a sequence is missing, waits within configured queue/budget limits.
- If the budget is exceeded, surfaces an explicit overload/failure condition.
- Does not advance encoder PTS with synthetic skips unless the recording has already entered a
  diagnosed failure/degraded state.

### Preview Reorder

Used by preview only.

- Prefers sequence order.
- Has a latency budget.
- May skip missing or stale frames only for preview.
- Must not affect recording or Flashback completeness.

## Deadline-Aware Preview

At 120 Hz, one frame is about 8.333 ms. Preview frames have a useful display window.

Candidate policy:

```text
target latency = adaptive target depth * frame interval
soft deadline = target + 2 frames
hard deadline = target + 4 frames
```

Behavior:

- If the next frame is ready and within deadline, display it.
- If the next sequence is missing briefly, wait a small amount.
- If waiting would make preview stale, skip the missing sequence for preview only.
- If queued frames are already too old, drop stale preview frames until latency returns near target.

## Adaptive Preview Jitter Buffer

The preview buffer should adjust dynamically.

Increase quickly:

```text
if underflow count increases:
  targetDepth += 1

if preview-only deadline skips increase:
  targetDepth += 1

if input burst p95 exceeds threshold:
  targetDepth += 1
```

Decrease slowly:

```text
if stable for 10-20 seconds:
  targetDepth -= 1
```

Clock nudging:

```text
if queueDepth > targetDepth:
  output interval = nominal interval * 0.995

if queueDepth < targetDepth:
  output interval = nominal interval * 1.005

otherwise:
  output interval = nominal interval
```

The intent is to recover latency gradually instead of bursting frames.

## Renderer Copy Reduction

Add a renderer submit path that accepts a frame lease instead of copying from a raw pointer:

```text
SubmitRawFrameLease(PooledVideoFrameLease frame)
```

The renderer owns the lease until upload/render queue processing is complete, then releases it.

## Recording Telemetry

Add runtime counters to prove correctness while recording:

- Source frames delivered.
- Compressed frames queued.
- Compressed frames dropped.
- Decoded frames produced.
- Decoded frames accepted by recording.
- Decoded frames rejected by recording and rejection reason.
- Recording frames accepted.
- Recording frames submitted to encoder.
- Recording frames encoded.
- Recording frame sequence gaps.
- Recording queue depth and max depth.
- Recording oldest queued frame age.
- Recording backpressure time.
- Recording overload/failure state and first failure reason.
- Encoder input PTS continuity.
- Encoder output packet count.
- Encoder dropped frame count.
- Encoder error count.
- Storage write errors or sustained muxer stalls.

Expose both totals and recent deltas.

## Flashback Telemetry

Confirm or add:

- Source-to-Flashback accepted frames.
- Flashback input sequence gaps.
- Flashback queue depth and max depth.
- Oldest queued frame age.
- Encoder submit failures.
- Segment continuity.
- Dropped frame count.

## Verification

After recording stop, verification should compare:

- Expected frame count from duration/source cadence.
- Actual encoded video frames.
- ffprobe cadence gaps.
- Container frame rate.
- Video/audio duration parity.
- Estimated dropped frames.
- Runtime source/decode/encoder accounting.

Already proven recently:

- Flashback-backed short recording verification passed.
- ffprobe cadence estimated zero dropped frames.

Still needed:

- Dedicated non-Flashback LibAv recording verification at 4K120.

## Backpressure Rules

Preview overload:

- Drop stale preview frames.
- Adjust target depth.
- Nudge output clock.
- Never block recording/Flashback.

Recording overload:

- Buffer within explicit budget.
- Never drop merely because old.
- Backpressure non-recording consumers first.
- If recording cannot accept a delivered frame within its explicit budget, enter a diagnosed
  overload/failure state rather than silently evicting or PTS-skipping frames.
- Surface fatal/health warning if the budget is exceeded.

Flashback overload:

- Follow Flashback retention policy.
- Count and surface every drop.

Decode overload:

- Preview may drop before decode.
- Recording continues buffering until budget is exceeded.
- Diagnostics should report decode below real-time.

## Testing Plan

Unit tests:

- Shared decode queue preserves sequence accounting.
- Strict reorder emits complete ordered stream.
- Preview reorder skips only preview path.
- Adaptive buffer increases on underflow.
- Adaptive buffer decreases after stability.
- Leased frame releases exactly once per consumer.

Stress tests:

- Synthetic delayed decoder worker.
- Synthetic bursty decode completion.
- Synthetic missing MJPEG input.
- Synthetic slow renderer.
- Recording consumer never misses a frame unless source/decode failed or explicit overload occurs.

Live tests:

- 4K120 preview for 60 seconds.
- 4K120 Flashback for 5 minutes.
- 4K120 recording with Flashback backend.
- 4K120 recording with dedicated LibAv backend.
- PresentMon capture before and after.
- ffprobe verification after each recording.

## Implementation Order

1. Add missing recording/Flashback/decode telemetry.
2. Add shared compressed decode queue.
3. Introduce pooled decoded frame lease type.
4. Split decoded-frame consumers.
5. Implement strict recording/Flashback reorder consumers.
6. Implement deadline-aware preview reorder consumer.
7. Make preview jitter buffer adaptive.
8. Add renderer frame-lease submit path.
9. Remove redundant preview NV12 copies.
10. Run Flashback and dedicated recording verification.
11. Tune thresholds from real 4K120 measurements.

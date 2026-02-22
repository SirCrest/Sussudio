# CFR Policy (HDR Record Path)

## Video Clock
- Video is the master clock for recording.
- CFR cadence is deterministic from negotiated FPS.
- PTS generation is frame-index based:
  - choose fixed stream timebase (`num/den`)
  - `pts = frameIndex * den / fpsNumerator * fpsDenominator` with deterministic rounding
- Capture timestamp jitter is logged but does not create VFR mux behavior.

## Video Drop/Duplicate Policy
- Maintain CFR target cadence explicitly.
- Track:
  - dropped frames required to maintain CFR
  - duplicated frames required to maintain CFR
- In HDR mode, no color-format fallback is allowed; only timing correction is allowed.

## Audio Policy
- Audio follows the video master clock.
- Correct drift asynchronously via resampling/stitching.
- Track and report:
  - `audio_pts - video_pts` over time
  - min/max/p95 drift
  - total correction applied

## Validation Gates
- Cadence fails when any threshold is violated:
  - estimated dropped frames >= 5%
  - severe gaps >= 3%
  - p95 interval >= 2.5x expected interval
- Validator failure is a recording failure.


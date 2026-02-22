# HDR Implementation Constraints

## Non-Negotiable Rules
1. Investigation-first: validator and logging are required before architecture refactors.
2. HDR mode has no fallback: if P010 negotiation fails at any point, fail the run explicitly.
3. One change per experiment: every experiment must be logged append-only in `docs/experiment_log.md`.
4. Every capture run must end with `tools/validate_hdr.ps1`; validator failure means recording failure.

## HDR Output Acceptance
- `tools/validate_hdr.ps1` must pass for:
  - HEVC Main10 output, and
  - AV1 10-bit output.
- Required HDR signaling:
  - `color_primaries=bt2020`
  - `color_transfer=smpte2084`
  - `color_space=bt2020nc` (or `bt2020c` where indicated by plan)
- If static HDR metadata is requested in settings, it must be present in output side-data.

## Hard-Fail Conditions in HDR Mode
- Media type negotiation is not P010.
- Any non-P010 frame reaches the record path.
- Encoder cannot be configured for 10-bit HEVC/AV1 + HDR signaling.
- Post-record validator fails.

## Evidence Requirements
- Capture logs must include:
  - negotiated media subtype (`P010` / `MFVideoFormat_P010`)
  - converter-disable state (`MF_READWRITE_DISABLE_CONVERTERS`)
  - ingest pixel format entering FFmpeg (`AV_PIX_FMT_P010LE`)
  - codec-specific 10-bit configuration evidence (HEVC Main10 / AV1 10-bit)
  - ffprobe fields consumed by validator


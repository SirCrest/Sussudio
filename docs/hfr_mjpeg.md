# HFR / High FrameRate (4K120) via MJPEG — Design Notes

## Context
Some capture cards expose **4K120** primarily (or only) as **MJPEG (MJPG)** over UVC. To record 4K120 as **HEVC/AV1**, the pipeline must effectively do:

**MJPEG (compressed frames) → decode → (colorspace/bitdepth) → encode (HEVC/AV1)**

If decode happens on CPU (e.g., FFmpeg MJPEG decode), it often becomes the bottleneck (e.g., ~70 fps @ 4K even on high-end CPUs), and the system can’t sustain 120 fps.

## Goals
- Support **4K120 recording** to **HEVC and/or AV1** (preferred).
- Keep the capture-card interface **Media Foundation / MediaCapture** driven (FFmpeg is encode-only; it must not open the card).
- Avoid CPU copies and CPU decode in the 4K120 path wherever possible.

## Key Constraint: Bandwidth & Copies
At 4K120, any pipeline that:
- decodes to CPU memory (e.g., `SoftwareBitmap`),
- performs CPU colorspace conversion,
- then re-uploads to GPU,
or
- sends raw frames over a pipe at huge rates

…is very likely to miss real-time.

**Rule of thumb:** If we want true 4K120, we need **GPU surfaces end-to-end**.

---

## Approaches (a few different ways)

### 1) GPU surfaces end-to-end (most likely to work)
**Path**
- Ingest via MF/MediaCapture as **DXGI/D3D11-backed surfaces** (avoid CPU-backed `SoftwareBitmap`).
- **Hardware MJPEG decode** (DXVA / Media Foundation transforms) → NV12/P010 surfaces on GPU.
- **Hardware HEVC/AV1 encode** (AMF / NVENC / QSV) consuming GPU surfaces.

**Why it works**
- Minimizes CPU time and memory bandwidth.
- Avoids CPU↔GPU round trips for decoded frames.

**Risks**
- Hardware MJPEG decode availability varies by GPU/driver.
- Hardware AV1 encode availability varies by GPU generation and driver.
- Requires a preview/record architecture that can pass GPU surfaces to the encoder.

### 2) Stay compressed on ingest, decode on GPU (good if UVC path supports it)
**Path**
- Use MF SourceReader or equivalent to receive **compressed MJPEG samples** directly (still “MF talks to the card”).
- Decode MJPEG using **hardware-accelerated MF transform** to GPU surfaces.
- Hardware encode to HEVC/AV1.

**Why it works**
- Keeping frames compressed until hardware decode reduces CPU pressure and bus traffic.

**Risks**
- Depends on device/driver exposing stable MJPEG compressed sample delivery.
- Requires careful timestamp handling and buffering to maintain 120 fps.

### 3) CPU decode + hardware encode (usually insufficient for 4K120)
**Path**
- CPU MJPEG decode (FFmpeg or other)
- Hardware encode HEVC/AV1

**Why it usually fails**
- MJPEG decode + colorspace conversion on CPU is too expensive at 4K120.
- Even if encode is GPU, CPU decode becomes the limiter.
- CPU→GPU upload bandwidth can become a limiter too.

**When it can be acceptable**
- Lower resolutions, lower fps, or unusually optimized decode path.
- “Best-effort” modes where frame drops are acceptable.

### 4) MJPEG passthrough recording (fast but not HEVC/AV1)
**Path**
- Record MJPEG frames “as-is” into a container with timestamps.

**Why it works**
- Minimal processing: no decode/encode at runtime.

**Why it’s not sufficient**
- Doesn’t satisfy “must support HEVC/AV1 output” for 4K120.
- Still useful as an optional “record what the device outputs” mode later.

---

## What vendor apps likely do (inference)
If ElgatoStudio can record high-fps to H.264/H.265/AV1, it strongly suggests:
- **hardware decode + hardware encode**, or
- a specialized pipeline that avoids CPU decode and avoids CPU copies.

This is consistent with end-to-end GPU surface workflows.

---

## Proof / Instrumentation (required to avoid guessing)
To determine whether 4K120 is feasible on a given machine/device, log:
- Whether incoming frames are **GPU surfaces** vs CPU bitmaps (`SoftwareBitmap` vs `Direct3DSurface`).
- Decode stage:
  - decoder type (hardware vs software),
  - per-frame decode time,
  - queue depth / late frames / drops.
- Convert stage:
  - GPU convert vs CPU convert,
  - any CPU memcpy volume.
- Encode stage:
  - hardware encoder selected,
  - per-frame encode time,
  - encoder input pixel format.

Decision rule:
- If frames become CPU-backed at any point in the HFR path, **4K120 HEVC/AV1 should be considered unsupported** for that configuration.

---

## Implications for our architecture
A “rawvideo over stdin” encoder path (pipe) is not a good fit for 4K120:
- It forces enormous CPU memory traffic and context switching overhead.
- It tends to imply CPU copies and CPU-managed frame buffers.

To truly support 4K120 MJPEG → HEVC/AV1, we likely need:
- GPU-surface ingest,
- a GPU-capable encode path (hardware encoder API or in-process pipeline that can consume GPU surfaces),
- and strict capability gating per device/GPU/driver.

---

## Capability Gating Suggestion
Expose 4K120 modes only when runtime probing confirms:
- MJPEG ingest at 120 fps is available from the card, AND
- hardware MJPEG decode is available, AND
- hardware HEVC/AV1 encode is available, AND
- the pipeline stays on GPU surfaces (no CPU fallback).


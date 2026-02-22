# Hybrid HDR (P010) + CFR Recording Plan (Investigation-First, Hard-Fail HDR)

  This plan is anchored to the current repo shape:

  - Recording verification already exists via ElgatoCapture/Services/RecordingVerifier.cs (ffprobe-based, includes HDR colorimetry + cadence analysis).
  - Current runtime recording backend is DirectShow feeding an ffmpeg.exe subprocess via ElgatoCapture/Services/FFmpegEncoderService.cs and ElgatoCapture/Services/DirectShowFfmpegRecordingSink.cs.
  - Runtime snapshot already carries HDR parity fields and observed pixel-format telemetry via ElgatoCapture/Services/CaptureService.cs.

  Your requested end-state is a hybrid pipeline:

  - Ingest: Media Foundation (optionally DXGI/D3D11 surfaces) negotiating true P010 frames (10-bit).
  - Preview: WinUI preview sourced from the same capture session/stream (optionally SDR-tonemapped).
  - Encode/Mux: FFmpeg libraries (libavcodec, libavformat) using 10-bit input and output HEVC Main10 or AV1 10-bit with correct HDR signaling.
  - Hard rules enforced: investigation-first; HDR mode is strict (no fallback); one-change-per-experiment with recorded evidence.

  ———

  ## Phase 1: Investigation Only (Logging + HDR Validator Gate) (Hard rule #1)

  ### Goals

  1. Precisely identify where HDR fails today (device negotiation vs ingest vs encoder args vs container tags).
  2. Create a single source of truth “HDR validator” that can be run on every experiment output.
  3. In HDR mode: if output is not HDR-valid, the run must hard-fail (no “it recorded but was wrong”).

  ### Implementation tasks (additive, minimal surface area)

  1. Tighten and formalize the HDR validator criteria in the existing verifier:
      - Extend/adjust ElgatoCapture/Services/RecordingVerifier.cs to enforce:
          - codec allowlist: hevc OR av1 only (when HDR requested).
          - pixel format allowlist for “10-bit”: accept only p010le, yuv420p10le, yuv422p10le, yuv444p10le (do not accept “contains 10”).
          - strict colorimetry: color_primaries=bt2020, color_transfer=smpte2084, color_space=bt2020nc (optionally bt2020c).
          - mastering metadata: required only if requested (already modeled via RequestedHdrMasteringMetadata).
      - Keep cadence checks as-is for CFR signal (already implemented via ffprobe frame timestamp analysis).
  2. Add a “validator gate” to the reliability gates:
      - Wire a new optional step into tools/reliability-gates.ps1:
          - Run a scripted ffprobe-based validator against the last produced recording artifact (or a supplied path).
          - Exit non-zero on HDR mismatch (for HDR-mode experiments).
      - This is still “investigation-only”: it does not change capture/encode paths; it just fails wrong outputs.
  3. Add missing capture-time logging hooks (no behavior change):
      - ElgatoCapture/Services/CaptureService.cs:
          - When HDR requested: log a single structured line summarizing requested vs negotiated vs observed:
              - requested: HDR enabled, desired subtype/pix_fmt (target P010 / p010le), target fps/size.
              - negotiated (current): _actualPixelFormat, _actualWidth/_actualHeight, _actualFrameRate/_actualFrameRateArg.
              - observed (already present): FirstObservedFramePixelFormat, LatestObservedFramePixelFormat, LatestObservedSurfaceFormat, ObservedP010FrameCount, ObservedNv12FrameCount,
                ObservedOtherFrameCount.
          - Add bit-depth sampling counters that are already stubbed in snapshot:
              - ObservedP010BitDepthSampleCount
              - ObservedP010Low2BitNonZeroPercent (on extracted 10-bit values)
              - ObservedP010Likely8BitUpscaled
          - Sampling method (investigation-safe): in P010, each sample is a 16-bit LE word with the 10-bit value stored in the high bits. Extract sample10 = (word >> 6), then analyze whether
            (sample10 & 0x3) is almost always 0 (strong signal of 8-bit content upscaled <<2 into a 10-bit container). Optionally log a small histogram of (sample10 & 0x3); also assert
            (word & 0x3F) == 0 for canonical P010 packing (non-zero padding bits imply mispack/format mismatch).

  ### Done / exit criteria

  - You can run one HDR recording and get a single PASS/FAIL result with evidence:
      - PASS: ffprobe fields and cadence thresholds are all satisfied.
      - FAIL: validator produces an actionable mismatch code (pix_fmt, primaries, transfer, matrix, side-data missing, cadence).

  ———

  ## Phase 2: Minimal MF Ingest Harness (Negotiate True P010; No Encoding Yet)

  ### Goal

  Prove Media Foundation can negotiate and deliver P010 from “Elgato 4K X” without silently inserting converters that downgrade to NV12.

  ### Shape (small harness, not a refactor)

  Create a new minimal harness project under tests/ (keeps app unchanged):

  - tests/ElgatoCapture.HdrLab/ (console app or headless test executable)
  - Outputs artifacts under artifacts/hdr-lab/<timestamp>/.

  ### Core tasks (function-level)

  1. Enumerate capture device and list available formats:
      - Prefer an IMFSourceReader harness with MF_READWRITE_DISABLE_CONVERTERS=TRUE (no silent conversion).
      - If using MediaCapture + VideoDeviceController.GetAvailableMediaStreamProperties(MediaStreamType.VideoRecord) (or Preview), log every candidate:
          - subtype/fourcc (string)
          - width/height
          - frame rate numerator/denominator
          - any exposed color space metadata (if available)
  2. Select exact target media type:
      - Prefer a “no conversion” stance:
          - Do not enable video processing/conversion flags.
          - Request uncompressed P010 subtype explicitly (string "P010" where applicable).
  3. Read frames via MediaFrameReader:
      - For each MediaFrameReference, log:
          - VideoMediaFrame.SoftwareBitmap.BitmapPixelFormat (expect P010)
          - If using Direct3DSurface, log DXGI format via interop (expect DXGI_FORMAT_P010).
          - frame SystemRelativeTime and any MF timestamps accessible (for later CFR work).
      - Hard-fail if any frame arrives as NV12/anything other than P010 when HDR is requested.
  4. Write raw samples for proof:
      - Write the first frame to disk as a tight-packed raw p010le dump (P010 is semiplanar: Y plane then interleaved UV; 16-bit LE words with 10-bit in high bits). Include a JSON sidecar with width/height/strides.
      - Warn: DXGI/D3D11 surfaces often have row-pitch padding; repack to tight planes before writing/feeding ffmpeg.
      - Optional: write 120 frames (~4 seconds @30) to confirm stability.

  ### Evidence required

  - Log excerpt proving negotiation is P010 and frames arrive as P010 (not NV12).
  - Raw dump exists, with correct expected byte size per frame (tight-packed, no padding): width * height * 3 bytes (P010 4:2:0 as 16-bit LE words).

  ### Done / exit criteria

  - Harness consistently captures P010 for at least 120 frames at a chosen mode (e.g., 4K30 HDR).

  ———

  ## Phase 3: FFmpeg Encode Harness (Start with CLI for Fast Isolation; Then Libraries)

  You asked for FFmpeg libraries as the end-state, but for convergence speed the plan uses a two-step harness approach:

  1. CLI encode from the raw P010 dump to validate tag/metadata mechanics quickly.
  2. Implement the equivalent path using libavcodec/libavformat once the CLI behavior is proven.

  ### Phase 3A: CLI encode experiments (fast, one-change-per-experiment)

  Input: the raw p010le dump from Phase 2.

  HEVC Main10 (example command template):

  ffmpeg -v error -f rawvideo -pix_fmt p010le -s:v <WxH> -r <fps> -i input_p010.yuv `
    -c:v libx265 -profile:v main10 -pix_fmt yuv420p10le `
    -color_primaries bt2020 -color_trc smpte2084 -colorspace bt2020nc `
    -x265-params "master-display=<...>:max-cll=<maxcll>,<maxfall>:hdr10=1:hdr10-opt=1" `
    -movflags +faststart -tag:v hvc1 output_hevc_hdr.mp4
  
  Note: For HEVC HDR metadata repair/injection post-encode, consider the hevc_metadata bitstream filter and re-validate with ffprobe.

  AV1 10-bit (example command template):

  ffmpeg -v error -f rawvideo -pix_fmt p010le -s:v <WxH> -r <fps> -i input_p010.yuv `
    -c:v <av1_encoder> -pix_fmt yuv420p10le `
    -color_primaries bt2020 -color_trc smpte2084 -colorspace bt2020nc `
    -movflags +faststart output_av1_hdr.mp4

  Validation: run the HDR validator (Phase 1) and record PASS/FAIL.

  ### Phase 3B: Library encode harness (decision-complete approach)

  Chosen approach: use FFmpeg.AutoGen in a new harness project first, then integrate into the app after it’s stable.

  - Rationale: minimal native build system work to start; can still move to a native DLL later if needed.

  Library harness tasks:

  1. Allocate AVFormatContext (MP4), add one video stream, set:
      - codec: HEVC or AV1
      - color_primaries, color_trc, colorspace on codec context/stream where applicable
  2. Accept input frames as:
      - CPU p010le (from dump first)
      - Later: D3D11 texture ingestion path (optional) if perf demands it.
  3. Encode/mux and emit MP4.
  4. Validate output with the same HDR validator.

  Done criteria:

  - Library harness produces an MP4 that passes the validator for HEVC Main10 and/or AV1 10-bit.

  ———

  ## Phase 4: Integrate MF Ingest + Preview + libav Encode Into the App (Minimal, Additive)

  ### Goal

  Introduce a new HDR-only recording backend without refactoring existing SDR/DirectShow paths.

  ### Implementation shape

  1. Add a new recording backend (new sink) alongside DirectShowFfmpegRecordingSink:
      - e.g. MediaFoundationLibAvRecordingSink (name TBD)
  2. Extend CaptureService backend selection:
      - If HDR requested: use MF+libav sink only.
      - If HDR not requested: keep existing behavior.

  ### Preview requirement (same capture stream)

  Use a single MediaCapture session for both:

  - Preview: WinUI pipeline (can be SDR-tonemapped if needed; explicitly label UI state as “HDR ingest, SDR preview tonemap”).
  - Encode input: MediaFrameReader from the same MediaCapture.

  ### Hard-fail rules (HDR mode, no fallback)

  When HDR is requested:

  - If negotiated subtype is not P010, fail immediately before recording starts.
  - If any frame arrives as NV12 (or anything non-P010), fail immediately.
  - If libav encoder cannot be configured for 10-bit output + bt2020/pq/bt2020nc, fail immediately.
  - If post-record validator fails, treat the run as failed (and preserve artifacts/logs).

  Done criteria:

  - App can record a short HDR clip that passes validator and shows preview.

  ———

  ## Phase 5: CFR + Audio Sync Policy (Explicit, Measurable)

  ### CFR policy (video PTS strategy)

  Define one authoritative frame clock for encoding:

  - Expected CFR FPS = the selected capture mode FPS (from negotiated mode).
  - Produce PTS as frame_index in a fixed time_base (e.g. 1/90000 or 1/fps rational):
      - pts = frame_index * (time_base_den / fps_num) style, rounded deterministically.
  - For capture timestamps:
      - Log them, but do not let irregular capture timestamps create VFR in the mux.
  - Frame drop/duplication rules:
      - In HDR mode, do not silently “smooth” by conversion; only timing adjustments are allowed.
      - Record metrics for “frames dropped to maintain CFR” vs “frames duplicated”.

  ### Audio sync policy

  Pick a single master clock (recommendation: video CFR clock is master) and resample audio to match:

  - Resample/stitch via libswresample to correct drift rather than altering video PTS.
  - Log drift:
      - audio_pts - video_pts over time, max/min, and correction applied.

  Done criteria:

  - RecordingVerifier cadence metrics remain within thresholds.
  - Long capture (10+ minutes) shows bounded A/V drift in logs (and user-perceived sync).

  ———

  ## Phase 6: Optional HDR Metadata (HDR10 static metadata) (Strict if requested)

  ### Scope

  Only HDR10 static metadata (mastering display + maxCLL/maxFALL) as you described:

  - mastering display (SMPTE ST 2086; chromaticities + luminance)
  - content light level (MaxCLL/MaxFALL)

  ### Rules

  - If metadata is supplied in settings: HEVC requires it and validator enforces presence via ffprobe side data list; AV1 keeps it optional unless proven end-to-end for the chosen encoder/container.
  - If metadata not supplied: validator enforces only colorimetry + 10-bit.

  Done criteria

  - Files with metadata requested show Mastering display metadata and/or Content light level metadata in ffprobe side_data_list.

  ———

  # Deliverable B: Exact Logs/Metrics to Add (Loss Localization)

  Log at 4 choke points: negotiate → ingest → encode input → encode output.

  ## 1) Capture negotiation (Media Foundation / MediaCapture)

  For the selected mode (and a short list of rejected candidates):

  - Device identity: friendly name, device id.
  - Media type fields:
      - Subtype/FourCC (expect "P010" for HDR)
      - width/height
      - frame rate numerator/denominator
      - interlace mode
      - stride (if exposed)
  - MF/MFT attributes to log when available (log raw GUID + interpreted name if known):
      - MF_MT_SUBTYPE
      - MF_MT_FRAME_SIZE
      - MF_MT_FRAME_RATE
      - MF_MT_INTERLACE_MODE
      - MF_MT_DEFAULT_STRIDE
      - MF_MT_FIXED_SIZE_SAMPLES
      - MF_MT_SAMPLE_SIZE
      - Colorimetry (if present):
          - MF_MT_VIDEO_PRIMARIES
          - MF_MT_TRANSFER_FUNCTION
          - MF_MT_YUV_MATRIX
          - MF_MT_VIDEO_NOMINAL_RANGE

  ## 2) Ingest (per-frame, but only first N frames + counters thereafter)

  - SoftwareBitmap.BitmapPixelFormat (expect P010)
  - If surface-backed:
      - DXGI_FORMAT (expect DXGI_FORMAT_P010)
      - texture desc: width/height/format/mipLevels/arraySize/sampleDesc
  - Frame timestamps:
      - capture timestamp source and value (SystemRelativeTime and any MF sample time you can access)
  - Counters:
      - frame counts by format (P010/NV12/other)
      - “first non-P010 frame index” (if it happens)

  ## 3) Encode input (libav side)

  - For the first encoded frame and then periodic (every X seconds):
      - AVFrame.format (expect AV_PIX_FMT_P010LE in input stage)
      - linesize[], data[] presence, and color_primaries/color_trc/colorspace/color_range on frame if you propagate it
  - CFR:
      - PTS generation: log first 10 PTS values and time_base
      - dropped/duplicated frames counters

  ## 4) Encode output (validator-driven)

  - On stop:
      - full ffprobe JSON captured into artifacts
      - validator PASS/FAIL with mismatch codes
      - cadence metrics summary (already implemented in verifier)

  ———

  # Deliverable C: “HDR Validator” Spec (ffprobe Fields + Pass/Fail)

  ## Input

  - Output file path: *.mp4
  - Mode: HDR required if --expectHdr or inferred from runtime snapshot / experiment label.

  ## ffprobe command (canonical)

  ffprobe -v error -select_streams v:0 `
    -show_entries format=format_name `
    -show_entries stream=codec_name,profile,width,height,avg_frame_rate,r_frame_rate,pix_fmt,color_primaries,color_transfer,color_space,color_range,side_data_list `
    -of json "<file>"

  Optional cadence probe (only when CFR must be validated):

  ffprobe -v error -select_streams v:0 -show_frames `
    -show_entries frame=best_effort_timestamp_time,pkt_dts_time,pkt_pts_time `
    -of json "<file>"

  ## Pass/Fail criteria (HDR mode)

  FAIL if any of the following is true:

  - stream.codec_name not in { "hevc", "av1" }
  - stream.pix_fmt not in { "p010le", "yuv420p10le", "yuv422p10le", "yuv444p10le" }
  - stream.color_primaries does not contain bt2020
  - stream.color_transfer is not smpte2084
  - stream.color_space not in { bt2020nc, bt2020c }
  - If HDR10 static metadata was requested (HEVC only for now):
      - stream.side_data_list does not include Mastering display metadata and/or Content light level metadata (at least one present; ideally both when both provided)

  CFR (when enforced) FAIL if:

  - Estimated dropped frames >= 5% OR
  - Severe gaps >= 3% OR
  - P95 interval >= 2.5x expected interval (same thresholds as current RecordingVerifier)

  ## PowerShell script outline (not full code)

  param(
    [Parameter(Mandatory=$true)] [string] $File,
    [switch] $ExpectHdr,
    [ValidateSet('hevc','av1','either')] [string] $Codec = 'either',
    [switch] $RequireHdr10StaticMetadata,
    [double] $ExpectedFps = 0
  )

  # 1) Run ffprobe JSON (format+stream fields)
  # 2) Parse JSON; extract v:0 stream fields
  # 3) Apply rules above; accumulate mismatch codes
  # 4) If $ExpectedFps -gt 0, run cadence ffprobe JSON and compute intervals/jitter metrics
  # 5) Print a single-line PASS/FAIL + mismatch list; exit 0 on PASS, 1 on FAIL

  Reads: the output file passed as $File. Emits: PASS or FAIL plus mismatch codes, and optionally writes the ffprobe JSON to artifacts/<timestamp>/ffprobe.json.

  ———

  # Deliverable D: Minimal Experiment Matrix (8 Experiments, Converge Fast)

  Each experiment must record:

  - What changed (one change only)
  - How to run it (exact command / UI steps)
  - PASS/FAIL with evidence (logs + validator output + ffprobe JSON)

  1. E1: Baseline validator on known-good HDR sample
      - Change: add standalone validator script (or wire verifier output to file).
      - Run: tools/hdr-validate.ps1 -File <known_hdr_mp4> -ExpectHdr
      - PASS criteria: validator passes on a reference HDR file (proves validator isn’t broken).
  2. E2: Current app HDR recording, validator only
      - Change: wire validator gate to run on output artifact after recording stop.
      - Run: record HDR in app once.
      - Expected: likely FAIL today if ingestion is NV12 or tags missing; evidence tells you exactly which.
  3. E3: MF negotiate P010 (log-only)
      - Change: new ElgatoCapture.HdrLab harness that enumerates modes and selects P010.
      - Run: dotnet run --project tests/ElgatoCapture.HdrLab -- --device \"Elgato 4K X\" --mode P010 --frames 1
      - PASS: logs show selected subtype P010 and first frame arrives as P010.
  4. E4: MF P010 stability (120 frames + counters)
      - PASS: 120/120 P010; no NV12 observed.
      - Change: none to app; only a command line using the raw dump.
      - Run: ffmpeg CLI encode template (HEVC).
      - PASS: validator passes; proves “tags+metadata+container” path is solvable.
  6. E6: Raw P010 -> AV1 10-bit via ffmpeg CLI
      - Change: CLI only.
      - Run: AV1 encode template.
      - PASS: validator passes (or you learn exactly what metadata is missing for AV1-in-MP4 on your ffmpeg build).
  7. E7: Library encode harness (CPU input)
      - Change: new minimal libav harness that reads the raw dump and produces MP4.
      - Run: dotnet run --project tests/ElgatoCapture.LibAvLab -- --in input_p010.yuv --codec hevc --out out.mp4 --hdr pq
      - PASS: validator passes; proves libav integration works before touching MF or the app.
  8. E8: End-to-end hybrid (MF ingest + libav encode)
      - Change: integrate MF ingest feeding libav encoder in the app (HDR mode only).
      - Run: app HDR record 10–30 seconds.
      - PASS: validator passes; logs show P010 all the way through.

  (If you need CFR/audio evidence earlier, swap E8 with CFR experiments once E5/E7 are stable.)

  ———

  # Deliverable E: Common Edge Cases and How We Detect Them

  - Silent MF converter fallback (P010 requested, NV12 delivered)
      - Detect: per-frame SoftwareBitmap.BitmapPixelFormat counters + immediate hard-fail in HDR mode.
  - P010 negotiation succeeds but it’s actually 8-bit padded
      - Detect: extract 10-bit samples (word >> 6) and check whether the low 2 bits of those 10-bit values are almost-always zero (strong signal of 8-bit upscaled <<2 into a 10-bit container).
  - 10-bit output without HDR tags (PQ/BT.2020 missing)
      - Detect: validator requires color_primaries=bt2020, color_transfer=smpte2084, color_space=bt2020nc.
  - Preview tonemapping confusion (preview looks SDR but recording is HDR, or vice versa)
      - Detect: log and UI label two independent states:
          - ingest/encode HDR state (validator-driven)
          - preview presentation mode (tonemap on/off)
      - Never infer recording HDR correctness from preview appearance.
  - CFR timestamp strategy accidentally produces VFR
      - Detect: cadence ffprobe analysis (already in verifier) plus logging PTS/time_base generation in the encoder.
  - AV1 vs HEVC HDR metadata differences (static metadata sometimes not carried as expected in MP4)
      - Detect: validator distinguishes “ColorimetryOnly” vs “FullMetadata” based on whether mastering metadata was requested; enforce only what you asked for.

  ———

  ## Assumptions / Defaults (explicit)

  - HDR output target is Rec.2100 PQ (SMPTE 2084), BT.2020 primaries, BT.2020 non-constant luminance matrix (bt2020nc).
  - Container is MP4 unless later requirements change.
  - HDR10 static metadata is optional unless explicitly provided; if provided, it becomes required and validated.
  - No fallback in HDR mode means: no NV12 downgrade, no SDR “just record anyway,” and validator failure is a hard failure.

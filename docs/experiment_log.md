# Condensed Experiment Log

Last reviewed: 2026-05-12.

This file is a compact, current-state replacement for the former append-only
`docs/experiment_log.md`. The last large version was deleted in commit
`af98fe1` (`Delete docs directory`); the last pre-delete source can be read with:

```powershell
git show 802c742a83a75735b1f26b924f09dcbc4a5ce3c6:docs/experiment_log.md
```

That historical file was about 50k words. Use it only for exact old evidence,
not as current truth. This condensed file is the agent-facing map.

## Reading Rules

- Prefer current source, current logs, and current diagnostic summaries over old
  experiment text.
- Treat pre-2026-05-02 names as historical: `ElgatoCapture` -> `Sussudio`,
  `ecctl` -> `ssctl`, `ElgatoCapture_Debug.log` -> `Sussudio_Debug.log`, and
  `ELGATOCAPTURE_*` -> `SUSSUDIO_*`.
- Old numbered entries had duplicate IDs and correction entries. Do not infer
  chronology from an `E###` number alone.
- Static test/build checkpoints prove source contracts only. Runtime claims
  still require a live `ssctl diagnostic-session ... --verify` summary.
- Avoid re-expanding this file. If a new finding matters, replace or tighten a
  bullet instead of appending a long narrative.

## Current Evidence Surfaces

- Live app log: `temp/logs/Sussudio_Debug.log`.
- Diagnostic summaries: `temp/diagnostic-sessions/<session>/summary.json`.
- Saved e2e evidence from the flashback/audio audit:
  `results/e2e-20260505-193751` and `results/e2e-20260505-focused`.
- Automation pipe: `SussudioAutomation`, with optional
  `SUSSUDIO_AUTOMATION_TOKEN`.
- Main CLI: `tools/ssctl`; shared protocol/catalog live under
  `Sussudio.Automation.Contracts`.
- Useful command family:
  `ssctl diagnostic-session --seconds <N> --verify <scenario>`.
- UI/MCP-visible toggles include `ssctl frametime show|hide` and
  `ssctl flashback timeline show|hide`.

## Stable Current Facts

### Recording And Encoding

- The active recording backend is in-process LibAV, centered on
  `LibAvRecordingSink` and `LibAvEncoder`; do not reason from the older
  subprocess FFmpeg backend.
- Normal recording sends video through raw/GPU/CUDA encoder interfaces, while
  HDMI audio writes through `IRecordingSink.WriteAudioAsync`.
- Microphone capture is intentionally asymmetric: it writes through a delegate
  to `LibAvRecordingSink.WriteMicrophoneAudioAsync(...)`.
- AV1 recording should not silently downgrade to HEVC. The current source maps
  `RecordingFormat.Av1Mp4` to `av1_nvenc` and throws if that encoder is not
  available.
- Recording verifier comparisons should prefer negotiated capture geometry and
  timing, falling back to requested settings only when negotiation metadata is
  unavailable.

### Flashback

- `FlashbackBackendResources` is the authoritative owner for the preview-owned
  Flashback sink, buffer manager, exporter, playback controller, settings
  snapshot, preserve policy, and finalize orchestration.
- Flashback recording/export must force-rotate/finalize the live-edge segment
  before concatenation. Do not export from still-open active segments.
- If Flashback is disabled, the timeline should be hidden/locked out, not merely
  left visible with a warning state.
- Scrub/seek frames should not contaminate playback cadence metrics. Treat
  user scrubbing as intentionally choppy and separate from playback smoothness.
- For playback audio continuity, watch:
  `FlashbackPlaybackAudioMasterFallbacks`,
  `FlashbackPlaybackAudioMasterDriftOutlierFallbacks`,
  `FlashbackPlaybackAudioBufferedDurationMs`,
  `WasapiPlaybackRenderSilenceCount`, dropped frames, and submit failures.
- Historical short smokes passed after the restructuring work, but long go-live
  claims still need fresh 180s-300s diagnostic summaries on the current build.

### Preview, Cadence, And HFR MJPEG

- 4K120 MJPG is a special SDR capture mode used to fit USB bandwidth. Source
  telemetry can still report the HDMI source as HDR/59.94 while capture is
  selected/negotiated as MJPG/120.
- HFR MJPEG duplicate-source cadence is a known classification: packet/output
  cadence can be near 120fps while unique visual changes are near 60fps. That is
  source-signal evidence, not automatically a preview or encoder drop.
- Live preview latency now means app receive/source-reader arrival to estimated
  visible refresh. Formatter text calls this `app receive -> estimated visible`.
- The stats UI keeps source telemetry separate from capture negotiation. Do not
  use `ReaderSourceSubtype` or `NegotiatedPixelFormat` as HDMI-source facts.
- `SourceTelemetryDiagnosticSummary` is not guaranteed to use structured
  `nativexu:` segments. Preserve plain failure/reason strings when parsing.

### Audio

- WASAPI audio writes are a hot path. Implementations should copy/enqueue
  synchronously, avoid blocking or real async work, and return a completed task.
- `GetAudioRampTrace` is the app-layer surface for preview audio ramp forensics.
  It records UI/control envelope and render-side WASAPI output-level fields.
- Audio diagnostics snapshots read `AudioPeak`; if only the animated UI meter is
  updated, automation can still report silence.
- Current saved evidence did not prove recording loss, playback drops, submit
  failures, or export failures for the residual audible-stutter complaint. The
  most relevant saved lead was audio-master fallback counters during short
  flashback playback/export runs.

### Automation And Tooling

- `AutomationCommandCatalog` is the shared source for command metadata, payload
  shape, readiness gating, response timeout, path policy, CLI help text, and MCP
  description text.
- If `AutomationCommandKind` changes, update the contracts catalog/protocol,
  dispatcher, `ssctl`, MCP tools, diagnostic sessions, tests, and freshness
  inputs together.
- `ssctl --help` is the preferred helper check. Old `ecctl` references are
  historical.
- Long Flashback export operations use the Flashback mutation timeout path
  (`305000ms`) across shared protocol, MCP, and diagnostic sessions.
- In sandboxed agent runs, named-pipe access can fail with access denied even
  when the app and CLI are healthy in the user context. Verify outside the
  sandbox before blaming the app pipe.

### Structure Checkpoints

- `LibAvRecordingSink` uses fair queue drain ordering: audio/microphone first,
  bounded video batches, then audio/microphone again.
- Recording and Flashback video `TryEnqueue*VideoFrame` paths should be
  immediate tries. Reintroducing sleeps/retry waits in the capture callback path
  risks preview and audio continuity.
- `CaptureModeOptionsBuilder` owns deterministic resolution/video-format option
  construction; `MainViewModel` still owns selection state and reinit decisions.
- `StatsPresentationBuilder` owns pure stats text/status projection; WinUI files
  own polling, row pools, brushes, and drawing.
- `FlashbackBackendResources` is the right place for new backend ownership
  behavior. Avoid adding a second manager or parallel source of truth in
  `CaptureService`.

## Historical Topics Worth Rechecking From Source

- Old `LibAvEncoder` drift notes contradicted later code more than once. Before
  acting on A/V drift claims, read the live `EncodeAudioChunk()`, queued sample
  helpers, and CPU/GPU/CUDA video send paths.
- Old NativeXu/EGAV/RTICE audio-control experiments were exploratory. Current
  implementation choices should be verified from source, not revived from those
  notes.
- Old performance numbers are evidence for the build/session that produced
  them only. Re-run diagnostics before presenting them as current behavior.
- The old append-only log preserved some failed experiments and corrected
  duplicate entries; treat it as archaeology, not a runbook.

## Fast Agent Workflow

1. Read this file for orientation.
2. Inspect the current source file named by the relevant bullet.
3. Inspect the newest matching `summary.json` or `Sussudio_Debug.log` evidence.
4. Run a focused diagnostic session when runtime behavior matters.
5. Update this file only with durable conclusions that reduce future reading.

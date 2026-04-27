# Real-Time Capture Engine Map

This is the Phase 1 current-state map for
`docs/realtime-capture-engine-rewrite-plan.md`. It records what exists today,
what the target engine wants, and where old paths should be deleted or
quarantined as later phases land.

## Current Hot Path

```text
MainWindow / MainViewModel
  -> CaptureSessionCoordinator
  -> CaptureService
  -> UnifiedVideoCapture
  -> MfSourceReaderVideoCapture
  -> ParallelMjpegDecodePipeline when MJPEG HFR is active
  -> LibAvRecordingSink or FlashbackEncoderSink
  -> D3D11PreviewRenderer
```

Key ownership today:

- `CaptureService` is still the central lifecycle owner for device, preview,
  recording, Flashback, audio, and snapshot state.
- `CaptureSessionCoordinator` serializes user and automation lifecycle
  commands before they reach `CaptureService`.
- `UnifiedVideoCapture` wraps source-reader capture, recording sink fanout,
  preview sink fanout, MJPEG decode, pooled-frame fanout, and capture counters.
- `MfSourceReaderVideoCapture` owns the IMFSourceReader loop and emits raw or
  compressed frames into the app.
- `FrameLedger` records recent source-frame events across capture, MJPEG decode,
  preview, recording, and Flashback fanout. It is intentionally bounded and
  diagnostic-first; it is not a persistence layer.
- `ParallelMjpegDecodePipeline`, `PooledVideoFrame`, and
  `MjpegPreviewJitterBuffer` carry the strict MJPEG/lease path. The preview
  jitter buffer assigns preview-present IDs and records selected/dropped source
  ownership.
- `LibAvRecordingSink` and `FlashbackEncoderSink` are both recording sinks with
  explicit queue/failure counters. Post-stop recording integrity is exposed as
  a single summary in runtime and automation snapshots.
- `D3D11PreviewRenderer` owns the D3D preview path and exposes render/swap-chain
  data through preview snapshots, including app present IDs and UTC timestamps
  for PresentMon correlation.
- `AutomationDiagnosticsHub` merges view-model, capture, preview, health, and
  verification data into the automation snapshot consumed by UI, CLI, and MCP.
  It produces the top-level diagnostic health/stage/evidence fields that should
  lead user-facing diagnostics.

## Target Alignment

The target engine flow is:

```text
capture ingress
  -> frame identity
  -> compressed ingress ring
  -> decode ring
  -> ordered decoded frame store
  -> recording / Flashback / preview consumers
  -> renderer / PresentMon correlation
  -> shared control and diagnostics plane
```

The frame identity and ledger layer now exists as a bounded diagnostic spine.
The automation snapshot remains the practical control-plane boundary, while
timed diagnostic sessions export snapshots, frame-ledger traces, timelines,
PresentMon captures, and recording-verification results as artifacts.

## Duplication To Collapse

- Long-form formatter text still exists in both shared MCP formatting and
  ecctl-specific formatting. The field set is test-aligned, but a future pass
  can make ecctl delegate more of the full snapshot text to the shared
  formatter.
- Some advanced counters remain available in the snapshot even when the new
  diagnostic lane fields already summarize the same subsystem. Keep them while
  they are useful for forensic runs, but avoid adding new UI around them unless
  they answer a distinct question.
- The stats window and automation hub each derive a health summary from nearby
  telemetry. The automation snapshot is authoritative for remote tools; the
  stats window copy is a lightweight in-process view.

## Deletion And Quarantine Candidates

Do not delete these blindly. Each item should be removed only after the
replacement ledger/engine path has tests and live evidence.

- Quarantine any recording path that can silently evict, drop, or synthesize
  continuity without an explicit failure state.
- Remove preview-only timing counters that duplicate ledger or diagnostic-lane
  fields without adding clock-domain information.
- Delete legacy raw-copy preview paths once renderer frame-lease submission is
  proven across preview, screenshot, and Flashback playback.
- Collapse duplicated automation formatter code once ecctl no longer needs
  bespoke full-snapshot sections.
- Demote snapshot fields that are always zero, stale, or inferred from another
  field after the new diagnostics UI has replacement values.

## High-Risk Files

- `ElgatoCapture/Services/Capture/CaptureService.cs`
- `ElgatoCapture/Services/Capture/CaptureService.Snapshots.cs`
- `ElgatoCapture/Services/Capture/UnifiedVideoCapture.cs`
- `ElgatoCapture/Services/Capture/MfSourceReaderVideoCapture.cs`
- `ElgatoCapture/Services/Gpu/ParallelMjpegDecodePipeline.cs`
- `ElgatoCapture/Services/Capture/PooledVideoFrame.cs`
- `ElgatoCapture/Services/Capture/MjpegPreviewJitterBuffer.cs`
- `ElgatoCapture/Services/Recording/LibAvRecordingSink.cs`
- `ElgatoCapture/Services/Recording/LibAvEncoder.cs`
- `ElgatoCapture/Services/Flashback/FlashbackEncoderSink.cs`
- `ElgatoCapture/Services/Flashback/FlashbackBufferManager.cs`
- `ElgatoCapture/Services/Preview/D3D11PreviewRenderer.cs`
- `ElgatoCapture/Services/Preview/D3D11PreviewRenderer.Rendering.cs`
- `ElgatoCapture/Services/Automation/AutomationDiagnosticsHub.cs`
- `ElgatoCapture/Services/Automation/AutomationCommandDispatcher.cs`
- `ElgatoCapture/Models/AutomationContracts.cs`
- `ElgatoCapture/ViewModels/MainViewModel*.cs`
- `ElgatoCapture/MainWindow*.cs`

## Phase 10 Status And Next Entry Point

Phases 0.5 through 9 have landed in the current branch. The next cleanup pass
should be evidence-led: only delete fields or services after a diagnostic
session proves the replacement surface answers the same question. Good next
targets are formatter consolidation, stale score-only UI references, and any
snapshot field that remains constantly zero across live preview, recording,
Flashback, and combined diagnostic-session artifacts.

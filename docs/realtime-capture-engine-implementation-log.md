# Real-Time Capture Engine Implementation Log

This log tracks small, reviewable landing points for
`docs/realtime-capture-engine-rewrite-plan.md`. Large runtime outputs belong
under `temp/`, not in git.

> **Project rename — 2026-05-02.** The project was renamed from `ElgatoCapture`
> to `Sussudio` (code identity) / `Simple Sussudio` (display name). Entries in
> this log dated before 2026-05-02 reference the old paths and namespaces
> (`ElgatoCapture/`, `tests/ElgatoCapture.*`, `tools/ecctl/`,
> `ElgatoCapture_Debug.log`). Read those as the historical equivalent of the
> current `Sussudio/`, `tests/Sussudio.*`, `tools/ssctl/`, `Sussudio_Debug.log`.
> The forward-looking plan in `docs/realtime-capture-engine-rewrite-plan.md`
> uses the new names. Full rename context: see the `2026-05-02 — Renamed
> project to Simple Sussudio` entry in `docs/experiment_log.md`.

## 2026-04-26 - Phase 0 baseline and guardrails

Starting point:

- Branch/worktree: `Flashback`
- Repo root: `C:/Users/crest/source/repos/ElgatoCapture-Flashback`
- Rollback checkpoint named by the rewrite plan: `8961079 Checkpoint experimental capture pipeline`
- Initial app process check: `ElgatoCapture` was not running

Baseline commands run:

```powershell
dotnet build ElgatoCapture\ElgatoCapture.csproj -c Debug -p:Platform=x64 --no-restore /nr:false /m:1 -p:UseSharedCompilation=false
dotnet build tools\ecctl\ecctl.csproj -c Debug --no-restore /nr:false /m:1 -p:UseSharedCompilation=false
dotnet build tools\McpServer\McpServer.csproj -c Debug --no-restore /nr:false /m:1 -p:UseSharedCompilation=false
dotnet build tools\NativeXuAudioProbe\NativeXuAudioProbe.csproj -c Debug -t:Rebuild --no-restore /nr:false /m:1 -p:UseSharedCompilation=false
dotnet run --project tests\ElgatoCapture.Tests\ElgatoCapture.Tests.csproj -c Debug -p:Platform=x64 --no-restore -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"
```

Results:

- App build passed with 14 existing warnings in Flashback playback, D3D renderer nullability, and window interop structs.
- `ecctl` build passed with 0 warnings.
- MCP server build passed with 0 warnings.
- `NativeXuAudioProbe` rebuild passed with 0 warnings.
- Runtime snapshot regression tests passed.
- Post-test log tail contained the expected unit-test synthetic HFR/no-audio lines and no new unexpected failure class.

Follow-up artifact:

- Added `tools/capture-rewrite-live-baseline.ps1` for Phase 0.5 live baseline runs.

## 2026-04-26 - Phase 1 engine map

Added `docs/realtime-capture-engine-map.md` as the first current-state map and
deletion-candidate note. It is intentionally short and should be updated as
ledger/recording-accounting phases replace current snapshot and telemetry
surfaces.

## 2026-04-26 - Phase 2 frame ledger skeleton

Added the first bounded frame-ledger surface:

- `FrameIdentity`, `FrameLedgerStage`, `FrameLedgerEventSnapshot`, and
  `FrameLedgerSummary` models.
- `FrameLedger`, a bounded in-memory ring with retained-event summaries.
- Initial `UnifiedVideoCapture` events for capture arrival, compressed MJPEG
  queue acceptance, decoded MJPEG strict release, preview enqueue,
  recording enqueue, and Flashback enqueue.
- `CaptureRuntimeSnapshot` and `AutomationSnapshot` fields for ledger capacity,
  total event count, retained-drop count, and recent events.
- Regression checks for retention behavior and snapshot contract exposure.

Validation:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools\capture-rewrite-live-baseline.ps1 -ValidateOnly
dotnet build ElgatoCapture\ElgatoCapture.csproj -c Debug -p:Platform=x64 --no-restore /nr:false /m:1 -p:UseSharedCompilation=false
dotnet run --project tests\ElgatoCapture.Tests\ElgatoCapture.Tests.csproj -c Debug -p:Platform=x64 --no-restore -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"
```

Results:

- Harness validation passed.
- App build passed with the existing warning family.
- Runtime snapshot regression tests passed, including the new frame-ledger tests.

## 2026-04-26 - Phase 3 recording accounting skeleton

Added the first post-stop recording integrity surface:

- `RecordingIntegritySummary`, exposed through runtime and automation snapshots.
- Recording-span source/accepted/drop accounting for Flashback and dedicated LibAv paths.
- Integrity counters for submitted, encoded, packet-written, encoder-drop, sequence-gap,
  queue-depth, queue-age, and backpressure telemetry.
- A recording-start counter baseline so Flashback-as-recording-backend does not report
  pre-recording encoder totals in active integrity snapshots.
- Shared CLI/MCP snapshot formatting for the integrity line.

Harness fixes landed while running the live baseline:

- PowerShell 5 compatibility for `ConvertFrom-Json`.
- Null-safe PresentMon stdout/stderr cleanup.
- Reliable child-process exit-code capture for PresentMon and dedicated LibAv verification.
- Compact baseline summaries now include the recording integrity fields.

Validation:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools\capture-rewrite-live-baseline.ps1 -ValidateOnly
dotnet build ElgatoCapture\ElgatoCapture.csproj -c Debug -p:Platform=x64 --no-restore /nr:false /m:1 -p:UseSharedCompilation=false
dotnet build tools\ecctl\ecctl.csproj -c Debug --no-restore /nr:false /m:1 -p:UseSharedCompilation=false
dotnet build tools\McpServer\McpServer.csproj -c Debug --no-restore /nr:false /m:1 -p:UseSharedCompilation=false
dotnet build tools\NativeXuAudioProbe\NativeXuAudioProbe.csproj -c Debug --no-restore -t:Rebuild /nr:false /m:1 -p:UseSharedCompilation=false
dotnet run --project tests\ElgatoCapture.Tests\ElgatoCapture.Tests.csproj -c Debug -p:Platform=x64 --no-restore -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"
```

Results:

- Harness validation passed.
- App, `ecctl`, MCP server, and `NativeXuAudioProbe` builds passed.
- Runtime snapshot regression tests passed, including recording integrity checks.
- Short live Flashback recording smoke passed verification:
  `temp/capture-rewrite-baselines/20260426_154626`.
- The live integrity report stayed `Incomplete` because Flashback reported source
  sequence gaps. That is now visible instead of silent and remains a Phase 5 strict-delivery
  hardening target.

## 2026-04-26 - Phase 4 audio and A/V integrity

Added audio integrity accounting to runtime and automation snapshots:

- WASAPI callback gap, discontinuity, timestamp-error, and aggregate glitch counters.
- Recording audio counters for enabled/active state, arrived frames, sink-boundary frames,
  encoded samples, sink drops, WASAPI discontinuities, timestamp errors, and callback gaps.
- Runtime and final recording A/V drift fields, with large drift treated as an integrity
  reason instead of display-only telemetry.
- Shared CLI/MCP formatting and compact live-baseline summary fields for audio integrity.

Validation:

```powershell
dotnet build ElgatoCapture\ElgatoCapture.csproj -c Debug -p:Platform=x64 --no-restore /nr:false /m:1 -p:UseSharedCompilation=false
dotnet build tools\ecctl\ecctl.csproj -c Debug --no-restore /nr:false /m:1 -p:UseSharedCompilation=false
dotnet run --project tests\ElgatoCapture.Tests\ElgatoCapture.Tests.csproj -c Debug -p:Platform=x64 --no-restore -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"
powershell -NoProfile -ExecutionPolicy Bypass -File tools\capture-rewrite-live-baseline.ps1 -AuthToken codex-local -RecordingSeconds 10 -SkipPresentMon -SkipPreviewOnly -SkipFlashbackRetention -SkipDedicatedLibAv -AppendImplementationLog
```

Results:

- App and `ecctl` builds passed; app build still has only the existing warning family.
- Runtime snapshot regression tests passed, including audio discontinuity and drift integrity checks.
- Live Flashback recording verification passed:
  `temp/capture-rewrite-baselines/20260426_160832`.
- Audio integrity was `Clean` in the live run with no audio drops, discontinuities,
  timestamp errors, or severe callback gaps. The recording remained `Incomplete` only
  because Flashback still reported source sequence gaps, now isolated for Phase 5.

## 2026-04-26 - Phase 5 strict MJPEG delivery proof

Hardened the strict recording evidence around the existing pooled MJPEG decode path:

- Flashback recording integrity now uses recording-scoped accepted source sequence gaps,
  avoiding false positives from pre-recording sink backlog.
- Recording audio integrity is sink-scoped for both Flashback and dedicated LibAv final
  summaries, while global WASAPI arrival telemetry remains available in runtime snapshots.
- The live-baseline harness now treats the dedicated LibAv verifier JSON `Result: PASS`
  as success if PowerShell does not return a process exit code.

Validation:

```powershell
dotnet build ElgatoCapture\ElgatoCapture.csproj -c Debug -p:Platform=x64 --no-restore /nr:false /m:1 -p:UseSharedCompilation=false
dotnet build tools\ecctl\ecctl.csproj -c Debug --no-restore /nr:false /m:1 -p:UseSharedCompilation=false
dotnet run --project tests\ElgatoCapture.Tests\ElgatoCapture.Tests.csproj -c Debug -p:Platform=x64 --no-restore -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"
powershell -NoProfile -ExecutionPolicy Bypass -File tools\capture-rewrite-live-baseline.ps1 -ValidateOnly
powershell -NoProfile -ExecutionPolicy Bypass -File tools\capture-rewrite-live-baseline.ps1 -AuthToken codex-local -RecordingSeconds 10 -SkipPresentMon -SkipPreviewOnly -SkipFlashbackRetention -SkipDedicatedLibAv -AppendImplementationLog
powershell -NoProfile -ExecutionPolicy Bypass -File tools\capture-rewrite-live-baseline.ps1 -AuthToken codex-local -RecordingSeconds 10 -SkipPresentMon -SkipPreviewOnly -SkipFlashbackRetention -SkipFlashbackRecording -AppendImplementationLog
git diff --check
```

Results:

- App and `ecctl` builds passed; app warnings remain the existing warning family.
- Runtime snapshot regression tests passed, including the Flashback scoped-sequence guard.
- `git diff --check` passed with only line-ending normalization warnings.
- Live Flashback recording completed cleanly with verified output and
  `RecordingIntegrityStatus=Complete`:
  `temp/capture-rewrite-baselines/20260426_161334`.
- Live dedicated LibAv recording verified PASS and app integrity completed cleanly with
  zero sequence gaps, zero queue drops, and clean audio:
  `temp/capture-rewrite-baselines/20260426_161711`.

## 2026-04-26 - Phase 6 preview scheduler ownership

Added correlation-ready preview ownership telemetry on top of the existing adaptive
MJPEG preview scheduler:

- `MjpegPreviewJitterBuffer` now assigns a scheduler preview-present id to each
  selected frame, records the selected decoded source sequence and source-to-scheduler
  latency, and tracks the latest preview drop sequence/reason.
- `IPreviewFrameSink`/`D3D11PreviewRenderer` now carry scheduler ownership metadata
  through leased and copied raw-frame paths without adding another NV12 copy.
- D3D preview diagnostics expose last submitted/rendered preview-present ids, source
  sequences, render QPC ticks, scheduler-to-present latency, and last renderer drop reason.
- Automation snapshots and `ecctl`/MCP formatting surface the scheduler and renderer
  ownership fields alongside existing swap-chain address and render CPU timing.

Validation:

```powershell
dotnet build ElgatoCapture\ElgatoCapture.csproj -c Debug -p:Platform=x64 --no-restore /nr:false /m:1 -p:UseSharedCompilation=false
dotnet build tools\ecctl\ecctl.csproj -c Debug --no-restore /nr:false /m:1 -p:UseSharedCompilation=false
dotnet build tools\McpServer\McpServer.csproj -c Debug --no-restore /nr:false /m:1 -p:UseSharedCompilation=false
dotnet build tools\NativeXuAudioProbe\NativeXuAudioProbe.csproj -c Debug --no-restore -t:Rebuild /nr:false /m:1 -p:UseSharedCompilation=false
dotnet run --project tests\ElgatoCapture.Tests\ElgatoCapture.Tests.csproj -c Debug -p:Platform=x64 --no-restore -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"
powershell -NoProfile -ExecutionPolicy Bypass -File tools\capture-rewrite-live-baseline.ps1 -AuthToken codex-local -PreviewSeconds 10 -SkipPresentMon -SkipFlashbackRetention -SkipFlashbackRecording -SkipDedicatedLibAv -AppendImplementationLog
```

Results:

- App, `ecctl`, MCP server, and NativeXu tool builds passed.
- Runtime snapshot regression tests passed, including preview scheduler deadline tests
  and the D3D ownership diagnostics contract.
- Live 4K120 preview run populated matching scheduler and renderer ownership:
  scheduler present/source `1375/1403`, rendered present/source `1375/1403`,
  scheduler-to-present latency `1.6011ms`, source-to-scheduler latency `54.4012ms`.
  Output: `temp/capture-rewrite-baselines/20260426_163053`.

## 2026-04-26 - Phase 7 PresentMon correlation

Connected app-level preview presents to exact-swap-chain PresentMon evidence:

- D3D preview ownership metrics now include UTC Unix millisecond timestamps for last
  submitted/rendered/dropped frames, alongside present id, source sequence, and QPC.
- `PresentMonProbe` parses `CPUStartTime`, exposes an app-present correlation object,
  classifies displayed vs superseded/not-displayed outcomes, and refuses correlations
  outside a 50ms window.
- `ecctl presentmon` and MCP PresentMon tools accept optional app present id/source
  sequence/UTC timestamp arguments and auto-fill current preview swap-chain/present data
  when available.
- The live-baseline harness now correlates snapshot-sampled app presents to PresentMon
  rows for the exact expected swap-chain address and writes the correlation into the
  scenario summary. Swap-chain misses are reported as `SwapChainMismatch`.

Validation:

```powershell
dotnet build ElgatoCapture\ElgatoCapture.csproj -c Debug -p:Platform=x64 --no-restore /nr:false /m:1 -p:UseSharedCompilation=false
dotnet build tools\ecctl\ecctl.csproj -c Debug --no-restore /nr:false /m:1 -p:UseSharedCompilation=false
dotnet build tools\McpServer\McpServer.csproj -c Debug --no-restore /nr:false /m:1 -p:UseSharedCompilation=false
dotnet build tools\NativeXuAudioProbe\NativeXuAudioProbe.csproj -c Debug --no-restore -t:Rebuild /nr:false /m:1 -p:UseSharedCompilation=false
dotnet run --project tests\ElgatoCapture.Tests\ElgatoCapture.Tests.csproj -c Debug -p:Platform=x64 --no-restore -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"
powershell -NoProfile -ExecutionPolicy Bypass -File tools\capture-rewrite-live-baseline.ps1 -ValidateOnly
powershell -NoProfile -ExecutionPolicy Bypass -File tools\capture-rewrite-live-baseline.ps1 -AuthToken codex-local -PreviewSeconds 7 -PresentMonSeconds 5 -SkipFlashbackRetention -SkipFlashbackRecording -SkipDedicatedLibAv -AppendImplementationLog
```

Results:

- App, `ecctl`, MCP server, and NativeXu tool builds passed.
- Runtime snapshot regression tests passed, including the PresentMon app-correlation
  parser case.
- Live PresentMon run matched exact swap-chain `0x1C900162C40`, selected 590/592 rows,
  and correlated app present/source `770/796` to PresentMon CPUStartTime with `2.995ms`
  delta and `Displayed` outcome. Output:
  `temp/capture-rewrite-baselines/20260426_164311`.

## 2026-04-26 15:24 - Phase 0.5 live baseline run

- Output directory: C:\Users\crest\source\repos\ElgatoCapture-Flashback\temp\capture-rewrite-baselines\20260426_151540
- Summary report: C:\Users\crest\source\repos\ElgatoCapture-Flashback\temp\capture-rewrite-baselines\20260426_151540\summary.md
- App process: 81344
- Scenario preview-only: captured
- Scenario flashback-retention: captured
- Scenario flashback-recording: captured
- Scenario dedicated-libav-recording: captured

## 2026-04-26 15:38 - Phase 0.5 live baseline run

- Output directory: C:\Users\crest\source\repos\ElgatoCapture-Flashback\temp\capture-rewrite-baselines\20260426_153702
- Summary report: C:\Users\crest\source\repos\ElgatoCapture-Flashback\temp\capture-rewrite-baselines\20260426_153702\summary.md
- App process: 10500
- Scenario flashback-recording: captured

## 2026-04-26 15:47 - Phase 0.5 live baseline run

- Output directory: C:\Users\crest\source\repos\ElgatoCapture-Flashback\temp\capture-rewrite-baselines\20260426_154626
- Summary report: C:\Users\crest\source\repos\ElgatoCapture-Flashback\temp\capture-rewrite-baselines\20260426_154626\summary.md
- App process: 71184
- Scenario flashback-recording: captured

## 2026-04-26 16:06 - Phase 0.5 live baseline run

- Output directory: C:\Users\crest\source\repos\ElgatoCapture-Flashback\temp\capture-rewrite-baselines\20260426_160541
- Summary report: C:\Users\crest\source\repos\ElgatoCapture-Flashback\temp\capture-rewrite-baselines\20260426_160541\summary.md
- App process: 151352
- Scenario flashback-recording: captured

## 2026-04-26 16:09 - Phase 0.5 live baseline run

- Output directory: C:\Users\crest\source\repos\ElgatoCapture-Flashback\temp\capture-rewrite-baselines\20260426_160832
- Summary report: C:\Users\crest\source\repos\ElgatoCapture-Flashback\temp\capture-rewrite-baselines\20260426_160832\summary.md
- App process: 67620
- Scenario flashback-recording: captured

## 2026-04-26 16:14 - Phase 0.5 live baseline run

- Output directory: C:\Users\crest\source\repos\ElgatoCapture-Flashback\temp\capture-rewrite-baselines\20260426_161334
- Summary report: C:\Users\crest\source\repos\ElgatoCapture-Flashback\temp\capture-rewrite-baselines\20260426_161334\summary.md
- App process: 143712
- Scenario flashback-recording: captured

## 2026-04-26 16:15 - Phase 0.5 live baseline run

- Output directory: C:\Users\crest\source\repos\ElgatoCapture-Flashback\temp\capture-rewrite-baselines\20260426_161451
- Summary report: C:\Users\crest\source\repos\ElgatoCapture-Flashback\temp\capture-rewrite-baselines\20260426_161451\summary.md
- App process: 28856
- Scenario dedicated-libav-recording: captured

## 2026-04-26 16:18 - Phase 0.5 live baseline run

- Output directory: C:\Users\crest\source\repos\ElgatoCapture-Flashback\temp\capture-rewrite-baselines\20260426_161711
- Summary report: C:\Users\crest\source\repos\ElgatoCapture-Flashback\temp\capture-rewrite-baselines\20260426_161711\summary.md
- App process: 12540
- Scenario dedicated-libav-recording: captured

## 2026-04-26 16:31 - Phase 0.5 live baseline run

- Output directory: C:\Users\crest\source\repos\ElgatoCapture-Flashback\temp\capture-rewrite-baselines\20260426_163053
- Summary report: C:\Users\crest\source\repos\ElgatoCapture-Flashback\temp\capture-rewrite-baselines\20260426_163053\summary.md
- App process: 20120
- Scenario preview-only: captured

## 2026-04-26 16:40 - Phase 0.5 live baseline run

- Output directory: C:\Users\crest\source\repos\ElgatoCapture-Flashback\temp\capture-rewrite-baselines\20260426_164036
- Summary report: C:\Users\crest\source\repos\ElgatoCapture-Flashback\temp\capture-rewrite-baselines\20260426_164036\summary.md
- App process: 130400
- Scenario preview-only: captured

## 2026-04-26 16:43 - Phase 0.5 live baseline run

- Output directory: C:\Users\crest\source\repos\ElgatoCapture-Flashback\temp\capture-rewrite-baselines\20260426_164311
- Summary report: C:\Users\crest\source\repos\ElgatoCapture-Flashback\temp\capture-rewrite-baselines\20260426_164311\summary.md
- App process: 50780
- Scenario preview-only: captured

## 2026-04-26 17:02 - Phase 8 diagnostics UI and summary surface

- Automation snapshots now include value-first diagnostic fields:
  `DiagnosticHealthStatus`, `DiagnosticLikelyStage`, `DiagnosticSummary`,
  `DiagnosticEvidence`, and per-lane source/decode/preview/render/present/
  recording/audio summaries.
- The diagnostic stage is derived from real capture cadence, MJPEG decode,
  preview scheduler, D3D renderer, present cadence, recording integrity, and
  audio integrity telemetry. The legacy performance score remains available but
  is demoted in formatter output.
- The detached stats window now shows a top Health section with status, likely
  stage, and evidence before lower-level timing and telemetry details.
- Shared CLI/MCP formatters now print a Diagnostics section before the legacy
  score and include all frame-lane summaries.

Validation:

- `dotnet build ElgatoCapture\ElgatoCapture.csproj -c Debug -p:Platform=x64 --no-restore /nr:false /m:1 -p:UseSharedCompilation=false`
- `dotnet build tools\ecctl\ecctl.csproj -c Debug --no-restore /nr:false /m:1 -p:UseSharedCompilation=false`
- `dotnet build tools\McpServer\McpServer.csproj -c Debug --no-restore /nr:false /m:1 -p:UseSharedCompilation=false`
- `dotnet build tools\NativeXuAudioProbe\NativeXuAudioProbe.csproj -c Debug --no-restore -t:Rebuild /nr:false /m:1 -p:UseSharedCompilation=false`
- `powershell -NoProfile -ExecutionPolicy Bypass -File tools\capture-rewrite-live-baseline.ps1 -ValidateOnly`
- `dotnet run --project tests\ElgatoCapture.Tests\ElgatoCapture.Tests.csproj -c Debug -p:Platform=x64 --no-restore -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"`

## 2026-04-26 17:15 - Phase 9 diagnostic-session runner and live soak smoke

- Added shared `DiagnosticSessionRunner` for timed diagnostic sessions.
- Added `ecctl diagnostic-session` / `ecctl session` with scenarios:
  `observe`, `preview-only`, `recording-only`, `flashback`, and `combined`.
- Added MCP `run_diagnostic_session` with the same scenario model.
- Each run writes `summary.json`, `samples.json`, `frame-ledger.json`, and
  `timeline.json`; recording scenarios automatically run strict verification.
- Optional PresentMon capture resolves the current preview swap-chain and app
  present/source correlation data from live snapshots.

Offline validation:

- `dotnet build tools\ecctl\ecctl.csproj -c Debug --no-restore /nr:false /m:1 -p:UseSharedCompilation=false`
- `dotnet build tools\McpServer\McpServer.csproj -c Debug --no-restore /nr:false /m:1 -p:UseSharedCompilation=false`
- `dotnet build tools\NativeXuAudioProbe\NativeXuAudioProbe.csproj -c Debug --no-restore -t:Rebuild /nr:false /m:1 -p:UseSharedCompilation=false`
- `powershell -NoProfile -ExecutionPolicy Bypass -File tools\capture-rewrite-live-baseline.ps1 -ValidateOnly`
- `dotnet run --project tests\ElgatoCapture.Tests\ElgatoCapture.Tests.csproj -c Debug -p:Platform=x64 --no-restore -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"`

Live 4K120 smoke:

- Preview + PresentMon:
  `temp\diagnostic-sessions\phase9-preview-live`, PASS, 11 samples, PresentMon
  selected 596 rows from swap chain `0x1F6829D36F0`.
- Recording-only:
  `temp\diagnostic-sessions\phase9-recording-live`, PASS, strict verification
  passed.
- Flashback:
  `temp\diagnostic-sessions\phase9-flashback-live`, PASS.
- Combined Flashback + recording:
  `temp\diagnostic-sessions\phase9-combined-live`, PASS, strict verification
  passed.

## 2026-04-26 17:34 - Phase 10 cleanup and documentation validation

- Updated `docs/automation.md` so the diagnostic health/stage/evidence fields
  are the preferred automation surface and documented the new
  `diagnostic-session` CLI/MCP workflow.
- Updated `docs/realtime-capture-engine-map.md` to stop describing the frame
  ledger, recording integrity summary, and PresentMon correlation as future
  work. The map now lists remaining cleanup targets as evidence-led follow-up
  work rather than speculative deletion.
- Closed the live app process after Phase 9 smoke runs.

Validation:

- `git diff --check` passed; only existing line-ending normalization warnings
  were reported.
- `dotnet build ElgatoCapture\ElgatoCapture.csproj -c Debug -p:Platform=x64 --no-restore /nr:false /m:1 -p:UseSharedCompilation=false`
- `dotnet build tools\ecctl\ecctl.csproj -c Debug --no-restore /nr:false /m:1 -p:UseSharedCompilation=false`
- `dotnet build tools\McpServer\McpServer.csproj -c Debug --no-restore /nr:false /m:1 -p:UseSharedCompilation=false`
- `dotnet build tools\NativeXuAudioProbe\NativeXuAudioProbe.csproj -c Debug --no-restore -t:Rebuild /nr:false /m:1 -p:UseSharedCompilation=false`
- `powershell -NoProfile -ExecutionPolicy Bypass -File tools\capture-rewrite-live-baseline.ps1 -ValidateOnly`
- `dotnet run --project tests\ElgatoCapture.Tests\ElgatoCapture.Tests.csproj -c Debug -p:Platform=x64 --no-restore -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"`

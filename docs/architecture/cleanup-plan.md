# Architecture Cleanup Plan

Last reviewed: 2026-05-12.

## Objective

Make the repo feel intentionally laid out and safe to change without moving
capture, preview, recording, Flashback, or automation behavior by vibes alone.
Performance and runtime semantics stay primary; file layout changes must earn
their keep with smaller ownership boundaries and passing checks.

## Completed Slices

Automation contracts have been extracted into `Sussudio.Automation.Contracts/`.
This removes the old linked-source arrangement where app and tools compiled
protocol/catalog files from `tools/Common`.

Changed ownership:

- `AutomationCommandKind.cs`
- `AutomationCommandCatalog.cs`
- `AutomationPipeProtocol.cs`
- `AutomationPipeSecurityPolicy.cs`

Diagnostic session scenario names and scenario-level metadata now live in
`tools/Common/DiagnosticSessionScenarios.cs`; the runner still owns execution
flow and summary writing.

Fullscreen transition mechanics now live in
`Sussudio/Controllers/FullScreenController.cs`. `MainWindow.FullScreen.cs`
remains the XAML event adapter and Flashback keyboard/scrub bridge.

Automation whole-window screenshot capture now lives in
`Sussudio/Controllers/WindowScreenshotController.cs`. `MainWindow.Screenshot.cs`
is only the automation adapter.

Audio and microphone meter rendering now lives in
`Sussudio/Controllers/AudioMeterController.cs`. The broader control-bar binding
and microphone-row animation code remains in `MainWindow.Bindings.cs`.

Capture session transition legality now lives in
`Sussudio/Models/Capture/CaptureSessionTransitionPolicy.cs`. `CaptureService`
uses it before entering a transition and delegates steady-state resolution to
the same pure policy; resource ownership has not moved in this slice.

Stats dock and frame-time overlay lifecycle now live in
`Sussudio/Controllers/StatsOverlayController.cs`. `MainWindow.StatsOverlay.cs`
still renders metric values and dynamic diagnostic rows, but polling, visibility
state, and dock animations are out of the shell fields.

Diagnostic session DTOs now live in
`tools/Common/DiagnosticSessionModels.cs`. `DiagnosticSessionRunner.cs` still
owns orchestration and scenario execution, but the public options/result/sample
contracts are separated from runner behavior.

Diagnostic-session result text now lives in
`tools/Common/DiagnosticSessionResultFormatter.cs`. The runner keeps
`Format(...)` as a compatibility wrapper so existing ssctl and MCP callers do
not need to know about the formatter owner.

Shared diagnostic-session text helpers now live in
`tools/Common/DiagnosticSessionText.cs`. Keep cross-cutting string helpers
there instead of reintroducing private duplicates in the runner, formatter, or
validation policy files.

Diagnostic-session pipe retry/error classification now lives in
`tools/Common/DiagnosticSessionPipeRetryPolicy.cs`, keeping access-denied as a
permanent failure and connect failed/timeout responses retryable.

Diagnostic-session JSON artifact helpers now live in
`tools/Common/DiagnosticSessionJsonArtifacts.cs`. The runner still owns the
session lifecycle, but JSON writing, frame-ledger extraction, and snapshot /
verification response extraction have a smaller home.

Diagnostic-session cleanup restore validation now lives in
`tools/Common/DiagnosticSessionCleanupPolicy.cs`. It owns warnings for preview,
Flashback, and playback state that remain active after the runner attempts
cleanup.

Diagnostic-session sampling now lives in
`tools/Common/DiagnosticSessionSampler.cs`. Keep the sample append before the
optional checkpoint callback so checkpoint failures cannot orphan an unseen
sample.

Diagnostic-session metric projection now lives in
`tools/Common/DiagnosticSessionMetrics.cs`. It owns snapshot-only projections
for source cadence, preview cadence, visual cadence, D3D slow-frame summaries,
playback command health, and reset-aware counter deltas.

Diagnostic-session Flashback export helpers now live in
`tools/Common/DiagnosticSessionFlashbackExports.cs`. They own strict export
verification payload construction, rotated-export segment-count parsing,
range-selection cleanup, and the range export audio-switch companion command
while the runner keeps scenario command sequencing.

Diagnostic-session Flashback metric projection now lives in
`tools/Common/DiagnosticSessionFlashbackMetrics.cs`. It owns snapshot-only
recording, playback, and export metric projection while the runner retains
scenario control and validation warning policy.

Diagnostic-session Flashback segment handling now lives in
`tools/Common/DiagnosticSessionFlashbackSegments.cs`. It owns segment DTOs,
`FlashbackGetSegments` parsing, completed-segment waits, and playable-boundary
headroom waits while the runner keeps scenario command sequencing.

Diagnostic-session Flashback snapshot waits now live in
`tools/Common/DiagnosticSessionFlashbackWaits.cs`. They own read-only polling
loops for playback state, playback warmup, preview active, Flashback active,
and recording-ready checks while the runner keeps scenario command sequencing.

Diagnostic-session Flashback stress orchestration now lives in
`tools/Common/DiagnosticSessionFlashbackStressScenario.cs`. It owns the stress
command sequence, playback-command thresholds, warm-playback budget, and
audio-master fallback classification while the runner only starts the scenario
task.

Diagnostic-session Flashback validation now lives in
`tools/Common/DiagnosticSessionFlashbackValidation.cs`. It owns recording,
playback, and preview-scheduler warning thresholds over already projected
metrics while the runner retains scenario orchestration.

Diagnostic-session health policy now lives in
`tools/Common/DiagnosticSessionHealthPolicy.cs`. It owns health severity,
Flashback warmup filtering, sparse cadence tolerances, and tolerated warning
classification while the runner still owns scenario execution and warning
emission.

Remaining `tools/Common` ownership:

- `AutomationPipeClient.cs`
- `DiagnosticSessionCleanupPolicy.cs`
- `DiagnosticSessionFlashbackExports.cs`
- `DiagnosticSessionFlashbackMetrics.cs`
- `DiagnosticSessionFlashbackSegments.cs`
- `DiagnosticSessionFlashbackStressScenario.cs`
- `DiagnosticSessionFlashbackWaits.cs`
- `DiagnosticSessionFlashbackValidation.cs`
- `DiagnosticSessionHealthPolicy.cs`
- `DiagnosticSessionJsonArtifacts.cs`
- `DiagnosticSessionMetrics.cs`
- `DiagnosticSessionModels.cs`
- `DiagnosticSessionPipeRetryPolicy.cs`
- `DiagnosticSessionResultFormatter.cs`
- `DiagnosticSessionSampler.cs`
- `DiagnosticSessionText.cs`
- `DiagnosticSessionRunner.cs`
- `AutomationSnapshotFormatter.cs`
- `AutomationResponseState.cs`
- `JsonOptions.cs`
- `PresentMonProbe.cs`

## Next Slices

1. Continue splitting diagnostic-session runner by scenario family.

   `tools/Common/DiagnosticSessionRunner.cs` is still large. Scenario catalog
   ownership is extracted; next, move preview, recording, Flashback, and cleanup
   scenarios behind small runner classes. Keep JSON summary shape unchanged.

2. Reduce custom regression harness size.

   `tests/Sussudio.Tests/Program.cs` should keep the legacy runner entry point,
   but new checks should land in focused xUnit files. Move low-risk contract
   tests first.

3. Continue converting MainWindow partial concerns into controllers.

   `FullScreen`, automation `Screenshot`, and audio meter rendering are
   extracted. `StatsOverlay` lifecycle is extracted; next UI candidates are
   preview startup, Flashback timeline UI, and the remaining stats row/snapshot
   projection. Keep XAML bindings stable.

4. Move MainViewModel feature state behind a facade.

   Preserve the root `MainViewModel` public surface while introducing feature
   view models or adapters for capture selection, recording, audio, Flashback,
   diagnostics, and automation.

5. Extract capture resource owners behind the transition policy.

   The policy is now the legality/steady-state owner. The next deeper capture
   slices should keep it authoritative while introducing smaller owners for
   audio graph, recording controller, Flashback backend resources, and video
   pipeline lifetime.

## Guardrails

- Preserve public automation command names and numeric IDs.
- Preserve manifest revision rules in `AutomationCommandKind`.
- Preserve XAML binding names until a focused binding migration changes them.
- Preserve Flashback disable lockout behavior.
- Preserve preview/recording no-restart semantics unless a test proves the
  transition intentionally restarts.
- Run `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore` after each
  structural slice.
- Run the console harness when source ownership, automation, capture, recording,
  or Flashback contracts move.

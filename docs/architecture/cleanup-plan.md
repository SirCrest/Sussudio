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

Remaining `tools/Common` ownership:

- `AutomationPipeClient.cs`
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

3. Create capture transition policy.

   Add a pure state/transition policy used by `CaptureSessionCoordinator` and
   `CaptureService` before moving resource lifetimes. The first pass should
   validate legal transitions only; it should not touch Media Foundation,
   D3D11, WASAPI, FFmpeg, or Flashback resources.

4. Continue converting MainWindow partial concerns into controllers.

   `FullScreen` and automation `Screenshot` are extracted. Next candidates are
   `StatsOverlay` and `AudioMeter` because they are easier to isolate than
   preview startup and close/recording lifecycle. Keep XAML bindings stable.

5. Move MainViewModel feature state behind a facade.

   Preserve the root `MainViewModel` public surface while introducing feature
   view models or adapters for capture selection, recording, audio, Flashback,
   diagnostics, and automation.

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

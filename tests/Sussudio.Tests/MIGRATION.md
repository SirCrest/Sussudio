# Sussudio.Tests Migration Plan

The test project runs a large legacy check catalog through a hand-rolled
`Program.cs` runner that loads `Sussudio.dll` via reflection. Cluster
`test-framework-migration` opens the dual-stack path: keep the legacy runner,
add xUnit alongside, and port incrementally.

## What's in place

- xUnit 2.9 + `xunit.runner.visualstudio` + `Microsoft.NET.Test.Sdk` referenced
  in `Sussudio.Tests.csproj`. `OutputType=Exe` stays so the legacy runner keeps
  working via `dotnet run`; `dotnet test` discovers `[Fact]`/`[Theory]` members.
- `[assembly: InternalsVisibleTo("Sussudio.Tests")]` on `Sussudio` and
  `tools/ssctl`. The seam is open even though tests still resolve types via
  reflection — see the targeting note below for why direct references stay
  off the table for now.
- `XUnit.RecordingContractsTests.cs` is the worked example — port pattern
  uses `Assembly.LoadFrom` against the staged `Sussudio.dll` (the legacy
  runner already does the same), and asserts via xUnit primitives instead
  of the hand-rolled `AssertContains/AssertEqual` helpers. It also owns the
  ported RecordingStats value-contract check.
- `XUnit.AutomationContractsTests.cs` owns the former legacy app-surface
  checks, including the bool/visibility converter wrapper class.
- `StatsOverlay.Lifecycle.Tests.cs` now owns xUnit source-contract checks for
  stats overlay lifecycle and section chrome wiring. The remaining stats
  presentation checks still migrate incrementally from the legacy catalog.
- `XUnit.StatsPresentation.Formatting.Tests.cs` owns the former legacy
  detached-window, encoder formatting, expected-display-repeat, compact preview
  summary, frame-time range, and frame-time geometry stats presentation behavior
  checks.
- `XUnit.StatsHardwareRowsTests.cs` owns the former legacy hardware decode/GPU
  row formatting behavior checks and hardware-row input sampling policy checks.
- `XUnit.CoreRuntimeContractsTests.cs` owns the former legacy HdrOutputPolicy
  behavior, HDR output environment-switch, and disabled telemetry-provider
  checks.
- `XUnit.CoreRuntimeContractsTests.cs` owns focused runtime helper behavior
  checks.
- `XUnit.SnapshotModelsTests.cs` and its `SnapshotModels.*` partials own the
  former legacy CaptureDiagnosticsSnapshot, CaptureHealthSnapshot,
  SourceSignalTelemetrySnapshot, SourceTelemetryDetailEntry, and source
  telemetry automation projection contract checks, plus AutomationSnapshot CPU
  MJPEG, MJPEG preview, preview diagnostics, capture-command, recording,
  Flashback recording/playback/export, visual cadence, and AutomationOptions DTO
  shape checks.
  `SnapshotModels.Automation.Tests.cs` owns the focused AutomationSnapshot and
  AutomationOptions metric-shape checks plus shared AutomationSnapshot property
  assertions.
- `XUnit.SmallContractsTests.cs` owns the former legacy audio input, audio
  level event, capture device, and automation window action small contracts.
- `XUnit.CaptureConfigurationModelsTests.cs` owns shared reflection helpers,
  capture mode option display metadata, option-builder behavior, capture
  settings defaults, output path/file naming, bitrate policy, MJPEG HFR policy,
  MediaFormat equality/hash-code behavior, recording selection policy, encoder
  support, and recording pipeline option xUnit contract checks.
- `XUnit.RecordingContractsTests.cs` owns recording contract DTO checks plus
  the former legacy recording pipeline, recording-model/Flashback buffer, and
  core-runtime recording xUnit execution surfaces. The public wrapper classes
  remain separate inside this file so existing test identities stay stable.
- `XUnit.CoreRuntimeContractsTests.cs` owns the former legacy core runtime
  subgroup: runtime telemetry, capture-service snapshot, NativeXu, frame ledger,
  recording-integrity, and basic app contract checks.
- `RecordingArtifactManager.Tests.cs` owns the former legacy temp artifact
  finalization and rollback behavior checks for recording output cleanup.
- `XUnit.MjpegPipelineContractsTests.cs` owns the former legacy CPU MJPEG
  pipeline, timing metric, stopwatch timeout, software decoder shape,
  pooled-frame lease/fan-out, preview jitter, cadence, and queued lease-release
  checks.
- `XUnit.FlashbackModelsTests.cs` owns the former legacy Flashback buffer option
  sizing, session, playback-state, export progress, export segment, and export
  request model contract checks plus the shared reflection/nullability assertion
  helpers for that suite.
- `XUnit.FlashbackContractsTests.cs` owns the former legacy Flashback encoder
  sink, playback, decoder, and exporter xUnit wrapper classes while preserving
  their frame-rate, codec, queue, force-rotate, startup, command-queue,
  source-shape, cadence, frame-buffer, state/lifetime, timestamp, audio,
  request validation, segment, cancellation, output path/finalization, and
  source-ownership checks.
- `Flashback.Playback.Markers.Tests.cs` owns the former legacy Flashback
  playback in/out marker API, normalization, disposal, and marker clamp checks.
- `StatsDockPresentation.Tests.cs` and `StatsPresentation.Ownership.Tests.cs`
  own the former legacy stats dock, row chrome, builder ownership, and HDMI
  source telemetry panel checks.
- `MainWindowUiContract.StatsSnapshot.Tests.cs` owns the former legacy stats
  snapshot construction and health/renderer metric projection checks.
- `XUnit.ToolFormatterContractsTests.cs` owns focused tool formatter contract
  checks.
- `XUnit.AutomationContractsTests.cs` owns the former legacy automation xUnit
  execution groups: catalog, manifest, path-policy, reliability-gates,
  app-surface, ViewModel/Flashback UI, dispatcher, capture/Flashback routing,
  diagnostics snapshot projection, and diagnostics-loop polling checks. The
  public wrapper classes remain separate inside this file so existing test
  identities stay stable while the execution surface is easier to scan.
- `XUnit.ToolContractsTests.cs` owns the former legacy PresentMon parser, ssctl
  pipe transport, KS audio-node, EGAVDS probe, shared automation snapshot
  formatter core, Flashback, Preview D3D, source ownership, ssctl formatter,
  NVML snapshot, CaptureSessionSnapshot default-state, and RTK I2C
  unsafe-native-path contract checks. The public wrapper classes remain
  separate inside this file so existing test identities stay stable while the
  backing PresentMon, pipe transport, KS audio-node, and EGAVDS probe checks
  live together in `ToolProbeContracts.Tests.cs`.
- `XUnit.ToolContractsTests.cs` owns the former legacy ssctl
  command-handler routing, source ownership, and catalog-backed help contract
  checks.
- `Program.cs` owns the legacy runner entry point and the xUnit bootstrap helper
  that initializes the staged app assembly before wrappers call legacy reflection
  helpers.
- `XUnit.McpContractsTests.cs` owns the former legacy MCP tool execution
  groups: window/preview wait, screenshot, frame-capture, window action,
  preview-toggle/probe, PresentMon correlation, performance timeline,
  frame-pacing verdict, general tool surface, command-routing, host/pipe,
  verification, Flashback tool, and diagnostic-session tool entry checks. The
  public wrapper classes remain separate inside this file so existing test
  identities stay stable while the execution surface is easier to scan.
- `XUnit.McpContractsTests.cs` also owns the former legacy
  diagnostic-session xUnit execution bands: infrastructure, result surface,
  command/run context, scenario execution, Flashback scenarios/helpers/metrics,
  core sampler/metric/health checks, and runner behavior checks. The public
  wrapper classes remain separate inside this file so existing test identities
  stay stable while the execution surface is easier to scan.
- `Program.cs` keeps the legacy runner entry point, but the
  diagnostic-session catalog has no remaining legacy registrations.
- `XUnit.PresentationPreviewStartupContractsTests.cs` owns the former legacy
  presentation-preview harness registration guard.
- `XUnit.PresentationPreviewMainViewModelContractsTests.cs` owns the former
  legacy presentation-preview MainViewModel xUnit execution groups: initial
  recording transition failure propagation, audio controls and monitoring,
  output path and disk-space presentation, source telemetry, dependency
  composition, runtime automation/capture settings, preview lifecycle, and
  audio ramp trace telemetry. The public wrapper classes remain separate inside
  this file so existing test identities stay stable while the execution surface
  is easier to scan.
- `XUnit.PresentationPreviewMainWindowContractsTests.cs` owns the former legacy
  presentation-preview MainWindow xUnit execution groups: window lifecycle,
  launch/startup, preview screenshot, shell chrome, visual shell, recording
  controls, audio controls, responsive layout, capture selection, resolution
  selection, capture runtime guardrails, MainWindow initial, preview runtime
  shell/policy, capture option, and output path checks. The public wrapper
  classes remain separate inside this file so existing test identities stay
  stable while the execution surface is easier to scan.
- `XUnit.PresentationPreviewMainViewModelContractsTests.cs` also owns the
  former legacy presentation-preview frame-rate selection group:
  `ShowAllCaptureOptions`, source-filter, auto-selection, and timing-policy
  behavior/ownership checks.
- `XUnit.PresentationPreviewMainViewModelContractsTests.cs` also owns
  the former legacy presentation-preview late device-format probe retarget group:
  retarget policy ownership, decision behavior, and UI-side retarget application
  checks.
- `XUnit.PresentationPreviewMainViewModelContractsTests.cs` also owns the
  former legacy presentation-preview capture selection-policy group:
  mode-selection state, capture format selection, and recording settings
  selection ownership/behavior checks.
- `XUnit.PresentationPreviewStartupContractsTests.cs` owns the former legacy
  presentation-preview preview-startup groups: ownership, controller behavior,
  signal/failure text, startup ordering, capture preview-lifecycle, and
  Flashback buffer startup/recovery checks.
- Preview-startup ordering checks also live in
  `XUnit.PresentationPreviewStartupContractsTests.cs`.
- `AppSurface.Tests.cs` and `XUnit.AutomationContractsTests.cs` own the former
  legacy project-file build/publish policy implementation and xUnit execution
  check after its removal from the presentation-preview capture catalog.
- `XUnit.PresentationPreviewD3DContractsTests.cs` owns the former legacy
  presentation-preview D3D registration groups: pacing, geometry/screenshot,
  present cadence, device-lost, diagnostics, contracts/metrics ownership,
  runtime capture, render setup/resource, and render pipeline checks. The legacy
  D3D catalog hook was removed after the final group moved to xUnit.
- `ArchitectureDocs.ReferenceIntegrity.Tests.cs` owns the former legacy
  AGENT_MAP ownership, path-reference, test-project shape guard,
  architecture-doc reference drift, and migration-inventory guard checks.
- Additional focused `[Fact]`/`[Theory]` files such as
  `XUnit.AutomationContractsTests.cs`,
  `AutomationToolContracts.ProtocolXunit.Tests.cs` (automation client timeout
  policy, advanced command-map alignment, and pipe/tool protocol contracts),
  `RuntimeContracts.Tests.cs`, `MainWindow.ControllerOwnership.Layout.Tests.cs`,
  `CaptureService.HealthSnapshots.AssemblyAndSamplerOwnership.Tests.cs`,
  `CaptureService.HealthSnapshots.FlashbackOwnership.Tests.cs`, and
  `CaptureService.HealthSnapshots.RecordingAndSourceTelemetryOwnership.Tests.cs`
  already run through `dotnet test`.

## Targeting reality

The test project targets **net8.0** while `Sussudio.csproj` targets
**net8.0-windows10.0.19041.0**. Adding a `<ProjectReference>` from tests to
Sussudio would force a Windows target on the test rig and pull WinUI/Windows
SDK deps into VSTest discovery — that's a much larger change than the
xUnit migration warrants. Until Sussudio.Models/Sussudio.Services.Contracts
are extracted into a true `net8.0` (or netstandard) library, ported xUnit
tests will keep using `Assembly.LoadFrom` to resolve Sussudio types. The
`InternalsVisibleTo` seam still pays off because tests can call internal
APIs by reflection without `BindingFlags.NonPublic` gymnastics on private
backing fields.

## How to run

- Legacy runner:
  `dotnet exec tests\Sussudio.Tests\bin\Debug\net8.0\Sussudio.Tests.dll "<path-to-Sussudio.dll>"`
- xUnit cases:
  `dotnet test tests/Sussudio.Tests/Sussudio.Tests.csproj --no-restore`

Both must stay green during the migration. The legacy check catalog is
currently empty, but the executable runner still provides the offline
`dotnet exec` validation shim used by architecture cleanup slices.

## Migration order

Port in roughly this order, then retire the legacy `Program.cs` runner once
every check has a `[Fact]`/`[Theory]` equivalent:

1. **Pure-data Model tests** (`CaptureSettings`, `MediaFormat`,
   `RecordingFormat`, `SplitEncodeSupport`, etc.). Until the app-facing model
   types move into a neutral target, xUnit tests still resolve them from the
   staged `Sussudio.dll`; `InternalsVisibleTo` reduces private-member cracking
   but does not remove the assembly-loading boundary.
2. **Contracts tests** (`RecordingContext`, `FinalizeResult`,
   `GpuPipelineHandles`, `IRecordingSink` shape).
3. **Behavioral service tests** that exercise pure logic
   (`FrameLedger`, `RecordingPipelineOptions`, `PresentMonProbe`).
4. **Source-shape tests** - replace broad implementation-grep tests with
   behavioral tests where behavior is the contract. Keep focused ownership and
   source-shape assertions when the architecture boundary itself is the
   contract, and keep them small enough that a legitimate move has one obvious
   test update.
5. **Reflection-over-private tests** — rewrite to use the now-internal API
   directly. If a test still needs to peek at a private field, the design is
   probably the bug; consider exposing the value or restructuring.
6. **Integration tests** — stand up in-process flows via the
   `IProcessSupervisor` `DispatchProxy` seam already in
   `RecordingVerifier.Integration.Tests.cs`. Targets: HDR/SDR encode round-trip,
   flashback ring-buffer rotation, automation hub ssctl/MCP command surface.

## Conventions for ported tests

- Prefer a class per source-file-area. New migration-only owner files may use
  `XUnit.<Area>Tests.cs`; existing focused xUnit files can keep their area
  names when that is clearer than renaming churn.
- Use `Assert.Equal/True/False/Contains/Empty`. Avoid `Assert.Equal(expected,
  actual, comparer)` overloads unless the type genuinely needs custom equality.
- For HDR/P010 paths, prefer behavioural tests that drive the encoder against a
  small fixture buffer. Source-grep contract assertions stay only when the
  public API shape is the contract.
- Keep the legacy `Program.cs` runner available while the repo still requires
  the offline `dotnet exec` validation shim; add new coverage to xUnit instead
  of restoring a harness catalog.

## Open work tracked separately

- `source_text_grep_assertions_couple_to_text` — replace per-area, biggest win.
- `structural_snapshot_tests_dont_verify_behavior` — collapse property-name
  loops into behavioural tests.
- `untested_critical_subsystems_on_hdr_rail` — needs the integration-test seam
  before meaningful coverage lands.

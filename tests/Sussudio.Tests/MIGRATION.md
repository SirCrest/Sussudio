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
- `XUnit.BoolConvertersTests.cs` owns the former legacy bool/visibility
  converter checks.
- `XUnit.MediaFormatTests.cs` owns the former legacy MediaFormat equality and
  hash-code checks.
- `StatsOverlay.Lifecycle.Tests.cs` now owns xUnit source-contract checks for
  stats overlay lifecycle and section chrome wiring. The remaining stats
  presentation checks still migrate incrementally from the legacy catalog.
- `XUnit.StatsPresentation.Formatting.Tests.cs` owns the former legacy
  detached-window, encoder formatting, expected-display-repeat, and compact
  preview summary stats presentation behavior checks.
- `XUnit.StatsPresentation.FrameTime.Tests.cs` owns the former legacy frame-time
  range and frame-time geometry stats presentation behavior checks.
- `XUnit.StatsHardwareRowsTests.cs` owns the former legacy hardware decode/GPU
  row formatting behavior checks. `XUnit.StatsHardwareRows.InputProvider.Tests.cs`
  owns hardware-row input sampling policy checks.
- `XUnit.CapturePoliciesTests.cs` owns the former legacy HdrOutputPolicy
  behavior and HDR output environment-switch checks.
- `XUnit.RuntimeHelpersTests.cs` owns focused runtime helper behavior checks.
- `XUnit.SnapshotModelsTests.cs` and its `SnapshotModels.*` partials own the
  former legacy CaptureDiagnosticsSnapshot, CaptureHealthSnapshot,
  SourceSignalTelemetrySnapshot, SourceTelemetryDetailEntry, and source
  telemetry automation projection contract checks, plus AutomationSnapshot CPU
  MJPEG, MJPEG preview, preview diagnostics, capture-command, recording,
  Flashback recording/playback/export, visual cadence, and AutomationOptions DTO
  shape checks.
  `SnapshotModels.Automation.CpuMjpeg.Tests.cs`,
  `SnapshotModels.Automation.MjpegPreview.Tests.cs`,
  `SnapshotModels.Automation.PreviewDiagnostics.Tests.cs`,
  `SnapshotModels.Automation.CaptureCommands.Tests.cs`,
  `SnapshotModels.Automation.Recording.Tests.cs`,
  `SnapshotModels.Automation.FlashbackRecording.Tests.cs`,
  `SnapshotModels.Automation.FlashbackPlayback.Tests.cs`,
  `SnapshotModels.Automation.FlashbackExport.Tests.cs`, and
  `SnapshotModels.Automation.VisualCadence.Tests.cs` own the focused
  AutomationSnapshot metric-shape checks.
- `XUnit.SmallContractsTests.cs` owns the former legacy audio input, audio
  level event, capture device, and automation window action small contracts.
- `XUnit.CaptureConfigurationModelsTests.cs` owns shared reflection helpers for
  capture configuration xUnit contract checks.
- `XUnit.CaptureModeOptionsTests.cs` owns capture mode option display metadata
  and option-builder behavior checks.
- `XUnit.CaptureSettingsContractsTests.cs` owns capture settings defaults,
  output path/file naming, bitrate policy, and MJPEG HFR policy checks.
- `XUnit.RecordingConfigurationPolicyTests.cs` owns recording selection policy,
  encoder support, and recording pipeline option contract checks.
- `XUnit.RecordingPipelineContractsTests.cs` owns the former legacy recording
  queue overload-policy, LibAv sink, WASAPI, capture fan-out, and CaptureService
  recording ownership checks.
- `XUnit.RecordingModelContractsTests.cs` owns the former legacy recording
  model execution surface for LibAv sink loop/source-ownership checks,
  capture runtime failure/runtime-flag checks, and Flashback buffer manager
  behavior/source-ownership checks.
- `XUnit.CoreRuntimeRecordingContractsTests.cs` owns the former legacy core
  runtime recording subgroup: recording verifier, LibAv encoder, Flashback
  recording integrity, shared formatter, and dedicated LibAv verification
  script checks.
- `XUnit.CoreRuntimeContractsTests.cs` owns the former legacy core runtime
  subgroup: runtime telemetry, capture-service snapshot, NativeXu, frame ledger,
  recording-integrity, and basic app contract checks.
- `RecordingArtifactManager.Tests.cs` owns the former legacy temp artifact
  finalization and rollback behavior checks for recording output cleanup.
- `MjpegPipeline.Timing.Tests.cs` owns the former legacy CPU MJPEG timing
  metric, stopwatch timeout, and software decoder shape checks.
- `XUnit.MjpegPipelineContractsTests.cs` owns the former legacy CPU MJPEG
  pipeline, pooled-frame lease/fan-out, preview jitter, cadence, and queued
  lease-release checks.
- `XUnit.FlashbackModelsTests.cs` owns the former legacy Flashback buffer option
  sizing, session, playback-state, export progress, export segment, and export
  request model contract checks; `XUnit.FlashbackModels.PropertyAssertions.cs`
  owns the shared reflection/nullability assertion helpers for that suite.
- `XUnit.FlashbackEncoderSinkContractsTests.cs` owns the former legacy
  Flashback encoder sink frame-rate, codec, counter, queue, force-rotate,
  packet-drain, startup, and source-ownership checks.
- `XUnit.FlashbackPlaybackContractsTests.cs` owns the former legacy Flashback
  playback startup, command-queue, source-shape, cadence, submission, reopen,
  transition-guard, and metric-reset checks.
- `XUnit.FlashbackDecoderContractsTests.cs` owns the former legacy Flashback
  decoder frame-buffer, source-ownership, state/lifetime, timestamp, audio,
  frame-validation, and cancellation checks.
- `XUnit.FlashbackExporterContractsTests.cs` owns the former legacy Flashback
  exporter cleanup, request validation, failure classification, segment,
  cancellation, output path/finalization, and source-ownership checks.
- `Flashback.Playback.Markers.Tests.cs` owns the former legacy Flashback
  playback in/out marker API, normalization, disposal, and marker clamp checks.
- `StatsDockPresentation.Tests.cs`, `StatsPresentation.Ownership.Tests.cs`,
  and `StatsPresentation.SourceTelemetry.Tests.cs` own the former legacy stats
  dock, row chrome, builder ownership, and HDMI source telemetry panel checks.
- `MainWindowUiContract.StatsSnapshot.Tests.cs` owns the former legacy stats
  snapshot construction and health/renderer metric projection checks.
- `XUnit.ToolFormatterContractsTests.cs` owns focused tool formatter contract
  checks.
- `XUnit.AutomationCatalogContractsTests.cs` owns the former legacy automation
  catalog, manifest, path-policy, and reliability-gates contract checks.
- `XUnit.AutomationAppSurfaceContractsTests.cs` owns the former legacy
  automation app-surface registration group: App exception policy, converter
  and display formatting, LoggingJsonContext, MainWindow automation IDs,
  full-screen/window dispatch adapters, pipe/auth, and Stream Deck auth-envelope
  checks.
- `XUnit.AutomationViewModelFlashbackUiContractsTests.cs` owns the former
  legacy automation ViewModel/Flashback UI registration group: automation
  settings, audio/device/capture/recording routes, async Flashback/probe
  surface, runtime snapshot ownership, scrub/toggle behavior, timeline
  geometry, and Flashback presentation controller ownership checks.
- `XUnit.AutomationDispatcherContractsTests.cs` owns the former legacy
  automation dispatcher registration group: payload parsing, catalog metadata,
  readiness classification, authorization, manifest, command coverage, and
  focused dispatcher command-owner checks.
- `XUnit.AutomationCaptureFlashbackRoutingContractsTests.cs` owns the former
  legacy automation capture/Flashback routing registration group: Flashback
  routing, capture transition policy, capture session coordinator contracts,
  service namespace ownership, and diagnostics snapshot refresh serialization.
- `XUnit.AutomationSnapshotProjectionContractsTests.cs` owns the former legacy
  automation diagnostics snapshot-projection registration group: snapshot
  status/evaluation, audio, capture/settings, source/cadence, MJPEG, recording,
  process/A/V sync, preview, and Flashback projection ownership checks.
- `XUnit.ToolProbeContractsTests.cs` owns the former legacy PresentMon parser,
  ssctl pipe transport, KS audio-node, and EGAVDS probe contract checks.
- `XUnit.AutomationSnapshotFormatterContractsTests.cs` owns the former legacy
  shared automation snapshot formatter core, Flashback, Preview D3D, and source
  ownership contract checks.
- `XUnit.SsctlCommandHandlerContractsTests.cs` owns the former legacy ssctl
  command-handler routing, source ownership, and catalog-backed help contract
  checks.
- `XUnit.SsctlFormatterContractsTests.cs` owns the former legacy ssctl
  formatter snapshot, source ownership, and timeline output contract checks.
- `XUnit.ToolModelContractsTests.cs` owns the former legacy NVML snapshot and
  CaptureSessionSnapshot default-state tool-contract checks.
  `XUnit.TargetAssemblyBootstrap.cs` lets xUnit wrapper facts initialize the
  staged app assembly before calling legacy reflection helpers.
- `XUnit.NativeToolProbeContractsTests.cs` owns the former legacy RTK I2C probe
  unsafe-native-path guard check.
- `XUnit.McpWindowPreviewToolContractsTests.cs` owns the former legacy MCP
  window/preview wait, screenshot, frame-capture, window-action, and
  preview-toggle/probe checks.
- `XUnit.McpPerformanceToolContractsTests.cs` owns the former legacy MCP
  PresentMon correlation, performance timeline, and frame-pacing verdict checks.
- `XUnit.McpToolSurfaceContractsTests.cs` owns the former legacy MCP tool
  surface, command-routing, host/pipe, verification, Flashback tool, and
  diagnostic-session tool entry checks.
- `XUnit.PresentationPreviewHarnessRegistrationTests.cs` owns the former legacy
  presentation-preview harness registration guard.
- `XUnit.PresentationPreviewMainViewModelInitialContractsTests.cs` owns the
  former legacy presentation-preview MainViewModel initial registration group:
  recording transition start/stop failure propagation.
- `XUnit.PresentationPreviewMainWindowInitialContractsTests.cs` owns the former
  legacy presentation-preview MainWindow initial registration group: close
  cancellation and window screenshot helper ownership checks.
- `XUnit.ArchitectureDocsAgentMapOwnershipTests.cs` owns the former legacy
  AGENT_MAP ownership, path-reference, and test-project shape guard checks.
- `XUnit.ArchitectureDocsReferenceIntegrityTests.cs` owns the former legacy
  architecture-doc reference drift and migration-inventory guard checks.
- Additional focused `[Fact]`/`[Theory]` files such as
  `AutomationContracts.ProtocolXunit.Tests.cs`,
  `AutomationToolContracts.ProtocolXunit.Tests.cs` (automation client timeout
  policy, advanced command-map alignment, and pipe/tool protocol contracts),
  `RuntimeContracts.Tests.cs`, `WindowSnapRegionLayoutPolicy.Tests.cs`,
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

Both must stay green during the migration.

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
- Don't touch the legacy `Program.cs` runner: keep both stacks green until the
  port is complete.

## Open work tracked separately

- `source_text_grep_assertions_couple_to_text` — replace per-area, biggest win.
- `structural_snapshot_tests_dont_verify_behavior` — collapse property-name
  loops into behavioural tests.
- `untested_critical_subsystems_on_hdr_rail` — needs the integration-test seam
  before meaningful coverage lands.

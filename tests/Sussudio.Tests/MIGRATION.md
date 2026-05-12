# Sussudio.Tests Migration Plan

The test project ran ~400 cases through a hand-rolled `Program.cs` runner that
loaded `Sussudio.dll` via reflection. Cluster `test-framework-migration` opens
the dual-stack path: keep the legacy runner, add xUnit alongside, and port
incrementally.

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
  of the hand-rolled `AssertContains/AssertEqual` helpers.

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

- Legacy runner (existing 400 cases):
  `dotnet run --project tests/Sussudio.Tests -- "<path-to-Sussudio.dll>"`
- xUnit cases:
  `dotnet test tests/Sussudio.Tests/Sussudio.Tests.csproj`

Both must stay green during the migration.

## Migration order

Port in roughly this order, then retire the legacy `Program.cs` runner once
every check has a `[Fact]`/`[Theory]` equivalent:

1. **Pure-data Model tests** (`CaptureSettings`, `MediaFormat`,
   `RecordingFormat`, `SplitEncodeSupport`, etc.). They have no reflection
   dependency once `InternalsVisibleTo` is in place.
2. **Contracts tests** (`RecordingContext`, `FinalizeResult`,
   `GpuPipelineHandles`, `IRecordingSink` shape).
3. **Behavioral service tests** that exercise pure logic
   (`FrameLedger`, `RecordingPipelineOptions`, `PresentMonProbe`).
4. **Source-text-grep tests** — replace each with a behavioral test that calls
   the real method and asserts on outputs or recorded log lines. Keep one
   tightly-scoped Roslyn-syntax-API test per public surface where stability
   itself is the contract (e.g. `IAutomationCommandDispatcher` shape).
5. **Reflection-over-private tests** — rewrite to use the now-internal API
   directly. If a test still needs to peek at a private field, the design is
   probably the bug; consider exposing the value or restructuring.
6. **Integration tests** — stand up in-process flows via the
   `IProcessSupervisor` `DispatchProxy` seam already in
   `RecordingVerifier.Integration.Tests.cs`. Targets: HDR/SDR encode round-trip,
   flashback ring-buffer rotation, automation hub ssctl/MCP command surface.

## Conventions for ported tests

- Class per source-file-area, named `XUnit.<Area>Tests.cs` while migration is
  in progress so `git mv` later is mechanical.
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

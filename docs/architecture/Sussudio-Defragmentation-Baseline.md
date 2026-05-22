# Sussudio Defragmentation Baseline

This file should be filled or regenerated before the next architecture slice. It exists so the active goal can measure defragmentation against concrete data instead of vibes.

Run from the repository root:

```powershell
./scripts/architecture/Capture-SussudioDefragBaseline.ps1
```

That script writes `docs/architecture/Sussudio-Defragmentation-Baseline.generated.md`.

## Values to capture

- Total production `.cs` files.
- Total test `.cs` files.
- Percentage of production `.cs` files under 60 lines.
- Percentage of production `.cs` files under 80 lines.
- Largest partial-type clusters by file count.
- Largest implementation files by line count.
- Areas where a normal feature/bug review requires more than about five primary production files.

## Known reported symptoms to verify

- `AutomationDiagnosticsHub`: approximately 217 files.
- `CaptureService`: approximately 109 files.
- `MainWindow`: approximately 95 files.
- `MainViewModel`: approximately 66 files.
- Approximately 44% of all `.cs` files are under 60 lines.

## Slice evidence format

For each completed slice, add a short entry:

```text
Date:
Area:
Problem:
Files consolidated:
Files added:
Net production .cs delta:
Partial clusters reduced:
Build/tests/runtime checks:
CLI/MCP/pipe checks, if applicable:
Behavior preserved:
Notes for future agents:
```

## Slice Evidence

Date: 2026-05-21
Area: Tool snapshot formatting
Problem: Thread-health formatter rows were scattered across one section-order file plus three one-row partial files in both the shared `AutomationSnapshotFormatter` and ssctl `Formatters` implementations.
Files consolidated: `tools/Common/AutomationSnapshotFormatter.ThreadHealth.*.cs`; `tools/ssctl/Formatters.Snapshot.ThreadHealth.*.cs`
Files added: none
Net production .cs delta: -6
Partial clusters reduced: `AutomationSnapshotFormatter` -3 files; `Formatters` -3 files
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; offline runtime snapshot harness; `git diff --check`
CLI/MCP/pipe checks, if applicable: covered by ssctl formatter contracts and runtime snapshot formatter tests
Behavior preserved: formatter output paths and section order remain unchanged; only source ownership changed
Notes for future agents: keep formatter row families grouped when they are one visible section; split again only for a named formatter collaborator or a demonstrable testability boundary

Date: 2026-05-21
Area: Tool snapshot formatting
Problem: Flashback encoding subsection routing lived in one-method shared and ssctl partial files separate from the Flashback section owner.
Files consolidated: `tools/Common/AutomationSnapshotFormatter.Flashback.Encoding.cs`; `tools/ssctl/Formatters.Snapshot.Flashback.Encoding.cs`
Files added: none
Net production .cs delta: -2
Partial clusters reduced: `AutomationSnapshotFormatter` -1 file; `Formatters` -1 file
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; offline runtime snapshot harness; `git diff --check`
CLI/MCP/pipe checks, if applicable: covered by ssctl/shared formatter contract tests
Behavior preserved: Flashback formatter gating, section order, encoding status, and encoding health output stay unchanged
Notes for future agents: keep one-method subsection routers with their section owner unless the router grows real policy

Date: 2026-05-21
Area: Tool snapshot formatting
Problem: Automation response-success detection lived in a one-method partial file separate from the other JSON value accessors.
Files consolidated: `tools/Common/AutomationSnapshotFormatter.Response.cs`
Files added: none
Net production .cs delta: -1
Partial clusters reduced: `AutomationSnapshotFormatter` -1 file
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; offline runtime snapshot harness; `git diff --check`
CLI/MCP/pipe checks, if applicable: covered by shared formatter contracts and MCP/ssctl tool formatter tests
Behavior preserved: `AutomationSnapshotFormatter.IsSuccess` signature and semantics stay unchanged
Notes for future agents: keep generic JSON response/value helpers together unless response handling becomes a named policy object

Date: 2026-05-21
Area: Diagnostic session result formatting
Problem: Three small diagnostic-session summary rows lived in separate partial files from the formatter orchestration, forcing a reader to open four files to understand the top-level report flow.
Files consolidated: `tools/Common/DiagnosticSessionResultFormatter.RecordingVerification.cs`; `tools/Common/DiagnosticSessionResultFormatter.PresentMon.cs`; `tools/Common/DiagnosticSessionResultFormatter.ProcessPerformance.cs`
Files added: none
Net production .cs delta: -3
Partial clusters reduced: `DiagnosticSessionResultFormatter` -3 files
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; offline runtime snapshot harness; `git diff --check`
CLI/MCP/pipe checks, if applicable: covered by diagnostic-session formatter ownership tests and runtime formatter tests
Behavior preserved: diagnostic-session report order and row text remain unchanged
Notes for future agents: keep short scalar summary rows with the formatter root unless they grow separate formatting policy

Date: 2026-05-21
Area: Diagnostic session result models
Problem: Preview cadence and visual-cadence DTO fields were split across two tiny partial files even though callers treat them as one preview cadence result surface.
Files consolidated: `tools/Common/DiagnosticSessionResult.PreviewVisualCadence.cs`
Files added: none
Net production .cs delta: -1
Partial clusters reduced: `DiagnosticSessionResult` -1 file
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; offline runtime snapshot harness; `git diff --check`
CLI/MCP/pipe checks, if applicable: covered by diagnostic-session model ownership and formatter tests
Behavior preserved: DTO property names and init semantics stay unchanged
Notes for future agents: keep preview cadence DTO fields grouped unless visual cadence grows independent behavior or a separate model type

Date: 2026-05-21
Area: Diagnostic session result formatting
Problem: Preview diagnostic-session section ordering lived in a one-method router file separate from the formatter orchestration.
Files consolidated: `tools/Common/DiagnosticSessionResultFormatter.Preview.cs`
Files added: none
Net production .cs delta: -1
Partial clusters reduced: `DiagnosticSessionResultFormatter` -1 file
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; offline runtime snapshot harness; `git diff --check`
CLI/MCP/pipe checks, if applicable: covered by diagnostic-session formatter ownership and runtime formatter tests
Behavior preserved: preview diagnostic-session section order and subsection text remain unchanged
Notes for future agents: keep one-method formatter routers with the report orchestration unless the router grows real policy

Date: 2026-05-21
Area: Automation diagnostics evaluation lanes
Problem: Realtime decode and recording/audio lane text lived in tiny partials separate from the lane orchestration that consumes them.
Files consolidated: `Sussudio/Services/Automation/AutomationDiagnosticsHub.DiagnosticEvaluationLanes.Realtime.Mjpeg.cs`; `Sussudio/Services/Automation/AutomationDiagnosticsHub.DiagnosticEvaluationLanes.Realtime.Recording.cs`
Files added: none
Net production .cs delta: -2
Partial clusters reduced: `AutomationDiagnosticsHub` -2 files
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; offline runtime snapshot harness; `git diff --check`
CLI/MCP/pipe checks, if applicable: covered by automation diagnostics evaluation ownership and runtime snapshot regression tests
Behavior preserved: Diagnostic lane text still reports the same decode, recording integrity, and audio integrity details
Notes for future agents: keep trivial realtime lane text helpers with `DiagnosticEvaluationLanes.cs` unless they grow lane-specific policy

Date: 2026-05-21
Area: Automation diagnostics Flashback evaluation lanes
Problem: Flashback diagnostic lane text lived in three tiny partials separate from the lane orchestration that consumes them.
Files consolidated: `Sussudio/Services/Automation/AutomationDiagnosticsHub.DiagnosticEvaluationLanes.Flashback.Recording.cs`; `Sussudio/Services/Automation/AutomationDiagnosticsHub.DiagnosticEvaluationLanes.Flashback.Export.cs`; `Sussudio/Services/Automation/AutomationDiagnosticsHub.DiagnosticEvaluationLanes.Flashback.Playback.cs`
Files added: none
Net production .cs delta: -3
Partial clusters reduced: `AutomationDiagnosticsHub` -3 files
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; offline runtime snapshot harness; `git diff --check`
CLI/MCP/pipe checks, if applicable: covered by automation diagnostics evaluation ownership and runtime snapshot regression tests
Behavior preserved: Flashback diagnostic lane text still reports the same recording, export, temp-cache, playback command, and playback performance details
Notes for future agents: keep simple diagnostic lane text helpers with `DiagnosticEvaluationLanes.cs` unless they grow lane-specific policy

Date: 2026-05-21
Area: Diagnostic session result formatting
Problem: Flashback playback performance text was split across separate cadence, 1% low, audio-master, and row-assembly fragments even though those helpers only compose the single `Flashback Playback Perf` row.
Files consolidated: `tools/Common/DiagnosticSessionResultFormatter.FlashbackPlayback.Cadence.cs`; `tools/Common/DiagnosticSessionResultFormatter.FlashbackPlayback.OnePercentLow.cs`; `tools/Common/DiagnosticSessionResultFormatter.FlashbackPlayback.AudioMaster.cs`
Files added: none
Net production .cs delta: -3
Partial clusters reduced: `DiagnosticSessionResultFormatter` -3 files
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; offline runtime snapshot harness; `git diff --check`
CLI/MCP/pipe checks, if applicable: covered by diagnostic-session formatter ownership and runtime formatter tests
Behavior preserved: Flashback playback performance row text and helper output order remain unchanged
Notes for future agents: keep helper-only text builders with their owning formatter row unless they become reusable policy

Date: 2026-05-21
Area: Diagnostic session result formatting
Problem: Preview D3D diagnostic-session text split performance/slow-frame output and CPU-timing output across separate tiny files even though both rows describe the same Preview D3D report concern.
Files consolidated: `tools/Common/DiagnosticSessionResultFormatter.PreviewD3D.CpuTiming.cs`
Files added: none
Net production .cs delta: -1
Partial clusters reduced: `DiagnosticSessionResultFormatter` -1 file
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; offline runtime snapshot harness; `git diff --check`
CLI/MCP/pipe checks, if applicable: covered by diagnostic-session formatter ownership and runtime formatter tests
Behavior preserved: Preview D3D performance and CPU-timing report rows remain in the same order with unchanged field text
Notes for future agents: keep tightly coupled report rows together when they describe the same runtime subsystem

Date: 2026-05-21
Area: Diagnostic session result models
Problem: Flashback playback result fields kept 1% low and audio-master performance properties in separate tiny DTO partials from the cadence/frame-delivery properties that consume the same playback performance projection.
Files consolidated: `tools/Common/DiagnosticSessionResult.FlashbackPlayback.OnePercentLow.cs`; `tools/Common/DiagnosticSessionResult.FlashbackPlayback.AudioMaster.cs`
Files added: none
Net production .cs delta: -2
Partial clusters reduced: `DiagnosticSessionResult` -2 files
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; offline runtime snapshot harness; `git diff --check`
CLI/MCP/pipe checks, if applicable: covered by diagnostic-session model ownership and runtime snapshot regression tests
Behavior preserved: Diagnostic-session JSON property names, initialization semantics, and formatter output remain unchanged
Notes for future agents: keep property-only result partials grouped by the projection/report concern they model

Date: 2026-05-21
Area: Diagnostic session result construction
Problem: Flashback playback result builder projections kept 1% low and audio-master value mappings in separate tiny partials from the cadence/frame-delivery projection owner, while the result DTO and formatter now group these playback performance concerns together.
Files consolidated: `tools/Common/DiagnosticSessionResultBuilder.FlashbackPlaybackOnePercentLowResult.cs`; `tools/Common/DiagnosticSessionResultBuilder.FlashbackPlaybackAudioMasterResult.cs`
Files added: none
Net production .cs delta: -2
Partial clusters reduced: `DiagnosticSessionResultBuilder` -2 files
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; offline runtime snapshot harness; `git diff --check`
CLI/MCP/pipe checks, if applicable: covered by diagnostic-session builder ownership and runtime snapshot regression tests
Behavior preserved: Flashback playback result projection values and final diagnostic-session JSON fields remain unchanged
Notes for future agents: keep builder projection records near the mapping code for the same result/formatter concern

Date: 2026-05-21
Area: Diagnostic session result construction
Problem: Preview result construction kept scheduler and visual-cadence result projection records in separate small partials even though they are preview DTO mappings consumed by the same final result initializer.
Files consolidated: `tools/Common/DiagnosticSessionResultBuilder.PreviewSchedulerResult.cs`; `tools/Common/DiagnosticSessionResultBuilder.PreviewVisualCadenceResult.cs`
Files added: none
Net production .cs delta: -2
Partial clusters reduced: `DiagnosticSessionResultBuilder` -2 files
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; offline runtime snapshot harness; `git diff --check`
CLI/MCP/pipe checks, if applicable: covered by diagnostic-session builder ownership and runtime snapshot regression tests
Behavior preserved: Preview scheduler, preview cadence, and visual-cadence result projection values remain unchanged
Notes for future agents: keep simple preview result projection records together unless a projection grows independent policy

Date: 2026-05-21
Area: Diagnostic session result models
Problem: End-of-run overview fields lived in a tiny property-only partial separate from the root diagnostic-session summary DTO.
Files consolidated: `tools/Common/DiagnosticSessionResult.Overview.cs`
Files added: none
Net production .cs delta: -1
Partial clusters reduced: `DiagnosticSessionResult` -1 file
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; offline runtime snapshot harness; `git diff --check`
CLI/MCP/pipe checks, if applicable: covered by diagnostic-session model ownership and runtime snapshot regression tests
Behavior preserved: Diagnostic-session JSON overview fields, property names, and initialization semantics remain unchanged
Notes for future agents: keep tiny root-summary DTO fragments with the root result model unless they represent a runtime subsystem

Date: 2026-05-21
Area: Automation diagnostics HDR projection
Problem: HDR pixel-format detection lived in a 9-line core partial even though its only caller is preview HDR state projection.
Files consolidated: `Sussudio/Services/Automation/AutomationDiagnosticsHub.Hdr.cs`
Files added: none
Net production .cs delta: -1
Partial clusters reduced: `AutomationDiagnosticsHub` -1 file
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; offline runtime snapshot harness; `git diff --check`
CLI/MCP/pipe checks, if applicable: covered by automation diagnostics source-ownership and runtime snapshot regression tests
Behavior preserved: Preview HDR input detection still uses `MediaFormat.IsHdrPixelFormat` for negotiated pixel format
Notes for future agents: keep single-use helpers with their only runtime projection owner unless they become shared policy

Date: 2026-05-21
Area: Automation diagnostics alerts
Problem: Flashback playback alert orchestration lived in a tiny router partial separate from the alert orchestration root that calls it.
Files consolidated: `Sussudio/Services/Automation/AutomationDiagnosticsHub.FlashbackPlaybackAlerts.cs`
Files added: none
Net production .cs delta: -1
Partial clusters reduced: `AutomationDiagnosticsHub` -1 file
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; offline runtime snapshot harness; `git diff --check`
CLI/MCP/pipe checks, if applicable: covered by automation diagnostics alert source-ownership and runtime snapshot regression tests
Behavior preserved: Flashback playback command and performance alert routing remains unchanged
Notes for future agents: keep one-method alert routers with the alert orchestration root unless they grow policy

Date: 2026-05-21
Area: Automation diagnostics snapshot flattening
Problem: Projection-to-flattened-set dispatch lived in a tiny router partial separate from the flattened projection-set owner.
Files consolidated: `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.cs`
Files added: none
Net production .cs delta: -1
Partial clusters reduced: `AutomationDiagnosticsHub` -1 file
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; offline runtime snapshot harness; `git diff --check`
CLI/MCP/pipe checks, if applicable: covered by automation diagnostics snapshot-construction ownership and runtime snapshot regression tests
Behavior preserved: Automation snapshot projection flattening still routes through the flattened projection set before final DTO initialization
Notes for future agents: keep one-method flattening routers with the flattened set owner unless they grow real dispatch policy

Date: 2026-05-21
Area: Diagnostic session result formatting
Problem: Flashback diagnostic-session section ordering lived in a one-method router file separate from the formatter orchestration.
Files consolidated: `tools/Common/DiagnosticSessionResultFormatter.Flashback.cs`
Files added: none
Net production .cs delta: -1
Partial clusters reduced: `DiagnosticSessionResultFormatter` -1 file
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; offline runtime snapshot harness; `git diff --check`
CLI/MCP/pipe checks, if applicable: covered by diagnostic-session formatter ownership and runtime formatter tests
Behavior preserved: Flashback diagnostic-session section order and subsection text remain unchanged
Notes for future agents: keep one-method formatter routers with the report orchestration unless the router grows real policy

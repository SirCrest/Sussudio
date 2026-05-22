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
Area: Automation diagnostics PreviewD3D projection
Problem: PreviewD3D projection data and matching flattened DTO field projection lived in parallel partial fragments, forcing agents to open several extra files to audit one automation snapshot concern.
Files consolidated: `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.PreviewD3D.cs`; `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.PreviewD3D.CpuTiming.cs`; `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.PreviewD3D.LatencyAndStats.cs`; `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.PreviewD3D.FrameFlow.cs`
Files added: none
Net production .cs delta: -4
Partial clusters reduced: `AutomationDiagnosticsHub` -4 files
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; offline runtime snapshot harness; `git diff --check`
CLI/MCP/pipe checks, if applicable: covered by automation diagnostics projection ownership tests and runtime snapshot regression tests
Behavior preserved: PreviewD3D automation snapshot fields still flow through typed projection records before final DTO initialization
Notes for future agents: keep PreviewD3D projection and direct flattening logic beside the matching projection owners unless the flattening policy becomes shared

Date: 2026-05-21
Area: Automation diagnostics snapshot status projection
Problem: Snapshot status projection and final flattened DTO field projection lived in separate tiny partials even though the flattening is a direct one-to-one projection of the same status fields.
Files consolidated: `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.SnapshotStatus.cs`
Files added: none
Net production .cs delta: -1
Partial clusters reduced: `AutomationDiagnosticsHub` -1 file
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; offline runtime snapshot harness; `git diff --check`
CLI/MCP/pipe checks, if applicable: covered by automation diagnostics snapshot projection ownership tests and runtime snapshot regression tests
Behavior preserved: Snapshot status automation fields still flow through typed projection records before final DTO initialization
Notes for future agents: keep direct snapshot status flattening beside the snapshot status projection unless the flattening policy grows shared logic

Date: 2026-05-21
Area: Automation diagnostics capture transport projection
Problem: Capture transport projection and final flattened DTO field projection lived in separate tiny partials even though the flattening is a direct one-to-one projection of memory, subtype, and frame-ledger fields.
Files consolidated: `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.CaptureTransport.cs`
Files added: none
Net production .cs delta: -1
Partial clusters reduced: `AutomationDiagnosticsHub` -1 file
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; offline runtime snapshot harness; `git diff --check`
CLI/MCP/pipe checks, if applicable: covered by automation diagnostics capture transport projection ownership tests and runtime snapshot regression tests
Behavior preserved: Capture transport automation fields still flow through typed projection records before final DTO initialization
Notes for future agents: keep direct capture transport flattening beside the capture transport projection unless the flattening policy grows shared logic

Date: 2026-05-21
Area: Automation diagnostics Flashback recording projection
Problem: Flashback recording projection data and matching flattened DTO field projection lived in parallel partial fragments, forcing agents to open twelve files to audit one automation snapshot concern.
Files consolidated: `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.FlashbackRecording.cs`; `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.FlashbackRecording.StartupCache.cs`; `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.FlashbackRecording.Queues.cs`; `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.FlashbackRecording.Runtime.cs`; `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.FlashbackRecording.Backend.cs`; `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.FlashbackRecording.Encoder.cs`
Files added: none
Net production .cs delta: -6
Partial clusters reduced: `AutomationDiagnosticsHub` -6 files
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; offline runtime snapshot harness; `git diff --check`
CLI/MCP/pipe checks, if applicable: covered by automation diagnostics projection ownership tests and runtime snapshot regression tests
Behavior preserved: Flashback recording automation snapshot fields still flow through typed projection records before final DTO initialization
Notes for future agents: keep Flashback recording projection and flattening logic beside the matching focused projection owners unless the flattening policy becomes shared

Date: 2026-05-21
Area: Automation diagnostics Flashback playback projection
Problem: Flashback playback projection data and matching flattened DTO field projection lived in parallel partial fragments, forcing agents to open ten files to audit one automation snapshot concern.
Files consolidated: `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.FlashbackPlayback.cs`; `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.FlashbackPlayback.AudioMaster.cs`; `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.FlashbackPlayback.Timing.cs`; `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.FlashbackPlayback.Decode.cs`; `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.FlashbackPlayback.Commands.cs`
Files added: none
Net production .cs delta: -5
Partial clusters reduced: `AutomationDiagnosticsHub` -5 files
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; offline runtime snapshot harness; `git diff --check`
CLI/MCP/pipe checks, if applicable: covered by automation diagnostics projection ownership tests and runtime snapshot regression tests
Behavior preserved: Flashback playback automation snapshot fields still flow through typed projection records before final DTO initialization
Notes for future agents: keep Flashback playback projection and flattening logic beside the matching focused projection owners unless the flattening policy becomes shared

Date: 2026-05-21
Area: Automation diagnostics audio and ingest projection
Problem: Audio/ingest projection data and matching flattened DTO field projection lived in parallel partial fragments, forcing agents to open ten files to audit one automation snapshot concern.
Files consolidated: `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.AudioAndIngest.cs`; `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.AudioAndIngest.Signal.cs`; `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.AudioAndIngest.CaptureIngest.cs`; `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.AudioAndIngest.SourceReader.cs`; `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.AudioAndIngest.WasapiCapture.cs`; `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.AudioAndIngest.WasapiPlayback.cs`
Files added: none
Net production .cs delta: -6
Partial clusters reduced: `AutomationDiagnosticsHub` -6 files
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; offline runtime snapshot harness; `git diff --check`
CLI/MCP/pipe checks, if applicable: covered by automation diagnostics projection ownership tests and runtime snapshot regression tests
Behavior preserved: Audio, capture-ingest, source-reader, and WASAPI automation snapshot fields still flow through typed projection records before final DTO initialization
Notes for future agents: keep audio/ingest projection and flattening logic beside the matching focused projection owners unless the flattening policy becomes shared

Date: 2026-05-21
Area: Automation diagnostics MJPEG preview jitter projection
Problem: MJPEG preview jitter projection data and matching flattened DTO field projection lived in parallel partial fragments, forcing agents to open ten files to audit one automation snapshot concern.
Files consolidated: `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.MjpegPreviewJitter.cs`; `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.MjpegPreviewJitter.Queue.cs`; `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.MjpegPreviewJitter.Timing.cs`; `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.MjpegPreviewJitter.Adaptive.cs`; `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.MjpegPreviewJitter.Events.cs`
Files added: none
Net production .cs delta: -5
Partial clusters reduced: `AutomationDiagnosticsHub` -5 files
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; offline runtime snapshot harness; `git diff --check`
CLI/MCP/pipe checks, if applicable: covered by automation diagnostics projection ownership tests and runtime snapshot regression tests
Behavior preserved: MJPEG preview jitter automation snapshot fields still flow through typed projection records before final DTO initialization
Notes for future agents: keep MJPEG preview jitter projection and flattening logic beside the matching focused projection owners unless the flattening policy becomes shared

Date: 2026-05-21
Area: Automation diagnostics CaptureFormat projection
Problem: CaptureFormat projection-to-flattened DTO mapping was split across seven tiny flattening partials separate from the matching CaptureFormat projection owners.
Files consolidated: `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.CaptureFormat.cs`, `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.CaptureFormat.Requested.cs`, `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.CaptureFormat.HdrRequest.cs`, `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.CaptureFormat.Actual.cs`, `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.CaptureFormat.Negotiated.cs`, `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.CaptureFormat.ReaderObservation.cs`, `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.CaptureFormat.Encoder.cs`
Files added: none
Net production .cs delta: -7
Partial clusters reduced: `AutomationDiagnosticsHub` -7 files
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; offline runtime snapshot harness; `git diff --check`
CLI/MCP/pipe checks, if applicable: covered by automation diagnostics CaptureFormat projection ownership and runtime snapshot regression tests
Behavior preserved: CaptureFormat automation snapshot field names and projection staging remain unchanged; final flattening now lives beside the matching CaptureFormat projection owners
Notes for future agents: keep one-to-one CaptureFormat flattening with its projection owner unless it grows independent policy

Date: 2026-05-21
Area: Automation diagnostics PreviewRuntime projection
Problem: PreviewRuntime projection-to-flattened DTO mapping was split across seven flattening partials separate from the matching PreviewRuntime projection owners.
Files consolidated: `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.PreviewRuntime.cs`, `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.PreviewRuntime.Frame.cs`, `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.PreviewRuntime.Cadence.cs`, `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.PreviewRuntime.Surface.cs`, `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.PreviewRuntime.Startup.cs`, `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.PreviewRuntime.GpuPlayback.cs`, `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.PreviewRuntime.Color.cs`
Files added: none
Net production .cs delta: -7
Partial clusters reduced: `AutomationDiagnosticsHub` -7 files
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; offline runtime snapshot harness; `git diff --check`
CLI/MCP/pipe checks, if applicable: covered by automation diagnostics PreviewRuntime projection ownership and runtime snapshot regression tests
Behavior preserved: PreviewRuntime automation snapshot field names and projection staging remain unchanged; final flattening now lives beside the matching PreviewRuntime projection owners
Notes for future agents: keep one-to-one PreviewRuntime flattening with its projection owner unless it grows independent policy

Date: 2026-05-21
Area: Automation diagnostics RecordingIntegrity projection
Problem: RecordingIntegrity projection-to-flattened DTO mapping was split across six flattening partials separate from the matching RecordingIntegrity projection owners.
Files consolidated: `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.RecordingIntegrity.cs`, `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.RecordingIntegrity.Summary.cs`, `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.RecordingIntegrity.Video.cs`, `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.RecordingIntegrity.Backpressure.cs`, `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.RecordingIntegrity.Audio.cs`, `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.RecordingIntegrity.AvSync.cs`
Files added: none
Net production .cs delta: -6
Partial clusters reduced: `AutomationDiagnosticsHub` -6 files
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; offline runtime snapshot harness; `git diff --check`
CLI/MCP/pipe checks, if applicable: covered by automation diagnostics RecordingIntegrity projection ownership and runtime snapshot regression tests
Behavior preserved: RecordingIntegrity automation snapshot field names and projection staging remain unchanged; final flattening now lives beside the matching RecordingIntegrity projection owners
Notes for future agents: keep one-to-one RecordingIntegrity flattening with its projection owner unless it grows independent policy

Date: 2026-05-21
Area: Automation diagnostics RecordingPipeline projection
Problem: RecordingPipeline projection-to-flattened DTO mapping was split across five flattening partials separate from the matching RecordingPipeline projection owners.
Files consolidated: `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.RecordingPipeline.cs`, `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.RecordingPipeline.Encoder.cs`, `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.RecordingPipeline.Ingest.cs`, `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.RecordingPipeline.VideoQueue.cs`, `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.RecordingPipeline.HardwareQueues.cs`
Files added: none
Net production .cs delta: -5
Partial clusters reduced: `AutomationDiagnosticsHub` -5 files
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; offline runtime snapshot harness; `git diff --check`
CLI/MCP/pipe checks, if applicable: covered by automation diagnostics RecordingPipeline projection ownership and runtime snapshot regression tests
Behavior preserved: RecordingPipeline automation snapshot field names and projection staging remain unchanged; final flattening now lives beside the matching RecordingPipeline projection owners
Notes for future agents: keep one-to-one RecordingPipeline flattening with its projection owner unless it grows independent policy

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
Area: Automation diagnostics signal alert orchestration
Problem: Signal alert orchestration lived in a one-method router partial separate from the alert orchestration root that calls it.
Files consolidated: `Sussudio/Services/Automation/AutomationDiagnosticsHub.SignalAlerts.cs`
Files added: none
Net production .cs delta: -1
Partial clusters reduced: `AutomationDiagnosticsHub` -1 file
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; offline runtime snapshot harness; `git diff --check`
CLI/MCP/pipe checks, if applicable: covered by automation diagnostics alert ownership and runtime snapshot regression tests
Behavior preserved: Signal alert orchestration still delegates to preview, audio/recording, and capture alert rule owners in the same order
Notes for future agents: keep one-method alert routers with `AutomationDiagnosticsHub.Alerts.cs` unless they grow real policy

Date: 2026-05-21
Area: Automation diagnostics Flashback recording alerts
Problem: Single-rule Flashback export, temp-cache, and encoder alert helpers lived in tiny partials separate from the Flashback recording alert owner that routes them.
Files consolidated: `Sussudio/Services/Automation/AutomationDiagnosticsHub.FlashbackRecordingAlerts.Export.cs`; `Sussudio/Services/Automation/AutomationDiagnosticsHub.FlashbackRecordingAlerts.Storage.cs`; `Sussudio/Services/Automation/AutomationDiagnosticsHub.FlashbackRecordingAlerts.Encoder.cs`
Files added: none
Net production .cs delta: -3
Partial clusters reduced: `AutomationDiagnosticsHub` -3 files
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; offline runtime snapshot harness; `git diff --check`
CLI/MCP/pipe checks, if applicable: covered by automation diagnostics alert ownership and runtime snapshot regression tests
Behavior preserved: Flashback recording alerts still evaluate export stall/rotation gap, temp-cache pressure, encoder failure, and degradation rules in the same order
Notes for future agents: keep small Flashback recording alert rules with `FlashbackRecordingAlerts.cs` unless the rule grows enough policy to need its own owner

Date: 2026-05-21
Area: Automation diagnostics Flashback playback performance alerts
Problem: The frame-submission failure alert lived in a tiny partial separate from the Flashback playback performance alert owner that routes it.
Files consolidated: `Sussudio/Services/Automation/AutomationDiagnosticsHub.FlashbackPlaybackPerformanceAlerts.Submit.cs`
Files added: none
Net production .cs delta: -1
Partial clusters reduced: `AutomationDiagnosticsHub` -1 file
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; offline runtime snapshot harness; `git diff --check`
CLI/MCP/pipe checks, if applicable: covered by automation diagnostics alert ownership and runtime snapshot regression tests
Behavior preserved: Flashback playback performance alert orchestration still evaluates cadence, audio, and submit-failure alerts in the same order
Notes for future agents: keep single-rule playback performance alerts with `FlashbackPlaybackPerformanceAlerts.cs` unless the rule grows enough policy to need its own owner

Date: 2026-05-21
Area: Automation diagnostics source snapshot flattening
Problem: Source flattening orchestration lived in a tiny partial separate from the source-signal flattened projection owner.
Files consolidated: `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.Source.cs`
Files added: none
Net production .cs delta: -1
Partial clusters reduced: `AutomationDiagnosticsHub` -1 file
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; offline runtime snapshot harness; `git diff --check`
CLI/MCP/pipe checks, if applicable: covered by automation diagnostics source projection ownership and runtime snapshot regression tests
Behavior preserved: Source signal and source telemetry projections still flatten through the same source flattened projection handoff
Notes for future agents: keep tiny source flattening orchestration with the source signal flattened projection owner unless it grows real policy

Date: 2026-05-21
Area: Automation diagnostics A/V sync snapshot projection
Problem: A/V sync projection and final flattening lived in two tiny partials even though the fields are a direct one-to-one handoff.
Files consolidated: `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.AvSync.cs`
Files added: none
Net production .cs delta: -1
Partial clusters reduced: `AutomationDiagnosticsHub` -1 file
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; offline runtime snapshot harness; `git diff --check`
CLI/MCP/pipe checks, if applicable: covered by automation diagnostics A/V sync projection ownership and runtime snapshot regression tests
Behavior preserved: A/V sync capture drift, drift rate, encoder drift, and correction sample fields still flatten into the automation snapshot unchanged
Notes for future agents: keep direct one-to-one projection/flattening pairs together unless either side grows independent policy

Date: 2026-05-21
Area: Automation diagnostics audio-drop snapshot projection
Problem: Audio-drop projection and final flattening lived in two tiny partials even though the fields are a direct one-to-one handoff.
Files consolidated: `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.AudioDrops.cs`
Files added: none
Net production .cs delta: -1
Partial clusters reduced: `AutomationDiagnosticsHub` -1 file
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; offline runtime snapshot harness; `git diff --check`
CLI/MCP/pipe checks, if applicable: covered by automation diagnostics audio projection ownership and runtime snapshot regression tests
Behavior preserved: Audio drop queue saturation, backlog eviction, chunk drop, realtime queue drop, and file-writer queue drop fields still flatten into the automation snapshot unchanged
Notes for future agents: keep direct one-to-one projection/flattening pairs together unless either side grows independent policy

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

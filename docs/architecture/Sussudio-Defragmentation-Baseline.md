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
Area: Automation diagnostics Flashback evaluation
Problem: Active/stalled Flashback export diagnostic verdict construction lived in a small partial even though it is only called by the Flashback diagnostic owner that orders storage, recording, export, and playback verdicts.
Files consolidated: `Sussudio/Services/Automation/AutomationDiagnosticsHub.DiagnosticEvaluationFlashback.Export.cs`
Files added: none
Net production .cs delta: -1
Partial clusters reduced: `AutomationDiagnosticsHub` -1 file
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; offline runtime snapshot harness; `git diff --check`
CLI/MCP/pipe checks, if applicable: covered by automation diagnostics evaluation source-ownership tests and runtime snapshot regression tests
Behavior preserved: Flashback export diagnostic severity, code, progress-age detail, running/stalled messages, and lane mapping remain unchanged
Notes for future agents: keep lightweight export verdict policy with `DiagnosticEvaluationFlashback.cs`; keep recording and playback separate while they own larger policy sets

Date: 2026-05-21
Area: Automation diagnostics realtime evaluation
Problem: MJPEG duplicate source-signal and decode/reorder diagnostic verdict construction lived in a small partial even though it is only called by the realtime diagnostic owner that orders realtime state, recording, source, MJPEG, and preview verdicts.
Files consolidated: `Sussudio/Services/Automation/AutomationDiagnosticsHub.DiagnosticEvaluationRealtime.Mjpeg.cs`
Files added: none
Net production .cs delta: -1
Partial clusters reduced: `AutomationDiagnosticsHub` -1 file
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; offline runtime snapshot harness; `git diff --check`
CLI/MCP/pipe checks, if applicable: covered by automation diagnostics evaluation source-ownership tests and runtime snapshot regression tests
Behavior preserved: Realtime MJPEG duplicate-source and decode/reorder diagnostic severity, codes, messages, and lane mapping remain unchanged
Notes for future agents: keep lightweight MJPEG realtime verdict policy with `DiagnosticEvaluationRealtime.cs`; keep source and preview separate while they own larger policy sets

Date: 2026-05-21
Area: Automation diagnostics capture-format projection
Problem: Encoder format/codec/profile projection mappings lived in a tiny capture-format partial even though the capture-format projection owner immediately composes and flattens them with the rest of the capture-format group.
Files consolidated: `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.CaptureFormat.Encoder.cs`
Files added: none
Net production .cs delta: -1
Partial clusters reduced: `AutomationDiagnosticsHub` -1 file
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; offline runtime snapshot harness; `git diff --check`
CLI/MCP/pipe checks, if applicable: covered by automation diagnostics capture-format projection ownership tests and runtime snapshot regression tests
Behavior preserved: Encoder input/output pixel format, codec, profile, and ten-bit confirmation still map into the same flattened automation snapshot fields
Notes for future agents: keep encoder capture-format DTO mappings with `CaptureFormat.cs`; keep requested, negotiated, and reader-observation projections separate while they remain larger scan units

Date: 2026-05-21
Area: Automation diagnostics verification
Problem: Flashback-export verification profile shaping lived in a tiny helper partial even though it is only used by `VerifyFileAsync`, the explicit file-verification entry point.
Files consolidated: `Sussudio/Services/Automation/AutomationDiagnosticsHub.Verification.Profile.cs`
Files added: none
Net production .cs delta: -1
Partial clusters reduced: `AutomationDiagnosticsHub` -1 file
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; offline runtime snapshot harness; `git diff --check`
CLI/MCP/pipe checks, if applicable: covered by automation diagnostics runtime ownership tests and runtime snapshot regression tests
Behavior preserved: Explicit file verification still applies the flashback-export profile by preserving requested/negotiated format fields and substituting the export output path
Notes for future agents: keep explicit verification profile adaptation with `Verification.cs`; keep auto-verification scheduling separate while it remains snapshot-refresh lifecycle policy

Date: 2026-05-21
Area: Automation diagnostics realtime evaluation
Problem: Recording and audio integrity diagnostic verdict construction lived in a small realtime partial even though it is only called by the realtime diagnostic owner that orders state, recording, source, MJPEG, and preview verdicts.
Files consolidated: `Sussudio/Services/Automation/AutomationDiagnosticsHub.DiagnosticEvaluationRealtime.Recording.cs`
Files added: none
Net production .cs delta: -1
Partial clusters reduced: `AutomationDiagnosticsHub` -1 file
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; offline runtime snapshot harness; `git diff --check`
CLI/MCP/pipe checks, if applicable: covered by automation diagnostics evaluation source-ownership tests and runtime snapshot regression tests
Behavior preserved: Realtime recording-integrity and audio-integrity diagnostic severity, codes, messages, and lane mapping remain unchanged
Notes for future agents: keep lightweight realtime recording/audio verdict policy with `DiagnosticEvaluationRealtime.cs`; keep source and preview separate while they own larger policy sets

Date: 2026-05-21
Area: Automation diagnostics realtime evaluation
Problem: Source/capture cadence diagnostic verdict construction lived in a small realtime partial even though it is only called by the realtime diagnostic owner that orders state, recording, source, MJPEG, and preview verdicts.
Files consolidated: `Sussudio/Services/Automation/AutomationDiagnosticsHub.DiagnosticEvaluationRealtime.Source.cs`
Files added: none
Net production .cs delta: -1
Partial clusters reduced: `AutomationDiagnosticsHub` -1 file
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; offline runtime snapshot harness; `git diff --check`
CLI/MCP/pipe checks, if applicable: covered by automation diagnostics evaluation source-ownership tests and runtime snapshot regression tests
Behavior preserved: Realtime source/capture cadence diagnostic severity, codes, messages, and lane mapping remain unchanged
Notes for future agents: keep lightweight realtime source verdict policy with `DiagnosticEvaluationRealtime.cs`; keep preview separate while it owns scheduler and renderer policy

Date: 2026-05-21
Area: Automation diagnostics preview D3D projection
Problem: Preview D3D frame-latency wait and frame-statistics projection mappings lived in tiny partials even though the Preview D3D projection owner immediately composes and flattens both with pipeline latency; the larger frame-flow mapping remains its own focused owner.
Files consolidated: `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.PreviewD3D.FrameLatencyWait.cs`; `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.PreviewD3D.FrameStats.cs`
Files added: none
Net production .cs delta: -2
Partial clusters reduced: `AutomationDiagnosticsHub` -2 files
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; offline runtime snapshot harness; `git diff --check`
CLI/MCP/pipe checks, if applicable: covered by automation diagnostics preview D3D projection ownership tests and runtime snapshot regression tests
Behavior preserved: Preview D3D frame-latency wait counters and DXGI frame-statistics fields still map into the same flattened automation snapshot fields
Notes for future agents: keep frame-latency wait and frame-stats DTO mappings with `PreviewD3D.cs`; keep frame-flow separate while it remains a larger scan unit

Date: 2026-05-21
Area: Automation diagnostics realtime preview evaluation
Problem: Preview scheduler diagnostic verdict construction lived in a one-method partial even though it is only called by the realtime preview diagnostic owner that already composes preview scheduler, renderer, and present/display verdicts.
Files consolidated: `Sussudio/Services/Automation/AutomationDiagnosticsHub.DiagnosticEvaluationRealtime.PreviewScheduler.cs`
Files added: none
Net production .cs delta: -1
Partial clusters reduced: `AutomationDiagnosticsHub` -1 file
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; offline runtime snapshot harness; `git diff --check`
CLI/MCP/pipe checks, if applicable: covered by automation diagnostics evaluation source-ownership tests and runtime snapshot regression tests
Behavior preserved: Preview scheduler diagnostic severity, code, message selection, and lane mapping remain unchanged
Notes for future agents: keep lightweight preview scheduler verdict policy with `DiagnosticEvaluationRealtime.Preview.cs`; keep present/display separate while it owns its larger cadence and 1% low policy

Date: 2026-05-21
Area: Automation diagnostics preview runtime projection
Problem: Preview frame counters/pipeline latency and preview color/HDR state lived in tiny partials even though the preview runtime projection owner immediately composes and flattens both groups with the rest of the preview runtime DTO.
Files consolidated: `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.PreviewRuntime.Frame.cs`; `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.PreviewRuntime.Color.cs`
Files added: none
Net production .cs delta: -2
Partial clusters reduced: `AutomationDiagnosticsHub` -2 files
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; offline runtime snapshot harness; `git diff --check`
CLI/MCP/pipe checks, if applicable: covered by automation diagnostics preview runtime projection ownership tests and runtime snapshot regression tests
Behavior preserved: preview frame counters, estimated pipeline latency, HDR input detection, tone-map mode, color context, and adapter color metadata still map into the same flattened automation snapshot fields
Notes for future agents: keep tiny preview runtime projection groups with `PreviewRuntime.cs` unless a group grows independent policy or a reusable collaborator boundary

Date: 2026-05-21
Area: Automation diagnostics recording pipeline projection
Problem: Recording pipeline encoder, ingest, video queue, and hardware queue projection mappings lived in four tiny partials even though the recording pipeline projection owner immediately composes and flattens all four groups.
Files consolidated: `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.RecordingPipeline.Encoder.cs`; `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.RecordingPipeline.Ingest.cs`; `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.RecordingPipeline.VideoQueue.cs`; `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.RecordingPipeline.HardwareQueues.cs`
Files added: none
Net production .cs delta: -4
Partial clusters reduced: `AutomationDiagnosticsHub` -4 files
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; offline runtime snapshot harness; `git diff --check`
CLI/MCP/pipe checks, if applicable: covered by automation diagnostics recording pipeline projection ownership tests and runtime snapshot regression tests
Behavior preserved: recording encoder, ingest, video queue, GPU, and CUDA health fields still map into the same flattened automation snapshot fields
Notes for future agents: keep recording pipeline DTO mapping groups with `RecordingPipeline.cs` unless a group grows independent policy or a reusable collaborator boundary

Date: 2026-05-21
Area: Automation diagnostics recording integrity projection
Problem: Recording integrity summary, video, backpressure, audio, and A/V sync projection mappings lived in five tiny partials even though the recording integrity projection owner immediately composes and flattens all five groups.
Files consolidated: `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.RecordingIntegrity.Summary.cs`; `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.RecordingIntegrity.Video.cs`; `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.RecordingIntegrity.Backpressure.cs`; `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.RecordingIntegrity.Audio.cs`; `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.RecordingIntegrity.AvSync.cs`
Files added: none
Net production .cs delta: -5
Partial clusters reduced: `AutomationDiagnosticsHub` -5 files
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; offline runtime snapshot harness; `git diff --check`
CLI/MCP/pipe checks, if applicable: covered by recording integrity automation projection ownership tests and runtime snapshot regression tests
Behavior preserved: recording integrity status, reason, video counters, backpressure, audio integrity, and A/V sync fields still map into the same flattened automation snapshot fields
Notes for future agents: keep recording integrity DTO mapping groups with `RecordingIntegrity.cs` unless a group grows independent policy or a reusable collaborator boundary

Date: 2026-05-21
Area: Automation diagnostics MJPEG preview jitter projection
Problem: MJPEG preview jitter queue, timing, adaptive, and event projection mappings lived in four tiny partials even though the preview jitter projection owner immediately composes and flattens all four groups.
Files consolidated: `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.MjpegPreviewJitter.Queue.cs`; `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.MjpegPreviewJitter.Timing.cs`; `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.MjpegPreviewJitter.Adaptive.cs`; `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.MjpegPreviewJitter.Events.cs`
Files added: none
Net production .cs delta: -4
Partial clusters reduced: `AutomationDiagnosticsHub` -4 files
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; offline runtime snapshot harness; `git diff --check`
CLI/MCP/pipe checks, if applicable: covered by automation diagnostics MJPEG projection ownership tests and runtime snapshot regression tests
Behavior preserved: MJPEG preview jitter queue, timing, adaptive, and scheduler event fields still map into the same flattened automation snapshot fields
Notes for future agents: keep MJPEG preview jitter DTO mapping groups with `MjpegPreviewJitter.cs` unless a group grows independent policy or a reusable collaborator boundary

Date: 2026-05-21
Area: Automation diagnostics Flashback recording projection
Problem: Flashback recording startup-cache, runtime, backend, and encoder projection mappings lived in four tiny partials even though the Flashback recording projection owner immediately composes and flattens those groups; the larger queue/backpressure mapping remains its own focused owner.
Files consolidated: `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.FlashbackRecording.StartupCache.cs`; `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.FlashbackRecording.Runtime.cs`; `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.FlashbackRecording.Backend.cs`; `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.FlashbackRecording.Encoder.cs`
Files added: none
Net production .cs delta: -4
Partial clusters reduced: `AutomationDiagnosticsHub` -4 files
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; offline runtime snapshot harness; `git diff --check`
CLI/MCP/pipe checks, if applicable: covered by automation diagnostics Flashback recording projection ownership tests and runtime snapshot regression tests
Behavior preserved: Flashback recording startup-cache, runtime, backend, codec downgrade, export verification, and encoder fields still map into the same flattened automation snapshot fields
Notes for future agents: keep smaller Flashback recording DTO mapping groups with `FlashbackRecording.cs`; keep queue/backpressure mapping separate unless it can be folded without making the owner hard to scan

Date: 2026-05-21
Area: Automation diagnostics preview runtime projection
Problem: Preview runtime surface visibility and GPU playback projection mappings lived in tiny partials even though the preview runtime projection owner immediately composes and flattens both groups with the rest of the preview runtime DTO.
Files consolidated: `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.PreviewRuntime.Surface.cs`; `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.PreviewRuntime.GpuPlayback.cs`
Files added: none
Net production .cs delta: -2
Partial clusters reduced: `AutomationDiagnosticsHub` -2 files
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; offline runtime snapshot harness; `git diff --check`
CLI/MCP/pipe checks, if applicable: covered by automation diagnostics preview runtime projection ownership tests and runtime snapshot regression tests
Behavior preserved: preview surface visibility, renderer attachment, GPU playback state, natural size, position, and position-event fields still map into the same flattened automation snapshot fields
Notes for future agents: keep tiny preview runtime DTO mapping groups with `PreviewRuntime.cs`; keep cadence/startup separate while they remain more semantic scan units

Date: 2026-05-21
Area: Automation diagnostics audio projection
Problem: Audio signal projection lived in a tiny partial even though the audio/ingest projection owner immediately composes and flattens it with the rest of the audio DTO.
Files consolidated: `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Audio.Signal.cs`
Files added: none
Net production .cs delta: -1
Partial clusters reduced: `AutomationDiagnosticsHub` -1 file
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; offline runtime snapshot harness; `git diff --check`
CLI/MCP/pipe checks, if applicable: covered by automation diagnostics audio projection ownership tests and runtime snapshot regression tests
Behavior preserved: audio peak, clipping, signal-present, and muted-suspected fields still map into the same flattened automation snapshot fields
Notes for future agents: keep audio signal DTO mapping with `Audio.cs`; keep audio drop accounting separate because it is composed independently from capture health

Date: 2026-05-21
Area: Automation diagnostics signal alerts
Problem: Capture cadence, audio muted, and recording growth signal alert rules lived in tiny partials separate from the signal alert owner even though they all update alert state from the same automation snapshot surface.
Files consolidated: `Sussudio/Services/Automation/AutomationDiagnosticsHub.SignalAlerts.Capture.cs`; `Sussudio/Services/Automation/AutomationDiagnosticsHub.SignalAlerts.AudioRecording.cs`
Files added: none
Net production .cs delta: -2
Partial clusters reduced: `AutomationDiagnosticsHub` -2 files
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; offline runtime snapshot harness; `git diff --check`
CLI/MCP/pipe checks, if applicable: covered by automation diagnostics alert ownership tests and runtime snapshot regression tests
Behavior preserved: capture, audio-muted, and recording-growth alert state transitions still call `SetAlertState` with the same IDs, severities, categories, messages, and throttle settings
Notes for future agents: keep lightweight snapshot-driven signal alert rules with `SignalAlerts.Preview.cs` unless a rule family grows independent state or policy

Date: 2026-05-21
Area: Automation diagnostics Flashback evaluation
Problem: Flashback temp-storage pressure verdict construction lived in a tiny partial separate from the Flashback diagnostic evaluation ordering that calls it first.
Files consolidated: `Sussudio/Services/Automation/AutomationDiagnosticsHub.DiagnosticEvaluationFlashback.Storage.cs`
Files added: none
Net production .cs delta: -1
Partial clusters reduced: `AutomationDiagnosticsHub` -1 file
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; offline runtime snapshot harness; `git diff --check`
CLI/MCP/pipe checks, if applicable: covered by automation diagnostics evaluation ownership tests and runtime snapshot regression tests
Behavior preserved: Flashback storage-pressure diagnostic verdict still uses the same active/startup-cache/temp-drive thresholds, severity, category, message, and lane mapping
Notes for future agents: keep first-branch Flashback diagnostic verdicts with the Flashback evaluation ordering unless the verdict family grows independent policy

Date: 2026-05-21
Area: Automation diagnostics HDR projection
Problem: Preview HDR state and HDR truth classification lived in separate partials even though both project HDR diagnostics from the same capture, view-model, preview, and verification evidence surface.
Files consolidated: `Sussudio/Services/Automation/AutomationDiagnosticsHub.Hdr.Truth.cs`; `Sussudio/Services/Automation/AutomationDiagnosticsHub.Hdr.Preview.cs`
Files added: `Sussudio/Services/Automation/AutomationDiagnosticsHub.Hdr.cs`
Net production .cs delta: -1
Partial clusters reduced: `AutomationDiagnosticsHub` -1 file
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; offline runtime snapshot harness; `git diff --check`
CLI/MCP/pipe checks, if applicable: covered by automation diagnostics runtime ownership tests and runtime snapshot regression tests
Behavior preserved: preview HDR input/tone-map projection and HDR truth classification still use the same pixel-format, UI request, GPU-active, source-HDR, and verification metadata inputs
Notes for future agents: keep HDR diagnostics projection together unless preview HDR state or truth classification grows an independent collaborator boundary

Date: 2026-05-21
Area: Automation diagnostics PreviewD3D projection
Problem: D3D pipeline-latency projection lived in a 23-line partial even though the PreviewD3D projection owner composes it immediately and flattens its values into the same latency-and-stats DTO.
Files consolidated: `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.PreviewD3D.PipelineLatency.cs`
Files added: none
Net production .cs delta: -1
Partial clusters reduced: `AutomationDiagnosticsHub` -1 file
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; offline runtime snapshot harness; `git diff --check`
CLI/MCP/pipe checks, if applicable: covered by automation diagnostics PreviewD3D projection ownership tests and runtime snapshot regression tests
Behavior preserved: PreviewD3D pipeline-latency sample count, average, p95, p99, and max values still map from `PreviewRuntimeSnapshot` into the same flattened automation snapshot fields
Notes for future agents: keep tiny metric projection builders with the PreviewD3D projection owner unless the metric family grows independent policy or reusable behavior

Date: 2026-05-21
Area: Automation diagnostics snapshot refresh
Problem: Public latest-snapshot/read-refresh entry points and refresh-gate serialization lived in a tiny partial separate from the snapshot refresh core they guard.
Files consolidated: `Sussudio/Services/Automation/AutomationDiagnosticsHub.Snapshots.Access.cs`
Files added: none
Net production .cs delta: -1
Partial clusters reduced: `AutomationDiagnosticsHub` -1 file
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; offline runtime snapshot harness; `git diff --check`
CLI/MCP/pipe checks, if applicable: covered by automation diagnostics refresh pipeline ownership tests, command dispatcher source tests, MCP timeline contract tests, and runtime snapshot regression tests
Behavior preserved: `GetLatestSnapshot`, `RefreshSnapshotNowAsync`, and refresh gate serialization still wrap the same latest snapshot state and `RefreshSnapshotCoreAsync` path
Notes for future agents: keep public snapshot refresh entry points with `Snapshots.cs` unless refresh coordination becomes a named service boundary

Date: 2026-05-21
Area: Automation diagnostics Flashback playback alerts
Problem: Flashback playback performance alert routing and frame-submission failure alert logic lived in a tiny router partial separate from the alert orchestration root that calls it.
Files consolidated: `Sussudio/Services/Automation/AutomationDiagnosticsHub.FlashbackPlaybackPerformanceAlerts.cs`
Files added: none
Net production .cs delta: -1
Partial clusters reduced: `AutomationDiagnosticsHub` -1 file
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; offline runtime snapshot harness; `git diff --check`
CLI/MCP/pipe checks, if applicable: covered by automation diagnostics alert ownership tests and runtime snapshot regression tests
Behavior preserved: Flashback playback cadence/audio performance routing and `flashback-playback-submit-failures` alert state still use the same active-state, target FPS, severity, category, message, recovery text, and throttle
Notes for future agents: keep one-method Flashback playback performance routers with `Alerts.cs` unless the routing becomes independent policy

Date: 2026-05-21
Area: Automation diagnostics snapshot projection
Problem: Live A/V sync projection and flattening lived in a tiny partial even though the snapshot projection root already owns top-level projection dispatch into composition and flattening.
Files consolidated: `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.AvSync.cs`
Files added: none
Net production .cs delta: -1
Partial clusters reduced: `AutomationDiagnosticsHub` -1 file
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; offline runtime snapshot harness; `git diff --check`
CLI/MCP/pipe checks, if applicable: covered by automation diagnostics system projection ownership tests and runtime snapshot regression tests
Behavior preserved: A/V sync capture drift, drift rate, encoder drift, and encoder correction samples still map from `CaptureRuntimeSnapshot` into the same flattened automation snapshot fields
Notes for future agents: keep tiny top-level system projection leaves with `SnapshotProjection.cs` unless they grow independent policy or belong to a named runtime domain owner

Date: 2026-05-21
Area: Automation diagnostics realtime counters
Problem: MJPEG recent-counter baselines lived in a tiny partial separate from the realtime preview counter owner that already tracks preview jitter and D3D deltas for the same snapshot refresh loop.
Files consolidated: `Sussudio/Services/Automation/AutomationDiagnosticsHub.Counters.Mjpeg.cs`
Files added: none
Net production .cs delta: -1
Partial clusters reduced: `AutomationDiagnosticsHub` -1 file
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; offline runtime snapshot harness; `git diff --check`
CLI/MCP/pipe checks, if applicable: covered by automation diagnostics counter ownership tests, preview pacing ownership tests, and runtime snapshot regression tests
Behavior preserved: MJPEG recent dropped, decode failure, emit failure, and compressed queue drop deltas still use the same interlocked baselines and reset semantics
Notes for future agents: keep realtime snapshot-loop counter baselines with `Counters.RealtimePreview.cs` unless a counter family grows independent lifecycle policy

Date: 2026-05-21
Area: Automation diagnostics realtime evaluation
Problem: Idle and warmup diagnostic verdicts lived in a tiny partial separate from the realtime diagnostic verdict ordering that always evaluates them first.
Files consolidated: `Sussudio/Services/Automation/AutomationDiagnosticsHub.DiagnosticEvaluationRealtime.State.cs`
Files added: none
Net production .cs delta: -1
Partial clusters reduced: `AutomationDiagnosticsHub` -1 file
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; offline runtime snapshot harness; `git diff --check`
CLI/MCP/pipe checks, if applicable: covered by automation diagnostics evaluation ownership tests and runtime snapshot regression tests
Behavior preserved: idle and warming-up `diagnostic_unavailable` verdicts keep the same severities, messages, details, and lane mappings
Notes for future agents: keep first-branch realtime state verdicts with `DiagnosticEvaluationRealtime.cs` unless they grow independent policy

Date: 2026-05-21
Area: Automation diagnostics capture-format projection
Problem: HDR-request and actual capture-format projection groups lived in tiny partials even though the capture-format projection owner immediately composes and flattens them with the rest of the format DTO.
Files consolidated: `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.CaptureFormat.HdrRequest.cs`; `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.CaptureFormat.Actual.cs`
Files added: none
Net production .cs delta: -2
Partial clusters reduced: `AutomationDiagnosticsHub` -2 files
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; offline runtime snapshot harness; `git diff --check`
CLI/MCP/pipe checks, if applicable: covered by automation diagnostics capture-format projection ownership tests and runtime snapshot regression tests
Behavior preserved: HDR activation/auto-downgrade fields and actual capture dimensions/frame-rate still map from `CaptureRuntimeSnapshot` into the same flattened automation snapshot fields
Notes for future agents: keep tiny capture-format projection groups with `CaptureFormat.cs` unless a group grows independent policy or a reusable collaborator boundary

Date: 2026-05-21
Area: Automation diagnostics HDR pipeline projection
Problem: HDR pipeline policy projection and final flattened DTO field projection lived in separate partials even though the flattening is a direct one-to-one projection of HDR runtime, warmup, pipeline-mode, telemetry-alignment, and truth-verdict fields.
Files consolidated: `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.HdrPipeline.cs`
Files added: none
Net production .cs delta: -1
Partial clusters reduced: `AutomationDiagnosticsHub` -1 file
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; offline runtime snapshot harness; `git diff --check`
CLI/MCP/pipe checks, if applicable: covered by automation diagnostics HDR pipeline projection ownership tests and runtime snapshot regression tests
Behavior preserved: HDR pipeline automation fields still flow through typed projection records before final DTO initialization
Notes for future agents: keep direct HDR pipeline flattening beside the HDR pipeline projection unless HDR runtime policy grows a separate owner

Date: 2026-05-21
Area: Automation diagnostics settings and Flashback export projection
Problem: Settings and Flashback export final flattened DTO field projection lived in separate partials even though each flattening step is a direct one-to-one projection from its matching typed projection records.
Files consolidated: `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.Settings.cs`; `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.FlashbackExport.cs`
Files added: none
Net production .cs delta: -2
Partial clusters reduced: `AutomationDiagnosticsHub` -2 files
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; offline runtime snapshot harness; `git diff --check`
CLI/MCP/pipe checks, if applicable: covered by automation diagnostics settings/Flashback export projection ownership tests and runtime snapshot regression tests
Behavior preserved: settings and Flashback export automation fields still flow through typed projection records before final DTO initialization
Notes for future agents: keep direct settings and Flashback export flattening beside their projection owners unless either grows real cross-domain policy

Date: 2026-05-21
Area: Automation diagnostics source, visual cadence, and snapshot evaluation projection
Problem: Source signal, visual cadence, and snapshot evaluation final flattened DTO field projections lived in separate partials even though each flattening step is a direct one-to-one projection from its matching typed projection records.
Files consolidated: `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.Source.Signal.cs`; `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.VisualCadence.cs`; `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.SnapshotEvaluation.cs`
Files added: none
Net production .cs delta: -3
Partial clusters reduced: `AutomationDiagnosticsHub` -3 files
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; offline runtime snapshot harness; `git diff --check`
CLI/MCP/pipe checks, if applicable: covered by automation diagnostics source/visual cadence/snapshot evaluation ownership tests and runtime snapshot regression tests
Behavior preserved: source, visual cadence, and snapshot evaluation automation fields still flow through typed projection records before final DTO initialization
Notes for future agents: keep direct source, visual cadence, and snapshot evaluation flattening beside their projection owners unless any grows real cross-domain policy

Date: 2026-05-21
Area: Automation diagnostics performance timeline projection
Problem: Core and system performance timeline field projection lived in tiny partials even though both are direct one-to-one inputs to the final `PerformanceTimelineEntry` builder.
Files consolidated: `Sussudio/Services/Automation/AutomationDiagnosticsHub.TimelineProjection.Core.cs`; `Sussudio/Services/Automation/AutomationDiagnosticsHub.TimelineProjection.System.cs`
Files added: none
Net production .cs delta: -2
Partial clusters reduced: `AutomationDiagnosticsHub` -2 files
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; offline runtime snapshot harness; `git diff --check`
CLI/MCP/pipe checks, if applicable: covered by automation diagnostics refresh-pipeline ownership tests, MCP performance timeline projection contract tests, and runtime snapshot regression tests
Behavior preserved: performance timeline entries still flow through typed core/system projection records before final DTO initialization
Notes for future agents: keep direct core/system timeline projection beside the timeline entry builder unless either grows independent policy

Date: 2026-05-21
Area: Automation diagnostics Flashback playback timeline projection
Problem: Flashback playback performance timeline projection was split across six tiny partials even though each group is a direct field projection from the same `AutomationSnapshot` into the same grouped timeline record.
Files consolidated: `Sussudio/Services/Automation/AutomationDiagnosticsHub.TimelineProjection.FlashbackPlayback.Cadence.cs`; `Sussudio/Services/Automation/AutomationDiagnosticsHub.TimelineProjection.FlashbackPlayback.Decode.cs`; `Sussudio/Services/Automation/AutomationDiagnosticsHub.TimelineProjection.FlashbackPlayback.Commands.cs`; `Sussudio/Services/Automation/AutomationDiagnosticsHub.TimelineProjection.FlashbackPlayback.AudioMaster.cs`; `Sussudio/Services/Automation/AutomationDiagnosticsHub.TimelineProjection.FlashbackPlayback.Stages.cs`; `Sussudio/Services/Automation/AutomationDiagnosticsHub.TimelineProjection.FlashbackPlayback.Backend.cs`
Files added: none
Net production .cs delta: -6
Partial clusters reduced: `AutomationDiagnosticsHub` -6 files
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; offline runtime snapshot harness; `git diff --check`
CLI/MCP/pipe checks, if applicable: covered by automation diagnostics refresh-pipeline ownership tests, MCP performance timeline projection contract tests, and runtime snapshot regression tests
Behavior preserved: Flashback playback timeline entries still flow through typed grouped projection records before final DTO initialization
Notes for future agents: keep direct Flashback playback timeline field groups beside the Flashback playback timeline projection owner unless a group grows independent policy

Date: 2026-05-21
Area: Automation diagnostics Flashback export timeline projection
Problem: Flashback export performance timeline projection lived in a tiny partial even though it is a direct field projection from `AutomationSnapshot` into the final `PerformanceTimelineEntry` builder.
Files consolidated: `Sussudio/Services/Automation/AutomationDiagnosticsHub.TimelineProjection.FlashbackExport.cs`
Files added: none
Net production .cs delta: -1
Partial clusters reduced: `AutomationDiagnosticsHub` -1 file
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; offline runtime snapshot harness; `git diff --check`
CLI/MCP/pipe checks, if applicable: covered by automation diagnostics refresh-pipeline ownership tests, MCP performance timeline projection contract tests, and runtime snapshot regression tests
Behavior preserved: Flashback export timeline fields still flow through a typed projection record before final DTO initialization
Notes for future agents: keep direct Flashback export timeline projection beside the timeline entry builder unless it grows independent policy

Date: 2026-05-21
Area: Automation diagnostics realtime source lane formatting
Problem: Source cadence and source-signal diagnostic lane formatting lived in a tiny partial separate from the diagnostic lane orchestration and neighboring lane formatting helpers.
Files consolidated: `Sussudio/Services/Automation/AutomationDiagnosticsHub.DiagnosticEvaluationLanes.Realtime.Source.cs`
Files added: none
Net production .cs delta: -1
Partial clusters reduced: `AutomationDiagnosticsHub` -1 file
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; offline runtime snapshot harness; `git diff --check`
CLI/MCP/pipe checks, if applicable: covered by automation diagnostics evaluation ownership tests and runtime snapshot regression tests
Behavior preserved: source and source-signal diagnostic lane text still feeds the same `DiagnosticEvaluationLanes` record before diagnostic verdict construction
Notes for future agents: keep lightweight diagnostic lane text builders with `DiagnosticEvaluationLanes.cs` unless a lane grows independent policy

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
Area: Automation diagnostics MJPEG packet hash projection
Problem: MJPEG packet hash projection and final flattened DTO field projection lived in separate tiny partials even though the flattening is a direct one-to-one projection of packet duplicate and unique-frame fields.
Files consolidated: `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.MjpegPacketHash.cs`
Files added: none
Net production .cs delta: -1
Partial clusters reduced: `AutomationDiagnosticsHub` -1 file
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; offline runtime snapshot harness; `git diff --check`
CLI/MCP/pipe checks, if applicable: covered by automation diagnostics MJPEG projection ownership tests and runtime snapshot regression tests
Behavior preserved: MJPEG packet hash automation fields still flow through typed projection records before final DTO initialization
Notes for future agents: keep direct MJPEG packet hash flattening beside the packet hash projection unless the flattening policy grows shared logic

Date: 2026-05-21
Area: Automation diagnostics capture command projection
Problem: Capture command projection and final flattened DTO field projection lived in separate tiny partials even though the flattening is a direct one-to-one projection of command queue counters, latency, and last-command fields.
Files consolidated: `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.CaptureCommands.cs`
Files added: none
Net production .cs delta: -1
Partial clusters reduced: `AutomationDiagnosticsHub` -1 file
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; offline runtime snapshot harness; `git diff --check`
CLI/MCP/pipe checks, if applicable: covered by automation diagnostics capture command projection ownership tests and runtime snapshot regression tests
Behavior preserved: Capture command automation fields still flow through typed projection records before final DTO initialization
Notes for future agents: keep direct capture command flattening beside the capture command projection unless the flattening policy grows shared logic

Date: 2026-05-21
Area: Automation diagnostics MJPEG root projection
Problem: Root MJPEG projection and final flattened DTO field projection lived in separate tiny partials even though the flattening is a direct one-to-one projection of MJPEG root counters and queue fields.
Files consolidated: `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.Mjpeg.cs`
Files added: none
Net production .cs delta: -1
Partial clusters reduced: `AutomationDiagnosticsHub` -1 file
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; offline runtime snapshot harness; `git diff --check`
CLI/MCP/pipe checks, if applicable: covered by automation diagnostics MJPEG projection ownership tests and runtime snapshot regression tests
Behavior preserved: MJPEG root automation fields still flow through typed projection records before final DTO initialization
Notes for future agents: keep direct root MJPEG flattening beside the root MJPEG projection; timing, preview jitter, and packet hash remain separate named sub-projections

Date: 2026-05-21
Area: Automation diagnostics capture cadence projection
Problem: Source capture cadence projection and final flattened DTO field projection lived in separate tiny partials even though the flattening is a direct one-to-one projection of cadence sample, interval, gap, and drop-estimate fields.
Files consolidated: `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.CaptureCadence.cs`
Files added: none
Net production .cs delta: -1
Partial clusters reduced: `AutomationDiagnosticsHub` -1 file
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; offline runtime snapshot harness; `git diff --check`
CLI/MCP/pipe checks, if applicable: covered by automation diagnostics source cadence projection ownership tests, preview pacing ownership tests, and runtime snapshot regression tests
Behavior preserved: Capture cadence automation fields still flow through typed projection records before final DTO initialization
Notes for future agents: keep direct capture cadence flattening beside the source capture cadence projection; visual cadence remains a separate preview visual signal

Date: 2026-05-21
Area: Automation diagnostics source telemetry projection
Problem: Source telemetry fallback/age projection and final flattened DTO field projection lived in separate partials even though the flattening is a direct one-to-one projection of the telemetry fields.
Files consolidated: `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.Source.Telemetry.cs`
Files added: none
Net production .cs delta: -1
Partial clusters reduced: `AutomationDiagnosticsHub` -1 file
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; offline runtime snapshot harness; `git diff --check`
CLI/MCP/pipe checks, if applicable: covered by automation diagnostics source telemetry projection ownership tests and runtime snapshot regression tests
Behavior preserved: Source telemetry automation fields still flow through fallback/age projection records before final DTO initialization
Notes for future agents: keep direct source telemetry flattening beside the source telemetry projection; source signal aggregate remains responsible for composing signal plus telemetry flattened projections

Date: 2026-05-21
Area: Automation diagnostics recording output projection
Problem: Recording backend/output projection and final flattened DTO field projection lived in separate partials even though the flattening only combines the backend and output records owned by the same projection file.
Files consolidated: `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.RecordingOutput.cs`
Files added: none
Net production .cs delta: -1
Partial clusters reduced: `AutomationDiagnosticsHub` -1 file
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; offline runtime snapshot harness; `git diff --check`
CLI/MCP/pipe checks, if applicable: covered by automation diagnostics recording projection ownership tests and runtime snapshot regression tests
Behavior preserved: Recording backend and output automation fields still flow through typed projection records before final DTO initialization
Notes for future agents: keep direct recording backend/output flattening beside the recording output projection unless the backend or output policy grows a separate owner

Date: 2026-05-21
Area: Automation diagnostics MJPEG timing projection
Problem: MJPEG timing projection and final flattened DTO field projection lived in separate partials even though the flattening is a direct one-to-one projection of timing and per-decoder fields.
Files consolidated: `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.MjpegTiming.cs`
Files added: none
Net production .cs delta: -1
Partial clusters reduced: `AutomationDiagnosticsHub` -1 file
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; offline runtime snapshot harness; `git diff --check`
CLI/MCP/pipe checks, if applicable: covered by automation diagnostics MJPEG projection ownership tests and runtime snapshot regression tests
Behavior preserved: MJPEG timing automation fields still flow through typed projection records before final DTO initialization
Notes for future agents: keep direct MJPEG timing flattening beside the MJPEG timing projection unless timing aggregation grows a separate policy owner

Date: 2026-05-21
Area: Automation diagnostics process resource projection
Problem: Process resource projection and final flattened DTO field projection lived in separate partials even though the flattening is a direct one-to-one projection of memory, CPU, GC, and thread-pool fields.
Files consolidated: `Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.ProcessResources.cs`
Files added: none
Net production .cs delta: -1
Partial clusters reduced: `AutomationDiagnosticsHub` -1 file
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; offline runtime snapshot harness; `git diff --check`
CLI/MCP/pipe checks, if applicable: covered by automation diagnostics system projection ownership tests and runtime snapshot regression tests
Behavior preserved: Process resource automation fields still flow through typed projection records before final DTO initialization
Notes for future agents: keep direct process resource flattening beside the process resource projection unless process metrics gain a richer aggregation policy

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

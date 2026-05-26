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

Date: 2026-05-24
Area: ssctl diagnostic command locality
Problem: Diagnostic tooling commands were split across observability, PresentMon, and diagnostic-session handler files even though they form one CLI investigation surface over shared tool helpers.
Files consolidated: `tools/ssctl/CommandHandlers.PresentMon.cs`; `tools/ssctl/CommandHandlers.DiagnosticSession.cs`
Files added: none
Net production .cs delta: -2
Partial clusters reduced: `CommandHandlers` -2 files
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; offline runtime snapshot harness; `git diff --check`
CLI/MCP/pipe checks, if applicable: affected `ssctl` build covered command-handler binding and shared tool references
Behavior preserved: `presentmon`, `diagnostic-session`, state/diagnostics/options/manifest/timeline/memory/audio-ramp command names, flags, payloads, and output formatting remain unchanged
Notes for future agents: superseded on 2026-05-25 by the ssctl command-handler consolidation; keep diagnostic tooling commands in `CommandHandlers.cs` unless a command becomes an independently tested workflow or shared helper

Date: 2026-05-24
Area: NativeXuAudioProbe I2C command family locality
Problem: The exploratory `i2c-cmd` probe surface was split across one router plus four tiny subcommand partials, forcing five files to understand one CLI command family.
Files consolidated: `tools/NativeXuAudioProbe/Program.I2cCommands.SelectorProbe.cs`; `tools/NativeXuAudioProbe/Program.I2cCommands.HighSelectorProbe.cs`; `tools/NativeXuAudioProbe/Program.I2cCommands.TopologyProbe.cs`; `tools/NativeXuAudioProbe/Program.I2cCommands.Verify.cs`
Files added: none
Net production .cs delta: -4
Partial clusters reduced: `NativeXuProbeI2cCommands` -4 files
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; offline runtime snapshot harness; `git diff --check`
CLI/MCP/pipe checks, if applicable: affected `NativeXuAudioProbe` build covered the consolidated exploratory CLI command family
Behavior preserved: `i2c-cmd` subcommands, routing names, direct KS/XU calls, SET/readback/restore flow, and topology/selector probe behavior remain unchanged
Notes for future agents: keep small NativeXu exploratory subcommands in `Program.I2cCommands.cs` while they are one CLI command family; split again only for a reusable transport or independently tested workflow

Date: 2026-05-24
Area: NativeXuAudioProbe runtime shim locality
Problem: Probe-local `Logger` and `CaptureDevice` shims lived in a 15-line standalone file even though they only exist to support the probe entrypoint's linked app-service sources.
Files consolidated: `tools/NativeXuAudioProbe/ToolRuntimeShims.cs`
Files added: none
Net production .cs delta: -1
Partial clusters reduced: none
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; offline runtime snapshot harness; `git diff --check`
CLI/MCP/pipe checks, if applicable: affected `NativeXuAudioProbe` build covered the linked-source shim binding
Behavior preserved: probe-local `Logger`, global `CaptureDevice`, `NativeXuInterfacePath`, and linked service-source compatibility remain unchanged
Notes for future agents: keep probe-only runtime shims with `tools/NativeXuAudioProbe/Program.cs` unless they become shared by another tool or need independent test coverage

Date: 2026-05-24
Area: MCP result formatting helper locality
Problem: MCP result object creation lived in a 30-line helper file even though it is shared formatting/result shaping used by the same MCP tool-command formatter family.
Files consolidated: `tools/McpServer/Tools/McpToolResultFactory.cs`
Files added: none
Net production .cs delta: -1
Partial clusters reduced: none
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; offline runtime snapshot harness; `git diff --check`
CLI/MCP/pipe checks, if applicable: covered by MCP command-routing, tool formatter, and tool-surface tests
Behavior preserved: `McpToolResultFactory` type name, `CallToolResult` text/error shaping, error-code append behavior, and MCP tool method outputs remain unchanged
Notes for future agents: keep shared MCP response/result shaping beside `ToolCommandFormatter` unless it grows into a transport-level policy or public tool surface

Date: 2026-05-24
Area: MainWindow responsive shell layout adapter
Problem: Responsive shell layout XAML wiring lived in a 45-line MainWindow partial even though it is shell chrome/control-bar composition and only delegates to named shell layout controllers.
Files consolidated: `Sussudio/MainWindow.ResponsiveShellLayout.cs`
Files added: none
Net production .cs delta: -1
Partial clusters reduced: `MainWindow` -1 file
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; offline runtime snapshot harness; `git diff --check`
CLI/MCP/pipe checks, if applicable: covered by responsive layout ownership tests and runtime snapshot regression checks
Behavior preserved: control-bar label set, responsive layout controller wiring, setup binding call, and layout breakpoints remain unchanged
Notes for future agents: keep shell layout XAML adapters with `MainWindow.ShellChrome.Composition.cs`; layout decisions remain in the `ResponsiveShellLayoutPolicy` type inside `ResponsiveShellLayoutController.cs`

Date: 2026-05-24
Area: MainWindow screenshot adapters
Problem: Preview screenshot button wiring and whole-window automation screenshot routing shared a 38-line MainWindow partial even though each adapter belongs with an existing owner: button actions and `IAutomationWindowControl` window shell methods.
Files consolidated: `Sussudio/MainWindow.Screenshot.cs`
Files added: none
Net production .cs delta: -1
Partial clusters reduced: `MainWindow` -1 file
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; offline runtime snapshot harness; `git diff --check`
CLI/MCP/pipe checks, if applicable: covered by screenshot ownership tests, automation dispatcher visual-capture tests, and runtime snapshot regression checks
Behavior preserved: XAML `ScreenshotButton_Click`, preview screenshot controller wiring, `CaptureWindowScreenshotAsync`, cancellation, dispatcher failure handling, and image encoding remain unchanged
Notes for future agents: keep XAML button adapters with `MainWindow.ButtonActions.cs` and `IAutomationWindowControl` screenshot routing with `MainWindow.ShellChrome.Composition.cs`

Date: 2026-05-24
Area: MainWindow preview runtime snapshot adapter
Problem: Preview runtime snapshot context wiring lived in a 28-line MainWindow partial even though it is only the automation-facing adapter around preview renderer/startup composition.
Files consolidated: `Sussudio/MainWindow.PreviewRuntimeSnapshot.cs`
Files added: none
Net production .cs delta: -1
Partial clusters reduced: `MainWindow` -1 file
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; offline runtime snapshot harness; `git diff --check`
CLI/MCP/pipe checks, if applicable: covered by named-pipe automation server, preview runtime snapshot ownership, and runtime snapshot regression checks
Behavior preserved: automation preview snapshot delegate, UI-dispatch sampling controller, startup signal wiring, and XAML bindings remain unchanged
Notes for future agents: keep tiny MainWindow preview-runtime adapters with the preview renderer composition unless they become standalone controllers

Date: 2026-05-24
Area: ssctl formatter helper locality
Problem: Recent diagnostic-event output and standalone memory/GC output lived in two tiny formatter partials even though they are direct console projections using the same root result/JSON helper owner.
Files consolidated: `tools/ssctl/Formatters.Diagnostics.cs`; `tools/ssctl/Formatters.Memory.cs`
Files added: none
Net production .cs delta: -2
Partial clusters reduced: `Formatters` -2 files
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; offline runtime snapshot harness; `git diff --check`
CLI/MCP/pipe checks, if applicable: covered by ssctl formatter source-ownership tests and runtime snapshot formatter contract checks
Behavior preserved: diagnostic-event text, memory/GC text, and shared result/JSON formatting remain unchanged
Notes for future agents: keep tiny standalone ssctl console projections in `Formatters.Common.cs` unless they grow a named report section or shared formatter collaborator

Date: 2026-05-24
Area: MCP performance/preview helper locality
Problem: Three tiny MCP helper partials owned single-use details that are only meaningful inside their parent tool/report owner, forcing extra file hops for timeline projection, PresentMon snapshot correlation, and preview-frame histogram rendering.
Files consolidated: `tools/McpServer/Tools/FramePacingVerdictTools.Timeline.cs`; `tools/McpServer/Tools/PresentMonTools.Correlation.cs`; `tools/McpServer/Tools/PreviewFrameCaptureTools.Histogram.cs`
Files added: none
Net production .cs delta: -3
Partial clusters reduced: `FramePacingVerdictTools` -1 file; `PresentMonTools` -1 file; `PreviewFrameCaptureTools` -1 file
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; offline runtime snapshot harness; `git diff --check`
CLI/MCP/pipe checks, if applicable: covered by MCP frame pacing, PresentMon, preview-frame capture, and tool-surface routing tests
Behavior preserved: MCP tool names, automation command IDs, PresentMon fallback behavior, frame-pacing timeline counters, and preview-frame histogram text remain unchanged
Notes for future agents: keep single-use MCP helper code with its parent tool/report owner unless it becomes shared policy or a separately testable collaborator

Date: 2026-05-24
Area: MCP tool control/configuration wrappers
Problem: Four tiny MCP tool classes were split one class per file even where they were adjacent control/configuration surfaces with reflected class names as the real contract rather than file names.
Files consolidated: `tools/McpServer/Tools/RecordingTools.cs`; `tools/McpServer/Tools/PipelineSettingsTools.cs`
Files added: none
Net production .cs delta: -2
Partial clusters reduced: none
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; offline runtime snapshot harness; `git diff --check`
CLI/MCP/pipe checks, if applicable: reflected MCP tool class names and method names remain covered by routing/surface tests
Behavior preserved: `PreviewTools`, `RecordingTools`, `CaptureSettingsTools`, and `PipelineSettingsTools` classes and their MCP method names remain unchanged
Notes for future agents: when co-locating tiny MCP tool classes, preserve reflected type names and tool method names; file names are not the protocol surface

Date: 2026-05-24
Area: ssctl Flashback command routing
Problem: Flashback export CLI flag parsing and payload shaping lived in a 25-line partial file even though the only caller is the Flashback command router, forcing an extra file hop for one subcommand.
Files consolidated: `tools/ssctl/CommandHandlers.Flashback.Export.cs`
Files added: none
Net production .cs delta: -1
Partial clusters reduced: `CommandHandlers` -1 file
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; offline runtime snapshot harness; `git diff --check`
CLI/MCP/pipe checks, if applicable: covered by ssctl command-handler routing/help/source-ownership tests
Behavior preserved: Flashback export CLI flags, default output path, parent-directory creation, and `FlashbackExport` payload shape remain unchanged
Notes for future agents: keep small ssctl subcommand handlers with their command router unless they grow independent parsing/policy surface

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

Date: 2026-05-23
Area: Recording encoder CPU video submission
Problem: Packed NV12/P010 software-frame copy helpers lived in a tiny partial even though they are private to `SendVideoFrame`, forcing CPU video-submission review to open an extra file with no independent boundary.
Files consolidated: `Sussudio/Services/Recording/LibAvEncoder.FrameCopy.cs`
Files added: none
Net production .cs delta: -1
Partial clusters reduced: `LibAvEncoder` -1 file
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; offline runtime snapshot harness; `git diff --check`
CLI/MCP/pipe checks, if applicable: covered by LibAvEncoder source-ownership and runtime recording contract tests
Behavior preserved: CPU packed-frame validation, copy, PTS assignment, HDR side-data handoff, encoder send, and packet drain logic are unchanged
Notes for future agents: keep CPU packed-frame copy helpers with `LibAvEncoder.VideoSubmission.cs` unless they become a reusable copy policy shared by another encoder path

Date: 2026-05-23
Area: Recording sink video queue ownership
Problem: Video/GPU/CUDA remaining-buffer cleanup and pooled packet return helpers lived in a separate small partial even though they operate on the packet records and queue-depth state owned by video queue submission.
Files consolidated: `Sussudio/Services/Recording/LibAvRecordingSink.QueueCleanup.cs`
Files added: none
Net production .cs delta: -1
Partial clusters reduced: `LibAvRecordingSink` -1 file
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; offline runtime snapshot harness; `git diff --check`
CLI/MCP/pipe checks, if applicable: covered by recording queue source-ownership and runtime recording contract tests
Behavior preserved: Queue overload handling, depth accounting, remaining video/GPU/CUDA buffer return, pooled byte-buffer return, and lease disposal logic are unchanged
Notes for future agents: keep video queue cleanup with `LibAvRecordingSink.VideoQueueSubmission.cs` unless queue cleanup grows an independent lifecycle policy shared beyond video/GPU/CUDA queues

Date: 2026-05-23
Area: Recording encoder core diagnostics
Problem: Generic open-state guards, FFmpeg error helpers, structured libav exceptions, and D3D11 device-removed detection lived in a small partial even though they are core encoder invariants used across initialization, submission, rotation, cleanup, audio, and hardware paths.
Files consolidated: `Sussudio/Services/Recording/LibAvEncoder.Diagnostics.cs`
Files added: none
Net production .cs delta: -1
Partial clusters reduced: `LibAvEncoder` -1 file
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; offline runtime snapshot harness; `git diff --check`
CLI/MCP/pipe checks, if applicable: covered by LibAvEncoder diagnostics reflection and source-ownership tests
Behavior preserved: Open-state validation, FFmpeg error message formatting, exception logging, and D3D11 TDR detection logic are unchanged
Notes for future agents: keep generic encoder guard/error helpers with `LibAvEncoder.cs`; only move device-specific policy out if it becomes a reusable collaborator with tests

Date: 2026-05-23
Area: D3D11 preview render-pass present accounting
Problem: The shared swap-chain present/accounting transaction lived in a one-method partial even though it is called directly by the VideoProcessor, NV12 shader, and HDR shader render-pass paths.
Files consolidated: `Sussudio/Services/Preview/D3D11PreviewRenderer.Present.cs`
Files added: none
Net production .cs delta: -1
Partial clusters reduced: `D3D11PreviewRenderer` -1 file
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; offline runtime snapshot harness; `git diff --check`
CLI/MCP/pipe checks, if applicable: covered by D3D11 preview renderer source-ownership and diagnostics contract tests
Behavior preserved: Screenshot-before-present ordering, swap-chain present error handling, first-frame notification, present cadence, DXGI statistics, frame ownership, pipeline-latency tracking, render CPU timing, and slow-frame diagnostics are unchanged
Notes for future agents: keep present/accounting with `D3D11PreviewRenderer.RenderPasses.cs` unless it grows independent policy beyond render-pass completion

Date: 2026-05-23
Area: D3D11 preview render-thread frame-latency pacing
Problem: Waitable swap-chain frame-latency setup and wait helpers lived in a tiny partial even though the wait is part of render-thread frame dequeue and dispatch pacing.
Files consolidated: `Sussudio/Services/Preview/D3D11PreviewRenderer.FrameLatency.cs`
Files added: none
Net production .cs delta: -1
Partial clusters reduced: `D3D11PreviewRenderer` -1 file
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; offline runtime snapshot harness; `git diff --check`
CLI/MCP/pipe checks, if applicable: covered by D3D11 preview renderer source-ownership and diagnostics contract tests
Behavior preserved: Waitable swap-chain setup, 8ms wait timeout, wait-result metrics, and unexpected-result logging are unchanged
Notes for future agents: keep waitable frame-latency pacing with `D3D11PreviewRenderer.RenderThread.cs` unless it becomes a reusable wait policy independent of render-thread dispatch

Date: 2026-05-23
Area: Automation diagnostics timeline projection locality
Problem: Preview and Flashback playback performance timeline projection still lived in separate partial files after their smaller projection fragments had already been consolidated, forcing readers to leave the timeline ring/builder file to understand direct `AutomationSnapshot` to `PerformanceTimelineEntry` field flow.
Files consolidated: `Sussudio/Services/Automation/AutomationDiagnosticsHub.TimelineProjection.Preview.cs`; `Sussudio/Services/Automation/AutomationDiagnosticsHub.TimelineProjection.FlashbackPlayback.cs`
Files added: none
Net production .cs delta: -2
Partial clusters reduced: `AutomationDiagnosticsHub` -2 files
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; offline runtime snapshot harness; `git diff --check`
CLI/MCP/pipe checks, if applicable: covered by automation diagnostics refresh-pipeline ownership tests, diagnostic-session preview tests, MCP performance timeline projection contract tests, preview pacing ownership tests, snapshot model projection checks, and runtime snapshot regression tests
Behavior preserved: Preview and Flashback playback timeline entries still flow through the same typed projection records before final DTO initialization
Notes for future agents: keep direct timeline projection records beside `BuildPerformanceTimelineEntry` unless a group grows independent policy or reusable ownership

Date: 2026-05-23
Area: MainWindow audio adapter locality
Problem: Audio binding setup, audio-meter adapter calls, and microphone-control adapter calls lived in three small MainWindow partials even though they are all XAML-facing adapters over the same audio/microphone controller cluster.
Files consolidated: `Sussudio/MainWindow.AudioMeter.cs`; `Sussudio/MainWindow.MicrophoneControls.cs`
Files added: none
Net production .cs delta: -2
Partial clusters reduced: `MainWindow` -2 files
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; offline runtime snapshot harness; `git diff --check`
CLI/MCP/pipe checks, if applicable: covered by MainWindow audio/controller ownership tests and runtime snapshot regression tests
Behavior preserved: Audio/microphone binding, presentation, meter, and row-animation behavior still route through the same controller types
Notes for future agents: keep XAML-facing audio/microphone adapter calls together in `MainWindow.AudioBindings.cs`; keep policy, animation state, and UI projection behavior in the audio controllers

Date: 2026-05-23
Area: MainWindow preview startup adapter locality
Problem: The XAML-facing preview startup adapter was split across session, readiness-signal, and watchdog MainWindow partials even though all three only wire the same startup controller family into MainWindow callbacks and state projections.
Files consolidated: `Sussudio/MainWindow.PreviewStartup.Signals.Composition.cs`; `Sussudio/MainWindow.PreviewStartup.Watchdog.cs`
Files added: none
Net production .cs delta: -2
Partial clusters reduced: `MainWindow` -2 files
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; offline runtime snapshot harness; `git diff --check`
CLI/MCP/pipe checks, if applicable: covered by preview startup session, signal, watchdog, and runtime snapshot regression tests
Behavior preserved: Preview startup session, signal, and watchdog behavior still route through the same controller types and callback delegates
Notes for future agents: keep preview startup MainWindow adapter glue together in `MainWindow.PreviewStartup.Session.Composition.cs`; keep state machines, timers, readiness logic, and formatting in the preview startup controllers

Date: 2026-05-24
Area: MainWindow recording adapter locality
Problem: Recording button action glue and recording state/chrome glue lived in separate MainWindow partials even though both are XAML-facing adapters around the same recording button controller family.
Files consolidated: `Sussudio/MainWindow.PropertyChangedRecording.cs`
Files added: none
Net production .cs delta: -1
Partial clusters reduced: `MainWindow` -1 file
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; offline runtime snapshot harness; `git diff --check`
CLI/MCP/pipe checks, if applicable: covered by MainWindow recording controller ownership tests and runtime snapshot regression tests
Behavior preserved: Recording action, chrome, state presentation, and property-change routing still use the same recording controllers and policy types
Notes for future agents: keep recording button/state MainWindow adapter glue in `MainWindow.ButtonActions.cs`; keep record-button behavior and lockout policy in the recording controllers

Date: 2026-05-24
Area: MCP performance timeline row projection locality
Problem: One private timeline row DTO and one JSON-to-row projection path were split across eight tiny partial fragments by field group, forcing MCP timeline review to open many files for one table-shaping behavior.
Files consolidated: `tools/McpServer/Tools/PerformanceTimelineTools.Rows.Preview.cs`; `tools/McpServer/Tools/PerformanceTimelineTools.Rows.FlashbackPlayback.cs`; `tools/McpServer/Tools/PerformanceTimelineTools.Rows.FlashbackExport.cs`; `tools/McpServer/Tools/PerformanceTimelineTools.Rows.System.cs`; `tools/McpServer/Tools/PerformanceTimelineTools.Rows.Model.Preview.cs`; `tools/McpServer/Tools/PerformanceTimelineTools.Rows.Model.FlashbackPlayback.cs`; `tools/McpServer/Tools/PerformanceTimelineTools.Rows.Model.FlashbackExport.cs`; `tools/McpServer/Tools/PerformanceTimelineTools.Rows.Model.System.cs`
Files added: none
Net production .cs delta: -8
Partial clusters reduced: `PerformanceTimelineTools` -8 files
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; offline runtime snapshot harness; `git diff --check`
CLI/MCP/pipe checks, if applicable: covered by MCP performance timeline source-ownership/projection contract tests and runtime snapshot regression tests
Behavior preserved: Timeline JSON fields still populate the same private row properties before rendering and trend summaries
Notes for future agents: superseded by the later MCP performance timeline report locality consolidation; keep the private row DTO and JSON projection methods with the timeline report owner unless a projection group grows independent parsing policy

Date: 2026-05-24
Area: MCP performance timeline Flashback trend rendering
Problem: Flashback export trend text lived in a 15-line partial even though it is only the final subsection of Flashback trend rendering.
Files consolidated: `tools/McpServer/Tools/PerformanceTimelineTools.Rendering.Trend.Flashback.Export.cs`
Files added: none
Net production .cs delta: -1
Partial clusters reduced: `PerformanceTimelineTools` -1 file
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; offline runtime snapshot harness; `git diff --check`
CLI/MCP/pipe checks, if applicable: covered by MCP performance timeline source-ownership/rendering/projection contract tests and runtime snapshot regression tests
Behavior preserved: Flashback export trend text, order, formatting helpers, and first-vs-last comparisons are unchanged
Notes for future agents: superseded by the later MCP timeline rendering consolidation; keep Flashback playback and export trend text together inside `PerformanceTimelineTools.Rendering.cs` unless Flashback trend rendering grows independent policy beyond the timeline renderer.

Date: 2026-05-24
Area: MCP performance timeline helper locality
Problem: Several MCP timeline helper groups were 25-40 line partial fragments for formatting and trend subsections, forcing small-file hops for one timeline rendering surface.
Files consolidated: `tools/McpServer/Tools/PerformanceTimelineTools.Formatting.Preview.cs`; `tools/McpServer/Tools/PerformanceTimelineTools.Formatting.Flashback.cs`; `tools/McpServer/Tools/PerformanceTimelineTools.Rendering.Trend.Preview.cs`; `tools/McpServer/Tools/PerformanceTimelineTools.Rendering.Trend.Flashback.cs`; `tools/McpServer/Tools/PerformanceTimelineTools.Summaries.Pressure.cs`
Files added: none
Net production .cs delta: -5
Partial clusters reduced: `PerformanceTimelineTools` -5 files
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; offline runtime snapshot harness; `git diff --check`
CLI/MCP/pipe checks, if applicable: covered by MCP performance timeline source-ownership/rendering/projection contract tests and runtime snapshot regression tests
Behavior preserved: MCP performance timeline command shape, row projection, table text, trend sections, pressure summaries, and helper formatting remain in the same public tool surface
Notes for future agents: start MCP timeline cleanup from the smallest helper fragments first; keep formatting helpers, first-vs-last trend text, table rendering, target summaries, and pressure summaries in `PerformanceTimelineTools.Rendering.cs`; split only when a subsection grows independent policy.

Date: 2026-05-24
Area: MCP Flashback segment-list command locality
Problem: `flashback_segments` lived in a 30-line partial even though it has no independent validation policy and belongs with the root Flashback MCP tool commands.
Files consolidated: `tools/McpServer/Tools/FlashbackTools.Segments.cs`
Files added: none
Net production .cs delta: -1
Partial clusters reduced: `FlashbackTools` -1 file
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; offline runtime snapshot harness; `git diff --check`
CLI/MCP/pipe checks, if applicable: covered by MCP Flashback tool surface/pipe-route tests and runtime snapshot regression tests
Behavior preserved: `flashback_segments` still sends `AutomationCommandKind.FlashbackGetSegments` and returns the same formatted response text.
Notes for future agents: keep low-policy Flashback MCP root commands in `FlashbackTools.cs`; keep action and export files separate while they own validation and payload policy.

Date: 2026-05-24
Area: MCP verification helper locality
Problem: Verification lookup and assertion JSON parsing lived in two 31-36 line partials even though both are only used by the root verification MCP methods.
Files consolidated: `tools/McpServer/Tools/VerificationTools.Parsing.cs`; `tools/McpServer/Tools/VerificationTools.Assertions.cs`
Files added: none
Net production .cs delta: -2
Partial clusters reduced: `VerificationTools` -2 files
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; offline runtime snapshot harness; `git diff --check`
CLI/MCP/pipe checks, if applicable: covered by MCP verification tool route/format tests and runtime snapshot regression tests
Behavior preserved: `verify_recording`, `assert_snapshot`, and `verify_file` command routing, assertion JSON cloning, and verification lookup from `Data.Verification` / `Snapshot.LastVerification` are unchanged.
Notes for future agents: keep small root-only verification parsing helpers in `VerificationTools.cs`; keep verification response text in `VerificationTools.Formatting.cs` while it remains a cohesive rendering surface.

Date: 2026-05-24
Area: ssctl command context locality
Problem: The per-invocation `CommandContext` wrapper lived in an 18-line partial even though it is constructed only by the root `CommandHandlers.ExecuteAsync` dispatcher.
Files consolidated: `tools/ssctl/CommandHandlers.Context.cs`
Files added: none
Net production .cs delta: -1
Partial clusters reduced: `CommandHandlers` -1 file
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; offline runtime snapshot harness; `git diff --check`
CLI/MCP/pipe checks, if applicable: covered by ssctl command-handler source-ownership/routing tests and runtime snapshot regression tests
Behavior preserved: `ExecuteAsync` still constructs the same context wrapper with transport, global JSON flag, and remaining arguments.
Notes for future agents: keep tiny root-only dispatcher support types in `CommandHandlers.cs`; keep command-family handlers split only when they own command-specific payload or validation policy.

Date: 2026-05-24
Area: ssctl command argument parsing locality
Problem: CLI usage validation, argument joining, flag consumption, optional flag parsing, and JSON detection/pretty-printing lived in three small `CommandHandlers` partials even though they are one command-line argument interpretation support surface.
Files consolidated: `tools/ssctl/CommandHandlers.Flags.cs`; `tools/ssctl/CommandHandlers.Json.cs`
Files added: none
Net production .cs delta: -2
Partial clusters reduced: `CommandHandlers` -2 files
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; offline runtime snapshot harness; `git diff --check`
CLI/MCP/pipe checks, if applicable: covered by ssctl command-handler routing/source-ownership tests and runtime snapshot regression tests
Behavior preserved: Flag removal, optional flag value parsing, usage exceptions, JSON detection, and pretty JSON formatting are unchanged
Notes for future agents: superseded on 2026-05-24 by the ssctl shared helper consolidation; generic argument/value helpers now live in `CommandHandlers.cs` with shared command sending and response exit-code handling.

Date: 2026-05-24
Area: ssctl simple snapshot section formatting
Problem: Simple snapshot row sections for state, audio, recording, diagnostics, performance, and memory/GC lived in five tiny formatter partials even though they are only called by the root snapshot formatter in fixed output order.
Files consolidated: `tools/ssctl/Formatters.Snapshot.CoreSections.cs`; `tools/ssctl/Formatters.Snapshot.Audio.cs`; `tools/ssctl/Formatters.Snapshot.Recording.cs`; `tools/ssctl/Formatters.Snapshot.DiagnosticLanes.cs`; `tools/ssctl/Formatters.Snapshot.ProcessResources.cs`
Files added: none
Net production .cs delta: -5
Partial clusters reduced: `Formatters` -5 files
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; offline runtime snapshot harness; `git diff --check`
CLI/MCP/pipe checks, if applicable: covered by ssctl formatter ownership/output tests and runtime snapshot regression tests
Behavior preserved: Snapshot section order, headers, field names, and formatted text for state, audio, recording, diagnostics, performance, and memory/GC are unchanged
Notes for future agents: superseded by later ssctl snapshot consolidations; keep snapshot row sections, D3D preview text, Flashback text, MJPEG text, and thread health with `Formatters.Snapshot.cs` unless a subsection grows independent policy beyond snapshot rendering.

Date: 2026-05-24
Area: ssctl snapshot small-section formatter locality
Problem: Six 20-39 line `ssctl` snapshot formatter partials owned simple sections that only make sense in the snapshot parent render order, increasing file count and forcing extra hops for console snapshot review.
Files consolidated: `tools/ssctl/Formatters.Snapshot.CaptureSettings.cs`; `tools/ssctl/Formatters.Snapshot.CaptureCadence.cs`; `tools/ssctl/Formatters.Snapshot.AvSync.cs`; `tools/ssctl/Formatters.Snapshot.Source.cs`; `tools/ssctl/Formatters.Snapshot.Preview.cs`; `tools/ssctl/Formatters.Snapshot.Runtime.cs`
Files added: none
Net production .cs delta: -6
Partial clusters reduced: `Formatters` -6 files
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; offline runtime snapshot harness; `git diff --check`
CLI/MCP/pipe checks, if applicable: covered by ssctl snapshot source-ownership tests and runtime snapshot regression tests
Behavior preserved: Snapshot section order and text projection stay in the same `FormatSnapshot` flow; D3D preview, Flashback, MJPEG, and thread-health sections now live with the snapshot renderer.
Notes for future agents: start ssctl formatter cleanup from the smallest snapshot sections first; keep simple one-section row writers in `Formatters.Snapshot.cs` unless they grow independent policy.

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
Area: Automation diagnostics realtime preview evaluation
Problem: Present/display preview diagnostic verdict construction lived in a small partial even though it is only called by the realtime preview diagnostic owner that orders scheduler, renderer, and present/display verdicts.
Files consolidated: `Sussudio/Services/Automation/AutomationDiagnosticsHub.DiagnosticEvaluationRealtime.PreviewPresent.cs`
Files added: none
Net production .cs delta: -1
Partial clusters reduced: `AutomationDiagnosticsHub` -1 file
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; offline runtime snapshot harness; `git diff --check`
CLI/MCP/pipe checks, if applicable: covered by automation diagnostics evaluation source-ownership tests and runtime snapshot regression tests
Behavior preserved: Realtime present/display cadence and preview display 1% low diagnostic severity, codes, messages, and lane mapping remain unchanged
Notes for future agents: realtime preview verdict policy is now folded into `DiagnosticEvaluationRealtime.cs`; extract a named collaborator only if preview verdict policy grows beyond one cohesive scan unit

Date: 2026-05-21
Area: Automation diagnostics Flashback recording alerts
Problem: Flashback recording path degradation alert construction lived in a small partial even though it is only called by the Flashback recording alert owner that computes the backing queue, audio queue, backpressure, and force-rotate conditions.
Files consolidated: `Sussudio/Services/Automation/AutomationDiagnosticsHub.FlashbackRecordingAlerts.Degradation.cs`
Files added: none
Net production .cs delta: -1
Partial clusters reduced: `AutomationDiagnosticsHub` -1 file
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; offline runtime snapshot harness; `git diff --check`
CLI/MCP/pipe checks, if applicable: covered by automation diagnostics alert ownership tests and runtime snapshot regression tests
Behavior preserved: Flashback recording degradation alert ID, condition, severity, message text, category, clear message, and throttle remain unchanged
Notes for future agents: keep Flashback recording alert condition assembly and alert emission together unless degradation policy grows into a named collaborator

Date: 2026-05-21
Area: Automation diagnostics Flashback playback alerts
Problem: Flashback playback audio-master fallback and audio-queue backlog alerts lived in a small partial even though they are only called by the Flashback playback performance alert owner in the root alert orchestration file.
Files consolidated: `Sussudio/Services/Automation/AutomationDiagnosticsHub.FlashbackPlaybackPerformanceAlerts.Audio.cs`
Files added: none
Net production .cs delta: -1
Partial clusters reduced: `AutomationDiagnosticsHub` -1 file
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; offline runtime snapshot harness; `git diff --check`
CLI/MCP/pipe checks, if applicable: covered by automation diagnostics alert ownership tests and runtime snapshot regression tests
Behavior preserved: Flashback playback audio-master fallback and audio-queue backlog alert IDs, conditions, severities, messages, categories, clear messages, and throttles remain unchanged
Notes for future agents: keep lightweight playback audio alert policy with `Alerts.cs`; keep playback cadence alerts separate while they remain a larger focused policy block

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
Notes for future agents: preview scheduler, renderer, and present/display verdict policy now live with `DiagnosticEvaluationRealtime.cs`; extract a named collaborator only if preview verdict policy grows beyond one cohesive scan unit

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
Notes for future agents: keep lightweight snapshot-driven signal alert rules with `AutomationDiagnosticsHub.Alerts.cs` unless a rule family grows independent state or policy

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
Notes for future agents: small Flashback recording alert rules now live with `Alerts.cs`; extract a named collaborator only if Flashback alert policy grows beyond one cohesive scan unit

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

Date: 2026-05-24
Area: ssctl snapshot formatter locality
Problem: D3D preview snapshot text lived in a 66-line partial even though it is only reached through the parent snapshot preview-routing method and shares the same console projection surface.
Files consolidated: `tools/ssctl/Formatters.Snapshot.PreviewD3D.cs`
Files added: none
Net production .cs delta: -1
Partial clusters reduced: `Formatters` -1 file
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; offline runtime snapshot harness; `git diff --check`; `git diff --cached --check`
CLI/MCP/pipe checks, if applicable: covered by ssctl snapshot formatter ownership tests, shared/ssctl field-alignment tests, and runtime snapshot regression tests
Behavior preserved: D3D preview renderer detection, section order, CPU timing, pipeline latency, frame-latency wait, frame stats, frame ownership, and slow-frame diagnostics remain in the same `FormatSnapshot` output flow
Notes for future agents: keep single-use ssctl D3D preview snapshot text with the parent preview-routing section unless it grows independent branching policy

Date: 2026-05-24
Area: ssctl shell command locality
Problem: Stats, settings, and frame-time visibility commands lived in a 59-line partial even though they are shell visibility toggles beside fullscreen, recordings-folder, and window commands.
Files consolidated: `tools/ssctl/CommandHandlers.UiVisibility.cs`
Files added: none
Net production .cs delta: -1
Partial clusters reduced: `CommandHandlers` -1 file
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; offline runtime snapshot harness; `git diff --check`; `git diff --cached --check`
CLI/MCP/pipe checks, if applicable: covered by ssctl command-handler source ownership, enum-command protocol, and routing tests
Behavior preserved: `stats`, `stats section`, `settings`, `frametime`, and `frame-time` commands still send the same automation command IDs and payload fields
Notes for future agents: superseded on 2026-05-25 by the ssctl command-handler consolidation; keep shell visibility commands in `CommandHandlers.cs` unless they grow non-shell policy or a shared UI-control command owner

Date: 2026-05-24
Area: Preview pacing classification locality
Problem: Preview pacing classifier input/output DTOs lived in a separate 65-line file even though they are only meaningful as the public evidence/result shape consumed by the slow-stage classifier policy.
Files consolidated: `Sussudio/Services/Automation/PreviewPacingClassificationModels.cs`
Files added: none
Net production .cs delta: -1
Partial clusters reduced: none
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; offline runtime snapshot harness; `git diff --check`; `git diff --cached --check`
CLI/MCP/pipe checks, if applicable: covered by preview pacing classifier ownership, classifier behavior tests, automation snapshot wiring tests, and runtime snapshot regression tests
Behavior preserved: `PreviewPacingClassificationInput`, `PreviewPacingClassification`, and `PreviewPacingSlowStageClassifier.Classify` keep the same namespace, public type names, and classification behavior
Notes for future agents: keep the classifier DTOs beside the classifier unless the evidence shape becomes shared by another independent policy

Date: 2026-05-24
Area: Shared automation snapshot formatter locality
Problem: The shared automation snapshot video-pipeline and thread-health text lived in a 76-line partial even though it is only called by the root snapshot formatter flow and is part of the same one-pass console projection.
Files consolidated: `tools/Common/AutomationSnapshotFormatter.VideoPipeline.cs`
Files added: none
Net production .cs delta: -1
Partial clusters reduced: `AutomationSnapshotFormatter` -1 file
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; offline runtime snapshot harness; `git diff --check`; `git diff --cached --check`
CLI/MCP/pipe checks, if applicable: covered by shared snapshot formatter ownership tests, formatter output order tests, field-alignment tests, and runtime snapshot regression tests
Behavior preserved: Video pipeline section text, thread-health section order, source-reader row, WASAPI capture row, and WASAPI playback row remain in the same `FormatSnapshot` output flow
Notes for future agents: keep one-pass shared snapshot row sections with the root formatter unless a section grows independent policy or reusable formatting behavior

Date: 2026-05-24
Area: ssctl snapshot formatter locality
Problem: The ssctl snapshot thread-health text lived in a 56-line partial even though it is only called by the root snapshot formatter flow and sits directly after the root video-pipeline section.
Files consolidated: `tools/ssctl/Formatters.Snapshot.ThreadHealth.cs`
Files added: none
Net production .cs delta: -1
Partial clusters reduced: `Formatters` -1 file
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; offline runtime snapshot harness; `git diff --check`; `git diff --cached --check`
CLI/MCP/pipe checks, if applicable: covered by ssctl snapshot ownership tests, formatter output order tests, field-alignment tests, and runtime snapshot regression tests
Behavior preserved: Thread-health section order plus source-reader, WASAPI capture, and WASAPI playback rows remain in the same `FormatSnapshot` output flow
Notes for future agents: keep single-use ssctl thread-health snapshot text with the root formatter unless it grows independent policy or reusable formatting behavior

Date: 2026-05-24
Area: Diagnostic session runner locality
Problem: `DiagnosticSessionRunner.cs` was a 25-line public wrapper over a single-use `DiagnosticSessionRunExecution.cs` phase-plan class, forcing agents to open two files to understand the diagnostic-session entry point and run sequence.
Files consolidated: `tools/Common/DiagnosticSessionRunExecution.cs`
Files added: none
Net production .cs delta: -1
Partial clusters reduced: none
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; offline runtime snapshot harness; `git diff --check`; `git diff --cached --check`
CLI/MCP/pipe checks, if applicable: covered by diagnostic-session runner, scenario execution, artifact/result, ssctl, and MCP tool-surface tests
Behavior preserved: Diagnostic-session public `RunAsync`/`Format` surface, phase order, output locking, cleanup, recording checks, post-run snapshots, result-build handoff, and live-state terminal write remain unchanged
Notes for future agents: keep the visible diagnostic-session phase plan with `DiagnosticSessionRunner.cs`; use named collaborators for context, scenario phase execution, cleanup, recording checks, post-run snapshots, and result building

Date: 2026-05-24
Area: Automation diagnostics evaluation locality
Problem: `AutomationDiagnosticsHub.DiagnosticEvaluation.cs` was a 115-line root verdict orchestration partial while `AutomationDiagnosticsHub.Evaluation.cs` already owned the performance score, diagnostic helpers, and health classifiers used to choose that verdict.
Files consolidated: `Sussudio/Services/Automation/AutomationDiagnosticsHub.DiagnosticEvaluation.cs`
Files added: none
Net production .cs delta: -1
Partial clusters reduced: `AutomationDiagnosticsHub` -1 file
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; offline runtime snapshot harness; `git diff --check`; `git diff --cached --check`
CLI/MCP/pipe checks, if applicable: covered by automation diagnostics source-ownership tests and runtime snapshot regression tests
Behavior preserved: Diagnostic verdict ordering still builds lanes first, checks Flashback-specific verdicts, checks realtime verdicts, and falls back to the same healthy/mixed summary/evidence
Notes for future agents: keep root diagnostic verdict orchestration with `AutomationDiagnosticsHub.Evaluation.cs`; keep Flashback, realtime, preview, and lane-specific verdict policy in their focused owners while they carry independent branching

Date: 2026-05-24
Area: D3D preview frame-upload locality
Problem: `D3D11PreviewRenderer.RawFrameUpload.cs` was a 124-line implementation-detail partial for the frame-upload owner, forcing agents to open two files to follow VideoProcessor input-view resolution and raw byte/lease texture upload fallback.
Files consolidated: `Sussudio/Services/Preview/D3D11PreviewRenderer.RawFrameUpload.cs`
Files added: none
Net production .cs delta: -1
Partial clusters reduced: `D3D11PreviewRenderer` -1 file
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; offline runtime snapshot harness; `git diff --check`; `git diff --cached --check`
CLI/MCP/pipe checks, if applicable: covered by D3D11 preview renderer ownership, diagnostics contract, and runtime snapshot regression tests
Behavior preserved: External texture input-view creation, raw frame size checks, direct `UpdateSubresource` upload, one-time staging fallback logging, staging `Map`/copy/`CopyResource`, and render-pass present/timing ownership remain unchanged
Notes for future agents: keep CPU-buffer upload helpers with `D3D11PreviewRenderer.FrameUpload.cs`; keep render-pass timing/accounting in `RenderPasses.cs` and shader draw execution in the shader-pass owners

Date: 2026-05-24
Area: D3D preview renderer metrics locality
Problem: Present cadence, render/pipeline/frame-latency tracking, and metric window reset/resizing were split across three small partials even though they all mutate and project the same renderer metric ring buffers.
Files consolidated: `Sussudio/Services/Preview/D3D11PreviewRenderer.PresentCadenceMetrics.cs`; `Sussudio/Services/Preview/D3D11PreviewRenderer.MetricsTracking.cs`; `Sussudio/Services/Preview/D3D11PreviewRenderer.MetricWindows.cs`
Files added: none
Net production .cs delta: -3
Partial clusters reduced: `D3D11PreviewRenderer` -3 files
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; offline runtime snapshot harness; `git diff --check`; `git diff --cached --check`
CLI/MCP/pipe checks, if applicable: covered by D3D11 preview renderer metrics ownership, cadence behavior tests, diagnostics contract tests, and runtime snapshot regression tests
Behavior preserved: Present-cadence sampling/suppression, pipeline-latency tracking, render CPU timing windows, frame-latency wait counters/timing, expected-frame-rate window sizing, and reset/clear lifecycle remain unchanged
Notes for future agents: keep renderer metric state, mutation, reset, read-only projection, metric DTOs, and shared summarization helpers together in `D3D11PreviewRenderer.Metrics.cs`

Date: 2026-05-24
Area: Flashback playback component lifecycle locality
Problem: `FlashbackPlaybackController.Lifecycle.cs` and `FlashbackPlaybackController.PreviewDetachLifecycle.cs` split component references, init/update/dispose, preview-detach timeout cleanup, and deferred preview reattach state from the root controller state they directly mutate.
Files consolidated: `Sussudio/Services/Flashback/FlashbackPlaybackController.Lifecycle.cs`; `Sussudio/Services/Flashback/FlashbackPlaybackController.PreviewDetachLifecycle.cs`
Files added: none
Net production .cs delta: -2
Partial clusters reduced: `FlashbackPlaybackController` -2 files
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; offline runtime snapshot harness; `git diff --check`; `git diff --cached --check`
CLI/MCP/pipe checks, if applicable: covered by Flashback playback submission/lifecycle source-shape tests, marker source aggregation tests, and runtime snapshot regression tests
Behavior preserved: Initialize/update/dispose behavior, audio/preview routing after component updates, preview-detach stop-timeout cleanup, deferred preview reattach retry scheduling, and live-state restoration order remain unchanged
Notes for future agents: keep component lifecycle and preview-detach deferred attach state with `FlashbackPlaybackController.cs`; keep command queue, playback thread, decoder file, frame submission, and audio routing behavior in their focused owners

Date: 2026-05-24
Area: MainWindow Flashback adapter locality
Problem: `MainWindow.Flashback.Presentation.cs` was an 86-line adapter-only partial that tests and docs already treated as one Flashback XAML adapter surface with `MainWindow.Flashback.Interactions.cs`.
Files consolidated: `Sussudio/MainWindow.Flashback.Presentation.cs`
Files added: none
Net production .cs delta: -1
Partial clusters reduced: `MainWindow` -1 file
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; offline runtime snapshot harness; `git diff --check`; `git diff --cached --check`
CLI/MCP/pipe checks, if applicable: not applicable; XAML event-handler and property-change callback names preserved
Behavior preserved: Flashback marker, playback presentation, track-size, buffer, position, export-progress, and exporting callbacks now live in the same XAML-facing Flashback adapter with unchanged controller calls
Notes for future agents: keep Flashback command, polling, playhead, scrub, settings, timeline, and presentation adapters together in `MainWindow.Flashback.Interactions.cs`; controller behavior remains in `Sussudio/Controllers/Flashback`

Date: 2026-05-24
Area: D3D preview renderer public lifecycle locality
Problem: `D3D11PreviewRenderer.Lifecycle.cs` kept public `Start`, `Dispose`, and renderer startup state in a tiny partial even though the root renderer already owns the public facade, construction references, runtime knobs, and observable state.
Files consolidated: `Sussudio/Services/Preview/D3D11PreviewRenderer.Lifecycle.cs`
Files added: none
Net production .cs delta: -1
Partial clusters reduced: `D3D11PreviewRenderer` -1 file
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; offline runtime snapshot harness; `git diff --check`; `git diff --cached --check`
CLI/MCP/pipe checks, if applicable: not applicable; no automation/tool contract changes
Behavior preserved: Public start/dispose semantics, startup dimension/FPS/HDR reset, first-frame reset, shared-device reset flag, frame-ready reset, render-thread creation, stop-before-start, shared-device disposal, and frame-ready event disposal remain unchanged
Notes for future agents: keep public lifecycle with `D3D11PreviewRenderer.cs`; keep stop/reinit-stop, panel unbind, native-call fencing, and pending-frame shutdown cleanup in `D3D11PreviewRenderer.RenderThread.cs`

Date: 2026-05-24
Area: CUDA D3D11 bridge lifecycle locality
Problem: `CudaD3D11Interop.Lifetime.cs` was a 62-line disposal partial for resources acquired by `CudaD3D11Interop.Initialization.cs`, forcing bridge construction and teardown invariants to be read across two files.
Files consolidated: `Sussudio/Services/Gpu/CudaD3D11Interop.Lifetime.cs`
Files added: none
Net production .cs delta: -1
Partial clusters reduced: `CudaD3D11InteropBridge` -1 file
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; offline runtime snapshot harness; `git diff --check`; `git diff --cached --check`
CLI/MCP/pipe checks, if applicable: not applicable; no automation/tool contract changes
Behavior preserved: CUDA resource unregistration, D3D texture disposal order, primary-context release, COM reference release, initialized flag reset, and disposal logging remain unchanged
Notes for future agents: keep bridge resource acquisition, disposal, native declarations, and CUDA struct/constant definitions together in `CudaD3D11Interop.Initialization.cs`; keep zero-copy/staging copy hot paths in `CudaD3D11Interop.Copy.cs`

Date: 2026-05-24
Area: MainViewModel device audio analog gain locality
Problem: `MainViewModel.AnalogAudioGain.cs` was a 64-line method-only partial that depended on device-audio state, selected-device guards, gain mapping, and flash-persist scheduling from `MainViewModel.DeviceAudioState.cs`.
Files consolidated: `Sussudio/ViewModels/MainViewModel.AnalogAudioGain.cs`
Files added: none
Net production .cs delta: -1
Partial clusters reduced: `MainViewModel` -1 file
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; offline runtime snapshot harness; `git diff --check`; `git diff --cached --check`
CLI/MCP/pipe checks, if applicable: not applicable; automation command names and IDs unchanged
Behavior preserved: Analog gain clamping, percent-to-byte mapping, native-XU volatile gain write, selected-device guards, status text updates, refresh suppression, deferred flash persistence, settings save, and cancellation checks remain unchanged
Notes for future agents: keep device-native audio UI state, analog gain writes, gain mapping, selected-device guards, mode switching, failure readback, and refresh/restore readback together in `MainViewModel.DeviceAudioState.cs`

Date: 2026-05-24
Area: LibAv recording sink startup locality
Problem: `LibAvRecordingSink.VideoSession.cs` was a 73-line startup-only partial that initialized per-recording video/GPU/CUDA queues and reset startup metrics directly around `StartAsync`.
Files consolidated: `Sussudio/Services/Recording/LibAvRecordingSink.VideoSession.cs`
Files added: none
Net production .cs delta: -1
Partial clusters reduced: `LibAvRecordingSink` -1 file
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; offline runtime snapshot harness; `git diff --check`; `git diff --cached --check`
CLI/MCP/pipe checks, if applicable: not applicable; no automation/tool contract changes
Behavior preserved: CUDA/GPU queue selection, bounded video/GPU/CUDA channel creation, width/height state reset, video/GPU/CUDA metric reset, enqueue/write tick reset, diagnostics reset, and startup ordering remain unchanged
Notes for future agents: keep per-recording video session queue setup and startup metric reset with `LibAvRecordingSink.Startup.cs`; keep public queue admission and packet cleanup in `LibAvRecordingSink.VideoQueueSubmission.cs`

Date: 2026-05-24
Area: NVDEC MJPEG decoder lifecycle locality
Problem: `NvdecMjpegDecoder.Lifetime.cs` was a 70-line disposal/error-text partial for resources allocated by `NvdecMjpegDecoder.Initialization.cs`, forcing acquisition and release invariants across two files.
Files consolidated: `Sussudio/Services/Gpu/NvdecMjpegDecoder.Lifetime.cs`
Files added: none
Net production .cs delta: -1
Partial clusters reduced: `NvdecMjpegDecoder` -1 file
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; offline runtime snapshot harness; `git diff --check`; `git diff --cached --check`
CLI/MCP/pipe checks, if applicable: not applicable; no automation/tool contract changes
Behavior preserved: Packet/frame/context/buffer release order, packed CPU buffer free, initialized flag reset, disposal logging, and FFmpeg error string formatting remain unchanged
Notes for future agents: keep NVDEC resource acquisition, caller-provided context adoption, packet decode, CPU download/copy, disposal, and FFmpeg error text together in `NvdecMjpegDecoder.cs` unless a real named collaborator emerges.

Date: 2026-05-24
Area: Capture session coordinator root locality
Problem: `CaptureSessionCoordinator.Commands.cs` and `CaptureSessionCoordinator.Snapshot.cs` were two tiny facade/projection partials around the coordinator's shared state and serialized command entry points, leaving the basic coordinator surface split across three files before reaching the real queue, disposal, and Flashback boundaries.
Files consolidated: `Sussudio/Services/Capture/CaptureSessionCoordinator.Commands.cs`; `Sussudio/Services/Capture/CaptureSessionCoordinator.Snapshot.cs`
Files added: none
Net production .cs delta: -2
Partial clusters reduced: `CaptureSessionCoordinator` -2 files
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; offline runtime snapshot harness; `git diff --check`; `git diff --cached --check`
CLI/MCP/pipe checks, if applicable: not applicable; no automation command names/IDs changed
Behavior preserved: Public lifecycle/audio command methods, emergency stop routing, audio monitoring mute/start/stop order, preview volume guard, snapshot projection fields, pending-command age bookkeeping, queue latency tracking, and serialized worker handoff remain unchanged
Notes for future agents: keep coordinator construction, shared state, public non-Flashback command facade, and snapshot projection together in `CaptureSessionCoordinator.cs`; keep queue worker mechanics in `Queue.cs`, disposal in `Disposal.cs`, and Flashback-specific facades in the Flashback partials

Date: 2026-05-24
Area: Capture session coordinator Flashback facade locality
Problem: `CaptureSessionCoordinator.Flashback.Playback.cs` was an 80-line adapter-only partial that used the guard and rejection telemetry owned by `CaptureSessionCoordinator.Flashback.cs`, so changing the coordinator Flashback facade required opening both files.
Files consolidated: `Sussudio/Services/Capture/CaptureSessionCoordinator.Flashback.Playback.cs`
Files added: none
Net production .cs delta: -1
Partial clusters reduced: `CaptureSessionCoordinator` -1 file
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; offline runtime snapshot harness; `git diff --check`; `git diff --cached --check`
CLI/MCP/pipe checks, if applicable: not applicable; no automation command names/IDs changed
Behavior preserved: Flashback scrub, seek, play, pause, go-live, nudge, in/out marker, clear-marker adapters, active-playback guard use, and rejection telemetry remain unchanged
Notes for future agents: keep coordinator Flashback status, export/segment forwarding, playback/scrub/marker adapters, and active playback-controller guard together in `CaptureSessionCoordinator.Flashback.cs`; keep queue worker mechanics and disposal in their focused partials

Date: 2026-05-24
Area: MF source-reader frame delivery DXGI locality
Problem: `MfSourceReaderVideoCapture.DxgiBuffers.cs` was a 58-line helper partial used only by `DeliverDualFrameFromBuffer`, splitting GPU texture extraction/fallback diagnostics from the frame-delivery branch that consumes those results.
Files consolidated: `Sussudio/Services/Capture/MfSourceReaderVideoCapture.DxgiBuffers.cs`
Files added: none
Net production .cs delta: -1
Partial clusters reduced: `MfSourceReaderVideoCapture` -1 file
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; offline runtime snapshot harness; `git diff --check`; `git diff --cached --check`
CLI/MCP/pipe checks, if applicable: not applicable; no automation command names/IDs changed
Behavior preserved: D3D-enabled guards, IMFDXGIBuffer detection, D3D texture IID lookup, resource/subresource failure logging, GPU texture release on subresource failure, and CPU fallback behavior remain unchanged
Notes for future agents: keep DXGI texture extraction and dual GPU/CPU delivery orchestration together in `MfSourceReaderVideoCapture.FrameDelivery.cs`; keep raw/compressed CPU buffer helpers in `RawFrameDelivery.cs` and shared packed layout math/subtype labels in `MfSourceReaderVideoCapture.cs`

Date: 2026-05-24
Area: D3D preview renderer render timing and viewport locality
Problem: `D3D11PreviewRenderer.DisplayClock.cs` and `D3D11PreviewRenderer.Viewport.cs` were sub-80-line partials that split display-clock projection from the DXGI frame-statistics state it samples and split viewport helpers from the render-pass execution paths that consume them.
Files consolidated: `Sussudio/Services/Preview/D3D11PreviewRenderer.DisplayClock.cs`; `Sussudio/Services/Preview/D3D11PreviewRenderer.Viewport.cs`
Files added: none
Net production .cs delta: -2
Partial clusters reduced: `D3D11PreviewRenderer` -2 files
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; offline runtime snapshot harness; `git diff --check`
CLI/MCP/pipe checks, if applicable: not applicable; no automation command names/IDs changed
Behavior preserved: DXGI frame-statistics sampling, visible-frame tick estimation, `IPreviewDisplayClock` snapshot construction, letterbox rectangle math, viewport constant-buffer upload, shader draw paths, and VideoProcessor destination-rectangle behavior remain unchanged
Notes for future agents: keep display-clock projection with `D3D11PreviewRenderer.DxgiFrameStatistics.cs`; keep letterbox/viewport helpers with `D3D11PreviewRenderer.RenderPasses.cs`; keep D3D resource creation in `Resources.cs` and VideoProcessor pipeline setup in `VideoProcessorPipeline.cs`

Date: 2026-05-24
Area: MainViewModel capture-settings adapter locality
Problem: `MainViewModel.CaptureSettings.cs` was a 50-line adapter partial that only sampled capture-selection, source telemetry, recording, Flashback, and audio UI state before delegating to `CaptureSettingsProjectionBuilder`, forcing one extra file hop for preview/recording settings review.
Files consolidated: `Sussudio/ViewModels/MainViewModel.CaptureSettings.cs`
Files added: none
Net production .cs delta: -1
Partial clusters reduced: `MainViewModel` -1 file
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; offline runtime snapshot harness; `git diff --check`
CLI/MCP/pipe checks, if applicable: not applicable; no automation command names/IDs changed
Behavior preserved: Effective resolution sampling, runtime/source telemetry capture, frame-rate option snapshotting, HDR/MJPEG/recording/Flashback/audio/microphone input projection, and pure `CaptureSettingsProjectionBuilder` policy remain unchanged
Notes for future agents: keep the impure `BuildCaptureSettings` adapter with `MainViewModel.CaptureState.cs`; keep pure capture-settings policy and DTOs in `CaptureSettingsProjectionBuilder.cs`

Date: 2026-05-24
Area: MainViewModel dispatch adapter locality
Problem: `MainViewModel.Dispatching.cs` was a 62-line facade partial that only forwarded stable private adapter names to `MainViewModelUiDispatchController` and fanned out preview events consumed by the controller graph, leaving composition-time ports split from the composition owner.
Files consolidated: `Sussudio/ViewModels/MainViewModel.Dispatching.cs`
Files added: none
Net production .cs delta: -1
Partial clusters reduced: `MainViewModel` -1 file
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; offline runtime snapshot harness; `git diff --check`
CLI/MCP/pipe checks, if applicable: not applicable; no automation command names/IDs changed
Behavior preserved: UI operation enqueue/execute/invoke adapter names, disposal-aware enqueue policy delegation, preview reinitialize event fan-out, renderer-stop event fan-out, timeout helper semantics, and controller graph port wiring remain unchanged
Notes for future agents: keep stable MainViewModel UI-dispatch adapter names and preview event fan-out in `MainViewModel.Composition.cs`; keep actual dispatcher queue policy in `MainViewModelUiDispatchController.cs`

Date: 2026-05-24
Area: NativeXuAudioProbe default experiment payload locality
Problem: `Program.ExperimentPayloads.cs` was a 48-line helper file used only by `Program.DefaultExperiment.cs`, splitting payload construction from the default experiment sequence that consumes every helper.
Files consolidated: `tools/NativeXuAudioProbe/Program.ExperimentPayloads.cs`
Files added: none
Net production .cs delta: -1
Partial clusters reduced: n/a; probe helper file count -1
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; offline runtime snapshot harness; `git diff --check`
CLI/MCP/pipe checks, if applicable: affected `NativeXuAudioProbe` build covered by solution build; no command names changed
Behavior preserved: short/int/byte experiment enumeration, invariant display-value formatting, width-based payload byte construction, unsupported-width exception behavior, and default experiment restore/set payload usage remain unchanged
Notes for future agents: superseded on 2026-05-25 by the NativeXu support consolidation and later default-experiment reporting consolidation; keep shared Native XU command IDs, default-experiment-only payload construction, and reporting/readback helpers in `Program.DefaultExperiment.cs`.

Date: 2026-05-24
Area: ssctl shared command helper locality
Problem: `CommandHandlers.Arguments.cs` and `CommandHandlers.Values.cs` were tiny generic support partials for the ssctl root command handler. They did not own a command family; they only supplied usage, flag, JSON, and primitive/domain value helpers consumed by the root router and feature command partials.
Files consolidated: `tools/ssctl/CommandHandlers.Arguments.cs`; `tools/ssctl/CommandHandlers.Values.cs`
Files added: none
Net production .cs delta: -2
Partial clusters reduced: `CommandHandlers` -2 files
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; offline runtime snapshot harness; `git diff --check`
CLI/MCP/pipe checks, if applicable: ssctl command routing tests cover command IDs/payloads; no automation command names/IDs changed
Behavior preserved: argument count checks, required word parsing, flag consumption, optional flag values, JSON pretty-printing/detection, primitive parsing, Flashback export duration validation, on/off and show/hide parsing, recording format normalization, snap action mapping, and assertion value parsing remain unchanged
Notes for future agents: keep generic ssctl argument/value helpers with `CommandHandlers.cs`; keep command-family payload shaping in `CaptureControls`, `Window`, `AutomationFlow`, `Flashback`, and `Observability`

Date: 2026-05-24
Area: Flashback exporter segment template locality
Problem: `FlashbackExporter.SegmentTemplate.cs` split first-usable-template selection and per-segment input preflight away from the multi-segment export shell in `FlashbackExporter.Segments.cs`, so reviewing segment export setup required opening an extra partial before reaching packet writing.
Files consolidated: `Sussudio/Services/Flashback/FlashbackExporter.SegmentTemplate.cs`
Files added: none
Net production .cs delta: -1
Partial clusters reduced: `FlashbackExporter` -1 file
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; offline runtime snapshot harness; `git diff --check`
CLI/MCP/pipe checks, if applicable: not applicable; no automation command names/IDs changed
Behavior preserved: template selection order, stream-info lookup, bounded stream-count validation, missing-video and incomplete-video skip diagnostics, output context/header setup, per-segment input open, stream-count mismatch handling, layout mismatch skip tracking, and close-on-failed-preflight behavior remain unchanged
Notes for future agents: keep multi-segment export shell, template selection, and segment input preflight together in `FlashbackExporter.Segments.cs`; keep packet writing orchestration and packet read/rebase hot loop behavior together in `SegmentPacketWriting.cs`

Date: 2026-05-24
Area: Flashback decoder lifecycle cleanup locality
Problem: `FlashbackDecoder.Lifetime.cs` only contained `CloseFileCore` and held-frame cleanup helpers, while `OpenFile`, `CloseFile`, and `Dispose` lived in `FlashbackDecoder.cs`; reviewing decoder lifecycle therefore required a second tiny partial for the cleanup path immediately called by the root lifecycle methods.
Files consolidated: `Sussudio/Services/Flashback/FlashbackDecoder.Lifetime.cs`
Files added: none
Net production .cs delta: -1
Partial clusters reduced: `FlashbackDecoder` -1 file
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; offline runtime snapshot harness; `git diff --check`
CLI/MCP/pipe checks, if applicable: not applicable; no automation command names/IDs changed
Behavior preserved: close logging, pending held-frame release, software buffer return, AVPacket/AVFrame/free cleanup, resampler/codec/input-context release, decoder state reset, D3D11 hardware context persistence, and best-effort held-frame release logging remain unchanged
Notes for future agents: keep decoder open/close/dispose and per-file cleanup in `FlashbackDecoder.cs`; keep decode-loop timing in `DecodeLoop.cs`, video codec setup in `VideoSetup.cs`, and output validation in `Validation.cs`

Date: 2026-05-24
Area: LibAv recording sink dispose locality
Problem: `LibAvRecordingSink.Lifetime.cs` only contained `Dispose`/`DisposeAsync`, deferred cleanup scheduling, and final dispose reset, while the root sink owned the state, queue completion helper, and encoding task those disposal paths manipulate.
Files consolidated: `Sussudio/Services/Recording/LibAvRecordingSink.Lifetime.cs`
Files added: none
Net production .cs delta: -1
Partial clusters reduced: `LibAvRecordingSink` -1 file
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; offline runtime snapshot harness; `git diff --check`
CLI/MCP/pipe checks, if applicable: not applicable; no automation command names/IDs changed
Behavior preserved: synchronous dispose fallback, async dispose idempotence, writer completion, cancellation, encode-task observation, deferred cleanup timeout logging, queue buffer returns, queue-depth reset, cancellation-source disposal, queue nulling, GPU/CUDA/microphone flag reset, work semaphore disposal, and encoder dispose failure logging remain unchanged
Notes for future agents: keep sink diagnostics, encoding loop, packet drains, queue completion signal, and dispose/deferred cleanup in `LibAvRecordingSink.cs`; keep startup in `Startup.cs` and stop/final output validation in `StopLifecycle.cs`

Date: 2026-05-25
Area: shared automation pipe client locality
Problem: `AutomationPipeClient.Commands.cs` and `AutomationPipeClient.Transport.cs` split the shared pipe client's command envelope/retry/response-state logic from the single request/response transport it immediately calls, leaving shared CLI/MCP pipe behavior in a tiny two-part partial family.
Files consolidated: `tools/Common/AutomationPipeClient/AutomationPipeClient.Commands.cs`; `tools/Common/AutomationPipeClient/AutomationPipeClient.Transport.cs`
Files added: `tools/Common/AutomationPipeClient/AutomationPipeClient.cs`
Net production .cs delta: -1
Partial clusters reduced: `AutomationPipeClient` -1 file; no longer appears as a partial cluster
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; offline runtime snapshot harness; `git diff --check`
CLI/MCP/pipe checks, if applicable: shared automation tool contract tests cover enum command routing, exact pipe connect error classification, response-state parsing, and synthetic error shaping; no automation command names/IDs changed
Behavior preserved: command resolution, typed `AutomationCommandKind` command-id handoff, request envelope serialization, `not_ready` retry delay bounds, response-state parsing, named-pipe connect classification, UTF-8 request/response framing, response timeout, and cancellation behavior remain unchanged
Notes for future agents: keep command envelope/retry/response-state parsing and named-pipe request transport together in `AutomationPipeClient.cs`; the follow-up command transport locality slice below folds higher-level timeout selection, response validation, and synthetic error handling into the same file while preserving the named `AutomationCommandTransport` type

Date: 2026-05-25
Area: shared automation command transport locality
Problem: `AutomationCommandTransport.cs` was a 72-line wrapper used only as the higher-level timeout/JSON/synthetic-error layer over `AutomationPipeClient`, leaving the shared CLI/MCP pipe client split across an extra tiny file after the partial-family consolidation.
Files consolidated: `tools/Common/AutomationPipeClient/AutomationCommandTransport.cs`
Files added: none
Net production .cs delta: -1
Partial clusters reduced: none
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; offline runtime snapshot harness; `git diff --check`; `git diff --cached --check`; regenerated `docs/architecture/Sussudio-Defragmentation-Baseline.generated.md`
CLI/MCP/pipe checks, if applicable: shared automation tool contract tests cover enum command routing, exact pipe connect error classification, response-state parsing, synthetic error shaping, and ssctl/MCP callers using the named `AutomationCommandTransport` type
Behavior preserved: public automation command names/IDs, typed and string command overloads, timeout selection, response-element validation, synthetic error shaping, pipe connect classification, JSON framing, and caller type names remain unchanged
Notes for future agents: keep `AutomationCommandTransport` as a named type in `AutomationPipeClient.cs` so ssctl/MCP callers still read as command transport users while the shared pipe-client implementation lives in one file.

Date: 2026-05-25
Area: test friend assembly metadata locality
Problem: `Sussudio/AssemblyInfo.cs` and `tools/ssctl/AssemblyInfo.cs` were metadata-only two-line files containing only `InternalsVisibleTo("Sussudio.Tests")`, leaving mechanical test-access attributes as extra production `.cs` files.
Files consolidated: `Sussudio/AssemblyInfo.cs`; `tools/ssctl/AssemblyInfo.cs`
Files added: none
Net production .cs delta: -2
Partial clusters reduced: none
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore` (first run hit known intermittent `Cannot write to a closed TextWriter` in `RoutesUiVisibilityCommands`, rerun passed); offline runtime snapshot harness; `git diff --check`; `git diff --cached --check`; regenerated `docs/architecture/Sussudio-Defragmentation-Baseline.generated.md`
CLI/MCP/pipe checks, if applicable: not applicable; assembly metadata moved into the owning project files
Behavior preserved: `Sussudio.Tests` remains a friend assembly for both the app and ssctl assemblies through project-generated assembly attributes.
Notes for future agents: keep mechanical friend assembly metadata in project files unless an assembly needs substantial handwritten assembly metadata.

Date: 2026-05-25
Area: app contracts using metadata locality
Problem: `Sussudio/GlobalUsings.cs` was a six-line project-wide import file containing only `global using Sussudio.Services.Contracts;`, leaving mechanical project import metadata as an extra production `.cs` file.
Files consolidated: `Sussudio/GlobalUsings.cs`
Files added: none
Net production .cs delta: -1
Partial clusters reduced: none
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; offline runtime snapshot harness; `git diff --check`; `git diff --cached --check`; regenerated `docs/architecture/Sussudio-Defragmentation-Baseline.generated.md`
CLI/MCP/pipe checks, if applicable: not applicable; namespace import moved into the owning project file
Behavior preserved: all app source files still receive the `Sussudio.Services.Contracts` namespace through project-generated global using metadata.
Notes for future agents: keep broad project-level imports in the project file when they are pure metadata and do not document a behavioral boundary.

Date: 2026-05-25
Area: automation pipe security policy locality
Problem: `AutomationPipeSecurityPolicy.cs` was a 14-line contract file containing only the fallback-security predicate for the named-pipe protocol, leaving pipe security fallback policy separated from the adjacent protocol constants and request-envelope owner that consumes the same automation contract surface.
Files consolidated: `Sussudio.Automation.Contracts/AutomationPipeSecurityPolicy.cs`
Files added: none
Net production .cs delta: -1
Partial clusters reduced: none
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; offline runtime snapshot harness; `git diff --check`; `git diff --cached --check`; regenerated `docs/architecture/Sussudio-Defragmentation-Baseline.generated.md`
CLI/MCP/pipe checks, if applicable: automation contract and named-pipe security tests preserve the public `AutomationPipeSecurityPolicy` type and predicate matrix
Behavior preserved: `AutomationPipeSecurityPolicy.ShouldDisableDefaultSecurityFallback(...)` remains public in `Sussudio.Tools`; named-pipe fallback decisions and test coverage are unchanged.
Notes for future agents: keep tiny automation pipe policy helpers beside `AutomationPipeProtocol.cs` unless they grow into a broader security collaborator.

Date: 2026-05-25
Area: diagnostic-session run context locality
Problem: `DiagnosticSessionRunState.cs` was a 63-line state holder constructed only by `DiagnosticSessionRunContext`, splitting terminal exception state, last-stage tracking, and best-effort artifact failure recording from the mutable run context that owns the lifecycle using that state.
Files consolidated: `tools/Common/DiagnosticSessionRunState.cs`
Files added: none
Net production .cs delta: -1
Partial clusters reduced: n/a; diagnostic-session shared helper file count -1
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; offline runtime snapshot harness; `git diff --check`
CLI/MCP/pipe checks, if applicable: diagnostic-session infrastructure ownership and artifact tests cover terminal state, live breadcrumb stage selection, summary last-stage projection, and runner context construction; no automation command names/IDs changed
Behavior preserved: initial last-stage value, terminal exception capture, warning text, canceled/failed/completed classification, result last-stage selection, and best-effort artifact write failure handling remain unchanged
Notes for future agents: keep mutable run lifecycle state with `DiagnosticSessionRunContext.cs`; keep `DiagnosticSessionLiveStateWriter.cs` focused on breadcrumb payload writing and throttling

Date: 2026-05-25
Area: diagnostic-session result artifact locality
Problem: `DiagnosticSessionResultArtifacts.cs` was a 74-line helper called by `DiagnosticSessionResultBuilder.BuildAndWriteAsync`, splitting pre-summary artifact path construction/writes and JSON artifact helpers from the summary write path that completes the same result.
Files consolidated: `tools/Common/DiagnosticSessionResultArtifacts.cs`
Files added: none
Net production .cs delta: -1
Partial clusters reduced: n/a; diagnostic-session shared helper file count -1
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; offline runtime snapshot harness; `git diff --check`
CLI/MCP/pipe checks, if applicable: diagnostic-session result ownership and artifact tests cover pre-summary writes, frame-ledger trace shaping, JSON artifact helpers, and summary write failure handling; no automation command names/IDs changed
Behavior preserved: summary/samples/frame-ledger/timeline artifact paths, pre-summary write stages, frame-ledger event de-duplication key, pretty JSON serialization, empty JSON object creation, and summary write failure handling remain unchanged
Notes for future agents: keep pre-summary artifact writes and summary write handling together in `DiagnosticSessionResultBuilder.cs`; keep run-context response extraction in `DiagnosticSessionRunContext.cs`

Date: 2026-05-25
Area: Flashback exporter single-file shell locality
Problem: `FlashbackExporter.SingleFile.cs` held only the synchronous single-file export shell directly called by `ExportSingleAsync`, while request routing, linked cancellation, background scheduling, progress normalization, and writer pacing lived in `FlashbackExporter.Execution.cs`; reviewing a single-file export required opening a tiny extra partial before the execution policy that invokes it.
Files consolidated: `Sussudio/Services/Flashback/FlashbackExporter.SingleFile.cs`
Files added: none
Net production .cs delta: -1
Partial clusters reduced: `FlashbackExporter` -1 file
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; offline runtime snapshot harness; `git diff --check`
CLI/MCP/pipe checks, if applicable: not applicable; no automation command names/IDs changed
Behavior preserved: single-file input/range/output validation, source overwrite refusal, temp-path overwrite refusal, export lock acquisition/release, FFmpeg initialization, stream-info lookup, bounded stream-count validation, seek warning behavior, output context/header setup, packet writing handoff, final output replacement, success/failure result shaping, native cleanup, temp cleanup, and cancellation handling remain unchanged
Notes for future agents: keep single-file request scheduling and the synchronous single-file export shell together in `FlashbackExporter.Execution.cs`; keep packet pump/rebasing state in `FlashbackExporter.SingleFilePacketReadLoop.cs`, shared stream setup in `Streams.cs`, temp/final output replacement in `OutputFiles.cs`, and validation helpers in `Validation.cs`

Date: 2026-05-25
Area: CaptureService Flashback export request locality
Problem: `CaptureService.FlashbackExportPlanning.cs` held segment metadata mapping, path normalization, PTS repair, and live-export throttle helpers that are only used while `CaptureService.FlashbackExportCore.cs` assembles `FlashbackExportRequest`, forcing request construction review across a tiny adjacent partial.
Files consolidated: `Sussudio/Services/Capture/CaptureService.FlashbackExportPlanning.cs`
Files added: none
Net production .cs delta: -1
Partial clusters reduced: `CaptureService` -1 file
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; offline runtime snapshot harness; `git diff --check`
CLI/MCP/pipe checks, if applicable: not applicable; no automation command names/IDs changed
Behavior preserved: segment path metadata lookup, active-segment exclusion, path normalization warning, segment PTS clamping, request segment construction, live queue-pressure throttle thresholds, high-resolution baseline throttling, throttle log cadence, active-file fallback request shape, force-rotate fallback behavior, and export diagnostics handoff remain unchanged
Notes for future agents: keep Flashback export request assembly, segment metadata mapping, live-export throttle policy, and force-rotate preparation together in `CaptureService.FlashbackExportCore.cs`; keep public range/last-N entry points and backend snapshot lock handoff in `FlashbackExportOperations.cs`

Date: 2026-05-25
Area: MainViewModel Flashback playback state locality
Problem: `MainViewModel.FlashbackPlaybackCommands.cs` contained read-only Flashback segment/playback snapshots, rejection status projection, marker commands, scrub/playback command routing, and automation action dispatch, while `MainViewModel.FlashbackState.cs` owned the UI state those commands read and update. Reviewing Flashback playback UI behavior required opening two tiny adjacent partials.
Files consolidated: `Sussudio/ViewModels/MainViewModel.FlashbackPlaybackCommands.cs`
Files added: none
Net production .cs delta: -1
Partial clusters reduced: `MainViewModel` -1 file
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; offline runtime snapshot harness; `git diff --check`
CLI/MCP/pipe checks, if applicable: automation surface tests cover Flashback action dispatch and async view-model ports; no automation command names/IDs changed
Behavior preserved: Flashback segment snapshot access, playback snapshot access, rejection status text/logging, scrub/seek/play/pause/go-live/nudge routing, in/out marker routing, clear-marker routing, automation Flashback action dispatch, UI-thread invocation, and buffer/status projection remain unchanged
Notes for future agents: keep Flashback playback state projection and the thin ViewModel playback command facade together in `MainViewModel.FlashbackState.cs`; keep export/save-picker behavior in `MainViewModel.FlashbackExport.cs`

Date: 2026-05-25
Area: Recording sink HDR validation locality
Problem: `HdrValidationRunner.cs` was a 100-line stop-time helper called only by `LibAvRecordingSink.StopLifecycle.cs`, splitting final-output validation from the lifecycle that decides whether a completed recording should succeed or fail.
Files consolidated: `Sussudio/Services/Recording/HdrValidationRunner.cs`
Files added: none
Net production .cs delta: -1
Partial clusters reduced: n/a; recording helper file count -1
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; offline runtime snapshot harness; `git diff --check`
CLI/MCP/pipe checks, if applicable: not applicable; no automation command names/IDs changed
Behavior preserved: HDR validator script resolution, codec selection, HDR/static-metadata arguments, expected-FPS argument formatting, 30-second process-supervisor timeout, stdout/stderr logging, timeout/start/exit-code failure details, missing-script skip behavior, and final `FinalizeResult` failure shaping remain unchanged
Notes for future agents: keep stop-time HDR script validation with `LibAvRecordingSink.StopLifecycle.cs`; keep ffprobe-based recording verification HDR policy in `RecordingVerifier.cs`

Date: 2026-05-25
Area: MainWindow test helper locality
Problem: Two tiny test helper files only re-read sources already covered by existing shared readers: fullscreen tests and shell-chrome tests both read `MainWindow.ShellChrome.Composition.cs`, while shutdown cleanup tests read `MainWindow.xaml.cs` through a duplicate helper.
Files consolidated: `tests/Sussudio.Tests/MainWindow.FullScreenOwnership.Helpers.cs`; `tests/Sussudio.Tests/MainWindow.ShutdownCleanupOwnership.Helpers.cs`
Files added: none
Net production .cs delta: 0
Net test .cs delta: -2
Partial clusters reduced: `Program` test harness -2 files
Build/tests/runtime checks: pending in current checkpoint
CLI/MCP/pipe checks, if applicable: not applicable; test helper source readers only
Behavior preserved: fullscreen, shell chrome, shutdown cleanup, Flashback polling, preview runtime, recording-finalization, and window automation ownership assertions now use the same source text through the already-existing MainWindow helper readers
Notes for future agents: prefer reusing the shared MainWindow root and shell-chrome source readers before adding another tiny `MainWindow.*Ownership.Helpers.cs` file

Date: 2026-05-25
Area: Automation diagnostics test source-family locality
Problem: The private `AutomationDiagnosticsHubSourceFamily` test DTO was split across four tiny partial files that only added grouped source-text properties and aggregate text composition, forcing diagnostics-refresh ownership tests through five helper files before the assertions themselves.
Files consolidated: `tests/Sussudio.Tests/MainViewModel.Automation.DiagnosticsRefresh.SourceFamily.Aggregate.cs`; `tests/Sussudio.Tests/MainViewModel.Automation.DiagnosticsRefresh.SourceFamily.Alerts.cs`; `tests/Sussudio.Tests/MainViewModel.Automation.DiagnosticsRefresh.SourceFamily.DiagnosticEvaluation.cs`; `tests/Sussudio.Tests/MainViewModel.Automation.DiagnosticsRefresh.SourceFamily.SnapshotProjection.cs`
Files added: none
Net production .cs delta: 0
Net test .cs delta: -4
Partial clusters reduced: `Program` test harness -4 files
Build/tests/runtime checks: pending in current checkpoint
CLI/MCP/pipe checks, if applicable: not applicable; test source readers only
Behavior preserved: diagnostics-refresh tests still read the same AutomationDiagnosticsHub source files, source-family fields, snapshot-projection fields, diagnostic-evaluation fields, alert fields, and aggregate `SourceFamilyText`
Notes for future agents: keep the diagnostics source-family reader, field list, and aggregate text in `MainViewModel.Automation.DiagnosticsRefresh.SourceFamily.cs`; add a new helper file only for executable assertions or a genuinely reusable reader

Date: 2026-05-25
Area: MainWindow test source reader locality
Problem: Legacy catalog tests and xUnit tests had two tiny files for the same `MainWindow.xaml.cs` source reader: `ReadMainWindowCompositionSource()` and `MainWindowCompositionSource.Read()`.
Files consolidated: `tests/Sussudio.Tests/MainWindowCompositionSource.cs`
Files added: none
Net production .cs delta: 0
Net test .cs delta: -1
Partial clusters reduced: n/a; MainWindow test helper file count -1
Build/tests/runtime checks: pending in current checkpoint
CLI/MCP/pipe checks, if applicable: not applicable; test source readers only
Behavior preserved: legacy `Program`-partial ownership tests and namespaced xUnit helper callers still read normalized `Sussudio/MainWindow.xaml.cs` text through their existing APIs
Notes for future agents: keep both MainWindow root source-reader APIs together in `MainWindow.CompositionSource.cs` unless one harness is retired

Date: 2026-05-25
Area: Legacy runtime harness shim locality
Problem: `HarnessCheckCatalog.cs` was an 8-line no-op compatibility method returning an empty result list, while `Program.cs` is the only caller and owns the offline `dotnet exec` runner.
Files consolidated: `tests/Sussudio.Tests/HarnessCheckCatalog.cs`
Files added: none
Net production .cs delta: 0
Net test .cs delta: -1
Partial clusters reduced: `Program` test harness -1 file
Build/tests/runtime checks: pending in current checkpoint
CLI/MCP/pipe checks, if applicable: offline runtime harness still runs through `dotnet exec tests\Sussudio.Tests\bin\Debug\net8.0\Sussudio.Tests.dll ...`
Behavior preserved: the offline compatibility runner still returns an empty check list and reports success after the app assembly loads; executable coverage remains in focused xUnit slices
Notes for future agents: keep the no-op `RunAllChecksAsync` shim with `Program.cs`; add new coverage to xUnit instead of restoring a harness catalog

Date: 2026-05-25
Area: MainWindow adapter test source-reader locality
Problem: Eight tiny MainWindow ownership helper files each exposed a single source-reader method for adjacent adapter files. That kept the test helper file count high and scattered the source-reader map for the same MainWindow adapter family.
Files consolidated: `tests/Sussudio.Tests/MainWindow.CaptureSelectionBindingsOwnership.Helpers.cs`; `tests/Sussudio.Tests/MainWindow.FlashbackOwnership.Helpers.cs`; `tests/Sussudio.Tests/MainWindow.PreviewRendererOwnership.Helpers.cs`; `tests/Sussudio.Tests/MainWindow.PreviewStartupOwnership.Helpers.cs`; `tests/Sussudio.Tests/MainWindow.PreviewTransitionsOwnership.Helpers.cs`; `tests/Sussudio.Tests/MainWindow.PropertyChangedPreviewOwnership.Helpers.cs`; `tests/Sussudio.Tests/MainWindow.ShellChromeOwnership.Helpers.cs`; `tests/Sussudio.Tests/MainWindow.StatsOverlayOwnership.Helpers.cs`
Files added: none
Net production .cs delta: 0
Net test .cs delta: -8
Partial clusters reduced: `Program` test harness -7 files; namespaced MainWindow stats source reader -1 file
Build/tests/runtime checks: pending in current checkpoint
CLI/MCP/pipe checks, if applicable: not applicable; test source readers only
Behavior preserved: all legacy `Program`-partial MainWindow adapter reader APIs and the namespaced `MainWindowStatsOverlaySource.Read()` API still read the same normalized source files
Notes for future agents: keep MainWindow root and adapter source readers in `MainWindow.CompositionSource.cs`; do not add another one-method `MainWindow.*Ownership.Helpers.cs` file for an adapter source unless it gains executable assertions

Date: 2026-05-25
Area: D3D preview xUnit execution-surface locality
Problem: The D3D preview xUnit execution surface was scattered across nine tiny wrapper files, each only loading the target assembly and delegating former legacy harness checks to `Program` methods. Reviewing the D3D xUnit coverage map required opening a file per subtopic even though the executable surface is one feature family.
Files consolidated: `tests/Sussudio.Tests/XUnit.PresentationPreviewD3DPacingContractsTests.cs`; `tests/Sussudio.Tests/XUnit.PresentationPreviewD3DGeometryContractsTests.cs`; `tests/Sussudio.Tests/XUnit.PresentationPreviewD3DCadenceContractsTests.cs`; `tests/Sussudio.Tests/XUnit.PresentationPreviewD3DDeviceLostContractsTests.cs`; `tests/Sussudio.Tests/XUnit.PresentationPreviewD3DDiagnosticsContractsTests.cs`; `tests/Sussudio.Tests/XUnit.PresentationPreviewD3DContractsAndMetricsOwnershipTests.cs`; `tests/Sussudio.Tests/XUnit.PresentationPreviewD3DRuntimeCaptureOwnershipTests.cs`; `tests/Sussudio.Tests/XUnit.PresentationPreviewD3DRenderSetupOwnershipTests.cs`; `tests/Sussudio.Tests/XUnit.PresentationPreviewD3DRenderPipelineOwnershipTests.cs`
Files added: `tests/Sussudio.Tests/XUnit.PresentationPreviewD3DContractsTests.cs`
Net production .cs delta: 0
Net test .cs delta: -8
Partial clusters reduced: n/a; D3D xUnit wrapper file count -8
Build/tests/runtime checks: pending in current checkpoint
CLI/MCP/pipe checks, if applicable: not applicable; xUnit wrapper consolidation only
Behavior preserved: the same public test classes, constructors, `[Fact]` method names, and delegated `Program` checks remain available under one D3D contracts file
Notes for future agents: keep D3D preview xUnit wrapper classes together in `XUnit.PresentationPreviewD3DContractsTests.cs`; add new D3D execution wrappers there unless they need independent fixtures or executable helper state

Date: 2026-05-25
Area: Preview startup xUnit execution-surface locality
Problem: The preview-startup xUnit execution surface was split across four tiny wrapper files for ownership, behavior, signal/failure text, and ordering checks. Each file only loaded the target assembly and delegated former legacy harness checks to `Program` methods.
Files consolidated: `tests/Sussudio.Tests/XUnit.PresentationPreviewStartupOwnershipContractsTests.cs`; `tests/Sussudio.Tests/XUnit.PresentationPreviewStartupBehaviorContractsTests.cs`; `tests/Sussudio.Tests/XUnit.PresentationPreviewStartupSignalContractsTests.cs`; `tests/Sussudio.Tests/XUnit.PresentationPreviewStartupOrderingContractsTests.cs`
Files added: `tests/Sussudio.Tests/XUnit.PresentationPreviewStartupContractsTests.cs`
Net production .cs delta: 0
Net test .cs delta: -3
Partial clusters reduced: n/a; preview-startup xUnit wrapper file count -3
Build/tests/runtime checks: pending in current checkpoint
CLI/MCP/pipe checks, if applicable: not applicable; xUnit wrapper consolidation only
Behavior preserved: the same public test classes, constructors, `[Fact]` method names, and delegated `Program` checks remain available under one preview-startup contracts file
Notes for future agents: keep preview-startup xUnit wrapper classes together in `XUnit.PresentationPreviewStartupContractsTests.cs`; add new startup execution wrappers there unless they need independent fixtures or executable helper state

Date: 2026-05-25
Area: MCP window and preview tool locality
Problem: `PreviewTools.cs` was a 108-line MCP tool file containing the preview toggle, recording toggle, and wait-condition tool types, while `WindowTools.cs` owned adjacent window, full-screen, recordings-folder, and UI visibility/settings automation controls. Changing user-visible window/preview control routes still required opening two small MCP files even though all public tool types use the same pipe-client formatting path.
Files consolidated: `tools/McpServer/Tools/PreviewTools.cs`
Files added: none
Net production .cs delta: -1
Net test .cs delta: 0
Partial clusters reduced: n/a; MCP window/preview tool file count -1
Build/tests/runtime checks: pending in current checkpoint
CLI/MCP/pipe checks, if applicable: MCP tool routing tests cover preview, recording, wait-condition, window actions, and typed automation command routing; public MCP tool class names and method names remain unchanged
Behavior preserved: `PreviewTools.control_preview`, `RecordingTools.control_recording`, `WaitTools.wait_for_condition`, response-timeout selection, formatted condition text, `WindowTools`, and `UiSettingsTools` remain on the same public MCP surface
Notes for future agents: keep window, UI settings, preview/recording controls, and wait-condition MCP wrappers together in `WindowTools.cs` unless a tool type gains independent helper state or a separate transport seam

Date: 2026-05-25
Area: ssctl command-handler locality
Problem: The ssctl CLI command router was still split across four small command-family partial files plus the root handler file. Each family was only a section of the same transport/argument/payload surface, so changing CLI routing or reviewing command ownership required hopping across files without gaining a real collaborator boundary.
Files consolidated: `tools/ssctl/CommandHandlers.Observability.cs`; `tools/ssctl/CommandHandlers.CaptureControls.cs`; `tools/ssctl/CommandHandlers.Window.cs`; `tools/ssctl/CommandHandlers.Flashback.cs`
Files added: none
Net production .cs delta: -4
Net test .cs delta: 0
Partial clusters reduced: `CommandHandlers` tool partial family removed; command-family sections now live in `tools/ssctl/CommandHandlers.cs`
Build/tests/runtime checks: pending in current checkpoint
CLI/MCP/pipe checks, if applicable: ssctl routing/source-ownership/help/protocol tests still cover typed command IDs, request payloads, Flashback export flags, UI visibility routes, observability routes, and diagnostic-session runner invocation
Behavior preserved: public ssctl command names, accepted aliases, argument parsing, automation command IDs, payload field names, response formatting, and diagnostic-session dynamic command forwarding are unchanged
Notes for future agents: keep ssctl command handlers in `CommandHandlers.cs`; add a new `CommandHandlers.*.cs` file only for a real independently tested collaborator, not as a partial-class section marker

Date: 2026-05-25
Area: NativeXuAudioProbe command support locality
Problem: `Program.Commands.cs` was a 40-line support file containing Native XU command IDs plus one raw-payload formatter. Those helpers are not an independent workflow; they are consumed by the default experiment, AT command, and reporting flows inside the same probe tool.
Files consolidated: `tools/NativeXuAudioProbe/Program.Commands.cs`
Files added: none
Net production .cs delta: -1
Net test .cs delta: 0
Partial clusters reduced: n/a; NativeXuAudioProbe support file count -1
Build/tests/runtime checks: pending in current checkpoint
CLI/MCP/pipe checks, if applicable: affected NativeXuAudioProbe build and source-ownership tests cover the moved command IDs and raw formatter
Behavior preserved: `NativeXuProbeCommands` constants, `NativeXuProbeFormatting.FormatRaw`, AT command read/write/set-input behavior, default experiment payloads, and reporting text stay unchanged
Notes for future agents: keep shared Native XU command IDs and raw-payload formatting with `Program.DefaultExperiment.cs` unless they become a shared library contract or an independently tested command-support type

Date: 2026-05-25
Area: MCP diagnostic-session xUnit execution-surface locality
Problem: Seven tiny xUnit wrapper files each owned one former legacy diagnostic-session catalog band, but every file only exposed public wrapper classes and delegated to `Program` checks. Reviewing the diagnostic-session xUnit execution surface required opening a file per band without gaining independent fixtures or helper state.
Files consolidated: `tests/Sussudio.Tests/XUnit.McpDiagnosticSessionInfrastructureContractsTests.cs`; `tests/Sussudio.Tests/XUnit.McpDiagnosticSessionResultSurfaceContractsTests.cs`; `tests/Sussudio.Tests/XUnit.McpDiagnosticSessionCommandRunContextContractsTests.cs`; `tests/Sussudio.Tests/XUnit.McpDiagnosticSessionScenarioExecutionContractsTests.cs`; `tests/Sussudio.Tests/XUnit.McpDiagnosticSessionFlashbackContractsTests.cs`; `tests/Sussudio.Tests/XUnit.McpDiagnosticSessionCoreContractsTests.cs`; `tests/Sussudio.Tests/XUnit.McpDiagnosticSessionRunnerBehaviorContractsTests.cs`
Files added: `tests/Sussudio.Tests/XUnit.McpDiagnosticSessionContractsTests.cs`
Net production .cs delta: 0
Net test .cs delta: -6
Partial clusters reduced: n/a; MCP diagnostic-session xUnit wrapper file count -6
Build/tests/runtime checks: pending in current checkpoint
CLI/MCP/pipe checks, if applicable: not applicable; xUnit wrapper consolidation only
Behavior preserved: the same public test classes, `[Fact]` method names, and delegated diagnostic-session `Program` checks remain available under one diagnostic-session contracts file
Notes for future agents: keep MCP diagnostic-session xUnit wrapper classes together in `XUnit.McpDiagnosticSessionContractsTests.cs`; add new wrapper classes there unless a band needs independent fixtures or executable helper state

Date: 2026-05-25
Area: MainViewModel presentation-preview xUnit execution-surface locality
Problem: Six tiny xUnit wrapper files each owned one MainViewModel presentation-preview group, but every file only loaded the target assembly and delegated to `Program` checks. Reviewing MainViewModel presentation-preview xUnit coverage required opening a file per subtopic without gaining independent fixtures or helper state.
Files consolidated: `tests/Sussudio.Tests/XUnit.PresentationPreviewMainViewModelInitialContractsTests.cs`; `tests/Sussudio.Tests/XUnit.PresentationPreviewMainViewModelAudioControlsContractsTests.cs`; `tests/Sussudio.Tests/XUnit.PresentationPreviewMainViewModelOutputPathContractsTests.cs`; `tests/Sussudio.Tests/XUnit.PresentationPreviewMainViewModelSourceTelemetryContractsTests.cs`; `tests/Sussudio.Tests/XUnit.PresentationPreviewMainViewModelDependencyCompositionContractsTests.cs`; `tests/Sussudio.Tests/XUnit.PresentationPreviewMainViewModelRuntimeContractsTests.cs`
Files added: `tests/Sussudio.Tests/XUnit.PresentationPreviewMainViewModelContractsTests.cs`
Net production .cs delta: 0
Net test .cs delta: -5
Partial clusters reduced: n/a; MainViewModel presentation-preview xUnit wrapper file count -5
Build/tests/runtime checks: pending in current checkpoint
CLI/MCP/pipe checks, if applicable: not applicable; xUnit wrapper consolidation only
Behavior preserved: the same public test classes, constructors, `[Fact]` method names, target-assembly bootstrap calls, and delegated `Program` checks remain available under one MainViewModel contracts file
Notes for future agents: keep MainViewModel presentation-preview xUnit wrapper classes together in `XUnit.PresentationPreviewMainViewModelContractsTests.cs`; add new wrapper classes there unless a group needs independent fixtures or executable helper state

Date: 2026-05-25
Area: MainWindow presentation-preview xUnit execution-surface locality
Problem: Eleven tiny presentation-preview xUnit wrapper files each owned one MainWindow or adjacent capture/selection group, but every file only loaded the target assembly and delegated to `Program` checks. Reviewing the MainWindow presentation-preview xUnit execution surface required opening a file per subtopic without gaining independent fixtures or helper state.
Files consolidated: `tests/Sussudio.Tests/XUnit.PresentationPreviewAudioControlContractsTests.cs`; `tests/Sussudio.Tests/XUnit.PresentationPreviewCaptureRuntimeGuardContractsTests.cs`; `tests/Sussudio.Tests/XUnit.PresentationPreviewCaptureSelectionContractsTests.cs`; `tests/Sussudio.Tests/XUnit.PresentationPreviewLaunchStartupContractsTests.cs`; `tests/Sussudio.Tests/XUnit.PresentationPreviewRecordingContractsTests.cs`; `tests/Sussudio.Tests/XUnit.PresentationPreviewResolutionSelectionContractsTests.cs`; `tests/Sussudio.Tests/XUnit.PresentationPreviewResponsiveLayoutContractsTests.cs`; `tests/Sussudio.Tests/XUnit.PresentationPreviewScreenshotContractsTests.cs`; `tests/Sussudio.Tests/XUnit.PresentationPreviewShellChromeContractsTests.cs`; `tests/Sussudio.Tests/XUnit.PresentationPreviewVisualShellContractsTests.cs`; `tests/Sussudio.Tests/XUnit.PresentationPreviewWindowLifecycleContractsTests.cs`
Files added: `tests/Sussudio.Tests/XUnit.PresentationPreviewMainWindowContractsTests.cs`
Net production .cs delta: 0
Net test .cs delta: -10
Partial clusters reduced: n/a; MainWindow presentation-preview xUnit wrapper file count -10
Build/tests/runtime checks: pending in current checkpoint
CLI/MCP/pipe checks, if applicable: not applicable; xUnit wrapper consolidation only
Behavior preserved: the same public test classes, constructors, `[Fact]` method names, target-assembly bootstrap calls, and delegated `Program` checks remain available under one MainWindow contracts file
Notes for future agents: keep MainWindow presentation-preview xUnit wrapper classes together in `XUnit.PresentationPreviewMainWindowContractsTests.cs`; add new wrapper classes there unless a group needs independent fixtures or executable helper state

Date: 2026-05-25
Area: Remaining presentation-preview xUnit execution-surface locality
Problem: Ten remaining tiny presentation-preview xUnit wrapper files each contained only a target-assembly bootstrap constructor plus delegated `Program` facts. They were meaningful test identities, but not meaningful file boundaries, so reviewing MainWindow runtime, MainViewModel selection policy, startup lifecycle, and Flashback buffer execution still required opening a separate shell per subtopic.
Files consolidated: `tests/Sussudio.Tests/XUnit.PresentationPreviewMainWindowInitialContractsTests.cs`; `tests/Sussudio.Tests/XUnit.PresentationPreviewRuntimeShellContractsTests.cs`; `tests/Sussudio.Tests/XUnit.PresentationPreviewRuntimePolicyContractsTests.cs`; `tests/Sussudio.Tests/XUnit.PresentationPreviewCaptureOptionContractsTests.cs`; `tests/Sussudio.Tests/XUnit.PresentationPreviewOutputPathContractsTests.cs`; `tests/Sussudio.Tests/XUnit.PresentationPreviewFrameRateSelectionContractsTests.cs`; `tests/Sussudio.Tests/XUnit.PresentationPreviewDeviceFormatProbeRetargetContractsTests.cs`; `tests/Sussudio.Tests/XUnit.PresentationPreviewCaptureSelectionPolicyContractsTests.cs`; `tests/Sussudio.Tests/XUnit.PresentationPreviewCapturePreviewLifecycleContractsTests.cs`; `tests/Sussudio.Tests/XUnit.PresentationPreviewCaptureFlashbackBufferContractsTests.cs`
Files added: none
Net production .cs delta: 0
Net test .cs delta: -10
Partial clusters reduced: n/a; remaining small presentation-preview xUnit wrapper file count -10
Build/tests/runtime checks: pending in current checkpoint
CLI/MCP/pipe checks, if applicable: not applicable; xUnit wrapper consolidation only
Behavior preserved: the same public test classes, constructors, `[Fact]` method names, target-assembly bootstrap calls, and delegated `Program` checks remain available inside the MainWindow, MainViewModel, and Startup presentation-preview contracts files
Notes for future agents: keep these wrapper classes in their parent presentation-preview contracts files unless a group gains independent fixtures or executable helper state; public class identity matters more than one wrapper file per legacy catalog band

Date: 2026-05-25
Area: Tool xUnit execution-surface locality
Problem: Five tiny tool-side xUnit wrapper files each contained only public wrapper classes that forwarded to legacy `Program` checks. Tool probe, formatter, model, and native-probe contract execution was still scattered across one shell file per former catalog band without independent fixtures or helper state.
Files consolidated: `tests/Sussudio.Tests/XUnit.AutomationSnapshotFormatterContractsTests.cs`; `tests/Sussudio.Tests/XUnit.SsctlFormatterContractsTests.cs`; `tests/Sussudio.Tests/XUnit.ToolProbeContractsTests.cs`; `tests/Sussudio.Tests/XUnit.ToolModelContractsTests.cs`; `tests/Sussudio.Tests/XUnit.NativeToolProbeContractsTests.cs`
Files added: `tests/Sussudio.Tests/XUnit.ToolContractsTests.cs`
Net production .cs delta: 0
Net test .cs delta: -4
Partial clusters reduced: n/a; tool-side xUnit wrapper file count -4
Build/tests/runtime checks: pending in current checkpoint
CLI/MCP/pipe checks, if applicable: not applicable; xUnit wrapper consolidation only
Behavior preserved: the same public test classes, `[Fact]` method names, target-assembly bootstrap calls where they existed, and delegated `Program` checks remain available under one tool contracts file
Notes for future agents: keep tool-side xUnit wrapper classes in `XUnit.ToolContractsTests.cs`; add new wrapper classes there unless a group needs independent fixtures or executable helper state

Date: 2026-05-25
Area: Automation xUnit execution-surface locality
Problem: Seven automation xUnit wrapper files still mirrored former legacy catalog bands even though each file only exposed public wrapper classes and delegated `Program` checks. Reviewing automation app-surface, dispatcher, ViewModel/Flashback UI, capture/Flashback routing, snapshot projection, catalog, and diagnostics-loop execution required opening one shell per band without gaining independent fixtures or helper state.
Files consolidated: `tests/Sussudio.Tests/XUnit.AutomationAppSurfaceContractsTests.cs`; `tests/Sussudio.Tests/XUnit.AutomationCatalogContractsTests.cs`; `tests/Sussudio.Tests/XUnit.AutomationDiagnosticsLoopContractsTests.cs`; `tests/Sussudio.Tests/XUnit.AutomationDispatcherContractsTests.cs`; `tests/Sussudio.Tests/XUnit.AutomationViewModelFlashbackUiContractsTests.cs`; `tests/Sussudio.Tests/XUnit.AutomationSnapshotProjectionContractsTests.cs`; `tests/Sussudio.Tests/XUnit.AutomationCaptureFlashbackRoutingContractsTests.cs`
Files added: `tests/Sussudio.Tests/XUnit.AutomationContractsTests.cs`
Net production .cs delta: 0
Net test .cs delta: -6
Partial clusters reduced: n/a; automation xUnit wrapper file count -6
Build/tests/runtime checks: pending in current checkpoint
CLI/MCP/pipe checks, if applicable: not applicable; xUnit wrapper consolidation only
Behavior preserved: the same public test classes, constructors, `[Fact]` method names, target-assembly bootstrap calls, and delegated `Program` checks remain available under one automation contracts file
Notes for future agents: keep automation xUnit wrapper classes in `XUnit.AutomationContractsTests.cs`; add new wrapper classes there unless a group needs independent fixtures or executable helper state

Date: 2026-05-25
Area: MCP tool xUnit execution-surface locality
Problem: Three MCP xUnit wrapper files still mirrored former legacy catalog bands for general tool-surface, performance/probe, and window/preview checks. Each file only exposed public wrapper classes and delegated `Program` checks, so reviewing MCP tool xUnit execution required opening separate shell files without gaining independent fixtures or helper state.
Files consolidated: `tests/Sussudio.Tests/XUnit.McpToolSurfaceContractsTests.cs`; `tests/Sussudio.Tests/XUnit.McpPerformanceToolContractsTests.cs`; `tests/Sussudio.Tests/XUnit.McpWindowPreviewToolContractsTests.cs`
Files added: `tests/Sussudio.Tests/XUnit.McpToolContractsTests.cs`
Net production .cs delta: 0
Net test .cs delta: -2
Partial clusters reduced: n/a; MCP tool xUnit wrapper file count -2
Build/tests/runtime checks: pending in current checkpoint
CLI/MCP/pipe checks, if applicable: not applicable; xUnit wrapper consolidation only
Behavior preserved: the same public test classes, `[Fact]` method names, and delegated `Program` checks remain available under one MCP tool contracts file
Notes for future agents: keep general MCP tool xUnit wrapper classes in `XUnit.McpToolContractsTests.cs`; add new wrapper classes there unless a group needs independent fixtures or executable helper state

Date: 2026-05-25
Area: Automation snapshot-model test locality
Problem: Eleven `SnapshotModelsTests` partial files split the AutomationSnapshot/AutomationOptions DTO shape contract by metric band. Each file contributed a handful of facts or shared property-list helpers to the same partial test type, so reviewing the automation snapshot DTO contract required opening many tiny files without gaining an independent fixture or helper boundary.
Files consolidated: `tests/Sussudio.Tests/SnapshotModels.Automation.CpuMjpegContractSpec.cs`; `tests/Sussudio.Tests/SnapshotModels.Automation.CpuMjpeg.Tests.cs`; `tests/Sussudio.Tests/SnapshotModels.Automation.MjpegPreview.Tests.cs`; `tests/Sussudio.Tests/SnapshotModels.Automation.PreviewDiagnostics.Tests.cs`; `tests/Sussudio.Tests/SnapshotModels.Automation.CaptureCommands.Tests.cs`; `tests/Sussudio.Tests/SnapshotModels.Automation.Recording.Tests.cs`; `tests/Sussudio.Tests/SnapshotModels.Automation.FlashbackRecording.Tests.cs`; `tests/Sussudio.Tests/SnapshotModels.Automation.FlashbackPlayback.Tests.cs`; `tests/Sussudio.Tests/SnapshotModels.Automation.FlashbackExport.Tests.cs`; `tests/Sussudio.Tests/SnapshotModels.Automation.VisualCadence.Tests.cs`; `tests/Sussudio.Tests/SnapshotModels.Automation.Options.Tests.cs`
Files added: `tests/Sussudio.Tests/SnapshotModels.Automation.Tests.cs`
Net production .cs delta: 0
Net test .cs delta: -10
Partial clusters reduced: `SnapshotModelsTests` automation partial-family file count -10
Build/tests/runtime checks: pending in current checkpoint
CLI/MCP/pipe checks, if applicable: not applicable; DTO contract test consolidation only
Behavior preserved: the same xUnit facts, shared property-list helpers, reflection checks, source-text assertions, and AutomationSnapshot/AutomationOptions property coverage remain under one automation snapshot-model test file
Notes for future agents: keep automation snapshot DTO shape checks in `SnapshotModels.Automation.Tests.cs`; add a new file only for a distinct snapshot DTO family or independent fixture/helper boundary

Date: 2026-05-25
Area: CaptureHealth snapshot-model test locality
Problem: Six `SnapshotModelsTests` partial files split the CaptureHealthSnapshot and SourceTelemetryDetailEntry DTO contract into property spec, defaults, source-telemetry detail, round-trip fixture, JSON, and root orchestration fragments. All fragments contributed helper methods or one fact to the same DTO contract surface, so changing CaptureHealth snapshot shape required opening several tiny files.
Files consolidated: `tests/Sussudio.Tests/SnapshotModels.CaptureHealth.PropertySpec.cs`; `tests/Sussudio.Tests/SnapshotModels.CaptureHealth.Defaults.Tests.cs`; `tests/Sussudio.Tests/SnapshotModels.CaptureHealth.SourceTelemetryDetail.Tests.cs`; `tests/Sussudio.Tests/SnapshotModels.CaptureHealth.RoundTrip.Tests.cs`; `tests/Sussudio.Tests/SnapshotModels.CaptureHealth.Json.Tests.cs`
Files added: none
Net production .cs delta: 0
Net test .cs delta: -5
Partial clusters reduced: `SnapshotModelsTests` CaptureHealth partial-family file count -5
Build/tests/runtime checks: pending in current checkpoint
CLI/MCP/pipe checks, if applicable: not applicable; DTO contract test consolidation only
Behavior preserved: the same CaptureHealthSnapshot/SourceTelemetryDetailEntry property specs, defaults, round-trip fixture, direct assertions, reflection JSON assertions, and source-shape checks remain in `SnapshotModels.CaptureHealth.Tests.cs`
Notes for future agents: keep CaptureHealth snapshot DTO shape checks in `SnapshotModels.CaptureHealth.Tests.cs`; add a new file only for a distinct snapshot DTO family or independent fixture/helper boundary

Date: 2026-05-25
Area: CaptureDiagnostics snapshot-model test locality
Problem: The CaptureDiagnosticsSnapshot DTO contract still split its registered property spec into a separate `SnapshotModelsTests` partial file while the neighboring defaults/round-trip/JSON/source-shape fact was the only direct owner. The helper also serves CaptureHealth inheritance coverage, but it is still part of the CaptureDiagnostics DTO contract and did not need an independent file boundary.
Files consolidated: `tests/Sussudio.Tests/SnapshotModels.CaptureDiagnostics.PropertySpec.cs`
Files added: none
Net production .cs delta: 0
Net test .cs delta: -1
Partial clusters reduced: `SnapshotModelsTests` CaptureDiagnostics partial-family file count -1
Build/tests/runtime checks: pending in current checkpoint
CLI/MCP/pipe checks, if applicable: not applicable; DTO contract test consolidation only
Behavior preserved: the same CaptureDiagnosticsSnapshot registered property spec remains available to CaptureDiagnostics and CaptureHealth snapshot tests through the same private helper on `SnapshotModelsTests`
Notes for future agents: keep CaptureDiagnostics snapshot DTO shape checks in `SnapshotModels.CaptureDiagnostics.Tests.cs`; add a new file only for a distinct snapshot DTO family or independent fixture/helper boundary

Date: 2026-05-25
Area: Service namespace architecture test locality
Problem: Two very small `Program` partial files only orchestrated existing service namespace checks: the harness-visible entry point and the MainViewModel source-ownership dispatcher. They did not own independent assertions or fixtures, so maintaining them as separate files inflated the architecture-test shell count.
Files consolidated: `tests/Sussudio.Tests/ServiceNamespace.Tests.cs`; `tests/Sussudio.Tests/ServiceNamespace.SourceOwnership.MainViewModelSource.Tests.cs`
Files added: none
Net production .cs delta: 0
Net test .cs delta: -2
Partial clusters reduced: service namespace architecture-test partial file count -2
Build/tests/runtime checks: pending in current checkpoint
CLI/MCP/pipe checks, if applicable: not applicable; architecture test locality only
Behavior preserved: the same harness-visible `ServiceNamespaces_FollowServiceFolders` entry point and MainViewModel source ownership dispatcher remain on the `Program` partial type
Notes for future agents: keep the service namespace harness entry point with `ServiceNamespace.FolderRules.Tests.cs` and keep MainViewModel source ownership orchestration with `ServiceNamespace.SourceOwnership.ServicesLayer.Tests.cs`; create a new service namespace file only for a distinct assertion owner or helper boundary

Date: 2026-05-25
Area: xUnit wrapper execution-surface locality
Problem: Three tiny xUnit wrapper files only exposed public test classes that delegated to existing `Program` checks: presentation-preview harness registration, ssctl command-handler routing/help coverage, and architecture-doc reference integrity. Each had a natural neighboring execution owner, so keeping them separate created file-count noise without preserving fixture or helper boundaries.
Files consolidated: `tests/Sussudio.Tests/XUnit.PresentationPreviewHarnessRegistrationTests.cs`; `tests/Sussudio.Tests/XUnit.SsctlCommandHandlerContractsTests.cs`; `tests/Sussudio.Tests/XUnit.ArchitectureDocsReferenceIntegrityTests.cs`
Files added: none
Net production .cs delta: 0
Net test .cs delta: -3
Partial clusters reduced: xUnit wrapper execution-surface file count -3
Build/tests/runtime checks: pending in current checkpoint
CLI/MCP/pipe checks, if applicable: not applicable; xUnit wrapper consolidation only
Behavior preserved: the same public test class names, `[Fact]` method names, delegated `Program` checks, and target-assembly bootstrap behavior remain available in neighboring xUnit owner files
Notes for future agents: keep presentation-preview harness registration wrappers with `XUnit.PresentationPreviewStartupContractsTests.cs`, ssctl command-handler wrappers with `XUnit.ToolContractsTests.cs`, and architecture-doc reference wrappers with `XUnit.ArchitectureDocsAgentMapOwnershipTests.cs` unless a group needs independent fixtures or executable helper state

Date: 2026-05-25
Area: Small test dispatcher locality
Problem: Three tiny test files only dispatched to nearby owner checks: the project-build xUnit wrapper, diagnostics-refresh core ownership dispatcher, and capture selection-binding device-audio projection check. Each belonged with a surrounding root/owner file and did not provide an independent fixture, helper boundary, or behavior owner.
Files consolidated: `tests/Sussudio.Tests/XUnit.ProjectBuildContractsTests.cs`; `tests/Sussudio.Tests/MainViewModel.Automation.DiagnosticsRefresh.CoreOwnership.Tests.cs`; `tests/Sussudio.Tests/MainWindow.ControllerOwnership.Capture.SelectionBindings.DeviceAudio.Tests.cs`
Files added: none
Net production .cs delta: 0
Net test .cs delta: -3
Partial clusters reduced: small test dispatcher/wrapper file count -3
Build/tests/runtime checks: pending in current checkpoint
CLI/MCP/pipe checks, if applicable: not applicable; test wrapper/dispatcher consolidation only
Behavior preserved: the same public xUnit class/fact names and `Program` helper names remain available from neighboring owner files, and the presentation-preview harness inventory now points at the consolidated selection-binding owner file
Notes for future agents: keep project-build xUnit execution with `ProjectBuildContracts.Tests.cs`, diagnostics-refresh root orchestration with `MainViewModel.Automation.DiagnosticsRefresh.Tests.cs`, and capture selection-binding device-audio projection checks with `MainWindow.ControllerOwnership.Capture.SelectionBindings.Tests.cs`

Date: 2026-05-25
Area: RecordingVerifier integration test locality
Problem: Seven RecordingVerifier integration scenario files split ffprobe failure, priority, codec, Flashback verification-format, mismatch, HDR, and cadence cases away from the single fake `IProcessSupervisor` seam and runtime-snapshot helper they all depend on. Reviewing or changing recording verification behavior required opening many small scenario fragments plus the helper owner.
Files consolidated: `tests/Sussudio.Tests/RecordingVerifier.Integration.Failures.Tests.cs`; `tests/Sussudio.Tests/RecordingVerifier.Integration.Priority.Tests.cs`; `tests/Sussudio.Tests/RecordingVerifier.Integration.Codec.Tests.cs`; `tests/Sussudio.Tests/RecordingVerifier.Integration.Flashback.Tests.cs`; `tests/Sussudio.Tests/RecordingVerifier.Integration.Mismatches.Tests.cs`; `tests/Sussudio.Tests/RecordingVerifier.Integration.Hdr.Tests.cs`; `tests/Sussudio.Tests/RecordingVerifier.Integration.Cadence.Tests.cs`
Files added: none
Net production .cs delta: 0
Net test .cs delta: -7
Partial clusters reduced: `Program` RecordingVerifier integration partial-family file count -7
Build/tests/runtime checks: pending in current checkpoint
CLI/MCP/pipe checks, if applicable: not applicable; recording verifier integration test consolidation only
Behavior preserved: the same internal `Program` test method names, fake supervisor seam, runtime snapshot helpers, and verifier invocation path remain in `RecordingVerifier.Integration.Tests.cs`
Notes for future agents: keep fake-ffprobe RecordingVerifier integration scenarios in `RecordingVerifier.Integration.Tests.cs` unless a scenario grows a distinct fixture or external process seam

Date: 2026-05-25
Area: MCP command-routing test locality
Problem: Eight MCP command-routing test fragments split one routing surface by tool band even though they all used the same pipe-capture helpers, reflection invocation seam, and command-request assertions. Reviewing MCP command routing required opening several small files before reaching the larger verification and host cases.
Files consolidated: `tests/Sussudio.Tests/McpToolSurface.CommandRouting.Device.Tests.cs`; `tests/Sussudio.Tests/McpToolSurface.CommandRouting.Capture.Tests.cs`; `tests/Sussudio.Tests/McpToolSurface.CommandRouting.Pipeline.Tests.cs`; `tests/Sussudio.Tests/McpToolSurface.CommandRouting.Recording.Tests.cs`; `tests/Sussudio.Tests/McpToolSurface.CommandRouting.Ui.Tests.cs`; `tests/Sussudio.Tests/McpToolSurface.CommandRouting.Formatting.Tests.cs`; `tests/Sussudio.Tests/McpToolSurface.CommandRouting.Host.Tests.cs`; `tests/Sussudio.Tests/McpToolSurface.CommandRouting.Verification.Tests.cs`
Files added: `tests/Sussudio.Tests/McpToolSurface.CommandRouting.Tests.cs`
Net production .cs delta: 0
Net test .cs delta: -7
Partial clusters reduced: `Program` MCP command-routing partial-family file count -7
Build/tests/runtime checks: pending in current checkpoint
CLI/MCP/pipe checks, if applicable: MCP command-routing tests remain in the xUnit suite and exercise the same named-pipe tool calls
Behavior preserved: the same internal `Program` test method names, tool reflection calls, command-id assertions, host JSON-RPC checks, and verification formatting checks remain in one command-routing owner file
Notes for future agents: keep MCP command-routing tests in `McpToolSurface.CommandRouting.Tests.cs` unless a route group grows a distinct fixture, process lifecycle, or helper seam

Date: 2026-05-25
Area: MCP window-preview test locality
Problem: Seven MCP window-preview tool-surface fragments split wait, window action, preview toggle, Flashback toggle, screenshot, preview-frame-capture, and probe checks across separate `Program` partial files while sharing the same MCP reflection and pipe helper seams. Reviewing this tool surface required opening many small files for one behavior family.
Files consolidated: `tests/Sussudio.Tests/McpToolSurface.WindowPreview.Preview.Tests.cs`; `tests/Sussudio.Tests/McpToolSurface.WindowPreview.Screenshot.Tests.cs`; `tests/Sussudio.Tests/McpToolSurface.WindowPreview.WindowActions.Tests.cs`; `tests/Sussudio.Tests/McpToolSurface.WindowPreview.Wait.Tests.cs`; `tests/Sussudio.Tests/McpToolSurface.WindowPreview.Flashback.Tests.cs`; `tests/Sussudio.Tests/McpToolSurface.WindowPreview.Probes.Tests.cs`; `tests/Sussudio.Tests/McpToolSurface.WindowPreview.PreviewFrameCapture.Tests.cs`
Files added: `tests/Sussudio.Tests/McpToolSurface.WindowPreview.Tests.cs`
Net production .cs delta: 0
Net test .cs delta: -6
Partial clusters reduced: `Program` MCP window-preview partial-family file count -6
Build/tests/runtime checks: pending in current checkpoint
CLI/MCP/pipe checks, if applicable: MCP window/preview tests remain in the xUnit suite and exercise the same named-pipe tool calls
Behavior preserved: the same internal `Program` test method names, helper methods, reflection calls, command assertions, and response-formatting checks remain in one window-preview owner file
Notes for future agents: keep MCP wait/window/preview/screenshot/probe tests in `McpToolSurface.WindowPreview.Tests.cs` unless a subgroup grows a distinct fixture, process lifecycle, or helper seam

Date: 2026-05-25
Area: MCP performance timeline test locality
Problem: MCP performance timeline checks split one timeline source-loading seam across source-ownership, rendering, projection, and Flashback command-counter partial files. Changing timeline fields or rendering required opening the root orchestrator plus several small helper/assertion fragments.
Files consolidated: `tests/Sussudio.Tests/McpToolSurface.Performance.TimelineContract.SourceOwnership.Tests.cs`; `tests/Sussudio.Tests/McpToolSurface.Performance.TimelineContract.Rendering.Tests.cs`; `tests/Sussudio.Tests/McpToolSurface.Performance.TimelineContract.Projection.Tests.cs`; `tests/Sussudio.Tests/McpToolSurface.Performance.TimelineFlashback.Tests.cs`
Files added: none
Net production .cs delta: 0
Net test .cs delta: -4
Partial clusters reduced: `Program` MCP performance timeline partial-family file count -4
Build/tests/runtime checks: pending in current checkpoint
CLI/MCP/pipe checks, if applicable: MCP performance timeline tests remain in the xUnit suite and exercise the same timeline tool pipe calls
Behavior preserved: the same internal `Program` test method names, timeline source loader, source-ownership assertions, rendering/projection contracts, and Flashback command-counter formatting check remain in `McpToolSurface.Performance.TimelineContract.Tests.cs`
Notes for future agents: keep MCP performance timeline source-loading, projection, rendering, and Flashback timeline formatting checks in `McpToolSurface.Performance.TimelineContract.Tests.cs` unless a subgroup grows a distinct fixture or helper seam

Date: 2026-05-25
Area: MCP diagnostic-session runner test locality
Problem: Diagnostic-session runner behavior tests split the reflective runner setup/helpers from initial snapshot, health policy, pipe retry, concurrency, artifacts, and Flashback playback cases. All fragments exercised the same `DiagnosticSessionRunner.RunAsync` seam through synthetic command delegates, so reviewing runner behavior required opening many small partial files.
Files consolidated: `tests/Sussudio.Tests/McpToolSurface.DiagnosticSession.Runner.Helpers.cs`; `tests/Sussudio.Tests/McpToolSurface.DiagnosticSession.Runner.InitialSnapshot.Tests.cs`; `tests/Sussudio.Tests/McpToolSurface.DiagnosticSession.Runner.HealthPolicy.Tests.cs`; `tests/Sussudio.Tests/McpToolSurface.DiagnosticSession.Runner.PipeRetry.Tests.cs`; `tests/Sussudio.Tests/McpToolSurface.DiagnosticSession.Runner.Concurrency.Tests.cs`; `tests/Sussudio.Tests/McpToolSurface.DiagnosticSession.Runner.Artifacts.Tests.cs`; `tests/Sussudio.Tests/McpToolSurface.DiagnosticSession.Runner.FlashbackPlayback.Tests.cs`
Files added: `tests/Sussudio.Tests/McpToolSurface.DiagnosticSession.Runner.Tests.cs`
Net production .cs delta: 0
Net test .cs delta: -6
Partial clusters reduced: `Program` diagnostic-session runner partial-family file count -6
Build/tests/runtime checks: pending in current checkpoint
CLI/MCP/pipe checks, if applicable: diagnostic-session runner tests remain in the xUnit suite and exercise the same reflective runner plus synthetic command delegates
Behavior preserved: the same internal `Program` test method names, helper methods, reflective runner setup, JSON parsing, and scenario assertions remain in one runner owner file
Notes for future agents: keep diagnostic-session runner helper and synthetic-command behavior tests in `McpToolSurface.DiagnosticSession.Runner.Tests.cs` unless a subgroup grows a distinct fixture or external-process seam

Date: 2026-05-25
Area: Automation tool contract test locality
Problem: Automation tool contract coverage split shared reflection helpers away from command-kind, catalog metadata, manifest/path-policy, and reliability-gates checks. These fragments all use the same automation contract/tool reflection helper surface and are small enough to review as one contract owner while leaving larger protocol and snapshot-formatter seams separate.
Files consolidated: `tests/Sussudio.Tests/AutomationToolContracts.CommandKinds.Tests.cs`; `tests/Sussudio.Tests/AutomationToolContracts.Catalog.Tests.cs`; `tests/Sussudio.Tests/AutomationToolContracts.Manifest.Tests.cs`; `tests/Sussudio.Tests/AutomationToolContracts.Reliability.Tests.cs`
Files added: none
Net production .cs delta: 0
Net test .cs delta: -4
Partial clusters reduced: `Program` automation tool contract partial-family file count -4
Build/tests/runtime checks: pending in current checkpoint
CLI/MCP/pipe checks, if applicable: automation tool contract checks remain in the xUnit suite through existing wrappers
Behavior preserved: the same internal `Program` test method names, `ExpectedAutomationCommands()` adapter, reflection helpers, catalog/manifest/path-policy assertions, and reliability-gates script checks remain in `AutomationToolContracts.Tests.cs`
Notes for future agents: keep shared automation command catalog/manifest/path-policy/reliability contract checks in `AutomationToolContracts.Tests.cs`; use separate files only for distinct protocol, snapshot formatter, or tool-probe seams

Date: 2026-05-25
Area: LibAv recording sink packet-drain locality
Problem: `LibAvRecordingSink.PacketDrain.cs` split the bounded video/GPU/CUDA and audio/microphone drain methods away from the encoding loop that orders and calls them. Reviewing queue drain fairness, frame-encoded events, and packet cleanup required opening a tiny partial plus the sink root loop.
Files consolidated: `Sussudio/Services/Recording/LibAvRecordingSink.PacketDrain.cs`
Files added: none
Net production .cs delta: -1
Partial clusters reduced: `LibAvRecordingSink` -1 file
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; `dotnet exec --% tests\Sussudio.Tests\bin\Debug\net8.0\Sussudio.Tests.dll Sussudio/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/Sussudio.dll`; `git diff --check`
CLI/MCP/pipe checks, if applicable: not applicable; no automation command names/IDs changed
Behavior preserved: encoding-loop drain order, bounded video/GPU/CUDA batch limits, unbounded audio/microphone drains, frame-encoded event dispatch, GPU texture release, CUDA frame free, pooled video packet return, and pooled audio buffer return remain unchanged
Notes for future agents: keep `EncodingLoop` and its `Drain*Packets` helpers together in `LibAvRecordingSink.cs` unless a distinct queue-drain collaborator with its own state/test seam is introduced.

Date: 2026-05-25
Area: CaptureService health snapshot sampler locality
Problem: Capture health snapshot sampling was split across `CaptureService.HealthSnapshots.cs`, `CaptureService.HealthSnapshotFlashbackBackend.cs`, `CaptureService.HealthSnapshotFlashbackPlayback.cs`, and `CaptureService.HealthSnapshotRecording.cs`, even though the retired files only contained private field builders and records called by `GetHealthSnapshot()`. Reviewing health projection required opening four CaptureService partials plus the assembler.
Files consolidated: `Sussudio/Services/Capture/CaptureService.HealthSnapshotFlashbackBackend.cs`; `Sussudio/Services/Capture/CaptureService.HealthSnapshotFlashbackPlayback.cs`; `Sussudio/Services/Capture/CaptureService.HealthSnapshotRecording.cs`
Files added: none
Net production .cs delta: -3
Partial clusters reduced: `CaptureService` -3 files
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; `dotnet exec --% tests\Sussudio.Tests\bin\Debug\net8.0\Sussudio.Tests.dll Sussudio/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/Sussudio.dll`; `git diff --check`
CLI/MCP/pipe checks, if applicable: not applicable; no automation command names/IDs changed
Behavior preserved: health snapshot public DTO mapping, capture-cadence and MJPEG projection, source telemetry health projection, Flashback buffer/backend staleness and queue projection, Flashback playback cadence/decode/audio-master/command projection, and recording LibAv/Flashback queue and failure precedence remain unchanged
Notes for future agents: keep private health snapshot field builders in `CaptureService.HealthSnapshots.cs`; keep `CaptureService.HealthSnapshotAssembler.cs` as the pure final DTO construction boundary.

Date: 2026-05-25
Area: Flashback encoder producer ingress locality
Problem: Flashback encoder producer input validation lived in `FlashbackEncoderSink.Inputs.cs`, while the immediately-called video/GPU/audio/microphone queue admission, write, and rejection helpers lived in `FlashbackEncoderSink.VideoQueueSubmission.cs`. Reviewing the hot producer path required opening two partials for one enqueue transaction family.
Files consolidated: `Sussudio/Services/Flashback/FlashbackEncoderSink.VideoQueueSubmission.cs`
Files added: none
Net production .cs delta: -1
Partial clusters reduced: `FlashbackEncoderSink` -1 file
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; `dotnet exec --% tests\Sussudio.Tests\bin\Debug\net8.0\Sussudio.Tests.dll Sussudio/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/Sussudio.dll`; `git diff --check`
CLI/MCP/pipe checks, if applicable: not applicable; no automation command names/IDs changed
Behavior preserved: raw/lease/GPU video validation, texture AddRef ownership, hot audio/microphone write adapters, accepted/rejected/overloaded queue transactions, force-rotate audio queue guard policy, depth/max-depth accounting, backlog eviction accounting, rejection counters, and throttled queue diagnostics remain unchanged
Notes for future agents: keep Flashback encoder producer input validation and queue admission/write/rejection helpers together in `FlashbackEncoderSink.Inputs.cs`; keep queue DTOs, packet buffer ownership, and queued-buffer cleanup in `FlashbackEncoderSink.Queues.cs`.

Date: 2026-05-25
Area: Flashback buffer purge lifecycle locality
Problem: `FlashbackBufferManager.Purge.cs` held explicit purge/delete-all cleanup while `FlashbackBufferManager.Lifecycle.cs` owned `Dispose()` and called `PurgeAllSegmentsCore()`. Reviewing dispose cleanup and recovery-preserve purge behavior required opening two adjacent partials for one lifecycle cleanup path.
Files consolidated: `Sussudio/Services/Flashback/FlashbackBufferManager.Purge.cs`
Files added: none
Net production .cs delta: -1
Partial clusters reduced: `FlashbackBufferManager` -1 file
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; `dotnet exec --% tests\Sussudio.Tests\bin\Debug\net8.0\Sussudio.Tests.dll Sussudio/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/Sussudio.dll`; `git diff --check`
CLI/MCP/pipe checks, if applicable: not applicable; no automation command names/IDs changed
Behavior preserved: completed-segment purge, full purge, dispose purge, recovery-preserve purge skip, session-directory path guard, active-segment delete/reset, freed-byte accounting, and eviction pause reset remain unchanged
Notes for future agents: keep explicit purge/delete-all cleanup with `FlashbackBufferManager.Lifecycle.cs`; keep retention eviction selection and pause/resume recording range policy in `FlashbackBufferManager.Retention.cs`.

Date: 2026-05-25
Area: Diagnostic-session Flashback metrics locality
Problem: `DiagnosticSessionFlashbackMetrics` had no concrete owner file; recording/export, playback-session observation, and playback-result projection lived in three partials totaling a reviewable metrics owner. Reviewing one snapshot-only projection family required opening three tiny files and kept an unnecessary partial cluster alive.
Files consolidated: `tools/Common/DiagnosticSessionFlashbackMetrics.RecordingExport.cs`; `tools/Common/DiagnosticSessionFlashbackMetrics.PlaybackSession.cs`; `tools/Common/DiagnosticSessionFlashbackMetrics.PlaybackResult.cs`
Files added: `tools/Common/DiagnosticSessionFlashbackMetrics.cs`
Net production .cs delta: -2
Partial clusters reduced: `DiagnosticSessionFlashbackMetrics` partial family removed
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; `dotnet exec --% tests\Sussudio.Tests\bin\Debug\net8.0\Sussudio.Tests.dll Sussudio/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/Sussudio.dll`; `git diff --check`
CLI/MCP/pipe checks, if applicable: tool contract coverage remains in the xUnit suite; no automation command names/IDs changed
Behavior preserved: recording/export metrics, export relevance gating, force-rotate fallback counters outside the export-observed relevance gate, playback active/relevant snapshot gating, session frame-count projection, 1% low capture, frame/decode/audio-master maxima, playback counter deltas, final result construction, and grouped command/cadence/decode/audio-master/stage reads remain unchanged
Notes for future agents: keep Flashback diagnostic-session metric projection in `tools/Common/DiagnosticSessionFlashbackMetrics.cs`; split only if a new independent metric subsystem grows its own state or external seam.

Date: 2026-05-25
Area: Device discovery locality
Problem: `DeviceService.FormatProbe.cs` split format-cache DTOs, cache load/save/delete, background format probing, inline Media Foundation probing, and format normalization away from the device discovery owner that calls those helpers during enumeration and startup refresh. Reviewing discovery behavior required opening two partial files for one cohesive capture/audio device option pipeline.
Files consolidated: `Sussudio/Services/Capture/DeviceService.FormatProbe.cs`
Files added: none
Net production .cs delta: -1
Partial clusters reduced: `DeviceService` partial family removed
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; `dotnet exec --% tests\Sussudio.Tests\bin\Debug\net8.0\Sussudio.Tests.dll Sussudio/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/Sussudio.dll`; `git diff --check`
CLI/MCP/pipe checks, if applicable: not applicable; no automation command names/IDs changed
Behavior preserved: capture/audio enumeration orchestration, format-cache warm/load/delete/save behavior, background probe event delivery, inline format probing, HDR detection, pixel-format normalization, frame-rate normalization, discovery summary text, priority scoring, audio association, and native XU interface resolution remain unchanged
Notes for future agents: keep device enumeration, discovery cache, and format probing together in `DeviceService.cs`; keep lower-level MF enumeration/source opening in `DeviceDiscovery/MfDeviceEnumerator.cs`.

Date: 2026-05-25
Area: PresentMon result formatting locality
Problem: `PresentMonProbe.Format.cs` split the public text-rendering surface away from the PresentMon tool owner that creates options, runs PresentMon, shapes result messages, and is called by ssctl/MCP output paths. Reviewing operator-facing PresentMon result behavior required opening a tiny formatting partial plus the root runner.
Files consolidated: `tools/Common/PresentMon/PresentMonProbe.Format.cs`
Files added: none
Net production .cs delta: -1
Partial clusters reduced: `PresentMonProbe` -1 file
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; `dotnet exec --% tests\Sussudio.Tests\bin\Debug\net8.0\Sussudio.Tests.dll Sussudio/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/Sussudio.dll`; `git diff --check`
CLI/MCP/pipe checks, if applicable: ssctl/MCP still call `PresentMonProbe.Format(result)`; no automation command names/IDs changed
Behavior preserved: PresentMon run orchestration, CSV parse handoff, result message shaping, successful/failed format output, stderr inclusion, summary context, metric rows, app correlation text, count fields, and swap-chain list rendering remain unchanged
Notes for future agents: superseded by the later PresentMon public model locality consolidation; keep PresentMon run orchestration, text formatting, and public DTOs in `PresentMonProbe.cs`; keep CSV parsing/aggregation in `PresentMonProbe.Csv.cs`.

Date: 2026-05-25
Area: PresentMon public model locality
Problem: `PresentMonProbe.Models.cs` held only public DTOs for the single PresentMon probe runner and CSV parser. Changing the probe option/result surface required opening a small model bucket plus the root runner, even though the DTOs are not shared independently of the probe.
Files consolidated: `tools/Common/PresentMon/PresentMonProbe.Models.cs`
Files added: none
Net production .cs delta: -1
Partial clusters reduced: n/a; PresentMon support file count -1
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; `dotnet exec --% tests\Sussudio.Tests\bin\Debug\net8.0\Sussudio.Tests.dll Sussudio/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/Sussudio.dll`
CLI/MCP/pipe checks, if applicable: PresentMon parser/source-ownership and MCP PresentMon tool tests cover the public DTOs, CSV parser, and formatted output; no public MCP tool names or automation command IDs changed
Behavior preserved: PresentMon option defaults, result shape, capture summary DTOs, swap-chain/app-correlation/metric DTOs, run orchestration, CSV parsing, and result formatting remain unchanged
Notes for future agents: keep PresentMon public DTOs with `PresentMonProbe.cs`; keep `PresentMonProbe.Csv.cs` as the parser/aggregation owner while it remains a distinct implementation surface.

Date: 2026-05-25
Area: NativeXuAudioProbe default experiment reporting locality
Problem: `Program.DefaultExperiment.Reporting.cs` held the AT read/decode/diff/snapshot reporting helpers and result records used only by `Program.DefaultExperiment.cs`. Reviewing the default Native XU experiment required opening two partial files for one exploratory experiment workflow.
Files consolidated: `tools/NativeXuAudioProbe/Program.DefaultExperiment.Reporting.cs`
Files added: none
Net production .cs delta: -1
Partial clusters reduced: `NativeXuProbeDefaultExperiment` partial family removed
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; `dotnet exec --% tests\Sussudio.Tests\bin\Debug\net8.0\Sussudio.Tests.dll Sussudio/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/Sussudio.dll`; `git diff --check`
CLI/MCP/pipe checks, if applicable: NativeXuAudioProbe build remains covered by solution build; no automation command names/IDs changed
Behavior preserved: baseline/final telemetry snapshot printing, AT getter reads, typed payload decoding, raw-payload formatting, before/after diff output, changed-result collection, interesting-change summary, analog gain sequence, restore behavior, and default experiment payload construction remain unchanged
Notes for future agents: keep default Native XU experiment sequencing, payload construction, and reporting/readback helpers in `Program.DefaultExperiment.cs`; split only if the reporting grows into a reusable probe output formatter with its own callers.

Date: 2026-05-25
Area: RecordingVerifier validation locality
Problem: `RecordingVerifier.Validation.cs` held only private validation policy called by the root `VerifyAsync` flow, so understanding verification outcomes required opening the root orchestration/result owner plus a small policy partial while the separate ffprobe process/probe owner was the only remaining distinct boundary.
Files consolidated: `Sussudio/Services/Recording/Verification/RecordingVerifier.Validation.cs`
Files added: none
Net production .cs delta: -1
Partial clusters reduced: `RecordingVerifier` production partial-family file count 3 -> 2
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; `dotnet exec --% tests\Sussudio.Tests\bin\Debug\net8.0\Sussudio.Tests.dll Sussudio/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/Sussudio.dll`; `git diff --check`
CLI/MCP/pipe checks, if applicable: not applicable; no automation command names/IDs changed
Behavior preserved: container, codec, dimensions, frame-rate, cadence, Flashback export verification format, and HDR validation logic moved unchanged beside the strict verifier orchestration and result shaping; ffprobe path/process/scalar/HDR/cadence probing remains in `RecordingVerifier.Ffprobe.cs`.
Notes for future agents: keep pure recording verification policy in the root verifier unless it grows a separate collaborator seam; keep external ffprobe process and JSON probe mechanics in `RecordingVerifier.Ffprobe.cs`.

Date: 2026-05-25
Area: Automation command dispatcher assert-snapshot locality
Problem: `AutomationCommandDispatcher.Assertions.cs` held private AssertSnapshot command parsing/comparison helpers while `AutomationCommandDispatcher.CustomCommands.cs` owned the custom command switch and the AssertSnapshot route. Reviewing one custom command required opening a small support partial plus the router even though no separate collaborator or public seam existed.
Files consolidated: `Sussudio/Services/Automation/AutomationCommandDispatcher.Assertions.cs`
Files added: none
Net production .cs delta: -1
Partial clusters reduced: `AutomationCommandDispatcher` production partial-family file count 3 -> 2
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; `dotnet exec --% tests\Sussudio.Tests\bin\Debug\net8.0\Sussudio.Tests.dll Sussudio/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/Sussudio.dll`; `git diff --check`
CLI/MCP/pipe checks, if applicable: automation command names/IDs and manifest metadata unchanged; coverage remains in automation command dispatcher tests
Behavior preserved: AssertSnapshot response shaping, payload parsing, field lookup cache, numeric/boolean/string comparison behavior, error code, status, and refreshed snapshot inclusion moved unchanged beside the custom command router.
Notes for future agents: keep custom command bodies and support helpers in `AutomationCommandDispatcher.CustomCommands.cs` unless a command grows an independent service/collaborator seam; keep manifest/auth/preflight/payload helpers and one-field handler tables in `AutomationCommandDispatcher.cs`.

Date: 2026-05-25
Area: WASAPI capture diagnostics locality
Problem: `WasapiAudioCapture.Diagnostics.cs` held the read-only metric properties, audio-level event projection, and callback/glitch accounting used directly by initialization resets and the capture loop. Reviewing capture lifecycle metrics required opening a small diagnostics partial plus the root lifecycle owner that owns the fields and reset policy.
Files consolidated: `Sussudio/Services/Audio/WasapiAudioCapture.Diagnostics.cs`
Files added: none
Net production .cs delta: -1
Partial clusters reduced: `WasapiAudioCapture` production partial-family file count 4 -> 3
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; `dotnet exec --% tests\Sussudio.Tests\bin\Debug\net8.0\Sussudio.Tests.dll Sussudio/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/Sussudio.dll`; `git diff --check`
CLI/MCP/pipe checks, if applicable: not applicable; no automation command names/IDs changed
Behavior preserved: audio frame counters, callback interval snapshot, severe-gap/discontinuity/timestamp/glitch counters, audio-level event throttling, peak/clipping calculation, callback tracking, packet flag tracking, and initialization-time metric reset consumers remain unchanged.
Notes for future agents: keep WASAPI capture lifecycle state and diagnostics counters in `WasapiAudioCapture.cs`; keep capture-thread fan-out in `WasapiAudioCapture.CaptureLoop.cs` and sample conversion/resampling in `WasapiAudioCapture.Conversion.cs`.

Date: 2026-05-25
Area: CaptureService Flashback controls locality
Problem: `CaptureService.FlashbackState.cs` owned public Flashback state, segment access, enable/disable, and restart while `CaptureService.FlashbackSettings.cs` owned adjacent settings, format, and encoder-cycle controls. Reviewing service-level Flashback controls required opening two adjacent partials for one transition surface.
Files consolidated: `Sussudio/Services/Capture/CaptureService.FlashbackState.cs`; `Sussudio/Services/Capture/CaptureService.FlashbackSettings.cs`
Files added: `Sussudio/Services/Capture/CaptureService.FlashbackControls.cs`
Net production .cs delta: -1
Partial clusters reduced: `CaptureService` -1 file
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; `dotnet exec --% tests\Sussudio.Tests\bin\Debug\net8.0\Sussudio.Tests.dll Sussudio/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/Sussudio.dll`; `git diff --check`
CLI/MCP/pipe checks, if applicable: not applicable; no automation command names/IDs changed
Behavior preserved: Flashback state/segment projections, enable/disable/restart transition gating, settings updates, playback GPU decode propagation, recording format updates, encoder-setting cycles, buffer-cycle lock ordering/rebuild fallbacks, rollback logging, and recording-active defer policy remain unchanged
Notes for future agents: keep service-level Flashback state, enable/restart, settings, format, encoder-cycle, and buffer-cycle coordination in `CaptureService.FlashbackControls.cs`; keep preview-backend startup/disposal in `CaptureService.FlashbackPreviewBackend.cs` and recording backend/session-context policy in `CaptureService.FlashbackRecording.cs`.

Date: 2026-05-25
Area: LibAvEncoder hardware-frame locality
Problem: `LibAvEncoder.HardwareFrames.cs` owned D3D11/CUDA hardware frame setup and pool adoption while `LibAvEncoder.HardwareSubmission.cs` owned the matching GPU/CUDA submit paths that consume the same state, frame context, and HDR side-data helper. Reviewing hardware encoding required opening two adjacent partials for one hardware-frame surface.
Files consolidated: `Sussudio/Services/Recording/LibAvEncoder.HardwareSubmission.cs`
Files added: none
Net production .cs delta: -1
Partial clusters reduced: `LibAvEncoder` -1 file
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; `dotnet exec --% tests\Sussudio.Tests\bin\Debug\net8.0\Sussudio.Tests.dll Sussudio/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/Sussudio.dll`
CLI/MCP/pipe checks, if applicable: not applicable; no automation command names/IDs changed
Behavior preserved: D3D11/CUDA hardware frame initialization, ArraySize=1 pool setup, GPU texture copy/reference setup, device-removed checks, PTS/keyframe assignment, first-frame HDR side-data attachment/removal, EAGAIN packet drains, packet draining, drop accounting, and hardware-frame unref cleanup remain unchanged
Notes for future agents: keep hardware frame setup and hardware submit paths together in `LibAvEncoder.HardwareFrames.cs`; keep CPU packed-frame submission and shared HDR side-data helper implementations in `LibAvEncoder.VideoSubmission.cs`.

Date: 2026-05-25
Area: ssctl general formatter locality
Problem: `Formatters.Common.cs` owned generic ssctl result/JSON, diagnostic, and memory projections while `Formatters.Options.cs` held a small capture-options/device-list projection that used the same `TryGetData` and `AutomationSnapshotFormatter` helper flow. Reviewing non-snapshot/non-timeline CLI projection behavior required opening two small adjacent partials.
Files consolidated: `tools/ssctl/Formatters.Options.cs`
Files added: none
Net production .cs delta: -1
Partial clusters reduced: `Formatters` -1 file
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; `dotnet exec --% tests\Sussudio.Tests\bin\Debug\net8.0\Sussudio.Tests.dll Sussudio/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/Sussudio.dll`
CLI/MCP/pipe checks, if applicable: ssctl formatter coverage and command-handler routing tests cover the public formatter methods; no automation command names/IDs changed
Behavior preserved: generic command result output, pretty JSON, diagnostic-event output, memory/GC output, capture options summary, device-list output, selected-option markers, disabled suffixes, and capture option list ordering remain unchanged
Notes for future agents: keep general ssctl projections in `Formatters.Common.cs`; keep app snapshot rendering in `Formatters.Snapshot.cs` and performance timeline table/trend rendering in `Formatters.Timeline.cs`.

Date: 2026-05-25
Area: NativeXuAudioProbe command workflow locality
Problem: `Program.cs` owned NativeXuAudioProbe routing and probe-local runtime shims, while two small adjacent files held the direct AT read/write/input command bodies and captured audio-switch replay workflow. Reviewing the top-level probe command surface required opening three small files even though these command bodies are thin CLI workflows and not shared collaborators.
Files consolidated: `tools/NativeXuAudioProbe/Program.AtCommands.cs`; `tools/NativeXuAudioProbe/Program.I2cSwitch.cs`
Files added: none
Net production .cs delta: -2
Partial clusters reduced: n/a; NativeXuAudioProbe support file count -2
Build/tests/runtime checks: `dotnet build tools\NativeXuAudioProbe\NativeXuAudioProbe.csproj -c Debug --no-restore`; `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; `dotnet exec --% tests\Sussudio.Tests\bin\Debug\net8.0\Sussudio.Tests.dll Sussudio/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/Sussudio.dll`
CLI/MCP/pipe checks, if applicable: affected NativeXuAudioProbe build and source-ownership tests cover the moved direct AT and audio-switch workflows; no public command names changed
Behavior preserved: `at-read`, `at-write`, `at-set-input`, and `i2c-switch` route names, argument handling, output strings, restore behavior, I2C-over-AT helper calls, and selected-device behavior remain unchanged
Notes for future agents: keep small top-level NativeXuAudioProbe command workflows with `Program.cs`; keep `Program.I2cTransport.cs` separate while multiple I2C command families share it.

Date: 2026-05-25
Area: diagnostic-session health policy locality
Problem: Diagnostic-session health severity/observation logic lived in `DiagnosticSessionHealthPolicy.cs`, while the source/preview/Flashback classifiers and sparse-run tolerance helpers lived in adjacent `DiagnosticSessionHealthTolerances.cs`. Understanding why a diagnostic health warning was emitted or tolerated required opening two small policy files that shared the same severity model.
Files consolidated: `tools/Common/DiagnosticSessionHealthTolerances.cs`
Files added: none
Net production .cs delta: -1
Partial clusters reduced: n/a; diagnostic-session support file count -1
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; `dotnet exec --% tests\Sussudio.Tests\bin\Debug\net8.0\Sussudio.Tests.dll Sussudio/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/Sussudio.dll`
CLI/MCP/pipe checks, if applicable: diagnostic-session ownership and runner reflection tests cover the moved sparse-cadence classifier; no CLI command names or automation payloads changed
Behavior preserved: diagnostic health severity mapping, Flashback warmup filtering, source/signal/preview/Flashback health classifiers, sparse source cadence tolerance, sparse preview scheduler tolerance, and Flashback warning-tolerance predicates remain unchanged
Notes for future agents: keep diagnostic-session health observation and health-warning tolerance policy together in `DiagnosticSessionHealthPolicy.cs`; do not recreate a separate tolerance bucket unless it becomes an independently tested collaborator with its own state or inputs.

Date: 2026-05-25
Area: MCP performance timeline report locality
Problem: `PerformanceTimelineTools.Rendering.cs` owned the MCP tool entry point, table rendering, trend rendering, pressure summaries, and formatting helpers while `PerformanceTimelineTools.Rows.cs` held the private JSON-to-row projection and row DTO consumed only by that renderer. Understanding or changing timeline output required opening both files even though they form one report surface.
Files consolidated: `tools/McpServer/Tools/PerformanceTimelineTools.Rows.cs`
Files added: none
Net production .cs delta: -1
Partial clusters reduced: `PerformanceTimelineTools` partial family removed
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; `dotnet exec --% tests\Sussudio.Tests\bin\Debug\net8.0\Sussudio.Tests.dll Sussudio/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/Sussudio.dll`
CLI/MCP/pipe checks, if applicable: MCP performance timeline contract tests cover row projection, row model, rendering, trend summaries, and command routing; no public MCP tool names or automation command IDs changed
Behavior preserved: `get_performance_timeline` payload, automation command kind, JSON field projection, table columns, trend summaries, target 1% low summaries, pressure summaries, and formatting helpers remain unchanged
Notes for future agents: keep the MCP performance timeline as one cohesive report owner in `PerformanceTimelineTools.Rendering.cs`; split only if row projection becomes a shared parser or a report subsection grows independent policy.

Date: 2026-05-25
Area: EGAVDS audio probe locality
Problem: `tools/EgavdsAudioProbe/Program.cs` owned the only EGAVDS probe command flow, device lookup, audio input/gain actions, and result text while `Program.NativeInterop.cs` held private SWIG, EGAVDeviceSupport, and SetupAPI declarations consumed only by that same probe. Understanding or changing this single exploratory CLI required opening both files.
Files consolidated: `tools/EgavdsAudioProbe/Program.NativeInterop.cs`
Files added: none
Net production .cs delta: -1
Partial clusters reduced: `EgavdsProbe` partial family removed
Build/tests/runtime checks: `dotnet build tools\EgavdsAudioProbe\EgavdsAudioProbe.csproj -c Debug --no-restore`; `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; `dotnet exec --% tests\Sussudio.Tests\bin\Debug\net8.0\Sussudio.Tests.dll Sussudio/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/Sussudio.dll`
CLI/MCP/pipe checks, if applicable: EGAVDS source-ownership tests and solution build cover the consolidated probe; no app automation command names or public tool commands changed
Behavior preserved: EGAVDS CLI arguments, SetupAPI device path selection, SWIG callback registration, EGAVDeviceSupport initialization/open/close calls, audio input switching, line-in gain query/set, HDR and connection info reads, and result text remain unchanged
Notes for future agents: keep EGAVDS probe-private native declarations with `tools/EgavdsAudioProbe/Program.cs` unless they become shared by another tool or need independent generated bindings.

Date: 2026-05-25
Area: KS audio node probe locality
Problem: `tools/KsAudioNodeProbe/Program.cs` owned the only KS audio node probe command entry, interface selection, open failure handling, and workflow dispatch while `Program.NativeInterop.cs` held private SetupAPI, file-handle, KS property transfer, topology, and Win32 helper declarations consumed only by that same probe. Understanding or changing this single exploratory CLI required opening both files before reaching the real scan workflow owner.
Files consolidated: `tools/KsAudioNodeProbe/Program.NativeInterop.cs`
Files added: none
Net production .cs delta: -1
Partial clusters reduced: n/a; KS audio node probe support file count -1
Build/tests/runtime checks: `dotnet build tools\KsAudioNodeProbe\KsAudioNodeProbe.csproj -c Debug --no-restore`; `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; `dotnet exec --% tests\Sussudio.Tests\bin\Debug\net8.0\Sussudio.Tests.dll Sussudio/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/Sussudio.dll`; `git diff --check`; `git diff --cached --check`
CLI/MCP/pipe checks, if applicable: affected source-ownership tests updated; no app automation command names or public tool commands changed
Behavior preserved: KS audio node probe CLI arguments, VID/PID parsing, MI_02 interface selection, handle open failure text, set-and-hold routing, full-probe routing, SetupAPI enumeration, KS property GET/SET transfer, topology enumeration, Win32 error formatting, and scan workflow behavior remain unchanged
Notes for future agents: keep KS audio node probe-private native declarations with `tools/KsAudioNodeProbe/Program.cs`; keep scan and mutation probe workflows in `Program.ScanWorkflows.cs` while they remain the real behavior owner.

Date: 2026-05-25
Area: NativeXuAudioProbe service workflow locality
Problem: `Program.cs` owned NativeXuAudioProbe routing and already-hosted top-level command workflows while `Program.ServiceProbe.cs` held the adjacent service-control and service-smoke command bodies. Reviewing the probe's service command surface required opening an extra small file even though these workflows are thin CLI actions called only by the root router.
Files consolidated: `tools/NativeXuAudioProbe/Program.ServiceProbe.cs`
Files added: none
Net production .cs delta: -1
Partial clusters reduced: n/a; NativeXuAudioProbe support file count -1
Build/tests/runtime checks: `dotnet build tools\NativeXuAudioProbe\NativeXuAudioProbe.csproj -c Debug --no-restore`; `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; `dotnet exec --% tests\Sussudio.Tests\bin\Debug\net8.0\Sussudio.Tests.dll Sussudio/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/Sussudio.dll`; `git diff --check`; `git diff --cached --check`
CLI/MCP/pipe checks, if applicable: affected NativeXuAudioProbe build and source-ownership tests cover the moved service workflows; no public command names changed
Behavior preserved: `service`, `--service-smoke`, `--device`, `--mode`, `--gain`, and `--dump-payload` handling, NativeXuAudioControlService state reads, service payload printing, set-mode/set-gain calls, service smoke output, and status/error text remain unchanged
Notes for future agents: keep small top-level NativeXuAudioProbe service-control workflows with `Program.cs`; keep `Program.I2cTransport.cs` separate while multiple I2C command families share it.

Date: 2026-05-25
Area: diagnostic-session scenario planning locality
Problem: `DiagnosticSessionScenarioCatalog.cs` owned every scenario entry and constructed every `DiagnosticSessionScenarioPlan`, while `DiagnosticSessionScenarioPlan.cs` held the adjacent flag DTO, creation factory, catalog lookup handoff, and grouped scenario predicates. Reviewing scenario requirements, plan metadata, and grouped warning policy required opening two small files that called back into each other.
Files consolidated: `tools/Common/DiagnosticSessionScenarioPlan.cs`
Files added: none
Net production .cs delta: -1
Partial clusters reduced: n/a; diagnostic-session support file count -1
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; `dotnet exec --% tests\Sussudio.Tests\bin\Debug\net8.0\Sussudio.Tests.dll Sussudio/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/Sussudio.dll`; `git diff --check`; `git diff --cached --check`
CLI/MCP/pipe checks, if applicable: diagnostic-session scenario ownership tests cover catalog entries, plan creation, grouped policy flags, and runner handoff; no CLI/MCP command names or automation command IDs changed
Behavior preserved: diagnostic scenario names, HelpList/Description text, scenario ordering, requirement queries, export verification filenames, plan flag construction, grouped Flashback warning/validation predicates, preview-cycle predicates, and runner consumption of named plan properties remain unchanged
Notes for future agents: keep scenario name metadata and `DiagnosticSessionScenarioPlan` flag policy together in `DiagnosticSessionScenarioCatalog.cs`; do not add scenario string comparisons in the runner.

Date: 2026-05-25
Area: diagnostic-session command transport locality
Problem: `DiagnosticSessionCommandChannel.cs` owned serialized diagnostic-session command sending and connect-retry invocation, while `DiagnosticSessionPipeRetryPolicy.cs` held the adjacent retry classifier and local failure-envelope helpers used by that transport surface and Flashback export diagnostics. Reviewing command transport failure behavior required opening two small files for one retry/error-envelope path.
Files consolidated: `tools/Common/DiagnosticSessionPipeRetryPolicy.cs`
Files added: none
Net production .cs delta: -1
Partial clusters reduced: n/a; diagnostic-session support file count -1
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; `dotnet exec --% tests\Sussudio.Tests\bin\Debug\net8.0\Sussudio.Tests.dll Sussudio/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/Sussudio.dll`; `git diff --check`; `git diff --cached --check`
CLI/MCP/pipe checks, if applicable: diagnostic-session infrastructure and protocol contract tests cover retry classification, local failure envelopes, and command-channel usage; no CLI/MCP command names or automation command IDs changed
Behavior preserved: access-denied remains permanent, pipe connect failed/timeout synthetic responses remain retryable, non-connect pipe exceptions and JSON exceptions still produce local failure responses, command-channel no-response fallback remains unchanged, and Flashback export diagnostics still call the named retry policy helpers
Notes for future agents: keep the named `DiagnosticSessionPipeRetryPolicy` type with `DiagnosticSessionCommandChannel.cs`; do not move retry classification into the runner.

Date: 2026-05-25
Area: preview screenshot PNG encoding locality
Problem: `PreviewScreenshotCapture.cs` owned preview-frame screenshot pixel conversion, BMP writing, 16-bit PNG frame capture, and the only call site for `PreviewPng16Encoder`, while `PreviewPng16Encoder.cs` held the subordinate PNG container/chunk/CRC helpers. Reviewing screenshot capture required opening two files even though the encoder is not shared outside the screenshot capture surface.
Files consolidated: `Sussudio/Services/Preview/PreviewPng16Encoder.cs`
Files added: none
Net production .cs delta: -1
Partial clusters reduced: n/a; preview screenshot support file count -1
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; `dotnet exec --% tests\Sussudio.Tests\bin\Debug\net8.0\Sussudio.Tests.dll Sussudio/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/Sussudio.dll`; `git diff --check`; `git diff --cached --check`
CLI/MCP/pipe checks, if applicable: screenshot capture source-ownership and PNG geometry tests cover the preserved internal `PreviewPng16Encoder` type and capture flow; no CLI/MCP command names or automation command IDs changed
Behavior preserved: preview BMP/PNG capture paths, HDR 10-bit to 16-bit PNG expansion, output-directory creation, PNG chunk writing, CRC table generation, CRC updates, and reflection-visible internal encoder helper names remain unchanged
Notes for future agents: keep `PreviewPng16Encoder` as a sibling type in `PreviewScreenshotCapture.cs` while it is only used by preview screenshot capture; split only if another capture/export path needs the PNG container writer.

Date: 2026-05-25
Area: NativeXuAudioProbe device locator locality
Problem: `tools/NativeXuAudioProbe/Program.cs` owned NativeXuAudioProbe routing, top-level command workflows, service smoke/payload commands, and probe-local runtime shims while `NativeXuProbeDeviceLocator.cs` held the adjacent supported-device lookup used only by those probe commands. Reviewing the device-selection path required opening a small sidecar file before returning to the root CLI owner.
Files consolidated: `tools/NativeXuAudioProbe/NativeXuProbeDeviceLocator.cs`
Files added: none
Net production .cs delta: -1
Partial clusters reduced: n/a; NativeXuAudioProbe support file count -1
Build/tests/runtime checks: `dotnet build tools\NativeXuAudioProbe\NativeXuAudioProbe.csproj -c Debug --no-restore`; `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; `dotnet exec --% tests\Sussudio.Tests\bin\Debug\net8.0\Sussudio.Tests.dll Sussudio/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/Sussudio.dll`; `git diff --check`; `git diff --cached --check`
CLI/MCP/pipe checks, if applicable: affected NativeXuAudioProbe build and source-ownership tests cover the moved supported-device locator; no public command names changed
Behavior preserved: supported VID/PID list, preferred interface selection, no-filter ambiguity handling, device filter matching, ambiguous/missing-device error text, and `CaptureDevice.NativeXuInterfacePath` assignment remain unchanged
Notes for future agents: keep the probe-only supported-device locator with `tools/NativeXuAudioProbe/Program.cs` while it is only consumed by top-level NativeXuAudioProbe command workflows.

Date: 2026-05-25
Area: NativeXuAudioProbe I2C legacy workflow locality
Problem: `Program.I2cCommands.cs` owned the exploratory NativeXu I2C command family and transport-probe workflows, while `Program.I2cLegacyProbe.cs` held the adjacent legacy `i2c-probe` selector scan and raw/AT-wrapped I2C frame experiments. Understanding NativeXu I2C probing required opening both files even though they share the same device lookup, KS/XU helpers, and transport framing context.
Files consolidated: `tools/NativeXuAudioProbe/Program.I2cLegacyProbe.cs`
Files added: none
Net production .cs delta: -1
Partial clusters reduced: stale `NativeXuProbeI2cCommands` partial marker removed
Build/tests/runtime checks: `dotnet build tools\NativeXuAudioProbe\NativeXuAudioProbe.csproj -c Debug --no-restore`; `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; `dotnet exec --% tests\Sussudio.Tests\bin\Debug\net8.0\Sussudio.Tests.dll Sussudio/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/Sussudio.dll`; `git diff --check`; `git diff --cached --check`
CLI/MCP/pipe checks, if applicable: affected NativeXuAudioProbe build and source-ownership tests cover the moved legacy `i2c-probe` workflow; no public command names changed
Behavior preserved: `i2c-probe` routing, selector scan range, raw I2C frame probes, alternate selector probes, AT-wrapped I2C frame probes, error/status text, and KS/XU helper calls remain unchanged
Notes for future agents: keep the legacy NativeXu `i2c-probe` workflow with `Program.I2cCommands.cs` while it is only another exploratory I2C probe path; keep `Program.I2cTransport.cs` separate while multiple I2C command families share it.

Date: 2026-05-25
Area: stale one-file partial marker cleanup
Problem: Several tool and model owners were still declared as `partial` after earlier consolidations removed their sibling files. That made generated partial-cluster reports look noisier and kept implying extension boundaries where the current architecture has a single cohesive owner.
Files consolidated: none
Files added: none
Net production .cs delta: 0
Partial clusters reduced: removed stale one-file partial markers from `PresentMonTools`, `FlashbackTools`, `VerificationTools`, `PerformanceTimelineTools`, `AutomationSnapshotFormatter`, `DiagnosticSessionResult`, `DiagnosticSessionScenarioCatalog`, `KsAudioNodeProbeScanWorkflows`, `EgavdsProbe`, and `CommandHandlers`
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; `dotnet exec --% tests\Sussudio.Tests\bin\Debug\net8.0\Sussudio.Tests.dll Sussudio/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/Sussudio.dll`; `git diff --check`; `git diff --cached --check`
CLI/MCP/pipe checks, if applicable: source-ownership tests cover the updated tool/model declarations; no public command names, tool names, automation IDs, or wire payloads changed
Behavior preserved: declaration keyword cleanup only; method bodies, route names, MCP attributes, diagnostic-session scenarios, formatter output, and command payload shaping remain unchanged
Notes for future agents: do not reintroduce `partial` on one-file owners unless a generated/XAML/platform split or a genuine two-to-three-way type split exists.

Date: 2026-05-25
Area: WASAPI endpoint watcher locality
Problem: `AudioDeviceWatcher.cs` was a 99-line Core Audio notification wrapper that only used the centralized WASAPI COM helpers and contracts from the adjacent interop family. Understanding audio endpoint enumeration, notification registration, COM callback handling, and endpoint-volume helpers required opening a small sidecar file plus `WasapiComInterop.cs`.
Files consolidated: `Sussudio/Services/Audio/AudioDeviceWatcher.cs`
Files added: none
Net production .cs delta: -1
Partial clusters reduced: n/a; audio service support file count -1
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; `dotnet exec --% tests\Sussudio.Tests\bin\Debug\net8.0\Sussudio.Tests.dll Sussudio/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/Sussudio.dll`; regenerated `docs/architecture/Sussudio-Defragmentation-Baseline.generated.md`
CLI/MCP/pipe checks, if applicable: n/a; no public automation command names, IDs, or wire payloads changed
Behavior preserved: `AudioDeviceWatcher` type name, constructor, `DevicesChanged` event, COM endpoint registration/unregistration, 500 ms debounce, notification callback handling, disposal guard, and view-model dependency graph construction remain unchanged
Notes for future agents: keep the debounced endpoint watcher with `WasapiComInterop.cs` while it is only a thin user of the centralized WASAPI/Core Audio COM declarations; split it again only if watcher behavior grows into an independently tested runtime policy.

Date: 2026-05-25
Area: Flashback decoder validation locality
Problem: `FlashbackDecoder.Validation.cs` was a 147-line validation-only partial on a seven-file decoder cluster. It held frame-size/dimension guards and stream/frame validation helpers used directly by the decoder root/open path and output path, so reviewing decoder guard behavior required an extra partial even though no independent collaborator existed.
Files consolidated: `Sussudio/Services/Flashback/FlashbackDecoder.Validation.cs`
Files added: none
Net production .cs delta: -1
Partial clusters reduced: `FlashbackDecoder` -1 file
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; `dotnet exec --% tests\Sussudio.Tests\bin\Debug\net8.0\Sussudio.Tests.dll Sussudio/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/Sussudio.dll`; regenerated `docs/architecture/Sussudio-Defragmentation-Baseline.generated.md`
CLI/MCP/pipe checks, if applicable: n/a; no public automation command names, IDs, or wire payloads changed
Behavior preserved: frame buffer sizing, video dimension bounds, D3D11/software decoded-frame validation, input stream-count bounds, stream-index checks, and Flashback decoder error text remain unchanged
Notes for future agents: keep decoder state guards, error helpers, and validation helpers with `FlashbackDecoder.cs`; keep decode-loop timing in `DecodeLoop.cs`, seeking in `Seeking.cs`, codec setup in `VideoSetup.cs`, and video/audio output conversion in their focused output owners.

Date: 2026-05-25
Area: stale one-file partial marker cleanup
Problem: Five one-file production owners still declared `partial`, implying extension seams that no longer exist. These were not generated, XAML, platform-specific, or real 2-3 way splits, so the declarations contradicted the defragmentation partial-class policy and made partial-sprawl scans noisier.
Files consolidated: none
Files added: none
Net production .cs delta: 0
Partial clusters reduced: removed stale one-file partial markers from `DiagnosticSessionFlashbackExportScenarios`, `FlashbackBackendResources`, `NamedPipeAutomationServer`, `MainViewModelControllerGraph`, `WasapiComInterop`, and `tools/AutomationClient/Program`
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; `dotnet exec --% tests\Sussudio.Tests\bin\Debug\net8.0\Sussudio.Tests.dll Sussudio/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/Sussudio.dll`; regenerated `docs/architecture/Sussudio-Defragmentation-Baseline.generated.md`
CLI/MCP/pipe checks, if applicable: affected `AutomationClient`, `McpServer`, `ssctl`, and automation pipe sources were covered by the solution build and source-shape tests; no CLI/MCP command names, automation command IDs, or wire payloads changed
Behavior preserved: declaration keyword cleanup only; type names, constructors, public members, command routes, automation pipe behavior, Flashback backend resource behavior, WASAPI helper/contract behavior, diagnostic-session scenario flow, and AutomationClient command protocol behavior remain unchanged
Notes for future agents: do not add `partial` to these owners unless a generated/XAML/platform split or a genuine multi-file ownership boundary is introduced with tests/docs.

Date: 2026-05-25
Area: MainWindow stats overlay shell adapter locality
Problem: `MainWindow.StatsOverlay.Composition.cs` was a 153-line XAML-facing adapter partial that only constructed `StatsOverlayCompositionController` contexts and forwarded stats visibility, polling, snapshot, and section chrome calls. The adjacent shell composition partial already owned shell chrome/fullscreen/status wiring that consumes those adapter methods, so reviewing shell stats behavior required an extra MainWindow partial even though controller behavior stayed elsewhere.
Files consolidated: `Sussudio/MainWindow.StatsOverlay.Composition.cs`
Files added: none
Net production .cs delta: -1
Partial clusters reduced: `MainWindow` -1 file
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; `dotnet exec --% tests\Sussudio.Tests\bin\Debug\net8.0\Sussudio.Tests.dll Sussudio/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/Sussudio.dll`; regenerated `docs/architecture/Sussudio-Defragmentation-Baseline.generated.md`
CLI/MCP/pipe checks, if applicable: n/a; no public automation command names, IDs, or wire payloads changed
Behavior preserved: stats overlay controller construction, shell-control wiring, snapshot source callbacks, dock target wiring, MJPEG/NVML sources, frame-time targets, lifecycle/polling wrappers, section header tap handling, stats section visibility routing, and frame-time overlay visibility routing remain unchanged
Notes for future agents: keep MainWindow's XAML-facing stats adapter with `MainWindow.ShellChrome.Composition.cs`; keep stats behavior in `StatsOverlayCompositionController`, `StatsOverlayController`, `StatsDockControllerGraph`, and related stats controllers.

Date: 2026-05-25
Area: diagnostic-session result builder projection locality
Problem: `DiagnosticSessionResultBuilder.DiagnosticHealth.cs` and `DiagnosticSessionResultBuilder.FlashbackPlaybackResult.cs` were private helper partials inside the same result-builder family. Diagnostic health verdict helpers are only used by the analysis pass, and Flashback playback result maps are only used by the projection-set owner, so reviewing result construction still required two extra files for subordinate behavior.
Files consolidated: `tools/Common/DiagnosticSessionResultBuilder.DiagnosticHealth.cs`; `tools/Common/DiagnosticSessionResultBuilder.FlashbackPlaybackResult.cs`
Files added: none
Net production .cs delta: -2
Partial clusters reduced: `DiagnosticSessionResultBuilder` -2 files
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore`; `dotnet exec --% tests\Sussudio.Tests\bin\Debug\net8.0\Sussudio.Tests.dll Sussudio/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/Sussudio.dll`; regenerated `docs/architecture/Sussudio-Defragmentation-Baseline.generated.md`
CLI/MCP/pipe checks, if applicable: diagnostic-session result ownership tests cover the moved health verdict and Flashback playback projection maps; no CLI/MCP command names, automation command IDs, or wire payloads changed
Behavior preserved: diagnostic health snapshot selection, verdict/tolerance warnings, sparse source and preview-scheduler warning tolerance, Flashback playback projection composition, command/cadence/1% low/decode/audio-master/stage result maps, and `summary.json` field shape remain unchanged
Notes for future agents: keep diagnostic health verdict helpers with `DiagnosticSessionResultBuilder.Analysis.cs`; keep Flashback playback result projection maps with `DiagnosticSessionResultBuilder.Projections.cs` unless they become a reusable result-projection collaborator outside diagnostic-session summary construction.

Date: 2026-05-26
Area: Flashback xUnit wrapper locality
Problem: Four tiny xUnit wrapper files only registered Flashback harness methods for decoder, encoder sink, exporter, and playback contract groups. The real test behavior already lives in focused Flashback test files and shared harness methods, so the wrapper layer added file-count noise without improving ownership or testability.
Files consolidated: `tests/Sussudio.Tests/XUnit.FlashbackDecoderContractsTests.cs`; `tests/Sussudio.Tests/XUnit.FlashbackEncoderSinkContractsTests.cs`; `tests/Sussudio.Tests/XUnit.FlashbackExporterContractsTests.cs`; `tests/Sussudio.Tests/XUnit.FlashbackPlaybackContractsTests.cs`
Files added: `tests/Sussudio.Tests/XUnit.FlashbackContractsTests.cs`
Net production .cs delta: 0; net test .cs delta: -3
Partial clusters reduced: n/a; xUnit wrapper file count -3
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore` (884 passed); `dotnet exec --% tests\Sussudio.Tests\bin\Debug\net8.0\Sussudio.Tests.dll Sussudio/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/Sussudio.dll`; regenerated `docs/architecture/Sussudio-Defragmentation-Baseline.generated.md`
CLI/MCP/pipe checks, if applicable: n/a; wrapper registration only, no automation command names, IDs, or wire payloads changed
Behavior preserved: Flashback decoder, encoder sink, exporter, and playback xUnit classes, fact method names, harness calls, and target-assembly bootstrap calls remain unchanged.
Notes for future agents: keep Flashback xUnit registration wrappers together in `XUnit.FlashbackContractsTests.cs`; add new detailed behavior coverage to the focused Flashback test files, not to additional one-purpose xUnit wrapper files.

Date: 2026-05-26
Area: capture configuration xUnit wrapper locality
Problem: `CaptureConfigurationModelsTests` was split across four xUnit files even though the files all contributed to one reflection-heavy capture configuration contract surface. Capture mode options, capture settings, MJPEG HFR/bitrate policy, recording selection, encoder support, and recording pipeline option checks shared the same helper class, so the split added file-count noise without a stronger test seam.
Files consolidated: `tests/Sussudio.Tests/XUnit.CaptureModeOptionsTests.cs`; `tests/Sussudio.Tests/XUnit.CaptureSettingsContractsTests.cs`; `tests/Sussudio.Tests/XUnit.RecordingConfigurationPolicyTests.cs`
Files added: none
Net production .cs delta: 0; net test .cs delta: -3
Partial clusters reduced: `CaptureConfigurationModelsTests` xUnit partial file count -3
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore` (884 passed); `dotnet exec --% tests\Sussudio.Tests\bin\Debug\net8.0\Sussudio.Tests.dll Sussudio/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/Sussudio.dll`; regenerated `docs/architecture/Sussudio-Defragmentation-Baseline.generated.md`
CLI/MCP/pipe checks, if applicable: n/a; xUnit wrapper consolidation only, no automation command names, IDs, or wire payloads changed
Behavior preserved: capture configuration helper methods, fact names, reflection assertions, capture mode option behavior checks, capture settings/MJPEG HFR/bitrate checks, recording selection policy checks, encoder support checks, and recording pipeline option capacity checks remain unchanged.
Notes for future agents: keep capture configuration xUnit reflection helpers and their related facts together in `XUnit.CaptureConfigurationModelsTests.cs`; add focused production behavior tests elsewhere only when they exercise a different runtime seam rather than another wrapper around the same contract surface.

Date: 2026-05-26
Area: stats xUnit wrapper locality
Problem: Stats presentation and hardware-row xUnit coverage still used two small partial pairs: frame-time facts lived beside formatting facts, and hardware-row input-provider facts lived beside hardware-row presentation facts. The pairs exercised the same StatsPresentation/StatsHardwareRows helper surfaces and shared reflection/file helper methods, so the split added wrapper-file count without improving behavioral locality.
Files consolidated: `tests/Sussudio.Tests/XUnit.StatsPresentation.FrameTime.Tests.cs`; `tests/Sussudio.Tests/XUnit.StatsHardwareRows.InputProvider.Tests.cs`
Files added: none
Net production .cs delta: 0; net test .cs delta: -2
Partial clusters reduced: `StatsPresentationTests` xUnit partial file count -1; `StatsHardwareRowsTests` xUnit partial file count -1
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore` (884 passed); `dotnet exec --% tests\Sussudio.Tests\bin\Debug\net8.0\Sussudio.Tests.dll Sussudio/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/Sussudio.dll`; regenerated `docs/architecture/Sussudio-Defragmentation-Baseline.generated.md`
CLI/MCP/pipe checks, if applicable: n/a; xUnit wrapper consolidation only, no automation command names, IDs, or wire payloads changed
Behavior preserved: stats presentation formatting, detached-window text, encoder text, expected visual-repeat behavior, compact preview summary source-shape checks, frame-time range and graph geometry checks, hardware decode/GPU row formatting, hardware-row input sampling policy, and presentation-preview harness registration coverage remain unchanged.
Notes for future agents: keep stats presentation xUnit formatting and frame-time behavior in `XUnit.StatsPresentation.Formatting.Tests.cs`; keep hardware row presentation and input-provider behavior in `XUnit.StatsHardwareRowsTests.cs` unless a new independently executable stats test seam emerges.

Date: 2026-05-26
Area: Flashback model xUnit helper locality
Problem: `XUnit.FlashbackModels.PropertyAssertions.cs` only provided reflection/nullability helpers for `XUnit.FlashbackModelsTests.cs`. Reviewing the Flashback model contract suite still required opening a helper partial even though it was not shared outside that test class.
Files consolidated: `tests/Sussudio.Tests/XUnit.FlashbackModels.PropertyAssertions.cs`
Files added: none
Net production .cs delta: 0; net test .cs delta: -1
Partial clusters reduced: `FlashbackModelsTests` xUnit partial file count -1
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore` (884 passed); `dotnet exec --% tests\Sussudio.Tests\bin\Debug\net8.0\Sussudio.Tests.dll Sussudio/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/Sussudio.dll`; regenerated `docs/architecture/Sussudio-Defragmentation-Baseline.generated.md`
CLI/MCP/pipe checks, if applicable: n/a; xUnit helper consolidation only, no automation command names, IDs, or wire payloads changed
Behavior preserved: Flashback buffer option sizing, session/playback/export DTO reflection checks, required/init-only/nullability assertions, property setters/backing-field helpers, enum-value assertions, and collection count helpers remain unchanged.
Notes for future agents: keep Flashback model reflection helpers with `XUnit.FlashbackModelsTests.cs` while they are used only by that contract suite.

Date: 2026-05-26
Area: snapshot model helper locality
Problem: `SnapshotModels.PropertyAssertions.cs` only carried helper/property/nullability assertion methods for the same `SnapshotModelsTests` partial family. Reviewing snapshot DTO reflection, JSON, and property contracts still required a side helper file without an independent test seam.
Files consolidated: `tests/Sussudio.Tests/SnapshotModels.PropertyAssertions.cs`
Files added: none
Net production .cs delta: 0; net test .cs delta: -1
Partial clusters reduced: `SnapshotModelsTests` helper partial file count -1
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore` (884 passed); `dotnet exec --% tests\Sussudio.Tests\bin\Debug\net8.0\Sussudio.Tests.dll Sussudio/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/Sussudio.dll`; regenerated `docs/architecture/Sussudio-Defragmentation-Baseline.generated.md`
CLI/MCP/pipe checks, if applicable: n/a; xUnit helper consolidation only, no automation command names, IDs, or wire payloads changed
Behavior preserved: snapshot DTO property-list assertions, nullability checks, registered property specs, generic-list/single-item helpers, non-null string helper, and reflection JSON registered-property coverage remain unchanged.
Notes for future agents: keep snapshot model reflection/nullability helper methods with `SnapshotModels.Tests.cs` while they are used only by that contract suite.

Date: 2026-05-26
Area: xUnit app-surface and bootstrap helper locality
Problem: `XUnit.BoolConvertersTests.cs` only carried a tiny bool-converter xUnit wrapper that belongs with the existing app-surface contract wrappers, while `XUnit.TargetAssemblyBootstrap.cs` only carried an 11-line helper for the legacy runner's staged app assembly state. Both files added test-file count without an independent behavioral seam.
Files consolidated: `tests/Sussudio.Tests/XUnit.BoolConvertersTests.cs`; `tests/Sussudio.Tests/XUnit.TargetAssemblyBootstrap.cs`
Files added: none
Net production .cs delta: 0; net test .cs delta: -2
Partial clusters reduced: `Program` helper partial file count -1; xUnit wrapper file count -1
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore` (884 passed); `dotnet exec --% tests\Sussudio.Tests\bin\Debug\net8.0\Sussudio.Tests.dll Sussudio/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/Sussudio.dll`; regenerated `docs/architecture/Sussudio-Defragmentation-Baseline.generated.md`
CLI/MCP/pipe checks, if applicable: n/a; xUnit wrapper/helper consolidation only, no automation command names, IDs, or wire payloads changed
Behavior preserved: bool converter xUnit fact names and reflection assertions, app-surface wrapper behavior, staged target assembly loading lock, and legacy runtime snapshot runner assembly resolution remain unchanged.
Notes for future agents: keep small app-surface xUnit wrappers in `XUnit.AutomationContractsTests.cs`; keep the xUnit target assembly bootstrap with `Program.cs` while it only initializes the legacy runner assembly cache.

Date: 2026-05-26
Area: MainWindow UI contract test locality
Problem: MainWindow automation ID inventory, full-screen/window automation contracts, and UI-dispatching contracts were split across three tiny legacy `Program` partial files even though they all guard the same MainWindow UI/automation surface and share the same source-reader helpers. Reviewing MainWindow's agent-facing UI contract still required opening multiple small shards.
Files consolidated: `tests/Sussudio.Tests/MainWindowUiContract.AutomationIds.Tests.cs`; `tests/Sussudio.Tests/MainWindowUiContract.WindowAutomation.Tests.cs`; `tests/Sussudio.Tests/MainWindowUiContract.Dispatching.Tests.cs`
Files added: `tests/Sussudio.Tests/MainWindowUiContract.Tests.cs`
Net production .cs delta: 0; net test .cs delta: -2
Partial clusters reduced: legacy `Program` MainWindow UI contract partial file count -2
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore` (884 passed); `dotnet exec --% tests\Sussudio.Tests\bin\Debug\net8.0\Sussudio.Tests.dll Sussudio/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/Sussudio.dll`; regenerated `docs/architecture/Sussudio-Defragmentation-Baseline.generated.md`
CLI/MCP/pipe checks, if applicable: n/a; test-file consolidation only, no automation command names, IDs, wire payloads, XAML bindings, or runtime behavior changed
Behavior preserved: MainWindow automation ID inventory checks, uniqueness checks, full-screen automation transition assertions, window automation controller routing checks, snap-region ownership assertion, and UI dispatching/run-handler source contract assertions remain unchanged.
Notes for future agents: keep MainWindow's agent-facing UI contract checks in `MainWindowUiContract.Tests.cs`; add focused behavior tests elsewhere only when they exercise a different runtime seam rather than another source-contract shard for the same UI surface.

Date: 2026-05-26
Area: ssctl command-handler routing test locality
Problem: ssctl command-handler routing checks were split across three small legacy `Program` partial files even though they all drive the same pipe-captured routing harness, share the same golden command ID assertion helper, and execute through the same `XUnit.ToolContractsTests` wrapper. Reviewing CLI command routing required bouncing across control, Flashback, and workflow shards.
Files consolidated: `tests/Sussudio.Tests/CommandHandlers.Routing.Control.Tests.cs`; `tests/Sussudio.Tests/CommandHandlers.Routing.Flashback.Tests.cs`; `tests/Sussudio.Tests/CommandHandlers.Routing.Workflow.Tests.cs`
Files added: `tests/Sussudio.Tests/CommandHandlers.Routing.Tests.cs`
Net production .cs delta: 0; net test .cs delta: -2
Partial clusters reduced: legacy `Program` ssctl routing partial file count -2
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore` (884 passed); `dotnet exec --% tests\Sussudio.Tests\bin\Debug\net8.0\Sussudio.Tests.dll Sussudio/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/Sussudio.dll`; regenerated `docs/architecture/Sussudio-Defragmentation-Baseline.generated.md`
CLI/MCP/pipe checks, if applicable: pipe-captured ssctl routing tests remained covered by `XUnit.ToolContractsTests`; no public automation command names, IDs, or wire payloads changed
Behavior preserved: device, capture-control, recordings, Flashback, window, manifest, observability, automation-flow, UI visibility, and verification ssctl routing assertions remain unchanged.
Notes for future agents: keep ssctl command-handler routing coverage in `CommandHandlers.Routing.Tests.cs`; keep routing helpers in `CommandHandlers.Helpers.cs` and source-ownership assertions in `CommandHandlers.SourceOwnership.Tests.cs`.

Date: 2026-05-26
Area: app-surface legacy test locality
Problem: app startup exception-policy checks, XAML bool converter checks, and compact display formatter checks were split across three legacy `Program` partial files even though they are executed together by `AutomationAppSurfaceContractsTests` and all guard the same app-facing surface. Reviewing the app-surface contract still required several tiny files plus the xUnit wrapper.
Files consolidated: `tests/Sussudio.Tests/App.xaml.Tests.cs`; `tests/Sussudio.Tests/BoolConverters.Tests.cs`; `tests/Sussudio.Tests/DisplayFormatters.Tests.cs`
Files added: `tests/Sussudio.Tests/AppSurface.Tests.cs`
Net production .cs delta: 0; net test .cs delta: -2
Partial clusters reduced: legacy `Program` app-surface partial file count -2
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore` (884 passed); `dotnet exec --% tests\Sussudio.Tests\bin\Debug\net8.0\Sussudio.Tests.dll Sussudio/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/Sussudio.dll`; regenerated `docs/architecture/Sussudio-Defragmentation-Baseline.generated.md`
CLI/MCP/pipe checks, if applicable: n/a; test-file consolidation only, no public automation command names, IDs, wire payloads, XAML bindings, or runtime behavior changed
Behavior preserved: App unhandled-exception/recoverability assertions, single-instance/startup source assertions, bool/inverse/visibility converter behavior checks, and `DisplayFormatters.FormatSourceHdr` mapping checks remain unchanged.
Notes for future agents: keep legacy app-surface checks in `AppSurface.Tests.cs` while `XUnit.AutomationContractsTests.cs` remains their xUnit execution surface; move only checks that guard a different runtime seam into a separate owner.

Date: 2026-05-26
Area: MainWindow shell title test locality
Problem: `WindowTitleController.Tests.cs` only carried the formatting behavior check for the same `WindowTitleController` already covered by `MainWindow.ShellOwnership.Chrome.Tests.cs` source-shape assertions. Reviewing MainWindow title behavior required opening a separate one-method legacy `Program` partial.
Files consolidated: `tests/Sussudio.Tests/WindowTitleController.Tests.cs`
Files added: none
Net production .cs delta: 0; net test .cs delta: -1
Partial clusters reduced: legacy `Program` MainWindow shell/title partial file count -1
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore` (884 passed); `dotnet exec --% tests\Sussudio.Tests\bin\Debug\net8.0\Sussudio.Tests.dll Sussudio/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/Sussudio.dll`; regenerated `docs/architecture/Sussudio-Defragmentation-Baseline.generated.md`
CLI/MCP/pipe checks, if applicable: n/a; test-file consolidation only, no public automation command names, IDs, wire payloads, XAML bindings, or runtime behavior changed
Behavior preserved: invariant build-stamp title formatting, missing build-time fallback, idle title, recording suffix, and culture-restoration checks remain unchanged.
Notes for future agents: keep WindowTitleController source-shape and formatting behavior checks with `MainWindow.ShellOwnership.Chrome.Tests.cs` while the controller remains a shell chrome concern.

Date: 2026-05-26
Area: service namespace contract helper locality
Problem: `ServiceNamespace.ServiceContracts.Tests.cs` only carried `AssertServiceContractsBoundaryOwnership`, and the sole caller was the harness-visible `ServiceNamespaces_FollowServiceFolders` entry point in `ServiceNamespace.FolderRules.Tests.cs`. Reviewing service namespace/boundary rules required opening a helper-only partial without an independent execution surface.
Files consolidated: `tests/Sussudio.Tests/ServiceNamespace.ServiceContracts.Tests.cs`
Files added: none
Net production .cs delta: 0; net test .cs delta: -1
Partial clusters reduced: legacy `Program` service namespace helper partial file count -1
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore` (884 passed); `dotnet exec --% tests\Sussudio.Tests\bin\Debug\net8.0\Sussudio.Tests.dll Sussudio/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/Sussudio.dll`; regenerated `docs/architecture/Sussudio-Defragmentation-Baseline.generated.md`
CLI/MCP/pipe checks, if applicable: n/a; test-helper consolidation only, no public automation command names, IDs, wire payloads, XAML bindings, or runtime behavior changed
Behavior preserved: app-service contract namespace assertions, automation-contract isolation checks, pooled-frame lease locality check, service-interface locality checks, and AGENT_MAP service-contract coverage checks remain unchanged.
Notes for future agents: keep service namespace entry-point orchestration and service-contract boundary helper assertions in `ServiceNamespace.FolderRules.Tests.cs`; keep shared project/source parsing helpers in `ServiceNamespace.Helpers.Tests.cs`.

Date: 2026-05-26
Area: MCP frame-pacing verdict test locality
Problem: `McpToolSurface.Performance.FramePacingVerdict.SourceOwnership.Tests.cs` only carried source-shape assertions for the same MCP frame-pacing verdict tool whose behavior contracts already lived in `McpToolSurface.Performance.FramePacingVerdict.Tests.cs`. Reviewing or changing the verdict tool required opening a separate one-method source-ownership shard plus the behavior tests.
Files consolidated: `tests/Sussudio.Tests/McpToolSurface.Performance.FramePacingVerdict.SourceOwnership.Tests.cs`
Files added: none
Net production .cs delta: 0; net test .cs delta: -1
Partial clusters reduced: legacy `Program` MCP frame-pacing verdict partial file count -1
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore` (884 passed); `dotnet exec --% tests\Sussudio.Tests\bin\Debug\net8.0\Sussudio.Tests.dll Sussudio/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/Sussudio.dll`; regenerated `docs/architecture/Sussudio-Defragmentation-Baseline.generated.md`
CLI/MCP/pipe checks, if applicable: MCP frame-pacing verdict behavior and source-shape checks remained covered by `XUnit.McpToolContractsTests`; no public automation command names, IDs, or wire payloads changed
Behavior preserved: frame-pacing verdict tool registration, snapshot/timeline command routing, timeline/channel/policy/rendering locality assertions, half-rate verdict behavior, insufficient-sample verdict behavior, and command payload assertions remain unchanged.
Notes for future agents: keep MCP frame-pacing verdict source-shape and behavior checks together in `McpToolSurface.Performance.FramePacingVerdict.Tests.cs` while they guard one tool surface.

Date: 2026-05-26
Area: capture session coordinator contract test locality
Problem: `CaptureSessionCoordinator.Contracts.Tests.cs` only carried coordinator model/facade source-shape checks adjacent to the public API and snapshot contract checks in `CaptureSessionCoordinator.Api.Tests.cs`. Reviewing the coordinator's API/model/facade contract required opening a separate small legacy `Program` partial.
Files consolidated: `tests/Sussudio.Tests/CaptureSessionCoordinator.Contracts.Tests.cs`
Files added: none
Net production .cs delta: 0; net test .cs delta: -1
Partial clusters reduced: legacy `Program` capture session coordinator contract partial file count -1
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore` (884 passed); `dotnet exec --% tests\Sussudio.Tests\bin\Debug\net8.0\Sussudio.Tests.dll Sussudio/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/Sussudio.dll`; regenerated `docs/architecture/Sussudio-Defragmentation-Baseline.generated.md`
CLI/MCP/pipe checks, if applicable: n/a; test-file consolidation only, no public automation command names, IDs, wire payloads, XAML bindings, or runtime behavior changed
Behavior preserved: coordinator public method coverage, command-kind enum coverage, snapshot contract/default-state checks, model-surface locality checks, and Flashback facade locality checks remain unchanged.
Notes for future agents: keep CaptureSessionCoordinator API, model, snapshot, and facade contract assertions in `CaptureSessionCoordinator.Api.Tests.cs`; keep queue/Flashback behavior and broader source-ownership checks in their focused owner files.

Date: 2026-05-26
Area: pooled video frame lease test locality
Problem: `PooledVideoFrame.QueuedLeaseRelease.Tests.cs` only carried queued-lease return checks for the same pooled-frame lease lifecycle/fan-out contract surface already owned by `PooledVideoFrame.Leases.Tests.cs`. Reviewing lease ownership across preview, recording, and Flashback paths required opening a separate small legacy `Program` partial.
Files consolidated: `tests/Sussudio.Tests/PooledVideoFrame.QueuedLeaseRelease.Tests.cs`
Files added: none
Net production .cs delta: 0; net test .cs delta: -1
Partial clusters reduced: legacy `Program` pooled-frame lease partial file count -1
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore` (884 passed); `dotnet exec --% tests\Sussudio.Tests\bin\Debug\net8.0\Sussudio.Tests.dll Sussudio/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/Sussudio.dll`; regenerated `docs/architecture/Sussudio-Defragmentation-Baseline.generated.md`
CLI/MCP/pipe checks, if applicable: n/a; test-file consolidation only, no public automation command names, IDs, wire payloads, XAML bindings, or runtime behavior changed
Behavior preserved: pooled-frame lease lifecycle, MJPEG pooled-frame fan-out, D3D pending-frame queued lease disposal, and recording/Flashback queued packet lease cleanup checks remain unchanged.
Notes for future agents: keep queued lease return coverage in `PooledVideoFrame.Leases.Tests.cs` with the rest of the pooled-frame lease contract tests; keep jitter policy/queue behavior in their focused MJPEG jitter files.

Date: 2026-05-26
Area: service namespace MainViewModel device-audio helper locality
Problem: `ServiceNamespace.SourceOwnership.MainViewModelDeviceAudio.Tests.cs` only carried private source-ownership assertions invoked by the service-layer ownership parent. Reviewing service namespace drift for MainViewModel device-audio required opening a separate helper-only partial with no independent execution surface.
Files consolidated: `tests/Sussudio.Tests/ServiceNamespace.SourceOwnership.MainViewModelDeviceAudio.Tests.cs`
Files added: none
Net production .cs delta: 0; net test .cs delta: -1
Partial clusters reduced: legacy `Program` service namespace source-ownership helper partial file count -1
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore` (884 passed); `dotnet exec --% tests\Sussudio.Tests\bin\Debug\net8.0\Sussudio.Tests.dll Sussudio/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/Sussudio.dll`; regenerated `docs/architecture/Sussudio-Defragmentation-Baseline.generated.md`
CLI/MCP/pipe checks, if applicable: n/a; test-helper consolidation only, no public automation command names, IDs, wire payloads, XAML bindings, or runtime behavior changed
Behavior preserved: MainViewModel device-native audio state, device audio mode/gain ownership, request-controller port shape, and deleted legacy audio-control partial guards remain unchanged.
Notes for future agents: keep MainViewModel device-audio source-ownership assertions in `ServiceNamespace.SourceOwnership.ServicesLayer.Tests.cs` with the service-layer orchestration entry point; keep runtime and broader device/capture assertions in their focused sibling owner files.

Date: 2026-05-26
Area: diagnostics refresh alert-event helper locality
Problem: `MainViewModel.Automation.DiagnosticsRefresh.AlertEvents.Tests.cs` only carried private alert/event ownership assertions invoked by `DiagnosticsSnapshotRefresh_IsSerializedForRecordingResponses` in the diagnostics refresh entry-point file. Reviewing diagnostics refresh orchestration and the alert/event ownership checks required opening a separate helper-only partial.
Files consolidated: `tests/Sussudio.Tests/MainViewModel.Automation.DiagnosticsRefresh.AlertEvents.Tests.cs`
Files added: none
Net production .cs delta: 0; net test .cs delta: -1
Partial clusters reduced: legacy `Program` diagnostics-refresh helper partial file count -1
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore` (884 passed); `dotnet exec --% tests\Sussudio.Tests\bin\Debug\net8.0\Sussudio.Tests.dll Sussudio/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/Sussudio.dll`; regenerated `docs/architecture/Sussudio-Defragmentation-Baseline.generated.md`
CLI/MCP/pipe checks, if applicable: n/a; test-helper consolidation only, no public automation command names, IDs, wire payloads, XAML bindings, or runtime behavior changed
Behavior preserved: diagnostics UpdateAlerts ownership, diagnostic event state guards, signal alert guards, Flashback alert routing guards, and deleted legacy alert partial guards remain unchanged.
Notes for future agents: keep diagnostics alert/event ownership assertions in `MainViewModel.Automation.DiagnosticsRefresh.Tests.cs` with the orchestration entry point; keep detailed Flashback alert coverage in the focused RecordingAndStorage and PlaybackAndPreview owner files.

Date: 2026-05-26
Area: diagnostics refresh source-reader helper locality
Problem: `MainViewModel.Automation.DiagnosticsRefresh.SourceReaderOwnership.Tests.cs` only carried private source-reader ownership assertions invoked by `DiagnosticsSnapshotRefresh_IsSerializedForRecordingResponses` in the diagnostics refresh entry-point file. Reviewing diagnostics refresh orchestration and its source-reader ownership contract required opening a separate helper-only partial.
Files consolidated: `tests/Sussudio.Tests/MainViewModel.Automation.DiagnosticsRefresh.SourceReaderOwnership.Tests.cs`
Files added: none
Net production .cs delta: 0; net test .cs delta: -1
Partial clusters reduced: legacy `Program` diagnostics-refresh source-reader helper partial file count -1
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore` (884 passed); `dotnet exec --% tests\Sussudio.Tests\bin\Debug\net8.0\Sussudio.Tests.dll Sussudio/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/Sussudio.dll`; regenerated `docs/architecture/Sussudio-Defragmentation-Baseline.generated.md`
CLI/MCP/pipe checks, if applicable: n/a; test-helper consolidation only, no public automation command names, IDs, wire payloads, XAML bindings, or runtime behavior changed
Behavior preserved: source-reader cadence coherence guards, vtable/DXGI/frame-layout/read-loop/initialization/frame-delivery source ownership checks, and deleted legacy source-reader partial guards remain unchanged.
Notes for future agents: keep diagnostics refresh source-reader ownership assertions in `MainViewModel.Automation.DiagnosticsRefresh.Tests.cs` with the orchestration entry point; keep reusable source-family readers with the rest of the refresh source-family helpers in `MainViewModel.Automation.DiagnosticsRefresh.SourceFamily.cs`.

Date: 2026-05-26
Area: diagnostics refresh snapshot-projection helper locality
Problem: `MainViewModel.Automation.DiagnosticsRefresh.SnapshotProjection.Tests.cs` only carried private compact integration-smoke assertions invoked by `DiagnosticsSnapshotRefresh_IsSerializedForRecordingResponses` in the diagnostics refresh entry-point file. Reviewing diagnostics refresh orchestration and the projection-set/flattening handoff required opening a separate helper-only partial.
Files consolidated: `tests/Sussudio.Tests/MainViewModel.Automation.DiagnosticsRefresh.SnapshotProjection.Tests.cs`
Files added: none
Net production .cs delta: 0; net test .cs delta: -1
Partial clusters reduced: legacy `Program` diagnostics-refresh snapshot-projection helper partial file count -1
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore` (884 passed); `dotnet exec --% tests\Sussudio.Tests\bin\Debug\net8.0\Sussudio.Tests.dll Sussudio/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/Sussudio.dll`; regenerated `docs/architecture/Sussudio-Defragmentation-Baseline.generated.md`
CLI/MCP/pipe checks, if applicable: n/a; test-helper consolidation only, no public automation command names, IDs, wire payloads, XAML bindings, or runtime behavior changed
Behavior preserved: snapshot status/evaluation projection guards, projection-set composition routing, flattened projection handoff guards, and detailed leaf projection ownership checks remain covered by the focused diagnostics projection owner files.
Notes for future agents: keep the compact diagnostics refresh snapshot-projection smoke in `MainViewModel.Automation.DiagnosticsRefresh.Tests.cs`; keep detailed projection source-shape contracts in `MainViewModel.Automation.DiagnosticsProjection.*.Tests.cs`.

Date: 2026-05-26
Area: diagnostics refresh diagnostic-session preview helper locality
Problem: `MainViewModel.Automation.DiagnosticsRefresh.DiagnosticSessionPreview.Tests.cs` only carried private diagnostic-session preview/visual-cadence/D3D/process metric assertions invoked by `DiagnosticsSnapshotRefresh_IsSerializedForRecordingResponses` in the diagnostics refresh entry-point file. Reviewing diagnostics refresh orchestration and preview metric coverage required opening a separate helper-only partial.
Files consolidated: `tests/Sussudio.Tests/MainViewModel.Automation.DiagnosticsRefresh.DiagnosticSessionPreview.Tests.cs`
Files added: none
Net production .cs delta: 0; net test .cs delta: -1
Partial clusters reduced: legacy `Program` diagnostics-refresh diagnostic-session preview helper partial file count -1
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore` (884 passed); `dotnet exec --% tests\Sussudio.Tests\bin\Debug\net8.0\Sussudio.Tests.dll Sussudio/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/Sussudio.dll`; regenerated `docs/architecture/Sussudio-Defragmentation-Baseline.generated.md`
CLI/MCP/pipe checks, if applicable: n/a; test-helper consolidation only, no public automation command names, IDs, wire payloads, XAML bindings, or runtime behavior changed
Behavior preserved: diagnostic-session preview scheduler metrics, capture-mode metrics, D3D CPU/frame-stat metrics, visual cadence metrics, process metrics, and timeline preview/Flashback/export projection guards remain unchanged.
Notes for future agents: keep diagnostic-session preview metric assertions in `MainViewModel.Automation.DiagnosticsRefresh.Tests.cs` while they are only invoked by the diagnostics refresh orchestration entry point; keep larger playback and scenario helpers separate unless folding still keeps the owner cohesive.

Date: 2026-05-26
Area: MCP diagnostic-session result-builder Flashback helper locality
Problem: `McpToolSurface.DiagnosticSession.ResultOwnership.Builder.Flashback.Tests.cs` only carried private Flashback playback/recording/export result projection assertions invoked by `DiagnosticSessionResultBuilder_OwnsSummaryConstruction` in the result-builder ownership file. Reviewing result-builder ownership required opening a separate helper-only partial for one call in the same summary-construction contract.
Files consolidated: `tests/Sussudio.Tests/McpToolSurface.DiagnosticSession.ResultOwnership.Builder.Flashback.Tests.cs`
Files added: none
Net production .cs delta: 0; net test .cs delta: -1
Partial clusters reduced: legacy `Program` MCP diagnostic-session result-builder helper partial file count -1
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore` (884 passed); `dotnet exec --% tests\Sussudio.Tests\bin\Debug\net8.0\Sussudio.Tests.dll Sussudio/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/Sussudio.dll`; regenerated `docs/architecture/Sussudio-Defragmentation-Baseline.generated.md`
CLI/MCP/pipe checks, if applicable: diagnostic-session result-builder source-shape checks remain covered through `XUnit.McpDiagnosticSessionContractsTests`; no public automation command names, IDs, or wire payloads changed
Behavior preserved: Flashback playback, recording, and export result projection routing, flattening handoff, and deleted legacy result-builder partial guards remain unchanged.
Notes for future agents: keep Flashback result-builder projection assertions in `McpToolSurface.DiagnosticSession.ResultOwnership.Builder.Tests.cs` with the summary-construction orchestration; keep preview/completion and health-analysis checks in their focused sibling owner while they retain independent xUnit coverage.

Date: 2026-05-26
Area: service namespace MainViewModel device/capture helper locality
Problem: `ServiceNamespace.SourceOwnership.MainViewModelDeviceAndCapture.Tests.cs` only carried private source-ownership assertions invoked by the service-layer ownership parent. Reviewing service namespace drift for MainViewModel device/capture/source-telemetry/recording-capability concerns required opening a separate helper-only partial with no independent execution surface.
Files consolidated: `tests/Sussudio.Tests/ServiceNamespace.SourceOwnership.MainViewModelDeviceAndCapture.Tests.cs`
Files added: none
Net production .cs delta: 0; net test .cs delta: -1
Partial clusters reduced: legacy `Program` service namespace source-ownership helper partial file count -1
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore` (884 passed); `dotnet exec --% tests\Sussudio.Tests\bin\Debug\net8.0\Sussudio.Tests.dll Sussudio/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/Sussudio.dll`; regenerated `docs/architecture/Sussudio-Defragmentation-Baseline.generated.md`
CLI/MCP/pipe checks, if applicable: n/a; test-helper consolidation only, no public automation command names, IDs, wire payloads, XAML bindings, or runtime behavior changed
Behavior preserved: MainViewModel device refresh, capture device selection, audio device scan handoff, format probe retargeting, source telemetry, recording capability, and preview renderer enqueue source-ownership guards remain unchanged.
Notes for future agents: keep MainViewModel device/capture/source-telemetry service-namespace assertions in `ServiceNamespace.SourceOwnership.ServicesLayer.Tests.cs` with the service-layer orchestration entry point; keep MainViewModel runtime ownership assertions there too while they remain private helper checks invoked by that same parent.

Date: 2026-05-26
Area: service namespace MainViewModel runtime helper locality
Problem: `ServiceNamespace.SourceOwnership.MainViewModelRuntime.Tests.cs` only carried private runtime source-ownership assertions invoked by the service-layer ownership parent. Reviewing MainViewModel service-namespace drift for UI dispatch, property-change, runtime lifecycle/event-ingress, recording runtime, and disposal concerns required opening a separate helper-only partial with no independent execution surface.
Files consolidated: `tests/Sussudio.Tests/ServiceNamespace.SourceOwnership.MainViewModelRuntime.Tests.cs`
Files added: none
Net production .cs delta: 0; net test .cs delta: -1
Partial clusters reduced: legacy `Program` service namespace source-ownership helper partial file count -1
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore` (884 passed); `dotnet exec --% tests\Sussudio.Tests\bin\Debug\net8.0\Sussudio.Tests.dll Sussudio/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/Sussudio.dll`; regenerated `docs/architecture/Sussudio-Defragmentation-Baseline.generated.md`
CLI/MCP/pipe checks, if applicable: n/a; test-helper consolidation only, no public automation command names, IDs, wire payloads, XAML bindings, or runtime behavior changed
Behavior preserved: MainViewModel UI dispatch, audio/capture property-change, runtime lifecycle/event-ingress, recording runtime, disk-space presentation, disposal, and capture status/error source-ownership guards remain unchanged.
Notes for future agents: keep MainViewModel runtime source-ownership assertions in `ServiceNamespace.SourceOwnership.ServicesLayer.Tests.cs` with the service-layer orchestration entry point; create a separate helper only for an independently invoked runtime ownership seam.

Date: 2026-05-26
Area: Flashback buffer segment lookup/query test locality
Problem: `Flashback.Buffer.SegmentQueries.Tests.cs` and `Flashback.Buffer.SegmentLookups.Tests.cs` split the same read-only Flashback buffer segment access surface across two small legacy `Program` partial files. Reviewing position lookup, next-segment walking, path normalization, range queries, active path, segment count, and segment-list behavior required opening both files even though they share the same reflected buffer-manager fixture and completed-segment helpers.
Files consolidated: `tests/Sussudio.Tests/Flashback.Buffer.SegmentQueries.Tests.cs`
Files added: none
Net production .cs delta: 0; net test .cs delta: -1
Partial clusters reduced: legacy `Program` Flashback buffer segment test helper partial file count -1
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore` (884 passed); `dotnet exec --% tests\Sussudio.Tests\bin\Debug\net8.0\Sussudio.Tests.dll Sussudio/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/Sussudio.dll`; regenerated `docs/architecture/Sussudio-Defragmentation-Baseline.generated.md`
CLI/MCP/pipe checks, if applicable: n/a; test-only consolidation, no public automation command names, IDs, wire payloads, XAML bindings, or runtime behavior changed
Behavior preserved: Flashback buffer segment position lookup, next-segment path lookup, path normalization, segment-start PTS, range query, active path, segment-count, and segment-list behavior tests remain registered through the same xUnit recording model contract surface.
Notes for future agents: keep read-only Flashback buffer segment access tests in `Flashback.Buffer.SegmentLookups.Tests.cs`; keep shared reflected buffer-manager factories in `Flashback.Buffer.Helpers.cs` while retention, segment mutation, and query/lookup tests all use them.

Date: 2026-05-26
Area: Flashback buffer startup validation test locality
Problem: `Flashback.Buffer.Validation.Tests.cs` only carried session-id and segment-extension validation tests for the same Flashback startup/session scanner policy already asserted by `Flashback.Buffer.Retention.StartupCleanup.Tests.cs`. Reviewing startup session directory safety, segment extension normalization, stale cleanup, and cache-budget behavior required opening a separate tiny validation shard before returning to the startup-cleanup owner.
Files consolidated: `tests/Sussudio.Tests/Flashback.Buffer.Validation.Tests.cs`
Files added: none
Net production .cs delta: 0; net test .cs delta: -1
Partial clusters reduced: legacy `Program` Flashback buffer startup validation test partial file count -1
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore` (884 passed); `dotnet exec --% tests\Sussudio.Tests\bin\Debug\net8.0\Sussudio.Tests.dll Sussudio/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/Sussudio.dll`; regenerated `docs/architecture/Sussudio-Defragmentation-Baseline.generated.md`
CLI/MCP/pipe checks, if applicable: n/a; test-only consolidation, no public automation command names, IDs, wire payloads, XAML bindings, or runtime behavior changed
Behavior preserved: Flashback buffer unsafe session-id rejection and segment extension normalization/rejection tests remain registered through the same xUnit recording model contract surface.
Notes for future agents: keep Flashback buffer session-id and segment-extension validation tests in `Flashback.Buffer.Retention.StartupCleanup.Tests.cs` while they guard the same startup/session scanner policy; keep segment completion metadata validation in `Flashback.Buffer.Segments.Validation.Tests.cs`.

Date: 2026-05-26
Area: automation dispatcher readiness test locality
Problem: `AutomationCommandDispatcher.Readiness.Tests.cs` carried dispatcher readiness gating, UI readiness bypass, preview-health wait-condition, and window-close completion guards while `AutomationCommandDispatcher.ReadyIndependent.Tests.cs` owned the no-hardware ready-independent command harness. Reviewing automation readiness behavior required opening both files even though they protect the same command-readiness surface and share dispatcher source/harness helpers.
Files consolidated: `tests/Sussudio.Tests/AutomationCommandDispatcher.Readiness.Tests.cs`
Files added: none
Net production .cs delta: 0; net test .cs delta: -1
Partial clusters reduced: legacy `Program` automation dispatcher readiness test partial file count -1
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore` (884 passed); `dotnet exec --% tests\Sussudio.Tests\bin\Debug\net8.0\Sussudio.Tests.dll Sussudio/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/Sussudio.dll`; regenerated `docs/architecture/Sussudio-Defragmentation-Baseline.generated.md`
CLI/MCP/pipe checks, if applicable: n/a; test-only consolidation, no public automation command names, IDs, wire payloads, XAML bindings, or runtime behavior changed
Behavior preserved: dispatcher ready-device classification, ready-independent no-hardware command execution, UI readiness bypass guards, preview-renderer-health wait condition, and window-close completion checks remain registered through the same xUnit automation contract surface.
Notes for future agents: keep readiness gating and no-hardware ready-independent command coverage in `AutomationCommandDispatcher.ReadyIndependent.Tests.cs`; keep payload parsing/catalog and command ownership checks in their existing focused dispatcher owner files.

Date: 2026-05-26
Area: D3D11 preview renderer cadence diagnostics test locality
Problem: `D3D11PreviewRenderer.Cadence.Tests.cs` only carried present-cadence metric shape and suppression baseline tests, while `D3D11PreviewRenderer.DiagnosticsContract.Tests.cs` owned the adjacent renderer diagnostics API/source-shape contract. Reviewing renderer timing diagnostics required opening a separate tiny cadence shard before returning to the diagnostics contract owner.
Files consolidated: `tests/Sussudio.Tests/D3D11PreviewRenderer.Cadence.Tests.cs`
Files added: none
Net production .cs delta: 0; net test .cs delta: -1
Partial clusters reduced: legacy `Program` D3D11 preview diagnostics test partial file count -1
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore` (884 passed); `dotnet exec --% tests\Sussudio.Tests\bin\Debug\net8.0\Sussudio.Tests.dll Sussudio/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/Sussudio.dll`; regenerated `docs/architecture/Sussudio-Defragmentation-Baseline.generated.md`
CLI/MCP/pipe checks, if applicable: n/a; test-only consolidation, no public automation command names, IDs, wire payloads, XAML bindings, or runtime behavior changed
Behavior preserved: present cadence metric reflection and suppression-baseline tests remain registered through the same xUnit presentation-preview D3D contract surface.
Notes for future agents: keep present cadence metric shape and suppression-baseline checks in `D3D11PreviewRenderer.DiagnosticsContract.Tests.cs` with the rest of the renderer diagnostics API contract; keep snapshot-model and performance-timeline DTO reflection in their focused sibling files.

Date: 2026-05-26
Area: MainViewModel preview reinitialization test locality
Problem: `MainViewModel.Capture.Reinitialization.Tests.cs` only carried MainViewModel preview lifecycle/reinitialize controller placement assertions, while `MainViewModel.Capture.PreviewStartup.SessionReinit.Tests.cs` already owned the adjacent preview startup session/reinit adapter wiring and pending Flashback cycle wait checks. Reviewing preview reinitialization ownership required opening a separate tiny source-shape shard before returning to the session/reinit owner.
Files consolidated: `tests/Sussudio.Tests/MainViewModel.Capture.Reinitialization.Tests.cs`
Files added: none
Net production .cs delta: 0; net test .cs delta: -1
Partial clusters reduced: legacy `Program` MainViewModel preview reinitialization test partial file count -1
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore` (884 passed); `dotnet exec --% tests\Sussudio.Tests\bin\Debug\net8.0\Sussudio.Tests.dll Sussudio/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/Sussudio.dll`; regenerated `docs/architecture/Sussudio-Defragmentation-Baseline.generated.md`
CLI/MCP/pipe checks, if applicable: n/a; test-only consolidation, no public automation command names, IDs, wire payloads, XAML bindings, or runtime behavior changed
Behavior preserved: MainViewModel preview lifecycle/reinitialize controller placement, preview startup session/reinit adapter wiring, and pending Flashback cycle wait source-shape checks remain registered through the same xUnit presentation-preview contract surfaces.
Notes for future agents: keep MainViewModel preview lifecycle/reinitialize controller placement checks in `MainViewModel.Capture.PreviewStartup.SessionReinit.Tests.cs` with the preview startup session/reinit adapter ownership checks.

Date: 2026-05-26
Area: automation diagnostics projection test locality
Problem: nine small `MainViewModel.Automation.DiagnosticsProjection.*.Tests.cs` shards split the focused automation snapshot projection ownership surface by projection family even though they share the same xUnit execution surface, source readers, and production projection family. Reviewing snapshot root, audio/ingest, capture command/settings, capture format/transport, source/cadence, MJPEG, recording, preview, and Flashback projection ownership required hopping across tiny legacy `Program` partial files after the production projection code had already been consolidated into cohesive parent files.
Files consolidated: `tests/Sussudio.Tests/MainViewModel.Automation.DiagnosticsProjection.Snapshot.Tests.cs`; `tests/Sussudio.Tests/MainViewModel.Automation.DiagnosticsProjection.Audio.Tests.cs`; `tests/Sussudio.Tests/MainViewModel.Automation.DiagnosticsProjection.Capture.Tests.cs`; `tests/Sussudio.Tests/MainViewModel.Automation.DiagnosticsProjection.CaptureFormatTransport.Tests.cs`; `tests/Sussudio.Tests/MainViewModel.Automation.DiagnosticsProjection.SourceCadence.Tests.cs`; `tests/Sussudio.Tests/MainViewModel.Automation.DiagnosticsProjection.Mjpeg.Tests.cs`; `tests/Sussudio.Tests/MainViewModel.Automation.DiagnosticsProjection.Recording.Tests.cs`; `tests/Sussudio.Tests/MainViewModel.Automation.DiagnosticsProjection.Preview.Tests.cs`; `tests/Sussudio.Tests/MainViewModel.Automation.DiagnosticsProjection.Flashback.Tests.cs`
Files added: `tests/Sussudio.Tests/MainViewModel.Automation.DiagnosticsProjection.Tests.cs`
Net production .cs delta: 0; net test .cs delta: -8
Partial clusters reduced: legacy `Program` automation diagnostics projection test partial file count -8
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore` (884 passed); `dotnet exec --% tests\Sussudio.Tests\bin\Debug\net8.0\Sussudio.Tests.dll Sussudio/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/Sussudio.dll`; regenerated `docs/architecture/Sussudio-Defragmentation-Baseline.generated.md`
CLI/MCP/pipe checks, if applicable: n/a; test-only consolidation, no public automation command names, IDs, wire payloads, XAML bindings, or runtime behavior changed
Behavior preserved: all 23 automation diagnostics projection ownership tests remain registered through `XUnit.AutomationContractsTests`, covering snapshot root, audio/ingest, capture command/settings, capture format/transport/HDR, source/cadence, MJPEG, recording, preview, and Flashback projection assertions.
Notes for future agents: keep focused automation diagnostics projection ownership assertions in `MainViewModel.Automation.DiagnosticsProjection.Tests.cs`; do not recreate the tiny projection-family test shards or move this surface back into the diagnostics refresh mega owner.

Date: 2026-05-26
Area: Flashback playback command queue test locality
Problem: Flashback playback command queue capacity/drop-oldest, scrub coalescing, seek-slot barrier, and rejected-barrier failure-mode coverage lived in four tiny legacy `Program` partial files even though they all protect the same private command queue contract in `FlashbackPlaybackController.CommandQueue.cs`. Reviewing command queue behavior required opening separate shard files for queue capacity, scrub coalescing, seek slots, and failure modes.
Files consolidated: `tests/Sussudio.Tests/Flashback.Playback.CommandQueue.Capacity.Tests.cs`; `tests/Sussudio.Tests/Flashback.Playback.CommandQueue.ScrubCoalescing.Tests.cs`; `tests/Sussudio.Tests/Flashback.Playback.CommandQueue.SeekSlots.Tests.cs`; `tests/Sussudio.Tests/Flashback.Playback.CommandQueue.SeekSlots.FailureModes.Tests.cs`
Files added: `tests/Sussudio.Tests/Flashback.Playback.CommandQueue.Tests.cs`
Net production .cs delta: 0; net test .cs delta: -3
Partial clusters reduced: legacy `Program` Flashback playback command queue test partial file count -3
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore` (884 passed); `dotnet exec --% tests\Sussudio.Tests\bin\Debug\net8.0\Sussudio.Tests.dll Sussudio/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/Sussudio.dll`; regenerated `docs/architecture/Sussudio-Defragmentation-Baseline.generated.md`
CLI/MCP/pipe checks, if applicable: n/a; test-only consolidation, no public automation command names, IDs, wire payloads, XAML bindings, or runtime behavior changed
Behavior preserved: Flashback playback command queue capacity/drop-oldest, scrub coalescing source ownership, seek-slot barrier, and rejected-barrier failure-mode checks remain registered through `XUnit.FlashbackContractsTests`.
Notes for future agents: keep Flashback playback command queue contract coverage in `Flashback.Playback.CommandQueue.Tests.cs`; do not recreate separate capacity, scrub-coalescing, seek-slot, or seek-slot failure-mode shards.

Date: 2026-05-26
Area: output path and disk-space presentation test locality
Problem: `MainViewModel.DiskSpacePresentation.Tests.cs` carried output picker ownership and output drive free-space presentation checks even though those assertions point at the same recording-output workflow covered by `MainWindow.ControllerOwnership.Output.Tests.cs`: `OutputPathController`, MainWindow output display/actions, `MainViewModel.RecordingState`, and `OutputDriveSpacePresentationBuilder`. Reviewing output path behavior required opening separate tiny MainViewModel and MainWindow shards.
Files consolidated: `tests/Sussudio.Tests/MainViewModel.DiskSpacePresentation.Tests.cs`
Files added: none
Net production .cs delta: 0; net test .cs delta: -1
Partial clusters reduced: legacy `Program` output path/disk-space presentation test partial file count -1
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore` (884 passed); `dotnet exec --% tests\Sussudio.Tests\bin\Debug\net8.0\Sussudio.Tests.dll Sussudio/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/Sussudio.dll`; regenerated `docs/architecture/Sussudio-Defragmentation-Baseline.generated.md`
CLI/MCP/pipe checks, if applicable: n/a; test-only consolidation, no public automation command names, IDs, wire payloads, XAML bindings, or runtime behavior changed
Behavior preserved: output path display/action, output picker ownership, invalid output-drive probing, and output drive free-space presentation ownership checks remain registered through the presentation-preview xUnit/harness surfaces.
Notes for future agents: keep output path display/action and disk-space presentation bridge checks in `MainWindow.ControllerOwnership.Output.Tests.cs`; do not recreate a separate `MainViewModel.DiskSpacePresentation.Tests.cs` shard.

Date: 2026-05-26
Area: automation dispatcher payload test locality
Problem: dispatcher JSON payload helper coverage and dispatcher/catalog payload metadata parity lived in two small `AutomationCommandDispatcher.Payload.*.Tests.cs` shards even though both protect the same automation dispatcher payload contract: extraction helpers, default payload parsing, one-field handler metadata, and the `GetAudioRampTrace.maxEntries` guardrail.
Files consolidated: `tests/Sussudio.Tests/AutomationCommandDispatcher.Payload.Extraction.Tests.cs`; `tests/Sussudio.Tests/AutomationCommandDispatcher.Payload.Catalog.Tests.cs`
Files added: `tests/Sussudio.Tests/AutomationCommandDispatcher.Payload.Tests.cs`
Net production .cs delta: 0; net test .cs delta: -1
Partial clusters reduced: legacy `Program` automation dispatcher payload test partial file count -1
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore` (884 passed); `dotnet exec --% tests\Sussudio.Tests\bin\Debug\net8.0\Sussudio.Tests.dll Sussudio/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/Sussudio.dll`; regenerated `docs/architecture/Sussudio-Defragmentation-Baseline.generated.md`
CLI/MCP/pipe checks, if applicable: n/a; test-only consolidation, no public automation command names, IDs, wire payloads, XAML bindings, or runtime behavior changed
Behavior preserved: dispatcher payload extraction helpers, missing/default window and wait payload behavior, one-field handler/catalog parity, and `GetAudioRampTrace.maxEntries` metadata checks remain registered through `XUnit.AutomationContractsTests`.
Notes for future agents: keep dispatcher payload extraction and catalog metadata parity checks in `AutomationCommandDispatcher.Payload.Tests.cs`; do not recreate separate payload extraction/catalog shards.

Date: 2026-05-26
Area: diagnostic-session result formatter test locality
Problem: `McpToolSurface.DiagnosticSession.ResultOwnership.Formatter.Tests.cs` only carried the formatted-summary ownership assertion for `DiagnosticSessionResultFormatter`, while `McpToolSurface.DiagnosticSession.ResultOwnership.Tests.cs` already owned diagnostic-session result model, optional text, artifact, JSON, and summary-write ownership assertions through the same diagnostic-session result source-reader surface. Reviewing the result surface required opening a one-method formatter shard beside the main result owner.
Files consolidated: `tests/Sussudio.Tests/McpToolSurface.DiagnosticSession.ResultOwnership.Formatter.Tests.cs`
Files added: none
Net production .cs delta: 0; net test .cs delta: -1
Partial clusters reduced: legacy `Program` diagnostic-session result ownership test partial file count -1
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore` (884 passed); `dotnet exec --% tests\Sussudio.Tests\bin\Debug\net8.0\Sussudio.Tests.dll Sussudio/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/Sussudio.dll`; regenerated `docs/architecture/Sussudio-Defragmentation-Baseline.generated.md`
CLI/MCP/pipe checks, if applicable: n/a; test-only consolidation, no public automation command names, IDs, wire payloads, XAML bindings, or runtime behavior changed
Behavior preserved: diagnostic-session result formatter, model ownership, optional text formatting, artifact, JSON, and summary-write ownership checks remain registered through `XUnit.McpDiagnosticSessionContractsTests`.
Notes for future agents: keep diagnostic-session result formatter ownership in `McpToolSurface.DiagnosticSession.ResultOwnership.Tests.cs`; keep builder projection assertions in the focused builder owner files.

Date: 2026-05-26
Area: MainWindow Flashback interaction test locality
Problem: Flashback scrub/fullscreen bridge ownership and Flashback timeline toggle rollback/lockout ownership lived in separate `MainViewModel.Capture.FlashbackRouting.*.Tests.cs` shards even though both assert the same MainWindow Flashback interaction adapter and controller surface: `MainWindow.Flashback.Interactions.cs`, `FlashbackCommandController`, `FlashbackTimelineController`, scrub interaction, playhead motion, fullscreen bridge hooks, and settings/property-change lockout behavior.
Files consolidated: `tests/Sussudio.Tests/MainViewModel.Capture.FlashbackRouting.Scrub.Tests.cs`; `tests/Sussudio.Tests/MainViewModel.Capture.FlashbackRouting.Toggle.Tests.cs`
Files added: `tests/Sussudio.Tests/MainViewModel.Capture.FlashbackRouting.Interactions.Tests.cs`
Net production .cs delta: 0; net test .cs delta: -1
Partial clusters reduced: legacy `Program` MainWindow Flashback interaction test partial file count -1
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore` (884 passed); `dotnet exec --% tests\Sussudio.Tests\bin\Debug\net8.0\Sussudio.Tests.dll Sussudio/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/Sussudio.dll`; regenerated `docs/architecture/Sussudio-Defragmentation-Baseline.generated.md`
CLI/MCP/pipe checks, if applicable: n/a; test-only consolidation, no public automation command names, IDs, wire payloads, XAML bindings, or runtime behavior changed
Behavior preserved: Flashback scrub release/cancel/capture-lost, geometry math, fullscreen Flashback bridge, timeline toggle rollback, lockout, and settings/property-change assertions remain registered through `XUnit.AutomationContractsTests`.
Notes for future agents: keep MainWindow Flashback scrub, fullscreen bridge, toggle rollback, and timeline lockout checks in `MainViewModel.Capture.FlashbackRouting.Interactions.Tests.cs`; keep capture-service Flashback cadence/backend lifecycle tests in their focused owner files.

Date: 2026-05-26
Area: dispatcher and UnifiedVideoCapture test locality
Problem: two smallest-file candidates were legacy `Program` partial shards rather than meaningful standalone owners. `AutomationCommandDispatcher.FlashbackFailures.Tests.cs` split Flashback command routing/failure diagnostics away from the dispatcher command ownership surface, while `UnifiedVideoCapture.Runtime.Tests.cs` split CPU-MJPEG format reporting and stop-failure retention away from the existing UnifiedVideoCapture frame-ingress/fanout ownership checks. Reviewing those behaviors required opening extra tiny files beside the real parent test owners.
Files consolidated: `tests/Sussudio.Tests/AutomationCommandDispatcher.FlashbackFailures.Tests.cs`; `tests/Sussudio.Tests/UnifiedVideoCapture.Runtime.Tests.cs`
Files added: none
Net production .cs delta: 0; net test .cs delta: -2
Partial clusters reduced: legacy `Program` automation dispatcher and UnifiedVideoCapture test partial file count -2
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore` (884 passed); `dotnet exec --% tests\Sussudio.Tests\bin\Debug\net8.0\Sussudio.Tests.dll Sussudio/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/Sussudio.dll`; regenerated `docs/architecture/Sussudio-Defragmentation-Baseline.generated.md`; `git diff --check`
CLI/MCP/pipe checks, if applicable: n/a; test-only consolidation, no public automation command names, IDs, wire payloads, XAML bindings, or runtime behavior changed
Behavior preserved: Flashback automation action failure diagnostics and Flashback command routing checks remain registered through `XUnit.AutomationContractsTests`; UnifiedVideoCapture CPU-MJPEG format reporting and stop-failure retention checks remain registered through `XUnit.MjpegPipelineContractsTests`.
Notes for future agents: keep Flashback dispatcher routing/failure assertions in `AutomationCommandDispatcher.CommandOwnership.Tests.cs`; keep UnifiedVideoCapture CPU-MJPEG runtime behavior checks in `RecordingQueue.CaptureFanout.Tests.cs` with frame-ingress, fanout, and backend aggregate ownership coverage.

Date: 2026-05-26
Area: named-pipe automation test locality
Problem: `NamedPipeAutomationServer.Tests.cs` and `NamedPipeAutomationServer.Security.Tests.cs` split one pipe-hosting contract across two legacy `Program` partial shards. Request timeout/framing checks, app-surface auth wiring, documentation coverage, Windows explicit-security fallback policy, and the shared throwing-proxy helper all protect the same `NamedPipeAutomationServer.cs` owner and xUnit automation app-surface contract.
Files consolidated: `tests/Sussudio.Tests/NamedPipeAutomationServer.Security.Tests.cs`
Files added: none
Net production .cs delta: 0; net test .cs delta: -1
Partial clusters reduced: legacy `Program` named-pipe automation test partial file count -1
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore` (884 passed); `dotnet exec --% tests\Sussudio.Tests\bin\Debug\net8.0\Sussudio.Tests.dll Sussudio/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/Sussudio.dll`; regenerated `docs/architecture/Sussudio-Defragmentation-Baseline.generated.md`; `git diff --check`
CLI/MCP/pipe checks, if applicable: n/a; test-only consolidation, no public automation command names, IDs, wire payloads, XAML bindings, or runtime behavior changed
Behavior preserved: named-pipe request timeout, explicit-security fallback, app automation-host auth fallback wiring, Stream Deck auth-envelope documentation, and shared throwing-proxy helper coverage remain registered through `XUnit.AutomationContractsTests`.
Notes for future agents: keep named-pipe automation server framing, timeout, security fallback, and app auth wiring checks together in `NamedPipeAutomationServer.Tests.cs`; do not recreate a separate security shard unless the production server grows a genuinely separate named collaborator.

Date: 2026-05-26
Area: MainWindow shell native bootstrap test locality
Problem: `MainWindow.ShellOwnership.NativeBootstrap.Tests.cs` was a one-method legacy `Program` partial shard, while xUnit already grouped the native bootstrap assertion with the MainWindow window-lifecycle contract surface. The test reads the same `MainWindow.ShellChrome.Composition.cs` adapter, `NativeWindowBootstrapController`, first-frame reveal hooks, close lifecycle registration order, and window lifecycle docs as the existing shell window-lifecycle owner.
Files consolidated: `tests/Sussudio.Tests/MainWindow.ShellOwnership.NativeBootstrap.Tests.cs`
Files added: none
Net production .cs delta: 0; net test .cs delta: -1
Partial clusters reduced: legacy `Program` MainWindow shell/window lifecycle test partial file count -1
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore` (884 passed); `dotnet exec --% tests\Sussudio.Tests\bin\Debug\net8.0\Sussudio.Tests.dll Sussudio/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/Sussudio.dll`; regenerated `docs/architecture/Sussudio-Defragmentation-Baseline.generated.md`; `git diff --check`
CLI/MCP/pipe checks, if applicable: n/a; test-only consolidation, no public automation command names, IDs, wire payloads, XAML bindings, or runtime behavior changed
Behavior preserved: MainWindow native bootstrap, DWM cloak/dark-mode setup, first-composed-frame reveal, shell adapter ordering, close lifecycle registration, and window lifecycle assertions remain registered through `XUnit.PresentationPreviewMainWindowContractsTests`.
Notes for future agents: keep MainWindow native bootstrap and close/shutdown lifecycle ownership checks together in `MainWindow.ShellOwnership.WindowLifecycle.Tests.cs`; keep launch entrance and loaded-time startup checks in `MainWindow.ShellOwnership.Startup.Launch.Tests.cs`.

Date: 2026-05-26
Area: CaptureService recording lifecycle test locality
Problem: `CaptureService.RecordingOutcomeOwnership.Tests.cs` split recording start rollback and recording outcome-state assertions away from `CaptureService.RecordingLifecycleOwnership.Tests.cs`, even though the production ownership surface is the same recording lifecycle family: start rollback, outcome state in `CaptureService.RecordingLifecycle.cs`, backend finalization call sites, and active recording backend resources.
Files consolidated: `tests/Sussudio.Tests/CaptureService.RecordingOutcomeOwnership.Tests.cs`
Files added: none
Net production .cs delta: 0; net test .cs delta: -1
Partial clusters reduced: legacy `Program` CaptureService recording lifecycle/outcome test partial file count -1
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore` (884 passed); `dotnet exec --% tests\Sussudio.Tests\bin\Debug\net8.0\Sussudio.Tests.dll Sussudio/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/Sussudio.dll`; regenerated `docs/architecture/Sussudio-Defragmentation-Baseline.generated.md`; `git diff --check`
CLI/MCP/pipe checks, if applicable: n/a; test-only consolidation, no public automation command names, IDs, wire payloads, XAML bindings, or runtime behavior changed
Behavior preserved: CaptureService recording lifecycle, backend resource aggregate, stop-finalization failure propagation, recording start rollback, and recording outcome-state ownership checks remain registered through `XUnit.RecordingPipelineContractsTests`.
Notes for future agents: keep recording lifecycle, rollback, and outcome-state ownership checks in `CaptureService.RecordingLifecycleOwnership.Tests.cs`; keep Flashback-specific recording orchestration checks in the Flashback orchestration/source owner files.

Date: 2026-05-26
Area: Flashback exporter request failure test locality
Problem: `Flashback.Exporter.Cancellation.Tests.cs` split cancellation precedence and cancelled lock-wait assertions away from `Flashback.Exporter.Basic.Tests.cs`, even though both protect the same Flashback exporter top-level request/failure surface before native export work: request validation, missing/invalid inputs, empty segment lists, cancellation precedence, and export throttle policy.
Files consolidated: `tests/Sussudio.Tests/Flashback.Exporter.Cancellation.Tests.cs`
Files added: none
Net production .cs delta: 0; net test .cs delta: -1
Partial clusters reduced: legacy `Program` Flashback exporter request/failure test partial file count -1
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore` (884 passed); `dotnet exec --% tests\Sussudio.Tests\bin\Debug\net8.0\Sussudio.Tests.dll Sussudio/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/Sussudio.dll`; regenerated `docs/architecture/Sussudio-Defragmentation-Baseline.generated.md`; `git diff --check`
CLI/MCP/pipe checks, if applicable: n/a; test-only consolidation, no public automation command names, IDs, wire payloads, XAML bindings, or runtime behavior changed
Behavior preserved: Flashback exporter request validation, missing input/output failure results, null request failure, empty segment failure, cancellation precedence, cancelled lock-wait behavior, and export throttle assertions remain registered through `XUnit.FlashbackContractsTests`.
Notes for future agents: keep top-level Flashback exporter request validation and cancellation-precedence checks in `Flashback.Exporter.Basic.Tests.cs`; keep temp-output replacement and orphan cleanup checks in `Flashback.Exporter.OutputFinalization.Tests.cs`.

Date: 2026-05-26
Area: MainWindow layout policy test locality
Problem: `WindowSnapRegionLayoutPolicy.Tests.cs` was a small standalone xUnit policy island even though snap-region bounds are part of the MainWindow window layout/controller surface. The existing `MainWindow.ControllerOwnership.Layout.Tests.cs` owner already protects responsive layout controller adapters, breakpoints, and layout policy behavior, so reviewing layout policy required one extra tiny file.
Files consolidated: `tests/Sussudio.Tests/WindowSnapRegionLayoutPolicy.Tests.cs`
Files added: none
Net production .cs delta: 0; net test .cs delta: -1
Partial clusters reduced: xUnit MainWindow layout policy file count -1
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore` (884 passed); `dotnet exec --% tests\Sussudio.Tests\bin\Debug\net8.0\Sussudio.Tests.dll Sussudio/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/Sussudio.dll`; regenerated `docs/architecture/Sussudio-Defragmentation-Baseline.generated.md`; `git diff --check`
CLI/MCP/pipe checks, if applicable: n/a; test-only consolidation, no public automation command names, IDs, wire payloads, XAML bindings, or runtime behavior changed
Behavior preserved: responsive shell layout ownership, breakpoint/placement policy, and snap-region rectangle policy checks remain discovered through xUnit and documented under the MainWindow layout owner.
Notes for future agents: keep MainWindow responsive layout and snap-region policy checks in `MainWindow.ControllerOwnership.Layout.Tests.cs`; do not recreate a standalone snap-region policy test file unless the production policy becomes a separate named collaborator.

Date: 2026-05-26
Area: MainWindow capture selection normalizer test locality
Problem: `MainWindow.ControllerOwnership.Capture.SelectionNormalizer.Tests.cs` was a small standalone legacy `Program` partial shard for the helper that already lives inside `CaptureSelectionBindingController.cs`, while `MainWindow.ControllerOwnership.Capture.SelectionBindings.Tests.cs` already owns the selection binding controller shell, selection helper placement, and selection owner assertions. Reviewing capture ComboBox fallback behavior required one extra tiny file beside the real parent test owner.
Files consolidated: `tests/Sussudio.Tests/MainWindow.ControllerOwnership.Capture.SelectionNormalizer.Tests.cs`
Files added: none
Net production .cs delta: 0; net test .cs delta: -1
Partial clusters reduced: legacy `Program` MainWindow capture selection binding test partial file count -1
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore` (884 passed); `dotnet exec --% tests\Sussudio.Tests\bin\Debug\net8.0\Sussudio.Tests.dll Sussudio/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/Sussudio.dll`; regenerated `docs/architecture/Sussudio-Defragmentation-Baseline.generated.md`; `git diff --check`; `git diff --cached --check`
CLI/MCP/pipe checks, if applicable: n/a; test-only consolidation, no public automation command names, IDs, wire payloads, XAML bindings, or runtime behavior changed
Behavior preserved: capture/audio device live-instance matching, resolution exact/fallback behavior, frame-rate auto/exact/fallback behavior, and string ComboBox fallback behavior remain registered through `XUnit.PresentationPreviewMainWindowContractsTests`.
Notes for future agents: keep capture selection binding controller ownership and pure `CaptureComboBoxSelectionNormalizer` fallback-policy checks in `MainWindow.ControllerOwnership.Capture.SelectionBindings.Tests.cs`; do not recreate a standalone selection normalizer test shard unless the production helper becomes a separate named collaborator.

Date: 2026-05-26
Area: MainViewModel audio control test locality
Problem: `MainViewModel.NativeXuAudioControlService.AudioMeters.Tests.cs` mixed two different ownership surfaces in one small legacy `Program` partial shard: native XU audio-control service cohesion and MainViewModel audio meter callback state. The service cohesion checks belong with device-audio control ownership, while meter callback assertions belong with preview audio monitoring and audio-state ownership.
Files consolidated: `tests/Sussudio.Tests/MainViewModel.NativeXuAudioControlService.AudioMeters.Tests.cs`
Files added: none
Net production .cs delta: 0; net test .cs delta: -1
Partial clusters reduced: legacy `Program` MainViewModel audio-control test partial file count -1
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore` (884 passed); `dotnet exec --% tests\Sussudio.Tests\bin\Debug\net8.0\Sussudio.Tests.dll Sussudio/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/Sussudio.dll`; regenerated `docs/architecture/Sussudio-Defragmentation-Baseline.generated.md`; `git diff --check`; `git diff --cached --check`
CLI/MCP/pipe checks, if applicable: n/a; test-only consolidation, no public automation command names, IDs, wire payloads, XAML bindings, or runtime behavior changed
Behavior preserved: native XU audio-control service cohesion/profile/payload/raw transport assertions remain registered through `XUnit.PresentationPreviewMainViewModelContractsTests`; audio meter callback-state assertions remain registered through the same xUnit surface.
Notes for future agents: keep native XU audio-control service cohesion checks in `MainViewModel.AudioControls.DeviceAudio.Tests.cs`; keep audio meter callback-state checks in `MainViewModel.AudioControls.GainAndMonitoring.Tests.cs`.

Date: 2026-05-26
Area: LibAv recording sink test locality
Problem: `RecordingQueue.LibAvSink.Queue.Tests.cs` and `RecordingQueue.LibAvSink.Lifecycle.Tests.cs` split the LibAv sink review surface across two legacy `Program` partial shards. Queue admission, audio/video queue cleanup, nonblocking enqueue behavior, drain ordering, output validation, startup, stop, and lifetime helpers are all part of the same LibAv recording sink ownership cluster.
Files consolidated: `tests/Sussudio.Tests/RecordingQueue.LibAvSink.Queue.Tests.cs`
Files added: none
Net production .cs delta: 0; net test .cs delta: -1
Partial clusters reduced: legacy `Program` LibAv recording sink test partial file count -1
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore` (884 passed); `dotnet exec --% tests\Sussudio.Tests\bin\Debug\net8.0\Sussudio.Tests.dll Sussudio/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/Sussudio.dll`; regenerated `docs/architecture/Sussudio-Defragmentation-Baseline.generated.md`; `git diff --check`; `git diff --cached --check`
CLI/MCP/pipe checks, if applicable: n/a; test-only consolidation, no public automation command names, IDs, wire payloads, XAML bindings, or runtime behavior changed
Behavior preserved: LibAv sink nonblocking video/GPU/CUDA enqueue checks, audio queue ownership, video queue submission ownership, output validation, bounded drain loop, encoding-loop, startup, stop-lifecycle, and lifetime-helper assertions remain registered through `XUnit.RecordingPipelineContractsTests` and `XUnit.RecordingModelContractsTests`.
Notes for future agents: keep LibAv recording sink queue and lifecycle ownership checks together in `RecordingQueue.LibAvSink.Lifecycle.Tests.cs`; do not recreate a separate queue test shard unless production queue behavior becomes a separately named collaborator with its own test seam.

Date: 2026-05-26
Area: PresentMon probe test locality
Problem: `PresentMonProbe.SourceOwnership.Tests.cs` split PresentMonProbe source-family ownership assertions away from `PresentMonProbe.Tests.cs`, even though both protect the same `tools/Common/PresentMon/PresentMonProbe*.cs` review surface: run/process setup, result formatting, DTO ownership, CSV parsing, swap-chain selection, artifact filtering, and app-present correlation.
Files consolidated: `tests/Sussudio.Tests/PresentMonProbe.SourceOwnership.Tests.cs`
Files added: none
Net production .cs delta: 0; net test .cs delta: -1
Partial clusters reduced: legacy `Program` PresentMon probe test partial file count -1
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore` (884 passed); `dotnet exec --% tests\Sussudio.Tests\bin\Debug\net8.0\Sussudio.Tests.dll Sussudio/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/Sussudio.dll`; regenerated `docs/architecture/Sussudio-Defragmentation-Baseline.generated.md`; `git diff --check`; `git diff --cached --check`
CLI/MCP/pipe checks, if applicable: n/a; test-only consolidation, no public automation command names, IDs, wire payloads, XAML bindings, or runtime behavior changed
Behavior preserved: PresentMon parser behavior and source-family ownership checks remain registered through `XUnit.ToolContractsTests`.
Notes for future agents: keep PresentMon parser behavior and source ownership checks together in `PresentMonProbe.Tests.cs`; do not recreate a standalone PresentMon source-ownership shard unless the production probe gains a separately named collaborator with its own test seam.

Date: 2026-05-26
Area: MJPEG preview jitter test locality
Problem: `PooledVideoFrame.MjpegJitterPolicy.Tests.cs` split MJPEG preview jitter source-ownership and adaptive deadline policy assertions away from `PooledVideoFrame.MjpegJitterQueue.Tests.cs`, even though both protect the same `MjpegPreviewJitterBuffer` review surface: frame ingress, queue ordering, deadline drops, adaptive target depth, reprime behavior, emit-loop placement, frame pacing, and metrics.
Files consolidated: `tests/Sussudio.Tests/PooledVideoFrame.MjpegJitterPolicy.Tests.cs`
Files added: none
Net production .cs delta: 0; net test .cs delta: -1
Partial clusters reduced: legacy `Program` MJPEG preview jitter test partial file count -1
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore` (884 passed); `dotnet exec --% tests\Sussudio.Tests\bin\Debug\net8.0\Sussudio.Tests.dll Sussudio/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/Sussudio.dll`; regenerated `docs/architecture/Sussudio-Defragmentation-Baseline.generated.md`; `git diff --check`; `git diff --cached --check`
CLI/MCP/pipe checks, if applicable: n/a; test-only consolidation, no public automation command names, IDs, wire payloads, XAML bindings, or runtime behavior changed
Behavior preserved: MJPEG preview jitter adaptive deadline/source-ownership checks and queue/drop/reprime behavior checks remain registered through `XUnit.MjpegPipelineContractsTests`.
Notes for future agents: keep MJPEG preview jitter source ownership, adaptive policy, queue/drop, and reprime checks together in `PooledVideoFrame.MjpegJitterQueue.Tests.cs`; keep shared pooled-frame reflection/factory helpers in `PooledVideoFrame.Tests.cs`.

Date: 2026-05-26
Area: NativeXu RTK probe test locality
Problem: `RtkI2cProbe.Tests.cs` split NativeXuAudioProbe RTK unsafe-path behavior checks away from `ServiceNamespace.NativeXuProbe.Tests.cs`, even though that owner already protects NativeXuAudioProbe routing, RTK command wiring, linked source layout, and `RtkI2cProbe.cs` source guard text.
Files consolidated: `tests/Sussudio.Tests/RtkI2cProbe.Tests.cs`
Files added: none
Net production .cs delta: 0; net test .cs delta: -1
Partial clusters reduced: legacy `Program` NativeXu probe/RTK test partial file count -1
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore` (884 passed); `dotnet exec --% tests\Sussudio.Tests\bin\Debug\net8.0\Sussudio.Tests.dll Sussudio/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/Sussudio.dll`; regenerated `docs/architecture/Sussudio-Defragmentation-Baseline.generated.md`; `git diff --check`; `git diff --cached --check`
CLI/MCP/pipe checks, if applicable: n/a; test-only consolidation, no public automation command names, IDs, wire payloads, XAML bindings, or runtime behavior changed
Behavior preserved: RTK missing native-XU-path rejection, disabled switch rejection, and RTK device-name normalization remain registered through `XUnit.ToolContractsTests`.
Notes for future agents: keep NativeXuAudioProbe routing/source ownership and RTK unsafe-path behavior checks in `ServiceNamespace.NativeXuProbe.Tests.cs`; do not recreate a standalone RTK test shard unless the RTK probe gains an independent fixture or broader behavior surface.

Date: 2026-05-26
Area: architecture-doc helper test locality
Problem: `ArchitectureDocs.OwnershipFileEnumerators.cs` split private ownership-file enumeration and exact code-span helper logic away from `ArchitectureDocs.MarkdownReferenceHelpers.cs`, even though both files formed one helper boundary for architecture-doc path/reference assertions. Reviewing architecture-doc validation helper behavior required opening two small private-helper shards.
Files consolidated: `tests/Sussudio.Tests/ArchitectureDocs.OwnershipFileEnumerators.cs`
Files added: none
Net production .cs delta: 0; net test .cs delta: -1
Partial clusters reduced: legacy `Program` architecture-doc helper partial file count -1
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore` (884 passed); `dotnet exec --% tests\Sussudio.Tests\bin\Debug\net8.0\Sussudio.Tests.dll Sussudio/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/Sussudio.dll`; regenerated `docs/architecture/Sussudio-Defragmentation-Baseline.generated.md`; `git diff --check`; `git diff --cached --check`
CLI/MCP/pipe checks, if applicable: n/a; test/docs-only consolidation, no public automation command names, IDs, wire payloads, XAML bindings, or runtime behavior changed
Behavior preserved: AGENT_MAP file/path coverage, cleanup-plan coverage, xUnit inventory discovery, markdown token resolution, and UI/capture/tool ownership enumerators remain registered through `XUnit.ArchitectureDocsAgentMapOwnershipTests`.
Notes for future agents: keep architecture-doc markdown/path helper methods and ownership-file enumerators together in `ArchitectureDocs.MarkdownReferenceHelpers.cs`; create a separate helper file only for an independently named docs-validation support boundary.

Date: 2026-05-26
Area: Flashback test locality
Problem: `Flashback.Buffer.Segments.DisposalRecovery.Tests.cs` split disposed-state and recovery-preserve segment safety checks away from the same Flashback buffer segment validation owner. `Flashback.Exporter.FailureClassifier.Tests.cs` also split top-level Flashback export failure mapping away from the request/failure surface already covered by `Flashback.Exporter.Basic.Tests.cs`.
Files consolidated: `tests/Sussudio.Tests/Flashback.Buffer.Segments.DisposalRecovery.Tests.cs`; `tests/Sussudio.Tests/Flashback.Exporter.FailureClassifier.Tests.cs`
Files added: none
Net production .cs delta: 0; net test .cs delta: -2
Partial clusters reduced: legacy `Program` Flashback buffer/exporter test partial file count -2
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore` (884 passed); `dotnet exec --% tests\Sussudio.Tests\bin\Debug\net8.0\Sussudio.Tests.dll Sussudio/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/Sussudio.dll`; regenerated `docs/architecture/Sussudio-Defragmentation-Baseline.generated.md`; `git diff --check`; `git diff --cached --check`
CLI/MCP/pipe checks, if applicable: n/a; test/docs-only consolidation, no public automation command names, IDs, wire payloads, XAML bindings, or runtime behavior changed
Behavior preserved: Flashback buffer segment metadata, outside-path, disposed no-op, and recovery-preserve tests remain registered through `XUnit.RecordingModelContractsTests` and `XUnit.PresentationPreviewStartupContractsTests`; Flashback export failure-classifier mapping remains registered through `XUnit.RecordingPipelineContractsTests`.
Notes for future agents: keep Flashback buffer segment safety checks in `Flashback.Buffer.Segments.Validation.Tests.cs`; keep top-level Flashback exporter request/failure classification checks in `Flashback.Exporter.Basic.Tests.cs` unless either surface gains a separate named collaborator with its own test seam.

Date: 2026-05-26
Area: test harness core locality
Problem: `HarnessCore.Assertions.cs`, `HarnessCore.AsyncLifecycle.cs`, `HarnessCore.ObjectFactories.cs`, `HarnessCore.Reflection.cs`, and `HarnessCore.SourceText.cs` split the private legacy `Program` harness primitives into five small helper shards. Understanding or updating shared assertions, repo/source readers, reflection fixtures, object factories, async disposal, and polling waits required opening multiple sub-80-line support files even though they are one harness support boundary.
Files consolidated: `tests/Sussudio.Tests/HarnessCore.Assertions.cs`; `tests/Sussudio.Tests/HarnessCore.AsyncLifecycle.cs`; `tests/Sussudio.Tests/HarnessCore.ObjectFactories.cs`; `tests/Sussudio.Tests/HarnessCore.Reflection.cs`; `tests/Sussudio.Tests/HarnessCore.SourceText.cs`
Files added: `tests/Sussudio.Tests/HarnessCore.cs` (renamed from `HarnessCore.Reflection.cs`)
Net production .cs delta: 0; net test .cs delta: -4
Partial clusters reduced: legacy `Program` shared harness helper partial file count -4
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore` (884 passed); `dotnet exec --% tests\Sussudio.Tests\bin\Debug\net8.0\Sussudio.Tests.dll Sussudio/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/Sussudio.dll`; regenerated `docs/architecture/Sussudio-Defragmentation-Baseline.generated.md`; `git diff --check`
CLI/MCP/pipe checks, if applicable: n/a; test/docs-only consolidation, no public automation command names, IDs, wire payloads, XAML bindings, or runtime behavior changed
Behavior preserved: generic harness assertions, source text readers, reflection/private-field helpers, synthetic capture/settings/recording fixtures, capture-service initialization, async disposal, polling waits, and field-value fixtures remain in the same private `Program` harness surface.
Notes for future agents: keep shared harness primitives in `HarnessCore.cs`; create a separate harness support file only for an independently named fixture family with enough behavior to justify its own owner.

Date: 2026-05-26
Area: MCP tool-surface helper locality
Problem: `McpToolSurface.Helpers.Process.cs`, `McpToolSurface.Helpers.Reflection.cs`, `McpToolSurface.Helpers.PipeCapture.cs`, and `McpToolSurface.Helpers.Assertions.cs` split one private MCP test-support boundary across process/JSON-RPC, reflection/tool-result, pipe-capture, and JSON assertion helper shards. Updating MCP tool-surface tests required opening multiple small helper files even though the helpers are only meaningful together as the MCP harness support surface.
Files consolidated: `tests/Sussudio.Tests/McpToolSurface.Helpers.Assertions.cs`; `tests/Sussudio.Tests/McpToolSurface.Helpers.Process.cs`; `tests/Sussudio.Tests/McpToolSurface.Helpers.Reflection.cs`; `tests/Sussudio.Tests/McpToolSurface.Helpers.PipeCapture.cs`
Files added: `tests/Sussudio.Tests/McpToolSurface.Helpers.cs` (renamed from `McpToolSurface.Helpers.PipeCapture.cs`)
Net production .cs delta: 0; net test .cs delta: -3
Partial clusters reduced: legacy `Program` MCP helper partial file count -3
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore` (884 passed); `dotnet exec --% tests\Sussudio.Tests\bin\Debug\net8.0\Sussudio.Tests.dll Sussudio/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/Sussudio.dll`; regenerated `docs/architecture/Sussudio-Defragmentation-Baseline.generated.md`; `git diff --check`
CLI/MCP/pipe checks, if applicable: full solution build rebuilt `tools/McpServer`; this is a test-helper/docs-only consolidation and does not change public automation command names, IDs, wire payloads, or MCP tool implementations.
Behavior preserved: MCP process startup/teardown, JSON-RPC line exchange, MCP tool reflection invocation, tool-result text/error extraction, formatter batch invocation, pipe request capture, and JSON command assertion helpers remain in the same private `Program` harness surface.
Notes for future agents: keep shared MCP tool-surface support helpers in `McpToolSurface.Helpers.cs`; create a separate MCP helper file only when a fixture family becomes independently named and reviewable apart from the MCP test harness.

Date: 2026-05-26
Area: diagnostics refresh source-helper locality
Problem: `MainViewModel.Automation.DiagnosticsRefresh.SourceReaders.cs` split reusable diagnostics refresh source/fixture readers away from `MainViewModel.Automation.DiagnosticsRefresh.SourceFamily.cs`, even though both files existed to assemble source text used by diagnostics refresh ownership assertions. Understanding the diagnostics refresh source fixture surface required opening two helper-only partial shards.
Files consolidated: `tests/Sussudio.Tests/MainViewModel.Automation.DiagnosticsRefresh.SourceReaders.cs`
Files added: none
Net production .cs delta: 0; net test .cs delta: -1
Partial clusters reduced: legacy `Program` diagnostics refresh source-helper partial file count -1
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore` (884 passed); `dotnet exec --% tests\Sussudio.Tests\bin\Debug\net8.0\Sussudio.Tests.dll Sussudio/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/Sussudio.dll`; regenerated `docs/architecture/Sussudio-Defragmentation-Baseline.generated.md`; `git diff --check`
CLI/MCP/pipe checks, if applicable: n/a; test/docs-only consolidation, no public automation command names, IDs, wire payloads, XAML bindings, or runtime behavior changed
Behavior preserved: diagnostics hub, capture service, MF source reader, diagnostic-session, and diagnostic-session tool-surface source readers still feed the same refresh ownership assertions.
Notes for future agents: keep diagnostics refresh source-family and source-reader helpers together in `MainViewModel.Automation.DiagnosticsRefresh.SourceFamily.cs`; only extract a new helper file for executable assertions or a source family reused outside diagnostics-refresh ownership tests.

Date: 2026-05-26
Area: Flashback decoder test locality
Problem: `Flashback.Support.Tests.cs` contained a single Flashback decoder support/logging and D3D11VA discovery source-shape contract, separate from the Flashback decoder test owner that already covers decoder lifecycle, validation, audio/video output, D3D11VA setup, and source-shape assertions. Reviewing decoder setup/logging behavior required opening an extra tiny test shard.
Files consolidated: `tests/Sussudio.Tests/Flashback.Support.Tests.cs`
Files added: none
Net production .cs delta: 0; net test .cs delta: -1
Partial clusters reduced: legacy `Program` Flashback decoder/support test partial file count -1
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore` (884 passed); `dotnet exec --% tests\Sussudio.Tests\bin\Debug\net8.0\Sussudio.Tests.dll Sussudio/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/Sussudio.dll`; regenerated `docs/architecture/Sussudio-Defragmentation-Baseline.generated.md`; `git diff --check`
CLI/MCP/pipe checks, if applicable: n/a; test/docs-only consolidation, no public automation command names, IDs, wire payloads, XAML bindings, or runtime behavior changed
Behavior preserved: Flashback decoder open/close warning logs, D3D11VA discovery/setup fallback logs, and removed legacy D3D11 shard guards remain registered through `XUnit.FlashbackContractsTests`.
Notes for future agents: keep Flashback decoder support/logging and D3D11VA source-shape contracts with `Flashback.Decoder.Tests.cs`; keep `Flashback.Tests.cs` as shared source-reader/helper support only.

Date: 2026-05-26
Area: diagnostics refresh Flashback alert test locality
Problem: `MainViewModel.Automation.DiagnosticsRefresh.FlashbackAlerts.RecordingAndStorage.Tests.cs` and `MainViewModel.Automation.DiagnosticsRefresh.FlashbackAlerts.PlaybackAndPreview.Tests.cs` split one diagnostics-refresh Flashback alert assertion surface by topic even though both private helpers are invoked by the same diagnostics-refresh entry point and read the same diagnostics/counter source families.
Files consolidated: `tests/Sussudio.Tests/MainViewModel.Automation.DiagnosticsRefresh.FlashbackAlerts.RecordingAndStorage.Tests.cs`; `tests/Sussudio.Tests/MainViewModel.Automation.DiagnosticsRefresh.FlashbackAlerts.PlaybackAndPreview.Tests.cs`
Files added: `tests/Sussudio.Tests/MainViewModel.Automation.DiagnosticsRefresh.FlashbackAlerts.Tests.cs` (renamed from `FlashbackAlerts.PlaybackAndPreview.Tests.cs`)
Net production .cs delta: 0; net test .cs delta: -1
Partial clusters reduced: legacy `Program` diagnostics refresh Flashback alert helper partial file count -1
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore` (884 passed); `dotnet exec --% tests\Sussudio.Tests\bin\Debug\net8.0\Sussudio.Tests.dll Sussudio/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/Sussudio.dll`; regenerated `docs/architecture/Sussudio-Defragmentation-Baseline.generated.md`; `git diff --check`
CLI/MCP/pipe checks, if applicable: n/a; test/docs-only consolidation, no public automation command names, IDs, wire payloads, XAML bindings, or runtime behavior changed
Behavior preserved: Flashback export/storage/recording/force-rotate and playback/preview/MJPEG/renderer alert source-shape assertions remain invoked by `DiagnosticsSnapshotRefresh_IsSerializedForRecordingResponses`.
Notes for future agents: keep diagnostics-refresh Flashback alert helper assertions together in `MainViewModel.Automation.DiagnosticsRefresh.FlashbackAlerts.Tests.cs` unless a future slice introduces an executable alert fixture with independent setup.

Date: 2026-05-26
Area: diagnostics refresh evaluation test locality
Problem: `MainViewModel.Automation.DiagnosticsRefresh.EvaluationOwnership.Tests.cs` contained one private helper assertion invoked only by the central diagnostics-refresh ownership test, so reviewing refresh evaluation-policy and lane ownership required opening an extra helper-only shard before returning to the primary diagnostics-refresh owner.
Files consolidated: `tests/Sussudio.Tests/MainViewModel.Automation.DiagnosticsRefresh.EvaluationOwnership.Tests.cs`
Files added: none
Net production .cs delta: 0; net test .cs delta: -1
Partial clusters reduced: legacy `Program` diagnostics refresh ownership helper partial file count -1
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore` (884 passed); `dotnet exec --% tests\Sussudio.Tests\bin\Debug\net8.0\Sussudio.Tests.dll Sussudio/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/Sussudio.dll`; regenerated `docs/architecture/Sussudio-Defragmentation-Baseline.generated.md`
CLI/MCP/pipe checks, if applicable: n/a; test/docs-only consolidation, no public automation command names, IDs, wire payloads, XAML bindings, or runtime behavior changed
Behavior preserved: diagnostics refresh evaluation-policy, diagnostic evaluation, realtime lane, and Flashback lane source-shape assertions remain invoked by `DiagnosticsSnapshotRefresh_IsSerializedForRecordingResponses`.
Notes for future agents: keep diagnostics-refresh evaluation ownership assertions with `MainViewModel.Automation.DiagnosticsRefresh.Tests.cs` while they remain private helper assertions for the central refresh ownership entry point.

Date: 2026-05-26
Area: diagnostic-session Flashback test locality
Problem: `MainViewModel.Automation.DiagnosticsRefresh.DiagnosticSessionPlayback.Tests.cs` contained one private Flashback playback metrics/result assertion invoked only by the central diagnostics-refresh ownership test, while `MainViewModel.Automation.DiagnosticsRefresh.DiagnosticSessionScenarios.Tests.cs` owned adjacent diagnostic-session Flashback scenario, stress, health-policy, and warning-tolerance source-shape assertions.
Files consolidated: `tests/Sussudio.Tests/MainViewModel.Automation.DiagnosticsRefresh.DiagnosticSessionPlayback.Tests.cs`
Files added: none
Net production .cs delta: 0; net test .cs delta: -1
Partial clusters reduced: legacy `Program` diagnostics refresh diagnostic-session helper partial file count -1
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore` (884 passed); `dotnet exec --% tests\Sussudio.Tests\bin\Debug\net8.0\Sussudio.Tests.dll Sussudio/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/Sussudio.dll`; regenerated `docs/architecture/Sussudio-Defragmentation-Baseline.generated.md`
CLI/MCP/pipe checks, if applicable: n/a; test/docs-only consolidation, no public automation command names, IDs, wire payloads, XAML bindings, or runtime behavior changed
Behavior preserved: diagnostic-session Flashback playback metrics/result, scenario, stress, health-policy, and warning-tolerance source-shape assertions remain invoked by `DiagnosticsSnapshotRefresh_IsSerializedForRecordingResponses`.
Notes for future agents: keep diagnostic-session Flashback playback metrics and scenario ownership assertions together in `MainViewModel.Automation.DiagnosticsRefresh.DiagnosticSessionScenarios.Tests.cs` while they remain private source-shape checks for the central diagnostics-refresh entry point.

Date: 2026-05-26
Area: MCP diagnostic-session Flashback scenario test locality
Problem: `McpToolSurface.DiagnosticSession.Flashback.Stress.Tests.cs` split Flashback stress scenario flow and audio-master fallback classification checks away from `McpToolSurface.DiagnosticSession.Flashback.Scenarios.Tests.cs`, even though both files protect the MCP diagnostic-session Flashback scenario review surface and are registered through the same xUnit diagnostic-session contract group.
Files consolidated: `tests/Sussudio.Tests/McpToolSurface.DiagnosticSession.Flashback.Stress.Tests.cs`
Files added: none
Net production .cs delta: 0; net test .cs delta: -1
Partial clusters reduced: legacy `Program` MCP diagnostic-session Flashback scenario partial file count -1
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore` (884 passed); `dotnet exec --% tests\Sussudio.Tests\bin\Debug\net8.0\Sussudio.Tests.dll Sussudio/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/Sussudio.dll`; regenerated `docs/architecture/Sussudio-Defragmentation-Baseline.generated.md`
CLI/MCP/pipe checks, if applicable: full solution build rebuilds `tools/McpServer` and `tools/ssctl`; test/docs-only consolidation, no public automation command names, IDs, wire payloads, or MCP tool implementations changed
Behavior preserved: Flashback stress/scrub-stress ownership assertions and audio-master fallback classifier checks remain registered through `XUnit.McpDiagnosticSessionContractsTests`.
Notes for future agents: keep Flashback stress scenario ownership checks with `McpToolSurface.DiagnosticSession.Flashback.Scenarios.Tests.cs`; keep metric projection checks in their focused sibling file unless that surface becomes helper-only.

Date: 2026-05-26
Area: MCP diagnostic-session Flashback export test locality
Problem: `McpToolSurface.DiagnosticSession.Flashback.Export.Tests.cs` split Flashback export scenario flow, export-helper, and segment wait/parsing ownership checks away from the primary MCP Flashback scenario test owner, even though those checks protect the same diagnostic-session Flashback scenario review surface and are registered through the same xUnit diagnostic-session contract group.
Files consolidated: `tests/Sussudio.Tests/McpToolSurface.DiagnosticSession.Flashback.Export.Tests.cs`
Files added: none
Net production .cs delta: 0; net test .cs delta: -1
Partial clusters reduced: legacy `Program` MCP diagnostic-session Flashback scenario/export partial file count -1
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore` (884 passed); `dotnet exec --% tests\Sussudio.Tests\bin\Debug\net8.0\Sussudio.Tests.dll Sussudio/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/Sussudio.dll`; regenerated `docs/architecture/Sussudio-Defragmentation-Baseline.generated.md`
CLI/MCP/pipe checks, if applicable: full solution build rebuilds `tools/McpServer` and `tools/ssctl`; test/docs-only consolidation, no public automation command names, IDs, wire payloads, or MCP tool implementations changed
Behavior preserved: Flashback export scenario flow, export-helper, and segment wait/parsing ownership assertions remain registered through `XUnit.McpDiagnosticSessionContractsTests`.
Notes for future agents: keep MCP diagnostic-session Flashback scenario, stress, export, and segment-flow ownership checks together in `McpToolSurface.DiagnosticSession.Flashback.Scenarios.Tests.cs`; keep metric projection and health-policy checks in focused sibling files while they remain independently reviewable.

Date: 2026-05-26
Area: MCP diagnostic-session Flashback health-policy test locality
Problem: `McpToolSurface.DiagnosticSession.Flashback.HealthPolicy.Tests.cs` split Flashback warmup health-policy, warning-policy, and snapshot polling wait ownership checks away from the MCP Flashback scenario test owner, even though those checks are scenario-support invariants used by the same diagnostic-session Flashback review surface.
Files consolidated: `tests/Sussudio.Tests/McpToolSurface.DiagnosticSession.Flashback.HealthPolicy.Tests.cs`
Files added: none
Net production .cs delta: 0; net test .cs delta: -1
Partial clusters reduced: legacy `Program` MCP diagnostic-session Flashback scenario/support partial file count -1
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore` (884 passed); `dotnet exec --% tests\Sussudio.Tests\bin\Debug\net8.0\Sussudio.Tests.dll Sussudio/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/Sussudio.dll`; regenerated `docs/architecture/Sussudio-Defragmentation-Baseline.generated.md`
CLI/MCP/pipe checks, if applicable: full solution build rebuilds `tools/McpServer` and `tools/ssctl`; test/docs-only consolidation, no public automation command names, IDs, wire payloads, or MCP tool implementations changed
Behavior preserved: Flashback warmup health-policy, warning-policy, and snapshot polling wait ownership assertions remain registered through `XUnit.McpDiagnosticSessionContractsTests`.
Notes for future agents: keep MCP diagnostic-session Flashback scenario-support ownership checks with `McpToolSurface.DiagnosticSession.Flashback.Scenarios.Tests.cs`; keep metric projection checks in `McpToolSurface.DiagnosticSession.Flashback.Metrics.Tests.cs` while that projection surface remains independently reviewable.

Date: 2026-05-26
Area: MCP diagnostic-session result-builder test locality
Problem: `McpToolSurface.DiagnosticSession.ResultOwnership.Builder.PreviewAndCompletion.Tests.cs` split preview projection, analysis-warning, diagnostic-health, and artifact-handoff assertions away from `McpToolSurface.DiagnosticSession.ResultOwnership.Builder.Tests.cs`, even though the central result-builder ownership test already invokes those private helpers as part of summary-construction ownership.
Files consolidated: `tests/Sussudio.Tests/McpToolSurface.DiagnosticSession.ResultOwnership.Builder.PreviewAndCompletion.Tests.cs`
Files added: none
Net production .cs delta: 0; net test .cs delta: -1
Partial clusters reduced: legacy `Program` MCP diagnostic-session result-builder partial file count -1
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore` (884 passed); `dotnet exec --% tests\Sussudio.Tests\bin\Debug\net8.0\Sussudio.Tests.dll Sussudio/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/Sussudio.dll`; regenerated `docs/architecture/Sussudio-Defragmentation-Baseline.generated.md`
CLI/MCP/pipe checks, if applicable: full solution build rebuilds `tools/McpServer` and `tools/ssctl`; test/docs-only consolidation, no public automation command names, IDs, wire payloads, or MCP tool implementations changed
Behavior preserved: result-builder preview projection, analysis-warning, diagnostic-health, artifact-handoff, and summary-construction ownership assertions remain registered through `XUnit.McpDiagnosticSessionContractsTests`.
Notes for future agents: keep diagnostic-session result-builder ownership checks together in `McpToolSurface.DiagnosticSession.ResultOwnership.Builder.Tests.cs`; split again only if a production result-builder collaborator gets an independent executable behavior fixture.

Date: 2026-05-26
Area: MCP diagnostic-session lifecycle ownership test locality
Problem: `McpToolSurface.DiagnosticSession.Ownership.Planning.Tests.cs`, `McpToolSurface.DiagnosticSession.Ownership.Execution.Tests.cs`, and `McpToolSurface.DiagnosticSession.Ownership.TeardownAndReporting.Tests.cs` split one diagnostic-session helper ownership surface by lifecycle phase. Reviewing planning/setup, startup/sampling, teardown/reporting, post-run snapshots, recording verification, and shared session metrics required jumping across three legacy `Program` partial shards.
Files consolidated: `tests/Sussudio.Tests/McpToolSurface.DiagnosticSession.Ownership.Planning.Tests.cs`; `tests/Sussudio.Tests/McpToolSurface.DiagnosticSession.Ownership.Execution.Tests.cs`; `tests/Sussudio.Tests/McpToolSurface.DiagnosticSession.Ownership.TeardownAndReporting.Tests.cs`
Files added: `tests/Sussudio.Tests/McpToolSurface.DiagnosticSession.Ownership.Tests.cs` (renamed from `Ownership.Planning.Tests.cs`)
Net production .cs delta: 0; net test .cs delta: -2
Partial clusters reduced: legacy `Program` MCP diagnostic-session lifecycle ownership partial file count -2
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore` (884 passed); `dotnet exec --% tests\Sussudio.Tests\bin\Debug\net8.0\Sussudio.Tests.dll Sussudio/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/Sussudio.dll`; regenerated `docs/architecture/Sussudio-Defragmentation-Baseline.generated.md`
CLI/MCP/pipe checks, if applicable: full solution build rebuilds `tools/McpServer` and `tools/ssctl`; test/docs-only consolidation, no public automation command names, IDs, wire payloads, or MCP tool implementations changed
Behavior preserved: diagnostic-session planning/setup, background task draining, PresentMon startup, sample-loop ordering, cleanup restore warning, recording verification, post-run snapshot, and shared metrics ownership assertions remain registered through `XUnit.McpDiagnosticSessionContractsTests`.
Notes for future agents: keep diagnostic-session lifecycle helper ownership checks in `McpToolSurface.DiagnosticSession.Ownership.Tests.cs`; keep infrastructure, result ownership, Flashback scenario, and Flashback metric projection checks in their focused sibling files.

Date: 2026-05-26
Area: shell window automation production locality
Problem: `WindowAutomationHostLifecycleController.cs` was a small shell automation host lifecycle file separate from `WindowAutomationController.cs`, even though both own the shell window automation surface used by `IAutomationWindowControl`: geometry/recordings-folder commands on one side, named-pipe diagnostics/dispatcher startup and shutdown on the other. Reviewing app-shell automation startup, auth fallback, and window automation command ownership required jumping across two tiny production owners.
Files consolidated: `Sussudio/Controllers/Window/WindowAutomationHostLifecycleController.cs`
Files added: none
Net production .cs delta: -1; net test .cs delta: 0
Partial clusters reduced: shell window automation production owner count -1
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore` (884 passed); `dotnet exec --% tests\Sussudio.Tests\bin\Debug\net8.0\Sussudio.Tests.dll Sussudio/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/Sussudio.dll`; `git diff --check`; regenerated `docs/architecture/Sussudio-Defragmentation-Baseline.generated.md`
CLI/MCP/pipe checks, if applicable: full solution build rebuilds `tools/McpServer`, `tools/ssctl`, and automation tooling; no public automation command names, IDs, wire payloads, auth environment variables, pipe names, or XAML bindings changed
Behavior preserved: automation token/pipe-name resolution, diagnostics hub construction, command dispatcher construction, named-pipe server construction, once-only startup, ready/disabled logging, and pipe-before-hub shutdown disposal now live in `Sussudio/Controllers/Window/WindowAutomationController.cs` with the window automation command owner.
Notes for future agents: keep native DWM bootstrap and recording-aware close/finalization in their existing window owners; keep shell automation host lifecycle with `WindowAutomationController.cs` unless it grows an independently executable lifecycle policy.

Date: 2026-05-26
Area: launch flow production locality
Problem: `LaunchStartupController.cs` was a 54-line loaded-time startup owner split away from `LaunchEntranceAnimationController.cs`, even though startup immediately schedules shell reveal/device refresh, starts automation in the finally path, and triggers the splash/entrance sequence owned by the adjacent launch entrance controller. Reviewing first-load startup, no-preview fallback, automation start timing, and launch entrance choreography required two production files plus the same shell adapter/tests.
Files consolidated: `Sussudio/Controllers/Launch/LaunchStartupController.cs`; `Sussudio/Controllers/Launch/Entrance/LaunchEntranceAnimationController.cs`
Files added: `Sussudio/Controllers/Launch/LaunchFlowController.cs` (renamed from `LaunchEntranceAnimationController.cs`)
Net production .cs delta: -1; net test .cs delta: 0
Partial clusters reduced: launch flow production owner count -1; sub-80-line production file count -1
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore` (884 passed); `dotnet exec --% tests\Sussudio.Tests\bin\Debug\net8.0\Sussudio.Tests.dll Sussudio/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/Sussudio.dll`; `git diff --check`; regenerated `docs/architecture/Sussudio-Defragmentation-Baseline.generated.md`
CLI/MCP/pipe checks, if applicable: full solution build rebuilds `tools/McpServer`, `tools/ssctl`, and automation tooling; no public automation command names, IDs, wire payloads, XAML bindings, launch timing constants, or startup actions changed
Behavior preserved: native shell reveal scheduling, initial ViewModel settings load, preview audio fade priming before device refresh, no-preview fallback, automation host startup, splash trigger, loading phrase start/stop, splash fade, shell entrance choreography, deferred preview reveal logging, and control-bar shadow fade remain in the same order.
Notes for future agents: keep launch startup and entrance choreography together in `Sussudio/Controllers/Launch/LaunchFlowController.cs`; keep splash phrase text selection in `SplashLoadingPhraseController.cs` because it is a reusable phrase/timer leaf.

Date: 2026-05-26
Area: screenshot controller production locality
Problem: `PreviewScreenshotController.cs` was a 77-line preview-frame screenshot workflow split away from the whole-window screenshot controller, even though both are reviewed by the same screenshot ownership tests and docs and together define screenshot action/persistence policy. Reviewing screenshot behavior required opening separate `Preview` and `Window` subfolders for button workflow, output path/status text, automation whole-window capture, and image encoding.
Files consolidated: `Sussudio/Controllers/Screenshot/Preview/PreviewScreenshotController.cs`; `Sussudio/Controllers/Screenshot/Window/WindowScreenshotController.cs`
Files added: `Sussudio/Controllers/Screenshot/ScreenshotControllers.cs` (renamed from `WindowScreenshotController.cs`)
Net production .cs delta: -1; net test .cs delta: 0
Partial clusters reduced: screenshot production owner count -1
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore` (884 passed after source-shape test adjustment); `dotnet exec --% tests\Sussudio.Tests\bin\Debug\net8.0\Sussudio.Tests.dll Sussudio/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/Sussudio.dll`; `git diff --check`; regenerated `docs/architecture/Sussudio-Defragmentation-Baseline.generated.md`
CLI/MCP/pipe checks, if applicable: full solution build rebuilds `tools/McpServer`, `tools/ssctl`, and automation tooling; no public automation command names, IDs, wire payloads, XAML bindings, screenshot status/log text, output path policy, or PNG/BMP encoding behavior changed
Behavior preserved: preview-frame screenshot preview-required guard, output directory fallback, timestamped filename, button disable/reenable, status/log text, whole-window dispatch cancellation/failure handling, native PrintWindow capture, result shaping, and PNG/BMP byte-stream encoding remain covered by the existing screenshot tests.
Notes for future agents: keep preview-frame screenshot workflow and whole-window screenshot capture together in `Sussudio/Controllers/Screenshot/ScreenshotControllers.cs`; renderer-level preview-frame GPU readback still belongs with `D3D11PreviewRenderer.ScreenshotCapture.cs`.

Date: 2026-05-26
Area: D3D11 preview renderer resource locality
Problem: `D3D11PreviewRenderer.VideoProcessorPipeline.cs` was a 135-line partial that directly owned resource creation/recreation, output-view/RTV reuse, processor teardown, and color-space state over fields declared and cleaned up in `D3D11PreviewRenderer.Resources.cs`. Reviewing VideoProcessor resource lifetime required opening both files even though the methods were coupled to the resource owner and did not form an independent collaborator.
Files consolidated: `Sussudio/Services/Preview/D3D11PreviewRenderer.VideoProcessorPipeline.cs`
Files added: none
Net production .cs delta: -1; net test .cs delta: 0
Partial clusters reduced: `D3D11PreviewRenderer` production partial count 15 -> 14
Build/tests/runtime checks: `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore`; `dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore` (884 passed); `dotnet exec --% tests\Sussudio.Tests\bin\Debug\net8.0\Sussudio.Tests.dll Sussudio/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/Sussudio.dll`; `git diff --check`; regenerated `docs/architecture/Sussudio-Defragmentation-Baseline.generated.md`
CLI/MCP/pipe checks, if applicable: full solution build rebuilds preview renderer consumers and automation tooling; no public automation command names, IDs, wire payloads, XAML bindings, renderer mode labels, VideoProcessor color-space choices, or render-pass behavior changed
Behavior preserved: VideoProcessor setup, input-resource creation handoff, output-view recreation, swap-chain RTV reuse, color-space updates, and processor-resource teardown now live in `D3D11PreviewRenderer.Resources.cs` with the fields and top-level cleanup they operate on.
Notes for future agents: keep render-pass selection and present accounting in `D3D11PreviewRenderer.RenderPasses.cs`; keep shader/SRV lifecycle in `D3D11PreviewRenderer.ShaderRendering.cs`; keep VideoProcessor resource lifetime in `D3D11PreviewRenderer.Resources.cs`.

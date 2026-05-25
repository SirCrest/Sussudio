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
Notes for future agents: keep ssctl diagnostic tooling commands in `CommandHandlers.Observability.cs`; split only if a command becomes an independently tested workflow or shared helper

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
Notes for future agents: keep XAML button adapters with `MainWindow.ButtonActions.cs` and `IAutomationWindowControl` screenshot routing with `MainWindow.WindowShell.cs`

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
Notes for future agents: keep the private MCP timeline row DTO with JSON projection methods in `PerformanceTimelineTools.Rows.cs`; split only if a projection group grows independent parsing policy

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
Notes for future agents: keep shell visibility commands with `CommandHandlers.Window.cs` unless they grow non-shell policy or a shared UI-control command owner

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
Notes for future agents: keep public lifecycle with `D3D11PreviewRenderer.cs`; keep stop/reinit-stop, panel unbind, native-call fencing, and pending-frame shutdown cleanup in `D3D11PreviewRenderer.StopLifecycle.cs`

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
Notes for future agents: keep bridge resource acquisition and disposal together in `CudaD3D11Interop.Initialization.cs`; keep zero-copy/staging copy hot paths in `CudaD3D11Interop.Copy.cs` and native declarations in `CudaD3D11Interop.Native.cs`

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
Notes for future agents: keep default-experiment-only payload construction with `Program.DefaultExperiment.cs`; keep shared Native XU command IDs in `Program.Commands.cs` and reporting/readback helpers in `Program.DefaultExperiment.Reporting.cs`

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
Notes for future agents: keep multi-segment export shell, template selection, and segment input preflight together in `FlashbackExporter.Segments.cs`; keep packet writing orchestration in `SegmentPacketWriting.cs` and packet read/rebase hot loop behavior in `SegmentPacketReadLoop.cs`

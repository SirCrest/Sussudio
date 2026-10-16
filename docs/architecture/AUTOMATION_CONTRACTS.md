# Automation Contracts

The app exposes a JSON automation protocol over a named pipe. The default pipe
name is:

```text
SussudioAutomation
```

Clients use `SUSSUDIO_AUTOMATION_PIPE` when no pipe name is supplied, then fall
back to `SussudioAutomation`. Set the variable for both the app and client
processes when connecting through a custom pipe. `ssctl --pipe` and
AutomationClient `--pipe`/`-p` values override the environment for that client.

The shared command IDs, protocol constants, manifest/catalog, and pipe security
policy live in `Sussudio.Automation.Contracts/`.
`tools/Common/` remains helper-only for shared clients, formatters, diagnostic
sessions, and probes; do not put protocol/catalog/security contract sources
there.
When adding automation commands, treat `Sussudio.Automation.Contracts/` as the
source of truth for command IDs, protocol constants, command metadata, payload
shape, readiness gating, timeouts, path policy, CLI help, and MCP descriptions.
Then keep these consumers in sync:

- `Sussudio/Services/Automation/AutomationCommandDispatcher*.cs`
- `Sussudio.Automation.Contracts/AutomationCommandCatalog.cs`
- `Sussudio.Automation.Contracts/AutomationPipeProtocol.cs`
- `tools/ssctl/`
- `tools/McpServer/`
- `tools/AutomationClient/`
- `tools/send-automation-command.ps1`
- `tests/Sussudio.Tests/`

The app reads `SUSSUDIO_AUTOMATION_TOKEN` at startup. When a nonblank server token
is configured, every command requires the same token, including
`GetAutomationManifest`. Clients should send it in the request's top-level
`authToken` field; comparison is exact and case-sensitive.

ssctl accepts the global `--token` or `-t` option **before** the command. MCP
accepts `--token` when its server process starts; credentials are configured once
for the server, not exposed as arguments on individual MCP tools:

```powershell
ssctl --token "<token>" manifest
ssctl -t "<token>" state
McpServer --token "<token>"
```

Both clients pass an explicitly supplied token unchanged. When that value is
omitted or null, the shared request builder reads the client's
`SUSSUDIO_AUTOMATION_TOKEN` environment variable. A non-null empty or whitespace
value remains explicit and does not trigger that environment fallback. These
options do not modify the process environment. AutomationClient and
`tools/send-automation-command.ps1` retain their existing explicit-token route.

For compatibility, the server accepts `payload.authToken` on **all commands**
when the top-level `authToken` is null, empty, or whitespace. A wrong nonblank
top-level token takes precedence and cannot be rescued by a matching payload
token. Missing or incorrect effective credentials produce `unauthorized`.
New clients should use the top-level field.

- `invalid-state` means the request was valid but cannot be applied in the current
  app state, such as changing Flashback settings during a Flashback recording.

The manifest's `Authentication` object describes these locations, precedence,
and the server-token configuration rule. It is static contract metadata: it
contains no credential and does not report whether a particular running server
has configured a token. Retrieve the manifest with valid credentials when the
server requires authentication.

## Performance telemetry commands

Manifest revision 3 appends commands 58-64: `GetPerfReport`, `PerfWindow`,
`AddPerfMarker`, `FrameTrace`, `GetTuningKnobs`, `SetTuningKnob` and
`GetLogTail`. They read and control process-wide telemetry only, so they are
ready-independent (they work with no capture device) and their responses carry
the result in `Data` without an embedded automation snapshot. `FrameTrace dump`
writes a file under the `WriteFile` path policy; `SetTuningKnob` rejects unknown
names, wrong types and out-of-range values with `invalid-request` and never
clamps. `SUSSUDIO_AUTOMATION_TOKEN` is not a settable knob. The wire contract is
specified in `docs/architecture/performance-telemetry.md` section 3.

Command 68, `PerfExperimentLease`, was appended later in revision 3 (no bump):
`{ action: status|acquire|renew|release, lease?, holder?, knobs?, ttlSeconds?,
force? }`. One perf experiment at a time holds the knobs it varies; while it
does, `SetTuningKnob` refuses those knobs with `invalid-state` unless its new
optional `lease` field carries the holder's id. A holder that stops renewing
loses the lease at expiry and the app restores its knobs. The experiment runner
uses acquire, renew and release; ssctl `perf lease [status|release --force]` and
MCP `perf_experiment_lease` expose status and the forced release only
(performance-telemetry.md section 2.12).

Eight ssctl verbs and eight MCP tools are projections of those commands
(ssctl `perf report|window|mark|trace|lease`, `knobs`, `knob`, `log`; MCP
`get_perf_report`, `perf_window`, `add_perf_marker`, `frame_trace`,
`get_tuning_knobs`, `set_tuning_knob`, `perf_experiment_lease`, `get_log_tail`). The performance surface
also has tool-only members that have no app command behind them (section 7 of the
spec): ssctl `perf trace analyze` reads a trace dump on the local machine and
needs no running app, ssctl `perf experiment` and MCP `run_perf_experiment` are
clients of the existing commands driven by the shared runner in
`tools/Common/Performance`, and MCP `analyze_frame_trace` is the MCP form of the
offline analyzer. A consumer audit must therefore not assume that every perf verb
or tool maps to one command, or that every command appears as a tool.

## Agent sense commands

Manifest revision 3 also appends commands 65-67, which give an automation agent
ears and fingers. All three are ready-independent, return their result in `Data`
without an embedded automation snapshot, and use the extended response timeout.

- `CaptureAudioOutput` (65) `{ seconds?, outputPath?, measureLatency? }` records
  this process's own rendered audio through Windows process loopback and returns
  an analysis. `seconds` is 0.5 to 30 (default 5) and is rejected, never
  clamped, outside that range. `outputPath` is a `WriteFile` path and must end in
  `.wav`. A capture that did not produce audible audio is a failed response
  (`command-failed`) that still carries the full result in `Data`; status
  `no-audio-rendered` is never reported as a pass. `measureLatency` measures
  capture delivery to the mixer and excludes device input latency and endpoint
  output latency; the result states that scope.
- `GetUiTree` (66) `{ root?, maxDepth?, interactiveOnly? }` reads the live WinUI
  visual tree (window content plus open popups) on the UI thread. `maxDepth` is
  1 to 64 (default 10). The output is capped and says when it truncated.
- `InvokeUiElement` (67) `{ target, action?, value?, force? }` operates a control
  through its automation peer. A request the agent can fix by rewording
  (`not-found`, `ambiguous`, `invalid-action`, `invalid-value` (including a
  list `select` whose value matches no item or several), `action-unsupported`)
  is `invalid-request`; a state of the live UI it cannot
  change by rewording (`hidden`, `disabled`, `action-failed`,
  `window-unavailable`) is `command-failed`. Both carry the full result in `Data`.
  The `accelerator` action is not supported: WinUI has no public API to raise a
  `KeyboardAccelerator`, so the tree lists accelerators and the agent invokes the
  element that owns one.

The wire contract is specified in `docs/architecture/performance-telemetry.md`
sections 2.9 and 2.10. The ssctl verbs `audio listen`, `ui tree` and `ui invoke`
and the MCP tools `listen_audio_output`, `get_ui_tree` and `invoke_ui_element` are
projections of those three commands; there is no tool-only member. The MCP tools
return a failed call's `Data` as structured content as well (unlike the query
tools), because the candidate list of an ambiguous target and the analysis of a
silent capture are how the agent recovers.

Ownership for each consumer is mapped in `docs/architecture/AGENT_MAP.md`; the
architecture guardrail tests cross-check this checklist against that map.

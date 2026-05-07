# Claude Code Instructions

Read `docs/project-plan.md` and `docs/constraints.md` before proposing changes.
They define the HDR pipeline contract and preview model — do not introduce
patterns that conflict.

## Critical Rails

Rules whose violation costs real work. Apply exactly.

- **HDR pipeline must never silently degrade.** If P010 negotiation fails, the
  operation fails. No 8-bit fallback. See `docs/constraints.md`.
- **Preserve every `AutomationProperties.AutomationId`.** The IPC layer and MCP
  tools index on these strings — renaming one breaks tests and external control.
- **Never stash, reset, clean, or checkout-discard as a debugging step.** Those
  are irreversible; the working tree may hold the user's in-progress work. Read
  diffs and reason first.
- **Never edit a file a background Codex task is touching.** Race condition.
  Flag the conflict to the user and work elsewhere until Codex finishes.
- **`AccessViolationException` cannot be caught in .NET 8+** (it's a
  corrupted-state exception). Do not propose try/catch around it.
- **Check MCP app-state before `dotnet build`.** If the app is previewing, close
  it via `window_action(close, armClose=true)` and wait a few seconds. Avoids
  file-lock errors that waste a build cycle.

## Debugging

A/V, encoder, and playback bugs lie — symptoms rarely point at root cause. These
rules exist because past fixes on surface symptoms papered over deeper issues.

- **Instrument before theorizing.** Add logging/metrics, verify runtime state,
  *then* propose a fix. Never claim a fix works without log evidence.
- **Trace the full data path end-to-end** before proposing a fix:
  capture → buffer → encoder → sink → muxer. No "one-line fix" claims — verify
  the whole flow.
- **Build a diagnostic probe if you'd need 2+ rebuild cycles to narrow
  something down.** One MCP probe beats N edit-build-log loops.
- **Generate competing hypotheses before any multi-file fix.** Spawn 2–3
  analysis agents (yours + Codex) instructed to find *alternative* explanations.
  Proceed only when they converge or a probe rules one in. Cost of validating is
  small compared to reverting a wrong refactor.
- **Stop after 3 failed incremental attempts.** Propose a rewrite or
  architectural change and ask the user before continuing.
- **Do not speculate about sub-agent, Codex, or MCP results.** If you don't
  have the output in hand, say so and do the work yourself.

## Project Invariants

- WinUI 3 / .NET 8. Manual code-behind binding (PropertyChanged switch +
  `SetupBindings`). No x:Bind.
- Append to `docs/experiment_log.md` when making investigative changes so
  future sessions see what was tried and why.
- Confirm the worktree with `git rev-parse --show-toplevel` before editing; this
  repository has had multiple sibling worktrees and stale path assumptions are
  easy to make.

## Tools

- **Prefer MCP tools over PowerShell pipe scripts.** At session start, try
  `get_app_state` to confirm availability. Use `get_app_state`,
  `capture_window_screenshot`, `window_action`, `control_preview`,
  `control_recording`, `get_diagnostics` for app interaction. Never hand-build
  pipe JSON when an MCP tool exists — enum ordinals drift.
- **Read `temp/logs/Sussudio_Debug.log` after every build/test.** Some
  local worktrees may have an ignored Claude hook that auto-tails the log after
  `dotnet build` / `dotnet run`, but clean checkouts should not assume it is
  present. Read or tail the log explicitly when the hook output is absent.
- **After a backgrounded app launch, watch the log for ≤60 seconds.** The
  auto-tail hook only captures the instant the Bash call returns; GUI launches
  return immediately and real stability issues take seconds to appear. Tail
  the log with `tail -f temp/logs/Sussudio_Debug.log` as a background
  Bash, Monitor it with a 60-second cap, kill the tail at cap or on exit.
  - New lines arrive → act on them (errors → investigate before continuing).
  - Nothing new in 60s → the app is stable on this path. Resume work, prompt
    the user to begin manual testing, or kick off the CLI/MCP automated test
    routine — don't sit idle waiting longer.
- **When environmental state is ambiguous, ask.** Process detection flakes,
  app-launch checks fail — a 10-second question beats 20 minutes of spiraling.

## Build & Test

App must be closed first (see MCP app-state rule above).

```bash
dotnet build Sussudio/Sussudio.csproj -p:Platform=x64 -p:StageLatestBuild=true
dotnet run --project tests/Sussudio.Tests/ -- "Sussudio/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/Sussudio.dll"
```

Logs: `temp/logs/Sussudio_Debug.log`

## Workflows

- For large implementation, refactor, or diagnostic work, define the execution
  contract first, keep file ownership clear, and run build/test validation before
  handing work back.

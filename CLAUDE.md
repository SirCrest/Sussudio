# Claude Code Instructions

Read `docs/project-plan.md` before proposing changes. It defines the project
scope, HDR pipeline contract, and preview model. Do not introduce features or
patterns that conflict with those goals.

## Shell

- The working directory is already the repo root. Do not `cd` into the repo
  before running commands — it triggers unnecessary permission prompts.

## Workflow

- **Always prefer MCP tools over PowerShell pipe scripts.** At the start of
  every conversation, check if MCP tools are available (e.g. try
  `get_app_state`). If they respond, use MCP for all app interaction:
  `get_app_state`, `capture_window_screenshot`, `window_action`,
  `control_preview`, `control_recording`, `get_diagnostics`, etc. Only fall
  back to PowerShell pipe scripts when MCP is unavailable or for operations
  MCP doesn't cover. Never manually construct pipe JSON or count enum values
  when an MCP tool exists for the same action.
- **Always read `temp/logs/ElgatoCapture_Debug.log` after build/test.** Don't
  wait for the user to paste logs.
- **Trace the full data path end-to-end** before proposing any fix — not just
  the surface layer.
- **Launch perf-review agent** after writing any non-trivial change, before
  declaring done.
- **Never say "one-line fix" or "that's it."** Always verify the full flow.
- **Before iterative debugging, check your instrumentation.** If you'd need 2+
  rebuild cycles to narrow something down, build an MCP diagnostic probe first.
  A one-time investment in a probe beats N iterations of edit-build-log.
- **Before building, check MCP state and close the app if idle.** Don't build
  first and discover the lock error after. If the app is previewing (not
  recording), close it via `window_action(close, armClose=true)` and wait a
  few seconds before running `dotnet build`.
- **Never edit files a background Codex task is touching.** Check if a running
  Codex task has the file in scope. If so, wait for Codex to finish or work on
  a different file. Flag the conflict to the user rather than silently creating
  a race condition.
- **When uncertain about environmental state, ask the user.** If you can't
  detect a process, can't tell if the app launched, or hit an ambiguous state —
  stop and ask. A 10-second question beats 20 minutes of spiraling.
- **Never escalate destructively when uncertain.** Do not stash, reset, or
  clean the working tree as a debugging strategy. Read diffs and reason about
  them first. Those operations are irreversible and can destroy in-progress work.
- **Use subagents aggressively for review and verification.**
- **Never commit to the first root-cause hypothesis.** When you analyze a bug
  and arrive at a plausible explanation, treat it as *one candidate*, not the
  answer. Before writing any fix, spawn 2-3 competing analysis agents (Both Codex and your own) with the same evidence but an explicit instruction to find
  *alternative* explanations. Only proceed when hypotheses converge, or when a
  diagnostic probe confirms one and rules out others. This applies especially
  when the fix would touch multiple files or change architecture — the cost of
  validating is small compared to the cost of reverting a wrong refactor.

## Conventions

- WinUI 3 / .NET 8. Manual code-behind binding (PropertyChanged switch +
  SetupBindings). No x:Bind.
- Preserve all `AutomationProperties.AutomationId` values — the IPC layer
  depends on them.
- HDR pipeline must never silently degrade. If P010 negotiation fails, the
  operation fails. See `docs/constraints.md`.
- Append to `docs/experiment_log.md` when making investigative changes.

## Agent Teams

- This flashback worktree is very complicated. 
- Use Claude Agent Teams for large refactors using Opus 4.6 1M on High or Max Effort.
- You decide how to split up work and how to handle prompting.
- Three agents should be enough. You can move to 4 or 5 if you believe the coding work or research is big enough.
- During exploration and research, feel free to have agents do overlapping theorizing/reading.
- Ensure agents aren't idle during work
- Ensure agents aren't working on the same files as another agent.

## Build & Test

Verify if the app is running before running the following build commands.

```bash
dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -p:StageLatestBuild=true
dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"
```

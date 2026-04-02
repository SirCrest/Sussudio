# Claude Code Instructions

Read `docs/project-plan.md` before proposing changes. It defines the project
scope, HDR pipeline contract, and preview model. Do not introduce features or
patterns that conflict with those goals.

## Shell

- The working directory is already the repo root. Do not `cd` into the repo
  before running commands — it triggers unnecessary permission prompts.

## Debugging Rules

- When debugging A/V, playback, or encoding issues: instrument and measure
  before theorizing. Add diagnostic logging/metrics first, verify the actual
  runtime state, then propose fixes based on evidence. Never claim a fix is
  working without verifiable proof.
- When multiple incremental fix attempts fail (3+ cycles), stop and propose a
  full rewrite or architectural change instead of continuing to tweak. Ask the
  user before continuing down a failing path.

## Pre-Flight Checks

- Before building or deploying, always verify the target app/process is closed.
- Before editing files, confirm you are on the correct git worktree with
  `git rev-parse --show-toplevel`.

## Communication Rules

- Do NOT speculate about results from sub-agents, Codex, or external tools. If
  you don't have concrete output, say so and do the actual work. Never "dream"
  about what a tool might have returned.

## Language-Specific Notes

### C#

- `AccessViolationException` cannot be caught in .NET 8+ (it's a corrupted
  state exception). Always check runtime-specific behaviors before proposing
  exception handling patterns.

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

## Architect-Team Workflow

When the user says **"architect-team"**, read `.claude/workflows/architect-team.md`
and execute the workflow **as team lead yourself**. Do NOT delegate to a sub-agent.
You create the team (TeamCreate), spawn teammates (Agent with `team_name`),
coordinate phases via SendMessage and tasks, run builds, and shut down the team.

This is the primary workflow for large implementation, refactoring, and
diagnostic/bug-fix tasks on this codebase.

## Agent Teams (general)

- This flashback worktree is very complicated.
- Use Claude Agent Teams for large refactors using Opus 4.6 1M on Max Effort.
- During exploration and research, feel free to have agents do overlapping
  theorizing/reading.
- Ensure agents aren't idle during work.
- Ensure agents aren't working on the same files as another agent.

## Build & Test

Verify if the app is running before running the following build commands.

```bash
dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -p:StageLatestBuild=true
dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"
```

# Claude Code Instructions

Read `docs/project-plan.md` before proposing changes. It defines the project
scope, HDR pipeline contract, and preview model. Do not introduce features or
patterns that conflict with those goals.

## Shell

- The working directory is already the repo root. Do not `cd` into the repo
  before running commands — it triggers unnecessary permission prompts.

## Workflow

- **Always read `temp/logs/ElgatoCapture_Debug.log` after build/test.** Don't
  wait for the user to paste logs.
- **Trace the full data path end-to-end** before proposing any fix — not just
  the surface layer.
- **Launch perf-review agent** after writing any non-trivial change, before
  declaring done.
- **Never say "one-line fix" or "that's it."** Always verify the full flow.
- **Before iterative debugging, check your instrumentation.** If you'd need 3+
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
- Use subagents aggressively for review and verification.

## Conventions

- WinUI 3 / .NET 8. Manual code-behind binding (PropertyChanged switch +
  SetupBindings). No x:Bind.
- Preserve all `AutomationProperties.AutomationId` values — the IPC layer
  depends on them.
- HDR pipeline must never silently degrade. If P010 negotiation fails, the
  operation fails. See `docs/constraints.md`.
- Append to `docs/experiment_log.md` when making investigative changes.

## Codex CLI

Route all non-trivial implementation work to OpenAI Codex CLI. Only make
small edits (3-line fixes, single-file tweaks) directly in Claude Code.
Budget is unlimited — maximize reasoning depth and self-verification.

### Execution

- Config defaults: `gpt-5.3-codex`, `xhigh` reasoning, `multi_agent = true`
- Run via `codex exec --full-auto "<prompt>"` (non-interactive, auto-approve)
- Execute `codex` directly via Bash — do not ask the user to copy-paste
- Encourage Codex to spawn sub-agents for parallel work when efficient

### Claude Code's role as PM

The user gives short, high-level requests ("add format caching", "fix the
reinit flash"). Claude Code does ALL of the following before dispatching:

1. **Explore the codebase** — read the relevant files, trace callers, identify
   edge cases. Use subagents for broad searches.
2. **Write the full Codex prompt** — fill in every field of the template below
   with specific file paths, method names, caller lists, and constraints
   discovered during exploration. The user should never need to type file paths
   or implementation details.
3. **Dispatch to Codex** — run `codex exec --full-auto` with the assembled prompt.
4. **Verify Codex's output** — git diff, build, test, log review, perf-review
   agent. Fix anything Codex missed before reporting back to the user.

The user's only job is intent. Claude Code handles specification and verification.

### Prompt template

Every Codex prompt MUST follow this structure. Do not skip phases.

```
CONTEXT: Read these files first and understand them fully before writing
any code: [list every file to modify + key callers/consumers].

Read docs/project-plan.md for project scope and constraints.

TASK: [precise description of what to implement/fix]

CALLER TRACING: Before changing any method, function, or property, search
for ALL callers and consumers. If you change a signature, return type, or
behavior, update every call site. Use grep/search — do not guess.

IMPLEMENT: Write the code.

VERIFY (mandatory — do not skip):
1. Build: dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -p:StageLatestBuild=true
2. Test: dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"
3. Read temp/logs/ElgatoCapture_Debug.log and check for errors or warnings.
4. Re-read every file you modified. Look for: typos, missing null checks,
   wrong variable names, threading issues (UI thread vs background),
   broken callers, and off-by-one errors. Fix anything you find.
5. If you found and fixed issues in step 4, re-run build + test again.

FAILURE RECOVERY: If the build or tests fail, diagnose the root cause,
fix it, and retry. Repeat up to 3 times. Do not stop and report the
error — fix it yourself.

EDGE CASES TO CHECK:
- Null or empty device lists / format lists
- First-run scenarios (no saved settings, no cache files)
- UI thread safety (ObservableCollection mutations must be on UI thread)
- HDR pipeline: P010 negotiation must never silently degrade
- AutomationProperties.AutomationId values must not be removed or changed
```

### After Codex returns

Claude Code must independently verify Codex's work:
- `git diff` and read the actual changes (not just trust Codex's summary)
- Confirm build + test pass in your own shell
- Read `temp/logs/ElgatoCapture_Debug.log`
- Launch perf-review agent if the change touches hot paths

## Build & Test

```bash
dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -p:StageLatestBuild=true
dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"
```

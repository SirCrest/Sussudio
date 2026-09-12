# Sussudio Agent Guide

## Engineering Taste

Sussudio values careful, measured, high-performance engineering. Make the code feel intentional and durable.

- Prefer boring, explicit ownership over clever abstraction.
- Prefer measured performance preservation over speculative optimization.
- Prefer small, verified steps over dramatic rewrites.
- Prefer names that explain the runtime role of a thing, not just its implementation detail.
- Prefer code that a performance-minded engineer could audit quickly: clear state transitions, bounded queues, explicit lifetimes, and visible failure modes.
- When touching hot paths, think in allocations, copies, locks, thread hops, GPU/CPU synchronization, and shutdown behavior.
- When touching UI, preserve polish: transitions, spacing, visible state, and demo-facing behavior matter.
- When unsure, investigate the uncertainty and leave evidence.

## Preferred Agent Behavior

- Start from live repo evidence. Re-read the files you are about to change, even if the prompt includes a detailed summary.
- Give each behavior one obvious owner. When moving behavior to a new file, controller, facade, or partial, update matching ownership tests and `docs/architecture/AGENT_MAP.md` in the same slice.
- Preserve runtime contracts. Capture, recording, HDR, Flashback, audio, preview pacing, and automation protocol behavior should stay identical unless the task explicitly asks for a behavior change.
- Commit coherent checkpoints during long-running cleanup work so rollback stays easy. Commit only changes belonging to the current task; inspect the staged diff and preserve unrelated work.

## Refactor Standard

A cleanup is successful only if the system is easier to reason about afterward. Reduce the number of files an agent must inspect before making a safe change, and keep names and folders deliberate.

Avoid changes that only move code around, hide complexity behind vague names, create abstractions before the responsibility boundary is proven, or make future debugging require more guesswork.

## Hard Safety Rails

- Do not silently fall back from HDR or selected recording codecs.
- Do not reintroduce blocking waits into source-reader hot paths.
- Do not change automation command IDs, names, or wire protocol behavior without updating every consumer and test.
- Do not claim work is complete if validation was skipped or failed.

## Agent Failure Modes To Watch For

Agents have repeatedly made these mistakes in this repo. Check for them before finishing:

- Looking in old pre-rename paths. The app source is under `Sussudio/`.
- Treating named-pipe automation as only app plus dispatcher. Check shared contracts, `ssctl`, MCP, AutomationClient, PowerShell helper scripts, and tests.
- Trimming usings or dependencies by only searching for method calls. Search for type names too.
- Making mechanical PowerShell rewrites without preserving UTF-8 and inspecting the diff immediately afterward.

## Validation

Use focused tests while editing. After meaningful code changes, including ownership moves and runtime changes, run the single validation entry point:

```powershell
powershell -NoProfile -File scripts\validate.ps1
```

It builds the solution, runs the xUnit suite, performs the assembly-load smoke check, and runs `git diff --check`, then writes `artifacts/validation.json` describing what ran, what passed, and what the run does not prove. Exit code is 0 only when every step succeeded. Read the JSON rather than scraping console output.

That smoke step only loads the built app assembly and executes **zero tests**; it is reported as its own step kind and must never be cited as regression coverage. The xUnit suite is the only source of test results.

`scripts\validate.ps1` uses `--no-restore` because NuGet writes to the global package cache, which a workspace-scoped agent sandbox denies silently. Run it with `-Restore` when running outside a confined sandbox, and note that a confined sandbox also blocks both MSBuild compiler paths (the shared `VBCSCompiler` named pipe and the piped-stdio `csc` task), so builds inside one require elevated access.

When editing shared automation or tool sources, also rebuild affected tools such as `ssctl` and `NativeXuAudioProbe`.

Passing automated checks does not prove live capture, audible output, HDR display correctness, or recording playback. When those behaviors change, also run relevant live checks where possible. Report automated results and live observations separately, and identify any failed, skipped, or unavailable checks and the resulting limits on what was verified.

Treat build failures diagnostically. Check for locked app/tool processes and stale binaries before assuming the source is broken. Before stopping a process such as `Sussudio.exe` or `McpServer.exe` that locks build outputs, check whether it is recording or serving an active session. Coordinate any interruption with the session owner, using existing authorization where applicable. Once the lock is safely cleared, rerun the real build path and restore the session afterward where possible.

The common case is `MSB3027`/`MSB3026` on `tools\McpServer\bin\...\Sussudio.Automation.Contracts.dll`: a running `McpServer` locks the copy step, which fails the build even though `McpServer.csproj` compiled fine. **The Codex desktop app relaunches McpServer whenever it opens**, so this recurs and killing the processes by hand is not a fix. Run validation with `-ClearToolLocks`, which stops only lockers whose image lives under this repository and records what it stopped:

```powershell
powershell -NoProfile -File scripts\validate.ps1 -ClearToolLocks
```

`artifacts/validation.json` reports `environmentLockDetected` and the `lockClearing` record, so a locked build is never mistaken for broken source. If `lockClearing.respawned` lists anything, the launcher won the race and the build will still fail.

## Dead Surface

Run before starting work in an area, and after finishing a cleanup slice:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\architecture\Report-DeadSurface.ps1
```

It writes `artifacts/dead-surface.json` and reports production code that nothing uses: environment variables read but never set or documented, private fields assigned but never read, and private methods with no call site.

**Every reported item is either deleted, or annotated in place with a comment saying why it must stay.** There is no third option. A report that is read and then ignored is worse than no report, because the next agent cannot tell a deliberate keeper from an oversight.

The detection is deliberately conservative: false negatives are fine, false positives are not, because a report that cries wolf gets ignored. A `ref`/`out` use counts as a read, event and delegate fields are skipped, XAML-wired handlers are resolved from markup, and field reads are resolved across partial-class siblings. If you find a new false-positive class, fix the script rather than dismissing the report.

## Windows And Worktree Notes

- Read `.claude/napkin.md` before substantial work; it contains repo-specific traps and recent lessons.
- Do not rely on remembered worktree names. Verify current checkouts with `git worktree list --porcelain`.
- If working in a sibling worktree, run Git and build commands against that exact path.
- Use PowerShell syntax rather than bash idioms. Prefer separate commands for clarity and compatibility with Windows PowerShell 5.1; PowerShell 7 supports `&&`.
- Prefer `rg PATTERN folder --glob "*.cs"` over wildcard path arguments such as `Sussudio/MainWindow*.cs`.
- Avoid broad repo-wide `Select-String`; narrow the path or use `rg`.

# Sussudio Agent Guide

## Engineering Taste

Sussudio values slow, careful, high-performance engineering over rushed churn. Make the code feel intentional, measured, and durable.

- Prefer boring, explicit ownership over clever abstraction.
- Prefer measured performance preservation over speculative optimization.
- Prefer small, verified steps over dramatic rewrites.
- Prefer names that explain the runtime role of a thing, not just its implementation detail.
- Prefer code that a performance-minded engineer could audit quickly: clear state transitions, bounded queues, explicit lifetimes, and visible failure modes.
- When touching hot paths, think in allocations, copies, locks, thread hops, GPU/CPU synchronization, and shutdown behavior.
- When touching UI, preserve polish: transitions, spacing, visible state, and demo-facing behavior matter.
- When unsure, slow down, inspect the live code, and leave evidence.

## Preferred Agent Behavior

- Start from live repo evidence. Re-read the files you are about to change, even if the prompt includes a detailed summary.
- Keep ownership visible. When moving behavior to a new file, controller, facade, or partial, update matching ownership tests and `docs/architecture/AGENT_MAP.md` in the same slice.
- Preserve runtime contracts. Capture, recording, HDR, Flashback, audio, preview pacing, and automation protocol behavior should stay identical unless the task explicitly asks for a behavior change.
- Validate at the right depth. Focused tests are useful while editing, but meaningful ownership or runtime changes should finish with build, tests, offline harness, and `git diff --check`.
- Treat build failures diagnostically. Check for locked app/tool processes and stale binaries before assuming the source is broken.
- Use Windows-friendly commands. Prefer `rg PATTERN folder --glob "*.cs"` over wildcard path arguments, and use PowerShell syntax rather than bash idioms.
- Commit coherent checkpoints during long-running cleanup work so rollback stays easy.

## Refactor Standard

A cleanup is successful only if the system is easier to reason about afterward.

Good cleanup:
- gives behavior one obvious owner;
- reduces the number of files an agent must inspect before making a safe change;
- preserves performance and runtime semantics;
- updates tests and docs so the new boundary is enforceable;
- leaves names and folders feeling deliberate.

Bad cleanup:
- only moves code around;
- hides complexity behind vague names;
- creates abstractions before the responsibility boundary is proven;
- changes capture, recording, Flashback, HDR, audio, or preview behavior accidentally;
- makes future debugging require more guesswork.

## Hard Safety Rails

- Do not silently fall back from HDR or selected recording codecs.
- Do not reintroduce blocking waits into source-reader hot paths.
- Do not change automation command IDs, names, or wire protocol behavior without updating every consumer and test.
- Do not claim work is complete if validation was skipped or failed.

## Agent Failure Modes To Watch For

Agents have repeatedly made these mistakes in this repo. Check for them before finishing:

- Trusting old prompt context over live files. Re-read the exact source before patching.
- Looking in old pre-rename paths. The app source is under `Sussudio/`.
- Using Windows wildcard paths with `rg`, such as `Sussudio/MainWindow*.cs`. Search the folder and use `--glob`.
- Moving methods between partials without updating ownership tests and `docs/architecture/AGENT_MAP.md`.
- Stopping at focused tests after moving ownership boundaries. Run the full offline harness too.
- Treating named-pipe automation as only app plus dispatcher. Check shared contracts, `ssctl`, MCP, AutomationClient, PowerShell helper scripts, and tests.
- Trimming usings or dependencies by only searching for method calls. Search for type names too.
- Treating locked build outputs as source breakage before checking for running app/tool processes.
- Making mechanical PowerShell rewrites without preserving UTF-8 and inspecting the diff immediately afterward.
- Letting cleanup change runtime semantics in capture, recording, HDR, Flashback, audio, or preview pacing.

## Validation

Run the normal validation sequence after meaningful code changes:

```powershell
dotnet build Sussudio.slnx -p:Platform=x64 --no-restore
dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore
dotnet exec tests\Sussudio.Tests\bin\Debug\net8.0\Sussudio.Tests.dll "Sussudio/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/Sussudio.dll"
git diff --check
```

When editing shared automation or tool sources, also rebuild affected tools such as `ssctl` and `NativeXuAudioProbe`.

## Windows And Worktree Notes

- Read `.claude/napkin.md` before substantial work; it contains repo-specific traps and recent lessons.
- Do not rely on remembered worktree names. Verify current checkouts with `git worktree list --porcelain`.
- If working in a sibling worktree, run Git and build commands against that exact path.
- PowerShell does not accept bash-style `&&`; use separate commands or PowerShell-native separators.
- Avoid broad repo-wide `Select-String`; narrow the path or use `rg`.
- If a running `Sussudio.exe` or `McpServer.exe` locks build outputs, stop the process and rerun the real build path.

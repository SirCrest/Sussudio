# Repository Guidelines

Read `docs/project-plan.md` before proposing changes. It defines the project
scope, HDR pipeline contract, and preview model. Do not introduce features or
patterns that conflict with those goals.

## Debugging Workflow (follow this order, no shortcuts)

1. **Read the log first.** The app writes to `temp/logs/ElgatoCapture_Debug.log`. Read it after every build/run.
2. **Find the failure token, then find the code that produced it.** When you see a structured log token like `PREVIEW_START_TIMEOUT missing=MediaOpened` or `HDR_VALIDATE_RESULT FAIL`, do NOT generate a list of possible causes from the token text alone. Instead:
   - Grep the source for the exact string that produced the log line (e.g., `PREVIEW_START_TIMEOUT`, `HDR_VALIDATE_RESULT`).
   - Read that source location in full — the surrounding guards, the state that feeds it, and the code that sets that state.
   - Only then propose a diagnosis.
3. **Trace the full data path end-to-end.** A fix is only valid once you can name the exact code location where the failure originates and explain why the current code takes the wrong branch.
4. **Do not say "I found the precise issue" unless you have read the relevant source.** Correlating log tokens is not diagnosis. Causation requires code.
5. **One change per fix.** Make the smallest change that addresses the root cause. Do not clean up surrounding code, add logging, or refactor unrelated sections in the same commit.

## HDR Pipeline Rules (non-negotiable)

- When `HdrEnabled = true` and `HdrOutputMode = Hdr10Pq`, every layer must deliver P010/HDR10-PQ output or **fail loudly**. No silent fallback to SDR.
- If P010 negotiation fails at any point, throw — do not continue with a degraded pipeline.
- Post-recording: `tools/validate_hdr.ps1 -ExpectHdr` must pass. A recording that fails validation is a failed recording.
- See `docs/constraints.md` for the full list of hard-fail conditions.

## Experiment Log (append-only)

- Every investigative change must be logged in `docs/experiment_log.md` using the template in that file.
- **Never edit or delete prior entries.** Append only.
- Each experiment must have: one scoped change, how to run it, and what the validator/log output showed.
- Do not make more than one change per experiment entry.

## Project Structure & Module Organization
- `ElgatoCapture/`: WinUI 3 app source (`MainWindow.xaml`, `ViewModels/`, `Services/`, `Models/`, `Converters/`, `Assets/`).
- `ElgatoCapture/ViewModels/`: UI state, commands, and capture orchestration.
- `ElgatoCapture/Services/`: device discovery, capture lifecycle, recording sinks, FFmpeg integration, and coordination.
- `ElgatoCapture/Models/`: settings, formats, diagnostics, and runtime state models.
- `tools/`: automation scripts (`reliability-gates.ps1`, `stage-builds.ps1`).
- `builds/`: staged binaries; `artifacts/`: generated outputs/diagnostics.
- `tests/ElgatoCapture.Tests/`: runtime snapshot regression harness.

## Build, Test, and Development Commands
- `dotnet build ElgatoCapture/ElgatoCapture.csproj -c Debug -p:Platform=x64`: build local debug binary.
- `dotnet build ElgatoCapture/ElgatoCapture.csproj -c Release -p:Platform=x64`: build optimized release binary.
- `powershell -File tools/reliability-gates.ps1 -Configuration Debug`: run reliability gate (fails on `MVVMTK0045`; optional warning gate support).
- `powershell -File tools/stage-builds.ps1`: build Debug/Release and mirror outputs to `builds/win-x64/*`.
- `powershell -File RunApp.ps1`: open the solution in Visual Studio.

## Coding Style & Naming Conventions
- Language stack: C# on `.NET 8` (`net8.0-windows10.0.19041.0`) with nullable enabled.
- Use 4-space indentation and UTF-8 text files.
- Naming conventions: `PascalCase` for types/methods/properties, `camelCase` for locals/private fields.
- Use descriptive async method names ending with `Async`.
- Keep UI/binding logic in XAML + `ViewModels`; keep capture/device/encoding behavior in `Services`.

## Testing Guidelines
- Before opening a PR, complete all of the following checks.
1. Build `Debug` and `Release` for `x64`.
2. Run `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"`.
3. Run `powershell -File tools/reliability-gates.ps1 -Configuration Debug`.
4. Manually smoke test device enumeration, preview start/stop, recording start/stop, and output file creation.

## Commit & Pull Request Guidelines
- Follow existing commit format: `<type>: <summary>` (examples: `chore:`, `perf:`, `refactor:`, `compat:`).
- Keep commits focused to one concern and avoid unrelated changes.
- PRs should include a behavior summary, linked issue/task (if available), validation steps/results, and screenshots for UI/XAML changes.

## Subagents
- ALWAYS wait for all subagents to complete before yielding.
- Spawn subagents automatically when:
- Parallelizable work (e.g., install + verify, npm test + typecheck, multiple tasks from plan)
- Long-running or blocking tasks where a worker can run independently.
- Isolation for risky changes or checks

## Command Request Communication
- Before any shell command request, provide a one-sentence summary in chat describing the command's purpose.
- For escalated commands, the `justification` question must match that same one-sentence purpose.
- Treat this as mandatory for both standard and escalated command requests.

## Approval Minimization
- For app-control runs, reuse existing approved command prefixes whenever possible; do not request new elevated prefixes unless strictly necessary.
- Prefer a narrow allowlist model: launch app, drive `AutomationClient.exe`, stop app, and read repo-local artifacts/logs.
- Avoid ad-hoc elevated shell one-liners for routine control actions when an approved transport command already exists.

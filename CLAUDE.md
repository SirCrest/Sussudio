# Claude Code Instructions

Read `docs/project-plan.md` before proposing changes. It defines the project
scope, HDR pipeline contract, and preview model. Do not introduce features or
patterns that conflict with those goals.

## Workflow

- **Always read `temp/logs/ElgatoCapture_Debug.log` after build/test.** Don't
  wait for the user to paste logs.
- **Trace the full data path end-to-end** before proposing any fix — not just
  the surface layer.
- **Launch perf-review agent** after writing any non-trivial change, before
  declaring done.
- **Never say "one-line fix" or "that's it."** Always verify the full flow.
- Use subagents aggressively for review and verification.

## Conventions

- WinUI 3 / .NET 8. Manual code-behind binding (PropertyChanged switch +
  SetupBindings). No x:Bind.
- Preserve all `AutomationProperties.AutomationId` values — the IPC layer
  depends on them.
- HDR pipeline must never silently degrade. If P010 negotiation fails, the
  operation fails. See `docs/constraints.md`.
- Append to `docs/experiment_log.md` when making investigative changes.

## Build & Test

```bash
dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -p:StageLatestBuild=true
dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"
```

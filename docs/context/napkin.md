# Napkin

## Operating Invariants
- Treat ElgatoCapture automation/validation as x64-only (`builds\\win-x64\\...` or `bin\\x64\\...\\win-x64\\ElgatoCapture.exe`).
- Hardware testing is authoritative only when launched elevated.
- Keep commands bounded with explicit timeouts/progress; avoid long silent hangs.

- Keep PowerShell execution policy restrictive (`CurrentUser=Restricted`) unless a task explicitly needs script execution.

## User Preferences
- Prefer simplifying to x64-only when multi-arch adds friction.
- Wants unattended automation that can dismiss known crash popups without manual intervention.
- Wants runnable builds staged in repo root for easy manual testing.
- Expects bounded, observable execution (no long silent hangs).
- For reliability review automation, prefers one skill with internal feature routing instead of multiple separate skills.

## Consolidated Corrections
| Date | Source | Mistake Pattern | Correct Rule |
|------|--------|-----------------|--------------|
| 2026-02-12 | self | Wrong app architecture/path caused startup crashes (`0xe0434352`). | Pin launches to x64 output path only. |
| 2026-02-12 | user+self | Non-elevated/sandboxed launches produced false hardware negatives. | Require elevated launch for hardware truth; reject non-elevated results. |
| 2026-02-12 | self | Aborted test runs left orphaned `dotnet`/`testhost` processes and phantom relaunches. | Kill orphaned runners immediately after abort before next run. |
| 2026-02-12 | self | Treated transitional device-init log as failure. | `Device not initialized, initializing now...` is expected startup unless explicit failure follows. |
| 2026-02-12 | self | Misdiagnosed preview blink as capture-device instability. | Check `Preview UI stall` metrics first; compare 1080p60 vs 1080p30 to isolate UI pipeline limits. |
| 2026-02-12 | self | UIA assumptions were brittle (always-visible IDs, desktop-root list item scraping). | Treat visibility-dependent controls as conditional and scope combo item search to combo descendants. |
| 2026-02-12 | self | PowerShell probe scripts reused reserved `$PID` variable name. | Use `$targetProcId` (or similar) instead of `$pid`. |
| 2026-02-12 | self | Profile load noise/error polluted command output. | Keep accidental profile files removed and use `-NoProfile` in automation commands. |
| 2026-02-12 | self | Misread user intent and started shaping unrelated docs/skills before confirming the target set was reliability-pass files and a single skill design. | Confirm target source files and one-skill-vs-many architecture before making edits. |
| 2026-02-12 | self | Broke PowerShell `rg` command parsing by wrapping a backtick-containing regex in double quotes. | Use single-quoted PowerShell strings for regex patterns containing markdown backticks. |
| 2026-02-12 | self | `dotnet build/test` failed before execution due first-run PATH setup writing under a restricted home path. | Set `DOTNET_CLI_HOME` to a writable repo-local path and `DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1` for sandboxed runs. |
| 2026-02-12 | self | Solution-level `dotnet build` in this sandbox can fail intermittently without diagnostics under default parallelism. | Prefer `dotnet build ... -m:1` for deterministic reliability verification in sandbox sessions. |
| 2026-02-12 | self | A shell read command hung for an extended period due missing explicit timeout. | Always set `timeout_ms` on shell calls, including read-only inspections, and fail fast on stalls. |
| 2026-02-12 | self | Ad-hoc UI automation could not access WinUI controls because app manifest requires admin and test host context was non-elevated. | For direct UI driving, run the Codex session itself elevated; run-as attempts from inside this session can be canceled or blocked. |
| 2026-02-12 | self | Assumed `Start-Process` would provide reliable `ExitCode` for gate commands; it returned null in this sandbox and produced false-pass logic. | Use a `Start-Job` wrapper with `Wait-Job -Timeout` to enforce timeout and capture `$LASTEXITCODE` deterministically. |

## Fast Checks
- Launch elevated x64 app and confirm fresh timestamps in `%USERPROFILE%\\Documents\\ElgatoCapture_Debug.log`.
- Validate preview with paired markers: `Frame reader start result: Success` and `Preview started successfully`.
- If preview blinks/stalls, compare 1080p60 against 1080p30 before blaming hardware.
- After any interrupted run, clean orphaned `dotnet`/`testhost` processes.
- Restore user-facing state after automation pokes (preview stopped, output path unchanged, expected defaults restored).

## Domain Notes
- Hardware is now Xbox Series X feed through Game Capture 4K X.
- UI automation is phase 1; MCP/API bridge is phase 2.
- In this Codex sandbox, `dotnet restore/build/test` may fail with `NU1301` against `https://api.nuget.org/v3/index.json` due blocked outbound sockets; prefer static review or pre-restored packages when network is restricted.
- User wants automation stack debt removed and rebuilt from a minimal baseline rather than incrementally patched.
- Offline restore with `--ignore-failed-sources` still fails (`NU1101`) when required packages are not already cached locally.
- Git metadata writes (`refs`, `index`, `objects`) can fail in sandbox mode; branch/tag operations may require outside-sandbox execution.
- A leftover `.codex` directory may include deny ACL entries that block deletion from this agent context even when file content has been moved.

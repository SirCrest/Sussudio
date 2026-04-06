# Autonomous QA Skill — Design Spec

## Purpose

Long-running (8-10hr) autonomous QA skill for the Elgato Capture app. Generates an exhaustive test matrix from the app's available options, executes each test sequentially with verification gates, and fixes bugs discovered along the way with multi-model sub-agent code review.

The user walks away. The skill runs until every combination is tested or the session ends. It never asks for input. It never batches commands. It never assumes success.

## Problem Statement

Previous QA sessions failed because Claude:
- Batched 5-17 `ecctl` commands into single Bash scripts
- Continued firing commands at a crashed/hung app for 10+ steps
- Acknowledged the batching problem when interrupted, then relapsed within minutes
- Created shell functions (`run()`, `check()`) that made failures unobservable
- Used `sleep N` instead of actual state verification

The skill must structurally enforce sequential execution — instructions alone are insufficient, as demonstrated across 4+ user interruptions in a single session.

## Architecture

### Main QA Agent (Opus, default effort)
- Executes all hands-on testing
- ONE `ecctl` command per Bash tool call — no exceptions
- Reads and interprets every command's output before proceeding
- Makes code fixes when tests fail
- Updates the test matrix file at every state transition

### Code Review Panel (spawned only when a code fix is made)
Three models with decorrelated reasoning:
- **Opus (medium effort)** — thorough structural review
- **Sonnet** — different reasoning pattern, good at "does this actually solve the problem"
- **Haiku** — literal, surface-level, catches obvious regressions that overthinking misses

**Unanimous agreement required.** Any dissent triggers another fix iteration. If Haiku finds something Opus missed, that's valid — decorrelated reasoning is the entire point. Max 2 rounds of review before escalating to Codex as final arbiter.

### Codex
- Reviews every code fix for codebase-context issues (free, always used)
- Acts as final tiebreaker if the 3-model panel can't reach unanimity after 2 rounds

## Phase 1: Test Matrix Generation

1. Run `ecctl options` to discover all available settings and valid values
2. Run `ecctl state --json` to understand current baseline state
3. Generate exhaustive matrix of valid combinations:
   - Codecs x resolutions x FPS x bitrates x formats
   - Quality levels, presets, encoder modes
   - Toggle combinations: HDR, audio, mic, split encode
   - Use supported resolution/FPS combos from `ecctl options` — don't test impossible combinations, but be thorough beyond just the basics
4. Write matrix to `docs/qa/test-matrix-YYYY-MM-DD.md`
5. Commit the plan file before starting execution

### Test Matrix File Format

The matrix file is the single source of truth — plan, progress tracker, and checkpoint for pause/resume.

```markdown
# QA Test Matrix — 2026-04-02

## Run Status
- **Started:** 2026-04-02 10:30
- **Last Updated:** 2026-04-02 14:22
- **Progress:** 47/128 complete
- **Bugs Found:** 3 (2 fixed, 1 blocked)

## Results

| #  | Category | Setting | Value | Status | UI | Behavior | Recording | Output | Notes |
|----|----------|---------|-------|--------|----|----------|-----------|--------|-------|
| 1  | codec    | codec   | H.264 | PASS   | ok | ok       | ok        | ok     |       |
| 2  | codec    | codec   | HEVC  | FIXED  | ok | ok       | ok        | ok     | pipe hang on switch, fixed in encoder.cs:234 |
| 3  | combo    | AV1+4K  | ...   | BLOCKED| ok | FAIL     |           |        | app crashes, 2 attempts failed |
| 4  | resolution| res    | 1440p | TESTING|    |          |           |        | currently executing |
| 5  | resolution| res    | 2160p | PENDING|    |          |           |        |       |
```

### Status Values

| Status | Meaning |
|--------|---------|
| `PENDING` | Test hasn't started |
| `TESTING` | Currently executing this test case |
| `FIXING` | Found a failure, investigating/fixing (notes say what failed) |
| `REVIEWING` | Fix made, sub-agents reviewing the code change |
| `PASS` | All verification steps confirmed |
| `FIXED` | Failed initially, code fix applied and re-test passed |
| `BLOCKED` | 2 fix attempts failed, moved on |
| `SKIPPED` | Depends on a BLOCKED feature, can't meaningfully test |

The file is updated at every state transition — if you open it mid-run, you see exactly where the skill is and what it's doing.

## Phase 2: Sequential Execution

For each test case in the matrix:

```
1. HEALTH CHECK
   └─ ecctl state --json — is the app alive and responsive?
   └─ If unresponsive → Recovery Protocol (see below)

2. ACTION
   └─ Single ecctl set command (ONE setting change)

3. WAIT 5 SECONDS
   └─ Let the UI react to the change

4. UI VERIFY
   └─ ecctl state --json
   └─ Did the setting value change in the app's state?
   └─ Does the UI reflect the new value?

5. BEHAVIOR VERIFY
   └─ ecctl state --json (deeper inspection)
   └─ Did the app actually switch behavior?
   └─ e.g., switching 1080p→1440p: is the capture pipeline now 1440p, not just the dropdown?

6. TEST RECORDING
   └─ ecctl record
   └─ Wait 5-10 seconds
   └─ ecctl stop

7. OUTPUT VERIFY
   └─ ffprobe / ecctl verify on the output file
   └─ Correct codec? Resolution? FPS? Bitrate in expected range?
   └─ File is playable and not corrupted?

ALL PASS → Update matrix as PASS → next test case
ANY FAIL → Enter Fix Loop (Phase 3)
```

**Every step is a separate Bash tool call.** The main agent reads and interprets the output of each call before issuing the next.

## Phase 3: Fix Loop

When any verification step fails:

```
1. UPDATE MATRIX → status = FIXING, notes = what failed and at which step

2. INVESTIGATE
   └─ Read relevant source code
   └─ Understand why the failure occurred
   └─ Check if this is a known pattern (e.g., pipe hang during reinit)

3. MAKE CODE FIX

4. CODE REVIEW — spawn 3 sub-agents in parallel:
   Each receives (limited context — no test history, no matrix, just the fix):
     - The bug description (what failed and how)
     - The code before the fix
     - The code after the fix
     - Surrounding code for context
   
   Each answers:
     - APPROVE or REQUEST CHANGES
     - One-paragraph reasoning
     - Any regression concerns
   
   Models: Opus (medium), Sonnet, Haiku
   
   Unanimous APPROVE required.
   Any REQUEST CHANGES → address concern → re-submit to all 3
   Max 2 review rounds → if still no unanimity, Codex decides

5. CODEX REVIEW
   └─ Receives: bug description, fix diff, relevant codebase context
   └─ Reviews for codebase-wide implications
   └─ APPROVE or REQUEST CHANGES

6. UPDATE MATRIX → status = REVIEWING

7. RE-TEST ENTIRE FEATURE (all 7 steps from Phase 2, not just the failed step)

8. If re-test passes → status = FIXED → next test case
   If re-test fails → attempt 2 (back to step 2)
   If attempt 2 fails → status = BLOCKED → log details → next test case
```

## Phase 4: End-of-Run Summary

After all test cases are processed:

1. Revisit all BLOCKED items for one fresh attempt (fresh eyes)
2. Update matrix file with final results
3. Generate summary section at top of matrix file:
   - Total tests, passed, fixed, blocked, skipped
   - List of all code changes made with file:line references
   - List of all BLOCKED issues with reproduction steps
4. Commit everything

## Pause / Resume

### Pause
User interrupts (Ctrl+C, closes session, etc.). No special action needed — the matrix file already has current state because it's updated at every transition.

### Resume
User says "continue auto-qa" or invokes the skill with `--continue`:
1. Find the most recent `docs/qa/test-matrix-*.md` file
2. Read it, find the first test case that isn't PASS/FIXED/BLOCKED/SKIPPED
3. If a test was TESTING or FIXING when interrupted, re-test it from the top (don't assume mid-test state)
4. Continue executing from there
5. Any code fixes from previous runs are already committed — the current codebase is the starting point

### Cancel
User explicitly cancels. Write summary of progress so far and stop.

## Recovery Protocol

When the app is unresponsive or crashed:

```
1. Check process — is the app still running?
2. If dead → relaunch the app
3. Wait for named pipe — poll ecctl state every 5s, max 60s timeout
4. Verify baseline state — ecctl state --json returns valid data
5. If pipe available → resume from current test case
6. If 60s timeout → log as infrastructure failure, attempt one more relaunch
7. If still dead → BLOCKED all remaining tests, write summary, stop
```

## Hard Rules

These are structural enforcement — not suggestions:

1. **ONE ecctl command per Bash tool call.** No `&&` chains. No shell scripts. No shell functions. No for loops. If you catch yourself writing a multi-command Bash call for ecctl, DELETE IT and do one command.

2. **Read every output.** Every ecctl call returns output. Read it. Interpret it. Only then decide the next action.

3. **Never ask the user.** The user is asleep/away. Investigate, fix, or skip. If genuinely stuck after 2 attempts, mark BLOCKED and move on.

4. **Never batch.** "Efficiency" through batching is the anti-pattern that caused every previous QA session to fail. Sequential is correct. One at a time is correct.

5. **Never assume success.** "I just set it so it should be..." NO. Check. Read the state. Verify.

6. **Update the matrix file at every state transition.** PENDING→TESTING, TESTING→FIXING, FIXING→REVIEWING, etc. The file is the checkpoint. If it's not in the file, it didn't happen.

7. **Never use sleep as verification.** `sleep 10` does not mean the setting applied. `ecctl state --json` showing the correct value means the setting applied. The only acceptable sleep is the 5-second UI reaction wait after a setting change.

## Anti-Patterns (from real session failures)

| What Claude Did | Why It Failed | What To Do Instead |
|----------------|---------------|-------------------|
| Shell function `run()` wrapping 10 test steps | Unobservable, can't interrupt, failures cascade | One ecctl call per Bash tool call |
| `sleep 5 && ecctl record && sleep 10 && ecctl stop` | If record fails, stop still runs | Separate calls, check each output |
| "QA: Methodical test - one at a time" with 9 ecctl calls | Self-deception — calling it methodical doesn't make it so | Actually one at a time |
| Background health monitor disconnected from main loop | Findings never influenced test decisions | Health check is step 1 of every test |
| Claiming "You're right" then batching again 5 minutes later | Instructions alone don't prevent relapse | Structural enforcement in the skill |

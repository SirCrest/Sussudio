# Testing Reset Plan

Automation debt was intentionally removed to restart with a smaller and more reliable test stack.

## Current state

- No UI automation harness is active.
- No goal-runner orchestration is active.
- `tools/reliability-gates.ps1` currently validates build health only.

## Rebuild rules

1. Add one automation layer at a time.
2. Keep every automation step timeout-bounded.
3. Prefer deterministic local prerequisites over environment-dependent heuristics.
4. Require one stable smoke scenario before adding matrix scenarios.

## Incremental milestones

1. `M1`: Add one deterministic UI smoke test (launch, refresh, preview start/stop, close).
2. `M2`: Add one deterministic recording smoke test (short record, artifact check, clean shutdown).
3. `M3`: Add diagnostics assertions around drops/errors for the same two flows.
4. `M4`: Add matrix coverage only after M1-M3 are stable for multiple runs.

## Exit criteria for each new test

1. Enforces a hard timeout.
2. Produces readable failure logs.
3. Leaves no orphan process.
4. Can be run by a single command from repo root.

---
name: subagent-consensus-review
description: Run multi-subagent design and verification loops for medium/large feature work, refactors, and reliability fixes. Use when work should be proposed by multiple subagents, merged by explicit consensus (or hybrid), then reviewed by a second subagent swarm for performance, latency, reliability, plan accuracy, and project fit before final sign-off.
---

# Subagent Consensus Review

Use this skill for any non-trivial implementation where multiple approaches are possible or where regressions are costly.

## Core Workflow

1. Define execution contract before coding.
2. Run option-generation subagents in parallel.
3. Merge to a selected option or explicit hybrid by scoring.
4. Implement in bounded increments.
5. Run post-implementation review subagents in parallel.
6. Require convergence or loop with fixes until convergence.

## Phase 1: Pre-Implementation Option Swarm

Run 5 subagents in parallel with distinct ownership:

1. `architecture`: end-to-end shape, boundaries, coupling risks.
2. `api-dataflow`: public interfaces, state transitions, diagnostics contracts.
3. `reliability`: failure modes, retries/timeouts, edge-case safety.
4. `performance-latency`: throughput, hot paths, queue/backpressure risks.
5. `project-fit`: compatibility with existing patterns and maintainability.

Require each subagent to return:

1. `Option A`: preferred implementation.
2. `Option B`: viable alternative.
3. `Hybrid`: if A and B can be combined.
4. `Risks`: top regressions and mitigations.
5. `Validation`: tests/checks needed to prove correctness.

Synthesize using the scoring matrix in `references/templates.md`.

Decision rule:

1. Choose highest-scoring option when clear winner exists.
2. Choose hybrid when it improves score without adding fragile complexity.
3. Record rejected options and why.

## Phase 2: Implementation Discipline

Implement in checkpoints that preserve runnable state where feasible.

After each checkpoint:

1. Run targeted checks relevant to the changed area.
2. Update diagnostics/logging if observability changed.
3. Confirm behavior still matches selected option.

## Phase 3: Post-Implementation Review Swarm

Run a new 5-subagent review pass on the completed diff:

1. `performance-review`: CPU, memory, queue pressure, dropped-frame risk.
2. `latency-review`: end-to-end delay, synchronization, timing drift.
3. `reliability-review`: retries, cancellation, teardown, error paths.
4. `plan-accuracy-review`: matches selected plan and acceptance criteria.
5. `integration-review`: consistency with repo patterns and side effects.

Require each review subagent to return:

1. `Findings`: severity-tagged issues with file/line references.
2. `Verdict`: pass, conditional pass, or fail.
3. `Required Fixes`: concrete edits/tests needed.

Convergence gate:

1. No `fail` verdicts.
2. No high-severity open findings.
3. Conditional findings either fixed or explicitly accepted as tradeoffs.

If not converged, fix and rerun review swarm.

## Orchestration Rules

1. Always use the term `subagents` in status updates.
2. Always wait for all spawned subagents before yielding.
3. Close completed subagents to avoid thread-cap exhaustion.
4. Keep role boundaries strict; avoid duplicate reviews.
5. Prefer parallel subagents for independent checks.

## Output Requirements

When using this skill, deliver:

1. Selected approach summary.
2. Consensus matrix (scores + rationale).
3. Implementation checkpoints completed.
4. Review-swarm findings by severity.
5. Final convergence statement with residual risks.

Use templates in `references/templates.md` for consistency.

---
name: elgatocapture-reliability-review
description: Run and maintain reliability review logs in `docs/reliability-reviews` with seeded five-pass analysis, severity triage, consensus gating, and remediation tracking.
---

# ElgatoCapture Reliability Review

Use this single skill for all reliability-pass tasks. Select the internal feature
based on the user request instead of switching skills.

## Scope

This skill governs reliability review work for:
- `docs/reliability-reviews/README.md`
- `docs/reliability-reviews/CU-*.md`
- `docs/reliability-reviews/CU-AT-*.md`

## Feature Router

Pick one feature path first, then execute it fully.

- Feature A: Seed a new review log
  - Trigger: "create a new CU reliability doc", "start review log", "seed passes"
- Feature B: Update an existing CU review
  - Trigger: "append findings", "add pass results", "refresh impact sweep"
- Feature C: Run targeted pass analysis
  - Trigger: "correctness pass", "concurrency pass", "compatibility pass"
- Feature D: Compute consensus gate
  - Trigger: "approved or blocked", "gate decision", "severity rollup"
- Feature E: Create remediation follow-up
  - Trigger: "spin remediation CU", "track fix unit", "link follow-up work"

## Common Rules

- Keep heading order consistent with existing files.
- Keep language concise, factual, and verifiable.
- If no findings, explicitly write `Findings: none.`
- Include concrete verification commands and outcomes.
- Do not change naming conventions from `README.md`.

## Feature A: Seed A New Review Log

1. Determine CU id:
   - Runtime/app change units: `CU-###`
   - Automation/testing change units: `CU-AT-###`
2. Start from `references/cu-review-template.md`.
3. Populate:
   - Scope
   - Selected Quality Profiles
   - Baseline Verification
   - Seeded 5-Pass Reviews
   - Impact Sweep
   - Consensus Gate
   - Verification Commands And Outcomes
   - Remediation Units
4. Use seed format from ledger:
   - `CU-XXX-P<pass>-<utc_ticks>`
   - `CU-AT-XXX-P<pass>-<utc_ticks>`

## Feature B: Update Existing Review

1. Read the target CU file and preserve style already used in that file.
2. Update only impacted sections.
3. If findings changed severity, update:
   - pass findings lines
   - consensus gate counts
   - remediation section
4. Keep previous rounds intact; append new round blocks.

## Feature C: Run Targeted Pass Analysis

Use these pass lenses:
- Pass 1: Correctness/State (`Spec Lawyer`)
- Pass 2: Concurrency/Lifecycle (`Race Paranoid`)
- Pass 3: Performance/Memory (`Throughput Skeptic`)
- Pass 4: Data Safety/Failure Recovery (`Failure Analyst`)
- Pass 5: Integration/Compatibility (`Contract Auditor`)

For each requested pass:
1. Inspect changed files and related tests/docs.
2. Record findings with severity (`Sev0`..`Sev3`) or `none`.
3. Add at least one verification command unless truly docs-only.

## Feature D: Consensus Gate

Use this gate policy:
- Open `Sev0` > 0: blocked
- Open `Sev1` > 0: blocked
- Open `Sev2` > 0: must be fixed or explicitly accepted with mitigation
- Open `Sev3`: optional cleanup

Write explicit gate summary:
- Open `Sev0`: N
- Open `Sev1`: N
- Open `Sev2`: N
- Verification: pass/fail
- Result: `Approved` or `Blocked`

## Feature E: Remediation Follow-Up

When gate is blocked or a deferred `Sev2` requires tracking:
1. Add a remediation item in `Remediation Units`.
2. Reference the follow-up CU id.
3. Keep the original CU status accurate (do not silently mark approved).

## Fast Commands

- List CU files:
  - `rg --files docs/reliability-reviews`
- Find required sections:
  - `rg -n "## Scope|## Baseline Verification|## Consensus Gate" docs/reliability-reviews/CU-*.md docs/reliability-reviews/CU-AT-*.md`
- Validate seed formatting:
  - `rg -n "Seed: `CU(-AT)?-[0-9]{3}-P[1-5]-[0-9]+(-R[0-9]+)?`" docs/reliability-reviews/CU-*.md docs/reliability-reviews/CU-AT-*.md`
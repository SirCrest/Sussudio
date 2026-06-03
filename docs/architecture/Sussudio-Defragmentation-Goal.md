# Sussudio Defragmentation Goal

This file is the source of truth for the active Codex goal. The `/goal` objective intentionally stays compact and points here so the goal remains within the documented objective limit while preserving a detailed policy for future agents.

## Intent

Re-organize Sussudio into a cleaner, more intentional, better-tested architecture. This is not a decomposition campaign. The current risk is over-fragmentation: one logical behavior can be scattered across too many partial files, tiny implementation files, and incidental helper types. The intended end state is more navigable, not merely smaller or larger.

The core design objective is behavioral locality: an engineer or agent should be able to find, understand, change, and test a behavior by opening a small, obvious cluster of files.

## Known symptom snapshot

Verify these numbers before relying on them as the formal baseline, but treat them as the failure pattern this goal is trying to correct:

- `AutomationDiagnosticsHub` is reported at roughly 217 files.
- `CaptureService` is reported at roughly 109 files.
- `MainWindow` is reported at roughly 95 files.
- `MainViewModel` is reported at roughly 66 files.
- Roughly 44% of `.cs` files are reported as under 60 lines.

These are not targets by themselves. They are evidence that splitting and partial-class sprawl have become counterproductive.

## Success metrics

A slice is successful when it improves ownership, behavior locality, testability, or reviewability without regressing runtime behavior.

The overall goal is complete when:

- Named architectural boundaries exist for the major behaviors listed below.
- Trivial partial fragments have been materially consolidated.
- The net production `.cs` file count is below the captured baseline.
- The largest partial-type clusters have been reduced to cohesive files or real collaborators.
- Ordinary feature or bug review usually requires no more than about five primary production files, and no more than about ten total files including tests and docs.
- Build, focused tests, and relevant runtime or CLI/MCP checks pass for each completed slice.
- Architecture notes and playbooks reflect the new layout.
- Remaining changes would be cosmetic churn.

Do not merge unrelated code merely to reduce file count. Do not split cohesive code merely to reduce line count.

## File-size policy

Line count is a context-window guardrail, not an architectural boundary.

- A cohesive 300-800 line implementation file is normally acceptable.
- An 800-1200 line file is a review smell and should be inspected, but it is not an automatic split trigger.
- A file over 1200 lines needs an explicit locality/testability rationale if left as-is.
- A sub-80-line production file is a smell unless it is a meaningful leaf type, adapter, option object, enum, interface, record, generated/designer artifact, XAML companion, test fixture, or otherwise independently useful concept.
- Test files may increase when they create meaningful deterministic coverage, but avoid test fragmentation for its own sake.

Prefer a cohesive 650-line file over 30 tiny partials that must be mentally reassembled to understand one behavior.

## Partial-class policy

`partial class` is not an architectural boundary.

Use partial classes only for:

- Generated, designer, XAML, or source-generated code.
- Platform-specific splits.
- A genuine two- or three-way split of one cohesive type where the split is stable and obvious.

Otherwise, either consolidate the fragments into a cohesive file or extract a named collaborator with a clear responsibility and a useful test seam.

Do not create additional partial fragments unless the slice also reduces net sprawl and improves locality.

## Priority areas

Prioritize the areas where scattered ownership blocks testing, review, or safe change:

1. Shared automation contracts currently linked from tools/Common source.
2. Stale or misleading architecture/testing docs.
3. `MainWindow` partial concerns, moved into named controllers where those controllers represent real UI behavior boundaries.
4. `MainViewModel`, moved toward feature-oriented view models and ports, with compatibility facades used only as temporary scaffolding and removed after callers migrate.
5. `CaptureService`, with a capture transition state machine introduced before moving capture resources.
6. `AutomationDiagnosticsHub`, diagnostic-session code, Flashback, recording, and preview-renderer code, reshaped into a small set of named, independently-testable collaborators.
7. Over-fragmented partial sprawl in `FlashbackPlaybackController`, `D3D11PreviewRenderer`, and similar classes.

## Testing and agent-QA requirements

Testing is a first-class architectural outcome. The codebase should become easier for both humans and agents to verify.

Prefer deterministic build/test/CLI/MCP or pipe-driven checks over brittle UI-only testing. Where practical, expose dev-only automation seams that let agents:

- Create or load realistic scenarios.
- Drive behavior through stable commands.
- Inspect state without private reflection hacks.
- Toggle relevant feature flags or settings.
- Trigger capture, recording, Flashback, preview, export, diagnostics, and background-job behavior.
- Capture evidence such as logs, frames, screenshots, state snapshots, or structured assertions.
- Verify expected results through machine-readable output.

Do not widen public automation contracts or command IDs casually. Preserve existing public command names, IDs, XAML bindings, recording semantics, Flashback behavior, HDR semantics, and optimized capture/preview/recording hot paths unless a measured improvement is proven.

## Required workflow per slice

Before editing:

1. Identify the behavior/locality/testability problem being addressed.
2. Inspect the current files and call graph enough to avoid cosmetic churn.
3. Check the baseline notes for current file-count and partial-cluster risks.
4. Define the expected reduction in scattering or the new named boundary being introduced.

During editing:

1. Keep commits coherent and rollback-friendly.
2. Prefer consolidation of trivial fragments before introducing new files.
3. Introduce new files only for real named concepts with stable ownership.
4. Remove temporary facades once callers have migrated.
5. Avoid changing hot paths unless the change is measured and justified.

After editing:

1. Run the relevant build and focused tests.
2. Run the relevant CLI/MCP, pipe, or runtime smoke checks for behavior-sensitive areas.
3. Record what changed, what was verified, and whether the slice improved locality.
4. Update docs when layout or test seams change.

## Anti-patterns to avoid

- Splitting a class because a line count threshold was crossed.
- Creating many one-method or one-property partial files.
- Treating namespace folders as design boundaries when the behavior remains scattered.
- Leaving compatibility facades in place after the migration is complete.
- Creating new abstractions without tests or clear callers.
- Churning files that do not improve ownership, locality, testability, or reviewability.
- Growing production `.cs` file count while claiming defragmentation succeeded.

## Stop rule

Stop when named boundaries exist, partial sprawl is materially consolidated, the captured file-count baseline has improved, tests and docs reflect the new layout, and further changes would mostly be cosmetic. The goal is a healthier architecture, not endless refactoring.

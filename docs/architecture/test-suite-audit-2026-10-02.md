# Test Suite Audit (2026-10-02)

Question asked: with AI coding agents doing the work, do we still need the xUnit suite, and if so which parts?

Short answer: about three quarters of the suite does the job AGENTS.md says isolated tests are for (wire contracts, failure modes E2E cannot reach, exact logic). About a quarter, roughly 15,000 of 64,900 lines, pins source text, type shape, or happy paths and should go. The suite is cheap to run (under 30 s) but expensive to carry through refactors (0.77 test lines changed per production line). Of 13 injected agent-style mistakes, the suite caught 10, including every hard-rail and wire-contract mutation. The three it missed were a pre-cancelled start, a blocking sleep on a hot path, and a removed lock: one cancellation gap an isolated test could close, and two concurrency hazards that only live checks or targeted stress tests can see.

Nothing in this audit was changed in the suite. It is a review with a recommended plan.

## Method

1. All 115 test files (64,862 lines, 1,359 `[Fact]`/`[Theory]`, 2,321 cases) were read in full by nine parallel reviewers, each classifying every test class against the AGENTS.md testing policy:
   - A: wire contract consumed by external tools (command IDs, pipe protocol, ssctl/MCP payloads, probe byte formats). Keep.
   - B: failure-mode isolation (cancellation, races, timeouts, shutdown, crash recovery, malformed input, fault injection). Keep.
   - C: source-text, file-layout, file-existence, or reflection-shape assertions. Delete (policy forbids them).
   - D: DTO shape / property-name loops not consumed externally. Delete.
   - E: tautological (assert the initializer, assert a constant, duplicate). Delete.
   - F: happy path a live `ssctl` diagnostic session or MCP tool could observe. Convert to E2E or delete.
   - G: exact logic E2E cannot reach deterministically. Keep.
2. Git history was measured for cost (test churn per production line) and for whether past bug fixes needed test changes.
3. A mutation experiment injected 13 single-line, agent-style mistakes into production code one at a time, rebuilt the solution, ran the full suite, recorded which tests failed, and reverted. Driver: `mutation_audit.py` (scratchpad), results below.

## What the suite costs

| Measure | Value | Source |
|---|---|---|
| Test code | 64,862 lines in 115 files | `wc -l tests/Sussudio.Tests/*.cs` |
| Production code | 128,888 lines (`Sussudio/`, `tools/`, contracts) | same |
| Cases per run | 2,321, 0 skipped | `artifacts/validation.json` |
| Run time | build 21-32 s, tests 20-28 s | validate runs at HEAD |
| Test lines changed per production line, last 200 `refactor:` commits | 0.77 | `git show --numstat` |
| Same ratio, last 200 `fix:` commits | 0.05 | same |
| Refactor commits that touched `tests/` | 2,765 of 2,809 | `git rev-list` |
| Source-text assert calls (`AssertContains`/`AssertOccursBefore`/`AssertMemberContains`/`ReadRepoFile` families) | roughly 1,900 across the suite | reviewer counts |

The refactor ratio is the real tax. Nearly every refactor has to drag test changes along, and the reviewers found that most of that drag comes from the C/D/E buckets: tests that break when code moves, not when behavior changes.

History also shows the suite is not a bug detector by itself. The April 2026 commit "fix: 28 bugs across capture, flashback, recording, preview, and audio pipelines" changed tests only to delete 17 lines; the suite of the day was green with 28 bugs present. The March 2026 stale `AutomationClient` CommandMap bug shipped, and the executing wire-contract test that would have caught it was added in September. The tests that now exist in bucket A were mostly written in response to bugs that reached users.

## What the suite catches: mutation results

Each row is one realistic agent mistake, applied alone to a clean tree, built, tested, reverted.

| ID | Mistake injected (one line, production code) | Result | Caught by |
|---|---|---|---|
| M01 | Disable the P010 guard in `MfSourceReaderVideoCapture` (silent NV12 fallback) | Caught | `MjpegPipelineContractsTests.StandardSourceReaderOutput_RejectsNv12WhenP010IsRequired` |
| M02 | Swap the numeric IDs of `SetRecordingFormat` and `SetQuality` | Caught (2 tests) | `AutomationDispatcherTrivialHandlerPayloadFieldsMatchCatalog`, `McpToolSurface…CaptureSettingsToolsRouteProvidedSettings` |
| M03 | Change the wire string `recording-final-output-invalid` | Caught (1) | `RecordingFinalizationTruthTests.RecordingFailureCodes_PinTheWireValues…`, a source-text test and the sole guard |
| M04 | Rename payload key `deviceId` to `device_id` | Caught (2) | `AutomationDispatcherNewAudioAndFlashbackCommandsRoutePayloads`, `McpToolSurface…DeviceToolsRoute…` |
| M05 | Remove `ThrowIfCancellationRequested` at `FlashbackEncoderSink.StartAsync` | Escaped | none; no test starts the sink with a cancelled token |
| M06 | `ShouldResize` width comparison `>=` to `>` (off-by-one) | Caught (2) | `PreviewOutputSizePolicyTests.ShouldResize_TreatsTheThresholdAsInclusive` and the per-axis theory (see build note) |
| M07 | Export out-point eviction boundary `<=` to `<` | Caught (1) | `FlashbackExportPlannerTests.AbsoluteRangeRejectsEvictedOutPoint` |
| M08 | `StatusMessages.PreviewStarted` text changed | Caught (1) | `PreviewStartupSessionControllerPreservesAttemptStateContracts` (pins UI copy; a cost when the change is intentional) |
| M09 | Allow the Recording transition from any session state | Caught (1) | `CaptureSessionTransitionPolicyDefinesCoreLifecycleRules` |
| M10 | Pacing classifier `P99OverBudgetRatio` 1.08 to 1.8 | Caught (1) | `AutomationContractsTests` "Preview pacing classifier falls back to render submit…" |
| M11 | `HdrOutputPolicy` HDR-requested test inverted (HDR silently off) | Caught (6) | three `HdrOutputPolicy` tests, both NVENC P010 lifecycle tests, `PreviewRendererStartupPlanBuilderPreservesFallbackPolicy` |
| M12 | `Thread.Sleep(20)` inserted in the WASAPI `WriteAudioAsync` hot path | Escaped | none; hot-path blocking is only guarded by a source grep, never timed |
| M13 | Remove the `lock` around the encoder sink's started flag | Escaped | none; no concurrent-start test |

Readings:

- Hard rails (M01, M11) and wire contracts (M02, M03, M04) are well guarded, usually by more than one test and from more than one direction (dispatcher plus MCP tool surface).
- M03 is a warning for the deletion plan: a bucket-C source-text test is the only thing pinning the recording failure-code strings. Convert such tests to value pins (read the constant by reflection, as `FailureCodeRegistryContractsTests` already does) instead of deleting them outright.
- The escapes are the AGENTS.md hot-path and shutdown rules in action: isolated tests do not time hot paths or race starts. M05 is cheap to close with one pre-cancelled `StartAsync` test. M12 and M13 belong to live pacing checks and targeted stress, not to this suite.
- Per-mutation cost was about 21 s build plus 20 s tests.

Build note: the first pass ran `dotnet test --no-build` after `dotnet build Sussudio.slnx -p:Platform=x64`. The solution build writes the test and contracts assemblies to the AnyCPU output folders (`bin/Debug/net8.0`), so the x64 test assembly under `bin/x64/Debug/net8.0` stayed stale and M06 appeared to escape. Re-running with `dotnet test` compiling the project, as `scripts/validate.ps1` does, caught it. Any script that builds the solution and then tests with `--no-build` has the same trap.

## Composition by bucket (whole suite)

Reviewer counts, approximate. Lines include each bucket's share of wrappers and fixtures.

| Bucket | Methods | Lines | Share of lines | Disposition |
|---|---|---|---|---|
| A wire contract | ~160 | ~6,900 | 11% | Keep |
| B failure-mode isolation | ~450 | ~19,300 | 30% | Keep |
| G exact logic E2E cannot reach | ~415 | ~15,800 | 24% | Keep |
| C source-text / layout / reflection-shape | ~105 | ~6,800 | 10% | Delete |
| D DTO shape loops | ~35 | ~1,700 | 3% | Delete |
| E tautological / duplicate | ~130 | ~3,100 | 5% | Delete |
| F happy path observable live | ~90 | ~4,200 | 6% | Convert to E2E or delete |
| Wrappers, harness, fixtures not attributed | | ~7,000 | 11% | Shrinks with the above |

## Per-cluster verdicts

| Files | Lines | Deletable | Dominant finding |
|---|---|---|---|
| `XUnit.AutomationContractsTests.cs` | 7,601 | ~3,400 (45%) | 43 of 127 methods are pure source greps (1,318 source-text assert calls, 131 `ReadRepoFile`). Dispatcher, pipe-server and `CopyYuvWithStride` tests are real. |
| `XUnit.PresentationPreviewContractsTests.cs`, `MainWindowControllerOwnershipTests.cs`, `CaptureServiceOwnershipTests.cs`, `ArchitectureGuardrailsTests.cs` | 7,002 | ~2,550 (37%) | 428 source-text asserts and ~310 "property exists" reflection asserts in PresentationPreview. The two "Ownership" files mostly execute real policy despite their names. |
| `XUnit.ToolContractsTests.cs` | 9,154 | ~2,450 (27%) | Real synthetic-pipe suite for ssctl/MCP/DiagnosticSession. 16 source-grep bodies, the CLI snapshot formatter is golden-tested three times, ~150 lines of dead helpers. |
| `XUnit.CoreRuntimeContractsTests.cs`, `XUnit.ModelContractsTests.cs`, `HarnessCore.cs`, `MjpegPipelineTests.cs` | 8,549 | ~2,250 (26%) | ModelContracts is 60% shape loops, record equality, and stats-text pins. Mjpeg is the strongest file (jitter buffer, lease lifetime, P010 rail). HarnessCore has ~200 dead lines now, ~600 once source-grep tests go. |
| `XUnit.RecordingContractsTests.cs`, `XUnit.FlashbackContractsTests.cs` | 6,966 | ~1,780 (26%) | 21 source-text tests in FlashbackContracts (encoder sink, decoder, playback). Exporter output-transaction and FlashbackBufferManager tests are genuine failure-mode work. |
| Preview / capture / UI controllers (22 files) | 7,560 | ~1,200 (16%) | No source-text tests. 21 happy-path UI-binding tests pin labels like "42%", glyphs, and translate offsets; 26 duplicates. Geometry, pacing, cache, dispatch, cleanup-barrier tests are solid. |
| Recording / native / process (26 files) | 6,535 | ~750 (11%) | Every hard-rail guard is here and is kept: P010 mismatch rollback, no preset fallback, no native-probe fallback, HDR verifier. `RecordingFinalizationTruthTests` pins failure-code strings via source text (205 lines). |
| Flashback behavior + device discovery (18 files) | 6,403 | ~600 (9%) | 85% legitimate fault/race/cancellation tests on real `FlashbackBackendResources`, mailbox, planner. Delete `XUnit.FlashbackSinkHardeningTests` and `XUnit.FlashbackUiHealthTests`; fold `FlashbackFailureIdentityTests` into `XUnit.FlashbackFailureTests`. |
| Diagnostic / automation / MCP (16 classes) | 5,094 + 2.7k JSON | ~475 + 300 JSON (9%) | Golden fixtures (807-field snapshot, 159-field timeline) are real wire contracts read by ssctl, MCP and `assert_snapshot`. Command-ID table is frozen in four places; keep one. |
| Total | 64,864 | ~15,450 (24%) | |

## Recommended plan

Ordered by risk, lowest first. Each step is a self-contained PR that leaves the suite green.

1. **Delete bucket C (~6,800 lines, no behavior coverage lost).** Every `AssertContains(sourceText, ...)`, `AssertOccursBefore`, `AssertMemberContains`, `ReadRepoFile` body, XAML `AutomationId` grep, csproj/ps1 grep, PE/IL metadata walk, and `GetProperty(...) != null` loop. Then remove the orphaned HarnessCore source-scanning engine (lines ~565-830), `RuntimeContractSource` readers, `ReadMainWindow*`/`ReadCaptureService*` helpers, `ConfigPropertySpec` family, and the two dead `MainWindow*Source` classes. Three guards need a decision first (see Risks).
2. **Delete buckets D and E (~4,800 lines).** Shape loops, record equality/hash tests, default-value snapshots, enum-name arrays, and the duplicates listed by the reviewers: command-ID table frozen in 4 places, formatter goldens in 3, live-edge rotation message in 3, "env var selects pipe" in 4, export-failure identity in 2 files, force-rotate happy path in 2.
3. **Replace bucket F with E2E (~4,200 lines).** The pieces worth converting, each as a diagnostic-session or MCP check that leaves an artifact under `artifacts/`:
   - dispatch every manifest command in a session and assert none returns unknown-command (replaces `AllCommandKinds…Handled`);
   - `ssctl --help` once (replaces the 179-line help-text test);
   - `flashback-export-playback` scenario plus `verify_recording` (replaces the 186-line scripted fake-app runner test);
   - `control_recording` plus `verify_recording` for the H264/HEVC verifier happy paths;
   - `assert_snapshot` for status text, window title, stats labels, recording-settings application;
   - a UIA tree walk for `AutomationId`s, or drop them.
   Control-to-ViewModel routing (a click reaching the VM) is only covered by the fake-WinUI F tests today; write the UI-driving E2E before deleting those.
4. **Structural fix for the keepers.** Most bucket B/G tests reach production through `Assembly.LoadFrom` plus private field names (`SetPrivateField` is used 65 times in `MjpegPipelineTests` alone; a 26-arg constructor is found by arity). Link the pure policies with `<Compile Include>` as `PreviewOutputSizePolicyTests` already does, and use the existing `InternalsVisibleTo` seam instead of name strings. This removes the 4-line `Program` wrapper per test (~200 lines) and most of the refactor tax that survives steps 1-3.

## Keep list: tests that earn their place

- Hard rails: `CaptureServiceRejectsP010NegotiationMismatchAndRollsBack`, `ValidateOptions_RejectsHdrWithH264`/`RejectsHdrWithoutP010`, `RecordingVerifier_DetectsHdrColorimetryMismatch`, the 5 `HdrOutputPolicy` tests (fold into one Theory), the standard-reader NV12-when-P010-required rejections in `MjpegPipelineTests`, no-preset-fallback and no-native-probe-fallback tests in `NativeFfmpegCapabilitiesTests`.
- Wire contracts: `AutomationCommandCatalogTests` frozen ID table and append gate, failure-code registries, `AutomationSnapshotRegressionTests` and PerformanceTimeline goldens, MCP error envelope, PresentMon CSV parser, `XUnit.KsExtensionUnitNativeTests` golden request bytes, `XUnit.NativeXuProbePayloadContractsTests`, ssctl command routing.
- Failure modes: `FlashbackBackendCleanupTests` (recovery segments survive purge and disposal, the napkin trap), `XUnit.FlashbackRecordingBoundaryTests` (fence under queued work), exporter output-transaction races, `XUnit.CaptureCleanupBarrierTests`, `XUnit.DiagnosticCycleLifetimeTests`/`ExportCleanup`/`RangeFailure`/`Cancellation` (cancel-at-step and transport faults a live session cannot inject), logger saturation and I/O failure, WASAPI failed transitions, `RecordingRecoveryDiscoveryTests`, `InProcessRecordingStructureVerifierTests` over the committed malformed fixtures.
- Exact logic: `FrameTimeGraphGeometryTests`, `PreviewPacingSlowStageClassifierTests`, `PreviewInputViewCacheTests`, `FlashbackPlaybackMailboxTests` coalescing, `XUnit.FlashbackExportPlannerTests`, `AtomicCounterTests` (CAS race with exactly 333 successes), `MfSourceReaderFrameStrideTests`.

## Risks and decisions needed before deleting

- `ArchitectureGuardrailsTests` lines 137-195 grep the native RTK shim for `MUTATING_UNVERIFIED_FORWARD(rtk_burnToFlash)`. It is a source-text test, and it is also the only automated check that the shim blocks destructive firmware and EDID writes. Replace with a behavioral native test or consciously accept losing the guard.
- `MainWindowAutomationIds` and `XUnit.FlashbackUiHealthTests` pin XAML `AutomationId`s. No in-repo consumer references them. Delete unless an external UIA driver (Stream Deck plugin, scripts) depends on them.
- `VerifyFile_RejectsUnknownProfile` in `XUnit.AutomationContractsTests.cs` uses its own test file as the existing-file input; point it at a temp file before any file deletion or rename.
- `RecordingPipelineContractsTests` guarded the "no `Thread.Sleep` in the LibAv sink" rail by grepping source and was removed in the pruning pass, so the suite now has no guard at all for hot-path blocking. Decide whether a timing-based test is worth writing or whether the live pacing checks (`Measure-PreviewScenario.ps1`, `get_frame_pacing_verdict`) own that rail.
- Deleting bucket F UI-binding tests before an E2E exists removes the only control-to-VM routing check.
- Reviewer line counts are estimates from reading, not from running a deletion. Expect the real number to land within about 10% of 15,000.

## What this audit does not prove

The reviewers read, they did not execute, each file; counts are approximate. The mutation set is 13 hand-picked mistakes, not a mutation-testing sweep, so it shows what kinds of errors the suite sees, not a coverage percentage. Automated checks still prove nothing about live capture, audible output, HDR display, or recording playback; those remain live-check territory per AGENTS.md.

## Applied on 2026-10-03

Steps 1 and 2 of the plan were applied to the automation, tool, recording, Flashback, diagnostic, native, and controller test files, with the suite rebuilt and re-run after each stage. The results:

| Measure | Before | After |
|---|---|---|
| Test cases | 2,321 | 1,993 |
| Test source lines | 64,862 in 102 files | 56,232 in 98 files |
| Full validation | green | green (`scripts/validate.ps1`, 0 failed, 0 skipped) |

What changed:

- Whole files removed: `XUnit.FlashbackSinkHardeningTests.cs`, `XUnit.FlashbackUiHealthTests.cs`, `FlashbackFailureIdentityTests.cs` (its distinct cases folded into `XUnit.FlashbackFailureTests.cs`), and `XUnit.PreviewRendererPerformanceTests.cs` (its zero-allocation check folded into `PreviewInputViewCacheTests.cs`).
- Source-text, shape, tautology, and duplicate tests removed from `XUnit.AutomationContractsTests.cs` (7,601 to 4,420 lines), `XUnit.ToolContractsTests.cs` (9,154 to 7,386), `XUnit.RecordingContractsTests.cs` (3,807 to 3,116), `XUnit.FlashbackContractsTests.cs` (3,159 to 2,138), `AutomationCommandCatalogTests.cs` (577 to 448), and about thirty smaller files.
- Sole-guard conversions instead of deletions: the recording failure-code and automation error-code pins now read each constant's value by reflection; the three exporter tests that only asserted `Succeeded == false` now assert the expected failure code; `MapCodecName` asserts exact encoder names; `CommandsNoOpBeforeInitialize` fails loudly if a command method disappears; `VerifyFile_RejectsUnknownProfile` uses a temp file; the stale `PreviewStartupState` drift was fixed in `populated.json` instead of being patched in the comparer; `scenarios.json` keeps only the two wire-visible fields per scenario.
- `HarnessCore.cs` lost 322 lines of source-reading helpers that no test called any more.
- `docs/architecture/AGENT_MAP.md` and `tests/Sussudio.Tests/MIGRATION.md` were corrected where they described the removed tests.

Not applied:

- The presentation/ownership/guardrails slice (`XUnit.PresentationPreviewContractsTests.cs`, `MainWindowControllerOwnershipTests.cs`, `CaptureServiceOwnershipTests.cs`, `ArchitectureGuardrailsTests.cs`) and the core/model/MJPEG slice (`XUnit.CoreRuntimeContractsTests.cs`, `XUnit.ModelContractsTests.cs`, `MjpegPipelineTests.cs`). The session's permission classifier refused to delegate those deletions, so roughly 4,700 of the planned lines remain. Known leftovers from the applied slices that point into these files: five source-text bodies in `XUnit.AutomationContractsTests.cs` whose wrappers still live in `XUnit.PresentationPreviewContractsTests.cs`; the body `MainWindowAutomationIds_CoverAgentCriticalSurface` in `MainWindowControllerOwnershipTests.cs`, whose wrapper was removed so the XAML `AutomationId` grep no longer runs; the uncalled `SharedFormatter_RendersRecordingIntegrity` body and the unused `RuntimeContractSource.ReadAutomationPipeClientSource` reader in `XUnit.CoreRuntimeContractsTests.cs`.
- Step 3 (E2E conversions) and step 4 (direct `<Compile Include>` links for the kept reflection tests).

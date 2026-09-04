# Preview performance and stats implementation

Baseline: `b2853a86`, September 4, 2026.

## Execution contract

1. Coordinate preview selection with display readiness. Keep waits off capture
   callbacks, retain bounded queues and shutdown cancellation, and preserve source
   timestamps. Validate pacing choices before changing latency defaults.
2. Share UI stats collection across dock, graph, and detached window. Keep heavy
   sampling separate from graph animation and preserve automation freshness and
   session epochs. Avoid per-animation-frame percentile work and large allocations.
3. Replace the two-Hz graph with a native presentation that can animate at display
   cadence, targeting 60 Hz or higher. Use a timestamped fixed-duration history,
   aligned FPS/frame-time views, honest spikes/gaps, and readable budget markers.
   Stop unnecessary work when hidden or inactive and respect reduced motion.
4. Improve stats readability and replace dependent width animation with compositor
   motion. Preserve docking, fullscreen, DPI, and final preview sizing behavior.
5. Reduce recurring GPU input-view creation with bounded explicit ownership and
   reset handling. Measure repeated texture use and investigate shared-context
   costs before undertaking more complex device separation.

Recording, Flashback ordering, selected codecs, HDR, audio, capture negotiation,
and automation command IDs/response fields must remain compatible. Changes must
update ownership tests and AGENT_MAP with their implementation slices.

## Validation contract

- Focused behavior tests for sampling, timestamp history, graph geometry, pacing,
  and bounded resource reuse, with relevant performance probes.
- Full solution/tool build, xUnit suite, offline assembly-load harness, generated
  architecture baseline, and git whitespace check after source changes settle.
- Inspect actual UI and exercise available live preview modes when the local
  hardware permits. Record unavailable coverage and do not infer scan-out or
  frame-drop improvement from compilation, averages, or a browser mockup.
- Five design subagents, option scoring, followed by five independent review
  subagents; resolve findings before final sign-off.

## Decisions and evidence

### Design consensus

Scores are engineering judgments on a five-point scale, not benchmark results.
Weights: correctness 30%, reliability 20%, performance 15%, latency 15%, project
fit 10%, simplicity 10%.

| Option | Correctness | Reliability | Performance | Latency | Fit | Simplicity | Weighted |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Minimal XAML graph and duplicated polling cleanup | 4 | 4 | 3 | 3 | 5 | 5 | 3.90 |
| New scheduler, background telemetry bus, GPU device split | 3 | 2 | 4 | 4 | 2 | 1 | 2.80 |
| Native compositor graph, shared sampler, bounded renderer changes | 5 | 4 | 4 | 4 | 5 | 4 | 4.40 |

Selected the hybrid unanimously supported by the five design subagents. Native
Composition line geometry is available in the installed Windows App SDK and
requires neither Win2D nor another swap chain. A fixed timestamped history and
reusable geometry keep sampling separate from compositor scrolling. One UI
sampler feeds existing presentations; automation retains independent fresh reads.

The dock retains its final layout slot and fullscreen contract, but animates an
inner surface using compositor translation and opacity. Preview selection moves
after resize/readiness work, preserving existing stale-frame eligibility and
pacing defaults. A small external input-view cache owns only native references,
clearing before processor resources are released. Broader scheduling and device
separation were rejected because their added synchronization and lifecycle
complexity are not supported by current measurements.

Principal risks are stale completion callbacks, false graph continuity across
sessions, retention of large texture arrays, and reset during a readiness wait.
Validation must exercise these boundaries explicitly. Present-return cadence is
labeled as such; it is not scan-out measurement.

### Implementation and validation

The five implementation checkpoints are complete:

1. **Preview readiness and ownership.** The render thread now performs resize and
   display-readiness work before choosing a queued frame, with stop/reset/session
   checks before rendering. Existing pacing defaults remain: Present interval 1,
   maximum frame latency 1, two swap-chain buffers, queue capacity 4, and waitable
   pacing disabled. When enabled, its existing 8 ms timeout policy remains. DXGI
   wait handles now have explicit `SafeWaitHandle` ownership across resize and
   swap-chain replacement.
2. **Shared stats collection.** `StatsUiSampler` collects labels every 250 ms and
   expensive health/percentile metrics every 500 ms while a visible consumer needs
   them. It fans out one immutable snapshot and rejects stale producer epochs.
   Graph animation does not collect stats. `StatsWindow` accepts the shared
   subscription and pauses when minimized; the current application has no
   construction site for that detached window, so this change adds no launch flow.
3. **Native graph.** A 4,096-entry history feeds aligned FPS and frame-time plots
   covering ten seconds. Geometry updates at 10 Hz while native Composition moves
   it at display cadence, targeting 60 Hz or higher. Sessions, redraw exclusions,
   missing samples, and long gaps break the trace; budget lines and amber overflow
   preserve spike information. Hidden/inactive windows pause work, and reduced
   motion stops continuous scrolling. Labels identify present-call cadence rather
   than implying scan-out timing or source visual changes.
4. **Stats presentation and motion.** Capture, preview, and recording summaries
   sit above expandable details. Dark-theme contrast, numerical scale labels,
   explicit estimated latency, and the P99-equivalent label improve readability.
   Missing latency displays an em dash. The dock keeps its final layout slot and
   animates an inner surface through Composition translation and opacity;
   generation checks protect rapid reversals and fullscreen transitions.
5. **Bounded input-view reuse.** An eight-entry cache reuses video-processor input
   views by retained native texture identity and normalized subresource. It owns
   its COM references, evicts explicitly, and clears before processor teardown.
   `SUSSUDIO_PREVIEW_INPUT_VIEW_CACHE=0` disables it for comparison (default is 1).
   Infrequent `D3D11_PREVIEW_INPUT_VIEW_CACHE` logs report hit/miss, eviction,
   creation count, and creation duration. GPU device separation remains deferred.

The architecture map, test ownership checks, test inventory, and generated
baseline were updated with these boundaries. Recording production code and
automation command contracts were not changed. Final validation exposed an
existing test race: two recording test classes replaced the same process-wide
recovery-directory variable concurrently. Their tests now share an isolated xUnit
collection, preserving the assertions and production recovery behavior.

### Validation results

Final checks on September 4, 2026:

| Check | Result |
| --- | --- |
| Full solution build, x64, no restore | Passed; 0 warnings, 0 errors |
| Full xUnit suite | Passed; 1,174 tests, 0 failures, 0 skipped |
| Offline assembly-load harness | Passed |
| Architecture baseline regeneration | Passed |
| `git diff --check` | Passed |
| Native UI without a signal | Passed fullscreen hide/restore, six alternating dock toggles, minimize/restore, graph visibility |
| Observation script | Fake-ssctl successful collection and expected-setting mismatch rejection passed |

Focused tests cover incremental history and gaps, 60/120/240 Hz geometry, sampler
demand and epochs, dock reversal, wait-handle disposal, bounded cache reuse,
eviction/exception cleanup, and source ownership. Native UI inspection used the
actual WinUI app at 200% DPI, including its existing 1,500 by 900 DIP minimum size.
The app was closed after inspection; saved settings matched the pre-inspection
snapshot. No new stats/graph failures were logged during those checks.

Release-mode managed helper measurements used .NET 8.0.30 x64, 100,000 warmup
iterations, and five runs of one million operations:

| Operation | Result |
| --- | --- |
| Cached texture-description lookup plus input-view hit | 23.9–37.8 ns per operation; median 35.5 ns; 0 allocated bytes |
| History append plus incremental copy every 64 samples | 31.2–32.3 ns per operation; 0 allocated bytes |
| 10,000 unique cache resources | Retained 8; evicted/disposed 9,992; all 10,000 disposed after repeated clear |

These are isolated helper measurements, not before/after application benchmarks.
They measure neither GPU view creation nor rendering throughput. Local raw
evidence is under the ignored `artifacts/preview-performance/` directory:
`build.log`, `full-tests.log`, `test-results/preview-full.trx`,
`offline-harness.log`, `ui-checks.log`, and `renderer-helper-results.json`.
The native screenshot `final-stats-graph.png` records the layout before the final
zero-latency-to-em-dash text correction.

### Independent review and convergence

Five design reviews supported the selected hybrid. Five subsequent review
perspectives covered performance, latency, reliability, plan accuracy, and project
fit/integration. Findings were resolved as follows:

| Finding | Correction and evidence |
| --- | --- |
| P2: a fullscreen-hidden dock could continue sampling because its saved toggle stayed checked | Track effective dock visibility separately; demand regression and native fullscreen check passed |
| P3: graph clock started behind the geometry timestamp after a rebuild | Start animation at elapsed QPC time measured after rebuilding geometry; latency rereview passed |
| P3: architecture map omitted cache ownership and used an incorrect test identifier | Correct exact ownership paths and test inventory |
| P3: graph footer displayed zero for missing latency | Use the common latency formatter; test positive cadence with unavailable latency |

The final review state has no remaining required code fixes. Signal-dependent
validation limits below remain explicit; static review is not evidence of a
measured reduction in frame drops.

### Remaining measurement limits and repeatable follow-up

The user confirmed that an active signal was unavailable. Live frame-drop rates,
P99 present cadence, scan-out latency, GPU cache savings/retained bytes, and loaded
compositor 60/120 Hz delivery could not be verified. Recording/Flashback/HDR under
load and cross-monitor DPI transitions also remain untested. The cache is bounded
by entries, but each retained texture may reference a large texture array.

Once a moving signal is available, collect matched baseline/current runs with the
same source, display refresh rate, duration, and recording/Flashback state. Compare
graph hidden/visible and dock hidden/visible, then repeat with input-view caching
disabled and enabled using the launch environment. Use cache logs to confirm reuse
before interpreting timing changes. The observation helper uses existing ssctl
diagnostics and optionally PresentMon; it rejects an inactive preview and can
check expected pacing settings:

```powershell
./scripts/performance/Measure-PreviewScenario.ps1 -Label current-graph-on `
    -ScenarioDescription '1080p59.94 on 60 Hz, SDR, graph/dock visible, Flashback off' `
    -ExpectedWaitable 0 -ExpectedMaxFrameLatency 1 -ExpectedBufferCount 2 -PresentMon
```

Pacing defaults should change only after those matched measurements demonstrate
better tail latency without unacceptable drops or increased preview delay.

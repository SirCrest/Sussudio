# Sussudio Agent Map

Last reviewed: 2026-05-12.

This file maps the current repo shape to named owners, entry points, invariants,
and fast checks. It is intentionally mechanical so future agents can find the
right file without guessing from old chat transcripts.

## High-Risk Large Files

These files are allowed to be large today, but they are not good expansion
targets. Prefer extracting new behavior into a named collaborator or feature
folder.

| Area | Current large files | Preferred next owner |
|------|---------------------|----------------------|
| Diagnostic sessions | `tools/Common/DiagnosticSessionRunner.cs` | scenario catalog, result formatter, plus per-scenario runners |
| Offline regression harness | `tests/Sussudio.Tests/Program.cs` | xUnit slices and focused contract tests such as `StatsPresentation.Contract.Tests.cs` |
| Capture runtime | `Sussudio/Services/Capture/CaptureService.cs`, `CaptureService.Snapshots.cs` | transition state machine, snapshot builder, resource managers |
| Automation diagnostics | `Sussudio/Services/Automation/AutomationDiagnosticsHub.cs` | diagnostic collectors and evaluation policies |
| Recording | `Sussudio/Services/Recording/LibAvEncoder.cs`, `LibAvRecordingSink.cs` | encoder option policy, sink lifecycle, verifier/finalizer |
| Flashback | `FlashbackPlaybackController.cs`, `FlashbackEncoderSink.cs`, `FlashbackExporter.cs` | playback, buffer, encoder, export modules |
| Preview rendering | `D3D11PreviewRenderer.cs`, `D3D11PreviewRenderer.Rendering.cs` | renderer host, present cadence, screenshot capture, timing models |
| UI shell | `MainWindow.*.cs` partial family | named controllers under an app shell folder |
| Presentation | `MainViewModel.*.cs` partial family | feature view models behind the root facade |

## Automation

Primary owner: `Sussudio.Automation.Contracts/`

Entry points:

- `AutomationCommandKind.cs` owns numeric command IDs. Append only; never
  renumber or reuse values.
- `AutomationCommandCatalog.cs` owns command metadata, payload shape, readiness
  gating, timeout policy, path policy, CLI help, and MCP descriptions.
- `AutomationPipeProtocol.cs` owns pipe names, auth env var, manifest revision,
  command resolution, and request envelope shape.
- `AutomationPipeSecurityPolicy.cs` owns the fallback-security predicate shared
  by app and tests.

Do not reintroduce linked source for these files from `tools/Common`. Consumers
should reference `Sussudio.Automation.Contracts`.

Fast checks:

```powershell
dotnet build Sussudio.slnx -p:Platform=x64 --no-restore
dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore
```

## Capture Runtime

Primary current owner: `Sussudio/Services/Capture/`

Important entry points:

- `CaptureSessionCoordinator.cs` serializes lifecycle mutations.
- `CaptureSessionTransitionPolicy.cs` owns pure transition legality and
  steady-state resolution for `CaptureService`.
- `CaptureService.cs` still owns too many resource lifetimes and should not
  receive unrelated UI, Flashback, or diagnostics behavior.
- `CaptureService.Snapshots.cs` builds runtime snapshots consumed by UI and
  automation.

Invariants:

- Starting or stopping recording must not restart live preview unless the
  transition explicitly requires it.
- Capture lifecycle legality should be expressed in
  `CaptureSessionTransitionPolicy`, not scattered through ad hoc boolean checks.
- Mutating capture lifecycle state should go through serialized coordinator or
  transition-lock paths.
- Snapshot display state should be derived from service/runtime snapshots, not
  hand-updated independently in multiple event handlers.

## Flashback

Primary current owner: `Sussudio/Services/Flashback/`

Entry points:

- `FlashbackBackendResources.cs` owns backend resource grouping.
- `FlashbackBufferManager.cs` owns segment retention and buffer state.
- `FlashbackPlaybackController*.cs` owns playback and scrub control.
- `FlashbackExporter.cs` owns export path validation and temp-file finalization.

Invariants:

- Disable means the timeline should be hidden/locked out.
- Scrub frames must not contaminate live/playback cadence metrics.
- Export must not overwrite without the explicit force path.

## UI Shell And Presentation

Primary current owners:

- `Sussudio/MainWindow.*.cs` for shell, renderer, fullscreen, screenshots,
  animations, and window lifecycle.
- `Sussudio/Controllers/FullScreenController.cs` owns fullscreen transition
  state, overlay reparenting, button state, and auto-hide timer behavior. Keep
  `MainWindow.FullScreen.cs` as the XAML-facing adapter and Flashback shortcut
  bridge.
- `Sussudio/Controllers/WindowScreenshotController.cs` owns automation whole-
  window screenshot dispatch, native PrintWindow capture, and PNG/BMP encoding.
  Keep `MainWindow.Screenshot.cs` as the `IAutomationWindowControl` adapter.
- `Sussudio/Controllers/PreviewScreenshotController.cs` owns the XAML preview-
  frame screenshot button workflow: output directory fallback, file naming,
  preview-frame capture, status text, logging, and button enable/disable state.
  `MainWindow.PreviewScreenshot.cs` is the XAML-facing adapter.
- `Sussudio/Controllers/WindowAutomationController.cs` owns window geometry
  automation plus the recordings-folder command. `MainWindow.WindowAutomation.cs`
  is the `IAutomationWindowControl` adapter; recording-aware close handling
  stays with `MainWindow.CloseLifecycle.cs`.
- `Sussudio/MainWindow.Startup.cs` owns first-load startup, first-frame
  uncloaking, initial ViewModel/device refresh, automation pipe hosting, and
  the launch entrance trigger. Close/finalize behavior stays in
  `MainWindow.CloseLifecycle.cs`.
- `Sussudio/MainWindow.WindowSizing.cs` owns top-level shell resize telemetry
  for preview compositor transforms. `MainWindow.PreviewRenderer.cs` owns
  preview renderer instances, frame counters, expected-present interval, and
  renderer cadence state. `MainWindow.PreviewSurface.cs` owns preview surface
  sizing, GPU panel visibility, and video/control-bar composition shadows.
- `Sussudio/MainWindow.PreviewRuntimeSnapshot.cs` owns the UI-thread automation
  preview snapshot provider that dispatches to the renderer/startup snapshot
  projection, plus the read-only preview runtime snapshot construction.
  Close/finalize behavior stays with `MainWindow.CloseLifecycle.cs`.
- `Sussudio/MainWindow.WindowTitle.cs` owns window title base/build-stamp
  formatting and the recording-time suffix used by property changes.
- `Sussudio/MainWindow.CloseLifecycle.cs` owns `AppWindow.Closing`,
  automation close completion, and recording-aware pre-close protection.
- `Sussudio/MainWindow.ShutdownCleanup.cs` owns `Closed` shutdown cleanup:
  timer stops, event detaches, preview shutdown, automation diagnostics disposal,
  NVML disposal, and ViewModel disposal.
- `Sussudio/MainWindow.NativeWindow.cs` owns native `AppWindow` lookup and DWM
  cloak/dark-mode helpers used by shell startup and automation controllers.
- `Sussudio/MainWindow.Dispatching.cs` owns UI-thread enqueue helpers and
  guarded async event-handler execution used by automation adapters and XAML
  event handlers.
- `Sussudio/Controllers/AudioMeterController.cs` owns audio/microphone meter
  smoothing, timer lifetime, peak/range markers, and meter clip rendering.
  Keep microphone row layout animation in `MainWindow.Bindings.cs` until that
  binding surface is split separately.
- `Sussudio/Controllers/StatsOverlayController.cs` owns stats dock visibility,
  frame-time overlay visibility, polling lifetime, and dock show/hide
  animations. `MainWindow.StatsOverlay.cs` still owns metric text projection
  and snapshot assembly for now.
- `tests/Sussudio.Tests/StatsOverlay.Contract.Tests.cs` owns legacy harness
  contract checks for stats overlay lifecycle wiring, source-telemetry panel
  projection, and diagnostic row pooling.
- `Sussudio/Controllers/StatsDiagnosticRowsController.cs` owns dynamic
  decode/GPU/diagnostic row pools, empty-state rows, group headers, and
  diagnostic row style updates. `MainWindow.StatsOverlay.cs` still owns metric
  text assignment and snapshot assembly for now.
- `Sussudio/MainWindow.FrameTimeOverlay.cs` owns compact frame-time overlay
  text projection and graph line drawing. Keep frame-time canvas math there,
  while `StatsPresentationBuilder` owns the range/sample text policy.
- `tests/Sussudio.Tests/StatsPresentation.Contract.Tests.cs` owns legacy
  harness contract checks for stats presentation and frame-time overlay policy.
  Keep new stats presentation ownership assertions there instead of growing
  `tests/Sussudio.Tests/Program.cs`.
- `Sussudio/MainWindow.StatsHardwareSections.cs` owns decode and GPU stats
  row projection. It should gather current MJPEG/NVML values and delegate row
  element reuse to `StatsDiagnosticRowsController`.
- `Sussudio/Controllers/FlashbackTimelineController.cs` owns Flashback
  timeline visibility, lockout, toggle synchronization, and show/hide
  animation state. `MainWindow.FlashbackTimeline.cs` is the XAML-facing
  adapter; scrub/playback commands remain in `MainWindow.Flashback.cs`.
- `Sussudio/MainWindow.FlashbackScrub.cs` owns active Flashback pointer-scrub
  state, scrub throttling, release/cancel/capture-lost cleanup, and timeline
  fraction/duration geometry helpers used by marker and playhead presentation.
- `Sussudio/MainWindow.FlashbackPlayhead.cs` owns Flashback current-time-
  indicator compositor visuals, magnetic scrub movement, long-horizon linear
  playhead extrapolation, and CTI anchor timing.
- `Sussudio/MainWindow.FlashbackMarkers.cs` owns Flashback marker placement,
  selection-region layout, and compact duration text formatting.
- `Sussudio/Controllers/FlashbackPollingController.cs` owns Flashback status
  and playback-position polling timers. `MainWindow.FlashbackPolling.cs` is the
  XAML-facing adapter; CTI anchor timing stays with playhead motion.
- `Sussudio/Controllers/SettingsShelfController.cs` owns settings shelf
  visibility, the animation gate, and show/hide storyboard construction.
  `MainWindow.SettingsShelf.cs` is the XAML-facing adapter.
- `Sussudio/Controllers/SplashLoadingPhraseController.cs` owns splash phrase
  loading, timer pacing, and two-line text animation. `MainWindow.SplashLoading.cs`
  is the XAML-facing adapter.
- `Sussudio/Controllers/LaunchEntranceAnimationController.cs` owns the splash-
  to-shell launch choreography, initial hidden/scaled shell state, and one-shot
  entrance state. `MainWindow.LaunchEntrance.cs` is the XAML-facing adapter.
- `Sussudio/Controllers/ControlBarAnimationController.cs` owns the control-bar
  button list used by launch entrance animation plus hover/press/release scale
  behavior. `MainWindow.ControlBarAnimations.cs` is the XAML-facing adapter.
- `Sussudio/Controllers/ShellElevationController.cs` owns static shell
  ThemeShadow and translation setup for the control bar and record button.
  `MainWindow.ShellElevation.cs` is the XAML-facing adapter.
- `Sussudio/Controllers/PreviewTransitionAnimationController.cs` owns preview
  shell/content fade and scale transitions, unavailable-placeholder fades, and
  startup/unavailable presentation prep. `MainWindow.PreviewTransitions.cs` is
  the XAML-facing adapter.
- `Sussudio/Controllers/RecordButtonAnimationController.cs` owns the recording
  button circle/pill width morph used by recording state changes.
  `MainWindow.RecordButtonAnimations.cs` is the XAML-facing adapter.
- `Sussudio/Controllers/RecordingButtonActionController.cs` owns the recording
  button command workflow and preview-state logging after a start.
  `MainWindow.RecordingActions.cs` is the XAML-facing adapter.
- `Sussudio/Controllers/LiveSignalInfoController.cs` owns live-signal pill
  visibility state, show/hide debounce timers, and the small scale/fade
  animation. `MainWindow.LiveSignalInfo.cs` is the XAML-facing adapter.
- `Sussudio/Controllers/PreviewAudioFadeController.cs` owns preview-volume
  fade-in/fade-out state, saved target volume, storyboard lifetime, and volume
  save suppression. `MainWindow.PreviewAudioFade.cs` is the XAML-facing adapter.
- `Sussudio/MainWindow.PreviewStartup.cs` owns preview startup state,
  watchdog/telemetry timers, first-visual confirmation, and timeout recovery.
  `MainWindow.PreviewStartupSignals.cs` owns readiness-signal
  collection, missing-signal formatting, and playback-progress diagnostics.
  `MainWindow.PropertyChangedPreview.cs` owns preview-specific ViewModel events
  and property-change projections for preview start/stop/reinit state. Keep
  preview startup fields out of the composition root.
- `Sussudio/MainWindow.PreviewFadeIn.cs` owns delayed reveal after first visual:
  the rendered-frame threshold, fade-in timer, and preview-audio fade start.
  Keep timeout/watchdog recovery in `MainWindow.PreviewStartup.cs`.
- `Sussudio/MainWindow.PreviewStartupOverlay.cs` owns preview-startup loading
  overlay presentation while the app waits for visual confirmation.
- `Sussudio/MainWindow.PropertyChangedRecording.cs` owns recording-specific
  property-change projections for the record button, recording glow, and
  recording-time control lockouts.
- `Sussudio/MainWindow.PropertyChangedFlashback.cs` owns Flashback-specific
  property-change projections for timeline lockout, markers, playhead updates,
  export progress, and settings-control synchronization.
- `Sussudio/MainWindow.PropertyChangedAudio.cs` owns audio and microphone
  property-change projections: audio toggles, monitoring meter state, preview
  volume slider sync, microphone enablement, and microphone volume sync.
- `Sussudio/Controllers/MicrophoneControlsController.cs` owns microphone volume
  slider synchronization, save triggers, shelf enablement, and mic-meter row
  animation state. `MainWindow.MicrophoneControls.cs` is the XAML-facing adapter.
- `Sussudio/Controllers/ResponsiveShellLayoutController.cs` owns control-bar
  label visibility and capture-settings narrow/wide grid placement.
  `MainWindow.ResponsiveShellLayout.cs` is the XAML-facing adapter.
- `Sussudio/Controllers/CaptureSelectionBindingController.cs` owns
  capture/audio/microphone/encoder selection synchronization, collection-change
  debounce, pending-device apply state, and device-audio mode/gain control
  projection. `MainWindow.CaptureSelectionBindings.cs` is the XAML-facing
  adapter.
- `Sussudio/Controllers/CaptureDeviceActionController.cs` owns the capture-
  device refresh/apply button workflows and preserves the explicit apply/reinit
  path. `MainWindow.CaptureDeviceActions.cs` is the XAML-facing adapter.
- `Sussudio/MainWindow.CaptureOptionPresentation.cs` owns presentation-only
  rules for capture option affordances: HDR readiness hints, FPS telemetry
  tooltips, MJPEG decoder count selection/visibility, bitrate mode visibility,
  and audio clipping visibility.
- `Sussudio/Controllers/OutputPathDisplayController.cs` owns recording output-
  path truncation and tooltip updates. `MainWindow.OutputPathDisplay.cs` is the
  XAML-facing adapter used by binding setup and property changes.
- `Sussudio/Controllers/OutputPathActionController.cs` owns recording output-
  path browse/open-recordings button workflows. `MainWindow.OutputPathActions.cs`
  is the XAML-facing adapter.
- `Sussudio/ViewModels/MainViewModel.*.cs` for root presentation state and
  automation-facing compatibility. `MainViewModel.AudioMeters.cs` owns live
  audio/microphone meter callback state; keep callback-thread meter targets
  out of the root facade file. `MainViewModel.AudioRampTrace.cs` owns the audio
  ramp diagnostic ring buffer and sampler; keep preview monitoring call sites
  in `MainViewModel.AudioMonitoring.cs`. `MainViewModel.MicrophoneVolume.cs`
  owns microphone endpoint volume synchronization and persistence.
  `MainViewModel.AudioControls.cs` owns device-native audio mode/gain management.
  `MainViewModel.AudioPropertyChanges.cs` owns audio, microphone, and
  device-audio observable property handlers.
  `MainViewModel.Dispatching.cs` owns shared
  dispatcher enqueue/invoke helpers and preview event fan-out for the partial
  family. `MainViewModel.Runtime.cs` owns live runtime text, timer refreshes,
  recording bitrate display, capture status/error fan-out, and resume cleanup
  callbacks. `MainViewModel.CaptureSettings.cs` owns capture settings
  projection from UI selection and observed runtime/source state.
  `MainViewModel.Capture.cs` owns device initialization, preview start/stop,
  selected-device apply, output-path browsing, and preview reinitialization.
  `MainViewModel.RecordingLifecycle.cs` owns recording toggle serialization,
  graceful stop, emergency stop, and start/stop recording transitions.
  `MainViewModel.RecordingState.cs` owns recording option selections, output
  path, counters, and transition flags.
  `MainViewModel.Disposal.cs` owns bounded teardown, event unsubscription, and
  export-cancellation cleanup.
  `MainViewModel.AutomationSnapshots.cs` owns automation-facing snapshot,
  probe, and options projection. `MainViewModel.FlashbackPlayback.cs` owns
  Flashback playback commands, marker commands, and buffer/bitrate status
  projection. `MainViewModel.FlashbackExport.cs` owns Flashback UI/automation
  export flow, progress/cancellation state, and segment projection.
  `MainViewModel.FrameRateOptions.cs` owns frame-rate option rebuilding,
  source-rate filtering, and automatic frame-rate selection.
  `MainViewModel.FrameRateTiming.cs` owns shared frame-rate timing family,
  rational parsing, source-rate fallback, and preferred-format ranking helpers
  used by frame-rate, resolution, capture-settings, and automation projections.
  Device enumeration and selected-device capability rebuilds stay in
  `MainViewModel.DeviceManagement.cs`.
  `MainViewModel.DeviceFormatProbes.cs` owns late device-format probe
  reconciliation, capability refresh after background probes, and active-preview
  HDR/SDR/session-mismatch retarget checks.
  `MainViewModel.AutoResolutionOptions.cs` owns automatic resolution ranking,
  source-aware auto-selection, and auto-resolved dimension/frame-rate state.
  `MainViewModel.ResolutionSelectionPolicy.cs` owns source-aware, HDR-aware,
  and SDR fallback resolution selection helpers. `MainViewModel.ResolutionOptions.cs`
  owns the resolution dropdown rebuild and effective resolution display/query
  helpers. `MainViewModel.Settings.cs` owns settings load/save and simple
  persistence reactions. `MainViewModel.FlashbackSettings.cs` owns active
  Flashback reactions to recording-format, encoder, buffer, and GPU-decode
  setting changes. `MainViewModel.AutomationUi.cs` owns UI-only automation mutators
  for stats/settings visibility, frame-time overlay display, Flashback timeline
  visibility, show-all capture options, and preview volume persistence.
  `MainViewModel.AutomationCaptureMode.cs` owns automation mutators for
  resolution, frame rate, video format, MJPEG decoder count, and the shared
  reinitialization gate used after active capture-mode changes.
  `MainViewModel.AutomationRecordingSettings.cs` owns recording format,
  encoder preset/quality/split-mode/custom-bitrate, and output-path automation
  mutators. `MainViewModel.RecordingOptionsRefresh.cs` owns startup refresh for
  FFmpeg-backed recording formats and split-encode modes.
  Remaining automation command mutation code stays in `MainViewModel.Automation.cs`.

Refactor direction:

- Keep `MainWindow.xaml.cs` as a shell/composition root over time.
- Prefer named controllers for preview startup, remaining stats projection
  pieces, timeline UI, and other shell behavior that currently lives in
  partials.
- Keep `MainViewModel` as a compatibility facade while moving feature state to
  capture, recording, audio, Flashback, diagnostics, and automation adapters.

## Tooling And Diagnostics

Primary owners:

- `tools/ssctl/` for the preferred CLI.
- `tools/McpServer/` for MCP bridge tools.
- `tools/Common/` for shared tool helpers that are not contracts, including
  pipe client, snapshot formatting, diagnostic sessions, diagnostic scenario
  cataloging, diagnostic-session pipe retry policy, PresentMon probing, and
  shared JSON options.
- `tools/Common/DiagnosticSessionModels.cs` owns diagnostic session options,
  result, and sample DTOs. Keep summary/live JSON shape changes there rather
  than expanding the runner header.
- `tools/Common/DiagnosticSessionJsonArtifacts.cs` owns diagnostic-session JSON
  artifact writing, frame-ledger extraction, and automation response shape
  helpers.
- `tools/Common/DiagnosticSessionRunState.cs` owns diagnostic-session terminal
  exception state, last-stage tracking, live-state breadcrumbs, and
  best-effort artifact write failure recording.
- `tools/Common/DiagnosticSessionBackgroundTasks.cs` owns diagnostic-session
  background task registration, deterministic await/drain order, PresentMon
  task completion, and interrupted-task warning collection.
- `tools/Common/DiagnosticSessionCleanupPolicy.cs` owns cleanup restore
  validation after diagnostic sessions stop recording, preview, Flashback, or
  playback state.
- `tools/Common/DiagnosticSessionFlashbackCycleScenarios.cs` owns Flashback
  diagnostic restart-cycle and encoder-cycle command flows, including export
  verification and preset restoration.
- `tools/Common/DiagnosticSessionMetrics.cs` owns read-only projection from
  diagnostic snapshots into session metrics: source cadence, preview cadence,
  visual cadence, D3D slow-frame summaries, playback command health, and
  counter deltas.
- `tools/Common/DiagnosticSessionFlashbackExports.cs` owns Flashback export
  diagnostic helpers: strict export verification payloads, rotated-export
  segment-count parsing, range-selection cleanup, and the audio-toggle
  companion used by the range export audio-switch scenario.
- `tools/Common/DiagnosticSessionFlashbackExportScenarios.cs` owns Flashback
  export diagnostic command flows: concurrent exports, disable-during-export,
  rotated exports, export during playback, and selection-range exports.
- `tools/Common/DiagnosticSessionFlashbackLifecycleScenarios.cs` owns
  Flashback playback disable/re-enable lifecycle diagnostic flow.
- `tools/Common/DiagnosticSessionFlashbackMetrics.cs` owns read-only
  diagnostic-session Flashback metric projection for recording, playback, and
  export sessions, including the playback result fields copied into
  `DiagnosticSessionResult`.
- `tools/Common/DiagnosticSessionFlashbackPreviewCycleScenarios.cs` owns
  Flashback diagnostic preview stop/restart flows for normal Flashback,
  playback, and recording-backed scenarios.
- `tools/Common/DiagnosticSessionFlashbackRejectedExports.cs` owns Flashback
  rejected-export diagnostic scenarios for inactive buffers and active
  Flashback recording backends.
- `tools/Common/DiagnosticSessionFlashbackRecordingSettingsScenarios.cs` owns
  Flashback recording-settings deferral checks and post-stop preset restore.
- `tools/Common/DiagnosticSessionFlashbackSegmentPlaybackScenarios.cs` owns the
  Flashback completed-segment playback scenario and its recording-assisted
  segment-rotation cleanup helper.
- `tools/Common/DiagnosticSessionFlashbackSegments.cs` owns read-only
  diagnostic-session Flashback segment parsing, completed-segment waits, and
  playable-boundary headroom waits. Do not add state-mutating scenario steps
  there.
- `tools/Common/DiagnosticSessionFlashbackStressScenario.cs` owns the
  Flashback stress and scrub-stress command sequences, playback-command
  thresholds, and audio-master fallback classifier shared by stress
  diagnostics.
- `tools/Common/DiagnosticSessionFlashbackWaits.cs` owns read-only snapshot
  polling waits used by Flashback diagnostic scenarios, including playback
  state, playback warmup, preview active, Flashback active, and Flashback
  recording-ready waits.
- `tools/Common/DiagnosticSessionFlashbackValidation.cs` owns Flashback
  diagnostic-session warning policy for recording, playback, and preview
  scheduler metrics.
- `tools/Common/DiagnosticSessionHealthPolicy.cs` owns diagnostic-session health
  severity, warmup filtering, sparse-cadence tolerance, and tolerated Flashback
  warning classification.
- `tools/Common/DiagnosticSessionSampler.cs` owns snapshot sample collection.
  Preserve its ordering: append the cloned sample before running checkpoint
  callbacks.
- `tools/Common/DiagnosticSessionResultFormatter.cs` owns the human-readable
  diagnostic-session text used by ssctl and MCP. Keep
  `DiagnosticSessionRunner.Format(...)` as the stable compatibility wrapper.
- `tools/Common/DiagnosticSessionText.cs` owns shared diagnostic-session text
  helpers used by the runner, formatter, and validation policies.
- `tools/Common/DiagnosticSessionPipeRetryPolicy.cs` owns diagnostic-session
  connect retry classification and local failure-response envelopes.
- `tools/Common/DiagnosticSessionScenarioPlan.cs` owns normalized scenario
  flags and grouped warning/validation policies used by the runner. Keep new
  scenario booleans there instead of adding string comparisons in
  `DiagnosticSessionRunner`.

Invariants:

- Do not add new automation metadata to tool-specific files if it belongs in
  `Sussudio.Automation.Contracts`.
- Long-running Flashback operations must use catalog timeouts, not hard-coded
  shorter client defaults.
- Diagnostic sessions are evidence surfaces; preserve summary JSON stability
  when refactoring runners.
- Preserve diagnostic-session artifact filenames and JSON shapes when moving
  artifact helpers; tests read `summary.json`, `session-live.json`, samples,
  frame ledger, and timeline outputs.
- Preserve diagnostic-session terminal-state semantics: canceled wins when the
  caller token is canceled, otherwise terminal exceptions fail and clean runs
  complete. `session-live.json` is best-effort breadcrumb output.
- Preserve diagnostic metric projection semantics; these helpers must stay
  read-only over sampled snapshots and must not send automation commands.
- Preserve Flashback metric projection semantics; this helper should only read
  sampled snapshots and derive deltas/statuses, not mutate playback/export
  state.
- Preserve Flashback validation warning thresholds; these warnings feed
  diagnostic-session pass/fail summaries and should stay explainable in result
  text.
- Preserve health policy semantics when moving tolerance logic; warmup filtering
  must still ignore only transient low-severity Flashback startup observations.
- Preserve sampler checkpoint ordering; checkpoint callbacks are allowed to
  observe the sample that was just appended.
- Preserve diagnostic-session background task await order when moving scenario
  tasks; interrupted-task warnings are evidence and should keep stable stage
  names.
- Preserve result text compatibility when refactoring diagnostic-session
  formatting; ssctl and MCP both flow through `DiagnosticSessionRunner.Format`.
- Preserve pipe error-code semantics when refactoring diagnostic-session retry:
  `pipe-access-denied` is permanent, while connect failed/timeout are retried.
- Add new diagnostic-session scenario names in
  `tools/Common/DiagnosticSessionScenarios.cs` before wiring scenario behavior
  into `DiagnosticSessionRunner`.
- Keep diagnostic-session scenario flag derivation in
  `tools/Common/DiagnosticSessionScenarioPlan.cs`; the runner should consume
  named properties instead of comparing normalized scenario strings directly.

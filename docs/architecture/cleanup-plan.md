# Architecture Cleanup Plan

Last reviewed: 2026-05-12.

## Objective

Make the repo feel intentionally laid out and safe to change without moving
capture, preview, recording, Flashback, or automation behavior by vibes alone.
Performance and runtime semantics stay primary; file layout changes must earn
their keep with smaller ownership boundaries and passing checks.

## Completed Slices

Automation contracts have been extracted into `Sussudio.Automation.Contracts/`.
This removes the old linked-source arrangement where app and tools compiled
protocol/catalog files from `tools/Common`.

Changed ownership:

- `AutomationCommandKind.cs`
- `AutomationCommandCatalog.cs`
- `AutomationPipeProtocol.cs`
- `AutomationPipeSecurityPolicy.cs`

Diagnostic session scenario names and scenario-level metadata now live in
`tools/Common/DiagnosticSessionScenarios.cs`; the runner still owns execution
flow and summary writing.

Automation diagnostics now have named partial owners instead of one large hub
body. `AutomationDiagnosticsHub.cs` is the compact field/constructor and
counter state owner. `AutomationDiagnosticsHub.Snapshots.cs` owns snapshot
refresh, read-only snapshot access, and performance-timeline reads.
`AutomationDiagnosticsHub.Alerts.cs` owns alert publication, alert state, event
throttling, Flashback export completion events, and recent event storage.
`AutomationDiagnosticsHub.Evaluation.cs` owns diagnostic lane policy,
performance scoring, alert-detail formatting, and health classifiers.
`AutomationDiagnosticsHub.Hdr.cs` owns HDR truth classification.
`AutomationDiagnosticsHub.Lifecycle.cs` owns start/stop/dispose and the polling
loop. `AutomationDiagnosticsHub.Verification.cs` owns recording/file
verification commands and verification-profile adaptation.

Fullscreen transition mechanics now live in
`Sussudio/Controllers/FullScreenController.cs`. `MainWindow.FullScreen.cs`
remains the XAML event adapter and Flashback keyboard/scrub bridge.

Automation whole-window screenshot capture now lives in
`Sussudio/Controllers/WindowScreenshotController.cs`. `MainWindow.Screenshot.cs`
is only the automation adapter.

Preview-frame screenshot button behavior now lives in
`Sussudio/Controllers/PreviewScreenshotController.cs`.
`MainWindow.PreviewScreenshot.cs` is the XAML-facing adapter for output
directory fallback, file naming, preview-frame capture, status text, logging,
and button enable/disable state.

Window geometry automation and the recordings-folder command now live in
`Sussudio/Controllers/WindowAutomationController.cs`.
`MainWindow.WindowAutomation.cs` is the `IAutomationWindowControl` adapter.
Recording-aware close behavior remains in `MainWindow.CloseLifecycle.cs`.

UI-thread dispatching helpers and guarded async event-handler execution now
live in `Sussudio/MainWindow.Dispatching.cs`. Window close completion and
recording-aware close handling remain in `MainWindow.CloseLifecycle.cs`.

First-load startup, first-frame uncloaking, initial ViewModel/device refresh,
automation pipe hosting, and the launch entrance trigger now live in
`Sussudio/MainWindow.Startup.cs`. Window close completion and recording-aware
close handling remain in `MainWindow.CloseLifecycle.cs`.

Top-level shell resize telemetry for preview compositor transforms now lives in
`Sussudio/MainWindow.WindowSizing.cs`. Preview surface sizing, GPU panel
visibility, and video/control-bar composition shadows now live in
`Sussudio/MainWindow.PreviewSurface.cs`. `MainWindow.PreviewRenderer.cs` keeps
preview renderer instances, frame counters, expected-present interval, and
renderer cadence state.
`Sussudio/MainWindow.PreviewRuntimeSnapshot.cs` owns the UI-thread automation
preview snapshot provider and read-only preview runtime snapshot construction.
Close/finalize handling remains in
`MainWindow.CloseLifecycle.cs`.

Window title base/build-stamp formatting and the recording-time suffix now live
in `Sussudio/MainWindow.WindowTitle.cs`.

Window close lifecycle and native window helpers are now explicit:
`Sussudio/MainWindow.CloseLifecycle.cs` owns `AppWindow.Closing`, automation
close completion, and recording-aware pre-close protection. `MainWindow.ShutdownCleanup.cs`
owns `Closed` shutdown cleanup: timer stops, event detaches, preview shutdown,
automation diagnostics disposal, NVML disposal, and ViewModel disposal.
`Sussudio/MainWindow.NativeWindow.cs` owns native `AppWindow` lookup and DWM
cloak/dark-mode helpers.

Audio and microphone meter rendering now lives in
`Sussudio/Controllers/AudioMeterController.cs`. The broader control-bar binding
and microphone-row animation code remains in `MainWindow.Bindings.cs`.

Capture session transition legality now lives in
`Sussudio/Models/Capture/CaptureSessionTransitionPolicy.cs`. `CaptureService`
uses it before entering a transition and delegates steady-state resolution to
the same pure policy; resource ownership has not moved in this slice.

Capture service source telemetry and observed pixel-format accounting now live
in `Sussudio/Services/Capture/CaptureService.Telemetry.cs`. The root capture
service still owns runtime resources, but telemetry polling, fallback merging,
NTSC frame-rate correction, and pixel-format counters are no longer embedded in
the lifecycle/orchestration file.

Capture audio preview and microphone monitoring now live in
`Sussudio/Services/Capture/CaptureService.Audio.cs`. This includes preview
volume/mute application, audio level events, mic monitor setup/teardown, WASAPI
playback attach/detach, audio-preview start/stop, and live audio input
switching while preserving the root service transition lock.

Explicit capture cleanup now lives in
`Sussudio/Services/Capture/CaptureService.Cleanup.cs`. That file owns the
public cleanup transition, shutdown teardown order, failed Flashback recording
segment preservation, deferred LibAv/unified-video cleanup handoff, WASAPI
capture disposal, mic teardown, telemetry stop, and final session-state reset.

Capture transition coordination and disposal now live in
`Sussudio/Services/Capture/CaptureService.Coordination.cs`. That file owns
`RunTransitionAsync`, steady-state resolution, initialization/disposal guards,
async disposal cleanup, and best-effort semaphore/eviction cleanup helpers used
by the other capture-service partials.

Deferred capture cleanup now lives in
`Sussudio/Services/Capture/CaptureService.DeferredCleanup.cs`. That file owns
Flashback backend/export lock release helpers, deferred Flashback artifact
cleanup after encoder/export drains, deferred unified-video cleanup after LibAv
drains, and the pending LibAv drain reentry guard.

Capture read-only automation probes now live in
`Sussudio/Services/Capture/CaptureService.Probes.cs`. Video source probing,
preview color probing, and preview-frame screenshot waits are separated from
runtime lifecycle mutation code.

Fatal capture and backend failure handling now lives in
`Sussudio/Services/Capture/CaptureService.Failures.cs`. That file owns fatal
error callbacks, last-failure telemetry, GPU device-lost classification, and
the async cleanup launchers that move the service into faulted states.

Flashback-facing capture controls now live in
`Sussudio/Services/Capture/CaptureService.FlashbackControls.cs`. That file owns
public Flashback state, segment access, enable/settings mutations, restarts,
recording-format changes, and encoder-setting cycles while backend resource
construction stays in the Flashback orchestration partial.

Flashback recording policy and session-context helpers now live in
`Sussudio/Services/Capture/CaptureService.FlashbackRecording.cs`. That file owns
Flashback backend ownership checks, audio attach, session-context construction,
frame-rate rational inference, codec/HDR guardrails, encoded-frame forwarding,
and recording topology validation.

Preview sink and MJPEG timing handoff now lives in
`Sussudio/Services/Capture/CaptureService.PreviewPipeline.cs`. That file owns
preview-frame sink attachment, late Flashback playback preview wiring, shared
D3D preview-device handoff, negotiated video getters, and cached MJPEG pipeline
timing details.

Recording integrity policy now lives in
`Sussudio/Services/Capture/CaptureService.RecordingIntegrity.cs`. That file
owns recording/video/audio counter snapshots, baseline deltas, integrity summary
classification, and the structured `RECORDING_INTEGRITY` log line; the snapshot
partial now consumes that policy instead of containing it.

Runtime capture snapshot projection now lives in
`Sussudio/Services/Capture/CaptureService.RuntimeSnapshots.cs`. That file owns
the read-only `CaptureRuntimeSnapshot` DTO construction consumed by UI,
automation, and verification; the general snapshot partial still owns health
snapshot projection and shared helper policy.

Capture health snapshot projection now lives in
`Sussudio/Services/Capture/CaptureService.HealthSnapshots.cs`. That file owns
the large diagnostics/automation health DTO construction for Flashback,
recording, MJPEG, source telemetry, and visual cadence; the general snapshot
partial is now shared helper policy plus the diagnostics-snapshot compatibility
entry point.

Stats dock and frame-time overlay lifecycle now live in
`Sussudio/Controllers/StatsOverlayController.cs`. `MainWindow.StatsOverlay.cs`
still renders metric values and assembles snapshots, but polling, visibility
state, dynamic diagnostic row pools, and dock animations are out of the shell
fields.
Stats overlay lifecycle, source-telemetry panel, and diagnostic row pooling
contract checks now live in
`tests/Sussudio.Tests/StatsOverlay.Contract.Tests.cs`.
Frame-time overlay graph drawing now lives in
`Sussudio/MainWindow.FrameTimeOverlay.cs`; `MainWindow.StatsOverlay.cs` keeps
the stats dock projection and snapshot adapter.
Decode and GPU hardware stats row projection now lives in
`Sussudio/MainWindow.StatsHardwareSections.cs`; row element pooling still
belongs to `StatsDiagnosticRowsController`.
Stats presentation and frame-time overlay contract checks now live in
`tests/Sussudio.Tests/StatsPresentation.Contract.Tests.cs` instead of expanding
the legacy harness body in `tests/Sussudio.Tests/Program.cs`.

Dynamic stats diagnostic row pools now live in
`Sussudio/Controllers/StatsDiagnosticRowsController.cs`. It owns decode/GPU
row reuse, telemetry diagnostics empty state, group headers, and diagnostic row
style updates.

Flashback timeline visibility, lockout, toggle synchronization, and show/hide
animation state now live in
`Sussudio/Controllers/FlashbackTimelineController.cs`.
`MainWindow.FlashbackTimeline.cs` is the XAML-facing adapter; scrub/playback
commands remain in `MainWindow.Flashback.cs`.

Active Flashback pointer-scrub state now lives in
`Sussudio/MainWindow.FlashbackScrub.cs`. It owns scrub throttling,
release/cancel/capture-lost cleanup, and the timeline fraction/duration
geometry helpers that marker and playhead presentation share.

Flashback CTI/playhead compositor state now lives in
`Sussudio/MainWindow.FlashbackPlayhead.cs`. It owns magnetic scrub movement,
long-horizon linear playhead extrapolation, and CTI anchor timing; the broader
Flashback partial keeps command handling and toggle/apply workflows.

Flashback marker placement and compact duration text now live in
`Sussudio/MainWindow.FlashbackMarkers.cs`, including in/out marker visibility,
selection-region layout, and `m:ss` formatting.

Flashback status and playback-position polling timers now live in
`Sussudio/Controllers/FlashbackPollingController.cs`.
`MainWindow.FlashbackPolling.cs` is the XAML-facing adapter; CTI anchor timing
stays with playhead motion.

Settings shelf visibility, the animation gate, and show/hide storyboard
construction now live in
`Sussudio/Controllers/SettingsShelfController.cs`. `MainWindow.SettingsShelf.cs`
is the XAML-facing adapter.

Splash phrase loading, randomized timer pacing, and the two-line splash text
animation now live in `Sussudio/Controllers/SplashLoadingPhraseController.cs`.
`MainWindow.SplashLoading.cs` is the XAML-facing adapter.

Splash-to-shell launch entrance choreography, initial hidden/scaled shell state,
and one-shot playback state now live in
`Sussudio/Controllers/LaunchEntranceAnimationController.cs`.
`MainWindow.LaunchEntrance.cs` is the XAML-facing adapter.

Control-bar button ownership and hover/press/release scale behavior now live in
`Sussudio/Controllers/ControlBarAnimationController.cs`.
`MainWindow.ControlBarAnimations.cs` is the XAML-facing adapter.

Static shell ThemeShadow and translation setup for the control bar and record
button now live in `Sussudio/Controllers/ShellElevationController.cs`.
`MainWindow.ShellElevation.cs` is the XAML-facing adapter.

Preview shell/content fade and scale transitions plus unavailable-placeholder
presentation now live in
`Sussudio/Controllers/PreviewTransitionAnimationController.cs`.
`MainWindow.PreviewTransitions.cs` is the XAML-facing adapter; composition
shadow animation remains in `MainWindow.Animations.cs`.

Record-button circle/pill width animation now lives in
`Sussudio/Controllers/RecordButtonAnimationController.cs`.
`MainWindow.RecordButtonAnimations.cs` is the XAML-facing adapter.

Recording button command execution and preview-state logging after a recording
start now live in `Sussudio/Controllers/RecordingButtonActionController.cs`.
`MainWindow.RecordingActions.cs` is the XAML-facing adapter.

Live-signal pill visibility state, show/hide debounce timers, and the small
scale/fade animation now live in
`Sussudio/Controllers/LiveSignalInfoController.cs`. `MainWindow.LiveSignalInfo.cs`
is the XAML-facing adapter.

Preview-volume fade-in/fade-out state, saved target volume, storyboard lifetime,
and volume save suppression now live in
`Sussudio/Controllers/PreviewAudioFadeController.cs`.
`MainWindow.PreviewAudioFade.cs` is the XAML-facing adapter.

Preview startup state, watchdog/telemetry timers, first-visual confirmation,
and timeout recovery now live in `Sussudio/MainWindow.PreviewStartup.cs`
instead of the composition-root constructor partial. Readiness-signal collection,
missing-signal formatting, and playback-progress diagnostics live in
`Sussudio/MainWindow.PreviewStartupSignals.cs`. This keeps the root shell
focused on wiring while leaving the existing startup state machine behavior
unchanged.
Delayed preview reveal after first visual now lives in
`Sussudio/MainWindow.PreviewFadeIn.cs`; watchdog/timeout recovery remains in
`MainWindow.PreviewStartup.cs`.
Preview startup loading overlay presentation now lives in
`Sussudio/MainWindow.PreviewStartupOverlay.cs`.

Preview-specific ViewModel events and property-change projections now live in
`Sussudio/MainWindow.PropertyChangedPreview.cs`. The broad
`MainWindow.PropertyChanged.cs` dispatcher still routes `PropertyChanged`
notifications, but preview start/stop/reinit choreography has a named owner.

Recording-specific ViewModel property projections now live in
`Sussudio/MainWindow.PropertyChangedRecording.cs`: record-button morphing,
recording glow, and the recording-time lockout state for capture/audio controls.

Flashback-specific ViewModel property projections now live in
`Sussudio/MainWindow.PropertyChangedFlashback.cs`: timeline lockout, marker and
playhead refresh, export progress, and Flashback settings-control sync.

Audio and microphone-specific ViewModel property projections now live in
`Sussudio/MainWindow.PropertyChangedAudio.cs`: audio toggles, monitoring meter
state, preview volume slider sync, microphone enablement, and microphone volume
sync.

Microphone volume slider synchronization, save triggers, shelf enablement, and
mic-meter row animation state now live in
`Sussudio/Controllers/MicrophoneControlsController.cs`.
`MainWindow.MicrophoneControls.cs` is the XAML-facing adapter.

Control-bar label visibility and capture-settings narrow/wide grid placement
now live in `Sussudio/Controllers/ResponsiveShellLayoutController.cs`.
`MainWindow.ResponsiveShellLayout.cs` is the XAML-facing adapter.

Capture, audio, microphone, and encoder selection synchronization now lives in
`Sussudio/Controllers/CaptureSelectionBindingController.cs`. It owns
collection-change debounce, pending-device apply state, and device-audio
mode/gain projection while `MainWindow.CaptureSelectionBindings.cs` keeps the
old method names for `PropertyChanged` and binding setup.

Capture-device refresh/apply button workflows now live in
`Sussudio/Controllers/CaptureDeviceActionController.cs`.
`MainWindow.CaptureDeviceActions.cs` is the XAML-facing adapter and keeps the
explicit apply/reinit path separate from selection synchronization.

Presentation-only rules for capture option affordances now live in
`Sussudio/MainWindow.CaptureOptionPresentation.cs`: HDR readiness hints, FPS
telemetry tooltips, MJPEG decoder count selection/visibility, bitrate mode
visibility, and audio clipping visibility.

Recording output-path truncation and tooltip updates now live in
`Sussudio/Controllers/OutputPathDisplayController.cs`.
`MainWindow.OutputPathDisplay.cs` is the XAML-facing adapter used by binding
setup and property changes.

Recording output-path browse/open-recordings button workflows now live in
`Sussudio/Controllers/OutputPathActionController.cs`.
`MainWindow.OutputPathActions.cs` is the XAML-facing adapter.

Diagnostic session DTOs now live in
`tools/Common/DiagnosticSessionModels.cs`. `DiagnosticSessionRunner.cs` still
owns orchestration and scenario execution, but the public options/result/sample
contracts are separated from runner behavior.

Diagnostic-session result text now lives in
`tools/Common/DiagnosticSessionResultFormatter.cs`. The runner keeps
`Format(...)` as a compatibility wrapper so existing ssctl and MCP callers do
not need to know about the formatter owner.

Shared diagnostic-session text helpers now live in
`tools/Common/DiagnosticSessionText.cs`. Keep cross-cutting string helpers
there instead of reintroducing private duplicates in the runner, formatter, or
validation policy files.

Diagnostic-session pipe retry/error classification now lives in
`tools/Common/DiagnosticSessionPipeRetryPolicy.cs`, keeping access-denied as a
permanent failure and connect failed/timeout responses retryable.

Diagnostic-session JSON artifact helpers now live in
`tools/Common/DiagnosticSessionJsonArtifacts.cs`. The runner still owns the
session lifecycle, but JSON writing, frame-ledger extraction, and snapshot /
verification response extraction have a smaller home.

Diagnostic-session run state now lives in
`tools/Common/DiagnosticSessionRunState.cs`. It owns last-stage tracking,
terminal exception classification, `session-live.json` breadcrumbs, and
best-effort artifact write failure recording while the runner keeps the
scenario flow readable.

Diagnostic-session background task tracking now lives in
`tools/Common/DiagnosticSessionBackgroundTasks.cs`. It owns scenario task
registration, deterministic await/drain order, PresentMon completion, and
interrupted-task warning collection while the runner only starts tasks.

Diagnostic-session scenario flagging now lives in
`tools/Common/DiagnosticSessionScenarioPlan.cs`. It owns normalized scenario
booleans plus grouped warning/validation policy switches so the runner does not
grow direct scenario string comparisons.

Diagnostic-session cleanup restore validation now lives in
`tools/Common/DiagnosticSessionCleanupPolicy.cs`. It owns warnings for preview,
Flashback, and playback state that remain active after the runner attempts
cleanup.

Diagnostic-session Flashback cycle scenarios now live in
`tools/Common/DiagnosticSessionFlashbackCycleScenarios.cs`. They own the
restart-cycle and encoder-cycle command flows, export verification, and preset
restoration while the runner only starts the scenario tasks.

Diagnostic-session sampling now lives in
`tools/Common/DiagnosticSessionSampler.cs`. Keep the sample append before the
optional checkpoint callback so checkpoint failures cannot orphan an unseen
sample.

Diagnostic-session metric projection now lives in
`tools/Common/DiagnosticSessionMetrics.cs`. It owns snapshot-only projections
for source cadence, preview cadence, visual cadence, D3D slow-frame summaries,
playback command health, and reset-aware counter deltas.

Diagnostic-session Flashback export helpers now live in
`tools/Common/DiagnosticSessionFlashbackExports.cs`. They own strict export
verification payload construction, rotated-export segment-count parsing,
range-selection cleanup, and the range export audio-switch companion command
while scenario command sequencing lives in a separate owner.

Diagnostic-session Flashback export scenarios now live in
`tools/Common/DiagnosticSessionFlashbackExportScenarios.cs`. They own
concurrent export, disable-during-export, rotated export, export during
playback, and selection-range export flows while the runner only starts the
scenario tasks.

Diagnostic-session Flashback lifecycle checks now live in
`tools/Common/DiagnosticSessionFlashbackLifecycleScenarios.cs`. They own the
pause/seek/play disable-and-re-enable flow and post-disable playback queue
assertions while the runner only starts the lifecycle task.

Diagnostic-session Flashback metric projection now lives in
`tools/Common/DiagnosticSessionFlashbackMetrics.cs`. It owns snapshot-only
recording, playback, playback-result, and export metric projection while the
runner retains scenario control and validation warning policy.

Diagnostic-session Flashback preview-cycle scenarios now live in
`tools/Common/DiagnosticSessionFlashbackPreviewCycleScenarios.cs`. They own
preview stop/restart flows for normal Flashback, playback, and recording-backed
diagnostics while the runner only starts the scenario tasks.

Diagnostic-session Flashback rejected-export scenarios now live in
`tools/Common/DiagnosticSessionFlashbackRejectedExports.cs`. They own inactive
buffer and active-recording rejection flows, including failure-kind and
post-rejection state assertions.

Diagnostic-session Flashback recording-settings deferral now lives in
`tools/Common/DiagnosticSessionFlashbackRecordingSettingsScenarios.cs`. It owns
preset mutation rejection during Flashback recording plus post-stop preset
verification and restore.

Diagnostic-session Flashback segment playback now lives in
`tools/Common/DiagnosticSessionFlashbackSegmentPlaybackScenarios.cs`. It owns
completed-segment playback crossing plus recording-assisted segment rotation,
while `DiagnosticSessionFlashbackSegments.cs` stays read-only segment parsing
and wait policy.

Diagnostic-session Flashback segment handling now lives in
`tools/Common/DiagnosticSessionFlashbackSegments.cs`. It owns segment DTOs,
`FlashbackGetSegments` parsing, completed-segment waits, and playable-boundary
headroom waits while the runner keeps scenario command sequencing.

Diagnostic-session Flashback snapshot waits now live in
`tools/Common/DiagnosticSessionFlashbackWaits.cs`. They own read-only polling
loops for playback state, playback warmup, preview active, Flashback active,
and recording-ready checks while the runner keeps scenario command sequencing.

Diagnostic-session Flashback stress orchestration now lives in
`tools/Common/DiagnosticSessionFlashbackStressScenario.cs`. It owns the stress
and scrub-stress command sequences, playback-command thresholds,
warm-playback budget, and audio-master fallback classification while the runner
only starts the scenario tasks.

Diagnostic-session Flashback validation now lives in
`tools/Common/DiagnosticSessionFlashbackValidation.cs`. It owns recording,
playback, and preview-scheduler warning thresholds over already projected
metrics while the runner retains scenario orchestration.

Diagnostic-session health policy now lives in
`tools/Common/DiagnosticSessionHealthPolicy.cs`. It owns health severity,
Flashback warmup filtering, sparse cadence tolerances, and tolerated warning
classification while the runner still owns scenario execution and warning
emission.

Remaining `tools/Common` ownership:

- `AutomationPipeClient.cs`
- `DiagnosticSessionBackgroundTasks.cs`
- `DiagnosticSessionCleanupPolicy.cs`
- `DiagnosticSessionFlashbackCycleScenarios.cs`
- `DiagnosticSessionFlashbackExports.cs`
- `DiagnosticSessionFlashbackExportScenarios.cs`
- `DiagnosticSessionFlashbackLifecycleScenarios.cs`
- `DiagnosticSessionFlashbackMetrics.cs`
- `DiagnosticSessionFlashbackPreviewCycleScenarios.cs`
- `DiagnosticSessionFlashbackRejectedExports.cs`
- `DiagnosticSessionFlashbackRecordingSettingsScenarios.cs`
- `DiagnosticSessionFlashbackSegmentPlaybackScenarios.cs`
- `DiagnosticSessionFlashbackSegments.cs`
- `DiagnosticSessionFlashbackStressScenario.cs`
- `DiagnosticSessionFlashbackWaits.cs`
- `DiagnosticSessionFlashbackValidation.cs`
- `DiagnosticSessionHealthPolicy.cs`
- `DiagnosticSessionJsonArtifacts.cs`
- `DiagnosticSessionMetrics.cs`
- `DiagnosticSessionModels.cs`
- `DiagnosticSessionPipeRetryPolicy.cs`
- `DiagnosticSessionResultFormatter.cs`
- `DiagnosticSessionRunState.cs`
- `DiagnosticSessionSampler.cs`
- `DiagnosticSessionScenarioPlan.cs`
- `DiagnosticSessionText.cs`
- `DiagnosticSessionRunner.cs`
- `AutomationSnapshotFormatter.cs`
- `AutomationResponseState.cs`
- `JsonOptions.cs`
- `PresentMonProbe.cs`

## Next Slices

1. Continue splitting diagnostic-session runner by scenario family.

   `tools/Common/DiagnosticSessionRunner.cs` is still large. Scenario catalog
   ownership is extracted; next, move preview, recording, Flashback, and cleanup
   scenarios behind small runner classes. Keep JSON summary shape unchanged.

2. Reduce custom regression harness size.

   `tests/Sussudio.Tests/Program.cs` should keep the legacy runner entry point,
   but checks should keep migrating into focused xUnit files or focused
   partial contract files while the dual-stack harness remains. Continue with
   low-risk contract groups first.

3. Continue converting MainWindow partial concerns into controllers.

   `FullScreen`, automation `Screenshot`, and audio meter rendering are
   extracted. `StatsOverlay` lifecycle, frame-time overlay drawing, and
   hardware stats sections are extracted; next UI candidates are preview
   startup, Flashback timeline UI, and the remaining stats row/snapshot
   projection. Keep XAML bindings stable.

4. Move MainViewModel feature state behind a facade.

   Preserve the root `MainViewModel` public surface while introducing feature
   view models or adapters for capture selection, recording, audio, Flashback,
   diagnostics, and automation. The live audio/microphone meter callback state
   now has a named owner in `MainViewModel.AudioMeters.cs`; keep future meter
   behavior there instead of growing the root facade file. Audio ramp trace
   buffering/sampling now lives in `MainViewModel.AudioRampTrace.cs`; keep the
   preview monitoring call sites in `MainViewModel.AudioMonitoring.cs`.
   Microphone endpoint volume synchronization and persistence now live in
   `MainViewModel.MicrophoneVolume.cs`; device-native audio mode/gain
   management stays in `MainViewModel.AudioControls.cs`. Audio, microphone, and device-audio
   observable property handlers now live in `MainViewModel.AudioPropertyChanges.cs`. Shared
   dispatcher enqueue/invoke helpers now live in `MainViewModel.Dispatching.cs`,
   and live runtime text/timer/status/error handling now lives in
   `MainViewModel.Runtime.cs`. Capture settings projection from UI/runtime state
   now lives in `MainViewModel.CaptureSettings.cs`, leaving
   `MainViewModel.Capture.cs` focused on device/preview/reinitialize
   transitions. Recording toggle serialization, graceful stop, emergency stop,
   and start/stop recording transitions now live in
   `MainViewModel.RecordingLifecycle.cs`. Recording option selections, output
   path, counters, and transition flags now live in
   `MainViewModel.RecordingState.cs`. Bounded teardown and event unsubscription now live
   in `MainViewModel.Disposal.cs`. Automation-facing snapshot/probe/options
   projection now lives in `MainViewModel.AutomationSnapshots.cs`. Flashback
   playback commands, marker commands, and buffer/bitrate status projection now
   live in `MainViewModel.FlashbackPlayback.cs`. Flashback UI/automation export
   flow, progress/cancellation state, and segment projection now live in
   `MainViewModel.FlashbackExport.cs`. Frame-rate option rebuilding, source-rate
   filtering, and automatic frame-rate selection now live in
   `MainViewModel.FrameRateOptions.cs`. Shared frame-rate timing family,
   rational parsing, source-rate fallback, and preferred-format ranking now live
   in `MainViewModel.FrameRateTiming.cs`; keep device enumeration and selected
   device capability rebuilds in `MainViewModel.DeviceManagement.cs`.
   Late-arriving device format probe reconciliation and active-preview retarget
   checks now live in `MainViewModel.DeviceFormatProbes.cs`.
   Automatic resolution ranking, source-aware auto-selection, and auto-resolved
   dimension/frame-rate state now live in `MainViewModel.AutoResolutionOptions.cs`.
   Source-aware, HDR-aware, and SDR fallback resolution selection policy now
   lives in `MainViewModel.ResolutionSelectionPolicy.cs`; keep dropdown rebuild
   and effective resolution display in `MainViewModel.ResolutionOptions.cs`.
   Settings persistence and load/save option restoration stay in
   `MainViewModel.Settings.cs`; active Flashback reactions to recording format,
   encoder quality/preset/split/bitrate, and buffer/GPU decode changes now live
   in `MainViewModel.FlashbackSettings.cs`.
   UI-only automation mutators now live in `MainViewModel.AutomationUi.cs`.
   Recording format, encoder preset/quality/split-mode/custom-bitrate, and
   output-path automation mutators now live in
   `MainViewModel.AutomationRecordingSettings.cs`.
   Capture-mode automation mutators for resolution, frame rate, video format,
   and MJPEG decoder count now live in
   `MainViewModel.AutomationCaptureMode.cs`.
   Startup refresh for FFmpeg-backed recording formats and split-encode modes
   now lives in `MainViewModel.RecordingOptionsRefresh.cs`.
   Keep the remaining command mutation code in `MainViewModel.Automation.cs`.

5. Extract capture resource owners behind the transition policy.

   The policy is now the legality/steady-state owner. The next deeper capture
   slices should keep it authoritative while introducing smaller owners for
   audio graph, recording controller, Flashback backend resources, and video
   pipeline lifetime.

## Guardrails

- Preserve public automation command names and numeric IDs.
- Preserve manifest revision rules in `AutomationCommandKind`.
- Preserve XAML binding names until a focused binding migration changes them.
- Preserve Flashback disable lockout behavior.
- Preserve preview/recording no-restart semantics unless a test proves the
  transition intentionally restarts.
- Run `dotnet build Sussudio.slnx -p:Platform=x64 --no-restore` after each
  structural slice.
- Run the console harness when source ownership, automation, capture, recording,
  or Flashback contracts move.

# Flashback Hardening Investigation — Pause / Scrub / Live-Switch Reliability

Date: 2026-06-10. Scope: `Sussudio/Services/Flashback/*`, `Services/Capture/CaptureService.Flashback.cs`,
`Services/Audio/WasapiAudioPlayback.cs`, `Controllers/Flashback/FlashbackUiControllers.cs`,
`ViewModels/MainViewModel.FlashbackState.cs`. Goal: identify why the system is prone to frame
drops and audio stutters around pause, scrub, and live↔playback transitions, and propose fixes
for implementation by Codex. Findings are static-analysis based; each P0/P1 item includes a
runtime verification step (use `tools/ssctl` + app logs — the subsystem is already heavily
instrumented with `FLASHBACK_*` log tokens and diagnostics counters).

## Architecture recap (for the implementer)

- **Capture side**: `FlashbackEncoderSink` (NVENC) writes 1-second-GOP segments
  (`GopSize = round(fps)`, FlashbackEncoderSink.cs:290-293) rotated on a fixed segment duration,
  registering them with `FlashbackBufferManager` (PTS index, eviction, disk budget).
- **Playback side**: `FlashbackPlaybackController` runs a dedicated `FlashbackPlayback` thread
  (lazy-started). Commands (Seek/BeginScrub/UpdateScrub/EndScrub/Play/Pause/GoLive/Nudge/Stop)
  flow through a bounded channel (256) with latest-intent slot coalescing for Seek/UpdateScrub.
  `FlashbackDecoder` (FFmpeg, D3D11VA or software) decodes; video frames are submitted to the
  preview sink; audio chunks go to `WasapiAudioPlayback` via `AudioChunkCallback`.
- **Pacing**: audio-master clock (`RenderingPtsTicks` extrapolated by wall clock) with wall-clock
  fallback; sleep+spin hybrid with `timeBeginPeriod(1)` and MMCSS registration.
- **UI**: WinUI; status poll 250 ms, playback-position poll 33 ms, scrub pointer events throttled
  to 16 ms; composition-layer playhead extrapolation.

---

## P0 — Audio stutter: prebuffer rewind double-delivers ~180 ms of audio on every resume

**Where**: `FlashbackPlaybackController.PrimePlaybackAudioBuffer` (FlashbackPlaybackController.cs:2189-2289),
`TryRewindPlaybackAudioPrebuffer` (:2291-2323), `RestoreAudioCallback` (:2325-2374).
Call sites: `HandlePlayCommand` (ThreadCommands.cs:541), `HandleSeekCommand` resume (:709),
`HandleEndScrubCommand` resume (:896).

**Mechanism**:

1. On resume, the controller seeks to `resumeTarget`, restores the audio callback gated at
   `resumeTarget`, flushes WASAPI, then calls `PrimePlaybackAudioBuffer`, which decodes up to 96
   video frames to push ≥180 ms of audio (`[target, target+180ms]`) into the WASAPI queue. The
   decoded **video** frames are released immediately (:2252) — not kept.
2. Because the decoder has now advanced ~96 frames past the resume point, the code **rewinds**:
   `TryRewindPlaybackAudioPrebuffer` seeks back to `resumeTarget` and calls
   `RestoreAudioCallback(decoder, resumeTarget.Ticks)`.
3. `RestoreAudioCallback` **resets `_lastAudioPtsTicks` to 0** (:2332) and sets the gate to
   `resumeTarget`. The playback loop then re-decodes from `resumeTarget`, and the audio packets in
   `[target, target+180ms]` pass both the gate (PTS ≥ target) and the monotonic check (prev=0),
   so they are **enqueued a second time** behind the prebuffered copy.

**User-visible effect**: every Play / seek-while-playing / end-of-scrub resume replays ~180 ms of
audio (echo/stutter), then runs ~180 ms behind video. The audio-master clock also regresses
(`RenderingPtsTicks` jumps backward when the duplicate chunks render), which destabilizes pacing
right after resume — the exact moment users report stutters. Drift correction (see P1-3) is far
too weak to recover quickly, so the desync persists into the 100 ms dead band.

**Contrast**: the fMP4-reopen path does this correctly — `RestoreAudioAfterFmp4Reopen`
(PlaybackFrames.cs:1087-1099) explicitly documents gating "at last played position" semantics, and
`TrySwitchToNextSegment` (:957) gates at `_lastAudioPtsTicks` to avoid both gaps and duplicates.

**Fix options** (prefer A; B is the minimal patch):

- **A (removes the rewind entirely, also fixes P1-6)**: stop releasing the prebuffer's video
  frames; enqueue them into the existing `prebufferedFrames` queue, which
  `TryReadNextPlaybackFrame` (PlaybackFrames.cs:596-609) already consumes first. No rewind seek,
  no re-decode, no duplicate audio — video plays out of the queue while the decoder continues
  forward. The queue/cleanup infrastructure (`ClearPrebufferedFrames` on every command,
  thread-exit, etc.) already exists and is plumbed through all call sites — this looks like the
  original design intent. Bound the queue at the 96-frame budget (D3D11VA frames hold decoder
  pool surfaces — verify the decoder pool has ≥96+ surfaces or cap lower, e.g. hold ≤32 frames
  and shrink the audio prebuffer target accordingly).
- **B (minimal)**: after the rewind, gate audio at the **last prebuffered audio PTS**
  (`_lastAudioPtsTicks` captured before the rewind), not `resumeTarget`; or simply don't reset
  `_lastAudioPtsTicks` in `RestoreAudioCallback` for this path so the monotonic check rejects the
  re-decoded duplicates.

**Verify**: resume playback mid-buffer; in logs expect `FLASHBACK_PLAYBACK_AUDIO_PREBUFFER ...
rewound=true` followed by `FLASHBACK_AV_DRIFT` showing drift_ms ≈ +180 within the first second,
and `WasapiAudioPlayback.RenderingPtsTicks` regressing. After fix: drift_ms ≈ 0 after resume and
no PTS regression.

---

## P0 — Live frame drops: multi-GB file deletes and File.Exists run under `_indexLock` on the encoder frame callback

**Where**: `FlashbackBufferManager.UpdateLatestPts` (FlashbackBufferManager.cs:248-293) →
`EvictOldestSegments` (:1354-1413) → `DeleteFileForEviction`/`File.Delete` — all **under
`_indexLock`**, invoked from `FlashbackEncoderSink.OnVideoFrameEncoded`
(FlashbackEncoderSink.cs:2078-2107), i.e. on the encoding thread, per encoded frame.
`UpdateDiskBytes` (:300-355, called at 4 Hz from the same thread) can also evict under the lock.

**Contention partners on the same lock**: UI status poll at 4 Hz calls `GetSegmentInfoList`
(:1062-1103) and `SegmentCount` (:1017-1027), each doing `File.Exists` per segment under the lock;
playback thread calls `GetValidSegmentFileForPosition` / `GetNextSegmentFile` /
`GetSegmentStartPts` (File.Exists under lock) on every scrub update / segment switch.

**Mechanism**: once the buffer is full (steady state after `FlashbackBufferMinutes`), eviction
fires regularly. Deleting a multi-hundred-MB/GB segment can take tens to hundreds of ms (AV
scanning, HDD, SMR drives, thermally throttled NVMe). While the delete holds `_indexLock`, the
encoding thread blocks inside its frame callback → encoder input queues back up → live capture
drops frames. Even without delete stalls, `File.Exists` storms (UI poll × N segments × 4 Hz) add
lock hold time on the hot path.

**Fix**:

1. Under `_indexLock`, only **unlink the index entries** and collect evictable paths; perform
   `File.Delete` outside the lock (either inline after releasing, or on a dedicated low-priority
   background deleter with retry — the retry also fixes P2-12's "locked file stops eviction").
2. Remove `File.Exists` from query paths under the lock: trust the index, and treat
   open-failure in the playback controller as the (already-handled) eviction race; or maintain an
   `Exists` flag updated only by the eviction/registration paths.
3. Keep `UpdateLatestPts`'s fast path lock-free as it is today (it already short-circuits when
   under budget) — the change is only inside the eviction branch.

**Verify**: logs already emit `FLASHBACK_BUFFER_SEGMENT_EVICT_DELETED ... elapsed_ms=` — correlate
high elapsed_ms with capture-side frame-drop counters (`VideoFramesDropped`) before the fix; after
the fix, delete latency no longer correlates with drops.

---

## P1 — WASAPI pause/resume handshake is fire-and-forget; a late pause flushes the freshly-primed audio

**Where**: `WasapiAudioPlayback.PauseRendering`/`ResumeRendering` (WasapiAudioPlayback.cs:283-305)
set flags + signal; the render thread processes them later (:538-582), and the pause handler calls
`Flush()` (:551). Controller resume sequence (`HandlePlayCommand` ThreadCommands.cs:483-543):
`SafePauseRendering("play")` … seek … `SafeFlushPlayback` … `PrimePlaybackAudioBuffer` …
`SafeResumePlaybackRendering`.

**Mechanism**: if the render thread is slow to wake (busy system, priority inversion), the
`_pauseRequested` flag may be processed **after** the controller has primed 180 ms of audio — the
pause's `Flush()` then discards the prebuffer, and rendering resumes dry → underrun/silence burst
and A/V desync at exactly the resume moment. The same unsynchronized pattern applies to every
`SuppressLiveAudio`/`RestoreLiveAudio` transition.

**Fix**: add an acknowledged state transition — e.g. `bool WaitForRenderState(paused/running,
timeout)` backed by a `ManualResetEventSlim` the render thread sets after applying
pause/resume. The controller calls it (with a ~100 ms timeout and a log on timeout) after
`PauseRendering()` and before priming. Alternatively, make `Flush()` part of the *controller's*
sequence only (remove the implicit flush from the pause handler) so a late pause cannot destroy
queued audio; audit all callers if you take this route.

**Verify**: stress resume under CPU load (e.g. ssctl flashback cycle scenario +
`start /low` CPU burner); before fix, occasional `WASAPI_PLAYBACK_RENDER_PAUSED` logged *after*
`FLASHBACK_PLAYBACK_AUDIO_PREBUFFER` for the same resume, followed by `RenderSilenceCount`
increments; after fix, ordering is deterministic.

---

## P1 — A/V drift policy has a dead band and an uncorrectable zone

**Where**: `PaceFrameInterval` (FlashbackPlaybackController.cs:2535-2618);
`TryResolveAudioDriftFrameSkip` (PlaybackFrames.cs:629-724).

**Issues**:

1. Correction is capped at `min(0.1 ms, 2% of frame interval)` **per frame** (:2568, :2575) —
   ~6-12 ms of correction per second. Recovering a 150 ms drift takes 5-25 s.
2. The ±100 ms `syncThresholdMs` dead band means steady-state lip-sync error of up to 100 ms is
   accepted by design; >45 ms is generally noticeable.
3. `MaxAudioMasterCorrectionMs = 250`: |drift| > 250 ms → wall-clock fallback with **no resync
   path**. Video-behind drift between 250 ms (correction gives up) and 500 ms (frame-skip kicks
   in, PlaybackFrames.cs:652) is never corrected — a permanent desync zone.

**Fix**: make the correction proportional (e.g. shave/add 10% of the excess drift per frame,
capped at ~25% of the frame interval), tighten the dead band to ~30-40 ms, and close the gap:
either lower `FrameSkipThresholdMs` to 250 or raise the outlier fallback to 500 so one of the two
mechanisms always owns the drift. For video-ahead drift > 250 ms persisting N consecutive frames,
do a hard re-anchor (treat as seek: pause render, flush, re-gate) instead of pacing by wall clock
forever. The existing counters (`PlaybackAudioMasterDelayDoubles/Shrinks/Fallbacks`,
`FLASHBACK_AV_DRIFT` 1 Hz log) make convergence directly observable.

---

## P1 — Resume does up to 3× redundant decode work on the playback thread (frame drops + long freezes)

**Where**: resume paths in ThreadCommands.cs (`HandlePlayCommand`, `HandleSeekCommand`,
`HandleEndScrubCommand`); `FlashbackDecoder.SeekTo` forward-decode cap 960 frames
(FlashbackDecoder.cs:701-794).

**Mechanism**: a single resume = exact `SeekTo` (keyframe + forward decode up to a full 1 s GOP)
→ prebuffer decode (≤96 frames) → rewind `SeekTo` (full GOP forward decode **again**) → playback
re-decodes the prebuffered interval a **third** time. With software decode at 4K (~25 ms/frame)
that is potentially seconds of stall, during which the command handler blocks the thread — a
queued Pause/GoLive/scrub waits it out (commands are only read between frames, and not at all
inside `PrimePlaybackAudioBuffer`).

**Fix**: Fix A from the first P0 (keep prebuffer frames, drop the rewind) eliminates passes 2-3.
Additionally:

- Check `commandChannel.Reader.TryPeek` inside `PrimePlaybackAudioBuffer`'s loop and bail when a
  control command is queued (the EOF-retry path can spin for the full 1 s timeout near the live
  edge — PlaybackAudioPrebufferTimeoutMs=1000, FlashbackPlaybackController.cs:2037-2041).
- Pass a cancellation/`TryPeek` check into `SeekTo`'s 960-frame forward loop so a newer scrub/seek
  can abort an in-flight exact seek (the loop honors the CTS token but that only fires on
  thread stop, not on newer commands).

**Verify**: `FLASHBACK_PLAYBACK_CMD_COMPLETE kind=Play duration_ms=` and
`MaxCommandQueueLatencyMs` before/after; with software decode forced (GpuDecodeEnabled=false at
1080p so the budget snap doesn't trigger), resume duration should drop ~3×.

---

## P1 — Live-edge playback churns EOF-waits and underruns audio

**Where**: `HandleEndOfSegment` write-head wait (PlaybackFrames.cs:885-900);
`CheckNearLiveEdge` warmup gate `frameCount > 60` (:1127).

**Mechanism**: playing within ~180 ms of the write head drains the audio queue while the decoder
sits in 50 ms EOF-wait loops; the near-live auto-snap is suppressed for the first 60 frames
(`requireFrameWarmup`), so a resume that starts near the edge can oscillate
decode→EOF→wait→decode with audible audio gaps before either catching content or snapping.

**Fix**: maintain a minimum live lead — clamp resume/seek targets to
`LatestPts - max(250ms, audioPrebufferTarget)` (the pause-from-live path already backs off one
frame; generalize), and allow the near-live snap without warmup when the WASAPI buffered duration
hits zero at the write head (audio underrun at edge ⇒ snap to live immediately rather than
stuttering toward it).

---

## P2 — UX seams (seamlessness rather than correctness)

1. **Pause-from-live jumps back up to ~1 s** (one GOP): `HandlePauseCommand` displays the
   keyframe at/before `latest - 1 frame` (ThreadCommands.cs:568-612). With GOP=1 s the freeze
   frame can be a second older than what the user just saw. Options: forward-decode from keyframe
   to the pause target (bounded by 1 s GOP; one-shot cost), or freeze the last live preview frame
   (the preview sink already holds it) and let the decoder catch up in the background.
2. **Scrub granularity is keyframe-only** (1 s steps) while the label reports exact ms. Add a
   "settle refinement": when the pointer is stationary ~150 ms during a scrub (or on EndScrub into
   Paused), do an exact forward decode to the target so the displayed frame matches the label.
   Keep keyframe-only while the pointer is moving.
3. **First interaction is cold**: playback thread, decoder, and file open all happen lazily on the
   first command (`EnsurePlaybackThread` ThreadCommands.cs:135-191, `CreateDecoder`). Pre-warm when
   the timeline panel opens (`StartStatusPolling` is the natural hook): start the thread and open
   the active segment so the first pause/scrub is instant.
4. **Involuntary snap-to-live is silent**: decode errors, software-budget snaps, and near-live
   snaps flip state to Live with only a log line; the UI's 250 ms poll later moves the playhead.
   Surface a transient UI notice ("Returned to live — playback error") via a state-change callback
   from `SetState` into the VM (event > poll; also removes up to 250 ms of state staleness).
5. **Eviction can't delete the segment the decoder holds open** — `File.Delete` fails (no
   FILE_SHARE_DELETE in ffmpeg's file protocol) and `EvictOldestSegments` `break`s, so disk can
   exceed budget during a long pause on old content. The background-deleter retry from the P0 fix
   largely resolves this; alternatively have the controller close the decoder file when paused for
   > N minutes on an old segment (reopen on resume — pendingExactResumeTarget already supports it).
6. **`GapFromLive` reports zero before the first decoded frame** (FlashbackPlaybackController.cs:170-180,
   `_lastVideoPtsTicks==0` in the deferred pause-from-live display path) → "-0:00" label lies
   briefly. Fall back to `LatestPts - (PlaybackPosition + frozenValidStart)` when no frame PTS yet.

---

## Suggested implementation order for Codex

| # | Item | Files | Risk |
|---|------|-------|------|
| 1 | Prebuffer: keep frames in `prebufferedFrames`, drop rewind (P0-1 fix A, also resolves P1-6 core) | FlashbackPlaybackController.cs, ThreadCommands.cs | Medium — D3D11VA surface pool budget; verify pool depth, cap held frames |
| 2 | Eviction deletes off-lock + remove File.Exists from locked query paths (P0-2) | FlashbackBufferManager.cs | Low-Medium — keep index/byte accounting consistent when a deferred delete fails |
| 3 | WASAPI pause/resume acknowledgment (P1) | WasapiAudioPlayback.cs, controller call sites | Low |
| 4 | Drift policy: proportional correction, tighter band, close 250-500 gap, hard re-anchor (P1) | FlashbackPlaybackController.cs, PlaybackFrames.cs | Medium — tune with FLASHBACK_AV_DRIFT logs at 60/120 fps |
| 5 | Command responsiveness inside prime/SeekTo loops; live-edge min-lead clamp (P1) | controller partials, FlashbackDecoder.cs | Low |
| 6 | UX seams: pause-frame accuracy, scrub settle refinement, pre-warm, snap notification | ThreadCommands.cs, PlaybackFrames.cs, UI controllers, VM | Low |

Each step should land with: (a) targeted contract tests in
`tests/Sussudio.Tests/XUnit.FlashbackContractsTests.cs` (e.g. "audio gate after prebuffer rejects
duplicate PTS", "eviction never deletes under _indexLock" via an interception seam), and (b) a
manual verification pass using the diagnostic scenarios in
`tools/Common/DiagnosticSessionFlashback*Scenarios.cs` (cycle + stress scenarios already exist)
plus the log tokens listed per finding. Existing counters to watch end-to-end:
`PlaybackDroppedFrames`, `PlaybackLateFrames`, `PlaybackAudioMasterFallbacks`,
`RenderSilenceCount`, `PlaybackSubmitFailures`, `MaxCommandQueueLatencyMs`, `FLASHBACK_AV_DRIFT`.

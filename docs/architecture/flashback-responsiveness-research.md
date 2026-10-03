# Flashback responsiveness and app latency research

Date: 2026-10-02. Branch: `claude/flashback-performance-optimization-63ae05`.
Baseline commit: `a50ead960` (main after PR #8).

This report extends `latency-frame-time-research.md` (2026-09-28, code-only).
That report covered live preview, recording and audio; it listed Flashback
(its section 5) but implemented none of it. This pass went after Flashback
first, then re-verified the preview and background findings against current
code, and, unlike the earlier pass, it has measurements: offline runs of the
segment files the app wrote today, and timings from the app's own logs.
Nothing was run in the live app during this pass.

Labels:

- **Measured**: a number from an ffprobe/ffmpeg run on a real segment, or from
  an app log line. The command or log tag is named.
- **Verified**: read in the current code at the cited line.
- **Suspected**: a mechanism that code plus measurement make likely but that
  needs one live run to confirm. The check that settles it is named.

Paths are relative to `Sussudio/` unless stated.

## 0. The build you have been running does not contain the September fixes (Measured)

Today's log (`temp/logs/Sussudio_Debug_20261002_213746.log`) was written by
`latest-build/Sussudio.exe` dated 2026-09-24 13:49. The latency work merged
on 2026-09-28 (`3e9a9dd1a`) and the status work merged on 2026-10-01 are not
in it: the log has no `WASAPI_CLIENT3_INIT` line and only four
`MMCSS registered` lines (read loop, render thread, MJPEG worker, MJPEG
emitter), none for the encode loops or the WASAPI workers. Every live
impression formed this week predates those fixes. Re-stage the build
(`tools/stage-builds.ps1`) before judging anything below by feel.

## 1. Measured facts about the Flashback files and decoder

Session `9638b484...` (today, 18:43 to 20:59), segment
`fb_9638b484094d4510bed161e56204114b_0021.ts`, probed with the ffprobe and
ffmpeg in `C:\Windows` (2024-04-29 build). The app ships avcodec 62; the
behaviour below belongs to the HEVC decoder and is the same in both.

| Fact | Value | How |
|---|---|---|
| Stream | 3840x2160 HEVC Main, 60000/1001 fps, no B-frames, AAC | `ffprobe -show_entries stream=...` |
| Bitrate | 196 Mbit/s; 7.36 GB per 300 s segment | `format=bit_rate,size,duration` |
| GOP | IDR every 60 frames (1.001 s), forced; I about 787 KB, P about 384 KB, GOP about 23 MB | `-show_entries packet=flags,size,pos` over 4 s |
| Timestamps | absolute from session start; segment 21 begins at 6300.344 s | `format=start_time` |
| TS seek landing | `av_seek_frame(BACKWARD)` lands on the packet just before the target, a P frame (target 6400.5 lands 6400.494; 6450.25 lands 6450.244; 6537.7 lands 6537.698). The mpegts demuxer has no index and its binary search marks every packet as a keyframe | `-read_intervals T%+#2 -show_packets` |
| First frame the decoder emits after that seek | the **next IDR**, 0 to 1.0 s after the target (6450.25 gives 6450.494, +244 ms). The mid-GOP P frames are discarded with `Could not find ref with POC n` and `Error constructing the frame RPS` logged per packet. Same for software and `-hwaccel d3d11va`, default flags | `ffmpeg -ss T -vf showinfo -frames:v 3` |
| Same with `-flags2 +showall` | the P frames are emitted, decoded against grey references (corrupt). The app does not set this flag, so it shows clean frames, late | same |
| Same with `-skip_frame nokey` | same IDR, no RPS errors, about 2.5x faster to first frame (0.090 s vs 0.225 s process time) | same |
| D3D11VA decode rate on this machine | 180 frames in 0.97 s wall (about 5 ms per frame); one 60-frame GOP from its IDR: 0.39 s | `ffmpeg -benchmark -hwaccel d3d11va` |
| Software decode rate | 180 frames in 1.2 s wall using 12 s CPU (all 16 cores) | `ffmpeg -benchmark` |
| 2 s preroll cost | `-ss T-2 -t 2` with d3d11va: 0.79 s process time (about 0.65 s net) | same |
| Segment rotation | 26 rotations, `total_ms` mean 1.5, max 5.2 | `LIBAV_MUX_PHASE operation=rotate` in today's log |
| Encoder queue rejects | 1 GPU-queue reject in the whole two-hour session | `FLASHBACK_SINK_GPU_QUEUE_REJECT` |

Live playback timings exist only for a 1080p59.94 software-decode session
(`temp/logs/Sussudio_Debug_20260905_235755.log`, which logged
`FLASHBACK_DECODER_INIT d3d11va=false reason=no_device`), from
`FLASHBACK_PLAYBACK_CMD_COMPLETE`:

| Command | n | median ms | p90 ms | max ms |
|---|---|---|---|---|
| Seek | 18 | 58 | 194 | 226 |
| Play | 8 | 64 | 108 | 182 |
| Pause | 9 | 44 | 55 | 71 |
| BeginScrub | 1 | 55 | | |
| UpdateScrub | 4 | 32 | 40 | 44 |
| EndScrub | 1 | 27 | | |
| GoLive | 8 | 2.6 | 3.2 | 5.3 |

Those are 1080p numbers. At 4K60 HEVC the per-frame decode is about 5 ms on
the GPU path, so every item below that is "frames decoded" scales by that.
There is no 4K playback log yet; `FLASHBACK_PLAYBACK_CMD_COMPLETE` on a 4K
session is the first thing to capture.

## 2. Where Flashback time goes, per interaction (4K60 HEVC, GPU decode)

### Scrub update (Verified mechanism, Measured decoder behaviour)

`HandleUpdateScrubCommand` calls `SeekAndDisplayKeyframe`, which calls
`SeekToKeyframe` and then one `TryDecodeNextVideoFrame`
(`Services/Flashback/FlashbackPlaybackController.ThreadCommands.cs:728-781`,
`FlashbackPlaybackController.PlaybackFrames.cs:431-519`,
`FlashbackDecoder.cs:779-843, 949-1013`).

1. TS binary seek over a 7 GB file: about 25 probe reads of 0.4 to 0.8 MB
   each (`mpegts_get_dts` reads to the next PES). Estimated 10 to 30 ms from
   page cache.
2. The decoder is fed every packet from the landing point to the next IDR:
   on average 30, up to 60 packets (12 to 23 MB of reads). Each is parsed,
   fails RPS construction and is discarded. Each failure emits two or three
   `AV_LOG_ERROR` lines, and the app's log callback forwards every error
   level line to `Logger.Log` (`Services/Runtime/FfmpegRuntimeLocator.cs:296-315`),
   so one scrub update produces roughly 60 to 180 `FFMPEG_LOG` lines
   (Suspected; count `FFMPEG_LOG ... Could not find ref` in a 4K scrub log).
3. One IDR decode, about 5 ms, plus the submit.
4. The displayed frame is on average 0.5 s and up to 1.0 s **after** the
   pointer. `PlaybackPosition` is then set from the decoded PTS
   (`PlaybackFrames.cs:545-548`), so the playhead jumps ahead of the drag.

Estimated 40 to 70 ms per update, with the wrong frame. Scrub updates are
coalesced in the mailbox and throttled to about 16 ms in the UI, so the
pointer always waits on the previous update.

### Play from paused (Verified)

`HandlePlayCommand` (`ThreadCommands.cs:431-514`):

1. `TrySeekWithActiveFmp4Reopen` calls `SeekTo`: keyframe seek to target
   minus 2 s, then decode forward until PTS reaches the target
   (`FlashbackDecoder.cs:846-947, 1029-1036`). At 60 fps that is 120 to 180
   frames, 0.6 to 0.9 s on the GPU path. Every intermediate frame is cloned
   and validated (GPU) or fully converted to NV12 (software) before its PTS
   is checked.
2. `PrimePlaybackAudioBuffer` decodes until 180 ms of audio is buffered
   (`FlashbackPlaybackController.cs:1892-2026`). GPU frames cannot be kept
   (`PlaybackAudioPrebufferMaxHeldFrames = 1`, GPU frames excluded at
   `:1963`), so they are released.
3. `TryRewindPlaybackAudioPrebuffer` performs a **second** exact `SeekTo`
   with another 2 s preroll (`:2028-2063`).
4. `SafeResumePlaybackRendering` waits up to 100 ms for the WASAPI thread,
   whose prebuffer poll sleeps in 5 ms steps.

Estimated 1.3 to 2.0 s from Play to motion at 4K60. The 1080p log shows
64 to 182 ms for the same sequence because its frames cost about 1 ms each.

### Pause from live (Verified)

`HandlePauseCommand` (`:516-576`): the target is `LatestPts` minus the 300 ms
`MinimumPlaybackLiveLead` (`FlashbackPlaybackController.cs:1687`), then
`EnsureFileOpen` (a first open pays the probe described under Go live), a
keyframe seek that lands as in the scrub case, then
`DecodeForwardToPauseTarget` for up to one GOP (`PlaybackFrames.cs:584-635`).
About 0.3 to 0.5 s, and the frozen frame is at least 300 ms behind what was
on screen.

### Go live (Verified)

`HandleGoLiveCommand` calls `RestoreLiveForPlaybackThreadExit`
(`ThreadCommands.cs:322-334`): decoder close and dispose (2.4 ms measured),
`RestoreLiveAudio` with a wait of up to 100 ms, and only then
`ResumePreviewSubmission`. The next interaction pays `CreateDecoder` (a new
D3D11VA device context) plus `OpenFile`, whose `avformat_find_stream_info`
runs with a 20 MB / 5 s window and `skip_frame=all` only when the demuxer
already knows the dimensions, which a fresh TS open does not
(`FlashbackDecoder.cs:406-408, 465-510`). The segment continuation path
already uses the fast settings (`fps_probe_size=0`, 100 ms analyze,
`:513-583`).

### Continuous playback (Verified)

- Pacing sleeps and then spins the last 2 ms of every frame
  (`FlashbackPlaybackController.cs:2375-2392`): about 24 percent of a core
  at 120 fps, 12 percent at 60 fps.
- The MJPEG jitter buffer polls every 2 ms while preview is suppressed, so
  during any Flashback playback on an MJPEG source a second thread wakes 500
  times a second (`Services/Capture/MjpegPreviewJitterBuffer.cs:293-306`).
- Software frames take three full-frame copies: the decoder ring,
  `Marshal.Copy` into an `ArrayPool` buffer on the playback thread
  (`Services/Preview/D3D11PreviewRenderer.cs:327-330`), then
  `UpdateSubresource` on the render thread
  (`D3D11PreviewRenderer.RenderPasses.cs:347`). The pooled-lease overload
  that avoids the middle copy, `SubmitRawFrameLease`, is not used by
  Flashback (`PlaybackFrames.cs:153`).
- The renderer's input-view cache holds 8 entries
  (`Services/Preview/PreviewInputViewCache.cs:11`) while D3D11VA playback
  cycles through up to 12 retained frames plus the codec pool
  (`FlashbackDecoder.cs:58, 1522`). LRU over a cycle longer than the cache
  misses on every frame (Suspected; the `D3D11_PREVIEW_INPUT_VIEW_CACHE`
  line during 4K GPU playback settles it; live preview today shows
  `entries=3 misses=3`, so no thrash there).
- Audio packets are sent to the AAC decoder during seeks even with the
  callback detached (`FlashbackDecoder.cs:1178-1194`): small, about 10 ms
  per 2 s preroll.

### UI feedback (Verified)

- Engine-to-UI state is poll-only at 250 ms
  (`Controllers/Flashback/FlashbackUiControllers.cs:221, 270-285`;
  `ViewModels/MainViewModel.FlashbackState.cs:359-364`). The play/pause
  glyph, Go Live enablement and the position label lag up to 250 ms, and
  `TogglePlayPause` reads the stale state, so a second press inside that
  window resends the same command.
- The scrub throttle uses `Environment.TickCount64 < 16`, which quantises
  to 15.6 ms steps and drops the trailing move until release
  (`FlashbackUiControllers.cs:1062-1065`).
- The position label never uses the pointer position while scrubbing
  (`:488-499, 592-607`).
- `PrepareForPreviewDetach` joins the playback thread on the UI thread under
  `_playbackThreadSync` with a 10 s timeout (`ThreadCommands.cs:209-250`);
  today's log shows a 14 ms detach, but any `EnsurePlaybackThread` caller
  blocks on that lock meanwhile.
- Nudge commands are not coalesced; held arrow keys queue serial decodes.

## 3. A/V sync in Flashback files drifts by seconds (Measured)

Audio PTS is a sample counter and video PTS a frame counter, neither anchored
to a capture clock (prior report, section 6). The consequence is now
measured:

| Where | audio PTS minus video PTS at the same byte offset |
|---|---|
| segment 18, 30 s in | +1.05 s |
| segment 18, 150 s and 280 s in | +6.55 s, +6.58 s |
| segment 21, 30 / 150 / 280 s in | +6.71 / +6.74 / +6.77 s |
| segment 23, 30 / 150 / 280 s in | +6.86 / +6.90 / +6.96 s |
| short session `e37ea379...` (213 s) | -0.01 s throughout |

Segment 21 also contains no audio packets at all for its first 10 s (the
first audio packet sits at byte 246 MB). The encoder's own `LIBAV_AV_SYNC`
line for that session goes -13 ms at 5 s, +2.36 s at 10 s, +6.35 s at
400 s, +11.0 s at 5140 s, +16.6 s at 5520 s and +19.3 s at 7220 s, in steps
of 1 to 5.5 s plus a creep of about 0.06 percent of elapsed time, with
`totalCorrectionSamples=0` every time. The steps coincide with capture
restarts and stalls, not with sink queue rejects (one in two hours), so the
missing frames are lost before the sink and the frame counter never skips
them.

Effect on Flashback: playback audio is seconds away from the picture, the
audio-master pacer sees a drift above its 500 ms outlier limit and falls
back to wall-clock pacing (`FlashbackPlaybackController.cs:2305-2312`), and
the audio prebuffer gate passes audio from the wrong time. This is the
largest "Flashback does not work right" item in the tree, and no amount of
seek work fixes it.

## 4. Findings and fixes, ranked for Flashback feel

| # | Finding | Status | Fix |
|---|---|---|---|
| F1 | Scrub shows the IDR after the pointer, up to 1 s late, after discarding up to 60 packets with error logging per packet | Measured | Short term: set `skip_frame = AVDISCARD_NONKEY` on the video context for scrub seeks and seek to target minus one GOP, which yields the IDR at or before the pointer with header-only parsing. Long term: F2 |
| F2 | No keyframe index; exact seeks preroll 2 s and decode 120 to 180 frames | Verified | In `WritePacket` (`Services/Recording/LibAvEncoder.VideoFrames.cs:681-690`) record `(pts, avio_tell)` for every `AV_PKT_FLAG_KEY` packet per segment, publish it through `FlashbackBufferManager`, and seek by byte (`avformat_seek_file` with `AVSEEK_FLAG_BYTE`, then flush) to the IDR at or before the target. Scrub becomes one IDR decode (about 5 ms); exact seeks decode at most 59 frames (mean 30, about 150 ms) |
| F3 | Play does two exact seeks because the audio prebuffer discards GPU frames and rewinds | Verified | Prebuffer audio by demuxing: queue the video `AVPacket`s (about 1 MB for 22 packets) instead of decoding them, deliver audio, then feed the queued packets when playback starts. No rewind, no second seek. Alternatively keep decoded GPU frames (raise `MaxRetainedHardwareFrames` to cover 180 ms) and drop the 180 ms target to 60 to 80 ms |
| F4 | Decoder and D3D11VA context disposed on every Go Live; the next open probes 20 MB / 5 s | Verified | Keep the decoder warm (dispose on Flashback disable, preview detach, device loss). Open with the session's known codec parameters: `fps_probe_size=0`, `max_analyze_duration` 100 ms, `probesize` about two GOPs, exactly as `TryContinueMpegTsSegment` already does |
| F5 | Pause from live decodes from disk to a frame 300 ms or more behind what was on screen | Verified | Freeze the frame the renderer is already showing (stop submission, keep the last texture), record its source PTS as the pause position, and defer the decoder work to the first scrub or play. Needs product sign-off: it changes the pause target from "300 ms behind" to "exactly what you saw" |
| F6 | Go Live resumes preview last, after decoder teardown and audio waits | Verified | Reorder: `ResumePreviewSubmission` first, then audio restore, then cleanup (or no cleanup, with F4) |
| F7 | Intermediate frames in `SeekTo` are converted or cloned before the PTS test | Verified | Split receive from convert; convert only when the PTS reaches the target or when keeping a best-so-far |
| F8 | 2 ms spin per paced frame | Verified | High-resolution waitable timer (`CreateWaitableTimerExW` with `CREATE_WAITABLE_TIMER_HIGH_RESOLUTION`) and a spin of at most 0.3 ms |
| F9 | Jitter buffer polls at 2 ms while suppressed | Verified | Wait on the signal with a long timeout while suppressed; `ReprimeAfterPreviewResume` already sets it |
| F10 | UI state lags 250 ms; the label ignores the pointer; the throttle is quantised; nudges are not coalesced | Verified | Post a coalesced refresh from `StateChanged`; set view-model state optimistically on command accept; derive the label from the pointer while scrubbing; `Stopwatch` throttle with a trailing flush; coalesce nudge deltas |
| F11 | Software frames copied three times; the view cache may thrash during GPU playback | Verified / Suspected | Decode into a `PooledVideoFrameLease` and call `SubmitRawFrameLease`; raise `PreviewInputViewCache.Capacity` to 32 or key it on the texture array |
| F12 | A/V drift of 7 to 19 s over a two-hour session | Measured | Stamp audio from `qpcPosition` and video from the MF sample time, insert exactly the missing audio or skip video PTS at gaps (`SkipVideoFrame` exists with no callers), and alarm on `LIBAV_AV_SYNC_DRIFT_WARNING` in diagnostics. Policy details are in the prior report, section 6 |
| F13 | The FFmpeg log callback forwards every `AV_LOG_ERROR` line and marshals every format string at `AV_LOG_VERBOSE` | Verified | `av_log_set_level(AV_LOG_ERROR)` plus rate limiting; with F1 the error source disappears |

Not a problem, contrary to the earlier estimate: segment rotation (1.5 ms
mean, 5.2 ms max, `LIBAV_MUX_PHASE`) and decoder dispose (2.4 ms).

## 5. Live preview and FPS, re-verified against current code

Line numbers in `D3D11PreviewRenderer.cs` moved by 9 to 17 lines since the
prior report; the partials did not. All items below are Verified unless
marked.

- The waitable swap-chain wait is still a fixed 8 ms (`:1050`); the measured
  refresh interval (`_measuredRefreshIntervalTicks`, `:2074`) is available
  to derive it. The renderer never raises timer resolution, so below 100 fps
  the wait can round to 15.6 ms (Suspected).
- Dequeue is FIFO with a 1.5-interval stale threshold (`:877-1010`); at
  fps equal to Hz a one-frame standing offset persists. Latest-wins at depth
  2 or more needs a live A/B.
- CPU upload uses `UpdateSubresource` of 12 to 25 MB on the render thread
  (`RenderPasses.cs:347-354`); the encoders and the Flashback decoder share
  the same immediate context, serialised by D3D's internal lock.
- HDR texture path: two `CopySubresourceRegion` calls plus a shader pass per
  frame and a `Description` query per frame (`RenderPasses.cs:604-616`).
- `ResizeCompositionSwapChain` disposes the processor, all input views and
  the HDR ring, and the next `EnsureInputResources` rebuilds the SDR ring
  (about 75 MB SDR, 150 MB HDR at 4K) on every size change of 96 px or more
  (`Resources.cs:910-947, 1489`).
- Four shaders are compiled with `D3DCompile` on every `InitializeD3D`,
  including HDR shaders for SDR sessions (`Resources.cs:553-647`).
- Per submit: a `PendingFrame` object, a managed texture wrapper, a lock, five
  interlocked writes and `DateTimeOffset.UtcNow` (`:420-433, 568, 2143-2150`).
- `IsDeviceLostException` matches only 0x887A0005 and 0x887A0007; DEVICE_HUNG
  (0x887A0006) falls into an unthrottled per-frame failure log
  (`Resources.cs:1163`; `:922`).
- BMP screenshot capture maps a staging texture and writes the file on the
  render thread (`RenderPasses.cs:818`; `PreviewScreenshotCapture.cs:77`).
- `GetEstimatedPipelineLatencyMs` still sums up to 2400 samples with a
  modulo each under `_pipelineLatencyLock` (`:1753`).

Background work that competes with the hot threads (Verified):

- The automation hub polls every 500 ms with no subscriber and builds the
  preview snapshot **three times per tick on the UI thread** (before, value,
  after), each with about seven copy-and-sort passes over 1.2 to 4.8 k
  doubles (`Services/Automation/AutomationDiagnosticsHub.Snapshots.cs:163-168, 312-332`;
  `Controllers/Preview/Renderer/PreviewRuntimeSnapshotControllers.cs:33`).
- Source telemetry sends 24 to 52 synchronous USB control IOCTLs every
  500 ms to the streaming device plus about 14 log lines per tick
  (`Services/Capture/CaptureService.RuntimeSnapshots.cs:1892-1918`;
  `Services/Telemetry/NativeXuAtCommandProvider.cs:96, 509-577`).
- NVML runs ten driver calls every 500 ms from app start, including two PCIe
  throughput samples (`Services/Gpu/NvmlMonitor.cs:81`).
- The 1 s view-model timer builds a full runtime snapshot and calls
  `DriveInfo.AvailableFreeSpace` on the UI thread
  (`Controllers/ViewModel/MainViewModelLifecycleController.cs:208-241`).
- Preview-audio ramps sample the full runtime snapshot every 10 ms, resuming
  on the UI thread, during preview start
  (`Controllers/ViewModel/PreviewAudioTransitionControllers.cs:215-222`).
- The stats sampler runs `GetHealthSnapshot` on the UI thread every 500 ms
  while the stats dock is open (`Controllers/Stats/StatsUiSampler.cs:118`).

Runtime config: workstation concurrent GC, tiered JIT, ReadyToRun only on
publish; no `GCSettings.LatencyMode`, no process-wide timer resolution, no
thread-pool minimum. Nothing calls `GC.Collect`.

## 6. Order of work

1. **Re-stage the build** and capture a 4K GPU-decode playback log
   (`FLASHBACK_PLAYBACK_CMD_COMPLETE`, `FLASHBACK_DECODER_OPEN probe_ms`,
   `FLASHBACK_PLAYBACK_AUDIO_PREBUFFER`, `D3D11_PREVIEW_INPUT_VIEW_CACHE`).
   Without it every 4K figure above stays an estimate.
2. **Scrub**: F1 now (two lines in the decoder plus a one-GOP back-off), then
   F2 (index). This is the single largest feel change: the exact keyframe at
   the pointer, about 10 ms per update instead of 40 to 70 ms and a late
   frame.
3. **Play**: F3 (no rewind) and F2 together take Play from an estimated 1.3 to
   2 s to about 0.2 to 0.3 s at 4K60.
4. **Enter and leave**: F4, F5, F6. Pause becomes instant, Go Live restores
   preview first, and the first scrub after Go Live no longer pays decoder
   creation plus a probe.
5. **Steady state**: F8, F9, F11, F13.
6. **UI**: F10 as one slice; it is independent of the engine work.
7. **A/V sync**: F12. Large, cross-cutting, and the only item that is a
   correctness bug rather than latency; schedule it as its own project with
   the policies from the prior report's section 6.
8. **Preview and background**: hub snapshot once per tick and demand-gated,
   XU poll at 2 to 5 s, NVML gated to the stats dock, waitable timeout from
   the measured refresh, view-cache capacity, lease path for software frames.

Contracts to preserve: no silent codec or preset change, no blocking in the
source-reader fan-out, automation field names unchanged, segment and recovery
semantics unchanged. F5 is the one behaviour change that needs a product
decision.

## 7. How to verify

- Offline, any segment: `ffprobe -v error -select_streams v -read_intervals
  "T%+#2" -show_entries packet=pts_time,flags,pos` shows the landing packet;
  `ffmpeg -hwaccel d3d11va -seek_timestamp 1 -ss T -i seg.ts -an -frames:v 2
  -vf showinfo -f null -` shows the first emitted frame. T is absolute (add
  the segment's `start_time`).
- Offline, A/V offset: `ffprobe -read_intervals "T%+#40" -show_entries
  packet=stream_index,pts_time` and subtract the first video PTS from the
  first audio PTS.
- Live: `run_diagnostic_session` scenario `flashback-scrub-stress` reports
  `FlashbackPlaybackMaxCommandQueueLatencyMs` and coalesced scrub counts; add
  `FLASHBACK_PLAYBACK_CMD_COMPLETE` percentiles per kind to its artifact so
  the table in section 1 can be reproduced for 4K.
- After F1 and F2: `FLASHBACK_PLAYBACK_SEEK_OK` must show `file_pts_ms` at or
  before the requested target, and `FFMPEG_LOG ... Could not find ref` must
  not appear during scrubs.
- After F12: `LIBAV_AV_SYNC driftMs` stays within 50 ms over a two-hour
  session, and the offline offset above stays near zero in every segment.

## 8. Implemented

### Phase A: Flashback engine (branch `claude/flashback-performance-optimization-63ae05`)

| # | What shipped | Where |
|---|---|---|
| F1 | Keyframe-only decode after a seek: `skip_frame = AVDISCARD_NONKEY` on the single-threaded (D3D11VA) decoder, so the IDR costs one decode. A frame-threaded software decoder submits packets ahead of the frame being received and holds keyframes behind the reorder delay, so that path decodes normally and drops frames until the first keyframe. Timestamp fallback for MPEG-TS seeks back one GOP plus 100 ms and prefers the next IDR when it is still at or before the target | `FlashbackDecoder.SeekToKeyframe`, `TryDecodeKeyframeOnly`, `TryDecodeDroppingUntilKeyframe`, `ResolveExactSeekStart` |
| F2 | Writer-side keyframe index. `WritePacket` samples `avio_tell` before the interleaved write of every `AV_PKT_FLAG_KEY` packet and raises `VideoKeyframeWritten`; the Flashback sink forwards it into `FlashbackBufferManager` (bounded per segment, removed with the segment). Seeks look the keyframe up and seek by byte (`AVSEEK_FLAG_BYTE`) before decoding; a stale entry falls back to the timestamp path | `LibAvEncoder.VideoFrames.cs`, `FlashbackEncoderSink.OnVideoKeyframeWritten`, `FlashbackBufferManager.RecordKeyframe` / `TryFindKeyframeAtOrBefore`, `FlashbackDecoder.TrySeekDemuxerToBytePosition` |
| F3 | Audio prebuffer by demuxing: `PrebufferAudio` decodes and delivers audio while stashing the video packets it passes (bounded at 128); `FeedNextVideoPacket` feeds the stash before reading the demuxer again. No video decode, no rewind, no second seek | `FlashbackDecoder.PrebufferAudio`, `FlashbackPlaybackController.PrimePlaybackAudioBuffer` |
| F4 | Warm decoder: Go Live closes only the file and keeps the decoder and its D3D11VA device context; Stop and thread exit dispose it; it is recreated only when the D3D device pointer changes. Hinted open: the sink publishes `FlashbackPlaybackStreamHints` (size, P010, codec, rate); the decoder pre-fills codec parameters and opens with `probesize` 8 MB, `max_analyze_duration` 200 ms, `fps_probe_size = 0`, `skip_estimate_duration_from_pts = 1` | `FlashbackPlaybackController.ThreadCommands.cs` (`RestoreLiveKeepingDecoder`, `EnsureWorkerDecoder`), `FlashbackDecoder.OpenFile` / `TryApplyStreamHints` |
| F5 | Instant pause from live: live submission stops so the renderer keeps the last presented picture; the paused position is the live edge (`LatestPts`); the first scrub, nudge or play seeks exactly to it. No file is opened and nothing is decoded on pause | `HandlePauseCommand` (Live branch) |
| F6 | Live restore order is preview first, then audio, then decoder cleanup, in every restore path | `RestoreLiveAfterNoFile`, `RestoreLiveKeepingDecoder`, `RestoreLiveForPlaybackThreadExit`, `RestoreLiveAfterDecoderPlaybackFailure` |
| F7 | Exact seek receives raw frames and converts only the presented one; the best-so-far candidate is an `av_frame_clone` reference, converted only if the decode ends before the target. Scrub updates display the keyframe (`exact: false`); seeks while paused, nudges and end-of-scrub are exact | `FlashbackDecoder.SeekTo`, `SeekAndDisplayFrame(..., exact, ...)` |
| F8 | `PrecisionWaiter`: per-thread high-resolution waitable timer (`CREATE_WAITABLE_TIMER_HIGH_RESOLUTION`) with a 0.25 ms spin tail; sleep/spin fallback when the OS declines one | `Services/Runtime/RuntimeHelpers.cs`, `WaitUntilPacingTarget` |
| F13 | Native log callback: `[UnmanagedCallersOnly]` function pointer filters by level before any marshalling, formats errors with `av_log_format_line2` and the original va_list, rate limits to 40 lines/s with a suppressed count, `av_log_set_level(AV_LOG_ERROR)` | `FfmpegRuntimeLocator.cs` |

Validation: `scripts\validate.ps1 -ClearToolLocks` green (2317 xUnit tests
passed, 0 failed) and `Report-DeadSurface.ps1` at zero. Contract and behaviour
tests were updated to the new design (`XUnit.FlashbackContractsTests.cs`,
`FlashbackPlaybackWorkerBehaviorTests.cs`, `FlashbackPrebufferBehaviorTests.cs`).

Not yet verified live: the 4K60 GPU-decode timings in sections 1 and 2 after
these changes. Run `run_diagnostic_session` scenario `flashback-scrub-stress`
against a built app and compare `FLASHBACK_PLAYBACK_CMD_COMPLETE`,
`FLASHBACK_DECODER_SEEK_INDEXED`, `FLASHBACK_DECODER_OPEN probe_ms hinted=true`
and `FLASHBACK_PLAYBACK_AUDIO_PREBUFFER stop=`.

Still open after Phase A: F9 (jitter buffer wait while suppressed), F10 (UI
feedback), F11 (software frame lease and view-cache capacity), F12 (A/V PTS
lock), and the section 5 preview and background items.

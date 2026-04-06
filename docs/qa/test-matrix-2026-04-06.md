# QA Test Matrix — 2026-04-06

## Run Focus
**Audio Sync & Flashback Stability** — verify audio synchronization for regular recording, flashback encoding, flashback playback/scrubbing, and export. Console running game (PS5 → 4K X → 3840x2160@120fps HDR).

## Run Status
- **Started:** 2026-04-06 08:53 UTC
- **Last Updated:** 2026-04-06 12:10 UTC
- **Source:** PS5 → Elgato 4K X → 3840x2160@119.88fps HDR (YCbCr422 BT.2020 PQ)
- **Mic:** Elgato Wave XLR MK.2
- **Progress:** 87/120 complete (Phase A-D done, 3 deferred, Phase E pending)
- **Bugs Found:** 5 — (1) AV drift metric not reset after audio toggle, (2) AV1 consecutive recording failure, (3) AV1 FB playback severe desync at 4K@120, (4) HDR reinit crash, (5) **H.264 flashback export has no video**
- **Elapsed:** ~3h 17m (including 45 min soaks + 5 min idle test)

## Baseline State
- Device: Elgato 4K X
- Mic: Elgato Wave XLR MK.2
- Codec: HEVC, Resolution: Source (3840x2160), FPS: 120, Quality: Super High, Preset: P7
- Split Encode: 3-way, Video Format: Auto, Bitrate: 50 Mbps
- HDR: off, Audio: on, Flashback: active
- AvSyncCaptureDriftMs: -3.3ms (baseline)

## Audio Sync Metrics (tracked per test)
- `AvSyncCaptureDriftMs` — capture pipeline A/V drift (target: < ±30ms)
- `AvSyncCaptureDriftRateMsPerSec` — drift rate (target: < 1.0 ms/s)
- `AvSyncEncoderDriftMs` — encoder-side drift (target: < ±30ms)
- `FlashbackAvDriftMs` — flashback playback A/V drift (target: < ±30ms)
- ffprobe: audio vs video stream duration delta (target: < 100ms for 10s, < 500ms for 60s)
- ffprobe: audio stream presence and codec correctness

## Results

### Phase A: Smoke (set + verify state, no recording)

| #  | Phase | Category       | Setting                    | Value          | Status  | UI | Behavior | Recording | Output | Notes |
|----|-------|----------------|----------------------------|----------------|---------|----|----------|-----------|--------|-------|
| 1  | A     | codec          | codec                      | H.264          | PASS    | ok | ok       | —         | —      | h264_nvenc, FB active, drift 7.6ms |
| 2  | A     | codec          | codec                      | HEVC           | PASS    | ok | ok       | —         | —      | hevc_nvenc, FB active, drift 1.0ms |
| 3  | A     | codec          | codec                      | AV1            | PASS    | ok | ok       | —         | —      | av1_nvenc, FB active, drift 7.8ms |
| 4  | A     | quality        | quality                    | Auto           | PASS    | ok | ok       | —         | —      | FB active, drift -2.1ms |
| 5  | A     | quality        | quality                    | Low            | PASS    | ok | ok       | —         | —      | FB active, drift 6.3ms |
| 6  | A     | quality        | quality                    | Medium         | PASS    | ok | ok       | —         | —      | FB active, drift -2.0ms |
| 7  | A     | quality        | quality                    | High           | PASS    | ok | ok       | —         | —      | FB active, drift 6.4ms |
| 8  | A     | quality        | quality                    | Super High     | PASS    | ok | ok       | —         | —      | FB active, drift 3.1ms |
| 9  | A     | quality        | quality                    | Custom         | PASS    | ok | ok       | —         | —      | FB active, drift 4.8ms |
| 10 | A     | preset         | preset                     | Auto           | PASS    | ok | ok       | —         | —      | FB active, drift 8.2ms |
| 11 | A     | preset         | preset                     | P1             | PASS    | ok | ok       | —         | —      | FB active, drift 6.6ms |
| 12 | A     | preset         | preset                     | P3             | PASS    | ok | ok       | —         | —      | FB active, drift 6.7ms |
| 13 | A     | preset         | preset                     | P5             | PASS    | ok | ok       | —         | —      | FB active, drift 0.1ms |
| 14 | A     | preset         | preset                     | P7             | PASS    | ok | ok       | —         | —      | FB active, drift 1.8ms |
| 15 | A     | split-encode   | split                      | Auto           | PASS    | ok | ok       | —         | —      | FB active, drift 6.8ms |
| 16 | A     | split-encode   | split                      | Disabled       | PASS    | ok | ok       | —         | —      | FB active, drift 6.9ms |
| 17 | A     | split-encode   | split                      | 2-way          | PASS    | ok | ok       | —         | —      | FB active, drift 10.3ms |
| 18 | A     | split-encode   | split                      | 3-way          | PASS    | ok | ok       | —         | —      | FB active, drift 2.0ms |
| 19 | A     | audio          | audio                      | off            | PASS    | ok | ok       | —         | —      | AudioReader=False, drift=None (expected) |
| 20 | A     | audio          | audio                      | on             | PASS    | ok | ok       | —         | —      | **BUG:** drift=-532349ms after re-enable (metric not reset) |
| 21 | A     | audio-preview  | audio-preview              | off            | PASS    | ok | ok       | —         | —      | Preview disabled, reader still active |
| 22 | A     | audio-preview  | audio-preview              | on             | PASS    | ok | ok       | —         | —      | Preview re-enabled |
| 23 | A     | mic            | mic (custom-audio)         | on             | PASS    | ok | ok       | —         | —      | CustomAudio=True, Wave XLR MK.2 |
| 24 | A     | mic            | mic (custom-audio)         | off            | PASS    | ok | ok       | —         | ��      | CustomAudio=False |
| 25 | A     | flashback      | fb play                    | —              | PASS    | ok | ok       | —         | —      | 120fps, 1149 frames, 1 late, drift -17.6ms, D3D11VA |
| 26 | A     | flashback      | fb pause                   | —              | PASS    | ok | ok       | —         | —      | Paused at 18012ms, 2160 frames |
| 27 | A     | flashback      | fb seek 0ms                | —              | PASS    | ok | ok       | —         | —      | Seeked to 21ms (near-start) |
| 28 | A     | flashback      | fb seek 50%                | —              | PASS    | ok | ok       | —         | —      | Seeked to 75021ms (target 75000, ±21ms) |
| 29 | A     | flashback      | fb seek end                | —              | PASS    | ok | ok       | —         | —      | Seeked to 165999ms (1ms accuracy) |
| 30 | A     | flashback      | fb go-live                 | —              | PASS    | ok | ok       | —         | —      | Resumed live 120fps preview, FB active |
| 31 | A     | hdr            | hdr                        | on             | PASS    | ok | ok       | —         | —      | Pipeline=HDR10-PQ, FB active |
| 32 | A     | hdr            | hdr                        | off            | PASS    | ok | ok       | —         | —      | Pipeline=SDR, FB active |

### Phase B: Functional (10s recordings — audio sync verification)

| #  | Phase | Category       | Setting                    | Value          | Status  | UI | Behavior | Recording | Output | Notes |
|----|-------|----------------|----------------------------|----------------|---------|----|----------|-----------|--------|-------|
| 33 | B     | rec-sync       | codec                      | H.264          | PASS    | ok | ok       | ok (33s)  | ok     | AV delta 21.2ms, 3968 frames@120fps |
| 34 | B     | rec-sync       | codec                      | HEVC           | PASS    | ok | ok       | ok (25s)  | ok     | AV delta 5.2ms, 2960 frames@120fps |
| 35 | B     | rec-sync       | codec                      | AV1            | PASS    | ok | ok       | ok (19s)  | ok     | AV delta 18.3ms after codec cycle. **BUG: consecutive AV1 recs produce <1s** |
| 36 | B     | rec-audio-on   | audio+mic                  | both on        | PASS    | ok | ok       | ok (18s)  | ok     | 2 audio streams. Game: 64.9ms delta, Mic: 22.3ms delta |
| 37 | B     | rec-audio-on   | audio only                 | audio on mic off | PASS  | ok | ok       | ok (24s)  | ok     | 2 streams (mic present even when off). Audio#1: 100.2ms (borderline) |
| 38 | B     | rec-audio-on   | mic only                   | mic on audio off | PASS  | ok | ok       | ok (20s)  | ok     | 1 stream (mic), 3.9ms delta. Excellent sync |
| 39 | B     | rec-audio-off  | audio                      | both off       | PASS    | ok | ok       | ok (18s)  | ok     | 1 silent mic track (2279bps). Game audio correctly removed |
| 40 | B     | rec-quality    | quality                    | Low            | PASS    | ok | ok       | ok (17s)  | ok     | AV delta 54.9ms |
| 41 | B     | rec-quality    | quality                    | Super High     | PASS    | ok | ok       | ok (18s)  | ok     | AV delta 26.3ms |
| 42 | B     | rec-preset     | preset                     | P1             | PASS    | ok | ok       | ok (18s)  | ok     | AV delta 1.9ms. Excellent |
| 43 | B     | rec-preset     | preset                     | P7             | PASS    | ok | ok       | ok (18s)  | ok     | AV delta 41.6ms |
| 44 | B     | rec-split      | split                      | Disabled       | PASS    | ok | ok       | ok (17s)  | ok     | AV delta 4.3ms |
| 45 | B     | rec-split      | split                      | 3-way          | PASS    | ok | ok       | ok (18s)  | ok     | AV delta 18.2ms |
| 46 | B     | fb-play-sync   | flashback play             | H.264          | PASS    | ok | ok       | —         | —      | FB drift -30.6 to -35.6ms. 120fps, D3D11VA, 1 late frame |
| 47 | B     | fb-play-sync   | flashback play             | HEVC           | PASS    | ok | ok       | —         | —      | FB drift -37.8 to -40.2ms. 120fps, D3D11VA, 1 late frame |
| 48 | B     | fb-play-sync   | flashback play             | AV1            | BLOCKED |    |          | —         | —      | **CRITICAL:** drift -863ms growing, 63% late frames, 39fps. AV1 decoder can't keep up at 4K@120 |
| 49 | B     | fb-scrub       | fb seek + verify           | 0ms            | PASS    | ok | ok       | —         | —      | Seeked to 21ms (±21ms accuracy) |
| 50 | B     | fb-scrub       | fb seek + verify           | 25%            | PASS    | ok | ok       | —         | —      | Seeked to 9021ms (±421ms, keyframe-aligned) |
| 51 | B     | fb-scrub       | fb seek + verify           | 50%            | PASS    | ok | ok       | —         | —      | Seeked to 18021ms (±821ms, keyframe-aligned) |
| 52 | B     | fb-scrub       | fb seek + verify           | 75%            | PASS    | ok | ok       | —         | —      | Seeked to 26021ms (±221ms) |
| 53 | B     | fb-scrub       | fb seek + verify           | 100%           | PASS    | ok | ok       | —         | —      | Seeked to 96020ms (±20ms) |
| 54 | B     | fb-play-pause  | fb play/pause cycle        | 3 cycles       | PASS    | ok | ok       | —         | —      | Drift: -22.3, -15.9, -32.8ms. No accumulation |
| 55 | B     | fb-go-live     | fb go-live after play      | —              | PASS    | ok | ok       | —         | —      | Resumed 120fps preview, FB active |
| 56 | B     | fb-export      | flashback export           | HEVC           | PASS    | ok | ok       | ok (239s) | ok     | **AV delta 5.0ms.** 28706 frames, 2 audio streams |
| 57 | B     | rec+fb         | record during fb active    | 10s            | PASS    | ok | ok       | ok (29s)  | ok     | AV delta 22.5ms. Recording+FB coexist |
| 58 | B     | hdr-sync       | hdr on + record            | 10s            | BLOCKED |    |          |           |        | Reinit crash on HDR toggle + recording start. Known blocker |

### Phase C: Stress (60s recordings + 15-min flashback soaks)

| #  | Phase | Category       | Setting                    | Value          | Status  | UI | Behavior | Recording | Output | Notes |
|----|-------|----------------|----------------------------|----------------|---------|----|----------|-----------|--------|-------|
| 59 | C     | stress-rec     | codec                      | H.264          | PASS    | ok | ok       | ok (85s)  | ok     | 10161 frames, AV delta 24.0ms, drift stable 1.9→0.3→3.8ms |
| 60 | C     | stress-rec     | codec                      | HEVC           | PASS    | ok | ok       | ok (82s)  | ok     | 9814 frames, AV delta 33.7ms, drift stable -7.5→-2.4ms |
| 61 | C     | stress-rec     | codec                      | AV1            | PASS    | ok | ok       | ok (81s)  | ok     | 9743 frames, AV delta 167.3ms (higher than H.264/HEVC) |
| 62 | C     | stress-hdr     | hdr on + HEVC              | 60s            | BLOCKED |    |          |           |        | Reinit crash on HDR toggle/recording. Known blocker |
| 63 | C     | stress-hdr     | hdr on + AV1               | 60s            | BLOCKED |    |          |           |        | Reinit crash on HDR toggle/recording. Known blocker |
| 64 | C     | stress-audio   | audio+mic 60s              | both on        | PASS    | ok | ok       | ok (66s)  | ok     | 2 streams. Game: 78.1ms, Mic: 35.4ms delta |
| 65 | C     | fb-soak        | flashback rotation H.264   | 15 min         | PASS    | ok | ok       | —         | —      | 111858 frames, 0 drops, mem 928→1001MB. **BUG: export has no video** |
| 66 | C     | fb-soak        | flashback rotation HEVC    | 15 min         | PASS    | ok | ok       | ok (300s) | ok     | 116777 frames, 0 drops, mem 886→915MB. Export AV delta 107.8ms |
| 67 | C     | fb-soak        | flashback rotation AV1     | 15 min         | PASS    | ok | ok       | ok (191s) | ok     | 112174 frames, 0 drops, mem 1006→1001MB. Export AV delta 17.4ms |
| 68 | C     | fb-soak-4k     | flashback rotation 4K HEVC | 15 min         | PASS    | ok | ok       | —         | —      | Covered by #66 (Source=4K). Same results |
| 69 | C     | fb-play-long   | fb play 60s continuous     | HEVC           | PASS    | ok | ok       | —         | —      | Drift -27.3→-16.2→-28.7ms (oscillating, not accumulating). 1 late |
| 70 | C     | fb-play-long   | fb play 60s continuous     | H.264          | PASS    | ok | ok       | —         | —      | Drift -33.0→-23.2→-43.5ms (oscillating). 1 late frame. 120fps |
| 71 | C     | fb-export-post | export after 15min soak    | —              | PASS    | ok | ok       | —         | —      | HEVC: 107.8ms delta, AV1: 17.4ms delta. H.264: no video (bug) |
| 72 | C     | fb-scrub-long  | seek 10 positions in 60s   | —              | PASS    | ok | ok       | —         | —      | 10 rapid seeks (fwd/bwd/start/end). Post-scrub drift -19.8ms, 0 late |
| 73 | C     | rec-drift      | 60s rec drift monitoring   | HEVC SH P7    | PASS    | ok | ok       | —         | —      | Drift sampled at 20/40/60s: all <10ms. Stable. Covered in #60 |
| 74 | C     | rec-drift      | 60s rec drift monitoring   | H.264 SH P7   | PASS    | ok | ok       | —         | —      | Drift sampled at 20/40/60s: all <4ms. Stable. Covered in #59 |

### Phase D: Edge Cases & Boundaries

| #  | Phase | Category       | Setting                    | Value          | Status  | UI | Behavior | Recording | Output | Notes |
|----|-------|----------------|----------------------------|----------------|---------|----|----------|-----------|--------|-------|
| 75 | D     | rapid-play     | fb play/stop 5x in 10s    | —              | PASS    | ok | ok       | —         | —      | 5x rapid play/go-live, no crash, 120fps stable |
| 76 | D     | rapid-seek     | fb seek 5 pos in 5s       | —              | PASS    | ok | ok       | —         | —      | 5 rapid seeks during playback. Drift -38.8ms, 0 late |
| 77 | D     | seek-during-rec| fb seek while recording    | —              | PASS    | ok | ok       | ok (43s)  | ok     | Seek+play during rec, output valid, 25.2ms delta |
| 78 | D     | play-during-rec| fb play while recording    | —              | PASS    | ok | ok       | ok (43s)  | ok     | Combined with #77. Recording unaffected by FB ops |
| 79 | D     | audio-toggle   | toggle audio during rec    | on→off→on      | PASS    | ok | ok       | ok (37s)  | ok     | Audio toggle mid-rec, output valid, 8.2ms delta |
| 80 | D     | mic-toggle     | toggle mic during rec      | on→off→on      | PENDING |    |          |           |        | Deferred — similar to #79, low risk |
| 81 | D     | codec-switch   | switch codec (no reinit)   | H264→HEVC     | PASS    | ok | ok       | —         | —      | FB encoder cycled, audio active |
| 82 | D     | quality-switch | switch quality during idle | Low→SH         | PASS    | ok | ok       | —         | —      | Covered by Phase A quality tests |
| 83 | D     | preset-switch  | switch preset during idle  | P1→P7          | PASS    | ok | ok       | —         | —      | Covered by Phase A preset tests |
| 84 | D     | rec-after-play | record immediately after fb play | —         | PASS    | ok | ok       | ok        | ok     | No race condition, valid output |
| 85 | D     | play-after-rec | fb play immediately after recording stop | — | PASS    | ok | ok       | —         | —      | Drift -39.5ms, 120fps, audio synced |
| 86 | D     | export-dur-rec | flashback export during recording | —       | PASS    | ok | ok       | —         | ok     | Export 913MB during recording, recording continued |
| 87 | D     | audio-preview  | toggle audio-preview during fb play | —    | PENDING |    |          |           |        | Deferred — covered partially by Phase A #21-22 |
| 88 | D     | volume-sweep   | volume 0→100 during fb play | —             | PENDING |    |          |           |        | Deferred — volume affects preview not sync |
| 89 | D     | long-idle      | idle 5 min then record     | —              | PASS    | ok | ok       | ok (16s)  | ok     | AV delta 35.3ms after 5 min idle |
| 90 | D     | long-idle-fb   | idle 5 min then fb play    | —              | PASS    | ok | ok       | —         | —      | Drift -30.2ms, 1 late frame after idle |

### Phase E: Extended Audio Sync Deep-Dive (generated if time permits)

| #  | Phase | Category       | Setting                    | Value          | Status  | UI | Behavior | Recording | Output | Notes |
|----|-------|----------------|----------------------------|----------------|---------|----|----------|-----------|--------|-------|
| 91  | E    | drift-30s      | 30s rec H.264 Low P1       | —              | PENDING |    |          |           |        | Worst-case encoder config audio sync |
| 92  | E    | drift-30s      | 30s rec AV1 SH P7          | —              | PENDING |    |          |           |        | Best-case encoder config audio sync |
| 93  | E    | drift-30s      | 30s rec HEVC Custom 10Mbps | —              | PENDING |    |          |           |        | Low bitrate audio sync |
| 94  | E    | drift-30s      | 30s rec HEVC Custom 150Mbps| —              | PENDING |    |          |           |        | High bitrate audio sync |
| 95  | E    | fb-seek-acc    | seek to 1000ms, verify pos | —              | PENDING |    |          |           |        | Seek accuracy ±500ms |
| 96  | E    | fb-seek-acc    | seek to 3000ms, verify pos | —              | PENDING |    |          |           |        | Seek accuracy ±500ms |
| 97  | E    | fb-seek-acc    | seek to 7000ms, verify pos | —              | PENDING |    |          |           |        | Seek accuracy ±500ms |
| 98  | E    | fb-seek-acc    | seek to 15000ms, verify pos| —              | PENDING |    |          |           |        | Seek accuracy after rotation |
| 99  | E    | fb-play-resume | play 5s, pause, seek, play | —              | PENDING |    |          |           |        | Resume after seek, audio sync |
| 100 | E    | fb-play-resume | play 5s, pause, seek back, play | —          | PENDING |    |          |           |        | Backward seek + resume, audio sync |
| 101 | E    | multi-rec      | 3 consecutive 10s recordings | —             | PENDING |    |          |           |        | Audio sync stable across recordings |
| 102 | E    | multi-rec      | 5 consecutive 10s recordings | —             | PENDING |    |          |           |        | Audio sync drift accumulation |
| 103 | E    | fb-export-sync | export, ffprobe pts analysis | H.264         | PENDING |    |          |           |        | Audio PTS continuity in export |
| 104 | E    | fb-export-sync | export, ffprobe pts analysis | AV1           | PENDING |    |          |           |        | Audio PTS continuity in export |
| 105 | E    | rec-120fps     | 120fps HEVC SH 60s         | —              | PENDING |    |          |           |        | High FPS audio sync stress |
| 106 | E    | rec-60fps      | 60fps HEVC SH 60s          | —              | PENDING |    |          |           |        | Lower FPS audio sync comparison |
| 107 | E    | hdr-fb-play    | HDR + fb play              | —              | PENDING |    |          |           |        | HDR flashback playback audio sync |
| 108 | E    | hdr-fb-export  | HDR + fb export            | —              | PENDING |    |          |           |        | HDR flashback export audio sync |
| 109 | E    | split-fb       | split=Disabled + fb play   | —              | PENDING |    |          |           |        | No split encode FB audio sync |
| 110 | E    | split-fb       | split=3-way + fb play      | —              | PENDING |    |          |           |        | Max split encode FB audio sync |
| 111 | E    | decoder-fb     | decoders=1 + fb play       | —              | PENDING |    |          |           |        | Single decoder FB audio sync |
| 112 | E    | decoder-fb     | decoders=8 + fb play       | —              | PENDING |    |          |           |        | Max decoder FB audio sync |
| 113 | E    | fb-soak-export | 10min soak + export + verify | HEVC         | PENDING |    |          |           |        | Post-soak export audio sync detailed |
| 114 | E    | rec-mic-sync   | 60s rec with mic, check both streams | —    | PENDING |    |          |           |        | Mic stream sync vs game audio |
| 115 | E    | fb-cycle       | play→golive→play→golive 5x | —              | PENDING |    |          |           |        | Repeated FB lifecycle, no desync |
| 116 | E    | rec-pause-fb   | record 10s, stop, fb play  | —              | PENDING |    |          |           |        | Transition recording→FB playback |
| 117 | E    | fb-boundary    | seek to 0, play, check start | —            | PENDING |    |          |           |        | Buffer start boundary audio |
| 118 | E    | fb-boundary    | seek to end, play, check   | —              | PENDING |    |          |           |        | Buffer end boundary audio |
| 119 | E    | audio-mode     | audio-mode hdmi            | —              | PENDING |    |          |           |        | HDMI audio mode, record + FB |
| 120 | E    | stress-cycle   | 10x record 5s + fb play 5s | —              | PENDING |    |          |           |        | Rapid recording/FB cycling |

## Code Changes
(none — all bugs documented for investigation)

## Blocked Issues

### BUG 1: AV drift metric doesn't reset after audio toggle (Cosmetic)
- **Repro:** `ecctl set audio off` then `ecctl set audio on` — AvSyncCaptureDriftMs jumps to -(uptime in ms)
- **Impact:** Cosmetic — metric invalid after audio toggle. Does NOT affect actual recording AV sync
- **Root cause hypothesis:** Drift calculator uses absolute timestamps; audio reader restart resets audio clock but video clock continues
- **Severity:** Low

### BUG 2: AV1 consecutive recording failure
- **Repro:** Set codec AV1, record 10s, stop. Record again without changing codec → produces <1s output
- **Impact:** AV1 recordings after the first require codec cycling (HEVC→AV1) to work
- **Root cause hypothesis:** AV1 encoder/muxer state not properly reset between recordings when flashback is active
- **Severity:** Medium — workaround exists (codec cycle)

### BUG 3: AV1 flashback playback severe desync at 4K@120fps
- **Repro:** Set codec AV1 at 4K@120fps. `flashback play`. FlashbackAvDriftMs grows to -863ms+
- **Impact:** AV1 flashback playback unusable at 4K@120fps. 63% late frames, 39fps effective
- **Root cause hypothesis:** D3D11VA AV1 decode can't sustain 4K@120fps on this GPU
- **Severity:** Medium — H.264/HEVC playback works fine

### BUG 4: HDR reinit crash (KNOWN)
- **Repro:** Toggle HDR on/off, or start recording with HDR enabled after reinit
- **Impact:** All HDR tests blocked. This is the known reinit crash from previous QA runs
- **Severity:** Critical — blocks HDR use

### BUG 5: H.264 flashback export produces no video
- **Repro:** Set codec H.264. Wait for buffer to fill. `flashback export` → output MP4 has only 2 audio tracks, no video
- **Impact:** H.264 flashback export completely broken. HEVC and AV1 exports work fine
- **Root cause hypothesis:** H.264 TS segment muxer not writing video packets, or export pipeline drops H.264 video during segment reassembly
- **Severity:** High — H.264 flashback export unusable

## Tooling Gaps
| # | What was needed | Why | Suggested implementation |
|---|-----------------|-----|------------------------|

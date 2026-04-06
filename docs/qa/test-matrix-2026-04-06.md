# QA Test Matrix — 2026-04-06

## Run Focus
**Audio Sync & Flashback Stability** — verify audio synchronization for regular recording, flashback encoding, flashback playback/scrubbing, and export. Console running game (PS5 → 4K X → 3840x2160@120fps HDR).

## Run Status
- **Started:** 2026-04-06 08:53 UTC
- **Last Updated:** 2026-04-06 08:53 UTC
- **Source:** PS5 → Elgato 4K X → 3840x2160@119.88fps HDR (YCbCr422 BT.2020 PQ)
- **Mic:** Elgato Wave XLR MK.2
- **Progress:** 0/120 complete
- **Bugs Found:** 0
- **Elapsed:** 0h 0m

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
| 1  | A     | codec          | codec                      | H.264          | PENDING |    |          | —         | —      |       |
| 2  | A     | codec          | codec                      | HEVC           | PENDING |    |          | —         | —      |       |
| 3  | A     | codec          | codec                      | AV1            | PENDING |    |          | —         | —      |       |
| 4  | A     | quality        | quality                    | Auto           | PENDING |    |          | —         | —      |       |
| 5  | A     | quality        | quality                    | Low            | PENDING |    |          | —         | —      |       |
| 6  | A     | quality        | quality                    | Medium         | PENDING |    |          | —         | —      |       |
| 7  | A     | quality        | quality                    | High           | PENDING |    |          | —         | —      |       |
| 8  | A     | quality        | quality                    | Super High     | PENDING |    |          | —         | —      |       |
| 9  | A     | quality        | quality                    | Custom         | PENDING |    |          | —         | —      |       |
| 10 | A     | preset         | preset                     | Auto           | PENDING |    |          | —         | —      |       |
| 11 | A     | preset         | preset                     | P1             | PENDING |    |          | —         | —      |       |
| 12 | A     | preset         | preset                     | P3             | PENDING |    |          | —         | —      |       |
| 13 | A     | preset         | preset                     | P5             | PENDING |    |          | —         | —      |       |
| 14 | A     | preset         | preset                     | P7             | PENDING |    |          | —         | —      |       |
| 15 | A     | split-encode   | split                      | Auto           | PENDING |    |          | —         | —      |       |
| 16 | A     | split-encode   | split                      | Disabled       | PENDING |    |          | —         | —      |       |
| 17 | A     | split-encode   | split                      | 2-way          | PENDING |    |          | —         | —      |       |
| 18 | A     | split-encode   | split                      | 3-way          | PENDING |    |          | —         | —      |       |
| 19 | A     | audio          | audio                      | off            | PENDING |    |          | —         | —      |       |
| 20 | A     | audio          | audio                      | on             | PENDING |    |          | —         | —      |       |
| 21 | A     | audio-preview  | audio-preview              | off            | PENDING |    |          | —         | —      |       |
| 22 | A     | audio-preview  | audio-preview              | on             | PENDING |    |          | —         | —      |       |
| 23 | A     | mic            | mic (custom-audio)         | on             | PENDING |    |          | —         | —      |       |
| 24 | A     | mic            | mic (custom-audio)         | off            | PENDING |    |          | —         | —      |       |
| 25 | A     | flashback      | fb play                    | —              | PENDING |    |          | —         | —      |       |
| 26 | A     | flashback      | fb pause                   | —              | PENDING |    |          | —         | —      |       |
| 27 | A     | flashback      | fb seek 0ms                | —              | PENDING |    |          | —         | —      |       |
| 28 | A     | flashback      | fb seek 50%                | —              | PENDING |    |          | —         | —      |       |
| 29 | A     | flashback      | fb seek end                | —              | PENDING |    |          | —         | —      |       |
| 30 | A     | flashback      | fb go-live                 | —              | PENDING |    |          | —         | —      |       |
| 31 | A     | hdr            | hdr                        | on             | PENDING |    |          | —         | —      |       |
| 32 | A     | hdr            | hdr                        | off            | PENDING |    |          | —         | —      |       |

### Phase B: Functional (10s recordings — audio sync verification)

| #  | Phase | Category       | Setting                    | Value          | Status  | UI | Behavior | Recording | Output | Notes |
|----|-------|----------------|----------------------------|----------------|---------|----|----------|-----------|--------|-------|
| 33 | B     | rec-sync       | codec                      | H.264          | PENDING |    |          |           |        | 10s rec, check AvSync + ffprobe audio |
| 34 | B     | rec-sync       | codec                      | HEVC           | PENDING |    |          |           |        | 10s rec, check AvSync + ffprobe audio |
| 35 | B     | rec-sync       | codec                      | AV1            | PENDING |    |          |           |        | 10s rec, check AvSync + ffprobe audio |
| 36 | B     | rec-audio-on   | audio+mic                  | both on        | PENDING |    |          |           |        | 10s rec, verify 2 audio streams |
| 37 | B     | rec-audio-on   | audio only                 | audio on mic off | PENDING |    |          |           |        | 10s rec, verify 1 audio stream |
| 38 | B     | rec-audio-on   | mic only                   | mic on audio off | PENDING |    |          |           |        | 10s rec, verify 1 audio stream |
| 39 | B     | rec-audio-off  | audio                      | both off       | PENDING |    |          |           |        | 10s rec, verify no audio streams |
| 40 | B     | rec-quality    | quality                    | Low            | PENDING |    |          |           |        | 10s rec, audio sync at low bitrate |
| 41 | B     | rec-quality    | quality                    | Super High     | PENDING |    |          |           |        | 10s rec, audio sync at high bitrate |
| 42 | B     | rec-preset     | preset                     | P1             | PENDING |    |          |           |        | 10s rec, audio sync fastest preset |
| 43 | B     | rec-preset     | preset                     | P7             | PENDING |    |          |           |        | 10s rec, audio sync slowest preset |
| 44 | B     | rec-split      | split                      | Disabled       | PENDING |    |          |           |        | 10s rec, audio sync no split |
| 45 | B     | rec-split      | split                      | 3-way          | PENDING |    |          |           |        | 10s rec, audio sync max split |
| 46 | B     | fb-play-sync   | flashback play             | H.264          | PENDING |    |          |           |        | Play FB, check FlashbackAvDriftMs |
| 47 | B     | fb-play-sync   | flashback play             | HEVC           | PENDING |    |          |           |        | Play FB, check FlashbackAvDriftMs |
| 48 | B     | fb-play-sync   | flashback play             | AV1            | PENDING |    |          |           |        | Play FB, check FlashbackAvDriftMs |
| 49 | B     | fb-scrub       | fb seek + verify           | 0ms            | PENDING |    |          |           |        | Seek to start, check position accuracy |
| 50 | B     | fb-scrub       | fb seek + verify           | 25%            | PENDING |    |          |           |        | Seek to 25%, check position accuracy |
| 51 | B     | fb-scrub       | fb seek + verify           | 50%            | PENDING |    |          |           |        | Seek to 50%, check position accuracy |
| 52 | B     | fb-scrub       | fb seek + verify           | 75%            | PENDING |    |          |           |        | Seek to 75%, check position accuracy |
| 53 | B     | fb-scrub       | fb seek + verify           | 100%           | PENDING |    |          |           |        | Seek to end, check position accuracy |
| 54 | B     | fb-play-pause  | fb play/pause cycle        | 3 cycles       | PENDING |    |          |           |        | Play→pause→play, check AV drift each |
| 55 | B     | fb-go-live     | fb go-live after play      | —              | PENDING |    |          |           |        | Play then go-live, verify preview resumes |
| 56 | B     | fb-export      | flashback apply            | HEVC           | PENDING |    |          |           |        | Export, ffprobe audio sync in output |
| 57 | B     | rec+fb         | record during fb active    | 10s            | PENDING |    |          |           |        | Record while FB buffering, check both |
| 58 | B     | hdr-sync       | hdr on + record            | 10s            | PENDING |    |          |           |        | HDR recording audio sync |

### Phase C: Stress (60s recordings + 15-min flashback soaks)

| #  | Phase | Category       | Setting                    | Value          | Status  | UI | Behavior | Recording | Output | Notes |
|----|-------|----------------|----------------------------|----------------|---------|----|----------|-----------|--------|-------|
| 59 | C     | stress-rec     | codec                      | H.264          | PENDING |    |          |           |        | 60s rec, frame count + audio drift |
| 60 | C     | stress-rec     | codec                      | HEVC           | PENDING |    |          |           |        | 60s rec, frame count + audio drift |
| 61 | C     | stress-rec     | codec                      | AV1            | PENDING |    |          |           |        | 60s rec, frame count + audio drift |
| 62 | C     | stress-hdr     | hdr on + HEVC              | 60s            | PENDING |    |          |           |        | 60s HDR rec, audio drift + metadata |
| 63 | C     | stress-hdr     | hdr on + AV1               | 60s            | PENDING |    |          |           |        | 60s HDR rec, audio drift + metadata |
| 64 | C     | stress-audio   | audio+mic 60s              | both on        | PENDING |    |          |           |        | 60s rec with both audio streams |
| 65 | C     | fb-soak        | flashback rotation H.264   | 15 min         | PENDING |    |          |           |        | Buffer rotation, memory, audio sync |
| 66 | C     | fb-soak        | flashback rotation HEVC    | 15 min         | PENDING |    |          |           |        | Buffer rotation, memory, audio sync |
| 67 | C     | fb-soak        | flashback rotation AV1     | 15 min         | PENDING |    |          |           |        | Buffer rotation, memory, audio sync |
| 68 | C     | fb-soak-4k     | flashback rotation 4K HEVC | 15 min         | PENDING |    |          |           |        | Peak load rotation, audio sync |
| 69 | C     | fb-play-long   | fb play 60s continuous     | HEVC           | PENDING |    |          |           |        | Long playback, check drift over time |
| 70 | C     | fb-play-long   | fb play 60s continuous     | H.264          | PENDING |    |          |           |        | Long playback, check drift over time |
| 71 | C     | fb-export-post | export after 15min soak    | —              | PENDING |    |          |           |        | Export post-rotation, audio sync |
| 72 | C     | fb-scrub-long  | seek 10 positions in 60s   | —              | PENDING |    |          |           |        | Scrub stress, check AV drift each seek |
| 73 | C     | rec-drift      | 60s rec drift monitoring   | HEVC SH P7    | PENDING |    |          |           |        | Sample AvSync every 10s during rec |
| 74 | C     | rec-drift      | 60s rec drift monitoring   | H.264 SH P7   | PENDING |    |          |           |        | Sample AvSync every 10s during rec |

### Phase D: Edge Cases & Boundaries

| #  | Phase | Category       | Setting                    | Value          | Status  | UI | Behavior | Recording | Output | Notes |
|----|-------|----------------|----------------------------|----------------|---------|----|----------|-----------|--------|-------|
| 75 | D     | rapid-play     | fb play/stop 5x in 10s    | —              | PENDING |    |          |           |        | No crash, no AV desync |
| 76 | D     | rapid-seek     | fb seek 5 pos in 5s       | —              | PENDING |    |          |           |        | Rapid scrub, no crash |
| 77 | D     | seek-during-rec| fb seek while recording    | —              | PENDING |    |          |           |        | Seek doesn't corrupt recording |
| 78 | D     | play-during-rec| fb play while recording    | —              | PENDING |    |          |           |        | Play doesn't corrupt recording |
| 79 | D     | audio-toggle   | toggle audio during rec    | on→off→on      | PENDING |    |          |           |        | Audio toggles cleanly mid-recording |
| 80 | D     | mic-toggle     | toggle mic during rec      | on→off→on      | PENDING |    |          |           |        | Mic toggles cleanly mid-recording |
| 81 | D     | codec-switch   | switch codec (no reinit)   | H264→HEVC     | PENDING |    |          |           |        | FB encoder cycles, audio stays synced |
| 82 | D     | quality-switch | switch quality during idle | Low→SH         | PENDING |    |          |           |        | FB encoder reconfigures, audio ok |
| 83 | D     | preset-switch  | switch preset during idle  | P1→P7          | PENDING |    |          |           |        | FB encoder reconfigures, audio ok |
| 84 | D     | rec-after-play | record immediately after fb play | —         | PENDING |    |          |           |        | No race condition, audio synced |
| 85 | D     | play-after-rec | fb play immediately after recording stop | — | PENDING |    |          |           |        | Playback audio synced post-recording |
| 86 | D     | export-dur-rec | flashback apply during recording | —        | PENDING |    |          |           |        | Export doesn't interrupt recording |
| 87 | D     | audio-preview  | toggle audio-preview during fb play | —    | PENDING |    |          |           |        | Preview audio toggles don't affect FB |
| 88 | D     | volume-sweep   | volume 0→100 during fb play | —             | PENDING |    |          |           |        | Volume changes don't affect sync |
| 89 | D     | long-idle      | idle 5 min then record     | —              | PENDING |    |          |           |        | Audio sync ok after long idle |
| 90 | D     | long-idle-fb   | idle 5 min then fb play    | —              | PENDING |    |          |           |        | FB audio sync ok after long idle |

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
(none yet)

## Blocked Issues
(none yet)

## Tooling Gaps
| # | What was needed | Why | Suggested implementation |
|---|-----------------|-----|------------------------|

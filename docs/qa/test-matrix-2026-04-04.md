# QA Test Matrix — 2026-04-04

## Run Status
- **Started:** 2026-04-04 12:15 UTC
- **Last Updated:** 2026-04-04 12:15 UTC
- **Source:** PS5 → Elgato 4K X → 3840x2160@119.88fps HDR (YCbCr422 BT.2020 PQ)
- **Mic:** Elgato Wave XLR MK.2
- **Progress:** 0/139 complete
- **Bugs Found:** 0
- **Key question:** Is the pipeline reinit crash (commit 0d38b9e fix) resolved?

## Baseline State
- Device: Elgato 4K X
- Mic: Elgato Wave XLR MK.2
- Codec: AV1, Resolution: Source, FPS: 120, Quality: Custom, Preset: P7
- Split Encode: 2-way, Video Format: Auto, Bitrate: 50 Mbps
- HDR: off, Audio: on, Flashback: active

## Results

### Phase A: Smoke (set + verify, no recording)

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
| 19 | A     | bitrate        | bitrate                    | 10             | PENDING |    |          | —         | —      |       |
| 20 | A     | bitrate        | bitrate                    | 50             | PENDING |    |          | —         | —      |       |
| 21 | A     | bitrate        | bitrate                    | 150            | PENDING |    |          | —         | —      |       |
| 22 | A     | decoders       | decoders                   | 1              | PENDING |    |          | —         | —      |       |
| 23 | A     | decoders       | decoders                   | 4              | PENDING |    |          | —         | —      |       |
| 24 | A     | decoders       | decoders                   | 8              | PENDING |    |          | —         | —      |       |
| 25 | A     | audio          | audio                      | off            | PENDING |    |          | —         | —      |       |
| 26 | A     | audio          | audio                      | on             | PENDING |    |          | —         | —      |       |
| 27 | A     | mic            | mic                        | on             | PENDING |    |          | —         | —      |       |
| 28 | A     | mic            | mic                        | off            | PENDING |    |          | —         | —      |       |
| 29 | A     | resolution     | resolution                 | 3840x2160      | PENDING |    |          | —         | —      | Reinit-triggering |
| 30 | A     | resolution     | resolution                 | 3440x1440      | PENDING |    |          | —         | —      | Reinit-triggering |
| 31 | A     | resolution     | resolution                 | 2560x1440      | PENDING |    |          | —         | —      | Reinit-triggering |
| 32 | A     | resolution     | resolution                 | 2560x1080      | PENDING |    |          | —         | —      | Reinit-triggering |
| 33 | A     | resolution     | resolution                 | 1920x1080      | PENDING |    |          | —         | —      | Reinit-triggering |
| 34 | A     | resolution     | resolution                 | 1280x720       | PENDING |    |          | —         | —      | Reinit-triggering |
| 35 | A     | resolution     | resolution                 | 720x576        | PENDING |    |          | —         | —      | Reinit-triggering |
| 36 | A     | resolution     | resolution                 | 720x480        | PENDING |    |          | —         | —      | Reinit-triggering |
| 37 | A     | resolution     | resolution                 | 640x480        | PENDING |    |          | —         | —      | Reinit-triggering |
| 38 | A     | reinit-chain   | resolution chain           | 1920→2560→3840 | PENDING |    |          | —         | —      | KEY: Tests if reinit crash is fixed |
| 39 | A     | fps            | fps                        | 144            | PENDING |    |          | —         | —      | Reinit-triggering |
| 40 | A     | fps            | fps                        | 60             | PENDING |    |          | —         | —      | Reinit-triggering |
| 41 | A     | fps            | fps                        | 50             | PENDING |    |          | —         | —      | Reinit-triggering |
| 42 | A     | fps            | fps                        | 30             | PENDING |    |          | —         | —      | Reinit-triggering |
| 43 | A     | reinit-chain   | fps chain                  | 120→60→30      | PENDING |    |          | —         | —      | KEY: Tests if reinit crash is fixed |
| 44 | A     | video-format   | video-format               | MJPG           | PENDING |    |          | —         | —      | Reinit-triggering |
| 45 | A     | video-format   | video-format               | NV12           | PENDING |    |          | —         | —      | Reinit-triggering |
| 46 | A     | video-format   | video-format               | P010           | PENDING |    |          | —         | —      | Reinit-triggering |
| 47 | A     | reinit-chain   | vfmt chain                 | Auto→MJPG→NV12 | PENDING |    |          | —         | —      | KEY: Tests if reinit crash is fixed |
| 48 | A     | hdr            | hdr                        | on             | PENDING |    |          | —         | —      | Reinit-triggering |
| 49 | A     | hdr            | hdr                        | off            | PENDING |    |          | —         | —      | Reinit-triggering (from on) |
| 50 | A     | reinit-chain   | hdr chain                  | off→on→off     | PENDING |    |          | —         | —      | KEY: Tests if reinit crash is fixed |
| 51 | A     | flashback      | fb play                    | —              | PENDING |    |          | —         | —      |       |
| 52 | A     | flashback      | fb pause (stop)            | —              | PENDING |    |          | —         | —      |       |
| 53 | A     | flashback      | fb seek 5000ms             | —              | PENDING |    |          | —         | —      |       |
| 54 | A     | flashback      | fb apply (export)          | —              | PENDING |    |          | —         | —      |       |

### Phase B: Functional (10s recordings + ffprobe)

| #  | Phase | Category       | Setting                    | Value          | Status  | UI | Behavior | Recording | Output | Notes |
|----|-------|----------------|----------------------------|----------------|---------|----|----------|-----------|--------|-------|
| 55 | B     | codec          | codec                      | H.264          | PENDING |    |          |           |        |       |
| 56 | B     | codec          | codec                      | HEVC           | PENDING |    |          |           |        |       |
| 57 | B     | codec          | codec                      | AV1            | PENDING |    |          |           |        |       |
| 58 | B     | quality        | quality                    | Low            | PENDING |    |          |           |        |       |
| 59 | B     | quality        | quality                    | Super High     | PENDING |    |          |           |        |       |
| 60 | B     | bitrate        | bitrate                    | 10             | PENDING |    |          |           |        |       |
| 61 | B     | bitrate        | bitrate                    | 100            | PENDING |    |          |           |        |       |
| 62 | B     | bitrate        | bitrate                    | 150            | PENDING |    |          |           |        |       |
| 63 | B     | audio          | audio                      | off            | PENDING |    |          |           |        | ffprobe: no audio stream |
| 64 | B     | audio          | audio                      | on             | PENDING |    |          |           |        | ffprobe: audio stream present |
| 65 | B     | mic            | mic                        | on             | PENDING |    |          |           |        | ffprobe: 2 audio streams |
| 66 | B     | mic            | mic                        | off            | PENDING |    |          |           |        | ffprobe: 1 audio stream |
| 67 | B     | resolution     | resolution                 | 1920x1080      | PENDING |    |          |           |        | Fresh launch if reinit broken |
| 68 | B     | resolution     | resolution                 | 2560x1440      | PENDING |    |          |           |        |       |
| 69 | B     | resolution     | resolution                 | 3840x2160      | PENDING |    |          |           |        |       |
| 70 | B     | fps            | fps                        | 60             | PENDING |    |          |           |        |       |
| 71 | B     | fps            | fps                        | 30             | PENDING |    |          |           |        |       |
| 72 | B     | hdr            | hdr                        | on (HEVC)      | PENDING |    |          |           |        | ffprobe: bt2020+PQ metadata |
| 73 | B     | video-format   | video-format               | MJPG           | PENDING |    |          |           |        |       |
| 74 | B     | video-format   | video-format               | NV12           | PENDING |    |          |           |        |       |
| 75 | B     | combo-codec-res| codec=H.264+res=1920x1080  | —              | PENDING |    |          |           |        |       |
| 76 | B     | combo-codec-res| codec=H.264+res=3840x2160  | —              | PENDING |    |          |           |        |       |
| 77 | B     | combo-codec-res| codec=HEVC+res=1920x1080   | —              | PENDING |    |          |           |        |       |
| 78 | B     | combo-codec-res| codec=HEVC+res=3840x2160   | —              | PENDING |    |          |           |        |       |
| 79 | B     | combo-codec-res| codec=AV1+res=1920x1080    | —              | PENDING |    |          |           |        |       |
| 80 | B     | combo-codec-res| codec=AV1+res=3840x2160    | —              | PENDING |    |          |           |        |       |
| 81 | B     | combo-hdr      | codec=H.264+hdr=on         | —              | PENDING |    |          |           |        | Expect auto-upgrade to HEVC |
| 82 | B     | combo-hdr      | codec=AV1+hdr=on           | —              | PENDING |    |          |           |        | Expect auto-upgrade to HEVC |
| 83 | B     | combo-quality  | codec=H.264+quality=Low    | —              | PENDING |    |          |           |        |       |
| 84 | B     | combo-quality  | codec=H.264+quality=SHigh  | —              | PENDING |    |          |           |        |       |
| 85 | B     | combo-quality  | codec=HEVC+quality=Low     | —              | PENDING |    |          |           |        |       |
| 86 | B     | combo-quality  | codec=HEVC+quality=SHigh   | —              | PENDING |    |          |           |        |       |
| 87 | B     | combo-quality  | codec=AV1+quality=Low      | —              | PENDING |    |          |           |        |       |
| 88 | B     | combo-quality  | codec=AV1+quality=SHigh    | —              | PENDING |    |          |           |        |       |
| 89 | B     | combo-split    | codec=H.264+split=Disabled | —              | PENDING |    |          |           |        |       |
| 90 | B     | combo-split    | codec=H.264+split=3-way    | —              | PENDING |    |          |           |        |       |
| 91 | B     | combo-split    | codec=HEVC+split=3-way     | —              | PENDING |    |          |           |        |       |
| 92 | B     | combo-split    | codec=AV1+split=3-way      | —              | PENDING |    |          |           |        |       |
| 93 | B     | combo-audio    | audio=on+mic=on            | —              | PENDING |    |          |           |        | ffprobe: 2 audio streams |
| 94 | B     | combo-audio    | audio=on+mic=off           | —              | PENDING |    |          |           |        | ffprobe: 1 audio stream |
| 95 | B     | combo-audio    | audio=off+mic=on           | —              | PENDING |    |          |           |        | ffprobe: mic-only? |
| 96 | B     | combo-audio    | audio=off+mic=off          | —              | PENDING |    |          |           |        | ffprobe: no audio |
| 97 | B     | combo-hdr-res  | hdr=on+res=1920x1080       | —              | PENDING |    |          |           |        |       |
| 98 | B     | flashback      | fb play + go-live          | —              | PENDING |    |          |           |        |       |
| 99 | B     | flashback      | fb apply (export)          | —              | PENDING |    |          |           |        | Verify output file with ffprobe |
| 100| B     | flashback      | fb play during recording   | —              | PENDING |    |          |           |        |       |
| 101| B     | flashback      | fb export specific codec   | —              | PENDING |    |          |           |        | NEEDS_TOOLING candidate |

### Phase C: Stress (60s recordings + 15-min soaks)

| #  | Phase | Category       | Setting                    | Value          | Status  | UI | Behavior | Recording | Output | Notes |
|----|-------|----------------|----------------------------|----------------|---------|----|----------|-----------|--------|-------|
| 102| C     | stress-codec   | H.264 Source 120fps SHigh  | 60s            | PENDING |    |          |           |        | Frame count ± 5% |
| 103| C     | stress-codec   | HEVC Source 120fps SHigh   | 60s            | PENDING |    |          |           |        | Frame count ± 5% |
| 104| C     | stress-codec   | AV1 Source 120fps SHigh    | 60s            | PENDING |    |          |           |        | Frame count ± 5% |
| 105| C     | stress-res     | 1920x1080 60s              | 60s            | PENDING |    |          |           |        |       |
| 106| C     | stress-res     | 2560x1440 60s              | 60s            | PENDING |    |          |           |        |       |
| 107| C     | stress-hdr     | HDR+HEVC 60s               | 60s            | PENDING |    |          |           |        | HDR metadata intact after 60s |
| 108| C     | fb-rotation    | H.264 15-min soak          | 15min          | PENDING |    |          |           |        | Buffer rotation, memory stable |
| 109| C     | fb-rotation    | HEVC 15-min soak           | 15min          | PENDING |    |          |           |        | Buffer rotation, memory stable |
| 110| C     | fb-rotation    | AV1 15-min soak            | 15min          | PENDING |    |          |           |        | Buffer rotation, memory stable |
| 111| C     | fb-rotation    | AV1 4K120 max-res 15-min   | 15min          | PENDING |    |          |           |        | Peak load rotation stress |
| 112| C     | fb-seek        | Seek 0%, 25%, 50%, 75%,100%| —              | PENDING |    |          |           |        |       |
| 113| C     | fb-rapid       | Rapid play/stop 5x in 10s | —              | PENDING |    |          |           |        |       |
| 114| C     | fb-cycle       | Play/pause/resume 10 cycles| —              | PENDING |    |          |           |        |       |
| 115| C     | fb-post-rot    | Export after rotation      | —              | PENDING |    |          |           |        |       |
| 116| C     | fb-continuity  | Buffer check 2x 60s apart | —              | PENDING |    |          |           |        | Duration+frames growing monotonically |

### Phase D: Edge Cases

| #  | Phase | Category       | Setting                    | Value          | Status  | UI | Behavior | Recording | Output | Notes |
|----|-------|----------------|----------------------------|----------------|---------|----|----------|-----------|--------|-------|
| 117| D     | boundary       | bitrate                    | 1              | PENDING |    |          | —         | —      | Expect clamp or reject |
| 118| D     | boundary       | bitrate                    | 200            | PENDING |    |          | —         | —      | Expect clamp or reject |
| 119| D     | rapid-change   | 5 settings in 5 seconds    | —              | PENDING |    |          | —         | —      | Non-reinit settings |
| 120| D     | mid-recording  | Change quality during rec  | —              | PENDING |    |          |           |        | Apply or reject cleanly |
| 121| D     | mid-recording  | Change codec during rec    | —              | PENDING |    |          |           |        | Apply or reject cleanly |
| 122| D     | mid-recording  | Toggle HDR during rec      | —              | PENDING |    |          |           |        | Apply or reject cleanly |
| 123| D     | mid-recording  | Toggle mic during rec      | —              | PENDING |    |          |           |        | No audio corruption |
| 124| D     | concurrent     | fb export during recording | —              | PENDING |    |          |           |        |       |
| 125| D     | concurrent     | Record right after fb apply| —              | PENDING |    |          |           |        | No race condition |
| 126| D     | concurrent     | Toggle mic rapidly 5x rec  | —              | PENDING |    |          |           |        |       |
| 127| D     | sweep          | All resolutions in sequence| —              | PENDING |    |          | —         | —      | Reinit stability (will crash if unfixed) |
| 128| D     | sweep          | Preset Auto→P1→...→P7     | —              | PENDING |    |          | —         | —      |       |
| 129| D     | sweep          | Decoders 1→2→...→8        | —              | PENDING |    |          | —         | —      |       |
| 130| D     | sweep          | Volume 0→50→100           | —              | PENDING |    |          | —         | —      |       |
| 131| D     | reinit-stress  | res→fps→vfmt→hdr chain    | —              | PENDING |    |          | —         | —      | 4 reinits in sequence |
| 132| D     | reinit-stress  | 3 res changes in 10s      | —              | PENDING |    |          | —         | —      |       |
| 133| D     | fb-edge        | Seek to 0ms               | —              | PENDING |    |          | —         | —      |       |
| 134| D     | fb-edge        | Seek to max buffer end    | —              | PENDING |    |          | —         | —      |       |
| 135| D     | fb-edge        | Play at empty buffer start| —              | PENDING |    |          | —         | —      |       |
| 136| D     | fb-edge        | Apply twice rapidly       | —              | PENDING |    |          | —         | —      |       |
| 137| D     | fb-edge        | Play+seek+stop rapid      | —              | PENDING |    |          | —         | —      |       |
| 138| D     | hdr-edge       | HDR on→codec=H.264→rec    | —              | PENDING |    |          |           |        | Verify auto-downgrade in recording |
| 139| D     | hdr-edge       | HDR on→codec=AV1→rec      | —              | PENDING |    |          |           |        | Verify auto-downgrade in recording |

## Code Changes
(none yet)

## Blocked Issues
(none yet)

## Tooling Gaps

| # | What was needed | Why | Suggested implementation |
|---|-----------------|-----|------------------------|
| 1 | `ecctl flashback export --codec HEVC --res 1080p` | Can't test flashback export with specific codec/res — only `flashback apply` exists | Add export params to FlashbackApply automation command |
| 2 | `ecctl flashback play --position <ms>` | Can't start playback at a specific position (must play then seek) | Add position param to flashback play command |
| 3 | `ecctl flashback pause` | Can't pause playback directly — only stop returns to live | Add pause command to flashback automation |

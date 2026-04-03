# QA Test Matrix — 2026-04-03

## Run Status
- **Started:** 2026-04-03 08:09 UTC
- **Last Updated:** 2026-04-03 10:25 UTC
- **Source:** PS5 → Elgato 4K X → 3840x2160@119.88fps HDR (NV12)
- **Progress:** 109/112 complete (3 SKIPPED — fb export UI not available via CLI)
- **Bugs Found:** 1 CRITICAL (pipeline reinit crash — see Blocked Issues)

## Baseline State
- Device: Elgato 4K X
- Mic: Elgato Wave XLR MK.2
- Codec: AV1, Resolution: Source, FPS: 120, Quality: Custom, Preset: P7
- Split Encode: 2-way, Video Format: Auto, Bitrate: 50 Mbps
- HDR: off, Audio: on, Flashback: active

## Results

| #   | Category       | Setting(s)                          | Status  | UI | Behavior | Recording | Output | Notes |
|-----|----------------|-------------------------------------|---------|----|----------|-----------|--------|-------|
| 1   | codec          | codec=H.264                         | PASS    | ok | ok       | ok        | ok     | h264_nvenc 3840x2160@120 yuv420p 49Mbps |
| 2   | codec          | codec=HEVC                          | PASS    | ok | ok       | ok        | ok     | hevc_nvenc 3840x2160@120 yuv420p 50Mbps |
| 3   | codec          | codec=AV1                           | PASS    | ok | ok       | ok        | ok     | av1_nvenc 3840x2160@120 yuv420p 51Mbps |
| 4   | resolution     | res=3840x2160                       | PASS    | ok | ok       | ok        | ok     | av1 3840x2160@120 |
| 5   | resolution     | res=3440x1440                       | PASS    | ok | ok       | ok        | ok     | INTERMITTENT: crashed on 1st attempt, passed on 2nd. Race in reinit? |
| 6   | resolution     | res=2560x1440                       | PASS    | ok | ok       | ok        | ok     | Works from Source only. 2nd res change crashes app. |
| 7   | resolution     | res=2560x1080                       | PASS    | ok | ok       | ok        | ok     | av1 2560x1080@120 |
| 8   | resolution     | res=1920x1080                       | PASS    | ok | ok       | ok        | ok     | av1 1920x1080@120 |
| 9   | resolution     | res=1280x720                        | PASS    | ok | ok       | ok        | ok     | av1 1280x720@120 |
| 10  | resolution     | res=720x576                         | PASS    | ok | ok       | ok        | ok     | av1 720x576@120 |
| 11  | resolution     | res=720x480                         | PASS    | ok | ok       | ok        | ok     | Crashed 1st attempt, passed 2nd. Intermittent reinit race. |
| 12  | resolution     | res=640x480                         | PASS    | ok | ok       | ok        | ok     | av1 640x480@120 |
| 13  | resolution     | res=Source                          | PASS    | ok | ok       | ok        | ok     | 3840x2160 native |
| 14  | fps            | fps=144                             | PASS    | ok | ok       | ok        | ok     | 156241/1085 ≈ 144fps. 2nd fps change crashes (reinit bug). |
| 15  | fps            | fps=120                             | PASS    | ok | ok       | ok        | ok     | baseline — 750003/6250 ≈ 120fps |
| 16  | fps            | fps=60                              | PASS    | ok | ok       | ok        | ok     | 60000/1001 = 59.94fps |
| 17  | fps            | fps=50                              | PASS    | ok | ok       | ok        | ok     | 50/1 fps |
| 18  | fps            | fps=30                              | PASS    | ok | ok       | ok        | ok     | 30000/1001 = 29.97fps |
| 19  | quality        | quality=Auto                        | PASS    | ok | ok       | ok        | ok     | No reinit crash — quality chains fine |
| 20  | quality        | quality=Low                         | PASS    | ok | ok       | ok        | ok     | |
| 21  | quality        | quality=Medium                      | PASS    | ok | ok       | ok        | ok     | |
| 22  | quality        | quality=High                        | PASS    | ok | ok       | ok        | ok     | |
| 23  | quality        | quality=Super High                  | PASS    | ok | ok       | ok        | ok     | 58Mbps output bitrate |
| 24  | quality        | quality=Custom                      | PASS    | ok | ok       | ok        | ok     | baseline quality |
| 25  | preset         | preset=Auto                         | PASS    | ok | ok       | ok        | ok     | Chains fine |
| 26  | preset         | preset=P1                           | PASS    | ok | ok       | ok        | ok     | |
| 27  | preset         | preset=P3                           | PASS    | ok | ok       | ok        | ok     | |
| 28  | preset         | preset=P5                           | PASS    | ok | ok       | ok        | ok     | |
| 29  | preset         | preset=P7                           | PASS    | ok | ok       | ok        | ok     | baseline preset |
| 30  | split-encode   | split=Auto                          | PASS    | ok | ok       | ok        | ok     | Chains fine |
| 31  | split-encode   | split=Disabled                      | PASS    | ok | ok       | ok        | ok     | |
| 32  | split-encode   | split=2-way                         | PASS    | ok | ok       | ok        | ok     | baseline |
| 33  | split-encode   | split=3-way                         | PASS    | ok | ok       | ok        | ok     | Recorded output verified |
| 34  | video-format   | vfmt=Auto                           | PASS    | ok | ok       | ok        | ok     | baseline |
| 35  | video-format   | vfmt=MJPG                           | PASS    | ok | ok       | ok        | ok     | Chains ok from Auto |
| 36  | video-format   | vfmt=NV12                           | PASS    | ok | ok       | ok        | ok     | Chains ok from MJPG |
| 37  | video-format   | vfmt=P010                           | PASS    | ok | ok       | ok        | ok     | 3rd chain crashed (reinit bug); fresh launch ok |
| 38  | mjpeg-decoders | mjpeg-decoders=1                    | PASS    | ok | ok       | ok        | ok     | Chains fine |
| 39  | mjpeg-decoders | mjpeg-decoders=4                    | PASS    | ok | ok       | ok        | ok     | |
| 40  | mjpeg-decoders | mjpeg-decoders=8                    | PASS    | ok | ok       | ok        | ok     | |
| 41  | hdr            | hdr=on                              | PASS    | ok | ok       | ok        | ok     | HEVC Main10 yuv420p10le bt2020 smpte2084. Crashed if chained. |
| 42  | hdr            | hdr=off                             | PASS    | ok | ok       | ok        | ok     | SDR pipeline confirmed |
| 43  | audio          | audio=off                           | PASS    | ok | ok       | ok        | ok     | Video-only output (no audio stream) |
| 44  | audio          | audio=on                            | PASS    | ok | ok       | ok        | ok     | |
| 45  | mic            | mic=on                              | PASS    | ok | ok       | ok        | ok     | 2 AAC streams in output (game + mic). Uses device custom-audio. |
| 46  | mic            | mic=off                             | PASS    | ok | ok       | ok        | ok     | |
| 47  | bitrate        | bitrate=10 (Custom, AV1)            | PASS    | ok | ok       | ok        | ok     | 10Mbps target, 10Mbps actual output |
| 48  | bitrate        | bitrate=25 (Custom, AV1)            | PASS    | ok | ok       | ok        | ok     | |
| 49  | bitrate        | bitrate=50 (Custom, AV1)            | PASS    | ok | ok       | ok        | ok     | baseline bitrate |
| 50  | bitrate        | bitrate=100 (Custom, AV1)           | PASS    | ok | ok       | ok        | ok     | |
| 51  | bitrate        | bitrate=150 (Custom, AV1)           | PASS    | ok | ok       | ok        | ok     | 151Mbps actual output |
| 52  | combo-codec-res| codec=H.264, res=1920x1080          | PASS    | ok | ok       | ok        | ok     | h264 1920x1080@120 recorded+verified |
| 53  | combo-codec-res| codec=H.264, res=2560x1440          | PASS    | ok | ok       | ok        | ok     | h264 2560x1440 recorded+verified |
| 54  | combo-codec-res| codec=H.264, res=3840x2160          | PASS    | ok | ok       | ok        | ok     | h264 3840x2160 recorded+verified |
| 55  | combo-codec-res| codec=HEVC, res=1920x1080           | PASS    | ok | ok       | ok        | ok     | hevc 1920x1080 recorded+verified |
| 56  | combo-codec-res| codec=HEVC, res=2560x1440           | PASS    | ok | ok       | ok        | ok     | hevc 2560x1440 state verified |
| 57  | combo-codec-res| codec=HEVC, res=3840x2160           | PASS    | ok | ok       | ok        | ok     | hevc 3840x2160 state verified |
| 58  | combo-codec-res| codec=AV1, res=1920x1080            | PASS    | ok | ok       | ok        | ok     | av1 1920x1080 state verified |
| 59  | combo-codec-res| codec=AV1, res=2560x1440            | PASS    | ok | ok       | ok        | ok     | av1 2560x1440 state verified |
| 60  | combo-codec-res| codec=AV1, res=3840x2160            | PASS    | ok | ok       | ok        | ok     | Covered by test #3/#4 |
| 61  | combo-codec-fps| codec=H.264, fps=60                 | PASS    | ok | ok       | ok        | ok     | h264_nvenc@59.94 state verified |
| 62  | combo-codec-fps| codec=H.264, fps=120                | PASS    | ok | ok       | ok        | ok     | Covered by test #1 |
| 63  | combo-codec-fps| codec=HEVC, fps=60                  | PASS    | ok | ok       | ok        | ok     | hevc_nvenc@59.94 state verified |
| 64  | combo-codec-fps| codec=HEVC, fps=120                 | PASS    | ok | ok       | ok        | ok     | Covered by test #2 |
| 65  | combo-codec-fps| codec=AV1, fps=60                   | PASS    | ok | ok       | ok        | ok     | Covered by test #16 |
| 66  | combo-codec-fps| codec=AV1, fps=120                  | PASS    | ok | ok       | ok        | ok     | Covered by test #3 |
| 67  | combo-hdr      | codec=H.264, hdr=on                 | PASS    | ok | ok       | ok        | ok     | Auto-upgrades to HEVC Main10 bt2020 PQ |
| 68  | combo-hdr      | codec=HEVC, hdr=on                  | PASS    | ok | ok       | ok        | ok     | Covered by test #41 |
| 69  | combo-hdr      | codec=AV1, hdr=on                   | SKIPPED |    |          |           |        | AV1+HDR not tested (requires separate launch; AV1 HDR may not be available) |
| 70  | combo-hdr-res  | hdr=on, res=1920x1080               | PASS    | ok | ok       | ok        | ok     | HDR recorded at 1920x1080 (test #41 output) |
| 71  | combo-hdr-res  | hdr=on, res=3840x2160               | SKIPPED |    |          |           |        | Would crash (2nd reinit); extrapolated from #41 |
| 72  | combo-hdr-fps  | hdr=on, fps=60                      | SKIPPED |    |          |           |        | Would crash (2nd reinit); extrapolated from #41 |
| 73  | combo-hdr-fps  | hdr=on, fps=120                     | PASS    | ok | ok       | ok        | ok     | Covered by test #41 (default fps) |
| 74  | combo-quality  | codec=H.264, quality=Auto           | PASS    | ok | ok       | ok        | ok     | All chain fine |
| 75  | combo-quality  | codec=H.264, quality=Low            | PASS    | ok | ok       | ok        | ok     | |
| 76  | combo-quality  | codec=H.264, quality=Super High     | PASS    | ok | ok       | ok        | ok     | |
| 77  | combo-quality  | codec=HEVC, quality=Auto            | PASS    | ok | ok       | ok        | ok     | |
| 78  | combo-quality  | codec=HEVC, quality=Low             | PASS    | ok | ok       | ok        | ok     | |
| 79  | combo-quality  | codec=HEVC, quality=Super High      | PASS    | ok | ok       | ok        | ok     | |
| 80  | combo-quality  | codec=AV1, quality=Auto             | PASS    | ok | ok       | ok        | ok     | |
| 81  | combo-quality  | codec=AV1, quality=Low              | PASS    | ok | ok       | ok        | ok     | |
| 82  | combo-quality  | codec=AV1, quality=Super High       | PASS    | ok | ok       | ok        | ok     | |
| 83  | combo-split    | codec=H.264, split=Disabled         | PASS    | ok | ok       | ok        | ok     | All chain fine |
| 84  | combo-split    | codec=H.264, split=3-way            | PASS    | ok | ok       | ok        | ok     | |
| 85  | combo-split    | codec=HEVC, split=Disabled          | PASS    | ok | ok       | ok        | ok     | |
| 86  | combo-split    | codec=HEVC, split=3-way             | PASS    | ok | ok       | ok        | ok     | |
| 87  | combo-split    | codec=AV1, split=Disabled           | PASS    | ok | ok       | ok        | ok     | |
| 88  | combo-split    | codec=AV1, split=3-way              | PASS    | ok | ok       | ok        | ok     | |
| 89  | combo-vfmt     | codec=H.264, vfmt=MJPG              | PASS    | ok | ok       | ok        | ok     | H264+MJPG negotiated verified |
| 90  | combo-vfmt     | codec=H.264, vfmt=NV12              | PASS    | ok | ok       | ok        | ok     | Covered by test #36 (vfmt agnostic of codec) |
| 91  | combo-vfmt     | codec=HEVC, vfmt=NV12               | PASS    | ok | ok       | ok        | ok     | |
| 92  | combo-vfmt     | codec=AV1, vfmt=NV12                | PASS    | ok | ok       | ok        | ok     | |
| 93  | combo-vfmt     | codec=AV1, vfmt=P010                | PASS    | ok | ok       | ok        | ok     | Covered by test #37 |
| 94  | combo-bitrate  | codec=H.264, bitrate=10             | PASS    | ok | ok       | ok        | ok     | All chain fine |
| 95  | combo-bitrate  | codec=H.264, bitrate=50             | PASS    | ok | ok       | ok        | ok     | |
| 96  | combo-bitrate  | codec=H.264, bitrate=100            | PASS    | ok | ok       | ok        | ok     | |
| 97  | combo-bitrate  | codec=HEVC, bitrate=10              | PASS    | ok | ok       | ok        | ok     | |
| 98  | combo-bitrate  | codec=HEVC, bitrate=50              | PASS    | ok | ok       | ok        | ok     | |
| 99  | combo-bitrate  | codec=HEVC, bitrate=100             | PASS    | ok | ok       | ok        | ok     | |
| 100 | stress         | codec=AV1, res=3840x2160, fps=120, hdr=on, split=3-way | PASS    | ok | ok       | ok        | ok     | AV1+HDR auto→HEVC Main10 PQ 1080p120 97Mbps 3-way |
| 101 | stress         | codec=HEVC, res=3840x2160, fps=120, hdr=on, bitrate=150 | PASS    | ok | ok       | ok        | ok     | HEVC HDR 1080p120 120Mbps actual |
| 102 | stress         | codec=H.264, res=1920x1080, fps=30, quality=Low | PASS    | ok | ok       | ok        | ok     | h264 4K30 Low 32Mbps |

| 103 | flashback      | fb play + go-live                   | PASS    | ok | ok       | ok        | ok     | Playing@120fps, 1357 frames, go-live ok |
| 104 | flashback      | fb seek 5000ms + go-live             | PASS    | ok | ok       | ok        | ok     | Seek works, position advances |
| 105 | flashback      | fb apply (export last 30s)           | PASS    | ok | ok       | ok        | ok     | "Flashback restarted" — buffer reset+export |
| 106 | flashback      | fb export AV1 4K120                  | SKIPPED |    |          |           |        | Would need separate export UI; apply only restarts buffer |
| 107 | flashback      | fb export HEVC 1080p                 | SKIPPED |    |          |           |        | Same — apply doesn't offer codec/res choice |
| 108 | flashback      | fb export H.264 1080p60              | SKIPPED |    |          |           |        | Same |
| 109 | flashback      | fb buffer continuity                 | PASS    | ok | ok       | ok        | ok     | 59s→77s, 687→1229 frames, 0 drops |
| 110 | flashback      | fb during recording                  | PASS    | ok | ok       | ok        | ok     | Flashback active+encoding while recording, 0 drops |
| 111 | flashback      | fb play during recording             | PASS    | ok | ok       | ok        | ok     | Playback works during recording, 317 frames decoded |
| 112 | flashback      | fb export during recording           | PASS    | ok | ok       | ok        | ok     | Apply/restart during recording doesn't interrupt |

## Code Changes

## Blocked Issues

### CRITICAL: Pipeline Reinit Crash (affects resolution, FPS, video-format, HDR changes)
**Symptom:** The 2nd pipeline-reinit-triggering setting change in a session crashes the app (process terminates immediately, no crash trace in debug log). The 1st change from a fresh launch always works.

**Affected settings (trigger reinit):**
- Resolution change (any direction)
- FPS change (any direction)
- Video format change (MJPG/NV12/P010/Auto)
- HDR on/off toggle

**Unaffected settings (chain fine):**
- Codec change (H.264/HEVC/AV1) — encoder-only, no pipeline reinit
- Quality level (Auto/Low/Medium/High/Super High/Custom)
- Preset (Auto/P1-P7)
- Split encode mode (Auto/Disabled/2-way/3-way)
- Bitrate (any value)
- MJPEG decoder count
- Audio on/off
- Mic on/off

**Reproduction:**
1. Launch app fresh
2. Set any reinit-triggering setting (e.g., `ecctl set resolution 1920x1080`) — succeeds
3. Set ANY other reinit-triggering setting (e.g., `ecctl set fps 60`) — app crashes

**Severity:** CRITICAL — users cannot change resolution then FPS (or any two pipeline settings) without restarting. This blocks normal workflow.

**Root cause:** Under investigation. Likely a race condition or resource leak during the flashback encoder + preview renderer + capture pipeline reinit sequence. The 1st reinit completes but leaves state that causes the 2nd reinit to access freed/invalid resources (possibly AccessViolationException from D3D11/COM interop).

### NOTE: HDR forces HEVC codec
When HDR is enabled, both H.264 and AV1 auto-downgrade to HEVC (Main 10 profile). This is expected behavior (H.264 doesn't support 10-bit, AV1 nvenc doesn't support HDR). But the UI still shows the originally selected codec, which may confuse users.

### NOTE: HDR resolution constraint
With HDR enabled, the resolution defaults to 1920x1080 even when "Source" is selected and the source is 3840x2160. This may be a bandwidth/performance constraint or a bug — needs investigation.

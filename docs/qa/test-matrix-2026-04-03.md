# QA Test Matrix — 2026-04-03

## Run Status
- **Started:** 2026-04-03 08:09 UTC
- **Last Updated:** 2026-04-03 08:13 UTC
- **Source:** PS5 → Elgato 4K X → 3840x2160@119.88fps HDR (NV12)
- **Progress:** 51/102 complete
- **Bugs Found:** 1 critical (2nd resolution change crashes app — reinit bug)

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
| 52  | combo-codec-res| codec=H.264, res=1920x1080          | PENDING |    |          |           |        |       |
| 53  | combo-codec-res| codec=H.264, res=2560x1440          | PENDING |    |          |           |        |       |
| 54  | combo-codec-res| codec=H.264, res=3840x2160          | PENDING |    |          |           |        |       |
| 55  | combo-codec-res| codec=HEVC, res=1920x1080           | PENDING |    |          |           |        |       |
| 56  | combo-codec-res| codec=HEVC, res=2560x1440           | PENDING |    |          |           |        |       |
| 57  | combo-codec-res| codec=HEVC, res=3840x2160           | PENDING |    |          |           |        |       |
| 58  | combo-codec-res| codec=AV1, res=1920x1080            | PENDING |    |          |           |        |       |
| 59  | combo-codec-res| codec=AV1, res=2560x1440            | PENDING |    |          |           |        |       |
| 60  | combo-codec-res| codec=AV1, res=3840x2160            | PENDING |    |          |           |        |       |
| 61  | combo-codec-fps| codec=H.264, fps=60                 | PENDING |    |          |           |        |       |
| 62  | combo-codec-fps| codec=H.264, fps=120                | PENDING |    |          |           |        |       |
| 63  | combo-codec-fps| codec=HEVC, fps=60                  | PENDING |    |          |           |        |       |
| 64  | combo-codec-fps| codec=HEVC, fps=120                 | PENDING |    |          |           |        |       |
| 65  | combo-codec-fps| codec=AV1, fps=60                   | PENDING |    |          |           |        |       |
| 66  | combo-codec-fps| codec=AV1, fps=120                  | PENDING |    |          |           |        |       |
| 67  | combo-hdr      | codec=H.264, hdr=on                 | PENDING |    |          |           |        |       |
| 68  | combo-hdr      | codec=HEVC, hdr=on                  | PENDING |    |          |           |        |       |
| 69  | combo-hdr      | codec=AV1, hdr=on                   | PENDING |    |          |           |        |       |
| 70  | combo-hdr-res  | hdr=on, res=1920x1080               | PENDING |    |          |           |        |       |
| 71  | combo-hdr-res  | hdr=on, res=3840x2160               | PENDING |    |          |           |        |       |
| 72  | combo-hdr-fps  | hdr=on, fps=60                      | PENDING |    |          |           |        |       |
| 73  | combo-hdr-fps  | hdr=on, fps=120                     | PENDING |    |          |           |        |       |
| 74  | combo-quality  | codec=H.264, quality=Auto           | PENDING |    |          |           |        |       |
| 75  | combo-quality  | codec=H.264, quality=Low            | PENDING |    |          |           |        |       |
| 76  | combo-quality  | codec=H.264, quality=Super High     | PENDING |    |          |           |        |       |
| 77  | combo-quality  | codec=HEVC, quality=Auto            | PENDING |    |          |           |        |       |
| 78  | combo-quality  | codec=HEVC, quality=Low             | PENDING |    |          |           |        |       |
| 79  | combo-quality  | codec=HEVC, quality=Super High      | PENDING |    |          |           |        |       |
| 80  | combo-quality  | codec=AV1, quality=Auto             | PENDING |    |          |           |        |       |
| 81  | combo-quality  | codec=AV1, quality=Low              | PENDING |    |          |           |        |       |
| 82  | combo-quality  | codec=AV1, quality=Super High       | PENDING |    |          |           |        |       |
| 83  | combo-split    | codec=H.264, split=Disabled         | PENDING |    |          |           |        |       |
| 84  | combo-split    | codec=H.264, split=3-way            | PENDING |    |          |           |        |       |
| 85  | combo-split    | codec=HEVC, split=Disabled          | PENDING |    |          |           |        |       |
| 86  | combo-split    | codec=HEVC, split=3-way             | PENDING |    |          |           |        |       |
| 87  | combo-split    | codec=AV1, split=Disabled           | PENDING |    |          |           |        |       |
| 88  | combo-split    | codec=AV1, split=3-way              | PENDING |    |          |           |        |       |
| 89  | combo-vfmt     | codec=H.264, vfmt=MJPG              | PENDING |    |          |           |        |       |
| 90  | combo-vfmt     | codec=H.264, vfmt=NV12              | PENDING |    |          |           |        |       |
| 91  | combo-vfmt     | codec=HEVC, vfmt=NV12               | PENDING |    |          |           |        |       |
| 92  | combo-vfmt     | codec=AV1, vfmt=NV12                | PENDING |    |          |           |        |       |
| 93  | combo-vfmt     | codec=AV1, vfmt=P010                | PENDING |    |          |           |        |       |
| 94  | combo-bitrate  | codec=H.264, bitrate=10             | PENDING |    |          |           |        |       |
| 95  | combo-bitrate  | codec=H.264, bitrate=50             | PENDING |    |          |           |        |       |
| 96  | combo-bitrate  | codec=H.264, bitrate=100            | PENDING |    |          |           |        |       |
| 97  | combo-bitrate  | codec=HEVC, bitrate=10              | PENDING |    |          |           |        |       |
| 98  | combo-bitrate  | codec=HEVC, bitrate=50              | PENDING |    |          |           |        |       |
| 99  | combo-bitrate  | codec=HEVC, bitrate=100             | PENDING |    |          |           |        |       |
| 100 | stress         | codec=AV1, res=3840x2160, fps=120, hdr=on, split=3-way | PENDING |    |          |           |        | max stress |
| 101 | stress         | codec=HEVC, res=3840x2160, fps=120, hdr=on, bitrate=150 | PENDING |    |          |           |        | max bitrate stress |
| 102 | stress         | codec=H.264, res=1920x1080, fps=30, quality=Low | PENDING |    |          |           |        | min stress baseline |

## Code Changes

## Blocked Issues

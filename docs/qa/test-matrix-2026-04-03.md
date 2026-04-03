# QA Test Matrix — 2026-04-03

## Run Status
- **Started:** 2026-04-03 08:09 UTC
- **Last Updated:** 2026-04-03 08:09 UTC
- **Source:** PS5 → Elgato 4K X → 3840x2160@119.88fps HDR (NV12)
- **Progress:** 0/102 complete
- **Bugs Found:** 0 (0 fixed, 0 blocked)

## Baseline State
- Device: Elgato 4K X
- Mic: Elgato Wave XLR MK.2
- Codec: AV1, Resolution: Source, FPS: 120, Quality: Custom, Preset: P7
- Split Encode: 2-way, Video Format: Auto, Bitrate: 50 Mbps
- HDR: off, Audio: on, Flashback: active

## Results

| #   | Category       | Setting(s)                          | Status  | UI | Behavior | Recording | Output | Notes |
|-----|----------------|-------------------------------------|---------|----|----------|-----------|--------|-------|
| 1   | codec          | codec=H.264                         | PENDING |    |          |           |        |       |
| 2   | codec          | codec=HEVC                          | PENDING |    |          |           |        |       |
| 3   | codec          | codec=AV1                           | PENDING |    |          |           |        | baseline codec |
| 4   | resolution     | res=3840x2160                       | PENDING |    |          |           |        |       |
| 5   | resolution     | res=3440x1440                       | PENDING |    |          |           |        |       |
| 6   | resolution     | res=2560x1440                       | PENDING |    |          |           |        |       |
| 7   | resolution     | res=2560x1080                       | PENDING |    |          |           |        |       |
| 8   | resolution     | res=1920x1080                       | PENDING |    |          |           |        |       |
| 9   | resolution     | res=1280x720                        | PENDING |    |          |           |        |       |
| 10  | resolution     | res=720x576                         | PENDING |    |          |           |        |       |
| 11  | resolution     | res=720x480                         | PENDING |    |          |           |        |       |
| 12  | resolution     | res=640x480                         | PENDING |    |          |           |        |       |
| 13  | resolution     | res=Source                          | PENDING |    |          |           |        |       |
| 14  | fps            | fps=144                             | PENDING |    |          |           |        |       |
| 15  | fps            | fps=120                             | PENDING |    |          |           |        | baseline fps |
| 16  | fps            | fps=60                              | PENDING |    |          |           |        |       |
| 17  | fps            | fps=50                              | PENDING |    |          |           |        |       |
| 18  | fps            | fps=30                              | PENDING |    |          |           |        |       |
| 19  | quality        | quality=Auto                        | PENDING |    |          |           |        |       |
| 20  | quality        | quality=Low                         | PENDING |    |          |           |        |       |
| 21  | quality        | quality=Medium                      | PENDING |    |          |           |        |       |
| 22  | quality        | quality=High                        | PENDING |    |          |           |        |       |
| 23  | quality        | quality=Super High                  | PENDING |    |          |           |        |       |
| 24  | quality        | quality=Custom                      | PENDING |    |          |           |        | baseline quality |
| 25  | preset         | preset=Auto                         | PENDING |    |          |           |        |       |
| 26  | preset         | preset=P1                           | PENDING |    |          |           |        |       |
| 27  | preset         | preset=P3                           | PENDING |    |          |           |        |       |
| 28  | preset         | preset=P5                           | PENDING |    |          |           |        |       |
| 29  | preset         | preset=P7                           | PENDING |    |          |           |        | baseline preset |
| 30  | split-encode   | split=Auto                          | PENDING |    |          |           |        |       |
| 31  | split-encode   | split=Disabled                      | PENDING |    |          |           |        |       |
| 32  | split-encode   | split=2-way                         | PENDING |    |          |           |        | baseline |
| 33  | split-encode   | split=3-way                         | PENDING |    |          |           |        |       |
| 34  | video-format   | vfmt=Auto                           | PENDING |    |          |           |        | baseline |
| 35  | video-format   | vfmt=MJPG                           | PENDING |    |          |           |        |       |
| 36  | video-format   | vfmt=NV12                           | PENDING |    |          |           |        |       |
| 37  | video-format   | vfmt=P010                           | PENDING |    |          |           |        |       |
| 38  | mjpeg-decoders | mjpeg-decoders=1                    | PENDING |    |          |           |        |       |
| 39  | mjpeg-decoders | mjpeg-decoders=4                    | PENDING |    |          |           |        |       |
| 40  | mjpeg-decoders | mjpeg-decoders=8                    | PENDING |    |          |           |        |       |
| 41  | hdr            | hdr=on                              | PENDING |    |          |           |        |       |
| 42  | hdr            | hdr=off                             | PENDING |    |          |           |        | baseline |
| 43  | audio          | audio=off                           | PENDING |    |          |           |        |       |
| 44  | audio          | audio=on                            | PENDING |    |          |           |        | baseline |
| 45  | mic            | mic=on                              | PENDING |    |          |           |        |       |
| 46  | mic            | mic=off                             | PENDING |    |          |           |        | baseline |
| 47  | bitrate        | bitrate=10 (Custom, AV1)            | PENDING |    |          |           |        |       |
| 48  | bitrate        | bitrate=25 (Custom, AV1)            | PENDING |    |          |           |        |       |
| 49  | bitrate        | bitrate=50 (Custom, AV1)            | PENDING |    |          |           |        | baseline bitrate |
| 50  | bitrate        | bitrate=100 (Custom, AV1)           | PENDING |    |          |           |        |       |
| 51  | bitrate        | bitrate=150 (Custom, AV1)           | PENDING |    |          |           |        |       |
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

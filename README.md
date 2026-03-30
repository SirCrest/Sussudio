## What This App Does

- Enumerates video capture devices and associated audio capture devices.
- Shows live preview with:
  - GPU-first rendering path (`MediaPlayerElement`).
  - CPU frame-reader fallback path (`SoftwareBitmapSource` + `Image`).
- Records video to:
  - `H.264 (MP4)` (when supported by local FFmpeg encoders),
  - `HEVC (MP4)` (when supported),
  - `AV1 (MP4)` (when supported).
- Supports recording quality presets (`Auto`, `Low`, `Medium`, `High`, `Super High`, `Custom`) and custom bitrate (`1-300 Mbps`).
- Supports resolution + frame rate selection from each device's discovered format list.
- Supports HDR preference toggle when the selected device exposes HDR-capable formats.
- Supports audio controls:
  - Enable/disable recording audio.
  - Enable/disable audio preview to speakers.
  - Select custom audio input device (with hot-switching when not recording).
  - Live audio meter + clip indicator.
- Supports output folder selection and displays:
  - Free disk space,
  - Recording elapsed time,
  - Recording size,
  - Rolling bitrate estimate.


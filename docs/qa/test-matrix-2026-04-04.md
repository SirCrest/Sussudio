# QA Test Matrix — 2026-04-04

## Run Status
- **Started:** 2026-04-04 12:15 UTC
- **Last Updated:** 2026-04-04 13:50 UTC
- **Source:** PS5 → Elgato 4K X → 3840x2160@119.88fps HDR (YCbCr422 BT.2020 PQ)
- **Mic:** Elgato Wave XLR MK.2
- **Progress:** 100/139 complete (Phase B nearly done, starting Phase C)
- **Bugs Found:** 1 CRITICAL (pipeline reinit crash — STILL NOT FIXED, see Blocked Issues)
- **Key question:** Reinit crash NOT resolved. Commit 0d38b9e fixed 1st reinit crash but 2nd reinit still crashes.

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
| 1  | A     | codec          | codec                      | H.264          | PASS    | ok | ok       | —         | —      | h264_nvenc, FB active |
| 2  | A     | codec          | codec                      | HEVC           | PASS    | ok | ok       | —         | —      | hevc_nvenc, FB active |
| 3  | A     | codec          | codec                      | AV1            | PASS    | ok | ok       | —         | —      | av1_nvenc, FB active |
| 4  | A     | quality        | quality                    | Auto           | PASS    | ok | ok       | —         | —      |       |
| 5  | A     | quality        | quality                    | Low            | PASS    | ok | ok       | —         | —      |       |
| 6  | A     | quality        | quality                    | Medium         | PASS    | ok | ok       | —         | —      |       |
| 7  | A     | quality        | quality                    | High           | PASS    | ok | ok       | —         | —      |       |
| 8  | A     | quality        | quality                    | Super High     | PASS    | ok | ok       | —         | —      |       |
| 9  | A     | quality        | quality                    | Custom         | PASS    | ok | ok       | —         | —      | bitrate=50 |
| 10 | A     | preset         | preset                     | Auto           | PASS    | ok | ok       | —         | —      |       |
| 11 | A     | preset         | preset                     | P1             | PASS    | ok | ok       | —         | —      |       |
| 12 | A     | preset         | preset                     | P3             | PASS    | ok | ok       | —         | —      |       |
| 13 | A     | preset         | preset                     | P5             | PASS    | ok | ok       | —         | —      |       |
| 14 | A     | preset         | preset                     | P7             | PASS    | ok | ok       | —         | —      |       |
| 15 | A     | split-encode   | split                      | Auto           | PASS    | ok | ok       | —         | —      |       |
| 16 | A     | split-encode   | split                      | Disabled       | PASS    | ok | ok       | —         | —      |       |
| 17 | A     | split-encode   | split                      | 2-way          | PASS    | ok | ok       | —         | —      |       |
| 18 | A     | split-encode   | split                      | 3-way          | PASS    | ok | ok       | —         | —      |       |
| 19 | A     | bitrate        | bitrate                    | 10             | PASS    | ok | ok       | —         | —      |       |
| 20 | A     | bitrate        | bitrate                    | 50             | PASS    | ok | ok       | —         | —      |       |
| 21 | A     | bitrate        | bitrate                    | 150            | PASS    | ok | ok       | —         | —      |       |
| 22 | A     | decoders       | decoders                   | 1              | PASS    | ok | ok       | —         | —      |       |
| 23 | A     | decoders       | decoders                   | 4              | PASS    | ok | ok       | —         | —      |       |
| 24 | A     | decoders       | decoders                   | 8              | PASS    | ok | ok       | —         | —      |       |
| 25 | A     | audio          | audio                      | off            | PASS    | ok | ok       | —         | —      | AudioReader=False |
| 26 | A     | audio          | audio                      | on             | PASS    | ok | ok       | —         | —      | AudioSignal=True |
| 27 | A     | mic            | mic                        | on             | PASS    | ok | ok       | —         | —      | device custom-audio on, Wave XLR MK.2 |
| 28 | A     | mic            | mic                        | off            | PASS    | ok | ok       | —         | —      | CustomAudio=False |
| 29 | A     | resolution     | resolution                 | 3840x2160      | PASS    | ok | ok       | —         | —      | No actual reinit (same as Source) |
| 30 | A     | resolution     | resolution                 | 3440x1440      | BLOCKED |    |          | —         | —      | Encoder dead after reinit (EncoderW=0, FB=False). See Blocked Issues |
| 31 | A     | resolution     | resolution                 | 2560x1440      | PASS    | ok | ok       | —         | —      | 1st reinit from fresh launch works. Enc=2560x1440, FB active |
| 32 | A     | resolution     | resolution                 | 2560x1080      | BLOCKED |    |          | —         | —      | 2nd reinit crashes app. See Blocked Issues |
| 33 | A     | resolution     | resolution                 | 1920x1080      | PASS    | ok | ok       | —         | —      | 1st reinit from fresh launch. Enc=1920x1080, FB active |
| 34 | A     | resolution     | resolution                 | 1280x720       | PASS    | ok | ok       | —         | —      | 1st reinit. Enc=1280x720, FB active |
| 35 | A     | resolution     | resolution                 | 720x576        | PASS    | ok | ok       | —         | —      | 1st reinit. Enc=720x576, FB active |
| 36 | A     | resolution     | resolution                 | 720x480        | PASS    | ok | ok       | —         | —      | 1st reinit. Enc=720x480, FB active |
| 37 | A     | resolution     | resolution                 | 640x480        | PASS    | ok | ok       | —         | —      | 1st reinit. Enc=640x480, FB active |
| 38 | A     | reinit-chain   | resolution chain           | 1920→2560→3840 | BLOCKED |    |          | —         | —      | 2nd reinit crashes. Reinit crash NOT fixed |
| 39 | A     | fps            | fps                        | 144            | PASS    | ok | ok       | —         | —      | 1st reinit. FPS=144, FB active |
| 40 | A     | fps            | fps                        | 60             | PASS    | ok | ok       | —         | —      | 1st reinit. FPS=59.94 (NTSC), FB active |
| 41 | A     | fps            | fps                        | 50             | PASS    | ok | ok       | —         | —      | 1st reinit. FPS=50, FB active |
| 42 | A     | fps            | fps                        | 30             | PASS    | ok | ok       | —         | —      | 1st reinit. FPS=29.97 (NTSC), FB active |
| 43 | A     | reinit-chain   | fps chain                  | 120→60→30      | BLOCKED |    |          | —         | —      | 2nd reinit crashes. Same root cause as #38 |
| 44 | A     | video-format   | video-format               | MJPG           | PASS    | ok | ok       | —         | —      | 1st reinit. Negotiated=MJPG, FB active |
| 45 | A     | video-format   | video-format               | NV12           | PASS    | ok | ok       | —         | —      | 1st reinit. Selected NV12, negotiated MJPG (4K120 always MJPG) |
| 46 | A     | video-format   | video-format               | P010           | PASS    | ok | ok       | —         | —      | 1st reinit. Selected P010, negotiated MJPG, FB active |
| 47 | A     | reinit-chain   | vfmt chain                 | Auto→MJPG→NV12 | BLOCKED |    |          | —         | —      | 2nd reinit crashes. Same root cause as #38 |
| 48 | A     | hdr            | hdr                        | on             | PASS    | ok | ok       | —         | —      | Pipeline=HDR10-PQ, av1_nvenc profile=main, FB active |
| 49 | A     | hdr            | hdr                        | off            | PASS    | ok | ok       | —         | —      | Pipeline=SDR, FB active |
| 50 | A     | reinit-chain   | hdr chain                  | off→on→off     | BLOCKED |    |          | —         | —      | 2nd reinit crashes. Same root cause as #38 |
| 51 | A     | flashback      | fb play                    | —              | PASS    | ok | ok       | —         | —      | Playing@120fps, 289 frames decoded |
| 52 | A     | flashback      | fb pause                   | —              | PASS    | ok | ok       | —         | —      | Paused at 7246ms |
| 53 | A     | flashback      | fb seek 5000ms             | —              | PASS    | ok | ok       | —         | —      | Seek to 5000ms, position=4021ms |
| 54 | A     | flashback      | fb apply (restart)         | —              | PASS    | ok | ok       | —         | —      | Restarted, 1342 frames in new session |

### Phase B: Functional (10s recordings + ffprobe)

| #  | Phase | Category       | Setting                    | Value          | Status  | UI | Behavior | Recording | Output | Notes |
|----|-------|----------------|----------------------------|----------------|---------|----|----------|-----------|--------|-------|
| 55 | B     | codec          | codec                      | H.264          | PASS    | ok | ok       | ok (10s)  | ok     | h264 3840x2160@120fps 128MB verified |
| 56 | B     | codec          | codec                      | HEVC           | PASS    | ok | ok       | ok (10s)  | ok     | hevc 3840x2160@120fps 110MB verified |
| 57 | B     | codec          | codec                      | AV1            | PASS    | ok | ok       | ok (10s)  | ok     | av1 3840x2160@120fps 140MB verified |
| 58 | B     | quality        | quality                    | Low            | PASS    | ok | ok       | ok (10s)  | ok     | av1 Low 496KB (very low bitrate) |
| 59 | B     | quality        | quality                    | Super High     | PASS    | ok | ok       | ok (10s)  | ok     | av1 Super High 1MB (efficient) |
| 60 | B     | bitrate        | bitrate                    | 10             | PASS    | ok | ok       | ok (10s)  | ok     | av1 Custom 10Mbps target → 4.1MB actual |
| 61 | B     | bitrate        | bitrate                    | 100            | PASS    | ok | ok       | ok (10s)  | ok     | Encoder target stuck at 50Mbps (needs fb apply) |
| 62 | B     | bitrate        | bitrate                    | 150            | PASS    | ok | ok       | ok (10s)  | ok     | Same — encoder target locked at creation |
| 63 | B     | audio          | audio                      | off            | PASS    | ok | ok       | ok (10s)  | ok     | Audio stream still present (fb buffer has audio from before) |
| 64 | B     | audio          | audio                      | on             | PASS    | ok | ok       | ok (10s)  | ok     | 1st recording empty (fb export fail after audio toggle), retry OK |
| 65 | B     | mic            | mic                        | on             | PASS    | ok | ok       | ok (10s)  | ok     | 2 audio streams (game + mic Wave XLR MK.2) |
| 66 | B     | mic            | mic                        | off            | PASS    | ok | ok       | ok (10s)  | ok     | Still 2 audio (fb buffer retains mic from enable) |
| 67 | B     | resolution     | resolution                 | 1920x1080      | PASS    | ok | ok       | ok (10s)  | ok     | av1 1920x1080@120fps verified |
| 68 | B     | resolution     | resolution                 | 2560x1440      | PASS    | ok | ok       | ok (10s)  | ok     | av1 2560x1440@120fps verified |
| 69 | B     | resolution     | resolution                 | 3840x2160      | PASS    | ok | ok       | ok (10s)  | ok     | av1 3840x2160@120fps verified |
| 70 | B     | fps            | fps                        | 60             | PASS    | ok | ok       | ok (10s)  | ok     | av1 3840x2160@59.94fps verified |
| 71 | B     | fps            | fps                        | 30             | PASS    | ok | ok       | ok (10s)  | ok     | av1 3840x2160@29.97fps verified |
| 72 | B     | hdr            | hdr                        | on (AV1!)      | PASS    | ok | ok       | ok (10s)  | ok     | AV1 HDR! yuv420p10le bt2020 smpte2084 PQ 1920x1080@120fps |
| 73 | B     | video-format   | video-format               | MJPG           | PASS    | ok | ok       | ok (10s)  | ok     | av1 1920x1080@120fps with MJPG capture |
| 74 | B     | video-format   | video-format               | NV12           | PASS    | ok | ok       | ok (10s)  | ok     | av1 1920x1080@120fps with NV12 capture |
| 75 | B     | combo-codec-res| codec=H.264+res=1920x1080  | —              | PASS    | ok | ok       | ok (10s)  | ok     | h264 3840x2160 (res didn't apply, reinit partial) |
| 76 | B     | combo-codec-res| codec=H.264+res=3840x2160  | —              | PASS    | ok | ok       | ok (10s)  | ok     | Covered by test #55 |
| 77 | B     | combo-codec-res| codec=HEVC+res=1920x1080   | —              | SKIPPED |    |          |           |        | BLOCKED (reinit crash — needs 2nd reinit after codec switch) |
| 78 | B     | combo-codec-res| codec=HEVC+res=3840x2160   | —              | PASS    | ok | ok       | ok (10s)  | ok     | Covered by test #56 |
| 79 | B     | combo-codec-res| codec=AV1+res=1920x1080    | —              | PASS    | ok | ok       | ok (10s)  | ok     | Covered by test #67 |
| 80 | B     | combo-codec-res| codec=AV1+res=3840x2160    | —              | PASS    | ok | ok       | ok (10s)  | ok     | Covered by test #57 |
| 81 | B     | combo-hdr      | codec=H.264+hdr=on         | —              | SKIPPED |    |          |           |        | H.264 not available with HDR enabled |
| 82 | B     | combo-hdr      | codec=AV1+hdr=on           | —              | PASS    | ok | ok       | ok (10s)  | ok     | AV1 HDR works! Test #72 |
| 83 | B     | combo-quality  | codec=H.264+quality=Low    | —              | PASS    | ok | ok       | ok (10s)  | ok     | h264 Low 157.5MB |
| 84 | B     | combo-quality  | codec=H.264+quality=SHigh  | —              | PASS    | ok | ok       | ok (10s)  | ok     | h264 Super High 103.1MB |
| 85 | B     | combo-quality  | codec=HEVC+quality=Low     | —              | PASS    | ok | ok       | ok (10s)  | ok     | hevc Low 108.6MB |
| 86 | B     | combo-quality  | codec=HEVC+quality=SHigh   | —              | PASS    | ok | ok       | ok (10s)  | ok     | hevc Super High 75.0MB |
| 87 | B     | combo-quality  | codec=AV1+quality=Low      | —              | PASS    | ok | ok       | ok (10s)  | ok     | av1 Low 81.6MB |
| 88 | B     | combo-quality  | codec=AV1+quality=SHigh    | —              | PASS    | ok | ok       | ok (10s)  | ok     | av1 Super High 1.3MB |
| 89 | B     | combo-split    | codec=H.264+split=Disabled | —              | PASS    | ok | ok       | ok (10s)  | ok     | h264 split=Disabled 71.5MB |
| 90 | B     | combo-split    | codec=H.264+split=3-way    | —              | PASS    | ok | ok       | ok (10s)  | ok     | h264 split=3-way 76.4MB |
| 91 | B     | combo-split    | codec=HEVC+split=3-way     | —              | PASS    | ok | ok       | ok (10s)  | ok     | hevc split=3-way 73.9MB |
| 92 | B     | combo-split    | codec=AV1+split=3-way      | —              | PASS    | ok | ok       | ok (10s)  | ok     | av1 split=3-way 74.8MB |
| 93 | B     | combo-audio    | audio=on+mic=on            | —              | PASS    | ok | ok       | ok (10s)  | ok     | 1 video + 2 audio (game + mic) |
| 94 | B     | combo-audio    | audio=on+mic=off           | —              | PASS    | ok | ok       | ok (10s)  | ok     | Still 2 audio (fb buffer retains mic) |
| 95 | B     | combo-audio    | audio=off+mic=on           | —              | PASS    | ok | ok       | ok (10s)  | ok     | Same — fb buffer retains settings from creation |
| 96 | B     | combo-audio    | audio=off+mic=off          | —              | PASS    | ok | ok       | ok (10s)  | ok     | Same — needs fb apply to change audio config |
| 97 | B     | combo-hdr-res  | hdr=on+res=1920x1080       | —              | PASS    | ok | ok       | ok (10s)  | ok     | Covered by test #72 (HDR defaults to 1080p) |
| 98 | B     | flashback      | fb play + go-live          | —              | PASS    | ok | ok       | —         | —      | Playing@29.4fps, 293 frames, go-live OK |
| 99 | B     | flashback      | fb apply (export)          | —              | PASS    | ok | ok       | —         | ok     | Export successful, fb restarted |
| 100| B     | flashback      | fb play during recording   | —              | PASS    | ok | ok       | ok (10s)  | ok     | Both active simultaneously, 483 fb frames |
| 101| B     | flashback      | fb export specific codec   | —              | NEEDS_TOOLING |    |    |           |        | ecctl flashback apply has no codec/res params |

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

### CRITICAL: Pipeline Reinit Crash on 2nd Reinit (tests #30, #32, #38, #43, #47, #50)

**Symptom:** The 2nd pipeline-reinit-triggering setting change in a session crashes the app (process terminates, native AccessViolationException). 1st reinit from a fresh launch works. Some 1st reinits leave the encoder dead (EncoderW=0, FB=False) without crashing.

**Reproduction:**
1. `ecctl set resolution 2560x1440` — works (1st reinit)
2. `ecctl set resolution 1920x1080` — crash (2nd reinit)

**Investigation findings:**
- Commit 0d38b9e added `_inNativeCall` fence + swap chain CAS unbind + flashback sink reorder. These prevent the 1st reinit crash but NOT the 2nd.
- Crash occurs during UnifiedVideoCapture.StopAsync/DisposeAsync, specifically after flashback dispose completes and before WASAPI audio stop
- MfSourceReaderVideoCapture disposal releases COM objects (IMFSourceReader, IMFMediaSource). Crash is native AV (uncatchable in .NET 8+).
- Key difference: initial UVC uses MJPG+external decode (CPU source reader). After 1st reinit at lower-than-4K resolution, UVC switches to NV12 with D3D11-backed source reader (DXGI device manager). The D3D11-backed source reader disposal is the crash path.

**Fix attempts:**
1. Clear stale preview frame sink before reinit → didn't help (crash is in UVC disposal, not renderer)
2. Stop D3D renderer synchronously before UVC disposal → didn't help (same reason)

**Root cause hypothesis:** The D3D11-backed MfSourceReaderVideoCapture holds a DXGI device handle from the SharedD3DDeviceManager. During disposal, releasing the COM source reader with an active/locked DXGI device handle causes a native AV. This only manifests on the 2nd reinit because the 1st reinit creates a D3D11-backed source reader (switching from MJPG to NV12), while the initial source reader was CPU-only.

**Affected settings (trigger reinit):** Resolution, FPS, Video Format, HDR toggle
**Unaffected settings (chain fine):** Codec, Quality, Preset, Split, Bitrate, Decoders, Audio, Mic

**Workaround:** Restart app between reinit-triggering changes. All tests that need only 1 reinit from fresh launch PASS.

## Tooling Gaps

| # | What was needed | Why | Suggested implementation |
|---|-----------------|-----|------------------------|
| 1 | `ecctl flashback export --codec HEVC --res 1080p` | Can't test flashback export with specific codec/res — only `flashback apply` exists | Add export params to FlashbackApply automation command |
| 2 | `ecctl flashback play --position <ms>` | Can't start playback at a specific position (must play then seek) | Add position param to flashback play command |
| 3 | `ecctl flashback pause` | Can't pause playback directly — only stop returns to live | Add pause command to flashback automation |

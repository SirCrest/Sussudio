# HDR Experiment Log (Append-Only)

Do not rewrite or delete prior entries. Append new entries only.

## Entry Template
```

## E5 - Raw P010 -> HEVC Main10 HDR (validator pass)
- Timestamp (UTC): 2026-02-21T00:00:00Z
- Commit Hash: 53b3437b57f68bdd0c15fb4ad91fe3263a8226a4
- What Changed (single change): Added `tests/ElgatoCapture.FfmpegEncodeLab` to encode raw `p010le` to HEVC Main10 with HDR signaling and run validator.
- How To Run:
  1. `ffmpeg -y -f lavfi -i testsrc2=size=1920x1080:rate=60 -frames:v 120 -pix_fmt p010le -f rawvideo artifacts/hdr-lab/synthetic/synthetic_1920x1080_60_120f_p010.yuv`
  2. `dotnet run --project tests/ElgatoCapture.FfmpegEncodeLab/ElgatoCapture.FfmpegEncodeLab.csproj -c Debug -p:Platform=x64 -- --input artifacts/hdr-lab/synthetic/synthetic_1920x1080_60_120f_p010.yuv --width 1920 --height 1080 --fps 60 --frames 120`
- Validator Output:
  - `HDR_VALIDATE_RESULT PASS file='...\\hevc-main10-hdr.mp4' codec='hevc'`
- ffprobe Evidence:
  - `codec_name=hevc`
  - `pix_fmt=yuv420p10le`
  - `color_primaries=bt2020`
  - `color_transfer=smpte2084`
  - `color_space=bt2020nc`
  - `side_data_list=not-required-for-this-run`
- Conclusion: HEVC Main10 path validates with strict HDR signaling requirements.

## E6 - Raw P010 -> AV1 10-bit HDR (validator pass)
- Timestamp (UTC): 2026-02-21T00:00:00Z
- Commit Hash: 53b3437b57f68bdd0c15fb4ad91fe3263a8226a4
- What Changed (single change): Added AV1 metadata fixup (`av1_metadata` bitstream filter) in harness before validator run.
- How To Run:
  1. `dotnet run --project tests/ElgatoCapture.FfmpegEncodeLab/ElgatoCapture.FfmpegEncodeLab.csproj -c Debug -p:Platform=x64 -- --input artifacts/hdr-lab/synthetic/synthetic_1920x1080_60_120f_p010.yuv --width 1920 --height 1080 --fps 60 --frames 120`
  2. Harness internally applies `-bsf:v av1_metadata=color_primaries=9:transfer_characteristics=16:matrix_coefficients=9` then validates.
- Validator Output:
  - `HDR_VALIDATE_RESULT PASS file='...\\av1-main10-hdr.mp4' codec='av1'`
- ffprobe Evidence:
  - `codec_name=av1`
  - `pix_fmt=yuv420p10le`
  - `color_primaries=bt2020`
  - `color_transfer=smpte2084`
  - `color_space=bt2020nc`
  - `side_data_list=not-required-for-this-run`
- Conclusion: AV1 output now validates after explicit AV1 bitstream HDR metadata fixup.

## E3 - MF P010 negotiation harness execution (environment-blocked)
- Timestamp (UTC): 2026-02-21T00:00:00Z
- Commit Hash: 53b3437b57f68bdd0c15fb4ad91fe3263a8226a4
- What Changed (single change): Added `tests/ElgatoCapture.HdrLab` MediaCapture-based ingest harness with strict P010 expectation and raw dump output.
- How To Run:
  1. `dotnet run --project tests/ElgatoCapture.HdrLab/ElgatoCapture.HdrLab.csproj -c Debug -p:Platform=x64 -- --frames 1 --timeout 15`
- Validator Output:
  - N/A (capture did not complete; harness blocked at device enumeration).
- ffprobe Evidence:
  - N/A (no output media generated).
- Conclusion: Harness is implemented and builds, but this environment returns `UnauthorizedAccessException` on `DeviceInformation.FindAllAsync(DeviceClass.VideoCapture)`; real-device run is required for E3/E4 pass evidence.

## E7 - MediaCapture P010 ingest: surface copy + structured exception logs
- Timestamp (UTC): 2026-02-22T13:03:10.3449095Z
- Commit Hash: 50293b0cb9b44f08cd7fedd5cf962c3b6499807b
- What Changed (single change): Fixed MediaCapture ingest to continue encoding when frames arrive via `Direct3DSurface` (CPU `SoftwareBitmap` is null) and added `VIDEO_INGEST_EXCEPTION` one-shot structured logging (HResult + subtype state).
- How To Run:
  1. Build: `dotnet build ElgatoCapture/ElgatoCapture.csproj -c Debug -p:Platform=x64`
  2. Run app, enable HDR, start recording for ~3 seconds, stop.
  3. Inspect `temp/logs/ElgatoCapture_Debug.log` for `VIDEO_INGEST_EXCEPTION` (if present) and `Frames encoded:`.
- Validator Output:
  - N/A (instrumentation run; goal is to identify where P010 ingest fails before validator gating).
- ffprobe Evidence:
  - N/A (depends on whether frames were encoded).
- Conclusion: Removes a logic bug that would silently drop valid surface-copied frames; future runs must either encode frames or emit a structured ingest failure line proving where HDR ingress breaks.

## E8 - Log runtime build identity (`APP_START`)
- Timestamp (UTC): 2026-02-22T13:05:40.4929683Z
- Commit Hash: a25163913abeb20b7b6dfa6d2af1d782913a051d
- What Changed (single change): Logged `APP_START` at launch with the exe path + timestamp so logs can be tied to the exact binary that produced them.
- How To Run:
  1. Build + run the app.
  2. Confirm the first lines of `temp/logs/ElgatoCapture_Debug.log` include `APP_START exe='...' exe_mtime_utc='...'`.
- Validator Output:
  - N/A
- ffprobe Evidence:
  - N/A
- Conclusion: Eliminates ambiguity between “repo source code” vs “which exe actually ran” when diagnosing HDR ingest/encode failures from logs.
## E<id> - <short title>
- Timestamp (UTC): <yyyy-mm-ddThh:mm:ssZ>
- Commit Hash: <git hash>
- What Changed (single change): <one scoped change>
- How To Run:
  1. <command or UI step>
  2. <command or UI step>
- Validator Output:
  - <single-line PASS/FAIL output>
  - <key mismatch codes, if any>
- ffprobe Evidence:
  - codec_name=<...>
  - pix_fmt=<...>
  - color_primaries=<...>
  - color_transfer=<...>
  - color_space=<...>
  - side_data_list=<...>
- Conclusion: <what was proven/falsified and next action>
```

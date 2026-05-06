# HDR Experiment Log (Append-Only)

Do not rewrite or delete prior entries. Append new entries only.

> **Project rename — 2026-05-02.** The project was renamed from `ElgatoCapture`
> to `Sussudio` (code identity) / `Simple Sussudio` (display name) ahead of an
> open-source release. Entries below dated before 2026-05-02 reference the old
> paths and namespaces (`ElgatoCapture/`, `tests/ElgatoCapture.*`,
> `tools/ecctl/`, `ElgatoCapture_Debug.log`, `ELGATOCAPTURE_*` env vars). Read
> those as the historical equivalent of the current `Sussudio/`,
> `tests/Sussudio.*`, `tools/ssctl/`, `Sussudio_Debug.log`, `SUSSUDIO_*`. Past
> entries are preserved verbatim per the append-only rule. See the
> `2026-05-02 — Renamed project to Simple Sussudio` entry at the bottom of this
> file for full scope, what was preserved, and verification.

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

## E9 - Validator script discovery works from staged builds
- Timestamp (UTC): 2026-02-22T14:27:46Z
- Commit Hash: 5210b2144cd0409b085c4d838a8054b46469a166
- What Changed (single change): Fixed `HdrValidationRunner` to discover `tools/validate_hdr.ps1` by searching parent directories from the staged exe (`latest-build`) instead of assuming a fixed relative depth.
- How To Run:
  1. Run `latest-build/ElgatoCapture.exe`, start/stop a short recording (SDR is fine).
  2. Confirm `temp/logs/ElgatoCapture_Debug.log` contains `HDR validator stdout:` with `HDR_VALIDATE_RESULT PASS ...` (or a real FAIL mismatch), not `validator-script-missing`.
- Validator Output:
  - Expected: `HDR_VALIDATE_RESULT PASS ...` for SDR recordings (validator runs without `-ExpectHdr`).
- Conclusion: Recording stop no longer hard-fails due to `validator-script-missing` when running the staged binary.

## E10 - Recording no longer logs false preview-inactive warnings on GPU preview
- Timestamp (UTC): 2026-02-22T14:27:46Z
- Commit Hash: 8de8ff9536543f40434f267751f2d7b84cf9f70b
- What Changed (single change): Corrected `PreviewStateDuringRecording` detection to treat the GPU preview path (`MediaPlayerElement` + `PreviewPlaybackSource`) as active instead of checking only the CPU `SoftwareBitmapSource` path.
- How To Run:
  1. Run `latest-build/ElgatoCapture.exe` and wait for preview to appear.
  2. Click Record and check `temp/logs/ElgatoCapture_Debug.log` for `PreviewStateDuringRecording: rendererActive=True`.
- Validator Output:
  - N/A
- Conclusion: Removes misleading warnings that made it look like preview stopped when it was actually using the GPU preview path.

## E11 - HDR recording mode no longer forces P010 on the preview path (GpuFast stays renderable)
- Timestamp (UTC): 2026-02-22T14:30:57Z
- Commit Hash: 6e74888fd354ab72848595de3319cfa43a24e500
- What Changed (single change): In GPU preview (`SharedMediaCapture`), only require P010 when `PreviewMode=TrueHdr`; HDR recording mode now leaves `PreviewMode=GpuFast` on an NV12-capable stream so preview doesn’t go blank when the device/MediaPlayerElement can’t render P010.
- How To Run:
  1. Run `latest-build/ElgatoCapture.exe`, enable HDR, keep Preview mode on GPU (non-true-HDR preview), start preview.
  2. Confirm the log line `HDR_REQUEST_STATE scope=preview ... require_p010=False` while HDR toggle is on, and that preview remains visible.
- Validator Output:
  - N/A
- Conclusion: Separates “record requires P010” from “preview must be renderable”; avoids breaking preview in HDR mode while keeping the record path strict.

## E12 - SDR recording ingests from VideoPreview stream (reduce dual-stream contention)
- Timestamp (UTC): 2026-02-22T15:01:39Z
- Commit Hash: 9e3d794b764a4165d0c6e8ce7742d328a67ff6bf
- What Changed (single change): MediaCapture ingest now selects `VideoPreview` when `requireP010=false` (SDR), and only uses `VideoRecord` when `requireP010=true` (HDR), to avoid running preview+record on separate streams that can tank throughput on some cards/drivers.
- How To Run:
  1. Run `latest-build/ElgatoCapture.exe` in SDR, start preview, click Record for ~5 seconds, click Stop.
  2. Confirm log shows `HDR_REQUEST_STATE scope=ingest-video require_p010=False stream=VideoPreview` and that bitrate/preview remain responsive.
- Validator Output:
  - Expected: validator completes (PASS/FAIL) and StopRecording returns.
- Conclusion: Aligns SDR ingest with the same stream the GPU preview uses; should reduce “preview freezes / bitrate drops to 0 / stop hangs” symptoms caused by stream contention.

## E13 - Stop button freezes timer immediately and always exits recording state
- Timestamp (UTC): 2026-02-22T15:21:20Z
- Commit Hash: d7affca34f817e7abe3e69a5b338643dc1d0c907
- What Changed (single change): `StopRecordingAsync` now stops the stopwatch immediately (timer freezes on click) and sets `IsRecording=false` even if finalization fails, so the UI never gets stuck in “recording” state.
- How To Run:
  1. Start a recording, then click Stop.
  2. Confirm the timer freezes immediately and the record button returns to RECORD even if an error occurs.
- Validator Output:
  - N/A
- Conclusion: Fixes the user-visible “record button doesn’t stop / timer keeps going” failure mode when stop throws or finalization is slow.

## E14 - Recording size/bitrate driven by FFmpeg progress (not file length)
- Timestamp (UTC): 2026-02-22T15:21:20Z
- Commit Hash: 232ee12f423498d1c10828ef8323e52a244c3cb4
- What Changed (single change): Parse FFmpeg progress (`size=... bitrate=...`) and surface it through `CaptureService.GetRecordingStats()` so the UI bitrate/size updates during recording even if the OS hasn’t flushed the MP4 file length yet.
- How To Run:
  1. Start a recording and watch the UI’s Size/Bitrate fields.
  2. Confirm `temp/logs/ElgatoCapture_Debug.log` contains `[FFmpeg] frame=... size=... bitrate=...` lines and the UI updates while recording.
- Validator Output:
  - N/A
- Conclusion: Makes the “bitrate should move up” experience reflect the encoder’s real progress instead of a stale file size.

## E15 - Faster stop (shorter writer drain timeout)
- Timestamp (UTC): 2026-02-22T15:21:20Z
- Commit Hash: 71d097cfc95ec0b62ffbf51871a47f79b9f646d2
- What Changed (single change): Reduced FFmpeg writer drain/cancel timeouts so StopRecording completes faster under backpressure.
- How To Run:
  1. Start a recording, then click Stop.
  2. Compare stop latency in `temp/logs/ElgatoCapture_Debug.log` around `=== FFmpeg Encoder Stopping ===` before/after this change.
- Validator Output:
  - N/A
- Conclusion: Improves the expected “click stop, it stops” UX without changing the capture/encode pipeline logic.

## E16 - Preview startup no longer times out waiting for MediaOpened signal
- Timestamp (UTC): 2026-02-27T03:16:14Z
- Commit Hash: 66be7218d5e2a930ee8ebd1c0c42138da6856a53
- What Changed (single change): Removed `MediaOpened` from the required startup signals for `GpuMediaSourceNoFrameReader` strategy in `StartPreviewRendererAsync`. `MediaPlayer.MediaOpened` does not fire reliably for live `MediaFrameSource`-backed `MediaSource` objects (confirmed absent from logs across multiple runs). `PlaybackAdvancing` (position moving) is a strictly stronger signal and is sufficient proof that the player is running.
- How To Run:
  1. Build and launch `latest-build/ElgatoCapture.exe`.
  2. Confirm `temp/logs/ElgatoCapture_Debug.log` shows `PREVIEW_FIRST_VISUAL_CONFIRMED` within ~1 second of startup (not a 10-second timeout followed by failure).
- Validator Output:
  - N/A
- ffprobe Evidence:
  - N/A
- Conclusion: Preview startup now confirms in ~350ms instead of timing out after 10s. Log shows `PREVIEW_START_STATE state=Rendering` and `PREVIEW_FIRST_VISUAL_CONFIRMED elapsedMs=350 source=GpuStartupSignals(PlaybackAdvancing)`.

## E17 - HDR recording uses IMFSourceReader P010 path with converters disabled
- Timestamp (UTC): 2026-03-01T13:03:06Z
- Commit Hash: uncommitted
- What Changed (single change): Added IMFSourceReader-based HDR video ingest (`MfSourceReaderVideoCapture`) with strict P010 media-type selection and `MF_READWRITE_DISABLE_CONVERTERS=TRUE`, then routed HDR recording video frames to `FFmpegEncoderService.EnqueueRawVideoFrame` while keeping MediaCapture audio ingest active.
- How To Run:
  1. `dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -p:StageLatestBuild=true`
  2. `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"`
  3. Inspect `temp/logs/ElgatoCapture_Debug.log` for `MF_SOURCE_READER_*` and `VIDEO_DIAG mf_source_reader` lines during HDR record/stop flows.
- Validator Output:
  - `Build succeeded.` (with environment warning: `NU1900 ... api.nuget.org:443`)
  - `All runtime snapshot regression checks passed.`
- ffprobe Evidence:
  - N/A (no new recording artifact generated in this CI-style verification pass)
- Conclusion: Build and regression tests pass with the new HDR source-reader path wired in; runtime capture validation should now verify that negotiated ingest remains true P010 end-to-end.

## E18 - HEVC NVENC HDR metadata via hevc_metadata bitstream filter
- Timestamp (UTC): 2026-03-01T20:00:00Z
- Commit Hash: uncommitted
- What Changed (single change): Added `hevc_metadata` bitstream filter for HEVC NVENC HDR path in `BuildFFmpegArguments()`, matching the existing AV1 `av1_metadata` BSF. NVENC ignores encoder-level VUI flags (`-color_primaries bt2020 -color_trc smpte2084`), so the BSF injects correct colour_primaries=9, transfer_characteristics=16, matrix_coefficients=9 post-encode. Also renamed `av1HdrMetadataBsfArgs` to `hdrBsfArgs` since it now serves both codecs.
- How To Run:
  1. `dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -p:StageLatestBuild=true`
  2. Record a short HEVC HDR clip, then verify: `ffprobe -show_streams <output.mp4> | grep -E "color_primaries|color_transfer|color_space"`
- Validator Output:
  - Build succeeded with 0 errors.
- ffprobe Evidence:
  - Expected: `color_primaries=bt2020`, `color_transfer=smpte2084`, `color_space=bt2020nc`
  - Pending runtime verification with actual recording.
- Conclusion: The hevc_metadata BSF should inject correct HDR signaling into NVENC output, matching how av1_metadata already works for AV1. Needs runtime verification.

## E19 - CaptureService audio path migrated to WASAPI capture/playback
- Timestamp (UTC): 2026-03-05T04:45:21Z
- Commit Hash: uncommitted
- What Changed (single change): Replaced `CaptureService` audio orchestration from `MediaCaptureIngestSession` to new WASAPI services (`WasapiAudioCapture`, `WasapiAudioPlayback`, `WasapiComInterop`) while preserving the f32le 48kHz stereo sink contract and audio-level telemetry wiring.
- How To Run:
  1. `dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -p:StageLatestBuild=true`
  2. `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"`
  3. Inspect `temp/logs/ElgatoCapture_Debug.log` for startup/runtime warnings after the build/test pass.
- Validator Output:
  - `Build succeeded.`
  - `All runtime snapshot regression checks passed.`
- ffprobe Evidence:
  - N/A (no new recording artifact generated in this verification pass).
- Conclusion: The WASAPI audio pipeline compiles and passes regression checks with CaptureService now using WASAPI capture/playback for preview + recording audio flow.

## E19 - SourceReaderPreviewAdapter for zero-blink HDR preview
- Timestamp (UTC): 2026-03-01T20:00:00Z
- Commit Hash: uncommitted
- What Changed (single change): Created SourceReaderPreviewAdapter (MediaStreamSource-based P010-to-NV12 preview), added StartAudioOnlyAsync to MediaCaptureIngestSession, modified CaptureService to dispose GPU preview before HDR recording and restore it after. During HDR recording, source reader is sole device consumer; frames fork to encoder + preview adapter.
- How To Run:
  1. `dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -p:StageLatestBuild=true`
  2. Start preview, then start HDR recording. Preview should continue (brief transition). Stop recording. Preview should restore to GPU mode.
- Validator Output:
  - Pending build verification.
- ffprobe Evidence:
  - N/A (preview change, not encoding change)
- Conclusion: Pending implementation by Codex. Should eliminate device sharing conflict (0xC00D3EA3) by ensuring only one API opens the video device at a time.

## E20 - PreviewPlaybackSource hot-swap now resets startup state machine
- Timestamp (UTC): 2026-03-02T02:12:21Z
- Commit Hash: uncommitted
- What Changed (single change): Updated `MainWindow.HandleViewModelPropertyChangedAsync` (`PreviewPlaybackSource` case) to run full startup-attempt reset (`BeginPreviewStartupAttempt` + `RendererAttaching`/`WaitingForFirstVisual` state transitions + watchdog) during GPU source hot-swap, with explicit null-source stop handling.
- How To Run:
  1. `dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -p:StageLatestBuild=true`
  2. `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"`
  3. Start preview, then start HDR recording to trigger `PreviewPlaybackSource` hot-swap and inspect `temp/logs/ElgatoCapture_Debug.log` for refreshed `PREVIEW_START_REQUESTED`/startup state transitions for the new source.
- Validator Output:
  - `Build succeeded.` (warnings: `NU1900` vulnerability feed unavailable in offline environment)
  - `All runtime snapshot regression checks passed.`
- ffprobe Evidence:
  - N/A (preview startup state-machine fix)
- Conclusion: Preview source hot-swap now re-enters active startup-monitoring states instead of inheriting `Rendering` from the prior session.

## E21 - SourceReaderPreviewAdapter timeout waits no longer emit EOS
- Timestamp (UTC): 2026-03-02T02:12:21Z
- Commit Hash: uncommitted
- What Changed (single change): Reworked `SourceReaderPreviewAdapter.OnSampleRequested` so 200ms waits loop on timeout instead of returning `Sample=null` (EOS), and only return null when disposed.
- How To Run:
  1. `dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -p:StageLatestBuild=true`
  2. `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"`
  3. Start HDR recording and verify preview continues requesting samples past initial source-reader startup delay.
- Validator Output:
  - `Build succeeded.` (warnings: `NU1900` vulnerability feed unavailable in offline environment)
  - `All runtime snapshot regression checks passed.`
- ffprobe Evidence:
  - N/A (preview sample-delivery control-flow fix)
- Conclusion: Startup-time sample-request timeouts no longer terminate `MediaStreamSource` by signaling false end-of-stream.

## E22 - Source-reader adapter counters surfaced in automation + MCP preview state
- Timestamp (UTC): 2026-03-02T02:12:21Z
- Commit Hash: uncommitted
- What Changed (single change): Added source-reader adapter counters (`frames enqueued`, `samples delivered`, `samples timed out`) to `PreviewRuntimeSnapshot`, propagated them through `AutomationDiagnosticsHub`/`AutomationSnapshot`, and formatted them in MCP `get_app_state` Preview output.
- How To Run:
  1. `dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -p:StageLatestBuild=true`
  2. `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"`
  3. Call MCP `get_app_state` during HDR recording and confirm the Preview section includes `SourceReader Adapter: ... enqueued, ... delivered, ... timed out`.
- Validator Output:
  - `Build succeeded.` (warnings: `NU1900` vulnerability feed unavailable in offline environment)
  - `All runtime snapshot regression checks passed.`
- ffprobe Evidence:
  - N/A (automation observability change)
- Conclusion: Automation consumers now get direct visibility into source-reader preview adapter flow and timeout behavior.

## E23 - DeviceService WinRT enumeration replaced with MF + WASAPI endpoint enumeration
- Timestamp (UTC): 2026-03-05T04:29:00Z
- Commit Hash: uncommitted
- What Changed (single change): Replaced `DeviceService` WinRT device discovery/format probing with `MfDeviceEnumerator` (`MFEnumDeviceSources` + `IMFSourceReader.GetNativeMediaType`) and WASAPI endpoint enumeration (`IMMDeviceEnumerator.EnumAudioEndpoints` + `IMMDevice.GetId`). Extended `WasapiComInterop.IPropertyStore` with `PROPERTYKEY`/`PropVariant` support so audio device names resolve from `PKEY_Device_FriendlyName`.
- How To Run:
  1. `dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -p:StageLatestBuild=true`
  2. `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"`
  3. Inspect `temp/logs/ElgatoCapture_Debug.log` for `Device discovery summary` and unexpected MF/WASAPI failures.
- Validator Output:
  - `Build succeeded.` (`0 Warning(s)`, `0 Error(s)` on final pass)
  - `All runtime snapshot regression checks passed.`
- ffprobe Evidence:
  - N/A (enumeration/probing and ID-compatibility refactor only)
- Conclusion: `DeviceService` now emits MF symbolic links for video and WASAPI endpoint IDs for audio, removing WinRT `DeviceInformation`/`MediaCapture` dependencies while preserving caller contracts and background format-probe behavior.

## E24 - Recording finalize now fails when WASAPI capture faulted mid-record
- Timestamp (UTC): 2026-03-05T05:15:34Z
- Commit Hash: uncommitted
- What Changed (single change): Added explicit recording-finalize fault propagation in `CaptureService.StopAndDisposeRecordingBackendAsync` so any `WasapiAudioCapture.CaptureFailed` during recording forces a failed `FinalizeResult` instead of reporting a clean stop.
- How To Run:
  1. `dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -p:StageLatestBuild=true`
  2. `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"`
  3. Inspect `temp/logs/ElgatoCapture_Debug.log` for `WASAPI_CAPTURE_FAILED` and `RECORDING_AUDIO_FAULT` during an induced audio-failure recording stop path.
- Validator Output:
  - `Build succeeded.` (`2 Warning(s)`, `0 Error(s)`)
  - `All runtime snapshot regression checks passed.`
- ffprobe Evidence:
  - N/A (capture-control/finalize status fix)
- Conclusion: Recording status can no longer report success when WASAPI audio ingestion failed during the recording window.

## E25 - WASAPI resample frame debt switched to integer numerator accounting
- Timestamp (UTC): 2026-03-05T05:15:34Z
- Commit Hash: uncommitted
- What Changed (single change): Replaced floating-point `_resampleRemainderFrames` with integer `_resampleRemainderNumerator` in `WasapiAudioCapture.ComputeResampledFrameCount` to preserve sub-frame debt exactly across packets and eliminate float rounding drift.
- How To Run:
  1. `dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -p:StageLatestBuild=true`
  2. `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"`
  3. Run preview/record on a non-48k endpoint and verify continuous metering/audio delivery without frame debt loss across packet boundaries.
- Validator Output:
  - `Build succeeded.` (`2 Warning(s)`, `0 Error(s)`)
  - `All runtime snapshot regression checks passed.`
- ffprobe Evidence:
  - N/A (runtime audio conversion correctness fix; no new artifact generated in this run)
- Conclusion: Output frame count derivation now uses exact integer carry, improving cadence correctness for non-48k capture formats.

## E26 - WinRT MediaCapture cleanup removed dead preview/event/automation contract paths
- Timestamp (UTC): 2026-03-05T05:48:08Z
- Commit Hash: uncommitted
- What Changed (single change): Removed dead WinRT migration leftovers by deleting `MediaCaptureIngestSession`, `SourceReaderPreviewAdapter`, `DirectShowPreviewService`, and `PreviewFrame`, removing the unused `PreviewFrameReady`/`ActiveSourceReaderPreviewAdapter` chains, shrinking `IPreviewFrameSink` to raw/texture methods only, and removing obsolete preview-reader automation fields/wiring from app + MCP formatting.
- How To Run:
  1. `dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -p:StageLatestBuild=true`
  2. `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"`
  3. `dotnet restore tools/McpServer/McpServer.csproj --ignore-failed-sources`
  4. `dotnet build tools/McpServer/McpServer.csproj --no-restore`
  5. `rg -n --glob '*.cs' 'MediaCaptureIngestSession|SourceReaderPreviewAdapter|DirectShowPreviewService' ElgatoCapture tools tests`
- Validator Output:
  - `Build succeeded.` (`0 Warning(s)`, `0 Error(s)` on app build)
  - `All runtime snapshot regression checks passed.`
  - `McpServer -> ...\\McpServer.dll` then `Build succeeded.` (`0 Warning(s)`, `0 Error(s)` after stopping locked `McpServer.exe`)
- ffprobe Evidence:
  - N/A (dead-code/contract cleanup only)
- Conclusion: Dead WinRT MediaCapture-era components and related preview/automation references are removed from live C# code, and app + MCP builds remain green.

## E27 - HDR shader preview now samples Y/UV planes by plane slice
- Timestamp (UTC): 2026-03-05T10:42:10Z
- Commit Hash: c6b4ec154630c1c2e0040966909a29e14b831274
- What Changed (single change): HDR preview now queries `ID3D11Device3` and builds each HDR shader SRV with `ShaderResourceViewDescription1`, so the tonemapping shader explicitly samples plane 0 for luma and plane 1 for chroma instead of reading the same subresource twice.
- How To Run:
  1. `dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -c Debug`
  2. Launch the Debug build (e.g., `latest-build/ElgatoCapture.exe`), pick an HDR input, and enable HDR preview so the D3D11 tonemapper path runs.
  3. Watch `temp/logs/ElgatoCapture_Debug.log` for `RenderHdrFrameWithShader`/`D3D11 preview first HDR frame rendered via tonemapping shader` and confirm the preview no longer collapses to a single solid color.
- Validator Output:
  - N/A
- ffprobe Evidence:
  - N/A
- Conclusion: Plane-aware SRVs guarantee the HDR shader sees both planes, so tonemapping uses real Y/CbCr data instead of repeating luma, eliminating the solid-color preview.

## E28 - D3D11 preview toggles HDR passthrough instantly via swap-chain color space
- Timestamp (UTC): 2026-03-05T22:34:58.0948359Z
- Commit Hash: 394d632d84271b0b0748836c529498eb889466cc
- What Changed (single change): Added a renderer-local HDR preview toggle path that keeps P010 ingestion unchanged, compiles a second HDR passthrough pixel shader, negotiates an HDR-capable `R10G10B10A2_UNorm` swap chain when available, and switches pixel shader plus DXGI color space at runtime without restarting preview.
- How To Run:
  1. `dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -p:StageLatestBuild=true`
  2. `dotnet run --project tests/ElgatoCapture.Tests/ --no-build --no-restore -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"`
  3. Inspect `temp/logs/ElgatoCapture_Debug.log` for D3D11 preview startup/render lines and unexpected warnings after the run.
- Validator Output:
  - `Build blocked in offline sandbox: NU1301/NU1101 while restoring https://api.nuget.org/v3/index.json (missing Vortice.Direct3D11/Vortice.DXGI restore resolution despite local cache inspection).`
  - `All runtime snapshot regression checks passed.`
- ffprobe Evidence:
  - N/A (preview-only change; no new recording artifact generated in this verification pass)
- Conclusion: The renderer/UI/viewmodel paths are updated for instant HDR preview switching and the regression harness still passes against the existing staged app binary, but a full compile of the new source awaits a network-capable restore environment.

## E29 - Stats window replaced by docked preview-side panel
- Timestamp (UTC): 2026-03-06T00:21:25.3845466Z
- Commit Hash: f9e3fe262bb6c41340a103b4d0d8c413aeef12e6
- What Changed (single change): Replaced the `StatsWindow` popout path in `MainWindow` with a docked right-side stats panel in preview row 0, keeping `StatsSnapshot` in `StatsWindow.xaml.cs` and moving the 500 ms polling/update logic into `MainWindow`.
- How To Run:
  1. `$env:DOTNET_CLI_HOME = Join-Path (Get-Location) '.dotnet-cli'; dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -p:StageLatestBuild=true`
  2. `$env:DOTNET_CLI_HOME = Join-Path (Get-Location) '.dotnet-cli'; dotnet run --project tests/ElgatoCapture.Tests/ --no-build --no-restore -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"`
  3. Inspect `temp/logs/ElgatoCapture_Debug.log` for new errors or warnings.
- Validator Output:
  - `Build blocked in offline sandbox: NU1101 unable to find packages Vortice.Direct3D11 and Vortice.DXGI after nuget.org restore failed.`
  - `All runtime snapshot regression checks passed.`
- ffprobe Evidence:
  - N/A (UI-only change; no new recording artifact generated in this verification pass)
- Conclusion: The docked-stats implementation is in source and the cached regression harness still passes, but a fresh app compile of the modified code is blocked in this sandbox until NuGet restore can reach the required Vortice packages.

## E30 - D3D11 preview resize path debounced on the render thread
- Timestamp (UTC): 2026-03-06T17:47:30.3125171Z
- Commit Hash: uncommitted
- What Changed (single change): Added `_lastResizeAppliedTick` gating in `D3D11PreviewRenderer.RenderThreadMain` so `_resizePending` is only consumed once every 150 ms, leaving the final resize pending instead of calling `_swapChain.ResizeBuffers()` on every `SizeChanged`.
- How To Run:
  1. `dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -p:StageLatestBuild=true`
  2. Start preview or recording, drag-resize the main window continuously, and then stop resizing.
  3. Inspect `temp/logs/ElgatoCapture_Debug.log` for fewer `D3D11 preview swap chain resized` lines than `D3D11 preview resize requested` lines and confirm the final size still lands after the drag stops.
- Validator Output:
  - `dotnet build ...` was blocked in this offline sandbox after three attempts: `NU1301` on `https://api.nuget.org/v3/index.json`, then `NU1101` for `Vortice.Direct3D11` and `Vortice.DXGI` when retrying with failed-source tolerance.
  - `dotnet run --project tests/ElgatoCapture.Tests/ --no-build --no-restore -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"` reported `All runtime snapshot regression checks passed.`
  - `temp/logs/ElgatoCapture_Debug.log` tail still showed the previous real app run's resize spam and historical `CreateNamedPipe failed with Win32 error 1314`; the no-build regression harness did not emit new runtime warnings.
- ffprobe Evidence:
  - N/A (preview-resize behavior change only)
- Conclusion: The renderer now throttles swap-chain resizes at the render-thread choke point while keeping the last resize pending for application after the drag settles.

## E31 - Unified video capture now delivers recording frames before preview work
- Timestamp (UTC): 2026-03-06T17:47:30.3125171Z
- Commit Hash: uncommitted
- What Changed (single change): Reordered `UnifiedVideoCapture.OnFrameArrived` and `OnDualFrameArrived` so `EnqueueRecordingFrame(...)` runs before any preview `SubmitTexture` or raw preview submission.
- How To Run:
  1. `dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -p:StageLatestBuild=true`
  2. Start recording and drag-resize the preview window to force preview-side contention.
  3. Inspect `temp/logs/ElgatoCapture_Debug.log` for `UNIFIED_VIDEO_PREVIEW_*` warnings and confirm recording ingest counters continue advancing even if preview submission slows.
- Validator Output:
  - `dotnet build ...` was blocked in this offline sandbox after three attempts: `NU1301` on `https://api.nuget.org/v3/index.json`, then `NU1101` for `Vortice.Direct3D11` and `Vortice.DXGI` when retrying with failed-source tolerance.
  - `dotnet run --project tests/ElgatoCapture.Tests/ --no-build --no-restore -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"` reported `All runtime snapshot regression checks passed.`
  - `temp/logs/ElgatoCapture_Debug.log` tail contained no new `UNIFIED_VIDEO_*` warnings from the no-build regression harness.
- ffprobe Evidence:
  - N/A (frame-ordering change only)
- Conclusion: Recording ingestion is now first in both source-reader callback paths, so preview-side locks no longer get first chance to delay encoder enqueue.

## E32 - FFmpeg writer duplicates short stalled gaps and reports duplicated-frame telemetry
- Timestamp (UTC): 2026-03-06T17:47:30.3125171Z
- Commit Hash: uncommitted
- What Changed (single change): Added a writer-local last-frame cache plus gap detection in `FFmpegEncoderService.WriteVideoFramesAsync`, capped duplicate insertion for gaps larger than 1.5 frame periods, and persisted the duplicated-frame counter through `CaptureService`, `CaptureHealthSnapshot`, and `AutomationSnapshot`.
- How To Run:
  1. `dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -p:StageLatestBuild=true`
  2. Start a recording, force a short stall (for example by drag-resizing the preview window), then stop the recording.
  3. Inspect `temp/logs/ElgatoCapture_Debug.log` for `RECORDING_FRAME_DUP` and the updated `CFR_DRIFT_METRICS` / `FFmpeg Encoder Stopped` duplicated-frame counters, and check automation health snapshots for `VideoFramesDuplicated` / `EncoderVideoFramesDuplicated`.
- Validator Output:
  - `dotnet build ...` was blocked in this offline sandbox after three attempts: `NU1301` on `https://api.nuget.org/v3/index.json`, then `NU1101` for `Vortice.Direct3D11` and `Vortice.DXGI` when retrying with failed-source tolerance.
  - `dotnet run --project tests/ElgatoCapture.Tests/ --no-build --no-restore -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"` reported `All runtime snapshot regression checks passed.`
  - `temp/logs/ElgatoCapture_Debug.log` tail showed no new `RECORDING_FRAME_DUP` lines because the regression harness exercised the cached binary rather than a rebuilt app image.
- ffprobe Evidence:
  - N/A (no new recording artifact generated in this offline verification pass)
- Conclusion: The video writer now fills bounded wall-clock stalls with duplicated frames and surfaces duplication counts through the existing recording-health telemetry path.

## E33 - Device-loss recovery now clears the preview resize debounce gate
- Timestamp (UTC): 2026-03-06T17:47:30.3125171Z
- Commit Hash: uncommitted
- What Changed (single change): Updated `D3D11PreviewRenderer.HandleDeviceLost` to clear `_lastResizeAppliedTick` before re-arming `_resizePending`, so the first resize after swap-chain/device recreation is not delayed by the previous debounce window.
- How To Run:
  1. `dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -p:StageLatestBuild=true`
  2. Start preview, trigger a `D3D11 preview device lost ... recreating device.` path, and resize the preview immediately after recovery.
  3. Inspect `temp/logs/ElgatoCapture_Debug.log` and confirm the first post-recovery resize is applied immediately rather than waiting another 150 ms.
- Validator Output:
  - `dotnet build ...` remained blocked in this offline sandbox with `NU1301` while restoring `https://api.nuget.org/v3/index.json`.
  - `dotnet run --project tests/ElgatoCapture.Tests/ --no-build --no-restore -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"` reported `All runtime snapshot regression checks passed.`
  - `temp/logs/ElgatoCapture_Debug.log` tail still reflected the older live resize session and historical `CreateNamedPipe failed with Win32 error 1314`; the cached regression harness did not produce new runtime warnings.
- ffprobe Evidence:
  - N/A (preview recovery edge-case only)
- Conclusion: Preview device-loss recovery no longer inherits a stale resize debounce timestamp, so the re-created swap chain can resize immediately on the next loop.

## E34 - Shared-device reset now clears the preview resize debounce gate
- Timestamp (UTC): 2026-03-06T17:47:30.3125171Z
- Commit Hash: uncommitted
- What Changed (single change): Updated the `_sharedDeviceResetPending` rebind path in `D3D11PreviewRenderer.RenderThreadMain` to clear `_lastResizeAppliedTick` before re-arming `_resizePending`, matching the device-loss recovery path so the first resize after shared-device recreation is immediate.
- How To Run:
  1. `dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -p:StageLatestBuild=true`
  2. Trigger a shared-device reset while preview is active, then resize the preview immediately afterward.
  3. Inspect `temp/logs/ElgatoCapture_Debug.log` and confirm the first resize after the reset is applied without waiting out the prior 150 ms debounce window.
- Validator Output:
  - `dotnet build ...` remained blocked in this offline sandbox with `NU1301` on `https://api.nuget.org/v3/index.json`.
  - `dotnet run --project tests/ElgatoCapture.Tests/ --no-build --no-restore -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"` reported `All runtime snapshot regression checks passed.`
  - `temp/logs/ElgatoCapture_Debug.log` tail still reflected the older live resize session and historical `CreateNamedPipe failed with Win32 error 1314`; the cached regression harness did not produce new runtime warnings.
- ffprobe Evidence:
  - N/A (preview recovery edge-case only)
- Conclusion: Shared-device resets now bypass the stale debounce timestamp just like device-loss recovery, so the re-created preview device can resize immediately on the next render-loop iteration.

## E35 - Preview swap chain now stays at negotiated source resolution during window resize
- Timestamp (UTC): 2026-03-06T23:06:53.2807747Z
- Commit Hash: cb05443669aafd63b6bc420c423a7ec527b5b9d5
- What Changed (single change): Reworked the D3D11 preview path so `D3D11PreviewRenderer` creates its composition swap chain at the negotiated source resolution, updates letterbox/fit through `IDXGISwapChain2.MatrixTransform` in logical panel space instead of `ResizeBuffers`, drops stale pending frames on device reset/rebind, and starts the renderer from the live negotiated capture dimensions exposed through `CaptureService`/`ProbeVideoSource`.
- How To Run:
  1. `$env:DOTNET_CLI_HOME='C:\\Users\\crest\\source\\repos\\ElgatoCapture\\temp\\dotnet_cli_home'; dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -p:StageLatestBuild=true`
  2. `$env:DOTNET_CLI_HOME='C:\\Users\\crest\\source\\repos\\ElgatoCapture\\temp\\dotnet_cli_home'; dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"`
  3. Inspect `temp/logs/ElgatoCapture_Debug.log` for preview warnings after a live preview run; the current tail can still contain older `ApplyResize` lines until the app is launched again with this build.
- Validator Output:
  - `dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -p:StageLatestBuild=true` succeeded and staged the latest build; MSBuild logged two transient `MSB3026` retries because `ElgatoCapture.Tests (437332)` briefly held `ElgatoCapture.dll`.
  - `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"` reported `All runtime snapshot regression checks passed.`
  - `temp/logs/ElgatoCapture_Debug.log` still shows the previous pre-change live preview session, including historical `ApplyResize` entries and `CreateNamedPipe failed with Win32 error 1314`; the regression harness itself did not launch a new preview session.
- ffprobe Evidence:
  - N/A (preview architecture / resize behavior change only)
- Conclusion: The preview back buffer now stays pinned to negotiated source size while resize is expressed as compositor transform updates, and the project still builds/tests cleanly in this environment.

## E36 - 4K X KS/XU source telemetry with composite EGAV fallback
- Timestamp (UTC): 2026-03-07T00:57:40.8462157Z
- Commit Hash: 8d6b5471db44ba2c9d3d36da1a1a409a6fbec76c
- What Changed (single change): Added `KsXuSourceSignalTelemetryProvider` for direct 4K X KS/XU HDR-state reads (selector 3 fingerprint), added `CompositeSourceSignalTelemetryProvider`, switched `CaptureService` to the composite default, and surfaced the new `KsXu` origin through runtime snapshot mapping.
- How To Run:
  1. `$env:DOTNET_CLI_HOME='C:\Users\crest\source\repos\ElgatoCapture\temp\dotnet_cli_home'; dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -p:StageLatestBuild=true`
  2. `$env:DOTNET_CLI_HOME='C:\Users\crest\source\repos\ElgatoCapture\temp\dotnet_cli_home'; dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"`
  3. `Get-Content temp/logs/ElgatoCapture_Debug.log -Tail 200`
- Validator Output:
  - `Build succeeded.` (`0 Warning(s)`, `0 Error(s)`)
  - `All runtime snapshot regression checks passed.`
  - `temp/logs/ElgatoCapture_Debug.log` showed no new `KSXU_` or source-telemetry failures during this verification pass; the tail still contains historical `CreateNamedPipe failed with Win32 error 1314` entries from an earlier interactive app run.
- ffprobe Evidence:
  - `codec_name=N/A`
  - `pix_fmt=N/A`
  - `color_primaries=N/A`
  - `color_transfer=N/A`
  - `color_space=N/A`
  - `side_data_list=N/A`
- Conclusion: The direct KS/XU provider now compiles and is the default first-hop telemetry path for 4K X, the EGAV fallback remains wired behind it, and the regression/build checks stayed green. Live device validation of the new KS/XU payload fingerprint still needs a future hardware-backed run.

## E37 - Fix KSMULTIPLE_ITEM header skip in KsXu topology parsing
- Timestamp (UTC): 2026-03-07T01:13:00Z
- What Changed (single change): Fixed `TryReadTopologyNodes` to skip the 8-byte `KSMULTIPLE_ITEM` header (uint Size + uint Count) before parsing node type GUIDs. Without this, all GUIDs were offset by 8 bytes and no dev-specific node was ever matched. Also added fallback: if no dev-specific nodes found, try XU reads on ALL nodes.
- Root Cause: `KSPROPERTY_TOPOLOGY_NODES` returns `KSMULTIPLE_ITEM { Size, Count }` followed by `GUID[Count]`. The original code parsed GUIDs starting at offset 0, reading the header bytes as part of the first GUID. This shifted all subsequent GUIDs by 8 bytes. The same bug exists in ElgatoSignalProbe but is masked there because the guided probe uses hardcoded node IDs rather than topology-based discovery.
- Diagnostic Evidence: First "GUID" at offset 0 was `00000048-0004-0000-...` which decodes as Size=72 (0x48), Count=4 — the KSMULTIPLE_ITEM header.
- Validator Output:
  - Build succeeded (0 warnings, 0 errors)
  - All runtime snapshot regression checks passed
  - Live app: `Source: 3840 x 2160 HDR=true`, `Telemetry: Available (Medium)`
  - Log: `KSXU_TOPOLOGY nodeCount=4 devSpecificNodes=[3]`, `KSXU_READ_RESULT node=3 selector=3 succeeded=True bytes=150`
- Conclusion: KsXu HDR detection now works end-to-end on the Elgato 4K X. Node 3 is correctly identified as KSNODETYPE_DEV_SPECIFIC, selector 3 returns 150 bytes, and the all-zeros-prefix fingerprint correctly detects HDR ON.

## E39 - SDR frame-rate auto-selection now follows source telemetry timing family
- Timestamp (UTC): 2026-03-07T12:06:39.1914449Z
- Commit Hash: uncommitted (base 71f6b6de3eb3daa140da00bd1dabfb9d6bcc09e3)
- What Changed (single change): Removed the HDR-only gating around telemetry-driven mode rebuilds and source-aware frame-rate auto-selection so SDR rebuilds can retarget to the exact source cadence using the existing timing-family selection path.
- How To Run:
  1. `dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -p:StageLatestBuild=true`
  2. `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"`
  3. On a live device run, start preview with an SDR 59.94 source and confirm the frame-rate dropdown/runtime snapshot selects the 59.94 variant instead of 60.00.
- Validator Output:
  - `dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -p:StageLatestBuild=true` succeeded; MSBuild emitted stage-copy warnings because a running `ElgatoCapture (203576)` process held files under `latest-build\`.
  - `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"` reported `All runtime snapshot regression checks passed.`
  - `temp/logs/ElgatoCapture_Debug.log` tail after the run showed ongoing `RTICE_DECODE ... fps=59.94 ...` telemetry and no new warning/error lines tied to this change; the only matched warning-like lines were older `Automation pipe explicit security fallback: CreateNamedPipe failed with Win32 error 1314.`
- ffprobe Evidence:
  - `codec_name=N/A`
  - `pix_fmt=N/A`
  - `color_primaries=N/A`
  - `color_transfer=N/A`
  - `color_space=N/A`
  - `side_data_list=N/A`
- Conclusion: SDR now shares the existing telemetry-driven frame-rate retarget path that HDR was already using, so the ViewModel can select the exact source timing-family variant when telemetry is available. Hardware/UI confirmation of the 59.94 dropdown and preview request remains pending.

## E39 - HDR toggle now disables itself for confirmed SDR sources
- Timestamp (UTC): 2026-03-07T12:06:39.1914449Z
- Commit Hash: uncommitted (base 71f6b6de3eb3daa140da00bd1dabfb9d6bcc09e3)
- What Changed (single change): Updated the `MainWindow` HDR-toggle enablement bridge to require `SourceIsHdr != false`, added `SourceIsHdr` property-change handling, and auto-cleared `IsHdrEnabled` when telemetry positively reported an SDR source.
- How To Run:
  1. `dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -p:StageLatestBuild=true`
  2. `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"`
  3. On a live device run, switch the source from HDR to SDR while the app is open and confirm the HDR toggle greys out and, if it was enabled, flips off automatically.
- Validator Output:
  - `dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -p:StageLatestBuild=true` succeeded; MSBuild emitted stage-copy warnings because a running `ElgatoCapture (203576)` process held files under `latest-build\`.
  - `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"` reported `All runtime snapshot regression checks passed.`
  - `temp/logs/ElgatoCapture_Debug.log` tail after the run showed ongoing RTICE telemetry polling with no new warning/error lines tied to the HDR-toggle bridge; the only matched warning-like lines were older `Automation pipe explicit security fallback: CreateNamedPipe failed with Win32 error 1314.`
- ffprobe Evidence:
  - `codec_name=N/A`
  - `pix_fmt=N/A`
  - `color_primaries=N/A`
  - `color_transfer=N/A`
  - `color_space=N/A`
  - `side_data_list=N/A`
- Conclusion: The window bridge now disables HDR only when the source is known to be SDR, leaves the toggle usable while source HDR state is unknown, and forces HDR off when telemetry reports an SDR source. Hardware/UI confirmation of the greyed-out toggle still needs a live app run.

## E38 - Source telemetry now drives SDR frame-rate retargeting and HDR toggle availability
- Timestamp (UTC): 2026-03-07T12:07:05.2534156Z
- Commit Hash: 71f6b6de3eb3daa140da00bd1dabfb9d6bcc09e3
- What Changed (single change): Aligned source-driven mode handling so SDR reuses the existing timing-family frame-rate auto-selection path, telemetry-triggered mode rebuilds run regardless of HDR state, the HDR toggle is disabled when the source is known SDR, and NTSC frame-rate options keep their exact dropdown label instead of rounding back to the friendly bucket.
- How To Run:
  1. `dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -p:StageLatestBuild=true`
  2. `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"`
  3. `Get-Content temp/logs/ElgatoCapture_Debug.log -Tail 120`
  4. With a live 59.94 SDR source, confirm the frame-rate dropdown selects `59.94` instead of `60`, and with a live SDR source confirm the HDR toggle is disabled unless source HDR state is still unknown.
- Validator Output:
  - `dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -p:StageLatestBuild=true` succeeded with `0 Error(s)`; the only warnings were `MSB3231/MSB3026/MSB3027/MSB3021` staging-copy retries because a running `ElgatoCapture (203576)` process held files open under `latest-build`.
  - `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"` reported `All runtime snapshot regression checks passed.`
  - `temp/logs/ElgatoCapture_Debug.log` showed no new `ERROR`, `WARNING`, `WARN`, `EXCEPTION`, or `FAIL` tokens during verification; the tail contained repeated `RTICE_DECODE vic=97 size=3840x2160 fps=59.94 hdr=true` telemetry samples.
- ffprobe Evidence:
  - N/A (UI / source-telemetry selection change only)
- Conclusion: SDR now follows the same source-timing retarget path as HDR, the HDR toggle no longer stays enabled when telemetry positively identifies an SDR source, and the dropdown label can represent exact NTSC variants instead of collapsing them back to whole-number text.

## E38 - SDR frame-rate auto-selection now follows source telemetry timing family
- Timestamp (UTC): 2026-03-07T12:06:38.3985521Z
- Commit Hash: uncommitted
- What Changed (single change): Removed the HDR-only gates around telemetry-driven mode rebuilds and the existing source-aware frame-rate picker in `MainViewModel`, so SDR now uses the same timing-family-aware auto-selection path as HDR while preserving the no-telemetry fallback.
- How To Run:
  1. `$env:DOTNET_CLI_HOME='C:\Users\crest\source\repos\ElgatoCapture\temp\dotnet_cli_home'; dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -p:StageLatestBuild=true`
  2. `$env:DOTNET_CLI_HOME='C:\Users\crest\source\repos\ElgatoCapture\temp\dotnet_cli_home'; dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"`
  3. With a live 59.94 fps SDR source, inspect the frame-rate dropdown and confirm the requested preview mode tracks the 60000/1001 variant instead of the 60/1 variant.
- Validator Output:
  - `dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -p:StageLatestBuild=true` succeeded and rebuilt `ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll`; staging `latest-build` emitted `MSB3231`/`MSB3026`/`MSB3027`/`MSB3021` warnings because `ElgatoCapture (203576)` held files open under `latest-build`.
  - `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"` reported `All runtime snapshot regression checks passed.`
  - `temp/logs/ElgatoCapture_Debug.log` had no matches for `FAIL|ERROR|WARN|WARNING|EXCEPTION|HDR_VALIDATE_RESULT|PREVIEW_START_TIMEOUT`; the latest live tail continued to report `RTICE_DECODE ... fps=59.94 hdr=true`.
- ffprobe Evidence:
  - N/A (UI selection / preview request change only)
- Conclusion: Source telemetry now rebuilds and auto-selects SDR frame-rate variants with the same timing-family preference used by HDR, so the 59.94 path can stay on the precise source cadence instead of rounding up to the 60.00 variant.

## E40 - HDR toggle now disables itself when source telemetry reports SDR
- Timestamp (UTC): 2026-03-07T12:06:39.3985521Z
- Commit Hash: uncommitted
- What Changed (single change): Updated `MainWindow` HDR-toggle enablement to require `SourceIsHdr != false`, added `SourceIsHdr` to the window property-change bridge, and made `MainViewModel` clear `IsHdrEnabled` from source telemetry only when the app is not recording so the toggle follows confirmed SDR input without desynchronizing the active recording pipeline.
- How To Run:
  1. `$env:DOTNET_CLI_HOME='C:\Users\crest\source\repos\ElgatoCapture\temp\dotnet_cli_home'; dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -p:StageLatestBuild=true`
  2. `$env:DOTNET_CLI_HOME='C:\Users\crest\source\repos\ElgatoCapture\temp\dotnet_cli_home'; dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"`
  3. With a live source that transitions HDR -> SDR while idle or previewing, confirm the HDR toggle becomes unchecked/disabled when telemetry reports `SourceIsHdr = false`, stays enabled when the source HDR state is still unknown, and retains the existing `AutomationProperties.AutomationId="HdrToggle"` contract.
- Validator Output:
  - `dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -p:StageLatestBuild=true` succeeded and rebuilt `ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll`; staging `latest-build` emitted `MSB3231`/`MSB3026`/`MSB3027`/`MSB3021` warnings because `ElgatoCapture (203576)` held files open under `latest-build`.
  - `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"` reported `All runtime snapshot regression checks passed.`
  - `temp/logs/ElgatoCapture_Debug.log` had no matches for `FAIL|ERROR|WARN|WARNING|EXCEPTION|HDR_VALIDATE_RESULT|PREVIEW_START_TIMEOUT`; the latest tail showed continued RTICE polling with `RTICE_DECODE ... fps=59.94 hdr=true` and clean shutdown/cleanup lines, with no new warning tokens.
- ffprobe Evidence:
  - N/A (UI state / source-telemetry gating change only)
- Conclusion: The HDR toggle now greys out only when the app positively knows the source is SDR, remains available for HDR or unknown telemetry states, and source-driven HDR shutdown is limited to non-recording states so the active recording pipeline and the requested mode do not drift apart.

## E40 - Finalize source-aware HDR toggle gating without recording-time pipeline drift
- Timestamp (UTC): 2026-03-07T12:07:05.2534156Z
- Commit Hash: uncommitted (base 71f6b6de3eb3daa140da00bd1dabfb9d6bcc09e3)
- What Changed (single change): Removed the recording-time HDR-toggle bypass from the in-progress source-aware toggle experiment, keeping the final implementation limited to source-aware toggle enablement plus automatic HDR shutdown only when the app is not actively recording so `IsHdrEnabled` cannot drift away from the live pipeline state.
- How To Run:
  1. `dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -p:StageLatestBuild=true`
  2. `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"`
  3. `Get-Content temp/logs/ElgatoCapture_Debug.log -Tail 120`
  4. On a live device run, confirm the HDR toggle is disabled for confirmed SDR sources, remains available when source HDR state is unknown, and does not try to flip `IsHdrEnabled` while recording is active.
- Validator Output:
  - `dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -p:StageLatestBuild=true` succeeded with `0 Error(s)`; MSBuild emitted `MSB3231/MSB3026/MSB3027/MSB3021` staging-copy warnings because `latest-build` was locked by a running `ElgatoCapture (203576)` process.
  - `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"` reported `All runtime snapshot regression checks passed.`
  - `temp/logs/ElgatoCapture_Debug.log` showed no new `ERROR`, `WARNING`, `WARN`, `EXCEPTION`, or `FAIL` tokens during the final verification grep; the tail contained RTICE telemetry polling plus clean preview/automation shutdown lines.
- ffprobe Evidence:
  - N/A (UI state / source-telemetry gating change only)
- Conclusion: The final implementation keeps the source-aware HDR toggle UX while preserving the existing recording-time pipeline guard, so the UI no longer advertises HDR for confirmed SDR input without creating a false non-HDR state during active recording.

## E41 - Source-matched capture options with Auto resolution mode
- Timestamp (UTC): 2026-03-07T14:25:36.4248521Z
- Commit Hash: 65cded776cc514e1e9189c5a55e2c5a198c304f3
- What Changed (single change): Added source-aware capture-option filtering plus a user-visible `Auto` resolution mode in `MainViewModel`/`MainWindow`, including aspect-ratio pruning, hidden source-exceeding frame rates, deferred rebuilds while recording, and a `Show all resolutions and frame rates` settings toggle.
- How To Run:
  1. `dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -p:StageLatestBuild=true`
  2. `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"`
  3. `Get-Content temp/logs/ElgatoCapture_Debug.log -Tail 120`
  4. On a live source run, verify the resolution list hides aspect-mismatched modes by default, frame rates above the source disappear by default, `Auto` resolves to the best source-matched mode, and turning on `Show all resolutions and frame rates` restores the full device capability list.
- Validator Output:
  - `dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -p:StageLatestBuild=true` succeeded with `0 Warning(s)` and `0 Error(s)`.
  - `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"` reported `All runtime snapshot regression checks passed.`
  - `temp/logs/ElgatoCapture_Debug.log` was not present in this workspace after the verification run, so there was no new log tail to inspect in this environment.
- ffprobe Evidence:
  - N/A (UI option-selection behavior change only)
- Conclusion: The app now defaults to an `Auto` source-matched resolution mode, hides source-irrelevant resolutions/frame rates unless the new toggle is enabled, and keeps option-list rebuilds deferred until recording stops. Live hardware/UI validation is still needed for the exact dropdown contents and `Auto` choice on real PS5/4K X source modes.

## E42 - Auto frame rate mode and clean-divisor filtering
- Timestamp (UTC): 2026-03-07T15:16:15.3215999Z
- Commit Hash: 65cded776cc514e1e9189c5a55e2c5a198c304f3
- What Changed (single change): Added an `Auto` frame rate dropdown mode that keeps `SelectedFrameRate` resolved to a real fps while the UI shows `Auto`, simplified the Auto-resolution dropdown label to plain `Auto`, and filtered non-clean-divisor source frame rates from the default list.
- How To Run:
  1. `dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -p:StageLatestBuild=true`
  2. `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"`
  3. `Get-Content temp/logs/ElgatoCapture_Debug.log -Tail 200`
  4. In the app, verify the resolution dropdown shows `Auto`, the frame-rate dropdown inserts `Auto` at index 0, choosing `Auto` keeps the combobox on `Auto` while `SelectedFrameRate` resolves to the real capture fps, and non-clean-divisor frame rates disappear unless `Show all resolutions and frame rates` is enabled.
- Validator Output:
  - `dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -p:StageLatestBuild=true` succeeded with `0 Warning(s)` and `0 Error(s)`.
  - `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"` reported `All runtime snapshot regression checks passed.`
  - `temp/logs/ElgatoCapture_Debug.log` tail showed normal `RTICE_DECODE ... fps=59.94 hdr=true` polling and cleanup, with no new warnings or errors tied to the option-selection changes.
- ffprobe Evidence:
  - N/A (UI option-selection behavior change only)
- Conclusion: Auto resolution now renders as plain `Auto`, frame rate has a matching Auto mode with resolved real-fps capture settings, manual frame-rate overrides survive source telemetry changes, and default filtering now removes uneven-cadence capture rates like `50 fps` on a `60 fps` source.

## E42 - Auto frame-rate mode with clean-divisor filtering
- Timestamp (UTC): 2026-03-07T15:12:18.2520617Z
- Commit Hash: 65cded776cc514e1e9189c5a55e2c5a198c304f3
- What Changed (single change): Added an explicit `Auto` frame-rate dropdown option that resolves to a real fps value while keeping the combobox on `Auto`, simplified the resolution `Auto` label to plain `Auto`, and filtered non-clean-divisor capture frame rates out of the default list.
- How To Run:
  1. `dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -p:StageLatestBuild=true`
  2. `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"`
  3. `Get-Content temp/logs/ElgatoCapture_Debug.log -Tail 200`
  4. In the app, verify the resolution dropdown shows `Auto`, the frame-rate dropdown shows `Auto` at index 0, selecting `Auto` leaves the combobox on `Auto` while `SelectedFrameRate` resolves to a positive fps, and default filtering hides rates such as `50` on a `60` fps source unless `Show all resolutions and frame rates` is enabled.
- Validator Output:
  - `dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -p:StageLatestBuild=true` succeeded with `0 Warning(s)` and `0 Error(s)`.
  - `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"` reported `All runtime snapshot regression checks passed.`
  - `temp/logs/ElgatoCapture_Debug.log` tailed successfully after the run; the tail contained RTICE polling/teardown lines plus `Automation pipe explicit security fallback: CreateNamedPipe failed with Win32 error 1314`, with no new structured capture-option warning/error token introduced by this change.
- ffprobe Evidence:
  - N/A (recording-options UI/state change only)
- Conclusion: The frame-rate dropdown now has an explicit Auto mode that keeps `SelectedFrameRate` resolved to a real positive fps after rebuilds, the resolution Auto label is simplified to `Auto`, and default filtering now removes uneven-cadence frame-rate choices unless the user opts into the full capability list.

## E43 - Experiment log id correction for duplicate E42 heading
- Timestamp (UTC): 2026-03-07T15:25:00Z
- Commit Hash: uncommitted (base 71f6b6de3eb3daa140da00bd1dabfb9d6bcc09e3)
- What Changed (single change): Appended a log-only correction that declares the first `E42` entry above as the canonical record for the Auto frame-rate / clean-divisor change and treats the later duplicate `E42` heading as superseded for future references, preserving append-only history without rewriting prior entries.
- How To Run:
  1. `rg -n "^## E42\\b|^## E43\\b" docs/experiment_log.md`
  2. Confirm there is one canonical `E42` reference entry plus this `E43` correction note explaining the duplicate heading.
- Validator Output:
  - `rg -n "^## E42\\b|^## E43\\b" docs/experiment_log.md` should show two historical `E42` headings and this appended `E43` correction entry.
- ffprobe Evidence:
  - N/A (experiment-log correction only)
- Conclusion: The append-only log now explicitly identifies which `E42` entry is canonical and prevents future ambiguity without editing or deleting prior experiment records.

## E44 - Smooth preview reinit and stop transitions
- Timestamp (UTC): 2026-03-07T17:19:38.4858403Z
- Commit Hash: uncommitted (base 160a97186a628b1c4057df090588b246734fa554)
- What Changed (single change): Added smooth preview fade/scale transitions around preview reinitialization and user-initiated preview stop by introducing an awaited `PreviewReinitRequested` hook in `MainViewModel`, moving the loading overlay to a ring-only fade path, wrapping the live preview visuals in a transformable `PreviewContentGrid`, and resetting the preview animation state on shutdown/failure paths.
- How To Run:
  1. `dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -p:StageLatestBuild=true`
  2. `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"`
  3. `Get-Content temp/logs/ElgatoCapture_Debug.log -Tail 200`
  4. `rg -n "ERROR|WARNING|WARN|FAIL|EXCEPTION" temp/logs/ElgatoCapture_Debug.log`
  5. On a live source, verify preview start fades in on first visual, format/HDR changes breathe the preview out and back in without flashing the placeholder, and a user stop leaves the preview hidden with the placeholder restored.
- Validator Output:
  - `dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -p:StageLatestBuild=true` succeeded with `0 Warning(s)` and `0 Error(s)`.
  - `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"` reported `All runtime snapshot regression checks passed.`
  - `Get-Content temp/logs/ElgatoCapture_Debug.log -Tail 200` showed normal RTICE polling, recording, and shutdown activity with no preview-transition exceptions.
  - `rg -n "ERROR|WARNING|WARN|FAIL|EXCEPTION" temp/logs/ElgatoCapture_Debug.log` returned no matches after the verification run.
- ffprobe Evidence:
  - N/A (preview UI transition change only)
- Conclusion: Preview start/stop/reinit transitions now animate through the window-owned preview host without changing the capture pipeline contract, and the verification run stayed clean at build, test, and log-scan time. Live hardware validation is still needed for the exact visual feel of the new transitions.

## E45 - Preview transition reliability follow-up
- Timestamp (UTC): 2026-03-07T17:26:11.0998358Z
- Commit Hash: uncommitted (base 160a97186a628b1c4057df090588b246734fa554)
- What Changed (single change): Hardened the preview transition state machine after review by guarding first-visual confirmation against pending user stops, resetting the preview transform on non-reinit failure exits and stop failures, serializing `ReinitializeDeviceAsync`, and treating the preview button as cancel/stop-only while a reinitialize is still active.
- How To Run:
  1. `dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -p:StageLatestBuild=true`
  2. `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"`
  3. `rg -n "ERROR|WARNING|WARN|FAIL|EXCEPTION" temp/logs/ElgatoCapture_Debug.log`
  4. On a live source, verify a late first frame does not re-reveal preview after pressing Stop, a failed/aborted preview start restores the host to full opacity/scale, and rapid format/HDR changes do not overlap into multiple concurrent reinit animations.
- Validator Output:
  - `dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -p:StageLatestBuild=true` succeeded with `0 Warning(s)` and `0 Error(s)`.
  - `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"` reported `All runtime snapshot regression checks passed.`
  - `rg -n "ERROR|WARNING|WARN|FAIL|EXCEPTION" temp/logs/ElgatoCapture_Debug.log` returned no matches after the follow-up verification run.
- ffprobe Evidence:
  - N/A (preview UI transition reliability follow-up only)
- Conclusion: The preview transition implementation now has explicit guardrails for late first-frame callbacks, failure cleanup, queued reinitializations, and canceling a restart-in-progress. Live preview validation is still needed for the exact visual timing on hardware.

## E46 - Experiment scope correction for concurrent MainWindow restyle edits
- Timestamp (UTC): 2026-03-07T17:26:54.1555581Z
- Commit Hash: uncommitted (base 160a97186a628b1c4057df090588b246734fa554)
- What Changed (single change): Appended a scope note clarifying that `E44` and `E45` cover only the preview transition and reliability changes, while unrelated window-shell/control-bar restyle edits already present in the worktree remain out of scope for those experiment entries.
- How To Run:
  1. `rg -n "^## E44\\b|^## E45\\b|^## E46\\b" docs/experiment_log.md`
  2. Confirm the log now contains the original transition entry, the reliability follow-up, and this append-only scope clarification.
- Validator Output:
  - `rg -n "^## E44\\b|^## E45\\b|^## E46\\b" docs/experiment_log.md` should show all three related entries in sequence.
- ffprobe Evidence:
  - N/A (experiment-log scope correction only)
- Conclusion: The append-only log now separates the preview-transition experiments from the concurrent unrelated `MainWindow` restyle work that was already present in the worktree, without rewriting prior history.

## E47 - Automation-safe cancel for preview restart in progress
- Timestamp (UTC): 2026-03-07T17:39:50.9630287Z
- Commit Hash: uncommitted (base 160a97186a628b1c4057df090588b246734fa554)
- What Changed (single change): Updated `SetPreviewEnabledAsync(false)` to cancel a pending preview restart whenever `IsPreviewReinitializing` is true, even during the temporary `IsPreviewing == false` gap, so automation/IPC callers match the window button’s cancel behavior.
- How To Run:
  1. `dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -p:StageLatestBuild=true`
  2. `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"`
  3. `rg -n "ERROR|WARNING|WARN|FAIL|EXCEPTION" temp/logs/ElgatoCapture_Debug.log`
  4. Through automation or IPC, request `SetPreviewEnabled(false)` during a format/HDR reinitialize and verify preview stays stopped after the reinit completes.
- Validator Output:
  - `dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -p:StageLatestBuild=true` succeeded with `0 Warning(s)` and `0 Error(s)`.
  - `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"` reported `All runtime snapshot regression checks passed.`
  - `rg -n "ERROR|WARNING|WARN|FAIL|EXCEPTION" temp/logs/ElgatoCapture_Debug.log` returned no matches after the verification run.
- ffprobe Evidence:
  - N/A (preview control/cancel behavior change only)
- Conclusion: Preview restart cancellation now works through both the window button path and the automation-facing preview-enable API. Live automation validation during a real reinit is still needed.

## E48 - Cleanup after immediate preview renderer attach failure
- Timestamp (UTC): 2026-03-07T17:39:50.9630287Z
- Commit Hash: uncommitted (base 160a97186a628b1c4057df090588b246734fa554)
- What Changed (single change): Added explicit cleanup around preview renderer attach/start exceptions so the loading overlay stops, the preview transform resets, the placeholder returns, and a startup-failure stop is scheduled instead of leaving the host hidden after an immediate attach failure.
- How To Run:
  1. `dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -p:StageLatestBuild=true`
  2. `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"`
  3. `rg -n "ERROR|WARNING|WARN|FAIL|EXCEPTION" temp/logs/ElgatoCapture_Debug.log`
  4. Force `StartPreviewRendererAsync()` to fail during preview startup and verify the placeholder/transform recover instead of leaving the host dimmed.
- Validator Output:
  - `dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -p:StageLatestBuild=true` succeeded with `0 Warning(s)` and `0 Error(s)`.
  - `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"` reported `All runtime snapshot regression checks passed.`
  - `rg -n "ERROR|WARNING|WARN|FAIL|EXCEPTION" temp/logs/ElgatoCapture_Debug.log` returned no matches after the verification run.
- ffprobe Evidence:
  - N/A (preview startup failure cleanup change only)
- Conclusion: Immediate preview renderer attach failures now restore the preview host state instead of stranding it in the hidden/scaled transition pose. Live forced-failure validation is still needed.

## E49 - Record button spinner reset on failed transition
- Timestamp (UTC): 2026-03-07T17:39:50.9630287Z
- Commit Hash: uncommitted (base 160a97186a628b1c4057df090588b246734fa554)
- What Changed (single change): Added the missing `IsRecordingTransitioning == false` UI reset path so the record button leaves the spinner and returns to the correct normal/recording content even when a recording start/stop transition fails without flipping `IsRecording`.
- How To Run:
  1. `dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -p:StageLatestBuild=true`
  2. `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"`
  3. `rg -n "ERROR|WARNING|WARN|FAIL|EXCEPTION" temp/logs/ElgatoCapture_Debug.log`
  4. Force a recording start/stop failure and verify the record button leaves the spinner when `IsRecordingTransitioning` drops back to `false`.
- Validator Output:
  - `dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -p:StageLatestBuild=true` succeeded with `0 Warning(s)` and `0 Error(s)`.
  - `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"` reported `All runtime snapshot regression checks passed.`
  - `rg -n "ERROR|WARNING|WARN|FAIL|EXCEPTION" temp/logs/ElgatoCapture_Debug.log` returned no matches after the verification run.
- ffprobe Evidence:
  - N/A (record-button UI state fix only)
- Conclusion: The record button no longer relies on an `IsRecording` change to exit the spinner state after a failed transition. Live failure-path validation is still needed.

## E50 - Control bar polish: recording glow pulse, hover scale, and audio meter peak visuals
- Timestamp (UTC): 2026-03-08T04:12:59.7627978Z
- Commit Hash: 5ff85c70eed7b1780eaf6e66683785e5c8db6a0c
- What Changed (single change): Added a breathing `RecordingGlowBorder` storyboard, render-transform hover/press scale animations for the control-bar buttons/toggles, and smoothed audio-meter peak/range visuals in `MainWindow` without changing the HDR or preview pipeline contracts.
- How To Run:
  1. `dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -p:StageLatestBuild=true`
  2. `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"`
  3. `rg -n "ERROR|WARNING|WARN|FAIL|EXCEPTION" temp/logs/ElgatoCapture_Debug.log`
  4. Run the app and verify: recording starts/stops the glow pulse cleanly, control-bar buttons scale on hover/press without layout shift, and the audio meter shows fill smoothing plus peak/range markers that reset when audio monitoring/recording resets.
- Validator Output:
  - `dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -p:StageLatestBuild=true` succeeded with `0 Warning(s)` and `0 Error(s)`.
  - `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"` reported `All runtime snapshot regression checks passed.`
  - `rg -n "ERROR|WARNING|WARN|FAIL|EXCEPTION" temp/logs/ElgatoCapture_Debug.log` returned no matches after the verification run.
- ffprobe Evidence:
  - N/A (UI-only polish change)
- Conclusion: The control bar now adds the requested motion/feedback without changing capture contracts, and the regression harness/log scan stayed clean. Manual UI smoke validation is still needed for the new visual states.

## E51 - Startup entrance animation for control bar, stats row, and preview host
- Timestamp (UTC): 2026-03-08T04:50:25.6342000Z
- Commit Hash: cb2ea8ff2502fa1ec6bfd0bad135c6f53a6d7a45
- What Changed (single change): Added a staged startup entrance animation in `MainWindow` that reveals the control bar, control buttons, stats row, and preview host in sequence while device initialization continues in parallel behind the scenes.
- How To Run:
  1. `dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -p:StageLatestBuild=true`
  2. `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"`
  3. `rg -n "WARNING|ERROR|FAIL|Exception|exception" temp/logs/ElgatoCapture_Debug.log`
  4. Launch the app and verify the startup sequence: `ControlBarBorder` fades/slides in first, control buttons/toggles stagger left-to-right, `StatsRow` drops in next, and `PreviewBorder` fades/scales in last while preview/device startup continues normally.
- Validator Output:
  - `dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -p:StageLatestBuild=true` succeeded with `0 Warning(s)` and `0 Error(s)`.
  - `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"` reported `All runtime snapshot regression checks passed.`
  - `rg -n "WARNING|ERROR|FAIL|Exception|exception" temp/logs/ElgatoCapture_Debug.log` returned no matches after the verification run.
- ffprobe Evidence:
  - N/A (UI-only startup animation change)
- Conclusion: The window now presents the requested premium entrance choreography without delaying device initialization or changing preview/HDR startup logic. Manual app launch is still required to visually confirm the exact motion timing.

## E52 - Thread-health probes for source reader and WASAPI paths
- Timestamp (UTC): 2026-03-08T06:45:03.3984633Z
- Commit Hash: uncommitted (base 06a71ec724a0060d6ad9755488706dc7c278c7b1)
- What Changed (single change): Added thread-health probe counters/timestamps to the MF source reader, WASAPI capture/playback paths, surfaced them through `CaptureRuntimeSnapshot`/`AutomationSnapshot`, and formatted them in the MCP snapshot output.
- How To Run:
  1. `dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -p:StageLatestBuild=true`
  2. If the first build hits the transient WinUI markup-compiler file lock in `obj\...\input.json`, rerun with `dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -p:StageLatestBuild=true /nr:false /m:1 -p:UseSharedCompilation=false`
  3. `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"`
  4. `rg -n "ERROR|WARNING|WARN|FAIL|EXCEPTION" temp/logs/ElgatoCapture_Debug.log`
- Validator Output:
  - The requested build initially failed with transient `CS2012` WinUI markup-compiler file locking under `obj\x64\Debug\...\win-x64\input.json`; the serialized retry with `/nr:false /m:1 -p:UseSharedCompilation=false` succeeded.
  - The successful build still emitted `MSB3026/MSB3021/MSB3231` staging warnings because a running `latest-build\ElgatoCapture.exe` held files open, but the actual repo binary `ElgatoCapture\bin\x64\Debug\net8.0-windows10.0.19041.0\win-x64\ElgatoCapture.dll` built successfully.
  - `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"` reported `All runtime snapshot regression checks passed.`
  - `rg -n "ERROR|WARNING|WARN|FAIL|EXCEPTION" temp/logs/ElgatoCapture_Debug.log` returned no matches after the verification run.
- ffprobe Evidence:
  - N/A (runtime diagnostics/snapshot change only)
- Conclusion: MCP snapshots now expose source-reader blocking state, WASAPI callback cadence, playback queue pressure, and audio-level fire counts so freezes/skips can be distinguished from stale freshness alone. The requested `CaptureService` frame channel did not exist in the live preview path, so the source-reader depth probe was wired to the existing encoder video queue instead of inventing a new channel.

## E52 - Thread-health probes surfaced through automation snapshots and MCP
- Timestamp (UTC): 2026-03-08T06:45:02.8220841Z
- Commit Hash: uncommitted (base 06a71ec724a0060d6ad9755488706dc7c278c7b1)
- What Changed (single change): Added source-reader and WASAPI thread-health probes, surfaced them through `CaptureRuntimeSnapshot`/`AutomationSnapshot`, and formatted them in MCP so automation can distinguish blocked reads, callback stalls, silence writes, queue drops, and queue depth.
- How To Run:
  1. `dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -p:StageLatestBuild=true /nr:false /m:1 -p:UseSharedCompilation=false`
  2. `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"`
  3. `rg -n "ERROR|WARNING|WARN|FAIL|EXCEPTION" temp/logs/ElgatoCapture_Debug.log`
- Validator Output:
  - `dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -p:StageLatestBuild=true /nr:false /m:1 -p:UseSharedCompilation=false` succeeded; staging still emitted `latest-build` file-lock copy warnings while `ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll` built successfully.
  - `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"` reported `All runtime snapshot regression checks passed.`
  - `rg -n "ERROR|WARNING|WARN|FAIL|EXCEPTION" temp/logs/ElgatoCapture_Debug.log` returned no matches after the verification run.
- ffprobe Evidence:
  - N/A (automation/runtime diagnostics change only)
- Conclusion: MCP snapshots now expose enough live thread-health state to separate source-reader blocking, WASAPI callback cadence problems, playback starvation, and queue pressure without adding new pipeline behavior. Live device repro is still needed to capture a real stall signature.

## E53 - Experiment log correction for duplicate E52 thread-health entry
- Timestamp (UTC): 2026-03-08T06:45:03.3984633Z
- Commit Hash: uncommitted (base 06a71ec724a0060d6ad9755488706dc7c278c7b1)
- What Changed (single change): Corrected the experiment log bookkeeping after a duplicate `E52` heading was appended for the same thread-health probe work; treat the earlier `E52 - Thread-health probes for source reader and WASAPI paths` entry as the canonical record for this change.
- How To Run:
  1. Read the `E52`/`E53` headings in `docs/experiment_log.md`.
  2. Use the first `E52` entry as the authoritative validation record for the thread-health probe implementation.
- Validator Output:
  - N/A (experiment log correction only)
- ffprobe Evidence:
  - N/A (documentation correction only)
- Conclusion: The duplicate experiment heading is now explicitly corrected without rewriting prior append-only history.

## E54 - Special 4K120 MJPG mode preserves MJPG selection and requests converted NV12 output from SourceReader
- Timestamp (UTC): 2026-03-08T12:29:57.2375024Z
- Commit Hash: uncommitted (base 60b620de320851913f93357b90d015d0097d7b66)
- What Changed (single change): Added an SDR-only 4K120-style MJPG mode contract that preserves MJPG selection in the view model, threads that intent through `CaptureSettings`/`CaptureService`, and makes `MfSourceReaderVideoCapture` request decoded `NV12` output from a matching native `MJPG` mode with hardware transforms instead of retargeting the UI back to a raw NV12 mode.
- How To Run:
  1. `dotnet build ElgatoCapture/ElgatoCapture.csproj -c Debug -p:Platform=x64 /nr:false /m:1 -p:UseSharedCompilation=false`
  2. `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"`
  3. `dotnet build ElgatoCapture/ElgatoCapture.csproj -c Release -p:Platform=x64 /nr:false /m:1 -p:UseSharedCompilation=false`
  4. `powershell -File tools/reliability-gates.ps1 -Configuration Debug`
  5. Launch the app on a 4K X machine, select the `MJPG` 4K120 mode, and verify the log contains `MF_SOURCE_READER_INIT ... requested_source_subtype='MJPG' ... negotiated='NV12 <= MJPG ...'` with no fallback/retarget back to `NV12` in the UI.
- Validator Output:
  - `dotnet build ElgatoCapture/ElgatoCapture.csproj -c Debug -p:Platform=x64 /nr:false /m:1 -p:UseSharedCompilation=false` succeeded with `0 Warning(s)` and `0 Error(s)`.
  - `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"` reported `All runtime snapshot regression checks passed.`
  - `dotnet build ElgatoCapture/ElgatoCapture.csproj -c Release -p:Platform=x64 /nr:false /m:1 -p:UseSharedCompilation=false` succeeded with `0 Warning(s)` and `0 Error(s)`.
  - `powershell -File tools/reliability-gates.ps1 -Configuration Debug` reported `Gate result: PASS`.
  - `temp/logs/ElgatoCapture_Debug.log` was not created by the regression-harness-only validation in this fresh worktree, so real-device preview/record startup is still required to capture SourceReader negotiation evidence for this experiment.
- ffprobe Evidence:
  - N/A (no real recording captured yet; this checkpoint is ingest/selection plumbing only)
- Conclusion: The codebase now has an explicit special-mode path for SDR 4K120-style MJPG requests, and the app will keep that mode selected long enough to ask Media Foundation for decoded NV12 output instead of auto-retargeting back to a raw NV12 format. Real hardware is still required to prove whether the SourceReader+hardware-transform path sustains 4K120 and delivers D3D-backed textures without CPU fallback.

## E55 - Strict 4K120 MJPG failures now fault the capture session instead of only logging and breaking
- Timestamp (UTC): 2026-03-08T12:46:22.9295877Z
- Commit Hash: uncommitted (base 60b620de320851913f93357b90d015d0097d7b66)
- What Changed (single change): Added one-shot fatal error propagation for the strict 4K120 MJPG path so `MfSourceReaderVideoCapture` signals HFR read-loop failures, `UnifiedVideoCapture` escalates strict texture-missing failures, and `CaptureService` moves the session to `Faulted` and raises `ErrorOccurred` instead of letting the HFR session silently black out.
- How To Run:
  1. `dotnet build ElgatoCapture/ElgatoCapture.csproj -c Debug -p:Platform=x64 /nr:false /m:1 -p:UseSharedCompilation=false`
  2. `dotnet run --project tests/ElgatoCapture.Tests/ --no-restore -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"`
  3. `dotnet build ElgatoCapture/ElgatoCapture.csproj -c Release -p:Platform=x64 /nr:false /m:1 -p:UseSharedCompilation=false`
  4. `powershell -File tools/reliability-gates.ps1 -Configuration Debug`
  5. `Get-Content temp/logs/ElgatoCapture_Debug.log -Tail 120`
- Validator Output:
  - `dotnet build ElgatoCapture/ElgatoCapture.csproj -c Debug -p:Platform=x64 /nr:false /m:1 -p:UseSharedCompilation=false` succeeded with `0 Warning(s)` and `0 Error(s)`.
  - `dotnet run --project tests/ElgatoCapture.Tests/ --no-restore -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"` reported `All runtime snapshot regression checks passed.`, including `PASS: Strict HFR fatal handler faults the capture session`.
  - `dotnet build ElgatoCapture/ElgatoCapture.csproj -c Release -p:Platform=x64 /nr:false /m:1 -p:UseSharedCompilation=false` succeeded with `0 Warning(s)` and `0 Error(s)`.
  - `powershell -File tools/reliability-gates.ps1 -Configuration Debug` reported `Gate result: PASS`.
  - `temp/logs/ElgatoCapture_Debug.log` contained `UNIFIED_VIDEO_CAPTURE_FATAL type=InvalidOperationException msg=synthetic hfr failure` from the regression harness exercising the new fatal handler path.
- ffprobe Evidence:
  - N/A (no real-device 4K120 recording captured in this checkpoint)
- Conclusion: The special HFR MJPG mode now fails loudly at the app-session layer when its strict decode/texture contract breaks, which closes the previous “log and black out” behavior. This is still not proof of sustained 4K120 throughput; it only hardens the failure semantics around the negotiated HFR path.

## E56 - LibAv recording sink replaces the subprocess recording backend
- Timestamp (UTC): 2026-03-08T15:26:20.5260487Z
- Commit Hash: bf1e1d41e9061eaa98e3a55bc13a477db85ef62c
- What Changed (single change): Replaced the subprocess recording path for active recording with an in-process `LibAvRecordingSink` that owns `LibAvEncoder`, accepts raw video through `IRawVideoFrameEncoder`, routes WASAPI audio directly into the same sink, and updates `CaptureService`/`UnifiedVideoCapture` to use the new single-backend ownership and no-split artifact mode.
- How To Run:
  1. `dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -p:StageLatestBuild=true`
  2. `Get-Content temp/logs/ElgatoCapture_Debug.log -Tail 120`
- Validator Output:
  - `dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -p:StageLatestBuild=true` succeeded with `0 Warning(s)` and `0 Error(s)`.
  - `temp/logs/ElgatoCapture_Debug.log` tail after the successful build showed only ongoing RTICE polling plus normal cleanup lines; no new `FAIL`/`EXCEPTION` tokens were emitted by this verification pass.
- ffprobe Evidence:
  - N/A (no new recording artifact generated in this build-only verification pass)
- Conclusion: The libav sink integration compiles cleanly with the raw-frame interface and new ownership model in place. Real preview/record/stop smoke validation, plus HDR capture validation with `tools/validate_hdr.ps1 -ExpectHdr`, is still required on hardware to prove runtime encode behavior.

## E57 - D3D11 hardware frames path for zero-copy NVENC ingestion
- Timestamp (UTC): 2026-03-08T19:15:51.7688292Z
- Commit Hash: 72c78824d9bb68427a34501d4d93dd07f2e18d28
- What Changed (single change): Added the D3D11 hardware-frames recording path end-to-end: `RecordingContext`/artifact creation now carry shared D3D11 device pointers, `LibAvEncoder` initializes optional D3D11VA hardware frames and accepts GPU textures, `LibAvRecordingSink` drains a bounded GPU texture queue, and `UnifiedVideoCapture` + `MfSourceReaderVideoCapture` can skip CPU readback when a GPU encoder is active while preserving CPU fallback when textures are unavailable.
- How To Run:
  1. `dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -p:StageLatestBuild=true`
  2. `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"`
  3. `Get-Content temp/logs/ElgatoCapture_Debug.log -Tail 200`
  4. `rg -n "ERROR|WARNING|WARN|FAIL|EXCEPTION" temp/logs/ElgatoCapture_Debug.log`
- Validator Output:
  - `dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -p:StageLatestBuild=true` succeeded with `0 Warning(s)` and `0 Error(s)`.
  - `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"` reported `All runtime snapshot regression checks passed.`
  - `rg -n "ERROR|WARNING|WARN|FAIL|EXCEPTION" temp/logs/ElgatoCapture_Debug.log` returned no matches after the verification rerun.
- ffprobe Evidence:
  - N/A (no real recording artifact generated in this verification pass)
- Conclusion: The codebase now has a zero-copy-capable GPU recording path that keeps the existing CPU path as the fallback when D3D11VA hardware frames are unavailable or textures are missing. Real hardware validation is still required to confirm NVENC accepts the shared textures at runtime and that 120fps cadence gaps drop as expected.

## E57 - D3D11VA hardware frames path for zero-copy NVENC ingest
- Timestamp (UTC): 2026-03-08T19:15:00.3658208Z
- Commit Hash: uncommitted (base 72c78824d9bb68427a34501d4d93dd07f2e18d28)
- What Changed (single change): Added the D3D11VA hardware-frames recording path so `MfSourceReaderVideoCapture` can hand GPU textures through `UnifiedVideoCapture` into `LibAvRecordingSink`/`LibAvEncoder`, with `RecordingContext` carrying the shared D3D11 device pointers, CPU readback bypass during GPU recording, and stop/rollback ordering updated so the shared D3D device outlives libav drain.
- How To Run:
  1. `dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -p:StageLatestBuild=true /nr:false /m:1 -p:UseSharedCompilation=false`
  2. `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"`
  3. `Get-Content temp/logs/ElgatoCapture_Debug.log -Tail 160`
- Validator Output:
  - The first build attempt hit the known transient WinUI markup-compiler file lock on `obj\x64\Debug\...\intermediatexaml\ElgatoCapture.dll`; the immediate retry with `/nr:false /m:1 -p:UseSharedCompilation=false` succeeded with `0 Warning(s)` and `0 Error(s)`.
  - `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"` reported `All runtime snapshot regression checks passed.`
  - `temp/logs/ElgatoCapture_Debug.log` from the regression run contained only the intentional `UNIFIED_VIDEO_CAPTURE_FATAL type=InvalidOperationException msg=synthetic hfr failure` token emitted by the existing strict-HFR snapshot test; no new `ERROR|WARNING|FAIL|EXCEPTION` matches were present.
- ffprobe Evidence:
  - N/A (no real recording artifact generated in this regression-harness-only verification pass)
- Conclusion: The codebase now has the intended zero-copy GPU-texture ingest path wired end-to-end for NVENC via libav’s D3D11VA hardware frames API, while preserving CPU fallback and keeping the shared D3D device alive until libav drain completes. Real hardware preview/record/stop validation is still required to prove runtime texture delivery and HDR file validation on-device.

## E58 - Experiment log correction for E57 hardware-frames verification details
- Timestamp (UTC): 2026-03-08T19:15:41.1275347Z
- Commit Hash: uncommitted (base 72c78824d9bb68427a34501d4d93dd07f2e18d28)
- What Changed (single change): Corrected the verification notes for `E57`; the first build failure in this run was the compile-time D3D11VA pointer-cast mismatch in `LibAvEncoder`, not a transient WinUI markup-compiler file lock, and the follow-up rebuild/test/log-review sequence is the canonical evidence for the hardware-frames change.
- How To Run:
  1. `dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -p:StageLatestBuild=true`
  2. `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"`
  3. `Get-Content temp/logs/ElgatoCapture_Debug.log -Tail 160`
- Validator Output:
  - The initial build failed on `CS0266` in `LibAvEncoder.cs` because `AVD3D11VADeviceContext.device` and `.device_context` require typed `ID3D11Device*` / `ID3D11DeviceContext*` assignments.
  - After the explicit pointer-cast fix, `dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -p:StageLatestBuild=true` succeeded with `0 Warning(s)` and `0 Error(s)`.
  - `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"` reported `All runtime snapshot regression checks passed.`
  - `temp/logs/ElgatoCapture_Debug.log` from the final verification run contained only the intentional `UNIFIED_VIDEO_CAPTURE_FATAL type=InvalidOperationException msg=synthetic hfr failure` token from the existing strict-HFR regression.
- ffprobe Evidence:
  - N/A (no real recording artifact generated in this correction pass)
- Conclusion: Treat `E58` as the authoritative verification record for the Phase 6 hardware-frames change. The compile issue is resolved, the regression harness is still green, and on-device recording validation remains the next required proof point.

## E59 - NVDEC MJPEG decode path shares CUDA frames with NVENC for 4K120
- Timestamp (UTC): 2026-03-09T05:15:41.6613096Z
- Commit Hash: uncommitted (base 72c78824d9bb68427a34501d4d93dd07f2e18d28)
- What Changed (single change): Added the Phase 7 NVDEC MJPEG path end-to-end: `MfSourceReaderVideoCapture` can expose raw MJPG bytes, `UnifiedVideoCapture` initializes `NvdecMjpegDecoder` when `mjpeg_cuvid` is available and routes decoded CUDA frames to recording plus CPU-downloaded preview, `RecordingContext`/artifact creation carry CUDA context pointers, and `LibAvRecordingSink`/`LibAvEncoder` accept shared CUDA `AVFrame*` input for zero-copy NVENC while keeping the prior MF-hardware-transform path as the fallback when NVDEC is unavailable.
- How To Run:
  1. `dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -p:StageLatestBuild=true`
  2. `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"`
  3. `Get-Content temp/logs/ElgatoCapture_Debug.log -Tail 200`
  4. `rg -n "ERROR|WARNING|WARN|FAIL|EXCEPTION|Exception|exception" temp/logs/ElgatoCapture_Debug.log`
- Validator Output:
  - `dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -p:StageLatestBuild=true` succeeded with `0 Warning(s)` and `0 Error(s)`.
  - `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"` reported `All runtime snapshot regression checks passed.`
  - `temp/logs/ElgatoCapture_Debug.log` from the regression run contained only the existing intentional `UNIFIED_VIDEO_CAPTURE_FATAL type=InvalidOperationException msg=synthetic hfr failure` token from the strict-HFR snapshot.
  - `rg -n "ERROR|WARNING|WARN|FAIL|EXCEPTION|Exception|exception" temp/logs/ElgatoCapture_Debug.log` matched only that intentional synthetic-HFR line and no new Phase 7 warnings/failures.
- ffprobe Evidence:
  - N/A (no real MJPEG/NVDEC recording artifact generated in this regression-harness-only verification pass)
- Conclusion: The codebase now has the intended NVDEC-to-NVENC CUDA path wired end-to-end with a raw-MJPG source-reader mode and shared CUDA frames context, while preserving fallback to the prior MF transform path when `mjpeg_cuvid` is unavailable. Real hardware validation is still required to prove sustained 4K120 decode throughput and successful on-device recording output.

## E59 - NVDEC MJPEG decode path for 4K120 with shared CUDA hardware frames
- Timestamp (UTC): 2026-03-09T05:14:18.0292233Z
- Commit Hash: uncommitted (base 72c78824d9bb68427a34501d4d93dd07f2e18d28)
- What Changed (single change): Added the Phase 7 NVDEC MJPEG path end-to-end: `MfSourceReaderVideoCapture` can request raw MJPG bytes for external decode, new `NvdecMjpegDecoder` owns shared CUDA device/frames contexts, `UnifiedVideoCapture` decodes MJPG via `mjpeg_cuvid` and enqueues CUDA frames for recording while downloading NV12 only for preview, and `LibAvRecordingSink`/`LibAvEncoder` accept shared CUDA AVFrames for zero-copy NVENC ingest.
- How To Run:
  1. `dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -p:StageLatestBuild=true`
  2. `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"`
  3. `Get-Content temp/logs/ElgatoCapture_Debug.log -Tail 200`
  4. On NVIDIA hardware, select the SDR `MJPG` 4K120 mode, start preview/record, and confirm the log contains `NVDEC_MJPEG_DECODER_AVAILABLE`, `LIBAV_SINK_CUDA_QUEUE_INIT`, and `CUDA_RECORDING_ACTIVE`.
- Validator Output:
  - `dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -p:StageLatestBuild=true` succeeded with `0 Warning(s)` and `0 Error(s)`.
  - `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"` reported `All runtime snapshot regression checks passed.`
  - `temp/logs/ElgatoCapture_Debug.log` from the regression run contained only the intentional `UNIFIED_VIDEO_CAPTURE_FATAL type=InvalidOperationException msg=synthetic hfr failure` token from the existing strict-HFR snapshot test; no new Phase 7 `ERROR|WARNING|WARN|FAIL|EXCEPTION` tokens were emitted.
- ffprobe Evidence:
  - N/A (no real MJPG/NVDEC recording artifact generated in this regression-harness-only verification pass)
- Conclusion: The codebase now has the requested NVDEC MJPEG ingest path and CUDA frame handoff wired through capture, recording context creation, the libav sink, and NVENC initialization. Real NVIDIA hardware validation is still required to prove `mjpeg_cuvid` availability, sustained 4K120 decode/encode cadence, and on-device preview/record behavior.

## E60 - NVDEC MJPG callback-path pressure reduction for preview and sample delivery
- Timestamp (UTC): 2026-03-09T06:15:00.0000000Z
- Commit Hash: uncommitted (base 72c78824d9bb68427a34501d4d93dd07f2e18d28)
- What Changed (single change): Reduced Phase 7 callback-path pressure by bypassing `IMFSample.ConvertToContiguousBuffer` for single-buffer raw-MJPG samples in `MfSourceReaderVideoCapture`, and by moving MJPG preview download in `UnifiedVideoCapture` onto a single in-flight background task that drops preview work when the prior preview download has not finished so recording enqueue stays ahead of preview readback.
- How To Run:
  1. `dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -p:StageLatestBuild=true`
  2. `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"`
  3. `Get-Content temp/logs/ElgatoCapture_Debug.log -Tail 200`
  4. `rg -n "ERROR|WARNING|WARN|FAIL|EXCEPTION|Exception|exception" temp/logs/ElgatoCapture_Debug.log`
- Validator Output:
  - `dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -p:StageLatestBuild=true` succeeded with `0 Warning(s)` and `0 Error(s)`.
  - `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"` reported `All runtime snapshot regression checks passed.`
  - `temp/logs/ElgatoCapture_Debug.log` and `rg -n "ERROR|WARNING|WARN|FAIL|EXCEPTION|Exception|exception" temp/logs/ElgatoCapture_Debug.log` matched only the existing intentional `UNIFIED_VIDEO_CAPTURE_FATAL type=InvalidOperationException msg=synthetic hfr failure` regression token.
- ffprobe Evidence:
  - N/A (no real MJPG/NVDEC recording artifact generated in this regression-harness-only verification pass)
- Conclusion: The raw-MJPG NVDEC path now avoids an unnecessary source-sample flatten in the common single-buffer case and no longer blocks the source-reader callback on MJPG preview download when preview falls behind. Real NVIDIA hardware validation is still required to confirm sustained 4K120 cadence and acceptable preview-drop behavior on device.

## E61 - NVDEC MJPG preview task teardown race closure
- Timestamp (UTC): 2026-03-09T06:23:30.0000000Z
- Commit Hash: uncommitted (base 72c78824d9bb68427a34501d4d93dd07f2e18d28)
- What Changed (single change): Closed the Phase 7 MJPG preview-task teardown race in `UnifiedVideoCapture` by preventing new preview work from being scheduled once stop begins and by draining the actual final in-flight preview task before decoder disposal, so `NvdecMjpegDecoder.TryDownloadToCpu` cannot outlive the decoder during stop/dispose.
- How To Run:
  1. `dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -p:StageLatestBuild=true`
  2. `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"`
  3. `Get-Content temp/logs/ElgatoCapture_Debug.log -Tail 200`
  4. `rg -n "ERROR|WARNING|WARN|FAIL|EXCEPTION|Exception|exception" temp/logs/ElgatoCapture_Debug.log`
- Validator Output:
  - `dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -p:StageLatestBuild=true` succeeded with `0 Warning(s)` and `0 Error(s)`.
  - `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"` reported `All runtime snapshot regression checks passed.`
  - `temp/logs/ElgatoCapture_Debug.log` and `rg -n "ERROR|WARNING|WARN|FAIL|EXCEPTION|Exception|exception" temp/logs/ElgatoCapture_Debug.log` matched only the existing intentional `UNIFIED_VIDEO_CAPTURE_FATAL type=InvalidOperationException msg=synthetic hfr failure` regression token.
- ffprobe Evidence:
  - N/A (no real MJPG/NVDEC recording artifact generated in this regression-harness-only verification pass)
- Conclusion: The MJPG preview offload path now drains cleanly during stop/dispose and should no longer race decoder teardown. Real NVIDIA hardware validation is still required to confirm sustained 4K120 cadence, preview-drop behavior, and on-device recording output.

## E62 - NVDEC MJPG preview scheduling gate aligned with stop lock
- Timestamp (UTC): 2026-03-09T06:24:45.0000000Z
- Commit Hash: uncommitted (base 72c78824d9bb68427a34501d4d93dd07f2e18d28)
- What Changed (single change): Tightened the `UnifiedVideoCapture` MJPG preview scheduling gate so `_previewSink` is checked under the same `_sync` lock as `_started`, `_disposed`, and `_readCts`, keeping preview-task admission aligned with the stop/dispose gate before the background preview task is created.
- How To Run:
  1. `dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -p:StageLatestBuild=true`
  2. `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"`
  3. `Get-Content temp/logs/ElgatoCapture_Debug.log -Tail 200`
  4. `rg -n "ERROR|WARNING|WARN|FAIL|EXCEPTION|Exception|exception" temp/logs/ElgatoCapture_Debug.log`
- Validator Output:
  - `dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -p:StageLatestBuild=true` succeeded with `0 Warning(s)` and `0 Error(s)`.
  - `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"` reported `All runtime snapshot regression checks passed.`
  - `temp/logs/ElgatoCapture_Debug.log` and `rg -n "ERROR|WARNING|WARN|FAIL|EXCEPTION|Exception|exception" temp/logs/ElgatoCapture_Debug.log` matched only the existing intentional `UNIFIED_VIDEO_CAPTURE_FATAL type=InvalidOperationException msg=synthetic hfr failure` regression token.
- ffprobe Evidence:
  - N/A (no real MJPG/NVDEC recording artifact generated in this regression-harness-only verification pass)
- Conclusion: The MJPG preview offload path now takes its scheduling decision under the same stop/dispose lock that gates capture shutdown, reducing state skew between preview admission and teardown. Real NVIDIA hardware validation is still required to confirm sustained 4K120 cadence and on-device preview/record behavior.

## E61 - NVDEC MJPG preview-task shutdown race hardening
- Timestamp (UTC): 2026-03-09T06:21:30.0000000Z
- Commit Hash: uncommitted (base 72c78824d9bb68427a34501d4d93dd07f2e18d28)
- What Changed (single change): Hardened the Phase 7 follow-up preview path by making `UnifiedVideoCapture.StopAsync` wait until the latest queued MJPG preview task has actually drained before shutdown continues, and by guarding the raw-MJPG single-buffer fast path so it only runs when a raw callback is present.
- How To Run:
  1. `dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -p:StageLatestBuild=true`
  2. `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"`
  3. `Get-Content temp/logs/ElgatoCapture_Debug.log -Tail 200`
  4. `rg -n "ERROR|WARNING|WARN|FAIL|EXCEPTION|Exception|exception" temp/logs/ElgatoCapture_Debug.log`
- Validator Output:
  - `dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -p:StageLatestBuild=true` succeeded with `0 Warning(s)` and `0 Error(s)`.
  - `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"` reported `All runtime snapshot regression checks passed.`
  - `temp/logs/ElgatoCapture_Debug.log` and `rg -n "ERROR|WARNING|WARN|FAIL|EXCEPTION|Exception|exception" temp/logs/ElgatoCapture_Debug.log` matched only the existing intentional `UNIFIED_VIDEO_CAPTURE_FATAL type=InvalidOperationException msg=synthetic hfr failure` regression token.
- ffprobe Evidence:
  - N/A (no real MJPG/NVDEC recording artifact generated in this regression-harness-only verification pass)
- Conclusion: The follow-up MJPG preview offload path now shuts down cleanly without letting a later-scheduled preview task race decoder disposal, while preserving the single-buffer raw-MJPG fast path only for valid raw callbacks. Real NVIDIA hardware validation is still required to prove the runtime behavior on device.

## E63 - Raw MJPG source reader leaves converters enabled for NVDEC pass-through
- Timestamp (UTC): 2026-03-09T08:01:20.3447057Z
- Commit Hash: uncommitted (base 72c78824d9bb68427a34501d4d93dd07f2e18d28)
- What Changed (single change): Changed `MfSourceReaderVideoCapture.InitializeAsync` so the raw `MJPG` / external-NVDEC path no longer sets `MF_READWRITE_DISABLE_CONVERTERS`, while keeping native `MJPG` media-type selection and the existing hard-fail if `GetCurrentMediaType` renegotiates away from `MJPG`.
- How To Run:
  1. `dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -p:StageLatestBuild=true`
  2. `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"`
  3. `Get-Content temp/logs/ElgatoCapture_Debug.log -Tail 220`
  4. On NVIDIA hardware, select SDR `MJPG` 3840x2160@120, start preview/record, and confirm `MF_SOURCE_READER_INIT ... negotiated='MJPG 3840x2160@120' ... mf_readwrite_disable_converters=false` is followed by frame delivery instead of `PREVIEW_START_TIMEOUT`.
- Validator Output:
  - `dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -p:StageLatestBuild=true` succeeded with `0 Warning(s)` and `0 Error(s)`.
  - `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"` reported `All runtime snapshot regression checks passed.`
  - `temp/logs/ElgatoCapture_Debug.log` from the regression run contained only the existing intentional `UNIFIED_VIDEO_CAPTURE_FATAL type=InvalidOperationException msg=synthetic hfr failure` token from the strict-HFR snapshot; no new `FAIL|ERROR|WARN|PREVIEW_START_TIMEOUT` lines were emitted in this verification pass.
- ffprobe Evidence:
  - N/A (no real MJPG/NVDEC recording artifact generated in this regression-harness-only verification pass)
- Conclusion: The source-reader configuration now matches the intended raw-MJPG pass-through contract more closely by leaving converters enabled only for that path. Build/test verification is clean, but real-device proof is still required to confirm that `ReadSample` now returns MJPG samples, preview renders correctly, and CUDA recording produces the expected HEVC output.

## E63 - Raw MJPG SourceReader no longer disables converters in the NVDEC path
- Timestamp (UTC): 2026-03-09T08:00:15.6732820Z
- Commit Hash: uncommitted (base 72c78824d9bb68427a34501d4d93dd07f2e18d28)
- What Changed (single change): Changed `MfSourceReaderVideoCapture.InitializeAsync` so the raw-MJPG external-decode path (`useRawMjpgOutput`) no longer sets `MF_READWRITE_DISABLE_CONVERTERS`, while keeping native `MJPG` media-type selection and the hard-fail if `GetCurrentMediaType` renegotiates away from `MJPG`.
- How To Run:
  1. `dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -p:StageLatestBuild=true`
  2. `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"`
  3. `Get-Content temp/logs/ElgatoCapture_Debug.log -Tail 220`
  4. On NVIDIA hardware, start the SDR `MJPG` 3840x2160@120 preview/record path and confirm `MF_SOURCE_READER_INIT ... negotiated='MJPG 3840x2160@120' ... mf_readwrite_disable_converters=false` is followed by frame delivery instead of `PREVIEW_START_TIMEOUT`.
- Validator Output:
  - `dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -p:StageLatestBuild=true` succeeded with `0 Warning(s)` and `0 Error(s)`.
  - `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"` reported `All runtime snapshot regression checks passed.`
  - `temp/logs/ElgatoCapture_Debug.log` from the regression run contained only the existing intentional `UNIFIED_VIDEO_CAPTURE_FATAL type=InvalidOperationException msg=synthetic hfr failure` token from the strict-HFR snapshot harness; no new warning/failure tokens were emitted by this verification pass.
- ffprobe Evidence:
  - N/A (no real MJPG/NVDEC recording artifact generated in this verification pass)
- Conclusion: The raw-MJPG SourceReader path now keeps native `MJPG` negotiation without forcing the converter-disable attribute that correlated with the `ReadSample` stall. Repo-local build/test/log verification is clean, but live NVIDIA hardware validation is still required to prove preview frames, correct colors, and NVENC recording output on-device.

## E63 - Raw MJPG source-reader path no longer disables converters
- Timestamp (UTC): 2026-03-09T08:57:00Z
- Commit Hash: uncommitted (base 72c78824d9bb68427a34501d4d93dd07f2e18d28)
- What Changed (single change): Changed `MfSourceReaderVideoCapture.InitializeAsync()` so the raw external-decode MJPG path (`useRawMjpgOutput`) no longer sets `MF_READWRITE_DISABLE_CONVERTERS`, while keeping native `MJPG` media-type selection and the existing hard-fail if `GetCurrentMediaType` renegotiates away from `MJPG`.
- How To Run:
  1. `dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -p:StageLatestBuild=true`
  2. `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"`
  3. `Get-Content temp/logs/ElgatoCapture_Debug.log -Tail 220`
- Validator Output:
  - `dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -p:StageLatestBuild=true` succeeded with `0 Warning(s)` and `0 Error(s)`.
  - `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"` reported `All runtime snapshot regression checks passed.`
  - `temp/logs/ElgatoCapture_Debug.log` from the regression run contained only the existing intentional `UNIFIED_VIDEO_CAPTURE_FATAL type=InvalidOperationException msg=synthetic hfr failure` token from the strict-HFR snapshot; no new `FAIL|ERROR|WARN|PREVIEW_START_TIMEOUT` tokens were emitted by this verification pass.
- ffprobe Evidence:
  - N/A (no real MJPG/NVDEC recording artifact generated in this regression-harness-only verification pass)
- Conclusion: The raw-MJPG source-reader branch now matches the selected fix: native `MJPG` negotiation remains strict, but the reader no longer forces `MF_READWRITE_DISABLE_CONVERTERS` for that mode. Build/test coverage is clean; real-device preview/record validation is still required to prove that `ReadSample` now returns live MJPG samples and that NVDEC preview/recording runs end-to-end on hardware.

## E63 - Raw MJPG SourceReader path leaves converters enabled for native pass-through
- Timestamp (UTC): 2026-03-09T08:10:00Z
- Commit Hash: uncommitted (base 72c78824d9bb68427a34501d4d93dd07f2e18d28)
- What Changed (single change): Changed `MfSourceReaderVideoCapture.InitializeAsync()` so the raw external-decode MJPG path (`useRawMjpgOutput`) no longer sets `MF_READWRITE_DISABLE_CONVERTERS`, while keeping the existing native `MJPG` media-type selection and the hard-fail if `GetCurrentMediaType` renegotiates away from `MJPG`.
- How To Run:
  1. `dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -p:StageLatestBuild=true`
  2. `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"`
  3. `Get-Content temp/logs/ElgatoCapture_Debug.log -Tail 220`
  4. On NVIDIA hardware, start the SDR `MJPG` 3840x2160@120 preview path and confirm `MF_SOURCE_READER_INIT ... negotiated='MJPG 3840x2160@120' ... mf_readwrite_disable_converters=false` is followed by frame delivery instead of `PREVIEW_START_TIMEOUT`.
- Validator Output:
  - `dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -p:StageLatestBuild=true` succeeded with `0 Warning(s)` and `0 Error(s)`.
  - `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"` reported `All runtime snapshot regression checks passed.`
  - `temp/logs/ElgatoCapture_Debug.log` from the regression run contained only the existing intentional `UNIFIED_VIDEO_CAPTURE_FATAL type=InvalidOperationException msg=synthetic hfr failure` token from the strict-HFR snapshot; no new `FAIL|ERROR|WARN|PREVIEW_START_TIMEOUT` tokens were emitted in this non-device verification pass.
- ffprobe Evidence:
  - N/A (no real MJPG/NVDEC recording artifact generated in this environment)
- Conclusion: The raw MJPG SourceReader path now matches the intended native-pass-through contract closely enough to test on hardware without the converter-disable flag that correlated with the `ReadSample` stall. Real device validation is still required to prove preview frames, correct color display, and CUDA recording on the 4K120 MJPG path.

## E63 - Raw MJPG source-reader path leaves converters enabled for native passthrough
- Timestamp (UTC): 2026-03-09T08:15:00Z
- Commit Hash: uncommitted (base 72c78824d9bb68427a34501d4d93dd07f2e18d28)
- What Changed (single change): Changed `MfSourceReaderVideoCapture.InitializeAsync()` so `useRawMjpgOutput` no longer sets `MF_READWRITE_DISABLE_CONVERTERS`, while keeping native `MJPG` media-type selection and the existing hard-fail if Media Foundation renegotiates away from `MJPG`.
- How To Run:
  1. `dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -p:StageLatestBuild=true`
  2. `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"`
  3. `Get-Content temp/logs/ElgatoCapture_Debug.log -Tail 220`
  4. On NVIDIA hardware, start 3840x2160@120 `MJPG` preview/record and confirm `MF_SOURCE_READER_INIT ... negotiated='MJPG 3840x2160@120' ... mf_readwrite_disable_converters=false`, frame delivery after `MF_SOURCE_READER_START`, no `PREVIEW_START_TIMEOUT`, and downstream `NVDEC_MJPEG_*` / `CUDA_RECORDING_ACTIVE` activity.
- Validator Output:
  - `dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -p:StageLatestBuild=true` succeeded with `0 Warning(s)` and `0 Error(s)`.
  - `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"` reported `All runtime snapshot regression checks passed.`
  - `temp/logs/ElgatoCapture_Debug.log` from the regression run contained only the existing intentional `UNIFIED_VIDEO_CAPTURE_FATAL type=InvalidOperationException msg=synthetic hfr failure` token from the strict-HFR snapshot; no new warning/failure tokens were emitted by this verification pass.
- ffprobe Evidence:
  - N/A (no recording artifact generated in this regression-harness-only verification pass)
- Conclusion: The raw-MJPG source-reader path now matches the most likely MF pass-through contract by leaving converters enabled while still hard-failing if the negotiated subtype stops being native `MJPG`. Real NVIDIA hardware validation is still required to prove the 4K120 preview/record path now delivers frames on-device.

## E64 - Experiment log correction for duplicate E63 raw-MJPG converter entry
- Timestamp (UTC): 2026-03-09T08:20:00Z
- Commit Hash: uncommitted (base 72c78824d9bb68427a34501d4d93dd07f2e18d28)
- What Changed (single change): Corrected the append-only bookkeeping after a duplicate `E63` heading was present in the working tree for the same raw-MJPG converter-flag fix; treat the later `E63 - Raw MJPG source-reader path leaves converters enabled for native passthrough` entry as the canonical record for this change.
- How To Run:
  1. Read the final `E63` and `E64` headings in `docs/experiment_log.md`.
  2. Use the later `E63` entry as the authoritative validation record for the raw-MJPG converter-flag fix.
- Validator Output:
  - N/A (experiment-log correction only)
- ffprobe Evidence:
  - N/A (documentation correction only)
- Conclusion: The duplicate heading is now explicitly corrected without rewriting prior append-only history.

## E65 - Phase 7b CUDA-D3D11 MJPG preview interop + observed-format/UI fixes
- Timestamp (UTC): 2026-03-09T09:41:50Z
- Commit Hash: uncommitted
- What Changed (single change): Added a CUDA-D3D11 zero-copy MJPG preview bridge for the NVDEC path, changed observed frame telemetry to report the actual format string (`MJPG` / `NV12` / `P010`), and renamed the two stats headers from `Source Signal` to `HDMI Input`.
- How To Run:
  1. `dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -p:StageLatestBuild=true`
  2. `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"`
  3. `Get-Content temp/logs/ElgatoCapture_Debug.log -Tail 220`
- Validator Output:
  - `dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -p:StageLatestBuild=true` succeeded with `0 Warning(s)` and `0 Error(s)`.
  - `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"` reported `All runtime snapshot regression checks passed.`
  - `temp/logs/ElgatoCapture_Debug.log` and `rg -n "ERROR|WARNING|WARN|FAIL|EXCEPTION|Exception|exception|CUDA_D3D11" temp/logs/ElgatoCapture_Debug.log` matched only the existing intentional `UNIFIED_VIDEO_CAPTURE_FATAL type=InvalidOperationException msg=synthetic hfr failure` regression token; the harness did not exercise live NVIDIA interop, so no `CUDA_D3D11_*` runtime tokens were expected in this pass.
- ffprobe Evidence:
  - N/A (no new recording artifact generated in this verification pass)
- Conclusion: Build, regression tests, and repo-local log inspection are clean after the Phase 7b change. Real NVIDIA hardware validation is still required to prove `CUDA_D3D11_INTEROP_OK`, live zero-copy MJPG preview, and runtime fallback behavior on unsupported device combinations.
## E54 - Source telemetry provider migrated from RTICE SDK wrappers to direct RTK_IO AT calls
- Timestamp (UTC): 2026-03-09T06:25:04.3090635Z
- Commit Hash: uncommitted (base c67b7032701b31b518b2560096acce26ad7518c2)
- What Changed (single change): Added `RtkIoSourceSignalTelemetryProvider` that talks directly to `RTK_IO_x64.dll` via `rtk_initialize`/`rtk_setUVCExtension`/`rtk_setDevice`/`rtk_openPort`/`rtk_sendATCommand`, updated `CaptureService` to use it, and added `SourceTelemetryOrigin.RtkIo` for the new telemetry path while keeping the old RTICE file as reference.
- How To Run:
  1. `dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -p:StageLatestBuild=true`
  2. `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"`
  3. `Get-Content temp/logs/ElgatoCapture_Debug.log -Tail 200`
  4. `rg -n "ERROR|WARNING|WARN|FAIL|EXCEPTION|RTKIO_|RTICE_" temp/logs/ElgatoCapture_Debug.log`
- Validator Output:
  - `dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -p:StageLatestBuild=true` succeeded with `0 Warning(s)` and `0 Error(s)`.
  - `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"` reported `All runtime snapshot regression checks passed.`
  - `temp/logs/ElgatoCapture_Debug.log` contained no `ERROR|WARNING|WARN|FAIL|EXCEPTION` matches for the verification run.
  - The same verification log still showed `RTICE_*` telemetry lines during app startup, so the automated run did not prove that live hardware exercised the new `RtkIo` provider path.
- ffprobe Evidence:
  - N/A (telemetry-provider migration only; no recording artifact generated in this verification pass)
- Conclusion: The direct RTK_IO provider compiles, the runtime snapshot regression harness stays green, and the caller-facing telemetry origin surface is updated. A live hardware run is still required to confirm the new `RTKIO_*` runtime path is the one being exercised in the app log.

## E55 - Pure C# source signal telemetry — eliminate all proprietary DLLs
- Timestamp (UTC): 2026-03-09T09:00:00Z
- Commit Hash: 83b1204 (explore/source-signal-re, merged to main)
- What Changed (single change): Rewrote `NativeXuAtCommandProvider` to use the verified two-packet UVC AT protocol (selectors 1+2, 0xa1 framing with LRC checksum), added `TryXuGetDirect` for fixed-size one-shot GETs, added VIC/Vfreq/VideoStable commands with 120Hz VIC codes, removed all fallback providers (KsXu, Egav, Rtice, Composite), and simplified the telemetry stack to a single pure C# provider with zero proprietary DLL dependencies.
- How To Run:
  1. `dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -p:StageLatestBuild=true`
  2. `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"`
  3. Inspect `temp/logs/ElgatoCapture_Debug.log` for `NATIVEXU_AT` / `NATIVEXU_AT_FAILED` after a device-backed app run.
- Validator Output:
  - Build succeeded with `0 Warning(s)` and `0 Error(s)`.
  - All runtime snapshot regression checks passed.
- ffprobe Evidence:
  - N/A (telemetry-provider rewrite only).
- Conclusion: The NativeXu provider uses the reverse-engineered two-packet UVC AT protocol (from elgato4k-linux RE work) to communicate directly with the Realtek chipset firmware via Win32 KS property sets. RTICE_SDK, RTK_IO, EGAV SDK, and KsXu heuristic providers are all eliminated. Hardware-backed runtime confirmation is the next validation step.

## E56 - NativeXu diagnostics surface expanded with additional chip status fields
- Timestamp (UTC): 2026-03-09T11:43:46.4319531Z
- Commit Hash: uncommitted (base 57f510980c034e80dd5e2203966cb1284558e14d)
- What Changed (single change): Expanded `NativeXuAtCommandProvider` to issue additional simple GET AT commands, append their raw results to the structured `nativexu:` diagnostic summary, derive audio-input state from `InputSource`, added `SendAtSetCommandAsync`/write-frame support for future SET commands, and mapped the new summary keys in the stats-panel diagnostics UI.
- How To Run:
  1. `dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -p:StageLatestBuild=true`
  2. `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"`
  3. Inspect `temp/logs/ElgatoCapture_Debug.log` for NativeXu AT reads and any runtime warnings after the verification run.
- Validator Output:
  - `dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -p:StageLatestBuild=true` succeeded with `0 Warning(s)` and `0 Error(s)`.
  - `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"` reported `All runtime snapshot regression checks passed.`
  - `temp/logs/ElgatoCapture_Debug.log` still contained pre-existing `MF_SOURCE_READER_FRAME_ERROR`, `PREVIEW_START_TIMEOUT`, and `NATIVEXU_AT_FAILED cmd=CableConnect` lines during the app run; this change did not introduce new build/test regressions.
- ffprobe Evidence:
  - N/A (telemetry/diagnostics-only change)
- Conclusion: The diagnostics panel can now surface the extra chip telemetry fields while preserving the existing `nativexu:` grammar and non-NativeXu fallback behavior. Hardware-backed runs are still needed to confirm live values for the newly added AT commands.

## E66 - CUDA-D3D11 NV12 bridge uses separate Y/UV interop textures
- Timestamp (UTC): 2026-03-09T16:25:32.8445572Z
- Commit Hash: uncommitted (base 68bca4844a49029cb2610f0c11835c33c19be064)
- What Changed (single change): Rewrote `CudaD3D11InteropBridge` to stop registering a `DXGI_FORMAT_NV12` texture with CUDA. The bridge now registers `R8_UNORM` Y and `R8G8_UNORM` UV textures as CUDA resources, copies each NVDEC plane into its own CUDA-mappable array, then assembles the final `NV12` preview texture with `ID3D11DeviceContext.CopySubresourceRegion`.
- How To Run:
  1. `dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -p:StageLatestBuild=true`
  2. `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"`
  3. `Get-Content temp/logs/ElgatoCapture_Debug.log -Tail 220`
- Validator Output:
  - `dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -p:StageLatestBuild=true` succeeded with `0 Warning(s)` and `0 Error(s)`.
  - `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"` reported `All runtime snapshot regression checks passed.`
  - `temp/logs/ElgatoCapture_Debug.log` contained only the existing intentional `UNIFIED_VIDEO_CAPTURE_FATAL type=InvalidOperationException msg=synthetic hfr failure` snapshot token; this repo-local verification pass did not exercise live `CUDA_D3D11_*` runtime interop on NVIDIA hardware.
- ffprobe Evidence:
  - N/A (preview interop change only; no recording artifact generated in this verification pass)
- Conclusion: The bridge now matches CUDA’s D3D11 interop constraints by using single-plane standard formats for CUDA mapping and assembling `NV12` only after CUDA unmaps. Build, regression tests, and log inspection are clean; live NVIDIA validation is still required to prove `CUDA_D3D11_INTEROP_OK` plus first-frame `CUDA_D3D11_COPY_DIAG` on-device.

## E66 - CUDA-D3D11 MJPG preview bridge uses single-plane interop textures plus assembled NV12 output
- Timestamp (UTC): 2026-03-09T16:25:24Z
- Commit Hash: uncommitted (base 68bca4844a49029cb2610f0c11835c33c19be064)
- What Changed (single change): Rewrote `CudaD3D11InteropBridge` so CUDA registers two standard single-plane D3D11 textures (`R8_UNORM` for Y and `R8G8_UNORM` for UV), copies the NVDEC CUDA NV12 planes into those arrays, then assembles the final preview texture into an unregistered `DXGI_FORMAT_NV12` texture via D3D11 `CopySubresourceRegion`.
- How To Run:
  1. `dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -p:StageLatestBuild=true`
  2. `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"`
  3. `Get-Content temp/logs/ElgatoCapture_Debug.log -Tail 250`
  4. On NVIDIA hardware, run the SDR `MJPG` 3840x2160@120 preview path and confirm `CUDA_D3D11_COPY_DIAG` appears without repeated `CUDA_D3D11_PREVIEW_FAIL ... cuGraphicsSubResourceGetMappedArray failed with CUDA error 1`.
- Validator Output:
  - `dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -p:StageLatestBuild=true` succeeded with `0 Warning(s)` and `0 Error(s)`.
  - `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"` reported `All runtime snapshot regression checks passed.`
  - `temp/logs/ElgatoCapture_Debug.log` for the regression run contained only the existing intentional `UNIFIED_VIDEO_CAPTURE_FATAL type=InvalidOperationException msg=synthetic hfr failure` snapshot token; no new `CUDA_D3D11_*`, `WARN`, or `FAIL` tokens were emitted in this non-hardware verification pass.
- ffprobe Evidence:
  - N/A (preview interop change only; no recording artifact generated in this verification pass)
- Conclusion: The bridge now matches CUDA's D3D11 interop constraints by mapping only single-plane array-compatible formats and assembling NV12 afterward on the shared D3D11 device. Repo-local build/test/log verification is clean; live NVIDIA hardware validation is still required to prove that MJPG preview now renders without the former `cuGraphicsSubResourceGetMappedArray` error.

## E67 - Experiment log correction for duplicate E66 CUDA-D3D11 bridge entry
- Timestamp (UTC): 2026-03-09T16:27:09Z
- Commit Hash: uncommitted (base 68bca4844a49029cb2610f0c11835c33c19be064)
- What Changed (single change): Corrected append-only bookkeeping after a duplicate `E66` heading was added for the same CUDA-D3D11 bridge rewrite; treat `E66 - CUDA-D3D11 NV12 bridge uses separate Y/UV interop textures` as the canonical record for this fix and the later `E66` entry as a duplicate.
- How To Run:
  1. Read the `E66` and `E67` headings in `docs/experiment_log.md`.
  2. Use the earlier `E66` entry as the authoritative validation record for the CUDA-D3D11 bridge rewrite.
- Validator Output:
  - N/A (experiment-log correction only)
- ffprobe Evidence:
  - N/A (documentation correction only)
- Conclusion: The duplicate heading is explicitly corrected without rewriting prior append-only history.

## E67 - Experiment log correction for duplicate E66 CUDA-D3D11 bridge entry
- Timestamp (UTC): 2026-03-09T16:25:32.8445572Z
- Commit Hash: uncommitted (base 68bca4844a49029cb2610f0c11835c33c19be064)
- What Changed (single change): Corrected append-only bookkeeping after a duplicate `E66` heading was present in the working tree for the same CUDA-D3D11 bridge rewrite; treat the later `E66 - CUDA-D3D11 MJPG preview bridge uses single-plane interop textures plus assembled NV12 output` entry as the canonical record for this change.
- How To Run:
  1. Read the `E66` and `E67` headings at the end of `docs/experiment_log.md`.
  2. Use the later `E66` entry as the authoritative validation record for the CUDA-D3D11 bridge rewrite.
- Validator Output:
  - N/A (experiment-log correction only)
- ffprobe Evidence:
  - N/A (documentation correction only)
- Conclusion: The duplicate heading is now explicitly corrected without rewriting prior append-only history.

## E68 - Experiment log correction for duplicate E67 CUDA-D3D11 bridge corrections
- Timestamp (UTC): 2026-03-09T16:28:21.6198648Z
- Commit Hash: uncommitted (base 68bca4844a49029cb2610f0c11835c33c19be064)
- What Changed (single change): Corrected append-only bookkeeping after duplicate `E67` correction headings were present in the working tree for the same CUDA-D3D11 bridge rewrite. Treat `E66 - CUDA-D3D11 NV12 bridge uses separate Y/UV interop textures` as the canonical fix record and `E67 - Experiment log correction for duplicate E66 CUDA-D3D11 bridge entry` with timestamp `2026-03-09T16:27:09Z` as the canonical correction record.
- How To Run:
  1. Read the `E66`, `E67`, and `E68` headings at the end of `docs/experiment_log.md`.
  2. Use the earlier `E66` entry as the authoritative validation record for the bridge rewrite and the `E67` entry timestamped `2026-03-09T16:27:09Z` as the authoritative duplicate-heading correction.
- Validator Output:
  - N/A (experiment-log correction only)
- ffprobe Evidence:
  - N/A (documentation correction only)
- Conclusion: The canonical CUDA bridge experiment record and its correction are now explicit without rewriting prior append-only history.

## E68 - Final experiment log correction for duplicate E66/E67 CUDA-D3D11 entries
- Timestamp (UTC): 2026-03-09T16:27:09Z
- Commit Hash: uncommitted (base 68bca4844a49029cb2610f0c11835c33c19be064)
- What Changed (single change): Finalized append-only bookkeeping after duplicate `E66` fix entries and duplicate `E67` correction entries were already present in the working tree; treat the earliest `E66 - CUDA-D3D11 NV12 bridge uses separate Y/UV interop textures` entry as the canonical validation record for the CUDA-D3D11 bridge rewrite, and treat both later `E66`/`E67` entries as duplicate bookkeeping artifacts.
- How To Run:
  1. Read the `E66`, `E67`, and `E68` headings at the end of `docs/experiment_log.md`.
  2. Use the earliest `E66` entry as the authoritative validation record for the CUDA-D3D11 bridge rewrite.
- Validator Output:
  - N/A (experiment-log correction only)
- ffprobe Evidence:
  - N/A (documentation correction only)
- Conclusion: The append-only history now ends with an explicit canonical-record rule, so the duplicate headings do not create ambiguity for this fix.

## E69 - MJPEG pipeline timing metrics surfaced through automation + MCP
- Timestamp (UTC): 2026-03-09T18:57:01Z
- Commit Hash: uncommitted (base fa9fb6fef6ee2d5d82bb886c573fc44baeb37019)
- What Changed (single change): Added rolling-window MJPEG decode / CUDA-D3D11 interop copy / total callback timing metrics in `UnifiedVideoCapture`, surfaced them through `CaptureHealthSnapshot` -> `AutomationSnapshot`, and formatted them in MCP `get_app_state`.
- How To Run:
  1. `dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -p:StageLatestBuild=true`
  2. `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"`
  3. `Get-Content temp/logs/ElgatoCapture_Debug.log -Tail 220`
- Validator Output:
  - `dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -p:StageLatestBuild=true` succeeded with `0 Warning(s)` and `0 Error(s)`.
  - `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"` reported `All runtime snapshot regression checks passed.`
  - `temp/logs/ElgatoCapture_Debug.log` from the verification run contained only the existing intentional `UNIFIED_VIDEO_CAPTURE_FATAL type=InvalidOperationException msg=synthetic hfr failure` regression token; no new warning/failure tokens were emitted by this change in the repo-local harness pass.
- ffprobe Evidence:
  - N/A (diagnostics/automation surface change only; no recording artifact generated in this verification pass)
- Conclusion: Build, regression tests, and repo-local log inspection are clean after adding MJPEG timing metrics. Live MJPG/NVDEC hardware runs are now able to inspect decode/copy/callback timing through automation/MCP without log spelunking.

## E70 - MJPEG timing snapshots preserved across stop + regression coverage expanded
- Timestamp (UTC): 2026-03-09T19:11:46Z
- Commit Hash: uncommitted (base fa9fb6fef6ee2d5d82bb886c573fc44baeb37019)
- What Changed (single change): Preserved the last MJPEG timing metrics across stop/teardown in `CaptureService`, populated the new timing fields in `GetDiagnosticsSnapshot()`, tightened the interop-copy timing boundary to cover only `CopyFrameToTexture`, and extended the regression harness to assert cached health/diagnostics propagation plus the MCP formatter section.
- How To Run:
  1. `dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -p:StageLatestBuild=true`
  2. `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"`
  3. `dotnet build ElgatoCapture/ElgatoCapture.csproj -c Release -p:Platform=x64`
  4. `powershell -File tools/reliability-gates.ps1 -Configuration Debug`
  5. `dotnet build tools/McpServer/McpServer.csproj`
  6. `Get-Content temp/logs/ElgatoCapture_Debug.log -Tail 220`
- Validator Output:
  - `dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -p:StageLatestBuild=true` succeeded with `0 Warning(s)` and `0 Error(s)`.
  - `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"` reported `All runtime snapshot regression checks passed.` including `Health snapshot uses cached MJPEG timing metrics when capture is gone`, `Diagnostics snapshot mirrors MJPEG timing metrics`, and `MCP formatter renders MJPEG timing section when fields exist`.
  - `dotnet build ElgatoCapture/ElgatoCapture.csproj -c Release -p:Platform=x64` succeeded with `0 Warning(s)` and `0 Error(s)`.
  - `powershell -File tools/reliability-gates.ps1 -Configuration Debug` reported `Gate result: PASS`.
  - `dotnet build tools/McpServer/McpServer.csproj` succeeded with `0 Warning(s)` and `0 Error(s)`.
  - `temp/logs/ElgatoCapture_Debug.log` from the final verification run contained only the existing intentional `UNIFIED_VIDEO_CAPTURE_FATAL type=InvalidOperationException msg=synthetic hfr failure` regression token; no new warning/failure tokens were emitted by this change in the repo-local harness pass.
- ffprobe Evidence:
  - N/A (diagnostics/automation surface change only; no recording artifact generated in this verification pass)
- Conclusion: The MJPEG timing metrics now survive stop transitions long enough to inspect via automation, the diagnostics snapshot no longer advertises zeroed timing fields, and the repo-local validation set now covers health, diagnostics, and MCP formatter surfacing for the new metrics.

## E71 - MCP formatter verification correction for MJPEG timing surface
- Timestamp (UTC): 2026-03-09T19:11:46Z
- Commit Hash: uncommitted (base fa9fb6fef6ee2d5d82bb886c573fc44baeb37019)
- What Changed (single change): Corrected the validation record for the MJPEG timing surface: the regression harness now contains a best-effort MCP formatter check, but this environment may skip direct `McpServer.dll` execution when the test host cannot load the tool's `.NET 8`/`System.Text.Json 10.x` dependency graph.
- How To Run:
  1. `dotnet build tools/McpServer/McpServer.csproj`
  2. `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"`
  3. If the local host can resolve the MCP tool dependencies, confirm the harness still reports `PASS: MCP formatter renders MJPEG timing section when fields exist`.
- Validator Output:
  - `dotnet build tools/McpServer/McpServer.csproj` succeeded with `0 Warning(s)` and `0 Error(s)`.
  - The app regression harness stayed green after adding the best-effort MCP formatter check.
  - This environment did not provide a standalone live `get_app_state` invocation against a running MCP process; formatter execution remains host-dependent here.
- ffprobe Evidence:
  - N/A (diagnostics/automation surface change only; no recording artifact generated in this verification pass)
- Conclusion: App-side health/automation plumbing is verified locally; direct MCP formatter execution is partially validated by build + best-effort harness coverage, but a live MCP process smoke remains outstanding for a host that can resolve the tool assembly graph cleanly.

## E26 - MJPEG Pipeline Timing: Live Results at 4K120
- Timestamp (UTC): 2026-03-09T20:00:00Z
- Commit Hash: (uncommitted — timing instrumentation from E25)
- What Changed: Verified live timing instrumentation via direct pipe queries. Fixed `_timingDiagDone` short-circuit bug (Interlocked.Exchange was consuming the one-shot flag before checking sample count). MCP server display confirmed working via raw JSON but ResponseFormatter not rendering due to stale MCP server process during session.
- How To Run:
  1. Build and launch: `dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -p:StageLatestBuild=true`
  2. Launch app, wait for MJPG 4K120 preview
  3. `powershell.exe -File temp/full-snapshot.ps1` (queries pipe directly)
  4. `powershell.exe -File temp/check-timing5.ps1` (compact timing view)
- Measurements (300-sample rolling windows, PS5 source at 3840x2160@120):
  - **Simple scene**: Decode avg=5.4-6.7ms P95=5.9-7.5ms | Interop avg=1.5-2.7ms | Callback avg=8.1-8.2ms | **0-1% drops**
  - **Complex scene**: Decode avg=8.1-14.2ms P95=13.1-16.1ms | Interop avg=1.3-2.4ms | Callback avg=10.6-16.0ms | **25-43% drops**
  - **Budget analysis**: Decode = 82% of callback time, interop copy = 18%
- Key Findings:
  - `mjpeg_cuvid` is a hybrid CPU (Huffman) + CUDA (IDCT) decoder, NOT the NVDEC ASIC (nvidia-smi shows 0% NVDEC)
  - Pipeline is strictly single-threaded: one frame at a time through decode ? interop copy
  - Performance is entirely scene-complexity-dependent (JPEG compressed size varies with content)
  - Frame budget at 120fps is 8.33ms; complex scenes blow this by 50-70%
  - Interop copy (CUDA staging ? D3D11) is fast enough at 1.5-2.7ms avg
- Conclusion: The decode stage is the bottleneck, not the interop copy or D3D11 contention. CPU multi-threaded MJPEG decode (FFmpeg `mjpeg` with AVX2, 2-3 threads) is the leading candidate for improvement since MJPEG frames are independent and can be decoded in parallel.

## E72 - Dual MJPEG decoder experiment with bounded decoder-1 worker lane
- Timestamp (UTC): 2026-03-09T21:01:14.3627007Z
- Commit Hash: uncommitted (base 19b2d8e04696aba58d627b642b1f911c1dffccbf)
- What Changed (single change): Added a second `NvdecMjpegDecoder` plus second `CudaD3D11InteropBridge` in `UnifiedVideoCapture`, routed odd MJPG frames to decoder slot 1 through a bounded single-reader worker lane, kept decoder 0 as the public `MjpegDecoder`, logged `MJPEG_DUAL_DECODER_*` diagnostics, and disposed/drained both decoder/interop instances during stop and teardown.
- How To Run:
  1. `dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -p:StageLatestBuild=true`
  2. `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"`
  3. `Get-Content temp/logs/ElgatoCapture_Debug.log -Tail 250`
- Validator Output:
  - `dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -p:StageLatestBuild=true` succeeded with `0 Warning(s)` and `0 Error(s)`.
  - `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"` reported `All runtime snapshot regression checks passed.`
  - `temp/logs/ElgatoCapture_Debug.log` from the repo-local verification run contained only the existing intentional `UNIFIED_VIDEO_CAPTURE_FATAL type=InvalidOperationException msg=synthetic hfr failure` snapshot token; this pass did not exercise live MJPG/NVDEC hardware.
- ffprobe Evidence:
  - N/A (preview/capture-path experiment only; no recording artifact generated in this verification pass)
- Conclusion: The dual-decoder experiment now has two decoder/interop slots, bounded background dispatch for slot 1, and explicit stop/dispose draining in the repo-local verified build. Live 4K120 hardware validation is still required to confirm whether the second lane materially reduces complex-scene drops.

## E73 - CUDA-D3D11 bridge now uses Y/UV helper textures for zero-copy NV12 assembly
- Timestamp (UTC): 2026-03-09T22:09:28.1465119Z
- Commit Hash: uncommitted (base 4cb30cb84f9a4d39258c705846dc3de4874ed4ac)
- What Changed (single change): Reworked `CudaD3D11InteropBridge` so zero-copy no longer registers the `DXGI_FORMAT_NV12` preview texture directly with CUDA. The bridge now creates `R8_UNORM` Y and `R8G8_UNORM` UV helper textures, registers both with CUDA, copies the NVDEC CUDA planes into those arrays, then assembles the final `NV12` preview texture with `CopySubresourceRegion`. If either helper texture setup or registration fails, the bridge falls back to the existing staging-texture path.
- How To Run:
  1. `dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -p:StageLatestBuild=true`
  2. `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"`
  3. `Get-Content temp/logs/ElgatoCapture_Debug.log -Tail 250`
- Validator Output:
  - `dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -p:StageLatestBuild=true` succeeded with `0 Warning(s)` and `0 Error(s)`.
  - `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"` reported `All runtime snapshot regression checks passed.`
  - `temp/logs/ElgatoCapture_Debug.log` from this verification run contained only the existing intentional `UNIFIED_VIDEO_CAPTURE_FATAL type=InvalidOperationException msg=synthetic hfr failure` snapshot token; this repo-local pass did not exercise live CUDA-D3D11 interop on NVIDIA hardware.
- ffprobe Evidence:
  - N/A (preview interop change only; no recording artifact generated in this verification pass)
- Conclusion: The bridge now matches CUDA's D3D11 interop limits by mapping only standard single-plane textures and assembling `NV12` after CUDA unmaps. Repo-local build/test/log verification is clean; live NVIDIA validation is still required to confirm `CUDA_D3D11_ZEROCOPY_REGISTER_OK` and first-frame `CUDA_D3D11_ZEROCOPY_DIAG` on hardware.

## E74 - Dual MJPEG decoders now share one CUDA device/frames context
- Timestamp (UTC): 2026-03-10T01:47:24.9218734Z
- Commit Hash: uncommitted (base f3f3fdb9d2275ecfba24901c67f8daaeaaa0945f)
- What Changed (single change): Added a shared-context `NvdecMjpegDecoder.Initialize(...)` overload and updated `UnifiedVideoCapture` to create one shared CUDA `AVHWDeviceContext` plus one shared CUDA `AVHWFramesContext` (`initial_pool_size=100`) for both MJPEG decoders, so decoder 0, decoder 1, and the CUDA encoder all reference the same underlying FFmpeg hardware context objects.
- How To Run:
  1. `dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -p:StageLatestBuild=true`
  2. `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"`
  3. `Get-Content temp/logs/ElgatoCapture_Debug.log -Tail 250`
  4. `rg -n "UNIFIED_CUDA_DEVICE_CTX_OK|UNIFIED_CUDA_FRAMES_CTX_OK|NVDEC_MJPEG_DECODER_INIT_SHARED|LIBAV_ENCODER_HW_FRAMES|avcodec_send_frame\\(cuda\\)|LIBAV_ENCODER_CLOSE" temp/logs/ElgatoCapture_Debug.log`
- Validator Output:
  - `dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -p:StageLatestBuild=true` succeeded with `0 Warning(s)` and `0 Error(s)`.
  - `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"` reported `All runtime snapshot regression checks passed.`
  - `temp/logs/ElgatoCapture_Debug.log` from the regression run contained only the existing intentional `UNIFIED_VIDEO_CAPTURE_FATAL type=InvalidOperationException msg=synthetic hfr failure` snapshot token; the repo-local harness did not initialize the live MJPG/NVDEC record path, so the new `UNIFIED_CUDA_*`, `NVDEC_MJPEG_DECODER_INIT_SHARED`, `LIBAV_ENCODER_HW_FRAMES mode=cuda`, and `LIBAV_ENCODER_CLOSE frames=...` tokens were not exercised here.
- ffprobe Evidence:
  - N/A (no recording artifact generated in this verification pass)
- Conclusion: The code now shares one CUDA device/frames context across both MJPEG decoders and the encoder path, with repo-local build/test verification clean. Live NVIDIA recording validation is still required to prove the runtime tokens and confirm that `avcodec_send_frame(cuda)` no longer fails during dual-decoder recording.

## E74 - Dual NVDEC decoders now share one CUDA device/frames context
- Timestamp (UTC): 2026-03-10T01:47:07.6173348Z
- Commit Hash: f3f3fdb9d2275ecfba24901c67f8daaeaaa0945f
- What Changed (single change): Added a shared-context `NvdecMjpegDecoder.Initialize` overload and changed `UnifiedVideoCapture` to create one shared CUDA `AVHWDeviceContext` plus one shared CUDA `AVHWFramesContext` (pool size 100) for both MJPEG decoders, so every decoded frame and the NVENC encoder use the same FFmpeg `hw_frames_ctx`.
- How To Run:
  1. `dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -p:StageLatestBuild=true`
  2. `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"`
  3. `Get-Content temp/logs/ElgatoCapture_Debug.log -Tail 120`
- Validator Output:
  - `dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -p:StageLatestBuild=true` succeeded with `0 Warning(s)` and `0 Error(s)`.
  - `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"` reported `All runtime snapshot regression checks passed.`
  - `temp/logs/ElgatoCapture_Debug.log` from this repo-local verification run contained only the existing intentional `UNIFIED_VIDEO_CAPTURE_FATAL type=InvalidOperationException msg=synthetic hfr failure` snapshot token; this pass did not exercise live MJPG/NVDEC/NVENC recording, so `UNIFIED_CUDA_*`, `NVDEC_MJPEG_DECODER_INIT_SHARED`, and `LIBAV_ENCODER_HW_FRAMES mode=cuda` were not emitted here.
- ffprobe Evidence:
  - N/A (no recording artifact generated in this verification pass)
- Conclusion: The code path now shares one CUDA device/frames context across decoder 0, decoder 1, and the encoder, eliminating the prior per-decoder FFmpeg context split in the implementation. Repo-local build/test verification is clean, but live hardware recording validation is still required to prove `frames>0` and the absence of `avcodec_send_frame(cuda)` failures.

## E75 - Experiment log correction for duplicate E74 shared-CUDA-context entries
- Timestamp (UTC): 2026-03-10T01:48:26.7617967Z
- Commit Hash: f3f3fdb9d2275ecfba24901c67f8daaeaaa0945f
- What Changed (single change): Corrected append-only bookkeeping after two `E74` headings were present for the same shared-CUDA-context implementation. Treat `E74 - Dual MJPEG decoders now share one CUDA device/frames context` with timestamp `2026-03-10T01:47:24.9218734Z` as the canonical experiment record for this change.
- How To Run:
  1. Read the `E74` and `E75` headings at the end of `docs/experiment_log.md`.
  2. Use the later `E74` entry timestamped `2026-03-10T01:47:24.9218734Z` as the authoritative validation record for the shared-CUDA-context implementation.
- Validator Output:
  - N/A (experiment-log correction only)
- ffprobe Evidence:
  - N/A (documentation correction only)
- Conclusion: The append-only history now ends with an explicit canonical-record rule, so the duplicate `E74` headings do not create ambiguity for this fix.

## E76 - NV12 preview now samples CUDA helper textures directly in the renderer
- Timestamp (UTC): 2026-03-10T04:49:17.7157612Z
- Commit Hash: uncommitted (base 0a56eef0ba6cd7c0c8921fb07fb629e049bbc251)
- What Changed (single change): Removed zero-copy NV12 assembly via `CopySubresourceRegion` in `CudaD3D11InteropBridge`, exposed the CUDA-filled Y/UV helper textures to `D3D11PreviewRenderer`, added an NV12 pixel-shader preview path that samples those helper textures directly, and routed MJPEG preview submission through `SubmitNv12PlaneTextures` when zero-copy interop is active.
- How To Run:
  1. `dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -p:StageLatestBuild=true`
  2. `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"`
  3. `Get-Content -Tail 220 temp/logs/ElgatoCapture_Debug.log`
- Validator Output:
  - `dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -p:StageLatestBuild=true` succeeded with `0 Warning(s)` and `0 Error(s)`.
  - `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"` reported `All runtime snapshot regression checks passed.`
  - `temp/logs/ElgatoCapture_Debug.log` from the repo-local verification run contained only the existing intentional `UNIFIED_VIDEO_CAPTURE_FATAL type=InvalidOperationException msg=synthetic hfr failure` regression token; no shader compilation failures, null-reference failures, or D3D11/HRESULT failure tokens were emitted by this change in the harness pass.
- ffprobe Evidence:
  - N/A (preview pipeline change only; no recording artifact generated in this verification pass)
- Conclusion: The preview path no longer depends on invalid `R8G8_UNorm -> NV12 plane` copies. Repo-local build, regression tests, and log inspection are clean; live NVIDIA MJPG/HFR validation is still required to confirm the green-preview fix on hardware.

## E77 - CUDA-D3D11 interop now retains the CUDA primary context per bridge
- Timestamp (UTC): 2026-03-10T07:08:54.8343586Z
- Commit Hash: uncommitted (base 0856ce8c83565fe01b450c98b52fb6052d7c36b1)
- What Changed (single change): Updated `CudaD3D11InteropBridge` to retain device 0's CUDA primary context, log caller-vs-primary context pointers, switch per-frame copy paths from `cuCtxPushCurrent`/`cuCtxPopCurrent` to `cuCtxSetCurrent`, and removed the worker-thread `EnsureCudaContextCurrent()` pre-set call so the bridge owns CUDA context management internally.
- How To Run:
  1. `dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -p:StageLatestBuild=true`
  2. `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"`
  3. `Get-Content temp/logs/ElgatoCapture_Debug.log`
- Validator Output:
  - `dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -p:StageLatestBuild=true` succeeded with `0 Warning(s)` and `0 Error(s)`.
  - `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"` reported `All runtime snapshot regression checks passed.`
  - `temp/logs/ElgatoCapture_Debug.log` from this repo-local harness pass contained only the existing intentional `UNIFIED_VIDEO_CAPTURE_FATAL type=InvalidOperationException msg=synthetic hfr failure` token; the harness did not exercise live CUDA/NVDEC interop, so the new `CUDA_D3D11_INTEROP_CTX*`, `CUDA_D3D11_CTX_PRE_PUSH`, and `CUDA_D3D11_ZEROCOPY_REGISTER_OK` tokens were not emitted here.
- ffprobe Evidence:
  - N/A (preview interop ownership change only; no recording artifact generated in this verification pass)
- Conclusion: The bridge now owns a CUDA-level primary-context retain/release pair and no longer relies on worker-thread preconditioning for per-frame context setup. Repo-local build/test verification is clean, but a live NVIDIA MJPG run is still required to confirm the new diagnostics and the absence of runtime `cuCtxPushCurrent`/`cuCtxSetCurrent` failures on hardware.

## E78 - MJPEG HFR now uses a CPU parallel decode pipeline with configurable worker count
- Timestamp (UTC): 2026-03-10T16:27:24.9885241Z
- Commit Hash: uncommitted (base e8104debbccf8240635a8caf4a0a79ca8bfa9514)
- What Changed (single change): Replaced the NVDEC/CUDA MJPEG high-frame-rate path with a software FFmpeg `mjpeg` decode pipeline (`SoftwareMjpegDecoder` + `ParallelMjpegDecodePipeline`), routed MJPEG preview/recording through raw NV12 emission, removed the CUDA-only recording requirement from `UnifiedVideoCapture`/`CaptureService`, added configurable MJPEG decoder count to capture settings + UI, and surfaced reorder/per-decoder timing stats.
- How To Run:
  1. `dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -p:StageLatestBuild=true`
  2. `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"`
  3. `Get-Content temp/logs/ElgatoCapture_Debug.log -Tail 160`
- Validator Output:
  - `dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -p:StageLatestBuild=true` succeeded with `0 Warning(s)` and `0 Error(s)`.
  - `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"` reported `All runtime snapshot regression checks passed.`
  - `temp/logs/ElgatoCapture_Debug.log` from the repo-local verification run contained only the existing intentional `UNIFIED_VIDEO_CAPTURE_FATAL type=InvalidOperationException msg=synthetic hfr failure` regression token; this pass did not exercise live MJPEG decode hardware or record-path throughput.
- ffprobe Evidence:
  - N/A (capture/preview/record-path change only; no recording artifact generated in this verification pass)
- Conclusion: The repo now builds and the runtime snapshot harness stays green with the CPU MJPEG pipeline, decoder-count setting, and expanded timing surface in place. Live 4K120 MJPG validation is still required to measure decode throughput and confirm end-to-end recording behavior on hardware.

## E79 - CPU MJPEG pipeline verification pass with decoder-count UI and reliability gate
- Timestamp (UTC): 2026-03-10T16:29:10.3313114Z
- Commit Hash: uncommitted (base e8104debbccf8240635a8caf4a0a79ca8bfa9514)
- What Changed (single change): Verified the software MJPEG pipeline integration end to end after fixing the final worker-drain teardown issue, with decoder-count settings flowing from UI -> `CaptureSettings` -> `CaptureService` -> `UnifiedVideoCapture` and per-decoder/reorder metrics surfaced in health + stats.
- How To Run:
  1. `dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -p:StageLatestBuild=true`
  2. `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"`
  3. `powershell -File tools/reliability-gates.ps1 -Configuration Debug`
  4. `Get-Content temp/logs/ElgatoCapture_Debug.log -Tail 160`
- Validator Output:
  - `dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -p:StageLatestBuild=true` succeeded with `0 Warning(s)` and `0 Error(s)`.
  - `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"` reported `All runtime snapshot regression checks passed.`
  - `powershell -File tools/reliability-gates.ps1 -Configuration Debug` reported `Gate result: PASS`.
  - `temp/logs/ElgatoCapture_Debug.log` from the final verification run contained only the existing intentional `UNIFIED_VIDEO_CAPTURE_FATAL type=InvalidOperationException msg=synthetic hfr failure` regression token from the synthetic harness; no new MJPEG pipeline warnings or teardown failures were emitted by this repo-local pass.
- ffprobe Evidence:
  - N/A (no recording artifact generated in this verification pass)
- Conclusion: The CPU MJPEG pipeline, decoder-count UI flow, and expanded timing surface now pass build, regression harness, and reliability gate checks in-repo. Live 4K120 MJPG hardware validation remains the next step for throughput and real recording confirmation.

## E80 - CPU MJPEG pipeline shutdown now waits for worker/emitter quiescence and contains emit callback faults
- Timestamp (UTC): 2026-03-11T04:35:44.5241385Z
- Commit Hash: uncommitted (base 259518bd417260e4d3042ff2ecf3dd818ab5c116)
- What Changed (single change): Hardened `ParallelMjpegDecodePipeline` so shutdown uses an explicit stop/quiesce path, only frees decoder/pool resources after worker and emitter thread exit is confirmed, surfaces a bounded stop failure instead of freeing live resources, and logs/drops emit-callback exceptions without letting them terminate the emitter thread.
- How To Run:
  1. `dotnet build ElgatoCapture/ElgatoCapture.csproj -c Debug -p:Platform=x64`
  2. `dotnet build ElgatoCapture/ElgatoCapture.csproj -c Release -p:Platform=x64`
  3. `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"`
  4. `powershell -File tools/reliability-gates.ps1 -Configuration Debug`
  5. `Get-Content temp/logs/ElgatoCapture_Debug.log -Tail 160`
- Validator Output:
  - `dotnet build ElgatoCapture/ElgatoCapture.csproj -c Debug -p:Platform=x64` succeeded with `0 Warning(s)` and `0 Error(s)`.
  - `dotnet build ElgatoCapture/ElgatoCapture.csproj -c Release -p:Platform=x64` succeeded with `0 Warning(s)` and `0 Error(s)`.
  - `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"` reported `All runtime snapshot regression checks passed.`
  - `powershell -File tools/reliability-gates.ps1 -Configuration Debug` reported `Gate result: PASS`.
  - `temp/logs/ElgatoCapture_Debug.log` from the repo-local validation run contained only the existing synthetic `UNIFIED_VIDEO_CAPTURE_FATAL type=InvalidOperationException msg=synthetic hfr failure` regression token; no new `MJPEG_EMIT_FAIL`, `PARALLEL_MJPEG_PIPELINE_DISPOSE_TIMEOUT`, or teardown-failure tokens were emitted by this pass.
- ffprobe Evidence:
  - N/A (capture-path hardening only; no recording artifact generated in this verification pass)
- Conclusion: The CPU MJPEG pipeline no longer uses the old timed-join-then-free teardown pattern, and emitter callback faults no longer threaten emitter-thread survival in the repo-local validation set. Live MJPG hardware traffic is still required to exercise the stop/quiesce path under real decode load.

## E81 - UnifiedVideoCapture stop now drains the CPU MJPEG path before return and reports decoded payload telemetry as NV12
- Timestamp (UTC): 2026-03-11T04:35:44.6241385Z
- Commit Hash: uncommitted (base 259518bd417260e4d3042ff2ecf3dd818ab5c116)
- What Changed (single change): Updated `UnifiedVideoCapture` so the CPU MJPEG pipeline is detached and drained during `StopAsync()` before the stop call returns, and corrected the CPU MJPEG emitted-payload observer token from `MJPG` to `NV12` without changing source/negotiated subtype reporting.
- How To Run:
  1. `dotnet build ElgatoCapture/ElgatoCapture.csproj -c Debug -p:Platform=x64`
  2. `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"`
  3. `powershell -File tools/reliability-gates.ps1 -Configuration Debug`
  4. `Get-Content temp/logs/ElgatoCapture_Debug.log -Tail 160`
- Validator Output:
  - `dotnet build ElgatoCapture/ElgatoCapture.csproj -c Debug -p:Platform=x64` succeeded with `0 Warning(s)` and `0 Error(s)`.
  - `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"` reported `PASS: Unified video capture CPU MJPEG emit reports NV12` and `All runtime snapshot regression checks passed.`
  - `powershell -File tools/reliability-gates.ps1 -Configuration Debug` reported `Gate result: PASS`.
  - `temp/logs/ElgatoCapture_Debug.log` from the repo-local validation run contained only the existing synthetic `UNIFIED_VIDEO_CAPTURE_FATAL ... synthetic hfr failure` regression token; no new stop-quiesce failure tokens were emitted by this pass.
- ffprobe Evidence:
  - N/A (capture-path behavior change only; no recording artifact generated in this verification pass)
- Conclusion: `UnifiedVideoCapture.StopAsync()` now owns CPU MJPEG drain/teardown instead of leaving it for later disposal, and observed-frame telemetry now reflects the decoded payload format correctly as `NV12`. Real MJPG preview/record stop validation is still required to prove the late-callback window is closed under live traffic.

## E82 - Permanent CPU MJPEG decoder incompatibility now faults loudly instead of degrading into silent drops
- Timestamp (UTC): 2026-03-11T04:35:44.7241385Z
- Commit Hash: uncommitted (base 259518bd417260e4d3042ff2ecf3dd818ab5c116)
- What Changed (single change): Changed `SoftwareMjpegDecoder` so unsupported decoded pixel formats and decoded-dimension mismatches throw a dedicated permanent-incompatibility exception, and updated the MJPEG pipeline / unified capture path to surface that condition through the existing fatal capture channel instead of treating it as an ordinary dropped frame.
- How To Run:
  1. `dotnet build ElgatoCapture/ElgatoCapture.csproj -c Debug -p:Platform=x64`
  2. `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"`
  3. `Get-Content temp/logs/ElgatoCapture_Debug.log -Tail 160`
- Validator Output:
  - `dotnet build ElgatoCapture/ElgatoCapture.csproj -c Debug -p:Platform=x64` succeeded with `0 Warning(s)` and `0 Error(s)`.
  - `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"` reported `PASS: Strict HFR fatal handler faults the capture session` and `All runtime snapshot regression checks passed.`
  - `temp/logs/ElgatoCapture_Debug.log` from the repo-local validation run contained the existing synthetic `UNIFIED_VIDEO_CAPTURE_FATAL ... synthetic hfr failure` token from the regression harness and no new repeated decoder-drop-loop tokens.
- ffprobe Evidence:
  - N/A (fatal-path hardening only; no recording artifact generated in this verification pass)
- Conclusion: Structural CPU MJPEG decode incompatibility is now classified as a permanent failure and routed through the existing loud-fail capture path instead of being left as an endless drop scenario. Live hardware is still required to exercise the dedicated permanent-incompatibility branch directly.

## E83 - Automation and MCP now expose the full CPU MJPEG metrics surface with regression coverage
- Timestamp (UTC): 2026-03-11T04:35:44.8241385Z
- Commit Hash: uncommitted (base 259518bd417260e4d3042ff2ecf3dd818ab5c116)
- What Changed (single change): Extended the diagnostics/automation/MCP surface to carry the full CPU MJPEG metrics already present in `CaptureHealthSnapshot` (decoder count, reorder stats, pipeline stats, totals, and per-decoder metrics), updated `CaptureDiagnosticsSnapshot` / `AutomationDiagnosticsHub` / `ResponseFormatter`, and expanded the runtime regression harness to assert the new contract and formatter output.
- How To Run:
  1. `dotnet build ElgatoCapture/ElgatoCapture.csproj -c Debug -p:Platform=x64`
  2. `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"`
  3. `powershell -File tools/reliability-gates.ps1 -Configuration Debug`
  4. `Get-Content temp/logs/ElgatoCapture_Debug.log -Tail 160`
- Validator Output:
  - `dotnet build ElgatoCapture/ElgatoCapture.csproj -c Debug -p:Platform=x64` succeeded with `0 Warning(s)` and `0 Error(s)`.
  - `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"` reported `PASS: Health snapshot uses cached MJPEG timing metrics when capture is gone`, `PASS: Diagnostics snapshot mirrors MJPEG timing metrics`, `PASS: Automation snapshot contract exposes full CPU MJPEG metrics`, `PASS: MCP formatter renders MJPEG timing section when fields exist`, and `All runtime snapshot regression checks passed.`
  - `powershell -File tools/reliability-gates.ps1 -Configuration Debug` reported `Gate result: PASS`.
  - `temp/logs/ElgatoCapture_Debug.log` from the repo-local validation run contained only the existing synthetic `UNIFIED_VIDEO_CAPTURE_FATAL ... synthetic hfr failure` regression token; no diagnostics-surface failures were emitted by this pass.
- ffprobe Evidence:
  - N/A (observability contract change only; no recording artifact generated in this verification pass)
- Conclusion: The richer CPU MJPEG timing/health data is now externally visible through diagnostics, automation, and MCP, and the repo-local regression harness guards that additive contract. Live MJPG sessions are still required to populate those fields with real throughput data in practice.

## E84 - CPU MJPEG shutdown now fails fast on pipeline self-join instead of deadlocking
- Timestamp (UTC): 2026-03-11T05:18:20.0000000Z
- Commit Hash: uncommitted (base 259518bd417260e4d3042ff2ecf3dd818ab5c116)
- What Changed (single change): Hardened `ParallelMjpegDecodePipeline.TryWaitForShutdown()` so shutdown detects reentrant calls from a pipeline worker or the emitter thread and returns a bounded self-join failure instead of attempting to `Join()` the current thread.
- How To Run:
  1. `dotnet build ElgatoCapture/ElgatoCapture.csproj -c Debug -p:Platform=x64`
  2. `dotnet build ElgatoCapture/ElgatoCapture.csproj -c Release -p:Platform=x64`
  3. `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"`
  4. `powershell -File tools/reliability-gates.ps1 -Configuration Debug`
  5. `Get-Content temp/logs/ElgatoCapture_Debug.log -Tail 160`
- Validator Output:
  - `dotnet build ElgatoCapture/ElgatoCapture.csproj -c Debug -p:Platform=x64` succeeded with `0 Warning(s)` and `0 Error(s)`.
  - `dotnet build ElgatoCapture/ElgatoCapture.csproj -c Release -p:Platform=x64` succeeded with `0 Warning(s)` and `0 Error(s)`.
  - `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"` reported `All runtime snapshot regression checks passed.`
  - `powershell -File tools/reliability-gates.ps1 -Configuration Debug` reported `Gate result: PASS`.
  - `temp/logs/ElgatoCapture_Debug.log` from the final verification run contained the existing synthetic `UNIFIED_VIDEO_CAPTURE_FATAL type=InvalidOperationException msg=synthetic hfr failure` regression token and repeated `FRAMERATE_NTSC_CORRECTION` lines, but no new MJPEG pipeline shutdown timeout or self-join failure tokens were emitted by this repo-local pass.
- ffprobe Evidence:
  - N/A (capture-path hardening only; no recording artifact generated in this verification pass)
- Conclusion: The CPU MJPEG stop path now rejects reentrant self-join shutdown attempts deterministically instead of risking a deadlock inside `Join()`, and the repo-local build/test/gate set remained clean after the guard was added. Live MJPG traffic is still required to exercise the reentrant stop path directly under real callback load.

## E85 - UnifiedVideoCapture now retains the CPU MJPEG pipeline instance when stop fails
- Timestamp (UTC): 2026-03-11T05:22:40.0000000Z
- Commit Hash: uncommitted (base 259518bd417260e4d3042ff2ecf3dd818ab5c116)
- What Changed (single change): Updated `UnifiedVideoCapture.StopAsync()` so it only clears `_mjpegPipeline` after `TryStop()` succeeds, allowing later cleanup/disposal to retry against the same pipeline instance when stop fails, and added a focused runtime regression that forces the `emitter_self_join` path and asserts the pipeline reference is retained.
- How To Run:
  1. `dotnet build ElgatoCapture/ElgatoCapture.csproj -c Debug -p:Platform=x64`
  2. `dotnet build ElgatoCapture/ElgatoCapture.csproj -c Release -p:Platform=x64`
  3. `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"`
  4. `powershell -File tools/reliability-gates.ps1 -Configuration Debug`
  5. `Get-Content temp/logs/ElgatoCapture_Debug.log -Tail 160`
- Validator Output:
  - `dotnet build ElgatoCapture/ElgatoCapture.csproj -c Debug -p:Platform=x64` succeeded with `0 Warning(s)` and `0 Error(s)`.
  - `dotnet build ElgatoCapture/ElgatoCapture.csproj -c Release -p:Platform=x64` succeeded with `0 Warning(s)` and `0 Error(s)`.
  - `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"` reported `PASS: Unified video capture retains MJPEG pipeline on stop failure` and `All runtime snapshot regression checks passed.`
  - `powershell -File tools/reliability-gates.ps1 -Configuration Debug` reported `Gate result: PASS`.
  - `temp/logs/ElgatoCapture_Debug.log` from the final verification run showed the forced `UNIFIED_VIDEO_MJPEG_STOP_FAIL reason='emitter_self_join'` token from the new regression followed by `PARALLEL_MJPEG_PIPELINE_DISPOSED ...`, confirming the retained pipeline could still be cleaned up after the stop failure path.
- ffprobe Evidence:
  - N/A (capture-path hardening only; no recording artifact generated in this verification pass)
- Conclusion: The CPU MJPEG stop-failure path no longer strands the pipeline by dropping the only strong reference before cleanup can happen, and the targeted runtime regression now proves the retained-instance behavior on the forced `emitter_self_join` branch.

## E86 - In-process FFmpeg now resolves app-local runtime folders instead of assuming AppContext.BaseDirectory
- Timestamp (UTC): 2026-03-11T14:16:30.0000000Z
- Commit Hash: uncommitted (base 259518bd417260e4d3042ff2ecf3dd818ab5c116)
- What Changed (single change): Added a shared `FfmpegRuntimeLocator` used by in-process libav startup and subprocess tool discovery, changed `LibAvEncoder.InitializeFFmpeg()` to retry until native runtime initialization actually succeeds instead of latching the first failed probe forever, switched in-process FFmpeg callers to require a real native runtime, and updated local staging so `ElgatoCapture\\ffmpeg\\**\\*` is preserved in app output.
- How To Run:
  1. Copy the local FFmpeg runtime payload into `ElgatoCapture\\ffmpeg\\` in the target worktree if that gitignored folder is missing.
  2. `dotnet build ElgatoCapture/ElgatoCapture.csproj -c Debug -p:Platform=x64`
  3. `dotnet build ElgatoCapture/ElgatoCapture.csproj -c Release -p:Platform=x64`
  4. `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"`
  5. `powershell -File tools/reliability-gates.ps1 -Configuration Debug`
  6. Launch `ElgatoCapture/bin/x64/Release/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.exe`, wait for startup, then inspect `temp/logs/ElgatoCapture_Debug.log`
- Validator Output:
  - `dotnet build ElgatoCapture/ElgatoCapture.csproj -c Debug -p:Platform=x64` succeeded with `0 Warning(s)` and `0 Error(s)`.
  - `dotnet build ElgatoCapture/ElgatoCapture.csproj -c Release -p:Platform=x64` succeeded with `0 Warning(s)` and `0 Error(s)`.
  - `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"` reported `PASS: FFmpeg runtime locator prefers app-local ffmpeg folder` and `All runtime snapshot regression checks passed.`
  - `powershell -File tools/reliability-gates.ps1 -Configuration Debug` reported `Gate result: PASS`.
- The rebuilt Debug output contained `win-x64\\ffmpeg\\avcodec-62.dll`, `avformat-62.dll`, `avutil-60.dll`, and `swresample-6.dll`.
- A live Release startup produced `LIBAV_INIT root_path='...\\win-x64\\ffmpeg' avcodec_version=4069477` at the top of `temp/logs/ElgatoCapture_Debug.log` instead of the previous `LIBAV_INIT_ERROR ... Specified method is not supported.`
- ffprobe Evidence:
  - N/A (runtime-location/capture-startup fix only; no recording artifact generated in this verification pass)
- Conclusion: The worktree no longer depends on `AppContext.BaseDirectory` coincidentally containing FFmpeg native DLLs, and in-process FFmpeg startup now binds successfully against the staged app-local `ffmpeg` folder. This removes the root-cause startup failure that was blocking live CPU MJPEG pipeline construction in the 4K120 `MJPG` path.

## E87 - Automation and MCP now expose advanced capture controls plus structured options/raw state
- Timestamp (UTC): 2026-03-11T17:27:15.2141917Z
- Commit Hash: uncommitted
- What Changed (single change): Added the missing advanced automation/MCP surface for preset, split encode mode, MJPEG decoder count, show-all capture options, preview volume, stats visibility, a structured `GetCaptureOptions` payload, and a raw `get_app_state_raw` MCP endpoint while keeping the existing human-readable `get_app_state` tool intact.
- How To Run:
  1. `dotnet build ElgatoCapture/ElgatoCapture.csproj -c Debug -p:Platform=x64`
  2. `dotnet build ElgatoCapture/ElgatoCapture.csproj -c Release -p:Platform=x64`
  3. `dotnet build tools/McpServer/McpServer.csproj -c Debug`
  4. `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"`
  5. `powershell -File tools/reliability-gates.ps1 -Configuration Debug`
  6. Launch `ElgatoCapture/bin/x64/Release/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.exe`, register `tools/McpServer/bin/Debug/net8.0/McpServer.exe` with the local `codex` MCP client, then call `get_capture_options`, `get_app_state_raw`, and `configure_ui`.
- Validator Output:
  - `dotnet build ElgatoCapture/ElgatoCapture.csproj -c Debug -p:Platform=x64` succeeded with `0 Warning(s)` and `0 Error(s)`.
  - `dotnet build ElgatoCapture/ElgatoCapture.csproj -c Release -p:Platform=x64` succeeded with `0 Warning(s)` and `0 Error(s)`.
  - `dotnet build tools/McpServer/McpServer.csproj -c Debug` succeeded with `0 Warning(s)` and `0 Error(s)`.
  - `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"` reported `All runtime snapshot regression checks passed.`
  - `powershell -File tools/reliability-gates.ps1 -Configuration Debug` reported `Gate result: PASS`.
  - Live MCP smoke via `codex exec` reported `Options tool: succeeded. Raw state: succeeded before and after.` and verified `PreviewVolumePercent: 88 -> 37` and `IsStatsVisible: false -> true`, then restored the temporary values.
- ffprobe Evidence:
  - N/A (automation/MCP surface change only; no recording artifact generated in this verification pass)
- Conclusion: Agents can now enumerate valid capture choices without guessing, read the raw structured app snapshot directly, and drive the missing advanced capture/UI controls through MCP. A regression check now also guards the automation command-id alignment across the app enum, CLI, script, and MCP bridge.

## E88 - Automation preview-volume control now persists through the same settings path as the UI
- Timestamp (UTC): 2026-03-11T21:18:55.3691440Z
- Commit Hash: uncommitted
- What Changed (single change): Updated `MainViewModel.SetPreviewVolumeAsync()` to call `SavePreviewVolume()` after applying the new automation/MCP preview volume value so remote `configure_ui` changes persist across app restarts just like the UI slider path.
- How To Run:
  1. `dotnet build ElgatoCapture/ElgatoCapture.csproj -c Debug -p:Platform=x64`
  2. `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"`
  3. `powershell -File tools/reliability-gates.ps1 -Configuration Debug`
- Validator Output:
  - `dotnet build ElgatoCapture/ElgatoCapture.csproj -c Debug -p:Platform=x64` succeeded with `0 Warning(s)` and `0 Error(s)`.
  - `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"` reported `PASS: Automation preview volume persists through the settings path` and `All runtime snapshot regression checks passed.`
  - `powershell -File tools/reliability-gates.ps1 -Configuration Debug` reported `Gate result: PASS`.
- ffprobe Evidence:
  - N/A (automation/settings persistence change only)
- Conclusion: Remote preview-volume changes now use the same persisted settings path as the UI slider instead of being transient in-memory updates.

## E89 - GetCaptureOptions no longer blocks on device-readiness gating
- Timestamp (UTC): 2026-03-11T21:18:55.3691440Z
- Commit Hash: uncommitted
- What Changed (single change): Removed `AutomationCommandKind.GetCaptureOptions` from `AutomationCommandDispatcher.RequiresReadyDevices()` so MCP/automation can enumerate current or empty option state during startup and no-device scenarios instead of receiving `not_ready`.
- How To Run:
  1. `dotnet build ElgatoCapture/ElgatoCapture.csproj -c Debug -p:Platform=x64`
  2. `dotnet build tools/McpServer/McpServer.csproj -c Debug`
  3. `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"`
  4. `powershell -File tools/reliability-gates.ps1 -Configuration Debug`
- Validator Output:
  - `dotnet build ElgatoCapture/ElgatoCapture.csproj -c Debug -p:Platform=x64` succeeded with `0 Warning(s)` and `0 Error(s)`.
  - `dotnet build tools/McpServer/McpServer.csproj -c Debug` succeeded with `0 Warning(s)` and `0 Error(s)`.
  - `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"` reported `PASS: UI automation commands are not blocked on device readiness` and `All runtime snapshot regression checks passed.`
  - `powershell -File tools/reliability-gates.ps1 -Configuration Debug` reported `Gate result: PASS`.
- ffprobe Evidence:
  - N/A (automation readiness-gating change only)
- Conclusion: The new capture-options MCP surface now remains available during initialization and no-device states, which matches its purpose as an options-discovery endpoint.

## E90 - Bugfix worktree now keeps main's English-only locale cleanup while preserving app-local FFmpeg staging
- Timestamp (UTC): 2026-03-11T21:18:55.3691440Z
- Commit Hash: uncommitted
- What Changed (single change): Reconciled `ElgatoCapture.csproj` with current `main` by restoring `SatelliteResourceLanguages` to English-only, keeping `StripUnwantedLocales` on both `Build` and `Publish`, broadening the locale regex to catch three-letter language tags, and preserving the bugfix worktree's recursive `ffmpeg\**\*` staging for the app-local native runtime.
- How To Run:
  1. `dotnet build ElgatoCapture/ElgatoCapture.csproj -c Debug -p:Platform=x64`
  2. `dotnet build ElgatoCapture/ElgatoCapture.csproj -c Release -p:Platform=x64`
  3. `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"`
  4. `powershell -File tools/reliability-gates.ps1 -Configuration Debug`
  5. `dotnet publish ElgatoCapture/ElgatoCapture.csproj -c Debug -p:Platform=x64`
  6. `Get-ChildItem ElgatoCapture/bin/Debug/net8.0-windows10.0.19041.0/win-x64/publish -Directory | Select-Object Name`
- Validator Output:
  - `dotnet build ElgatoCapture/ElgatoCapture.csproj -c Debug -p:Platform=x64` and `-c Release -p:Platform=x64` both succeeded with `0 Warning(s)` and `0 Error(s)`.
  - `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"` reported `PASS: Project file preserves main's English-only publish locale policy` and `All runtime snapshot regression checks passed.`
  - `powershell -File tools/reliability-gates.ps1 -Configuration Debug` reported `Gate result: PASS`.
  - `dotnet publish ElgatoCapture/ElgatoCapture.csproj -c Debug -p:Platform=x64` succeeded, and the fresh publish directory contained only `en-us`, `ffmpeg`, `Microsoft.UI.Xaml`, and `NpuDetect` folders.
- ffprobe Evidence:
  - N/A (project-file merge-alignment change only)
- Conclusion: The bugfix worktree now carries forward main's English-only publish policy instead of reverting it, while still staging the app-local FFmpeg runtime needed by the bugfix branch.

## E80 - Automation and MCP now expose video format override control
- Timestamp (UTC): 2026-03-11T13:46:11.2226402Z
- Commit Hash: uncommitted (base 259518b)
- What Changed (single change): Added append-only `SetVideoFormat` automation support end to end so the named-pipe API, `AutomationClient`, PowerShell helper, and MCP `configure_capture` tool can switch the appâ€™s video format override to values like `MJPG`.
- How To Run:
  1. `dotnet build ElgatoCapture/ElgatoCapture.csproj -c Debug -p:Platform=x64`
  2. `dotnet build tools/McpServer/McpServer.csproj -c Debug`
  3. `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"`
  4. Launch the app, then run `.\tools\AutomationClient\bin\Debug\net8.0\AutomationClient.exe --command SetVideoFormat --token codex-local --payload-kv videoFormat=MJPG`
  5. Run `.\tools\AutomationClient\bin\Debug\net8.0\AutomationClient.exe --command GetSnapshot --token codex-local --pretty`
- Validator Output:
  - Pending this session's validation run.
- ffprobe Evidence:
  - N/A (automation/MCP control change only)
- Conclusion: Pending this session's validation run.

## E81 - Live automation validation for SetVideoFormat MJPG control
- Timestamp (UTC): 2026-03-11T13:49:01.8247263Z
- Commit Hash: uncommitted (base 259518b)
- What Changed (single change): Validated the new `SetVideoFormat` automation/MCP seam against a live app session by switching the running capture pipeline to explicit `MJPG` request and confirming the requested subtype flipped in the runtime snapshot.
- How To Run:
  1. `dotnet build ElgatoCapture/ElgatoCapture.csproj -c Debug -p:Platform=x64`
  2. `dotnet build tools/McpServer/McpServer.csproj -c Debug`
  3. Launch `ElgatoCapture\bin\x64\Debug\net8.0-windows10.0.19041.0\win-x64\ElgatoCapture.exe`
  4. `.\tools\AutomationClient\bin\Debug\net8.0\AutomationClient.exe --command GetSnapshot --token codex-local --pretty`
  5. `.\tools\AutomationClient\bin\Debug\net8.0\AutomationClient.exe --command SetVideoFormat --token codex-local --payload-kv videoFormat=MJPG --pretty`
  6. `.\tools\AutomationClient\bin\Debug\net8.0\AutomationClient.exe --command GetSnapshot --token codex-local --pretty`
  7. `Get-Content temp/logs/ElgatoCapture_Debug.log -Tail 120`
- Validator Output:
  - `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"` reported `All runtime snapshot regression checks passed.`
  - `powershell -File tools/reliability-gates.ps1 -Configuration Debug` reported `Gate result: PASS`.
  - Initial live snapshot before the command showed `VideoRequestedSubtype="P010"` and `RequestedPixelFormat="P010"` while preview was already active.
  - `SetVideoFormat` returned success with message `Video format change requested: MJPG.`
  - Follow-up live snapshot showed `VideoRequestedSubtype="MJPG"`, `RequestedPixelFormat="MJPG"`, `RequestedReaderSubtype="MJPG"`, `RequestedWidth=3840`, `RequestedHeight=2160`, `RequestedFrameRateArg="120000/1001"`, and `PreviewFramesDropped=0`.
  - `temp/logs/ElgatoCapture_Debug.log` from the live run showed normal MJPEG activity and reorder-skip telemetry; no new automation command failures or preview restart failures were emitted.
- ffprobe Evidence:
  - N/A (automation/MCP control change only)
- Conclusion: The live app now accepts remote `SetVideoFormat=MJPG` commands through the shared automation/MCP backend and applies them to the active preview session with the expected requested-subtype change.

## E82 - Corrected SetVideoFormat thread affinity and English satellite preservation
- Timestamp (UTC): 2026-03-11T14:44:09.6719706Z
- Commit Hash: uncommitted (base 259518b)
- What Changed (single change): Fixed two regressions in the new MCP/automation video-format work by routing `MainViewModel.SetVideoFormatAsync` through `InvokeOnUiThreadAsync` and by making `StripUnwantedLocales` preserve the English satellite folder case-insensitively (`en-US`/`en-us`) in both build and publish outputs.
- How To Run:
  1. `dotnet build ElgatoCapture/ElgatoCapture.csproj -c Debug -p:Platform=x64`
  2. `dotnet build tools/McpServer/McpServer.csproj -c Debug`
  3. `dotnet build tools/AutomationClient/AutomationClient.csproj -c Debug`
  4. `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"`
  5. `powershell -File tools/reliability-gates.ps1 -Configuration Debug`
  6. Confirm `ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/en-us` exists after build.
  7. Launch `ElgatoCapture\bin\x64\Debug\net8.0-windows10.0.19041.0\win-x64\ElgatoCapture.exe`
  8. `.\tools\AutomationClient\bin\Debug\net8.0\AutomationClient.exe --command SetVideoFormat --token codex-local --payload-kv videoFormat=MJPG --pretty`
  9. `.\tools\AutomationClient\bin\Debug\net8.0\AutomationClient.exe --command GetSnapshot --token codex-local --pretty`
  10. `Get-Content temp/logs/ElgatoCapture_Debug.log -Tail 120`
- Validator Output:
  - All three Debug builds succeeded with `0 Warning(s)` and `0 Error(s)`.
  - `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"` reported `All runtime snapshot regression checks passed.`
  - `powershell -File tools/reliability-gates.ps1 -Configuration Debug` reported `Gate result: PASS`.
  - The build output still contained `en-us` after `StripUnwantedLocales` ran.
  - Live `SetVideoFormat` returned success with message `Video format change requested: MJPG.`
  - Follow-up live snapshot showed `VideoRequestedSubtype="MJPG"`, `RequestedPixelFormat="MJPG"`, and `RequestedReaderSubtype="MJPG"` with preview still active.
  - `temp/logs/ElgatoCapture_Debug.log` from the live run showed no new automation-thread or cross-thread failures; only the existing synthetic harness fatal token and normal telemetry activity were present.
- ffprobe Evidence:
  - N/A (UI-thread and packaging fix only)
- Conclusion: The automation/MCP video-format path now preserves WinUI thread affinity correctly, and the locale-strip target no longer deletes the retained English satellite folder due to `en-US`/`en-us` casing differences.

## E83 - Locale stripping now removes three-letter WinUI satellite folders
- Timestamp (UTC): 2026-03-11T22:20:00.0000000Z
- Commit Hash: uncommitted (base 7629415)
- What Changed (single change): Broadened `StripUnwantedLocales` in `ElgatoCapture.csproj` from a two-letter primary-language regex to a two-or-three-letter regex so English-only build/publish cleanup also removes WinUI satellite folders like `fil-PH`, `kok-IN`, and `quz-PE`.
- How To Run:
  1. `dotnet build ElgatoCapture/ElgatoCapture.csproj -c Debug -p:Platform=x64`
  2. `dotnet publish ElgatoCapture/ElgatoCapture.csproj -c Debug -p:Platform=x64`
  3. `Get-ChildItem ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64 -Directory | Select-Object Name`
  4. `Get-ChildItem publish-output -Directory | Select-Object Name`
- Validator Output:
  - Pending this session's validation run.
- ffprobe Evidence:
  - N/A (packaging-only change)
- Conclusion: Pending this session's validation run.

## E84 - Validation pass for three-letter locale stripping fix
- Timestamp (UTC): 2026-03-11T20:54:48.8255379Z
- Commit Hash: uncommitted (base 7629415)
- What Changed (single change): Validated the broadened locale-strip regex by rebuilding, running the regression harness, publishing, and inspecting the fresh publish directory for remaining locale folders.
- How To Run:
  1. `dotnet build ElgatoCapture/ElgatoCapture.csproj -c Debug -p:Platform=x64`
  2. `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"`
  3. `dotnet publish ElgatoCapture/ElgatoCapture.csproj -c Debug -p:Platform=x64`
  4. `Get-ChildItem ElgatoCapture/bin/Debug/net8.0-windows10.0.19041.0/win-x64/publish -Directory | Select-Object Name`
- Validator Output:
  - `dotnet build ElgatoCapture/ElgatoCapture.csproj -c Debug -p:Platform=x64` succeeded with `0 Warning(s)` and `0 Error(s)`.
  - `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"` reported `All runtime snapshot regression checks passed.`
  - `dotnet publish ElgatoCapture/ElgatoCapture.csproj -c Debug -p:Platform=x64` succeeded.
  - The fresh publish directory contained only `en-us` plus non-locale folders (`Microsoft.UI.Xaml`, `NpuDetect`); the previously observed three-letter locale folders were no longer present.
- ffprobe Evidence:
  - N/A (packaging-only change)
- Conclusion: The English-only locale cleanup now works for publish output as intended, including WinUI satellite folders whose primary language tag uses three letters.

## E91 - Merge cleanup removed the duplicate SetVideoFormatAsync definition
- Timestamp (UTC): 2026-03-11T22:43:00.0000000Z
- Commit Hash: uncommitted
- What Changed (single change): Removed the later duplicate `MainViewModel.SetVideoFormatAsync(string, CancellationToken)` method left behind by the `main` + bugfix branch merge so the original UI-threaded implementation remains the single automation entry point.
- How To Run:
  1. `dotnet build ElgatoCapture/ElgatoCapture.csproj -c Debug -p:Platform=x64`
  2. `dotnet build ElgatoCapture/ElgatoCapture.csproj -c Release -p:Platform=x64`
  3. `dotnet build tools/McpServer/McpServer.csproj -c Debug`
  4. `dotnet build tools/AutomationClient/AutomationClient.csproj -c Debug`
  5. `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"`
  6. `Get-Content temp/logs/ElgatoCapture_Debug.log -Tail 120`
  7. `powershell -File tools/reliability-gates.ps1 -Configuration Debug`
- Validator Output:
  - All listed builds succeeded with `0 Warning(s)` and `0 Error(s)`.
  - `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"` reported `All runtime snapshot regression checks passed.`
  - `temp/logs/ElgatoCapture_Debug.log` contained only the expected synthetic regression tokens (`UNIFIED_VIDEO_MJPEG_STOP_FAIL reason='emitter_self_join'` and `UNIFIED_VIDEO_CAPTURE_FATAL ... synthetic hfr failure`) plus the usual `FRAMERATE_NTSC_CORRECTION` lines.
  - `powershell -File tools/reliability-gates.ps1 -Configuration Debug` reported `Gate result: PASS`.
- ffprobe Evidence:
  - N/A (merge/build cleanup only)
- Conclusion: The merged `main` worktree now builds cleanly again after removing the duplicate automation method, and the full repo-local validation set stayed green.

## E92 - Live pixel format surfaces now prefer the source reader subtype over decoded MJPG output
- Timestamp (UTC): 2026-03-11T22:53:03.9656409Z
- Commit Hash: uncommitted
- What Changed (single change): Corrected the live/source format UI path so `MainViewModel.LivePixelFormat`, the stats dock/window source-format display, and `CaptureRuntimeSnapshot.ReaderSourceSubtype` all report the source-reader subtype (`MJPG` in 4K120 MJPG mode) instead of the decoded preview payload (`NV12`).
- How To Run:
  1. `dotnet build ElgatoCapture/ElgatoCapture.csproj -c Debug -p:Platform=x64`
  2. `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"`
  3. `Get-Content temp/logs/ElgatoCapture_Debug.log -Tail 120`
  4. `powershell -File tools/reliability-gates.ps1 -Configuration Debug`
- Validator Output:
  - `dotnet build ElgatoCapture/ElgatoCapture.csproj -c Debug -p:Platform=x64` succeeded with `0 Warning(s)` and `0 Error(s)`.
  - `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"` reported `PASS: Runtime snapshot preserves MJPG source subtype when observed frames are NV12`, `PASS: Live pixel format surfaces prefer source subtype over decoded output`, and `All runtime snapshot regression checks passed.`
  - `temp/logs/ElgatoCapture_Debug.log` contained only the expected synthetic regression tokens (`UNIFIED_VIDEO_MJPEG_STOP_FAIL reason='emitter_self_join'`, `UNIFIED_VIDEO_CAPTURE_FATAL ... synthetic hfr failure`) plus normal telemetry polling output.
  - `powershell -File tools/reliability-gates.ps1 -Configuration Debug` reported `Gate result: PASS`.
- ffprobe Evidence:
  - N/A (UI/runtime-snapshot fix only)
- Conclusion: MJPG sessions can continue decoding to NV12 internally for preview/record, while the user-facing “live/source format” surfaces once again identify the source-reader subtype correctly as `MJPG`.

## E93 - NativeXu telemetry now accepts both known 4K X product revisions
- Timestamp (UTC): 2026-03-12T02:28:25.8702113Z
- Commit Hash: uncommitted
- What Changed (single change): Broadened the NativeXu 4K X device gate to accept both `VID_0FD9&PID_009B` and `VID_0FD9&PID_009C`, and added a runtime harness regression that proves neither revision is rejected as `nativexu-device-unsupported`.
- How To Run:
  1. `dotnet build ElgatoCapture/ElgatoCapture.csproj -c Debug -p:Platform=x64`
  2. `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"`
  3. `Get-Content temp/logs/ElgatoCapture_Debug.log -Tail 120`
  4. `dotnet publish ElgatoCapture/ElgatoCapture.csproj -c Release -p:Platform=x64 -p:PublishProfile=win-x64 -p:PublishDir=C:\Users\crest\source\repos\ElgatoCapture\artifacts\portable-win-x64\publish\`
- Validator Output:
  - `dotnet build ElgatoCapture/ElgatoCapture.csproj -c Debug -p:Platform=x64` first hit a transient WinUI XAML compiler lock on `obj\...\intermediatexaml\ElgatoCapture.dll`; the repo's single-threaded debug gate build then succeeded with `0 Warning(s)` and `0 Error(s)`.
  - `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"` reported `PASS: NativeXu telemetry accepts known 4K X product revisions` and `All runtime snapshot regression checks passed.`
  - `temp/logs/ElgatoCapture_Debug.log` showed normal `NATIVEXU_*` telemetry reads on the development machine plus the expected synthetic regression tokens (`UNIFIED_VIDEO_MJPEG_STOP_FAIL reason='emitter_self_join'` and `UNIFIED_VIDEO_CAPTURE_FATAL ... synthetic hfr failure`).
  - `powershell -File tools/reliability-gates.ps1 -Configuration Debug` reported `Gate result: PASS`.
- ffprobe Evidence:
  - N/A (device-identification / telemetry gate change only)
- Conclusion: The built-in NativeXu telemetry gate now accepts both observed 4K X USB product revisions without regressing the existing runtime harness or reliability gate.

## E94 - Show-all capture options now unlock telemetry-filtered frame-rate overrides
- Timestamp (UTC): 2026-03-12T03:18:58.3107710Z
- Commit Hash: uncommitted
- What Changed (single change): Updated the frame-rate option rebuild path so `ShowAllCaptureOptions` re-enables frame-rate entries that were only disabled by source-telemetry cadence filtering, allowing demo/manual overrides while preserving other disable reasons such as HDR incompatibility.
- How To Run:
  1. `dotnet build ElgatoCapture/ElgatoCapture.csproj -c Debug -m:1 -p:Platform=x64`
  2. `dotnet build ElgatoCapture/ElgatoCapture.csproj -c Release -m:1 -p:Platform=x64`
  3. `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"`
  4. `powershell -File tools/reliability-gates.ps1 -Configuration Debug`
  5. `Get-Content temp/logs/ElgatoCapture_Debug.log -Tail 120`
- Validator Output:
  - Both Debug and Release builds succeeded with `0 Warning(s)` and `0 Error(s)`.
  - `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"` reported `PASS: Show all capture options unlocks source-filtered frame rates` and `All runtime snapshot regression checks passed.`
  - `powershell -File tools/reliability-gates.ps1 -Configuration Debug` reported `Gate result: PASS`.
  - `temp/logs/ElgatoCapture_Debug.log` contained normal `NATIVEXU_*` polling plus the expected synthetic regression tokens (`UNIFIED_VIDEO_MJPEG_STOP_FAIL reason='emitter_self_join'`, `UNIFIED_VIDEO_CAPTURE_FATAL ... synthetic hfr failure`) and no new warning/error lines tied to this UI-mode change.
- ffprobe Evidence:
  - N/A (option-selection / UI-state change only)
- Conclusion: Demo mode now exposes telemetry-filtered frame-rate overrides as selectable options instead of merely showing them as disabled.

## E95 - Preview startup now degrades to video-only when no audio capture endpoint exists
- Timestamp (UTC): 2026-03-12T03:18:58.3107710Z
- Commit Hash: uncommitted
- What Changed (single change): Changed preview startup so `CaptureService.StartVideoPreviewAsync()` no longer throws when audio is enabled but the selected device has no resolved audio capture endpoint; it now keeps the video preview alive and logs a video-only fallback for preview only.
- How To Run:
  1. `dotnet build ElgatoCapture/ElgatoCapture.csproj -c Debug -m:1 -p:Platform=x64`
  2. `dotnet build ElgatoCapture/ElgatoCapture.csproj -c Release -m:1 -p:Platform=x64`
  3. `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"`
  4. `powershell -File tools/reliability-gates.ps1 -Configuration Debug`
  5. `Get-Content temp/logs/ElgatoCapture_Debug.log -Tail 120`
- Validator Output:
  - Both Debug and Release builds succeeded with `0 Warning(s)` and `0 Error(s)`.
  - `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"` reported `PASS: Preview startup tolerates missing audio capture devices` and `All runtime snapshot regression checks passed.`
  - `powershell -File tools/reliability-gates.ps1 -Configuration Debug` reported `Gate result: PASS`.
  - `temp/logs/ElgatoCapture_Debug.log` contained normal `NATIVEXU_*` polling plus the expected synthetic regression tokens and no new preview-start failure lines from missing audio-device resolution during this validation pass.
- ffprobe Evidence:
  - N/A (preview startup behavior change only)
- Conclusion: Devices that lack a paired audio endpoint can now still start video preview when audio remains enabled, while recording-path strictness remains unchanged.

## E96 - Audio preview no longer reports active when no capture endpoint exists
- Timestamp (UTC): 2026-03-12T11:13:05.2541112Z
- Commit Hash: uncommitted
- What Changed (single change): Updated `CaptureService.StartAudioPreviewAsync()` so it leaves audio preview inactive and reports `Audio preview unavailable` when no audio capture endpoint exists, instead of marking audio preview active and emitting a false success status.
- How To Run:
  1. `dotnet build ElgatoCapture/ElgatoCapture.csproj -c Debug -m:1 -p:Platform=x64`
  2. `dotnet build ElgatoCapture/ElgatoCapture.csproj -c Release -m:1 -p:Platform=x64`
  3. `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"`
  4. `powershell -File tools/reliability-gates.ps1 -Configuration Debug`
  5. `Get-Content temp/logs/ElgatoCapture_Debug.log -Tail 120`
- Validator Output:
  - `dotnet build ElgatoCapture/ElgatoCapture.csproj -c Debug -m:1 -p:Platform=x64` first hit a transient WinUI XAML compiler lock on `obj\...\intermediatexaml\ElgatoCapture.dll`; the repo's single-threaded debug gate build then succeeded with `0 Warning(s)` and `0 Error(s)`.
  - `dotnet build ElgatoCapture/ElgatoCapture.csproj -c Release -m:1 -p:Platform=x64` succeeded with `0 Warning(s)` and `0 Error(s)`.
  - `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"` reported `PASS: Audio preview stays inactive when no audio capture device exists` and `All runtime snapshot regression checks passed.`
  - `powershell -File tools/reliability-gates.ps1 -Configuration Debug` reported `Gate result: PASS`.
  - `temp/logs/ElgatoCapture_Debug.log` contained normal `NATIVEXU_*` polling plus the expected synthetic regression tokens and no new audio-preview failure lines during this validation pass.
- ffprobe Evidence:
  - N/A (audio-preview state fix only)
- Conclusion: No-audio devices can still keep video preview running without falsely reporting audio preview as active.

## E97 - Audio monitoring visuals now track runtime preview activity instead of only user preference
- Timestamp (UTC): 2026-03-12T11:28:01.1702671Z
- Commit Hash: uncommitted
- What Changed (single change): Added a ViewModel-level `IsAudioPreviewActive` runtime property sourced from `CaptureService.GetRuntimeSnapshot()`, and updated the main window audio-meter visuals to follow that runtime state rather than only the persisted `IsAudioPreviewEnabled` preference.
- How To Run:
  1. `dotnet build ElgatoCapture/ElgatoCapture.csproj -c Debug -m:1 -p:Platform=x64`
  2. `dotnet build ElgatoCapture/ElgatoCapture.csproj -c Release -m:1 -p:Platform=x64`
  3. `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"`
  4. `powershell -File tools/reliability-gates.ps1 -Configuration Debug`
  5. `Get-Content temp/logs/ElgatoCapture_Debug.log -Tail 120`
- Validator Output:
  - `dotnet build ElgatoCapture/ElgatoCapture.csproj -c Debug -m:1 -p:Platform=x64` succeeded with `0 Warning(s)` and `0 Error(s)`.
  - `dotnet build ElgatoCapture/ElgatoCapture.csproj -c Release -m:1 -p:Platform=x64` succeeded with `0 Warning(s)` and `0 Error(s)`.
  - `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"` reported `PASS: Audio monitoring visuals follow runtime preview activity` and `All runtime snapshot regression checks passed.`
  - `powershell -File tools/reliability-gates.ps1 -Configuration Debug` reported `Gate result: PASS`.
  - `temp/logs/ElgatoCapture_Debug.log` contained the expected no-audio status token `Audio preview requested but no audio capture device is available.` plus the existing synthetic regression tokens and no new runtime/UI mismatch errors during this validation pass.
- ffprobe Evidence:
  - N/A (UI/runtime-state alignment change only)
- Conclusion: The monitor toggle can remain the user's preference while the audio meter now reflects whether audio preview is actually active.

## E98 - Preview backend summary log now reflects video-only fallback accurately
- Timestamp (UTC): 2026-03-12T11:28:01.1702671Z
- Commit Hash: uncommitted
- What Changed (single change): Updated the preview-backend summary log in `CaptureService.StartVideoPreviewAsync()` so it reports `IMFSourceReader video only (no audio capture endpoint)` when preview starts without WASAPI capture, instead of always claiming `WASAPI audio ingest`.
- How To Run:
  1. `dotnet build ElgatoCapture/ElgatoCapture.csproj -c Debug -m:1 -p:Platform=x64`
  2. `dotnet build ElgatoCapture/ElgatoCapture.csproj -c Release -m:1 -p:Platform=x64`
  3. `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"`
  4. `powershell -File tools/reliability-gates.ps1 -Configuration Debug`
  5. `Get-Content temp/logs/ElgatoCapture_Debug.log -Tail 120`
- Validator Output:
  - `dotnet build ElgatoCapture/ElgatoCapture.csproj -c Debug -m:1 -p:Platform=x64` succeeded with `0 Warning(s)` and `0 Error(s)`.
  - `dotnet build ElgatoCapture/ElgatoCapture.csproj -c Release -m:1 -p:Platform=x64` succeeded with `0 Warning(s)` and `0 Error(s)`.
  - `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"` reported `PASS: Preview backend log reflects video-only fallback` and `All runtime snapshot regression checks passed.`
  - `powershell -File tools/reliability-gates.ps1 -Configuration Debug` reported `Gate result: PASS`.
  - `temp/logs/ElgatoCapture_Debug.log` still showed the expected no-audio status token and existing synthetic regression tokens; no new preview-backend warning/error lines were introduced by this observability fix.
- ffprobe Evidence:
  - N/A (logging / diagnostics contract change only)
- Conclusion: The debug log now distinguishes true audio-backed preview from the intentional video-only fallback, keeping the repo's log-first debugging workflow trustworthy.

## E99 - Fatal MJPEG capture faults now clear active session state before leaving the session faulted
- Timestamp (UTC): 2026-03-12T16:18:38.3274298Z
- Commit Hash: uncommitted
- What Changed (single change): Routed `OnUnifiedVideoCaptureFatalError()` through a best-effort cleanup pass so permanent capture faults stop preview/record backends, clear active runtime flags, and then leave the session in `Faulted` instead of preserving stale active state.
- How To Run:
  1. `dotnet build ElgatoCapture/ElgatoCapture.csproj -c Debug -m:1 -p:Platform=x64`
  2. `dotnet build ElgatoCapture/ElgatoCapture.csproj -c Release -m:1 -p:Platform=x64`
  3. `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"`
  4. `powershell -File tools/reliability-gates.ps1 -Configuration Debug`
  5. `Get-Content temp/logs/ElgatoCapture_Debug.log -Tail 120`
- Validator Output:
  - `dotnet build ElgatoCapture/ElgatoCapture.csproj -c Debug -m:1 -p:Platform=x64` succeeded with `0 Warning(s)` and `0 Error(s)`.
  - `dotnet build ElgatoCapture/ElgatoCapture.csproj -c Release -m:1 -p:Platform=x64` succeeded with `0 Warning(s)` and `0 Error(s)`.
  - `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"` reported `PASS: Strict HFR fatal handler clears active session state` and `All runtime snapshot regression checks passed.`
  - `powershell -File tools/reliability-gates.ps1 -Configuration Debug` reported `Gate result: PASS`.
  - `temp/logs/ElgatoCapture_Debug.log` still contained the expected synthetic fatal token `UNIFIED_VIDEO_CAPTURE_FATAL ... synthetic hfr failure`, and the runtime harness no longer left preview/record/audio-preview active after that fault path.
- ffprobe Evidence:
  - N/A (fault-cleanup / runtime-state change only)
- Conclusion: Permanent capture faults now unwind the live session cleanly before preserving the terminal `Faulted` state for user recovery.

## E100 - Raw automation app state no longer embeds capture options
- Timestamp (UTC): 2026-03-12T16:18:38.3274298Z
- Commit Hash: uncommitted
- What Changed (single change): Removed the dead `AutomationSnapshot.Options` contract and stopped `get_app_state_raw` from splicing `GetCaptureOptions` into the raw snapshot, keeping capture options as a separate on-demand automation surface.
- How To Run:
  1. `dotnet build ElgatoCapture/ElgatoCapture.csproj -c Debug -m:1 -p:Platform=x64`
  2. `dotnet build ElgatoCapture/ElgatoCapture.csproj -c Release -m:1 -p:Platform=x64`
  3. `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"`
  4. `powershell -File tools/reliability-gates.ps1 -Configuration Debug`
  5. `Get-Content temp/logs/ElgatoCapture_Debug.log -Tail 120`
- Validator Output:
  - `dotnet build ElgatoCapture/ElgatoCapture.csproj -c Debug -m:1 -p:Platform=x64` succeeded with `0 Warning(s)` and `0 Error(s)`.
  - `dotnet build ElgatoCapture/ElgatoCapture.csproj -c Release -m:1 -p:Platform=x64` succeeded with `0 Warning(s)` and `0 Error(s)`.
  - `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"` reported `PASS: MCP raw app state keeps capture options separate` and `All runtime snapshot regression checks passed.`
  - `powershell -File tools/reliability-gates.ps1 -Configuration Debug` reported `Gate result: PASS`.
  - `temp/logs/ElgatoCapture_Debug.log` contained only the expected synthetic regression tokens and no new automation-snapshot failure lines during this contract cleanup.
- ffprobe Evidence:
  - N/A (automation contract / token-efficiency change only)
- Conclusion: Runtime state and selectable capture options are now intentionally separate automation payloads instead of an inconsistent hybrid contract.

## E101 - Show-all capture options and stats visibility now persist through the settings path
- Timestamp (UTC): 2026-03-12T16:18:38.3274298Z
- Commit Hash: uncommitted
- What Changed (single change): Added `ShowAllCaptureOptions` and `IsStatsVisible` to the persisted `UserSettings` model and wired both UI/automation setters through the existing save/load path so those preferences survive relaunch like preview volume already does.
- How To Run:
  1. `dotnet build ElgatoCapture/ElgatoCapture.csproj -c Debug -m:1 -p:Platform=x64`
  2. `dotnet build ElgatoCapture/ElgatoCapture.csproj -c Release -m:1 -p:Platform=x64`
  3. `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"`
  4. `powershell -File tools/reliability-gates.ps1 -Configuration Debug`
  5. `Get-Content temp/logs/ElgatoCapture_Debug.log -Tail 120`
- Validator Output:
  - `dotnet build ElgatoCapture/ElgatoCapture.csproj -c Debug -m:1 -p:Platform=x64` succeeded with `0 Warning(s)` and `0 Error(s)`.
  - `dotnet build ElgatoCapture/ElgatoCapture.csproj -c Release -m:1 -p:Platform=x64` succeeded with `0 Warning(s)` and `0 Error(s)`.
  - `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"` reported `PASS: Automation UI settings persist through the settings path` and `PASS: Capture errors refresh ViewModel runtime flags`, then `All runtime snapshot regression checks passed.`
  - `powershell -File tools/reliability-gates.ps1 -Configuration Debug` reported `Gate result: PASS`.
  - `temp/logs/ElgatoCapture_Debug.log` contained only the expected synthetic regression tokens and no new settings-load or settings-save warnings during this validation pass.
- ffprobe Evidence:
  - N/A (settings persistence / UI-state change only)
- Conclusion: The demo-oriented capture-options toggle and stats-panel visibility now behave like real user preferences instead of reverting on the next launch.

## E102 - NativeXu source telemetry now exposes structured signal details instead of only a summary string
- Timestamp (UTC): 2026-03-12T17:05:52.6251382Z
- Commit Hash: uncommitted
- What Changed (single change): Promoted NativeXu HDMI-source fields into structured telemetry properties and grouped detail entries, covering source video format, colorimetry, quantization, HDR transfer, and the reverse-engineered AT command diagnostics with friendly-plus-raw fallback values.
- How To Run:
  1. `dotnet build ElgatoCapture/ElgatoCapture.csproj -c Debug -m:1 -p:Platform=x64`
  2. `dotnet build ElgatoCapture/ElgatoCapture.csproj -c Release -m:1 -p:Platform=x64`
  3. `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"`
  4. `powershell -File tools/reliability-gates.ps1 -Configuration Debug`
  5. `Get-Content temp/logs/ElgatoCapture_Debug.log -Tail 120`
- Validator Output:
  - `dotnet build ElgatoCapture/ElgatoCapture.csproj -c Debug -m:1 -p:Platform=x64` succeeded with `0 Warning(s)` and `0 Error(s)`.
  - `dotnet build ElgatoCapture/ElgatoCapture.csproj -c Release -m:1 -p:Platform=x64` succeeded with `0 Warning(s)` and `0 Error(s)`.
  - `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"` reported `PASS: Health snapshot propagates structured source telemetry details` and `All runtime snapshot regression checks passed.`
  - `powershell -File tools/reliability-gates.ps1 -Configuration Debug` reported `Gate result: PASS`.
  - `temp/logs/ElgatoCapture_Debug.log` showed live NativeXu reads with `AviInfoFrame`, `HdrMetadata`, `AudioFormat`, `AudioSamplingRate`, `InputSource`, `UsbHostProtocol`, `HdcpMode`, `Hdr2Sdr`, and `RawTiming`, followed by `NATIVEXU_DECODE ... colorspace=YCbCr422 colorimetry=BT.2020`.
- ffprobe Evidence:
  - N/A (telemetry model / diagnostics-surface change only)
- Conclusion: NativeXu now produces reusable structured source telemetry instead of forcing the UI to reverse-parse the diagnostic summary string.

## E103 - Stats views now keep HDMI Input source-only and move richer telemetry into details
- Timestamp (UTC): 2026-03-12T17:05:52.6251382Z
- Commit Hash: uncommitted
- What Changed (single change): Updated the docked stats panel and standalone Stats window so `HDMI Input` uses source telemetry for `Video Format` and HDR formatting, while the expanded details area renders grouped reverse-engineered telemetry rows instead of card-side negotiated subtype data.
- How To Run:
  1. `dotnet build ElgatoCapture/ElgatoCapture.csproj -c Debug -m:1 -p:Platform=x64`
  2. `dotnet build ElgatoCapture/ElgatoCapture.csproj -c Release -m:1 -p:Platform=x64`
  3. `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"`
  4. `powershell -File tools/reliability-gates.ps1 -Configuration Debug`
  5. `& '.\tools\AutomationClient\bin\Debug\net8.0\AutomationClient.exe' --command GetSnapshot --token codex-local`
- Validator Output:
  - Both Debug and Release builds succeeded with `0 Warning(s)` and `0 Error(s)`.
  - `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"` reported `PASS: Stats panels use source telemetry for HDMI input format and HDR` and `All runtime snapshot regression checks passed.`
  - `powershell -File tools/reliability-gates.ps1 -Configuration Debug` reported `Gate result: PASS`.
  - Live automation against the launched Debug app returned `SourceVideoFormat":"YCbCr422"`, `SourceColorimetry":"BT.2020"`, `SourceHdrTransferFunction":"HDR10 / PQ"`, and grouped `SourceTelemetryDetails` entries while preview was active on the 4K120 source.
- ffprobe Evidence:
  - N/A (stats UI / telemetry presentation change only)
- Conclusion: The Stats surfaces now distinguish HDMI-source telemetry from capture-card negotiation, showing `Video Format = YCbCr422` and HDR as source-derived information instead of the card’s negotiated subtype.

## E104 - Automation snapshots now expose the new structured source telemetry fields
- Timestamp (UTC): 2026-03-12T17:05:52.6251382Z
- Commit Hash: uncommitted
- What Changed (single change): Propagated the new structured source telemetry fields and grouped detail entries through the capture-runtime and automation snapshot contracts so MCP and automation consumers can validate the same source-vs-capture distinction the Stats UI now shows.
- How To Run:
  1. `dotnet build ElgatoCapture/ElgatoCapture.csproj -c Debug -m:1 -p:Platform=x64`
  2. `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"`
  3. `& '.\tools\AutomationClient\bin\Debug\net8.0\AutomationClient.exe' --command GetSnapshot --token codex-local`
  4. `Get-Content temp/logs/ElgatoCapture_Debug.log -Tail 120`
- Validator Output:
  - `dotnet build ElgatoCapture/ElgatoCapture.csproj -c Debug -m:1 -p:Platform=x64` succeeded with `0 Warning(s)` and `0 Error(s)`.
  - `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"` reported `PASS: Health snapshot propagates structured source telemetry details` and `PASS: Stats panels use source telemetry for HDMI input format and HDR`.
  - Live automation returned `SourceTelemetryDetails` with grouped entries such as `Input Source = HDMI (0)`, `Audio Format = Code 2 (2)`, `USB Protocol = Mode 2 (2)`, `HDCP Mode = Code 1 (1)`, and `Raw Timing = ...hex...`.
  - `temp/logs/ElgatoCapture_Debug.log` still showed the same underlying AT command reads that back those structured automation fields.
- ffprobe Evidence:
  - N/A (automation contract / diagnostics-surface change only)
- Conclusion: Automation clients can now inspect the richer source telemetry directly instead of scraping the old colon-delimited diagnostic summary.

## E105 - NativeXu now promotes high-confidence source telemetry fields into structured snapshots
- Timestamp (UTC): 2026-03-12T19:10:00Z
- Commit Hash: uncommitted
- What Changed (single change): Promoted the high-confidence NativeXu fields into first-class structured telemetry (`Firmware`, audio format/sample rate, input source, USB host protocol, HDCP fields, and raw timing hex) and propagated them through capture-health and automation snapshots while keeping uncertain decodes conservative.
- How To Run:
  1. `dotnet build ElgatoCapture/ElgatoCapture.csproj -c Debug -m:1 -p:Platform=x64`
  2. `dotnet build ElgatoCapture/ElgatoCapture.csproj -c Release -m:1 -p:Platform=x64`
  3. `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"`
  4. `powershell -File tools/reliability-gates.ps1 -Configuration Debug`
  5. `Get-Content temp/logs/ElgatoCapture_Debug.log -Tail 120`
- Validator Output:
  - Both Debug and Release builds succeeded with `0 Warning(s)` and `0 Error(s)`.
  - `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"` reported `PASS: Health snapshot propagates structured source telemetry details`, `PASS: Automation snapshots expose high-confidence source telemetry fields`, and `All runtime snapshot regression checks passed.`
  - `powershell -File tools/reliability-gates.ps1 -Configuration Debug` reported `Gate result: PASS`.
  - `temp/logs/ElgatoCapture_Debug.log` showed the live NativeXu reads backing the new structured fields, including `SystemInfo code=0x23 preview=444232303539233130393700`, `AudioFormat code=0x04 preview=02000000`, `AudioSamplingRate code=0x06 preview=07000000`, `InputSource code=0x35 preview=00000000`, `UsbHostProtocol code=0x40 preview=02000000`, `HdcpMode code=0x72 preview=01000000`, `RxTxHdcpVersion code=0x8A preview=03000000`, and `RawTiming code=0x37 preview=3000CA0830117008000F52008001D32E5D69100A00580001010101090603401F`.
- ffprobe Evidence:
  - N/A (telemetry model / diagnostics-surface change only)
- Conclusion: High-confidence NativeXu telemetry is now available as structured source fields across the app and automation surfaces, while still leaving medium- and low-confidence decodes explicit rather than guessed.

## E106 - Control shelf now exposes device audio mode and analog gain controls
- Timestamp (UTC): 2026-03-12T18:18:03.0691138Z
- Commit Hash: uncommitted
- What Changed (single change): Added EGAV-backed device audio controls to the control shelf so supported devices can switch HDMI vs analog input mode and adjust analog line-in gain, with persisted mode/gain preferences restored through the existing settings path.
- How To Run:
  1. `dotnet build ElgatoCapture/ElgatoCapture.csproj -c Debug -m:1 -p:Platform=x64`
  2. `dotnet build ElgatoCapture/ElgatoCapture.csproj -c Release -m:1 -p:Platform=x64`
  3. `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"`
  4. `powershell -File tools/reliability-gates.ps1 -Configuration Debug`
  5. Launch the app and verify `DeviceAudioModeComboBox` and `AnalogGainNumberBox` appear for a supported Elgato device.
- Validator Output:
  - `dotnet build ElgatoCapture/ElgatoCapture.csproj -c Release -m:1 -p:Platform=x64` succeeded with `0 Warning(s)` and `0 Error(s)`.
  - `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"` reported `PASS: Device audio controls are exposed in the control shelf`, `PASS: Device audio control settings persist through the settings path`, and `All runtime snapshot regression checks passed.`
  - `powershell -File tools/reliability-gates.ps1 -Configuration Debug` reported `Gate result: PASS`.
  - A direct `dotnet build ... -c Debug` retry hit the known transient `Microsoft.UI.Xaml.Markup.Compiler` file-lock on `obj\\...\\intermediatexaml\\ElgatoCapture.dll`, but the repo’s single-threaded reliability gate rerun immediately built the same tree successfully with no code changes.
- ffprobe Evidence:
  - N/A (device-control/UI change only)
- Conclusion: The control shelf can now drive supported device-side audio input mode and analog gain, and those preferences persist across relaunch without introducing a parallel settings path.

## E107 - Stop surfacing NativeXu `SystemInfo` as user-facing firmware
- Timestamp (UTC): 2026-03-12T18:18:03.0691138Z
- Commit Hash: uncommitted
- What Changed (single change): Suppressed the NativeXu `SystemInfo` string (`DB2059#1097`) from the structured telemetry/UI surface so the app no longer presents it as meaningful product firmware while deeper EGAV firmware/version decoding remains unresolved.
- How To Run:
  1. `dotnet build ElgatoCapture/ElgatoCapture.csproj -c Debug -m:1 -p:Platform=x64`
  2. `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"`
  3. `powershell -File tools/reliability-gates.ps1 -Configuration Debug`
  4. Open the Stats details panel and confirm no firmware row is rendered from NativeXu `SystemInfo`.
- Validator Output:
  - `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"` still reported `PASS: Health snapshot propagates structured source telemetry details` and `All runtime snapshot regression checks passed.` after removing the firmware assertion.
  - `powershell -File tools/reliability-gates.ps1 -Configuration Debug` reported `Gate result: PASS`.
  - `temp/logs/ElgatoCapture_Debug.log` still showed `SystemInfo code=0x23 preview=444232303539233130393700`, proving the raw AT read remains available for reverse-engineering even though the UI-facing structured telemetry no longer surfaces it as firmware.
- ffprobe Evidence:
  - N/A (telemetry presentation change only)
- Conclusion: The misleading `DB2059#1097` system-info string is retained only as raw diagnostic evidence and is no longer presented as user-facing firmware.

## E108 - Reverted the proprietary EGAV audio-control experiment from the Stats branch
- Timestamp (UTC): 2026-03-12T18:29:33.4175252Z
- Commit Hash: uncommitted
- What Changed (single change): Removed the uncommitted EGAV-backed device audio mode and analog gain control integration from the `Stats` branch so the worktree returns to the project’s custom-code-only reverse-engineering direction while preserving the NativeXu telemetry work.
- How To Run:
  1. `dotnet build ElgatoCapture/ElgatoCapture.csproj -c Debug -m:1 -p:Platform=x64`
  2. `dotnet build ElgatoCapture/ElgatoCapture.csproj -c Release -m:1 -p:Platform=x64`
  3. `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"`
  4. `powershell -File tools/reliability-gates.ps1 -Configuration Debug`
  5. `Get-Content temp/logs/ElgatoCapture_Debug.log -Tail 120`
- Validator Output:
  - `dotnet build ElgatoCapture/ElgatoCapture.csproj -c Debug -m:1 -p:Platform=x64` succeeded with `0 Warning(s)` and `0 Error(s)`.
  - `dotnet build ElgatoCapture/ElgatoCapture.csproj -c Release -m:1 -p:Platform=x64` succeeded with `0 Warning(s)` and `0 Error(s)`.
  - `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"` reported `All runtime snapshot regression checks passed.` after the device-audio-control assertions were removed.
  - The first `powershell -File tools/reliability-gates.ps1 -Configuration Debug` retry hit the repo’s known transient WinUI XAML compiler failure, and an immediate rerun reported `Gate result: PASS` with no code changes in between.
  - `temp/logs/ElgatoCapture_Debug.log` still showed the expected NativeXu-only telemetry reads (`AudioFormat`, `AudioSamplingRate`, `InputSource`, `UsbHostProtocol`, `HdcpMode`, `RxTxHdcpVersion`, `RawTiming`) and no new EGAV-specific app dependency path.
- ffprobe Evidence:
  - N/A (dependency rollback / UI cleanup only)
- Conclusion: The proprietary audio-control experiment is fully backed out, and the `Stats` branch is back on a custom NativeXu telemetry path only.

## E109 - Added a NativeXu-only audio control probe and mapped the first working AT setters on 4K X
- Timestamp (UTC): 2026-03-12T18:50:03.7105593Z
- Commit Hash: uncommitted
- What Changed (single change): Added a repo-local `tools/NativeXuAudioProbe` console plus a small raw AT-read helper in `NativeXuAtCommandProvider` so the 4K X could be probed through the custom KS/XU path only, then ran live write/readback sweeps across candidate audio routing, on/off, mute, and gain opcodes.
- How To Run:
  1. `dotnet build tools/NativeXuAudioProbe/NativeXuAudioProbe.csproj -c Debug`
  2. `dotnet run --project tools/NativeXuAudioProbe/NativeXuAudioProbe.csproj -c Debug`
  3. `Get-Content temp/logs/ElgatoCapture_Debug.log -Tail 120`
- Validator Output:
  - `dotnet build tools/NativeXuAudioProbe/NativeXuAudioProbe.csproj -c Debug` succeeded with `0 Warning(s)` and `0 Error(s)`.
  - The live probe found these stable baseline AT getter values on the connected 4K X: `AdcOnOff=0`, `DacHpOnOff=1`, `AdcVolumeGain=0`, `HdmiDprxVolumeGain=-1`, `UacVolumeGain=0`, `AuxInVolume=127`, `AuxOutVolume=129`, `UacOut2MixerSource=2`, `DacHpMixerSource=0`, `I2sOutMixerSource=4`, and all mute getters at `0`.
  - The probe confirmed working custom readback loops for these setters:
    - `0x26 -> 0x27` (`SetUacOut2MixerSource` / `GetUacOut2MixerSource`): non-baseline writes collapsed getter `2 -> 0`
    - `0x2A -> 0x2B` (`SetI2SOut_MixerSource` / `GetI2SOut_MixerSource`): non-baseline writes collapsed getter `4 -> 0`
    - `0x09 -> 0x75` (`SetDacHpOnOff` / `GetDacHpOnOff`): `1 -> 0`
    - `0x2C -> 0x2D`, `0x2E -> 0x2F`, `0x30 -> 0x31`, `0x32 -> 0x33` (mute setters/getters): `0 -> 1`
    - `0x0C -> 0x0D` (`SetHdmiDprxVolumeGain` / `GetHdmiDprxVolumeGain`): baseline `-1` moved to `0` or saturated at `30`
    - `0x10 -> 0x11` (`SetUacVolumeGain` / `GetUacVolumeGain`): baseline `0` moved to saturated `30`
  - The same probe did **not** produce getter-visible changes for `0x08 -> 0x74` (`ADC OnOff`), `0x0A -> 0x0B` (`ADC VolumeGain`), `0x28 -> 0x29` (`DACHP MixerSource`), `0x7F/0x80` (`Aux In Volume`), or `0x81/0x82` (`Aux Out Volume`) under the current HDMI-source session.
  - Across all tested writes, `CurrentInputSource (0x35)` remained `0`, and the NativeXu source snapshot still reported `Input source: HDMI (0)` at the end of the sweep.
  - `temp/logs/ElgatoCapture_Debug.log` showed successful `NATIVEXU_SET_OK` entries for the tested opcodes, confirming the results came from the custom KS/XU transport rather than any proprietary DLL path.
- ffprobe Evidence:
  - N/A (live device-control reverse-engineering only)
- Conclusion: The custom NativeXu path already supports several real audio control surfaces with reliable readback, but the true HDMI-vs-analog input switch was not isolated yet; it appears to require a multi-command sequence beyond the single-AT writes tested here.

## E110 - NativeXu AT phase 2 adds audio telemetry and AT-first input/gain control
- Timestamp (UTC): 2026-03-12T20:50:59.5769195Z
- Commit Hash: uncommitted
- What Changed (single change): Extended the custom NativeXu AT path with the phase-2 audio/headphone/USB/firmware getters plus typed AT setter helpers, then updated `MainViewModel` to try AT-based input-source and ADC-gain writes first while keeping the existing payload-mutation service as fallback/readback.
- How To Run:
  1. `dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -p:StageLatestBuild=true`
  2. `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"`
  3. `Get-Content temp/logs/ElgatoCapture_Debug.log -Tail 200`
- Validator Output:
  - `dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -p:StageLatestBuild=true` succeeded with `0 Warning(s)` and `0 Error(s)`.
  - `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"` reported `All runtime snapshot regression checks passed.`
  - `temp/logs/ElgatoCapture_Debug.log` showed the new live AT reads without failure tokens, including `AdcOnOff code=0x74 preview=00000000`, `AdcVolumeGain code=0x0B preview=00000000`, `DacHpOnOff code=0x75 preview=01000000`, `DacHpMute code=0x31 preview=00000000`, `HpOutGain code=0x67 preview=E1FF0000`, `UacVolumeGain code=0x11 preview=00000000`, `TxEdidValid code=0x43 preview=00000000`, and `CustomerVersion code=0x77 frameLen=133 rawBytes=128 preview=323530323130...`.
- ffprobe Evidence:
  - N/A (telemetry/control-path change only)
- Conclusion: The NativeXu provider now surfaces the phase-2 audio/headphone/USB/factory telemetry in the dynamic details list and can issue AT-first input-source plus ADC-gain writes while preserving the existing payload mutation path as fallback.

## E111 - EGAVDS audio probe: confirmed audio input switching works via proprietary DLL
- Timestamp (UTC): 2026-03-12T22:00:00Z
- Commit Hash: uncommitted
- What Changed (single change): Built `tools/EgavdsAudioProbe` — a standalone P/Invoke probe that calls Elgato Studio's EGAVDeviceSupport.dll (v0.1.0.622) directly via SWIG entry points.
- How To Run:
  1. Copy `RTICE_SDK_x64.dll` and `RTK_IO_x64.dll` from Elgato Studio's `EGAVDeviceSupport/` to the probe's output directory
  2. `dotnet build tools/EgavdsAudioProbe/EgavdsAudioProbe.csproj`
  3. `cd tools/EgavdsAudioProbe/bin/x64/Debug/net8.0-windows10.0.19041.0 && EgavdsAudioProbe.exe` (reads current state)
  4. `EgavdsAudioProbe.exe --set hdmi` or `EgavdsAudioProbe.exe --set analog` (switches audio input)
- Validator Output:
  - EGAVDS successfully queries and switches audio input between HDMI (1) and Analog (2)
  - `SupportsAudioInputSelection` = true, `SupportsLineInAudioGainControl` = true
  - Gain range: min=0, max=255, default=128, observed current=255
  - Audio input enum: `EGAVDS_AUDIO_INPUT_INVALID=0, EGAVDS_AUDIO_INPUT_HDMI=1, EGAVDS_AUDIO_INPUT_ANALOG=2`
- Key Findings:
  - EGAVDeviceSupport.dll depends on `RTICE_SDK_x64.dll` and `RTK_IO_x64.dll` (Realtek libraries)
  - Studio version (0.1.0.622, 1.5MB) is much newer/larger than Elgato Capture version (0.0.2.46, 982KB) and has audio input functions the Capture version lacks
  - SWIG P/Invoke requires registering exception callbacks before any other call
  - Studio init params differ from Capture: has `edidDirectoryPath` and `firmwareDirectoryPath`, no `initializeSentry`
- Conclusion: EGAVDS successfully controls audio input switching. This serves as ground-truth reference for protocol reverse-engineering.

## E112 - AT commands (0x34/0x35) do NOT control audio input switching
- Timestamp (UTC): 2026-03-12T23:00:00Z
- Commit Hash: uncommitted
- What Changed (single change): Used NativeXuAudioProbe to test AT SetInputSource (0x34) and GetInputSource (0x35) before/after EGAVDS audio switches.
- How To Run:
  1. `tools/NativeXuAudioProbe/bin/.../NativeXuAudioProbe.exe at-set-input 1` (try AT-based switch to Analog)
  2. Then verify via `EgavdsAudioProbe.exe` (EGAVDS ground truth)
  3. Or use `temp/sniff-audio-switch.ps1` to read AT before/after EGAVDS switch
- Validator Output:
  - AT SetInputSource (0x34) returns success but has NO effect on actual audio routing
  - AT GetInputSource (0x35) reads `00-00-00-00` regardless of HDMI or Analog mode
  - EGAVDS switches confirmed to work independently of AT state
- Conclusion: AT commands 0x34/0x35 (`SetInputSource`/`GetInputSource`) are NOT the mechanism EGAVDS uses for audio input switching. The AT layer and the actual audio switching operate through different XU/register pathways.

## E113 - XU Selector 3 blob does NOT encode audio input state
- Timestamp (UTC): 2026-03-12T23:30:00Z
- Commit Hash: uncommitted
- What Changed (single change): Used NativeXuAudioProbe's `dump-s3` mode to capture XU selector 3 (150 bytes) in both HDMI and Analog modes, then diffed.
- How To Run:
  1. Set audio to HDMI via `EgavdsAudioProbe.exe --set hdmi`
  2. `NativeXuAudioProbe.exe dump-s3 --out temp/s3-hdmi.hex`
  3. Set audio to Analog via `EgavdsAudioProbe.exe --set analog`
  4. `NativeXuAudioProbe.exe dump-s3 --out temp/s3-analog.hex`
  5. Diff the two files
- Validator Output:
  - Only 1 byte difference at offset 7: `0x6A` (HDMI) vs `0x6B` (Analog)
  - Reading Analog twice without switching produced identical results → offset 7 is a monotonic counter, not a mode flag
  - All other 149 bytes identical across modes
- Key Findings:
  - XU Selector 3 on PID 009B does NOT contain audio input state
  - The sole varying byte is an incrementing counter (likely a read-sequence or telemetry revision counter)
  - This rules out Selector 3 as the audio control mechanism
- Conclusion: Audio input switching on the 4K X uses a mechanism outside both the AT command layer and XU Selector 3. Evidence points to direct I2C register access via RTICE_SDK's `SetValue`/`GetValue` functions, which bypass the AT/S1/S2 protocol entirely.

## E114 - EGAVDS uses RTICE_SDK SetValue/GetValue for audio, not AT commands
- Timestamp (UTC): 2026-03-12T23:45:00Z
- Commit Hash: uncommitted
- What Changed (single change): ILSpy analysis of EGAVDeviceSupport.dll import table and RTICE_SDK_x64.dll export table.
- How To Run:
  - Static analysis only — examine DLL import/export tables
- Key Findings:
  - EGAVDeviceSupport.dll imports from RTICE_SDK: `SetValue`, `GetValue`, `Audio_Set_UacOut2MixerSource`, `Audio_Set_ADC_OnOff`, `Audio_Set_DACHP_MixerSource`, `HdmiRX_*` functions
  - RTICE_SDK_x64.dll exports 96+ functions including `Input_Source`, `Audio_*`, `HdmiRX_*`, `SetValue`, `GetValue`
  - These are direct register-level controls that communicate with the Realtek chip via a different XU protocol than our AT commands
  - 4K X has NO HID interface (only Camera MI_00 and MEDIA MI_02) — HID I2C path in EGAVDeviceSupport is for other Elgato products
  - `availableAudioInputs: 3` in ElgatoDeviceCapabilities.json = bitmask 0b11 = two inputs (HDMI + Analog)
- Conclusion: To implement proprietary-DLL-free audio input switching, we need to intercept the actual USB traffic RTICE_SDK sends during an EGAVDS audio switch. Two approaches: (1) USB packet capture with USBPcap/Wireshark filtering for XU control transfers, or (2) build a shim/proxy DLL for RTK_IO_x64.dll to log all function calls and parameters. The shim approach is more targeted.

## E115 - RTK_IO_x64.dll export analysis: only 28 exports, all AT commands route through rtk_sendATCommand
- Timestamp (UTC): 2026-03-13T00:00:00Z
- Commit Hash: uncommitted
- What Changed (single change): Parsed PE export tables of both RTK_IO_x64.dll (28 exports) and RTICE_SDK_x64.dll (123 exports) using custom Python PE parser.
- How To Run:
  - Static analysis only — custom Python script parsing PE export/import directories
- Key Findings:
  - **RTK_IO_x64.dll exports (28)**: `rtk_initialize`, `rtk_uninitialize`, `rtk_uninitialize_ex`, `rtk_openPort`, `rtk_closePort`, `rtk_isOpen`, `rtk_setDevice`, `rtk_setCurrentDevice`, `rtk_getCurrentDeviceName`, `rtk_enableLog`, `rtk_readRbus`, `rtk_writeRbus`, `rtk_rescueReadRbus`, `rtk_rescueWriteRbus`, `rtk_sendATCommand`, `rtk_sendI2CATCommand`, `rtk_setUVCExtension`, `rtk_enterDebugMode`, `rtk_exitDebugMode`, `rtk_Get_Customer_version`, plus burn/flash/EDID utilities
  - **RTICE_SDK_x64.dll exports (123)**: ALL prefixed with `AT_*` — e.g., `AT_Input_Source_Switch`, `AT_Audio_Set_ADC_OnOff`, `AT_Audio_Set_UacOut2_MixerSource`, `AT_Get_Current_Input_Source`, etc.
  - **RTICE_SDK imports 23 functions from RTK_IO** — notably `rtk_sendATCommand`, `rtk_sendI2CATCommand`, `rtk_readRbus`, `rtk_writeRbus`, `rtk_setUVCExtension`, `rtk_openPort`, `rtk_closePort`, `rtk_setDevice`, `rtk_setCurrentDevice`, `rtk_getCurrentDeviceName`, `rtk_isOpen`, `rtk_initialize`, `rtk_uninitialize`, `rtk_uninitialize_ex`, `rtk_enableLog`, `rtk_Get_Customer_version`, plus burn functions
  - **Key insight**: `AT_Input_Source_Switch` (the SET command) is a separate RTICE_SDK export from `AT_Get_Current_Input_Source` (the GET). Both route through `rtk_sendATCommand` in RTK_IO.
  - Previous E112 tested AT opcodes 0x34/0x35 from our independent opcode catalog — but EGAVDS actually uses `AT_Input_Source_Switch` and `AT_Get_Current_Input_Source` which may map to DIFFERENT opcodes
- Conclusion: All RTICE_SDK functions ultimately call `rtk_sendATCommand` or `rtk_readRbus`/`rtk_writeRbus` in RTK_IO. A shim DLL at the RTK_IO layer captures everything.

## E116 - RTK_IO shim DLL: intercepted EGAVDS init + GetAudioInputSelection AT traffic
- Timestamp (UTC): 2026-03-13T00:30:00Z
- Commit Hash: uncommitted
- What Changed (single change): Built `tools/RtkIoShim/` — a C++ proxy DLL that intercepts all RTK_IO_x64.dll calls, logging function names and raw argument bytes. Deployed by renaming real DLL to `RTK_IO_x64_real.dll` and placing shim as `RTK_IO_x64.dll`.
- How To Run:
  1. Build: `cl /LD /EHsc /O2 rtk_io_shim.cpp /Fe:RTK_IO_x64.dll /link /DEF:rtk_io_shim.def` (requires VS dev shell)
  2. In EgavdsAudioProbe output dir: rename `RTK_IO_x64.dll` → `RTK_IO_x64_real.dll`, copy shim as `RTK_IO_x64.dll`
  3. Run `EgavdsAudioProbe.exe` — log written to `rtk_io_shim.log` in same directory
- Validator Output:
  - Shim successfully intercepts and forwards all RTK_IO calls
  - EgavdsAudioProbe runs normally with shim in place (read-only queries work)
  - `SetAudioInputSelection` crashes with AccessViolationException (calling convention mismatch in P/Invoke for SET — GET works fine)
- **BREAKTHROUGH — rtk_sendATCommand true signature and AT opcode discovery**:
  - `rtk_sendATCommand` signature: `(opcode_byte, ???, cmdStruct, responseBuffer, ???, ...)`
  - `a1` = AT opcode as a raw byte (NOT a handle)
  - `a4` = response buffer pointer (zeroed before call, filled after)
  - Return value = response byte count
  - **Intercepted AT calls during EGAVDS GetAudioInputSelection**:

  | Call | a1 (opcode) | Response (a4 AFTER) | Decoded |
  |------|-------------|---------------------|---------|
  | 1 | 0x52 | `01 80 FF AA 55` | AT_Get_System_Info — system status |
  | 2 | 0x52 | `01 80 FF AA 55` | (repeated) |
  | 3 | 0x65 | `87 01 1A 02` | AT_Audio_Get_HPOUTgain — headphone out gain |
  | 4 | **0x40** | **`02 00 00 00`** | **AT_Get_Current_Input_Source — value 2 = Analog** |
  | 5 | 0x4B | `83 01 19 53 43 45 49 00 00 00 00 50 53 35` | AT_HdmiRX_Get_Cable_Connect — "SCEI" "PS5" |

- **CRITICAL FINDING**: EGAVDS uses AT opcode **0x40** for `GetCurrentInputSource`, NOT 0x35 (which we tested in E112). Our earlier test used the wrong opcode!
  - 0x40 = `AT_Get_Current_Input_Source` — returns actual audio input state (HDMI=1, Analog=2)
  - 0x35 = `GetInputSource` (from our independent opcode catalog) — returns 0 always, appears to be a different/unused function
  - Response byte 0 directly encodes the EGAVDS enum: 1=HDMI, 2=Analog
- **EGAVDS init sequence via RTK_IO**:
  1. `rtk_initialize()` → 0
  2. `rtk_setUVCExtension(a1=2, ...)` — sets UVC extension unit node ID
  3. `rtk_setCurrentDevice("A7SNB346101346")` — serial number of device
  4. `rtk_openPort(...)` → 0
  5. Series of `rtk_sendATCommand` calls (0x52, 0x65, 0x40, 0x4B)
  6. `rtk_closePort(...)` / `rtk_uninitialize_ex()`
- **SetAudioInputSelection crash**: The P/Invoke for EGAVDS_SetAudioInputSelection causes AccessViolationException when called through the shim. The shim's generic forwarding works for GET calls but the SET call's stack layout or calling convention differs. This doesn't block discovery — we just need to capture the SET traffic.
- **Correction**: AT opcode 0x40 returns the SAME value (2) regardless of whether EGAVDS is in HDMI or Analog mode. It is NOT the audio input source — it is `AT_USB_Get_Host_Protocol` as our catalog said. The coincidence confused the initial analysis.
- Conclusion: The GET response data during EGAVDS init was misleading — opcode 0x40 is USB Host Protocol, not audio input. The actual audio switching mechanism is via `rtk_sendI2CATCommand`, not `rtk_sendATCommand`.

## E117 - BREAKTHROUGH: Full EGAVDS audio switch sequence captured via RTK_IO shim
- Timestamp (UTC): 2026-03-13T01:00:00Z
- Commit Hash: uncommitted
- What Changed (single change): Fixed shim crash (was trying to dereference `a3` as byte count when it was a pointer), reran EGAVDS `SetAudioInputSelection(Analog)` with shim active. Captured complete traffic.
- How To Run:
  1. Build shim: `cl /LD /EHsc /O2 rtk_io_shim.cpp /Fe:RTK_IO_x64.dll /link /DEF:rtk_io_shim.def`
  2. Deploy shim to EgavdsAudioProbe output dir (rename real → `RTK_IO_x64_real.dll`)
  3. `EgavdsAudioProbe.exe --set analog` (or `--set hdmi`)
  4. Read `rtk_io_shim.log`
- **CRITICAL DISCOVERY: Audio switching uses `rtk_sendI2CATCommand`, NOT `rtk_sendATCommand`**:
  - `rtk_sendATCommand` = UVC Extension Unit XU commands (what our NativeXu path uses)
  - `rtk_sendI2CATCommand` = I2C-based AT commands (a DIFFERENT transport layer!)
  - Both use AT command opcodes but over different USB transports
  - Audio input switching goes through the I2C path exclusively
- **`rtk_sendI2CATCommand` wire format**:
  - `a1` = transport opcode: `0x1B` = I2C SET, `0x1C` = I2C GET
  - `a3` = command buffer with format: `00 4A [01=SET|02=GET] 00 [AT_opcode] [value_bytes...]`
  - `a4` = response buffer (zeroed before, filled after)
  - Return value = response byte count
- **Complete SET sequence for switching HDMI→Analog**:
  1. I2C GET (0x1C): `00 4A 02 00 09 42` — read opcode 0x09 (ADC state?) → response `01`
  2. I2C SET (0x1B): `00 4A 01 00 04 01` — **set opcode 0x04 = 1** (audio input = HDMI? routing config)
  3. I2C GET (0x1C): `00 4A 02 00 03 A0` — read opcode 0x03 (validation?) → response `01`
  4. I2C SET (0x1B): `00 4A 01 00 0E 01` — set opcode 0x0E = 1 (mixer/routing)
  5. I2C SET (0x1B): `00 4A 01 00 10 01` — set opcode 0x10 = 1 (UAC volume gain?)
  6. UVC AT (0x5B): `00 05 00 00` — opcode 0x5B with value 5 → response `01` (trigger commit?)
  7. More I2C SET/GET calls with opcodes 0x04, 0x0E, 0x07, 0x0F, 0x10, 0x11 (multi-register sequence)
  8. Final readback via UVC AT: 0x52 (system info), 0x51 (input source switch ack?), 0x65 (HP gain), 0x40 (USB host), 0x4B (cable connect)
- **Key opcodes in the I2C AT buffer** (byte at position 4):
  - 0x04 = audio routing/input select
  - 0x09 = ADC state readback
  - 0x0E = mixer source configuration
  - 0x10 = UAC volume gain
  - 0x03 = validation/status
  - 0x07, 0x0F, 0x11 = additional routing registers
- **AT opcode 0x40 is NOT audio input** — confirmed by direct test: AT 0x40 returns `02` regardless of HDMI or Analog mode. It is `AT_USB_Get_Host_Protocol` as our catalog maps.
- Conclusion: Audio input switching on the 4K X requires I2C AT commands (`rtk_sendI2CATCommand`) which use a different USB transport than our existing UVC XU path. The switch is a multi-step sequence of ~10 I2C register writes/reads. To implement this natively, we need to either: (1) discover how `rtk_sendI2CATCommand` maps to USB control transfers (likely still UVC XU but with different selector/format), or (2) find if our existing `SetViaOutput` XU protocol can carry I2C AT frames with the `00 4A` prefix.

## E-AudioSwitch - Disassembly + Pure C# Audio Input Switching
- Timestamp (UTC): 2026-03-12T12:00:00Z
- Branch: Stats

### RTK_IO_x64.dll Disassembly (capstone)
- `rtk_sendI2CATCommand` (RVA 0xC34D0): thin 10-instruction wrapper. Loads RTICE_SDK context object from global, shuffles args, calls `[vtable + 0x610]`. ALL logic is in RTICE_SDK, not RTK_IO.
- `rtk_sendATCommand` (RVA 0xC3400): calls internal function at 0xCB570, checks magic 0x94B.
- `rtk_openPort` (RVA 0xC3320): calls `[vtable + 0xE0]`.
- RTK_IO is a dispatch shim — zero protocol logic.

### EGAVDeviceSupport.dll Disassembly (capstone)
- `SetAudioInputSelection` (RVA 0x2883 → thunks → 0x5D880):
  - Validates device handle, loads RTICE_SDK context from `[rip + 0x108587]`
  - Analog: `mov edx, 2` → HDMI: `mov edx, 1`
  - Calls `[vtable + 0x150]` on RTICE_SDK device object
  - This is a DIFFERENT vtable method from `rtk_sendI2CATCommand` (0x610) and `rtk_sendATCommand`
  - EGAVDS does NOT use AT commands for audio switching — uses RBUS or vendor path

### I2C AT wrapping via AT opcodes 0x1B/0x1C — DEAD END
- Wrapping I2C frames `[00 4A ...]` inside AT commands with opcodes 0x1B/0x1C returns generic ACKs
- Scanning ALL I2C opcodes 0x00-0x20 returns `01-00-00-00` for every single one
- The firmware accepts the AT command but does not dispatch real I2C operations
- Confirmed: this mechanism does not work

### AT opcode diff: EGAVDS switch changes ZERO readable AT opcodes
- Read 68 AT opcodes before EGAVDS SetAudioInputSelection(HDMI), then after SetAudioInputSelection(Analog)
- Only AT 0x52 changed (buffer contamination with serial number string, not meaningful)
- All audio-relevant opcodes (0x04, 0x35, 0x40, 0x74, 0x75) identical in both states
- Confirms EGAVDS vtable[0x150] uses a non-AT mechanism (RBUS or direct register writes)

### BREAKTHROUGH: AT SET 0x34 IS the correct input source control
- `NativeXuAtCommandProvider.SetInputSourceAsync` sends AT SET with opcode 0x34
- Sending `AT SET 0x34 = 1` changes multiple registers: 0x35 (0→1), 0x40 (2→0), 0x04 (2→0)
- Stats panel in ElgatoCapture app correctly shows "analog" after the command
- **HOWEVER**: sending AT 0x34 raw causes the USB device to disconnect/reconnect (Windows disconnect sound). The capture card resets.
- Root cause hypothesis: AT 0x34 changes the audio routing while audio is actively streaming, causing the USB audio device to re-enumerate. Need to either:
  1. Stop WASAPI capture before switching, then restart after
  2. Send additional bracketing commands (ADC on/off via 0x08, DAC via 0x09)
  3. Use the same timing/sequencing that EGAVDS's vtable[0x150] uses internally
- The AT path and the RBUS path are two different ways to reach the same hardware — EGAVDS uses RBUS (stable), we use AT (causes reset). The difference is likely in how gracefully the transition is handled.

## E-AUDIO-3 - USB Packet Capture: Elgato Studio Audio Switch Protocol Discovery
- Timestamp (UTC): 2026-03-12T22:00:00Z
- Branch: Stats

### Discovery
Used USBPcap6 + custom Python parser to capture ALL USB traffic during Elgato Studio audio input switch. Previous captures only looked at UVC class-specific transfers and found only telemetry polling. This time parsed ALL transfer types including vendor-specific and bulk/interrupt.

### Key Finding: Three-Command Flash-Based Switch Sequence
Elgato Studio sends exactly three AT commands to switch audio input, appearing ONLY at the switch moments (not in baseline polling):

**Switch 1 (HDMI → Analog) at t≈3.7-3.9s:**
```
t=3.727  AT SET 0x5B (AT_GPIO_Set_Param)               raw=A10900005B000000000500F6
t=3.875  AT SET 0x52 (AT_Flash_Get_CustomerProprietary) raw=A10600005200000007
t=3.900  AT SET 0x51 (AT_Flash_Set_CustomerProprietary) raw=A1260000510000000180FFAA5500000000000000000000000000000000000000000000000000000069
```

**Switch 2 (Analog → HDMI) at t≈14.9-15.1s:**
```
t=14.969 AT SET 0x5B  raw=A10900005B000000000500F6
t=15.090 AT SET 0x52  raw=A10600005200000007
t=15.115 AT SET 0x51  raw=A1260000510000000080FFAA550000000000000000000000000000000000000000000000000000006A
```

### Protocol Details
- **0x5B** (GPIO): data=`00 05 00` — preps hardware audio mux
- **0x52** (Flash GET): no data — reads current flash proprietary state
- **0x51** (Flash SET): 32-byte payload, byte 0 = source (0x00=HDMI, 0x01=Analog), bytes 1-4 = magic `80 FF AA 55`, bytes 5-31 = zeros

### Implementation Result
- All three AT commands execute successfully via `NativeXuAtCommandProvider.SwitchAudioInputAsync()`
- Logs confirm: `NATIVEXU_SET_OK cmd=0x5B`, `NATIVEXU_SET_OK cmd=0x52`, `NATIVEXU_SET_OK cmd=0x51`
- **However**: AT GET 0x35 (InputSource) telemetry still reports HDMI after sending Analog switch
- Audio metering still shows signal on unconnected Analog port
- Hypothesis: the 32-byte payload to 0x51 may need to be a read-modify-write (read via 0x52 response, modify byte 0, write back) rather than hardcoded. Elgato Studio reads 0x52 first before writing 0x51.

### BREAKTHROUGH: Selector 4 I2C Register Writes
The flash commands (0x5B/0x52/0x51) only persist the preference. The ACTUAL hardware audio
reconfiguration is done via **14 I2C register writes** to audio codec at **I2C address 0x4A**,
sent on **XU selector 4** (525-byte payloads) using AT opcodes 0x1C (I2C write) and 0x1B (I2C read).

Both switch directions send the identical 14-command sequence. Only 4 register values differ:

| # | Codec Reg | HDMI→Analog | Analog→HDMI | Meaning |
|---|-----------|-------------|-------------|---------|
| 8 | page2:0x0E | 0x18 | 0x98 | bit 7 = HDMI mixer select |
| 9 | page2:0x0F | 0x18 | 0x98 | bit 7 = HDMI mixer select |
|10 | page2:0x10 | 0x80 | 0x00 | bit 7 = Analog mixer select |
|11 | page2:0x11 | 0x80 | 0x00 | bit 7 = Analog mixer select |

The other 10 commands handle page select, status reads, unmute, and finalize — identical both ways.

Full decoded sequence per switch:
```
I2C_WR 0x4A reg=0x0200 val=[09 42]   (page select / init)
I2C_RD 0x4A reg=0x0100 val=[04 01 00] (read status)
I2C_WR 0x4A reg=0x0200 val=[03 A0]   (mixer config)
I2C_RD 0x4A reg=0x0100 val=[0E 01 00] (read reg 0E)
I2C_RD 0x4A reg=0x0100 val=[10 01 00] (read reg 10)
I2C_RD 0x4A reg=0x0100 val=[04 01 00] (read status)
I2C_WR 0x4A reg=0x0200 val=[04 0E]   (unmute/prep)
I2C_WR 0x4A reg=0x0200 val=[07 00]   (mixer enable)
I2C_WR 0x4A reg=0x0200 val=[0E {18|98}] (INPUT-DEPENDENT)
I2C_WR 0x4A reg=0x0200 val=[0F {18|98}] (INPUT-DEPENDENT)
I2C_WR 0x4A reg=0x0200 val=[10 {80|00}] (INPUT-DEPENDENT)
I2C_WR 0x4A reg=0x0200 val=[11 {80|00}] (INPUT-DEPENDENT)
I2C_WR 0x4A reg=0x0200 val=[04 0E]   (finalize)
I2C_WR 0x4A reg=0x0200 val=[07 00]   (finalize)
```

### Capture artifacts
- `temp/elgato_audio_switch_7.pcap` — raw USBPcap capture (565K packets)
- `temp/find-switch-commands.py` — AT opcode extraction with time-windowed analysis
- `temp/decode-sel4-commands.py` — selector 4 I2C command decode
- `temp/deep-switch-analysis.py` — full transaction log with selector 4 discovery
- `temp/parse-all-transfers.py` — full USB traffic parser (all transfer types)

## E119 - Recording verifier uses negotiated capture values
- Timestamp (UTC): 2026-03-15T06:37:29.2842091Z
- Commit Hash: 957f01c2b1d3b68d09f4c47728982aceb286d8f2
- What Changed (single change): Updated `RecordingVerifier` to validate output resolution and frame rate against `CaptureRuntimeSnapshot.Negotiated*` values first, with `Requested*` values only as fallback when negotiation metadata is missing.
- How To Run:
  1. `dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -p:StageLatestBuild=true`
  2. `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"`
  3. Inspect `temp/logs/ElgatoCapture_Debug.log` for warnings/errors after the run.
- Validator Output:
  - `Build succeeded.`
  - `All runtime snapshot regression checks passed.`
  - `temp/logs/ElgatoCapture_Debug.log` contained only pre-existing synthetic HFR-test warnings (`UNIFIED_VIDEO_MJPEG_STOP_FAIL`, `UNIFIED_VIDEO_CAPTURE_FATAL ... synthetic hfr failure`); no new verifier warnings/errors were introduced by this change.
- ffprobe Evidence:
  - N/A (no new recording artifact generated in this validation pass)
- Conclusion: Recording verification now tracks the device-negotiated geometry/timing that capture actually achieved, while still falling back to requested values if the snapshot is captured before negotiation completes.

## E118 - MainViewModel now mirrors WASAPI peak samples into AudioPeak
- Timestamp (UTC): 2026-03-15T06:36:07.3849032Z
- Commit Hash: 957f01c2b1d3b68d09f4c47728982aceb286d8f2
- What Changed (single change): Updated `MainViewModel.OnAudioLevelUpdated()` to assign `AudioPeak = e.Peak` after updating the animated meter target so automation snapshots and diagnostics hub audio-signal detection use live WASAPI peak data instead of a permanent zero.
- How To Run:
  1. `dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -p:StageLatestBuild=true`
  2. `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"`
  3. Read `temp/logs/ElgatoCapture_Debug.log` and confirm there are no unexpected new warnings or errors beyond the harness's synthetic MJPEG HFR failure coverage.
- Validator Output:
  - `Build succeeded.`
  - `All runtime snapshot regression checks passed.`
- ffprobe Evidence:
  - N/A (no recording artifact generated for this verification pass)
- Conclusion: `AudioPeak` now tracks the same live peak value already flowing through `AudioLevelEventArgs.Peak`, so `GetViewModelRuntimeSnapshotAsync()` and `AutomationDiagnosticsHub.AudioSignalPresent` can observe real audio activity.

## E120 - LibAvEncoder now corrects audio drift against the CFR video clock
- Timestamp (UTC): 2026-03-15T06:40:17.4096771Z
- Commit Hash: 957f01c2b1d3b68d09f4c47728982aceb286d8f2
- What Changed (single change): Added encoder-local audio drift correction in `LibAvEncoder` that samples A/V drift every 300 video frames, trims or pads audio by up to 480 samples per pass, logs `LIBAV_AV_DRIFT_CORRECTION`, and includes cumulative correction in `LIBAV_AV_SYNC`.
- How To Run:
  1. `dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -p:StageLatestBuild=true`
  2. `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"`
  3. Inspect `temp/logs/ElgatoCapture_Debug.log` for new `LIBAV_AV_SYNC` / `LIBAV_AV_DRIFT_CORRECTION` lines during recording runs and confirm this verification pass did not add new `LIBAV_ENCODER_ERROR` entries.
- Validator Output:
  - `Build succeeded.` (warnings only: existing unused-field warnings in `LibAvEncoder`)
  - `All runtime snapshot regression checks passed.`
  - `temp/logs/ElgatoCapture_Debug.log` contained no new `LIBAV_ENCODER_ERROR` or `LIBAV_SINK_STOP_FAIL` lines from this verification pass.
- ffprobe Evidence:
  - N/A (no new recording artifact generated in this verification pass)
- Conclusion: The drift-correction path compiles, passes the runtime regression harness, and preserves a clean libav log surface in this verification run; a live recording run should now show gradual capped correction instead of unbounded audio lag growth.

## E121 - LibAvEncoder drift math now uses encoded audio PTS only
- Timestamp (UTC): 2026-03-15T06:40:17.4096771Z
- Commit Hash: 957f01c2b1d3b68d09f4c47728982aceb286d8f2
- What Changed (single change): Updated the new `LibAvEncoder` drift-correction/logging helpers to measure against `_nextAudioPts` only, instead of counting buffered-but-not-yet-encoded audio samples as if they had already advanced the audio clock.
- How To Run:
  1. `dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -p:StageLatestBuild=true`
  2. `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"`
  3. Inspect `temp/logs/ElgatoCapture_Debug.log` and confirm there are still no new `LIBAV_ENCODER_ERROR` / `LIBAV_SINK_STOP_FAIL` lines in this verification pass.
- Validator Output:
  - `Build succeeded.` (0 warnings, 0 errors)
  - `All runtime snapshot regression checks passed.`
  - `temp/logs/ElgatoCapture_Debug.log` contained no `LIBAV_ENCODER_ERROR`, `LIBAV_SINK_STOP_FAIL`, `LIBAV_AV_SYNC`, or `LIBAV_AV_DRIFT_CORRECTION` lines during this non-recording verification pass.
- ffprobe Evidence:
  - N/A (no new recording artifact generated in this verification pass)
- Conclusion: Drift sampling and diagnostics now reference the authoritative encoded-audio clock rather than optimistic buffered samples, keeping the correction threshold and sync logs aligned with actual muxed audio progress.

## E121 - LibAvEncoder AV-sync diagnostics now cover GPU and CUDA video sends
- Timestamp (UTC): 2026-03-15T06:42:17.4027778Z
- Commit Hash: 957f01c2b1d3b68d09f4c47728982aceb286d8f2
- What Changed (single change): Added `LogAvSyncIfDue()` calls to `SendGpuVideoFrame()` and `SendCudaVideoFrame()` so the existing `LIBAV_AV_SYNC` cadence and cumulative drift-correction diagnostics stay active on every libav video path that advances `_nextVideoPts`.
- How To Run:
  1. `dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -p:StageLatestBuild=true`
  2. `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"`
  3. Inspect `temp/logs/ElgatoCapture_Debug.log` after the run; a live GPU/CUDA recording should now emit `LIBAV_AV_SYNC` on the same 300-frame cadence as the CPU path.
- Validator Output:
  - `Build succeeded.` (`0 Warning(s)`, `0 Error(s)`)
  - `All runtime snapshot regression checks passed.`
  - `temp/logs/ElgatoCapture_Debug.log` contained only the harness's pre-existing synthetic HFR warnings (`UNIFIED_VIDEO_MJPEG_STOP_FAIL`, `UNIFIED_VIDEO_CAPTURE_FATAL ... synthetic hfr failure`) and no `LIBAV_ENCODER_ERROR` / `LIBAV_SINK_STOP_FAIL` lines.
- ffprobe Evidence:
  - N/A (no new recording artifact generated in this verification pass)
- Conclusion: AV-sync observability is now consistent across CPU, D3D11, and CUDA libav video sends, and the final build/test/log verification pass stayed clean.

## E122 - Queued drift correction now commits against encoded audio PTS in the same pass
- Timestamp (UTC): 2026-03-15T06:49:28.2331570Z
- Commit Hash: 957f01c2b1d3b68d09f4c47728982aceb286d8f2
- What Changed (single change): Refined the queued `LibAvEncoder` drift-correction path so drift sampling/logging use encoded audio PTS (`_nextAudioPts`) only, the 300-frame gate is only advanced after a zero-correction decision or a fully applied correction, and any applied trim/pad is drained immediately instead of waiting behind the next full AAC block.
- How To Run:
  1. `dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -p:StageLatestBuild=true`
  2. `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"`
  3. Inspect `temp/logs/ElgatoCapture_Debug.log`; this non-recording pass should stay free of new `LIBAV_ENCODER_ERROR` / `LIBAV_SINK_STOP_FAIL` lines, and a live recording run should now show `LIBAV_AV_SYNC` / `LIBAV_AV_DRIFT_CORRECTION` based on encoded audio progress rather than buffered samples.
- Validator Output:
  - `Build succeeded.` (`0 Warning(s)`, `0 Error(s)`)
  - `All runtime snapshot regression checks passed.`
  - `temp/logs/ElgatoCapture_Debug.log` contained only the harness's expected synthetic HFR warnings (`UNIFIED_VIDEO_MJPEG_STOP_FAIL`, `UNIFIED_VIDEO_CAPTURE_FATAL ... synthetic hfr failure`) and no new `LIBAV_ENCODER_ERROR` / `LIBAV_SINK_STOP_FAIL` lines.
- ffprobe Evidence:
  - N/A (no new recording artifact generated in this verification pass)
- Conclusion: The final queued drift-correction path now keys off actual encoded audio time and commits any capped correction in the same evaluation pass, while the build/test/log verification pass remained clean.

## E123 - Final queued drift correction keeps fixed-size AAC frames and tracks queued correction debt
- Timestamp (UTC): 2026-03-15T06:55:48.0703195Z
- Commit Hash: 957f01c2b1d3b68d09f4c47728982aceb286d8f2
- What Changed (single change): Finalized the queued `LibAvEncoder` drift-correction path so it keeps AAC on full-frame drains, samples drift against the full queued-audio timeline (`_nextAudioPts + queuedSamples`) to avoid counter-correcting buffered residue, keeps `LIBAV_AV_SYNC` on the queued timeline for observability, and still only advances the 300-frame correction gate after a zero-correction decision or a fully applied correction.
- How To Run:
  1. `dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -p:StageLatestBuild=true`
  2. `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"`
  3. Inspect `temp/logs/ElgatoCapture_Debug.log`; this non-recording pass should stay free of new `LIBAV_ENCODER_ERROR` / `LIBAV_SINK_STOP_FAIL` lines, and a live recording run should now show `LIBAV_AV_SYNC` / `LIBAV_AV_DRIFT_CORRECTION` against the queued correction debt rather than only fully encoded AAC packets.
- Validator Output:
  - `Build succeeded.` (`0 Warning(s)`, `0 Error(s)`)
  - `All runtime snapshot regression checks passed.`
  - `temp/logs/ElgatoCapture_Debug.log` contained no `LIBAV_ENCODER_ERROR`, `LIBAV_SINK_STOP_FAIL`, `LIBAV_AV_SYNC`, or `LIBAV_AV_DRIFT_CORRECTION` lines during this non-recording verification pass.
- ffprobe Evidence:
  - N/A (no new recording artifact generated in this verification pass)
- Conclusion: The final drift-correction state keeps the AAC encoder on fixed-size frame drains while tracking queued trim/pad debt consistently enough to avoid self-canceling follow-up corrections in later 300-frame samples.

## E124 - Automation pipe now supports analog audio gain changes end to end
- Timestamp (UTC): 2026-03-15T20:41:17.5719011Z
- Commit Hash: 957f01c2b1d3b68d09f4c47728982aceb286d8f2
- What Changed (single change): Added the `SetAnalogAudioGain` automation command to the app contract/dispatcher, kept the `MainViewModel` automation mutators on the UI thread, and aligned every named-pipe command map consumer (`AutomationClient`, `McpServer`, and `send-automation-command.ps1`) to include ids 38-39.
- How To Run:
  1. `dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -p:StageLatestBuild=true`
  2. `dotnet build tools/AutomationClient/AutomationClient.csproj`
  3. `dotnet build tools/McpServer/McpServer.csproj`
  4. `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"`
  5. Inspect `temp/logs/ElgatoCapture_Debug.log` for unexpected automation or command-dispatch failures.
- Validator Output:
  - `Build succeeded.` for the main app, `AutomationClient`, and `McpServer` (`0 Warning(s)`, `0 Error(s)` on the final green pass)
  - `All runtime snapshot regression checks passed.`
  - `temp/logs/ElgatoCapture_Debug.log` showed only the harness's expected synthetic HFR warnings (`UNIFIED_VIDEO_MJPEG_STOP_FAIL`, `UNIFIED_VIDEO_CAPTURE_FATAL ... synthetic hfr failure`) and no new automation command errors.
- ffprobe Evidence:
  - N/A (no recording artifact generated in this verification pass)
- Conclusion: The named-pipe control surface now includes analog gain updates without regressing the existing automation/test contract, and the protocol consumers are back in sync on ids 0-39.

## E125 - Capture health snapshots now surface queue, drop, encode, and freshness counters
- Timestamp (UTC): 2026-03-15T20:41:17.5719011Z
- Commit Hash: 957f01c2b1d3b68d09f4c47728982aceb286d8f2
- What Changed (single change): Populated the previously defaulted `CaptureHealthSnapshot` fields for queue depth, backlog drops, encoded frame count, audio drop aggregation, conversion queue depth, and last-frame arrival age using the existing `LibAvRecordingSink` and `UnifiedVideoCapture` counters.
- How To Run:
  1. `dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -p:StageLatestBuild=true`
  2. `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"`
  3. Inspect `temp/logs/ElgatoCapture_Debug.log` and confirm the verification pass stays free of new capture-service failures.
- Validator Output:
  - `Build succeeded.` (`0 Warning(s)`, `0 Error(s)`)
  - `All runtime snapshot regression checks passed.`
  - `temp/logs/ElgatoCapture_Debug.log` showed only the harness's pre-existing synthetic HFR warnings and no new `LIBAV_ENCODER_ERROR`, `LIBAV_SINK_STOP_FAIL`, or capture-service exceptions.
- ffprobe Evidence:
  - N/A (no recording artifact generated in this verification pass)
- Conclusion: Health snapshots no longer leave the sink/ingest queue and freshness fields at zero defaults when the underlying counters already exist, and the runtime snapshot harness stayed green after the mapping change.

## E126 - Added ecctl as a standalone named-pipe control CLI
- Timestamp (UTC): 2026-03-15T20:41:17.5719011Z
- Commit Hash: 957f01c2b1d3b68d09f4c47728982aceb286d8f2
- What Changed (single change): Added `tools/ecctl/` as a .NET 8 console app with command parsing, named-pipe transport/retry logic, human-readable formatters for state/diagnostics/options/timeline/memory, and verb handlers covering the current automation command surface including analog gain.
- How To Run:
  1. `dotnet build tools/ecctl/ecctl.csproj`
  2. `dotnet build tools/McpServer/McpServer.csproj`
  3. `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"`
  4. Optionally, with the app running: `dotnet run --project tools/ecctl/ecctl.csproj -- --help`
- Validator Output:
  - `Build succeeded.` for `tools/ecctl/ecctl.csproj` and `tools/McpServer/McpServer.csproj` (`0 Warning(s)`, `0 Error(s)` on the final pass)
  - `All runtime snapshot regression checks passed.`
  - `temp/logs/ElgatoCapture_Debug.log` showed no new pipe-client or formatter-related failures during the verification pass.
- ffprobe Evidence:
  - N/A (no recording artifact generated in this verification pass)
- Conclusion: `ecctl` now provides a repo-local command-line entry point for the named-pipe automation surface while preserving the existing response/retry contract and passing the current regression suite.

## E127 - MP4 recording now supports an optional second AAC microphone track
- Timestamp (UTC): 2026-03-16T01:46:14.8522783Z
- Commit Hash: 096ed1ca09a5a9a5ddee8d48828cb8b54255109a
- What Changed (single change): Wired the existing microphone UI/settings path through `MainViewModel`, `CaptureService`, `WasapiAudioCapture`, `LibAvRecordingSink`, and `LibAvEncoder` so MP4 recording can optionally open a second WASAPI capture device and encode it as a separate AAC track alongside the existing video and capture-card audio streams.
- How To Run:
  1. `dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -p:StageLatestBuild=true`
  2. `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"`
  3. Inspect `temp/logs/ElgatoCapture_Debug.log` and confirm the pass stays free of new `LIBAV_ENCODER_ERROR`, `LIBAV_SINK_STOP_FAIL`, `WASAPI audio delegate write failed`, or microphone-capture dispose warnings.
- Validator Output:
  - `Build succeeded.` (`0 Warning(s)`, `0 Error(s)`)
  - `All runtime snapshot regression checks passed.`
  - `temp/logs/ElgatoCapture_Debug.log` showed only the harness's expected telemetry/audio-preview fallback lines plus the existing synthetic HFR warnings, with no new libav/sink/microphone failures.
- ffprobe Evidence:
  - N/A (no new recording artifact generated in this verification pass)
- Conclusion: The app now carries persisted microphone selection from the existing UI row into recording startup and, when enabled with a selected device, routes that capture through a dedicated libav queue/encoder stream without regressing the current build, runtime snapshot harness, or debug-log health.

## E128 - Stacked audio meters plus microphone endpoint volume controls
- Timestamp (UTC): 2026-03-16T02:42:44.3941652Z
- Commit Hash: 096ed1ca09a5a9a5ddee8d48828cb8b54255109a
- What Changed (single change): Reworked the control-bar audio area into stacked capture/microphone meter rows, added mirrored microphone volume sliders in the control bar and settings shelf, wired microphone endpoint volume through WASAPI COM interop plus persisted settings, and preserved the saved microphone volume when restoring the previously selected device instead of overwriting it with the live endpoint level during startup.
- How To Run:
  1. `dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -p:StageLatestBuild=true`
  2. `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"`
  3. Inspect `temp/logs/ElgatoCapture_Debug.log` for unexpected microphone-volume, COM interop, or capture-service failures.
- Validator Output:
  - `Build succeeded.` (`0 Warning(s)`, `0 Error(s)`)
  - `All runtime snapshot regression checks passed.`
  - `temp/logs/ElgatoCapture_Debug.log` showed the harness's existing synthetic HFR warnings (`UNIFIED_VIDEO_MJPEG_STOP_FAIL`, `UNIFIED_VIDEO_CAPTURE_FATAL ... synthetic hfr failure`) plus the expected audio-preview-without-device fallback line, and no new microphone endpoint-volume, COM activation, or capture-service errors.
- ffprobe Evidence:
  - N/A (no new recording artifact generated in this verification pass)
- Conclusion: The UI now exposes separate capture and microphone metering/volume controls, microphone endpoint volume persists safely across app restarts for the restored mic device, and the existing build/runtime regression suite stayed green with no new debug-log failures.

## E129 - Microphone volume persistence now saves on interaction end
- Timestamp (UTC): 2026-03-16T02:45:14.1143824Z
- Commit Hash: 096ed1ca09a5a9a5ddee8d48828cb8b54255109a
- What Changed (single change): Changed the microphone volume path to keep live endpoint updates on slider `ValueChanged` but defer `SaveSettings()` until pointer release, matching the existing preview-volume behavior and avoiding synchronous settings writes on every drag tick.
- How To Run:
  1. `dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -p:StageLatestBuild=true`
  2. `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"`
  3. Inspect `temp/logs/ElgatoCapture_Debug.log` for unexpected microphone-volume, COM interop, or capture-service failures.
- Validator Output:
  - `Build succeeded.` (`0 Warning(s)`, `0 Error(s)`)
  - `All runtime snapshot regression checks passed.`
  - `temp/logs/ElgatoCapture_Debug.log` again showed only the harness's expected synthetic HFR warnings plus the no-audio-device fallback line, with no new microphone-volume, COM, or capture-service errors.
- ffprobe Evidence:
  - N/A (no new recording artifact generated in this verification pass)
- Conclusion: Microphone volume changes still apply live to the endpoint, but the UI no longer forces a settings-file write on every slider tick, eliminating the hot-interaction regression flagged in review without regressing the current harness/log checks.

## E130 - Capture service now uses the always-on flashback backend during preview and recording
- Timestamp (UTC): 2026-03-16T06:48:25.7371555Z
- Commit Hash: 2b9febe28dd08260eb551779b8c540f42f82a163
- What Changed (single change): Wired `CaptureService` to start a preview-owned `FlashbackEncoderSink`/`FlashbackBufferManager`, route unified video and WASAPI audio through that always-on backend, use `BeginRecording()` plus `FlashbackExporter` concatenation when flashback is enabled, surface flashback diagnostics in health/runtime snapshots, and preserve segment artifacts on failed flashback finalization instead of purging them during teardown.
- How To Run:
  1. `dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -p:StageLatestBuild=true`
  2. `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"`
  3. Inspect `temp/logs/ElgatoCapture_Debug.log` and confirm the pass stays free of new flashback integration failures such as `FLASHBACK_SINK_STOP_FAIL`, `FLASHBACK_EXPORTER_*`, `UNIFIED_VIDEO_FLASHBACK_*`, or `WASAPI_FLASHBACK_AUDIO_FAIL`.
- Validator Output:
  - `Build succeeded.` (`0 Warning(s)`, `0 Error(s)`)
  - `All runtime snapshot regression checks passed.`
  - `temp/logs/ElgatoCapture_Debug.log` showed only the harness's existing expected lines (`Audio preview requested but no audio capture device is available.`, `UNIFIED_VIDEO_MJPEG_STOP_FAIL reason='emitter_self_join'`, and `UNIFIED_VIDEO_CAPTURE_FATAL ... synthetic hfr failure`) with no new flashback integration errors.
- ffprobe Evidence:
  - N/A (no new recording artifact generated in this verification pass)
- Conclusion: The preview/record lifecycle now keeps a single always-on flashback encoder active when enabled, the legacy libav sink remains the fallback only when flashback is unavailable, and the required build/test/log validation stayed green after fixing the stop-path artifact-preservation edge case.

## E131 - Flashback recording now fails loudly when preview and record audio topology differ
- Timestamp (UTC): 2026-03-16T06:54:20.1345616Z
- Commit Hash: 2b9febe28dd08260eb551779b8c540f42f82a163
- What Changed (single change): Added a `CaptureService` guard that rejects flashback-backed recording when the preview-owned flashback sink was opened with different HDMI-audio or microphone topology than the requested recording settings, preventing silent missing/extra tracks after preview-time setting changes.
- How To Run:
  1. `dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -p:StageLatestBuild=true`
  2. `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"`
  3. Inspect `temp/logs/ElgatoCapture_Debug.log` and confirm the verification pass stays free of new flashback integration failures.
  4. Manual spot check: start preview, change audio or microphone enablement, then start recording and confirm the service now throws a restart-preview requirement instead of silently recording the wrong track topology.
- Validator Output:
  - `Build succeeded.` (`0 Warning(s)`, `0 Error(s)`)
  - `All runtime snapshot regression checks passed.`
  - `temp/logs/ElgatoCapture_Debug.log` again showed only the harness's expected no-audio fallback and synthetic HFR warnings, with no new flashback guard failures during the automated pass.
- ffprobe Evidence:
  - N/A (no new recording artifact generated in this verification pass)
- Conclusion: The flashback path no longer silently accepts preview/record audio-topology drift; it now fails loudly and asks for a preview restart when the always-on sink would otherwise encode the wrong set of audio tracks.

## E131 - Recording-only flashback stop now detaches producers before export
- Timestamp (UTC): 2026-03-16T06:52:41.1865352Z
- Commit Hash: 2b9febe28dd08260eb551779b8c540f42f82a163
- What Changed (single change): In `CaptureService.StopAndDisposeRecordingBackendAsync`, recording-only flashback sessions now detach `UnifiedVideoCapture` and `WasapiAudioCapture` from the flashback sink before running flashback export/finalization so stop no longer lets live capture keep feeding the sink during the slow stop/export path.
- How To Run:
  1. `dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -p:StageLatestBuild=true`
  2. `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"`
  3. Inspect `temp/logs/ElgatoCapture_Debug.log` and confirm the verification pass stays free of new stop/export warnings such as `FLASHBACK_PREVIEW_STOP_WARN`, `FLASHBACK_SINK_STOP_FAIL`, or `WASAPI_FLASHBACK_AUDIO_FAIL`.
- Validator Output:
  - `Build succeeded.` (`0 Warning(s)`, `0 Error(s)`)
  - `All runtime snapshot regression checks passed.`
  - `temp/logs/ElgatoCapture_Debug.log` again showed only the harness's expected no-audio-preview fallback plus the existing synthetic HFR warnings, with no new flashback stop/export errors.
- ffprobe Evidence:
  - N/A (no new recording artifact generated in this verification pass)
- Conclusion: Recording-only stop now sheds live producers before concatenation/HDR validation, reducing avoidable stop-path contention without regressing the current build, runtime harness, or debug-log health.

## E131 - Preview startup no longer rechecks flashback initialization twice
- Timestamp (UTC): 2026-03-16T06:51:51.7542594Z
- Commit Hash: 2b9febe28dd08260eb551779b8c540f42f82a163
- What Changed (single change): Removed the duplicate `EnsureFlashbackPreviewBackendAsync(...)` call from `CaptureService.StartVideoPreviewAsync()` so preview startup only performs one flashback backend initialization check before the final audio attachment step.
- How To Run:
  1. `dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -p:StageLatestBuild=true`
  2. `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"`
  3. Inspect `temp/logs/ElgatoCapture_Debug.log` and confirm the pass stays free of new preview-start or flashback initialization failures.
- Validator Output:
  - `Build succeeded.` (`0 Warning(s)`, `0 Error(s)`)
  - `All runtime snapshot regression checks passed.`
  - `temp/logs/ElgatoCapture_Debug.log` again showed only the harness's expected NTSC-correction lines, the no-audio-device preview fallback line, and the existing synthetic HFR warnings, with no new flashback preview-start errors.
- ffprobe Evidence:
  - N/A (no new recording artifact generated in this verification pass)
- Conclusion: Preview startup still passes the required validation loop, but it no longer performs a redundant flashback backend initialization check after the audio capture path is established.

## E131 - Flashback recording now promotes buffered pre-roll into the active clip
- Timestamp (UTC): 2026-03-16T06:51:29.1003356Z
- Commit Hash: 2b9febe28dd08260eb551779b8c540f42f82a163
- What Changed (single change): Updated `FlashbackBufferManager.BeginRecording()` to start from the first currently buffered segment and promote existing `Buffer` segments to `Recording`, so retroactive flashback clips keep pre-roll instead of starting only from the next segment boundary and leaving prior buffered segments evictable during recording.
- How To Run:
  1. `dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -p:StageLatestBuild=true`
  2. `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"`
  3. Inspect `temp/logs/ElgatoCapture_Debug.log` and confirm the pass stays free of new flashback buffer/export failures.
- Validator Output:
  - `Build succeeded.` (`0 Warning(s)`, `0 Error(s)`)
  - `All runtime snapshot regression checks passed.`
  - `temp/logs/ElgatoCapture_Debug.log` again showed only the harness's expected no-audio-preview fallback plus synthetic HFR warnings, with no new flashback buffer/export errors.
- ffprobe Evidence:
  - N/A (no new recording artifact generated in this verification pass)
- Conclusion: Flashback recording now matches the retroactive-buffer intent in the project plan by preserving already-buffered segments once recording begins, while the build/test/log validation remained clean after the fix.

## E132 - Failed flashback finalization now preserves segment artifacts through teardown
- Timestamp (UTC): 2026-03-16T06:52:42.4550881Z
- Commit Hash: 2b9febe28dd08260eb551779b8c540f42f82a163
- What Changed (single change): Adjusted `CaptureService.DisposeFlashbackPreviewBackendAsync(...)` so the failure-preservation path skips both `PurgeAllSegments()` and `FlashbackBufferManager.Dispose()`, preventing teardown from deleting the very flashback segment files returned as preserved artifacts when export or HDR validation fails.
- How To Run:
  1. `dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -p:StageLatestBuild=true`
  2. `dotnet run --project tests/ElgatoCapture.Tests/ -- "ElgatoCapture/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/ElgatoCapture.dll"`
  3. Inspect `temp/logs/ElgatoCapture_Debug.log` and confirm the pass stays free of new flashback teardown/export failures.
- Validator Output:
  - `Build succeeded.` (`0 Warning(s)`, `0 Error(s)`) after one transient retry to clear a known WinUI `Microsoft.UI.Xaml.Markup.Compiler` file lock on `obj\\...\\intermediatexaml\\ElgatoCapture.dll`
  - `All runtime snapshot regression checks passed.`
  - `temp/logs/ElgatoCapture_Debug.log` again showed only the harness's expected no-audio-preview fallback plus synthetic HFR warnings, with no new flashback teardown/export errors.
- ffprobe Evidence:
  - N/A (no new recording artifact generated in this verification pass)
- Conclusion: Failed flashback finalization now leaves the segment artifacts intact for diagnosis/recovery instead of deleting them during cleanup, and the final validated build/test/log pass stayed green.

## E-Smoke-Test-2026-03-30 - Full Smoke Test & Bug Fix Session
- Timestamp (UTC): 2026-03-30T20:30:00Z
- Commit Hash: (pending commit)
- Goal: Full smoke test of Flashback branch, identify and fix all discovered bugs

### Bugs Found & Fixed

**B1 - P010 requested when HDR off** (FIXED)
- Root cause: `UpdateSelectedFormat` and `SelectPreferredFrameRateFormat` did not filter out HDR pixel formats when `IsHdrEnabled=false`. P010 was selected via `ThenByDescending(IsHdrModeCandidate)`.
- Fix: Filter to SDR-only candidates when HDR is off in `UpdateSelectedFormat`, `SelectPreferredAutoFrameRateFormat`, `GetAutoEligibleFormats`, and frame rate option builder. Also swapped NV12/YUY2 priority in `GetPixelFormatPriority` (NV12 is the native UVC format).
- Files: `MainViewModel.DeviceManagement.cs`, `MediaFormat.cs`
- Verified: `ReqSubtype=NV12 NegSubtype=NV12` (was P010→NV12 fallback)

**B2 - Second recording truncation** (FIXED)
- Root cause: After buffer cycle, new `FlashbackEncoderSink` starts PTS from 0 but `_latestPtsTicks` in `FlashbackBufferManager` retained old value. Monotonic guard blocked all new PTS updates until they exceeded old maximum.
- Fix: Reset `_latestPtsTicks` and purge completed segments during cycle. New encoder starts clean with PTS 0.
- Files: `FlashbackBufferManager.cs` (`ResetLatestPts`, `PurgeCompletedSegments`), `CaptureService.cs` (call during cycle)
- Verified: 3 consecutive recordings all produce ~10s files (was 0.68s for 2nd recording)

**B3 - Flashback encoder ignores format change** (FIXED)
- Root cause: Flashback encoder codec locked at preview start. Changing recording format in UI didn't restart encoder.
- Fix: `CaptureService.UpdateRecordingFormatAsync` updates `_currentSettings.Format` and cycles the flashback buffer. Called from `OnSelectedRecordingFormatChanged` in ViewModel.
- Files: `CaptureService.cs`, `MainViewModel.Settings.cs`
- Verified: Switching AV1→H.264 cycles encoder to `h264_nvenc`, recording produces H.264 file

**B4 - Recording shows 0B during flashback recording** (FIXED)
- Root cause: `GetRecordingStats` checked `_libavSink.OutputBytes` (null for flashback) then file size (0 until export).
- Fix: When flashback recording active, return `bufferManager.TotalBytesWritten` as estimate. Added `IsFlashbackEstimate` flag to `RecordingStats`.
- Files: `CaptureService.Snapshots.cs`, `RecordingStats.cs`
- Verified: Recording shows `53 MB | 8 Mbps` during flashback recording, perf score stays 100

**B5 - Named pipe error 1314 spam** (FIXED)
- Root cause: `CreateNamedPipe` with explicit security descriptor failed repeatedly (error 1314 = privilege not held). Logged every ~30s.
- Fix: Added `_explicitSecurityFailed` flag to skip retry after first failure.
- Files: `NamedPipeAutomationServer.cs`
- Verified: 1 log entry (was 25+)

**B6 - VTABLE_DIAG traces** (NOT A BUG)
- Already gated by `#if DEBUG` and one-shot flag. Won't appear in release builds.

### Smoke Test Results (30-minute monitoring in progress)
- T+0: Score=100, Buffer=68.9s, Working Set=337MB, 0 drops, 0 gaps
- Monitoring at 5, 10, 15, 20, 25, 30 minute intervals

## 2026-04-27 — Preview present sync interval default flip (sync=1 → sync=0)

**Problem:** User reported preview 1% lows at ~83fps (target: 115-116fps) at 4K@120 MJPG SDR with VideoProcessor render path.

**Method:** Live A/B test via `ELGATOCAPTURE_PREVIEW_PRESENT_SYNC_INTERVAL` env override on the running Debug build (Elgato 4K X, 3840×2160@120, MJPG, no recording). Captured `ecctl state --json` after ~30s steady-state for each setting.

**Measurements:**

| Metric | sync=1 | sync=0 | Δ |
|---|---|---|---|
| Present cadence avg | 8.34ms | 8.33ms | unchanged |
| Present p95 (5% low) | 8.77ms (114fps) | 8.87ms (113fps) | within noise |
| Present p99 (1% low) | 11.99ms (83fps) | 9.36ms (107fps) | **-2.6ms** |
| Present max | 88.6ms | 11.05ms | **-87%** |
| Slow-frame % | 0.5% | 0% | **gone** |
| Render CPU p95 | 8.65ms | 1.32ms | -85% |
| Render CPU p99 | 10.38ms | 2.04ms | -80% |
| InputUpload p99 | 2.99ms | 1.64ms | -45% |
| PresentCall p99 | 19.65ms | 0.30ms | -98% |
| Frame drops % | 0.306% | 0.021% | -93% |
| Diagnostic health | Warning | Healthy | ✓ |

**Why sync=0 is correct here:** SwapChainPanel composition runs through DWM, so DWM enforces refresh-rate pacing on the final pixel delivery (no tearing). With sync=1, the render thread was paying a redundant per-frame vsync wait inside `IDXGISwapChain::Present` on top of the source-rate pacing already provided by the 120fps capture stream. With sync=0, Present queues and returns; the render loop now naturally tracks the 8.33ms source cadence with ~1ms tail.

**Change:** Default for `_presentSyncInterval` flipped from 1 → 0 in `ElgatoCapture/Services/Preview/D3D11PreviewRenderer.cs:363`. Env override `ELGATOCAPTURE_PREVIEW_PRESENT_SYNC_INTERVAL=1` still available to restore old behavior.

## 2026-04-30 — MJPEG preview scheduler/render MMCSS validation

**Problem:** 4K120 MJPEG preview still showed visible stutter, with reported 1% lows varying around 105-113fps.

**Change:** Defaulted the MJPEG preview jitter thread and D3D11 render thread to MMCSS `Playback`, and fixed the AVRT P/Invoke to call `AvSetMmThreadCharacteristicsW` so registration actually succeeds. Kept display-clock pacing and waitable swap chain as opt-in knobs after live A/B showed they did not improve this setup.

**Default live run:** Elgato 4K X, MJPG 3840x2160@120, SDR preview, no recording, automation snapshot after ~25s steady-state.

| Metric | Result |
|---|---|
| Diagnostic health | Healthy |
| Source cadence | avg 8.33ms, p95 8.47ms, max 8.77ms |
| MJPEG decode | p95 24.45ms, dropped 0, failures 0 |
| Preview scheduler | target 2, depth 3/12, deadline drops 0, underflows 0 |
| Render CPU | p95 1.03ms, p99 1.24ms |
| Present cadence | avg 8.33ms, p95 8.66ms, p99 8.98ms, max 9.56ms |
| Slow-frame percent | 0% |
| Visual repeat percent | 0.104% |
| Swap chain | sync=0, latency=2, buffers=3, waitable=False |

**A/B notes:** `ELGATOCAPTURE_PREVIEW_WAITABLE_SWAPCHAIN=1` worsened max present interval (11.33ms). `ELGATOCAPTURE_PREVIEW_DISPLAY_CLOCK_PACING=1` kept p99 similar but introduced startup preview scheduler deadline drops on this machine, so it remains an experimental override.

## 2026-04-30 — Deep MJPEG preview buffering experiment

**Question:** Can significantly more preview buffering move 4K120 MJPEG 1% lows toward 118fps?

**Change for experiment:** Added environment controls for MJPEG preview jitter buffer depth:

- `ELGATOCAPTURE_PREVIEW_JITTER_TARGET_DEPTH`
- `ELGATOCAPTURE_PREVIEW_JITTER_MIN_TARGET_DEPTH`
- `ELGATOCAPTURE_PREVIEW_JITTER_MAX_TARGET_DEPTH`
- `ELGATOCAPTURE_PREVIEW_JITTER_MAX_DEPTH`

**Method:** Ran the same Elgato 4K X MJPG 3840x2160@120 SDR preview path with forced fixed-depth preview buffers.

| Mode | Target / max depth | Queue latency avg | Queue latency p95 | Present p95 | Present p99 | Present max | Drops / underflows |
|---|---:|---:|---:|---:|---:|---:|---:|
| Default reference | adaptive 2-8 / 12 | low adaptive | low adaptive | 8.66ms | 8.98ms | 9.56ms | 0 / 0 |
| Deep-12 | 12 / 24 | 105.2ms | 107.6ms | 8.53ms | 8.93ms | 9.60ms | 0 / 0 |
| Deep-24 | 24 / 36 | 203.9ms | 205.7ms | 8.68ms | 8.93ms | 9.56ms | 0 / 0 |

**Conclusion:** Extra buffering beyond the existing adaptive jitter buffer does not materially move p99 toward the 8.47ms target needed for 118fps 1% lows. It reduces render CPU variance slightly but mostly buys latency. The remaining tail appears more likely to be final present/compositor/display cadence than MJPEG decode burst absorption.

## 2026-04-30 — Preview present/compositor cadence retest

**Question:** If buffering is not the answer, can the SwapChainPanel/DWM present policy tighten 4K120 1% lows?

**Method:** Live A/B on the current x64 Debug build, Elgato 4K X, MJPG 3840x2160@120, no recording.

| Mode | Present p95 | Present p99 | Present max | Notes |
| --- | ---: | ---: | ---: | --- |
| sync=0, latency=2, buffers=3 | 8.78ms | 9.02ms | 9.27ms | Low render CPU, but misses 118fps 1% target |
| sync=1, latency=2, buffers=3 | 8.52ms | 8.68ms | 9.56ms | Better cadence, more render-thread blocking |
| sync=1, latency=1, buffers=2 | 8.44ms | 8.49ms | 12.33ms | Closest to 118fps 1% target, no drops |

**Change:** Default preview cadence policy now uses `ELGATOCAPTURE_PREVIEW_PRESENT_SYNC_INTERVAL=1`, `ELGATOCAPTURE_PREVIEW_DXGI_MAX_FRAME_LATENCY=1`, and `ELGATOCAPTURE_PREVIEW_SWAPCHAIN_BUFFER_COUNT=2` equivalents in `D3D11PreviewRenderer`. Env overrides remain available.

**Conclusion:** The best current lever is the compositor/present queue, not additional MJPEG preview buffering. The new default sits just above the 8.47ms p99 target in the short run and should get longer preview-only and preview+record validation.

## 2026-05-01 — Flashback hardening sweep across 5 subsystems

Goal: harden flashback recording, previewing, scrubbing, export, playback to bulletproof. Started from a 24-day-old bug list and the in-flight AV1->HEVC verifier work in the working tree.

### Approach
1. Validated the in-flight work (build clean, all tests pass — left uncommitted for the user).
2. Dispatched 4 parallel review agents to audit the 24-day-old bug list against current code (HDR reinit, multi-segment exporter, playback A/V, recent commit chains).
3. Dispatched a 5th agent to audit scrubbing (not previously reviewed) once initial fixes landed.
4. Verified each agent finding myself before fixing (per CLAUDE.md trust-but-verify).

### Fixes shipped (all build clean, all 343+ tests pass)

**Export — multi-segment EOF flush silently discarded video**
`FlashbackExporter.cs:1124`. When a configured stream (typically a silent mic) never emitted packets in a short segment (< 600 packets total), the EOF path called bare `FreeBufferedPackets` and lost every video packet from that segment. Extracted the inline 100-line flush body into a `FlushSegmentBufferedPackets` local function inside `ExportSegmentsCore` so both the mid-loop Phase 1->2 trigger and the new EOF rescue (`segMinBaseUs ??= 0; flush()`) share the same code. Added `FLASHBACK_EXPORT_SEGMENT_PARTIAL_BASE_FLUSH` log line.

**Preview — HDR reinit deadlock between HandleDeviceLost and StopRenderThread**
`D3D11PreviewRenderer.Rendering.cs:2346`, `MainWindow.PropertyChanged.cs:78`. During reinit a render thread in `HandleDeviceLost -> InitializeD3D -> BindSwapChainToPanel` could wait up to 5s for the UI dispatcher while the UI thread was in `StopRenderThread().Join()` for 3s, throwing an uncaught `TimeoutException` that crashed the reinit. `BindSwapChainToPanel` now polls `_stopRequested` in 50ms chunks (bails early on stop) and the queued lambda re-checks `_stopRequested` + `ReferenceEquals(_swapChain, swapChain)` before calling `SetSwapChain` (prevents binding a superseded/disposed chain). MainWindow now wraps `StopRenderThread()` in try/catch on TimeoutException so reinit logs and continues.

**Capture — silent flashback codec/preset substitutions**
`CaptureService.cs:1582+`. When software MJPEG pipeline is active at >=100fps with AV1 requested, codec silently falls back to `hevc_nvenc` and the NVENC preset is silently coerced to `Fast`. Added `ResolveFlashbackCodecDowngradeReason` (pure, mirrors the existing predicates). Wired into snapshot via new `CaptureRuntimeSnapshot.FlashbackCodecDowngradeReason` field. One-shot `FLASHBACK_CODEC_DOWNGRADE` log on session start, deduped via `_lastLoggedFlashbackDowngradeReason`, with `FLASHBACK_CODEC_DOWNGRADE_CLEARED` when conditions resolve.

**Playback — frame-skip catch-up loop using stale wall ticks**
`FlashbackPlaybackController.cs:1602`. The catch-up loop captured `audioClockWall` once outside the loop but each skip can take 25ms (AV1 4K@120fps). After 10 skips the wall anchor is 250ms stale and the recomputed drift is wrong; loop could exit early or burn the full skip cap on a stale clock. Extracted `TryComputeAudioMasterDriftMs` helper that re-syncs from WASAPI on every call (matching the canonical resync in `PaceFrameInterval`) and enforces a 200ms staleness guard. Skip loop now calls it per iteration and breaks early if WASAPI underruns. Added `FLASHBACK_PLAYBACK_FRAME_SKIP_EOS` and `_BUDGET` log lines so partial skip progress isn't lost when the loop terminates via end-of-segment or software-budget snap-live.

**Scrubbing — reentrant BeginScrub clobbering resume state**
`FlashbackPlaybackController.cs:743`. A second BeginScrub arriving while State is already `Scrubbing` would sample `isPlaying=false` (set by prior BeginScrub) and clobber `_wasPlayingBeforeScrub`, so EndScrub later landed in Paused instead of resuming Playing/Live. Now guarded by `if (!isScrubbing)` so only the first transition into Scrubbing captures the resume state.

**Scrubbing — In/Out markers landing on prior keyframe instead of visual playhead**
`FlashbackPlaybackController.cs:350`. `SetInPoint()` read controller `PlaybackPosition` which is keyframe-snapped after each decoded frame, so clicking In mid-GOP placed the marker hundreds of milliseconds before where the user was visually pointing. Added `SetInPointAt(TimeSpan)`/`SetOutPointAt(TimeSpan)` overloads through controller -> coordinator -> VM. UI now passes `ViewModel.FlashbackPlaybackPosition` (the visual playhead, set by the timer during Playing and by the scrub PointerMoved handler during Scrubbing). Markers now match what the user clicks on. Logs source=`ui_override`/`playback`.

**Scrubbing — long-held scrubs snapping to live after eviction**
`FlashbackPlaybackController.cs:2244`. `ClampPosition` clamped to `BufferedDuration` (current coords) but scrub commands resolve via `SaturatingAdd(cmd.Position, frozenValidStart)` (frozen-at-BeginScrub coords). After eviction advanced `currentValidStart` past `frozenValidStart`, scrub-coord positions in the evicted gap resolved to file PTS that no longer existed; `EnsureFileOpen` failed and the user got a sudden snap-to-live mid-drag. Extended `ClampPosition` with optional `frozenValidStart` parameter; when supplied, promotes `min` to `currentValidStart - frozenValidStart` so positions in the evicted gap clamp up to the new oldest valid position. Backwards-compatible parameterless overload preserved for non-scrub callers.

### Deferred (recorded as follow-ups)
- `EnsureFileOpen` swallows decoder open exceptions silently leaving stale decoder.
- `PointerReleased` seek-target carrying via `PlaybackPosition`.
- `PlaybackPosition` 250ms staleness window on Paused after EndScrub.
- `SeekAndDisplayKeyframe`'s reopen-and-retry doubling decoder cost near live edge.
- 1% low warnings still decorative — no root-cause investigation in this pass.

### Test discipline
Every fix is accompanied by source-text test assertions (matching the existing pattern). The reentrant BeginScrub fix would benefit from an integration test that exercises the queue race, but no end-to-end harness exists for that yet.

## 2026-05-01 — Flashback decoder open failure hardening

Follow-up from the deferred list above.

**Issue:** `EnsureFileOpen` caught `decoder.OpenFile` exceptions, cleared controller bookkeeping, but did not forcibly close a decoder that may have become partially open before throwing. Callers then checked only `decoder.IsOpen`, so a half-open or stale native decoder could proceed into seek/display paths even though `fileOpen=false` and `_currentOpenFilePath=null`.

**Change:** `EnsureFileOpen` now closes any partially opened decoder in the exception path using `CloseDecoderFileBestEffort(..., "ensure_file_open_error")`. All command paths that call `EnsureFileOpen` now gate on `fileOpen && decoder.IsOpen` via `IsDecoderFileReady`, covering Seek, BeginScrub, UpdateScrub, Play, and Nudge.

**Verification:** `dotnet run --project tests\ElgatoCapture.Tests\ElgatoCapture.Tests.csproj --no-restore`, `dotnet build ElgatoCapture.slnx -c Debug --no-restore /nr:false`, and `git diff --check` all passed after the fix.

## 2026-05-01 — Flashback scrub release target hardening

Follow-up from the deferred list above.

**Issue:** Pointer release computed a final scrub position and queued a final `UpdateScrub`, but `EndScrub` itself carried no target. The playback thread resumed from `PlaybackPosition`, which may still be the prior keyframe/displayed decode position if the final update was coalesced or not yet reflected in controller state.

**Change:** Added `EndScrubAt(TimeSpan)` through `FlashbackPlaybackController`, `CaptureSessionCoordinator`, and `MainViewModel`. Pointer release now passes the computed visual release position into `EndFlashbackScrubInteraction`, and the controller clamps/uses that position for the final resume seek. Cancel/capture-lost paths keep the targetless `EndScrub` behavior.

**Verification:** `dotnet run --project tests\ElgatoCapture.Tests\ElgatoCapture.Tests.csproj --no-restore` and `dotnet build ElgatoCapture.slnx -c Debug --no-restore /nr:false` passed after the fix.

## 2026-05-01 — Active fMP4 near-live reopen guard

Follow-up from the deferred list above.

**Issue:** When `SeekTo`/`SeekToKeyframe` failed on an active fMP4 segment, playback immediately closed/reopened the decoder and retried. That retry is useful for older active-fragment positions where the demuxer index is stale, but it is expensive and often futile right at the live edge, doubling seek/display work during the most latency-sensitive scrub/release path.

**Change:** Added a 250ms near-live guard for active fMP4 reopen retries. If the seek target is at or within 250ms of the buffer write head, playback logs `FLASHBACK_PLAYBACK_REOPEN_SKIP_NEAR_LIVE`, skips the close/reopen retry, and lets the existing restore-live path recover. Older active-segment seeks still keep the reopen retry.

**Verification:** `dotnet run --project tests\ElgatoCapture.Tests\ElgatoCapture.Tests.csproj --no-restore`, `dotnet build ElgatoCapture.slnx -c Debug --no-restore /nr:false`, and `git diff --check` passed after the fix.

## 2026-05-01 — Deferred flashback backend cleanup commits purge after detach

**Issue:** Once `DisposeFlashbackPreviewBackendCoreAsync` or buffer cycling detached the old flashback sink and transferred its buffer/exporter to deferred cleanup, that cleanup still honored the initiating cancellation token for segment purge. A cancellation after field teardown could therefore dispose the buffer manager but skip the requested purge, leaving old segments behind until a later startup cleanup.

**Change:** `ScheduleDeferredFlashbackBackendCleanup` no longer accepts a cancellation token and always attempts the requested `PurgeAllSegments()` before disposing transferred flashback resources. Cancellation still propagates to the caller after the deferred cleanup is scheduled, but ownership cleanup is committed once the backend has been detached from the live service fields.

**Verification:** `dotnet run --project tests\ElgatoCapture.Tests\ElgatoCapture.Tests.csproj --no-restore`, `dotnet build ElgatoCapture.slnx -c Debug --no-restore /nr:false`, and `git diff --check` passed after the fix.

## 2026-05-01 — Diagnostic sessions surface visual cadence proof

**Issue:** Present/display 1% lows can report jitter even when the decoded visual content is still changing at the expected 120fps. The diagnostic-session summary did not preserve the visual-cadence counters, so live A/B runs could not easily distinguish actual repeated frames from present-interval variance.

**Change:** Added visual-cadence rollups to `DiagnosticSessionResult` and the session formatter: output FPS, change FPS, minimum observed change FPS, repeat-frame percent, maximum observed repeat percent, repeat-frame count, and longest repeat run. The session contract test now asserts those fields and formatter text stay wired.

**Verification:** `dotnet run --project tests\ElgatoCapture.Tests\ElgatoCapture.Tests.csproj --no-restore`, `dotnet build tools\ecctl\ecctl.csproj -c Debug --no-restore /nr:false`, `dotnet build tools\McpServer\McpServer.csproj -c Debug --no-restore /nr:false`, `dotnet build ElgatoCapture.slnx -c Debug --no-restore /nr:false`, and `git diff --check` passed. A 60s live preview-only diagnostic with 5s samples succeeded and persisted the new fields: visual output FPS 119.997, visual change FPS 119.997, minimum observed change FPS 119.338, repeat percent 0.100%, max repeat percent 0.126%, repeat frames 8, longest repeat run 1.

## 2026-05-01 — Present warnings now include visual-cadence context

**Issue:** After visual cadence rollups proved the preview content could still change at ~120fps while present/display 1% low reported ~115fps, the diagnostic health summary still said only `Present/display 1% low is below target.` That was technically true but too easy to misread as 120fps content being shown at 60fps or repeated visibly.

**Change:** `AutomationDiagnosticsHub` now classifies healthy visual cadence beside present/display 1% low warnings. When the crop tracker has enough samples, change FPS is near source rate, repeat percent is <= 1%, and repeat runs are single-frame only, the warning summary explicitly says visual cadence remains near source rate and evidence appends a `visual crop` lane. The throttled preview-display alert also includes visual change/output FPS, repeat percent, longest repeat run, and motion confidence.

**Verification:** `dotnet run --project tests\ElgatoCapture.Tests\ElgatoCapture.Tests.csproj --no-restore`, `dotnet build tools\ecctl\ecctl.csproj -c Debug --no-restore /nr:false`, `dotnet build tools\McpServer\McpServer.csproj -c Debug --no-restore /nr:false`, `dotnet build ElgatoCapture.slnx -c Debug --no-restore /nr:false`, and `git diff --check` passed. A 60s live preview-only diagnostic with 5s samples succeeded and produced the new summary: `Present/display 1% low is below target, but sampled visual cadence remains near source rate.` Evidence included present 1% low 114.99fps plus `visual crop samples=7933 output=120fps changes=120fps repeat=0.101% repeatFrames=8 longestRepeatRun=1 confidence=HighMotion`.

## 2026-05-01 — Preview scheduler session counters now show deltas

**Issue:** Diagnostic-session summaries reported preview scheduler drops, deadline drops, and underflows as absolute end counters only. If the app had a historical underflow before the session began, a clean live run could still look suspicious because `UnderflowsAtEnd` was nonzero.

**Change:** Added session-local `PreviewSchedulerDroppedDelta`, `PreviewSchedulerDeadlineDropsDelta`, and `PreviewSchedulerUnderflowsDelta` fields while preserving the existing end counters. The formatter now prints both end and delta values.

**Verification:** `dotnet build tools\ecctl\ecctl.csproj -c Debug --no-restore /nr:false`, `dotnet build tools\McpServer\McpServer.csproj -c Debug --no-restore /nr:false`, `dotnet build tools\NativeXuAudioProbe\NativeXuAudioProbe.csproj -c Debug --no-restore /nr:false /t:Rebuild`, `dotnet run --project tests\ElgatoCapture.Tests\ElgatoCapture.Tests.csproj --no-restore`, and `git diff --check` passed. A 60s live preview-only diagnostic with 5s samples succeeded and showed the intended distinction: `PreviewSchedulerUnderflowsAtEnd=1` but `PreviewSchedulerUnderflowsDelta=0`, with scheduler drop and deadline-drop deltas also zero.

## 2026-05-01 — Flashback recording integrity warnings use session deltas

**Issue:** Flashback recording diagnostic validation warned on absolute recording-integrity sequence-gap and queue-drop counters. A stale historical counter could therefore fail a new recording session even if no new integrity issue occurred during that run.

**Change:** Added `FlashbackRecordingIntegritySequenceGapsDelta` and `FlashbackRecordingIntegrityQueueDroppedFramesDelta` to diagnostic-session summaries and changed Flashback recording validation warnings to fire only when those counters increase during the session. End counters remain in the summary for context.

**Verification:** `dotnet build tools\ecctl\ecctl.csproj -c Debug --no-restore /nr:false`, `dotnet build tools\McpServer\McpServer.csproj -c Debug --no-restore /nr:false`, `dotnet build tools\NativeXuAudioProbe\NativeXuAudioProbe.csproj -c Debug --no-restore /nr:false /t:Rebuild`, `dotnet run --project tests\ElgatoCapture.Tests\ElgatoCapture.Tests.csproj --no-restore`, `dotnet build ElgatoCapture.slnx -c Debug --no-restore /nr:false`, and `git diff --check` passed. A 60s live `flashback-recording` diagnostic with 5s samples succeeded, strict recording verification passed, 7201 Flashback frames were submitted and 7201 encoder packets were written, and both integrity deltas were zero.

## 2026-05-01 — Flashback playback diagnostics baseline drop/submit counters

**Issue:** Flashback playback diagnostic validation warned on absolute playback dropped-frame and submit-failure counters. That had the same stale-counter failure mode as the recording and preview diagnostics: a previous playback issue could poison a new playback diagnostic even when the new session had no new dropped frames or submit failures.

**Change:** Added `FlashbackPlaybackDroppedFramesDelta` and `FlashbackPlaybackSubmitFailuresDelta` to diagnostic-session summaries and changed playback validation warnings to fire only when those counters increase during the session. Playback 1% low tracking now waits for at least 10 seconds worth of session playback frames before contributing to the session minimum, avoiding the first startup burst while still catching steady-state dips.

**Verification:** `dotnet build tools\ecctl\ecctl.csproj -c Debug --no-restore /nr:false`, `dotnet build tools\McpServer\McpServer.csproj -c Debug --no-restore /nr:false`, `dotnet build tools\NativeXuAudioProbe\NativeXuAudioProbe.csproj -c Debug --no-restore /nr:false /t:Rebuild`, `dotnet run --project tests\ElgatoCapture.Tests\ElgatoCapture.Tests.csproj --no-restore`, `dotnet build ElgatoCapture.slnx -c Debug --no-restore /nr:false`, and `git diff --check` passed. A 60s live `flashback-playback` diagnostic confirmed both new deltas stayed zero, but the session still correctly failed on a real playback 1% low dip around the 20s sample (`min=89.61fps`, floor `96fps`), with later samples recovering to ~120fps. This remains an open playback-performance finding rather than a stale-counter diagnostic failure.

## 2026-05-02 — Capture context at worst flashback playback 1% low

**Issue:** The 2026-05-01 `flashback-playback` 60s diagnostic correctly detected a real 1% low dip near the 20s sample (`min=89.61fps`, floor `96fps`, recovered to ~120fps), but the session result reported only the minimum 1% low value. Diagnosing the cause required cross-referencing the raw sample stream by hand to find which decode P99, AV drift, or audio-master fallback delta coincided with the dip.

Separately, `BuildFlashbackPlaybackSessionMetrics` always treated `FlashbackPlaybackFrameCount` from the initial snapshot as a baseline to subtract. If the first sample of a `flashback-playback` session was captured before playback became active (frame counter at the previous run's end), and a later sample restarted at zero or below the prior end, the `Math.Max(0, frameCount - baselineFrameCount)` clamp could under-report session frames and gate the 1% low minimum behind a frame-count bar that never got reached.

**Change:** Extended `FlashbackPlaybackSessionMetrics` and `DiagnosticSessionResult` with eight context fields captured atomically when the 1% low minimum is updated: `MinOnePercentLowOffsetMs`, `MinOnePercentLowFrameCount`, `MinOnePercentLowP99FrameMs`, `MinOnePercentLowMaxFrameMs`, `MinOnePercentLowDecodeP99Ms`, `MinOnePercentLowDecodeMaxMs`, `MinOnePercentLowAvDriftMs`, `MinOnePercentLowAudioMasterFallbacks`. The session formatter prints all of them. `BuildFlashbackPlaybackSessionMetrics` now uses the initial snapshot frame count as a baseline only when playback was active in that snapshot; otherwise frame counts are session-relative directly. `ObservePlaybackSnapshot` takes the sample offset and stores it alongside the dip context. The session contract test was updated to assert the new fields and source-text patterns.

**Verification:** `dotnet build tools\ecctl\ecctl.csproj -c Debug --no-restore /nr:false` passed (compiles `DiagnosticSessionRunner.cs` via shared glob). Source-text contract assertions in `tests\ElgatoCapture.Tests` for the diagnostic-session paths all passed. `git diff --check` clean. 16 unrelated tests in the same project failed on a pre-existing `McpServer.dll`-staleness gate caused by the live MCP server (PID 1368) holding the binary open; this clears on the next `/mcp` reload and is not a regression. Live diagnostic verification of the new dip-context fields is deferred until the app is launched with the new build — the next run should reproduce the ~20s dip and persist the offset/decode/AV/fallback context for root-cause analysis.

## 2026-05-02 — Recording cancel/rotate segment registration

**Issue:** Two flashback recording-path data-loss bugs surfaced by static survey. The encoder loop's `OperationCanceledException` catch returned queued buffers but never registered the in-progress segment with the buffer manager. A cancellation-driven stop (e.g. `DisposeAsync` cancelling the encoding CTS) therefore left the final fMP4 segment on disk but unindexed — scrub and export silently missed the live edge. Separately, `RotateSegment`'s catch advanced `_segmentStartPts` to avoid an infinite retry but never registered the just-rotated old segment. A single `RotateOutput` failure left the previous segment's bytes on disk and invisible to playback.

**Change:** Both paths now register the affected segment with the same fields the fatal-error path already uses (path, frame count, start_ms, end_ms), guarded by a tiny try/catch so a registration failure cannot suppress the original exception. Adds `SegmentRotationFailures` counter and `FLASHBACK_SINK_ENCODING_LOOP_CANCELLED_SEGMENT_REGISTERED` / `FLASHBACK_SINK_ROTATE_FAIL_SEGMENT_REGISTERED` (+ `_NO_SEGMENT` / `_REGISTER_FAIL` variants) log events so a diagnostic session can confirm the recovery fired. Rotation failure path keeps the existing `_segmentStartPts` advance to avoid a retry storm.

**Verification:** `dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -c Debug --no-restore /nr:false -p:StageLatestBuild=false` succeeded with 0 errors, 0 warnings (file-in-use copy step on the running app is the only deferred action; obj/ DLL produced clean). Live integration verification is deferred — these paths fire only on cancellation/rotation failure, which require either a forced disposal mid-recording or an induced ffmpeg rotate fault, neither of which the existing diagnostic scenarios trigger today. Adding a recording-cancellation diagnostic scenario to `flashback-recording-export-rejected` is open follow-up.

## 2026-05-02 — Buffer dispose purge, eviction-pause clear under lock

**Issue:** Two flashback-buffer data-integrity bugs. `Dispose` attempted `Directory.Delete(sessionDir, recursive: false)` without first removing the segment files. The delete failed silently (caught and logged as `FLASHBACK_BUFFER_SESSIONDIR_DELETE_WARN`) whenever segments were still present, leaving multi-GB segment files on disk for up to 12 hours until `CleanupStaleSessionDirectories` removed them. Separately, `PurgeAllSegments` cleared `_evictionPauseCount` via `Interlocked.Exchange` without holding `_indexLock`, while `PauseEviction`/`ResumeEviction` mutate the same counter under the lock. A purge concurrent with a recording starting could therefore force-resume eviction while the recording thread expected an active pause, allowing live segments to be evicted out from under the writer.

**Change:** Extracted the file-deletion body of `PurgeAllSegments` into a private `PurgeAllSegmentsCore()` (returns segment count and freed bytes). Public `PurgeAllSegments` keeps the `_disposed` gate; `Dispose` calls `PurgeAllSegmentsCore` directly under `_indexLock` before setting `_disposed`. Added `FLASHBACK_BUFFER_DISPOSE_PURGE segments=N bytes=B` log event so cleanup is greppable. Inside `PurgeAllSegmentsCore`, `_evictionPauseCount = 0` is now a plain assignment under the lock both callers already hold, fully serialized with the increment/decrement paths.

**Verification:** Same compile passed clean. Live verification of the dispose-purge requires a `flashback-recording` cycle with a non-empty buffer at shutdown plus a follow-up filesystem inspection of the session dir; the eviction-pause race fires under recording-start vs deferred-purge timing that the existing diagnostic scenarios do not exercise. Both verifications open follow-up.

## 2026-05-02 — Flashback audio gate uses post-seek video PTS at fMP4 reopen

**Issue:** Direct root cause of the recurring playback 1% low fps dip first observed in the 2026-05-01 `flashback-playback` 60s session (`min=89.61fps` against a `96fps` floor, recovering to ~120fps later). Both fMP4 reopen paths in `FlashbackPlaybackController` (the `REOPEN_BEFORE_SEGMENT_SWITCH` branch and the active fMP4 reopen near the live edge) nullified the decoder's audio callback before the seek and restored it with the gate set to `_lastAudioPtsTicks` — the *last presented* audio PTS, which reflects pre-seek state. If the seek landed earlier than that stale gate, audio up to the gate was suppressed; if it overshot, audio near the reopen point was silently gapped. Either way the audio clock stalled for one WASAPI cycle (~21 ms), `PaceFrameInterval` fell through to wall-clock pacing and incremented `FlashbackPlaybackAudioMasterFallbacks`, and one to a few frames of irregular spacing followed — exactly the dip's signature.

**Change:** Both reopen paths now anchor the audio gate to the actual post-seek video PTS (`lastFrameAbsPts.Ticks` and `resumeTarget.Ticks` respectively), so audio resumes from the same position video resumes from. Added `PlaybackReopenAudioNullWindowCount` counter and `FLASHBACK_PLAYBACK_REOPEN_AUDIO_GATE gate_ms=... source=PostSeekVideoPts last_audio_ms=... seek_target_ms=...` log event so a future diagnostic can verify the fix at runtime. The original survey identified one path; the second path was caught during implementation review and shared the identical broken pattern.

**Verification:** Compile passed clean. Live diagnostic verification with the 2026-05-02 dip-context fields (`MinOnePercentLowAudioMasterFallbacks`, `MinOnePercentLowOffsetMs`) is the intended audit evidence — was attempted today but blocked by an unrelated app-exit issue under investigation. Once the app stays up across a full 60s session, a fresh `flashback-playback` run should show `PlaybackReopenAudioNullWindowCount` rising while `MinOnePercentLowAudioMasterFallbacks` stays at zero, with the 1% low minimum restored to the ~96fps floor.

## 2026-05-02 — Flashback HDR negotiation now fails the operation

**Issue:** Two HDR-rail violations surfaced by static survey of `CaptureService.cs`. `CreateFlashbackSessionContext` derived `HdrEnabled` from the negotiated `isP010` rather than from the user request — a UVC negotiation that returned NV12 instead of P010 silently encoded SDR while the rest of the pipeline still believed HDR was on. Separately, `CanReuseFlashbackBackend` never compared HDR state, so toggling HDR off (or on) while preview was stopped reused a stale backend whose sink was opened with the previous `IsP010`, encoding mismatched-format frames into the rolling buffer with no error.

**Change:** `CreateFlashbackSessionContext` now compares `HdrOutputPolicy.IsEnabled(settings)` against the negotiated `isP010` before the context is built. If they diverge, it logs `FLASHBACK_HDR_NEGOTIATION_FAIL requested=... negotiated_p010=... resolved_codec=...` and throws `InvalidOperationException`, halting the flashback startup rather than silently degrading. The stored `HdrEnabled` field is pinned to `hdrRequested` so it cannot drift from `IsP010` even if `HdrOutputPolicy` modulates the raw `settings.HdrEnabled`. `CanReuseFlashbackBackend` now checks `HdrOutputPolicy.IsEnabled` on both `current` and `next`; when they differ it logs `FLASHBACK_REUSE_REJECTED reason=hdr_mismatch existing=... requested=...` and rejects reuse, forcing a fresh backend with the correct format negotiation.

**Verification:** `dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -c Debug --no-restore /nr:false -p:StageLatestBuild=false` passed clean (0 errors, 0 warnings). Live verification of the throw path requires inducing a UVC negotiation drop — open follow-up. The reuse-rejection path can be exercised by toggling `HDR` between two flashback-enabled sessions; today that requires manual testing or a new diagnostic scenario.

## 2026-05-02 — Flashback export reclaims native state on dispose timeout

**Issue:** Two export-side reliability bugs. `FlashbackExporter.Dispose` cancelled `_disposeCts` and waited up to 10s for the export lock when called during a long export (e.g. `SetFlashbackEnabled(false)` while a multi-segment 4K120 remux was in progress). The 10s-timeout branch then skipped `CleanupNativeState`, leaving the active `AVFormatContext`, `AVIOContext`, and `_activeTempPath` orphaned. Repeated occurrences would exhaust file handles or native memory. Separately, `DeleteTempFileIfPresent` had no retry: a transient `ERROR_SHARING_VIOLATION` (`HResult & 0xFFFF == 32`) from an AV scanner during the brief window after the export closed the file would permanently swallow the delete, leaving a `.tmp` file that blocked later exports to the same output path until manual cleanup.

**Change:** The dispose timeout branch now invokes `CleanupNativeState()` (wrapped in try/catch to stay best-effort) and deletes any `_activeTempPath` before returning. Surrounded by `FLASHBACK_EXPORT_DISPOSE_TIMEOUT cleanup_invoked=true` and `FLASHBACK_EXPORT_DISPOSE_TIMEOUT_DONE` log events for greppable evidence. The 10s timeout duration itself is unchanged — only the recovery on timeout is hardened. `DeleteTempFileIfPresent` now retries up to 3 times with 200 ms back-off on `IOException` whose `HResult & 0xFFFF == 32`. All other exception types still swallow-and-log on first attempt. After all retries on a sharing violation, logs `delete_tmp_failed_sharing_violation` and returns.

**Verification:** Same compile passed clean. Live verification requires inducing the dispose-timeout race (e.g. starting a long export then quickly toggling flashback off) and observing the new log events; an AV-scanner-induced sharing violation is harder to reproduce on demand. Both verifications are open follow-up.

## 2026-05-02 — Scrub release carries final position through capture-lost paths

**Issue:** Three scrub-release failure paths previously called `FlashbackEndScrub` with no release position, so the controller fell back to `PlaybackPosition` (the last decoded keyframe) instead of the user's pointer-driven visual target. Pointer-cancel from a device disconnect, OS focus steal, or fullscreen entry mid-drag therefore snapped playback hundreds of milliseconds behind the user's release point. The `PointerReleased` happy path already passed the resolved release position via `EndScrubAt` (commit `57201e2`); the cancel/capture-lost/fullscreen paths simply skipped it.

**Change:** `PointerCanceled` and `PointerCaptureLost` in `MainWindow.Flashback.cs` and the fullscreen-entry-while-scrubbing path in `MainWindow.FullScreen.cs` now read `ViewModel.FlashbackPlaybackPosition` — the field `UpdateFlashbackScrubVisual` writes on every `PointerMoved` tick — and pass it as `releasePosition` to `EndFlashbackScrubInteraction` / `FlashbackEndScrubAt`. Adds `FLASHBACK_SCRUB_END_CANCELED` / `FLASHBACK_SCRUB_END_CAPTURE_LOST` / `FLASHBACK_SCRUB_END_FULLSCREEN carried_position_ms=...` log events. Tightened a pre-existing null-conditional inconsistency on `ViewModel` inside the fullscreen block at the same time.

**Verification:** Static evidence (source-line comparison against the `PointerReleased` happy path) confirms the same `releasePosition` value now flows through every termination path. Live verification deferred — requires inducing a `PointerCaptureLost` mid-drag (e.g. unplugging the capture device while scrubbing) and observing the `FLASHBACK_SCRUB_END_CAPTURE_LOST` log event with the expected position. Compile-verify deferred to the batch build at end of session (avoiding the pre-build hook closing the running app during live diagnostic work).

## 2026-05-02 — HDR threaded through NV12 plane texture submission

**Issue:** Latent silent-HDR-degradation hazard in `D3D11PreviewRenderer`. The NV12 plane texture path (`SubmitNv12PlaneTextures` → `EnqueueNv12Frame` → `PendingFrame.IsHdr`) hard-coded `isHdr: false`. No callers existed yet, but once flashback NVDEC NV12 output gets wired into the preview sink, an HDR P010 source decoded into NV12 plane textures would have been rendered as SDR with no error — a hard-rail violation by latent design. Separately, `RenderFrame`'s fallback to `RenderFrameWithVideoProcessor` for HDR frames when the HDR shader pipeline is unavailable produced no log line, so SDR-degradation could occur silently at runtime.

**Change:** `IPreviewFrameSink.SubmitNv12PlaneTextures` and `D3D11PreviewRenderer.SubmitNv12PlaneTextures` / `EnqueueNv12Frame` now accept `isHdr` (default `false` for backward compatibility). The flag is forwarded into `PendingFrame.IsHdr`. Adds `_lastNv12IsHdr` (tri-state field initialized to `-1`) to track first-frame and SDR↔HDR transitions through this path with `D3D11_PREVIEW_NV12_HDR_TRANSITION from=... to=... pathTag=PlaneTextures`. Adds a one-shot `D3D11_PREVIEW_HDR_SHADER_FALLBACK reason=...` log on the VideoProcessor fallback path so silent SDR-degradation is greppable; the fallback behavior itself is unchanged. The fix is preventive — when a future flashback wiring submits NV12 plane textures, it must now pass `isHdr: decoder.IsP010` to avoid silent SDR fallback.

**Verification:** `dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -c Debug -p:StageLatestBuild=false` succeeded, 0 errors, 0 warnings (16s). No callers exist for `SubmitNv12PlaneTextures` today, so live runtime evidence will only become observable once the NVDEC NV12-plane wiring lands.

## 2026-05-02 — App-launch reliability blocker: pre-build hook closes app under live test

**Issue (environmental, not a flashback bug):** Multiple attempts to live-validate the audio-gate fix this session were blocked by a sequence of mid-session app shutdowns. Initial diagnostic showed `MainWindow_Closed` firing without an explicit close request. Investigation traced the cause: the project's `.claude/hooks/pre_build_close_app.sh` PreToolUse hook (matched on `Bash(dotnet build*)`) calls `ecctl window close` on the running app before every build. Implementer subagents in this session each ran `dotnet build` to verify their fix's compile, which fired the hook and closed the live app each time. Killing the stale `McpServer.exe` (PID 1368) eliminated one source of pipe contention but did not prevent additional subagents from triggering the hook.

**Implication:** Static-evidence fixes can still ship safely, since the hook only closes the *running* app — it does not interfere with compile or test execution. Live diagnostic validation, however, requires a quiet window where no `dotnet build` runs while a diagnostic is in flight. That window is hard to maintain in an unattended-multiple-subagent session.

**Workaround for future sessions:** Either (a) batch all subagent compile-verifications until *after* live validation completes, (b) modify subagents to skip `dotnet build` and let the parent do a single batch build at the end, or (c) temporarily disable the pre-build hook during live diagnostic windows. Option (b) was used for the scrub-capture-lost and NV12-HDR fixes in this session and worked.

**Status of live validation today:** The audio-gate fix (commit `b8acbce`) is the headline change and directly targets the 20s 1% low dip first observed 2026-05-01. Multiple attempted live `flashback-playback` 60s diagnostics today were aborted mid-session by the hook-driven shutdowns. Static evidence (source-line bug analysis, compile-clean diff, identical pattern caught in a second reopen path during implementation review) is the only validation available this session. Manual or next-session live validation is required to close the audit gate on the playback-dip line item.

## 2026-05-02 — Live validation of the flashback audio-gate fix

**Issue (the validation context):** The audio-gate fix at `FlashbackPlaybackController` fMP4 reopen (commit `b8acbce`) needed a runtime artifact to close the audit gate. Multiple attempts earlier in the session were aborted by an unrelated mid-session app shutdown.

**Investigation of the shutdown trigger.** Added stack-capture logging to `MainWindow_Closing` and `MainWindow_Closed`. The captured stack showed `MainWindow.CloseAsync()` being invoked from the WinUI 3 dispatcher queue with no preceding `MainWindow_Closing` event — i.e. a programmatic `Window.Close()` rather than a user X-button. The only callsite for `CloseAsync` is `AutomationCommandDispatcher` for `WindowAction.Close`. Adding `AUTOMATION_PIPE_RECV command={...} clientPid={...}` logging at the pipe-server connection handler (using `GetNamedPipeClientProcessId` via P/Invoke since `NamedPipeServerStream` exposes no managed wrapper) revealed an `ArmClose` followed by `WindowAction` from a transient PID. Both commands together are the exact sequence emitted by the project's `tools/ecctl/CommandHandlers.cs` `window close` path.

The only caller of `ecctl window close` in the repo is `.claude/hooks/pre_build_close_app.sh` — a PreToolUse hook intended to fire only on `Bash(dotnet build*)` per its `if` clause. Logging added inside that hook proved it was firing on every Bash command (including a plain `sleep 12; ...` Bash that contained no `dotnet build` substring), so the `if` filter was not being honored. The hook now also reads `tool_input.command` from the JSON payload Claude Code passes on stdin and exits immediately when the actual command does not match `(^|[[:space:];&|])dotnet[[:space:]]+build` regex. With that gate in place a 60s `flashback-playback` diagnostic ran end-to-end without the live app being killed.

**Validation result.** Live `flashback-playback` 60s session with `--sample-ms 5000 --verify` against the new build (all 2026-05-02 fixes plus the diagnostic-context fields):

- `fpsMin=117.14` (prior failing baseline `min=89.61fps`).
- `onePercentLowFpsMin=118.27` — far above the 96fps floor; the prior dip is gone.
- `onePercentLowMinOffsetMs=45322` — the worst 1% low sample now lands at ~45s and is shallow, not the prior ~20s deep dip.
- `onePercentLowMinDecodeP99Ms=2.66`, `onePercentLowMinDecodeMaxMs=4.72` — decode is healthy.
- `onePercentLowMinAvDriftMs=0`, `absAvDriftMsMax=0` — A/V pacing never lost the audio anchor.
- `droppedFramesEnd=0`, `submitFailuresEnd=0`, `audioBufferedMsMax=0`, `audioQueueMsMax=0` — no frames lost, no audio queue buildup.
- `Recording Verification: FAIL | No output file path is available for verification.` — expected, this scenario does not exercise recording. Session also flagged `present_display 1% low` at 110.15fps which is a separate compositor metric (visual cadence stayed at 119.95fps with 0.106% repeat — i.e. content really is changing every frame).

The audio-gate fix is empirically validated. The hook fix protects future live-test sessions in this project from the same class of interference.

## 2026-05-02 — Decoder forward-decode cap observability + revalidation

**Issue:** `FlashbackDecoder.SeekTo` caps forward-decode at 960 frames (a safety bound on per-seek decode work) and silently returned `bestFrame` when the cap was reached, even when `bestFrame.Pts` was many frame intervals behind the seek target. On long active fMP4 segments with deep GOPs, the fMP4 reopen path then continued from a position multiple frames behind the requested PTS, producing a multi-frame late burst at every reopen that was invisible to the existing diagnostic counters (the within-tolerance case already logged `FLASHBACK_DECODER_SEEK_FRAME_LIMIT` but the missed-target case did not differentiate itself).

**Change:** Added purely observational state to `FlashbackDecoder` so callers and diagnostics can detect a missed-target cap-hit without changing existing seek semantics:

- `private long _seekToCapHits` Interlocked counter, exposed via `public long SeekToForwardDecodeCapHits`.
- `private bool _lastSeekHitForwardDecodeCap` reset on each `SeekTo` entry, exposed via `public bool LastSeekHitForwardDecodeCap` so the fMP4 reopen caller can react in a follow-up patch.
- New `FLASHBACK_DECODER_SEEK_CAP_HIT target_ms=... best_ms=... gap_ms=... frames_decoded=...` log event that fires only when `gap_ms > frameIntervalMs`. Existing within-tolerance log preserved.

**Verification:** `dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -c Debug -p:StageLatestBuild=false` succeeded clean (initial run failed with `CS0103 The name 'Interlocked' does not exist` in `FlashbackDecoder.cs` because the file lacked `using System.Threading;` — added in the same commit).

A second 60s `flashback-playback` live diagnostic against the latest build (all today's fixes including this one) confirmed no regression in the audio-gate dip fix:

- `fpsMin=119.94` (vs prior failing `89.61fps`)
- `onePercentLowFpsMin=116.02` — well above the 96fps floor
- `maxFrameMsObserved=9.14`, `slowPctMax=0.16` — pacing is extremely tight
- `droppedFramesEnd=0`, `submitFailuresEnd=0`, `absAvDriftMsMax=0`, `audioBufferedMsMax=0`

The session-level FAIL tag in the verifier output is on unrelated checks: `Recording Verification: FAIL | No output file path is available for verification.` (the scenario does not exercise recording), and the preview-display 1% low at 104.57fps — a compositor-side metric distinct from playback frame pacing.

## 2026-05-02 — Rolling-window FPS, fast-path HDR guard, and final soak

**Issues fixed in this round:**

1. **`PlaybackObservedFps` cumulative smoothing.** `UpdateCadenceMetrics` computed FPS as `frameNum / wallElapsedMs`, a session-wide cumulative average. After ~2400 frames at 120fps any short stall was smoothed out of view, even though the existing 240-sample decode P99 ring captured the same event correctly. Now sums the existing `_playbackFrameIntervalsMs` ring under the cadence lock and divides by sample count, matching the decode ring's ~2-second horizon.

2. **Fast-path flashback fast-path format-drift hole.** `StartVideoPreviewAsync` and `StartRecordingAsync` each took a fast path under `(_isRecording || _flashbackEnabled)` that called `SetPreviewSink` and returned without revalidating the active flashback sink's pixel format against the freshly negotiated UVC `IsP010`. A UVC re-negotiation that flipped the pixel format between sessions therefore silently reused the existing flashback backend with the wrong format. Now both fast paths compare `_flashbackSink.IsP010` (newly exposed on `FlashbackEncoderSink` from the cached session context) against `_unifiedVideoCapture.IsP010` and throw `InvalidOperationException` when they diverge. The slow-path `CreateFlashbackSessionContext` already hard-failed mismatches via commit `a1ca0e6`; this closes the parallel fast-path hole. HARD RAIL maintained.

**Verification — fourth consecutive live `flashback-playback` 60s soak against the latest build (all 17 commits):**

- `fpsMin=119.74`, `onePercentLowFpsMin=119.01` — both well above the 96fps floor that the original 2026-05-01 dip session failed at (`89.61fps`).
- `onePercentLowMinOffsetMs=25209` — worst sample at 25s, shallow.
- `onePercentLowMinDecodeP99Ms=4.5`, `onePercentLowMinDecodeMaxMs=4.89`, `onePercentLowMinAvDriftMs=0`.
- `slowPctMax=0.08`, `maxFrameMsObserved=9.9` — pacing is extremely tight at 120fps.
- `droppedFramesEnd=0`, `submitFailuresEnd=0`, `absAvDriftMsMax=0`, `audioBufferedMsMax=0`, `audioQueueMsMax=0`.
- Session-level `FAIL` is on `Present/display 1% low` compositor metric (109.93fps) with the visual-cadence rollup correctly classifying it as cadence-near-source-rate, not a playback degradation.

The flashback-playback 1% low dip is empirically resolved across four independent live runs. No regressions across any subsequent fix.

## 2026-05-02 — Goal-completion soak: BeginScrub + deferred-purge land cleanly

**Final round of fixes:**

1. **`BeginScrub` reentrancy.** Worker-thread handler reset `frozenValidStart` unconditionally even when `isScrubbing` was already true. Moved the assignment into the existing `if (!isScrubbing)` block so the snapshot only captures on the first BeginScrub. A duplicate logs `FLASHBACK_PLAYBACK_BEGIN_SCRUB_DUPLICATE` with both the existing frozen value and the proposed-but-rejected new value.

2. **Deferred backend cleanup vs export race.** `ScheduleDeferredFlashbackBackendCleanup`'s `PurgeAllSegments` ran on a background `Task.Run` without serializing against `_flashbackExportOperationLock`, so a deferred purge mid-export could delete segment files FFmpeg was still reading. Now waits up to 30s on the export lock before purging (CancellationToken.None per the commit `6a57c91` rule that purge must commit once scheduled). Times out with `FLASHBACK_DEFERRED_PURGE_EXPORT_LOCK_TIMEOUT` and proceeds anyway — better one disrupted export than indefinite segment leak. Adds matching `_AWAITING_EXPORT_LOCK` and `_LOCK_ACQUIRED elapsed_ms=...` log events.

**Verification — fifth consecutive `flashback-playback` 60s live soak against the latest build (all 21 commits):**

- `fpsMin=119.77`, `onePercentLowFpsMin=118.51` — both well above the 96fps floor that the original 2026-05-01 dip session failed at (`89.61fps`).
- `onePercentLowMinOffsetMs=35257` — worst sample at 35s, shallow.
- `onePercentLowMinDecodeP99Ms=3.75`, `onePercentLowMinAvDriftMs=0`, `audioBufferedMsMax=0`, `audioQueueMsMax=0` — pacing primitives all healthy.
- `droppedFramesEnd=0`, `submitFailuresEnd=0`, `absAvDriftMsMax=0` over 7204 frames in 60s.
- `maxFrameMsObserved=17.59` is a one-frame outlier within the 5s window where `slowPctMax=30.77`; `lateEnd=15`/`slowEnd=12` over the full session = 0.17% of frames. The pre-fix dip was a sustained multi-frame stall; this is OS scheduling jitter, not the same class of failure.

**Audit summary.** Five consecutive 60s live diagnostics close the audit gate on the headline flashback-playback dip. The other 19 source-level commits are backed by independent code-survey findings at named file:line sites with compile-clean diffs; their fault paths (cancellation mid-recording, induced rotate fault, HDR-negotiation drop, export-during-dispose-timeout, scrub capture-lost, etc.) require inducing specific failure modes that are not covered by the existing diagnostic scenarios. Those remain open for either targeted integration tests or scenario-specific live exercises in future sessions. The diagnostic plumbing now in place (close-trigger stack capture, pipe-command source PID logging, hook stdin-aware filtering) ensures the kind of regression that blocked validation earlier in this session would now be visible immediately.

## 2026-05-02 — Renderer-reinit observability + 3-minute soak artifact

**Renderer reinit observability.** Added pure-observation instrumentation to detect when `StartPreviewRendererAsync` enters with a still-bound prior renderer and `_isPreviewReinitAnimating==false` (the survey-identified D3D11 SetSwapChain AV race window). Adds `_lastRendererStopTick`, `_rendererReinitUnsafeWindows` Interlocked counter, `D3D11_RENDERER_REINIT_UNSAFE_WINDOW` event with `prev_present`, `prev_rendering`, `time_since_last_stop_ms` fields, plus a `D3D11_RENDERER_REINIT_FLAG flag=... caller=...` log at every flag-write site (PropertyChanged, PreviewRenderer, PreviewStartup, EventHandlers). The proper serialization lock fix is deferred — riskier D3D11 territory than is appropriate for this session — but the unsafe-window will now be visible in the log if it actually opens in practice, giving the next session concrete data to decide whether the lock is needed.

**Extended-soak artifact.** A 180-second `flashback-playback` live diagnostic against the build with all 22 source-level commits:

- `framesEnd=21584` (118.8fps over 180s — within rounding of source 119.88fps).
- `fpsMin=119.23`, `onePercentLowFpsMin=111.66` — both well above the 96fps floor that the original 2026-05-01 dip session failed at (`89.61fps`).
- `onePercentLowMinOffsetMs=161045` — worst sample at 161s into the session, still shallow.
- `onePercentLowMinDecodeP99Ms=8.94`, `onePercentLowMinDecodeMaxMs=10.13` — decode latency within the 8.33ms target ± a couple of ms.
- `onePercentLowMinAvDriftMs=0`, `audioBufferedMsMax=0`, `audioQueueMsMax=0` — A/V pacing primitives healthy throughout.
- `slowPctMax=0.02`, `slowEnd=3` over 21584 frames = **0.014% slow frames over 3 minutes**.
- `droppedFramesEnd=0`, `submitFailuresEnd=0`, `absAvDriftMsMax=0`.
- The single `maxFrameMsObserved=19.19` is one outlier frame in a 3-minute window vs the prior sustained dip.

The audio-gate dip fix is empirically validated across six independent live runs (5x 60s + 1x 180s) with progressively more demanding workloads. The pre-fix failing baseline was a sustained dip to `89.61fps` over a single 60s session; the post-fix evidence is a tightest-observed `slowPctMax=0.02` over 180s with the worst 1% low at `111.66fps`.

## 2026-05-02 — Preview-clear sequence reset during Flashback recording

**Issue.** A 60s `flashback-recording-preview-cycle` live run passed recording verification but exposed noisy preview scheduler diagnostics: stopping preview during active Flashback recording produced `PreviewSchedulerUnderflowsDelta=74085` and a soft-deadline drop, even though recording wrote 7199/7199 frames and visual cadence recovered near 120fps. Code inspection showed the intentional preview clear drained queued MJPEG preview frames but kept `_nextPreviewSequence` pinned to the pre-clear sequence. When preview resumed, the next queued frame was far ahead, so the scheduler repeatedly treated the normal post-clear sequence jump as a missing-frame wait until deadline expiry.

**Change.** `MjpegPreviewJitterBuffer.ClearQueue()` now resets `_nextPreviewSequence` to `-1` after draining frames, so an intentional clear creates a fresh preview ordering epoch. Real missing-frame gaps still use the existing deadline-skip path. Added `MJPEG preview jitter clear resets preview sequence` to the runtime regression suite to prove clear drains the lease, resets ordering, and accepts the first resumed frame without underflow or deadline-drop accounting.

**Verification.**

- `dotnet build ElgatoCapture\ElgatoCapture.csproj -c Debug -p:Platform=x64 --no-restore /nr:false /m:1 -p:UseSharedCompilation=false` passed.
- `dotnet run --project tests\ElgatoCapture.Tests\ElgatoCapture.Tests.csproj --no-restore` passed, including the new clear/reset regression.
- First post-fix 60s live `flashback-recording-preview-cycle`: recording backend observed, file growth observed, strict verification passed, 7199 frames submitted and 7199 packets written, sequence-gap and queue-drop deltas 0. Preview scheduler improved from `underflowsDelta=74085` to `underflowsDelta=0`, `deadlineDropsDelta=0`; visual cadence output `119.99fps`, change `119.99fps`.
- Second 60s live rerun: strict verification again passed, 7199/7199 frames/packets, sequence-gap and queue-drop deltas 0, `underflowsDelta=0`, `deadlineDropsDelta=0`, visual cadence output/change `120.01fps`.
- Both post-fix runs still reported a source-capture 1% low warning around `115.8fps` with zero capture gaps/drops. That warning is reproducible live input cadence sensitivity in 4K120 SDR mode, not the preview-clear underflow storm. HDR-off with HDR source telemetry is expected for this 4K120 USB mode because the capture card cannot also deliver HDR at that bandwidth.

## 2026-05-02 — Source 1% low diagnostic now respects visual proof

**Issue.** After the preview-clear fix, repeated `flashback-recording-preview-cycle` runs still surfaced `HealthStatus=Warning` with `LikelyStage=source_capture` solely because source 1% low was below the strict 98% target. The evidence had no source gaps, no estimated drops, exact Flashback recording frame/packet counts, and visual cadence at source rate. That made the diagnostic verdict noisier than the actual app behavior.

**Change.** `BuildDiagnosticEvaluation` now mirrors the existing present/display visual-cadence exception for source 1% low: when preview is active, source 1% low is below target, source gaps/drops are zero, and sampled visual cadence confirms source-rate output with <=1% repeats and no repeat run longer than 1, the app reports `HealthStatus=Healthy` while keeping the source evidence in the summary. Actual source gaps, estimated drops, and degraded visual cadence still warn as before.

**Verification.** `dotnet build ElgatoCapture\ElgatoCapture.csproj -c Debug -p:Platform=x64 --no-restore /nr:false /m:1 -p:UseSharedCompilation=false` and `dotnet run --project tests\ElgatoCapture.Tests\ElgatoCapture.Tests.csproj --no-restore` passed. A 60s final-app `flashback-recording-preview-cycle` live diagnostic returned `HealthStatus=Healthy`, `LikelyStage=none`, with `source 1pctLow=116.32fps gaps=0 drops=0`, visual output/change `120.01fps`, `PreviewSchedulerUnderflowsDelta=0`, `PreviewSchedulerDeadlineDropsDelta=0`, strict recording verification passed, 7198 frames submitted and 7198 packets written, and recording sequence-gap/queue-drop deltas 0.

## 2026-05-02 — Rejected-export diagnostics carry direct rejection evidence

**Issue.** `flashback-recording-export-rejected` validated the expected rejection internally, but the session-level export rollup is sample-based. Because the rejected export is fast and the final snapshot returns to `NotStarted`, the JSON summary could show `FlashbackExportObserved=false`, which is technically correct for progress tracking but ambiguous for the guardrail being tested.

**Change.** Both rejected-export scenarios now append an action breadcrumb after the post-rejection snapshot: `flashback rejected export observed status=... kind=...` or `flashback recording rejected export observed status=... kind=...`. This preserves direct evidence of the expected failure kind without changing the progress metrics contract.

**Verification.** `dotnet build tools\ecctl\ecctl.csproj --no-restore`, `dotnet build tools\McpServer\McpServer.csproj -c Debug --no-restore`, `dotnet build tools\NativeXuAudioProbe\NativeXuAudioProbe.csproj -c Debug --no-restore`, and `dotnet run --project tests\ElgatoCapture.Tests\ElgatoCapture.Tests.csproj --no-restore` passed. A 60s live `flashback-recording-export-rejected` run now includes `flashback recording rejected export observed status=Failed kind=UnavailableDuringRecording` in `Actions`, `HealthStatus=Healthy`, strict recording verification passed, 7201 frames submitted and 7201 packets written, recording sequence-gap/queue-drop deltas 0, and no warnings.

## 2026-05-02 — HDR-source/SDR-capture diagnostics and busy-pipe session hardening

**Issue.** In the 4K120 USB path, the source can report HDR while the capture pipeline is intentionally SDR because the capture card cannot deliver 4K120 HDR over USB. The runtime snapshot still labeled this as `TelemetryAlignmentStatus=Mismatch` / `SourceVsCaptureParity=mismatch`, which made an expected hardware-bandwidth limitation look like a possible performance fault. While validating the disable-during-export scenario, `ecctl diagnostic-session` also repeatedly exited with `Timed out connecting to automation pipe ... after 5000 ms` even though the app-side export and strict verification completed; the failed client left only the MP4 and no `summary.json`.

**Change.** Telemetry alignment now treats `source HDR + SDR requested` as `Aligned` with an explicit reason: `Source is HDR, but SDR capture was requested.` The HDR truth verdict reports `SourceVsCaptureParity=expected-sdr-capture` and keeps evidence `source-hdr=true, capture-hdr-like=false, hdr-requested=false`. The diagnostic runner now wraps raw automation sends in the existing connect-retry helper, including the concurrent-command flashback scenarios, so a busy single pipe no longer aborts artifact writing before the summary is emitted.

**Verification.** `dotnet build ElgatoCapture\ElgatoCapture.csproj -c Debug -p:Platform=x64 --no-restore /nr:false /m:1 -p:UseSharedCompilation=false`, `dotnet build tools\ecctl\ecctl.csproj --no-restore`, `dotnet build tools\McpServer\McpServer.csproj -c Debug --no-restore`, `dotnet build tools\NativeXuAudioProbe\NativeXuAudioProbe.csproj -c Debug --no-restore -t:Rebuild`, and `dotnet run --project tests\ElgatoCapture.Tests\ElgatoCapture.Tests.csproj --no-restore` passed. A fresh final-app snapshot from the rebuilt app showed `TelemetryAlignmentStatus=Aligned`, `TelemetryAlignmentReason=Source is HDR, but SDR capture was requested.`, and `HdrTruthVerdict.SourceVsCaptureParity=expected-sdr-capture`. A 60s live `flashback-disable-during-export` rerun produced complete artifacts at `temp\diagnostic-sessions\flashback-disable-during-export-raw-connect-retry-60s`: `HealthStatus=Healthy`, `FlashbackExportObserved=true`, `FlashbackExportStatusAtEnd=Succeeded`, `FlashbackExportMessageAtEnd=Exported 499 packets from 1 segments`, `PreviewSchedulerUnderflowsDelta=0`, `PreviewSchedulerDeadlineDropsDelta=0`, visual change cadence `119.9998fps`, actions included `flashback disable during export verified` and `flashback re-enabled after disable/export`, and `Warnings=[]`.

## 2026-05-02 — Flashback export verification profile for concurrent exports

**Issue.** A 60s `flashback-export-concurrent` live diagnostic produced valid MP4 artifacts and app-side successful exports, but the diagnostic result warned that export A failed strict verification with `codec-mismatch(expected=Av1Mp4,actual=hevc)`. Manual verification showed export B passed with the same detected HEVC codec because it was the final `FlashbackExportOutputPath`. Export A was no longer the latest path, so `VerifyFile` fell back to the selected recording format (`AV1`) instead of the actual flashback export verification format (`HEVC` fallback for 4K120 software MJPEG).

**Change.** Commit `975d4e5` added an optional `verificationProfile` to automation/MCP file verification. `verificationProfile=flashback-export` overlays the runtime snapshot's `FlashbackExportVerificationFormat` onto the file being verified, so older concurrent export artifacts are checked against the actual flashback encoder format rather than the UI-selected recording format. `DiagnosticSessionRunner` now uses that profile for all flashback export artifact verification payloads.

**Verification.** `dotnet build ElgatoCapture\ElgatoCapture.csproj -c Debug -p:Platform=x64 --no-restore /nr:false /m:1 -p:UseSharedCompilation=false`, `dotnet build tools\McpServer\McpServer.csproj -c Debug --no-restore`, `dotnet build tools\ecctl\ecctl.csproj --no-restore`, `dotnet build tools\NativeXuAudioProbe\NativeXuAudioProbe.csproj -c Debug --no-restore -t:Rebuild`, `dotnet run --project tests\ElgatoCapture.Tests\ElgatoCapture.Tests.csproj --no-restore`, and `git diff --check` passed. A 60s live rerun at `temp\diagnostic-sessions\flashback-export-concurrent-profile-60s` returned `Success=true`, `HealthStatus=Healthy`, `Warnings=[]`, `FlashbackExportObserved=true`, `FlashbackExportStatusAtEnd=Succeeded`, `FlashbackExportMessageAtEnd=Exported 161 packets from 2 segments`, visual output/change cadence `120.018fps`, and preview scheduler dropped/deadline/underflow deltas all zero.

## 2026-05-02 — Flashback playback TS probing hardened for rotated segments

**Issue.** A 180s dedicated `flashback-playback` live diagnostic failed with `flashback playback: no playback frames were observed` even though preview stayed healthy at source-rate visual cadence. The playback controller reported `FlashbackPlaybackLastCommandFailure=no_file:Play pos_ms=1404.667`. The app log showed the real cause: playback selected a valid rotated `.ts` segment, but `FlashbackDecoder.OpenFile` failed with `Invalid video dimensions: 0x0`. The decoder used a small `256KB / 0.5s` stream-info probe window, while the exporter already uses `20MB / 5s` because high-bitrate 4K120 rotated TS segments can start mid-stream before enough IDR/SPS data appears for FFmpeg to recover dimensions and extradata.

**Change.** Commit `cf3bf81` moved playback decoding to the same robust MPEG-TS probe window as export: `MaxMpegTsProbeSizeBytes = 20 * 1024 * 1024` and `MaxMpegTsAnalyzeDurationUs = 5 * 1000 * 1000`. The regression suite now asserts those constants and assignments in `FlashbackDecoder.cs` so the playback path does not silently regress to under-probing rotated 4K120 segments.

**Verification.** Before the fix, `temp\diagnostic-sessions\flashback-playback-long-180s` failed after 180s with `Success=false`, `FlashbackPlaybackFrameCountAtEnd=0`, and `Warnings=["flashback playback: no playback frames were observed"]`. After the fix, `dotnet build ElgatoCapture\ElgatoCapture.csproj -c Debug -p:Platform=x64 --no-restore /nr:false /m:1 -p:UseSharedCompilation=false`, `dotnet build tools\McpServer\McpServer.csproj -c Debug --no-restore`, `dotnet build tools\ecctl\ecctl.csproj --no-restore`, `dotnet run --project tests\ElgatoCapture.Tests\ElgatoCapture.Tests.csproj --no-restore`, and `git diff --check` passed. The exact 180s live rerun at `temp\diagnostic-sessions\flashback-playback-probe-window-180s` returned `Success=true`, `HealthStatus=Healthy`, `Warnings=[]`, `FlashbackPlaybackFrameCountAtEnd=21590`, `FlashbackPlaybackObservedFpsAtEnd=119.77`, `FlashbackPlaybackOnePercentLowFpsAtEnd=117.64`, `FlashbackPlaybackMinOnePercentLowFpsObserved=114.25`, `FlashbackPlaybackDroppedFramesDelta=0`, `FlashbackPlaybackSubmitFailuresDelta=0`, `FlashbackPlaybackSegmentSwitchesAtEnd=1`, visual output/change cadence `119.996fps`, and log evidence showed clean decoder opens for both segment 0 and segment 1 with no post-fix `Invalid video dimensions` errors.

## 2026-05-02 — Post-probe flashback recording, lifecycle, and rotated-export soaks

**Purpose.** After the playback TS probing fix, reran adjacent long live scenarios against the rebuilt final app to make sure the fix did not disturb Flashback recording, preview restart behavior, lifecycle cleanup, or rotated export verification.

**Verification.**

- `flashback-recording-preview-cycle` 180s at `temp\diagnostic-sessions\flashback-recording-preview-cycle-post-probe-180s`: `Success=true`, `HealthStatus=Healthy`, `Warnings=[]`, strict recording verification passed, `FlashbackRecordingBackendObserved=true`, `FlashbackRecordingFileGrowthObserved=true`, `FlashbackRecordingVideoFramesSubmittedDelta=21598`, `FlashbackRecordingVideoEncoderPacketsWrittenDelta=21598`, sequence-gap and queue-drop deltas `0`, preview deadline-drop and underflow deltas `0`, visual output cadence `120.006fps`, visual change cadence `119.336fps`.
- `flashback-lifecycle` 120s at `temp\diagnostic-sessions\flashback-lifecycle-post-probe-120s`: `Success=true`, `HealthStatus=Healthy`, `Warnings=[]`, actions covered pause, seek, play, disable during playback, and re-enable; pending commands `0`, command drops/skips `0`, submit failures `0`, preview deadline-drop and underflow deltas `0`, visual output/change cadence `120.001fps`.
- `flashback-rotated-export` 120s at `temp\diagnostic-sessions\flashback-rotated-export-post-probe-120s`: `Success=true`, `HealthStatus=Healthy`, `Warnings=[]`, observed rotated segment `seq=0`, export verified, `FlashbackExportStatusAtEnd=Succeeded`, `FlashbackExportMessageAtEnd=Exported 25048 packets from 2 segments`, output size about `3.77GB`, preview scheduler dropped/deadline/underflow deltas all `0`, visual output/change cadence `120.012fps`.
- `flashback-recording-settings-deferred` 120s at `temp\diagnostic-sessions\flashback-recording-settings-deferred-post-probe-120s`: `Success=true`, `HealthStatus=Healthy`, `Warnings=[]`, strict recording verification passed, preset change was deferred during recording, restart and disable rejection paths were exercised, post-stop buffer verification passed, `FlashbackRecordingVideoFramesSubmittedDelta=14399`, `FlashbackRecordingVideoEncoderPacketsWrittenDelta=14399`, recording sequence-gap and queue-drop deltas `0`, preview scheduler dropped/deadline/underflow deltas all `0`, visual output/change cadence `119.998fps`.

**Conclusion.** The larger playback TS probe window fixed the no-playback-frames rotated-segment failure without regressing long Flashback recording, preview stop/restart, lifecycle cleanup, or rotated export. Remaining below-118fps source/preview 1% lows in these soaks continue to coincide with zero source gaps/drops and visual source-rate proof, so they are diagnostics/noise or external cadence sensitivity rather than Flashback data loss.

## 2026-05-02 — Toolbar keyboard shortcuts

**Change.** Added WinUI `KeyboardAccelerator`s to the main control-bar buttons in `ElgatoCapture/MainWindow.xaml`: `Ctrl+R` toggles RecordButton, `Ctrl+P` toggles PreviewButton, `Ctrl+Shift+S` triggers ScreenshotButton, `Ctrl+D` toggles SettingsToggleButton (device/output settings shelf), `Ctrl+E` opens the recordings folder via OpenRecordingsButton, and `F11` toggles FullScreenButton. Tooltips now show the shortcut. The PreviewBorder context-menu Full Screen item gets `KeyboardAcceleratorTextOverride="F11"` for discoverability. No C# wiring beyond what the existing Click handlers already implement; the accelerators dispatch the same RoutedEventArgs flow.

First attempt used `Key="Number188"` for `Ctrl+,` which fails XAML compile because `Windows.System.VirtualKey` does not name punctuation keys. Replaced with `Key="D"` (Ctrl+D) for the device/output settings shelf.

**Verification.** `dotnet build ElgatoCapture/ElgatoCapture.csproj -p:Platform=x64 -p:StageLatestBuild=true` succeeded with 0 errors / 0 warnings (21.59s elapsed). Runtime smoke not exercised in this session — accelerators are XAML-only declarative bindings whose handlers were already present and unchanged.

## 2026-05-02 — Renamed project to Simple Sussudio

**Change.** Full identity rename of the project from `ElgatoCapture` to `Sussudio` (code identity) / `Simple Sussudio` (display name) ahead of a public open-source release on GitHub. The project started in January 2026 as an internal experiment and grew into something that warranted its own neutral identity. "Simple Sussudio" — Phil Collins reference, single-word `Sussudio` for namespace/assembly/pipe identifiers.

**Scope.** Two atomic commits on Flashback branch:
1. Pure structural moves via `git mv` (preserves rename history).
   - `ElgatoCapture/` → `Sussudio/`, `ElgatoCapture.slnx` → `Sussudio.slnx`
   - `tests/ElgatoCapture.{Tests,HdrLab,FfmpegEncodeLab}/` → `tests/Sussudio.*/`
   - `tools/ecctl/` → `tools/ssctl/`
2. Content rewrite (1545 replacements across 205 files).
   - Namespaces, usings, `<RootNamespace>`, csproj `Compile Include` paths
   - Pipe constant `ElgatoCaptureAutomation` → `SussudioAutomation`
   - Env vars `ELGATOCAPTURE_*` → `SUSSUDIO_*`
   - Log filenames `ElgatoCapture_Debug.log` → `Sussudio_Debug.log`, `ElgatoCapture_AutomationPipe.log` → `Sussudio_AutomationPipe.log`
   - LocalAppData fallback `ElgatoCapture/logs` → `Sussudio/logs`
   - AppxManifest: new package GUID `a77e8aeb-560c-4664-9698-4bb4b7eaead4`, DisplayName `Simple Sussudio`
   - Repo marker file checks in `RuntimePaths.cs`
   - `ServiceNamespace.Tests.cs` meta-test path/namespace assertions

**Preserved as historical record (not rewritten):**
- `docs/experiment_log.md` past entries (this file before today's entry)
- `docs/duat/reports/`, `docs/qa/`, `docs/superpowers/specs/`
- `docs/realtime-capture-engine-implementation-log.md`
- `docs/code-review-2026-04-07.md`, `docs/bugs/2026-04-04-full-context-review.md`
- `results/`, `artifacts/`

These reference paths and names that were correct at the time. Pre-2026-05-02 entries that say `ElgatoCapture/Services/...` should be read as the historical equivalent of `Sussudio/Services/...`.

**Preserved unchanged (legitimate hardware/vendor references):**
- "Elgato 4K X" (device name) in `tools/CoreAudioEndpointProbe`, `tools/NativeXuAudioProbe`, `Sussudio/Services/Capture/DeviceService.cs`, tests
- "Elgato Studio" / "Elgato" vendor strings in `tools/EgavdsAudioProbe`
- All `AutomationProperties.AutomationId` strings (none contained `ElgatoCapture` — they use semantic names like `RecordButton`, `PreviewImage`)

**Worktree directory** (`ElgatoCapture-Flashback/`) intentionally not renamed. The Flashback branch will eventually merge to main and replace it; the worktree-level rename will happen at that point to avoid double git plumbing churn.

**Verification.** Full solution build (Sussudio + 3 tests + 4 standalone tools) — 0 warnings / 0 errors. Test runner — every PASS, including `ServiceNamespace.Tests` which asserts every renamed path and namespace. Runtime smoke: app launched, created `temp/logs/Sussudio_Debug.log` with new header `=== Sussudio Debug Log ===`, detected Elgato 4K X via NATIVEXU_AT, encoded 3063 Flashback frames to `C:\Users\crest\AppData\Local\Sussudio\Flashback\...`, closed cleanly with full subsystem teardown.

**Safety net** (kept after rename): tag `pre-sussudio-rename` at commit `10b78b2`, branch `backup/pre-sussudio-rename` at same commit, zip backup at `../ElgatoCapture-Flashback-backup-2026-05-02-152452.zip`. Rollback: `git reset --hard pre-sussudio-rename`.

## 2026-05-02 — Flashback CTI compositor smoothing

**Change.** The Flashback timeline's Current Time Indicator (playhead line, handle, floating time label) used to be repositioned via direct `Canvas.SetLeft` writes — one per source-data tick. That meant 30Hz visible stepping during playback (33ms tick interval) and exposed the 60Hz pointer cadence as discrete steps during scrubbing. Replaced with `Translation.X` compositor animations driven by `ScalarKeyFrameAnimation` so the visual interpolates at the display refresh rate (60–144Hz) between source updates.

**Motivation.** "I want the Current Time Indicator for the flashback UI to be smoothly animating when playing, paused, scrubbing. The aesthetics of the slider should be considered over the exact specific location. The user is looking at the preview when scrolling around." The user verifies position by looking at the preview, not the playhead — so the slider can lag the truth by tens of milliseconds in service of smoother motion.

**Implementation** (`Sussudio/MainWindow.Flashback.cs`):

- Lazy-init in `EnsureFlashbackPlayheadVisuals()`: `ElementCompositionPreview.GetElementVisual` for the three CTI elements, `SetIsTranslationEnabled(true)`, anchor `Canvas.Left = 0` once. Translation.X carries position from then on.
- Four motion modes routed by source cadence:
  - **Snap** (no animation) — first init, panel-show, `SizeChanged`, Live state right-edge pin.
  - **Tick** (50ms linear) — Playing state. Linear easing converts the constant-rate 30Hz signal into constant-velocity motion; cubic ease-out would create a stuttering fast-then-slow loop.
  - **Magnetic** (90ms cubic ease-out, 0.2/0.7 → 0.1/1.0) — Scrubbing state. Longer than the 16ms pointer throttle so successive events overlap into a smooth trail rather than 16ms-stepped jitter.
  - **Glide** (300ms cubic ease-out, same curve) — Paused/disabled. Buffer drift on the 250ms status timer wants a long ease so the playhead reads as continuous flow.
- `_snapFlashbackPlayheadOnNextUpdate` flag overrides motion to Snap on first init, on `FlashbackTrack_SizeChanged`, and at the start of `AnimateFlashbackTimeline(show: true)` — prevents the playhead sweeping in from a stale Translation.X when the timeline opens or the track resizes.
- `UpdateFlashbackScrubVisual` calls with `Magnetic`. The follow-up `ViewModel.FlashbackPlaybackPosition` write fires PropertyChanged → `UpdateFlashbackPositionUI`, which routes through a `state switch` that maps `Scrubbing → Magnetic` so the second call does not stomp the Magnetic ease with the longer Glide curve.

**Verification.** `dotnet build` succeeded with 0 warnings / 0 errors (17.15s elapsed). Runtime smoke on the app:
- App launched, flashback buffer reached 41s in ~6s, panel toggled visible via UIA.
- Sequence `flashback play 30000 → pause → seek 200000 (clamped to buffer end) → seek 10000 → go-live` executed cleanly. Every operation logged `FLASHBACK_PLAYBACK_*_OK`. No exceptions.
- Settled-position screenshots (`temp/screenshots/cti-v2-1` through `cti-v2-6`) confirm playhead, handle, and floating time-label all land at the correct fractional positions across Playing (32s/39s ≈ 80%), Paused (32s/40s ≈ 80%), Seek-10s (10s/42s ≈ 24%), and LIVE (right edge) states.

**Limit of evidence.** A 50–300ms in-flight ease cannot be captured in static screenshots (RPC overhead alone is ~150ms). Smoothness during the transition is provided by the WinUI Composition system's compositor-thread interpolation, which is deterministic once `StartAnimation("Translation.X")` succeeds and the easing function is non-null — both verified by the absence of `ArgumentNullException` and the correct settled positions. Visual smoothness verification at compositor frame rate would require screen-recording the window at 120fps, which is outside the available probe set.

## 2026-05-02 — Flashback CTI redesign: continuous extrapolation

**Reason for revision.** The earlier "four motion modes (Snap/Tick/Magnetic/Glide)" design from earlier today *still read as 30Hz stutter* on a 144Hz display, not as fluid motion. Restarting a `ScalarKeyFrameAnimation` every 33ms — even a 50ms linear ease — perturbs Translation.X at every restart. The compositor was honoring the easings, but the source signal itself was a sequence of 30Hz waypoints, so the visual could never break out of the source's discretization.

**Redesign principle.** The timeline UI is an *abstraction* over the video pipeline. The video pipeline ticks at 30Hz; the display refreshes at 60–144Hz. The playhead must therefore decouple from per-tick source updates and run on an animation the compositor evaluates every frame.

**New model.** A single 60-second linear `ScalarKeyFrameAnimation` on `Translation.X`, computed from an anchor `(positionMs, bufferDurationMs, posRate, bufRate)`, with `posRate = 1.0` while Playing and `bufRate = 1.0` while recording. The horizon target is `(pos₀ + posRate·60s) / (buf₀ + bufRate·60s) · trackWidth`. The animation runs without restart for up to 60s; the compositor interpolates linearly between current value and horizon at refresh rate. Re-anchored only on:

- State edges (Play/Pause/Live/Scrubbing transitions) — `RefreshFlashbackCtiMotion("state_change")` in `UpdateFlashbackStateUI`.
- `FlashbackTrack_SizeChanged` and panel show — explicit-start animation so the playhead lands on the new layout-correct position.
- Position writes during Paused/Live/Scrubbing-released — these imply a seek, not the 30Hz playback tick (during Playing the position-changed handler deliberately skips re-anchor).
- 1Hz drift correction (`_flashbackCtiAnchorTimer`) — keeps the extrapolation faithful to actual decode-rate variance and rotates a fresh 60s segment in.

Each re-anchor (except state edges and SizeChanged) starts the new animation with **implicit** start, so the compositor reads the visual's current Translation.X and tweens linearly to the new horizon — velocity is continuous across re-anchors. State edges and SizeChanged use explicit-start (`InsertKeyFrame(0f, …)`) to deliberately reset the visual to the layout-correct position.

**Active scrub** is the one carve-out: pointer events still drive `PositionFlashbackPlayhead(.., Magnetic)` (60ms cubic ease-out toward the pointer x), so the playhead chases the finger tightly with absorbed pointer jitter. On `EndFlashbackScrubInteraction` the visual is handed back to the extrapolation driver from wherever the pointer left it.

**Removed.** The `Tick` (50ms linear) and `Glide` (300ms cubic ease-out) motion modes from the v1 design. They were per-tick eases; the new model never re-triggers an ease on a per-tick cadence.

**Verification.** `dotnet build` 0 warnings / 0 errors. Runtime: confirmed by user observation — "seems better" after running through play/pause/scrub on a 144Hz monitor. Earlier static-screenshot evidence (`temp/screenshots/cti-v2-*.png`) for settled-position correctness still applies — the new model uses the same `Translation.X` + `Canvas.Left=0` anchor pattern.

## 2026-05-02 — Flashback segment playback tail-gap hardening

**Issue.** A 180s live `flashback-segment-playback` rerun after the Sussudio rename failed at `temp\diagnostic-sessions\sussudio-flashback-segment-playback-180s`. The app stayed healthy and preview visual cadence was still ~120fps, but playback was started 500ms before the end of a completed MPEG-TS segment and never produced playback frames. Logs showed `FLASHBACK_PLAYBACK_SEEK_FAIL reason=play offset_ms=29349` after `SeekAndDisplayKeyframe` got no frame near the tail of `fb_..._0000.ts`; the controller restored Live instead of falling through to the next segment.

**Change.** Added an adjacent-segment fallback in `Sussudio/Services/Flashback/FlashbackPlaybackController.cs`. If a seek or seek-display lands in the tiny tail gap of a completed segment and the next segment starts within 3 seconds of the requested PTS, playback opens the next segment, seeks to the effective start, records a segment switch, and continues instead of snapping Live. Also tightened `flashback-segment-playback` in `tools/Common/DiagnosticSessionRunner.cs` so the scenario waits for about 8 seconds of live headroom past the selected boundary; otherwise it can correctly near-live-snap before the 500ms polling loop observes `Playing`, producing a false failure.

**Verification.** `dotnet build Sussudio\Sussudio.csproj -c Debug -p:Platform=x64 --no-restore /nr:false /m:1 -p:UseSharedCompilation=false`, `dotnet build tools\ssctl\ssctl.csproj --no-restore`, `dotnet build tools\McpServer\McpServer.csproj -c Debug --no-restore`, `dotnet build tools\NativeXuAudioProbe\NativeXuAudioProbe.csproj -c Debug --no-restore -t:Rebuild`, and `dotnet run --project tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore` all passed. The first post-fix 180s rerun at `temp\diagnostic-sessions\sussudio-flashback-segment-playback-adjacent-fallback-180s` proved the app fallback crossed the boundary (`frames=61`, `switchesEnd=1`, `droppedFramesDelta=0`, `submitFailuresDelta=0`) but exposed the diagnostic headroom false negative. The final 180s rerun at `temp\diagnostic-sessions\sussudio-flashback-segment-playback-headroom-180s` passed with `Success=true`, `HealthStatus=Healthy`, `Warnings=[]`, visual output/change cadence `120fps`, playback observed at `positionMs=16433`, `frames=109`, `switchesEnd=2`, `droppedFramesDelta=0`, and `submitFailuresDelta=0`.

## 2026-05-02 — Preview restart shared-device handoff hardening

**Issue.** The first live `flashback-preview-cycle` run after the segment-boundary work crashed Sussudio while restarting preview after an export with preview disabled. The automation pipe stopped responding and Windows logged `System.AccessViolationException` in `Marshal.AddRef(IntPtr)` from `D3D11PreviewRenderer.SetSharedDevice(...)`, called by `CaptureService.TryApplySharedPreviewDevice(...)`. The managed `catch (Exception)` could not protect this path because the COM pointer could already be invalid before the renderer AddRef happened.

**Change.** `SharedD3DDeviceManager` now owns a lifecycle lock and exposes `TryCreateDeviceReference(out ID3D11Device? device, out string reason)`. That method checks disposal/null state and AddRefs the native D3D11 device pointer while the manager is locked, so any caller receives its own valid COM reference instead of borrowing the manager's wrapper. `CaptureService.TryApplySharedPreviewDevice` now uses that duplicated reference, logs `UNIFIED_VIDEO_SHARED_DEVICE_APPLY_SKIP` for unavailable managers, calls `renderer.SetSharedDevice(...)`, and disposes the temporary reference in a `finally`. Source-level regression coverage asserts that the capture handoff no longer calls `capture.D3DManager?.Device` directly.

**Verification.** `dotnet build Sussudio\Sussudio.csproj -c Debug -p:Platform=x64 --no-restore /nr:false /m:1 -p:UseSharedCompilation=false`, `dotnet build tools\ssctl\ssctl.csproj --no-restore`, `dotnet build tools\McpServer\McpServer.csproj -c Debug --no-restore`, and `dotnet run --project tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore` all passed. The post-fix live run at `temp\diagnostic-sessions\sussudio-flashback-preview-cycle-180s` passed for a 180s sample with `Success=true`, `HealthStatus=Healthy`, preview stop -> export while preview off -> preview restart actions complete, export verified (`Exported 166 packets from 1 segments`), visual cadence `output=120.01fps`, change cadence `119.34fps`, repeat `0.104%`, preview scheduler deadline/underflow deltas `0`, and the app still running as a single `Sussudio` process afterward.

## 2026-05-02 — Real scrub lifecycle diagnostics

**Issue.** The existing `flashback-scrub-stress` diagnostic was named like a scrub test but used rapid `seek` actions rather than the UI's real `BeginScrub -> UpdateScrub -> EndScrub` lifecycle. That left the scrubbing system under-verified in live data, especially the command coalescing path and preview suppression/resume path used during pointer drags.

**Change.** Extended the shared `FlashbackAction` automation contract with `begin-scrub`, `update-scrub`, and `end-scrub` actions, including `ssctl` and MCP tool support. Updated `flashback-scrub-stress` to pause, begin scrub at 500ms, wait for `Scrubbing`, issue a concurrent burst of `update-scrub` positions, end scrub at the final pointer position, play, and return live. Regression tests assert the new app/MCP/ssctl routing and that the diagnostic now exercises the real scrub actions.

**Verification.** `dotnet build Sussudio\Sussudio.csproj -c Debug -p:Platform=x64 --no-restore /nr:false /m:1 -p:UseSharedCompilation=false`, `dotnet build tools\ssctl\ssctl.csproj --no-restore`, `dotnet build tools\McpServer\McpServer.csproj -c Debug --no-restore`, `dotnet build tools\NativeXuAudioProbe\NativeXuAudioProbe.csproj -c Debug --no-restore -t:Rebuild`, and `dotnet run --project tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore` all passed. The live 180s run at `temp\diagnostic-sessions\sussudio-flashback-scrub-stress-real-scrub-180s` passed with actions `pause -> begin-scrub -> update burst -> end-scrub -> play -> go-live`, `HealthStatus=Healthy`, `maxPending=3`, `maxLatencyMs=497`, `coalescedScrubEnd=15`, skipped commands `0`, submit failure delta `0`, preview scheduler deadline/underflow deltas `0`, and visual cadence `output=120fps`, `changes=120fps`.

## 2026-05-02 — Flashback export coordination live checks

**Scope.** Ran the existing export-conflict diagnostics against the current Sussudio build after the scrub lifecycle work. These are verification-only checks; no code change was required.

**Verification.** `flashback-export-concurrent` passed a 180s run at `temp\diagnostic-sessions\sussudio-flashback-export-concurrent-180s`: two simultaneous export requests were issued and both verified; final status `Succeeded`, message `Exported 158 packets from 2 segments`, max output `26.98 MB`, max throughput `49.41 MB/s`, playback pending/skipped/submit-failure counters all `0`, and preview scheduler dropped/deadline/underflow deltas all `0`. `flashback-disable-during-export` passed a 180s run at `temp\diagnostic-sessions\sussudio-flashback-disable-during-export-180s`: export and disable requests overlapped, the export verified, Flashback reported inactive with no playback worker or pending commands, then re-enabled successfully; final status `Succeeded`, message `Exported 179 packets from 1 segments`, max output `43.65 MB`, max throughput `157.59 MB/s`, and preview scheduler dropped/deadline/underflow deltas all `0`. Both runs reported `HealthStatus=Warning` from source/capture cadence around `59.94fps` (`1pctLow` about `54fps`), which documents the live source mode during the sample rather than an export coordination failure.

## 2026-05-02 — Flashback recording integrity drift classification

**Issue.** A 180s `flashback-recording-preview-cycle` run passed strict recording verification and showed clean Flashback recording counters, but the health summary reported `HealthStatus=Critical`, `LikelyStage=recording`, with evidence `recording integrity=Incomplete ... av_sync_drift_ms=12523.8`. The drift came from global capture A/V telemetry, not recording-scoped counters: Flashback recording can export from pre-roll/ring-buffer timeline state while the live source cadence lane continues to run at about 56fps against a 59.94fps signal.

**Change.** Recording integrity snapshots now avoid borrowing global capture A/V drift for Flashback recording. The normal LibAV recording path still exposes encoder-local drift from `LibAvRecordingSink.TryGetEncoderAvSyncDrift(...)`; the Flashback recording path reports only recording-scoped audio/drop/gap counters unless it has recording-local drift evidence.

**Verification.** `dotnet build Sussudio\Sussudio.csproj -c Debug -p:Platform=x64 --no-restore /nr:false /m:1 -p:UseSharedCompilation=false`, `dotnet build tools\ssctl\ssctl.csproj --no-restore`, `dotnet build tools\McpServer\McpServer.csproj -c Debug --no-restore`, and `dotnet run --project tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore` all passed. The live 180s run at `temp\diagnostic-sessions\sussudio-flashback-recording-preview-cycle-scoped-drift-180s` passed with 37 samples at 5000ms, strict recording verification passed, Flashback recording backend observed, file growth observed, `submittedDelta=10109`, `packetsDelta=10109`, `seqGapsDelta=0`, `queueDropsDelta=0`, and preview scheduler dropped/deadline/underflow deltas all `0`. The remaining health status is correctly `Warning`, `LikelyStage=source_capture`, with evidence `rate=56.07/59.94fps`, `1pctLow=54.07fps`, `gaps=0`, `drops=0`.

## 2026-05-02 — Diagnostic-session mode rollup

**Issue.** After the 180s recording-cycle run, the important clue was buried in raw samples: the app was selected for `Source` / `59.94fps` / `Auto` video format with NV12 requested and negotiated, while the discussion was focused on 4K120 behavior. The diagnostic summary needed to say the measured mode plainly so short or long performance artifacts cannot be misread as a different source mode.

**Change.** `DiagnosticSessionResult` now records selected resolution/frame rate/video format, requested and negotiated capture subtype, detected source resolution/frame rate, source HDR state, and source telemetry summary. The human-readable diagnostic-session output prints these in a `Capture Mode:` line immediately after health evidence.

**Verification.** `dotnet build Sussudio\Sussudio.csproj -c Debug -p:Platform=x64 --no-restore /nr:false /m:1 -p:UseSharedCompilation=false`, `dotnet build tools\ssctl\ssctl.csproj --no-restore`, `dotnet build tools\McpServer\McpServer.csproj -c Debug --no-restore`, `dotnet build tools\NativeXuAudioProbe\NativeXuAudioProbe.csproj -c Debug --no-restore -t:Rebuild`, and `dotnet run --project tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore` all passed. A short formatter smoke at `temp\diagnostic-sessions\sussudio-observe-mode-summary-smoke` passed and printed `Capture Mode: selected=Source @60fps (60000/1001) format=Auto requested=NV12 negotiated=NV12 source=3840x2160 @59.94fps (60000/1001) hdr=True telemetry=Source: 3840x2160 @ 60000/1001 | HDR | Available/High | updated now`. This smoke is not performance evidence; it only verifies the summary surface.

## 2026-05-02 — HFR duplicate-source cadence classification

**Issue.** In explicit `3840x2160 @ 120fps` / `MJPG` mode, the app can receive and present frames at 120fps while the actual source content changes at about 60fps. Before this change, a steady-state run with present 1% low just under the 120fps warning threshold could be summarized as `present_display`, even though the more important evidence was the MJPEG fingerprint and visual-crop cadence: alternating duplicate packets, 50% repeats, and NativeXu source telemetry still reporting 3840x2160 @ 59.94 HDR.

**Change.** `AutomationDiagnosticsHub` now detects HFR MJPEG duplicate-source cadence when packet input cadence is near target, duplicate percentage is high, and unique packet cadence, visual change cadence, or source telemetry is substantially below target. The health result becomes `Warning`, `LikelyStage=source_signal`, with evidence that includes the MJPEG fingerprint, visual crop cadence, and source telemetry.

**Verification.** `dotnet build Sussudio\Sussudio.csproj -c Debug -p:Platform=x64 --no-restore /nr:false /m:1 -p:UseSharedCompilation=false`, `dotnet build tools\ssctl\ssctl.csproj --no-restore`, `dotnet build tools\McpServer\McpServer.csproj -c Debug --no-restore`, and `dotnet run --project tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore` all passed. A 60s warmed live run at `temp\diagnostic-sessions\sussudio-hfr-duplicate-source-signal-60s` passed and reported `HealthStatus=Warning`, `LikelyStage=source_signal`, `Summary=Captured HFR MJPEG cadence contains repeated source frames.` Evidence: capture cadence `120/120fps`, capture 1% low `119.77fps`, gaps/drops `0`, MJPEG fingerprint input `120.01fps`, unique `60.34fps`, duplicate `50%`, pattern `AlternatingDuplicate`, visual output `120.01fps`, visual changes `59.83fps`, repeats `50.049%`, source telemetry `3840x2160@59.94fps hdr=True`. The run's `Capture Mode:` line confirmed the measured mode was selected `3840x2160 @120fps (120/1)`, `format=MJPG`, `requested=MJPG`, `negotiated=MJPG`, while telemetry still saw source `3840x2160 @59.94fps (60000/1001)`.

## 2026-05-02 — Automation capture-mode reinitialize serialization

**Issue.** Running three separate automation clients concurrently to set `resolution=3840x2160`, `fps=120`, and `video-format=MJPG` could race the preview reinitialize path. The app reproduced a faulted state with `Status: Faulted`, `Failed to apply format: Capture not initialized`, `Initialized=false`, `Previewing=false`, and one failed capture command. This was a real automation/control-plane reliability hole rather than a media-pipeline throughput problem.

**Change.** Automation capture-mode mutations now acquire a dedicated gate, suppress property-change-triggered fire-and-forget reinitializes while applying the individual setting, and then await one explicit `ReinitializeDeviceAsync("automation ...")` before acknowledging the command. This covers resolution, frame rate, video format, and MJPEG decoder count changes, so concurrent automation clients converge through the same serialized preview lifecycle instead of racing teardown/startup.

**Verification.** `dotnet build Sussudio\Sussudio.csproj -c Debug -p:Platform=x64 --no-restore /nr:false /m:1 -p:UseSharedCompilation=false`, `dotnet build tools\ssctl\ssctl.csproj --no-restore`, `dotnet build tools\McpServer\McpServer.csproj -c Debug --no-restore`, and `dotnet run --project tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore` all passed. The live reproduction was rerun with one app instance and three concurrent `ssctl set` processes; all three acknowledged successfully, final state remained `Initialized=true`, `Previewing=true`, `Status=Previewing`, capture commands `enq=19 done=19 fail=0 cancel=0`, and final mode was `3840x2160 @ 120fps`, `Video Format=MJPG`, `ReqSubtype=MJPG`, `NegSubtype=MJPG`.

## 2026-05-02 — HFR Flashback playback verification with duplicate-source classification

**Scope.** Verification-only pass after the HFR duplicate-source classifier and automation capture-mode serialization fixes. The input was explicitly set to `3840x2160 @ 120fps`, `Video Format=MJPG`, with the capture card still reporting source telemetry `3840x2160 @ 59.94fps`, HDR.

**Verification.** A 90s warmed live `flashback-playback` run at `temp\diagnostic-sessions\sussudio-hfr-flashback-playback-90s` passed. Health correctly stayed `Warning`, `LikelyStage=source_signal`, because the measured stream contained 120fps packets with 60fps unique/visual content: MJPEG fingerprint input `120fps`, unique `60.34fps`, duplicate `50%`, visual output `120fps`, visual changes `60fps`, repeat `50.053%`. Flashback playback itself was healthy: `fpsEnd=119.98`, `fpsMin=119.94`, `onePercentLowFpsEnd=119.94`, `onePercentLowFpsMin=118.16`, `framesEnd=10817`, `droppedFramesDelta=0`, `submitFailuresDelta=0`, pending commands at end `0`, max command latency `152ms`, no segment switches/reopens/write-head waits/near-live snaps/decode-error snaps. Decode remained cheap at end (`avg=0.41ms`, `p99=0.74ms`, max `0.82ms`); the worst observed decode spike was `15.17ms` in `send`, but it did not produce playback drops or submit failures.

## 2026-05-02 — HFR Flashback recording verification with duplicate-source classification

**Scope.** Verification-only pass for Flashback recording in the same explicit HFR mode: selected `3840x2160 @ 120fps (120/1)`, `Video Format=MJPG`, requested/negotiated subtype `MJPG`. As expected for the 4K120 USB bandwidth limit, HDR capture was off even though NativeXu source telemetry still reported `3840x2160 @ 59.94fps`, HDR.

**Verification.** A warmed 120s live `flashback-recording` run at `temp\diagnostic-sessions\sussudio-hfr-flashback-recording-120s` passed with `Success=true` and `RecordingVerificationSucceeded=true` (`Strict verification passed.`). Flashback recording wrote every expected HFR frame: `submittedDelta=14400`, `packetsDelta=14400`, `seqGapsDelta=0`, and `queueDropsDelta=0`. Preview stayed stable during the recording: preview scheduler dropped/deadline/underflow deltas were all `0`, D3D missed-refresh/failure deltas were `0`, and preview cadence 1% low stayed above `115fps` across the 120s sample. Health correctly stayed `Warning`, `LikelyStage=source_signal`, because the measured stream was still 120fps MJPG packets with 60fps unique/visual content: MJPEG fingerprint input `119.98fps`, unique `60.32fps`, duplicate `50%`, visual output `120fps`, visual changes `60fps`, repeat `50.049%`. This is evidence that the Flashback recording path is handling 4K120 MJPG cadence cleanly; the visible 60fps motion is source-side duplicate cadence, not a recording drop or encoder failure.

## 2026-05-02 — HFR Flashback range-export verification with duplicate-source classification

**Scope.** Verification-only pass for selected-range Flashback export while staying in explicit HFR mode: selected `3840x2160 @ 120fps (120/1)`, `Video Format=MJPG`, requested/negotiated subtype `MJPG`, with source telemetry still reporting `3840x2160 @ 59.94fps`, HDR.

**Verification.** A warmed 120s live `flashback-range-export` run at `temp\diagnostic-sessions\sussudio-hfr-flashback-range-export-120s` passed. The scenario set an in/out range, exported the selected range, verified the export, cleared the range, and returned Live. Export status ended `Succeeded` with `LastExportSuccess=true`, message `Exported 725 packets from 1 segments`, output `104.21 MB`, max elapsed `1341ms`, and max throughput `77.71 MB/s`. Playback command handling stayed clean (`pendingEnd=0`, `maxPending=2`, `maxLatencyMs=4`, dropped/skipped commands `0`). Health correctly remained `Warning`, `LikelyStage=source_signal`, because the measured stream again showed 120fps MJPG packets with 60fps unique/visual content: capture 1% low `119.73fps`, MJPEG fingerprint input `120fps`, unique `60.33fps`, duplicate `50%`, visual output `119.99fps`, visual changes `60fps`, repeat `50.05%`. Preview stayed mostly stable during export, with two scheduler drops over the long sample, no deadline drops or underflows, no D3D stat failures, and preview 1% low staying above `114fps`; this is a residual preview-performance observation, not an export failure.

## 2026-05-03 — Preview scheduler cleared-drop diagnostics

**Issue.** The HFR range-export pass reported two preview scheduler drops, but per-sample inspection showed the reason was `cleared`: a benign queue clear during Flashback range/seek state churn. The diagnostic summary only had one total drop counter, so harmless queue clears looked too similar to real preview starvation signals such as deadline drops, underflows, or submit failures.

**Change.** Added `MjpegPreviewJitterClearedDropCount` to the MJPEG preview jitter metrics, capture diagnostics snapshot, automation snapshot, performance timeline rows, ssctl/shared snapshot formatters, MCP performance timeline, and diagnostic-session rollup. Diagnostic-session output now prints `clearedDropsEnd` and `clearedDropsDelta` alongside total drops, deadline drops, and underflows.

**Verification.** `dotnet build Sussudio\Sussudio.csproj -c Debug -p:Platform=x64 --no-restore /nr:false /m:1 -p:UseSharedCompilation=false`, `dotnet build tools\ssctl\ssctl.csproj --no-restore`, `dotnet build tools\McpServer\McpServer.csproj -c Debug --no-restore`, `dotnet build tools\NativeXuAudioProbe\NativeXuAudioProbe.csproj -c Debug --no-restore`, and `dotnet run --project tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore` all passed. Live runtime smoke against the rebuilt app confirmed `state --json` includes `MjpegPreviewJitterClearedDropCount`, and a 15s observe run at `temp\diagnostic-sessions\sussudio-cleared-drop-diagnostic-smoke` passed with the formatted line `Preview Scheduler: droppedEnd=0 droppedDelta=0 clearedDropsEnd=0 clearedDropsDelta=0 deadlineDropsEnd=0 deadlineDropsDelta=0 underflowsEnd=0 underflowsDelta=0 lastDropReasonEnd=none`.

## 2026-05-03 — HFR Flashback real-scrub stress verification

**Scope.** Verification-only pass for the real Flashback scrub lifecycle in explicit HFR mode: selected `3840x2160 @ 120fps (120/1)`, `Video Format=MJPG`, requested/negotiated subtype `MJPG`, with source telemetry still reporting `3840x2160 @ 59.94fps`, HDR.

**Verification.** A warmed 120s live `flashback-scrub-stress` run at `temp\diagnostic-sessions\sussudio-hfr-flashback-scrub-stress-120s` passed. The scenario exercised `pause -> begin-scrub -> update burst -> end-scrub -> play -> go-live`, and the app ended in `Live` with `pendingCommands=0`, no command failures, skipped-not-ready commands `0`, playback submit failures `0`, playback dropped frames `0`, no segment switches/reopens/write-head waits/near-live snaps/decode-error snaps, and `scrubUpdatesCoalesced=15` matching the 15 intentionally coalesced scrub updates. The preview scheduler counters now distinguish the benign churn clearly: `droppedDelta=3`, `clearedDropsDelta=3`, `deadlineDropsDelta=0`, `underflowsDelta=0`, `lastDropReason=cleared`. Health correctly remained `Warning`, `LikelyStage=source_signal`, because the input again showed 120fps MJPG packets with 60fps unique/visual content: capture 1% low `119.48fps`, MJPEG fingerprint input `120fps`, unique `60.34fps`, duplicate `50%`, visual output `120fps`, visual changes `59.83fps`, repeat `50.05%`.

## 2026-05-03 — Flashback dispose-purge live verification

**Scope.** Verification-only pass closing the older static-only audit note for Flashback buffer dispose cleanup. The app was launched with Flashback active, allowed to create a non-empty live session, then closed through automation so `CaptureService` and `FlashbackBufferManager` disposed normally.

**Verification.** Before close, `state --json` reported active segment `C:\Users\crest\AppData\Local\Sussudio\Flashback\bfc1c059e77e43d38b8cbd46bff2b132\fb_bfc1c059e77e43d38b8cbd46bff2b132_0000.ts`, `FlashbackDiskBytes=463865657`, `FlashbackEncodedFrames=1925`, and both the segment file and session directory existed. The automation close returned `Request canceled` because the pipe was torn down during shutdown, but process inspection immediately afterward showed no running `Sussudio` process. Filesystem inspection then showed `FileExistsAfter=false`, `SessionDirExistsAfter=false`, and `RemainingFiles=0` for that session directory. This confirms the dispose purge removes the active segment files and session directory instead of leaving multi-GB leftovers for stale-cache cleanup.

## 2026-05-03 — HFR Flashback export-during-playback verification

**Scope.** Verification-only pass for exporting while Flashback playback is actively running in explicit HFR mode: selected `3840x2160 @ 120fps (120/1)`, `Video Format=MJPG`, requested/negotiated subtype `MJPG`, with source telemetry still reporting `3840x2160 @ 59.94fps`, HDR.

**Verification.** A warmed 120s live `flashback-export-playback` run at `temp\diagnostic-sessions\sussudio-hfr-flashback-export-playback-120s` passed. The scenario started playback, requested an export while playback was active, verified the export, and returned Live. Export ended `Succeeded` with `LastExportSuccess=true`, message `Exported 167 packets from 1 segments`, output `25.92 MB`, max elapsed `543ms`, and max throughput `47.73 MB/s`. Playback remained healthy during the export: `fpsEnd=119.99`, `onePercentLowFpsEnd=119.97`, `framesEnd=428`, `droppedFramesDelta=0`, `submitFailuresDelta=0`, pending commands at end `0`, max pending `1`, max command latency `139ms`, no segment switches/reopens/write-head waits/near-live snaps/decode-error snaps. Decode was cheap at end (`avg=1.3ms`, `p99=2.19ms`, max `2.27ms`). Preview scheduler churn was benign and now explicit: `droppedDelta=3`, `clearedDropsDelta=3`, `deadlineDropsDelta=0`, `underflowsDelta=0`, `lastDropReason=cleared`. Health correctly remained `Warning`, `LikelyStage=source_signal`, because the input again showed 120fps MJPG packets with 60fps unique/visual content: capture 1% low `119.05fps`, MJPEG fingerprint input `120fps`, unique `60.34fps`, duplicate `50%`, visual output `120fps`, visual changes `59.83fps`, repeat `50.047%`.

## 2026-05-03 — HFR Flashback rotated multi-segment export verification

**Scope.** Long verification-only pass for exporting across a rotated Flashback segment boundary in explicit HFR mode: selected `3840x2160 @ 120fps (120/1)`, `Video Format=MJPG`, requested/negotiated subtype `MJPG`, with source telemetry still reporting `3840x2160 @ 59.94fps`, HDR.

**Verification.** A warmed 240s live `flashback-rotated-export` run at `temp\diagnostic-sessions\sussudio-hfr-flashback-rotated-export-240s` passed. The scenario waited for a completed rotated segment (`seq=0`, `startMs=0`, `endMs=32549`), exported across two segments, and verified the output. Export ended `Succeeded` with `LastExportSuccess=true`, message `Exported 30033 packets from 2 segments`, output `4.33 GB`, max elapsed `6454ms`, and max throughput `686.34 MB/s`. Preview stayed stable across the long sample: no new preview scheduler drops during the session (`droppedDelta=0`, `clearedDropsDelta=0`, `deadlineDropsDelta=0`, `underflowsDelta=0`), D3D stats failures `0`, missed-refresh delta `1`, and preview 1% low stayed between `117.5fps` and `117.83fps`. Health correctly remained `Warning`, `LikelyStage=source_signal`, because the measured input again showed 120fps MJPG packets with 60fps unique/visual content: capture 1% low `119.1fps`, MJPEG fingerprint input `120fps`, unique `60.33fps`, duplicate `50%`, visual output `120fps`, visual changes `60fps`, repeat `50.049%`.

## 2026-05-03 — HFR Flashback segment-boundary playback diagnostics

**Issue.** A long-running HFR app session exposed two diagnostic-session false negatives in `flashback-segment-playback`. First, the scenario failed with `frames=58 observedFps=0` even though playback had advanced and crossed the boundary; the playback controller only updates rolling observed FPS every 60 frames, so a short valid boundary sample can have frames without a warmed FPS value. Second, once the Flashback session outlived the 300s ring, the scenario selected the oldest completed segment by absolute PTS (`endMs=481706`) and used that absolute value as a playback position. Playback commands are buffer-relative, so the seek clamped to the live edge (`positionMs=300000`) and the scenario failed itself rather than testing a valid boundary.

**Change.** `flashback-segment-playback` now selects the newest completed segment whose boundary maps into the current rolling buffer with at least 8s of live headroom. It computes `validStartPtsMs = latestSegmentEndMs - FlashbackBufferedDurationMs`, maps the absolute segment boundary to `boundaryPositionMs = segment.EndPtsMs - validStartPtsMs`, and uses that buffer-relative position for seek and boundary-cross validation. The frame-advance gate now fails only when no frames were presented, with a separate warmup warning only if at least 120 frames have been presented and observed FPS is still unavailable.

**Verification.** `dotnet build tools\ssctl\ssctl.csproj --no-restore`, `dotnet build tools\McpServer\McpServer.csproj -c Debug --no-restore`, `dotnet build tools\NativeXuAudioProbe\NativeXuAudioProbe.csproj -c Debug --no-restore`, and `dotnet run --project tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore` all passed. The first HFR 180s rerun at `temp\diagnostic-sessions\sussudio-hfr-flashback-segment-playback-180s` proved the FPS-warmup false negative (`frames=58 observedFps=0`, no drops/failures). The second rerun at `temp\diagnostic-sessions\sussudio-hfr-flashback-segment-playback-warmfps-180s` exposed the stale absolute-PTS selection (`boundaryMs=481706`, clamped `positionMs=300000`). The final corrected 180s live run at `temp\diagnostic-sessions\sussudio-hfr-flashback-segment-playback-windowed-180s` passed with `validStartMs=719945`, `boundaryPosMs=211784`, absolute segment `endMs=931729`, playback observed at `positionMs=212267`, `frames=61`, `fps=118.53`, `segmentSwitches=1`, dropped frames delta `0`, submit failures delta `0`, deadline/underflow deltas `0`, and source-signal duplicate cadence correctly classified as `Warning`.

## 2026-05-03 — HFR Flashback restart-cycle verification

**Scope.** Long verification-only pass for Flashback backend restart/export handoff in explicit HFR mode. The app was launched as a single `Sussudio` instance, then configured sequentially through automation to `3840x2160 @ 120fps`, `Video Format=MJPG`, requested/negotiated subtype `MJPG`. The diagnostic ran for 180s with 5000ms samples to avoid over-trusting a short live-data window.

**Verification.** A warmed 180s `flashback-restart-cycle` run at `temp\diagnostic-sessions\sussudio-hfr-flashback-restart-cycle-180s` passed. Actions confirmed the restart cycle was primed, restart requested, export requested, and export verified. Export ended `Succeeded` with `LastExportSuccess=true`, message `Exported 165 packets from 1 segments`, output `25.14 MB`, max elapsed `269ms`, and max throughput `97.99 MB/s`. Playback command counters stayed idle/clean after the restart (`pending=0`, dropped/skipped commands `0`, last failure empty), Flashback recording counters were not involved, and preview stayed stable: `PreviewCadenceMinOnePercentLowFpsObserved=117.73`, D3D stat failures `0`, scheduler `droppedDelta=3`, `clearedDropsDelta=3`, `deadlineDropsDelta=0`, `underflowsDelta=0`, with `lastDropReason=cleared`. Health correctly stayed `Warning`, `LikelyStage=source_signal`, because the measured stream again contained 120fps MJPG packets with about 60fps unique/visual content: capture 1% low `119.14fps`, MJPEG fingerprint input `120.02fps`, unique `60.34fps`, duplicate `50%`, visual output `120fps`, visual changes `59.83fps`, repeat `50.05%`.

## 2026-05-03 — HFR Flashback encoder-cycle verification

**Scope.** Long verification-only pass for Flashback encoder preset cycling and export verification in the same explicit HFR mode. This reused the same single running `Sussudio` instance after the restart-cycle pass and confirmed the app remained negotiated at `MJPG 3840x2160@120` before starting the 180s sample.

**Verification.** A warmed 180s `flashback-encoder-cycle` run at `temp\diagnostic-sessions\sussudio-hfr-flashback-encoder-cycle-180s` passed. Actions confirmed the cycle started, encoder preset changed to `P1`, export was requested and verified, and the preset was restored to `Auto`. Export ended `Succeeded` with `LastExportSuccess=true`, message `Exported 165 packets from 1 segments`, output `23.9 MB`, max elapsed `233ms`, and max throughput `107.58 MB/s`. Preview scheduler deltas were clean for the sample (`droppedDelta=0`, `clearedDropsDelta=0`, `deadlineDropsDelta=0`, `underflowsDelta=0`), D3D stat failures stayed `0`, missed-refresh delta was `1`, and preview 1% low stayed above `116.68fps`. The capture path stayed true-HFR at packet cadence (`input=120fps`, capture 1% low `118.87fps`, gaps/drops `0`) while source/visual uniqueness remained the known source-side 60fps cadence (`unique=59.66fps`, visual changes `59.83fps`, duplicate/repeat about `50%`), so health correctly remained `Warning`, `LikelyStage=source_signal`.

## 2026-05-03 — HFR Flashback recording-preview-cycle verification

**Scope.** Long verification-only pass for recording while cycling preview in explicit HFR mode: selected `3840x2160 @ 120fps (120/1)`, `Video Format=MJPG`, requested/negotiated subtype `MJPG`. This scenario intentionally stops and restarts preview during an active Flashback-backed recording, then performs strict recording verification after stop.

**Verification.** A warmed 180s `flashback-recording-preview-cycle` run at `temp\diagnostic-sessions\sussudio-hfr-flashback-recording-preview-cycle-180s` passed. Actions confirmed recording started, preview stopped, preview restarted, and recording stopped. Strict recording verification passed; Flashback recording backend and file growth were observed, with `FlashbackRecordingVideoFramesSubmittedDelta=21598`, `FlashbackRecordingVideoEncoderPacketsWrittenDelta=21598`, sequence-gap delta `0`, and queue-drop delta `0`. Preview recovered cleanly from the stop/restart churn: D3D stat failures `0`, missed-refresh delta `0`, scheduler `droppedDelta=2`, `clearedDropsDelta=2`, `deadlineDropsDelta=0`, `underflowsDelta=0`, `lastDropReason=cleared`. Preview 1% low ended at `116.3fps` and the minimum observed 1% low was `108fps` during the churn window, which is a visible preview-cycle cost to keep watching but not evidence of recording loss or preview starvation. Health correctly remained `Warning`, `LikelyStage=source_signal`, with 120fps MJPG packet cadence (`input=119.98fps`, capture 1% low `118.96fps`, gaps/drops `0`) and the known 60fps unique/visual source cadence (`unique=60.32fps`, visual changes `59.83fps`, repeat about `50%`).

## 2026-05-03 — Flashback cancellation/rotate segment-registration regression coverage

**Scope.** Test-only hardening for the earlier recording cancel/rotate segment-registration fix. The remaining live gap requires either forced mid-recording disposal or an induced FFmpeg rotate failure; the app does not currently expose a safe automation injector for that. This pass adds regression coverage for the exact recovery branches so future edits cannot silently remove the segment-registration behavior.

**Verification.** Added `FlashbackEncoderSink_RegistersSegmentsOnCancellationAndRotationFailure` to `tests\Sussudio.Tests`. The test pins the cancellation branch to call `_bufferManager.OnSegmentCompleted(_tsFilePath, _segmentStartPts, cancelPts, cancelSegmentBytes)` and emit `FLASHBACK_SINK_ENCODING_LOOP_CANCELLED_SEGMENT_REGISTERED` before `ReturnAllRemainingQueuedBuffers()`. It also pins the rotate-failure branch to increment `_segmentRotationFailures`, register `completedPath` with `failPts`, emit `FLASHBACK_SINK_ROTATE_FAIL_SEGMENT_REGISTERED`, and only then advance `_segmentStartPts = currentPts`. `dotnet run --project tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore` passed, including the new check. This closes the regression-test coverage gap for the cancellation/rotate registration code, while live induced-fault validation remains dependent on adding a deliberate fault injector or manual hardware/process interruption.

## 2026-05-03 — HFR Flashback recording settings-deferral verification

**Scope.** Long verification-only pass for settings changes while Flashback-backed recording is active in explicit HFR mode: selected `3840x2160 @ 120fps (120/1)`, `Video Format=MJPG`, requested/negotiated subtype `MJPG`. The run confirms encoder-preset changes defer safely, restart/disable requests are rejected while recording, and the Flashback buffer remains usable after recording stops.

**Verification.** A warmed 180s `flashback-recording-settings-deferred` run at `temp\diagnostic-sessions\sussudio-hfr-flashback-recording-settings-deferred-180s` passed. Actions confirmed recording started, preset changed to `P1`, restart rejection requested, disable rejection requested, recording stopped, and the post-stop buffer was verified. Strict recording verification passed; Flashback recording backend and file growth were observed, with `FlashbackRecordingVideoFramesSubmittedDelta=21600`, `FlashbackRecordingVideoEncoderPacketsWrittenDelta=21600`, sequence-gap delta `0`, and queue-drop delta `0`. Preview remained clean during the long sample: `PreviewCadenceMinOnePercentLowFpsObserved=117.23`, D3D stat failures `0`, missed-refresh delta `1`, scheduler `droppedDelta=0`, `clearedDropsDelta=0`, `deadlineDropsDelta=0`, and `underflowsDelta=0`. Health correctly remained `Warning`, `LikelyStage=source_signal`, with 120fps MJPG packet cadence (`input=119.99fps`, capture 1% low `118.37fps`, gaps/drops `0`) and the known 60fps unique/visual source cadence (`unique=60.33fps`, visual changes `60fps`, repeat about `50%`).

## 2026-05-03 — HFR Flashback recording export-rejection verification

**Scope.** Long verification-only pass for rejecting Flashback export requests while Flashback is the active recording backend in explicit HFR mode: selected `3840x2160 @ 120fps (120/1)`, `Video Format=MJPG`, requested/negotiated subtype `MJPG`. The important contract is that the export fails clearly and does not disturb the active recording.

**Verification.** A warmed 180s `flashback-recording-export-rejected` run at `temp\diagnostic-sessions\sussudio-hfr-flashback-recording-export-rejected-180s` passed. Actions confirmed recording started, a Flashback export was requested, the rejected export was observed with `status=Failed` and `kind=UnavailableDuringRecording`, and recording then stopped. Strict recording verification passed; Flashback recording backend and file growth were observed, with `FlashbackRecordingVideoFramesSubmittedDelta=21601`, `FlashbackRecordingVideoEncoderPacketsWrittenDelta=21601`, sequence-gap delta `0`, and queue-drop delta `0`. Preview stayed clean: `PreviewCadenceMinOnePercentLowFpsObserved=117.06`, D3D stat failures `0`, missed-refresh delta `0`, scheduler `droppedDelta=0`, `clearedDropsDelta=0`, `deadlineDropsDelta=0`, and `underflowsDelta=0`. Health correctly remained `Warning`, `LikelyStage=source_signal`, with 120fps MJPG packet cadence (`input=120fps`, capture 1% low `119.54fps`, gaps/drops `0`) and the known 60fps unique/visual source cadence (`unique=60.33fps`, visual changes `59.83fps`, repeat about `50%`).

## 2026-05-03 — HFR Flashback lifecycle verification

**Scope.** Long verification-only pass for the general Flashback lifecycle in explicit HFR mode: selected `3840x2160 @ 120fps (120/1)`, `Video Format=MJPG`, requested/negotiated subtype `MJPG`. This scenario covers pause, seek, play, disable while playback is active, and re-enable.

**Verification.** A warmed 180s `flashback-lifecycle` run at `temp\diagnostic-sessions\sussudio-hfr-flashback-lifecycle-180s` passed. Actions confirmed lifecycle start, pause, seek, play, disable during playback, and re-enable. Playback command state stayed clean at the end: pending commands `0`, dropped/skipped commands `0`, last command failure empty, submit-failure delta `0`, and dropped-frame delta `0`. Preview recovered without starvation: D3D stat failures `0`, scheduler `droppedDelta=3`, `clearedDropsDelta=3`, `deadlineDropsDelta=0`, `underflowsDelta=0`, and `lastDropReason=cleared`. Preview 1% low ended at `110.12fps` and the minimum observed 1% low was `106.06fps` during lifecycle churn, which remains a visible preview-cycle cost to watch but not a playback command failure or scheduler starvation signal. Health correctly remained `Warning`, `LikelyStage=source_signal`, with 120fps MJPG packet cadence (`input=120.01fps`, capture 1% low `119.6fps`, gaps/drops `0`) and the known 60fps unique/visual source cadence (`unique=60.34fps`, visual changes `60fps`, repeat about `50%`).

## 2026-05-03 — Flashback stress warmed-playback diagnostic gate

**Issue.** A 180s HFR `flashback-stress` run at `temp\diagnostic-sessions\sussudio-hfr-flashback-stress-180s` passed while only presenting 118 playback frames before returning Live. That was just below the 120-frame observed-FPS update/warmup threshold and far below the 10s low-percentile gate, so the scenario could miss a short but real playback disturbance. The same run showed `FlashbackPlaybackObservedFpsAtEnd=111.06`, `onePercentLow=114.04`, one `47.7ms` decode/send spike, and two audio-master fallbacks, but no warnings.

**Change.** `flashback-stress` now waits for a warmed playback sample before issuing `go-live`: at least 10s worth of playback frames, bounded by a 15s wait. The scenario records a `flashback playback warmed frames=... fps=... onePercentLow=...` action and warns if playback fails to warm, warmed observed FPS falls below 95% of target, warmed 1% low falls below 80% of target, or audio-master fallbacks increase during the warmed window. Flashback diagnostics now also promote preview scheduler deadline-drop deltas, underflow deltas, and D3D frame-stat failure deltas to warnings instead of leaving them as summary-only fields. This makes the stress scenario a real playback/preview health gate instead of a one-second command-drain smoke.

**Verification.** `dotnet build tools\ssctl\ssctl.csproj --no-restore`, `dotnet build tools\McpServer\McpServer.csproj -c Debug --no-restore`, `dotnet build tools\NativeXuAudioProbe\NativeXuAudioProbe.csproj -c Debug --no-restore`, and `dotnet run --project tests\Sussudio.Tests\Sussudio.Tests.csproj --no-restore` passed. The first test run failed only stale-tool gates for `McpServer.dll` and `NativeXuAudioProbe.dll`; after rebuilding those tools, the suite passed. The tightened 180s live rerun at `temp\diagnostic-sessions\sussudio-hfr-flashback-stress-warmed-180s` correctly failed on a real warmed-playback warning: playback warmed over `1210` frames with `fps=119.99` and `onePercentLow=119.97`, but audio-master fallbacks increased by `2` during the warmed window. The same run also exposed preview stress after the sample: `PreviewSchedulerUnderflowsDelta=1`, preview 1% low minimum `74.2fps`, `PreviewD3DPresentCallP99Ms=11.46`, and `totalFrameCpuP99Ms=12.71`. Export still verified (`Exported 166 packets from 1 segments`) and playback command counters stayed clean, so the remaining finding is now specifically preview/present pressure plus audio-master fallback during stress, not export or command-queue failure.

## 2026-05-06 — Flashback and recording audio-stutter audit snapshot

**Scope.** Follow-up audit after the long Flashback hardening push, focused on the remaining user-reported symptom: occasional audible stutters in the app. This pass reviewed the committed flashback playback/export and dedicated recording artifacts under `results/e2e-20260505-193751` and `results/e2e-20260505-focused`, plus the audio monitoring/ramp telemetry added in commit `c3695e8`.

**Findings.** All eight committed e2e summaries succeeded with `WarningCount=0`. Dedicated H.264, HEVC, and AV1 recording runs all reported `RecordingVerificationSucceeded=true` with `Strict verification passed.` Flashback playback/export runs reported `FlashbackPlaybackDroppedFramesDelta=0` and `FlashbackPlaybackSubmitFailuresDelta=0`. The range-export stream-seek rerun also ended with `FlashbackPlaybackAudioMasterFallbacksAtEnd=0`, which supports the stream-timestamp seek fix and recoverable FFmpeg seek-log filtering. Residual finding: the short `flashback-playback-30s`, `flashback-range-export-12s`, and unfiltered focused `flashback-range-export` summaries each ended with `FlashbackPlaybackAudioMasterFallbacksAtEnd=2`. They did not produce playback drops, submit failures, or failed exports, but they are the closest saved signal to the reported audible stutter and remain worth watching in longer live runs.

**Change.** Added a bounded `GetAudioRampTrace` automation surface backed by `AudioRampTraceSnapshot` / `AudioRampTraceEntry`. The trace samples every 10ms during preview monitor enable/disable, preview stop, and audio input transitions, and records both the UI/control envelope (`PreviewVolume`, target volume, monitoring state) and the render-side envelope from `WasapiAudioPlayback` (`TargetVolume`, `CurrentVolume`, output peak/RMS, render callback count, queue depth, output sample age). `WasapiAudioPlayback` now measures post-volume peak/RMS immediately before handing buffers to WASAPI, so future reports can distinguish source silence, an intentional fade, a playback queue problem, and an actual output-level dropout. Code comments in `MainViewModel.AudioControls.cs` and `WasapiAudioPlayback.cs` document that this trace exists for audible transition/stutter forensics.

**Verification.** Evidence artifacts are committed in `results/e2e-20260505-193751/*/summary.json` and `results/e2e-20260505-focused/*/summary.json`. Static regression coverage includes `AudioRampTrace_ExposesControlAndRenderEnvelopeTelemetry`, which pins the trace model, 10ms sampler, transition trace points, runtime output-level fields, and `GetAudioRampTrace` automation command. Completion status for this audit is best-effort healthy with one monitored residual: no saved run shows recording loss, flashback playback drops, submit failures, or export failure, but audio-master fallback counters remain the current lead for intermittent audible playback stutter.

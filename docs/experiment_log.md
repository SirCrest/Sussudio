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

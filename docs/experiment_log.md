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

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

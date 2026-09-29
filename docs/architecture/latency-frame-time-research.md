# Latency and frame-time research (code-only)

Date: 2026-09-28. Branch: `claude/latency-frame-time-research-d5a206`.
Baseline commit: `e49f05afb`.

No live HDMI source was available. Every number below is derived from reading
the code, from documented Windows/FFmpeg behaviour, or from offline FFmpeg
benchmarks run against the shipped `avcodec-62.dll`. Nothing was measured in
the running app. Each finding is labelled:

- **Verified**: read directly in code by the lead reviewer, with the cited lines.
- **Confirmed**: read directly in code by a stage reviewer.
- **Suspected**: depends on driver, OS, or runtime behaviour that code alone
  cannot prove. A log line or metric that would settle it is named where one exists.

Paths are relative to `Sussudio/` unless stated. Line numbers are at the
baseline commit.

## 1. Where the time goes

### Live preview, native NV12/P010 at fps = Hz (D3D texture path)

| Segment | Bound | Source |
|---|---|---|
| USB, driver, Frame Server, source-reader queue | unknown; not instrumented | `arrivalTick` is taken after `ReadSample` returns (`Services/Capture/MfSourceReaderVideoCapture.cs:1169`), so every in-app latency metric excludes this segment |
| Read loop fan-out | 5-40 µs (D3D path); 0.4-0.9 ms luma scan before preview submit | `Services/Capture/UnifiedVideoCapture.cs:821-834, 1126, 1247-1277` |
| Renderer queue residence | up to 1.5 source intervals (stale-drop threshold), oldest frame rendered first | `Services/Preview/D3D11PreviewRenderer.cs:877-882, 969-1002` |
| Waitable swap-chain wait | fixed 8 ms timeout; up to 15.6 ms with default timer resolution | `D3D11PreviewRenderer.cs:1041` |
| Render CPU | 2-4 ms at 4K (VideoProcessorBlt on cached input view; zero copy for SDR) | `D3D11PreviewRenderer.RenderPasses.cs:185-250` |
| Present to photon | 1-2 refresh intervals plus scan-out (composition swap chain, DWM composes) | `D3D11PreviewRenderer.Resources.cs:885, 997-1012` |

Typical: 1.5-2.5 source frames after the read loop. Worst case: 4.5-5.5 frames.

### Live preview, 4K120 MJPEG (software decode path, default above 60 fps)

Adds roughly 35-43 ms (about 4-5 frames) before the renderer:

| Segment | Estimate | Source |
|---|---|---|
| CPU JPEG decode, one thread | about 16 ms (offline benchmark, 4K yuvj420p) | `Services/Capture/Mjpeg/ParallelMjpegDecodePipeline.cs:1545-1547` |
| Planar to NV12 pack | about 2 ms | `ParallelMjpegDecodePipeline.cs:1454-1489` |
| Worker wake through ThreadPool | 0.05-0.3 ms | `ParallelMjpegDecodePipeline.cs:291, 617-623` |
| Jitter buffer hold | 17-25 ms: about 2 intervals right after priming, settling toward 3 intervals in steady state (target depth 3; ratchets to 8) | `UnifiedVideoCapture.cs:697`, `Services/Capture/MjpegPreviewJitterBuffer.cs:295-306, 339-367, 1180-1212` |

The jitter-buffer residence has two regimes. The emit loop waits until the
queue holds `target` frames and then dequeues the oldest at once
(`MjpegPreviewJitterBuffer.cs:295-306`), so at that moment the frame has
waited about `target - 1` intervals (16.7 ms at 120 fps). The output-interval
trim is evaluated after each dequeue (`:367, 1180-1191`) and compares the
post-dequeue depth with the target, stretching the interval by 0.5 % while the
depth is below target, so with a source at nominal rate the queue drifts to
`target` frames after dequeue and the residence settles near `target` intervals
(25 ms). Either way the buffer is 40-60 % of the pre-renderer budget and the
largest controllable term in the pipeline.

### Audio monitoring (WASAPI shared mode)

About 30-50 ms at defaults, drifting upward over a session. About 10-15 ms is
reachable with the minimum engine period. See section 2, item 2 and section 3.

### Flashback interaction

| Action | Estimate | Dominant cause |
|---|---|---|
| Go live | 40-200 ms plus 1-2 frames | preview resumed last, after decoder teardown and a blocking audio wait |
| Play or resume | 0.15-1.2 s plus 180 ms audio prebuffer | two exact seeks, each with a 2 s decode-forward preroll |
| Pause from live | 0.1-0.8 s, then a 300 ms jump back | decoder open plus seek plus decode-forward, target is 300 ms behind live |

## 2. Findings that add whole frames or break the latency design

These are ordered by expected impact per unit of risk. Items 1-3 and 6 are
small, contained changes.

### 2.1 Recording start forces a full preview renderer reset (Verified)

`SetSharedDevice` (`Services/Preview/D3D11PreviewRenderer.Resources.cs:757-788`)
never compares the incoming device pointer with the one it already holds. It
always clears `_sharedDeviceActive` and, once D3D resources exist, queues a
reset. The render thread then drains the frame queue, unbinds the swap chain
through a UI-thread hop with a 2 s wait, disposes all D3D resources, and runs
`InitializeD3D` including a runtime `D3DCompile` of four shaders
(`D3D11PreviewRenderer.cs:786-805`, `Resources.cs:553-647`).

While the reset is pending, `SubmitTexture` throws
(`D3D11PreviewRenderer.cs:409-412`). The capture side logs each failure without
throttling (`UnifiedVideoCapture.cs:952`) and, in strict native-MJPEG mode,
signals a fatal capture error after five consecutive failures
(`UnifiedVideoCapture.cs:959-969`), which is about 42 ms at 120 fps.

Triggers: every standard (non-Flashback) recording start while preview is
active (`Services/Capture/CaptureService.RecordingLifecycle.cs:484` re-applies
the same capture's device), and every preview re-attach
(`CaptureService.PreviewLifecycle.cs:209, 266, 424`).

Fix: return early when the pointer is unchanged and the device is active.
Only the first apply after renderer creation needs the reset.

### 2.2 The IAudioClient3 low-latency path cannot work as declared (Verified)

`Services/Audio/WasapiComInterop.cs:267` declares
`[ComImport] interface IAudioClient3 : IAudioClient` and redeclares the twelve
`IAudioClient` methods with `new`. That redeclaration is required: built-in
`[ComImport]` interop does not inherit vtable slots from a managed base
interface (only source-generated COM does), so a derived interface's vtable is
IUnknown plus the methods it declares itself. What the declaration omits is the
three `IAudioClient2` methods that sit between `IAudioClient` and
`IAudioClient3` in the native vtable (`IsOffloadCapable`, `SetClientProperties`,
`GetBufferSizeLimits`; no reference exists anywhere in the repo). The three
`IAudioClient3` methods are therefore bound three slots early, to
`IAudioClient2`'s positions, with incompatible native signatures. (An earlier
revision of this report and of the branch's first fix described the mechanism
as double-counting and removed the redeclarations, which bound the methods to
`IAudioClient`'s slots 6-8 instead; a Codex CLI review caught that, and the
branch now keeps the twelve redeclarations and adds the three missing slots.)

`TryInitializeSharedStreamWithAudioClient3` (`WasapiComInterop.cs:784-808`)
returns `false` on any failing HRESULT with no log line and the callers fall
back to the legacy `Initialize` with a zero buffer duration
(`WasapiAudioCapture.cs:153-163`, `WasapiAudioPlayback.cs:218-250`). No test
references `IAudioClient3` and no log line records which path ran.

Even when the layout is corrected, the call passes `defaultPeriodInFrames` and
discards the minimum period (`WasapiComInterop.cs:795`), so it would still run
at the default engine period (about 10 ms) instead of the minimum (about 2.7 ms
on typical HD Audio, device dependent).

Fix: keep the twelve `new` redeclarations and insert the three `IAudioClient2`
methods between them and the `IAudioClient3` methods, so the declaration lists
all eighteen slots in native order (12 + 3 + 3). Request the minimum period,
rounded to a multiple of the fundamental period, and fall back to default on
failure. Log the HRESULT and the four periods at initialisation. Estimated gain
per side: 3.7 ms average, 7 ms worst. No test exercises native dispatch through
`IAudioClient3`, so a green suite cannot detect a wrong layout; the
`WASAPI_CLIENT3_INIT` log line on a live run is the check.

### 2.3 Encode threads run at normal priority without MMCSS while overflow is fatal (Verified)

Both encode loops start as `Task.Factory.StartNew(..., LongRunning)` with no
priority or MMCSS registration (`Services/Recording/LibAvRecordingSink.cs:211-215`,
`Services/Flashback/FlashbackEncoderSink.cs:251-255`). Every other hot thread
registers through `MmcssThreadRegistration`
(`Services/Runtime/RuntimeHelpers.cs:785-868`). The MJPEG emitter thread and
both WASAPI worker threads are also unregistered
(`ParallelMjpegDecodePipeline.cs:709-717`, `WasapiAudioCapture.cs:289-294`,
`WasapiAudioPlayback.cs:390-395`).

The recording GPU queue holds four frames (`LibAvRecordingSink.cs:24`) and
overflow calls `FailEncoding`, which stops the recording
(`LibAvRecordingSink.cs:1212-1241, 1546-1579`). That is 33 ms of tolerated
stall at 120 fps for a thread that also performs NVENC submission, AAC encode
and inline file writes through a 32 KB avio buffer.

Fix: register the encode loops, the MJPEG emitter and the WASAPI workers with
MMCSS ("Playback" for video, "Pro Audio" for audio) and set `AboveNormal`.
Then address the queue depth through item 2.4.

### 2.4 GPU textures are consumed after the Media Foundation sample is released (Confirmed; corruption Suspected)

On the D3D path the read loop hands consumers an `AddRef`'d `ID3D11Texture2D`
plus subresource index, then releases the `IMFSample` at the end of the
iteration (`MfSourceReaderVideoCapture.cs:1198-1201`). The recording sink,
Flashback sink and preview renderer copy or blit from that texture 1-8 frames
later on their own threads (`LibAvRecordingSink.cs:1398`,
`FlashbackEncoderSink.cs:1261`, `D3D11PreviewRenderer.cs:418`;
`Services/Recording/LibAvEncoder.VideoFrames.cs:336-344`). A reference on the
array texture does not stop the source reader's allocator from recycling that
slice. No `ID3D11Fence`, query or keyed mutex exists in the repository.

Fix: perform the `CopySubresourceRegion` into an app-owned texture ring on the
read-loop thread (a GPU command enqueue costing tens of microseconds of CPU)
and queue the ring index. This removes the hazard and removes a thread hop
before the copy. The eight-texture pool already exists
(`LibAvEncoder.VideoFrames.cs:106`); it needs to grow to 16-24 slots (power of
two) at 12-25 MB each.

The copy alone does not change the overflow policy: `TryEnqueueGpuPacket`
still calls `FailEncoding` when its bounded channel is full
(`LibAvRecordingSink.cs:1212-1241`). Making overflow survivable is a separate,
explicit policy change with its own timing contract: on a full queue drop the
newest frame, count it as a queue drop in the recording-integrity counters,
record the sequence gap on the GPU lane (today only the CPU lane feeds the gap
tracker, `LibAvRecordingSink.cs:1818`), and advance the encoder's video PTS by
the dropped frame so audio and video stay aligned (`SkipVideoFrame`,
`LibAvEncoder.cs:91`, exists for this and has no callers). Without the PTS
step a drop shortens the video timeline by one frame time, so the policy
change and the PTS handling must ship together.

Evidence: record a frame-counter test signal with preview and Flashback active
and check the output for repeated or out-of-order counters.

### 2.5 MJPEG preview jitter buffer holds three frames and cannot recover from a lost packet (Confirmed)

- Default target depth 3 with a floor of 2 that is reached only after 15 s of
  stability; each underflow raises the target by one, up to 8, and un-primes
  the buffer so it waits for `target` frames again
  (`UnifiedVideoCapture.cs:689-699`, `MjpegPreviewJitterBuffer.cs:146-148,
  295-306, 355-359, 1193-1245`).
- Known-missing sequences recorded by the decode pipeline
  (`ParallelMjpegDecodePipeline.cs:1007`) never reach the jitter buffer, which
  waits for an exact next sequence. Both deadline drops fire before the
  skip-recovery branch (`MjpegPreviewJitterBuffer.cs:624-650, 1099, 1134`), so
  one lost packet freezes preview for about five intervals and also drops the
  next good frame.
- The output clock is nominal fps with a ±0.5-1.5 % trim
  (`MjpegPreviewJitterBuffer.cs:228, 1180-1191`). Measured input intervals are
  recorded (`:807`) but only feed metrics. A local live run from 2026-09-05
  (the gitignored `artifacts/live-e2e-20260905-232055/REPORT.md` in the main
  checkout; it is not in the repository, so this figure cannot be verified from
  the tree) recorded 66-95 incoming fps against a 120 target. If a source
  delivers below nominal like that, this design underflows chronically and
  ratchets the target to the maximum. Reproduce with a diagnostic session that
  records `MjpegPreviewJitterUnderflowCount` and `MjpegPreviewJitterTargetDepth`
  over a minute of preview, and keep the JSON under `artifacts/`.
- Every `ResumePreviewSubmission` and preview re-attach forces a full re-prime.
  `ResumePreviewSubmission` (`UnifiedVideoCapture.cs:1040`) has no
  already-resumed early-out.

Fix, in order: A/B today with `SUSSUDIO_PREVIEW_JITTER_TARGET_DEPTH=1`,
`SUSSUDIO_PREVIEW_JITTER_MIN_TARGET_DEPTH=1`,
`SUSSUDIO_PREVIEW_JITTER_MAX_TARGET_DEPTH=1`; then change the default target to
2 with floor 1; feed known-missing sequences to the buffer; derive the emit
interval from an EMA of measured input intervals; stop un-priming on underflow;
decay in 3-5 s.

### 2.6 GPU-to-CPU readback runs on the read loop when it is not needed (Confirmed)

`SkipCpuReadback` has one setter, called after `Start()`
(`CaptureService.PreviewLifecycle.cs:268-277`). A recording started without a
live preview builds its own capture (`CaptureService.RecordingLifecycle.cs:451-471`)
and never sets it. Every frame then runs `Lock2D` on the DXGI buffer, which is
an MF staging copy plus a synchronous `Map` (`MfSourceReaderVideoCapture.cs:1593-1606, 1825`),
followed by both luma trackers because the span is non-empty. Estimated 2-6 ms
per 4K frame on the capture thread, and the code comment at
`PreviewLifecycle.cs:270-273` attributes about 5 % cadence drops at 120 fps to
this readback.

Fix: set the flag inside `UnifiedVideoCapture` when D3D output is negotiated,
before `Start()`, and clear it only when a CPU-mode sink needs bytes.

### 2.7 The live audio monitor has no fill target, so latency only ratchets up (Confirmed)

On underrun the render side fills the remaining endpoint space with silence
(`WasapiAudioPlayback.cs:965-968, 1010-1015`). The queued backlog then plays at
real-time rate, so every stall adds its duration to monitoring latency
permanently. Nothing trims the queue until it holds 128 chunks (about 1.28 s at
10 ms packets), at which point the oldest chunk is dropped
(`WasapiAudioPlayback.cs:42, 614-633`). Capture and render clocks are
independent, so at 100 ppm relative error the queue grows about 6 ms per
minute or underruns about every 100 s.

Fix: a live-mode controller checked each render callback with a target of
about one capture period plus one render period; trim whole chunks with a
short crossfade or slew ±0.1 % through the existing resample-phase machinery
(`WasapiAudioPlayback.cs:1051-1122`); make queue depth a constructor policy
(live 8-16 chunks, file playback deep); silence-fill only the missing part.

### 2.8 Preview is the last consumer, behind a full-frame luma scan (Confirmed)

Fan-out order is recording, then Flashback, then preview in every path
(`UnifiedVideoCapture.cs:779-785, 821-834, 880-979`). Before the preview
submit, `TrackPreviewVisualFrame` runs two `VisualCadenceTracker.RecordFrame`
calls that scan 640x360 plus 320x180 luma samples in scalar, bounds-checked
code while holding the tracker lock
(`UnifiedVideoCapture.cs:1126, 1201, 1247-1277`;
`Services/Capture/CaptureCadenceTrackers.cs:158-213, 255-312`). On the MJPEG
strict emitter the same scan precedes the recording and Flashback enqueue
(`UnifiedVideoCapture.cs:853-862`). The only consumers are the stats
"visual fps" row and automation health fields. Estimated 0.4-0.9 ms per frame,
every frame, unconditionally.

Fix: submit preview first; run the scan only while a stats or automation
consumer is active; vectorise with row-wise `SequenceEqual`; or hand a lease to
a one-slot drop-if-busy worker.

### 2.9 CPU samples on the D3D-manager path take a three-copy route (Confirmed; reachability device dependent)

`Start()` selects the dual callback whenever D3D output is enabled
(`UnifiedVideoCapture.cs:427-435`). If a sample arrives without a DXGI buffer
(logged as `MF_SOURCE_READER_D3D_BUFFER_MISS`, `MfSourceReaderVideoCapture.cs:1882`),
`OnDualFrameArrived` runs with a null texture and copies the frame inline three
times: recording sink, Flashback sink and preview `Marshal.Copy`
(`UnifiedVideoCapture.cs:887, 896, 977`; `D3D11PreviewRenderer.cs:327-330`).
The single pooled copy in `FanOutPooledCpuFrame` (`UnifiedVideoCapture.cs:793`)
is only reachable from the non-D3D callback. Cost: about 3.6 ms versus 1.2 ms
per 4K NV12 frame on the read loop.

Fix: route `gpuTexture == 0 && !frameData.IsEmpty` to `FanOutPooledCpuFrame`.
Check the session log for the buffer-miss line to learn whether this branch is
live on the target device.

## 3. Per-frame hot-path costs

Each of these is small on its own but sits on the critical path.

| # | Finding | Location | Estimate | Status |
|---|---|---|---|---|
| 3.1 | Waitable swap-chain timeout is a fixed 8 ms rather than derived from the measured refresh interval (`_measuredRefreshIntervalTicks` exists at `:2057`); at 60 Hz the wait expires before a slot frees, the frame is chosen early and `Present` blocks under the shared device lock. Default timer resolution can stretch it to 15.6 ms since `timeBeginPeriod(1)` is raised only by the jitter buffer at 100 fps and above | `D3D11PreviewRenderer.cs:1041`; `MjpegPreviewJitterBuffer.cs:259` | up to 8 ms staleness | Confirmed constant; impact Suspected. Check `FrameLatencyWaitMetrics.TimeoutCount` |
| 3.2 | Renderer dequeues oldest-first with a hard-coded 1.5-interval stale threshold, so at fps = Hz a standing one-frame offset can persist | `D3D11PreviewRenderer.cs:877-882, 1001` | 1-1.5 frames | Confirmed; add latest-wins when depth ≥ 2 |
| 3.3 | CPU frames (MJPEG path) upload through `UpdateSubresource` of 12-25 MB on the render thread while holding the shared immediate-context lock; fallback path does per-row copies into a staging texture | `RenderPasses.cs:347, 370-421` | 1-3 ms, serialises encoder copies | Confirmed. `RenderCpuTimingMetrics.InputUpload` measures it |
| 3.4 | HDR texture path does two `CopySubresourceRegion` calls plus a shader pass per frame; `Description` is queried per frame | `RenderPasses.cs:604-616` | 2 GPU copies | Confirmed. VideoProcessor passthrough would be zero-copy; `CheckVideoProcessorFormatConversion` is never called |
| 3.5 | `SemaphoreSlim(0, 1).Release()` throws `SemaphoreFullException` on every redundant signal from WASAPI and capture threads; the catch-and-count shows the author saw it | `LibAvRecordingSink.cs:43, 1528-1544` | 10-50 µs per throw on hot threads | Verified. Use an atomic gate: producers `Interlocked.Exchange` a pending flag and only `Release` on the 0-to-1 transition; the consumer clears the flag after its wait returns and before draining. A `CurrentCount` check is not enough, because two producers can both read zero and one still throws. `AutoResetEvent` (idempotent `Set`) is the alternative. Implemented on this branch as the interlocked gate |
| 3.6 | `GetDeviceRemovedReason` per encoded frame under the device lock | `LibAvEncoder.VideoFrames.cs:349` | small | Confirmed. Check every N frames or after a send failure |
| 3.7 | Two stats getters sort 2400-sample windows while holding locks the render thread takes every frame; `RingBufferHelpers.Copy` does a modulo per element | `D3D11PreviewRenderer.cs:1765-1783, 1814-1824, 2228, 2293`; `RuntimeHelpers.cs:212-230` | 100-200 µs stall per stats poll | Confirmed. Copy under lock, sort outside; use two `Array.Copy` segments; rings are single-writer so a seqlock removes the lock |
| 3.8 | MJPEG decode workers wake through `WaitToReadAsync().GetAwaiter().GetResult()` on a channel without `AllowSynchronousContinuations`, so each wake goes through the ThreadPool | `ParallelMjpegDecodePipeline.cs:291, 617-623` | 50-300 µs per wake | Suspected. Use a direct MPMC queue with Monitor or `SemaphoreSlim` |
| 3.9 | `ResizeBuffers` disposes the video processor and both input texture rings (about 75 MB at 4K) although input size is unchanged | `Resources.cs:898-935, 1477-1493` | hitch per resize | Confirmed. Keep textures; or `IDXGISwapChain2::SetSourceSize` |
| 3.10 | Shader bytecode compiled with `D3DCompile` at every `InitializeD3D` and every device reset | `Resources.cs:553-647` | first-frame latency | Confirmed. Precompile |
| 3.11 | Lost-wake race: `ResetFrameReady` after render has no re-check, unlike the idle path | `D3D11PreviewRenderer.cs:844-855, 932-937` | up to one interval, rare | Confirmed |
| 3.12 | MF read loop is a permanent `Task.Run` pool thread that sets its own priority and never restores it; `ReadSample` is synchronous and `StopAsync` awaits it with no timeout | `MfSourceReaderVideoCapture.cs:1023, 1051, 1096-1105` | shutdown hang risk | Confirmed. Dedicated thread; consider async callback with an MMCSS work queue |
| 3.13 | Stride-strip copy runs before the pooled copy when pitch differs from width | `MfSourceReaderVideoCapture.cs:1738-1742, 1795` | one extra full copy | Confirmed. Pass pitch through |
| 3.14 | Frame buffers come from `ArrayPool<byte>.Shared` at 12-25 MB, rounded to 16/32 MiB LOH buckets; CPU queues are bounded by count (360 recording, 180/128 Flashback), not bytes | `Services/Contracts/ServiceContracts.cs:454, 511`; `LibAvRecordingSink.cs:22`; `FlashbackEncoderSink.cs:24-25` | cold-pool page faults about 4 ms; GBs of LOH under backlog | Suspected. Fixed pinned pool sized to queue depth; byte budget like the MJPEG pipeline's 64 MB |
| 3.15 | `AudioPeak` raises `PropertyChanged` on the WASAPI capture thread about 15 times per second and is routed synchronously through a nine-way string router; nothing binds it | `ViewModels/MainViewModel.AudioState.cs:571`; `MainWindow.xaml.cs:230-238, 2243-2296` | UI routing on the audio thread | Confirmed. Make it a `Volatile` field like `AudioMeterTarget` |
| 3.16 | Flashback audio enqueue shares `_videoQueueSync` with the video producer and logs inside the lock on eviction | `FlashbackEncoderSink.cs:1391, 1432, 1461, 1485` | audio thread blocked by a preempted video holder | Confirmed |
| 3.17 | HDR render pass builds an interpolated message string every frame that only the first frame uses | `RenderPasses.cs:678` | about 20 KB/s garbage at 120 fps | Confirmed |
| 3.18 | Jitter buffer polls every 2 ms while preview is suppressed (Flashback playback) | `MjpegPreviewJitterBuffer.cs:295-300` | about 500 wakeups per second | Confirmed |

## 4. Background overhead that competes with the hot paths

All of these run whether or not anything consumes the result.

| Poller | Cadence | What it costs | Location |
|---|---|---|---|
| Automation diagnostics hub | 500 ms, always, from pipe-server start; nothing subscribes to `SnapshotUpdated` | two UI-thread hops; seven renderer stats getters (about seven sorts of 2400 doubles) on the UI thread; a full health snapshot with 25-30 sorts on a pool thread; an 807-field snapshot and a 159-field timeline entry; about 0.4-0.6 MB allocated per poll | `Services/Automation/AutomationDiagnosticsHub.cs:57, 214-242`; `.Snapshots.cs:134-292`; `Controllers/Window/WindowControllers.cs:68-70` |
| Process resource sampling | with the hub | `Process.Refresh()` plus `WorkingSet64`/`PrivateMemorySize64` go through a system-wide process enumeration | `AutomationDiagnosticsHub.Snapshots.cs:887-916` |
| NativeXu source telemetry | 500 ms for the whole preview session | `SetupDiGetClassDevs`, open KS interface, topology IOCTL, then 6-11 USB control transfers to the device that is streaming video, plus a log line per poll | `CaptureService.RuntimeSnapshots.cs:1819, 1887-1919`; `Services/Telemetry/NativeXuAtCommandProvider.cs:96-263, 312-390`; `Services/NativeXu/KsExtensionUnitNative.cs:66` |
| NVML | 500 ms from app start; only the stats dock GPU rows consume it | ten driver calls including two `nvmlDeviceGetPcieThroughput` calls, each documented as a roughly 20 ms measurement window | `Services/Gpu/NvmlMonitor.cs:81, 105-149`; `MainWindow.xaml.cs:60, 1599` |
| Stats UI sampler (visible only) | 250 ms, health every 500 ms | full health snapshot on the UI thread; MJPEG pipeline timing computed twice | `ViewModels/StatsUiSampler.cs:21-22, 118-121`; `Controllers/.../StatsOverlayCompositionController.cs:1190` |
| MainViewModel timer | 1 s, always | `GetRuntimeSnapshot` including a 64-entry ledger summary even when idle; `new DriveInfo(...).AvailableFreeSpace` on the UI thread | `Controllers/ViewModel/MainViewModelLifecycleController.cs:208-242`; `ViewModels/ViewModelBuilders.cs:17-18` |

Whether the NativeXu control transfers and NVML calls actually disturb
streaming or NVENC is Suspected, not confirmed; it needs a live A/B with the
polls disabled.

Fix: demand-gate the hub (start on first pipe connection or command, keep alive
30 s), the NVML timer (GPU section visible, 1-2 s, drop PCIe throughput) and the
NativeXu poll (cache the interface handle, 2-5 s, only while telemetry UI is
visible). Replace `Process.Refresh` with `GetProcessMemoryInfo`. Let the stats
sampler reuse the hub's latest snapshot.

## 5. Flashback interaction latency

| # | Finding | Location | Status |
|---|---|---|---|
| 5.1 | Play or resume performs two exact seeks: the play handler seeks, the audio prebuffer releases every decoded D3D11 frame and then rewinds with a second `SeekTo`. Each exact seek is a binary seek plus decode-forward from 2 s before the target (up to 960 frames) | `FlashbackPlaybackController.ThreadCommands.cs:485, 664, 831`; `FlashbackPlaybackController.cs:1892, 1967-1985, 2016-2028`; `FlashbackDecoder.cs:864, 1029-1036` | Confirmed. Keep the primed frames (read-ahead already retains 12) |
| 5.2 | The 2 s preroll exists because a TS binary seek can land after the target. GOP is 1 s with forced IDR | `FlashbackDecoder.cs:1029-1036`; `FlashbackEncoderSink.cs:312`; `LibAvEncoder.cs:672-677` | Confirmed. Record `(pts, avio_tell)` per IDR write and `avio_seek` directly |
| 5.3 | GoLive restores preview submission last, after decoder teardown and a blocking audio wait of up to 100 ms | `FlashbackPlaybackController.ThreadCommands.cs:322-334`; `FlashbackPlaybackController.cs:1338, 1745, 1776, 1815` | Confirmed. Resume preview first, defer cleanup |
| 5.4 | Live edge sits about 300 ms behind. `LatestPts` is published before the mux write; `av_interleaved_write_frame` holds video until a later-DTS audio packet; mpegts never sets `flush_packets` (fragmented MP4 does) so the 32 KB avio buffer can hold many frames at low bitrate | `FlashbackEncoderSink.cs:2216-2219`; `LibAvEncoder.cs:515, 835, 950-956, 971` | Verified for `flush_packets`; rest Confirmed. Then lower the 300 ms lead |
| 5.5 | `OpenFile` probes with 20 MB / 5 s and applies `skip_frame=all` only when dimensions are already known, so a fresh open can software-decode 4K HEVC/AV1; the decoder and hardware context are disposed on every GoLive | `FlashbackDecoder.cs:407-408, 487-496, 532-537`; `FlashbackPlaybackController.cs:1338-1386` | Confirmed. Reuse the fast-probe settings; keep the decoder warm |
| 5.6 | Segment rotation runs inline on the encode thread: trailer, close, open, header, plus `File.Exists` under the index lock | `LibAvEncoder.cs:705-786`; `FlashbackBufferManager.cs:1072` | Confirmed (5-30 ms estimate). Pre-open the next avio context off-thread |
| 5.7 | Pause-from-live decodes from disk to a target 300 ms behind live; a deferred-display path already exists | `FlashbackPlaybackController.ThreadCommands.cs:25-26, 538-569` | Confirmed. Behaviour change; needs product sign-off |
| 5.8 | Decoder contexts lack `AV_CODEC_FLAG_LOW_DELAY`; a 2 ms `SpinWait` tail per paced frame | `FlashbackDecoder.cs:1517-1529, 1739-1762`; `FlashbackPlaybackController.cs:2375-2392` | Confirmed |
| 5.9 | The audio master clock is stamped when data is copied into the endpoint buffer, not when audible; no `IAudioClock` use | `WasapiAudioPlayback.cs:973-975, 992`; `FlashbackPlaybackController.cs:2213-2250` | Confirmed. Video paced 10-40 ms early |

## 6. A/V timing correctness

Video PTS is a frame counter (`LibAvEncoder.VideoFrames.cs:370, 438, 506`) and
audio PTS is a sample counter (`LibAvEncoder.Audio.cs:326-337`). Neither uses a
capture timestamp. `SkipVideoFrame` (`LibAvEncoder.cs:91`) has no callers, so
every dropped or rejected video frame permanently shifts video earlier by one
frame time. The WASAPI `qpcPosition` returned by `GetBuffer` is discarded
(`WasapiAudioCapture.cs:671-678`), `DATA_DISCONTINUITY` is only counted
(`:551-554`), and drift correction is disabled for program audio
(`LibAvEncoder.Audio.cs:442-447`). The sequence-gap tracker is never fed on the
GPU lane (`LibAvRecordingSink.cs:1818`). The in-app drift telemetry subtracts
its first sample as a baseline and divides by nominal fps
(`CaptureService.RuntimeSnapshots.cs:1434-1498`), so it cannot report absolute
offset and shows NTSC rate mismatch as drift.

Fix: stamp audio packets with `qpcPosition` and video with the MF sample time,
derive PTS from those stamps or insert exactly the missing samples or frames at
gap boundaries, feed the gap tracker on the GPU lane, and add an
`ingress_lag_ms` metric (now minus sample time) so backlog inside the source
reader becomes visible. Two policies have to be defined before that is safe:

- **Invalid audio stamps.** When `GetBuffer` returns
  `AUDCLNT_BUFFERFLAGS_TIMESTAMP_ERROR` the device and QPC positions are
  unreliable; the capture worker already observes the flag
  (`WasapiAudioCapture.cs:551-554`). For such packets keep the current
  sample-count PTS (previous PTS plus frames delivered) and re-anchor from the
  next packet that carries a valid stamp, treating any jump larger than one
  period as a gap to fill rather than a re-timing of delivered audio.
- **Clock domains.** An MF sample time is a stream presentation time, not
  necessarily on the WASAPI QPC epoch. Correlate once per session: record
  `(sampleTime, QPC at ReadSample return)` for the first N samples, take the
  minimum offset as the fixed mapping, and treat later offset growth as ingress
  lag. If the offset is not stable (a source whose sample times are not
  QPC-derived), fall back to arrival-QPC-based video stamps and log which
  mapping is in use, so the encoder never mixes epochs.

## 7. Where existing libraries or OS facilities should do the work

### Adopt

| Area | Today | Use instead |
|---|---|---|
| Audio engine period | default period through a broken `IAudioClient3` declaration | corrected `IAudioClient3` with the minimum shared-mode period |
| Audio capture conversion | custom per-sample switch plus a stateless linear resampler with no anti-alias filter (`WasapiAudioCapture.cs:816-893`) | `AUDCLNT_STREAMFLAGS_AUTOCONVERTPCM | SRC_DEFAULT_QUALITY` on capture, as render already does, so the fast path is always taken |
| Audio position | sample counting and copy-time stamps | `IAudioClock::GetPosition` plus `qpcPosition` from `GetBuffer` |
| Thread scheduling | MMCSS on five threads, none on encode, emitter or audio | existing `MmcssThreadRegistration` on all of them; `PROCESS_POWER_THROTTLING` opt-out for EcoQoS and timer-resolution throttling; process-wide `timeBeginPeriod(1)` or high-resolution waitable timers on the 60 fps path |
| HDR preview pass | two GPU copies plus a custom shader | `ID3D11VideoProcessor` with `YcbcrStudioG2084LeftP2020` input and `RgbFullG2084NoneP2020` output on the existing 10-bit swap chain; probe with `CheckVideoProcessorFormatConversion` first |
| Preview resize | `ResizeBuffers` plus full input-ring rebuild | `IDXGISwapChain2::SetSourceSize` |
| Preview scan-out | composition swap chain through a VideoProcessor blit | probe `IDXGIOutput3::CheckOverlaySupport` for NV12/P010; a hardware overlay would skip the blit and the BGRA back-buffer write (hardware dependent) |
| Source-reader timing | arrival tick after `ReadSample` | `IMFSample::GetSampleTime`, `MFSampleExtension_DeviceTimestamp`, `MF_LOW_LATENCY`; log the converter chain with `IMFSourceReaderEx::GetTransformForStream` in converted-MJPEG mode |
| Muxer I/O | `avio_open2` with the 32 KB default (about seven `write` calls per 200 KB packet, inline on the encode thread) | custom `AVIOContext` with a 1-4 MiB buffer and a bounded writer thread; `flush_packets=1` for mpegts |
| Flashback decode | default decoder flags | `AV_CODEC_FLAG_LOW_DELAY` (stream has no B-frames); keyframe byte index instead of TS binary seek |
| FFmpeg logging | `av_log_set_level(AV_LOG_VERBOSE)` with a callback that marshals every format string before discarding levels above ERROR (`Services/Runtime/FfmpegRuntimeLocator.cs:296-300, 432`) | `AV_LOG_ERROR` |
| MJPEG decode output | decode into an FFmpeg frame, then copy Y and interleave UV into the pooled NV12 buffer | custom `get_buffer2` that decodes Y straight into the destination; SIMD UV interleave; refcounted `AVBuffer` around the pooled JPEG buffer to avoid the hidden `av_packet_ref` copy |
| Frame buffers | `ArrayPool<byte>.Shared` | fixed pinned pool sized to the queue depths, with a byte budget |
| Statistics | clone-and-sort under locks, modulo-per-element ring copies | `MemoryExtensions.Sort` on a pooled span outside the lock, or quickselect; two `Array.Copy` segments; seqlock rings |
| Signalling | `SemaphoreSlim(0, 1)` with exception-based dedupe; sync-over-async channel waits | `AutoResetEvent`; direct Monitor pulse |
| Process memory | `Process.Refresh()` | `GetProcessMemoryInfo` or `Environment.WorkingSet` |
| GC | defaults | keep concurrent workstation GC; `GCSettings.LatencyMode = SustainedLowLatency` while capture is active; `System.GC.RetainVM` |

### Do not switch (evaluated and rejected)

| Candidate | Why not |
|---|---|
| NVDEC MJPEG (`mjpeg_cuvid`, `-hwaccel cuda`) | Not pursued now, not rejected on latency grounds. The offline figures are throughput through the FFmpeg CLI, which pipelines: 86 fps single session vs 62 fps CPU single thread, four sessions 272 fps vs six CPU processes 310 fps. They show no capacity advantage, but they say nothing about submission-to-completion latency, which is unmeasured (if the single session were strictly serial the inverse would be about 11.6 ms vs 16.1 ms per frame, but that is not established). Revisit only with a per-frame latency measurement, and weigh it against the CUDA or D3D11 interop every consumer would then need |
| libjpeg-turbo | Parity at best on current evidence, not slower. The offline comparison measured full-colour output (27 ms) which includes a colour conversion the app would not perform; the grey-only figure (16 ms) matches FFmpeg's decoder, which is consistent with both being entropy-bound. TurboJPEG's `tjDecompressToYUV` writes planes directly, so a fair test is that path plus the same NV12 packing. Measure it before adding a native dependency; do not expect a large win |
| FFmpeg frame or slice threading for MJPEG | the decoder reports no threading capability; app-level workers are the right structure |
| `delay=0` on the standard recording path | makes `avcodec_send_frame` synchronous per frame and serialises the NVENC pipeline; risks falling behind real time at 4K120 with p6/p7. The default delay only affects packet emission, not file timing |
| Zero-copy MF texture into NVENC | saves about 0.1 ms of GPU time; needs `MF_SA_D3D11_BINDFLAGS` render-target binding and pinned samples |
| Separate D3D11 device for the encoder | one copy must still happen on the shared context; a deferred context still takes the immediate-context lock on `ExecuteCommandList`. Only revisit if copy-duration histograms show lock waits |
| Exclusive-mode WASAPI | locks out vendor software and other apps and takes over the user's output |
| Restart-marker intra-frame JPEG parallelism | only worthwhile if the device emits DRI markers; log `0xFFDD` presence first |

## 8. Measurement blind spots to close before tuning

1. `arrivalTick` is taken after `ReadSample` returns, so USB, driver, Frame
   Server and source-reader queueing are invisible. Add `ingress_lag_ms` from
   the MF sample time.
2. `EstimateVisibleTick` assumes display at the next vblank after `Present` and
   under-reports by up to one refresh interval. Use PresentMon `MsUntilDisplayed`.
3. No log line records the WASAPI engine period, buffer size, stream latency,
   or which initialisation path ran.
4. GPU-lane queue dwell in the recording sink is not tracked; only the CPU lane
   feeds the latency tracker.
5. The audio glitch detector's threshold collapses to 100 ms because
   `expectedIntervalMs` is a rate ratio, and callback timing uses
   `Environment.TickCount64` at 15.6 ms resolution
   (`WasapiAudioCapture.cs:497-500, 613`).
6. Local and automation runs are Debug builds per the project file comments;
   `Debug.WriteLine` in the logger is a syscall per log line there. Compare
   latency numbers on Release builds only.
7. Documentation mismatches: `AGENT_MAP.md:139-140` says
   `SUSSUDIO_PREVIEW_DXGI_FRAME_STATS_SAMPLE_INTERVAL` is milliseconds but the
   code counts frames; `scripts/performance/Measure-PreviewScenario.ps1:17`
   passes `-ExpectedWaitable 0` while the default is 1.

## 9. Suggested order of work

1. No code: A/B the jitter knobs at 1/1/1, `SUSSUDIO_PREVIEW_PRESENT_SYNC_INTERVAL=0`,
   `SUSSUDIO_PREVIEW_DISPLAY_CLOCK_PACING=1`, and
   `SUSSUDIO_PREVIEW_COMPOSITION_MODE_PROBE=1` with the stats overlay hidden.
   Read `FrameLatencyWaitMetrics`, `RenderCpuTimingMetrics`, the input-view
   cache log, `MF_SOURCE_READER_D3D_BUFFER_MISS`, and `MJPEG_DECODE_PATH`.
2. Contained fixes with no behaviour change: 2.1 device-pointer check, 2.2
   interop declaration plus minimum period plus logging, 2.3 MMCSS, 2.6
   readback flag, 3.5 signalling, 3.6, 3.7, 3.15, 3.17, section 4 demand gating.
3. Structural: 2.4 copy-at-capture texture ring (also fixes fatal overflow),
   2.8 fan-out order and scan gating, 2.9 pooled fallback, 2.5 jitter defaults
   and missing-sequence feed, 2.7 monitor fill target, section 6 timestamps.
4. Flashback: 5.3, 5.4, 5.1, 5.2, 5.5, 5.6.
5. Only with measurements: 3.1 timeout policy, 3.2 latest-wins, HDR
   VideoProcessor pass, overlay swap chain, mailbox present.

Every change to capture, recording, HDR, Flashback or audio must keep the
existing contracts (no silent codec or preset downgrade; source-reader fan-out
stays non-blocking) and needs a live run for the numbers above to become
evidence.

## 10. Implemented on this branch (2026-09-29)

The contained, contract-preserving items were implemented on this branch and
pass `scripts\validate.ps1` (build with 0 errors, 2,321 xUnit tests with 0
failures, assembly-load smoke, `git diff --check`). None of them changes pacing
defaults, codec or preset selection, automation wire behaviour, or the
non-blocking source-reader fan-out. What the automated run does not prove is
listed after the table; every item still needs a live run with a device.

| Item | Change |
|---|---|
| 2.1 | `SetSharedDevice` returns early when the pointer is unchanged and the device is active, so recording start and preview re-attach no longer rebuild the swap chain |
| 2.2 | `IAudioClient3` keeps the twelve redeclared `IAudioClient` methods (built-in interop does not inherit slots) and now carries the three `IAudioClient2` slots ahead of its own three; initialisation requests the minimum shared-mode period, retries with the default period, then falls back to legacy `Initialize`; `WASAPI_CLIENT3_INIT` logs the HRESULT, period mode and all four periods |
| 2.3 | Both encode loops run `AboveNormal` under MMCSS "Playback"; the MJPEG emitter runs `AboveNormal` under the decode class; both WASAPI workers register as "Pro Audio" |
| 2.6 | Preview sets the readback skip before `Start()`; recording start sets it to `gpuEncoder != null` whenever the D3D manager is present (covers recording-only sessions and software recordings on the D3D path); recording stop restores the preview-only policy. When start reuses the preview capture it records the prior value in the rollback state and a failed start first detaches the recording consumer it attached (previously left attached to a disposed sink, a pre-existing gap) and then restores the value, so a surviving preview never inherits a software recording's readback or a dead recording route |
| 2.8 | Preview is the first consumer in all four fan-out paths: the pooled CPU path, the non-pooled CPU fallback, the MJPEG strict emitter, and the DXGI texture path in `OnDualFrameArrived` (its recording and Flashback enqueues now follow the texture submit). The visual-cadence luma scan runs last in every path, after both the preview submit and the sink enqueues, gated as before on an active preview sink |
| 2.9 | A CPU-backed sample on the D3D-manager path takes the single pooled copy unless strict texture delivery is required, in which case the strict failure accounting still runs |
| 3.5 | Recording-sink work signal uses an interlocked pending gate; redundant signals no longer throw |
| 3.7 | Pipeline-latency and frame-latency-wait metrics copy under the lock and sort outside it; ring copies use two `Array.Copy` segments |
| 3.11 | The render loop re-checks the queue after resetting the frame-ready event on the post-render path |
| 3.15 | `AudioPeak` is a volatile property; it no longer raises `PropertyChanged` from the WASAPI thread |
| 3.17 | The HDR first-frame message is a constant string |
| extra | `ResumePreviewSubmission` returns early when preview was never suppressed (no redundant drop and MJPEG re-prime); the pixel-format observer guard does a plain read before its CAS |

Evaluated and not changed on this branch:

- 2.4 texture ring and overflow policy, 2.5 jitter defaults and missing-sequence
  feed, 2.7 monitor fill target, 3.1 and 3.2 pacing, 3.3 and 3.4 upload and
  HDR pass, section 4 demand gating (automation freshness is a wire contract),
  section 5 Flashback, section 6 timestamps, GC settings: each changes
  behaviour that needs a live measurement or product sign-off first.
- FFmpeg log level: libav calls a custom log callback for every level and only
  the default callback honours `av_log_set_level`, so lowering the level would
  not reduce the marshalling; the fix would be a `byte*`-typed callback that
  decodes the format string only for errors. Left as is.

Live checks to run first with a device: `WASAPI_CLIENT3_INIT` should report
`period_mode=minimum` (or a logged rejection with the reason); `MMCSS registered`
lines should appear for the encode loops and the "Pro Audio" workers; no
`UNIFIED_VIDEO_PREVIEW_TEXTURE_FAIL` burst at recording start; a recording-only
session should log no `MF_SOURCE_READER_D3D_BUFFER_MISS`-driven readback; and a
recording made with preview active should verify with `verify_recording`.

## 11. Environment knobs that shape latency (defaults)

| Knob | Default | Owner |
|---|---|---|
| `SUSSUDIO_PREVIEW_JITTER_TARGET_DEPTH` / `_MIN_TARGET_DEPTH` / `_MAX_TARGET_DEPTH` / `_MAX_DEPTH` | 3 / 2 / 8 / 12 | `MjpegPreviewJitterBuffer.cs:229-253` |
| `SUSSUDIO_PREVIEW_DISPLAY_CLOCK_PACING` / `_SUBMIT_DELAY_MS` / `_MIN_LEAD_MS` | 0 / 0.25 / 2.0 | `MjpegPreviewJitterBuffer.cs:212-214` |
| `SUSSUDIO_PREVIEW_PRESENT_SYNC_INTERVAL` | 1 | `D3D11PreviewRenderer.cs:60` |
| `SUSSUDIO_PREVIEW_DXGI_MAX_FRAME_LATENCY` | 1 | `:61` |
| `SUSSUDIO_PREVIEW_SWAPCHAIN_BUFFER_COUNT` | 2 | `:62` |
| `SUSSUDIO_PREVIEW_RENDER_QUEUE_DEPTH` | 4 | `:63` |
| `SUSSUDIO_PREVIEW_WAITABLE_SWAPCHAIN` | 1 | `:64` |
| `SUSSUDIO_PREVIEW_INPUT_VIEW_CACHE` | 1 | `:65` |
| `SUSSUDIO_PREVIEW_DXGI_FRAME_STATS` / `_SAMPLE_INTERVAL` (frames) / `_DWM_FLUSH` | 1 / 2 / 0 | `:66-68` |
| `SUSSUDIO_PREVIEW_RENDER_STALE_DROP` | 1 | `:73` |
| `SUSSUDIO_PREVIEW_INPUT_TEXTURE_RING` | 3 | `:74` |
| `SUSSUDIO_PREVIEW_RENDER_MMCSS_TASK` / `_PRIORITY` | Playback / 1 | `:75-76` |
| `SUSSUDIO_CAPTURE_READLOOP_MMCSS_TASK` / `_PRIORITY` | Capture / 1 | `MfSourceReaderVideoCapture.cs:49-52` |
| `SUSSUDIO_CAPTURE_POOLED_FANOUT` | 1 | `UnifiedVideoCapture.cs:83` |
| `SUSSUDIO_MJPEG_GPU_NATIVE_DECODE` | 1 at 60 fps and below, else 0 | `UnifiedVideoCapture.cs:622` |
| `SUSSUDIO_MJPEG_PREVIEW_EARLY_FORK` / `_LOAD_SHED` | 1 / 1 | `ParallelMjpegDecodePipeline.cs:122-125` |
| `SUSSUDIO_MJPEG_DECODE_MMCSS_TASK` / `_PRIORITY` | Playback / 0 | `:126-129` |
| `SUSSUDIO_MJPEG_COMPRESSED_BUDGET_MB` / `_REORDER_SLOTS` | 64 / 16 | `:609, 1333` |
| `SUSSUDIO_FLASHBACK_PLAYBACK_MMCSS_TASK` / `_PRIORITY` | Playback / 1 | `FlashbackPlaybackController.ThreadCommands.cs:32-33` |

No knobs exist for audio periods, encoder thread priority, source-reader
attributes, `SkipCpuReadback`, sink queue depths, or NVENC options.

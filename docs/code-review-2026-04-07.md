# Full-Codebase Code Review — 2026-04-07

Scope: All ~54K lines of C#/XAML (90 source files)
Method: Full files loaded into 1M context, 5 parallel review agents + direct MainWindow review
Focus: Performance, stability, races, resource lifecycle

Each finding is tagged with a **reality check** — how likely it is to actually cause problems.

---

## Confirmed Real Bugs

### ~~BUG-1: HdrValidationRunner has duplicate `-File` argument~~ FALSE POSITIVE
- **File**: `Services/HdrValidationRunner.cs:43-44`
- **Severity**: ~~HIGH~~ NOT A BUG
- **Category**: ~~Functional bug~~ Agent error
- **What**: PowerShell arguments contain `-File` twice. The first `-File` is PowerShell.exe's parameter (run this script). Everything after the script path is passed as script arguments. The second `-File` is the validate_hdr.ps1 script's own `-File` parameter. This is syntactically correct — PowerShell parses them as separate scopes.
- **Reality**: FALSE POSITIVE. Verified against the script's `param()` block. The script accepts `-File` as its media path parameter. The command line is valid.

### BUG-2: SettingsService non-atomic file write
- **File**: `Services/SettingsService.cs:83`
- **Severity**: MEDIUM
- **Category**: Data integrity
- **What**: `File.WriteAllText(settingsFilePath, json)` writes directly to the target file. If the process crashes or is killed mid-write (task manager, power loss, BSOD), the settings file is left partially written and unreadable.
- **Impact**: Next launch loads defaults — all user settings lost (output path, audio config, encoder settings, flashback config).
- **Fix**: Write to `.tmp`, then `File.Move(tmp, target, overwrite: true)` for atomic replace.
- **Reality**: CONFIRMED — standard file corruption scenario. Probability per session is low, but over thousands of users it will happen.

### BUG-3: SettingsService has no thread safety
- **File**: `Services/SettingsService.cs:46-91`
- **Severity**: MEDIUM
- **Category**: Race condition
- **What**: `Load` and `Save` are static methods with no synchronization. UI thread can trigger Save (via property changes) while background services read via Load.
- **Impact**: Corrupted read of partially-written JSON. In practice, the file is small and writes are fast, so the window is narrow.
- **Fix**: Add a static lock, or serialize through a single DispatcherQueue.
- **Reality**: CONFIRMED — real race, low probability per occurrence but exists.

### BUG-4: CancellationTokenSource leak on gain slider drag
- **File**: `ViewModels/MainViewModel.AudioControls.cs:264-281`
- **Severity**: MEDIUM
- **Category**: Resource leak
- **What**: `_gainFlashDebounceCts` is cancelled but never disposed when the user drags the analog gain slider. Each drag creates a new CTS. Over a session with frequent gain adjustment, this leaks CTS objects and their kernel event handles.
- **Impact**: Slow memory growth. Not a crash, but poor resource hygiene.
- **Fix**: Add `oldCts?.Dispose()` before creating the new CTS, matching the pattern already used for `_gainXuDebounceCts`.
- **Reality**: CONFIRMED — visible in source. Whether it matters depends on how often the gain slider is used.

---

## Real But Low Probability

### RISK-1: Single GPU texture failure kills 4K120 MJPEG session
- **File**: `Services/Capture/UnifiedVideoCapture.cs:548-557`
- **Severity**: HIGH (when it fires)
- **Category**: Error handling / stability
- **What**: When `_strictPreviewTextureRequired` is true (high-frame-rate MJPEG + D3D output + uncompressed), a single GPU texture delivery failure signals fatal error and tears down the entire capture session. No retry count or grace period.
- **Conditions**: Only fires in 4K120 MJPEG mode with D3D output. A transient driver hiccup (GPU thermal throttle, driver update check, display mode change) can trigger it.
- **Impact**: Preview and recording stop abruptly. User sees "fatal capture error."
- **Fix**: Add a consecutive failure threshold (3-5 failures) before signaling fatal.
- **Reality**: REAL but narrow path. Only affects 4K120 MJPEG users, and only on transient GPU errors. Has this been observed in QA? If not, it's theoretical.

### RISK-2: NVENC has no device-lost (TDR) recovery
- **File**: `Services/Recording/LibAvEncoder.cs:969-1143`
- **Severity**: HIGH (when it fires)
- **Category**: Error recovery / data loss
- **What**: The preview renderer has `HandleDeviceLost` + `IsDeviceLostException` checks. The encoder does not. If a TDR occurs during a long recording, `CopySubresourceRegion` fails with DXGI_ERROR_DEVICE_REMOVED, the exception propagates through the encoding loop, and the recording is lost.
- **Conditions**: TDR events. These are rare on stable systems but can happen during GPU driver updates, sustained thermal throttle, or competing GPU-heavy applications.
- **Impact**: Active recording is lost — the file may not be finalized.
- **Fix**: Catch device-removed HRESULTs, drain queued frames, finalize the current segment, then surface the error.
- **Reality**: REAL risk for long recordings (hours). Probability per session is low but consequence is total data loss of the recording.

### RISK-3: Named pipe server blocks all clients during slow commands
- **File**: `Services/Automation/NamedPipeAutomationServer.cs:132-155`
- **Severity**: MEDIUM
- **Category**: Availability
- **What**: Single-connection serial processing. Only one automation client served at a time. A slow command (flashback export, up to 300s) blocks all other ecctl/MCP clients.
- **Impact**: Automation tools hang until the slow command completes.
- **Fix**: Spawn a task per connection, or create the next pipe instance before handling the current one.
- **Reality**: REAL — but only matters when multiple automation clients connect simultaneously, or when a long export is running while MCP/ecctl needs the pipe.

### RISK-4: AudioDeviceWatcher timer race on rapid device changes
- **File**: `Services/Audio/AudioDeviceWatcher.cs:33-51`
- **Severity**: MEDIUM
- **Category**: Race condition / resource leak
- **What**: `ScheduleNotification` disposes the old timer and creates a new one. Called from COM callback threads. Two rapid notifications can both dispose and create timers concurrently, leaking one.
- **Impact**: Leaked timer fires a stale callback. Worst case: double device-changed notification. Not a crash.
- **Fix**: Use `Interlocked.Exchange` to atomically swap timers, or use `Timer.Change()` on a single instance.
- **Reality**: REAL race, but the consequence (extra device refresh) is benign. USB devices don't typically fire rapid-fire change events.

### RISK-5: Fire-and-forget tasks swallow flashback encoder failures
- **File**: `ViewModels/MainViewModel.Settings.cs:43,329,365`
- **Severity**: MEDIUM
- **Category**: Error handling
- **What**: `_ = _captureService.UpdateRecordingFormatAsync(format)` and similar. If these throw, the flashback encoder may not match the user's selected settings.
- **Impact**: Silent encoding mismatch — user selects H.264 but flashback continues encoding HEVC (or vice versa). No crash, but the export won't match expectations.
- **Fix**: Add `.ContinueWith(t => Logger.LogException(t.Exception), TaskContinuationOptions.OnlyOnFaulted)`.
- **Reality**: REAL pattern issue. Whether the underlying methods actually throw depends on the capture service implementation.

### RISK-6: CTS disposed while telemetry poll task running
- **File**: `Services/Capture/CaptureService.cs:2748-2753`
- **Severity**: MEDIUM
- **Category**: Resource lifecycle
- **What**: `StopTelemetryPoll` cancels and disposes the CTS without awaiting the poll task. If `Task.Delay` is mid-wait when the CTS is disposed, it may throw `ObjectDisposedException` instead of `OperationCanceledException`.
- **Impact**: Unobserved task exception during shutdown. Not a crash in .NET 8 (unobserved exceptions are swallowed by default), but makes debugging harder.
- **Fix**: Await the poll task before disposing the CTS, or don't dispose the CTS (let GC handle it).
- **Reality**: PARTIALLY REAL — in .NET 8, disposing an already-canceled CTS is safe. The race window is whether Cancel() and Dispose() happen atomically relative to the Task.Delay check. Low probability.

### RISK-7: Logger WriteDirect fallback on channel saturation
- **File**: `Logger.cs:56-68`
- **Severity**: LOW-MEDIUM
- **Category**: Performance / UI blocking
- **What**: When the 10,000-entry bounded channel is full, `TryWrite` falls back to synchronous `File.AppendAllText` on the caller's thread.
- **Impact**: If the UI thread logs during extreme saturation, it blocks on disk I/O.
- **Reality**: The channel would need 10,000 unprocessed entries. This only happens if the log writer task is dead or the disk is extremely slow. Effectively never in practice.

### RISK-8: CUDA interop Dispose releases resources in wrong order
- **File**: `Services/Gpu/CudaD3D11Interop.cs:370-401`
- **Severity**: MEDIUM
- **Category**: Resource lifecycle
- **What**: Primary context released before D3D11 textures. Also disposes `_deviceContext` and `_multithread` that belong to the shared D3D11 device.
- **Impact**: If CUDA unregistration fails silently, CUDA accesses freed D3D11 memory. Disposing shared device context decrements refcount on a shared object.
- **Reality**: REAL ordering issue but the CUDA unregistration almost always succeeds. The shared context dispose is the more concrete concern — could cause issues if the shared device is still in use elsewhere during cleanup.

### RISK-9: FlashbackExporter Dispose blocks indefinitely
- **File**: `Services/Flashback/FlashbackExporter.cs:116`
- **Severity**: MEDIUM
- **Category**: Stability / hang
- **What**: `_exportLock.Wait()` with no timeout after CTS cancellation. If FFmpeg is stuck in native I/O, the lock never releases and Dispose hangs.
- **Impact**: App hangs on close if an export was stuck.
- **Fix**: Add a 10-second timeout.
- **Reality**: REAL but requires FFmpeg to be stuck in native I/O (e.g., writing to a dead network share). Low probability on local storage.

### RISK-10: D3D11 Present(0, None) — no vsync, unbounded GPU queue
- **File**: `Services/Preview/D3D11PreviewRenderer.Rendering.cs:249,342,454`
- **Severity**: LOW-MEDIUM
- **Category**: Performance
- **What**: `Present(0, PresentFlags.None)` means no vsync. On a 120fps source with a 60Hz display, the GPU renders all 120fps into the flip queue, wasting GPU time and power.
- **Impact**: Higher GPU utilization and power draw than necessary. Does not cause visual issues.
- **Fix**: Use `Present(1, None)` for vsync, or `Present(0, AllowTearing)` for lowest latency.
- **Reality**: REAL but intentional? The app may want to match source cadence rather than display refresh. Worth discussing but may be by design.

---

## Defensive Concerns (Not Bugs, But Worth Noting)

### DEF-1: Shared AVPacket across encoder drains
- **File**: `Services/Recording/LibAvEncoder.cs:358`
- **What**: Single `_packet` reused across video/audio/mic drain methods.
- **Status**: SAFE under current sequential calling pattern. Would break if parallelized.
- **Action**: Low-cost fix (allocate 3 packets) for future-proofing, but not urgent.

### DEF-2: HW texture pool could theoretically wrap
- **File**: `Services/Recording/LibAvEncoder.cs:492`
- **What**: 8-texture round-robin pool. Agent speculated NVENC could hold 6-7 refs with slow presets.
- **Status**: With default presets, NVENC pipeline depth is 3-4. Pool of 8 is fine.
- **Action**: Only investigate if using P7/slow preset. Add a pool-depth warning log if desired.

### DEF-3: D3D11VA frame clone vs. renderer race
- **File**: `Services/Flashback/FlashbackDecoder.cs:894`
- **What**: Playback controller frees previous frame's D3D11 texture while renderer may still be copying.
- **Status**: D3D11 commands on the same immediate context are sequentially ordered. If they share a context, there's no race. Need to verify device context sharing.
- **Action**: Investigate whether decoder and renderer share the same D3D11 device context. If yes, safe. If no, add a GPU fence.

### DEF-4: WASAPI blocking async on capture thread
- **File**: `Services/Audio/WasapiAudioCapture.cs:454-476`
- **What**: `.GetAwaiter().GetResult()` on `WriteAudioAsync`.
- **Status**: FALSE POSITIVE. Both `LibAvRecordingSink.WriteAudioAsync` and `FlashbackEncoderSink.WriteAudioAsync` return `Task.CompletedTask` synchronously. They just copy bytes and enqueue. Zero deadlock risk.
- **Action**: None required. Code is correct.

### DEF-5: NVDEC DecodeFrame returns internal frame
- **File**: `Services/Gpu/NvdecMjpegDecoder.cs:338-387`
- **What**: Returns `_decodedFrame` owned by the decoder. Next decode unrefs it.
- **Status**: All callers clone the frame before use. Safe under current calling pattern.
- **Action**: Future-proofing only. Document the ownership contract.

### DEF-6: CaptureSettings is mutable, shared across threads
- **File**: `Models/CaptureSettings.cs:35-70`
- **What**: Mutable class with public setters passed to pipeline components.
- **Status**: In practice, settings are built once and passed to pipelines. UI changes create new settings objects via `BuildCurrentSettings()`.
- **Action**: Make immutable (record) if doing a refactor pass, but not a current bug.

### DEF-7: LibAvRecordingSink.Dispose blocks calling thread
- **File**: `Services/Recording/LibAvRecordingSink.cs:468`
- **What**: `DisposeAsync().AsTask().GetAwaiter().GetResult()` — deadlock risk if called on UI thread.
- **Status**: In practice, recording sink dispose is called from CaptureService cleanup which runs on a background thread. The sync Dispose path exists as IDisposable fallback.
- **Action**: Verify all callers use DisposeAsync. If so, the sync path is dead code.

### DEF-8: FlashbackEncoderSink.Dispose same pattern
- **File**: `Services/Flashback/FlashbackEncoderSink.cs:500`
- **Status**: Same as DEF-7. Verify callers.

### DEF-9: MainViewModel.Dispose blocks on Task.Run of async dispose
- **File**: `ViewModels/MainViewModel.cs:1251-1273`
- **What**: Sync Dispose calls `Task.Run(DisposeCoreAsync).GetAwaiter().GetResult()`.
- **Status**: MainWindow.Closed calls `await ViewModel.DisposeAsync()`, not the sync path. The sync Dispose is IDisposable fallback.
- **Action**: Verify no caller uses sync Dispose. If dead code, consider removing.

### DEF-10: Settings mutation without transition lock
- **File**: `Services/Capture/CaptureService.cs:145-169`
- **What**: `UpdateFlashbackSettings` and `UpdateEncodingSettings` mutate `_currentSettings` without `_sessionTransitionLock`.
- **Status**: These are called from the UI thread. Readers on other threads could see partially-updated settings, but the values are independent (buffer minutes, GPU decode flag) and a stale read produces a slightly-off config, not a crash.
- **Action**: Low risk. Fix if doing a threading cleanup pass.

### DEF-11: Flashback exporter field lifecycle
- **File**: `Services/Capture/CaptureService.cs:362-430`
- **What**: Agent flagged `_flashbackExporter ??= new FlashbackExporter()` lifecycle gap.
- **Status**: Both export and dispose paths share `_sessionTransitionLock`, so they can't race. The agent contradicted itself on this one.
- **Action**: None required.

### DEF-12: MfSourceReaderVideoCapture cadence ring buffer
- **File**: `Services/Capture/MfSourceReaderVideoCapture.cs:746-773`
- **What**: Writer uses Volatile, reader uses lock. Mixed synchronization.
- **Status**: The worst case is a slightly stale FPS reading in the stats overlay. Not a crash or corruption.
- **Action**: Cosmetic. Fix if doing a threading cleanup pass.

---

## Low Priority / Code Quality

### QUALITY-1: Logger opens/closes file on every write
- **File**: `Logger.cs:164-177`
- `File.AppendAllText` per log entry. Batch writes would reduce I/O.

### QUALITY-2: Logger static constructor does file I/O
- **File**: `Logger.cs:32-46`
- Can cause `TypeInitializationException` if RuntimePaths fails.

### QUALITY-3: App constructor runs WMI queries synchronously
- **File**: `App.xaml.cs:22-23`
- `Logger.LogSystemInfo()` runs WMI queries (100-500ms each) on UI thread during startup.

### QUALITY-4: FlashbackBufferManager List.RemoveAt(0) is O(n)
- **File**: `Services/Flashback/FlashbackBufferManager.cs:680`
- Use LinkedList or Queue for O(1) head removal.

### QUALITY-5: NativeXuAtCommandProvider opens/closes device handle every poll
- **File**: `Services/Telemetry/NativeXuAtCommandProvider.cs:218`
- 2 handle open/close cycles per second. Could cache the handle.

### QUALITY-6: ProcessSupervisor output tasks may leak on timeout
- **File**: `Services/ProcessSupervisor.cs:86-87`
- stdout/stderr ReadToEndAsync tasks may not complete if process is killed.

### QUALITY-7: FfmpegRuntimeLocator blocks thread pool on Lazy init
- **File**: `Services/FfmpegRuntimeLocator.cs:230-265`
- `where.exe` execution is synchronous inside a Lazy. If it hangs, all threads block.

### QUALITY-8: AutomationDiagnosticsHub polls Process.Refresh every 500ms
- **File**: `Services/Automation/AutomationDiagnosticsHub.cs:441-456`
- Polls even when no clients are connected.

### QUALITY-9: FlashbackEncoderSink disk bytes update interval
- **File**: `Services/Flashback/FlashbackEncoderSink.cs:941-943`
- Updates only every 300 frames (~2.5s at 120fps). Up to ~142MB can accumulate beyond disk limit.

### QUALITY-10: SoftwareMjpegDecoder UV interleave is scalar
- **File**: `Services/Gpu/SoftwareMjpegDecoder.cs:220-231`
- 248M iterations/second at 4K120. AVX2 intrinsics would reduce by 32x.

### QUALITY-11: NativeXuAudioControlService Thread.Sleep on async path
- **File**: `Services/Audio/NativeXuAudioControlService.cs:302`
- `Thread.Sleep(100)` in an async method. Should be `await Task.Delay(100)`.

### QUALITY-12: FlashbackBufferManager mixed Interlocked + lock for eviction pause
- **File**: `Services/Flashback/FlashbackBufferManager.cs:197-208`
- Redundant Interlocked inside a lock. Pick one synchronization strategy.

### QUALITY-13: Volume fade-in save race
- **File**: `MainWindow.Bindings.cs:389-396`
- Brief window where PreviewVolume=0 with SuppressVolumeSave=false. VolumeSaveOverride mitigates.

### QUALITY-14: EnableDependentAnimation on entrance animations
- **File**: `MainWindow.Animations.cs:111-282`
- Dependent animations run on UI thread. One-time cost, unlikely to matter.

### QUALITY-15: Stats dock Width animation is dependent
- **File**: `MainWindow.StatsOverlay.cs:176-183`
- Width animation forces layout every frame while preview is running.

### QUALITY-16: AutomationCommandDispatcher SetMicrophoneEnabled missing UI dispatch
- **File**: `Services/Automation/AutomationCommandDispatcher.cs:572-575`
- Sets `IsMicrophoneEnabled` directly without `InvokeOnUiThreadAsync`. Unlike every other setter.

### QUALITY-17: FlashbackPlaybackController scrub drain may reorder commands
- **File**: `Services/Flashback/FlashbackPlaybackController.cs:407-418`
- Re-queued non-scrub command goes to back of queue, potentially behind newer scrub commands.

### QUALITY-18: MediaFormat Equals/GetHashCode fuzzy float comparison
- **File**: `Models/MediaFormat.cs:59-93`
- Fuzzy equality (0.01 threshold) with rounded hash. Consistent but fragile.

---

## False Positives (Agent Errors)

### FP-1: WASAPI capture thread deadlock on WriteAudioAsync
- **Flagged as**: P0 Critical
- **Reality**: Both sink implementations return `Task.CompletedTask` synchronously. No deadlock possible.

### FP-2: Flashback exporter lifecycle race
- **Flagged as**: P1 High
- **Reality**: Agent noted both paths share `_sessionTransitionLock`, then flagged it as a race anyway. The lock serializes them.

### FP-3: Logger BoundedChannel FullMode.Wait deadlock
- **Flagged as**: P0 Critical
- **Reality**: The code uses `TryWrite` (non-blocking), not `WriteAsync`. The FullMode.Wait setting is irrelevant because it only affects `WriteAsync`.

### FP-4: StatsWindow WndProc GC collection
- **Flagged as**: P1 High (later downgraded by agent)
- **Reality**: Delegate is stored in `_minSizeProc` field, preventing GC. Agent caught its own error.

---

## Summary

| Category | Count |
|----------|------:|
| Confirmed real bugs | 3 |
| Real but low probability risks | 10 |
| Defensive concerns (safe today) | 12 |
| Code quality improvements | 18 |
| False positives | 5 |
| **Total findings** | **48** |

## Recommended Fix Order

1. **BUG-2** — SettingsService atomic write. FIXED 2026-04-07.
2. **BUG-3** — SettingsService thread safety. FIXED 2026-04-07.
3. **BUG-4** — CTS leak on gain slider. FIXED 2026-04-07.
4. **RISK-1** — GPU failure threshold for 4K120. Add retry count.
5. **RISK-2** — NVENC device-lost recovery. Larger effort but prevents data loss.
6. **RISK-3** — Pipe server parallel connections. Unblocks automation.

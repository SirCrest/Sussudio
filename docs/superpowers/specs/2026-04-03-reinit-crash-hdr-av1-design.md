# Fix: Pipeline Reinit Crash, AV1+HDR, HDR Resolution Comment

Date: 2026-04-03

## Problem Statement

Three issues found during autonomous QA:

1. **HDR resolution drop undocumented**: The app intentionally drops resolution when HDR is enabled to maintain the user's frame rate (capture card USB bandwidth constraint). This is correct behavior but has no code comment explaining why.

2. **AV1+HDR forced to HEVC**: When HDR is enabled, `RebuildRecordingFormatOptions()` unconditionally selects HEVC, ignoring the user's AV1 selection even though AV1 nvenc supports HDR (Main 10, bt2020, PQ).

3. **Pipeline reinit crash**: The 2nd pipeline-reinit-triggering setting change (resolution, FPS, video format, HDR) crashes the app. Root cause: timing gaps between Stop and Start allow the render thread and flashback encoder to access freed resources.

## Fix 1: HDR Resolution Comment

**File**: `MainViewModel.DeviceManagement.cs`
**Location**: Above the `SelectHdrResolutionOption` call (~line 803)

Add a block comment explaining:
- The 4K X capture card cannot deliver HDR at all resolution+FPS combinations due to USB bandwidth limits
- The app intentionally drops resolution to preserve the user's chosen frame rate
- This is a hardware constraint, not a software bug

No behavior change.

## Fix 2: AV1+HDR Codec Selection

**File**: `MainViewModel.DeviceManagement.cs`
**Location**: Lines 564-570, inside `RebuildRecordingFormatOptions()`

**Current code** (broken):
```csharp
if (IsHdrEnabled)
{
    targetFormat = formats.FirstOrDefault(format =>
        string.Equals(format, HevcRecordingFormat, ...))
        ?? formats.FirstOrDefault(format =>
            string.Equals(format, Av1RecordingFormat, ...))
        ?? formats.FirstOrDefault();
}
```

**Fixed code**:
```csharp
if (IsHdrEnabled)
{
    // Preserve user's codec if it already supports HDR (AV1 or HEVC)
    if (!string.IsNullOrWhiteSpace(SelectedRecordingFormat) &&
        formats.Any(f => string.Equals(f, SelectedRecordingFormat, ...)) &&
        IsHdrCompatibleRecordingFormat(SelectedRecordingFormat))
    {
        targetFormat = SelectedRecordingFormat;
    }
    else
    {
        targetFormat = formats.FirstOrDefault(format =>
            string.Equals(format, HevcRecordingFormat, ...))
            ?? formats.FirstOrDefault(format =>
                string.Equals(format, Av1RecordingFormat, ...))
            ?? formats.FirstOrDefault();
    }
}
```

The existing `IsHdrCompatibleRecordingFormat()` (line 534) already returns true for both HEVC and AV1. The only change is checking the user's current selection first.

## Fix 3: Pipeline Reinit Crash

### 3a. D3D11 Renderer — Close TOCTOU window in render thread

**File**: `D3D11PreviewRenderer.Rendering.cs`

**Problem**: The render thread reads `_swapChain` (line 56) and checks `_swapChainBound` (line 57), but between that check and the actual D3D11 call (`VideoProcessorBlt`, `Present`), `Stop()` on the UI thread can CAS `_swapChainBound` to 0 and unbind the panel. The render thread then calls into a native object backed by an unbound swap chain, causing an `AccessViolationException` (.NET 8 cannot catch this).

**Fix**:

1. In `RenderFrameWithVideoProcessor()` (~line 208): Add a guard before the `VideoProcessorBlt` call:
   ```csharp
   if (Volatile.Read(ref _stopRequested) != 0 || Volatile.Read(ref _swapChainBound) == 0)
       return;
   ```

2. Add the same guard in `RenderNv12WithShader()` and `RenderHdrFrameWithShader()` before their `Present()` calls.

3. In `Stop()` (~line 570): Set `_stopRequested = 1` first (already done), then add a brief `SpinWait` (8 iterations) before the CAS unbind at line 583, giving any in-flight `RenderFrame` call a chance to see `_stopRequested` and bail out before the swap chain is unbound.

4. Move `_frameReadyEvent.Set()` (line 606) to after the CAS unbind block — wake the render thread AFTER the swap chain is safely unbound, so it sees `_stopRequested` and exits without trying to render.

### 3b. Flashback Sink — Fix disposal ordering

**File**: `CaptureService.cs`, `DisposeFlashbackPreviewBackendAsync()` (~line 642)

**Problem**: Lines 646-654 set `_flashbackSink = null` BEFORE calling `StopAsync()`. Between null-assignment and stop, code that checks `_flashbackSink` (like `IsFlashbackActive`) sees null and may skip operations the still-running encoder depends on.

**Fix**: Reorder the disposal sequence:
1. Detach feeds (already at lines 671-673) — stops new frames from entering
2. Call `StopAsync()` on the sink — waits for encoding loop to fully drain
3. Dispose the sink
4. THEN set `_flashbackSink = null` and clear other fields
5. Continue with buffer manager disposal

This ensures the encoding loop is fully exited before any code can observe the null state.

### 3c. FlashbackEncoderSink — Frame size validation (defense-in-depth)

**File**: `FlashbackEncoderSink.cs`

**Problem**: `DrainVideoPackets()` (line 866) passes `_width` and `_height` to the encoder without validating that the incoming buffer size matches. If a stale frame from the old resolution leaks through, the encoder receives a buffer with wrong dimensions.

**Fix**: In the video frame enqueue method, add a buffer size check:
```csharp
var expectedSize = _width * _height * 3 / 2; // NV12
if (packet.Length != expectedSize)
{
    Logger.Log($"FLASHBACK_FRAME_SIZE_MISMATCH expected={expectedSize} actual={packet.Length}");
    ReturnBuffer(packet.Buffer);
    Interlocked.Increment(ref _droppedVideoFrames);
    continue;
}
```

This is a safety net — fix 3b should prevent mismatched frames from arriving, but if they do, we drop them instead of crashing.

## Testing

After all fixes:
1. Rapid resolution changes: Source → 1080p → 1440p → 4K without relaunch
2. Rapid FPS changes: 120 → 60 → 30 → 120 without relaunch
3. HDR toggle on/off/on without relaunch
4. AV1 + HDR: verify av1_nvenc is used, output has yuv420p10le + bt2020 + smpte2084
5. Flashback continues encoding through all reinit cycles (0 dropped frames)
6. Record during/after reinit cycles — verify output integrity with ffprobe

## Files Modified

| File | Change |
|------|--------|
| `MainViewModel.DeviceManagement.cs` | Comment (fix 1), AV1+HDR selection (fix 2) |
| `D3D11PreviewRenderer.Rendering.cs` | Stop-flag guards before native calls (fix 3a) |
| `D3D11PreviewRenderer.cs` | SpinWait + event reorder in Stop() (fix 3a) |
| `CaptureService.cs` | Disposal ordering in DisposeFlashbackPreviewBackendAsync (fix 3b) |
| `FlashbackEncoderSink.cs` | Frame size validation (fix 3c) |

# ElgatoCapture - Project Scope & Roadmap

## Vision

A professional Windows desktop capture application for Elgato devices with:
- Realtime video and audio preview
- Customizable resolution/framerate from device hardware
- Raw/lossless and compressed recording via FFmpeg
- Professional recording status displays

---

## Current Status

### ✅ Implemented
| Feature | Status | Notes |
|---------|--------|-------|
| Device enumeration | ✅ Complete | Elgato devices with audio matching |
| Video preview | ✅ Complete | GPU→CPU conversion for YUY2/NV12 |
| Audio preview | ✅ Complete | AudioGraph passthrough to speakers |
| Resolution selector | ✅ Complete | Populated from device capabilities |
| Framerate selector | ✅ Complete | Dynamic per resolution |
| Format selector | ✅ Complete | H.264, HEVC, Uncompressed AVI |
| Quality presets | ✅ Complete | Low→Lossless + Custom bitrate |
| HDR toggle | ✅ Complete | Device-aware enable/disable |
| Output path | ✅ Complete | Folder picker + Videos default |
| Recording timer | ✅ Complete | HH:MM:SS display |
| Disk space | ✅ Complete | Real-time GB free display |
| Recording indicator | ✅ Complete | Red "REC" badge with blinking dot |
| FFmpeg encoding | ✅ Complete | Dual named pipes for A/V sync |

### ❌ Missing Features
| Feature | Priority | Notes |
|---------|----------|-------|
| Rolling bitrate average | HIGH | Calculate from FFmpeg or file growth |
| Current file size | HIGH | Display during recording |
| True lossless codec | MEDIUM | FFV1, UT Video, or ProRes |
| Dropped frame counter | MEDIUM | Warning if frames missed |
| HDR passthrough | LOW | Preserve P010 10-bit through pipeline |
| Audio format options | LOW | Currently fixed at AAC 192kbps |
| Encoder preset selector | LOW | ultrafast→veryslow |

---

## Supported Pixel Formats

| Format | Source | Status |
|--------|--------|--------|
| YUY2 | Capture devices | ✅ Detected, converted to BGRA8 |
| NV12 | Capture devices | ✅ Detected, converted to BGRA8 |
| P010 | HDR devices | ⚠️ Detected, but 10-bit lost in conversion |
| YV12 | Some devices | ⚠️ Not explicitly tested |
| BGRA8 | Internal format | ✅ Used for all encoding |

**Pipeline:** Device (YUY2/NV12/P010) → GPU → SoftwareBitmap (BGRA8) → FFmpeg

---

## Recording Formats

| Format | Encoder | Container | CFR | Notes |
|--------|---------|-----------|-----|-------|
| H.264 | libx264 | MP4 | ✅ | Default, force-cfr=1 |
| HEVC | libx265 | MP4 | ⚠️ | No force-cfr option |
| Lossless | libx264 CRF 0 | MP4 | ✅ | Visually lossless |
| Uncompressed | rawvideo | AVI | ✅ | True raw BGRA frames |

---

## UI Layout

```
┌─────────────────────────────────────────────────────────────────┐
│  [Device ▼]  [Refresh]                                          │
├─────────────────────────────────────────────────────────────────┤
│                                                        [REC]    │
│                      VIDEO PREVIEW                              │
│                        1920x1080                                │
│                                                                 │
├─────────────────────────────────────────────────────────────────┤
│  Resolution: [1920x1080 ▼]  FPS: [60 ▼]  Format: [H.264 ▼]      │
│  Quality: [High ▼]  Bitrate: [25 Mbps]  HDR: [OFF]  Audio: [ON] │
├─────────────────────────────────────────────────────────────────┤
│  Output: [C:\Users\...\Videos]  [Browse]                        │
│  Free: 245.3 GB  |  Size: 1.2 GB  |  Bitrate: 24.5 Mbps         │
├─────────────────────────────────────────────────────────────────┤
│      [Start Preview]              [● RECORD / 00:05:32 ■]       │
└─────────────────────────────────────────────────────────────────┘
```

---

## Phase 1: Bug Fixes (COMPLETED)

Critical stability fixes applied:
- ✅ Dispose() deadlock prevention (IAsyncDisposable)
- ✅ Thread-safe shared state (volatile, Interlocked)
- ✅ Audio callback race conditions (local variable capture)
- ✅ FFmpeg exit detection
- ✅ Error propagation for audio failures
- ✅ DeviceInputNode proper disposal

---

## Phase 2: Recording Status UI (TODO)

### 2.1 File Size Display
- Monitor output file size during recording
- Update every 500ms
- Format: "1.24 GB" or "124.5 MB"

### 2.2 Rolling Bitrate Average
- Calculate from file size growth over time
- Window: last 5 seconds
- Format: "24.5 Mbps"

### 2.3 Implementation
**Files to modify:**
- [MainViewModel.cs](ElgatoCapture/ViewModels/MainViewModel.cs) - Add properties
- [MainWindow.xaml](ElgatoCapture/MainWindow.xaml) - Add UI elements
- [FFmpegEncoderService.cs](ElgatoCapture/Services/FFmpegEncoderService.cs) - Expose stats

---

## Phase 3: Format Enhancements (TODO)

### 3.1 True Lossless Output
- Add FFV1 or UT Video codec option
- MKV container for FFV1

### 3.2 HDR Passthrough
- Preserve P010 format through encoding
- Add 10-bit H.265 output option

### 3.3 Audio Format Options
- Add PCM (WAV) audio option
- Add FLAC audio option

---

## Architecture Notes

### FFmpeg Pipeline
```
Video: FrameReader → BGRA8 Queue → stdin (pipe:0) → FFmpeg
Audio: AudioGraph → PCM Queue → Named Pipe → FFmpeg
```

### Audio Timestamp Fix (Implemented)
DirectShow audio had device-uptime timestamps (~32930s offset).
Solution: Dual named pipes - we control both stream timestamps.

---

## Technical Debt

1. **Async void handlers** - PreviewFrameReader_FrameArrived should be sync
2. **60fps preview queuing** - No frame skipping under load
3. **Silent catch blocks** - Some exceptions swallowed without logging
4. **Hardcoded device list** - Only "Game Capture Neo" in allowlist

---

## Proposed CLAUDE.md Update

Replace current CLAUDE.md with the following:

```markdown
# ElgatoCapture

Windows desktop capture application for Elgato devices with realtime preview and professional recording features.

## Project Vision

- **Realtime preview**: Video and audio passthrough with low latency
- **Hardware-driven settings**: Resolution/framerate from device capabilities
- **Flexible output**: Raw/lossless and compressed via FFmpeg
- **Professional UX**: Recording stats, disk space, bitrate monitoring

## Tech Stack

- .NET 8.0 / WinUI 3 (Windows App SDK 1.8)
- MVVM with CommunityToolkit.Mvvm
- FFmpeg subprocess for CFR video encoding
- Windows 10 Build 17763+

## Supported Devices

Elgato capture cards with video+audio device matching:
- Game Capture Neo
- HD60 S+, HD60 X
- 4K60 Pro, 4K X, 4K S

## Project Structure

```
ElgatoCapture/
├── Models/
│   ├── CaptureDevice.cs      # Device with audio association
│   ├── MediaFormat.cs        # Resolution, FPS, HDR specs
│   └── CaptureSettings.cs    # Recording config + bitrate calc
├── Services/
│   ├── DeviceService.cs      # Device enumeration, audio matching
│   ├── CaptureService.cs     # Capture pipeline, preview, AudioGraph
│   └── FFmpegEncoderService.cs  # FFmpeg subprocess, dual named pipes
├── ViewModels/
│   └── MainViewModel.cs      # UI state & commands
├── MainWindow.xaml(.cs)      # UI + preview frame handling
└── Logger.cs                 # Debug log: ~/Documents/ElgatoCapture_Debug.log
```

## Pixel Format Support

| Format | Source | Handling |
|--------|--------|----------|
| YUY2 | Most Elgato devices | GPU→BGRA8 conversion |
| NV12 | Some devices | GPU→BGRA8 conversion |
| P010 | HDR devices (10-bit) | GPU→BGRA8 (loses HDR) |

**Pipeline:** Device → Direct3DSurface (GPU) → SoftwareBitmap (BGRA8) → FFmpeg

## Recording Formats

| Format | Encoder | CFR | Use Case |
|--------|---------|-----|----------|
| H.264 MP4 | libx264 | ✅ | Default, broad compatibility |
| HEVC MP4 | libx265 | ⚠️ | 40% smaller, HDR metadata |
| Uncompressed AVI | rawvideo | ✅ | Lossless workflows |

Quality presets: Low (8Mbps) → Lossless (CRF 0) with resolution/framerate scaling.

## Audio Architecture

- **Preview**: AudioGraph → default output device (speakers)
- **Recording**: Separate AudioGraph → PCM s16le → named pipe → FFmpeg
- **Format**: 48kHz stereo, AAC 192kbps output

Dual named pipes ensure video (stdin) and audio (named pipe) both start at timestamp 0.

## UI Features

### Implemented
- Device selector with refresh
- Resolution/framerate from device capabilities
- Format and quality selectors
- HDR toggle (device-aware)
- Audio preview toggle
- Recording timer (HH:MM:SS)
- Disk space display
- Recording indicator badge

### Planned
- Rolling bitrate average
- Current file size during recording
- True lossless codecs (FFV1)
- Dropped frame warning

## Conventions

- **Fields**: `_camelCase` | **Properties**: `PascalCase`
- **Async**: Always suffix with `Async`
- **MVVM**: `[ObservableProperty]`, `[RelayCommand]` - no UI refs in Services
- **Never** use `.Result` or `.Wait()` (causes deadlocks)
- **Disposal**: Implement `IAsyncDisposable` for async cleanup

## Key Technical Patterns

### GPU Frame Conversion
```csharp
softwareBitmap = await SoftwareBitmap.CreateCopyFromSurfaceAsync(
    frame.VideoMediaFrame.Direct3DSurface,
    BitmapAlphaMode.Ignore);
```

### Thread-Safe State
```csharp
private volatile bool _isEncoding;  // Cross-thread reads
Interlocked.Increment(ref _encodedFrameCount);  // Atomic updates
```

### Race Condition Prevention
```csharp
// Capture local references before null checks
var encoder = _ffmpegEncoder;
var outputNode = _audioFrameOutputNode;
if (encoder == null || outputNode == null) return;
```

## Troubleshooting

| Issue | Check |
|-------|-------|
| Blank preview | Is Direct3DSurface being converted? Check logs |
| No audio | Look for "✓ Found audio device" in logs |
| VFR output | Ensure using FFmpeg encoder, not Media Foundation |
| Audio sync issues | Check for dual named pipes in FFmpeg args |
| UI freeze on close | Verify IAsyncDisposable is used, not blocking Dispose() |

**Log file**: `C:\Users\{username}\Documents\ElgatoCapture_Debug.log`

## Development Notes

- Primary test device: **Elgato Game Capture Neo**
- FFmpeg must be in PATH or application directory
- WinUI 3 quirk: No Width/Height in Window XAML - use `AppWindow.Resize()`
```

---

## Bug Fixes Applied (Reference)

### CRITICAL Issues (Fix First)

### 1. Dispose() Deadlock - Both Services
**Files**: [FFmpegEncoderService.cs:684](ElgatoCapture/Services/FFmpegEncoderService.cs), [CaptureService.cs:821](ElgatoCapture/Services/CaptureService.cs)

```csharp
// PROBLEM: Blocks UI thread, can deadlock
public void Dispose()
{
    StopEncodingAsync().GetAwaiter().GetResult();  // DEADLOCK RISK
}
```

**Fix**: Implement async disposal pattern or use fire-and-forget with proper logging.

---

### 2. Thread-Unsafe Shared State
**File**: [FFmpegEncoderService.cs](ElgatoCapture/Services/FFmpegEncoderService.cs)

Multiple threads read/write without synchronization:
- `_isEncoding` (lines 31, 342, 416, 505, 571)
- `_useAudioPipe` (lines 40, 148)
- `_audioQueueReady` (lines 32, 69, 382)

**Fix**: Add `volatile` keyword or use `Interlocked` operations.

---

### 3. Race Condition in Audio Callback
**File**: [CaptureService.cs:444-481](ElgatoCapture/Services/CaptureService.cs)

`RecordingAudioGraph_QuantumStarted` accesses `_ffmpegEncoder` and `_audioFrameOutputNode` while `StopRecordingAsync()` can set them to null on UI thread.

**Fix**: Add lock or null-conditional checks with local variable capture.

---

### 4. Async Void Frame Handler
**File**: [MainWindow.xaml.cs:480](ElgatoCapture/MainWindow.xaml.cs)

```csharp
private async void PreviewFrameReader_FrameArrived(...)  // async void = crash risk
```

Fires at 60fps. If exception occurs, crashes silently with no recovery.

**Fix**: Convert to sync with `TryEnqueue`, add frame skipping.

---

## HIGH Priority Issues

### 5. Audio Pipe Timeout Silent Failure
**File**: [FFmpegEncoderService.cs:468-477](ElgatoCapture/Services/FFmpegEncoderService.cs)

15-second timeout silently drops audio with no user notification.

**Fix**: Fire `ErrorOccurred` event when timeout occurs.

---

### 6. Null Check-Then-Use Race Conditions
**File**: [FFmpegEncoderService.cs:416-424](ElgatoCapture/Services/FFmpegEncoderService.cs)

```csharp
while (_videoStream != null)  // Check
{
    await _videoStream.WriteAsync(...);  // Use - can be null by now!
}
```

**Fix**: Capture to local variable before null check.

---

### 7. No FFmpeg Premature Exit Detection
**File**: [FFmpegEncoderService.cs:410-450](ElgatoCapture/Services/FFmpegEncoderService.cs)

`WriteVideoFramesAsync` doesn't check `_ffmpegProcess.HasExited`. Continues writing to closed pipe.

**Fix**: Check `HasExited` in write loop, fire error event.

---

### 8. Stderr Reader Task Not Awaited
**File**: [FFmpegEncoderService.cs:563-612](ElgatoCapture/Services/FFmpegEncoderService.cs)

`_stderrReaderTask` created at line 190 but never awaited in `StopEncodingAsync`.

**Fix**: Add `await _stderrReaderTask` in cleanup sequence.

---

### 9. Incomplete Audio Graph Cleanup
**File**: [CaptureService.cs:602-611](ElgatoCapture/Services/CaptureService.cs)

`DeviceInputNode` created in `SetupRecordingAudioCaptureAsync` but never disposed.

**Fix**: Store reference to input node and dispose in cleanup.

---

### 10. Fire-and-Forget Cleanup Calls
**File**: [MainWindow.xaml.cs:459,466](ElgatoCapture/MainWindow.xaml.cs)

```csharp
_ = _previewFrameReader.StopAsync();  // Not awaited!
```

**Fix**: Await or track these tasks.

---

## MEDIUM Priority Issues

### 11. Audio Sample Drain Race Condition
**File**: [FFmpegEncoderService.cs:496-502](ElgatoCapture/Services/FFmpegEncoderService.cs)

No synchronization between `EnqueueAudioSamples()` and drain loop. Samples could be reordered.

---

### 12. Preview Auto-Start Race Condition
**File**: [MainViewModel.cs:204-228](ElgatoCapture/ViewModels/MainViewModel.cs)

```csharp
await Task.Delay(50);  // RACE CONDITION
SelectedDevice = Devices[0];
await StartPreviewAsync();
```

If called rapidly, old preview frames mixed with new device.

**Fix**: Explicit stop before device change.

---

### 13. Preview Frame Rate Not Limited
**File**: [MainWindow.xaml.cs:597-624](ElgatoCapture/MainWindow.xaml.cs)

Queues 60 tasks/sec to dispatcher. Under load, causes lag/memory buildup.

**Fix**: Add frame skipping (process max 30fps).

---

### 14. Silent Exception Catches (9 locations)
- [DeviceService.cs:168](ElgatoCapture/Services/DeviceService.cs) - `catch (Exception) { }`
- [Logger.cs:23,42](ElgatoCapture/Logger.cs) - `catch { }`
- [FFmpegEncoderService.cs:110,627,637](ElgatoCapture/Services/FFmpegEncoderService.cs) - `catch { }`
- [CaptureService.cs:479](ElgatoCapture/Services/CaptureService.cs) - Debug.WriteLine only

**Fix**: Add at minimum `Logger.Log()` in each catch.

---

### 15. FFmpeg Error Detection Incomplete
**File**: [FFmpegEncoderService.cs:549-551](ElgatoCapture/Services/FFmpegEncoderService.cs)

Only catches stderr lines containing "Error". Misses: "Invalid encoder", "pipe format not supported", etc.

**Fix**: Expand error detection patterns.

---

### 16. Audio Setup Failures Not Propagated
**File**: [CaptureService.cs:397-428](ElgatoCapture/Services/CaptureService.cs)

Multiple silent returns instead of error events when AudioGraph fails.

**Fix**: Fire `ErrorOccurred` event for each failure path.

---

## LOW Priority Issues

### 17. No Bounds Validation on CaptureSettings
**File**: [CaptureSettings.cs](ElgatoCapture/Models/CaptureSettings.cs)

Width/Height/FrameRate have no validation. Can crash FFmpeg with bad values.

---

### 18. Hardcoded Device Allowlist
**File**: [DeviceService.cs:16-23](ElgatoCapture/Services/DeviceService.cs)

Only "Game Capture Neo" enabled. New devices require code changes.

---

### 19. Hardcoded Audio Format (48kHz)
**File**: [FFmpegEncoderService.cs:393](ElgatoCapture/Services/FFmpegEncoderService.cs)

Some devices support 44.1kHz or 96kHz.

---

### 20. EncodedFrameCount Not Thread-Safe
**File**: [FFmpegEncoderService.cs:425](ElgatoCapture/Services/FFmpegEncoderService.cs)

`EncodedFrameCount++` without `Interlocked.Increment`.

---

## Implementation Plan

### Phase 1: Critical Fixes (Stability)
1. Fix Dispose() deadlocks in both services
2. Add volatile/Interlocked for shared state
3. Fix race condition in audio callback
4. Convert async void handler to sync

### Phase 2: High Priority Fixes (Reliability)
5. Add error notification for audio pipe timeout
6. Fix null check-then-use patterns
7. Detect FFmpeg premature exit
8. Await stderr reader task
9. Dispose DeviceInputNode
10. Await cleanup calls

### Phase 3: Medium Priority (Quality)
11-16: Address remaining medium issues

### Phase 4: Test Recording
- Verify dual named pipes work correctly
- Check logs for proper startup sequence
- Verify audio/video sync

---

## Files to Modify

| File | Changes |
|------|---------|
| [FFmpegEncoderService.cs](ElgatoCapture/Services/FFmpegEncoderService.cs) | Volatile fields, disposal fix, null safety, FFmpeg exit detection |
| [CaptureService.cs](ElgatoCapture/Services/CaptureService.cs) | Audio callback safety, DeviceInputNode disposal, error propagation |
| [MainWindow.xaml.cs](ElgatoCapture/MainWindow.xaml.cs) | Frame handler refactor, await cleanup calls, frame rate limiting |
| [MainViewModel.cs](ElgatoCapture/ViewModels/MainViewModel.cs) | Fix preview auto-start race |

---

## Verification

1. Build with no errors
2. Start preview - should work without crashes
3. Start/stop recording multiple times rapidly - no deadlocks
4. Unplug device during recording - graceful error handling
5. Check log for "Recording audio graph started" and proper cleanup
6. Record 30+ seconds - verify audio/video sync in output

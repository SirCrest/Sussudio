# ElgatoCapture

Windows desktop capture application for Elgato devices with realtime preview and professional recording features.

## Project Vision

- **Realtime preview**: Video and audio passthrough with low latency
- **Hardware-driven settings**: Resolution/framerate from device capabilities
- **Flexible output**: Raw/lossless and compressed via FFmpeg
- **Professional UX**: Recording stats, disk space, bitrate monitoring

## Feature Summary

**Hardware-accelerated capture with guaranteed constant frame rate and broadcast-quality audio**

### What Makes It Special

✅ **NVIDIA NVENC encoding** - 10-20x faster than CPU with <10% usage
✅ **Guaranteed CFR output** - No VFR sync issues that plague other capture tools
✅ **Custom audio mixing** - Blend capture card audio with mic or system audio
✅ **Real-time monitoring** - OBS-style peak meters, live bitrate, and recording stats
✅ **360-frame buffer** - Absorbs encoder stalls without dropping frames

### Key Features

**Recording Formats**
- H.264 MP4 (broad compatibility)
- HEVC MP4 (40% smaller files)
- AV1 MP4 (best compression when supported)
- Uncompressed AVI (lossless editing)

**Quality Control**
- 7 presets: Low (8 Mbps) → Lossless (CRF 0)
- Custom bitrate: 1-300 Mbps
- Auto-scaling by resolution/framerate

**Audio**
- 48kHz stereo, AAC 320kbps
- Independent preview monitoring
- Peak meters with clip detection
- Custom input device selection

**Monitoring**
- Recording timer (HH:MM:SS)
- Live bitrate display
- Current file size
- Disk space tracking

### Why Choose This Over OBS/Streamlabs?

- **Pure capture focus** - No streaming bloat, just clean recording
- **CFR guarantee** - Works perfectly in Premiere/DaVinci/Final Cut
- **Lower overhead** - Dedicated tool beats general-purpose solutions
- **Professional audio** - 320kbps AAC vs typical 160kbps

### Requirements

- Windows 10 Build 17763+
- Compatible Elgato device (Neo, HD60 S+/X, 4K60 Pro/X/S)
- FFmpeg in PATH
- Optional: NVIDIA GPU (graceful CPU fallback)

## Tech Stack

- .NET 8.0 / WinUI 3 (Windows App SDK 1.8)
- MVVM with CommunityToolkit.Mvvm
- FFmpeg subprocess for CFR video encoding
- Windows 10 Build 17763+

## Completed Features

### ✅ NVENC Hardware Encoding (January 2026)
- Automatic NVIDIA NVENC detection (h264_nvenc, hevc_nvenc)
- 10-20x faster encoding vs CPU with <10% CPU usage
- Quality presets p1-p7 with VBR/CBR/lossless rate control
- 360-frame buffer for sustained realtime encoding
- Graceful CPU fallback when NVENC unavailable
- **Limitation**: HDR10 metadata requires libx265 (CPU)
- See: [multi-encoder.md](docs/plans/multi-encoder.md) for AMD/Intel GPU support

## Session Summary (2026-01-31)

- Switched MP4/HEVC audio to a post-mux pipeline (video-only encode, audio to WAV, then mux to AAC).
- Added custom audio input toggle + dropdown; preview and recording use the selected device.
- Implemented OBS-style peak meter (dBFS scaling, PPM decay) with color bands and clip indicator.
- Added preview UI stall diagnostics and drop-if-busy preview frames to reduce hover/resize blink.
- Refined layout and alignment (top bar, bottom controls, button sizing, output row).

## Supported Devices

Elgato capture cards with video+audio device matching:
- Game Capture Neo
- HD60 S+, HD60 X
- 4K60 Pro, 4K Pro, 4K X, 4K S

## Project Structure

```
ElgatoCapture/
  Models/
    CaptureDevice.cs        # Device with audio association
    AudioInputDevice.cs     # Custom audio input device model
    MediaFormat.cs          # Resolution, FPS, HDR specs
    CaptureSettings.cs      # Recording config + bitrate calc
  Services/
    DeviceService.cs        # Device enumeration, audio matching
    CaptureService.cs       # Capture pipeline, preview, AudioGraph
    FFmpegEncoderService.cs # FFmpeg subprocess
  ViewModels/
    MainViewModel.cs        # UI state & commands
  MainWindow.xaml(.cs)      # UI + preview frame handling
  Logger.cs                 # Debug log: ~/Documents/ElgatoCapture_Debug.log
```

## Pixel Format Support

| Format | Source | Handling |
|--------|--------|----------|
| YUY2 | Most Elgato devices | GPU->BGRA8 conversion |
| NV12 | Some devices | GPU->BGRA8 conversion |
| P010 | HDR devices (10-bit) | GPU->BGRA8 (loses HDR) |

**Pipeline:** Device -> Direct3DSurface (GPU) -> SoftwareBitmap (BGRA8) -> FFmpeg

## Recording Formats

| Format | Encoder | CFR | Use Case |
|--------|---------|-----|----------|
| H.264 MP4 | h264_nvenc (GPU) or libx264 (CPU) | Yes | Default, broad compatibility, fast |
| HEVC MP4 | hevc_nvenc (GPU) or libx265 (CPU) | Yes | 40% smaller, fast GPU encoding |
| AV1 MP4 | av1_nvenc (GPU) or libsvtav1/libaom-av1 (CPU) | Yes | Best compression, newest codec |
| Uncompressed AVI | rawvideo | Yes | Lossless workflows |

**Encoder selection**: Automatic NVENC detection with CPU fallback
**Quality presets**: Low (8Mbps) → Lossless (CRF 0/lossless mode) with resolution/framerate scaling

## Audio Architecture

- **Preview**: AudioGraph -> default output device (speakers), meter taps frame output
- **Recording (MP4/HEVC)**: AudioGraph -> float32 WAV (48kHz stereo) -> FFmpeg mux pass
- **Mux**: `-c:v copy -c:a aac -b:a 320k -movflags +faststart`
- **Recording (AVI)**: MediaCapture path (custom audio input not wired for AVI yet)
- **Custom audio input**: User-selectable audio capture device for preview + recording

## UI Features

### Implemented
- Device selector with refresh
- Resolution/framerate from device capabilities
- Format and quality selectors
- HDR toggle (device-aware)
- Audio preview toggle
- Audio record toggle + custom audio input selector
- Audio meter (peak, color bands, clip indicator)
- Recording timer (HH:MM:SS)
- Disk space display
- Recording indicator badge

### Planned
- Rolling bitrate average
- Current file size during recording
- True lossless codecs (FFV1)
- Dropped frame warning
- Headless test mode + scripted build/log checks

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
| No audio | Look for "Found audio device" in logs |
| VFR output | Ensure using FFmpeg encoder, not Media Foundation |
| Audio sync issues | Check audio mux logs and device selection |
| Preview flicker | Look for "Preview UI stall" log entries |
| UI freeze on close | Verify IAsyncDisposable is used, not blocking Dispose() |

**Log file**: `C:\Users\{username}\Documents\ElgatoCapture_Debug.log`

## Development Notes

- Primary test device: **Elgato Game Capture Neo**
- FFmpeg must be in PATH or application directory
- WinUI 3 quirk: No Width/Height in Window XAML - use `AppWindow.Resize()`
- Automation testing is currently reset; rebuild guidance lives in `docs/testing/README.md`







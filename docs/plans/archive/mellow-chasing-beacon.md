# Elgato Capture - WinUI 3 Video Capture Application

## Overview
Build a modern WinUI 3 desktop app for capturing video from Elgato UVC devices (4K S, 4K X, HD60 X) with support for SDR/HDR recording, including uncompressed raw capture.

## Requirements
- **Audio**: Capture both video and embedded audio from HDMI input
- **Uncompressed Format**: AVI container for raw video
- **Compressed Codecs**: H.264 and HEVC (H.265)

## Architecture

### Technology Stack
- **UI**: WinUI 3 with Windows App SDK 1.8 (already configured)
- **Video Capture**: Windows.Media.Capture (MediaCapture API)
- **Device Enumeration**: Windows.Devices.Enumeration
- **File I/O**: High-performance async file writing with large buffers

### Project Structure
```
ElgatoCapture/
├── App.xaml(.cs)
├── MainWindow.xaml(.cs)
├── Services/
│   ├── CaptureService.cs        # Video capture management
│   └── DeviceService.cs         # Device enumeration
├── Models/
│   ├── CaptureDevice.cs         # Device info model
│   ├── CaptureSettings.cs       # Recording settings model
│   └── MediaFormat.cs           # Format definitions
└── ViewModels/
    └── MainViewModel.cs         # MVVM view model
```

## Implementation Plan

### Phase 1: Core Infrastructure
1. **Add NuGet packages**:
   - CommunityToolkit.Mvvm (for MVVM pattern)

2. **Create DeviceService**:
   - Enumerate video capture devices via `DeviceInformation.FindAllAsync`
   - Filter for video capture devices
   - Query supported formats (resolutions, frame rates, pixel formats)
   - Detect HDR capability
   - Enumerate associated audio capture devices (HDMI audio input)

3. **Create Models**:
   - `CaptureDevice`: Name, Id, supported formats, HDR capable, audio capabilities
   - `CaptureSettings`: Resolution, FPS, format, HDR mode, output path, audio enabled
   - `MediaFormat`: Width, Height, FrameRate, PixelFormat

### Phase 2: Video Preview
4. **Configure MediaCapture**:
   - Initialize with selected device
   - Set up video preview stream
   - Handle HDR format negotiation

5. **Preview UI**:
   - Add `MediaPlayerElement` or `SwapChainPanel` for video preview
   - Aspect ratio handling

### Phase 3: Recording
6. **Create CaptureService**:
   - Start/stop recording
   - Support multiple recording modes:
     - **H.264**: Standard compressed via MediaEncodingProfile
     - **HEVC**: High-efficiency compressed for 4K/HDR
     - **Uncompressed AVI**: Raw frame capture to AVI container
   - Audio capture from device (embedded HDMI audio)
   - HDR metadata preservation

7. **File Writing**:
   - For compressed (H.264/HEVC): Use `MediaCapture.StartRecordToStorageFileAsync` with MP4 container
   - For uncompressed: Use `MediaFrameReader` for raw frames, write to AVI with audio track
   - Audio: AAC for compressed, PCM for uncompressed AVI

### Phase 4: UI Implementation
8. **MainWindow Layout**:
   ```
   ┌─────────────────────────────────────────────────────┐
   │  Device: [Dropdown▼]                                │
   ├─────────────────────────────────────────────────────┤
   │                                                     │
   │                                                     │
   │              Video Preview Area                     │
   │                                                     │
   │                                                     │
   ├─────────────────────────────────────────────────────┤
   │  Resolution: [▼]  FPS: [▼]  Format: [▼]  HDR: [○]  │
   │  Output: [Path...]                    [● RECORD]   │
   │  Status: Ready                        00:00:00     │
   └─────────────────────────────────────────────────────┘
   ```

9. **Controls**:
   - ComboBox for device selection
   - ComboBox for resolution (populated from device capabilities)
   - ComboBox for FPS (24, 30, 60, etc.)
   - ComboBox for format (H.264 MP4, HEVC MP4, Uncompressed AVI)
   - ToggleSwitch for HDR mode
   - Button for output folder selection
   - Large Record button with visual feedback
   - Recording timer display

### Phase 5: Polish
10. **Enhancements**:
    - Recording indicator animation
    - Disk space monitoring
    - Error handling with user-friendly messages
    - Settings persistence

## Key Technical Considerations

### HDR Support
- Query `MediaCaptureVideoProfile` for HDR profiles
- Use `Hdr10` or `HLG` video encoding profiles
- P010 pixel format (10-bit YUV 4:2:0)
- Preserve BT.2020 color space and ST.2084 transfer function

### Uncompressed Recording
- Use `MediaFrameReader` to get raw frames
- `SoftwareBitmap` or `Direct3DSurface` for frame data
- Write frames to AVI container with uncompressed video
- Include PCM audio track
- Use large write buffers and async I/O for sustained throughput

### Performance
- Use `DispatcherQueue` for UI updates
- Async file I/O with large buffers (4MB+)
- Consider memory-mapped files for highest throughput
- Monitor frame drops

## Files to Create/Modify

| File | Action |
|------|--------|
| `ElgatoCapture.csproj` | Add CommunityToolkit.Mvvm package |
| `Services/DeviceService.cs` | Create - device enumeration |
| `Services/CaptureService.cs` | Create - capture management |
| `Models/CaptureDevice.cs` | Create - device model |
| `Models/CaptureSettings.cs` | Create - settings model |
| `Models/MediaFormat.cs` | Create - format model |
| `ViewModels/MainViewModel.cs` | Create - main view model |
| `MainWindow.xaml` | Modify - add full UI |
| `MainWindow.xaml.cs` | Modify - wire up view model |

## Verification
1. Build and run the application
2. Verify Elgato devices appear in device dropdown
3. Verify preview shows live video
4. Test recording in compressed mode (H.264)
5. Test recording in uncompressed mode
6. Verify HDR toggle works with HDR-capable devices
7. Verify files are written correctly and playable

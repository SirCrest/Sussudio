# HDR10 (High Dynamic Range) Capture and Encoding Implementation Plan

## Research Summary

### Elgato HDR Capabilities

**Elgato 4K X ([specs](https://help.elgato.com/hc/en-us/articles/23658118721421)):**
- ✅ **P010 format support** at 4K30 HDR (4:2:0 P010 10-bit)
- ✅ HDR10 capture at 1440p60 and 4K30
- ✅ Requires NVIDIA 10 series+ GPU for HDR recording on Windows
- ❌ macOS: No P010 support (OS limitation)

**Elgato 4K S ([specs](https://www.elgato.com/us/en/explorer/products/capture/elgato-game-capture-4k-s-technical-specifications/)):**
- ✅ HDR10 capture up to 1080p60 (Windows only)
- ✅ HDR10 passthrough up to 4K60
- ✅ HDMI 2.0 connectivity

**Critical finding:** P010 is the required pixel format for HDR capture on Windows ([source](https://obsproject.com/forum/threads/elgato-4k60-pro-mk-2-p010-video-format-missing-despite-function-hdr-capture.189198/)).

### Windows MediaCapture P010 Support

**From Microsoft docs ([VideoMediaFrame.Direct3DSurface](https://learn.microsoft.com/en-us/uwp/api/windows.media.capture.frames.videomediaframe.direct3dsurface)):**
- ✅ P010 format available via Direct3DSurface (GPU memory)
- ⚠️ **SoftwareBitmap conversion may fail** for P010 due to format compatibility
- ✅ Direct memory access to P010 data via Direct3D11 interop possible
- ✅ Can access raw P010 bytes from GPU surface

### FFmpeg P010 HDR Encoding

**P010 Input ([FFmpeg pixel formats](https://ffmpeg.org/ffmpeg-formats.html)):**
- ✅ FFmpeg supports `-pix_fmt p010le` for 10-bit YUV 4:2:0 input
- ✅ P010 is 2 bytes per component (10-bit + 6-bit padding in LSBs)

**HDR10 Metadata ([Code Calamity guide](https://codecalamity.com/encoding-uhd-4k-hdr10-videos-with-ffmpeg/)):**
```bash
-color_primaries bt2020 \
-color_trc smpte2084 \
-colorspace bt2020nc \
-x265-params "master-display=G(13250,34500)B(7500,3000)R(34000,16000)WP(15635,16450)L(10000000,1):max-cll=1000,400:hdr-opt=1"
```

**CRITICAL LIMITATION:** NVENC does NOT support HDR10 metadata ([source](https://www.voukoder.org/forum/thread/487-hdr-support-in-x265-and-nvenc-hevc/)). Must use **libx265 (CPU)** for proper HDR10 compliance.

### OBS HDR Workflow

[OBS HDR capture guide](https://www.elgato.com/us/en/explorer/products/capture/record-hdr-gameplay-with-obs-studio-and-elgato/):
- Set Elgato source to P010 10-bit, Rec 2100 PQ
- Enable HDR mode in OBS with P010 10-bit
- Encode with Main 10-profile HEVC
- Adjust SDR preview tonemapping for monitoring

---

## Implementation Plan

### Phase 1: P010 Capture Pipeline

#### Goal
Capture P010 10-bit HDR frames from Elgato 4K X/S and convert to raw P010 bytes for FFmpeg.

#### Current Pipeline (SDR)
```
Elgato → YUY2/NV12 Direct3DSurface → SoftwareBitmap (BGRA8) → FFmpeg stdin (bgra)
```

#### New Pipeline (HDR)
```
Elgato → P010 Direct3DSurface → Raw P010 bytes (GPU memory copy) → FFmpeg stdin (p010le)
```

#### Changes Required

**File: [CaptureService.cs](ElgatoCapture/Services/CaptureService.cs)**

**Change 1: Detect HDR format (around line 115)**

```csharp
private async Task<bool> InitializeAsync(CaptureDevice device, int width, int height, int frameRate, bool enableAudio)
{
    // ... existing code ...

    // Find matching format
    foreach (var format in formats)
    {
        if (format.Width == width && format.Height == height &&
            Math.Abs(frameRate - format.FrameRate) < 0.1)
        {
            matchingFormat = format;

            // Check for HDR (P010 format)
            var subtype = format.VideoFormat.Subtype;
            bool isHdr = subtype == MediaEncodingSubtypes.P010;

            Logger.Log($"✓ Found matching format: {width}x{height}@{frameRate}fps ({subtype})");
            if (isHdr)
            {
                Logger.Log("⚠️ HDR format detected (P010 10-bit)");
            }

            break;
        }
    }
}
```

**Change 2: Add P010→Raw conversion method**

```csharp
private async Task<byte[]?> ConvertP010ToRawAsync(Direct3D11CaptureFrame frame)
{
    try
    {
        var surface = frame.Surface;
        if (surface == null) return null;

        // Access Direct3D11 surface
        var access = surface as Windows.Graphics.DirectX.Direct3D11.IDirect3DDxgiInterfaceAccess;
        if (access == null) return null;

        // Get IDXGISurface from Direct3DSurface
        var pUnknown = access.GetInterface(typeof(SharpDX.DXGI.Surface).GUID);
        var dxgiSurface = new SharpDX.DXGI.Surface(pUnknown);

        // Get surface description
        var desc = dxgiSurface.Description;

        // P010 format: 2 bytes per Y sample, 2 bytes per UV pair
        // For 1920x1080: Y plane = 1920*1080*2, UV plane = 1920*1080 (interleaved)
        int yPlaneSize = desc.Width * desc.Height * 2;  // 10-bit Y
        int uvPlaneSize = desc.Width * desc.Height;      // 10-bit UV (packed)
        int totalSize = yPlaneSize + uvPlaneSize;

        byte[] buffer = new byte[totalSize];

        // Lock surface for CPU access
        var dataBox = dxgiSurface.Map(SharpDX.DXGI.MapFlags.Read);
        try
        {
            // Copy Y plane
            unsafe
            {
                fixed (byte* pDest = buffer)
                {
                    // Copy Y plane (full width * height * 2 bytes)
                    for (int row = 0; row < desc.Height; row++)
                    {
                        var srcPtr = (IntPtr)dataBox.DataPointer + row * dataBox.Pitch;
                        var dstPtr = (IntPtr)pDest + row * desc.Width * 2;
                        Buffer.MemoryCopy(srcPtr.ToPointer(), dstPtr.ToPointer(),
                                         desc.Width * 2, desc.Width * 2);
                    }

                    // Copy UV plane (starts after Y plane in P010)
                    int uvHeight = desc.Height / 2;  // 4:2:0 subsampling
                    for (int row = 0; row < uvHeight; row++)
                    {
                        var srcPtr = (IntPtr)dataBox.DataPointer + (desc.Height + row) * dataBox.Pitch;
                        var dstPtr = (IntPtr)pDest + yPlaneSize + row * desc.Width * 2;
                        Buffer.MemoryCopy(srcPtr.ToPointer(), dstPtr.ToPointer(),
                                         desc.Width * 2, desc.Width * 2);
                    }
                }
            }
        }
        finally
        {
            dxgiSurface.Unmap();
        }

        dxgiSurface.Dispose();
        return buffer;
    }
    catch (Exception ex)
    {
        Logger.Log($"Error converting P010 frame: {ex.Message}");
        return null;
    }
}
```

**Change 3: Modify recording frame callback (around line 385)**

```csharp
private async void RecordingFrameReader_FrameArrived(MediaFrameReader sender, MediaFrameArrivedEventArgs args)
{
    using var frame = sender.TryAcquireLatestFrame();
    if (frame?.VideoMediaFrame == null) return;

    // Check if HDR format (P010)
    var subtype = frame.VideoMediaFrame.VideoFormat.MediaFrameFormat.Subtype;
    bool isP010 = subtype == MediaEncodingSubtypes.P010;

    if (isP010)
    {
        // HDR path: Convert P010 Direct3DSurface to raw bytes
        var rawP010 = await ConvertP010ToRawAsync(frame.VideoMediaFrame.Direct3DSurface);
        if (rawP010 != null)
        {
            // Enqueue to FFmpeg (will be sent as p010le)
            _ffmpegEncoder?.EnqueueP010Frame(rawP010);
        }
    }
    else
    {
        // SDR path: existing YUY2/NV12 → BGRA8 conversion
        var softwareBitmap = await SoftwareBitmap.CreateCopyFromSurfaceAsync(
            frame.VideoMediaFrame.Direct3DSurface,
            BitmapAlphaMode.Ignore);

        if (softwareBitmap.BitmapPixelFormat != BitmapPixelFormat.Bgra8)
        {
            softwareBitmap = SoftwareBitmap.Convert(softwareBitmap, BitmapPixelFormat.Bgra8);
        }

        _ffmpegEncoder?.EnqueueVideoFrame(softwareBitmap);
    }
}
```

**Dependencies:**
- Add NuGet package: `SharpDX.Direct3D11` for Direct3D11 interop
- Add NuGet package: `SharpDX.DXGI` for DXGI surface access

---

### Phase 2: FFmpeg P010 HDR Encoding

#### Goal
Encode P010 frames with proper HDR10 metadata using libx265.

#### Changes Required

**File: [FFmpegEncoderService.cs](ElgatoCapture/Services/FFmpegEncoderService.cs)**

**Change 1: Add HDR support fields (around line 40)**

```csharp
private bool _isHdrCapture = false;
private BlockingCollection<byte[]>? _p010FrameQueue;  // Separate queue for P010 frames
private const int MaxP010QueueSize = 240;  // P010 frames are 50% larger than BGRA8
```

**Change 2: Add P010 enqueue method**

```csharp
public void EnqueueP010Frame(byte[] p010Data)
{
    if (_p010FrameQueue == null) return;

    if (!_p010FrameQueue.TryAdd(p010Data, VideoQueueWaitMs))
    {
        Interlocked.Increment(ref _droppedFrameCount);
        Logger.Log("Dropped P010 video frame (queue full)");
    }
}
```

**Change 3: Update BuildFFmpegArguments for HDR (around line 260)**

```csharp
private string BuildFFmpegArguments(CaptureSettings settings, string outputPath,
                                   string frameRateArg, string? audioPipeName)
{
    var (codec, qualityArgs) = GetEncoderSettings(settings);

    // Input pixel format
    string pixelFormat = _isHdrCapture ? "p010le" : "bgra";
    string inputColorArgs = "";

    if (_isHdrCapture)
    {
        // HDR input color space
        inputColorArgs = "-color_primaries bt2020 -color_trc smpte2084 -colorspace bt2020nc";
    }

    var args = new StringBuilder();
    args.Append($"-y -probesize 32 -analyzeduration 0 ");
    args.Append($"-f rawvideo -pixel_format {pixelFormat} ");
    args.Append($"-video_size {settings.Width}x{settings.Height} ");
    args.Append($"-framerate {frameRateArg} ");
    args.Append($"{inputColorArgs} ");  // HDR color space for input
    args.Append($"-thread_queue_size 512 -i pipe:0 ");

    // Audio input (if present)
    if (audioPipeName != null)
    {
        args.Append($"-f s16le -ar 48000 -ac 2 ");
        args.Append($"-thread_queue_size 1024 -i \\\\.\\pipe\\{audioPipeName} ");
    }

    // Video encoder
    args.Append($"-c:v {codec} {qualityArgs} ");

    // HDR metadata (HEVC only)
    if (_isHdrCapture && codec.Contains("265"))
    {
        args.Append($"-r {frameRateArg} -pix_fmt yuv420p10le ");
        args.Append($"-color_primaries bt2020 -color_trc smpte2084 -colorspace bt2020nc ");

        // HDR10 metadata (typical values for gaming HDR)
        args.Append($"-x265-params \"");
        args.Append($"colorprim=bt2020:");
        args.Append($"transfer=smpte2084:");
        args.Append($"colormatrix=bt2020nc:");
        args.Append($"master-display=G(13250,34500)B(7500,3000)R(34000,16000)WP(15635,16450)L(10000000,1):");
        args.Append($"max-cll=1000,400:");
        args.Append($"hdr-opt=1");
        args.Append($"\" ");
    }
    else if (!_isHdrCapture)
    {
        // SDR output
        args.Append($"-r {frameRateArg} -pix_fmt yuv420p ");
    }

    // Audio encoder
    if (audioPipeName != null)
    {
        args.Append($"-c:a aac -b:a 192k ");
    }

    // Container options
    args.Append($"-shortest -movflags +faststart \"{outputPath}\"");

    return args.ToString();
}
```

**Change 4: Update WriteVideoFramesAsync for P010 (around line 470)**

```csharp
private async Task WriteVideoFramesAsync(CancellationToken ct)
{
    try
    {
        Logger.Log("Video writer task started");

        var queue = _isHdrCapture ? _p010FrameQueue : _frameQueue;

        foreach (var frameData in queue!.GetConsumingEnumerable(ct))
        {
            try
            {
                await _videoStream!.WriteAsync(frameData, 0, frameData.Length, ct);
                Interlocked.Increment(ref _encodedFrameCount);

                // Return buffer to pool (only for BGRA8 frames, P010 uses different memory)
                if (!_isHdrCapture && _frameBufferPool.Count < MaxPoolSize)
                {
                    _frameBufferPool.Add(frameData);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        Logger.Log($"Video writer task ended. Frames encoded: {_encodedFrameCount}");
    }
    catch (Exception ex)
    {
        Logger.Log($"Error in video writer task: {ex.Message}");
    }
}
```

**Change 5: Update GetEncoderSettings for HDR (replace existing method)**

```csharp
private (string codec, string qualityArgs) GetEncoderSettings(CaptureSettings settings)
{
    string codec;
    string qualityArgs;

    // HDR REQUIRES libx265 (NVENC doesn't support HDR10 metadata)
    bool forceLibx265 = _isHdrCapture;
    bool useNvenc = _nvencAvailable && !forceLibx265;

    switch (settings.Format)
    {
        case RecordingFormat.HevcMp4:
            codec = useNvenc ? "hevc_nvenc" : "libx265";
            break;
        case RecordingFormat.UncompressedAvi:
            codec = useNvenc ? "h264_nvenc" : "libx264";
            break;
        default: // H264Mp4
            codec = useNvenc ? "h264_nvenc" : "libx264";
            break;
    }

    // Log HDR encoding notice
    if (_isHdrCapture)
    {
        Logger.Log($"HDR encoding active: using {codec} (NVENC unavailable for HDR10 metadata)");
    }

    // Quality settings (existing logic)
    if (settings.Quality == VideoQuality.Custom)
    {
        var bitrate = (int)(settings.CustomBitrateMbps * 1000);

        if (useNvenc)
        {
            qualityArgs = $"-preset p4 -rc cbr -b:v {bitrate}k -maxrate {bitrate}k -bufsize {bitrate * 2}k -profile:v high -bf 3";
        }
        else
        {
            qualityArgs = $"-b:v {bitrate}k -maxrate {bitrate}k -bufsize {bitrate * 2}k";
        }
    }
    else
    {
        var qualityValue = settings.Quality switch
        {
            VideoQuality.Low => 28,
            VideoQuality.Medium => 23,
            VideoQuality.High => 18,
            VideoQuality.VeryHigh => 15,
            VideoQuality.Lossless => 0,
            _ => 23
        };

        string preset = settings.Quality switch
        {
            VideoQuality.Lossless => useNvenc ? "p7" : "slow",
            VideoQuality.VeryHigh => useNvenc ? "p6" : "medium",
            VideoQuality.High => useNvenc ? "p5" : "medium",
            _ => useNvenc ? "p4" : "medium"
        };

        if (useNvenc)
        {
            if (settings.Quality == VideoQuality.Lossless)
            {
                qualityArgs = $"-preset {preset} -rc lossless -profile:v high -bf 3";
            }
            else
            {
                qualityArgs = $"-preset {preset} -rc vbr -cq {qualityValue} -b:v 0 -profile:v high -bf 3";
            }
        }
        else
        {
            if (settings.Quality == VideoQuality.Lossless)
            {
                qualityArgs = $"-preset {preset} -crf 0";
            }
            else
            {
                qualityArgs = $"-preset {preset} -crf {qualityValue}";
            }
        }
    }

    return (codec, qualityArgs);
}
```

---

### Phase 3: HDR Metadata Extraction

#### Goal
Extract actual HDR metadata from the capture device stream instead of using hardcoded values.

#### Implementation

**File: [CaptureService.cs](ElgatoCapture/Services/CaptureService.cs)**

**Add method to extract HDR metadata:**

```csharp
private (string? masterDisplay, string? maxCll) ExtractHdrMetadata(VideoMediaFrame frame)
{
    try
    {
        // Access extended frame properties
        var properties = frame.VideoFormat.Properties;

        // Try to get HDR metadata from stream
        // Note: This is device-dependent and may not be available
        // Fallback to typical HDR10 values if not present

        string? masterDisplay = null;
        string? maxCll = null;

        // Attempt to read mastering display color volume
        if (properties.TryGetValue(new Guid("{C380465D-2271-428C-9B83-ECEA3B4A85C1}"), out var mdcvObj))
        {
            // Parse MDCV data if available
            // Format: R(x,y) G(x,y) B(x,y) WP(x,y) L(max,min)
            masterDisplay = mdcvObj?.ToString();
        }

        // Attempt to read content light level
        if (properties.TryGetValue(new Guid("{1DB47C00-3F00-4103-9F26-2C9A8B09A9AE}"), out var cllObj))
        {
            // Parse CLL data if available
            // Format: MaxCLL,MaxFALL
            maxCll = cllObj?.ToString();
        }

        // Fallback to typical HDR10 values
        if (masterDisplay == null)
        {
            // Typical HDR10 mastering display (DCI-P3 D65 primaries)
            masterDisplay = "G(13250,34500)B(7500,3000)R(34000,16000)WP(15635,16450)L(10000000,1)";
        }

        if (maxCll == null)
        {
            // Typical gaming HDR: 1000 nits peak, 400 nits average
            maxCll = "1000,400";
        }

        Logger.Log($"HDR metadata: master-display={masterDisplay}, max-cll={maxCll}");
        return (masterDisplay, maxCll);
    }
    catch (Exception ex)
    {
        Logger.Log($"Error extracting HDR metadata: {ex.Message}");
        return (null, null);
    }
}
```

---

### Phase 4: UI Integration

#### Goal
Add HDR toggle and status indicators to the UI.

#### Changes Required

**File: [MainViewModel.cs](ElgatoCapture/ViewModels/MainViewModel.cs)**

**Add HDR properties:**

```csharp
[ObservableProperty]
private bool _isHdrSupported;

[ObservableProperty]
private bool _isHdrEnabled;

partial void OnIsHdrEnabledChanged(bool value)
{
    if (value && !IsHdrSupported)
    {
        IsHdrEnabled = false;
        // Show error: HDR not supported by current device
    }
}
```

**Update device selection to check HDR support:**

```csharp
partial void OnSelectedDeviceChanged(CaptureDevice? value)
{
    if (value != null)
    {
        // ... existing code ...

        // Check if device supports HDR
        IsHdrSupported = CheckDeviceHdrSupport(value);

        if (!IsHdrSupported)
        {
            IsHdrEnabled = false;
        }
    }
}

private bool CheckDeviceHdrSupport(CaptureDevice device)
{
    // Check if device supports P010 format
    // Implementation depends on device capabilities
    // For now, hardcode supported devices
    var supportedDevices = new[] { "4K X", "4K60 Pro", "4K S" };
    return supportedDevices.Any(d => device.Name.Contains(d, StringComparison.OrdinalIgnoreCase));
}
```

**File: [MainWindow.xaml](ElgatoCapture/MainWindow.xaml)**

**Add HDR toggle (around line 85, near HDR toggle):**

```xaml
<CheckBox Content="Enable HDR10"
          IsChecked="{x:Bind ViewModel.IsHdrEnabled, Mode=TwoWay}"
          IsEnabled="{x:Bind ViewModel.IsHdrSupported, Mode=OneWay}"
          ToolTipService.ToolTip="Enable HDR10 capture (P010 10-bit, HEVC only)"
          Margin="0,8,0,0"/>

<TextBlock Text="⚠️ HDR requires HEVC format and CPU encoding (slower)"
           Foreground="Orange"
           FontSize="11"
           Visibility="{x:Bind ViewModel.IsHdrEnabled, Mode=OneWay}"
           Margin="0,4,0,0"/>
```

---

## Verification Plan

### Test 1: P010 Detection
1. Connect Elgato 4K X with HDR source (PS5/Xbox set to HDR mode)
2. Start application
3. Check log for: `"HDR format detected (P010 10-bit)"`
4. Verify: IsHdrSupported = true in UI

### Test 2: P010 Frame Capture
1. Enable HDR toggle in UI
2. Select HEVC format
3. Start 10-second recording
4. Check log for:
   - `"HDR encoding active: using libx265"`
   - `"HDR metadata: master-display=..."`
   - FFmpeg args show `-pixel_format p010le`
   - Expected frames: 600 (10s × 60fps)
   - Frame loss: <1%

### Test 3: HDR Metadata Validation
1. Record 30-second HDR clip
2. Analyze with MediaInfo:
   ```bash
   mediainfo output.mp4
   ```
3. Verify output shows:
   - **Bit depth:** 10 bits
   - **Color primaries:** BT.2020
   - **Transfer characteristics:** PQ (SMPTE ST 2084)
   - **Matrix coefficients:** BT.2020 non-constant
   - **Mastering display color primaries:** Present
   - **Maximum Content Light Level:** Present

### Test 4: HDR Playback
1. Play recording in HDR-capable player (VLC, MPC-HC with madVR)
2. Verify:
   - Video displays with wide color gamut
   - Highlights are brighter than SDR
   - Colors are more vibrant
   - No banding in gradients

### Test 5: Performance
**Expected encoding speed with libx265:**
- 1080p60 HDR: 0.5-1.0x realtime (slower than NVENC)
- CPU usage: 50-80% (vs <10% with NVENC)
- RAM usage: ~3.5GB (P010 frames are larger)

**Acceptable frame loss:**
- <2% (HDR encoding is CPU-intensive)

### Test 6: Fallback to SDR
1. Disable HDR toggle
2. Start recording
3. Verify:
   - Encoding uses BGRA8 input
   - NVENC is used if available
   - No HDR metadata in output
   - Encoding speed returns to 3-10x

---

## Trade-offs and Limitations

### Pros
✅ **Professional HDR10 compliance** - Proper BT.2020, PQ, metadata
✅ **Wide color gamut** - Captures full HDR color range
✅ **10-bit precision** - No banding in gradients
✅ **Future-proof** - HDR is industry standard for gaming/content

### Cons
❌ **CPU encoding required** - NVENC doesn't support HDR10 metadata
❌ **Slower encoding** - 0.5-1.0x realtime vs 10-20x with NVENC
❌ **Higher CPU usage** - 50-80% vs <10% with NVENC
❌ **Larger files** - P010 input is 50% larger than BGRA8
❌ **Windows only** - macOS doesn't support P010 capture
❌ **HEVC required** - H.264 doesn't support 10-bit officially

### Mitigation Strategies

**For encoding speed:**
- Use x265 preset "medium" or "fast" (not "slow"/"slower")
- Lower CRF to 20-22 for High quality (vs 18)
- Accept 1-2% frame loss as normal for HDR

**For file size:**
- HEVC compression is 40% better than H.264
- 10-bit often compresses better than 8-bit (less banding = better compression)

**For CPU usage:**
- HDR recordings are special use case (not every recording)
- User can toggle HDR off for regular SDR recordings with NVENC

---

## Implementation Priority

**Phase 1: P010 Capture** - CRITICAL
- Must work before anything else

**Phase 2: FFmpeg Encoding** - CRITICAL
- Core HDR10 output functionality

**Phase 3: Metadata Extraction** - OPTIONAL
- Can use hardcoded typical values initially
- Implement dynamic extraction later

**Phase 4: UI Integration** - IMPORTANT
- Needed for user control
- Can start with basic toggle

---

## Sources

- [Elgato 4K X HDR specifications](https://help.elgato.com/hc/en-us/articles/23658118721421-Elgato-Game-Capture-4K-X-Technical-Specifications)
- [Elgato 4K S specifications](https://www.elgato.com/us/en/explorer/products/capture/elgato-game-capture-4k-s-technical-specifications/)
- [OBS HDR capture with P010 format](https://obsproject.com/forum/threads/elgato-4k60-pro-mk-2-p010-video-format-missing-despite-function-hdr-capture.189198/)
- [OBS HDR workflow guide](https://www.elgato.com/us/en/explorer/products/capture/record-hdr-gameplay-with-obs-studio-and-elgato/)
- [FFmpeg HDR10 encoding guide](https://codecalamity.com/encoding-uhd-4k-hdr10-videos-with-ffmpeg/)
- [NVENC HDR limitations](https://www.voukoder.org/forum/thread/487-hdr-support-in-x265-and-nvenc-hevc/)
- [Windows MediaCapture Direct3DSurface](https://learn.microsoft.com/en-us/uwp/api/windows.media.capture.frames.videomediaframe.direct3dsurface)
- [FFmpeg P010 pixel format](https://ffmpeg.org/pipermail/ffmpeg-devel/2016-January/186714.html)

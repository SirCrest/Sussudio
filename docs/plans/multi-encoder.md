# Multi-Encoder Support Plan

## Goal
Allow users to select between CPU and GPU-accelerated encoders based on available hardware:
- **CPU**: libx264, libx265 (software encoding)
- **NVIDIA**: h264_nvenc, hevc_nvenc (NVENC)
- **AMD**: h264_amf, hevc_amf (AMF - Advanced Media Framework)
- **Intel**: h264_qsv, hevc_qsv (Quick Sync Video)

---

## Current State

### Implemented (as of NVENC PR)
- ✅ CPU encoding: libx264, libx265
- ✅ NVENC detection and automatic fallback
- ✅ Quality preset mapping for CPU and NVENC
- ✅ Rate control modes: VBR, CBR, lossless

### Architecture
**File: [FFmpegEncoderService.cs](ElgatoCapture/Services/FFmpegEncoderService.cs)**
- Line 40: `private bool _nvencAvailable = false;`
- Lines 133-175: `CheckNvencAvailabilityAsync()` - probes `ffmpeg -encoders`
- Lines 354-408: `GetEncoderSettings()` - selects codec and builds quality args

---

## Encoder Comparison

| Encoder | Speed | Quality | Platform | HDR10 | Notes |
|---------|-------|---------|----------|-------|-------|
| **libx264** | 0.5-2.0x | Excellent | Any CPU | ❌ 8-bit only | Industry standard, best quality/size |
| **libx265** | 0.3-1.0x | Excellent | Any CPU | ✅ Full support | Required for HDR, slower |
| **h264_nvenc** | 5-20x | Very Good | NVIDIA GPU | ❌ 8-bit only | Fast, low CPU usage |
| **hevc_nvenc** | 5-20x | Very Good | NVIDIA GPU | ⚠️ No metadata | Fast, but missing HDR10 SEI |
| **h264_amf** | 5-15x | Good | AMD GPU | ❌ 8-bit only | Fast, AMD alternative |
| **hevc_amf** | 5-15x | Good | AMD GPU | ⚠️ No metadata | Fast, but missing HDR10 SEI |
| **h264_qsv** | 3-10x | Good | Intel iGPU/CPU | ❌ 8-bit only | Requires Intel hardware |
| **hevc_qsv** | 3-10x | Good | Intel iGPU/CPU | ⚠️ Limited | 10-bit possible, metadata unclear |

**Key Finding**: Only **libx265** properly supports HDR10 metadata (master-display, max-cll). GPU encoders lack SEI message support.

---

## Implementation Plan

### Phase 1: Multi-Encoder Detection

**Goal**: Detect all available encoders on startup and store capabilities.

#### Changes Required

**File: [FFmpegEncoderService.cs](ElgatoCapture/Services/FFmpegEncoderService.cs)**

**Change 1: Add encoder availability fields (around line 40)**

```csharp
// Encoder availability flags
private bool _nvencAvailable = false;
private bool _amfAvailable = false;
private bool _qsvAvailable = false;
```

**Change 2: Replace CheckNvencAvailabilityAsync with comprehensive detection (lines 133-175)**

```csharp
private async Task DetectAvailableEncodersAsync()
{
    try
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = _ffmpegPath,
            Arguments = "-hide_banner -encoders",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo);
        if (process == null)
        {
            Logger.Log("Failed to probe FFmpeg encoders");
            return;
        }

        var output = await process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync();

        // Detect NVIDIA NVENC
        bool hasH264Nvenc = output.Contains("h264_nvenc");
        bool hasHevcNvenc = output.Contains("hevc_nvenc");
        _nvencAvailable = hasH264Nvenc || hasHevcNvenc;

        // Detect AMD AMF
        bool hasH264Amf = output.Contains("h264_amf");
        bool hasHevcAmf = output.Contains("hevc_amf");
        _amfAvailable = hasH264Amf || hasHevcAmf;

        // Detect Intel Quick Sync
        bool hasH264Qsv = output.Contains("h264_qsv");
        bool hasHevcQsv = output.Contains("hevc_qsv");
        _qsvAvailable = hasH264Qsv || hasHevcQsv;

        // Log results
        Logger.Log("=== Hardware Encoder Detection ===");
        if (_nvencAvailable)
            Logger.Log($"✓ NVIDIA NVENC (H.264: {hasH264Nvenc}, HEVC: {hasHevcNvenc})");
        if (_amfAvailable)
            Logger.Log($"✓ AMD AMF (H.264: {hasH264Amf}, HEVC: {hasHevcAmf})");
        if (_qsvAvailable)
            Logger.Log($"✓ Intel Quick Sync (H.264: {hasH264Qsv}, HEVC: {hasHevcQsv})");
        if (!_nvencAvailable && !_amfAvailable && !_qsvAvailable)
            Logger.Log("⚠️ No hardware encoders available, will use CPU encoding");
    }
    catch (Exception ex)
    {
        Logger.Log($"Encoder detection failed: {ex.Message}");
    }
}
```

**Change 3: Update StartAsync to call new detection method (around line 120)**

```csharp
public async Task<bool> StartAsync(CaptureSettings settings, string outputPath, string? audioPipeName = null)
{
    // ... existing code ...

    // Detect available encoders
    await DetectAvailableEncodersAsync();

    // ... rest of method ...
}
```

---

### Phase 2: Encoder Selection Model

**Goal**: Add user-selectable encoder preference to settings.

#### Changes Required

**File: [Models/CaptureSettings.cs](ElgatoCapture/Models/CaptureSettings.cs)**

**Add new enum and property:**

```csharp
public enum EncoderType
{
    Auto,       // Best available: NVENC > AMF > QSV > CPU
    CPU,        // Force software encoding
    NVENC,      // Force NVIDIA (if available)
    AMF,        // Force AMD (if available)
    QSV         // Force Intel Quick Sync (if available)
}

public EncoderType Encoder { get; set; } = EncoderType.Auto;
```

**File: [ViewModels/MainViewModel.cs](ElgatoCapture/ViewModels/MainViewModel.cs)**

**Add encoder selection property:**

```csharp
[ObservableProperty]
private EncoderType _selectedEncoder = EncoderType.Auto;

partial void OnSelectedEncoderChanged(EncoderType value)
{
    // Update settings
    CaptureSettings.Encoder = value;
}
```

**Add encoder availability properties (for UI binding):**

```csharp
[ObservableProperty]
private bool _isNvencAvailable;

[ObservableProperty]
private bool _isAmfAvailable;

[ObservableProperty]
private bool _isQsvAvailable;
```

---

### Phase 3: Codec Selection Logic

**Goal**: Select appropriate codec based on user preference and availability.

#### Changes Required

**File: [FFmpegEncoderService.cs](ElgatoCapture/Services/FFmpegEncoderService.cs)**

**Change 1: Add method to determine active encoder (new method)**

```csharp
private string GetActiveEncoder(CaptureSettings settings)
{
    // HDR mode ALWAYS uses libx265 (GPU encoders lack HDR10 metadata)
    if (_isHdrCapture)
    {
        Logger.Log("HDR mode active: forcing libx265 (GPU encoders lack HDR10 metadata support)");
        return "CPU";
    }

    // Handle user preference
    switch (settings.Encoder)
    {
        case EncoderType.CPU:
            Logger.Log("Encoder: CPU (user selected)");
            return "CPU";

        case EncoderType.NVENC:
            if (_nvencAvailable)
            {
                Logger.Log("Encoder: NVIDIA NVENC (user selected)");
                return "NVENC";
            }
            Logger.Log("⚠️ NVENC not available, falling back to CPU");
            return "CPU";

        case EncoderType.AMF:
            if (_amfAvailable)
            {
                Logger.Log("Encoder: AMD AMF (user selected)");
                return "AMF";
            }
            Logger.Log("⚠️ AMF not available, falling back to CPU");
            return "CPU";

        case EncoderType.QSV:
            if (_qsvAvailable)
            {
                Logger.Log("Encoder: Intel Quick Sync (user selected)");
                return "QSV";
            }
            Logger.Log("⚠️ Quick Sync not available, falling back to CPU");
            return "CPU";

        case EncoderType.Auto:
        default:
            // Auto-select best available: NVENC > AMF > QSV > CPU
            if (_nvencAvailable)
            {
                Logger.Log("Encoder: NVIDIA NVENC (auto-selected)");
                return "NVENC";
            }
            if (_amfAvailable)
            {
                Logger.Log("Encoder: AMD AMF (auto-selected)");
                return "AMF";
            }
            if (_qsvAvailable)
            {
                Logger.Log("Encoder: Intel Quick Sync (auto-selected)");
                return "QSV";
            }
            Logger.Log("Encoder: CPU (no hardware encoders available)");
            return "CPU";
    }
}
```

**Change 2: Update GetEncoderSettings to use active encoder (replace existing method, lines 354-408)**

```csharp
private (string codec, string qualityArgs) GetEncoderSettings(CaptureSettings settings)
{
    string codec;
    string qualityArgs;

    // Determine active encoder
    string encoder = GetActiveEncoder(settings);

    // Select codec based on format and encoder
    switch (settings.Format)
    {
        case RecordingFormat.HevcMp4:
            codec = encoder switch
            {
                "NVENC" => "hevc_nvenc",
                "AMF" => "hevc_amf",
                "QSV" => "hevc_qsv",
                _ => "libx265"  // CPU
            };
            break;

        case RecordingFormat.UncompressedAvi:
        case RecordingFormat.H264Mp4:
        default:
            codec = encoder switch
            {
                "NVENC" => "h264_nvenc",
                "AMF" => "h264_amf",
                "QSV" => "h264_qsv",
                _ => "libx264"  // CPU
            };
            break;
    }

    // Build quality arguments based on encoder
    if (settings.Quality == VideoQuality.Custom)
    {
        qualityArgs = BuildCustomBitrateArgs(encoder, settings.CustomBitrateMbps);
    }
    else
    {
        qualityArgs = BuildQualityPresetArgs(encoder, settings.Quality);
    }

    return (codec, qualityArgs);
}
```

---

### Phase 4: Quality Preset Mapping

**Goal**: Translate user quality levels to encoder-specific presets and CRF/CQ values.

#### Preset Systems

| Quality Level | libx264/265 | NVENC | AMF | QSV |
|--------------|-------------|-------|-----|-----|
| **Low** | fast, CRF 28 | p4, CQ 28 | speed, QP 28 | fast, CQ 28 |
| **Medium** | fast, CRF 23 | p4, CQ 23 | balanced, QP 23 | medium, CQ 23 |
| **High** | medium, CRF 18 | p5, CQ 18 | quality, QP 18 | medium, CQ 18 |
| **VeryHigh** | medium, CRF 15 | p6, CQ 15 | quality, QP 15 | slow, CQ 15 |
| **Lossless** | slow, CRF 0 | p7, lossless | CQP 0 | veryslow, CQ 0 |

#### Implementation

**File: [FFmpegEncoderService.cs](ElgatoCapture/Services/FFmpegEncoderService.cs)**

**Add helper methods:**

```csharp
private string BuildQualityPresetArgs(string encoder, VideoQuality quality)
{
    var qualityValue = quality switch
    {
        VideoQuality.Low => 28,
        VideoQuality.Medium => 23,
        VideoQuality.High => 18,
        VideoQuality.VeryHigh => 15,
        VideoQuality.Lossless => 0,
        _ => 23
    };

    switch (encoder)
    {
        case "NVENC":
            return BuildNvencQualityArgs(quality, qualityValue);

        case "AMF":
            return BuildAmfQualityArgs(quality, qualityValue);

        case "QSV":
            return BuildQsvQualityArgs(quality, qualityValue);

        case "CPU":
        default:
            return BuildCpuQualityArgs(quality, qualityValue);
    }
}

private string BuildNvencQualityArgs(VideoQuality quality, int qualityValue)
{
    string preset = quality switch
    {
        VideoQuality.Lossless => "p7",
        VideoQuality.VeryHigh => "p6",
        VideoQuality.High => "p5",
        _ => "p4"
    };

    if (quality == VideoQuality.Lossless)
    {
        return $"-preset {preset} -rc lossless -profile:v high -bf 3";
    }
    else
    {
        return $"-preset {preset} -rc vbr -cq {qualityValue} -b:v 0 -profile:v high -bf 3";
    }
}

private string BuildAmfQualityArgs(VideoQuality quality, int qualityValue)
{
    string preset = quality switch
    {
        VideoQuality.Low => "speed",
        VideoQuality.Lossless => "quality",
        VideoQuality.VeryHigh => "quality",
        VideoQuality.High => "quality",
        _ => "balanced"
    };

    if (quality == VideoQuality.Lossless)
    {
        return $"-quality {preset} -rc cqp -qp_i 0 -qp_p 0";
    }
    else
    {
        // AMF uses qp_i (I-frame) and qp_p (P-frame), typically qp_p = qp_i + 2
        return $"-quality {preset} -rc cqp -qp_i {qualityValue} -qp_p {qualityValue + 2}";
    }
}

private string BuildQsvQualityArgs(VideoQuality quality, int qualityValue)
{
    string preset = quality switch
    {
        VideoQuality.Lossless => "veryslow",
        VideoQuality.VeryHigh => "slow",
        VideoQuality.High => "medium",
        _ => "fast"
    };

    // QSV uses -global_quality for constant quality mode
    // Range: 1-51 (lower = better quality)
    return $"-preset {preset} -global_quality {qualityValue}";
}

private string BuildCpuQualityArgs(VideoQuality quality, int qualityValue)
{
    string preset = quality switch
    {
        VideoQuality.Lossless => "slow",
        VideoQuality.VeryHigh => "medium",
        VideoQuality.High => "medium",
        _ => "fast"
    };

    if (quality == VideoQuality.Lossless)
    {
        return $"-preset {preset} -crf 0";
    }
    else
    {
        return $"-preset {preset} -crf {qualityValue}";
    }
}

private string BuildCustomBitrateArgs(string encoder, double bitrateMbps)
{
    var bitrate = (int)(bitrateMbps * 1000);

    switch (encoder)
    {
        case "NVENC":
            return $"-preset p4 -rc cbr -b:v {bitrate}k -maxrate {bitrate}k -bufsize {bitrate * 2}k -profile:v high -bf 3";

        case "AMF":
            return $"-quality balanced -rc cbr -b:v {bitrate}k -maxrate {bitrate}k -bufsize {bitrate * 2}k";

        case "QSV":
            return $"-preset medium -b:v {bitrate}k -maxrate {bitrate}k -bufsize {bitrate * 2}k";

        case "CPU":
        default:
            return $"-b:v {bitrate}k -maxrate {bitrate}k -bufsize {bitrate * 2}k";
    }
}
```

---

### Phase 5: UI Integration

**Goal**: Add encoder selector to UI with availability indicators.

#### Changes Required

**File: [MainWindow.xaml](ElgatoCapture/MainWindow.xaml)**

**Add encoder selector (around line 75, near format selector):**

```xaml
<TextBlock Text="Encoder:" Margin="0,12,0,4"/>
<ComboBox x:Name="EncoderComboBox"
          SelectedItem="{x:Bind ViewModel.SelectedEncoder, Mode=TwoWay}"
          MinWidth="200">
    <ComboBoxItem Content="Auto (Best Available)" Tag="Auto"/>
    <ComboBoxItem Content="NVIDIA NVENC (Hardware)" Tag="NVENC"
                  IsEnabled="{x:Bind ViewModel.IsNvencAvailable, Mode=OneWay}"/>
    <ComboBoxItem Content="AMD AMF (Hardware)" Tag="AMF"
                  IsEnabled="{x:Bind ViewModel.IsAmfAvailable, Mode=OneWay}"/>
    <ComboBoxItem Content="Intel Quick Sync (Hardware)" Tag="QSV"
                  IsEnabled="{x:Bind ViewModel.IsQsvAvailable, Mode=OneWay}"/>
    <ComboBoxItem Content="CPU (Software)" Tag="CPU"/>
</ComboBox>

<!-- Encoder status indicator -->
<TextBlock Margin="0,4,0,0" FontSize="11" Foreground="Gray">
    <Run Text="Available: "/>
    <Run Text="NVENC " Foreground="{x:Bind ViewModel.IsNvencAvailable, Mode=OneWay, Converter={StaticResource BoolToColorConverter}}"/>
    <Run Text="| AMF " Foreground="{x:Bind ViewModel.IsAmfAvailable, Mode=OneWay, Converter={StaticResource BoolToColorConverter}}"/>
    <Run Text="| QSV " Foreground="{x:Bind ViewModel.IsQsvAvailable, Mode=OneWay, Converter={StaticResource BoolToColorConverter}}"/>
</TextBlock>
```

**Add value converter for availability colors:**

```xaml
<!-- In Page.Resources -->
<local:BoolToColorConverter x:Key="BoolToColorConverter"/>
```

**File: [MainWindow.xaml.cs](ElgatoCapture/MainWindow.xaml.cs)** (or separate Converters file)

```csharp
public class BoolToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        bool isAvailable = value is bool b && b;
        return isAvailable ? new SolidColorBrush(Colors.Green) : new SolidColorBrush(Colors.Red);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
```

---

### Phase 6: HDR Enforcement

**Goal**: Force libx265 when HDR is enabled, disable hardware encoders.

#### Changes Required

**File: [ViewModels/MainViewModel.cs](ElgatoCapture/ViewModels/MainViewModel.cs)**

**Update HDR property to enforce CPU encoding:**

```csharp
partial void OnIsHdrEnabledChanged(bool value)
{
    if (value)
    {
        if (!IsHdrSupported)
        {
            IsHdrEnabled = false;
            // Show error: HDR not supported by current device
            return;
        }

        // HDR requires CPU encoding (libx265)
        if (SelectedEncoder != EncoderType.CPU)
        {
            Logger.Log("⚠️ HDR enabled: forcing CPU encoder (GPU encoders lack HDR10 metadata)");
            SelectedEncoder = EncoderType.CPU;
        }

        // HDR requires HEVC format
        if (SelectedFormat != RecordingFormat.HevcMp4)
        {
            Logger.Log("⚠️ HDR enabled: forcing HEVC format");
            SelectedFormat = RecordingFormat.HevcMp4;
        }
    }
}
```

**File: [MainWindow.xaml](ElgatoCapture/MainWindow.xaml)**

**Disable encoder selector when HDR is enabled:**

```xaml
<ComboBox x:Name="EncoderComboBox"
          SelectedItem="{x:Bind ViewModel.SelectedEncoder, Mode=TwoWay}"
          IsEnabled="{x:Bind ViewModel.IsHdrEnabled, Mode=OneWay, Converter={StaticResource InvertBoolConverter}}"
          MinWidth="200">
    <!-- ... items ... -->
</ComboBox>

<TextBlock Text="⚠️ HDR requires CPU encoding (libx265)"
           Foreground="Orange"
           FontSize="11"
           Visibility="{x:Bind ViewModel.IsHdrEnabled, Mode=OneWay}"
           Margin="0,4,0,0"/>
```

---

## Testing Plan

### Test 1: Encoder Detection
**Platform: NVIDIA GPU system**
1. Launch application
2. Check log for encoder detection:
   ```
   === Hardware Encoder Detection ===
   ✓ NVIDIA NVENC (H.264: True, HEVC: True)
   ```
3. Verify UI shows NVENC option enabled

**Platform: AMD GPU system**
1. Launch application
2. Check log for:
   ```
   ✓ AMD AMF (H.264: True, HEVC: True)
   ```

**Platform: Intel iGPU system**
1. Launch application
2. Check log for:
   ```
   ✓ Intel Quick Sync (H.264: True, HEVC: True)
   ```

**Platform: CPU-only system**
1. Launch application
2. Check log for:
   ```
   ⚠️ No hardware encoders available, will use CPU encoding
   ```

---

### Test 2: Auto Encoder Selection
**Platform: NVIDIA GPU**
1. Set Encoder: Auto
2. Start recording with H.264 format
3. Check log: `"Encoder: NVIDIA NVENC (auto-selected)"`
4. Verify FFmpeg args: `-c:v h264_nvenc`

**Platform: AMD GPU (no NVENC)**
1. Set Encoder: Auto
2. Start recording
3. Check log: `"Encoder: AMD AMF (auto-selected)"`
4. Verify FFmpeg args: `-c:v h264_amf`

---

### Test 3: Manual Encoder Selection
**All platforms:**
1. Set Encoder: CPU
2. Start recording with HEVC format
3. Check log: `"Encoder: CPU (user selected)"`
4. Verify FFmpeg args: `-c:v libx265`

**NVIDIA system:**
1. Set Encoder: NVENC
2. Start recording
3. Verify NVENC is used

---

### Test 4: Encoder Fallback
**NVIDIA system:**
1. Set Encoder: AMF (not available)
2. Start recording
3. Check log: `"⚠️ AMF not available, falling back to CPU"`
4. Verify FFmpeg args: `-c:v libx264`

---

### Test 5: HDR Enforcement
**Any system with HDR device:**
1. Enable HDR toggle
2. Set Encoder: NVENC
3. Check log: `"HDR mode active: forcing libx265"`
4. Verify encoder selector is disabled in UI
5. Verify FFmpeg args: `-c:v libx265` with HDR metadata

---

### Test 6: Encoding Performance
**Expected speeds (1080p60):**

| Encoder | Speed | CPU Usage | File Size (1min) |
|---------|-------|-----------|------------------|
| libx264 (CRF 23) | 0.8-1.5x | 40-60% | ~25 MB |
| libx265 (CRF 23) | 0.5-1.0x | 60-80% | ~15 MB |
| h264_nvenc (CQ 23) | 5-15x | <10% | ~30 MB |
| hevc_nvenc (CQ 23) | 5-15x | <10% | ~18 MB |
| h264_amf (QP 23) | 5-12x | <15% | ~32 MB |
| hevc_amf (QP 23) | 5-12x | <15% | ~20 MB |
| h264_qsv (CQ 23) | 3-8x | 10-20% | ~30 MB |

**Acceptable frame loss:**
- Hardware encoders: <0.5%
- CPU encoders: <2%

---

## Trade-offs and Limitations

### Pros
✅ **Flexibility**: Users can choose encoder based on hardware/needs
✅ **Performance**: Hardware encoding 5-20x faster than CPU
✅ **Compatibility**: Supports NVIDIA, AMD, Intel GPUs
✅ **Auto-selection**: Intelligent fallback ensures recording always works
✅ **Future-proof**: Easy to add new encoders (e.g., VideoToolbox on macOS)

### Cons
❌ **Complexity**: More code paths, more testing required
❌ **Quality variance**: Hardware encoders slightly lower quality at same bitrate
❌ **HDR limitation**: GPU encoders can't do HDR10 metadata (libx265 only)
❌ **Platform-specific**: QSV only on Intel, AMF only on AMD, NVENC only on NVIDIA
❌ **FFmpeg dependency**: Requires FFmpeg built with all encoder support

### Known Limitations

**Hardware Encoder Quirks:**
1. **NVENC**: Requires NVIDIA 10-series or newer
2. **AMF**: Requires AMD RX 400-series or newer, driver version matters
3. **QSV**: Requires Intel iGPU enabled in BIOS, may conflict with discrete GPU
4. **All GPU encoders**: Slightly worse quality per bitrate than libx264/265

**HDR Limitation:**
- GPU encoders (NVENC, AMF, QSV) can encode 10-bit pixel data but **cannot write HDR10 metadata** (master-display, max-cll SEI messages)
- This means GPU-encoded HDR will have wide color gamut but players won't know it's HDR
- **Solution**: HDR mode always forces libx265 (CPU)

---

## Implementation Priority

**Phase 1: Detection** - CRITICAL
- Must detect all encoders before anything else

**Phase 2: Selection Model** - CRITICAL
- UI and settings need encoder choice

**Phase 3: Codec Selection** - CRITICAL
- Core logic to use correct encoder

**Phase 4: Quality Mapping** - IMPORTANT
- Each encoder needs proper presets

**Phase 5: UI Integration** - IMPORTANT
- Users need visual control

**Phase 6: HDR Enforcement** - OPTIONAL
- Only needed if HDR plan is implemented first

---

## Future Enhancements

### Multi-Pass Encoding
- Add 2-pass mode for CPU encoders (better quality at target bitrate)
- Not available on hardware encoders

### Look-Ahead / Rate Control
- Expose advanced rate control options:
  - NVENC: `-rc-lookahead 32`, `-spatial-aq`, `-temporal-aq`
  - AMF: `-preanalysis 1`
  - QSV: `-look_ahead 1`

### Encoder Benchmarking
- Run quick 5-second test encode on startup
- Measure actual encoding speed
- Recommend encoder based on performance

### macOS Support
- Add VideoToolbox encoder detection
- `h264_videotoolbox`, `hevc_videotoolbox`

### AV1 Encoding
- Future codec support:
  - `av1_nvenc` (NVIDIA 40-series+)
  - `av1_amf` (AMD RX 7000-series+)
  - `av1_qsv` (Intel Arc)
  - `libaom-av1`, `libsvtav1` (CPU)

---

## Sources
- [FFmpeg NVENC documentation](https://trac.ffmpeg.org/wiki/HWAccelIntro#NVENC)
- [FFmpeg AMF documentation](https://ffmpeg.org/ffmpeg-codecs.html#amf)
- [FFmpeg QSV documentation](https://trac.ffmpeg.org/wiki/Hardware/QuickSync)
- [NVENC vs x264 quality comparison](https://github.com/Koenkk/FFmpeg/wiki/NVENC-vs-x264)
- [AMD AMF encoder parameters](https://github.com/GPUOpen-LibrariesAndSDKs/AMF/blob/master/amf/doc/AMF_Video_Encode_API.pdf)
- [Intel QSV encoder parameters](https://www.intel.com/content/www/us/en/developer/articles/technical/common-bitrate-control-methods-in-intel-media-sdk.html)

# QA Test Findings - 2026-04-02

## Bugs Found

### FIXED: H.264 Flashback recording produces empty output (Critical)
- **Symptom**: Recording with H.264 codec produces 0-byte .mp4 files
- **Root cause**: `EndRecordingAsync` returns stale/zero `endPts` because `ResumeEviction()` only captures end PTS when the eviction pause count reaches 0, but `FinalizeFlashbackRecordingAsync` holds an outer pause
- **Effect**: `GetValidSegmentPaths(startPts, 0)` returns empty list, fallback opens freshly-rotated empty file
- **Fix**: Capture `LatestPts` directly before calling `ResumeEviction` in `EndRecordingAsync` (FlashbackEncoderSink.cs)
- **Status**: FIXED and verified - all 3 codecs now produce valid recordings

### OPEN: App crashes on HDR→SDR transition and some resolution changes (Critical)
- **Symptom**: Toggling HDR OFF crashes. Some resolution changes crash (4K→Source). FPS changes work fine.
- **Reproduce**: `ecctl set hdr on` then `ecctl set hdr off`, or `ecctl set resolution 3840x2160` then `ecctl set resolution Source`
- **Root cause (narrowed)**: NOT in device reinit itself (breadcrumbs show all init phases complete). The Flashback encoder hits `ENCODING_LOOP_FATAL` with `av_bsf_receive_packet AVERROR_INVALIDDATA` ~30s after HDR→SDR transition. This triggers `BeginFatalCaptureCleanup` which runs `CleanupAsync` on a background thread. When the second reinit (from the HDR toggle UI event) happens concurrently, both the cleanup and reinit race to access/dispose native D3D11 resources → native crash.
- **Evidence**: Log shows `FLASHBACK_SINK_ENCODING_LOOP_FATAL` at 02:54:27, then second reinit at 02:54:53, then crash after StopVideoPreview with no REINIT breadcrumbs.
- **Fix direction**: Either gate the reinit behind the fatal cleanup completion, or cancel the fatal cleanup when a new reinit is requested.
- **Additional finding**: The crash also happens WITHOUT the ENCODING_LOOP_FATAL. When FPS changes from 60→120, the first reinit completes at 59.94fps, then ~48s later the card re-reports its native 120fps, triggering a second reinit. The second reinit crashes during or just before StartVideoPreview. The `_previewReinitializeGate` serializes them but native D3D11/MF resources may not be fully released.
- **Status**: OPEN - crashes on any reinit cascade (back-to-back reinits from format re-probe)

### FIXED: CLI `set format h264` rejected (Minor)
- **Symptom**: `ecctl set format h264` returns "Recording format 'h264' is not available"
- **Root cause**: App expects exact string "H.264" (with dot), CLI passed lowercase "h264"
- **Fix**: Added `NormalizeRecordingFormat()` in ecctl CommandHandlers.cs to map aliases
- **Status**: FIXED

### FIXED: Bitrate changes don't apply to Flashback encoder (Minor)
- **Symptom**: `ecctl set bitrate 100` updates the UI setting but `EncoderTargetBitRate` stays at old value
- **Root cause**: Flashback encoder not restarted, and `_currentSettings` not updated with new bitrate
- **Fix**: Added `UpdateEncodingSettings()` to propagate bitrate/audio from ViewModel to CaptureService before restart. `flashback apply` now correctly applies bitrate changes.
- **Status**: FIXED and verified — `set bitrate 50` + `flashback apply` → encoder at 50Mbps, verified via ffprobe

### FIXED: Audio toggle requires manual preview restart before recording (Medium)
- **Symptom**: Toggling audio on/off then recording fails with "Flashback recording settings changed after preview start"
- **Root cause**: `EnsureFlashbackRecordingTopologyMatches` throws when audio topology mismatches
- **Fix**: Auto-restart flashback backend inline when topology mismatches at recording time (CaptureService.cs StartRecordingAsync)
- **Status**: FIXED and verified — audio on→off→record works, audio off→on→record works

### Added: New automation commands
- `RestartFlashback` (ecctl: `flashback apply`) — restarts Flashback encoder with current settings
- `SetMicrophoneEnabled` (ecctl: `set mic on|off`) — toggles microphone recording
- `NormalizeRecordingFormat` — CLI accepts h264/h.264/avc/h265/h.265 as aliases

## Test Results

### Round 2 (with fixes): 16/16 PASS
| Test | Config | Result |
|------|--------|--------|
| H264-1 | H.264 1080p 120fps 50Mbps | PASS (1062 packets) |
| H264-2 | H.264 back-to-back | PASS (1044 packets) |
| HEVC-1 | HEVC 1080p 120fps 50Mbps | PASS (1049 packets) |
| HEVC-2 | HEVC back-to-back | PASS (1052 packets) |
| AV1-1 | AV1 1080p 120fps 50Mbps | PASS (1131 packets) |
| AV1-2 | AV1 back-to-back | PASS (122 packets) |
| H264-RT | H.264 round-trip | PASS (1047 packets) |
| 1Mbps | Bitrate extreme low | PASS (1042 packets) |
| 100Mbps | Bitrate extreme high | PASS (1048 packets) |
| PostApply | After flashback apply | PASS (1047 packets) |
| QualityAuto | Quality: Auto | PASS (1040 packets) |
| QualityLow | Quality: Low | PASS (1050 packets) |
| QualityHigh | Quality: High | PASS (1040 packets) |
| PresetP1 | Preset P1 | PASS (1038 packets) |
| PresetP7 | Preset P7 | PASS (1043 packets) |

### Not Yet Tested
- FPS changes (crashes app)
- Resolution changes (crashes app)
- HDR on/off
- Audio enable/disable
- Split encode modes
- Video format (MJPG/NV12/P010)
- Flashback playback/seek/export
- Window management
- Long-duration recordings
- Flashback buffer eviction under memory pressure

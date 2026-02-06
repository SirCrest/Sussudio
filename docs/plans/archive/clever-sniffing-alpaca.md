# Audio Pipe Timing Fix

## Problem

Recording has a 4+ second audio sync issue caused by startup timing:

```
[22:37:59.200] FFmpeg recording started
[22:38:03.332] [FFmpeg] Input #1, s16le, from '\\.\pipe\ElgatoCaptureAudio':
```

FFmpeg takes ~4 seconds to probe video input before connecting to the audio pipe. During this time, NO audio is being captured because the audio graph starts AFTER FFmpeg.

---

## Root Cause

**Current sequence in StartCompressedRecordingAsync()** (lines 234-278):

```
1. PrepareAudioQueue()              ← Creates empty queue
2. StartEncodingAsync()             ← Starts FFmpeg, waits for pipe connection (~4s)
3. SetupRecordingAudioCaptureAsync() ← Audio starts HERE (too late!)
4. SetupRecordingFrameReaderAsync() ← Video frames start
```

The audio graph starts AFTER FFmpeg begins probing, so there's no audio buffered when FFmpeg finally connects.

---

## Solution

Start audio capture BEFORE FFmpeg starts, so audio samples buffer during FFmpeg's probe phase.

**New sequence:**

```
1. PrepareAudioQueue()              ← Creates empty queue
2. SetupRecordingAudioCaptureAsync() ← Audio starts buffering NOW
3. StartEncodingAsync()             ← FFmpeg starts, audio is already buffering
4. SetupRecordingFrameReaderAsync() ← Video frames start
```

When FFmpeg connects to the audio pipe after 4 seconds, there will be ~4 seconds of audio already queued and ready.

---

## Implementation

### File: [CaptureService.cs](ElgatoCapture/Services/CaptureService.cs)

**Change in StartCompressedRecordingAsync() (around line 260-275):**

Before:
```csharp
await _ffmpegEncoder.StartEncodingAsync(settings, _recordingFile.Path, audioDevice, effectiveFrameRate, frameRateArg);

// Set up audio capture via AudioGraph (pipes samples to FFmpeg)
if (settings.AudioEnabled && !string.IsNullOrEmpty(_audioDeviceName))
{
    await SetupRecordingAudioCaptureAsync();
}

// Set up frame reader for recording
await SetupRecordingFrameReaderAsync(settings);
```

After:
```csharp
// Set up audio capture FIRST - start buffering before FFmpeg starts
// This ensures audio samples are queued during FFmpeg's ~4 second probe phase
if (settings.AudioEnabled && !string.IsNullOrEmpty(_audioDeviceName))
{
    await SetupRecordingAudioCaptureAsync();
    Logger.Log("Audio capture started - buffering while FFmpeg initializes");
}

await _ffmpegEncoder.StartEncodingAsync(settings, _recordingFile.Path, audioDevice, effectiveFrameRate, frameRateArg);

// Set up frame reader for recording
await SetupRecordingFrameReaderAsync(settings);
```

---

## Verification

1. **Build** the application
2. **Start recording** and check log for:
   - `Recording audio graph started` appears BEFORE `FFmpeg process started`
   - Audio pipe connects within expected time
   - No "Dropped audio samples" warnings at startup
3. **Record 30+ seconds** and verify:
   - No video frame drops
   - Audio/video stay in sync
   - Smooth encoding speed (≥1.0x)
4. **Play back** the recording and verify audio is present from the start

---

## Expected Log Output After Fix

```
[...] PrepareAudioQueue called
[...] Recording audio graph started            ← NOW BEFORE FFmpeg
[...] FFmpeg process started (PID: xxx)
[...] Audio writer: waiting for FFmpeg...
[...] (FFmpeg probes video for ~4 seconds)
[...] [FFmpeg] Input #1, s16le, from pipe
[...] Audio writer: pipe connected
[...] Audio writer: drained initial buffer     ← Buffered audio flows
```

The buffered audio from the 4-second delay will be drained immediately when FFmpeg connects.

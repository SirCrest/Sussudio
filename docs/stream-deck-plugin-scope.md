# Stream Deck Plugin — Scope

## What It Is

A Stream Deck plugin that controls ElgatoCapture via the existing named pipe
automation server. No changes to the main app — the pipe protocol already
exposes everything needed.

## Transport

- Named pipe: `ElgatoCaptureAutomation`
- JSON line protocol: one `NamedPipeClientStream` connection per command
- UTF-8 encoding, newline-delimited request/response
- Request: `{"command": <int>, "correlationId": "<guid>", "payload": {...}}`
- Response: `{"Success": bool, "Status": "ok|error|not_ready", "Data": {...}, "Snapshot": {...}}`
- Retry on `Status: "not_ready"` using `RetryAfterMs` hint
- Standard timeout: 15s, long ops (export/verify): 60s

Reference implementation: `tools/ecctl/PipeTransport.cs` (simplest client).

## SDK

Stream Deck SDK 6.x (current). Plugin is a Node.js or .NET process that
receives button events via WebSocket from the Stream Deck application.

Two approaches:

1. **Node.js** — Use `@elgato/streamdeck` SDK. Spawn a child process or use
   `net` module to connect to the named pipe. Simpler setup, weaker pipe
   interop on Windows.
2. **.NET** — Use `StreamDeckToolkit` or raw WebSocket. Native
   `NamedPipeClientStream` for pipe comms. Can share PipeTransport code from
   ecctl directly. Recommended given the existing .NET codebase.

## Actions (Stream Deck Buttons)

### Tier 1 — Ship First

| Action | Pipe Command | Button Behavior |
|--------|-------------|-----------------|
| **Toggle Preview** | `SetPreviewEnabled` (16) | Toggle. Icon shows live/off state. |
| **Toggle Recording** | `SetRecordingEnabled` (17) | Toggle. Red dot when recording. Shows elapsed time if SDK supports title updates. |
| **Flashback Save** | `FlashbackExport` (42) | Single press. Saves last N seconds (configurable in action settings, default 60s). Brief spinner, then checkmark. |
| **Screenshot** | `CapturePreviewFrame` (26) | Single press. Flash feedback. |
| **Status Display** | `GetSnapshot` (1) | Read-only. Polls every 2s. Shows: recording state, codec, resolution, FPS. No press action. |

### Tier 2 — Quality of Life

| Action | Pipe Command | Button Behavior |
|--------|-------------|-----------------|
| **Set Codec** | `SetRecordingFormat` (9) | Multi-action or cycle press: H.264 → HEVC → AV1. Icon shows current codec. |
| **Toggle HDR** | `SetHdrEnabled` (12) | Toggle. Greyed out when `IsHdrAvailable` is false. |
| **Toggle Audio Monitor** | `SetAudioPreviewEnabled` (14) | Toggle. Speaker icon on/off. |
| **Flashback Play/Pause** | `FlashbackAction` (41) | Toggle play/pause. |
| **Flashback Go Live** | `FlashbackAction` (41) | Single press, sends `go-live`. |
| **Set Quality** | `SetQuality` (10) | Cycle: Low → Medium → High → SuperHigh. |
| **Mute/Unmute Audio** | `SetAudioEnabled` (13) | Toggle. |

### Tier 3 — Power User

| Action | Pipe Command | Button Behavior |
|--------|-------------|-----------------|
| **Switch Audio Mode** | `SetDeviceAudioMode` (36) | Toggle HDMI / Analog. |
| **Select Device** | `SelectDevice` (4) | Dial or multi-action with device list from `GetCaptureOptions` (29). |
| **Window Control** | `WindowAction` (19) | Minimize / restore / close. |
| **Verify Last Recording** | `VerifyLastRecording` (21) | Single press. Shows pass/fail result. |

## State Polling

The plugin needs to poll `GetSnapshot` (command 1) to keep button states
current. The snapshot includes everything needed:

- `IsPreviewing` — preview button state
- `IsRecording` — record button state
- `IsHdrAvailable` / `IsHdrEnabled` — HDR button state + availability
- `IsAudioEnabled` / `IsAudioPreviewEnabled` — audio button states
- `SelectedRecordingFormat` — codec display
- `SelectedQuality` — quality display
- `SelectedResolution` / `SelectedFrameRate` — status display

Poll interval: 2 seconds when Stream Deck is visible, 10 seconds when
minimized. Use a single shared poll — don't poll per-button.

## Plugin Manifest Structure

```
com.elgato.capture/
  manifest.json          — plugin metadata, action definitions
  bin/
    ElgatoCaptureSD.exe  — .NET 8 plugin process (or index.js for Node)
    PipeTransport.cs     — copy from ecctl or shared lib
  images/
    preview-on.svg
    preview-off.svg
    record-on.svg
    record-off.svg
    flashback-save.svg
    screenshot.svg
    status.svg
    codec-h264.svg
    codec-hevc.svg
    codec-av1.svg
    hdr-on.svg
    hdr-off.svg
    audio-on.svg
    audio-off.svg
  property-inspector/
    index.html           — per-action settings (flashback duration, etc.)
```

## Action Settings (Property Inspector)

- **Flashback Save**: duration in seconds (default 60, range 5-300)
- **Screenshot**: output directory override (default: app's output path)
- **Status Display**: which fields to show (resolution, codec, FPS, HDR)
- **Set Codec**: which codecs to cycle through
- **Connection**: pipe name override (default: `ElgatoCaptureAutomation`)

## Error Handling

- **App not running**: Pipe connection fails. Show grey/disconnected icon on
  all buttons. Retry connection every 5 seconds.
- **App not ready**: `Status: "not_ready"` response. Same as disconnected
  display. Retry using `RetryAfterMs`.
- **Command fails**: `Success: false`. Flash error state on the button for 2
  seconds, then revert to current state.
- **Export in progress**: `FlashbackExport` blocks until complete (up to 60s
  timeout). Show spinner on button during export.

## What the Plugin Does NOT Do

- No OBS-style scene switching or source management
- No audio mixing or level control (volume is a single slider, not a mixer)
- No video preview on the Stream Deck display (not enough bandwidth)
- No multi-device management (app is one-device-at-a-time)
- No direct USB communication — everything goes through the pipe server

## Dependencies on Main App

None. The pipe server and command protocol are stable and already consumed by
three clients (ecctl, McpServer, AutomationClient). The plugin is client #4.
No app-side changes required for Tier 1 or Tier 2. Tier 3's device selection
may benefit from a dedicated `GetDeviceList` command if `GetCaptureOptions`
is too heavy for frequent polling, but that's an optimization, not a blocker.

## Estimated Scope

- Tier 1 (5 actions): ~2 days for a working prototype with icons
- Tier 2 (7 actions): ~2 more days
- Tier 3 (4 actions): ~1 day
- Polish (icons, property inspector, error states): ~2 days
- Total: ~7 days for full plugin

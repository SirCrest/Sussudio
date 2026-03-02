# ElgatoCapture

WinUI 3 desktop app for local HDMI capture preview and recording on Windows.

This README is based on the current code in this repository and is intended to double as a human-readable project scope.

## What This App Does

- Enumerates video capture devices and associated audio capture devices.
- Shows live preview with:
  - GPU-first rendering path (`MediaPlayerElement`).
  - CPU frame-reader fallback path (`SoftwareBitmapSource` + `Image`).
- Records video to:
  - `H.264 (MP4)` (when supported by local FFmpeg encoders),
  - `HEVC (MP4)` (when supported),
  - `AV1 (MP4)` (when supported).
- Supports recording quality presets (`Auto`, `Low`, `Medium`, `High`, `Super High`, `Custom`) and custom bitrate (`1-300 Mbps`).
- Supports resolution + frame rate selection from each device's discovered format list.
- Supports HDR preference toggle when the selected device exposes HDR-capable formats.
- Supports audio controls:
  - Enable/disable recording audio.
  - Enable/disable audio preview to speakers.
  - Select custom audio input device (with hot-switching when not recording).
  - Live audio meter + clip indicator.
- Supports output folder selection and displays:
  - Free disk space,
  - Recording elapsed time,
  - Recording size,
  - Rolling bitrate estimate.

## Project Scope

### In Scope (Implemented)

- Single-machine, local capture + record workflow.
- One active selected capture device at a time.
- Low-latency preview with automatic fallback strategy.
- FFmpeg-backed compressed recording pipeline with queueing/drop-policy controls.
- Audio capture strategies for compressed recording:
  - Default post-mux workflow (video + RF64 WAV temp audio + final mux).
  - Experimental named-pipe live audio into FFmpeg.
- Robust lifecycle handling:
  - Serialized session transitions (`Initialize`, `Start/Stop Recording`, `Start/Stop Audio Preview`, `Cleanup`),
  - Health/diagnostic snapshots,
  - Timeout-based cleanup/dispose behavior,
  - Structured logging and crash breadcrumbs.

### Out of Scope (Not Present In Current Code)

- Live streaming integrations (Twitch/YouTube/RTMP/etc.).
- Multi-device simultaneous capture.
- Timeline editing, overlays, scenes, transitions, or compositing.
- Cloud upload/sync/account features.
- Cross-platform support (this is Windows-only).

## Architecture Overview

### UI + ViewModel

- `ElgatoCapture/MainWindow.xaml`: full desktop UI (device selection, preview, recording controls, audio controls, output path, status).
- `ElgatoCapture/MainWindow.xaml.cs`: UI orchestration, preview renderer lifecycle, binding sync, event handling.
- `ElgatoCapture/ViewModels/MainViewModel.cs`: state, commands, timer-driven stats, capture settings creation, coordinator interaction.

### Core Services

- `ElgatoCapture/Services/DeviceService.cs`
  - Enumerates video/audio devices.
  - Discovers and ranks supported video formats.
  - Associates best-matching audio device with each capture device.
- `ElgatoCapture/Services/CaptureService.cs`
  - Owns capture session lifecycle and recording orchestration.
  - Handles recording backend selection, frame/audio pipelines, muxing, and cleanup.
  - Emits status/error/frame/audio-level events.
- `ElgatoCapture/Services/CaptureSessionCoordinator.cs`
  - Serializes capture operations through a single worker queue.
  - Tracks session snapshot state and pending commands.
- `ElgatoCapture/Services/FFmpegEncoderService.cs`
  - Probes local FFmpeg encoder support.
  - Spawns FFmpeg process and feeds raw video/audio via pipes/queues.
  - Handles queue pressure, drop strategy, and stop-time drain/timeout logic.
- `ElgatoCapture/Services/RecordingArtifactManager.cs`
  - Creates temp/final output artifact set.
  - Finalizes or preserves recovery artifacts on mux failure.
- Recording sink contracts and backends:
  - `FfmpegRecordingSink.cs` (FFmpeg encode/mux),
  - `MediaCaptureIngestSession.cs` (MediaCapture -> frames/audio -> sink).

### Models

- Capture settings, formats, state and diagnostics:
  - `CaptureSettings`, `MediaFormat`, `CaptureDevice`, `AudioInputDevice`,
  - `CaptureSessionState`, `CaptureHealthSnapshot`, `CaptureDiagnosticsSnapshot`,
  - `RecordingPipelineOptions`, `RecordingStats`, `EncoderSupport`.

## Recording Pipeline Details

### Compressed (MP4) Path

1. Capture frames via `MediaFrameReader`.
2. Queue into conversion channel with bounded capacity and drop policy.
3. Convert frames to `NV12` when needed.
4. Send frames to FFmpeg sink.
5. Audio:
   - Default mode: capture to RF64 WAV temp file, then mux into final MP4 at stop.
   - Experimental mode: feed float PCM audio into FFmpeg named pipe during recording.

### Current Recording Backend

- Current runtime recording path is MediaCapture frame ingest -> FFmpeg encode/mux (`MediaCaptureIngestSession` + `FfmpegRecordingSink` + `FFmpegEncoderService`).

## Runtime Dependencies

- Windows desktop (x64).
- .NET 8 target framework (`net8.0-windows10.0.19041.0`).
- WinUI 3 / Windows App SDK.
- FFmpeg available at runtime for compressed recording/muxing.
  - Path discovery checks:
    - `<app>\ffmpeg\ffmpeg.exe`
    - `<app>\ffmpeg.exe`
    - `C:\Program Files\ffmpeg\bin\ffmpeg.exe`
    - `ffmpeg.exe` via `PATH`

## Build and Run

### Visual Studio

- Open `ElgatoCapture.slnx`.
- Use `x64` configuration.
- Run profile:
  - `ElgatoCapture (Unpackaged)` for local dev runs,
  - `ElgatoCapture (Package)` for MSIX packaging flow.

Helper script:

- `RunApp.ps1` opens the solution in Visual Studio.

### CLI

- Build:
  - `dotnet build ElgatoCapture\ElgatoCapture.csproj -c Debug -p:Platform=x64`
  - To stage outputs into `latest-build\` at repo root: add `-p:StageLatestBuild=true`.
- Staging script:
  - `tools/stage-builds.ps1` builds Debug/Release and mirrors outputs to `builds/win-x64/*`.
- Reliability gate script:
  - `tools/reliability-gates.ps1` runs a bounded build gate and fails on `MVVMTK0045` (and optionally any warnings).
- Runtime snapshot regression tests:
  - `dotnet run --project tests/ElgatoCapture.Tests/ElgatoCapture.Tests.csproj -c Debug -p:Platform=x64`
- Automation smoke script:
  - `powershell -ExecutionPolicy Bypass -File tools/automation-snapshot-smoke.ps1`

## Configuration (Environment Variables)

- `ELGATOCAPTURE_PREVIEW_RESIZE_DEBOUNCE_MS` (default: `250`, clamp: `50-2000`)
  - Suppresses preview presents during resize churn.
- `ELGATOCAPTURE_PREVIEW_USE_GPU` (default: `true`)
  - Enables/disables GPU preview path.
- `ELGATOCAPTURE_PREVIEW_START_TIMEOUT_MS` (default: `10000`, clamp: `1000-15000`)
  - Max wait for strict dual-signal startup readiness (`MediaOpened` + first capture frame) before watchdog marks startup as failed (no hidden auto-retry).
- `ELGATOCAPTURE_FORCE_FRAME_READER_DURING_RECORDING` (default: `false`)
  - Forces frame-reader preview compatibility mode on recording start.
- `ELGATOCAPTURE_PREVIEW_SHUTDOWN_TIMEOUT_MS` (default: `3000`, clamp: `250-30000`)
  - Timeout for preview frame-reader stop during window close.
- `ELGATOCAPTURE_RECORDING_FIRST_FRAME_TIMEOUT_MS` (default: `5000`, clamp: `500-30000`)
  - Timeout when probing recording frame source viability.
- `ELGATOCAPTURE_SESSION_TRANSITION_TIMEOUT_MS` (default: `60000`, clamp: `1000-300000`)
  - Timeout for capture session transition lock acquisition.
- `ELGATOCAPTURE_DISPOSE_CLEANUP_TIMEOUT_MS` (default: `30000`, clamp: `1000-300000`)
  - Cleanup timeout for `CaptureService` dispose flows.
- `ELGATOCAPTURE_COORDINATOR_DISPOSE_TIMEOUT_MS` (default: `15000`, clamp: `1000-300000`)
  - Drain timeout for command coordinator shutdown.
- `ELGATOCAPTURE_VIEWMODEL_DISPOSE_STEP_TIMEOUT_MS` (default: `30000`, clamp: `1000-300000`)
  - Per-step timeout for ViewModel dispose sequence.
- `ELGATOCAPTURE_VIEWMODEL_DISPOSE_TIMEOUT_MS` (default: `30000`, clamp: `1000-300000`)
  - Overall ViewModel dispose timeout.

## Logging and Diagnostics

- Log file (dev / repo run): `temp\logs\ElgatoCapture_Debug.log`.
- Log file (non-repo fallback): `%LOCALAPPDATA%\ElgatoCapture\logs\ElgatoCapture_Debug.log`.
- Automation diagnostics guide: `docs/automation.md`.
- Includes:
  - system info (in verbose mode),
  - session transition events,
  - FFmpeg command/stderr output,
  - periodic capture health snapshots,
  - capture diagnostics snapshot at recording stop,
  - fatal breadcrumbs before fail-fast termination for non-recoverable unhandled exceptions.

## Known Current Constraints

- Target platform is x64 only.
- Compressed recording quality/codec availability depends on local FFmpeg build and installed encoders.
- Automated tests currently cover runtime snapshot/telemetry logic; device pipeline behavior still requires manual smoke validation.

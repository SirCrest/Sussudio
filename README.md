# Sussudio

Sussudio is a Windows HDMI capture application focused on one-device local
recording. It is built around WinUI 3, Media Foundation source-reader capture,
D3D11 preview rendering, WASAPI audio, and in-process FFmpeg/libav encoding.

This repository is for engineering review and team collaboration. It is not a
marketing page and it is not a finished public release.

## Current Status

- Pre-release internal app.
- Primary hardware target today is Elgato 4K X, with broader UVC support treated
  as future work.
- Main app source lives under `Sussudio/`.
- Historical documents and older log entries may still use the former
  `ElgatoCapture` name. Treat those as old-path references unless the current
  source or README says otherwise.
- Generated diagnostics, local agent state, build outputs, captures, and other
  large artifacts are intentionally ignored.

## What Works Today

- Device and audio endpoint enumeration.
- WinUI 3 UI with live D3D11 preview.
- Recording to MP4 through in-process libav/FFmpeg bindings.
- H.264, HEVC, and AV1 recording formats.
- HDR recording pipeline for HEVC/AV1 with strict validation requirements.
- Preview and recording share the same Media Foundation source-reader callback,
  so starting or stopping a recording is designed not to restart the preview.
- HDMI audio capture through WASAPI.
- Optional microphone recording as a separate track.
- Live audio monitoring with volume control.
- Audio input switching between HDMI and analog/Chat Link path through the
  device audio control service.
- Pure C# UVC extension-unit telemetry for HDMI source state on supported
  devices.
- Flashback/retroactive recording implementation with MPEG-TS segment buffering,
  timeline playback, and export support.
- Automation through a named-pipe server, `ssctl`, the MCP bridge, and the
  generic automation client.
- Runtime diagnostics, frame pacing data, audio telemetry, and diagnostic-session
  artifacts for evidence-based testing.

## Active / Not Yet Final

- 4K120 MJPEG capture support is implemented but still performance-sensitive.
  The NVDEC/CUDA/D3D11 path and CPU fallback both need real hardware validation.
- Flashback is present in `main`, but it is still an active hardening area.
  Playback cadence, A/V sync, export behavior, and live-pipeline interaction
  should be validated with diagnostic sessions before treating results as final.
- The current encoder path is NVIDIA NVENC-oriented. There is no software encode
  fallback for the main recording pipeline.
- FFmpeg runtime DLLs are not checked into the repo. The project expects local
  FFmpeg binaries under `Sussudio/ffmpeg/` for runtime scenarios that need them.
- The Stream Deck plugin is a companion effort, not part of this app repository.
  This repo contains the app automation surface that a plugin can call.

## Build Requirements

- Windows 10/11.
- x64 machine.
- .NET 8 SDK.
- Visual Studio / Build Tools with Windows desktop and WinUI/Windows App SDK
  support.
- Windows App SDK runtime for unpackaged debug runs.
- FFmpeg runtime binaries available locally when testing recording, verification,
  or playback paths that load libav.

The app targets:

- `net8.0-windows10.0.19041.0`
- `win-x64`
- WinUI 3 / Windows App SDK

## Build

From the repository root:

```powershell
dotnet build Sussudio\Sussudio.csproj -p:Platform=x64 -p:StageLatestBuild=true
```

`StageLatestBuild=true` copies the debug build output to `latest-build/`, which
is ignored by Git and useful for local manual runs.

## Test

Main regression harness:

```powershell
dotnet run --project tests\Sussudio.Tests\ -- "Sussudio/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/Sussudio.dll"
```

For deeper runtime validation, especially capture/recording/Flashback work, use
diagnostic sessions instead of one-off snapshots:

```powershell
dotnet tools\ssctl\bin\Debug\net8.0\ssctl.dll diagnostic-session --scenario preview-only --seconds 60 --sample-ms 5000 --presentmon
dotnet tools\ssctl\bin\Debug\net8.0\ssctl.dll diagnostic-session --scenario recording-only --seconds 30 --sample-ms 5000
```

Other useful checks:

```powershell
powershell -ExecutionPolicy Bypass -File tools\automation-snapshot-smoke.ps1
powershell -ExecutionPolicy Bypass -File tools\validate_hdr.ps1 -FilePath <output.mp4> -ExpectHdr
```

The automation smoke test requires the app to be running.

## Repository Layout

| Path | Purpose |
|------|---------|
| `Sussudio/` | WinUI app, capture pipeline, preview renderer, recording, audio, Flashback, automation server |
| `tests/Sussudio.Tests/` | Console-based regression harness |
| `tests/Sussudio.HdrLab/` | HDR ingest/validation lab |
| `tests/Sussudio.FfmpegEncodeLab/` | FFmpeg/libav encode lab |
| `tools/Common/` | Shared automation protocol, command catalog, formatting, diagnostic-session helpers |
| `tools/ssctl/` | Preferred command-line automation client |
| `tools/McpServer/` | MCP bridge over the app automation pipe |
| `tools/AutomationClient/` | Lower-level named-pipe automation client |
| `tools/NativeXuAudioProbe/` | Standalone probe for native UVC/I2C audio control work |
| `tools/CoreAudioEndpointProbe/` | WASAPI endpoint diagnostic probe |
| `tools/KsAudioNodeProbe/` | Kernel streaming audio topology probe |
| `tools/EgavdsAudioProbe/` | Elgato audio virtual-device diagnostic probe |
| `docs/` | Architecture notes, constraints, automation docs, design notes, experiment log |
| `temp/`, `artifacts/`, `results/`, `latest-build/` | Local generated state; ignored |

## Important Docs

- `docs/project-plan.md` - current architecture map, goals, feature status.
- `docs/constraints.md` - non-negotiable HDR rules.
- `docs/cfr_policy.md` - constant-frame-rate policy.
- `docs/automation.md` - automation snapshot contract and diagnostic-session
  guidance.
- `docs/hfr_mjpeg.md` - 4K120 MJPEG pipeline design.
- `docs/stream-deck-plugin-scope.md` - intended app/plugin automation boundary.
- `docs/experiment_log.md` - append-only development and validation log.

## Automation Notes

The app exposes a JSON automation protocol over a named pipe. The default pipe
name is:

```text
SussudioAutomation
```

The shared command catalog is in `tools/Common/AutomationCommandCatalog.cs`.
When adding automation commands, keep these in sync:

- `Sussudio/Models/AutomationCommandKind.cs`
- `Sussudio/Services/Automation/AutomationCommandDispatcher.cs`
- `tools/Common/AutomationCommandCatalog.cs`
- `tools/ssctl/`
- `tools/McpServer/`
- `tools/AutomationClient/`
- `tools/send-automation-command.ps1`
- `tests/Sussudio.Tests/`

If `SUSSUDIO_AUTOMATION_TOKEN` is set, automation clients must provide the same
token.

## Runtime Logs and Generated Files

Local repo runs write logs to:

```text
temp/logs/Sussudio_Debug.log
```

Packaged installs write logs under:

```text
%LocalAppData%\Sussudio\logs\
```

Generated capture outputs, screenshots, diagnostic sessions, PresentMon output,
build folders, local MCP/Codex/Claude state, and FFmpeg DLLs are ignored by Git.

## Engineering Rules Worth Knowing

- HDR must fail loudly if the pipeline cannot produce valid HDR output.
- Do not silently fall back from HDR to SDR.
- Do not treat a short snapshot as proof of performance. Use timed diagnostic
  sessions for cadence, 1% lows, 5% lows, dropped frames, and A/V sync claims.
- Flashback playback/scrubbing should not contaminate live preview cadence
  metrics.
- Keep `GetSnapshot` and `GetCaptureOptions` separate in automation payloads.
- Prefer repo docs and current source over older experiment-log command paths.

## License

MIT. See `LICENSE`.

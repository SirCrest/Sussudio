# Simple Sussudio

A focused HDMI capture application for Windows. Records video from supported
capture devices with full control over codec, quality, and HDR pipeline.

Built around four ideas:

- **HDR that actually works.** P010 end-to-end. If the pipeline can't hit
  HDR10-PQ, recording fails loudly — never silently fallback to SDR.
- **Zero-blink preview.** Starting or stopping a recording never interrupts
  the live preview. The D3D11 renderer shares the same source-reader callback
  as recording.
- **Flashback buffer.** Continuous rolling capture. Hit Save and the last
  N seconds become a clip on disk.
- **Automation-first.** A named-pipe IPC server plus the `ssctl` CLI and an
  MCP bridge expose full app state, so scripts (and Claude Code) can drive
  preview, recording, and verification end-to-end.

Not a streaming tool, not a multi-scene compositor, not a general-purpose
media player. One device, one recording, one output file.

## Features

- Device + audio enumeration with hot-switching when not recording
- Live D3D11 preview (GPU path) with software-decode fallback
- Recording: H.264, HEVC, AV1 — all to MP4
- Quality presets (Auto/Low/Medium/High/Super High/Custom) plus custom
  bitrate from 1–300 Mbps
- Resolution + frame rate selection per device's discovered format list
- HDR preference toggle when the device exposes HDR-capable formats
- Live audio meter, clip indicator, mic mix, recording-audio toggle
- Output folder picker with free disk space, elapsed time, size, and
  rolling bitrate estimate
- Flashback rolling buffer with on-demand export

## Status

Pre-release. Internal use today; public release soon. The app currently
targets the Elgato 4K X capture device specifically, with broader UVC support
on the roadmap.

## Build

Requires the [.NET 8 SDK](https://dotnet.microsoft.com/download) with the
`net8.0-windows10.0.19041.0` workload, Windows 10 19041+, and the WinUI 3
runtime.

```pwsh
dotnet build Sussudio/Sussudio.csproj -p:Platform=x64 -p:StageLatestBuild=true
```

The full test suite:

```pwsh
dotnet run --project tests/Sussudio.Tests/ -- "Sussudio/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/Sussudio.dll"
```

Logs are written to `temp/logs/Sussudio_Debug.log` from inside the repo, or
`%LocalAppData%\Sussudio\logs\` from a packaged install.

## Repository layout

| Path | What lives here |
|------|------|
| `Sussudio/` | The WinUI 3 app — UI, capture pipeline, D3D11 preview, recording, Flashback |
| `tools/ssctl/` | `ssctl` CLI — drives the app via the named-pipe automation server |
| `tools/McpServer/` | MCP server bridge (~50 tools, exposes app state to Claude Code) |
| `tools/AutomationClient/` | Lightweight pipe client used by scripts |
| `tools/NativeXuAudioProbe/` | Standalone diagnostic for the device's audio control unit |
| `tests/Sussudio.Tests/` | Unit + integration tests (run as a console app) |
| `docs/` | Project plan, constraints, automation reference, design notes |

## Documentation

- `docs/project-plan.md` — goals, scope, and what's explicitly out of scope
- `docs/constraints.md` — pipeline contract (HDR, preview, encoder)
- `docs/automation.md` — IPC protocol and `ssctl` command reference
- `docs/experiment_log.md` — running record of investigations, fixes, and
  long-running diagnostic sessions

## Name

The Phil Collins reference is intentional.

## License

MIT — see [LICENSE](LICENSE).

# Sussudio

Sussudio is a Windows app for recording HDMI capture devices. Plug in a capture
card, see a live low-latency preview, and record straight to MP4 — including
proper 10-bit HDR when your source and codec support it.

It started as a vibecoded side project and grew into a real capture app. It's
still pre-release and rough in places, but the core pipeline works.

## What it can do

- **Live preview** of your HDMI source, rendered on the GPU with D3D11.
- **Record to MP4** in H.264, HEVC, or AV1 using NVIDIA hardware encoding.
- **Real HDR recording** (HEVC/AV1). If the HDR pipeline can't be set up
  correctly, recording fails loudly instead of silently falling back to SDR.
- **Flashback** — a rolling retroactive buffer, so you can scrub back through
  the last few minutes and export a clip of something that already happened.
- **Audio done right**: HDMI audio via WASAPI, optional microphone on a
  separate track, live monitoring with volume control, and switching between
  HDMI and analog/Chat Link inputs.
- **Up to 4K120 capture**, including an MJPEG path with GPU decode.
- **Remote control**: everything the app does can be driven externally — a
  `ssctl` command-line tool, an MCP server (so AI agents can operate the app),
  and a named-pipe JSON protocol underneath both.

## What powers it

- **WinUI 3** on **.NET 8** for the app itself.
- **Media Foundation** for capture — preview and recording share one capture
  stream, so starting a recording never interrupts the preview.
- **Direct3D 11** for preview rendering and video processing.
- **WASAPI** for audio capture and monitoring.
- **FFmpeg/libav** (via FFmpeg.AutoGen) for encoding and muxing, with **NVENC**
  doing the heavy lifting. Release downloads bundle the native FFmpeg DLLs; if
  you're building from source, drop them in `Sussudio/ffmpeg/` yourself (they
  aren't checked into the repo).

## What supports it

- Windows 10/11, x64.
- An NVIDIA GPU for recording (the encoder path is NVENC-only right now — no
  software fallback yet).
- The main hardware target today is the **Elgato 4K X**. Other UVC capture
  devices may work, but broader support is future work.

## Building it

You'll need the .NET 8 SDK and Visual Studio (or Build Tools) with Windows
desktop / Windows App SDK support.

```powershell
dotnet build Sussudio.slnx -p:Platform=x64
```

Tests:

```powershell
dotnet test tests\Sussudio.Tests\Sussudio.Tests.csproj
```

The app lives in `Sussudio/`, the automation tools in `tools/`, and tests in
`tests/`. Logs land in `temp/logs/Sussudio_Debug.log` when running from the
repo.

## License

MIT. See `LICENSE`.

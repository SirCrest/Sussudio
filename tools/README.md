# Tooling guide

`tools/` owns runnable automation programs, hardware probes, and operational
build, release, and validation helpers. Repository analysis and measurement
workflows belong in purpose folders under `scripts/`, such as
[architecture baselines](../scripts/architecture/Capture-SussudioDefragBaseline.ps1)
and [preview measurements](../scripts/performance/Measure-PreviewScenario.ps1).
Keep helpers beside the program or workflow they serve.

## Buildable programs

These nine C# projects are included in [Sussudio.slnx](../Sussudio.slnx).

| Project | Purpose |
| --- | --- |
| [ssctl](ssctl/ssctl.csproj) | Preferred automation CLI and diagnostic sessions. |
| [McpServer](McpServer/McpServer.csproj) | MCP bridge to automation and diagnostics. |
| [AutomationClient](AutomationClient/AutomationClient.csproj) | Low-level named-pipe client; see its [usage guide](AutomationClient/README.md). |
| [CoreAudioEndpointProbe](CoreAudioEndpointProbe/CoreAudioEndpointProbe.csproj) | Capture endpoint enumeration and volume-control probe. |
| [EgavdsAudioProbe](EgavdsAudioProbe/EgavdsAudioProbe.csproj) | Audio routing and gain experiments through Elgato Studio's EGAVDeviceSupport DLL. |
| [KsAudioNodeProbe](KsAudioNodeProbe/KsAudioNodeProbe.csproj) | Kernel Streaming audio node and control probes. |
| [NativeXuAudioProbe](NativeXuAudioProbe/NativeXuAudioProbe.csproj) | Native extension-unit and AT command audio probes. |
| [Sussudio.HdrLab](HdrLab/Sussudio.HdrLab/Sussudio.HdrLab.csproj) | WinRT P010 capture lab for testing HDR-capable video sources. |
| [Sussudio.FfmpegEncodeLab](HdrLab/Sussudio.FfmpegEncodeLab/Sussudio.FfmpegEncodeLab.csproj) | FFmpeg encode and validation harness for HDR lab captures. |

[RtkIoShim](RtkIoShim/rtk_io_shim.cpp) is a separate C++ DLL build. Its
[build.bat](RtkIoShim/build.bat) requires an x64 Visual Studio Developer Command
Prompt and produces the RTK_IO shim; it is outside the C# solution build.

## Shared source

[Common](Common/) and [DiagnosticSession](DiagnosticSession/) have no project
of their own. Both C# project files for `ssctl` and `McpServer` compile these
folders through `Compile Include` globs:

- `Common/` owns shared snapshot formatting and the PresentMon process wrapper.
- `DiagnosticSession/` owns scenario registration, execution, metrics, and reports.

An edit in either directory affects both binaries. Rebuild and validate both
consumers after changing shared source. Automation wire contracts live in the
separately referenced [Sussudio.Automation.Contracts](../Sussudio.Automation.Contracts/)
project.

[Common/PresentMon/PresentMonProbe.cs](Common/PresentMon/PresentMonProbe.cs) is
maintained wrapper code. The optional PresentMon executable downloaded into
`tools/PresentMon/` is a local dependency.

`NativeXuAudioProbe` explicitly links the production capture model and native
XU service sources listed in its project file. Its device model therefore stays
identical to the app's model. The probe supplies a small Trace logging adapter
suited to its console host; the app owns its file logger and background writer.

## Operational scripts

Existing entry-point paths stay stable for callers and release documentation.

| Workflow | Entry points |
| --- | --- |
| Build and release | [stage-builds.ps1](stage-builds.ps1), [reliability-gates.ps1](reliability-gates.ps1), [package-github-release.ps1](package-github-release.ps1) |
| Release support | [release/release-helpers.ps1](release/release-helpers.ps1), [release/test-release-helpers.ps1](release/test-release-helpers.ps1) |
| Live automation | [send-automation-command.ps1](send-automation-command.ps1), [automation-snapshot-smoke.ps1](automation-snapshot-smoke.ps1), [capture-rewrite-live-baseline.ps1](capture-rewrite-live-baseline.ps1) |
| Recording validation | [validate_hdr.ps1](validate_hdr.ps1), [verify-dedicated-libav-recording.ps1](verify-dedicated-libav-recording.ps1) |

The packaging entry point loads its adjacent `release/` helpers. Add new
operational helpers beside their owning tool or workflow; add repository
analysis and measurement scripts under `scripts/<purpose>/`.

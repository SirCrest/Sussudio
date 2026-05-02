# AutomationClient

Small transport executable for Sussudio's named-pipe automation API.

## Build

```powershell
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1'
$env:DOTNET_CLI_HOME=(Resolve-Path 'temp').Path
dotnet build tools/AutomationClient/AutomationClient.csproj -c Debug
```

## Usage

```powershell
.\tools\AutomationClient\bin\Debug\net8.0\AutomationClient.exe --command GetSnapshot --token codex-local --pretty
```

```powershell
.\tools\AutomationClient\bin\Debug\net8.0\AutomationClient.exe --command SetPreviewEnabled --token codex-local --payload '{"enabled":true}'
```

```powershell
.\tools\AutomationClient\bin\Debug\net8.0\AutomationClient.exe --command SetFrameRate --token codex-local --payload-kv frameRate=60
```

```powershell
.\tools\AutomationClient\bin\Debug\net8.0\AutomationClient.exe --command SetVideoFormat --token codex-local --payload-kv videoFormat=MJPG
```

## Notes

- Commands can be passed by name (`GetSnapshot`) or numeric id.
- Payload must be a JSON object string.
- `--payload-kv` can be repeated for quote-safe payload values (`enabled=true`, `frameRate=60`, `format=HEVC (MP4)`).
- Current server enum parsing uses numeric command values under the hood; the client handles that mapping.

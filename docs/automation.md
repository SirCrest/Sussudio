# Automation Diagnostics Guide

This project exposes runtime diagnostics over the named pipe automation server.
The most important payload is the diagnostics snapshot (`GetSnapshot`) which
combines:

- `ViewModelRuntimeSnapshot`
- `CaptureRuntimeSnapshot`
- `PreviewRuntimeSnapshot`
- capture health + recording verification state

## Runtime Snapshot Contract Notes

`CaptureRuntimeSnapshot` is consumed by `AutomationDiagnosticsHub` to produce:

- HDR truth verdict (`HdrTruthVerdict`)
- telemetry alignment status
- mux result state
- frame-format observations used for HDR/SDR classification

If you add new fields to `CaptureRuntimeSnapshot`, update
`CaptureService.GetRuntimeSnapshot` in the same change so the automation
pipeline does not silently drift.

## Regression Checklist

Run these checks before merging automation/runtime changes:

1. `dotnet build ElgatoCapture/ElgatoCapture.csproj -c Debug -p:Platform=x64`
2. `dotnet build ElgatoCapture/ElgatoCapture.csproj -c Release -p:Platform=x64`
3. `powershell -ExecutionPolicy Bypass -File tools/reliability-gates.ps1 -Configuration Debug`
4. `dotnet run --project tests/ElgatoCapture.Tests/ElgatoCapture.Tests.csproj -c Debug -p:Platform=x64`
5. `powershell -ExecutionPolicy Bypass -File tools/automation-snapshot-smoke.ps1 -PipeName <pipe> -AuthToken <token>`
6. Manual smoke:
   - device enumerate
   - preview start/stop
   - record start/stop
   - output file exists
7. Automation smoke:
   - run `tools/automation-snapshot-smoke.ps1` to validate idle -> preview ->
     recording -> stopped transitions
   - verify `TelemetryAlignmentStatus`, observed format counters, HDR state
     fields, and mux fields are populated and internally consistent

## Edge Cases To Cover

- Telemetry unavailable/stale:
  - `SourceTelemetryAvailability` should move to unavailable/stale state
  - `TelemetryAlignmentStatus` should become `Unavailable`/`Inconclusive`
- HDR requested but non-P010 ingress:
  - `HdrAutoDowngraded` and `HdrDowngradeCode` should be populated
- Cleanup cancellation:
  - preview and recording stop paths should honor cancellation tokens
- Audio pipe connection lag:
  - retry loop should timeout predictably without leaving pending connect work
- Dispose-time failures:
  - cleanup exceptions should be logged (not swallowed silently)

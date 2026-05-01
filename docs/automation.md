# Automation Diagnostics Guide

This project exposes runtime diagnostics over the named pipe automation server.
The most important payload is the diagnostics snapshot (`GetSnapshot`) which
combines:

- `ViewModelRuntimeSnapshot`
- `CaptureRuntimeSnapshot`
- `PreviewRuntimeSnapshot`
- capture health + recording verification state

The top-level diagnostic fields are the preferred first read:

- `DiagnosticHealthStatus`
- `DiagnosticLikelyStage`
- `DiagnosticSummary`
- `DiagnosticEvidence`
- `DiagnosticSourceLane`
- `DiagnosticDecodeLane`
- `DiagnosticPreviewLane`
- `DiagnosticRenderLane`
- `DiagnosticPresentLane`
- `DiagnosticRecordingLane`
- `DiagnosticAudioLane`

The older `PerformanceScore` fields are still present for compatibility, but
new tools should lead with the diagnostic health/stage/evidence fields.

## Timed Diagnostic Sessions

Use timed diagnostic sessions when validating live capture behavior. They
sample snapshots, export recent frame-ledger events, capture the performance
timeline, optionally capture PresentMon, and verify recordings when the
scenario records.

Do not treat a single live snapshot as proof of cadence, 1% lows, 5% lows, or
steady-state A/V sync. Use at least a 30-second run for 4K120 preview/playback
smoke validation, prefer 60 seconds when making optimization decisions from
1%/5% lows, and include the run duration plus sample interval in any reported
result.

CLI:

```powershell
dotnet tools/ecctl/bin/Debug/net8.0/ecctl.dll diagnostic-session --scenario preview-only --seconds 10 --sample-ms 500 --presentmon
dotnet tools/ecctl/bin/Debug/net8.0/ecctl.dll diagnostic-session --scenario recording-only --seconds 10 --sample-ms 500
```

MCP:

- `run_diagnostic_session`

Scenarios:

- `observe`
- `preview-only`
- `recording-only`
- `flashback`
- `flashback-playback`
- `combined`

Each run writes:

- `summary.json`
- `samples.json`
- `frame-ledger.json`
- `timeline.json`

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
6. Diagnostic-session smoke:
   - preview-only with PresentMon
   - recording-only with strict verification
   - flashback
   - combined
7. Manual smoke:
   - device enumerate
   - preview start/stop
   - record start/stop
   - output file exists
8. Automation smoke:
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

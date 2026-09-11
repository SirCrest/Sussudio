# Diagnostic composition compatibility fixtures

These fixtures were captured from the existing `ssctl.dll` before removing the diagnostic result projection layers and scenario flag matrix. The binary contained `DiagnosticSessionResultProjectionSet` and had SHA-256 `1D2C51A0AEB9D7526256008B4E1F08D667ECB5E64183AB5B8A5F4B3E8AD9D760`.

- `retained-analysis.json` records all 197 serialized result fields. `DiagnosticCompositionFixture.BuildResult` supplies distinct deterministic values in the retained analysis so field swaps and omissions are visible.
- `playback-metrics.json` records extraction when playback was observed and when it was not observed, including the retained counters and observed-only snapshot defaults.
- `scenarios.json` records all 25 public names in catalog order, their startup requirements, export filenames, and warning policies. The `combined` entry retains all three startup requirements and its existing policy set.

The fixture capture invokes only managed result, metric, and catalog code. It does not run a diagnostic session or access devices. Update an expected fixture only for an intentional contract change after examining the field-level differences.